using System;
using System.IO;
using System.Collections.Generic;

namespace GoogleTestAdapter.VsPackage.Helpers
{
    public abstract class CsvReader<T>
    {

        private readonly string csvFile;
        private readonly char separator;
        private readonly int firstDataRow;

        public CsvReader(string csvFile, char separator, bool header)
        {
            if (!File.Exists(csvFile))
                throw new ArgumentException($"File does not exist: {csvFile}", nameof(csvFile));

            this.csvFile = csvFile;
            this.separator = separator;
            firstDataRow = header ? 1 : 0;
        }

        public List<T> GetObjects()
        {
            List<T> result = new List<T>();
            string[] lines = File.ReadAllLines(csvFile);
            for (int i = firstDataRow; i < lines.Length; i++)
            {
                string[] columns = lines[i].Split(separator);
                result.Add(GetObject(columns));
            }
            return result;
        }

        protected abstract T GetObject(string[] columns);

    }

}