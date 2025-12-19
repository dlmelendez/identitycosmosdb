using System;
using System.Text.Json.Serialization;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Model
{
    interface IResource<TKey> where TKey : IEquatable<TKey>
    {
        [JsonPropertyName("id")]
        TKey Id { get; set; }

        [JsonPropertyName("_etag")]
        string ETag { get; set; }

        string PartitionKey { get; set; }
        void SetPartitionKey();

    }
}
