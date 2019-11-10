// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUserLogin<TKey> : Microsoft.AspNetCore.Identity.IdentityUserLogin<TKey>
        where TKey : IEquatable<TKey>
    {
        public override string LoginProvider { get; set; }

        public override string ProviderKey { get; set; }

        public override string ProviderDisplayName { get; set; }

        public override TKey UserId { get; set; }
    }
}