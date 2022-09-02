// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUser : IdentityUser<string>
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
    public class IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin> : Microsoft.AspNetCore.Identity.IdentityUser<TKey>, IResource<TKey>
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

        [JsonProperty("userName")]
        public override string UserName { get; set; }

        [JsonProperty("normalizedUserName")]
        public override string NormalizedUserName { get; set; }

        [JsonProperty("email")]
        public override string Email { get; set; }

        [JsonProperty("normalizedEmail")]
        public override string NormalizedEmail { get; set; }

        [JsonProperty("emailConfirmed")]
        public override bool EmailConfirmed { get; set; }

        [JsonProperty("passwordHash")]
        public override string PasswordHash { get; set; }

        [JsonProperty("securityStamp")]
        public override string SecurityStamp { get; set; }

        [JsonProperty("concurrencyStamp")]
        public override string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("phoneNumber")]
        public override string PhoneNumber { get; set; }

        [JsonProperty("phoneNumberConfirmed")]
        public override bool PhoneNumberConfirmed { get; set; }

        [JsonProperty("twoFactorEnabled")]
        public override bool TwoFactorEnabled { get; set; }

        [JsonProperty("lockoutEnd")]
        public override DateTimeOffset? LockoutEnd { get; set; }

        [JsonProperty("lockoutEnabled")]
        public override bool LockoutEnabled { get; set; }

        [JsonProperty("accessFailedCount")]
        public override int AccessFailedCount { get; set; }





        [JsonProperty("roles")]
        public virtual IList<TUserRole> Roles { get; } = new List<TUserRole>();
        
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
