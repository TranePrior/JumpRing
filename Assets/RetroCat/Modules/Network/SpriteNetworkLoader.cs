using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RetroCat.Modules.Network
{
    public static class SpriteNetworkLoader
    {
        public sealed class IconLoadOptions
        {
            public Vector2 Pivot { get; set; } = new(0.5f, 0.5f);
            public float PixelsPerUnit { get; set; } = 100f;
            public uint Extrude { get; set; } = 0;
            public SpriteMeshType MeshType { get; set; } = SpriteMeshType.FullRect;
            public bool Readable { get; set; } = false;
            public int TimeoutSeconds { get; set; } = 15;
            public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(10);
        }

        private sealed class CacheEntry
        {
            public Sprite Sprite;
            public DateTime ExpiresAtUtc;
        }

        private static readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Task<Sprite>> _inFlight = new(StringComparer.Ordinal);
        
        public static Task<Sprite> LoadSpriteAsync(
            string url,
            IconLoadOptions? options = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL is null/empty.", nameof(url));

            IconLoadOptions opt = options ?? new IconLoadOptions();

            if (_cache.TryGetValue(url, out var entry))
            {
                if (entry.Sprite != null && DateTime.UtcNow <= entry.ExpiresAtUtc)
                    return Task.FromResult(entry.Sprite);

                _cache.Remove(url);
            }

            if (_inFlight.TryGetValue(url, out var existingTask))
                return existingTask;

            var task = LoadSpriteInternalAsync(url, opt, ct);

            _inFlight[url] = task;

            _ = task.ContinueWith(_ =>
            {
                _inFlight.Remove(url);
            }, TaskScheduler.FromCurrentSynchronizationContext());

            return task;
        }

        private static async Task<Sprite> LoadSpriteInternalAsync(string url, IconLoadOptions opt, CancellationToken ct)
        {
            using var request = UnityWebRequestTexture.GetTexture(url, nonReadable: !opt.Readable);
            request.timeout = Mathf.Max(1, opt.TimeoutSeconds);

            using var reg = ct.Register(() =>
            {
                try
                {
                    request.Abort();
                }
                catch
                {
                    
                }
            });

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
                throw new Exception($"Icon load failed. url={url}, error={request.error}");

            var texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
                throw new Exception($"Downloaded texture is null. url={url}");

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                opt.Pivot,
                opt.PixelsPerUnit,
                opt.Extrude,
                opt.MeshType
            );

            _cache[url] = new CacheEntry
            {
                Sprite = sprite,
                ExpiresAtUtc = DateTime.UtcNow.Add(opt.CacheTtl)
            };

            return sprite;
        }
        
        public static void ClearCache(bool destroySprites = true)
        {
            if (destroySprites)
            {
                foreach (var kv in _cache)
                {
                    if (kv.Value?.Sprite != null)
                    {
                        UnityEngine.Object.Destroy(kv.Value.Sprite);
                    }
                }
            }

            _cache.Clear();
        }
        
        public static void Invalidate(string url, bool destroySprite = true)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            if (_cache.TryGetValue(url, out var entry))
            {
                if (destroySprite && entry.Sprite != null)
                    UnityEngine.Object.Destroy(entry.Sprite);

                _cache.Remove(url);
            }
        }
    }
}