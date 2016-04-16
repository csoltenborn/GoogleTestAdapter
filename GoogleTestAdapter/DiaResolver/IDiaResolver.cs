using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.DiaResolver
{
    public interface IDiaResolver : IDisposable
    {
        IEnumerable<SourceFileLocation> GetFunctions(string symbolFilterString);
    }
}