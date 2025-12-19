using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Helpers
{
    /// <summary>
    /// Used for simple object to JSON serialization needs
    /// </summary>
    internal static class JsonHelper
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        /// <summary>
        /// Default JsonSerializerOptions for serialization, web API use cases
        /// </summary>
        public static JsonSerializerOptions JsonOptions => _jsonOptions;
    }
}
