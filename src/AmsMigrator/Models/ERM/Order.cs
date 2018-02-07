using System;
using System.Collections.Generic;

namespace AmsMigrator.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class Order
    {
        public Order()
        {
            OrderPositions = new HashSet<OrderPosition>();
            OrderFiles = new HashSet<OrderFile>();
        }

        public long Id { get; set; }
        public Guid ReplicationCode { get; set; }
        public long SourceOrganizationUnitId { get; set; }
        public long DestOrganizationUnitId { get; set; }
        public string Number { get; set; }
        public long FirmId { get; set; }
        public DateTime BeginDistributionDate { get; set; }
        public DateTime EndDistributionDatePlan { get; set; }
        public DateTime EndDistributionDateFact { get; set; }
        public int WorkflowStepId { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? RejectionDate { get; set; }
        public DateTime SignupDate { get; set; }
        public bool IsTerminated { get; set; }
        public long? DgppId { get; set; }
        public byte HasDocumentsDebt { get; set; }
        public string DocumentsComment { get; set; }
        public long? InspectorCode { get; set; }
        public string Comment { get; set; }
        public int TerminationReason { get; set; }
        public int OrderType { get; set; }
        public int PaymentMethod { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }

        public OrganizationUnit DestOrganizationUnit { get; set; }
        public OrganizationUnit SourceOrganizationUnit { get; set; }
        public ICollection<OrderPosition> OrderPositions { get; set; }
        public ICollection<OrderFile> OrderFiles { get; set; }
    }
}
