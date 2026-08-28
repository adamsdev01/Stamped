using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.Models
{
    public enum ApplicationStatus { 
        Draft, 
        PendingApproval, 
        Submitted 
    }

    public class JobApplication
    {
        public int Id { get; set; }
        public int JobMatchId { get; set; }
        public JobMatch JobMatch { get; set; } = null!;
        public string CoverLetter { get; set; } = "";
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
        public DateTime? SubmittedAt { get; set; }
    }
}
