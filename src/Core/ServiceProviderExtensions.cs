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

    public static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            return (T) serviceProvider.GetService(typeof(T));
        }

        public static T RequireService<T>(this IServiceProvider serviceProvider) =>
            (T) serviceProvider.RequireService(typeof(T));

        public static object RequireService(this IServiceProvider serviceProvider, Type serviceType)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            var service = serviceProvider.GetService(serviceType);
            if (service == null)
                throw new Exception($"Service {serviceType.FullName} is unavailable.");
            return service;
        }
    }
}