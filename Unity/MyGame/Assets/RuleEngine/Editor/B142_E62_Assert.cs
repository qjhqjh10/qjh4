// B142_E62_Assert.cs — 批次 **B142** · 执行写手 `E62`（2026-11-04 · 第一百二十九会话）
//
// ============================ 它盯的是什么 ============================
//   A 组：`B142-QUESTPOINTS-DELAY`（账 `其他_基础设施:586`）—— 原版 `PlayerManager.AddQuestPoints` 的
//        **中间值**：`ManaManager.SetQuestPoints(新值, 阈值, silently:false)` 先把**加完、扣阈值之前**的
//        累计值印上屏（并弹跳/播上升特效），`WaitForSeconds` 之后才**静默**落到余数。我们原来只把余数交给
//        HUD ⇒ `n >= 阈值` 时数字根本不弹。本件钉住引擎侧新带出来的那一位 `BattleEvent.QuestPointsAfterAdd`。
//   B 组：`B142-PLAYEVENT-DEFENSIVE`（账 `泛型与启动链_补读:243`）—— `PlayEvent<,>.StartMatch` 的防御卡分支：
//        三态枚举的值、谓词的三条件、以及「**只认 `== HaveItInInventory`**」这条接线。
//
// ============================ 原版判据（逐句现读）============================
//   · `d:/2/tools/decomp_full/PlayerManager__AddQuestPoints.c`
//       `:51-65` 先把 `questPoints(+0xC4) += newMana`；`:66-71` 比 `新值 >= 阈值`；
//       `:80`（非结算支）/ `:97`（结算支）`ManaManager__SetQuestPoints(新值, 阈值, 0)` —— 第三实参 `0` = `silently:false`
//       = **数字即时到 + `DoTextAnim()` + `TryDOVfx(新值)`**（`ManaTypeHolder.SetMana(int,int,bool)`，
//       `out_cpp2il_dev_ilspy/ManaTypeHolder.cs` / `out_il2csharp/Assembly-CSharp/ManaTypeHolder.cs`）；
//       `:101-104`（结算支）才 `questPoints -= 阈值`；`:121` / `:81` 两档 delay 字面量
//       `DAT_1834b2bb8 = 1.0f`（结算支）/ `DAT_1834b2bb4 = 0.5f`（非结算支）。
//   · `d:/2/tools/decomp_full/PlayerManager._SetQuestPointsWithDelay_d__43__MoveNext.c`
//       `:24-31` `WaitForSeconds(delay)`；`:76` `ManaManager__SetQuestPoints(余数, 阈值, 1)` —— `silently:true`
//       ⇒ **只改文本、不弹不闪**（「降到余数」那一下是静默的）。
//   · `d:/2/tools/decomp_full/DeckUtility__HasDefensiveCards.c` 全文 +
//     `out_il2csharp/Assembly-CSharp/DeckUtility.cs:239-251,759-770`：
//       `deck.defensiveCard != null ⇒ HaveItAssigned(2)`；否则
//       `库存.Any(c ⇒ c.cardArmy == deck.deckHero.cardArmy ∧ c.cardType == Tactic(20) ∧ c.spellType == 210)`
//       `⇒ HaveItInInventory(1)`；否则 `DontHave(0)`。枚举三值出处 = `dump.cs:21826-21832`。
//   · `_tmp_view/decomp_b91/PlayEvent_object__object_.StartMatch__1127df0.c:51-114`：
//       `:51` `HasDefensiveCards(deck, 0)`（那个 `0` **不是形参** —— 原版签名只有 1 个，`dump.cs:21883`；
//       它是 Ghidra 给每个调用多印的 `MethodInfo*` 尾巴，同形 `.c` 里 `ShowPopUp(…,0,0)` 7 形参印 9 实参）；
//       `:52` **只判 `== 1`**；`:112` 0 与 2 都直接 `StartMatchNoCheck`。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   A1 ← 把 `QuestPointsAfterAdd` 改成读**扣完阈值之后**的 `ps.QuestPoints` ⇒ 结算支那几条红（余数 0 ≠ 分子 3）。
//   A2 ← 把它恒留 `-1`（不填）⇒ A1/A2/A3 全红。
//   A3 ← 在 `DoFactionResource` 里把 `questShown` 记在 `QuestPointThreshold(...)` 之后 ⇒ 结算支红（读到的是余数）。
//   A4 ← 给非任务点事件也填一位（例如写 `Amount`）⇒ 哨兵那条红。
//   B1 ← 把三态枚举写成 `bool`（或把 `HaveItAssigned` 的值改成 1）⇒ 值断言红。
//   B2 ← 谓词少一个条件（只比 `spellType`、或只比阵营）⇒ B2-2/B2-3/B2-4 里必有一条红。
//   B3 ← 把 `StartMatch` 那行写回 `!= DontHave`（= 把「已分配」也当成「有库存没分配」）⇒ B3-3 红（开不了战 + 弹错窗）。
//
// ============================ 怎么接（宿主 / 调度台）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B142E62_Assert", B142_E62_Assert.Run); _pass += B142_E62_Assert.Pass; _fail += B142_E62_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**；接线由调度台统一做，本写手不改那个文件。）
//
// ⚠️ 本件**无副作用**：只建裸 `BattleContext`（不建 GameObject、不碰存档、不改静态表）＋
//    一个**闭合泛型**上的假事件实例（用完把 `OnErrorPopup` / `OnConfirmPopup` 还原成原值；
//    `OnConfirmPopup` 是 2026-11-05 `B146-E8` 加的两钮弹窗口，见 B3-2d~j）。
using System.Collections.Generic;
using CardPresentation;      // PlayEvent<,> / PlayEventSaveGame / PlayEventData / DefensiveCardInventoryCheck / PlayEventDefensiveCards
using RuleEngine;            // RuleCore / CardDef / BattleContext / BattleEvent / EvtKind
using UnityEngine;

/// <summary>B142-E62 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B142_E62_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B142-E62：任务点的「中间值」 + `PlayEvent.StartMatch` 的防御卡分支");
        GroupA_QuestPointsIntermediateValue();
        GroupB_DefensiveCardBranch();
        Summary();
    }

    // ==================================================================
    //  A 组 · 任务点中间值（账 `其他_基础设施:586`）
    // ==================================================================
    static void GroupA_QuestPointsIntermediateValue()
    {
        // ---------- A0 哨兵 ----------
        Check(new BattleEvent().QuestPointsAfterAdd, -1,
              "A0 ★ 新事件的默认值是 **-1（没写）**，⛔ 不拿 0 顶（0 是合法的任务点数）"
            + "｜🧨 改成默认 0 ⇒ 本条红");

        // ---------- A1 非结算支：0 → +1（没到阈值）----------
        {
            var ctx = NewCtx();
            Check(RuleCore.QuestPointStepFor(ctx), 3, "（前提）经典档阈值 = 3（`ScenarioVariables.questPointsForTrigger`）");
            int from = ctx.Signals.Count;
            CheckTrue(RuleCore.GrantQuestPointsToOpponentOfCarrier(ctx, 1, 1, "A1"),
                      "A1（前提）给 P1 的「对手」（= P0）加 1 点任务点，执行成功");
            var ev = FirstSignalOfKind(ctx, EvtKind.GainQuest, from);
            CheckTrue(ev != null, "A1（前提）发了一条 `GainQuest` 信号");
            if (ev != null)
            {
                Check(ev.Amount, 1, "A1a `Amount` 仍是 +1（⛔ 别拿它当分子）");
                Check(ev.QuestPointsAfterAdd, 1,
                      "A1b ★ 中间值 = 加完的累计值 1（= 原版 `SetQuestPoints(新值, 阈值)` 打印的分子）"
                    + "｜🧨 恒留 -1 不填 ⇒ 红");
                Check(ev.Player, 0, "A1c `Player` = 收点数那一方（0）");
            }
            Check(ctx.Players[0].QuestPoints, 1, "A1d 余数 = 1（没到阈值 ⇒ 原版不扣）");
            Check(CountSignals(ctx, EvtKind.QuestSpent), 0, "A1e 没到阈值 ⇒ 0 条 `QuestSpent`");
        }

        // ---------- A2 结算支：0 → +3（正好满阈值）----------
        {
            var ctx = NewCtx();
            int from = ctx.Signals.Count;
            RuleCore.GrantQuestPointsToOpponentOfCarrier(ctx, 1, 3, "A2");
            var ev = FirstSignalOfKind(ctx, EvtKind.GainQuest, from);
            CheckTrue(ev != null, "A2（前提）发了一条 `GainQuest` 信号");
            if (ev != null)
                Check(ev.QuestPointsAfterAdd, 3,
                      "A2 ★ **结算支的分子是 3，不是余数 0** —— 这正是我们原来缺的那一拍"
                    + "（原版 `.c:97` 先印 3/3，1.0 s 后 `.MoveNext.c:76` 静默变 0/3）"
                    + "｜🧨 改成读扣完阈值的 `ps.QuestPoints` ⇒ 本条红");
            Check(ctx.Players[0].QuestPoints, 0, "A2b 余数 = 0（3 − 阈值 3）");
            Check(CountSignals(ctx, EvtKind.QuestSpent), 1, "A2c 扣了一个阈值 ⇒ 恰 1 条 `QuestSpent`");
        }

        // ---------- A3 结算支 + 溢出：0 → +5 ----------
        {
            var ctx = NewCtx();
            int from = ctx.Signals.Count;
            RuleCore.GrantQuestPointsToOpponentOfCarrier(ctx, 1, 5, "A3");
            var ev = FirstSignalOfKind(ctx, EvtKind.GainQuest, from);
            CheckTrue(ev != null, "A3（前提）发了一条 `GainQuest` 信号");
            if (ev != null)
                Check(ev.QuestPointsAfterAdd, 5,
                      "A3 ★ 一次加超阈值时分子 = **加完的 5**（⛔ 不是「满阈值 3」—— `.c:66-71` 比的是 `新值 >= 阈值`）"
                    + "｜🧨 把分子夹成 `Min(新值, 阈值)` ⇒ 本条红");
            Check(ctx.Players[0].QuestPoints, 2, "A3b 余数 = 2（5 − 3）");
        }

        // ---------- A4 两次加：分子逐次是当时的累计值 ----------
        {
            var ctx = NewCtx();
            int from = ctx.Signals.Count;
            RuleCore.GrantQuestPointsToOpponentOfCarrier(ctx, 1, 2, "A4a");   // 0 → 2
            RuleCore.GrantQuestPointsToOpponentOfCarrier(ctx, 1, 2, "A4b");   // 2 → 4 → 扣 3 ⇒ 余 1
            var a = FirstSignalOfKind(ctx, EvtKind.GainQuest, from);
            var b = SecondSignalOfKind(ctx, EvtKind.GainQuest, from);
            CheckTrue(a != null && b != null, "A4（前提）两次加各发一条 `GainQuest`");
            if (a != null) Check(a.QuestPointsAfterAdd, 2, "A4a 第一次分子 = 2（没到阈值）");
            if (b != null) Check(b.QuestPointsAfterAdd, 4, "A4b 第二次分子 = 4（2 + 2，扣阈值**之前**）");
            Check(ctx.Players[0].QuestPoints, 1, "A4c 余数 = 1（4 − 3）");
        }
    }

    // ==================================================================
    //  B 组 · `PlayEvent.StartMatch` 的防御卡分支（账 `泛型与启动链_补读:243`）
    // ==================================================================
    static void GroupB_DefensiveCardBranch()
    {
        // ---------- B1 三态枚举的值（`dump.cs:21826-21832` 逐个抄）----------
        Check((int)DefensiveCardInventoryCheck.DontHave, 0, "B1-1 `DefensiveCardInventoryCheck.DontHave` = 0");
        Check((int)DefensiveCardInventoryCheck.HaveItInInventory, 1, "B1-2 `HaveItInInventory` = 1");
        Check((int)DefensiveCardInventoryCheck.HaveItAssigned, 2, "B1-3 `HaveItAssigned` = 2"
            + "｜🧨 改成 bool（或把 2 写成 1）⇒ 本条红 —— 而 `HaveItAssigned` 与 `HaveItInInventory` 混成同一档，"
            + "会让**已经分配好防御卡**的卡组开不了战（原版没有这个行为）");

        // ---------- B2 谓词三条件（`DeckUtility` 的 lambda，`out_il2csharp …DeckUtility.cs:759-770`）----------
        var ok = DefensiveCard("B142E62_DA_Def", "DarkAngels", 210, tactic: true);
        CheckTrue(PlayEventDefensiveCards.IsDefensiveCardOf(ok, "DarkAngels"),
                  "B2-1 同阵营 ∧ `cardType == Tactic(20)` ∧ `spellType == 210` ⇒ 真（三条件全中）");
        CheckTrue(!PlayEventDefensiveCards.IsDefensiveCardOf(ok, "Goff"),
                  "B2-2 ★ 阵营不同 ⇒ 假（原版比的是 `deck.deckHero.cardArmy == c.cardArmy`）"
                + "｜🧨 去掉阵营那一条 ⇒ 红");
        CheckTrue(!PlayEventDefensiveCards.IsDefensiveCardOf(
                      DefensiveCard("B142E62_DA_Normal", "DarkAngels", 0, tactic: true), "DarkAngels"),
                  "B2-3 ★ 同阵营、同 `Tactic`，但 `spellType != 210` ⇒ 假（普通战术卡不是防御卡）"
                + "｜🧨 去掉 `spellType` 那一条 ⇒ 红");
        CheckTrue(!PlayEventDefensiveCards.IsDefensiveCardOf(
                      DefensiveCard("B142E62_DA_Unit", "DarkAngels", 210, tactic: false), "DarkAngels"),
                  "B2-4 ★ 同阵营、`spellType == 210`，但 `cardType != Tactic` ⇒ 假"
                + "｜🧨 去掉 `cardType` 那一条 ⇒ 红");
        CheckTrue(!PlayEventDefensiveCards.IsDefensiveCardOf(null, "DarkAngels"),
                  "B2-5 `null` 卡 ⇒ 假（不抛）");

        // ---------- B3 接线：`StartMatch` **只认 `== HaveItInInventory`** ----------
        //   ✅ 已复刻（2026-11-05 · B146-E8 · 判据 `decomp_b91/PlayEvent.__c__DisplayClass13_0…_StartMatch_b__0.c`
        //      = `StartMatchNoCheck(this, deck)` / `…__c_object__object_._StartMatch_b__13_1.c`
        //      = `WindowsManager.CloseWindow(wm, wm.CurrentWindow(0x58), 0)`）：`HaveItInInventory` 那一支现在是**两钮弹窗**
        //      （原版文案是疑问句「…start the match anyway?」）⇒ 本组改成「假弹窗同时捕获 onOk/onCancel，
        //      并**实调**两个回调」—— ⛔ 不许只断 popup key（那拦不住「两颗钮都挂空」）。
        var prev = PlayEvent<PlayEventSaveGame, B142E62FakeData>.OnErrorPopup;
        var prevConfirm = PlayEvent<PlayEventSaveGame, B142E62FakeData>.OnConfirmPopup;
        string popup = null;
        string confirmKey = null;
        System.Action confirmOk = null, confirmCancel = null;
        PlayEvent<PlayEventSaveGame, B142E62FakeData>.OnErrorPopup = k => popup = k;
        PlayEvent<PlayEventSaveGame, B142E62FakeData>.OnConfirmPopup =
            (k, ok, cancel) => { confirmKey = k; confirmOk = ok; confirmCancel = cancel; };
        try
        {
            // B3-1 `DontHave`（0）⇒ 直接开打
            {
                var e = new B142E62FakeEvent { Defensive = DefensiveCardInventoryCheck.DontHave };
                popup = null; confirmKey = null;
                e.StartMatch();
                CheckTrue(e.Started, "B3-1 `DontHave`(0) ⇒ 直接 `StartMatchNoCheck`（原版 `.c:112`）");
                Check((object)popup, null, "B3-1b 且不弹窗");
                Check((object)confirmKey, null, "B3-1c 且**不走**两钮弹窗");
            }
            // B3-2 `HaveItInInventory`（1）⇒ **两钮**：先不开打；点 OK 照打
            {
                var e = new B142E62FakeEvent { Defensive = DefensiveCardInventoryCheck.HaveItInInventory };
                popup = null; confirmKey = null; confirmOk = null; confirmCancel = null;
                e.StartMatch();
                CheckTrue(!e.Started, "B3-2 `HaveItInInventory`(1) ⇒ **先不开打**（等玩家点钮）");
                Check(confirmKey, "MenuDeck/Error/HasNotAssignedDefensiveCard",
                      "B3-2b 两钮弹窗的文案键逐字 = `MenuDeck/Error/HasNotAssignedDefensiveCard`（原版疑问句那一条）");
                Check((object)popup, null, "B3-2c ⛔ 这一支**不再**走单钮 `OnErrorPopup`（原版这里是两颗钮）");
                CheckTrue(confirmOk != null && confirmCancel != null,
                          "B3-2d ★ 两颗钮**都挂了回调**（OK ⇒ `StartMatchNoCheck` / Cancel ⇒ 关当前窗）"
                        + "｜🧨 少挂一颗 ⇒ 那颗钮点了什么都不发生（原版 `b__0` / `b__13_1` 两颗都在）");
                if (confirmOk != null) confirmOk();
                CheckTrue(e.Started, "B3-2e ★ 点 **OK** ⇒ 照打（`StartMatchNoCheck`）—— ⛔ **不是**「自动分配防御卡」"
                    + "｜🧨 OK 接成关窗 / 什么都不做 ⇒ 本条红");
                Check(e.LastDeckId, "B142E62_fake_deck",
                      "B3-2f OK 那一下带的是**同一个** deckId（`b__0` 捕的就是 closure 里那一副）");
                Check(e.CloseCalls, 0, "B3-2g 点 OK **不**关窗");
            }
            // B3-2h~j Cancel 那一支：新实例，点 Cancel ⇒ 不开打 + 关当前窗（原版 `b__13_1`）
            {
                var e = new B142E62FakeEvent { Defensive = DefensiveCardInventoryCheck.HaveItInInventory };
                confirmKey = null; confirmOk = null; confirmCancel = null;
                e.StartMatch();
                CheckTrue(confirmCancel != null, "B3-2h（前提）Cancel 回调已捕获");
                if (confirmCancel != null) confirmCancel();
                CheckTrue(!e.Started, "B3-2i ★ 点 **Cancel** ⇒ **不开打**");
                Check(e.CloseCalls, 1,
                      "B3-2j ★ 点 Cancel ⇒ 恰调一次「关当前窗」（原版 `b__13_1` = `WindowsManager.CloseWindow(CurrentWindow)`）"
                    + "｜🧨 把 Cancel 接成 OK / 接成什么都不做 ⇒ 本条红");
            }
            // B3-3 `HaveItAssigned`（2）⇒ 直接开打（这条就是「别写成 `!= DontHave`」的闸）
            {
                var e = new B142E62FakeEvent { Defensive = DefensiveCardInventoryCheck.HaveItAssigned };
                popup = null;
                e.StartMatch();
                CheckTrue(e.Started,
                          "B3-3 ★ `HaveItAssigned`(2) ⇒ **直接开打**（原版只判 `== 1`）"
                        + "｜🧨 把接线写成 `!= DontHave` ⇒ 本条红（已分配好防御卡的卡组会被挡在门外 + 弹错窗）");
                Check((object)popup, null, "B3-3b 且不弹窗");
            }
            // B3-4 卡组校验先跑（`InvalidDeck` 那支优先，原版 `.c:43-49`）
            {
                var e = new B142E62FakeEvent { DeckValid = false, Defensive = DefensiveCardInventoryCheck.HaveItInInventory };
                popup = null;
                e.StartMatch();
                CheckTrue(!e.Started, "B3-4 卡组不合法 ⇒ 不开打");
                Check(popup, "MenuDeck/Error/InvalidDeck",
                      "B3-4b 且先报 `InvalidDeck`（防御卡那一跳根本不到）");
            }
        }
        finally
        {
            PlayEvent<PlayEventSaveGame, B142E62FakeData>.OnErrorPopup = prev;   // ⚠️ 泛型类的 static 是**按闭合类型各一份** ⇒ 只还原本夹具这一份
            PlayEvent<PlayEventSaveGame, B142E62FakeData>.OnConfirmPopup = prevConfirm;
        }

        // ---------- B4 真实卡池：13 阵营各自都有防御卡（`spellType == 210`）----------
        //   判据 = `cards_truth.json` 现读 `spellType == 210` 的那些卡；这里只断「本阵营那一条路能走通」，
        //   ⛔ 不断张数（张数会随卡表版本变）。
        CheckTrue(PlayEventDefensiveCards.OwnsAnyDefensiveCard("DarkAngels"),
                  "B4-1 卡池里找得到 DarkAngels 的防御卡（`spellType 210` ∧ `cardType 20`）");
        CheckTrue(!PlayEventDefensiveCards.OwnsAnyDefensiveCard("NoSuchArmy_B142E62"),
                  "B4-2 不存在的阵营 ⇒ 假（不抛）");
        Check(PlayEventDefensiveCards.Check("B142E62_NoSuchDeck_B142E62"), DefensiveCardInventoryCheck.DontHave,
              "B4-3 卡组 id 查不到 ⇒ `DontHave` 放行（原版拿的是 `CardDeck` 对象，不存在这一档；我们出声）");
    }

    // ==================================================================
    //  夹具 · 引擎（与 `B104_W64_Assert.NewCtx` 同一形状：真 `NewBattle` + `shuffle: false`）
    // ==================================================================
    static CardDef Unit(string id)
        => new CardDef(id, id, "unit", "", null, "Test", 1, 0, 1, 0, null);

    static CardDef Hero()
        => new CardDef("B142E62_Warlord", "B142E62_Warlord", "hero", "", null, "Test", 0, 2, 30, 0, null,
                       subtype: "Warlord");

    /// <summary>只给任务点那一族用：双方各一副 filler + 督军，P1 回合、能量充足。</summary>
    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d0.Add(Unit("filler" + i));
        var d1 = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d1.Add(Unit("foefiller" + i));
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
        ToP1Turn(ctx, 1);
        ctx.Players[0].Energy = 12;
        return ctx;
    }

    /// <summary>推进到「P1 的第 n 个回合」（与 `RuleEngineTest.ToP1Turn` 同构）。</summary>
    static void ToP1Turn(BattleContext ctx, int n)
    {
        int target = 2 * n - 1;
        int guard = 0;
        while (ctx.Turn != target || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
            if (++guard > 40) { CheckTrue(false, "回合推进超限，疑似死循环"); return; }
        }
    }

    // ==================================================================
    //  夹具 · 卡（谓词用）
    // ==================================================================
    /// <summary>`spellType` 是 `CardDef` 构造函数的具名可选参数（`Core/CardDef.cs:912`）；
    /// `cardType` 走 `AttachCardType`（`Core/CardDef.cs:6617`，`-1` = 未知 ⇒ 必须显式挂）。</summary>
    static CardDef DefensiveCard(string id, string faction, int spellType, bool tactic)
    {
        var c = new CardDef(id, id, tactic ? "tactic" : "unit", "", "common", faction, 0, 0, 0, 0, null,
                            subtype: tactic ? "defence" : null, spellType: spellType);
        c.AttachCardType(tactic ? CardDef.CardTypeTactic : CardDef.CardTypeMinion);
        return c;
    }

    // ==================================================================
    //  信号读数
    // ==================================================================
    static int CountSignals(BattleContext ctx, EvtKind k)
    {
        int n = 0;
        for (int i = 0; i < ctx.Signals.Count; i++)
            if (ctx.Signals[i] != null && ctx.Signals[i].Kind == k) n++;
        return n;
    }

    /// <summary>从 `from` 起**第一条**该种类的信号（`null` = 没有）。</summary>
    static BattleEvent FirstSignalOfKind(BattleContext ctx, EvtKind k, int from)
    {
        for (int i = from; i < ctx.Signals.Count; i++)
            if (ctx.Signals[i] != null && ctx.Signals[i].Kind == k) return ctx.Signals[i];
        return null;
    }

    /// <summary>从 `from` 起**第二条**该种类的信号（`null` = 没有）。</summary>
    static BattleEvent SecondSignalOfKind(BattleContext ctx, EvtKind k, int from)
    {
        int seen = 0;
        for (int i = from; i < ctx.Signals.Count; i++)
        {
            if (ctx.Signals[i] == null || ctx.Signals[i].Kind != k) continue;
            if (++seen == 2) return ctx.Signals[i];
        }
        return null;
    }

    // ==================================================================
    //  口令 / 汇总
    // ==================================================================
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B142_E62 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B142_E62 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}

// ======================================================================
//  夹具事件（形状照 `CardPresentation/Editor/GameModesCheck.cs:481` 的 `FakePlayEvent`）
//  ⚠️ 只在**本闭合泛型**上用；`OnErrorPopup` 是泛型类的 static ⇒ 按闭合类型各一份，用完还原。
// ======================================================================
sealed class B142E62FakeData : PlayEventData { }

sealed class B142E62FakeEvent : PlayEvent<PlayEventSaveGame, B142E62FakeData>
{
    public override PlayModes EventPlayMode { get { return PlayModes.PracticeLodge; } }

    /// <summary>原版 `abstract GetMatchmakingFilter` —— 本夹具不参与撮合。</summary>
    public override string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost) { return null; }

    // ---- 断言用开关 ----
    public bool DeckValid = true;
    public DefensiveCardInventoryCheck Defensive = DefensiveCardInventoryCheck.DontHave;
    public bool Started;
    /// <summary>`StartMatchNoCheck` 收到的那副卡组 id（两钮弹窗点 OK 时带回来的）。</summary>
    public string LastDeckId;
    /// <summary>`CloseCurrentWindow` 被调了几次（两钮弹窗点 Cancel 的等价口）。</summary>
    public int CloseCalls;

    public override string GetEventDeck() { return "B142E62_fake_deck"; }
    public override bool ValidateEventDeck(string deckId, bool validateOwnership = true) { return DeckValid; }
    protected override DefensiveCardInventoryCheck HasDefensiveCards(string deckId) { return Defensive; }
    public override void StartMatchNoCheck(string deckId) { Started = true; LastDeckId = deckId; }
    /// <summary>原版 `b__13_1` = `WindowsManager.CloseWindow(CurrentWindow)` 的观测口（真实现要窗口层，夹具里改成计数）。</summary>
    protected override void CloseCurrentWindow() { CloseCalls++; }
}
