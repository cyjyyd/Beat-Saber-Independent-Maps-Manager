# Beat Saber Independent Maps Manager (BSIMM)

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)

**Beat Saber 独立曲包管理器** 是一款功能强大的 Windows 桌面应用程序，专为 Beat Saber 玩家设计，提供自定义歌曲地图的整理、分类、导出和搜索功能。

**下载地址：** [GitHub Releases](https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager/releases)

---

## 功能特性

### 核心功能

| 功能 | 描述 |
|------|------|
| 多版本游戏管理 | 自动检测 Steam/Oculus 平台的多个 Beat Saber 安装实例，支持跨版本曲包共享 |
| 曲包文件夹管理 | 将歌曲分类文件夹作为独立曲包添加至游戏，或导出为 `.bplist` 歌单文件 |
| BeatSaver 搜索集成 | 内置 BeatSaver API 搜索，支持高级筛选条件筛选歌曲 |
| 歌单编辑 | 创建、编辑、导出歌单，支持自定义封面图片 |
| 本地缓存筛选 | 下载 BeatSaver 全量缓存数据，支持离线筛选和批量导出 |

### 详细功能列表

- **游戏实例管理**
  - 自动检测 Steam 和 Oculus 版本的 Beat Saber 安装
  - 支持多版本共存，自动识别版本号
  - Mod 安装状态检测（SongCore、BSML、SiraUtil）
  - 盗版游戏检测与提示

- **歌曲管理**
  - 拖拽添加歌曲分类文件夹
  - 歌曲完整性检测（缺失谱面文件、封面等）
  - 重复添加识别
  - 散落歌曲合并功能
  - Hash 缓存加速加载

- **歌单功能**
  - 创建自定义歌单
  - 自动生成歌单封面（从歌曲封面合成）
  - 支持自定义歌单封面
  - 导出收藏歌曲文件
  - 批量导出筛选结果为 `.bplist`

- **BeatSaver 搜索**
  - 关键词搜索（歌曲名、艺术家、谱师等）
  - 高级筛选系统（BPM、NPS、时长、星级等）
  - Mod 支持筛选（Chroma、Noodle Extensions、Mapping Extensions、Cinema、Vivify）
  - 排行榜筛选（ScoreSaber/BeatLeader 排位/待评级）
  - 筛选预设保存与加载
  - 音频预览播放（使用 NAudio）

- **本地缓存模式**
  - 下载 BeatSaver 全量数据缓存
  - 支持更丰富的筛选条件
  - 批量筛选多个预设
  - 结果数量限制与排序

---

## 系统要求

| 项目 | 要求 |
|------|------|
| 操作系统 | Windows 10/11 |
| 运行时 | .NET Framework 4.8 或 .NET 8.0+ |
| 游戏依赖 | Beat Saber (Steam/Oculus 正版) + SongCore Mod |

### 可选组件

- **Everything** (推荐): 安装后可启用增强模式，自动检测磁盘中的多个 Beat Saber 安装实例
  - 下载地址: [Voidtools Everything](https://www.voidtools.com/)
  - 安装后保持 Everything 服务运行即可

---

## 安装与使用

### 安装

1. 从 [Releases](https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager/releases) 页面下载最新版本
2. 解压到任意目录
3. 运行 `BeatSaberIndependentMapsManager.exe`

### 基本使用

#### 方法一：添加曲包到游戏（PC 平台）

1. 将歌曲分类文件夹拖入程序窗口
2. 点击 **保存列表**
3. 歌曲将作为独立分类曲包添加至 Beat Saber

#### 方法二：导出歌单文件（全平台）

1. 将歌曲分类文件夹拖入程序窗口
2. 点击 **导出歌单**
3. 将生成的 `.bplist` 文件放入游戏的 `Playlists` 目录
4. 需要安装 PlaylistManager Mod，未下载的歌曲需要在游戏中下载后才能游玩

---

## 项目架构

### 技术栈

- **框架**: .NET Framework 4.8 / .NET 8.0 Windows Forms
- **JSON 处理**: Newtonsoft.Json
- **音频播放**: NAudio (支持 Ogg Vorbis)
- **HTTP 请求**: HttpClient
- **文件搜索**: Everything SDK (可选增强)

### 目录结构

```
BeatSaberIndependentMapsManager/
├── Program.cs              # 程序入口
├── MainForm.cs             # 主窗体 (Tabbed UI)
├── SongMap.cs              # 歌曲地图数据模型
├── PlayList.cs             # 歌单容器模型
├── BSVerInfo.cs            # Beat Saber 版本信息
├── MapHash.cs              # 地图 Hash 包装类
├── Config.cs               # INI 配置管理
├── BeatSaverAPI.cs         # BeatSaver API 客户端
├── BeatSaverMapSlim.cs     # 轻量级地图数据结构
├── LocalCacheManager.cs    # 本地缓存管理器
├── LocalCacheReader.cs     # 缓存流式读取器
├── FilterCondition.cs      # 筛选条件定义
├── FilterConditionType.cs  # 筛选条件类型枚举
├── FilterGroup.cs          # 筛选条件组
├── FilterPreset.cs         # 筛选预设
├── FilterBuilderForm.cs    # 筛选构建器窗体
├── FilterBuilderPanel.cs   # 筛选构建器面板
├── SettingsForm.cs         # 设置窗体
├── UpdateManager.cs        # 自动更新管理器
├── GitHubReleaseClient.cs  # GitHub API 客户端
├── ProgressBarEx.cs        # 自定义进度条
├── VolumeBarEx.cs          # 自定义音量条
├── assets/
│   ├── json/               # JSON 数据文件
│   │   └── bs-versions.json  # Beat Saber 版本信息
│   ├── temp/               # 临时文件
│   └── scripts/            # 脚本文件
└── tests/                  # 单元测试项目
```

### 核心数据模型

#### SongMap
代表一个 Beat Saber 自定义歌曲，包含：
- 歌曲元数据（名称、艺术家、谱师、BPM）
- 难度信息
- 文件路径引用

#### PlayList
歌单容器，包含：
- 歌单标题、作者、描述
- 封面图片（Base64）
- 歌曲 Hash 列表

#### FilterPreset / FilterGroup / FilterCondition
三级筛选系统：
- `FilterPreset`: 筛选预设，包含多个条件组
- `FilterGroup`: 条件组，支持 AND/OR 逻辑
- `FilterCondition`: 单个筛选条件

---

## 高级筛选系统

### 支持的筛选条件

#### 基本条件
| 条件 | 类型 | 说明 |
|------|------|------|
| 搜索关键词 | 文本 | 支持指定搜索字段（歌曲名、艺术家、谱师等） |
| 排序方式 | 选择 | Latest/Relevance/Rating/Curated/Random/Duration |

#### 数值范围
| 条件 | 单位 |
|------|------|
| BPM | 节拍/分钟 |
| NPS | 每秒音符数 |
| 时长 | 秒 |
| SS 星级 | ScoreSaber 星级 |
| BL 星级 | BeatLeader 星级 |

#### Mod 支持
- Chroma
- Noodle Extensions
- Mapping Extensions
- Cinema
- Vivify
- 自定义 Mod（包含/排除）

#### 本地缓存专属条件
| 条件 | 说明 |
|------|------|
| SS/BL 排位 | 已排位歌曲 |
| SS/BL 待评级 | 等待评级歌曲 |
| 游玩/下载次数 | 统计数据 |
| 点赞/踩数/比例 | 社区评价 |
| Sage 分数 | 谱面质量评分 |
| 标签 | 包含/排除特定标签 |
| 上传时间 | 时间范围筛选 |
| 难度参数 | NJS、炸弹数、偏移、事件数等 |

### 筛选预设

筛选条件可以保存为预设文件（JSON 格式），方便重复使用：

```json
{
  "Name": "高难度排位歌曲",
  "Description": "NPS 8+ 的 ScoreSaber 排位歌曲",
  "Groups": [
    {
      "Name": "主条件",
      "Conditions": [
        { "Type": "NpsRange", "Value": { "Min": 8.0 } },
        { "Type": "Ranked", "Value": true }
      ]
    }
  ]
}
```

---

## 配置文件

程序配置保存在 `BSIMM.ini`：

```ini
[Settings]
HashCache=true      ; 启用 Hash 缓存加速加载
LastFolder=true     ; 记住上次打开的文件夹
DownProxy=false     ; 下载代理设置
LocalCache=false    ; 本地缓存模式
SkipVersion=        ; 跳过的更新版本号
LastUpdateCheck=    ; 上次检查更新时间
```

---

## 开发指南

### 构建项目

```bash
# 克隆仓库
git clone https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager.git
cd Beat-Saber-Independent-Maps-Manager

# 构建
dotnet build BeatSaberIndependentMapsManager.sln

# 发布 (Release)
dotnet build BeatSaberIndependentMapsManager.sln -c Release
```

### 运行测试

```bash
dotnet test tests/BeatSaberIndependentMapsManager.Tests/BeatSaberIndependentMapsManager.Tests.csproj
```

### 依赖项

| 包 | 用途 |
|-----|------|
| Newtonsoft.Json | JSON 序列化/反序列化 |
| NAudio | 音频播放（Ogg Vorbis 支持） |
| Everything64.dll | Everything SDK（可选，增强文件搜索） |

---

## 依赖说明

### Beat Saber Mods
- **SongCore** (必需): 自定义歌曲加载核心 Mod
- **PlaylistManager** (可选): 歌单管理 Mod，用于游戏中显示 `.bplist` 歌单

### 增强扩展
- **Everything** (推荐): Voidtools 出品的文件搜索工具
  - 启用后可自动检测磁盘中的所有 Beat Saber 安装
  - 支持快速扫描散落歌曲

---

## 常见问题

### Q: 程序提示"未检测到Beat Saber实例目录"？
A: 请确保：
1. 已安装正版 Beat Saber（Steam 或 Oculus 版本）
2. 已正确安装 SongCore Mod
3. 尝试安装 Everything 并保持运行

### Q: 导出的歌单在游戏中不显示？
A: 请确保：
1. 已安装 PlaylistManager Mod
2. 歌单文件放在正确的 `Playlists` 目录下
3. 歌单文件格式正确（`.bplist`）

### Q: 筛选结果为空？
A: 可能原因：
1. 本地缓存未下载或已过期
2. 筛选条件过于严格
3. 网络连接问题（在线搜索模式）

---

## 许可证

本项目采用 MIT 许可证开源。详见 [LICENSE](LICENSE) 文件。

---

## 致谢

- [BeatSaver](https://beatsaver.com/) - Beat Saber 自定义歌曲平台
- [BSC-ScrapeData](https://github.com/qe201020335/BSC-ScrapeData) - BeatSaver 缓存数据源
- [Everything](https://www.voidtools.com/) - 文件搜索工具
- [NAudio](https://github.com/naudio/NAudio) - .NET 音频库

---

## 贡献

欢迎提交 Issue 和 Pull Request！

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

---

**作者**: @万毒不侵

**项目主页**: [GitHub](https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager)