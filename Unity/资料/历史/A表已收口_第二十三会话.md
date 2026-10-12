# A 表已收口 —— 第二十三会话

> 本文件收 **第二十三会话（2026-10-12）** 收口/核销掉的账。每条带「谁做的 · 关键结论 · **还欠什么**」。
> ⚠️ **账表里对应的整行已搬到这里**（`资料/任务_卡组卡牌战斗.md` 只留【还开着的】）——
> 但**行尾那半句「还欠什么」如果还成立，就【同时】在账表上另立了一行**（搜编号即可）。
> 判据全文一律在 **`资料/普查产出_第二十三会话/`** 的各份报告里（本文件只留结论与指针）。

---

## 一、引擎 · 解析层（写手 `W1`）

| 账 | 谁做的 | 关键结论 | 还欠什么 |
|---|---|---|---|
| **`A1686`** | `W1` | **前缀形 `When <事件>, <效果>` 现在与后缀形（`A1461`）落在同一条路上** —— `Dispatch` 新增 **8c-ter** → `whenclause` **标记 op**，切句判据**转调 `CardDef.TryParseWhenSentence`**（与注册层/报表层同一个函数，⛔ 没另写第二份）。**改前全池 13 张**「事件从句被当成目标段」的垃圾 op ⇒ **改后 0 张** | 🔴 **引进两条既有断言的回归**（`RuleEngineTest` 红 1 / 红 4）—— 根因与修法见 `DIAG_五条红.md`，**正在修**；另见下方「新账 §1」 |
| **`A1446`** | `W1` | `Lower cost by N when <事件> instead` 的两半**共用同一个剥离口＋同一份抽取判据**（新 `EffectText.TryCostWhen`）⇒ `CardDef.CostWhens.Count` **0 → 1**；**不带 `instead` 的那一支逐字不变**（`AM60`/`AM61`/`TAU66` 本来就在） | ⚠️ **`instead` 的结算语义 = 判据空缺**（搜过全池 1126 张 + `card_stats.json` 1213 张、原版 26,282 个方法体：**该形状 0 张**）⇒ 只把那个词**如实传出去**（`EffectOp.Instead` / `CostWhen.Instead`），**不据此改结算**。⛔ 没替它发明 |

**报告** → `资料/普查产出_第二十三会话/W1_解析层A1686A1446.md`
**同批自报读数（`D-S23-1` 复核过、都对）**：`scan` 的 `T:Raw=when` **13 → 0 行** · `unparsed` 非空 **176 → 129** · `partial` 非空 **10 → 1**（只剩 `BL25` 的 `After receiving a Dark Pact`，另一种形状）· 逐行 diff **78 行**（其余 1042 行逐字节相同）· `willrun` 事件层认领 **26 → 81**、(c) tactic `收到 444/空表 1` → **`445/0`** · 「收漏 / 无消费点 / 段级」仍 **0/0/0**。

---

## 二、引擎 · 结算层（写手 `W2`）

| 账 | 谁做的 | 关键结论 | 还欠什么 |
|---|---|---|---|
| **`A1682`** | `W2` | 🔴 **原版语义查实**：`AbilityEffect.summonUnit(70)` → `AbilityLogic__PlayAbility.c:733-1010` → `BattleManager._ResolveSummonUnit_d__510__MoveNext.c` → **`BattleCardManager__CreateCard(RawCardScript)`** ⇒ **复制品 = 原始定义那一档**（不是当前状态）；落点 = `MinionManager__GetNextSlotWithoutDisplacing()`。改 `EffectResolver.cs:2397-2431` 新增 `pool.CopyOfPrev` 支。**读数**：`SAU72` 改前 **1 → 1 份**（+日志「部署不了 —— 这条没生效」），改后 **1 → 2 份**（复制品满血 `3/7`、带 `remnant`/`regeneration`、**实例号不同**、原件仍在原格） | ⚠️ `SummonCriteria.keepEffects` / `chooseCardInPlay` 对 `SAU72` **实际填几读不到**（卡数据在远端 CCD）⇒ 按「原始定义」实现（**这是我们挑的**）。⚠️ 「复制品带召唤病」按同一条入口推、**没逐条读**原版。⚠️ `deploy a copy of it/them` 今天 0 张，指代**只取 `ctx.LastTarget`** |
| **`A1685`** | `W2` | 把 `instead` 的「配对 + 裁决」**收进一份共用 `InsteadSkips`**（`EffectResolver.cs:99-152`），出牌重载改调用（`:240`，A/B 逐字不变），**能力层也接上**（`:5073`）。**读数**：能力层无条件 `instead` **9 伤 → 6 伤**（与出牌那条路同值） | 🔴 **姊妹账**：能力层【**有条件** `instead`】条件成立时**两条都结算** ⇒ `ASH31`/`ASH36`/`AM40`/`DA37`/`GOF94` **过伤**（实测目标带 `Flying` 时打 9、卡面正解 6）。⚠️ **先于本笔存在**（A/B 逐字相同）⇒ 已另立账 |
| **`A1617`** | `W2` | 照调度台裁定做（**没自己再裁**）。判据 = `_ResolveAttack_d__438__MoveNext.c:339-379`；**闸 = 「有没有 `dropPod`」**（原版那两跳在同一个 `has dropPod` 分支里）· 位置 = 扣配额/现身/失去伪装之后 · 取消 = **最小档**。改 `RuleCore.cs:5022` `OpenDropPod` → `bool`。**读数**：守方 **6/9（挨 3）→ 9/9（0 伤害）**；配额照扣（`Exhausted=True`）；两组对照改前改后逐字相同 | ⚠️ 「**撤事件**」那一半**判据为空** ⇒ 按最小档（`return OK`、不撤 `EvtKind.Attack`、不撤替身）—— **这是我们挑的**，已写进 `KeywordTable.Landing` 的 doc。⚠️ `CardDef.cs` 两处 doc（`Landing` / `DropPod`）**由本会话调度台补改** |

**报告** → `资料/普查产出_第二十三会话/W2_结算层A1682A1685A1617.md`
**验证底座**：`D:/tmp/wf_w2/`（`dotnet` 直编 `Core/*.cs`）＋ **A/B 对照** `D:/tmp/wf_w2_base/`（HEAD 的 `EffectResolver.cs`/`RuleCore.cs` + 当前其余）⇒ 12 格 A/B 矩阵，只有本笔该动的格子不同。

---

## 三、引擎 · 离线探针（写手 `W3`）

| 账 | 谁做的 | 关键结论 | 还欠什么 |
|---|---|---|---|
| **`A1478`** | `W3` | ① **csproj 末尾那句注释就地订正**（带 `⚠️ 更正` 痕）：**「`Core` 里已经没有任何 Unity 类型了」不成立** —— 拿掉 `Stub.cs` ⇒ **6 个文件 / 18 处** `CS0103: UnityEngine`（⛔ **账上原来那个「53 个错误」复现不出**，三种 verbosity 下都是 36 行 = 18×2 的重复回显）。② 那 6 个文件的 `Debug` **一处没改**（裁定：最小改动 + 不动语义）。③ 另加零风险提示：`ruleprobe.sh` 的失败分支多打一句「错全是 `CS0103 …UnityEngine` 且落在 `Core/*.cs` ⇒ 是 `Stub.cs` 掉了」 | ⚠️ 「53」的差因**未坐实**（只给最可能原因：那次是**仓库外副本工程** `D:/tmp/wf_a1440/h1/`）。⚠️ 没加第二份 `.csproj`（构造性实测：同目录两份 csproj ⇒ `MSB1011`，会**直接打断** `ruleprobe.sh`） |
| **`A1590②`** | `W3` | dump 补上 **`Source`**（四处渲染 + 哨兵 ⑧，红法实测：去掉渲染 ⇒ 红）。**先决条件 `A1568①` 判据完整有出处**（`EffectText.cs:102` 的 `EffectOp.Source` 原样）—— ⛔ **不是** `③`「该打哪些字段无出处」那条。🔴 块**必须排在 `/{T:` 之后**（排前面会被 `Sentinel.Legacy` 吞进 legacy 段） | 🔴 **`A1439` 的精确两路对比还差一步**：**非问玩家**的收集层 op（`CardDef.SpiritOps` / `TriggerOps`）没有任何入口打 `Source` ⇒ 要收 `A1439` 还得给 `WillRun` 补 `Source`（**调度台已裁：要做**，见新账）。⚠️ 基线已重生成（**含别人 89 行漂移**，见新账） |

**报告** → `资料/普查产出_第二十三会话/W3_探针A1478A1590b.md`

---

## 四、引擎 · 断言（写手 `W5`）

| 账 | 谁做的 | 关键结论 | 还欠什么 |
|---|---|---|---|
| **`A1337`（后半）** | `W5` | 定出「判据落点」的**可操作定义**并落成检测器 `S23ReadScan` + 断言 `TestS23AssertImplementedReadSites`。**读点 = 词的字面量（大小写无关）或常量标识符出现在五种取值语法位之一**（`call` / `cmp` / `case` / `return` / `index`）；**⛔ 不算读点**：初始化器成员 · `const` 声明 · 注释 · 赋值右值。**真池读数（本会话实跑）**：`Core/*.cs` 22 份 · `Implemented` **70** 词 · 读点 **221** 处 · **无读点 0 个**。灭自证 = 「同一份只有一张表的合成源 ⇒ 前半必报 0 缺、后半必报 1 缺」（两句同一输入、要求相反） | ⚠️ 判据**偏宽一处**（如实标着）：`RuleCore.cs:7836` 的 `Hurt(ctx,t,dmg,"Unstable")` 是**来源标签**不是判据，却会被算成读点。要再严得判「被调函数是不是判据」，另一个量级 |
| **`A1590①`** | `W5` | 🔴 **账文前提已失效**：`A1413` 那两格代码**已被上一会话 `A1412` 删掉**（收费抬到 `ability` 层）⇒ **改钉【行为】、不钉那两行**。断言 `TestS23AssertA1413PayGroup`（22 条），期望值来自原版反编译（`PayActiveAbilityCostOath.c:24-27` · `CanUseActiveAbility.c:68-71`）+ 离线探针实测。判别式 ①「Δ 与 N 无关」②「付不起 ⇒ 标着 `CostShared` 的后半句也不许落地」（🧨 改前**白给 `flank`**） | ⚠️ `A1413` ① 夹具是**合成卡**（全池 0 张可达）。⚠️ ②的「改前会白给」**没实测过**（那两行已删、无法复现）。⚠️ 反控那句**只断 `rc != OK`**、不锁码值（实测 13 = `ErrUnimplemented`，语义可疑 ⇒ **未裁定、故意不固化**） |

**报告** → `资料/普查产出_第二十三会话/W5_引擎断言A1337A1590a.md`

> 🔴 **`A1590` 只收了 ①②**，**③（`A1397②` 搬迁后 `Auras.Recompose` 排在 stealth 到期之前、「全池 0 张卡用光环授予 Stealth」那个「0 张」未复核）仍开着** ⇒ 账表上留着。

---

## 五、卡牌线（写手 `W4`）

| 账 | 谁做的 | 关键结论 | 还欠什么 |
|---|---|---|---|
| **`A1627`** | `W4` | **夹具前提错，不是生产代码坏。** ⑦/⑦b **从加上那天起就结构上不可能绿**：三张合成探针卡原来 `desc: ""` / `descZh: ""`，而 `BattleDriver.FaceTextFull` 有一条**空正文早退**（`:11959`）⇒ 探针**根本走不到** `KeywordSegment`（`:11975`）。**已修**：① 夹具补一句**不含关键词声明**的 `desc`；② 各补一条**前提断言**（`⑦·前提` / `⑦b·前提`），前提破时**说得出名字**。判据另证：「那一行比断言早」（`git log -S` → `720df56` 2026-09-15，断言是第二十会话加的）⇒ **born broken** | ✅ **本会话调度台已跑 `IconSetup.Verify` 收口：全部通过（rc=0）**，含 `⑦·前提` / `⑦b·前提` 两条新的，实测输出非空。⛔ **`BattleDriver.cs` / `CardText.cs` 一个字没动**（不在 `W4` 白名单，判据上也不需要） |

**报告** → `资料/普查产出_第二十三会话/W4_卡牌A1627.md`
**`W4` 顺手查出、本会话调度台当场修掉的那一笔** → 见下方「新账 §1（`A1687`）」。

---

## 六、本会话调度台自己下场做的（不属任何账，记在这里备查）

| 事 | 结论 |
|---|---|
| **特效线 ③ `Extra Color` 的 `_Color` 那一跳** | **结清**：48 个 PS 变体全反汇编 ⇒ **`cb2[1]` = `_Color`，且确实喂最终颜色**（32/32 VS 变体写 `o2 = LinearToSRGB(_Color)×vcol`，12 个 Universal-Forward PS 变体全乘 `v2`）。上一轮那两条推断**都反了**，已在 shader 文件头就地订正。报告 → `V23_特效ExtraColor的PS读数.md` |
| **特效线 ④ 表驱动收口** | `gen_shader_blend.py` 补 3 个包（86 → **108** 条）+ 新增 `cull`/`zTest`/`colMask` 三组六列；`OriginalShaderBlendTable` 新增 `CullMap`（78 条）+ `InferCull` 改**查表优先**；两个 `Legacy Shaders/Particles/*` 补 `ColorMask RGB`。🔴 **过程中查出生成器自己一个真错**（一律读 `pass0`，而 `Particles/Standard Unlit` 的 `pass0` 是 `gpuProgramID = -1` 的空 pass）⇒ 已修 + 加「失败就删掉半好的表」 |
| **特效线 ⑤「白名单还剩几个弱兜底」** | **现读定案 = 10**（判据 = `UseOriginal` ∩（`Replacements` 值 = `WarpforgeVFX/Particles/Extra Color`））。正本里「9 / 10 / 11 三个数并存」已就地订正为一个数 + 一条可现跑的命令 |

---

## 七、调度台在同步点自己收口的（含两条「诊断 + 修 + 复核」闭环）

| 账 | 结论 |
|---|---|
| **`A1619`**（**四个指纹全钉**） | 原文（「两个/三个都要重钉、但现在别钉」+「等 `C5` 交件后是最后一个窗口」）**到此结清**。**现读**：`PinnedPoolMd5` = `0acd13bde721a78297f11e1a35681a97` · `PinnedBaselineMd5` = `bf852491965a8a55f5b7446cbc428ff8` · `PinnedParseMd5` = `c40f8f2191fb53cdd3eb76880fdec235` · **`PinnedPlayMd5` = `83655a105f8eeb8111eab9f0fa479fc7`**（第四个原来没人提）。**判据**：`check` ⇒ **解析差异 0 行 · 哨兵失败 0 条 · rc=0**；`play check` ⇒ **逐卡全等 · 哨兵过 · rc=0**。🔴 **重钉依据（⛔ 不是把红抹平）**：① `RuleEngineTest.Run` **5246/5246 全过**（上一版 5098）· ② 解析基线重生成**只动 1 行**（`SW25\|Long Fang`，与 `F1` 自报吻合）· ③ 结算基线 21 张**逐条归因完毕**（`W1` 的 `faceOps` 1 张 ＋ 上一会话 `A1605` 的 `SOR52/55/66` ＋ `A1599` 的 `bucket` 17 张）。逐条理由写在 `Program.cs` / `PlayProbe.cs` 各自常量上方 |
| **`A1691`**（红 1 / 红 4 两条回归 —— **诊断 → 修 → 复跑闭环**） | 只读诊断代理 `D-S23-1` 判 **(γ) 本批回归**（并把「哪几条是 (α) 断言钉旧形状」分开）⇒ 写手 `F1` 修完 ⇒ **`RuleEngineTest.Run` 5246/5246 全绿**。**① `FillAdjacentAnchors` 补 ⑤·三**（`EffectText.cs:1495-1515`，`whenclause` 标记 op 也要过「相邻锚点认不出」那道闸；+ 新助手 `HasBareAdjacent`）· **② `WhenEvents.ParseAll`/`Parse` 加显式 `bool recordUnknown = true` 并逐层透传**，解析层的**试探**传 `false`、注册层保持 `true`（`WhenEvent.cs:466/491/502` · `CardDef.cs:2391/2436/2449` · `EffectText.cs:7046`）。**收益没丢**：`T:Raw=when` 仍 0 · `whenclause` op 仍 78 · `unparsed` 仍 129（没弄回 176）· 事件层认领仍 81 |
| **`A1687`**（卡面空正文吞关键词行） | ✅ **已修 + 实渲验证**（第二十三会话 · `W4` 顺手查出、调度台当场修）—— `BattleDriver.FaceTextFull:11959` `if (string.IsNullOrEmpty(body)) return new FaceBody(notes, "desc");` **排在 `KeywordSegment`（`:11975`）之前** ⇒ **凡 `desc`/`descZh` 双空、又有关键词的卡，卡面效果区整片空白**。现扫全池 1126 张双空 **6 张**（`ASH12` `BL11` `BL43` `GOF12` `TL12` `UM14`），**只有 `GOF12 Hornhelmz Boy` 带关键词**（`keywords:["Tide 2"]`）⇒ 今天影响面 1 张、但形状是坏的。**修法**：把 `seg` 的算法提到早退之前、早退那行改成 `seg + notes`；⚠️ **其余 1125 张输出一字不变**（关键词为空的卡 `seg == ""`）。**验证**：`CardFaceProbe.Run` 实渲 `_tmp_view/cardface/Hornhelmz_Boy.png` ⇒ 效果区**已画出 `〔骷髅〕Tide 2`**，与 PnP 成品卡 `d:/2/Warpforge部队卡片/Orks/3部队/Warpforge_12_Hornhelmz-Boy.png`（**调度台亲读**：效果区只有 `Tide 2` + 图标）**逐项吻合**；`keywords` 层实测 `text=` 以 `<link=tide><nobr><sprite` 开头（改前为空）。⚠️ **诚实标注**：我们印 `Tide 2.`（带句号）、PnP 不带 —— 句尾符由 `CardText.KeywordSegment` 统一决定（84/85 张带正文的印 `X:`、其余 `X.`）⇒ **不因这一张改口径**。⚠️ PnP 把卡名印成带撇号的写法，而**授权键表 `数据/本地化/i18n/zh_CN.csv:1693` 写的是 `Hornhelmz Boy`** ⇒ **不改我们的卡名**（PnP 是印刷品）。 |与 `A1627` 是**同一段代码**的两个方向：`W4` 查 `A1627` 时顺手发现 ⇒ 调度台当场修，**修完不影响 `W4` 的夹具修**（两者叠起来 `IconSetup.Verify` 全绿） |
| ⚠️ 一次**白名单偏离**（已接受） | `F1` 为「逐层透传」必须改 `Core/CardDef.cs`（中间那一层就是 `TryParseWhenSentence` / `FinishWhenSentence`）—— **纯加带默认值的形参 ⇒ 既有调用方行为一字不变**（`F1` 用全池 A/B 证过）。判据与先例同 `W9` 那次（`A1605` 写 `card_icon_plan.json`）⇒ **算必要改动、不算越界**；⛔ 但下不为例：派单要写全「这条改动必须经过哪些层」 |
| ⚠️ `W2` 报的「`ruleprobe.sh:93 unexpected EOF`」 | **复现不出** ⇒ 判**并发期的瞬时现象**（那一刻另有写手在改 `ruleprobe.sh`）。本会话收尾时 `bash -n` 通过、`check`/`play check`/`b19test`/`willrun` **四条全部跑通且 rc 合理**，**不立账** |
| ⚠️ `W2` 报的「探针锁在持有期间消失」 | **同上** ⇒ 并发期现象（另一个写手的脚本删了锁目录）。今天无并发，锁行为正常，**不立账**；但**并发派单时要知道这个风险**（`资料/已知的坑.md` 已有同族条目） |
| **`A1669`**（换牌期提示行文案同值两份） | ✅ **2026-10-12 调度台当场做掉**：`BattleDriver.cs:4452` 的字面量改成 **`SetHint(Loc.T("Battle/Hint/MulliganPick"))`**（同文件 `:4465` 早有 `Loc.T` 先例）⇒ **两处收成一处**；**现读 `grep 换牌中` 只剩 `Core/Loc.cs:1691` 那一份**（+ 两处 `BattleAutoDrive` 的无关日志）。⚠️ **改动前已核「没有断言引用这串字面量」**（`Editor/` 下零命中）⇒ 改它不会翻红。**行尾未翻**（CRLF 15157 / 裸 LF 0）· **`typecheck` 0 错 ✅**。（原判据 = `W2` 顺手查出 · 当时 `Core/Loc.cs` **冻结**故没改，解冻后即做） |
| **`A1706`**（`CardProgress.Level` 缺省 1 → `MaxLevel`） | ✅ **已落地**（第二十三会话 · 用户 2026-10-12 裁定「**改成默认最高级**」· 写手 `丙`）—— 改的是**一行**（`Shell/CardDetailPopup.cs:141`；⚠️ 简报点的 `Core/CardProgress.cs` **不存在**，`CardProgress` 是 `CardDetailPopup.cs:73` 里的 `static class`）。**判据（亲读反编译）**：原版**卡框档与升级面板吃的是同一个数** —— `UpgradePanel__CheckUpgradeRequirements.c:57-60` 的 `RawCardScript.get_CardTier()` 同时喂 `CardFramesSO.GetClanFrame(army, tier, …)` 与 `CardTierConfig.GetNextTierInfo(tier, …)`；我们拆成两条路、`CardArt.TierOf` 早已恒返 `MaxTier`（用户 09-27 拍板）而 `Level` 还是 1 ⇒ **同一个数在两处打架**。**用户可见**：详情窗升级面板标题 `level 2` → **`level 4`**；三格由「2 张/500 金/+100」→ 第 4 级那一行；**钮照旧灰着点不动**。✅ **专核过「不会坏别的语义」**：`Level` **不存盘**（纯内存 `_lvl`）· **全仓唯一写入点 `SetLevel` 只被 `DoUpgrade` 调**（钮灰着 ⇒ 永远取缺省）· 别处没有第二种含义；波及 10 处逐处列在 `BING_默认最高级与创建副本文案.md` §二·3（`CanUpgrade` 恒真→恒假 · `ReadyForLevelUp` 恒真→恒假 —— **这是修对**）。⚠️ **尾巴另立 `A1708`**（满级时 `No Upgrade Warning` 恒不露 = 几何硬约束；**已裁：用户口径优先**）。 |
