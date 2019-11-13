# identitycosmosdb

This project is an open source high performance plugin to ASP.NET Core Identity framework using Azure CosmosDB using the native SQL API. 

[![NuGet Badge](https://buildstats.info/nuget/ElCamino.AspNetCore.Identity.DocumentDB)](https://www.nuget.org/packages/ElCamino.AspNetCore.Identity.DocumentDB/)
[![NuGet Badge](https://buildstats.info/nuget/ElCamino.AspNetCore.Identity.CosmosDB)](https://www.nuget.org/packages/ElCamino.AspNetCore.Identity.CosmosDB/)

Identity Core 3.x - Use ElCamino.AspNetCore.Identity.CosmosDB, sample Mvc app: https://github.com/dlmelendez/identitycosmosdb/tree/master/sample/samplecore3.mvc

Identity Core 2.x - Use ElCamino.AspNetCore.Identity.DocumentDB, sample Mvc app: https://github.com/dlmelendez/identitycosmosdb/tree/master/sample/samplecore3.mvc

Identity Core 1.x - Use ElCamino.AspNetCore.Identity.DocumentDB, sample Mvc app: https://github.com/dlmelendez/identitycosmosdb/tree/master/sample/samplecore.mvc

## Breaking changes to v3.0
### Naming changes from DocumentDB to CosmosDB
- Namespace change from **ElCamino.AspNetCore.Identity.DocumentDB** to **ElCamino.AspNetCore.Identity.CosmosDB**
- Any class, method or configuration key has been changed from **DocumentDB** to **CosmosDB**
### Data Migration
For existing data using ElCamino.AspNetCore.Identity.DocumentDB < v3.0, you should create a new container for the v3.0 configuration and then copy documents to the new container adding "/partitionKey" for the partition key. The partition key should be the last 4 characters of the id field.

Existing document:
```json
{
    "id": "abc5cf45-1781-4f56-a9f0-abc64ffbef0f",
    "_rid": "MBxvAOomeosCAAAAAAAAAA==",
    "_self": "dbs/MBxvAA==/colls/MBxvAOomeos=/docs/MBxvAOomeosCAAAAAAAAAA==/",
    "_etag": "\"00000000-0000-0000-919d-8fe1789d01d5\"",
    "userName": "263225f79db04d99be098bed7bee3e03",
    "normalizedUserName": "263225F79DB04D99BE098BED7BEE3E03"
}
```

New document with Partition Key:
```json
{
    "id": "abc5cf45-1781-4f56-a9f0-abc64ffbef0f",
    "_rid": "MBxvAOomeosCAAAAAAAAAA==",
    "_self": "dbs/MBxvAA==/colls/MBxvAOomeos=/docs/MBxvAOomeosCAAAAAAAAAA==/",
    "_etag": "\"00000000-0000-0000-919d-8fe1789d01d5\"",
    "userName": "263225f79db04d99be098bed7bee3e03",
    "normalizedUserName": "263225F79DB04D99BE098BED7BEE3E03",
    "partitionKey": "ef0f",
}
```

## Quick Start for Identity Core 3

Download the CosmosDB [emulator](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator) or setup an [instance in Azure](https://docs.microsoft.com/en-us/azure/cosmos-db/create-documentdb-dotnet).

Remove the NuGet packages EntityFramework and Microsoft.AspNetCore.Identity.EntityFramework packages using the Manage NuGet Packages.

##### Simplify your project references
```xml
 <ItemGroup>
    <PackageReference Include="ElCamino.AspNetCore.Identity.CosmosDB" Version="3.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.UI" Version="3.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="3.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="3.0.0" />
    <PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="3.0.0" />
  </ItemGroup> 
```

Remove all using statements referencing EntityFramework.
Delete the /Data/Migrations directory and all files in it.

##### Changes to [/areas/identity/**.cs](https://github.com/dlmelendez/identitycosmosdb/blob/master/sample/samplecore3.mvc/Areas/Identity/)

Remove: ~~using Microsoft.AspNetCore.Identity.EntityFramework;~~

Add:
```C#
using IdentityUser = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityUser;
```

##### Changes to [/*****/****/_ViewImports.cshtml](https://github.com/dlmelendez/identitycosmosdb/blob/master/sample/samplecore3.mvc/)

Add this using to override the IdentityUser class in the Microsoft.AspNetCore.Identity namespace.
```C#
@using IdentityUser = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityUser
```

##### Changes to [/Data/ApplicationDbContext.cs](https://github.com/dlmelendez/identitycosmosdb/blob/master/sample/samplecore3.mvc/Data/ApplicationDbContext.cs)
```C#
using ElCamino.AspNetCore.Identity.CosmosDB;
using ElCamino.AspNetCore.Identity.CosmosDB.Model;
...
    public class ApplicationDbContext : IdentityCloudContext
    {
        public ApplicationDbContext(IdentityConfiguration config) : base(config) { }
    }
```
##### Changes to [/Startup.cs](https://github.com/dlmelendez/identitycosmosdb/blob/master/sample/samplecore3.mvc/Startup.cs)
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
          //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(
            //        Configuration.GetConnectionString("DefaultConnection")));
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddCosmosDBStores<ApplicationDbContext>(() =>
                {
                    return new IdentityConfiguration()
                    {
                        Uri = Configuration["IdentityCosmosDB:identityConfiguration:uri"],
                        AuthKey = Configuration["IdentityCosmosDB:identityConfiguration:authKey"],
                        Database = Configuration["IdentityCosmosDB:identityConfiguration:database"],
                        IdentityCollection = Configuration["IdentityCosmosDB:identityConfiguration:identityCollection"],
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
                //.AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddRazorPages();
...
```
##### Changes to [/appsettings.json](https://github.com/dlmelendez/identitycosmosdb/blob/master/sample/samplecore3.mvc/appsettings.json)
Defaults for the local CosmosDB emulator. database and collection need to follow the CosmosDB naming rules, otherwise use whatever you like.
```json
{

"IdentityCosmosDB": {
    "identityConfiguration": {
      "uri": "https://localhost:8081",
      "authKey": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
      "database": "Id",
      "identityCollection": "users3"
    }
```
