// MIT License Copyright (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.CosmosDB.Tests.ModelTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IdentityRole = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityRole;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Tests
{
    [TestClass]
    public class UserOnlyStoreTests : BaseTest<ApplicationUser, IdentityRole, IdentityCloudContext>
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [TestCategory("UserOnlyStore")]
        public async Task UserOnlyStore_CreateUser_Success()
        {
            // Arrange
            var store = CreateUserOnlyStore();
            var user = new ApplicationUser()
            {
                Email = "test@example.com",
                UserName = "testuser",
                NormalizedUserName = "TESTUSER",
                NormalizedEmail = "TEST@EXAMPLE.COM"
            };

            // Act
            var result = await store.CreateAsync(user, TestContext.CancellationToken);

            // Assert
            Assert.IsTrue(result.Succeeded, string.Join(", ", result.Errors));
            Assert.IsFalse(string.IsNullOrEmpty(user.Id));

            // Cleanup
            await store.DeleteAsync(user, TestContext.CancellationToken);
            store.Dispose();
        }

        [TestMethod]
        [TestCategory("UserOnlyStore")]
        public async Task UserOnlyStore_FindByName_Success()
        {
            // Arrange
            var store = CreateUserOnlyStore();
            var user = new ApplicationUser()
            {
                Email = "test2@example.com",
                UserName = "testuser2",
                NormalizedUserName = "TESTUSER2",
                NormalizedEmail = "TEST2@EXAMPLE.COM"
            };

            await store.CreateAsync(user, TestContext.CancellationToken);

            // Act
            var foundUser = await store.FindByNameAsync(user.NormalizedUserName, TestContext.CancellationToken);

            // Assert
            Assert.IsNotNull(foundUser);
            Assert.AreEqual(user.UserName, foundUser.UserName);
            Assert.AreEqual(user.Email, foundUser.Email);

            // Cleanup
            await store.DeleteAsync(user, TestContext.CancellationToken);
            store.Dispose();
        }

        [TestMethod]
        [TestCategory("UserOnlyStore")]
        public async Task UserOnlyStore_AddClaim_Success()
        {
            // Arrange
            var store = CreateUserOnlyStore();
            var user = new ApplicationUser()
            {
                Email = "test3@example.com",
                UserName = "testuser3",
                NormalizedUserName = "TESTUSER3",
                NormalizedEmail = "TEST3@EXAMPLE.COM"
            };

            await store.CreateAsync(user, TestContext.CancellationToken);

            var claim = new System.Security.Claims.Claim("test_claim", "test_value");

            // Act
            await store.AddClaimsAsync(user, [claim], TestContext.CancellationToken);
            await store.UpdateAsync(user, TestContext.CancellationToken);

            var claims = await store.GetClaimsAsync(user, TestContext.CancellationToken);

            // Assert
            Assert.HasCount(1, claims);
            Assert.AreEqual("test_claim", claims[0].Type);
            Assert.AreEqual("test_value", claims[0].Value);

            // Cleanup
            await store.DeleteAsync(user, TestContext.CancellationToken);
            store.Dispose();
        }

        private UserOnlyStore<ApplicationUser, IdentityCloudContext> CreateUserOnlyStore()
        {
            return new UserOnlyStore<ApplicationUser, IdentityCloudContext>(GetContext());
        }
    }
}
