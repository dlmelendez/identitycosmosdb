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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;
using IdentityRole = ElCamino.AspNetCore.Identity.DocumentDB.Model.IdentityRole;
using IdentityUser = ElCamino.AspNetCore.Identity.DocumentDB.Model.IdentityUser;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Tests
{
    public partial class UserStoreTests : BaseTest<ApplicationUser, IdentityRole, IdentityCloudContext>
    {
        #region Static and Const Members
        public static object objectLock = new object();
        public static string DefaultUserPassword = "M" + Guid.NewGuid().ToString();


#endregion

        private ApplicationUser currentUser = null;
        private ApplicationUser currentEmailUser = null;

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

        public ApplicationUser CurrentUser
        {
            get
            {
                if(currentUser == null)
                {
                    lock (objectLock)
                    {
                        if (currentUser == null)
                        {
                            currentUser = CreateUser<ApplicationUser>();
                        }
                    }
                }
                return currentUser;
            }
        }
        public ApplicationUser CurrentEmailUser
        {
            get
            {
                if (currentEmailUser == null)
                {
                    lock (objectLock)
                    {
                        if (currentEmailUser == null)
                        {
                            currentEmailUser = CreateUser<ApplicationUser>();
                        }
                    }
                }

                return currentEmailUser;
            }
        }




        #region Test Initialization
        [TestInitialize]
        public void Initialize()
        {
            

        }
        #endregion

        private void WriteLineObject<t> (t obj)  where t : class
        {
            Console.WriteLine(typeof(t).Name);
            string strLine = obj == null ? "Null" : Newtonsoft.Json.JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine("{0}", strLine);
        }

        private static Claim GenAdminClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestAdminClaim, Guid.NewGuid().ToString());
        }

        private Claim GenAdminClaimEmptyValue()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestAdminClaim, string.Empty);
        }

        private Claim GenUserClaim()
        {
            return new Claim(Constants.AccountClaimTypes.AccountTestUserClaim, Guid.NewGuid().ToString());
        }
        private static UserLoginInfo GenGoogleLogin()
        {
            return new UserLoginInfo(Constants.LoginProviders.GoogleProvider.LoginProvider,
                        Constants.LoginProviders.GoogleProvider.ProviderKey, string.Empty);
        }

        private static ApplicationUser GenTestUser()
        {
            Guid id = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Email = id.ToString() + "@live.com",
                UserName = id.ToString("N"),
                LockoutEnabled = false,
                LockoutEnd = null,
                PhoneNumber = "555-555-5555",
                TwoFactorEnabled = false,
            };

            return user;
        }

        private ApplicationUser GetTestAppUser()
        {
            Guid id = Guid.NewGuid();
            ApplicationUser user = new ApplicationUser()
            {
                Email = id.ToString() + "@live.com",
                UserName = id.ToString("N"),
                LockoutEnabled = false,
                LockoutEnd = null,
                PhoneNumber = "555-555-5555",
                TwoFactorEnabled = false,
                FirstName = "Jim",
                LastName = "Bob"
            };
            return user;
        }

        

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void CheckDupUser()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = GenTestUser();
            var user2 = GenTestUser();
            var result1 = manager.CreateAsync(user).Result;
            Assert.IsTrue(result1.Succeeded, string.Concat(result1.Errors.Select(e => e.Code)));
            user2.UserName = user.UserName;
            var result2 = manager.CreateAsync(user2).Result;
            Assert.IsFalse(result2.Succeeded);
            Assert.IsTrue(new IdentityErrorDescriber().DuplicateUserName(user.UserName).Code
                == result2.Errors.First().Code);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void CheckDupEmail()
        {
            IdentityOptions options = new IdentityOptions();
            options.User.RequireUniqueEmail = true;
            UserManager<ApplicationUser> manager = CreateUserManager(options);


            var user = GenTestUser();
            var user2 = GenTestUser();
            var result1 = manager.CreateAsync(user).Result;
            Assert.IsTrue(result1.Succeeded, string.Concat(result1.Errors.Select(e => e.Code)));

            user2.Email = user.Email;
            var result2 = manager.CreateAsync(user2).Result;

            Assert.IsFalse(result2.Succeeded);
            Assert.IsTrue(new IdentityErrorDescriber().DuplicateEmail(user.Email).Code
                == result2.Errors.First().Code);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void CreateUserTest()
        {
            WriteLineObject(CreateTestUser<ApplicationUser>());
        }

        public T CreateUser<T>() where T : IdentityUser, new()
        {
            return CreateTestUser<T>();
        }

        private T CreateTestUser<T>(bool createPassword = true, bool createEmail = true,
            string emailAddress = null) where T : IdentityUser, new()
        {

            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = GenTestUser();
            if (!createEmail)
            {
                user.Email = null;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(emailAddress))
                {
                    user.Email = emailAddress;
                }
            }
            var taskUser = createPassword ?
                manager.CreateAsync(user, DefaultUserPassword) :
                manager.CreateAsync(user);
            taskUser.Wait();
            Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));
            user = manager.FindByIdAsync(user.Id).Result;

            for (int i = 0; i < 1; i++)
            {
                AddUserClaimHelper(user, GenAdminClaim());
                AddUserLoginHelper(user, GenGoogleLogin());
                AddUserRoleHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
            }

            AssertInnerExceptionType<AggregateException, ArgumentException>(() => store.CreateAsync(null).Wait());

            var getUserTask = manager.FindByIdAsync(user.Id);
            getUserTask.Wait();
            return getUserTask.Result as T;
        }

        private async Task<ApplicationUser> CreateTestUserLite(bool createPassword = true, bool createEmail = true,
            string emailAddress = null)
        {
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = GenTestUser();
            if (!createEmail)
            {
                user.Email = null;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(emailAddress))
                {
                    user.Email = emailAddress;
                }
            }
            var taskUser = createPassword ?
                await manager.CreateAsync(user, DefaultUserPassword) :
                await manager.CreateAsync(user);
            Console.WriteLine("User Id: {0}", user.Id);
            user = await manager.FindByIdAsync(user.Id);
            Assert.IsTrue(taskUser.Succeeded, string.Concat(taskUser.Errors));
            List<Claim> lClaims = new List<Claim>();
            for (int i = 0; i < 2; i++)
            {
                string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
                var identityRole = CreateRoleIfNotExists(roleName);

                await manager.AddToRoleAsync(user, roleName);
                await manager.AddLoginAsync(user, GenGoogleLogin());
                lClaims.Add(GenAdminClaim());
            }

            await manager.AddClaimsAsync(user, lClaims);


            return user;
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void DeleteUser()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = GenTestUser();

            var taskUser = manager.CreateAsync(user, DefaultUserPassword);
            taskUser.Wait();
            Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

            user = manager.FindByIdAsync(user.Id).Result;

            for (int i = 0; i < 7; i++)
            {
                AddUserClaimHelper(user, GenAdminClaim());
                AddUserLoginHelper(user, GenGoogleLogin());
                AddUserRoleHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")));
            }

            var findUserTask2 = manager.FindByIdAsync(user.Id);
            findUserTask2.Wait();
            user = findUserTask2.Result;
            WriteLineObject<IdentityUser>(user);


            DateTime start = DateTime.UtcNow;
            var taskUserDel = manager.DeleteAsync(user);
            taskUserDel.Wait();
            Assert.IsTrue(taskUserDel.Result.Succeeded, string.Concat(taskUser.Result.Errors));
            Console.WriteLine("DeleteAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Thread.Sleep(1000);

            var findUserTask = manager.FindByIdAsync(user.Id);
            findUserTask.Wait();
            Assert.IsNull(findUserTask.Result);

            AssertInnerExceptionType<AggregateException, ArgumentException>(() => store.DeleteAsync(null).Wait());
        
        }


        [TestMethod]
        [TestCategory("UserStore.User")]
        public void UpdateApplicationUser()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = GetTestAppUser();
            WriteLineObject<ApplicationUser>(user);
            var taskUser = manager.CreateAsync(user, DefaultUserPassword);
            taskUser.Wait();
            Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

            string oFirstName = user.FirstName;
            string oLastName = user.LastName;

            var taskFind1 = manager.FindByNameAsync(user.UserName);
            taskFind1.Wait();
            Assert.AreEqual<string>(oFirstName, taskFind1.Result.FirstName);
            Assert.AreEqual<string>(oLastName, taskFind1.Result.LastName);

            user = taskFind1.Result;
            string cFirstName = string.Format("John_{0}", Guid.NewGuid());
            string cLastName = string.Format("Doe_{0}", Guid.NewGuid());

            user.FirstName = cFirstName;
            user.LastName = cLastName;

            var taskUserUpdate = manager.UpdateAsync(user);
            taskUserUpdate.Wait();
            Assert.IsTrue(taskUserUpdate.Result.Succeeded, string.Concat(taskUserUpdate.Result.Errors));

            var taskFind = manager.FindByNameAsync(user.UserName);
            taskFind.Wait();
            Assert.AreEqual<string>(cFirstName, taskFind.Result.FirstName);
            Assert.AreEqual<string>(cLastName, taskFind.Result.LastName);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void UpdateUser()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = GenTestUser();
            WriteLineObject<IdentityUser>(user);
            var taskUser = manager.CreateAsync(user, DefaultUserPassword);
            taskUser.Wait();
            Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));
            user = manager.FindByIdAsync(user.Id).Result;
            user.FirstName = "Mike";
            var taskUserUpdate = manager.UpdateAsync(user);
            taskUserUpdate.Wait();

            user = manager.FindByIdAsync(user.Id).Result;
            Assert.IsTrue(taskUserUpdate.Result.Succeeded, string.Concat(taskUserUpdate.Result.Errors));
            Assert.AreEqual<string>("Mike", user.FirstName);
            Assert.ThrowsException<AggregateException>(() => store.UpdateAsync(null).Wait());
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void ChangeUserName()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var firstUser = CreateTestUser<ApplicationUser>();
            Console.WriteLine("{0}", "Original User");
            WriteLineObject(firstUser);
            string originalPlainUserName = firstUser.UserName;
            string originalUserId = firstUser.Id;
            string userNameChange = Guid.NewGuid().ToString("N");

            DateTime start = DateTime.UtcNow;

            var taskUserUpdate = manager.SetUserNameAsync(firstUser, userNameChange);

            taskUserUpdate.Wait();
            Console.WriteLine("UpdateAsync(ChangeUserName): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.IsTrue(taskUserUpdate.Result.Succeeded, string.Concat(taskUserUpdate.Result.Errors));
            Task.Delay(200).Wait();
            var taskUserChanged = manager.FindByNameAsync(userNameChange);
            taskUserChanged.Wait();
            var changedUser = taskUserChanged.Result;

            Console.WriteLine("{0}", "Changed User");
            WriteLineObject<IdentityUser>(changedUser);

            Assert.IsNotNull(changedUser);
            Assert.IsFalse(originalPlainUserName.Equals(changedUser.UserName, StringComparison.OrdinalIgnoreCase), "UserName property not updated.");

            Assert.AreEqual<int>(firstUser.Roles.Count, changedUser.Roles.Count);
            //Assert.IsTrue(changedUser.Roles.All(r => r.PartitionKey == changedUser.Id.ToString()), "Roles partition keys are not equal to the new user id");

            Assert.AreEqual<int>(firstUser.Claims.Count, changedUser.Claims.Count);
            //Assert.IsTrue(changedUser.Claims.All(r => r.PartitionKey == changedUser.Id.ToString()), "Claims partition keys are not equal to the new user id");

            Assert.AreEqual<int>(firstUser.Logins.Count, changedUser.Logins.Count);
            //Assert.IsTrue(changedUser.Logins.All(r => r.PartitionKey == changedUser.Id.ToString()), "Logins partition keys are not equal to the new user id");

            Assert.AreEqual<string>(originalUserId, changedUser.Id);
            Assert.AreNotEqual<string>(originalPlainUserName, changedUser.UserName);
            //Check email
            var taskFindEmail = manager.FindByEmailAsync(changedUser.Email);
            taskFindEmail.Wait();
            Assert.IsNotNull(taskFindEmail.Result);

            //Check the old username is deleted
            var oldUserTask = manager.FindByNameAsync(originalUserId);
            oldUserTask.Wait();
            Assert.IsNull(oldUserTask.Result);

            //Check logins
            foreach (var log in taskFindEmail.Result.Logins)
            {
                var taskFindLogin = manager.FindByLoginAsync(log.LoginProvider, log.ProviderKey);
                taskFindLogin.Wait();
                Assert.IsNotNull(taskFindLogin.Result);
                Assert.AreEqual<string>(originalUserId, taskFindLogin.Result.Id.ToString());
            }

            AssertInnerExceptionType<AggregateException, ArgumentNullException>(() => store.UpdateAsync(null).Wait());
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void FindUserByEmail()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = CreateUser<ApplicationUser>();
            WriteLineObject<IdentityUser>(user);

            DateTime start = DateTime.UtcNow;
            var findUserTask = manager.FindByEmailAsync(user.Email);
            findUserTask.Wait();
            Console.WriteLine("FindByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.AreEqual<string>(user.Email, findUserTask.Result.Email);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void FindUsersByEmail()
        {
            string strEmail = Guid.NewGuid().ToString() + "@live.com";

            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            int createdCount = 11;
            for (int i = 0; i < createdCount; i++)
            {
                var task = CreateTestUserLite(true, true, strEmail);
                task.Wait();
            }

            DateTime start = DateTime.UtcNow;
            Console.WriteLine("FindAllByEmailAsync: {0}", strEmail);

            var findUserTask = store.FindAllByEmailAsync(strEmail); 
            findUserTask.Wait();
            Console.WriteLine("FindAllByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Console.WriteLine("Users Found: {0}", findUserTask.Result.Count());
            Assert.AreEqual<int>(createdCount, findUserTask.Result.Count());

            var listCreated = findUserTask.Result.ToList();

            //Change email and check results
            string strEmailChanged = Guid.NewGuid().ToString() + "@live.com";
            var userToChange = listCreated.Last();
            manager.SetEmailAsync(userToChange, strEmailChanged).Wait();

            var findUserChanged = manager.FindByEmailAsync(strEmailChanged);
            findUserChanged.Wait();
            Assert.AreEqual<string>(userToChange.Id, findUserChanged.Result.Id);
            Assert.AreNotEqual<string>(strEmail, findUserChanged.Result.Email);


            //Make sure changed user doesn't show up in previous query
            start = DateTime.UtcNow;

            findUserTask = store.FindAllByEmailAsync(strEmail);
            findUserTask.Wait();
            Console.WriteLine("FindAllByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Console.WriteLine("Users Found: {0}", findUserTask.Result.Count());
            Assert.AreEqual<int>((listCreated.Count() - 1), findUserTask.Result.Count());


        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void FindUserById()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = CurrentUser;
            DateTime start = DateTime.UtcNow;
            var findUserTask = manager.FindByIdAsync(user.Id);
            findUserTask.Wait();
            Console.WriteLine("FindByIdAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.AreEqual<string>(user.Id, findUserTask.Result.Id);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void FindUserByName()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = CurrentUser;
            WriteLineObject<IdentityUser>(user);
            DateTime start = DateTime.UtcNow;
            var findUserTask = manager.FindByNameAsync(user.UserName);
            findUserTask.Wait();
            Console.WriteLine("FindByNameAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.AreEqual<string>(user.UserName, findUserTask.Result.UserName);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void AddUserLogin()
        {
            var user = CreateTestUser<ApplicationUser>(false);
            WriteLineObject(user);
            AddUserLoginHelper(user, GenGoogleLogin());
        }

        public void AddUserLoginHelper(ApplicationUser user, UserLoginInfo loginInfo)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();


            var userAddLoginTask = manager.AddLoginAsync(user, loginInfo);
            userAddLoginTask.Wait();
            Assert.IsTrue(userAddLoginTask.Result.Succeeded, string.Concat(userAddLoginTask.Result.Errors));

            var loginGetTask = manager.GetLoginsAsync(user);

            loginGetTask.Wait();
            Assert.IsTrue(loginGetTask.Result
                .Any(log => log.LoginProvider == loginInfo.LoginProvider
                    & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

            DateTime start = DateTime.UtcNow;

            var loginGetTask2 = manager.FindByLoginAsync(loginGetTask.Result.First().LoginProvider, loginGetTask.Result.First().ProviderKey);

            loginGetTask2.Wait();
            Console.WriteLine(string.Format("FindAsync(By Login): {0} seconds", (DateTime.UtcNow - start).TotalSeconds));
            Assert.IsNotNull(loginGetTask2.Result);

        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void AddRemoveUserToken()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = GenTestUser();
            WriteLineObject<IdentityUser>(user);
            var taskUser = manager.CreateAsync(user, DefaultUserPassword);
            taskUser.Wait();
            Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));
            user = manager.FindByIdAsync(user.Id).Result;

            string tokenValue = Guid.NewGuid().ToString();
            string tokenName = string.Format("TokenName{0}", Guid.NewGuid().ToString());
            string tokenName2 = string.Format("TokenName2{0}", Guid.NewGuid().ToString());
            Console.WriteLine($"UserId: {user.Id}");
            Console.WriteLine($"TokenName: {tokenName2}");
            Console.WriteLine($"ToienValue: {tokenValue}");

            manager.SetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName,
                tokenValue).Wait();

            string getTokenValue = manager.GetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName).Result;
            Assert.IsNotNull(tokenName);
            Assert.AreEqual(getTokenValue, tokenValue);

            manager.SetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName2,
                tokenValue).Wait();

            manager.RemoveAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName).Wait();

            getTokenValue = manager.GetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName).Result;
            Assert.IsNull(getTokenValue);

            getTokenValue = manager.GetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName2).Result;
            Assert.IsNotNull(getTokenValue);
            Assert.AreEqual(getTokenValue, tokenValue);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void AddRemoveUserLogin()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = GenTestUser();
            WriteLineObject<IdentityUser>(user);
            var taskUser = manager.CreateAsync(user, DefaultUserPassword);
            taskUser.Wait();
            Assert.IsTrue(taskUser.Result.Succeeded, string.Concat(taskUser.Result.Errors));

            var loginInfo = GenGoogleLogin();

            user = manager.FindByIdAsync(user.Id).Result;
            var userAddLoginTask = manager.AddLoginAsync(user, loginInfo);

            userAddLoginTask.Wait();
            Assert.IsTrue(userAddLoginTask.Result.Succeeded, string.Concat(userAddLoginTask.Result.Errors));


            var loginGetTask = manager.GetLoginsAsync(user);

            loginGetTask.Wait();
            Assert.IsTrue(loginGetTask.Result
                .Any(log => log.LoginProvider == loginInfo.LoginProvider
                    & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

            var loginGetTask2 = manager.FindByLoginAsync(loginGetTask.Result.First().LoginProvider, loginGetTask.Result.First().ProviderKey);

            loginGetTask2.Wait();
            Assert.IsNotNull(loginGetTask2.Result);

            var userRemoveLoginTaskNeg1 = manager.RemoveLoginAsync(user, string.Empty, loginInfo.ProviderKey);

            userRemoveLoginTaskNeg1.Wait();

            var userRemoveLoginTaskNeg2 = manager.RemoveLoginAsync(user, loginInfo.LoginProvider, string.Empty);

            userRemoveLoginTaskNeg2.Wait();


            var userRemoveLoginTask = manager.RemoveLoginAsync(user, loginInfo.LoginProvider, loginInfo.ProviderKey);

            userRemoveLoginTask.Wait();
            Assert.IsTrue(userRemoveLoginTask.Result.Succeeded, string.Concat(userRemoveLoginTask.Result.Errors));
            var loginGetTask3 = manager.GetLoginsAsync(user);

            loginGetTask3.Wait();
            Assert.IsTrue(!loginGetTask3.Result.Any(), "LoginInfo not removed");

            //Negative cases

            var loginFindNeg = manager.FindByLoginAsync("asdfasdf", "http://4343443dfaksjfaf");

            loginFindNeg.Wait();
            Assert.IsNull(loginFindNeg.Result);

            Assert.ThrowsException<ArgumentNullException>(() => store.AddLoginAsync(null, loginInfo).Wait());

            Assert.ThrowsException<ArgumentNullException>(() => store.AddLoginAsync(user, null).Wait());

            Assert.ThrowsException<ArgumentNullException>(() => store.RemoveLoginAsync(null, loginInfo.ProviderKey, loginInfo.LoginProvider).Wait());

            Assert.ThrowsException<AggregateException>(() => store.GetLoginsAsync(null).Wait());
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void AddUserRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            WriteLineObject<IdentityUser>(CurrentUser);
            AddUserRoleHelper(CurrentUser, strUserRole);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void GetUsersByRole()
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            var identityRole = CreateRoleIfNotExists(strUserRole);

            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();
            int userCount = 4;
            DateTime start2 = DateTime.UtcNow;
            ApplicationUser tempUser = null;
            IdentityRole role = CreateRoleIfNotExists(strUserRole);
            Console.WriteLine($"RoleId: {role.Id}");
            for (int i = 0; i < userCount; i++)
            {
                DateTime start = DateTime.UtcNow;
                Console.WriteLine("CreateTestUserLite()");
                tempUser = CreateTestUserLite(true, true).Result;
                Console.WriteLine("CreateTestUserLite(): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                AddUserRoleHelper(tempUser, strUserRole);
            }
            Console.WriteLine("GenerateUsers(): {0} user count", userCount);
            Console.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

            start2 = DateTime.UtcNow;
            var users = manager.GetUsersInRoleAsync(strUserRole).Result;
            Console.WriteLine("GetUsersInRoleAsync(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

            Assert.AreEqual(users.Where(u => u.Roles.Any(r=> r.RoleId == role.Id)).Count(), userCount);
        }

        private IdentityRole CreateRoleIfNotExists(string roleName)
        {
            var rmanager = CreateRoleManager();

            var userRole = rmanager.FindByNameAsync(roleName);
            userRole.Wait();
            IdentityRole role = userRole.Result;
            if (userRole.Result == null)
            {
                var taskResult = rmanager.CreateAsync(new IdentityRole(roleName)).Result;
                Assert.IsTrue(taskResult.Succeeded);
                role = rmanager.FindByNameAsync(roleName).Result;
            }

            return role;
        }

        public IdentityRole AddUserRoleHelper(ApplicationUser user, string roleName)
        {
            var identityRole = CreateRoleIfNotExists(roleName);
            Console.WriteLine($"RoleId: {identityRole.Id}");

            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var userRoleTask = manager.AddToRoleAsync(user, roleName);

            userRoleTask.Wait();
            Assert.IsTrue(userRoleTask.Result.Succeeded, string.Concat(userRoleTask.Result.Errors));


            var roles2Task = manager.IsInRoleAsync(user, roleName);
            roles2Task.Wait();
            Assert.IsTrue(roles2Task.Result, "Role not found");
            return identityRole;
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void AddRemoveUserRole()
        {
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestAdminRole, Guid.NewGuid().ToString("N"));
            
            var adminRole = CreateRoleIfNotExists(roleName);

            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();
            var user = CurrentUser;
            user = manager.FindByIdAsync(user.Id).Result;
            WriteLineObject(user);
            var userRoleTask = manager.AddToRoleAsync(user, roleName);

            userRoleTask.Wait();
            Assert.IsTrue(userRoleTask.Result.Succeeded, string.Concat(userRoleTask.Result.Errors));
            DateTime getRolesStart = DateTime.UtcNow;
            var rolesTask = manager.GetRolesAsync(user);

            rolesTask.Wait();
            var tempRoles = rolesTask.Result;
            var getout = string.Format("{0} ms", (DateTime.UtcNow - getRolesStart).TotalMilliseconds);
            Console.WriteLine(getout);
            Console.WriteLine(getout);
            Assert.IsTrue(rolesTask.Result.Contains(roleName), "Role not found");

            DateTime isInRolesStart = DateTime.UtcNow;


            var roles2Task = manager.IsInRoleAsync(user, roleName);

            roles2Task.Wait();
            var isInout = string.Format("IsInRoleAsync() {0} ms", (DateTime.UtcNow - isInRolesStart).TotalMilliseconds);
            Console.WriteLine(isInout);
            Assert.IsTrue(roles2Task.Result, "Role not found");


            manager.RemoveFromRoleAsync(user, roleName).Wait();

            DateTime start = DateTime.UtcNow;
            var rolesTask2 = manager.GetRolesAsync(user).Result;

            Console.WriteLine("GetRolesAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.IsFalse(rolesTask2.Contains(roleName), "Role not removed.");


            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.AddToRoleAsync(null, roleName).Wait()).InnerException is ArgumentException);

            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.AddToRoleAsync(user, null).Wait()).InnerException is ArgumentException);

            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.AddToRoleAsync(user, Guid.NewGuid().ToString()).Wait()).InnerException is InvalidOperationException);

            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.RemoveFromRoleAsync(null, roleName).Wait()).InnerException is ArgumentException);

            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.RemoveFromRoleAsync(user, null).Wait()).InnerException is ArgumentException);

            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.GetRolesAsync(null).Wait()).InnerException is ArgumentException);

        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void IsUserInRole()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = CurrentUser;
            WriteLineObject(user);
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));

            AddUserRoleHelper(user, roleName);

            DateTime start = DateTime.UtcNow;

            var roles2Task = manager.IsInRoleAsync(user, roleName);

            roles2Task.Wait();
            Console.WriteLine("IsInRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Assert.IsTrue(roles2Task.Result, "Role not found");

            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.IsInRoleAsync(null, roleName).Wait()).InnerException is ArgumentException);
            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.IsInRoleAsync(user, null).Wait()).InnerException is ArgumentException);
           
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public async Task GenerateUsers()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            int userCount = 10;
            DateTime start2 = DateTime.UtcNow;
            for (int i = 0; i < userCount; i++)
            {
                DateTime start = DateTime.UtcNow;
                Console.WriteLine("CreateTestUserLite()");
                await CreateTestUserLite(true, true);
                Console.WriteLine("CreateTestUserLite(): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            }
            Console.WriteLine("GenerateUsers(): {0} user count", userCount);
            Console.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void AddUserClaim()
        {
            WriteLineObject<IdentityUser>(CurrentUser);
            AddUserClaimHelper(CurrentUser, GenUserClaim());
        }

        private void AddUserClaimHelper(ApplicationUser user, Claim claim)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();


            var userClaimTask = manager.AddClaimAsync(user, claim);

            userClaimTask.Wait();
            Assert.IsTrue(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors.Select(e => e.Code)));
            var claimsTask = manager.GetClaimsAsync(user);

            claimsTask.Wait();
            Assert.IsTrue(claimsTask.Result.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void GetUsersByClaim()
        {
            var claim = GenUserClaim();
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            int userCount = 5;
            DateTime start2 = DateTime.UtcNow;
            ApplicationUser tempUser = null;
            for (int i = 0; i < userCount; i++)
            {
                DateTime start = DateTime.UtcNow;
                Console.WriteLine("CreateTestUserLite()");
                tempUser = CreateTestUserLite(true, true).Result;
                Console.WriteLine("CreateTestUserLite(): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                AddUserClaimHelper(tempUser, claim);
            }
            Console.WriteLine("GenerateUsers(): {0} user count", userCount);
            Console.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

            start2 = DateTime.UtcNow;
            var users = manager.GetUsersForClaimAsync(claim).Result;
            Console.WriteLine("GetUsersForClaimAsync(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

            Assert.AreEqual(users.Where(u => u.Claims.Single(c => c.ClaimType == claim.Type && c.ClaimValue == c.ClaimValue) !=null).Count(), userCount);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void AddRemoveUserClaim()
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore();
            UserManager<ApplicationUser> manager = CreateUserManager();

            var user = CurrentUser;
            WriteLineObject<IdentityUser>(user);
            Claim claim = GenAdminClaim();

            var userClaimTask = manager.AddClaimAsync(user, claim);

            userClaimTask.Wait();
            Assert.IsTrue(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors));

            var claimsTask = manager.GetClaimsAsync(user);

            claimsTask.Wait();
            Assert.IsTrue(claimsTask.Result.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");


            var userRemoveClaimTask = manager.RemoveClaimAsync(user, claim);

            userRemoveClaimTask.Wait();
            Assert.IsTrue(userClaimTask.Result.Succeeded, string.Concat(userClaimTask.Result.Errors));
            var claimsTask2 = manager.GetClaimsAsync(user);

            claimsTask2.Wait();
            Assert.IsTrue(!claimsTask2.Result.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not removed");

            //adding test for removing an empty claim
            Claim claimEmpty = GenAdminClaimEmptyValue();

            var userClaimTask2 = manager.AddClaimAsync(user, claimEmpty);

            userClaimTask2.Wait();

            var userRemoveClaimTask2 = manager.RemoveClaimAsync(user, claimEmpty);

            userRemoveClaimTask2.Wait();
            Assert.IsTrue(userClaimTask2.Result.Succeeded, string.Concat(userClaimTask2.Result.Errors));

            Assert.ThrowsException<ArgumentNullException>(() => store.AddClaimsAsync(null, new List<Claim>() { claim }).Wait());

            Assert.ThrowsException<ArgumentNullException>(() => store.AddClaimsAsync(user, null).Wait());

            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.RemoveClaimsAsync(null, new List<Claim>() { claim }).Wait()).InnerException is ArgumentException);

            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.RemoveClaimsAsync(user, null).Wait()).InnerException is ArgumentException);

            Assert.ThrowsException<ArgumentNullException>(() => store.RemoveClaimsAsync(user, new List<Claim>() { new Claim(claim.Type, null) }).Wait());

            Assert.ThrowsException<ArgumentNullException>(() => store.AddClaimsAsync(null, new List<Claim>() { claim }).Wait());
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public void ThrowIfDisposed()
        {
            var store = new UserStore<ApplicationUser, IdentityRole, IdentityCloudContext>(GetContext(), describer: new IdentityErrorDescriber());
            store.Dispose();
            GC.Collect();

            Assert.IsTrue(Assert.ThrowsException<AggregateException>(() => store.DeleteAsync(new ApplicationUser()).Wait()).InnerException is ObjectDisposedException);            
        }

    }
}
