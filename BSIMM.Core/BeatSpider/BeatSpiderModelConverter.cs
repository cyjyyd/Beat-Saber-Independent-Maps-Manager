using System;
using System.Linq;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.BeatSaver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        var obj = JObject.Parse(json);

        // BSIMM stores duration as double but BeatSpiderSharp expects an integer;
        // a plain JSON round-trip would fail on values like "120.0".
        var duration = obj["metadata"]?["duration"];
        if (duration != null && duration.Type == JTokenType.Float)
            obj["metadata"]["duration"] = (int)Math.Round((double)duration);

        SanitizeDateFields(obj);

        return obj.ToObject<Song>(JsonSerializer.Create(_serializerSettings))!;
    }

    private static void SanitizeDateFields(JObject obj)
    {
        // BSIMM models use non-nullable DateTime that defaults to DateTime.MinValue,
        // which DateTimeOffset cannot represent and would break the BSS round-trip.
        foreach (var field in new[] { "createdAt", "updatedAt", "lastPublishedAt", "uploaded" })
        {
            var token = obj[field];
            if (token != null && token.Type != JTokenType.Null && IsOutOfRangeDate(token))
                obj.Remove(field);
        }

        var versions = obj["versions"] as JArray;
        if (versions != null)
        {
            foreach (var version in versions.OfType<JObject>())
            {
                var created = version["createdAt"];
                if (created != null && IsOutOfRangeDate(created))
                    version.Remove("createdAt");
            }
        }
    }

    private static bool IsOutOfRangeDate(JToken token)
    {
        try
        {
            var value = token.Value<DateTime>();
            _ = new DateTimeOffset(value);
            return false;
        }
        catch
        {
            return true;
        }
    }

    public static BeatSpiderSong ToBeatSpiderSong(this BeatSaverMap map)
    {
        return BeatSpiderSong.FromBeatSaverSong(ToBeatSpiderSongModel(map));
    }

    public static BeatSaverMap ToBeatSaverMap(this BeatSpiderSong song)
    {
        var json = JsonConvert.SerializeObject(song.BeatSaverSong);
        return JsonConvert.DeserializeObject<BeatSaverMap>(json, _serializerSettings);
    }
}
