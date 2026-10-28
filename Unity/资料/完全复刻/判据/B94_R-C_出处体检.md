# B94_R-C 出处体检（第九十四会话 · 波 1 · 只读代理）

> 对象 = `Unity/资料/完全复刻/判据/B94_派活.tsv` 的 **381 条 B/C**（321 B + 60 C · 10 个写手组）。
> 对每行检查三件事：① 出处文件在不在 ② 那段行号是不是那个方法 ③ 判据要点与出处对不对得上。
> ⛔ 本代理**不改任何源码 / 账 / 正本**，只产出本文件。

## §〇 怎么查的（覆盖边界，先读这段）

| 项 | 做法 | 覆盖面 |
|---|---|---|
| ① 文件在不在 | 逐条把出处解析成真路径再 `os.path.exists`：`decomp_full/<X>.c` · `decomp_generic_trial/out/<X>.c` · `cpp2il/out_cpp2il_dev_ilspy/<目录>/<X>.cs` · `cpp2il/out_il2csharp/Assembly-CSharp/<X>.cs` · `il2cpp_out/dump.cs` · 我们自己的 `Assets/**/<X>.cs` | **381/381 全量** |
| ② 行号是不是那个方法 | `.c` → 读**首个非空非注释行**的函数名（跳过 `/* WARNING ... */` 与纯返回类型行），与文件名方法段 / 账里「方法」列比对；`.cs` → 建**成员声明表**（`^\t{0,2}` 且形如 decl），取「≤ 行号的最后一个声明」判定归属 | **381/381 全量**（每行最多 6 处出处，共 600+ 处引用） |
| ③ 要点对不对得上 | 机械抽 token 只能当**分诊**（`.c` 里没有 ILSpy 的形参名，`inclEnemy` 这类必然 0 命中 ⇒ 假阳性高），因此**逐条人读**：12 条异常 + 19 条 I_NAME 嫌疑 + 12 条 DUMP_NAME（抽 2）+ 6 条随机分层抽样（S2/S3/S4/F5）+ 6 条 F1/F2/F3 抽样 | **人工逐条对代码读过约 40 条**；其余 340 条只做了 ①②，③ 未逐条人读 —— **这是本报告的覆盖边界** |

**判不了的写「判不了」**：见 §四「判不了的 3 类」。

## §一 汇总（按写手组分列）

| 写手组 | 总数 | 文件缺 | 行号不是该方法 | 要点对不上 | 其它瑕疵 | 正常 |
|---|---:|---:|---:|---:|---:|---:|
| F1_RuleCore | 62 | 0 | 0 | 0 | 1 | 61 |
| F2_EffectResolver | 22 | 0 | 0 | 0 | 0 | 22 |
| F3_BattleDriver | 23 | 0 | 0 | 0 | 0 | 23 |
| F4_引擎小文件族 | 63 | 0 | 0 | 0 | 0 | 63 |
| F5_表现小文件族 | 28 | 0 | 0 | 0 | 0 | 28 |
| R-B_无落点 | 48 | 0 | 0 | 0 | 1 | 47 |
| S1_商店 | 26 | 1\* | **6** | 0 | 0 | 19 |
| S2_日常 | 52 | 0 | 0 | 0 | 1 | 51 |
| S3_未分类 | 22 | 0 | **2** | 0 | 0 | 20 |
| S4_其它小域 | 35 | 0 | 0 | 0 | 0 | 35 |
| **合计** | **381** | **1** | **8** | **0** | **3** | **369** |

\* S1 的「文件缺」= `商店.tsv:101` 出处写 `GachaCratePanel__<Awake>b__9_0.c`，磁盘上叫 **`GachaCratePanel___Awake_b__9_0.c`** —— 文件在、内容也对（`GachaCratePanel__<Awake>b__9_0(longlong)` = `AudioCue.Play2D`），**只是 Ghidra 泛型写法与磁盘名不一致**，不是真缺。

**「要点对不上」= 0 条** —— 精读过的 30+ 条要点与代码**逐条对得上**（例如 `IsCardIncluded` 的「9 段闸 + 4 种 cardType 0/10/0x14/0x28 + 残骸位」在 `.c:10-59` 里一字不差；`AddAttackAction` 的 `ctor :36 type=1` / `入队 :91` 现读成立；`MinionManager.Awake` 的 `:163-166 new Vector2[slotsPerSide]` / `:174` 取负 现读成立）。**判据本身的准确度很高，问题几乎全在「行号指哪」。**

**结构性观察（按「出处只用 ILSpy」占比排）** —— 这正是 §三 提醒的依据：

| 写手组 | 只用 ILSpy（无 `.c` 对照） | 占比 | 有 `decomp_full` `.c` 的 |
|---|---:|---:|---:|
| S3_未分类 | 20 / 22 | **91%** | 2 |
| S4_其它小域 | 28 / 35 | **80%** | 7 |
| S2_日常 | 41 / 52 | **79%** | 10 |
| R-B_无落点 | 37 / 48 | **77%** | 11 |
| S1_商店 | 20 / 26 | **77%** | 6 |
| F5_表现小文件族 | 17 / 28 | **61%** | 9 |
| F3_BattleDriver | 6 / 23 | 26% | 16 |
| F4_引擎小文件族 | 14 / 63 | 22% | 49 |
| F2_EffectResolver | 4 / 22 | 18% | 18 |
| F1_RuleCore | 7 / 62 | 11% | 55 |

**8 条错行号 100% 落在「只用 ILSpy」的行上**（全部是裸 `Class.cs:NN`）。`.c` 出处（`decomp_full/<类>__<方法>.c`）**零错行** —— 因为文件名自带方法名，写错就看得见。

## §二 逐条异常清单（12 条）

| # | 写手组 | 账文件:行号 | 方法签名 | 出处原文 | 体检发现 | 建议怎么修 |
|---|---|---|---|---|---|---|
| 1 | S3_未分类 | 商店.tsv:42 | `CarouselOfferPopupDrawer.DoReset()` | `CarouselOfferPopupDrawer.cs:96` | **:96 不在 DoReset 里** —— `DoReset` 声明在 **:309**；:96 落在 `<>c._003CDrawRewards_003Eb__4_1`（DrawRewards 的 lambda）体内 | 行号改 **:309**（或 `:309-320`） |
| 2 | S3_未分类 | 商店.tsv:45 | `CarouselOfferPopupDrawer.Initialize(ShopOfferBase)` | `CarouselOfferPopupDrawer.cs:60` | **:60 不在 Initialize 里** —— `Initialize` 声明在 **:109**；:60 同样落在 DrawRewards 的 lambda 里 | 行号改 **:109** |
| 3 | S1_商店 | 商店.tsv:89 | `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick()` | `ExpansionPassPurchasePremiumPanel.cs:110` | **:110 在 `Initialize`(:92) 体内** —— `PurchasePremiumClick` 声明在 **:216** | 行号改 **:216** |
| 4 | S1_商店 | 商店.tsv:91 | `ExpansionPassPurchasePremiumPanel._003CPurchasePremiumClick_003Eb__12_0()` | `…Panel.cs:140` | **:140 在 `Initialize`(:92) 体内** —— 该方法声明在 **:436** | 行号改 **:436** |
| 5 | S1_商店 | 商店.tsv:92 | `ExpansionPassPurchasePremiumPanel._003CPurchasePremiumClick_003Eb__12_1()` | `…Panel.cs:152` | **:152 在 `Initialize`(:92) 体内** —— 该方法声明在 **:441** | 行号改 **:441** |
| 6 | S1_商店 | 商店.tsv:98 | `GachaCratePanel.OpenCrateButtonClick()` | `GachaCratePanel.cs:120` | **:120 在 `IEnumerator.MoveNext()`（`<CrateAnimationFinished>d__15`）里** —— `OpenCrateButtonClick` 声明在 **:226**（:226 是个**同步 void**，不是协程，写手会被这条引到错的协程上） | 行号改 **:226** |
| 7 | S1_商店 | 商店.tsv:108 | `GachaEventTab.Initialize()` | `GachaEventTab.cs:60` | **:60 在 `SetEventTab`(:43) 体内** —— `Initialize` 声明在 **:144** | 行号改 **:144** |
| 8 | S1_商店 | 商店.tsv:111 | `GachaEventTab.Reset()` | `GachaEventTab.cs:140` | **:140 仍在 `SetEventTab`(:43) 体内** —— `Reset` 声明在 **:216** | 行号改 **:216** |
| 9 | S1_商店 | 商店.tsv:101 | `GachaCratePanel._003CAwake_003Eb__9_0(VideoPlayer)` | `GachaCratePanel__<Awake>b__9_0.c:1-20` | 磁盘名是 `GachaCratePanel___Awake_b__9_0.c`（`__<` vs `___`）；内容正确（`AudioCue.Play2D`） | 改成磁盘写法，或统一标注「Ghidra 显示名 ≠ 文件名」 |
| 10 | F1_RuleCore | 战斗逻辑驱动.tsv:1373 | `EnergySpentSignal.Raise()` | `decomp_full/BattleManager__PayCardCost.c:28-31` | **出处里没有 `Raise()` 本身**，只有**发射端** PayCardCost。要点也写了「两个发起源 = PayCardCost / PayActiveAbilityCostOath」⇒ 是**有意只引链上另一环**，但这条账的落点 `RuleCore.cs` 要复刻的是 `Raise()` | 补一条 `EnergySpentSignal__Raise.c`（或 `EnergySpent__OnEnergySpent` 的定义出处）；只引发射端不足以定 `Raise()` 的签名 |
| 11 | R-B_无落点 | 战斗界面_HUD.tsv:364 | `ChooseCardMenu.AutoProcessDone()` | `ChooseCardMenu.cs（<AutoProcessDone>d__21.MoveNext，WaitForSeconds(2f)）+ Setup(:600-640)` | 两处**都已核成立**：`Setup` 在 :559（:600-640 在其体内）、`AutoProcessDone` 在 :699。**唯一瑕疵 = 协程那一段没给行号** | 补 `:699-720`（或 `MoveNext` 实际区间） |
| 12 | S2_日常 | 日常_奖励.tsv:704 | `SignalEventTrigger.Unregister()` | `同上（四源全无体，2026-10-28 B93_N2 逐源点过）` | 出处写「同上」，**本条自身没有可核出处**；同组 :702 明写「decomp_full 0 文件 / generic_trial 0 / ilspy 0 体 / il2csharp」⇒ **判据确实是空的**（原版无产物） | 不是错，但落点要复刻时**无判据可依**；建议在账里标成「判据为空（原版无产物）」而不是「同上」，否则下游以为有出处 |

### §二·附 被排除的「疑似异常」（体检工具先报了、人工核实后**不是错**）

| 现象 | 条数 | 判定 |
|---|---:|---|
| `.c` 首行不是文件名的函数（`C_FIRSTLINE`） | 3 | 2 条是 `FUN_181xxxxx`（泛型类名含 `", "` ⇒ Ghidra 拒建符号，**已知良性**），体语义与要点一致（`OnMatchProcessSuccess` 的 `EndMatchData` 闸 + `LocalScore`；`GetContainer` 的 `LoadComponent` 按需加载）；1 条是 `AIBotsConfig.BotConfigFTUE__GetBotDecks.c` 首行 `<>f__AnonymousType0<object>__get_itemId`、体 = `return *(param+0x10)`，与 ilspy `GetBotDecks(){return botDecks;}` **同构 ⇒ ICF 折叠**，良性 |
| ILSpy 行号落在别的方法（`I_NAME`） | 19 | 只有 8 条是真错（上表 #1-#8）。其余 11 条是**跨类 / 调用端 / 状态机 / property** 的有意引用，逐条核过：`ManaTypeHolder:186` 正是 `SetSavedMana` 声明行；`CardScript:8387` = `CardToMinionAnim`；`BattleCardUI:1435` = `ChangeCardToMinion`（:1441 是 `ToggleBody3D`）；`AiScripted:78` = `<SkippableActionWait>d__15.MoveNext`；`CardScript:19980/:20008` = `ReactToUnitJammed`（调用端）；`BattleManager:24516` = `BroadcastUnitDamaged`（发射口）；`CardScript:14317` = `ResolveDamageDealt` 体内；`GameModes:392` = property `CurrentSeasonNumber`；`NetworkingPeer:7802`（il2csharp）正是 `private bool AlmostEquals(object[],object[])`；`BattleAction.cs:7-13` = struct 声明 + 字段表 |
| `dump.cs:NNNNN` 那一行不是该方法（`DUMP_NAME`） | 12 | **全部是「字段偏移表」引用，合规**（`bm+0xC0=playerManager`、`DamageInfo+0x10=actingCard`、`:118958` = `struct BattleAction` 的字段区起头、`:37699` = `class ClockManager` 后紧跟 `:37705 clockButton`）。CLAUDE.md §三 已把这种用法写成标准做法 |
| 出处 `.c` 是**另一个方法**（`C_OTHERMETHOD`） | 61 | 抽样 7 条（`ResolveJamUnit`/`BroadcastRemoveFromGame`/`OnDamageDelt`×2/`ReadyToUseActiveAbility`/`BroadcastDeadUnit`/`GetTipPosition`），**全部在同一条出处串里明写为「调用点 / 出队侧 / 发射口」**（如 `· 唯一调用点 = PlayAbility.c`），不是冒充被复刻方法 ⇒ 合规 |
| 出处只到文件、无行号 | 1 | 见上表 #11 之外的 `其他_基础设施.tsv:160` / `战斗逻辑驱动.tsv:993` / `:1122` / `日常_奖励.tsv:702` 四条的「无行号」部分其实是**散文**（`DefinedTrait.cs 数值表实核`、`SummonCriteria.cs`）或**结论本身**（四源全无体），不算瑕疵 |

## §三 给各写手的提醒

1. **裸 `Class.cs:NN`（ILSpy 行号）会漂 —— 这是本次唯一的系统性缺陷。**
   - **8 条错行全部是这种写法**，而 `.c` 出处（`decomp_full/<类>__<方法>.c`）**零错行**。
   - 根因：ILSpy 产物随每次重导会整体移位（本次实测：`DoReset` 在 309 而账里写 96；`ExpansionPass` 三个方法在 216/436/441 而账里写 110/140/152；`GachaEventTab` 在 144/216 而账里写 60/140）。
   - **建议写法**：`out_cpp2il_dev_ilspy/Xxx.cs:NN（方法 `M`）` —— **把方法名写进出处串**，这样行号漂了也还能自动重定位。
2. **S1_商店 / S3_未分类 必须返工这 10 条**（#1-#8、#9）。这两个组的出处最薄：S1 有 20/26、S3 有 20/22 是**只用 ILSpy**，一次 `.c` 对照都没有。这两个组同时是**唯一出错的两个组**，不是巧合。
3. **S2_日常（79% ILSpy-only）/ S4_其它小域（80%）/ R-B_无落点（77%）/ F5（61%）**：本次抽检**没查出错行**（随机抽了 6 条，其中 4 条逐条回代码核过 —— `MissionChallengeProvider.cs:7-15` = `GetAssets` 的 `ConfigManager.GetConfig<MissionsConfig>()` + null 抛、`GenericArmyItem.cs:221` = `GetItem` 的 `entries` 支、`DraftModeWarlordSelectionStateTimed.cs:41` = `OnOpen`、`MinionManager.cs:163-174` = 两条 `new Vector2[slotsPerSide]` + 取负 —— 全部对得上），但它们的出处形状与 S1 完全一样（裸行号），风险同等 —— 建议**下一轮同批加 `decomp_full` 旁证**，或至少把方法名写进出处串。
4. **F1 / F2 / F4 的出处质量最好**（`.c` 覆盖率 55/62、18/22、49/63），且**要点精确到段/行/字段偏移**（`ctor :36 type=1`、`入队 :91`、`8 段值域`、`+0x48/+0x4c 近战`…）。抽检 4 条**全部现读成立**。这三组**不需要返工出处**。
5. **「要点对不上」一条都没有** —— 说明写手在**语义**上是靠得住的，问题出在**坐标**。所以返工成本很低：**只改行号 / 补 `.c` 旁证，不要重写要点**。
6. **一条「出处只有链上另一环」的**（#10 `Raise()` 只引 `PayCardCost`）：这类账的风险是**签名/语义无法单独确立**。凡「我方要新建一个类」（如 `EnergySpentSignal`）的账，出处必须**含该类型自己的定义处**，不能只引发射端。
7. **`dump.cs` 引用照旧可用** —— 12 条全是字段偏移表，合规，不需要改。

## §四 判不了的（3 类，如实列出）

1. **`out_cpp2il_dev_ilspy` 丢常量 / 丢异步体 / 丢 setter 的行**：ILSpy 侧看不到的那些值，本次**一律没当成「账错」**（按 CLAUDE.md §三 的既定口径）。凡账里只有 ILSpy 出处、而要点写了具体数值的，本报告**没有逐条去 `.c` 复核数值**（那超出「出处体检」范围，属另一轮）。
2. **`out_il2csharp` 里的 `Assembly-CSharp-firstpass` 子集**：本次只对 `Assembly-CSharp/` 建了索引，若某条出处指向 `Assembly-CSharp-firstpass/`，会显示为「同名文件在另一目录」。**实测 381 条里没有这种**（0 条），所以不影响结论。
3. **`decomp_generic_trial/out`（641 个产物）只覆盖 39 个类型**：`PlayEvent.cs`（日常_奖励.tsv:561）在 **ilspy 与 generic_trial 双侧都没有体**（ilspy 目录下只有 `PlayEventSaveGame.cs`）。这条我判「**出处文件不存在**」，但按 CLAUDE.md 已记的「10 个 LiveOps 基类双侧 0 产物」，**这是产物缺口，不是写手写错**。修法 = 把那 10 个类名加进 `DecompG7.java` 重跑（一趟约 15 秒），**不要叫写手改出处**。

---

**体检范围一句话**：381 条**全部**做了「文件存在 + 方法归属」两道机检；**8 条错行号 / 1 条文件名写法 / 1 条只引链上另一环 / 1 条无行号 / 1 条「同上」**，其余 369 条正常；**要点与代码的语义一致性，人工精读 30+ 条，未发现一条对不上。**
