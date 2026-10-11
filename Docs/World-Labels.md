# 世界场景中文标签

营地四柱显示「星核、观星学徒、装备图鉴、职业试炼」；地面显示「营地」，
传送门与副本地面标题统一为「沉星遗迹」。这是场景文字修正，未给装饰柱新增交互。
场景对象的英文内部名称保持不变，以免影响查找对象与旧测试。

## 随包字体

`Assets/Resources/Fonts/EmberfallWorldLabels.otf` 为专用中文子集字体（17,584 字节），
作为 `WorldBuilder.Label` 固定标签的后备字体。本 iOS 分支优先保留主线 `GameFont.Shared` 的完整 Noto Sans SC 字库；UI、掉落与新增浮字也使用该共享字体。子集随构建打包，不依赖设备
是否安装微软雅黑、苹方等字体。TextMesh 同时绑定该字体及其材质，并请求 64 像素字形。
保留原标签的世界位置、居中锚点、颜色和字号比例；四字中文标题比原长英文标题更紧凑。

源字体为 Noto Sans CJK SC Regular 2.004，来自 `fonts-noto-cjk` 的
`NotoSansCJK-Regular.ttc` 第 2 个索引（从 0 起计），使用 fontTools 4.61.1 截取
全部 18 个实际中文字形。修改后的主字体名为 `Emberfall World Labels`，保留原始版权
并附 SIL Open Font License 1.1。完整许可既在字体元数据内，也在同目录的
`EmberfallWorldLabels-LICENSE.txt` 中，后者同样位于 Resources 中随构建保留。

- 上游：https://github.com/notofonts/noto-cjk
- 原 TTC SHA-256：`b76b0433203017ca80401b2ee0dd69350349871c4b19d504c34dbdd80541690a`
- 字体格式与打包：[Unity 字体资源文档](https://docs.unity3d.com/2022.3/Documentation/Manual/class-Font.html)
- 字体与材质绑定：[Unity Text Mesh 文档](https://docs.unity3d.com/Manual/create-meshes-text-strings.html)

新增标签文字时须同步更新子集字形与验证期望，不能把该子集当通用 UI 字体使用。
字体资源由项目共享；切换区域不会销毁该资源或创建每标签专属材质。

## 验证

无需 Unity 的资源检查（开发工具需要 Python 的 `fonttools`）：

```sh
python3 Tools/validate-world-labels.py
# 可选：Pillow 绘制字体样张，不能据此声称游戏渲染通过
python3 Tools/validate-world-labels.py --preview /tmp/emberfall-world-label-font.png
```

检查七处文本、内部对象名称、资源/材质绑定、字体打包设置、许可证，以及每个字符
在实际后备子集二进制字体中的 cmap 与非空轮廓。完整 Noto 字库沿用主线原始 blob，本次云检查不代表其 Unity 导入或设备渲染已验证。

`WorldLabelValidation` 已接入 Runtime smoke test，在原野及副本分别检查实际 TextMesh
文字、字体材质、字形几何与标签宽度。它必须在可启动 Unity 的机器上运行。
本次云端仅完成资源检查与编译/逻辑测试；Unity 因既有本地 IPC 限制无法启动，
因此没有游戏画面、Unity 字体导入或 Windows/iOS 真机渲染通过结论。
