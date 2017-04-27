// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Helpers
{
    public static class JsonHelpers
    {
        public static T CreateObject<T>(string json) where T : class
        {
            var t = Activator.CreateInstance<T>();
            JsonConvert.PopulateObject(json, t);
            return t;
        }
    }
}
