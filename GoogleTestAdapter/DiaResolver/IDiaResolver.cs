using System;
using System.Collections.Generic;

namespace GoogleTestAdapter.DiaResolver
{
    public interface IDiaResolver : IDisposable
    {
        IList<SourceFileLocation> GetFunctions(string symbolFilterString);
    }
}