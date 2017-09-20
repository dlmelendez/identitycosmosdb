// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Model
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
        public override TKey Id { get; set; }

        [JsonProperty(PropertyName = "_rid")]
        public virtual string ResourceId { get; set; }

        [JsonProperty(PropertyName = "_self")]
        public virtual string SelfLink { get; set; }

        [JsonIgnore]
        public string AltLink { get; set; }

        [JsonConverter(typeof(UnixDateTimeConverter))]
        [JsonProperty(PropertyName = "_ts")]
        public virtual DateTime Timestamp { get; set; }

        [JsonProperty(PropertyName = "_etag")]
        public virtual string ETag { get; set; }

        public IdentityRole(string roleName) : this()
        {
            Name = roleName;
        }


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