# B9_W40WIRE2 · 掉舱震屏与轮抽进攻卡分派（`BattleDriver.cs` 一侧接线）

> 执行代理 `B9_W40WIRE2`（2026-10-27）。
> 输入判据 = `资料/完全复刻/报告/B9_W33FX3_震屏与进攻卡.md` §四「转派单」A（行 130）· B（行 208）。
> 本代理白名单 = **只** `Assets/CardPresentation/Battle/BattleDriver.cs`（+ 本报告 + `账/_parts/B9_W40WIRE2.tsv`）。
> ⛔ 引擎侧（`RuleEngine/Core/EffectResolver.cs` · `RuleCore.cs` · `UnitState.cs` …）归 W27，本笔**一行未碰**。

---

## 一、结论

1. **A（掉舱震屏的事件消费端）已接上。** `BattleDriver` 的事件分发现在认 `EvtKind.TraitRemoved`：
   `e.CardId == KeywordTable.DropPod` ⇒ `WarpforgeVFX.WFModuleScreenShake.DoSimpleShake(e.Effect == "droppod-destroyed" ? 3f : 0.5f)`。
   - 🔴 **用 `e.CardId`、不是 `e.Keyword`**（引擎把关键词塞第 4 个位置实参，见 `BattleEvent.cs:388` 那族 doc）。
   - 与别的 feel 同档**只在 `animateFeel` 档跑**（批处理无帧循环、DOTween 不推进，会把相机/HUD 留在推歪的位）。
   - ✅ **现在接上无害**：引擎侧今天**还没发**这两条子标签（`EffectResolver.EmitTraitRemoved` 不传 `effect`），
     所以这条分支今天**走不到**；等 W27 落地 `droppod-open` / `droppod-destroyed` 后整链可达。
2. **B（轮抽 `GameMode.ClosedDeck` 的进攻卡分派）已接上「面板路径」那一半。**
   `OffensiveChoicesOwned` 现在按 `Ctx.PlayMode == GameMode.ClosedDeck` 走
   `[空卡] + OffensiveCards.ChoicesForDraft(faction, offensive: true, victories)`；其余模式**逐字不变**
   （仍 `ChoicesOwnedAt(faction, ForgeData.LevelOf(faction))`）。
   - 🔴 **空卡仍由 `OffensiveCards.Choices` 的下标 0 前插**（`ChoicesForDraft` 按原版不含它 ⇒ ⛔ 别让它自己加）。
   - 🔶 **胜场只接了本机一侧**（对面那一半本地拿不到）⇒ 已按裁定标 `🔶 自己设计（非原版）`，待裁定项见 §三·2。
3. **账：三行由 A 降到 C（并入补丁），另两行只订正文字（状态仍 C）。** 五行的 `状态` 现在都是 **C** ——
   理由：**全链都还差一处不在本笔白名单里的活**（详见 §四），按「A = 已对齐」的口径不能判 A。
   - `战斗逻辑驱动:851`（`CardScript.RemoveDropPod`）—— 消费端已接，**发端点（W27）未落地**。
   - `战斗逻辑驱动:351` / `:352`（`GetEnvEffectCards` / `GetEnviromentalCardList`）—— **面板路径**已接，
     **AI 路径 / 防御半边 / 对面胜场 min** 三处未接。
   - `特效动画:130` / `:208` —— 只订正过期话（「`DoSimpleShake` 全仓 0 调用点」「`ChoicesForDraft` 0 外部调用点」
     **都已过期**），状态仍 C（铁律 5：两份说法不许打架）。
4. **源码改动 = 3 处，其中 2 处是「一行换一行」、1 处是类尾追加 93 行** ⇒
   **本笔对 `BattleDriver.cs` 现有行号零漂移**（最大账落点 35395 < 追加起点 35441）。

---

## 二、证据（文件:行号）

### 2·1 A —— 掉舱震屏消费端

| 项 | 出处 | 读数 |
|---|---|---|
| 事件进不进时间线 | `CardPresentation/Core/EventTiming.cs:501` | `EvtKind.TraitRemoved` 在 `ZeroDurationKinds` ⇒ 时长 0、**照旧入 `_timeline`**（`BattleDriver.PlaySignals` 的 `_timeline.Add`，`BattleDriver.cs:19458`）⇒ 会被 `AdvanceTimeline` 弹出并调 `PlaySignal` |
| 分发点（本笔新增） | `CardPresentation/Battle/BattleDriver.cs:22830` | `if (e.Kind == EvtKind.ShowCardsWindow) OpenShowCardsWindow(e); else if (e.Kind == EvtKind.TraitRemoved) OnTraitRemovedShake(e);`（**一行换一行，零行数变化**；两 kind 互斥 ⇒ `else if` 不改既有行为） |
| 消费本体（本笔新增） | `BattleDriver.cs:35526`（`OnTraitRemovedShake`） | 三道闸：`Kind == TraitRemoved` → `animateFeel` → `e.CardId == KeywordTable.DropPod`；然后 `DoSimpleShake(e.Effect == "droppod-destroyed" ? 3f : 0.5f)` |
| 关键词在 `CardId` | `RuleEngine/Core/BattleEvent.cs:388`（`EvtKind.TraitRemoved` doc）· `EffectResolver.cs:7862` | `ctx.Emit(kind, p, slot, keyword, …)` ⇒ 关键词落在 `CardId`；⛔ 不是 `Keyword` |
| 子标签载体 | `RuleEngine/Core/BattleEvent.cs:597` | `public string Effect;`（本笔按 `"droppod-destroyed"` 判 3.0，其余/缺省 = 0.5） |
| 常量真值 | `报告/B9_W33FX3_震屏与进攻卡.md` §2·1（`read_literal.py` 现读） | `DAT_1834b2bb4` = **4B float 0.5** · `DAT_1834b2e8c` = **4B float 3.0** |
| 请求侧本体 | `Assets/WarpforgeVFX/Runtime/WFModuleScreenShake.cs:209`（`DoSimpleShake`） | preset = `Shake Big`、`overwriteAmplitude = 1`（= 原版那句 `p.amplitude = strength`），`usePresetSignal: false` |
| 下游 | `BattleDriver.cs:3579`（`HookAnimFxShake`）· `:3584`（`mag = amplitude × |direction|`） | 已有、未动 |
| 发端点（**未做**） | `RuleEngine/Core/EffectResolver.cs:7857`（`EmitTraitRemoved`） | 今天**不传 `effect`**（W27 归口：加可选形参 + `RuleCore.OpenDropPod` / 两个破舱点发两条子标签） |

### 2·2 B —— 轮抽分派

| 项 | 出处 | 读数 |
|---|---|---|
| wrapper（本笔改） | `BattleDriver.cs:9337`（签名不动）· `:9339`（`return OffensiveChoicesOwnedCore(faction);`） | 一行换一行；账落点 = 方法级（`BattleDriver.cs:9337（OffensiveChoicesOwned）`）⇒ 不变 |
| 分派本体（本笔新增） | `BattleDriver.cs:35470`（`OffensiveChoicesOwnedCore`） | `Ctx.PlayMode == GameMode.ClosedDeck` ⇒ `[Choices(faction)[0]] + ChoicesForDraft(faction, offensive:true, victories)`；否则原式 |
| 模式号来源 | `RuleEngine/Core/BattleContext.cs:945`（`public GameMode PlayMode`）· 赋值点 `BattleDriver.cs:5480`（`Ctx.PlayMode = playMode ?? …`） | `Begin` 里落的，早于环境卡阶段（`:9237` 那次调用） |
| 规则层（未动） | `CardPresentation/Core/OffensiveCards.cs:182`（`ChoicesForDraft`）· `:74`（`Choices`，空卡在**下标 0**）· `:100`（`ChoicesOwnedAt`） | 规则已对齐（`B9_E2UDW`）；本笔**只接分派**，规则未改 |
| 原版判据 | `decomp_full/BattleManager__GetEnviromentalCardList.c:20-51` · `BattleManager__GetEnvEffectCards.c:105-129` | `mode(+0x18) == 7 ⇒ GetEnvCardListDraft(...)`；形状 = **先 `Add(空卡)` 再 `AddRange(list)`** |
| 胜场来源 | `CardPresentation/Shell/DraftModeWindow.cs:716`（`static DraftRunSave _run`）· `:719`（`public static DraftRunSave RunForTest`）· `:167`（`public int matchesWon`） | 静态 ⇒ 活过「关窗 → 进对局」；**本机一侧**，对面拿不到 |
| 本笔取值口 | `BattleDriver.cs:35496`（`DraftVictoriesLocalForEnvCards`） | `DraftModeWindow.RunForTest?.matchesWon ?? 0`，已标 `🔶 自己设计（非原版）` |
| 面板入口 | `BattleDriver.cs:9237`（`var list = OffensiveChoicesOwned(poolFaction);`）· `:9501`（`OnOffensiveDone` 取同一份） | 两处**同一个函数** ⇒ 摆了哪几张、发出去的就是哪一张（未动） |

### 2·3 行号影响（自证「零漂移」）

| 项 | 读数 |
|---|---|
| 改动前 / 后行数 | 35,441 → 35,534（**+93**，全部在类尾） |
| 本笔改到的已有行 | `9339`、`22830`（**各 1 行换 1 行**，行数不变） |
| 账里引用的 `BattleDriver.cs` 最大行号 | **35,395**（`特效动画.tsv`）；追加起点 = **35,441** ⇒ 无账落点被推到 |
| 行尾 | CRLF 35,534 / LF 35,534 · 裸 CR **0**（改完复读） |

---

## 三、没查清的部分

1. **对面胜场从哪来（待裁定）** —— 原版取**两端** `+0x88` 的 **min**（`BattleManager__GetEnviromentalCardList.c:36-51`，`:47-50` `iVar1 = iVar2`）。
   我们打 bot，`DraftModeWindow` 只有本机 `_run.matchesWon`，`DraftPacks` 里**没有**对手胜场。
   本笔按调度台 2026-10-27 的裁定**暂用本机值**（代码里已标 `🔶 自己设计（非原版）`）。
   **还差的判据**：原版那个「对面 `+0x88`」到底是**同一个 `DraftEventSaveGame` 的两个不同字段**、
   还是**两个玩家各自的存档**（bot 局里对面那份是否存在）—— 本地 `.c` 只看到两次取址后取 min，
   没读到「对面那一份从哪个对象来」。⇒ 需要更深的 VA 级追踪或用户裁定。
2. **防御半边怎么接** —— `ChoicesForDraft(offensive: false, …)` 返回的是 `OffensiveCards.Card`
   （SO 的 `defensiveCards[0..2]`），而我们的防御面板用的是卡池 `CardDef`（`DefensiveChoices`）。
   两边**数据同源**（39 张里 38 张逐张对上）但**类型不同**，要接得先建一张 `idx ↔ CardDef` 的映射表；
   而且防御面板默认关（`defensiveCardPanelEnabled = false`）、后手那张由引擎 `RuleCore.GrantDefenceCard` 发。
   ⇒ 本笔未接，**如实记在账 351/352 的差异列**。
3. **AI 路径没接** —— `PickOffensiveForAi`（`BattleDriver.cs:9583`）仍走未筛的 `OffensiveCards.Choices`。
   原版 `GetAiEnvEffectCard`（`ClickChosenCardDone.c:59/:67`，`justOwnedCards:0`）同样过
   `GetEnviromentalCardList` 的 mode 7 分派 ⇒ **严格说这也是同一条缺口的一半**。
   本笔**未动**（简报只点名 `OffensiveChoicesOwned`；且该路径被 `offensiveCardEnabled = false` 挡着 = 今天不可达）。
   ⚠️ 注意：**别**用 `OffensiveChoicesOwned` 去「顺手统一」AI 那条（那是原版没有的锻造闸，见 `:9556-9559` 的既有告诫）——
   要接只能接 `ChoicesForDraft`。
4. **`DraftModeWindow.RunForTest` 是「测试口」** —— 它自己的 doc（`DraftModeWindow.cs:718`）写着
   「⛔ 别在生产路径拿它算账，统一走 `UpdateState`」。本笔**只能**用它（它是今天唯一公开、且活过场景切换的只读口），
   已在代码注释与报告里标出；建议界面线补一个正式只读口（届时只改 `DraftVictoriesLocalForEnvCards` 一处）。
5. **本笔没有跑 Unity 自检**（代理不许跑批处理）⇒ 两条接线**只有 `typecheck 0/0` + 代码阅读**做保。
   可观测性：① 掉舱今天池内 **0 张**卡带 `droppod`（且引擎不发事件）⇒ **不可观测**；
   ② 轮抽面板被 `offensiveCardEnabled = false` 挡着 ⇒ **今天不可见**。⇒ 没有回归保护，建议轮抽线接线时补断言。

---

## 四、改动清单

| 文件 | 改了什么 |
|---|---|
| `Assets/CardPresentation/Battle/BattleDriver.cs` | ① `:22830` 一行换一行：`… ShowCardsWindow … ; else if (e.Kind == EvtKind.TraitRemoved) OnTraitRemovedShake(e);` ② `:9339` 一行换一行：`return OffensiveChoicesOwnedCore(faction);` ③ 类尾追加 93 行：`OffensiveChoicesOwnedCore`（:35470）· `DraftVictoriesLocalForEnvCards`（:35496，含 `🔶 自己设计`）· `OnTraitRemovedShake`（:35526） |
| `账/_parts/B9_W40WIRE2.tsv` | 5 行（11 列、表头齐全）：`战斗逻辑驱动:351/352/851` **A→C** + `特效动画:130/208` 文字订正（状态仍 C）。`check_patch_conflicts.py` ⇒「没有跨补丁重复键（4 份补丁）」；`merge_ledger_parts.py --dry --only B9_W40WIRE2` ⇒「更新 5 · 新增 0 · 无变化 0」 |
| 本报告 | 新建 |

### 账的判定（为什么五行都留在 C）

| 行 | 状态 | 差什么（写进补丁的差异/备注列） |
|---|---|---|
| `战斗逻辑驱动:851` `CardScript.RemoveDropPod(bool isDestroyed)` | C（原 A，等级由**空**补 **P2**） | 消费端已接（`:35526`）；**发端点未落地**：`EffectResolver.EmitTraitRemoved` 不传 `effect` + `RuleCore.OpenDropPod` / 两个破舱点（`:8603` / `:8657-8662`）还没发两条子标签 ⇒ 全链不通 |
| `战斗逻辑驱动:351` `GetEnvEffectCards(...)` | C（原 A） | 面板路径已接；**AI 路径**·**防御半边**·**对面胜场 min** 未接 |
| `战斗逻辑驱动:352` `GetEnviromentalCardList(...)` | C（原 A） | 同上（同一条缺口，判据只写一份） |
| `特效动画:130` `DoSimpleShake(float strength)` | C（不变） | 就地订正「全仓 0 调用点」已过期（现 1 个）；仍差引擎 emit |
| `特效动画:208` `GetEnvCardListDraft(...)` | C（不变） | 就地订正「0 外部调用点」已过期（现有）；仍差 AI / 防御 / 对面胜场 |

<details>
<summary>本笔为什么没把 851/351/352 改回 A（原文口径 + 我的判法）</summary>

简报写的是「接完线并验证后（A 接上 + B 接上），把这三行改回 A；**若只接上一半，就只把接上的那半改 A，另一半留 C 并写清差什么**」。
- **851 的「另一半」= 引擎发端点**：简报自己写着「引擎侧的活不归你……等 W27 交还后我再派人发」⇒ 本笔只完成消费端。
  按「A = 已对齐」判 A 会让账与事实矛盾（玩家今天**看不到震屏**）。
- **351/352 的「另一半」= AI 路径 + 防御半边 + 对面胜场**：都还没接。
⇒ 五行留 **C**，并把「差哪一处、归谁」写进补丁；W27 / 轮抽线落地后**改 A 只需改这一列**。
</details>

---

## 五、顺手发现

1. 🔴 **`BattleDriver.cs` 有**非本笔引入**的既有行号漂移**：`check_ledger_points.py`（只读）报
   `卡牌面_渲染` / `卡组编辑` / `战斗界面_HUD` 里 **18+ 条** `BattleDriver.cs` 落点漂移
   （例：账里 `28160` → 实际 `28227`，+67；`34529` → `34602`，+73）。
   本笔对现有行号**零影响**（只原地换 2 行 + 类尾追加，追加起点 35441 > 最大落点 35395）。
   ⇒ 这些漂移来自本工作树里**此前未提交的改动**，同步点做 `check_ledger_points --all-A --fix` 时能一起归位
   （⛔ 必须在「没有 `.cs` 写手 + 补丁已合并」时跑）。
2. **`战斗逻辑驱动.tsv:851` 的「我们的落点」列有一处格式残留**：`RuleCore.cs:7870（OpenDropPod） ｜ \` ｜ RuleCore.OpenDropPod ［RuleEngine/Core/RuleCore.cs:7472］`
   —— 中间那一段**多了一个反引号 + 空段**（历史拼接留下的）。本笔没有重排结构（怕与别的补丁撞），
   只在其后追加了新落点；建议下次整表整理时清掉那一段。
3. **`OffensiveCards.ChoicesForDraft` 仍然没有任何自检宿主**（`DumpDraftAvailabilityForTest` 是 `public` 但 0 调用点，
   见 `B9_W33FX3` §五·3）—— 本笔接线后**依旧没有回归保护**。轮抽线接线时建议在 `BattleScene` / `RuleEngineTest` 挂一条
   （那两个文件不在本笔白名单）。
4. **`OffensiveChoicesOwned` 的 draft 分支会让「先手面板」在 `victories < 3` 时只拿到 1 张（空卡）**
   ⇒ 走 `if (list.Count < 2) return false;`（`BattleDriver.cs:9238`）**不弹面板**。
   这**正是原版行为**（`list.Count < 2` ⇒ 直接取 `list[0]` = 空卡），不是缺陷 —— 但它是本轮改动**唯一可观测的行为变化**
   （前提：打开 `offensiveCardEnabled` 且模式是 ClosedDeck）。
5. **`animateFeel` 闸的必要性有实证依据**：`CardFeel.ShakeCamera` 开头就 `SettleShake()`，
   而它自己的注释写着「批处理里更必然 —— DOTween 不跑，震动全靠手动推」⇒ 若不在批处理里闸掉，
   相机/HUD 会被留在推歪的位置。本笔按 `ScheduleUnitShakes` 的同一条口径加了 `if (!animateFeel) return;`。
6. **本笔额外动了 `特效动画:130/208` 两行**（简报只点名 `战斗逻辑驱动` 三行）—— 理由 = 铁律 5：
   那两行里「`DoSimpleShake` 全仓 0 调用点」「`ChoicesForDraft` 0 外部调用点」在本笔之后**已是过期结论**，
   留着就是「两份说法打架」。若调度台认为超范围，合并时**跳过这两行**即可（主表其他字段不受影响）。
