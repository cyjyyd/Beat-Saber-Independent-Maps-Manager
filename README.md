# Beat Saber Independent Maps Manager
提示：功能开发中，本软件依据BSSFM（Beat Saber Song Folder Manager）提供的功能使用C#重构，并预期在未来添加更多功能：（如自动检测当前目录）
能够将曲包文件夹作为单独歌单添加至游戏中（PC平台），还可以导出为Playlist(.bplist)歌曲列表（**全平台**），以及导出收藏歌曲（**全平台**）。
**下载地址：**[点击进入](https://github.com/cyjyyd/Beat-Saber-Independent-Maps-Manager/releases)

### 两种使用方法
1. 拖入歌曲分类文件夹，然后点击`保存列表`即可将该文件夹内的歌曲作为单独分类曲包添加至PC版Beat Saber（推荐）；
2. 拖入歌曲分类文件夹，然后点击`导出歌单`即可将该文件夹内的歌曲导出为`.bplist`格式歌曲列表，将该列表文件添加至已安装PlaylistManager(Mod)的游戏对应目录下，即可在游戏中找到歌单，对于没有下载过的歌曲，需要在游戏中下载后才可游玩。

### 支持功能
* 游戏路径识别
* 编辑歌曲存放路径
* 拖放添加
* 生成歌单封面
* 导出收藏歌曲文件
* 导出收藏歌曲列表
* 将曲包导出为Playlist (.bplist)
* 正负一级路径纠错
* 重复添加识别
* 有效歌曲数量识别
* Mod安装有效性识别（测试）

### 关于本项目
本项目将曲包文件夹作为单独歌单添加至游戏中是通过SongCore这个PC平台BeatSaber基础Mod来实现的。  

如使用此项目为用户提供服务，需注明原作者并提供源码。
