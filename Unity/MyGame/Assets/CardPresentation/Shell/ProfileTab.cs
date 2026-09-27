using UnityEngine;

namespace CardPresentation
{
    /// <summary>`Profile Tab`（类名 `ProfileTab`）—— 原版字段：`changeNameButton, changeNameWindow, consecutiveLoginCount, eventSection, infoSection, inviteToAllianceButton, playerIdDisplay, rankingSection`。</summary>
    public class ProfileTab : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileInfo; } }
        protected override int PageIndex { get { return 0; } }
        public override PxRect PageRect { get { return new PxRect(PlayerProfileWindow.ContentL, PlayerProfileWindow.RedT, PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); } }

        protected override void Build()
        {
            // ⏳ 内容待建：层 × 参数表在 `资料/普查产出_0927/档案窗_Profile页.md`（2026-09-27 子代理产出中）。
            //    本页根节点已按真值摆好（`PlayerProfileWindow.ContentL … PlayerProfileWindow.ContentRf`，见 `PlayerProfileWindow.BuildPages`）。
            Debug.Log("[Profile] `ProfileTab`（原版页根）**内容还没建** —— 表见 `资料/普查产出_0927/档案窗_Profile页.md`");
        }
    }

}
