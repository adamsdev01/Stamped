using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Stamped.Core.Resumes
{
    public interface IResumeParser
    {
        Task<Models.Resume> ParseAsync(Stream pdfStream, string fileName, CancellationToken ct = default);
    }
}
