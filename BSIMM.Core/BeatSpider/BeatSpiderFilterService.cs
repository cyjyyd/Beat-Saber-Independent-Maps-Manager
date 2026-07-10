using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Preset;

namespace BeatSaberIndependentMapsManager.BeatSpiderSharp;

public class BeatSpiderFilterService : IDisposable
{
    private readonly BsimSpider _spider;

    public BeatSpiderFilterService(bool verbose = false)
    {
        _spider = new BsimSpider(verbose);
    }

    public async Task<List<BeatSaverMap>> FilterAsync(
        IAsyncEnumerable<BeatSaverMap> source, FilterPreset filterPreset,
        CancellationToken cToken = default)
    {
        var preset = BsfToPresetConverter.Convert(filterPreset);
        var songs = source.Select(map => map.ToBeatSpiderSong());
        var filtered = _spider.Filter(songs, preset);
        var results = new List<BeatSaverMap>();

        await foreach (var song in filtered.WithCancellation(cToken))
        {
            var map = song.ToBeatSaverMap();
            if (map != null)
                results.Add(map);
        }

        return results;
    }

    public async Task<List<BeatSaverMapSlim>> FilterSlimAsync(
        IAsyncEnumerable<BeatSaverMapSlim> source, FilterPreset filterPreset,
        CancellationToken cToken = default)
    {
        var maps = source.Select(s => s.ToFullMap());
        var fullResults = await FilterAsync(maps, filterPreset, cToken);
        return fullResults.Select(m => ToSlim(m)).ToList();
    }

    private static BeatSaverMapSlim ToSlim(BeatSaverMap map)
    {
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(map);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<BeatSaverMapSlim>(json);
    }

    public void Dispose()
    {
        ((IDisposable)_spider).Dispose();
        GC.SuppressFinalize(this);
    }
}
