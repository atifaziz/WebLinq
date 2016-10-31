namespace WebLinq.Samples
{
    #region Imports

    using System;
    using System.IO;
    using static HttpQuery;
    using static Html.HtmlQuery;

    #endregion

    static class Program
    {
        public static void Main()
        {
            Sample1();
        }

        static void Sample1()
        {
            var q =
                from com in Http.UserAgent(@"Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko")
                    .Get(new Uri("http://www.example.com/"))
                    .Html()
                select new {com.Id, Html = com.Content.QuerySelector("p")?.OuterHtml}
                into com
                from net in Http.Get(new Uri("http://www.example.net/"))
                    .Html()
                from link in Links(net.Content)
                select new
                {
                    Com = com,
                    Net = new
                    {
                        net.Id,
                        Html = net.Content.QuerySelector("p")?.OuterHtml,
                        Link = link,
                    }
                }
                into e
                where e.Com.Html?.Length == e.Net.Html?.Length
                select e;

            q.Dump();
        }

        static void Dump<T>(this Query<T> query, TextWriter output = null)
        {
            output = output ?? Console.Out;
            foreach (var e in query.ToEnumerable(DefaultQueryContext.Create))
                output.WriteLine(e);
        }
    }
}
