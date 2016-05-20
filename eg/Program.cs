namespace WebLinq.Samples
{
    #region Imports

    using System;
    using System.ComponentModel.Design;
    using System.Linq;
    using static Query;
    using WebClient = WebClient;

    #endregion

    static class Program
    {
        public static void Main()
        {
            var q =
                from com in Http.UserAgent(@"Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko")
                                .Get(new Uri("http://www.example.com/"),
                                     (id, rsp) => new { Id = id, Response = rsp })
                from html in Html(com.Response)
                select new { com.Id, Html = html.OuterHtml("p") } into com
                from net in Http.Get(new Uri("http://www.example.net/"),
                                     (id, rsp) => new { Id = id, Response = rsp })
                from links in Links(net.Response, delegate { return 1; })
                from html in Html(net.Response)
                select new
                {
                    Com = com,
                    Net = new
                    {
                        net.Id,
                        Html = html.OuterHtml("p"),
                        LinkCount = links.Count(),
                    }
                }
                into e
                where e.Com.Html.Length == e.Net.Html.Length
                select e;

            var services = new ServiceContainer();
            services.AddServiceFactory<IWebClient>(ctx => new WebClient(ctx));
            services.AddServiceFactory<IHtmlParser>(ctx => new HtmlParser(ctx));
            var context = new QueryContext(serviceProvider: services);

            Console.WriteLine(q.Invoke(context));
        }

        static void AddServiceFactory<T>(this IServiceContainer sc, Func<QueryContext, T> factory) =>
            sc.AddService(factory);

        static void AddService<T>(this IServiceContainer sc, T service) =>
            sc.AddService(typeof(T), service);
    }
}
