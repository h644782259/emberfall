# Windows 一键构建与安装

使用统一仓库 [h644782259/emberfall](https://github.com/h644782259/emberfall) 的完整源码。此文件夹中的 `Install.cmd` 调用 `Run-Install.ps1`，后者调用同目录的 `Build-Install-Windows.ps1` 和 `Update-Main.ps1`，再调用仓库 `Tools` 中的构建、打包工具和安装器源码。

## 准备

- Windows 10/11、Windows PowerShell 5.1 或更新版本、Git for Windows与可用的GitHub访问凭据。
- Unity Hub中安装 `ProjectSettings/ProjectVersion.txt` 指定版本（当前6000.6.4f1）及 Windows Build Support (Mono)，并激活许可证。
- 关闭游戏和打开此项目的Unity编辑器。

构建脚本按项目版本定位编辑器；自定义安装位置可传 `-UnityPath`，应指向同版本Unity。构建CLI显式选择Windows64，输出 `BuildsWindows`。

## 运行

在仓库内双击 `ToolsWindowsInstallInstall.cmd`。需要桌面入口时，为这个仓库内的 `Install.cmd` 创建快捷方式；四个脚本保留在原目录，不能只复制两个文件到桌面。

入口要求main分支：干净工作区执行 `git pull --ff-only origin main`；存在本地源码改动时保留改动、跳过拉取并构建当前源码，不自动提交、推送或丢弃修改。更新失败或不能快进时停止。Unity更新的ProjectVersion不会被丢弃，若阻碍pull也会停止。

默认安装到 `E:\EMBERFALL-exe`，安装包默认位于独立的 `E:\EMBERFALL-packages` 目录，并创建 `Emberfall` 桌面快捷方式。成功时直接结束；失败时 `Install.cmd` 保留错误窗口并等待按键。游戏存档位于独立的LocalLow目录，安装更新保留存档。

在仓库根目录自定义参数：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .ToolsWindowsInstallRun-Install.ps1 -InstallDirectory "E:Emberfall" -PackageDirectory "E:EmberfallInstaller"
```

可选 `-UnityPath "D:UnityEditorUnity.exe"`、`-SkipDesktopShortcut`。源码在其它位置时可传 `-ProjectDirectory "D:Projectsemberfall"`；不存在合法项目时明确停止，不回退到旧E盘源码。

安装与安装包目录必须相互独立、不能相同或相互嵌套，且必须是仓库外的绝对本地专用目录，不能是磁盘根目录；选择存在且可写的盘符。无需自定义路径时使用当前用户默认目录。

## 输出

流程为源码准备 → Unity构建 → 打包 → 安装 → 核对全部构建文件SHA-256 → 创建快捷方式。新Unity日志必须包含本次成功标记才可打包，不能复用旧EXE冒充新构建。

- `BuildsWindowsEmberfall.exe`：与该目录全部配套文件一同分发。
- 安装包目录中的 `Emberfall-Setup.exe`、`Emberfall-Windows-x64.zip` 和 `SHA256SUMS.txt`。
- 安装目录中的 `build-info.json`：源码路径、提交、是否包含本地改动和构建UTC时间。
- `Logswindows-build.log`：Unity构建日志；`Logswindows-install.log`：入口运行记录。

脚本不自动启动游戏。完成后双击桌面快捷方式；构建、安装、启动与实际游玩验收应分别记录。只运行发行安装器的电脑不需要Unity或源码。
