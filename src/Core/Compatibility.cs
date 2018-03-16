namespace WebLinq
{
    using System.Data;
    using System.IO;
    using System.Linq;
    using Dsv;

    static class Compatibility
    {
        public static DataTable ParseXsvAsDataTable(this TextReader reader, string delimiter, bool quoted,
                                                    params DataColumn[] columns) =>
            LineReader
                .ReadLines(() => reader)
                .ParseDsv(new Format(delimiter[0]).WithQuote(quoted ? '"' : (char?) null))
                .ToDataTable(columns.Select(c => DataColumnSetup.Of(c.ColumnName, c.DataType))
                                    .Cast<IDataColumnBuilder>()
                                    .ToArray());
    }
}
