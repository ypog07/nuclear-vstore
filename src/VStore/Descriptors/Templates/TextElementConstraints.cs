﻿using System;

namespace NuClear.VStore.Descriptors.Templates
{
    public abstract class TextElementConstraints : ITextElementConstraints, IEquatable<TextElementConstraints>
    {
        public int? MaxSymbols { get; set; }
        public int? MaxSymbolsPerWord { get; set; }
        public int? MaxLines { get; set; }

        public bool WithoutControlChars => true;

        public bool WithoutNonBreakingSpace => true;

        public bool Equals(TextElementConstraints other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return MaxSymbols == other.MaxSymbols &&
                   MaxSymbolsPerWord == other.MaxSymbolsPerWord &&
                   MaxLines == other.MaxLines;
        }

        public override bool Equals(object obj)
        {
            var other = obj as TextElementConstraints;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MaxSymbols.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxSymbolsPerWord.GetHashCode();
                hashCode = (hashCode * 397) ^ MaxLines.GetHashCode();
                return hashCode;
            }
        }
    }
}
