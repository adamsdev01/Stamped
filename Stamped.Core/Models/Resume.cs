using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.Models
{
    public class Resume
    {
        public int Id { get; set; }
        public string FileName { get; set; } = "";
        public string RawText { get; set; } = "";
        public string MostRecentRole { get; set; } = "";
        public int YearsExperience { get; set; }
        public List<string> Skills { get; set; } = new();
        public DateTime ParsedAt { get; set; }
    }
}
