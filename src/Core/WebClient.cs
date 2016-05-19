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

namespace WebLinq
{
    using System;

    public interface IWebClient
    {
        string DownloadString(Uri url);
    }

    public class WebClient : IWebClient
    {
        readonly QueryContext _context;

        public WebClient(QueryContext context)
        {
            _context = context;
        }

        public string DownloadString(Uri url) =>
            new System.Net.WebClient().DownloadString(url);
    }
}