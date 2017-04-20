﻿namespace NuClear.VStore.Objects.ContentValidation.Errors
{
    public class BinaryTooLargeError : ObjectElementValidationError
    {
        public override ElementConstraintViolations ErrorType => ElementConstraintViolations.MaxSize;
    }
}