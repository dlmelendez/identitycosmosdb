// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using ElCamino.AspNetCore.Identity.DocumentDB;
using ElCamino.AspNetCore.Identity.DocumentDB.Model;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ElCamino.AspNetCore.Identity.DocumentDB.Tests.ModelTests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Documents.Client;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Tests
{
    public partial class BaseTest<TUser, TRole, TContext> : IDisposable
        where TUser : Model.IdentityUser, new()
        where TRole : Model.IdentityRole, new()
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

        protected static IdentityBuilder Builder;
        protected static IServiceProvider Provider;
        static BaseTest()
        {
            Builder = new IdentityBuilder(typeof(TUser), typeof(TRole), new ServiceCollection());
            Builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            // Add Identity services to the services container.
            Builder.Services.AddIdentity<TUser, TRole>(
            (config) =>
            {
                config.Lockout = new LockoutOptions() { MaxFailedAccessAttempts = 2 };
                config.Password.RequireDigit = false;
                config.Password.RequiredLength = 3;
                config.Password.RequireLowercase = false;
                config.Password.RequireNonAlphanumeric = false;
                config.Password.RequireUppercase = false;
            })
            .AddDocumentDBStores<TContext>(() => GetConfig())
            .AddDefaultTokenProviders();
            Builder.Services.AddLogging();

            Provider = Builder.Services.BuildServiceProvider();
        }

        public static IdentityConfiguration GetConfig()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("config.json", reloadOnChange:true, optional:false);

            var root = configuration.Build();

            var idconfig = new IdentityConfiguration()
            {
                Uri = root["IdentityDocumentDB:identityConfiguration:uri"],
                AuthKey = root["IdentityDocumentDB:identityConfiguration:authKey"],
                Database = root["IdentityDocumentDB:identityConfiguration:database"],
                IdentityCollection = root["IdentityDocumentDB:identityConfiguration:identityCollection"],
                Policy = new ConnectionPolicy()
                {
                     ConnectionMode = ConnectionMode.Gateway,
                     ConnectionProtocol = Protocol.Https
                }
            };

            return idconfig;
        }

        public IdentityCloudContext GetContext() => new IdentityCloudContext(GetConfig());

        public RoleStore<TRole, TContext> CreateRoleStore()
        {
            var temp = Provider.GetRequiredService<IRoleStore<TRole>>();
            return temp as RoleStore<TRole, TContext>;
        }

        //public RoleStore<TRole> CreateRoleStore(TContext context)
        //{
        //    return new RoleStore<TRole>(context);
        //}

        public RoleManager<TRole> CreateRoleManager()
        {
            return Provider.GetRequiredService<RoleManager<TRole>>();
        }

        //public RoleManager<TRole> CreateRoleManager(TContext context)
        //{
        //    return CreateRoleManager(new RoleStore<TRole>(context));
        //}

        //public RoleManager<TRole> CreateRoleManager(RoleStore<TRole> store)
        //{
        //    return Provider.GetRequiredService<RoleManager<TRole>>();
        //}


        public UserStore<TUser, TRole, TContext> CreateUserStore()
        {
            var temp = Provider.GetRequiredService<IUserStore<TUser>>();
            return temp as UserStore<TUser, TRole, TContext>;
        }

        //public UserStore<TUser> CreateUserStore(TContext context)
        //{
        //    return new UserStore<TUser>(context);
        //}

        public UserManager<TUser> CreateUserManager()
        {
            return Provider.GetRequiredService<UserManager<TUser>>();
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
            .AddDocumentDBStores<TContext>(() => GetConfig())
            .AddDefaultTokenProviders();
            services.AddLogging();

            return services.BuildServiceProvider().GetService(typeof(UserManager<TUser>)) as UserManager<TUser>;
        }

        public void AssertInnerExceptionType<A, I>(Action action) 
            where A : Exception 
            where I : Exception
        {
            var ex = Assert.ThrowsException<A>(action).InnerException;
            Assert.IsTrue(ex is I, string.Format("Exception is not correct type: {0}", ex.GetType().Name));
        }

    }
}
