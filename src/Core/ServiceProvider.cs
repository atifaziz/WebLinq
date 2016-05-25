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
    using Mannex.Collections.Generic;

    public static class ServiceProvider
    {
        public static readonly IServiceProvider Empty = new DelegatingServiceProvider(delegate { return null; });

        public static IServiceProvider Create(params Action<Action<Type, object>>[] registrationHandlers)
        {
            if (registrationHandlers == null)
                return Empty;
            var services = new Dictionary<Type, object>();
            foreach (var handler in registrationHandlers)
                handler(services.Add);
            return new DelegatingServiceProvider(services.Find);
        }
    }

    sealed class DelegatingServiceProvider : IServiceProvider
    {
        readonly Func<Type, object> _handler;

        public DelegatingServiceProvider(Func<Type, object> handler)
        {
            _handler = handler;
        }

        public object GetService(Type serviceType) => _handler(serviceType);
    }
}
