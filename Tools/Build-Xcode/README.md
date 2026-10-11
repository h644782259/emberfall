# Emberfall Xcode 导出

统一工程的完整说明见 [Docs/Build-Xcode.md](../../Docs/Build-Xcode.md)，真机与模拟器安装步骤见 [Docs/iOS.md](../../Docs/iOS.md)。

```bash
bash Tools/Build-Xcode/Build.command all
bash Tools/Build-Xcode/Build.command device
bash Tools/Build-Xcode/Build.command simulator
```

`Build.command` 默认导出两个目标，并恢复运行前的 Player Settings。Unity 导出不包含 Xcode 编译、签名、安装或启动；详细输出路径和日志见上述文档。
