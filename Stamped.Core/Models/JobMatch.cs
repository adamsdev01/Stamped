using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.Models
{
    public class JobMatch
    {
        public int Id { get; set; }
        public int ResumeId { get; set; }
        public Resume Resume { get; set; } = null!;
        public int JobPostingId { get; set; }
        public JobPosting JobPosting { get; set; } = null!;
        public int MatchScore { get; set; } // 0-100
        public string Reasoning { get; set; } = "";
    }
}
