namespace ElCamino.AspNetCore.Identity.CosmosDB.Helpers
{
    public static class PartitionKeyHelper
    {
        public static string GetPartitionKeyFromId(string id)
        {
            id = id?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (id.Length > 4)
                {
                    return id.Substring(id.Length - 4, 4);
                }
            }

            return string.Empty;
        }
    }
}
