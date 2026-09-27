using UnityEngine;

namespace CardPresentation
{
    /// <summary>`Battle Log Tab`（类名 `BattleLogTab`）—— 原版字段：`holder, logPrefab`。🔴 `logPrefab` 是**对局历史行**的模板，后面做「对局历史」那件要直接用它。</summary>
    public class BattleLogTab : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileBattleLog; } }
        protected override int PageIndex { get { return 3; } }
        public override PxRect PageRect { get { return new PxRect(PlayerProfileWindow.ContentL, PlayerProfileWindow.RedT, PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); } }

        protected override void Build()
        {
            // ⏳ 内容待建：层 × 参数表在 `资料/普查产出_0927/档案窗_BattleLog与页签按钮.md`（2026-09-27 子代理产出中）。
            //    本页根节点已按真值摆好（`PlayerProfileWindow.ContentL … PlayerProfileWindow.ContentRf`，见 `PlayerProfileWindow.BuildPages`）。
            Debug.Log("[Profile] `BattleLogTab`（原版页根）**内容还没建** —— 表见 `资料/普查产出_0927/档案窗_BattleLog与页签按钮.md`");
        }
    }

}
