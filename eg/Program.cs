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
            GoogleSearch();
            QueenSongs();
            ScheduledTasksViaSpawn();
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
                        var next = curr.TryBaseHref(curr.QuerySelectorAll("#foot a.fl")
                                                        .Last() // Next
                                                        .GetAttributeValue("href"));
                        return Http.Get(new Uri(next)).Html().Content();
                    })
                    .TakeWhile(h => (TryParse.Int32(HttpUtility.ParseQueryString(h.BaseUrl.Query)["start"]) ?? 1) < 30)
                from r in sr.QuerySelectorAll(".g")
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
                from t in doc.Elements("Tasks").Elements(ns + "Task")
                from e in t.Elements(ns + "Actions").Elements(ns + "Exec")
                select new
                {
                    Name      = ((XComment)t.PreviousNode).Value.Trim(),
                    Command   = (string)e.Element(ns + "Command"),
                    Arguments = (string)e.Element(ns + "Arguments"),
                };

            q.Dump();
        }

        static void QueenSongs()
        {
            var q =

                from t in Http.Get(new Uri("https://en.wikipedia.org/wiki/Queen_discography")).Tables().Content()
                              .Where(t => t.HasClass("wikitable"))
                              .Take(1)
                from tr in t.TableRows((_, trs) => trs)
                select tr.FirstOrDefault(e => e?.AttributeValueEquals("scope", "row") == true) into th
                where th != null
                let a = th.QuerySelector("a[href]")
                select new
                {
                    Title = a.GetAttributeValue("title")?.Trim(),
                    Href = a.Owner.TryBaseHref(a.GetAttributeValue("href")?.Trim()),
                }
                into e
                select new
                {
                    e.Title,
                    Url = TryParse.Uri(e.Href, UriKind.Absolute),
                }
                into e
                where !string.IsNullOrEmpty(e.Title) && e.Url != null
                select e
                into album

                from html in Http.Get(album.Url).Html().Content()

                from tb in html.Tables(".tracklist").Take(2)
                let trs = tb.QuerySelectorAll("tr")
                let hdrs =
                    trs.FirstOrDefault(tr => tr.QuerySelectorAll("th").Take(4).Count() >= 3)
                        ?.QuerySelectorAll("th")
                        .Select(th => th.InnerTextSource.Decoded.Trim())
                        .ToArray()
                where hdrs != null
                let idxs =
                    new[] { "Title", "Writer(s)", "Length" }
                        .Select(h => Array.FindIndex(hdrs, he => he == h))
                        .ToArray()
                let his = new
                {
                    Title   = idxs[0],
                    Writers = idxs[1],
                    Length  = idxs[2],
                }
                from tr in trs
                let tds =
                    tr.QuerySelectorAll("td")
                      .Select(td => td.InnerTextSource.Decoded)
                      .ToArray()
                where tds.Length >= 3
                select new
                {
                    Album    = album.Title,
                    Title    = tds[his.Title],
                    Author   = his.Writers >= 0 ? tds[his.Writers] : null,
                    Duration = tds[his.Length],
                };

            q.Dump();
        }

        static void Dump<T>(this IQuery<T> query, TextWriter output = null)
        {
            output = output ?? Console.Out;
            foreach (var e in query.ToEnumerable(DefaultQueryContext.Create))
                output.WriteLine(e);
        }
    }
}
