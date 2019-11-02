// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
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
using ElCamino.AspNetCore.Identity.DocumentDB.Helpers;

namespace ElCamino.AspNetCore.Identity.DocumentDB
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

        public override IQueryable<TRole> Roles => Context.Client.CreateDocumentQuery<TRole>(Context.IdentityDocumentCollection.DocumentsLink);

        public override IQueryable<Model.IdentityRoleClaim<string>> RoleClaims => Context.Client.CreateDocumentQuery<Model.IdentityRoleClaim<string>>(Context.IdentityDocumentCollection.DocumentsLink);


        public override async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            role = await FindByIdAsync(role.Id);
            if (role != null)
                return await role.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToListAsync(cancellationToken: cancellationToken);
            return new List<Claim>();
        }

        /// <summary>
        /// Adds the <paramref name="claim"/> given to the specified <paramref name="role"/>.
        /// </summary>
        /// <param name="role">The role to add the claim to.</param>
        /// <param name="claim">The claim to add to the role.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
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
            return TaskCacheExtensions.CompletedTask;
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
        private DocumentCollection _roleTable;

        public RoleStore(TContext context, IdentityErrorDescriber describer = null) : base(describer)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            _roleTable = context.IdentityDocumentCollection;

        }

        public override async Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            var doc = await Context.Client.CreateDocumentAsync(Context.IdentityDocumentCollection.DocumentsLink, role
                        , Context.RequestOptions, true);
            Context.SetSessionTokenIfEmpty(doc.SessionToken);
            role = JsonHelpers.CreateObject<TRole>(doc);
            return IdentityResult.Success;
        }

        public async override Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            role.ConcurrencyStamp = Guid.NewGuid().ToString();
            var doc = await Context.Client.UpsertDocumentAsync(Context.IdentityDocumentCollection.DocumentsLink, role, Context.RequestOptions,
                disableAutomaticIdGeneration: true);
            Context.SetSessionTokenIfEmpty(doc.SessionToken);
            return IdentityResult.Success;
        }

        public async override Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            var doc = await Context.Client.DeleteDocumentAsync(role.SelfLink,
                    Context.RequestOptions);
            Context.SetSessionTokenIfEmpty(doc.SessionToken);

            return IdentityResult.Success;
        }

        public new void Dispose()
        {
            base.Dispose();
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (Context != null)
                {
                    this.Context.Dispose();
                }
                this._roleTable = null;
                this.Context = null;
                this._disposed = true;
            }
        }


        /// <summary>
        /// Finds the role who has the specified ID as an asynchronous operation.
        /// </summary>
        /// <param name="id">The role ID to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
        public override async Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return await Task.FromResult<TRole>(FindById(id));
        }

        /// <summary>
        /// Finds the role who has the specified normalized name as an asynchronous operation.
        /// </summary>
        /// <param name="normalizedName">The normalized role name to look for.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>A <see cref="Task{TResult}"/> that result of the look up.</returns>
        public override async Task<TRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            SqlQuerySpec query = new SqlQuerySpec("SELECT * FROM Roles r WHERE (r.normalizedName = @normalizedName)", new SqlParameterCollection(){
             new SqlParameter("@normalizedName", normalizedName)
            });

            var fo = Context.FeedOptions;
            fo.MaxItemCount = 1;
            var doc = Context.Client.CreateDocumentQuery(Context.IdentityDocumentCollection.DocumentsLink,
                query, fo)
            .ToList()
            .FirstOrDefault();

            TRole role = doc;
            return await Task.FromResult(role);
        }

        private TRole FindById(string roleKeyString)
        {
            SqlQuerySpec query = new SqlQuerySpec("SELECT * FROM Roles r WHERE (r.id = @id)", new SqlParameterCollection(){
             new SqlParameter("@id", roleKeyString)
            });

            var fo = Context.FeedOptions;
            fo.MaxItemCount = 1;
            var doc = Context.Client.CreateDocumentQuery(Context.IdentityDocumentCollection.DocumentsLink,
                query, fo)
            .ToList()
            .FirstOrDefault();

            TRole role = doc;
            return role;
        }



        /// <summary>
        /// Removes the <paramref name="claim"/> given from the specified <paramref name="role"/>.
        /// </summary>
        /// <param name="role">The role to remove the claim from.</param>
        /// <param name="claim">The claim to remove from the role.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
        /// <returns>The <see cref="Task"/> that represents the asynchronous operation.</returns>
        public override Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
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

            return TaskCacheExtensions.CompletedTask;
        }

        /// <summary>
        /// Gets the context for this store.
        /// </summary>
        public TContext Context { get; private set; }


        public override IQueryable<TRole> Roles => Context.Client.CreateDocumentQuery<TRole>(Context.IdentityDocumentCollection.DocumentsLink);

        public virtual IQueryable<TRoleClaim> RoleClaims => Context.Client.CreateDocumentQuery<TRoleClaim>(Context.IdentityDocumentCollection.DocumentsLink);

       
    }
}
