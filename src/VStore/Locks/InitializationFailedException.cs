using System;

namespace NuClear.VStore.Locks
{
    public sealed class InitializationFailedException : Exception
    {
        public InitializationFailedException() : base ("Locks infrastructure is not initialized. Check connection endpoints.")
        {
        }
    }
}