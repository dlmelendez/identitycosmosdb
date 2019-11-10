// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ElCamino.AspNetCore.Identity.DocumentDB;
using Microsoft.AspNetCore.Identity;
using ElCamino.AspNetCore.Identity.DocumentDB.Model;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.Azure.Documents.Linq;
using System.Linq;
using System.Threading;
using ElCamino.AspNetCore.Identity.DocumentDB.Tests.ModelTests;
using System.Security.Claims;
using IdentityRole = ElCamino.AspNetCore.Identity.DocumentDB.Model.IdentityRole;
using IdentityUser = ElCamino.AspNetCore.Identity.DocumentDB.Model.IdentityUser;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Tests
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
        public void AddRemoveRoleClaim()
        {
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            Console.WriteLine($"RoleId: {roleNew}");
            var role = new IdentityRole(roleNew);
            var start = DateTime.UtcNow;
            var createTask = manager.CreateAsync(role);
            createTask.Wait();

            Console.WriteLine("CreateRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Claim c1 = GenRoleClaim();
            Claim c2 = GenRoleClaim();

            AddRoleClaimHelper(role, c1);
            AddRoleClaimHelper(role, c2);

            RemoveRoleClaimHelper(role, c1);

        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public void AddRoleClaim()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var start = DateTime.UtcNow;
            var createTask = manager.CreateAsync(role);
            createTask.Wait();

            Console.WriteLine("CreateRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            AddRoleClaimHelper(role, GenRoleClaim());

            role = manager.FindByIdAsync(role.Id).Result;
            WriteLineObject(role);
        }

        private void AddRoleClaimHelper(IdentityRole role, Claim claim)
        {
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            var userClaimTask = manager.AddClaimAsync(role, claim);

            userClaimTask.Wait();
            var claimsTask = manager.GetClaimsAsync(role);

            claimsTask.Wait();
            Assert.IsTrue(claimsTask.Result.ToList().Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");           
        }

        private void RemoveRoleClaimHelper(IdentityRole role, Claim claim)
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            var userClaimTask = manager.RemoveClaimAsync(role, claim);

            userClaimTask.Wait();
            var claimsTask = manager.GetClaimsAsync(role);

            claimsTask.Wait();
            Assert.IsFalse(claimsTask.Result.ToList().Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public void CreateRoleScratch()
        {
            Guid id = Guid.NewGuid();
            IdentityCloudContext context = GetContext();
            var doc = new { id = id.ToString(), SpiderMonkey = "Monkey baby" };
            var docTask = context.Client.CreateDocumentAsync(context.IdentityDocumentCollection.SelfLink,
                doc, context.RequestOptions);
            docTask.Wait();
            var docResult = docTask.Result;
            Console.WriteLine(docResult.Resource.ToString());

            var fo = context.FeedOptions;
            fo.MaxItemCount = 1;
            var docQrTask = context.Client.CreateDocumentQuery(context.IdentityDocumentCollection.DocumentsLink
                , fo)
                .Where(d => d.Id == doc.id)
                .Select(s => s)
                .ToList()
                .FirstOrDefault();
            Console.WriteLine(docQrTask.ToString());


        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public void CreateRole()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            var role = CreateRoleHelper(manager);
            WriteLineObject<IdentityRole>(role);

            AssertInnerExceptionType<AggregateException, ArgumentNullException>(() => store.CreateAsync(null).Wait());
              
        }

        private IdentityRole CreateRoleHelper(RoleManager<IdentityRole> manager)
        {
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var createTask = manager.CreateAsync(role);
            createTask.Wait();
            return role;
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public void ThrowIfDisposed()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = new RoleStore<IdentityRole, IdentityCloudContext>(new IdentityCloudContext(GetConfig()), new IdentityErrorDescriber());
            store.Dispose();

            AssertInnerExceptionType<AggregateException, ObjectDisposedException>(() => store.DeleteAsync(new IdentityRole()).Wait());
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public void UpdateRole()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());

            var role = new IdentityRole(roleNew);
            var createTask = manager.CreateAsync(role);
            createTask.Wait();

            role.Name = Guid.NewGuid() + role.Name;
            var updateTask = manager.UpdateAsync(role);
            updateTask.Wait();

            var findTask = manager.FindByIdAsync(role.Id);

            Assert.IsNotNull(findTask.Result, "Find Role Result is null");
            Assert.AreNotEqual<string>(roleNew, findTask.Result.Name, "Name not updated.");

            AssertInnerExceptionType<AggregateException, ArgumentNullException>(() => store.UpdateAsync(null).Wait());
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public void UpdateRole2()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("{0}_TestRole", Guid.NewGuid());

            var role = new IdentityRole(roleNew);
            var createTask = manager.CreateAsync(role);
            createTask.Wait();

            role.Name = role.Name + Guid.NewGuid();
            var updateTask = manager.UpdateAsync(role);
            updateTask.Wait();

            var findTask = manager.FindByIdAsync(role.Id);
            findTask.Wait();
            Assert.IsNotNull(findTask.Result, "Find Role Result is null");
            Assert.AreEqual<string>(role.Id, findTask.Result.Id, "RowKeys don't match.");
            Assert.AreNotEqual<string>(roleNew, findTask.Result.Name, "Name not updated.");
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public void DeleteRole()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            string roleNew = string.Format("TestRole_{0}", Guid.NewGuid());
            var role = new IdentityRole(roleNew);
            var createTask = manager.CreateAsync(role);
            createTask.Wait();

            role = manager.FindByIdAsync(role.Id).Result;
            var delTask = manager.DeleteAsync(role);
            delTask.Wait();

            var findTask = manager.FindByIdAsync(role.Id);
            findTask.Wait();
            Assert.IsNull(findTask.Result, "Role not deleted ");

            AssertInnerExceptionType<AggregateException, ArgumentNullException>(() => store.DeleteAsync(null).Wait());               
        }


        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public void FindRoleById()
        {
            RoleStore<IdentityRole, IdentityCloudContext> store = CreateRoleStore(true);
            RoleManager<IdentityRole> manager = CreateRoleManager(true);
            DateTime start = DateTime.UtcNow;
            var role = CreateRoleHelper(manager);
            var findTask = manager.FindByIdAsync(role.Id);
            findTask.Wait();
            Console.WriteLine("FindByIdAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);


            Assert.IsNotNull(findTask.Result, "Find Role Result is null");
            WriteLineObject<IdentityRole>(findTask.Result);
            Assert.AreEqual<string>(role.Id, findTask.Result.Id, "Role Ids don't match.");
        }

        [TestMethod]
        [TestCategory("RoleStore.Role")]
        public void FindRoleByName()
        {
            RoleManager<IdentityRole> manager = CreateRoleManager(true);

            var role = CreateRoleHelper(manager);
            DateTime start = DateTime.UtcNow;
            var findTask = manager.FindByNameAsync(role.Name);
            findTask.Wait();
            Console.WriteLine("FindByNameAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.IsNotNull(findTask.Result, "Find Role Result is null");
            Assert.AreEqual<string>(role.Name, findTask.Result.Name, "Role names don't match.");
        }

        private void WriteLineObject<t>(t obj) where t : class
        {
            Console.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine("{0}", strLine);
        }

    }
}
