using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.DiaResolver
{
    public interface IDiaResolver : IDisposable
    {
        List<string> ErrorMessages { get; }
        IEnumerable<SourceFileLocation> GetFunctions(string symbolFilterString);
    }
}