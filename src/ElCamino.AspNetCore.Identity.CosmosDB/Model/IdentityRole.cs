// MIT License Copyright (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
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

    public class IdentityRole<TKey> : IdentityRole<TKey, IdentityUserRole<TKey>, IdentityRoleClaim<TKey>>
        where TKey : IEquatable<TKey>
    {
    }

    public class IdentityRole<TKey, TUserRole, TRoleClaim> : Microsoft.AspNetCore.Identity.IdentityRole<TKey>, IResource<TKey>
        where TKey : IEquatable<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
    {
        public IdentityRole() { }

        [JsonPropertyName("id")]
        public override TKey Id
        {
            get => base.Id;
            set
            {
                base.Id = value;
                SetPartitionKey();
            }
        }

        [JsonPropertyName("_etag")]
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

        public virtual IList<TRoleClaim> Claims { get; } = [];

        public override string Name { get; set; }

        public override string NormalizedName { get; set; }

        public override string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        public override string ToString()
        {
            return Name;
        }
    }
}
