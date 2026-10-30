// B99C1_Assert.cs — 执行代理 `B99_C1`（第九十九会话 · 波 1 · 组 C）
//
// ============================ 它盯的是什么 ============================
//   组 C 的四条（施工单 → `资料/完全复刻/判据/B99_R3a_施工单_战斗按文件.md` §二 组 C）：
//     · **#34 `G23③`** —— `BattleManager.BroadcastCardCreated` 的**棋盘那一跳**
//       （`RuleEngine/Core/EffectResolverPartial_B97W2.cs` 类尾 `BroadcastCardCreatedBoardHop`）
//       ＋ 回手路径补发（`EffectResolver.cs` 的 `case "hand"`）。
//     · **#4 `C5·Q4`** —— 「候选是卡」的 criteria 消费端（`RuleEngine/Core/CardPoolCriteriaFilter.cs`）。
//     · **`U3·T1` ③** —— `reloadduty` 的 `n++` 改按「清空前捕获的 `ActiveAbilityUsed`」计（`EffectResolver.cs`）。
//     · **`GetCardEffectsToShow` 残差 S3-c** —— 本波**没做**（`StatMod` 缺 `RowGroup` 字段，在 `UnitState.cs`）⇒ 无断言。
//
// 判据（⛔ 别在这里抄第二份）→ `decomp_full/BattleManager__BroadcastCardCreated.c:19-33` ·
//   `decomp_full/CardScript__IsInPlay.c` 全文 · `decomp_full/CardScript__ReactToCardCreated.c:6-9` ·
//   `decomp_full/AbilityLogic__GetTargets.c:1086-1095` + `:123` ·
//   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs` ·
//   `decomp_full/BattleManager__ResolveResetDuty.c:6`。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   A3 把 `BroadcastCardCreatedBoardHop` 的闸写成 `u.CardStateOf(ctx) != CardStateOptions.inPlay`
//      （= 施工单草稿的写法）⇒ `inPlayTransforming(15)` 那张被跳过 ⇒ **A3 红**
//      （原版 `CardScript__IsInPlay()` 认 `{2,3,15,17}` 四档）。
//   A4 把闸整个删掉 ⇒ `waitingToDie(5)` 那张也被算进去 ⇒ **A4 红**。
//   A5 给闸补一句 `!u.IsRemnant`（施工单草稿的「残骸不算」）⇒ 置了 `IsRemnant` 的那张被跳过 ⇒ **A5 红**。
//   B1 把 `CardPoolCriteriaFilter.Apply` 的 `if (ok.Value) kept.Add(c);` 写成无条件 `kept.Add(c);`
//      ⇒ 3 张全留下 ⇒ **B1 红**。
//   B4 把 `if (ok == null) return pool;`（判不了 ⇒ 不筛）改成「当不成立」⇒ 池被筛空 ⇒ **B4 红**。
//   C1 把 `if (!wasUsed) continue;` 改回 `if (!u.DutyUsed) continue;`（旧写法）⇒ `n` 恒 0、
//      日志变「装填了 0 个」⇒ **C1 红**。
//   C2 把 `u.ActiveAbilityUsed = false;` 挪到 `wasUsed` 那句 `continue` **之后** ⇒ 用过主动能力但没 duty
//      的单位漏清 ⇒ **C2 红**。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   在 `CardPresentation/Editor/BattleScene.cs` 的 `BattleScene.Run` 里，紧跟现有那串
//   `B99A3_Assert.Run(); B99A1_Assert.Run();` **同一物理行末尾**追加：
//     ` B99C1_Assert.Run(); pass += B99C1_Assert.Pass; fail += B99C1_Assert.Fail;`
//   （同一物理行追加 ⇒ 行数零变化，⛔ 别新起一行。）
//   · 本件**不建 GameObject**、不碰 `BattleDriver`、不依赖宿主立好的任何画布 ⇒ 任何宿主都能跑；
//     但推荐仍放 `BattleScene.Run`（与同批断言同一行，便于一次跑完）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B99_C1`：组 C 的 #34（棋盘那一跳）· #4（卡池 criteria）· `U3·T1` ③ 断言。</summary>
public static class B99C1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B99C1] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 夹具

    static CardDef Hero(string name, string faction)
    {
        // ⚠️ `subtype` 必须给 `"Warlord"`（真卡 56 个督军全是），否则会掉进「原版没给兵种」那一类。
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static CardDef PlainUnit(string id, int cost)
    {
        var c = new CardDef(id, id, "unit", "", "common", "Test", cost, 1, 1, 0, (string[])null);
        c.AttachCardTraits(new string[0]);      // 挂空表 ⇒ `HasDefaultTrait` 不再「判不了」
        return c;
    }

    /// <summary>一张**带 210 正文**的单位卡（`AttachTriggerOpsByNumber` 是 public，自检专用口）。</summary>
    static CardDef ListenerUnit(string id)
    {
        var c = PlainUnit(id, 1);
        var map = new Dictionary<int, List<EffectOp>>();
        map[210] = new List<EffectOp> { new EffectOp { Verb = "gainhealth", Source = id, Amount = 1 } };
        c.AttachTriggerOpsByNumber(map);
        return c;
    }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B99C1_W0", "Ultramarines") };
        var d1 = new List<CardDef> { Hero("B99C1_W1", "Goff") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static UnitState Put(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>手牌池 + 费 ≥ 1（= 现池 `AM31`/`GOF24`/`UM20` 那三行的形状）。</summary>
    static CardDef.CountSetSpec SpecHandCostAtLeast1()
    {
        return new CardDef.CountSetSpec
        {
            TargetsAffected = 50,                     // inHandFiltered（`GetTargets.c:895`，0x32）
            IncludeHero = 0, IncludeMinions = 1, IncludeSpells = 0, IncludeWhispers = 0,
            SpellType = 0,
            TraitsFilter = 0,
            AttackFilterOption = 0, AttackFilter = 0,
            CurrentHealthFilterOption = 0, CurrentHealthFilter = 0,
            ManaCostFilterOption = 2,                 // StatEqualOrHigher
            ManaCostFilter = 1,
            RarityFilterOption = 0, RarityFilter = 0,
            ArmyFilter = 0, ChosenArmy = 0,
            KeywordFilter = 0, ExcludeRace = 0,
            FilterSpecificCardIds = null,
        };
    }

    static bool SawLog(BattleContext ctx, string needle)
    {
        var ev = ctx.Events;
        for (int i = 0; i < ev.Count; i++)
            if (ev[i] != null && ev[i].IndexOf(needle, System.StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B99C1：组 C —— #34 棋盘那一跳 / #4 卡池 criteria / U3·T1 ③");
        SectionA_BoardHop();
        SectionB_CardPoolCriteria();
        SectionC_ReloadDutyCount();
        Debug.Log(_sink.P + "B99C1 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · #34 棋盘那一跳
    static void SectionA_BoardHop()
    {
        MenuCheck.Section(_sink, "A #34 棋盘那一跳（`BroadcastCardCreatedBoardHop`，原版 `BroadcastCardCreated.c:19-33`）");

        var ctxA = NewCtx();
        var made = ctxA.NewInstance(PlainUnit("B99C1_Made", 1));

        // ---- A1：三个护栏（null / null / created.Card 拿不到）⇒ 0，且不抛 ----
        C(BroadcastCardCreatedBoardHop(null, made), 0, "A1-a `ctx == null` ⇒ 返 0（不筛、不抛）");
        C(BroadcastCardCreatedBoardHop(ctxA, null), 0, "A1-b `created == null` ⇒ 返 0");

        // ---- A2：两张在场监听者（都带 210 正文）⇒ 都入队 ----
        var uA = Put(ctxA, 0, 3, ListenerUnit("B99C1_L0"));
        var uB = Put(ctxA, 1, 5, ListenerUnit("B99C1_L1"));
        int nA2 = BroadcastCardCreatedBoardHop(ctxA, made);
        C(nA2, 2,
          "A2 两张**在场**的 210 监听者（双方各一）⇒ 各入队 1 次，共 2"
        + "｜判据 `decomp_full/BattleManager__BroadcastCardCreated.c:19-33`（遍历 `unitsInPlay`、闸 `IsInPlay()`）");

        // ---- A3：瞬态 15（inPlayTransforming）**仍算在场** —— 灭自证挡 `CardStateOf != inPlay` 那种写法 ----
        Tr(uA.MarkCardState(CardStateOptions.inPlayTransforming), "A3 （前提）把 slot 3 那张标成 `inPlayTransforming(15)`");
        C(uA.CardStateOf(ctxA), CardStateOptions.inPlayTransforming, "A3 （前提）它的 `cardState` 现读 = 15");
        C(BroadcastCardCreatedBoardHop(ctxA, made), 2,
          "A3 `inPlayTransforming(15)` 那张**仍然算在场** ⇒ 仍是 2"
        + "｜判据 `decomp_full/CardScript__IsInPlay.c`（`{2,3,15,17}` 四档）"
        + "｜🧨 把闸写成 `CardStateOf(ctx) != inPlay`（施工单草稿的写法）⇒ 这里变 1 ⇒ 红");
        uA.ClearCardStateMark();

        // ---- A4：`waitingToDie(5)` 不算在场 —— 灭自证挡「闸整个删掉」 ----
        ctxA.DyingThisAction.Add(uB);
        C(uB.CardStateOf(ctxA), CardStateOptions.waitingToDie, "A4 （前提）slot 5 那张现读 `cardState` = 5（`DyingThisAction`）");
        C(BroadcastCardCreatedBoardHop(ctxA, made), 1,
          "A4 `waitingToDie(5)` 那张**不算在场** ⇒ 只剩 1"
        + "｜判据同 `CardScript__IsInPlay.c`（⑤ 不在那四档里）"
        + "｜🧨 把 `if (!u.IsInPlayState(ctx)) continue;` 删掉 ⇒ 这里变 2 ⇒ 红");
        ctxA.DyingThisAction.Remove(uB);

        // ---- A5：残骸**照算**（原版 `IsInPlay()` 只看 cardState，不看 `isRemnant`）----
        //   ⚠️ 施工单 §二 写的「② 残骸不算」**不成立** —— `CardScript__IsInPlay.c` 全文里没有任何
        //   `+0x65 isRemnant` 的判断；残骸在棋盘上 `cardState == 2` ⇒ **算**。这里把它钉住，
        //   ⛔ 免得下一个人照施工单草稿往闸里加 `!u.IsRemnant`（那会造出原版没有的差异）。
        uB.IsRemnant = true;      // 公开字段，自检直接置位（本引擎残骸位由变形路径产生，这里只借位做判别式）
        C(uB.CardStateOf(ctxA), CardStateOptions.inPlay, "A5 （前提）置 `IsRemnant = true` 后它的 `cardState` 仍是 2（在场）");
        C(BroadcastCardCreatedBoardHop(ctxA, made), 2,
          "A5 闸只认 `cardState`、**不认** `isRemnant` ⇒ 残骸也照算（= 原版 `CardScript__IsInPlay.c` 全文）"
        + "｜🧨 给闸补一句 `!u.IsRemnant` ⇒ 这里变 1 ⇒ 红（那是**多出来的**判据，原版没有）");
        uB.IsRemnant = false;
    }

    static int BroadcastCardCreatedBoardHop(BattleContext ctx, CardInstance created)
    {
        return RuleCore.BroadcastCardCreatedBoardHop(ctx, created);
    }

    // ============================================================ B · #4 卡池 criteria 消费端
    static void SectionB_CardPoolCriteria()
    {
        MenuCheck.Section(_sink, "B #4 「候选是卡」的 criteria 筛（`CardPoolCriteriaFilter.Apply`，原版 `AbilityLogic__GetTargets.c:1086-1095`）");

        var ctx = NewCtx();

        // ---- B0：结构前提 —— 只认「候选池本来就是一批卡」那几档 ----
        Tr(CardPoolCriteriaFilter.IsCardPoolTargetsAffected(50), "B0-a `ta=50 inHandFiltered` ⇒ 是卡池档");
        Tr(CardPoolCriteriaFilter.IsCardPoolTargetsAffected(51), "B0-b `ta=51 inHandAndDeckFiltered` ⇒ 是卡池档");
        Tr(CardPoolCriteriaFilter.IsCardPoolTargetsAffected(52), "B0-c `ta=52 inHandAndPlayFiltered` ⇒ 是卡池档");
        Tr(CardPoolCriteriaFilter.IsCardPoolTargetsAffected(60), "B0-d `ta=60 inCemeteryFiltered` ⇒ 是卡池档");
        Tr(CardPoolCriteriaFilter.IsCardPoolTargetsAffected(65), "B0-e `ta=65 inDeckFiltered` ⇒ 是卡池档");
        // 🔴 2026-10-30（`B100_F14` · 报告 `B100_X3` #38/#39）：这两条**原来是反的** —— 实现侧在
        //   `CardPoolCriteriaFilter.IsCardPoolTargetsAffected` 里于**同一批（2026-10-30）删掉了 `case 180` / `case 190`**，
        //   两处一起改才不打架。下面每条都把判据抄进文案（⛔ 不是「调到实现现在返回 false」）。
        Tr(!CardPoolCriteriaFilter.IsCardPoolTargetsAffected(180),
           "B0-f `ta=180 choiceResolution` ⇒ **不是**卡池档（180 的候选池归 `rawCard.choicesDefinition`，"
         + "不套 `abilityLogic.targetCriteria` 那 36 格）"
         + "｜判据 ILSpy `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:25622` `GetChoiceFullPool`："
         + "`:25633 list = choicesDefinition.filterSpecificCards;` + `:25634 if (choicesDefinition.targetsAffected != TargetsAffected.choiceResolution)` "
         + "才从 `targets` 建池 —— 即 `ta==180` 时池子**就是** `filterSpecificCards`（空则回 `cardsTargetedInChosenResolution`），36 格一格不跑"
         + "｜🧨 把 `case 180` 加回去 ⇒ `B99_D1` 实测会让选牌池筛空、`EffectResolver.cs:4556` 越界崩 3 张卡");
        Tr(!CardPoolCriteriaFilter.IsCardPoolTargetsAffected(190),
           "B0-g `ta=190 lastPlayedIfMeetsCriteria` ⇒ **不是**卡池档（原版那一支直接返回，不过末尾那句 `CheckIfMeetsCriteria` 循环）"
         + "｜判据 `decomp_full/AbilityLogic__GetTargets.c:173-182`：`if (iVar6 == 0xbe) { … CemeteryManager__GetCardsPlayed(…) … goto LAB_180818485; }`，"
         + "而 `LAB_180818485`（`:1378-1380`）= `return *puVar13;`；末尾那句筛在 `:1095` `FilterMethods__CheckIfMeetsCriteria(param_3,uVar8,param_5,param_2,0)` "
         + "｜🧨 把 `case 190` 加回去 ⇒ 这一格被 36 格筛一遍（原版没有这一步）");
        Tr(!CardPoolCriteriaFilter.IsCardPoolTargetsAffected(10), "B0-h `ta=10 allFiltered`（棋盘档）⇒ **不是**卡池档");
        Tr(!CardPoolCriteriaFilter.IsCardPoolTargetsAffected(150), "B0-i `ta=150 previousAbilityFiltered` ⇒ **不是**卡池档");
        Tr(!CardPoolCriteriaFilter.IsCardPoolTargetsAffected(20), "B0-j `ta=20 self`（身份档）⇒ **不是**卡池档");
        Tr(!CardPoolCriteriaFilter.IsCardPoolTargetsAffected(CardDef.CountSetSpec.Unknown), "B0-k `ta` 未知 ⇒ **不是**卡池档（不筛）");

        // ---- B1：`ta=50` + 费 ≥ 1 ⇒ 0 费那张被剔 ----
        {
            var pool = new List<CardDef> { PlainUnit("B99C1_C0", 0), PlainUnit("B99C1_C1", 1), PlainUnit("B99C1_C2", 2) };
            var op = new EffectOp { Verb = "choosecard", Source = "B99C1", TargetCriteriaSet = SpecHandCostAtLeast1() };
            var got = CardPoolCriteriaFilter.Apply(ctx, op, pool);
            C(got.Count, 2,
              "B1 `ta=50` + `manaCostFilter=1 + EqualOrHigher` ⇒ 3 张里只剩 2 张（0 费那张被剔）"
            + "｜判据 `FilterMethods__CheckIfRawCardMeetsCriteria.c:55`（费组）"
            + "｜🧨 把 `if (ok.Value) kept.Add(c);` 写成无条件 `kept.Add(c);` ⇒ 变 3 ⇒ 红");
            bool hasZero = false;
            for (int i = 0; i < got.Count; i++) if (got[i].Id == "B99C1_C0") hasZero = true;
            Tr(!hasZero, "B1-b 0 费那张**真的不在**结果里（不是只数对、内容错）");
        }

        // ---- B2：`set == null`（这条 op 没有 criteria 行）⇒ **不筛** ----
        {
            var pool = new List<CardDef> { PlainUnit("B99C1_D0", 0), PlainUnit("B99C1_D1", 1), PlainUnit("B99C1_D2", 2) };
            var op = new EffectOp { Verb = "choosecard", Source = "B99C1", TargetCriteriaSet = null };
            C(CardPoolCriteriaFilter.Apply(ctx, op, pool).Count, 3,
              "B2 `TargetCriteriaSet == null` ⇒ **原样返回**（本仓「没有 criteria 行 = 不筛」的约定，同 `MatchesRawCard`）");
        }

        // ---- B3：`ta=10`（棋盘档）⇒ **不筛**（拿卡面级筛去套棋盘档 = 筛错东西）----
        {
            var spec = SpecHandCostAtLeast1();
            spec.TargetsAffected = 10;
            var pool = new List<CardDef> { PlainUnit("B99C1_E0", 0), PlainUnit("B99C1_E1", 1) };
            var op = new EffectOp { Verb = "choosecard", Source = "B99C1", TargetCriteriaSet = spec };
            C(CardPoolCriteriaFilter.Apply(ctx, op, pool).Count, 2,
              "B3 `ta=10 allFiltered` ⇒ **不筛卡池**（那档的候选是棋盘单位，不是卡）"
            + "｜🧨 把 `if (!IsCardPoolTargetsAffected(ta)) return pool;` 删掉 ⇒ 变 1 ⇒ 红");
        }

        // ---- B4：某一格判不了 ⇒ **整池不筛**（⛔ 不当「不成立」筛空）----
        {
            var spec = SpecHandCostAtLeast1();
            spec.IncludeMinions = CardDef.CountSetSpec.Unknown;      // 卡种闸判不了
            var pool = new List<CardDef> { PlainUnit("B99C1_F0", 0), PlainUnit("B99C1_F1", 1) };
            var op = new EffectOp { Verb = "choosecard", Source = "B99C1", TargetCriteriaSet = spec };
            C(CardPoolCriteriaFilter.Apply(ctx, op, pool).Count, 2,
              "B4 卡种闸判不了（`includeMinions` 未知）⇒ **整池不筛、原样返回**（与 `ApplyTargetCriteriaFilter` 同一条纪律）"
            + "｜🧨 把 `if (ok == null) { … return pool; }` 改成「当不成立」⇒ 池被筛空 ⇒ 红");
        }
    }

    // ============================================================ C · U3·T1 ③（`reloadduty` 的计数口径）
    static void SectionC_ReloadDutyCount()
    {
        MenuCheck.Section(_sink, "C U3·T1 ③：`reloadduty` 的 `n++` 按「清空前捕获的 `ActiveAbilityUsed`」计");

        // 费 = 1（🔴 2026-10-30 `B100_F14` · 报告 `B100_X3` #40：原来写 3 ⇒ 本段是「第 1 回合 · 先手」，
        //   `MaxEnergy = startingMana(1) + TurnCount(1) × manaPerTurn(1) = 2`（`RuleCore.BeginTurnCore` 那句式子 ·
        //   `GameplayVariables.cs:102` 的经典档 1/1/1）⇒ `CanPlayTactic` 的 `needNow > ps.Energy` 回 `ErrCost(2)`
        //   （`EffectResolver.cs:2025`），C1/C2 全是它的级联。本段只验 `reloadduty` 的**计数口径**、与费用无关
        //   ⇒ 取最小值 1；⛔ 别拿「把断言改成期望 ErrCost」来换绿（那是把要验的东西删掉）。
        var reload = new CardDef("B99C1_Reload", "B99C1_Reload", "tactic",
                                 "Reload the Duty abilities of all your units", null, "Test", 1, 0, 0, 0, (string[])null);
        var dutyCard = new CardDef("B99C1_Duty", "B99C1_Duty", "unit", "", "common", "Test",
                                   2, 3, 5, 0, new[] { "Duty" }, subtype: "Infantry");

        // 牌库形状照 `RuleEngineTest.Deck`：督军在最前，手牌**倒序**追加（抽牌是 pop_back）。
        var d0 = new List<CardDef> { Hero("B99C1_W0", "Ultramarines") };
        for (int i = 0; i < 20; i++) d0.Add(PlainUnit("filler" + i, 1));
        d0.Add(reload);
        var d1 = new List<CardDef> { Hero("B99C1_W1", "Goff") };

        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        int guard = 0;
        while (ctx.Turn != 1 || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx); else RuleCore.EndTurn(ctx);
            if (++guard > 40) { CheckTrueFail("回合推进超限（夹具坏了）"); return; }
        }

        int hi = -1;
        var hand = ctx.Players[0].Hand;
        for (int i = 0; i < hand.Count; i++) if (hand[i].Card.Name == "B99C1_Reload") hi = i;
        Tr(hi >= 0, "C0 （前提）手牌里有那张 `Reload the Duty abilities of all your units`");
        if (hi < 0) return;

        var u = Put(ctx, 0, 3, dutyCard);
        u.ActiveAbilityUsed = true;      // 本局用过主动能力
        u.DutyUsed = false;              // ⚠️ **但没走过 duty** —— 旧口径（读 `DutyUsed`）会把它当「本来就能用」跳过
        Tr(u.ActiveAbilityUsed && !u.DutyUsed, "C0 （前提）`ActiveAbilityUsed = 真` ∧ `DutyUsed = 假`");

        C(RuleCore.PlayTactic(ctx, 0, hi, -1), RuleCodes.OK, "C0 （前提）战术卡打得出去");

        Tr(SawLog(ctx, "装填了 1 个单位的职责"),
           "C1 **`n` 按清空前捕获的 `ActiveAbilityUsed` 计** ⇒ 日志写「装填了 1 个单位的职责」"
         + "｜判据 `BattleManager__ResolveResetDuty.c:6`（原版读通用 `+0x4c usedActiveAbility`，`dump.cs:21979`）"
         + "｜🧨 把 `if (!wasUsed) continue;` 改回 `if (!u.DutyUsed) continue;`（旧写法）⇒ `n` 恒 0、日志变「装填了 0 个」⇒ 红");

        Tr(!u.ActiveAbilityUsed,
           "C2 **装填照样清 `+0x4c`**（`DutyUsed` 是假的也照清 —— 原版那一条是无条件写 0）"
         + "｜🧨 把 `u.ActiveAbilityUsed = false;` 挪到 `wasUsed` 那句 `continue` 之后 ⇒ 本行红（用过主动能力但没 duty 的单位会漏清）");
        Tr(!u.DutyUsed, "C3 装填后 `DutyUsed` 仍是假（它本来就是假；⛔ 不是「没清」）");
    }

    static void CheckTrueFail(string msg) { MenuCheck.True(_sink, false, msg); }
}
