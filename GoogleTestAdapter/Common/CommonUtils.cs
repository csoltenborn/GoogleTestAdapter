using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleTestAdapter.Common
{
    public static class CommonUtils
    {
        public static string GetExtendedPath(string pathExtension)
        {
            string path = Environment.GetEnvironmentVariable("PATH");
            return string.IsNullOrEmpty(pathExtension) ? path : $"{pathExtension};{path}";
        }
    }
}
