# B9_E1P01 · `RuleCore.cs` 施工报告（2026-10-26 · 第八十四会话）

## ① 结论（一句话 + 数字）

本文件可做的 4 件全部落地：**landing 续攻（P0，唯一那条红的实现侧）· `CheckIfCodexTriggered` 闸② · `Sacrifice` 快照收待死 · 两处过期注释订正**；
判据**不在**本文件的 2 件（`Side=="any"` 分流口 / camouflage 闸）已定位到确切文件与行并写清改法，未越界改。
**typecheck 0/0 · `play check` rc=0 且指纹 `af8e817839fa1dd199d33320f6bf1b2e` 未变（主档 1128 + 电池 586 逐卡全等）· 伏击回归读数仍 7 伤。**
改动量：`RuleCore.cs +161 / −56`（净 **+105**，14,325 → 14,430 行）· 账补丁 `账/_parts/B9_E1P01.tsv` 4 行（表头 11 列与主表逐字相同）。

## ② 逐条（结论 + 证据）

### ②-1 landing 续攻（P0）· 已做
**结论**：把 `DeclareAttackCore` 劈成**声明层 / 执行层**，landing 支改成「**压一条 `Instant` 道续攻 + `return`**」。X1 记的残余偏差③（就地续打 ⇒ `Landing:` 正文对本记伤害不生效）已消。

**判据（三处，逐条读过）**
- `d:/2/tools/decomp_full/BattleManager__AddAttackContinuationAction.c:451` = `BattleAction___ctor(&action, **1**, …)`（`1` = `BattleActionType.attack`）；
- 同文件 `:638` = `BattleManager__AddAutoActionToQueue(bm, action, *(int *)(globalVars + 0xB8))` —— 道号 = `VarsGlobal.instantPriority`（`d:/2/tools/il2cpp_out/dump.cs:120117`），与 `Landing:` 那条 ability 的默认道（`CardAbility__ctor.c:5` 写 `+0x18 = 1`）**同一条** ⇒ 严格 FIFO：开舱 + `Landing:` 正文先、这一记后；
- 调用点 `BattleManager._ResolveAttack_d__438__MoveNext.c:428`，紧跟 `:379 OnTrigger(0x1b8 /*Landing*/)`；
- 复检口径 = `BattleManager__AllowResolveAttack.c:236-341`（`lStack_210 == 0` 失败 / `hasAvailableActions` / `!HasCurrentTrait(100)` / `IsUnit` / **`:244-245` `0 < CurrentMeleeAttack || 0 < CurrentRangeAttack`（是 OR，不是只看本次打法那一档）** / `!0x96 ∧ !0x370` / `IsValidAttackTarget`）。

**落点**
- `RuleCore.cs:6218`（`DeclareAttackCore`）= **声明层**：闸（`CanAttackNow` / `IsValidTarget`）+ 扣配额 + 现身/失去伪装 + 开舱；
- `RuleCore.cs:6488` = landing 支：`EnqueueAbility(ctx, ActionPriority.Instant, "落地续攻:…", …)` 之后 `return RuleCodes.OK`。入队体里做三件：① `AllowResolveAttack` 复检（攻方还在那一格 / 没晕 / 近战或远程至少一档 > 0 / `IsValidTarget`）② **`FieldAttack` 现算攻击力**（原版每个 `_ResolveAttack` 实例各算一次 `:1116 GetCombatDamage`）③ `using (BeginAction(ctx)) DeclareAttackExecute(...)`；
- `RuleCore.cs:6551`（新方法 `DeclareAttackExecute`）= **执行层**：从「替身改向」到 `CheckWinner` 的整段结算，**不复跑声明层任何一道闸**。普通攻击在声明层末尾**就地**调它（攻击力用声明层取的那一份 ⇒ 与改前逐字相同）。

**验收（本笔跑到的）**
| 读数口 | 命令 | 结果 |
|---|---|---|
| 指纹 | `bash 工具/ruleprobe.sh play check` | **rc=0** · 指纹 `af8e8178…` **未变** · 主档 1128 张 / 电池 586 张逐字节全等（可解释：全池 0 张带 `landing`/`dropPod`；`play` 夹具把单位正面朝上摆） |
| 伏击续攻回归 | `bash 工具/ruleprobe.sh play GSC16 preset face=ambush` | **7 攻 → 实际 7 伤**（日志序 = `面朝下地出手` → `触发 AMBUSH` → `AMBUSH 给了 1 个目标` → `近战攻击 … 7 攻`）⇒ 劈层没有碰到伏击那支 |

🔴 **这条红在测试侧还有一个 α，见 §③-1 / §⑤-1** —— 只改实现它**不会**变绿。

### ②-2 `Side=="any"` 的 19 张卡打不到对面 · **判据不在本文件 ⇒ 未改（已定位）**
**结论**：调度台简报说的「分流口 `RuleCore.cs:3890`」经现读**只是转调那一句**，真正要改的四处有三处在 `EffectResolver.cs`：

| 位置（现读） | 现状 |
|---|---|
| `RuleCore.cs:3890` `return PlayTactic(ctx, p, handIdx, ChosenTargetSlot(ctx, chosen, slot));` | 只传**格号**、不传**侧**；`PlayTactic` 的签名也没有 `side` / `chosen`（`EffectResolver.cs:2417`） |
| `EffectResolver.cs:1988-1992`（`IsLegalPick`）· `:2046-2050`（`PlayTargetPool`） | **已经是两侧并集**（`own = Side=="own"||"any"` / `enemy = Side=="enemy"||"any"`）⇒ 这两处不用改 |
| `EffectResolver.cs:1975` · `PlayTacticCore:2435-2440` | 格位解析**只读我方**（`int side = spec.Side == "enemy" ? 1 - p : p;` 同款）⇒ `"any"` 落进 `p` 一侧 ⇒ 点对面的格子必空 |

⇒ 治本改法（需 `EffectResolver` 写手同批）：`PlayTactic(ctx, p, handIdx, targetSlot, side = -1)`（或直接传 `chosen` 的 `OwnerOf`）→ 两处格位解析改成「**按侧 + 格**取，`side < 0` 时两侧各试一次」，调用点 `RuleCore.cs:3890` 跟着传 `chosen != null ? OwnerOf(ctx, chosen) : -1`。
⚠️ 本文件**没有** `"any"` 这个串（`grep '"any"' RuleCore.cs` = 0 命中）—— 简报里那个行号指向的是一句 `PlayTactic` 转调，不是分流逻辑本身。

### ②-3 `CheckIfCodexTriggered` 闸②（0 费卡不触发）· 已做（本文件那一半）
**判据**（`d:/2/tools/decomp_full/BattleManager__CheckIfCodexTriggered.c:16-43`，全文 43 行现读）：
```
if (param_3 == 0) {                    // ① manaLeft == 0
    if (param_5 != '\0') {             // ② shouldCheckCost
        iVar12 = get_CurrentCost(param_2);   // param_2 = 刚打出的那张牌
        if (iVar12 < 1) return;              // 0 费 ⇒ 整段不触发
    }
    … BattleAction___ctor(&action, 0x46, …) → AddAutoActionToQueue(…, 1, …) …
}
```
调用点实参对照：`PayCardCost.c:30` 第 5 实参 = **1**（普通出牌那条路）；`PayCardPlayedCostOath.c:24` / `PayActiveAbilityCostOath.c:30` = **0**（誓约/主动技那两条路**原版不设**这道闸）。

**落点**：`RuleCore.cs:13075`（`CheckCodex`）新增两个**可选**形参 `CardInstance playedCard = null, bool shouldCheckCost = false`，闸② = `CostOf(ctx, p, playedCard) < 1 ⇒ return`（拿不到牌时**放行 + 出声**）；调用点 `RuleCore.cs:4214`（`PlayCardCore` 末尾）传 `inst, shouldCheckCost: true`。
**残余（B，不是 A）**：战术卡那条路 `EffectResolver.cs:2693` 的 `CheckCodex(ctx, p)` **没传**这两格 ⇒ 0 费**战术**卡打出后仍会触发 Codex（比原版宽）。该文件不在本笔白名单；改法一行：传 `<刚打出的 CardInstance>, shouldCheckCost: true`。
**读数**：`play check` 指纹未变 —— **可解释但不可当证据**：夹具只摆 `FIX_A/FIX_B`、没有 Codex 单位 ⇒ 这一族在夹具上结构上不可观测。

### ②-4 `Sacrifice` 的棋盘快照不含待死单位 · 已做
**判据**：原版 `BattleManagerSupport.BroadcastUnitUsedSacrifice`（`out_cpp2il_dev_ilspy/BattleManagerSupport.cs:1764-1805`）三趟，闸**只有** `cardScript != unitSacrifice`（自排除）——**棋盘那一趟没有 `IsInPlay()`**（与 `BroadcastUnitSpiritStone` `:1057` / `Summoned` 那一族不同）⇒ 待死（≈ `waitingToDie(5)`）照样收。接收方 `CardScript.ReactToUnitUsedSacrifice`（`CardScript.cs:7783-7788`）体内也只有 `OnTrigger(225, …)`。
**落点**：`RuleCore.cs:12481` —— `SnapshotBoardListeners(ctx, null, null, includePendingDeath: keyword == KeywordTable.Sacrifice)`。
**为什么不是调 `BoardJumpIncludesPendingDeath(kind)`**：那是 `EffectResolver` 的 **private**（`EffectResolver.cs:16096`），本文件拿不到 ⇒ 按**同一族名**对齐，并在注释里写死「那份名单是唯一权威、这里不许长出第三份」。另一条广播口（`BroadcastWhenCore`，`EffectResolver.cs:9742`）B1 已经按族打开了。
**状态留 B**：手牌两趟的排除、以及「自排除落在调用方」这两点**未核**（见 §③-3）。

### ②-5 camouflage 闸 · **不在本文件 ⇒ 未改**
`grep -n camouflage RuleCore.cs` 只有 `:6392`（攻击后摘掉那个词）。**可选中性**那道闸在 `EffectResolver.cs:2812` 一带（`IsLegalPick` 的「敌方那一侧的三种不能被选中关键词」）。原版那一支比我们多一个「**施放者 `cardType == 20`**」条件（引擎缺口队列 #6 · `L4` 悬案已查清）⇒ 交给 `EffectResolver` 写手。池内 0 张。

## ③ 没查清的部分（还差什么）

1. **S14 子例④ 的最后一条断言不可满足（α，不是 β）**：`RuleEngineTest_S14.TestS14DropPodOpensAfterAttack` 子例④给**守方**挂了 `dropPod 5` 血池（`:2328`），而 `RuleCore.Hurt` 对带 `dropPod` 的单位是「**血池吃伤害、生命一点不动**」（`RuleCore.cs:8523-8545`：`absorbed = true`、池 `5 → 2`、`dealt = 0`）⇒ `Check(defL.Health, 6, "伤害照落 3 点")` **恒为 9**。
   **同一测试的子例① 已经自证这个口径**：它同样给守方挂 `dropPod 5`、打 3 伤，断的是 `Check(def.KwValue(DropPod), 2, "池只按伤害扣（5 − 3 = 2）")`。
   ⇒ 建议改法（二选一）：**A** 把末条换成断池 `Check(defL.KwValue(KeywordTable.DropPod), 2, …)` + 生命断 9；**B** 去掉守方那个 `AddKeyword(DropPod, 5)`（子例④ 不需要它 —— 它原本是为了「别让这一格被 `ApplyDamage` 打空摘掉」）。**另外**：续攻现在是**入队**，任何「读血量」的断言都要排在泵点之后（原版也是两拍）。
   ⚠️ 本笔**不跑 Unity**（红线）⇒ `RuleEngineTest.Run` 的真实红/绿要调度台起一次才知道；本报告只把「这条断言为什么不可能绿」的证据链给全。
2. **`Side=="any"` 的四处联动没做**（见 §②-2）—— 需要 `EffectResolver` 写手与 `RuleCore.cs:3890` 同批改，单改任一侧都不成立。
3. **`BroadcastUnitUsedSacrifice` 只核了「棋盘那一跳的待死闸」**：手牌两趟我们走 `BroadcastWhenCore` 的统一手牌口（是否逐趟排除发起者那张**未核**）；自排除的落点（原版在**调用方**、我们 `triggers:*` 一族按 `WhenEvents.SelfExcludesOnKeywordTrigger` 的口径**不自排除**）**未核** ⇒ 可能多叫醒发起者自己。
4. **`CheckIfCodexTriggered` 在「主动技能付费」那条路**（`PayActiveAbilityCostOath.c:30`，传 `shouldCheckCost = 0`）我们这边没有对应落点 —— 我们是否有一条「主动技能付费后检查 Codex」的链，**没查**。
5. **`landing` 续攻与 `triggerPriority` 的关系**：今天 `landing`/`ambush` 一族恒为默认 `Instant`；若将来出现非默认道的同类卡，续攻的道号要按那张卡分派（注释里已经写死，未做）。

## ④ 改动清单

**文件：`Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`**（行尾纯 LF，改后复核 `crlf=0 / bare_cr=0 / 14,430 行`；`git diff --numstat` = **161 / 56**）

| hunk（现读行） | 加/删 | 内容 |
|---|---|---|
| `4208 → 4214` | +6 | `PlayCardCore` 末尾的 `CheckCodex` 调用点：传 `inst, shouldCheckCost: true` + 判据注释 |
| `6206 → 6212` | +5 | `DeclareAttackCore` 的 `<summary>`：标明它现在是**声明层**、结算交给 `DeclareAttackExecute` |
| `6440 → 6451` | +2 | 订正一句过期话（原来写「现在这一段不再早退」—— 现在是「压续攻 + return」） |
| `6447-6471 → 6460-6487` | 25 → 28 | 把 X1 那段「这一支仍然是就地继续往下打 / 已记账的残余偏差③」整段换成**新形状**（三处判据 + 为什么必须劈层 + `AllowResolveAttack` 复检口径） |
| `6472-6495 → 6488-6527` | 24 → 38 | landing 支代码：就地续打 → **入队续攻**（`EnqueueAbility(Instant, "落地续攻:…")`）+ 执行层转调 |
| `6498 → 6529` | +30 | **新方法 `DeclareAttackExecute`**（doc + 签名 + 攻方/目标重取）。放在原执行体的**物理位置**上（不是搬到类尾）⇒ 下游只漂移这 30 行，⛔ 没有把整段 700 行搬走 |
| `8488 → 8548` | +2 | `ApplyDamage` 空投舱那一支的注释：原来写「出手那处第二支**仍未做**」→ 改成「已做（`A1617` 取消 + `E1P01` 续攻）」 |
| `12404 → 12467` | +12 / +1 | `FireTriggerAt` 的监听者快照：`includePendingDeath: keyword == KeywordTable.Sacrifice` + 判据/权威出处 |
| `12984 → 13059` · `12988 → 13081` | +17 / +12 | `CheckCodex` 的 doc（判据原文 + 两条调用路的实参对照 + 三档处置）+ 形参 + 闸② |

**新文件：`Unity/资料/完全复刻/账/_parts/B9_E1P01.tsv`**（4 行；表头与 `战斗逻辑驱动.tsv` 逐字相同；`check_patch_conflicts.py` = **无跨补丁重复键**）
- `AddAttackContinuationAction(...)` → **A**（landing 续攻做完，残余说明）
- `AddFollowResolutionAttackContinuationAction(...)` → **A**（把「landing 支仍就地续打」那句过期话删掉）
- `CheckIfCodexTriggered(...)` → **B**（闸② 已补；战术卡调用点未接）
- `BroadcastUnitUsedSacrifice(...)` → **B**（棋盘待死闸已开；手牌两趟 / 自排除未核）

## ⑤ 🔴 顺手发现（看到但**不在**本范围 ⇒ 只报不改）

1. **`RuleEngineTest_S14` 子例④ 的两条断言互相矛盾（α）** —— 详见 §③-1。这是本笔**唯一**没法靠自己变绿的地方：断言断的是「生命 9 → 6」，而同一夹具的守方带着 5 点空投舱血池、池先吃这一记。**建议调度台改测试**（两条改法见 §③-1），而不是再回头改实现。
2. **`forced`（强制攻击）也走「开舱 + landing 支」** —— `OpenDropPod(...)` 与 landing 支都**没有** `forced` 闸（`RuleCore.cs:6429` / `:6488`），而原版 `landing` 那一跳在 `_ResolveAttack` 协程里、`_ResolveForceAttack` 是**另一条协程**（伏击那一支的注释 `:6267` 一带已经查实「`SetAmbush` 的 7 个调用点里没有 `_ResolveForceAttack`」）⇒ 按对称性，强制攻击**大概率也不该开舱/发 Landing**。**未核**（池内 0 张 `dropPod`/`landing`）⇒ 建议与伏击那条一起查一次 `_ResolveForceAttack_d__504__MoveNext.c` 有没有 `0xe6`/`0x302` 那两跳。
3. **`BoardJumpIncludesPendingDeath` 建议改成 `public`**：现在 `RuleCore.cs:12481` 只能按「同一族名」对齐（`keyword == KeywordTable.Sacrifice`），与 `EffectResolver` 那份名单是**两处维护**。把那个函数开成 `public`、两处都调它，就只剩一份判据（`EffectResolver` 写手一行）。
4. **`play check` 的夹具里没有 Codex 单位** ⇒ 「Codex 这一族」在结算指纹上**永远不可观测**（`PlayProbe` 的夹具只有 `FIX_A`/`FIX_B`）。本笔的闸②因此**拿不到指纹证据**。建议给 `PlayProbe` 加一个量具（例如 `preset codex=1` 把 `FIX_A` 换成池内一张带 `Codex` 的单位并预置 0 费手牌），否则这一族只能靠 `RuleEngineTest`。
5. **`EnergySpentSignal` 的判据位置现读确认**：`BattleManager__PayCardCost.c:28-31` 里 `UseMana` 之后紧接着就是 `EnergySpentSignal___ctor(uVar4, param_2, uVar1 /*isPlayer*/, uVar2 /*cost*/, …)` + `Raise<object>` ⇒ 派活包 #1373 说的「三个实参」成立，位置就在 `CheckIfCodexTriggered` 调用的**前一句**。

**📌 读数时间与并发边界（重要）**：上面那组读数取于 **21:44–21:49**（当时工作树是干净的）。
之后同一批再跑两次都**被别的写手的半成品挡住**：`bash 工具/ruleprobe.sh play check` ⇒ `RuleEngine/Core/CountEval.cs(193,28)/(355,28) error CS0103: 名称"ApplyMaxTargets" 不存在`（P-06 正在写）；
`bash 工具/typecheck.sh` ⇒ **19 条全在 `CardPresentation/Battle/BattleDriver.cs`、1 条在 `CardPresentation/Battle/AbilityIconAnim.cs`**（P-11 / P-12 正在写），**`RuleCore.cs` 零错误**。
⇒ 「typecheck 0/0 / `play check` rc=0」说的是**本文件**（以及当时那个一致的工作树）；本笔最后一次改 `.cs` 之后立刻跑过一次 **0/0**，此后每一次复查都没有一条错误落在 `RuleCore.cs`。
