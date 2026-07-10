using System;
using System.Collections.Generic;
using System.Linq;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.BeatSaver;
using Newtonsoft.Json;

namespace BeatSaberIndependentMapsManager.BeatSpiderSharp;

public static class BeatSpiderModelConverter
{
    private static readonly JsonSerializerSettings _serializerSettings = new()
    {
        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static Song ToBeatSpiderSongModel(this BeatSaverMap map)
    {
        var json = JsonConvert.SerializeObject(map);
        return JsonConvert.DeserializeObject<Song>(json, _serializerSettings)!;
    }

    public static BeatSpiderSong ToBeatSpiderSong(this BeatSaverMap map)
    {
        return BeatSpiderSong.FromBeatSaverSong(ToBeatSpiderSongModel(map));
    }

    public static IAsyncEnumerable<BeatSpiderSong> ToBeatSpiderSongs(
        this IAsyncEnumerable<BeatSaverMap> maps)
    {
        return maps.Select(m => m.ToBeatSpiderSong());
    }

    public static BeatSaverMap ToBeatSaverMap(this BeatSpiderSong song)
    {
        var json = JsonConvert.SerializeObject(song.BeatSaverSong);
        return JsonConvert.DeserializeObject<BeatSaverMap>(json, _serializerSettings);
    }
}
