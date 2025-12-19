// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Text.Json.Serialization;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    public class IdentityRoleClaim : IdentityRoleClaim<string> { }

    public class IdentityRoleClaim<TKey> : Microsoft.AspNetCore.Identity.IdentityRoleClaim<TKey>
        where TKey : IEquatable<TKey>
    {
        [JsonIgnore]
        public override int Id { get => base.Id; set => base.Id = value; }

        public override string ClaimType { get => base.ClaimType; set => base.ClaimType = value; }

        public override string ClaimValue { get => base.ClaimValue; set => base.ClaimValue = value; }

        public override TKey RoleId { get => base.RoleId; set => base.RoleId = value; }

    }
}
