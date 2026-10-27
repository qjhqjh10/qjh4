# B9 · E5ENG —— `RuleCore` 三条真偏离（R2T3B 现核）修复报告

日期：2026-10-27 · 代理：**E5ENG**（执行型）· 独占文件 = `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`
判据来源：`资料/完全复刻/报告/B9_R2T3B_B行后半核账.md` §②（第 1/5/6 条）· 原版方法体 = `d:/2/tools/decomp_full/`
账补丁 → `账/_parts/B9_E5ENG.tsv`（5 行 · 11 列表头逐字复制 · 只改 `差异`/`轮次`/`备注` · `状态`/`等级` 原样）

---

## ① 结论（三条逐条）

| # | 条目 | 结论 | 代码动了吗 |
|---|---|---|---|
| 1 | `CardScript.RemoveAttackThisTurn` / `BattleManager.AddForceAttack` 的**入队预扣** | ✅ **已修**（照原版先预扣、失败原样退回） | **是**（`RuleCore.cs:14408-14427`） |
| 2 | `CardScript.CancelAttack` 的 `+0x350` 闸 | ✅ **字段查清 = `playerInitiatedAttack`**；**引擎侧无可加的闸**（原版 8 个结算侧调用点全传 `playerInitiated = 0` ⇒ 闸恒放行） | **否**（刻意不加，加了是死代码） |
| 3 | `MatchData.SetBotOpponentDeck` 洗牌闸漏 140 | ✅ **已修**「两档都不洗 + 两侧分开判」；⚠️ 残留一格 = 入口窗还没传 `matchType`（见 ③） | **是**（`RuleCore.cs:148 / 156 / 183-185 / 191-193 / 222 / 224`） |

---

## ② 证据（逐条，`文件:行号`）

### 条 1：预扣（最严重 · 多给玩家 1 次攻击）

原版（`decomp_full/BattleManager__AddForceAttack.c`，全文 97 行逐句读过）：
- `:39` `local_111 = 0;`
- `:40` `if (param_7 /*= dontCountAttack*/ != '\0') {`
- `:41` `local_111 = *(int *)(param_2 + 0x48) == 0;`  ← `wasFree`（`+0x48` = `attacksThisTurn`）
- `:42` `CardScript__RemoveAttackThisTurn(param_2, 0);` ← **无条件下调**（`CardScript__RemoveAttackThisTurn.c:5-7`：`if (0 < attacksThisTurn) attacksThisTurn--`，下限 0）
- 退还：`BattleManager._ResolveForceAttack_d__504__MoveNext.c:73` `if (*(char *)(param_1 + 0x117) != '\0')` ⇒ `:77` 排 `AddResetActions`（`+0x117` = 状态机里那份 `BattleAction` 的 `+0xF7` = `wasFree`）
- **同形的第二个调用点**：`decomp_full/BattleManager__AddScriptedForceAttack.c:43-48`（`237`/`238` 走它，见 `AbilityLogic__PlayAbility.c:2756 / 2772`）；`231` 走 `AddForceAttack`（`PlayAbility.c:2666-2686` 的 `0xe7` 支）

我们（改前）：`RuleCore.cs` 的 `DeclareAttackRefundingForced` 只在「**攻击前**计数 == 0」时**打完之后** −1，**入队不预扣** ⇒
- `攻击前 == 0`：原版 `0→0→+1→-1 = 0`；我们 `0→+1→-1 = 0` ✅ 一致
- `攻击前 > 0`：原版 `n→n-1→+1 = n`（不排 ResetActions）；我们 `n→+1 = n+1` ❌ **多 1**

改后（`RuleCore.cs:14415-14427`，逐行）：
```
14418  int countBefore = atkUnit != null ? atkUnit.AttacksThisTurn : 0;
14419  bool wasFree = atkUnit != null && countBefore == 0, refunds = ForcedAttackRefunds(ctx);
14420  if (refunds) RemoveAttackThisTurn(atkUnit);          // ① 入队即预扣
14421  int code = DeclareAttackCore(ctx, p, atkSlot, tgtP, tgtSlot, ranged, true);
14423  if (code != RuleCodes.OK) { if (refunds && atkUnit != null) atkUnit.AttacksThisTurn = countBefore; return code; }
14424-14425  if (wasFree && refunds) RefundForcedAttackAction(...)
```
⇒ 两个分支都与原版**净结果一致**。**为什么预扣必须做在 `DeclareAttackCore` 之前**（不是「打完之后再 −1」这种等价写法）：
`DeclareAttackCore` 里 `attacker.AttacksThisTurn++` 之后**紧接着**就是 `EmitBloodThirst`（`RuleCore.cs:6411-6412` → `:12392` 的 `if (u.AttacksThisTurn != 1) return;`，= 原版 `CardScript__ActivateBloodThirst.c` 的 `+0x48 == 1`）⇒ 「攻击前 = 1」那一格若不在**攻击之前**把计数压回 0，嗜血会读到 2、**不亮**；原版读到 1、会亮。预扣把这一格也一并对齐。

**唯一不照抄的一格**（已写进代码注释与账）：原版「预扣了但目标不合法 ⇒ 白扣一点行动」是它自己的 bug（`_ResolveForceAttack…:34-60` 不合法直接 `goto LAB_1809b42e5` → `FinishResolvingAction`，补偿那一跳永远排不上）；而 `EffectResolver.TryAttackOnce`（`EffectResolver.cs:3310-3323` 的 `foreach`）会对**每个候选**各调一次 `DeclareAttack` ⇒ 照抄会连扣多次。**我们改成「失败就把预扣原样退回」**。

### 条 2：`CancelAttack` 的 `+0x350`

- `decomp_full/CardScript__CancelAttack.c:41`：`if ((param_2 != '\0') && (*(char *)(param_1 + 0x350) == '\0')) { LogWarning; return; }`
- **`+0x350` = `playerInitiatedAttack`（`bool`）**，两路互证：
  - 写点 ①：`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:12413` —— `SetupAttack(bool playSound, bool playerInitiated = false)` 体内 `playerInitiatedAttack = playerInitiated;`
  - 写点 ②：`decomp_full/CardScript__TryDraggingFromBoard.c:60` —— `*(undefined1 *)(param_1 + 0x350) = 1;`（玩家拖拽起手的那一记）
  - 读点：`CardScript__CancelAttack.c:41`
- 语义：**不是玩家发起的那一记攻击，不许被「玩家取消」这条 UI 路撤掉**（只打一条 LogWarning、`PrepareAttack(false)` 不跑）。
- **可达性（决定「要不要加闸」）**：全库 `CardScript__CancelAttack` 调用点共 10 处，逐个数完 ——
  - 结算/引擎侧 8 处**全部传 0**：`_ResolveAttack_d__438__MoveNext.c:172 / 299 / 372 / 436 / 537 / 1314` · `CancelBattleActionFromQueue.c:35` · `OnTurnEnd.c:45` · `Stun.c:113`
  - 只有 2 处传 1，**都是 UI 输入**：`BattleManager__Update.c:1088`（玩家点空处 / 攻击被打断 ⇒ 取消瞄准）· `BattleManager__CancelActionStates.c:29`（`UIstate == 6` 那一档）
  ⇒ **引擎侧闸恒放行、恒无差**；我们的等价物在**表现层的攻击选择器**（战斗界面线），`RuleCore` 里没有 `CancelAttack(playerInitiated)` 这个入口。⛔ 因此**不加**（加了是永远走不到的死代码）。
- 附带：`CancelActionStates` 那一档（账主表第 237 行）已在补丁里补了「那个 `true` 会命中这道闸」的后果说明。

### 条 3：`SetBotOpponentDeck` / `CreatePlayerDeck` 的洗牌闸

原版是**两条独立的闸**，判据逐字：
- 玩家侧：`decomp_full/BattleManager__CreatePlayerDeck.c:88` `cVar1 = MatchData__ShouldShuffleDeck(*(undefined4 *)(lVar4 + 0x1c) /*matchType*/, 0);` → `:90` `ShuffleDeck`
- 对手侧：`decomp_full/MatchData__SetBotOpponentDeck.c:20-26` 建好 `DeckAndWarlordData` 后 `if ((matchType != 100) && (matchType != 0x8c /*140*/)) ShuffleCardLibrary(...)`
- 闸本体：`decomp_full/MatchData__ShouldShuffleDeck.c:3-6` —— `(param != 100 && param != 0x8c) ? 1 : 0`
⇒ 两档 `{100 Tutorial, 140 TutorialReplay}` **都不洗**，其余 12 档都洗；两侧同一个 pair。

我们的改法（`RuleCore.cs`）：
- `:156` 新增可选形参 `MatchType matchType = MatchType.Undefined`（追加在形参表**末尾** ⇒ 位置调用/具名调用都不受影响）
- `:148` 该形参的 XML doc（同一行追加，**不动行号**）
- `:183-185`：
  ```
  MatchType shuffleMt = matchType != Undefined ? matchType : (tutorial != null ? Tutorial : Undefined);
  bool shuffleP1Deck = shuffle && TutorialRules.ShouldShuffleDeck(shuffleMt), shuffleP2Deck = shuffle && ShouldShuffleDeck(shuffleMt);
  ```
- `:191-193` 教程分支里那句 `if (shuffle && !TutorialRules.ShouldShuffleDeck(MatchType.Tutorial))` 改成 `if (shuffle && !shuffleP1Deck)`（**只留一条规则**，不再自己判一次）并去掉 `shuffle = false;`（旗标已经是权威）
- `:222 / :224` 两侧分别喂 `shuffleP1Deck` / `shuffleP2Deck`
- `TutorialRules.ShouldShuffleDeck`（`TutorialScript.cs:1378`）本来就已经是 `!(Tutorial || TutorialReplay)` —— 错的是**调用点硬编码 `MatchType.Tutorial`**，不是那个函数。

---

## ③ 没查清 / 残留（如实）

1. **`matchType` 入口接线还没做**（不在我的文件白名单里）：`BattleDriver.Begin`（`CardPresentation/Battle/BattleDriver.cs:5433`）现在不传 `matchType`，而它落 `Ctx.PlayMode` 在 `NewBattle` **之后**（同文件 `:5440` 那一句）⇒ 引擎在开局那一刻**拿不到真实模式号**。今天**行为等价**（非教程局 = `Undefined` = 洗 = 原版 14 档里 12 档的默认；教程局由 `tutorial != null` 兜住 `{100,140}` 这对 pair）。**要把「140 不洗」真正接上，得在 `BattleDriver.Begin` 那句加 `matchType: MatchTypes.For(playMode ?? …)`** —— 一行，但 `BattleDriver.cs` 另有写手，未动。
2. **`GameMode.TutorialReplay(9)` 全仓没有启动路径**：`grep TutorialReplay` 只命中枚举（`GameplayVariables.cs:44`）、`MatchTypes.For` 的派生平（`BattleContext.cs:2097`）、`GameplayVariables.For` 的教程档、档案/奖励的判档 ⇒ **这一格今天不可达**，「接上就生效」。
3. **`+0x350` 的读点我只找到 `CancelAttack.c:41` 一处**。没有做「全仓按偏移扫」的穷举（`0x350` 会大量撞上别的类的同偏移字段，按偏移扫会刷假阳性）⇒ 若将来出现第二个读点，需重核。
4. **`+0x350` 的类型补证只有 ILSpy 一处**（`bool`，`12413`）；`dump.cs` 里 `CardScript` 字段块（`il2cpp_out/dump.cs:35370` 起）我**没有**逐条拉出 `0x350` 那一行来交叉验证（`dump.cs` 有字段名 + 偏移，理论上可查；时间关系未做）。⇒ 结论等级：**两路互证（ILSpy 写点 + 反汇编写点，偏移完全一致）**，不是单点。
5. 条 2 的**表现层那一半**（玩家点空处取消瞄准要不要看「这一记是不是玩家发起的」）没有核，属战斗界面线。

---

## ④ 改动清单 · 指纹归因 · 断言配方 · 顺手发现

### 4.1 改动清单（全在 `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` 一个文件）

| 行 | 改了什么 |
|---|---|
| `:148` | `botSeat` 的 `</param>` 之后**同一行**追加 `<param name="matchType">` 说明 |
| `:156` | 形参表末尾追加 `MatchType matchType = MatchType.Undefined`（**同行**，不动行号） |
| `:183` | 新增 1 行注释：两条原版洗牌闸、为什么两侧分开判 |
| `:184-185` | 新增 `shuffleMt` / `shuffleP1Deck` / `shuffleP2Deck` |
| `:191` | 教程分支的闸改成复用 `shuffleP1Deck`；删掉 `shuffle = false;` |
| `:222 / :224` | 两侧 `BuildPlayer` 分别喂各自的旗标 |
| `:14406` | doc 里「为什么不在入口预扣」→「**入队预扣**」 |
| `:14379-14391` | 13 行「净语义」注释块**整块重写**（原文明确写着「⛔ 不能真的照 ① 在入口处预扣」—— 那条已被推翻，按铁律 5 就地改掉，保留订正痕迹） |
| `:14418-14423` | 预扣 + 失败原样退回（+3 行） |

**行数漂移**：整个文件 **+3 行**（`15009 → 15012`）。漂移全部落在 `:14428` 之后 ⇒ `账/战斗逻辑驱动.tsv` 里 `RuleCore.cs:14xxx` 的落点会报 +3（重同步点 `check_ledger_points --all-A --fix` 一次修）。行尾：**CRLF 0 / LF 15012 / 裸 CR 0**（二进制现读，未翻行尾）。

### 4.2 验证

- `TMPDIR=/tmp/wf_E5ENG bash Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
  （中途有一次 3 个错全落在 `EffectResolver.cs`（`DrawnNow` / `DrawHandHopTakes` 未定义）= **E6ENG 正在写的文件**，按 §13·3 ≠ 我的问题；稍后复跑即 0/0。）
- `bash Unity/工具/ruleprobe.sh check` ⇒ **哨兵失败 0 条**，解析差异 0 行（`PlayProbe` 之外的哨兵全绿）。
- `bash Unity/工具/ruleprobe.sh play check` ⇒ **rc=1**（预期红灯，基线 = `b671acdb…`，当次 = `517be27d…`）。

### 4.3 指纹差异**逐条归因** —— 全部**不是我的**

做法：**对照实验**（临时把预扣那一行注掉、把退回那一行去掉，跑同一份 `play check`，再原样还原 —— 文件只有我一个写手，安全）：

| 状态 | 结算指纹 |
|---|---|
| 改后（预扣 ON） | `517be27dd33773e07861d5762f118241` |
| 对照（预扣 **OFF**，= 改前语义） | **`517be27dd33773e07861d5762f118241`** · 逐卡差异也一模一样（`1 张 / 13 张`） |

⇒ **我的改动对 `play check` 的影响 = 0 条**，正是本任务说的「结构上在探针盲区里」（`PlayProbe.Capture` 只记 攻/远/甲/血/关键词，**不记 `AttacksThisTurn` / `Exhausted`**）。

因此下面这 14 条红灯是**本波其它代理**的改动造成的，**不归我**（重同步点请按各自改动去归因）：

| 档 | 卡 | 变化形状（摘） |
|---|---|---|
| ①-a 主档 1 张 | `BL6` | 落场后自身数值 `2/3/0/4` → `2/3/0/6`、附魔 `{camouflage1,darkpact1,markofchaos1}` → `…2`；能量 20→18、手牌少一张 |
| ①-b 电池 13 张 | `AM38` `BL6` `GOF11` `GOF2` `GSC4` `SAU30` `SAU67` `SW25` `TAU19` `TAU28` `UM104` `UM107` `UM18` | 统一形状 = `FIX_WAY … {waystone1}` 的途中变化从 `{…,waystone1}` 变成 `{…}`/`0/0/0/1`、`FIX_A` 攻/血变化、`net` 普遍 −1～−7 |

（`waystone` / 附魔层数 / `FIX_A` 三样都不是我这三条改动碰得到的东西。）
🔴 **我没有重钉基线、没有改 `PlayProbe.PinnedPlayMd5`**（按红线）。

### 4.4 断言配方（`ruleprobe` 看不见 ⇒ **必须靠断言**；写进 `RuleEngine/Editor/RuleEngineTest.cs`）

> `RuleEngineTest.cs` **不在我的白名单**（`RuleEngine/Core/**` 之外的写手归属未明）⇒ 以下按「可直接粘贴」给出，请调度台在同步点派给测试文件的写手。
> 夹具沿用现成的 `TestA4Batch2ForceAttack`（`RuleEngineTest.cs:1760` 起，`ProbeBattle / ToP1Turn / Place / HandIdx / ForcedAttackRefund.SetForTest` 都在用）。
> 已有的 `AttacksThisTurn == 0` 断言（`:1810`）覆盖的是 **`wasFree` 那一格**，我这次改的**不是**那一格 ⇒ 它改前改后都该绿（我用它当「没把旧语义改坏」的回归锚）。

**(A) 条 1 的主断言（`!wasFree` 那一格 · 唯一抓得住「漏预扣」的一条）**

```csharp
// ---- ①·b 「本回合已经行动过」的单位被强制攻击 ⇒ **净计数不变**（原版 1 →（预扣）0 →（真打）1）----
{
    ForcedAttackRefund.SetForTest("T_MurderousDesires2", ForcedAttackRefund.Verdict.Refund);
    var card = Tactic("T_MurderousDesires2", 4, "Make a damaged friendly unit attack by itself");
    var ctx  = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
    ToP1Turn(ctx, 3);
    ctx.Players[0].Energy = 12;
    var foe = Place(ctx, 1, 3, Unit("EBig", 1, 0, 20), exhausted: true);   // 攻击力 0 ⇒ 不反击
    var wounded = Place(ctx, 0, 2, Unit("FWounded", 1, 3, 5));
    wounded.Health = 3;                       // 满足 `DamagedOnly`
    wounded.AttacksThisTurn = 1;              // ★ 关键：本回合**已经行动过**
    wounded.Exhausted = true;                 // ★ 与上一行配套（已用完额度 ⇒ 疲劳）
    int foeBefore = foe.Health;

    CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_MurderousDesires2"), 2), RuleCodes.OK,
              "已行动过的单位仍能被强制攻击（原版 `forceAttack` 那条路不看配额）");
    Check(foeBefore - foe.Health, 3, "★ 强制攻击**真的打出去**了（预扣只是记账，不封攻击）" + LogTail(ctx));
    Check(wounded.AttacksThisTurn, 1,
          "★ 灭自证：**净计数不变（1→1）** —— 原版 `AddForceAttack.c:41-43` 入队预扣 + 打后不再 -1。"
        + "⚠️ 改前这里是 **2**（漏预扣 = 多给玩家 1 次攻击）；🔴 `ruleprobe play` **看不见这一格**"
        + "（`PlayProbe.Capture` 只记 攻/远/甲/血/关键词）⇒ 这条是这一格**唯一**的守卫");
    CheckTrue(wounded.Exhausted,
              "★ 配套：疲劳态不变（原版这一支**不排** `AddScriptedAttack`/`ResetActions` ⇒ 两位不重算）");
}
```

**(B) 条 1 的「失败原样退回」（我们刻意不照抄原版 bug）**

```csharp
// 预扣只对「真打得出去」的那一记生效；候选全不合法 ⇒ 一个额度都不扣（原版在这里白扣一点，我们不照抄）
{
    ForcedAttackRefund.SetForTest("T_MurderousDesires3", ForcedAttackRefund.Verdict.Refund);
    var card = Tactic("T_MurderousDesires3", 4, "Make a damaged friendly unit attack by itself");
    var ctx  = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
    ToP1Turn(ctx, 3); ctx.Players[0].Energy = 12;
    var wounded = Place(ctx, 0, 2, Unit("FWounded", 1, 3, 5));
    wounded.Health = 3;
    wounded.AttacksThisTurn = 1; wounded.Exhausted = true;
    // 目标全非法：只放一个**不可被近战选中**的敌方（例：飞行/noMelee 之类，按现有夹具挑一档），或干脆不给目标
    // ⇒ 走 `DeclareAttack` 的 `IsValidTarget` 失败分支
    int before = wounded.AttacksThisTurn;
    CheckTrue(RuleCore.DeclareAttack(ctx, 0, 2, 1, /*非法格*/, ranged: false, forced: true) != RuleCodes.OK, "…");
    Check(wounded.AttacksThisTurn, before, "★ 失败的强制攻击**不扣**行动额度（预扣原样退回；原版会白扣一点）");
}
```

**(C) 条 3（洗牌闸）**

```csharp
// `{100,140}` 两档都不洗、其余都洗；**两侧分开判**
{
    var a = PDeck(有序的 30 张卡); var b = PDeck(同一份);
    string id0(PlayerState ps) => ps.Deck.Count == 0 ? "" : ps.Deck[0].Card.Id;
    var c100 = RuleCore.NewBattle(a, b, seed: 7, shuffle: true, matchType: MatchType.Tutorial);
    var c140 = RuleCore.NewBattle(a, b, seed: 7, shuffle: true, matchType: MatchType.TutorialReplay);
    var c10  = RuleCore.NewBattle(a, b, seed: 7, shuffle: true, matchType: MatchType.Ranked);
    Check(id0(c100.Players[0]), 原首张, "★ Tutorial(100)：玩家牌库**不洗**");
    Check(id0(c100.Players[1]), 原首张, "★ Tutorial(100)：对手牌库**不洗**（两侧分开判的另一半）");
    Check(id0(c140.Players[0]), 原首张, "★ TutorialReplay(140)：玩家牌库**不洗**（改前这里会被洗）");
    Check(id0(c140.Players[1]), 原首张, "★ TutorialReplay(140)：对手牌库**不洗**（原版 `SetBotOpponentDeck` 那条闸）");
    CheckTrue(!SequenceEqual(c10.Players[0].Deck, 原序), "★ 反例：Ranked(10) 照原版默认**要洗**（同种子 ⇒ 确定性）");
}
```
⚠️ 判「洗没洗」用**首张 id + 同种子逐位比**，别用「洗出来的顺序 != 原序」这种概率断言（同种子下其实是确定的，但换个种子就可能巧合相等 ⇒ 首张那条更稳，两者一起写最保险）。

### 4.5 顺手发现（不归我改，按要求报上来）

1. 🔴 **`play check` 对「计数类」偏离全盲，今天被实测坐实**：同一份代码「预扣 ON / OFF」结算指纹**逐字节相同**（`517be27d…`）。⇒ 这类账行（`AttacksThisTurn` / `Exhausted` / `canAct` / `canAttack`）**永远不能靠 `play check` 升 A**。若要把它们纳入探针，得给 `PlayProbe.Capture` 加两列（会改钉住的 md5 ⇒ 属调度台决策，⛔ 我没动）。
2. **账行已存在重号/漂移（供重同步点核）**：
   - `账/战斗逻辑驱动.tsv:140`（`AddForceAttack`）`轮次` 列**本来就有 `B9_E5`**（不是 `B9_E5ENG`）—— 疑似同名前缀的早期代理留下的；我这行补丁把 `轮次` 覆写成 `B9_E5ENG`，若那批要保留 `B9_E5` 请以补丁为准合并。
   - 同文件 `:845` 的 `我们的落点` 写 `RuleCore.cs:14346 / 14367 / 14383`，**实际已是 `14408 / 14432 / 14448`**（差 ~60 行，改前就漂了）；`:702` 的落点写 `RuleCore.cs:5240-5243`，`IsAttackingOnBoard` 的声明实际在 `:5339`。⇒ 建议在 `--fix` 那一次一并按**符号名**重定位（`--fix` 能修行号，但修不了「名字已变」的那一类）。
3. `bash ruleprobe.sh check` 里有两条**改前就在**的告警：卡池 md5 与基线 md5 都跟钉住的不同（`cc603636… / ce3faf91…` vs 钉住值），且 `Source` 列有 6 条被 `;` 多切、5 条被截断 —— 与我这三条无关。
4. `DeclareAttackRefundingForced` 的 `ForcedAttackRefunds(ctx)` 现在**在攻击之前**求值一次（改前是攻击之后求值）。它只读 `FactionSourceCard → ActingUnit.Card → CardPlaySource`（`RuleCore.cs:14512-14526`），而 `DeclareAttackCore` 不改这三格 ⇒ 取值**与改前逐字同源**；已写进代码注释，供日后复核。

---

# 第二批 · 探针补列（`工具/ruleprobe/PlayProbe.cs` · 2026-10-27 · 调度台派单）

目标：把「`AttacksThisTurn` / `Exhausted` 对 `play check` 全盲」这个洞补上。
本波 `工具/ruleprobe/` 归我独占 ⇒ 只动了 `PlayProbe.cs` 一个文件（**没碰** `RuleEngine/Core/**`、**没碰** `RuleEngineTest.cs`）。

## B-① 改了什么（逐点，全部**只追加**）

| 位置 | 改动 |
|---|---|
| `Snap`（`PlayProbe.cs:182-193`） | 新增 `ActCnt[2]` / `Exh[2]` 两个字段 + 为什么必须加的 doc（含「ON/OFF 指纹逐字节相同」那条实证） |
| `Capture`（`:2245-2248` 一带） | 逐方填 `UnitNumCol(ctx, p, false/true)` |
| **新方法 `UnitNumCol`**（`:2300-2319`） | 把每格 `AttacksThisTurn` / `Exhausted` 摊成 `P0[3]=1 P0[5]=2`；**只收有人那格 · 只打非 0 值**（空串 = 全员 0 / 全员未疲劳）⇒ 既看得见「0→1」「1→2」、又不会把 dump 刷成两倍大（第一版打全量 0 时 dump 从 174 KB 涨到 337 KB，已按此收紧 → 216 KB） |
| `Fingerprint`（`:2325-2328`） | 末尾追加 `行动计数 … ‖ …` / `疲劳位 … ‖ …` 两段（`play <id>` 打前打后那两屏直接看得见） |
| `Reading`（`:166-172`） | 新增 `AtkCount` / `Exh`（doc 里写明**进**指纹，与 `PlayOrder`/`PickTarget` **相反**） |
| `Measure` / `MeasureHero` 尾部 | 各自把终局快照那两格写进 `Reading` |
| `PfCols` + `FingerprintRow`（`:3444 / :3468-3471`） | **末尾**追加两列 `atkCount` / `exhausted` ⇒ **进结算指纹** |
| `PfECols` + `FingerprintERow`（`:3512 / :3529-3532`） | 同上（事件电池那一套行）—— 电池档原来也全盲 |
| `EventReading`（`:1500-1505`）+ 电池填充（`:2126-2127`） | 电池局**终局**那两格（早退那几条也带上） |
| dump 表头（`:2730-2735`）+ 行写入（`:2770-2773`） | **末尾**追加 `atkCount` / `exhausted` 两列（前面 23 列一个字没动） |

**没做的一件事（刻意）**：**没有把这两格塞进 `Diff()`**。塞进去会让 `changes`/`changesFull` 多出两类新行 ⇒ `mech`/`other`/「A1431 失明候选」的判定口径全变，等于顺手改了一把全池判决尺子。⇒ 新增走**独立两列**，`Diff` 保持原样（下面的实测证明：旧列 1128×22 格**零变化**）。

## B-② 新指纹（⛔ 我**没有**改常量）

```
public const string PinnedPlayMd5 = "79057e33cd3984c36055a11a60e0a83f";
```
- 现常量仍是 `b671acdb0b60d86b17ffe3a9f9e69c25`（`PlayProbe.cs:3470`，**未动**）；基线文件 `基线/out_play_baseline.txt` 也**未重生成**。
- 取法（不是手算）：`bash 工具/ruleprobe.sh play all pf /tmp/wf_play_new.txt` 写完盘后，再 `play check /tmp/wf_play_new.txt` —— ② 哨兵那行会把**文件 md5 全串**打出来（`:3866`），我照抄。
- ⚠️ **这是 2026-10-27 07:14 的快照**。里面还含**别的代理**那 14 条改动（见 B-③）⇒ 若同步点前引擎再动，重生成后这个串会变 ⇒ **重钉时以那时的现算值为准**。
- 重钉两步（归你）：① `bash 工具/ruleprobe.sh play all pf d:/4/Unity/工具/ruleprobe/基线/out_play_baseline.txt` ② 把上面那行常量改成它算出来的 md5。

## B-③ 新旧两版**逐卡差异**归因（`play all dump` 前后各一次 ＋ 指纹逐列比）

**A. dump 层**（`/tmp/wf_play_before.tsv` ← 改前 / `/tmp/wf_play_after.tsv` ← 改后；1128 张 × 23→25 列）：

```
列数 before=23 after=25 · 新增列 = ['atkCount', 'exhausted'] · 消失列 = []
行数 1128 / 1128 · 卡集合一致 = True
=== 旧列是否有任何变化 ===
  OK  旧列 0 处变化（逐卡 x 逐列全等 => 加列没有改动任何既有读数语义）
```

**B. 指纹层**（`基线/out_play_baseline.txt`（旧钉住版）↔ 我的新 `pf`）：

| 档 | 只在新增两列上不同 | 别的列也变了 | 结论 |
|---|---|---|---|
| 主档 1128 | **1127 张** | **1 张**：`BL6`（`changesFull`） | `BL6` 是**别的代理**的改动（同一张也出现在第一批那次 `play check` 红灯里，形状 = `FIX_WAY {waystone1}` 途中变化 + 附魔层数 `…1→…2`） |
| 事件电池 586 | **573 张** | **13 张**：`AM38` `BL6` `GOF11` `GOF2` `GSC4` `SAU30` `SAU67` `SW25` `TAU19` `TAU28` `UM104` `UM107` `UM18`（`net`/`netFull`/`steps`/`netDmg`） | 与第一批实测的 13 张**同一批**，同样不是我的 |

⇒ **加这两列没有带来任何卡行为变化**；「所有行都不同」只是因为多了两列（旧文件根本没有这两列）。

## B-④ 🔴 关键发现：**列补上了，但主档那一半仍然没信号**（如实报）

| 档 | `atkCount` 非空行 | 说明 |
|---|---|---|
| 主档 1128 | **0 / 1128** | 🔴 **2026-10-27 第四批更正**：不是「没人出手」（`EC46 dmg=2` / `GOF93 dmg=15` 证明出手了）—— 是**探针把强制攻击一律按「退还」处理**（`ForcedAttackRefund` 表不在探针编译集 ⇒ 恒 `Unknown`）⇒ 计数必然回 0。见 `判据/主档夹具缺口_评估.md` §一·3 |
| 事件电池 586 | **214 / 586** | ✅ **有真信号** —— 电池的 `softAtk` 那一步真的打了一记，终局 `P0[2]=1` |

🔴 **2026-10-27（第四批）就地更正 —— 本段原来写「231 族连牌都打不出去，因为夹具没有受伤的友方单位」，那两句都错**：
1. 夹具默认就给我方三张部队各扣 2 血（`PlayProbe.cs:697-711`，`FixtureChip=2`）—— `play AM70 preset pick=1` 能把 `FIX_A` 从 `2/0/0/2` 治到 `2/0/0/4` ⇒ **已受伤友方本来就在**。
2. `BL64` 打不出去的真因 = 它 `unitConditions` 的 `{role:target, cond:341 canUseDuty, ex:1}` 被 `ConditionEval.cs:548-550`（`!u.DutyUsed`，对正常单位恒真）配 `ex=1` **取反后把目标池全排除** ⇒ **引擎判据问题，不是夹具**。
3. 「235/236 读数里 `行动计数` 恒空」也不是「没攻击」—— 是探针退款表没装载（见上表第 1 行）。

⇒ **结论（诚实，已按第四批改写）**：这两列修的是**读数**那一半（电池档 214 张真有信号、`Exhausted` 位也进指纹了）；
主档那一半的**真因是两件事**：(a) 探针「一律退还」的失真、(b) `cond=341` 把 `BL64` 挡在选目标那一步 —— **两件都不是夹具缺口**。
⇒ 我第一批那条 ① 的守卫**仍然是断言**（已落地：§第三批 A/A2），⛔ 不能因为「补了列」就当它已被 `play check` 盯住。
~~「给夹具补两个位（受伤友方 + 已行动友方）」~~ ⇒ **第四批判为【不做】**：受伤友方本来就有（收益 0 张），已行动友方只对 1～2 张名义有效且 `BL64` 还卡在选目标 ⇒ 收益 ≈0、代价 = 全池 1128 行重读。详见 `判据/主档夹具缺口_评估.md`。

## B-⑤ 落点「旧 → 新」（你点名要的两处 ＋ 我顺手看到的）

**`账/战斗逻辑驱动.tsv`（⛔ 主表我没动）**：

| 行 | 列 | 旧 | 新（2026-10-27 现读） |
|---|---|---|---|
| `:845` `RemoveAttackThisTurn()` | 我们的落点 | `RuleCore.cs:14346` / `:14367` / `:14383` | **`RuleCore.cs:14408`**（`DeclareAttackRefundingForced`）/ **`:14432`**（`RemoveAttackThisTurn`）/ **`:14448`**（`RefundForcedAttackAction`） |
| `:702` `CancelAttack(bool)` | 我们的落点 | `RuleCore.cs:5240-5243` | **`RuleCore.cs:5324-5342`**（`IsAttackingOnBoard`：doc 5324-5338 · 声明 5339 · 体 5341；那道 `cardState ∈ {3,0x11}` 闸的判据行 = 5325-5326） |

**另外三处 `PlayProbe.cs:NNNN` 引用（在 `备注`/`判据要点` 散文里，不是 A 行落点，闸不会报；但**早就陈了**，顺手记）**：

| 账行 | 原引用 | 现在真正的位置 | 备注 |
|---|---|---|---|
| `:195`（`AlreadyPlayedTroopThisTurn`） | `PlayProbe.cs:1583` | **`:2233`**（`s.Troops[p] = ctx.TroopsPlayed[p]`；`🎖 出兵数` 那条 diff 在 `:2389`） | 差 650 行 ⇒ **改前就陈**（我这轮只加了 73 行） |
| `:1291`（`UsesQuestPoints`） | `PlayProbe.cs:453-457`（`Deck(p0Fac…)` 的注释） | **`:306`**（`HeroOf("FIXWarlord_" + fac, fac)`）· 夹具注释见 `:1240-1260` 一带 | 差 147 行 ⇒ 同样是陈的 |
| `:1344`（`ShouldRemnantDestroyOnTurnEnd`） | `PlayProbe.cs:1707`（夹具阵营改 SaimHann） | **`:1997`**（`new CardDef("FIX_WAY", …, "SaimHann", …)`） | 差 290 行 ⇒ 陈的 |

## B-⑥ 验证

- `bash Unity/工具/ruleprobe.sh play all dump <out>` ⇒ rc=0（1128 张全跑，列数 23→25）。
- `bash Unity/工具/ruleprobe.sh play all pf <out>` ⇒ rc=0，写入 1714 行（主档 1128 ＋ 电池 586），头两行注释里的列名已自动带上 `…/changesFull/atkCount/exhausted`。
- `bash Unity/工具/ruleprobe.sh play check <新 pf>` ⇒ **逐卡 0 差异**（同一份现算 vs 同一份文件），② 哨兵红 = **预期**（常量还没重钉），并打出新 md5。
- `bash Unity/工具/ruleprobe.sh check` ⇒ **rc=0**（解析哨兵没被我碰坏）。
- ⛔ 未跑 Unity、未动 git、未改 `CLAUDE.md`/`项目任务.md`/账主表。

## B-⑦ 产物

- 代码：`d:/4/Unity/工具/ruleprobe/PlayProbe.cs`（唯一改动；`PinnedPlayMd5` 一个字没改）
- 跑出来的三份读数（**临时**，`_tmp_view/` 已 gitignore）：
  `d:/4/_tmp_view/e5eng/dump_before.tsv`（改前 dump）· `dump_after.tsv`（改后 dump）· `pf_new.txt`（新指纹文件，md5 = `79057e33…`）
  ⚠️ 判读完可以整目录删掉；⛔ 别把它当基线用（基线仍然是 `工具/ruleprobe/基线/out_play_baseline.txt`）。

---

# 第三批 · 断言落地（`RuleEngine/Editor/RuleEngineTest.cs` · 2026-10-27 · 调度台派单）

第一批那三条配方（§4.4）**已真正落成断言**，不是散文了。

## C-① 改了哪里（一个文件 · 只追加）

| 位置 | 改动 |
|---|---|
| `Run()`（`:117-119`） | 在 `Step(TestA4Batch2ForceAttack);` **之后**插 `Section(…)` + `Step(TestB9EngForcedAttackAndShuffleGate);`（+3 行） |
| 类尾（`:38349` 起） | 新方法 `TestB9EngForcedAttackAndShuffleGate()`（含判据注释块 ＋ 五组断言，+176 行） |

⛔ 没有覆盖任何既有内容：`Run()` 的插入点在 `TestA4Batch2ForceAttack` **之后**、类尾是**追加**（`E7ENG` 那批 `EvtKind` 计数断言一个字没动）。

## C-② 落地的五组断言（每条都「前提先断、结论后断」，⛔ 无 `if (前提) Check(结论)`）

| 组 | 被测行为 | 前提断言 | 结论断言（★ = 判别式） |
|---|---|---|---|
| **(A)** `!wasFree` 强制攻击 | 已行动过的单位被强制攻击 | 受伤前提 `Health==3` · 卡在手 · `PlayTactic` 返回 `OK` | ★ `AttacksThisTurn == 1`（**改前 = 2**）· 敌人掉 3 血（= 攻击真的打出去）· 疲劳位仍真（配套读数，**不区分新旧**，已如实标注） |
| **(A2)** 时序灭自证 | **预扣必须在 `DeclareAttackCore` 之前** | 带 `Blood Thirst` · `ActionQuota==2` · `PlayTactic OK` | ★ **`Trigger`+`BloodThirst` 信号 = 1 条**（嗜血「亮」说明计数在 `++` **之前**已被压回 0）· `AttacksThisTurn == 1`。🧨 **把预扣挪到 `DeclareAttackCore` 之后（= 「打后补减」那种净结果等价的写法）⇒ 信号数变 0** —— 这条能把「净结果一样、时序错」唯一地分开 |
| **(B)** 失败候选退回 | 目标不合法 ⇒ 预扣原样退回 | 敌方 1 号格为空 · 返回码非 `OK` | ★ `AttacksThisTurn` 不变（仍是 1）。🧨 只加预扣、忘了退回 ⇒ 变 **0**（那正是原版「白扣一点」那一格，我们刻意不照抄） |
| **(B2)** 不退还族的反例 | `NoRefund` 族（230/233/235/236）**不许**预扣 | `Of(id)==NoRefund` · 攻击成功 | ★ 计数正常 +1（1→2）。🧨 去掉预扣那一步的 `refunds` 闸（无条件预扣）⇒ 变 **1** |
| **(C)** 洗牌闸 | `{100,140}` 不洗、其余洗、**两侧分开** | 两局都建出来了、牌库非空 | ★ Tutorial(100) 双侧 == `shuffle:false` 基准（逐张同序）· ★ TutorialReplay(140) 双侧同（🧨 闸里写死 `Tutorial` = 改前 ⇒ 红）· ★ 反例：Ranked(10) 必须**与基准不同**（🧨 闸写成「一律不洗」⇒ 红） |

**(C) 的判法说明**：拿 `shuffle:false` 那一局当基准，比 `Deck 顺序 + Hand 顺序` 的逐张 id 串 ⇒ 不依赖「洗出来像不像随机」这种概率判断；`Ranked` 那一格是**同种子确定性**的（新随机源 = 开局预烧表 + 游标，`E1RNG`），所以「必须不同」是硬事实、不会偶发绿。

## C-③ 静态核对过的三处「最可能翻车」的假设（跑不了 Unity，只能读代码核）

1. **强制攻击的**攻击者池**不看**「这个单位本回合还能不能动」** —— `DoForceAttack` 的 `attackers` 只由 `ResolveTargets(… op.Target/Condition …)` + `IsAlive` 筛（`EffectResolver.cs:3153-3164`），**没有** `CanAttackNow`；`IsLegalPick` 也只是 `pool.Contains(u)`（`:2028`）⇒ 「已行动过」的单位**仍然能被选中去打**。
2. **`DeclareAttackCore` 的 `allowAutoActions` 短路**（`RuleCore.cs:5697-5705`）在 `forced` 时不查疲劳/配额 ⇒ 已行动过的单位打得出这一记。
3. **`ForcedAttackRefund.Of(null)` 不抛**（`ForcedAttackRefund.cs:112-121`：`string.IsNullOrEmpty(cardId)` 先挡）⇒ `(B)` 里不设 `ActingUnit` 时走 `Unknown` ⇒ 下游**退还**（= 预扣会真的发生），断言才不是空转。

## C-④ 没能做的

- ⛔ **没跑 `RuleEngineTest.Run`**（红线：不跑 Unity；同步点你统一跑）⇒ 五组断言的**语义**只做了上面 C-③ 那三处静态核对 + typecheck。
  若红了，**优先看这三处**（尤其 (A) 的 `PlayTactic` 返回码、`(A2)` 的信号条数），别先怀疑引擎。
- ⛔ 没给 `账` 补丁：我那 5 行补丁**已合并**，再发一份同键补丁会在下次合并时把后来的手工修订盖回去（`CLAUDE.md` 那条「补丁是定时炸弹」）。
  ⇒ 建议你在同步点往 `:845` / `:140` / `:1080` 的 `备注` 各加一句「断言已落地：`RuleEngineTest.cs` 的 `TestB9EngForcedAttackAndShuffleGate`（A/A2/B/B2/C 五组）」。

## C-⑤ 行号漂移（供 `--fix` / 手工订正）

- `RuleEngineTest.cs`：**+179 行**（`38521-179 = 38342` → 现 **38521**；`Run()` 里 +3、类尾 +176）。行尾 **38521 CRLF / 裸 CR 0**（逐字节核过，没被翻）。
- ⇒ **所有 ≥ `:117` 的 `RuleEngineTest.cs:NNNN` 引用一律 +3**。我现数了账里共 **11 处**，**全部在 `差异` / `备注` 散文里、没有一处在 `我们的落点`** ⇒ `check_ledger_points` 不会因为这一批报红，但那些字会指错 3 行。清单（旧 → 新）：
  `35835→35838` · `15434→15437` · `4062→4065` · `5460-5465→5463-5468` · `5852→5855` · `17487→17490` · `4073→4076` · `4189→4192` · `2182→2185` · `5002→5005` · `33610-33613→33613-33616` · `34695→34698` · `27323-27352→27326-27355`（后两行同属 `:981` / `:1218`）。

## C-⑥ 产物

- 代码：`d:/4/Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`
- 验证：`TMPDIR=/tmp/wf_E5ENG bash Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（编辑器程序集已覆盖 `RuleEngine/Editor/`）。

---

# 第四批 · 夹具缺口评估（只诊断 · 不改代码 · 2026-10-27）

**产出正本 → `资料/完全复刻/判据/主档夹具缺口_评估.md`**（四问逐条 + 决策点）。本节只留结论与「纠错」。

## D-① 先纠错：我第二批那句「夹具没有受伤的友方单位」是**错的**（铁律 5，已就地改在 §B-④）
- 夹具默认给我方三张部队各扣 2 血：`PlayProbe.cs:697-711`（`FixtureChip = 2`，`:55`）。
- 实测 `play AM70 preset pick=1`：`FIX_A 2/0/0/2 → 2/0/0/4`（治疗 2 点生效）⇒ **已受伤友方本来就在**。

## D-② `BL64` 打不出去的真因（查到数据行级）：**不是夹具，是 `cond=341` 把目标池筛空**
- 它的 `unitConditions` 两行：`{ab:-3,lg:-1,role:cardtarget,cond:40 damaged,ex:0}`（`ab=-3` 没挂上 ⇒ 不生效）＋ **`{ab:0,lg:0,role:target,cond:341 canUseDuty,ex:1}`**（生效）。
- `ConditionEval.cs:548-550` 的 `case 341: return !u.DutyUsed;` 对**还没用过 Duty 的单位恒真**，数据写的是 `ex:1`（取反）⇒ **池里正常单位全被排除** ⇒ `IsLegalPick` 假 ⇒ `ErrNoTargetAvailable`。
- 排除法：不是 damaged 筛（另两张 `DamagedOnly=1` 的 `UM2`/`UM116` 主档 `rc=OK`）· 不是解析（基线里 `BL64` 的 spec 是 `Side=own|Kind=unit|Count=1|SubtypeFilter=-|DamagedOnly=1`）· 不是合成卡（`preset sub=BL1` 换真卡照样失败）。
- ⇒ **新增待办（不在我白名单）**：`cond=341` 的 `ex` 语义 / 判据要重核（原版这张卡是可打的）。

## D-③ `atkCount` 主档全空的真因（本条最要紧）：**探针「一律退还」的量具失真，不是夹具**
- `ForcedAttackRefundMap.cs` 在 `Data/`，**不在 `ruleprobe` 的编译集** ⇒ `LoadFromUnity` 是空 `partial` ⇒ 表恒 `null` ⇒ `Of()` 恒 `Unknown` ⇒ 下游**一律退还** ⇒ 强制攻击成功后计数必然回 0。
- **实证**：`play GOF93`（`chosenAbility=235` = 原版**不退还**）日志出现「Hunta Rig 重置行动：本回合攻击次数 **1 → 0**」⇒ 探针把它当退还族了。
- ⇒ `EC46 dmg=2` / `GOF93 dmg=15` 说明**强制攻击确实发生了**（已经由 `dmg`/`changesFull` 记到）；`atkCount` 的正确用途是**「退款有没有丢」的哨兵**，不是「攻击发生没发生」的读数。

## D-④ 强制攻击那 12 张的根因分级（问 1 的正面回答）
| 类 | 张数 | 卡 |
|---|---|---|
| 打出去那一拍就该攻击、**跑通** | **2** | `EC46`(237) · `GOF93`(235 Rally) |
| 打出去那一拍就该攻击、**打不出去** | **1** | `BL64`(231) —— `cond=341` 筛空（D-②） |
| **主档那一拍本来就不该攻击**（Teleport / Slay / Ecstasy / Ferocity / Rally / Stomp / Cruelty / Stimulation / 回合开始） | **9** | `DA25` · `EC12` · `SAU70` · `SAU33` · `GOF86` · `SW9` · `SW5` · `SW29` · `EC22` |
| 池内 0 张 | — | `230` / `233` |

另：主档另外 **11 张** `ErrNoTargetAvailable` 属**另一族**（8 张要 `friendly Vehicle/Battlesuit`、2 张要「受伤的敌人」），
**已排掉「夹具缺一辆 Vehicle」**（`FIX_B` 就是 `Vehicle`，`:650`），**根因没查清** ⇒ 正本 §一·2 写了下一轮该从哪入手。

## D-⑤ 四问的答案
1. **根因分级**：见 D-④。强制攻击族里**没有一条是「缺受伤友方 / 缺已行动友方 / 缺垫场单位」**。
2. **最小夹具增量（两个位）覆盖**：受伤友方 **0 张**（本来就有）；已行动友方名义 **1～2 张**（且 `BL64` 仍卡在选目标）⇒ **收益 ≈ 0**。
3. **影响面**：若真改全池夹具 ⇒ **1128/1128 行读数变**、上界 12 行 `playRc` 可能翻、399 张「实得 0 条」的判决要重做、基线重建 **1 次**、`PinnedPlayMd5` 再变 **1 轮**。
4. **替代方案**：**A** 让 `ruleprobe` 装载 `ForcedAttackRefund` 表（影响面 = 12 张里成功的那 2 张：`EC46` 不变 / `GOF93` 变；基线重建 1 次）· **B** 扩 `play <id> preset` 两个旋钮 `chipfriend=` / `actedfriend=`（单卡专用、**不进基线**、零全池影响；但**按现证据暂时救不了任何一张**）· **C** 改全池夹具（不推荐）。

## D-⑥ 决策点（一句话，供你拍板）
> **不做全池夹具改动。** 先做 (a) 修 `cond=341 canUseDuty` 的 `ex` 语义（让 `BL64` 能打）＋ (b) 让 `ruleprobe` 装载 `ForcedAttackRefund` 表（消掉「一律退还」的失真）；专用 preset 旋钮**暂缓**。

## D-⑦ 本批边界
- ⛔ **没改任何代码**（`PlayProbe` 一个字节没动、`Core/**` 与 `RuleEngineTest.cs` 没碰、账主表没动、没跑 Unity、没动 git）。
- 唯一写盘：`资料/完全复刻/判据/主档夹具缺口_评估.md`（新文件）+ 本报告 + §B-④ 的就地纠错。
- 读过的数据：`cards_engine.json`（`unitConditions` / `abilityTriggers` / `chosenAbility`）· `基线/out_baseline.txt`（BL64 行）· `_tmp_view/e5eng/dump_after.tsv` · `play` 单卡读数（`BL64` / `GOF93` / `AM70` / `GOF86` / `SAU33` / `SW56` / `GOF93`）。

---

# 第五批 · (b) 探针装载退款表 ＋ (a) `cond=341` 语义核定（2026-10-27）

## E-(b) 探针现在**装载** `ForcedAttackRefund` 表 —— 已做（`工具/ruleprobe/Program.cs`）

**做法**（最省、且不多读一次文件）：`Program.LoadPool` 本来就在逐卡读 `cards_engine.json`，在**同一个循环**里把 `chosenAbility` 交给
`Core/ForcedAttackRefund.Install(id, int[])`（= 装载口，**判定规则只在那儿一处**，探针不抄第二份）。不引入 `Data/ForcedAttackRefundMap.cs`
（那半个 partial 走 `Resources.Load`，探针里是桩、恒 null）。**同时在读数区加了一行**，让装载一旦失效当场看得见：

```
ForcedAttackRefund: 灌入 1043 条（缺 `chosenAbility` 列 = 0 张）⇒ 实得 退还 9 / 不退还 3 / Unknown 1116
```
（期望值就是 **退还 9 / 不退还 3**：`{231,237,238}` 九张、`{230,233,235,236}` 三张，与 `cards_engine.json` 现读一致。）

### ① 两张卡的 `atkCount` 改前 / 改后（`play <id>` 的「打出去之后」那一行）

| 卡 | `chosenAbility` | 真值 | 改前 | **改后** |
|---|---|---|---|---|
| `EC46`（237） | 退还族 | 打后计数回 0 | `行动计数  ‖`（空） | **`行动计数  ‖`（空，不变）** ✅ 符合预期 |
| `GOF93`（235） | **不退还族** | 打后应留 1 | `行动计数  ‖`（空，被错退） | **`行动计数 P0[2]=1 ‖`** ✅ 变了 |
| 旁证 | — | — | `play GOF93` 日志有「Hunta Rig 重置行动：本回合攻击次数 **1 → 0**」 | **那行消失了** |

### ② 新指纹
```
public const string PinnedPlayMd5 = "0766fbe1f712696f4e57d2b7cfeb8f38";
```
- 取法同前：`play all pf <out>` 之后 `play check <out>`，② 哨兵那行把文件 md5 全串打出来（`PlayProbe.cs:3866`）。**常量我一个字没改**（仍是 `b671acdb…`），基线文件也没重生成。
- ⚠️ 又变了（第二批是 `79057e33…`）⇒ 同步点重钉时**以那时的现算值为准**（这轮之后若还有人改引擎/探针，md5 会再变）。

### ③ 改动的**影响面 = 恰好 1 张卡**
用两次 `play all pf` 逐卡逐列比（脚本见 §附）：

| 档 | 变化 |
|---|---|
| 主档 1128 | **1 张**：`GOF93`，且**只差 `atkCount` 一列**；`EC46` 等其余 1127 张**逐列全等** |
| 事件电池 586 | **0 张**（逐字节一致） |

（⚠️ 拿它跟**钉住的旧基线** `b671acdb` 比会是「1128 / 586 全变」—— 那是因为旧基线里**根本没有这两列**，不是行为变化。）
⇒ 这条修复是「**把探针口径对齐引擎**」，只落在真正受影响的那 1 张上。

### ④ 这条修完之后 `atkCount` 到底能不能当哨兵 —— **能，但只是「弱哨兵」，定位要写死**

| 它能抓什么 | 它能看见吗 |
|---|---|
| **不退还族（235/236，3 张）**该留 1 却没留 / 留错格 | ✅ 能（`GOF93` 就是实证） |
| **退还族（231/237/238，9 张）**该回 0 却没回（= 退还丢了 / 预扣没做） | ✅ 能 —— 会留下 `1` |
| **`!wasFree`（已行动过的单位被强制攻击成 `n→n`）** | ✅ **能**（退款族那一支 `wasFree=false` 时不退还 ⇒ 计数停在不 0 的值上）—— **前提是夹具真能造出这种局面**，现在造不出（见第四批） |
| **「强制攻击发生了没有」（退款族）** | ❌ **不能**，而且**永远不能** —— 退款族按设计就该回到 0。这一半由 `dmg` / `changesFull` 覆盖（`EC46 dmg=2` / `GOF93 dmg=15` 都已记到） |

⇒ **定位**：`atkCount` = 「**退款 / 行动额度记账**」的哨兵；⛔ 不是「攻击发生没发生」的读数。写进交接时请照这句写，别写成「它盯着强制攻击」。

## E-(a) `cond=341 canUseDuty` 的 `ex` 语义 —— **查清了，结论是【不改代码】**（并更正第四批的判断）

### 1. `ex` 到底是什么 —— 反编译原文
`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/FilterMethods.cs:776`
```csharp
public static bool MeetsConditionCriteria(CardScript card, UnitConditions conditionsFilter,
                                          bool excludeTrait, CardScript thisCard = null)
...
IL_0d2d:
if (excludeTrait && !flag2) goto IL_0c29;      // IL_0c29: return true;
object obj12 = (excludeTrait ? 1 : 0) ^ 1;
return ((obj12 & flag2) != 0);
```
⇒ `flag2` = 条件结果：**`ex=0` ⇒ 保留 iff 条件真；`ex=1` ⇒ 保留 iff 条件假（取反）**。消费者只有一处：
`FilterMethods.cs:541` `MeetsConditionCriteria(evaluatedCard, targetCriteria.conditionsFilter, targetCriteria.excludeCondition, thisCard)`
—— 在 `GetTargets` 的**逐候选**循环里 ⇒ 它确实是**目标池的筛**。**我们 `ApplyStructuredConditionFilter` 的取反方式与之同形。**

### 2. `341 canUseDuty` 是什么 —— 反编译原文
`d:/2/tools/decomp_full/FilterMethods__MeetsConditionCriteria.c:170-174`
```c
if (param_2 == 0x155) { bVar3 = *(char *)(param_1 + 0x4c) == '\0'; ... }
```
`+0x4C` = **`usedActiveAbility`**（`il2cpp_out/dump.cs` 的 EntityScript 块：`0x40 isPlayer` / `0x44 orderInPlay` / **`0x48 attacksThisTurn`** / **`0x4C usedActiveAbility`** —— `0x48` 与既有判据吻合，可反证偏移）。
写点：`CardScript.cs:14768`（`UseActiveAbility` 内，**且只在 `hasActiveAbility` 为真时**才置 `true`）。
⇒ **`canUseDuty` = `!usedActiveAbility`**。我们的 `ConditionEval.cs:550` `return !u.DutyUsed;`（同文件 `:547` 早就写着「B-18 已核 `+0x4c` 就是它」）**逐字同形**。

### 3. ⇒ 数据说的是什么（`cards_truth.json`，唯一一条 criteria）
```
BL64.abilities[0].logic[0].targetCriteria = { targetsAffected: 30, conditionsFilter: 341, excludeCondition: 1 }
```
`ex=1` ⇒ 保留 `canUseDuty == 假` 的单位 = **「本回合已经用掉主动能力」的单位**。
⇒ **原版语义下 `BL64` 的目标池就是「有主动能力且本回合已用过」的友方单位**，而夹具里 P0 的三张 `FIX_A/B/C` **都没有主动能力**（`hasActiveAbility=0`）⇒ 池空 ⇒ `ErrNoTargetAvailable`。

**对照实验（证明机器没问题）**：`BL50`（`cond=40 damaged, ex=0`）在默认夹具下同样 `ErrNoTargetAvailable`，加上
`preset chipenemy=1`（把敌人打成受伤）⇒ **打出去了**（`☠ 离场 P1[3] FIX_E1 2/0/0/2 → .`，实得 2 条）
⇒ `ex` 的取反、`cond=40`、`ApplyStructuredConditionFilter` **全都在正常工作**，`BL64` 是**孤例**（现池 `role=target & cond=341 & ex=1` **只有它 1 张**）。

### 4. 🔴 更正第四批的两句（铁律 5）
| 第四批原话 | 更正 |
|---|---|
| 「`cond=341` 的 **`ex` 语义 / 判据错**（`Case 341` 对正常单位恒真 ⇒ 池空）⇒ **引擎判据问题**」 | **不是代码错**：`ex` 的取反、`Case 341` 的实现**都与原版逐字同形** ⇒ **⛔ 不要改 `ConditionEval.cs`**（改了反而与反编译不符）。 |
| 「`BL64` 打不出去 = 引擎问题，不是夹具」 | 反过来了：**是夹具缺口**，但缺的不是「受伤的友方」，而是 **「一个有主动能力、且本回合已经用掉它的友方单位」**（1 张卡的收益）。 |

⚠️ 仍存的一个上游疑点（**没查清，需上游核**）：`BL64` 这条数据（`excludeCondition=1` + `canUseDuty`）**读起来反直觉**（「只能选已经用过 Duty 的单位」）。
两种可能：① 原版就是这样（那这张卡本来就极难打，我们的行为**是对的**，`ErrNoTargetAvailable` 是**夹具没有这种单位**）；
② `cards_truth.json` 抽取时把 `excludeCondition` 取错了（那一列归 `工具/gen_cards_engine.py` + SO 解包链，**不在本波白名单**）。
⇒ 建议下一轮**从 SO 侧核一次**（`d:/2` 那 1351 张 SO 里 `BL64` 的 `targetCriteria`），核完再决定要不要报数据缺陷。**本轮不动代码**。

## E-⑤ 本批改动 & 边界
- 代码：`工具/ruleprobe/Program.cs` —— ① `LoadPool` 自带 `Install` 灌表（+17 行，含判据注释）② 读数区加一行 `ForcedAttackRefund: …`（+15 行）。**没碰** `RuleEngine/Core/**`（`ConditionEval.cs` 一个字节没改）、`RuleEngineTest.cs`、`CardPresentation/**`、正本、账主表、`Pinned*` 常量。
- 验证：`ruleprobe.sh check` ⇒ **哨兵失败 0 条 / rc=0**；`play all dump` / `play all pf` 均 rc=0；`play check` 现算 = `0766fbe1…`（与钉住的 `b671acdb…` 不同 —— **预期**，等你重钉）。
- ⛔ 没跑 Unity（`ruleprobe` 是纯 dotnet，不占 Unity 实例）、没动 git。
- 复现用的两份指纹/落盘件：`_tmp_view/e5eng/pf_new.txt`（第二批）/ `pf_b5.txt`（本批）/ `dump_b5.tsv`（都在 gitignore 目录，可删）。

---

# 第六批 · 10 条断言订正（`RuleEngineTest*.cs` · 2026-10-27）

照 `报告/B9_D2RE_RuleEngineTest10红定性.md`（10 条**全 α**）逐条改**断言**，**实现一个字没动**。

## F-① 根因 A（8 条）：`BroadcastCardDrawn` 当前回合方手牌**响两遍** ⇒ 期望 `+1 → +2`

| # | 改后落点 | 改了什么 |
|---|---|---|
| 1 | `RuleEngineTest.cs:3819` | `n0 + 1` → **`n0 + 2`**；文案「多了一条」→「多了**两条**」+ 写明「照抄原版漏 `!`，⛔ 别改回 +1」；同段历史注释 `:3811` 补「`n0` 现在是 **2**」的原因 |
| 2 | `:3848` | `2 + (n0 + 1)` → **`2 + (n0 + 2)`**（两处）+ 注释同步 |
| 4 | `:30808` | `1` → **`2`**（④-b 前提） |
| 5 | `:30811` | `2` → **`4`**；🧨 方向按报告改成「早退留在棋盘块之前 ⇒ **两趟一起被掐 ⇒ 实得 0**」（原写「实得 1」是单跳时代的判据） |
| 6 | `:30887` | `n0` 期望 `1` → **`2`** |
| 7 | `:30889` | `n0 + 1` → **`n0 + 2`** |
| 8 | `:30918` | `1` → **`2`**（⑤ 对照） |
| 9 | `:32541` | `1` → **`2`**；并把下游 `:32563` 的 `>= 2` 改成 **`>= before2 + 2`**（`before2` = 回合那一抽的两跳）——
  原来那句 🧨「实得 1 ⇒ 红」**本身已不成立**（机制坏掉时计数停在 2，`>= 2` 仍绿）；改成相对增量后 🧨 才为真 |

🔴 **按报告要求没动的东西**：`:30845` 的 **④-c**（量棋盘那一跳、只响 1 次 —— 这族唯一不靠双跳数字的判别式）· `DrawHandHopTakes` / `hops`（照抄原版 bug 的正确实现）。
✅ 另核过**不该受影响的**：`:3786`（W3，`play` 事件 `drawMode=false`）· `:3960` / `:3991`（W6/W8，`prays` / `darkpact` 触发，不是 draw 事件）—— 与报告结论一致，均未改。

## F-② 根因 B（1 条 #3）：观测式重写（**假绿通道**比过期更值得记）
`:6827-6836`：原断言是「手牌数**或**血量变了」—— 打出战术卡本身就让手牌 −1（`PlayTacticCore` 的 `Hand.RemoveAt`）⇒ **「一个选项都没结算」时它照样绿**（假绿通道）；P-04 换随机源后挑到「Draw a card」把 −1 抵消 ⇒ 反倒报红（假红）。
⇒ 改成**钉住选项 + 逐项断言**（报告方案 A）：
```csharp
ctx.ChoosePicks.Enqueue(1);                       // 钉住第 2 项「Heal 1 to your Warlord」（0 基；DoChooseOne 依次出队）
Check(wl.Health, hp0 + 1, "★ 钉住的那一项真的结算了（血量 +1）");
Check(ctx.Players[0].Hand.Count, hand0 - 1, "★ 反例（灭自证）：另外两项都没跑 ⇒ 手牌只剩「打出战术卡 −1」");
```
（`ChoosePicks` = `public readonly Queue<int>`，`BattleContext.cs:1457`；既有先例 `RuleEngineTest.cs:13273`。）

## F-③ 根因 C（1 条 #10）：卡表 v32 → **v34**
`RuleEngineTest_EngineD4.cs:99` `Check(dto.version, 34, …)`：文案改成「v34 = 加 `cardType` 一列 · v33 = 加 `enchantTrait` 一列」，补两行历史注释（照 `E7ENG` 的 `gen_cards_engine.py:5572`/`:5580`），🧨「改回 31」→「**改回 33**」。

## F-④ 顺带修的过期文案（报告 §四·3）
- `RuleEngineTest.cs` ⑤ 那段的 🧨：「换回 `Draw` ⇒ 实得 3」→ **「实得 6」**（起手 3 张 × 响两遍）。
- `:32563` 的 🧨：见 #9（改成相对增量，原理由已不成立）。
- `:3811` 的 `n0` 历史注：5 → 1 → **2**。

## F-⑤ 诊断入口过期（报告 §四·1）
`PYTHONIOENCODING=utf-8 python -I -X utf8 Unity/工具/gen_diag_runner.py` ⇒ **`Run()` 用例表 357 条 · 用例方法 357 个**（修前 356 vs 357，缺的就是 `TestB9EngForcedAttackAndShuffleGate`）· 重写 `RuleEngineTest_Diag.cs`。

## F-⑥ 验证 & 行数
- `TMPDIR=/tmp/wf_E5ENG bash Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- 行数：`RuleEngineTest.cs` **38521 → 38533（+12）**（行尾仍 **CRLF / 裸 CR 0**）· `RuleEngineTest_EngineD4.cs` **+4**（纯 LF，原样）· `RuleEngineTest_Diag.cs` 重生成（+1，纯 LF）。
- ⚠️ **账里 11 处 `RuleEngineTest.cs:NNNN` 散文引用**：本批之前的「+3」仍然成立，本批又在 `:3820/6830/30886/32541` 一带**再 +12**（全部落在 `:3811` 之后）。
  受影响的是 `35835→`**`35850`**（含上批 +3 共 +15）；其余十条都在 `:3811` 之前 ⇒ 只吃上批的 +3。`--fix` 修不了散文里的行号，得手工/批量改。
- ⛔ 没跑 Unity（`RuleEngineTest.Run` 你统一复跑）· 没动 git · 没改正本与账主表 · 没动任何实现文件。

---

# 第七批 · NetBattle 夹具口径（`CardPresentation/Editor/NetBattleTest.cs` · 2026-10-27）

照 `报告/B9_D4NT_NetBattle重放不确定.md` 改**夹具**，生产代码一个字没动。

## G-① 改了什么（两处）

**1) 取动作：从「取第一条」改成「整表候选」**
`PickAction(ctx)` → **`PickActions(ctx)`**（返回 `List<AiAction>`，`EndTurn` 仍不进候选）。原实现 `foreach … return a;` 只给第一条。

**2) 循环体：取一条 → 应用 → **不是 `OK` 就换下一条**；被拒的那条**不广播****（`:395-414`）
```csharp
foreach (var cand in PickActions(ctx))
{
    var w = NetProtocol.ToWire(cand, ctx, 0);
    w.actor = nb.MySeat;
    nb.CaptureLocalAnswers(ctx, cand);
    int rc = NetApply.Apply(ctx, w, nb.MySeat);
    if (rc == RuleCodes.OK) { act = cand; code = rc; break; }
    Debug.Log(P + " 本机这条被拒了（换下一条）：" + RuleCodes.Describe(rc) + " " + cand);
}
if (act == null) { /* 全部被拒 ⇒ 退回 EndTurn（恒可执行）；它也被拒才 Fail+break */ }
nb.OnLocalAction(act);      // 🔴 只有 OK 才走到这里 —— 生产口径 = BattleDriver.cs:308
```
- **为什么必须同时改两处**：只改「被拒就不广播」，`PickActions[0]` 会一直是那条付不起的誓约 ⇒ 原地打转直到 `guard 400`；只改「换下一条」而不改广播，被拒动作仍会把对端打成「打岔判负」。
- **`EndTurn` 兜底**：所有候选都被拒时不空转、不误判 —— 收手是引擎里恒合法的那条（原 `PickAction` 在空表时也是这么兜的，行为一致）。
- ⚠️ **一条前提，如实标着**：换下一条时**没有**在试之前做「这条合不合法」的预判（引擎没有统一的可应用性谓词），而是**直接 `Apply` 再看返回码**。这依赖「被拒的 `Apply` 是零副作用」（本仓引擎的惯例：`DeclareAttack` 一类都是**先校验后改动**）。
  若哪天某条动作**先改了状态再返非 OK**，本轮的「每步抽一次两端 `Fingerprint`」（`:422`）会当场报分叉 —— 不会静默。⛔ 别为了省这一步把 `Apply` 换成「只问不做」的伪检查（没有那个口）。

## G-② 步数（⚠️ 我没法验，要你复跑）
`RuleEngineTest` 那种「跑 Unity」我这边跑不了 ⇒ **步数从 9 回到多少，必须由你复跑 `NetBattleTest.Run` 确认**。
预期：回到本批洗牌换源之前的量级（报告写「昨天 37 步」，你要求 **20+**）—— 因为第 9 步不再污染对端 `Winner`，循环会继续往下走。
若复跑后仍卡在个位数，**第一嫌疑不是这次改动**，而是「`PickActions` 里每一条都被拒」（日志里会有成排的「本机这条被拒了（换下一条）」）——那时要看的是**枚举出来的动作为什么全都不可应用**。

## G-③ 顺带两条（⛔ 我没动，只写判据 —— 跨面，需单独立账）

### 1. `ErrUnimplemented` 被拿去当「付不起 / 没生效」
`RuleEngine/Core/RuleCore.cs:11717-11723`（`ActivateOathAbility` 那一跳）：
```csharp
bool ok = ResolveOathAbility(ctx, p, u);
if (!ok) { ctx.Log("（这次激活没有生效 —— 次数没有消耗）"); return RuleCodes.ErrUnimplemented; }
```
`ResolveOathAbility` 返 false 的成因**包括「付不起誓约费」和「条件不成立」**（同段注释自己写着「付不起 / 条件不成立…」）
⇒ 玩家/AI 看到的是「**该功能本版未实现**」（`RuleCodes.Describe`），而真实原因是**钱不够**。
🔴 这正是这次最容易走偏的地方：夹具/诊断会把「没钱」当成「未实现」去追错方向（`D4NT` 报告第一步就差点这样）。
**建议**：加一个语义正确的码（如 `ErrNotEnoughResource` / 复用已有的 `ErrCost` 一族），`ResolveOathAbility` 的失败原因再细分。
⛔ 跨面（`RuleCore.cs` 不在我这波白名单）⇒ **只立判据，不动代码**。判据链：`RuleCore.cs:11717-11723` + `ResolveOathAbility` 的失败支 + `RuleCodes.Describe(ErrUnimplemented)` 的文案。

### 2. `AiScoring.ActiveAbilityManaCost` 默认 `null` ⇒ AI **没有**「这个誓约付不起」的信号
`RuleEngine/Data/AiScoring.cs:1120`（`public static Func<CardDef,int> ActiveAbilityManaCost;`）—— 自陈「那份数据列还没落进 `cards_engine.json`」。
判据落点 `AbilityManaCostInRange`（同文件 `:1127` 起）：`unknown`（钩子 null / 判不出）⇒ **`return false`**（= 「不在范围内」）。
⇒ `ScoreFromManaBuff` 的 ⑥⑦ 两段**恒 0** ⇒ **打分里根本没有「这个主动技付不起」这一项**。
⚠️ 措辞要准：**不是「AI 判成付得起」，而是「AI 完全没有这个信号」**。
后果：真 AI（`SimpleAI`）也可能反复挑到付不起的誓约 —— 与本轮夹具踩的是**同一个坑的另一侧**（夹具侧已用「换下一条」绕开）。
**建议**：把 `manaCost` 落进 `cards_engine.json`（`gen_cards_engine.py` + `CardDef` 两处，`AiScoring.cs` 的 doc 已写明），再给 `ActiveAbilityManaCost` 接上。⛔ 跨面 ⇒ 只写判据。

## G-④ 边界 & 行数
- 改动文件：**只有** `CardPresentation/Editor/NetBattleTest.cs`（+42 / −12，净 **+30** 行；纯 **LF**、裸 CR 0）。
- ⛔ 没碰生产代码（`NetBattle.cs` / `BattleDriver.cs` / `RuleEngine/**` 一个字没动）、没跑 Unity、没动 git、没改正本与账主表。
- `typecheck` ⇒ **运行时 0 / 编辑器 0**。账里**没有**任何 `NetBattleTest.cs:NNNN` 引用 ⇒ 无行号漂移要跟。

---

# 第八批 · `ctx.Events` 污染（刀 1 · 夹具侧 · 2026-10-27）

照 `报告/B9_D6NB_NetBattle第9步分叉.md`：**第 9 步那 5 条红不是夹具改错**，是**被拒动作在「拒」的同时往 `ctx.Events` 写了 4 行诊断日志**，
而被拒又不广播 ⇒ 主机 55 / 客机 51 ⇒ `Fingerprint` 含 `Events.Count` ⇒ 差 `4 × 31² = 3844`（两端 `PlayerHash` 其实完全相同）。

## H-① 改了什么（`Editor/NetBattleTest.cs`，候选循环内，+12 行）
```csharp
int evBefore = ctx.Events.Count;                     // ← 候选循环外记一次
foreach (var cand in PickActions(ctx))
{
    … int rc = NetApply.Apply(ctx, w, nb.MySeat);
    if (rc == RuleCodes.OK) { act = cand; code = rc; break; }
    if (ctx.Events.Count > evBefore)                 // ← 失败就把这次（含之前几次失败）追加的行截掉
        ctx.Events.RemoveRange(evBefore, ctx.Events.Count - evBefore);
    Debug.Log(P + " 本机这条被拒了（换下一条）：" + …);
}
```
- 🔴 **只截 `Events`、⛔ 不截 `Signals`**（按你的要求）：`Signals` 是表现层 `PlaySignals → DrainSignals` 要抽走的（`BattleDriver.cs:19302-19305`），
  `Events` 才是诊断流 ⇒ 截错会把演示/高亮吞掉。
- 🔴 **⛔ 没有另写「余额够不够」的预筛**：判据只许在引擎里一份（两处写同一条规则 = 迟早不一致）。
- ✅ **索引截断是安全的、有据可查**：`Events` 只在**超过 `EventCapacity = 2000`** 时才从头部裁（`BattleContext.cs:620 / :1858`），
  而这个夹具全程 `Events.Count ≈ 55` ⇒ 试的这几次里**不可能触发头部裁剪** ⇒ `evBefore` 不会失效。
- ⚠️ **D6NB 指出的下一个坑（本波刻意不处理，记在这里）**：「先试后拒」是在**活的 ctx** 上做的 ⇒
  被拒那一试仍可能留下**不可回滚**的副作用（`LastAttackType` / `CurrentAttackType` / `Auras.Recompose` 重挂）。
  本波只截 `Events`；**若以后 `PickActions` 里出现「试了会改局面」的候选**，得改成「先在快照/克隆上试」或
  「给引擎加一个统一的 `CanApply` 谓词」——那属于引擎面，另立账。

## H-② 顺带（⛔ 没改，只写判据）：`NetProtocol.cs:571-575` 对 `ctx.Events` 的说明**是错的**
原文：「`ctx.Events` 是**待播事件队列**，表现层（`PlaySignals` / `DropSignals`）随时在抽干它」。
🔴 **事实**：
- `PlaySignals`（`CardPresentation/Battle/BattleDriver.cs:19300-19308`）抽的是 **`Ctx.Signals`**（`Ctx.DrainSignals(_signalBuf)`），**不是** `ctx.Events`；
- `ctx.Events` 是 `List<string>` 的**诊断文本流**，全仓**唯一**的裁剪是「超过 `EventCapacity` 从头部裁」（`BattleContext.cs:620 / :1858`）—— **从不清空**；
- `Signals` 才是「搬完即空、不用记游标」的那个（`BattleContext.cs:411-413`）。
⇒ **结论（`StateHash` 不含 `Events` ⇒ 本地录像用它）仍然成立**，但**理由要改**：
  不是因为「Events 会被表现层抽干」，而是因为 **`Events` 是纯诊断文本、与局面无关**（而且它会把「被拒动作写了日志」这种**非局面**差异也算进 `Fingerprint`）。
  另：`DropSignals` 这个名字在 `NetProtocol.cs` 那句里也一并提到 —— 同属「抽的是 Signals」那一族，措辞同样要改。
⛔ 跨面（`CardPresentation/Net/**` 不在本波白名单）⇒ 只写判据。

## H-③ 边界 & 行数
- 改动文件：**只有** `CardPresentation/Editor/NetBattleTest.cs`（累计 **+54 / −12**，本批 +12；纯 **LF** / 裸 CR 0）。
- ⛔ 没碰 `NetProtocol.cs` / `NetBattle.cs` / `BattleDriver.cs` / `RuleEngine/**`；没跑 Unity、没动 git、没改正本与账主表。
- `typecheck` = **0 / 0**。**第 9 步那段能不能真的走完，要你复跑 `NetBattleTest.Run`**（我这边跑不了 Unity）。

---

# 第九批 · 余额闸断言（4 条 · 2026-10-27）

那 4 条红是 `E6ENG` 余额闸的**预期后果**（(α) 断言过期）：实得 `[2] = ErrCost` = **判定阶段就拦**，不再走到结算层才失败。

## I-① 两处期望码 `13 → 2`（并写死判据，防以后按老口径改回去）
| 落点 | 改动 |
|---|---|
| `RuleEngineTest.cs:17501`（⑤ 付不起 ⇒ 不生效） | `ErrUnimplemented` → **`ErrCost`** + 注释块 |
| `RuleEngineTest_S14.cs:1680`（`A1412` 反控） | 同上（`CanUseOathAbility` / `UseOathAbility` **两条都断**） |
| `RuleEngineTest_S23_Assert.cs:613`（`A1413②` 反控） | 原来只断 `!= OK`、文案写「实测 rc = 13 = `ErrUnimplemented`」⇒ **收紧成 `CheckCode(…, ErrCost, …)`**（那条「码值另立账」现在结掉了） |

三处注释里统一写死判据（照 `AbilityCostGate` 的 doc 逐字）：
> **原版 `PlayerManager__HasEnoughMana.c` 读该组第一个 `ObscuredInt` = `unusedMana(+0x4C)`，`return cost <= 那个量`**
> ⇒「**未用余额 ≥ 费用**」；⛔ **不是「能量 − 已预留」**（原版根本没有「预留」这一层）。

## I-② 两处「出声」断言：**我先读了新路径，结论是「新路径没有日志」**（⛔ 不是把它的话改掉）
逐字核过三个函数：`CanUseOathAbility`（`RuleCore.cs:11657-11669`）· `AbilityCostGate`（`:15038-15049`）· `CostPrefixPayable`（`EffectResolver.cs:760-771`）——**三处都没有 `ctx.Log` / `Debug.Log`**。
⇒ 旧的 `HasLog(ctx, "一并跳过")` 断的是**结算层**那句（`EffectResolver.cs:727`），而闸在 `Can*` 阶段就拒 ⇒ **根本走不到那儿**。
**为什么闸不该写日志**（这两条我写进了断言注释）：
1. 闸在**还没有任何效果「开始过」**的地方拒 ⇒ 说「其余效果一并跳过」是**假话**；
2. 写 `ctx.Log` 会往 **`ctx.Events`** 加行，而 `NetProtocol.Fingerprint` **把 `Events.Count` 算进去** ⇒ 一次「本地被拒」就让联机两端指纹差 N 行（`D6NB` 实测 55 vs 51 就是这么来的）。
⇒ **「不静默」的载体换成两件可断的东西**（S14 / S23 两处都这么改）：
```csharp
CheckTrue(RuleCodes.Describe(RuleCodes.ErrCost) != RuleCodes.Describe(RuleCodes.ErrUnimplemented),
          "★ 不静默：拒的载体是**可区分**的返回码（ErrCost =「能量不足」，不再是「本版打不出去」）");
Check(ctx.Events.Count, evBefore, "★ 一行诊断都不写 —— 写了会让 Fingerprint 两端分叉（D6NB）");
```
并把「玩家可见的那一声在**表现层** `BattleDriver` 的 `code != OK` 分支（`Debug.Log(…这一手打不出去…)` + tip）」写进注释 —— 本宿主够不到那一层，如实标着。

## I-③ 持久夹具：其实**已经落地**在 `A1412` 那一块（`RuleEngineTest_S14.cs:1673` 起）
现在它是一条完整的「**付不起**」夹具（不再只是「改个期望码」）：
- **判定阶段**：`CheckCode(CanUseOathAbility(ctx,0,3), ErrCost, …)` —— 直接断「先判钱、再放行」那一跳；
- **结算入口**：`CheckCode(UseOathAbility(…), ErrCost, …)`；
- **零副作用**：能量不扣 · `OathUsesThisTurn` 不涨 · `!Has("flank")`（`CostShared` 后半句也不许落地 = `A1412` 的原判别式）；
- **诊断流不增**：`ctx.Events.Count` 前后相等（= 联机安全）。
- **正控**在 `RuleEngineTest_S23_Assert.cs:595-607`：余额 **刚好 = 2** ⇒ `OK` + 真扣 2 点 + `flank` 落地 + 次数记 1（与反控成对，防「一律拒绝」也全绿）。

⚠️ **没做的一件（如实）**：`Ability:`（`CanStartAbility` 那条）**没有独立夹具** —— 全仓**没有**一张「带付费前缀的 `Ability:`」夹具卡，而卡面语法我**没法在跑不了测试的情况下确认**（写一个猜的语法 ⇒ 可能是一条我诊断不了的红）。
**做法（给你派活时用）**：从 `cards_engine.json` 里挑一张**真卡**（desc 形如 `N: …` 且 `keyword` 含 `Ability`），`Place` 上场、能量压到 `N-1`，然后断 `CanStartAbility == ErrCost` —— 它和誓约**共用同一个 `AbilityCostGate`**（`RuleCore.cs:11077` / `:11667` 两个调用点，判据一份）。

## I-④ 顺带（⛔ 没改，只报）：`BattleDriver.cs:306` 的注释**已经过期**
原文：「③ 引擎在「拒」的同时会 `ctx.Log(...)` 写诊断 —— 实测 `RuleCore.UseOathAbility` 那条路 **4 行**」。
余额闸落地后**一行都不写了**（见 I-②）⇒ 那句失效；而且**那 4 行正是 `D6NB` 里 `Events` 差 4 的来源** ——
⇒ 这次闸的落地**顺带把那个污染的源头去掉了**（第八批夹具侧的 `RemoveRange` 从「兜底」变成「双保险」）。
该文件不在本波白名单 ⇒ 只记，未改。

## I-⑤ 边界 & 行数
- 改动文件：`RuleEngine/Editor/` 下 **3 个**：`RuleEngineTest.cs`（本批 **+7**，累计 38533→**38540**，CRLF 保持）· `RuleEngineTest_S14.cs`（**+20**，纯 LF）· `RuleEngineTest_S23_Assert.cs`（**+11**，纯 LF）。
- ⛔ 没碰 `RuleEngine/Core/**`（闸的实现一个字没动）、`CardPresentation/**`（含 `BattleDriver` 那句过期注释）、正本、账主表；没跑 Unity、没动 git。
- `typecheck` = **0 / 0**。
- ⚠️ 账里 11 处 `RuleEngineTest.cs:NNNN` 散文引用：本批再 **+7**（全落在 `:17500` 之后）⇒ 那条唯一在末尾的 `35835` 现应 **`35857`**（+3+12+7）；其余十条按各自之前的偏移。`S14` / `S23` 两个文件**没有**账引用。
