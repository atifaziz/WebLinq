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

    public class QueryContext : IServiceProvider
    {
        public int Id { get; }
        public IServiceProvider ServiceProvider { get; }

        public QueryContext(int id = 1,
                            IServiceProvider serviceProvider = null)
        {
            Id = id;
            ServiceProvider = serviceProvider;
        }

        object IServiceProvider.GetService(Type serviceType) =>
            GetService(serviceType);

        object GetService(Type serviceType) =>
            ServiceProvider?.GetService(serviceType);

        public virtual T GetService<T>()
        {
            var service = (T) GetService(typeof(T));
            if (service == null)
                throw new Exception($"Service {typeof(T).FullName} is unavailable.");
            return service;
        }

        public TResult Eval<TService, TResult>(Func<TService, TResult> evaluator) =>
            evaluator(GetService<TService>());

        public TResult Eval<TService1, TService2, TResult>(Func<TService1, TService2, TResult> evaluator) =>
            evaluator(GetService<TService1>(),
                      GetService<TService2>());

        public TResult Eval<TService1, TService2, TService3, TResult>(Func<TService1, TService2, TService3, TResult> evaluator) =>
            evaluator(GetService<TService1>(),
                      GetService<TService2>(),
                      GetService<TService3>());

        public TResult Eval<TService1, TService2, TService3, TService4, TResult>(Func<TService1, TService2, TService3, TService4, TResult> evaluator) =>
            evaluator(GetService<TService1>(),
                      GetService<TService2>(),
                      GetService<TService3>(),
                      GetService<TService4>());
    }
}