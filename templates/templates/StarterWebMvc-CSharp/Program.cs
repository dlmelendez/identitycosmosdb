using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using samplemvccore.Data;
using IdentityUser = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityUser;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//ElCamino configuration
.AddCosmosDBStores<ApplicationDbContext>(() =>
{
    return new IdentityConfiguration()
    {
        Uri = builder.Configuration["IdentityCosmosDB:identityConfiguration:uri"],
        AuthKey = builder.Configuration["IdentityCosmosDB:identityConfiguration:authKey"],
        Database = builder.Configuration["IdentityCosmosDB:identityConfiguration:database"],
        IdentityCollection = builder.Configuration["IdentityCosmosDB:identityConfiguration:identityCollection"],
        Options = new CosmosClientOptions()
        {
            ConnectionMode = ConnectionMode.Gateway,
            ConsistencyLevel = ConsistencyLevel.Session,
            SerializerOptions = new CosmosSerializationOptions()
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            }
        }
    };
})
.CreateCosmosDBIfNotExists<ApplicationDbContext>(); //can remove after first run;
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
