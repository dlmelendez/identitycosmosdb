// MIT License Copyright (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using ElCamino.AspNetCore.Identity.CosmosDB.Tests.ModelTests;
using IdentityRole = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityRole;
using IdentityUser = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityUser;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Tests
{
    [TestClass]
    public partial class UserStoreTests : BaseTest<ApplicationUser, IdentityRole, IdentityCloudContext>
    {
        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task AccessFailedCount(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles); 
            var user = await CreateTestUser<ApplicationUser>(includeRoles);
            var taskUser = await manager.GetAccessFailedCountAsync(user);
            Assert.AreEqual<int>(user.AccessFailedCount, taskUser);

            var taskAccessFailed = await manager.AccessFailedAsync(user);
            Assert.IsTrue(taskAccessFailed.Succeeded, string.Concat([.. taskAccessFailed.Errors.Select(e => e.Code)]));

            user = await manager.FindByIdAsync(user.Id);

            var taskAccessReset = await manager.ResetAccessFailedCountAsync(user);
            Assert.IsTrue(taskAccessReset.Succeeded, string.Concat(taskAccessReset.Errors));

            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.GetAccessFailedCountAsync(null, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.IncrementAccessFailedCountAsync(null, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.ResetAccessFailedCountAsync(null, TestContext.CancellationToken));
        }

        private static async Task SetValidateEmail(UserManager<ApplicationUser> manager,
            ApplicationUser user,
            string strNewEmail)
        {
            string originalEmail = user.Email;
            var taskUserSet = await manager.SetEmailAsync(user, strNewEmail);

            Assert.IsTrue(taskUserSet.Succeeded, string.Concat(taskUserSet.Errors));

            var taskUser = await manager.GetEmailAsync(user);
            Assert.AreEqual<string>(strNewEmail, taskUser);

            if (!string.IsNullOrWhiteSpace(strNewEmail))
            {
                var taskFind = await manager.FindByEmailAsync(strNewEmail);
                Assert.AreEqual<string>(strNewEmail, taskFind.Email);
            }
            else
            {
                var noEmailUser = await manager.FindByIdAsync(user.Id);
                Assert.IsTrue(string.IsNullOrWhiteSpace(noEmailUser.Email));
            }
            //Should not find old by old email.
            if (!string.IsNullOrWhiteSpace(originalEmail))
            {
                var taskFind = await manager.FindByEmailAsync(originalEmail);
                Assert.IsNull(taskFind);
            }

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task EmailNone(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles); 
            var user = await CreateTestUser<ApplicationUser>(false, false);
            string strNewEmail = string.Format("{0}@hotmail.com", Guid.NewGuid().ToString("N"));
            await UserStoreTests.SetValidateEmail(manager, user, strNewEmail);

            await UserStoreTests.SetValidateEmail(manager, user, string.Empty);

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task Email(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles); 
            var user = await CurrentUser(includeRoles);

            string strNewEmail = string.Format("{0}@gmail.com", Guid.NewGuid().ToString("N"));
            await UserStoreTests.SetValidateEmail(manager, user, strNewEmail);

            await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetEmailAsync(null, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetEmailAsync(null, strNewEmail, TestContext.CancellationToken));
            await store.SetEmailAsync(user, null, TestContext.CancellationToken);
            Assert.IsNull(user.Email);
        }


        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task EmailConfirmed(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);
            var user = await CreateTestUser<ApplicationUser>(includeRoles);


            string token = await manager.GenerateEmailConfirmationTokenAsync(user);
            Assert.IsFalse(string.IsNullOrWhiteSpace(token), "GenerateEmailConfirmationToken failed.");
            var taskConfirm = await manager.ConfirmEmailAsync(user, token);
            Assert.IsTrue(taskConfirm.Succeeded, string.Concat(taskConfirm.Errors));


            user = await manager.FindByEmailAsync(user.Email);

            var taskConfirmGet = await store.GetEmailConfirmedAsync(user, TestContext.CancellationToken);
            Assert.IsTrue(taskConfirmGet, "Email not confirmed");

            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.SetEmailConfirmedAsync(null, true, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetEmailConfirmedAsync(null, TestContext.CancellationToken));

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task LockoutEnabled(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);
            var user = await CurrentUser(includeRoles);

            var taskLockoutSet = await manager.SetLockoutEnabledAsync(user, true);
            Assert.IsTrue(taskLockoutSet.Succeeded, string.Concat(taskLockoutSet.Errors));


            DateTimeOffset offSet = new DateTimeOffset(DateTime.Now.AddMinutes(3));
            var taskDateSet = await manager.SetLockoutEndDateAsync(user, offSet);
            Assert.IsTrue(taskDateSet.Succeeded, string.Concat(taskDateSet.Errors));


            var taskEnabledGet = await manager.GetLockoutEnabledAsync(user);
            Assert.IsTrue(taskEnabledGet, "Lockout not true");


            var taskDateGet = await manager.GetLockoutEndDateAsync(user);
            Assert.AreEqual(offSet, taskDateGet);

            DateTime tmpDate = DateTime.UtcNow.AddDays(1);
            user.LockoutEnd = tmpDate;
            var taskGet = await store.GetLockoutEndDateAsync(user, TestContext.CancellationToken);
            Assert.AreEqual<DateTimeOffset?>(new DateTimeOffset?(tmpDate), taskGet);


            user.LockoutEnd = null;
            var taskGet2 = await store.GetLockoutEndDateAsync(user, TestContext.CancellationToken);
            Assert.AreEqual<DateTimeOffset?>(new DateTimeOffset?(), taskGet2);


            var minOffSet = DateTimeOffset.MinValue;
            await store.SetLockoutEndDateAsync(user, minOffSet, TestContext.CancellationToken);
            Assert.IsNotNull(user.LockoutEnd);
            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.GetLockoutEnabledAsync(null, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetLockoutEndDateAsync(null, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetLockoutEndDateAsync(null, offSet, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetLockoutEnabledAsync(null, false, TestContext.CancellationToken));
        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task PhoneNumber(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles); 
            var user = await CurrentUser(includeRoles);

            string strNewPhoneNumber = "542-887-3434";

            var taskPhoneNumberSet = await manager.SetPhoneNumberAsync(user, strNewPhoneNumber);
            Assert.IsTrue(taskPhoneNumberSet.Succeeded, string.Concat(taskPhoneNumberSet.Errors));


            var taskUser = await manager.GetPhoneNumberAsync(user);
            Assert.AreEqual<string>(strNewPhoneNumber, taskUser);

            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.GetPhoneNumberAsync(null, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.SetPhoneNumberAsync(null, strNewPhoneNumber, TestContext.CancellationToken));
            await store.SetPhoneNumberAsync(user, null, TestContext.CancellationToken);
            Assert.IsNull(user.PhoneNumber);
        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task PhoneNumberConfirmed(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles); 
            var user = await CreateTestUser<ApplicationUser>(includeRoles);
            string strNewPhoneNumber = "425-555-1111";

            string token = await manager.GenerateChangePhoneNumberTokenAsync(user, strNewPhoneNumber);

            Assert.IsFalse(string.IsNullOrWhiteSpace(token), "GeneratePhoneConfirmationToken failed.");


            var taskConfirm = await manager.ChangePhoneNumberAsync(user, strNewPhoneNumber, token);
            Assert.IsTrue(taskConfirm.Succeeded, string.Concat(taskConfirm.Errors));


            user = await manager.FindByEmailAsync(user.Email);

            var taskConfirmGet = await store.GetPhoneNumberConfirmedAsync(user, TestContext.CancellationToken);
            Assert.IsTrue(taskConfirmGet, "Phone not confirmed");

            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.SetPhoneNumberConfirmedAsync(null, true, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.GetPhoneNumberConfirmedAsync(null, TestContext.CancellationToken));

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task TwoFactorEnabled(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);
            var user = await CurrentUser(includeRoles);

            bool twoFactorEnabled = true;

            var taskTwoFactorEnabledSet = await manager.SetTwoFactorEnabledAsync(user, twoFactorEnabled);
            Assert.IsTrue(taskTwoFactorEnabledSet.Succeeded, string.Concat(taskTwoFactorEnabledSet.Errors));

            var taskUser = await manager.GetTwoFactorEnabledAsync(user);
            Assert.AreEqual<bool>(twoFactorEnabled, taskUser);

            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.GetTwoFactorEnabledAsync(null, TestContext.CancellationToken));
            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.SetTwoFactorEnabledAsync(null, twoFactorEnabled, TestContext.CancellationToken));

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task PasswordHash(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles); 
            var user = await CurrentUser(includeRoles);
            string passwordPlain = Guid.NewGuid().ToString("N");

            string passwordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, passwordPlain);

            await store.SetPasswordHashAsync(user, passwordHash, TestContext.CancellationToken);

            var taskHasHash = await manager.HasPasswordAsync(user);

            Assert.IsTrue(taskHasHash, "PasswordHash not set");

            var taskUser = await store.GetPasswordHashAsync(user, TestContext.CancellationToken);
            Assert.AreEqual<string>(passwordHash, taskUser);
            user.PasswordHash = passwordHash;

            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.GetPasswordHashAsync(null, TestContext.CancellationToken));

            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.SetPasswordHashAsync(null, passwordHash, TestContext.CancellationToken));

            await store.SetPasswordHashAsync(user, null, TestContext.CancellationToken);
            Assert.IsNull(user.PasswordHash);
        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task UsersProperty(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);
            DateTime start = DateTime.UtcNow;
            var list = manager.Users.ToList();

            WriteLineObject<IdentityUser>(list.First());

            Console.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Console.WriteLine("UserQuery: {0} users", list.Count);
            Console.WriteLine("");

            string email = "A" + Guid.NewGuid().ToString() + "@gmail.com";
            await CreateTestUser<ApplicationUser>(includeRoles, true, true, email);

            DateTime start3 = DateTime.UtcNow;
            var list3 = manager.Users.Where(w=> w.Email == email).Select(s => s.Email).ToList();

            Console.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start3).TotalSeconds);
            Console.WriteLine("UserQuery.Email: {0} users", list3.Count);
            Console.WriteLine("");
            Assert.HasCount(1, list3);

            DateTime start4 = DateTime.UtcNow;
            var list4 = manager.Users.Select(s => s).ToList();
            WriteLineObject<IdentityUser>(list4.First());

            Console.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start4).TotalSeconds);
            Console.WriteLine("UserQuery: {0} users", list4.Count);
            Console.WriteLine("");

            var type = store.Users.ElementType;
            System.Collections.IEnumerable enumFoo = store.Users as System.Collections.IEnumerable;
            var tempEnumerator = enumFoo.GetEnumerator();
            if (tempEnumerator.MoveNext())
            {
                var obj = tempEnumerator.Current as IdentityUser;
                Console.WriteLine("IEnumerable.GetEnumerator: First user");
                WriteLineObject<IdentityUser>(obj);
                Console.WriteLine("");
            }

            var query = manager.Users.Provider.CreateQuery(store.Users.Expression);
            tempEnumerator = query.GetEnumerator();
            if (tempEnumerator.MoveNext())
            {
                var obj = tempEnumerator.Current as IdentityUser;
                Console.WriteLine("UserQuery.CreateQuery(): First user");
                WriteLineObject<IdentityUser>(obj);
                Console.WriteLine("");
            }

            DateTime start5 = DateTime.UtcNow;
            var list5 = manager.Users.Where(t=> t.UserName != null).Take(10).ToList();

            WriteLineObject<IdentityUser>(list.First());

            Console.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start5).TotalSeconds);
            Console.WriteLine("UserQuery: {0} users", list5.Count);
            Console.WriteLine("");          

            DateTime start7 = DateTime.UtcNow;
            var list7 = manager.Users.Where(t => t.UserName != null).Count();

            Console.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start7).TotalSeconds);
            Console.WriteLine("UserQuery: {0} users", list7);
            Console.WriteLine("");

            DateTime start9 = DateTime.UtcNow;
            var list9 = manager.Users.Where(w => w.Email != null).ToList().Select(s => s.Email).FirstOrDefault();

            Console.WriteLine("UserQuery.Email.First(): {0} seconds", (DateTime.UtcNow - start9).TotalSeconds);
            Console.WriteLine("");


            Assert.IsNotNull(manager.Users.Select(s => s.Email).ToList().FirstOrDefault());

            
            Assert.IsNotNull(store.Users);

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task SecurityStamp(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles); 
            var user = await CreateTestUser<ApplicationUser>(includeRoles);

            var taskUser = await manager.GetSecurityStampAsync(user);

            Assert.AreEqual<string>(user.SecurityStamp, taskUser);

            string strNewSecurityStamp = Guid.NewGuid().ToString("N");
            await store.SetSecurityStampAsync(user, strNewSecurityStamp, TestContext.CancellationToken);

            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.GetSecurityStampAsync(null, TestContext.CancellationToken));

            await Assert.ThrowsAsync<ArgumentNullException>(async() => await store.SetSecurityStampAsync(null, strNewSecurityStamp, TestContext.CancellationToken));

        }

    }
}
