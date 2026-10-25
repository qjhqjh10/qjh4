# G3 · 「IL 层面读不出来」全量清单 + VA 反汇编取证路径

- 生成：2026-10-25（第八十会话 · 未覆盖面普查 · 代理 G3）
- 覆盖问题：用户点名「**少数只能靠 VA 级反汇编**」的那一类 —— 以前只知道零散 5 处
  （`GetConnectionStatus` 的 `ClientState` 映射表 · `ReconnectToBattleRoom` 的七条分支 ·
  `LeaveCurrentRoom` 的 `:360` 之后 · `AllianceRatingDisplay` 的段位拼接 ·
  `InterpolateMilestones` 的内部分母），**没人知道总共多少、分别卡在哪一步**。
- 配套产出：
  - `G3_IL不可读方法_全量.tsv` —— **4,639 个方法**的逐条清单（可重跑，生成脚本 `d:/4/Unity/_tmp_view/g3/gen_tsv.py`）
  - `G3_附_未知被调用目标_305.tsv` —— 305 个「被调用目标未命名」的 VA → 真名反查表（206 命中）

> ## ⚠️ 2026-10-25 更正（R6 现读，覆盖本文件 §三·A #3 与 §四·4 的结论）
> 原文写「`AI.ScoreFromCriteria` 的 `.c`（367 行）完整、Ghidra 已跟到 ⇒ 可直接用」——**不成立**。
> **现读**：`AI__ScoreFromCriteria.c` 的签名被反成 **`void`**（真函数返 `float`），**每一处浮点累加都被 Ghidra 丢了**
> （case 1 的两次 `Count()`、case 2 的 `GetValueInPlay`、case 3–5 的 helper 返回值全丢），函数尾只剩 `return;`。同族：`AI__ScoreFromAttack.c` 也是 `void`。
> ⇒ **口径改成**：这类方法的 `.c` **只给控制流骨架**，**算式必须读 RVA**（`ScoreFromCriteria` 的 36 项跳转表 = **`RVA 0x93B148`**）。
> 判据全文与新公式 → `判据/AI打分与动作选择_第2轮.md`；报告 → `报告/R6_AI打分判据_第2轮.md`。
> 🔑 **通用教训**：`.c` **看起来有 367 行 ≠ 逻辑完整** —— 先看**返回类型对不对**（`void` 出现在本该返 `float` 的函数上 = 值全丢），再看控制流。

> ## ⚠️ 2026-10-25 更正之二（R7 现读）：真缺口 **20 → 19**
> 原文把 `LiveOpsHandler<T>.GetBaseEvents` 判成「泛型分发跳板、逻辑静态拿不到」——**不成立**。
> 那 5 条指令 `[klass+0x2E8] / [klass+0x2F0]` 是 **vtable[slot 27] 的 `{methodPtr, methodInfo}`**（vtable 基址 `0x138`、槽宽 `0x10`，`0x138+27*16=0x2E8` 精确吻合），slot 27 = `GetEvents`
> ⇒ 它就是 **`return GetEvents(b);` 的尾跳**，**不是缺口**。
> 🔑 两把通用钥匙（本文件的结论可直接用）→ `资料/全量反编译_入口与用法.md`「VA 反汇编的两把通用钥匙」。

---

## §一 结论（先说数）

### 1·1 一句话

> **「只能靠 VA 反汇编」这一类的真实规模是 20 条，不是「少数几处」也不是「一大片」。**
> 全仓 **4,639 个方法**在 IL 层带反编译器注记，但其中 **262 条是「控制流/结构被破坏」（SEV1+SEV2）**，
> 这 262 条里 **242 条在 `decomp_full/**.c`（Ghidra）里已经能读到完整逻辑**（其中 **41 条**能读到真正的 `switch`），
> **只剩 20 条**（**全部是泛型共享体 / 分发跳板**）在两层都没有产物 —— 那才是真正要动 VA 的。

⇒ **给调度台的口径建议**：旧账里凡是写着「**只能 VA 反汇编**」的，
**先按 §三 的表查一遍「有没有 `.c`」**；有 `.c` 的直接照 `.c` 做，**不用反汇编**（成本从「半天」降到「5 分钟」）。

### 1·2 全量计数（方法级 · 按最严重的一档归类）

| 档 | 含义 | 方法数 | 占比 |
|---|---|---|---|
| **SEV1** | **结构被破坏**：方法体整段缺失 / 跳转目标落在方法边界外（方法体被截断）/ cpp2il 自报栈不平衡 | **91** | 2.0% |
| **SEV2** | **控制流丢失**：间接跳转（跳转表目标未解析） | **171** | 3.7% |
| SEV3 | 单条指令未实现（SSE 转换类）/ il2cpp 运行时 helper 未命名 | 637 | 13.7% |
| SEV4 | 被调用方法地址未映射到名字 | 2,077 | 44.8% |
| SEV5 | **无伤**：内存基址未定 / ILSpy 类型推断噪声 | 1,663 | 35.8% |
| **合计** | 至少带一条注记的方法 | **4,639** | — |

（SEV1+SEV2 里还有 **36 条是我们引擎/战斗的核心方法** —— `AI.GetActionScore` · `AI.ScoreFromCriteria` ·
`AI.ExecuteAction` · `AbilityLogic.{GetPos,CanPlayAbility,GetValueFromCriteria,GetTargets,PlayAbility}` ·
`CardScript.{AddEffect,ResolveDamageDealt,RemoveEffect}` · `BattleManager.{ResolveAction,Update,GetAnimTransform}` ·
`FilterMethods.{MeetsConditionCriteria,MeetsStatCriteria}` · `GameStaticData.GetEffectDesc` ·
`MatchData.{GetRankedBotPrebuiltDeck,ShoultSetAiOpponent}` · `Evaluation.Check` · `NetworkCustomManager.{GetConnectionStatus,ReconnectToBattleRoom,LeaveCurrentRoom}` 等。
**这 36 条 `.c` 一应俱全**（见 §三表 A）。）

### 1·3 原始注记计数（未归并）

| 注记 | 条数 | 说明 |
|---|---|---|
| `Unmanaged memory load` | 20,847 | 内存基址未定（多为字符串 / 静态字段）—— **不影响控制流** |
| `Method not found @<addr>` | 6,651 | 被调用目标地址未映射（**305 个不同地址**，206 个可反查真名） |
| `Indirect call` | 1,898 | 间接调用（委托 / 虚调用 / 泛型槽）—— 语义正常，只是目标不在 IL 里 |
| `Unknown call target operand` | 744 | **520 `il2cpp_value_box` · 143 `il2cpp_object_unbox` · 80 `il2cpp_vm_object_is_inst` · 1 `SzArrayNew`** ⇒ 语义已知 |
| `Not implemented instruction` | 563 | **注记里直接给了指令原文**（`cvtsi2ss` / `psrldq` / `ucomisd` / `sqrtpd` …）⇒ 语义可判 |
| `Invalid instruction` | 252 | **全部是 `Jump target not found in method: 0x…`**（129 个不同目标）⇒ 方法边界截断 |
| `Indirect jump` | 202 | 跳转表未解析 ⇒ `switch` 分支整段丢失 |
| `HARD_FAIL`（`throw new Exception("Decompilation failed…")`） | 3 | cpp2il 彻底放弃 |
| `Warning`（栈不平衡 / `this` 丢失） | 4 | cpp2il 自报「输出可能错」 |
| （旁）`//IL_xxxx:` 类型推断注释 | 53,912 | **不是缺口** —— 控制流完整，只是栈上类型没推出来 |

---

## §二 口径（怎么扫的 · 什么算「读不出来」）

### 2·1 扫的是什么

| 层 | 位置 | 规模 | 用法 |
|---|---|---|---|
| **IL 层（第一判据）** | `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**.cs` | **1,980 个 `.cs`** | 全量正则扫 `Cpp2ILHelpers.NoteDecompilerIssue("…")` + `throw new Exception("Decompilation failed…")` + `//IL_xxxx:` |
| **Ghidra 层（第二判据）** | `D:/2/tools/decomp_full/**.c` | **25,096 个 `.c`** | 逐个方法找同名/同形产物；扫 `switch(` / `Could not recover jumptable` / `Removing unreachable block` / `unaff_` / `in_stack_` / `switchD_` |
| **命名/地址层** | `D:/2/tools/il2cpp_out/script.json` | **216,881 条 `Address`+`Name`** | 把 `Method not found @<VA>` 反查成真名；`工具/rva_of_method.py` 可按名查 RVA |
| **字节层（VA 反汇编）** | `D:/2/unity_run_ref/GameAssembly.dll`（76,765,184 B） | `.text` / `il2cpp` / `.rdata` / `.data` / `.pdata` / `_RDATA` / `.reloc` 七节 | `工具/disasm_va.py`（capstone 5.0.7）+ 本会话新写的 PE/jump-table 读取（`d:/4/Unity/_tmp_view/g3/va_switch.py`） |

### 2·2 什么算「IL 层面读不出来」

**算**（三条，缺一不算）：

1. **控制流丢失** —— 有一条分支的去向在 IL 里不存在（`Indirect jump` / `Invalid instruction: Jump target not found`）；
2. **方法体不完整** —— 方法没有 `return` 就结束、或 cpp2il 自报栈不平衡 / `this` 丢失 / 直接抛 `Decompilation failed`；
3. **目标不可名** —— 调用点/被调用点的目标在 IL 里是「未解析操作数」（`Method not found @addr` / `Unknown call target operand`）。

**不算**（明确排除 —— 这是本报告最重要的一条，**以前把它们混在一起了**）：

- `//IL_xxxx: Expected Obj, but got I4` 这类 **ILSpy 类型推断噪声**（53,912 条）—— 控制流是完整的，只是栈上类型没推出来；
- `Unmanaged memory load`（20,847 条）—— 只是「这个全局/静态字段的地址没法静态定」，**分支一条不少**；
- `Not implemented instruction`（563 条）—— **cpp2il 把指令原文写进注记了**（例：`Not implemented instruction: "psrldq xmm1,8"`），按助记符就能判语义；
- `Unknown call target operand` 里那 744 条 —— 全是 `il2cpp_value_box` / `il2cpp_object_unbox` / `il2cpp_vm_object_is_inst` / `SzArrayNew`，**语义是公开的**。

### 2·3 为什么**不**直接采信账（`资料/完全复刻/账/*.tsv`）

按简报要求把 21 份账里「没查到 / 读不到 / 间接跳转 / 剥体 / 未解」那批捞出来（**251 行**，其中命中 IL 层措辞的 **94 行**），
逐行现读后发现：**账里的措辞绝大多数已经过期** ——

- 账里写「B 报的 ILSpy `Invalid jump target` 只是 Cpp2IL 的问题，方法体**已解出**」（`卡牌面_渲染.tsv`，`CardDisplayWindow.CardUIClicked`）；
- 账里 63 行写「空方法 / 空体」，逐条回源后**多数是原版方法本身就是空体**（例：`BasicCardUI.HideDescription()` / `DeckCollectionFilter.Start()`），
  属于铁律 11 例外①「原版本身就没有」，**不是读不出来**。

⇒ **本报告不以账为口径**，改以「**IL 层的机器可检出注记**」为口径（可重跑、可复核）。

### 2·4 「读取失败」的分档模型

| 档 | 判据 | 默认取证手段 |
|---|---|---|
| **SEV1** | `HARD_FAIL` / `Warning` / `Invalid instruction` | ① 读 `.c` 同名产物（**76% 命中**）② 无产物 → `rva_of_method.py` 取 RVA → `disasm_va.py` |
| **SEV2** | `Indirect jump` | ① 读 `.c`（**switch 直接给全**）② 无产物 → 解 4 字节 RVA 跳转表（§四·1 有可直接复用的脚本） |
| **SEV3** | `Not implemented instruction` / `Unknown call target operand` | **不用做**（注记自带语义） |
| **SEV4** | `Method not found @addr` | `script.json` 的 `Address→Name` 反查（**本会话已建表**） |
| **SEV5** | `Unmanaged memory load` / `//IL_xxxx` | **不用做**（只在需要那个常量时才读字节） |

---

## §三 逐条清单

> 全量在 **`G3_IL不可读方法_全量.tsv`**（4,639 行；列：`cs_file / member / short_name / severity / priority /
> note_kinds / best_evidence_cpp2il / note_count / c_status / c_file / c_switch / core_flag`）。
> 下面只列**必须动手的**那几组。
>
> ⚠️ **`c_status` 读法**：`exact` = `decomp_full` 里有逐字同名产物（可直接信）；
> `exact_sm` = 按状态机类名精确命中；**`fuzzy` = 按同类前缀 + 方法名片段猜的，动手前请顺序看一眼 `c_file` 对不对**；
> `other_class` = 只有别的类有同名方法（通常是泛型/抽象基类，读任一实例即可）；`missing` = 没找到。

### 3·A **SEV1+SEV2 且是引擎/战斗核心的 36 条 —— 全部有 `.c`，可直接照做**

`core_flag=1`。`c_status=exact` 表示 `decomp_full/<类>__<方法>.c` 逐字同名。

| # | 类.方法 | 档 | 卡在哪 | `.c` 产物 | 备注 |
|---|---|---|---|---|---|
| 1 | `AI.GetActionScore` | SEV2 | `Indirect jump` | `AI__GetActionScore.c` | **AI 打分主入口** |
| 2 | `AI.ExecuteAction` | SEV2 | `Indirect jump` | `AI__ExecuteAction.c` | |
| 3 | `AI.ScoreFromCriteria` | **SEV1** | `Invalid instruction ×8`（跳转目标 0x18093B0B5/0x18093B13F 落在方法边界外） | `AI__ScoreFromCriteria.c`（367 行，**Ghidra 已建 `LAB_18093b139` 跟到了**） | 见 §四·4 |
| 4 | `AbilityLogic.GetPos` | SEV2 | `Indirect jump` | `AbilityLogic__GetPos.c` | |
| 5 | `AbilityLogic.CanPlayAbility` | SEV2 | `Indirect jump` | `AbilityLogic__CanPlayAbility.c` | |
| 6 | `AbilityLogic.GetValueFromCriteria` | SEV2 | `Indirect jump` | `AbilityLogic__GetValueFromCriteria.c` | |
| 7 | `AbilityLogic.GetTargets` | **SEV1** | `Invalid instruction ×30`（目标 0x18081AF8F ×41，落在两个重载之间） | `AbilityLogic__GetTargets.c` | 见 §四·4 |
| 8 | `AbilityLogic.PlayAbility` | **SEV1** | `Invalid instruction ×30` | `AbilityLogic__PlayAbility.c` | |
| 9 | `CardScript.AddEffect` | SEV2 | `Indirect jump` | `CardScript__AddEffect.c` | `switch` 已恢复 |
| 10 | `CardScript.ResolveDamageDealt` | SEV2 | `Indirect jump` | `CardScript__ResolveDamageDealt.c` | `switch` 已恢复 |
| 11 | `CardScript.RemoveEffect` | **SEV1** | `Invalid instruction ×17` | `CardScript__RemoveEffect.c` | `switch` 已恢复 |
| 12 | `CardScript.IsValidActiveAbilityTarget` | SEV2 | `Indirect jump` | `CardScript__IsValidActiveAbilityTarget.c` | |
| 13 | `CardScript.OnTouchDown` | **SEV1** | `Invalid instruction ×3` | `CardScript__OnTouchDown.c` | `switch` 已恢复 |
| 14 | `BattleManager.ResolveAction` | SEV2 | `Indirect jump` | `BattleManager__ResolveAction.c` | `switch` 已恢复 |
| 15 | `BattleManager.Update` | SEV2 | `Indirect jump` | `BattleManager__Update.c` | `switch` 已恢复 |
| 16 | `BattleManager.GetAnimTransform` | **SEV1** | `Invalid instruction ×9` | `BattleManager__GetAnimTransform.c` | |
| 17 | `BattleManager.CreateAnimFromAnimFxController` | **SEV1** | `Invalid instruction` | `BattleManager__CreateAnimFromAnimFxController.c` | |
| 18 | `BattleManager.<>c__AddNextReplayAction.MoveNext` | **SEV1** | 同上 + `Indirect jump ×1` | `BattleManager._AddNextReplayAction_d__420__MoveNext.c` | 录像/回放链 |
| 19 | `BattleManager.<>c__ApplyOffensiveAndDefensiveEffects.MoveNext` | **SEV1** | 同上 | `BattleManager._ApplyOffensiveAndDefensiveEffects_d__337__MoveNext.c` | |
| 20 | `AiScripted.ExecuteAction` | SEV2 | `Indirect jump` | `AiScripted__ExecuteAction.c` | `switch` 已恢复 |
| 21 | `AiScripted.GetTipPosition` | SEV2 | `Indirect jump` | `AiScripted__GetTipPosition.c` | `switch` 已恢复 |
| 22 | `FilterMethods.MeetsConditionCriteria` | SEV2 | `Indirect jump` | `FilterMethods__MeetsConditionCriteria.c` | `switch` 已恢复 |
| 23 | `FilterMethods.MeetsStatCriteria` | SEV2 | `Indirect jump` | `FilterMethods__MeetsStatCriteria.c` | `switch` 已恢复 |
| 24 | `GameStaticData.GetEffectDesc` | SEV2 | `Indirect jump` | `GameStaticData__GetEffectDesc.c` | `switch` 已恢复（效果文案） |
| 25 | `GameStaticData.CheckHiddenCardsInDeck` | SEV2 | `Indirect jump` | `GameStaticData__CheckHiddenCardsInDeck.c` | |
| 26 | `MatchData.GetRankedBotPrebuiltDeck` | SEV2 | `Indirect jump` | `MatchData__GetRankedBotPrebuiltDeck.c` | `switch` 已恢复 |
| 27 | `MatchData.ShoultSetAiOpponent` | SEV2 | `Indirect jump` | `MatchData__ShoultSetAiOpponent.c` | `switch` 已恢复 |
| 28 | `MatchData.GetMatchType` | **SEV1** | `Invalid instruction` | `MatchData__GetMatchType.c` | `switch` 已恢复 |
| 29 | `MatchData.GetMilestones` | **SEV1** | `Invalid instruction` | `MatchData__GetMilestones.c` | `switch` 已恢复 |
| 30 | `Evaluation.Check` | SEV2 | `Indirect jump` | `Evaluation__Check.c` | `switch` 已恢复（`or` 条件求值） |
| 31 | `ScriptedActionData.CheckIfMatchesActionData` | SEV2 | `Indirect jump` | `ScriptedActionData__CheckIfMatchesActionData.c` | `switch` 已恢复 |
| 32 | `NetworkCustomManager.GetConnectionStatus` | SEV2 | `Indirect jump`（跳转表） | `NetworkCustomManager__GetConnectionStatus.c` | **§四·1 已用 VA 字节独立复核** |
| 33 | `NetworkCustomManager.ReconnectToBattleRoom` | SEV2 | `Indirect jump` | `NetworkCustomManager__ReconnectToBattleRoom.c` | `switch` 已恢复（七条分支） |
| 34 | `NetworkCustomManager.LeaveCurrentRoom` | **SEV1** | `Invalid instruction` | `NetworkCustomManager__LeaveCurrentRoom.c` | `switch` 已恢复（简报点名的那条） |
| 35 | `BattleManager.ConnectionStatusChanged`（委托） | SEV2 | `Indirect jump ×8` | 委托目标散在 `Everguild.BattleErrorUIManager__ConnectionStatusChanged.c` | |
| 36 | `MatchData..ctor` | SEV2 | `Indirect jump` | `MatchData__.ctor.c` | |

### 3·B **SEV2 里能读到真 `switch` 的 26 条**（= 当年「IL 只有半个 switch」的全部）

`grep -P "\tSEV2\t.*\tswitch\t"` / 见 `G3_IL不可读方法_全量.tsv` 的 `c_switch` 列。除上表已列的之外还有：

`CardHighlight.ChangeState` · `ChatGlobalManager.OnPrivateMessage` · `CloudscriptHandler.ShowInPlaystream` ·
`EverguildSceneManager.<>c__LoadTransition.MoveNext` · `FlexibleGridLayout.CalculateLayoutInputHorizontal` ·
`NetworkingPeer.OnOperationResponse`

> 这 26 条**不需要反汇编** —— `decomp_full` 里的 `.c` 已经是 Ghidra 从跳转表重建出来的 `switch(...)`。

### 3·C **真正「两层都没有产物」的 20 条（= 唯一需要 VA 反汇编的那一类）**

前 4 条是**泛型共享体**（`decomp_full` 只有具体实例化，没有共享体），其余是**分发跳板 / lambda 命名差**：

| # | 类.方法 | 档 | 卡在哪 | 为什么没 `.c` | 下一步 |
|---|---|---|---|---|---|
| 1 | `LiveOpsHandler<TEvent>.GetBaseEvents` | **SEV2** | `Indirect jump` + cpp2il 报 `End of method reached without returning` | 泛型类方法 ⇒ `script.json` 里命名的是一条 **5 条指令的分发跳板**，真身无名 | **已跑通一半**：见 §四·3 |
| 2 | `IdentifiableWrapper<T>.GetID` | SEV2 | `Indirect jump ×2` / `Indirect call×1` | 泛型基类，只有具体实例 `.c`（`AllianceItem__GetID.c` 等） | 读任一具体实例即可（各实例结体相同） |
| 3 | `ItemDrawer<T>.GetItemName` | SEV2 | `Indirect jump` | 同上 | 同上（`AllianceBadgeDrawer__GetItemName.c`） |
| 4 | `LoadableReference<T>.GetHashCode` | SEV2 | `Indirect jump` | 同上 | 同上（`CardDeck__GetHashCode.c`） |
| 5 | `Points.GetID` | SEV2 | `Indirect jump` | 抽象基类 | 读具体实例 |
| 6 | `ItemInfo.Type`（getter） | SEV2 | `Indirect jump` | 抽象/泛型 getter | 读具体实例 |
| 7 | `PlayfabWrapper.<>c__ToAsync<T>.d__0.MoveNext` | SEV2 | `Indirect jump` | 泛型状态机 | `rva_of_method.py` 取 RVA |
| 8 | `InAppPurchaseFake.<>c__DisplayClass4_0.<OnValidatePurchase>b__0` | SEV2 | `Indirect jump ×2` | lambda 命名差 | 按 `b__` 编号在 `.c` 里找 |
| 9 | `InAppPurchaseSteam.<>c__DisplayClass8_0.*` | SEV2 | `Indirect jump` | 同上 | 同上 |
| 10 | `PurchasesManager.<TryRecoverFromPendingPurchases>g__…` | SEV2 | `Indirect jump` | 本地函数命名差 | 同上 |
| 11 | `AllianceRankingDataProvider.<>c__DisplayClass0_0.<FetchRanking>b__0` | SEV2 | `Indirect jump` | lambda 命名差 | 同上 |
| 12 | `DraftRankingDataProvider.<>c__DisplayClass0_0.<FetchRanking>b__0` | SEV2 | `Indirect jump` | 同上 | 同上 |
| 13 | `RawLiveOpsHandlerPersistence.<.ctor>b__1_1` / `b__1_2` | SEV2 | `Indirect jump` | 同上 | 同上 |
| 14 | `RewardWindowContext.<.ctor>b__0_0` | SEV2 | `Indirect jump` | 同上 | 同上 |
| 15 | `SpreadsheetData.<.ctor>b__0_0` | SEV2 | `Indirect jump` | 同上 | 同上 |
| 16 | `ToggleCollectionFilter.<>c<T,TEnum>.<Filter>b__7_2` | SEV2 | `Indirect jump` | **`decomp_full` 只有 `b__7_0` / `b__7_1`** ⇒ 真的少一个 | `rva_of_method.py` |
| 17 | `RawCardScript` 的 `do{…}` 局部 | SEV2 | `Indirect jump` | 成员归属没解析对 | 按行号回 `.cs` 定位 |
| 18 | `--f__AnonymousType108.ToString` | **SEV1** | `Warning: 栈不平衡(-68)` + `Method not found ×38` | 匿名类型 | `rva_of_method.py` |
| 19 | `ListExtensions.<>c__9<T>.<ChunkBy>b__9_2` | **SEV1** | `Warning: 'this' local not found` | 泛型 lambda | `rva_of_method.py` |
| 20 | `PlayfabWrapper.<ToAsync>b__1` | **SEV1** | `Warning: 栈不平衡(-60)` | 泛型状态机 | `rva_of_method.py` |

> **取证固定四步**（`资料/战斗规则与数值_出处.md` §三 那套，本会话已复跑验证）：
> `PY=D:/2/Warpforge_tools/py312/python.exe`
> `$PY d:/4/Unity/工具/rva_of_method.py "<类>$$<方法>"` → 得 VA →
> `$PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll <VA> 60`
> ⇒ **自检**：头几条必须是合法函数序言（`push` / `sub rsp,??`），出现 `(bad)` = RVA 映射错了。

### 3·D **SEV4：305 个「被调用目标未命名」的地址 —— 已批量反查**

`G3_附_未知被调用目标_305.tsv`。要点：

- **206 / 305 反查成功**（用 `script.json` 的 `Address`→`Name`）；
  ⚠️ **2026-10-25 G5 加限定**：`script.json` 有 **13,991 个地址被多个方法共用**（354,653 条记录只落在 216,881 个地址上；最极端 `0x1804B33B0` 一个地址服 **6,376 个空方法**）⇒ 这 206 里**有一部分反查到的是「同址的任意一个名字」**。
  实证：`LiveOpsHandler.RefreshHandler` 的洞被反查成 `TMPro.TMP_Text.GetCompoundBounds`（显然错），按同址方法族读真身 = **`AsyncTaskMethodBuilder.Create()`**（同址 24 名里 21 个叫 `Create`）。⇒ 单个反查结果**必须用「同址方法族」交叉**才可信。
- 命中率不高的原因：**未命中的 99 个地址吃掉了 5,023 / 6,651 条注记**（长尾在前几名 —— `0x1800021F0` 一个就 999 条）；
- **未命中的那 99 个全在 il2cpp 运行时 helper 段**：`0x180001F00`–`0x1800066E0`、`0x1803F3xxx`、`0x18048xxxx`、`0x1805Exxxx`。
  它们**不是业务方法**（例：`0x1803F44F0` = `il2cpp_codegen_initialize_runtime_metadata` 类，
  每个 `.c` 开头那串 `FUN_1803f44f0(&DAT_…)` 就是它；`0x1803F47A0` = NullReferenceException 抛出器）。
  ⇒ **建一次 99 条的「helper 名字表」≈ 1 小时**，之后全仓 6,651 条注记可一次性消掉。

### 3·E **P3 候选（等用户裁定，本代理不判 D）**

以下**只列不判**，属「服务器 / 账户 / 云后端」面：

`PlayfabWrapper` · `CloudscriptHandler` · `LiveOpsManager` / `LiveOpsHandler` · `PurchasesManager` ·
`InAppPurchaseFake` / `InAppPurchaseSteam` / `InAppPurchaseProxy` · `GooglePlayManager` ·
`Unity.Services.Core.*`（`Metrics.SendSumMetric` 是最高频的已解析 callee，449 次）· `Firebase.*` ·
`LoginManager` / `AccountTab` / `AuthenticationValues`

---

## §四 可行性实证（VA 反汇编真的跑通了，不只 1 条）

### 4·1 主实证：`NetworkCustomManager.GetConnectionStatus` 的 `ClientState → ConnectionStatus` 跳转表

**这是简报点名的第一条，也是「IL 完全读不出来、VA 字节能 100% 重建」的最干净例子。**

**① cpp2il（IL 层）读到的是什么** —— `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/NetworkCustomManager.cs:507-526`：

```csharp
public unsafe ConnectionStatus GetConnectionStatus()
{
    //IL_0017: Expected Obj, but got Ref        ← ILSpy 类型噪声
    //IL_0073: Expected Obj, but got I8
    //IL_008d: Expected Obj, but got I8
    ClientState connectionStateDetailed = PhotonNetwork.connectionStateDetailed;
    ...
    if (connectionStateDetailed2 <= ClientState.Authenticating)
    {
        object obj = 6442450944L;               ← ← ← 0x1_8000_0000 = ImageBase！
        Cpp2ILHelpers.NoteDecompilerIssue("Unmanaged memory load: [v72 @ rdx_v4+7D1CDC+v58 @ rax_v7 (ClientState)*4]");
        object obj2 = 0 + 6442450944L;
        Cpp2ILHelpers.NoteDecompilerIssue("Indirect jump: v75 @ rcx_v10 (should have been resolved before IL gen)");
    }
    return ConnectionStatus.undefined;          ← ← ← 21 条分支全丢，只剩 default
}
```

⇒ **IL 层完全读不出映射表**。但注记把**表地址（`+7D1CDC`）· 索引语义（`ClientState * 4`）· 基址（`0x180000000`）** 全给了。

**② 取 RVA 并反汇编**（`工具/rva_of_method.py` + `工具/disasm_va.py`，本会话实跑）：

```
$ rva_of_method.py "NetworkCustomManager$$GetConnectionStatus"
  Address=8199136  RVA=0x007D1BE0  VA=0x1807D1BE0

$ disasm_va.py GameAssembly.dll 0x1807D1BE0 70
1807D1C7C  83 f8 14                 cmp eax, 0x14          ; ClientState <= 20 ?
1807D1C7F  77 51                    ja  0x1807D1CD2        ; -> xor eax,eax / ret  (= 0 undefined)
1807D1C81  48 8d 15 78 e3 82 ff     lea rdx, [rip - 0x7d1c88]   ; rdx = 0x1807D1C88-0x7D1C88 = 0x180000000
1807D1C88  48 98                    cdqe
1807D1C8A  8b 8c 82 dc 1c 7d 00     mov ecx, [rdx + rax*4 + 0x7D1CDC]   ; ← 跳转表，32 位项
1807D1C91  48 03 ca                 add rcx, rdx                        ; ← 项是【RVA】，不是绝对地址
1807D1C94  ff e1                    jmp rcx
1807D1C96  b8 02 00 00 00           mov eax, 2      ; ← case 4
1807D1CA0  b8 04 00 00 00           mov eax, 4      ; ← case 9
1807D1CAA  b8 05 00 00 00           mov eax, 5      ; ← case 0/1/15
1807D1CB4  b8 01 00 00 00           mov eax, 1      ; ← case 16
1807D1CBE  b8 07 00 00 00           mov eax, 7      ; ← case 18
1807D1CC8  b8 06 00 00 00           mov eax, 6      ; ← 其余
1807D1CD2  33 c0 / 48 83 c4 48 / c3 xor eax,eax ; add rsp,0x48 ; ret
```

**③ 直接读表字节**（`d:/4/Unity/_tmp_view/g3/va_switch.py`，纯 `struct` 读 PE）：

```
table @RVA 0x7D1CDC, 21 × 4B:
aa1c7d00 aa1c7d00 c81c7d00 c81c7d00 961c7d00 c81c7d00 c81c7d00
c81c7d00 c81c7d00 a01c7d00 c81c7d00 c81c7d00 c81c7d00 c81c7d00
c81c7d00 aa1c7d00 b41c7d00 c81c7d00 be1c7d00 c81c7d00 c81c7d00
```

**④ 结论（纯 VA 字节 + 反汇编得出，未参考 `.c`）**

| ClientState | 名字 | → ConnectionStatus |
|---|---|---|
| 0 `Uninitialized` / 1 `PeerCreated` / 15 `Disconnected` | | **5 `disconnected`** |
| 2 `Queued` / 3 `Authenticated` / 5 `DisconnectingFromMasterserver` / 6 `ConnectingToGameserver` / 7 `ConnectedToGameserver` / 8 `Joining` / 10 `Leaving` / 11 `DisconnectingFromGameserver` / 12 `ConnectingToMasterserver` / 13 `QueuedComingFromGameserver` / 14 `Disconnecting` / 17 `ConnectingToNameServer` / 19 `DisconnectingFromNameServer` / 20 `Authenticating` | | **6 `changing`** |
| 4 `JoinedLobby` | | **2 `lobby`** |
| 9 `Joined` | | **4 `inRoom`** |
| 16 `ConnectedToMaster` | | **1 `connected`** |
| 18 `ConnectedToNameServer` | | **7 `limbo`** |
| >20（`ja`） | | **0 `undefined`** |

（枚举名出处：`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ConnectionStatus.cs` ·
`…/ClientState.cs`；`ConnectionStatus` = `undefined/connected/lobby/mainMenu/inRoom/disconnected/changing/limbo`，
21 个 `ClientState` 值全部覆盖。）

**⑤ 交叉验证（这一步才是「路走通了」的证据）**：同一方法的 Ghidra `.c`
（`D:/2/tools/decomp_full/NetworkCustomManager__GetConnectionStatus.c`）给出

```c
switch(uVar1) {
case 0: case 1: case 0xf:  return 5;
case 2: case 3: case 5: case 6: case 7: case 8: case 10: case 0xb:
case 0xc: case 0xd: case 0xe: case 0x11: case 0x13: case 0x14: return 6;
case 4: return 2;   case 9: return 4;
case 0x10: return 1; case 0x12: return 7;
default: return 0; }
```

⇒ **与 VA 字节推出来的表逐格一致（21/21）**。两条独立路径互证。

**⑥ 顺带把简报点名的另外两条也结了**（都在 `.c` 里，**不用反汇编**）：

- **`ReconnectToBattleRoom` 的七条分支**（`NetworkCustomManager__ReconnectToBattleRoom.c`）：
  `1,2 → JoinRoom(this+0x40)` · `default(0/3) → LogWarning 后【贯穿】到 5` · `5 → ConnectToPhoton()` ·
  `6 → return` · `4 → break`（跳出 switch 走「房间检查」：`room==null → LogError` 否则
  `room.Name != this+0x40 → LeaveRoom(1)`）· `7 → PhotonNetwork.Disconnect()`。
- **`LeaveCurrentRoom` 的 `:360` 之后**（`NetworkCustomManager__LeaveCurrentRoom.c:24`）：
  `1 → Log; return` · `2 → LogError; return` · `default → LogWarning; return` ·
  `5 → ConnectToPhoton(); return` · `4,6 → break` → 之后是「房间检查 + `Room.set_EmptyRoomTtl(0)` + `PhotonNetwork.LeaveRoom(1)`」。

### 4·2 次实证：cpp2il **彻底失败**的 3 条，Ghidra `.c` 全部完整

| 方法 | cpp2il | `.c` |
|---|---|---|
| `PriceDisplay.Setup(CurrencyValue)` | `throw new Exception("Decompilation failed: Stack state not settling! (500001 blocks already visited)")` | `PriceDisplay__Setup.c` —— 完整：取 `currency.ToCurrency()` → 遍历 vtable 找 `DAT_18429e6f0` 槽 → 调它 → 再取 `param_2.vtable[0x168/2]` → 递归回 `Setup(display, currency, icon)`；空/`0x28` 时走 `SetupFree` |
| `RemnantAeldari.Toggle(bool, bool, Action)` | 同上 | `RemnantAeldari__Toggle.c` —— 完整 3 步：`gameObject.SetActive(param_2)` → `RemnantBody.BodyVisibilityToggle(param_2 ^ 1)` → `return` |
| `UIResourceCounter.Initialize(ICurrency, int)` | 同上 | `UIResourceCounter__Initialize.c` —— 完整：vtable 取 icon → `Image.set_sprite` → `FUN_1800021f0(3,…)` 写 `+0x38`（`maxAmount`）→ `SetQuantity(param_3)` |

⇒ **「cpp2il 抛 Decompilation failed」≠「读不出来」** —— 3/3 都能在 `.c` 读到。
（`RemnantAeldari.Toggle` 正是我们 `useWaystone` 主动收集那条链的入口，见 `资料/` 里 2026-09-25 补的那段。）

### 4·3 第三实证：`LiveOpsHandler<TEvent>.GetBaseEvents` —— 泛型分发跳板（这是真缺口的样子）

**① cpp2il 一个字都读不出** —— `…/LiveOpsHandler.cs:3135-3147`：

```csharp
public IEnumerable<ILiveOps> GetBaseEvents(bool includeBlockedEvents = false)
{
    //IL_0005: Expected I, but got Obj
    nint num = (nint)this;
    Cpp2ILHelpers.NoteDecompilerIssue("Unmanaged memory load: [v0 @ r8_v1 (Il2CppClass<LiveOpsHandler`1<TEvent>>)+2E8]");
    object obj = 0;
    Cpp2ILHelpers.NoteDecompilerIssue("Unmanaged memory load: [v0 @ r8_v1 (Il2CppClass<LiveOpsHandler`1<TEvent>>)+2F0]");
    object obj2 = 0;
    Cpp2ILHelpers.NoteDecompilerIssue("Indirect jump: v2 @ rax_v1 (should have been resolved before IL gen)");
    /*Error: End of method reached without returning.*/;
}
```

**② `decomp_full` 里没有产物**（`ls decomp_full | grep LiveOpsHandler` → **0 条**）。

**③ VA 反汇编把它解释清楚了**（`script.json` 里 `LiveOpsHandler<object>$$GetBaseEvents` = `Address 16875488` = RVA `0x1017FE0`）：

```
181017FE0  4c 8b 01              mov r8, qword ptr [rcx]        ; r8 = *(this) = Il2CppClass*
181017FE3  49 8b 80 e8 02 00 00  mov rax, qword ptr [r8 + 0x2e8]  ; rax = klass->slot[0x2E8]
181017FEA  4d 8b 80 f0 02 00 00  mov r8,  qword ptr [r8 + 0x2f0]  ; r8  = klass->slot[0x2F0]  (= MethodInfo)
181017FF1  48 ff e0              jmp rax                          ; ← 目标【只有运行时才知道】
181017FF4  cc cc cc …            int3 (对齐填充)
181018000  48 89 5c 24 10 …      ← 下一个方法（script.json 说是 $$GetEvent）的真函数体
```

**④ 这一条的定性（也是「20 条真缺口」的模板）**：
`GetBaseEvents` **是接口 `ILiveOpsHandler` 的成员**（`…/ILiveOpsHandler.cs:37`：
`IEnumerable<ILiveOps> GetBaseEvents(bool includeBlockedEvents = false);`）。
il2cpp 给它生成的入口是 **「{代码指针, MethodInfo} 双槽分发跳板」** —— `0x2E8/0x2F0` 正好是一对相邻 8 字节
（形如 `VirtualInvokeData{methodPtr, method}`），**槽里的值只有运行时才知道，所以逻辑静态拿不到**。
⚠️ 同族的 `GetEvent`（`0x1018000`）字节上是**普通函数体**、`GetEvents`（`0x1018130`）同理 ——
**只有 `GetBaseEvents` 这一条是跳板形态**，为什么不同，**未查清**（见 §五·2）。

**⑤ 下一步（可执行）**：拿 `LiveOpsHandler<object>` 的 `Il2CppClass*`（`D:/2/tools/il2cpp_out/dump.cs` /
`il2cpp.h` 有类型布局；或运行时 dump），读 `klass+0x2E8` 得真函数指针 → 再 `disasm_va.py`。
**成本 1–2 h**；⚠️ 这条链是 **LiveOps（活动）线**的地基，值得做。

### 4·4 第四实证：「跳转目标不在方法内」不是逻辑丢失，是**方法边界截断**

cpp2il 报了 129 个 `Jump target not found in method: 0x…`。逐个把目标地址按 `script.json` 排序定位后，
**全部落在「两个命名方法之间」**（不是别的方法里）：

| 报错目标 | 归属方法 | 距方法起点 | 下一个命名方法 |
|---|---|---|---|
| `0x18093B0B5` / `0x18093B13F` | `AI.ScoreFromCriteria` @ `0x93A290` | +0xE25 / +0xEAF | `AI.ScoreFromDamagingBuffedUnit` @ `0x93B1E0` |
| `0x18081AF8F`（41 次，最多） | `AbilityLogic.GetTargets` @ `0x817E90` | +0x30FF | 另一个 `GetTargets` 重载 @ `0x81AFC0` |
| `0x180821524` | `AbilityLogic.PlayAbility` @ `0x81C000` | +0x5524 | `AbilityLogic.PlayAnim` @ `0x8215E0` |

⇒ 机制 = **cpp2il 按元数据切方法时把尾巴切短了**（共享尾块 / 重载间共享代码）。
**Ghidra 不受这个边界约束**：`AI__ScoreFromCriteria.c`（367 行）里**真的建出了 `LAB_18093b139` 标签**，
`AbilityLogic__GetTargets.c` 与 `AbilityLogic__PlayAbility.c` 里也各自出现了对应目标 —— **跟过去了，逻辑完整**。

⇒ 所以 SEV1 的取证顺序应该是：**先看 `.c` 有没有那个 `LAB_…`；有就完事**，没有才反汇编。

---

## §五 没查清

1. **§三·C 那 20 条里，14 条「lambda/本地函数命名差」的没逐条去找 `.c`。**
   `ToggleCollectionFilter.<Filter>b__7_2` 已核**真的没有**（`decomp_full` 只有 `b__7_0` / `b__7_1`）；
   其余 13 条只是我的匹配规则没命中，**不代表没有** —— 需要按 `b__` 编号 + 类名前缀人工过一遍。
2. **`klass+0x2E8 / +0x2F0` 的确切语义只坐实到「一对 {methodPtr, MethodInfo}」这一步。**
   已核实：`GetBaseEvents` 是接口 `ILiveOpsHandler` 的成员（`ILiveOpsHandler.cs:37`），
   所以这是**接口/虚分发**的解析槽；`0x2E8` 与 `0x2F0` 相邻 8 字节、形状同 `VirtualInvokeData`。
   **未查清**：为什么同族的 `GetEvent`（`0x1018000`）是普通函数体而不是跳板。
3. **99 个未解析 callee 地址只给了「在 il2cpp helper 段」的区间判断，没有逐个命名。**
   本会话只对**最高频的两个**做了反汇编（**只读到「形状」，没读到名字**）：
   - `0x1803F44F0` = `sub rsp,0x28 / call 0x18043c4e0 / lock or dword[rsp],0 / add rsp,0x28 / ret`
     —— `lock or [rsp],0` 是 il2cpp 的**元数据初始化栅栏**惯用法，**推断**是 `il2cpp_codegen_initialize_runtime_metadata` 一族，
     **没有符号证据**；
   - `0x1803F47A0` = `sub rsp,0x28 / call 0x18040f710 / int3…` —— `call` 之后紧跟 `int3` ⇒ **该 call 不返回**
     （Ghidra 也正是因此写 `/* WARNING: Subroutine does not return */`），形状上是**异常抛出器**；具体是哪一个**未验**。
   其余 97 个只按区间判断，**未命名**。

4. **`Ghidra` 层「结构性失败」的量化只做到「不可能」这一步**：
   `switchD_*::caseD_*`（真正的未收敛计算 goto）**全库 0 处**；
   925 个 `Could not recover jumptable … Too many branches` 逐条看下来**全部同时带 `Treating indirect jump as call`**
   （= Ghidra 把一次 `call [reg]`/尾调用误报成跳转，**语义可读**）。
   `unaff_` 仅 6 个文件（`RawCardScript.GetSummon/Attack/DeathSound` + 两个 `GetParentRecursively`），
   `unaff_RDI` 只是 Unity `op_Inequality` 的第 5 个参数位，**无损**。
   ⇒ **但 `in_stack_`（190 文件）/ `extraout_`（89 文件）没逐条看**，可能还藏着真正丢值的地方。
5. **`AllianceRatingDisplay` 的段位拼接 / `InterpolateMilestones` 的内部分母**（简报点名的后两条）
   只做了「在 IL 层是 SEV2/SEV5」的归类（`AllianceRatingDisplay.Initialize` = `Indirect call×3`；
   `SupportMethods.InterpolateMilestones` = `ILSpyComment×15`，**不是控制流问题**），
   **没有把这两条的具体数值结论写出来** —— 那属于「照 `.c` 去复刻」的活，不是本普查的范围。

---

## §六 顺手发现（不在本代理的活里，但值钱）

1. 🔴 **`cpp2il` 的 `Not implemented instruction` / `Invalid instruction` 注记，全仓第一次被正确解析。**
   以前按 `NoteDecompilerIssue("…")` 直接正则匹配会**静默漏掉 1,559 条**（563 + 252 + 744），
   因为这些注记的 payload 里带**转义的引号**（`\"cvtsi2ss xmm3,eax\"`），`[^"]*` 会在 `\` 后的第一个 `"` 停下。
   ⇒ **要统计反编译问题，必须按「逐字符解析字符串字面量」而不是正则 `[^"]*`**。本会话的 `parse_notes()` 可直接复用。
2. 🔴 **`script.json`（216,881 条 `Address`+`Name`）是「被调用目标未命名」的现成解药，但没人用过。**
   一条正则就能把 6,651 条注记里的 206 个地址反查成真名。**建议进正轨工具**
   （`工具/rva_of_method.py` 已经能按名查 RVA，缺一个**反向**的 `va_of_address.py`）。
   ⚠️ 而且 `rva_of_method.py` 的匹配对**泛型名**（`LiveOpsHandler\u003Cobject\u003E$$GetBaseEvents`）会漏 ——
   直接 `grep GetBaseEvents script.json` 能搜到，工具却报「没命中」。**这是个工具 bug，不是数据缺失。**
3. 🟡 **`decomp_full` 里 925 个 `Could not recover jumptable` 是「误报」不是「失败」** ——
   全部是 `(**(code **)(*obj + 0x1B8))(obj, …)` 形状的尾调用/vtable 调用。
   ⛔ 别拿这个数字当「925 个方法读不出来」写进任何文档。
4. 🟡 **`LiveOpsHandler<TEvent>` 的【11 个普通方法】在 `decomp_full` 里一条产物都没有** ——
   而它是 LiveOps 线的地基。⚠️ **2026-10-25 就地订正**：本报告初稿写的是「`LiveOpsHandler` 在 `decomp_full` 里
   一条产物都没有」，**那是错的**（我只 grep 了 `LiveOpsHandler__GetBaseEvents` 一条就下了结论，犯了铁律 5 禁的那个错）。
   **现读真值**：`ls decomp_full | grep -i LiveOpsHandler` = **41 条**，但**全是状态机与 lambda**
   （`_SetupEvents_d__18__MoveNext` · `_RefreshHandler_d__26/29` · `_RefreshHandlerLocally_d__27` ·
   `<>c__object____SetupEvents_b__18_0` · `<>c__DisplayClass21_0…ExpireEvent_b__1` 等）。
   **缺的是那批普通方法**：`ClearEvents` / `DeserializeSharedData` / `ExpireEvent` / `GetBaseEvents` /
   `GetEvent` / `GetEvents` / `HandleBeforeRefresh` / `InitializeHandler` / `QueueEventPopup` /
   `SetupEvents` / `UpdateCachedSave` —— 它们**在 `script.json` 里都有名字和地址**
   （`GetBaseEvents` = `0x1017FE0`，`GetEvent` = `0x1018000`，`GetEvents` = `0x1018130`，`HandleBeforeRefresh` = `0x10181B0`，
   `InitializeHandler` = `0x1018250`，`QueueEventPopup` = `0x1018490`，`ClearEvents` = `0x10176B0`,
   `DeserializeSharedData` = `0x1017BD0`, `ExpireEvent` = `0x1017C80`），**只是 Ghidra 那一侧没给它们建产物**。
   ⇒ 这说明 `decomp_full` 的导出**是按方法清单过的，某个方法在 Ghidra 里没被单独识别成函数（或被并进邻居）就整条没有产物** ——
   **这是一个还没被记录过的导出缺口类型**，建议补导这一段（0x1017600–0x1018A00）。

5. 🟡 **`AI.ScoreFromCriteria` 的 cpp2il 产物是残缺的**（`Invalid instruction ×8`，尾部跳到 `0x18093B0B5`/`0x18093B13F`），
   而 `.c` 是完整的 367 行。**凡是当年「照 cpp2il 读 AI 打分」得出的结论都要回 `.c` 复核一遍。**
