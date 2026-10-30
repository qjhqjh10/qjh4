// B98G4_Assert.cs — 第九十八会话 · 执行代理 `B98_G4`（2026-10-29 · 波 5b · 战斗表现层写手）
//
// ============================ 它盯的是什么 ============================
// 本代理在 `CardPresentation/Battle/BattleDriver.cs` 落地的 6 件（判据 → `报告/B98_G4.md`）：
//
//   · **账 240** `BattleManager.CancelBattleActionFromQueue` —— `createHandCard(0x17)` 被撤时
//     **把那张卡体 `SetActive(false)`**（原版 `CancelBattleActionFromQueue.c:44-52`）。引擎半 =
//     `ActionQueue.OnCancelCreateHandCard`（`B97_W38` 已落），这里接**订阅**（`Begin()` 里 `=` /
//     `ForEachStaticHook` 里置 null）+ 按 `UnitState` **身份**找 `CardView`。
//   · **账 254 / 255 ②** `ChoiceOfCardEnemy` / `ChoiceOfCardPlayer` —— 面板的候选**照用引擎
//     `GetChoiceOptions` 等价物算好的那一份**（`ctx.ChoiceOptions` + `ChoiceOptionsSource`），
//     ⛔ 不再自己抽一边（原来走 `ctx.ShowRng` 是另一条流）。
//   · **账 233** `BattleManager.CanPlayCard` —— `showTips` 那一半的**收口核对**（提示 = `NotifyCantDoAction`，
//     四档拒因 1:1；`actionAlreadyStarted` 引擎侧只对齐签名）。本件**零代码改动**，只落断言。
//   · **账 94** `AiScripted.ExecuteAction(Campaign)` —— `ICampaignScriptHost` 的**真实宿主**在
//     `BeginBattleAfterSetup` 建（原来只有 `NullCampaignScriptHost` 出声兜底）。
//   · **召唤语音闸**（`B97_R10` §三·3）—— `SpeakFor` 的 `case EvtKind.Deploy` 现在与 `PlaySummonDelayedVfx`
//     **共用** `SummonAnimGate`（`ShouldTriggerSummonAnim`）。
//   · **账 1037** `EntityScript.GetThisCardPlayedDamage` —— `BattleBoardPreview` 的 `other` 不再**只认战术卡**
//     （手牌里的**单位卡**也走 `ThisCardPlayedDamageEntries` 那一支）。
//
// ============================ 🔴 覆盖边界（如实记，⛔ 不假装盖住了）============================
//   · **召唤语音闸会真的改可观测行为**（效果召唤 / `ephemeral` / `ambush` 不再播登场语音）。
//     这里断的是**闸本身**（`SummonAnimGate` 五档）；「`SpeakFor` 真的调了它」这条**只能靠源码钉**
//     （`grep -c "if (!ShouldTriggerSummonAnim(e, out _)) return;" BattleDriver.cs` = 1）+
//     **真机听感验收**（声音是删掉一类，最容易「删多了没人发现」）。
//   · **账 1037 的数值**要真机核（预览是不是画在正确的那一格上）；这里断的是「哪一支算式被走到」。
//   · **账 94 接上仍零可观测**：战役脚本数据本地不存在 ⇒ 这里只断「宿主不是 `Null`」。
//   · 账 240 **今天休眠**：全仓 `actionType: 0x17` 入队点 = 0 ⇒ 这里用手工调用验副作用，不是验真链路。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B98G4_Assert.CheckB98G4(driver);`  —— 参数可为 null（内部按 `FindFirstObjectByType` 兜）。
//   · 前置硬检查 C0/C0b **取不到就红**（⛔ 不静默跳过）；之后才有 `if (drv == null) return;` 的早退。
//   · 本件**会临时改三样东西并全部还原**：① 一张卡视图的 `activeSelf`；② `ctx.PlayingCard` / `ctx.ActingUnit`
//     （换回来）；③ 一个单位的 `ephemeral` / `ambush` 关键词（用完摘掉）。
//     ⚠️ **唯一没还原的是 `ctx.ChoiceOptions`**（`SetChoiceOptions` 是 `internal`、编辑器程序集调不到）——
//     它被留成本次抽出的那一份；这是**良性**的：它仍是**同一候选池里合法的一次随机抽取**，而面板只在
//     `ChoiceOptionsSource` 命中时才用它（见 `SharedChoiceOptionsFor`）。跑在 `Run()` 方法尾 ⇒ 之后没有别的断言依赖它。
//   · 建议接在宿主 `Run()` 的**方法尾聚合行**（之后没有别的断言），同 `B97W3_Assert` 的接法。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B98_G4` 的断言口（账 240 / 254 / 255 / 233 / 94 / 1037 + 召唤语音闸）。</summary>
public static class B98G4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[G4] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    /// <summary>唯一入口。</summary>
    public static void CheckB98G4(BattleDriver drv = null)
    {
        MenuCheck.Section(_sink, "G4：账 240 / 254 / 255 / 233 / 94 / 1037 + 召唤语音闸");
        if (drv == null) drv = Object.FindFirstObjectByType<BattleDriver>();
        Tr(drv != null, "C0（前提）取得到 `BattleDriver`（⛔ 取不到就红，不静默跳过）");
        if (drv == null) return;
        Tr(drv.Ctx != null, "C0b（前提）这局已经打起来（`Ctx != null`）");
        if (drv.Ctx == null) return;

        Check240CancelCreateHandCard(drv);
        Check254PanelSharedOptions(drv);
        Check233CantDoTips(drv);
        Check94CampaignHost(drv);
        CheckSummonVoiceGate(drv);
        Check1037PlayedDamagePreview(drv);

        Debug.Log(_sink.P + "G4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ==================================================================
    //  账 240：`createHandCard(0x17)` 撤销 ⇒ 那张卡体 `SetActive(false)`
    //    判据 = `BattleManager__CancelBattleActionFromQueue.c:44-52`
    // ==================================================================
    static void Check240CancelCreateHandCard(BattleDriver drv)
    {
        MenuCheck.Section(_sink, "240 · `CancelBattleActionFromQueue` 的 `SetActive(false)`（表现层那一跳）");

        // 前提 ①：`Begin()` 里订阅上了（`DetachStaticHooks` 之后会是 null ⇒ 那才是缺陷）
        Tr(ActionQueue.OnCancelCreateHandCard != null,
           "240a（前提）`ActionQueue.OnCancelCreateHandCard` 已经有人订阅（= `BattleDriver.Begin()` 那一行）"
         + "｜🧨 没订阅 ⇒ 引擎那边会退回「出声 + 计数」的待接点（账 240 原文）");

        // 前提 ②：得有一张**此刻是激活的**卡视图（它是这条副作用的承受方）。
        // 🔴 2026-10-30（`B100_F24` · `报告/B100_X3` #22 落地 · 铁律 11「夹具自建，⛔ 别依赖宿主现场」）：
        //   原来这里**只找棋盘单位** ⇒ 宿主现场「棋盘有单位、但没有激活视图」时整段没验到（含 240c~240e）。
        //   而账 240 的原件其实是**手牌那一档**：`actionType 0x17 = createHandCard` = 造一张**手牌**，
        //   原版撤它时关的正是**那张手牌**的卡体 ——
        //   `BattleManager__CancelBattleActionFromQueue.c:44-52`：`targetCard.gameObject.SetActive(false)`。
        //   我们的等价读口 `FindCardViewOfUnit` 三档顺序是「我方棋盘 → 敌方棋盘 → **我方手牌**」
        //   （`BattleDriver.cs:38658-38670`，手牌那档按 `CardView.Inst` 身份比）⇒ 手牌档是**原件路径**、
        //   棋盘档是顺带覆盖 ⇒ 先棋盘、取不到就退手牌，两条路都验同一句产品代码。
        //   ⚠️ `CardView.Inst` 是 `CardInstance`（`CardView.cs:1276`），`UnitState.Instance` 才是同一样东西。
        var ctx = drv.Ctx;
        CardView pick = null;
        UnitState pickUnit = null;
        string pickFrom = "（没找到）";
        var views = Object.FindObjectsByType<CardView>(FindObjectsSortMode.None);
        for (int side = 0; side < 2 && pick == null; side++)
        {
            var ps = ctx.Players[side];
            if (ps == null || ps.Board == null) continue;
            for (int slot = 0; slot < ps.Board.Length && pick == null; slot++)
            {
                var u = ps.Board[slot];
                if (u == null || u.Instance == null) continue;
                for (int i = 0; i < views.Length; i++)
                {
                    var v = views[i];
                    if (v == null || v.Inst == null || !ReferenceEquals(v.Inst, u.Instance)) continue;
                    if (!v.gameObject.activeSelf) continue;
                    pick = v; pickUnit = u; pickFrom = "棋盘 side=" + side + " slot=" + slot; break;
                }
            }
        }
        if (pick == null)
        {
            // 退路：手牌里那张「刚被造出来」的卡（= 0x17 的字面语义）。视图的 `Inst` 就是它的实例身份，
            // 这里只是按 `UnitState` 的形状把它交给产品那个口（`FindCardViewOfUnit` 只读 `Instance`）。
            var hand = drv.HandViewsForTest;
            for (int i = 0; hand != null && i < hand.Count && pick == null; i++)
            {
                var h = hand[i];
                if (h == null || h.Inst == null || !h.gameObject.activeSelf) continue;
                pick = h;
                pickUnit = new UnitState(h.Inst, false);
                pickFrom = "手牌 idx=" + i + "「" + (h.Inst.Card != null ? h.Inst.Card.Name : "?") + "」";
            }
        }
        Tr(pick != null && pickUnit != null,
           "240b（前提）有一张**此刻是激活的**卡视图当承受方（先棋盘、取不到退手牌 —— 0x17 的字面语义是"
         + "「造一张手牌」，原版撤的是**那张手牌**的卡体）｜取到：" + pickFrom
         + "｜🧨 两条路都取不到 ⇒ 240c~240e 没验到（⛔ 别读成通过）");
        if (pick == null) return;

        bool was = drv.CancelCreateHandCardForTest(pickUnit);
        Tr(was, "240c（前提）调用前那张视图**确实是激活的**（`activeSelf == true`）｜承受方 = " + pickFrom);
        Tr(!pick.gameObject.activeSelf,
           "240d ★ 撤一条 `createHandCard(0x17)` ⇒ 那张卡体 **`SetActive(false)`**｜承受方 = " + pickFrom
         + "（原版 `targetCard.gameObject.SetActive(false)`；🧨 方法里没真的关 / `FindCardViewOfUnit` 认不出它 ⇒ 红）");
        pick.gameObject.SetActive(true);                 // 还原现场（这一段只验副作用，不留痕）
        Tr(pick.gameObject.activeSelf, "240e（还原）把那张视图重新激活（本段不留副作用）");
    }

    // ==================================================================
    //  账 254 / 255 ②：面板的候选 = 引擎 `GetChoiceOptions` 算好的**同一份**
    //    `ctx.ChoiceOptions` + `ctx.ChoiceOptionsSource`（`B97_W21` 落）；面板只**读**、不再自己抽
    // ==================================================================
    static void Check254PanelSharedOptions(BattleDriver drv)
    {
        MenuCheck.Section(_sink, "254/255 ② · 面板候选 == 引擎 `ChoiceOptions`（逐项且同序）· 随机流只消耗一处");

        var ctx = drv.Ctx;
        var pool = new List<CardDef>(ctx.CardPool);
        Tr(pool.Count > 0, "254a（前提）卡池非空（`ctx.CardPool`）");
        if (pool.Count == 0) return;

        // 找一张「**会开面板**」的卡（`maxTargets > 0`）—— 全池 979 张是 0 ⇒ 必须挑
        CardDef pending = null;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && pool[i].ChoicesMaxTargets > 0) { pending = pool[i]; break; }
        Tr(pending != null, "254b（前提）卡池里有一张 `ChoicesMaxTargets > 0` 的卡（原版 `choicesDefinition.maxTargets`）");
        if (pending == null) return;

        // `BuildChoiceOptions` 的 `ChoiceLimitOf` 读 `ActingUnit?.Card ?? PlayingCard` ⇒ 先摆成这一张
        var savedPlaying = ctx.PlayingCard;
        var savedActing = ctx.ActingUnit;
        ctx.ActingUnit = null;
        ctx.PlayingCard = pending;
        try
        {
            int idx0 = ctx.RandomNumbersIndex;
            var opts = RuleCore.BuildChoiceOptions(ctx, pool);
            int advanced = ctx.RandomNumbersIndex - idx0;

            int want = pending.ChoicesMaxTargets < pool.Count ? pending.ChoicesMaxTargets : pool.Count;
            C(opts.Count, want, "254c（前提）抽出的张数 == `min(池, maxTargets)`（" + pending.Id + " ⇒ " + want + "）");
            C(advanced, opts.Count, "254d ★ 引擎随机流**只前进 `options.Count` 格**（原版 `GetRandomInt` 每抽一次推进一格）");
            Tr(ReferenceEquals(ctx.ChoiceOptionsSource, pending),
               "254e（前提）`ctx.ChoiceOptionsSource` == 这次问的那张卡（面板用它判「是不是我问的那一次」）");

            // ★ 本体：面板拿到的就是这一份（**逐项且同序**，⛔ 不是再抽一遍）
            var shown = drv.PanelSharedChoiceOptionIdsForTest(pending, pool);
            Tr(shown != null, "254f ★ 面板走「照用引擎那一份」这条支（`null` = 退回自己抽 = 账 254 ② 原文）");
            if (shown != null)
            {
                bool same = shown.Length == opts.Count;
                if (same)
                    for (int i = 0; i < shown.Length; i++)
                        if (!string.Equals(shown[i], opts[i] != null ? opts[i].Id : null,
                                           System.StringComparison.Ordinal)) { same = false; break; }
                Tr(same, "254g ★ 面板候选 **== `ctx.ChoiceOptions` 逐项且同序**（" + shown.Length + " 项，🧨 面板自己再抽一边 ⇒ 红）");
            }

            // ★ 面板这一步**不消耗随机流**（消耗只发生在引擎那一处）
            int idx1 = ctx.RandomNumbersIndex;
            drv.PanelSharedChoiceOptionIdsForTest(pending, pool);
            C(ctx.RandomNumbersIndex, idx1, "254h ★ 「面板取候选」这一步**一格随机都不消耗**（抓「面板与引擎各抽一次」）");

            // ★ 排序仍在面板（原版玩家那一路一定排）：集合相同、顺序按 `CompareChoiceOptions`
            var sorted = drv.PanelSortedChoiceOptionIdsForTest(pending, pool);
            Tr(sorted != null && sorted.Length == opts.Count, "254i ★ 排序后的候选张数不变（" + (sorted != null ? sorted.Length : -1) + "）");
            if (sorted != null && sorted.Length == opts.Count)
            {
                bool setSame = true;
                for (int i = 0; i < sorted.Length; i++)
                {
                    bool hit = false;
                    for (int j = 0; j < opts.Count; j++)
                        if (opts[j] != null && string.Equals(sorted[i], opts[j].Id, System.StringComparison.Ordinal)) { hit = true; break; }
                    if (!hit) { setSame = false; break; }
                }
                Tr(setSame, "254j ★ 排序只改**摆法**、不改**集合**（原版 `GetChoiceOptions.c:110-113` 的 `Sort`）");
            }

            // ★ 负例：来源卡不是这一张 ⇒ **不许**拿别人的 options（退回旧兜底）
            CardDef other = null;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null && !ReferenceEquals(pool[i], pending)) { other = pool[i]; break; }
            Tr(other != null, "254k（前提）卡池里还有第二张卡（负例用）");
            if (other != null)
                Tr(drv.PanelSharedChoiceOptionIdsForTest(other, pool) == null,
                   "254l ★ 来源卡不是这一张 ⇒ 面板回 `null`（走旧兜底）—— 🧨 去掉 `ChoiceOptionsSource` 那道闸 ⇒ 红");
        }
        finally
        {
            ctx.PlayingCard = savedPlaying;
            ctx.ActingUnit = savedActing;
        }
    }

    // ==================================================================
    //  账 233：`CanPlayCard` 的 `showTips` 那一半（提示 = `NotifyCantDoAction`，四档拒因 1:1）
    //    ⚠️ 本件**零代码改动**：它已由 `X19/P2` / `W63` 接好，这里只把「四档文案确实不同」钉住
    // ==================================================================
    static void Check233CantDoTips(BattleDriver drv)
    {
        MenuCheck.Section(_sink, "233 · `CanPlayCard` 的提示气泡（四档拒因文案 1:1）");
        int[] codes = { RuleCodes.ErrCost, RuleCodes.ErrNotEnoughRoom, RuleCodes.ErrNoTargetAvailable, RuleCodes.ErrNotTurn };
        string[] texts = new string[codes.Length];
        for (int i = 0; i < codes.Length; i++)
        {
            drv.SetLastPlayRejectCodeForTest(codes[i]);
            texts[i] = drv.IllegalActionTextForTest(null);
            Tr(!string.IsNullOrEmpty(texts[i]), "233a-" + i + "（前提）拒因码 " + codes[i] + " 有**人话**文案（⛔ 空串 = 玩家什么都看不到）");
        }
        for (int i = 0; i < texts.Length; i++)
            for (int j = i + 1; j < texts.Length; j++)
                Tr(!string.Equals(texts[i], texts[j], System.StringComparison.Ordinal),
                   "233b ★ 拒因 " + codes[i] + " 与 " + codes[j] + " 的文案**不同**（🧨 四档塌成一档 ⇒ 红）");
        // 「一个合法目标都没有」那种最坏档：退回 `DragToTarget`，⛔ 也不许空
        drv.SetLastPlayRejectCodeForTest(RuleCodes.OK);
        Tr(!string.IsNullOrEmpty(drv.IllegalActionTextForTest(null)),
           "233c ★ 连引擎码都没有那一档也**不静默**（退回旧文案）");
        // 还原：`RuleCodes.OK` 就是「没被引擎拒」的初始值（下一次 `CanDropAtSlot` 也会先清）
    }

    // ==================================================================
    //  账 94：`ICampaignScriptHost` 的真实宿主
    //    ⚠️ 接上仍**零可观测**（战役脚本数据本地不存在）⇒ 只断「不是 `Null` 那个出声兜底」
    // ==================================================================
    static void Check94CampaignHost(BattleDriver drv)
    {
        MenuCheck.Section(_sink, "94 · 战役脚本真实宿主（`ICampaignScriptHost`）");
        Tr(drv.CampaignHost != null, "94a ★ `BeginBattleAfterSetup` 建了宿主（`null` = 那一行没跑 / 没接）");
        if (drv.CampaignHost != null)
            Tr(!(drv.CampaignHost is NullCampaignScriptHost),
               "94b ★ 宿主**不是** `NullCampaignScriptHost`（那一档 = 出声兜底 = 等于没接）");
    }

    // ==================================================================
    //  召唤语音闸（`B97_R10` §三·3）：`SummonAnimGate` 的五档
    //    判据 = `CardScript._MinionPlayedIntoField_d__314.MoveNext.c:186-207`（state 3 的两道闸）
    //    ⚠️ 「`SpeakFor` 真的调了它」+「听感」只能靠源码钉 + 真机（见文件头）
    // ==================================================================
    static void CheckSummonVoiceGate(BattleDriver drv)
    {
        MenuCheck.Section(_sink, "召唤语音闸 · `SummonAnimGate`（登场语音与 summon 动画共用这一道）");
        var ctx = drv.Ctx;
        UnitState u = null;
        var me = ctx.Players[drv.MySeat];
        if (me != null && me.Board != null)
            for (int i = 0; i < me.Board.Length; i++)
                if (me.Board[i] != null) { u = me.Board[i]; break; }
        if (u == null)
        {
            var foe = ctx.Players[1 - drv.MySeat];
            if (foe != null && foe.Board != null)
                for (int i = 0; i < foe.Board.Length; i++)
                    if (foe.Board[i] != null) { u = foe.Board[i]; break; }
        }
        Tr(u != null, "SV0（前提）场上有一个单位（本段拿它验闸）");
        if (u == null) return;

        bool hadEphemeral = u.Has(BattleDriver.EphemeralKeywordForTest);
        bool hadAmbush = u.Has(KeywordTable.Ambush);
        Tr(!hadEphemeral && !hadAmbush, "SV1（前提）这个单位**既不是** `ephemeral` **也不是** `ambush`（否则下面 3 条判不了）");
        if (hadEphemeral || hadAmbush) return;

        Tr(BattleDriver.SummonAnimGate(DeploySource.HandPlayed, u),
           "SV2 ★ 从手牌打出 ∧ 没有 ephemeral/ambush ⇒ **要播登场语音**（原版过了闸才 invoke `+0x138`）");
        Tr(!BattleDriver.SummonAnimGate(DeploySource.Summon, u),
           "SV3 ★ 效果召唤（`Summon` / `Reanimate` / `AutoPlayFromHand`）⇒ **不播**（原版 `shouldTriggerSummon` 假）");
        Tr(!BattleDriver.SummonAnimGate(DeploySource.HandPlayed, null),
           "SV4 ★ 取不到单位 ⇒ **不播**（原版那时也摆不到「卡的位置」上）");

        u.AddKeyword(BattleDriver.EphemeralKeywordForTest, 1);
        Tr(!BattleDriver.SummonAnimGate(DeploySource.HandPlayed, u), "SV5 ★ 挂了 `ephemeral` ⇒ **不播**");
        u.RemoveKeyword(BattleDriver.EphemeralKeywordForTest, 1);
        Tr(BattleDriver.SummonAnimGate(DeploySource.HandPlayed, u), "SV5b（灭自证）摘掉 `ephemeral` ⇒ **又播**（🧨 闸只看常量不看运行时值 ⇒ 红）");

        u.AddKeyword(KeywordTable.Ambush, 1);
        Tr(!BattleDriver.SummonAnimGate(DeploySource.HandPlayed, u), "SV6 ★ 挂了 `ambush` ⇒ **不播**");
        u.RemoveKeyword(KeywordTable.Ambush, 1);
        Tr(BattleDriver.SummonAnimGate(DeploySource.HandPlayed, u), "SV6b（灭自证）摘掉 `ambush` ⇒ **又播**");
    }

    // ==================================================================
    //  账 1037：`BattleBoardPreview` 的 `other` 不再**只认战术卡**
    //    ⚠️ 数值要真机核；这里断「哪一支算式被走到」+ 引擎取数口自洽
    // ==================================================================
    static void Check1037PlayedDamagePreview(BattleDriver drv)
    {
        MenuCheck.Section(_sink, "1037 · `GetThisCardPlayedDamage`（手牌里的**单位卡**也能出预览）");
        var ctx = drv.Ctx;
        int me = drv.MySeat;

        // 前提：我方棋盘上有一格单位（预览的 `self`）
        int selfSlot = -1;
        var ps = ctx.Players[me];
        if (ps != null && ps.Board != null)
            for (int i = 0; i < ps.Board.Length; i++)
                if (ps.Board[i] != null) { selfSlot = i; break; }
        Tr(selfSlot >= 0, "1037a（前提）我方棋盘上有一个单位（预览的 `self`）");
        if (selfSlot < 0) return;
        // `self` 还得有一张**登记在册的棋盘视图** —— `BattleBoardPreview` 第一句就是
        //   `if (!TryLocateBoardView(self, …)) return info;`（`BattleDriver.cs:34442`），
        //   而它认的是 `_myUnits` 那张表 ⇒ 宿主此刻若还没给棋盘建视图，这一支永远退回假。
        //   `RefreshAll()` 就是产视图那条路（`Place20` 那一族夹具用的同一个口）⇒ 先同步一次再判。
        drv.RefreshAll();
        Tr(drv.BoardViewAt(selfSlot, true) != null,
           "1037b-1（前提）我方那一格有**棋盘卡视图**（`TryLocateBoardView(self)` 认的是 `_myUnits` 那张表）"
         + "｜🧨 没有 ⇒ 1037c/1037d 没验到（别读成通过）");
        if (drv.BoardViewAt(selfSlot, true) == null) return;

        // 前提：手牌里有一张**单位卡**视图（`CardView.Inst` 指向一张 `IsUnit` 的卡实例）
        // 🔴 2026-10-30（`B100_F24` · `报告/B100_X3` #23 落地 · 铁律 11「夹具自建，⛔ 别依赖宿主现场」）：
        //   宿主现场那一刻手牌可能只剩战术卡（日志 `[HANDTEXT] hand=1` = `舰队之主`）⇒ 本段 1037c/1037d
        //   **一次都没验到**。夹具改成两支：① 现场真有单位卡手牌就用它；② 没有就**自建一张**
        //   （`CardView.Create` + 手动挂 `Inst`）—— 产品那条路只读 `other.Inst` / `other.Data` / `other.OwnAttackType`，
        //   而且它判断「是不是在手里」靠的是 `TryLocateBoardView(other, …) == false`（不在棋盘上），
        //   自建这张天生不在棋盘 ⇒ **走的正是「单位卡在手里」那条支**（与原版 `GetThisCardPlayedDamage` 的 `other` 同形）。
        CardView handUnit = null;
        string huFrom = "（没找到）";
        GameObject huRoot = null;
        for (int i = 0; i < ps.Hand.Count; i++)
        {
            var inst = ps.Hand[i];
            if (inst == null || inst.Card == null || !inst.Card.IsUnit) continue;
            var vs = Object.FindObjectsByType<CardView>(FindObjectsSortMode.None);
            for (int k = 0; k < vs.Length; k++)
                if (vs[k] != null && ReferenceEquals(vs[k].Inst, inst)) { handUnit = vs[k]; break; }
            if (handUnit != null) { huFrom = "现场手牌 idx=" + i + "「" + inst.Card.Name + "」"; break; }
        }
        if (handUnit == null)
        {
            CardDef unitDef = null;
            for (int i = 0; i < ctx.CardPool.Count && unitDef == null; i++)
                if (ctx.CardPool[i] != null && ctx.CardPool[i].IsUnit) unitDef = ctx.CardPool[i];
            Tr(unitDef != null, "1037b-0（前提）卡池里找得到一张单位卡（自建夹具要用）");
            if (unitDef != null)
            {
                huRoot = new GameObject("chk_b98g4_1037_unit");
                var huInst = ctx.NewInstance(unitDef);
                var v = CardView.Create(huRoot.transform, BattleDriver.ToCardData(unitDef, unitDef.Faction),
                                        "chk_b98g4_1037_card",
                                        CardFace.Hand);   // 手牌那一档的展示场景（`SyncHand` 建手牌视图用的就是它）
                v.Inst = huInst;                       // ← 产品读的就是这一格（`CardView.Inst`，`CardView.cs:1276`）
                handUnit = v;
                huFrom = "自建夹具「" + unitDef.Name + "」";
            }
        }
        Tr(handUnit != null, "1037b（前提）手牌里有一张**单位卡**的视图（带 `Inst` 引用）｜取到：" + huFrom
                           + "｜🧨 取不到 ⇒ 1037c/1037d 没验到（⛔ 别读成通过）");
        if (handUnit == null) return;

        var info = drv.BoardPreviewAgainstCardForTest(me, selfSlot, handUnit,
                                                     isAttacker: false, changeAttackType: false, formUnityAbility: false);
        Tr(info.Valid,
           "1037c ★ 手牌里的**单位卡**也能出预览（`Valid == true`）｜夹具：" + huFrom + " —— 🧨 把 `BattleBoardPreview` 的 "
         + "`|| !TryLocateBoardView(other, …)` 去掉（退回「只喂战术卡」）⇒ `other` 落不到棋盘 ⇒ `Valid == false` ⇒ 红");
        C(info.OtherIsTactic, false,
           "1037d ★ 单位卡**不点亮**攻击类型底光（`OtherIsTactic` 只对计策卡为真 —— 原版 `.c:44/:132`）");
        if (huRoot != null) Object.DestroyImmediate(huRoot);      // 自建夹具用完就拆（不留现场）

        // 引擎取数口自洽：`ThisCardPlayedDamage` 就是逐条 entries 的和（两处必须同一份）
        int checkedCards = 0, bad = 0;
        for (int i = 0; i < ctx.CardPool.Count && checkedCards < 3; i++)
        {
            var c = ctx.CardPool[i];
            if (c == null || string.IsNullOrEmpty(c.Desc)) continue;
            var ents = RuleCore.ThisCardPlayedDamageEntries(ctx, c);
            int sum = 0;
            for (int k = 0; k < ents.Count; k++) sum += ents[k];
            if (sum != RuleCore.ThisCardPlayedDamage(ctx, c)) bad++;
            checkedCards++;
        }
        Tr(checkedCards > 0, "1037e（前提）抽到了 " + checkedCards + " 张带正文的卡（引擎取数口自洽性用）");
        C(bad, 0, "1037f ★ `ThisCardPlayedDamage == Σ ThisCardPlayedDamageEntries`（" + checkedCards + " 张逐张对）");
    }
}
