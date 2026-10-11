# Emberfall · 星烬纪元

Unity 3D 即时战斗 RPG，Windows、iPhone 和 iPad 共用此源码工程。四职业战斗、技能成长、召唤、装备、城镇旅行、副本和多角色存档共享实现；桌面键鼠与移动触控各自保留对应布局和性能策略。

```bash
git clone https://github.com/h644782259/emberfall.git
cd emberfall
```

用 Unity Hub 打开包含 `Assets`、`Packages`、`ProjectSettings` 的仓库根目录，安装 `ProjectSettings/ProjectVersion.txt` 指定的 Unity（当前为 **6000.6.4f1**）。入口场景为 `Assets/Scenes/Main.unity`，运行时资源在 `Assets/Resources`。

## Windows 构建与安装

需要 Windows 10/11、Windows PowerShell 5.1、Git for Windows，以及该 Unity 版本的 **Windows Build Support (Mono)**。关闭游戏和打开此项目的 Unity 编辑器，在仓库根目录执行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Build-Windows.ps1
```

构建输出为 `Builds\Windows\Emberfall.exe`；分发时必须带上整个目录。脚本按项目版本定位 Unity，并显式构建 Windows64，不继承编辑器最近选择的 iOS 目标。

双击 `Tools\WindowsInstall\Install.cmd` 可更新源码、构建、打包、安装并核对完整文件的 SHA-256。默认安装到 `%LOCALAPPDATA%\Programs\Emberfall`。此入口要求 main：干净工作区拉取 `origin/main` 并且只接受快进；有本地源码改动则保留改动、跳过拉取并构建当前源码；不自动提交或推送。完整参数与输出见 [Windows 安装说明](Tools/WindowsInstall/README.md)。

## iPhone / iPad 构建与安装

需要 macOS、完整 Xcode、指定 Unity 版本和 **iOS Build Support**。先检查工具，再选择导出目标：

```bash
bash Tools/Export-iOS.sh --check-only
bash Tools/Build-Xcode/Build.command simulator
bash Tools/Build-Xcode/Build.command device
# 依次导出模拟器和真机：bash Tools/Build-Xcode/Build.command all
```

模拟器与真机分别输出到 `Builds/iOS/Simulator/Emberfall.xcodeproj` 和 `Builds/iOS/Xcode/Emberfall.xcodeproj`，两套 IL2CPP 工程独立。打开对应工程并选择 `Emberfall` scheme；真机需要选择自己的签名团队和连接的设备。默认 Bundle ID 为 `com.h644782259.emberfall.ios`，iPhone+iPad 通用、横屏、ARM64、Metal，最低 iOS 15。

[Mac/Xcode 操作说明](Docs/iOS.md) 和 [导出入口与日志](Docs/Build-Xcode.md) 区分 Unity 导出、Xcode 编译、签名、安装、启动及实际设备验收。导出脚本使用当前本地源码并恢复运行前的 Player Settings；不拉取或推送 Git。

## 操作与存档

桌面使用 WASD 移动、空格跳跃、Shift 闪现、鼠标左键普攻、F 使用生命药剂；I 行囊、K 技能、J 成就、P 商店、O 铁匠、M 旅行地图、E 场景交互、Esc 暂停。技能槽按游戏内绑定施放，右键与滚轮控制镜头或取消当前施法状态。

移动端左侧浮动摇杆与右侧攻击可同时操作，主动技能直接瞄准释放，被动自动生效；跳跃、闪现、药剂和技能栏按手机/平板安全区布局。进入游戏、回城和切换副本时，整备界面显示阶段与进度，场景准备完成后才恢复行动。

新角色创建独立存档；标题页可选择角色，暂停菜单可保存或读取。每份 `emberfall-save*.json` 带备份与恢复状态，备份时保留完整存档目录。Windows 存档位于 `%USERPROFILE%\AppData\LocalLow\EmberfallStudio\Emberfall`，iOS 位于应用沙盒。统一源码仓库不提供跨设备云存档；更换 Bundle ID 或删除 App 前应先备份设备容器。

## 项目维护

- [统一源码与平台维护](Docs/UnifiedProject.md)
- [维护文档](Docs/README.md)
- [可复用验证入口](Tools/cloud-validation.md)

源码、资源、Unity `.meta` 和必要维护工具纳入 Git。测试记录、报告、日志、一次性脚本、截图、个人存档、签名材料、编辑器缓存和构建产物保持本地。
