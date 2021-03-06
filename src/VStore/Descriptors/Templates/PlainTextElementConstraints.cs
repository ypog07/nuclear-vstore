﻿using System;

namespace NuClear.VStore.Descriptors.Templates
{
    public class PlainTextElementConstraints : TextElementConstraints, IEquatable<PlainTextElementConstraints>
    {
        public override bool Equals(object obj)
        {
            var other = obj as PlainTextElementConstraints;
            return Equals(other);
        }

        public bool Equals(PlainTextElementConstraints other) => base.Equals(other);

        public override int GetHashCode() => base.GetHashCode();
    }
}
