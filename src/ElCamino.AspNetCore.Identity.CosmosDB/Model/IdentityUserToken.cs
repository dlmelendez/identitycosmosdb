// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Text.Json.Serialization;
using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    public class IdentityUserToken : IdentityUserToken<string> 
    {
        public IdentityUserToken()
        {
            Id = Guid.NewGuid().ToString();
        }
    }


    public class IdentityUserToken<TKey> : Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>,
        IResource<TKey> where TKey : IEquatable<TKey>
    {
        private TKey _Id;

        [JsonPropertyName("id")]
        public TKey Id
        {
            get => _Id;
            set
            {
                _Id = value;
                SetPartitionKey();
            }
        }

        public void SetPartitionKey()
        {
            PartitionKey = PartitionKeyHelper.GetPartitionKeyFromId(Id?.ToString());
        }

        [JsonPropertyName("_rid")]
        public virtual string ResourceId { get; set; }

        [JsonPropertyName("_etag")]
        public virtual string ETag { get; set; }

        public string PartitionKey { get; set; }

        public override TKey UserId { get; set; }

        public override string LoginProvider { get; set; }

        public override string Name { get; set; }

        public override string Value { get; set; }
    }
}
