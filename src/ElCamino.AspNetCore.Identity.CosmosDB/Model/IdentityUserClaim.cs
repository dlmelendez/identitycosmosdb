// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Security.Claims;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUserClaim<TKey> : Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey> 
        where TKey : IEquatable<TKey>
    {
        [JsonIgnore]
        public override int Id { get => base.Id; set => base.Id = value; }

        [JsonProperty(PropertyName = "userId")]
        public override TKey UserId { get; set; }

        [JsonProperty(PropertyName = "claimType")]
        public override string ClaimType { get; set; }

        [JsonProperty(PropertyName = "claimValue")]
        public override string ClaimValue { get; set; }

       
    }
}