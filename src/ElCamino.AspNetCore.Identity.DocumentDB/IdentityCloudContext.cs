// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
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

namespace ElCamino.AspNetCore.Identity.DocumentDB
{
    public class IdentityCloudContext : IDisposable       
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
       

        public IdentityCloudContext(IdentityConfiguration config)
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
            _getUserByLoginSproc = _client.CreateStoredProcedureQuery(_identityDocumentCollection.StoredProceduresLink,
                this.FeedOptions).Where(s => s.Id == strId).ToList().FirstOrDefault();
            //if (_getUserByLoginSproc != null)
            //{
            //    var task = _client.DeleteStoredProcedureAsync(_getUserByLoginSproc.SelfLink,
            //        RequestOptions);
            //    task.Wait();
            //    _getUserByLoginSproc = null;
            //}
            if (_getUserByLoginSproc == null)
            {
                var task = _client.CreateStoredProcedureAsync(_identityDocumentCollection.SelfLink,
                    new StoredProcedure()
                    {
                        Id = strId,
                        Body = body,
                    },
                    RequestOptions);
                task.Wait();
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
            _getUserByIdSproc = _client.CreateStoredProcedureQuery(_identityDocumentCollection.StoredProceduresLink,
                this.FeedOptions).Where(s => s.Id == strId).ToList().FirstOrDefault();
            //if (_getUserByIdSproc != null)
            //{
            //    var task = _client.DeleteStoredProcedureAsync(_getUserByIdSproc.SelfLink,
            //        RequestOptions);
            //    task.Wait();
            //    _getUserByIdSproc = null;
            //}
            if (_getUserByIdSproc == null)
            {
                var task = _client.CreateStoredProcedureAsync(_identityDocumentCollection.SelfLink,
                    new StoredProcedure()
                    {
                        Id = strId,
                        Body = body,
                    },
                    RequestOptions);
                task.Wait();
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
            _getUserByUserNameSproc = _client.CreateStoredProcedureQuery(_identityDocumentCollection.StoredProceduresLink,
                this.FeedOptions).Where(s => s.Id == strId).ToList().FirstOrDefault();
            //if (_getUserByUserNameSproc != null)
            //{
            //    var task = _client.DeleteStoredProcedureAsync(_getUserByUserNameSproc.SelfLink,
            //        RequestOptions);
            //    task.Wait();
            //    _getUserByUserNameSproc = null;
            //}
            if (_getUserByUserNameSproc == null)
            {
                var task = _client.CreateStoredProcedureAsync(_identityDocumentCollection.SelfLink,
                    new StoredProcedure()
                    {
                        Id = strId,
                        Body = body,
                    },
                    RequestOptions);
                task.Wait();
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
            _getUserByEmailSproc = _client.CreateStoredProcedureQuery(_identityDocumentCollection.StoredProceduresLink,
                this.FeedOptions).Where(s => s.Id == strId).ToList().FirstOrDefault();
            //if (_getUserByEmailSproc != null)
            //{
            //    var task = _client.DeleteStoredProcedureAsync(_getUserByEmailSproc.SelfLink,
            //        RequestOptions);
            //    task.Wait();
            //    _getUserByEmailSproc = null;
            //}
            if (_getUserByEmailSproc == null)
            {
                var task = _client.CreateStoredProcedureAsync(_identityDocumentCollection.SelfLink,
                    new StoredProcedure()
                    {
                        Id = strId,
                        Body = body,
                    },
                    RequestOptions);
                task.Wait();
                _getUserByEmailSproc = task.Result;
            }
        }

        ~IdentityCloudContext()
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

}
