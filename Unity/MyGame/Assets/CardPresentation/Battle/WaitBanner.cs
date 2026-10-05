// WaitBanner.cs — 「等待提示」（原版 `WaitText`）
//
// 原版出处：运行时 dump `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv:350-355`
//   `BackCanvas/Safe area BackCanvas/WaitText`  anchor (0.5,1) · anchoredPosition **(7.0, −175.1)**
//                                              size **1344.0 × 79.4** · activeSelf=**False**
//     ├── `Dark Shade`              size **3963.5 × 3366**（**无图，纯色**，`color = (0,0,0,0.6118)`）
//     └── `Generic Popup Background` size **1323 × 90**  图 `40k_popup`
//           ├── `Mask`                                图 `40k_popup`（拉伸填满）
//           │     └── `Background fill`               图 `40k_popup_texture`（128×128）
//           └── `Text`  anchor(0,1) pos(661.5,−45) size **1269.5 × 70**
//
// 🔴 **这份文档有两处是我们挑的，都写在下面**（原版查不到，别当成复刻）：
//   ① **什么时机显示** —— 原版 `WaitText` 的触发点在反编译与场景 JSON 里**都查不到**。
//      我们接的是「**不是我的回合、且不在换牌/结算**」，也就是对手思考的那段时间。
//   ② **文字文案** —— dump 里 `Text` 节点是**空的**（运行时才赋），原版的本地化 key 没解出来。
//      我们用 `CardText.Phrase("WAITING FOR OPPONENT")`（这一条是**我们加的**词条）。
//
// ⚠️ **底板：2026-09-17 已改回原版**（原来写「我们没九宫格 ⇒ 自建实底」，见 `Build()` 里那段更正）——
//    现在用 `MenuDraw.Nine`（`40k_popup`，Sliced）+ `MenuDraw.Tiled`（`40k_popup_texture`，Tiled）。
//    两件原版事实是从场景 JSON 里读出来的，不是猜的：
//    `Generic Popup Background.m_Type = 1`（Sliced）、`Background fill.m_Type = 2`（Tiled）。
//    🔴 **2026-10-04（A50③）两层的路都收口到 `MenuDraw` 了**（原来直调 `ImageQuad.Create*` = 绕开公共件、
//    拿不到 `clip`）：平铺那次是 A25⑤、框架这次。⚠️ 中心值给的是 `PopupRectPx()`（画布 px），
//    **z 由 `Build()` 里那两行 `localPosition` 显式补回来**（`MenuDraw.Local` 给的 z 恒为 `0 − 父件 z`）。
using UnityEngine;

namespace CardPresentation
{
    public class WaitBanner : MonoBehaviour
    {
        // ---- 原版字段（1080p 下 108 px = 1 世界单位，和 `SettingsPanel` 同一套换算）----
        const float BarDx = 7f, BarDy = -175.1f;     // `WaitText` 的锚 (0.5,1) 起算
        static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.6118f);   // 原版 `Dark Shade` 的色

        /// <summary>z —— 和 `SettingsPanel` 同一层（比结算面板靠前、比任何卡靠前）</summary>
        const float Z = -0.45f;

        static float U(float px) { return px / 108f; }

        ImageQuad _shade;
        GameObject _popup;          // 原版 `Generic Popup Background`（九宫格框 + 平铺填充）
        GameObject _fillRoot;       // 填充那层的父节点（单独挂着，自检好分开数）
        Label _text;

        /// <summary>原版 `Generic Popup Background` 的尺寸（**1323×90**）——
        /// ⚠️ 注意它**不是** 父节点 `WaitText` 的 1344×79.4：真正画出来的是这个子节点。</summary>
        const float PopupW = 1323f, PopupH = 90f;

        /// <summary>🔴 **根节点（原版 `WaitText`）自己的矩形** —— 2026-10-11（A218）为写 `sizeDelta` 立的常量，
        /// 值 = 运行时 dump 里 `WaitText` 的 `m_SizeDelta = (**1344.0, 79.44**)`
        /// （`runtime_ui_dump_drive_0912.tsv:350-355`；与 `bundle_scenes_scenes_battlearena1` 的
        /// `WaitText` 实读 `sizeDelta (1344.0000, 79.4400)` · `anchor (0.5,1)` · `ap (7.0, −175.1)` 逐位一致）。
        /// ⛔ 别拿 `PopupW/H` 顶替 —— 那两个是**子节点** `Generic Popup Background` 的。</summary>
        const float RootW = 1344f, RootH = 79.44f;

        /// <summary>这一层用的**渲染队列**。🔴 **必须显式给** —— 本件原来一次都没调过
        /// `SetRenderQueue`（= 材质默认档），而 `MenuDraw.Tiled` **会写**队列：
        /// 不传就等于在战斗现场**顺手换了一次排序**。3000 = `Sprites/Default` 的默认档
        /// （本件原本就是它），也是战斗侧 `CardDisplayWindow.QChrome` 那一档。</summary>
        const int BattleQChrome = 3000;

        /// <summary>提示条在**画布像素**里的矩形 —— 原版 `WaitText` 锚 `(0.5,1)` +
        /// `anchoredPosition (7.0, −175.1)`（`runtime_ui_dump_drive_0912.tsv:350-355`）
        /// ⇒ 中心 = (960 + 7, 175.1)（**自上而下量**），宽高取上面那对。
        /// ⚠️ 给 `MenuDraw.Nine` / `MenuDraw.Tiled` 用（它们吃画布 px、不吃世界坐标）。
        /// 🔴 **现在只有宽高是必需的**：两层的落位都由 `Build()` 里随后那行 `localPosition` 显式覆盖
        /// （九宫格/平铺的块都按**根节点的局部原点**铺，与矩形中心值无关 —— 那两行注释写了为什么）。</summary>
        static PxRect PopupRectPx()
        {
            float px = LayoutSpace.DesignPxW * 0.5f + BarDx, py = -BarDy;
            return new PxRect(px - PopupW * 0.5f, py - PopupH * 0.5f, px + PopupW * 0.5f, py + PopupH * 0.5f);
        }

        public bool Visible { get { return _popup != null && _popup.activeSelf; } }
        /// <summary>自检用：现在条上写的字</summary>
        public string ShownText { get { return _text != null ? _text.Text : "<无>"; } }
        /// <summary>自检用：提示条的**世界尺寸**（断言它等于原版 `Generic Popup Background` 的 1323×90 px）</summary>
        public float BarWorldW { get { return U(PopupW); } }
        public float BarWorldH { get { return U(PopupH); } }
        /// <summary>自检用：底板取到图了没有。
        /// 🔴 **必须数「框」自己的块，不能数 `_popup.transform.childCount`** ——
        ///    2026-09-17 第一版就是这么写的，结果**框没建起来（贴图取空了）它照样绿**，
        ///    因为填充那些块也挂在同一个父节点下（典型的「尺子自己会坏」）。</summary>
        public bool BarHasArt { get { return BarFramePieces == 9; } }
        /// <summary>自检用：九宫格框建了几块（原版 `Sliced` 应当是 **9**）</summary>
        public int BarFramePieces
        {
            get
            {
                if (_popup == null) return 0;
                int n = 0;
                foreach (Transform c in _popup.transform)
                    if (c.name.StartsWith("wait_popup_")) n++;
                return n;
            }
        }
        /// <summary>`40k_popup_texture` 的**平铺格宽**（**画布 px**）= **64**。
        /// 🔴 判据 = 原版那条 `Background fill` 的 `m_PixelsPerUnitMultiplier = **2.0**`：
        ///    uGUI 的格宽 = `(m_Rect.width − 左右 border) ÷ multipliedPixelsPerUnit`
        ///    （`Image.cs:756`：`multipliedPixelsPerUnit = pixelsPerUnit × m_PixelsPerUnitMultiplier`；
        ///      `:1232`：`tileWidth = (spriteSize.x − border.x − border.z) ÷ multipliedPixelsPerUnit`），
        ///    参考分辨率 1920×1080 + Canvas `m_ReferencePixelsPerUnit = 100` + sprite `m_PixelsToUnits = 100`
        ///    ⇒ `pixelsPerUnit = 1` ⇒ **128 ÷ 2 = 64**。
        /// 本工程另有 5 处同一张图的先例都传这个数（`PromptPopup.FillTilePx` · `ImportDeckPopup.FillTilePx` ·
        /// `MissionRerollPopup` · `ProfileTab` · `DuelPopupWindow`，各自那条注释里都写着同一个推导）。
        /// ⚠️ **本件原来传的是 `128f`**（= 把 `m_Rect.width` 直接当节距、漏了 `ppuMul`）—— 它是**全工程唯一的例外**，
        /// 画面后果是**格子大一倍**（11 块 vs 42 块）。**2026-10-05 改回 64。**</summary>
        const float FillTilePx = 64f;

        /// <summary>自检用：填充层**建了几块**（原版 `Tiled`：1323÷64 向上取整 × 90÷64 向上取整 = **21×2 = 42**）。
        /// 🔴 **2026-10-04 修（A50④）**：原来数的是 `_fillRoot.transform.childCount` —— 那是 `MenuDraw.Tiled`
        ///    返回的**根节点**（`CreateTiled` 把块平铺挂在它下面）⇒ **恒等于 1**，
        ///    而 `Editor/BattleScene.cs` 那条断的是 `>= 1` ⇒ **改坏块数一条都不会红**（弱断言，等于没查）。
        ///    现在数**真正的 quad 块**（`ImageQuad` 子件）。
        /// 🔴 **2026-10-05 就地订正**：本段原来写「节距 = 贴图 **128** px ⇒ **11** 块」—— **那个节距是错的**
        ///    （漏了 `m_PixelsPerUnitMultiplier`，见 `FillTilePx` 的注释）。改成 64 之后：
        ///    · 1323 ÷ 64 = 20.67 → **21 列**；90 ÷ 64 = 1.41 → **2 行** ⇒ **42**。
        ///    ⚠️ 节距（画布 px）与 `ImageQuad.PixelsPerUnit = 108`（画布 px / 世界单位）**同一个口径**，
        ///      和本件 `U()` 那套换算是同一个换算。
        /// ⚠️ 别数 `_fillRoot` 自己（空节点、没有 `ImageQuad`）；也别回头去数 `childCount`。</summary>
        public int BarFillPieces
        {
            get
            {
                if (_fillRoot == null) return 0;
                int n = 0;
                foreach (var q in _fillRoot.GetComponentsInChildren<ImageQuad>(true)) if (q != null) n++;
                return n;
            }
        }
        /// <summary>自检用：压暗层的颜色（断言 α = 原版 0.6118）</summary>
        public Color ShadeTint { get { return _shade != null ? _shade.Tint : Color.clear; } }
        /// <summary>自检用（**A276**）：框那一层写进去的**局部 z**（没建出来 ⇒ `NaN`）。
        /// 判据 = 它**必须只由本件的 `Z` 决定**，不许随父节点的世界 z 变 —— 同父的 `_shade`/`_text`
        /// 都是裸局部 z ⇒ 同一父节点下只该有一套口径（判据全文 → `资料/普查产出_1010/V4b_三件口径.md` §Q2）。</summary>
        public float PopupLocalZ { get { return _popup != null ? _popup.transform.localPosition.z : float.NaN; } }

        public static WaitBanner Create(Transform parent)
        {
            return New(parent, CardArt.DeckUi("40k_popup"), CardArt.Ui("40k_popup_texture"), "WaitBanner");
        }

        /// <summary>🔴 **自检专用**（`Editor/BattleScene.cs` 的「美术取不到那一档」那条）：照**同一段 `Build`** 建一份，
        /// 只是两张图由调用方给 —— 传 `null` 就是「美术目录被删 / 图取不到」那一档（本工程明确支持的退路）。
        /// ⛔ 生产路（`Create`）别改走它：那边必须自己去 `CardArt` 取图（`Create` 就是它的唯一调用方）。</summary>
        public static WaitBanner CreateWithArt(Transform parent, Texture2D popupTex, Texture2D fillTex)
        {
            return New(parent, popupTex, fillTex, "WaitBannerNoArt");
        }

        /// <summary>建这条提示的根节点。🔴 **2026-10-07（A92）：根节点是 `RectTransform`**（原来是裸 `Transform`）。
        /// **判据 = 原版这一件本来就是 `RectTransform`**：`bundle_scenes_scenes_battlearena1` 里
        /// 同名件 `WaitText` 的组件实读就是 `RectTransform`（那个场景 988 `RectTransform` / 236 裸 `Transform`，
        /// 裸的那批只有卡框 3D 锚、`HandArea`/`Board Center` 一类 3D 挂点与粒子件）。
        /// 位置/尺寸不受影响：本件摆位走 `MenuDraw.Local`（= 世界坐标差），与节点类型无关。
        /// ⛔ 别写成 `AddComponent&lt;RectTransform&gt;()`。</summary>
        static WaitBanner New(Transform parent, Texture2D popupTex, Texture2D fillTex, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            // 🔴 **2026-10-11（A218）**：根节点写 `sizeDelta` —— 判据 = 原版 `WaitText` 自己那对
            //    `(1344, 79.44)`（见 `RootW/RootH` 的注释）。⚠️ 真正画出来的是子节点 `Generic Popup Background`
            //    的 1323×90（`PopupW/H`），两者**不是**同一个数，⛔ 别互相顶替。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「`WaitText` 根 = 1344×79.44」红。
            MenuDraw.SetPxSize(go.transform, RootW, RootH);
            var b = go.AddComponent<WaitBanner>();
            b.Build(popupTex, fillTex);
            b.SetVisible(false);
            return b;
        }

        void Build(Texture2D popupTex, Texture2D fillTex)
        {
            // ---- 整屏压暗（原版 `Dark Shade`：无图、纯色、α 0.6118）----
            _shade = ImageQuad.Create(transform,
                                      SettingsPanel.SolidTex(LayoutSpace.VisibleWidth / LayoutSpace.DesignHeight),
                                      new Vector3(0f, 0f, Z + 0.02f),
                                      LayoutSpace.DesignHeight * 1.05f,
                                      new Vector2(0.5f, 0.5f), "wait_shade");
            if (_shade != null) _shade.SetTint(ShadeColor);

            // ---- 提示条：位置与尺寸照原版（`runtime_ui_dump_drive_0912.tsv:350-355`）----
            //   `WaitText`(1344×79.4, 只是个容器)
            //     └ `Generic Popup Background` **1323×90** = Image `40k_popup`
            //         · `m_Type = 1`（**Sliced**）、`m_Border = (169,160,169,160)`、`m_Rect = 359×336`
            //         └ `Mask` + `Background fill` = Image `40k_popup_texture`
            //             · `m_Type = 2`（**Tiled**）、`m_Rect = 128×128`、`m_Color` 纯白
            //         └ `Text` 1269.5×70（原版 `m_fontSize = 0` ⇒ 字号取不到，见文件头）
            // 🔴 **2026-09-17 更正**：原来这里写「我们**没有九宫格**，`40k_popup` 硬拉到 1344×79.4 会横向糊成
            //    一条 ⇒ 改用自建实底」，**那是我们挑的**。现在 `ImageQuad.CreateNineSlice / CreateTiled`
            //    补上了这两个原版的 `Image.Type`，就照原版画。
            float cx = U(BarDx);
            float cy = LayoutSpace.DesignHeight * 0.5f + U(BarDy);
            // ⚠️ **两张图在工程的**不同**目录里**：`40k_popup` 在 `Art/ui_deck/`（`CardArt.DeckUi`），
            //    `40k_popup_texture` 在 `Art/ui/`（`CardArt.Ui`）—— 2026-09-17 第一次取错目录，
            //    九宫格静默没建（断言当时也没抓住，见 `BarFramePieces` 那段注释）。
            // 🔴 **2026-10-04（A50③）：框架这一层也收口到公共件 `MenuDraw.Nine`** ——
            //    原来直调 `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，拿不到 `clip`：滚动视口里
            //    画九宫格会一直画到视口外）。两处**逐项等价**，别改：
            //    ① **尺寸 = `PopupRectPx()` 的宽高**（1323×90 px）—— 矩形中心是**占位**：九块是按
            //       **根节点的局部原点**铺的（尺寸从 `−尺寸/2` 起算），根节点摆哪由下面那行 `localPosition` 定。
            //       ⛔ 别删那一行：`MenuDraw.Local` 走的是「按可见宽拉伸」的画布映射，16:9 下与 `cx/cy`
            //       逐位相同、非 16:9 会差几个 px ⇒ 不覆盖就会**框和字对不上**（字这一层一直用 `cx/cy`）；
            //    ② **渲染队列 = 3000**（`BattleQChrome`）—— 原来一次都没显式设过（= 材质默认档，正是 3000）。
            _popup = MenuDraw.Nine(transform, popupTex, PopupRectPx(),
                                   new Vector4(169f, 160f, 169f, 160f), 359f, 336f,
                                   BattleQChrome, name: "wait_popup");
            if (_popup == null)
            {
                // 🔴 **2026-10-05 就地订正（A71①）：这一支必须连填充层一起跳过 —— 原来那条 NRE 路径就在下面。**
                //    改走公共件之前，框这一层直调 `ImageQuad.CreateNineSlice`，它 **从不返回 null**
                //    （`Battle/ImageQuad.cs:316`：`tex == null` 只打警告、**仍返回 root**）
                //    ⇒ 下面那句 `_fillRoot.transform.SetParent(_popup.transform, …)` 是**死码、永远安全**。
                //    现在框走 `MenuDraw.Nine`，而它 **`tex == null` 时返回 null**（`Shell/MenuDraw.cs:758`）
                //    ⇒ 同一句变成一条真正的 NRE：「美术目录被删 / 图取不到」那一档从
                //    「打警告 + 退化」变成**抛 `NullReferenceException`**。
                //    ⛔ 别再把建填充那几行挪回 `if` 外面（同批另外三处都判空/早退：
                //      `Battle/SettingsPanel.cs:197` · `Battle/WfSlider.cs:103-115` · `Core/Tooltip.cs:296/309`）。
                Debug.LogWarning("[WaitBanner] 提示条底板没建起来（`40k_popup` 没取到？）—— "
                                 + "填充层一并跳过（它挂在框下面）；整条提示不显示（`Visible` 恒假）。"
                                 + "取图请跑 `工具/import_original_art.py`");
            }
            else
            {
                // x/y = `cx/cy`（本件原来那对值，与下面 `_text` 同一口径）；
                // z = `Z` —— **裸局部 z**，与**同父**的 `_shade`（`Z + 0.02`）· `_text`（`Z − 0.01`）
                // 同一套口径（本件 z 越负越靠前）。
                // 🔴 这一行**必须**覆盖 `MenuDraw.Nine` 给的那个局部坐标：它走 `MenuDraw.Local`
                //    （`Shell/MenuDraw.cs:25`：`RectCenter − parent.position`），父件在世界原点时那个 z 是 `0`
                //    ⇒ 不覆盖的话框会落到 z = 0、**比压暗层还靠后**。
                // ⚠️ **A276（2026-10-11）把这里从 `Z - transform.position.z` 改成裸 `Z`** ——
                //    这是**收口径**（同一父节点下只留一套口径），**不是「对齐原版」**：这三层的
                //    **原版 z 关系没有判据**（`Z` 这个常量本身是我们挑的，见 `资料/普查产出_1010/V4b_三件口径.md` §Q2）。
                //    两种写法**今天逐位相同**（`transform.position.z ≡ 0`、`lossyScale.z ≡ 1`）⇒ 零观测风险；
                //    改法 = 与兄弟件同口径后，「父链被挪 z」时这一层不会再跟着漂。
                //    改坏法：写回 `Z - transform.position.z` ⇒ `Editor/BattleScene.cs` 那条 A276 探针断言红。
                _popup.transform.localPosition = new Vector3(cx, cy, Z);

                // 填充在**框后面**（我们这个坐标系的 z 越负越靠前）—— 原版它是被 `Mask` 裁在框里的。
                // 单独挂一个子节点，好让自检能分开数「框几块 / 填充几块」。
                // 🔴 **2026-10-04（A25⑤）**：原来在这里直调 `ImageQuad.CreateTiled` —— 那是**绕开公共件**
                //    的平铺路（`MenuDraw.Tiled` 才带 `clip`），已收口。两处**逐项等价**，别改：
                //    ① **渲染队列 = 3000** —— `MenuDraw.Tiled` 会**显式写**队列，不传就等于在战斗现场
                //       换一次排序。3000 = `Sprites/Default` 的默认档（本件原来就是它），也是战斗侧的
                //       `CardDisplayWindow.QChrome`；
                //    ② z 补回 **+0.01** —— ⚠️ **2026-10-04 就地订正（F13）：理由原来写反了**。`MenuDraw.Tiled`
                //       给的根节点 z 走 `MenuDraw.Local` = `RectCenter 的 z(恒 0) − 父件 z`，而**父件就是框**
                //       （`_popup`，世界 z = `Z` = −0.45）⇒ 它会把这一层摆到 **z = 0**；本件的坐标系
                //       **z 越负越靠前**（见上面 `_popup` 那一行）⇒ 0 是**更靠后**、不是「更前面」。
                //       这一行 `+0.01` 是**把填充压到框后面**（框 −0.45 → 填充 −0.44）；**不补**的话它落在
                //       **和框同一层** z 上 ⇒ 同队列同距离，谁先画由排序/枚举决定（不是「跑到框前面」）。
                //       （这一行本身是对的，只有理由那句话方向反了。）
                _fillRoot = new GameObject("wait_fillRoot", typeof(RectTransform));
                _fillRoot.transform.SetParent(_popup.transform, false);
                // 🔴 **2026-10-11（A218）**：这一层是 `RectTransform` + 写 `sizeDelta` ——
                //    它的原版对应件是 `Generic Popup Background/Mask`（`bundle_scenes_scenes_battlearena1`
                //    实读：`Mask` 与它下面的 `Background fill` **两件都是 `RectTransform`**；
                //    `Mask` 是 stretch 锚 + `sizeDelta (0,0)` ⇒ 它的矩形 = **父件那块 1323×90**）。
                //    ⚠️ 节点名 `wait_fillRoot` **是我们自己起的**（原版那两级叫 `Mask` / `Background fill`）——
                //    名字要不要照原版是**另一件**（A92 报告 §四·2a 已登记），本轮不动。
                //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「填充层根 = 1323×90」红。
                MenuDraw.SetPxSize(_fillRoot.transform, PopupW, PopupH);
                _fillRoot.transform.localPosition = new Vector3(0f, 0f, 0.01f);
                var fillR = PopupRectPx();
                var fillGo = MenuDraw.Tiled(_fillRoot.transform, fillTex, fillR,
                                            FillTilePx, BattleQChrome, "fill");
                if (fillGo != null) fillGo.transform.localPosition = Vector3.zero;   // = `_fillRoot` 原位（见上②）
                else Debug.LogWarning("[WaitBanner] 填充层没建起来（`40k_popup_texture` 没取到？）");
            }

            _text = Label.Create(transform, CardText.Phrase("WAITING FOR OPPONENT"),
                                 new Vector3(cx, cy, Z - 0.01f), 4,
                                 new Color(0.95f, 0.93f, 0.88f), new Vector2(0.5f, 0.5f), "wait_text");
        }

        /// <summary>开 / 关。**重复调用同一个值不会重复设置**（`UpdateHud` 每帧都会调它）。</summary>
        public void SetVisible(bool on)
        {
            if (_popup == null) return;
            if (_popup.activeSelf == on) return;
            _popup.SetActive(on);
            if (_shade != null) _shade.gameObject.SetActive(on);
            if (_text != null) _text.gameObject.SetActive(on);
        }
    }
}
