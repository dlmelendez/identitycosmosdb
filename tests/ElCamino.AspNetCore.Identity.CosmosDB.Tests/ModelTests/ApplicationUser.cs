// MIT License Copyright (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using ElCamino.AspNetCore.Identity.CosmosDB.Model;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Tests.ModelTests
{

    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser() : base() { }

        public string FirstName { get; set; }

        public string LastName { get; set; }
    }
}
