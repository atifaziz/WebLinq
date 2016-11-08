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
    using System.Threading;
    using Collections;
    using Html;
    using Sys;

    public static class DefaultQueryContext
    {
        public static QueryContext Create() =>
            new QueryContext(
                serviceProvider: ServiceProvider.Create(
                    new HttpClient().Register,
                    new HapHtmlParser().Register,
                    new SysSpawnService().RegistrationHelper()));
    }

    #if DEBUG
    [System.Diagnostics.DebuggerDisplay("Id = {Id}")]
    #endif
    public class QueryContext : IServiceProvider
    {
        #if DEBUG
        static int _globalId;
        public int Id { get; }
        #endif

        public IServiceProvider ServiceProvider { get; }

        public QueryContext(IServiceProvider serviceProvider = null, Map<string, object> items = null)
        {
            #if DEBUG
            Id = Interlocked.Increment(ref _globalId);
            #endif

            ServiceProvider = serviceProvider;
            Items = items ?? Map<string, object>.Empty;
        }

        public Map<string, object> Items { get; }

        public QueryContext WithItems(Map<string, object> items) =>
            new QueryContext(ServiceProvider, items ?? Map<string, object>.Empty);

        public QueryContext WithServiceProvider(IServiceProvider serviceProvider) =>
            new QueryContext(serviceProvider, Items);

        object IServiceProvider.GetService(Type serviceType) =>
            FindService(serviceType);

        object FindService(Type serviceType) =>
            ServiceProvider?.GetService(serviceType);

        protected T FindService<T>() =>
            (T) FindService(typeof(T));

        public virtual T GetService<T>()
        {
            var service = FindService<T>();
            if (service == null)
            {
                var factory = FindService<Func<QueryContext, T>>();
                if (factory == null)
                    throw new Exception($"Service {typeof (T).FullName} is unavailable.");
                return factory(this);
            }
            return service;
        }
    }
}