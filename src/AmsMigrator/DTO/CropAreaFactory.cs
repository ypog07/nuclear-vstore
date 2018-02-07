using AmsMigrator.DTO.Okapi;
using AmsMigrator.Models;


namespace AmsMigrator.DTO
{
    public class CropAreaFactory
    {
        public static CropArea Create(Amsv1MaterialData data)
        {
            var area = new CropArea
            {
                Top = data.CropTop,
                Left = data.CropLeft,
                Height = data.CropHeight,
                Width = data.CropWidth
            };

            return area;
        }
    }
}
