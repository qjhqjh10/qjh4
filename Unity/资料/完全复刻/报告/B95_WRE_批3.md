# B95_WRE 批 3 — Rally 闸次序重排（`RuleCore.cs`）＋ 残骸 `DeployedTurn`

> 依据 = `判据/B95_Rally闸_核查.md` §八（`B95_A3` 产出，调度台已核）· CLAUDE.md 18·0（原版有判据 ⇒ 照原版）。
> **本批只做三件事**：① `RuleCore.PlayCardCore` 的触发次序重排（方案 (a)）· ② 四处 🔶 自设计注释订正 · ③ 残骸初始化器补 `DeployedTurn`。
> ⛔ 没顺手改别的。没跑 Unity / ruleprobe，没动 git，没碰账主表 / `Editor/RuleEngineTest.cs`。`CardDef.cs` 只改一行注释（调度台本批授权）。

---

## 一、重排后**实际的执行次序**（`RuleCore.PlayCardCore`，按 `RuleCore.cs` 行号；只列可执行语句）

| 行 | 语句 | 对应原版拍 |
|---|---|---|
| `:4087` | `BroadcastWhen(WhenEventKind.Play, …)` | （文字通道「当你打出一张牌」，不在 §八·1 那张表里，本批**没动**） |
| `:4095` | `Enchantments.OnCarrierPlayed` ＋ `AttachGrantedByPlayedCard` | （附魔随牌走，本批没动） |
| **`:4104` 前段** | **`FireTriggerOnBoard(… Rally …)`** | **1 · `trigger 0`**（`:1081` → `ResolveCardPlayed.c:22-24` → `OnCardPlayedWithTarget.c:43`） |
| **`:4104` 中段** | **`ResolveSpiritAbility(ctx, p, unit)`** | **2 · `600 UseSpiritStone`**（同函数 `:66`） |
| **`:4104` 后段** | `ctx.NotePlayed` ＋ `ApplyPlayedCardTailEffects` | **3 · `590 Combo` ＋ 打出时摘 ephemeral**（`ResolveCardPlayed.c:25-31`） |
| `:4125-4126` | `PreCallBoardListener(… selfOnlyOnly: true)` | **4 · `195 ThisUnitSummoned`**（`ResolveUnitSummoned.c:47`） |
| `:4139` | `SpawnTideCopies`（潮涌） | （不在 §八·1 表里 —— 见 §一·残余） |
| `:4146` | `PlayCompanions`（伴生） | （不在 §八·1 表里 —— 见 §一·残余） |
| **`:4213` 首句** | **`ResolveFaithAbility(ctx, p, unit)`** | **5 · `605 UseFaith`**（`ResolveUnitSummoned.c:51`） |
| **`:4213` 第 2 句** | **`BroadcastWhen(WhenEventKind.Deploy, …)`** | **6 · `190` 的文字通道**（广播轮 `:1426` → `BroadcastCardPlayed` → `ResolveUnitSummoned.c:32`） |
| **`:4213` 第 3 句** | **`ResolveDeploy(ctx, p, unit)`** | **6/7 · `190`＋`550` 的常驻作业**（同文件 `:32` / `:66` → `:1426`） |
| **`:4213` 第 4 句** | **`FireTriggerOnSide(… Uprising …)`** | **8 · `680 Uprising`**（`ResolveUnitSummoned.c:98`） |
| `:4228-4233` | `Teleport` 支 | （不在 §八·1 表里，本批**没动**） |

⇒ 与原版那张表逐拍对齐：**`Rally(0)` → `600` → `590` → `195` → `605` → `190/550/680`**。

### 一·残余（如实标，**本批没动**）
- `195`(`:4125`) 与 `605`(`:4213`) 之间仍夹着 `SpawnTideCopies`(`:4139`) 与 `PlayCompanions`(`:4146`)。**这两句不在原版 8 拍表里**，与 605 的先后**无据可查** ⇒ 不改（不发明口径）。方案 (a) 原本的诉求「195 → 605 紧邻」在「触发句之间没有别的**触发**」这层意义上成立，但源码行间确实还夹着这两句。
- `BroadcastWhen(WhenEventKind.Play, …)`(`:4087`) 排在最前，早于 Rally —— 它不在 8 拍表里，属「文字通道」，本批不动。

---

## 二、四处 🔶 自设计注释的订正

| 位置 | 原来写的 | 改成 |
|---|---|---|
| `RuleCore.cs:4131-4132`（`ResolveDeploy` 上方） | 「⚠️ 排在 `Rally` **之前**：这样 Rally 结算时看得见刚给出的关键词。**这一条是我们挑的**（原版这一段的先后无据可查）」 | 「**已推翻（18·0）**：原版**有**判据 —— `ResolveDeploy` 属最后的广播轮（190/550/680，`:1426` → `ResolveUnitSummoned.c:32/66/98`）⇒ 已下移」 |
| `RuleCore.cs:4175-4176`（`ResolveSpiritAbility` 上方） | 「⚠️ **排在 `Rally` 之前**…⚠️ **这个先后是我们挑的**」 | 「**已推翻**：原版 600 **紧跟 trigger 0**（同一函数 `:43` → `:66`）⇒ 已上移到 `:4104`」 |
| `RuleCore.cs:4221-4222`（`Teleport` 上方） | 「⚠️ 位置排在 `Rally` 之后…先后**无据可查** ⇒ **我们挑的**」 | 见下（**这一格我改了措辞，与调度台给的原话不同，请看理由**） |
| `CardDef.cs:8091`（`Uprising` 的时机点说明） | 「时机点在 `RuleCore.PlayCard`（`ResolveDeploy` 之后、`Rally` 之前）」 | 「已作废；按原版次序本跳现在排在**最后的广播轮**（605 之后，与 190/550 同轮）」 |

🔴 **`:4221` 我没有照抄调度台给的「已按原版次序（`:1081` Rally → `:1426` 广播轮）」**，理由：`Teleport` **本身**在 §八·1 那张 8 拍表里**没有对应拍** ⇒
「它与 605 / 广播轮的相对位置」仍然**无据可查**。照抄那句话等于把「我们挑的」伪装成「原版的」。
所以我写的是：**上半句（Rally 在它之前）已不再是我们的选择**（那是把 Rally 上移的原版后果）；**但 `Teleport` 与 605/广播轮的相对位置仍是 🔶 自己设计**。
⇒ 若调度台认为应当把 `Teleport` 也当「已知」处理，需要先给判据，我不发明。

---

## 三、断言（新建文件的 `B95WRECaseRallyBeforeDeployBroadcast`）

`Assets/RuleEngine/Editor/RuleEngineTest_EngineB95WRE.cs`（**接线归调度台**：`RuleEngineTest.cs` 的 `Run()` 加一行 `Step(CheckB95WREAttackerPlumbing);` —— 三条用例共用这一个入口）。

做法 = 按源码扫描两个锚点的字符序（`Application.dataPath + "/RuleEngine/Core/RuleCore.cs"`）：

```csharp
const string rallyAnchor  = "FireTriggerOnBoard(ctx, unit, KeywordTable.Rally, chosen)";
const string deployAnchor = "BroadcastWhen(ctx, WhenEventKind.Deploy, p, unit.Card, unit, preCalled: unit)";
```

🔴 **扫的是【完整调用串】，不是关键词** —— 重排后源码里留了**注释存根**（`FireTriggerOnBoard(Rally)` / `BroadcastWhen(Deploy)`，不带实参），
按关键词找会**先找到注释**、断言就变成永真。前提硬检查（都先断前提再断结论，⛔ 没有 `if (找到) Check(…)`）：
① 两个锚点都 `IndexOf >= 0`；② 两者不是同一处；③ 每个锚点全文**只出现一次**（注释存根一旦带上实参，这条就红）。
**已离线预验**：`rally count 1 / deploy count 1 / rally before deploy: True`。

🧨 什么改动会让它红：把 `:4104` 前三句恢复成老次序，或把 Deploy 广播挪回 Rally 之前；或给注释存根补上实参（前提红）。

---

## 四、行号对照

**`RuleCore.cs`：17,139 → 17,139，一行没变。**
本批所有改动都是**在同一行内追加文本**，或在原位置把「语句行」换成「注释行」（**等量替换，行数守恒**）。
机检佐证：`check_ledger_points.py 战斗逻辑驱动.tsv` ⇒ **A 998 条 · 通过 998 · 漂移 0 · 失败 0**。

| 行 | 改前 | 改后 |
|---|---|---|
| `4104` | `ctx.NotePlayed(…); ApplyPlayedCardTailEffects(…);` | 同一行**前插** `FireTriggerOnBoard(Rally); ResolveSpiritAbility(600);` |
| `4127` | `BroadcastWhen(ctx, WhenEventKind.Deploy, …);` | **注释存根**（已下移） |
| `4131-4132` | 两行 🔶 自设计注释 | 两行订正注释（**行数不变**） |
| `4133` | `ResolveDeploy(ctx, p, unit);` | **注释存根**（已下移） |
| `4156` | `FireTriggerOnSide(… Uprising …);` | **注释存根**（已下移） |
| `4175-4176` | 两行 🔶 自设计注释 | 两行订正注释 |
| `4183` | `ResolveSpiritAbility(ctx, p, unit);` | **注释存根**（已上移） |
| `4211` | `FireTriggerOnBoard(… Rally …);` | **注释存根**（已上移） |
| `4213` | `ResolveFaithAbility(ctx, p, unit);` | 同一行**后插**三句（Deploy 广播 / ResolveDeploy / Uprising） |
| `4221-4222` | 两行 🔶 自设计注释 | 两行订正注释 |
| `9871`（残骸） | `PlayOrder = u.PlayOrder,` | 同一行追加 `DeployedTurn = u.DeployedTurn,` |
| `CardDef.cs:8091` | 一行注释 | 一行订正注释 |

⇒ **345 / 461 个 `RuleCore.cs` 落点一条都不用改**（含区内那 3 条：`:4104` / `:4186` / `:4232` —— 它们所在的行**内容仍然成立**，`:4104` 只是前面多了两句、`:4186` 与 `:4232` 未动）。

---

## 五、预期 `play check` 指纹变化面

**会变，且我预计变化面 = 最多 53 张 Rally 卡**（判据 §八·4 的现读上界）：
次序差异**看得见**的条件 = 「被打出的那张自己有 Rally 正文、且正文读的属性/关键词正好被场上某个 190 观察者改了」。
- 观察者是 `BL30`(给 troop 随机 Dark Pact) / `TAU34`(给 troop +2 Health) 这类「对**任意 troop** 生效」时，池内带 Rally 的 troop = **85**，其中正文读属性/关键词的 = **53**。
- Vehicle 观察者（`AM7` / `UM12` / `ASH66`）覆盖 18 张 Rally 卡里的 13 张（含在 53 里）。
- Stormboy / Drone / Destroyer 三族**池内没有带 Rally 的卡** ⇒ 零可观测。
- `BL30` / `TAU25` 同时是「Rally 卡」与「190 观察者」，但 190 是 `op_Inequality` ⇒ **无自环**。

⚠️ **但我无法实测**（⛔ 不跑 ruleprobe）⇒ 这只是**上界**，真实变化面取决于这 53 张在实际对局里是否同时满足「观察者在场」。**同步点必须单开一处 A/B 隔离归因**。

### 我担心可能**超出** 53 张的两处（如实体检）
1. **`ResolveDeploy` 下移**：它扫的是「常驻效果盯着某类牌」。下移之后它排在 `SpawnTideCopies` / `PlayCompanions` **之后** ⇒ 若潮涌复制/伴生**新部署**了单位，`ResolveDeploy` 现在会**多看见**它们（改前看不见）。这条**不属于「Rally 卡」，可能是新的一族**。判据上原版确实是「广播轮在最后」⇒ 方向对，但变化面要单独量。
2. **`Uprising`(680) 下移**：它的闸是 `playedIntoFieldTurnCounter == turnCounter`。下移只改「何时问」，不改闸 ⇒ 理论上等价；但若 `SpawnTideCopies` 会重置某些计数器，就可能不等价 —— **未核实**。

---

## 六、残骸 `DeployedTurn`（**独立于次序重排，请单独归因**）

- 全仓 `grep "IsRemnant = true"`：**只有一处**造 `UnitState` 残骸 = `RuleCore.cs:9861-9875`（`CleanupDeaths` 的翻面支）。
  另一处 `RuleCore.cs:9794` 是**进墓地的那条记录**（`DeadUnit`，不是 `UnitState`）⇒ **不需要**这一项。
- 改动：`RuleCore.cs:9871` 的初始化器追加 `DeployedTurn = u.DeployedTurn,`（行内追加，**零行数变化**）。
- 判据：原版残骸是**同一个 `CardScript` 对象**（`TransformIntoRemnant` 只改状态位），`turnPlayed(+0x30c)` **原样保留**，`ResetToValuesInHand.c` 也不碰它。
- 后果（改前）：残骸 `DeployedTurn` 回落 `-1` ⇒「本回合上场 → 变残骸 → 复活回单位」这一族原版 `IsTheSameTurnPlayed` 为真、我们为假 ⇒ 誓约能力被 `RuleCore.cs:11752` 的 `u.DeployedTurn != ctx.Turn` 挡掉。
- ⛔ **没动默认值**：`UnitState.DeployedTurn = -1` 保持原样（「只搬不清」）。
- 账：`CardScript.TransformIntoRemnant()`（账行 939）的「（等价）」一句已就地订正。

---

## 七、补丁 & typecheck

- 账补丁：`账/_parts/B95_WRE.tsv` —— **17 行**（批 1 的 13 ＋ 批 2 的 658 ＝ 14，本批新增 869 / 939 / 1264 三行）。
  `merge_ledger_parts.py --dry` ⇒ **更新 17 · 新增 0 · 无变化 0**，无列数错、无签名拒收。
- typecheck：`TMPDIR=/tmp/wf_WRE3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
