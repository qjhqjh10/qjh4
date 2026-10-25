# 2026-10-25 第七十五/七十六会话 · 主对话进度（滚动更新）

## 已完成
1. **16 宿主全量复跑**（字体重烘后）：15 绿；`CollectionScene` **2355/2358 · 3 红**（诊断 → `Dg5_Collection3红诊断.md`）。
2. **EffectIso 两趟** rc=0（备份 `_tmp_view/iso_bak_1025/`，1190 PNG）。⚠️ **只覆盖 9 个效果**；`Void Combat` / `Raging Storm` / `ShieldTraitEffect_Idle Necron Variant` **不在 iso 集** ⇒ 要扩清单再跑。
3. **player 构建** 09:15（`_tmp_view/player/WarpforgePlayer_Data/Managed/Assembly-CSharp.dll`）。
4. **真机批量一轮**：24 张图 + `_tmp_view/shots/2026-10-25_第七十五会话/findings.md`；结论已进 `资料/真Play待验清单.md` §二。
5. **台账核销**：AUD2 五处矛盾 + 5 条顺手冲突已就地订正；用户三条拍板（存档路径改 / 成就补计数器 / D5 暂不动）已记入 `界面线` / `成就`。
6. **`companyName/productName` 已改** → `QJHWarpforge / WarpforgeReplica`（实际写的是 `WarpforgeReplica`），旧档不迁移。
7. **完整核查**（4 个代理，≈150 条目）→ 4 份 `AUD3_*.md`；约 50 条「早做完但没核销」。
8. **判据**：`待办判据_引擎_1025b.md` / `待办判据_成就计数器_1025b.md`。

## 主对话裁断
- **歧义 32 行 / 23 张 = 走「乙」**（按 `chosenAbility` 家族映射，键 = `CardDef.ActiveAbilityKindFamily`）。依据：原版逐条 ability 各判各结算（`RawCardScript__TriggerAbilities.c:207/231`），甲（OR）在 19/23 张上恒真=空转。代价 = 加 `ca` 列 ⇒ 卡池 md5 变 ⇒ 重钉由主对话做。
- 成就 F2：按原版（模式运行时类型），去掉 E13 加的 `_net != null`。

## 进行中（6 个代理）
核销落盘（AUD4）· X1 卡面溢出+比例 · X2 成就计数器 · X3 引擎判据 · X4 界面四缺陷 · X5 Collection 3 红。

## 待办（Unity，等代理收工后串行跑）
`B14` GrabScreen A/B · 扩 EffectIso 清单重跑 T02/新账-69 · 卡牌 tier3/4 拱顶重渲 + 12 阵营并排 PnP · 复跑受影响宿主 · `A1905` UM/SAU 参数化一局。

## 工作树注意
`CardBase.unity` / `DeckEditor.unity` 被自检宿主重写（已知行为，`!u!115` 全场景 = **0**）；`NotoSerifCJK-Regular SDF.asset` +2865 行（kerning）。

## 2026-10-25 午后批次（滚动追加）

### 用户口径（现行）
1. **先做完 Unity 活 → 再自检 → 自检红了先修 → 再解卡牌审查问题**（我出方案）。
2. **一个子代理负责一条线的多个任务**（⛔ 不要一任务一代理，除非只剩一个）。
3. 真机验收已解禁；批量做。

### 已完成（本批次）
- 16 宿主全量复跑（15 绿；CollectionScene 3 红已诊断+已修、待复跑）
- 真机第一轮（24 图）· player 首次构建 09:15
- `ca` 列打通（卡表 **v29**，md5 重钉 = `ef431e8054822f359162862fdf51dd5b`）
- 台账：核销 48 条（AUD4）· 改判 22 处（AUD5）· **落账 53 条新发现（AUD9）**
- 修完：卡面溢出（X1，根因=自检量字墨/生产量行盒 + 未激活对象早退）· 成就 6 计数源（X2）· 引擎 4 件（X3）· 界面模式卡（X4）· Collection 3 红（X5）· 单位卡 trigger-0 族闸（X6b）· bool 化+Skirmish 预设（X7）· 攻击三件（X8）· 跨侧落点+输入闸+25 条断言（X9）· swarm/ephemeral（X12）· **祈祷"下回合 +1 信仰"全链**（X13）· **A1905 UM vs SAU 真局 19 条**（X14）· **特效关键字同族撞车**（X15）· B14 501 可 A/B（X10）· EffectIso 可筛（X11）
- `EffectIso` 新增 3 效果覆盖：Void Combat **1.222→待复测** · Raging Storm **0.958（门内）** · Necron Variant **1.500（判据不足、未改）**

### 待办（严格按序）
1. Chain B 进行中：EffectIso 复测 → PlayerBuild
2. `B14` A/B（只能 player，⛔ 批处理禁开 `WF_GRAB501=1`）
3. 真机第二轮（选目标态/技能钮/卡池小卡/A1905/UIstate 闸/回放拖拽）
4. **自检全套**（RuleEngineTest 347 条 · BattleScene 含 X8/X9/X14 新断言 · Collection 2360 · CardBaseDemo/CardFaceProbe · MainMenu/Shell）
5. 自检修红 → 修
6. 卡牌线方案（一代理一条线 4 件）
