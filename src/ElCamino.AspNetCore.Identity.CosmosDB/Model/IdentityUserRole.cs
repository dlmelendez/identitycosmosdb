// MIT License Copyright (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    public class IdentityUserRole : IdentityUserRole<string> { }

    public class IdentityUserRole<TKey> : Microsoft.AspNetCore.Identity.IdentityUserRole<TKey>
        where TKey : IEquatable<TKey>
    {
        public override TKey UserId { get; set; }

        public override TKey RoleId { get; set; }
    }
}
