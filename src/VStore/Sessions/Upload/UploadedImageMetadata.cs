using NuClear.VStore.Descriptors;

namespace NuClear.VStore.Sessions.Upload
{
    public sealed class UploadedImageMetadata : IUploadedFileMetadata
    {
        public UploadedImageMetadata(FileType fileType, string fileName, string contentType, long fileLength, ImageSize size)
        {
            FileType = fileType;
            FileName = fileName;
            ContentType = contentType;
            FileLength = fileLength;
            Size = size;
        }

        public FileType FileType { get; }
        public string FileName { get; }
        public string ContentType { get; }
        public long FileLength { get; }

        public ImageSize Size { get; }
    }
}