// MIT License Copyright (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;
using Microsoft.Azure.Cosmos;
using System.Text.Json.Serialization;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    public class IdentityConfiguration
    {
        public string Uri { get; set; }

        public string AuthKey { get; set; }

        public string Database { get; set; }

        public string IdentityCollection { get; set; } = Constants.ContainerIds.DefaultIdentityCollection;

        [JsonIgnore]
        public CosmosClientOptions Options { get; set; } = new CosmosClientOptions()
        {
            ConsistencyLevel = ConsistencyLevel.Session
        };

        public override string ToString()
        {
            return HashHelper.ConvertToHash(
                Uri ?? string.Empty + 
                AuthKey ?? string.Empty + 
                Database ?? string.Empty + 
                IdentityCollection ?? string.Empty); 
        }

    }
}
