// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Security.Claims;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityRoleClaim<TKey> : Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>
        where TKey : IEquatable<TKey>
    {
        [JsonIgnore]
        public override int Id { get => base.Id; set => base.Id = value; }

        [JsonProperty(PropertyName = "claimType")]
        public override string ClaimType { get => base.ClaimType; set => base.ClaimType = value; }

        [JsonProperty(PropertyName = "claimValue")]
        public override string ClaimValue { get => base.ClaimValue; set => base.ClaimValue = value; }

        [JsonProperty(PropertyName = "roleId")]
        public override TKey RoleId { get => base.RoleId; set => base.RoleId = value; }

    }
}