using Stamped.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.Jobs
{
    public interface IJobSource
    {
        Task<List<JobPosting>> SearchAsync(string query, string location, CancellationToken ct = default);
    }
}
