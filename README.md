# identitycosmosdb

This project is an open source high performance plugin to ASP.NET Core Identity framework using Azure CosmosDB. 

[![NuGet Badge](https://buildstats.info/nuget/ElCamino.AspNetCore.Identity.CosmosDB)](https://www.nuget.org/packages/ElCamino.AspNetCore.Identity.CosmosDB/)

Identity Core 3.x - Use ElCamino.AspNetCore.Identity.AzureTable, sample Mvc app: https://github.com/dlmelendez/identitycosmosdb/tree/master/sample/samplecore3.mvc

Identity Core 2.x - Use ElCamino.AspNetCore.Identity.AzureTable, sample Mvc app: https://github.com/dlmelendez/identitycosmosdb/tree/master/sample/samplecore2.mvc

Identity Core 1.x - Use ElCamino.AspNetCore.Identity.AzureTable, sample Mvc app: https://github.com/dlmelendez/identitycosmosdb/tree/master/sample/samplecore.mvc

## Quick Start for Identity Core 3

Download the CosmosDB [emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator) or setup an [instance in Azure](https://docs.microsoft.com/en-us/azure/cosmos-db/create-documentdb-dotnet).

Remove the NuGet packages EntityFramework and Microsoft.AspNetCore.Identity.EntityFramework packages using the Manage NuGet Packages.

##### Simplify your project references
```xml
 <ItemGroup>
    <PackageReference Include="ElCamino.AspNetCore.Identity.CosmosDB" Version="3.0.0" />
    <PackageReference Include="Microsoft.AspNetCore" Version="2.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.Cookies" Version="2.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc" Version="2.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.ViewCompilation" Version="2.0.0" PrivateAssets="All" />
    <PackageReference Include="Microsoft.AspNetCore.StaticFiles" Version="2.0.0" />
    <PackageReference Include="Microsoft.VisualStudio.Web.BrowserLink" Version="2.0.0" />
    <PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="2.0.0" PrivateAssets="All" />  </ItemGroup>

  <ItemGroup>
    <DotNetCliToolReference Include="Microsoft.Extensions.SecretManager.Tools" Version="2.0.0" />
    <DotNetCliToolReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Tools" Version="2.0.0" />
  </ItemGroup> 
```

Remove all using statements referencing EntityFramework.
Delete the /Data/Migrations directory and all files in it.

##### Changes to [/Models/ApplicationUser.cs](https://github.com/dlmelendez/identitycosmosdb/blob/master/sample/samplecore2.mvc/Models/ApplicationUser.cs)
```C#
using IdentityUser = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityUser;
...
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
    }
```
##### Changes to [/Data/ApplicationDbContext.cs](https://github.com/dlmelendez/identitycosmosdb/blob/master/sample/samplecore2.mvc/Data/ApplicationDbContext.cs)
```C#
using ElCamino.AspNetCore.Identity.CosmosDB;
using ElCamino.AspNetCore.Identity.CosmosDB.Model;
...
    public class ApplicationDbContext : IdentityCloudContext
    {
        public ApplicationDbContext(IdentityConfiguration config) : base(config) { }
    }
```
##### Changes to [/Startup.cs](https://github.com/dlmelendez/identitycosmosdb/blob/master/sample/samplecore2.mvc/Startup.cs)
```c#
using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using Microsoft.Azure.Documents.Client;
using IdentityRole = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityRole;
...
    public class Startup
    {
...
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
             .AddCosmosDBStores<ApplicationDbContext>(() =>
             {
                 return new IdentityConfiguration()
                 {
                     Uri = Configuration["IdentityCosmosDB:identityConfiguration:uri"],
                     AuthKey = Configuration["IdentityCosmosDB:identityConfiguration:authKey"],
                     Database = Configuration["IdentityCosmosDB:identityConfiguration:database"],
                     IdentityCollection = Configuration["IdentityCosmosDB:identityConfiguration:identityCollection"],
                     Policy = new ConnectionPolicy()
                     {
                         ConnectionMode = ConnectionMode.Gateway,
                         ConnectionProtocol = Protocol.Https
                     }
                 };
             }).AddDefaultTokenProviders();
...
```
##### Changes to [/appsettings.json](https://github.com/dlmelendez/identitycosmosdb/blob/master/sample/samplecore2.mvc/appsettings.json)
Defaults for the local CosmosDB emulator. database and collection need to follow the CosmosDB naming rules, otherwise use whatever you like.
```json
{
...
  "IdentityCosmosDB": {
    "identityConfiguration": {
      "uri": "https://localhost:8081",
      "authKey": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
      "database": "Id",
      "identityCollection": "users"
    }
  }
...
```
