# `B8_BM` —— 强制攻击「退还额度」数据化 · `RuleEngineTest` 17 红处置

> 输入：`资料/完全复刻/报告/B8_BK_17红诊断.md`（诊断正本，**未重查**）· `_tmp_view/wf_rule_0826.log`
> 产出：9 条 γ（改代码）· 3+1 条 δ（夹具）· 5 条 α（断言过期）· 卡表 v31→v32 · 一条账补丁
> 门禁：`typecheck` **0 / 0** · `ruleprobe check` **rc=0 失败 0** · `play check` **rc=0** · `check_ledger_points` **漂移 12（全是旧账）/ 失败 0**
> ⛔ 本报告**没有跑 Unity**（红线：`RuleEngineTest.Run` 由调度台跑）⇒ 所有「改完会绿」都是**按代码路径推的**

---

## ① 任务 1：退还判据改成数据驱动（9 条 γ 的根因）

### 1·1 落地形状（四步，全部落盘）

1. **数据侧** —— `工具/gen_cards_engine.py` 新增 `chosen_abilities_of()`，逐卡落 **`chosenAbility`** 列
   （= `cardAbilities[].abilityLogic[].chosenAbility` ∪ `activeAbility.activeAbilityLogic[]` ∪
   `alternativeAbility.activeAbilityLogic[]` 的**去重升序**表，形状与 `abilityTriggers` 同口径）。
   - 真值里没这张卡 ⇒ **不落列**（未知，⛔ 不是空表）；坏值跳过并出声（`CHOSEN_ABILITY_REPORT["bad"]`）。
   - **bump `version` 31 → 32**（生成器 return 里那段 v32 注释写明了判据、消费端、缺列后果）。
   - 跑完自检读数（生成器现打印）：
     `chosenAbility` 落列 **1128/1128** 张 · 缺列 0 · **退还族 {231,237,238} = 9 张**
     （`BL64` `DA25` `EC12` `EC46` `SAU70` `SW9` `SW5` `SW29` `EC22`）·
     **不退还族 {230,233,235,236} = 3 张**（`SAU33` `GOF93` `GOF86`）· 坏值 0 处。
   - `--write` 落盘后再 `--check` = **一致 ✅**（幂等）。文件 **3,328 KB**（v31 时 3,289 KB 量级，只多这一列）。

2. **判据口** —— 新增 `RuleEngine/Core/ForcedAttackRefund.cs`（`public static partial class`）：三态
   `Unknown / Refund / NoRefund`；家族集合 **只此一处**（`{231,237,238}` 退 / `{230,233,235,236}` 不退）；
   `Install(卡id, int[] chosenAbility)` 负责判定，`Of(卡id)` 是唯一查口。
   - 🔴 **`Unknown` 的下游语义 = 退回旧行为（退还）** —— 这是**故意**的，理由见 §5·①②。

3. **装载** —— 新增 `RuleEngine/Data/ForcedAttackRefundMap.cs`（**同一个 partial class 的另一半**，
   照 `Data/RelatedCardMap.cs` / `Data/TutorialDatabase.cs` 的先例）：`Resources.Load<TextAsset>(CardDatabase.ResourcePath)`
   + `JsonUtility` 解析**瘦 DTO**（只要 `id` + `chosenAbility`）→ `Install`。
   - **只试一次**、懒加载（第一次 `Of()` 才发生）；载不到/解析失败/`version < 32` 一律**出声**并留空表。
   - 🔴 写表用**合并**（`EnsureMap()`），因为自检夹具可能先写过 `SetForTest` 条目（同 `RelatedCardMap` 那条教训）。

4. **引擎侧** —— `RuleCore.cs` 三处（**一个字节的行数都没动**，见 §3）：
   - `DeclareAttackRefundingForced`（`:14102-14119`）的退还条件：
     `if (code == RuleCodes.OK && wasFree)` → `if (code == RuleCodes.OK && wasFree && ForcedAttackRefunds(ctx))`。
   - **类尾新增** `ForcedAttackRefunds(ctx)`（`:14206-14223`）：能力属主 =
     `ctx.FactionSourceCard`（常驻作业/手牌陷阱）→ `ctx.ActingUnit.Card`（单位自己的能力，`ResolveOne` 逐 op save/restore）
     → `ctx.CardPlaySource`（战术卡整条结算 / 事件型常驻效果）；⛔ **不用** `ctx.PlayingCard`（从不清空）。
   - 就地订正 `:14087-14096` 那段错注释（铁律 5）：删掉「池内每一张强制攻击卡都退还」+ 补上漏掉的 `0xeb`/`0xec`。

### 1·2 读数（**推导值，未实测** —— ⛔ 本笔不能跑 Unity）

| 卡 | `chosenAbility` | 改**前**（一刀切退还） | 改**后** | 判据 |
|---|---|---|---|---|
| `GOF86 Unleashed Tramplasquig` | 235 | `AttacksThisTurn` 1→**0** · `Exhausted` true→**false** · 日志有「重置行动：1 → 0」 | `AttacksThisTurn` = **1** · `Exhausted` = **true** · **没有**那条日志 | `PlayAbility.c:2701-2713` 走 `AddScriptedAttack`，签名/方法体**没有** `dontCountAttack` |
| `GOF93 Hunta Rig` | 235（另有 250/320/380） | 同上 | 同上 | 同上 |
| `SAU33 Vargard Obyron` | 236 | 同上 | 同上 | `PlayAbility.c:2714-2745` 走 `AddRedirectedAttack`（`dump.cs:32505`） |
| `Dust Storm`（`ef42ad6c…`） | 236 | 同上 | **池外卡，列里没有它 ⇒ `Unknown` ⇒ 仍退还** | ⚠️ 见 §5·④ |
| `BL64 Murderous Desires` | 231 | `AttacksThisTurn` 1→0 · `Exhausted` **false** | **不变**（仍退还） | `PlayAbility.c:2666-2686` 传 `AddForceAttack(…, 1, 0)` |
| `DA25 Deathwing Knight` | 231 | 同上 | 不变 | 同上 |
| `EC46 Peerless Bladesmen` | 237 | 同上 | 不变 | `PlayAbility.c:2752-2766` `AddScriptedForceAttack(…, 1, 0)` |
| `EC12` `EC22` `SAU70` `SW29` `SW5` `SW9` | 238 | 同上 | 不变 | `PlayAbility.c:2767-2780` 同上 |
| `SW56 Let Loose` | **30/260/320（无攻击族）** | 退还 | **仍退还**（`Unknown` 回退档） | ⚠️ 见 §5·① |
| `Let Loose` 附魔 `f16df0b3…` | 238 | 退还 | **池外卡 ⇒ `Unknown` ⇒ 仍退还** | 方向上正确（原版也退还），但**靠的是回退档** |

**唯一现成的读数口** = `RefundForcedAttackAction` 那条 `ctx.Log("… 重置行动：本回合攻击次数 X → Y …")`
（`AttacksThisTurn`/`Exhausted` 都不进 `ruleprobe` 任何指纹）。

`RuleEngineTest` 侧的可观察差异（这就是 9 条 γ）：`A1606` 夹具①的「前提」格
`CanAttackNow` 由 `OK(0)` 回到 `ErrExhausted(6)`；「疲劳」格 `sq.Exhausted` 由 false 回到 true；
「配额」格由 `OK(0)` 回到 `ErrExhausted(6)`；`A1632` 的灭自证同。

---

## ② 任务 2（δ 夹具 3 条 + 顺手 1 条）与任务 3（α 断言 5 条）改了哪几行

### 2·1 δ —— 残骸夹具给阵营（`DestroyRemnants` 已按阵营判，⛔ 没动它）

| 处 | 原 | 现 | 行（改后） |
|---|---|---|---|
| `TestFactionResources` ④b | `Unit("SauRem", …, "Remnant")`（阵营 `Test`） | `UnitFaction("SauRem", 1,1,1, "Sautekh", "Remnant")` | `RuleEngineTest.cs:4178` |
| `TestRemnant` ①～④ 的卡模板 | `new CardDef("FixtureRemnant", …, "common", "Test", …)` | 阵营 `"Sautekh"` | `:16030` |
| `TestAuraSettle` ⑦ | `Unit("RemNear"/"RemFar", …, "Remnant")` | `UnitFaction(…, "Sautekh", "Remnant")` | `:18641-18642` |
| 🆕 **顺手（铁律 14①）** `TestFactionResources` ④c | `Unit("SauRemF", …, "Remnant")` | `UnitFaction(…, "Sautekh", "Remnant")` | `:4216` |

- ④b/⑦ 的注释里写清了「为什么必须是 `Sautekh`」（`IsNecronRemnant` ← `ShouldRemnantDestroyOnTurnEnd.c:1-27`）。
- ⚠️ 改完 `:18608`（「被罩住的残骸留场」）**不再是空转绿**：这一趟现在真会扫到两具残骸，
  被 `Nemesor Zahndrekh` 罩住的那具留场、另一具被摧毁 ⇒ 正反两条都有判别力。
- 顺手那一条（④c）原本也是**空转绿**（阵营 `Test` ⇒ 摧毁那一趟根本不扫它，「翻回来之后扫不到」恒成立）；
  改成 `Sautekh` 之后它会真的走「先 reanimate、后摧毁」那一趟。判据同 ④b（已查清）· 改动就在本文件 · typecheck 过 ⇒ 按铁律 14① 顺手做。
- ⛔ **没改** `TestFactionResources` ④c 上面那一处 `Unit("SauOnly", …, "Remnant")`（`:4159`）：
  它断言的是「死灵残骸**不是**可收集的路标石」，换成 `Sautekh` 会让它先被 `EndTurn` 摧毁
  （`ErrEmptySlot` ≠ `ErrNotWaystone`）⇒ 需要重排断言，**不在本笔**。已记 §5·⑤。

### 2·2 α —— 5 条断言过期（每条都写了「期望值怎么来的」，⛔ 没有按实得值反填）

| 处 | 旧期望 | 新期望 + 灭自证 | 行（改后） |
|---|---|---|---|
| `TestA4Batch2ForceAttack` ①（`T_MurderousDesires`，= 231） | `wounded.Exhausted == true` | `!Exhausted` **＋** `AttacksThisTurn == 0` | `:1805` / `:1809` |
| 同上 ③（`T_LetLoose`，代表 238） | 两只兽都疲劳 | 两只都**不**疲劳 **＋** 两只 `AttacksThisTurn == 0` | `:1893` / `:1895-1898` |
| 同上 ⑥（`T_ForceKill`，231 形状） | 攻击者疲劳 | **不**疲劳 **＋** `AttacksThisTurn == 0` | `:1981` / `:1983` |
| `RuleEngineTest_EngineD4.cs` A1 | 卡表 `version == 30` | **`32`**（文案补 v31 `keyword` / v32 `chosenAbility`） | `EngineD4.cs:96-104` |
| `RuleEngineTest_EngineC5.cs` Q4b（`EC46` = 237） | `atk.Exhausted == true` | **不**疲劳 **＋** `AttacksThisTurn == 0` | `EngineC5.cs:119` / `:121` |

**夹具侧的三条显式声明**（合成夹具在卡表里没有键 ⇒ 必须自己说清代表哪一族，⛔ 别靠回退档凑绿）：

| 夹具 | 声明 | 行 |
|---|---|---|
| `T_MurderousDesires` / `T_ForceKill` | `Refund`（231 那一族） | `:1774` / `:1962` |
| `T_LetLoose` | `Refund`（代表 238 —— 真卡是附魔链，见 §5·①） | `:1868` |
| `FixtureA1606Each` / `FixtureA1606You` / `FixtureA1632Each` | `NoRefund`（模仿 `GOF86` = 235） | `:34879-34880` / `:35147` |

**数据链路自证**（`TestA1606TurnStartForceAttack` 开头，`:34884-34890`）：
`InstalledRowsForTest == pool.Count` · `Of("GOF86") == NoRefund` · `Of("BL64") == Refund`
—— 万一表没装载，这三条会**先**红，避免把「表没载到」误读成「引擎又退回去了」。

---

## ③ 改动清单（文件 / 行 / 位移影响）

| 文件 | 改动 | 行数变化 | 账/行号影响 |
|---|---|---|---|
| `Unity/工具/gen_cards_engine.py` | 新函数 `chosen_abilities_of` + `CHOSEN_ABILITY_REPORT`；主循环与附魔模板循环各落一列；`version` 31→32 + 注释块；跑完读数打印 | **+91 行**（工作树改前 6924 → 7015；CRLF 已归一，裸 CR 0） | 账里没有本文件的 `:行号` 引用 ⇒ 无影响 |
| `MyGame/Assets/RuleEngine/Resources/cards_engine.json` | 重生成：`version=32` · `count=1128` · 每卡带 `chosenAbility`（1128/1128） | 3,328 KB | `ruleprobe check` 卡池 md5 变（见 §4） |
| 🆕 `MyGame/Assets/RuleEngine/Core/ForcedAttackRefund.cs`（+ `.meta`） | 三态判据口（`Install`/`Of`/`SetForTest`/`ResetForTest`/`TableCards`/`UnknownLookupsForTest`） | 新文件 155 行，LF | 无（新文件） |
| 🆕 `MyGame/Assets/RuleEngine/Data/ForcedAttackRefundMap.cs`（+ `.meta`） | 装载半页（`Resources.Load` + 瘦 DTO）；`InstalledRowsForTest` 的 getter 自带装载 | 新文件 106 行，LF | 无（新文件） |
| `MyGame/Assets/RuleEngine/Core/RuleCore.cs` | ①`:14087-14096` 注释**就地改写**（10 行 → 10 行）②`:14100-14101` 两句 doc 合成一句（-1 行）③`:14115` 退还条件加一个合取项 ④`<summary>` 多写一行（+1 行）⑤**类尾**追加 `ForcedAttackRefunds` | 14188 → **14223**（净 +35，**全部在 14186 之后**） | 🔴 **既有成员的绝对行号一个都没动**：`:6203` `:14102` `:14123` `:14139` `:14181` `:2787` `:4702` `:10861` 等逐条核过；`check_ledger_points` **漂移仍 12（全是旧账）、失败 0** |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | δ 4 处夹具 + α 3 处断言 + 3 条夹具声明 + A1606/A1632 声明 + 数据链路自证 | 38213 → **38272**（+59） | ⚠️ **38213 之后的旧行号会整体 +≤59**；账里 `战斗逻辑驱动.tsv` 的**「观测量」散文列**引了几处 `RuleEngineTest.cs:xxxx`（如 `:5460-5465` `:4189` `:4953` `:33610`）⇒ **散文会漂**，⛔ 不是 `check_ledger_points` 管的那一列（该工具**0 漂移 0 失败**）；要不要改散文由调度台定 |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineD4.cs` | A1 版本断言 30→32 + 文案 | 535 → 540（+5） | 同上（散文级） |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineC5.cs` | Q4b 期望翻转 + 灭自证 | 877 → 886（+9） | 同上（散文级） |
| 🆕 `资料/完全复刻/账/_parts/B8_BM.tsv` | **1 行**：更新 `CardScript.RemoveAttackThisTurn()`（键 = 原版类+方法） | 新文件 | `check_patch_conflicts.py` = ✅ 没有跨补丁重复键（1 份补丁） |

**行尾**：`RuleCore.cs` / `EngineD4` / `EngineC5` / 新文件 = **LF（原样）**；`RuleEngineTest.cs` = **CRLF（原样，每一处都按 `\r\n` 写）**；
`gen_cards_engine.py` = **CRLF（原样，插完发现 80 行被写成 LF ⇒ 已整篇归一回 CRLF，裸 CR = 0）**。
全部核过 `bareCR == 0`。

---

## ④ 新指纹（⛔ 我一个 `Pinned*Md5` 都没改）

`bash 工具/ruleprobe.sh check` → **rc=0 · 哨兵失败 0** · 全池 1121 张 · 解析差异 0 行 · 改名 0 · 新增 0 · 消失 0：

| 钉子 | 现值 | 该不该重钉 |
|---|---|---|
| `PinnedPoolMd5` | **`8aa37475268b685929c113e925f613ad`**（原 `d78f557e4272c2c571a9327139c7202a`） | ✅ **要重钉**（卡表 v31→v32 加了 `chosenAbility` 一列；既有卡内容一字未改） |
| `PinnedBaselineMd5` | `5fb45aec9e162dab2349d9b86bf5740a` | ❌ 不动（基线文件没重生成） |
| `PinnedParseMd5` | `18439d965d14288fe28cfbe5986dc445`（⑦ 输出指纹**一致**） | ❌ 不动（解析输出逐字节没变） |

`bash 工具/ruleprobe.sh play check` → **rc=0**（①-a 主档 / ①-b 电池全等 · ② 基线文件哨兵 ✓ `6e888bbe…`）⇒ **play 侧零变化、不用重钉**。
> 🔴 这不是「行为没变」的证据，而是「**探针看不见这个量**」的证据：`Data/` 半页**不在探针编译集** ⇒ 表恒空 ⇒ 一律 `Unknown` ⇒ 探针里仍是**旧行为（一律退还）**。见 §5·③。

---

## ⑤ 没查清 / 已知边界（都没在本笔动）

1. **`SW56 Let Loose` 的 238 在附魔上，卡表按卡 id 查不到** —— 它的 `abilities` 只有 `30/260/320`，
   攻击落在附魔模板 `f16df0b3a3bf1974db565107bda33847`（`chosenAbility 238`）上。
   ⇒ 本笔对它是 `Unknown` ⇒ **退还**（与改前一致、与**原版一致**，但**是回退档给的，不是数据判据**）。
   真修要么把「卡 → 它造的附魔」这条边带进卡表，要么让引擎在**附魔能力**结算时按附魔卡的 `chosenAbility` 判。
   ⚠️ 一旦有人在**附魔结算路径**上让 `ActingUnit`/`CardPlaySource` 指向附魔卡，本笔会自动正确 —— 但今天没核。
2. **池外两张带强制攻击的卡**（`Dust Storm` `ef42ad6c…` 236 / `Let Loose` 附魔 `f16df0b3…` 238）**不在 1128 张池内**
   ⇒ 列里没有它们 ⇒ 若将来它们真能作为 `CardDef` 出现，**236 那张会错误退还**（238 那张方向对）。
   要覆盖得让生成器按**真值 1,351 张**落列（现在是按池落）。**没做**，因为池内今天不可达。
3. **探针看不见**：`工具/ruleprobe` 只编 `Core/*.cs`（+ 显式列的 4 个 `Data/` 文件）⇒
   `Data/ForcedAttackRefundMap.cs` **不在编译集** ⇒ 表恒空 ⇒ 探针一律 `Unknown`（旧行为）。
   这也意味着本批的既有 G3/G4 缺口**没有变好**：`AttacksThisTurn`/`Exhausted` 仍不进任何指纹。
4. **`chosenAbility` 列还没进 `CardDef`**：要动 `Core/CardDef.cs` + `Data/CardDatabase.cs`（本笔白名单之外）。
   现在的替代做法是 `Data/ForcedAttackRefundMap.cs` **用瘦 DTO 再解析一次** 3.3 MB 卡表（懒加载、只一次，
   代价 = 第一次强制攻击时多一次 `JsonUtility` 解析）。**请求见 §6·②**。
5. **`TestFactionResources` ④c 的 `Unit("SauOnly", …, "Remnant")`（`:4159`）仍是 `Test` 阵营** ——
   它断言「死灵残骸不是可收集的路标石」，改阵营会先被 `EndTurn` 摧毁（返回 `ErrEmptySlot` 而不是 `ErrNotWaystone`）
   ⇒ 要重排断言，本笔**没动**（不在 17 红里）。
6. **`play all dump` 逐字节不可能是本笔的证据**（见 §5·③）；本笔的行为差只可能从 `RuleEngineTest` 或那条
   `ctx.Log("… 重置行动 …")` 看出来。

---

## ⑥ 给调度台的请求

1. **重钉 `PinnedPoolMd5` → `8aa37475268b685929c113e925f613ad`**（另两颗不动，理由见 §4）。⚠️ 另有一份 pending 补丁
   `_parts/B8_AL.tsv`（不是我的），合并时按 `check_patch_conflicts.py` 的顺序走。
2. **要不要把 `chosenAbility` 接进 `CardDef`**（`Core/CardDef.cs` 加一个 `AttachChosenAbilities(int[])` +
   `Data/CardDatabase.cs` 在 `Parse` 里一行）⇒ 之后可**删掉** `Data/ForcedAttackRefundMap.cs` 的二次解析，
   而且**探针会自动看到真值**（`ruleprobe.csproj` 已经编 `CardDatabase.cs`）⇒ §5·③ 那个缺口会一起关掉。
   本笔**没动**那两个文件（白名单）。
3. 跑 `RuleEngineTest.Run` 之后如果 **A1606 只剩真卡那 3 条红**：先看 `:34884` 那三条**前提**断言 ——
   它们红了就说明表没装载（`Resources.Load` 那条链），⛔ 别去改引擎的退还逻辑。
4. `RuleEngineTest.cs` 的行号整体 **+≤59**（散文列里的 `RuleEngineTest.cs:xxxx` 会漂）；要我把散文列一起对齐就说一声。
