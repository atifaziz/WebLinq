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
    using System.Collections.Generic;

    public static class LinkedServiceProvider
    {
        public static LinkedServiceProvider<T> Create<T>(Type serviceType, T service)
            where T : class =>
            Create(serviceType, service, null);

        public static LinkedServiceProvider<T> Create<T>(Type serviceType, T service, IServiceProvider previous)
            where T : class =>
            new LinkedServiceProvider<T>(serviceType, service, previous);

        public static LinkedServiceProvider<T> Create<T>(T service)
            where T : class =>
            Create(service, null);

        public static LinkedServiceProvider<T> Create<T>(T service, IServiceProvider previous)
            where T : class =>
            new LinkedServiceProvider<T>(service, previous);
    }

    public abstract class LinkedServiceProviderBase : IServiceProvider
    {
        Dictionary<Type, object> _cache;
        public IServiceProvider Link { get; }
        bool HasCache => _cache != null;

        protected LinkedServiceProviderBase(IServiceProvider link)
        {
            Link = link;
        }

        public Dictionary<Type, object> Cache =>
            _cache ?? (_cache = new Dictionary<Type, object>());

        public virtual object GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (Link == null)
                return null;
            object service;
            if (HasCache && Cache.TryGetValue(serviceType, out service))
                return service;
            service = Link.GetService(serviceType);
            Cache.Add(serviceType, service);
            return service;
        }
    }

    public sealed class LinkedServiceProvider<T> : LinkedServiceProviderBase
        where T : class
    {
        public Type ServiceType { get; }
        public T Service { get; }

        public LinkedServiceProvider(T service) :
            this(null, service) {}

        public LinkedServiceProvider(Type serviceType, T service) :
            this(serviceType, service, null) {}

        public LinkedServiceProvider(T service, IServiceProvider link) :
            this(null, service, link) {}

        public LinkedServiceProvider(Type serviceType, T service, IServiceProvider link) :
            base(link)
        {
            ServiceType = serviceType ?? typeof(T);
            Service = service;
        }

        public override object GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            return serviceType == ServiceType ? Service : base.GetService(serviceType);
        }
    }
}