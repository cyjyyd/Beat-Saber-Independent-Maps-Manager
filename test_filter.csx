#!/usr/bin/env dotnet-script
#r "E:\C#源码\cyjyyd\BSIMM\bin\Debug\net9.0-windows10.0.17763.0\BeatSaberIndependentMapsManager.dll"
#r "nuget: Newtonsoft.Json, 13.0.3"

using System;
using System.IO;
using Newtonsoft.Json;
using BeatSaberIndependentMapsManager;

Console.WriteLine("=== 本地缓存筛选测试 ===\n");

string cachePath = @"E:\cache.json";

// 测试1: 直接解析JSON并统计
Console.WriteLine("测试1: 直接解析JSON并统计Ranked=true...");
int directCount = 0;
int totalCount = 0;

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

    while (jsonReader.Read() && jsonReader.TokenType != JsonToken.EndArray)
    {
        if (jsonReader.TokenType == JsonToken.StartObject)
        {
            var map = serializer.Deserialize<BeatSaverMap>(jsonReader);
            totalCount++;

            if (map != null && map.Ranked)
            {
                directCount++;
                if (directCount <= 3)
                {
                    Console.WriteLine($"  找到: {map.Name} (ID: {map.Id}, Ranked: {map.Ranked})");
                }
            }
        }
    }
}

Console.WriteLine($"  直接解析结果: 总数={totalCount}, Ranked=true={directCount}\n");

// 测试2: 使用LocalCacheManager筛选
Console.WriteLine("测试2: 使用LocalCacheManager筛选...");

// 创建测试预设
var preset = new FilterPreset("测试");
var group = new FilterGroup("组1");
group.UseLocalCache = true;

var condition = new FilterCondition(FilterConditionType.Ranked);
condition.Value = true;
condition.IsEnabled = true;
group.AddCondition(condition);
preset.AddGroup(group);

Console.WriteLine($"  条件类型: {condition.Type} = {(int)condition.Type}");
Console.WriteLine($"  条件值: {condition.Value}");
Console.WriteLine($"  HasValue: {condition.HasValue()}");
Console.WriteLine($"  RequiresLocalCache: {FilterConditionMetadata.RequiresLocalCache(condition.Type)}");

// 创建管理器
var manager = new LocalCacheManager();
// 使用反射设置缓存路径
var cachePathField = typeof(LocalCacheManager).GetField("cachePath",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
cachePathField?.SetValue(manager, cachePath);

var cacheAvailableField = typeof(LocalCacheManager).GetField("cacheAvailable",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
cacheAvailableField?.SetValue(manager, true);

int managerCount = 0;
foreach (var map in manager.StreamFilterMaps(preset, null))
{
    managerCount++;
}

Console.WriteLine($"  LocalCacheManager筛选结果: {managerCount}\n");

// 测试3: 手动测试MatchesCondition
Console.WriteLine("测试3: 手动测试MatchesCondition...");
Console.WriteLine($"  preset为null时返回: {manager.MatchesFilter(null, null)}");
Console.WriteLine($"  preset为空组时返回: {manager.MatchesFilter(new FilterPreset("空"), null)}");

// 获取一个Ranked=true的map来测试
BeatSaverMap testMap = null;
using (var fs = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
using (var sr = new StreamReader(fs))
using (var jr = new JsonTextReader(sr))
{
    var ser = new JsonSerializer();
    while (jr.Read())
    {
        if (jr.TokenType == JsonToken.StartArray && jr.Path == "docs")
            break;
    }
    while (jr.Read() && jr.TokenType != JsonToken.EndArray)
    {
        if (jr.TokenType == JsonToken.StartObject)
        {
            var m = ser.Deserialize<BeatSaverMap>(jr);
            if (m != null && m.Ranked)
            {
                testMap = m;
                break;
            }
        }
    }
}

if (testMap != null)
{
    Console.WriteLine($"  测试曲谱: {testMap.Name}");
    Console.WriteLine($"  Ranked属性值: {testMap.Ranked}");
    Console.WriteLine($"  MatchesFilter结果: {manager.MatchesFilter(testMap, preset)}");
}

Console.WriteLine("\n=== 测试完成 ===");