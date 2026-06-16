using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager.Services
{
    internal class HashCacheService
    {
        private const string CacheFileName = "hash.cache";
        private Dictionary<string, string> _songsHash;

        private readonly object _hashLock = new object();

        public Dictionary<string, string> SongsHash
        {
            get
            {
                lock (_hashLock) return _songsHash;
            }
            set
            {
                lock (_hashLock) _songsHash = value;
            }
        }

        public bool HasCachedData
        {
            get
            {
                lock (_hashLock) return _songsHash != null && _songsHash.Count > 0;
            }
        }

        /// <summary>
        /// Load hash cache from disk.
        /// </summary>
        public void LoadCache()
        {
            if (!File.Exists(CacheFileName))
                return;

            try
            {
                byte[] encryptedHash = Convert.FromBase64String(File.ReadAllText(CacheFileName));
                _songsHash = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                    System.Text.Encoding.UTF8.GetString(encryptedHash));
            }
            catch (JsonException)
            {
                File.Delete(CacheFileName);
            }
        }

        /// <summary>
        /// Save hash cache to disk.
        /// </summary>
        public void SaveCache()
        {
            Dictionary<string, string> copy;
            lock (_hashLock)
            {
                if (_songsHash == null)
                    return;
                copy = new Dictionary<string, string>(_songsHash);
            }

            string hashResults = JsonConvert.SerializeObject(copy, Formatting.None);
            File.WriteAllText(CacheFileName,
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(hashResults)));
        }

        /// <summary>
        /// Ensure hashes for all songs in a music pack are cached.
        /// </summary>
        public async Task EnsurePackHashesAsync(
            Dictionary<string, SongMap> packSongs,
            Action<int> onProgress = null)
        {
            lock (_hashLock)
            {
                if (_songsHash == null)
                {
                    _songsHash = new Dictionary<string, string>();
                }
            }

            int processed = 0;
            int total = packSongs.Count;

            foreach (var kvp in packSongs)
            {
                bool containsKey;
                lock (_hashLock)
                {
                    containsKey = _songsHash.ContainsKey(kvp.Key);
                }

                if (!containsKey)
                {
                    string hash = await ComputeSongHashAsync(kvp.Value);
                    lock (_hashLock)
                    {
                        _songsHash[kvp.Key] = hash;
                    }
                }
                processed++;
                onProgress?.Invoke(processed * 100 / total);
            }
        }

        /// <summary>
        /// Get hash for a single song (from cache or compute).
        /// </summary>
        public async Task<string> GetOrComputeHashAsync(string key, SongMap songMap)
        {
            lock (_hashLock)
            {
                if (_songsHash != null && _songsHash.TryGetValue(key, out string cached))
                    return cached;
                if (_songsHash == null)
                    _songsHash = new Dictionary<string, string>();
            }

            string hash = await ComputeSongHashAsync(songMap);
            
            lock (_hashLock)
            {
                _songsHash[key] = hash;
            }
            return hash;
        }

        /// <summary>
        /// Compute SHA1 hash of song files (Info.dat + difficulty files).
        /// </summary>
        public static async Task<string> ComputeSongHashAsync(SongMap songMap)
        {
            using SHA1 sha1 = SHA1.Create();
            string[] files = new[] { songMap.songFolder + "\\Info.dat" }
                .Concat(songMap.GetDifficultiesFiles()
                    .Select(f => songMap.songFolder + "\\" + f))
                .ToArray();

            foreach (string filePath in files)
            {
                if (!File.Exists(filePath)) continue;

                using FileStream fileStream = new FileStream(
                    filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
                byte[] buffer = new byte[65536];
                int bytesRead;
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
                }
            }

            sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[] hashBytes = sha1.Hash;
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Delete the cache file from disk.
        /// </summary>
        public void DeleteCache()
        {
            if (File.Exists(CacheFileName))
                File.Delete(CacheFileName);
            _songsHash = null;
        }

        public void Clear()
        {
            _songsHash?.Clear();
        }
    }
}
