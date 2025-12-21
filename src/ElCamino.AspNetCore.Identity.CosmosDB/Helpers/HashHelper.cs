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

            // Convert the input string to a byte array and compute the hash. 
            Span<byte> encodedBytes = stackalloc byte[Encoding.UTF8.GetMaxByteCount(input.Length)];
            int encodedByteCount = Encoding.UTF8.GetBytes(input, encodedBytes);

            byte[] data = shaHash.ComputeHash(encodedBytes.ToArray());
            Console.WriteLine(string.Format("Key Size before hash: {0} bytes", Encoding.UTF8.GetBytes(input).Length));

            // Create a new Stringbuilder to collect the bytes 
            // and create a string.
            StringBuilder sBuilder = new StringBuilder(32);

            // Loop through each byte of the hashed data  
            // and format each one as a hexadecimal string. 
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("X2"));
            }
            Console.WriteLine(string.Format("Key Size after hash: {0} bytes", data.Length));

            // Return the hexadecimal string. 
            return sBuilder.ToString();
        }
#endif
    }
}
