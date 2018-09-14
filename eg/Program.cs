namespace WebLinq.Samples
{
    #region Imports

    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Reactive.Linq;
    using System.Web;
    using System.Xml.Linq;
    using Text;
    using Html;
    using Modules;
    using Xsv;
    using static Modules.HttpModule;
    using static Modules.SpawnModule;
    using static Modules.XmlModule;
    using System.Runtime.InteropServices;

    #endregion

    partial class Program
    {
        static void Wain(string[] args)
        {
            var ruler1 = new string('=', Console.BufferWidth - 1);
            var ruler2 = new string('-', Console.BufferWidth - 1);

            var samples =
                from s in new[]
                {
                    new { Title = nameof(GoogleSearch)          , Query = GoogleSearch()          , IsWindowsOnly = false },
                    new { Title = nameof(QueenSongs)            , Query = QueenSongs()            , IsWindowsOnly = false },
                    new { Title = nameof(ScheduledTasksViaSpawn), Query = ScheduledTasksViaSpawn(), IsWindowsOnly = true  },
                    new { Title = nameof(TopHackerNews)         , Query = TopHackerNews(100)      , IsWindowsOnly = false },
                    new { Title = nameof(MsdnBooksXmlSample)    , Query = MsdnBooksXmlSample()    , IsWindowsOnly = false },
                    new { Title = nameof(MockarooCsv)           , Query = MockarooCsv()           , IsWindowsOnly = false },
                    new { Title = nameof(TeapotError)           , Query = TeapotError()           , IsWindowsOnly = false },
                    new { Title = nameof(BasicAuth)             , Query = BasicAuth()             , IsWindowsOnly = false },
                    new { Title = nameof(AutoRedirection)       , Query = AutoRedirection()       , IsWindowsOnly = false },
                    new { Title = nameof(FormPost)              , Query = FormPost()              , IsWindowsOnly = false },
                }
                where args.Length == 0
                   || args.Any(a => s.Title.Equals(a, StringComparison.OrdinalIgnoreCase))
                where !s.IsWindowsOnly
                   || RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                select s;

            foreach (var sample in samples)
            {
                Console.WriteLine(ruler1);
                Console.WriteLine(sample.Title);
                Console.WriteLine(ruler2);
                foreach (var e in sample.Query.ToEnumerable())
                    Console.WriteLine(e);
            }
        }

        static IObservable<object> GoogleSearch() =>

            from sr in Http.Get(new Uri("http://google.com/"))
                           .Submit(0, new NameValueCollection { ["q"] = "foobar" })
                           .Html()
                           .Expand(curr =>
                           {
                               var next = curr.Content.TryBaseHref(curr.Content.QuerySelectorAll("#foot a.fl")
                                                                               .Last() // Next
                                                                               .GetAttributeValue("href"));
                               return curr.Client.Get(new Uri(next)).Html();
                           })
                           .TakeWhile(h => (int.TryParse(HttpUtility.ParseQueryString(h.Content.BaseUrl.Query)["start"], out var n) ? n : 1) < 30)
            select sr.Content into sr
            from r in sr.QuerySelectorAll(".g")
            select new
            {
                Title = r.QuerySelector(".r")?.InnerText,
                Summary = r.QuerySelector(".st")?.InnerText,
                Href = sr.TryBaseHref(r.QuerySelector(".r a")?.GetAttributeValue("href")),
            }
            into e
            where !string.IsNullOrWhiteSpace(e.Title)
                && e.Href != null
                && !string.IsNullOrWhiteSpace(e.Summary)
            select e;

        static IObservable<object> ScheduledTasksViaSpawn() =>

            from xml in Spawn("schtasks", "/query /xml ONE").Delimited(Environment.NewLine)
            let ns = XNamespace.Get("http://schemas.microsoft.com/windows/2004/02/mit/task")
            from t in ParseXml(xml).Elements("Tasks").Elements(ns + "Task")
            from e in t.Elements(ns + "Actions").Elements(ns + "Exec")
            select new
            {
                Name      = ((XComment)t.PreviousNode).Value.Trim(),
                Command   = (string)e.Element(ns + "Command"),
                Arguments = (string)e.Element(ns + "Arguments"),
            };

        static IObservable<object> QueenSongs() =>

            from t in Http.Get(new Uri("https://en.wikipedia.org/wiki/Queen_discography")).Tables().Content((http, t) => new { Http = http, Table = t })
                          .Where(t => t.Table.HasClass("wikitable"))
                          .Take(1)
            from tr in t.Table.TableRows((_, trs) => trs)
            let th = tr.FirstOrDefault(e => e?.AttributeValueEquals("scope", "row") == true)
            where th != null
            let a = th.QuerySelector("a[href]")
            select new
            {
                t.Http,
                Title = a.GetAttributeValue("title")?.Trim(),
                Href = a.Owner.TryBaseHref(a.GetAttributeValue("href")?.Trim()),
            }
            into e
            select new
            {
                e.Http,
                e.Title,
                Url = Uri.TryCreate(e.Href, UriKind.Absolute, out var url) ? url : null,
            }
            into e
            where !string.IsNullOrEmpty(e.Title) && e.Url != null
            select e
            into album

            from html in album.Http.Get(album.Url).Html().Content()

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
                Author   = his.Writers >= 0 ? tds[his.Writers].Trim() : null,
                Duration = tds[his.Length],
            };

        static IObservable<object> TopHackerNews(int score) =>

            from sp in Http.Get(new Uri("https://news.ycombinator.com/")).Html().Content()
            let scores =
                from s in sp.QuerySelectorAll(".score")
                select new
                {
                    Id = Regex.Match(s.GetAttributeValue("id"), @"(?<=^score_)[0-9]+$").Value,
                    Score = s.InnerText,
                }
            from e in
                from r in sp.QuerySelectorAll(".athing")
                select new
                {
                    Id = r.GetAttributeValue("id"),
                    Link = r.QuerySelector(".storylink")?.GetAttributeValue("href"),
                }
                into r
                join s in scores on r.Id equals s.Id
                select new
                {
                    r.Id,
                    Score = int.Parse(Regex.Match(s.Score, @"\b[0-9]+(?= +points)").Value),
                    r.Link,
                }
                into e
                where e.Score > score
                select e
            select e;

        static IObservable<object> MsdnBooksXmlSample() =>

            from html in
                Http.Get(new Uri("https://msdn.microsoft.com/en-us/library/ms762271.aspx"))
                    .Html()
                    .Content()
            select html.QuerySelector("#main pre code.lang-xml").InnerText.TrimStart()
            into xml
            from book in ParseXml(xml).Descendants("book")
            select new
            {
                Id            = (string)   book.Attribute("id"),
                Title         = (string)   book.Element("title"),
                Author        = (string)   book.Element("author"),
                Genre         = (string)   book.Element("genre"),
                Price         = (float)    book.Element("price"),
                PublishedDate = (DateTime) book.Element("publish_date"),
                Description   = ((string)  book.Element("description")).NormalizeWhitespace(),
            };

        static string NormalizeWhitespace(this string str) =>
            string.IsNullOrEmpty(str)
            ? str
            : string.Join(" ", str.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));

        static IObservable<object> MockarooCsv() =>

            from t in Http.Get(new Uri("https://www.mockaroo.com/"))
                          .SubmitTo(new Uri("https://www.mockaroo.com/schemas/download"), "#schema_form", new NameValueCollection
                          {
                              ["preview"]             = "false",
                              ["schema[file_format]"] = "csv",
                          })
                          .Accept("text/csv")
                          .CsvToDataTable(
                              new DataColumn("id", typeof(int)),
                              new DataColumn("first_name"),
                              new DataColumn("last_name"),
                              new DataColumn("email"),
                              new DataColumn("gender"),
                              new DataColumn("ip_address"))
                          .Content()

            from DataRow row in t.Rows
            select new
            {
                Id        = row["id"        ],
                FirstName = row["first_name"],
                LastName  = row["last_name" ],
                Email     = row["email"     ],
                Gender    = row["gender"    ],
                IpAddress = row["ip_address"],
            };

        static IObservable<object> TeapotError() =>

            from e in Http.Get(new Uri("http://httpbin.org/status/418"))
                          .ReturnErroneousFetch()
            select new { e.StatusCode, e.ReasonPhrase };

        static readonly Uri HttpbinBasicAuthUrl = new Uri("http://httpbin.org/basic-auth/user/passwd");

        static IObservable<object> BasicAuth() =>

            from fst in Http.Get(HttpbinBasicAuthUrl)
                            .ReturnErroneousFetch()
            from snd in fst.Client.WithConfig(fst.Client.Config.WithCredentials(new NetworkCredential("user", "passwd")))
                       .Get(HttpbinBasicAuthUrl)
            select new
            {
                First  = new { fst.StatusCode, fst.ReasonPhrase },
                Second = new { snd.StatusCode, snd.ReasonPhrase },
            };

        static IObservable<object> AutoRedirection() =>

            from e in Http.Get(new Uri("http://httpbin.org/redirect-to?url=" + Uri.EscapeDataString("http://example.com/")))
            select e.RequestUrl;

        static IObservable<object> FormPost() =>

            Http.Get(new Uri("http://httpbin.org/forms/post"))
                .Submit(null, new NameValueCollection
                {
                    { "custname" , "John Doe"            },
                    { "custtel"  , "+99 99 9999 9999"    },
                    { "custemail", "johndoe@example.com" },
                    { "size"     , "small"               },
                    { "topping"  , "cheese"              },
                    { "topping"  , "mushroom"            },
                    { "topping"  , "onion"               },
                    { "delivery" , "19:30"               },
                })
                .Text()
                .Content();
    }
}
