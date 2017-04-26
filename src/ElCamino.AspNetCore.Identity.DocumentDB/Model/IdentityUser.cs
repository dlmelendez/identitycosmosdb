// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Model
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
    public class IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin> : Resource  
        where TKey : IEquatable<TKey>
    {
        public IdentityUser() { }

        public IdentityUser(string userName) : this()
        {
            UserName = userName;
        }

        [JsonIgnore]
        private TKey _id;
        public new virtual TKey Id
        {
            get { return _id; }
            set
            {
                _id = value;
                if (_id == null)
                {
                    base.Id = null;
                }
                else
                {
                    base.Id = value.ToString();
                }
            }
        }

        [JsonProperty("userName")]
        public virtual string UserName { get; set; }

        [JsonProperty("normalizedUserName")]
        public virtual string NormalizedUserName { get; set; }

        [JsonProperty("email")]
        public virtual string Email { get; set; }

        [JsonProperty("normalizedEmail")]
        public virtual string NormalizedEmail { get; set; }

        [JsonProperty("emailConfirmed")]
        public virtual bool EmailConfirmed { get; set; }

        [JsonProperty("passwordHash")]
        public virtual string PasswordHash { get; set; }

        [JsonProperty("securityStamp")]
        public virtual string SecurityStamp { get; set; }

        [JsonProperty("concurrencyStamp")]
        public virtual string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("phoneNumber")]
        public virtual string PhoneNumber { get; set; }

        [JsonProperty("phoneNumberConfirmed")]
        public virtual bool PhoneNumberConfirmed { get; set; }

        [JsonProperty("twoFactorEnabled")]
        public virtual bool TwoFactorEnabled { get; set; }

        [JsonProperty("lockoutEnd")]
        public virtual DateTimeOffset? LockoutEnd { get; set; }

        [JsonProperty("lockoutEnabled")]
        public virtual bool LockoutEnabled { get; set; }

        [JsonProperty("accessFailedCount")]
        public virtual int AccessFailedCount { get; set; }

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
