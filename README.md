# Beat Saber Independent Maps Manager
提示：功能开发中，本软件依据BSSFM（Beat Saber Song Folder Manager）提供的功能使用C#重构，并预期在未来添加更多功能：（如自动检测当前目录）
能够将曲包文件夹作为单独歌单添加至游戏中（PC平台），还可以导出为Playlist(.bplist)歌曲列表（**全平台**），以及导出收藏歌曲（**全平台**）。
**下载地址：**[点击进入](https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager/releases)

### 两种使用方法
1. 拖入歌曲分类文件夹，然后点击`保存列表`即可将该文件夹内的歌曲作为单独分类曲包添加至PC版Beat Saber（推荐）；
2. 拖入歌曲分类文件夹，然后点击`导出歌单`即可将该文件夹内的歌曲导出为`.bplist`格式歌曲列表，将该列表文件添加至已安装PlaylistManager(Mod)的游戏对应目录下，即可在游戏中找到歌单，对于没有下载过的歌曲，需要在游戏中下载后才可游玩。

### 支持功能
* Beat Saber多版本游戏识别及管理
* 曲包目录跨版本共享
* 编辑歌曲存放路径，检测磁盘中散落的歌曲并进行合并
* 生成歌单封面，歌单封面自定义
* 导出收藏歌曲文件
* 导出收藏歌曲列表
* 将曲包导出为Playlist (.bplist)
* 任意文件夹添加识别（如果不知道放哪了）
* 重复添加识别
* 歌曲完整性检测（是否缺失铺面文件，封面文件等）
* Mod安装有效性识别（测试）
* 歌曲目录/歌单（.bplist)编辑功能

### 关于本项目
Beat Saber mod依赖：[Song Core]
增强扩展：[EveryThing]
部分功能依赖everything实现，请提前安装好everything或在软件内点击安装并保持运行状态。（也可以选择不安装，但是部分功能体验将降级）


