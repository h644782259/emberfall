# 路线与具体成长目标（策划 E 第一批）

路线卡现在区分「基础不足」「基础循环可用」「机制强化齐备」。反击、叠毒引爆、碎冰/灼烧、伙伴指挥均不把可选机制装备当成基础条件。基础按真实职业技能、平台可用快捷栏、元素专精和召唤路线判断；每张卡只给一个下一步骤，跳转对应技能、切换路线、去行囊换装或明确选择可选核心目标。无额外机制需求的路线不会虚构装备门槛。

具体核心目标使用机制枚举与最低品质作为身份，可以追踪尚未拥有的第二核心。持有其他核心、曾经领取首通或发现其他配方都不能完成它。稀有装备不能选择升华目标；列表提供对应史诗核心前置目标，实际取得后用户再自行选择这件史诗装备的升华，不自动切换目标。

变体、重铸和升华目标绑定装备稳定 ID，选择资格与操作 API 共源。变体执行入口为一次性解锁，不用可反向切换的 Toggle 代替；重铸记录选择时目标等级，实际重铸使用当前角色等级/部位强化。移除目标装备后，同名替代品不会接管原目标。完成后目标仍保留。

`SelectedProgressionGoal(inCamp)` 返回稳定 `Identity` 和单个下一操作；`ActionIdentity` 还包括动作与装备 ID。UI 提交该动作标识，拒绝已过时按钮：例如兑换成功后的重复兑换请求不能自动穿戴或再扣材料。定向领取（含待领取/恢复栏）、穿戴、变体解锁、重铸、升华都调用现有事务操作。营地、冒险入口和结算继续通过同一个 `ProgressionGoalStatus` 展示材料、里程碑及本局所得。

旧存档的泛化 Core 目标保留 `Core/legacy` 兼容态与原首件完成含义，不凭职业默认值发明具体核心；显示需明确选择，且不给定向领取动作。畸形/跨职业机制身份恢复为未选择具体机制。迁移只规范内存、不在读取时写盘。枚举旧序号不变，新 Reforge/ClassTutorial 仅追加。

HUD 共源优先级为：房间/地下城胜利阻碍 → 营地可执行操作 → 自选目标 → 未选择目标时的职业教学。教学检查当前平台可用技能；召唤初始伙伴可直接教学，无需购买共鸣装备。桌面已调用 `TryGrowthHudHint(out title,out detail)`；移动普通营地/原野 HUD 由 A07 owner 集成同一方法，特殊模式卡不替换。

## 验证与边界

- `ProgressionGoalIdentityTests.Run(root)`：52 项真实 ProgressionService 持久事务、旧/坏目标迁移、缺失第二核心、目标物品移除、材料/首通写入失败、重复动作、待领取链和重启身份检查。
- `ProgressionRouteLayerTests.Run(root)`：47 项路线三层、四职业×两路线×平台 Ready witnesses、真实装备部位/等级、单一步骤、HUD 优先级与教学可用性检查。
- 既有 `ProgressionGrowthTests` 2204 项及 `AdventureProgressionTests` 103 项通过。旧重复选择写失败夹具改成实际切换目标：重复选择同一目标现在幂等、不写盘。
- `ProgressionGrowthSourceTests.py` 19 项通过；升华资格源码检查跟随共用 `MechanicGoalEligibility`，真实资格和失败行为另由上述生产测试检查。
- 所有运行时源码使用已有 **Unity 2021 参考 DLL** 做 Android 条件编译，零警告、零错误；这仅检查可访问 API/语法，不是 Unity 6 编译或设备运行。Unity 6、触控/文字布局和真实 HUD 视觉仍待固定 SHA 原生验收。

测试编译：ProgressionService 的所有托管测试新增 `Assets/Scripts/Core/ProgressionGoalState.cs` 依赖。路线/HUD测试另外依赖 `CampRouteCards.cs`、`RunChoices.cs`、`RunChoices.Rooms.cs`、`ProgressionAttention.cs`、`ProgressionHudHint.cs`；使用现有 GameTypes/ProgressionService/ProgressionTests 与其常规 Core 支架，不混入其他 Unity 桩。
