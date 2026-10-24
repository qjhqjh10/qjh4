# E2 · 逐 op 同步残值 / EvtKind 出声 / UM77 窗口（2026-10-24）

> 执行代理 E2 交件，主对话原样落盘（该代理按会话指令只回文本）。

## 结论

1. **`PreviousAbilityTargets` 残值 —— 能在不改架构的前提下根治，已做完。**
   判据（`d:/2/tools/decomp_full/` 现读）：`AbilityLogic__PlayAbility.c` **41 个 `case 0x..` / 48 处引用 `LAB_18081cc87`**，每个效果出口都是 `uVar18 = *(param_1+0x80); … SetPreviousAbilityTargets(param_1,uVar18,0); return;`（全函数只有 `:2745` 一处直接调用）⇒ **逐效果整表覆盖**；且 `AbilityLogic__DeepCopy.c` **不拷贝 `+0x80` 那一格** ⇒ 每次 play 从空开始（这条是新证据，把「每条 ability 一个新表」坐实了）。E8 报告 §六·2 的方向对。
   **改前**：只在两条 `ResolveOps` 出口同步一次、取 `LastTargets`（= 最后一条 op）⇒ 同一次结算里**后面**的 op 读到**上一张卡**的目标（静默多数），且丢「一条 ability 内多效果目标的并集」。
   **改后**：`BattleContext` 新增并集表 `AbilityTargetsSaved`（原版 `+0x80` 对应物）；两条 `ResolveOps` 入口清空；两个主循环 **4 处 `ResolveOne` 之后各同步一次** + 出口幂等一次；发布视图 = 至今并集。
2. **`EvtKind` 三处 `default`** —— **改前就已经被别的代理做完了**（`EventTiming.cs` 现读：登记表 `ZeroDurationKinds`/`TimedKinds` + `ReportUnregisteredKind` 出声+计数 + `IsSourced` 回 false），断言已在 `CardPresentation/Editor/BattleScene.cs:1618-1685`（含「往枚举加一档不登记 ⇒ 红」的完备性闸）。本代理只做**独立复核**：现 19 档**全部登记**（脚本枚举比对：未登记 0 / 孤儿 0 / 重复 0），并在自己文件里再钉一遍。
3. **`UM77`「died since last turn」** —— **也已做完**（A1 + B7）。`EffectText.cs:4744` 归出独立 scope `diedsincelastturn`；`EffectResolver.cs:9897` 用 `DeadUnits`+`DeathTurn ∈ {Turn, Turn-1}`；`RuleCore.cs:1561-1562` 回合滚动；基线 `工具/ruleprobe/基线/out_baseline.txt:984` 现读 UM77 = `repeat/…/own|troop//diedsincelastturn`。
   **台账裁定：总表第 16 行「已核销（A1+B7）」是对的；第 77 行 `✗ 未做` 是过期行**。

## 证据（改后行号）

- `RuleEngine/Core/EffectResolver.cs:9793` `NotePreviousAbilityTargets`（并集+去重+发布）；调用点 `:489 / :546 / :595`（出牌路）· `:6514`（触发路）· 出口幂等 `:603 / :6528`；入口清空 `:331 / :6381`。
- `RuleEngine/Core/BattleContext.cs:1094` `AbilityTargetsSaved`；`:1046-1074` `PreviousAbilityTargets` 的 doc **就地订正**（铁律 5，写明「原来那条『只在出口』是错的」）。
- `RuleEngine/Core/EffectResolver.cs:9897`（UM77 分支）· `RuleCore.cs:1561` · `EffectText.cs:4744`。
- 新文件 `CardPresentation/Editor/RuleEngineTest_EngineE2.cs`（+ `.meta`，guid `3f2652a776644b439721b6da1e4c0f12`）：入口 `TestEngineE2`（`:26`），4 个用例。

## 断言（全部写死字面量；每条带 🧨）

- `E2T1PerOpFreshness`：上一张卡打 3 个 ⇒ 本条 ability 第 1 条只打 1 个 ⇒ 第 2 条（探针卡 `mult` 行 `ta=150`，正文数=3）**只掉 1 血**。改回「只在出口同步」= 3 血；结构化乘数没接上 = 3 血 ⇒ 都红。前面另有一条独立钉 `CountEval` 的 `ta=150` = 3（把失败点拆开）。
- `E2T2UnionAcrossOps`：一条 ability「先全体 3、再一个 1」⇒ 槽 = **3**（旧口径 1 ⇒ 红）；下一条 ability 只打 1 ⇒ 槽回 **1**（起手不清 ⇒ 3 ⇒ 红）。
- `E2T3EvtKindNotSilent`：全枚举登记齐全（缺一档 ⇒ 红）· `(EvtKind)999` ⇒ `IsSourced=false`+带原因（旧 `default:true` ⇒ 红）· `SourceOf` 含「未登记」· 计数上涨 · 已登记档（`ShowCardsWindow` / `Hit`）读数不变。
- `E2T4Um77RealCard`：**真卡 `UM77.Desc`** 解析出 `diedsincelastturn` + `own|troop`（旧口径会是 `died` ⇒ 红）；计数 本回合 1 → 过一回合**仍 1** → 再过一回合 0；对照 `died` 在 Turn2 = 0（合并两条 scope ⇒ 至少一条红）。

## 验证

- `TMPDIR=/tmp/wf_e2 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**。
- ⛔ 按红线**没跑 Unity**（4 个用例只有静态把关）· 没跑 ruleprobe · 没动 git。
- `git diff --numstat`：`EffectResolver.cs 65/16` · `BattleContext.cs 31/5`（CRLF 未翻，逐行核对 `\r\n` 数 = `\n` 数）。

## 需要主对话做（都在 E2 白名单外）

1. `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加 `Step(TestEngineE2);` —— **不加的话 `:1157` 那条覆盖闸会红并点名该方法**。
2. `工具/gen_diag_runner.py` 重生成 `RuleEngineTest_Diag.cs` —— 否则 `:1177` 的「两个口同口径」闸会红。
3. **两处过期注释**（E2 白名单外）：`CountEval.cs:411` 写「写点 = `ResolveOps` 出口」、`CountEval.cs:432-436` 的「必须先判抽牌」理由整段建立在「结算中读到的是上一张卡残值」上（现在不成立了；推理结论仍对，但依据要改）；`Data/AiTargets.cs:395` 写「取的是 `ctx.LastTargets`」。
4. 可选：重跑 `RuleEngineTest.Run` 后重钉 `play` 基线（本件对 `ta=150` 的真实消费行 0 条 ⇒ 预期 `play` 零变化）。

## 没查清 / 残留近似（都写进代码注释）

- 原版 `abilityTargetsToSave` 可装**手牌/牌库的卡**，我们只装 `UnitState`（抽牌那半边由 `DrawnThisResolve` 单独建模）。
- **嵌套 `ResolveOps`（监听器触发）会清掉外层并集** —— 与原版「嵌套 ability 有自己的新表」方向一致，但外层已解出的目标会丢一截；今天 `ta=150` 消费行 0 条 ⇒ 零可观测。
- **触发路入口的 `prime`（触发主体）也会进并集**（原版表里通常本来就有它）⇒ 已标注，零可达。
- `DiedLastTurn` 这个字段**生产侧没人读**（唯一读者是 `RuleEngineTest_EngineA1.cs:166` 自己）—— 真正的消费者走 `DeadUnits`+`DeathTurn`。

## 顺手发现（⛔ 只报不改）

1. 🔴 `BattleContext.DiedLastTurn` 是**生产侧只写不读**的字段（写点 `RuleCore.cs:1561`，读点只有自检）⇒ 它的断言接近「自证」形状。真正让 UM77 正确的判据是 `DeadUnits.DeathTurn`。建议要么接线、要么在 doc 里写明「只为自检保留」。
2. `CardPresentation/Core/EventTiming.cs:770-778` 有一条注释署名 **`E2` · `DG2` 红 #1~#3** —— 不是本批（1024）的 E2，说明 `IsSourced` 已被另一路改过；本批**没有交集**，只读了它。
3. E8 报告 §六·2 写「共 27 处 `param_1 + 0x80` 引用」，本代理数的同步口是 **48 处跳 `LAB_18081cc87` / 41 个 case**（27 应是 `+0x80` 的不同用法数）。结论同向，只是计数口径不同。
4. `RuleEngineTest_EngineE2.cs` 落在 `CardPresentation/Editor/`（按简报指定），与其余 `RuleEngine/Editor/RuleEngineTest_*.cs` 不同目录 —— 无 asmdef，两者同在 `Assembly-CSharp-Editor`，已用 typecheck 证实可见。
