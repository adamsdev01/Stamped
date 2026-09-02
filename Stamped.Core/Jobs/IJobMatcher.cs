using Stamped.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.Jobs
{
    public interface IJobMatcher
    {
        Task<List<JobMatch>> MatchAsync(Resume resume, List<JobPosting> jobs, CancellationToken ct = default);
    }
}
