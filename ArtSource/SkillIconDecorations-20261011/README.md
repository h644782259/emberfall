# 技能图标实体装饰

使用内置 imagegen 生成 8 种真实材质的周边装饰，完整提示词见 prompts.json。主体来自 SkillIdentity 的同一技能实际三维效果摄影；装饰仅增加 UI 的填充感，不代表额外命中范围、技能效果或装备。

本目录保留 8 种材质的原始 PNG 与生成提示词。运行时成品位于 `Assets/Resources/IconArt/skill-compositions-*`；同目录的 `skill-entities-*` 保留技能主体图，既有 `skills-*` 保留绘画图标。

运行时仅读取烘焙后的 128px 图片，没有 GPU 读回、逐帧裁切或新增绘制。冷却沿用自下而上恢复和居中计时，边框在图片上层呈现。
