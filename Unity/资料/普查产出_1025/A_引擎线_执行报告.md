# 执行代理 `A` · 引擎线 5 件 · 执行报告（2026-10-25 · 第七十五会话）

> 白名单：`MyGame/Assets/RuleEngine/**` · `MyGame/Assets/CardPresentation/Editor/RuleEngineTest*.cs` · `工具/ruleprobe/**`
> 红线遵守：⛔ 没跑 Unity（任何 `-executeMethod`/`Unity.exe`）· ⛔ 没动 git（无 commit/checkout/stash/reset/add）·
> ⛔ 没改 `CLAUDE.md` / `项目任务.md` / 任何 `任务总表.md` · 改 `.cs` 一律 Edit/Write（没碰 `sed -i`）·
> 行尾逐文件核过（`EffectResolver.cs` 仍 CRLF 13841/13841；其余 LF 未翻）。

| # | 活 | 结果 |
|---|---|---|
| **A1** | `forceattack` 族把「受害者 criteria」套在「攻击者池」上 | ✅ **已修**（`EC46` 真的打起来了；`play check` 指纹里它回到基线那一版） |
| **A2** | `play` 基线重钉 | ✅ **已重钉**（旧 `c7236f26…` → 新 `87209677…`；`play check` **rc=1 → rc=0**） |
| **A3** | `talent`（Trigger 46）回合开始那一拍没有事件（96 行 / 96 卡） | ✅ **引擎侧已做**（①事件 ④断言）；⚠️ **②`VfxMap` 放行 / ③`TraitFrames` 记账都在黑名单里，未做** → 见 §A3·未做 |
| **A4** | 督军 `abilityTargets`：序列化顺序 ≠ 卡面文字顺序 | ✅ **已做**（按块头对齐 + 断言；今天 0 可观测变化，`play check`/`check` 双绿） |
| **A5** | 自检断言线 `T03`：`B6` 夹具用 `ResolveAll`、生产用 `ResolveAllTimed` | ⛔ **被白名单挡住，未做** —— B6 夹具在 `CardPresentation/Editor/BattleScene.cs`（"其它 `Editor/*.cs`" = 黑名单）→ 见 §A5 |

**验收读数（本批最后一次现跑）**

```
bash 工具/typecheck.sh                 → 运行时错误数: 0 · 编辑器错误数: 0
bash 工具/ruleprobe.sh play check      → 结算指纹 现算 87209677070c8421b8479e3bcc0effab == 基线（一致）
                                         ①-a 差异 0 张 · ①-b 差异 0 张 · ② 哨兵 ✓ → rc=0
bash 工具/ruleprobe.sh check           → 解析差异 0 行 · 哨兵合计 失败 0 条 → rc=0
```
⚠️ **`RuleEngineTest.Run` 没跑**（子代理禁止跑 Unity）⇒ 本批新增的 4 条断言只过了 typecheck，**要主对话在同步点跑**。

---

## A1 `forceattack` 族的 criteria 归侧（真缺陷）

### 结论（含一处**对既有结论的订正**）

修好了 `EC46 Peerless Bladesmen`，但 **`W25` 报告 §六·1 的处方「把 criteria 挪到受害者池」只对一半的动词成立**，
不能当通则：

| `chosenAbility` | 原版行为（第一权威逐句读） | criteria 建出来的是**谁** |
|---|---|---|
| **230 `forceAttack` / 231 `forceAttackDontCountAttack` / 233 `forceAttackFriendly`** | `AbilityLogic__PlayAbility.c:2641-2694` 把 `possibleTargets[i]` 传给 `AddForceAttack(targetCard, …)`；而 `BattleManager._ResolveForceAttack_d__504__MoveNext.c:33-66` 拿**那张卡**去 `AI__SelectBestAttackAction(…, card, …)` 再 `AddScriptedAttack(attacker = 那张卡, …)` ⇒ **那张卡是被逼着去攻击的那一个** | **攻击者** |
| **235 / 236 / 237 / 238 `scripted*`** | `case 0xeb/0xec/0xed/0xee`：攻击者由能力本身定死（`abilityData + 0x10/+0x18/+0x20`），`possibleTargets[i]` 一律落 `AddScriptedAttack / AddScriptedForceAttack / AddRedirectedAttack` 的 **targetCard** | **受害者** |

⇒ 数据反证：`BL64 Murderous Desires`（`chosenAbility = 231`）的 criteria 是
`{targetsAffected:30, conditionsFilter:341(damaged), excludeCondition:1}` —— 它描述的是**被点名的那个友方攻击者**
（卡面 `Make a damaged friendly unit attack by itself`）。若按 `W25` 的处方一律挪到受害者池，会把
「damaged」筛到敌人身上（真偏离）。同理 `DA25`（231，`ta=20 self`）。

**实际改法**（`Core/EffectResolver.cs` 的 `DoForceAttack`）：
新增 `critOnDefender` 判据 —— `op.Target != null && op.Target2 != null && op.TargetCriteriaSet != null && Payload != "byitself"`
⇒ 攻击者池**不**过 criteria、受害者池（`DefenderPool`）**过**（并且是交给 `ResolveTargets` 在**裁剪之前**过，
与 `AbilityLogic__GetTargets` 的次序一致：先筛、再 `maxTargets`/`targetSelectMethod`）。

### 影响面（逐个列出，⛔ 不猜）

`cards_truth.json` 现读：`chosenAbility ∈ {230,231,233,235,236,237,238}` 的卡 **12 张**（池内 10 张）：

| 卡 | chosenAbility | 卡面 | 两栏都点名？ | 今天行为 |
|---|---|---|---|---|
| **EC46** Peerless Bladesmen | **237** | `Target friendly unit attacks the enemy with highest attack` | ✅（全池**唯一**） | 🔴 改前：criteria 套攻击者池 ⇒ `4 → 0` 空过；**改后真打** |
| BL64 Murderous Desires | 231 | `Make a damaged friendly unit attack by itself` | ✗（只有攻击者） | ✅ 本来就对（criteria 归攻击者） |
| DA25 Deathwing Knight | 231 | `… attack by itself`（`ta=20 self`） | ✗ | ✅ 不变 |
| EC12 / EC22 / SAU70 / SW9 / GOF86 | 238 / 238 / 238 / 238 / 235 | 都是裸 `Attack(s) <受害者>` / `Attacks …` | ✗（`Target` 为空 ⇒ 攻击者=本卡） | ⚠️ **criteria 今天根本没用上**（`op.Target == null` 不走 `ResolveTargets`）→ 见「没查清」 |
| GOF93 Hunta Rig | 235 | `… This troop attacks it` | ✗（`Target2 = it`、`ta=201`） | 保持不变（`ta=200/201` 在 `ResolveTargets` 里另有专门支） |
| SAU33 Vargard Obyron | 236 | `Any attack against your Warlord targets this troop instead`（**不是强制攻击**，是替代效果） | — | 不动 |

**`forceattackfriendly` / `fight`**：本仓**没有**这两个动词/结算函数（`EffectDispatch` 只有 `forceattack` 一条，
`grep "fight\|forceattackfriendly"` 在 `Core/` 零命中），`chosenAbility 233` 池内 **0 张** ⇒ 没有第三处要改。

### 验收（`bash 工具/ruleprobe.sh play EC46`，改后）

```
（按「+attack」挑：FIX_E1、FIXWarlord、FIX_E2、FIX_E3 → FIX_E2）
FIX_A 近战攻击 FIX_E2：2 攻 → 实际 2 伤（FIX_E2 剩 4）
FIX_E2 反击 FIX_A：3 攻 → 实际 3 伤（FIX_A 剩 -1）
战术卡：「Target friendly unit attacks the enemy with highest attack」FIX_A 被强制攻击 FIX_E2
```
改前那一行是 `（按原版 targetCriteria 36 格筛（(ab=0, lg=0)）：4 → 0）` + `一个能去攻击的单位都没有，空过`。

### 顺带订正的两处注释口径（写进代码里了）

1. **`EffectResolver` 不是一个类** —— `Core/EffectResolver.cs` 文件里写的是
   `public static partial class RuleCore`（全仓没有叫 `EffectResolver` 的类型）。全工程文档里的
   `EffectResolver.Xxx` 都是**口头叫法**；`C5` §Q1 的「`EffectResolver.EmitCardTrigger(...)`」照抄会编不过
   （本批真撞到 `CS0103`）。已在 `EmitTalentTrigger` 的 doc 里写明。
2. `AbilityTargetSpec.Trigger` 的 doc 原写「消费端按**下标**对齐」—— A4 之后**按块头对齐**（见 §A4），已就地改掉。

### 没查清 / 还差什么

- ⚠️ **`Target` 为空的那 5 张（EC12/EC22/SAU70/SW9/GOF86）的 criteria 今天被完全忽略**：按原版
  （235/236/238）它该筛**受害者**池。本批**没动**它们，理由两条：
  ① 它们今天在 `play check` 里**零差异**（= 改了看不出来），无法用基线自证；
  ② **`SW9 Fenrisian Wolf` 的 criteria 行可疑**：`{targetsAffected:10, enemyUnits:1, includeMinions:1,
     traitsFilter:1260(huntMark), maxTargets:1}` —— 而卡面是 `Attack a **non Flying** enemy troop.
     If it has a Hunt Mark, gain +1 Attack`。把「huntMark」当**受害者筛子**会打成「只打带猎杀标记的」，
     与卡面相反 ⇒ 先得查清 `(ab=0,lg=0)` 这一行到底属于哪条 `abilityLogic`（是「攻击」那条还是「+1 Attack」那条）。
  ⇒ **建议另立一条账**：先派只读代理核 `SW9` / 其余 4 张的 `abilityLogic[i].targetCriteria` 与卡面句子的对应，
  再决定要不要把 `critOnDefender` 放宽到 `op.Target == null`。

---

## A2 `play` 基线重钉

### 重钉前后

| 项 | 值 |
|---|---|
| 旧 `PinnedPlayMd5` | `c7236f26c4a0cbcd1cef184fe2c4a7c9` |
| 新 `PinnedPlayMd5` | `87209677070c8421b8479e3bcc0effab` |
| 基线文件 | `工具/ruleprobe/基线/out_play_baseline.txt`（**4 行变**，1713 → 1713 行，735,722 B） |
| `play check` 重钉前 | **rc=1** · ①-a **2 张**（AM41 · EC46）· ①-b **3 张**（EC23 · UM13 · UM22）· ② 哨兵 ✓ |
| `play check` 重钉后 | **rc=0** · ①-a **0 张** · ①-b **0 张** · ② 哨兵 ✓ |

### 为什么重钉（逐条归因，⛔ 没有一条是「为了让 check 变绿」）

**A1 只让 1 张卡离开了差异表（EC46）**，而且它是「回到基线那一版」（基线是 W2 接 criteria **之前**录的）
⇒ EC46 那一行**根本没有进这次重钉的 4 行**。

剩下 4 行**不是 A1 引入的** —— 它们是 2026-10-24 `W2`/`W25` 把 v25 的 `abilityTargetCriteria`
（原版 `TargetCriteria` 其余 36 格）接进目标池之后，**探针第一次看得见筛子**造成的正确收敛：

| 行 | 卡面（判据） | 原版字段（`cards_engine.json` 现读） | 旧基线 | 新读数 | 判 |
|---|---|---|---|---|---|
| 主档 `AM41` | `Give Flank to all your **Infantry** troops` | `friendlyUnits=1 · includeMinions=1 · keywordFilter=5` | 给 3 个（含载具/兽） | 只给 `FIX_A` | ✅ 旧值**宽了** |
| 电池 `E|EC23` | `… to **another random friendly troop**` | `includeSelf=0 · maxTargets=1` | 给 3 个（含自己） | 给 1 个别人 | ✅ |
| 电池 `E|UM13` | `… to your **Infantry** troops` | `keywordFilter=5 · includeSelf=1` | 给 3 个 | 只给 `FIX_A` | ✅ |
| 电池 `E|UM22` | `… to your **other** troops` | `includeSelf=0` | 自己那一口也在 | 自己不吃（下游战斗读数连带变） | ✅ |

`keywordFilter=5` = `Keyword.Infantry`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/Keyword.cs:5` 现读）。
重钉的归因也写进了 `PlayProbe.cs` 常量上方的注释块（那里是历史，允许留）。

⚠️ **`ruleprobe play check` 的「差异」与「判据」是两件事**：它只判「结算结果有没有离开被确认的那一版」，
判据仍在卡面上 —— 上面 4 条是**照卡面逐张判过**才重钉的。

---

## A3 `talent`（Trigger 46）回合开始那一拍

### 已做（引擎侧）

| 改动 | 落点 |
|---|---|
| ① **发事件** | `Core/RuleCore.cs` `SpawnTalents`（`:11783` 之后）：闸 = `TalentCardData.Resolve(...).Count > 0 \|\| u.Card.TalentName != null`，**放在两条生成分支之前**（原版 `OnTrigger(46)` 先发、手牌满造不成时动画照样演）；发射体 = `Core/EffectResolver.cs` 新增 `EmitTalentTrigger`（内部 `EmitCardTrigger(..., "Talent", "talent:turnStart")`）|
| ④ **断言** | `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineC5.cs` 新增 `C5Q1TalentTurnStartTrigger()`（挂在已接线的 `TestEngineC5()` 里，**不动 `RuleEngineTest.cs`**）|

**判据**（五跳 → `资料/普查产出_1024c/C5_引擎战斗剩余判据.md` §Q1）：
`CardScript.OnTurnStart.c:160`（`HasCurrentTrait(0x34d)`）→ `:167 OnTrigger(0x2e = 46)` →
`RawCardScript.TriggerAbility.c:60 PlayTriggerTraitAnim` → `BattleAction 0x44` →
`CardScript.ActivateTriggerTraitAnim` → `GetTraitFromAbilityTrigger(46)` = `DefinedTrait.talent(845)` →
`BattleCardUI.DisplayTriggerAnim(845)`（`Talent.asset` = `triggerType 3 / TalentTraitTrigger / TalentEffect`）。

**闸的三口径现读完全相等（96 / 96 / 96）**：`cardTraits` 含 `talent` = 96 · `abilityTriggers` 含 46 = 96 ·
`desc` 有 `Talent:` = 96 · `talent_cards.json` 的 `entries` = 96 —— 三个集合逐 id 相同（本轮脚本现算）。

### 断言内容（4 组，全部带 `🧨`）

1. **契约前提**：`AbilityTriggerNumber("Talent") == 46` · `AbilityTriggerNumber("regeneration") == -1`。
2. **灭自证（第一半）**：`SOR9 Preacher`（`abilityTriggers=[46,605]`，`animTimes` 里**没有** `Talent` 行）上
   `HasTriggerSourceForTest(…, "Talent") == true` 而 `HasAnimRowOnlyForTest(…) == false`
   ⇒ 结构上排除「有动画行才发」那一版实现。
3. **行为**：同一局把 `SOR9`（带天赋）与 `DA25`（`abilityTriggers=[690]`，无天赋）都摆上 P0 棋盘，
   推到 P0 的下一个回合开始 ⇒ `SOR9` **恰好 1 条** `EvtKind.Trigger("Talent")`、`DA25` **0 条**
   （按 `Event.InstanceId` 归属，⛔ 不按卡名）。
4. **次序**：手牌塞满 10 张 ⇒ 天赋卡造不成，**事件照样发 1 条** ⇒「把 `EmitTalentTrigger` 挪到造卡成功之后」必红。

### 未做（②③）—— 都在黑名单里

- **② `VfxMap` 放行 `talent`**：`CardPresentation/Core/VfxMap.cs`（`IsRoutedTriggerName` / `MustRouteTriggerNames`）
  = **`Core/**` 黑名单** ⇒ 没碰。今天引擎已经发 `EvtKind.Trigger("Talent")`，表现层仍只能掉进 `trigger` 粗桶
  （`IsNonTriggerBeatTraitRow` 对 `TriggerType 3/6` 放行 ⇒ 粗桶会播）。
  **交接**：把 `"talent"` 加进 `VfxMap.IsRoutedTriggerName` + `MustRouteTriggerNames` 即可（`TraitFrames.DisplayTrigger`
  按 trait 名小写查表、key 就是 `"talent"`，**不用改 `TraitFrames`**）。
- **③ `regeneration` 不可达清单**：建议落点 `CardPresentation/Core/TraitFrames.cs`（黑名单）或
  `工具/gen_trait_effect_bindings.py`（不在白名单）。**本批把它写进了断言文件的 doc**（`RuleEngineTest_EngineC5.cs` 的
  `C5Q1TalentTurnStartTrigger` 注释）并配了 `AbilityTriggerNumber("regeneration") == -1` 这条机器断言 —— 记账落地，
  但**没有**落在 `TraitFrames` 那一侧。
  🔴 **就地订正（铁律 5）**：`C5` §Q1 建议里那句「同样没有 AbilityTrigger 的 … **`resolution`** …」**不成立** ——
  `AbilityTrigger.Resolution = 45` 存在（`AbilityTrigger.cs:12` 现读），它走的是 name-parse 那条路。
  本轮实测「没有 AbilityTrigger」的只有：`regeneration` · `concussive` · `frenzied` · `huntMark` · `large` ·
  `mekaniak` · `stomp`（`bloodThirst` 有直接调用点、**不在**这一组）。

### 顺手发现（A3 带出来的）

📌 **`HasTriggerSource` 会给每张天赋卡打一条一次性「有 ability、没动画行」的 warning（全池 96 条 / 进程）**。
它**技术上没错**（`animTimes` 里确实没有 `Talent` 行 —— trait 框走 `trait_effect_bindings.json`），
但文案「表现层这一拍可能没有东西可播（数据缺口）」对 trait 框是**误导**。
建议：给 `NoteAbilityWithoutAnimRow` 加一个「trait 框族」白名单（`Talent`/`Codex`/`Slay`… 那些靠 `trait_effect_bindings` 播的），
或把文案改成「`animTimes` 里没有，可能由 trait 框承担」。⛔ 别直接删这条日志（那是红线上的静默）。

---

## A4 督军 `abilityTargets` 的序列化序 ≠ 卡面文字序

### 结论

`EffectText.AssignAbilityTargets` 的**整条 desc 分支**不再假定「第 b 块 = 第 b 条 ability」，
改为新增的 `AlignAbilityIndexByBlockHead`：**块头词 → `AbilityTrigger` 数值 → 在 `abil` 里找 Trigger 相等的那一条**。

- 块头词来源 = `Parse` 的 `SegResult.HeadKeyword`（`segLeadKeyword[blockStartSeg[b]]`，与
  `IsAlternativeActionBlockHead` 同一份数据）；
- 数值表 = `CardDef.AbilityTriggerNumber`（**唯一一份**，`Talent=46 / Pray=655 / Duty=662 / Ferocity=725 / Agenda=705 / Oath=730 …`）；
- **三种情况一律退回下标 `b`（⛔ 不猜）**：块头拿不到（主正文块）· 词认不出 · `abil` 里没有/不止一条这个 trigger；
- 对齐结果 ≠ `b` 时**出声一次**（`CardTargetNotice`，同一 `(卡, b)` 只喊一次）。

`AttachTargetCriteria(card, blk[j], ab, 0)` 也跟着用**对齐后**的下标（否则 criteria 会按旧下标取错行）。

### 数据级反例（本批现读复核过）

`SOR65 Celestian Sacresant Aveline`：`desc` = `Pray: … . Talent: Daemonbreaker`，
而 `abilityTargets = [{trigger:46 Talent, ta:[0]}, {trigger:655 Pray, ta:[20]}]`（`cards_engine.json` 现读）。

改前：块 0 = `Pray`（被 `IsAlternativeActionBlockHead` 跳过）、块 1 = `Talent` 拿走 `abil[1]`（**Pray 的 ta=20**）。
改后：块 1 → trigger 46 → `abil[0]` ⇒ `create` op 的 `subjTa = 0`、`fm = 0`（`ruleprobe card "Celestian Sacresant Aveline"` 现读）。
同族还有 `SOR1/SOR2/SOR3`（Pray+Talent）与 `DA3/DA75`（Agenda+Talent）。

### 验收

- `bash 工具/ruleprobe.sh check` → **解析差异 0 行 · rc=0**（A4 没有把任何卡改坏）。
- `bash 工具/ruleprobe.sh play check` → **rc=0**（结算层零变化）。
- 新断言 `E6T4AbilityBlockHeadAlignment()`（挂在已接线的 `TestEngineE6()`，文件
  `CardPresentation/Editor/RuleEngineTest_EngineE6.cs`）：4 条前提 + 2 条判别式
  （`create.SubjectTargetsAffected == 0`，⛔ 退化成 `20` 就红；对照组 `give.SubjectTargetsAffected == -1`）。

### 🔴 顺手发现（重要 · **仪器缺口**）

**`ruleprobe` 的 `scan` / `check` 基线里根本没有 `ext=` 块** —— `Program.Ext()`（`subjTa` / `fm` / `dest` / `subj`…）
只被 `card` / `seg` 两个入口调用，`Scan.cs` 的行渲染**不调它**（现读 `Scan.cs` 全文无 `Ext(`）。
⇒ **`SubjectTargetsAffected` 在解析基线上是「零覆盖」**，A4 这种改动在 `check` 里天然看不见
（本批是「0 差异」而不是「验过了」）。同族的 `A12` 在 `Program.cs:1532` 写着「补上 subjTa，否则归位有没有生效
探针里没有任何仪器可看」—— **那句话只对 `card`/`seg` 成立，对 `check` 不成立**，建议就地订正。
⇒ 本批因此**只能靠单元断言**守住 A4（已加）。

---

## A5 `T03`：`B6` 夹具 `ResolveAll` vs 生产 `ResolveAllTimed`

### ⛔ 未做 —— 被白名单挡住（不是「做不了」）

- **现场**：`MyGame/Assets/CardPresentation/Editor/BattleScene.cs:38580-38697`（B6 段，断言前缀 `★ B6·`）——
  夹具用 `VfxMap.ResolveAll(evt, faction, name)`（`:38580`）拿**候选名表**，再逐件比对 `ResolveAllTimed` 的 `T`/`Duration`。
  问题 = `ResolveAll` **不是生产口**（生产一律 `ResolveAllTimed`，见 `BattleDriver.cs:19656/19679/20079/21355`、
  `CardAnimClips.cs:254/294`）⇒ 候选表的来源与生产不是同一条路，断言可能「因为错的原因」成立。
- **为什么没做**：`CardPresentation/Editor/BattleScene.cs` 属于简报里的 **「其它 `Editor/*.cs`」= 黑名单**；
  我的白名单只放了 `CardPresentation/Editor/RuleEngineTest*.cs`。按红线⑦停手，没有越界改。
- **交接（给下一位有 `BattleScene.cs` 权限的执行代理）**：
  1. 夹具的**候选表**改由 `ResolveAllTimed(evt, faction, name, card.AnimTimes)` 自己给（`b6timed[i].Name`），
     **不要**再调 `ResolveAll`；`ResolveAll` 只保留在「带时间版与旧口的名字/顺序逐位相同」那条回归断言里。
  2. 补**前提成立**断言（防「两边一起改回去」的自证）：断言 `ResolveAllTimed(...).Length >= 2` 且
     **至少两条 `T` 不同**（即 `anyGap` 由 `b6timed` 自己算出，而不是由 `ResolveAll` 那一路的 `exp[]` 算出）；
     再加一条「`ResolveAllTimed` 的候选取自逐卡桶而不是 `ByEvent` 兜底」判别式（例如传 `cardIdOrName=null` 时结果不同）。
  3. ⚠️ 该断言宿主 = `BattleScene.Run`（跑一次约 8 分钟）⇒ **要攒批**（铁律 12）。

---

## 改动清单（文件 / 行数 / 行尾）

`git diff --numstat`（只列我碰的；仓里还有别的代理在飞的文件）：

```
53   0    Unity/MyGame/Assets/CardPresentation/Editor/RuleEngineTest_EngineE6.cs   (LF)
82   5    Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs                     (CRLF 13841/13841)
64   2    Unity/MyGame/Assets/RuleEngine/Core/EffectText.cs                         (LF)
21   0    Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs                           (LF)
173  0    Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineC5.cs          (LF)
19   1    Unity/工具/ruleprobe/PlayProbe.cs                                        (LF)
4    4    Unity/工具/ruleprobe/基线/out_play_baseline.txt                            (LF, 1713 数据行)
```

| 文件 | 改了什么 |
|---|---|
| `Core/EffectResolver.cs` | ① `DoForceAttack` 新增 `critOnDefender` 判据 + 攻击者池不吃 criteria + `DefenderPool(..., withCriteria)` 让受害者池**在裁剪之前**过 criteria（判据散文写在函数 ① 上方）② 新增 `EmitTalentTrigger` |
| `Core/RuleCore.cs` | `SpawnTalents`：判出「这张卡有天赋」之后、两条生成分支之前发 `Talent` 事件 |
| `Core/EffectText.cs` | 新增 `AlignAbilityIndexByBlockHead` + 整条 desc 分支改用它（`AttachTargetCriteria` 同步用对齐后的 `ab`） |
| `Core/Editor/RuleEngineTest_EngineC5.cs` | 新增 `C5Q1TalentTurnStartTrigger()`（A3，4 组断言）· `C5Q4bForceAttackCriteriaSide()`（A1，真卡 `EC46` 的判别式） |
| `CardPresentation/Editor/RuleEngineTest_EngineE6.cs` | 新增 `E6T4AbilityBlockHeadAlignment()`（A4，`SOR65`） |
| `工具/ruleprobe/PlayProbe.cs` | `PinnedPlayMd5` 重钉 + 归因注释块（A2） |
| `工具/ruleprobe/基线/out_play_baseline.txt` | 4 行（`AM41` / `E|EC23` / `E|UM13` / `E|UM22`），其余逐字节不变 |

---

## 顺利发现的其它条目（⛔ 只报不改）

1. 🔴 **`W25` §六·1 的处方不能当通则** —— 见 §A1 结论（230/231/233 的 criteria 归**攻击者**）。
   已在代码注释里写清，台账那一条若还写着「一律挪到受害者池」，建议就地订正。
2. 🔴 **`C5` §Q1 的 `regeneration` 不可达清单把 `resolution` 也列进去了** —— `AbilityTrigger.Resolution = 45` 存在。见 §A3。
3. 🔴 **`EffectResolver` 这个类不存在**（`Core/EffectResolver.cs` 里是 `partial class RuleCore`）⇒ 所有文档里的
   `EffectResolver.Xxx` 都是口头叫法，照抄会 `CS0103`。
4. 🔴 **`ruleprobe` 的 `check`/`scan` 不覆盖 `ext=` 那一整块**（`subjTa`/`fm`/`dest`…）⇒ `A12`/`B-17`/A4 这一族
   在解析基线上**零覆盖**。见 §A4。
5. 🟡 **`EmitTalentTrigger` 会让 96 张天赋卡各打一条「有 ability、没动画行」的 warning**（每进程一次）——
   技术上真、文案误导（trait 框走另一张表）。见 §A3。
6. 🟡 **`TalentCardData.SetForTest` 是共享静态表**：现有天赋用例（`WTalHost`）会把「WTalHost → 一条候选」写进表里，
   本批的 `C5Q1` 用例不依赖那张表（用 `TalentName` 那半条闸），所以互不干扰；但**将来加天赋用例要注意表的顺序依赖**。
7. ℹ️ 验证期间 `Editor/BattleScene.cs` 反复出现不属于我的编译错（`PendingHpBySlotCountForTest` / `TickForTest` /
   `PlayedCenterDisplaysForSeatForTest` / `DateTime`）—— 那是别的代理正在写的文件（按铁律 13·3 只报不改），
   本批最后一次 typecheck 是 **0 / 0**。

---

## 没查清 / 还需主对话做的事

| # | 事项 | 谁做 | 判据 |
|---|---|---|---|
| 1 | **跑 `RuleEngineTest.Run`** 验本批新增断言（`C5Q1TalentTurnStartTrigger` · `C5Q4bForceAttackCriteriaSide` · `E6T4AbilityBlockHeadAlignment`；都挂在已接线的入口方法里，**没有**新增 `Step`） | 主对话（Unity） | 本报告 §A1/§A3/§A4 |
| 2 | `VfxMap` 放行 `talent`（`IsRoutedTriggerName` + `MustRouteTriggerNames`） | 有 `CardPresentation/Core/` 权限的代理 | `C5` §Q1 建议 2 |
| 3 | `regeneration` 不可达清单落到 `TraitFrames` 侧 | 同上 | `C5` §Q1 建议 3（名单已在本报告订正） |
| 4 | **`B6` 夹具改走 `ResolveAllTimed`**（A5） | 有 `Editor/BattleScene.cs` 权限的代理 | 本报告 §A5 的三步交接 |
| 5 | `SW9` / `EC12` / `EC22` / `SAU70` / `GOF86` 的 criteria 该不该筛受害者池（**先核 `(ab,lg)` 那一行属于哪条 logic**） | 只读代理 | 本报告 §A1「没查清」 |
| 6 | 台账订正：`W25` §六·1（criteria 归侧按 `chosenAbility` 分族）· `C5` §Q1（`resolution`）· `A12`（`check` 不覆盖 `ext`） | 主对话（正本只有它能写） | 本报告 §顺手发现 1/2/4 |
