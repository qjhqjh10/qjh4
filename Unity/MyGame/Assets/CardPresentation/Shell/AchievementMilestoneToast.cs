// AchievementMilestoneToast.cs — 原版 `Everguild.LiveOps.Achievement.OnReachMilestone(ChallengeMilestone)`
// 的**客户端那一半**（成就跨档 Toast）。
//
// ============================ 为什么单开一个文件 ============================
// `Shell/ProfileData.cs` 里在账里还有 5 条落点排在 `AddAchievementValue`（`:1191`）**之后**
// （`:1203` ×2 个账本 · `:1228` ×2）⇒ 往那个方法体里**加一行**会连带漂移。
// 本笔按 `CLAUDE.md` 铁律 18·b 的精神（**零行数变化**）把逻辑放**新文件**，
// `ProfileData.AddAchievementValue` 里只在**既有那一行末尾**追加一次回调（行数不变）。
//
// ============================ 出处（第一权威）============================
// 🥇 `d:/2/tools/decomp_full/Everguild.LiveOps.Achievement__OnReachMilestone.c`
// 🥈 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/Achievement.cs:658-719`（逐句同形）
//
//   ① `tier = challenge.rewards.IndexOf(currentChallenge) + 1`
//   ② `msg   = I2("Achievements/NewAchievementMessage")`            → `"New achievement unlocked!\n{0}."`
//   ③ `title = I2("Achievements/Title/" + GetID())`（取不到 ⇒ 空串）
//   ④ `arg   = title + " " + tier`  → `text = string.Format(msg, arg)`
//   ⑤ `icon  = AchievementsManager.GetIconByTier(tier)` → `ToastNotificationContext(text, icon, …)`
//      → `ToastNotificationController.ScheduleNotification(ctx)`
//   ⑥ `CurrentPoints += rewards.FirstOrDefault().Quantity`
//   ⑦ Steam：`SteamUserStats.SetAchievement($"{GetID()}_{tier}")` + `StoreStats()`
//
// ============================ 我们只做哪一半（如实记）============================
// · ②③④⑤ 的**文字部分**：✅ 复刻（三个键现读全部在 `Resources/official_loc_13lang.json`：
//   `Achievements/NewAchievementMessage` 有、`Achievements/Title/Ach_1..Ach_102` **102 条全在**，
//   而成就 id 现读就是 `Ach_1..Ach_102`（`Resources/profile_cosmetics.json` 的 `achievements`）⇒ 逐条对得上）。
// · ⑤ 的**勋章图**：✅ **2026-10-28（B89-E16）已复刻** —— 走 `ToastNotificationController`（**不是** `MessageToast`）：
//   `ToastNotifications.Schedule(ToastNotificationType.Achievement, text, icon)` + `MedalArtOf(tier)`；
//   档↔图 = `defaultTierIcon[tier-1]`（`AchievementsManager__GetIconByTier.c`），5 条 PPtr 落名 medal1..5 = **证据**。
//   🔴 原写「我们的 Toast 只有文字、没有图位」是**指错了通道**：`MessageToast` = `UIMessageController`（错误横幅），不是这条链。
// · ⑥：✅ **等价** —— 我们的 `CurrentPoints` 不是可变字段，而是 `ProfileData.AchievementPointsOf`
//   **按已达档求和**（`ProfileData.cs:1228`）⇒ 跨档之后那个和自然 +该档 `Quantity`，**不需要再写一次**。
// · ⑦ Steam：❌ P3（原版客户端 SDK / 账户域）⇒ 不做，如实记。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>成就**跨档**时弹一条 Toast（原版 `Achievement.OnReachMilestone`）。
    /// 触发点 = <see cref="ProfileData.AddAchievementValue"/>（本仓成就进度**唯一的累加写口**）。</summary>
    public static class AchievementMilestoneToast
    {
        // ---- 官方词条（原版 `Localize.mTerm` 原文；`official_loc_13lang.json` 逐条命中）----
        public const string TermMessage = "Achievements/NewAchievementMessage";
        public const string TermTitlePrefix = "Achievements/Title/";

        /// <summary>本进程一共弹过几条（自检读口；⛔ 别拿它当业务状态）。</summary>
        public static int FiredCount { get; private set; }
        /// <summary>最后一条弹出去的文字（自检读口）。</summary>
        public static string LastText { get; private set; }
        /// <summary>自检专用：清零计数（**必须清** —— 否则下一条断言量到的是上一条的）。</summary>
        public static void ResetForTest() { FiredCount = 0; LastText = null; LastIconArt = null; }

        /// <summary>进度从 <paramref name="before"/> 变到 <paramref name="after"/> 之后调用。
        /// **每跨过一档**弹一条（原版是「每命中一个 milestone 调一次 `OnReachMilestone`」）。
        /// `before &gt;= after` ⇒ 什么都不做（原版不会倒着走）。</summary>
        public static void OnValueChanged(string achievementId, int before, int after)
        {
            if (string.IsNullOrEmpty(achievementId) || after <= before) return;
            var a = ProfileData.AchievementById(achievementId);
            if (a == null || a.Thresholds == null) return;
            for (int i = 0; i < a.Thresholds.Length; i++)
            {
                int thr = a.Thresholds[i];
                if (before < thr && after >= thr) Fire(achievementId, i + 1);   // `tier = IndexOf(rewards) + 1`
            }
        }

        /// <summary>原版第 ②③④ 步：`Format(I2(NewAchievementMessage), I2("Achievements/Title/"+id) + " " + tier)`。
        /// ⚠️ 称号键取不到 ⇒ **空串**（原版 `translation2 == null ⇒ arg = ""`），⛔ 别把键名印上屏。</summary>
        public static string ToastText(string achievementId, int tier)
        {
            string title = Loc.T(TermTitlePrefix + achievementId);
            if (string.IsNullOrEmpty(title) || title == TermTitlePrefix + achievementId) title = "";
            string fmt = Loc.T(TermMessage);
            if (string.IsNullOrEmpty(fmt) || fmt == TermMessage) fmt = "{0}";
            // 原版 `$"{arg} {tier}"` —— 称号为空时这里会留下一个前导空格（照抄，⛔ 不「修好看」）
            return string.Format(fmt, title + " " + tier);
        }

        /// <summary>最后一条用的勋章图名（自检读口；= 原版 `GetIconByTier(tier)` 的返回值）。
        /// 声明在**类尾**：它必须在账落点 `:56`（`OnValueChanged`）之后，否则那条行号要漂。</summary>
        public static string LastIconArt { get; private set; }

        /// <summary>原版 ⑤ 的 `AchievementsManager.GetIconByTier(int tier)` —— **有证据的档↔图映射**。
        /// <para>判据（2026-10-28 现读，三级都读过）：① `decomp_full/AchievementsManager__GetIconByTier.c` 的体 =
        /// `arr = *(this + 0x40); if ((uint)(tier - 1) &lt; arr.length) return arr[tier - 1];`（越界抛 `IndexOutOfRange`）；
        /// ② 字段名 = `Sprite[] defaultTierIcon`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AchievementsManager.cs`）；
        /// ③ 实例在 `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity:7141`
        /// （`MonoBehaviour &amp;374`，`m_Script` guid `4fff846a8757f563d16399ca02065c86` = `AchievementsManager`）——
        /// 那 5 条 PPtr 的 guid 逐个落到 `Assets/Sprite/40k_Achievements_icon_medal1..5.asset.meta`
        /// ⇒ **档 1..5 ↔ medal1..5 是证据，不是推断**（🔴 铁律 5：`AchievementsMenu.cs` 里「这是推断，不是证据」那两句已就地订正）。</para>
        /// <para>越界（本仓成就最多 5 档：`profile_cosmetics.json` 现读 **100 条 × 5 档 + 2 条 × 1 档**）⇒ **出声 + 不出图**：
        /// 原版这里会抛异常；我们既不能抛（会把跨档链打断），也不能静默（那等于「这条成就没有勋章」）。</para></summary>
        public static string MedalArtOf(int tier)
        {
            if (tier >= 1 && tier <= AchievementsMenu.MedalMaxTier)
                return AchievementsMenu.ArtMedalPrefix + tier;
            Debug.LogWarning("[Achievement] `GetIconByTier(" + tier + ")` 越界（原版这一支抛 `IndexOutOfRange`）"
                           + " ⇒ 这条跨档提示**不带勋章图**（⛔ 不拿别的档顶上去）");
            return null;
        }

        static void Fire(string achievementId, int tier)
        {
            string text = ToastText(achievementId, tier);
            string icon = MedalArtOf(tier);
            FiredCount++;
            LastText = text;
            LastIconArt = icon;
            // ⑤ = 原版 `ToastNotificationController.ScheduleNotification(new ToastNotificationContext(text, icon))`
            //   （🥇 `Everguild.LiveOps.Achievement__OnReachMilestone.c`：`AchievementsManager__GetIconByTier` →
            //    `ToastNotificationContext___ctor(ctx, text, icon, …)` → `ToastNotificationController__ScheduleNotification`）。
            // 🔴 **原来这里走 `MessageToast` = `UIMessageController`（屏幕上方那条错误/分享横幅）—— 走错通道**：
            //   原版是两个独立系统，成就归 `Toast Notification Controller`（`ToastNotificationType.Achievement = 0` → `Toast` 模板，
            //   带 `Icon` 图位）。判据 → `Shell/ToastNotifications.cs` 文件头 ①②。
            if (!ToastNotifications.Schedule(ToastNotifications.ToastNotificationType.Achievement, text, icon))
                Debug.LogWarning("[Achievement] 跨档提示没排上（壳里没有 `ToastNotificationController`）：\"" + text + "\"");
            // ⑦ Steam `SetAchievement` / `StoreStats` = P3（原版客户端 SDK）⇒ 不做。
            Debug.Log("[Achievement] 跨档：`" + achievementId + "` 到第 " + tier + " 档 —— " + text);
        }
    }
}
