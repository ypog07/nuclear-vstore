﻿using System;

namespace NuClear.VStore.Descriptors.Templates
{
    public class LinkElementConstraints : PlainTextElementConstraints, IEquatable<LinkElementConstraints>
    {
        public bool ValidLink => true;

        public override bool Equals(object obj)
        {
            var other = obj as LinkElementConstraints;
            return Equals(other);
        }

        public bool Equals(LinkElementConstraints other) => base.Equals(other);

        public override int GetHashCode() => base.GetHashCode();
    }
}
