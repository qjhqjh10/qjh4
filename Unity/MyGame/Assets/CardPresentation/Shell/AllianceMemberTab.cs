// AllianceMemberTab.cs — 社交窗「联盟」页的**第二支**：已在盟里那一态
//   （原版 `AllianceMemberTab`，挂在 `AllianceNotMemberVariant` 的**兄弟**节点 `AllianceMemberVariant` 下，
//     **出厂 act = F**）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/社交_联盟与好友页.md` §B·5（**两个 `GeneralDetails` 不是同一份**）· §B·11（二级页签）·
// §A·1 第 **255–379 行**（这一支那 120 个节点）· §A·2·3（成员行 `AllianceMemberEntry` 独立根）。
//
// ---- 🔴 三条判据 ----
// ① **这一支本地【走不到】**：原版 `AlliancesTab.ToFocus` 按 `AlliancesManager.CurrentGroupCached`（服务器）
//    在 `AllianceSearchTab`(0x30) 与 `AllianceMemberTab`(0x38) 之间二选一 —— 本地那个值恒空
//    ⇒ **永远停在未入盟支**（判据 → `多人界面_入口与调用.md` §③ 社交那一条）。
//    **我们照样把它建出来**（用户口径「有什么复刻什么」），只是**永远不亮** —— 这是**忠实地走不到**，
//    不是漏做。自检里我们**手动 `SetActive(true)`** 把它验一遍（`MainMenuScene` 那一段）。
// ② **`GeneralDetails` 有两份、同名不同 pid**（§B·5）：**这一支（`AllianceMemberVariant`）用的是
//    RT `-4327119531760820061`（act T）那份**；未入盟支（`AllianceNotMemberVariant`）那份是
//    RT `-8680982849342087005`（act F）。
//    ⇒ 我们的做法是**一份 builder、两棵都建**（`AllianceGeneralDetails.Build(..., Variant)`）：
//      **这一棵**（`AllianceMemberTab`）几何取 **act T**（A29，2026-10-04）；
//      **未入盟支那一棵**（`AllianceSearchTab`）几何取 **act F**、出厂 act F（A55②，2026-10-04，
//      见 `AlliancesTab.cs` 的 `BuildGeneralDetails()` 与 `HandleDisplayAlliance`）。
//    🔴 **2026-10-04（A29）：改之前那棵树是「两份实例各取一半」**（上半段 act F / 下半段 act T）——
//      判据、逐节点清单与这次的处置都写在 `AllianceGeneralDetails.Build` 那段注释里。
// ③ **二级页签 `General` / `Trophies` 只在这一支里有**（§B·11）：`AllianceMemberTab.generalButton` →
//    `Generic Tab UI Button Info`（文案 **`General`**，节点名却是 `Info`）/ `trophiesButton` → `…Trophies`。
//    ⚠️ `TrophiesWindow` 出厂 act **F** ⇒ 点 `Trophies` 才亮（这个切换是纯本地的，**能用**）。
//
// ---- 出厂 act=F 的件（**不建**，逐条写明为什么）----
//   · 🔴 **2026-10-04 更正（A29）**：这一行原来写「`Config fields` 下的 `LanguagesDropdown` /
//     `Privacy Dropdown` / `Edit` / `Confirm` / `Cancel`（五个 act F ⇒ 不建）」—— **那是另一份实例
//     （act F）的取值**。**这一支（act T）里那五个全是 `T`、反而是 `extra_info` 是 `F`**
//     （普查 `社交_联盟与好友页.md:279-315`）⇒ 现在**建那五个、去掉 `extra_info`**
//     （判据与逐值见 `AllianceGeneralDetails.Build` 的「A29」那一段）。
//   · 两处 `Secondary Icon`（评级旁边的小图标，act F）；
//   · 成员行里的 `Avatar Name`（act F）与 `Raycast Target`（act F，那是命中层，我们另建 `Hit`）；
//   · `GeneralDetails>DEBUG_TEXTS`（原档调试残留，§C·2 明说别照抄）；
//   · 两个下拉的 `Template`（Unity 内置模板）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `AllianceMemberTab`。</summary>
    public class AllianceMemberTab : SocialView
    {
        protected override int QOff { get { return AlliancesTab.QBandMember; } }

        // 队列档（本视图内 0–4）
        const int L_Panel = 0, L_Btn = 1, L_Art = 2, L_Text = 3, L_Hit = 3;
        const int L_Line = 1;

        // ---- 奖杯格 `TrophyDisplay` **内部**那一叠（🔴 按**原版兄弟序**派，⛔ 不是按「种类」派）----
        //   判据 = 原始 JSON 的 `m_Children`（uGUI 按兄弟序画 ⇒ **后面的压前面的**）：
        //     · `bundle_menus_assets_all/RectTransform/RectTransform_-3094581417016303453.json`
        //       （`TrophyDisplay`）= `[Collectable Highlight, bg, BadgeDrawer, title, Progress]`
        //       ⇒ `bg` **压** `Collectable Highlight`（那圈描边只在格子**外**沿露出来）；
        //     · `…/RectTransform_8075696931376455843.json`（`ProgressBar`）= `[Background, Outline, counter]`，
        //       而 `end` 是 `…/RectTransform_-1375540381073612637.json`（`Fill`）**唯一**的子
        //       ⇒ `Outline` **压** `end`（两者重叠 ≈ 5.7px）。
        //   ⇒ 格子里从下到上 = Ring < Back < Slot < End < Frame < Text（**6 层**）。
        //   ⚠️ **一处如实标注的「近似」（FX-1 记的，本批照旧）**：原版 `title` 是 `TrophyDisplay` 的第四颗子件、
        //      `Progress` 是第五颗 ⇒ 严格照兄弟序，进度条整叠应当压在 `title` **上面**；这里把 `title` 摆在
        //      `LC_Text`（最上一档）。两者**不重叠**（见 `BuildTrophyCell` 里那一行的算式：`title` 底边 = 格 +277.83
        //      < 进度条顶边 = 格 +282.90）⇒ **画面上分不出来**。**这一条不是「照原版」，是「已知且不可见」的近似**。
        //      🔴 **2026-10-04（A74②）为什么不顺手改成严格版**：严格版要 **7 个号/格**
        //      （`Ring < bg < title < 槽底 < end < 描边 < counter`），而带子只有 **90** 号、格数上限 **15**
        //      ⇒ `7 × 15 = 105 > 90`，装不下（多出来的 15 号会撞上聊天窗那一段 3300–3308）。
        //      按 FX-1 那条告诫（「别借用旁边的号」）**保持现状**，不拿 `LC_Slot` 去顶 `title`。
        //
        // ============================================================================================
        // 🔴 **2026-10-04（A74②）：队列号改成【按格号错开】—— 一格一整段带**
        // ============================================================================================
        //   **为什么**（判据 = 原始 `m_Children` 兄弟序 + **零间距**网格 `AllianceTrophyGrid.GapX = 0`）：
        //     原版 `Item Drawer` 是 `GridLayoutGroup`，每格 `TrophyDisplay` 是**按序 Instantiate 的兄弟**，
        //     而 uGUI **按兄弟序整棵整棵地画** ⇒ **第 i 格的整棵子树都画在第 i−1 格整棵子树之上**。
        //     这件事在 `Collectable Highlight` 上**看得见**：它比格子大一圈
        //     （`−31.07 / −41.38 / +31.39 / +31.36`），而 `GapX = 0`（两格紧挨着）
        //     ⇒ 它的**左/上那一圈压在左邻、上邻身上**（原版：后画的整格赢），
        //     **右/下那一圈被右邻、下邻压住**。
        //     ⛔ 我们原来按「**同类同号**」摆（`hl` 全格一个号、`bg` 全格一个号）⇒ 第 i 格的 `hl`
        //     落在第 i−1 格的 `bg` **下面** —— 与原版**正好相反**（今天 `hl` 恒灭 ⇒ 看不见，但这是真偏离）。
        //   **做法**：每格一段 **6 个号**的带、格号越大带越高 ⇒ 格内那 6 层的相对次序不变，
        //     而**任意两格的任意两件**都保持「格号大的在上」= 原版的整棵子树次序。
        //   **带 = 3210–3299（90 个号）**：
        //     · 起点 3210 = 本页带 3200–3209 **之后**（⛔ 不再借 3204 —— 见下面那条更正）；
        //     · 终点 3299 = 聊天窗 `ChatPanel.QBase = 3300` **之前**（层带不许重叠：那一档是别的窗的）。
        //   **为什么 90 个号够（= 最多 15 格）**：只有**与视口相交**的格才建（`BuildTrophyRows` 里
        //     `MenuDraw.ClipRect` 那一支），而视口高 `1079.84 − 378.51 = 701.33`、排距 `354 + 15 = 369`
        //     ⇒ 偏移落在 `(59.67, 377)` 时**三排同时可见**（第 1 排底 755.51 − s > 378.51 且
        //     第 3 排顶 1139.51 − s < 1079.84）⇒ 最多 **3 排 × 5 列 = 15 格**。
        //     🔴 **这道上限要守住**：超了会越到聊天窗那一档上 ⇒ `BuildTrophyRows` 里数到 `QCellMax`
        //     就 `LogError` **出声**并夹住（⛔ 不许静默越界）。
        //   ⚠️ **编号取「建出来的第几格」而不是 `n` 里的下标 `i`**：视口外的格**不建** ⇒ 下标会跳号，
        //     而建出来的那一批在 row-major 序里是**连续**的一段 ⇒ 按建出来的次序编号，相对次序与原版逐格相同。
        const int QCellBase = 3210;        // 奖杯格那一带的起点（页带 3200–3209 之后、聊天窗 3300 之前）
        const int QCellStride = 6;         // 一格 6 个号（= 格内那 6 层）
        const int QCellMax = 15;           // 带子装得下的格数 = 5 列 × 3 排（见上面那道几何推导）
        // 格内阶梯（**一格内**的偏移，0 最底、5 最上）：
        const int LC_Ring = 0;             // `Collectable Highlight`（最底：只露格子外那一圈）
        const int LC_Back = 1;             // 格子底板 `bg`
        const int LC_Slot = 2;             // `ProgressBar>Background`（槽底）
        const int LC_End = 3;              // `Fill>end`（端帽）
        const int LC_Frame = 4;            // `ProgressBar>Outline`（描边，压 `end`）
        const int LC_Text = 5;             // `title` / `counter`（字压一切；`Hit` 仍用 `L_Hit`：透明区不吃层序）
        /// <summary>一格占的那一段带里的**第 `layer` 层号**。`builtIndex` = **建出来的第几格**（⛔ 不是 `n` 的下标）。</summary>
        static int CellQueue(int builtIndex, int layer)
        {
            return QCellBase + Mathf.Clamp(builtIndex, 0, QCellMax - 1) * QCellStride + layer;
        }
        //   🔴 **2026-10-04（A74②）更正：`LC_Ring` 原来是 `-1`（= 借 3204）** —— 证据与理由本来写在
        //      上面（「本页带 3200–3209 十个号、一格要 6 层、`L_*` 只有 5 个号 ⇒ 最低那一号借 3204」）。
        //      现在格子有**专属带**（3210 起）⇒ **那个 `-1` 特例整个消失**，3204 也不再被任何件借用
        //      （FX-1 留的告诫「⛔ 别把别的件挪到 -1 上」就此了结 —— **没有 `-1` 了**）。
        //      ⚠️ 这条更正**没有**推翻 FX-1 的任何结论：兄弟序那一套（`bg` 压 `hl`、`Outline` 压 `end`）
        //      与「同类同号」的错，都还在、都还是对的 —— 改的只是**号段**。
        //   ⚠️ `Hit` 仍是 `L_Hit`（页带 3208）**没进格子带** —— 它是**透明命中区**（`α=0`），
        //     队列只决定**点击优先级**、不决定画面；它与别的命中区**在屏幕上不重叠** ⇒ 同号无害
        //     （挪进格子带反而会压过页带里别的命中区，没有任何好处）。

        // ---- 真值 ----
        static readonly PxRect HeaderBtnR = new PxRect(331.17f, 116.65f, 1920.00f, 188.83f);
        static readonly PxRect HeadDivR = new PxRect(330.67f, 185.15f, 1920.50f, 188.83f);
        static readonly PxRect TabsRowR = new PxRect(359.97f, 116.65f, 1417.27f, 185.15f);
        static readonly PxRect InfoBtnR = new PxRect(359.97f, 117.08f, 619.97f, 184.72f);
        static readonly PxRect TrophiesBtnR = new PxRect(632.42f, 117.08f, 892.42f, 184.72f);
        static readonly Vector4 TabBtnBorder = new Vector4(188f, 0f, 99f, 30f);
        static readonly Color TabOnCol = new Color(1f, 0.631f, 0f, 1f), TabOffCol = new Color(1f, 0.544f, 0f, 1f);
        const string ArtTabOn = "40K_tab_button", ArtTabOff = "40K_tab_button_overwindow";

        /// <summary>`GeneralDetails`（= 原版 `AllianceView`）**根节点**的矩形。
        /// 🔴 **出处 = `AllianceMemberVariant>GeneralDetails`**（RT `-4327119531760820061`，act **T**，
        /// 普查 `社交_联盟与好友页.md:264`）—— **正是我们这棵树该用的那一份**
        /// （我们这一支 = `AllianceMemberVariant`，见文件头 ②）。
        /// ⚠️ **另一份**（`AllianceNotMemberVariant>GeneralDetails`，RT `-8680982849342087005`，act **F**，
        /// 同份普查 `:171`）= **332.67,162.04→1919.00,1080.02** —— 与这一份差 **(左 1.5px、上 26.79px)**，
        /// 是「**查看别的盟**」那一态（`AllianceSearchTab.HandleDisplayAlliance` 才把它点亮）用的。
        /// 🔴 **2026-10-04（A29）改**：这里原来取的是 act F 那一份 —— 判据与逐节点清单见
        /// `AllianceGeneralDetails.Build` 的「A29」那一段。
        /// 📌 它同时是那棵树 `GeneralDetails>Content` 的矩形（两份**同一个矩形**，
        /// 逐字段相同 ⇒ 只留一个常量，免得两处各写一遍迟早不一致，CLAUDE.md §三）。</summary>
        public static readonly PxRect GeneralR = new PxRect(331.17f, 188.83f, 1920.00f, 1080.05f);
        public static readonly PxRect TrophiesR = new PxRect(366.97f, 189.04f, 1921.00f, 1079.84f);

        /// <summary>🆕 2026-10-04（A30）：`TrophiesWindow>Scroll Rect` 的矩形 —— 它**同时是视口**。
        /// 出处 = 普查 `社交_联盟与好友页.md:356`（`366.97,378.51→1921.00,1079.84`，组件
        /// `ScrollRect,RectMask2D,Image`）+ 原始 JSON 实读
        /// （`MonoBehaviour_920958765198729379.json` 的 `m_Viewport → 3982199931461492899`
        ///  = **这个节点自己的 RT**）—— 两条独立路对上 ⇒ 这一格**没有**另建 `Viewport` 子节点
        /// （与 `MemberList>Scroll View>Viewport` 那份不同，别照那边抄）。</summary>
        public static readonly PxRect TrophyScrollR = new PxRect(366.97f, 378.51f, 1921.00f, 1079.84f);

        /// <summary>`Scroll Rect>Item Drawer` 那个 `GridLayoutGroup` 的参数 —— 见本文件末尾的
        /// `AllianceTrophyGrid`（放命名空间那一层，自检在**编辑器程序集**里也要调它的 `ColumnsFor`）。</summary>

        static readonly PxRect ChatPreviewR = new PxRect(1479.80f, 109.80f, 1879.80f, 169.80f);

        Transform _general, _trophies;
        ImageQuad _infoBg, _trophiesBg;

        /// <summary>🆕 2026-10-03（A25④）：`MemberList>Scroll View` 那一格的**纵向滚动区**
        /// （全壳唯一一份滚动实现 = `MenuScroll`）。由 `AllianceGeneralDetails.Build` 建好后写进来
        /// （滚动视口归那一层所有 —— `SocialPage.Clip` 也是从那儿设的）。
        /// 🔴 原版档位**先读再定**：`AllianceMemberVariant>GeneralDetails>MemberList>Scroll View` 是
        /// `Image + ScrollRect` `h=0 v=1` · **`m_MovementType=1`(Elastic)** · `m_Inertia=1`
        /// · `m_Elasticity=0.1` · `decel=0.135` · `m_ScrollSensitivity=50`（原始 JSON 实读：
        /// `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-2224558054710517597.json`
        /// 的 `m_MovementType: 1` + `m_Content → -1690316978600091485`）；
        /// 视口身上是 `Image + Mask`（`showGraphic=0`）—— 我们拿 `Viewport` 那个节点自己的矩形当视口。
        /// ⚠️ **2026-10-04 更正 + 记账（A35③ 审查查出）** —— 这一段原来写「它**整套几何**用的是另一份实例
        /// （`AllianceNotMemberVariant>GeneralDetails`…）」：**说重了，实际是【两份混着用】**。
        /// 逐节点核过（判据 = 普查 `社交_联盟与好友页.md:264-330` 那份 act T 子树 vs `:171-240` 那份 act F 子树）：
        /// <list type="bullet">
        /// <item>我们这棵树的**上半段 = `AllianceNotMemberVariant>GeneralDetails`**（RT `-8680982849342087005`，
        /// 原版 **act F**）：根 `332.67,162.04→1919.00,1080.02` · `BadgeDrawer 360.40,170.48→610.95,405.60`
        /// · 盟名 `614.88,177.48→1114.24,248.56` · 两个评级块 y `237.38 / 318.21` · `Config fields
        /// 1079.54,183.02→1862.35,243.02`。</item>
        /// <item>**下半段 = `AllianceMemberVariant>GeneralDetails`**（RT `-4327119531760820061`，act **T**，
        /// 也就是**这棵树该用的那一份**）：`Description input text 1130.44,288.85→1879.21,482.08` ·
        /// `Divisor line members 330.67,489.85→1920.50,493.53` · `MemberList 369.42,446.55→1880.92,1080.05`
        /// · `members label 369.42,447.01→964.25,488.44` · `Scroll View 369.67,493.63→1880.67,1080.05`
        /// —— 这几处**都对**。</item>
        /// </list>
        /// ⇒ 🔴 **这一棵树的几何是「两份实例各取一半」= 真偏离**（act T 那份的上半段整体比 act F **低 26.79px**
        /// （根 `y 162.04 → 188.83`）、**左移 1.5px**（根 `x 332.67 → 331.17`））。
        /// 🔴 **同一处的第二半（更要紧）**：`Config fields` 五个子件的**显隐也是取的 act F 那份**，与 act T **正好相反** ——
        /// act F 里 `extra_info`=T、`LanguagesDropdown`/`Privacy Dropdown`/`Edit`/`Confirm`/`Cancel` 全 **F**
        /// （普查 `:186-220`：`:187` vs `:188,201,214,217,220`）；
        /// act T 里 `extra_info`=**F**、那五个全 **T**（普查 `:279-313`：`:280` vs `:281,294,307,310,313`）。
        /// <para>✅ **2026-10-04（A29）上面两条都已收掉** —— 这棵树现在**整套几何、整套显隐都取 act T 那一份**
        /// （`GeneralR` = 331.17,188.83→1920.00,1080.05；上半段十几处矩形逐个换；`Config fields` 那一行
        /// 换成两个下拉 + 三个钮、去掉 `extra_info`）。逐值判据与出处见 `AllianceGeneralDetails.Build`
        /// 的「A29」那一段。⛔ **A35 那句「本批没有动它」的记账到此作废**（不是推翻，是那条待办做完了）。</para>
        /// <para>✅ **2026-10-04（A55②）第二条记账也收掉了**：那一棵（**未入盟支**
        /// `AllianceNotMemberVariant>GeneralDetails`，RT `-8680982849342087005`，act F）**已经建出来**
        /// —— 挂在 `AlliancesTab.cs` 的 `AllianceSearchTab.BuildGeneralDetails()` 下，几何取 `GeoF`、
        /// 出厂 act F，由 `HandleDisplayAlliance`（「查看别的盟」那一态，
        /// `decomp_full/AllianceSearchTab__HandleDisplayAlliance.c`：把 `join`/`create` 两个菜单关掉、
        /// 把 `allianceView` 点亮再 `AllianceView.Draw(group)`）点亮。
        /// ⛔ 本行原来写的「**一个 `GeneralDetails` 都没有** ⇒ **还欠一棵**」**到此作废**（那条待办做完了）。</para></summary>
        public MenuScroll MemberScroll;

        /// <summary>`GeneralDetails>MemberList>Scroll View>Viewport>Content`（行挂它下面；重建时清它）。</summary>
        Transform _memberContent;

        /// <summary>🆕 2026-10-04（A30）：`TrophiesWindow>Scroll Rect` 那一格的**纵向滚动区**
        /// （与 `MemberScroll` 同一份实现 = `MenuScroll`；`TrophiesWindow` 与 `GeneralDetails` 是一对互斥支）。
        /// 🔴 原版档位**先读再定**（原始 JSON 实读
        /// `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_920958765198729379.json` =
        /// `TrophiesWindow>Scroll Rect` 上的 `ScrollRect`）：`m_Horizontal 0 / m_Vertical 1` ·
        /// **`m_MovementType = 1`（Elastic）** · `m_Inertia 1` · `m_Elasticity 0.1` · `m_DecelerationRate 0.135`
        /// · `m_ScrollSensitivity 50` · `m_Content → 4634450164399136931`（= `Item Drawer`）·
        /// `m_Viewport → 3982199931461492899` = **它自己那个 RT**（⇒ 视口就是 `Scroll Rect` 自己的矩形，
        /// 这一格**没有**另建一个 `Viewport` 子节点 —— 与 `MemberList>Scroll View` 那份不同）。
        /// 身上组件 = `ScrollRect + RectMask2D + Image`（那层 `Image` 是 `&lt;无图> Simple (1,1,1,0)`）。
        /// ⚠️ 与社交另两处不同：**只有它这一格的 `RectMask2D.m_Softness = (0,50)`**
        /// （`_tmp_view/q1_rm2d.txt:45`；`Open Alliances>Viewport` 与 `Friends Container/Viewport` 都是 `(0,0)`）——
        /// 软边怎么接见 `TrophyClipSoftness` 那段注释。</summary>
        public MenuScroll TrophyScroll;

        /// <summary>`TrophiesWindow>Scroll Rect>Item Drawer`（奖杯格挂它下面；重建时清它）。</summary>
        Transform _trophyContent;

        public GameObject GeneralView { get { return _general != null ? _general.gameObject : null; } }
        public GameObject TrophiesView { get { return _trophies != null ? _trophies.gameObject : null; } }

        /// <summary>由 `AllianceGeneralDetails.Build` 建完 `Content` 后登记（重建成员列要清它）。
        /// ⚠️ 只为「清空重填」用 —— **别在外面拿它摆件**（画图一律走本视图那几个助手）。</summary>
        public void SetMemberContent(Transform content) { _memberContent = content; }

        /// <summary>自检用：按当前 `SocialData.Members` 重画成员列（= 原版 `AllianceMemberList` 清空重填那条路）。
        /// **只给自检**（同 `FriendsTab.RebuildForTest` / `LeaderboardWindow.RebuildForTest`）。
        /// 🔴 **2026-10-04（A35⑧）：那一句早退原来是【静默】的**（`if (…) return;`）—— 形状危险：
        /// 真走到那一支就是「点了/滚了，什么都没发生、也什么都没说」（工程红线：不许静默失败）。
        /// 正常路径上到不了（`AllianceGeneralDetails.Build` 建完 `Content` 一定会 `SetMemberContent`），
        /// 所以补的是**出声**而不是改行为。</summary>
        public void RebuildMembersForTest()
        {
            if (_memberContent == null)
            {
                Debug.LogWarning("[Social] `AllianceMemberTab.RebuildMembersForTest`："
                               + "`MemberList>Scroll View>Viewport>Content` 还没登记（`SetMemberContent`）"
                               + "⇒ **成员列这一格不会重画**（画出来的还是上一次那一批行）。");
                return;
            }
            AllianceMemberRow.BuildAll(this, _memberContent, MemberScroll);
        }

        /// <summary>由 `AllianceMemberTab.BuildTrophies` 建完 `Item Drawer` 后登记（重建奖杯格要清它）。
        /// ⚠️ 只为「清空重填」用 —— **别在外面拿它摆件**（同 `SetMemberContent` 那条）。</summary>
        public void SetTrophyContent(Transform content) { _trophyContent = content; }

        /// <summary>🆕 2026-10-04（A30）：按当前 `SocialData.AllianceTrophies` 重画奖杯格
        /// （= 原版 `AllianceTrophiesView` 清空重填 `trophyHolder` 那条路；`trophyEntryPrefab` 逐条 Instantiate）。
        /// **只给自检**（本地奖杯数恒 0 —— 原版那个数是服务器的），滚轮回调也走它。
        /// 形状照 `RebuildMembersForTest`（**没登记就出声**，不静默）。</summary>
        public void RebuildTrophiesForTest()
        {
            if (_trophyContent == null)
            {
                Debug.LogWarning("[Social] `AllianceMemberTab.RebuildTrophiesForTest`："
                               + "`TrophiesWindow>Scroll Rect>Item Drawer` 还没登记（`SetTrophyContent`）"
                               + "⇒ **奖杯格这一格不会重画**（画出来的还是上一次那一批格）。");
                return;
            }
            BuildTrophyRows(_trophyContent);
        }

        public void Build()
        {
            BuildHeader();
            _general = Node(Root, "GeneralDetails", GeneralR);
            // ⚠️ 盟名传 `SocialData.AllianceName`（**当前盟**的名字 —— 这一支就是「我在的盟」那个盟）。
            //    未入盟支那一棵传的是「**正在看的那个盟**」的名字，见 `AlliancesTab.cs` 的 `HandleDisplayAlliance`。
            AllianceGeneralDetails.Build(this, _general, GeneralR, AllianceGeneralDetails.Variant.Member,
                                         SocialData.AllianceName ?? "", out _);
            _trophies = Node(Root, "TrophiesWindow", TrophiesR);
            BuildTrophies(_trophies);
            BuildChatPreview();
            ShowGeneral();       // 出厂：`TrophiesWindow` act F ⇒ 默认 General 那一支
        }

        // ---------------------------------------------------------- 二级页签

        void BuildHeader()
        {
            var head = Node(Root, "Alliance Header Buttons (1)", HeaderBtnR);
            Nine(head, "40k_Separator_Fade_Sides_Horizontal", HeadDivR, new Vector4(63f, 0f, 63f, 0f),
                 "Divisor line", L_Line, new Color(0.875f, 0.552f, 0.286f, 1f));
            var row = Node(head, "Tab buttons", TabsRowR);
            // 🔴 **2026-10-18（第七轮）：两颗页签的字走词条 —— 认过树才接的**（本文件这两颗的节点名
            //   `"Generic Tab UI Button Info"` / `"… Trophies"` 就是 `Toggle` 的第 3 实参）。
            //   配对判据（逐颗按 pid 读原版）：
            //     · `Generic Tab UI Button Info/Button Text` = TMP `General` + `mTerm` **`Settings/General/Title`**
            //       —— 🔴 **原版自己复用了【设置窗】那条通用词条**（表里 `Settings/General/Title` 那条，**本批不新增**）；
            //       它的父链 = `Button Text < Generic Tab UI Button Info < Tab buttons < Alliance Header Buttons (1)
            //       < AllianceMemberVariant < Alliances Tab < …` ⇒ **与我们这一颗逐节同名**；
            //     · `Generic Tab UI Button Trophies/Button Text` = TMP `Trophies` + `mTerm` **`SocialMenu/Alliances/Trophies`**
            //       （父链同上）⇒ 同一条。
            //   ⚠️ 这与 `Alliances invitations:` 那处的差别：那里**同名文案身上的 term 是别的窗的键**且父链对不上；
            //     这里是**父链逐节同名 + 节点名逐字相同** ⇒ 配对成立（不是凭名字像）。
            //   ⛔ 节点名那两个实参（`"Generic Tab UI Button Info"` / `"… Trophies"`）一字未动。
            _infoBg = Toggle(row, InfoBtnR, "Generic Tab UI Button Info", Loc.T("Settings/General/Title"), true);
            _trophiesBg = Toggle(row, TrophiesBtnR, "Generic Tab UI Button Trophies", Loc.T("SocialMenu/Alliances/Trophies"), false);
            Hit(row, "InfoHit", InfoBtnR, L_Hit, ShowGeneral);
            Hit(row, "TrophiesHit", TrophiesBtnR, L_Hit, ShowTrophies);
        }

        // ============================================================================================
        // 🔴 **13 处 `alignLeft` 对齐表**（全文件 **只此一处** —— 铁律 6：真值只写一份）
        // ============================================================================================
        // **判据 = 原版 `m_HorizontalAlignment` 的原文**（`1` = Left · `2` = Center · `4` = Right）。
        // 判据命令（本文件这一族**两个根**，各一条）：
        //   · `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "AllianceMemberVariant" --depth 20 --md`
        //     ⇒ 读那一行的「对齐=」列（例：页签键报 `Center/Midline`）；
        //   · 成员行那一族是**独立根**（`AllianceMemberEntry`）：
        //     `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all --rt 6030421012472178610 --md`。
        // 🔴 **原始 JSON 复核点**（两份实例各一份，⛔ 不是转述 —— 这两条正是「同名不同档」的那一对）：
        //   · `AllianceMemberVariant > {Alliance,Draft} Rating Display > Individual rating value`
        //     = **`Left/Midline`** ⇒ `m_HorizontalAlignment = 1`
        //     （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-69344794122919773.json` /
        //      `…_-7031947786318323549.json`）；
        //   · `Alliance Member Entry > {Draft,Ranked} Rating > Individual rating value`
        //     = **`Right/Midline`** ⇒ `m_HorizontalAlignment = 4`
        //     （`…/MonoBehaviour_-6736837615115902813.json` / `…_5962791513722300338.json`）。
        //   ⇒ 🔴 **同名不同档**：这两族**必须按祖先链各取各的**（铁律 5·c）。
        //     此前两份普查（`资料/普查产出_1011/W5_A307_A255_A258.md:110-111` · `W5b_A317.md` §四·2）
        //     把「`Individual rating value` ×3 全是 `Right`」当成一句话 ⇒ **照它做会把上面那两处
        //     本来对的 `Left` 改成居中**（本件按 `Left` 落的）。
        //
        // | # | 站（节点） | 原版 | 我们这一侧 |
        // |---|---|---|---|
        // | 1 | 二级页签 `Button Text`（`General`/`Trophies`，1 个调用点） | `Center`(2) | **`alignLeft: false`**（原来没传 ⇒ 借缺省 `true` 被 `MenuDraw.AlignLeft` 推到左边缘，**真偏离**）· 标称 `60f` → **`44f`**（A413） |
        // | 2 | `CurrentActiveBadge Name/Text` | `Left`(1) | `alignLeft: true`（显式化，原缺省同值） |
        // | 3 | `CurrentActiveBadge Count/Text` | `Left`(1) | 同上 |
        // | 4 | 聊天行 `Message Preview/text` | `Left`(1) | 同上 |
        // | 5 | `Alliance name text/Text` | `Left`(1) | 同上 |
        // | 6 | `Config fields/extra_info`（act F 那棵） | `Right`(4) | **不动**（本来就对：`alignLeft: false` + `MenuDraw.AlignRight`） |
        // | 7 | `description text/Text` | `Left`(1) | `alignLeft: true`（显式化） |
        // | 8 | `members label/Text` | `Left`(1) | 同上（**另有 A412：这一颗不吃自适应**，见那一行的注释） |
        // | 9 | `{Alliance,Draft} Rating Display/Individual rating value`（2 个调用点） | `Left`(1) | **`false` → `true`**（原来传 `false` = 居中，**真偏离**）· 标称 `45f` → **`42f`**（A413） |
        // | 10 | 成员行 `member index` | `Center`(2) | **`alignLeft: false`**（原来没传 ⇒ 左对齐，**真偏离**） |
        // | 11 | 成员行 `member name` | `Left`(1) | `alignLeft: true`（显式化） |
        // | 12 | 成员行 `member role` | `Left`(1) | 同上 |
        // | 13 | 成员行 `{Draft,Ranked} Rating/Individual rating value`（2 个调用点） | `Right`(4) | `alignLeft: false`（已有）+ **`MenuDraw.AlignRight`**（这个口只有「左/居中」两档 ⇒ 右对齐要在 `Text(...)` 之后另接，先例 = `Shell/ProfileTab.cs` 的 `Consecutive login days`） |
        //
        // ⚠️ **两件事要把话说清**（别把「显式化」读成「改行为」）：
        //   · #2/#3/#4/#5/#7/#8/#11/#12 = **零行为变化**（缺省本来就是 `true`）—— 它们今天**是**对的；
        //     显式写出来的价值是「**钉住这个节点是 `Left`**」：谁把它改成 `false`，渲出来的左缘就离开矩形左缘。
        //   · 🔴 **`wrap` / `alignLeft` 的缺省今天都已经删掉**（两个口的形参必填，见
        //     `Shell/SocialWindow.cs` 的 `SocialPage.Text` / `SocialView.Text`）⇒ 这 13 处**必须逐处显式**，
        //     本表就是「谁该写什么」的唯一真值表。
        // ⚠️ `alignLeft: false` 的语义 = **不调 `MenuDraw.AlignLeft`**（不是「反向对齐」）——
        //   `SocialView.Text` 里那句是 `if (alignLeft) MenuDraw.AlignLeft(lb, r);` ⇒ 传 `false` 时
        //   没有任何对齐动作，字停在 TMP 自己那一档（原版 `m_HorizontalAlignment = 2` 的两处
        //   —— #1 / #10 —— 要的正是这个）。
        // ⚠️ **两条账不在本表里**（另有出处，别混进来）：`SocialView.Text` 的 `wrap` 真值 ⇒
        //   `Shell/SocialWindow.cs` 的 `SocialPage.Text` 头（A213 表）· 标称字号那一族 ⇒ A413 / A406。
        // ============================================================================================

        ImageQuad Toggle(Transform parent, PxRect r, string name, string text, bool on)
        {
            var n = Node(parent, name, r);
            var q = Rect(n, on ? ArtTabOn : ArtTabOff, r, "Image", L_Btn, on ? TabOnCol : TabOffCol);
            // 🔴 **A319 #1**：原版 `Generic Tab UI Button {Info,Trophies}/Button Text` = **`Center`(2)**
            //   （`menu_dump … "AllianceMemberVariant"` 那份的「对齐=」列报 `Center/Midline`）。
            //   原来没传 `alignLeft` ⇒ 借缺省 `true` 被 `MenuDraw.AlignLeft` 推到左边缘（**真偏离**）。
            // 🔴 **2026-10-12（A413）：标称字号 `60f` → `44f`** —— 原版 `m_fontSize = **44.0**`
            //   （同一行实读：`字号=44.0 基准=12.0 auto[12.0~44.0]`；`Info`/`Trophies` 两颗**逐颗**读过）。
            //   ⚠️ **同族陷阱**：`Alliance Header Buttons/Tab buttons/Generic Tab UI Button {Search,Create}/Button Text`
            //   **才是 60**（`字号=60.0 auto[12.0~60.0]`）—— 同一个 `Tab buttons` 家族**两套值**，
            //   ⛔ 别按名字猜（铁律 5·c）。📌 `F2_字号线收尾.md` §五·5 把那一对写成 `{Join,Create}`
            //   **是错的**：这两棵树里**没有 `Join`**（本处只管 `{Info,Trophies}` 这一对）。
            //   验收：`44 > autoMinPx 12` ⇒ `MenuDraw.TextBox` 那条守卫**照旧为真**、自适应跑的档不受影响
            //   （改的只是「自适应不跑」那一档会现形的声明值）。
            var tabBtnR = new PxRect(r.x1 + 9.66f, r.y1 + 4.66f, r.x2 - 9.66f, r.y2 + 4.66f);
            var lbTab = Text(n, tabBtnR, text, Color.white,
                 "Button Text", 44f, L_Text, 12f, wrap: false, alignLeft: false);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版
            //   `Alliance Header Buttons (1)/Tab buttons/Generic Tab UI Button {Info,Trophies}/Button Text`
            //   = `对齐=Center/Midline` ⇒ `m_VerticalAlignment = 4096 (Midline)`
            //   （判据 = `python 工具/menu_dump.py bundle_menus_assets_all "AllianceMemberVariant" --depth 20 --md`。
            //    ⚠️ **两颗同档**：同一族的 `{Search,Create}` 那对在 `AlliancesTab`，那边也是 `Midline`）。
            MenuDraw.SetVAlign(lbTab, Label.VAlign.Midline, tabBtnR);
            // 🔴 **2026-10-08（波 C3 · A213 的 17 处收尾）：本文件有 6 处原版 `m_TextWrappingMode = 0`**
            //   （`SocialPage.Text` / `SocialView.Text` 的 `wrap` 缺省是 `true` = `Normal`，而
            //    `SetAutoFitBox` → `SetWrapWidth` 会**无条件**把它开成 `Normal`）——
            //    真值逐条现读的判据只写一处：`Shell/SocialWindow.cs` 的 `SocialPage.Text` 文档头
            //    （§A213 那张表在 `资料/普查产出_1008/波C3_A212其余_A213_A214.md`）。本文件这 6 处是：
            //      · 本行（页签键 `Button Text`，原版 `Generic Tab UI Button {General,Trophies}/Button Text` = **0**）
            //      · `:345` 关注徽标 `CurrentActiveBadge Name > Text`（`Featured: Trophy Name`）= **0**
            //      · `:349` 同族 `CurrentActiveBadge Count > Text`（`45 Trophies Achieved!`）= **0**
            //      · `:711` 聊天行 `MsgRow > text`（`…/Message Preview > text`，fs23）= **0**
            //      · `:877` 概览 `GeneralDetails/Content/Alliance name text > Text` = **0**
            //      · `:1182` 名次 `member index > Text` = **0**
            //   ⛔ 其余 7 处（`:899`/`:944`/`:952`/`:1029`/`:1200`/`:1202`/`:1220`）原版都是 **1**
            //   ⇒ **不许一刀切**把它们也关掉（那 7 处改了就是**新的**偏离）。
            //   🔴 **2026-10-11（A258）**：上面那 6 处（已显式传 `false`）**一个字没动**，而**那 7 处现在各自显式传
            //   `wrap: true`**（与缺省值逐字等价）。⚠️ **缺省值本批没删成**（阻塞点 = `Editor/MainMenuScene.cs:4675`
            //   一条 7 实参的 `Clip` 探针，那文件不在白名单）⇒ 判据与最小改法只写在
            //   `Shell/SocialWindow.cs` 的 `SocialPage.Text` 文档头，**别在这儿抄第二份**。
            //   ⛔ **行号那一串会漂** —— 认节点名，别认行号。
            return q;
        }

        public void ShowGeneral()
        {
            if (_general != null) _general.gameObject.SetActive(true);
            if (_trophies != null) _trophies.gameObject.SetActive(false);
            SwapTabArt(false);
        }

        public void ShowTrophies()
        {
            if (_general != null) _general.gameObject.SetActive(false);
            if (_trophies != null) _trophies.gameObject.SetActive(true);
            SwapTabArt(true);
        }

        void SwapTabArt(bool trophiesOn)
        {
            var onTex = Page.ArtOf(ArtTabOn);
            if (_infoBg != null && onTex != null)
            {
                _infoBg.SetTexture(onTex);
                _infoBg.SetAspect(InfoBtnR.W / InfoBtnR.H);
                _infoBg.SetTint(trophiesOn ? TabOffCol : TabOnCol);
            }
            if (_trophiesBg != null && onTex != null)
            {
                _trophiesBg.SetTexture(onTex);
                _trophiesBg.SetAspect(TrophiesBtnR.W / TrophiesBtnR.H);
                _trophiesBg.SetTint(trophiesOn ? TabOnCol : TabOffCol);
            }
        }

        // ---------------------------------------------------------- `TrophiesWindow`（`AllianceTrophiesView`）

        /// <summary>联盟奖杯页。⚠️ **两个标题用的就是 prefab 里的字面样例串**
        /// （`Featured: Trophy Name` / `45 Trophies Achieved!` —— 都是资产里的真字符串，不是我们编的）；
        /// 奖杯行是 `TrophyDisplay`（`Item Drawer` 里的一个，运行期按数量生成）。
        /// 🔴 这一支**本地走不到**（见文件头 ①）⇒ 我们只把**看得见的骨架**照建。</summary>
        void BuildTrophies(Transform root)
        {
            var nm = Node(root, "CurrentActiveBadge Name",
                          new PxRect(598.41f, 217.39f, 1493.50f, 267.22f));
            Text(nm, new PxRect(598.41f, 217.39f, 1493.50f, 267.22f), "Featured: Trophy Name",
                 Color.white, "Text", 50f, L_Text, 12f, wrap: false, alignLeft: true);   // A319 #2（原版 `Left`(1)）· 原版 `折行=0`（判据见 `Toggle` 那段）
            var cnt = Node(root, "CurrentActiveBadge Count",
                           new PxRect(597.94f, 267.62f, 1504.44f, 318.73f));
            Text(cnt, new PxRect(597.94f, 267.62f, 1504.44f, 318.73f), "45 Trophies Achieved!",
                 Color.white, "Text", 40f, L_Text, 12f, wrap: false, alignLeft: true);   // A319 #3（原版 `Left`(1)）· 原版 `折行=0`（同上）
            // `CurrentActiveBadge`：那一枚大徽标（`AllianceBadgeDrawer`）—— 徽标图在**服务器**上（盟自己没有存档）
            var badge = Node(root, "CurrentActiveBadge", new PxRect(414.00f, 204.89f, 579.95f, 360.62f));
            Node(badge, "Frame", new PxRect(414.00f, 204.89f, 579.95f, 360.62f));
            Nine(root, "40k_Separator_Fade_Sides_Horizontal", new PxRect(331.07f, 376.71f, 1920.10f, 380.39f),
                 new Vector4(63f, 0f, 63f, 0f), "Divisor line Trophies", L_Line,
                 new Color(0.875f, 0.552f, 0.286f, 1f));
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：这颗 `Scroll Rect` 就是**裁切状态的载体**。
            //    ⚠️ **本页没有 `Viewport` 节点**（原版树是 `TrophiesWindow/Scroll Rect/{Viewport}/Item Drawer`，
            //    我们少了中间那层）⇒ 同 `AvatarTab`/`TitleTab`：`ViewportClip` **直接挂在现成的
            //    `Scroll Rect` 上**（它的 rect 就是 `TrophyScrollR` = 滚动区的视口矩形，零结构改动，
            //    `FindAbove` 从 `Item Drawer` 走一级就命中）。
            //    参数：`padding = (0,0,0,0)` · `softness = TrophyClipSoftness = (0,50)`
            //    （原版 `TrophiesWindow>Scroll Rect` 那个 `RectMask2D` 实读）。
            var scroll = ViewportClip.Hang(root, "Scroll Rect", TrophyScrollR, Vector4.zero, TrophyClipSoftness).transform;
            // 🆕 **2026-10-04（A30）：这一格补上纵向滚动区 + 裁切**（此前是**一个空节点** ⇒ 奖杯一多就
            //   **画到框外**：原版那层 `RectMask2D` 没人等效）。做法与社交另两处
            //   （`Open Alliances` / `Friends Container`）和成员列**同一套**（同一份 `MenuScroll`）：
            //   **先有滚动区、再谈裁切** —— 只补裁切会把后面的格**藏掉**而不是可滚。
            //   🔴 **2026-10-13（A435 阶段 2 · 丙）**：裁切状态已迁到上面 `ViewportClip.Hang` 那颗节点上。
            //   ⚠️ 档位 = 原版 `m_MovementType = 1`（**Elastic**）—— ⛔ 别套 `BattleLogPopup` 那一档。
            //   ⚠️ `Owner` 取 `TrophiesWindow` **这一棵**（点 `General` 键切回来时它是关的 ⇒
            //     这一格必须一起失去滚轮命中；`HitScroll` 判的就是 `Owner.activeInHierarchy`）。
            var sc = MenuScroll.TopAligned(TrophyScrollR, 0f);     // 内容高在 `BuildTrophyRows` 里按格数写
            sc.Owner = root.gameObject;
            sc.Elastic = true;
            sc.OnChanged = RebuildTrophiesForTest;                 // 滚轮只改 `Offset`、**画是调用方的事**
            SocialPage.RegisterScroll(sc);                         // 指针层要认识它，滚轮才落得到这一格上
            TrophyScroll = sc;

            // 内容容器（原版 `Item Drawer` = 那个带 `GridLayoutGroup` + `ContentSizeFitter(VerticalFit=1 MinSize)`
            // 的节点）：出厂高 **377**（= 通式在 1 排的取值，两条路对上，见 `AllianceTrophyGrid.H1Row`）
            var drawer = Node(scroll, "Item Drawer", new PxRect(TrophyScrollR.x1, TrophyScrollR.y1,
                                                               TrophyScrollR.x2, TrophyScrollR.y1 + AllianceTrophyGrid.H1Row));
            SetTrophyContent(drawer);      // 🔴 A435·丙起：裁切在**上一行那颗 `Scroll Rect` 的 `ViewportClip`** 上
            BuildTrophyRows(drawer);
        }

        /// <summary>奖杯格（原版 `TrophyDisplay`）—— 按 `Scroll Rect>Item Drawer` 那个 `GridLayoutGroup` 逐格摆。
        /// 🔴 **格位要自己推**（`menu_dump` 的布局算法只做 V/H、不做 grid，普查 §C·3）——判据 = UGUI
        /// `GridLayoutGroup.cs:184/188`，参数逐值实读原始 JSON（见本文件末尾 `AllianceTrophyGrid` 那段注释）。
        /// 视口宽 1554.03 ⇒ **5 列**（`376.97,401.51` 那一格 = 视口左上 + (10, 23) 可反证）。
        /// 🆕 2026-10-04（A30）：格按**滚动偏移之后**的位置摆（`MenuScroll.Shift`）、
        /// 整格滚出视口的**不建**；裁切（框 + `(0,50)` 软边）长在 `Scroll Rect` 那颗 `ViewportClip` 上
        /// （A435·丙 起 —— 原来那句「画之前 `SetClip(视口)`、画完清掉」已作废）。</summary>
        void BuildTrophyRows(Transform drawer)
        {
            for (int i = drawer.childCount - 1; i >= 0; i--) SocialWindow.DestroySafe(drawer.GetChild(i).gameObject);
            int n = SocialData.AllianceTrophies;
            var sc = TrophyScroll;
            // 🔴 **裁切/摆位只有一个来源 = 滚动区的 `Viewport`**（同 `FriendsTab.BuildRows` 那条收口：
            //    没有滚动区时才退回常量 —— 两处取不同矩形 = 迟早不一致）。
            var vpR = sc != null ? sc.Viewport : TrophyScrollR;
            int cols = AllianceTrophyGrid.ColumnsFor(vpR.W);
            int rows = Mathf.CeilToInt(n / (float)cols);

            // 内容高 = UGUI `GridLayoutGroup.cs:188` 的 MinSize：
            //   `padding.vertical + (cell.y + spacing.y) × 排数 − spacing.y`
            //   （原版那个 `ContentSizeFitter` 的 `m_VerticalFit = 1 (MinSize)` —— 原始 JSON 实读
            //    `MonoBehaviour_-909385972349017949.json` 挂在 `Item Drawer` GO 上）
            //   ⇒ 1 排 = 377（= 原版出厂那个 `sizeDelta.y`）、0 排 = 8。
            //   ⚠️ 空表那一支**照样要写**（不写 ⇒ 上一次的内容高留在区里 = 静默的脏值）。
            float h = AllianceTrophyGrid.PadT + AllianceTrophyGrid.PadB + rows * AllianceTrophyGrid.CellH + (rows - 1) * AllianceTrophyGrid.GapY;
            if (sc != null) sc.ContentX2 = vpR.y1 + h;
            // 🔴 **先把 `Item Drawer` 摆到位、再建格** —— 格是按「绝对画布坐标」算 `localPosition` 的
            //    （`MenuDraw.Local` = 绝对中心 − **父节点世界位置**）⇒ 建完再挪父节点，整排会跟着偏
            //    （同 `FriendsTab.BuildRows` 那条实测教训）。
            drawer.localPosition = MenuDraw.Local(drawer.parent, vpR.x1, vpR.y1, vpR.x2, vpR.y1 + h);

            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来这里是 `_trophyClip = vpR; SetClip(vpR);` 那一对
            //    —— **删掉了**。裁切状态（框 + `(0,50)` 软边）长在 `BuildTrophy` 建的
            //    `Scroll Rect` 那颗 `ViewportClip` 上：三条 `*Soft` 助手与 `Page.*` 那条路都沿父链取它。
            //    ⛔ 留着 = 形参永远非空 ⇒ `Resolve` 走第 1 支 ⇒ 节点一个像素都不生效（静默）。
            // 🔴 **2026-10-04（A74②）**：`built` = **建出来的第几格**（⛔ 不是 `n` 里的下标 `i`）——
            //   它是格子队列带的编号来源（见本类顶部那段），因为**视口外的格不建**、下标会跳号。
            int built = 0;
            for (int i = 0; i < n; i++)
            {
                int col = i % cols, row = i / cols;
                float x = vpR.x1 + AllianceTrophyGrid.PadL + col * (AllianceTrophyGrid.CellW + AllianceTrophyGrid.GapX);
                float y = vpR.y1 + AllianceTrophyGrid.PadT + row * (AllianceTrophyGrid.CellH + AllianceTrophyGrid.GapY);
                var r = new PxRect(x, y, x + AllianceTrophyGrid.CellW, y + AllianceTrophyGrid.CellH);
                if (sc != null)
                {
                    r = sc.Shift(r);                                  // 内容坐标 → 屏幕坐标（**只偏移、不裁**）
                    // 整格滚出视口 ⇒ **连节点一起不建**（省 quad，顺带它的点击区也不存在）。
                    // 🔴 求交那一份 = `MenuDraw.Visible`（**全工程唯一一份**求交；`ClipRect` 是它「顺带夹出
                    //    可见矩形」的那版，别在这儿再写一遍 `Max/Min`）。⚠️ 2026-10-07 更正（铁律 5 / A12①）：
                    //    原文写「= `MenuDraw.ClipRect`（全工程唯一一份）」—— 收口后那两句是**同一份**。
                    // 🔴 **2026-10-13（A776 · δ 族）**：形参由**自持的 `vpR`** 换成 **`null`（走父链节点）** ——
                    //    原来这是**第二状态源**：节点搬了粗筛还停在旧矩形（白建 / 漏建几块，静默）。
                    //    **逐处算过的等价推算（本处不是「一刀切」）**：
                    //      · 节点框 = `ClipPx`（世界坐标反推，A497 实测残差 **−6.1e-5 px**）；
                    //      · 节点 `padding = Vector4.zero` ⇒ `RenderClip == ClipPx`（`PaddedClip` 首句早退）；
                    //      · 而节点与 `vpR` **是同一条来源**：`ViewportClip.Hang(root, "Scroll Rect",
                    //        TrophyScrollR, …)` 与 `MenuScroll.TopAligned(TrophyScrollR, 0f)` ⇒ `sc.Viewport ==
                    //        TrophyScrollR` ⇒ 两者差**只有那趟 float32 往返**（~1e-4 px 量级）
                    //        ⇒ **远小于 0.05px 容差**（`NearPx` 那一档）⇒ 逐位等价。
                    //      · ⚠️ 自检宿主都先把 `cam.aspect` 钉成 `LayoutSpace.DesignAspect` 才建树
                    //        （`Editor/*.cs` 那句「批处理默认 4:3」）⇒ 那趟往返在自检里就是纯浮点误差。
                    //    ⛔ 别再退回裸 `ClipRect(r, vpR, …)`（A776 就是把它当缺陷记的）。
                    if (!MenuDraw.VisibleAbove(drawer, r, null)) continue;
                }
                // 🔴 **2026-10-04（A74②）：格数上限出声**（`QCellMax` = 那一带装得下的格数）。
                //   ⛔ 不许静默越界 —— 越过去就画到聊天窗（3300–3308）那一档上了，而画面上看不出来。
                //   ⚠️ 只在**第一次**越界时喊一声（`==` 而不是 `>=`）：连喊 N 条同一句话就是噪声。
                if (built == QCellMax)
                    Debug.LogError($"[Social] 奖杯格一次建了 **超过 {QCellMax} 格**，超出队列带 `{QCellBase}–"
                                 + $"{QCellBase + QCellMax * QCellStride - 1}` 的容量（`QCellMax = {QCellMax}` 格）"
                                 + " —— 多出来的格子**会与别的窗的层带重叠**（下一个是聊天窗 3300）。"
                                 + "要么改小格子/视口，要么给这一带扩容。");
                BuildTrophyCell(drawer, r, built);
                built++;
            }
        }

        /// <summary>一格 `TrophyDisplay`（原版那棵树，逐值见普查 `:358-372`）。
        /// <para>🔴 **2026-10-04（A30）订正两处几何**：① 格内偏移原来写的是**相对视口左上**的数
        /// （`Progress` 写 `x+35`/`x+283`，而那是 `401.97−366.97`）—— 单行硬摆时看不出来，
        /// 换到网格后**每一格都偏 10px** ⇒ 现在一律**相对格子自己**；② `Background`（进度条底）
        /// 原来拿的是 `ProgressBar`（Slider）那个矩形，现在是普查里 `Background` 自己的
        /// `400.72,693.93→649.97,722.46` + 它自己的染色 `(0.299,0.289,0.689,1)`。</para>
        /// <para>✅ **2026-10-04（A55③）把「本体」补齐**（A30 只做了滚动区）：`bg` · `BadgeDrawer` ·
        /// `title` · `Fill`/`end`/`Outline` · `Collectable Highlight`(act F) · 「点格子开
        /// `trophyInfoPopup`」那条命中路 —— 逐件几何/染色/九宫/`ppuMul` 见下面每行注释（出处 = 普查 `:359-372`）。
        /// 五张图**都已在 `Resources/Art/`**：`UI_Deck_Selection_Back_simple`（`ui_deck/`）·
        /// `40k_campaign_bar_{bg,fill,end,outline}` · `OctagonUI_Border_SDF`（`ui_menu/`）。</para>
        /// <para>🔴 **2026-10-04（FX-1）层序改成「照原版兄弟序」**（原来按「种类」派队列 ⇒ 两处**正好反的**）：
        /// 本格那一段带里的号（`CellQueue(builtIndex, LC_*)` —— 定义与逐条判据见本类顶部 ——
        /// 判据 = 原始 JSON 的 `m_Children`：`TrophyDisplay` = `[Collectable Highlight, bg, …]` ·
        /// `ProgressBar` = `[Background, Outline, counter]` 而 `end` 是 `Fill` 唯一的子）。
        /// 修的两处：**`bg` 压 `Collectable Highlight`**（原来 `hl` 在 `bg` 上面）·
        /// **`Outline` 压 `end`**（原来 `end` 在 `Outline` 上面）。</para>
        /// <para>🔴 **两处已知偏离，如实记着**（都不是「猜的」，是**没有那套机制**）：
        /// ① 原版 `Background` / `Fill` / `Outline` 三个 `Image` 上都挂着 **`Mask`（`showGraphic=1`）**
        ///    —— 出厂进度 0 ⇒ `Fill` 宽 **0** ⇒ 它的 `Mask` 把子件 `end` 整个裁掉。**我们没有掩码体系**
        ///    （同 `ProfileTab` 文件头那条「只建节点、不做真裁切」）⇒ 这一格会把 `end` 端帽画出来。
        ///    ⚠️ 本地奖杯数恒 0 ⇒ 一格都不建，**看不到**；哪天有真进度数据、`Fill` 撑开了，这两者才一致。
        ///    📌 **2026-10-04（FX-1）**：`end` 与 `Outline` 重叠的那 ≈5.7px 现在由 `Outline` 压住
        ///    （队列按原版兄弟序，见上面那段）—— 单看这一处，明暗关系已经与原版一致。
        /// ② `title` / `counter` 的**字**：奖杯名与进度在服务器 ⇒ 我们按「零值」摆（`""` / `0/0`），
        ///    不拿 prefab 里的样例串（`Trophy Name ` / `100/200`）—— 一格一个名字会被当成真数据
        ///    （口径 = `SocialData` 文件头「别为了好看塞假数据」）。**节点、字号、对齐照建**。</para></summary>
        /// <para>🔴 **2026-10-04（A74②）：队列号按【格号】错开** —— 本格用自己那一段带
        /// `CellQueue(builtIndex, LC_*)`（见本类顶部那一段），⛔ 不再是「同类同号」。
        /// `builtIndex` = **建出来的第几格**（不是 `n` 里的下标 —— 视口外的格不建，下标会跳号）。</para>
        void BuildTrophyCell(Transform drawer, PxRect r, int builtIndex)
        {
            var cell = Node(drawer, "TrophyDisplay", r);
            // 🔴 本格那一段带的号是**绝对**的（3210 起），而本类那三个助手一律算 `Page.Q + QOff + qOff`
            //    （= 3205 + qOff）⇒ 这里把绝对号减回**页内偏移**。恒等式：
            //    `3205 + (CellQueue(i, L) − 3205) == CellQueue(i, L)`。
            int CellQ(int layer) { return CellQueue(builtIndex, layer) - (Page.Q + QOff); }

            // `Collectable Highlight`（八角描边 · 九宫 52 · `fillCenter=0` · 染色 `(1,0.545,0,1)` · ppuMul 0.94）
            //   —— **出厂 act F**：原版 `AllianceTrophyEntry.Initialize` 第一句就是
            //   `highlight.SetActive(false)`（反编译 `AllianceTrophyEntry__Initialize.c:19`，字段 0x48）。
            //   🔴 **点亮它的判据是 `IsFeatured`**（2026-10-04 FX-1 订正 —— 原来写的是「可收集/达成」）：
            //   `AllianceTrophiesView.Draw` **只对 `AllianceTrophy.IsFeatured` 为真的那一枚**调
            //   `ToggleSelected`（`AllianceTrophiesView__Draw.c:192-196`：
            //   `cVar3 = AllianceTrophy__get_IsFeatured(uVar8,0); if (cVar3 != '\0') { …ToggleSelected… }`），
            //   而 `ToggleSelected` 才是 `SetActive(entry+0x48, 1)` 那一处（`…__ToggleSelected.c`）
            //   —— `Select`/`Deselect` 全仓**没有调用点**。我们这边奖杯状态在服务器 ⇒ 照出厂态
            //   **建了但关着**（节点在 ⇒ 那一态可切；⛔ 别当成「装饰件不建」—— 它是有状态的那一件）。
            //   矩形比格子大（`(0,0)→(1,1)` + sizeDelta 62.46×72.74 ⇒ 四周各溢出一圈）。
            //   🔴 **队列 = `CellQ(LC_Ring)`（本格带的最底那一号，3210 + 6×格号）**：原版 `TrophyDisplay`
            //   的 `m_Children` 里它是**第一颗**子件 ⇒ 画在 `bg` **下面**（露出来的只有格子边缘外那一圈）。
            //   ⚠️ 改之前它是 `L_Art`(2) > `bg`(0) —— **正好反的**，点亮时整圈会盖在格子内容上。
            //   🔴 **2026-10-04（A74②）**：它同时靠「格号越大带越高」压过**左邻/上邻**的整棵子树
            //   （原版是后画的整格在上）—— 这是本批改的那一条，判据见本类顶部那段。
            var hl = NineSoft(cell, "OctagonUI_Border_SDF",
                              new PxRect(r.x1 - 31.07f, r.y1 - 41.38f, r.x1 + 329.39f, r.y1 + 385.36f),
                              new Vector4(52f, 52f, 52f, 52f), "Collectable Highlight", CellQ(LC_Ring),
                              new Color(1f, 0.545f, 0f, 1f), false,
                              new Vector4(52f / 0.94f, 52f / 0.94f, 52f / 0.94f, 52f / 0.94f));
            if (hl != null) hl.SetActive(false);

            // `bg`（格子底板）：`UI_Deck_Selection_Back_simple` 440×656 · 九宫 (197,0,199,0) · Sliced
            //   ⚠️ 无 `ppuMul`（=1）；⚠️ 九宫左右角块 **197+199 = 396 > 格宽 298** ⇒ 按轴等比压扁
            //   （原版 `Image.GetAdjustedBorders` 也是这么退化的，见 `ImageQuad.CreateNineSlice`）。
            //   原版这颗身上是 `EverguildButton trans=0 (None)` ⇒ **没有悬停/按下换图**（不绑 `WindowButton.Bind`）。
            //   🔴 **队列 = `CellQ(LC_Back)`**：它是 `TrophyDisplay` 的**第二颗**子件 ⇒ **压**
            //   `Collectable Highlight`、被 `title`/`Progress` 压（逐条见上面那一组常量），
            //   同时**压过左邻/上邻的整棵子树**、被右邻/下邻的整棵子树压（A74②）。
            NineSoft(cell, "UI_Deck_Selection_Back_simple", r, new Vector4(197f, 0f, 199f, 0f), "bg", CellQ(LC_Back));

            // `BadgeDrawer`（那一枚徽标：Frame + Badge）—— 徽标图的来源在服务器（盟自己的存档）
            var bdr = new PxRect(r.x1 + 33.98f, r.y1 + 6.98f, r.x1 + 264.03f, r.y1 + 237.03f);
            var bd = Node(cell, "BadgeDrawer", bdr);
            Node(bd, "Frame", bdr);
            Node(bd, "Badge", bdr);

            // `title`（奖杯名）—— 原版 `Center/Middle` · 字号 38 auto[12,38] · 折行 1（普查 `:364`）
            //   🔴 队列 = `CellQ(LC_Text)` —— **这一处是「近似」，如实记着**：原版 `title` 是 `TrophyDisplay`
            //   的**第四颗**子件、`Progress` 是第五颗 ⇒ 原版**进度条整叠压在 `title` 上面**；要严格照兄弟序排
            //   还差**一个号**（严格版要 7 号/格，而带子只有 90 号、格数上限 15 ⇒ `7×15 = 105` 装不下）。
            //   两者**不重叠**（`title` 底边 = 格 +277.83 < 进度条顶边 = 格 +282.90）⇒ **画面上分不出来**；
            //   这里按「字压一切」摆，与 `bg` 的那层关系（字在底板之上，两者确实重叠）是对的 ——
            //   而且万一将来两者真重叠了，「字在上」也不会让字被压掉。
            //   ⛔ 真要做严格版：先给这一带扩容到 7 号/格（`QCellStride` 与 `QCellMax` 一起改），
            //    **别**借用旁边的号（FX-1 那条告诫原样成立）。
            TextSoft(cell, new PxRect(r.x1 + 23.94f, r.y1 + 237.03f, r.x1 + 273.00f, r.y1 + 277.83f),
                     "", Color.white, "title", 38f, CellQ(LC_Text), 12f, false);

            // `Progress` > `ProgressBar`（原版是 `Slider` + `ProgressBar` 组件；出厂 `value` = 0）
            var prog = Node(cell, "Progress",
                            new PxRect(r.x1 + 25.00f, r.y1 + 299.07f, r.x1 + 273.00f, r.y1 + 324.54f));
            var bar = Node(prog, "ProgressBar",
                           new PxRect(r.x1 + 23.75f, r.y1 + 282.90f, r.x1 + 273.00f, r.y1 + 330.46f));

            // `Background`（槽底）：`40k_campaign_bar_bg` 42×18 · 九宫 (20,0,20,0) · ppuMul 0.9
            //   🔴 队列 = `CellQ(LC_Slot)`：`ProgressBar` 的**第一颗**子件 ⇒ 压 `bg`、被 `Outline`/`counter` 压。
            var bgq = NineSoft(bar, "40k_campaign_bar_bg",
                               new PxRect(r.x1 + 23.75f, r.y1 + 292.42f, r.x1 + 273.00f, r.y1 + 320.95f),
                               new Vector4(20f, 0f, 20f, 0f), "Background", CellQ(LC_Slot),
                               new Color(0.299f, 0.289f, 0.689f, 1f), true,
                               new Vector4(20f / 0.9f, 0f, 20f / 0.9f, 0f));
            var bgT = bgq != null ? bgq.transform : bar;      // 取不到图时退回挂在 `ProgressBar` 上（不静默丢节点）

            // `Fill Area` > `Fill`（原版出厂宽 **0** —— `Slider.value` 系列化就是 0）
            //   > `end`（端帽：`40k_campaign_bar_end` 22×18 · Simple · preserveAspect · 染色 α **0.698**）
            var fa = Node(bgT, "Fill Area",
                          new PxRect(r.x1 + 23.75f, r.y1 + 294.84f, r.x1 + 273.00f, r.y1 + 318.53f));
            var fillR = new PxRect(r.x1 + 23.75f, r.y1 + 294.84f, r.x1 + 23.75f, r.y1 + 318.53f);
            var fillGo = NineSoft(fa, "40k_campaign_bar_fill", fillR,
                                  new Vector4(10f, 0f, 10f, 0f), "Fill", CellQ(LC_Slot),
                                  new Color(1f, 0.509f, 0f, 1f), true,
                                  new Vector4(10f / 0.9f, 0f, 10f / 0.9f, 0f));
            // 🔴 **零宽那一支**：`MenuDraw.ClipRect:75`（有裁切时）对 `W ≤ 0.01` 的矩形判「不建」
            //   ⇒ `NineSoft` 会返回 null。**但节点必须在**（原版树里 `Fill` 就是这一层、`end` 挂在它下面）
            //   ⇒ 退回一个**只占位、不画图**的节点（`Fill` 的**零宽**是原版出厂的**真值**，不是我们省的）。
            //   ⚠️ **队列**：它今天**没有 quad**（`fillR` 是写死的零宽）⇒ 取哪一档都看不出来，这里与槽底
            //   同号（`LC_Slot`）。哪天真做进度条（给 `fillR` 一个宽度）就必须把它**单开一号** ——
            //   位置在 `LC_Slot` 与 `LC_End` 之间（原版兄弟序：槽底 < `Fill` < `end`）。
            //   ⚠️ **A74② 之后这个「单开一号」是字面可行的**（格子已经有 6 号一段的带）—— 但那要
            //   `QCellStride` 变 7、而 90 号带子装不下 15 格×7（见本类顶部那段）⇒ 真做进度条时要**先扩容**。
            if (fillGo == null) fillGo = Node(fa, "Fill", fillR).gameObject;
            var fillT = fillGo.transform;
            // `end` 的队列 = `CellQ(LC_End)`：它在 `Fill` 里 ⇒ 压槽底、被 `Outline` 压（原版兄弟序）。
            RectSoft(fillT, "40k_campaign_bar_end",
                     new PxRect(r.x1 + 0.01f, r.y1 + 291.75f, r.x1 + 29.45f, r.y1 + 323.61f),
                     "end", CellQ(LC_End), new Color(1f, 1f, 1f, 0.698f), true);

            // `Outline`（那一圈描边）：`40k_campaign_bar_outline` 46×22 · 九宫 (20,0,20,0) · ppuMul 0.9
            //   🔴 队列 = `CellQ(LC_Frame)`：`ProgressBar` 的**第二颗**子件 ⇒ **压** `Background` 整棵子树
            //   （含 `Fill>end`，两者重叠 ≈ 5.7px）。改之前它是 `L_Panel`(0) < `end` 的 `L_Art`(2) —— **反的**。
            NineSoft(bar, "40k_campaign_bar_outline",
                     new PxRect(r.x1 + 23.75f, r.y1 + 292.42f, r.x1 + 273.00f, r.y1 + 320.95f),
                     new Vector4(20f, 0f, 20f, 0f), "Outline", CellQ(LC_Frame),
                     new Color(1f, 0.841f, 0f, 1f), true,
                     new Vector4(20f / 0.9f, 0f, 20f / 0.9f, 0f));

            // `counter`（`100/200` 那一串）—— ⚠️ 它挂在 **`ProgressBar`** 下（普查 `:372` 的缩进是 11 级、
            //   与 `Background`/`Outline` 同层）；A30 那版借挂在 `Progress` 上了，本批订正。
            //   原版 `Center/Middle` · 字号 26.95 auto[12,35] · 折行 1。
            //   🔴 队列 = `CellQ(LC_Text)`：`ProgressBar` 的**第三颗**子件 ⇒ 连 `Outline` 也压在它下面。
            TextSoft(bar, new PxRect(r.x1 + 37.59f, r.y1 + 294.87f, r.x1 + 261.61f, r.y1 + 320.43f),
                     "0/0", Color.white, "counter", 26.95f, CellQ(LC_Text), 12f, false);

            // 「点格子开 `trophyInfoPopup`」那条命中路（原版 `AllianceTrophyEntry.backgroundButton` →
            //   `HandleClick` 发 `OnClick` → `AllianceTrophiesView.HandleTrophyClick` →
            //   `WindowsManager.OpenWindow(trophyInfoPopup, 那一枚奖杯)`；
            //   反编译 `AllianceTrophiesView__HandleTrophyClick.c` / `AllianceTrophyEntry__Initialize.c`）。
            //   🆕 **2026-10-04（A70）：那个弹窗建出来了** —— `Shell/TrophyInfoPopup.cs`（照原版 prefab
            //   `Alliance Trophy Info Popup` 逐节点搭的），这里**真的开它**（不再只出声）。
            //   ⚠️ 奖杯数据仍在服务器（`AllianceTrophy` 由 `AlliancesTrophyController` 从远端配置建）
            //   ⇒ 弹窗开出来是**零值态**（名字/描述/进度留空、徽标不画），它在 `Open()` 里如实出声。
            //   ⚠️ 本地奖杯数恒 0 ⇒ 一格都不建 ⇒ **这条命中路本地走不到**（文件头 ①）；
            //   队列号 = `L_Hit`（页带，**没进格子带** —— 透明命中区只吃点击优先级，见本类顶部那段）。
            Hit(cell, "Hit", r, L_Hit, OpenTrophyPopup);
        }

        /// <summary>点奖杯格 → 开 `TrophyInfoPopup`（原版 `AllianceTrophiesView.HandleTrophyClick`：
        /// `WindowsManager.OpenWindow(trophyInfoPopup, 那一枚奖杯)`）。
        /// ⚠️ 那枚奖杯（`AllianceTrophy`）本地没有（服务器）⇒ 开的是**零值态**，`TrophyInfoPopup` 里出声。</summary>
        void OpenTrophyPopup()
        {
            Say("点奖杯那一格：原版 `AllianceTrophyEntry.backgroundButton` → `HandleClick` → "
              + "`AllianceTrophiesView.HandleTrophyClick` → `WindowsManager.OpenWindow(trophyInfoPopup, 那一枚奖杯)`。"
              + "⚠️ 奖杯数据在服务器 ⇒ 本地这一格压根不会有（见文件头 ①），开出来是零值态。");
            var wm = Win != null && Win.Manager != null ? Win.Manager : WindowsManager.Instance;
            if (wm == null)
            {
                Debug.LogWarning("[Social] 没有 `WindowsManager` ⇒ `TrophyInfoPopup` 开不了"
                               + "（先走 `WindowsManager.EnsureHost()`）。");
                return;
            }
            TrophyInfoPopup.Open(wm);
        }

        // ============================================================ 软边（`RectMask2D.m_Softness`）
        //
        // 🔴 **为什么这一格单独一条画图路**：`SocialPage.Rect/Nine/Text/Hit` 只转 `Clip`、
        //    **没有软边那个形参**（`SocialPage` / `SocialView` 上也没有 `ClipSoftness` 字段 ——
        //    全壳只有 `MenuWindowBase` 与 `PlayerProfileWindow` 那两处自己声明了）。
        //    而**社交这四处滚动里只有这一格**的原版 `m_Softness ≠ (0,0)`（`_tmp_view/q1_rm2d.txt:45`；
        //    `Open Alliances>Viewport` / `Friends Container/Viewport` 都是 `(0,0)`）。
        //    ⚠️ 队列档与取图仍走宿主（`Page.Q + QOff` / `Page.ArtOf`），与那几路**同一口径**。
        //    🔴 **2026-10-13（A435 阶段 2 · 丙）**：软边**已经迁到 `Scroll Rect` 那颗 `ViewportClip` 上**
        //    （见 `BuildTrophy` 里 `ViewportClip.Hang(…)` 那一段）⇒ 下面三条软边助手
        //    **不再自己转 `clip`/`clipSoftness`**，一律传 `null` 让 `MenuDraw.*` 沿父链解析到那颗节点。
        //    ⛔ `TrophyClipSoftness` **留着**（它是节点的取值来源，也是回落那一档的带宽）—— 别删。

        /// <summary>原版 `TrophiesWindow>Scroll Rect` 那个 `RectMask2D` 的 `m_Softness`
        /// （**画布像素**：`x` 管左右两条边、`y` 管上下两条边 ⇒ `(0,50)` = 上下各 50px 渐隐）。
        /// 🔴 **类型是 `Vector2Int`**：它现在**同时**是那颗 `ViewportClip` 节点的 `softness`
        /// （`ViewportClip.softness` 照原版 `RectMask2D.m_Softness` 就是 `Vector2Int`）——
        /// ⛔ 别再加一份「节点专用」的副本（两处写同一个值 = 迟早不一致）。</summary>
        static readonly Vector2Int TrophyClipSoftness = new Vector2Int(0, 50);

        /// <summary>🔴 **2026-10-13（A435 阶段 2 · 丙）：`_trophyClip` 字段已删。**
        /// 它原来是「这一格画内容时的裁切边界」—— 现在那份状态长在 `Scroll Rect` 那颗
        /// `ViewportClip` 上（见 `BuildTrophy` 里 `ViewportClip.Hang(…)` 那一段），
        /// 上面三条助手一律传 `null` 让 `MenuDraw.*` 沿父链解析。</summary>

        /// <summary>九宫格 + 软边（`SocialView.Nine` 的软边版）。
        /// 🆕 2026-10-04（A55③）：补 `borderOutPx` —— 原版那几件的 `m_PixelsPerUnitMultiplier`
        /// 会**缩放画出来的角块**（角块 = `m_Border ÷ ppuMul`，见 `MenuDraw.Nine` 的同名形参注释）。
        /// 奖杯格这一族里三张带 ppuMul 的：`OctagonUI Border SDF` 52 ÷ **0.94** = 55.319 ·
        /// `40k_campaign_bar_bg` / `_outline` 20 ÷ **0.9** = 22.222 · `40k_campaign_bar_fill` 10 ÷ **0.9** = 11.111
        /// （普查 `:359,367,369,371` 的 `ppuMul=` 那一列）。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）**：`clip`/`clipSoftness` **不再自己传** —— 传 `null`
        /// 让 `MenuDraw.Nine` 沿父链解析到 `Scroll Rect` 那颗 `ViewportClip`（框与 `(0,50)` 软边都在它身上）。</summary>
        GameObject NineSoft(Transform parent, string art, PxRect r, Vector4 border, string name, int qOff,
                            Color? tint = null, bool fillCenter = true, Vector4? borderOutPx = null)
        {
            var tex = Page.ArtOf(art);              // 取不到会记进宿主窗的 `MissingArt`（自检会红）
            if (tex == null) return null;
            return MenuDraw.Nine(parent, tex, r, border, tex.width, tex.height, Page.Q + QOff + qOff,
                                 tint, fillCenter, name, borderOutPx: borderOutPx);
        }

        /// <summary>一张图 + 软边（`SocialView.Rect` 的软边版，`NineSoft` 的同族）。
        /// `art == null` 不给（这一族全是**有图的**件；纯色件走 `SocialView.Rect` + `CardArt.Solid()`）。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）**：同 `NineSoft` —— 裁切状态走父链上的节点。</summary>
        ImageQuad RectSoft(Transform parent, string art, PxRect r, string name, int qOff,
                           Color? tint = null, bool keepAspect = false)
        {
            var tex = Page.ArtOf(art);
            if (tex == null) return null;
            return MenuDraw.Rect(parent, tex, r, name, Page.Q + QOff + qOff, tint, keepAspect);
        }

        /// <summary>一段字 + 软边（`SocialView.Text` 的软边版）。
        /// ⚠️ 顺序与 `MenuWindowBase.Text` 一致：**先定字号、再裁**（`SetText`/`SetGlyphHeight` 会把
        /// mesh 重算回去 ⇒ 裁早了等于没裁）。
        /// 🔴 2026-10-04（A30）：`alignLeft` 也与 `SocialView.Text` **同义** —— 奖杯那格的 `counter`
        /// 原版是 **`Center/Middle`**（普查 `:372`）⇒ 传 `false`（此前那一行借用 `Text(...)` 的缺省
        /// `alignLeft: true`，**把居中的数按左对齐画了** —— 顺带订正）。
        /// 🔴 **2026-10-13（A403①）：`autoMinPx` / `alignLeft` 的缺省值【已删 · 两个形参都必填】**
        /// —— 与 A323（`SocialPage.Text` / `SocialView.Text` / `ProfilePage.Text`）**同一套口径**：
        /// 缺省值**不是原版概念**（原版只有**逐个节点**的真值）⇒ 去掉它才能**倒逼逐处现读**。
        /// · **`alignLeft` 那一个原本就是【死的】**：本口**只有两个调用点**（本文件 `BuildTrophyCell` 里
        ///   `title` 与 `counter` 各一处，现读 `:612` / `:667`），**两处都显式传了 `alignLeft: false`**
        ///   ⇒ 删它**零行为变化**（判据 = `grep -n "TextSoft(" Shell/AllianceMemberTab.cs` ⇒ 只有那两处）。
        /// · **`autoMinPx` 是被 C# 一起拽下来的**：`CS1737`（必填形参不许排在可选形参后面）⇒ 要删
        ///   `alignLeft` 的缺省就得连它前面那个一起定。**同样是零行为变化** —— 那两个调用点今天
        ///   也都显式传了 `12f`。</summary>
        Label TextSoft(Transform parent, PxRect r, string text, Color color, string name, float fontPx, int qOff,
                       float autoMinPx, bool alignLeft)
        {
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：两处都改走**节点态**那一版 ——
            //   ① 求交：`ClipRect`（纯矩形函数，手上没有 `Transform`）换成 `ClipRectAbove`；
            //   ② `ClipText` 收的必须是**调用方原样那一份**（这里 = `null`），它内部自己解析
            //      （节点挪了/后挂都跟得上，A484）。⛔ 别把解析后的框传进去（那会存成快照）。
            if (!MenuDraw.ClipRectAbove(parent, r, null, out _)) return null;
            var lb = MenuDraw.TextBox(parent, r, text, color, name, fontPx, autoMinPx, Page.Q + QOff + qOff);
            if (lb != null && alignLeft) MenuDraw.AlignLeft(lb, r);
            if (lb != null) MenuDraw.ClipText(lb, null, Vector2.zero);
            return lb;
        }

        // ---------------------------------------------------------- 联盟聊天预览（属于**这一支**）

        /// <summary>`ChatPreview`（`1479.80,109.80→1879.80,169.80`）—— ⚠️ **它是联盟这一支的孩子、
        /// 不属于好友页**（§B·10）。两条消息行是 `ChatPreviewMessage`；底下那颗钮开的是 `ChatPanel`。</summary>
        void BuildChatPreview()
        {
            var cp = Node(Root, "ChatPreview", ChatPreviewR);
            var box = Node(cp, "Container", new PxRect(1479.50f, 109.80f, 1851.80f, 169.80f));
            Nine(box, "Closed-Chat_background", new PxRect(1479.50f, 109.80f, 1851.80f, 169.80f),
                 Vector4.zero, "Image", L_Panel);
            MsgRow(box, new PxRect(1494.50f, 112.80f, 1851.80f, 139.80f), "Message Preview");
            MsgRow(box, new PxRect(1494.50f, 139.80f, 1851.80f, 166.80f), "Message Preview (1)");
            var btn = Node(cp, "Button", new PxRect(1851.80f, 109.80f, 1879.80f, 169.80f));
            Rect(btn, "40k_alliances_icon_chat", new PxRect(1851.80f, 109.80f, 1879.80f, 169.80f),
                 "Icon", L_Art, null, true);
            Hit(btn, "Hit", new PxRect(1851.80f, 109.80f, 1879.80f, 169.80f), L_Hit, () =>
            {
                Say("`ChatPreview` 那颗钮：原版开**聊天窗**（`ChatPreview.OpenChat` → `WindowsManager`）。");
                // 🔴 聊天窗本身已建（`Shell/ChatPanelWindow.cs`）⇒ 这里**真的开它**，不是只出声。
                if (Win != null) Win.OpenChat();
            });
        }

        void MsgRow(Transform parent, PxRect r, string name)
        {
            var row = Node(parent, name, r);
            // 原版两条预览的文案是富文本（`<color=#00FF20>Player Name:</color> Message`），字号 23
            Text(row, r, "<color=#00FF20>Player Name:</color> Message", Color.white, "text", 23f, L_Text, 0f,
                 wrap: false, alignLeft: true);                                 // A319 #4（原版 `Left`(1)）· 原版 `折行=0`（判据见 `Toggle` 那段）
        }
    }

    // ==================================================================
    //  `GeneralDetails`（= 原版 `AllianceView`）
    // ==================================================================

    /// <summary>原版 `AllianceView`（节点名 `GeneralDetails`）——原版**两支各一份实例**
    /// （同名不同 pid，§B·5）。🔴 **2026-10-04（A55②）起两份都建**（此前只建了一棵）：
    /// <list type="bullet">
    /// <item>**已入盟支那一棵** = `AllianceMemberVariant>GeneralDetails`（RT `-4327119531760820061`，act **T**）
    /// —— `AllianceMemberTab` 建它（几何取 act T，A29 定的）。</item>
    /// <item>**未入盟支那一棵** = `AllianceNotMemberVariant>GeneralDetails`（RT `-8680982849342087005`，act **F**）
    /// —— `AllianceSearchTab` 建它（几何取 act F，**出厂关着**），由「**点公开列表里某个盟 → 看它的详情**」
    /// 那一态点亮（原版 `AllianceSearchTab.HandleDisplayAlliance`，见 `AlliancesTab.cs` 同名方法）。</item>
    /// </list>
    /// ⚠️ 本段原来写「**一份 builder，建两棵独立的树**」—— 那是**没兑现的说法**（`grep Build(` 只有一处调用，
    /// 2026-10-04 A35③ 审查查出、A29 就地订正；**A55② 把第二棵补上了** —— 那次留的 `r` 形参 / 这次的
    /// `Variant` 形参就是那个口子）。
    /// 🔴 **两份的几何**：**不是纯平移、更不是等比缩放**（左锚件差 **(+1.5, −26.79)**、右锚件 x 差 **−1.0**
    /// —— 因为根的右边界 `1919.00` vs `1920.00`、宽差 2.5）⇒ **逐值各写一套字面值**（`GeoT` / `GeoF`），
    /// ⛔ 别拿一份去推算另一份。</summary>
    public static class AllianceGeneralDetails
    {
        /// <summary>两棵 `GeneralDetails`（同名不同 pid，§B·5）—— `Build` 用哪一套几何 + 哪一套显隐。</summary>
        public enum Variant
        {
            /// <summary>`AllianceMemberVariant>GeneralDetails`（RT `-4327119531760820061`，act **T**）。
            /// `Config fields` 那一行建**两个下拉 + 三个钮**、`extra_info` 不建（普查 `:279-315`）。</summary>
            Member,
            /// <summary>`AllianceNotMemberVariant>GeneralDetails`（RT `-8680982849342087005`，act **F**）。
            /// `Config fields` 那一行**只建 `extra_info`**（那只读摘要）、那五个控件不建（普查 `:186-222`）。</summary>
            Search,
        }

        /// <summary>🆕 **2026-10-12（A392）：两个评级块那一格的空态串 = `'-------'`（7 个连字符）**
        /// —— 原版预制体**自己在那一格里写的就是这个**：
        /// `AllianceMemberVariant>GeneralDetails>Content>{Alliance,Draft} Rating Display>Individual rating value`，
        /// 判据 = `python 工具/menu_dump.py bundle_menus_assets_all "AllianceMemberVariant" --depth 7 --md`
        /// 的「文字（字号/对齐/色）」列两行逐字是 `'-------'`；原始 JSON 实读两份实例
        /// （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-69344794122919773.json` 与
        /// `…_-7031947786318323549.json`，`len(m_text) = 7` 用 python 逐字节数过）。
        /// <para>**为什么用「资产里的字面串」而不是留空串**（本件改的就是这一点）：与本文件同族已有的两处**同一口径**
        /// —— `extra_info` 的 `English / Private`（`:187`）与 `members label` 的 `Members: --/20`，
        /// 那两格与这两格的共同点 = **运行期那个值在服务器上、本地没有源**（铁律 11：缺的部分照原版补，
        /// ⛔ 不自己发明、也不留一个原版没有的空白）。**同族先例** = `Shell/RankedTab.cs:149` 的 `ScoreEmpty`
        /// —— 同一类节点（`AllianceRatingDisplay>Individual rating value`）、同样是「照预制体自己在值那一格写的」；
        /// ⚠️ **那颗是 6 个连字符、本处是 7 个** ⇒ **各取各的，别互相套用**。</para>
        /// <para>🔴 **一处如实标注（读过分编译方法体，⛔ 不是猜）**：原版**运行期**会把这一格**覆盖成数字** ——
        /// `AllianceView__Draw.c:154,185` 两处都调 `AllianceRatingDisplay__Initialize(…, 评级 int, …)`，
        /// 而 `AllianceRatingDisplay__Initialize.c` 里那条路是 `System_Int32__ToString` /
        /// `System_String__Format` 算完就 `set_text`（那颗 = prefab 的 `ratingText`，反编译里是 `*(param_1 + 0x20)`）
        /// ⇒ **在那个状态下破折号画不出来**，
        /// 它只在「预制体出厂态 / 没人赋值」时可见（另证：`d:/2/tools/il2cpp_out/stringliteral.json` 里
        /// **没有** 7 连字符这条字面量 —— 表里有 `-` / `--` / `---` / 10 个 / 21 个，**没有 7 个**
        /// ⇒ 它不是代码产出的串，纯粹是资产字段）。
        /// 我们拿不到那个数字（盟评级在服务器）⇒ 显示**资产自带的空态串**；⛔ **不是**抄同族成员行那两颗的
        /// 示例数字（`member` 那两格原版写的是 `'32'`，那是**数据那一档**的样例，此处本来就只有破折号）。</para></summary>
        public const string RateEmpty = "-------";

        /// <summary>`MemberList>Scroll View` / `Viewport` 的矩形 = **369.67,493.63→1880.67,1080.05**。
        /// 🔴 **出处 = `AllianceMemberVariant>GeneralDetails>MemberList>Scroll View`**（RT `-1553194882346393437`，
        /// 普查 `资料/普查产出_0927/社交_联盟与好友页.md:323`）—— **正是 `AllianceMemberTab` 那一棵该用的那一份**。
        /// 原版 `Scroll View` 是 `Image + ScrollRect h=0 v=1`、`Viewport` 是 `Image + Mask`（`showGraphic=0`）。
        /// 🔴 **`Scroll View` 与 `Viewport` 在这个 prefab 里是同一个矩形**（逐字段相同）⇒ 只留一个常量，
        /// 免得两处各写一遍迟早不一致（CLAUDE.md §三）。滚动区（`MenuScroll.Viewport`）就用它。
        /// ⚠️ **另一棵**（`AllianceNotMemberVariant` 下那份，act F）的视口 = `SearchViewportR`（下面那个），
        /// **不是这个数** —— 两棵各取各的。
        /// ⚠️ **2026-10-04 更正（A35③）**：本段原来把两份实例写反了（说 369.67… 出自
        /// `AllianceNotMemberVariant>GeneralDetails`、把 371.17… 派给 `AllianceMemberVariant`）——
        /// **反了**：`AllianceNotMemberVariant>GeneralDetails>MemberList>Scroll View`
        /// （RT `8161084728224702627`，act F）才是 **371.17,466.85→1882.17,1080.02**（普查 `:230`）。
        /// 两个 `GeneralDetails` 的父链/act 见本文件里 `MemberScroll` 的注释（同处还有一条「几何混了两份」的记账）。</summary>
        public static readonly PxRect MemberViewportR = new PxRect(369.67f, 493.63f, 1880.67f, 1080.05f);

        /// <summary>`AllianceNotMemberVariant>GeneralDetails>MemberList>Scroll View` / `Viewport` 的矩形
        /// = **371.17,466.85→1882.17,1080.02**（🆕 2026-10-04 A55② 补：未入盟支那一棵的视口）。
        /// 出处 = 普查 `社交_联盟与好友页.md:230`（RT `8161084728224702627`，act F 那一棵）——
        /// ⚠️ 与 `MemberViewportR`（act T 的 `369.67,493.63→1880.67,1080.05`）**不是同一个矩形**
        /// （上边差 26.78、左差 1.5、右差 1.5），别混用。</summary>
        public static readonly PxRect SearchViewportR = new PxRect(371.17f, 466.85f, 1882.17f, 1080.02f);

        // ============================================================ 🔴 2026-10-04（A55②）：两套几何
        //
        // 原版两份 `GeneralDetails` 的逐节点真值（**同名不同 pid、数值也不同**）。
        // 判据 = 普查 `资料/普查产出_0927/社交_联盟与好友页.md`：
        //   · **act T 那一份**（`AllianceMemberVariant>GeneralDetails`，RT `-4327119531760820061`）= `:264-348`
        //   · **act F 那一份**（`AllianceNotMemberVariant>GeneralDetails`，RT `-8680982849342087005`）= `:171-240`
        // 每个常量后面的 `:NNN` 就是它那一行的行号（⛔ 不是「自洽的算式」）。
        // 🔴 **两套的差不是常数**：左锚件 **(+1.5, −26.79)**、右锚件 x **−1.0**、底锚件 y **−0.03**
        //    （根：act T `331.17,188.83→1920.00,1080.05` vs act F `332.67,162.04→1919.00,1080.02`）
        //    ⇒ **逐值各写一套**（铁律 5·c：一个值 ≠ 全部情况）。

        /// <summary>一棵 `GeneralDetails` 的整套几何。</summary>
        sealed class Geo
        {
            public PxRect Badge;        // `BadgeDrawer`（`Frame` / `Badge` 与它同矩形）
            public PxRect Name;         // `Alliance name text`
            public float RateX1, RateIconX2, RateX2, RateH;   // 两个评级块共用的横向四值
            public float RateY1, RateY2;                      // 两块各自的 y（高恒 `RateH`）
            public PxRect Cfg;          // `Config fields`（`HorizontalLayoutGroup` 的容器）
            public PxRect? ExtraInfo;   // `Config fields>extra_info`（**只有 act F 那份亮**）
            public PxRect Desc;         // `Description input text`
            public PxRect DescText;     // └ `description text`
            public PxRect Divider;      // `Divisor line members`
            public PxRect List;         // `MemberList`
            public PxRect ListLabel;    // └ `members label`
            public PxRect Viewport;     // └ `Scroll View` = `Viewport`（两份**同一个矩形**）
        }

        /// <summary>**act T 那一份**（普查 `:264-348`）—— `AllianceMemberVariant>GeneralDetails`，
        /// 也是 `AllianceMemberTab` 那一棵（A29 定的）。</summary>
        static readonly Geo GeoT = new Geo
        {
            Badge = new PxRect(358.90f, 197.27f, 609.45f, 432.39f),      // :267
            Name = new PxRect(613.38f, 204.27f, 1112.74f, 275.35f),      // :270
            RateX1 = 613.38f, RateIconX2 = 673.38f, RateX2 = 1099.96f, RateH = 80.83f,   // :271-278
            RateY1 = 264.16f, RateY2 = 344.99f,                          // :271 / :275
            Cfg = new PxRect(1080.54f, 209.81f, 1863.35f, 269.81f),      // :279
            ExtraInfo = null,                                            // act T 里它是 **F**（:280）
            Desc = new PxRect(1130.44f, 288.85f, 1879.21f, 482.08f),     // :316
            DescText = new PxRect(1146.32f, 291.64f, 1863.33f, 481.71f), // :319
            Divider = new PxRect(330.67f, 489.85f, 1920.50f, 493.53f),   // :320
            List = new PxRect(369.42f, 446.55f, 1880.92f, 1080.05f),     // :321
            ListLabel = new PxRect(369.42f, 447.01f, 964.25f, 488.44f),  // :322
            Viewport = MemberViewportR,                                  // :323-324
        };

        /// <summary>**act F 那一份**（普查 `:171-240`）—— `AllianceNotMemberVariant>GeneralDetails`，
        /// 原版由 `AllianceSearchTab.HandleDisplayAlliance` 点亮（「看别的盟」那一态）。</summary>
        static readonly Geo GeoF = new Geo
        {
            Badge = new PxRect(360.40f, 170.48f, 610.95f, 405.60f),      // :174
            Name = new PxRect(614.88f, 177.48f, 1114.24f, 248.56f),      // :177
            RateX1 = 614.88f, RateIconX2 = 674.88f, RateX2 = 1101.46f, RateH = 80.83f,   // :178-185
            RateY1 = 237.38f, RateY2 = 318.21f,                          // :178 / :182
            Cfg = new PxRect(1079.54f, 183.02f, 1862.35f, 243.02f),      // :186
            ExtraInfo = new PxRect(1408.66f, 183.02f, 1862.35f, 243.02f), // :187（act F 里它是 **T**）
            Desc = new PxRect(1129.44f, 262.06f, 1878.21f, 455.29f),     // :223
            DescText = new PxRect(1145.32f, 264.85f, 1862.33f, 454.92f), // :226
            Divider = new PxRect(332.17f, 463.06f, 1919.50f, 466.74f),   // :227
            List = new PxRect(370.92f, 419.76f, 1882.42f, 1080.02f),     // :228
            ListLabel = new PxRect(370.92f, 420.23f, 965.75f, 461.65f),  // :229
            Viewport = SearchViewportR,                                  // :230-231
        };

        /// <summary>建一棵 `GeneralDetails`。`variant` 决定**用哪一套几何 + 哪一套显隐**；
        /// `allianceName` 是盟名那一行（已入盟支那棵传当前盟的名字、未入盟支那棵传「正在看的那个盟」的名字）。
        /// **返回盟名那个 `Label`** —— 未入盟支那棵要在切进详情态时改字，调用方得留个句柄；
        /// `scroll` 交出的成员列滚动区同理（两棵树的视口不同，各自的主人要拿它做自检/回调）。
        /// ⚠️ 第一形参是 `SocialView`（不是 `AllianceMemberTab`）：两棵树分属**两个不同的子视图**
        /// （`AllianceMemberTab` / `AllianceSearchTab`，都派生自 `SocialView`）。`MemberScroll` /
        /// `SetMemberContent` 只有 `AllianceMemberTab` 有 ⇒ 那里按类型判一下（见方法末）。</summary>
        public static Label Build(SocialView v, Transform root, PxRect r, Variant variant, string allianceName,
                                  out MenuScroll scroll)
        {
            var g = variant == Variant.Member ? GeoT : GeoF;

            // ============================================================ 判据（**铁律 4：先按父链/用途字段判用途，再取那一份的值**）
            //   · 原版挂着**两个** `GeneralDetails`（同名不同 pid，§B·5）：
            //     `AllianceMemberVariant>GeneralDetails`（RT `-4327119531760820061`，**act T**）与
            //     `AllianceNotMemberVariant>GeneralDetails`（RT `-8680982849342087005`，act **F**）；
            //   · **两份各自都对，按用途各取各的**（不是「谁权威」）：
            //       - `AllianceMemberTab`（已入盟支，二级页签 `General`/`Trophies`、`ChatPreview` 都只在它里面）
            //         ⇒ 取 **act T** —— 那一态里 `LanguagesDropdown`/`Privacy Dropdown`/`Edit`/`Confirm`/`Cancel`
            //         五个**全亮**、`extra_info` 灭（普查 `:279-315`）。**A29（2026-10-04）定的**。
            //       - `AllianceSearchTab`（未入盟支）⇒ 取 **act F** —— 原版由 `HandleDisplayAlliance`
            //         （「**点公开列表里某个盟 → 看它的详情**」）把它点亮
            //         （`decomp_full/AllianceSearchTab__HandleDisplayAlliance.c`：关掉 `join`/`create`
            //         两个菜单 → `SetActive(allianceView, true)` → `AllianceView.Draw(group)`；
            //         点亮之前先把 `AllianceView.content` 关掉、拿到 group 由 `Draw` 再开 —— 见下）。
            //         那一态里 `extra_info`（`English / Private` 那行**只读摘要**）亮、可编辑的那五件全灭
            //         （普查 `:186-222`）—— **正好与 act T 那份相反**。**A55②（2026-10-04）建的**。
            //   ⚠️ A29 之前那棵树是「两份实例各取一半」（上半段 act F / 下半段 act T）；A55② 之前
            //      **act F 那棵压根没建**。两笔账现在都清了。

            // `GeneralDetails>Content`：原版这棵树**多一层 `Content`**（与根同矩形，普查 `:265`（act T）/
            //   `:172`（act F））⇒ 照建。⚠️ 原版 `HandleDisplayAlliance` 点亮 `allianceView` 之后
            //   **先把 `content` 关掉**、等 `AllianceView.Draw(group)` 拿到数据才开（避免闪一帧空面板）
            //   —— 我们没有那个异步源（盟数据在服务器）⇒ 这一层**建完就开着**（`Variant.Search` 那棵整棵树出厂关着，
            //   见 `AllianceSearchTab.BuildGeneralDetails`），并把「哪些是服务器数据」在调用方出声。
            var content = Node(root, "Content", r);

            // `BadgeDrawer`（盟徽：Frame + Badge）—— 徽标图的来源在服务器（盟自己的存档）
            var bd = Node(content, "BadgeDrawer", g.Badge);
            Node(bd, "Frame", g.Badge);
            Node(bd, "Badge", g.Badge);

            // 盟名（原版静态样例是 `Alliance Name bla bla`；我们读数据源，空就空着）
            var nm = Node(content, "Alliance name text", g.Name);
            var nameLabel = v.Text(nm, g.Name, allianceName ?? "", Color.white, "Text", 50f, 3, 18f,
                                   wrap: false, alignLeft: true);   // A319 #5（原版 `Left`(1)）· 原版 `折行=0`（判据见本文件 `Toggle` 那段）
            // 🆕 **2026-10-16（A712 阶段 2）**：原版 `对齐=Left/Midline` ⇒ `m_VerticalAlignment = 4096`
            //  （判据 = 上面那条 `AllianceMemberVariant --depth 20` 的同一次 dump，节点 `Alliance name text`）。
            MenuDraw.SetVAlign(nameLabel, Label.VAlign.Midline, g.Name);

            // 两个评级块：`Alliance Rating Display`（段位图标）/ `Draft Rating Display`（骷髅图标）
            // 🔴 **两块的 `Main Icon` 不是同一张图**：一个 `40k_UI_icon_ranked_Skirmish`、
            //    一个 `40k_battle_Win Skull`（普查 `:273` / `:277`）。
            // 🆕 2026-10-12（A392）：值那一格用 `RateEmpty`（= 原版资产里那一格自己的 `'-------'`），
            //    原来传的是**空串** —— 判据与理由全写在 `RateEmpty` 的头注释里（⛔ 别在这儿抄第二份）。
            RatingRow(v, content, g, g.RateY1, "Alliance Rating Display", "40k_UI_icon_ranked_Skirmish", RateEmpty);
            RatingRow(v, content, g, g.RateY2, "Draft Rating Display", "40k_battle_Win_Skull", RateEmpty);

            var cf = Node(content, "Config fields", g.Cfg);
            if (variant == Variant.Search)
            {
                // ---- 未入盟支那一棵（act F）：`Config fields` 里**只有 `extra_info`**（只读摘要）----
                //   ⛔ 那五个可编辑件在 act F 那份里**全灭**（普查 `:188,201,214,217,220`）——
                //      它们的「亮」是**改盟设置**那一态（`AllianceView.OnToggleEditClicked` 才点亮），
                //      而那一态要求先点 `Edit button`，`Edit` 自己在这份里就是灭的 ⇒ **本地切不到** ⇒ 不建
                //      （与本文件头那条「可切到的状态 ⇒ 建；只是未激活态 ⇒ 不建」同一口径）。
                if (g.ExtraInfo.HasValue)
                {
                    var ei = g.ExtraInfo.Value;
                    // 文案 = **资产里那一行的字面样例串**（普查 `:187`）—— 运行期那两个值（语言 / 隐私）
                    // 是服务器给的，本地没有源。口径同下面 `Members: --/20` 与奖杯页那两个样例串。
                    // 对齐照原版 `Right/Middle`（`SocialView.Text` 只给「左对齐 / 居中」两档 ⇒ 右对齐自己接一下）。
                    var lb = v.Text(cf, ei, "English / Private", Color.white, "extra_info", 40f, 3, 18f,
                                    alignLeft: false, wrap: true);   // A258：原版 `折行=1`
                    if (lb != null) MenuDraw.AlignRight(lb, ei);
                }
            }
            else
            {
                // ---- 已入盟支那一棵（act T）：五个子件**全建** ----
                //   `Config fields` 是 `HorizontalLayoutGroup`（spacing 14.5）；
                //   ⚠️ 五件的 x 是布局跑出来的（逐值照普查 `:281-315`；相邻两件之间正好 = spacing 14.5）。
                //   ⚠️ 点击一律**出声**（改盟设置要服务器；下拉的展开要靠 Unity 内置 `Template` —— 我们不建它，
                //      同 `AlliancesTab.CreateAllianceMenu` 那两处下拉的处理）。
                Dropdown(v, cf, "LanguagesDropdown",
                         new PxRect(1130.90f, 210.41f, 1380.90f, 269.81f),      // :281
                         new PxRect(1355.90f, 230.11f, 1375.90f, 250.11f));      // :283
                Dropdown(v, cf, "Privacy Dropdown",
                         new PxRect(1395.40f, 210.41f, 1645.40f, 269.81f),      // :294
                         new PxRect(1620.40f, 230.11f, 1640.40f, 250.11f));      // :296
                YellowBtn(v, cf, "Edit button",
                          new PxRect(1659.90f, 209.81f, 1718.05f, 269.81f),      // :307
                          "40k_general_bt_yellow_edit",
                          new PxRect(1662.00f, 212.49f, 1712.15f, 267.13f),      // :309
                          "`Edit button`：原版进**改盟设置**那一态（`AllianceView.OnToggleEditClicked` —— 把两个下拉与"
                        + "`Confirm`/`Cancel` 一起点亮并置 `interactable`）。**改设置要服务器**（原版 `OnEditSave`"
                        + " 发一条改盟请求）⇒ 本地没有那个源。");
                YellowBtn(v, cf, "Confirm button",
                          new PxRect(1732.55f, 209.81f, 1790.70f, 269.81f),      // :310
                          "40k_general_bt_yellow_confirm",
                          new PxRect(1734.23f, 215.18f, 1787.20f, 261.95f),      // :312
                          "`Confirm button`：原版**提交改盟设置**（`AllianceView.OnEditSave`）—— 要服务器。");
                YellowBtn(v, cf, "Cancel button",
                          new PxRect(1805.20f, 209.81f, 1863.35f, 269.81f),      // :313
                          "40k_general_bt_yellow_back",
                          new PxRect(1804.80f, 208.31f, 1860.95f, 266.31f),      // :315
                          "`Cancel button`：原版**退出改盟态**（`AllianceView.OnEditCancel` —— 把两个下拉与"
                        + "`Confirm`/`Cancel` 关掉、把 `Edit` 重新点亮）。那一态在 act T 里**出厂就是亮的**，"
                        + "所以这一颗在我们这儿**没有可切的状态**。");
                // ⛔ `extra_info`（语言 / 隐私摘要，`1408.66,183.02→1862.35,243.02`）在这棵上**不建** ——
                //    它在 act T 那份里是 **act F**（普查 `:280`）⇒ 建在这棵树上就是**取错实例**
                //    （📌 A29 之前这里画的正是它；它在**另一棵**上是亮的，见上面 `Variant.Search` 那一支）。
            }

            // 联盟简介（`Description input text`；原版样例是一串 Aliance Description，属调试残留 §C·2）
            //   ⚠️ 两份的矩形不同（act T `1130.44,288.85→1879.21,482.08` / act F `1129.44,262.06→1878.21,455.29`）
            var desc = Node(content, "Description input text", g.Desc);
            var dtx = Node(desc, "description text", g.DescText);
            var lbDesc = v.Text(dtx, g.DescText, "", new Color(1f, 1f, 1f, 1f), "Text", 38f, 3, 0f, wrap: true,
                   alignLeft: true);   // A319 #7（原版 `Left`(1)）· A258：原版 `折行=1`
            // 🆕 **2026-10-16（A712 阶段 2）**：原版 `对齐=Left/**Top**` ⇒ `m_VerticalAlignment = **256 (Top)**`
            //  （判据 = `python 工具/menu_dump.py bundle_menus_assets_all "AllianceMemberVariant" --depth 20 --md`
            //   ⇒ 节点 `description text` = `717.00×190.07 · 字号=38.0 · 对齐=Left/Top`）。
            // 🔴 **本处是「Top」这一档在全仓的【第一处】生产调用**（`Top`/`Bottom` 要**框高** ⇒ 这里传的
            //   `g.DescText` **必须**是原版那个框（717×190.07）—— 传我们自己的窄框就会算错半框）。
            // ⚠️ **两条如实登记的保留**：① 我们传的是**空串**（原版那串是调试残留的长文案）⇒ 这一档
            //   **画面上看不出差别**（`OneLineBoxWorld` 量不到可见字 ⇒ `OurInkCenterWorld` 走守卫报 0，
            //   位移 = `OrigInkCenterPx(Top)` = **+79.03px**，但**没有可见字可移**）；
            //   ② 🔴 **原版这一格 `折行=1` = 多行，而本模型对多行串的 `Top` 没有正确口径**（本笔现推）：
            //   `RefreshBounds` 把**整块的行盒中心**摆在节点上，而位移只有**一行**的量级 ⇒ 落完之后
            //   **首行墨心**在 `节点 + (n−1)/2×行盒高 + 目标`（单行时那一项才为 0）。
            //   ⇒ **今天无害（空串），但谁把这段描述写进去、或谁要照这一处复制到真有多行文案的地方，
            //   必须先解决多行口径**（`W11` §5·4 / `WA712` §六·5 都是空白；要改 `Label.VOffsetWorldNow`
            //   吃块高 —— 那是机制本体）。**同一天 `Shell/ReferralPopupWindow.cs` 的 `Descripton`（真·3 行）
            //   就是照这条判据【没落】**（理由写在那一处，本笔没改机制）。
            MenuDraw.SetVAlign(lbDesc, Label.VAlign.Top, g.DescText);

            // 成员区分隔线 + `MemberList`
            v.Nine(content, "40k_Separator_Fade_Sides_Horizontal", g.Divider,
                   new Vector4(63f, 0f, 63f, 0f), "Divisor line members", 1,
                   new Color(0.875f, 0.552f, 0.286f, 1f));
            var ml = Node(content, "MemberList", g.List);
            var lbl = Node(ml, "members label", g.ListLabel);
            // 🔴 **2026-10-12（A412）：这一颗【不吃自适应】** —— `autoMinPx` 由 `18f` 改成 **`0f`**。
            //   判据 = 原版 `m_enableAutoSizing = **0**`（**原始 MB 实读两份实例，逐字段同值** ——
            //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5317387791711264931.json`
            //   （GO `-2659507220284251997`）与 `…_6451041557333008547.json`（GO `5602064994046501027`）：
            //   `m_text='Members: --/20'` · `m_fontSize=40.0` · `m_fontSizeBase=40.0` · `m_fontSizeMin=18.0`
            //   · `m_fontSizeMax=72.0` · `m_enableAutoSizing=0` · `m_HorizontalAlignment=1(Left)` ·
            //   `m_TextWrappingMode=1`）。
            //   ⚠️ **`autoMinPx = 0f` 的全部效果 = 不调 `SetAutoFitBox`**（`Shell/MenuDraw.cs` 那条守卫
            //   `if (autoMinPx > 0f && fontPx > autoMinPx)` 不成立）⇒ 没有别的副作用。
            //   ⚠️ **字号仍然画 38.35**：`SetGlyphHeight(fontPx)` 在守卫**之前**、与自适应无关
            //   （只在 `fontPx <= 0` 时才跳过）⇒ 改完**不会变成 0**。**改的是「状态」，不是「字」。**
            //   🔴 **同时如实记一条【已知偏离】（没改，留给标称那一笔）**：我们传 **38.35**、
            //   原版 `m_fontSize = **40.0**` —— 自适应关掉之后就是**永远画 38.35**（关之前是
            //   「可能被缩、也可能不缩」的不确定态）。⚠️ **本件没跑 Unity** ⇒ **画面差没实测**，
            //   不写成「无可见差」；能断的是**状态**（`Label.AutoSizing`）。
            // 🔴 **A319 #8**：原版 `Left`(1) ⇒ `alignLeft: true`（显式化，原缺省同值）。
            var lbMembers = v.Text(lbl, g.ListLabel, "Members: --/20", Color.white, "Text", 38.35f, 3, 0f, wrap: true,
                   alignLeft: true);   // A258：原版 `折行=1`
            // 🆕 **2026-10-16（A712 阶段 2）**：原版 `members label` = `对齐=Left/**Midline**`
            //  （同一份 dump；`594.83×41.42 · 字号=40.0`）。
            MenuDraw.SetVAlign(lbMembers, Label.VAlign.Midline, g.ListLabel);
            var sv = Node(ml, "Scroll View", g.Viewport);
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：这颗 `Viewport` 是**裁切状态的载体**
            //    （原版这上面是 `Image + Mask`(`showGraphic=0`)）—— 参数取原版实读的全 0
            //    ⇒ 与迁移前的 `v.SetClip(vp0)`（`vp0 = sc.Viewport`，同一个 `g.Viewport`）逐位同值。
            //    ⚠️ 两种变体（act T / act F）的 `g.Viewport` **不同** ⇒ 两棵树**各自**建一颗节点（各自记名）。
            var vp = ViewportClip.Hang(sv, "Viewport", g.Viewport, Vector4.zero, Vector2Int.zero).transform;
            // 🆕 2026-10-03（A25④）：**照原版把滚动区补上**（此前这一格一处滚动都没有 —— 见 `MemberScroll` 注释）。
            //   ⚠️ 顺序要紧：**先有滚动区、再谈裁切** —— 只补裁切会把后面的行**藏掉**而不是可滚
            //     （`BattleLogPopup` 上就是先补滚动区才对的）。
            //   ⚠️ `Owner` 取 `GeneralDetails` **这一棵**（不是整页）：点 `Trophies` 键切走（或切到另一支）时
            //     它是关的，这一格必须**一起失去滚轮命中**（`HitScroll` 判的就是 `Owner.activeInHierarchy`）。
            //   ⚠️ 两棵树的视口**矩形不同**（act T `369.67,493.63…` / act F `371.17,466.85…`）⇒ 取 `g.Viewport`。
            var sc = MenuScroll.TopAligned(g.Viewport, 0f);        // 内容高在 `AllianceMemberRow.BuildAll` 里按条数写
            sc.Owner = root.gameObject;
            sc.Elastic = true;                                     // 原版 `m_MovementType = 1` = Elastic
            SocialPage.RegisterScroll(sc);                         // 指针层要认识它，滚轮才落得到这一格上

            var mcontent = Node(vp, "Content", new PxRect(g.Viewport.x1, g.Viewport.y1,
                                                          g.Viewport.x2, g.Viewport.y1 + 184f));
            // 滚轮只改 `Offset`、**画是调用方的事**（这条回调 = 原版 `AllianceMemberList` 清空重填那条路）。
            // 🔴 2026-10-04（A55②）：这里从 `v.RebuildMembersForTest`（方法组）改成**闭包** ——
            //    因为 `v` 现在可能是 `AllianceSearchTab`（没有那个方法）。行的建法与原来**同一份**
            //    （`AllianceMemberRow.BuildAll`），`AllianceMemberTab.RebuildMembersForTest` 那条
            //    「没登记就出声」的守卫**照旧供外部调用**（自检走它）。
            sc.OnChanged = () => AllianceMemberRow.BuildAll(v, mcontent, sc);

            var mt = v as AllianceMemberTab;
            if (mt != null)
            {
                mt.MemberScroll = sc;
                mt.SetMemberContent(mcontent);   // 🔴 A435·丙起：裁切在 `AllianceGeneralDetails` 建的 `Viewport` 节点上
            }
            AllianceMemberRow.BuildAll(v, mcontent, sc);
            scroll = sc;
            return nameLabel;
        }

        /// <summary>一个**合上的下拉**（`Config fields` 里那两个）：九宫底 + 箭头 + 一个出声的命中区。
        /// ⚠️ 原版那两件的 `Label` **出厂是空串**（运行期由 `AllianceView.Start` 从本地化服务里填语言名 /
        ///    隐私枚举名 —— §C·2）⇒ 我们**不编**：底 + 箭头照建，文字留空、点了出声。
        /// ⚠️ 展开要 `Template`（Unity 内置模板，§B·13 明说别当业务节点）⇒ 不建。
        /// 底图/箭头与染色**逐值照普查**（`:281-283`）：`40K_dropdown_field_closed` 727×102 九宫 (60,35,60,35)
        /// · 染色 `(1,0.475,0.098,1)`；`40K_dropdown_arrow_closed` 46×19 · 染色 `(0.361,0.188,0.0588,1)`。</summary>
        static void Dropdown(SocialView v, Transform parent, string name, PxRect fieldR, PxRect arrowR)
        {
            var f = Node(parent, name, fieldR);
            v.Nine(f, "40K_dropdown_field_closed", fieldR, new Vector4(60f, 35f, 60f, 35f), "Bg", 0,
                   new Color(1f, 0.475f, 0.098f, 1f));
            v.Rect(f, "40K_dropdown_arrow_closed", arrowR, "Arrow", 2,
                   new Color(0.361f, 0.188f, 0.0588f, 1f), true);
            v.Hit(f, "Hit", fieldR, 3, () => SocialPage.Say(
                $"`{name}` 下拉：**打不开**（展开要 Unity 内置 `Template`；选中的值由 `AllianceView.Start` "
              + "从本地化服务 / 隐私枚举里填，本地没有那个源 —— 改盟设置也要服务器）。"));
        }

        /// <summary>一颗黄圆钮（`Edit` / `Confirm` / `Cancel`）：`40k_general_bt_yellow`（71×71 · Simple ·
        /// **preserveAspect** · 染色 `(1,0.773,0.333,1)`，普查 `:307`）+ 图标（`…_edit`/`_confirm`/`_back`）。
        /// ⚠️ 三件的 `Button Text`（'X'）出厂 **act F**（普查 `:308,311,314`）⇒ 不建（原版就是图标钮）。
        /// 🔴 **悬停/按下那两张图是【查到的原版值】**（不是同族先例 —— 2026-10-04 用 `工具/menu_dump.py`
        /// 直读这三颗的 `m_SpriteState`，三颗一致：`HL=40k_general_bt_yellow_hover`；
        /// 按下那张 = `40k_general_bt_yellow_pressed`，原版普查件 `资料/普查产出_1003/按钮悬停图_普查.md:36` 明列）。</summary>
        static void YellowBtn(SocialView v, Transform parent, string name, PxRect btnR, string icon,
                              PxRect iconR, string say)
        {
            var n = Node(parent, name, btnR);
            var bg = v.Rect(n, "40k_general_bt_yellow", btnR, "Image", 1,
                            new Color(1f, 0.773f, 0.333f, 1f), true);
            v.Rect(n, icon, iconR, "Image", 2, null, true);
            v.Hit(n, "Hit", btnR, 3, () => SocialPage.Say(say), bg, null,
                  "40k_general_bt_yellow_hover", "40k_general_bt_yellow_pressed");
        }

        /// <summary>一个评级块：`Main Icon` + `Individual rating value`（🔴 **左对齐** —— 见下）。
        /// ⚠️ 原版还有 `Secondary Icon`（act **F**）⇒ 不建。
        /// 行内两件的 x 一律取**这一套几何**的 `RateX1/RateIconX2/RateX2`（两份各一套，见 `Geo`）。
        /// <para>🔴 **2026-10-12（A319 #9）就地订正（铁律 5，保留更正痕迹）**：本段原来写「**右对齐**」，
        /// 代码里也传着 `alignLeft: false`（= 居中）—— **两条都错**。原版是 **`Left/Midline`**
        /// （`m_HorizontalAlignment = **1**`；原始 JSON 实读两份实例
        /// `MonoBehaviour_-69344794122919773.json` / `…_-7031947786318323549.json`，
        /// 同一条读数见 `menu_dump … "AllianceMemberVariant"` 的「对齐=」列）。
        /// **错因**：把这一族与**成员行**那一族（`Alliance Member Entry > {Draft,Ranked} Rating >
        /// Individual rating value` = **`Right`(4)**）当成了同一档 —— **同名不同档**，必须按祖先链取
        /// （铁律 5·c）。此前两份普查（`普查产出_1011/W5_A307_A255_A258.md:110-111` · `W5b_A317.md` §四·2）
        /// 写的「×3 全是 `Right`」**只对成员行那两处成立**。
        /// ⚠️ **今天零可观测差异**（这两格的文案恒传空串，空串下左/中/右三档在画面上同形）——
        /// 订正的是**声明**，不是画面；这条也如实写在这里。</para>
        /// <para>🔴 **2026-10-12（A413）标称 `45f` → `42f`**：原版 `m_fontSize = **42.0**`、
        /// `m_fontSizeBase = 31.3799991607666`、`auto[18.0~42.0]`（上两份 JSON 实读，两颗同值；
        /// `menu_dump` 那一行同报 `字号=42.0 基准=31.38`）。验收：`42 > autoMinPx 18` ⇒ 自适应那条路
        /// **一位不受影响**，改的只是「自适应不跑」那一档会现形的声明值。</para></summary>
        static void RatingRow(SocialView v, Transform root, Geo g, float y, string name, string art, string value)
        {
            var row = Node(root, name, new PxRect(g.RateX1, y, g.RateX2, y + g.RateH));
            v.Rect(row, art, new PxRect(g.RateX1, y, g.RateIconX2, y + g.RateH), "Main Icon", 2, null, true);
            var valR = new PxRect(g.RateIconX2, y, g.RateX2, y + g.RateH);
            var val = Node(row, "Individual rating value", valR);
            var lbRate = v.Text(val, valR, value ?? "", Color.white,
                   "Text", 42f, 3, 18f, alignLeft: true, wrap: true);   // A319 #9（原版 `Left`(1)）· A413（42f）· A258：原版 `折行=1`
            // 🆕 **2026-10-16（A712 阶段 2）**：原版 `{Alliance,Draft} Rating Display/Individual rating value`
            //   = `对齐=Left/**Midline**`（同一份 dump，两颗同档）⇒ `m_VerticalAlignment = 4096`。
            MenuDraw.SetVAlign(lbRate, Label.VAlign.Midline, valR);
        }

        static Transform Node(Transform parent, string name, PxRect r) { return MenuDraw.Node(parent, name, r); }
    }

    // ==================================================================
    //  奖杯格的网格（原版 `TrophiesWindow>Scroll Rect>Item Drawer` 的 `GridLayoutGroup`）
    // ==================================================================

    /// <summary>🆕 2026-10-04（A30）：`Scroll Rect>Item Drawer` 那个 `GridLayoutGroup` 的参数 ——
    /// **逐值实读原始 JSON** `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_3577077230339809443.json`
    /// （挂在 `Item Drawer` GO `-436770581587566429` 上；普查 `社交_联盟与好友页.md:357` 同值）：
    /// `m_CellSize 298×354` · `m_Spacing (x 0, y 15)` · `m_Padding (左10, 右10, 上23, 下0)` ·
    /// `m_Constraint 0`(Flexible) ⇒ 列数由宽度式算（`GridLayoutGroup.cs:184`）。
    /// ⚠️ **Unity** 的 `RectOffset` 序是 **(左,右,上,下)** —— 普查那份表也是这个序，第一格
    /// `376.97,401.51` = 视口左上 + (padLeft 10, padTop 23) 可反证（普查 `:358`）。
    /// <para>**为什么单独一个 `public static` 类**：自检在**编辑器程序集**里要调 `ColumnsFor`
    /// （与 `AllianceMemberRow.ColumnsFor` 同一个理由）；常量也一并放这，免得格位算式两处各写一遍。</para></summary>
    public static class AllianceTrophyGrid
    {
        public const float CellW = 298f, CellH = 354f, GapX = 0f, GapY = 15f;
        public const float PadL = 10f, PadR = 10f, PadT = 23f, PadB = 0f;
        /// <summary>原版那份 `Item Drawer` 出厂的高度（`378.51→755.51` = **377**）= 通式在「1 排」的取值
        /// （`23 + 0 + 1×354 + 0`）—— 两条路对上（普查 `:357`）。</summary>
        public const float H1Row = PadT + PadB + CellH;
        /// <summary>列数式 = `GridLayoutGroup.cs:184`（与 `AllianceMemberRow.ColumnsFor` **同一份原式**、
        /// 只换 cell 参数）：`Max(1, Floor((width − padding.horizontal + spacing.x + 0.001f)/(cellSize.x + spacing.x)))`。
        /// 视口宽 1554.03 ⇒ `Floor(1534.031/298)` = **5 列**。</summary>
        public static int ColumnsFor(float contentW)
        {
            return Mathf.Max(1, Mathf.FloorToInt(
                (contentW - (PadL + PadR) + GapX + 0.001f) / (CellW + GapX)));
        }
    }

    // ==================================================================
    //  成员行（原版 `AllianceMemberEntry`，§A·2·3 的独立根 `6030421012472178610`）
    // ==================================================================

    /// <summary>成员行（750×100 —— 与 `MemberList>Scroll View>Viewport>Content` 的
    /// `GridLayoutGroup` cellSize **逐值相同**，所以直接吃 cell 尺寸即可，不用再推）。
    /// ⚠️ 行里**两处评级圆**用的图不同：`Draft Rating` = **骷髅** · `Ranked Rating` = **段位图标**。</summary>
    public static class AllianceMemberRow
    {
        // ---- 行几何 / 网格（原版 `MemberList>Scroll View>Viewport>Content` 的 `GridLayoutGroup`）----
        /// <summary>`GridLayoutGroup` cellSize **750×100** · spacing (10,**7.22**) · pad **(左0,右0,上9,下75)**
        /// （普查 §B·12 表）—— 与该容器里那个行实例的 rect 逐值对得上
        /// （§A·1 第 326 行 `369.67,502.63→1119.67,602.63` = 视口左上 **+ (padLeft 0, padTop 9)**）。
        /// ⚠️ 收成常量是为了让「内容高」这条算式只有一处（`BuildAll` 里用它写 `MenuScroll.ContentX2`）。
        /// 🔴 **`PadL` 是 0**（2026-10-03 订正：原来写的是 `369.67f + 9f`，把 padTop 也加到了 x 上
        /// ⇒ 每一行**右移 9px**；原版那个 `RectOffset` 是 `(left 0, right 0, top 9, bottom 75)`）。
        /// 🆕 2026-10-04（A40）：`CellGapX`（= `m_Spacing.x = 10`）与 `PadR`（= 0）**补进来** ——
        /// 列数式与两列的 x 步进（`cellW + 10 = 760`）都要它们（原始 JSON 实读
        /// `MonoBehaviour_6868526478606655651.json`：`m_Spacing {'x': 10.0, 'y': 7.21999979019165}` ·
        /// `m_Padding {'Left': 0, 'Right': 0, 'Top': 9, 'Bottom': 75}` · `m_CellSize {'x': 750, 'y': 100}` ·
        /// `m_Constraint 0`）。</summary>
        public const float CellW = 750f, CellH = 100f, CellGapX = 10f, CellGapY = 7.22f;
        public const float PadL = 0f, PadR = 0f, PadT = 9f, PadB = 75f;

        /// <summary>按数据条数逐格建（原版 `AllianceMemberList` 用 `entryPrefab` 逐条 Instantiate）。
        /// 格位按 `GridLayoutGroup`（cell **750×100** · spacing **(10, 7.22)** · pad (0,0,9,75) ·
        /// `m_Constraint 0` Flexible）推。
        /// 🆕 2026-10-03（A25④）：`sc != null` 时格按**滚动偏移之后**的位置摆（`MenuScroll.Shift`）、
        /// 整格滚出视口的**不建**；裁切长在 `AllianceGeneralDetails` 建的 `Viewport` 那颗 `ViewportClip` 上
        /// （A435·丙 起 —— 原来那句「画之前 `SetClip(视口)`、画完清掉」已作废）。
        /// 🆕 2026-10-04（A40）：**改两列**（原版就是两列）—— 见下面那段注释。</summary>
        public static void BuildAll(SocialView v, Transform content, MenuScroll sc)
        {
            for (int i = content.childCount - 1; i >= 0; i--) SocialWindow.DestroySafe(content.GetChild(i).gameObject);
            var all = SocialData.Members;
            int n = all.Count;

            // 🔴 列数：原版那个 `GridLayoutGroup` 的 `m_Constraint = 0`（Flexible）⇒ 列数由**宽度式**算
            //   （UGUI `GridLayoutGroup.cs:184`）。视口宽 **1511**（= `MemberViewportR.W`）·
            //   `cellSize.x 750` · `spacing.x 10` · `padding.horizontal 0`
            //   ⇒ `Max(1, Floor((1511 − 0 + 10 + 0.001)/760))` = **2 列**
            //   （`750 + 10 + 750 = 1510 ≤ 1511`，装得下两列而装不下三列）。
            //   ⚠️ `ColumnsFor` 取「**滚动区的视口**」——与 `FriendsTab.ColumnsFor(ContainerR.W)` 同一口径；
            //   没有滚动区时才退回常量矩形（`Viewport` 是那份真值的唯一来源）。
            //   📌 **2026-10-04（A40）**：这一段原来写「本行的『行数』仍取『每行一条』= 本文件现在的排法」
            //   并把两列**记成待办**（A35 顺手查出）—— 本批照原版改成两列了。
            var vp0 = sc != null ? sc.Viewport : AllianceGeneralDetails.MemberViewportR;
            int cols = ColumnsFor(vp0.W);
            int rows = Mathf.CeilToInt(n / (float)cols);

            // 🔴 内容高写进滚动区（= 原版 `Content` 上 `ContentSizeFitter` 跑出来的高度），**按【排数】算**。
            //   不写 ⇒ `ContentX1 == ContentX2 == Viewport.y1` ⇒ `ClampLo == ClampHi == 0` ⇒ **这一格滚不动**，
            //   而下面「整格滚出视口 ⇒ 不建」那道守卫会把后面的格**彻底藏掉**（同 `BattleLogPopup` 那条）。
            //   算式 = UGUI 那份唯一判据 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/
            //   Layout/GridLayoutGroup.cs:188` 的 MinSize：
            //   `padding.vertical + (cell.y + spacing.y) × 排数 − spacing.y`
            //   ⇒ 这里 `= (PadT + PadB) + 排数×CellH + (排数−1)×CellGapY`。
            //   🔴 **2026-10-04 订正（A35④）**：空表那一支原来写 **`0f`**，原版是 **76.78** ——
            //   `排数 = 0` ⇒ `(9 + 75) + 0 − 7.22 = 76.78`，而原版那份**0 子节点实例**的
            //   `Content.sizeDelta.y` 正是 **76.78**（`AllianceNotMemberVariant>GeneralDetails>…>Content`，
            //   RT `-1825538911737290589`，普查 `社交_联盟与好友页.md:232`）—— **两条独立路对上**。
            //   同一条算式还反证了 `AllianceGeneralDetails.Build` 里 `Content` 的出厂高 `184`：
            //   `排数 = 1` ⇒ `84 + 100 + 0 = 184` ✓。
            //   ⚠️ **空表那一支照样要写**（写 76.78，不是 0）—— 不写 ⇒ 上一次的内容高留在区里 = 静默的脏值。
            float h = PadT + PadB + rows * CellH + (rows - 1) * CellGapY;
            if (sc != null) sc.ContentX2 = vp0.y1 + h;
            // 🔴 **先把 `Content` 摆到位、再建格**（= 原版 `ContentSizeFitter` 把内容容器撑到 MinSize；
            //   格是按「绝对画布坐标」算 `localPosition` 的 ⇒ 建完再挪父节点，整排会跟着偏 ——
            //   同 `FriendsTab.BuildRows` 那条实测教训）。
            content.localPosition = MenuDraw.Local(content.parent, vp0.x1, vp0.y1, vp0.x2, vp0.y1 + h);
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来这里是 `if (sc != null) v.SetClip(vp0);` /
            //    末尾 `v.SetClip(null);` 那一对 —— **删掉了**：裁切状态长在 `AllianceGeneralDetails.Build`
            //    建的 `Viewport` 那颗 `ViewportClip` 上（`v.Nine/Rect/Text/Hit/Cosmetic` 沿父链取它）。
            //    ⛔ 留着 = `SocialPage.Clip` 非空 ⇒ 形参赢 ⇒ 节点一像素都不生效（静默）。
            for (int i = 0; i < n; i++)
            {
                int col = i % cols, row = i / cols;
                // padLeft 0 · padRight 0（原版 pad = (0,0,9,75)）；**x 的步进 = cellW + spacing.x = 760**
                float x = vp0.x1 + PadL + col * (CellW + CellGapX);
                float y = vp0.y1 + PadT + row * (CellH + CellGapY);
                var r = new PxRect(x, y, x + CellW, y + CellH);
                if (sc != null)
                {
                    r = sc.Shift(r);                                   // 内容坐标 → 屏幕坐标（**只做偏移、不裁**）
                    // 🔴 求交那一份 = `MenuDraw.Visible`（**全工程唯一一份**求交；`ClipRect` 是它「顺带夹出
                    //    可见矩形」的那版，别在这儿再写一遍 `Max/Min`）。⚠️ 2026-10-07 更正（铁律 5 / A12①）：
                    //    原文写「= `MenuDraw.ClipRect`（**全工程唯一一份**）」—— 收口后那两句是**同一份**。
                    // 🔴 **2026-10-13（A776 · δ 族）**：`vp0` 这一份自持矩形**不再喂给粗筛**（第二状态源），
                    //    改走父链上的那颗 `Viewport` 节点（`AllianceGeneralDetails.Build` 建的、
                    //    `Hang` 的矩形就是同一个 `g.Viewport`，`sc = TopAligned(g.Viewport, 0f)` ⇒ `vp0 ==
                    //    sc.Viewport == g.Viewport`）。等价推算与残差量级见 `BuildTrophyRows` 那一处
                    //    （节点框 ≡ 自持矩形，只差一趟 float32 往返 ~1e-4px ≪ 0.05px）。
                    //    ⚠️ 两种变体的 `g.Viewport` 不同（act T / act F）⇒ `content` 挂在**各自那棵树**的
                    //    节点下，解析到的是各自那一颗 —— 这正是不再自持的好处。
                    if (!MenuDraw.VisibleAbove(content, r, null)) continue;
                }
                Build(v, content, all[i], r);
            }
        }

        /// <summary>🆕 2026-10-04（A40）：**网格列数** —— 原版 `GridLayoutGroup` 那一式，逐字照抄
        /// `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/GridLayoutGroup.cs:184`：
        /// <c>cellCountX = Max(1, FloorToInt((width − padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)))</c>
        /// —— 三个细节都要照抄：① 分子吃 **`padding.horizontal = 左 + 右`**；② `spacing.x` 是**加在分子上**；
        /// ③ 末尾那个 **`+ 0.001f`**（整除边界上的决胜项）。
        /// <para>**为什么是个 `public static`**：`FriendsTab.ColumnsFor` 的先例 —— 纯函数，自检要拿**若干个内容宽**
        /// 去调它、与照原式独立算出来的字面量逐值对（只看这一屏 1511 一种宽度分不出写法的对错）。
        /// ⚠️ 与 `FriendsTab.ColumnsFor` 是**同一份原式、各自的 cell 参数**（两处的 cell/pad 不同，合并不了；
        /// 真要收成一份得动 `FriendsTab.cs`，不在本批白名单内 —— 已记在报告里）。
        /// ⚠️ `public`（不是 `internal`）：自检在**编辑器程序集**里，跨程序集看不到 `internal`
        /// （⚠️ **2026-10-15 就地订正（W14 · 铁律 5）**：原来这半句指「先例 → `SocialPage.SetClip` 那段注释」——
        /// **那段注释已随 A753 = A744 的「全删」整体删掉** ⇒ 指针悬空。改指**还在的那一份**：
        /// `资料/已知的坑.md` 的「`internal` 在自检里用不了」那一节；它另带一句更该看的 ——
        /// **别拿「有先例」说服自己**，先确认你看到的那个先例到底是哪一个类）。</para></summary>
        public static int ColumnsFor(float contentW)
        {
            return Mathf.Max(1, Mathf.FloorToInt(
                (contentW - (PadL + PadR) + CellGapX + 0.001f) / (CellW + CellGapX)));
        }

        /// <summary>一行（坐标都是**行内相对**，逐值照 §A·2·3 的表 —— 那份表用的根尺寸正好是 750×100）。</summary>
        public static void Build(SocialView v, Transform content, SocialData.Member m, PxRect r)
        {
            var row = MenuDraw.Node(content, "Alliance Member Entry", r);
            v.Nine(row, "40K_dropdown_bg", r, new Vector4(23f, 20f, 23f, 20f), "background", 0,
                   new Color(1f, 0.36f, 0f, 1f));
            // `background/Image`（左侧那条深色竖带）+ 名次数字
            v.Rect(row, null, new PxRect(r.x1 + 2.24f, r.y1 + 2.52f, r.x1 + 47.54f, r.y1 + 97.59f),
                   "Image", 0, new Color(0.481f, 0.182f, 0f, 1f));
            var idxR = new PxRect(r.x1 + 2.52f, r.y1 + 2.56f, r.x1 + 47.04f, r.y1 + 97.34f);
            var lbIdx = v.Text(row, idxR,
                   m.Index.ToString(), Color.white, "member index", 40f, 3, 18f, wrap: false,
                   alignLeft: false);   // A319 #10（原版 `Center`(2)）· 原版 `折行=0`
            // 🆕 **2026-10-16（A712 阶段 2）**：原版 `member index` = `对齐=Center/**Midline**`
            //   （同一份 dump；`Alliance Member Entry` 那一族 ⇒ **同名不同档**，与 `Individual rating value`
            //   那两族一样要**按祖先链各取各的**，见本文件头 `alignLeft` 表那一段）。
            MenuDraw.SetVAlign(lbIdx, Label.VAlign.Midline, idxR);
            // 头像（`Avatar Item Small`：Highlight + Border + Image）—— 立绘走 `CardArt.Cosmetics`
            var av = new PxRect(r.x1 + 50.57f, r.y1 + 11.87f, r.x1 + 149.44f, r.y1 + 114.93f);
            var avn = MenuDraw.Node(row, "Avatar Item Small", av);
            v.Rect(avn, "Player_Avatar_selected", new PxRect(av.x1, av.y1 - 3.53f, av.x2 + 3.34f, av.y2 - 36.37f),
                   "Highlight", 2, null, true);
            var imgC = MenuDraw.Node(avn, "Image Container", new PxRect(av.x1, av.y1, av.x2, av.y1 + 65.69f));
            v.Cosmetic(imgC, m.AvatarArt, new PxRect(av.x1, av.y1 - 2.70f, av.x2, av.y1 + 62.99f), "Image", 2);
            v.Rect(avn, "Player_Profile_Border", new PxRect(av.x1, av.y1 + 6.57f, av.x2, av.y1 + 72.26f),
                   "Border", 2, null, true);
            // 在线状态点
            if (m.Online)
                v.Rect(row, "40K_icon_status_online", new PxRect(r.x1 + 52.90f, r.y1 + 68.33f, r.x1 + 75.46f, r.y1 + 97.35f),
                       "connection status", 2, new Color(0f, 1f, 0.0736f, 1f), true);
            else
                v.Rect(row, "40K_icon_status_offline", new PxRect(r.x1 + 52.90f, r.y1 + 68.33f, r.x1 + 75.46f, r.y1 + 97.35f),
                       "connection status", 2, new Color(0.84f, 0.494f, 0.44f, 1f), true);
            var memNameR = new PxRect(r.x1 + 149.44f, r.y1 + 11.87f, r.x1 + 694.75f, r.y1 + 59.26f);
            var lbMemName = v.Text(row, memNameR, m.Name ?? "",
                   Color.white, "member name", 50f, 3, 18f, wrap: true, alignLeft: true);   // A319 #11（原版 `Left`(1)）· A258：原版 `折行=1`
            // 🆕 **2026-10-16（A712 阶段 2）**：原版 `member name` = `对齐=Left/**Midline**`（同一份 dump）。
            //   ⚠️ 同格的 `member role`（下一句）原版是 `Left/**Middle**` ⇒ **两行不同档**，⛔ 别一刀切。
            MenuDraw.SetVAlign(lbMemName, Label.VAlign.Midline, memNameR);
            v.Text(row, new PxRect(r.x1 + 149.44f, r.y1 + 59.26f, r.x1 + 495.52f, r.y1 + 98.56f), m.Role ?? "",
                   new Color(0.887f, 0.887f, 0.887f, 1f), "member role", 41.45f, 3, 18f, wrap: true,
                   alignLeft: true);   // A319 #12（原版 `Left`(1)）· A258：原版 `折行=1`
            // 两处评级（`VerticalLayoutGroup` 里上下两行，各 46.5 高）
            // 🔴 2026-10-12（A391）：两颗的【折行**不同档**】—— Draft `折行=1` / Ranked `折行=0`
            //    （判据与理由 → `Rating` 的头注释，⛔ 别在这儿抄第二份）。
            Rating(v, row, r, 3.36f, "Draft Rating", "40k_battle_Win_Skull", m.DraftRating, rightPivot: false, wrap: true);
            Rating(v, row, r, 49.86f, "Ranked Rating", "40k_UI_icon_ranked_Skirmish", m.RankedRating, rightPivot: true, wrap: false);
            v.Hit(row, "Hit", r, 3, () =>
                SocialPage.Say("点成员那一行：原版是 `AllianceMemberEntry` 的 `button`（开成员选项弹窗 "
                             + "`AllianceMemberOptionsPopup`，8 个钮全要服务器 —— 判据 → `多人界面_入口与调用.md` §③）。"));
        }

        /// <summary>一处评级：`Main Icon` + `Individual rating value`（**右对齐**）。
        /// ⚠️ 上下两块的 `Main Icon` 的 pivot **不同**（上 `(.5,.5)`、下 `(1,.5)` ⇒ 下那块是贴右的），
        /// 逐值照 §A·2·3 第 474/478 行。
        /// <para>🔴 **2026-10-12（A319 #13）：右对齐要【自己接一下】** —— `SocialView.Text` 那个口
        /// 只有「左对齐 / 居中」两档（见 `Shell/SocialWindow.cs` 的 `alignLeft`），原版这里却是
        /// **`Right`(4)**（`Alliance Member Entry > {Draft,Ranked} Rating > Individual rating value`，
        /// `m_HorizontalAlignment = 4`；原始 JSON 实读 `MonoBehaviour_-6736837615115902813.json` /
        /// `…_5962791513722300338.json`）⇒ 照先例在 `Text(...)` **之后**另接 `MenuDraw.AlignRight(lb, tr)`
        /// （先例 = `Shell/ProfileTab.cs` 的 `Consecutive login days`：同样传 `alignLeft: false`
        /// 再 `AlignRight`）。⚠️ `alignLeft: false` 那一句**保留**（两个动作各管一半：
        /// 前者不调 `AlignLeft`，后者按矩形右缘摆）—— ⛔ 别只留一句。</para>
        /// <para>🔴 **2026-10-12（A391）：两颗的【折行】不是同一档 —— 新增 `wrap` 形参**。
        /// 原版：`Draft Rating/Individual rating value` = **`折行=1`** · `Ranked Rating/…` = **`折行=0`**
        /// （判据 = 本件亲跑 `python 工具/menu_dump.py bundle_menus_assets_all --rt 6030421012472178610
        /// --depth 6 --md`，两行的「折行=」列逐字 `折行=1` / `折行=0`；原始 JSON 实读、并**核过各属哪一行**
        /// —— `Draft Rating` 的三个子件里有 `RectTransform_-3889825537448877917` ⇒ MB
        /// `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-6736837615115902813.json`
        /// （`m_TextWrappingMode = 1`）；`Ranked Rating` 的子件里有 `RectTransform_-5810472960067917902`
        /// ⇒ MB `…/MonoBehaviour_5962791513722300338.json`（`= 0`）。两份的
        /// `m_HorizontalAlignment = 4`(Right) / `m_VerticalAlignment = 4096`(Midline) 同值）。
        /// 🔴 **原来两个调用点共用一句 `wrap: true`，那句 inline 注释写的「A258：原版 `折行=1`」
        /// 只对 Draft 那一颗成立**（Ranked 那一颗是**真偏离**）。**错因**：A258 按「这一族的
        /// `Individual rating value` 都长一样」推的，而原版这一族里**同名不同档**
        /// —— 同一份 dump 里 `member name` / `member role` 是 `1`、`member index` 是 `0`
        /// （铁律 5·c「一个值 ≠ 全部情况」；同一坑的另一面见本文件 `Toggle` 上方那张 13 处对齐表）。
        /// ⚠️ **`wrap` 与 `rightPivot` 是两个独立的原版属性**（这里只是恰好一起变）⇒ 各传各的，
        /// ⛔ **别拿 `rightPivot` 去推折行档**（那会把两件事绑死、下一个人看不出它们是两件）。</para>
        /// <para>🔴 **顺序（H35 §三·2 的先例，⛔ 别改）**：折行必须在**对齐之前**落定 ——
        /// `SocialPage.Text` 内部就是 `TextBox` → `SetWrapping(false)` →（`alignLeft` 那一档）
        /// （`Shell/SocialWindow.cs` 那两句的次序注释），而本函数的 `MenuDraw.AlignRight(lb, tr)`
        /// 排在 `v.Text(...)` **之后** ⇒ 「先折行、后对齐」天然成立
        /// （`Label.AlignRightOn` 是按**当时的 `WorldW`** 反推位置的 ⇒ 反过来写会偏 `(旧宽−新宽)/2`）。
        /// **加 `wrap` 形参不改这个次序**：两颗仍然是 `Text(...)` 之后再 `AlignRight`。</para>
        /// ⚠️ **今天这一改的可见差**：Ranked 那一格原版矩形 **130** 宽、值传的是名字里的 `"32"`（自检夹具）
        /// 或数据 —— 短串下折行档**画面上同形**；改的是**声明**（真值档），并让断言能咬住它
        /// （`Label.WrappingMode` 直接报原版原文的 `0`/`1`；断言**待接线** —— 落点与现成文案写在
        /// `资料/普查产出_1012/H39_口径与折行文案.md` §五·1，⛔ 本件改不了 `Editor/*`）。</summary>
        static void Rating(SocialView v, Transform row, PxRect r, float dy, string name, string art,
                           string value, bool rightPivot, bool wrap)
        {
            float x1 = rightPivot ? r.x1 + 610.18f : r.x1 + 675.18f;
            v.Rect(row, art, new PxRect(x1, r.y1 + dy, x1 + 65f, r.y1 + dy + 46.5f), name + "/Main Icon", 2, null, true);
            var tr = new PxRect(r.x1 + 610.18f, r.y1 + dy, r.x1 + 740.18f, r.y1 + dy + 46.5f);
            var lb = v.Text(row, tr, value ?? "",
                            Color.white, name + "/Individual rating value", 42f, 3, 18f, alignLeft: false, wrap: wrap);
            if (lb != null) MenuDraw.AlignRight(lb, tr);   // A319 #13：原版 `Right`(4)
            // 🆕 **2026-10-16（A712 阶段 2）**：原版 `{Draft,Ranked} Rating/Individual rating value`
            //   = `对齐=Right/**Midline**`（两颗同档；判据 = `AllianceMemberVariant --depth 20` 那一份 dump ——
            //   ⚠️ 与**上面页内**那两颗同名的 `{Alliance,Draft} Rating Display/Individual rating value`
            //   （`Left/Midline`）是**同名不同档**，见本文件头那张表：横向取 `Right`、纵向**两颗都是 `Midline`**）。
            MenuDraw.SetVAlign(lb, Label.VAlign.Midline, tr);
        }
    }
}
