// SocialView.cs — 社交窗里的**子视图**（页内再分的那几支）的公共底座。
// 🔴 **2026-10-13 从 `SocialWindow.cs` 拆出**（A1506：Unity 要求 MonoBehaviour 的**类名与所在文件名相同**——
//    它是 `AllianceSearchTab` / `AllianceMemberTab` / `CreateAllianceMenu` 等的基类，
//    `AddComponent<派生>()` 时 Unity 要把整条链都解析成脚本资产）。
//    出处与纪律见 `SocialWindow.cs` 的文件头（本块**逐字搬**，未改一字）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>社交窗里的**子视图**（在某一页内部再分的那几支：`AllianceSearchTab` / `AllianceMemberTab` /
    /// `CreateAllianceMenu`…）。原版这些是挂在页根下面的**独立组件**，不是页。
    /// 它们共用宿主页那一套画图助手，只多一个**页内队列偏移**（`QOff`）：
    /// 一页 10 个号，子视图各占一段，互不重叠（同 `MenuWindowBase.QPanel/QContent` 那条纪律）。</summary>
    public abstract class SocialView : MonoBehaviour
    {
        /// <summary>宿主页（`AlliancesTab` / `FriendsTab`）—— 取图与队列档都走它。</summary>
        public SocialPage Page;

        /// <summary>本视图在**页内**的队列偏移（页那一档是 `SocialWindow.QPageBase + PageIndex*10`）。</summary>
        protected abstract int QOff { get; }

        protected Transform Root { get { return transform; } }

        /// <summary>宿主窗（取图、开别的窗都要它）。</summary>
        protected SocialWindow Win { get { return Page != null ? Page.Win : null; } }

        protected static Transform Node(Transform parent, string name, PxRect r) { return MenuDraw.Node(parent, name, r); }
        protected Transform Node(string name, PxRect r) { return MenuDraw.Node(Root, name, r); }

        // 🔴 **2026-10-14（A753 · A744 的「全删」）：本视图那个 `SetClip`（转调宿主页）已整体删掉。**
        //   子视图画的件也一视同仁 —— 走去**父链上那颗 `ViewportClip`** 这一条路（见 `SocialPage` 顶上那段）。
        //   ⚠️ 原来那个 `Page == null` 的告警随方法一起去掉了；本类里没有任何「页没接上」的静默支路了。

        public ImageQuad Rect(Transform parent, string art, PxRect r, string name, int qOff,
                                 Color? tint = null, bool keepAspect = false)
        { return Page.Rect(parent, art, r, name, QOff + qOff, tint, keepAspect); }

        public GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                                  Color? tint = null, bool fillCenter = true)
        { return Page.Nine(parent, art, r, border, name, QOff + qOff, tint, fillCenter); }

        /// <summary>转调宿主页的 <see cref="SocialPage.Text"/>（队列 = `QOff + qOff`）。
        /// 🆕 **2026-10-08（A213）**：`wrap` 一并转下去 —— `AllianceMemberTab` 那 **9** 处 `v.Text(...)`
        /// 走的就是这条路（真值见 `资料/普查产出_1008/波C3_A212其余_A213_A214.md` §A213 表 B②）；
        /// 同文件另 **4** 处（页签钮 / 奖杯页两行 / 聊天行）是**裸 `Text(...)`**、**不经调用方显式写 `v.`**。
        /// 🔴 **2026-10-12（A323）订正**：原文写那 4 处「走继承来的 **`SocialPage.Text`**、**不经过本函数**」——
        /// **后半句对（确实不走调用点那条 `v.Text(...)`）、前半句错**：`AllianceMemberTab : SocialView`、
        /// 而 **`SocialView : MonoBehaviour`**（与 `SocialPage.cs` 的 `SocialPage : WindowTabBase` **没有继承关系**）——
        /// ⚠️ **2026-10-13 就地订正（铁律 5）**：原写「与**本文件** `:208` 的 `SocialPage : WindowTabBase`」——
        ///   ① 2026-10-13 拆出 `SocialPage.cs` 之后「本文件」已不成立 ⇒ 改指**文件名**；
        ///   ② 那个 `:208` **原本就是错的**（拆出前的真值 = `SocialWindow.cs:257`）⇒ 一并去掉，本注释不再留行号。
        /// ⇒ 那 4 处解析到的是**本函数（`SocialView.Text`）**、不是 `SocialPage.Text`。
        /// **行为上无差别**（两个口的签名与转调逐字同形），改成正确说法只为「文档 = 事实」。
        /// 🔴 **2026-10-11（A317）**：`autoMinPx` / `wrap` 的缺省值**已与 `SocialPage.Text` 同步删掉**
        /// （同一份判据与理由**只写在那边**，别在这儿再写一遍）。
        /// ✅ **2026-10-12（A323 · 收尾半 · 已落地）**：`alignLeft` 的缺省**也删掉了**（与 `SocialPage.Text`
        /// 同一批、同一天）—— 那 13 处阻塞点（`AlliancesTab.cs` 9 + `FriendsTab.cs` 4）已**逐处现读原版**
        /// （全 `Left`）各补 `alignLeft: true`。逐行清单与判据见 `SocialPage.Text` 的头，**别在这儿抄第二份**。
        /// ⚠️ 全仓**只有** `SocialPage` / `SocialView` / 继承它们的 `AlliancesTab`·`FriendsTab`·`AllianceMemberTab`
        /// 这几处会走到本函数 —— 上一批（A258）已把其中 **21 处**补成显式 `wrap:`。
        /// ⛔ **别在这里再补一个「转调专用」的重载来绕开必填** —— 那是把唯一的口子藏起来。</summary>
        public Label Text(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                             int qOff, float autoMinPx, bool wrap, bool alignLeft,
                             float autoMaxPx = 0f, float autoBasePx = 0f,
                             TmpFont.Face face = TmpFont.Face.Cjk)   // 🆕 2026-10-17（A1024③）：可选的字体面
        { return Page.Text(parent, r, text, color, name, fontPx, QOff + qOff, autoMinPx, wrap, alignLeft,
                           autoMaxPx, autoBasePx, face); }

        public Transform Hit(Transform parent, string name, PxRect r, int qOff, System.Action onClick,
                             ImageQuad target = null, string art = null,
                             string hoverArt = null, string pressedArt = null)
        { return Page.Hit(parent, name, r, QOff + qOff, onClick, target, art, hoverArt, pressedArt); }

        /// <summary>**装饰品立绘**（`Resources/Art/avatars/` 那批，名字含空格、原样传）。
        /// 走 `CardArt.Cosmetics` 而**不是** `MenuUi` —— 两批的目录与命名规矩都不同。</summary>
        public ImageQuad Cosmetic(Transform parent, string art, PxRect r, string name, int qOff, bool keepAspect = true)
        {
            Texture2D tex = null;
            if (!string.IsNullOrEmpty(art))
            {
                tex = CardArt.Cosmetics(art);
                if (tex == null && Page != null && Page.Win != null && !Page.Win.MissingArt.Contains(art))
                    Page.Win.MissingArt.Add(art);
            }
            // 🔴 **2026-10-14（A753）**：尾参原来是 `Page.ClipNow`（那个只读口已随 `Clip` 一起删掉）
            //    ⇒ 走缺省 `clip = null`：由**父链上那颗 `ViewportClip`** 说了算。
            return MenuDraw.Rect(parent, tex, r, name, Page.Q + QOff + qOff, null, keepAspect);
        }

        protected static void Say(string what) { SocialPage.Say(what); }
    }
}
