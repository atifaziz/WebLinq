namespace WebLinq.Samples
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reactive.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Xml.Linq;
    using Collections;
    using Html;
    using Modules;
    using Newtonsoft.Json;
    using Sys;
    using Text;
    using Xsv;
    using static Modules.HttpModule;
    using static Modules.SpawnModule;
    using static Modules.XmlModule;
    using static Modules.UriModule;

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
                    new { Title = nameof(GoogleSearch)          , Query = GoogleSearch()           , IsWindowsOnly = false },
                    new { Title = nameof(QueenSongs)            , Query = QueenSongs()             , IsWindowsOnly = false },
                    new { Title = nameof(ScheduledTasksViaSpawn), Query = ScheduledTasksViaSpawn() , IsWindowsOnly = true  },
                    new { Title = nameof(TopHackerNews)         , Query = TopHackerNews(100)       , IsWindowsOnly = false },
                    new { Title = nameof(MsdnBooksXmlSample)    , Query = MsdnBooksXmlSample()     , IsWindowsOnly = false },
                    new { Title = nameof(MockarooCsv)           , Query = MockarooCsv()            , IsWindowsOnly = false },
                    new { Title = nameof(TeapotError)           , Query = TeapotError()            , IsWindowsOnly = false },
                    new { Title = nameof(BasicAuth)             , Query = BasicAuth()              , IsWindowsOnly = false },
                    new { Title = nameof(AutoRedirection)       , Query = AutoRedirection()        , IsWindowsOnly = false },
                    new { Title = nameof(FormPost)              , Query = FormPost()               , IsWindowsOnly = false },
                    new { Title = nameof(ITunesMovies)          , Query = ITunesMovies("Bollywood"), IsWindowsOnly = false },
                    new { Title = nameof(NuGetSignedStatusForMostDownloadedPackages),
                                                                  Query = NuGetSignedStatusForMostDownloadedPackages(true),
                                                                                                     IsWindowsOnly = false },
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
                           .Submit(0, SubmissionData.Set("q", "foobar"))
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
                Title = r.QuerySelector(".r")?.NormalInnerText,
                Summary = r.QuerySelector(".st")?.NormalInnerText,
                Href = sr.TryBaseHref(r.QuerySelector(".r a")?.GetAttributeValue("href")),
            }
            into e
            where !string.IsNullOrWhiteSpace(e.Title)
                && e.Href != null
                && !string.IsNullOrWhiteSpace(e.Summary)
            select e;

        static IObservable<object> ScheduledTasksViaSpawn() =>

            from xml in Spawn("schtasks", ProgramArguments.Var("/query", "/xml", "ONE")).Delimited(Environment.NewLine)
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

            from dg in Http.Get(new Uri("https://en.wikipedia.org/wiki/Queen_discography"))
                           .Html()

            from album in Observable.For(

                from t in dg.Content.Tables()
                                    .Where(t => t.HasClass("wikitable"))
                                    .Take(1)
                from tr in t.TableRows((_, trs) => trs)
                let th = tr.FirstOrDefault(e => e?.AttributeValueEquals("scope", "row") == true)
                where th != null
                let a = th.QuerySelector("a[href]")
                select new
                {
                    Http  = dg.Client,
                    Title = a.GetAttributeValue("title")?.Trim(),
                    Href  = a.Owner.TryBaseHref(a.GetAttributeValue("href")?.Trim()),
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
                select e,

                album =>
                    from html in album.Http.Get(album.Url).Html().Content()
                    select new
                    {
                        album.Title,
                        Html = html,
                    })

            from tb in album.Html.Tables(".tracklist").Take(2)
            let trs = tb.QuerySelectorAll("tr")
            let hdrs =
                trs.FirstOrDefault(tr => tr.QuerySelectorAll("th").Take(4).Count() >= 3)
                  ?.QuerySelectorAll("th")
                   .Select(th => th.NormalInnerText)
                   .ToArray()
            where hdrs != null
            let idxs =
                Enumerable.ToArray(
                    from h in new[] { "Title", "Writer(s)", "Length" }
                    select Array.FindIndex(hdrs, he => he == h))
            let his = new
            {
                Title   = idxs[0],
                Writers = idxs[1],
                Length  = idxs[2],
            }
            from tr in trs
            let tds =
                tr.QuerySelectorAll("td")
                  .Select(td => td.NormalInnerText)
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

            from sp in Http.Get(new Uri("https://news.ycombinator.com/"))
                           .Html()
                           .Content()
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
                    Score = int.Parse(Regex.Match(s.Score, @"\b[0-9]+(?=\s+points)").Value),
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
                          .SubmitTo(new Uri("https://www.mockaroo.com/schemas/download"), "#schema_form",
                              SubmissionData.Collect(
                                  SubmissionData.Set("preview", "false"),
                                  SubmissionData.Set("schema[file_format]", "csv")))
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
            select new
            {
                e.StatusCode,
                e.ReasonPhrase
            };

        static IObservable<object> BasicAuth() =>

            from url in Observable.Return(new Uri("http://httpbin.org/basic-auth/user/passwd"))
            from fst in Http.Get(url)
                            .ReturnErroneousFetch()
            from snd in fst.Client.WithConfig(fst.Client.Config.WithCredentials(new NetworkCredential("user", "passwd")))
                           .Get(url)
            from result in new[]
            {
                new { fst.StatusCode, fst.ReasonPhrase },
                new { snd.StatusCode, snd.ReasonPhrase },
            }
            select result;

        static IObservable<object> AutoRedirection() =>

            from e in Http.Get(FormatUri($@"http://httpbin.org/redirect-to
                                                ? url = {"http://example.com/"}"))
            select e.RequestUrl;

        static IObservable<object> FormPost() =>

            Http.Get(new Uri("http://httpbin.org/forms/post"))
                .Submit(null,
                    SubmissionData.Collect(
                        SubmissionData.Set("custname" , "John Doe"           ),
                        SubmissionData.Set("custtel"  , "+99 99 9999 9999"   ),
                        SubmissionData.Set("custemail", "johndoe@example.com"),
                        SubmissionData.Set("size"     , "small"              ),
                        SubmissionData.Set("topping"  , Strings.Array("cheese", "mushroom", "onion")),
                        SubmissionData.Set("delivery" , "19:30"              )))
                .Text()
                .Content();

        static IObservable<HttpFetch<IEnumerable<(string Title, Uri Url)>>>
            GetITunesMovieGenres(this IHttpClient http) =>

            from toc in
                http.Get(new Uri("https://itunes.apple.com/us/genre/movies/id33"))
                    .Html()
            select
                toc.WithContent(
                    from a in toc.Content.QuerySelectorAll("#genre-nav ul > li a[href^=https]")
                    select (a.NormalInnerText,
                            new Uri(toc.Content.TryBaseHref(a.GetAttributeValue("href")))));

        static IObservable<object> ITunesMovies(string genreSought) =>

            from genres in
                Http.WithConfig(Http.Config.WithHeader("Accept-Language", "en-US")
                                           .WithUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36"))
                    .GetITunesMovieGenres()
            from genre in
                genres.Content
                      .Where(e => e.Title.IndexOf(genreSought, StringComparison.OrdinalIgnoreCase) >= 0)
                      .Take(1)
            from home in genres.Client.Get(genre.Url).Html()
            from page in
                Observable.For(
                    from a in home.Content.QuerySelectorAll("ul.list.alpha a[href^=https]")
                    select new
                    {
                        Title = a.NormalInnerText,
                        Url    = new Uri(home.Content.TryBaseHref(a.GetAttributeValue("href"))),
                    }
                    into e
                    where Regex.IsMatch(e.Title, @"^[A-Z*]$")
                    select e,
                    ap => // ap = alpha page
                        from html in home.Client.Get(ap.Url).Html()
                        from part in
                            Observable
                                .For(
                                    from a in html.Content.QuerySelectorAll("ul.list.paginate a[href^=https]")
                                    where !a.HasClass("paginate-more")
                                    select new
                                    {
                                        PageNumber = int.Parse(a.NormalInnerText, NumberStyles.None, CultureInfo.InvariantCulture),
                                        Url = new Uri(home.Content.TryBaseHref(a.GetAttributeValue("href"))),
                                    }
                                    into e
                                    // We always have page 1 (which we prepend later, below)
                                    // so take pages 2 and onward
                                    where e.PageNumber > 1
                                    select e,
                                    np => // np = numbered page
                                        from page in html.Client.Get(np.Url).Html().Content()
                                        select new
                                        {
                                            np.Url,
                                            np.PageNumber,
                                            Html = page
                                        }
                                        into e
                                        select e)
                                // We always have page 1 so start with it
                                .StartWith(new
                                {
                                    ap.Url,
                                    PageNumber = 1,
                                    Html = html.Content
                                })
                        select new
                        {
                            ap.Title,
                            part.PageNumber,
                            part.Html,
                            part.Url,
                        })
            from a in page.Html.QuerySelectorAll("#selectedcontent .column li a[href^=https]")
            select new
            {
                Title = a.NormalInnerText,
                Url = page.Html.TryBaseHref(a.GetAttributeValue("href")),
                Alpha = page.Title,
                page.PageNumber,
                SourceUrl = page.Url.AbsoluteUri,
            };

        //
        // The following sample was lifted from a scraping application that was
        // discussed in the following by blog entry by Phil Haack:
        //
        //   "Why NuGet Package Signing Is Not (Yet) for Me"
        //   https://haacked.com/archive/2019/04/03/nuget-package-signing/
        //
        // The original application can be found at:
        // https://github.com/Haacked/NugetSignChecker
        //
        // The specific version of the program was:
        // https://github.com/Haacked/NugetSignChecker/blob/2cf8edda3108a306a28fc573922b4890d48576e2/src/Program.cs
        //

        #region Copyright (c) 2019 Phil Haack
        //
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        //
        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.
        //
        #endregion

        static IObservable<object> NuGetSignedStatusForMostDownloadedPackages(bool communityPackagesOnly,
                                                                              int top = 100) =>
            from e in
                Observable.Merge(maxConcurrent: 4, sources:
                    from page in
                        Http.Get(new Uri("https://www.nuget.org/stats/packages"))
                            .Html()
                            .Content()

                    let prototype = new { versions = default(string[]) }

                    from id in
                        Enumerable.Take(count: top, source:
                            from tr in
                                page.QuerySelectorAll("table[data-bind]")
                                    .Single(e => e.GetAttributeValue("data-bind") == "visible: " + (communityPackagesOnly ? "!showAllPackageDownloads()" : "showAllPackageDownloads"))
                                    .QuerySelectorAll("tr")
                                    .Skip(1)
                            where tr.QuerySelector("td") != null
                            select tr.QuerySelectorAll("td")
                                     .Select(td => td.InnerText.Trim())
                                     .ElementAt(1)
                            into id
                            group id by id.Split('.')[0] into g
                            select g.First())

                    select
                        from json in
                            Http.Get(new Uri($"https://api.nuget.org/v3-flatcontainer/{id}/index.json"))
                                .Text()
                                .Content()
                        let version = JsonConvert.DeserializeAnonymousType(json, prototype)
                                                 .versions.LastOrDefault()
                        select new
                        {
                            Id            = id,
                            Version       = version,
                            NuPkgFileName = $"{id}.{version}.nupkg"
                        })

            let downloadPath = Path.Combine(Path.GetTempPath(), e.NuPkgFileName)

            from nupkg in
                File.Exists(downloadPath)
                ? Observable.Return(new LocalFileContent(downloadPath))
                : Http.Get(new Uri($"https://api.nuget.org/v3-flatcontainer/{e.Id}/{e.Version}/{e.NuPkgFileName}"))
                      .Download(downloadPath)
                      .Content()

            from signed in
                from ls in Spawn("nuget", ProgramArguments.Var("verify", "-Signatures", nupkg.Path)).ToArray()
                select string.Join(Environment.NewLine, ls) into output
                select Regex.Match(output, @"(?<=\bSignature +type *: *)(Repository|Author)\b",
                                   RegexOptions.IgnoreCase | RegexOptions.CultureInvariant).Value
            where signed.Length > 0
            select new
            {
                e.Id,
                e.Version,
                Signed = signed,
            };

        // End of "Copyright (c) 2019 Phil Haack"
    }
}
