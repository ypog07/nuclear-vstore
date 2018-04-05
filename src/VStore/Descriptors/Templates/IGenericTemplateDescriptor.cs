using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace NuClear.VStore.Descriptors.Templates
{
    public interface IGenericTemplateDescriptor<TElementDescriptor> : IDescriptor
        where TElementDescriptor: IElementDescriptor
    {
        string Author { get; set; }
        string AuthorLogin { get; set; }
        string AuthorName { get; set; }
        JObject Properties { get; set; }
        IReadOnlyCollection<TElementDescriptor> Elements { get; set; }
    }
}