# B94_R-T · 全表 B 行「我们的落点」体检

> 产出：只读账务代理 R-T（第九十四会话 · 波 3）。**本文件是唯一产物，未改任何源码 / 账 / 正本。**
> 快照时间 ≈ **2026-10-29 01:48**。⚠️ 审计期间账文件**被其他代理并发改写**（现读 mtime：`战斗逻辑驱动.tsv` 01:47:32 · `其他_基础设施.tsv` 01:41:19 · `未分类_其他.tsv` 01:41:56 · 其余 01:32–01:33）⇒ **本表是某一刻的快照**，同步点后应复跑。
> 方法：脚本解析每行 `我们的落点` 列（首处 + 第 2/3 处 `文件:行`），用**括号配平的 C# 外壳解析器**判定该行所属的声明（方法/属性/字段/类），再与 `方法` 列 / ref 自述方法名对账；无法自动判定的**逐条回源读代码**确认。

---

## §一 汇总（按账文件 × 五档）

B 行总数 **258**（14 个账文件）。判定五档：成立 / 行号漂了 / 指错方法 / 文件不存在 / 判不了（含无落点、bare、散文）。

| 账文件 | B 行 | 成立 | 行号漂了 | 指错方法 | 无落点(声明) | bare | 散文 |
|---|---:|---:|---:|---:|---:|---:|---:|
| 个人资料_成就_排行.tsv | 6 | 5 | 1 | 0 | 0 | 0 | 0 |
| 其他_基础设施.tsv | 2 | 1 | 1 | 0 | 0 | 0 | 0 |
| 商店.tsv | 15 | 13 | 0 | 0 | 2 | 0 | 0 |
| 战斗界面_HUD.tsv | 2 | 0 | 0 | 0 | 2 | 0 | 0 |
| 战斗逻辑驱动.tsv | 127 | 112 | 2 | 5 | 6 | 3 | 0 |
| 教程_引导.tsv | 2 | 1 | 0 | 0 | 1 | 0 | 0 |
| 日常_奖励.tsv | 56 | 51 | 4 | 0 | 0 | 0 | 1 |
| 未分类_其他.tsv | 20 | 15 | 0 | 2 | 3 | 1 | 0 |
| 泛型与启动链_补读.tsv | 5 | 5 | 0 | 0 | 0 | 0 | 0 |
| 特效动画.tsv | 7 | 6 | 0 | 0 | 1 | 0 | 0 |
| 社交_聊天.tsv | 3 | 3 | 0 | 0 | 0 | 0 | 0 |
| 菜单_外壳.tsv | 4 | 0 | 0 | 0 | 0 | 1 | 3 |
| 轮抽.tsv | 8 | 7 | 1 | 0 | 0 | 0 | 0 |
| 锻造.tsv | 1 | 1 | 0 | 0 | 0 | 0 | 0 |
| **合计** | **258** | **221** | **9** | **7** | **15** | **5** | **4** |

> 注：「指错方法」含 2 处工具假阳性（`1367` 实为正确、`273` 仅判不了），逐条见 §二/§四。
> **真正需要改账的 = 13 行**（§三 可直接改）。**「无落点(声明)」15 + 散文 4 = 19 行**是**诚实的「没有落点」**（未实现 / 阻塞 / 原版本就无对应物），**不是错**，但见 §五 关于「B 行无落点也该留 blocker」的建议。

---

## §二 逐条异常清单（只列需要看的）

### A. 行号漂了（方法找得到，行号点偏到别处/注释行）

| 账文件:行号 | 原版类.方法 | 现落点 | 体检发现 | 建议正确落点 | 证据 |
|---|---|---|---|---|---|
| 个人资料_成就_排行.tsv:198 | `RankedArmyBonusContainer.OnInitialize` | `MainMenuRuntime.cs:2807（BuildModeCard）` | 2807 落在注释行；`BuildModeCard` 声明在 2822 | `MainMenuRuntime.cs:2822` | `grep -n "void BuildModeCard" MainMenuRuntime.cs → :2822` |
| 其他_基础设施.tsv:460 | `PlayerDataManager.QuitApplication` | `MainMenuRuntime.cs:4351（QuitApplication）· :1746 · :4378` | 4351 在 `<para>` 注释里；decl 在 4375。**且两处次要落点也漂**：`:1746`→注释（`QuitGame` 声明 1761）、`:4378`→注释（`FullyQuitApplication` 声明 4402） | `:4375（QuitApplication）· :1761（QuitGame 转调）· :4402（FullyQuitApplication 协程）` | `grep -n "public static void QuitApplication()" → :4375`；`QuitGame() → :1761`；`FullyQuitApplication() → :4402` |
| 战斗逻辑驱动.tsv:193 | `BattleManager.AllowResolveAttack` | `RuleCore.cs.CanAttackNow（:5693）· :5267-5510（IsValidTarget）` | 5693 在 `<summary>` 注释里；`CanAttackNow` 声明在 5706。（次要区间 `:5267-5510` **覆盖**了 `IsValidTarget` 声明 5392 ⇒ 次要落点OK） | `RuleCore.cs:5706（CanAttackNow）· :5392-5510（IsValidTarget，声明 5392）` | `grep -n "public static int CanAttackNow" → :5706`；`IsValidTarget → :5392` |
| 战斗逻辑驱动.tsv:553 | `BattleManager.ResolveTriggerMob` | `RuleCore.cs:12496 FireTriggerAt(Mob)` | 12496 在注释里；`FireTriggerAt` 声明在 12540 | `RuleCore.cs:12540（FireTriggerAt）` | `grep -n "static bool FireTriggerAt" → :12540` |
| 日常_奖励.tsv:470 | `MissionContainer.SetChallenge` | `MissionsTab.cs:610（BuildDailyRow）` | 610 是 `/// 内部锚点…` 注释；`BuildDailyRow` 声明在 611 | `MissionsTab.cs:611` | `grep -n "public void BuildDailyRow" → :611` |
| 日常_奖励.tsv:549 | `OpenEventButton.SetButtonInfo` | `MainMenuRuntime.cs:3780（TryGetEventNavInfo）· :4440（StepEventNavTimers）` | 3780 在别的语句里、`TryGetEventNavInfo` 声明在 3804；`:4440` 是注释、`StepEventNavTimers(float)` 声明在 4464（无参重载 4461） | `:3804（TryGetEventNavInfo）· :4464（StepEventNavTimers）` | `grep -n "TryGetEventNavInfo" → :3804`；`StepEventNavTimers → :4464` |
| 未分类_其他.tsv:708 | `UIResourceCounter.SetQuantity(…,animated)` | `MainMenuRuntime.cs:2177 CounterText` + `:2257（0.5s 滚动）` | 2177 落在 `MaxOf` 体内、`CounterText` 声明在 2192；`:2257` 是 `RefreshTopBarCountersIfChanged`（不是滚动本体）。**滚动的真身是 `TopBar.StepCounters`（TopBar.cs:601），由 `Update` 每帧泵** | `MainMenuRuntime.cs:2192（CounterText）+ Shell/TopBar.cs:601（StepCounters）` | `grep -n "public static string CounterText" → :2192`；`TopBar.StepCounters → :601`（调用点 Update:2273） |
| 轮抽.tsv:6 | `DraftEventBase.Expire` | `DraftEventBase.cs:257（Expire）` | 257 是 `/// <summary>原版…Expire…` 注释；`public override void Expire` 声明在 277 | `DraftEventBase.cs:277` | `grep -n "public override void Expire" → :277` |

### B. 指错方法 / 指错落点（点在注释或别的方法上）

| 账文件:行号 | 原版类.方法 | 现落点 | 体检发现 | 建议正确落点 | 证据 |
|---|---|---|---|---|---|
| 战斗逻辑驱动.tsv:227 | `BattleManager.BroadcastUnitRegiment` | `RuleCore.cs:6695（FireTriggerAt(…,KeywordTable.Regiment,…)）· EffectResolver.cs:7372（triggers:regiment→OtherCardRegiment）` | **两处都指错**：`RuleCore.cs:6695` 是关于 `splashKills` 的注释，且 `RuleCore` 里**根本不存在** `FireTriggerAt(…,Regiment,…)`（全 engine grep 无）；`EffectResolver.cs:7372` 是 `ReportUnresolved` 函数体、与 regiment 无关。真身：`case KeywordTable.Regiment → OtherCardRegiment` 在 **EffectResolver.cs:7607**；`triggers:regiment` 的手牌两跳接线在 **EffectResolver.cs:10020**（`BroadcastUnitRegimentHandHops`） | `EffectResolver.cs:7607（case Regiment→OtherCardRegiment）· EffectResolver.cs:10020（BroadcastUnitRegimentHandHops 手牌两跳）` | `grep -n "case KeywordTable.Regiment" EffectResolver.cs → :7607`；`grep -n "BroadcastUnitRegimentHandHops" EffectResolver.cs → :10020`；`grep -rn "FireTriggerAt.*Regiment" RuleEngine/ → 无` |
| 战斗逻辑驱动.tsv:427 | `BattleManager.IsThereDoubleOathEffect` | `Aura.cs:707（誓约授予那一段…）` | 707 是注释（讲 `oathDouble` **不在**光环这一段、指向 `RuleCore.OathExtraReplays`）。等价实现在别处 | `RuleCore.cs:11685（OathExtraReplays = IsThereDoubleOathEffect 的等价物）` | `Aura.cs:707-709` 注释明写「见 `RuleCore.OathExtraReplays`」；`grep -n "public static int OathExtraReplays" RuleCore.cs → :11685` |
| 战斗逻辑驱动.tsv:645 | `BattleManagerSupport.BroadcastUnitAttacked` | `RuleCore.cs:6980（PreCallBoardListener ×2…）· :6984（attackType 显式传）` | 6980/6984 都是注释。真实代码：两条 `PreCallBoardListener(…Attack…)` 在 **6996 / 6999**；`BroadcastWhen(…attackType: ranged ? 2 : 1)` 在 **7005** | `RuleCore.cs:6996/+6999（PreCallBoardListener ×2）· :7005（BroadcastWhen(Attack) attackType）` | `sed -n '6990,7006p' RuleCore.cs`：6996、6999 两处 PreCallBoardListener，7005 处 attackType |
| 日常_奖励.tsv:229 | `ExpansionPassMissionsTab.OnClose` | `ExpansionPassEventWindow.cs:2878（ExpansionPassMissionsTabLogic.OnClose）` | 2878 是**同族助手** `ReleasesEventAssetsOnClose`（不是 OnClose 本体）。ref 自述的 `…Logic.OnClose` 真身在 2829 | `ExpansionPassEventWindow.cs:2829（OnClose(bool,bool)）` | `grep -n "static bool OnClose" → :2829`；`:2878 = ReleasesEventAssetsOnClose` |
| 日常_奖励.tsv:232 | `ExpansionPassMissionsTab.Reset` | `ExpansionPassEventWindow.cs:2881（…Logic.Reset）` | 2881 是助手 `ReleasesEventAssetsOnReset`。`Reset(bool,bool)` 真身在 2841 | `ExpansionPassEventWindow.cs:2841（Reset(bool,bool)）` | `grep -n "static ReleasePlan Reset" → :2841`；`:2881 = ReleasesEventAssetsOnReset` |

### C. 工具假阳性（**经复核正确，不需改**）—— 列出以免下个会话重复怀疑

| 账文件:行号 | 现落点 | 复核结论 |
|---|---|---|
| 战斗逻辑驱动.tsv:1367 | `ProfileTab.cs:1833 ResolveWarlordMastery` | ✅ **正确**：`:1833` 就是 `public static WarlordMasteryView ResolveWarlordMastery(`（工具因括号里带反引号没认出名字而误报） |
| 战斗逻辑驱动.tsv:253 | `RuleCore.cs.CheckUnitsCancellingAttack`（bare，无行号） | ✅ 方法**存在**：声明 `:16701`；私有助手 `HasCancelAttackAbility` 在 `:16734`。仅缺行号锚点（可加，非错） |
| 战斗逻辑驱动.tsv:411 | `TargetCriteriaEval.cs · Matches`（bare） | ✅ `Matches` 声明在 `TargetCriteriaEval.cs:93`。仅缺行号锚点 |
| 战斗逻辑驱动.tsv:534 | `RuleCore.cs 类尾 ExileFromCemetery / IsExiledFromCemetery`（bare） | ✅ 声明 `:16911` / `:16927`，读点 `:15800`。仅缺行号锚点 |
| 菜单_外壳.tsv:58 | `DeckRuntime.cs（banned 桩）`（bare） | ✅ 文件在、桩 `bool banned = false;` 在 `:3128`。建议补行号 `:3128`（非错，可精确化） |

---

## §三 可直接改账的清单（11 列的 `我们的落点` 一格）

> 只改第 7 列（`我们的落点`）。以下是「现值 → 建议值」。均为上文已回源确认。**B 档不需要 `--fix`**（`--fix` 只管 A 行），手工改或后续统一处理。

| # | 账文件:行 | 现 `我们的落点` | 建议改为 |
|---|---|---|---|
| 1 | 个人资料_成就_排行.tsv:198 | `Shell/MainMenuRuntime.cs:2807（BuildModeCard）` | `Shell/MainMenuRuntime.cs:2822（BuildModeCard）` |
| 2 | 其他_基础设施.tsv:460 | `CardPresentation/Shell/MainMenuRuntime.cs:4351（QuitApplication）· :1746（QuitGame → 转调 QuitApplication）· :4378（FullyQuitApplication 协程）` | `CardPresentation/Shell/MainMenuRuntime.cs:4375（QuitApplication）· :1761（QuitGame → 转调 QuitApplication）· :4402（FullyQuitApplication 协程）` |
| 3 | 战斗逻辑驱动.tsv:193 | `RuleEngine/Core/RuleCore.cs.CanAttackNow（:5693）· :5267-5510（IsValidTarget）` | `RuleEngine/Core/RuleCore.cs.CanAttackNow（:5706）· :5392（IsValidTarget）` |
| 4 | 战斗逻辑驱动.tsv:553 | `RuleEngine/Core/RuleCore.cs:12496 FireTriggerAt(Mob)` | `RuleEngine/Core/RuleCore.cs:12540 FireTriggerAt(Mob)` |
| 5 | 战斗逻辑驱动.tsv:227 | `RuleEngine/Core/RuleCore.cs:6695（`FireTriggerAt(…, KeywordTable.Regiment, …)`）· RuleEngine/Core/EffectResolver.cs:7372（`triggers:regiment` → `OtherCardRegiment`）` | `RuleEngine/Core/EffectResolver.cs:7607（case KeywordTable.Regiment → OtherCardRegiment）· RuleEngine/Core/EffectResolver.cs:10020（BroadcastUnitRegimentHandHops 手牌两跳）` |
| 6 | 战斗逻辑驱动.tsv:645 | `RuleEngine/Core/RuleCore.cs:6980（PreCallBoardListener ×2 + BroadcastWhen(Attack)） · RuleEngine/Core/RuleCore.cs:6984（Attack 广播**显式**传 `attackType: ranged ? 2 : 1`）` | `RuleEngine/Core/RuleCore.cs:6996/:6999（PreCallBoardListener ×2） · RuleEngine/Core/RuleCore.cs:7005（BroadcastWhen(Attack) 显式传 attackType）` |
| 7 | 战斗逻辑驱动.tsv:427 | `RuleEngine/Core/Aura.cs:707（誓约授予那一段；`RuleEngine/Core/Aura.cs:703` 起的注释引了这条判据）` | `RuleEngine/Core/RuleCore.cs:11685（OathExtraReplays = 原版 IsThereDoubleOathEffect 的等价物）` |
| 8 | 日常_奖励.tsv:229 | `Shell/ExpansionPassEventWindow.cs:2878（ExpansionPassMissionsTabLogic.OnClose）` | `Shell/ExpansionPassEventWindow.cs:2829（ExpansionPassMissionsTabLogic.OnClose）` |
| 9 | 日常_奖励.tsv:232 | `Shell/ExpansionPassEventWindow.cs:2881（ExpansionPassMissionsTabLogic.Reset）` | `Shell/ExpansionPassEventWindow.cs:2841（ExpansionPassMissionsTabLogic.Reset）` |
| 10 | 日常_奖励.tsv:470 | `CardPresentation/Shell/MissionsTab.cs:610（BuildDailyRow —— 六件仍内联）` | `CardPresentation/Shell/MissionsTab.cs:611（BuildDailyRow —— 六件仍内联）` |
| 11 | 日常_奖励.tsv:549 | `CardPresentation/Shell/MainMenuRuntime.cs:3780（`TryGetEventNavInfo`）+ :4440（`StepEventNavTimers`）` | `CardPresentation/Shell/MainMenuRuntime.cs:3804（TryGetEventNavInfo）+ :4464（StepEventNavTimers）` |
| 12 | 未分类_其他.tsv:708 | `Shell/MainMenuRuntime.cs:2177 CounterText` + `:2257`（0.5s ceil 滚动）` | `Shell/MainMenuRuntime.cs:2192（CounterText）+ Shell/TopBar.cs:601（StepCounters，0.5s 滚动本体）` |
| 13 | 轮抽.tsv:6 | `CardPresentation/Shell/DraftEventBase.cs:257（Expire）` | `CardPresentation/Shell/DraftEventBase.cs:277（Expire）` |

**可选精确化（非错，仅补行号）**：
- 战斗逻辑驱动.tsv:253 → `RuleEngine/Core/RuleCore.cs:16701（CheckUnitsCancellingAttack）+ :16734（HasCancelAttackAbility）`
- 战斗逻辑驱动.tsv:411 → `RuleEngine/Core/TargetCriteriaEval.cs:93（Matches）`
- 战斗逻辑驱动.tsv:534 → `RuleEngine/Core/RuleCore.cs:16911（ExileFromCemetery）/ :16927（IsExiledFromCemetery）；读点 :15800`
- 菜单_外壳.tsv:58 → 在 `CardPresentation/Deck/DeckRuntime.cs` 后补 `:3128（banned 桩）`

---

## §四 判不了的（差什么）

1. **未分类_其他.tsv:273** `Enchantments.TryGetCard<T>(T cardEffect, out RawCardScript)` → 落点只写 `RuleEngine/Core/Enchantment.cs（附魔层）`（**bare，无成员锚点**）。该文件里**没有** `TryGetCard`（现有：`Find/Has/On/Matching/FindSecretOrder/FindCancelAttack…`）⇒ **无法核对它该落在哪个成员**。差：要么补一个具体的成员名+行号，要么说明「等价靠逐调用点内联、无单点实现」（对照 396/398/399 三行的写法）。
2. **跑时抽样核实的边界**：本审计判定「那一行是不是那个方法」用的是**括号配平的声明归属** + 人工回源；对**多行签名 / 泛型 / 表达式体成员**以人工为准。已抽 13 行回源逐字确认（§二/§三），未逐字核的其余「成立」行按工具判定（命中率抽样 100%）。
3. **账在被并发改写**（见页首 mtime）⇒ 若同步点后行号/行内容变了，本表需复跑（脚本逻辑已固化，可直接复用）。

---

## §五 给调度台的提醒：这类错误为什么一直没被发现 + 怎么并进日常闸

### 根因（一句话）
**账闸 `check_ledger_points.py:715` 硬编码 `if c[i_status].strip() != "A": continue`** —— 它**只机检 A 行**；B/C/D 行一条都不看。而 B 行（=「有等价实现但仍有差异/未完全对齐」）**恰恰是落点最容易随重构漂移的一档**（等价实现常被搬位置 / 改名 / 被 doc 注释包裹）。

### 为什么「B 行落点错」比「A 行落点错」更隐蔽
- A 行有闸：错=红，`--fix` 会报漂移。
- B 行**永远绿**（闸根本不解它）⇒ 错落点**静默留存**，下个会话照着错行号去读，读到的是**别的函数/注释**，却「看着有依据」（行号对得上一个真实位置）。
- 本波实测：**13 / 258（5.0%）的 B 行落点确实错**；其中 **11 处是「点在紧邻的注释行」**（编者按旧行号写的，代码加了几行 doc 后整体下移）。这正是「B 行从不机检」放过的典型形态。

### 建议（按成本从低到高）
1. 🔴 **最低成本：把闸从「只查 A」改成「A 必查、B 只报告不判失败」。**
   - 改动点就在 `check_ledger_points.py:715`：把 `!= "A": continue` 改成 `if status not in ("A","B"): continue`，并对 B 行把 `fails` 降级为 `warns`（⛔ 不影响退出码、⛔ 不参与 `--fix` —— `--fix` 本来就只对 A）。
   - 收益立竿见影：本次 13 处会**当场报成「漂移/名字不在这一行（B 档·仅报告）」**。
2. **中成本：B 行也纳入「第 2/3 处落点」检查**（闸已有 `check_extra_refs`，目前只跑在 A 行）。本次 `460 / 549 / 227 / 645` 的**次要落点**全漂在注释行，就是这类。
3. **低成本、可立刻做：把「落点必须点声明行、不点 doc 注释行」写进闸的判法说明**（现在闸判「名字在不在这一行」，而注释行里出现方法名会被当成命中 —— 这正是大量「点偏到注释」能过 A 闸的原因之一，B 行将来接入也会中同一招）。
   - 建议加一条 `WARN_ON_COMMENT`：落点行首是 `//` / `///` / `*` 且该行是 doc 注释时，报「点在注释行（可能漂移）」——**只报告**（有的等价实现确实只在注释里写了判据，需人读，不能一刀切判错，与既有 `WARN_GETTER` 同口径）。
4. **建议并进同步点流程**：每次 `--fix`（串行、无写手时）之前，先用本报告 §三 那 13 条**手核一次**；B 行落点属「人对账」，不进自动 `--fix`（自动改 B 行风险高：名字匹配 ≠ 实现等价）。

### 一句话给下一会话
> **「A 行绿」≠「全账落点对」。B 行 13/258 错、且 11/13 是「点在注释行」——闸的 `:715` 只查 A，漏了整整一档。**

---

## 附：审计脚本位置（只读，可复跑）
`D:/4/_tmp_view/rt94/`：`outline2.py`（C# 声明归属解析）· `main.py`（B 行体检主程序，输出 `B94_detail.tsv`）· `secondary.py`（第 2/3 处落点检查）。脚本纯只读，未改任何工程文件。
