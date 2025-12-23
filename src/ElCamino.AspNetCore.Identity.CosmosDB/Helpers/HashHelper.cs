// MIT License Copyright (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Cosmos.Core;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Helpers
{
    public static class HashHelper
    {
        public static string ConvertToHash(string input)
        {
            using SHA256 sha = SHA256.Create();
            return GetHash(sha, input);
        }

#if NET8_0_OR_GREATER
        public static string GetHash(SHA256 shaHash, ReadOnlySpan<char> input)
        {
            Span<byte> encodedBytes = stackalloc byte[Encoding.UTF8.GetMaxByteCount(input.Length)];
            int encodedByteCount = Encoding.UTF8.GetBytes(input, encodedBytes);

            Span<byte> hashedBytes = stackalloc byte[SHA256.HashSizeInBytes];
            int hashedByteCount = SHA256.HashData(encodedBytes.Slice(0, encodedByteCount), hashedBytes);

            return Convert.ToHexString(hashedBytes.Slice(0, hashedByteCount));
        }

#else
        private static string GetHash(SHA256 shaHash, string input)
        {
            // Convert the input string to a byte array and compute the hash
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = shaHash.ComputeHash(inputBytes);

            // Convert byte array to hexadecimal string more efficiently
            return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
        }
#endif
    }
}
