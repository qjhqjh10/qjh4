# 断言批 · 规格表（B 组）—— 第十三会话 9 份写手/诊断报告里「该补什么断言」的汇总

> 第十四会话 · 只读普查代理交件。**本表是给调度台照着写断言的规格**，不需要再回去读原始报告。
> ⛔ 本代理只写了这一个文件；没跑 Unity、没动 git、没改任何 `.cs` / 正本。
> 🔴 **「报告未给」= 原始报告里确实没有这一格**，不是我没查到 —— ⛔ 没有一处是我猜的。

## 〇、读了哪 9 份 / 各多少行 / 有没有「该补什么断言」这一节

| 报告 | 行数 | 有「该补什么断言」节？ |
|---|---|---|
| `W5d_A1339探针口径.md` | 186 | ❌ **无该节**。只有「⑥没查清 / ⑦顺手发现」；仅 ⑦-2 顺带提一句「可在 `check` 里加一条索引自证」 |
| `W5e_A1347徽记回归.md` | 273 | ❌ **无该节**。有「四、怎么验收（给调度台，收口那一趟用）」—— 等价物，给了 3 条可执行的验收 |
| `W6a_A1344与A1357数据侧.md` | 248 | ❌ **无该节**。「四、引擎侧那一半需要做什么」给的是**落点规格**（4 行表）＋「改完必跑 `RuleEngineTest.Run`」 |
| `W6b_引擎四笔.md` | 332 | ✅ **有**，四笔各一节（A1342 `:36-44` · A1346 `:76-91` · A1358 `:102-110` · A1357 `:137-148`） |
| `W6c_A1350A1351A1359.md` | 196 | ✅ **有**，三笔各一节（A1350① `:33-41` · A1351 `:62-69` · A1359 `:111-127`） |
| `W7_A1363与A1364.md` | 226 | ❌ **无该节**。只有 §四 静态复现链 ＋ §五「收口那趟该带 `RuleEngineTest.Run` / 并排渲一张」 |
| `W8_A1365与A1361清理.md` | 286 | ❌ **无该节**。本批**零行为改动**（五处纯注释 ＋ 一条断言消息文本 ＋ 两份文档数字），确实没有可断的东西 |
| `W9_A1368探针补列.md` | 231 | ❌ **无该节**。⑥-1 给了一条「补 `CostKind` 列」的建议（工具侧，非 Unity 宿主） |
| `D_A1334诊断.md` | 133 | ❌ **无该节**。⑤-3「**可选加固**（钉住现状、防回归）」给了一条断言规格（宿主 = `RuleEngineTest.cs`） |
| **合计** | **2111** | 只有 **2 份**（`W6b` / `W6c`）用了这个标题；另 **6 份**给了**等价物**（标题不同）；**1 份**（`W8`）确实没有 |

> ⚠️ **`W5d` / `W6a` / `W7` / `W9` 各有「没查清 / 顺手发现」提到的东西 —— 那些不属断言批**，收在下面 §三。
> ⚠️ **`W8` 是唯一「本该没有断言」的一份**：它改的全是注释与文档数字，逐处给了静态论证。

---

## 一、断言规格表（一行一条）

> 宿主栏的路径均相对 `D:/4/Unity/MyGame/Assets/`（`工具/ruleprobe` 除外，那是仓库根的 `D:/4/Unity/工具/ruleprobe`）。

| # | 断言该断什么（一句话） | 宿主文件 | 被测对象/落点（类.方法 / 文件:行号） | 灭自证那一半（没有就写「报告未给」） | 出处（报告文件:行号） | 前置（要不要夹具/临改某个常量） |
|---|---|---|---|---|---|---|
| 1 | `KeywordTable.Parse(new[]{"Rally: Deal 2 damage to an enemy"})` 的 `"rally"` 值 == **1**（正文里的 `2` 不算参数） | `RuleEngine/Editor/RuleEngineTest.cs` | `CardDef.KeywordTable.Parse`（`RuleEngine/Core/CardDef.cs:3021-3031`，取值走新 `HeadOf` `:2995-3001`） | 必须与 #2 成对 —— 只断这条时把 `FirstNumber` 改成**恒返回 1** 也全绿 | `W6b_引擎四笔.md:36-42` | 无（直接喂字符串数组） |
| 2 | 反方向：`Parse(new[]{"Blast 3: Deal 9 damage"})` 的 `"blast"` == **3**（不是 1 也不是 9） | 同上 | 同上 | 与 #1 **方向相反**，改任一边必红 | `W6b_引擎四笔.md:39-42` | 无 |
| 3 | 兜底：`Parse(new[]{"Rally: Deal 2 damage"})` 的 `"rally"` 仍是 **1**（不是 0） | 同上 | `FirstNumber` 的 `return 1` 兜底（`CardDef.cs:3053`） | 报告未给 | `W6b_引擎四笔.md:39` | 无 |
| 4 | `HasNumber("Blast 3: Deal 9 damage") == true` 且 `HasNumber("Blast: Deal 9 damage") == false` | 同上 | `CardDef.KeywordTable.HasNumber`（`CardDef.cs:3071`） | 报告未给独立灭自证；其作用 = 把 `HasNumber` 与 `Parse` **钉在同一个切分口**（防两份切分） | `W6b_引擎四笔.md:43-44` | 无 |
| 5 | `Parse(UM81 Phobos Librarian.desc)` 恰好 **2 条 op**；两条 `Cost == 3` 且 `CostKind` 归一后都是 `"oath"`；**第一条 `CostShared == false`、第二条 `true`**；`CardDef.OathCost == 3` | 同上 | `EffectText.Parse` 的 `carry`（`RuleEngine/Core/EffectText.cs:1101-1155`）＋ `SegResult.PaidCost/PaidKind`（`:1382-1405` · `:1898-1911`） | carry 改成**无条件继承上一段**也能绿（全池只 `UM81` 吃第 ③ 条）⇒ 必须配 #6 / #7 两条反向沙包 | `W6b_引擎四笔.md:76-88` | **夹具 = 真卡 `UM81`**（`EffectText.Parse` 出来的 op 列表 ＋ `CardDef.OathCost`） |
| 6 | 反向沙包：`Parse("Oath 1: Gain Stealth. Slay: Create a random card in hand")` ⇒ **第二条 `Cost == 0`**（`UM_Phobos_Lieutenant` 原文） | 同上 | 同 #5 | 「继承」与「清零」两个方向同时被钉住 | `W6b_引擎四笔.md:80-82` | 无（字符串原文） |
| 7 | 反向沙包：`Parse("3 [Icon]: Gain +1 Health. Pray: Deploy a Sister Novitiate")` ⇒ **第二条 `Cost == 0`**（`SOR7` 原文） | 同上 | 同 #5 | 同 #6 | `W6b_引擎四笔.md:81-82` | 无（字符串原文） |
| 8 | 真打一局、场上放 `UM81`，调 `RuleCore.UseOathAbility` ⇒ `Energy` **恰好 −3**（不是 −6） | 同上 | `EffectResolver.ResolveOneCore` 的 `CostShared` 分支（`RuleEngine/Core/EffectResolver.cs:315-341`） | 只断「只扣 3」不够 —— 把 `_oathOps` 改回**只收一条**（旧行为）也扣 3 ⇒ 见 #9 | `W6b_引擎四笔.md:83-85` | **夹具 = 真打一局** ＋ 场上放 `UM81`，记 `Energy` before/after |
| 9 | 同一次激活：该敌人**真的挨了 2-4 点**、它死了**真的拿到 `Shield`**（两条效果都发生） | 同上 | 同 #8 | 与 #8 **联合**：必须同时断「两条效果都发生了」；再补 `UM93 Fall Back` **恰好 −2**（不是 −4） | `W6b_引擎四笔.md:84-91` | 同 #8 |
| 10 | `UM93 Fall Back` / `SOR6 Righteous Repugnance` 各解析出 **2 条 op**、同价同货币、**第二条 `CostShared == true`** | 同上 | `EffectText.StampPaidCost`（`EffectText.cs:1409-1430`） | 见 #11 | `W6b_引擎四笔.md:102-107` | **夹具 = 这两张卡的原文**（`UM93` = `Oath 2: Lower its cost by 2 and give it Flank` · `SOR6` = `4 Energy: Give +2 … instead and Heal them 1`） |
| 11 | 结算：`Energy` **只掉一次**（−2 / −4），而且**两条效果都发生**（`lowercost` 真降了 2 费 ＋ `Flank` 真挂上 / 两条增益 ＋ `Heal 1` 都发生） | 同上 | `EffectResolver` 的 `CostShared` 支（`EffectResolver.cs:337-341`） | **反向**：把 `CostShared` 抹掉（回旧写法）时 `Energy` **必须掉两次** —— 用「只收一份」与「两条效果都在」的**联合**把两边钉死（单断「只扣一次」能被「干脆不扣」= `Cost` 清零蒙过） | `W6b_引擎四笔.md:104-110` | 同 #10 ＋ 结算需真打一局 |
| 12 | `EffectCondition.Normalize("you have less than 6 ☀") == "faithcheck"` **且** `Normalize("you have less than 6 Energy") == "energycheck"`（**两个方向都要**） | 同上 | `EffectCondition.Normalize`（`EffectText.cs:7812`）／`EffectText.MentionsFaith`（`:6265-6276`） | 见 #13 | `W6b_引擎四笔.md:137-139` | 无（字符串） |
| 13 | 结算一对（照 `TestConditionKindFamily` 的 `withBeast/withoutBeast` 形状）：**信仰 5 / 能量 99 ⇒ 那半句执行**（`gainfaith` 真发生、信仰 +1×敌人单位数）；**信仰 6 / 能量 0 ⇒ 不执行** | 同上 | `EffectResolver.ConditionHolds` 的 `case "faithcheck"`（`EffectResolver.cs:7030-7047`） | 🔴 **能量与信仰必须【反向】**（5/99 与 6/0）；只断一侧、或两档里两个字段**同向**变化时，「读错字段」（改成读 `Energy`）照样全绿 —— 因为 `SOR72` 原来就是读 `Energy` | `W6b_引擎四笔.md:140-145` | **夹具 = 两档玩家状态**（信仰/能量各摆一个值） |
| 14 | `RuleCore.CanJudgeCondition("faithcheck") == true` | 同上 | `UnjudgeableConditions`（`EffectResolver.cs:6654`） | 报告未给 | `W6b_引擎四笔.md:146-148` | 无；⚠️ `RuleEngineTest.cs:8016-8033` 那条「全池出现的条件种类都必须判得了」会自动兜住（现读全池 `faithcheck` 只 1 处） |
| 15 | 单位卡 `When <事件>, gain X` 的**执行层** op **必须** `Target.Subjectless == true` | 同上（补在现成那格 ⑤ 旁） | `CardDef.FireWhen(...)` 的返回（或 `WhenTriggers[i].Ops[0].Target`）；现成那格 = `RuleEngineTest.cs:2379-2425` | 报告未给 | `D_A1334诊断.md:89-92` | ⛔ **别拿 `EffectText.Parse(整条 desc)` 的结果去断**（那正是今天这条分叉）；夹具沿用现成那格 ⑤ 的卡 |
| 16 | 4 张修女会卡（`SOR11` / `SOR23` / `SOR24` / `SOR72`）的付费前缀解析后 `CostKindOf` 给 **`'faith'`**（改前是 `''`） | 同上 | `EffectText.CostKindOf`（`EffectText.cs:6280` 一带）／`RePaid` `:6204` · `RePaidBare` `:6225` | 报告未给 | `W6a_A1344与A1357数据侧.md:104-120` ＋ `:199-203`（§五-1「建议收口那趟带 `RuleEngineTest.Run`」） | **夹具 = 这 4 张**；⚠️ 静态链已验、**运行期无证据** |
| 17 | 5 张（`SOR28`/`SOR43`/`SOR44`/`SOR49`/`SOR59`）的 `2 ☀:`/`4 ☀:`/`7 ☀:` 段解析出 **`kind='faith'`**；全池逐分句 `kind==''` 从 **6 → 0** | 同上 | 同 #16（`EffectText.cs:6204/6225/6280`） | 报告未给 | `W7_A1363与A1364.md:143-169` ＋ `:195`（§五-1「收口那趟该带 `RuleEngineTest.Run`」） | **夹具 = 这 5 张** |
| 18 | 既有那几条（`MainMenuScene.cs:11222` 的 A781 图 · `:11557/:11581` 导航图标）照旧绿（字面量 ＋ **0.5px 容差**）—— 它们现在跑的就是转调后的口 | `CardPresentation/Editor/MainMenuScene.cs` | `MainMenuScene.QuadPxRect`（`:12231`，已转调 `MenuDraw.QuadRectPx`） | 见 #19 | `W6c_A1350A1351A1359.md:33-37` | 沿用宿主已有的探针节点（`:11230` 那处已在临时节点上 `AddComponent<ViewportClip>()`） |
| 19 | 在**父链 `localScale = 2`** 的探针节点上建一颗 quad，断 `QuadPxRect` 报的仍是**设计矩形**（容差 **0.01px**） | 同上 | 同 #18 | 🔴 **这条本身就是那一半** —— 旧实现（甲式）在这条上**必红**（它的中心项用的是**已缩放**的 `position`、会整体偏 2 倍）；且检测器读的是 `q.transform.position`（世界量）与**设计矩形字面量**，两者不是同一处代码 ⇒ 两边一起改回去也救不了 | `W6c_A1350A1351A1359.md:36-41` | **探针节点 ＋ 父链 `localScale = 2`** |
| 20 | 把填条值摆到 `0.5`，断 **(a)** `MenuDraw.UnionQuadRectPx(fillRoot, …)` 报的宽 ≈ **整根轨道宽**（= 规范句② 的行为） | `CardPresentation/Editor/BattleScene.cs`（战斗档，`WfSlider.Create` 探针先例 = `:13423` 的 `A218Probe`）／`CardPresentation/Editor/SettingsScene.cs`（设置窗） | `MenuDraw.QuadRectPx` / `UnionQuadRectPx`（`CardPresentation/Shell/MenuDraw.cs:644-662`）对 `slider_fill` 子树 | 🔴 **(a) 与 (b) 必须【不相等】且各自对上期望** —— 两份量的是**两份不同的实现**（共用件的并集 vs 本件读口），期望值都是**字面量**；谁哪天把 `MenuDraw` 改成「自动乘回子件缩放」，(a) 就会 ≈ (b)、这条红（而那会打翻既有 40+ 个调用点） | `W6c_A1350A1351A1359.md:62-68` | **夹具 = `WfSlider.Create(...)` 探针** |
| 21 | 同一时刻断 **(b)** `s.FillWorldW * 108f` ≈ **值 × 轨道宽** | 同上 | `WfSlider.FillWorldW`（`CardPresentation/Battle/WfSlider.cs:704`）／私有 `FillSpanW`（`:676`） | **单写 (b) 是自证** —— 把 `LayoutFill` 与 `FillWorldW` 一起改成一个错的口径，(b) 照样绿 ⇒ 必须与 (a) 并排 | `W6c_A1350A1351A1359.md:64-69` | 同 #20 |
| 22 | `FillWorldLeftX × 108` = **−280.54**（值 = 1 / 0.5 / 0.2 **三格都是**） | `CardPresentation/Editor/BattleScene.cs`（战斗档）／`SettingsScene.cs`（设置窗，轨道宽用该页自己的那个数） | `WfSlider.FillWorldLeftX`（`WfSlider.cs:714`） | 见 #24 | `W6c_A1350A1351A1359.md:111-120` | **夹具 = `WfSlider.Create(...)` 探针**；战斗档 `trackW = 561.08 / trackH = 12 / capScale = 1 / handlePx = 34.406`，`SetValue(1f/0.5f/0.2f, fire:false)`；🔴 **期望一律写字面量、⛔ 不引用 `WfSlider.*` 常量**；容差 **0.05px** |
| 23 | `FillWorldW × 108` = **值 × 561.08**（561.08 / 280.54 / 112.216） | 同上 | `WfSlider.FillWorldW`（`WfSlider.cs:704`） | ⛔ **别只断这条**（两种实现同值 ⇒ 恒真）；⛔ 也别拿它当 `FillCapWorldW` 的期望来源 | `W6c_A1350A1351A1359.md:114-126` | 同 #22 |
| 24 | `FillCapWorldW × 108` = **15**（`30 ÷ ppuMul 2`），且值 = 1 / 0.5 / 0.2 **三格相等** | 同上 | `WfSlider.FillCapWorldW`（`WfSlider.cs:727`） | 🔴 ① 断 `FillCapWorldW` 在**两个不同的值上【相等】** —— 「整体横向缩放」那条旧实现**结构上做不到**（值 0.5 时它必掉到 7.5）；② 同一次运行里**同时**断「中段宽 = `值 × 561.08 − 30`」与「端帽宽 = 15」—— 旧实现下这两条**不能同真** | `W6c_A1350A1351A1359.md:116-126` | 同 #22；⚠️ **端帽恒宽那条只在 `值 ≥ 30/561.08 ≈ 0.0535` 的格里成立**（再小按 `GetAdjustedBorders` 一起缩）；设置窗那三根：端帽 = `15 × 0.9 = 13.5` |
| 25 | `IconSetup.Verify` ② 那栏从 `2127/2127` → **`2131/2131`**；②b 报 **卡 655 / 记号 2131** | `CardPresentation/Editor/IconSetup.cs` | `IconSetup.Verify` 的 ② / ②b 两栏 | 报告未给（判据是 `pc > 100 && pi > 200`、**不是等值**） | `W5e_A1347徽记回归.md:212-214` | 无夹具；⚠️ 该条有一条**既有红** `★ ②c【A980-c】`，⛔ 别当本笔引入；⚠️ **这个数是 W5e 当批快照** —— `W7`/`W8` 之后现读是 **655 / 2140**（见 §三） |
| 26 | `CardFaceProbe.Run` 渲单卡与 PnP 成品图**并排比**（铁律 10 第 6 条）—— `W5e` 那 4 张 ＋ `W7` 那 5 张 | `CardPresentation/Editor/CardFaceProbe.cs`（渲染体是**写死的 `Names` 数组**，`:31-…`） | `CardFaceProbe.Run`；已有**同形样本** `Lychguard`（裸关键词那支）· `Armorium Cherub`（符号那支） | 报告未给 | `W5e_A1347徽记回归.md:215-219` ＋ `W7_A1363与A1364.md:198`（§五-3） | 🔴 **要临改 `Names` 数组**（加 `"Devastator Marine"` 等）—— 该 `.cs` 不在写手白名单 ⇒ **没加**；产物落 `d:/4/_tmp_view/cardface/` |
| 27 | 在 `check` 里加一条「**索引自证**」（防「谁红了顺手重生成基线」把探针口径静默退回修前） | `工具/ruleprobe`（⚠️ **不能加在 `RuleEngine/**`，那是黑名单**） | `check` 的实现（`工具/ruleprobe.sh` / `CheckMain`） | 报告未给 | `W5d_A1339探针口径.md:174-177`（§⑦ 顺手发现 2） | 需**新写一段检查逻辑**（非 Unity 宿主）；现状只有**文档级**防呆（`README.md:52-57` ＋ `:1024` 那一行哨兵） |
| 28 | 在 `Scan.cs:74` 旁边补一列 `CostKind`（`ck=`）＋ **再重生成一次基线** —— 否则 `check` 看不见「只改货币名（`energy`↔`faith`）」的改动 | `工具/ruleprobe/Scan.cs` | `Scan` 的 ops 序列化（`Scan.cs:70-78`） | 报告未给；⚠️ `W9` §④ 给了**可复用的构造性手法**：「造一个变异 → 旧 dump 0 行差异 / 新 dump N 行差异」 | `W9_A1368探针补列.md:194-198`（§⑥ 顺手发现 1） | 改完要**重生成** `工具/ruleprobe/基线/out_baseline.txt` |
| 29 | 不跑 Unity 的三步复核：① `git diff` 看 `card_icon_plan.json` 是否**纯插入 4 块**；② 数 `cards`/`items` = **655 / 2131**；③ 看那 4 条 `token` 是否 = 卡池 `desc` 的段首那一段 | 无宿主（命令 / 离线） | `Unity/数据/游戏数据/card_icon_plan.json`（＋运行时那份 `CardPresentation/Resources/card_icon_plan.json`） | 报告未给 | `W5e_A1347徽记回归.md:220-222`（§四-3） | 无；⚠️ 数字是**当批快照**，收口时按 `W8` 现读 = **655 / 2140** |

### 按宿主分组计数

| 宿主 | 条数 | 行号 |
|---|---|---|
| `RuleEngine/Editor/RuleEngineTest.cs` | **17** | #1–#17 |
| `CardPresentation/Editor/BattleScene.cs`（含 `SettingsScene.cs` 同族） | **5** | #20–#24 |
| `CardPresentation/Editor/MainMenuScene.cs` | **2** | #18–#19 |
| `CardPresentation/Editor/IconSetup.cs` | **1** | #25 |
| `CardPresentation/Editor/CardFaceProbe.cs` | **1** | #26 |
| `工具/ruleprobe`（非 Unity 宿主） | **2** | #27–#28 |
| 无宿主 / 离线命令 | **1** | #29 |
| **合计** | **29** | |

---

## 二、落这批断言之前必须先知道的三件事（都来自报告原文，不是我推的）

1. 🔴 **`IconSetup.Verify` 有一条【既有红】** `★ ②c【A980-c】`（`DA44` 只画一枚图标）—— `W5e:214` 明写「别当本笔引入」。⚠️ 而且 `W8:143-153` 已把那条的**注释与消息文本都改了**（行号 `:268-275` / `:289-300`），原因也随之翻了 —— 红没红**只有实跑 `IconSetup.Verify` 才知道**。
2. 🔴 **在引擎侧 `faithcheck` 落地【之前】，`RuleEngineTest` 若断言「`unresolved` 为空」会红** —— `W6a:199-203` 明写「那不是本批的 bug，是另一半还没做」。⚠️ 该半已由 `W6b` 落地（`EffectText.cs:7812` ＋ `EffectResolver.cs:7030-7047`），所以**今天跑 #12–#14 是奔着绿去的**；但若红的正是 `unresolved` 那一条，先看是不是这一族。
3. ⛔ **`A1350` 的 x 斜率那一半（差 ≤ 2.5e-4 px）在宿主里【照不出来】**（`cam.aspect` 被钉成 `DesignAspect`）—— `W6c:175-176` 明写「**别写一条「换 aspect 才红」的断言当它的保证**；要照它只能另开宿主，本轮没做」。⇒ **#18–#19 只覆盖中心项与尺寸项，x 斜率这一半【没有断言可写】**（这是报告如实承认的缺口，不是遗漏）。

---

### 三、报告里提到、但不属断言批的东西

> 这一节要回流到待办正本；每条只是一句话 ＋ 出处，细节在各自出处里。

### `W5d_A1339探针口径.md`

- 简报 ④ 的「预期 **11 行**」**挂错了账** —— 那是 `D_A1334` ⑤-1 主修（改 `EffectText.cs:1849-1861`）的影响面，不是探针笔的；探针这一侧的上限本来就 ≤ 2 行（`W5d:169-172`）
- `check` 会「重生成基线」把**真回归抹平**；`README.md:52-57` 只补了**文档级**防呆，机器级没有（`W5d:173-177`）
- `BattleProbe.cs:149/155/290` 是**第四种**不过 `LoadPool` 的解析入口；今天句子里没有卡名所以无害，有人往里塞带卡名的句子就会再犯一次（`W5d:178-180`）
- `RuleEngine/Core/RuleCore.cs(2750,13)` 的 `CS0162 无法访问的代码` 编译时一直在报，可能与当时另外四个改 `RuleEngine/**` 的写手有关（`W5d:181-183`）
- 临时副本仍在盘上：`D:/tmp/w5d/`（仓库外，不影响工程；复验「1 行」直接用它）（`W5d:184-186`）

### `W5e_A1347徽记回归.md`

- 🔴 `资料/卡面图标_对照与缺口.md` 被本笔**推过期**（写着 654/2127）；生成器是 `工具/gen_icon_doc.py`（白名单外没跑）（`W5e:228-234`）
- ⚠️ **`UM74` 的中文 `descZh` 段首缺 `誓言` 后面那个空格** ⇒ **中文卡面那枚徽记也画不出来**（现读 `descZh` = `3：获得哨戒 2。`）；改法在 `cardface_fixes.json` 的 `descZh` 列，**但判据要先定**（中文是我们自己译的）（`W5e:235-241`）
- ⚠️ **正文【句中】的关键词图标仍然不画**（`UM74` = `Gain Sentry 2`，少一枚哨戒徽记）—— 是 `KEYWORD_SCAN` 只认句首的**既有设计**，不是本笔回归（`W5e:242-246`）
- `A1345`（`KeywordSegment` 判据①压掉 `UM74` 该印的 `Sentry 2.`）**本笔没碰**（`W5e:247-249`）
- ⛔ **「点一下真能激活誓约能力 / 真按 4·3·1·3 扣费」仍未验** —— 那是 `A1331` 挂着的半笔（`RuleEngineTest.Run`）（`W5e:250-251`）
- `Editor/IconSetup.cs:268` 的 `★②c【A980-c】` 注释**指向已失效的行号/条目**（引 `gen_icon_plan.py:187`，现读 `:194` 已订正成 `("DA44","[Ranged]")`）（`W5e:257-262`）—— ⚠️ **`W8` 已就地订正**（`W8:132-154`）
- `gen_icon_plan.py:47` 的 `OUT_MD` 是**死变量**（`main()` 从不写它）；文件头「产物」第 2 项与实际不符（`W5e:263-265`）
- `Unity/工具/` 下两个**未跟踪**脚本 `fix_booster_prefabs.py` · `gen_tmp_font_assets.py`（都不是本笔产生的）（`W5e:271-273`）

### `W6a_A1344与A1357数据侧.md`

- 🔴 同一族**另外 5 张 / 6 处**（`SOR28`/`SOR43`/`SOR44`×2/`SOR49`/`SOR59`）：`R6` 的判据锚在 **desc 开头**、把非首句的全漏了（`W6a:216-230`）—— ⚠️ **已由 `W7` 做掉**（`W7:11-23`）
- `R6` 那条判据的**措辞值得就地订正**（写「全池命中恰 4 处」，读者会以为全池只有这 4 处；逐分句实测 = 13 处）（`W6a:231-234`）
- `A1344` 修完后 4 张**会不会丢徽记**（与 `A1347` 同形？）`W6a` 没查（`W6a:235-239`）—— ⚠️ **`W7` 的 `A1364` 给了证据化答案：不丢、净增 5 枚**（`W7:80-129`）
- ⚠️ **`python -I` 隐含 `-E`、会忽略 `PYTHONIOENCODING`** ⇒ `资料/命令速查.md` 若写了「要带 `PYTHONIOENCODING`」建议补一句「**别加 `-I`**」（`W6a:240-242`）—— ⚠️ **`W8` 实测与这条不符**（见下）
- `cardface_fixes.json` 的 `desc` 列对同名卡用**裸卡名键**（本批 4 个名字全池各 1 张、无跨阵营串味）（`W6a:243-245`）
- ⚠️ **`Crusader` 这个名字很危险**：`SOR13 Hymn of Battle` 的 `desc` 里含 `… or Deploy a Crusader` ⇒ **改这个文件的锚点一律取整行**（`W6a:246-248`）

### `W6b_引擎四笔.md`

- ⚠️ `工具/ruleprobe` 的 dump 原不打印 `Cost`/`CostShared`/`ConditionKind`/`CountRef` —— 而这四样正是「付费/条件/计数」三族改动的**全部可观测面**（`W6b:306-309`）—— ⚠️ **`W9` 已补后三列 ＋ `Cost`**（`W9:14-16`）
- ⚠️ **新账**：`ParseSegment` 的「`, plus`」两支，`r = rHeadB` 换掉 `r` 之后**付费前缀只贴 `rPlus` 那一批**，`rHead` 那批 op 的 `Cost` 是 **0**（= 前半句**白给**）；今天不可观测（唯一形状 `Alpha Warrior` 没有付费前缀）（`W6b:310-314`）
- ⚠️ **新账**：`for each` 计数型 op 仍会按 `repeats` 遍**重复付费**（`EffectResolver.cs:244` 那个循环），`CostShared` 挡不住；今天没有「付费前缀 ＋ for each」的卡 ⇒ 不可观测（`W6b:315-317`）
- ⚠️ **新账**：`CostShared` 堵不住「付钱那条被 `ResolveOps` 的 `instead` 机制整条跳过」的洞 —— 同组其余 op 会一分钱不掏地结算；今天 0 张可观测（唯一含 `instead` 的付费卡 `SOR49` 那段只有 1 条 op）（`W6b:326-332`）
- ⛔ **建议不要再按 `R6` 原文去改 `ResolveOathAbility`** —— 那会变成**第二份付费判据**（`W6b:318-321`）
- `R6` 的 `A1358` 候选 5 张 → **实读只有 2 张**真出 2 条 op（`UM93` / `SOR6`）（`W6b:322-323`）
- ⚠️ 「一份前缀 ＋ 一串 op」的**收钱口径**落在 `ResolveOneCore`（与 `R6` 建议的 `ResolveOathAbility` 不同）；两处唯一可观测差别 = 同一张卡有两份不同价誓约前缀时付 7 vs 5，**全池 0 张** ⇒ **哪一条是原版行为没查，判据未定所以没动**（`W6b:284-289`）
- ⚠️ `Talent:` / `Companion N:` / `When …` 这类「别的层接手的句子」**不参与 carry 清零**；今天不可观测（唯一形状 `ASH42` 的 `Talent:` 段产出 0 条 op）（`W6b:290-295`）
- `EffectCondition`（`EffectText.cs:7704`）是**另一个顶层类**，看不见 `EffectText` 的私有成员（`W6b:324-325`）
- ⚠️ 结算层那三处读数（`UM81` −3 · `UM93` −2 · `SOR6` −4）**没有真跑**，只有代码级证据（`W6b:271-278`）

### `W6c_A1350A1351A1359.md`

- `Editor/BattleScene.cs:9643-9647` 与 `Editor/SettingsScene.cs:3342-3344` 两处注释写着「`WfSlider` 上**没有**对外访问器」—— **现在有了**（`FillWorldW` / `FillCapWorldW` / `FillWorldLeftX`）⇒ 已过期（`W6c:71-73`）—— ⚠️ **`W8` 已就地订正**（`W8:17-35`）
- `Editor/SettingsScene.cs:3443-3450`：`s.SetValue(1f, false)` 的理由写着「值拉到 1 ⇒ 填条不缩放的那一帧」—— ⚠️ **对 `capFl` 其实没用**（`capFl` 读 `WorldW`，而 `WorldW` 从来不含缩放）（`W6c:74-76`）—— ⚠️ **`W8` 已订正理由句**（`W8:37-47`）
- `Shell/SettingsWindow.cs:1429` 拿 `_fillRoot.transform.localScale` 当旁证 —— 那一句 **`A1359` 起改到了三块子 quad 上**（`W6c:183-184`）—— ⚠️ **`W8` 已就地订正：那句【仍在】、只是降级为后备路**（`W8:48-66`）
- 全仓 `AddComponent<ViewportClip>()` 生产站点共 **7 处**，两条滑块父链都不在其中 ⇒ `A1359` **不涉裁切**（`W6c:194-196`）
- `Shell/MenuDraw.cs:574-604` 的 doc 块有**既存** XML 格式错（`CS1570`，`WF_DOC=1` 看得见）（`W6c:192-193`）
- **`A1350` 第②③步 ＋ `RenderedRect`（`:244`，35 个调用点）＋ 4 份 `TmpRenderedRect` 副本 ＋ `ShellScene.NodeRectPx` 系列 ＋ 余下 ~121 处 `PxOf` 调用点 —— 一律没做**（`W6c:169-173`）
- ⚠️ `WfSlider` 的**后路分支**（`_fillAnchorMode == false`，旧的 `localScale` 写法）**留不留 ⇒ 待调度台裁**（`W6c:177` ＋ `:131-134`）
- 🔴 `A1359` **没有任何实跑证据**；「端帽在屏幕上是 15px 而不是 7.5px」**是算式推的**，唯一能拍板的是同步点自检 ＋ 真 Play（`W6c:128-131`）

### `W7_A1363与A1364.md`

- 🔴 **`Stun` 从句首那枚金螺旋图标，全池一条都没画** —— `Resources/Art/traits/stun.png` **盘上有**，但不在 `gen_icon_plan.py` 的 sprite 词表里（全池 2140 条 icon 条目 0 条含 `stun`；`DA39` 在 icon plan 里**整条卡都不存在**）；**判据未定**（「原版该印、我们漏了」还是「只是强调记号」）（`W7:207-214`）
- ⚠️ `SOR44` 那句「`as well`」卡面**没有句号**、我们的 `desc` 有（`.`. `\n` `\r` 都是分句界、功能无影响）（`W7:215-218`）
- ⚠️ `DA39` 的 `desc` 与卡面的「`Rally:` 在不在正文」**不一致**（`DA39` 不在正文、`SOR28` 在正文）—— 哪个对没查（`W7:219-222`）
- ℹ️ `A1344`(`W6a`) 那 4 笔的**图标增量此前一直没人量过** —— 本批顺手量：`2131 → 2135`（+4、0 删）（`W7:223-224`）
- ⚠️ `工具/gen_icon_doc.py` 没跑 ⇒ `资料/卡面图标_对照与缺口.md` **更过期了**（`A1361` 又被推远一步）（`W7:196-197`）—— ⚠️ **`W8` 已跑、现读 655 / 2140**（`W8:88-128`）
- ⚠️ **并排渲一张验收没做**（铁律 10⑥ / `A1364` 简报也点名要）—— **挂给收口那趟**（`W7:198-199`）

### `W8_A1365与A1361清理.md`

- ⚠️ `Editor/SettingsScene.cs` 那条 `< 200f` 启发式**没有换成** `FillCapWorldW` —— `W8` 给了完整**静态论证**（换法与现写法**同值**、判别力为零，只是去掉「隐藏耦合」＋ 把「端帽」从「按宽度猜」变成「指名道姓」）⇒ **要换由调度台在收口时另开一笔**（`W8:225` ＋ `:67-85`）
- 🔴 **简报 ④-4 的前提被现读推翻**：`Battle/WfSlider.cs:428` 那句 `_fillRoot.transform.localScale = …` **仍在**（只是从常规路径降级成 `Layout` 的后备路）⇒ 拿它当「空指针」写进注释就是**换一条新错话上去**（`W8:239-243`）
- ⚠️ **`python -I` 本机实测与「会崩在 GBK」不符** —— `W8` 用 `python -I gen_icon_doc.py`（不带 `PYTHONIOENCODING`）跑过：`EXIT=0`、中文正常、产出逐字节相同（大概是脚本自己 `sys.stdout.reconfigure`）；⇒ `资料/命令速查.md` 那条**目前与实测不符**，怎么改由调度台定（`W8:244-250`）
- 🔴 **「盘上有图、计划表一条都没用」这一类在本工程没有任何可观测点** —— `IconSetup.Verify` 只查【计划表里的 sprite 名 → 资产里有没有】，`gen_icon_doc.py` 只把计划表原样铺开 ⇒ `stun` **正是从这个缝里漏过去的**（`W8:251-256`）
- ℹ️ `资料/卡面图标_对照与缺口.md` §七 那句「`traits/` **78 张**（73 ＋ 5）」—— 前半是**算的**、后半是**硬编码的**；只要多一张不是图集切片的图，两个数就会打架（`W8:257-261`）
- ℹ️ `gen_icon_plan.py:47` 的 `OUT_MD` 是**死变量**（`grep -n OUT_MD` 只有 `:47`；`W8` 独立复核过，与 `W5e` 同一条）（`W8:262-264`）
- ℹ️ `Core/CardIcons.cs:7` 的文件头例子仍写**裸词** `"+3 Ranged"` 当 `DA44` 的真 `desc` —— 与现读 `"+3 [Ranged]"`（**带方括号**）不一致（`W8:265-266`）
- ℹ️ 两份图标文档都是**纯 LF、且以 `\n` 结尾（无结尾换行）**——下一个手改它的人注意（`W8:271-272`）

### `W9_A1368探针补列.md`

- ⚠️ `countRef` 的 `|` 让「按 `|` 数列」这个土办法失效（旧基线 19 行 → 本笔推到 **44 行**）；正确做法是**只认前 2 个 `|`、其余按 `/` 与 `;` 切**（`CheckMain` 就是这么写的）（`W9:215-217`）
- ⚠️ **`资料/已知的坑.md:5725` 与 `:5733` 的行号引用被本笔推漂**（写 `Scan.cs:34`，现读 `:63`）；同一句还复制在 `普查产出_1018/S11_UM85与探针脚本.md:190` 与 `W2_A1311代词目标补侧.md:84,96`（`W9:199-203`）
- ⚠️ `普查产出_1018/DB_手牌效果登记6红.md:86` 的 dump 格式描述**行号与内容都不对了**（历史快照、按铁律 6 可留）（`W9:204-205`）
- ⚠️ **`filter` 模式对「没有 `choosecard` op 的卡」零输出** ⇒ 看着像命令没跑（`filter zzzznotacard` 反而会打 `!! no card …`）—— 既存行为、不是本笔引入（`W9:206-208`）
- ⚠️ **`CountScope` 与 `Condition`（条件原文）两列仍没打** —— 于是 `condKind=` 为空时分不出「没条件」与「有条件但认不出」（后者是**已知的静默风险**）（`W9:177-183`）
- ℹ️ `资料/引擎离线对拍_探针.md` 没跟改（缺一节指向 `ruleprobe/README.md` 的指针）（`W9:213-214`）
- ✅ `W6b`「没查清 3」（`SOR72` 的 `for each enemy unit` 遍数没观测到）**现在可销账**：本笔补列后实测 `countRef=[enemy\|unit\|all]`（⚠️ 结算侧真跑一遍仍归 `RuleEngineTest`）（`W9:209-212`）
- ⚠️ `RuleEngineTest` 里**有没有断言依赖旧基线** —— **没查**（`RuleEngine/**` 是黑名单；判据：基线是探针自己的产物，但只做了静态判断、没跑过）（`W9:187-188`，与 `W5d:154-156` 同一条）

### `D_A1334诊断.md`

- ⚠️ **注释过期（会误导下一个人）**：`RuleEngineTest.cs:2418-2423` 写着「`gain Fast` 是无目标的正文、在 `DoGive` 里会落到**己方全体**」，与**同文件 ⑥**（`RuleEngineTest.cs:2448-2456`「⛔ 不再洒给己方全体」）**相反**，也与本报告 ① 相反（`D:111-114`）
- 🔴 **`ruleprobe` 与引擎不同口径**（台账 `A1339`；`W5d` 已修）（`D:115-124`）
- ⚠️ **潜伏账 `A1340`**：`CardDatabase.cs:113` 的 `CreatePool.BuildNameIndex(list)` 排在**所有 `CardDef` 构造之后**，而 `CardDef` 构造期就缓存 `_whenTriggers`/`_oathOps` 等 ⇒ 这些缓存里的「卡名目标」**永远是 null**（现读全池卡名目标只 2 张、**都是 tactic** ⇒ 今天无实际受害卡）（`D:125-130`）
- ⚠️ `SAU72 Immortals Phalanx` 在 baseline 里 **ops 列为空**（同族另一种形状；运行时是否靠别的层生效**没查**）⇒ 已并入 `A1340`（`D:131-133`）
- ⚠️ 「无目标 ⇒ 己方全体」这句话的**原版判据我们手里没有**，只有旁证（`rule_core.gd`）⇒ ⛔ **别把它当权威去定 `null` 的语义**（`D:56-59`）
- ⚠️ 台账 `A1311`「`UM_Angels_of_Death` 是 3 参重载唯一的真卡用户」**很可能也是同一处假象**（引擎里它 `Side=own`、走 4 参）⇒ 改法结论不变，但「唯一真用户」这句该订正（`D:122-124`）
- ⚠️ `A1334` 的 **⑤-1 主修**（`EffectText.cs:1849-1861` 之后补 `When <事件>, …` / `After receiving a Dark Pact, …` 分派）影响面 **11 行**；⚠️ 会连带改 `Coverage` 的两个计数 ⇒ **动之前先看 `RuleEngineTest` 里有没有钉住这 10 张的断言语料**（`D:79-85`）

---

## 四、我自己没查清的部分（如实）

1. 🔴 **报告里的行号全部是「当时工作区快照」**，`EffectResolver.cs` / `BattleDriver.cs` / `EffectText.cs` 在飞（`D:5` 明写「复核时按**内容**、别按行号」）。⇒ 本表的落点行号**照抄报告**，⛔ **我一个都没现读核过**（我是只读普查、且核心 `.cs` 不在我白名单，核了也不改）。
2. ⚠️ **断言条数按「一行一条」拆的**，含我自己的拆分决定（例：`W6b` 的 A1342 一节我拆成 #1–#4；A1346 一节拆成 #5–#9）。报告原文是**成组写**的，**组数 ≠ 条数**。
3. ⚠️ **`W5e` #25 / #29 的数字（2131）已被后续批次推过期** —— 现读是 `2140`（`W8:100-102`）。我在格里注了，但**没去核 `IconSetup.Verify` 今天到底报什么**（那要跑 Unity，红线）。
4. ⚠️ **哪些断言落哪个宿主，有 3 条是报告「推定」而非「写明」的**：
   - #16 / #17 报告只说「收口那趟该带 `RuleEngineTest.Run`」，**没说具体断言落在哪一格**；
   - #25 / #26 是 `W5e` 的「怎么验收」节，**它自己没叫「断言」**，是我按可执行性收进来的；
   - #27 / #28 是**工具侧**（非 Unity 宿主），报告只写「建议补」，**没给落点行号**（#28 的 `Scan.cs:74` 是报告原文给的）。
5. ⚠️ **没有一处我去核过「这条断言写下去会不会顶掉既有断言」** —— 那是写手该在落地时查的（报告里只有 `D:85` 提醒了 #15 那一族要查）。
6. ℹ️ **`W8` 那份报告我一条断言都没抽出来** —— 它自己说「零行为改动」，这不是我漏读（逐节读过：五处纯注释 ＋ 一条消息文本 ＋ 两份文档数字）。
