# B89 执行代理 `E5c` —— 缩放接点（账 713）+ `HeroLandIntoField` 的 `showStats` 分组 + `L930` 收口

- 文件：`MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` · `MyGame/Assets/CardPresentation/Core/CardView.cs`（**只这两个**）
- 补丁：`账/_parts/B89_E5c.tsv`（11 列 × 3 行，列数已核）
- 行数 / 行尾：`BattleDriver.cs` **36700 行不变**（`crlf 36700`，改动全是就地改写同一行）· `CardView.cs` **本来就是 LF**（`crlf 0`，`git diff --numstat` = **+77 / −4**，只有我新增的那一段 + 两处上下文）· 新增成员**全在各自类尾**

---

## §1 逐条结论

### ① 账 `713` 的待接点（`ArenaCardScale` 的 else 支）—— 已接
- **真语义**：`GetUnitSizeInPlay` 第三支 = `minionInPlayScale(0.75) × (isPlayer ? 1 : enemyUnitsScaleRatio(0.86))`（`ArenaSlots.cs:229-256`；`0.86` 出自 `GlobalVariables.asset:38`；**C# 里那个 `0.93` 是被资产覆盖的死值**）。
- **改法**（`BattleDriver.cs:22051`，11 行 → 11 行，**零漂移**）：原来 `warlord ? HeroScale : CardScale` 两档把「非部队 / 非督军的在场卡」错当成部队；现在补第三档
  `return (u != null && !u.Card.IsUnit) ? ArenaSlots.InPlayScale(foe) : ArenaSlots.CardScale(foe);`
- ⚠️ `GetUnitSizeRatio(bool)` 三源 **0 调用者 = 死方法**，**没有**按它建（调度台已叮嘱，与 `ArenaSlots.cs:229-240` 的结论一致）。
- ⚠️ **今天没有观测面**：`PlayerState.Board` 是 `UnitState[]`，非部队/非督军的在场卡（defence 39 / enchantment 2）还没进 Board ⇒ 接上也没有画面差异，只能靠算式断言。

### ② `L782` `HeroLandIntoField` 的 `showStats` 分组 —— 已落
- **原版判据（本轮逐句解出，之前只有 TODO）**：`CardScript._HeroLandIntoField_d__318__MoveNext.c:86-338`
  - `showStats == 0` ⇒ **只** `WaitForSeconds(waitMult × 0.3)`，**一个容器都不碰**；
  - `showStats == 1` ⇒ 对**两族**计数器控制器各做一次：
    · `BattleCardUI +0xC0` = **`armourCountersController`** · `+0xA0` = **`healthCountersController`**
      （字段名/偏移 = `d:/2/tools/il2cpp_out/dump.cs:19811` 起的 `BasicCardUI` 字段表）；
    · 控制器 `currentState(+0x64) = 1`，再对 `containers(+0x30)` 逐项 `container(+0x10).SetActive(flag)`，
      flag 由该项 `activationRules(+0x18)` 算：**含 `Normal(0)` 或 `NoSimplifiedView(15)` ⇒ 开；
      含 `SmallScreen(5)` ⇒ 仅小屏开（否则保持原样）；含 `SimplifiedView(10)` ⇒ 保持原样**
      （枚举值 = `dump.cs:36933`）。
- **我们的落点**：`CardView.cs` 类尾新增
  `StatContainerRules`（:`15257`，**四个枚举值就是原版的 0/5/10/15**）·
  `StatContainerRuleForCell`（:`15267`）·
  `ApplyStatContainersOnHeroLand(showStats, smallScreen=false)`（:`15275`）·
  `ContainerOpens(rules, smallScreen)`（:`15293`）；
  调用点 = `BattleDriver.HeroLandInto` 的 `SchedulePresentationCallback` 里（`BattleDriver.cs:34667`，段 0 收尾那一刻）。
- **等价物说明**：容器 = 我们那两个数值图层本身；「开」= 用**同一条**逐格判据 `StatShownFor(s, Data)` 重落一次显隐（与 `SyncStatLayers` 同一段），**⛔ 没有第二份判据**。

### 🔴 原版值 vs 我们挑的（调度台要求单列）
| 项 | 来源 |
|---|---|
| 两族 = `armourCountersController(+0xC0)` / `healthCountersController(+0xA0)` | **原版**（`dump.cs:19811` 起字段表） |
| 规则枚举 = Normal 0 / SmallScreen 5 / SimplifiedView 10 / NoSimplifiedView 15 | **原版**（`dump.cs:36933`） |
| 规则判定顺序（0/15 开 · 5 仅小屏 · 10 保持原样） | **原版**（`.c:120-160` 的循环） |
| `showStats == 0` ⇒ 不碰任何容器 | **原版**（`.c:95-97`） |
| **每族只有「一个」容器**（我们一个数值 = 一个图层） | 🔶 **我们挑的** |
| **该容器规则取 `NoSimplifiedView(15)`** | 🔶 **我们挑的**（理由：原版是 prefab 上序列化的私有数组 `CardTextCountersController.containers`，本机**没有那张 prefab 的 MB JSON**；取「任何视图都显示」最保守的一档） |
| **`smallScreen` 恒 false** | 🔶 **我们挑的**（我们**没有** `smallScreenUI(+0x11c)` 这一档；同 `GameplayVariables.HandCardDisplayOffset` 几处的既有口径） |

⇒ 若将来能从 prefab MB JSON 读出真实 `containers` / `activationRules`，**只需改 `StatContainerRuleForCell` 一处**。

### ③ `L930` 残差 3 条 —— **保持 B**（⛔ 不造路）
已接 6 个原版调用点；`DestroyCardOffensive` 已在 `StartRevealFlight`（`:10171`）判为等价。余 3 条**前提缺失**：
- **`MoveToDeck`** = 本仓**没有「回牌库」的视觉路**（只有时长账 `卡组:194`；`Core/EventTiming.cs:179-195` 只算时长）；
- **`RemoveFromGameCard`** = 引擎侧**没有「移出游戏」事件**（`RuleEngine/Core/BattleEvent.cs:25-55` 的 `EvtKind` 只有 `Return` 管回手/回牌库）；
- **`DestroyCard`** = 没有对应的销毁路。
⇒ 三条都写进了账（`B89_E5c.tsv` 第 3 行），**没有为了让数字好看而造路**。

---

## §2 验证读数

- `TMPDIR=/tmp/wf_B89E5c bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（最后一次）。⚠️ 中途几轮曾报别人正在写的文件里的错（`RuleEngine/Core/BattleContext.cs` CS0708 · `Core/TMProWarpText.cs` CS1061 · `Shell/BaseEventPopup.cs` CS0117），**都不是本代理的文件**，未去改，重跑后归零。
- 行尾：`BattleDriver.cs` 36700/36700（CRLF）· `CardView.cs` 15304 行 / `crlf 0`（**本来就是 LF**，非我翻转；`git diff --numstat` = +77/−4 佐证）。
- 零漂移自检：`ArenaCardScale` 仍在 `:22051`；`HeroLandInto` 仍在 `:34656`（只改了 lambda 里那一行）。

---

## §3 顺手发现 + 真机待验

### 顺手发现
1. **`BattleCardUI +0xA0/+0xC0` 的字段名本轮查到了**（`healthCountersController` / `armourCountersController`）—— 旧账只写「两族子对象、判据未逐句解」，现在有字段级出处（`dump.cs:19811` 起）。
2. **`CardTextCountersController.containers` 是私有序列化数组**，本机没有对应 prefab 的 MB JSON ⇒ 「每组几项、各项什么规则」**读不到**，这是 🔶 的唯一原因（不是没查）。
3. `CardView.cs` **是 LF 文件**（`BattleDriver.cs` 是 CRLF）—— 两个文件的换行风格**不同**，改的时候别一刀切（本波各自保持原样）。

### 真机待验
1. **教程/战役开局的督军入场**（唯一会走到 `HeroLandInto` 的场合；普通开局不调它）：敌方督军落地那一拍，**血量/护甲那两个数值格按常规显示**（不闪、不消失），震镜照旧；`showStats` 恒 1 ⇒ 走的是「按规则开」那一支。
2. **普通对局回归**：任何单位的场上数值（近战/远程/护甲/生命）显示**零变化** —— 本轮只动了 `HeroLandInto`（`animateFeel` 且教程/战役才走），以及 `ArenaCardScale` 的第三档（当前无观测面）。
3. **`ArenaCardScale` 第三档**：今天**点不出来**（非部队/非督军的在场卡还不进 Board）⇒ 只能靠自检算式断言，真机暂无验收口。

---

## §4 没查清 / 明确没做

1. **原版 `containers` 的真实项数与各项 `activationRules`**：需要那张卡 prefab 的 MB JSON（`CardTextCountersController.containers` 私有序列化数组）。本机 `assets_full` 里没找到对应资产（未逐一穷举到那份 prefab）⇒ 按调度台裁定就地打 🔶，不冒充原版。
2. **`smallScreenUI(+0x11c)` 的等价物**：本仓没有；`SmallScreen(5)` 那一档因此永不触发（与既有几处「恒传 false」同口径）。
3. **`L930` 的三条前提**（见 §1·③）—— 明确不做，等那三条路本身建出来。

---

## §5 交接

- **`CardView.cs` 类尾新增区 = `:15232-15304`**（`StatContainerRules` / `StatContainerRuleForCell` / `ApplyStatContainersOnHeroLand` / `ContainerOpens`）。下一棒若还要在 `CardView` 加东西，**继续往 `:15304` 之后追加**。
- **`BattleDriver.cs` 类尾仍是 `:36700`**（本波没加成员）。
- `ApplyStatContainersOnHeroLand` 目前**唯一调用点** = `BattleDriver.HeroLandInto`；若将来别处也要用，⛔ 别复制判据（`StatShownFor` 是唯一来源）。
- 🔶 标记**本波新增 3 处**（方法上方一行 + `StatContainerRuleForCell` 上方一行 + 枚举那段长注里一句），都在 `CardView.cs` 类尾新增区内。⚠️ 全文件 `grep -c 自己设计` 现读 = **10**（其余 7 处是既有的，别当我这次加的）。
