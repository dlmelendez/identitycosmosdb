using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using Microsoft.Azure.Cosmos;
using samplerazorpagescore.Data;
using IdentityUser = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityUser;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
