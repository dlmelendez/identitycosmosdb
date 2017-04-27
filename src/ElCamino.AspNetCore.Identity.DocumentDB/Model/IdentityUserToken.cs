// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Azure.Documents;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUserToken<TKey> : Resource where TKey : IEquatable<TKey>
    {
        public IdentityUserToken()
        {
            Id = Guid.NewGuid().ToString();
        }

        [JsonProperty("userId")]
        public virtual TKey UserId { get; set; }

        [JsonProperty("loginProvider")]
        public virtual string LoginProvider { get; set; }

        [JsonProperty("name")]
        public virtual string Name { get; set; }

        [JsonProperty("value")]
        public virtual string Value { get; set; }
    }
}