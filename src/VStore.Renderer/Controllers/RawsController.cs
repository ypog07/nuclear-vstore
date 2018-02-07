using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using NuClear.VStore.Http.Core.Controllers;
using NuClear.VStore.ImageRendering;
using NuClear.VStore.Objects;
using NuClear.VStore.S3;

namespace NuClear.VStore.Renderer.Controllers
{
    [ApiVersion("1.0")]
    [Route("raws")]
    public sealed class RawsController : VStoreController
    {
        private readonly RawFileStorageInfoProvider _rawFileStorageInfoProvider;
        private readonly ObjectsStorageReader _objectsStorageReader;

        public RawsController(RawFileStorageInfoProvider rawFileStorageInfoProvider, ObjectsStorageReader objectsStorageReader)
        {
            _rawFileStorageInfoProvider = rawFileStorageInfoProvider;
            _objectsStorageReader = objectsStorageReader;
        }

        [HttpGet("{sessionId:guid}/{fileKey}")]
        [ProducesResponseType(302)]
        [ProducesResponseType(404)]
        public IActionResult RedirectToRaw(Guid sessionId, string fileKey) => Redirect(_rawFileStorageInfoProvider.GetRawFileUrl(sessionId, fileKey));

        [HttpGet("{id:long}/{versionId}/{templateCode:int}")]
        [ProducesResponseType(302)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RedirectToRaw(long id, string versionId, int templateCode)
        {
            try
            {
                var imageElementValue = await _objectsStorageReader.GetImageElementValue(id, versionId, templateCode);
                return Redirect(_rawFileStorageInfoProvider.GetRawFileUrl(imageElementValue.Raw));
            }
            catch (ObjectNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}