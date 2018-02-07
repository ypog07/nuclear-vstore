using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmsMigrator.Models
{
    public sealed class OrderFile
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long FileId { get; set; }
        public int FileKind { get; set; }
        public string Comment { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public long OwnerCode { get; set; }
        public long CreatedBy { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        [Timestamp]
        public byte[] Timestamp { get; set; }

        public File File { get; set; }
        public Order Order { get; set; }
    }
}
