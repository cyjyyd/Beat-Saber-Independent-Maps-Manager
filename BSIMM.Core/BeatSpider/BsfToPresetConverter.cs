using System;
using System.Collections.Generic;
using System.Linq;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Enums;
using BeatSpiderSharp.Models.Preset;
using BeatSpiderSharp.Models.Preset.FilterOptions;

namespace BeatSaberIndependentMapsManager.BeatSpiderSharp;

public static class BsfToPresetConverter
{
    private static readonly HashSet<FilterConditionType> _unmappableTypes = new()
    {
        FilterConditionType.None,
        FilterConditionType.Custom,
        FilterConditionType.Leaderboard,
        FilterConditionType.Curated,
        FilterConditionType.Verified,
        FilterConditionType.CustomMod,
        FilterConditionType.ExcludeCustomMod,
    };

    public static Preset Convert(FilterPreset filterPreset)
    {
        var preset = new Preset
        {
            Name = filterPreset.Name ?? string.Empty,
            Description = filterPreset.Description ?? string.Empty,
            Input = { Source = SongInputSource.BeatSaver },
        };

        var resultLimit = filterPreset.HasResultLimit()
            ? ResolveResultLimit(filterPreset)
            : null;

        if (resultLimit.HasValue)
        {
            preset.Output.LimitSongs = true;
            preset.Output.MaxSongs = resultLimit;
        }

        var activeGroups = filterPreset.GetActiveGroups();
        var filterConfigs = new List<FilterConfig>();

        foreach (var group in activeGroups)
        {
            var segments = SplitOrSegments(group.Conditions.Where(c => c.IsEnabled).ToList());
            foreach (var segment in segments)
            {
                var config = SegmentToFilterConfig(segment);
                filterConfigs.Add(config);
            }
        }

        foreach (var config in filterConfigs)
        {
            // Disable BSS defaults that BSIMM doesn't support
            config.SongDetailFilter.FullSpread.Enable = false;
            config.SongDetailFilter.AutoMapper.Enable = false;
            preset.FilterOptions.Add(config);
        }
        return preset;
    }

    private static List<List<FilterCondition>> SplitOrSegments(List<FilterCondition> conditions)
    {
        var segments = new List<List<FilterCondition>>();
        var current = new List<FilterCondition>();

        for (int i = 0; i < conditions.Count; i++)
        {
            var cond = conditions[i];
            current.Add(cond);

            if (i < conditions.Count - 1 && conditions[i].Operator == LogicOperator.Or)
            {
                segments.Add(current);
                current = new List<FilterCondition>();
            }
        }

        if (current.Count > 0)
            segments.Add(current);

        return segments.Count == 0 ? new List<List<FilterCondition>> { current } : segments;
    }

    private static FilterConfig SegmentToFilterConfig(List<FilterCondition> conditions)
    {
        var config = new FilterConfig();

        foreach (var condition in conditions)
        {
            if (_unmappableTypes.Contains(condition.Type))
                continue;

            ApplyCondition(condition, config);
        }

        return config;
    }

    private static void ApplyCondition(FilterCondition cond, FilterConfig config)
    {
        var type = cond.Type;

        if ((int)type >= 100 && (int)type < 200 && !IsRangeType(type))
            ApplyLegacyCondition(cond, config);
        else if ((int)type >= 200)
            ApplyRangeCondition(cond, config);
        else if (type > FilterConditionType.None && (int)type < 100)
            ApplyBasicCondition(cond, config);
    }

    private static bool IsRangeType(FilterConditionType type)
    {
        return (int)type >= 200 && (int)type < 300;
    }

    private static void ApplyBasicCondition(FilterCondition cond, FilterConfig config)
    {
        var s = config.SongDetailFilter;
        var l = config.LevelDetailOptions;
        var q = config.SearchOptions;

        switch (cond.Type)
        {
            case FilterConditionType.Query:
                if (cond.Value is string query && !string.IsNullOrWhiteSpace(query))
                {
                    q.Enable = true;
                    q.SearchTitle = true;
                    q.SearchSongName = true;
                    q.SearchAuthor = true;
                    q.SearchMapper = true;
                    q.AdvanceTerms.Add(new AdvanceSearchTerm { Content = query });
                }
                break;

            case FilterConditionType.MinBpm: SetRangeMin(s.Bpm, cond.Value); break;
            case FilterConditionType.MaxBpm: SetRangeMax(s.Bpm, cond.Value); break;
            case FilterConditionType.MinDuration: SetDurationMin(s.Duration, cond.Value); break;
            case FilterConditionType.MaxDuration: SetDurationMax(s.Duration, cond.Value); break;

            case FilterConditionType.MinNps: SetRangeMin(l.Nps, cond.Value); break;
            case FilterConditionType.MaxNps: SetRangeMax(l.Nps, cond.Value); break;

            case FilterConditionType.MinSsStars: SetRangeMin(l.ScoreSaberStars, cond.Value); break;
            case FilterConditionType.MaxSsStars: SetRangeMax(l.ScoreSaberStars, cond.Value); break;
            case FilterConditionType.MinBlStars: SetRangeMin(l.BeatLeaderStars, cond.Value); break;
            case FilterConditionType.MaxBlStars: SetRangeMax(l.BeatLeaderStars, cond.Value); break;

            case FilterConditionType.Chroma:
                l.RequireMods.Enable = true;
                l.RequireMods.Filter.Add(MMod.Chroma);
                break;
            case FilterConditionType.Noodle:
            case FilterConditionType.Ne:
                l.RequireMods.Enable = true;
                l.RequireMods.Filter.Add(MMod.NoodleExtensions);
                break;
            case FilterConditionType.Me:
                l.RequireMods.Enable = true;
                l.RequireMods.Filter.Add(MMod.MappingExtensions);
                break;
            case FilterConditionType.Cinema:
                l.RequireMods.Enable = true;
                l.RequireMods.Filter.Add(MMod.Cinema);
                break;
            case FilterConditionType.Vivify:
                l.RequireMods.Enable = true;
                l.RequireMods.Filter.Add(MMod.Vivify);
                break;

            case FilterConditionType.Automapper:
                s.AutoMapper.Enable = true;
                if (cond.Value is bool automapper)
                    s.AutoMapper.Filter = automapper;
                break;

            case FilterConditionType.Characteristic:
                if (cond.Value is string charName && !string.IsNullOrWhiteSpace(charName))
                    if (Enum.TryParse<MCharacteristic>(charName, true, out var mc))
                    {
                        l.IncludeCharacteristics.Enable = true;
                        l.IncludeCharacteristics.Filter.Add(mc);
                    }
                break;
            case FilterConditionType.Difficulty:
                if (cond.Value is string diffName && !string.IsNullOrWhiteSpace(diffName))
                    if (Enum.TryParse<MDifficulty>(diffName, true, out var md))
                    {
                        l.IncludeDifficulties.Enable = true;
                        l.IncludeDifficulties.Filter.Add(md);
                    }
                break;
        }
    }

    private static void ApplyRangeCondition(FilterCondition cond, FilterConfig config)
    {
        if (cond.Value is not RangeValue range)
            return;

        var s = config.SongDetailFilter;
        var l = config.LevelDetailOptions;

        switch (cond.Type)
        {
            case FilterConditionType.BpmRange: SetRange(s.Bpm, range); break;
            case FilterConditionType.DurationRange: SetDurationRange(s.Duration, range); break;
            case FilterConditionType.NpsRange: SetRange(l.Nps, range); break;
            case FilterConditionType.SsStarsRange: SetRange(l.ScoreSaberStars, range); break;
            case FilterConditionType.BlStarsRange: SetRange(l.BeatLeaderStars, range); break;

            case FilterConditionType.ScoreRange: SetRange(s.Rating, range); break;
            case FilterConditionType.UpvotesRange: SetRangeInt(s.UpVotes, range); break;
            case FilterConditionType.DownvotesRange: SetRangeInt(s.DownVotes, range); break;
            case FilterConditionType.UpvoteRatioRange: SetRangeFloat(s.UpVotePercentage, range); break;
            case FilterConditionType.DownvoteRatioRange: SetRangeFloat(s.DownVotePercentage, range); break;
            case FilterConditionType.SageScoreRange: SetRangeInt(s.SageScore, range); break;

            case FilterConditionType.NjsRange: SetRange(l.Njs, range); break;
            case FilterConditionType.BombsRange: SetRangeInt(l.Bombs, range); break;
            case FilterConditionType.OffsetRange: SetRange(l.Offset, range); break;
            case FilterConditionType.EventsRange: SetRangeInt(l.Events, range); break;
            case FilterConditionType.ObstaclesRange: SetRangeInt(l.Walls, range); break;
            case FilterConditionType.NotesRange: SetRangeInt(l.Notes, range); break;
            case FilterConditionType.SecondsRange: SetRange(l.Seconds, range); break;
            case FilterConditionType.LengthRange: SetRange(l.Beats, range); break;
            case FilterConditionType.ParityErrorsRange: SetRangeInt(l.ParityErrors, range); break;
            case FilterConditionType.ParityWarnsRange: SetRangeInt(l.ParityWarns, range); break;
            case FilterConditionType.ParityResetsRange: SetRangeInt(l.ParityResets, range); break;
            case FilterConditionType.MaxScoreRange: SetRangeInt(l.MaxScore, range); break;
        }
    }

    private static void ApplyLegacyCondition(FilterCondition cond, FilterConfig config)
    {
        var s = config.SongDetailFilter;
        var l = config.LevelDetailOptions;
        var q = config.SearchOptions;

        switch (cond.Type)
        {
            case FilterConditionType.Ranked: SetRanking(s.ScoreSaberRanking, cond.Value, RankingStatus.Ranked); break;
            case FilterConditionType.BlRanked: SetRanking(s.BeatLeaderRanking, cond.Value, RankingStatus.Ranked); break;
            case FilterConditionType.Qualified: SetRanking(s.ScoreSaberRanking, cond.Value, RankingStatus.Qualified); break;
            case FilterConditionType.BlQualified: SetRanking(s.BeatLeaderRanking, cond.Value, RankingStatus.Qualified); break;

            case FilterConditionType.MinPlays: break; // obsolete
            case FilterConditionType.MaxPlays: break;
            case FilterConditionType.MinDownloads: break;
            case FilterConditionType.MaxDownloads: break;

            case FilterConditionType.MinUpvotes: SetRangeMinInt(s.UpVotes, cond.Value); break;
            case FilterConditionType.MaxUpvotes: SetRangeMaxInt(s.UpVotes, cond.Value); break;
            case FilterConditionType.MinDownvotes: SetRangeMinInt(s.DownVotes, cond.Value); break;
            case FilterConditionType.MaxDownvotes: SetRangeMaxInt(s.DownVotes, cond.Value); break;
            case FilterConditionType.MinScore: SetRangeMin(s.Rating, cond.Value); break;
            case FilterConditionType.MaxScore: SetRangeMax(s.Rating, cond.Value); break;

            case FilterConditionType.MinUpvoteRatio: SetRangeMinFloat(s.UpVotePercentage, cond.Value); break;
            case FilterConditionType.MaxUpvoteRatio: SetRangeMaxFloat(s.UpVotePercentage, cond.Value); break;
            case FilterConditionType.MinDownvoteRatio: SetRangeMinFloat(s.DownVotePercentage, cond.Value); break;
            case FilterConditionType.MaxDownvoteRatio: SetRangeMaxFloat(s.DownVotePercentage, cond.Value); break;

            case FilterConditionType.MinSageScore: SetRangeMinInt(s.SageScore, cond.Value); break;
            case FilterConditionType.MaxSageScore: SetRangeMaxInt(s.SageScore, cond.Value); break;

            case FilterConditionType.Tags:
                if (cond.Value is string tags && !string.IsNullOrWhiteSpace(tags))
                {
                    s.IncludeTags.Enable = true;
                    foreach (var tag in tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        s.IncludeTags.Filter.Add(tag);
                }
                break;

            case FilterConditionType.ExcludeTags:
                if (cond.Value is string exTags && !string.IsNullOrWhiteSpace(exTags))
                {
                    s.ExcludeTags.Enable = true;
                    foreach (var tag in exTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        s.ExcludeTags.Filter.Add(tag);
                }
                break;

            case FilterConditionType.UploaderName:
                if (cond.Value is string uploader && !string.IsNullOrWhiteSpace(uploader))
                {
                    s.UploaderName.Enable = true;
                    s.UploaderName.Filter.Add(uploader);
                }
                break;

            case FilterConditionType.MinUploadedDate: SetUploadTimeMin(s.UploadTime, cond.Value); break;
            case FilterConditionType.MaxUploadedDate: SetUploadTimeMax(s.UploadTime, cond.Value); break;

            case FilterConditionType.MinNjs: SetRangeMin(l.Njs, cond.Value); break;
            case FilterConditionType.MaxNjs: SetRangeMax(l.Njs, cond.Value); break;
            case FilterConditionType.MinBombs: SetRangeMinInt(l.Bombs, cond.Value); break;
            case FilterConditionType.MaxBombs: SetRangeMaxInt(l.Bombs, cond.Value); break;
            case FilterConditionType.MinOffset: SetRangeMin(l.Offset, cond.Value); break;
            case FilterConditionType.MaxOffset: SetRangeMax(l.Offset, cond.Value); break;
            case FilterConditionType.MinEvents: SetRangeMinInt(l.Events, cond.Value); break;
            case FilterConditionType.MaxEvents: SetRangeMaxInt(l.Events, cond.Value); break;

            case FilterConditionType.MinObstacles: SetRangeMinInt(l.Walls, cond.Value); break;
            case FilterConditionType.MaxObstacles: SetRangeMaxInt(l.Walls, cond.Value); break;
            case FilterConditionType.MinBombsMap: SetRangeMinInt(l.Bombs, cond.Value); break;
            case FilterConditionType.MaxBombsMap: SetRangeMaxInt(l.Bombs, cond.Value); break;
            case FilterConditionType.MinNotes: SetRangeMinInt(l.Notes, cond.Value); break;
            case FilterConditionType.MaxNotes: SetRangeMaxInt(l.Notes, cond.Value); break;
            case FilterConditionType.MinSeconds: SetRangeMin(l.Seconds, cond.Value); break;
            case FilterConditionType.MaxSeconds: SetRangeMax(l.Seconds, cond.Value); break;
            case FilterConditionType.MinLength: SetRangeMin(l.Beats, cond.Value); break;
            case FilterConditionType.MaxLength: SetRangeMax(l.Beats, cond.Value); break;

            case FilterConditionType.MinParityErrors: SetRangeMinInt(l.ParityErrors, cond.Value); break;
            case FilterConditionType.MaxParityErrors: SetRangeMaxInt(l.ParityErrors, cond.Value); break;
            case FilterConditionType.MinParityWarns: SetRangeMinInt(l.ParityWarns, cond.Value); break;
            case FilterConditionType.MaxParityWarns: SetRangeMaxInt(l.ParityWarns, cond.Value); break;
            case FilterConditionType.MinParityResets: SetRangeMinInt(l.ParityResets, cond.Value); break;
            case FilterConditionType.MaxParityResets: SetRangeMaxInt(l.ParityResets, cond.Value); break;

            case FilterConditionType.MinMaxScore: SetRangeMinInt(l.MaxScore, cond.Value); break;
            case FilterConditionType.MaxMaxScore: SetRangeMaxInt(l.MaxScore, cond.Value); break;
        }
    }

    private static int? ResolveResultLimit(FilterPreset preset)
    {
        if (preset.TopLevelResultLimit is { Count: > 0 } topLimit)
            return topLimit.Count;

        foreach (var group in preset.GetActiveGroups())
        {
            var limit = group.GetResultLimit();
            if (limit is { Count: > 0 } rlv)
                return rlv.Count;
        }

        return null;
    }

    private static void SetRanking(IncludeOption<RankingStatus> option, object value, RankingStatus status)
    {
        if (value is bool enabled && enabled)
        {
            option.Enable = true;
            option.Filter.Add(status);
        }
    }

    private static void SetRange(RangeOption<float> option, RangeValue range)
    {
        option.Enable = true;
        if (range.Min is double min)
            option.Min = (float)min;
        if (range.Max is double max)
            option.Max = (float)max;
    }

    private static void SetRangeInt(RangeOption<int> option, RangeValue range)
    {
        option.Enable = true;
        if (range.Min is double min)
            option.Min = (int)min;
        if (range.Max is double max)
            option.Max = (int)max;
    }

    private static void SetRangeFloat(RangeOption<float> option, RangeValue range)
    {
        option.Enable = true;
        if (range.Min is double min)
            option.Min = (float)min;
        if (range.Max is double max)
            option.Max = (float)max;
    }

    private static void SetDurationRange(RangeOption<int> option, RangeValue range)
    {
        option.Enable = true;
        if (range.Min is double min)
            option.Min = (int)min;
        if (range.Max is double max)
            option.Max = (int)max;
    }

    private static void SetRangeMin(RangeOption<float> option, object value)
    {
        if (value is double d)
        {
            option.Enable = true;
            option.Min = (float)d;
        }
    }

    private static void SetRangeMax(RangeOption<float> option, object value)
    {
        if (value is double d)
        {
            option.Enable = true;
            option.Max = (float)d;
        }
    }

    private static void SetRangeMinInt(RangeOption<int> option, object value)
    {
        if (value is double d)
        {
            option.Enable = true;
            option.Min = (int)d;
        }
    }

    private static void SetRangeMaxInt(RangeOption<int> option, object value)
    {
        if (value is double d)
        {
            option.Enable = true;
            option.Max = (int)d;
        }
    }

    private static void SetRangeMinFloat(RangeOption<float> option, object value)
    {
        if (value is double d)
        {
            option.Enable = true;
            option.Min = (float)d;
        }
    }

    private static void SetRangeMaxFloat(RangeOption<float> option, object value)
    {
        if (value is double d)
        {
            option.Enable = true;
            option.Max = (float)d;
        }
    }

    private static void SetDurationMin(RangeOption<int> option, object value)
    {
        if (value is double d)
        {
            option.Enable = true;
            option.Min = (int)d;
        }
    }

    private static void SetDurationMax(RangeOption<int> option, object value)
    {
        if (value is double d)
        {
            option.Enable = true;
            option.Max = (int)d;
        }
    }

    private static void SetUploadTimeMin(RangeOption<DateTimeOffset> option, object value)
    {
        if (value is DateTime dt)
        {
            option.Enable = true;
            option.Min = new DateTimeOffset(dt);
        }
    }

    private static void SetUploadTimeMax(RangeOption<DateTimeOffset> option, object value)
    {
        if (value is DateTime dt)
        {
            option.Enable = true;
            option.Max = new DateTimeOffset(dt);
        }
    }

    public static FilterPreset ConvertBack(Preset preset)
    {
        var fpreset = new FilterPreset(preset.Name) { Description = preset.Description };
        foreach (var fc in preset.FilterOptions)
        {
            var group = new FilterGroup("筛选组");
            MapFilterConfigToGroup(fc, group);
            fpreset.AddGroup(group);
        }
        if (preset.Output.LimitSongs && preset.Output.MaxSongs > 0)
        {
            var limit = new ResultLimitValue(preset.Output.MaxSongs.Value);
            if (fpreset.Groups.Count > 0)
                fpreset.Groups[0].AddCondition(new FilterCondition(FilterConditionType.ResultLimit, limit));
            else
                fpreset.TopLevelResultLimit = limit;
        }
        return fpreset;
    }

    private static void MapFilterConfigToGroup(FilterConfig fc, FilterGroup group)
    {
        var s = fc.SongDetailFilter;
        var l = fc.LevelDetailOptions;
        var q = fc.SearchOptions;

        if (s.Bpm.Enable && (s.Bpm.Min.HasValue || s.Bpm.Max.HasValue))
            group.AddCondition(new FilterCondition(FilterConditionType.BpmRange, new RangeValue { MinRaw = DVal(s.Bpm.Min), MaxRaw = DVal(s.Bpm.Max) }));
        if (s.Duration.Enable && (s.Duration.Min.HasValue || s.Duration.Max.HasValue))
            group.AddCondition(new FilterCondition(FilterConditionType.DurationRange, new RangeValue { MinRaw = DValInt(s.Duration.Min), MaxRaw = DValInt(s.Duration.Max) }));
        if (s.Rating.Enable && (s.Rating.Min.HasValue || s.Rating.Max.HasValue))
            group.AddCondition(new FilterCondition(FilterConditionType.ScoreRange, new RangeValue { MinRaw = DVal(s.Rating.Min), MaxRaw = DVal(s.Rating.Max) }));
        if (s.ScoreSaberRanking.Enable && s.ScoreSaberRanking.Filter.Contains(RankingStatus.Ranked))
            group.AddCondition(new FilterCondition(FilterConditionType.Ranked, true));
        if (s.ScoreSaberRanking.Enable && s.ScoreSaberRanking.Filter.Contains(RankingStatus.Qualified))
            group.AddCondition(new FilterCondition(FilterConditionType.Qualified, true));
        if (s.BeatLeaderRanking.Enable && s.BeatLeaderRanking.Filter.Contains(RankingStatus.Ranked))
            group.AddCondition(new FilterCondition(FilterConditionType.BlRanked, true));
        if (s.IncludeTags.Enable && s.IncludeTags.Filter.Count > 0)
            group.AddCondition(new FilterCondition(FilterConditionType.Tags, string.Join(",", s.IncludeTags.Filter)));
        if (s.ExcludeTags.Enable && s.ExcludeTags.Filter.Count > 0)
            group.AddCondition(new FilterCondition(FilterConditionType.ExcludeTags, string.Join(",", s.ExcludeTags.Filter)));
        if (l.Nps.Enable && (l.Nps.Min.HasValue || l.Nps.Max.HasValue))
            group.AddCondition(new FilterCondition(FilterConditionType.NpsRange, new RangeValue { MinRaw = DVal(l.Nps.Min), MaxRaw = DVal(l.Nps.Max) }));
        if (l.ScoreSaberStars.Enable && (l.ScoreSaberStars.Min.HasValue || l.ScoreSaberStars.Max.HasValue))
            group.AddCondition(new FilterCondition(FilterConditionType.SsStarsRange, new RangeValue { MinRaw = DVal(l.ScoreSaberStars.Min), MaxRaw = DVal(l.ScoreSaberStars.Max) }));
        if (l.BeatLeaderStars.Enable && (l.BeatLeaderStars.Min.HasValue || l.BeatLeaderStars.Max.HasValue))
            group.AddCondition(new FilterCondition(FilterConditionType.BlStarsRange, new RangeValue { MinRaw = DVal(l.BeatLeaderStars.Min), MaxRaw = DVal(l.BeatLeaderStars.Max) }));

        foreach (var mod in l.RequireMods.Filter)
        {
            var ct = mod switch
            {
                MMod.Chroma => FilterConditionType.Chroma,
                MMod.NoodleExtensions => FilterConditionType.Noodle,
                MMod.MappingExtensions => FilterConditionType.Me,
                MMod.Cinema => FilterConditionType.Cinema,
                MMod.Vivify => FilterConditionType.Vivify,
                _ => FilterConditionType.None
            };
            if (ct != FilterConditionType.None) group.AddCondition(new FilterCondition(ct, true));
        }

        if (q.Enable)
            group.AddCondition(new FilterCondition(FilterConditionType.Query, q.AdvanceTerms.FirstOrDefault()?.Content ?? ""));
    }

    private static double DVal(float? v) => v.HasValue ? v.Value : double.NaN;
    private static double DValInt(int? v) => v.HasValue ? v.Value : double.NaN;
}
