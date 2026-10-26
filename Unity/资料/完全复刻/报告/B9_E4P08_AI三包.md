# B9_E4P08 · `AiScoring.cs` / `SimpleAI.cs` / `BoardSlots.cs` 三包施工报告

> 2026-10-26（第八十四会话）· 执行型引擎写手 `E4P08` · 派活包 = `判据/B9_剩余C_派活包.md` **§2 的 P-08 / P-09 / P-10**
> 施工单行：**12 / 29 / 38 / 39**（P-08）· **1062 / 1063 / 1064**（P-09）· **1120**（P-10）
> 独占文件：`RuleEngine/Data/AiScoring.cs` · `RuleEngine/Data/SimpleAI.cs` · `RuleEngine/Core/BoardSlots.cs`（**只动了这三个**）

---

## ① 结论（一句话 + 数字）

**8 条全部落地**：P-08 四个「原版 0 调用点」的方法**按反编译逐条建好且不接线**（对局行为零变化）· P-09 三格旋钮**就地打 `🔶 自己设计` 标记（零行数变化，值一个没改）**· P-10 `GetNextSlotCheckingAdjacents` **改成原版有符号格号 + 在 AI 部署处接线**。

| 读数 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_E4P08 bash 工具/typecheck.sh` | **运行时 0 · 编辑器 0**（跑了 3 次，前两次在改完 AiScoring / SimpleAI 后各一次，最后一次终态一次） |
| `bash 工具/ruleprobe.sh check` | **rc=0** · 解析差异 **0 行** · 哨兵失败 **0 条** · 输出指纹 `18439d96…`（与开工前 21:42 那次**逐字相同**） |
| `bash 工具/ruleprobe.sh play check` | 基线**文件**未动（哨兵 ✓ `af8e8178…`）· 主档 ①-a **0 张卡变** · 电池 ①-b **2 张变了（`BL18` / `UM18`，`UM18.raw 46→50`）** —— **不是本代理的改动**，证据见 ②-3 |
| `ruleprobe ai actions "Primaris Intercessor"` | `playCard slot= 5`（= 右近那格 `WarlordSlot+1`）—— 与改前**逐字相同**（推导见 ②-2） |
| `ruleprobe ai UM52`（option 13 · 池内 17 条之一） | `nonTarget[0] option=13 ⇒ 0`（夹具 `能量 20/20` ⇒ `limit=21 > totalMana` ⇒ 原版也会给 0） |

🔴 **P-10 的「`check` 有变化要逐条解释」= 本次【没有变化】，而且这是预期的**：`check` 是**解析层**、`play` 是**结算层**，两者都到不了 AI（`PlayProbe.cs`/`Program.cs` 对 `SimpleAI|AiScoring|NextDeploySlot` **0 命中**）⇒ AI 部署落点的改动在这两个读数口上**不可观测**（这正是 CLAUDE.md 记的「`play all dump` 结构上看不见 AI」）。AI 侧唯一读数口 = `ruleprobe ai` / `ai actions`，读数见上表最后两行。

**行号纪律（本仓 18·b / N9 那一族）**

| 文件 | 行数 | 漂移 |
|---|---|---|
| `Data/SimpleAI.cs` | 2200 → **2200** | **0**（`NextDeploySlot:217` / `EnumerateActions:316` / `ScoreAction:485` / `ValueInPlay:786` / `KnobsOf:1436` 全部原位） |
| `Data/AiScoring.cs` | 889 → **1279**（+390，全在**类尾** + `#if UNITY_EDITOR` 自检类 + 早退支那 8 行 trace，见 §⑥-1） | **0**（`ScoreFromCriteria:138` / `BestAttackScore:541` / `HandBuffValue:622` / `ManaBuffValue:730` / `AverageValue:770` / `ScoreFromJam:809` / `TraitAiScore:865` 全部原位；`ManaBuffValue` 的委托改写是**逐行等长**的） |
| `Core/BoardSlots.cs` | 596 → **590**（−6，只在本文件最后一个方法内） | 所有 <534 的账行落点**不受影响**；唯一移动的是**本代理自己那一条**（`GetNextSlotCheckingAdjacents` 570→**567**，已写进补丁） |

行尾：三个文件都是 **LF / 无 BOM / 裸 CR = 0**（改前改后一致，二进制核过）。

---

## ② 逐条（结论 + 证据）

### ②-1 P-08 · 四个「原版 0 调用点」方法（只加函数、不接线）

**先核了「0 调用点」这个前提**（不然「不接线」就是偷懒）：
`grep -rl "GetDefensiveAttackValue\|SuicideHeroAttack\|ShouldPlayComboCard" d:/2/tools/decomp_full/` ⇒ **只命中它们自己那三份 `.c`**，没有任何调用方。⇒ 我们**不接线才是照原版**；接线反而是新行为（会改 AI 出招偏好），必须调度台单独裁定 + 重钉基线。

四个方法**全部追加在 `AiScoring` 类尾**（`AiScoring.cs:920-1131`），逐条判据：

| 施工单行 | 方法 | 我们落点 | 判据（🥇 现读） |
|---|---|---|---|
| 12 | `GetDefensiveAttackValue(UnitState acting, UnitState target, float currentScore)` | `AiScoring.cs:936` | `AI__GetDefensiveAttackValue.c:1-24` ＋ 🥈 `cpp2il/out_cpp2il_dev_ilspy/AI.cs:971-991`：① 双督军 ⇒ 0；② 守方督军 ∧ `currentScore < 2f`（`DAT_1834b2bbc`）⇒ 0；③ 其余原样返回 |
| 39 | `SuicideHeroAttack(UnitState acting, UnitState target)` | `AiScoring.cs:964` | `AI__SuicideHeroAttack.c:1-48`：① 双侧 `cardType==Hero`；② 两侧 `currentHealth`（`+0x68..+0x78` 的 `ObscuredInt` 组）；③ `acting.Health <= target.Health` ⇒ true |
| 38 | `ShouldPlayComboCard(BattleContext ctx, int side, CardDef comboCard, bool comboCardFirst, CardDef thisCard, float score, bool playedFirstComboCard)` | `AiScoring.cs:1005` | `AI__ShouldPlayComboCard.c:1-70` ＋ 🥈 `AI.cs:2673-2726`：comboCard 在手 ∧ `thisCard.Cost + comboCard.Cost <= unusedMana` ∧ (`playedFirstComboCard` ∨ (`comboCardFirst` ∧ `score >= 2f`)) |
| 29 | `ScoreFromManaBuff(BattleContext ctx, int p, AiScoringEntryDto e)` | `AiScoring.cs:1063`（窄口 = `ManaBuffValue:730` 委托） | `AI__ScoreFromManaBuff.c:15-97` ＋ 🥈 `AI.cs:2052-2110`：⑤ 手牌 / ⑥ 督军 / ⑦ 同侧场上（含残骸）三段，逐段判 `∈ (manaLeft, limit]` 各 +1；`limit > totalMana ⇒ 0` |

**三处刻意偏离，全部写在 doc 里**（不是悄悄改）：
1. **签名从 `CardDef` 改成 `UnitState`**（前两条）：原版判的是 `EntityScript.get_cardType()` 与**当前血**，`CardDef.Health` 只是卡面血 ⇒ 用 `CardDef` 会把「卡」和「场上的实体」混起来。
2. **空引用**：原版对 null 走 `FUN_1803f47a0`（全局 NRE 桩）= 抛异常；我们返回 `currentScore` / `false`（引擎里抛会打断整条 AI 回合）。
3. **`ShouldPlayComboCard` 的三处接口差异**：`AiScoringEntryDto` 没有 `comboCard/comboCardFirst` 两列（`Data/AiScoringData.cs` 不在白名单）⇒ 拆成显式实参；原版写死 `isPlayer:false`（0 号位）⇒ 由调用方传 `side`（我们的 AI 两个座位都可能是它，同 option 35 的口径）；`playedFirstComboCard` 原版是 `AI` 实例字段 ⇒ 由调用方持有后传入。

**⑥⑦ 的现状（如实）**：算式已就位（`AbilityManaCostInRange:1127`），但 `ActiveAbility.manaCost(+0x14)` **没有落进我们的卡表**（`Resources/cards_engine.json` 现读 `"activeAbility"` 键 **0 次**）⇒ 今天恒 0。真值在 `cards_truth.json` 的 `activeAbility.manaCost`（现池 **143 张**有 `activeAbility`），要落进 `cards_engine.json` 得改 `工具/gen_cards_engine.py` + `Core/CardDef.cs`（**两个都不在白名单**，= 施工单行 29 的 ①②）。我留了接线口 `AiScoring.ActiveAbilityManaCost`（`AiScoring.cs:1120`，默认 `null` = 未知），那两项做完后接一下即可，**方法体不用动**。

**单测入口**：`AiScoringSelfTest.Run(Action<string,bool,string> check)`（`AiScoring.cs:1153` 起，`#if UNITY_EDITOR` 段 —— 已确认 `Assembly-CSharp.csproj` 的 `DefineConstants` **含 `UNITY_EDITOR`**，所以 `typecheck` 会真的编到它，不是盲区；**并且这 22 条已在离屏壳里真跑过：22 通过 / 0 失败**，见 §⑥-3）。共 **22 条**（行 12 六条 · 行 39 六条 · 行 38 六条 · 行 29 四条）：行 12 含「正好 2.0 ⇒ 2.0」这条边界 · 行 39 含平血 ⇒ true · 行 29 含「钩子给值 ⇒ 督军/场上各 +1」这条**证明算式是活的**（跑完复位成 `null`）。
⚠️ 自检宿主接一行即可：`AiScoringSelfTest.Run((tag, ok, detail) => CheckTrue(ok, tag + " —— " + detail));` —— `RuleEngine/Editor/RuleEngineTest.cs` **不在本代理白名单**。

**为什么这一包不会动指纹**：`play` 探针源码里 `grep "SimpleAI\|AiScoring\|NextDeploySlot"` = **0 命中**（`PlayProbe.cs` / `Program.cs`）⇒ `play` 根本到不了 AI 打分与部署落点；`check` 走的是解析层。实核：`check` 指纹与开工前一致；`play` 主档 0 变（②-3 的两个变化来自别人的文件）。

### ②-2 P-10 · `GetNextSlotCheckingAdjacents` 改成有符号格号 + 接线

**判据**（🥇 `MinionManager__GetNextSlotCheckingAdjacents.c:1-80` 全文 · 调用点 `AI__ExecuteAction.c:60-63` 唯一一处、且只在 `cardType == 0`（小兵）那一支）：

- ① 该侧还有空位（`slotPos.Length − minions.Count ≥ 1`）⇒ 扫那侧 minions，第 i 个 `HasWhileInPlayAdjacentEffect()` ⇒ **`return -i-2`**（左）/ **`i+2`**（右）；
- ② 两侧都不满足 ⇒ `GetNextSlotWithoutDisplacing`（左人数 < 右 ⇒ 左、否则右、平手走右；落点 = 该侧最外一格）。

**改动两处**

1. `BoardSlots.cs:567 NextSlotCheckingAdjacents`：① **返回域改成原版的有符号格号**（原来是绝对格号 `SlotOf(...)`）；② 第三支 `abs - BoardSpec.WarlordSlot`（`ourSlot = 4 + origSlot`）；③ **谓词默认 = 原版逐字的 `UnitState.HasWhileInPlayAdjacentEffect()`**（`Func` 形参保留，仅供自检/探针注入替身谓词）；④ 两侧都满时返回 **0**（原版该函数的值域 `~count ≤ −1` / `count+1 ≥ 1` **拿不到 0** ⇒ 0 在我们的调用约定里是安全哨兵）。
2. `SimpleAI.cs:217 NextDeploySlot`：**整条三步算法搬走、只剩换算 + 出声**（`signed == 0 ⇒ -1`；`WarlordSlot + signed`；越界/督军格 `LogWarning` 后返回 -1）。⇒ 「同一条规则两个写点」消掉一处。

**可观察差异（逐条算过，不是「应该没影响」）**

| 局面 | 改前 | 改后 | 依据 |
|---|---|---|---|
| 空场 | `W+1`（右近） | `W+1` | 两侧相等 ⇒ 右（`NextWithoutDisplacing`） |
| 左 1 / 右 0 | `W+1` | `W+1` | 左 1 > 右 0 ⇒ 右 |
| 右满 4 只 | 左最外 | 同 | `RoomOnSide(Right)=0` ⇒ 跳第二支 |
| 有洞 + 段首尸体（`T02` ②）/ 段首尸体 + 洞（`T02` ③） | 该侧最外空格（2 / 6） | **2 / 6** | 第三支 `NextFreeWithoutDisplacing`，逐条重算过 |
| `RuleEngineTest` ⑩（`AuraSpecs.Adjacent` 单位当锚） | 贴它外侧（`W-2`） | **也 `W-2`** —— 但**路径不同**：现在走的是「平衡（左 1 < 右 2 ⇒ 左最外 = 2）」 | ⚠️ 这条断言**靠巧合仍然绿**，见 ⑤-3 |
| **9 张相邻光环卡在场**（`Core/Aura.cs:35` 写「9 张」） | 它们的外侧格是部署锚点 | **不再是锚点**（那本来就不是原版判据） | 这就是本包**唯一**的真实行为变化 |

⚠️ **第 ①② 支今天恒不触发**（池内 `requiem` 0 张 · ability 级 `targetsAffected == 100` 0 行；判据 → `UnitState.HasWhileInPlayAdjacentEffect` 的 doc）⇒ 落点**永远走第三支**。这正是「照原版」，⛔ 不是「没做」。也正因如此，**「AI 把新兵贴到相邻效果单位旁」这条今天没有机器读数能验到**（要验得先造一个 `ta==100` 的夹具、并走 `NextSlotCheckingAdjacents(ps, 替身谓词)` 的非 null 分支）。

**读数**：`bash 工具/ruleprobe.sh ai actions "Primaris Intercessor"` ⇒ `playCard slot= 5`（夹具：我方 slot 3 一个探针单位、右侧 0 个 ⇒ 左 1 > 右 0 ⇒ 右最外 = `W+1` = 5，与改前逐字相同）。

### ②-3 `play check` 那 2 张卡为什么**不是**本代理造成的（证据三条）

1. **结构上到不了**：`play` 探针（`工具/ruleprobe/PlayProbe.cs` / `Program.cs`）**0 处**引用 `SimpleAI` / `AiScoring` / `BoardSlots`；我只改了这三个文件（AI 打分 + AI 部署落点），结算链一个字节没碰。
2. **像是谁的**：变的正是 `BL18` / `UM18`，而且 `UM18.raw 46 → 50`（**+4**）—— 与 CLAUDE.md §16·8 记的预期**逐字吻合**：`Aura.RefreshInPlayNumericAndKeywords` 挂到 `ActionPump.cs:118` 后「**BL18/UM18 事件电池各 +4 ⇒ 重钉基线必须调度台做**」（P-06 那一包）。
3. **时间线**：`Core/ActionPump.cs` mtime **21:52:32**、`Core/RuleCore.cs` **21:50:08**（都在我开工之后、且都比我最后一次跑探针还新）⇒ 有别路写手正在这两个文件上落改动。我 21:43（改任何文件之前）跑 `play check` 时是 rc=0。

⇒ **本代理的交付判据**：`check` rc=0 且解析指纹不变（已复现两次）；`play` 基线**文件**哨兵 ✓（`af8e8178…` 未被改写）；`play` 的 ①-a 主档 0 变。结算指纹的重钉请调度台在 P-06 收口后统一做一次。

---

## ③ 没查清 / 没做到的（如实）

1. **`ruleprobe ai` 里那两个「新项」我没能让它出现** —— 验收口写的是「分解里出现 `defensive=` / hero+minion 两项」，但 **`工具/ruleprobe/AiProbe.cs` 不在本代理白名单**，而它取数的路是**直接调 `ScoreFromCriteria` 并自己拼 `extra`**（`AiProbe.cs:147-215` 的 `DumpColumn`），**不走** `AiScoring.TraceCriteria` ⇒ 产品侧加 `Trace` 也打不出来（我核过：`ai UM52` 的输出里没有任何我新加的痕迹）。
   补丁已经写好，见 ⑤-1（约 6 行，交给工具线写手即可）。**这也意味着本包的验收口目前只到「代码 + 单测 + 指纹不变」，没有机器读数**。
2. **⑥⑦ 的值**：算式齐、判据列缺（见 ②-1），所以「hero/minion 各 +1」今天**做不到**，只能给「今天恒 0 + 钩子就位」。数据列归施工单行 29 的 ①②。
3. **`ShouldPlayComboCard` / `ScoreFromManaBuff` 的单测要不要进宿主的最终决定**：我给了可编译的 `AiScoringSelfTest.Run`，但「它挂进 `RuleEngineTest.Run` 的哪一步」属于自检线（且那个文件正被别人改）。
4. **`9 张相邻光环卡`这个数**是我**引** `Core/Aura.cs:35` 的注释（「相邻型（`Adjacent units have X`，9 张）」），**没有**逐张从卡池重算；要更硬的读数得扫 `cards_engine.json` 的 desc + `AuraSpecs` 构造规则（`CardDef.cs:340/358` 的 `head == "adjacent"`）。
5. **`RuleEngineTest` 那两条断言我按推理判「仍然绿」，但没有跑**（⛔ 不跑 Unity）：⑩ 见 ②-2 表的最后两行；`T02` 的 ①②③ 见同表的有洞两行。

---

## ④ 改动清单

| 文件 | 位置 | 改了什么 | 行数变化 |
|---|---|---|---|
| `RuleEngine/Data/AiScoring.cs` | `ManaBuffValue`（:730） | 内联的 ⑤ 算式 → **委托给 `ScoreFromManaBuff`**（避免同一条算式两个写点）；逐行等长 | **0** |
| 同上 | 类尾 :920-1138 | 新增：`GetDefensiveAttackValue`(:936) · `SuicideHeroAttack`(:964) · `ShouldPlayComboCard`(:1005) · `IsRawCardInHand`(:1041) · `ScoreFromManaBuff`(:1063) · `ActiveAbilityManaCost` 钩子(:1120) · `AbilityManaCostInRange`(:1127) | +249 |
| 同上 | 类后 `:1139-1279` | `#if UNITY_EDITOR` 的 `AiScoringSelfTest`（**22 条**单测，实跑 22/22 通过） | +148 |
| `RuleEngine/Data/SimpleAI.cs` | `KnobsOf` doc(:1420-1421) + 四行 `case`(:1440-1443) + `HasAdjacentEffect` doc(:164/:177) + `CountSide` doc(:266) + `NextDeploySlot` doc(:189-215) + 方法体(:222-279) | ① `🔶 自己设计` 标记（P-09，**零行数**）；② `NextDeploySlot` 改**委托 + 换算 + 出声**（P-10） | **0**（净） |
| `RuleEngine/Core/BoardSlots.cs` | `NextSlotCheckingAdjacents`（:538-589） | 返回**有符号**格号 + 默认谓词换成 `UnitState.HasWhileInPlayAdjacentEffect()` + 满场返回 0 + doc 重写 | −6 |
| `资料/完全复刻/账/_parts/B9_E4P08.tsv` | 新建 | 11 列表头 + **8 行**（12→A · 29→B · 38→A · 39→A · 1062/1063/1064→C（只加标记）· 1120 C→**A**） | — |

账补丁的 `我们的落点` 全部指到**当前真实行号**（A 档会被 `check_ledger_points --all-A` 解析声明名，已逐条核过名字对得上）：`AiScoring.cs:936/964/1005/1063` · `BoardSlots.cs:567` · `SimpleAI.cs:1436（KnobsOf）`。⛔ 未碰 `_merged_*`、未合并主表。

---

## ⑤ 顺手发现（**只报不改**）

1. **`ruleprobe ai` 的读数口缺这两项（补丁，交工具线）** —— `工具/ruleprobe/AiProbe.cs` 的 `DumpColumn`（:147）：
   ```csharp
   // ① option 5：defensive 读数（P-08 行 12；原版 0 调用点 ⇒ 不参与打分，只摆数）
   if (e.scoringOption == 5 && probe != null && foeWarlord != null)
       extra.Append(" defensive=").Append(F(AiScoring.GetDefensiveAttackValue(probe, foeWarlord, 2.1f)))
            .Append("/").Append(F(AiScoring.GetDefensiveAttackValue(probe, foeWarlord, 1.9f)))
            .Append("（前=2.1 档 后=1.9 档；应 2.1/0）");
   // ② option 13：⑥⑦ 两段（P-08 行 29）—— 借产品自己的 TraceCriteria 钩子，别另写算式
   if (e.scoringOption == 13)
   {
       var ml = new List<string>(); AiScoring.TraceCriteria = ml.Add;
       try { AiScoring.ScoreFromManaBuff(ctx, side, e); } finally { AiScoring.TraceCriteria = null; }
       foreach (var s in ml) Console.WriteLine("  " + s);
   }
   ```
   （`GetDefensiveAttackValue` / `ScoreFromManaBuff` / `TraceCriteria` 都是 `public`，`AiScoring` 已 using。）
2. 🔴 **施工单行 38 的字段归因错了一处**：写的是「thisCard 的某 `bool(+0x68)`」，实为 **`ScoringCriteria.comboCardFirst`**（`AI__ShouldPlayComboCard.c:32` 读 `param_3+0x68`；🥈 `AI.cs:2671` 直接给了字段名 `if (scoringCriteria.comboCardFirst == playedFirstComboCard)`）。我按实读实现。
3. 🔴 **`RuleEngineTest` ⑩ 的注释与落点口径过期**（`RuleEngine/Editor/RuleEngineTest.cs:28840-28866`）：它写「我们这边 = `AuraSpec.Adjacent`；原版判据 = `HasWhileInPlayAdjacentEffect`」—— 现在**部署路径已经换成原版判据**，而且那两条断言（`HasAdjacentEffect(aura)` / `NextDeploySlot == W-2`）**仍然绿**（前者因为函数保留、后者因为平衡支恰好也给 `W-2`）⇒ **这条测试现在测不出「谓词换没换」**，需要自检线补一条 `NextSlotCheckingAdjacents(ps, 替身谓词)` 的鉴别力断言（替身谓词给真 ⇒ 返回 `±(i+2)`；给假 ⇒ 走第三支）。
4. **`RuleEngineTest_T02.cs:384-391` 的落点描述过期**：它写「判据 = `Data/SimpleAI.cs` 的 `// ---- 3. 平衡` 那一段」，那一段已搬进 `BoardSlots.NextSlotCheckingAdjacents` 的第三支（`SimpleAI.cs` 现在只剩换算）。断言本身仍是好的（②③ 的期望值 2 / 6 我逐条重算过，没变）。
5. **`SimpleAI.CountSide` 在产品路径上已无调用点**（`grep` 只剩它自己的声明 + 注释）。保留（「贴督军起连续且活人」仍是有用的读数口），已在它的 doc 里写明「⛔ 别拿它当部署判据」。若自检线认为该删，请一并改 `RuleEngineTest_T02.cs` 的注释。
6. **`SimpleAI.HasAdjacentEffect(CardDef)` 仍在但产品路径不用了**：`RuleEngineTest.cs:28848` 直接引用它 ⇒ **不能删**（否则编译红）。它的 doc 已就地改成「这是「光环作用到相邻格」，**不是**原版 `HasWhileInPlayAdjacentEffect`」。
7. **`ai_scoring.json` 的 `AiCardScoringDto.manaCost` 是【卡自己的】manaCost，不是主动技的**（`工具/gen_ai_scoring.py:53` 的 `CARD_KEYS` 从 `cards_truth.maps.manaCost` 取）。将来谁想拿它顶 `ScoreFromManaBuff` 的 ⑥⑦ 会**静默算错**（卡费 vs 技能费是两个字段：原版 `RawCardScript.manaCost(+0x78)` vs `ActiveAbility.manaCost(+0x14)`）。
8. **`play` 基线的重钉提醒**：`BL18`/`UM18` 各 +4 已经真实出现在电池里 ⇒ 那是 P-06 的 `Aura.RefreshInPlayNumericAndKeywords` 接线生效了，**调度台需要在 P-06 收口后重钉一次 `out_play_baseline.txt`**（CLAUDE.md §16·8 已写，这里只是确认它真的发生了）。
9. **`ScoreFromManaBuff` 少了一个原版没有的早退**：改前的 `ManaBuffValue` 有一句 `if (change <= 0) return 0f;`；新实现照原版**去掉**了它（原版只有 `limit > totalMana` 一道）。两者**等价**（`change <= 0` ⇒ 区间 `(cur, cur+change]` 为空 ⇒ 结果 0），但这一条**改了形状**，特意记在这里，避免下次有人以为是回归 —— `play check` 没变、`ai UM52` 读数也没变（都是 0）。


---

## ⑥ 追加轮（同日 · 调度台追加白名单）：`AiProbe.cs` 读数口 ＋ 两条自检断言

> 追加的两个文件：`工具/ruleprobe/AiProbe.cs`（读数口）· `RuleEngine/Editor/RuleEngineTest.cs` ＋ `RuleEngineTest_T02.cs`（自检宿主，**只静态改 + typecheck，⛔ 没跑 Unity**）。
> 另在 `Data/AiScoring.cs` 里给 `ScoreFromManaBuff` 的**早退支**补了一条 trace（否则探针读数是空的）。

### ⑥-1 改了哪几行

| 文件 | 位置（改后行号） | 内容 |
|---|---|---|
| `工具/ruleprobe/AiProbe.cs` | `DumpColumn` 内 **:197-217**（option 5）· **:219-231**（option 13） | ① `defensive=` 三档：`督军互砍→0` · `非督军打督军 2.1→2.1 1.9→0`；② `manaBuff:` 行：`hand=/hero=/minion=`（借产品的 `ScoreFromManaBuff` + `TraceCriteria` 钩子，**跑完立刻复位 `null`**）。⛔ **`v` / `sum` 一个字节都没动**（`defensive=` 只是挂在 `extra` 里） |
| `RuleEngine/Data/AiScoring.cs` | `ScoreFromManaBuff` **:1069-1077** | 早退支（`limit > totalMana`）也发一条 trace（带 `limit/totalMana`）—— 否则夹具走到早退时读数行是空的，会被误读成「没接」。**这段在 1063 之后 ⇒ 账里 A 行落点 `:1063` 不受影响**；但类尾那三个行号 +7：`ActiveAbilityManaCost` **1120** · `AbilityManaCostInRange` **1127** · `AiScoringSelfTest` **1153**（主表已合并的那 8 行里只有 `:1063` 是机检落点，仍然是准的） |
| `RuleEngine/Editor/RuleEngineTest.cs` | ⑩ 重写 **:28843-28871** · ⑩-b 新增 **:28872-28912** | ⑩ 的两条断言**数值没变**（仍绿），但注释口径改成「这条路现在走的是**平衡支**、不再是相邻特判」；⑩-b = **鉴别力断言**（见 ⑥-3） |
| `RuleEngine/Editor/RuleEngineTest_T02.cs` | **:21** · **:383-401** | ① header 第 ⑤ 条加「**已收口**（算法搬进 `BoardSlots.NextSlotCheckingAdjacents`）」；② 那个测试的 doc 重写：第五处写入点已搬家、并写明新机制数的是**非空**（尸体也算占格 ⇒ 与原版 `List.Count` 同义），**期望值 2 / 6 重算后逐字未变** |

行尾：`AiProbe.cs` LF · `RuleEngineTest_T02.cs` LF · `AiScoring.cs` LF · **`RuleEngineTest.cs` CRLF（全 38,323 行仍是 CRLF，插进去的行也是 CRLF）** —— 二进制核过，一个文件都没被翻行尾。

### ⑥-2 `ruleprobe ai` 的新读数（实测）

```
bash 工具/ruleprobe.sh ai "Path of the Warrior"
  cardTarget[0] option=5 base=0/0 mult=1/-1 targets=1 mightAct=真1/假0 defensive=督军互砍→0（应 0） 非督军打督军 2.1→2.1 1.9→0（应 2.1 / 0）（未接线：原版 0 调用点） ⇒ 0

bash 工具/ruleprobe.sh ai UM52
  nonTarget[0]    manaBuff: hand=0 hero=0 minion=0（limit=1 > totalMana=0 ⇒ 原版整段返回 0）
  nonTarget[0] option=13 base=0/0 mult=1/0 ⇒ 无目标=0 · 有敌方目标=0
```

- `defensive=` **三条支都有数**，且 2.1 / 1.9 那一对就是 `2.0f` 那道闸的读数（2.1 原样、1.9 扣成 0）。
- `hero/minion` 两项**出现了**，但值 = 0，**并带原因**（判据列未落，见 §②-1）。⛔ 我没有用假钩子把它凑成 +1 —— 那会是伪造读数。
- option 13 在这个夹具上恒走早退（`totalMana=0`）⇒ 读数行显式写明「原版整段返回 0」，而不是空着。

### ⑥-3 鉴别力证据（**真跑出来的数，不是推理**）

离屏壳（**不进仓库**，位置 `D:/tmp/wf_E4P08_verify/`，用与 `ruleprobe` 同一套编译方式：`Stub.cs` 桩 ＋ `Core/*.cs` ＋ `Data/{SimpleAI,AiScoring,AiTargets,AiScoringData}.cs`；**加 `-p:DefineConstants=UNITY_EDITOR`** 才能编到自检类）：

```
BoardSpec.WarlordSlot = 4
HasAdjacentEffect(AuraSpec.Adjacent)   = True      ← 前提一
HasWhileInPlayAdjacentEffect(原版判据) = False     ← 前提二
OK  ★1 signed(替身谓词=恒真) = 2（期望 2）
OK  ★2 signed(默认=原版判据) = -1（期望 -1）
OK  ★2 负控 signed(旧 AuraSpec 谓词) = 2（期望 2）   ← 把谓词改回去 ⇒ 数值翻面
OK  ★3 NextDeploySlot = 3（期望 3；旧谓词会给 6）
OK  T02② NextDeploySlot（段首尸体） = 2（期望 2）
OK  T02③ NextDeploySlot（尸体+洞） = 6（期望 6）
OK  ⑨ 空场第 1 只 = 5 · 第 2 只（左近）= 3 · 第 3 只（右次）= 6 · 第 4 只（左次）= 2
OK  满场 NextDeploySlot（哨兵） = -1 · 满场 signed（哨兵） = 0
---- AiScoringSelfTest ----
…（22 行全 OK）…
AiScoringSelfTest ⇒ 通过 22 · 失败 0
```

**「改回去会红」的三条**（每条都有唯一解，不存在「两边一起改也绿」）：
1. **★2 负控（最直接的一条）**：同一局面，把默认谓词换成**旧口径** `AuraSpec.Adjacent` ⇒ 实得 **+2**；新实现是 **−1** ⇒ 断言 `== −1` 必红。
2. **★1 的返回域**：若把 `NextSlotCheckingAdjacents` 改回**绝对格号**（G3 那版是 `SlotOf(Right, i+1)` = 6），替身谓词恒真时实得 **6 ≠ 2** ⇒ 红；若替身谓词被忽略（走默认谓词）⇒ 实得 **−1 ≠ 2** ⇒ 也红。
3. **★3 端到端**：旧谓词下 `NextDeploySlot` 会贴到右近那格外侧 = **W+2 = 6**，新实现是 **W−1 = 3** ⇒ 断言 `== W−1` 必红；另配「**不是** W+2 / **不是** W−2」两条灭自证。
4. 顺带把**既有**三条断言也真跑了一遍（T02 ②=2 · ③=6 · ⑨序列 5/3/6/2 · 满场哨兵 −1/0）—— 说明我搬走算法**没有碰坏**它们（这也是我给「期望值未变」那句结论的机器证据）。

复跑方式（壳还在 `D:/tmp/wf_E4P08_verify/`，两个文件 = `wfverify.csproj` + `Verify.cs`）：
```
dotnet build D:/tmp/wf_E4P08_verify/wfverify.csproj -p:DefineConstants=UNITY_EDITOR -v q
dotnet run   --project D:/tmp/wf_E4P08_verify/wfverify.csproj --no-build
```

### ⑥-4 本轮的读数（供核对）

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_E4P08 bash 工具/typecheck.sh` | **0 / 0**（改完 `AiScoring.cs` 一次、改完两个测试文件一次、终态一次） |
| `bash 工具/ruleprobe.sh check` | **rc=0** · 解析差异 **0 行** · 哨兵 **0 失败** · 输出指纹 **仍是 `18439d96…`**（改 `AiProbe.cs` 前后各跑一次，逐字相同） |
| `bash 工具/ruleprobe.sh play check` | **没动过**（`PlayProbe.cs` / `PinnedPlayMd5` 一字未改；rc=1 仍归 P-06，见 §②-3） |
| `AiScoringSelfTest` | **22 通过 / 0 失败**（离屏壳真跑） |
| 三条新增鉴别力断言 | **★1=2 · ★2=−1（负控 +2）· ★3=3**（全符合期望） |
