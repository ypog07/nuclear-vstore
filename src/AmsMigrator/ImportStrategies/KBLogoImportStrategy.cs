using System;
using System.Collections.Generic;
using System.Text;
using AmsMigrator.DTO.Okapi;
using AmsMigrator.Models;
using System.Threading.Tasks;
using AmsMigrator.DTO;
using System.Linq;

using AmsMigrator.Infrastructure;

namespace AmsMigrator.ImportStrategies
{
    public class KBLogoImportStrategy : AdvertisementMaterialImportStrategy
    {
        public KBLogoImportStrategy (ImportOptions options, IOkapiClient okapiClient)
            : base(options, okapiClient) { }

        protected override string Name => "KB Logotype";

        protected override long[] GetBindedNomenclatures() => _options.KbNomenclatureCodes;

        protected override (string, long) GetCreationTarget()
        {
            if (_options.KbTemplateCode != null)
            {
                return (By.Template, _options.KbTemplateCode.Value);
            }
            return (By.Nomenclature, _options.KbNomenclatureCodes.First());
        }

        protected override async Task PatchStubAsync(MaterialStub advMaterialStub, Amsv1MaterialData amsv1Data)
        {
            // original logo
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

            advMaterialStub.Properties.Name = "logo_kb";
        }
    }
}
