// B143_W6_Anim_Assert.cs — 第一百四十三会话 · 执行写手 `B143-W6`（`B142-QUESTSPENT-TIMING`：任务点降向特效的**时机与方向**）
//
// ============================ 它盯的是什么 ============================
//  原版 `ManaTypeHolder.TryDOVfx` **只在 `silently == false` 那一跳跑**：
//    · `AddQuestPoints` 第一跳 `SetQuestPoints(累计值, 阈值, silently:false)` ⇒ 放 `vfxOnValueUp`/`vfxOnValueDown`
//      并更新 `last(+0x70)`；
//    · 第二跳（扣阈值）`SetQuestPoints(余数, 阈值, silently:true)` ⇒ **既不放特效、也不更新 `last`**。
//  ⇒ ① 扣阈值**不播**降向；② 降向只在**下一次加点且新累计 < 上次印出的累计**时播（`+3` 后再 `+1` 就是这条，
//     🔴 旧结论「`vfxOnValueDown` 永不触发」是错的）；③ 值没变不播。
//  我们的发出口 = `BattleDriver.QuestVfxFor`（表现层选件；判据全文在它的 doc 里）。
//
// ============================ 🔴 灭自证（改哪儿 ⇒ 哪条红）============================
//  · A1 ← 把 `QuestVfxFor` 里 `if (e.Kind == EvtKind.QuestSpent) return null;` 删掉（改回 `GenericVfxFor` 那套）
//        ⇒ 扣阈值又发降向 ⇒ A1 红（这正是本账要修的「双播」）。
//  · A2 ← 把 `after < lastShown` 与 `after > lastShown` 两支对调（方向反了）⇒ A2 红 / A3 红。
//  · A2 ← 把方向比较整段删掉、恒回 `VfxMap.GainQuest` ⇒ A2 红（「+3 后再 +1」这条正是推翻旧结论的那条）。
//  · A4 ← 把 `after == lastShown` 那一支也当「上升」⇒ A4 红（原版只在值真的变了才 `TryDOVfx`）。
//  · A6 ← 把 `QuestPointsAfterAdd < 0`（未知累计）当 0 去比 ⇒ A6 红（会把「不知道」演成「降到 0」）。
//
// ============================ 怎么接 ============================
//  宿主 = `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加（追加文本必须在第一个 `//` 之前）：
//      `Guard("B143W6_Anim_Assert", B143_W6_Anim_Assert.Run); _pass += B143_W6_Anim_Assert.Pass; _fail += B143_W6_Anim_Assert.Fail;`
//  ⚠️ 本件**无副作用**：只调纯静态函数 `BattleDriver.QuestVfxFor` + 构造裸 `BattleEvent`（不建 GameObject、不碰存档）。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using CardPresentation.Net;
using RuleEngine;
using UnityEngine;

/// <summary>`B142-QUESTSPENT-TIMING` 的断言（独立类：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B143_W6_Anim_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B143-W6：任务点降向特效的时机与方向（`QuestVfxFor`）");

        // ---------- A0 前提 ----------
        CheckTrue(!string.IsNullOrEmpty(VfxMap.GainQuest) && !string.IsNullOrEmpty(VfxMap.QuestSpent),
                  "A0 前提：`VfxMap.GainQuest` / `VfxMap.QuestSpent` 都必须非空（否则后面两条「选件」断言没有判别力）");

        // ---------- A1 ★ 扣阈值【静默】----------
        // 原版第二跳 `silently:true` ⇒ 不放特效、也不更新 `last` ⇒ 这一拍**什么都不播**。
        CheckVfx(Spent(0), 3, null, 3,
                 "A1 ★ 扣阈值那一下（`EvtKind.QuestSpent`）**不播**任何任务点特效，且 `last` 不变"
               + "｜🧨 把 `QuestVfxFor` 的 `QuestSpent ⇒ null` 删掉 ⇒ 本条红（= 修前的「双播」）");

        // ---------- A2 ★★ 「+3 后再 +1」⇒ 下一次加点播【下降】----------
        // 上一轮印到 3（本轮 +3 打满、阈值被静默扣掉），这一次只 +1 ⇒ 新累计 1 < 3 ⇒ 原版走 `vfxOnValueDown`。
        CheckVfx(GainQuest(0, 1), 3, VfxMap.QuestSpent, 1,
                 "A2 ★★ 「上次印出 3、这次只 +1（新累计 1）」⇒ **播下降**（`Quest UI Trigger`）"
               + "｜🧨 恒回 `GainQuest`（旧结论「永不触发」）⇒ 红；🧨 方向对调 ⇒ 红");

        // ---------- A3 普通加点不播下降 ----------
        CheckVfx(GainQuest(0, 5), 3, VfxMap.GainQuest, 5,
                 "A3 新累计 5 > 上次 3 ⇒ **播上升**（`Quest UI Gain`），不播降向"
               + "｜🧨 恒回降向 / 方向对调 ⇒ 红");

        // ---------- A4 值没变不播 ----------
        CheckVfx(GainQuest(0, 3), 3, null, 3,
                 "A4 新累计 == 上次 3 ⇒ **不播**（原版只在「值真的变了」时才调 `TryDOVfx`）"
               + "｜🧨 把相等那一支并进上升 ⇒ 红");

        // ---------- A5 首帧（没有 last）----------
        CheckVfx(GainQuest(0, 2), int.MinValue, VfxMap.GainQuest, 2,
                 "A5 还没印过任何值（`last == int.MinValue`）⇒ 按**上升**处理并记下 2");

        // ---------- A6 未知累计（-1）----------
        CheckVfx(GainQuest(0, -1), 3, VfxMap.GainQuest, 3,
                 "A6 `QuestPointsAfterAdd == -1`（引擎没填累计值）⇒ 按**上升**处理、且**不动** `last`"
               + "（⛔ 别把「不知道」当成 0 去比 ⇒ 那会演成「降到 0」）");

        // ---------- A7 非任务点事件不插手 ----------
        var notQuest = new BattleEvent { Kind = EvtKind.GainFaith, Player = 0, Amount = 1 };
        CheckVfx(notQuest, 4, null, 4,
                 "A7 其它资源（`GainFaith`）不归这一族 ⇒ 返回 null（照旧走 `GenericVfxFor` 那条路）");

        GroupB_DesiredScale();
        GroupC_ReplayRandomCursor();
        GroupD_RoundClock();
        GroupE_PreBattleTransition();
        GroupF_NetReconnect();
        GroupG_LegacyInput();

        Summary();
    }

    // ============================================================ 夹具与工具
    // ==================================================================
    //  B 组 · `B142-ARENACARDSCALE-DUP`：`ArenaCardScale` 与 `PoseFor` 共用的那一份 `DesiredScale`
    //    判据 = 原版 `BattleManager.GetUnitSizeInPlay.c:16-73`（部队 desiredScale / 督军 heroScale /
    //    其余 `minionInPlayScale 0.75 × 0.86`）+「督军位那一格即使空着也按督军尺寸」。
    //  🧨 灭自证：把 `DesiredScale` 改回旧 `PoseScale3D` 的 `u == null ⇒ InPlayScale(0.75)` ⇒ **B2 红**；
    //    把「先判督军位」那半句删掉（改成只看 `u.IsWarlord`）⇒ **B1 红**。
    // ==================================================================
    static void GroupB_DesiredScale()
    {
        Section("🆕 B143-W6：`DesiredScale`（卡根缩放唯一合成口）");
        bool foe = false;
        float hero = ArenaSlots.HeroScale(foe), card = ArenaSlots.CardScale(foe), inPlay = ArenaSlots.InPlayScale(foe);
        CheckTrue(hero != card && inPlay != card,
                  "B0 前提：`HeroScale` / `CardScale` / `InPlayScale` 三个值互不相同（否则下面三条没有判别力）");

        // ① 督军位 + u == null ⇒ HeroScale（原版那一格永远是督军尺寸）
        Check(BattleDriver.DesiredScale(foe, ArenaSlots.WarlordSlot, null), hero,
              "B1 ★ 督军位（`slot == WarlordSlot`）**即使 `u == null`** 也 ⇒ `HeroScale`"
            + "｜🧨 删掉「先判督军位」、只按 `u` 判 ⇒ 本条红");

        // ② 普通格 + u == null ⇒ CardScale（🔴 这条钉住 `BuildDeployHandoff(null)` 那个回归）
        Check(BattleDriver.DesiredScale(foe, 2, null), card,
              "B2 ★★ 普通格 + `u == null` ⇒ **`CardScale`**（`BuildDeployHandoff` 传 null 走的就是这条）"
            + "｜🧨 改回旧 `PoseScale3D` 的 `u == null ⇒ 0.75 InPlayScale` ⇒ 本条红（那是回归）");

        // ③ 普通格 + 非部队卡 ⇒ InPlayScale（`B142-E26` 的第三支，别被合并时丢掉）
        var nonUnit = new UnitState(new CardDef("B143W6_NonUnit", "B143W6_NonUnit", "tactic", "", "common", "Test", 1, 0, 0, 0), false);
        Check(BattleDriver.DesiredScale(foe, 2, nonUnit), inPlay,
              "B3 普通格 + **非部队卡**（tactic/defence/enchantment）⇒ `InPlayScale`（0.75 / 0.645）"
            + "｜🧨 把第三支删掉（退化成 CardScale）⇒ 本条红");

        // ③b 普通格 + 部队卡 ⇒ CardScale；③c 普通格但单位自称督军 ⇒ HeroScale（旧 `ArenaCardScale` 有、旧 `PoseScale3D` 也有）
        var unit = new UnitState(new CardDef("B143W6_Unit", "B143W6_Unit", "unit", "", "common", "Test", 1, 1, 1, 0), false);
        Check(BattleDriver.DesiredScale(foe, 2, unit), card, "B4 普通格 + 部队卡 ⇒ `CardScale`");
        var selfWarlord = new UnitState(new CardDef("B143W6_Warlord", "B143W6_Warlord", "unit", "", "common", "Test", 1, 1, 1, 0), true);
        Check(BattleDriver.DesiredScale(foe, 2, selfWarlord), hero, "B5 普通格但单位自称督军（`IsWarlord`）⇒ `HeroScale`（两处旧算式都有这一档，别在合并时丢掉）");
    }

    static BattleEvent GainQuest(int player, int afterAdd)
    {
        return new BattleEvent { Kind = EvtKind.GainQuest, Player = player, Amount = 1, QuestPointsAfterAdd = afterAdd };
    }

    static BattleEvent Spent(int player)
    {
        return new BattleEvent { Kind = EvtKind.QuestSpent, Player = player, Amount = 3 };
    }

    static void CheckVfx(BattleEvent e, int last, string want, int wantLast, string msg)
    {
        int newLast;
        string got = BattleDriver.QuestVfxFor(e, last, out newLast);
        Check(got, want, msg + "（选件）");
        Check(newLast, wantLast, msg + "（新的 `last`）");
    }

    // ==================================================================
    //  C 组 · `B143-REPLAY-RANDOM-INDEX` 的**记账口与回放恢复**
    //    数据模型（两列 + `ValidateRandomCursors`）由 `B143-W1b` 落地、`B143_W1b_Assert` 已覆盖；
    //    这里钉的是**BattleDriver 侧**：三个 `Rec*` 都记账、`RecFinish` 转数组、回放侧恢复 + 逐动作对账。
    //  🧨 灭自证：删掉任一 `NoteRandomCursor();` ⇒ C1 红；删掉回放恢复那一句 ⇒ C3 红；
    //    删掉对账那一句 ⇒ C4 红；把「条数不符置 null」删掉 ⇒ C6 红。
    // ==================================================================
    static void GroupC_ReplayRandomCursor()
    {
        Section("🆕 B143-W6：回放随机游标两列（记账口 · 回放恢复 · 逐动作对账）");

        // 结构探针（读源码文本；先例 = `B142_E36_Assert` 的 C 组 / `B143_R1_Assert` 的 G1）
        string src = System.IO.File.ReadAllText("Assets/CardPresentation/Battle/BattleDriver.cs");
        int noteCalls = System.Text.RegularExpressions.Regex.Matches(src, "NoteRandomCursor\\(\\);").Count;
        Check(noteCalls, 3,
              "C1 ★ `NoteRandomCursor();` 必须**恰好 3 处**（`RecAct` / `RecRaw` / `RecMsg` 各一）"
            + "｜🧨 删任一处 ⇒ 那一族动作的游标列缺条 ⇒ 长度对不上 actions ⇒ 整局两列作废");
        CheckTrue(src.Contains("_rec.randomCursorBefore = _recCurBefore.ToArray()")
                  && src.Contains("_rec.randomCursorAfter = _recCurAfter.ToArray()"),
                  "C2 ★ `RecFinish` 必须把临时 List 转成 `ReplayRecord` 的两列（落盘前）");
        CheckTrue(src.Contains("Ctx.RandomNumbersIndex = rec.randomCursorBefore[i]"),
                  "C3 ★★ 回放侧（`ApplyReplayActionAt`）必须**按录像恢复游标**再落地"
                + "｜🧨 删掉 ⇒ 回放用「现在这条流」取数 ⇒ 与录制分叉");
        CheckTrue(src.Contains("ReplayCursorDivergenceForTest = i - 1"),
                  "C4 ★★ 回放侧必须**逐动作对账**（拿上一条的 `after` 比这一条之前的实际游标）"
                + "｜🧨 删掉 ⇒ 分叉了也没人报");

        // 链式口径的**前提**（「动作之外没有东西吃预烧表」）必须可观测：
        //   ① 纯判据 `RandomCursorChainGap(expectedBefore, actual)`；② 断裂时 `RandomCursorChainGapForTest++` + 该条两列写 -1（宁可「不判」也不错判）。
        CheckTrue(!BattleDriver.RandomCursorChainGap(3, 3), "C7a 链没断（`before == actual`）⇒ false");
        CheckTrue(BattleDriver.RandomCursorChainGap(3, 4), "C7b ★ 链断了（上一条落地后是 3，这一条开始时是 4）⇒ true"
            + "｜🧨 把这句改成恒 false（不做核对）⇒ 本条红");
        CheckTrue(!BattleDriver.RandomCursorChainGap(-1, 5) && !BattleDriver.RandomCursorChainGap(5, -1),
                  "C7c 首条 / 未知（`-1`）⇒ **不判**（⛔ 别把「还没记」当成「断链」）"
                + "｜🧨 把 `>= 0` 那两半删掉 ⇒ 本条红");
        CheckTrue(src.Contains("RandomCursorChainGapForTest++") && src.Contains("RandomCursorChainGap(_preActionCursor, after)")
                  && src.Contains("_recCurBefore.Add(-1); _recCurAfter.Add(-1);"),
                  "C8 ★★ 录制时必须**运行期核对**：断链 ⇒ 计数 +1 且该条两列写 `-1`（⛔ 静默恢复错位置是最危险的失败）"
                + "｜🧨 删掉核对/计数/写 -1 任一半 ⇒ 本条红");

        // 契约层：两列必须与 actions 同长，否则**两列一起作废**（半截数组会让回放逐条拿错游标且不报错）
        var rec = new ReplayRecord { actions = new List<MsgAction>() };
        rec.actions.Add(new MsgAction()); rec.actions.Add(new MsgAction());
        rec.randomCursorBefore = new[] { 0, 3 }; rec.randomCursorAfter = new[] { 3, 5 };
        Check(rec.ValidateRandomCursors("B143-W6 断言"), "ok",
              "C5 两列条数 = 动作条数 ⇒ `ValidateRandomCursors` 返回 ok");
        rec.randomCursorAfter = new[] { 3 };
        CheckTrue(rec.ValidateRandomCursors("B143-W6 断言") != "ok" && rec.randomCursorBefore == null && rec.randomCursorAfter == null,
                  "C6 ★ 条数不符 ⇒ 校验**报错并把两列都置 null**（⛔ 半截数组 = 回放逐条错位还不报）"
                + "｜🧨 把「置 null」删掉 ⇒ 本条红");
    }

    // ==================================================================
    //  D 组 · `B134` 回合与计时：**时钟三档**（C3）· **教程/战役不跑表**（C11）· **EventAI 玩家恒先手**（A2）
    //    判据：`ClockManager__GetTotalTime.c:10-36`（① 基准 ② 缩时 ③ matchType 覆盖：80⇒240 / 50⇒600）·
    //    `ClockManager__StartTimer.c:14-33`（教程 100/140、战役 130/190 直接 return）·
    //    `SupportMethods__GetPlayerGoesFirstWithInitiative.c:19-21`（`matchType == 0x50(80)` ⇒ 玩家先手）。
    //  🧨 灭自证：把 240/600 两支对调或删掉 ⇒ D1 红；把 `IsCampaignLike` 删掉（只判教程）⇒ D2 的 130/190 红；
    //    把 `FirstSeatForMatchType` 改成恒 -1 ⇒ D3 的 80 那条红。
    // ==================================================================
    static void GroupD_RoundClock()
    {
        Section("🆕 B143-W6：回合时钟三档 · 教程/战役不跑表 · EventAI 先手");

        // C3 · 三步结构：① 基准 ② 缩时 ③ matchType 覆盖（覆盖在缩时**之后** ⇒ 80/50 不吃缩时）
        Check(BattleDriver.TurnSecondsFor(80, false, 60f, 10f), 240f, "D1a EventAI(80) ⇒ **240 s**");
        Check(BattleDriver.TurnSecondsFor(50, false, 60f, 10f), 600f, "D1b ★ 练习(50) ⇒ **600 s**"
            + "｜🧨 两支对调/删掉 ⇒ 红（`matchType==50` 曾因写死 80 成为死代码）");
        Check(BattleDriver.TurnSecondsFor(200, false, 60f, 10f), 60f, "D1c 遭遇(200) ⇒ 基准 **60 s**");
        Check(BattleDriver.TurnSecondsFor(10, false, 60f, 10f), 60f, "D1d 排位(10) ⇒ 基准 **60 s**");
        Check(BattleDriver.TurnSecondsFor(80, true, 60f, 10f), 240f, "D1e 80 档**不吃**缩时（覆盖在缩时之后，原版同序）");
        Check(BattleDriver.TurnSecondsFor(200, true, 60f, 10f), 10f, "D1f 普通档吃缩时 ⇒ 10 s");

        // C11 · 教程 / 战役**不跑回合时钟**（原版 StartTimer 在置数之前 return）
        CheckTrue(BattleDriver.ClockSuppressedFor((RuleEngine.MatchType)100) && BattleDriver.ClockSuppressedFor((RuleEngine.MatchType)140),
                  "D2a 教程(100) / 教程回放(140) ⇒ 压表");
        CheckTrue(BattleDriver.ClockSuppressedFor((RuleEngine.MatchType)130) && BattleDriver.ClockSuppressedFor((RuleEngine.MatchType)190),
                  "D2b ★ 战役(130) / 场景战役(190) ⇒ 压表"
                + "｜🧨 只判 `IsTutorialMatch`（丢掉 campaign 那一半）⇒ 本条红");
        CheckTrue(!BattleDriver.ClockSuppressedFor((RuleEngine.MatchType)50) && !BattleDriver.ClockSuppressedFor((RuleEngine.MatchType)200) && !BattleDriver.ClockSuppressedFor((RuleEngine.MatchType)10),
                  "D2c 练习 / 遭遇 / 排位 ⇒ **照常走表**（⛔ 别顺手把练习也关了 —— 用户 2026-09-17 只要求保留练习档的**换牌**倒计时）");

        // A2 · EventAI(80) ⇒ 玩家恒先手（原版在 initiative/掷硬币**之前**直接 return true）
        Check(BattleDriver.FirstSeatForMatchType((RuleEngine.MatchType)80), 0, "D3 ★ EventAI(80) ⇒ **玩家（座位 0）恒先手**"
            + "｜🧨 把这一支删掉/恒 -1 ⇒ 红（今天 `GameMode.Dungeon` 没有入口 ⇒ 休眠，但判据已定就得在）");
        Check(BattleDriver.FirstSeatForMatchType((RuleEngine.MatchType)50), -1, "D3b 练习(50) ⇒ 不适用（-1，照旧走 initiative/掷硬币）");
        Check(BattleDriver.FirstSeatForMatchType((RuleEngine.MatchType)200), -1, "D3c 遭遇(200) ⇒ 不适用（-1）");
    }

    // ==================================================================
    //  E 组 · `B134-A10`：换牌 → 开战的两段过渡（**0.2 s** 与 **2.5 s** 分别钉，⛔ 不只钉总和 2.7 s）
    //    判据：`_FinishMulliganFinalPhase_d__351__MoveNext.c:156-236` —— case 3 洗牌 ⇒ `WaitForSeconds(0.2)`
    //     ⇒ case 4 ⇒ `WaitForSeconds(2.5)` ⇒ case 5 `StartBattlePhase`（`VarsGlobal.timeToPositionCard` / `introStageStepTime`）。
    //    相位：`1` = 等 0.2 · `2` = 等 2.5 · `3` = 开战。
    //  🧨 灭自证：把 2.5 改成 0（只等 0.2）⇒ E3 红；把 0.2 改成 0 ⇒ E1 红；把两段并成 2.7 ⇒ E3 红（2.49 时会提前开战）。
    // ==================================================================
    static void GroupE_PreBattleTransition()
    {
        Section("🆕 B143-W6：换牌→开战的 0.2 s + 2.5 s 过渡（两段分别钉）");
        Check(BattleDriver.TimeToPositionCardSeconds, 0.2f, "E0a 第一段 = **0.2 s**（`VarsGlobal.timeToPositionCard`）");
        Check(BattleDriver.IntroStageStepSeconds, 2.5f, "E0b 第二段 = **2.5 s**（`VarsGlobal.introStageStepTime`）");

        float t;
        // 第一段：0.2 之前**不开战**
        Check(BattleDriver.AdvancePreBattlePhase(1, 0.19f, 0f, out t), 1,
              "E1 ★ 第一段 0.19 s（未到 0.2）⇒ **仍停在相位 1**（这一段里还没 `BeginTurn`）"
            + "｜🧨 把 0.2 改成 0 ⇒ 本条红");
        Check(BattleDriver.AdvancePreBattlePhase(1, 0.19f, 0.02f, out t), 2, "E2 第一段走满 ⇒ 相位 2（开始等 2.5 s）");
        Check(t, 0.01f, "E2b 跨段的余量要**带进**第二段（0.19+0.02−0.2 = 0.01）");

        // 第二段：2.5 之前**依然不开战**（只钉总和的写法在这里会露馅）
        Check(BattleDriver.AdvancePreBattlePhase(2, 2.49f, 0f, out t), 2,
              "E3 ★★ 第二段 2.49 s（未到 2.5）⇒ **仍停在相位 2**"
            + "｜🧨 把 2.5 改成 0 或把两段并成 2.7 ⇒ 本条红");
        Check(BattleDriver.AdvancePreBattlePhase(2, 2.49f, 0.02f, out t), 3, "E4 第二段走满 ⇒ 相位 3 = 开战（`BeginTurn`）");
        Check(t, 0f, "E4b 到相位 3 时把计时清零（下一局复用同一对字段）");

        // 相位 3 不再推进（幂等）
        Check(BattleDriver.AdvancePreBattlePhase(3, 9f, 9f, out t), 3, "E5 已开战（相位 3）⇒ 不再推进");
        Check(BattleDriver.AdvancePreBattlePhase(0, 9f, 9f, out t), 0, "E6 没启动（相位 0）⇒ 不动（非换牌路径不受影响）");
    }

    // ==================================================================
    //  F 组 · `B134-D7`：**本机重连节拍 3 s**（原版 `_AttemptReconnect_d__320__MoveNext.c:110-125`
    //    —— 剩余秒数 `% 3 == 0` 那一拍才 `TryReconnecting`）。我们原来 2000ms（🔶 自定）。
    //  🧨 灭自证：把常量改回 2000 ⇒ F1 红。
    // ==================================================================
    static void GroupF_NetReconnect()
    {
        Section("🆕 B143-W6：联机重连节拍");
        Check(CardPresentation.Net.NetSession.ReconnectEveryMs, 3000,
              "F1 ★ 本机重连尝试间隔 = **3000 ms**（原版每 3 s 一拍）"
            + "｜🧨 改回 2000 ⇒ 红（那是旧的自定值）");
        Check(CardPresentation.Net.NetSession.HeartbeatMs, 1000, "F2 心跳间隔仍是 1 s（未动）");
        CheckTrue(CardPresentation.Net.NetSession.ConnectTimeoutMs > CardPresentation.Net.NetSession.ReconnectEveryMs,
                  "F3 前提：单次连接超时（" + CardPresentation.Net.NetSession.ConnectTimeoutMs
                + " ms）必须 > 重连节拍（" + CardPresentation.Net.NetSession.ReconnectEveryMs
                + " ms）—— 否则上一发还没超时下一发就来了");
    }

    // ==================================================================
    //  G 组 · 输入后端迁移：`BattleDriver.cs` 必须**全部走旧输入**（New Input System 一处不留）
    //    背景：全仓在把 `activeInputHandler` 改 0（New-only → 旧输入）；`BattleDriver` 里原来 11 处
    //    `Mouse.current` / `Keyboard.current` / `Touchscreen.current`，不改的话后端一改就**全哑**。
    //  🧨 灭自证：把任意一处改回 `Keyboard.current.xxx` ⇒ G1 红。
    // ==================================================================
    static void GroupG_LegacyInput()
    {
        Section("🆕 B143-W6：BattleDriver 输入后端（旧输入）");
        string src = File.ReadAllText("Assets/CardPresentation/Battle/BattleDriver.cs");
        int live = 0;
        foreach (var line in src.Split('\n'))
        {
            var code = System.Text.RegularExpressions.Regex.Replace(line, "//.*$", "");   // 去掉行尾注释再判（迁移痕迹写在注释里，不算活引用）
            if (System.Text.RegularExpressions.Regex.IsMatch(code, @"(Mouse|Keyboard|Touchscreen|Gamepad)\.current")) live++;
        }
        Check(live, 0,
              "G1 ★ `BattleDriver.cs` 活代码里 **0 处** `Mouse.current` / `Keyboard.current` / `Touchscreen.current`"
            + "｜🧨 任一处改回 New Input System ⇒ 改 `activeInputHandler` 那天那条键就哑了");
        var noComments = System.Text.RegularExpressions.Regex.Replace(src, "//[^\n]*", "");
        CheckTrue(!System.Text.RegularExpressions.Regex.IsMatch(noComments, @"using\s+UnityEngine\.InputSystem\s*;"),
                  "G2 不再 `using UnityEngine.InputSystem`（已改旧输入）");
        CheckTrue(src.Contains("Input.GetMouseButton(0)") && src.Contains("Input.GetKeyDown(KeyCode.Escape)")
                  && src.Contains("Input.GetKeyDown(KeyCode.Space)") && src.Contains("Input.GetTouch(0).position"),
                  "G3 三个代表口都已是旧输入（鼠标按住 / ESC / 空格 / 触摸位置）");
    }

    static void Section(string title) { Debug.Log("--- " + title + " ---"); }
    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + (actual == null ? "<null>" : actual.ToString()) + "，期望 = " + (expected == null ? "<null>" : expected.ToString()) + "）");
    }
    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }
    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[B143W6A] ✓ " + msg); }
        else { _fail++; Debug.LogError("[B143W6A] ✗ " + msg); }
    }
    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B143_W6_Anim 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B143_W6_Anim 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
