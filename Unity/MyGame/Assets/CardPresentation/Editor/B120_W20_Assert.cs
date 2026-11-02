// B120_W20_Assert.cs — 第一百二十会话 · 写手 `B120-W20`（`D118-80` 的**表现层那一半**）
//
// ============================ 它盯的是什么 ============================
//  原版：`CardScript.UpdateTraitIcons()` 由 **14 个调用方方法 / 18 处调用点分刷**（现扫
//    `d:/2/tools/decomp_full` 全量 xref：`SetAmbush` · `ResolveCardPlayed` · `ResolveActiveAbilityPlayed`×2 ·
//    `CheckReplacedAbility` · `OnTurnEnd` · `UpdateFigures` · `RemoveEffect`×2 · `RemoveTrait` · `Stun` ·
//    `RemoveCostEffects` · `RemoveBuffHandCostEffects` · `AddEffect`×3 · `ReactToUnitJammed`×2 ·
//    `BattleCardUI.SetObjectVisibility`），而 **4 类方法自己一处都不调**：
//      ① `AddTraitSilently`（`decomp_full/CardScript__AddTraitSilently.c`）·
//      ② `RemoveTraitSilently`（`CardScript__RemoveTraitSilently.c` 全文 55 行，**0 次** `UpdateTraitIcons`/`UpdateFigures`）·
//      ③ `SetBaseCardTraits`（`CardScript__SetBaseCardTraits.c` 同样 0 次；刷新交给调用方
//         `CardSetup.c:194` / `ResetToValuesInHand.c:163` 各自那次 `UpdateFigures`）·
//      ④ `SetTraitsOnPlayAttackingStatus`（`CardScript__SetTraitsOnPlayAttackingStatus.c` 只切 `0x492`
//         子节点上的攻击状态显示 = 我们的 `MarkerlightController.AimingAttack(bool)`，**不动 trait 列表**）。
//
//  我们原来是「**单口** `CardView.SetBadges` + `BattleDriver.SyncBoard` **每拍重推** `SetData`」——
//    引擎侧**没有任何「刷 / 不刷」的信号**（`报告/B120_W7_执行.md` §3·1 定性）。
//  本批补的就是那条信号：`UnitState.TraitRev`（只有**非静默** trait 写才 +1）→ `CardData.traitRev` →
//    `CardView.SetBadgesDirtyAware`（`dirty 或首次`才重建）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1：把 `AddKeyword` 末尾的 `if (!silent) TraitRev++` 删掉 ⇒ A1 实得 r0 ⇒ 红。
//  · A2：把那一行写成**无条件** `TraitRev++`（= 静默档也置脏）⇒ A2 实得 r1+1 ⇒ 红。
//  · B①：`RemoveAll` 末尾同样 —— 写成无条件 ⇒ B① 红；删掉 ⇒ B②c 红。**两条一起断**才挡得住「两边一起改」。
//  · B③：删掉 `ResetToValuesInHand` 整表重建那一行的 `TraitRev++`（原版 `SetBaseCardTraits` 的调用点跟一次
//        `UpdateFigures`）⇒ B③ 实得 r2 ⇒ 红。
//  · C1：删掉 `CardView.SetFace` 里那句 `_traitIconsDirty = true;` ⇒ 手牌档那次 `SetBadges(null)` 的结果被
//        「幂等短路」留着 ⇒ 换到场上**整排徽标不出现**（实得 0）⇒ C1 红。
//  · D1：删掉 `SetBadgesDirtyAware` 里的短路 ⇒ 同一 rev 再推一次也重建（实得 +1）⇒ D1 红。
//  · D2：把 `traitRev` 的透传丢掉（恒 0 / 恒同一个值）⇒ D2 实得 b0 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  ① 引擎侧（`RuleEngine/Editor/RuleEngineTest.cs` 的聚合行，现读 `:1303`）——**同一物理行追加**
//     （⚠️ 必须落在该行第一个 `//` 之前 ⇒ 零行数变化）：
//         Guard("CheckB120W20Gaps", CheckB120W20Gaps);
//  ② 表现层（`CardPresentation/Editor/BattleScene.cs` 的聚合行，`B101R4_Assert.Run();` 那一族）——
//     **同一物理行追加**：
//         B120W20_Assert.Run(); pass += B120W20_Assert.Pass; fail += B120W20_Assert.Fail;
//     ⚠️ 它自带探针根节点并在收尾 `DestroyImmediate`（同 `B101R4_Assert` 那一族），**零前提** ⇒ 可接在合计行之前。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B120_W20_执行.md`（4 类静默点清单 / 写点→置脏表 / 断言 / 待接线）
//  · 原版：上面那 5 份 `.c` ＋ `CardScript__Stun.c` / `CardScript__UseSurvivor.c` / `CardScript__RemoveEffect.c`
//    / `CardScript__ResolveCardPlayed.c` / `CardScript__CardSetup.c` / `CardScript__ResetToValuesInHand.c`
//    （逐个核过「静默写之后同方法/同链里跟没跟一次刷新」）
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>`D118-80` 表现层那一半：trait 图标的「脏」信号（`UnitState.TraitRev`）。</summary>
    public static void CheckB120W20Gaps()
    {
        Section("🆕 B120_W20 · `D118-80` trait 图标的「脏」信号（原版 14 调用方 / 18 调用点 vs 4 类不刷）");
        B120W20SilentChannel();
        B120W20BaseTraitsRefreshPoint();
        B120W20NoTraitListWrite();
        B120W20SourceGates();
    }

    /// <summary>`src/…`（相对 `Application.dataPath`）里有没有那段代码 —— 源码闸（挡「静默支被删掉」）。</summary>
    static bool B120W20Src(string rel, string needle)
    {
        try { return System.IO.File.ReadAllText(Application.dataPath + "/" + rel).Contains(needle); }
        catch (Exception e) { Debug.LogWarning($"[B120W20] 读源码失败 `{rel}`：{e.Message}"); return false; }
    }

    /// <summary>A / B① ② ③：静默通道（`AddKeyword(silent:)` / `RemoveAll(silent:)` / 整表重建）。</summary>
    static void B120W20SilentChannel()
    {
        var c = Unit("B120W20A", 1, 2, 3);
        var ctx = ProbeBattle(new[] { c }, new CardDef[] { });
        var u = Place(ctx, 0, 1, c);
        Check(u != null, true, "A0（前提）：`B120W20A` 上了棋盘");
        if (u == null) return;
        // ⚠️ 只断**增量**：上棋盘那一步可能已经走过光环/部署写点（`Place` 之后 rev 未必是 0）
        int r0 = u.TraitRev;

        u.AddKeyword("flank", 1);
        Check(u.TraitRev, r0 + 1,
              "🧨 A1 **非静默 `AddKeyword` 置脏**（`TraitRev` +1）—— 原版这类写点的调用方同方法/同链里都跟一次\n"
            + "            `UpdateTraitIcons`/`UpdateFigures`（`AddEffect`×3 · `Stun` · `RemoveTrait` … 全在 14 个调用方里）\n"
            + "            ｜🧨 删掉 `UnitState.AddKeyword` 末尾的 `if (!silent) TraitRev++` ⇒ 实得 r0 ⇒ 红");
        Check(u.Has("flank"), true, "A1b（前提）：`flank` 真的挂上了（否则 A1 断的是个空操作）");

        int r1 = u.TraitRev;
        u.AddKeyword(KeywordTable.Stun, 1, silent: true);
        Check(u.TraitRev, r1,
              "🧨 A2 `silent: true` **不置脏**（原版 ① `AddTraitSilently` = 4 类不刷之一）\n"
            + "            ｜🧨 把那一行写成无条件 `TraitRev++` ⇒ 实得 r1+1 ⇒ 红（**A1 与 A2 结构上不可能同时被「写反」解释**）");
        Check(u.Has(KeywordTable.Stun), true,
              "🧨 A2b **状态照改** —— `silent` 只挡「置脏」这一半，⛔ 不是「不写」\n"
            + "            （原版 `AddTraitSilently` 照样写 `+0x128`/`+0x130` 两本表，只是不重建图标）");

        int r2 = u.TraitRev;
        u.RemoveAll("flank", silent: true);
        Check(u.TraitRev, r2,
              "🧨 B① `RemoveAll(..., silent: true)` **不置脏**（原版 ② `RemoveTraitSilently` = 4 类不刷之二；\n"
            + "            `CardScript__RemoveTraitSilently.c` 全文 55 行 **0 次** `UpdateTraitIcons`/`UpdateFigures`）\n"
            + "            ｜🧨 把 `RemoveAll` 末尾写成无条件 `TraitRev++` ⇒ 红");
        Check(u.Has("flank"), false, "B①b（前提）：`flank` 真的摘掉了（`silent` ≠ 不动状态）");

        int r3 = u.TraitRev;
        u.AddKeyword("flank", 1);          // 非静默 ⇒ 置脏
        int r4 = u.TraitRev;
        Check(r4, r3 + 1, "B②（前提）：重新挂上时置了一次脏（与 A1 同一条）");
        u.RemoveAll("flank");              // **非静默**摘
        Check(u.TraitRev, r4 + 1,
              "🧨 B②c **非静默 `RemoveAll` 置脏** —— 原版 `RemoveTrait` 在 14 个调用方里（`CardScript__RemoveTrait.c:66-68`\n"
            + "            对 flank/fast 重算 anims 之后还要重建图标）｜🧨 删掉那一行 ⇒ 实得 r4 ⇒ 红");
    }

    /// <summary>B③：`ResetToValuesInHand` 的整表重建 = 原版 `SetBaseCardTraits` 的**刷新点**（4 类不刷之三）。</summary>
    static void B120W20BaseTraitsRefreshPoint()
    {
        var c = Unit("B120W20B", 1, 2, 3);
        var ctx = ProbeBattle(new[] { c }, new CardDef[] { });
        var u = Place(ctx, 0, 1, c);
        Check(u != null, true, "B3-0（前提）：`B120W20B` 上了棋盘");
        if (u == null) return;

        u.AddKeyword("flank", 1);
        int r0 = u.TraitRev;
        u.RemoveAll("flank", silent: true);              // 静默摘（原版 `RemoveTraitSilently`）⇒ 不置脏
        Check(u.TraitRev, r0, "B3-1（前提）：静默摘不置脏（与 B① 同一条）");

        int r1 = u.TraitRev;
        u.ResetToValuesInHand(clearBacklash: true);      // 内部 `_keywords.Clear()` + 按卡面整表重灌
        Check(u.TraitRev, r1 + 1,
              "🧨 B③ **整表重建（原版 `SetBaseCardTraits`）置脏** —— 它**自己**一处都不刷，但两个调用点\n"
            + "            `CardScript__CardSetup.c:194` / `CardScript__ResetToValuesInHand.c:163` **各自跟一次 `UpdateFigures`**\n"
            + "            ⇒ 净结果是「刷」｜🧨 删掉 `UnitState.ResetToValuesInHand` 那一行行尾的 `TraitRev++` ⇒ 实得 r1 ⇒ 红");
        Check(u.Has("flank"), false, "B3-2（前提）：整表重建确实把效果挂的 `flank` 清掉了（不是空转）");
    }

    /// <summary>B④：`SetTraitsOnPlayAttackingStatus` = 只切攻击状态显示的**表现层**动作，结构上不碰 trait 列表。</summary>
    static void B120W20NoTraitListWrite()
    {
        var t = typeof(MarkerlightController).GetMethod("AimingAttack", BindingFlags.Public | BindingFlags.Instance);
        Check(t != null, true, "B4-0（前提）：`MarkerlightController.AimingAttack` 存在（原版 ④ 的等价位）");
        if (t == null) return;
        var ps = t.GetParameters();
        Check(ps.Length, 1, "B4-1（前提）：它只有 1 个形参");
        Check(ps.Length == 1 && ps[0].ParameterType == typeof(bool), true,
              "🧨 B④ `SetTraitsOnPlayAttackingStatus(bool hideStatus)` 的等价位**只收一个 bool**\n"
            + "            （原版 `.c` 只 `Find(0x492)` 那个子节点、对它调一次带 bool 的方法 —— 全程不碰 trait 列表）\n"
            + "            ｜🧨 谁把它改成收 `UnitState` / 在里面改关键词 ⇒ 本条红（那就不再是「4 类不刷」之一了）");
    }

    /// <summary>源码闸：静默支 / 置脏支都还在（防「实现被删、行为断言又恰好绿」）。</summary>
    static void B120W20SourceGates()
    {
        Check(B120W20Src("RuleEngine/Core/UnitState.cs", "public bool AddKeyword(string keyword, int value, bool silent = false)"), true,
              "S1 源码闸：`AddKeyword` 的 `bool silent = false` 形参还在（缺省 = 老行为）");
        Check(B120W20Src("RuleEngine/Core/UnitState.cs", "if (!silent) TraitRev++; return true;"), true,
              "S2 源码闸：`AddKeyword` 末尾的静默闸还在（A1/A2 的行为源头）");
        Check(B120W20Src("RuleEngine/Core/UnitState.cs", "if (!silent && (keyword == \"fast\" || keyword == \"flank\")) RuleCore.RecomputeActAndAttack(this); if (!silent) TraitRev++;"), true,
              "S3 源码闸：`RemoveAll` 末尾的静默闸还在（B①/B②c 的行为源头）");
        Check(B120W20Src("CardPresentation/Core/CardView.cs", "if (traitRev <= 0) return SetBadges(badges);"), true,
              "S4 源码闸：`SetBadgesDirtyAware` 的「未接线 ⇒ 老行为」那一支还在（零影响的前提）");
    }
}

/// <summary>执行写手 `B120-W20`：**徽标刷新 = 「dirty 或首次」**（`CardView` 侧；需活 `CardView`，故走表现层宿主）。</summary>
public static class B120W20_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B120W20] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>两枚徽标夹具（`armour` / `blast` 在 `CardArt.Trait` 里都有图 —— 前提断言先硬查）。</summary>
    static List<Badge> TwoBadges()
    {
        return new List<Badge>
        {
            new Badge { sprite = "armour", counter = 2, active = true },
            new Badge { sprite = "blast",  counter = 3, active = true },
        };
    }

    static CardData ProbeData(int traitRev)
    {
        var d = CardData.Simple("B120W20Probe", 3, 3, 4);   // ⛔ 不设 `faction`：走「无卡框」兜底，与徽标无关
        d.badges = TwoBadges();
        d.traitRev = traitRev;
        return d;
    }

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B120_W20 · `D118-80` 徽标刷新 =「dirty 或首次」（`CardView.SetBadgesDirtyAware`）");

        // ---- 前提（硬 Check，⛔ 不写成 `if (x != null) Check(...)`：那就成了静默空转绿）----
        var tArmour = CardArt.Trait("armour");
        var tBlast = CardArt.Trait("blast");
        Tr(tArmour != null && tBlast != null,
           "★ 前提：两枚徽标图取得到（`CardArt.Trait(\"armour\")` / `(\"blast\")`）—— 取不到时 `BuildBadgeSlot` 整位不建，"
         + "「2 == 2」那一类会变成空转");
        Tr(CardArt.Ui("Base3d_Trait_Background") != null,
           "★ 前提：徽标底板图取得到（`CardArt.Ui(\"Base3d_Trait_Background\")`）—— 缺它 `BuildBadgeSlot` 同样整位不建");

        var root = new GameObject("B120W20_badge_dirty_probe");
        try
        {
            // ============================================================
            //  D. 幂等短路：同一 rev 连推两次 ⇒ **只重建一次**；rev 变了 ⇒ 再重建一次
            // ============================================================
            var dv = CardView.Create(root.transform, ProbeData(3), "b120w20_board_probe", CardFace.Board);
            Tr(dv != null, "★ 前提：场上档的探针卡建出来了（`CardFace.Board`）");
            if (dv != null)
            {
                dv.SetData(ProbeData(3));                       // 归一到「rev = 3 且刚建过」这一档
                int b0 = dv.BadgeRebuilds;
                dv.SetData(ProbeData(3));                       // **同一 rev 再推一次**
                C(dv.BadgeRebuilds, b0,
                  "🧨 D1 **同一 rev 再推一次 ⇒ 不重建**（= 原版「那 4 类刻意不刷」那一档：引擎没有非静默 trait 写，图标就停在上一次的样子）\n"
                + "            ｜🧨 删掉 `SetBadgesDirtyAware` 里的幂等短路 ⇒ 实得 b0+1 ⇒ 红");
                C(dv.BadgesShown, 2,
                  "D1b 短路**不改变画出来的东西**（仍然是那 2 枚）—— 短路的是「重建动作」，不是「内容」");

                dv.SetData(ProbeData(4));                       // rev 变了 = 引擎发生过一次非静默 trait 写
                C(dv.BadgeRebuilds, b0 + 1,
                  "🧨 D2 **rev 变了 ⇒ 重建一次**（引擎侧非静默写 = 刷新点）\n"
                + "            ｜🧨 把 `traitRev` 的透传丢掉（恒 0 / 恒同一个值）⇒ 实得 b0 ⇒ 红");

                // 视图被**复用到另一张卡**（rev 恰好相同）⇒ 短路必须失效，否则带着上一张的徽标
                int b2 = dv.BadgeRebuilds;
                var other = ProbeData(3);
                other.id = "B120W20Other"; other.title = "B120W20Other";
                dv.SetData(other);
                C(dv.BadgeRebuilds, b2 + 1,
                  "🧨 D2b `id` 变了（视图复用到**另一张卡**、`TraitRev` 恰好相同）⇒ **必须重建**\n"
                + "            ｜🧨 删掉 `SetBadgesDirtyAware` 里 `_traitIdSeen == Data.id` 那一格 ⇒ 实得 b2 ⇒ 红\n"
                + "            （槽位换人 / 手牌换牌都会走到这里 —— 漏了就是「新兵带旧兵的词条徽标」）");

                // rev 回退 / 未接线那一档必须退回**老行为**（每次都重建）—— 否则接线前的产品会「图标永不更新」
                dv.SetData(ProbeData(0));
                int b1 = dv.BadgeRebuilds;
                dv.SetData(ProbeData(0));
                C(dv.BadgeRebuilds, b1 + 1,
                  "🧨 D3 `traitRev == 0`（= 未接线，今天全工程都是这一档）**退回老行为**：每次都重建\n"
                + "            ｜🧨 把 `if (traitRev <= 0) return SetBadges(badges);` 删掉 ⇒ 接线前图标会「永不更新」⇒ 红");
            }

            // ============================================================
            //  C. 灭自证：**换形态也是一个刷新点**（手牌档清空 ⇒ 换到场上必须重新出现）
            // ============================================================
            var hv = CardView.Create(root.transform, ProbeData(7), "b120w20_hand_probe", CardFace.Hand);
            Tr(hv != null, "★ 前提：手牌档的探针卡建出来了（`CardFace.Hand`）");
            if (hv != null)
            {
                C(hv.BadgesShown, 0, "C0（前提）：手牌档一位徽标都不画（形态闸；判据见 `B101R4_Assert`）");
                int hb = hv.BadgeRebuilds;
                hv.SetFace(CardFace.Board);
                C(hv.BadgeRebuilds, hb + 1,
                  "C1 换形态**强制重建一次**（`SetFace` 先置脏）｜🧨 删掉 `CardView.SetFace` 里那句 `_traitIconsDirty = true;`\n"
                + "            ⇒ 手牌档那次 `SetBadges(null)` 的结果被幂等短路留着 ⇒ 本条红");
                C(hv.BadgesShown, 2,
                  "🧨 C1b 换到场上之后**两枚都出来**（rev 没变，但形态变了 ⇒ 必须重建）\n"
                + "            —— 这条挡的是「漏一条置脏路径 ⇒ 图标静默不更新」那类回归（用户看得见的只有画面）");

                // 显式置脏的口子也要真能生效（自检用口，今天没有生产调用点）
                hv.SetFace(CardFace.Hand);
                int hb2 = hv.BadgeRebuilds;
                hv.MarkTraitIconsDirty();
                hv.UpdateFigures();
                C(hv.BadgeRebuilds, hb2 + 1, "C2 显式 `MarkTraitIconsDirty()` 之后 `UpdateFigures` 会重建（自检用口）");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
