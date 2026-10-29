// RuleEngineTest_B98G7.cs — 第九十八会话 · 波 5b · 执行代理 `B98_G7`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆）。
// 本件只放本批新增判据的断言；宿主 `RuleEngineTest.cs` 里只需要加一个
//   `{ RunB98G7(); }` 块（⚠️ 方法名**不用 `Test`/`Report` 打头**，由调度台接线）。
//
// 本件覆盖两条账（判据 = `d:/2/tools/decomp_full/`，2026-10-29 逐句亲读）：
//   ① 账 93 · `AiScripted.ExecuteAction` 残差② —— `ResolveCard(140)` 取的是
//      `data[0].actingCard(+0x18)` 的**活跃实例**（`AiScripted__ExecuteAction.c:187-213`：
//      `lVar13 = *(lVar13 + 0x18)` 直接当第一实参送进 `RawCardScript__TriggerAbilities`），
//      **不是**按 `ourId` 回查卡池。
//      落点 = `RuleEngine/Core/TutorialScript.cs` 的 `RunResolveCard`（改）＋
//      `TutorialRules.ResolveActingCardInstance`（新增，追加在类尾）。
//   ② 账 548 · `BattleManager.ResolveTransformUnit` 的**手牌两跳**（`RuleCore.TransformUnitPostResolve`）：
//      `.c:1829/:1831` 两手 `PlayerHand.UpdateCardEffects(hand, 0, 0)` ⇒ `RuleCore.ExpireHandBuffs(ctx, false)`；
//      `.c:1931/:1981` 两手 `PlayerHand.CheckEffectsOnNewCard(hand, card)` ⇒ `RuleCore.SetupCardInHand` ×2。
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
// 🔴 **先断前提、再断结论**（`if (x != null) Check(...)` 那种「前提不成立就静默跳过」的写法一律不用）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀）。</summary>
    public static void RunB98G7()
    {
        Section("🆕 B98_G7：账 93 `ResolveCard(140)` 认活跃实例 · 账 548 `TransformUnit` 手牌两跳");
        B98G7T1ResolveCardLiveInstance();
        B98G7T1bResolveCardSkipsWithoutLiveInstance();
        B98G7T2SetupCardInHandTwoHops();
        B98G7T3ExpireHandBuffsHop();
    }

    // ==================================================================
    //  T1 · 账 93：`ResolveCard(140)` 认**活跃实例**（不是按 `ourId` 回查卡池）
    //  判据 = `decomp_full/AiScripted__ExecuteAction.c:187-213`
    // ==================================================================
    static void B98G7T1ResolveCardLiveInstance()
    {
        // 卡池里放一张**同 ourId**、但 desc 为空的对照卡：旧口径（`PoolLookup(ourId)`）只会跑到它
        // ⇒ `TriggerEnchantment` 两跳都没有 op ⇒ 整条动作空转、返回 false。
        var poolCard = new CardDef("B98G7Pool", "B98G7Pool", "unit", "", null, "Test", 1, 1, 1, 0, null);
        var liveCard = new CardDef("B98G7Live", "B98G7Live", "unit", "Gain +2 Attack", null, "Test", 1, 2, 3, 0, null);
        var ctx = BattlePool(new[] { liveCard }, new[] { Unit("B98G7Foe", 1, 0, 9) },
                             new List<CardDef> { poolCard, liveCard });
        ToP1Turn(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B98G7Live"), 1), RuleCodes.OK,
                  "前提：把活跃实例 `B98G7Live` 部署到 P0 棋盘");
        int slot = B98G7SlotByName(ctx, 0, "B98G7Live");
        CheckTrue(slot >= 0, "前提：在 P0 棋盘上找到它");
        if (slot < 0) return;
        var live = ctx.Players[0].Board[slot];
        CheckTrue(live != null && live.Card != null, "前提：那一格上真有单位、且带卡定义");
        if (live == null || live.Card == null) return;
        Check(live.Card.Desc, "Gain +2 Attack", "前提：活跃实例的正文可解析");
        Check(poolCard.Desc, "", "前提：卡池里同 id 那张 `B98G7Pool` 的 desc 为空（旧口径跑它 = 两跳空转）");

        // 卡名命中活跃实例；`ourId` **故意**指向卡池里那张 —— 这正是「身份不是 id」的那个形状。
        var acting = new TutorialUnitRef
        {
            unitType = (int)ScriptedActionUnit.PlayerMinion,
            unitTypeName = "PlayerMinion",
            name = "B98G7Live",
            ourId = "B98G7Pool",
        };

        int side, s2, hi;
        var card = TutorialRules.ResolveActingCardInstance(ctx, acting, out side, out s2, out hi);
        // 🧨 把 `ResolveActingCardInstance` 改回 `TutorialRules.PoolLookup(ctx)(r.ourId)` ⇒ 下面第一条红。
        CheckTrue(ReferenceEquals(card, live.Card),
                  "🧨 取到的是**活跃实例**那张 `CardDef`（`ReferenceEquals`）—— 原版 `.c:207` 直接把活对象当第一实参");
        CheckTrue(!ReferenceEquals(card, poolCard),
                  "🧨 灭自证：**不是**卡池里按 `ourId` 另查的那张");
        Check(side, 0, "定位回填：座位 0（`PlayerMinion`）");
        Check(s2, slot, "定位回填：格号 = 它在棋盘上的格号");
        Check(hi, -1, "定位回填：不在手牌里");

        // ---- 行为：同一张卡连发 `trigger 0` + `trigger 500`，两跳共用**同一次出牌实例** ----
        int atkBefore = ctx.Players[1].Warlord.Attack + ctx.Players[1].Warlord.RangedAttack;
        var script = new TutorialScript(null);
        var action = new TutorialAction
        {
            playerAction = true,
            data = new[] { new TutorialActionData { type = (int)ScriptedActionType.ResolveCard, acting = acting } },
        };
        bool ran = script.ExecuteAction(ctx, action);
        CheckTrue(ran, "🧨 `ResolveCard(140)` 真的跑到了 op（旧口径按 `ourId` 查池 ⇒ 池里那张 desc 空 ⇒ 两跳空转 ⇒ 返回 false）");
        Check(ctx.Players[1].Warlord.Attack + ctx.Players[1].Warlord.RangedAttack, atkBefore + 2,
              "🧨 `Gain +2 Attack` 落在 acting 上（原版 `thisCard = cardPlayed = GetHero(mgr,0)` = AI 督军）");
        // `TriggerEnchantment` 对**同一个 `enchantmentCard` 实参**连发两跳 ⇒ 日志点名活实例那张。
        CheckTrue(B98G7Events(ctx).IndexOf("附魔触发：「B98G7Live」跑 `trigger 0`", System.StringComparison.Ordinal) >= 0,
                  "🧨 日志点名的是**活跃实例**「B98G7Live」（两跳共用同一个实参 ⇒ 作用在同一次出牌实例上）");
    }

    // ==================================================================
    //  T1b · 账 93 的另一半：**认不出活跃实例 ⇒ 整支跳过（也不抛）**
    //  判据 = `AiScripted__ExecuteAction.c:191-193`（`actingCard == null` 那一支）
    // ==================================================================
    static void B98G7T1bResolveCardSkipsWithoutLiveInstance()
    {
        var ctx = Battle(new[] { Unit("B98G7NoLive", 1, 1, 1) }, new[] { Unit("B98G7Foe2", 1, 0, 9) });
        ToP1Turn(ctx, 1);
        // 前提：P0 棋盘上**一个非督军单位都没有**（`FindBoardUnit` 的 `list[0]` 也退不出来）。
        Check(B98G7CountMinions(ctx, 0), 0, "前提：P0 棋盘上没有任何部队");
        var acting = new TutorialUnitRef
        {
            unitType = (int)ScriptedActionUnit.PlayerMinion,
            unitTypeName = "PlayerMinion",
            name = "无此单位",
            ourId = "B98G7NoLive",
        };
        int side, slot, hi;
        var card = TutorialRules.ResolveActingCardInstance(ctx, acting, out side, out slot, out hi);
        CheckTrue(card == null, "被测：认不出活跃实例 ⇒ `null`（⛔ 不拿卡池那张顶上去）");
        var script = new TutorialScript(null);
        var action = new TutorialAction
        {
            playerAction = true,
            data = new[] { new TutorialActionData { type = (int)ScriptedActionType.ResolveCard, acting = acting } },
        };
        // 🧨 把「card == null ⇒ return false」改成 `card = PoolLookup(ourId)` ⇒ 本条红。
        CheckTrue(!script.ExecuteAction(ctx, action),
                  "🧨 原版 `.c:191-193`：`actingCard` 为空 ⇒ **整支跳过**（不抛、不改局面）");
        CheckTrue(B98G7Events(ctx).IndexOf("原版整段跳过", System.StringComparison.Ordinal) >= 0,
                  "被测：跳过要**说出来**（不许静默失败）");
    }

    // ==================================================================
    //  T2 · 账 548 第 ① 跳：两手 `CheckEffectsOnNewCard` ⇒ `SetupCardInHand` ×2
    //  判据 = `ResolveTransformUnit.c:1931/:1981`
    // ==================================================================
    static void B98G7T2SetupCardInHandTwoHops()
    {
        var beast = new CardDef("B98G7BeastForm", "B98G7BeastForm", "unit", "", null, "Test",
                                2, 3, 5, 0, null, subtype: "Beast");
        var src = Unit("B98G7SrcA", 1, 2, 4);
        var ctx = Battle(new[] { src }, new[] { Unit("B98G7FoeA", 1, 0, 9) });
        ToP1Turn(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B98G7SrcA"), 1), RuleCodes.OK,
                  "前提：部署 `B98G7SrcA`");
        int slot = B98G7SlotByName(ctx, 0, "B98G7SrcA");
        CheckTrue(slot >= 0, "前提：找到它的格号");
        if (slot < 0) return;

        // 两张**手牌登记表**各放一条「只登记、不贴牌」的记录（= 原版 `PlayerHand.activeEffects // +0x48`；
        // 我们的 `RuleCore.HandEffectRegistry` 按 `RecordId` 去重、`Op.Verb == "give"` 才收）。
        ctx.Players[0].HandEffectRecords.Add(
            B98G7Rec(ctx.NextHandEffectRecord(), "G7FIX-P0", "+1 melee attack", "Beast", false, null));
        ctx.Players[0].HandEffectRecords.Add(
            B98G7Rec(ctx.NextHandEffectRecord(), "G7FIX-P0-BAD", "+9 melee attack", "Vehicle", false, null));
        ctx.Players[1].HandEffectRecords.Add(
            B98G7Rec(ctx.NextHandEffectRecord(), "G7FIX-P1", "+2 melee attack", "Beast", false, null));
        Check(ctx.Players[0].HandEffectRecords.Count, 2, "前提：P0 登记表里有 2 条（1 条命中 Beast、1 条只配 Vehicle）");
        Check(ctx.Players[1].HandEffectRecords.Count, 1, "前提：P1 登记表里有 1 条");

        CheckTrue(RuleCore.TransformUnit(ctx, 0, slot, beast, keepEffects: false),
                  "被测：变身成 Beast 形态（`keepEffects:false` ⇒ 新实例手上本来是空的）");
        var nu = ctx.Players[0].Board[slot];
        CheckTrue(nu != null && nu.Instance != null, "前提：变身后那一格上有实例");
        if (nu == null || nu.Instance == null) return;
        var ni = nu.Instance;
        // 🧨 删掉 `TransformUnitPostResolve` 里那句 `SetupCardInHand`（或只留一侧）⇒ 这条红（实得 0 / 1）。
        Check(ni.HandEffects.Count, 2,
              "🧨 两手各一次 `CheckEffectsOnNewCard` ⇒ 新实例身上补挂 **2** 条（P0 的 +1 + P1 的 +2）");
        CheckTrue(B98G7HasPayload(ni, "+1 melee attack"), "🧨 `SetupCardInHand(ctx, 0, ni)` 那一跳真的跑了（P0 的 +1）");
        CheckTrue(B98G7HasPayload(ni, "+2 melee attack"), "🧨 `SetupCardInHand(ctx, 1, ni)` 那一跳真的跑了（P1 的 +2）");
        CheckTrue(!B98G7HasPayload(ni, "+9 melee attack"),
                  "🧨 灭自证：`Vehicle` 那条**没有**乱发给 Beast（`HandEffectFits` 的兵种筛）");
        Check(ctx.Players[0].HandEffectRecords.Count, 2,
              "🧨 灭自证：非 extrinsic 的记录**不被**这一趟摘掉（`ExpireHandBuffs` 只摘 extrinsic 那种）");
        CheckTrue(B98G7Events(ctx).IndexOf("补挂上 **2** 条", System.StringComparison.Ordinal) >= 0,
                  "被测：出声里报了 **2** 条（= 两跳各补了 1 条）");
    }

    // ==================================================================
    //  T3 · 账 548 第 ② 跳：两手 `UpdateCardEffects(hand, 0, 0)` ⇒ `ExpireHandBuffs(ctx, false)`
    //  判据 = `ResolveTransformUnit.c:1829/:1831` + `PlayerHand__UpdateCardEffects.c` 的 extrinsic 支线
    // ==================================================================
    static void B98G7T3ExpireHandBuffsHop()
    {
        var plain = Unit("B98G7SrcB", 1, 2, 4);
        var form = new CardDef("B98G7PlainForm", "B98G7PlainForm", "unit", "", null, "Test",
                               2, 3, 5, 0, null, subtype: "Beast");
        var ctx = Battle(new[] { plain }, new[] { Unit("B98G7FoeB", 1, 0, 9) });
        ToP1Turn(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "B98G7SrcB"), 1), RuleCodes.OK,
                  "前提：部署 `B98G7SrcB`");
        int slot = B98G7SlotByName(ctx, 0, "B98G7SrcB");
        CheckTrue(slot >= 0, "前提：找到它的格号");
        if (slot < 0) return;
        var ownHero = ctx.Players[0].Warlord;
        CheckTrue(ownHero != null, "前提：P0 督军在（要拿它当「施放者还在场」那条的施放者）");
        if (ownHero == null) return;

        int ridDie = ctx.NextHandEffectRecord();
        int ridLive = ctx.NextHandEffectRecord();
        // 两条都是 `extrinsic`（= 原版 `cardEffect.extrinsic(+0x30)`），区别只在**施放者在不在场**。
        ctx.Players[0].HandEffectRecords.Add(
            B98G7Rec(ridDie, "G7FIX-DIE", "+1 melee attack", "Beast", true, null));
        ctx.Players[0].HandEffectRecords.Add(
            B98G7Rec(ridLive, "G7FIX-LIVE", "+2 melee attack", "Beast", true, ownHero));
        Check(ctx.Players[0].HandEffectRecords.Count, 2, "前提：P0 登记表里两条 **extrinsic** 记录");

        CheckTrue(RuleCore.TransformUnit(ctx, 0, slot, form, keepEffects: false), "被测：变身成功");
        // 🧨 删掉 `TransformUnitPostResolve` 里那句 `ExpireHandBuffs(ctx, endOfTurn: false)` ⇒ 本条红（实得 2）。
        Check(ctx.Players[0].HandEffectRecords.Count, 1,
              "🧨 施放者不在场那条被 `ExpireHandBuffs(endOfTurn:false)` 摘掉"
              + "（原版 `.c:1829/:1831` 两手 `PlayerHand.UpdateCardEffects(hand, 0, 0)`）");
        var recs = ctx.Players[0].HandEffectRecords;
        CheckTrue(recs.Count == 1 && recs[0] != null && recs[0].RecordId == ridLive,
                  "🧨 灭自证：施放者**还在场**那条 `G7FIX-LIVE` 活着（不是「一律清空 extrinsic」）");
        CheckTrue(B98G7Events(ctx).IndexOf("摘掉 **1** 条", System.StringComparison.Ordinal) >= 0,
                  "被测：出声里报了摘掉 **1** 条记录（不许静默）");

        var nu = ctx.Players[0].Board[slot];
        CheckTrue(nu != null && nu.Instance != null, "前提：变身后那一格上有实例");
        if (nu == null || nu.Instance == null) return;
        Check(nu.Instance.HandEffects.Count, 1,
              "🧨 顺序是「先摘、后补」：只剩存活那条被补挂到新实例上（被摘那条**不会**再贴回来）");
        CheckTrue(B98G7HasPayload(nu.Instance, "+2 melee attack"), "被测：补挂上去的是存活那条（+2）");
        CheckTrue(!B98G7HasPayload(nu.Instance, "+1 melee attack"), "🧨 灭自证：被摘那条（+1）没有被补挂");
    }

    // ---------------- 本件私有小工具（与宿主里的同名助手不冲突）----------------

    /// <summary>造一条**手牌效果记录**（= 原版 `PlayerHand.activeEffects` 里的一条 `HandEffect`）。
    /// 只登记、不贴牌 —— 与 `EffectResolver.RegisterHandEffectRecord`（`inst == null` 那条路）同形。</summary>
    static CardInstance.HandEffect B98G7Rec(int rid, string source, string payload, string subtype,
                                            bool extrinsic, UnitState acting)
    {
        return new CardInstance.HandEffect
        {
            Op = new EffectOp { Verb = "give", Payload = payload, Source = source },
            Source = source,
            RecordId = rid,
            Expire = CardInstance.HandBuffExpiry.Never,
            Extrinsic = extrinsic,
            ActingUnit = acting,
            Target = new EffectTargetSpec
            {
                Raw = "all " + subtype + " in your hand",
                Side = "own",
                Kind = "unit",
                Count = 0,
                SubtypeFilter = subtype,
            },
        };
    }

    /// <summary>某一份牌身上有没有这条载荷的效果。</summary>
    static bool B98G7HasPayload(CardInstance inst, string payload)
    {
        if (inst == null) return false;
        for (int i = 0; i < inst.HandEffects.Count; i++)
        {
            var e = inst.HandEffects[i];
            if (e != null && e.Op != null && string.Equals(e.Op.Payload, payload, System.StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    /// <summary>按卡名在某一侧棋盘上找那个单位（小格号不保证 ⇒ 扫全场最稳）。</summary>
    static UnitState B98G7UnitByName(BattleContext ctx, int p, string name)
    {
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++)
        {
            var u = board[s];
            if (u != null && u.Card != null && u.Card.Name == name) return u;
        }
        return null;
    }

    /// <summary>那个单位在哪一格（`-1` = 没找到）。</summary>
    static int B98G7SlotByName(BattleContext ctx, int p, string name)
    {
        var u = B98G7UnitByName(ctx, p, name);
        if (u == null) return -1;
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++)
            if (ReferenceEquals(board[s], u)) return s;
        return -1;
    }

    /// <summary>某一侧棋盘上的**非督军单位**个数。</summary>
    static int B98G7CountMinions(BattleContext ctx, int p)
    {
        int n = 0;
        var board = ctx.Players[p].Board;
        for (int s = 0; s < board.Length; s++)
        {
            var u = board[s];
            if (u != null && !u.IsWarlord) n++;
        }
        return n;
    }

    static string B98G7Events(BattleContext ctx)
    {
        return ctx == null ? "" : string.Join("\n", ctx.Events);
    }
}
