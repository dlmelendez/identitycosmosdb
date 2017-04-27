// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUserLogin<TKey> where TKey : IEquatable<TKey>
    {
        public virtual string LoginProvider { get; set; }

        public virtual string ProviderKey { get; set; }

        public virtual string ProviderDisplayName { get; set; }

        public virtual TKey UserId { get; set; }
    }
}