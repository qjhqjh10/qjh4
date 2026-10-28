// B94Y3_Assert.cs — 执行代理 `B94_Y3`（第九十四会话 · 波 1.5）的断言
//
// ============================ 它盯的是什么 ============================
//   `BadgeNotifications.cs` 里两个**具体徽章**的类名原来是我们自造的，已按原版改名：
//     · `AlliancesBadge` → `AlliancesNotificationBadge`
//     · `SupportBadge`   → `SupportNotification`
//   缺陷形状（真实，不是洁癖）：红点计数走 `BadgeNotificationDefinition.Validate`
//   的「不过滤」那一支 = `notificationType.IsInstanceOfType(item)`（`BadgeNotifications.cs` 见 :578 那段）。
//   原版 provider 的泛型实参是 `AlliancesNotificationBadge` / `SupportNotification`
//   （`AlliancesManager : INotificationProvider<AlliancesNotificationBadge>` `AlliancesManager.cs:12`；
//    `HelpshiftManager : INotificationProvider<SupportNotification>` `HelpshiftManager.cs:9`）。
//   ⇒ 类名（= 发出去的通知类型）与 provider 声明的泛型实参**不符**时，`IsInstanceOfType` **恒 false**
//     ⇒ 计数恒 0（= Missions 那颗踩过的同一个坑，见 `报告/B94_T1.md`）。
// 本文件锁的就是：**「发出去的通知类型 == provider 声明的泛型实参」**，名字不符就红。
// 逐条判据 / 取舍 → `资料/完全复刻/报告/B94_Y3.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
//   `B94Y3_Assert.Run();`
//   + 宿主里两行（照 `B94T1_Assert` / `B94Z1_Assert` 的先例）：
//     `_sink.Pass += B94Y3_Assert.Pass; _sink.Fail += B94Y3_Assert.Fail;`
//   **建议宿主 = `Editor/MainMenuScene.cs`**（红点接线表就在那儿；⛔ 接线由调度台做）。
//   ⚠️ 本组**不建整窗**、不建持久资产、不改任何数据源 —— 只做类型身份 + 定义表校验。
//
// ============================ 🔴 灭自证 ============================
//   每条「正向」都配一条**结构上不可能同时满足**的反向（见断言文案里的「🧨」）：
//   发**别的**类型 ⇒ 不命中。否则「什么类型都算」也能绿，等于没测。
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B94_Y3`：徽章类名按原版对齐（发出去的通知类型 == provider 声明的泛型实参）。</summary>
public static class B94Y3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[Y3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_Y3 · 徽章类名按原版对齐（发出去的通知类型 == provider 声明的泛型实参）");
        SectionAlliances();
        SectionSupport();
        SectionNameTable();
    }

    // ------------------------------------------------------------ ① AlliancesNotificationBadge
    static void SectionAlliances()
    {
        // 原版 provider 声明：`AlliancesManager : INotificationProvider<AlliancesNotificationBadge>`（AlliancesManager.cs:12）
        // 「provider 声明的泛型实参」= typeof(AlliancesNotificationBadge)；
        // 「发出去的通知类型」= 原版 `CheckNotification()` 里 `new AlliancesNotificationBadge(...)` 的 GetType()。
        var emitted = new AlliancesNotificationBadge();
        C(emitted.GetType(), typeof(AlliancesNotificationBadge),
          "①a 发出去的通知类型 == provider 声明的泛型实参（`AlliancesNotificationBadge`）"
        + "。🧨 灭自证：若改回自造名 `AlliancesBadge`，它就不再等于 provider 泛型实参 ⇒ 编译/类型判定断");

        var defs = BadgeWiring.DefinitionsForTab("Alliances Tab Button");
        Tr(defs != null && defs.Count == 1,
           "①b 前提：`Alliances Tab Button` 有 1 条红点定义（没有 ⇒ ①c/①d/①e 空转，必红）");
        if (defs == null || defs.Count != 1) return;

        C(defs[0].Type, typeof(AlliancesNotificationBadge),
          "①c 定义里登记的类型（`IsInstanceOfType` 的左边）== provider 泛型实参");
        Tr(defs[0].Validate(emitted),
           "①d `IsInstanceOfType` **命中** ⇒ 接上真 provider 那天，计数不会是恒 0（这就是原缺陷的修复口）");
        Tr(!defs[0].Validate(new SocialBadge()),
           "①e 🧨 灭自证：发**别的**类型（`SocialBadge`）⇒ **不命中** ⇒ ①d 的正数才有意义"
         + "（= 类型不符会恒 false，这正是原缺陷）");
    }

    // ------------------------------------------------------------ ② SupportNotification
    static void SectionSupport()
    {
        // 原版 provider 声明：`HelpshiftManager : INotificationProvider<SupportNotification>`（HelpshiftManager.cs:9）
        var emitted = new SupportNotification();
        C(emitted.GetType(), typeof(SupportNotification),
          "②a 发出去的通知类型 == provider 声明的泛型实参（`SupportNotification`）"
        + "。🧨 灭自证：若改回自造名 `SupportBadge` ⇒ 编译/类型判定断");

        var def = new BadgeNotificationDefinition(typeof(SupportNotification));
        Tr(def.Validate(emitted),
           "②b `SupportNotification` 定义命中（`IsInstanceOfType` 认得出）—— 与 Alliances 同一条链");
        Tr(!def.Validate(new InboxBadge()),
           "②c 🧨 灭自证：别的类型不命中 ⇒ ②b 才有意义");
    }

    // ------------------------------------------------------------ ③ 全族名字表（防自造名回归）
    static void SectionNameTable()
    {
        // 原版 `Everguild.UI.Notifications` 的 15 个**具体**徽章类名（逐文件现读
        // `cpp2il/out_cpp2il_dev_ilspy/Everguild.UI.Notifications/`；不含抽象基类 `Notification`）。
        // 我们的每个具体徽章都必须以**原版类名**存在于 `CardPresentation` 程序集。
        var want = new string[]
        {
            "InboxBadge", "CollectionBadge", "ShopBadge", "InventoryBadge", "SocialBadge",
            "FriendsBadge", "AlliancesNotificationBadge", "SupportNotification", "RewardsBadge",
            "MissionsBadge", "CampaignBadge", "ForgeBadge", "BoosterPackBadge", "OfferBadge", "ChallengeMatch",
        };
        var asm = typeof(BadgeNotification).Assembly;
        var missing = new List<string>();
        for (int i = 0; i < want.Length; i++)
            if (asm.GetType("CardPresentation." + want[i]) == null) missing.Add(want[i]);

        C(missing.Count, 0,
          "③a 15 个具体徽章类全部以**原版类名**存在于 `CardPresentation` 程序集（自造名 / 漏改名会少一个 ⇒ 红；"
        + "缺：" + (missing.Count == 0 ? "无" : string.Join("、", missing.ToArray())) + "）");

        // 反查：**自造名**不该再出现在程序集里（AlliancesBadge / SupportBadge 必须已消失）
        Tr(asm.GetType("CardPresentation.AlliancesBadge") == null,
           "③b 🧨 自造名 `AlliancesBadge` 已从程序集消失（否则说明改名没落地）");
        Tr(asm.GetType("CardPresentation.SupportBadge") == null,
           "③c 🧨 自造名 `SupportBadge` 已从程序集消失");

        // 基类未被改名（仍叫 `BadgeNotification`）—— 锁住「本轮只改具体徽章、基类留待另案」这一事实
        Tr(asm.GetType("CardPresentation.BadgeNotification") != null,
           "③d 基类仍是 `BadgeNotification`（未改名成原版 `Notification`）—— 本轮范围之外，锁住这一事实");
    }
}
