// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElCamino.AspNetCore.Identity.DocumentDB.Model;
using Microsoft.Azure.Documents;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;

namespace ElCamino.AspNetCore.Identity.DocumentDB
{
    public class IdentityCloudContext : IDisposable
    {
        internal class InternalContext : IDisposable
        {
            private DocumentClient _client = null;
            private Database _db = null;
            private DocumentCollection _identityDocumentCollection;
            private StoredProcedure _getUserByEmailSproc = null;
            private StoredProcedure _getUserByUserNameSproc = null;
            private StoredProcedure _getUserByIdSproc = null;
            private StoredProcedure _getUserByLoginSproc = null;
            private string _sessionToken = string.Empty;
            private bool _disposed = false;

            public StoredProcedure GetUserByLoginSproc
            {
                get { return _getUserByLoginSproc; }
            }

            public StoredProcedure GetUserByIdSproc
            {
                get { return _getUserByIdSproc; }
            }

            public StoredProcedure GetUserByUserNameSproc
            {
                get { return _getUserByUserNameSproc; }
            }

            public StoredProcedure GetUserByEmailSproc
            {
                get { return _getUserByEmailSproc; }
            }


            public InternalContext(IdentityConfiguration config)
            {
                _client = new DocumentClient(new Uri(config.Uri), config.AuthKey, config.Policy, ConsistencyLevel.Session);
                InitDatabase(config.Database);
                _identityDocumentCollection = new DocumentCollection() { Id = config.IdentityCollection };

                InitCollection(config.IdentityCollection);
                InitStoredProcs();
            }

            private void InitDatabase(string database)
            {
                _db = _client.CreateDatabaseIfNotExistsAsync(new Database { Id = database }).Result.Resource;
            }

            private void InitCollection(string userCollectionId)
            {
                var ucresult = _client.CreateDocumentCollectionIfNotExistsAsync(_db.SelfLink, _identityDocumentCollection, this.RequestOptions).Result;
                IdentityDocumentCollection = ucresult.Resource;
            }

            private void InitStoredProcs()
            {
                InitGetUserByEmail();
                InitGetUserByUserName();
                InitGetUserById();
                InitGetUserByLogin();
            }

            private void InitGetUserByLogin()
            {
                string body = string.Empty;

                using (StreamReader sr = new StreamReader(typeof(IdentityCloudContext).GetTypeInfo().Assembly.GetManifestResourceStream(
                    "ElCamino.AspNetCore.Identity.DocumentDB.StoredProcs.getUserByLogin_sproc.js"), Encoding.UTF8))
                {
                    body = sr.ReadToEnd();
                }
                string strId = "getUserByLogin_v1";
                if (_getUserByLoginSproc == null)
                {
                    var task = _client.UpsertStoredProcedureAsync(_identityDocumentCollection.SelfLink,
                        new StoredProcedure()
                        {
                            Id = strId,
                            Body = body,
                        },
                        RequestOptions);
                    _getUserByLoginSproc = task.Result;
                }
            }

            private void InitGetUserById()
            {
                string body = string.Empty;

                using (StreamReader sr = new StreamReader(typeof(IdentityCloudContext).GetTypeInfo().Assembly.GetManifestResourceStream(
                    "ElCamino.AspNetCore.Identity.DocumentDB.StoredProcs.getUserById_sproc.js"), Encoding.UTF8))
                {
                    body = sr.ReadToEnd();
                }
                string strId = "getUserById_v1";
                if (_getUserByIdSproc == null)
                {
                    var task = _client.UpsertStoredProcedureAsync(_identityDocumentCollection.SelfLink,
                        new StoredProcedure()
                        {
                            Id = strId,
                            Body = body,
                        },
                        RequestOptions);
                    _getUserByIdSproc = task.Result;
                }
            }

            private void InitGetUserByUserName()
            {
                string body = string.Empty;

                using (StreamReader sr = new StreamReader(typeof(IdentityCloudContext).GetTypeInfo().Assembly.GetManifestResourceStream(
                    "ElCamino.AspNetCore.Identity.DocumentDB.StoredProcs.getUserByUserName_sproc.js"), Encoding.UTF8))
                {
                    body = sr.ReadToEnd();
                }
                string strId = "getUserByUserName_v1";

                if (_getUserByUserNameSproc == null)
                {
                    var task = _client.UpsertStoredProcedureAsync(_identityDocumentCollection.SelfLink,
                        new StoredProcedure()
                        {
                            Id = strId,
                            Body = body,
                        },
                        RequestOptions);
                    _getUserByUserNameSproc = task.Result;
                }
            }

            private void InitGetUserByEmail()
            {
                string body = string.Empty;

                using (StreamReader sr = new StreamReader(typeof(IdentityCloudContext).GetTypeInfo().Assembly.GetManifestResourceStream(
                    "ElCamino.AspNetCore.Identity.DocumentDB.StoredProcs.getUserByEmail_sproc.js")))
                {
                    body = sr.ReadToEnd();
                }
                string strId = "getUserByEmail_v1";
                if (_getUserByEmailSproc == null)
                {
                    var task = _client.UpsertStoredProcedureAsync(_identityDocumentCollection.SelfLink,
                        new StoredProcedure()
                        {
                            Id = strId,
                            Body = body,
                        },
                        RequestOptions);
                    _getUserByEmailSproc = task.Result;
                }
            }

            ~InternalContext()
            {
                this.Dispose(false);
            }

            public DocumentClient Client
            {
                get { ThrowIfDisposed(); return _client; }
            }

            public Database Database
            {
                get { ThrowIfDisposed(); return _db; }
            }


            public RequestOptions RequestOptions
            {
                get
                {
                    return new RequestOptions()
                    {
                        ConsistencyLevel = ConsistencyLevel.Session,
                        SessionToken = SessionToken,
                    };
                }
            }

            public FeedOptions FeedOptions
            {
                get
                {
                    return new FeedOptions()
                    {
                        EnableCrossPartitionQuery = true,
                        EnableScanInQuery = true,
                        SessionToken = SessionToken,
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

            public DocumentCollection IdentityDocumentCollection
            {
                get { ThrowIfDisposed(); return _identityDocumentCollection; }
                set { _identityDocumentCollection = value; }
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
                    if (_client != null)
                    {
                        _client.Dispose();
                    }
                    _disposed = true;
                    _client = null;
                    _db = null;
                    _identityDocumentCollection = null;
                    _sessionToken = null;
                }
            }
        }
        private bool _disposed = false;

        //Thread safe dictionary 
        private static readonly ConcurrentDictionary<string, InternalContext> ContextCache = new ConcurrentDictionary<string, InternalContext>();
        private string _configHash = null;
        private InternalContext _currentContext = null;

        public StoredProcedure GetUserByLoginSproc
        {
            get { return _currentContext.GetUserByLoginSproc; }
        }

        public StoredProcedure GetUserByIdSproc
        {
            get { return _currentContext.GetUserByIdSproc; }
        }

        public StoredProcedure GetUserByUserNameSproc
        {
            get { return _currentContext.GetUserByUserNameSproc; }
        }

        public StoredProcedure GetUserByEmailSproc
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

        public DocumentClient Client
        {
            get { ThrowIfDisposed(); return _currentContext.Client; }
        }

        public Database Database
        {
            get { ThrowIfDisposed(); return _currentContext.Database; }
        }


        public RequestOptions RequestOptions
        {
            get
            {
                return _currentContext.RequestOptions;
            }
        }

        public FeedOptions FeedOptions
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

        public DocumentCollection IdentityDocumentCollection
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
