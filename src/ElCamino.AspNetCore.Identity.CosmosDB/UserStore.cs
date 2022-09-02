﻿// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Diagnostics;
using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Newtonsoft.Json;
using System.Threading;
using ElCamino.AspNetCore.Identity.CosmosDB.Extensions;
using System.ComponentModel;
using System.Security.Claims;
using System.Globalization;
using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;
using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos.Scripts;

namespace ElCamino.AspNetCore.Identity.CosmosDB
{

    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    public class UserStore<TUser, TContext>
        : UserStore<TUser, Model.IdentityRole<string>, TContext>
        where TUser : Model.IdentityUser<string>, new()
        where TContext : IdentityCloudContext
    {
        /// <summary>
        /// Constructs a new instance of <see cref="UserStore{TUser, TContext}"/>.
        /// </summary>
        /// <param name="context">The <see cref="IdentityCloudContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }

    }
    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TRole">The type representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>   
    public class UserStore<TUser, TRole, TContext> 
        : UserStore<TUser, TRole, TContext, string, Model.IdentityUserClaim<string>, Model.IdentityUserRole<string>, Model.IdentityUserLogin<string>, Model.IdentityUserToken<string>, Model.IdentityRoleClaim<string>>
        where TUser : Model.IdentityUser<string>, new()
        where TRole : Model.IdentityRole<string>, new()
        where TContext : IdentityCloudContext
    {
        /// <summary>
        /// Constructs a new instance of <see cref="UserStore{TUser, TRole, TContext}"/>.
        /// </summary>
        /// <param name="context">The <see cref="IdentityCloudContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }

        public override async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var userId = user.Id;

            QueryDefinition query = new QueryDefinition("SELECT VALUE r.roleId " +
                "FROM u " +
                "JOIN r in u.roles " +
                "WHERE (u.id = @userid) ").WithParameter("@userid", userId);
            List<String> lroleIds =  await ExecuteSqlQuery<String>(query, Context.QueryOptions)
                                            .ToListAsync(cancellationToken: cancellationToken);
            if (lroleIds.Count > 0)
            {
                QueryDefinition query2 = new QueryDefinition(string.Format("SELECT VALUE r.name " +
                    "FROM r " +
                    "WHERE (r.id in ( {0} )) ", string.Join(",", lroleIds.Select(rn => "'" + rn + "'"))));
                return await ExecuteSqlQuery<String>(query2, Context.QueryOptions)
                    .ToListAsync(cancellationToken: cancellationToken);
            }

            return new List<String>();

        }

        public async override Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            QueryDefinition query = new QueryDefinition("SELECT VALUE u " +
                   "FROM ROOT u " +
                   "JOIN uc in u.claims " +
                   "WHERE (uc.claimValue = @claimValue) AND (uc.claimType = @claimType) ")
                .WithParameter("@claimValue", claim.Value)
                .WithParameter("@claimType", claim.Type);

            Debug.WriteLine(query.QueryText);
            return await ExecuteSqlQuery<TUser>(query, Context.QueryOptions)
                .ToListAsync(cancellationToken: cancellationToken);
        }


        /// <summary>
        /// Retrieves all users in the specified role.
        /// </summary>
        /// <param name="normalizedRoleName">The role whose users should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> contains a list of users, if any, that are in the specified role. 
        /// </returns>
        public override async Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(normalizedRoleName))
            {
                throw new ArgumentNullException(nameof(normalizedRoleName));
            }

            string roleId = await GetRoleIdByNormalizedNameAsync(normalizedRoleName, cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(roleId))
            {
                QueryDefinition query = new QueryDefinition("SELECT VALUE u " +
                    "FROM ROOT u " +
                    "JOIN ur in u.roles " +
                    "WHERE (ur.roleId = @roleId) ")
                    .WithParameter("@roleId", roleId);

                Debug.WriteLine(query.QueryText);
                return await ExecuteSqlQuery<TUser>(query, Context.QueryOptions)
                                .ToListAsync(cancellationToken: cancellationToken);
            }
            return new List<TUser>();
        }

        protected override async Task<Model.IdentityUserToken<string>> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            return await UserTokens.FirstOrDefaultAsync((t) => t.UserId == user.Id && t.LoginProvider == loginProvider && t.Name == name, cancellationToken)
                .ConfigureAwait(false);
        }

        public override async Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));
            }

            var roleEntity = await Roles.SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (roleEntity == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.RoleNotFound, normalizedRoleName));
            }
            user.Roles.Add(CreateUserRole(user, roleEntity));

        }

        protected override Model.IdentityUserRole<string> CreateUserRole(TUser user, TRole role)
        {
            return new Model.IdentityUserRole<string>()
            {
                UserId = user.Id,
                RoleId = role.Id
            };
        }

        protected override async Task<TRole> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            QueryDefinition roleQuery = new QueryDefinition("SELECT VALUE r " +
                            "FROM ROOT r " +
                            "WHERE (r.normalizedName = @normalizedName) ")
                            .WithParameter("@normalizedName", normalizedRoleName);

            Console.WriteLine(roleQuery.QueryText);
            return await ExecuteSqlQuery<TRole>(roleQuery, Context.QueryOptions)
                        .FirstOrDefaultAsync(cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
        }

        protected override async Task<Model.IdentityUserRole<string>> FindUserRoleAsync(string userId, string roleId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (string.IsNullOrEmpty(roleId))
            {
                throw new ArgumentNullException(nameof(roleId));
            }

            QueryDefinition query = new QueryDefinition("SELECT VALUE ur.roleId, ur.userId " +
                    "FROM ROOT u " +
                    "JOIN ur in u.roles " +
                    "WHERE (ur.roleId = @roleId) AND (ur.userId = @userId)")
                .WithParameter("@roleId", roleId)
                .WithParameter("@userId", userId);

                Debug.WriteLine(query.QueryText);
                return await ExecuteSqlQueryFirst<Model.IdentityUserRole<string>>(query, Context.QueryOptions).ConfigureAwait(false);
        }

        protected override Task<TUser> FindUserAsync(string userId, CancellationToken cancellationToken)
        {
            return base.FindByIdAsync(userId, cancellationToken);
        }

        protected override Task<Model.IdentityUserLogin<string>> FindUserLoginAsync(string userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            //not needed right now since overriding FindByLoginAsync
            throw new NotImplementedException();
            //cancellationToken.ThrowIfCancellationRequested();
            //ThrowIfDisposed();
            //if (string.IsNullOrEmpty(userId))
            //{
            //    throw new ArgumentNullException(nameof(userId));
            //}

            //if (string.IsNullOrEmpty(loginProvider))
            //{
            //    throw new ArgumentNullException(nameof(loginProvider));
            //} 

            //if (string.IsNullOrEmpty(providerKey))
            //{
            //    throw new ArgumentNullException(nameof(providerKey));
            //}

            //SqlQuerySpec query = new SqlQuerySpec("SELECT VALUE u.userId, u1.* " +
            //    "FROM ROOT u " +
            //    "JOIN ul in u.logins " +
            //    "WHERE (u.userId = @userId) " +  
            //    " AND (ul.loginProvider = @loginProvider) " +
            //    " AND (ul.providerKey = @providerKey) ", new SqlParameterCollection(){
            //        new SqlParameter("@loginProvider", loginProvider),
            //        new SqlParameter("@providerKey", providerKey),
            //        new SqlParameter("@userId", userId)
            //});

            //Debug.WriteLine(query.QueryText);
            //return await ExecuteSqlQuery<Model.IdentityUserLogin<string>>(query, Context.FeedOptions).SingleOrDefaultAsync();
        }

        protected override Task<Model.IdentityUserLogin<string>> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            //not needed right now since overriding FindByLoginAsync
            throw new NotImplementedException();
            //cancellationToken.ThrowIfCancellationRequested();
            //ThrowIfDisposed();
            //if (string.IsNullOrEmpty(loginProvider))
            //{
            //    throw new ArgumentNullException(nameof(loginProvider));
            //}

            //if (string.IsNullOrEmpty(providerKey))
            //{
            //    throw new ArgumentNullException(nameof(providerKey));
            //}

            //SqlQuerySpec query = new SqlQuerySpec("SELECT VALUE u.userId, u1.* " +
            //    "FROM ROOT u " +
            //    "JOIN ul in u.logins " +
            //    "WHERE  (ul.loginProvider = @loginProvider) " +
            //    " AND (ul.providerKey = @providerKey) ", new SqlParameterCollection(){
            //        new SqlParameter("@loginProvider", loginProvider),
            //        new SqlParameter("@providerKey", providerKey)
            //});

            //Debug.WriteLine(query.QueryText);
            //return await ExecuteSqlQuery<Model.IdentityUserLogin<string>>(query, Context.FeedOptions).SingleOrDefaultAsync();
        }

        protected override async Task AddUserTokenAsync(Model.IdentityUserToken<string> token)
        {
            var doc = await Context.IdentityContainer.CreateItemAsync<Model.IdentityUserToken<string>>(token, new PartitionKey(token.PartitionKey), Context.RequestOptions);
            Context.SetSessionTokenIfEmpty(doc.Headers.Session);
            _ = doc.Resource;
        }

        protected override Model.IdentityUserToken<string> CreateUserToken(TUser user, string loginProvider, string name, string value)
        {
            return new Model.IdentityUserToken()
            {
                UserId = user.Id,
                LoginProvider = loginProvider,
                Name = name,
                Value = value
            };
        }

        protected override async Task RemoveUserTokenAsync(Model.IdentityUserToken<string> token)
        {
            var doc = await Context.IdentityContainer.DeleteItemAsync<Model.IdentityUserToken<string>>(token.Id, new PartitionKey(token.PartitionKey), Context.RequestOptions);
            Context.SetSessionTokenIfEmpty(doc.Headers.Session);
        }
    }



    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TRole">The type representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
    /// <typeparam name="TUserClaim">The type representing a claim.</typeparam>
    /// <typeparam name="TUserRole">The type representing a user role.</typeparam>
    /// <typeparam name="TUserLogin">The type representing a user external login.</typeparam>
    /// <typeparam name="TUserToken">The type representing a user token.</typeparam>
    /// <typeparam name="TRoleClaim">The type representing a role claim.</typeparam>
    public abstract class UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim> :
        Microsoft.AspNetCore.Identity.UserStoreBase<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, TRoleClaim>,
        IUserLoginStore<TUser>,
        IUserRoleStore<TUser>,
        IUserClaimStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserEmailStore<TUser>,
        IUserLockoutStore<TUser>,
        IUserPhoneNumberStore<TUser>,
        IQueryableUserStore<TUser>,
        IUserTwoFactorStore<TUser>,
        IUserAuthenticationTokenStore<TUser>
        where TUser : Model.IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin>, new()
        where TRole : Model.IdentityRole<TKey, TUserRole, TRoleClaim>, new()
        where TContext : IdentityCloudContext
        where TKey : IEquatable<TKey>
        where TUserClaim : Model.IdentityUserClaim<TKey>, new()
        where TUserRole : Model.IdentityUserRole<TKey>, new()
        where TUserLogin : Model.IdentityUserLogin<TKey>, new()
        where TUserToken : Model.IdentityUserToken<TKey>, new()
        where TRoleClaim : Model.IdentityRoleClaim<TKey>, new()
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserStore"/>.
        /// </summary>
        /// <param name="context">The context used to access the store.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/> used to describe store errors.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer = null): base(describer)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            ErrorDescriber = describer ?? new IdentityErrorDescriber();
        }


        /// <summary>
        /// Gets the database context for this store.
        /// </summary>
        public TContext Context { get; private set; }

        internal protected async IAsyncEnumerable<Q> ExecuteSqlQuery<Q>(QueryDefinition sqlQuery, QueryRequestOptions queryOptions = null) where Q : class
        {
            if (queryOptions == null)
            {
                queryOptions = Context.QueryOptions;
            }

            string continuationToken = null;
            do
            {
                FeedIterator<Q> feedIterator = Context.IdentityContainer.GetItemQueryIterator<Q>(sqlQuery, continuationToken: continuationToken, requestOptions: queryOptions);

                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Q> feedResponse = await feedIterator.ReadNextAsync();
                    continuationToken = feedResponse.ContinuationToken;
                    foreach (Q q in feedResponse)
                    {
                        yield return q;
                    }
                }
            } while (continuationToken != null);
        }

        internal protected async Task<Q> ExecuteSqlQueryFirst<Q>(QueryDefinition sqlQuery, QueryRequestOptions queryOptions = null) where Q : class
        {
            if (queryOptions == null)
            {
                queryOptions = Context.QueryOptions;
                queryOptions.MaxConcurrency = 0; //max
                queryOptions.MaxItemCount = 1;
            }

            var feedIterator = Context.IdentityContainer.GetItemQueryIterator<Q>(sqlQuery, requestOptions: queryOptions);

            if (feedIterator.HasMoreResults)
            {
                return (await feedIterator.ReadNextAsync()).FirstOrDefault();
            }

            return null;
        }

        protected IOrderedQueryable<TUser> UsersSet
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<TUser>(true);
            }
        }
        protected IOrderedQueryable<TRole> Roles
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<TRole>(true);
            }
        }
        protected IOrderedQueryable<TUserClaim> UserClaims
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<TUserClaim>(true);
            }
        }

        protected IOrderedQueryable<TUserToken> UserTokens
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<TUserToken>(true);
            }
        }
               

        /// <summary>
        /// Creates the specified <paramref name="user"/> in the user store.
        /// </summary>
        /// <param name="user">The user to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
        public async override Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var doc = await Context.IdentityContainer.CreateItemAsync<TUser>(user, new PartitionKey(user.PartitionKey), Context.RequestOptions);
            Context.SetSessionTokenIfEmpty(doc.Headers.Session);
            _ = doc.Resource;
            return IdentityResult.Success;
        }

        /// <summary>
        /// Updates the specified <paramref name="user"/> in the user store.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the update operation.</returns>
        public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            try
            {
                ItemRequestOptions ro = Context.RequestOptions;
                //TODO: Investigate why UserManager is updating twice with different ETag
                //ro.IfMatchEtag = user.ETag;

                var doc = await Context.IdentityContainer.ReplaceItemAsync<TUser>(user, user.Id.ToString(), new PartitionKey(user.PartitionKey), ro);
                Context.SetSessionTokenIfEmpty(doc.Headers.Session);
                user = doc.Resource;
                return IdentityResult.Success;
            }
            catch (CosmosException dc) 
            {
                return ConcurrencyCheckResultFailed(dc);
            }

        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/rest/api/documentdb/http-status-codes-for-documentdb
        /// </summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        protected IdentityResult ConcurrencyCheckResultFailed(CosmosException dc)
        {
            if (dc.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return IdentityResult.Failed(ErrorDescriber.ConcurrencyFailure());
            }

            return IdentityResult.Failed();
        }

        /// <summary>
        /// Deletes the specified <paramref name="user"/> from the user store.
        /// </summary>
        /// <param name="user">The user to delete.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the update operation.</returns>
        public override async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                var doc = await Context.IdentityContainer.DeleteItemAsync<TUser>(user.Id.ToString(), new PartitionKey(user.PartitionKey), Context.RequestOptions);
                Context.SetSessionTokenIfEmpty(doc.Headers.Session);
            }
            catch (CosmosException dc)
            {
                return ConcurrencyCheckResultFailed(dc);
            }
            return IdentityResult.Success;
        }

        /// <summary>
        /// Finds and returns a user, if any, who has the specified <paramref name="userId"/>.
        /// </summary>
        /// <param name="userId">The user ID to search for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="userId"/> if it exists.
        /// </returns>
        public override async Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            StoredProcedureExecuteResponse<string> response = await Context.IdentityContainer.Scripts.ExecuteStoredProcedureAsync<string>(
                Context.GetUserByIdSproc,
                new PartitionKey(PartitionKeyHelper.GetPartitionKeyFromId(userId)),
                new dynamic[] { userId });

            return !string.IsNullOrWhiteSpace(response.Resource) ?
                JsonConvert.DeserializeObject<TUser>(response.Resource) : null;
            
        }

        /// <summary>
        /// Finds and returns a user, if any, who has the specified normalized user name.
        /// </summary>
        /// <param name="normalizedUserName">The normalized user name to search for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="normalizedUserName"/> if it exists.
        /// </returns>
        public override async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            //TODO: Deprecate Stored Proc: getUserByUserName_v1
            QueryDefinition query = new QueryDefinition("SELECT * FROM root r " +
                "WHERE (r.normalizedUserName = @normalizedName) ")
                .WithParameter("@normalizedName", normalizedUserName);

            Console.WriteLine(query.QueryText);
            return await ExecuteSqlQueryFirst<TUser>(query, Context.QueryOptions)
                                .ConfigureAwait(false);
        }

        /// <summary>
        /// A navigation property for the users the store contains.
        /// </summary>
        public override IQueryable<TUser> Users
        {
            get { return UsersSet; }
        }

        /// <summary>
        /// Removes the given <paramref name="normalizedRoleName"/> from the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the role from.</param>
        /// <param name="normalizedRoleName">The role to remove.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));
            }
            var roleEntity = await Roles.SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
            if (roleEntity != null)
            {
                var userRole = user.Roles.FirstOrDefault(r => r.RoleId.Equals(roleEntity.Id) && r.UserId.Equals(user.Id));
                if (userRole != null)
                {
                    user.Roles.Remove(userRole);
                }
            }
            //Update user is called by UserManager
        }


        /// <summary>
        /// Returns a flag indicating if the specified user is a member of the give <paramref name="normalizedRoleName"/>.
        /// </summary>
        /// <param name="user">The user whose role membership should be checked.</param>
        /// <param name="normalizedRoleName">The role to check membership of</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> containing a flag indicating if the specified user is a member of the given group. If the 
        /// user is a member of the group the returned value with be true, otherwise it will be false.</returns>
        public override async Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));
            }

            string roleId = await GetRoleIdByNormalizedNameAsync(normalizedRoleName, cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(roleId))
            {
                var userRole = user.Roles.FirstOrDefault(ur => ur.RoleId.Equals(roleId) && ur.UserId.Equals(user.Id));
                return userRole != null;
            }
            return false;
        }

        protected async Task<string> GetRoleIdByNormalizedNameAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            QueryDefinition roleQuery = new QueryDefinition("SELECT VALUE r.id " +
                "FROM ROOT r " +
                "WHERE (r.normalizedName = @normalizedName) ")
                .WithParameter("@normalizedName", normalizedRoleName);

            Console.WriteLine(roleQuery.QueryText);
            var roleIds = await ExecuteSqlQuery<String>(roleQuery, Context.QueryOptions)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return roleIds.FirstOrDefault();
        }

        /// <summary>
        /// Dispose the store
        /// </summary>
        public new void Dispose()
        {
            base.Dispose();
            //_disposed = true;
        }

        /// <summary>
        /// Get the claims associated with the specified <paramref name="user"/> as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose claims should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the claims granted to a user.</returns>
        public override async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await user.Claims.Where(uc => uc.UserId.Equals(user.Id)).Select(c => c.ToClaim())
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Adds the <paramref name="claims"/> given to the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the claim to.</param>
        /// <param name="claims">The claim to add to the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
            foreach (var claim in claims)
            {
                user.Claims.Add(CreateUserClaim(user, claim));
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Replaces the <paramref name="claim"/> on the specified <paramref name="user"/>, with the <paramref name="newClaim"/>.
        /// </summary>
        /// <param name="user">The user to replace the claim on.</param>
        /// <param name="claim">The claim replace.</param>
        /// <param name="newClaim">The new claim replacing the <paramref name="claim"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (newClaim == null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }

            var matchedClaims = await UserClaims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            foreach (var matchedClaim in matchedClaims)
            {
                matchedClaim.ClaimValue = newClaim.Value;
                matchedClaim.ClaimType = newClaim.Type;
            }
        }

        /// <summary>
        /// Removes the <paramref name="claims"/> given from the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the claims from.</param>
        /// <param name="claims">The claim to remove.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
            foreach (var claim in claims)
            {
                var matchedClaims = await user.Claims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type)
                    .AsQueryable()
                    .ToListAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                foreach (var c in matchedClaims)
                {
                    user.Claims.Remove(c);
                }
            }
        }

        /// <summary>
        /// Adds the <paramref name="login"/> given to the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the login to.</param>
        /// <param name="login">The login to add to the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override Task AddLoginAsync(TUser user, UserLoginInfo login,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }
            user.Logins.Add(CreateUserLogin(user, login));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes the <paramref name="loginProvider"/> given from the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the login from.</param>
        /// <param name="loginProvider">The login to remove from the user.</param>
        /// <param name="providerKey">The key provided by the <paramref name="loginProvider"/> to identify a user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var entry = user.Logins.SingleOrDefault(userLogin => userLogin.UserId.Equals(user.Id) && userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey);
            if (entry != null)
            {
                user.Logins.Remove(entry);
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves the associated logins for the specified <param ref="user"/>.
        /// </summary>
        /// <param name="user">The user whose associated logins to retrieve.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> for the asynchronous operation, containing a list of <see cref="UserLoginInfo"/> for the specified <paramref name="user"/>, if any.
        /// </returns>
        public override async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var userId = user.Id;
            return await user.Logins.Where(l => l.UserId.Equals(userId))
                .Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName))
                .ToListAsync(cancellationToken:cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves the user associated with the specified login provider and login provider key..
        /// Overrides base class for performance.
        /// </summary>
        /// <param name="loginProvider">The login provider who provided the <paramref name="providerKey"/>.</param>
        /// <param name="providerKey">The key provided by the <paramref name="loginProvider"/> to identify a user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> for the asynchronous operation, containing the user, if any which matched the specified login provider and key.
        /// </returns>
        public override async Task<TUser> FindByLoginAsync(string loginProvider, string providerKey,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            //TODO: Deprecate Stored Proc: getUserByLogin_v1
            QueryDefinition query = new QueryDefinition("SELECT VALUE r FROM root r JOIN l IN r.logins " +
                "WHERE l.loginProvider = @loginProvider " +
                "AND l.providerKey = @providerKey ")
                .WithParameter("@loginProvider", loginProvider)
                .WithParameter("@providerKey", providerKey);

            Console.WriteLine(query.QueryText);
            return await ExecuteSqlQueryFirst<TUser>(query, Context.QueryOptions)
                                .ConfigureAwait(false);
        }
     

        /// <summary>
        /// Gets the user, if any, associated with the specified, normalized email address.
        /// </summary>
        /// <param name="normalizedEmail">The normalized email address to return the user for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The task object containing the results of the asynchronous lookup operation, the user if any associated with the specified normalized email address.
        /// </returns>
        public override async Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            //TODO: Deprecate Stored Proc: getUserByEmail_v1
            QueryDefinition query = new QueryDefinition("SELECT * FROM root r " +
                "WHERE (r.normalizedEmail = @normalizedEmail) ")
                .WithParameter("@normalizedEmail", normalizedEmail);

            Console.WriteLine(query.QueryText);
            return await ExecuteSqlQueryFirst<TUser>(query, Context.QueryOptions)
                                .ConfigureAwait(false);
        }

        public async virtual Task<IList<TUser>> FindAllByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            
            QueryDefinition query = new QueryDefinition("SELECT * FROM root r " +
                "WHERE (r.normalizedEmail = @normalizedEmail) ")
                .WithParameter("@normalizedEmail", normalizedEmail);

            Console.WriteLine(query.QueryText);
            return await ExecuteSqlQuery<TUser>(query, Context.QueryOptions)
                                .ToListAsync(cancellationToken: cancellationToken)
                                .ConfigureAwait(false);
        }
    }
}
