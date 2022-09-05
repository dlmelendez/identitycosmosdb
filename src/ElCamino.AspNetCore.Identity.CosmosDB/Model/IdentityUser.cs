// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUser : IdentityUser<string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>>
    {
        public IdentityUser()
        {
            Id = Guid.NewGuid().ToString();
        }

        public IdentityUser(string userName) : this()
        {
            UserName = userName;
        }
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUser<TKey> : IdentityUser<TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>>
        where TKey : IEquatable<TKey>
    { }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin> : IdentityUser<TKey, TUserClaim, TUserLogin>
    where TKey : IEquatable<TKey>
    {
        public IdentityUser() { }

        public IdentityUser(string userName) : base(userName) { }

        [JsonProperty("roles")]
        public virtual IList<TUserRole> Roles { get; } = new List<TUserRole>();

    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUser<TKey, TUserClaim, TUserLogin> : Microsoft.AspNetCore.Identity.IdentityUser<TKey>, IResource<TKey>
        where TKey : IEquatable<TKey>
    {
        public IdentityUser() { }

        public IdentityUser(string userName) : this()
        {
            UserName = userName; 
        }

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

        public virtual void SetPartitionKey()
        {
            PartitionKey = PartitionKeyHelper.GetPartitionKeyFromId(Id?.ToString());
        }

        [JsonProperty(PropertyName = "_etag")]
        public virtual string ETag { get; set; }

        public virtual string PartitionKey { get; set; }

        [JsonProperty("concurrencyStamp")]
        public override string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        
        [JsonProperty("claims")]
        public virtual IList<TUserClaim> Claims { get; } = new List<TUserClaim>();

        [JsonProperty("logins")]
        public virtual IList<TUserLogin> Logins { get; } = new List<TUserLogin>();
        
        public override string ToString()
        {
            return UserName;
        }
    }
}
