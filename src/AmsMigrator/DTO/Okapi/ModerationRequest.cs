using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AmsMigrator.DTO.Okapi
{
    public class ModerationRequest
    {
        public string Status { get; set; }
        public string Comment { get; set; }
    }
}
