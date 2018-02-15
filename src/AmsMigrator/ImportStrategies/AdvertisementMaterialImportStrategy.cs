using AmsMigrator.DTO.Okapi;
using AmsMigrator.Exceptions;
using AmsMigrator.Models;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using AmsMigrator.Infrastructure;

namespace AmsMigrator.ImportStrategies
{
    public abstract class AdvertisementMaterialImportStrategy
    {
        protected readonly IOkapiClient _okapiClient;
        protected ImportOptions _options;
        private ILogger _logger = Log.Logger;

        public AdvertisementMaterialImportStrategy(ImportOptions options, IOkapiClient okapiClient)
        {
            _options = options;
            _okapiClient = okapiClient;
        }

        public async Task<MaterialCreationResult> ExecuteAsync(Amsv1MaterialData amsv1Data)
        {
            try
            {
                _logger.Information("Starting to execute {name} import strategy; Am id {uuid}; Firm id: {firmid}", Name, amsv1Data.Uuid, amsv1Data.FirmId);

                var (byWhat, targetCode) = GetCreationTarget();

                _logger.Information("Creating stub for material id: {uuid}", amsv1Data.Uuid);

                var advMaterialStub = await _okapiClient.CreateMaterialStubAsync(byWhat, targetCode.ToString(), amsv1Data.FirmId, _options.Language);

                _logger.Information("Stub for material id {uuid} successfully created. Stub id: {stubId}", amsv1Data.Uuid, advMaterialStub.Id);
                _logger.Information("Stub patching started. AMS v1.0 material id {uuid}, OKAPI stub id: {stubId}", amsv1Data.Uuid, advMaterialStub.Id);

                await PatchStubAsync(advMaterialStub, amsv1Data);

                _logger.Information("Creating material started. AMS v1.0 material id {uuid}, OKAPI stub id: {stubId}", amsv1Data.Uuid, advMaterialStub.Id);
                var material = await _okapiClient.CreateNewMaterialAsync(advMaterialStub.Id, amsv1Data.FirmId, advMaterialStub);

                _logger.Information("Material {stubId} has been created", advMaterialStub.Id);
                _logger.Information("Strategy {name} has been completed successfully. Material id: {uuid}; Firm id: {firmid}", Name, amsv1Data.Uuid, amsv1Data.FirmId);

                return new MaterialCreationResult
                {
                    MaterialId = advMaterialStub.Id,
                    FirmId = amsv1Data.FirmId,
                    BindedNomenclatures = GetBindedNomenclatures(),
                    VersionId = material.VersionId
                };
            }
            catch (UnprocessableEntityException ueex)
            {
                _logger.Error("[UNPROCESSABLE] {uuid} {firmid} {content} {header}", amsv1Data.Uuid, amsv1Data.FirmId, ueex.Content, ueex.CustomImageHeader);

                return null;
            }
            catch (Exception)
            {
                _logger.Information("Failed to process AM with AMS 1.0 id: {uuid} {firmid}", amsv1Data.Uuid, amsv1Data.FirmId);
                throw;
            }
        }

        protected abstract Task PatchStubAsync(MaterialStub stub, Amsv1MaterialData amsv1Data);

        protected abstract (string, long) GetCreationTarget();
        protected abstract long[] GetBindedNomenclatures();
        protected abstract string Name { get; }
    }
}
