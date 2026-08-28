using Stamped.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stamped.Core.CoverLetters
{
    public interface ICoverLetterDrafter
    {
        Task<string> DraftAsync(Resume resume, JobPosting job, CancellationToken ct = default);
    }
}
