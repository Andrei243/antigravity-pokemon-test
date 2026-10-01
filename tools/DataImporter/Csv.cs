using System.Text;

namespace DataImporter;

/// <summary>One PokeAPI CSV file: a header row, then rows read by column name.</summary>
public sealed class CsvTable
{
    public IReadOnlyList<CsvRow> Rows { get; }

    private CsvTable(List<CsvRow> rows) => Rows = rows;

    public static CsvTable Load(string path)
    {
        var records = Parse(File.ReadAllText(path, Encoding.UTF8));
        if (records.Count == 0) throw new InvalidDataException($"{path} is empty");
        var columns = new Dictionary<string, int>();
        for (int i = 0; i < records[0].Count; i++) columns[records[0][i]] = i;
        return new CsvTable(records.Skip(1).Where(r => r.Count > 1 || r[0] != "").Select(r => new CsvRow(columns, r, path)).ToList());
    }

    /// <summary>RFC 4180: commas separate fields, quoted fields may hold commas, quotes ("") and line breaks.</summary>
    private static List<List<string>> Parse(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
                continue;
            }
            switch (c)
            {
                case '"': quoted = true; break;
                case ',': record.Add(field.ToString()); field.Clear(); break;
                case '\r': break;
                case '\n':
                    record.Add(field.ToString());
                    field.Clear();
                    records.Add(record);
                    record = new List<string>();
                    break;
                default: field.Append(c); break;
            }
        }
        if (field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }
        return records;
    }
}

public sealed class CsvRow
{
    private readonly Dictionary<string, int> columns;
    private readonly List<string> values;
    private readonly string path;

    public CsvRow(Dictionary<string, int> columns, List<string> values, string path)
    {
        this.columns = columns;
        this.values = values;
        this.path = path;
    }

    public string this[string column]
    {
        get
        {
            if (!columns.TryGetValue(column, out int i)) throw new KeyNotFoundException($"{Path.GetFileName(path)} has no column {column}");
            return i < values.Count ? values[i] : "";
        }
    }

    public int Int(string column) => int.Parse(this[column]);

    /// <summary>The column as a number, or null when it is empty.</summary>
    public int? IntOrNull(string column) => this[column] == "" ? null : int.Parse(this[column]);

    public bool Bool(string column) => this[column] is "1" or "True" or "true";
}
