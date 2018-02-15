using System;
using System.Collections.Generic;
using System.Linq;
using AmsMigrator.DTO.Okapi;
using AmsMigrator.Models;
using System.Threading.Tasks;
using AmsMigrator.DTO;
using AmsMigrator.Infrastructure;

namespace AmsMigrator.ImportStrategies
{
    public class ZmkLogoImportStrategy : AdvertisementMaterialImportStrategy
    {
        public ZmkLogoImportStrategy(ImportOptions options, IOkapiClient okapiClient)
            : base(options, okapiClient) { }

        protected override string Name => "ZMK Brending";

        protected override long[] GetBindedNomenclatures() => _options.ZmkNomenclatureCodes;

        protected override (string, long) GetCreationTarget()
        {
            if (_options.ZmkTemplateCode != null)
            {
                return (By.Template, _options.ZmkTemplateCode.Value);
            }
            return (By.Nomenclature, _options.ZmkNomenclatureCodes.First());
        }

        protected override async Task PatchStubAsync(MaterialStub advMaterialStub, Amsv1MaterialData amsv1Data)
        {
            // logo
            var logoElement = advMaterialStub.GetElementByType(MaterialElementType.CompositeBitmapImage);

            string uploadUrl = logoElement.UploadUrl;
            var materialId = advMaterialStub.Id;

            string fileName = $"{amsv1Data.ImageName}.{amsv1Data.ImageExt}";
            var uploadHash = await _okapiClient.UploadFileAsync(materialId, new Uri(uploadUrl), fileName, amsv1Data.ImageData);

            logoElement.Value.Raw = uploadHash.Raw;

            // crop area
            logoElement.Value.CropArea = CropAreaFactory.Create(amsv1Data);

            // background color
            var bgColorElement = advMaterialStub.GetElementByType(MaterialElementType.Color);

            bgColorElement.Value.Raw = amsv1Data.BackgroundColor;

            // custom images
            var customImages = new List<SizeSpecificImage>();

            foreach (var c in amsv1Data.SizeSpecificImages)
            {
                var img = await CreateCustomImageAsync(materialId, new Uri(uploadUrl), c);
                customImages.Add(img);
            }

            logoElement.Value.SizeSpecificImages = customImages.ToArray();

            advMaterialStub.Properties.Name = "logo_zmk";
        }

        private async Task<SizeSpecificImage> CreateCustomImageAsync(long materialId, Uri uploadUrl, SizeSpecificImageData data)
        {
            var ci = new SizeSpecificImage
            {
                Size = new SizeSpecificImageSize
                {
                    Height = data.Height,
                    Width = data.Width
                }
            };

            var headerValue = $"{data.Width}x{data.Height}";
            var hash = await _okapiClient.UploadFileAsync(materialId, uploadUrl, data.Name, data.Data, headerValue);
            ci.Raw = hash.Raw;

            return ci;
        }
    }
}
