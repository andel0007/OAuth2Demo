// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer4.Services
{
    /// <summary>
    /// Default CORS policy service.
    /// </summary>
    public class CorsPolicyService : ICorsPolicyService
    {
        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILogger Logger;


        /// <summary>
        /// The context to get the origins
        /// </summary>
        protected readonly ConfigurationDbContext Context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultCorsPolicyService"/> class.
        /// </summary>
        public CorsPolicyService(ILogger<CorsPolicyService> logger , ConfigurationDbContext context)
        {
            Logger = logger;
            AllowedOrigins = new HashSet<string>();
            Context = context;
            InitOrigins();
        }

        /// <summary>
        /// The list allowed origins that are allowed.
        /// </summary>
        /// <value>
        /// The allowed origins.
        /// </value>
        public ICollection<string> AllowedOrigins { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all origins are allowed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if allow all; otherwise, <c>false</c>.
        /// </value>
        public bool AllowAll { get; set; }

        /// <summary>
        /// Determines whether the origin allowed.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <returns></returns>
        public virtual Task<bool> IsOriginAllowedAsync(string origin)
        {
            if (!String.IsNullOrWhiteSpace(origin))
            {
                if (AllowAll)
                {
                    Logger.LogDebug("AllowAll true, so origin: {0} is allowed", origin);
                    return Task.FromResult(true);
                }

                if (AllowedOrigins != null)
                {
                    if (AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                    {
                        Logger.LogDebug("AllowedOrigins configured and origin {0} is allowed", origin);
                        return Task.FromResult(true);
                    }
                    else
                    {
                        Logger.LogDebug("AllowedOrigins configured and origin {0} is not allowed", origin);
                    }
                }

                Logger.LogDebug("Exiting; origin {0} is not allowed", origin);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets The allowed cors origins of the enabled clents
        /// <summary>
        public void InitOrigins()
        {
            var query = from origin in Context.Set<ClientCorsOrigin>()
                        join client in Context.Set<Client>() on origin.ClientId equals client.Id
                        select origin.Origin;


            foreach (var allowedOrigin in query.ToList())
            {
                AllowedOrigins.Add(allowedOrigin);
            }
        }

    }
}
