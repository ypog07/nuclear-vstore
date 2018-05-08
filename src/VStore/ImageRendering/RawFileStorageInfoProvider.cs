using System;

using Amazon.Extensions.NETCore.Setup;

using NuClear.VStore.Options;
using NuClear.VStore.S3;

namespace NuClear.VStore.ImageRendering
{
    public sealed class RawFileStorageInfoProvider
    {
        private readonly string _endpointUrl;

        public RawFileStorageInfoProvider(AWSOptions awsOptions, CephOptions cephOptions)
        {
            _endpointUrl = awsOptions.DefaultClientConfig.ServiceURL.AsRawFilePath(cephOptions.FilesBucketName);
        }

        public string GetRawFileUrl(Guid sessionId, string fileKey) => _endpointUrl.AsRawFilePath(sessionId, fileKey);
        public string GetRawFileUrl(string rawValue) => _endpointUrl.AsRawFilePath(rawValue);
    }
}