# W · 批次 A —— `BattleScene` 7 红 ＋ `RuleEngineTest` 5 红（写手回执）

> 写手：`W_red_A`（2026-10-18）。诊断依据 = `资料/普查产出_1018/DIAG_red_A_RuleEngine_Battle.md`（本报告简称「诊断」）。
> **没跑 Unity / 没跑任何 `*.Run` / 没碰 git 写操作 / 没改正本。**
> 类型检查：`TMPDIR=/tmp/wf_redA bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 · 编辑器 0**（改完即跑，一次通过）。
> 行尾：三个文件改完都**没翻**（`Label.cs` / `RuleEngineTest.cs` 仍全 CRLF；`CreatePool.cs` 仍全 LF；`git diff --numstat` 13/0 · 69/17 · 11/6）。

---

## §一 逐条表

| # | 红（诊断编号） | 改了什么（文件:行） | 依据 | typecheck |
|---|---|---|---|---|
| A1–A7 | Battle 7 颗「折行 = 原版 0（实得 1）」：`TitleText_Me` · `TitleText_Foe` · `EnemyPlateText` · `PlayerPlateText` · A1192 `NameHolder/Name Text` · `TitleHolder/Title Text` · `Alliance/Alliance Name`（诊断 #6–#12） | `CardPresentation/Battle/Label.cs:1238` 记 `int wrap = WrappingMode`；`:1243` 重放后 `if (wrap >= 0) SetWrappingMode(wrap)`（整段在 `ReplayFitForFace` `:1224-1244`） | 诊断 §一·6)–12) 的根因链：`SetTextTmp:1433` → `ReplayFitForFace` → `SetAutoFitBox` → `SetWrapWidth` **无条件**写 `Normal(1)`；调用方的 `SetWrappingMode(0)` 在灌文本**之前**。`SetWrappingMode` 自带「值没变就早退 + `ForceRelayout()`」（`Label.cs:699-722`），还原后版面会推下去 | 0/0 |
| B1 | 中文名查不回 1 张（`RuleEngineTest.cs` 中文索引不变量） | `RuleEngineTest.cs:19569` 新增 `nonCjk` 计数；`:19580-19582` 过滤条件加「**含汉字**」（`0x4E00–0x9FFF`，与 `CreatePool.IsCjk` 同段）；`:19598` **新增断言** `Check(nonCjk, 1, …)`；`:19592-19594` 断言文案改成「有中文名（`nameZh` 含汉字）」 | 诊断 §一·3)：`AM24 Bullgryn Bone'ead` 的 `nameZh` 就是英文（`gen_cards_engine.py:1624-1641` 裸字符串分支 + `579bcad` 提交说明）；索引侧 `CreatePool.cs:741` 的 `HasCjk` 闸正确。**本代理独立复算**：`cards_engine.json` 1127 张、有 `nameZh` 1127 张、**非汉字恰 1 张（AM24）** | 0/0 |
| B2 | 被点到的卡数 66→67（`:19369`）· 总处数 127→128（`:19376`） | 两个期望值各 +1；两处文案补「2026-10-18 再 +1：`GOF12 Ork Spanner`→`Spanner`，`GOF15 Burna Boy` 原文 `a Spanner` 首次命中」 | 诊断 §一·4)5)：HEAD 复算 `128/67`、上一版 `127/66`，**差集只有 `GOF12`** | 0/0 |
| B2·注 | 同块上方 **成因注释写错**（原文把 `-1` 归给 `S13`） | `RuleEngineTest.cs:19346-19356` 整段重写（铁律 5 留痕：「原来写 X（S13 68→67·129→128），实际是 Y（S13 已在 66/127 基线里；68/129 是 HEAD 停掉裸写半段的 OFF 读数；真成因 = GOF12 改名），错因 Z（把上一版基线当成未扣 S13 的原值）」） | 诊断 §一·4)5) 的「⚠️ 铁律 5」那一节 | 0/0 |
| C1/C2 | `整局里部署触发段真的跑过` · `整局里真的筛中了 Drone`（`RuleEngineTest.cs:8630/8631`，诊断 #1/#2） | `RuleEngineTest.cs:8588-8614`：在 `RuleCore.BeginTurn` 之后、`PlayAiTurn` 之前加一段夹具 —— 若**该行动方名下**已有 `Trigger=="deploy"` 常驻效果、且本局还没出现过 `部署触发段：`、且手里有 Drone、有空格、付得起 ⇒ 直接 `RuleCore.PlayCard` 打一张（走真路径，`ResolveDeploy` 记 `fired`/`granted`）。每局只补一次（`firedYet` 闸） | 诊断 §一·1)2) 的**方案 (a)**；⛔ 未采用 (b) 换种子（会把同一颗雷推给下一次数据改动）；⛔ 没删断言、没改引擎 | 0/0 |
| D | `CreatePool.cs` 英文侧那份 doc 的数值与复算不符 | `RuleEngine/Core/CreatePool.cs:840-850` 重写该 bullet（铁律 5 留痕）：HEAD 现算 **ON `128/67` · OFF `129/68`**；上一版（`4995622`）**ON `127/66` · OFF `128/67`**；原文 `125/64` 与两版都对不上 ⇒ **判不出**它测的是哪一版（只确认原文「OFF 退回 128/67」= 上一版快照的 OFF） | 诊断 §一·4)5) ＋ §三·3 | 0/0 |
| 顺手 | 同一句话在测试文件里还有 2 处过期数字（`127/66` · `125/64`） | `RuleEngineTest.cs:19142-19144`（②·b 段）· `:19397-19398`（⑤·b 段）各加 3 行「2026-10-18 就地订正」，**只加更正、不删原文** | 铁律 5（同一句话被复制到别处要一起改）＋ 诊断 §三·3 | 0/0 |

**在本次 12 条之外、但同文件同源的一处，仍在（见 §四·2）：`CreatePool.cs:851` 的「差值是 `+7`（`125 → 132`）」仍拿 125 当英文基线 —— 没有复算判据，**没改**，只记。**

---

## §二 关键 diff 说明

### ① `Label.ReplayFitForFace`（唯一实现改动）

```csharp
int wrap = WrappingMode;          // 重放【前】记下调用方此刻的档（-1 = 点阵后端）
_fitValid = false;
_fitReplaying = true;
try { SetAutoFitBox(w, h, lo, hi, bc); }
finally { _fitReplaying = false; }
if (wrap >= 0) SetWrappingMode(wrap);   // 重放经 SetWrapWidth 开回 Normal ⇒ 这里还原
```

- **为什么是这一处**：诊断实测「7 颗红的形态完全同构 = 建时 `face: LatinUi` + 初始空串 + 之后灌汉字才翻面」。翻面那一刻 `SetTextTmp`（`:1433`）才调 `ReplayFitForFace`，**只有这条路**会在「调用方已设好折行」之后把模式改回 1。
- **为什么不动 `SetAutoFitBox`**：既有调用点依赖「它之后折行 = 1」（诊断 §一·6–12 明写，且 `SetWrapWidth` 的 doc 把这条当契约）。
- **为什么不在这 7 个调用点补 `SetWrappingMode(0)`**：文本是**运行期**灌的（`BattleDriver._myText.SetText` / `AlliancePanelWindow.Apply`），补在建字那一刻之后无效 —— 这正是 `4995622` 漏掉的那一跳。
- **副作用边界**：`SetWrappingMode` 值没变就早退 ⇒ 原本折行=1 的标签（`MatchSkullsScore` / `*FaithText` / `*SpiritStoneText`）**一个字节都不动**；第三档 `3`（输入框）也照原值还原。`ForceRelayout()` 只做 `SetFontSize(现值)`（早退）+ `RefreshBounds()`，不会反过来清 `_fitValid`（`_fitValid` 只在 `SetWrapWidth:522` 与 `ReplayFitForFace:1239` 置 false、`SetAutoFitBox:1215` 置 true）。
- **`WrappingMode` 读在 `_fitValid = false` 之前**（顺序重要：`SetAutoFitBox` 一路会把 `_fitValid` 置回 true，但那时候折行已经被开成 1 了）。

### ② 部署触发段夹具（`RuleEngineTest.TestDeployBuffAndCostMore` ③ 段）

- 触发条件（全部满足才补打）：`ctx.PersistentEffects` 里存在 `Trigger=="deploy" && Owner==ctx.Active`；本局 `ctx.Events` 里还没有 `部署触发段：`；手里有 `IsUnit && Subtype=="Drone"`；`FirstFreeSlot(ps)`（本文件 `:24270` 那个本地助手）≥ 0；`RuleCore.CanPlayCard(...) == OK`。
- **打的是真路径 `RuleCore.PlayCard`** ⇒ 走 `ResolveDeploy` ⇒ 日志落 `—— 部署触发段：「Experimental Drones」盯上 …` ⇒ 既有的两个计数器（`fired` / `granted`）原样 +1，断言一个字没改。
- **位置偏离了诊断的字面建议（「AI 回合之后」）**：改在 `BeginTurn` 之后、`PlayAiTurn` 之前。原因是能量 —— `BeginTurn` 刚做 `p.Energy = p.MaxEnergy`（`RuleCore.cs:1078`），而 `PlayAiTurn` 会把能量花到付不起为止 ⇒ 「AI 回合之后」补打大概率吃 `ErrCost`，**保证不了**。已写进注释。
- **每局只补一次**：`firedYet` 闸看 `ctx.Events` 里有没有 `部署触发段：`。

### ③ 中文名断言

- 过滤谓词从「`nameZh` 非空」改成「`nameZh` 含汉字」——与 `CreatePool.IsCjk`（`U+4E00–U+9FFF`）同段（测试侧不能调它，它是 `private`，所以就地写了同一段判断，注释写了出处）。
- 新增「非汉字 `nameZh` = 1 张」的钉子：将来数据/生成器再冒出一张英文原文会红；有人把过滤谓词改回「只判非空」也会先在这里红。

---

## §三 同步点要跑的自检

**`RuleEngineTest.Run` ＋ `BattleScene.Run`**（两条都必须跑；本次改动跨了两个宿主，且 `RuleEngineTest` 里同时动了引擎侧数据断言与夹具）。

判绿时请顺带看这三行/两类断言：

1. `ruleengine.log` 里那条实战统计 `实战：4 局里 … 被登记 N 次 · 部署触发段跑了 M 次 · 筛中 Drone K 次` —— 期望 **M > 0 且 K > 0**（改动前是 `2 / 0 / 0`）。
2. `RuleEngineTest` 的三条期望值：被点到卡数 **67** · 总处数 **128** · 非汉字 `nameZh` **1**。
3. `battle.log` 里 7 颗折行断言：`TitleText_Me` / `TitleText_Foe` / `EnemyPlateText` / `PlayerPlateText` / A1192 那 3 颗 —— 期望全部 `WrappingMode == 0`。

⚠️ 若 `RuleEngineTest.Run` 的 `fired` 仍是 0：先看 `ctx.Events` 的 2000 条上限（`BattleContext.cs:534`，`Log` 会丢最旧的）—— 那说明是**计数器被冲刷**，不是夹具没打出去（见 §四·3）。

---

## §四 没查清 / 顺手发现

1. **路径笔误（已按实际文件改）**：任务书 ④ 写的是 `RuleEngine/Data/CreatePool.cs`，**全仓只有 `RuleEngine/Core/CreatePool.cs`**（`glob` 实证），诊断里指的也是 `Core/`。已改 `Core/CreatePool.cs`。
2. **`CreatePool.cs:851` 那条「差值是 `+7`，不是 `+4`（`125 → 132`）」仍是旧基线**：本代理**没有**复算「中文档 = 英文 + 7」这条关系（诊断也没给），⇒ 只记不改，**判不出**现在应是 `128 → 135` 还是别的关系。下一轮要动它时请先按 `max=999` 那套口径重算。
3. **夹具的计数器有被冲刷的潜在风险（本来就有，不是这次引入的）**：`BattleContext.Events` 上限 2000（`BattleContext.cs:534`），而注册/触发那几行是**整局结束后**才回读的。本次观察到的对局长度下没被冲掉（上一轮绿的时候 `4/5` 都读得到），但若同步点跑出来 `registered` 突然变小，先怀疑这条。**没改**（不在本批范围，且改它要动引擎）。
4. **`fired > 0` 没有实跑验证**（本代理不许跑 Unity）：逻辑上「登记后的下一个自己的回合、能量刚满、只要场上有空位就会打」；若那局到终局都没空位，仍可能不触发 —— 但 4 局里只要一局成立即可。
5. **`Label` 侧同型风险没普查**（诊断 §三·4 已如实写）：`DeckScene` / `ShellScene` / `CollectionScene` 里「拉丁面 + 空串 + 后灌汉字 + 期望折行 0」的标签是否有同型红，**本批没跑、没查**。修法在公共件里，若有会一起被修掉（`ReplayFitForFace` 只在翻面时走）。
6. **`SetWrappingMode` 在 `ReplayFitForFace` 里会让 `ForceRelayout()` → `RefreshBounds()` 再挪一次 TMP 子节点**（它的 doc 说「挪过之后要重新对齐」）。这与本来的 `ReplayFitForFace` 已做的事同类（`SetAutoFitBox` 尾也 `RefreshBounds`），且 `SetTextTmp` 末尾还有一次 `RefreshBounds`，**没有新风险**；若同步点出现「字位置差几像素」的断言红，先看这一条。
