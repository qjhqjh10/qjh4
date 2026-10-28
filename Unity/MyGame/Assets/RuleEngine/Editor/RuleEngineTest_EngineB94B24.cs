// RuleEngineTest_EngineB94B24.cs — 第九十四会话 · 波 6 · 执行代理 `B94_B24`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 本件只放本批 **「座位/归属位」两格的断言**（`UnitState.OwnerSeat` + `BattleContext.EventActingCard`）。
// 宿主 `RuleEngineTest.cs` 里由调度台加一个 `{ CheckB94B24SeatAndActingCard(); }` 块
// （⚠️ 方法名**不用 `Test`/`Report` 打头**，与 `B94_Z2` / `B94_B13` 那几段同形 ——
//  否则会被 `VerifyRunCaseCoverage` / `gen_diag_runner` 认成用例）。
//
// 🔴 **本件断的是「两格语义」本身，不是「引擎某条链跑起来了」**：
//    这两格是【前置格】（账 647 / 1278 的公共缺口），写点 / 读点在 `RuleCore` / `EffectResolver` /
//    `ActionPump`（本轮别的写手在改）⇒ 本件**不假设**它们已被接线，只钉：
//      ① `OwnerSeat` 的两态（未知 / 已知）+ 非 0/1 不写；
//      ② **离场后座位仍在**（原版 `EntityScript.isPlayer(+0x40)` 离场不清 ⇒ 账 647「外加那一支」的前提）；
//    ③ `ChangeOwner` 是取反（抢单位那一跳），且**未知态不编造**；
//    ④ `EventActingCard` 与 `EventTarget` / `EventCard` / `ActingUnit` **是四个独立格**（账 1278 残余①）。
//
// 判据（`d:/2/tools/` 2026-10-29 现读）：
//  · 字段：`il2cpp_out/dump.cs:21973` `private bool <isPlayer>k__BackingField; // 0x40`（`EntityScript`）。
//  · 写点：`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:6462`（`CardSetup`）· `:6739`（`SetAsPlayer`）
//          · `:11855`（`ChangeOwner` = `bool flag = !isPlayer; isPlayer = flag;`）。
//  · 读点：`decomp_full/CardScript__ResolveDamageDealt.c:69/118`（`GetHero(isPlayer)`）
//          · `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:37385`（`battleAction->isPlayer = actingCard.isPlayer`）。
//  · 事件两格：`il2cpp_out/dump.cs` `BattleAction{ isPlayer //0x4 · actingCard //0x8 · targetCard //0x10 }`
//          · `DamageInfo{ actingCard //0x10 · damageType //0x1c }`。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void CheckB94B24SeatAndActingCard()
    {
        Section("🆕 B94_B24：「座位/归属位」两格（`UnitState.OwnerSeat` + `BattleContext.EventActingCard`）");
        B94B24T1OwnerSeatTwoStates();
        B94B24T2SeatSurvivesLeavingBoard();
        B94B24T3ChangeOwnerFlips();
        B94B24T4EventActingCardDistinct();
        B94B24T5OffBoardActingCardSeat();
    }

    // ==========================================================================================
    //  T1 —— `OwnerSeat` 两态（未知 / 已知）+ 非 0/1 不写
    // ==========================================================================================
    static void B94B24T1OwnerSeatTwoStates()
    {
        var u = new UnitState(B94B24Unit("B24Seat"), false);
        // ---- 前提：刚建出来是「未知」 ----
        Check(u.OwnerSeat, -1, "前提：新建 `UnitState` 的 `OwnerSeat` = `-1`（未知 = 还没接线）");
        Check(u.SeatKnown, false, "前提：`SeatKnown` = false（`-1` 不算已知座位）");

        // ---- 已知态：0 / 1 都能写、`SeatKnown` 跟着变 ----
        u.SetOwnerSeat(1);
        Check(u.OwnerSeat, 1, "`SetOwnerSeat(1)` ⇒ `OwnerSeat = 1`（座位 1）");
        Check(u.SeatKnown, true, "`SeatKnown` = true（1 是合法座位）");
        u.SetOwnerSeat(0);
        Check(u.OwnerSeat, 0, "`SetOwnerSeat(0)` ⇒ `OwnerSeat = 0`（座位 0）");

        // ---- 非 0/1 不写（原版字段是 bool ⇒ 只可能是 0/1；传错数不许被静默当成某个座位）----
        u.SetOwnerSeat(2);
        Check(u.OwnerSeat, 0, "★ `SetOwnerSeat(2)` **不写**（仍 0）—— 原版 `isPlayer` 是 bool，2 不是合法座位"
                             + "｜🧨 把校验改成 `OwnerSeat = seat` ⇒ 本行变 2 ⇒ 红");
        u.SetOwnerSeat(-1);
        Check(u.OwnerSeat, 0, "★ `SetOwnerSeat(-1)` **不写**（仍 0）—— `-1` 是哨兵、⛔ 不是用来「清座位」的写值"
                             + "｜🧨 把校验放宽到接受 `-1` ⇒ 本行变 -1 ⇒ 红");
    }

    // ==========================================================================================
    //  T2 —— **离场后座位仍在**（账 647 的核心前提：原版 `isPlayer` 离场不清）
    // ==========================================================================================
    static void B94B24T2SeatSurvivesLeavingBoard()
    {
        var ctx = B94B24Ctx(0xB24A);
        var u = new UnitState(B94B24Unit("B24Leaver"), false);
        ctx.Players[1].Board[0] = u;        // 摆到座位 1 的 0 号格
        u.SetOwnerSeat(1);                  // 模拟「部署那一跳写座位」（写点归 RuleCore，本件手工摆）

        // ---- 前提（在场时两个视角必须一致）----
        Check(B94B24BoardScanSeat(ctx, u), 1, "前提：在场时棋盘扫描得到座位 1（`Board[]` 里找得到）");
        Check(u.OwnerSeat, 1, "前提：在场时 `OwnerSeat` 也是 1（两者一致）");

        // ---- 离场 ----
        ctx.Players[1].Board[0] = null;

        // 情形 A：**拿不到** —— 棋盘扫描（改前的唯一读法）离场即 -1
        Check(B94B24BoardScanSeat(ctx, u), -1,
              "★ 离场后**棋盘扫描得 -1**（= 改前 `OwnerOf` / `FindBoardCell` 的形状 —— 账 647「外加那一支」因此拿不到施动者）");

        // 情形 B：**拿得到** —— 复刻原版 `isPlayer` 离场不清 ⇒ `OwnerSeat` 仍在
        Check(u.OwnerSeat, 1,
              "★ 离场后 `OwnerSeat` **仍是 1**（复刻原版 `EntityScript.isPlayer(+0x40)` 离场不清）"
            + "｜🧨 把离场 / 摘盘那一跳顺手 `SetOwnerSeat` 清成 -1 ⇒ 本行变 -1 ⇒ 红");

        // 消费方取座位的**统一写法**（在场以棋盘为准 · 离场以本字段为准 ⇒ 两态都对）
        Check(B94B24SeatOr(ctx, u), 1,
              "★ 解析式 `OwnerSeat >= 0 ? OwnerSeat : 棋盘扫描` 离场后得 1（这就是账 647 要的那一格）"
            + "｜🧨 把解析式写死成「只查棋盘」⇒ 得 -1 ⇒ 红");

        // ---- 未知态：没写过座位时，解析式**回落**到棋盘扫描（= 本字段没接线时零行为变化）----
        var v = new UnitState(B94B24Unit("B24Unknown"), false);
        Check(v.OwnerSeat, -1, "前提：新单位 `OwnerSeat` = -1（没接线）");
        ctx.Players[0].Board[0] = v;        // 在场、但没人写座位
        Check(B94B24SeatOr(ctx, v), 0,
              "★ `OwnerSeat = -1` 时解析式**回落到棋盘扫描**（得 0）—— 证明「没接线 ⇒ 行为零变化」");
        ctx.Players[0].Board[0] = null;
        Check(B94B24SeatOr(ctx, v), -1,
              "★ 同上：离场 + 没写座位 ⇒ 解析式 -1（回落到棋盘扫描的原结果，不凭空编座位）");
    }

    // ==========================================================================================
    //  T3 —— `ChangeOwner` 是**取反**（抢单位那一跳 `_ResolveStealMinion`），且未知态不编造
    // ==========================================================================================
    static void B94B24T3ChangeOwnerFlips()
    {
        var u = new UnitState(B94B24Unit("B24Steal"), false);
        u.SetOwnerSeat(0);
        u.ChangeOwner();
        Check(u.OwnerSeat, 1, "`ChangeOwner()`：0 ⇒ 1（原版 `isPlayer = !isPlayer`）"
                             + "｜🧨 写成 `OwnerSeat = 1`（恒置 1）也能过这一行，所以下面配对称那一行");
        u.ChangeOwner();
        Check(u.OwnerSeat, 0, "★ 再 `ChangeOwner()`：1 ⇒ 0（**取反**不是恒置，对称回到 0）"
                             + "｜🧨 把它写成 `OwnerSeat = 1` / `OwnerSeat++` ⇒ 本行不会回到 0 ⇒ 红");

        // ---- 未知态：不翻（原版 bool 没未知态；翻一个未知值 = 编造座位）----
        var w = new UnitState(B94B24Unit("B24StealUnknown"), false);
        Check(w.OwnerSeat, -1, "前提：`w.OwnerSeat` = -1（未知）");
        w.ChangeOwner();
        Check(w.OwnerSeat, -1, "★ 未知态 `ChangeOwner()` **仍 -1**（⛔ 不把 -1 翻成 0——那是编造座位）"
                              + "｜🧨 写成无条件 `1 - OwnerSeat`（-1 ⇒ 0）⇒ 本行变 0 ⇒ 红");
    }

    // ==========================================================================================
    //  T4 —— `EventActingCard` 与 `EventTarget` / `EventCard` / `ActingUnit` **是四个独立格**
    // ==========================================================================================
    static void B94B24T4EventActingCardDistinct()
    {
        var ctx = B94B24Ctx(0xB24B);

        // ---- 前提：默认 null ----
        CheckTrue(ctx.EventActingCard == null, "前提：新 `BattleContext` 的 `EventActingCard` = null");

        var acting = new UnitState(B94B24Unit("B24Actor"), false);
        var tgt = new UnitState(B94B24Unit("B24Target"), false);
        var evCard = B94B24Unit("B24EventCard");
        var actUnit = new UnitState(B94B24Unit("B24ActingUnit"), false);

        ctx.EventActingCard = acting;
        ctx.EventTarget = tgt;
        ctx.EventCard = evCard;
        ctx.ActingUnit = actUnit;

        // ---- 四格各读各的（写一个不动另一个）----
        CheckTrue(ReferenceEquals(ctx.EventActingCard, acting),
                  "★ `EventActingCard` 独立可读（= 施动卡 `acting`）"
                + "｜🧨 若把它写成 `EventTarget` 的别名（同字段）⇒ 本行拿到 tgt ⇒ 红");
        CheckTrue(ReferenceEquals(ctx.EventTarget, tgt),
                  "★ 写 `EventActingCard` **不改** `EventTarget`（≠ `acting`）—— 两格不打架"
                + "｜🧨 把两格合成一个字段 ⇒ 本行拿到 acting ⇒ 红");
        CheckTrue(ReferenceEquals(ctx.EventCard, evCard), "★ `EventCard` 仍是那张卡定义（`CardDef`，不受施动卡影响）");
        CheckTrue(ReferenceEquals(ctx.ActingUnit, actUnit), "★ `ActingUnit`（正在结算那条 op 的施放者）与施动卡是**不同**格");

        // ---- 判别式：施动卡与宾语是**不同对象**且互不覆盖（账 1278 残余①：两格并存）----
        CheckTrue(!ReferenceEquals(ctx.EventActingCard, ctx.EventTarget),
                  "★ 施动卡 ≠ 宾语（`actingCard` / `targetCard` 原版两格并存 ⇒ 我们能分别种）"
                + "｜🧨 若只留一个 `seed` 槽（把 acting 也塞进 EventTarget）⇒ 本行红");

        // ---- 还原：置回 null（瞬态纪律）----
        ctx.EventActingCard = null;
        CheckTrue(ctx.EventActingCard == null, "`EventActingCard` 可被还原成 null（瞬态格：广播 / 结算完要还原）");
        CheckTrue(ReferenceEquals(ctx.EventTarget, tgt), "还原 `EventActingCard` **不动** `EventTarget`（还原也独立）");
    }

    // ==========================================================================================
    //  T5 —— 组合：**施动卡离场后，经 `EventActingCard` 仍读得到它的座位**（账 647 的完整形状）
    // ==========================================================================================
    static void B94B24T5OffBoardActingCardSeat()
    {
        var ctx = B94B24Ctx(0xB24C);
        var attacker = new UnitState(B94B24Unit("B24Attacker"), false);
        ctx.Players[1].Board[0] = attacker;   // 敌方座位 1 的 0 号格
        attacker.SetOwnerSeat(1);             // 部署那一跳写座位（本件手工摆）

        // 事件：某件事的施动卡 = 攻击者，宾语 = 别人
        ctx.EventActingCard = attacker;
        var victim = new UnitState(B94B24Unit("B24Victim"), false);
        ctx.EventTarget = victim;

        // 攻击者离场（已死 / 被移出棋盘）
        ctx.Players[1].Board[0] = null;

        // 前提 / 结论：经施动卡引用仍拿得到座位（离场后）
        CheckTrue(ctx.EventActingCard != null, "前提：`EventActingCard` 引用仍在（持有离场单位的对象引用）");
        Check(B94B24BoardScanSeat(ctx, ctx.EventActingCard), -1,
              "★ 前提：这个施动卡**已不在棋盘上**（棋盘扫描 -1）");
        Check(ctx.EventActingCard.SeatKnown, true, "前提：施动卡 `SeatKnown` = true（部署时写过座位）");
        Check(B94B24SeatOr(ctx, ctx.EventActingCard), 1,
              "★ **施动卡离场后仍拿得到座位 1**（账 647「外加那一支」要的正是这一格：原版读 `actingCard.isPlayer`）"
            + "｜🧨 只按棋盘扫描取座位 ⇒ 得 -1 ⇒ 红");
    }

    // ==========================================================================================
    //  本件私有小工具（都带 `B24` 前缀，⛔ 不与宿主 / 兄弟分册的同名助手冲突）
    // ==========================================================================================

    /// <summary>一个**手搭的**最小对局：两个玩家 + 两个督军（不跑开局的抽牌/洗牌）。
    /// 目的 = 摆出「棋盘 + 空座位」这一个截面（同 `B94B13Ctx` 的做法）。</summary>
    static BattleContext B94B24Ctx(int seed)
    {
        var ctx = new BattleContext(seed);
        ctx.Players[0] = new PlayerState();
        ctx.Players[1] = new PlayerState();
        var h0 = new UnitState(B94B24Hero("B24HeroP0"), true);
        ctx.Players[0].Warlord = h0; ctx.Players[0].Board[BoardSpec.WarlordSlot] = h0;
        var h1 = new UnitState(B94B24Hero("B24HeroP1"), true);
        ctx.Players[1].Warlord = h1; ctx.Players[1].Board[BoardSpec.WarlordSlot] = h1;
        ctx.Active = 0;
        return ctx;
    }

    static CardDef B94B24Hero(string name)
    {
        return new CardDef(name, name, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static CardDef B94B24Unit(string name)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 1, 0, 1, 0, null);
    }

    /// <summary>**改前唯一那种读法**（`OwnerOf` / `FindBoardCell` 的形状）：按引用扫两方棋盘，找不到 ⇒ -1。
    /// 只在本件里当「拿不到」的对照，⛔ 不是引擎的第二份判据（引擎那份仍在 `EffectResolver.OwnerOf`）。</summary>
    static int B94B24BoardScanSeat(BattleContext ctx, UnitState u)
    {
        if (ctx == null || u == null) return -1;
        for (int p = 0; p < ctx.Players.Length; p++)
        {
            var b = ctx.Players[p].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (ReferenceEquals(b[s], u)) return p;
        }
        return -1;
    }

    /// <summary>**本批定下的统一取座位写法**（消费方照抄这一句）：
    /// `OwnerSeat >= 0 ? OwnerSeat : 棋盘扫描` —— 在场以棋盘为准、离场以本字段为准，
    /// `-1`（没接线）时回落到原行为 ⇒ 零行为变化。</summary>
    static int B94B24SeatOr(BattleContext ctx, UnitState u)
    {
        if (u == null) return -1;
        return u.OwnerSeat >= 0 ? u.OwnerSeat : B94B24BoardScanSeat(ctx, u);
    }
}
