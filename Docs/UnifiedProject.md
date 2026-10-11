# 统一项目维护

`Assets`、`Packages`、`ProjectSettings` 是 Windows、iPhone 和 iPad 共用的 Unity 工程。平台输入、布局、渲染与生命周期由运行平台选择；修复共享玩法时只维护这一份源码。

## 来源

合并前已同步两个旧仓的远端 `main`，并保留其本地工作修改：

- iOS：`11d605adbeddf54b325cdea5a4d18b2ffa1c83d2`
- Windows：`6ce40ea0c542f33c689396ef7e0754e581bc03e9`

以 iOS 的存档、经济与奖励提交规则为基准，整合 Windows 的城镇服务、世界设施和桌面功能入口。装备等级档、掉落概率、奖励回执和存储安全保持原有规则。共享资源保留既有 GUID；新增资源带完整 `.meta`。

## 平台构建

使用 `ProjectSettings/ProjectVersion.txt` 指定的 Unity 版本。Windows 构建入口为 `Tools/Build-Windows.ps1`，iOS 导出入口为 `Tools/Build-Xcode/Build.command`。模拟器与真机导出目录独立，构建产物保持本地。

应用名称、公司名和 iOS Bundle ID 保持已有身份，已有存档仍位于原应用数据目录。源码统一不改变云同步能力。更新游戏应保留应用容器；不要通过卸载清空存档。

## 仓库范围

维护游戏源码、运行资源、美术源文件、可复用构建与回归入口及必要设计说明。日志、截图、测试运行记录、报告、临时工具、个人存档、编辑器缓存和构建目录通过 `.gitignore` 排除。验证和预览输出写入忽略目录，禁止把输出再次作为交付文件加入 Git。
