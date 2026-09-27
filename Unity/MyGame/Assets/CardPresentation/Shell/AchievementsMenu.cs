using UnityEngine;

namespace CardPresentation
{
    /// <summary>`Trophies Tab`（**类名 `AchievementsMenu`**、键名 `Trophies`、文案 `Achievements`）—— 原版字段：`categoryTogglePrefab, categoryToggles, containerPrefab, holder, pointsCounter`。</summary>
    public class AchievementsMenu : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileTrophies; } }
        protected override int PageIndex { get { return 4; } }
        public override PxRect PageRect { get { return new PxRect(PlayerProfileWindow.ContentL, PlayerProfileWindow.RedT, PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); } }

        protected override void Build()
        {
            // ⏳ 内容待建：层 × 参数表在 `资料/普查产出_0927/档案窗_Trophies页.md`（2026-09-27 子代理产出中）。
            //    本页根节点已按真值摆好（`PlayerProfileWindow.ContentL … PlayerProfileWindow.ContentRf`，见 `PlayerProfileWindow.BuildPages`）。
            Debug.Log("[Profile] `AchievementsMenu`（原版页根）**内容还没建** —— 表见 `资料/普查产出_0927/档案窗_Trophies页.md`");
        }
    }

}
