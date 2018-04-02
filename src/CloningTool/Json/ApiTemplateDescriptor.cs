using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace CloningTool.Json
{
    public sealed class ApiTemplateDescriptor
    {
        public long Id { get; set; }
        public string VersionId { get; set; }
        public DateTime LastModified { get; set; }
        public JObject Properties { get; set; }
        public IReadOnlyCollection<ApiTemplateElementDescriptor> Elements { get; set; }
    }
}
