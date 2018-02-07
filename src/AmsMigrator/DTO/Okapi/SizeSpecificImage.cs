using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AmsMigrator.DTO.Okapi
{
    public class SizeSpecificImage
    {
        public SizeSpecificImageSize Size { get; set; }
        public string Raw { get; set; }
    }
}
