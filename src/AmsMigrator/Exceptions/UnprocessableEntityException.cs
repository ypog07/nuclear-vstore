using System;
using System.Net.Http;
using System.Runtime.Serialization;

namespace AmsMigrator.Exceptions
{
    internal class UnprocessableEntityException : HttpRequestException
    {
        public string CustomImageHeader { get; set; }

        public string Content { get; set; }

        public UnprocessableEntityException()
        {
        }

        public UnprocessableEntityException(string message, string customHeader = null, string content = null) : base(message)
        {
            CustomImageHeader = customHeader;
            Content = content;
        }

        public UnprocessableEntityException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}