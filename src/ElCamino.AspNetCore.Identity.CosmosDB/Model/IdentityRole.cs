// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityRole : IdentityRole<string>
    {

        public IdentityRole()
        {
            Id = Guid.NewGuid().ToString();
        }

        public IdentityRole(string roleName) : this()
        {
            Name = roleName;
        }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityRole<TKey> : IdentityRole<TKey, IdentityUserRole<TKey>, IdentityRoleClaim<TKey>>
        where TKey : IEquatable<TKey>
    {
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]   
    public class IdentityRole<TKey, TUserRole, TRoleClaim> : Microsoft.AspNetCore.Identity.IdentityRole<TKey>, IResource<TKey>
        where TKey : IEquatable<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
    {
        public IdentityRole() { }

        [JsonProperty(PropertyName = "id")]
        public override TKey Id
        {
            get => base.Id;
            set
            {
                base.Id = value;
                SetPartitionKey();
            }
        }

        [JsonProperty(PropertyName = "_etag")]
        public virtual string ETag { get; set; }

        public IdentityRole(string roleName) : this()
        {
            Name = roleName;
        }

        public virtual void SetPartitionKey()
        {
            PartitionKey = PartitionKeyHelper.GetPartitionKeyFromId(Id?.ToString());
        }

        public virtual string PartitionKey { get; set; }

        [JsonProperty(PropertyName = "claims")]
        public virtual IList<TRoleClaim> Claims { get; } = new List<TRoleClaim>();


        [JsonProperty(PropertyName = "name")]
        public override string Name { get; set; }

        [JsonProperty(PropertyName = "normalizedName")]
        public override string NormalizedName { get; set; }

        [JsonProperty(PropertyName = "concurrencyStamp")]
        public override string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        public override string ToString()
        {
            return Name;
        }
    }
}