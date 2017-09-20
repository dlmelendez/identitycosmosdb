// MIT License Copyright 2017 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using ElCamino.AspNetCore.Identity.DocumentDB.Model;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ElCamino.AspNetCore.Identity.DocumentDB.Helpers
{
    public static class JsonHelpers
    {
        public static T CreateObject<T>(Document doc) where T : new()
        {
            var t = JsonConvert.DeserializeObject<T>(doc.ToString());
            var r = t as IResource<string>;
            if(r != null)
            {
                r.AltLink = doc.AltLink;
                r.ETag = doc.ETag;
                r.ResourceId = doc.ResourceId;
                r.SelfLink = doc.SelfLink;
                r.Timestamp = doc.Timestamp;
            }
            return t;
        }
    }
}
