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

            if (where == null)
            {
                List<TSource> list = source as List<TSource>;
                if (list != null)
                {
                    return Task.FromResult(list);
                }
                return Task.Run(() => { return source.ToList(); }, cancellationToken);
            }
            else
                return Task.Run(() => { return source.Where(where).ToList(); }, cancellationToken);
        }

        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> where = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => {
                if (where == null)
                    return source.FirstOrDefault();
                return source.FirstOrDefault(where);
            }, cancellationToken);
        }

        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> where = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() => {
                if(where == null)
                    return source.SingleOrDefault();
                return source.Where(where).SingleOrDefault();
            }, cancellationToken);
        }
    }
}
