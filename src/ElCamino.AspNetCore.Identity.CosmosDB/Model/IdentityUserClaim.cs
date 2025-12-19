// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Text.Json.Serialization;
namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    public class IdentityUserClaim : IdentityUserClaim<string> 
    {
        public IdentityUserClaim() { }
    }

    public class IdentityUserClaim<TKey> : Microsoft.AspNetCore.Identity.IdentityUserClaim<TKey> 
        where TKey : IEquatable<TKey>
    {
        [JsonIgnore]
        public override int Id { get => base.Id; set => base.Id = value; }

        public override TKey UserId { get; set; }

        public override string ClaimType { get; set; }

        public override string ClaimValue { get; set; }

       
    }
}
