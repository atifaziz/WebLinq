namespace WebLinq.Samples
{
    #region Imports

    using System;
    using System.ComponentModel.Design;
    using static Query;
    using WebClient = WebClient;

    #endregion

    static class Program
    {
        public static void Main()
        {
            var q =
                from com in DownloadString(new Uri("http://www.example.com/"), (id, html) => new { Id = id, Size = html.Length })
                from net in DownloadString(new Uri("http://www.example.net/"), (id, html) => new { Id = id, Size = html.Length })
                where com.Size == net.Size
                select new { Com = com, Net = net };

            var services = new ServiceContainer();
            services.AddService(typeof(IWebClient), new WebClient());
            var context = new QueryContext(serviceProvider: services);

            Console.WriteLine(q.Invoke(context));
        }
    }
}
