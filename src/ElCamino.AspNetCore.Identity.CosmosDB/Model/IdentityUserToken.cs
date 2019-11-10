// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Azure.Documents;

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

        [JsonProperty(PropertyName = "id")]
        public TKey Id { get; set; }

        [JsonProperty(PropertyName = "_rid")]
        public virtual string ResourceId { get; set; }

        [JsonProperty(PropertyName = "_self")]
        public virtual string SelfLink { get; set; }

        [JsonIgnore]
        public string AltLink { get; set; }

        [JsonConverter(typeof(UnixDateTimeConverter))]
        [JsonProperty(PropertyName = "_ts")]
        public virtual DateTime Timestamp { get; set; }

        [JsonProperty(PropertyName = "_etag")]
        public virtual string ETag { get; set; }



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