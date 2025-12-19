// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.CosmosDB.Tests.ModelTests;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IdentityRole = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityRole;
using IdentityUser = ElCamino.AspNetCore.Identity.CosmosDB.Model.IdentityUser;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Tests
{
    public partial class UserStoreTests : BaseTest<ApplicationUser, IdentityRole, IdentityCloudContext>
    {
        public static object objectLock = new object();
        public static string DefaultUserPassword = "M" + Guid.NewGuid().ToString();


        private ApplicationUser currentUser = null;
        private ApplicationUser currentRoleUser = null;

        private TestContext testContextInstance;
        private readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

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

        public Task<ApplicationUser> CurrentUser(bool includeRoles)
        {
            if (!includeRoles)
            {
                if (currentUser == null)
                {
                    lock (objectLock)
                    {
                        if (currentUser == null)
                        {
                            var task = CreateUser<ApplicationUser>(includeRoles);
                            task.Wait();
                            currentUser = task.Result;
                        }
                    }
                }
                return Task.FromResult(currentUser);
            }
            else
            {
                if (currentRoleUser == null)
                {
                    lock (objectLock)
                    {
                        if (currentRoleUser == null)
                        {
                            var task = CreateUser<ApplicationUser>(includeRoles);
                            task.Wait();
                            currentRoleUser = task.Result;
                        }
                    }
                }
                return Task.FromResult(currentRoleUser);
            }

        }
        




        #region Test Initialization
        [TestInitialize]
        public void Initialize()
        {
            

        }
        #endregion

        private void WriteLineObject<T>(T obj) where T : class
        {
            Console.WriteLine(typeof(T).Name);
            string strLine = obj is null ? "Null" : JsonSerializer.Serialize(obj, JsonOptions);
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
        [DataRow(true, DisplayName ="IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task CheckDupUser(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = GenTestUser();
            var user2 = GenTestUser();
            var result1 = await manager.CreateAsync(user);
            Assert.IsTrue(result1.Succeeded, string.Concat(result1.Errors.Select(e => e.Code)));
            user2.UserName = user.UserName;
            var result2 = await manager.CreateAsync(user2);
            Assert.IsFalse(result2.Succeeded);
            Assert.IsTrue(new IdentityErrorDescriber().DuplicateUserName(user.UserName).Code
                == result2.Errors.First().Code);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public async Task CheckDupEmail()
        {
            IdentityOptions options = new IdentityOptions();
            options.User.RequireUniqueEmail = true;
            UserManager<ApplicationUser> manager = CreateUserManager(options);


            var user = GenTestUser();
            var user2 = GenTestUser();
            var result1 = await manager.CreateAsync(user);
            Assert.IsTrue(result1.Succeeded, string.Concat(result1.Errors.Select(e => e.Code)));

            user2.Email = user.Email;
            var result2 = await manager.CreateAsync(user2);

            Assert.IsFalse(result2.Succeeded);
            Assert.IsTrue(new IdentityErrorDescriber().DuplicateEmail(user.Email).Code
                == result2.Errors.First().Code);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task CreateUserTest(bool includeRoles)
        {
            WriteLineObject(await CreateTestUser<ApplicationUser>(includeRoles));
        }

        public Task<T> CreateUser<T>(bool includeRoles) where T : IdentityUser, new()
        {
            return CreateTestUser<T>(includeRoles);
        }

        private async Task<T> CreateTestUser<T>(bool includeRoles, bool createPassword = true, bool createEmail = true,
            string emailAddress = null) where T : IdentityUser, new()
        {

            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

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
            Assert.IsTrue(taskUser.Succeeded, string.Concat(taskUser.Errors));
            user = await manager.FindByIdAsync(user.Id);

            for (int i = 0; i < 1; i++)
            {
                await AddUserClaimHelper(user, GenAdminClaim(), includeRoles);
                await AddUserLoginHelper(user, GenGoogleLogin(), includeRoles);
                if (includeRoles)
                {
                    await AddUserRoleHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")), includeRoles);
                }
            }

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.CreateAsync(null));

            return  (await manager.FindByIdAsync(user.Id)) as T;
        }

        private async Task<ApplicationUser> CreateTestUserLite(bool includeRoles, bool createPassword = true, bool createEmail = true,
            string emailAddress = null)
        {
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

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
                if (includeRoles)
                {
                    string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
                    await CreateRoleIfNotExists(includeRoles, roleName);

                    await manager.AddToRoleAsync(user, roleName);
                }
                await manager.AddLoginAsync(user, GenGoogleLogin());
                lClaims.Add(GenAdminClaim());
            }

            await manager.AddClaimsAsync(user, lClaims);


            return user;
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task DeleteUser(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = GenTestUser();

            var taskUser = await manager.CreateAsync(user, DefaultUserPassword);
            Assert.IsTrue(taskUser.Succeeded, string.Concat(taskUser.Errors));

            user = await manager.FindByIdAsync(user.Id);

            for (int i = 0; i < 7; i++)
            {
                await AddUserClaimHelper(user, GenAdminClaim(), includeRoles);
                await AddUserLoginHelper(user, GenGoogleLogin(), includeRoles);
                if (includeRoles)
                {
                    await AddUserRoleHelper(user, string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N")), includeRoles);
                }
            }

            user = await manager.FindByIdAsync(user.Id);
            WriteLineObject<ApplicationUser>(user);


            DateTime start = DateTime.UtcNow;
            var taskUserDel = await manager.DeleteAsync(user);
            Assert.IsTrue(taskUserDel.Succeeded, string.Concat(taskUser.Errors));
            Console.WriteLine("DeleteAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);


            var findUserTask = await manager.FindByIdAsync(user.Id);
            Assert.IsNull(findUserTask);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.DeleteAsync(null));
        
        }


        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task UpdateApplicationUser(bool includeRoles)
        {
            _ = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = GetTestAppUser();
            WriteLineObject<ApplicationUser>(user);
            var taskUser = await manager.CreateAsync(user, DefaultUserPassword);
            Assert.IsTrue(taskUser.Succeeded, string.Concat(taskUser.Errors));

            string oFirstName = user.FirstName;
            string oLastName = user.LastName;

            user = await manager.FindByNameAsync(user.UserName);
            Assert.AreEqual<string>(oFirstName, user.FirstName);
            Assert.AreEqual<string>(oLastName, user.LastName);

            string cFirstName = string.Format("John_{0}", Guid.NewGuid());
            string cLastName = string.Format("Doe_{0}", Guid.NewGuid());

            user.FirstName = cFirstName;
            user.LastName = cLastName;

            var taskUserUpdate = await manager.UpdateAsync(user);
            Assert.IsTrue(taskUserUpdate.Succeeded, string.Concat(taskUserUpdate.Errors));

            var taskFind = await manager.FindByNameAsync(user.UserName);
            Assert.AreEqual<string>(cFirstName, taskFind.FirstName);
            Assert.AreEqual<string>(cLastName, taskFind.LastName);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task UpdateUser(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = GenTestUser();
            WriteLineObject<ApplicationUser>(user);
            var taskUser = await manager.CreateAsync(user, DefaultUserPassword);
            Assert.IsTrue(taskUser.Succeeded, string.Concat(taskUser.Errors));
            user = await manager.FindByIdAsync(user.Id);
            user.FirstName = "Mike";
            var taskUserUpdate = await manager.UpdateAsync(user);

            user = await manager.FindByIdAsync(user.Id);
            Assert.IsTrue(taskUserUpdate.Succeeded, string.Concat(taskUserUpdate.Errors));
            Assert.AreEqual<string>("Mike", user.FirstName);
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.UpdateAsync(null));
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task ChangeUserName(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var firstUser = await CreateTestUser<ApplicationUser>(includeRoles);
            Console.WriteLine("{0}", "Original User");
            WriteLineObject(firstUser);
            string originalPlainUserName = firstUser.UserName;
            string originalUserId = firstUser.Id;
            string userNameChange = Guid.NewGuid().ToString("N");

            DateTime start = DateTime.UtcNow;

            var taskUserUpdate = await manager.SetUserNameAsync(firstUser, userNameChange);

            Console.WriteLine("UpdateAsync(ChangeUserName): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.IsTrue(taskUserUpdate.Succeeded, string.Concat(taskUserUpdate.Errors));
            var changedUser = await manager.FindByNameAsync(userNameChange);

            Console.WriteLine("{0}", "Changed User");
            WriteLineObject<IdentityUser>(changedUser);

            Assert.IsNotNull(changedUser);
            Assert.IsFalse(originalPlainUserName.Equals(changedUser.UserName, StringComparison.OrdinalIgnoreCase), "UserName property not updated.");

            Assert.AreEqual<int>(firstUser.Roles.Count, changedUser.Roles.Count);

            Assert.AreEqual<int>(firstUser.Claims.Count, changedUser.Claims.Count);

            Assert.AreEqual<int>(firstUser.Logins.Count, changedUser.Logins.Count);

            Assert.AreEqual<string>(originalUserId, changedUser.Id);
            Assert.AreNotEqual<string>(originalPlainUserName, changedUser.UserName);
            //Check email
            var taskFindEmail = await manager.FindByEmailAsync(changedUser.Email);
            Assert.IsNotNull(taskFindEmail);

            //Check the old username is deleted
            var oldUserTask = await manager.FindByNameAsync(originalUserId);
            Assert.IsNull(oldUserTask);

            //Check logins
            foreach (var log in taskFindEmail.Logins)
            {
                var taskFindLogin = await manager.FindByLoginAsync(log.LoginProvider, log.ProviderKey);
                Assert.IsNotNull(taskFindLogin);
                Assert.AreEqual<string>(originalUserId, taskFindLogin.Id.ToString());
            }

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.UpdateAsync(null));
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task FindUserByEmail(bool includeRoles)
        {
            _ = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = await CreateUser<ApplicationUser>(includeRoles);
            WriteLineObject<IdentityUser>(user);

            DateTime start = DateTime.UtcNow;
            var findUserTask = await manager.FindByEmailAsync(user.Email);
            Console.WriteLine("FindByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.AreEqual<string>(user.Email, findUserTask.Email);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task FindUsersByEmail(bool includeRoles)
        {
            string strEmail = Guid.NewGuid().ToString() + "@live.com";

            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);
#if NETCOREAPP3_0
            strEmail = manager.NormalizeEmail(strEmail);
#else
            strEmail = strEmail.ToUpper();
#endif
            int createdCount = 11;
            for (int i = 0; i < createdCount; i++)
            {
                var task = await CreateTestUserLite(includeRoles, true, true, strEmail);
            }

            DateTime start = DateTime.UtcNow;
            Console.WriteLine("FindAllByEmailAsync: {0}", strEmail);

            var findUserTask = await store.FindAllByEmailAsync(strEmail); 
            Console.WriteLine("FindAllByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Console.WriteLine("Users Found: {0}", findUserTask.Count());
            Assert.AreEqual<int>(createdCount, findUserTask.Count());

            var listCreated = findUserTask.ToList();

            //Change email and check results
            string strEmailChanged = Guid.NewGuid().ToString() + "@live.com";
            var userToChange = listCreated.Last();
            await manager.SetEmailAsync(userToChange, strEmailChanged);

            var findUserChanged = await manager.FindByEmailAsync(strEmailChanged);
            Assert.AreEqual<string>(userToChange.Id, findUserChanged.Id);
            Assert.AreNotEqual<string>(strEmail, findUserChanged.Email);


            //Make sure changed user doesn't show up in previous query
            start = DateTime.UtcNow;

            findUserTask = await store.FindAllByEmailAsync(strEmail);
            Console.WriteLine("FindAllByEmailAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Console.WriteLine("Users Found: {0}", findUserTask.Count());
            Assert.AreEqual<int>(listCreated.Count() - 1, findUserTask.Count());
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task FindUserById(bool includeRoles)
        {
            _ = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = await CurrentUser(includeRoles);
            DateTime start = DateTime.UtcNow;
            var findUser = await manager.FindByIdAsync(user.Id);

            Console.WriteLine("FindByIdAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.AreEqual<string>(user.Id, findUser.Id);

            findUser = await manager.FindByIdAsync(Guid.NewGuid().ToString());
            Assert.IsNull(findUser);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task FindUserByName(bool includeRoles)
        {
            _ = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = await CurrentUser(includeRoles);
            WriteLineObject<IdentityUser>(user);
            DateTime start = DateTime.UtcNow;
            var findUser = await manager.FindByNameAsync(user.UserName);
            Console.WriteLine("FindByNameAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.AreEqual<string>(user.UserName, findUser.UserName);

            findUser = await manager.FindByNameAsync(Guid.NewGuid().ToString());
            Assert.IsNull(findUser);

        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task AddUserLogin(bool includeRoles)
        {
            var user = await CreateTestUser<ApplicationUser>(includeRoles, false);
            WriteLineObject(user);
            await AddUserLoginHelper(user, GenGoogleLogin(), includeRoles);
        }

        public async Task AddUserLoginHelper(ApplicationUser user, UserLoginInfo loginInfo, bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);


            var userAddLoginTask = await manager.AddLoginAsync(user, loginInfo);
            Assert.IsTrue(userAddLoginTask.Succeeded, string.Concat(userAddLoginTask.Errors));

            var loginGetTask = await manager.GetLoginsAsync(user);

            Assert.IsTrue(loginGetTask
                .Any(log => log.LoginProvider == loginInfo.LoginProvider
                    & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

            DateTime start = DateTime.UtcNow;

            var loginGetTask2 = await manager.FindByLoginAsync(loginGetTask.First().LoginProvider, loginGetTask.First().ProviderKey);

            Console.WriteLine(string.Format("FindAsync(By Login): {0} seconds", (DateTime.UtcNow - start).TotalSeconds));
            Assert.IsNotNull(loginGetTask2);

        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task AddRemoveUserToken(bool includeRoles)
        {
            _ = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = GenTestUser();
            WriteLineObject<IdentityUser>(user);
            var taskUser = await manager.CreateAsync(user, DefaultUserPassword);
            Assert.IsTrue(taskUser.Succeeded, string.Concat(taskUser.Errors));
            user = await manager.FindByIdAsync(user.Id);

            string tokenValue = Guid.NewGuid().ToString();
            string tokenName = string.Format("TokenName{0}", Guid.NewGuid().ToString());
            string tokenName2 = string.Format("TokenName2{0}", Guid.NewGuid().ToString());
            Console.WriteLine($"UserId: {user.Id}");
            Console.WriteLine($"TokenName: {tokenName2}");
            Console.WriteLine($"ToienValue: {tokenValue}");

            await manager.SetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName,
                tokenValue);

            string getTokenValue = await manager.GetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName);
            Assert.IsNotNull(tokenName);
            Assert.AreEqual(getTokenValue, tokenValue);

            await manager.SetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName2,
                tokenValue);

            await manager.RemoveAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName);

            getTokenValue = await manager.GetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName);
            Assert.IsNull(getTokenValue);

            getTokenValue = await manager.GetAuthenticationTokenAsync(user,
                Constants.LoginProviders.GoogleProvider.LoginProvider,
                tokenName2);
            Assert.IsNotNull(getTokenValue);
            Assert.AreEqual(getTokenValue, tokenValue);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]

        public async Task AddRemoveUserLogin(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = GenTestUser();
            WriteLineObject<IdentityUser>(user);
            var taskUser = await manager.CreateAsync(user, DefaultUserPassword);
            Assert.IsTrue(taskUser.Succeeded, string.Concat(taskUser.Errors));

            var loginInfo = GenGoogleLogin();

            user = await manager.FindByIdAsync(user.Id);
            var userAddLoginTask = await manager.AddLoginAsync(user, loginInfo);

            Assert.IsTrue(userAddLoginTask.Succeeded, string.Concat(userAddLoginTask.Errors));

            var loginGetTask = await manager.GetLoginsAsync(user);

            Assert.IsTrue(loginGetTask
                .Any(log => log.LoginProvider == loginInfo.LoginProvider
                    & log.ProviderKey == loginInfo.ProviderKey), "LoginInfo not found: GetLoginsAsync");

            var loginGetTask2 = await manager.FindByLoginAsync(loginGetTask.First().LoginProvider, loginGetTask.First().ProviderKey);
            Assert.IsNotNull(loginGetTask2);

            var userRemoveLoginTaskNeg1 = await manager.RemoveLoginAsync(user, string.Empty, loginInfo.ProviderKey);

            var userRemoveLoginTaskNeg2 = await manager.RemoveLoginAsync(user, loginInfo.LoginProvider, string.Empty);

            var userRemoveLoginTask = await manager.RemoveLoginAsync(user, loginInfo.LoginProvider, loginInfo.ProviderKey);

            Assert.IsTrue(userRemoveLoginTask.Succeeded, string.Concat(userRemoveLoginTask.Errors));
           
            var loginGetTask3 = await manager.GetLoginsAsync(user);
            Assert.IsTrue(!loginGetTask3.Any(), "LoginInfo not removed");

            //Negative cases

            var loginFindNeg = await manager.FindByLoginAsync("asdfasdf", "http://4343443dfaksjfaf");
            Assert.IsNull(loginFindNeg);

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.AddLoginAsync(null, loginInfo));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.AddLoginAsync(user, null));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.RemoveLoginAsync(null, loginInfo.ProviderKey, loginInfo.LoginProvider));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.GetLoginsAsync(null));
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        public async Task AddUserRole(bool includeRoles)
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            WriteLineObject<IdentityUser>(await CurrentUser(includeRoles));
            await AddUserRoleHelper(await CurrentUser(includeRoles), strUserRole, includeRoles);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        public async Task GetUsersByRole(bool includeRoles)
        {
            string strUserRole = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));
            var identityRole = await CreateRoleIfNotExists(includeRoles, strUserRole);

            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);
            int userCount = 4;
            DateTime start2 = DateTime.UtcNow;
            ApplicationUser tempUser = null;
            IdentityRole role = await CreateRoleIfNotExists(includeRoles, strUserRole);
            Console.WriteLine($"RoleId: {role.Id}");
            for (int i = 0; i < userCount; i++)
            {
                DateTime start = DateTime.UtcNow;
                Console.WriteLine("CreateTestUserLite()");
                tempUser = await CreateTestUserLite(true, true);
                Console.WriteLine("CreateTestUserLite(): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                await AddUserRoleHelper(tempUser, strUserRole, includeRoles);
            }
            Console.WriteLine("GenerateUsers(): {0} user count", userCount);
            Console.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

            start2 = DateTime.UtcNow;
            var users = await manager.GetUsersInRoleAsync(strUserRole);
            Console.WriteLine("GetUsersInRoleAsync(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

            Assert.AreEqual(users.Where(u => u.Roles.Any(r=> r.RoleId == role.Id)).Count(), userCount);
        }

        private async Task<IdentityRole> CreateRoleIfNotExists(bool includeRoles, string roleName)
        {
            var rmanager = CreateRoleManager(includeRoles);

            var userRole = await rmanager.FindByNameAsync(roleName);
            IdentityRole role = userRole;
            if (userRole == null)
            {
                var taskResult = await rmanager.CreateAsync(new IdentityRole(roleName));
                Assert.IsTrue(taskResult.Succeeded);
                role = await rmanager.FindByNameAsync(roleName);
            }

            return role;
        }

        public async Task<IdentityRole> AddUserRoleHelper(ApplicationUser user, string roleName, bool includeRoles)
        {
            var identityRole = await CreateRoleIfNotExists(includeRoles, roleName);
            Console.WriteLine($"RoleId: {identityRole.Id}");
            _ = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var userRoleTask = await manager.AddToRoleAsync(user, roleName);
            Assert.IsTrue(userRoleTask.Succeeded, string.Concat(userRoleTask.Errors));


            var roles2Task = await manager.IsInRoleAsync(user, roleName);
            Assert.IsTrue(roles2Task, "Role not found");
            return identityRole;
        }

        [TestCategory("UserStore.User")]
        [TestMethod]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        public async Task AddRemoveUserRole(bool includeRoles)
        {
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestAdminRole, Guid.NewGuid().ToString("N"));
            
            var adminRole = await CreateRoleIfNotExists(includeRoles, roleName);

            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);
            var user = await CurrentUser(includeRoles);
            user = await manager.FindByIdAsync(user.Id);
            WriteLineObject(user.Id);
            var userRoleTask = await manager.AddToRoleAsync(user, roleName);

            Assert.IsTrue(userRoleTask.Succeeded, string.Concat(userRoleTask.Errors));
            DateTime getRolesStart = DateTime.UtcNow;
            var tempRoles = await manager.GetRolesAsync(user);

            var getout = string.Format("{0} ms", (DateTime.UtcNow - getRolesStart).TotalMilliseconds);
            Console.WriteLine(getout);
            Assert.IsTrue(tempRoles.Contains(roleName), "Role not found");

            DateTime isInRolesStart = DateTime.UtcNow;


            var roles2Task = await manager.IsInRoleAsync(user, roleName);

            var isInout = string.Format("IsInRoleAsync() {0} ms", (DateTime.UtcNow - isInRolesStart).TotalMilliseconds);
            Console.WriteLine(isInout);
            Assert.IsTrue(roles2Task, "Role not found");


            await manager.RemoveFromRoleAsync(user, roleName);

            DateTime start = DateTime.UtcNow;
            var rolesTask2 = await manager.GetRolesAsync(user);

            Console.WriteLine("GetRolesAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);

            Assert.IsFalse(rolesTask2.Contains(roleName), "Role not removed.");


            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await store.AddToRoleAsync(null, roleName));

            await Assert.ThrowsExceptionAsync <ArgumentException>(async () => await store.AddToRoleAsync(user, null));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await store.AddToRoleAsync(user, Guid.NewGuid().ToString()));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await store.RemoveFromRoleAsync(null, roleName));

            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await store.RemoveFromRoleAsync(user, null));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await store.GetRolesAsync(null));

        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        public async Task IsUserInRole(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = await CurrentUser(includeRoles);
            WriteLineObject(user);
            string roleName = string.Format("{0}_{1}", Constants.AccountRoles.AccountTestUserRole, Guid.NewGuid().ToString("N"));

            await AddUserRoleHelper(user, roleName, includeRoles);

            DateTime start = DateTime.UtcNow;

            var roles2Task = await manager.IsInRoleAsync(user, roleName);
            Console.WriteLine("IsInRoleAsync: {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            Assert.IsTrue(roles2Task, "Role not found");

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await store.IsInRoleAsync(null, roleName));
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await store.IsInRoleAsync(user, null));
           
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]

        public async Task GenerateUsers(bool includeRoles)
        {
            _ = CreateUserStore(includeRoles);
            _ = CreateUserManager(includeRoles);

            int userCount = 10;
            DateTime start2 = DateTime.UtcNow;
            for (int i = 0; i < userCount; i++)
            {
                DateTime start = DateTime.UtcNow;
                Console.WriteLine("CreateTestUserLite()");
                await CreateTestUserLite(includeRoles, true, true);
                Console.WriteLine("CreateTestUserLite(): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
            }
            Console.WriteLine("GenerateUsers(): {0} user count", userCount);
            Console.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task AddUserClaim(bool includeRoles)
        {
            WriteLineObject<IdentityUser>(await CurrentUser(includeRoles));
            await AddUserClaimHelper(await CurrentUser(includeRoles), GenUserClaim(), includeRoles);
        }

        private async Task AddUserClaimHelper(ApplicationUser user, Claim claim, bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var userClaimTask = await manager.AddClaimAsync(user, claim);
            Assert.IsTrue(userClaimTask.Succeeded, string.Concat(userClaimTask.Errors.Select(e => e.Code)));
            var claimsTask = await manager.GetClaimsAsync(user);
            Assert.IsTrue(claimsTask.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task GetUsersByClaim(bool includeRoles)
        {
            var claim = GenUserClaim();
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            int userCount = 5;
            DateTime start2 = DateTime.UtcNow;
            ApplicationUser tempUser = null;
            for (int i = 0; i < userCount; i++)
            {
                DateTime start = DateTime.UtcNow;
                Console.WriteLine("CreateTestUserLite()");
                tempUser = await CreateTestUserLite(includeRoles, true, true);
                Console.WriteLine("CreateTestUserLite(): {0} seconds", (DateTime.UtcNow - start).TotalSeconds);
                await AddUserClaimHelper(tempUser, claim, includeRoles);
            }
            Console.WriteLine("GenerateUsers(): {0} user count", userCount);
            Console.WriteLine("GenerateUsers(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

            start2 = DateTime.UtcNow;
            var users = await manager.GetUsersForClaimAsync(claim);
            Console.WriteLine("GetUsersForClaimAsync(): {0} seconds", (DateTime.UtcNow - start2).TotalSeconds);

            Assert.AreEqual(users.Where(u => u.Claims.Single(c => c.ClaimType == claim.Type && c.ClaimValue == c.ClaimValue) !=null).Count(), userCount);
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        [DataRow(true, DisplayName = "IncludeRoleProvider")]
        [DataRow(false, DisplayName = "NoRoleProvider")]
        public async Task AddRemoveUserClaim(bool includeRoles)
        {
            UserStore<ApplicationUser, IdentityRole, IdentityCloudContext> store = CreateUserStore(includeRoles);
            UserManager<ApplicationUser> manager = CreateUserManager(includeRoles);

            var user = await CurrentUser(includeRoles);
            WriteLineObject<IdentityUser>(user);
            Claim claim = GenAdminClaim();

            var userClaimTask = await manager.AddClaimAsync(user, claim);

            Assert.IsTrue(userClaimTask.Succeeded, string.Concat(userClaimTask.Errors));

            var claimsTask = await manager.GetClaimsAsync(user);

            Assert.IsTrue(claimsTask.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not found");

            var userRemoveClaimTask = await manager.RemoveClaimAsync(user, claim);

            Assert.IsTrue(userClaimTask.Succeeded, string.Concat(userClaimTask.Errors));
            var claimsTask2 = await manager.GetClaimsAsync(user);

            Assert.IsTrue(!claimsTask2.Any(c => c.Value == claim.Value & c.ValueType == claim.ValueType), "Claim not removed");

            //adding test for removing an empty claim
            Claim claimEmpty = GenAdminClaimEmptyValue();

            var userClaimTask2 = await manager.AddClaimAsync(user, claimEmpty);

            var userRemoveClaimTask2 = await manager.RemoveClaimAsync(user, claimEmpty);

            Assert.IsTrue(userClaimTask2.Succeeded, string.Concat(userClaimTask2.Errors));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.AddClaimsAsync(null, new List<Claim>() { claim }));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async() => await store.AddClaimsAsync(user, null));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await store.RemoveClaimsAsync(null, new List<Claim>() { claim }));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await store.RemoveClaimsAsync(user, null));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await store.RemoveClaimsAsync(user, new List<Claim>() { new Claim(claim.Type, null) }));

            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await store.AddClaimsAsync(null, new List<Claim>() { claim }));
        }

        [TestMethod]
        [TestCategory("UserStore.User")]
        public async Task ThrowIfDisposed()
        {
            var store = new UserStore<ApplicationUser, IdentityRole, IdentityCloudContext>(GetContext(), describer: new IdentityErrorDescriber());
            store.Dispose();
            GC.Collect();

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(async () => await store.DeleteAsync(new ApplicationUser()));
        }

    }
}
