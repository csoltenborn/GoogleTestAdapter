using System;
using System.Collections.Generic;
using System.IO;

namespace GoogleTestAdapter.Tests.Common.Helpers
{
    public abstract class CsvReader<T>
    {
        private readonly string _csvFile;
        private readonly char _separator;
        private readonly int _firstDataRow;

        protected CsvReader(string csvFile, char separator, bool header)
        {
            if (!File.Exists(csvFile))
                throw new ArgumentException($"File does not exist: {csvFile}", nameof(csvFile));

            _csvFile = csvFile;
            _separator = separator;
            _firstDataRow = header ? 1 : 0;
        }

        protected abstract T GetObject(string[] columns);

        public List<T> GetObjects()
        {
            List<T> result = new List<T>();
            string[] lines = File.ReadAllLines(_csvFile);
            for (int i = _firstDataRow; i < lines.Length; i++)
            {
                string[] columns = lines[i].Split(_separator);
                result.Add(GetObject(columns));
            }
            return result;
        }

    }

}