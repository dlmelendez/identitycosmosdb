using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using samplecore2.mvc.Data;
using samplecore2.mvc.Models;
using samplecore2.mvc.Services;
using ElCamino.AspNetCore.Identity.DocumentDB.Model;
using Microsoft.Azure.Documents.Client;
using IdentityRole = ElCamino.AspNetCore.Identity.DocumentDB.Model.IdentityRole;

namespace samplecore2.mvc
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
             .AddDocumentDBStores<ApplicationDbContext>(() =>
             {
                 return new IdentityConfiguration()
                 {
                     Uri = Configuration["IdentityDocumentDB:identityConfiguration:uri"],
                     AuthKey = Configuration["IdentityDocumentDB:identityConfiguration:authKey"],
                     Database = Configuration["IdentityDocumentDB:identityConfiguration:database"],
                     IdentityCollection = Configuration["IdentityDocumentDB:identityConfiguration:identityCollection"],
                     Policy = new ConnectionPolicy()
                     {
                         ConnectionMode = ConnectionMode.Gateway,
                         ConnectionProtocol = Protocol.Https
                     }
                 };
             }).AddDefaultTokenProviders();

            // Add application services.
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
               // app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
