namespace WebLinq
{
    using System;
    using System.Data;
    using System.Linq;
    using Dsv;
    using Mannex;

    static class Compatibility
    {
        public static DataTable ParseXsvAsDataTable(this string xsv, string delimiter, bool quoted,
                                                    params DataColumn[] columns)
        {
            var table = new DataTable();
            var lines = xsv.SplitIntoLines();
            var format = new Format(delimiter[0]).WithQuote(quoted ? '"' : (char?) null);

            if (columns.Length > 0)
            {
                table.Columns.AddRange(columns);

                var rows =
                    from e in
                        lines.ParseDsv(format,
                                       hr => columns.Select(c => hr.FindFirstIndex(c.ColumnName, StringComparison.OrdinalIgnoreCase))
                                                    .ToArray())
                    select
                        from i in e.Header
                        select i is int n && n < e.Row.Count ? (object) e.Row[n] : DBNull.Value;

                foreach (var row in rows)
                    table.Rows.Add(row.ToArray());
            }
            else
            {
                foreach (var row in lines.ParseDsv(format))
                {
                    if (row.LineNumber == 1)
                    {
                        foreach (var e in row)
                            table.Columns.Add(new DataColumn(e));
                    }
                    else
                    {
                        var newRow = table.NewRow();
                        var count = Math.Min(row.Count, table.Columns.Count);
                        for (var i = 0; i < count; i++)
                            newRow[i] = row[i];
                        table.Rows.Add(newRow);
                    }
                }
            }

            table.AcceptChanges();
            return table;
        }
    }
}
