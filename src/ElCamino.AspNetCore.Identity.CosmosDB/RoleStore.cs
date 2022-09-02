// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
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
using System.Security.Claims;
using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;

namespace ElCamino.AspNetCore.Identity.CosmosDB
{

    /// <summary>
    /// Creates a new instance of a persistence store for roles.
    /// </summary>
    /// <typeparam name="TRole">The type of the class representing a role.</typeparam>
    /// <typeparam name="TContext">The type of the data context class used to access the store.</typeparam>
    public class RoleStore<TRole, TContext> : RoleStore<TRole, TContext, string, Model.IdentityUserRole<string>, Model.IdentityRoleClaim<string>>,
        IQueryableRoleStore<TRole>,
        IRoleClaimStore<TRole>
        where TRole : Model.IdentityRole<string>, new()
        where TContext : IdentityCloudContext
    {
        /// <summary>
        /// Constructs a new instance of <see cref="RoleStore{TRole, TContext, TKey}"/>.
        /// </summary>
        /// <param name="context">The <see cref="IdentityCloudContext"/>.</param>
        /// <param name="describer">The <see cref="IdentityErrorDescriber"/>.</param>
        public RoleStore(TContext context, IdentityErrorDescriber describer = null) : base(context, describer) { }

        public override IQueryable<TRole> Roles
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<TRole>(true);
            }
        }

        public override IQueryable<Model.IdentityRoleClaim<string>> RoleClaims
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<Model.IdentityRoleClaim<string>>(true);
            }
        }

        public override async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            role = await FindByIdAsync(role.Id);
            if (role != null)
                return await role.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue))
                    .ToListAsync(cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            return new List<Claim>();
        }

        /// <summary>
        /// Adds the <paramref name="claim"/> given to the specified <paramref name="role"/>.
        /// </summary>
        /// <param name="role">The role to add the claim to.</param>
        /// <param name="claim">The claim to add to the role.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            role.Claims.Add(CreateRoleClaim(role, claim));
            return Task.CompletedTask;
        }        

    }


    public abstract class RoleStore<TRole, TContext, TKey, TUserRole, TRoleClaim> : RoleStoreBase<TRole, TKey, TUserRole, TRoleClaim>
        where TRole : Model.IdentityRole<TKey, TUserRole, TRoleClaim>, new()
        where TKey : IEquatable<TKey>
        where TContext : IdentityCloudContext
        where TUserRole : Model.IdentityUserRole<TKey>, new()
        where TRoleClaim : Model.IdentityRoleClaim<TKey>, new()
    {
        private bool _disposed;
        private Container _roleTable;

        public RoleStore(TContext context, IdentityErrorDescriber describer = null) : base(describer)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _roleTable = context.IdentityContainer;

        }

        public override async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            var doc = await Context.IdentityContainer.CreateItemAsync<TRole>(role, new PartitionKey(role.PartitionKey), Context.RequestOptions);
            Context.SetSessionTokenIfEmpty(doc.Headers.Session);
            return IdentityResult.Success;
        }

        public async override Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            role.ConcurrencyStamp = Guid.NewGuid().ToString();
            var doc = await Context.IdentityContainer.UpsertItemAsync<TRole>(role, new PartitionKey(role.PartitionKey), Context.RequestOptions);
            Context.SetSessionTokenIfEmpty(doc.Headers.Session);
            return IdentityResult.Success;
        }

        public async override Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            var doc = await Context.IdentityContainer.DeleteItemAsync<TRole>(role.Id.ToString(), new PartitionKey(role.PartitionKey), Context.RequestOptions);
            Context.SetSessionTokenIfEmpty(doc.Headers.Session);

            return IdentityResult.Success;
        }

        public new void Dispose()
        {
            base.Dispose();
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (Context != null)
                {
                    Context.Dispose();
                }
                _roleTable = null;
                Context = null;
                _disposed = true;
            }
        }


        /// <summary>
        /// Finds the role who has the specified ID as an asynchronous operation.
        /// </summary>
        /// <param name="id">The role ID to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
        public override Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return FindById(id);
        }

        /// <summary>
        /// Finds the role who has the specified normalized name as an asynchronous operation.
        /// </summary>
        /// <param name="normalizedName">The normalized role name to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
        public override async Task<TRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();

            QueryDefinition query = new QueryDefinition("SELECT * FROM Roles r WHERE (r.normalizedName = @normalizedName)")
               .WithParameter("@normalizedName", normalizedName);

            QueryRequestOptions options = Context.QueryOptions;
            options.MaxItemCount = 1;
            options.MaxBufferedItemCount = 1;
            options.MaxConcurrency = 0; //max parallel               
            

            var feedIterator = Context.IdentityContainer.GetItemQueryIterator<TRole>(query, requestOptions:options);

            if (feedIterator.HasMoreResults)
            {
                return (await feedIterator.ReadNextAsync()).FirstOrDefault();
            }

            return null;
        }

        private async Task<TRole> FindById(string roleKeyString)
        {
            try
            {
                var itemResponse = await Context.IdentityContainer.ReadItemAsync<TRole>(roleKeyString,
                    new PartitionKey(PartitionKeyHelper.GetPartitionKeyFromId(roleKeyString)));

                return itemResponse.Resource;
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                throw;
            }
        }



        /// <summary>
        /// Removes the <paramref name="claim"/> given from the specified <paramref name="role"/>.
        /// </summary>
        /// <param name="role">The role to remove the claim from.</param>
        /// <param name="claim">The claim to remove from the role.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            var claims = role.Claims.Where(rc => rc.RoleId.Equals(role.Id) && rc.ClaimValue == claim.Value && rc.ClaimType == claim.Type).ToList();
            foreach (var c in claims)
            {
                role.Claims.Remove(c);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the context for this store.
        /// </summary>
        public TContext Context { get; private set; }


        public override IQueryable<TRole> Roles
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<TRole>(true);
            }
        }

        public virtual IQueryable<TRoleClaim> RoleClaims
        {
            get
            {
                return Context.IdentityContainer.GetItemLinqQueryable<TRoleClaim>(true);
            }
        }

    }
}
