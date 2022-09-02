using Newtonsoft.Json;
using System;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    interface IResource<TKey> where TKey : IEquatable<TKey>
    {
        [JsonProperty(PropertyName = "id")]
        TKey Id { get; set; }

        [JsonProperty(PropertyName = "_etag")]
        string ETag { get; set; }

        string PartitionKey { get; set; }
        void SetPartitionKey();

    }
}
