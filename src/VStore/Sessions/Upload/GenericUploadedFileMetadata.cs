namespace NuClear.VStore.Sessions.Upload
{
    public sealed class GenericUploadedFileMetadata : IUploadedFileMetadata
    {
        public GenericUploadedFileMetadata(FileType fileType, string fileName, string contentType, long fileLength)
        {
            FileType = fileType;
            FileName = fileName;
            ContentType = contentType;
            FileLength = fileLength;
        }

        public FileType FileType { get; }
        public string FileName { get; }
        public string ContentType { get; }
        public long FileLength { get; }
    }
}