# T05d · GetPos 那 8 档的 UI 锚点（BattleHud.GetUIAnchor）

> 只读判据代理产出 · 2026-10-16 · 补 `T05b_cardAnim索引与参数喂给谁.md` §三 第 1 条（「`BattleHud.GetUIAnchor` 的内部映射没读」）。
> 判据权威顺序：原版反编译 `d:/2/tools/decomp_full/` ＞ 字段/枚举 `d:/2/tools/il2cpp_out/dump.cs`
> ＞ 解包场景 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena2/`（战斗侧一律 battlearena2）。
> ⛔ 没跑 Unity、没动 git、没改任何正本或 `.cs`。

---

## 〇、先订正 T05b §三 第 1 条（铁律 5）

T05b 写「**没搜到 `BattleHud__GetUIAnchor.c` 的方法体；`dump.cs` 里只有签名**」——
**方法体就在** `d:/2/tools/decomp_full/BattleHud__GetUIAnchor.c`（**64 行，本轮逐句亲读**）。
另有一份同族小helper `d:/2/tools/decomp_full/BattleHud.UIAnimAnchors__TryGetAnchor.c`（12 行）。
⇒ 这一格**不是「判不了」**，是本轮已解出（见下）。

---

## 一、`GetUIAnchor` 逐档映射

### 1·1 它是**数据驱动**的（这是本轮最关键的一条）

```
Transform BattleHud.GetUIAnchor(PositionOption desiredPosition, bool isPlayer)   // dump.cs:27301
```
它**不 switch**，而是**遍历 `BattleHud` 上的一个序列化数组**再查表（`BattleHud__GetUIAnchor.c:28-51`）：

- 数组字段 = **`BattleHud.uiAnimAnchors`（偏移 `0xB0`）**，类型 `BattleHud.UIAnimAnchors[]`
  （`dump.cs:27228`；`[Header("Anchors for animations")]`，`dump.cs:27226`）。
- 元素结构体 `BattleHud.UIAnimAnchors`（`dump.cs:27168-27180`，`[Serializable]`，**4 个字段**）：

  | 字段 | 偏移 | 类型 | 含义 |
  |---|---|---|---|
  | `positionOption` | `0x0` | `PositionOption` | **本档号**（查表键） |
  | `anchor` | `0x8` | `Transform` | 默认锚点 |
  | `useIsPlayer` | `0x10` | `bool` | 是否按 `isPlayer` 二选一 |
  | `opponentAnchor` | `0x18` | `Transform` | `useIsPlayer && !isPlayer` 时改用它 |

- 算法（`BattleHud__GetUIAnchor.c`，逐句）：
  - `:21` 取数组 `*(param_1+0xB0)`；`:28` 循环 `count = *(array+0x18)`；
    `:33` 元素基址 = `array + 0x20 + i*0x20`（**元素 32 字节**，`:33-36` 取 `anchor`=+0x8、`:36` 取布尔=+0x10）。
  - `:37` 若 `element.positionOption == desiredPosition`：`:38-41` **若 `useIsPlayer!=0 && isPlayer==0` 则取 `opponentAnchor`**，否则取 `anchor`。
  - `:43-45` 档号不匹配 ⇒ 该轮结果置 0、继续下一轮；`:49-50` 一旦取到**非 null** 就 `break`。
  - `:56-62` **跑完还是 null** ⇒ `CustomDebug.LogError("…{档号}…")` + **返回 `hud.transform`**（= **BattleHud 根节点自己的 transform**，`:61`）。
- ⚠️ **`TryGetAnchor`（那个 helper）在内联后被展开，`GetUIAnchor` 体内没调它** —— 逻辑等价，别以为是两条路。

### 1·2 逐档表（`battlearena2` 的实测数据）

改 GetPos 那 8 档的**真实返回值**如下（`isPlayer` 一栏见 1·3）：

| 档 | `PositionOption` 名 | 返回哪颗节点（battlearena2） | 节点 PathID | `useIsPlayer` | 出处 |
|---|---|---|---|---|---|
| **6** | `boardCenterUI` | **`Anchor: UI Center`** | RT 3098 | 0 | `MonoBehaviour_5150.json` uiAnimAnchors[2] |
| **7** | `cemetery` | **`CemeteryLogPanel`** | RT 3129 | 0 | 同上 [1] |
| **8** | `friendlyPower` | **数组里没有** ⇒ **LogError + BattleHud 根 `transform`** | — | — | 13 场数组都无 8（见 1·4） |
| **10** | `nextToCemetery` | **数组里没有** ⇒ **LogError + BattleHud 根 `transform`** | — | — | 无 10 |
| **11** | `nextToClock` | **`Anchor: Next To Clock`** | RT 3208 | 0 | uiAnimAnchors[6] |
| **12** | `friendlyMana` | `anchor`=**`PlayerMana`**(RT 2983) / `opponentAnchor`=**`EnemyMana`**(RT 3674) | 2983 / 3674 | **1** | uiAnimAnchors[3] |
| **13** | `enemyMana` | `anchor`=**`EnemyMana`**(RT 3674) / `opponentAnchor`=**`PlayerMana`**(RT 2983) | 3674 / 2983 | **1** | uiAnimAnchors[4] |
| **140** | `createCardPos` | **`Anchor: CreateCard`** | RT 2916 | 0 | uiAnimAnchors[0] |
| **145** | `stratagemGeneralShowCase` | **`Anchor: Stratagem ShowCase`** | RT 3000 | 0 | uiAnimAnchors[5] |

`battlearena2` 的 `uiAnimAnchors` 全表（7 条，`MonoBehaviour/MonoBehaviour_5150.json`）：

| # | positionOption | anchor | useIsPlayer | opponentAnchor |
|---|---|---|---|---|
| 0 | **140** | 2916 (`Anchor: CreateCard`) | 0 | 0 |
| 1 | **7** | 3129 (`CemeteryLogPanel`) | 0 | 0 |
| 2 | **6** | 3098 (`Anchor: UI Center`) | 0 | 0 |
| 3 | **12** | 2983 (`PlayerMana`) | **1** | 3674 (`EnemyMana`) |
| 4 | **13** | 3674 (`EnemyMana`) | **1** | 2983 (`PlayerMana`) |
| 5 | **145** | 3000 (`Anchor: Stratagem ShowCase`) | 0 | 0 |
| 6 | **11** | 3208 (`Anchor: Next To Clock`) | 0 | 0 |

### 1·3 `isPlayer` 怎么影响每一档 —— **只影响 12/13；其余 7 档被调用点【写死 false】**

关键在 **`AbilityLogic.GetPos` 怎么传第三参**（`AbilityLogic__GetPos.c`）：

- **档 6 / 7 / 8 / 10 / 11**（`:236-241`）与 **档 140（0x8c）/ 145（0x91）**（`:75`、`:78`）
  → 全 `goto switchD_18081797e_caseD_6`，而该 label **`uVar4 = 0;`**（`:82`）
  ⇒ **`isPlayer = false` 写死**（不是行动卡的 `isPlayer`）。
  ∵ 这 7 条的 `useIsPlayer` 都是 0 ⇒ 实际都是「取 `anchor`、不看 `isPlayer`」，写 false 无副作用。
- **档 12（0xc）/ 13（0xd）**（`:242-256`）→ `uVar4 = *(card+0x10 +0x40)`（= **行动卡 `EntityScript.isPlayer`**，
  `dump.cs:21963`）⇒ **`isPlayer = 行动卡是不是本地玩家的`**。
  - 档 12 `friendlyMana`：`isPlayer==真` ⇒ `PlayerMana`；`isPlayer==假` ⇒ `EnemyMana`。
  - 档 13 `enemyMana`：`isPlayer==真` ⇒ `EnemyMana`；`isPlayer==假` ⇒ `PlayerMana`。
  ⇒ **12/13 互为镜像**，都读「行动卡那一侧」：`friendly` = 行动方、`enemy` = 对面。
    （与 `AnimAnchors.cs` 文件头那条「`friendly`/`enemy` 是相对行动方、两档互取反」**同一口径**。）

### 1·4 13 个战场**完全一致**

`uiAnimAnchors` 在**全部 13 个 `bundle_scenes_scenes_battlearena*` 的 `BattleHud`** 上逐场现读
（每场一个 MonoBehaviour：arena1=4934 · arena2=5150 · arena3=5056 · aeldari=4910 · astra=5110 ·
blacklegion=5011 · darkangels=4979 · emperors=4976 · genestealers=4942 · leviathan=5104 ·
sororitas=5151 · spacewolves=4945 · tau=5538）：

- **都是 7 条、档号集合都是 `{6,7,11,12,13,140,145}`**；
- **12/13 都是一对 `useIsPlayer=1` 的镜像**；**其余 5 条 `useIsPlayer=0`**；
- **没有任何一场含 8 或 10**。
⇒ **8(friendlyPower) / 10(nextToCemetery) 在原版战斗 HUD 上【本来就没有锚点】** ——
   `GetUIAnchor` 对它们走 `LogError` + **返回 BattleHud 根 transform**。**这不是我们缺件。**

---

## 二、那些节点在解包场景里的名字与父链

（`bundle_scenes_scenes_battlearena2`，逐节点解 `m_Father` 父链；根 = `BattleHud`）

```
BattlePrefab(#1348) └ BattleHud(#1471, 纯 Transform, GameObject pid 230)
    ├ Canvas(#2952)
    │    ├ Anim Anchors(#2807)              ← 4 颗动画锚点的共同父
    │    │    ├ Anchor: UI Center        (#3098)  ← 档 6
    │    │    ├ Anchor: CreateCard       (#2916)  ← 档 140
    │    │    ├ Anchor: Stratagem ShowCase(#3000) ← 档 145
    │    │    └ Anchor: Next To Clock    (#3208)  ← 档 11
    │    └ BackCanvas(#2877)
    │         └ Safe area BackCanvas(#3693)
    │              ├ RightArea(#3709) → Right Anchor(#3158) → Energy And turn holder(#3554)
    │              │      ├ PlayerMana(#2983)     ← 档 12 的 anchor / 档 13 的 opponentAnchor
    │              │      └ EnemyMana(#3674)      ← 档 13 的 anchor / 档 12 的 opponentAnchor
    │              └ LeftArea(#3042)
    │                   └ CemeteryLogPanel(#3129) ← 档 7
```

各节点 RectTransform 实测（`RectTransform_*.json`）：**除注明外都是 `pivot=(0.5,0.5)`、`scale=1`**

| 节点 | PathID | aMin / aMax | pos | sizeDelta | 备注 |
|---|---|---|---|---|---|
| `Anchor: UI Center` | 3098 | (0.5,0.5)/(0.5,0.5) | (0, 0) | 100×100 | 正好在 Canvas 中心 |
| `Anchor: CreateCard` | 2916 | (0.5,0.5)/(0.5,0.5) | (885.56, 84.27) | 100×100 | 面板内 = (+885.56, +84.27) |
| `Anchor: Stratagem ShowCase` | 3000 | (0.5,0.5)/(0.5,0.5) | (529, 83) | 100×100 | |
| `Anchor: Next To Clock` | 3208 | (0.5,0.5)/(0.5,0.5) | (766, 83) | 100×100 | |
| `CemeteryLogPanel` | 3129 | (0, 0.1873)/(0, 0.7927) | **(-1200, 0)**，pivot=**(0, 0.5)** | 794.07 × 0 | 🔴 **`m_IsActive=false`**，且 x=-1200 ⇒ **屏外左侧** |
| `PlayerMana` | 2983 | (0.592,0.0224)/(0.918,0.2328) | (0.566, 0.700) | (-1.812, 1.970) | 相对 `Energy And turn holder` 中心 = (61.46, −132.10)，尺寸 76.0×77.0 |
| `EnemyMana` | 3674 | (0.592,0.768)/(0.903,0.988) | (0.066, 0.100) | (0.252, -0.830) | |

> `bararena2` 的 `PlayerMana/EnemyMana` 矩形与我们 `BattleDriver.cs:14015-14017` 已记的值一致
> （`PlayerMana` 97.7×97.7 那两颗），是同一对节点。

---

## 三、我们这边怎么接（改哪个文件、缺什么）

**结论：这 8 档现在【完全没接】** —— `Battle/AnimAnchors.cs` port 的是 `BattleManager.GetAnimTransform`
（`AnimAnchors.cs:124-192` 的 `Resolve`），而 `6-13/140/145` 在那条路上落「报错+兜底」档
（`AnimAnchors.cs:183-191`）。要接 `GetPos` 那条真路，需要：

**① 建 7 颗锚点节点（`HudCanvas.cs` 是落脚点）**
- 我们**已经有** `PlayerMana` / `EnemyMana`（`BattleDriver.cs:1474-1476, 14015-14017`）与
  `CemeteryLogPanel`（`BattleDriver.cs:871, 4514`）对应的 HUD 件 ⇒ 档 7/12/13 可**直接挂到既有件**。
- 我们**没有** `Anim Anchors` 那一族（`BattleDriver.cs:14489` 只在注释里列过原版的 HUD children；全仓无 `Anchor: UI Center` 等四颗）。
  ⇒ 需要在 HUD uGUI 层（`HudCanvas.cs`，`T09` 的落脚点）新建一颗 `Anim Anchors`（铺满的 `RectTransform`）
  与 4 颗子锚点，`anchoredPosition` 照上表：`UI Center (0,0)` · `CreateCard (885.56,84.27)` ·
  `Stratagem ShowCase (529,83)` · `Next To Clock (766,83)`（均 `sizeDelta=100×100`）。
  ⚠️ `Anim Anchors` 在原版是**铺满父 Canvas**（aMin(0,0)/aMax(1,1)、sd=0）⇒ 上面的 pos 就是**相对 Canvas 中心**的偏移，
  与我们 `HudCanvas.cs` 头注说的「1 canvas 单位 = 1 设计 px」同口径，**原样搬**即可。

**② 补解析口（只改 `AnimAnchors.cs`，⛔ 别开第二份）**
- 加一个 `Func<PositionOption,int,Transform>`（或几个 `Func<int,Transform>`）的 UI 锚点解析口，
  按本文 §1·2 的表把 7 档映射到上面 7 颗节点；8/10 档**照原版**：LogError + 返回 HUD 根 transform。
- `12/13` 按**行动卡 `isPlayer`** 二选一（`Ctx.actorSeat` 已有，`actorSeat==mySeat` ⇒ isPlayer=true）。
- ⛔ 别推翻 `AnimAnchors.cs` 文件头那两条已订正判据（`+0x40`=`isPlayer` · `18/19` 自洽）；
  它们与本表不冲突（本表是 `GetPos` 的另一条路）。

---

## 四、还差什么 / 判不准的

1. **8 / 10 两档「原版本就没锚点」是【数据结论】，不是【代码结论】** ——
   代码里 `case 8`/`case 10` 确实走 `GetUIAnchor`（`AbilityLogic__GetPos.c:236-241`），
   但 13 场 `uiAnimAnchors` **都没有 8/10** ⇒ 落 LogError + 根 transform。
   **判不清的是**：有没有哪张卡的 `CardAnim` 真用过 8/10（用到了才会现形）。
   我已扫我们本地的 `d:/4/Unity/数据/游戏数据/card_anim_map.json`（506 条 CardAnim）——
   **start/end 档只出现 `{0,1,2,3,4,5,14,16,17,18,19,50,61,155}`，无 6-13**（见 §五 第 4 条）。
   但**这份表是否覆盖原版全部 CardAnim 没核**（原版还有 402 条 `_guid_summary` GUID-only 条目没名字）⇒ 留疑。
2. **原版 CardAnim 数据里 6-13 档是否真被用过，需要 join `cards_1195_2022时代.json` 的 `cardAbilities[].abilityLogic[].cardAnimations[]`
   逐条展开后才能定论** —— 本轮**没做**这步 join（超出「解 GetUIAnchor（映射）」这个范围）。
   ⛔ 不猜：**只覆盖「本地 506 条 CardAnim」，没覆盖全部**。
3. **`useIsPlayer` / `opponentAnchor` 在本 build 上是否被运行时改过** —— `dump.cs` 的字段名与序列化值一致，
   但**没找到运行时写这两格的赋值点**（判据：没查 `BattleHud.Setup()`/`Awake()` 对 `uiAnimAnchors` 的写）。
   ⇒ 我们按**序列化值**复刻（那是场景里唯一能读到的真值）。还没查清「有没有运行时覆盖」。
4. **搜过但没用于本结论的路径/词**（登记以免重复搜）：
   `decomp_full` 里 `BattleHud*` 全部 34 个 `.c`（只 `GetUIAnchor`/`UIAnimAnchors.TryGetAnchor` 与本题相关）·
   `dump.cs` 词 `GetUIAnchor`/`UIAnimAnchors`/`PositionOption`/`CardStateOptions`/`AbilityTrigger`·
   13 个 `bundle_scenes_scenes_battlearena*/MonoBehaviour/`（`grep uiAnimAnchors`）。
   ⛔ **没用 Godot 时代的任何文件。**

### 附 · 顺带解掉的两条（T05b §三 第 2、6 条）

**（a）`CardScript + 0x228` = `CardStateOptions cardState`（`dump.cs:35454` / 属性 `:35533`；枚举 `:45465-45486`）**

`GetEffectAnchor`（`CardScript__GetEffectAnchor.c:9-20`）按它二选一：

| 分支 | 条件（`cardState ∈`） | 返回 |
|---|---|---|
| 手牌/牌堆态 | `{0 inDeck, 1 inHand, 4 waitingToBePlayed, 8 inHandShowing, 9 inHandMoving, 10 inHandPlaying, 12 inHandJustCreated, 16 inDeckDrawing}` | `CardUI(+0x290).card2DController(+0x30)` 的 `transform`（`:12-15`） |
| 其余（场上等） | 其它值（`2 inPlay / 3 inPlayAttacking / 5 waitingToDie / 6 inCemetery / 7 drawing / 11 removedFromGame / 13 inMulliganSelected / 14 inMulliganDiscarded / 15 inPlayTransforming / 17 inPlayAminingAttack`…） | `CardUI(+0x290).effectsAnchor(+0x1A0)`（`:18-19`） |

> `+0x290` = `BattleCardUI CardUI`（`dump.cs:35486`）；`BattleCardUI.card2DController` 继承自 `BasicCardUI`（`dump.cs:19815`，偏移 `0x30`）；
> `BattleCardUI.effectsAnchor` = `0x1A0`（`dump.cs:26190`）。⚠️ 两分支取不到对象时原版**抛异常**（`FUN_1803f47a0`，`"Subroutine does not return"`）。

**（b）`AbilityTrigger` 值 → 名（`dump.cs:20235-20336`，逐值现读）**

| 值 | 名 | 值 | 名 | 值 | 名 |
|---|---|---|---|---|---|
| 0 | ThisCardPlayed | 60 | UnitDamaged | 300 | Mob |
| 10 | OtherCardPlayed | 70 | UnitHealed | 301 | Regiment |
| 15 | PlayedThirdCardInTurn | 80 | ThisUnitDeath | 400 | ThisUnitAbilityUsed |
| 20 | WhileInPlay | 81 | Unstable | 410 | FollowUpResolution |
| 30 | TurnStart | 90 | OtherUnitDeath | 420/422 | Survivor/SurvivorSpent |
| 31 | TurnStartPlayer | 100 | Growth | 430/435 | Requiem/OtherCardRequiem |
| 40 | TurnEnd | 110 | HeroDefenseBonus | 440/450 | Landing/OtherCardLanding |
| 41 | TurnBeforeEnd | 120 | Slay | 455 | OtherCardSpiritStone |
| 43 | Courtesy | 130 | Rage | 460 | Mutated |
| 45 | Resolution | 145 | Codex | 470/480 | CardReturnedToHand/ToDeck |
| 46 | Talent | 155 | TurnSetup | 490 | QuestTriggered |
| 50 | UnitAttack | 160 | TurnStarted | 500 | TriggerEnchantment |
| 510/515/520 | UnitReceives{MarkOfChaos,Markerlight,HuntMark} | 170 | UnitDeathByPoison | 550 | Support |
| 175/180 | UnitDeathAttacking/ByAbility | 560 | Artifice | 185/187 | UnitDamagedBy{Ability,Attack} |
| 190/195 | OtherUnitSummoned/ThisUnitSummoned | 570 | Reanimated | 200 | TriggerOnceIfBoardCriteria |
| 210 | CardCreatedInHand | 580 | Strike | 220/225 | Sacrifice/OtherCardSacrifice |
| 230 | Relentless | 590 | Combo | 240 | WhileInPlayVariable |
| 245 | Pack | 600 | UseSpiritStone | 250 | ThisCardDrawnAsTrap |
| 260 | OtherCardDrawn | 605 | **UseFaith** | 270 | TrapResolved |
| 280 | AboutToAttack | 610 | OnCollectSpiritStone | 290/291 | UnitPoisoned/UnitStunned |
| 620/630 | Triggered{Swarm,Synapse} | 640/641 | EarnedSkull/SpentAllMana | 645/646 | OtherCard{Mob,Regiment} |
| 647 | UnitLoseStealth | 650 | TriggeredCodex | 655/660 | Pray/TriggeredPray |
| 661/662 | TriggeredDuty/Duty | 665 | OnCollectFaith | 670 | Ambush |
| 680 | Uprising | 690 | Teleport | 695 | OnCollectQuestPoints |
| 700/701 | On{Secret,Sabotage}Created | 705 | Agenda | 710 | Stimulation |
| 715/720 | Ecstasy/Cruelty | 725/726 | Ferocity/TriggeredFerocity | 730 | Oath |

> ✅ 数据里的实值 **605 = `UseFaith`**（`Sororitas` 那族）、**40 = `TurnEnd`**、**600 = `UseSpiritStone`** 都能对上了。
> ⚠️ `AbilityTrigger` 与 `AbilityEffect`（`d:/2/远端缓存_旧profile_1013/导出/AbilityEffect_枚举词典.tsv`）**不是一个枚举**，别混。

---

## 五、顺手发现的新问题（铁律 5·b → 请主对话立账）

1. 🔴 **T05b §三 第 1 条要就地订正**：「没搜到 `BattleHud__GetUIAnchor.c` 的方法体」——**方法体存在**
   （`decomp_full/BattleHud__GetUIAnchor.c`，64 行），本轮已解。**错因**：当时没在 `decomp_full` 里按 `BattleHud__*` 列全。
2. 🔴 **`GetUIAnchor` 是【数据驱动】的：`BattleHud.uiAnimAnchors[]`（`BattleHud + 0xB0`，7 条）** ——
   这 7 条映射是**场景序列化数据**，不是代码常量；13 个战场**逐场一致**。
   ⇒ 复刻时要么照抄这张表，要么做成同一张数据表（铁律 6：别在代码里再写死第二份）。
3. 🔴 **`CemeteryLogPanel`（档 7）默认 `m_IsActive=false` 且锚在 x=-1200（屏外左侧）** ——
   任何 `sp/ep=7` 的特效会锚到**一个隐藏且屏外**的面板。若确认原版也没卡用 7 档，则正确；若有卡用，得一并复刻它的「滑入」姿态。
4. 🔴 **本地 506 条 `CardAnim`（`数据/游戏数据/card_anim_map.json`）里 start/end 档从未用过 6-13**
   （只用 `{0,1,2,3,4,5,14,16,17,18,19,50,61,155}`）⇒ 这 8 档在**当前快照**里是**潜在死档**，
   优先级可以排在 `0-5/14/16-19/50/61/155` 之后；**但 GetPos 确实实现了它们**，且 `61/155` 是**真在用的高档**
   （说明高档这一族不是摆设）。⚠️ **覆盖范围只到本地这 506 条**（原版另有 402 条 GUID-only 未命名条目），
   ⇒ 「没用过」这个判断**别写成绝对**。
5. **`pointTowardsOption` 在本地数据里出现一个 `155`**（`card_anim_map.json`，各 1 次）——
   155 = `PositionOption.enemyHandWorld`，语义上像 `endPosOption` 的值。**可能是数据本身就这么写的**（照抄原值），
   但也**可能是我们提取时把某两个字段串了**（`pointTowardsOption` 吃到了 `endPosOption` 的值）。
   ⛔ 本轮**没核**（不在任务范围）；**建议主对话另派一条只读线索**去 `cards_1195_2022时代.json` 逐条对一遍。
