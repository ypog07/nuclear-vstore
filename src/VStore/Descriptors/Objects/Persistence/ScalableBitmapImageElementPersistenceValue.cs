namespace NuClear.VStore.Descriptors.Objects.Persistence
{
    public sealed class ScalableBitmapImageElementPersistenceValue : IBinaryElementPersistenceValue
    {
        public ScalableBitmapImageElementPersistenceValue(string raw, string filename, long? filesize, Anchor anchor)
        {
            Raw = raw;
            Filename = filename;
            Filesize = filesize;
            Anchor = anchor;
        }

        public string Raw { get; }
        public string Filename { get; }
        public long? Filesize { get; }
        public Anchor Anchor { get; }
    }
}