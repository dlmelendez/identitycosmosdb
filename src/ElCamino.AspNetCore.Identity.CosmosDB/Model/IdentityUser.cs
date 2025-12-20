// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
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

    public class IdentityUser<TKey> : IdentityUser<TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>>
        where TKey : IEquatable<TKey>
    { }

    public class IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin> : IdentityUser<TKey, TUserClaim, TUserLogin>
    where TKey : IEquatable<TKey>
    {
        public IdentityUser() { }

        public IdentityUser(string userName) : base(userName) { }

        public virtual IList<TUserRole> Roles { get; } = [];

    }

    public class IdentityUser<TKey, TUserClaim, TUserLogin> : Microsoft.AspNetCore.Identity.IdentityUser<TKey>, IResource<TKey>
        where TKey : IEquatable<TKey>
    {
        public IdentityUser() { }

        public IdentityUser(string userName) : this()
        {
            UserName = userName; 
        }

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

        public virtual void SetPartitionKey()
        {
            PartitionKey = PartitionKeyHelper.GetPartitionKeyFromId(Id?.ToString());
        }

        [JsonPropertyName("_etag")]
        public virtual string ETag { get; set; }

        public virtual string PartitionKey { get; set; }

        public override string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();
        
        public virtual IList<TUserClaim> Claims { get; } = [];

        public virtual IList<TUserLogin> Logins { get; } = [];
        
        public override string ToString()
        {
            return UserName;
        }
    }
}
