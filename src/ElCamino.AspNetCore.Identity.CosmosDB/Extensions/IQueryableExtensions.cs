// MIT License Copyright 2019 (c) David Melendez. All rights reserved. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElCamino.AspNetCore.Identity.CosmosDB.Extensions
{
    internal static class IQueryableExtensions
    {
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

        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> where = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (where == null)
                return Task.FromResult(source.FirstOrDefault());
            return Task.FromResult(source.FirstOrDefault(where));
        }

        public static Task<List<TSource>> ToListAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> where = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (where == null)
            {
                List<TSource> list = source as List<TSource>;
                if (list != null)
                {
                    return Task.FromResult(list);
                }
                return Task.FromResult(source.ToList());
            }
            return Task.FromResult(source.Where(where).ToList());
        }

        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> where = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (where == null)
                return Task.FromResult(source.SingleOrDefault());
            return Task.FromResult(source.Where(where).SingleOrDefault());
        }
    }
}
