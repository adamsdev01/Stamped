using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.Models
{
    public class JobPosting
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Company { get; set; } = "";
        public string Location { get; set; } = "";
        public string Description { get; set; } = "";
        public string Source { get; set; } = "folder";
        public string ExternalId { get; set; } = "";
        public string ExternalUrl { get; set; } = "";
    }
}
