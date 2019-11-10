// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Tests.ModelTests
{

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}
