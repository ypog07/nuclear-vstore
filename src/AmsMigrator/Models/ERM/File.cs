using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmsMigrator.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class File
    {
        public File()
        {
            OrderFiles = new HashSet<OrderFile>();
        }

        public long Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public long CreatedBy { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        [Timestamp]
        public byte[] Timestamp { get; set; }
        public long? DgppId { get; set; }
        public byte[] Data { get; set; }


        public ICollection<OrderFile> OrderFiles { get; set; }
    }
}
