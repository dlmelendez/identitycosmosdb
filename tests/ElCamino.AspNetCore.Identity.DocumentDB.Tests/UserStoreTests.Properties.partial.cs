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
    public partial class UserStoreTests : BaseTest<ApplicationUser, IdentityRole, IdentityCloudContext>
    {
        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void AccessFailedCount()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager(); var user = CreateTestUser<ApplicationUser>();
            var taskUser = manager.GetAccessFailedCountAsync(user);

            taskUser.Wait();
            Assert.AreEqual<int>(user.AccessFailedCount, taskUser.Result);

            var taskAccessFailed = manager.AccessFailedAsync(user);

            taskAccessFailed.Wait();

            Assert.IsTrue(taskAccessFailed.Result.Succeeded, string.Concat(taskAccessFailed.Result.Errors.Select(e => e.Code).ToArray()));

            var userTaskFindById = manager.FindByIdAsync(user.Id);
            userTaskFindById.Wait();
            user = userTaskFindById.Result;

            var taskAccessReset = manager.ResetAccessFailedCountAsync(user);

            taskAccessReset.Wait();
            Assert.IsTrue(taskAccessReset.Result.Succeeded, string.Concat(taskAccessReset.Result.Errors));

            Assert.ThrowsException<ArgumentNullException>(() => store.GetAccessFailedCountAsync(null).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.IncrementAccessFailedCountAsync(null).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.ResetAccessFailedCountAsync(null).Wait());
        }

        private void SetValidateEmail(UserManager<ApplicationUser> manager,
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store,
            ApplicationUser user,
            string strNewEmail)
        {
            string originalEmail = user.Email;
            var taskUserSet = manager.SetEmailAsync(user, strNewEmail);

            taskUserSet.Wait();
            Assert.IsTrue(taskUserSet.Result.Succeeded, string.Concat(taskUserSet.Result.Errors));

            var taskUser = manager.GetEmailAsync(user);

            taskUser.Wait();
            Assert.AreEqual<string>(strNewEmail, taskUser.Result);

            if (!string.IsNullOrWhiteSpace(strNewEmail))
            {
                var taskFind = manager.FindByEmailAsync(strNewEmail);
                taskFind.Wait();
                Assert.AreEqual<string>(strNewEmail, taskFind.Result.Email);
            }
            else
            {
                var noEmailUser = manager.FindByIdAsync(user.Id).Result;
                Assert.IsTrue(string.IsNullOrWhiteSpace(noEmailUser.Email));
            }
            //Should not find old by old email.
            if (!string.IsNullOrWhiteSpace(originalEmail))
            {
                var taskFind = manager.FindByEmailAsync(originalEmail);
                taskFind.Wait();
                Assert.IsNull(taskFind.Result);
            }

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void EmailNone()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager(); var user = CreateTestUser<ApplicationUser>(false, false);
            string strNewEmail = string.Format("{0}@hotmail.com", Guid.NewGuid().ToString("N"));
            SetValidateEmail(manager, store, user, strNewEmail);

            SetValidateEmail(manager, store, user, string.Empty);

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void Email()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager(); var user = CurrentUser;

            string strNewEmail = string.Format("{0}@gmail.com", Guid.NewGuid().ToString("N"));
            SetValidateEmail(manager, store, user, strNewEmail);

            Assert.ThrowsException<ArgumentNullException>(() => store.GetEmailAsync(null).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.SetEmailAsync(null, strNewEmail).Wait());
            store.SetEmailAsync(user, null).Wait();
            Assert.IsNull(user.Email);
        }


        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void EmailConfirmed()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();
            var user = CreateTestUser<ApplicationUser>();


            var taskUserSet = manager.GenerateEmailConfirmationTokenAsync(user);

            taskUserSet.Wait();
            Assert.IsFalse(string.IsNullOrWhiteSpace(taskUserSet.Result), "GenerateEmailConfirmationToken failed.");
            string token = taskUserSet.Result;


            var taskConfirm = manager.ConfirmEmailAsync(user, token);

            taskConfirm.Wait();
            Assert.IsTrue(taskConfirm.Result.Succeeded, string.Concat(taskConfirm.Result.Errors));


            var userTask02 = manager.FindByEmailAsync(user.Email);
            userTask02.Wait();
            user = userTask02.Result;

            var taskConfirmGet = store.GetEmailConfirmedAsync(user);
            taskConfirmGet.Wait();
            Assert.IsTrue(taskConfirmGet.Result, "Email not confirmed");

            Assert.ThrowsException<ArgumentNullException>(() => store.SetEmailConfirmedAsync(null, true).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.GetEmailConfirmedAsync(null).Wait());

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void LockoutEnabled()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();
            var user = CurrentUser;

            var taskLockoutSet = manager.SetLockoutEnabledAsync(user, true);

            taskLockoutSet.Wait();
            Assert.IsTrue(taskLockoutSet.Result.Succeeded, string.Concat(taskLockoutSet.Result.Errors));


            DateTimeOffset offSet = new DateTimeOffset(DateTime.Now.AddMinutes(3));
            var taskDateSet = manager.SetLockoutEndDateAsync(user, offSet);

            taskDateSet.Wait();
            Assert.IsTrue(taskDateSet.Result.Succeeded, string.Concat(taskDateSet.Result.Errors));


            var taskEnabledGet = manager.GetLockoutEnabledAsync(user);

            taskEnabledGet.Wait();
            Assert.IsTrue(taskEnabledGet.Result, "Lockout not true");


            var taskDateGet = manager.GetLockoutEndDateAsync(user);

            taskDateGet.Wait();
            Assert.AreEqual(offSet, taskDateGet.Result);

            DateTime tmpDate = DateTime.UtcNow.AddDays(1);
            user.LockoutEnd = tmpDate;
            var taskGet = store.GetLockoutEndDateAsync(user);
            taskGet.Wait();

            Assert.AreEqual<DateTimeOffset?>(new DateTimeOffset?(tmpDate), taskGet.Result);


            user.LockoutEnd = null;
            var taskGet2 = store.GetLockoutEndDateAsync(user);
            taskGet2.Wait();
            Assert.AreEqual<DateTimeOffset?>(new DateTimeOffset?(), taskGet2.Result);


            var minOffSet = DateTimeOffset.MinValue;
            var taskSet2 = store.SetLockoutEndDateAsync(user, minOffSet);
            taskSet2.Wait();
            Assert.IsNotNull(user.LockoutEnd);
            Assert.ThrowsException<ArgumentNullException>(() => store.GetLockoutEnabledAsync(null).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.GetLockoutEndDateAsync(null).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.SetLockoutEndDateAsync(null, offSet).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.SetLockoutEnabledAsync(null, false).Wait());
        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void PhoneNumber()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager(); var user = CurrentUser;

            string strNewPhoneNumber = "542-887-3434";

            var taskPhoneNumberSet = manager.SetPhoneNumberAsync(user, strNewPhoneNumber);

            taskPhoneNumberSet.Wait();
            Assert.IsTrue(taskPhoneNumberSet.Result.Succeeded, string.Concat(taskPhoneNumberSet.Result.Errors));


            var taskUser = manager.GetPhoneNumberAsync(user);
            taskUser.Wait();
            Assert.AreEqual<string>(strNewPhoneNumber, taskUser.Result);

            Assert.ThrowsException<ArgumentNullException>(() => store.GetPhoneNumberAsync(null).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.SetPhoneNumberAsync(null, strNewPhoneNumber).Wait());
            store.SetPhoneNumberAsync(user, null).Wait();
            Assert.IsNull(user.PhoneNumber);
        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void PhoneNumberConfirmed()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager(); var user = CreateTestUser<ApplicationUser>();
            string strNewPhoneNumber = "425-555-1111";

            var taskUserSet = manager.GenerateChangePhoneNumberTokenAsync(user, strNewPhoneNumber);

            taskUserSet.Wait();
            Assert.IsFalse(string.IsNullOrWhiteSpace(taskUserSet.Result), "GeneratePhoneConfirmationToken failed.");
            string token = taskUserSet.Result;


            var taskConfirm = manager.ChangePhoneNumberAsync(user, strNewPhoneNumber, token);

            taskConfirm.Wait();
            Assert.IsTrue(taskConfirm.Result.Succeeded, string.Concat(taskConfirm.Result.Errors));


            var uTask01 = manager.FindByEmailAsync(user.Email);
            uTask01.Wait();
            user = uTask01.Result;

            var taskConfirmGet = store.GetPhoneNumberConfirmedAsync(user);
            taskConfirmGet.Wait();
            Assert.IsTrue(taskConfirmGet.Result, "Phone not confirmed");

            Assert.ThrowsException<ArgumentNullException>(() => store.SetPhoneNumberConfirmedAsync(null, true).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.GetPhoneNumberConfirmedAsync(null).Wait());

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void TwoFactorEnabled()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();
            var user = CurrentUser;

            bool twoFactorEnabled = true;

            var taskTwoFactorEnabledSet = manager.SetTwoFactorEnabledAsync(user, twoFactorEnabled);

            taskTwoFactorEnabledSet.Wait();
            Assert.IsTrue(taskTwoFactorEnabledSet.Result.Succeeded, string.Concat(taskTwoFactorEnabledSet.Result.Errors));

            var taskUser = manager.GetTwoFactorEnabledAsync(user);

            taskUser.Wait();
            Assert.AreEqual<bool>(twoFactorEnabled, taskUser.Result);

            Assert.ThrowsException<ArgumentNullException>(() => store.GetTwoFactorEnabledAsync(null).Wait());
            Assert.ThrowsException<ArgumentNullException>(() => store.SetTwoFactorEnabledAsync(null, twoFactorEnabled).Wait());

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void PasswordHash()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager(); var user = CurrentUser;
            string passwordPlain = Guid.NewGuid().ToString("N");

            string passwordHash = new PasswordHasher<ApplicationUser>().HashPassword(user, passwordPlain);

            var taskUserSet = store.SetPasswordHashAsync(user, passwordHash);
            taskUserSet.Wait();

            var taskHasHash = manager.HasPasswordAsync(user);

            taskHasHash.Wait();
            Assert.IsTrue(taskHasHash.Result, "PasswordHash not set");

            var taskUser = store.GetPasswordHashAsync(user);
            taskUser.Wait();
            Assert.AreEqual<string>(passwordHash, taskUser.Result);
            user.PasswordHash = passwordHash;

            Assert.ThrowsException<ArgumentNullException>(() => store.GetPasswordHashAsync(null).Wait());

            Assert.ThrowsException<ArgumentNullException>(() => store.SetPasswordHashAsync(null, passwordHash).Wait());

            store.SetPasswordHashAsync(user, null).Wait();
            Assert.IsNull(user.PasswordHash);
        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void UsersProperty()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();
            DateTime start = DateTime.UtcNow;
            var list = manager.Users.ToList();

            WriteLineObject<IdentityUser>(list.First());

            Console.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Console.WriteLine("UserQuery: {0} users", list.Count());
            Console.WriteLine("");

            string email = "A" + Guid.NewGuid().ToString() + "@gmail.com";
            CreateTestUser<ApplicationUser>(true, true, email);

            DateTime start3 = DateTime.UtcNow;
            var list3 = manager.Users.Where(w=> w.Email != null).Select(s => s.Email).ToList();

            Console.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start3).TotalSeconds);
            Console.WriteLine("UserQuery.Email: {0} users", list3.Count());
            Console.WriteLine("");

            DateTime start4 = DateTime.UtcNow;
            var list4 = manager.Users.Select(s => s).ToList();
            WriteLineObject<IdentityUser>(list4.First());

            Console.WriteLine("UserQuery: {0} seconds", (DateTime.UtcNow - start4).TotalSeconds);
            Console.WriteLine("UserQuery: {0} users", list4.Count());
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
            Console.WriteLine("UserQuery: {0} users", list5.Count());
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


            Assert.ThrowsException<AggregateException>(() => manager.Users.Select(s => s.Email).FirstOrDefault());

            
            Assert.IsNotNull(store.Users);

        }

        [TestMethod]
        [TestCategory("UserStore.Properties")]
        public void SecurityStamp()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager(); var user = CreateTestUser<ApplicationUser>();

            var taskUser = manager.GetSecurityStampAsync(user);

            taskUser.Wait();
            Assert.AreEqual<string>(user.SecurityStamp, taskUser.Result);

            string strNewSecurityStamp = Guid.NewGuid().ToString("N");
            var taskUserSet = store.SetSecurityStampAsync(user, strNewSecurityStamp);
            taskUserSet.Wait();

            Assert.ThrowsException<ArgumentNullException>(() => store.GetSecurityStampAsync(null).Wait());

            Assert.ThrowsException<ArgumentNullException>(() => store.SetSecurityStampAsync(null, strNewSecurityStamp).Wait());

        }

    }
}
