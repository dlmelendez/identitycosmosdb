using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    interface IResource<TKey> where TKey : IEquatable<TKey>
    {
        [JsonProperty(PropertyName = "id")]
        TKey Id { get; set; }
        [JsonProperty(PropertyName = "_rid")]
        string ResourceId { get; set; }
        [JsonProperty(PropertyName = "_self")]
        string SelfLink { get; set;}
        [JsonIgnore]
        string AltLink { get; set; }
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [JsonProperty(PropertyName = "_ts")]
        DateTime Timestamp { get; set; }
        [JsonProperty(PropertyName = "_etag")]
        string ETag { get; set; }
    }
}
