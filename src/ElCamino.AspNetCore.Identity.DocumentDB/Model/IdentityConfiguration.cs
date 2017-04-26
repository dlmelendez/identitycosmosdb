// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Model
{
    [JsonObject("identityConfiguration", NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityConfiguration
    {
        public string Uri { get; set; }

        public string AuthKey { get; set; }

        public string Database { get; set; }

        public string IdentityCollection { get; set; } = Constants.DocumentCollectionIds.DefaultIdentityCollection;


        [JsonIgnore]
        public ConnectionPolicy Policy { get; set; } = ConnectionPolicy.Default;

    }
}
