// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Helpers
{
    public static class HashHelper
    {
        public static SHA256 sha = SHA256.Create();

        public static string ConvertToHash(string input)
        {
            return GetHash(sha, input);
        }

        private static string GetHash(SHA256 shaHash, string input)
        {
            // Convert the input string to a byte array and compute the hash. 
            byte[] data = shaHash.ComputeHash(Encoding.UTF8.GetBytes(input));
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
    }
}
