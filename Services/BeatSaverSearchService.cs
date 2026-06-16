using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BeatSaberIndependentMapsManager.Services
{
    internal class BeatSaverSearchService
    {
        private readonly BeatSaverClient _beatSaverClient;
        private readonly LocalCacheManager _localCacheManager;

        public BeatSaverSearchService(BeatSaverClient beatSaverClient, LocalCacheManager localCacheManager)
        {
            _beatSaverClient = beatSaverClient;
            _localCacheManager = localCacheManager;
        }

        public bool RequiresLocalCache(FilterPreset preset)
        {
            if (preset == null) return false;

            foreach (var group in preset.GetActiveGroups())
            {
                if (group.UseLocalCache)
                    return true;

                foreach (var condition in group.GetActiveConditions())
                {
                    if (FilterConditionMetadata.RequiresLocalCache(condition.Type))
                        return true;
                }
            }

            return false;
        }

        public bool HasOrLogic(FilterPreset preset)
        {
            if (preset == null) return false;

            var activeGroups = preset.GetActiveGroups();

            foreach (var group in activeGroups)
            {
                if (group.GroupOperator == LogicOperator.Or)
                    return true;

                var conditions = group.GetActiveConditions();

                if (IsModOnlyGroup(conditions))
                {
                    continue;
                }

                foreach (var condition in conditions)
                {
                    if (condition.Operator == LogicOperator.Or)
                        return true;
                }
            }

            return false;
        }

        private bool IsModOnlyGroup(List<FilterCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return false;

            var modConditionTypes = new HashSet<FilterConditionType>
            {
                FilterConditionType.Chroma,
                FilterConditionType.Noodle,
                FilterConditionType.Me,
                FilterConditionType.Cinema,
                FilterConditionType.Vivify
            };

            foreach (var condition in conditions)
            {
                if (!modConditionTypes.Contains(condition.Type))
                    return false;
            }

            return true;
        }

        public BeatSaverSearchFilter BuildSearchFilterFromPreset(FilterPreset preset)
        {
            var filter = new BeatSaverSearchFilter();
            if (preset == null) return filter;

            var modConditionTypes = new HashSet<FilterConditionType>
            {
                FilterConditionType.Chroma,
                FilterConditionType.Noodle,
                FilterConditionType.Me,
                FilterConditionType.Cinema,
                FilterConditionType.Vivify
            };

            foreach (var group in preset.GetActiveGroups())
            {
                var conditions = group.GetActiveConditions();
                FilterCondition previousModCondition = null;

                for (int i = 0; i < conditions.Count; i++)
                {
                    var condition = conditions[i];
                    bool isModCondition = modConditionTypes.Contains(condition.Type);

                    if (isModCondition && previousModCondition != null)
                    {
                        bool needsAndFiltering = (previousModCondition.Operator == LogicOperator.And);
                        ApplyConditionToFilter(filter, condition, needsAndFiltering);
                    }
                    else if (!isModCondition)
                    {
                        ApplyConditionToFilter(filter, condition, false);
                    }
                    else
                    {
                        bool hasNextModCondition = false;
                        bool nextModIsOr = false;

                        for (int j = i + 1; j < conditions.Count; j++)
                        {
                            if (modConditionTypes.Contains(conditions[j].Type))
                            {
                                hasNextModCondition = true;
                                nextModIsOr = (conditions[j].Operator == LogicOperator.Or);
                                break;
                            }
                        }

                        bool needsAndFiltering = hasNextModCondition && !nextModIsOr;
                        ApplyConditionToFilter(filter, condition, needsAndFiltering);
                    }

                    if (isModCondition)
                    {
                        previousModCondition = condition;
                    }
                }
            }

            return filter;
        }

        private void ApplyConditionToFilter(BeatSaverSearchFilter filter, FilterCondition condition, bool addModToFilter = false)
        {
            if (condition.Value == null) return;

            switch (condition.Type)
            {
                case FilterConditionType.Custom:
                    if (!string.IsNullOrWhiteSpace(condition.CustomName) && !string.IsNullOrWhiteSpace(condition.Value?.ToString()))
                    {
                        filter.Query = string.IsNullOrWhiteSpace(filter.Query)
                            ? $"{condition.CustomName}:{condition.Value}"
                            : $"{filter.Query} {condition.CustomName}:{condition.Value}";
                    }
                    break;
                case FilterConditionType.Query:
                    if (condition.Value is SearchQueryValue queryValue)
                        filter.Query = queryValue.ToApiQuery();
                    else
                        filter.Query = condition.Value.ToString();
                    break;
                case FilterConditionType.Order:
                    filter.Order = condition.Value.ToString();
                    break;
                case FilterConditionType.MinBpm:
                    if (double.TryParse(condition.Value.ToString(), out double minBpm))
                        filter.MinBpm = minBpm;
                    break;
                case FilterConditionType.MaxBpm:
                    if (double.TryParse(condition.Value.ToString(), out double maxBpm))
                        filter.MaxBpm = maxBpm;
                    break;
                case FilterConditionType.MinNps:
                    if (double.TryParse(condition.Value.ToString(), out double minNps))
                        filter.MinNps = minNps;
                    break;
                case FilterConditionType.MaxNps:
                    if (double.TryParse(condition.Value.ToString(), out double maxNps))
                        filter.MaxNps = maxNps;
                    break;
                case FilterConditionType.MinDuration:
                    if (int.TryParse(condition.Value.ToString(), out int minDur))
                        filter.MinDuration = minDur;
                    break;
                case FilterConditionType.MaxDuration:
                    if (int.TryParse(condition.Value.ToString(), out int maxDur))
                        filter.MaxDuration = maxDur;
                    break;
                case FilterConditionType.MinSsStars:
                    if (double.TryParse(condition.Value.ToString(), out double minSs))
                        filter.MinSsStars = minSs;
                    break;
                case FilterConditionType.MaxSsStars:
                    if (double.TryParse(condition.Value.ToString(), out double maxSs))
                        filter.MaxSsStars = maxSs;
                    break;
                case FilterConditionType.MinBlStars:
                    if (double.TryParse(condition.Value.ToString(), out double minBl))
                        filter.MinBlStars = minBl;
                    break;
                case FilterConditionType.MaxBlStars:
                    if (double.TryParse(condition.Value.ToString(), out double maxBl))
                        filter.MaxBlStars = maxBl;
                    break;
                case FilterConditionType.Chroma:
                    {
                        bool val = Convert.ToBoolean(condition.Value);
                        filter.Chroma = val;
                        if (addModToFilter)
                            filter.ModFiltersToApply.Add(new ModFilterCondition("Chroma", val));
                    }
                    break;
                case FilterConditionType.Noodle:
                    {
                        bool val = Convert.ToBoolean(condition.Value);
                        filter.Noodle = val;
                        if (addModToFilter)
                            filter.ModFiltersToApply.Add(new ModFilterCondition("Noodle", val));
                    }
                    break;
                case FilterConditionType.Me:
                    {
                        bool val = Convert.ToBoolean(condition.Value);
                        filter.Me = val;
                        if (addModToFilter)
                            filter.ModFiltersToApply.Add(new ModFilterCondition("Me", val));
                    }
                    break;
                case FilterConditionType.Cinema:
                    {
                        bool val = Convert.ToBoolean(condition.Value);
                        filter.Cinema = val;
                        if (addModToFilter)
                            filter.ModFiltersToApply.Add(new ModFilterCondition("Cinema", val));
                    }
                    break;
                case FilterConditionType.Vivify:
                    {
                        bool val = Convert.ToBoolean(condition.Value);
                        filter.Vivify = val;
                        if (addModToFilter)
                            filter.ModFiltersToApply.Add(new ModFilterCondition("Vivify", val));
                    }
                    break;
                case FilterConditionType.Ne:
                    {
                        bool val = Convert.ToBoolean(condition.Value);
                        filter.Noodle = val;
                        if (addModToFilter)
                            filter.ModFiltersToApply.Add(new ModFilterCondition("Noodle", val));
                    }
                    break;
                case FilterConditionType.Automapper:
                    var autoVal = condition.Value.ToString();
                    if (autoVal == "仅AI谱")
                        filter.Automapper = true;
                    else if (autoVal == "排除AI谱")
                        filter.Automapper = false;
                    break;
                case FilterConditionType.Leaderboard:
                    filter.Leaderboard = condition.Value.ToString();
                    break;
                case FilterConditionType.Curated:
                    filter.Curated = Convert.ToBoolean(condition.Value);
                    break;
                case FilterConditionType.Verified:
                    filter.Verified = Convert.ToBoolean(condition.Value);
                    break;
            }
        }

        public List<BeatSaverSearchFilter> BuildSearchFiltersWithOrLogic(FilterPreset preset)
        {
            var filters = new List<BeatSaverSearchFilter>();
            var activeGroups = preset.GetActiveGroups();

            if (!activeGroups.Any())
            {
                filters.Add(new BeatSaverSearchFilter());
                return filters;
            }

            var currentFilter = new BeatSaverSearchFilter();
            filters.Add(currentFilter);

            for (int i = 0; i < activeGroups.Count; i++)
            {
                var group = activeGroups[i];
                var conditions = group.GetActiveConditions();

                for (int j = 0; j < conditions.Count; j++)
                {
                    var condition = conditions[j];

                    if (j == 0 && filters.Count > 1 && i > 0)
                    {
                        if (group.GroupOperator == LogicOperator.Or)
                        {
                            currentFilter = new BeatSaverSearchFilter();
                            filters.Add(currentFilter);
                        }
                    }

                    if (condition.Operator == LogicOperator.Or && j > 0)
                    {
                        var newFilter = CloneFilter(currentFilter);
                        filters.Add(newFilter);
                        currentFilter = newFilter;
                    }

                    ApplyConditionToFilter(currentFilter, condition);
                }

                if (i < activeGroups.Count - 1)
                {
                    var nextGroup = activeGroups[i + 1];
                    if (nextGroup.GroupOperator == LogicOperator.Or)
                    {
                        currentFilter = new BeatSaverSearchFilter();
                        filters.Add(currentFilter);
                    }
                }
            }

            return filters;
        }

        private BeatSaverSearchFilter CloneFilter(BeatSaverSearchFilter source)
        {
            return new BeatSaverSearchFilter
            {
                Query = source.Query,
                Order = source.Order,
                MinBpm = source.MinBpm,
                MaxBpm = source.MaxBpm,
                MinNps = source.MinNps,
                MaxNps = source.MaxNps,
                MinDuration = source.MinDuration,
                MaxDuration = source.MaxDuration,
                MinSsStars = source.MinSsStars,
                MaxSsStars = source.MaxSsStars,
                MinBlStars = source.MinBlStars,
                MaxBlStars = source.MaxBlStars,
                Chroma = source.Chroma,
                Noodle = source.Noodle,
                Me = source.Me,
                Cinema = source.Cinema,
                Vivify = source.Vivify,
                Automapper = source.Automapper,
                Leaderboard = source.Leaderboard,
                Curated = source.Curated,
                Verified = source.Verified,
                ModFiltersToApply = new List<ModFilterCondition>(source.ModFiltersToApply)
            };
        }

        /// <summary>
        /// Fetch all maps for a specific preset (used by Batch Export)
        /// </summary>
        public async Task<List<BeatSaverMap>> FetchAllMapsForPresetAsync(FilterPreset preset, bool useSharedCache = false, Action<int> progressCallback = null)
        {
            var allMaps = new List<BeatSaverMap>();

            if (RequiresLocalCache(preset))
            {
                if (!_localCacheManager.IsCacheAvailable)
                {
                    throw new Exception("需要本地缓存但未下载");
                }

                allMaps = await Task.Run(() =>
                {
                    var progress = new Progress<int>(percent =>
                    {
                        progressCallback?.Invoke(percent);
                    });

                    if (useSharedCache)
                    {
                        var results = new List<BeatSaverMapSlim>();
                        foreach (var map in _localCacheManager.StreamFilterMapsShared(preset, null))
                        {
                            results.Add(map);
                        }
                        var limitedResults = _localCacheManager.ApplyResultLimitSlim(results, preset);
                        return limitedResults.Select(m => m.ToFullMap()).ToList();
                    }
                    else
                    {
                        return _localCacheManager.ParallelFilterMaps(preset, progress);
                    }
                });
            }
            else
            {
                var filter = BuildSearchFilterFromPreset(preset);
                int page = 0;
                int totalPagesFromApi = 0;

                while (true)
                {
                    var response = await _beatSaverClient.SearchMapsAsync(filter, page);
                    if (response?.Maps == null || response.Maps.Count == 0)
                        break;

                    if (page == 0)
                    {
                        if (response.Info != null)
                            totalPagesFromApi = response.Info.Pages;
                        else if (response.Metadata != null)
                            totalPagesFromApi = (response.Metadata.Total + response.Metadata.PageSize - 1) / response.Metadata.PageSize;
                    }

                    allMaps.AddRange(response.Maps);

                    if (page >= totalPagesFromApi - 1)
                        break;

                    page++;
                    await Task.Delay(150); 
                }
            }

            return allMaps;
        }

        public async Task<(List<BeatSaverMap> Results, int TotalPages)> SearchWithOrLogicAsync(FilterPreset preset, Action<string> statusCallback)
        {
            var searchFilters = BuildSearchFiltersWithOrLogic(preset);
            statusCallback?.Invoke($"正在执行 {searchFilters.Count} 个搜索任务...");

            var allResults = new Dictionary<string, BeatSaverMap>(); 

            foreach (var filter in searchFilters)
            {
                int page = 0;
                while (true)
                {
                    try
                    {
                        var response = await _beatSaverClient.SearchMapsAsync(filter, page);
                        if (response?.Maps == null || response.Maps.Count == 0)
                            break;

                        foreach (var map in response.Maps)
                        {
                            if (!string.IsNullOrEmpty(map.Id) && !allResults.ContainsKey(map.Id))
                            {
                                allResults[map.Id] = map;
                            }
                        }

                        if (response.Info != null && page < response.Info.Pages - 1)
                        {
                            page++;
                            statusCallback?.Invoke($"正在搜索... 已找到 {allResults.Count} 个结果");
                        }
                        else if (response.Metadata != null && (page + 1) * response.Metadata.PageSize < response.Metadata.Total)
                        {
                            page++;
                            statusCallback?.Invoke($"正在搜索... 已找到 {allResults.Count} 个结果");
                        }
                        else
                        {
                            break;
                        }

                        await Task.Delay(100);
                    }
                    catch
                    {
                        break;
                    }
                }
            }

            var list = allResults.Values.ToList();
            int totalPages = (int)Math.Ceiling((double)list.Count / 20); // Using 20 as PageSize
            return (list, totalPages);
        }

        public async Task<(List<BeatSaverMap> Results, int TotalPages)> SearchWithModFilteringAsync(BeatSaverSearchFilter filter, Action<string> statusCallback)
        {
            statusCallback?.Invoke("正在搜索并筛选...");
            var allResults = new Dictionary<string, BeatSaverMap>();
            int page = 0;
            int totalPagesFromApi = 0;

            while (true)
            {
                try
                {
                    var response = await _beatSaverClient.SearchMapsAsync(filter, page);
                    if (response?.Maps == null || response.Maps.Count == 0)
                        break;

                    if (page == 0)
                    {
                        if (response.Info != null)
                            totalPagesFromApi = response.Info.Pages;
                        else if (response.Metadata != null)
                            totalPagesFromApi = (response.Metadata.Total + response.Metadata.PageSize - 1) / response.Metadata.PageSize;
                    }

                    foreach (var map in response.Maps)
                    {
                        if (!string.IsNullOrEmpty(map.Id) && !allResults.ContainsKey(map.Id))
                        {
                            allResults[map.Id] = map;
                        }
                    }

                    if (page >= totalPagesFromApi - 1)
                        break;

                    page++;
                    statusCallback?.Invoke($"正在获取数据... 第 {page + 1}/{totalPagesFromApi} 页");
                    await Task.Delay(100);
                }
                catch
                {
                    break;
                }
            }

            statusCallback?.Invoke("正在筛选...");
            var filteredResults = _beatSaverClient.ApplyModFilters(allResults.Values.ToList(), filter);
            int totalPages = (int)Math.Ceiling((double)filteredResults.Count / 20);
            return (filteredResults, totalPages);
        }
    }
}
