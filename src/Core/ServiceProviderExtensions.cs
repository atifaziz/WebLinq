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

        public static T RequireService<T>(this IServiceProvider serviceProvider)
        {
            var service = serviceProvider.GetService<T>();
            if (service == null)
                throw new Exception($"Service {typeof(T).FullName} is unavailable.");
            return service;
        }

        public static TResult Eval<T, TResult>(this IServiceProvider serviceProvider,
            Func<T, TResult> evaluator)
        {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            return evaluator(serviceProvider.RequireService<T>());
        }

        public static TResult Eval<T1, T2, TResult>(this IServiceProvider serviceProvider,
            Func<T1, T2, TResult> evaluator)
        {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            return serviceProvider.Eval((T1 s1) => serviceProvider.Eval((T2 s2) => evaluator(s1, s2)));
        }

        public static TResult Eval<T1, T2, T3, TResult>(this IServiceProvider serviceProvider,
            Func<T1, T2, T3, TResult> evaluator)
        {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            return serviceProvider.Eval((T1 s1, T2 s2) => serviceProvider.Eval((T3 s3) => evaluator(s1, s2, s3)));
        }

        public static TResult Eval<T1, T2, T3, T4, TResult>(this IServiceProvider serviceProvider,
            Func<T1, T2, T3, T4, TResult> evaluator)
        {
            if (evaluator == null) throw new ArgumentNullException(nameof(evaluator));
            return serviceProvider.Eval((T1 s1, T2 s2, T3 s3) => serviceProvider.Eval((T4 s4) => evaluator(s1, s2, s3, s4)));
        }

        public static IServiceProvider LinkService<T>(this IServiceProvider serviceProvider, T service)
            where T : class
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            return LinkedServiceProvider.Create(service, serviceProvider);
        }

        public static IServiceProvider LinkService<T>(this IServiceProvider serviceProvider, Type serviceType, T service)
            where T : class
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            return LinkedServiceProvider.Create(serviceType, service, serviceProvider);
        }
    }
}