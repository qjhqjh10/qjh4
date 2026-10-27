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
// · ⑤ 的**勋章图**：❌ 没做 —— 我们的 Toast（`Shell/MessageToast.cs`）**只有文字**，没有图位；
//   原版那是个 `ToastNotificationContext(text, Sprite, …)`。⇒ 本行判 **B**（不是 A）。
//   图名口径已备：`40k_Achievements_icon_medal{档}`（`Shell/AchievementsMenu.cs` 的 `ArtMedalPrefix`，
//   ⚠️ 档↔图那一层在那边已标「推断，非证据」）。要补 = 给 `MessageToast` 加图位（那份文件不在本件白名单）。
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
        public static void ResetForTest() { FiredCount = 0; LastText = null; }

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

        static void Fire(string achievementId, int tier)
        {
            string text = ToastText(achievementId, tier);
            FiredCount++;
            LastText = text;
            // ⚠️ 原版还带勋章图（`GetIconByTier`）；我们的 Toast 只有文字 ⇒ 本行判 B，见文件头。
            if (!MessageToast.Show(text))
                Debug.LogWarning("[Achievement] 跨档 Toast 没弹出来（`MessageToast` 没有实例）：\"" + text + "\"");
            // ⑦ Steam `SetAchievement` / `StoreStats` = P3（原版客户端 SDK）⇒ 不做。
            Debug.Log("[Achievement] 跨档：`" + achievementId + "` 到第 " + tier + " 档 —— " + text);
        }
    }
}
