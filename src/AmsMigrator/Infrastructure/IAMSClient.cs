using System.Threading.Tasks;
using AmsMigrator.Models;

namespace AmsMigrator.Infrastructure
{
    public interface IAmsClient
    {
        Task<Amsv1MaterialData> GetAmsv1DataAsync(long firmId, bool miniImagesNeeded);
        Task<Amsv1MaterialData[]> GetAmsv1DataAsync(long[] firmIds, bool miniImagesNeeded);
        Task<Amsv1MaterialData[]> GetLogoInfoByUidAsync((string uuid, string hash)[] uuids, bool miniImagesNeeded);
    }
}