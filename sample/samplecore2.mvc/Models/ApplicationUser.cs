using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using IdentityUser = ElCamino.AspNetCore.Identity.DocumentDB.Model.IdentityUser;

namespace samplecore2.mvc.Models
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
    }
}
