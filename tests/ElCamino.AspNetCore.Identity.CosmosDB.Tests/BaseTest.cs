// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Cosmos;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Tests
{
    public partial class BaseTest<TUser, TRole, TContext> : IDisposable
        where TUser : Model.IdentityUser<string, Model.IdentityUserClaim<string>, Model.IdentityUserRole<string>, Model.IdentityUserLogin<string>>, new()
        where TRole : Model.IdentityRole<string>, new()
        where TContext : IdentityCloudContext
    {

#region IDisposable Support
        protected bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //  dispose managed state (managed objects).
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // set large fields to null.

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        protected static IServiceProvider Provider { get; private set; }
        protected static IServiceProvider RoleProvider { get; private set; }

        static BaseTest()
        {
            Provider = SetProvider(false);
            RoleProvider = SetProvider(true);
        }

        private static IServiceProvider GetProvider(bool includeRoles)
        {
            return includeRoles ? RoleProvider : Provider;
        }
            
        private static IServiceProvider SetProvider(bool includeRoles)
        {
            IdentityBuilder builder = new IdentityBuilder(typeof(TUser), typeof(TRole), new ServiceCollection());
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Add Identity services to the services container.
            static void options(IdentityOptions config)
            {
                config.Lockout = new LockoutOptions() { MaxFailedAccessAttempts = 2 };
                config.Password.RequireDigit = false;
                config.Password.RequiredLength = 3;
                config.Password.RequireLowercase = false;
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequireUppercase = false;
            }

            if (includeRoles)
            {
                builder.Services.AddIdentity<TUser, TRole>(options);
            }
            else
            {
                builder.Services.AddIdentityCore<TUser>(options);
            }
            
            builder.AddCosmosDBStores<TContext>(() => GetConfig())
                .CreateCosmosDBIfNotExists<TContext>()
                .AddDefaultTokenProviders();
            builder.Services.AddDataProtection();
            builder.Services.AddLogging();

            return builder.Services.BuildServiceProvider();
        }

        public static IdentityConfiguration GetConfig()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json", reloadOnChange:true, optional:false);

            var root = configuration.Build();

            var idconfig = new IdentityConfiguration()
            {
                Uri = root["IdentityCosmosDB:identityConfiguration:uri"],
                AuthKey = root["IdentityCosmosDB:identityConfiguration:authKey"],
                Database = root["IdentityCosmosDB:identityConfiguration:database"],
                IdentityCollection = root["IdentityCosmosDB:identityConfiguration:identityCollection"],
                Options = new CosmosClientOptions()
                {
                     ConnectionMode = ConnectionMode.Gateway,
                     ConsistencyLevel = ConsistencyLevel.Session,    
                     SerializerOptions = new CosmosSerializationOptions()
                }
            };

            idconfig.Options.SerializerOptions.PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase;
            
            return idconfig;
        }

        public IdentityCloudContext GetContext() => new IdentityCloudContext(GetConfig());

        public RoleStore<TRole, TContext> CreateRoleStore(bool includeRoles)
        {
            var temp = GetProvider(includeRoles).GetRequiredService<IRoleStore<TRole>>();
            return temp as RoleStore<TRole, TContext>;
        }

        //public RoleStore<TRole> CreateRoleStore(TContext context)
        //{
        //    return new RoleStore<TRole>(context);
        //}

        public RoleManager<TRole> CreateRoleManager(bool includeRoles)
        {
            return GetProvider(includeRoles).GetService<RoleManager<TRole>>();
        }

        //public RoleManager<TRole> CreateRoleManager(TContext context)
        //{
        //    return CreateRoleManager(new RoleStore<TRole>(context));
        //}

        //public RoleManager<TRole> CreateRoleManager(RoleStore<TRole> store)
        //{
        //    return Provider.GetRequiredService<RoleManager<TRole>>();
        //}


        public UserStore<TUser, TRole, TContext> CreateUserStore(bool includeRoles)
        {
            var temp = GetProvider(includeRoles).GetRequiredService<IUserStore<TUser>>();
            return temp as UserStore<TUser, TRole, TContext>;
        }

        //public UserStore<TUser> CreateUserStore(TContext context)
        //{
        //    return new UserStore<TUser>(context);
        //}

        public UserManager<TUser> CreateUserManager(bool includeRoles)
        {
            return GetProvider(includeRoles).GetRequiredService<UserManager<TUser>>();
        }

        //public UserManager<TUser> CreateUserManager(TContext context)
        //{
        //    return CreateUserManager(new UserStore<TUser>(context));
        //}

        public UserManager<TUser> CreateUserManager(IdentityOptions options)
        {
            //return new RoleManager<TRole>(store);
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // Add Identity services to the services container.
            services.AddIdentity<TUser, Model.IdentityRole>((config) =>
            {
                config.User.RequireUniqueEmail = options.User.RequireUniqueEmail;
            })
                            //.AddEntityFrameworkStores<ApplicationDbContext>()
            .AddCosmosDBStores<TContext>(() => GetConfig())
            .AddDefaultTokenProviders();
            services.AddLogging();

            return services.BuildServiceProvider().GetService(typeof(UserManager<TUser>)) as UserManager<TUser>;
        }

        public void AssertInnerExceptionType<A, I>(Action action) 
            where A : Exception 
            where I : Exception
        {
            var ex = Assert.Throws<A>(action).InnerException;
            Assert.IsTrue(ex is I, string.Format("Exception is not correct type: {0}", ex.GetType().Name));
        }

    }
}
