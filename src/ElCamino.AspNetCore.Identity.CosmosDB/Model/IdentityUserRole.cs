// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUserRole : IdentityUserRole<string> { }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUserRole<TKey> : Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>
        where TKey : IEquatable<TKey>
    {
        [JsonProperty("userId")]
        public override TKey UserId { get; set; }

        [JsonProperty("roleId")]
        public override TKey RoleId { get; set; }
    }
}
