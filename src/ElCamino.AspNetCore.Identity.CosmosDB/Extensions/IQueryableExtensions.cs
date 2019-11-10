// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Extensions
{
    internal static class IQueryableExtensions
    {
        public static Task<List<TSource>> ToListAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> where = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if(where == null)
                return Task.Run(() => { return source.ToList(); }, cancellationToken);
            else
                return Task.Run(() => { return source.Where(where).ToList(); }, cancellationToken);
        }

        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> where = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => {
                if (where == null)
                    return source.Where(where).FirstOrDefault();
                return source.Where(where).FirstOrDefault(where);
            }, cancellationToken);
        }

        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> where = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => {
                if(where == null)
                    return source.Where(where).SingleOrDefault();
                return source.Where(where).SingleOrDefault(where);
            }, cancellationToken);
        }
    }
}
