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

        [JsonProperty(PropertyName = "id")]
        public override string Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }

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
    public class IdentityRole<TKey, TUserRole, TRoleClaim> : Resource
        where TKey : IEquatable<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
    {
        public IdentityRole() { }

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

        public IdentityRole(string roleName) : this()
        {
            Name = roleName;
        }


        [JsonProperty(PropertyName = "claims")]
        public virtual IList<TRoleClaim> Claims { get; } = new List<TRoleClaim>();


        [JsonProperty(PropertyName = "name")]
        public virtual string Name { get; set; }

        [JsonProperty(PropertyName = "normalizedName")]
        public virtual string NormalizedName { get; set; }

        [JsonProperty(PropertyName = "concurrencyStamp")]
        public virtual string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        public override string ToString()
        {
            return Name;
        }
    }
}