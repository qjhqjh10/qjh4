# B9 · D3OT —— `BattleScene` / `RuleEngineTest` 之外全部红的定性（只读诊断）

> 诊断代理 `D3OT`，2026-10-27。**只读**：没改任何 `.cs` / 账 / 正本，没跑 Unity。
> 判据一律按【行首标记 + 断言合计】数，⛔ 没用 `grep -c ✗`（`✗` 大量出现在断言文案里）。

⚠️ **读数快照的时效**：6 份日志的写入时间是 **07:44–07:48**，而 `RuleEngineTest.cs` / `RuleEngineTest_Diag.cs` /
`RuleEngineTest_EngineD4.cs` 的 mtime 是 **07:57**（引擎代理在那之后还动过）⇒ **下面的读数不是当前工作树的终态**，
重跑之前别把它们当"现在"。

---

## ① 逐条表

| # | 宿主 | 断言原文（截断） | 文件:行 | 分类 | 证据 | 该怎么改 / 哪笔改动 / 缺什么 |
|---|---|---|---|---|---|---|
| A | **全部 6 个宿主** | `RE ✗ [诊断入口过期] … 反射看到 357 个用例方法，产物只认 356 个（缺 1 / 多 0）；缺：TestB9EngForcedAttackAndShuffleGat…` | `RuleEngine/Editor/RuleEngineTest_Diag.cs:223`（`DiagDriftAlarm`，`[InitializeOnLoadMethod]`） | **(α) 产物过期**（生成器产物落后于源码）· **工作树里已修** | 宿主日志 `menu.log:259` / `netbattle.log:259` / `cardface_e5.log:259` / `shell.log:261` / `deck.log:261` / `collection.log:259` 各一条。缺的那条用例确实在源码里：`RuleEngineTest.cs:119` `Step(TestB9EngForcedAttackAndShuffleGate);` + `:38373` 定义。`RuleEngineTest_Diag.cs`（生成物）mtime 10-26 19:18 < 那两条。**现在已重生成**：该文件 mtime 变 **10-27 07:57**，且里面已出现 `TestB9EngForcedAttackAndShuffleGate`（2 处） | **不用改代码**，下次跑就会消失。若要立刻确认：`PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`（工具在 `Unity/工具/gen_diag_runner.py`），或直接看下一轮日志。⚠️ 它**不计入任何宿主合计**（`:199-206` 的 doc 明写"只报警、不碰任何计数"）⇒ 见 ④-4 |
| B1 | `MainMenuScene` | `★★ In Alliance` 的直接子件 … 期望 `[Alliance Name\|*View Leaderboard Button\|Alliance Event Score Info]`，实得 `[…\|View Leaderboard Button\|…]`（`*` = 自己 `activeSelf=false`） | 断言 `Editor/MainMenuScene.cs:16122` | **(β) 实现缺陷**（已知未实现 = 账 128 / 界面线 `J-05`）· **不是回归** | 断言行内注释就是判据：`leaderBoardID 空 ⇒ 两钮 self activeSelf=false（AlliancesEventScorePanel__Initialize.c:123-137）⇒ 期望值＝目标态；J-05 落地前这两条**预期红**`。上一会话已是同样 2 红：`资料/交接_第八十四会话.md:39`、`:217`；`资料/任务线/界面线/任务总表.md:376` | 落地 `J-05`：`Shell/AllianceEventScorePanel.cs` 的 `Initialize` 读 `leaderBoardID`，空 ⇒ `_viewLb`(→`:122`) 与 `_noLb`(→`:173`) 各自 `SetActive(false)`。改完这两条自动转绿（`DirectKids` 会给关着的件加 `*`） |
| B2 | `MainMenuScene` | `★★ No Alliance` 的直接子件 … 期望 `[…\|*Leaderboard Button]`，实得 `[…\|Leaderboard Button]` | 断言 `Editor/MainMenuScene.cs:16124`（期望串在 `:16125`） | **(β)** 同上 | 同上 | 同上（同一次 `J-05` 落地一起转绿） |
| C | `MainMenuScene` | `[U2b] FAIL (V1) 没装备过（−1）⇒ **默认装备清单第 1 条**（实得 TitleIndex = 0）` | 断言 `Editor/ProfileAssertU2b.cs:105-109`；**病根** `Shell/ProfileData.cs:365` | **(γ) 本批回归** | `ProfileData.TitleIndex` 的 **getter** 本批（mtime 10-27 07:17）加了读口兜底：`get { EnsureSave(); if (_titleIndex < 0 && Titles.Count > 0) _titleIndex = 0; return _titleIndex; }`。而 `ProfileTab.EnsureLocalDefaultTitle()`（`Shell/ProfileTab.cs:538-556`）在 `:547` 读 `int cur = ProfileData.TitleIndex;` ⇒ 被兜底成 0 ⇒ `cur >= 0` ⇒ **返回 false** ⇒ 断言的 `seeded &&` 那半永远为假（后半个 `TitleIndex == 0` 其实是对的）。读数对照：上一会话 `MainMenuScene` **4,838 / 2** → 本轮 **5,026 / 3**，多出来的正是这条（子类总计 `[U2b] === U2b 合计：57 通过 / 1 失败 ===`，`menu.log:147773`；失败原文 `menu.log:146914`） | ⚠️ **要调度台定口径**（两种改法语义不同）：**(a) 保持"两处兜底"但让判据看得见原始值** —— 给 `ProfileData` 加一个原始读口（如 `TitleIndexRaw` / `HasEquippedTitle`），`EnsureLocalDefaultTitle` 用它判 `-1`；**(b) 认账"唯一兜底已挪到读口"**，把断言收成 `Check(ProfileData.TitleIndex == 0, …)` 并删掉/降级 `EnsureLocalDefaultTitle`。🔴 **附带真问题**：读口兜底落地后，`EnsureLocalDefaultTitle()` 对"没装备过"这一档**永远早退**（成了死码）—— 与 CLAUDE.md「两处写同一条规则 = 迟早不一致」同族，建议按 (a) 或 (b) 收口到一处 |
| D1 | `NetBattleTest` | `✗ ★ **重放之后客机的状态追平了主机**（这是「掉线能接着打」的唯一判据）（实际 -934489277，应为 -934427773）` | `Editor/NetBattleTest.cs:445`（文案 `:446`） | **(γ) 本批回归** | **同一宿主同一批断言，今天 00:37 时是全绿的**：`_tmp_view/fix_NetBattleTest.Run.log`（10-27 00:37）= `===== 通过 247 · 失败 0 =====`，且 `重连前后：主机 1409444004 · 客机(掉线前) 1409444004 · 客机(重放后) 1409444004` 三值全等；今天 `netbattle.log` = `===== 通过 244 · 失败 3 =====`（**总数 247 不变**，恰是 3 条翻红，不是新增/丢失断言）。日志三值 `netbattle.log:99415`：主机 −934427773 · 客机(掉线前) −934427773 · 客机(重放后) −934489277 | 见下方「D 的判别式」 |
| D2 | `NetBattleTest` | `✗ ★ 重放出来的状态**与掉线前一致**（重放是确定性的）（实际 -934489277，应为 -934427773）` | `Editor/NetBattleTest.cs:447` | **(γ)** 同 D1 | 同上 | 同上 |
| D3 | `NetBattleTest` | `✗ ★ **主机掉线这一支**：重连后客机也追平了主机（实际 -934489277，应为 -934427773）` | `Editor/NetBattleTest.cs:467`（文案 `:468`） | **(γ)** 同 D1 | `netbattle.log:99926`：`主机掉线前后：主机 −934427773 · 客机(掉线前) −934489277 · 客机(重放后) −934489277` ⇒ **重放是确定性的**（两次都 −934489277），差异是"live 落出来的局面"与"重放落出来的局面"系统性不同 | 同上 |
| E | `CardFaceAssertE5` | `BT T ✗ ★ T-5：表里**没有**小写 Cardmove（…Cardmove.json 是另一个 1-clip cue，本笔不要它） —— 🧨 把 EXTRA_CUES 写成 Cardmove ⇒ 本条红（顺带 T-2 也红）` | 断言 `Editor/SoundCueAssertT.cs:96`；**病根** `Resources/animfx_sounds.json` + 生成器 `工具/import_original_sfx.py:276` | **(γ) 本批回归** | 本批 `animfx_sounds.json` **+440 行 / −0 行**（`git diff --numstat`），其中新增一条 `{"name": "Cardmove", "clips": ["CardMove"], "minPitch": 1.0 …}`（在 diff 的 `@@ -754,4 +853,15 @@` 那块）。来源是 `import_original_sfx.py` 清单里 2026-10-27 `B2AU` 第九批加的一行 `"Cardmove", "Draft Mode Loss Jingle", "Draft Mode Warlord Appear",`。⚠️ 它的注释把依据写成「我们代码侧按名字播它：`SoundCueAssertT.cs:77`」，**而 `:77` 恰恰就是禁止这条 cue 的那条断言的文案**（"或把 `Cardmove`（小写 m）那条拿错 ⇒ 红"）⇒ 误读。全仓 `grep '"Cardmove"'` 只命中 `SoundCueAssertT.cs:96`（负例断言本身），**零生产消费点**；真正的换位 cue 是 `CardDisplayWindow.SwapSoundCue = "CardMove"`（大写 M，`Battle/CardDisplayWindow.cs:249`） | 从 `工具/import_original_sfx.py:276` 那一行里**删掉 `"Cardmove"`**（保留另两条 —— 它们有真消费点 `Shell/DraftModeWindow.cs:2648` / `:3536`），重跑生成器重建 `animfx_sounds.json`（把那条条目去掉）。改完 `T-5` 与 `T-2` 都绿 |
| F | `ShellScene` | 合计全绿但 **rc=1**：`Can't add script behaviour 'ProbeTab' because it's an editor script.` → `NullReferenceException … ShellScene.cs:10341` → `executeMethod method ShellScene.Run threw exception` | 崩溃点 `Editor/ShellScene.cs:10340-10341`（段头 `:10333`）；`Editor/ProbeTab.cs:14`（`sealed class ProbeTab : WindowTabBase`，**住在 `Assets/CardPresentation/Editor/` = 编辑器程序集**） | **(β) 实现缺陷（自检宿主自身）· 不是本批** | 见 ③ 节 | 见 ③ 节 |

### D 的判别式（给接手 NetBattleTest 的执行代理）

已知三条硬事实：

1. **客机 live 的那 14 条动作，落地调用与重放是同一套** —— 9 条真动作 + `Mulligan`×2 + `MulliganDone` 全部经 `NetApply.Apply`（夹具 `BareHost.ApplyLoggedAction` 就是 `NetApply.Apply`，`Editor/NetBattleTest.cs:66-68`；对打循环里本机那一条也是 `NetApply.Apply(ctx, wire, …)`，`:392`；对端来的那一条经 `NetKind.Applied` → `ApplyLoggedAction`）。**2 条环境卡**（`OffensivePick` / `DefensivePick`）在**出手那一侧**走的是直接调 `RuleCore`（`:283-287`），收侧走 `NetApply` —— 而 `NetApply` 那两支做的正是同一组调用（`Net/NetApply.cs:100-113`：`ChooseOffensiveCard` / `ChooseDefensiveCard` + `FindCard`(`:279` 返 `CardDef`) + `SetDefensiveCard`(`RuleCore.cs:563` 收 `CardDef`)），且 live 两端指纹相等 ⇒ **这两条不构成差异来源**。
2. **重放用的也是同一个 `NetApply.Apply`**（`Editor/NetBattleTest.cs:109-117`），动作来自 `hNB.Log`（14 条：`Mulligan`×2 → `MulliganDone` → `OffensivePick` → `DefensivePick` → 9 条真动作；其中 `kind=5 ActiveAbility[oath]` 在 live 与重放里**同样被拒**，`netbattle.log:98764` vs `:99329`）。
3. live 两端每一步指纹都相等（对打循环里的逐步对账没报分叉），**重放却确定性地落到另一个局面**。

⇒ 结论：**动作流已经不能唯一决定局面** —— 某处结算依赖了"动作流之外的进程级状态"，而重建出来的是本进程里**第 3 局**（第 1/2 局是 hostCtx / cliCtx）。

最短排查路（按顺序，前两步都不要动代码逻辑，只加读数）：

1. **先重跑一次**（`NetBattleTest.Run` 约 15 秒）。该宿主历史上有"起点 4 红、重跑即绿 = 抖动"（`交接_第八十四会话.md:44`）。若这次绿，本条判为抖动，收工。
2. 仍红 ⇒ **把 `Rebuild` 也套到主机上**：`hb.Ctx = cb.Rebuild();` 之后取 `Fingerprint(hb.Ctx)` 与 live 的 `hBefore` 比。
   · 重建后的主机也得到 **−934489277** ⇒ 问题在**"第 3 局 ≠ 第 1 局"的进程级状态**（构造期行为 / 静态缓存）；
   · 仍是 **−934427773** ⇒ 问题在**重放路径**特有的差异（`preApplied` / `PendingSetupBegin` / 面板答案 `picks` 的取舍）。
   这一步把面砍一半，再往下查。
3. 重点看本批 06:44–07:30 新加/改动的**构造期**行为：
   · `RuleEngine/Core/BattleContextRandom.cs:46-50` `InitRandomNumbers()` —— **构造时就消费 100 个 `Rng` 数**（`BattleContext.cs:681` 那行），游标 `RandomNumbersIndex` 起步 0；
   · `RuleEngine/Core/CardInstance.cs` 构造里新增 `ArmedTrap = TrapArmedOf(card)` —— **每造一张牌都调一次 `CardDef.IsTrapCard`**；
   · `RuleEngine/Core/PlayedThirdCardInTurn.cs`（新触发口 `FirePlayedThirdCardInTurn`，本轮对局里 `PlayCard` 恰好 3 次 ⇒ `CardsPlayedInTurn == 3` 那一闸**真会命中**）。
   （本批同一时间窗内被改且进结算的还有：`RuleCore.cs`(07:15) · `EffectResolver.cs`(07:30) · `Aura.cs`(07:08) · `Enchantment.cs`(07:09) · `CardDef.cs`(07:19) · `CountEval.cs` · `BattleEvent.cs` · `AchievementTally.cs` · `TutorialScript.cs`。）

---

## ② `DeckScene` / `CollectionScene` 到底跑没跑 —— **都跑了，而且都跑完、全过**

| 宿主 | 结论 | 证据 |
|---|---|---|
| `DeckScene` | **跑完 · 失败 0** | `deck.log` 尾段（最后一条 `DK` 行之后）：`DK === 结束：1856/1856 全过 ✅ ===`。源码 `Editor/DeckScene.cs:1343`：`if (_sink.Fail == 0) Debug.Log(P + $"=== 结束：{_sink.Pass}/{total} 全过 ✅ ===");` |
| `CollectionScene` | **跑完 · 失败 0** | `collection.log` 尾段：`[Collection] === 结束：2439/2439 全过 ✅ ===`（源码 `Editor/CollectionScene.cs:12630` 同上）。两条读数与 `交接_第八十四会话.md:37,42`（1,856 / 2,439）逐字一致 |

**为什么找不到"断言合计"**：这两个宿主用的是 **`=== 结束：{Pass}/{total} 全过 ✅ ===`** 这套文案（**失败时**才换成另一句 `=== 结束：… 通过，**N 条失败** ❌ ===`，见 `DeckScene.cs:1346` / `CollectionScene.cs:12633`）—— 里面**没有"合计"两个字**。日志里唯一含"合计"的那行是 `collection.log` 的 `合计 30 张；这副的卡都算进去了`（一句文案），不是断言合计。

**`deck.log` 里那 6 条 `✗` 一条都不是失败**（按行首标记逐条核对过）：

| 行 | 性质 |
|---|---|
| `:122832` / `:122847` | 在 **`✓` 断言的文案里**（"原来是 37.96 的居中留白 ✗" / "把卡画满整格的老口径 ✗"），不是失败标记 |
| `:157292` | **A1007 判别式探针故意打出的一条 ✗**：它先造出"没有 `Camera.main`"的上下文，验 `MenuCheck.Shoot` **必须出声**；这条 ✗ 记进的是**探针自己的子 sink**（前缀 `>>> A1007 探针`），不是宿主 sink ⇒ 不进总数 |
| `:157305` | 探针的**通过**判据本身：`✓ ★ 无相机 ⇒ MenuCheck.Shoot 必须【出声】：自己的 sink 上**恰好 1 条 ✗**（实得 1）` |
| `:157348` | 探针把子 sink 里那条 ✗ 的原文再打印一遍 |

⚠️ 附带：`deck.log` / `collection.log` 都**没有** `Application will terminate with return code N` 这一行（只有 `cardface_e5.log` / `shell.log` 有）⇒ 这两个宿主的"退出码 0"只能从运行脚本拿，**日志内证是那句"结束：N/N 全过"**。

---

## ③ `ShellScene` `rc=1` 的原因 —— **宿主崩了，合计根本没打印**（不是"全绿但 rc=1"）

`shell.log` 里唯一那条 `[E15] 合计：通过 59 / 失败 0` 是**子断言类 `SkirmishWindowAssertE15` 的总计**
（`Editor/ShellScene.cs:8463` 把它折进宿主 sink），**不是宿主合计**；宿主合计在 `ShellScene.cs:10581`
（`=== 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败 ===`），**在崩溃点之后 ⇒ 从未执行**。

崩溃链（`shell.log` 末段，逐句可读）：

```
Can't add script behaviour 'ProbeTab' because it's an editor script. To attach a script it needs to be outside the 'Editor' folder.
UnityEngine.GameObject:AddComponent<ProbeTab>()   ← Editor/ShellScene.cs:10340
NullReferenceException  … ShellScene.RunCore () … ShellScene.cs:10341
executeMethod method ShellScene.Run threw exception.
Application will terminate with return code 1
```

- 落点：`Editor/ShellScene.cs:10333-10341`（`④ 页签生命周期（GameWindowWithTabs / WindowTabBase / TabButtons）` 段），
  `var a = new GameObject("TabA").AddComponent<ProbeTab>();`（`:10340`）返回 **null**，紧接着 `a.type = …`（`:10341`）抛 NRE。
- 根因：`Editor/ProbeTab.cs:14` 的 `sealed class ProbeTab : WindowTabBase` **住在 `Assets/CardPresentation/Editor/`**
  ⇒ 属编辑器程序集 ⇒ **Unity 按定义拒绝把它挂到 GameObject 上**（`WindowTabBase` 是 MonoBehaviour）。这条不是偶发。
- **已跑了多少**：`shell.log` 里 `[Shell] ✓` 行 **1,963 条**、`[Shell] ✗` **0 条**；
  崩溃点到合计（`:10341` → `:10581`）之间还有 **72 处断言调用点**（其中在循环里 ⇒ 实际条数更多）**一条都没跑**。
- **不是本批**：`ShellScene.cs` mtime **10-26 23:04**、`ProbeTab.cs` mtime **10-26 18:15**，两个文件的 `git status` 都是**未修改**（已在 `bd58de33` 提交）。
  更早的同族证据：`_tmp_view/wf_ShellScene.Run2.log`（**10-26 18:31**）同样是 ProbeTab 崩 + 无合计 + `return code 1`（那次崩在 `:10257`）；
  而 **10-26 09:11** 的 `_tmp_view/wf_ShellScene.Run_b.log` 是 `[Shell] === 合计：2070 通过 / 0 失败 ===`
  ⇒ **这段 ④ 是 10-26 白天之后才加进来的，加进来当天就崩，此后没有一次成功**。
- 改法（两选一，需调度台定）：
  **(a)** 把 `ProbeTab.cs` 移出 `Editor/`（放进运行时装集，如 `Shell/`，类头注明"仅自检替身"）——
  改动最小、`AddComponent` 立刻可用；代价是运行时装集多一个测试类型。
  **(b)** 不新增类型：用运行时装集里**已有**的某个 `WindowTabBase` 子类当替身（如 Missions/Forge 页的真实 Tab 类）——
  不动程序集划分，但要确认它的 `Build()` 不会在批处理里做重活。
  ⚠️ 无论走哪条，**改完必须重跑整个 `ShellScene.Run`** —— `④` 之后那 72 处断言从未被跑过，里面可能还埋着别的红。

---

## ④ 顺手发现（⛔ 都不是要我修，交给调度台分流）

1. **四套"合计"文案并存 → 这就是"找不到断言合计"的根因**（本轮实测四种）：
   · `=== 合计：{Pass} 通过 / {Fail} 失败 ===` —— `MainMenuScene`(`:18269`)、`ShellScene`(`:10581`)
   · `=== 结束：{Pass}/{total} 全过 ✅ ===`（失败时才换另一句）—— `DeckScene`(`:1343`)、`CollectionScene`(`:12630`)
   · `===== 通过 {Pass} · 失败 {Fail} =====` —— `NetBattleTest`(`:1711`)
   · `合计：通过 {Pass} / 失败 {Fail}` —— 各**子断言类**（如 `SkirmishWindowAssertE15`、`U2b`）
   建议：`_run_8_checks.sh`/读数脚本里按宿主写死匹配式（或统一文案）。CLAUDE.md 已写"各条合计格式不统一，逐条认"，本轮就是它咬人的现场。
2. **`MainMenuScene` 折子断言类时只折了 `Pass`/`Fail`，没折 `Failures`** ⇒ 结尾"失败重列"会**漏条**：
   本轮 `_sink.Fail == 3`（`menu.log:218040`）而"失败重列"只印了 **2** 条（`menu.log:218052`、`:218064`），
   漏掉的正是 `[U2b]` 那条（它只体现在子类自己那行 `[U2b] === U2b 合计：57 通过 / 1 失败 ===`，`menu.log:147773`）。
   折叠点：`Editor/MainMenuScene.cs:8634, 8639, 8643, 8645, 8650, 8655, 10373, 10378, 10384, 10486, 10492, 10497, 10503`。
   ⇒ 谁要是只看"失败重列"，会**少判一条红**。
3. **`交接_第八十四会话.md` §1·b 与本轮实跑对不上（铁律 5：应就地改）**：

   | 宿主 | 交接写的 | 本轮实读 |
   |---|---|---|
   | `ShellScene` | `2,022 / 0 ✅`（`:38`） | **崩溃 rc=1**，1,963 ✓ / 0 ✗ 后死掉（且 10-26 18:31 起就崩） |
   | `CardFaceAssertE5` | `66 / 0 ✅`（`:48`） | `E5 合计：✓ 63 / ✗ 1（共 64 条）` ⇒ **总数是 64 不是 66**（多出的 2 条对不上，需原主核） |
   | `NetBattleTest` | `247 / 0 ✅`（`:44`） | `244 / 3`（总数仍 247 ⇒ 3 条翻红） |
   | `MainMenuScene` | `4,838 / 2`（`:39`） | `5,026 / 3`（+188 通过 —— 更多段跑到/更多数据驱动断言；+1 失败 = ① 的 C 条） |
4. **"诊断入口过期"那条 `LogError` 两头不落地**：`RuleEngineTest_Diag.cs:199-206` 明写"只报警、不碰任何计数"
   ⇒ 本轮 **6 份日志里每份都有一条真 `✗`**，而 `DeckScene` / `CollectionScene` 照样报"全过"、`ShellScene` 崩溃前计数失败也是 0。
   · 按"日志里有没有 `✗`"判绿红 ⇒ 被它骗（永远显示红）；
   · 按断言合计判 ⇒ 永远看不到它。
   建议读数脚本把这一条**单列**（`[诊断入口过期]` 关键字），别混进宿主合计数。
   📌 结论层面它**已经在工作树里修掉了**（`RuleEngineTest_Diag.cs` 07:57 重生成，见 ① A）。
5. **42 处注释仍按「结算随机走 `ctx.Rng`」的语气写，而代码已换成 `ctx.GetRandomInt`**（铁律 5的「文档与事实不一致」，**不是代码缺陷**）：
   本轮扫 `RuleEngine/Core/*.cs` 的注释行，提到 `ctx.Rng` 的共 **42 处**（`EffectResolver.cs` 20 · `RuleCore.cs` 9 · `EffectText.cs` 4 · `CreatePool.cs` 3 · …）。
   其中一部分是**合法的历史注记**（如「…2026-10-27 `E1RNG` 前是 `ctx.Rng`」、「`Rng` 现在只当烧表用的种子流」）；
   但两处是**直接描述当前代码、而且描述错了**：`EffectResolver.cs:2927`「`1-3` 区间…用 ctx.Rng 保证可复现」紧跟着下一行代码是 `ctx.GetRandomInt(op.Amount, op.AmountMax + 1)`；`:3509` 同样。
   ⇒ 建议与 `E1RNG` 那一批一起清（当前代码侧**无遗留**：代码级 `ctx.Rng.Next` 全仓 0 处，`Editor/` 里那几处是**探针故意读**它）。

6. **本轮读数是 07:44–07:48 的快照**：`RuleEngineTest.cs` / `RuleEngineTest_Diag.cs` / `RuleEngineTest_EngineD4.cs` 在 **07:57** 还被改过
   ⇒ 这 6 个宿主的读数（尤其 `RuleEngineTest` 那一侧）**都不是当前树的终态**，同步点复跑要重新算。

---

## ⑤ 一句话结论

- **A（诊断入口过期）** = 产物过期，**已在工作树修好**，下次跑即消失（不计入合计）。
- **B1/B2（`MainMenuScene` 那 2 条）** = **有据的预期红**（账 128 目标态，等界面线 `J-05` 落地），**不是回归**。
- **C（`MainMenuScene` 多的那 1 条）** = **本批回归**，根因是本批给 `ProfileData.TitleIndex` 加的**读口兜底**让 `EnsureLocalDefaultTitle()` 的判据失效（两处兜底并存，其中一处已成死码）。
- **D1/D2/D3（`NetBattleTest` 3 条）** = **本批回归**（00:37 还是 247/0），且是"重放确定性、但与 live 系统性不同"⇒ 动作流不再唯一决定局面；本批 06:44–07:30 的引擎改动（`InitRandomNumbers` 构造期预烧 / `ArmedTrap` 造牌期计算 / `PlayedThirdCardInTurn` 新触发口）是首要嫌疑。
- **E（`CardFaceAssertE5` 1 条）** = **本批回归**，`B2AU` 把断言明令禁止的 `Cardmove` cue 收进了表（删那一行、重跑生成器即可）。
- **F（`ShellScene` rc=1）** = **宿主崩溃（非本批，10-26 18:15 起）**：`Editor/` 里的 `ProbeTab` 永远 `AddComponent` 不上 ⇒ NRE ⇒ 合计从未打印，`④` 之后 **72 处断言从未被跑过**。
- **`DeckScene` / `CollectionScene`** = **都跑完且全过**（1856/1856、2439/2439），只是合计文案是"结束：N/N 全过"；`deck.log` 里 6 条 `✗` 全是文案与**故意**的 A1007 探针。
