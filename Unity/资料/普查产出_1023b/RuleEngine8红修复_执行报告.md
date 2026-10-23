# `RuleEngineTest.Run` 8 红修复 —— 执行报告（2026-10-23）

> 执行代理。判据 = `普查产出_1023b/诊断_RuleEngine8红.md`（只读诊断，结论 **0 条真回归**）。
> **没有跑 Unity**（按简报：主对话之后统一跑 `RuleEngineTest.Run`）· **没有动 git** · **没有改生产实现**
> （`RuleEngine/Core|Data/**`、`CardPresentation/**` 一字未动）。
> 只改了：`RuleEngine/Editor/RuleEngineTest*.cs` · 引擎线台账那一行 · 本报告。

---

## 0. 一句话

8 条红里 **4 条 α（断言/工具产物过期）+ 3 条 δ（夹具前提不成立）+ 1 条（⑨）是 ⑦ 的连累**，
全部按诊断修在**测试与测试工具**上；typecheck **0 / 0**。

---

## 1. 逐条改动

### ① `[诊断入口过期]`（α · 工具产物）—— 已重生成

- **动的是**：`Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_Diag.cs`（**生成物，⛔ 没有手改**）。
- **做了什么**：先 `--check` 复现（盘上 293 / 重生成 294，第 52 行起不一样，缺 `TestB01AiScorerHook`），
  再跑 `PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`，
  产物 **41664 字节**，写 294 条。再 `--check`：**「盘上的产物与重生成结果逐字节相同」**。
- **改后预期**：`[诊断入口过期]` 这条 **Warn 不再出现**（它本来就不在 `_failures` 里，见诊断 §0·a）。
- ⚠️ 生成器 `gen_diag_runner.py` **未改**（`--check` 一致，不需要动）。

### ② `T02·or①/②`（α · id 是截断串）—— 7 个短哈希换全长 id

- **动的是**：`RuleEngineTest_T02.cs:1029-1050` 的 `pickCards` / `randCards` 两个字面量表 + 一段就地订正注释（铁律 5）。
- **改成**（现读 `Resources/cards_engine.json`，`count = 1127`，**7 个前缀各唯一命中 1 张**，脚本核过）：

| 短串 | 全长 id | 卡 |
|---|---|---|
| `6ed51d59` | `6ed51d59b9f8ea24ab0dc7ceaef7956c` | Duelist's Hubris |
| `d01a5ba5` | `d01a5ba5865ded4418194ea50e333b21` | Inscrutable Cunning |
| `4d348cff` | `4d348cff71208c24d8e39252d913ff9d` | Hymn of Battle |
| `9f75a6fe` | `9f75a6fe11922eb409ffbad4ecdeb405` | Hero of the Empire |
| `760bd3af` | `760bd3afde513774d96d6492f995fd37` | Spirit Leech |
| `97e95944` | `97e959444b53f424ea4285e2ca02c7e6` | Chosen of the Four |
| `8482077f` | `8482077f309b08943b190f18b5874871` | Catechism of Death |

- ⛔ **没有**加「前缀匹配」兜底（诊断明令 + 本仓红线：一处规则只写一份）。
- **改后预期**：`T02·or①`（5 张进「玩家选」）与 `T02·or②`（10 张进「随机」）两条转绿。
  诊断 §4 已用 `ruleprobe.sh card` 现读验证过分桶（7 张 `chooseone` 全中、`randomPick` 0/1 各归各桶）。

### ③ `T27⑦`（δ · 夹具没建立「待死」）+ `⑧⑨`（连累）—— 夹具改用**另一个载体**

- **动的是**：`RuleEngineTest_Priority.cs` 的 `W61T27ActorLeftPlayCancels` ⑦ 段（原 `:419-426`）。
- **根因**（诊断 §3）：`IsRightContext` 的 50 档读 **`ctx.IsPendingDeath`**（= 在 `_pendingDeaths` 队列里，
  口径 = 原版 `waitingToDie = 5`），而旧夹具只写 `u.Health = 0` ⇒ **载体仍算在场** ⇒ 入队放行。
- **改法**（诊断 §3·4 ①，零引擎风险）：
  1. **另起一个载体** `dying = Place(ctx, 0, dslot, Unit("Dying", 1, 1, 1))` —— `u` **留给 ⑧⑨**（它们要「活着在场」）；
     末格起往下找一个空格（`BoardSpec.Size = 9`，本夹具只有 `Ghost` 一张，末格必空），并补一条前提断言；
  2. `dying.Health = 0;` + **`ctx.AddPendingDeath(0, dslot, -1);`** —— 这才是原版 state 5
     （现读签名：`public void AddPendingDeath(int p, int slot, int killer, UnitState actingCard = null, bool maySurvive = false)`，
     `Core/BattleContext.cs:1544`）；
  3. ⑦ 断言与「没结算」原样保留；用完 `ctx.Players[0].Board[dslot] = null` 清场。
- ⚠️ **如实标在注释里**：清队列的口 `TakePendingDeaths()` 是 **`internal`**（`BattleContext.cs:1590`），
  Editor 程序集够不着 ⇒ **撤不回来**；本用例在方法末尾，`ctx` 随即丢弃、漏不到别的用例。
  （另核过：`PumpActionQueues` **不**碰待死队列，`Core/ActionPump.cs` 里 `FlushDeaths` 只出现在一句注释里。）
- **改后预期**：⑦ 两条绿（`queued7 = false`、`ran = 4`）；**⑧⑨ 期望一个字没动**，基线恢复后自动绿（诊断 §3·3）。
- ⛔ 诊断里的选项 ②（`Core/ActionPump.cs` 补 `EnoughPendingDamageToDie`）**没有做** —— 那属引擎改动，本批不动生产代码。

### ④ `A1594 哨兵④-b`（α · 断言被 `D15` 作废）—— 期望换成新真值 + 逐值灭自证

- **动的是**：`RuleEngineTest.cs` 的 ④-b 块（原 `:10538-10547`）。
- **改了什么**：
  - **删掉** `wfTwiceKind` 变量与 `Check(wfTwiceKind, WRSegTwice, …)`（档位断言）；
  - 孤儿那条从 **期望 1** 改成 **期望 0**；
  - **新增** `Check(wfTwiceRow.RunOps.Count, 2, …)`（「`D15` 异文追加 ⇒ 两份正文都进腿 A」）；
  - **新增一条灭自证**：`RunOps` 里必须**同时**出现 `Verb == "deal" && Amount == 1` 与 `== 3`
    —— 只数条数挡不住「只追加了一条却报 2」。
- **判据**：`RawCardScript__TriggerAbilities.c:112-120,204-215`（原版遍历 `cardAbilities` 每个元素逐个触发，
  没有「先到先得」）＋ `CardDef.AddTriggerOp` 的 `have.AddRange(ops)`（`Core/CardDef.cs:3194`）
  ＋ `EffectText.WillRunOps` 读的是 `TriggerOps(key)` 合并表（`Core/EffectText.cs:2330-2336`，现读核过）。
- **改后预期**：④-b 三条绿；`A1594` 其余五条（②/④收漏/④付费段/⑤/④-a/④-c）不受影响。

### ⑤ 顺手发现（诊断 §6）—— 两处只写注释，不改实现

1. **`WRSegTwice`（同头两写）在 `D15` 之后结构上不可达 = 死代码** —— 三处注明：
   - 常量 `WRSegTwice` 的注释（`RuleEngineTest.cs` 段级三档那一处）；
   - `ClassifyWillRunFace` 的 ① 分支注释 + `Why` 文案；
   - 报表行那句（原「`TriggerTexts` 只留第一条 ⇒ 这一份不在 `WillRunOps` 的通道账里」）。
   理由：异文追加 ⇒ 每份正文的 op 都在 `TriggerOps` 里 ⇒ 一定同签名命中 ⇒ 一定不是孤儿。
   今天全池读数恒 0（真卡 `ASH69` / `DA7` 两份正文**签名相同**）。
   ⛔ **保留**这一档（探针 `WillRun.KTwice` 与报表行要同口径），只是标成死代码。
   另按铁律 5 的「grep 同一句话」把**报表方法自己的 doc**（`RuleEngineTest.cs:10174`）
   那句「后者被『先到先得』丢掉」也一并订正（诊断没点名、但同一句错话）。
2. **`IsRightContext(Play)` 用 `IsAlive` 近似原版 `IsInPlay`** —— 写进 `RuleEngineTest_Priority.cs` ④ 之后的注释：
   原版 `CardAbility__IsRightContext.c:18-32` = `IsInPlay && !EnoughPendingDamageToDie`，
   而 `CardScript__IsInPlay.c:7-13` **只看卡状态四档、不看血量** ⇒ 对「`Health ≤ 0` 且 `Survivor > 0`」这一格
   原版放行、我们拦下。现读**不可达**（幸存者支在 `CleanupDeaths` 里就把血救回来了），但**口径不完全等价**。
   ⚠️ 因为简报白名单不许改 `Core/ActionPump.cs`，这条**只能写在 Editor 侧的注释里**（没有第二处口径）。

### ⑥ 台账

- `Unity/资料/任务线/引擎线/任务总表.md` 的 `诊断-RuleEngine8红` 行：`◐` → **`✓`**，
  追加「**2026-10-23 已修**」与逐条摘要（含 typecheck、待验收）。只改了这一行。

---

## 2. 改后预期（主对话跑 `RuleEngineTest.Run` 时对这几条）

| 条目 | 期望 |
|---|---|
| `[诊断入口过期]` | 不再出现（Warn，本来不计入失败数） |
| `T02·or①` / `T02·or②` | 绿（7 张卡现读都在池里且分桶正确） |
| `T27⑦` ×2 | 绿（`queued7 = false`、`ran = 4`） |
| `T27⑧` / `T27⑨` | 绿（期望**未改**，基线恢复后自动对） |
| `A1594 哨兵④-b` ×3 | 绿（RunOps 2 / 孤儿 0 / `deal 1`+`deal 3` 同在） |
| 断言总数 | 会比上一轮 **多 3 条**（新增：⑦ 前提 1 条 + ④-b 灭自证 1 条 + 孤儿条替换后净 +1）—— 计算见下 |

> 断言条数变化（相对上一轮日志的 6631）：⑦ 净 **+1**（`dslot` 前提）· ④-b **净 +1**（删 1 条档位断言、
> 孤儿条保留、新增 RunOps 条 + 灭自证条 ⇒ +2−1）· 另 ④-b 的孤儿条**改的是期望值不是条数**。
> ⇒ 预期总条数 **≈ 6634**。⚠️ 以实跑为准，别拿这个数当判据。

---

## 3. typecheck

```
$ TMPDIR=/tmp/wf_refix bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾核对（改完现读，⚠️ 逐字节数出来的）：

| 文件 | CRLF | LF | 结论 |
|---|---|---|---|
| `RuleEngineTest.cs` | 34983 | 34983 | **纯 CRLF，未翻** |
| `RuleEngineTest_Priority.cs` | 0 | 956 | 纯 LF（本来就是 LF） |
| `RuleEngineTest_T02.cs` | 0 | 1139 | 纯 LF（本来就是 LF） |
| `RuleEngineTest_Diag.cs` | 0 | 534 | 纯 LF（生成物本来就是 LF） |
| `任务总表.md` | 0 | 89 | 纯 LF（行数未变） |

---

## 4. 没查清 / 留给主对话

1. **没有实跑 Unity**（简报红线）⇒ 上面 §2 全是「按判据推断的预期」，**不是实测**。
   唯一能被实跑推翻的是 ③⑦：如果 ⑦ 实跑仍然红，先看日志里有没有
   `[ActionQueue] **不触发**「T27⑦-待死50」` 那一行 —— **有** = 闸生效、正文没结算、
   那就要查「`ran` 是不是被别的东西加了一」；**没有** = `IsPendingDeath` 没认出来（那才是引擎问题）。
2. **④-b 的 `RunOps.Count == 2` 是读代码推的**（`AddTriggerOp` 追加 + `WillRunOps` 读合并表），
   **没有在 Unity 里现读**。反证在上一轮日志：孤儿实得 0（第二份正文已被收到）⇒ 与推断一致。
   若实跑红在 `RunOps.Count`，先看它是不是 1（那说明 `AddTriggerOp` 没追加）。
3. **顺手发现 ② 的口径差没改实现**（`Play(0)` 的 `IsAlive` 近似 `IsInPlay`）——
   诊断 §3·4 ② 说「今天零行为变化、可选加固」。本批按简报不动 `Core/`，**这一格仍开着**，
   要不要做由主对话/引擎线排期决定。
4. **`WRSegTwice` 档位「保留 vs 删掉」**：本批选了**保留 + 注明死代码**（与探针、报表行同口径）。
   若主对话决定**删掉**，要同步改三处：常量、`ClassifyWillRunFace` 分支、报表行，
   以及探针 `工具/ruleprobe/WillRun.cs` 的 `KTwice`（⛔ 本批无权改探针）。
5. **`nTwice` 报表行仍然保留**（恒 0）—— 这是有意的：全池读数那一行仍打「同头两写 0 条」，
   ⛔ 别把它当成「今天还有这一类缺陷」。
6. ⚠️ **没有改、但值得下个会话知道的一处**：`RuleEngineTest.cs:31665`（`TestA1564EcstasyHeadGates`
   的 doc）有一句「（改前也被『先到先得』兜住，`_triggerOps` 里那条先登记了就 `return`）」。
   那是**历史叙述（讲 A1469 修之前为什么真池零行为差异）**，不是「今天还这样」的判据；
   而且那条用例在 **2026-10-23 的日志里是绿的** ⇒ 结论仍然成立。
   ⛔ 本批**没有改它**（不在诊断点名的两处之内，也没有证据说它今天错了 —— 不猜）。
