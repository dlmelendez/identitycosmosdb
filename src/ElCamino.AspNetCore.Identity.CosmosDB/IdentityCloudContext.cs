// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.CosmosDB.Model;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Scripts;
using Microsoft.Azure.Cosmos.Linq;

namespace ElCamino.AspNetCore.Identity.CosmosDB
{
    public class IdentityCloudContext : IDisposable
    {
        internal class InternalContext : IDisposable
        {
            private CosmosClient _client = null;
            private Database _db = null;
            private readonly string _databaseId = null;
            private Container _identityContainer;
            private readonly string _identityContainerId;
            private string _sessionToken = string.Empty;
            private bool _disposed = false;

            public const string GetUserByIdSproc = "getUserById_v1";


            public InternalContext(IdentityConfiguration config)
            {
                
                _client = new CosmosClient(config.Uri, config.AuthKey, config.Options);
                _databaseId = config.Database;
                _identityContainerId = config.IdentityCollection;
                InitDatabase();
                InitCollection();
            }

            public async Task CreateIfNotExistsAsync()
            {
                await CreateDatabaseAsync();
                await CreateContainerAsync();
                await CreateStoredProcsAsync();
            }

            private async Task CreateDatabaseAsync()
            {
                _db = await _client.CreateDatabaseIfNotExistsAsync(_databaseId);
            }

            private void InitDatabase()
            {
                if (_db == null)
                {
                    _db = _client.GetDatabase(_databaseId);
                }
            }

            private async Task CreateContainerAsync()
            {
                var containerResponse = await _db.CreateContainerIfNotExistsAsync(new ContainerProperties(_identityContainerId, "/partitionKey"));
                IdentityContainer = containerResponse.Container;
            }

            private void InitCollection()
            {
                if (IdentityContainer == null)
                {
                    IdentityContainer = _db.GetContainer(_identityContainerId);
                }
            }

            private async Task CreateStoredProcsAsync()
            {
                await CreateSprocGetUserByIdAsync();
            }

            private async Task CreateSprocGetUserByIdAsync()
            {
                string body = string.Empty;

                using (StreamReader sr = new StreamReader(typeof(IdentityCloudContext).GetTypeInfo().Assembly.GetManifestResourceStream(
                    "ElCamino.AspNetCore.Identity.CosmosDB.StoredProcs.getUserById_sproc.js"), Encoding.UTF8))
                {
                    body = sr.ReadToEnd();
                }
                string strId = "getUserById_v1";
                
                TryDeleteStoredProcedure(_identityContainer, strId).Wait();
                _ = await _identityContainer.Scripts.CreateStoredProcedureAsync(
                    new StoredProcedureProperties(strId, body));
                
            }

            private async Task TryDeleteStoredProcedure(Container container, string sprocId)
            {
                Scripts cosmosScripts = container.Scripts;

                try
                {
                    StoredProcedureResponse sproc = await cosmosScripts.ReadStoredProcedureAsync(sprocId);
                    await cosmosScripts.DeleteStoredProcedureAsync(sprocId);
                }
                catch (CosmosException ex) 
                when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    //Nothing to delete
                }
            }

            ~InternalContext()
            {
                Dispose(false);
            }

            public CosmosClient Client
            {
                get { ThrowIfDisposed(); return _client; }
            }

            public Database Database
            {
                get { ThrowIfDisposed(); return _db; }
            }

            public ConsistencyLevel ConsistencyLevel { get; private set; } = ConsistencyLevel.Session;

            public ItemRequestOptions RequestOptions
            {
                get
                {
                    return new ItemRequestOptions()
                    {
                        ConsistencyLevel = ConsistencyLevel,
                        SessionToken = SessionToken,
                    };
                }
            }

            public QueryRequestOptions FeedOptions
            {
                get
                {
                    return new QueryRequestOptions()
                    {
                        EnableScanInQuery = true,
                        ConsistencyLevel = ConsistencyLevel,                           
                    };
                }
            }

            public string SessionToken
            {
                get { return _sessionToken; }
            }

            public void SetSessionTokenIfEmpty(string tokenNew)
            {
                if (string.IsNullOrWhiteSpace(_sessionToken))
                {
                    _sessionToken = tokenNew;
                }
            }

            public Container IdentityContainer
            {
                get { ThrowIfDisposed(); return _identityContainer; }
                set { _identityContainer = value; }
            }

            protected void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(base.GetType().Name);
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed && disposing)
                {
                    _disposed = true;
                    _client = null;
                    _db = null;
                    _identityContainer = null;
                    _sessionToken = null;
                }
            }
        }
        private readonly bool _disposed = false;

        //Thread safe dictionary 
        private static readonly ConcurrentDictionary<string, InternalContext> ContextCache = new ConcurrentDictionary<string, InternalContext>();
        private readonly string _configHash = null;
        private InternalContext _currentContext = null;

        public string GetUserByIdSproc
        {
            get { return InternalContext.GetUserByIdSproc; }
        }       

        public IdentityCloudContext(IdentityConfiguration config)
        {
            _configHash = config.ToString();
            if (!ContextCache.TryGetValue(_configHash, out InternalContext tempContext))
            {
                tempContext = new InternalContext(config);
                ContextCache.TryAdd(_configHash, tempContext);
                Debug.WriteLine(string.Format("ContextCacheAdd {0}", _configHash));
            }
#if DEBUG
            else
            {
                Debug.WriteLine(string.Format("ContextCacheGet {0}", _configHash));
            }
#endif
            _currentContext = tempContext;
        }
        

        ~IdentityCloudContext()
        {
            Dispose(false);
        }

        public CosmosClient Client
        {
            get { ThrowIfDisposed(); return _currentContext.Client; }
        }

        public Database Database
        {
            get { ThrowIfDisposed(); return _currentContext.Database; }
        }


        public ItemRequestOptions RequestOptions
        {
            get
            {
                return _currentContext.RequestOptions;
            }
        }

        public QueryRequestOptions QueryOptions
        {
            get
            {
                return _currentContext.FeedOptions;
            }
        }


        public string SessionToken
        {
            get { return _currentContext.SessionToken; }
        }

        public void SetSessionTokenIfEmpty(string tokenNew)
        {
            _currentContext.SetSessionTokenIfEmpty(tokenNew);
        }

        public Container IdentityContainer
        {
            get { ThrowIfDisposed(); return _currentContext.IdentityContainer; }
            set { _currentContext.IdentityContainer = value; }
        }

        public Task CreateIfNotExistsAsync()
        {
            return _currentContext.CreateIfNotExistsAsync();
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(base.GetType().Name);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Debug.WriteLine(string.Format("ContextCacheDispose({0})", _configHash));
                _currentContext = null;
            }
        }
    }

}
