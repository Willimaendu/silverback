﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Background;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class DependencyInjectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IDistributedLockManager"/> implementation and uses the specified DbContext to
        /// handle the distributed locks.
        /// </summary>
        public static IServiceCollection AddDbDistributedLockManager<TDbContext>(this IServiceCollection services)
            where TDbContext : DbContext
        {
            return services.AddSingleton<IDistributedLockManager, DbContextDistributedLockManager<TDbContext>>();
        }
    }
}