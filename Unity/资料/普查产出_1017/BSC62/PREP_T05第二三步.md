# PREP · T05 第 2/3 步施工面

> 只读摸底代理 · 2026-10-17 · ⛔ 未跑 Unity / 未跑 typecheck / 未动 git / 未改任何正本或 `.cs`。
> 判据权威序：反编译 `d:/2/tools/decomp_full/` ＞ 解包字段 `d:/2/新解包资源/assets_full/` ＞ 成品卡图。
> 本仓文件路径一律以 `d:/4/Unity/MyGame/Assets/CardPresentation/` 为根（下称 `<CP>/`）。

---

## 一、`GetPos` 36 档：是什么 · 现状几档 · 缺哪几档

**是什么**：`PositionOption`（枚举全文 = `<CP>/Battle/PositionOption.cs`，**36 个具名值**、出处 `dump.cs:25161`）。
技能（ability）路径的解析器 = **`AbilityLogic.GetPos`**（`d:/2/tools/decomp_full/AbilityLogic__GetPos.c`，331 行，**36 档全实现**）；
`BattleManager.GetAnimTransform`（144 行）**只是它的兜底**（T05b §一 问3）。

**现状 `AnimAnchors.cs` 实现了 11 个具名档**（`Resolve` = `<CP>/Battle/AnimAnchors.cs:124-192`，文档口称「10 档」因 1/2、3/4 两两同支）：
`self(0)` `cardTarget(1)` `abilityTarget(2)` `friendlyHero(3)` `enemyHero(4)` `boardCenter(5)`
`nextFriendlyMinionSpot(14)` `friendlyHand(18)` `enemyHand(19)` `friendlyDeck(50)` `enemyDeck(55)`。

**缺的 25 档（名单，值 → 名 → GetPos 的正确落点）**（逐档出处 = T05b §一 问3 那张 `GetPos` 表）：

| 缺档 | 名 | GetPos 正确落点 |
|---|---|---|
| 6 | boardCenterUI | `BattleHud.GetUIAnchor(6,…)` → `Anchor: UI Center` |
| 7 | cemetery | `GetUIAnchor(7)` → `CemeteryLogPanel` |
| 8 | friendlyPower | 数组里没 8 ⇒ **LogError + BattleHud 根 transform**（T05d §1·4，**原版就没锚点**） |
| 9 | enemyPower | `GetHero(isPlayer)` → `GetEffectAnchor()` |
| 10 | nextToCemetery | 数组里没 10 ⇒ **LogError + 根 transform**（同 8） |
| 11 | nextToClock | `GetUIAnchor(11)` → `Anchor: Next To Clock` |
| 12 | friendlyMana | `GetUIAnchor(12,isPlayer)` → `PlayerMana` / `EnemyMana` |
| 13 | enemyMana | `GetUIAnchor(13,isPlayer)` → `EnemyMana` / `PlayerMana`（12 的镜像） |
| 15 | nextEnemyMinionSpot | `CreateTempAbilityTransform(isPlayer, getEnemyPos=1, slot=1)` |
| 16 | secondFriendlyMinionSpot | `…(isPlayer, 0, slot=2)` |
| 17 | thirdFriendlyMinionSpot | `…(isPlayer, 0, slot=3)` |
| 60 | friendlyPsychicMana | `PlayerManager.GetPsychicManaTransform` |
| 61 | friendlySpiritStoneMana | `Get{SpiritStone,Psychic}ManaTransform`（按 isPlayer 取反） |
| 70 | enemyPsychicMana | 同 60/61 族 |
| 71 | enemySpiritStoneMana | 同 60/61 族 |
| 80 | nextToFriendlyPsychicMana | `GetNextToPsychicManaTransform` |
| 90 | nextToEnemyPsychicMana | `GetNextTo…ManaTransform` |
| 100 | friendlyPlasmaMana | `GetPlasmaManaTransform` |
| 110 | enemyPlasmaMana | `GetPlasmaManaTransform` |
| 120 | nextToFriendlyPlasmaMana | `GetNextToPlasmaManaTransform` |
| 130 | nextToEnemyPlasmaMana | `GetNextToPlasmaManaTransform` |
| 140 | createCardPos | `GetUIAnchor(140)` → `Anchor: CreateCard` |
| 145 | stratagemGeneralShowCase | `GetUIAnchor(145)` → `Anchor: Stratagem ShowCase` |
| 150 | friendlyHandWorld | `BattleManager.get_FriendlyHandAnchor`（T05b 表末行） |
| 155 | enemyHandWorld | `get_EnemyHandAnchor` |

**兜底那条路现在怎么走**（`AnimAnchors.cs:183-191`）：以上任一档落 `switch` 默认 ⇒
`Fallbacks++` + `Debug.LogError`（「不在原版那 10 档里」）+ **退回本地玩家**（`ctx.mySeat`）督军的 `EffectAnchor`。
⚠️ 这条兜底**只对 `GetAnimTransform` 成立** —— **技能路径原版不落这里**（T05b §一 问3）。

⚠️ **`AnimAnchors.Resolve(...)` 目前【零调用点】**（全仓 grep `AnimAnchors` 只有 `BattleDriver.cs` 的 5 处**赋值钩子** + `HookAnimAnchors()` 定义，`Ctx` 结构体**从没被构造过**）⇒
**第 2 步单做 = 没有可见效果**，必须被第 3/4 步消费。

---

## 二、第 2 步施工形态（改哪个函数 · 锚点真值从哪来 · 判据出处）

**动 3 个文件：**

1. **`<CP>/Battle/AnimAnchors.cs` —— `Resolve(PositionOption, Ctx)`（124-192）**
   - 给 25 个缺档加 `case`（照 §一 表的落点）；
   - `0/1/2` 改 **state-aware**：现在恒取 `card.EffectAnchor(true)`（196-201 的 `CardAnchorBoard`）⇒
     应改成按**卡的状态**二选一（`EffectAnchor(onBoard)` 已支持，见 `<CP>/Core/CardView.cs:840`；
     状态判据 = `CardScript.GetEffectAnchor` 的 `+0x228` 两分支，T05d §附(a)）。
   - `14` 改 **行动方**那一侧（现在 `NextMinionSpotOf(ctx.mySeat)`，`AnimAnchors.cs:160-166` ⇒ 改传 `ctx.actorSeat`）。
   - 加 UI 锚点解析口：`6/7/11/140/145` 用同一张 `GetUIAnchor` 数据表；`12/13` 按**行动卡 `isPlayer`** 二选一。
   - ⚠️ **换算留给消费方**（锚点不在同一空间 —— 文件头 44-47 行已经这么定，⛔ 别在 `AnimAnchors` 里偷偷换算）。

2. **`<CP>/Battle/BattleDriver.cs` —— `HookAnimAnchors()`（搜 `void HookAnimAnchors()`）**
   - 现有 5 个钩子（`HeroAnchorOf`/`HandAnchorOf`/`DeckAnchorOf`/`BoardCenterOf`/`NextMinionSpotOf`，`2237-2270`）
     **要加**：`9 enemyPower` 的 getter、`6/7/11/140/145` 的 UI 锚点 getter、`12/13` 的 Mana getter、
     `15/16/17` 的空位 getter、`150/155` 的手牌世界锚 getter、`60-130` 的三族 Mana 锚 getter（若做）。
   - **`Ctx` 的构造点**（现在没有）：出牌/技能那一刻按 `actorSeat`/`mySeat` 建 `AnimAnchors.Ctx` 再调 `Resolve`。

3. **`<CP>/Battle/HudCanvas.cs` —— 建 4 颗子锚点**（T05 §四 末段点名此文件）
   - 新建一颗 `Anim Anchors`（铺满父 canvas、aMin(0,0)/aMax(1,1)、sd=0）+ 4 子锚点，`anchoredPosition`：
     `Anchor: UI Center (0,0)` · `Anchor: CreateCard (885.56,84.27)` · `Anchor: Stratagem ShowCase (529,83)` ·
     `Anchor: Next To Clock (766,83)`（均 `sizeDelta=100×100`）—— **逐值在 T05d §二**，⛔ 别自己估。
   - 档 `7/12/13` **挂既有 HUD 件**（不新建）：`CemeteryLogPanel`（`BattleDriver.cs:871,4565` 区）、
     `PlayerMana`/`EnemyMana`（`BattleDriver.cs:1477-1479`，`97.7×97.7` 两颗）。
   - ⚠️ HudCanvas 三条硬约束（文件头 24-40）：**子 canvas 一律不加 `CanvasScaler`** · 根 canvas 建在**根对象**上。

**锚点名/位置的真值从哪来**：
- **原版字段 = `BattleHud.uiAnimAnchors`**（数组，`BattleHud +0xB0`，元素 `UIAnimAnchors{positionOption,anchor,useIsPlayer,opponentAnchor}`，
  出处 `dump.cs:27228/27168-27180`；算法 `d:/2/tools/decomp_full/BattleHud__GetUIAnchor.c:28-51`）。
- **值 = 解包场景 `bundle_scenes_scenes_battlearena2/MonoBehaviour/MonoBehaviour_5150.json`**（**7 条**，
  arena1/3… 逐场一致；⚠️ **战斗侧一律用 battlearena2**）。节点 PathID/rect 全表 → **T05d §1·2 与 §二**。
- **`GetPos` 的行为**（每档解析成什么）= `d:/2/tools/decomp_full/AbilityLogic__GetPos.c`（331 行，T05b §一 问3 已逐档整理）。

**判据出处（第 2 步）**：**T05b §一 问3 逐档表** ＋ **T05d §一 的 7 档 UI 映射**（**主对话已独立复核**）。

---

## 三、第 3 步施工形态（在哪层展开 · 表结构 + 真样本 · 吃哪些参数）

**在哪层展开 —— 驱动侧，`<CP>/Battle/BattleDriver.cs`**（口径 = 引擎不动、由驱动侧按表展开，T05 §四①）：
- 排时间线的地方 = **`PlaySignals()`**（搜 `void PlaySignals()`）：它把这一批事件按 `EventTiming`
  排进 `_timeline`（`List<PendingSignal>`，每项带 `at` 时刻），再由 **`AdvanceTimeline(dt)`**（搜 `public void AdvanceTimeline`）
  逐帧弹出、调 **`PlaySignal(e,…)`**（搜 `void PlaySignal(BattleEvent`）。
- **展开点 = `PlaySignals` 的 `_signalBuf` 循环**：遇到 `e.Kind == EvtKind.Ability` 时，去查 `card_anim_order.json`
  的该卡 `steps[]`，**把这一条事件换成 N 条 `PendingSignal`**（各带自己的 `at`：按每拍时长累加）。
- **播放点 = `PlaySignal`**：现在 `case EvtKind.Ability: evt = VfxMap.Ability;`（搜 `VfxMap.Ability;`）——
  它一次只解**一个** prefab（走 `CardEffects.FireEvent`）。要按拍播，需要**给 `PendingSignal` 加一个「本拍 prefab 名」字段**
  （或新开一个 `PlayAbilityBeat` 入口），⛔ **别在 `VfxMap` 里再写第二份逐卡表**（铁律 6）。
- ⚠️ **`PendingSignal`（搜 `struct PendingSignal`）现在只有 `{evt,at,drawFromEnd,points,pointIndex}`** —— 加字段是这条活的核心。

**`card_anim_order.json` 结构**（真身在 `d:/4/Unity/数据/游戏数据/card_anim_order.json`，3.5 MB；
**运行时需瘦身拷进 `<CP>/Resources/`** —— 同 `ability_anim_durations.json` / `card_vfx_by_card.json` 的口径：
- 顶层：`{_schema, _generatedBy, _what, _sources, _conventions, _coverage, cardAnims[536], cards[897]}`。
- `cards[]`：`{id, name, armyId, armyKey, abilities[]{ path, abilityIndex, logicIndex, trigger, triggerName, chosenAbility, chosenAbilityName, steps[]{ i, ca, why } }}`。
- `cardAnims[]`：`{guid, name, srcBundle, srcMono, resolvedBy, inCardAnimMapKeys[], vfx, vfxGuid, vfxType, params{…10}, animInfo{…~20}}`。
  `ca`（step）↔ `cardAnims[].guid` 是同一把键；`steps[i].i` 升序 = 原版 `cardAnimations` 的 List 下标序。

**一条真样本**（`cards[0]`）：`EC29 Heldrake`（`armyKey EmperorsChildren`）→ `abilities[0]` =
`{path:"cardAbilities", abilityIndex:0, logicIndex:0, trigger:0, triggerName:"ThisCardPlayed", chosenAbility:10, chosenAbilityName:"doDamage", steps:[{i:0, ca:"bc43540f679144d81b72a2c599c5cb63", why:null}]}`。
对应 `cardAnims` 项 `params` = `{startPosOption:2, endPosOption:2, pointTowardsOption:0, vfxDelayTime:0, startDelay:0, timeAtStartPos:0, timeMoving:1, timeAtEndPos:0, shouldMoveVFX:0, waitAnimToApplyEffect:1}`。

**展开时吃的参数**（每条 step 的 `cardAnims[]` 项）：
- **播哪件** = `cardAnims[].vfx`（= `AnimInfo.animAdressable(0x10)` → prefab 名；`vfx=null` 且 `vfxGuid` 非空 = 该 guid 不在本地索引）；
- **起/终/朝向锚点** = `startPosOption / endPosOption / pointTowardsOption` → 交给第 2 步的 `GetPos` 解析器；
- **时长/节奏** = `timeMoving / vfxDelayTime / startDelay / timeAtStartPos / timeAtEndPos`；
- 🔴 **是否等它播完 = 该拍自己的 `waitAnimToApplyEffect(0x58)`**（多目标分支里再 `&& 最后一个目标`）——
  **⛔ 不是「只有最后一拍才 wait」**（见 §七 风险条）。
- **纯延时拍**（`Wait_300ms/1s/1.5s/2s` 4 个）：`vfx=null`，时长在 `timeMoving`、`waitAnimToApplyEffect=1` ⇒ **照算时长、别当解不出丢掉**。

**覆盖率（现读 `_coverage`）**：`cardAnimationsRefs 1468`（`cardAbilities` 1284 + `activeAbility` 184）·
`abilityRowsWithMoreThanOneStep 318` · `stepsHistogram {1:794,2:281,3:36,4:1}` ·
`paramsViaCardAnimMap_AFTERrename_cardAbilitiesOnly {hit:672, namedButNotInFactionMap:599}`（≈52%）。
⚠️ **比 T05 §四 记的「596/1284」更高**（那张有序表已带上 `Genestealers→Tyranids` 换名，`ifGenestealersToTyranids=710`）。

**判据出处（第 3 步）**：**T05b §一 问1/问2** ＋ **`card_anim_order.json` 的 `_conventions`**（含 `waitForAnim`/`runtimeSkip`/`targetMultiplex` 三条语义）。

---

## 四、文件所有权表

| 文件 | 第 2 步 | 第 3 步 | 交叠 |
|---|---|---|---|
| `<CP>/Battle/AnimAnchors.cs` | **主改**：`Resolve` 加 25 档 + 加 UI/空位锚解析口 | 只读（第 3 步只调它的 `Resolve`） | ⚠️ 2/3 步都要碰「调 Resolve」的**调用点**（在 BattleDriver） |
| `<CP>/Battle/BattleDriver.cs` | 中改：`HookAnimAnchors()` 加钩子 + 建 `Ctx` | **主改**：`PlaySignals` 展开 + `PlaySignal`/`PendingSignal` 加字段 | 🔴 **同一文件，两块必须串行**（见 §五） |
| `<CP>/Battle/HudCanvas.cs` | 中改：建 `Anim Anchors` + 4 子锚点 | 不碰 | 无 |
| `<CP>/Battle/PositionOption.cs` | **只订正文件头注释**（见 §七） | 不碰 | 无 |
| `<CP>/Resources/card_anim_order.json`（新，瘦身版） | 不碰 | **新增**（拷/裁 `d:/4/Unity/数据/游戏数据/card_anim_order.json`） | 无 |
| 新载入器（`<CP>/Core/` 或 Battle/，如 `CardAnimOrder.cs`） | 不碰 | **新增**（仿 `Core/AbilityAnimDurations.cs`） | 无 |

改多少：`AnimAnchors.cs` 变化最大（+25 case，行数约 +120）；`BattleDriver.cs` 第 3 步改动集中在
`PlaySignals`/`PlaySignal`/`PendingSignal` 三处（**逐句引用用搜串，⛔ 别用行号**，见 §七）。

---

## 五、每步判据与先后依赖

- **先第 2 步，再第 3 步**：第 3 步「按拍播」时每拍要**起/终锚点**（`GetPos`），那是第 2 步的产物。
  但两者**文件交叠（都在 `BattleDriver.cs`）⇒ 同一时刻只能一个写手**（铁律 13·3）：建议**先派第 2 步做完收口，再派第 3 步**。
- **第 2 步判据**：T05b §一 问3 逐档表 · T05d §一 7 档 UI 映射（主对话已复核）· `AbilityLogic__GetPos.c`。
  验证 = **静态**（逐档对着判据比；`AnimAnchors` 有 `Calls/Fallbacks/LastFallback` 计数可用于自检）。
- **第 3 步判据**：T05b §一 问1/问2 · `card_anim_order.json._conventions`。验证 = **静态** + 条数比（§四 覆盖率）。
- ⚠️ **第 2 步单做完 = 零可见效果**（`Resolve` 无调用点）⇒ 判别「做完」要看**是否被第 3 步消费**为止。

---

## 六、⛔ 别顺手改的（邻接但属别的件）

- **`<CP>/Resources/card_vfx_by_card.json`**：它是**折叠后**的表（一个 `(卡,事件)` 一个 prefab）⇒
  ⛔ **别改它去承载顺序**，顺序走新表 `card_anim_order.json`（T05b §二 第 1 步、铁律 6）。
- **`RuleEngine` 的 `EvtKind.Ability` 语义**：⛔ **别改成引擎多发 N 条**（会污染录像/自检；口径见 T05 §四①）。
- **`AnimAnchors` 文件头两条已订正判据**：`+0x40 = EntityScript.isPlayer` · `18/19 两段自洽` —— ⛔ 别推翻。
- **`14` 与兜底档钉死用「本地玩家」**（`+0xe0`/`+0xc0`，真的不对称）—— ⛔ 别「顺手统一成行动方」（`AnimAnchors.cs:41-42,158-166`）。
- **`Anim Anchors` 单独建 = 死件**（T01 复核定论）⇒ 必须与 `PositionOption` 解析器**同批**做。
- **「卡上特效改走挂载（活在竞技场空间）」那笔大账**（`W_卡牌飞行族普查` §四 (c)；账里明写「一笔独立的大账」）——
  与「驱动侧展开 N 拍」是两回事，⛔ 别顺手并进来。
- **`pointTowardsOption` 出现值 `155` 的疑点**（T05d §五5：疑似提取串字段）—— 单独线索，⛔ 别在本批里猜改。

---

## 七、风险与已知坑

1. 🔴 **`BattleDriver.cs` 行号漂得极快（一天 200+ 行）＋纯 CRLF ＋本会话 7 个写手改过** ⇒
   **一切引用用「搜哪一串」**（本节全部给了搜串）· 改动用 Edit/`wb`，⛔ **别用 `sed -i`**（会翻 LF）。
2. 🔴 **`waitForAnim` 口径打架（动手前必须裁定）**：T05 §四② 第 3 步与 `<CP>/Core/AbilityAnimDurations.cs:12`
   文件头都写「**只有最后一拍才 `waitForAnim`**」；但 **`d:/2/tools/decomp_full/AbilityLogic__PlayAnim.c:218-221`**
   现读 = **每拍看它自己的 `waitAnimToApplyEffect(0x58)`**，`(iVar11 == count-1)` 那个 `&&` 是**多目标循环里的「最后一个目标」**、
   **不是「最后一拍」**；`card_anim_order.json._conventions.waitForAnim` 也已就地订正（「⛔ 不是只有最后一拍」）。
   ⇒ **以反编译为准**，⛔ 别照 T05/AbilityAnimDurations 那两句写。
3. 🔴 **批处理没有帧循环** ⇒ 第 3 步任何「随时间推进」必须挂 **`AdvanceTimeline`**，⛔ **别挂 `Update`**。
4. ⚠️ **原版取值一律 `battlearena2`**（battlearena1 的 2D 全树是截断版、缺的正好是 HUD，已致错两次）。
5. ⚠️ **`PositionOption.cs` 文件头 10-14/17-19 行已过期**：它写「只实现 10 档、别实现 36 档」——
   那**只对 `GetAnimTransform` 成立**；技能路径走 `GetPos`（36 档）。**只订正注释、⛔ 别改数值**（读-only 件，本轮没改）。
6. ⚠️ **阵营名三套对不上**：`cardArmy`（原版 id）→ 我们名（SaimHann/Goff/…）→ `card_anim_map` 键（原版**文件夹名**
   `Aeldari/Orks/…`）；`Genestealers→?` 是**推断**（已按 `Tyranids` 生成了有序表，`ifGenestealersToTyranids=710` vs `AFTERrename 709` ⇒ **要核**）。
   ⚠️ 但 `card_anim_order.json` 已把换名**烘焙进 `vfx`**，第 3 步消费**只需按卡 id**、不必再碰阵营名。
7. ⚠️ **`cards[]` 只 897 张**（池 1127）· **1468 引用里约一半「有名字、无参数」** ⇒ 有序表是**上限**、且**半数拍带不出参数**。

---

## 八、还没查清的（⛔ 不猜，写清还差什么）

1. **`uiAnimAnchors` 有没有运行时覆盖**（`BattleHud.Awake/Setup` 对它的写）—— T05d §四3 **没找到**；我们按序列化值复刻。
2. **8/10 两档「原版没锚点」是数据结论**：本地 506 条 CardAnim 没用过 6-13（T05d §五4），但**全量 402 条 GUID-only 条目没核** ⇒ 「真死档」别写成绝对。
3. **`GetPos` 的 60-130 三族 Mana 锚**在**我们 HUD 里没有对应件**（grep 只命中 `PositionOption.cs` 枚举）——
   要不要建、建几颗，**得看本地数据是否真用这些档**（T05d 只说 61/155 用过）。
4. **`Ctx` 的构造点还没定**（`AnimAnchors.Ctx` 从没被实例化）——第 2 步要顺带定「在哪调 `Resolve`」。
5. **飞行过渡** `__InTransitionToFlyState.c` / `__InTransitionToLandState.c` / `__Toggle.c`（T05 §2·4）方法体**还没读**（属 `A2081`，非本两步）。
6. **`pointTowardsOption=155` 疑点**（T05d §五5）—— 是否提取串字段，未核。
7. **`card_anim_order.json` 是否要瘦身成运行时表**、瘦到哪些字段（3.5 MB 含全 `animInfo`）—— 生成器侧待定。

---

## 附 · 还欠的账（T05 全部剩余，别漏）

第 2 步（36 档）· 第 3 步（驱动侧展开，**本文档主体**）· 第 4 步（接 `startDelay/vfxDelayTime/timeAtStartPos/timeMoving/timeAtEndPos`
+ 3 位置选项；再按需 `easeCurve/screenShake/unitsShake/tweenAnim/…`）· 第 5 步（提取层三笔：`A2096` 必须留关键词门 · `A2087` `gen_card_vfx_by_card.py:1338` 补 `at/an/ct/cn` · `A2086` 只提取、挂账）·
`§2·4` 飞行过渡（`__InTransition*`/`__Toggle`）· `§2·6` 余账（「接这 10 个参数到播放那一刻」仍开着）·
`A2082` 残余（`ByEvent` 退路 + 生成器不幂等）· `A2083` 残余（逐卡残骸体没接 = `A2096`）·
`A2080` 两条新账（验法改用 `PlayerBuild.Run()` · 自检覆盖洞：`AllNames()` 不收 `SummonCircleOf`、无「逐卡表值都在库里」断言）。
