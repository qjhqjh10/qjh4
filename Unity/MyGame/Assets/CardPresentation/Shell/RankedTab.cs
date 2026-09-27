using UnityEngine;

namespace CardPresentation
{
    /// <summary>`Ranking Tab`（**类名 `RankedTab`**、键名 `Ranked`、文案 `Ranking`）—— 原版字段开头 `divisionImage, …`（未读全）。⚠️ 别与 Profile 页里那个 `Ranking` 块、以及外面的排行榜弹窗搞混。</summary>
    public class RankedTab : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileRanking; } }
        protected override int PageIndex { get { return 5; } }
        public override PxRect PageRect { get { return new PxRect(PlayerProfileWindow.ContentL, PlayerProfileWindow.RedT, PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); } }

        protected override void Build()
        {
            // ⏳ 内容待建：层 × 参数表在 `资料/普查产出_0927/档案窗_Ranking页与图名表.md`（2026-09-27 子代理产出中）。
            //    本页根节点已按真值摆好（`PlayerProfileWindow.ContentL … PlayerProfileWindow.ContentRf`，见 `PlayerProfileWindow.BuildPages`）。
            Debug.Log("[Profile] `RankedTab`（原版页根）**内容还没建** —— 表见 `资料/普查产出_0927/档案窗_Ranking页与图名表.md`");
        }
    }

}
