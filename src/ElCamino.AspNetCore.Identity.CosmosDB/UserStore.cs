// MIT License Copyright (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.CosmosDB.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;

namespace ElCamino.AspNetCore.Identity.CosmosDB
{

    /// <summary>
    /// Represents a new instance of a persistence store for the specified user and role types.
    /// </summary>
    /// <typeparam name="TUser">The type representing a user.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    public class UserStore<TUser, TContext>
        : UserStore<TUser, Model.IdentityRole<string>, TContext>
        where TUser : Model.IdentityUser<string, Model.IdentityUserClaim<string>, Model.IdentityUserRole<string>, Model.IdentityUserLogin<string>>, new()
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
        where TUser : Model.IdentityUser<string, Model.IdentityUserClaim<string>, Model.IdentityUserRole<string>, Model.IdentityUserLogin<string>>, new()
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
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
#endif

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
                    .ToListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return [];

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
            return [];
        }

        public override async Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
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
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));
            }

            var roleEntity = await Roles.SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken: cancellationToken)
                .ConfigureAwait(false) ?? throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, Resources.RoleNotFound, normalizedRoleName));
            user.Roles.Add(CreateUserRole(user, roleEntity));

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
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
#endif
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));
            }
            var roleEntity = await Roles.SingleOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken)
                .ConfigureAwait(false);
            if (roleEntity is not null)
            {
                var userRole = user.Roles.FirstOrDefault(r => r.RoleId.Equals(roleEntity.Id) && r.UserId.Equals(user.Id));
                if (userRole is not null)
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
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(user);
#else
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }
#endif
            if (string.IsNullOrWhiteSpace(normalizedRoleName))
            {
                throw new ArgumentException(Resources.ValueCannotBeNullOrEmpty, nameof(normalizedRoleName));
            }

            string roleId = await GetRoleIdByNormalizedNameAsync(normalizedRoleName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(roleId))
            {
                var userRole = user.Roles.FirstOrDefault(ur => ur.RoleId.Equals(roleId) && ur.UserId.Equals(user.Id));
                return userRole is not null;
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

        protected async Task<Model.IdentityUserRole<string>> FindUserRoleAsync(string userId, string roleId, CancellationToken cancellationToken)
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
                return await ExecuteSqlQueryFirstAsync<Model.IdentityUserRole>(query, Context.QueryOptions).ConfigureAwait(false);
        }

        protected override Model.IdentityUserRole<string> CreateUserRole(TUser user, TRole role)
        {
            return new Model.IdentityUserRole()
            {
                UserId = user.Id,
                RoleId = role.Id
            };
        }

        protected override Task<TUser> FindUserAsync(string userId, CancellationToken cancellationToken)
        {
            return base.FindByIdAsync(userId, cancellationToken);
        }

        protected override Task<Model.IdentityUserLogin<string>> FindUserLoginAsync(string userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            //not needed right now since overriding FindByLoginAsync
            throw new NotImplementedException();
        }

        protected override Task<Model.IdentityUserLogin<string>> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            //not needed right now since overriding FindByLoginAsync
            throw new NotImplementedException();
        }

        protected override async Task<Model.IdentityUserToken<string>> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            return await UserTokens.FirstOrDefaultAsync((t) => t.UserId == user.Id && t.LoginProvider == loginProvider && t.Name == name, cancellationToken)
                .ConfigureAwait(false);
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
            var doc = await Context.IdentityContainer.DeleteItemAsync<Model.IdentityUserToken<string>>(token.Id, new PartitionKey(token.PartitionKey), Context.RequestOptions)
                .ConfigureAwait(false);
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
        UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken>,
        IUserRoleStore<TUser>
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
        public UserStore(TContext context, IdentityErrorDescriber describer = null): base(context)
        {
            ErrorDescriber = describer ?? new IdentityErrorDescriber();
        }

        protected IOrderedQueryable<TRole> Roles
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<TRole>(true);
            }
        }

        public abstract Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default);
        public abstract Task<IList<TUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default);
        public abstract Task AddToRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default);
        public abstract Task RemoveFromRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default);
        public abstract Task<bool> IsInRoleAsync(TUser user, string normalizedRoleName, CancellationToken cancellationToken = default);
        protected abstract Task<TRole> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken);
        protected abstract TUserRole CreateUserRole(TUser user, TRole role);
    }
}
