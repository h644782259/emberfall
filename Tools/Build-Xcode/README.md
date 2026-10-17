# Emberfall Xcode 导出

统一工程的完整说明见 [Docs/Build-Xcode.md](../../Docs/Build-Xcode.md)，真机与模拟器安装步骤见 [Docs/iOS.md](../../Docs/iOS.md)。

```bash
bash Tools/Build-Xcode/Build.command all
bash Tools/Build-Xcode/Build.command device
bash Tools/Build-Xcode/Build.command simulator
```

`Build.command` 默认导出两个目标，并恢复运行前的 Player Settings。Unity 导出不包含 Xcode 编译、签名、安装或启动；详细输出路径和日志见上述文档。

普通构建自动剔除 `EMBER_TERRAIN_LEGACY`、`EMBER_WORLD_LEGACY_CLIP` 两个旧版 A/B 分支，保留遮挡淡出和实例化变体；移动端简化草地改用材质标记，不再增加编译变体。只有需要运行旧版地形或裁剪对照时，才在直接调用 Unity 的命令中添加 `-emberfallIncludeShaderReviews`；普通导出脚本不启用它。

只打真机包时使用 `device`；双击默认的 `all` 会依次导出模拟器和真机两个目标。
