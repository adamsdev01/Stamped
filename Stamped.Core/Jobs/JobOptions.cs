using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.Jobs
{
    public class JobOptions
    {
        public string Source { get; set; } = "Folder";
        public AdzunaOptions Adzuna { get; set; } = new();
        public string DefaultLocation { get; set; } = "";
    }

    public class AdzunaOptions
    {
        public string AppId { get; set; } = "";
        public string AppKey { get; set; } = "";
        public string Country { get; set; } = "us";
    }
}
