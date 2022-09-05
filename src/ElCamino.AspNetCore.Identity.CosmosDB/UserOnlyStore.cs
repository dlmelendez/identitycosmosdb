using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Azure;
using ElCamino.AspNetCore.Identity.CosmosDB.Extensions;
using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Threading;

namespace ElCamino.AspNetCore.Identity.CosmosDB
{
    public class UserOnlyStore<TUser>
        : UserOnlyStore<TUser, IdentityCloudContext> where TUser : Model.IdentityUser<string>, new()
    {
        public UserOnlyStore(IdentityCloudContext context) : base(context) { }

    }

    public class UserOnlyStore<TUser, TContext>
        : UserOnlyStore<TUser, TContext, string, Model.IdentityUserClaim, Model.IdentityUserLogin, Model.IdentityUserToken>
        where TUser : Model.IdentityUser<string, Model.IdentityUserClaim<string>, Model.IdentityUserLogin<string>>, new()
        where TContext : IdentityCloudContext
    {
        public UserOnlyStore(TContext context) : base(context) { }
    }

    public class UserOnlyStore<TUser, TContext, TKey, TUserClaim, TUserLogin, TUserToken> :
        UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>
        , IDisposable
        where TUser : Model.IdentityUser<TKey, Model.IdentityUserClaim<TKey>, Model.IdentityUserLogin<TKey>>, new()
        where TKey : IEquatable<TKey>
        where TUserLogin : Model.IdentityUserLogin<TKey>, new()
        where TUserClaim : Model.IdentityUserClaim<TKey>, new()
        where TUserToken : Model.IdentityUserToken<TKey>, new()
        where TContext : IdentityCloudContext
    {
        protected bool _disposed;
        public TContext Context { get; private set; }
        public override IQueryable<TUser> Users { get; }

        public UserOnlyStore(TContext context) : base(new IdentityErrorDescriber())
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public override Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        protected override Task<TUser> FindUserAsync(TKey userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task<TUserLogin> FindUserLoginAsync(TKey userId, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task<TUserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        protected override Task<TUserToken> FindTokenAsync(TUser user, string loginProvider, string name, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task AddUserTokenAsync(TUserToken token)
        {
            throw new NotImplementedException();
        }

        protected override Task RemoveUserTokenAsync(TUserToken token)
        {
            throw new NotImplementedException();
        }

        // Virtual Overrides

    }
}
