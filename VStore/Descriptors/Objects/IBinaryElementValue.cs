﻿namespace NuClear.VStore.Descriptors.Objects
{
    public interface IBinaryElementValue : IObjectElementRawValue
    {
        string Filename { get; }
    }
}