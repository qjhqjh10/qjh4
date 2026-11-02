# B117-WMULL · 换牌补抽次序（引擎）＋ 选环境卡面板倒计时（表现）（2026-11-02）

> 执行代理 `B117-WMULL`。判据正本 = `判据/B117-G10-17_换牌Mulligan.md` **§二·A / §二·B**（本文件**不抄判据**，只写「改了什么 / 读数 / 归因」）。
> ⚠️ 本文件**只写证据与做法**；状态见 `活状态表.md`（那一行由调度台核销）。
> 白名单（只动这四件）：`RuleEngine/Core/RuleCore.cs` · `CardPresentation/Battle/BattleDriver.cs` · 新建 `RuleEngine/Editor/RuleEngineTest_B117WMULL.cs`（＋它的 `.meta`）· 本文件。

---

## 一、A · 换牌补抽次序 + 洗牌时机（`RuleCore.cs`）

### 1·1 改了什么（旧 → 新）

| # | 位置 | 旧 | 新 |
|---|---|---|---|
| A-1 | `RuleCore.cs:489-500`（`RuleCore.Mulligan` 的「弃回 + 补抽」段） | `ps.Deck.Add(ps.Hand[idx[i]])`（降序循环）→ **`Shuffle(ps.Deck, ctx)`** → `Draw` ×N | `moved` 按**升序**收集 → 降序摘手牌 → **`ps.Deck.Insert(0, moved[i])`** → `Draw` ×N（**不洗**） |
| A-2 | `RuleCore.cs:502` | 日志「弃回牌库 → 重洗 → 补抽」 | 日志「弃回牌库表尾 → 补抽表头；洗牌在 EndMulligan」 |
| A-3 | `RuleCore.cs:449`（`Mulligan` 的 summary） | 「弃回牌库 → 洗牌 → 补抽同样张数」 | 「弃回牌库（表尾）→ 补抽（表头）；洗牌**不在**这儿」 |
| A-4 | `RuleCore.cs:522`（`RuleCore.EndMulligan`，**同一物理行追加**） | 无洗牌 | `Shuffle(ctx.Players[ctx.FirstSeat].Deck, ctx); Shuffle(ctx.Players[1 - ctx.FirstSeat].Deck, ctx);`（**先手那副先洗**） |

### 1·2 镜像换算的推理（这是本条最容易做反的地方）

- **原版**（一手）：`PlayerHand__ReplaceMulliganCard.c` 把卡挂到牌堆 transform 后 **`List.Add`**（= 追加到**表尾**）；`BattleManager__GetMulliganCard.c` = `get_Item(deck, 0)` → `RemoveAt(deck, 0)`（= 从**表头**取）；**换牌期一次都不洗** —— `BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:156-160` 的 **case 3**（此时双方补牌都已完成）才 `ShuffleDeck` 两副（实参 = `playerGoesFirst` 与取反）。
- **我们的约定**（现读 `MoveDeckTopToHand`）：`ps.Deck[Count-1]` = **牌堆顶**，`Draw` 取的就是它 ⇒ **我们的下标 `0` 对应原版的「表末」**。
- ⇒ **原版「追加到表尾」= 我们 `Insert(0, …)`**（⛔ 不是 `Add`：`Add` 在我们这套约定下是「加到牌堆顶」= 换掉的牌会被当次抽回来）。
- ⇒ **原版「从表头取」= 我们的 `Draw`（取 `Count-1`）** —— 这一句**不用改**。
- ⇒ **插入次序**：原版按**手牌升序**逐张 `List.Add`（B 先、C 后 ⇒ 表末是 C）。镜像过来必须也按**升序** `Insert(0, …)`（先 B 后 C ⇒ 我们下标 0 是 C）。⚠️ 收集循环原来是**降序**（为了 `RemoveAt` 不串位）⇒ 现在拆成「**升序收集 → 降序摘牌 → 升序插回**」，次序才对得原版。
  （如实记：因为紧接着 `EndMulligan` 会把两副都洗掉、中间没有任何抽牌，**这个次序今天可观测为零**；仍然照做是为了不把「等一等就对了」写进代码。）

### 1·3 行数/落点纪律

- A-1 = **12 行 → 12 行**、A-4 = **4 行 → 4 行**、A-2/A-3 各 1 行 → 1 行 ⇒ `RuleCore.cs` **总行数不变**（现读 18,406 行；`\n` 18,406 / `\r\n` 0 ⇒ 原本就是纯 LF，没被翻）。
- 账落点现核：`RuleCore.cs:503`（4 处）、`:517`（1 处）**逐条仍在原符号上**（`:503` = `return idx.Count;`，`:517` = `public static void EndMulligan(...)`）。严格 grep（排除 `:4986`/`:5034` 这种前缀假命中）后，**489–530 区间内只有这两处落点**。

---

## 二、B · 选环境卡面板的倒计时（`BattleDriver.cs`）

### 2·1 改了什么

| # | 位置 | 旧 | 新 |
|---|---|---|---|
| B-1 | `BattleDriver.cs:14396`（主循环 `HandleMulligan()` 那一行，**同一物理行追加**、代码在 `//` **之前**） | 只有 `if (HandleMulligan()) { UpdateHud(); return; }` | 句末追加 `TickEnvPickCountdown(Time.deltaTime);` |
| B-2 | `BattleDriver.cs:40636-40696`（**类尾**，`BattleDriver` 类在 `:40696` 关、`ChoicePanelPool` 是第二个顶层类） | 无 | 新增 `_envPickShownSec` 字段 ＋ `EnvPickSecondsAfter(float)`（纯函数）＋ `EnvPickTickDue(...)`（纯函数）＋ `TickEnvPickCountdown(float)` |

### 2·2 行为

- 只在「**选环境卡**那扇面板开着」时走表：判据 = `_envPickViews.Count > 0`（这个列表**只**在 `BeginOffensivePhaseIfAny` 的进攻/防御两支里填，`ClearEnvPickViews()` 之后清空）⇒ ⛔ 不会碰到「选牌 / 选一 / 选效果」那几条路。
- 秒数源 = `_mulliganLeft`（**换牌倒计时剩下的秒数**，原版 `_MulliganCountdown` 停→启**同一个协程字面量** ⇒ 计数器不重置）。回合表（`_clockInCountdown` 那条链）**一格没动**，两套状态用两个字段（`_lastChoiceTick` vs `_envPickShownSec`）。
- 每秒**只喂一次**（`sec != _envPickShownSec`）：`CardChoicePanel.CountdownTrigger` 在「一张都没选」时会 `Random.Range` 替他选一张（原版如此）⇒ 每帧都喂 = 每帧重掷。
- `< 3` 的自动确认由 `CardChoicePanel.CountdownTrigger` 自己做（原版 `ProcessChooseDone`），本批**没有**另写一份。

### 2·3 行数/落点纪律

- B-1 是**同一行替换**（行数不变）；B-2 全部在**类尾之后**追加。
- 现核：账里 `BattleDriver.cs` 在 **`:40635` 之后没有任何 `文件:行` 引用**（`grep -oP "BattleDriver\.cs:4[0-9]{4}"` 于 `账/*.tsv` 零命中）⇒ 类尾追加**零漂移**；`:14396` 附近（`143xx`/`144xx`）也**零落点**。
- `BattleDriver.cs` 现读 40,789 行、`\r\n` 40,789 / `\n` 40,789（纯 CRLF，没被翻）。

---

## 三、探针读数（引擎真跑，不跑 Unity）

**口**：仓库白名单里没有可改的探针，而现成探针**都不做换牌局**（`ruleprobe` 全目录只有 `BattleProbe.cs:203` 一处带 `openMulligan: true`，那是 `defence` 那一路；`PlayProbe` 的 `NewBattle` 不带这个参数 ⇒ `Mulligan`/`EndMulligan` **一次都不跑**）。
⇒ 用一个**仓库外**的一次性程序（`Stub.cs` + `RuleEngine/Core/*.cs` + 探针同款 `Data` 清单，`dotnet build` 3 秒）编两档：**当前工作树**（已修）与**只把 `Mulligan` 那三句回滚**的副本（改前）。

> 📁 复现路径（**不在仓库里**，随 Temp 清掉即可）：`C:/Users/qjh36/AppData/Local/Temp/wmull_probe/`（= 已修）· `…/wmull_probe_before/`（= 改前，`patch.py` 里有那三行的回滚）
> 跑法：`dotnet run -c Debug --no-build`（单局读数）/ `… -- rate`（200 个种子的命中率）。

**夹具**：牌库 `[督军, d00…d19, HAND_C, HAND_B, HAND_A]`、`shuffle: false`、`openMulligan: true` ⇒ 起手 `HAND_A,HAND_B,HAND_C`（3 张），牌库 20 张（督军在 `NewBattle` 里被提成 `Warlord`、**不在牌库里**），牌堆顶 = `d19`。换掉手牌下标 1、2（= `HAND_B` / `HAND_C`）。

| 读数 | 改前（回滚档） | 改后（工作树） |
|---|---|---|
| 换后手牌 | `HAND_A, d10, **HAND_B**` ← **换掉的牌被当次抽回来了** | `HAND_A, d19, d18` |
| 换后牌库（前 8） | `d09,d11,d00,d17,d07,d18,d14,d12,…`（洗过） | `HAND_C,HAND_B,d00,d01,d02,d03,d04,d05,…` |
| 牌库底部 3 | `d03, HAND_C, d15` | `d15, d16, d17` |
| **200 个种子、各换 2 张：换掉的牌被抽回的次数** | **45 / 200（22.5%）** | **0 / 200（0%）** |
| `EndMulligan` 洗了两副 | （该档**保留了**本批的 `EndMulligan` 洗牌 ⇒ 这两行**不是改前读数**） | ✅ 两副都变序、签名不变 |
| 再调一次 `EndMulligan` | （同上） | ✅ 不再洗（幂等） |

- **原版概率恒 0 的那件事，实测改前 22.5%、改后 0%** —— 这就是 §1·2 那条「`Deck.Add` 在镜像是加到牌堆顶」的直接后果。
- 改后单局逐条：换掉的 2 张**不在手牌**、补抽抽的是**原牌堆顶 d19/d18**、回库两张压在**牌堆底且次序 = 原版升序扫描的镜像（C 在最底）**、其余 18 张**相对次序逐张不变**（= 换牌期没洗）。

---

## 四、`play check` 指纹变化面与逐卡归因（⛔ 本代理**不重钉**任何基线）

跑 `bash d:/4/Unity/工具/ruleprobe.sh play check` ⇒ **rc=1**，但**与本批无关**：

- **差异面 = 1 张卡：`UM64|Angels of Death`**（主档 `playRc 18 → 0`、`played 0→1`、`energyDelta -6`…；事件电池同一张）。
- **归因 = 别人在飞的改动**：同一张卡在 `bash ruleprobe.sh check`（**解析层**）里也是唯一的差异行（`UM64|Angels of Death`），而 `RuleEngine/Core/EffectText.cs` 的 mtime = **12:26**（本批之前）且在工作树里是 `M` ⇒ 「解析器把 UM64 的正文解出来了 ⇒ 它现在打得出去」。
- **结构性证明本批不可能影响它**：`PlayProbe` 的 `NewBattle(...)`（`PlayProbe.cs:952`）**不带 `openMulligan`** ⇒ `RuleCore.Mulligan` / `EndMulligan` 在 `play all` 里**一次都不被调用**；本批对 `RuleCore.cs` 的改动**只有这两个方法内的语句**。
- 解析层哨兵另有一条红：`[输出指纹]` 现算 `f937e54d…` / 钉住 `ebdac779…` —— 同上，属解析层，本批未碰。
- ⇒ **对「不换牌的对局」结算层是零变化**；对**换牌局**的变化面就是 §三 那张表（换掉的牌从「可能被抽回」变成「绝不可能」，且洗牌点从 `Mulligan` 挪到 `EndMulligan`）。
  ⚠️ 提示调度台：判据文件 §二·A 说「会改 `play check` 指纹（预期）」—— 就本机现状**不成立**，因为全池夹具根本不进换牌阶段。真正会被改的是**带换牌的对局**（产品单机局 / 联机局 / `RuleEngineTest` 里那几条 `openMulligan`），不是 `play check` 的池扫。

`bash ruleprobe.sh defence`（**唯一**跑 `EndMulligan` 的现成入口）⇒ **④ 那一格过**（`练习（PracticeOffline 50）→ 「Firestrike Turrets」（期望落 UM 防御池 …）`）；它另有 **3 条红**（`有值的督军数不是 50` / `覆盖阵营数 13 ≠ 11` / ⑤ `EC 督军…没真值却发了卡`）—— 那三格走的都是 `NewBattle` 内那条路（**没有 `openMulligan`** ⇒ 不进 `EndMulligan`），本批的 diff 够不着，**是早就在的红**。

---

## 五、typecheck

`TMPDIR=/tmp/wf_wmull bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（加了新断言文件之后复跑，仍 0/0）。

---

## 六、断言（新增 `RuleEngine/Editor/RuleEngineTest_B117WMULL.cs`）

- 入口：`RuleEngineTest_B117WMULL.Run()`（**独立静态类，不改任何既有宿主**；收尾打 `[B117WMULL] ==== 合计：✓ N · ✗ M ====`）。
- **共 24 条**（A 组 14 · B 组 10）。
  - **A（14）**：A0/A0′/A0″ 前提（在换牌阶段 · 起手 3 张 · 牌库 20 张 · 起手 = 牌堆顶三张）· A1 换 2 张 · **A2 换掉的牌没被当次抽回** · A3 补抽拿走的是原牌堆顶两张 · **A4 回库在牌堆底且次序 = 原版镜像** · **A5 换牌期不洗（其余 18 张逐张不变）** · A6 阶段关闭 · A7/A8 两副签名不变 · **A9/A10 `EndMulligan` 洗了先手/后手两副** · A11 幂等（再调不再洗）。
  - **B（10）**：B1/B1′/B1″ 秒数换算（`CeilToInt`）· **B2 下界钳 0** · **B3 秒数变了才喂** · **B4 秒数没变不许喂**（否则每帧重掷选牌）· **B5 不是「选环境卡」那扇不许喂** · B6 面板没开不许喂 · B7/B8 `< 3` 自动确认的边界（喂 3 不确认、喂 2 确认）。
  - 灭自证：A2（`Insert(0,…)` 改回 `Deck.Add` ⇒ 红）· A4（收集循环改降序 ⇒ 红）· A5（把 `Shuffle` 加回 `Mulligan` ⇒ 红）· A9/A10（`EndMulligan` 不洗 ⇒ 红）· B4（去掉「秒数没变」闸 ⇒ 红）· B5（去掉 `isEnvPick` 闸 ⇒ 红）。
- ⚠️ **本批没有跑 Unity**（红线：子代理不许 `-executeMethod`）⇒ 这 24 条**一条都还没执行过**；绿红以调度台复跑为准：
  ```
  unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
    -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest_B117WMULL.Run -logFile -
  ```
- ⚠️ **B 组只钉到「纯函数那一层」**：真正的接线（主循环每帧调 `TickEnvPickCountdown` → 面板确认钮真的出现 `0:SS` → `< 3` 自动确认）要**真机/`BattleScene`** 才看得见（本批改前它**实机上根本不出现**，所以最直观的验收就是：任意一局单机、选进攻卡那扇面板出现后，确认钮上应出现倒计时并在剩 2 秒时自动确认）。

---

## 七、没查清 / 留给调度台

1. **`_SetupEnviromentalEffectPhase` 那次 `StopCoroutine → StartCoroutine` 是否「接着剩下的秒数走」** —— 判据文件 §四·1 已记「无实况可确认」。本批按「**不重置**」（= 用 `_mulliganLeft` 的剩余量）实现；若将来实况证明是重置，只需在 `TickEnvPickCountdown` 里把 `_mulliganLeft` 重新置成 `mulliganSeconds`（一行）。
2. **`EndMulligan` 里两副牌的洗牌先后**：原版 `.c:159-160` 的两个实参是 `playerGoesFirst` 与取反，而 `GetDeck(...)` 那一跳被反编译吃掉了（`ShuffleDeck` 的 `.c` 第二参直接当 `List` 用）⇒ 我按「**先手那副先洗**」实现并在注释里写明这一跳读不到。两副各自都是均匀排列，顺序只影响「各吃哪几个随机数」。
3. **`play check` / `check` 那 2 条红**（UM64 解析差异 + `[输出指纹]`）不是本批的，**别在本批的账上记**；也**没有重钉**。
