using System.Collections.Concurrent;
using BeautyCommerce.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace BeautyCommerce.Tests.Cache;

// 6.3.8: stresses the exact mechanism flagged as a theoretical risk during
// the 6.3 read-only audit — MemoryCacheService.InvalidateTagAsync disposes
// the tag's CancellationTokenSource while a concurrent SetAsync may have
// already captured a reference to it via GetOrAdd. This runs many writers
// and invalidators against the SAME tag at once and checks for exactly the
// four failure modes called out in the audit:
//   1. exceptions from a disposed CancellationTokenSource;
//   2. entries that incorrectly survive an invalidation;
//   3. inconsistent/corrupted tag tokens;
//   4. stale data reappearing after an invalidation.
// If this passes, MemoryCacheService is not modified — per the explicit
// rule for 6.3, infrastructure only changes in response to a reproduced
// failure, not a theoretical one.
public class MemoryCacheServiceConcurrencyTests
{
    [Fact]
    public async Task Concurrent_SetAsync_And_InvalidateTagAsync_Never_Throw_Or_Leak_Stale_Entries()
    {
        var cache = new MemoryCacheService(new MemoryCache(new MemoryCacheOptions()));
        const string tag = "StressTag";
        const int writers = 8;
        const int invalidators = 4;
        const int iterationsPerWriter = 250;
        const int iterationsPerInvalidator = 150;

        var exceptions = new ConcurrentBag<Exception>();
        var writtenKeys = new ConcurrentBag<string>();

        var writerTasks = Enumerable.Range(0, writers).Select(w => Task.Run(async () =>
        {
            for (var i = 0; i < iterationsPerWriter; i++)
            {
                var key = $"key-{w}-{i}";
                try
                {
                    await cache.SetAsync(key, $"value-{w}-{i}", TimeSpan.FromMinutes(5), tag);
                    writtenKeys.Add(key);
                }
                catch (Exception ex)
                {
                    // Failure mode 1: an ObjectDisposedException (or any
                    // other exception) from racing a concurrent
                    // InvalidateTagAsync that disposed the CTS this SetAsync
                    // was about to register against.
                    exceptions.Add(ex);
                }
            }
        }));

        var invalidatorTasks = Enumerable.Range(0, invalidators).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < iterationsPerInvalidator; i++)
            {
                try
                {
                    await cache.InvalidateTagAsync(tag);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }

                await Task.Yield();
            }
        }));

        await Task.WhenAll(writerTasks.Concat(invalidatorTasks));

        exceptions.Should().BeEmpty(
            "SetAsync and InvalidateTagAsync racing on the same tag must never throw, " +
            "even when a CancellationTokenSource is disposed mid-registration");

        // Failure mode 2 & 4: force one final, unambiguous invalidation
        // after the chaos settles, then confirm literally none of the
        // hundreds of keys written during the storm are still retrievable.
        // Anything that survives this is either an entry that dodged
        // invalidation (mode 2) or stale data that should be gone (mode 4).
        await cache.InvalidateTagAsync(tag);

        var survivors = new List<string>();

        foreach (var key in writtenKeys.Distinct())
        {
            var value = await cache.GetAsync<string>(key);
            if (value != null)
                survivors.Add(key);
        }

        survivors.Should().BeEmpty(
            "no entry may survive a final explicit invalidation, even after heavy concurrent Set/Invalidate traffic");

        // Failure mode 3: the tag mechanism itself must still work
        // correctly after the storm — not left in some corrupted state
        // where invalidation silently stops doing anything.
        await cache.SetAsync("post-storm-key", "post-storm-value", TimeSpan.FromMinutes(5), tag);

        var beforeInvalidate = await cache.GetAsync<string>("post-storm-key");
        beforeInvalidate.Should().Be("post-storm-value");

        await cache.InvalidateTagAsync(tag);

        var afterInvalidate = await cache.GetAsync<string>("post-storm-key");
        afterInvalidate.Should().BeNull("the tag token mechanism must still invalidate correctly after concurrent stress, not just before it");
    }
}
