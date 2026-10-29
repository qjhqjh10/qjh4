// B97_SMALL_Assert.cs — 执行代理 `B97_SMALL`（第九十七会话 · 2026-10-29）五个小域 12 条 B/C 的断言
//
// ============================ 它盯的是什么 ============================
// 逐条对应 `资料/完全复刻/账/` 里那 12 行（判据全文 → `资料/完全复刻/判据/B96_小域19_分诊.md` ·
// 本轮逐条结论 → `资料/完全复刻/报告/B97_SMALL.md`）：
//
//   A `轮抽:14`  `DraftEventBase.GetPossibleWarlords()` 尾巴那**三档剔除**（本轮新建 `Shell/DraftWarlordTriage.cs`）
//      —— 真体 `out_b94/DraftEventBase_object__object_.GetPossibleWarlords__18109d0.c` 现读：
//      `inventoryOptions(+0x4c) == NotInInventory(10)` / `PlayerItem.IsHidden()`（虚槽 23）/ `RawCardScript.IsAlternateArt()`。
//      ⚠️ 期望值全是**字面量**（10 / 5 / 56），不从被测实现里读。
//   B `轮抽:14`  `RollWarlords` 的候选池**真的过了那三档**（每条返回的卡都过谓词；seed 可复现）。
//   C `轮抽:6`   `Expire` 为什么还是 B 的**机器可核证据**：`Resources/draft_packs.json` 里**没有**任何到期时刻
//      （原版触发源 = LiveOps 服务端到期事件）。这条不是「行为断言」，是**把 REMOTE 这个结论钉住**：
//      数据里一旦有人加了 `endAt`，C2 会红，提醒「触发源可能已经有了、该回去看 `Expire` 的接线」。
//   D `其他:218` `GameModes.GetActiveEvent(Type)` 为什么还是 B 的同一类证据：`GameModesHandler.RankedEventInterfaceType`
//      默认 `null`（那一支不进入）+ `GameModesConfigSource.MainRankedType` 默认 `null`（远端 config 本地 0 命中）。
//      ⚠️ 这一条**只钉现状**，不给「该不该照原版抛 NRE」定论 —— 那是调度台的裁定（见报告 §决策项）。
//
// ============================ 怎么用（宿主）============================
// 由调度台接线（本代理 ⛔ 不改 `Editor/RuleEngineTest.cs`）：
//   · 在 `Editor/MainMenuScene.cs`（轮抽 / 菜单域）的 `Run()` 里，紧接其它 `*_Assert.Run();` 之后加：
//       `B97_SMALL_Assert.Run();`
//       `_sink.Pass += B97_SMALL_Assert.Pass; _sink.Fail += B97_SMALL_Assert.Fail;`
//   · 本文件**不跑 Unity 批处理**、不建持久资产、不改任何全局状态（D 段只**读**默认值，不写）。
//
// ============================ 🔴 灭自证（改哪一处会让它红）============================
//   · A2：把 `DraftWarlordTriage.IsNotInInventory` 的 `== NotInInventory` 改成 `!= 0`（或 `>= 5`）
//         ⇒ 那 129 张 `inventoryOptions == 10` 的卡不再被剔除 / `CantAddToDeck(5)` 的 39 张被误剔 ⇒ A2/A3 红。
//   · A5：把 `IsHidden` 改成 `true`（或改成读某个还不存在的 config）⇒ A5 红。
//   · B1：把 `DraftModeWindow.RollWarlords` 里那一行谓词撤掉 ⇒ **不会**红（当前数据 56 个 hero 全过三档）
//         —— 所以 B1 单独**不足以**守住这条接线，A 段才是主判据（如实记，⛔ 不假装 B1 能守）。
//   · C2 / D1/D2：数据 / 默认值一变就红（这正是它们存在的意义）。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B97_SMALL`：五个小域 12 条 B/C 的断言。
/// <para>⛔ **期望值全是字面量**（`10` / `5` / `56` / `null`），**不从被测实现里读** —— 否则就是自证。</para></summary>
public static class B97_SMALL_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B97_SMALL] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_SMALL：轮抽:14 候选三档剔除去 · 轮抽:6 / 其他:218 的 REMOTE 证据钉桩");

        SectionA_候选三档剔除();
        SectionB_RollWarlords过三档();
        SectionC_轮抽6_无到期时刻();
        SectionD_其他218_远端gate默认值();
    }

    // ============================================================ A `轮抽:14` 三档剔除
    static void SectionA_候选三档剔除()
    {
        MenuCheck.Section(_sink, "A `GetPossibleWarlords` 尾巴三档剔除（`out_b94/…GetPossibleWarlords__18109d0.c`）");

        // A0 前提（硬检查，⛔ 不用 `if (前提) Check(结论)` 那种会静默跳过的写法）
        var all = CardDatabase.Load();
        Tr(all != null && all.Count > 0, "A0 前提：卡池加载成功（现读 " + (all == null ? -1 : all.Count) + " 张，期望 > 0）");
        if (all == null || all.Count == 0) { Debug.LogError("[B97_SMALL] A 段前提不成立 ⇒ 后面的断言**没跑**（⛔ 别当成通过）"); return; }

        // A1 字面量口径：`NotInInventory = 10`（`dump.cs:45676` 枚举），且 `CantAddToDeck = 5` **不**是这一档
        C(DraftWarlordTriage.NotInInventory, 10, "A1 ★ 档 A 的字面量 = `CardInventoryOptions.NotInInventory` = 10（`dump.cs:45676`）");

        // A2 真数据：池里存在 `inventoryOptions == 10` 的卡 ⇒ 必被剔除
        CardDef inv10 = null, inv5 = null;
        for (int i = 0; i < all.Count; i++)
        {
            var d = all[i];
            if (d == null) continue;
            if (inv10 == null && d.InventoryOptions == 10) inv10 = d;
            if (inv5 == null && d.InventoryOptions == 5) inv5 = d;
        }
        Tr(inv10 != null, "A2 前提：池里存在 `inventoryOptions == 10` 的卡（现读 129 张，期望 >= 1）");
        if (inv10 != null)
        {
            Tr(DraftWarlordTriage.IsNotInInventory(inv10), "A2 ★ `inventoryOptions == 10` ⇒ 档 A 命中（原版 `+0x4c == 10`）");
            Tr(!DraftWarlordTriage.IsPossibleWarlord(inv10), "A2 ★ 且**不能**当候选战将（卡 id = " + inv10.Id + "）");
        }

        // A3 逐字照抄的边界：`CantAddToDeck(5)` **不**被这一档剔除（原版是 `== 10`，不是 `>= 5`）
        Tr(inv5 != null, "A3 前提：池里存在 `inventoryOptions == 5`（CantAddToDeck）的卡（现读 39 张，期望 >= 1）");
        if (inv5 != null)
            Tr(!DraftWarlordTriage.IsNotInInventory(inv5)
               && DraftWarlordTriage.IsPossibleWarlord(inv5),
               "A3 ★ `CantAddToDeck(5)` **不**被档 A 剔除（原版写的是 `== 10`；改成 `>= 5` 时本条红）· 卡 id = " + inv5.Id);

        // A4 档 B / 档 C 的两格本地恒 false（= 原版该格的取值：`currentAlternateArt` 无写点 / `FeatureConfig` 在远端）
        var hero = FirstHero(all);
        Tr(hero != null, "A4 前提：池里存在 `type == \"hero\"` 的卡（现读 56 张，期望 >= 1）");
        if (hero != null)
        {
            Tr(!DraftWarlordTriage.IsHidden(hero), "A4 ★ 档 B（`PlayerItem.IsHidden()`，虚槽 23）本地恒 false —— 🔶 原版读远端 `FeatureConfig`，关服后取不到");
            Tr(!DraftWarlordTriage.IsAlternateArt(hero), "A4 ★ 档 C（`RawCardScript.IsAlternateArt()` = `+0x88 currentAlternateArt != null`）本地恒 false —— 原版托管层**无写点**");
        }

        // A5 `null` ⇒ false（原版 `.c` 里先判 `!= 0` 才往下；⛔ 别改成 true）
        Tr(!DraftWarlordTriage.IsPossibleWarlord(null), "A5 ★ `null` ⇒ `false`（原版先判 `card != 0`；改成 true 时本条红）");

        // A6 56 张 hero 全部过三档（当前数据下剔除 0 张 ⇒ 改前改后候选池一样大）
        int heroes = 0, passed = 0;
        for (int i = 0; i < all.Count; i++)
        {
            var d = all[i];
            if (d == null || d.Type != "hero") continue;
            heroes++;
            if (DraftWarlordTriage.IsPossibleWarlord(d)) passed++;
        }
        C(heroes, 56, "A6 ★ 池里 `type == \"hero\"` 的张数 = 56（`cards_engine.json` 现读）");
        C(passed, heroes, "A6 ★ 56 张 hero **全部**过三档（`inventoryOptions` 全是 0 ⇒ 本档当前剔除 0 张）");
    }

    static CardDef FirstHero(List<CardDef> all)
    {
        for (int i = 0; i < all.Count; i++) if (all[i] != null && all[i].Type == "hero") return all[i];
        return null;
    }

    // ============================================================ B `RollWarlords` 过三档 + 可复现
    static void SectionB_RollWarlords过三档()
    {
        MenuCheck.Section(_sink, "B `DraftModeWindow.RollWarlords` 的候选全部过三档（接线在位）");

        var a = DraftModeWindow.RollWarlords(777001, 5);
        var b = DraftModeWindow.RollWarlords(777001, 5);
        C(a == null ? -1 : a.Count, 5, "B1 ★ `RollWarlords(777001, 5)` 取满 5 张");
        Tr(a != null && b != null && a.Count == b.Count, "B2 前提：两次调用都拿到结果");
        if (a != null && b != null && a.Count == b.Count)
        {
            bool same = true;
            for (int i = 0; i < a.Count; i++) if (a[i] == null || b[i] == null || a[i].Id != b[i].Id) same = false;
            Tr(same, "B2 ★ 同 seed ⇒ 同结果（一条顺序流，判据 `RandomSeedProvider`）");
        }

        bool allPass = a != null && a.Count > 0;
        if (a != null) for (int i = 0; i < a.Count; i++) if (!DraftWarlordTriage.IsPossibleWarlord(a[i])) allPass = false;
        Tr(allPass, "B3 ★ 返回的每一张都过三档（池子已按 `DraftWarlordTriage.IsPossibleWarlord` 过滤）");
    }

    // ============================================================ C `轮抽:6` 无到期时刻（REMOTE 钉桩）
    static void SectionC_轮抽6_无到期时刻()
    {
        MenuCheck.Section(_sink, "C `轮抽:6 DraftEventBase.Expire` —— 触发源（服务端到期时刻）本地数据里确实不存在");

        string path = Path.Combine(Application.dataPath, "CardPresentation/Resources/draft_packs.json");
        Tr(File.Exists(path), "C1 前提：`Resources/draft_packs.json` 在盘上");
        if (!File.Exists(path)) return;

        string text;
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var sr = new StreamReader(fs, Encoding.UTF8))
            text = sr.ReadToEnd();

        Tr(text.Length > 1000, "C1 前提：文件读得出来（现读 " + text.Length + " 字符）");
        // 原版"活动到期"这一格在任何一起的键名都长这样；本表**一个都不该有**（用户 2026-10-24/25「不自己编到期时钟」）
        Tr(text.IndexOf("\"endAt\"", StringComparison.Ordinal) < 0, "C2 ★ 没有 `endAt` 键");
        Tr(text.IndexOf("\"endDate\"", StringComparison.Ordinal) < 0, "C2 ★ 没有 `endDate` 键");
        Tr(text.IndexOf("\"expireDate\"", StringComparison.Ordinal) < 0, "C2 ★ 没有 `expireDate\"` 键");
        Tr(text.IndexOf("PrebuiltPack", StringComparison.Ordinal) >= 0, "C2 前提：这份表确实是 `PrebuiltPack` 逐包转写（不是别的东西）");
    }

    // ============================================================ D `其他:218` 远端 gate 默认值（REMOTE 钉桩）
    static void SectionD_其他218_远端gate默认值()
    {
        MenuCheck.Section(_sink, "D `其他:218 GameModes.GetActiveEvent(Type)` —— 两个远端注入点的默认值");

        // 只读默认值，不写（⛔ 不动全局状态：本段跑在别的断言之前/之后都不该影响它们）
        C(GameModesHandler.RankedEventInterfaceType, null,
          "D1 ★ `RankedEventInterfaceType` 默认 null ⇒ 原版那条 `typeof(IRankedEvent)` 特殊支**不进入**"
          + "（等价于原版 `mainRankedType == typeof(IRankedEvent)` 那一档：进了支也只是跳过递归、落到同一条 LINQ）");
        C(GameModesConfigSource.MainRankedType, null,
          "D2 ★ `GameModesConfigSource.MainRankedType` 默认 null（远端 `GameModesConfig` 本地 0 命中）");
    }
}
