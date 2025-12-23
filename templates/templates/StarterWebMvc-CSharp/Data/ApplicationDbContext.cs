using ElCamino.AspNetCore.Identity.CosmosDB;
using ElCamino.AspNetCore.Identity.CosmosDB.Model;

namespace samplemvccore.Data
{
    public class ApplicationDbContext : IdentityCloudContext
    {
        public ApplicationDbContext(IdentityConfiguration config) : base(config)
        {
        }
    }
}
