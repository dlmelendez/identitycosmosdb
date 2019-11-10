// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using ElCamino.AspNetCore.Identity.CosmosDB;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class IdentityCosmosDBBuilderExtensions
	{

		public static IdentityBuilder AddCosmosDBStores<TContext>(this IdentityBuilder builder, Func<IdentityConfiguration> configAction)
			where TContext : IdentityCloudContext
		{
			builder.Services.AddSingleton<IdentityConfiguration>(new Func<IServiceProvider, IdentityConfiguration>(p=> configAction()));

            Type contextType = typeof(TContext);
            Type userStoreType = builder.RoleType != null ?
                typeof(UserStore<,,>).MakeGenericType(builder.UserType, builder.RoleType, contextType)
                : typeof(UserStore<,>).MakeGenericType(builder.UserType, contextType);

            builder.Services.AddScoped(contextType, contextType);

            builder.Services.AddScoped(
                typeof(IUserStore<>).MakeGenericType(builder.UserType),
                userStoreType);
            if (builder.RoleType != null)
            {
                Type roleStoreType = typeof(RoleStore<,>).MakeGenericType(builder.RoleType, contextType);

                builder.Services.AddScoped(
                typeof(IRoleStore<>).MakeGenericType(builder.RoleType),
                roleStoreType);
            }

            return builder;
		}

		
    }
}
