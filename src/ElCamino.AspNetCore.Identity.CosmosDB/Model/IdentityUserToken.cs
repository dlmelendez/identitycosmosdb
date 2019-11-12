// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ElCamino.AspNetCore.Identity.CosmosDB.Helpers;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUserToken : IdentityUserToken<string> 
    {
        public IdentityUserToken()
        {
            Id = Guid.NewGuid().ToString();
        }
    }


    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class IdentityUserToken<TKey> : Microsoft.AspNetCore.Identity.IdentityUserToken<TKey>,
        IResource<TKey> where TKey : IEquatable<TKey>
    {
        private TKey _Id;

        [JsonProperty(PropertyName = "id")]
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

        [JsonProperty(PropertyName = "_rid")]
        public virtual string ResourceId { get; set; }

        [JsonProperty(PropertyName = "_etag")]
        public virtual string ETag { get; set; }

        public string PartitionKey { get; set; }


        [JsonProperty("userId")]
        public override TKey UserId { get; set; }

        [JsonProperty("loginProvider")]
        public override string LoginProvider { get; set; }

        [JsonProperty("name")]
        public override string Name { get; set; }

        [JsonProperty("value")]
        public override string Value { get; set; }
    }
}