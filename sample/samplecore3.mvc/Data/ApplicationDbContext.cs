using System;
using System.Collections.Generic;
using System.Text;
using ElCamino.AspNetCore.Identity.CosmosDB;
using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using Microsoft.EntityFrameworkCore;

namespace samplecore3.mvc.Data
{
    public class ApplicationDbContext : IdentityCloudContext
    {
        public ApplicationDbContext(IdentityConfiguration config) : base(config) { }
    }
}
