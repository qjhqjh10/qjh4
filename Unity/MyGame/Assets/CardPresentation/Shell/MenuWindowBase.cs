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

        // ============================================================ 裁切状态（`Clip` / `ClipSoftness` / `ClipPad`）
        //
        // 🔴 **2026-10-07（A78②）：这三兄弟 + 两份转发（`RenderClip` / `AddHit`）已经上移到共同基类
        // `GameWindow`** —— 判据原文：「`MenuDraw.Hit` / `DeckCell` 的 `hitPad` 参数**只有 `MenuWindowBase`
        // 家族能喂到**……将来真要给那一族加，该做的是**把状态挪到 `GameWindow`**，⛔ 不是每扇窗各抄一段」。
        // ⇒ **本类不再声明自己的副本**（两份声明 = 迟早不一致）。**逐处实读表 / 判据 / 更正痕迹
        // 全部搬到了 `GameWindow`（`Shell/WindowsManager.cs`）那三个字段上**，⛔ 别在这儿再写第二份。
        // ⚠️ 本类的读法一个字没变（`Clip` / `ClipSoftness` / `ClipPad` / `RenderClip` 靠继承解析到同一份）；
        //    下面那几个包装（`Rect` / `Nine` / `Text` / `TextBox`）照旧把 `RenderClip` + `ClipSoftness` 转下去）。

        // ============================================================ 左栏键的规格

        /// <summary>一个左栏键：图标名 · 文案 · 原版字号(px) · autosize 区间 · 实例 ID · 红点纵向偏置。</summary>
        public struct TabBtnSpec
        {
            public readonly string Art, Label, InstId;
            public readonly float FontPx, AutoMin, AutoMax;
            /// <summary>红点相对键中心的 y 偏置（原版 `Badge Highlight` 的 `pos.y` 取负）。
            /// 日常那四个键里**第 4 键是 `+47.9`、其余是 `−27.2`** —— 逐键不同 ⇒ 放进规格里，别在循环里写死 `idx == 3`。</summary>
            public readonly float BadgeDy;
            /// <summary>🔴 **2026-10-12（A336①）：原版那一颗 `TabButtonLabel` 的 `m_fontSizeBase`**（画布 px）。
            /// 它只影响自适应的**二分起点**（`TextMeshPro.cs:2148-2149`），终点收敛到「装得下的最大号」
            /// ⇒ 渲染差 ≤ 0.05 fontSize 单位；**但不传就是错的字段**（`Battle/Label` 的 `SetAutoFitBox(…, basePx)` —— `basePx` 那个形参）。
            /// <para>📌 **缺省 `23f` 是【逐窗实读值】，不是通则**（铁律 5·c）：原版**四个窗的左栏键文案
            /// 各一颗一颗读下来全是 23.0** ——
            /// `Rewards Base Submenu Variant/Content Area/Tab Buttons/{MissionsRewardsButton,CampaignRewardsButton,Forge Button}/Label/TabButtonLabel`
            /// （`'Missions'`/`'Campaign'`/`'Forge'`）· 同一窗第 4 颗母版 `Menu Navigation Panel Button/…`
            /// （`'Booster Packs'`）· `Social Submenu Variant/…/{Alliances,Friends} Tab Button/…` ·
            /// `Shop Menu Variant/…/Shop Icon/…`（`'Pacotes'`）· `Collection Menu Variant/…/{Deck,Cards,CardBacks,Alternate Art}/…`
            /// ⇒ **共 11 颗、base 全 = 23.0**。
            /// 🔴 **其中 `Booster Packs` 那颗连区间都不同**（`auto[12~33]`，其余 `auto[5~36]`）——
            /// 那两格走 `AutoMin`/`AutoMax`，本字段只管 base。
            /// ⚠️ 读法：`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Rewards Base Submenu Variant" --depth 12 --md`
            /// **不印 base**（V7 §六·1）⇒ 本节的值是扫 `assets_full/*/MonoBehaviour/*.json` 的 `m_fontSizeBase` 得到的。</para></summary>
            public readonly float AutoBase;
            public TabBtnSpec(string art, string label, float fontPx, float autoMin, float autoMax, string instId,
                              float badgeDy = -27.2f, float autoBase = 23f)
            { Art = art; Label = label; FontPx = fontPx; AutoMin = autoMin; AutoMax = autoMax;
              InstId = instId; BadgeDy = badgeDy; AutoBase = autoBase; }
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

        /// <summary>空节点（**`RectTransform`**）。🔴 **2026-10-07（A92）**：原来是 `new GameObject(name)`
        /// （裸 `Transform`）⇒ 原版那些带矩形语义的容器节点我们这一层表达不了。判据与完整说明见
        /// `MenuDraw.Node` 的注释（同一份实读：`bundle_menus_assets_all` 16768 个 `GameObject` 里
        /// **16510 `RectTransform` / 258 裸 `Transform`**，那 258 个全是卡框 3D 锚与粒子件）。
        /// ⛔ 别写成 `AddComponent&lt;RectTransform&gt;()`。
        /// ⚠️ **原版本身是裸 `Transform` 的件**（全工程就一处）走 <see cref="NewPlainTransform"/>。</summary>
        public static Transform New(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>空节点，但**保留裸 `Transform`**（= 原版这一件就没有 `RectTransform`）。
        /// 🔴 **为什么单开一个方法而不是给 `New` 加开关**：`New` 有 20+ 个调用点，
        /// 而「原版是裸 `Transform`」是**逐个节点的事**、不是调用点的偏好 —— 单开一个名字让例外**显式可数**
        /// （数一下全工程几处调用它就等于「原版有几个裸 `Transform` 节点」）。
        /// <para>⚠️ **目前只有一处**：`ForgeTab.BuildParticleHosts` 的 `Particle System nebula`
        /// （原版是 3D 子件，`m_LocalPosition = (0, ~0, 0.553)`，见 `ForgeTab.cs` 那段注释）。
        /// 配套断言 = `Editor/RewardsScene.cs` 的「`Particle System nebula` **没有** `RectTransform`」。
        /// ⛔ **别拿它当「省一步」的捷径** —— 原版那 258/16768 之外全是 `RectTransform`。</para></summary>
        public static Transform NewPlainTransform(Transform parent, string name)
        {
            var go = new GameObject(name);          // ⚠️ 故意不带 `typeof(RectTransform)`
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

        /// <summary>原版像素矩形中心 → **相对 `parent` 的局部坐标**。
        /// 🔴 **2026-10-11（A297）：本方法已改成【转调 `MenuDraw.Local`】。**
        /// 原来这里自己又写了一遍 `RectCenter − parent.position` —— 与 `MenuDraw` 那份是**同一个算式**、
        /// 也**同样少除一次父链缩放**；而 A228（`Align*On`）与 A294（`MenuDraw` 那 4 处）**都已修完**
        /// ⇒ 不收这一处就是**两套口径并存**（一边设计空间、一边世界空间），**比两边都错更难查**。
        /// <para>**它服务谁**：本族四个窗（`RewardsWindow` / `ShopWindow` / `SocialWindow` / `CollectionWindow`）
        /// 的 `Node(`（容器节点）· `Text` / `TextBox`（文字）· 左栏键 · 内容区渐变背景 —— 小屏缩放开关一开，
        /// 窗根被 `TransformScalerBySmallScreenUI` 乘 M ⇒ 不除这一次，**整扇窗的文字与图**都按 `(1−M)·nl` 偏
        /// （`nl` = 父件到窗根的距离；父件越靠窗根越看不出来）。</para>
        /// <para>判据 / 算式 / 「除的是哪一级的 `lossyScale`」→ <see cref="MenuDraw.PosInDesignSpace"/>（**全壳唯一一份**）。
        /// ⚠️ `k == 1`（= 父件那一级链上没有缩放）时与旧写法**逐位相同** —— **开关出厂是关的** ⇒ 自检必须两态
        /// （宿主 = `Editor/ShopScene.cs` 的 A297 那一段）。</para></summary>
        public static Vector3 Local(Transform parent, float x1, float y1, float x2, float y2)
            => MenuDraw.Local(parent, x1, y1, x2, y2);

        /// <summary>原版像素**点** → 相对 `parent` 的局部坐标。
        /// 🔴 `ImageQuad.Create` / `Label.Create` 的 `pos` 都是 **localPosition** ——
        ///    直接喂 `LayoutSpace.FromPixel(...)`/`RectCenter(...)`（世界坐标）在父节点有偏移时会**双倍错位**。
        ///    第一版左栏四个图标、内容区渐变背景、进度条九宫格全栽在这上面，而且**断言全绿**。
        /// 🔴 **2026-10-11（A297）**：减号右边原来也是 `parent.position`（**同一份量纲病**）。
        ///    `MenuDraw` 里只有「矩形中心」那一份可转，没有「点」这一份 ⇒ 这里直接转调它的公共件
        ///    `PosInDesignSpace`（**别自己再写一遍除法**）。
        ///    生产调用点 = `Shell/SettingsWindow.cs` 里那两处 `MenuDraw.Local`（设置窗两根滑块的落位）与
        ///    `Shell/ShopWindow.cs` 里 `MainMenuSubmenuWindow.Local(ic, …)` 那一句（时间计数器图标）—— 两处**都只走本重载**、不读 `MenuDraw.Local`
        ///    ⇒ 两个重载都必须转调。</summary>
        public static Vector3 Local(Transform parent, float xPx, float yPx)
            => LayoutSpace.FromPixel(xPx, yPx) - MenuDraw.PosInDesignSpace(parent);

        /// <summary>建一个**有矩形语义的容器节点**（摆在原版那个矩形的中心）。
        /// 原版每个节点都有自己的 rect；我们的世界空间里「容器」自己不带渲染，但**位置与尺寸都要摆对** ——
        /// 否则自检量不到、将来做点击/滚动也会算错。
        /// 🔴 **2026-10-11（A218）**：位置与尺寸**一起**收口到 `MenuDraw.ApplyPxRect`
        /// （此前只写了 `localPosition` ⇒ `rect` 的宽高还是默认值）。⛔ 别在这里另写一套 `sizeDelta` 算式 ——
        /// 判据 / 锚点为什么写死 `(0.5,0.5)` / 「写完不动位置」的守卫，全在 `MenuDraw.SetPxSize` 那一份注释里。</summary>
        public static Transform Node(Transform parent, string name, PxRect r)
        {
            var t = New(parent, name);
            MenuDraw.ApplyPxRect(t, parent, r);       // = 位置（`Local`）＋ 尺寸（`sizeDelta`），同一份换算
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
            // 🔴 **裁切那一段只有 `MenuDraw.Rect` 一份**（2026-09-24 收口）—— 这里转调**基类的 `DrawRect`**
            //    （`GameWindow.DrawRect`：`RenderClip` + `ClipSoftness` → `MenuDraw.Rect` 那两个形参），
            //    别把「等比放进框 + 裁切 + 截 uv」再抄一遍（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
            // 🆕 2026-10-04：软边（`ClipSoftness`）一起转下去。
            // 🔴 **2026-10-07（A140②）：转的是 `RenderClip`（= `Clip` 按 `ClipPad` 内缩），不是裸 `Clip`** ——
            //    原版 `RectMask2D` 的**渲染那一面也读 `m_Padding`**（判据 → `MenuDraw.PaddedClip`）。
            // 🔴 **2026-10-07（A78②）**：转发那一句收口到 `GameWindow.DrawRect`（全族唯一一份），
            //    本方法只剩「取图 + 等比那一位」这一层；行为逐字未变。
            return DrawRect(parent, tex, new PxRect(x1, y1, x2, y2), name, q, tint,
                            keepAspect && art != null);   // ⚠️ 纯色块不做等比（同旧行为）
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
            // 🔴 **2026-10-07（A78②）**：转发那一句收口到 `GameWindow.DrawNine`（全族唯一一份）——
            //    本方法只剩「取图 + texW/texH 兜底」这一层；行为逐字未变（`RenderClip` 含 padding，不是裸 `Clip`）。
            return DrawNine(parent, tex, r, border, texW > 0f ? texW : tex.width, texH > 0f ? texH : tex.height,
                            q, tint, fillCenter, name, borderOutPx);
        }

        // 🔴 **`AddHit` 已上移到 `GameWindow`**（2026-10-07 · A78②）：它转发的那两样（裸 `Clip` + `ClipPad`）
        //    本来就是全族的状态，方法留在本类 = 「每扇窗各抄一段」那个缺口的形状。签名逐字未动
        //    ⇒ 本类与各页（`CollectionWindow` / `ForgeTab` / `CampaignTab` …）的调用点全部照旧解析到同一份实现。
        //    **判据 / 更正痕迹 / 端到端断言出处全在 `GameWindow.AddHit` 的注释里**，⛔ 别在这儿再写第二份。

        /// <summary>按像素矩形摆一段文字（居中）。`fontPx` = **原版 TMP 的 `m_fontSize`**（画布像素）
        /// —— 内部走 `Label.SetGlyphHeight(px/108)`；🔴 **别用 `SetFontSize(px/108)`**，那会大 2.7 倍。
        /// <para>🆕 **2026-10-09（A1173）：补上 `wrapPx` / `autoMinPx` / `autoMaxPx` / `autoBasePx` 四个形参**
        /// —— 本方法此前**一个 autosize 形参都没有** ⇒ 走本层的窗（`CollectionWindow` / `ShopWindow` /
        /// `CampaignTab` …）**不是「忘了传」，是「没地方传」**，只能各自在调用点手工补 `SetAutoFitBox`
        /// （**实读 11 处**，见下面那条 🔴）。形参表与内部接线**照同文件的兄弟方法
        /// <see cref="TextBox"/>（本文件 `:325`）与 `MenuDraw.TextCore` 抄**，量纲/命中条件逐条对齐：
        /// **`wrapPx &gt; 0` ∧ `autoMinPx &gt; 0` ∧ `fontPx &gt; autoMinPx` 三条全真**才真调
        /// `SetAutoFitBox`（`autoMaxPx &lt;= 0` 时上限退回 `fontPx` = 旧行为）——
        /// ⚠️ **只传 `autoMinPx` 不传 `wrapPx` = 死实参**（判据 = `Shell/MenuDraw.cs` 的 `TextCore`，⛔ 别抄第二份）。
        /// 四个新形参**全默认 `0`** ⇒ **24 个既有调用点一字不改、行为逐位不变**（与 `TextBox` 同一约定）。</para>
        /// <para>🔴 **裁切次序**：autosize 那两步（`SetWrapWidth` / `SetAutoFitBox`）会**重排 mesh**
        /// ⇒ 必须排在下面那句 `ClipText` **之前**（同 <see cref="TextBox"/> 与 `MenuDraw.TextCore` 的纪律）。
        /// 收口前那 11 处是「调用点先 `Text`、再 `SetAutoFitBox`」⇒ 那一刀裁在**重排之前**、**被冲掉**
        /// ⇒ 现在这些标签的裁切**第一次真生效**（画面差 = 只有「本当被视口裁掉」的那些字）。</para></summary>
        public Label Text(Transform parent, string text, float x1, float x2, float y1, float y2, int scale,
                          Color color, string name, float fontPx = 0f,
                          float wrapPx = 0f, float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f)
        {
            // **裁切**：① 「**整块**在视口外 ⇒ 不建」—— 收口到 `MenuDraw.Visible`（🔴 **2026-10-10 订正（A184）**：
            //   原写「A25④ 那四处内联的唯一实现」，那四处内联早已收口 ⇒ **现在它是全壳唯一一份求交**）；
            //         ② 🆕 2026-10-04：**压在视口边缘的那几个字要切**（`MenuDraw.ClipText` 把渲染网格裁到框内）。
            // 🔴 此前这里只做 ①，注释里写着「部分越界的字仍按原样画 —— 这条缺口写在 `Clip` 字段的注释里」。
            //    现在那条缺口**补掉了**（判据 = 原版 `RectMask2D` 对文字与图一视同仁）。
            // ⚠️ 裁的时机必须在**定完字号之后**（`SetGlyphHeight` 会重排 mesh），所以放在最后一步。
            // 🔴 **2026-10-07（A140②）**：这两处吃的是 `RenderClip`（= `Clip` 按 `ClipPad` 内缩）——
            //    原版 `RectMask2D` 对文字也用**内缩后的** `_ClipRect`（判据 → `MenuDraw.PaddedClip`）。
            // 🔴 **2026-10-12（A435①）：取状态走【一处】共用解析**（`ViewportClip.Resolve`）——
            //    本方法原来两处都直接读 `RenderClip` / `ClipSoftness` 两个**本窗字段**，于是本窗没设
            //    `Clip` 时：① 上面那句 `Visible` 拿到 `null` ⇒ 一律判「可见」；
            //    ② 下面那句 `if (RenderClip.HasValue)` ⇒ **根本不调 `ClipText`**。
            //    ⇒ **父链上有没有 `ViewportClip` 节点，对这段文字完全不起作用**（正是 H10 §五·1 记的那个缺口）。
            //    现在两处都吃**解析后**的那一份（`_st`）：形参非空 = 旧路赢、**逐位等于原来的 `RenderClip` /
            //    `ClipSoftness`**（`PaddedClip(·, Vector4.zero)` 首句早退）⇒ 今天（全仓无节点）**行为逐位不变**；
            //    本窗没设 `Clip` 而父链上有节点时，才由**节点**接管（阶段 2 的语义）。
            //    ⚠️ 解析起点 = `parent`（本方法建标签/图都挂在它下面）—— 与 `MenuDraw.Rect` / `ClipText`
            //    内部那一份取法同一口径（节点在父链上同样命中）。
            // 🔴 **动过这段就别忘下面那句 `ClipText`**：两处必须用**同一份** `_st`，否则会出现
            //    「按节点判了可见、却按本窗字段（= 不裁）建出来」这种半拉子状态（静默、且只在挂节点时现形）。
            var _st = ViewportClip.Resolve(parent, RenderClip, ClipSoftness, default(Vector4));
            if (!MenuDraw.Visible(new PxRect(x1, y1, x2, y2), _st.RenderClip)) return null;
            var lb = Label.Create(parent, text, Local(parent, x1, y1, x2, y2), scale, color,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(QText);
            if (fontPx > 0f) lb.SetGlyphHeight(LayoutSpace.Px(fontPx));
            // 🆕 **2026-10-09（A1173）：autosize 两步** —— 逐字照 `MenuDraw.TextCore`（`Shell/MenuDraw.cs`，
            //    其注释里写着为什么只有那三个条件同时成立才真调 `SetAutoFitBox`；⛔ 别在这儿再写第二份判据）。
            //    ⚠️ **必须排在下面那句 `ClipText` 之前** —— 这两步会重排 mesh，裁切在它们之前会被冲掉
            //    （正是收口前那 11 处调用点的写照：`Text` 里裁一刀、调用点再 `SetAutoFitBox` 把它抹掉）。
            //    ⚠️ 上限的兜底 `autoMaxPx <= 0 ⇒ fontPx` 与 `MenuDraw.Text` / `TextCore` 同义（A333）。
            if (wrapPx > 0f)
            {
                lb.SetWrapWidth(LayoutSpace.Px(wrapPx));
                if (autoMinPx > 0f && fontPx > autoMinPx)
                    lb.SetAutoFitBox(LayoutSpace.Px(wrapPx), LayoutSpace.Px(y2 - y1), autoMinPx,
                                     autoMaxPx > 0f ? autoMaxPx : fontPx, autoBasePx);
            }
            // 🔴 A435①：形参 = **调用方原样那一份**（`RenderClip` / `ClipSoftness`），**不是** `_st` 里那两份 ——
            //    形参非空时两条路逐位相同，而形参为 `null` 时它让 `ClipText` / `ClippedTextGuard`
            //    **跟着父链重解析**（守卫要在节点挪动之后重裁，快照会拿旧框裁，见 `ClippedTextGuard` 的类注释）。
            if (_st.RenderClip.HasValue) MenuDraw.ClipText(lb, RenderClip, ClipSoftness);
            return lb;
        }

        /// <summary>**按原版 TMP 的规矩**摆一段文字：**限宽换行**（`m_TextWrappingMode = 1`）+ 可选**自适应字号**。
        /// 🔴 **为什么必须有这个包装**：`Label` 内部把换行模式写死成 `NoWrap` ⇒
        /// 每日任务行那句 `Deal 500 damage to enemy units`（35px）直接**冲出卡外**，**61 条断言一条都没报**。
        /// ⚠️ `SetAutoFitBox` 内部按**比例**算 min/max（单位同 `fontSize`，不是世界单位 —— 直接填 px/108 会把字号压到 0.3px）。</summary>
        public Label TextBox(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                             float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f)
        {
            // 🔴 **2026-09-24**：实现挪到 `MenuDraw.TextBox`（活动窗那几扇也要用），这里**转调**；
            //    只有本层有 `Clip`，所以那道「整块在视口外就不建」的判断留在这儿。
            // 🆕 2026-10-04：改成 `MenuDraw.Visible`（A25④ 收口）+ **部分越界也裁**（`ClipText`，
            //    在 `TextBox` 的 autosize 定完字号**之后**才裁 —— 那一步会重排 mesh）。
            // 🆕 **2026-10-12（A333/A336②）**：`autoMaxPx` = 原版 `m_fontSizeMax`、`autoBasePx` = 原版
            //    `m_fontSizeBase`，**两个都 `<= 0` 时逐位等价于旧行为**（上限 = `fontPx`、base = 调用方那档）。
            //    判据 → `MenuDraw.Text` 的注释。
            // 🔴 **2026-10-12（A435①）**：同 `Text` —— 取状态走 `ViewportClip.Resolve`（本窗字段只是**形参**，
            //    本窗没设 `Clip` 时由父链上最近的 `ViewportClip` 节点接管）。形参非空 ⇒ 逐位等于原来的
            //    `RenderClip`；今天（无节点 + 本窗设了 `Clip`）**行为逐位不变**。
            var _st = ViewportClip.Resolve(parent, RenderClip, ClipSoftness, default(Vector4));
            if (!MenuDraw.Visible(r, _st.RenderClip)) return null;    // 🔴 `RenderClip`（含 padding），同 `Text`
            var lb = MenuDraw.TextBox(parent, r, text, color, name, fontPx, autoMinPx, QText,
                                      autoMaxPx, autoBasePx);
            // 🔴 两处必须同一份 `_st`（见 `Text` 那一段最后一条）；形参传**原样那一份**（理由同上）。
            if (lb != null && _st.RenderClip.HasValue) MenuDraw.ClipText(lb, RenderClip, ClipSoftness);
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
            // 🔴 **2026-10-09（A1173）**：autosize 那四个数收进 `Text` 的形参（原来是「先 `Text`、
            //    再手工 `SetAutoFitBox`」—— 两份写法并存 = 迟早不一致）。
            //    ⚠️ 框 = 底下那块 `Label` 底 (`labW`×`labH`)，与本行那对 `cx ± labW*0.5f` / `lb-labH..lb`
            //    **同一个矩形**；`autoBasePx` = 原版 `m_fontSizeBase`（23），见 `TabBtnSpec.AutoBase`。
            var txt = Text(b, (spec.Label ?? "").ToUpperInvariant(), cx - labW * 0.5f, cx + labW * 0.5f,
                           lb - labH, lb, 6, new Color(0.9569f, 0.8824f, 0.6745f), "Text", spec.FontPx,
                           wrapPx: labW, autoMinPx: spec.AutoMin, autoMaxPx: spec.AutoMax,
                           autoBasePx: spec.AutoBase);
            // 🔴 **2026-10-08（A212 · A62 主表 #31「四窗左栏页签」）**：上面 `Text(...)` 里那两步
            //    （`wrapPx > 0` ⇒ `SetWrapWidth` + `SetAutoFitBox`；**A1173 起收口进漏斗**）内部会
            //    `SetWrapWidth` ⇒ **无条件把模式开成 `Normal`**（`Core/TmpFont.cs` 的 `SetWrapWidth`），而原版**四个窗的
            //    左栏键文案一律 `m_TextWrappingMode = 0`**（判据 = 逐窗现读 `工具/menu_dump.py …
            //    "<窗口根>" --depth 6 --md` 的 `折行=` 列，四窗各一份、**没有一个是 1**）：
            //      · `Rewards Base Submenu Variant` → `Tab Buttons/*/Label/TabButtonLabel`
            //        `Missions` / `Campaign` / `Forge` / `Booster Packs` —— **4/4 = 0**（`Booster Packs`
            //        那颗是 `tabButtonPrefab` 母版，`25.65 auto[12~33]`，也是 0）；
            //      · `Collection Menu Variant` → `Deck` / `Cards` / `Cosmetics` / `Styles` —— **4/4 = 0**；
            //      · `Social Submenu Variant` → `Alliances` / `Friends` —— **2/2 = 0**；
            //      · `Shop Menu Variant` → 序列化的**只有母版** `Shop Icon > Label > TabButtonLabel`
            //        （`'Pacotes'`，葡语占位）**= 0**；另外两键由 `ShopWindow.CreateStoreTab` 在运行期
            //        **克隆这只母版**（`ShopData.Pages` 的注释）⇒ 同样吃这个 0。
            //    ⚠️ 这一句对**画面是可见的变化**：`Normal` 时 `BOOSTER PACKS` 那种带空格的三行文案会
            //    **折成两行**、再被 `auto` 压字号；`NoWrap` 时才照原版按**一行**缩到装得下。
            //    （🔴 **2026-10-13（A720）就地订正（铁律 5）—— 原来这两行写的是**：
            //     「四窗这几颗**没有**「渲染宽度 ≤ 框宽」的断言 —— 已 `grep` 核过；同形的两条在
            //      `Editor/MainMenuScene.cs` 的**档案窗**键循环里，那是 `PlayerProfileWindow` 自己那一族、不走本函数。」
            //     **实际是**：2026-10-08 波 C3（A212 主表 #31）已经给**四扇窗各自**加了该断言，
            //     **每窗 4 颗键逐颗断**，而且断的**正是本函数建出来的那几颗**
            //     （`BuildShell(…, "<X>TabButton_", …)` → `BuildTabBar:414` → `BuildTabButton`）：
            //       · 奖励窗 `Editor/RewardsScene`（找 `RewardsTabButton_*`）
            //       · 商店窗 `Editor/ShopScene`（找 `ShopTabButton_*`）
            //       · 收藏窗 `Editor/CollectionScene`（找 `CollectionTabButton_*`）
            //       · 社交窗 `Editor/MainMenuScene`（找 `SocialTabButton_*`；宿主是它）
            //     ⚠️ **只认符号名、⛔ 不记行号**（行号天天漂 —— 这正是 A1123 要治的）⇒ 现读就按上面
            //     那四个 `*TabButton_*` 前缀 `grep`，另一条关键词 = 「渲出来的宽 ≤ 框宽 155」。
            //     四条都是「`渲出来的宽 ≤ 155 + 0.5`」，框宽 **155** = 原版 `Tab Buttons/*/Label`
            //     的 `sz=(155,37.86)`；四窗各有一条同批的块注释自陈这件事。
            //     **错因**：原句写作时（波 C3 **之前**）确实成立 —— 它是**当时的实况**；
            //     波 C3 给四个宿主加断言时**没回头改这一句** ⇒ 典型的「当时对、现在不对」。
            //     ⚠️ 顺带两条（⛔ 都不在本件白名单）：① ✅ **2026-10-14（A795）就地订正（铁律 5）—— 这一条
            //     原来写的是**「那四条里**三条仍读缓存**（`RewardsScene`/`ShopScene`/`CollectionScene` 的
            //     `klb.WorldW * 108f` = `_tmpW` 缓存口 —— `Editor/MainMenuScene.cs` 那一条 A715 已换口）」；
            //     **实际是**：A750（2026-10-13）把点名的那三条**也**换成了 `TmpRenderedRect`（量 TMP 自己
            //     渲出来的那块网格）⇒ **四窗四条全是新口**（现读：三个宿主各一条 + `MainMenuScene.cs` 那条 A715，
            //     四条都印「★ …而且**渲出来的宽 … ≤ 框宽 155**」）。`Label.WorldW` 那一族只在这几个宿主
            //     **更外层的助手**（`RectOf` / `TextLeftPx` / `TextRightPx` 等）里还留着 —— 形状不同、用途也不同
            //     （三个宿主那份清单 → `资料/普查产出_1013/WA750_同族三处.md` §5·2）。
            //     **错因** = 同族「当时对、现在不对」：写这条时（A750 **之前**）确是实况，A750 换完口没回头改它。
            //     ② 原句后半「同形的两条在档案窗键循环里」也过期了 —— 那两条现读在 `Editor/MainMenuScene.cs`
            //     的档案窗键循环里（按 `TmpRenderedRect` 找），且早已换成 `TmpRenderedRect`）。）
            if (txt != null) txt.SetWrapping(false);

            // `Badge Highlight`：35²；色 **#BCBCBC**；纵向偏置**逐键不同**（见 `TabBtnSpec.BadgeDy`）。
            // 🔴 原版四个键**出厂 `m_IsActive = 1`**，但显隐走 `UiBadgeNotification` 的 **alpha 补间**
            //    （`Show()` → 1.0 + 文本=计数 · `Hide()` → 0）—— **不是 `SetActive`**。
            res.badge[idx] = Rect(b, "40K_notification_number", cx + 51.7f - 17.5f, cx + 51.7f + 17.5f,
                                  cy - spec.BadgeDy - 17.5f, cy - spec.BadgeDy + 17.5f, "Badge Highlight", QContent,
                                  new Color(0.7373f, 0.7373f, 0.7373f, 0f));   // 初值 alpha 0，由子类的刷新函数定

            // 点击区：整键（原版是 `EverguildToggle`，我们只用它的点击语义）
            // 🔴 **2026-10-11（A219②）：收口到 `MenuDraw.Hit`** —— 这里原来自己又写了一遍
            //   「建 `Hit` 节点 + 建那颗透明 quad + 挂 `WindowButton`」，与 `MenuDraw.Hit` 是**同一条规则
            //   两处各写一遍**（A92 那轮只把类型对齐了；`MakeHitQuad` 那一层当时已收口）。
            //   **逐项等价**（三样都别改 —— 改动前逐条对过）：
            //    ① **矩形 = 键那一格**：`x = cx ± BarW/2` · `y = cy ± TabBtnH/2` —— 与 `Node(...)` 给 `b` 的
            //       那个 `PxRect(ContentL, y1, ContentL + BarW, y2)` **同一格**（`cx`/`cy` 就是它的中心）；
            //    ② **落位** = `MenuDraw.Hit` 的既定约定（**节点**摆在父原点、**quad** 摆在矩形中心）——
            //       与旧代码**逐位相同**：旧代码也是 `New(b, "Hit")`（父原点）+ quad 喂 `Local(hit, cx, cy)`，
            //       而 `Local(parent, x1,y1,x2,y2)` 就是矩形中心 ⇒ 那颗 quad 的 localPosition 恒为 0；
            //    ③ **队列 = `QPanel`** · **tint = (0,0,0,0)**（`MakeHitQuad` 会替我们设这两样）。
            //   ⚠️ `clip` / `maskPad` / `target` 一律取默认值：左栏**不在任何滚动视口里**（原版那上面也没有
            //      `RectMask2D`），而悬停/按下那两档原版这几颗键**没有**（`m_Transition` → 见 `HoverTint`）。
            //   ⛔ 别把它改回「手写三件套」；断言 → `Editor/MainMenuScene.cs` 的社交窗左栏那一段（A219②）。
            int captured = idx;
            MenuDraw.Hit(b, "Hit",
                         new PxRect(cx - BarW * 0.5f, cy - TabBtnH * 0.5f, cx + BarW * 0.5f, cy + TabBtnH * 0.5f),
                         QPanel, () => tabButtons.Click(captured));
            return b;
        }

        /// <summary>子类在 `Build()` 里往 `Tabs` 下塞页时用的公共矩形（原版 `Tabs` 与 `Content Area` 同矩形）。</summary>
        public static PxRect TabsRect { get { return new PxRect(ContentL, ContentT, ContentR, ContentB); } }
    }

    /// <summary>原版共用件 `WindowHeaderWithBackButton` 的**唯一一份**建法。四层，**兄弟序照原版**：
    /// `Header Background` → [`Game Mode Icon`] → `Header Background (1)` → `Header Back Button`；
    /// 其中 `Window Title` 挂在**底板** `Header Background` 底下（原版父链 —— 缩进 3 层）。
    ///
    /// <para>🔴 **为什么要收口**：全工程有 **4 扇窗各自把它抄了一遍** —— `TutorialModePopup.BuildHeader` ·
    /// `LiveOpsEventWindow.BuildHeader` · `DailyStreakPopup.BuildHeader` ·
    /// `EnergySinglePlayerOnlyEventWindow.BuildHeader`；建的是**同一个原版 prefab**
    /// （同 sprite `WF_Campaign_Info_Background` 740×167 · 同九宫 `(335,0,395,0)` · 同 `Window Title`
    /// `fs 67.55` + 字距 5 + auto `[18, 67.55]` base 36），**连常量都被抄成了两份**
    /// （`DailyStreakPopup` 与 `LiveOpsEventWindow` 各有一组逐值相同的 `HeaderBorder` / `HeaderTexW,H`）。
    /// CLAUDE.md §三：「两处写同一条规则 = 迟早不一致」。</para>
    ///
    /// <para>⚠️ **四份之间的差异一个都没被抹掉** —— 全变成 <see cref="Spec"/> 上的显式字段
    /// （某一扇有一档 `TitleVAlign` / 某一扇少一刀自适应 / 返回钮的三种接线 / 尖角那一颗的节点名…）。
    /// 逐格对照表、「哪些不一样、每一格怎么处置」→
    /// `资料/普查产出_1018/S5_A866窗头收口.md` §2 / §4。</para>
    ///
    /// <para>⛔ **它放在本文件里**是调度台的指定（A866 那一笔的白名单：优先复用已有的
    /// `Shell/MenuWindowBase.cs`，别为共用件再开一个新文件）。</para></summary>
    public static class WindowHeader
    {
        // ============================================================ 原版常量（`Sprite/*.json` + `menu_dump` 实读）

        /// <summary>根名：3 扇窗是这一颗（`TutorialModePopup` / `DailyStreakPopup` / `EnergySingle…`）。</summary>
        public const string RootNameBack = "Header With Back Button";
        /// <summary>根名：遭遇战 / 排位那一族是这一颗（`LiveOpsEventWindow`）。</summary>
        public const string RootNameGameMode = "Game Mode Header With Back Button";
        public const string PlateName = "Header Background";
        public const string TitleName = "Window Title";
        /// <summary>往左延伸的那颗尖角。⚠️ `menu_dump` 实读就是这个名字（同族四份共用）。</summary>
        public const string WingName = "Header Background (1)";
        public const string BackName = "Header Back Button";
        public const string BackHitName = "BackHit";
        /// <summary>`BackStyle.QuadInOwnNode` 那一档：返回钮的图是具名节点的**子件**，原版叫 `Bg`。</summary>
        public const string BackQuadInNodeName = "Bg";

        /// <summary>原版那 4 颗 `Window Title` 的**出厂字段原文**（逐值相同）：
        /// `m_fontSize 67.55` · `m_characterSpacing 5` · 自适应区间 `[18, 67.55]` · `m_fontSizeBase 36.0`。
        /// <para>🔴 `TitleAutoBasePx` 的判据（A305①）：扫 `bundle_menus_assets_all` 里 `auto[18~67.55]` 那一族
        /// **8 颗全是 `m_fontSizeBase 36.0`**（例 `MonoBehaviour_3324232435684942507.json`，`'Daily Streak'`）。</para></summary>
        public const float TitleFontPx = 67.55f;
        public const float TitleCharSpacing = 5f;
        public const float TitleAutoMinPx = 18f, TitleAutoMaxPx = 67.55f, TitleAutoBasePx = 36f;

        /// <summary>`WF_Campaign_Info_Background`：**740×167 · 九宫 `(335,0,395,0)`**、`m_PixelsPerUnitMultiplier`
        /// 缺省 = 1（⇒ 画出来的角块就是 335 / 395px，两轴都没被挤到 `scX/scY` 那一步，`borderOutPx` 不用单传）。
        /// <para>判据（两处互证，⛔ 不是抄表）：① 本仓实读的 `Sprite/*.json`；② A641 那次全包清点 ——
        /// `bundle_menus_assets_all/MonoBehaviour/` 里**用这张 sprite 的 20 个 `Image` 实例 `m_Type` 全是 1
        /// (`Sliced`)**，没有一处走 `Simple` ⇒ 建这一族**一律 `MenuDraw.Nine`，⛔ 别退回 `Rect`**。</para>
        /// <para>🔴 **2026-10-17（A866）**：这三个值原来被抄成**两份**（`DailyStreakPopup` 与
        /// `LiveOpsEventWindow` 各一组、逐值相同；`EnergySingle…` 还私有一份 `HdrBorder`）—— 已收口到这一处。</para></summary>
        public static readonly Vector4 PlateBorder = new Vector4(335f, 0f, 395f, 0f);
        public const float PlateTexW = 740f, PlateTexH = 167f;

        /// <summary>`Window Title` 的「字距 / 自适应」这两刀怎么下、按什么次序 ——
        /// **四扇窗各是一种，一一对应**（2026-10-17 逐份实读）。
        /// <para>🔴 **次序不是小事**：两刀都**改渲染宽度**，而 `AlignLeftOn` 是「量**当时的** `WorldW`
        /// 再反推位置」⇒ 排在它之后改宽 = 那一行按**旧宽**定位、字整体往左溢出，**且不出声**
        /// （A475 / A492 那两笔账；同族先例 = `Shell/InboxWindow.cs` 的 A471）。</para></summary>
        public enum TitleFit
        {
            /// <summary>只加字距、**不调自适应** —— `DailyStreakPopup` 那一档（今天就是这样，本笔**照旧**）。</summary>
            SpacingOnly,
            /// <summary>先字距、后自适应 —— `TutorialModePopup` / `LiveOpsEventWindow` 那一档。
            /// <para>🔴 **A492（2026-10-14）把这两句对调过**（原文是 `SetAutoFitBox` 在上、`SetCharSpacing` 在下，
            /// 账上的话是「自适应不含字距」）—— 现在是**先字距、后自适应**，与本仓口径一致。</para>
            /// <para>⚠️ **静态上两种次序应收敛到同一结果**：`SetCharSpacing` 尾句 `ForceMeshUpdate()` 会把
            /// `m_fontSize` 复位成 `Clamp(m_fontSizeBase, min, max)`、**带着新字距重跑一次自适应**
            /// （判据 + 逐行注解 → `Battle/Label.cs` 的 `SetCharSpacing` docstring，**静态读 TMP 源码**得出、
            /// **未实跑验证**）⇒ 那次对调是**口径对齐**，不是「改回来一个原本读错的值」。</para>
            /// <para>🔴 **无牙口（如实登记）**：判据里「原版实际收敛到多少」是**空的** ⇒ 这一档**没有断言咬得住**
            /// （两种次序下 `SetAutoFitBox` 之后的 `FontPxNow` / `WorldW` 同值，断不出差别）。
            /// ⛔ 别为了让这条账「有牙口」而自定一个原版值 —— 那是发明判据。
            /// ⚠️ 改本档次序时**必须逐窗改**（另两档是 `SpacingOnly` / `FitBeforeSpacing`，见上）。</para></summary>
            FitAfterSpacing,
            /// <summary>先自适应、后字距 —— `EnergySinglePlayerOnlyEventWindow` 那一档
            /// （它与上一档**静态收敛**，但**是原样保留的差异**，别替它选边）。</summary>
            FitBeforeSpacing,
        }

        /// <summary>返回钮的三种接线 —— **四扇窗共三种，⛔ 别合并**（结构与节点名都不同）。</summary>
        public enum BackStyle
        {
            /// <summary>一张图直接挂顶栏根（名 `Header Back Button`）+ 透明命中区节点 `BackHit`：
            /// `TutorialModePopup`（`BackSwapArt` 空 ⇒ 无换图）与 `LiveOpsEventWindow`（非空 ⇒ 有换图）都走这一档。</summary>
            QuadOnHeader,
            /// <summary>多一层具名节点 `Header Back Button`、图是它的子件 `Bg` ——
            /// `EnergySinglePlayerOnlyEventWindow` 那一档（它自检量的就是 `/Header Back Button` 与 `…/Bg` 两个节点）。</summary>
            QuadInOwnNode,
            /// <summary>图**自己**就是按钮（`WindowButton` 挂在图上，**没有 `BackHit` 节点**）——
            /// `DailyStreakPopup` 那一档。</summary>
            BoundOnQuad,
        }

        /// <summary>建一扇「带返回钮的窗头」要的那些数 —— **每扇窗照抄它自己那一档的原值**，
        /// ⛔ 别为了「统一」而改动（这些差异本身就是原版那一族的几个态）。</summary>
        public class Spec
        {
            /// <summary>根节点名：<see cref="RootNameBack"/>（3 扇）或 <see cref="RootNameGameMode"/>（遭遇战 / 排位族）。</summary>
            public string RootName = RootNameBack;
            /// <summary>顶栏根那一格。⚠️ 各窗自己的 rect **逐值不同**（含 y 的零点），照抄。</summary>
            public PxRect RootRect;
            /// <summary>`Header Background`（底板；`Window Title` 挂在**它**底下）。</summary>
            public PxRect PlateRect;
            /// <summary>`Window Title`。</summary>
            public PxRect TitleRect;
            public string TitleText;
            public TitleFit TitleMode = TitleFit.FitAfterSpacing;
            /// <summary>自适应那两个量（画布 px）—— **按各窗自己那句原式给**：
            /// `TutorialModePopup` 给 `TitleR - TitleL` / `TitleB - TitleT`、
            /// `EnergySinglePlayerOnlyEventWindow` 给 `TitleR.W` / `TitleR.H`、`LiveOpsEventWindow` 给字面量。
            /// ⚠️ 别在共件里从 `TitleRect` 现算 —— 两种写法可能差 1 ulp，而这个差会静默改自适应结果。</summary>
            public float TitleFitW, TitleFitH;
            /// <summary>非空 = 调一次 `MenuDraw.SetVAlign`（`DailyStreakPopup` 是 `Capline`）；
            /// 空 = **不调**（= `Label` 出厂档 `Middle`，另三扇都是这一档）。</summary>
            public Label.VAlign? TitleVAlign;
            /// <summary>`Header Background (1)`（往左延伸的尖角）。</summary>
            public PxRect WingRect;
            /// <summary>尖角那一颗的**节点名**。缺省 <see cref="WindowHeader.WingName"/>（= `"Header Background (1)"`）。
            /// <para>🔴 **2026-10-18（A994① · 铁律 5 订正）**：原文（留痕）写着「⚠️ `LiveOpsEventWindow` 今天沿用
            /// `MenuDraw.Nine` 的缺省名 **`"Nine"`**（见 §5 的差异清单）」—— **那半句已不成立**：`A967` 把
            /// `Shell/LiveOpsEventWindow.cs` 的 `Spec` 里那一行 `WingName = "Nine",` **删掉了**
            /// （原版那颗尖角本名就是 `Header Background (1)`，判据 = 本常量那份 `menu_dump` 实读；
            /// 当年那一行是照**改前的实现行为**（`MenuDraw.Nine` 不传名）抄的，属于**与原版不符**）。
            /// ⇒ **四扇都用共件缺省 `Header Background (1)`，今天没有哪一扇用它覆盖**；
            /// 本字段**留给将来真出现差异的窗**（今天谁写 = 与缺省同值，等于没写）。</para>
            /// <para>⚠️ 断言侧：`Header Background (1)` 那颗由 `Editor/MainMenuScene.cs` 逐窗钉
            /// （教程窗 / 遭遇战（`LiveOpsEventWindow` 家族） / 能源活动窗各一条，期望值写**字面量**、⛔ 不读本字段）。</para></summary>
            public string WingName = WindowHeader.WingName;
            public PxRect BackRect;
            public BackStyle BackButtonStyle = BackStyle.QuadOnHeader;
            /// <summary>返回钮那张图的 `m_PreserveAspect`（三扇是 1、`DailyStreakPopup` 是 **0**）。</summary>
            public bool BackKeepAspect;
            /// <summary>返回钮的**常态图名**（悬停 / 按下那两张由它查表，⛔ 不是贴图）：
            /// 空 = 这一颗**没有换图**（`TutorialModePopup`）；`BoundOnQuad` 那一档也用它（`WindowButton.BindSelf`）。</summary>
            public string BackSwapArt;
            /// <summary>底板与尖角**同一张图**（四扇窗都是 `WF_Campaign_Info_Background`）。
            /// ⚠️ 传**贴图**、不传图名：各窗的取图口（`Tex` / `Art`）各自带 `MissingArt` 记账，收口时
            /// **不许**把那份记账换掉（换掉 = 取不到图时静默不建，而清单上看不见）。</summary>
            public Texture2D PlateTex;
            /// <summary>返回钮那张图（`UI_Button_Menu_Back`）—— 同上，走各窗自己的取图口。</summary>
            public Texture2D BackTex;
            /// <summary>各层的渲染队列（**逐窗不同**，照抄各窗自己的那一档常量）。</summary>
            public int QPlate, QWing, QTitle, QBack, QHit;
            /// <summary>点返回钮做什么。原版那条链 = `WindowHeaderWithBackButton.BackButtonPressed`
            /// → 各窗注册的 `UnityEvent` 回调（多数就是 `Close()`；`DailyStreakPopup` 还要先
            /// `DailyData.StreakAutoCollect()` —— 原版 `Close()` = `LiveOp.TryCollect(() => base.Close())`）。</summary>
            public System.Action OnBack;
        }

        /// <summary>建出来的那几件。**只给需要的窗用**：今天只有 `EnergySinglePlayerOnlyEventWindow`
        /// 要把它们存进字段（`_titleLabel` / `_title` / `_back` / `_backBg`），其余三扇不接返回值。</summary>
        public struct Parts
        {
            public Transform Root;       // 顶栏根
            public Transform Plate;      // `Header Background`（`Game Mode Icon` 那一颗也挂它底下）
            public Label Title;          // `Window Title`
            public Transform BackNode;   // 返回钮的宿主（`QuadInOwnNode` 那一档 = 子节点，其余 = 根）
            public ImageQuad BackQuad;   // 返回钮那张图
            public Transform BackHit;    // `BackHit`（`BoundOnQuad` 那一档**恒 null** —— 它没有命中区节点）
        }

        /// <summary>照 <see cref="Spec"/> 建那一扇窗头。</summary>
        public static Parts WithBackButton(Transform root, Spec s)
        {
            var parts = new Parts();
            var hdr = MenuDraw.Node(root, s.RootName, s.RootRect);
            parts.Root = hdr;

            // ---- `Header Background`（底板）：原版 `Sliced` · 九宫 `(335,0,395,0)` · 贴图 740×167
            //      ⚠️ **先 `Node(具名)`、再把 `Nine` 挂进它**（⛔ 不是直接 `Nine(hdr, …, "Header Background")`
            //      —— 那是「节点自己就是 quad」的另一种形状，原版这一颗**外面还有一层具名节点**）。
            //      F3·D3 红① 那个实现缺陷正是漏了这层名：`Nine` 落到缺省名 `"Nine"` ⇒ 自检读到「节点不在」。
            //      ⚠️ 两种挂法**位置逐位相同**：`MenuDraw.Nine` / `MenuDraw.Text` 都走 `Local(parent, 矩形)`
            //      = 矩形中心 − 父件位置 ⇒ 多插一层同矩形的 `Node` 之后仍等于原来的中心。
            var plate = MenuDraw.Node(hdr, PlateName, s.PlateRect);
            parts.Plate = plate;
            MenuDraw.Nine(plate, s.PlateTex, s.PlateRect, PlateBorder, PlateTexW, PlateTexH, s.QPlate);

            // ---- `Window Title`（原版 hAlign = **Left**，不是居中）
            var title = MenuDraw.Text(plate, s.TitleRect, s.TitleText, Color.white, TitleName, TitleFontPx, s.QTitle);
            parts.Title = title;
            if (title != null)
            {
                // 🔴 **次序不能反**：改**渲染宽度**的那两句必须排在 `AlignLeft` **之前**（判据见 `TitleFit`）。
                //    各窗那一档见 `s.TitleMode` —— 四扇里有三种，**都是原样保留的**。
                float fitW = LayoutSpace.Px(s.TitleFitW), fitH = LayoutSpace.Px(s.TitleFitH);
                switch (s.TitleMode)
                {
                    case TitleFit.FitBeforeSpacing:      // `EnergySingle…` 那一档
                        title.SetAutoFitBox(fitW, fitH, TitleAutoMinPx, TitleAutoMaxPx, TitleAutoBasePx);
                        title.SetCharSpacing(TitleCharSpacing);
                        break;
                    case TitleFit.FitAfterSpacing:       // `TutorialModePopup` / `LiveOpsEventWindow` 那一档
                        title.SetCharSpacing(TitleCharSpacing);
                        title.SetAutoFitBox(fitW, fitH, TitleAutoMinPx, TitleAutoMaxPx, TitleAutoBasePx);
                        break;
                    default:                             // `SpacingOnly` —— `DailyStreakPopup`
                        title.SetCharSpacing(TitleCharSpacing);
                        break;
                }
                // ⛔ 这里**不补** `ForceRelayout()` —— 与 `Shell/InboxWindow.cs` 那处（A471）不同：
                //    `AlignLeftOn` 自己头一句就是 `RefreshBounds()`（`Battle/Label.cs` 的 `AlignLeftOn`），
                //    那一次就按**含字距的** `textBounds` 重量了 `WorldW` ⇒ 再补一刀只是把同一件事做第二遍。
                //    ⚠️ 一旦哪一天把对齐挪到别处、而那里**不是** `AlignLeftOn`，这一刀就必须补回来。
                MenuDraw.AlignLeft(title, s.TitleRect);
                // ⚠️ `SetVAlign` 排在 `AlignLeft` **之后** —— 与 `DailyStreakPopup` 原来的次序逐句相同
                //    （两件事互不干涉：一个只动横向、一个只动纵向；`MenuDraw.AlignLeft` / `SetVAlign` 都 null 安全）。
                if (s.TitleVAlign.HasValue) MenuDraw.SetVAlign(title, s.TitleVAlign.Value, s.TitleRect);
            }

            // ---- `Header Background (1)`（往左延伸的尖角）
            MenuDraw.Nine(hdr, s.PlateTex, s.WingRect, PlateBorder, PlateTexW, PlateTexH,
                          s.QWing, null, true, s.WingName);

            // ---- `Header Back Button`（+ 它的命中区）
            var backHost = hdr;
            ImageQuad backQuad;
            if (s.BackButtonStyle == BackStyle.QuadInOwnNode)
            {
                backHost = MenuDraw.Node(hdr, BackName, s.BackRect);
                backQuad = MenuDraw.Rect(backHost, s.BackTex, s.BackRect, BackQuadInNodeName, s.QBack,
                                         null, s.BackKeepAspect);
            }
            else
            {
                backQuad = MenuDraw.Rect(hdr, s.BackTex, s.BackRect, BackName, s.QBack, null, s.BackKeepAspect);
            }
            parts.BackNode = backHost;
            parts.BackQuad = backQuad;

            if (s.BackButtonStyle == BackStyle.BoundOnQuad)
            {
                // 原版这一颗是 SpriteSwap（A17）：图**自己**就是按钮，**没有** `BackHit` 节点。
                if (backQuad != null)
                {
                    var wb = backQuad.gameObject.AddComponent<WindowButton>();
                    wb.onClick = s.OnBack;
                    wb.BindSelf(s.BackSwapArt);
                }
            }
            else
            {
                // 🔴 **`BackSwapArt` 为空时 `target` 也必须传 `null`** —— 只传 `target` 不传 `art`，
                //    `WindowButton.Bind` 会按 `HoverNameFor(null) = null` 去查一张并不存在的悬停图
                //    （在 `MissingSwapArt` 里记一条假账），并把 `tintOnHover` 关掉 ⇒ **这个按钮的悬停色偏没了**
                //    （`TutorialModePopup` 那一档今天就是「没有换图、靠色偏」）。
                parts.BackHit = MenuDraw.Hit(backHost, BackHitName, s.BackRect, s.QHit, s.OnBack,
                                             string.IsNullOrEmpty(s.BackSwapArt) ? null : backQuad,
                                             s.BackSwapArt);
            }
            return parts;
        }
    }
}
