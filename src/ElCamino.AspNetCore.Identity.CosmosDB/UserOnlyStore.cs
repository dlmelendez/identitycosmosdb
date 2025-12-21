// MIT License Copyright (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.CosmosDB.Extensions;
using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;

namespace ElCamino.AspNetCore.Identity.CosmosDB
{
    public class UserOnlyStore<TUser>
        : UserOnlyStore<TUser, IdentityCloudContext> where TUser : Model.IdentityUser<string, Model.IdentityUserClaim<string>, Model.IdentityUserLogin<string>>, new()
    {
        public UserOnlyStore(IdentityCloudContext context) : base(context) { }

    }

    public class UserOnlyStore<TUser, TContext>
        : UserOnlyStore<TUser, TContext, string, Model.IdentityUserClaim<string>, Model.IdentityUserLogin<string>, Model.IdentityUserToken<string>>
        where TUser : Model.IdentityUser<string, Model.IdentityUserClaim<string>, Model.IdentityUserLogin<string>>, new()
        where TContext : IdentityCloudContext
    {
        public UserOnlyStore(TContext context) : base(context) { }
    }

    public class UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken> :
        Microsoft.AspNetCore.Identity.UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>
        , IUserLoginStore<TUser>
        , IUserClaimStore<TUser>
        , IUserPasswordStore<TUser>
        , IUserSecurityStampStore<TUser>
        , IUserEmailStore<TUser>
        , IUserLockoutStore<TUser>
        , IUserPhoneNumberStore<TUser>
        , IQueryableUserStore<TUser>
        , IUserTwoFactorStore<TUser>
        , IUserAuthenticationTokenStore<TUser>
        , IDisposable
        where TUser : Model.IdentityUser<TKey, TUserClaim, TUserLogin>, new()
        where TKey : IEquatable<TKey>
        where TUserLogin : Model.IdentityUserLogin<TKey>, new()
        where TUserClaim : Model.IdentityUserClaim<TKey>, new()
        where TUserToken : Model.IdentityUserToken<TKey>, new()
        where TContext : IdentityCloudContext
    {
        public TContext Context { get; private set; }

        public UserOnlyStore(TContext context) : base(new IdentityErrorDescriber())
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(context);
            Context = context;
#else
            Context = context ?? throw new ArgumentNullException(nameof(context));
#endif
            ErrorDescriber = new IdentityErrorDescriber();
        }

        protected internal async IAsyncEnumerable<Q> ExecuteSqlQuery<Q>(QueryDefinition sqlQuery, QueryRequestOptions queryOptions = null) where Q : class
        {
            queryOptions ??= Context.QueryOptions;

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
            } while (continuationToken is not null);
        }

        protected internal async Task<Q> ExecuteSqlQueryFirstAsync<Q>(QueryDefinition sqlQuery, QueryRequestOptions queryOptions = null) where Q : class
        {
            if (queryOptions is null)
            {
                queryOptions = Context.QueryOptions;
                queryOptions.MaxConcurrency = 0; //max
                queryOptions.MaxItemCount = 1;
            }

            var feedIterator = Context.IdentityContainer.GetItemQueryIterator<Q>(sqlQuery, requestOptions: queryOptions);

            if (feedIterator.HasMoreResults)
            {
                return (await feedIterator.ReadNextAsync().ConfigureAwait(false)).FirstOrDefault();
            }

            return null;
        }

        protected internal async Task<Q> ExecuteSqlStoredProcFirstAsync<Q>(Scripts scripts, string sprocName, PartitionKey partitionKey, dynamic[] parameters) where Q : class
        {
            StoredProcedureExecuteResponse<string> response = await scripts.ExecuteStoredProcedureAsync<string>(
               sprocName,
               partitionKey,
               parameters);

            return !string.IsNullOrWhiteSpace(response.Resource) ?
                JsonSerializer.Deserialize<Q>(response.Resource, JsonHelper.JsonOptions) : null;
        }

        protected IOrderedQueryable<TUser> UsersSet
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<TUser>(true);
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
        /// A navigation property for the users the store contains.
        /// </summary>
        public override IQueryable<TUser> Users
        {
            get { return UsersSet; }
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

        public override async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
#endif

            var doc = await Context.IdentityContainer.CreateItemAsync<TUser>(user, new PartitionKey(user.PartitionKey), Context.RequestOptions, cancellationToken)
                .ConfigureAwait(false);
            Context.SetSessionTokenIfEmpty(doc.Headers.Session);
            return IdentityResult.Success;
        }

        public override async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
#endif

            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            try
            {
                ItemRequestOptions ro = Context.RequestOptions;
                //TODO: Investigate why UserManager is updating twice with different ETag
                //ro.IfMatchEtag = user.ETag;

                var doc = await Context.IdentityContainer.ReplaceItemAsync<TUser>(user, user.Id.ToString(), new PartitionKey(user.PartitionKey), ro, cancellationToken)
                    .ConfigureAwait(false);
                Context.SetSessionTokenIfEmpty(doc.Headers.Session);
                user = doc.Resource;
                return IdentityResult.Success;
            }
            catch (CosmosException dc) 
            {
                return ConcurrencyCheckResultFailed(dc);
            }
        }

        public override async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
#endif

            try
            {
                var doc = await Context.IdentityContainer.DeleteItemAsync<TUser>(user.Id.ToString(), new PartitionKey(user.PartitionKey), Context.RequestOptions, cancellationToken)
                    .ConfigureAwait(false);
                Context.SetSessionTokenIfEmpty(doc.Headers.Session);
            }
            catch (CosmosException dc)
            {
                return ConcurrencyCheckResultFailed(dc);
            }
            return IdentityResult.Success;
        }

        public override async Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(userId);
#else
            if (userId is null)
            {
                throw new ArgumentNullException(nameof(userId));
            }
#endif

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            return await ExecuteSqlStoredProcFirstAsync<TUser>(Context.IdentityContainer.Scripts,
                Context.GetUserByIdSproc,
                new PartitionKey(PartitionKeyHelper.GetPartitionKeyFromId(userId)),
                [userId]);
        }

        public override async Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(normalizedUserName);
#else
            if (normalizedUserName is null)
            {
                throw new ArgumentNullException(nameof(normalizedUserName));
            }
#endif

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            //TODO: Deprecate Stored Proc: getUserByUserName_v1
            QueryDefinition query = new QueryDefinition("SELECT * FROM root r " +
                "WHERE (r.normalizedUserName = @normalizedName) ")
                .WithParameter("@normalizedName", normalizedUserName);

            Console.WriteLine(query.QueryText);
            return await ExecuteSqlQueryFirstAsync<TUser>(query, Context.QueryOptions)
                                .ConfigureAwait(false);
        }

        protected override Task<TUser> FindUserAsync(TKey userId, CancellationToken cancellationToken)
        {
            return FindByIdAsync(userId.ToString(), cancellationToken);
        }

        protected override Task<TUserLogin> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            //not needed right now since overriding FindByLoginAsync
            throw new NotImplementedException();
        }

        protected override Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            //not needed right now since overriding FindByLoginAsync
            throw new NotImplementedException();
        }

        public override async Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
#endif

            return await user.Claims.Where(uc => uc.UserId.Equals(user.Id)).Select(c => c.ToClaim())
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public override Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(claims);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims is null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
#endif
            foreach (var claim in claims)
            {
                user.Claims.Add(CreateUserClaim(user, claim));
            }
            return Task.CompletedTask;
        }

        public override async Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(claim);
            ArgumentNullException.ThrowIfNull(newClaim);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claim is null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (newClaim is null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }
#endif

            var matchedClaims = await UserClaims.Where(uc => uc.UserId.Equals(user.Id) && uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            foreach (var matchedClaim in matchedClaims)
            {
                matchedClaim.ClaimValue = newClaim.Value;
                matchedClaim.ClaimType = newClaim.Type;
            }
        }

        public override async Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(claims);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (claims is null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
#endif
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

        public override Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
            ArgumentNullException.ThrowIfNull(login);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            if (login is null)
            {
                throw new ArgumentNullException(nameof(login));
            }
#endif
            user.Logins.Add(CreateUserLogin(user, login));
            return Task.CompletedTask;
        }

        public override Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
#endif
            var entry = user.Logins.SingleOrDefault(userLogin => userLogin.UserId.Equals(user.Id) && userLogin.LoginProvider == loginProvider && userLogin.ProviderKey == providerKey);
            if (entry is not null)
            {
                user.Logins.Remove(entry);
            }
            return Task.CompletedTask;
        }

        public override async Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
#endif
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
        public override async Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(loginProvider);
            ArgumentNullException.ThrowIfNull(providerKey);
#else
            if (loginProvider is null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }
            if (providerKey is null)
            {
                throw new ArgumentNullException(nameof(providerKey));
            }
#endif

            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            //TODO: Deprecate Stored Proc: getUserByLogin_v1
            QueryDefinition query = new QueryDefinition("SELECT VALUE r FROM root r JOIN l IN r.logins " +
                "WHERE l.loginProvider = @loginProvider " +
                "AND l.providerKey = @providerKey ")
                .WithParameter("@loginProvider", loginProvider)
                .WithParameter("@providerKey", providerKey);

            Console.WriteLine(query.QueryText);
            return await ExecuteSqlQueryFirstAsync<TUser>(query, Context.QueryOptions)
                                .ConfigureAwait(false);
        }

        public override async Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(normalizedEmail);
#else
            if (normalizedEmail is null)
            {
                throw new ArgumentNullException(nameof(normalizedEmail));
            }
#endif
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            //TODO: Deprecate Stored Proc: getUserByEmail_v1
            QueryDefinition query = new QueryDefinition("SELECT * FROM root r " +
                "WHERE (r.normalizedEmail = @normalizedEmail) ")
                .WithParameter("@normalizedEmail", normalizedEmail);

            Console.WriteLine(query.QueryText);
            return await ExecuteSqlQueryFirstAsync<TUser>(query, Context.QueryOptions)
                                .ConfigureAwait(false);
        }

        public virtual async Task<IList<TUser>> FindAllByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(normalizedEmail);
#else
            if (normalizedEmail is null)
            {
                throw new ArgumentNullException(nameof(normalizedEmail));
            }
#endif

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

        public override async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(claim);
#else

            claim = claim?? throw new ArgumentNullException(nameof(claim));
#endif
            QueryDefinition query = new QueryDefinition("SELECT VALUE u " +
                   "FROM ROOT u " +
                   "JOIN uc in u.claims " +
                   "WHERE (uc.claimValue = @claimValue) AND (uc.claimType = @claimType) ")
                .WithParameter("@claimValue", claim.Value)
                .WithParameter("@claimType", claim.Type);

            Debug.WriteLine(query.QueryText);
            return await ExecuteSqlQuery<TUser>(query, Context.QueryOptions)
                .ToListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            return await UserTokens.FirstOrDefaultAsync((t) => t.UserId.Equals(user.Id) && t.LoginProvider == loginProvider && t.Name == name, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task AddUserTokenAsync(TUserToken token)
        {
            var doc = await Context.IdentityContainer.CreateItemAsync<TUserToken>(token, new PartitionKey(token.PartitionKey), Context.RequestOptions);
            Context.SetSessionTokenIfEmpty(doc.Headers.Session);
            _ = doc.Resource;
        }

        protected override async Task RemoveUserTokenAsync(TUserToken token)
        {
            var doc = await Context.IdentityContainer.DeleteItemAsync<TUserToken>(token.Id.ToString(), new PartitionKey(token.PartitionKey), Context.RequestOptions)
                .ConfigureAwait(false);
            Context.SetSessionTokenIfEmpty(doc.Headers.Session);
        }

        protected override TUserToken CreateUserToken(TUser user, string loginProvider, string name, string value)
        {
            return new TUserToken()
            {
                UserId = user.Id,
                LoginProvider = loginProvider,
                Name = name,
                Value = value
            };
        }        
    }
}
