// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using ElCamino.AspNetCore.Identity.CosmosDB.Tests.ModelTests;
using System.Security.Claims;
using IdentityRole = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityRole;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Tests
{
    [TestClass]
    public class RoleStoreTests : BaseTest<ApplicationUser, IdentityRole, IdentityCloudContext>
    {
        private TestContext testContextInstance;
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        [TestInitialize]
        public void Initialize()
        {
        }


        private Claim GenRoleClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestUserClaim, Guid.NewGuid().ToString());
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task AddRemoveRoleClaim()
        {
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            Console.WriteLine($"RoleId: {roleNew}");
            var role = new IdentityRole(roleNew);
            var start = DateTime.UtcNow;
            _ = await manager.CreateAsync(role);

            Console.WriteLine("CreateRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Claim c1 = GenRoleClaim();
            Claim c2 = GenRoleClaim();

            await AddRoleClaimHelper(role, c1);
            await AddRoleClaimHelper(role, c2);

            await RemoveRoleClaimHelper(role, c1);

        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task AddRoleClaim()
        {
            _ = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var start = DateTime.UtcNow;
            _ = await manager.CreateAsync(role);

            Console.WriteLine("CreateRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            await AddRoleClaimHelper(role, GenRoleClaim());

            role = await manager.FindByIdAsync(role.Id);
            WriteLineObject(role);
        }

        private async Task AddRoleClaimHelper(IdentityRole role, Claim claim)
        {
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            var userClaimTask = await manager.AddClaimAsync(role, claim);
            var claimsTask = await manager.GetClaimsAsync(role);

            Assert.IsTrue(claimsTask.ToList().Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");           
        }

        private async Task RemoveRoleClaimHelper(IdentityRole role, Claim claim)
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            var userClaimTask = await manager.RemoveClaimAsync(role, claim);

            var claimsTask = await manager.GetClaimsAsync(role);

            Assert.IsFalse(claimsTask.ToList().Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task CreateRoleScratch()
        {
            Guid id = Guid.NewGuid();
            IdentityCloudContext context = GetContext();
            var doc = new IdentityRole() 
            { 
                Id = id.ToString(),
                Name = "SpiderMonkey"
            };
            var docResult = await context.IdentityContainer.CreateItemAsync(
                doc, new PartitionKey(doc.PartitionKey), context.RequestOptions);
            Console.WriteLine(docResult.Resource.ToString());

            var fo = context.QueryOptions;
            fo.MaxItemCount = 1;
            var docQrTask = context.IdentityContainer.GetItemLinqQueryable<IdentityRole>(true)
                .Where(d => d.Id == doc.Id)
                .ToList()
                .FirstOrDefault();
            Console.WriteLine(docQrTask.ToString());


        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task CreateRole()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            var role = await CreateRoleHelper(manager);
            WriteLineObject<IdentityRole>(role);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.CreateAsync(null));
              
        }

        private async Task<IdentityRole> CreateRoleHelper(RoleManager<IdentityRole> manager)
        {
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            _ = await manager.CreateAsync(role);
            return role;
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task ThrowIfDisposed()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = new RoleStore<IdentityRole, IdentityCloudContext>(new IdentityCloudContext(GetConfig()), new IdentityErrorDescriber());
            store.Dispose();

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async() => await store.DeleteAsync(new IdentityRole()));
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task UpdateRole()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());

            var role = new IdentityRole(roleNew);
            var createTask = await manager.CreateAsync(role);

            role.Name = Guid.NewGuid() + role.Name;
            var updateTask = await manager.UpdateAsync(role);

            var findTask = await manager.FindByIdAsync(role.Id);

            Assert.IsNotNull(findTask, "Find Role Result is null");
            Assert.AreNotEqual<string>(roleNew, findTask.Name, "Name not updated.");

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.UpdateAsync(null));
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task UpdateRole2()
        {
            _ = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("{0}_TestRole", Guid.NewGuid());

            var role = new IdentityRole(roleNew);
            var result = await manager.CreateAsync(role);
            Assert.AreEqual(IdentityResult.Success, result);

            role.Name = role.Name + Guid.NewGuid();
            result = await manager.UpdateAsync(role);
            Assert.AreEqual(IdentityResult.Success, result);

            var findTask = await manager.FindByIdAsync(role.Id);
            Assert.IsNotNull(findTask, "Find Role Result is null");
            Assert.AreEqual<string>(role.Id, findTask.Id, "RowKeys don't match.");
            Assert.AreNotEqual<string>(roleNew, findTask.Name, "Name not updated.");
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task DeleteRole()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var createTask = await manager.CreateAsync(role);

            role = await manager.FindByIdAsync(role.Id);
            var delTask = await manager.DeleteAsync(role);

            var findTask = await manager.FindByIdAsync(role.Id);
            Assert.IsNull(findTask, "Role not deleted ");

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.DeleteAsync(null));               
        }


        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task FindRoleById()
        {
            _ = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            DateTime start = DateTime.UtcNow;
            var role = await CreateRoleHelper(manager);
            var findTask = await manager.FindByIdAsync(role.Id);
            Console.WriteLine("FindByIdAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);


            Assert.IsNotNull(findTask, "Find Role Result is null");
            WriteLineObject<IdentityRole>(findTask);
            Assert.AreEqual<string>(role.Id, findTask.Id, "Role Ids don't match.");
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public async Task FindRoleByName()
        {
            RoleManager<IdentityRole> manager = CreateRoleManager(true);

            var role = await CreateRoleHelper(manager);
            DateTime start = DateTime.UtcNow;
            var findTask = await manager.FindByNameAsync(role.Name);
            Console.WriteLine("FindByNameAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.IsNotNull(findTask, "Find Role Result is null");
            Assert.AreEqual<string>(role.Name, findTask.Name, "Role names don't match.");
        }

        private void WriteLineObject<t>(t obj) where t : class
        {
            Console.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine("{0}", strLine);
        }

    }
}
