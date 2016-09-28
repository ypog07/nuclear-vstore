﻿using System.Linq;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NuClear.VStore.Host.Options;

namespace NuClear.VStore.Host.Controllers
{
    [Route("api/1.0/content")]
    public sealed class ContentController : Controller
    {
        private readonly IAmazonS3 _amazonS3;
        private readonly string _bucketName;

        public ContentController(IAmazonS3 amazonS3, IOptions<CephOptions> cephOptions)
        {
            _amazonS3 = amazonS3;
            _bucketName = cephOptions.Value.ContentBucketName;
        }

        [HttpGet]
        [Route("list")]
        public async Task<JsonResult> List()
        {
            var response = await _amazonS3.ListVersionsAsync(_bucketName);
            return Json(response.Versions.Where(x => !x.IsDeleteMarker).Select(x => new { x.Key, x.VersionId, x.IsLatest }));
        }

        [HttpGet]
        [Route("{key}/{version}")]
        public async Task<FileStreamResult> Get(string key, string version)
        {
            var response = await _amazonS3.GetObjectAsync(_bucketName, key, version);
            return File(response.ResponseStream, response.Headers.ContentType);
        }

        [HttpPut]
        [Route("{key}")]
        public async Task<string> Put(string key, IFormFile file)
        {
            var response = await _amazonS3.PutObjectAsync(
                               new PutObjectRequest
                                   {
                                       Key = key,
                                       BucketName = _bucketName,
                                       ContentType = file.ContentType,
                                       InputStream = file.OpenReadStream(),
                                       CannedACL = S3CannedACL.PublicRead
                                   });
            return response.VersionId;
        }
    }
}