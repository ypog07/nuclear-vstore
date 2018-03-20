namespace NuClear.VStore.Descriptors.Templates
{
    public interface ISizeRangedImageElementConstraints : IBinaryElementConstraints
    {
        ImageSizeRange ImageSizeRange { get; }
    }
}