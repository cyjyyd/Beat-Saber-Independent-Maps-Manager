using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberIndependentMapsManager.BeatSpiderSharp;
using BeatSpiderSharp.Extensions;
using BeatSpiderSharp.Models;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager.Services
{
    public class BeatSpiderPlaylistService
    {
        public async Task ExportPlaylistAsync(
            List<BeatSaverMap> maps, string title, string author, string targetPath,
            Stream coverStream, CancellationToken cToken = default)
        {
            var songs = maps.Select(m => m.ToBeatSpiderSong()).ToList();
            var playlist = new BeatSaberPlaylistsLib.Legacy.LegacyPlaylist(title, title,
                string.IsNullOrWhiteSpace(author) ? null : author)
            { ReadOnly = true };

            if (coverStream != null)
            {
                await coverStream.FlushAsync(cToken);
                coverStream.Position = 0;
                playlist.SetCover(coverStream);
            }

            foreach (var song in songs)
            {
                playlist.Add(song.Hash, song.BeatSaverSong.Metadata?.SongName, song.Bsr, null);
            }

            var serializer = JsonSerializer.Create(new JsonSerializerSettings { Formatting = Formatting.Indented });
            serializer.Serialize(playlist, targetPath);
        }
    }
}
