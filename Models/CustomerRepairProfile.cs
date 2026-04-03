using System;
using System.Collections.Generic;

namespace ESCenter.Models
{
    public class CustomerRepairProfile
    {
        public long? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public int TotalRepairs { get; set; }
        public int OpenRepairs { get; set; }
        public int ClosedRepairs { get; set; }
        public DateTime? FirstRepairDate { get; set; }
        public DateTime? LastRepairDate { get; set; }
        public List<RepairTicket> RepairHistory { get; set; } = new List<RepairTicket>();
    }
}
