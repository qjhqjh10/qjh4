// StrangerProfilePage.cs — 「看别人的档案」时六个页都换成这一个（A3②）。
// 🔴 **2026-10-13 从 `PlayerProfileWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**——
//    它会被 `AddComponent<StrangerProfilePage>()`（`PlayerProfileWindow.BuildPages` 那一带）⇒ 必须同名文件，
//    否则建不出脚本资产、场景存盘会把它烘成内联 `MonoScript` 存根 ⇒ player 加载即崩）。
//    出处与纪律见 `PlayerProfileWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 别人那一份（A3②）

    /// <summary>🆕 2026-10-03（§三 第 29 条 A3②）：**看别人的档案**时，六个页都换成这一个。
    ///
    /// 原版：排行榜的行被点 ⇒ `profileButton` ⇒ 开**那个玩家**的档案窗，六页都由服务器填。
    /// 我们：**没有别人的数据**（全在服务器），也**不拿本地自己那一份冒充他**（那会是假信息）
    /// ⇒ 照红线「不许静默失败」：开**同一扇窗**（外壳/左栏/关闭钮一模一样），
    /// 每页画两行**如实说明**。判据（原版行为）→ `LeaderboardRow.OnRowClicked` 的注释。</summary>
    public class StrangerProfilePage : ProfilePage
    {
        public override WindowTabType Type { get { return PlayerProfileWindow.TabTypeOf(PageIdx); } }

        public override PxRect PageRect
        {
            get
            {
                // 与六个具体页同值：`Avatar`/`Title` 两页两侧各溢 16.33（判据 → `BuildPages` 那段）
                float l = (PageIdx == 1 || PageIdx == 2) ? PlayerProfileWindow.WideL : PlayerProfileWindow.ContentL;
                return new PxRect(l, PlayerProfileWindow.RedT, PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB);
            }
        }

        protected override void Build()
        {
            string who = Win != null ? Win.ViewedPlayer : null;
            var r = PageRect;
            float cx = r.CX, cy = r.CY;

            // 🆕 2026-10-18（波 1b · P2b）：这两句改走语言表（波 0b 补进表的那 111 条之一）。
            // ⚠️ `MainMenu/Social/ProfileTitle` 是**带占位符**的键（`「{0}」的档案` / `{0}'s profile`）
            //   ⇒ 走 `string.Format`，⛔ 别用 `Replace`（键名里就有花括号，两列都要能换）。
            // ⚠️ `who` 是**运行期值**（看的是哪个玩家）⇒ 不翻，只做参数（施工单 §③「玩家数据不翻」）。
            var l1 = MenuDraw.Text(Root, new PxRect(cx - 700f, cy - 70f, cx + 700f, cy + 10f),
                                   string.Format(Loc.T("MainMenu/Social/ProfileTitle"), who ?? "?"),
                                   new Color(0.98f, 0.686f, 0.169f, 1f),
                                   "Stranger Name", 45f, Q, 1400f, 23f);   // A1024③ 判不出（自建页）⇒ 保持 Cjk
            if (l1 != null) l1.SetRenderQueue(Q);
            var l2 = MenuDraw.Text(Root, new PxRect(cx - 700f, cy + 20f, cx + 700f, cy + 120f),
                                   Loc.T("MainMenu/Social/ProfileOfflineNote"),
                                   Color.white, "Stranger Note", 32f, Q + 1, 1400f, 18f);   // A1024③ 判不出（自建页）⇒ 保持 Cjk
            if (l2 != null) l2.SetRenderQueue(Q + 1);

            // 🆕 账 未分类:479（原版 `PlayerInfo.SetBotInfo`）：**bot 档案**那一格 —— 名字已经走上面的 `who`
            //   （`Open()` 的 `ApplyBotInfo()` 把 `ViewedPlayer` 铺成 `BotName`），这里补 ELO。
            // 🔴 **只在 `BotELO > 0` 时显示**：原版第 4 句是 `if (0 < botElo) CurrentEloRating = botElo`
            //   ⇒ `botElo <= 0` 时**不写那一格**（保留原值）；我们这边「保留原值」= 什么都不显示（没有原值可言）。
            int botElo = (Win != null && Win.IsBot) ? Win.BotELO : 0;
            if (botElo > 0)
            {
                var l3 = MenuDraw.Text(Root, new PxRect(cx - 700f, cy + 130f, cx + 700f, cy + 200f),
                                       "ELO " + botElo, Color.white, "Bot Elo", 32f, Q + 2, 1400f, 18f);
                if (l3 != null) l3.SetRenderQueue(Q + 2);
            }

            Debug.Log("[Profile] 看**别人**的档案：「" + (who ?? "?") + "」的 " + Tabs[PageIdx]
                      + (Win != null && Win.IsBot
                            ? " 页 —— 这是 **bot 档案**（原版 `PlayerInfo.SetBotInfo`）：名字来自开窗数据，"
                              + (botElo > 0 ? "ELO = " + botElo : "ELO 未给（`botElo <= 0` ⇒ 原版不写那一格）")
                            : " 页 —— 原版这一页的数据**全在服务器**（那个玩家的存档）"
                              + "⇒ 我们**不拿本地自己那一份冒充他**，如实画一行说明（§三第29条 A3②）"));
        }

        public static readonly string[] Tabs =
        { "Profile", "Avatar", "Title", "Battle Log", "Trophies", "Ranking" };
    }
}
