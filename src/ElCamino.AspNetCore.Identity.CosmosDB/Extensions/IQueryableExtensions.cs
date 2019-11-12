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
#if NETSTANDARD2_1
        public static async Task<T> FirstOrDefaultAsync<T>(
                    this IAsyncEnumerable<T> asyncEnumerable,
                    CancellationToken cancellationToken = default)
        {
            await using (var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken))
            {
                if (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    return enumerator.Current;
                }
                return default;
            }
        }

        public static async Task<List<T>> ToListAsync<T>(
            this IAsyncEnumerable<T> asyncEnumerable,
            CancellationToken cancellationToken = default)
        {
            await using (var enumerator = asyncEnumerable.GetAsyncEnumerator(cancellationToken))
            {
                List<T> list = new List<T>();
                while (await enumerator.MoveNextAsync().ConfigureAwait(false))
                {
                    list.Add(enumerator.Current);
                }
                return list;
            }
        }
#endif
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
