#!/usr/bin/env dotnet-script
#r "E:\C#源码\cyjyyd\BSIMM\bin\Debug\net9.0-windows10.0.17763.0\BeatSaberIndependentMapsManager.dll"
#r "nuget: Newtonsoft.Json, 13.0.3"

using System;
using BeatSaberIndependentMapsManager;

Console.WriteLine("=== 预设序列化兼容性测试 ===\n");

// 测试1: 基本条件序列化
Console.WriteLine("测试1: 基本条件序列化...");
var preset1 = new FilterPreset("测试预设");
var group1 = new FilterGroup("组1");
group1.UseLocalCache = true;

var condition1 = new FilterCondition(FilterConditionType.Ranked);
condition1.Value = true;
condition1.Operator = LogicOperator.And;
group1.AddCondition(condition1);

var condition2 = new FilterCondition(FilterConditionType.MinBpm);
condition2.Value = 120.0;
group1.AddCondition(condition2);

var condition3 = new FilterCondition(FilterConditionType.Query);
condition3.Value = "test song";
group1.AddCondition(condition3);

preset1.AddGroup(group1);

string json1 = preset1.ToJson();
Console.WriteLine($"序列化结果:\n{json1}\n");

// 反序列化
var loaded1 = FilterPreset.FromJson(json1);
if (loaded1 != null)
{
    Console.WriteLine($"反序列化成功:");
    Console.WriteLine($"  名称: {loaded1.Name}");
    Console.WriteLine($"  组数: {loaded1.Groups.Count}");
    foreach (var g in loaded1.Groups)
    {
        Console.WriteLine($"  组 '{g.Name}': UseLocalCache={g.UseLocalCache}");
        foreach (var c in g.Conditions)
        {
            Console.WriteLine($"    条件: {c.Type} = {c.Value} (类型: {c.Value?.GetType().Name ?? "null"})");
        }
    }
}
else
{
    Console.WriteLine("反序列化失败!");
}

// 测试2: ResultLimitValue 序列化
Console.WriteLine("\n测试2: ResultLimitValue 序列化...");
var preset2 = new FilterPreset("结果限制测试");
var group2 = new FilterGroup("组1");

var limitCondition = new FilterCondition(FilterConditionType.ResultLimit);
limitCondition.Value = new ResultLimitValue(50, ResultSortOption.Random);
group2.AddCondition(limitCondition);

var dateCondition = new FilterCondition(FilterConditionType.MinUploadedDate);
dateCondition.Value = DateTime.Now.AddDays(-7);
group2.AddCondition(dateCondition);

preset2.AddGroup(group2);

string json2 = preset2.ToJson();
Console.WriteLine($"序列化结果:\n{json2}\n");

var loaded2 = FilterPreset.FromJson(json2);
if (loaded2 != null)
{
    Console.WriteLine($"反序列化成功:");
    foreach (var g in loaded2.Groups)
    {
        foreach (var c in g.Conditions)
        {
            if (c.Type == FilterConditionType.ResultLimit && c.Value is ResultLimitValue limit)
            {
                Console.WriteLine($"  ResultLimit: Count={limit.Count}, SortOption={limit.SortOption}");
            }
            else if (c.Type == FilterConditionType.MinUploadedDate && c.Value is DateTime dt)
            {
                Console.WriteLine($"  MinUploadedDate: {dt:yyyy-MM-dd}");
            }
            else
            {
                Console.WriteLine($"  {c.Type}: {c.Value} (类型: {c.Value?.GetType().Name ?? "null"})");
            }
        }
    }
}
else
{
    Console.WriteLine("反序列化失败!");
}

// 测试3: 所有条件类型
Console.WriteLine("\n测试3: 所有条件类型的默认值序列化...");
var allTypes = FilterConditionMetadata.GetAllConditions();
var preset3 = new FilterPreset("所有条件");
var group3 = new FilterGroup("测试");

foreach (var type in allTypes)
{
    var cond = new FilterCondition(type);
    cond.SetDefaultValue();
    group3.AddCondition(cond);
}

preset3.AddGroup(group3);

string json3 = preset3.ToJson();
var loaded3 = FilterPreset.FromJson(json3);

if (loaded3 != null)
{
    int successCount = 0;
    int failCount = 0;
    foreach (var c in loaded3.Groups[0].Conditions)
    {
        if (c.Value != null)
        {
            successCount++;
        }
        else if (c.ValueType == FilterValueType.Boolean)
        {
            // Boolean 类型默认值是 null，这是正常的
            successCount++;
        }
        else
        {
            failCount++;
            Console.WriteLine($"  警告: {c.Type} 的值为 null");
        }
    }
    Console.WriteLine($"成功: {successCount}, 失败: {failCount}");
}
else
{
    Console.WriteLine("反序列化失败!");
}

// 测试4: 向后兼容性 - 模拟旧格式
Console.WriteLine("\n测试4: 向后兼容性测试 (模拟旧格式)...");
string oldFormatJson = @"{
  ""Name"": ""旧预设"",
  ""CreatedAt"": ""2024-01-01T00:00:00"",
  ""ModifiedAt"": ""2024-01-01T00:00:00"",
  ""Groups"": [
    {
      ""Name"": ""组1"",
      ""Conditions"": [
        {
          ""Type"": 0,
          ""Value"": ""test"",
          ""Operator"": 0,
          ""IsEnabled"": true
        },
        {
          ""Type"": 3,
          ""Value"": 120.5,
          ""Operator"": 0,
          ""IsEnabled"": true
        }
      ],
      ""GroupOperator"": 0,
      ""IsEnabled"": true
    }
  ]
}";

var loaded4 = FilterPreset.FromJson(oldFormatJson);
if (loaded4 != null)
{
    Console.WriteLine($"旧格式加载成功: {loaded4.Name}");
    foreach (var c in loaded4.Groups[0].Conditions)
    {
        Console.WriteLine($"  条件: {c.Type} = {c.Value} (类型: {c.Value?.GetType().Name ?? "null"})");
    }
}
else
{
    Console.WriteLine("旧格式加载失败!");
}

Console.WriteLine("\n=== 测试完成 ===");