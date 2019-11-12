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
            private Container _identityContainer;
            private string _getUserByEmailSproc = null;
            private string _getUserByUserNameSproc = null;
            private string _getUserByIdSproc = null;
            private string _getUserByLoginSproc = null;
            private string _sessionToken = string.Empty;
            private bool _disposed = false;

            public string GetUserByLoginSproc
            {
                get { return _getUserByLoginSproc; }
            }

            public string GetUserByIdSproc
            {
                get { return _getUserByIdSproc; }
            }

            public string GetUserByUserNameSproc
            {
                get { return _getUserByUserNameSproc; }
            }

            public string GetUserByEmailSproc
            {
                get { return _getUserByEmailSproc; }
            }


            public InternalContext(IdentityConfiguration config)
            {
                
                _client = new CosmosClient(config.Uri, config.AuthKey, config.Options);

                InitDatabase(config.Database);
                InitCollection(config.IdentityCollection);
                InitStoredProcs();
            }

            private void InitDatabase(string database)
            {
                var databaseResponse = _client.CreateDatabaseIfNotExistsAsync(database);
                databaseResponse.Wait();
                _db = databaseResponse.Result;
            }

            private void InitCollection(string userCollectionId)
            {
                var ucresult = _db.CreateContainerIfNotExistsAsync(new ContainerProperties(userCollectionId, "/partitionKey"));
                ucresult.Wait();
                IdentityDocumentCollection = ucresult.Result.Container;
            }

            private void InitStoredProcs()
            {
                //InitGetUserByEmail();
                //InitGetUserByUserName();
                InitGetUserById();
                //InitGetUserByLogin();
            }

            private void InitGetUserByLogin()
            {
                string body = string.Empty;

                using (StreamReader sr = new StreamReader(typeof(IdentityCloudContext).GetTypeInfo().Assembly.GetManifestResourceStream(
                    "ElCamino.AspNetCore.Identity.CosmosDB.StoredProcs.getUserByLogin_sproc.js"), Encoding.UTF8))
                {
                    body = sr.ReadToEnd();
                }
                string strId = "getUserByLogin_v1";
                if (_getUserByLoginSproc == null)
                {
                    TryDeleteStoredProcedure(_identityContainer, strId).Wait();
                    Task<StoredProcedureResponse> task = _identityContainer.Scripts.CreateStoredProcedureAsync(
                        new StoredProcedureProperties(strId, body));
                    _getUserByLoginSproc = task.Result.Resource.Id;
                }
            }

            private void InitGetUserById()
            {
                string body = string.Empty;

                using (StreamReader sr = new StreamReader(typeof(IdentityCloudContext).GetTypeInfo().Assembly.GetManifestResourceStream(
                    "ElCamino.AspNetCore.Identity.CosmosDB.StoredProcs.getUserById_sproc.js"), Encoding.UTF8))
                {
                    body = sr.ReadToEnd();
                }
                string strId = "getUserById_v1";
                if (_getUserByIdSproc == null)
                {
                    TryDeleteStoredProcedure(_identityContainer, strId).Wait();
                    Task<StoredProcedureResponse> task = _identityContainer.Scripts.CreateStoredProcedureAsync(
                        new StoredProcedureProperties(strId, body));
                    task.Wait();

                    _getUserByIdSproc = task.Result.Resource.Id;
                }
            }

            private async Task TryDeleteStoredProcedure(Container container, string sprocId)
            {
                Scripts cosmosScripts = container.Scripts;

                try
                {
                    StoredProcedureResponse sproc = await cosmosScripts.ReadStoredProcedureAsync(sprocId);
                    await cosmosScripts.DeleteStoredProcedureAsync(sprocId);
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    //Nothing to delete
                }
            }
            private void InitGetUserByUserName()
            {
                string body = string.Empty;

                using (StreamReader sr = new StreamReader(typeof(IdentityCloudContext).GetTypeInfo().Assembly.GetManifestResourceStream(
                    "ElCamino.AspNetCore.Identity.CosmosDB.StoredProcs.getUserByUserName_sproc.js"), Encoding.UTF8))
                {
                    body = sr.ReadToEnd();
                }
                string strId = "getUserByUserName_v1";

                if (_getUserByUserNameSproc == null)
                {
                    TryDeleteStoredProcedure(_identityContainer, strId).Wait();
                    Task<StoredProcedureResponse> task = _identityContainer.Scripts.CreateStoredProcedureAsync(
                        new StoredProcedureProperties(strId, body));
                    _getUserByUserNameSproc = task.Result.Resource.Id;
                }
            }

            private void InitGetUserByEmail()
            {
                string body = string.Empty;

                using (StreamReader sr = new StreamReader(typeof(IdentityCloudContext).GetTypeInfo().Assembly.GetManifestResourceStream(
                    "ElCamino.AspNetCore.Identity.CosmosDB.StoredProcs.getUserByEmail_sproc.js")))
                {
                    body = sr.ReadToEnd();
                }
                string strId = "getUserByEmail_v1";
                if (_getUserByEmailSproc == null)
                {
                    TryDeleteStoredProcedure(_identityContainer, strId).Wait();
                    Task<StoredProcedureResponse> task = _identityContainer.Scripts.CreateStoredProcedureAsync(
                        new StoredProcedureProperties(strId, body));
                    _getUserByEmailSproc = task.Result.Resource.Id;
                }
            }

            ~InternalContext()
            {
                this.Dispose(false);
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
                        ConsistencyLevel = this.ConsistencyLevel,
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
                        ConsistencyLevel = this.ConsistencyLevel,                           
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

            public Container IdentityDocumentCollection
            {
                get { ThrowIfDisposed(); return _identityContainer; }
                set { _identityContainer = value; }
            }

            protected void ThrowIfDisposed()
            {
                if (this._disposed)
                {
                    throw new ObjectDisposedException(base.GetType().Name);
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
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
        private bool _disposed = false;

        //Thread safe dictionary 
        private static readonly ConcurrentDictionary<string, InternalContext> ContextCache = new ConcurrentDictionary<string, InternalContext>();
        private string _configHash = null;
        private InternalContext _currentContext = null;

        public string GetUserByLoginSproc
        {
            get { return _currentContext.GetUserByLoginSproc; }
        }

        public string GetUserByIdSproc
        {
            get { return _currentContext.GetUserByIdSproc; }
        }

        public string GetUserByUserNameSproc
        {
            get { return _currentContext.GetUserByUserNameSproc; }
        }

        public string GetUserByEmailSproc
        {
            get { return _currentContext.GetUserByEmailSproc; }
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
            this.Dispose(false);
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

        public QueryRequestOptions FeedOptions
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
            get { ThrowIfDisposed(); return _currentContext.IdentityDocumentCollection; }
            set { _currentContext.IdentityDocumentCollection = value; }
        }

        protected void ThrowIfDisposed()
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(base.GetType().Name);
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
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
