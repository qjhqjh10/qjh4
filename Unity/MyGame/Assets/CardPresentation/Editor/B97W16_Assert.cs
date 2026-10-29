// B97W16_Assert.cs — 执行代理 `B97_W16`（第九十七会话 · 波 1 补位 · 2026-10-29）
//
// ============================ 它盯的是什么 ============================
//   账 `特效动画:521`（`EntityScript.GetCardEffectsToShow`）这一轮落地的两段：
//     · **S1** `RuleEngine/Core/EffectsToShow.Of()` —— 原版 `activeEffects(+0x108)` 的 7 源只读投影；
//     · **S4** `CardPresentation/Battle/CardDisplayWindow.RowsOfEffects()` —— 投影 → 两列文字
//       ＋ 环境效果卡整组隐藏（`HidesEffectRowsFor` / `SetEffectRows(rows, hideForEnvironmental)`）。
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/GetCardEffectsToShow_施工细则.md` §三 S1/S4
//   ＋ `判据/CardEffectsToShow_判据.md` §一·补 ＋ 报告 `资料/完全复刻/报告/B97_W16.md`。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   `B97W16Assert.Run();`
//   ＋ 两句计数转发（照 `B94B18Assert` 的先例）：
//     `pass += B97W16Assert.Pass; fail += B97W16Assert.Fail;`
//   · **推荐宿主 = `Editor/BattleScene.cs`（`BattleScene.Run`）** —— C 段要**真建一扇展示窗**
//     （`CardDisplayWindow.Create`），那需要 `LayoutSpace.Cam` / `HudCanvas` 已经立起来（只有战斗宿主有）。
//     接到别的宿主上 **C0 会红**（那是故意的：⛔ 不许静默跳过）。
//   · 本件对宿主状态没有别的假设；A/B 段是纯函数，任何宿主都能跑。
//   · 本件建的 `GameObject`（那扇临时窗 + 它的世界壳）都在 `finally` 里 `DestroyImmediate`；
//     ⛔ 不碰宿主那扇 `driver.CardDisplay`（⛔ 也不碰共享子画布 —— 那是 `HudCanvas` 的，删了会连累生产窗）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   A1 把 `ApplyMarkBody` 的 `markOfChaosType` 恒传 0 ⇒ 四神聚合整条丢掉 ⇒ **A1 红**（Mark 行 0 条）。
//   A2 把 `FindTraitEffect` 的 `MarkOfChaosType` 比较去掉 ⇒ 同神两份并成一条 x2 ⇒ **A2 红**（期望 2 行→1 行）。
//   A3 把 `ShownEffect.Timed` 写死 `true` ⇒ 永久那条断「不含 one turn」时**红**。
//   A5 让 `Of()` 把空项直接丢掉（不占位）⇒ 行数 1 且 `RowsOfEffects` 里没有空串行 ⇒ **A5 / C1 红**。
//   A6 把 `TraitRowText` 退回裸关键词名（`CardText.KeywordDisplay` 直接返回）⇒ **A6 两条同时红**（期望串 + 判别式）。
//   A7 把 `_keywords` 整本搬出来当「影响」行 ⇒ 只有印刷关键词的那张卡**出力 > 0** ⇒ **A7 红**。
//   A8 把 `AddAuraKeyword` 那句 `if (!_auraKwSeq.ContainsKey(keyword))` 去掉（每次都取新戳）⇒ **A8 红**。
//   A9 让 `RowsOfEffects` 不按 `RowGroup` 合并 ⇒ 出 2 行 ⇒ **A9 红**。
//   A10/A11 把 `IsExcludedNormal` / `UnitState.IsEffectListEntry` 删掉 ⇒ 对应那条**红**。
//   B1 把 `SetEffectRows(rows, hide…)` 那半的闸删掉 ⇒ 环境卡那档 `EffectRowCount` 不为 0 ⇒ **C2 红**。
//   C1 把空文本项改成「不占槽」（`SetEffectRows` 的 `slot++` 两支改一支）⇒ 后面那行**前移一格** ⇒ **C1 红**。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B97_W16`：账 `特效动画:521` 的 S1（投影）+ S4（消费）断言。
/// <para>⛔ 期望值全是**字面量**（不从被测实现里反算）。</para></summary>
public static class B97W16Assert
{
    static readonly CheckSink _sink = new CheckSink("[W16] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ---- 夹具 -----------------------------------------------------------------------------------

    static List<CardDef> _pool;
    static CardDef Def(string name)
    {
        if (_pool == null) _pool = CardDatabase.Load();
        var d = CardDatabase.Find(_pool, name);
        C(d != null, true, $"（夹具前提）卡池里找得到「{name}」");
        return d;
    }

    static UnitState Unit(string cardName)
    {
        var d = Def(cardName);
        if (d == null) return null;
        var u = new UnitState(d, false);
        return u;
    }

    /// <summary>投影里某一族的条数（字面量断言用）。</summary>
    static int CountOf(List<ShownEffect> list, ShownFamily f)
    {
        int n = 0;
        for (int i = 0; i < list.Count; i++) if (list[i].Family == f) n++;
        return n;
    }

    /// <summary>投影里第一条某族的那一条（没有 ⇒ 默认值）。</summary>
    static ShownEffect FirstOf(List<ShownEffect> list, ShownFamily f)
    {
        for (int i = 0; i < list.Count; i++) if (list[i].Family == f) return list[i];
        return default(ShownEffect);
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "W16：账 特效动画:521 —— `GetCardEffectsToShow` 投影（S1）+ 展示窗消费（S4）");
        SectionA_投影();
        SectionB_文本();
        SectionC_真窗槽位与环境闸();
        Debug.Log(_sink.P + "W16 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 引擎投影（纯）
    static void SectionA_投影()
    {
        MenuCheck.Section(_sink, "A 引擎投影（`EffectsToShow.Of`）：mark 四神 / 永久 vs 一回合 / removeTrait / 空项 / 印刷关键词 / 序戳 / 排除档");

        // ---- A1 / A2：mark 按神各聚一条（连给 blood(10) + resilience(20)）-------------------------
        {
            var u = Unit("Deathwing Terminator");
            if (u != null)
            {
                u.ApplyTraitEffect(KeywordTable.MarkOfChaos, 1, markOfChaosType: EffectsToShow.MarkKhorne, source: "血之黑暗契约");
                u.ApplyTraitEffect(KeywordTable.MarkOfChaos, 1, markOfChaosType: EffectsToShow.MarkNurgle, source: "坚韧黑暗契约");

                var rows = EffectsToShow.Of(u, true);
                C(CountOf(rows, ShownFamily.Mark), 2,
                  "A1 连给 **两份不同神的** 黑暗契约 ⇒ 投影出 **2 条 Mark 行**（Khorne / Nurgle 各一条）");
                int khorne = 0, nurgle = 0;
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].Family != ShownFamily.Mark) continue;
                    if (rows[i].MarkOfChaosType == EffectsToShow.MarkKhorne) khorne = rows[i].TraitValue;
                    if (rows[i].MarkOfChaosType == EffectsToShow.MarkNurgle) nurgle = rows[i].TraitValue;
                }
                C(khorne, 1, "A1-a Khorne 那条的 `TraitValue` = 1");
                C(nurgle, 1, "A1-b Nurgle 那条的 `TraitValue` = 1（**不是** 1 条 x2 混在一起）");
                Tr(!(rows.Count == 1 && FirstOf(rows, ShownFamily.Mark).TraitValue == 2),
                   "A1-c 判别式：**不许**聚成「一条 x2」（那是 `markOfChaosType` 丢失的典型症状）");

                // A2：同神两份 ⇒ 一条、值 2
                var u2 = Unit("Deathwing Terminator");
                if (u2 != null)
                {
                    u2.ApplyTraitEffect(KeywordTable.MarkOfChaos, 1, markOfChaosType: EffectsToShow.MarkKhorne, source: "A");
                    u2.ApplyTraitEffect(KeywordTable.MarkOfChaos, 1, markOfChaosType: EffectsToShow.MarkKhorne, source: "B");
                    var r2 = EffectsToShow.Of(u2, true);
                    C(CountOf(r2, ShownFamily.Mark), 1, "A2 同神连给两份 ⇒ **1 条** Mark 行（不是 2 条）");
                    C(FirstOf(r2, ShownFamily.Mark).TraitValue, 2, "A2-a …而且 `TraitValue == 2`（`traitValue` 累加）");
                }
            }
        }

        // ---- A3：永久 vs 一回合 ---------------------------------------------------------------
        {
            var u = Unit("Deathwing Terminator");
            if (u != null)
            {
                u.AddStatMod("attack", 2);                                  // 永久（无时长）
                u.AddTempBuff(new UnitState.TempBuff
                {
                    Name = "attack", Value = 2, Owner = 0, UntilMyNextTurn = true,
                    Src = "战术卡", SourceCard = "Blind Librarian",
                });
                var rows = EffectsToShow.Of(u, true);
                bool perm = false, timed = false;
                string permText = null;
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].Family != ShownFamily.Stat || rows[i].Attr != "attack") continue;
                    if (rows[i].Value != 2) continue;
                    if (!rows[i].Timed && !perm) { perm = true; permText = CardDisplayWindow.RowsOfEffects(new[] { rows[i] }, true)[0].What; }
                    if (rows[i].Timed) timed = true;
                }
                Tr(perm, "A3 永久 `+2 melee`（`AddStatMod` 无时长）⇒ 投影里有一条 **`Timed == false`** 的 Stat 行");
                Tr(timed, "A3-a 限时 `+2 melee`（`TempBuff`）⇒ 投影里有一条 **`Timed == true`** 的 Stat 行");
                // 🔴 判据 = `GetEffectDesc.c:145-306` 的「五个 until* 任一为真才走 OneTurn 键」
                C(permText, "Get +2 Melee Attack",
                  "A3-b 永久那条右列 == `Get +2 Melee Attack`（**不含** `one turn`）");
                Tr(permText != null && permText.IndexOf("one turn", System.StringComparison.Ordinal) < 0,
                   "A3-c 判别式：永久那条**必不包含** `one turn`（写死 `Timed = true` 时这条红）");
            }
        }

        // ---- A4：`removeTrait` ----------------------------------------------------------------
        {
            var u = Unit("Deathwing Terminator");
            if (u != null)
            {
                u.MarkTraitRemoved("stealth");
                var rows = EffectsToShow.Of(u, true);
                C(CountOf(rows, ShownFamily.RemoveTrait), 1, "A4 `lose Stealth` ⇒ 投影出 **1 条 RemoveTrait 行**");
                C(FirstOf(rows, ShownFamily.RemoveTrait).Trait, "stealth", "A4-a …而且 `Trait == \"stealth\"`");
            }
        }

        // ---- A5：空项（附魔 buffType 15 ⇒ 右列空 ⇒ 占一槽的「洞」）----------------------------------
        {
            var u = Unit("Deathwing Terminator");
            if (u != null)
            {
                u.Instance.ActiveEffects.Add(new ActiveCardEffect
                {
                    BuffType = 15,                       // singleUseEnchantment
                    Source = "Pray Enchantment",
                    Enchantment = new ActiveEnchantment { SpellType = 0 },
                });
                u.AddStatMod("attack", 2);               // 一条有文本的

                var rows = EffectsToShow.Of(u, true);
                C(CountOf(rows, ShownFamily.Enchantment), 1, "A5 卡上挂一条附魔 ⇒ 投影里有 **1 条 Enchantment 行**");

                var ui = CardDisplayWindow.RowsOfEffects(rows, true);
                int holes = 0, nonEmpty = 0;
                for (int i = 0; i < ui.Count; i++)
                {
                    bool empty = string.IsNullOrEmpty(ui[i].Who) && string.IsNullOrEmpty(ui[i].What);
                    if (empty) holes++; else nonEmpty++;
                }
                C(ui.Count, 2, "A5-a 投影 2 条 ⇒ 两列文字也是 **2 行**（空项**不丢**，照样占一行）");
                C(holes, 1, "A5-b …其中 **恰 1 行是空的**（附魔那条 = 原版 `GetEffectDesc` 的 `case 15` 返空串）");
                C(nonEmpty, 1, "A5-c …另 1 行有字（`+2 melee`）");
            }
        }

        // ---- A6：关键词行有「获得 … / …一回合」这层壳（C1）---------------------------------------
        {
            var u = Unit("Deathwing Terminator");
            if (u != null)
            {
                u.ApplyTraitEffect(KeywordTable.Vanguard, 1, buffType: UnitState.BuffTypeAddTrait,
                                   source: "Blind Librarian", timingBits: 1);      // bit0 = untilEndOfTurn
                var rows = EffectsToShow.Of(u, true);
                var one = FirstOf(rows, ShownFamily.Trait);
                C(one.Trait, "vanguard", "A6（前提）投影里有一条 `vanguard` 的 Trait 行");
                C(one.Timed, true, "A6-a …而且它是**限时**那条（`TimingBits` bit0）");

                var wasLang = Loc.Current;
                try
                {
                    Loc.RestoreForTest(AvailableLanguages.English);
                    string en = CardDisplayWindow.RowsOfEffects(new[] { one }, true)[0].What;
                    C(en, "Get Vanguard 1 one turn",
                      "A6-b 英文档：`Get {0} one turn`，`{0}` = 关键词名 ＋ `\" \" + traitValue`（`.c:345-347` 的 `0 < +0x40` 才追加）");

                    Loc.RestoreForTest(AvailableLanguages.Chinese);
                    string zh = CardDisplayWindow.RowsOfEffects(new[] { one }, true)[0].What;
                    C(zh, "获得 先锋 1 一回合", "A6-c 中文档：同一句跟语档走");
                    Tr(zh != "先锋", "A6-d 判别式：**不许**是裸关键词名（退回 `CardText.KeywordDisplay` 直接当整句时这条红）");
                }
                finally { Loc.RestoreForTest(wasLang); }
            }
        }

        // ---- A7：只有**印刷**关键词的单位 ⇒ 0 条（`+0x108` 只装挂上来的，印刷的在 `+0x128`）-----------
        {
            var u = Unit("Ogryn");       // 卡面关键词只有 `Vanguard`（`cards_engine.json` 现读）⇒ 干净的正例
            if (u != null)
            {
                // 前提硬检查：这张卡**确实**带印刷关键词（否则本条是空转绿）
                C(u.Has(KeywordTable.Vanguard), true,
                  "A7（前提）「Ogryn」身上**确实**有印刷关键词 `vanguard`");
                C(CountOf(EffectsToShow.Of(u, true), ShownFamily.Trait), 0,
                  "A7 只有印刷关键词、没有任何挂上来的效果 ⇒ 投影 **0 条 Trait 行**");
                C(EffectsToShow.Of(u, true).Count, 0,
                  "A7-a …而且整张投影是 **0 条**（把 `_keywords` 整本搬出来时这条红）");
            }
        }

        // ---- A8：光环重算前后序戳不变（S2）------------------------------------------------------
        {
            var u = Unit("Deathwing Terminator");
            if (u != null)
            {
                u.AddAuraKeyword("vanguard", 1);
                int seq1 = u.AuraKeywordSeq("vanguard");
                Tr(seq1 > 0, "A8 光环给的关键词拿到了一枚序戳（> 0）");
                u.ClearAuraGrants();                     // = `Auras.Recompose` 的「整份摘掉」
                u.AddAuraKeyword("vanguard", 1);          // = 「重新加回」
                int seq2 = u.AuraKeywordSeq("vanguard");
                C(seq2, seq1, "A8-a 重算前后 **同一枚序戳**（出表顺序不跳队尾）");
                Tr(seq1 != 0, "A8-b 判别式：序戳**非 0**（0 会让这条恒等于 0 而空转绿）");
            }
        }

        // ---- A9：同一次 give 的多条属性 = 一条（`RowGroup` 合并；生产端 S3-c 不在本切片）-----------
        {
            var rows = new List<ShownEffect>
            {
                new ShownEffect { Family = ShownFamily.Stat, Attr = "attack", Value = 2, RowGroup = 7, Source = "Autarch" },
                new ShownEffect { Family = ShownFamily.Stat, Attr = "ranged", Value = 2, RowGroup = 7, Source = "Autarch" },
            };
            var ui = CardDisplayWindow.RowsOfEffects(rows, true);
            C(ui.Count, 1, "A9 `+2 melee and +2 ranged`（同一 `RowGroup`）⇒ **1 行**（原版 = 一条 `ChangeBothAttacks`）");
            C(ui.Count > 0 ? ui[0].What : null, "Get +2 Melee Attack and +2 Ranged Attack",
              "A9-a …右列 = `ChangeBothAttacks`（`{0}`/`{1}` 按近战/远程的实参序填）");
            // 反例：`RowGroup == 0`（今天生产端的真实状态）⇒ 不合并、出 2 行
            var rows2 = new List<ShownEffect>
            {
                new ShownEffect { Family = ShownFamily.Stat, Attr = "attack", Value = 2 },
                new ShownEffect { Family = ShownFamily.Stat, Attr = "ranged", Value = 2 },
            };
            C(CardDisplayWindow.RowsOfEffects(rows2, true).Count, 2,
              "A9-b 反例：`RowGroup == 0` ⇒ **不合并**、出 2 行（这是**今天**的真实行为 —— 生产端 S3-c 还没接）");
        }

        // ---- A10 / A11：两条排除档 --------------------------------------------------------------
        {
            Tr(EffectsToShow.IsExcludedNormal(EffectsToShow.BuffTypeAddTrait, 0, EffectsToShow.OathInAllTurns),
               "A10a `addTrait(3)` ∧ `trait == oathInAllTurns(1276)` ⇒ 被排除（`.c:146-164`）");
            Tr(EffectsToShow.IsExcludedNormal(EffectsToShow.BuffTypeAddTrait, 0, EffectsToShow.OathDouble),
               "A10b …`oathDouble(1277)` 同样");
            Tr(EffectsToShow.IsExcludedNormal(EffectsToShow.BuffTypeAddTrait, 0, EffectsToShow.OathTripleActivation),
               "A10c …`oathTripleActivation(1278)` 同样");
            Tr(!EffectsToShow.IsExcludedNormal(EffectsToShow.BuffTypeAddTrait, 0, 1),
               "A10d 判别式：**普适关键词**（trait 1）不被排除（把整档 addTrait 都删掉时这条红）");

            // 投影侧（名表兜底；池内不可达，见 `EffectsToShow.IsExcludedOathTrait` 的 doc）
            var u = Unit("Deathwing Terminator");
            if (u != null)
            {
                u.ApplyTraitEffect("oathdouble", 1);
                C(CountOf(EffectsToShow.Of(u, true), ShownFamily.Trait), 0,
                  "A10e 注入一条 `oathdouble` 记录 ⇒ 投影里 **0 条** Trait 行（§1.3 排除档）");
            }

            Tr(!UnitState.IsEffectListEntry(UnitState.BuffTypeChangeRace, false), "A11a `changeRace(6)` ⇒ 不进表（§1.4）");
            Tr(!UnitState.IsEffectListEntry(UnitState.BuffTypeAddRace, false), "A11b `addRace(30)` ⇒ 不进表");
            Tr(!UnitState.IsEffectListEntry(UnitState.BuffTypeDaemonHost, false), "A11c `daemonHost(35)` ⇒ 不进表");
            Tr(!UnitState.IsEffectListEntry(UnitState.BuffTypeChoiceEnchantment, false),
               "A11d `choiceEnchantment(16)` 且**没有**任务附魔 ⇒ 不进表");
            Tr(UnitState.IsEffectListEntry(UnitState.BuffTypeChoiceEnchantment, true),
               "A11e 判别式：有任务附魔时 `16` **进表**（把闸写成「恒删 16」时这条红）");

            // 投影侧：buffType 16 且卡上没有 `spellType == 120` 的附魔 ⇒ 被 `Of()` 的第二道过滤删掉
            var u2 = Unit("Deathwing Terminator");
            if (u2 != null)
            {
                u2.Instance.ActiveEffects.Add(new ActiveCardEffect
                {
                    BuffType = 16, Source = "X",
                    Enchantment = new ActiveEnchantment { SpellType = 0 },
                });
                C(CountOf(EffectsToShow.Of(u2, true), ShownFamily.Enchantment), 0,
                  "A11f 投影侧：`buffType 16` 且卡上没有任务附魔 ⇒ **0 条** Enchantment 行");
            }
        }
    }

    // ============================================================ B 文本
    static void SectionB_文本()
    {
        MenuCheck.Section(_sink, "B 两列文字（`RowsOfEffects`）：mark 专名 / removeTrait 拼接 / 环境卡闸谓词");

        // [B98_A10 2026-10-29] 本节标题订正：右列**不再是空串** —— `B98_A6` 起 mark 行右列 = `Battle/Effect/<神名>` 的译文
        // ---- B1：mark 行的左列 = `[[markOfKhorne]]`，`traitValue > 1` 追加 ` (xN)`；右列空 ------------
        //      ⚠️ 上面那句「右列空」是 `B95_FX` 的**错结论**（只查了 `CardTraitDescription/*` 那一族，而 `GetEffectDesc` 走的是
        //      `Battle/Effect/*`），**照录在此作历史**、说明「原来为什么判空」；现行判据见下一条 B1-b。
        {
            var e = new ShownEffect
            {
                Family = ShownFamily.Mark, BuffType = 8,
                MarkOfChaosType = EffectsToShow.MarkKhorne, TraitValue = 2,
            };
            var wasLang = Loc.Current;
            try
            {
                Loc.RestoreForTest(AvailableLanguages.Chinese);
                var r = CardDisplayWindow.RowsOfEffects(new[] { e }, true)[0];
                // ⚠️ `RewriteMarks` 会把 `[[markOfKhorne]]` 展成富文本（`<link=…><nobr><sprite…><b>词</b></nobr></link>`）
                // ⇒ 这里**只断词在里面 + 尾巴是 ` (x2)`**（⛔ 不断整串字面量：那会把 `CardIcons` 的排版也钉死）
                Tr(r.Who != null && r.Who.Contains(Loc.T("Card_Trait/markOfKhorne")),
                   $"B1 mark 行左列含 `Card_Trait/markOfKhorne`（血之黑暗契约；实得「{r.Who}」）");
                Tr(r.Who != null && r.Who.EndsWith(" (x2)", System.StringComparison.Ordinal),
                   $"B1-a …而且尾巴是 `\" (x2)\"`（`traitValue > 1` 才追加；实得「{r.Who}」）");
                Tr(!string.IsNullOrEmpty(r.What) && r.What != "Battle/Effect/Khorne",
                   $"B1-b …右列 = `Battle/Effect/Khorne` 的译文（**不是空串**；`LoadEffect.c:98-101` `goto LAB_18060aaa7` ⇒ `.c:148-153` 写右列），实得「{r.What}」"
                 + "｜🧨 退回 `new EffectRow(who, \"\")` 或改回 `CardTraitDescription/*` 那一族 ⇒ 本条红"
                 + "｜[B98_A10 2026-10-29] 期望值**不写死译文**：中文列原版也带 `[[先锋]]` 这种中文 token（`B98_A6` §六·3）");
            }
            finally { Loc.RestoreForTest(wasLang); }
        }

        // ---- B2：removeTrait 行 = `Battle/Effect/RemoveTrait` + `": "` + 关键词名 ------------------
        {
            var e = new ShownEffect { Family = ShownFamily.RemoveTrait, BuffType = 4, Trait = "stealth" };
            var wasLang = Loc.Current;
            try
            {
                Loc.RestoreForTest(AvailableLanguages.Chinese);
                var r = CardDisplayWindow.RowsOfEffects(new[] { e }, true)[0];
                C(r.What, "移除: 隐身",
                  "B2 `removeTrait` 行 = `Battle/Effect/RemoveTrait`(=移除) ＋ `\": \"` ＋ 关键词显示名（`GetEffectDesc.c:355-366` 的 Concat）");
            }
            finally { Loc.RestoreForTest(wasLang); }
        }

        // ---- B3：环境效果卡闸谓词（`spellType ∈ {210,200}`）-------------------------------------
        {
            var env = Def("Aspect Shrine");
            C(env != null ? env.SpellType : -1, 210, "B3（前提）「Aspect Shrine」的 `spellType == 210`");
            Tr(CardDisplayWindow.HidesEffectRowsFor(env),
               "B3 环境效果卡（`spellType 210`）⇒ 「受到以下影响」整组必须隐藏（`DisplayCardEffects.c:88-105`）");
            var norm = Def("Deathwing Terminator");
            Tr(!CardDisplayWindow.HidesEffectRowsFor(norm),
               "B3-a 判别式：普通单位**不**隐藏（把闸写成恒 true 时这条红）");
        }
    }

    // ============================================================ C 真窗（槽位模型 + 环境闸）
    static void SectionC_真窗槽位与环境闸()
    {
        MenuCheck.Section(_sink, "C 真窗：空项占槽不补位（§1.5）· 环境效果卡整组隐藏（§1.4）");

        var def = Def("Deathwing Terminator");
        CardDisplayWindow win = null;
        try
        {
            try { win = CardDisplayWindow.Create(null); }
            catch (System.Exception ex) { Debug.LogWarning("[W16] C 段建临时展示窗抛了：" + ex); win = null; }

            // C0 = 前置硬检查（⛔ 不静默跳过）
            C(win != null, true,
              "C0 **前提**：临时展示窗建起来了（`CardDisplayWindow.Create` ≠ null）—— 推荐宿主 = `Editor/BattleScene.cs`；"
            + "不成立 ⇒ C1/C2 **没跑**（这一条会红，不许静默跳过）");
            if (win == null) return;

            // ---- C1：空行占槽、后面的行**不往前补** ------------------------------------------------
            {
                var melee = new CardDisplayWindow.EffectRow("Blind Librarian", "Get +2 Melee Attack");
                var health = new CardDisplayWindow.EffectRow("Blind Librarian", "Get +2 Health");
                var rows = new List<CardDisplayWindow.EffectRow>
                {
                    new CardDisplayWindow.EffectRow("", ""),      // ← 洞（原版 `IsTextEmpty ⇒ Disable()`，槽号照走）
                    melee, health,
                };
                var cd = new CardData { id = def.Name, title = def.Name, isUnit = true, type = "troop", faction = def.Faction };
                win.Show(cd, def, rows);

                C(win.EffectRowCount, 2, "C1 3 行里有 1 行是空的 ⇒ `EffectRowCount == 2`（非空条数，不是吃掉的槽数）");
                C(win.EffectWho(0), "", "C1-a 槽 0 = 洞（`EffectWho(0)` 空）");
                C(win.EffectWhat(0), "", "C1-b 槽 0 那一格不画（`EffectWhat(0)` 空）");
                C(win.EffectWhat(1), "Get +2 Melee Attack",
                  "C1-c 槽 1 是**第 2 条**（空项占槽 ⇒ 后面的行**不往前补**；补位时这里会变成空）");
                C(win.EffectWhat(2), "Get +2 Health", "C1-d 槽 2 是**第 3 条**（同上）");
            }

            // ---- C2：环境效果卡整组隐藏 ------------------------------------------------------------
            {
                var env = Def("Aspect Shrine");
                var one = new List<CardDisplayWindow.EffectRow>
                {
                    new CardDisplayWindow.EffectRow("Blind Librarian", "Get +2 Melee Attack"),
                };
                var cd = new CardData { id = env.Name, title = env.Name, isUnit = false, type = "defence", faction = env.Faction };
                win.Show(cd, env, one);

                C(win.EffectRowCount, 0,
                  "C2 环境效果卡（`spellType 210`）即使有行也 ⇒ `EffectRowCount == 0`");
                C(win.EffectsVisible, false,
                  "C2-a …而且那块根节点 `activeSelf == false`（整组强制隐藏，不是「行空的」）");
            }
        }
        finally
        {
            if (win != null)
            {
                // ⛔ 只拆**本件建的那两个节点**（窗本体 + 它的世界壳）—— 共享子画布归 `HudCanvas`，别动
                var shell = win.CardsShell;
                var go = win.gameObject;
                if (shell != null) Object.DestroyImmediate(shell.gameObject);
                if (go != null) Object.DestroyImmediate(go);
            }
        }
    }
}
