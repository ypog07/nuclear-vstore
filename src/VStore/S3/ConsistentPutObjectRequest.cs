using Amazon.S3.Model;

namespace NuClear.VStore.S3
{
    public sealed class ConsistentPutObjectRequest : PutObjectRequest
    {
        protected override bool Expect100Continue => false;
    }
}