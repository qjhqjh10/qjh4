// B99E1_Assert.cs — 第九十九会话 · 波 1 · 执行代理 `B99_E1`（战斗线 · `RuleCore` 面）
//
// ============================ 它盯的是什么 ============================
//  账 **232**（`BattleManager.CanPlayCard`）的**最后一块残余**：形参 `actionAlreadyStarted` 收了却**体内不读**。
//  原版（`decomp_full/BattleManager__CanPlayCard.c:68-71`）那两句侧闸是：
//    ① `card.isPlayer ∧ !allowPlayerActions(+0x244)`
//    ② `initiateAction ∧ !card.isPlayer ∧ !allowEnemyActions(+0x246)`
//  而**每一项都再合取** `!(actionAlreadyStarted(param_5) ∧ resolvingTurnEnd(+0x245))` —— 即 `.c:69` / `:71`
//  两处同一个子式，那是整条 `CanPlayCard` 的**总旁路**：只有「回合结束已在结算中」这一窗口里还在跑的那条
//  出牌状态机（`BattleManager._ResolvePlayCardFromHand_d447__MoveNext.c:213` 传 `param_5 = 1`，**全仓只此一处**
//  传活值）才能绕开两面 allow 旗。
//
//  本件落地 = `RuleCore.CanPlayCard` 的 ③ 侧闸改成
//    `if (ctx.IsOver) return ErrNotTurn; if (p != ctx.Active && !(actionAlreadyStarted && ResolvingTurnEnd(ctx))) return ErrNotTurn;`
//  （**同一物理行**，零行数变化）+ 类尾 `ResolvingTurnEnd`（**恒 false**，理由见那里的 doc）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `d:/2/tools/decomp_full/BattleManager__CanPlayCard.c:66-72`（那两句侧闸 + 总旁路）
//   · `d:/2/tools/il2cpp_out/dump.cs:30816-30820`（三面旗的真名：`+0x244 allowPlayerActionsFlag` /
//     `+0x245 resolvingTurnEndFlag` / `+0x246 allowEnemyActionsFlag`）
//   · `d:/2/tools/decomp_full/BattleManager__EndTurnClick.c`（`+0x245` 置真点）·
//     `BattleManager__set_resolvingTurnEndFlag.c`（清 0 点）· `grep -rn "0x418"` 那一族与本账无关（那是账 866 的 ⑤）
//   · 施工判据 → `资料/完全复刻/判据/B98_J0_战斗剩余终局.md` §一「232」
//   · 本件报告 → `资料/完全复刻/报告/B99_E1.md`
//
// ============================ 🔴 覆盖边界（如实记）============================
//   · **本仓今天没有 `+0x245` 的等价位**：`EndTurn` 是同步的、也没有动作队列 ⇒「回合结束结算中」这个
//     可观测态不存在 ⇒ `ResolvingTurnEnd` 恒 `false` ⇒ **旁路不可达**（B 段的 232b 钉的就是这件事）。
//   · **全部调用点**（`RuleCore.PlayCard:3940` / `SimpleAI:339/:355` / `BattleDriver:5665` / 自检若干）
//     都把 `actionAlreadyStarted` 传默认 `false` ⇒ 本件对现池**零行为变化、零指纹变化**。
//   · 原版 ① 与 ② 的**差异**（② 只在 `initiateAction` 为真时才成立）我们仍按 `p != ctx.Active` 合并判
//     —— 那是改前就有的近似（`SimpleAI.cs:314` 已如实记），本件**不动**它。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加（现读 `:1294` 那条超长行，行内追加 ⇒ 零行数变化）：
//      B99E1_Assert.Run(); _pass += B99E1_Assert.Pass; _fail += B99E1_Assert.Fail;
//  （宿主由调度台代接线；本文件**只碰纯引擎**，不建 GameObject。）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · **232b**：把 `CanPlayCard` 那句改成 `if (p != ctx.Active && !actionAlreadyStarted)`（= 漏掉
//     `ResolvingTurnEnd` 那一半）⇒ 非行动方带旗**就能出牌** ⇒ 232b 立刻红。这正是「单独落 = 造出原版
//     没有的行为」（`B98_K2` 2026-10-29 的原话）那条风险的**唯一**结构性防线。
//   · **232d**：把 `ResolvingTurnEnd` 改成 `true` ⇒ 232b 也红（同上一条后果）。
//   · **232e**：把形参 `actionAlreadyStarted` 从签名里删掉 ⇒ 编译红（这条是签名对齐的守卫，不靠断言）。
//   · **232c**：把 `p != ctx.Active` 那一整句删掉 ⇒ 非行动方**不出旗也能出牌** ⇒ 232a 红。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B99_E1`：账 232 —— `CanPlayCard` 的总旁路 `actionAlreadyStarted ∧ resolvingTurnEnd`。</summary>
public static class B99E1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[E1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B99_E1 账 232：`CanPlayCard` 的 `actionAlreadyStarted ∧ resolvingTurnEnd` 总旁路");

        SectionA_Premise();
        SectionB_BypassClosedToday();

        Debug.Log(_sink.P + "B99E1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    //  ⚠️ 与 `RuleEngineTest.Battle` 同口径：督军在最前、其余倒序（抽牌是 `pop_back`）⇒ 最后入列的先抽到。
    static CardDef War(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord");
    }

    static CardDef Troop(string name, int cost)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, 1, 3, 0, null);
    }

    static BattleContext Fixture()
    {
        var d0 = new List<CardDef> { War("E1Warlord0", "SpaceWolves") };
        for (int i = 0; i < 8; i++) d0.Add(Troop("E1F0_" + i, 1));
        d0.Add(Troop("E1Mine", 0));                 // 先抽到（费用 0 ⇒ 一定付得起）
        var d1 = new List<CardDef> { War("E1Warlord1", "Goff") };
        for (int i = 0; i < 8; i++) d1.Add(Troop("E1F1_" + i, 1));
        d1.Add(Troop("E1FoeMine", 0));
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
        RuleCore.BeginTurn(ctx);                    // 开局不算回合 ⇒ 手动开第一回合（与 `ToP1Turn` 同口径）
        return ctx;
    }

    static int HandIdx(BattleContext ctx, int p, string name)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++) if (hand[i] != null && hand[i].Card != null && hand[i].Card.Name == name) return i;
        return -1;
    }

    /// <summary>那一侧的「费用 0 单位牌」名 —— 两个牌库各塞一张，谁先手都拿得到（夹具与 `FirstSeat` 解耦）。</summary>
    static string MineName(int p) { return p == 0 ? "E1Mine" : "E1FoeMine"; }

    static BattleContext _ctx;

    // ============================================================ A 前提（⛔ 不许写成 `if (前提) Check(结论)`）
    static void SectionA_Premise()
    {
        _ctx = Fixture();
        var ctx = _ctx;
        int act = ctx.Active, inact = 1 - act;

        Tr(!ctx.IsOver, "A0 前提：对局未结束（`ctx.IsOver == false`）");
        Tr(ctx.Active == 0 || ctx.Active == 1, "A1 前提：`ctx.Active` 是合法座位（0/1）");
        Tr(ctx.Players[act].Hand.Count > 0, "A2 前提：行动方手牌非空");
        Tr(HandIdx(ctx, act, MineName(act)) >= 0, "A3 前提：行动方手牌里拿到了那张费用 0 的单位牌（`E1Mine` / `E1FoeMine`）");
        Tr(ctx.Players[inact].Hand.Count > 0, "A4 前提：非行动方手牌非空（空手牌时 232a/232b 会退化成 `ErrNotTurn` 以外的码）");
    }

    // ============================================================ B 主断言（232a–d）
    static void SectionB_BypassClosedToday()
    {
        var ctx = _ctx;
        if (ctx == null) { Tr(false, "B0 前提：夹具没建起来（A 段已红）"); return; }

        int act = ctx.Active, inact = 1 - act;
        int mine = HandIdx(ctx, act, MineName(act));
        if (mine < 0) { Tr(false, "B0 前提：行动方手牌里没有那张费用 0 的单位牌（A3 已红）"); return; }

        // 232a —— 非行动方：不带旗 ⇒ 拒（改前的行为，⛔ 别让它变绿）
        C(RuleCore.CanPlayCard(ctx, inact, 0, 3), RuleCodes.ErrNotTurn,
          "232a 不是这一侧的回合 ⇒ `ErrNotTurn`（不带 `actionAlreadyStarted`）");

        // 232b —— 🔴 本件的核心：非行动方**带旗**也照样拒（因为 `resolvingTurnEnd` 本仓恒 false）。
        // 这条一旦变绿 = 有人把 `ResolvingTurnEnd` 那一半掉了 ⇒ 非行动方也能出牌 = 原版没有的行为。
        C(RuleCore.CanPlayCard(ctx, inact, 0, 3, actionAlreadyStarted: true), RuleCodes.ErrNotTurn,
          "232b ★ 不是这一侧的回合 + `actionAlreadyStarted: true` ⇒ **仍 `ErrNotTurn`**（旁路要 `resolvingTurnEnd` 同时为真才开，本仓恒 false）");

        // 232c —— 行动方：正常出牌不受影响（带不带旗都一样 ⇒ 旗只挂在「非本回合」那一支上）
        C(RuleCore.CanPlayCard(ctx, act, mine, 3), RuleCodes.OK,
          "232c 行动方自己的单位牌 ⇒ `OK`（改前行为不变）");
        C(RuleCore.CanPlayCard(ctx, act, mine, 3, actionAlreadyStarted: true), RuleCodes.OK,
          "232c′ 行动方 + `actionAlreadyStarted: true` ⇒ 仍 `OK`（旗不改正常路）");

        // 232d —— 前提断言：`+0x245` 的等价位**确实是 false**。将来真接上这个状态 ⇒ 这条先红，
        //  提醒把 232b 反过来写成正向断言（「旁路真能开」），⛔ 别默默把 232b 删掉。
        Tr(RuleCore.ResolvingTurnEndForTest(ctx) == false,
           "232d 前提：`ResolvingTurnEnd`（原版 `+0x245 resolvingTurnEndFlag`）本仓恒 false —— 接上真值后本段要改成正向断言");

        // 232e —— 签名对齐的守卫：`actionAlreadyStarted` 是**具名可选形参**（调用点全走默认值）。
        //  这条靠编译：把形参删掉 ⇒ 本文件编不过（⛔ 不写断言，只在此注明）。
    }
}
