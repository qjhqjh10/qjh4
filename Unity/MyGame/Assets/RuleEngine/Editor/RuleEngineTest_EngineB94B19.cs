// RuleEngineTest_EngineB94B19.cs — 第九十四会话 · 波 4 · 执行代理 `B94_B19`
//
// 本件要回答的问题：**教程白名单闸（原版 `BattleManager.CheckIfPlayerActionPermittedInTutorial`）
// 今天到底接没接上？** —— `报告/B94_B14.md` §五·1 说「`TutorialScript.PermitsPlayerAction` 全仓
// 0 调用点 ⇒ 整条教学白名单闸一次都没生效」。本件回源核完，**那句不成立**：
//   · 唯一生产入口 = `BattleDriver.TutorialPermits`（`CardPresentation/Battle/BattleDriver.cs:18659-18664`）；
//   · 它下面有 **10 处生产调用点**（:5661 / :14002 / :15179 / :15231 / :15237 / :15632 / :15974 /
//     :16342 / :16668 / :16749），原版那 14 处**逐处有对位**（见报告 §二 那张表）；
//   · `B94_B14` 之所以读到 0，是**搜索范围错**（只搜了 `RuleEngine/`）—— 同 `CLAUDE.md` §三
//     那条「判「某条链没有」⛔ 别只 grep 一个目录」。
//
// ⇒ 本件**不改生产代码**，只把「六步判据」补成**引擎级断言**（宿主那批断言在
//   `CardPresentation/Editor/BattleScene.cs` 的 ④ 节，是「整场 + 真驱动」的集成口径；
//   这里补的是**纯谓词的分支矩阵**，尤其是**非教学局恒放行**那一支）。
//
// 判据（第一权威 = `d:/2/tools/decomp_full/`，2026-10-29 逐句亲读）：
//   ① `BattleManager__CheckIfPlayerActionPermittedInTutorial.c`（六步）
//   ② `ScriptedActionData__CheckIfMatchesActionData.c`（动作大类 / 发起者 / 目标三关）
//   ③ `BattleManager__IsValidSpellTarget.c:21-25` · `BattleManager__CanPlayCard.c:49-63` 等 14 个调用点
//      （每一处都先 `IsTutorialMatch` ⇒ **非教程局一次都不问**）
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using RuleEngine;
using CardPresentation;      // `BattleDriver`（「非教学局恒放行」那一支在它里面）
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里那一个块调；⛔ 不用 `Test`/`Report` 打头）。</summary>
    public static void CheckB94B19TutorialGate()
    {
        Section("🆕 B94_B19：教程白名单闸 `CheckIfPlayerActionPermittedInTutorial` —— 六步逐句 + 非教学局恒放行");
        B94B19T1PointerGuards();
        B94B19T2CurrentActionGate();
        B94B19T3ActionTypeMatrix();
        B94B19T4ActingAndTargetGates();
        B94B19T5PlaySlotSentinel();
        B94B19T6DriverNonTutorialAlwaysPermits();
    }

    // ==================================================================
    //  夹具（全部合成、⛔ 不依赖 `tutorial_stages.json` 的现网数据）
    // ==================================================================

    /// <summary>造一关：**只填 `turns`** —— 闸门一个字段都不读别的。</summary>
    static TutorialStageData B94B19Stage(params TutorialTurn[] turns)
    {
        return new TutorialStageData { stage = 9419, so = "<B94_B19 自检合成>", turns = turns };
    }

    static TutorialTurn B94B19Turn(bool scriptedTurn, params TutorialAction[] actions)
    {
        return new TutorialTurn { turnName = "t", scriptedTurn = scriptedTurn, actions = actions };
    }

    static TutorialAction B94B19Act(bool playerAction, params TutorialActionData[] data)
    {
        return new TutorialAction { playerAction = playerAction, data = data };
    }

    /// <summary>一条 `scriptedActionData`；`actingUnit`/`targetUnit` 给 `0` ⇒ 那一格**留 null**
    /// （原版 `op_Inequality(null)` 为假 ⇒ 那一关不判）。</summary>
    static TutorialActionData B94B19Data(ScriptedActionType type,
                                        int actingUnit = 0, string actingId = null,
                                        int targetUnit = 0, string targetId = null)
    {
        var d = new TutorialActionData { type = (int)type };
        if (actingUnit != 0) d.acting = new TutorialUnitRef { unitType = actingUnit, ourId = actingId ?? "" };
        if (targetUnit != 0) d.target = new TutorialUnitRef { unitType = targetUnit, ourId = targetId ?? "" };
        return d;
    }

    /// <summary>「动作大类」那一关的单独口径：`acting`/`target` 都留 null ⇒ 后两关不判。</summary>
    static bool B94B19M(ScriptedActionType t, BattleActionType a)
    {
        return TutorialScript.Matches(B94B19Data(t), new TutorialAttempt { Action = a });
    }

    // ==================================================================
    //  T1 —— 指针三道守卫：① `Turn<1` ② 本回合没脚本 ③ 指针到末尾
    // ==================================================================

    static void B94B19T1PointerGuards()
    {
        // 判据：`BattleManager__CheckIfPlayerActionPermittedInTutorial.c`
        //   ① `:27-34`（`ObscuredInt @+0x3F8`（= `turnCounter`，1 起）`< 1` ⇒ `ret 0`）
        //   ② `:36-38`（`GetCurrentTurnScriptedData` 返 null ⇒ 落到 `:…74e` 的 `ret 1`）
        //   ③ `:39-40`（`actionCounter >= turn.actions.Count` ⇒ 落到 `ret 1`）
        var match = B94B19Data(ScriptedActionType.Attack, (int)ScriptedActionUnit.PlayerWarlord, null,
                               (int)ScriptedActionUnit.AiWarlord);
        var attOk = new TutorialAttempt
        {
            Action = BattleActionType.attack, ActingIsHero = true, ActingIsPlayerSide = true,
            TargetIsHero = true, TargetIsPlayerSide = false,
        };

        // ---- ① `Turn < 1`：**哪怕动作完全对得上**也拒 ----
        var g0 = new TutorialScript(B94B19Stage(B94B19Turn(true, B94B19Act(true, match))));
        CheckTrue(g0.Turn == 0, "（前提）新执行器的 `Turn` = 0（还没 `UpdateTurn`）");
        CheckTrue(!g0.PermitsPlayerAction(attOk),
            "★ **① `turnCounter < 1` ⇒ 拒**：动作本身对得上也不许（原版 `:32-33` 的 `if (iVar4 < 1) ret 0`）"
          + "｜🧨 删掉 `PermitsPlayerAction` 第一句 `if (Turn < 1) return false;` ⇒ 本条红"
          + "（`Turn=0` 时 `CurrentTurn` 返回 null ⇒ 会走 ② 的 `return true`）");
        g0.UpdateTurn(1);
        CheckTrue(g0.Turn == 1 && g0.ActionCounter == 0, "（前提）`UpdateTurn(1)` ⇒ `Turn=1 · 指针=0`");
        CheckTrue(g0.PermitsPlayerAction(attOk),
            "★ **同一个 `TutorialAttempt`、只把 `Turn` 改成 1 ⇒ 放行**（判别式：与上一条只差这一格）"
          + "｜🧨 把 ① 写成「恒 false」⇒ 本条红");
        CheckTrue(!g0.PermitsPlayerAction(new TutorialAttempt { Action = BattleActionType.playCardFromHand }),
            "★ **⑥ 一条都不中 ⇒ 拒**：当前那条只写 `Attack`，动作换成 `playCardFromHand` 就不认"
          + "｜🧨 把 `Matches` 里那条动作大类分支删掉（或恒 true）⇒ 本条红");

        // ---- ② 本回合没有脚本 ⇒ 放行（两个成因都要走一遍）----
        var gOff = new TutorialScript(B94B19Stage(
            B94B19Turn(false, B94B19Act(false, B94B19Data(ScriptedActionType.SmallTip)))));
        gOff.UpdateTurn(1);
        CheckTrue(gOff.CurrentTurn == null,
            "（前提）`scriptedTurn=false` ⇒ `CurrentTurn == null`（原版 `GetCurrentTurnScriptedData` 也要求 `+0x18 != 0`）");
        CheckTrue(gOff.PermitsPlayerAction(attOk),
            "★ **② 本回合没有脚本 ⇒ 放行**（原版那一支落到 `ret 1`）"
          + "｜🧨 把 `CurrentTurn` 里 `t.scriptedTurn` 那个判据删掉 ⇒ 本条红（会去比那条 `SmallTip` 的 ④）");
        var gNoTurns = new TutorialScript(B94B19Stage());
        gNoTurns.UpdateTurn(1);
        CheckTrue(gNoTurns.PermitsPlayerAction(attOk), "★ **② `turns` 整份为空同样放行**");

        // ---- ③ 指针跑到本回合末尾 ⇒ 放行 ----
        var gDone = new TutorialScript(B94B19Stage(
            B94B19Turn(true, B94B19Act(false, B94B19Data(ScriptedActionType.SmallTip)))));
        gDone.UpdateTurn(1);
        CheckTrue(!gDone.PermitsPlayerAction(attOk),
            "（前提）指针 0 时当前是「脚本自己执行」的 `SmallTip`（`playerAction=false`）⇒ ④ 把玩家的动作**全挡**");
        gDone.AdvancePlayerAction();                 // 原版 `AiScripted.PlayerChoiceAction` = 指针 +1
        CheckTrue(gDone.ActionCounter == 1, $"（前提）指针推到 1（= 本回合那 1 条之后），实得 {gDone.ActionCounter}");
        CheckTrue(gDone.PermitsPlayerAction(attOk),
            "★ **③ `actionCounter >= 本回合动作数` ⇒ 放行**（本回合引导跑完了，玩家自由行动）"
          + "｜🧨 删掉 `if (ActionCounter >= t.actions.Length) return true;` ⇒ 越界/本条红");
    }

    // ==================================================================
    //  T2 —— 当前那条：④ `playerAction=false` 的闸 · ⑤ `PlayerChoice`/`ActivateHandCards` · ⑥ 全不中
    // ==================================================================

    static void B94B19T2CurrentActionGate()
    {
        // 判据：`CheckIfPlayerActionPermittedInTutorial.c:41-53`（④）· `:55-70`（⑤）· `:71`（⑥ `ret 0`）
        var match = B94B19Data(ScriptedActionType.Attack, (int)ScriptedActionUnit.PlayerWarlord, null,
                               (int)ScriptedActionUnit.AiWarlord);
        var attOk = new TutorialAttempt
        {
            Action = BattleActionType.attack, ActingIsHero = true, ActingIsPlayerSide = true,
            TargetIsHero = true, TargetIsPlayerSide = false,
        };

        // ---- ④ 当前那条 `playerAction=false` 且不是 `ActivateHandCards` ⇒ 拒（**先于 ⑤**）----
        var g = new TutorialScript(B94B19Stage(B94B19Turn(true, B94B19Act(false, match))));
        g.UpdateTurn(1);
        CheckTrue(!g.PermitsPlayerAction(attOk),
            "★ **④ 当前那条 `playerAction == false` ⇒ 拒** —— `data` 里明明写着这一刀也不许（④ 在 ⑤ 之前）"
          + "｜🧨 删掉 `if (!cur.playerAction && !cur.IsActivateHandCards) return false;` ⇒ 本条红（会走 ⑤ 命中）");

        // ---- ④ 的例外：`ActivateHandCards(170)` 那一档 ⇒ ④ 不拦，⑤ 的 170 直接放行**任意**动作 ----
        var gAct = new TutorialScript(B94B19Stage(
            B94B19Turn(true, B94B19Act(false, B94B19Data(ScriptedActionType.ActivateHandCards)))));
        gAct.UpdateTurn(1);
        CheckTrue(gAct.PermitsPlayerAction(new TutorialAttempt { Action = BattleActionType.endTurn }),
            "★ **④ 的例外**：`ActivateHandCards(170)` 即便 `playerAction=false` 也不拦，⑤ 的 170 直接放行任意动作"
          + "｜🧨 把 `!cur.IsActivateHandCards` 去掉 ⇒ 本条红；把 ⑤ 的 `ActivateHandCards` 分支去掉 ⇒ 也红（`Matches` 不认 170）");

        // ---- ⑤ `PlayerChoice(70)` ⇒ 放行任意动作 ----
        var gChoice = new TutorialScript(B94B19Stage(
            B94B19Turn(true, B94B19Act(true, B94B19Data(ScriptedActionType.PlayerChoice)))));
        gChoice.UpdateTurn(1);
        CheckTrue(gChoice.PermitsPlayerAction(new TutorialAttempt { Action = BattleActionType.playCardFromHand }),
            "★ **⑤ `PlayerChoice(70)` ⇒ 放行任意动作**（`playCardFromHand` 不被任何 `Matches` 分支认 ⇒ 只能由 70 那一档给）"
          + "｜🧨 删掉 `if (d.type == (int)ScriptedActionType.PlayerChoice) return true;` ⇒ 本条红");

        // ---- ⑤ 逐条比、任一条命中即放行 ----
        var gMulti = new TutorialScript(B94B19Stage(B94B19Turn(true,
            B94B19Act(true, B94B19Data(ScriptedActionType.EndTurn), match))));
        gMulti.UpdateTurn(1);
        CheckTrue(gMulti.PermitsPlayerAction(attOk),
            "★ **⑤ 逐条比、任一条命中即放行**（第 0 条 `EndTurn` 不认这一刀、第 1 条 `Attack` 认）"
          + "｜🧨 把循环改成「只看第 0 条」⇒ 本条红");
        CheckTrue(!gMulti.PermitsPlayerAction(new TutorialAttempt { Action = BattleActionType.playCardFromHand }),
            "★ **⑥ 两条都不中 ⇒ 拒**（`EndTurn` 只认 `endTurn`、`Attack` 只认 `attack`/`attackContinuation`）");
    }

    // ==================================================================
    //  T3 —— 动作大类矩阵（`CheckIfMatchesActionData` 的第一关）
    // ==================================================================

    static void B94B19T3ActionTypeMatrix()
    {
        // 判据：`ScriptedActionData__CheckIfMatchesActionData.c:29-97`
        //   `PlayCard(20)` 只认 5 · `Attack(30)` 认 {1,53} · `AttackFreeMode(31)` 认 {1,19,53}（掩码 0x20000000080002）
        //   `ChangeToRanged(35)`/`ChangeToMelee(36)` 认 {19,1} · `ActiveAbility(40)` 只认 30 · `EndTurn(100)` 只认 14
        //   `TapCard(120)`：`useWaystone(76)` 立即 `ret 1`；**其余动作不在这里被拒**（继续往下走，后两关不约束 ⇒ 认）
        //   其余（含 32/33/34 与 `SmallTip(80)`）⇒ `ret 0`
        CheckTrue(B94B19M(ScriptedActionType.PlayCard, BattleActionType.playCardFromHand),
            "★ `PlayCard(20)` 认 `playCardFromHand(5)`");
        CheckTrue(!B94B19M(ScriptedActionType.PlayCard, BattleActionType.attack),
            "★ `PlayCard(20)` **不认** `attack(1)`｜🧨 把 `param_2 != 5` 那条改成恒认 ⇒ 红");
        CheckTrue(!B94B19M(ScriptedActionType.PlayCard, BattleActionType.endTurn),
            "★ `PlayCard(20)` 不认 `endTurn(14)`");

        CheckTrue(B94B19M(ScriptedActionType.Attack, BattleActionType.attack), "★ `Attack(30)` 认 `attack(1)`");
        CheckTrue(B94B19M(ScriptedActionType.Attack, BattleActionType.attackContinuation),
            "★ `Attack(30)` 认 `attackContinuation(53)`");
        CheckTrue(!B94B19M(ScriptedActionType.Attack, BattleActionType.changeAttack),
            "★ `Attack(30)` **不认** `changeAttack(19)`（原版那一档只比 1 和 0x35=53）"
          + "｜🧨 把那一行写成 `changeAttack` 也认 ⇒ 红");

        CheckTrue(B94B19M(ScriptedActionType.AttackFreeMode, BattleActionType.attack), "★ `AttackFreeMode(31)` 认 `attack(1)`");
        CheckTrue(B94B19M(ScriptedActionType.AttackFreeMode, BattleActionType.changeAttack), "★ `AttackFreeMode(31)` 认 `changeAttack(19)`");
        CheckTrue(B94B19M(ScriptedActionType.AttackFreeMode, BattleActionType.attackContinuation),
            "★ `AttackFreeMode(31)` 认 `attackContinuation(53)`");
        CheckTrue(!B94B19M(ScriptedActionType.AttackFreeMode, BattleActionType.playCardFromHand),
            "★ `AttackFreeMode(31)` 不认 `playCardFromHand(5)`｜🧨 位掩码 `0x20000000080002` 抄错 ⇒ 红");
        CheckTrue(!B94B19M(ScriptedActionType.AttackFreeMode, BattleActionType.playActiveAbility),
            "★ `AttackFreeMode(31)` 不认 `playActiveAbility(30)`");
        CheckTrue(!B94B19M(ScriptedActionType.AttackFreeMode, BattleActionType.endTurn),
            "★ `AttackFreeMode(31)` 不认 `endTurn(14)`");

        CheckTrue(B94B19M(ScriptedActionType.ChangeToRanged, BattleActionType.changeAttack),
            "★ `ChangeToRanged(35)` 认 `changeAttack(19)`");
        CheckTrue(B94B19M(ScriptedActionType.ChangeToRanged, BattleActionType.attack),
            "★ `ChangeToRanged(35)` **也认** `attack(1)`（原版那一档是两个值 `0x13`/`1`）");
        CheckTrue(!B94B19M(ScriptedActionType.ChangeToRanged, BattleActionType.attackContinuation),
            "★ `ChangeToRanged(35)` 不认 `attackContinuation(53)`");
        CheckTrue(B94B19M(ScriptedActionType.ChangeToMelee, BattleActionType.changeAttack), "★ `ChangeToMelee(36)` 认 `changeAttack(19)`");
        CheckTrue(!B94B19M(ScriptedActionType.ChangeToMelee, BattleActionType.endTurn), "★ `ChangeToMelee(36)` 不认 `endTurn(14)`");

        CheckTrue(B94B19M(ScriptedActionType.ActiveAbility, BattleActionType.playActiveAbility),
            "★ `ActiveAbility(40)` 认 `playActiveAbility(30)`");
        CheckTrue(!B94B19M(ScriptedActionType.ActiveAbility, BattleActionType.attack),
            "★ `ActiveAbility(40)` 不认 `attack(1)`｜🧨 把 `param_2 != 0x1e` 改成恒认 ⇒ 红");
        CheckTrue(!B94B19M(ScriptedActionType.ActiveAbility, BattleActionType.useWaystone),
            "★ `ActiveAbility(40)` 不认 `useWaystone(76)`");

        CheckTrue(B94B19M(ScriptedActionType.EndTurn, BattleActionType.endTurn), "★ `EndTurn(100)` 认 `endTurn(14)`");
        CheckTrue(!B94B19M(ScriptedActionType.EndTurn, BattleActionType.attack),
            "★ `EndTurn(100)` 不认 `attack(1)`｜🧨 把 `param_2 != 0xe` 那条删掉 ⇒ 红");
        CheckTrue(!B94B19M(ScriptedActionType.EndTurn, BattleActionType.playActiveAbility),
            "★ `EndTurn(100)` 不认 `playActiveAbility(30)`");

        CheckTrue(B94B19M(ScriptedActionType.TapCard, BattleActionType.useWaystone),
            "★ `TapCard(120)` 认 `useWaystone(76)`（原版那支 `ret 1` 立即返回）");
        CheckTrue(B94B19M(ScriptedActionType.TapCard, BattleActionType.attack),
            "★ `TapCard(120)` 对**别的动作不掉到 `ret 0`**（原版那支只有 `!= 0x78` 才拒；76 之外继续往下、"
          + "后两关在 acting/target 都空时不约束 ⇒ 认）—— **照抄原版的怪形状**，⛔ 别「修」成只认 76"
          + "｜🧨 给它加一条 `if (a.Action != useWaystone) return false;` ⇒ 本条红");

        CheckTrue(!B94B19M(ScriptedActionType.SmallTip, BattleActionType.attack),
            "★ `SmallTip(80)` ⇒ 认不出任何玩家动作（原版落到 `iVar4 != 0x78 ⇒ ret 0`）");
        CheckTrue(!B94B19M((ScriptedActionType)32, BattleActionType.attack),
            "★ 未定义档 `32` ⇒ 原版 `switch(param - 0x1e)` 落 `default` = `ret 0`"
          + "｜🧨 把 `default:` 改成 `return true` ⇒ 红");
        CheckTrue(!B94B19M((ScriptedActionType)34, BattleActionType.changeAttack),
            "★ 未定义档 `34` 同样 `ret 0`（原版 32/33/34 三档一起落 `caseD_2`）");
    }

    // ==================================================================
    //  T4 —— 发起者 + 目标两关（含 `NoTargetYet` 的**次序**）
    // ==================================================================

    static void B94B19T4ActingAndTargetGates()
    {
        // 判据：`ScriptedActionData__CheckIfMatchesActionData.c:103-171`（发起者只对 10/30/50 三档判，其余 LogError 后**不判**）
        //       `:176-186`（`param_4 == null ⇒ return 1` —— 位置在**发起者之后、目标之前**）
        //       `:188-330`（目标：`0` 认 · `10`/`20` 督军两侧 · `30/40/50/60` 才细判 · `>60` 认 · 其余认）
        const BattleActionType atk = BattleActionType.attack;
        const string A = "AAA", B = "BBB";

        // ---- 发起者 ----
        var dHero = B94B19Data(ScriptedActionType.Attack, (int)ScriptedActionUnit.PlayerWarlord);
        CheckTrue(TutorialScript.Matches(dHero, new TutorialAttempt { Action = atk, ActingIsHero = true }),
            "★ 发起者 `PlayerWarlord(10)` ⇒ 「是督军」那一档 **认**");
        CheckTrue(!TutorialScript.Matches(dHero, new TutorialAttempt { Action = atk, ActingIsHero = false }),
            "★ 发起者 `PlayerWarlord(10)` ⇒ 不是督军 ⇒ **拒**"
          + "｜🧨 删掉发起者那一段（`au == PlayerWarlord` 那支）⇒ 本条红");

        var dMinion = B94B19Data(ScriptedActionType.Attack, (int)ScriptedActionUnit.PlayerMinion, A);
        CheckTrue(TutorialScript.Matches(dMinion, new TutorialAttempt { Action = atk, ActingId = A }),
            "★ 发起者 `PlayerMinion(30)` + `ourId=AAA` ⇒ 同一张 ⇒ 认");
        CheckTrue(!TutorialScript.Matches(dMinion, new TutorialAttempt { Action = atk, ActingId = B }),
            "★ 换成另一张（`BBB`）⇒ **拒**"
          + "｜🧨 删掉 `a.ActingId != d.acting.ourId` 那半句 ⇒ 本条红");
        CheckTrue(!TutorialScript.Matches(dMinion, new TutorialAttempt { Action = atk, ActingIsHero = true }),
            "★ `PlayerMinion` 那档要求**不是督军** ⇒ 督军 ⇒ 拒");

        var dHand = B94B19Data(ScriptedActionType.Attack, (int)ScriptedActionUnit.PlayerCardInHand, A);
        CheckTrue(TutorialScript.Matches(dHand, new TutorialAttempt { Action = atk, ActingId = A }),
            "★ 发起者 `PlayerCardInHand(50)` 与 `PlayerMinion(30)` 同一支（原版 `au - 30 < 2`）⇒ 认");

        // ---- `NoTargetYet`（= 原版 `param_4 == null ⇒ return 1`）在**发起者之后** ----
        CheckTrue(!TutorialScript.Matches(dHero, new TutorialAttempt { Action = atk, ActingIsHero = false, NoTargetYet = true }),
            "★ **次序**：`NoTargetYet` 在**发起者那一关之后** ⇒ 发起者不对时它救不了"
          + "｜🧨 把 `if (a.NoTargetYet) return true;` 挪到发起者之前 ⇒ 本条红"
          + "（真机上的表现：把「还没选目标」的查询当成万用通行证 ⇒ 脚本这一步之外的动作也能开）");
        CheckTrue(TutorialScript.Matches(dHero, new TutorialAttempt { Action = atk, ActingIsHero = true, NoTargetYet = true }),
            "★ 发起者对得上 + `NoTargetYet` ⇒ 放行（目标那关整段不判）");

        // ---- 目标：两个督军档 ----
        var dFoeHero = B94B19Data(ScriptedActionType.Attack, 0, null, (int)ScriptedActionUnit.AiWarlord);
        CheckTrue(TutorialScript.Matches(dFoeHero, new TutorialAttempt { Action = atk, TargetIsHero = true, TargetIsPlayerSide = false }),
            "★ 目标 `AiWarlord(20)` ⇒ 督军 + 非玩家侧 ⇒ 认");
        CheckTrue(!TutorialScript.Matches(dFoeHero, new TutorialAttempt { Action = atk, TargetIsHero = true, TargetIsPlayerSide = true }),
            "★ 目标 `AiWarlord(20)` 却**在玩家侧** ⇒ 拒");
        CheckTrue(!TutorialScript.Matches(dFoeHero, new TutorialAttempt { Action = atk, TargetIsHero = false, TargetIsPlayerSide = false }),
            "★ 目标 `AiWarlord(20)` 却**不是督军** ⇒ 拒");

        var dMyHero = B94B19Data(ScriptedActionType.Attack, 0, null, (int)ScriptedActionUnit.PlayerWarlord);
        CheckTrue(TutorialScript.Matches(dMyHero, new TutorialAttempt { Action = atk, TargetIsHero = true, TargetIsPlayerSide = true }),
            "★ 目标 `PlayerWarlord(10)` + 玩家侧督军 ⇒ 认");
        CheckTrue(!TutorialScript.Matches(dMyHero, new TutorialAttempt { Action = atk, TargetIsHero = true, TargetIsPlayerSide = false }),
            "★ 目标 `PlayerWarlord(10)` 却非玩家侧 ⇒ 拒");

        // ---- 目标：只有 30/40/50/60 四档细判（掩码 `0x1004010040000000`）----
        var dFoeMinion = B94B19Data(ScriptedActionType.Attack, 0, null, (int)ScriptedActionUnit.EnemyMinion, A);
        CheckTrue(TutorialScript.Matches(dFoeMinion, new TutorialAttempt { Action = atk, TargetIsPlayerSide = false, TargetId = A }),
            "★ 目标 `EnemyMinion(40)` + `ourId=AAA` ⇒ 非玩家侧 + 同一张 ⇒ 认");
        CheckTrue(!TutorialScript.Matches(dFoeMinion, new TutorialAttempt { Action = atk, TargetIsPlayerSide = true, TargetId = A }),
            "★ 同一档但目标在**玩家侧** ⇒ 拒"
          + "｜🧨 删掉 `a.TargetIsPlayerSide == wantPlayerSide` 那半句 ⇒ 红");
        CheckTrue(!TutorialScript.Matches(dFoeMinion, new TutorialAttempt { Action = atk, TargetIsPlayerSide = false, TargetId = B }),
            "★ 同一档但**换一张**（`BBB`）⇒ 拒");
        CheckTrue(!TutorialScript.Matches(dFoeMinion, new TutorialAttempt { Action = atk, TargetIsHero = true, TargetIsPlayerSide = false, TargetId = A }),
            "★ `EnemyMinion` 那档要求**不是督军** ⇒ 督军 ⇒ 拒");

        CheckTrue(TutorialScript.Matches(B94B19Data(ScriptedActionType.Attack, 0, null, 100),
                                         new TutorialAttempt { Action = atk, TargetIsHero = true, TargetIsPlayerSide = true }),
            "★ 目标 `unitType > 60` ⇒ 原版**直接认**（不判目标）");
        // ⚠️ `B94B19Data` 的 `targetUnit == 0` 表示「不建 target」（= 原版 null 那一支）；
        //    要测「**有** target 对象但 `unitType == 0`」，得显式挂一个。
        var dTargetNone = B94B19Data(ScriptedActionType.Attack);
        dTargetNone.target = new TutorialUnitRef { unitType = 0, ourId = "" };
        CheckTrue(TutorialScript.Matches(dTargetNone, new TutorialAttempt { Action = atk, TargetIsHero = true, TargetIsPlayerSide = true }),
            "★ 目标 `unitType == 0` ⇒ 认（原版 `uVar1 == 0` 那一支 `return 1`）"
          + "｜🧨 把 0 也塞进那张掩码（`Bit(mask, 0)` 为真）⇒ 本条红（会掉进 30/40/50/60 那一支、被 `TargetIsHero` 拒掉）");
        CheckTrue(TutorialScript.Matches(B94B19Data(ScriptedActionType.Attack, 0, null, 25),
                                         new TutorialAttempt { Action = atk, TargetIsHero = true, TargetIsPlayerSide = true }),
            "★ 目标 `unitType` 落在 21..60 但**不在 {30,40,50,60}** ⇒ 认（那张掩码只覆盖四档）"
          + "｜🧨 把掩码换成「21..60 全判」⇒ 本条红");
        CheckTrue(TutorialScript.Matches(B94B19Data(ScriptedActionType.Attack),
                                         new TutorialAttempt { Action = atk, TargetIsHero = true, TargetIsPlayerSide = true }),
            "★ `data.target == null` ⇒ 目标那一关整段不判");
    }

    // ==================================================================
    //  T5 —— `PlayCard` 的左右落点只在「`targetPosition != 0` ∧ `target ∈ {31,32}`」时判
    // ==================================================================

    static void B94B19T5PlaySlotSentinel()
    {
        // 判据：`ScriptedActionData__CheckIfMatchesActionData.c:33-57`
        //   `if (param_5 != 0 && (targetUnitType - 0x1f) < 2) { … PositionWillBeAtLeft() … }`
        // ⚠️ 原版 14 个调用点传进去的 `targetPosition` **恒被掩码成 0**（`BattleManager__CanPlayCard.c:56-59`
        //   那句 `in_stack… &= 0xffffffff00000000`）⇒ 这条今天只在**我们额外加的第 ② 点**
        //   （`TutAttemptPlayResolve`，真落点）上可能生效。
        var dLeft = B94B19Data(ScriptedActionType.PlayCard, 0, null, (int)ScriptedActionUnit.PlayerMinionLeft);
        var dNotLeft = B94B19Data(ScriptedActionType.PlayCard, 0, null, (int)ScriptedActionUnit.PlayerMinionNotLeft);

        CheckTrue(TutorialScript.Matches(dLeft, new TutorialAttempt { Action = BattleActionType.playCardFromHand, TargetSlot = 0 }),
            "★ `TargetSlot == 0`（= 原版「未指定」哨兵）⇒ 左右那一关**整段不判**（`PlayerMinionLeft` 也认）");
        CheckTrue(TutorialScript.Matches(dNotLeft, new TutorialAttempt { Action = BattleActionType.playCardFromHand, TargetSlot = 0 }),
            "★ 同上：`PlayerMinionNotLeft` 在 `TargetSlot == 0` 时也认"
          + "｜🧨 删掉 `a.TargetSlot != 0` 那个哨兵 ⇒ 本条红（回退近似会把 0 号格算作「在督军左边」⇒ `NotLeft` 被拒）");
        CheckTrue(TutorialScript.Matches(B94B19Data(ScriptedActionType.PlayCard),
                                         new TutorialAttempt { Action = BattleActionType.playCardFromHand, TargetSlot = 2 }),
            "★ `data.target == null` + 给了落点 ⇒ 左右那一关也不判（原版 `param_1+0x20` 读的是 target 的 `unitType`）");
    }

    // ==================================================================
    //  T6 —— **非教学局恒放行**（最容易漏的那一支）+ 灭自证的两态判别
    // ==================================================================

    static void B94B19T6DriverNonTutorialAlwaysPermits()
    {
        // 判据：原版那 14 处**每一处**都先 `IsTutorialMatch(bm)`（`BattleManager__CanPlayCard.c:49` ·
        //   `BattleManager__CanAttackCard.c:8` · `__CanUseWaystone.c:48` · `__EndTurnClick.c:54` …）
        //   ⇒ **非教程局一次都不问**。我们的等价物 = `BattleDriver.TutorialPermits` 第一句 `tut == null ⇒ true`
        //   （`CardPresentation/Battle/BattleDriver.cs:18659-18664`）。
        var blocked = new TutorialAttempt { Action = BattleActionType.playCardFromHand };
        var allowed = new TutorialAttempt
        {
            Action = BattleActionType.attack, ActingIsHero = true, ActingIsPlayerSide = true,
            TargetIsHero = true, TargetIsPlayerSide = false,
        };

        var go = new GameObject("B94B19_GateProbe");
        var drv = go != null ? go.AddComponent<BattleDriver>() : null;
        CheckTrue(drv != null, "（前提）探针驱动建起来了（编辑模式 `AddComponent` 不触发 `Awake/Start`）");
        if (drv == null) { if (go != null) UnityEngine.Object.DestroyImmediate(go); return; }
        try
        {
            CheckTrue(drv.Ctx == null, "（前提）探针驱动还没有局面（`Ctx == null`）");
            CheckTrue(drv.TutorialPermitsForTest(blocked),
                "★ **没有局面 / 没有教程 ⇒ 恒放行**"
              + "｜🧨 把 `if (tut == null) return true;` 改成 `return false;` ⇒ 本条红");

            var plain = RuleCore.NewBattle(StarterCards.Of(StarterCards.EmberFaction),
                                           StarterCards.Of(StarterCards.TideFaction), 20260911);
            plain.Active = 1;      // 让 `SetCtxForTest → ResetClock` 的 `myTurnNow` 为假（`_me` 默认 0）⇒ 不播 StartTurn
            CheckTrue(plain.Tutorial == null, "（前提）`RuleCore.NewBattle` 造出来的是**普通局**（`ctx.Tutorial == null`）");
            drv.SetCtxForTest(plain);
            CheckTrue(drv.TutorialPermitsForTest(blocked),
                "★ **非教学局一律放行**（拿一条在教程局必被拒的动作用来问 —— 这一支最容易漏）"
              + "｜🧨 让 `TutorialPermits` 无条件走检查（`tut == null` 时不早退）⇒ 本条红");

            // ---- 灭自证：**同一台驱动、同一个普通局面**，只把 `ctx.Tutorial` 换成执行器 ----
            // ⚠️ 这是**夹具写**：生产唯一写点 = `RuleCore.NewBattle` 里的 `ctx.Tutorial = tutorial`（⛔ 别照抄到产品代码）。
            var tut = new TutorialScript(B94B19Stage(B94B19Turn(true,
                B94B19Act(true, B94B19Data(ScriptedActionType.Attack, (int)ScriptedActionUnit.PlayerWarlord, null,
                                           (int)ScriptedActionUnit.AiWarlord)))));
            plain.Tutorial = tut;
            drv.SetCtxForTest(plain);
            tut.UpdateTurn(1);
            CheckTrue(drv.TutorialPermitsForTest(allowed),
                "★ **同一台驱动 + 同一个局面，动作换成脚本写着的那一刀 ⇒ 放行**"
              + "（一准一不准 ⇒ 闸门真在看教程态，不是恒 true / 恒 false）");
            CheckTrue(!drv.TutorialPermitsForTest(blocked),
                "★ **灭自证**：只把 `ctx.Tutorial` 从 null 换成执行器 ⇒ 同一条「出牌」**立刻被拒**"
              + "｜🧨 两条同时放行 = 闸门没生效；两条同时拒 = 闸门恒 false");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
