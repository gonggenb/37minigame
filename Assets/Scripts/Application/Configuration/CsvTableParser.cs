using System;
using System.Collections.Generic;
using System.Text;

namespace WuxiaRoguelite.Application.Configuration
{
    public sealed class CsvFormatException : FormatException
    {
        public CsvFormatException(string message) : base(message)
        {
        }
    }

    public sealed class CsvRow
    {
        private readonly Dictionary<string, string> values;

        internal CsvRow(Dictionary<string, string> values)
        {
            this.values = values;
        }

        public string this[string column]
        {
            get
            {
                if (!values.TryGetValue(column, out string value))
                {
                    throw new KeyNotFoundException($"CSV 不存在列：{column}");
                }

                return value;
            }
        }

        public bool TryGet(string column, out string value)
        {
            return values.TryGetValue(column, out value);
        }
    }

    public sealed class CsvTable
    {
        internal CsvTable(List<string> headers, List<CsvRow> rows)
        {
            Headers = headers;
            Rows = rows;
        }

        public IReadOnlyList<string> Headers { get; }
        public IReadOnlyList<CsvRow> Rows { get; }
    }

    public sealed class CsvTableParser
    {
        public CsvTable Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new CsvFormatException("CSV 内容为空。");
            }

            List<List<string>> records = ReadRecords(text);
            while (records.Count > 0 && IsBlank(records[records.Count - 1]))
            {
                records.RemoveAt(records.Count - 1);
            }

            if (records.Count == 0)
            {
                throw new CsvFormatException("CSV 没有表头。");
            }

            List<string> headers = records[0];
            if (headers.Count > 0)
            {
                headers[0] = headers[0].TrimStart('\uFEFF');
            }

            var uniqueHeaders = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < headers.Count; i++)
            {
                headers[i] = headers[i].Trim();
                if (string.IsNullOrEmpty(headers[i]) || !uniqueHeaders.Add(headers[i]))
                {
                    throw new CsvFormatException($"CSV 表头为空或重复：第 {i + 1} 列。");
                }
            }

            var rows = new List<CsvRow>();
            for (int rowIndex = 1; rowIndex < records.Count; rowIndex++)
            {
                List<string> record = records[rowIndex];
                if (IsBlank(record))
                {
                    continue;
                }

                if (record.Count != headers.Count)
                {
                    throw new CsvFormatException(
                        $"CSV 第 {rowIndex + 1} 行列数为 {record.Count}，预期 {headers.Count}。");
                }

                var values = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int column = 0; column < headers.Count; column++)
                {
                    values.Add(headers[column], record[column].Trim());
                }

                rows.Add(new CsvRow(values));
            }

            return new CsvTable(headers, rows);
        }

        private static List<List<string>> ReadRecords(string text)
        {
            var records = new List<List<string>>();
            var record = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                if (quoted)
                {
                    if (character == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i += 1;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        field.Append(character);
                    }

                    continue;
                }

                if (character == '"' && field.Length == 0)
                {
                    quoted = true;
                }
                else if (character == ',')
                {
                    record.Add(field.ToString());
                    field.Clear();
                }
                else if (character == '\r' || character == '\n')
                {
                    if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i += 1;
                    }

                    record.Add(field.ToString());
                    field.Clear();
                    records.Add(record);
                    record = new List<string>();
                }
                else
                {
                    field.Append(character);
                }
            }

            if (quoted)
            {
                throw new CsvFormatException("CSV 存在未闭合的引号。");
            }

            if (field.Length > 0 || record.Count > 0)
            {
                record.Add(field.ToString());
                records.Add(record);
            }

            return records;
        }

        private static bool IsBlank(IReadOnlyList<string> record)
        {
            for (int i = 0; i < record.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(record[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
