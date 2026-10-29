# B97_ER · 再裁 #54 `CardAbility.CanTriggerAbility` —— acting 闸扩到**全部** trigger

> 2026-10-29 · 执行代理 **B97_ER**（有写权限）· 判据 = `资料/完全复刻/判据/B97_结构差与休眠_再裁.md` **第 54 行**（判乙：「必须做」）· 数据判据 = `资料/普查产出_1025/E3_actingCardCriteria.md`
> 一句话：**第一道闸（`actingCardCriteria` `+0x28`）从「只五族」扩到「全部 trigger」**，逐族给出原版 `actingCard` 的实参出处；
> **查不清的族仍返回 `null` ⇒ 出声 + 放行**（⛔ 不拿 `subject` 顶）。`typecheck` **0 / 0**；
> `play all dump` 与**改前**逐列**零差异**（原因见 §四：**探针根本不加载这一列** ⇒ 它对本闸是盲的）。

---

## 一 · 原版判据（先回源，逐句现读 —— 两份交叉）

**`d:/2/tools/decomp_full/CardAbility__CanTriggerAbility.c`（🥇 全文 144 行）**

| 行 | 内容 | 结论 |
|---|---|---|
| `:71` | `if (*(int *)(param_1 + 0x10) != param_2) return 0;` | 闸①：ability 的 `trigger` 号必须相等 |
| `:74-79` | `param_2 ∈ {0, 0x50(80), 0x91(145)}` ⇒ `goto LAB_180922e57` | **只跳过 `IsRightContext`** |
| `:81-85` | `param_2 ∈ {0xdc(220), 500}` ⇒ `goto LAB_180922e57` | 同上 |
| `:86-113` | `param_2 == 600`（`UseSpiritStone`） | `IsRightContext` + **灵气余额**门槛 ⇒ `:112 goto LAB_180922e57` |
| `:114-124` | `param_2 == 0x25d(605)`（`UseFaith`） | `IsRightContext` + **信仰余额**门槛 ⇒ 落到 `:130` |
| `:126-129` | 其余 trigger：`IsRightContext` = 假 ⇒ `return 0` | 只有**这一格**按 trigger 号跳过 |
| `:130` `LAB_180922e57` | — | **所有 trigger 都汇到这里** |
| `:131-132` | `CheckIfMeetsCriteria(param_4 /*thisCard*/, param_5 /*actingCard*/, param_6, *(ability+0x28), 0)` | **第一道闸 = 筛 `actingCard`，不看 trigger 号** |
| `:134-135` | `CheckIfMeetsCriteria(thisCard, targetCard, targetCard, *(ability+0x30), 0)` | 第二道闸 |
| `:136-138` | `BoardAnalysis.MeetsCriteria(manager, *(ability+0x38), thisCard, actingCard, targetCard, …)` | 第三道闸 |

**交叉核对（⛔ 不单信一份）**：
· 形参表 `d:/2/tools/il2cpp_out/dump.cs:23487` `OnTrigger(AbilityTrigger trigger, BattleManager manager, CardScript thisCard, CardScript actingCard, CardScript targetCard, DamageInfo)` ⇒ **`param_5` 就是 `actingCard`**（与 `.c` 的实参位置一致）；
· 候选语义 `decomp_full/FilterMethods__CheckIfMeetsCriteria.c:38-44`：`ta == 0x14(20) self` 那一支是 `op_Equality(param_1, *(lVar9+0x10) /*= param_2*/)` ⇒ **`param_2` = 被筛的那个「谁」**（actingCard）——与 `+0x28` 那一跳的实参位置吻合；
· ILSpy 那一份**不参与本判**（`CanTriggerAbility` 在 `out_cpp2il_dev_ilspy` 里实参常量有丢的历史）⇒ 本判全部以 `.c` ＋ `dump.cs` 形参表为准。

⇒ **再裁报告的说法成立**：`actingCardCriteria` 对**所有** trigger 生效；我们改前只在 `Attack` / `Die` / `GetDarkPact` / `Reanimated` / `GetsMarkerlight` 五族启用 = **比原版宽 ⇒ 多触发**。

---

## 二 · 逐族「`actingCard` 是哪一格」（本批新查的部分）

方法：对 23 个有 `abilityActingCriteria` 行的 trigger，**逐个找接收端的 `OnTrigger(…, thisCard, actingCard, targetCard, …)` 实参**（`.c` 现读），再对回我们 `BroadcastWhen` 的那一格。

| trigger | 落行 | 接收端（判据） | 原版 `actingCard` | 我们给的 | 依据 |
|---|---|---|---|---|---|
| 50 `UnitAttack` | 14 | `CardScript__ResolveUnitAttacked.c:28` `(this, param_2, param_3)` | 攻击者 | `subject` | 既有（E3） |
| 90 `OtherUnitDeath` | 12 | `RawCardScript__OnMinionDeath.c:19` `(this, param_4, param_5)` | **凶手** | `actingCard` 形参 | 既有（E3） |
| 80/81 `ThisUnitDeath`/`Unstable` | 1 | `RawCardScript__OnMinionDeath.c:22-23`（`thisCard == deadCard` 支） | **死者自己** | `subject`（监听者=死者时） | 🆕 本批 |
| 60 `UnitDamaged` | 0 | `CardScript__ResolveDamageDealt.c:160` `(this, *(DamageInfo+0x10), param_2)` | 伤害来源 | `actingCard` 形参 | 🆕 本批（发射口由 `B95_WRE` 批 2 建好） |
| 510/515/520/570 | 2+0+2+0 | `ReactToUnitMarked/Markerlight/HuntMarked/Reanimated.c:6` 全是 `(this, param_2, param_2)` | 被标记/被复活那个 | `subject` | 510/515/570 既有；520 🆕 |
| 10 `OtherCardPlayed` | 7 | `RawCardScript__OnCardPlayedWithTarget.c:96` `TriggerAbilities(…, 10, mgr, thisCard=param_3, cardPlayed=param_4, param_5)` | **被打出的那张** | `subject`（= 被打出的单位；战术牌 ⇒ `null`） | 🆕 本批 |
| 190/195 `OtherUnitSummoned`/`ThisUnitSummoned` | 11 | `CardScript__ResolveUnitSummoned.c:31/:46` `(this, param_2, param_3)` | 被召唤者 | `subject` | 🆕 本批 |
| 620/630/645/647/650/661/726 | 2+3+1+1+1+1+2 | `TriggeredSwarm.c:6` / `TriggeredSynapse.c:6` / `TriggerOtherCardMob.c:20` / `TriggerLoseStealth.c:6` / `TriggerOtherCardCodex.c:20` / `TriggeredDuty.c:6` / `TriggeredFerocity.c:6` —— **全是 `(this, param_2, …)`** | 触发这次关键词的那个单位 | `subject` | 🆕 本批（`RuleCore.FireTriggerAt` 那段「`subject` = 原版 `onTrigger` 的 `actingCard`」是同一句） |
| 660 `TriggeredPray` | 3 | `TriggeredPray.c:6` `(this, param_2, 0)` | 祈祷者 | `subject` | 🆕 本批 |
| 665 `OnCollectFaith` | 1 | `ReactToCollectFaith.c:6` `(this, param_2, param_2)` | 收集者 | `subject` | 🆕 本批 |
| 695 `OnCollectQuestPoints` | 3 | `ReactToCollectQuestPoints.c:6` `(this, param_2, param_2)` | 收集者 | `subject` | 🆕 本批 |
| 700 `OnSecretCreated` | 2 | `ReactToSecretCreated.c:6` `(this, param_2, param_2)` | 预调主体（造牌方督军） | `subject` | 🆕 本批（`EffectResolver.BroadcastCreatedCardWithWarlordPreCall` 已按此接） |
| **0** `ThisCardPlayed` | 2 | `AbilityLogic__PlayAbility.c:2038` `(…, *(param_3+0x10), *(param_3+0x10), *(param_3+0x20))` | 动作里的 `actingCard` | **`null`（判不了）** | 见 §二·b |
| **180** `UnitDeathByAbility` | 4 | `RawCardScript__OnMinionDeath.c:37` `(this, param_4, param_5)` | 凶手 | **无入队点**（我们这条只发 `EvtKind.Trigger`） | 见 §二·b |
| **260** `OtherCardDrawn` | 2 | `RawCardScript__OnCardDrawn.c:6` `(this=param_3, acting=param_4, 0)` | 被抽到的那张 | **`null`** | 我们的 `BroadcastWhen(Draw)` 只有 `card`（`CardDef`），没有 `UnitState` |
| **280** `AboutToAttack` | 1 | `CardScript__ResolveCancelAttack.c:12` `(this, param_2, param_3)` | 待攻者 | **`null`**（`TriggerNameOfWhenKind` 对 `triggers:abouttoattack` 返 `null` ⇒ 闸不进入） | 见 §二·b |
| **410** `FollowUpResolution` | 4 | `CardScript__FollowUpResolutionPlayed.c:6` `(this, param_1 /**self**/, param_2)` | **自己** | **`null`** | 我们的 410 走 `FollowUpSchedulerOf` 硬编码表，不在 `BroadcastWhen` 里 |
| **455** `OtherCardSpiritStone` | 1 | `TriggerOtherCardSpiritStone.c:20` `(this, param_2, 0)` | 用灵气石的那张 | **`null`** | 我们 `BroadcastWhen(SpiritAbility, who, null, null)` 的 `subject` = **null** |
| 291 `UnitStunned` | 0 | `ReactToUnitStunned.c:6` `(this, param_3 /**施害者**/, param_2)` | 施害者 | **`null`** | `BroadcastKeywordEvent` 只带被晕者（判据已查清、**缺发射口那一格**） |

### 二·b · 为什么这 6 族仍是 `null`（⛔ 不拿 `subject` 顶）

上表最后一列的 6 族（0 / 180 / 260 / 280 / 410 / 455 / 291）**要么我们的广播里根本没有那一格**、
**要么这条能力在我们的实现里根本不经 `BroadcastWhen`** ⇒ 硬接就等于「拿 `subject` 顶」= **筛错人** ⇒
按 `E3` 立的契约：**返回 `null` ⇒ 出声 + 放行**（⛔ 不把「不知道」当「不满足」）。
它们的落行数 = 2+4+2+1+4+1+0 = **14 行 / 约 13 张**（其中 180 那 4 行今天没有任何入队点、无可观测）。

---

## 三 · 改了什么（逐处 · `文件:行` 为改后现读行号）

| 文件 | 行 | 改动 |
|---|---|---|
| `RuleEngine/Core/EffectResolver.cs` | **:7766** | `AbilityActingGateApplies(kind)` 改成 `return !string.IsNullOrEmpty(kind)` —— **所有 trigger 都过第一道闸** |
| 同上 | **:7773-7803** | `AbilityActingGateCard(kind, subject, actingCard)` 重写：按 §二 那张表逐族给值，**查不清 ⇒ `null`** |
| 同上 | **:7717 一带（注释）** | 铁律 5 就地订正：原注释「⛔ 其余事件一律不启用」已过期，留下订正痕迹 + 判据（`:126-141`） |
| 同上 | **:10329-10333** | `FireBoardWhenListener` 的入队：`abilityActingCard` / **新** `abilityActingTrigger` / `abilityActingGate` 三格；**`Die` 且监听者就是死者自己** ⇒ trigger 名换 **`ThisUnitDeath`**、候选换 `subject`（判据 `OnMinionDeath.c:17-24`） |
| 同上 | **:10033 / :10039 / :10047** | `BroadcastPersistentWhen` / `BroadcastHandTrapWhen` / `BroadcastHandWhen` 三条非棋盘跳**透传 `actingCard`**（改前它们没有这一格） |
| 同上 | **:10520 / :10720 / :10825（签名）** | 上面三个方法各加尾参 `UnitState actingCard = null` |
| 同上 | **:10578 / :10775 / :10944** | 那三条入队点把 `abilityActingGate` 从**恒 `false`** 改成 `AbilityActingGateApplies(kind)`，并给 `abilityActingCard` / `abilityActingTrigger` |
| `RuleEngine/Core/ActionPump.cs` | **:289** | `EnqueueAbility` 新尾参 `string abilityActingTrigger = null` —— **与 `abilityTrigger` 解耦**（见下） |
| 同上 | **:369** | `string actingTrig = abilityActingTrigger ?? abilityTrigger;`（缺省回退 ⇒ **老调用点逐字不变**） |
| 同上 | **:372-431** | 第一道闸的**宿主卡 / 座位**改成与 target 闸逐字同形：① 有 `actor` ⇒ 现查格号 + `actor.Card`；② 没有 `actor` ⇒ `abilityHostCard` + `abilityOwnerSeat`（`thisCard` 传 `null`）；都拿不到 ⇒ 出声 + 放行 |
| `CardPresentation/Editor/B97_ER_Assert.cs`（新） | 全文 | `CheckB97ERAbilityGates()` 5 段断言（见 §六） |
| `CardPresentation/Editor/B97_ER_Assert.cs.meta`（新） | — | guid `9b1e6a63fbb443319f00d39b751f1be6`（全库唯一，已核） |
| `资料/完全复刻/账/_parts/B97_ER.tsv`（新） | — | 账补丁（11 列 · 首行表头 · 键 = `(CardAbility, CanTriggerAbility(...))`） |

### 为什么必须**解耦** `abilityActingTrigger`

原版三道 criteria 是**一条 `&&` 链**、对**所有** trigger 生效；但我们这三道闸的覆盖面**不一致**：
`acting` 本批已扩到全部族，而 `target(+0x30)` / `board(+0x38)` **仍只有七族**（`AbilityLevelGateApplies`，那是另一笔账）。
若沿用同一个 `abilityTrigger` 形参，扩 acting 就等于**顺带把 target/board 两道闸也扩出七族**
（`AbilityLevelCriteriaAllows` 在 `set == null && why == null` 时会**继续判 board 闸** ⇒ 真的会多筛）——
那是**本批没查清的第二件事**，⛔ 不能顺手带出来。⇒ 新增 `abilityActingTrigger`，只喂第一道闸。

**净行数**：`ActionPump.cs` **±0**（整块替换：+2 形参/局部量、−2 旧支路）；`EffectResolver.cs` **+47**
⇒ 该文件下游 A 档落点**整体漂移**（`check_ledger_points` 实测：EffectResolver 的漂移差值**只有 +22 / +38 / +47 三档**，
正好是我三处插入点的累计值 ⇒ **这些漂移全部由本批造成，且是均匀的**，`--fix` 一次可平）。

---

## 四 · 🔴 `play all dump` 前后逐列差异表（重点 —— **结果 = 全 0，但这不是「没生效」，是探针盲区**）

命令（两遍，改前 / 改后）：
```
bash 工具/ruleprobe.sh play all dump d:/4/Unity/_tmp_view/B97_ER_dump_final.tsv
```
对照 = **改前**那份 `d:/4/Unity/_tmp_view/repin_1029/play_after_remfix.tsv`（调度台指定的「改前 dump」）。
比较方式 = **按列名对齐**（25 列，表头逐字相同），逐卡逐列比。

| 列 | 改前 → 改后 | 变化卡数 |
|---|---|---|
| `id` `name` `type` `bucket` `warlord` | 逐字相同 | 0 |
| `playRc` `rcName` `played` `faceOps` `selfPlace` `mech` | 逐字相同 | 0 |
| `other` `expectedPay` `energyDelta` `handCostDelta` `dmg` `changes` | 逐字相同 | 0 |
| `crash` `note` `fixtureFit` `injected` `playOrder` `pickTarget` `atkCount` `exhausted` | 逐字相同 | 0 |
| **合计** | **两文件逐字节相同**（md5 `c2e2284d4abd67d084e9c25adbb2a32c`） | **0 / 1129** |

**变化卡名单 = 空。每张卡的变化列 = 无。**

### 四·b · 🔴 为什么必然是 0 —— **探针根本不加载这一列**

`工具/ruleprobe/Program.cs:625 LoadPool()` 是**手写的 `System.Text.Json` 映射**（不走 `Data/CardDatabase.cs`），
它**没有映射 `abilityActingCriteria`**（`grep -rn "abilityActing" 工具/ruleprobe/*.cs` = **0 命中**）⇒ 每一张卡的
`CardDef.AbilityActingCriteriaVersioned` 恒 `false` ⇒ `ActingLevelCriteriaForTrigger` 恒回「列没落」⇒
闸恒走「**判不了 ⇒ 放行**」。

**直接实测证据**（本代理在闸里临时打了一条 `ctx.Log`，跑完已删）：
```
[B97ER-DIAG] trig=OtherUnitSummoned card=Vox Operator cand=FIX_B verdict=null:`abilityActingCriteria` 列没落（旧表 < v27）
```
⇒ **本闸在 `play` / `play check` 这两条路上永远不筛**（这是 `A1911` 那一族「探针与引擎口径不同」的第 N 例）。

**对重钉的含义（🔴 请调度台照这条办）**：
· `PinnedPlayMd5` **不需要**因为本批重钉 —— 本批**没有**让结算层任何一个读数变化（逐字节相同）；
· 但**也⛔不能**把「dump 零差异」当成本批的验收证据 —— 它什么也没证明。
· 真验收 = Unity `RuleEngineTest` 里那 5 段断言（§六），**必须跑**。

### 四·c · `play check` 的实测读数（⛔ 与本批无关，别算在本批头上）

| 项 | 读数 | 归因 |
|---|---|---|
| `①-a 主档` | **28 张卡变了**（新增 0 · 消失 0） | 🔴 **改前就有**：同一批 28 张在**改前**的 `repin_1029/play_after_remfix.tsv` 与钉住基线的对比里**一模一样**（我用脚本逐列比过，三份 dump 都是 28 张）⇒ 属**上一会话 oath 那一批**的未重钉状态 |
| `①-b 电池` | **75 张卡变了**（12:59 那次跑是 73、13:02 是 75，**中间我只删了一行 `ctx.Log`**） | 🔴 **别的写手正在改 `RuleCore.cs`**（13:00 那次探针重建直接报 `RuleCore.cs(1914,28): error CS0103: EnqueueTurnSetupEffectRemovals` ⇒ 有人在写）⇒ 电池读数**当前不稳定**，⛔ 不能拿它判任何东西 |

---

## 五 · 待接线（⛔ 本代理白名单外，**没动**）

### ① `ThisUnitDeath(80/81)` 的**真可达入口**在 `RuleCore.cs`（一行补丁）

`GOF3` 的 80 行今天**能被查到**（`ActingLevelCriteriaForTrigger("ThisUnitDeath")` 有值，断言 ⑤ 钉着），但
**Die 广播那一刻死者已经被 `BoardSlots.RemoveAt` 摘掉了**（`RuleCore.cs:9911` 早于 `:10065` 的 `BroadcastWhen(Die)`）
⇒ 棋盘跳**根本不含它** ⇒ 我给的那条「监听者=死者 ⇒ 换 80」今天**走不到**。
真正承载 `ThisUnitDeath` 能力的是 **`FireTriggerAt(u, Backlash)`**（`RuleCore.cs` 的死亡族那一跳）
→ `FireTriggerEnqueue`（**`RuleCore.cs:12742`**）。**建议补丁**（一行，把两个实参挂上）：

```csharp
            bool queued = EnqueueAbility(ctx, lane,
                (u != null ? u.Name : "?") + "·" + keyword,
                body,
                actor: u, requireActorInPlay: ActorMustBeInPlayForTrigger(_carrierCard, null, keyword),
                extraTrigger: extraTrigger, abilityContext: _abilityCtx,
                requireActorInPlayOrDying: requireActorInPlayOrDying,
                abilityActingTrigger: _kwTriggerName, abilityActingCard: u, abilityActingGate: only == null && _kwTriggerName != null);
```
⚠️ 两处前提得先确认（我没查到）：`TriggerNameOfKeyword("backlash")` 今天走 `ExtraTriggerName`，**它没映射 `backlash`**
（`ActionPump.cs:1894-1911` 现读）⇒ 挂上去也只会拿到 `null` → 闸不进入。要真生效，**先给 `ExtraTriggerName` 补
`backlash → "ThisUnitDeath"` / `unstable → "Unstable"`**（判据：`OnMinionDeath.c:22-23` 那两个 id 就是这两族的名字）。

### ② `ruleprobe` 的 `LoadPool` 缺这一列（**这是「读数盲区」，不是我的活**）

`工具/ruleprobe/Program.cs:625` 之后那段映射要补 `abilityActingCriteria` → `CardDef(…, abilityActingCriteria:, abilityActingCriteriaVersioned:)`
（照 `abilityLevelCriteria` / `abilityTargetCriteria` 那几列的既有写法，并像 `A1911` 那样加一个 `nAct` 读数行 + 缺列出声）。
**补完以后**本闸才在 `play` / `play check` 上可观测，那时才谈得上「重钉 `PinnedPlayMd5`」。
⚠️ 顺带一提：同样的盲区**很可能也罩着 v26 `abilityLevelCriteria`（`+0x30` 那道闸）** —— 我没逐个核，只是 `grep` 时没看到它，**别据此当结论**。

---

## 六 · 断言（新文件，**未接线** —— ⛔ 本代理不动 `Editor/RuleEngineTest.cs`）

`CardPresentation/Editor/B97_ER_Assert.cs` · 入口 `CheckB97ERAbilityGates()` · 5 段：

| 段 | 验什么 | 灭自证 |
|---|---|---|
| ① 结构（反射） | 21 个「改前没启用」的 kind 现在**全过**闸（0 漏网）；`Deploy` 那族 `actingCard = subject`；`Draw` 那族**仍返回 null** | 退回五族 ⇒ 红；一律 return subject ⇒ 红 |
| ② 单元级（`Deploy` 家） | 正例（敌方 acting ⇒ 触发）· 负例（己方 ⇒ 不触发 + 出声）· 无候选 ⇒ 放行 + 出声 | 这一族没接 ⇒ 负例放行 ⇒ 红 |
| ③ **解耦** | 只给 `abilityActingTrigger` 时**第二道闸一次都不跑**（`AbilityLevelCriteriaWarningsForTest` 不变）；老调用点行为不变 | 改回共用 `abilityTrigger` ⇒ 计数变 ⇒ 红 |
| ④ 无 `actor` 的新来路 | 宿主卡 + 座位 ⇒ 正/负例都判得动；只有宿主卡没座位 ⇒ 放行 + 出声 | 删掉那条支 ⇒ 负例放行 ⇒ 红 |
| ⑤ 真卡池 | `GOF3` 的 `ThisUnitDeath` 行查得到（`ta=20`）；它在 `OtherUnitDeath` 上**没有**行；`Die` 广播对观察者仍是 `OtherUnitDeath` | 80 错落到 90 ⇒ 红；`Die` 整体改名 ⇒ 红 |

**建议插在 `Editor/RuleEngineTest.cs` 的 `Run()` 里**（紧接 `{ CheckB97W1Grants(); }` 之后、`B97_W1` 同一批）：
```csharp
        // 🆕 2026-10-29（调度台代接线）：B97_ER —— 再裁 #54 第一道闸扩到全部 trigger。
        { CheckB97ERAbilityGates(); }
```
🔴 **故意不用 `Step(...)`**（那会动 `_ranCases` / `VerifyRunCaseCoverage()` / `gen_diag_runner.py` 三处），与
`B97_W1` / `B96_CR` 同一约定。

---

## 七 · 验收读数（如实）

| 命令 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_er bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（新断言文件也被扫进去了 —— `gen_csc_rsp.py` 自动扫 `Assets/`） |
| `bash 工具/ruleprobe.sh play all dump …` | rc=0 · 全池 1129 张 · **与改前逐字节相同** |
| `bash 工具/ruleprobe.sh play check` | rc=**1**（`①-a` 28 张 / `①-b` 73→75 张）—— **全部与本批无关**，归因见 §四·c；⛔ 没动 `PinnedPlayMd5` / `PinnedBaselineMd5` / `PinnedParseMd5` / 基线文件 |
| `python 工具/check_ledger_points.py` | 合计 A 5561 · 通过 5413（告警 573）· **漂移 138** · 失败 10 —— 其中 **EffectResolver.cs 那部分（+22/+38/+47 三档）全部由本批 +47 行造成**；`ActionPump.cs` **净 ±0 行 ⇒ 零漂移**；其余漂移（+1，127 条）属别的写手。**建议调度台在「无 `.cs` 写手 + 补丁合并完」时跑一次 `--all-A --fix`** |
| 行尾 | `ActionPump.cs` LF 2345 / CRLF 0 / 裸 CR 0；`EffectResolver.cs` **CRLF 19585（未翻）**；新文件 LF |

---

## 八 · 没查清 / 残余（⛔ 不猜）

1. **`+0x30` / `+0x38` 两道闸仍只七族** —— 原版它们同样对**所有** trigger 生效（同一条 `&&` 链）。
   本批**只动了第一道**（再裁 #54 的范围）。⇒ **这是一条独立的、更大的缺口**（建议单立账：`AbilityLevelGateApplies` 的覆盖面）。
2. **六族的 `actingCard` 拿不到**（§二·b）：0 / 180 / 260 / 280 / 410 / 455 / 291。
   其中 **260 最接近可修**（原版 acting = 被抽到的那张，我们只有 `CardDef` 没有 `UnitState` ——
   要么给 `BroadcastWhen(Draw)` 带一个「抽牌来源」的 `UnitState`，要么承认 `ta` 里那 2 行不可判）。
3. **`play` 探针的盲区**（§五·②）没修 ⇒ **本批今天没有任何「真跑出来的行为读数」**，
   只有：原版判据（§一）+ 逐族实参表（§二）+ 结构/单元级断言（§六，**未执行**）。
   🔴 **这是一条如实标着的风险**：改的是「收紧」，最坏情况是**多筛掉本该触发的卡**；一旦 Unity
   那一跑出现异常，第一嫌疑就是 §二 表里某些族「`subject` ≠ 原版 acting」。
   🔴🆕 **可预期的连带**：Unity 里 `AbilityActingCriteriaVersioned` 是 **true** ⇒ 本闸**真的会筛**（探针里不会）。
   `RuleEngineTest.Run` 里凡涉及「监听器被叫醒」的既有断言，**都可能出现新的不触发**（那些正是改前「多触发」的地方）
   ⇒ 若下一跑出现若干红，**先按「本批收紧」解释、再逐条判是真缺陷还是期望值该更新**，⛔ 别先怀疑夹具。
4. **`Dependent`**：`Damaged(60)` 我按 `ResolveDamageDealt.c:160` 的 `*(DamageInfo+0x10)` 认定 = 我们的
   `actingCard` 形参（`RuleCore.cs:9406` 由 `B95_WRE` 批 2 传的 `attacker`）。**没有**核过 `EmitHit` 所有
   调用点是不是都传了来源 ⇒ `attacker` 为 `null` 时会走「判不了 ⇒ 放行」（安全，但也没筛）。池内 60 的 acting 行 = **0** ⇒ 今天零影响。
5. **`ThisUnitDeath` 那一半没有真的通**（§五·①）：触发器名能查到（断言钉着），但**入队口在 `RuleCore.cs`**（白名单外）
   且 `ExtraTriggerName` 还缺两个映射 ⇒ 今天 `GOF3` 那条 80 行**仍不会被筛**（它的 `ta=20 self` 本来就恒真，
   所以**无可观测差异**，如实记着）。
