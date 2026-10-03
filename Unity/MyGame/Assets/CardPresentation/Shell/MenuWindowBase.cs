// MenuWindowBase.cs — 主菜单「子菜单窗」的**公共外壳**（左栏 + 内容区 + 页签宿主 + 绘图助手）
//
// ============================ 为什么要抽这一层（2026-09-23） ============================
// `Rewards Base Submenu Variant`（日常/锻造厂/战役）与 `Shop Menu Variant`（商店）是**同一个壳**：
// **`Content Area` 的矩形实测完全一样**（`167.17,70.94 → 1920.01,1080.00`）· `Background` 同一套双色渐变 ·
// `Tab Buttons` 同一条左栏（165 宽 · `VLG padTop 120` · 每个键 180 高 · 同一张选中底图与名字条）。
// 商店那一件开工时要再写一遍这些 —— 而 CLAUDE.md §三写着「**两处写同一条规则 = 迟早不一致**」
// ⇒ 收口到这里，`RewardsWindow` 与 `ShopWindow` 都继承它。
//
// ⚠️ **这是一次纯抽取**：常量与算法**一字未改**（出处仍是 `资料/日常_原版规格.md` §一/§二 与
// `资料/主菜单_原版规格.md`），抽完靠 `RewardsScene` 那 265 条断言证明行为没变。
//
// 🔴 **两个窗口的差别只在三处**（各自填，别在基类里写死）：
//   ① `Buttons`（左栏键的图标/文案/字号）—— 日常那套 4 个键、商店那套是自己的页签；
//   ② 窗口参数（`type` / `windowsPlacement` / `closeOnESC`）—— **实测值逐窗不同**（见各自 `Create`）；
//   ③ 红点偏置 `TabBtnSpec.BadgeDy`（日常第 4 键是 `+47.9`、其余 `−27.2`）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>主菜单子菜单窗的公共外壳。**不要直接实例化**（`BuildShell` 要子类给键表）。</summary>
    public abstract class MainMenuSubmenuWindow : GameWindowWithTabs
    {
        // ============================================================ 外壳常量（原版实测）

        /// <summary>`Content Area`：`a=(0,0)-(1,1) pos=(83.59,-35.47) sz=(-167.2,-70.94)`
        /// ⇒ **x 167.17..1920.01 · y 70.94..1080.00**（`Rewards Base Submenu Variant` 与 `Shop Menu Variant` **实测同值**）。</summary>
        public const float ContentL = 167.17f, ContentT = 70.94f, ContentR = 1920.01f, ContentB = 1080f;

        /// <summary>`Tab Buttons`：`a=(0,0)-(0,1) sz=(165,0)` ⇒ x 167.17..332.17 · 与 Content Area 同高。</summary>
        public const float BarW = 165f;

        /// <summary>`VerticalLayoutGroup`：padTop **120** · spacing **0** · UpperCenter · 子高 180。</summary>
        public const float TabBtnH = 180f;

        /// <summary>左栏**第一个键距 `Content Area` 顶边**的距离 —— 原版**逐窗不同**：
        /// 奖励窗 / 商店 = **120** · 🆕 **收藏窗（`Collection Menu Variant`）= 30**
        /// （它的 `Tab Buttons` 是 `VerticalLayoutGroup` align=1 UpperCenter · `padTop=30`、每键 165×180）。
        /// ⇒ 做成可覆写：子类只改这一个数，**别再写一套左栏**。</summary>
        protected virtual float BarPadTop { get { return 120f; } }

        /// <summary>`Tab Buttons/Shadow`：奖励窗/商店实测 `sz=(-117.4,0)` ⇒ 宽 **47.64**、贴左栏左边（实测 x 167.18..214.81）。
        /// 🔴 **2026-09-27 改成可覆写**（原来是个 `const`）：社交窗那条 `Shadow` 实测 `sz=(165,0)` +
        /// `pivot(.5,.5) pos(82.5,−480)` ⇒ **高 0、原版静态就看不见**（判据 → `资料/普查产出_0927/社交_联盟与好友页.md`
        /// §A·1 第 45 行 / §B·15）。照 47.64 画它就**凭空多出一条线**来 ⇒ 社交窗覆写成 **0**，见
        /// `SocialWindow.BarShadowW`。⚠️ 这就是铁律 5·c 那条：**一个值 ≠ 全部情况**。
        /// ⚠️ 全工程只有 `BuildBar` 一处用它（`grep BarShadowW` 只有两行），改它不是改公共契约。</summary>
        protected virtual float BarShadowW { get { return 47.64f; } }

        /// <summary>页签页容器的实测矩形（`Tabs` 与 `Content Area` 同矩形）。</summary>
        public const float TabL = 166.69f, TabT = 69.20f, TabR = 1920f, TabB = 1080f;

        /// <summary>左栏底图 `40k_main_tab_background` 原版是 **Simple**（不是 Sliced），色白、ppuMul 1。</summary>
        public const string ArtBarBg = "40k_main_tab_background";
        public const string ArtBarShadow = "40k_main_tab_shadow";

        /// <summary>选中/未选中**共用同一张底图**：`40k_main_bt_selected BW` 纯红 #FF0000。
        /// ⚠️ 工程里的切片名是**下划线**版（导入器把空格换成下划线）：`40k_main_bt_selected_BW`。</summary>
        public const string ArtSelHighlight = "40k_main_bt_selected_BW";
        public const string ArtNametag = "40k_main_bt_nametag";

        /// <summary>`Content Area/Background` 的 `UIGradient`：c1 **#390503** · c2 **#0C0004** · angle **82**。</summary>
        public static readonly Color GradC1 = new Color(0x39 / 255f, 0x05 / 255f, 0x03 / 255f);
        public static readonly Color GradC2 = new Color(0x0C / 255f, 0.0f, 0x04 / 255f);

        // ⚠️ 分层用**渲染队列**、不用 z（`ImageQuad` 全是透明队列，按到相机的 3D 距离排序 —— 屏幕中间的
        //    反而更近）。**同一个队列 + z 都是 0 ⇒ 谁盖谁完全不确定**（2026-09-22 踩过）⇒ 每层差 1 都行。
        // 🔴 **数值与「页」这一档绑定**：这一档最高 3014；`ForgeTab`/`CampaignTab` 那些**页**用到 3027/3064；
        //    **弹窗必须再高一档**（`PromptPopup` 3140+ · `CampaignRewardWindow` 3110+）。
        //    `RewardsScene` 里有一条断言钉住「弹窗 > 页 > 窗」这个次序。
        public const int QPanel = 3005, QContent = 3010, QText = 3011, QOverlay = 3014;

        /// <summary>**裁切边界**（画布像素 · 左上原点）。非空时 `Rect` 把越界部分**截掉**、
        /// 并把 uv 跟着截（`ImageQuad.SetUvRect`）—— 这就是原版 `RectMask2D` 的等效物。
        /// 谁用它：滚动区在画内容**之前**设一次、画完清掉（`ForgeTab.BuildRewardCells` 那种）。
        /// 🆕 **2026-10-03：横纵两轴都裁了** —— 原来只裁 x，纵向滚动区（商店 `Packs Scroll View`）
        ///    接上来时才暴露出「纵向裁不住」。
        /// 🆕 **2026-10-03（本批）：九宫格与点击区也吃 `Clip` 了**（此前两处**都没接**）——
        ///   · `Nine()`（本类的包装 → `MenuDraw.Nine`）：整棵树仍按**原矩形**建（角块位置是照整块算的），
        ///     建完**逐子块**截到框内；根节点位置不动（原版 `RectMask2D` 也只裁渲染）；
        ///   · `AddHit()`（→ `MenuDraw.Hit`）：**视口外 → 连节点一起不建**；压在视口边上 → 命中区**截到视口内**
        ///     （判据 = 原版 `RectMask2D` 的**射线那一面**，出处见 `MenuDraw.ClipRect` 的注释）。
        /// ✅ **2026-10-04：文字也吃 `Clip` 了**（原记的那条缺口「部分越界的字仍按原样画」**已补**）——
        ///   `Text` / `TextBox` 现在把**渲染网格**裁到框内（`MenuDraw.ClipText`：TMP 逐字夹顶点 + 改 uv，
        ///   点阵兜底那条按同一个四边形处理）。判据 = 原版 `RectMask2D` 对文字与图**一视同仁**。
        ///   ⚠️ 文字**不能在建完之后再改**（`SetText`/`SetGlyphHeight`/`SetAutoFitBox` 会把 mesh 重算回去）——
        ///   本类两个包装都在**定完字号之后**才裁。</summary>
        public PxRect? Clip;

        /// <summary>🆕 **2026-10-04：软边**（原版 `RectMask2D.m_Softness`，**画布像素**：`x` 管左右两条边、
        /// `y` 管上下两条边；`(0,0)` = 硬边 = 本层原来那套行为）。**与 `Clip` 配对使用**：
        /// `Clip` 画内容前设一次、画完清掉，本字段同理（谁设 `Clip` 谁负责把软边一起设对）。
        ///
        /// 🔴 **原版真值（逐处实读 `assets_full` 的 `RectMask2D` JSON · 字段名 `m_Softness`）**，本壳用到的几处：
        ///   · 商店三页 `Viewport`（`Card Shop Tab` / `Item Shop Tab` / `Daily Shop Tab` /
        ///     `Item Shop Tab No Automatic Ordering` / `Card Shop VIP Tab Variant` /
        ///     `Shop Menu Variant/Content Area/Tabs/Shop Tab` 的 `Packs Scroll View/Viewport`）= **(0,25)**
        ///   · 锻造厂阵营条（`Forge Tab/Forge Army Selector/Viewport` 与
        ///     `Rewards Base Submenu Variant/…/Forge Tab/Forge Army Selector/Viewport`）= **(42,0)**
        ///   · 练习窗阵营条（`Practice Mode Menu/Deck Selector/Army Selector/Viewport`）= **(0,50)**；
        ///     同一窗的卡组列表（`…/Deck Buttons/Decks Scroll view/Viewport`）= **(0,23)**
        ///   · 玩家档案的 `Avatar Tab` / `Title Tab` 两个 `Item Display Panel/Scroll Rect` = **(0,50)**；
        ///     `Trophies Tab/Scroll/Viewport` 与 `Ranking Tab/AllFactions/scroll rect/viewport` = **(0,0)**
        ///   · 聊天（`Chat Tab/Viewport`，在 `bundle_mainmenualwaysloaded_assets_all`）= **(0,22)**
        ///   · 奖励窗 `Reward Window/Content/Scroll View/Viewport` = **(200,0)** ·
        ///     战役奖励窗 `Campaign Reward Window/Content/Scroll View/Viewport` = **(200,0)** ·
        ///     每日连击窗 `Daily Streak Popup/…/Rewards Scroll View/Viewport` = **(89,0)** ·
        ///     每日奖励窗 `Daily Reward Popup/Tracks/Rewards Scroll View/Viewport` = **(0,0)**
        ///   · 锻造奖励轨道（`…/Forge Tab/Rewards Scroll View/Viewport`）与战役轨道（`…/Campaign Track/Viewport`）
        ///     = **(0,0)**（硬边，但 `m_Padding` 是 (10,0,0,0) —— **我们没建模 padding**，见报告）
        ///   · 收藏窗各页 `Scroll View/Viewport`、选卡组窗 `Deck Scroll View/Viewport` = **(0,0)**
        ///   · 排行榜四棵的 `Content/Scroll View/Viewport` = (0,0)，而 `Ranking Display/Content/Army Selector/Viewport`
        ///     = **(42,0)**
        ///   ⚠️ 全库共 **222** 个 `RectMask2D`（菜单 150 / 通用弹窗 5 / 战场场景 65 / 主菜单 1 …），
        ///     这里只列了本壳用得上的那些；查询脚本与逐条清单见交接报告。
        /// 🔴 **机制与代价** → `MenuDraw.ApplySoftEdges` 的注释（几何等效：按渐隐带内沿切开 + 逐顶点 alpha 斜坡）。</summary>
        public Vector2 ClipSoftness;

        /// <summary>🆕 **2026-10-04（A9/A15 尾巴）：原版 `RectMask2D.m_Padding`** —— 与 `Clip` 配对，
        /// 但**只改「点不点得到」，不改「画到哪儿」**（判据 = 本地 UGUI `RectMask2D.cs:178-185`：
        /// 那个字段全文件只用在 `IsRaycastLocationValid` 一处，渲染那一面压根不读它）。
        /// 形状 = UGUI 的 `(x=Left, y=Bottom, z=Right, w=Top)`（画布像素）；**正值缩小、负值扩大**
        /// —— 完整判据、符号旁证与逐处真值表 → `MenuDraw.PaddedHitRect` 上面那一段。
        ///
        /// **本层怎么用**：谁设 `Clip` 谁顺手把它设对（与 `ClipSoftness` 同一条纪律），
        /// `AddHit` 会把两样一起转给 `MenuDraw.Hit`；`Rect`/`Nine`/`Text` **不吃它**（那是渲染）。
        /// ⚠️ `ForgeTab` / `CampaignTab` 那两份自己的 `AddHit` 副本**还没转发**（同 `Clip` 那条）。</summary>
        public Vector4 ClipPad;

        // ============================================================ 左栏键的规格

        /// <summary>一个左栏键：图标名 · 文案 · 原版字号(px) · autosize 区间 · 实例 ID · 红点纵向偏置。</summary>
        public struct TabBtnSpec
        {
            public readonly string Art, Label, InstId;
            public readonly float FontPx, AutoMin, AutoMax;
            /// <summary>红点相对键中心的 y 偏置（原版 `Badge Highlight` 的 `pos.y` 取负）。
            /// 日常那四个键里**第 4 键是 `+47.9`、其余是 `−27.2`** —— 逐键不同 ⇒ 放进规格里，别在循环里写死 `idx == 3`。</summary>
            public readonly float BadgeDy;
            public TabBtnSpec(string art, string label, float fontPx, float autoMin, float autoMax, string instId,
                              float badgeDy = -27.2f)
            { Art = art; Label = label; FontPx = fontPx; AutoMin = autoMin; AutoMax = autoMax;
              InstId = instId; BadgeDy = badgeDy; }
        }

        public readonly List<string> MissingArt = new List<string>();

        /// <summary>左栏建出来的东西（子类自己保存它要用的那几样）。
        /// 🔴 **必须是 `class` 不能是 `struct`** —— 它要在 `BuildShell → BuildBar` 之间**被填充**，
        ///    而 `struct` 是按值传的：传进去的是副本，填完回来**还是空的**
        ///    （2026-09-23 实测：`_btnHighlight` 全 null ⇒ `RefreshHighlights` 抛 NRE）。</summary>
        public class BarResult
        {
            public Transform bar, holder, tabs;
            public TabButtons buttons;
            public Transform[] roots;
            public ImageQuad[] highlight, badge;
            /// <summary>🆕 2026-10-03：**高亮层的根节点**。为什么单开一份：`Highlight` 现在是**九宫格**
            /// （`Image.Type = Sliced` + `ppuMultiplier 0.92`），一棵树里有 **9 个 quad** ——
            /// 只把其中**一个** `SetActive(false)` 会留下另外 8 块（原来的 `highlight[i].gameObject` 就是那个坑）。
            /// **切亮哪一个键，一律用这个根节点。**</summary>
            public GameObject[] highlightRoot;
        }

        // ⚠️ `tabButtons` / `tabHolder` **不在这里声明** —— `GameWindowWithTabs` 已经有了；
        //    这里再声明一次会**遮蔽**基类字段（`CS0108`），两处指向不同对象 = 迟早不一致。

        // ============================================================ 坐标与绘图工具
        //
        // 🔴 像素→世界的换算**只有 `LayoutSpace` 那一份**（`LayoutSpace.RectCenter` / `Px`）。
        //    这里只做「按像素矩形摆一张图 / 一段字」的包装。

        public static Transform New(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        public static void DestroySafe(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { DestroyImmediate(go); return; }
#endif
            Object.Destroy(go);
        }

        /// <summary>取图（`CardArt.MenuUi` 会在 `ui_menu/ → ui_deck/ → ui/` 三批里兜底）。取不到记进 `MissingArt`。
        /// 🔴 `CardArt.MenuUi` **不做「空格 → 下划线」转换** —— 传的必须是**导入后的文件名**。</summary>
        public Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        /// <summary>原版像素矩形中心 → **相对 `parent` 的局部坐标**。</summary>
        public static Vector3 Local(Transform parent, float x1, float y1, float x2, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2) - (parent != null ? parent.position : Vector3.zero);

        /// <summary>原版像素**点** → 相对 `parent` 的局部坐标。
        /// 🔴 `ImageQuad.Create` / `Label.Create` 的 `pos` 都是 **localPosition** ——
        ///    直接喂 `LayoutSpace.FromPixel(...)`/`RectCenter(...)`（世界坐标）在父节点有偏移时会**双倍错位**。
        ///    第一版左栏四个图标、内容区渐变背景、进度条九宫格全栽在这上面，而且**断言全绿**。</summary>
        public static Vector3 Local(Transform parent, float xPx, float yPx)
            => LayoutSpace.FromPixel(xPx, yPx) - (parent != null ? parent.position : Vector3.zero);

        /// <summary>建一个**有矩形语义的容器节点**（摆在原版那个矩形的中心）。
        /// 原版每个节点都有自己的 rect；我们的世界空间里「容器」自己不带渲染，但**位置要摆对** ——
        /// 否则自检量不到、将来做点击/滚动也会算错。</summary>
        public static Transform Node(Transform parent, string name, PxRect r)
        {
            var t = New(parent, name);
            t.localPosition = Local(parent, r.x1, r.y1, r.x2, r.y2);
            return t;
        }

        /// <summary>按**原版像素矩形**摆一张图。`art == null` = 纯色块。
        /// `keepAspect` = 原版 `Image.m_PreserveAspect`：**按图自身的宽高比放进框、居中**（不拉伸）。
        /// ⚠️ `ImageQuad.Create` 的 `pos` 是 **localPosition** ⇒ 这里要减掉父节点的世界位置。
        /// 🔴 `keepAspect` 曾经**收了不用**（永远走拉伸）：三张任务卡的 `Collect` 按钮原版是
        /// `Simple + PreserveAspect=1`（`40K_button` 489×107），骷髅卡那个框 187.47×80.49
        /// ⇒ 原版画出来只有 **187.47×41.02**，我们画满了 80.49。几处按钮的 `m_Pivot` 实测都是 **(.5,.5)**。</summary>
        public ImageQuad Rect(Transform parent, string art, float x1, float x2, float y1, float y2, string name,
                              int q, Color? tint = null, bool keepAspect = false)
        {
            var tex = art == null ? CardArt.Solid() : Art(art);
            // 🔴 **裁切那一段只有 `MenuDraw.Rect` 一份**（2026-09-24 收口）—— 这里转调它，
            //    别把「等比放进框 + 裁切 + 截 uv」再抄一遍（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
            // 🆕 2026-10-04：把 **`ClipSoftness`** 一起转下去（软边只在本层有 —— 各页拿不到）。
            return MenuDraw.Rect(parent, tex, new PxRect(x1, y1, x2, y2), name, q, tint,
                                 keepAspect && art != null, Clip, ClipSoftness);   // ⚠️ 纯色块不做等比（同旧行为）
        }

        /// <summary>同上，直接吃一个 `PxRect`（四处分写 `.x1,.x2,.y1,.y2` 容易抄错 ⇒ 收口成一个重载）。</summary>
        public ImageQuad Rect(Transform parent, string art, PxRect r, string name, int q,
                              Color? tint = null, bool keepAspect = false)
            => Rect(parent, art, r.x1, r.x2, r.y1, r.y2, name, q, tint, keepAspect);

        /// <summary>🆕 **2026-10-03**：原版 `Image.Type = Sliced` 的**九宫格**，和 `Rect` 一样**自动吃 `Clip`**。
        /// 🔴 为什么必须有这个包装：`MenuDraw.Nine` 的 `clip` 参数是**显式传**的（照 `Rect` 的形状），
        ///    而**只有本层有 `Clip`**（各页拿不到）⇒ 页里画九宫格时走这一份，别自己再传一次 `Clip`
        ///    （漏传 = 那条九宫格**画到视口外**，而且**静默**：断言量的是节点在不在、量不到「画多出去了」）。
        /// <para>`texW/texH` = **UV 切分用的贴图尺寸**（原版 `m_Border` 是按贴图像素量的）；
        /// 不传（0）= 用**这张图自己的** `width/height`。⚠️ 有的件原版 `m_Rect` 与贴图尺寸不同、
        /// 必须传给原版那个数（例：`LeaderboardRow` 的 `BgBorder` 配 32×32）—— 那种就照旧显式传。</para>
        /// <para>⚠️ `borderOutPx` = **画出来的角块长**（原版 `m_PixelsPerUnitMultiplier` 会缩放它，见 `MenuDraw.Nine`）。</para>
        /// <para>返回**整棵树的根**（不是单个 quad —— `SetHighlight` 那种整棵开关的用法见 `BarResult.highlightRoot`）。</para></summary>
        public GameObject Nine(Transform parent, string art, PxRect r, Vector4 border, int q,
                               Color? tint = null, bool fillCenter = true, string name = "Nine",
                               Vector4? borderOutPx = null, float texW = 0f, float texH = 0f)
        {
            var tex = Art(art);
            if (tex == null) return null;
            return MenuDraw.Nine(parent, tex, r, border,
                                 texW > 0f ? texW : tex.width, texH > 0f ? texH : tex.height,
                                 q, tint, fillCenter, name, borderOutPx, Clip, ClipSoftness);
        }

        /// <summary>一个**透明点击区**（整块矩形）+ `WindowButton`，返回那个节点。
        /// 原版这一层就是按钮自己的 `RectTransform`；我们这套没有 uGUI 事件 ⇒ 单独一个透明 quad 当命中区
        /// —— **`PointerLayer` 扫的就是它**（`GetComponentInChildren<ImageQuad>()` 拿矩形）。
        /// 🔴 2026-09-23：`ForgeTab` / `CampaignTab` 原来**各写了一遍**，按 CLAUDE.md §三 收口到这里。
        /// 🔴 **2026-09-24 再收口**：活动窗/搜索弹窗也要用 ⇒ 实现挪到 `MenuDraw.Hit`（共用的画图层），这里**转调**。
        /// 🔴 **2026-10-03（本批）`Clip` 生效时**：视口外的点击区**不建**（返回 null）、压在边上的**截到视口内**
        /// —— 判据 = 原版 `RectMask2D` 的射线那一面（`MenuDraw.ClipRect` 的注释里有出处）。
        /// ⚠️ 老注释那句「`Clip` 生效时视口外的点击区不会被建」**当时是写错的**（代码从没做这件事，
        /// `项目任务.md` §三 第 29 条 A9 记着）—— **现在这句才成立**。
        /// ⚠️ `ForgeTab` / `CampaignTab` **各有一份自己的 `AddHit` 副本**（不是转调本方法）⇒ 那两页
        /// **没吃到这道守卫**；把它们改成转调这里（或给 `MenuDraw.Hit` 传 `Clip`）即可，一行的事。
        /// 🆕 **2026-10-04（A9/A15 尾巴）：`ClipPad` 也转发下去了** —— 原版 `RectMask2D.m_Padding`
        /// **只改射线那一面**（判据/符号约定见 `MenuDraw.PaddedHitRect` 上面那一段），
        /// 所以渲染那一份（`Rect`/`Nine`/`Text`）**照旧不吃它**，只有这里这条命中区路吃。</summary>
        public Transform AddHit(Transform parent, string name, PxRect r, int q, System.Action onClick,
                                ImageQuad target = null, string art = null,
                                string hoverArt = null, string pressedArt = null)
        {
            return MenuDraw.Hit(parent, name, r, q, onClick, target, art, hoverArt, pressedArt, Clip, ClipPad);
        }

        /// <summary>按像素矩形摆一段文字（居中）。`fontPx` = **原版 TMP 的 `m_fontSize`**（画布像素）
        /// —— 内部走 `Label.SetGlyphHeight(px/108)`；🔴 **别用 `SetFontSize(px/108)`**，那会大 2.7 倍。</summary>
        public Label Text(Transform parent, string text, float x1, float x2, float y1, float y2, int scale,
                          Color color, string name, float fontPx = 0f)
        {
            // **裁切**：① 「**整块**在视口外 ⇒ 不建」—— 收口到 `MenuDraw.Visible`（A25④ 那四处内联的唯一实现）；
            //         ② 🆕 2026-10-04：**压在视口边缘的那几个字要切**（`MenuDraw.ClipText` 把渲染网格裁到框内）。
            // 🔴 此前这里只做 ①，注释里写着「部分越界的字仍按原样画 —— 这条缺口写在 `Clip` 字段的注释里」。
            //    现在那条缺口**补掉了**（判据 = 原版 `RectMask2D` 对文字与图一视同仁）。
            // ⚠️ 裁的时机必须在**定完字号之后**（`SetGlyphHeight` 会重排 mesh），所以放在最后一步。
            if (!MenuDraw.Visible(new PxRect(x1, y1, x2, y2), Clip)) return null;
            var lb = Label.Create(parent, text, Local(parent, x1, y1, x2, y2), scale, color,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(QText);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            if (Clip.HasValue) MenuDraw.ClipText(lb, Clip, ClipSoftness);
            return lb;
        }

        /// <summary>**按原版 TMP 的规矩**摆一段文字：**限宽换行**（`m_TextWrappingMode = 1`）+ 可选**自适应字号**。
        /// 🔴 **为什么必须有这个包装**：`Label` 内部把换行模式写死成 `NoWrap` ⇒
        /// 每日任务行那句 `Deal 500 damage to enemy units`（35px）直接**冲出卡外**，**61 条断言一条都没报**。
        /// ⚠️ `SetAutoFitBox` 内部按**比例**算 min/max（单位同 `fontSize`，不是世界单位 —— 直接填 px/108 会把字号压到 0.3px）。</summary>
        public Label TextBox(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                             float autoMinPx = 0f)
        {
            // 🔴 **2026-09-24**：实现挪到 `MenuDraw.TextBox`（活动窗那几扇也要用），这里**转调**；
            //    只有本层有 `Clip`，所以那道「整块在视口外就不建」的判断留在这儿。
            // 🆕 2026-10-04：改成 `MenuDraw.Visible`（A25④ 收口）+ **部分越界也裁**（`ClipText`，
            //    在 `TextBox` 的 autosize 定完字号**之后**才裁 —— 那一步会重排 mesh）。
            if (!MenuDraw.Visible(r, Clip)) return null;
            var lb = MenuDraw.TextBox(parent, r, text, color, name, fontPx, autoMinPx, QText);
            if (lb != null && Clip.HasValue) MenuDraw.ClipText(lb, Clip, ClipSoftness);
            return lb;
        }

        // ============================================================ 外壳：Content Area + 左栏 + Tabs

        /// <summary>建 `Content Area`（+ 双色渐变 `Background`）、左栏、`Tabs` 三层。
        /// 子类在 `Build()` 里调它一次，然后往返回的 `tabs` 里塞自己的页。</summary>
        protected BarResult BuildShell(Transform root, TabBtnSpec[] specs, string btnPrefix, string contentTag)
        {
            DestroyChildren(root);
            MissingArt.Clear();

            var areaRect = new PxRect(ContentL, ContentT, ContentR, ContentB);

            // ---- Content Area（页签内容的父）----
            var area = Node(root, "Content Area", areaRect);

            // `Background`：原版是 `Image(sprite=null)` + **`UIGradient` 双色**（c1 #390503 · c2 #0C0004 · angle 82）
            // ⇒ 走 `CardArt.Gradient`（主菜单整屏背景是同一套做法）。
            // ⚠️ **不能先 `Rect()` 再 `SetTexture`** —— `SetTexture` 会把 `_aspect` 改成贴图自己的
            //    宽高比（128×128 = 1.0），把 `Rect` 刚设好的 1752.83/1009.06 冲掉。
            {
                float w = areaRect.W, h = areaRect.H;
                var q = ImageQuad.Create(area, CardArt.Gradient(GradC1, GradC2, 82f),
                                         Local(area, areaRect.x1, areaRect.y1, areaRect.x2, areaRect.y2),
                                         LayoutSpace.Px(h), new Vector2(0.5f, 0.5f), "Background");
                if (q != null) { q.SetAspect(w / h); q.SetRenderQueue(QPanel); }
            }

            var res = new BarResult();
            res.bar = BuildBar(area, specs, res, btnPrefix);

            // ---- 页签页容器（`Tabs` 与 `Content Area` 同矩形，实证）----
            var tabsRect = new PxRect(ContentL, ContentT, ContentR, ContentB);
            res.tabs = Node(area, "Tabs", tabsRect);
            tabHolder = res.tabs;
            res.tabs.name = contentTag;

            // ---- `Shadow (1)`：出厂 active=false ⇒ **不建**（照 `MainMenuRuntime` 那条纪律③）----
            return res;
        }

        /// <summary>清空一个节点的全部子件（**批处理下要用 `DestroyImmediate`** —— 没有帧循环，
        /// `Destroy` 不会立刻消失，会和新建的叠在一起）。
        /// 🔴 2026-09-27：实现挪到 `MenuDraw.ClearChildren`（聊天窗不是本类的子类也要用），这里**转调**。</summary>
        public void DestroyChildren(Transform root) { MenuDraw.ClearChildren(root); }

        /// <summary>左栏：底图 + 阴影 + 每个键（`Highlight` / `Icon` / 名字条 + 文案 / `Badge`）。</summary>
        Transform BuildBar(Transform area, TabBtnSpec[] specs, BarResult res, string btnPrefix)        {
            var barRect = new PxRect(ContentL, ContentT, ContentL + BarW, ContentB);
            var bar = Node(area, "Tab Buttons", barRect);
            Rect(bar, ArtBarBg, barRect.x1, barRect.x2, barRect.y1, barRect.y2, "Background", QPanel);
            // `Shadow`：**宽 0 就不建**（社交窗那条原版自己就是 0 高/0 宽，见 `BarShadowW` 的注释）
            if (BarShadowW > 0f)
                Rect(bar, ArtBarShadow, ContentL, ContentL + BarShadowW, ContentT, ContentB, "Shadow", QPanel);

            var tb = bar.gameObject.AddComponent<TabButtons>();
            tb.options.Clear();
            res.buttons = tb;
            res.roots = new Transform[specs.Length];
            res.highlight = new ImageQuad[specs.Length];
            res.highlightRoot = new GameObject[specs.Length];
            res.badge = new ImageQuad[specs.Length];

            var holder = New(bar, "Buttons");
            res.holder = holder;
            for (int i = 0; i < specs.Length; i++)
            {
                // `VerticalLayoutGroup`：padTop 120 从**栏顶**起排 ⇒ 第 i 键顶边 = 70.94 + 120 + 180i
                float top = ContentT + BarPadTop + TabBtnH * i, bot = top + TabBtnH;
                res.roots[i] = BuildTabButton(holder, i, specs[i], top, bot, res, btnPrefix);
                tb.options.Add(new TabButtons.Option
                {
                    type = visualTypes.Count > i ? visualTypes[i] : WindowTabType.None,
                    button = res.roots[i].GetComponentInChildren<WindowButton>(true),
                });
            }

            // 🔴 **照原版：最后一个键是母版，`Initialize` 一进来就关掉**（见 `TabButtons.Initialize` 的注释）。
            //    ⇒ **左栏运行期比键表少一个**（日常那页：4 个键里第 4 个是母版 ⇒ 只见 3 个）。
            //    它照建不误（以后加活动页签要克隆它），只是不显示。
            tb.tabButtonPrefab = res.roots[specs.Length - 1].gameObject;
            tb.Initialize(this);
            return bar;
        }

        /// <summary>🆕 2026-10-03：切「左栏亮哪一个键」。**必须整棵九宫格一起开关** ——
        /// `Highlight` 从「单块拉伸」改成了**九宫格**（原版就是 `Image.Type = Sliced` +
        /// `m_PixelsPerUnitMultiplier = 0.92`，见 `BuildTabButton` 里那条判据），一棵树里 **9 个 quad**
        /// ⇒ 只切其中一个会**留下另外 8 块**（静默，只在画面上现形）。
        /// 四个窗口（Rewards / Shop / Social / Collection）的 `RefreshHighlights` 都走这一份。</summary>
        public static void SetHighlight(BarResult res, int sel)
        {
            if (res == null || res.highlightRoot == null) return;
            for (int i = 0; i < res.highlightRoot.Length; i++)
                if (res.highlightRoot[i] != null) res.highlightRoot[i].SetActive(i == sel);
        }

        /// <summary>一个键。子件几何**逐条照正本 §二·2 的公共参数表**（两个窗口共用）。</summary>
        Transform BuildTabButton(Transform parent, int idx, TabBtnSpec spec, float y1, float y2,
                                 BarResult res, string prefix)
        {
            var b = Node(parent, prefix + idx, new PxRect(ContentL, y1, ContentL + BarW, y2));
            float cx = ContentL + BarW * 0.5f;              // 键的水平中心（栏内）
            float cy = (y1 + y2) * 0.5f;

            // `Highlight`：整键矩形；`40k_main_bt_selected BW`，色 **#FF0000**，**出厂 en=1/a=1**
            // 🔴 **2026-10-03 订正：原版这一件是 `Image.Type = Sliced`（九宫格）+ `m_PixelsPerUnitMultiplier = 0.92`**
            //    —— 我们原来**单块拉伸**（71² 的图拉到 165×180）⇒ 那条 ~30px 的软边被拉成 ~70px。
            //    **判据（实读 + 采样）**：`menu_dump.py …bundle_menus_assets_all "MissionsRewardsButton" --depth 2 --relative`
            //      ⇒ `Highlight｜40k_main_bt_selected BW 71×71 九宫 30,30,30,30｜Sliced ppuMul=0.92`；
            //    再按 `_atlas_rects.json` 的 `[3529,1254,71,71]` 采样图集 ⇒ 边 `灰93–113/α159–208`、心 `184/α255`
            //      ⇒ **是软边晕，不是纯色**（原记录「底图是纯色 ⇒ 可能看不出来」**已被证伪**）。
            //    ⇒ 画出来的角块 = `30 ÷ 0.92 = 32.61px`（`borderOutPx`）。
            // ⚠️ 这里**故意直接调 `MenuDraw.Nine`、不传 `Clip`**：左栏不在任何滚动视口里
            //    （原版那上面也没有 `RectMask2D`），而 `BuildShell` 只从各窗的 `Build()` 走一次。
            //    别把它改成本类的 `Nine()`（那会吃的 `Clip` 是**别人**留下的）。
            var hlRoot = MenuDraw.Nine(b, Art(ArtSelHighlight),
                                       new PxRect(ContentL, y1, ContentL + BarW, y2),
                                       new Vector4(30f, 30f, 30f, 30f), 71f, 71f, QPanel,
                                       new Color(1f, 0f, 0f, 1f), true, "Highlight",
                                       new Vector4(30f / 0.92f, 30f / 0.92f, 30f / 0.92f, 30f / 0.92f));
            res.highlightRoot[idx] = hlRoot;
            res.highlight[idx] = hlRoot != null ? hlRoot.GetComponentInChildren<ImageQuad>() : null;

            // `Icon`：`a=(0,0)-(1,1) p=(.5,.7) sz=(0,0)` + **preserveAspect** ⇒ 等比放进 165×180 并居中
            //（UGUI 的 preserveAspect 是按 rect 居中收 padding，**不看 pivot**）
            var iconTex = Art(spec.Art);
            if (iconTex != null)
            {
                float side = Mathf.Min(BarW, TabBtnH);
                var q = ImageQuad.Create(b, iconTex, Local(b, cx, cy), LayoutSpace.Px(side),
                                         new Vector2(0.5f, 0.5f), "Icon");
                if (q != null) { q.SetAspect(1f); q.SetRenderQueue(QContent); }
            }
            else MissingArt.Add(spec.Art);

            // `Label` 底：`a=(.5,.5) p=(.5,0) pos=(0,-72.16) sz=(155,37.86)`
            const float labW = 155f, labH = 37.86f, labDy = -72.16f;
            float lb = cy - labDy;                          // pivot 在底边 ⇒ 底边 y
            Rect(b, ArtNametag, cx - labW * 0.5f, cx + labW * 0.5f, lb - labH, lb, "Text Background", QContent);

            // 文案：TMP 字号 = 原版 `m_fontSize`（画布像素）；色 **#F4E1AC**；`m_fontStyle=UpperCase`
            var txt = Text(b, (spec.Label ?? "").ToUpperInvariant(), cx - labW * 0.5f, cx + labW * 0.5f,
                           lb - labH, lb, 6, new Color(0.9569f, 0.8824f, 0.6745f), "Text", spec.FontPx);
            if (txt != null) txt.SetAutoFitBox(LayoutSpace.Px(labW), LayoutSpace.Px(labH), spec.AutoMin, spec.AutoMax);

            // `Badge Highlight`：35²；色 **#BCBCBC**；纵向偏置**逐键不同**（见 `TabBtnSpec.BadgeDy`）。
            // 🔴 原版四个键**出厂 `m_IsActive = 1`**，但显隐走 `UiBadgeNotification` 的 **alpha 补间**
            //    （`Show()` → 1.0 + 文本=计数 · `Hide()` → 0）—— **不是 `SetActive`**。
            res.badge[idx] = Rect(b, "40K_notification_number", cx + 51.7f - 17.5f, cx + 51.7f + 17.5f,
                                  cy - spec.BadgeDy - 17.5f, cy - spec.BadgeDy + 17.5f, "Badge Highlight", QContent,
                                  new Color(0.7373f, 0.7373f, 0.7373f, 0f));   // 初值 alpha 0，由子类的刷新函数定

            // 点击区：整键（原版是 `EverguildToggle`，我们只用它的点击语义）
            var hit = New(b, "Hit");
            var hq = ImageQuad.Create(hit, CardArt.Solid(), Local(hit, cx, cy), LayoutSpace.Px(TabBtnH),
                                      new Vector2(0.5f, 0.5f), "Hit");
            if (hq != null)
            {
                hq.SetAspect(BarW / TabBtnH);
                hq.SetTint(new Color(0f, 0f, 0f, 0f));
                hq.SetRenderQueue(QPanel);
            }
            int captured = idx;
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = () => tabButtons.Click(captured);
            return b;
        }

        /// <summary>子类在 `Build()` 里往 `Tabs` 下塞页时用的公共矩形（原版 `Tabs` 与 `Content Area` 同矩形）。</summary>
        public static PxRect TabsRect { get { return new PxRect(ContentL, ContentT, ContentR, ContentB); } }
    }
}
