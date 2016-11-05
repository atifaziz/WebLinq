namespace WebLinq.Samples
{
    #region Imports

    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Web;
    using System.Xml.Linq;
    using Text;
    using TryParsers;
    using Xml;
    using static HttpQuery;
    using static Sys.SysQuery;
    using static Html.HtmlQuery;
    using Html;

    #endregion

    static class Program
    {
        public static void Main()
        {
            HttpGetWithLinksAndHtmlParsing();
            GoogleSearch();            
            ScheduledTasksViaSpawn();
            QueenSongs();
        }

        static void HttpGetWithLinksAndHtmlParsing()
        {
            var q =
                from com in Http.UserAgent(@"Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko")
                                .Get(new Uri("http://www.example.com/"))
                                .Html()
                select new { com.Id, Html = com.Content.QuerySelector("p")?.OuterHtml }
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

        static void GoogleSearch()
        {
            var q =
                from sp in Http.Get(new Uri("https://google.com/"))
                               .Submit(0, new NameValueCollection { ["q"] = "foobar" })
                               .Html().Content()
                from sr in
                    Query.Generate(sp, curr =>
                    {
                        var next = curr.TryBaseHref(curr.QuerySelectorAll("a.fl")
                                                        .Single(a => "Next".Equals(a.InnerText.Trim(), StringComparison.InvariantCultureIgnoreCase))
                                                        .GetAttributeValue("href"));
                        return Http.Get(new Uri(next)).Html().Content();
                    })
                    .TakeWhile(h => (TryParse.Int32(HttpUtility.ParseQueryString(h.BaseUrl.Query)["start"]) ?? 1) < 30)
                from r in sr.QuerySelectorAll(".g").ToQuery()
                select new
                {
                    Title   = r.QuerySelector(".r")?.InnerText,
                    Summary = r.QuerySelector(".st")?.InnerText,
                    Href    = sr.TryBaseHref(r.QuerySelector(".r a")?.GetAttributeValue("href")),
                }
                into e
                where !string.IsNullOrWhiteSpace(e.Title)
                   && e.Href != null
                   && !string.IsNullOrWhiteSpace(e.Summary)
                select e;

            q.Dump();
        }

        static void ScheduledTasksViaSpawn()
        {
            var ns = XNamespace.Get("http://schemas.microsoft.com/windows/2004/02/mit/task");

            var q =
                from xml in Spawn("schtasks", @"/query /xml ONE").Delimited(Environment.NewLine)
                from doc in XmlQuery.Xml(new StringContent(xml))
                let execs =
                    from t in doc.Elements("Tasks").Elements(ns + "Task")
                    from e in t.Elements(ns + "Actions").Elements(ns + "Exec")
                    select new
                    {
                        Name      = ((XComment)t.PreviousNode).Value.Trim(),
                        Command   = (string)e.Element(ns + "Command"),
                        Arguments = (string)e.Element(ns + "Arguments"),
                    }
                from e in execs.ToQuery()
                select e;

            q.Dump();
        }

        static void QueenSongs()
        {
            var q =
                from html in Http.Get(new Uri("https://en.wikipedia.org/wiki/Queen_discography")).Html().Content()
                let albums =
                    from tr in html.Tables("table[border='1']").First().QuerySelectorAll("tr")
                    from th in tr.QuerySelectorAll("th[scope=row]")
                    let a = th.QuerySelector("a[href]")
                    where a != null
                    select new
                    {
                        title = a.GetAttributeValue("title")?.Trim(),
                        link = html.TryBaseHref(a.GetAttributeValue("href")?.Trim()),
                    }
                    into e
                    select new
                    {
                        e.title,
                        url = TryParse.Uri(e.link, UriKind.Absolute),
                    }
                    into e
                    where !string.IsNullOrEmpty(e.title) && e.url != null
                    select e

                from album in albums.ToQuery()
                select album

                into album

                from html in Http.Get(album.url).Html()
                from tb in html.Content.Tables(".tracklist").Take(2).ToQuery()
                from tr in tb.QuerySelectorAll("tr").ToQuery()
                where tr.QuerySelectorAll("td").Count() == (album.title == "Queen II" || album.title == "Innuendo (album)" ? 3 : 4)
                let titleTd = tr.QuerySelectorAll("td[style='text-align: left; vertical-align: top;']").Single()
                let authr = tr.QuerySelectorAll("td[style='vertical-align: top;']").SingleOrDefault()?.InnerText
                let durat = tr.QuerySelectorAll("td[style='padding-right: 10px; text-align: right; vertical-align: top;']").Last().InnerText
                let title = titleTd.HasChildElements ? titleTd.ChildElements.First().GetAttributeValue("title") : titleTd.InnerText
                where !string.IsNullOrEmpty(title)
                select new
                {
                    Album = album.title,
                    Title = title,
                    Author = authr,
                    Duration = durat
                };

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
