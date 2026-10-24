// EngineClosedDeckAssert.cs — 执行代理 `E2`（2026-10-24 · 第七十三会话）的独立断言入口
//
// 两件事，各一节：
//   §A **`GameplayVariables.For(GameMode.ClosedDeck)` 必须显式落到「12 张那套」** ——
//      判据（改前的缺陷）：`For` 原来只有 `Skirmish` / `Tutorial+TutorialReplay` 两条改道，
//      `ClosedDeck 7`（轮抽）靠 `default` **静默落进 Classic（30 张那套）**。
//      原版真值（`DraftEventData.gameplayVariables`）**在服务端、本地 0 命中** ⇒ 本档
//      **显式复用 Skirmish 的参数**，那是**我们挑的、不是原版真值**（见 `GameplayVariables.ClosedDeck` 的 doc）。
//   §B **`EventTiming` 的登记闸复核 + 补一条「登记了却没 `case`」的闸** ——
//      `IsSourced` / `SourceOf` / `DurationOf(EvtKind)` 三处原来都是 `default:` 一把兜住
//      （判据 → `资料/普查产出_1023e/J2_引擎结构自检.md` §四·3）。
//      前一轮（`A2` / `F2` / `G1b`）已把三处改成「查登记表 + 出声 + 计数」，断言在
//      `CardPresentation/Editor/BattleScene.cs` 那一段（本代理**复核**，⛔ 不重复写）。
//      本文件补的是**标准件没覆盖的那一格**：`TimedKinds` 只是**声明**，
//      「登记了但 `switch` 里没有 `case`」时 `IsSourced` 仍会回 `true`（静默）——
//      §B 逐档调 `DurationOf` 并盯未登记计数，把这一格变成红。
//
// 调用方式（任选其一）：
//     ① 单独跑：`-executeMethod EngineClosedDeckAssert.Run`
//     ② 并进某个宿主的收尾处（想并进总数就带后一行）：
//            EngineClosedDeckAssert.Run();
//            pass += EngineClosedDeckAssert.Pass; fail += EngineClosedDeckAssert.Fail;
//
// 🔴 **期望值从哪来（不许自证）**：
//   · §A 的字面量 12 / 4 / −10 / +2 / 无换牌 —— 原版**游戏内文案**（`资料/索引与盘点/解包资源使用地图.md:533`）：
//     `12-card decks` · `Up to 4 Legendaries (Warlord not included)` · `Warlords start with 10 less Health`
//     · `Gain +2 Maximum Energy each turn` · `No mulligan`；
//     8 / 4 / 1（手牌上限 / 起手 / 能量结转）只有【规则书，**非官方**】一条支撑 —— 注释里逐条标了。
//   · §B 的「19 档」是 `EvtKind` 枚举**现读**值，**不在这里写死**（`Enum.GetValues` 现数）。
// ⛔ 本文件只放断言：不改产品行为、不建场景、不进任何 `Editor/*Scene.cs` 宿主。
using System;
using System.Text;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class EngineClosedDeckAssert
{
    const string P = "[E2/ClosedDeck] ";

    /// <summary>本类这一趟的通过 / 失败数（想并进宿主的合计数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            Debug.Log(P + "===== §A `GameplayVariables.For(ClosedDeck)` =====");
            SectionA_ClosedDeck();
            Debug.Log(P + "===== §B `EvtKind` 登记闸（复核 + 补「登记了没 case」）=====");
            SectionB_EvtKindRegistry();
        }
        catch (Exception e)
        {
            Fail++;
            Debug.LogError(P + "✗ 断言过程抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            // 收尾：别把自检造出来的「未登记」痕迹留给后面的断言（本项目 BattleScene 那组也清）。
            try { EventTiming.ResetUnregisteredKindForTest(); EventTiming.ResetWarnedForTest(); } catch { }
        }
        string verdict = Fail == 0 ? "✅ 全绿" : "❌ 有红";
        Debug.Log(P + $"===== 合计 通过 {Pass} / 失败 {Fail} —— {verdict} =====");
    }

    // ==================================================================
    //  §A `GameplayVariables.For(GameMode.ClosedDeck)`
    // ==================================================================
    static void SectionA_ClosedDeck()
    {
        var cd = GameplayVariables.For(GameMode.ClosedDeck);
        if (!Ok(cd != null, "前提：`For(ClosedDeck)` 非 null")) return;

        // ---- A1 · 核心：**不许静默落进 Classic**（改前的缺陷就是这一格）----
        Ok(cd.deckSize != GameplayVariables.Classic.deckSize,
           $"★ A1 **`ClosedDeck` 不再落进经典那套**：`deckSize` = {cd.deckSize}（经典是 {GameplayVariables.Classic.deckSize}）"
         + " —— 🧨 删掉 `For` 里 `case GameMode.ClosedDeck:`（让它走 `default: return Classic;`）⇒ 本条红");
        Ok(!ReferenceEquals(cd, GameplayVariables.Classic),
           "★ A1（灭自证）**返回的不是同一个实例**（`ReferenceEquals(For(ClosedDeck), Classic)` = false）"
         + " —— 🧨 同上，改成 `return Classic;` ⇒ 本条与本组上一格**同时**红");

        // ---- A2 · 逐字段的值（**来源逐条标**；⛔ 别拿 `Skirmish` 证明 `ClosedDeck`）----
        Eq(cd.deckSize, 12, "A2 `deckSize = 12` —— 原版【文案】`12-card decks`");
        Eq(cd.maxLegendaries, 4, "A2 `maxLegendaries = 4` —— 原版【文案】`Up to 4 Legendaries`");
        Eq(cd.warlordLifeChange, -10, "A2 `warlordLifeChange = -10` —— 原版【文案】`10 less Health`");
        Eq(cd.manaPerTurn, 2, "A2 `manaPerTurn = 2` —— 原版【文案】`+2 Maximum Energy each turn`");
        Eq(cd.startingManaSecond, 2, "A2 `startingManaSecond = 2` —— 【推断】（文案 `P1 3 Energy P2 4 Energy` 那 1 点差）");
        Eq(cd.showMulligan, false, "A2 `showMulligan = false` —— 原版【文案】`No mulligan`");
        Eq(cd.shouldReshuffle, false, "A2 `shouldReshuffle = false` —— 原版【文案】`No mulligan` 的同族推断");
        Eq(cd.handLimit, 8, "A2 `handLimit = 8` —— 【规则书:58，**非官方**】");
        Eq(cd.startingHand, 4, "A2 `startingHand = 4` —— 【规则书:58，**非官方**】");
        Eq(cd.manaAccumulation, 1, "A2 `manaAccumulation = 1` —— 【规则书:60，**非官方**】");
        Ok(cd.IsSkirmish,
           "A2 `IsSkirmish = true`（判据 = `deckSize < 30`）—— ⚠️ 这只说明「吃的是 12 张那套参数」，"
         + "**不是模式号**（本局模式仍是 `ClosedDeck 7`）");
        Ok(cd.overtimeTurn.HasValue && cd.overtimeTurn.Value == 10,
           $"A2 `overtimeTurn = 10` —— 【推断】（本地没有服务端真值；实得 "
         + $"{(cd.overtimeTurn.HasValue ? cd.overtimeTurn.Value.ToString() : "null")}）");

        // ---- A3 · 幂等：同一档反复取必须是同一份（`For` 是全仓唯一的「模式 → 参数」入口）----
        Ok(ReferenceEquals(GameplayVariables.For(GameMode.ClosedDeck), cd),
           "A3 `For(ClosedDeck)` 幂等：两次调用同一实例（⛔ 别每次 new 一份，那会让状态两处不一致）");

        // ---- A4 · **全 15 档的落点表**（现读枚举；等价于「新加一档不会静默掉进经典」）----
        int total = 0, classicBucket = 0, skirmishBucket = 0, tutorialBucket = 0, closedBucket = 0;
        var wrong = new StringBuilder();
        foreach (GameMode m in Enum.GetValues(typeof(GameMode)))
        {
            total++;
            var v = GameplayVariables.For(m);
            bool wantSkirmish = m == GameMode.Skirmish;
            bool wantClosed = m == GameMode.ClosedDeck;
            bool wantTutorial = m == GameMode.Tutorial || m == GameMode.TutorialReplay;
            bool wantClassic = !wantSkirmish && !wantClosed && !wantTutorial;

            bool ok =
                (wantSkirmish && ReferenceEquals(v, GameplayVariables.Skirmish)) ||
                (wantClosed && ReferenceEquals(v, GameplayVariables.ClosedDeck)) ||
                (wantTutorial && ReferenceEquals(v, GameplayVariables.Tutorial)) ||
                (wantClassic && ReferenceEquals(v, GameplayVariables.Classic));

            if (wantSkirmish) skirmishBucket++;
            else if (wantClosed) closedBucket++;
            else if (wantTutorial) tutorialBucket++;
            else classicBucket++;
            if (!ok) wrong.Append(wrong.Length == 0 ? "" : " / ").Append(m).Append("(值 ").Append((int)m).Append(')');
        }
        Ok(wrong.Length == 0,
           $"★ A4 **15 档逐档的落点表**：遭遇 1 · 轮抽 1 · 教程 2 · 经典 {classicBucket}（合计 {total}）"
         + (wrong.Length == 0 ? "" : $"｜落错桶：{wrong}")
         + " —— 🧨 把 `ClosedDeck` 那一行删掉 ⇒ 它掉进经典桶 ⇒ 本条红");
        Eq(total, 15, "A4（前提）`GameMode` 枚举现读 15 档（原版 `PlayModes` 全表，⛔ 顺序/名字不许改）");
        Eq(closedBucket, 1, "A4 轮抽桶 = 1 档（`ClosedDeck`）");
        Eq(skirmishBucket, 1, "A4 遭遇桶 = 1 档（`Skirmish`）");
        Eq(tutorialBucket, 2, "A4 教程桶 = 2 档（`Tutorial` / `TutorialReplay`）");
        Eq(classicBucket, 11, "A4 经典桶 = 11 档（其余全部 —— 这是「我们只有四份实例」的必然结果）");

        // ---- A5 · 回归：另外三档一个字没动 ----
        Ok(ReferenceEquals(GameplayVariables.For(GameMode.Skirmish), GameplayVariables.Skirmish)
           && GameplayVariables.Skirmish.deckSize == 12,
           "A5 回归：`For(Skirmish)` 仍是遭遇那份（12 张）");
        Eq(GameplayVariables.For(GameMode.Classic).deckSize, 30, "A5 回归：`For(Classic)` 仍是 30 张那套");
        Ok(ReferenceEquals(GameplayVariables.For(GameMode.Tutorial), GameplayVariables.Tutorial)
           && GameplayVariables.Tutorial.startingHand == 3,
           "A5 回归：`For(Tutorial)` 仍是教程那份（起手 3 —— `TutorialScenario.startingHand`）");
    }

    // ==================================================================
    //  §B `EvtKind` 登记闸
    // ==================================================================
    static void SectionB_EvtKindRegistry()
    {
        EventTiming.ResetUnregisteredKindForTest();
        EventTiming.ResetWarnedForTest();

        int kinds = 0, zero = 0, timed = 0, unregistered = 0, doubled = 0;
        var unregNames = new StringBuilder();
        var doubleNames = new StringBuilder();

        foreach (EvtKind k in Enum.GetValues(typeof(EvtKind)))
        {
            kinds++;
            bool isZero = EventTiming.IsZeroDurationKind(k);
            bool isTimed = EventTiming.IsTimedKind(k);

            // B1 · 每一档**恰好**登记在一张表里（两张都登记 = 口径自相矛盾）
            if (!isZero && !isTimed)
            {
                unregistered++;
                unregNames.Append(unregNames.Length == 0 ? "" : " / ").Append(k);
                continue;
            }
            if (isZero && isTimed)
            {
                doubled++;
                doubleNames.Append(doubleNames.Length == 0 ? "" : " / ").Append(k);
                continue;
            }

            if (isZero)
            {
                zero++;
                // B2 · 零时长档：0 / 「（无）」/ 有出处（= 不占时间，没有「无出处的数值」）
                Eq(EventTiming.DurationOf(k), 0f, $"B2 `{k}`（零时长）`DurationOf` = 0");
                // 🩹 **2026-10-24（自检首跑订正）**：零时长档的 `SourceOf` **允许有解释串**
                //   （`EventTiming.SourceOf` 对 `AmbushExit` 就返回「**占 0**（原版 SetAmbush 不阻塞…）」）。
                //   原来一律期望「（无）」⇒ 把「有出处」误判成错。现在断「非空」，另对 `AmbushExit` 钉住
                //   「必须写『占 0』」这条具体判据。
                var srcZero = EventTiming.SourceOf(k);
                Ok(!string.IsNullOrEmpty(srcZero),
                   $"B2 `{k}`（零时长）`SourceOf` 有话说（「（无）」或解释串都行；实得 `{(srcZero ?? "(null)").Split('（')[0]}`）");
                if (k == EvtKind.AmbushExit)
                    Ok(srcZero != null && srcZero.Contains("占 0"),
                       "B2 `AmbushExit` 的 `SourceOf` 必须写「占 0」那条判据（原版 `CardScript.SetAmbush(card,false)` 不阻塞）");
                Ok(EventTiming.IsSourced(k),
                   $"B2 `{k}`（零时长）`IsSourced` = true（不占时间 ⇒ 没有「无出处的值」）");
                continue;
            }

            timed++;
            // B3 · 有时间档：**`switch` 里必须真有 `case`** —— 判据 = 调它不会触发「未登记」计数。
            //      🔴 这一格是 `IsRegisteredEventKind` 答不出来的（见 `EventTiming.IsTimedKind` 的 doc）。
            int before = EventTiming.UnregisteredKindHits;
            float d = EventTiming.DurationOf(k);
            string src = EventTiming.SourceOf(k);
            int after = EventTiming.UnregisteredKindHits;
            Ok(after == before,
               $"★ B3 `{k}`（登记「有时间」）**`DurationOf` 的 `switch` 里真有 `case`**"
             + $"（未登记计数 {before} → {after}，时长 {d:F2}）"
             + " —— 🧨 从 `TimedKinds` 保留它、但删掉 `DurationOf` 的 `case` ⇒ 本条红");
            Ok(!string.IsNullOrEmpty(src) && !src.Contains("未登记") && !src.Contains("没有它的 `case`"),
               $"★ B3 `{k}` **`SourceOf` 也有它的 `case`**（出处「{(src != null && src.Length > 40 ? src.Substring(0, 40) + "…" : src)}」）"
             + " —— 🧨 删掉 `SourceOf` 的 `case` ⇒ 本条红");

            // B4 · 出处口径：`Death` / `Ability` 是「值里有我们挑的那一半」⇒ 显式 false + 带原因
            bool wantSourced = !(k == EvtKind.Death || k == EvtKind.Ability);
            string why;
            bool sourced = EventTiming.IsSourced(k, out why);
            Ok(sourced == wantSourced && (wantSourced ? why == null : !string.IsNullOrEmpty(why)),
               $"B4 `{k}` `IsSourced` = {sourced}（期望 {wantSourced}）"
             + (wantSourced ? "，且无原因" : "，且**带原因**（那一格有我们挑的值）"));
        }

        // B5 · 全枚举覆盖：**一档都不许漏**（现读枚举，⛔ 不写死 19）
        //   ⚠️ 这里**不写**「kinds == zero+timed+unregistered+doubled」那种恒等式（按构造恒真 = 自证）；
        //     真正的判据是下面两条：`unregistered == 0` 与 `doubled == 0`。
        Debug.Log(P + $"   读数：`EvtKind` 现读 {kinds} 档 = 零时长 {zero} + 有时间 {timed}"
                     + $" + 未登记 {unregistered} + 重复登记 {doubled}");
        Ok(unregistered == 0,
           $"★ B5 **`EvtKind` 现读 {kinds} 档全部登记**（零时长 {zero} + 有时间 {timed}；未登记 {unregistered}"
         + (unregistered == 0 ? "" : "：" + unregNames) + "）"
         + " —— 🧨 往枚举里加一档不登记 ⇒ 本条红");
        Ok(doubled == 0,
           $"★ B5 **没有一档同时登记在两张表里**（重复 {doubled}"
         + (doubled == 0 ? "" : "：" + doubleNames) + "）"
         + " —— 🧨 把一个有时间的事件也塞进 `ZeroDurationKinds` ⇒ 本条红（口径自相矛盾）");

        // ---- B6 · 灭自证：**未登记 ⇒ 三处一起出声/报无出处**（这就是 J2 §四·3 那条缺陷的形状）----
        {
            EventTiming.ResetUnregisteredKindForTest();
            EventTiming.ResetWarnedForTest();
            var fake = (EvtKind)999;
            Ok(!EventTiming.IsRegisteredEventKind(fake) && !EventTiming.IsTimedKind(fake),
               "B6（前提）`(EvtKind)999` 既没登记、也不是「有时间」那一档");
            int before = EventTiming.UnregisteredKindHits;
            string why;
            bool src = EventTiming.IsSourced(fake, out why);
            float dur = EventTiming.DurationOf(fake);
            string so = EventTiming.SourceOf(fake);
            int after = EventTiming.UnregisteredKindHits;
            Ok(!src && !string.IsNullOrEmpty(why) && dur == 0f
               && !string.IsNullOrEmpty(so) && so.Contains("未登记") && after > before,
               $"★ B6（灭自证）**未登记的 `EvtKind` 三处都不再静默兜住** —— `IsSourced` = {src}（带原因 "
             + $"{(string.IsNullOrEmpty(why) ? "**空**" : "非空")}）· `DurationOf` = {dur:F2}（仍 0，但出声）"
             + $" · `SourceOf` =「{(so != null && so.Length > 30 ? so.Substring(0, 30) + "…" : so)}」"
             + $" · 计数 {before} → {after}"
             + " ｜🧨 任一处改回 `return true;` / `return 0f;` / `return \"（无）\";` ⇒ 本条红");
        }

        // ---- B7 · `ShowCardsWindow` **必须保持「显式零时长」**（原版那一拍非阻塞）----
        Ok(EventTiming.IsZeroDurationKind(EvtKind.ShowCardsWindow)
           && EventTiming.IsRegisteredEventKind(EvtKind.ShowCardsWindow)
           && EventTiming.IsSourced(EvtKind.ShowCardsWindow)
           && EventTiming.DurationOf(EvtKind.ShowCardsWindow) == 0f
           && EventTiming.SourceOf(EvtKind.ShowCardsWindow) == "（无）",
           "★ B7 `ShowCardsWindow` **显式登记为「零时长」**（原版 `FinishResolvingAction` 紧跟其后、无 `yield`）"
         + " ⇒ 读数与改前逐字相同"
         + " ｜🧨 把它从 `ZeroDurationKinds` 删掉 ⇒ 它会变成「未登记」噪音源 ⇒ 本条红");

        EventTiming.ResetUnregisteredKindForTest();
        EventTiming.ResetWarnedForTest();   // 收尾：别把 999 的痕迹留给后面的断言
    }

    // ==================================================================
    //  小工具（与 `DraftAssertD1` 同一形状）
    // ==================================================================
    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq<T>(T got, T want, string msg)
    {
        return Ok(System.Collections.Generic.EqualityComparer<T>.Default.Equals(got, want),
                  msg + $"（实得 {got} / 期望 {want}）");
    }
}
