using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeatSpiderSharp.Core;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Preset;

namespace BeatSaberIndependentMapsManager.BeatSpiderSharp;

public class BsimSpider : BeatSpider
{
    public BsimSpider(bool verbose = false) : base(verbose)
    {
    }

    public IAsyncEnumerable<BeatSpiderSong> Filter(
        IAsyncEnumerable<BeatSpiderSong> songs, Preset preset)
    {
        return FilterSongs(songs, preset);
    }

    public Task<int> Output(
        IAsyncEnumerable<BeatSpiderSong> songs, Preset preset,
        CancellationToken cToken = default)
    {
        return OutputSongsAsync(songs, preset, cToken);
    }
}
