#region Copyright (c) 2016 Atif Aziz. All rights reserved.

//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion

namespace WebLinq.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Xml.Linq;
    using Html;
    using Mannex.Collections.Generic;
    using Sys;
    using Xsv;
    using LoadOption = System.Xml.Linq.LoadOptions;

    public static class HttpModule
    {
        public static IHttpClient<HttpConfig> Http => new HttpClient(HttpConfig.Default);
    }

    public static class HtmlModule
    {
        public static IHtmlParser HtmlParser => Html.HtmlParser.Default;

        public static ParsedHtml ParseHtml(string html) =>
            ParseHtml(html, null);

        public static ParsedHtml ParseHtml(string html, Uri baseUrl) =>
            HtmlParser.Parse(html, baseUrl);
    }

    public static class XsvModule
    {
        public static IObservable<DataTable> CsvToDataTable(string text, params DataColumn[] columns) =>
            XsvToDataTable(text, ",", true, columns);

        public static IObservable<DataTable> XsvToDataTable(string text, string delimiter, bool quoted, params DataColumn[] columns) =>
            XsvQuery.XsvToDataTable(text, delimiter, quoted, columns);
    }

    public static class XmlModule
    {
        public static XDocument ParseXml(string xml) =>
            XDocument.Parse(xml, LoadOptions.None);
        public static XDocument ParseXml(string xml, LoadOption options) =>
            XDocument.Parse(xml, options);
    }

    public static class SpawnModule
    {
        public static IObservable<string> Spawn(string path, string args) =>
            Spawner.Default.Spawn(path, args, null);

        public static IObservable<string> Spawn(string path, string args, string workingDirectory) =>
            Spawner.Default.Spawn(path, args, workingDirectory, output => output, null);

        public static IObservable<KeyValuePair<T, string>> Spawn<T>(string path, string args, T stdoutKey, T stderrKey) =>
            Spawner.Default.Spawn(path, args, null, stdoutKey, stderrKey);

        public static IObservable<KeyValuePair<T, string>> Spawn<T>(string path, string args, string workingDirectory, T stdoutKey, T stderrKey) =>
            Spawner.Default.Spawn(path, args, workingDirectory,
                                  stdout => stdoutKey.AsKeyTo(stdout),
                                  stderr => stderrKey.AsKeyTo(stderr));

        public static IObservable<T> Spawn<T>(string path, string args, Func<string, T> stdoutSelector, Func<string, T> stderrSelector) =>
            Spawner.Default.Spawn(path, args, null, stdoutSelector, stderrSelector);
    }
}
