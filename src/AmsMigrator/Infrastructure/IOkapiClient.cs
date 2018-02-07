using System;
using System.Threading.Tasks;

using AmsMigrator.DTO.Okapi;
using AmsMigrator.Models;

namespace AmsMigrator.Infrastructure
{
    public interface IOkapiClient
    {
        Task<MaterialStub> CreateMaterialStubAsync(string type, string code, long firm, string language);
        Task<MaterialStub> CreateNewMaterialAsync(long id, long firm, MaterialStub stub);
        Task<bool> SetModerationState(long firmId, string version, Amsv1MaterialData materialData);
        Task<UploadResponse> UploadFileAsync(long advertisementId, Uri uploadUrl, string fileName, byte[] fileData, string customImageHeaderValue = null);
    }
}