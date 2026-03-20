using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSaberIndependentMapsManager.Tests
{
    /// <summary>
    /// 本地缓存筛选测试程序
    /// 用于测试筛选条件是否正确工作
    /// </summary>
    class LocalCacheFilterTest
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== 本地缓存筛选测试 ===\n");

            string cachePath = @"E:\cache.json";

            if (!File.Exists(cachePath))
            {
                Console.WriteLine($"错误: 缓存文件不存在: {cachePath}");
                return;
            }

            // 测试1: 读取缓存基本信息
            Console.WriteLine("测试1: 读取缓存基本信息...");
            TestCacheBasicInfo(cachePath);

            // 测试2: 统计ranked/blRanked曲谱数量
            Console.WriteLine("\n测试2: 统计排行榜曲谱数量...");
            TestRankedCount(cachePath);

            // 测试3: 测试筛选条件
            Console.WriteLine("\n测试3: 测试Ranked筛选条件...");
            TestRankedFilter(cachePath);

            // 测试4: 测试完整筛选流程
            Console.WriteLine("\n测试4: 测试完整筛选流程...");
            TestFullFilterFlow(cachePath);

            Console.WriteLine("\n=== 测试完成 ===");
        }

        static void TestCacheBasicInfo(string cachePath)
        {
            try
            {
                using (var fileStream = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
                using (var streamReader = new StreamReader(fileStream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    int mapCount = 0;
                    long cacheDate = 0;

                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.PropertyName && jsonReader.Value?.ToString() == "date")
                        {
                            jsonReader.Read();
                            cacheDate = jsonReader.Value != null ? Convert.ToInt64(jsonReader.Value) : 0;
                        }

                        if (jsonReader.TokenType == JsonToken.StartArray && jsonReader.Path == "docs")
                        {
                            while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                            {
                                if (jsonReader.TokenType == JsonToken.StartObject)
                                {
                                    mapCount++;
                                }
                            }
                            break;
                        }
                    }

                    var date = DateTimeOffset.FromUnixTimeSeconds(cacheDate).ToLocalTime();
                    Console.WriteLine($"  缓存日期: {date}");
                    Console.WriteLine($"  曲谱总数: {mapCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
            }
        }

        static void TestRankedCount(string cachePath)
        {
            try
            {
                using (var fileStream = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
                using (var streamReader = new StreamReader(fileStream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    var serializer = new JsonSerializer();

                    // Navigate to docs array
                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.StartArray && jsonReader.Path == "docs")
                            break;
                    }

                    int rankedTrue = 0, rankedFalse = 0, rankedNull = 0;
                    int blRankedTrue = 0, blRankedFalse = 0, blRankedNull = 0;
                    int qualifiedTrue = 0, blQualifiedTrue = 0;
                    int totalProcessed = 0;

                    while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                    {
                        if (jsonReader.TokenType == JsonToken.StartObject)
                        {
                            try
                            {
                                var obj = JObject.Load(jsonReader);
                                totalProcessed++;

                                // Check ranked
                                var ranked = obj["ranked"];
                                if (ranked == null) rankedNull++;
                                else if (ranked.Value<bool>()) rankedTrue++;
                                else rankedFalse++;

                                // Check blRanked
                                var blRanked = obj["blRanked"];
                                if (blRanked == null) blRankedNull++;
                                else if (blRanked.Value<bool>()) blRankedTrue++;
                                else blRankedFalse++;

                                // Check qualified
                                var qualified = obj["qualified"];
                                if (qualified != null && qualified.Value<bool>()) qualifiedTrue++;

                                // Check blQualified
                                var blQualified = obj["blQualified"];
                                if (blQualified != null && blQualified.Value<bool>()) blQualifiedTrue++;

                                if (totalProcessed % 10000 == 0)
                                {
                                    Console.WriteLine($"  已处理 {totalProcessed} 首曲谱...");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  解析错误: {ex.Message}");
                            }
                        }
                    }

                    Console.WriteLine($"  处理总数: {totalProcessed}");
                    Console.WriteLine($"  Ranked=true: {rankedTrue}, Ranked=false: {rankedFalse}, Ranked=null: {rankedNull}");
                    Console.WriteLine($"  BlRanked=true: {blRankedTrue}, BlRanked=false: {blRankedFalse}, BlRanked=null: {blRankedNull}");
                    Console.WriteLine($"  Qualified=true: {qualifiedTrue}");
                    Console.WriteLine($"  BlQualified=true: {blQualifiedTrue}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
            }
        }

        static void TestRankedFilter(string cachePath)
        {
            try
            {
                using (var fileStream = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
                using (var streamReader = new StreamReader(fileStream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    var serializer = new JsonSerializer();

                    // Navigate to docs array
                    while (jsonReader.Read())
                    {
                        if (jsonReader.TokenType == JsonToken.StartArray && jsonReader.Path == "docs")
                            break;
                    }

                    int matchedCount = 0;
                    int totalProcessed = 0;
                    List<string> sampleMaps = new List<string>();

                    while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
                    {
                        if (jsonReader.TokenType == JsonToken.StartObject)
                        {
                            try
                            {
                                var map = serializer.Deserialize<BeatSaverMap>(jsonReader);
                                totalProcessed++;

                                if (map != null)
                                {
                                    // 测试条件: Ranked == true
                                    if (map.Ranked == true)
                                    {
                                        matchedCount++;
                                        if (sampleMaps.Count < 5)
                                        {
                                            sampleMaps.Add($"{map.Name} (ID: {map.Id}, Ranked: {map.Ranked})");
                                        }
                                    }
                                }

                                if (totalProcessed % 10000 == 0)
                                {
                                    Console.WriteLine($"  已处理 {totalProcessed} 首曲谱，匹配 {matchedCount} 首...");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"  解析错误: {ex.Message}");
                            }
                        }
                    }

                    Console.WriteLine($"  处理总数: {totalProcessed}");
                    Console.WriteLine($"  Ranked=true 匹配数: {matchedCount}");
                    if (sampleMaps.Count > 0)
                    {
                        Console.WriteLine("  匹配示例:");
                        foreach (var sample in sampleMaps)
                        {
                            Console.WriteLine($"    - {sample}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
            }
        }

        static void TestFullFilterFlow(string cachePath)
        {
            try
            {
                // 创建测试用的FilterPreset
                var preset = new FilterPreset("测试SS Ranked");
                var group = new FilterGroup("条件组1");
                group.UseLocalCache = true;

                // 添加Ranked条件
                var condition = new FilterCondition(FilterConditionType.Ranked);
                condition.Value = true; // 筛选Ranked=true
                condition.IsEnabled = true;
                group.AddCondition(condition);
                preset.AddGroup(group);

                Console.WriteLine($"  预设名称: {preset.Name}");
                Console.WriteLine($"  条件组数: {preset.Groups.Count}");
                Console.WriteLine($"  条件数: {group.Conditions.Count}");
                Console.WriteLine($"  条件类型: {condition.Type}");
                Console.WriteLine($"  条件值: {condition.Value}");
                Console.WriteLine($"  条件启用: {condition.IsEnabled}");
                Console.WriteLine($"  条件HasValue: {condition.HasValue()}");

                // 检查RequiresLocalCache
                bool requiresLocalCache = FilterConditionMetadata.RequiresLocalCache(condition.Type);
                Console.WriteLine($"  RequiresLocalCache: {requiresLocalCache}");
                Console.WriteLine($"  Condition Type Value: {(int)condition.Type}");

                // 创建LocalCacheManager并执行筛选
                var manager = new LocalCacheManager();
                // 手动设置缓存路径用于测试
                var field = typeof(LocalCacheManager).GetField("cachePath",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(manager, cachePath);

                // 模拟设置缓存可用
                var cacheAvailableField = typeof(LocalCacheManager).GetField("cacheAvailable",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                cacheAvailableField?.SetValue(manager, true);

                Console.WriteLine($"  缓存可用: {manager.IsCacheAvailable}");

                int matchedCount = 0;
                int processedCount = 0;
                List<string> sampleMaps = new List<string>();

                foreach (var map in manager.StreamFilterMaps(preset, null))
                {
                    matchedCount++;
                    processedCount++;

                    if (sampleMaps.Count < 3)
                    {
                        sampleMaps.Add($"{map.Name} (Ranked: {map.Ranked})");
                    }
                }

                Console.WriteLine($"  筛选结果: 匹配 {matchedCount} 首");
                if (sampleMaps.Count > 0)
                {
                    Console.WriteLine("  匹配示例:");
                    foreach (var sample in sampleMaps)
                    {
                        Console.WriteLine($"    - {sample}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  错误: {ex.Message}");
                Console.WriteLine($"  堆栈: {ex.StackTrace}");
            }
        }
    }
}