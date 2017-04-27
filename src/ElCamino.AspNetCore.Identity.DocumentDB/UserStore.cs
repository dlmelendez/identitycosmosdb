// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.Net;
using System.Diagnostics;
using ElCamino.AspNetCore.Identity.DocumentDB.Model;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System.Threading;
using ElCamino.AspNetCore.Identity.DocumentDB.Extensions;
using System.ComponentModel;
using System.Security.Claims;
using System.Globalization;
using Microsoft.Azure.Documents.Client;
using ElCamino.AspNetCore.Identity.DocumentDB.Helpers;
using System.Collections.Concurrent;

namespace ElCamino.AspNetCore.Identity.DocumentDB
{
    /// <summary>
    /// Represents a new instance of a persistence store for users, using the default implementation
    /// of <see cref="IdentityUser{TKey}"/> with a string as a primary key.
    /// </summary>
    public class UserStore : UserStore<IdentityUser<string>>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="UserStore"/>.
        /// </summary>
        /// <param name="context">The <see cref="IdentityCloudContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public UserStore(IdentityCloudContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }
    }

    /// <summary>
    /// Creates a new instance of a persistence store for the specified user type.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    public class UserStore<TUser> : UserStore<TUser, IdentityRole, IdentityCloudContext, string>
        where TUser : IdentityUser<string>, new()
    {
        /// <summary>
        /// Constructs a new instance of <see cref="UserStore{TUser}"/>.
        /// </summary>
        /// <param name="context">The <see cref="IdentityCloudContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public UserStore(IdentityCloudContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }
    }

    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TRole">The type representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    public class UserStore<TUser, TRole, TContext> : UserStore<TUser, TRole, TContext, string>
        where TUser : IdentityUser<string>, new()
        where TRole : IdentityRole<string>, new()
        where TContext : IdentityCloudContext
    {
        /// <summary>
        /// Constructs a new instance of <see cref="UserStore{TUser, TRole, TContext}"/>.
        /// </summary>
        /// <param name="context">The <see cref="IdentityCloudContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }

        public override async Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var userId = user.Id;
            
            SqlQuerySpec query = new SqlQuerySpec("SELECT VALUE r.roleId " +
                "FROM u " +
                "JOIN r in u.roles " +
                "WHERE (u.id = @userid) ", new SqlParameterCollection(){
             new SqlParameter("@userid", userId)
            });

            List<String> lroleIds =  await ExecuteSqlQuery<String>(query, Context.FeedOptions).ToListAsync(cancellationToken: cancellationToken);

            if (lroleIds.Count > 0)
            {
                SqlQuerySpec query2 = new SqlQuerySpec(string.Format("SELECT VALUE r.name " +
                    "FROM r " +
                    "WHERE (r.id in ( {0} )) ", string.Join(",", lroleIds.Select(rn => "'" + rn + "'"))));

                return await ExecuteSqlQuery<String>(query2, Context.FeedOptions).ToListAsync(cancellationToken: cancellationToken);
            }

            return new List<String>();

        }

        public async override Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            SqlQuerySpec query = new SqlQuerySpec("SELECT VALUE u " +
                   "FROM ROOT u " +
                   "JOIN uc in u.claims " +
                   "WHERE (uc.claimValue = @claimValue) AND (uc.claimType = @claimType) ", new SqlParameterCollection(){
                    new SqlParameter("@claimValue", claim.Value),
                    new SqlParameter("@claimType", claim.Type)
               });

            Debug.WriteLine(query.QueryText);
            return await ExecuteSqlQuery<TUser>(query, Context.FeedOptions).ToListAsync(cancellationToken: cancellationToken);
        }


        /// <summary>
        /// Retrieves all users in the specified role.
        /// </summary>
        /// <param name="normalizedRoleName">The role whose users should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> contains a list of users, if any, that are in the specified role. 
        /// </returns>
        public override async Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
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
                SqlQuerySpec query = new SqlQuerySpec("SELECT VALUE u " +
                    "FROM ROOT u " +
                    "JOIN ur in u.roles " +
                    "WHERE (ur.roleId = @roleId) ", new SqlParameterCollection(){
                    new SqlParameter("@roleId", roleId)
                });

                Debug.WriteLine(query.QueryText);
                return await ExecuteSqlQuery<TUser>(query, Context.FeedOptions).ToListAsync(cancellationToken: cancellationToken);
            }
            return new List<TUser>();
        }

        internal protected override Task<IdentityUserToken<string>> FindToken(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
    => UserTokens.FirstOrDefaultAsync((t) => t.UserId == user.Id && t.LoginProvider == loginProvider && t.Name == name, cancellationToken);

        public override async Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
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

            var roleEntity = await Roles.SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken: cancellationToken);
            if (roleEntity == null)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.RoleNotFound, normalizedRoleName));
            }
            //UserRoles.Add(CreateUserRole(user, roleEntity));
            user.Roles.Add(CreateUserRole(user, roleEntity));
        }
    }

    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TRole">The type representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for a role.</typeparam>
    public class UserStore<TUser, TRole, TContext, TKey> : UserStore<TUser, TRole, TContext, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityUserToken<TKey>>
        where TUser : IdentityUser<TKey>, new()
        where TRole : IdentityRole<TKey>, new()
        where TContext : IdentityCloudContext
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Constructs a new instance of <see cref="UserStore{TUser, TRole, TContext, TKey}"/>.
        /// </summary>
        /// <param name="context">The <see cref="IdentityCloudContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }

        /// <summary>
        /// Called to create a new instance of a <see cref="IdentityUserRole{TKey}"/>.
        /// </summary>
        /// <param name="user">The associated user.</param>
        /// <param name="role">The associated role.</param>
        /// <returns></returns>
        protected override IdentityUserRole<TKey> CreateUserRole(TUser user, TRole role)
        {
            return new IdentityUserRole<TKey>()
            {
                UserId = user.Id,
                RoleId = role.Id
            };
        }

        /// <summary>
        /// Called to create a new instance of a <see cref="IdentityUserClaim{TKey}"/>.
        /// </summary>
        /// <param name="user">The associated user.</param>
        /// <param name="claim">The associated claim.</param>
        /// <returns></returns>
        protected override IdentityUserClaim<TKey> CreateUserClaim(TUser user, Claim claim)
        {
            var userClaim = new IdentityUserClaim<TKey> { UserId = user.Id };
            userClaim.InitializeFromClaim(claim);
            return userClaim;
        }

        /// <summary>
        /// Called to create a new instance of a <see cref="IdentityUserLogin{TKey}"/>.
        /// </summary>
        /// <param name="user">The associated user.</param>
        /// <param name="login">The sasociated login.</param>
        /// <returns></returns>
        protected override IdentityUserLogin<TKey> CreateUserLogin(TUser user, UserLoginInfo login)
        {
            return new IdentityUserLogin<TKey>
            {
                UserId = user.Id,
                ProviderKey = login.ProviderKey,
                LoginProvider = login.LoginProvider,
                ProviderDisplayName = login.ProviderDisplayName
            };
        }

        /// <summary>
        /// Called to create a new instance of a <see cref="IdentityUserToken{TKey}"/>.
        /// </summary>
        /// <param name="user">The associated user.</param>
        /// <param name="loginProvider">The associated login provider.</param>
        /// <param name="name">The name of the user token.</param>
        /// <param name="value">The value of the user token.</param>
        /// <returns></returns>
        protected override IdentityUserToken<TKey> CreateUserToken(TUser user, string loginProvider, string name, string value)
        {
            return new IdentityUserToken<TKey>
            {
                UserId = user.Id,
                LoginProvider = loginProvider,
                Name = name,
                Value = value
            };
        }       

        internal protected override Task<IdentityUserToken<TKey>> FindToken(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
    public abstract class UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken> :
        UserStore<TUser, TRole, TContext, TKey, TUserClaim, TUserRole, TUserLogin, TUserToken, IdentityRoleClaim<TKey>>
        where TUser : IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin>, new()
        where TRole : IdentityRole<TKey, TUserRole, IdentityRoleClaim<TKey>>, new()
        where TContext : IdentityCloudContext
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>, new()
        where TUserRole : IdentityUserRole<TKey>, new()
        where TUserLogin : IdentityUserLogin<TKey>, new()
        where TUserToken : IdentityUserToken<TKey>, new()
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserStore"/>.
        /// </summary>
        /// <param name="context">The context used to access the store.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/> used to describe store errors.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }
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
        where TUser : IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin>, new()
        where TRole : IdentityRole<TKey, TUserRole, TRoleClaim>, new()
        where TContext : IdentityCloudContext
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>, new()
        where TUserRole : IdentityUserRole<TKey>, new()
        where TUserLogin : IdentityUserLogin<TKey>, new()
        where TUserToken : IdentityUserToken<TKey>, new()
        where TRoleClaim : IdentityRoleClaim<TKey>, new()
    {
        /// <summary>
        /// Creates a new instance of <see cref="UserStore"/>.
        /// </summary>
        /// <param name="context">The context used to access the store.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/> used to describe store errors.</param>
        public UserStore(TContext context, IdentityErrorDescriber describer = null)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            ErrorDescriber = describer ?? new IdentityErrorDescriber();
        }

        private bool _disposed;

        /// <summary>
        /// Gets the database context for this store.
        /// </summary>
        public TContext Context { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="IdentityErrorDescriber"/> for any error that occurred with the current operation.
        /// </summary>
        public IdentityErrorDescriber ErrorDescriber { get; set; }

        internal protected IEnumerable<Q> ExecuteSqlQuery<Q>(SqlQuerySpec sqlQuery, FeedOptions feedOptions = null) where Q : class
        {
            if(feedOptions == null)
            {
                feedOptions = Context.FeedOptions;
            }

            var results = Context.Client.CreateDocumentQuery(Context.IdentityDocumentCollection.DocumentsLink, sqlQuery,
                Context.FeedOptions);
            foreach (dynamic d in results.ToList())
            {
                Q q = d;
                //Q q = JsonHelpers.CreateObject<Q>( d.ToString());
                yield return q;
            }
        }
        protected IOrderedQueryable<TUser> UsersSet
        {
            get
            {
                return Context.Client.CreateDocumentQuery<TUser>(Context.IdentityDocumentCollection.DocumentsLink,
                 Context.FeedOptions);
            }
        }
        protected IOrderedQueryable<TRole> Roles
        {
            get
            {
                return Context.Client.CreateDocumentQuery<TRole>(Context.IdentityDocumentCollection.DocumentsLink, 
                 Context.FeedOptions);
            }
        }
        protected IOrderedQueryable<TUserClaim> UserClaims
        {
            get
            {
                return Context.Client.CreateDocumentQuery<TUserClaim>(Context.IdentityDocumentCollection.DocumentsLink,
                 Context.FeedOptions);
            }
        }
        //protected IQueryable<TUserRole> UserRoles
        //{
        //    get
        //    {
        //        return Context.Client.CreateDocumentQuery<TUserRole>(Context.IdentityDocumentCollection.DocumentsLink,
        //         Context.FeedOptions);
        //    }
        //}
        //private IQueryable<TUserLogin> UserLogins
        //{
        //    get
        //    {
        //        return Context.Client.CreateDocumentQuery<TUserLogin>(Context.IdentityDocumentCollection.DocumentsLink,
        //         Context.FeedOptions);
        //    }
        //}
        protected IOrderedQueryable<TUserToken> UserTokens
        {
            get
            {
                return Context.Client.CreateDocumentQuery<TUserToken>(Context.IdentityDocumentCollection.DocumentsLink,
                 Context.FeedOptions);
            }
        }

        /// <summary>
        /// Creates a new entity to represent a user role.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="role"></param>
        /// <returns></returns>
        protected abstract TUserRole CreateUserRole(TUser user, TRole role);

        /// <summary>
        /// Create a new entity representing a user claim.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="claim"></param>
        /// <returns></returns>
        protected abstract TUserClaim CreateUserClaim(TUser user, Claim claim);

        /// <summary>
        /// Create a new entity representing a user login.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="login"></param>
        /// <returns></returns>
        protected abstract TUserLogin CreateUserLogin(TUser user, UserLoginInfo login);

        /// <summary>
        /// Create a new entity representing a user token.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="loginProvider"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected abstract TUserToken CreateUserToken(TUser user, string loginProvider, string name, string value);

        /// <summary>
        /// Gets the user identifier for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose identifier should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the identifier for the specified <paramref name="user"/>.</returns>
        public virtual Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(ConvertIdToString(user.Id));
        }

        /// <summary>
        /// Gets the user name for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose name should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the name for the specified <paramref name="user"/>.</returns>
        public virtual Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.UserName);
        }

        /// <summary>
        /// Sets the given <paramref name="userName" /> for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose name should be set.</param>
        /// <param name="userName">The user name to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.UserName = userName;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Gets the normalized user name for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose normalized name should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the normalized user name for the specified <paramref name="user"/>.</returns>
        public virtual Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.NormalizedUserName);
        }

        /// <summary>
        /// Sets the given normalized name for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose name should be set.</param>
        /// <param name="normalizedName">The normalized name to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.NormalizedUserName = normalizedName;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Creates the specified <paramref name="user"/> in the user store.
        /// </summary>
        /// <param name="user">The user to create.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the creation operation.</returns>
        public async virtual Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var doc = await Context.Client.CreateDocumentAsync(Context.IdentityDocumentCollection.DocumentsLink, user
                        , Context.RequestOptions, true);
            Context.SetSessionTokenIfEmpty(doc.SessionToken);
            user = JsonHelpers.CreateObject<TUser>(doc.Resource.ToString());
            return IdentityResult.Success;
        }

        /// <summary>
        /// Updates the specified <paramref name="user"/> in the user store.
        /// </summary>
        /// <param name="user">The user to update.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the <see cref="IdentityResult"/> of the update operation.</returns>
        public async virtual Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
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
                AccessCondition ac = new AccessCondition { Condition = user.ETag, Type = AccessConditionType.IfMatch };
                RequestOptions ro = Context.RequestOptions;
                //ro.AccessCondition = ac;
                var doc = await Context.Client.ReplaceDocumentAsync(user.SelfLink, user, ro);
                Context.SetSessionTokenIfEmpty(doc.SessionToken);
                user = JsonHelpers.CreateObject<TUser>(doc.Resource.ToString());
                return IdentityResult.Success;
            }
            catch (DocumentClientException dc) 
            {
                return ConcurrencyCheckResultFailed(dc);
            }

        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/rest/api/documentdb/http-status-codes-for-documentdb
        /// </summary>
        /// <param name="dc"></param>
        /// <returns></returns>
        protected IdentityResult ConcurrencyCheckResultFailed(DocumentClientException dc)
        {
            if (dc.StatusCode.HasValue && dc.StatusCode.Value == HttpStatusCode.PreconditionFailed)
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
        public async virtual Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                var doc = await Context.Client.DeleteDocumentAsync(user.SelfLink,
                    Context.RequestOptions);
                Context.SetSessionTokenIfEmpty(doc.SessionToken);
            }
            catch (DocumentClientException dc)
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
        public async virtual Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return await Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByIdSproc.SelfLink,
                Context.RequestOptions,
                new dynamic[] { userId })
                .ContinueWith((storedProcTask) => {
                    Context.SetSessionTokenIfEmpty(storedProcTask.Result.SessionToken);
                    return GetUserAggregate(storedProcTask.Result.Response.ToList());
                });

        }

        private TUser GetUserAggregate(List<dynamic> userResults)
        {
            TUser user = default(TUser);
            var vUser = userResults.Where(u => u.id == u.id).SingleOrDefault();

            if (vUser != null)
            {
                user = JsonHelpers.CreateObject<TUser>(vUser.ToString());
            }
            return user;
        }


        /// <summary>
        /// Converts the provided <paramref name="id"/> to a strongly typed key object.
        /// </summary>
        /// <param name="id">The id to convert.</param>
        /// <returns>An instance of <typeparamref name="TKey"/> representing the provided <paramref name="id"/>.</returns>
        public virtual TKey ConvertIdFromString(string id)
        {
            if (id == null)
            {
                return default(TKey);
            }
            return (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(id);
        }

        /// <summary>
        /// Converts the provided <paramref name="id"/> to its string representation.
        /// </summary>
        /// <param name="id">The id to convert.</param>
        /// <returns>An <see cref="string"/> representation of the provided <paramref name="id"/>.</returns>
        public virtual string ConvertIdToString(TKey id)
        {
            if (object.Equals(id, default(TKey)))
            {
                return null;
            }
            return id.ToString();
        }

        /// <summary>
        /// Finds and returns a user, if any, who has the specified normalized user name.
        /// </summary>
        /// <param name="normalizedUserName">The normalized user name to search for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing the user matching the specified <paramref name="normalizedUserName"/> if it exists.
        /// </returns>
        public async virtual Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return await Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByUserNameSproc.SelfLink,
                                Context.RequestOptions,
                                new dynamic[] { normalizedUserName }).ContinueWith((storedProcTask) => {
                                    Context.SetSessionTokenIfEmpty(storedProcTask.Result.SessionToken);
                                    return GetUserAggregate(storedProcTask.Result.Response.ToList());
                                });
        }

        /// <summary>
        /// A navigation property for the users the store contains.
        /// </summary>
        public virtual IQueryable<TUser> Users
        {
            get { return UsersSet; }
        }

        /// <summary>
        /// Sets the password hash for a user.
        /// </summary>
        /// <param name="user">The user to set the password hash for.</param>
        /// <param name="passwordHash">The password hash to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.PasswordHash = passwordHash;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Gets the password hash for a user.
        /// </summary>
        /// <param name="user">The user to retrieve the password hash for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the password hash for the user.</returns>
        public virtual Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.PasswordHash);
        }

        /// <summary>
        /// Returns a flag indicating if the specified user has a password.
        /// </summary>
        /// <param name="user">The user to retrieve the password hash for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> containing a flag indicating if the specified user has a password. If the 
        /// user has a password the returned value with be true, otherwise it will be false.</returns>
        public virtual Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));
        }

        /// <summary>
        /// Adds the given <paramref name="normalizedRoleName"/> to the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the role to.</param>
        /// <param name="normalizedRoleName">The role to add.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();            
        }

        /// <summary>
        /// Removes the given <paramref name="normalizedRoleName"/> from the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the role from.</param>
        /// <param name="normalizedRoleName">The role to remove.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async virtual Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
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
        /// Retrieves the roles the specified <paramref name="user"/> is a member of.
        /// </summary>
        /// <param name="user">The user whose roles should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the roles the user is a member of.</returns>
        public virtual Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();            
        }

        /// <summary>
        /// Returns a flag indicating if the specified user is a member of the give <paramref name="normalizedRoleName"/>.
        /// </summary>
        /// <param name="user">The user whose role membership should be checked.</param>
        /// <param name="normalizedRoleName">The role to check membership of</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> containing a flag indicating if the specified user is a member of the given group. If the 
        /// user is a member of the group the returned value with be true, otherwise it will be false.</returns>
        public virtual async Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
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

        protected async Task<string> GetRoleIdByNormalizedNameAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            SqlQuerySpec roleQuery = new SqlQuerySpec("SELECT VALUE r.id " +
                "FROM ROOT r " +
                "WHERE (r.normalizedName = @normalizedName) ", new SqlParameterCollection(){
                            new SqlParameter("@normalizedName", normalizedRoleName)
            });

            Console.WriteLine(roleQuery.QueryText);
            var roleIds = await ExecuteSqlQuery<String>(roleQuery, Context.FeedOptions).ToListAsync(cancellationToken: cancellationToken);
            return roleIds.FirstOrDefault();
        }

        /// <summary>
        /// Throws if this class has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Dispose the store
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
        }

        /// <summary>
        /// Get the claims associated with the specified <paramref name="user"/> as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose claims should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that contains the claims granted to a user.</returns>
        public async virtual Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return await user.Claims.Where(uc => uc.UserId.Equals(user.Id)).Select(c => c.ToClaim()).ToListAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Adds the <paramref name="claims"/> given to the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to add the claim to.</param>
        /// <param name="claims">The claim to add to the user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
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
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Replaces the <paramref name="claim"/> on the specified <paramref name="user"/>, with the <paramref name="newClaim"/>.
        /// </summary>
        /// <param name="user">The user to replace the claim on.</param>
        /// <param name="claim">The claim replace.</param>
        /// <param name="newClaim">The new claim replacing the <paramref name="claim"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async virtual Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
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

            var matchedClaims = await UserClaims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToListAsync(cancellationToken: cancellationToken);
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
        public async virtual Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
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
                var matchedClaims = await user.Claims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).AsQueryable().ToListAsync(cancellationToken: cancellationToken);
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
        public virtual Task AddLoginAsync(TUser user, UserLoginInfo login,
            CancellationToken cancellationToken = default(CancellationToken))
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
            return Task.FromResult(false);
        }

        /// <summary>
        /// Removes the <paramref name="loginProvider"/> given from the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user to remove the login from.</param>
        /// <param name="loginProvider">The login to remove from the user.</param>
        /// <param name="providerKey">The key provided by the <paramref name="loginProvider"/> to identify a user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey,
            CancellationToken cancellationToken = default(CancellationToken))
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
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Retrieves the associated logins for the specified <param ref="user"/>.
        /// </summary>
        /// <param name="user">The user whose associated logins to retrieve.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> for the asynchronous operation, containing a list of <see cref="UserLoginInfo"/> for the specified <paramref name="user"/>, if any.
        /// </returns>
        public async virtual Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var userId = user.Id;
            return await user.Logins.Where(l => l.UserId.Equals(userId))
                .Select(l => new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)).ToListAsync(cancellationToken:cancellationToken);
        }

        /// <summary>
        /// Retrieves the user associated with the specified login provider and login provider key..
        /// </summary>
        /// <param name="loginProvider">The login provider who provided the <paramref name="providerKey"/>.</param>
        /// <param name="providerKey">The key provided by the <paramref name="loginProvider"/> to identify a user.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> for the asynchronous operation, containing the user, if any which matched the specified login provider and key.
        /// </returns>
        public async virtual Task<TUser> FindByLoginAsync(string loginProvider, string providerKey,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            //var userLogin = await UserLogins.FindAsync(new object[] { loginProvider, providerKey }, cancellationToken);
            //if (userLogin != null)
            //{
            //    return await Users.FirstOrDefaultAsync(u => u.Id.Equals(userLogin.UserId), cancellationToken);
            //}
            var result = await Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByLoginSproc.SelfLink,
                    Context.RequestOptions,
                    new dynamic[] { loginProvider, providerKey });
            Context.SetSessionTokenIfEmpty(result.SessionToken);
            return GetUserAggregate(result.Response.ToList());
        }

        /// <summary>
        /// Gets a flag indicating whether the email address for the specified <paramref name="user"/> has been verified, true if the email address is verified otherwise
        /// false.
        /// </summary>
        /// <param name="user">The user whose email confirmation status should be returned.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The task object containing the results of the asynchronous operation, a flag indicating whether the email address for the specified <paramref name="user"/>
        /// has been confirmed or not.
        /// </returns>
        public virtual Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.EmailConfirmed);
        }

        /// <summary>
        /// Sets the flag indicating whether the specified <paramref name="user"/>'s email address has been confirmed or not.
        /// </summary>
        /// <param name="user">The user whose email confirmation status should be set.</param>
        /// <param name="confirmed">A flag indicating if the email address has been confirmed, true if the address is confirmed otherwise false.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.EmailConfirmed = confirmed;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Sets the <paramref name="email"/> address for a <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose email should be set.</param>
        /// <param name="email">The email to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.Email = email;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Gets the email address for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose email should be returned.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The task object containing the results of the asynchronous operation, the email address for the specified <paramref name="user"/>.</returns>
        public virtual Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.Email);
        }

        /// <summary>
        /// Returns the normalized email for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose email address to retrieve.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The task object containing the results of the asynchronous lookup operation, the normalized email address if any associated with the specified user.
        /// </returns>
        public virtual Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.NormalizedEmail);
        }

        /// <summary>
        /// Sets the normalized email for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose email address to set.</param>
        /// <param name="normalizedEmail">The normalized email to set for the specified <paramref name="user"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public virtual Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.NormalizedEmail = normalizedEmail;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Gets the user, if any, associated with the specified, normalized email address.
        /// </summary>
        /// <param name="normalizedEmail">The normalized email address to return the user for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The task object containing the results of the asynchronous lookup operation, the user if any associated with the specified normalized email address.
        /// </returns>
        public virtual async Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return await Context.Client.ExecuteStoredProcedureAsync<IEnumerable<dynamic>>(Context.GetUserByEmailSproc.SelfLink,
                                Context.RequestOptions,
                                new dynamic[] { normalizedEmail }).ContinueWith((storedProcTask) => {
                                    Context.SetSessionTokenIfEmpty(storedProcTask.Result.SessionToken);
                                    return GetUserAggregate(storedProcTask.Result.Response.ToList());
                                });
        }

        public async virtual Task<IList<TUser>> FindAllByEmailAsync(string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return await Users.ToListAsync(where:u => u.Email == email, cancellationToken: cancellationToken);
        }



        /// <summary>
        /// Gets the last <see cref="DateTimeOffset"/> a user's last lockout expired, if any.
        /// Any time in the past should be indicates a user is not locked out.
        /// </summary>
        /// <param name="user">The user whose lockout date should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the result of the asynchronous query, a <see cref="DateTimeOffset"/> containing the last time
        /// a user's lockout expired, if any.
        /// </returns>
        public virtual Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.LockoutEnd);
        }

        /// <summary>
        /// Locks out a user until the specified end date has passed. Setting a end date in the past immediately unlocks a user.
        /// </summary>
        /// <param name="user">The user whose lockout date should be set.</param>
        /// <param name="lockoutEnd">The <see cref="DateTimeOffset"/> after which the <paramref name="user"/>'s lockout should end.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.LockoutEnd = lockoutEnd;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Records that a failed access has occurred, incrementing the failed access count.
        /// </summary>
        /// <param name="user">The user whose cancellation count should be incremented.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the incremented failed access count.</returns>
        public virtual Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.AccessFailedCount++;
            return Task.FromResult(user.AccessFailedCount);
        }

        /// <summary>
        /// Resets a user's failed access count.
        /// </summary>
        /// <param name="user">The user whose failed access count should be reset.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        /// <remarks>This is typically called after the account is successfully accessed.</remarks>
        public virtual Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.AccessFailedCount = 0;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Retrieves the current failed access count for the specified <paramref name="user"/>..
        /// </summary>
        /// <param name="user">The user whose failed access count should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the failed access count.</returns>
        public virtual Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.AccessFailedCount);
        }

        /// <summary>
        /// Retrieves a flag indicating whether user lockout can enabled for the specified user.
        /// </summary>
        /// <param name="user">The user whose ability to be locked out should be returned.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, true if a user can be locked out, otherwise false.
        /// </returns>
        public virtual Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.LockoutEnabled);
        }

        /// <summary>
        /// Set the flag indicating if the specified <paramref name="user"/> can be locked out..
        /// </summary>
        /// <param name="user">The user whose ability to be locked out should be set.</param>
        /// <param name="enabled">A flag indicating if lock out can be enabled for the specified <paramref name="user"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.LockoutEnabled = enabled;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Sets the telephone number for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose telephone number should be set.</param>
        /// <param name="phoneNumber">The telephone number to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.PhoneNumber = phoneNumber;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Gets the telephone number, if any, for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose telephone number should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the user's telephone number, if any.</returns>
        public virtual Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.PhoneNumber);
        }

        /// <summary>
        /// Gets a flag indicating whether the specified <paramref name="user"/>'s telephone number has been confirmed.
        /// </summary>
        /// <param name="user">The user to return a flag for, indicating whether their telephone number is confirmed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, returning true if the specified <paramref name="user"/> has a confirmed
        /// telephone number otherwise false.
        /// </returns>
        public virtual Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        /// <summary>
        /// Sets a flag indicating if the specified <paramref name="user"/>'s phone number has been confirmed..
        /// </summary>
        /// <param name="user">The user whose telephone number confirmation status should be set.</param>
        /// <param name="confirmed">A flag indicating whether the user's telephone number has been confirmed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.PhoneNumberConfirmed = confirmed;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Sets the provided security <paramref name="stamp"/> for the specified <paramref name="user"/>.
        /// </summary>
        /// <param name="user">The user whose security stamp should be set.</param>
        /// <param name="stamp">The security stamp to set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.SecurityStamp = stamp;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Get the security stamp for the specified <paramref name="user" />.
        /// </summary>
        /// <param name="user">The user whose security stamp should be set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation, containing the security stamp for the specified <paramref name="user"/>.</returns>
        public virtual Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.SecurityStamp);
        }

        /// <summary>
        /// Sets a flag indicating whether the specified <paramref name="user"/> has two factor authentication enabled or not,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose two factor authentication enabled status should be set.</param>
        /// <param name="enabled">A flag indicating whether the specified <paramref name="user"/> has two factor authentication enabled.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            user.TwoFactorEnabled = enabled;
            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Returns a flag indicating whether the specified <paramref name="user"/> has two factor authentication enabled or not,
        /// as an asynchronous operation.
        /// </summary>
        /// <param name="user">The user whose two factor authentication enabled status should be set.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> that represents the asynchronous operation, containing a flag indicating whether the specified 
        /// <paramref name="user"/> has two factor authentication enabled or not.
        /// </returns>
        public virtual Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            return Task.FromResult(user.TwoFactorEnabled);
        }

        /// <summary>
        /// Retrieves all users with the specified claim.
        /// </summary>
        /// <param name="claim">The claim whose users should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> contains a list of users, if any, that contain the specified claim. 
        /// </returns>
        public virtual Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves all users in the specified role.
        /// </summary>
        /// <param name="normalizedRoleName">The role whose users should be retrieved.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>
        /// The <see cref="Task"/> contains a list of users, if any, that are in the specified role. 
        /// </returns>
        public virtual Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();           
        }

        internal protected abstract Task<TUserToken> FindToken(TUser user, string loginProvider, string name, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the token value for a particular user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="loginProvider">The authentication provider for the token.</param>
        /// <param name="name">The name of the token.</param>
        /// <param name="value">The value of the token.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public virtual async Task SetTokenAsync(TUser user, string loginProvider, string name, string value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var token = await FindToken(user, loginProvider, name, cancellationToken);
            if (token == null)
            {
                token = CreateUserToken(user, loginProvider, name, value);
                var doc = await Context.Client.CreateDocumentAsync(Context.IdentityDocumentCollection.DocumentsLink, token
                       , Context.RequestOptions, true);
                Context.SetSessionTokenIfEmpty(doc.SessionToken);
                token = JsonHelpers.CreateObject<TUserToken>(doc.Resource.ToString());
            }
            else
            {
                token.Value = value;
            }
        }

        /// <summary>
        /// Deletes a token for a user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="loginProvider">The authentication provider for the token.</param>
        /// <param name="name">The name of the token.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task RemoveTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var entry = await FindToken(user, loginProvider, name, cancellationToken);
            if (entry != null)
            {
                var doc = await Context.Client.DeleteDocumentAsync(entry.SelfLink,
                   Context.RequestOptions);
                Context.SetSessionTokenIfEmpty(doc.SessionToken);
            }
        }

        /// <summary>
        /// Returns the token value.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="loginProvider">The authentication provider for the token.</param>
        /// <param name="name">The name of the token.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public async Task<string> GetTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            var entry = await FindToken(user, loginProvider, name, cancellationToken);
            return entry?.Value;
        }
    }
}
