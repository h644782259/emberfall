# Emberfall 统一工程：Xcode 导出

在 macOS 上双击`Tools/Build-Xcode/Build.command`，或在终端执行：

```bash
bash Tools/Build-Xcode/Build.command
```

默认按顺序生成模拟器和真机工程。脚本可从任意工作目录调用；以上相对路径命令需在仓库根目录运行。

```bash
# 同时生成模拟器和真机工程
bash Tools/Build-Xcode/Build.command all
# 只生成真机工程
bash Tools/Build-Xcode/Build.command device
# 只生成 Apple Silicon ARM64 模拟器工程
bash Tools/Build-Xcode/Build.command simulator
```

固定输出路径：

- 真机：`Builds/iOS/Xcode/Emberfall.xcodeproj`
- 模拟器：`Builds/iOS/Simulator/Emberfall.xcodeproj`

在 Xcode 中打开对应工程，选择 `Emberfall` Scheme。旧导出会被替换，不保留历史导出；导出失败时保留此前工程。个人签名团队沿用现有工程设置。

## 前提和操作范围

需要 macOS、完整 Xcode，以及 `ProjectSettings/ProjectVersion.txt` 指定版本的 Unity 和 iOS Build Support。若 Unity 不在默认安装位置，可设置 `UNITY_PATH` 为 Unity 可执行文件路径。

运行前关闭此项目的 Unity 和 Xcode，运行期间不要修改 Player Settings。脚本使用**本地当前代码（包含未提交修改）**，不会拉取代码、提交或推送，也不会执行 Xcode 编译、签名、安装或启动应用。

脚本会保存并恢复运行前的 `ProjectSettings.asset`，避免导出切换 SDK 留下配置修改；不会用 Git HEAD 覆盖你的本地设置。脚本通过锁目录防止自身重复运行，请勿同时手动运行底层导出脚本。

## 失败排查

Unity 详细日志：

- `Logs/ios-export-simulator-latest.log`
- `Logs/ios-export-device-latest.log`

运行时每 15 秒显示最近一条 Unity 日志。若持续显示 Licensing / LicenseClient，表示授权服务尚未就绪，并非正在编译。终端出现错误后停止执行，可解决错误后重新运行。授权异常可先退出 Unity、重启 Unity Hub 并确认许可证正常。脚本被强制结束后，确认没有导出进程在运行，再删除 `Logs/xcode-export.lock`；系统临时目录中可能保留 `emberfall-player-settings.*` 设置备份。

授权初始化超过 120 秒且尚未进入项目导入时，会停止本次导出并报错，保留原有工程。重启编辑器不一定会重启后台授权客户端。

## 自动恢复

Unity 启动超过 120 秒会停止该次导出进程。若日志确认卡在授权初始化，脚本会在没有其他 Unity 编辑器运行时重启授权客户端，并且仅重试一次。首次失败日志保存在 `Logs/ios-export-<sdk>-latest.attempt-1.log`。检测到其他编辑器时会退出并要求先关闭它们；不会自动关闭其他项目。若重试仍失败，请在 Unity Hub 中检查登录和许可证后再执行脚本。网络或许可证失效无法靠重启保证恢复。进入项目导入后不采用这个启动超时限制。

导出前会检查所有 Unity 编辑器（包含 Unity Hub 启动的入门教程）。请关闭这些编辑器后运行；脚本不会自行关闭其他项目。没有其他编辑器时，脚本先等待旧授权客户端退出，再显式启动命名管道服务，随后启动 Unity，避免由编辑器异步启动授权服务导致反复超时。服务诊断日志在 `Logs/ios-license-service.log`。

统一源码：[h644782259/emberfall](https://github.com/h644782259/emberfall)。签名、设备选择与存档身份见 [iOS 说明](iOS.md)，Windows 构建入口见 [项目 README](../README.md)。
