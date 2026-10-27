# B9_W54ENG2 · 掉舱震屏的**发端点** ＋ `BattleEvent` **带目标列表**

> 2026-10-27 · 执行代理 `B9_W54ENG2` · 白名单：`RuleEngine/Core/BattleEvent.cs` · `Core/EffectResolver.cs` · `Core/RuleCore.cs`
> 判据：`资料/完全复刻/报告/B9_W33FX3_震屏与进攻卡.md` §四·A（转派单 ①~⑤）· `资料/完全复刻/报告/B9_W10BDR_施工单8-9-12.md` §#9（`PlayAnim` 的结构性缺口）

---

## 一、结论

| # | 件 | 结论 | 等级变化 | 改动 |
|---|---|---|---|---|
| ① | **掉舱震屏的发端点** | **整链已通**。`EffectResolver.EmitTraitRemoved` 加了可选形参 `effect`，`RuleCore` 三处按原版判据发 `"droppod-open"`（0.5）/ `"droppod-destroyed"`（3.0）。消费端（`BattleDriver.OnTraitRemovedShake`，`W40` 接的）**一个字没动**。 | 账 `特效动画:130` 与 `战斗逻辑驱动:851` 都 **C → B**（不再缺接线；剩两条真缺口，见 §三） | ✅ 改 |
| ② | **`BattleEvent` 带目标列表** | `BattleEvent.AbilityTargetList`（`List<BattleEventTarget>`，四元组 Player/Slot/CardId/InstanceId）＋ 两处 `EvtKind.Ability` 发点填上；`TargetPlayer/TargetSlot/TargetCardId` 三位**原样保留**。**实现形状严格照原版**：列表长度 = 引擎**实际解出的目标数**（判据只一份，转调 `ResolveTarget` —— 与 `ResolveEffect` 结算用的是同一个方法）。 ⚠️ **今天长度只有 0 / 1**：封闭文法（`EffectSpec`/`EffectTargets`）每条能力至多一个目标 ⇒ 这一层解锁的是**结构**，`ExpandAbilityBeats` 改成逐目标播放是**下一个写手**的事（§六）。 | 账 `特效动画:484`（`AbilityLogic.PlayAnim`）仍 **B**，但「唯一结构性缺口」那半已消 | ✅ 改（引擎侧） |

**必跑三项全过**（详见 §二·3）：

1. `TMPDIR=/tmp/wf_w54 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
2. `bash 工具/ruleprobe.sh check` ⇒ **rc=0**，`✓ [输出指纹] … c9a64798…`（**与改前逐字相同** ⇒ 解析输出没动）· 哨兵失败 0 · 解析差异 0 行。
3. `bash 工具/ruleprobe.sh play check` ⇒ **rc=1，仍然只有 `BL64` + `DA42` 两张**（别人的），**结算指纹 `e7f1de8f96f9e7a255ae72534cf263de` 与改前完全相同**，事件电池 586 张逐字节一致 ⇒ **没引入第三张**。

**仓库外夹具**（`/tmp/wf_w54fx`，dotnet 直编 `Core/*.cs`，⛔ 未进仓库）：**9 组断言全绿**（§二·1 / §二·2 的读数都是它跑出来的）。

---

## 二、证据（文件:行号）

### 2·1 ① 掉舱震屏 —— 判据 → 我们

| 项 | 出处 | 读数 |
|---|---|---|
| 原版方法体 | `decomp_full/CardScript__RemoveDropPod.c:9-18` | `RemoveBuffedTrait(0xe6)` → `RemoveTrait(0xe6)` → `strength = param_2 != 0 ? DAT_1834b2e8c : DAT_1834b2bb4` → `CameraShakerManager.DoSimpleShake(strength)` → `BattleCardUI.RemoveStatusAnim(0xe6, param_2)` → `UpdateFigures` |
| 两个常数 | `工具/read_literal.py`（`W33FX3` 复读） | `DAT_1834b2bb4` = 4B float **0.5** · `DAT_1834b2e8c` = 4B float **3.0** |
| 原版**四个**调用点 | `OnTurnStart.c:107`（回合开始开舱）· `BattleManager._ResolveAttack_d__438__MoveNext.c:359`（出手开舱）· `CardScript__RemoveEffect.c:75`（撤一条授予的 effect）—— 三处 `param_2 = 0`（0.5）；`CardScript__CheckIfDead.c:94`（池 < 1）`param_2 = 1`（**3.0**） | **只有「池被打空」那一支是 3.0** |
| 我们的发射口 | `RuleEngine/Core/EffectResolver.cs:7889`（`EmitTraitRemoved(…, string effect = null)`）· `:7895`（`ctx.Emit(…, effect: effect)`）· `BattleEvent.Effect` 在 `BattleEvent.cs:597`（早就有） | 透传一格，⛔ 没新开 `EvtKind` |
| 发点 ①（开舱） | `RuleEngine/Core/RuleCore.cs:7888`（`OpenDropPod` 里 `RemoveAll` 之后） | `EmitTraitRemoved(ctx, u, DropPod, left, "droppod-open")` —— 对应原版 `OnTurnStart.c:107` / `_ResolveAttack…:359` 那两处 `param_2 = 0` |
| 发点 ②（主伤害把池打空） | `RuleCore.cs:8616`（`if (after < 1)` 那一支，加了花括号）→ `:8632` | `… "droppod-destroyed"` —— 对应 `CheckIfDead.c:94` 的 `RemoveDropPod(card, 1)` |
| 发点 ③（易伤第二发把池打空） | `RuleCore.cs:8683`（`after2` 的 `RemoveAll` 之后，**判 `after2 < 1`**） | 同上 |
| 🔴 为什么 ③ 要判 `after2 < 1` | `CardScript__CheckIfDead.c:76-95`：`RemoveDropPod(card, 1)` 只在**池 < 1** 时调；池还活着时原版那次「摘 + 重加」**不是摘 trait** | 无条件发会**多震一下**（夹具 ①-e 就是这条的鉴别器，见下） |
| 转派单原文写的范围 | `B9_W33FX3` §四·A 第 4 条：「`after2` 的 `RemoveAll` 之后 → 同样补」 | 本代理按**判据收窄**成 `after2 < 1`（不是推翻转派单，是把「那一支」按原版闸收窄 —— 转派单里那句「易伤第二发**把池打空**那一支」本来就是条件式） |
| 消费端（**未动**） | `CardPresentation/Battle/BattleDriver.cs:22830`（`else if (e.Kind == EvtKind.TraitRemoved) OnTraitRemovedShake(e)`）→ `:35526`（`e.CardId == KeywordTable.DropPod` ⇒ `DoSimpleShake(e.Effect == "droppod-destroyed" ? 3f : 0.5f)`） | 读的是 **`e.CardId`**（引擎把关键词塞那儿），⛔ 不是 `e.Keyword` —— 与本实现一致 |
| 另两个既有调用点**未受影响** | `EffectResolver.cs:12015`（`lose <trait>`）· `RuleCore.cs:2377`（`markerlight`） | `effect` 是**可选**形参（缺省 `null` = 没写）⇒ 那两处字节不变 |

**夹具读数（`/tmp/wf_w54fx`，9 条断言全绿）**：

| 档 | 造法 | 事件读数 |
|---|---|---|
| ①-a 开舱 | 反射调 `RuleCore.OpenDropPod(ctx,0,3,u,false)`，池 5 | 恰 1 条 `TraitRemoved{CardId=droppod, Effect=droppod-open, Amount=5, P=0@3}` ✔ |
| ①-b 主伤害打空 | 池 2，`ApplyDamage(u, 5)` | 恰 1 条 `{Effect=droppod-destroyed, Amount=2}`；`u.Health` **一点没掉** ✔ |
| ①-c 池没打空 | 池 9，`ApplyDamage(u, 5)` | **0 条**（⛔ 不多震一下）✔ |
| ①-d 易伤第二发打空 | 池 3 ＋ `vulnerable 5`，`ApplyDamage(u, 1)` | 恰 1 条 `{Effect=droppod-destroyed, Amount=2}`（= 第二发前的池量）✔ |
| ①-e 易伤第二发之后池仍活着 | 池 9 ＋ `vulnerable 2`，`ApplyDamage(u, 1)` | **0 条**（池 9−1−2 = 6）✔ |

### 2·2 ② `EvtKind.Ability` 的目标列表

| 项 | 出处 | 读数 |
|---|---|---|
| 原版形状 | `decomp_full/AbilityLogic__PlayAbility.c:262` | `abilityTargets = GetTargets(...)` —— 一个 **`List<CardScript>`**（**不是**单个目标） |
| 消费侧怎么用它 | `decomp_full/AbilityLogic__PlayAnim.c:194 / :203 / :215`（**逐目标**各解一次位置）· `:217-222`（**只有最后一个目标**带 `waitAnimToApplyEffect`） | 这就是「每拍 × 全部目标」的拍序来源 |
| 我们原来有什么 | `BattleEvent.cs:496-502` 只有 `TargetPlayer / TargetSlot / TargetCardId` **一个**三位组 | 缺口原文 → `B9_W10BDR` §#9 |
| 新字段 | `BattleEvent.cs:763`（`public List<BattleEventTarget> AbilityTargetList;`）· `:775`（`public struct BattleEventTarget { Player; Slot; CardId; InstanceId; }`） | 四元组 = 原版那个 `CardScript` 引用的**等价身份**（`Slot` 不是身份、`InstanceId` 才是 —— 同 `BattleEvent.InstanceId` 的 doc） |
| 判据只一份 | `RuleCore.cs:15134`（`AbilityTargetsFor`）**转调** `RuleCore.ResolveTarget`（`:13668`，`ResolveEffect` 结算时用的**同一个**方法） | ⇒「事件里记的目标」与「真结算到的目标」**结构上不可能不一致**（没写第二套目标判定） |
| 挂载口 | `RuleCore.cs:15109`（`AttachAbilityTargets`）· 调用点 `:11595`（`UseAbilityCore`）· `:12024`（`UseAlternativeCore`） | 两处 `ctx.Emit(EvtKind.Ability, …)` **逐字未动**，只在下一行挂列表（理由见 §五·1） |
| `Target*` 三位保留 | 同上两处 emit 原样 | 它们 = **「玩家点没点目标」**的判别式（原版 `CemeteryManager.GetActionText` case `0xF` 读 `+0x28` 是否 null）；本列表 = **「GetTargets 自己选出来的实际目标」**。两件事，⛔ 别合并 |
| 第三处 `EvtKind.Ability`（誓约）**没填** | `RuleCore.cs:11756`（`UseOathAbilityCore`） | 原版的誓约也走 `ResolveActiveAbilityPlayed` ⇒ 理论上也有 `abilityTargets`；我们这一支**今天是「没有目标」**（`ResolveOathAbility` 走 `EffectOp` 表、不经过 `EffectSpec`）⇒ 保持 `null` = 没写，**如实标着**（§三·6） |

**夹具读数（4 档全绿）**：

| 档 | 卡（关键字写 `Ability: …`） | `Target*` | `AbilityTargetList` |
|---|---|---|---|
| ②-a | `Damage 2 EnemyUnit` | `P1@3 / FIX_FOE` | **1 条** `[P1@3 FIX_FOE inst=27]` |
| ②-b | `Heal 2 Self` | `-1@-1 / null` | **1 条** `[P0@3 FIX_AB inst=13]` ← 🎯 **正是「玩家没点 ≠ 没目标」** |
| ②-c | `Heal 2 OwnWarlord` | `-1@-1 / null` | **1 条** `[P0@4 督军 inst=14]`（`WarlordSlot`） |
| ②-d | `Draw 1` | `-1@-1 / null` | **0 条** |

每档都另验了「列表[0] 的 `InstanceId` 指得回场上那一份」。

### 2·3 必跑三项的原始读数

- `typecheck`：`运行时错误数: 0 / 编辑器错误数: 0`（跑了两遍；中途有一次红在 `Assets/WarpforgeArena1/Editor/ArenaBuilder.cs:5262/5270` 的 `TextureImporter.spriteMeshType`，**不是我改的文件**，隔几十秒复跑即 0/0 —— 那是别人在飞的手）。
- `ruleprobe check`：`=== 全池 1121 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 ===` · `哨兵合计：失败 0 条` · `✓ [输出指纹] … c9a64798…` · `结束（rc=0）`。
- `ruleprobe play check`：`🔴 2 张卡变了 · 新增 0 · 消失 0`（`BL64` / `DA42`，逐列与改前**逐字相同**）· `①-b 事件电池 ✅ 没有一张卡与基线不同（586 张逐字节一致）` · `结算指纹 当次现算 = e7f1de8f…`（**与改前同值**）· `结束（rc=1）`。

---

## 三、没查清的部分

1. **原版第 4 个调用点 `CardScript__RemoveEffect.c:75`（`param_2 = 0` ⇒ 0.5 震屏）我们没有等价路径。**
   原版：撤掉一条「授予 `dropPod`」的 effect 之后，若身上已经没有 `dropPod`，就 `RemoveDropPod(card, 0)`。
   我们：没有「按记录撤一条 effect ⇒ 顺手摘 `dropPod`」这条链（同一件事记在 `战斗逻辑驱动.tsv` 的 `CardScript.RemoveEffect` 行 case 3 特判里，`B9_R2T3B`/`E10TB` 那两轮已记）。
   ⇒ **这条路上不会震**。要补得先建那条链（属 `RemoveEffect` 那行，不属本行）。
2. **`RemoveDropPod` 体内另两步没核**：`BattleCardUI.RemoveStatusAnim(0xe6, isDestroyed)`（摘状态框）与 `CardScript.UpdateFigures`（刷数值）。
   `EvtKind.TraitRemoved` 的消费端**只做震屏** ⇒ 那两步在我们这边**有没有对应物**（`SyncKeywords` 的「词条消失」路？）**没查**。⇒ 这是两行账留在 B 而不是 A 的第二个理由。
3. **原版 `GetTargets` 对「无目标能力」（`Draw` 那一类）返回什么 —— 没查。**
   我们的口径是**空表**（`spec.Target` 为空 ⇒ 不解析目标）。若原版对某些「自身结算」类能力也塞一条（比如施法者自己），那我们的 0 会偏小。今天这一档**没有任何卡走得通**（封闭文法里 `Draw` 是唯一无目标动词）。
4. **「`AbilityTargetList` 长度 = 原版 `GetTargets` 长度」在多目标档【无法证明】。**
   池内没有任何多目标能力（`Ability:` 这一档全池 **0 张**，见 `EffectTargets.FriendlyUnit` 的 doc）⇒ 夹具只能证明「长度 = 引擎实际解出的目标数（0 / 1）」。**多目标档要等文法扩档**，那时这条验收才有真样本（届时改的只有 `AbilityTargetsFor` 一处）。
5. **`PlayAnim.c:62-64` 那个虚谓词**（每拍先问一次、假就跳过）—— 沿 `B9_W10BDR` §三·1，仍未解（`AssetReferenceTyped<T>` 的 vtable 槽 `+0x248`）。与本件无关，但多目标档真跑起来时它会决定「拍要不要播」。
6. **誓约那一支（`RuleCore.cs:11756`）今天不填列表** —— 原版誓约也走 `ResolveActiveAbilityPlayed`，理论上也有 `abilityTargets`。我们这一支的正文是 `EffectOp` 表（不经 `EffectSpec`）⇒ 「它的目标在哪」是另一个面（`EffectText` 的目标模型，`Count`/`Subjectless` 那一套），**没查**。**没有把它做成「拿 `chosen` 硬填一条」**（那会是编目标）。
7. **`TargetPlayer` 的语义边界**：`ctx.AbilityTargetsSaved` 的 doc 说原版 `abilityTargetsToSave`（`+0x80`）**能装手牌/牌库的卡**；本字段对应的是 `abilityTargets`（`PlayAbility` 的**局部**结果）。局部那份**是否也只装场上牌**没核 —— 我们只装场上牌（`FindSlot` 认不出 ⇒ 空表、⛔ 不猜）。

---

## 四、改动清单

| 文件 | 位置（**改后**行号） | 改了什么 | 行数变化 |
|---|---|---|---|
| `RuleEngine/Core/BattleEvent.cs` | `:736`–`:763`（**类尾追加**） | 新字段 `AbilityTargetList`（doc 27 行 + 字段 1 行）：判据 / 与 `Target*` 三位组的分工 / 与 `CardDef.AbilityTargets`、`ctx.AbilityTargetsSaved` 的区分 | +28 |
| 同上 | `:766`–`:781`（namespace 闭括号之前） | 新类型 `public struct BattleEventTarget`（Player/Slot/CardId/InstanceId） | +16 |
| （两处之间各 1 个空行 ⇒ 本文件合计 **+46**，**全部在 `:735` 之后**） | | | |
| `RuleEngine/Core/EffectResolver.cs` | `:7886`（doc 同一行内追加）· `:7889`（签名）· `:7895`（透传） | `EmitTraitRemoved(…, string effect = null)`；`effect` 透传给 `ctx.Emit` | **0**（三处都是**行内**改写，⛔ 没加行） |
| `RuleEngine/Core/RuleCore.cs` | `:7884`–`:7888`（`OpenDropPod`） | 开舱发 `"droppod-open"` | +5 |
| 同上 | `:8616`（`{`）· `:8630`–`:8633`（注释 + emit + `}`） | 主伤害打空发 `"droppod-destroyed"` | +1 / +4 |
| 同上 | `:8678`–`:8683` | 易伤第二发打空（`after2 < 1`）发 `"droppod-destroyed"` | +6 |
| 同上 | `:11594`–`:11595` | 挂 `AbilityTargetList`（`UseAbilityCore`） | +2 |
| 同上 | `:12022`–`:12024` | 挂 `AbilityTargetList`（`UseAlternativeCore`） | +3 |
| 同上 | `:15085`–`:15149`（**类尾追加**） | `AttachAbilityTargets`（`:15109`）＋ `AbilityTargetsFor`（`:15134`）＋ 两段判据 doc | **+65** |
| `账/_parts/B9_W54ENG2.tsv` | 新建 | 3 行（11 列 / 带表头 / `方法` 都是完整签名）：`特效动画:130` `DoSimpleShake` C→**B** · `战斗逻辑驱动:851` `RemoveDropPod` C→**B** · `特效动画:484` `PlayAnim` 差异列订正（缺口从「引擎侧」移到「表现层」） | — |

**行尾 / 裸 CR 现数（改完）**：`BattleEvent.cs` LF 823 / CRLF 0 / 裸 CR 0 · `RuleCore.cs` LF 15150 / CRLF 0 / 裸 CR 0 · `EffectResolver.cs` CRLF 16808 = LF 16808 / 裸 CR 0（**原样**）。

### 🔴 行号漂移表（给调度台在重同步点跑 `check_ledger_points --all-A --fix`）

`RuleCore.cs`：**15064 → 15150（+86）**。按**改前**行号区间：

| 改前区间 | 漂移 |
|---|---|
| **≤ 7883** | **不变** |
| 7884 – 8611 | **+5** |
| 8612 – 8623 | **+6** |
| 8624 – 8667 | **+10** |
| 8668 – 11577 | **+16** |
| 11578 – 12003 | **+18** |
| **≥ 12004** | **+21** |
| （其中 ≥ 15065 落在新追加的类尾块之前，`AbilityCostGate` 之后的 65 行是新增） | — |

`BattleEvent.cs`：**777 → 823（+46）**，**全部在 735 之后** —— 账里引用本文件的最大行号是 **443**（`BattleEvent.cs:443` / `:434` / `:396` / `:182` / `:177` / `:175`）⇒ **没有任何落点漂移**。
`EffectResolver.cs`：**行数不变（16808）** ⇒ **零漂移**。

---

## 五、顺手发现（⛔ 我没动，交调度台分流）

1. 🔴 **`RuleEngineTest.cs` 有一对「按 `EvtKind.Ability` 字面量出现次数」的结构断言，改发点形状就会把它们顶破。**
   `Editor/RuleEngineTest.cs:33120`（`CountOf(two, "EvtKind.Ability") == 3`，`two` = `RuleCore.cs` + `EffectResolver.cs`，**注释已剥**）
   与 `:33141`（全 `Core/` 合计 `== 4`）。
   我第一版把两处 `ctx.Emit(EvtKind.Ability, …)` 改写成走一个公共 helper ⇒ `RuleCore.cs` 里的代码出现数 **3 → 2**、全目录 **4 → 3** ⇒ **两条断言当场红**。
   ⇒ **最终实现改成「保持那两行逐字不变、下一行挂列表」**，代码出现数仍是 **3 / 4**，两条断言**不需要动**。
   📌 **给后来人**：`Core/` 里**新增任何 `EvtKind.Ability` 代码出现点**（哪怕只是 `Kind = EvtKind.Ability` 或 `e.Kind != EvtKind.Ability` 这种比较）都会撞这两条 ⇒ 一改就同步改那两条期望值并留痕。
2. **`RemoveDropPod` 在我们这边可能少了两步**：原版先 `RemoveBuffedTrait(0xe6)` 再 `RemoveTrait(0xe6)`，我们是一次 `u.RemoveAll(KeywordTable.DropPod)`。对**可叠的数值 trait** 两者净效果一致（都是整条摘光，见 `EffectResolver.cs:11984-11992` 那段已做的同类论证），但**「buffed 层」那一侧会不会另有广播**，本代理**没核**。
3. **原版 `CameraShakerManager` 的主菜单那一份是另一个 preset**（`W33FX3` 已记）—— 与本件无关，但 `DoSimpleShake` 若哪天被菜单侧调用，会拿到不同参数。
4. **夹具踩到的一个坑（值得记）**：`CardDef` 的 `Ability: …` 解析走 **`keywords` 参数**（`CardDef` 的 `foreach (var item in keywords)` → `KeywordTable.EffectText(item)`），**⛔ 不是 `desc`** —— 我第一版把 `Ability: Damage 2 EnemyUnit` 写进 `desc`，`u.Card.Ability` 恒 `null`、`UseAbility` 回 `ErrNoAbility(14)`，而**没有任何报错说「你的夹具写错了」**。同族：`EffectTargets` / `EffectSpec` 那套封闭文法只有合成夹具走得到（池内 0 张卡用它）。
5. **`BattleEvent` 的「同一条规则的两份表示」现在有三份，doc 里必须互相点名**（已写）：`CardDef.AbilityTargets`（卡表的 `abilityTargets` **判据行**）· `ctx.AbilityTargetsSaved`（一次 play 里**所有 op** 目标的**并集** = 原版 `abilityTargetsToSave`）· **`BattleEvent.AbilityTargetList`**（本条能力 `GetTargets` 的结果）。后来人**⛔ 别把三者混成一个**。

---

## 六、消费端该怎么改（给下一个表现层写手）

> 🔴 **本件只做「事件能带多目标」这一层，表现层一个字没动。** 下面是接线清单，⛔ 别自己按 criteria 重算目标（那是第二份判定）。

1. **入口**：`CardPresentation/Battle/BattleDriver.cs` 的 `ExpandAbilityBeats`（`:19564`）+ `PlayAbilityBeat`（`:23282`）。
2. **拍序要反过来**：现在是「外层按拍、内层无目标循环」⇒ 要改成**外层按拍、内层按 `e.AbilityTargetList` 逐目标各排一条 `PendingSignal`**。
   原版判据 = `AbilityLogic__PlayAnim.c:194/203/215`（逐目标解位置）＋ `:217-222`（**只有最后一个目标**带 `waitAnimToApplyEffect`）。
3. **目标的四元组怎么用**：`AbilityTargetList[i].Player / .Slot` 是**座位 + 格号**，`.InstanceId` 是**稳定身份**（换位/补位后格号会变）⇒ 取视图时**先按 `InstanceId` 找、找不到再退回 `Player@Slot`**（同名单位同格换人那条坑，见 `BattleEvent.InstanceId` 的 doc）。
4. **`null` 与空表要分**：`AbilityTargetList == null` = 「引擎没填」（誓约那一支、以及将来的新发点）⇒ **按改前行为走**（用 `TargetPlayer/TargetSlot/TargetCardId`，最多 1 个）；`Count == 0` = 「这一条能力确实没有目标」。
5. **`Target*` 三位还要不要看**：那是「**玩家点没点**目标」的判别式（原版 `GetActionText` case `0xF`）⇒ **战斗日志 / 动作文本**那一侧继续用它，⛔ 别拿列表长度去顶它（`Heal 2 Self` 那种「没点但有一个目标」会判反）。
6. **今天的可见变化 = 0**：列表长度只有 0 / 1，逐目标循环跑出来与现在**逐字相同**（夹具已证）⇒ 接线后先跑 `BattleScene.Run` 对一眼断言，**画面本该一模一样**；不一样的只有「扩档那天」。
7. **`EventTiming.DurationOf` / `VfxMap`** 不在本件事里 —— 若将来要按「目标数」拉长演出，那是另一条账（先把逐目标播放做出来再说）。
