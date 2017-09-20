using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using samplecore2.mvc.Models;
using ElCamino.AspNetCore.Identity.DocumentDB;
using ElCamino.AspNetCore.Identity.DocumentDB.Model;

namespace samplecore2.mvc.Data
{
    public class ApplicationDbContext : IdentityCloudContext
    {
        public ApplicationDbContext(IdentityConfiguration config) : base(config) { }
    }
}
