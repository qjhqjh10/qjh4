// RuleEngineTest_B101R2.cs — 第一百零一会话 · 波 1 · 执行代理 `B101_R2`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆，见
// `RuleEngineTest_B98G7.cs` / `RuleEngineTest_B99E6.cs`），而宿主 `RuleEngineTest.cs` 有大量账行按
// **行号**落在它上面 ⇒ 新成员放这里 = **零漂移**（铁律 18·b）。本件只放 `TestB88PlayerHandMultiplier`
// 的夹具工具，⛔ 不含任何断言（断言仍在宿主那个方法里，保持原位置）。
//
// 本件修的账：`RuleEngineTest.Run` 的 150642「★ 而且**说出了计数**（80 档 … 计数 = N）」。
// 根因（判据全文 → `资料/完全复刻/报告/B101_R2_四红判据.md` §一·#1）：
//   那条 `mult` 的计数走 `CountEval` 的 `ta=50` 卡池路 ⇒ 逐候选过 `TargetCriteriaEval.MatchesRawCard`
//   （`Core/TargetCriteriaEval.cs:1062`），它**无条件**读两格卡面值：
//     ① `RawCardMeetsTraitCriteria` 的 summonRift 门（`:1036` → `CardDef.HasDefaultTrait`；`cardTraits` 没挂 ⇒ `why`）
//     ② `RarityOf(card)`（`:1098`；`rarity` 认不出 ⇒ `null`）
//   **两格缺一，整条计数就 `null`**（`CountEval.cs:655` → `CountCardList` 直接 `return null`），
//   而 80 档那句出声在 `EffectResolver.cs:14154`（**计数之后**）⇒ 永远发不出来。
//   ⇒ 夹具卡必须显式给这两格；`BattlePool` 的 20 张 filler 也会被抽进敌手 ⇒ 另给一个不带 filler 的组局口。
//   ⛔ 别去改全局 `Unit(...)` / `Tactic(...)` / `Deck(...)` 的默认值 —— 几百条既有断言共用，
//      那些断言的「判不了 ⇒ 放行」前提会一起变（静默翻面）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>🆕 2026-10-30（`B101_R2`）：**卡面值齐全**的夹具卡 —— `rarity` + `cardTraits` 两格都显式给。
    /// <para>判据 = 原版 `FilterMethods__CheckIfRawCardMeetsCriteria.c:18-103`：
    /// `:38` 的 `RawCardMeetsTraitCriteria`（**先于** `traitsFilter == 0` 的短路，查 summonRift）＋
    /// `:74-79` 的 `rarity`（`MeetsStatCriteria` 的 `option == 0` 时由它自己放行，但**先得读得到**）。
    /// 原版每张卡 SO 这两格恒有 ⇒ 我们「没喂」只能如实判不了（`TargetCriteriaEval.cs:1036` / `:1098`）。</para>
    /// <para>`rarity` 给 `"common"`（`RarityOf` 只认 common/rare/epic/legendary/special）；
    /// `cardTraits` 给**空表**＝「已挂、只是没有 trait」（空表 ≠ 没喂，见 `CardDef.AttachCardTraits`）。</para>
    /// </summary>
    static CardDef GaugeCard(string name, string type, int cost, int atk, int hp, string desc = "")
    {
        var c = new CardDef(name, name, type, desc, "common", "Test", cost, atk, hp, 0, null);
        c.AttachCardTraits(new string[0]);
        return c;
    }

    /// <summary>🆕 2026-10-30（`B101_R2`）：`BattlePool` 的「**不带 filler**、可指定督军阵营」版。
    /// <para>为什么要它：`BattlePool`（`RuleEngineTest.cs:2593`）的对手牌库 = `Deck(...)`（`:2579`）里那
    /// **20 张 `filler`**，而 filler 是 `Unit(...)`（`rarity` / `cardTraits` 都没给）—— 它们被抽进手牌后
    /// 会让「手牌级 criteria 计数」整条 `null`（根因见本文件头）。`ProbeBattle`（`:2629`）虽然不带 filler，
    /// 但**不能指定阵营**、且 `cardPool` 恒空。</para>
    /// <para>顺序照 `DeckOf`（`:2601`）的约定：**督军在最前**，`deck[i]` 下标越小越先抽
    /// （`for i 倒序 Add` ＋ 抽牌 `pop_back`）。</para>
    /// </summary>
    static BattleContext CleanBattle(string faction0, string faction1,
                                     CardDef[] deck0, CardDef[] deck1, IList<CardDef> pool)
    {
        var d0 = new List<CardDef> { HeroOf("FixtureWarlord", faction0, 2, 30) };
        for (int i = deck0.Length - 1; i >= 0; i--) d0.Add(deck0[i]);
        var d1 = new List<CardDef> { HeroOf("FixtureWarlord", faction1, 2, 30) };
        for (int i = deck1.Length - 1; i >= 0; i--) d1.Add(deck1[i]);
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool);
    }

    /// <summary>🆕 2026-10-30（`B101_R2`）：`TestB88PlayerHandMultiplier` 的**组局**那一段。
    /// <para>敌方（P1）牌库的前 **4 张**就是后手起手的 4 张（见 `CleanBattle` 的次序约定）：
    /// `2 × unit`（该数）+ `1 × tactic`（该数）+ `1 × hero`（**不该数**：`GSC35` 的 `multiplier` 是
    /// `includeHero = 0 · includeMinions = 1 · includeSpells = 1`）。</para>
    /// <para>🔑 那张 `hero` 是**判别式**：它 `cardType 10` ⇒ 「按 `Hand.Count` 数」会多 1、而按卡种筛不会
    /// ⇒ 这条错写法在这里**必红**（不需要靠「附魔不该被数」那条**与原版相反**的假设 —— 判据
    /// `FilterMethods__CheckIfMeetsCriteria.c:286-300`：`includeSpells` 是 `cardType 20`，
    /// 而我们 39 张 `defence` + 2 张 `enchantment` 的 `cardType` **也都是 20**，见报告 §五·1）。</para>
    /// <para>垫牌只为「开局抽牌不把牌库抽空」：P0 起手 3 + 回合开始 1、P1 起手 4。</para>
    /// </summary>
    static BattleContext B101R2_B88Battle(CardDef gsc35, IList<CardDef> pool)
    {
        var deck1 = new[]
        {
            GaugeCard("EHandUnit1", "unit", 1, 0, 9),
            GaugeCard("EHandUnit2", "unit", 1, 0, 9),
            GaugeCard("EHandTactic", "tactic", 1, 0, 0, "Draw a card"),
            GaugeCard("EHandHero", "hero", 0, 2, 30),
            GaugeCard("EHandPad1", "unit", 1, 0, 1),
            GaugeCard("EHandPad2", "unit", 1, 0, 1),
        };
        var deck0 = new[]
        {
            gsc35,
            GaugeCard("P0Pad1", "unit", 1, 0, 1), GaugeCard("P0Pad2", "unit", 1, 0, 1),
            GaugeCard("P0Pad3", "unit", 1, 0, 1), GaugeCard("P0Pad4", "unit", 1, 0, 1),
        };
        // 督军阵营照改前逐字保留（`BattlePool` 的缺省 = `"Test"`）：GSC35 是 Genestealers 的卡，
        // 但改前就是拿 `"Test"` 督军打出去的（`PlayCard` 不按阵营拒牌）⇒ ⛔ 别顺手改成 "Genestealers"，
        // 那会顺带改变 `warlordFaction` 相关的别的判据（本件不该动）。
        return CleanBattle("Test", "Test", deck0, deck1, pool);
    }

    /// <summary>🆕 2026-10-30（`B101_R2` · `B101_E1` 普查的「督军格」判定）：**断言双方的督军都还在
    /// `BoardSpec.WarlordSlot`（= 4）那一格**。
    /// <para>为什么需要：`RuleCore.cs:655` 开局写 `p.Board[BoardSpec.WarlordSlot] = p.Warlord;`，
    /// 而夹具的 `Place(ctx, p, 4, …)`（`RuleEngineTest.cs:2725`）是**直接给 `Board[slot]` 赋值** ⇒
    /// 后写覆盖 ⇒ 督军被挤出盘面（`Board[4]` 变成一张普通部队、督军 `UnitState` 变孤儿）。
    /// 那 4 处夹具**原有的断言都与督军无关** ⇒ 顶掉了也不会红（= 潜伏缺陷）⇒ 用本条把它变成可断言的。</para>
    /// <para>🧨 判别式：把调用点的格号改回 `4`（或任何一处往 4 号格摆普通部队）⇒ 本行红，
    /// 且文案里打印出**实际坐在 4 号格的是谁**，一眼能看出被谁顶掉的。</para>
    /// <para>⚠️ 只读 `Players[i].Board` / `Players[i].Warlord`（不碰 `ctx.Active`、不走回合推进）⇒ 可放在任何位置。</para>
    /// </summary>
    static void B101R2RequireWarlord(BattleContext ctx, string tag)
    {
        if (ctx == null || ctx.Players == null) { CheckTrue(false, $"★（前提）{tag}：拿不到 `ctx` / `Players` ⇒ 督军格验不了"); return; }
        for (int i = 0; i < ctx.Players.Length && i < 2; i++)
        {
            var ps = ctx.Players[i];
            var at = (ps != null && ps.Board != null) ? ps.Board[BoardSpec.WarlordSlot] : null;
            string what = at == null ? "（空）" : (at.Card != null ? at.Card.Name : "（单位无卡）");
            CheckTrue(ps != null && ReferenceEquals(at, ps.Warlord),
                      $"★（前提·`{tag}`）P{i} 的督军**还在 {BoardSpec.WarlordSlot} 号格**（`RuleCore.cs:655` 写的那一格没被夹具顶掉）"
                    + $"｜🧨 往 {BoardSpec.WarlordSlot} 号格 `Place` 一张普通部队 ⇒ 本行红（实得坐着的是「{what}」）");
        }
    }
}
