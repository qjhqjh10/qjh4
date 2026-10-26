# B8_BC · `Core/**` 与 `Deck/**` 剩余 B 行 —— 落账报告

> 执行代理 **BC**（代号 `B8_BC`）· 2026-10-26
> 白名单：`CardPresentation/Core/**`（除 `CardView.cs` / `CardHighlight.cs` / `CardFeel.cs` / `TraitFrames.cs`）＋ `CardPresentation/Deck/**`
> 产物 = **`账/_parts/B8_BC.tsv`**（**21 行 × 11 列** · LF · 只放改过的行 · 表头照抄主表第一行）
> ⛔ 未改任何 `.cs` · 未跑 Unity 批处理 · 未动 git · **未改账主表** · 未改正本 · 未动别人的补丁。

---

## ① 结论（数字）

| 项 | 数 |
|---|---|
| 落账总行数 | **21** |
| **改成 `A`** | **5** |
| **刷新落点但保持 `B`** | **1**（`LanguageSelector.OnLanguageChanged`：4 跳对过 3 跳，第 4 跳不等价） |
| **改成 `C`** | **6** |
| **改成 `D`** | **9** |

**闸（全部跑过）**

| 闸 | 结果 |
|---|---|
| `check_ledger_points.py <补丁>` | **A 5 · 通过 5（告警 4 = PROSE）· 漂移 0 · 失败 0** |
| `verify_annot.py` 规则复算（在补丁上） | 同名 1（`Show`）· 异名已注解 4 · **缺注解 0** |
| `check_a_vs_diff.py` 规则复算（在补丁上） | `[非缺口]` 跳过 4 行 · **命中 0** |
| 判据引用 lint（自制，38 条 `文件:行号`） | **38/38 文件存在且行号不越界** |
| `check_patch_conflicts.py` | 无跨补丁重复键 |
| `TMPDIR=/tmp/wf_b8bc bash 工具/typecheck.sh` | **运行时 0 · 编辑器 0** |

**为什么只有 5 条 A（而不是简报的 ≥8）** —— 剩余 B 行的 4 类去向，逐类给数：

| 类 | 条数 | 为什么不能由我升 A |
|---|---|---|
| **① uGUI 基础设施**（`ContentSizeFitterMinMax` 3 · `RectExtensions` 3 · `UIExtension` 5 · `UIPreferedSizeFixer` 2 · `UICulling` 1 · `UIGradientUtils` 2 · `UICornersGradient` 1 = **17 条**） | 17 | 原版是 uGUI 运行时组件；本工程**没有 uGUI 运行时**（排版在 px 设计空间现算）⇒ 没有逐方法入口。等价物只能指某段的**内联算式**，按 `方案 §4.1` 只能 B |
| **② `TextTooltipController` 那 7 条** | 7 | 我们的悬停是「每帧轮询 + `CardView.ContainsNear` 门 + `TmpFont.LinkAt`」，与原件那 7 个 `IPointer*`/`ISelect`/`Toggle` 回调**不是同一形状**；要 A 必须在 `CardView.cs` / `BattleDriver.cs` 挂 per-text 组件 —— 那两个文件**不在我的白名单** |
| **③ `ScreenUtilities` 3 条** | 3 | 函数本体逐句照抄（已核），但原版两个消费点在 `Hand/**`（`PlayerHand.cs:163/187` 的手牌间距插值）与 `VarsDevice`；**helper 已落、消费点未接** ⇒ 不是 A |
| **④ 引擎/界面侧（落点在 `RuleEngine/**`、`Battle/**`、`CardView.cs` 等）** | 其余 | 那一片各有专人（我给不出「我方落点是声明行」的双侧方法级） |

⇒ 除非调度台把「①②③ 这三类怎么定」拍下来（见 ⑦·2），这 27 条在任何执行代理手里都到不了 A。

---

## ② A 档 5 条 —— 逐条证据

| # | 原版方法 | 判据出处（哪份反编译） | 我们的落点（行号范围） | 观测量 + 两侧数值 | 差异 |
|---|---|---|---|---|---|
| 1 | `DraggableController<T>.<OnEndDrag>b__6_2` | `decomp_generic_trial/out/DraggableController_object____OnEndDrag_b__6_2__18155e0.c:1-57`（🥇 VA 级；泛型类只有这份有体） | `Core/DraggableController.cs:202-222`（`OnEndDrag` 的 for 循环） | `DropCalls`（`:144` 声明 / `:219` 递增）：拖到 1 个 `IDropHandler<T>` 的格子 ⇒ **1**；拖到空处 ⇒ **0**。原版 = ForEach 展开后的件数，同值 | 逐句 3 句无差异；形状差 = 回调 vs for 循环（同序同语义） |
| 2 | `TextLinkProcessing.TryProcessText` | `out_il2csharp/Assembly-CSharp/TextLinkProcessing.cs:12-32`（🥈 体完整，RVA 0x8527C0） | `Core/TmpFont.cs:501-521`（`LinkAt`） | 指针压在 `<link=armour>` 上取到的 link id：原版 `GetLinkID()` = **`armour`**、我们 `LinkAt(...)` = **`armour`** | 原版命中后自行分派（数字档→空方法、URL 档→`OpenURL`），我们只返回 id 交调用方；那两档在我们的文本里无实例 `[非缺口]` |
| 3 | `EnviromentalEffectCardsSO.GetEnviromentalEffect` | `decomp_full/EnviromentalEffectCardsSO__GetEnviromentalEffect.c:1-60`（🥇）· `out_il2csharp/…/EnviromentalEffectCardsSO.cs:14-61`（🥈） | `Core/OffensiveCards.cs:55-63`（`For`）+ `Battle/BattleDriver.cs:9265-9269`（`EnvSOFor`） | 出一张进攻卡后生效的环境 SO 名（AstraMilitarum 槽 1）：原版 `offensiveCards[1].EnviromentalEffectVFX` = **`EnvironmentalCondition Astra Militarum Planetary Invasion`**；我们 `cards[idx=1].envSO` = **同一串**；空卡档两侧都 = `Astra Militarum Default Enviromental Condition` | 原版按卡引用（`uniqueId`）反查 SO；我们没有卡 SO，改按槽号取同一张 SO 摊平后的那一格 `[非缺口]` |
| 4 | `CardScript.ActivateTraitParticlesFromCode` | `decomp_full/CardScript__ActivateTraitParticlesFromCode.c:1-85`（🥇，硬编码 `TriggerType==5`；`.c:62` 传 `cardArmy`） | `Core/TraitParticles.cs:168-183`（`Play`）+ `:68-121`（`FromCode`） | 某 trait 触发时播的粒子 prefab：`stomp` ⇒ 原版 `Stomp.traitEffects[5].effectAnim = BluntImpact_Stomp_Forward` → `Attack_Stomp`；我们 `PrefabFor("stomp",100)` = **`Attack_Stomp`**。另 4 例同值：`ferocity→Ferocity_RecallToDeck` · `huntMark→HuntMark_DamageEffect` · `shuriken→ShurikenEffect` · `teleport→Trait Summon` | 原版走 `ResolveTraitAnim` 协程（只影响时长），我们同步 `Play`；两个原版方法都不带 callback `[非缺口]` |
| 5 | `EverguildTooltipItem.Show` | `decomp_full/EverguildTooltipItem__Show.c:1-111`（🥇：pivot 双浮点包值逐个解开）· `out_il2csharp/…/EverguildTooltipItem.cs:40-122`（🥈） | `Core/Tooltip.cs:307-355`（`Show`）· `:531-548`（`Pivot`）· `:571-654`（`Layout` ⇔ `ForceRebuildLayoutImmediate`） | 面板正文 + pivot：`anchor=30(BottomLeft)` ⇒ **(0,0)**、`anchor=55` ⇒ **(1,0)**（两侧同表）；正文 = `data.Text` ⇔ 我们 `ShownBody`（`:312`） | 原版多两层 prefab 件（根 `CanvasGroup` + `defaultOffset`，淡入在 `ShowAnimation` 里 0.3 s）；我们折进调用点 `offset` 与逐件 tint 淡入（`FadeTime=0.3f`，`:120`）`[非缺口]` |

---

## ③ D 档 9 条 —— 逐条 D 依据

| # | 原版方法 | 判据出处 | D 依据 |
|---|---|---|---|
| 1 | `TextLinkProcessing.ProcessText` | `out_il2csharp/…/TextLinkProcessing.cs:34-36`（**RVA 0x4B33B0**） | 原版体 = **空**（RVA 0x4B33B0 = 全仓共享空方法桩 `ret 0`，`dump.cs` 1,815 处引用）⇒ 双源 |
| 2 | `TextLinkProcessing.ProcessURL` | `out_il2csharp/…/TextLinkProcessing.cs:38-42` | **0 调用点**（两源全树）；同一行为内联在 `TryProcessText` 的 URL 档 |
| 3 | `EverguildTooltipItem.Awake` | `out_il2csharp/…/EverguildTooltipItem.cs:36-39`（**RVA 0x4B33B0**） | 原版体 = **空桩**（双源）；本类是「字段 Inspector 拖、`Show` 时直接读」，没有 Awake 缓存那一跳 |
| 4 | `DragAndDropItem.GetScreenPositionWithZValue` | `out_il2csharp/…/DragAndDropItem.cs:278-287`（RVA 0x806A10）· `decomp_full/DragAndDropItem__GetScreenPositionWithZValue.c:1-12` | **0 调用点**（两源全树，连本类内部也没调） |
| 5 | `LocalizedMessage.GetLocCodes` | `out_il2csharp/…/Everguild/LiveOps/LocalizedMessage.cs:54-59` | **0 调用点**（两源）+ 挂在远端消息 DTO 上（P3 数据面）；「取语言码集合」我们已有 `Loc.Lang13Codes`（`Core/Loc.cs:2971`） |
| 6 | `GameStaticData.GetTagTerm` | `out_il2csharp/…/GameStaticData.cs:425-428` | **0 调用点**（两源）+ 官方 13 语表里 `TagName/` 前缀 **0 条**（`grep -c TagName` on `official_loc_13lang.json`）⇒ 键族原版自己也没填过 |
| 7 | `GameStaticData.GetCardArmyTerm` | `out_il2csharp/…/GameStaticData.cs:451-454` | **0 调用点**（两源）；取词的活的是兄弟 `CardArmyToString`（本账 `其他_基础设施.tsv:229` 已 A；我们链 = `Core/CardText.cs:827-831` `Faction`） |
| 8 | `GameStaticData.GetCardRarityTerm` | `out_il2csharp/…/GameStaticData.cs:445-448` | **0 调用点**（两源）；同一映射活在 `CardRarityToString`（`:232` 已 A）；我们表 = `Core/FilterPanelModel.cs:389-393` |
| 9 | `ArmyUtilities.GetArmyByCode` | `out_il2csharp/…/ArmyUtilities.cs:86-144` | **0 调用点**（两源）+ **13 个 3 字码在整个 il2csharp 里只出现在本方法**（另一处是 `stringliteral.json`）⇒ 整张码表是死的 |

> ⚠️ 覆盖面（写进补丁备注）：`il2csharp` 的**泛型类**方法体是 `NotImplementedException`（体在 `decomp_generic_trial`）⇒ 「0 调用点」这一扫**不覆盖泛型类方法内部的调用点**；上表 9 条都是非泛型方法。

---

## ④ C 档 6 条 —— 缺口与补法

| # | 原版方法 | 缺口 | 补法（判据已有） |
|---|---|---|---|
| 1 | `ArmyUtilities.GetArmyMediumBackGround` | 13 条「阵营 → 中号底图」表未解 + 那 13 张图未导入 | SO = `sharedassets0.assets` 对象 **417**，可复用 `Core/ArmyIcons.cs` 文件头那套「原始字节手工切」；原版消费点 = `Shell/PurchasePremiumWindow.cs:185` / `Editor/ShellScene.cs:7032` |
| 2 | `ArmyUtilities.GetForgeIconForArmy` | 13 条 `forgeIcons` 表未解 + `40K_general_icon_Forge points_*` 图未导入 | 对象 **421**（`IconsByArmySO` 同族）；⚠️ 与 `ArmyIcons.IconName`（+0x28）是两张表，别复用 |
| 3 | `MenuSprites.GetFriendlyWorldSprite` | `MenuSprites` 的世界图一族（`worlds[]` / `friendlyWorld` / 两个动画字段）一条都没建 | 先解 SO 再定接法；我们主菜单背景目前是纯代码渐变（`CardArt.Gradient`） |
| 4 | `MenuSprites.GetWorldSprite` | 同上（`worlds` 数组 0 条） | 照抄原版兜底：越界 **出声 + 回最后一张**，别改成 clamp 静默 |
| 5 | `GameStaticData.GetTraitDesc` | **`isAlt` 那一支**：正文表只有「非 Alt」一列；回落链我们只有后两跳 | 官方表里补 `CardTraitDescription/<enum>Alt`；判据侧调用点 5 处（`TraitDescItem` / `TraitDescriptionUI` / `TextTooltipController` 都传 `isAltTraitConfig`） |
| 6 | `TutorialTip.GetLocalizedText` | 整条教程链未建（教程线 = P1）；词条那一半已有（`Loc.cs:1397-1419` 的 `Tips/*` 13 条） | 先建 `TutorialTip` 数据类（字段照 `out_il2csharp/TutorialTip.cs:8-16`）→ 再谈 `ShowTutorialTip`（5 个调用点在 `AiScripted`，属战斗线） |

---

## ⑤ 改了哪些文件、哪些行（新增行数）

| 文件 | 改动 |
|---|---|
| `d:/4/Unity/资料/完全复刻/账/_parts/B8_BC.tsv` | **新增**（表头 + **21 数据行**，LF，20,796 字节） |
| `.cs` / 主表 / 正本 / 别人的补丁 | **零改动** |
| `d:/4/_tmp_view/b8bc_patch_gen.py` · `b8bc_validate.py` · `b8bc_lint_refs.py` | 一次性脚本（生成补丁 / 复算两个闸 / 判据引用 lint）—— 可删；`b8bc_lint_refs.py` 建议留作「账引用体检」的雏形 |

> 为什么没有 `.cs` 改动：5 条 A 都是**已经实现且已接线的**，本批的活是「按方法级把证据钉死」；6 条 C 都不是「一小时内的顺手改」（要解 SO / 导图 / 建数据类），9 条 D 本来就不该实现 —— 符合铁律 14「能迅速解决就顺手做」的三条判据（都不满足）。

---

## ⑥ 断言草稿（给调度台在同步点落）

1. **`b__6_2`**：`DraggableController<int>` 用 1 个 `IDropHandler<int>` 的桩做拖拽 ⇒ `DropCalls == 1`；`hovered = null/空` ⇒ `DropCalls == 0`（零泄漏：两次调用之间不累加）。
2. **`TryProcessText`**：卡面关键词层「从左缘起 4 px 步长扫」（先例 `Battle/BattleLogPanel.cs:725`）⇒ `TmpFont.LinkAt(...) == "armour"`；并把 `TmpFont.LinkAtCalls` 在「指针在空处」时断言**不增长**。
3. **`GetEnviromentalEffect`**：`OffensiveCards.For("AstraMilitarum").cards[1].envSO == "EnvironmentalCondition Astra Militarum Planetary Invasion"` 且 `EnvSOFor(cards[1], faction)` 同串；`EnvSOFor(empty, faction) == a.defaultEnvSO`。
4. **`ActivateTraitParticlesFromCode`**：`TraitParticles.Traits` 每一条的 `PrefabFor(t, -1)` 非空且 ∈ 效果库；正向 `PrefabFor("stomp",100) == "Attack_Stomp"`；**反向** `PrefabFor("ambush", …) == null`（原版这条链对 ambush 本来就不播）。
5. **`Show`**：`Tooltip.Show("X", pos, 30)` ⇒ `Tooltip.ShownBody == "X"` 且 `Tooltip.PanelCenter` 落在 `pos` 的**右上**（pivot (0,0)）；`anchor=55` ⇒ **左上**（pivot (1,0)）。判别式：两次 `PanelCenter` 相对 `pos` 的 x 符号必须相反。
6. **换语言广播（缺口钉子，今天应红）**：`BattleHudLanguage.Notify()` 的返回订户数 `>= 4`（原版 4 个实现者：卡面 ×2 / 回合钮 / 战斗日志）；现在是 0 ⇒ 这条断言正是把 `本地化.tsv:7` 的缺口钉住的那一颗。

---

## ⑦ 没查清的部分（⛔ 都写在这里，没有拿猜测填空）

1. **`EverguildTextMeshPro.CheckFOntInPreset`**：我们**没有 `fontPreset` 概念**（按用途选面 = `TmpFont.Face`）。原版那个 `if (fontPreset.font != m_fontAsset) font = fontPreset.font` 在**资产上到底有没有被启用**（`fontPreset` 非空的 prefab 有几个）没查 ⇒ 保持 B。
2. **`GameStaticData.IsVulnerableDamage`**：是**活谓词**（6 个调用点：`AI.cs:1519/1556` · `CardScript.cs:16372/16424/16425`）。我们这边**没有 `DamageType` 类型**，同一信息由 `BattleEvent.IsVulnerableSecondHit`（bool）承载 ⇒ 无同名谓词、也不是同形载体。`AI.cs` 那两处（用它算预期伤害）我们对应到哪一步**没逐条对**（在 `RuleEngine/Data/SimpleAI.cs`，不在我白名单）。
3. **`GameStaticData.GetLanguageCode`**：原版**唯一调用点 = `TestLoginManager.Start`**（`decomp_full/TestLoginManager__Start.c:40`）—— 是个**测试宿主**。那个 MonoBehaviour 有没有被挂进正式场景**没查**（会影响它该判 D 还是 B）。我们侧：`Lang13Codes`（13 列序）已有，但没有 ISO 码消费者 ⇒ 保持 B。
4. **`EverguildLocalization.GetTranslationWithParameters`**：原版体 = `string.Format(GetTranslation(key), param1)` —— **只有 `param1` 被用，`p2/p3` 被忽略**（原版自己的缺陷）。我们是调用点内联 `string.Format(Loc.T(...), x)`，**没有共享助手** ⇒ 无方法级落点，保持 B。
5. **`CollectionTab<T>.GetCollection()`**：原版是 `protected abstract` 声明（子类各自返回自己的 list）。我们侧只能指到 `Deck/DeckEditorState.cs` 的 `Deck` / `Cards` **成员**（非方法声明行）⇒ 按口径不能 A。
6. **7 条 `TextTooltipController.*`**：见 ① 类②。另：原版 `OnPointerDown` / `OnDestroy` 的 RVA 也是 **0x4B33B0（空桩）** —— 如果账里有那两条，可以直接判 D（本批账里没有这两条）。
7. **uGUI 基础设施 17 条**：见 ① 类①。

---

## ⑧ 顺手发现（都给证据）

1. 🔴 **账里一条判据出处的行号是错的（已在本行订正）**：`EnviromentalEffectCardsSO.GetEnviromentalEffect` 原写 `cpp2il/EnviromentalEffectCardsSO.cs:63-`，而 **63 行是 `GetEmptyOffensiveCard`**，本方法在 **14-61**。
2. 🔴 **`il2csharp` 的「返回 Vector2/Vector3 的方法」其 float 返回形态不可信**：`GetPivotPosition` / `Show` 在 il2csharp 里写成 `real1 = 0f; return real1;`（看着像 y 分量被塌成 0），而 Ghidra 的 `local_38 = 0x3f00000000000000` 才是真值 **(0, 0.5)**。⇒ 这类方法**必须看 Ghidra 的包值**（这条坑值得并进 `资料/全量反编译_入口与用法.md` 的读法一节）。
3. `EverguildTooltipItem.Awake` 原账写「落点无法精确定位」，**真因是原版体为空桩**；同族 `TextLinkProcessing.ProcessText` 也是空桩 —— 说明「找不到落点」这个症状里混着「原版本来就没行为」这一类。
4. `Tooltip.Pivot` 与 `GetPivotPosition` 完全同表（含 default 出声回落）；`Show`（Ghidra `:36-72`）里那段是同一张表的**内联复制**，两处同值 ⇒ 这两处将来只能有一份表。
5. `SettingsPanel.ChooseLanguage`（`Battle/SettingsPanel.cs:1889-1912`）已经是 `LanguageSelector.OnLanguageChanged` 的**4 跳等价物**，**唯一不等价 = 广播订户 0**（该处注释自己写着「卡面重排字 / 日志重拼行还没订」）⇒ 换语言后**已画出来的**卡面/日志仍是旧语言。已写进 `本地化.tsv:7` 的备注。
6. `ArmyUtilities.GetArmyByCode` 的 13 个 3 字码在整个 il2csharp 里**只出现在本方法**（+ `stringliteral.json`）⇒ 码表本身是死的（这条让本行从「C 待做」改判 **D**）。
7. `TraitParticles` 的 `triggerType==5` 数据侧 **10 条**与 `数据/游戏数据/trait_collection.json` **逐条对齐**（`stomp` / `ferocity` / `huntMark` / `shuriken` / `teleport` / `swarm` / `sniper` / `synapse` / `longrange` / `markerlight`）。
8. ✅ **`CardDef.cs` 那条 `CS0708` 已经好了**：`TMPDIR=/tmp/wf_b8bc bash 工具/typecheck.sh` = 运行时 **0** / 编辑器 **0**。

---

## ⑨ 给调度台的请求

1. **`TextTooltipController` 那 7 条**（`卡牌面_渲染.tsv:124/126/127/129/130/131/134`）：要升 A，必须有人**在 `Core/CardView.cs`（关键词层）与 `Battle/BattleDriver.cs`（HUD 数值层）挂 per-text 的 `TextTooltipController` 等价组件**并接 `Toggle(bool)`。这两个文件不在我白名单 ⇒ 请派给它俩的写手；我这边可以把「原版 7 个回调 + `isHovering` + `registerEvents` 的逐句语义」整段交出去（本报告 §② 与判据出处已备好）。
2. **请给「架构性无对应物」一个口径**（共 **20 条**：uGUI 基础设施 17 + `ScreenUtilities` 3）。它们要么永远 B，要么单独一档（例如 `B-arch`），⛔ 不该让下一批代理再逐条重查一遍。
3. **`LoginManager.SetInitialLanguage` / `IsActivatedLanguage` 是跨表重复键**（`其他_基础设施.tsv:316/328` ＋ `泛型与启动链_补读.tsv:809/810`）：`merge_ledger_parts.py` 会因「命中 >1 处」**拒绝写盘**。需要先按 `账/_重复键_待消歧.md` 消歧，我没动这两条。
4. **简报里的落点分布和实际不符**：`ItemDrawer.cs 3 条` / `CollectionWindow.cs 2 条` / `PracticeModePopup.cs 2 条` / `DailyData.cs 1 条` —— 这 8 条的落点**全在 `Shell/**`**（不在我的白名单），我一条没动。如果它们该归我，请把 `Shell/**` 的对应文件划进来（或改派 Shell 线写手）。
5. **`CardRaceToString` / `MinionRaceToString` 的真实落点是 `Core/CardView.cs` 的 `RaceTerm`**（`CardView.cs:5333-5422`）—— 账里现在写的是 `Core/CardIcons.cs`（**错的**，`CardIcons` 是图标 markup）。CardView 不在我白名单且可能正被改 ⇒ 这两条（`其他_基础设施.tsv:231/259`）我没动，请转给 CardView 的写手。
