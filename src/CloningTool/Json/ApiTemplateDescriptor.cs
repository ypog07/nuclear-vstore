using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using NuClear.VStore.Descriptors.Templates;

namespace CloningTool.Json
{
    public sealed class ApiTemplateDescriptor : IGenericTemplateDescriptor<ITemplateElementDescriptor>
    {
        public long Id { get; set; }
        public string VersionId { get; set; }
        public DateTime LastModified { get; set; }
        public string Author { get; set; }
        public string AuthorLogin { get; set; }
        public string AuthorName { get; set; }
        public JObject Properties { get; set; }
        public IReadOnlyCollection<ITemplateElementDescriptor> Elements { get; set; }
    }
}
