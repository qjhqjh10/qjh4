// MainMenuRuntime.cs — 主菜单（阶段二第 1 层）的**唯一建界面处**
//
// ============================ 出处（唯一正本） ============================
// `资料/主菜单_原版规格.md` —— §一 整屏骨架 · §二 层×参数权威表 · **§五 绝对屏幕坐标（1920×1080，y 向下）**
// · §三② 出厂 vs 运行时实例化的分界线。下边每个常量后面都写了它出自哪一格。
//
// 🔴 **三条纪律**（都是这份施工图里踩出来的）：
//   ① **坐标一律照 §五** —— `主菜单全树.md` 那份**不能当坐标源**（它把父级拉伸节点算成 0×0 挂在 y=1080，
//      凡祖先链上有全屏拉伸的，高度退化成 sizeDelta、y 偏 +1080）；§五 的注释里有完整判据。
//   ② **布局组算出来的位置不是 JSON 值**：5 个导航钮（VLG sp −16.35 / padTop −5）、
//      `InboxBtn`/`Challenge`（HLG sp 9.75）—— §五 C/B 两表里标了「按 VLG/HLG 算」，**别回头去查 JSON**。
//   ③ **出厂 `activeSelf=False` 的一律不建**（`Feedback Button` / `Profile border` / `Tutorial highlight` …
//      —— 原版是运行时按状态开的，我们还没实现那套状态就先别摆一个常显的假件）。
//
// ⚠️ **已知缺口（本批没做，别当已完成）**：
//   · **模式卡**（`GameModes/Content` 里那些 535×414 的卡）—— 原版出厂 **0 子**、全靠 liveop 数据灌，
//     要先读 `bundle_menus_assets_all` 里卡 prefab 的内部结构 ⇒ 下一批。
//   · ~~**`Resources Container` 里的 5 个资源格**~~ ✅ **2026-10-13（A374）已建**（见下面的
//     `BuildResourcesBar` 一整套：原版建表序 / 常显 3 颗 / 显隐规则 / 上限机制 / 逐格几何都有出处）。
//     ⚠️ **还差 4 张币种小图标**（本地 `Resources/Art/ui_menu/` 只导了 2 张）—— 那几颗只画药丸 + 数字，
//     名字会进 `MissingArt`（`Build()` 末尾那条警告点名）。判据与修法 → `资料/普查产出_1013/W374_资源计数器.md` §六。
//   · **字号**：§二/§五 两张表**没有 TMP 的 fontSize** ⇒ 这里的字号是**按矩形高推的档位**，
//     **标注为「我们挑的」**，等补到原版字号再换（别把它当原版值）。
//     （⚠️ 顶栏那几颗资源计数器的字号**不在此列** —— 那是从 prefab 的 TMP 字段实读的。）
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>主菜单。挂在 `MainMenu.unity` 的根对象上。</summary>
    public class MainMenuRuntime : MonoBehaviour
    {
        public static MainMenuRuntime Instance { get; private set; }

        public readonly List<string> MissingArt = new List<string>();
        Transform _root;

        void Awake() { Instance = this; }

        void Start()
        {
            // 按 Play 的入口 —— **和自检是同一条路**（本工程的规矩）
            if (_root == null) Build();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // ============================================================ 坐标工具

        /// <summary>像素矩形（左上原点，**§五 的口径**）的中心 → 世界坐标。
        /// **public** 是有意的：自检要拿它算期望值 —— 两处各写一份换算 = 迟早不一致。
        /// 🔴 2026-09-23：实现**转发到 `LayoutSpace.RectCenter`**（阶段二「日常」那一层也要同一套换算，
        /// 判据只留一份）；签名保持不变，老调用点不用改。</summary>
        public static Vector3 Center(float x1, float x2, float y1, float y2)
            => LayoutSpace.RectCenter(x1, y1, x2, y2);

        /// <summary>像素高 → 世界高（1080px = 10 世界单位，见 `LayoutSpace`）。</summary>
        static float H(float y1, float y2) { return LayoutSpace.Px(y2 - y1); }

        /// <summary>按像素矩形摆一张图。**宽高都照表**（原版很多件是拉伸的，所以显式 `SetAspect`）。
        /// `tint` 传了就 `SetTint` —— 原版好几个件是「亮图 + `m_Color` 染暗」（见 §七 表一）。
        /// `keepAspect`（🆕 2026-09-27）= **按贴图宽高比内接**，与 `MenuDraw.Rect` 那条**同一套算法**。</summary>
        ImageQuad Rect(Transform parent, string art, float x1, float x2, float y1, float y2, string name, int q,
                       Color? tint = null, bool keepAspect = false)
        {
            // `art == null` = **纯色块**（原版那种「Image 没 sprite、只有 m_Color」的件，走 `CardArt.Solid()` + tint）
            var tex = art == null ? CardArt.Solid() : Art(art);
            if (tex == null) return null;
            var quad = RectTex(parent, tex, x1, x2, y1, y2, name, q, keepAspect);
            if (quad != null && tint.HasValue) quad.SetTint(tint.Value);
            return quad;
        }

        /// <summary>同上，但**贴图已经拿在手里**（头像那批不在菜单图库里、走 `CardArt.Cosmetics`）。
        /// `keepAspect = true` ⇒ 先把矩形**内缩**成贴图的宽高比再建（与 `MenuDraw.Rect:68-73` 同一条算法）。</summary>
        ImageQuad RectTex(Transform parent, Texture tex, float x1, float x2, float y1, float y2, string name, int q,
                          bool keepAspect = false)
        {
            if (tex == null) return null;
            if (keepAspect && tex.height > 0)
            {
                float sprAspect = (float)tex.width / tex.height, rectAspect = (x2 - x1) / Mathf.Max(1e-6f, y2 - y1);
                if (sprAspect > rectAspect) { float nh = (x2 - x1) / sprAspect, d = ((y2 - y1) - nh) * 0.5f; y1 += d; y2 -= d; }
                else { float nw = (y2 - y1) * sprAspect, d = ((x2 - x1) - nw) * 0.5f; x1 += d; x2 -= d; }
            }
            var quad = ImageQuad.Create(parent, tex, Center(x1, x2, y1, y2), H(y1, y2),
                                        new Vector2(0.5f, 0.5f), name);
            if (quad != null)
            {
                quad.SetAspect((x2 - x1) / (y2 - y1));
                quad.SetRenderQueue(q);
            }
            return quad;
        }

        /// <summary>按像素矩形摆一段文字（居中）。
        /// `fontPx` = **原版 TMP 的 `m_fontSize`**（UI 画布像素）—— TMP 的 `fontSize` 就是**字形 em 的尺寸**，
        /// 所以按「**汉字高 = `fontPx` 像素**」摆最准：内部走 `Label.SetGlyphHeight`，
        /// 而它用的是工程里**实测过的**换算 `TmpFont.WorldGlyphPerFontSize`（拿「国」字量的，汉字≈1 em）。
        /// 🔴 2026-09-22 实测更正：第一版用的是 `Label.SetFontSize(px/108)` —— **字会大到 2.7 倍**
        ///    （量出来：`Player Name` 才 32px 字号却渲出 **476.8px 宽 / 11 个字符 ≈ 每字 43px**）。
        ///    `fontSize` 到实际字形之间还差一层字体资产的换算，**别自己乘 108**。</summary>
        Label Text(Transform parent, string text, float x1, float x2, float y1, float y2, int scale,
                   Color color, string name, float fontPx = 0f, int queue = QText)
        {
            var lb = Label.Create(parent, text, Center(x1, x2, y1, y2), scale, color,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(queue);        // 🔴 顶栏那几段文字要跟着整条带子走（传 `QBarText`；2026-10-11 起那条带子**降到窗之下**）
            if (fontPx > 0f) lb.SetGlyphHeight(fontPx / 108f);
            return lb;
        }

        /// <summary>取图。菜单图分两批导的（0917 进 `ui_deck/`、0922 进 `ui_menu/`）⇒ `MenuUi` 自己会兜底。</summary>
        Texture2D Art(string name)
        {
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) MissingArt.Add(name);
            return t;
        }

        /// <summary>空节点（**`RectTransform`**）**＋ 原版那一件的 `sizeDelta`（宽高）**。
        /// 🔴 **2026-10-07（A92）**：原来是裸 `Transform` ⇒ 原版那些带矩形语义的容器节点表达不了。
        /// 🔴 **2026-10-11（A332）**：**尺寸走公共件 `MenuDraw.SetPxSize`**（= A218 收口那一份换算，
        /// ⛔ 别在别处再乘/除一次 108）；此前这 19 个调用点**只建节点、一个都没写 `sizeDelta`**
        /// ⇒「原版这一件有自己的矩形、我们表达不出来」（与 A218 修掉的是同一个缺陷）。
        ///
        /// <para>🔴 **本工厂【只写尺寸、故意不写位置】——这不是漏了，是本文件的一条不变式**：
        /// 本文件所有器件都把 `<see cref="Center"/>(…)`（= **绝对**设计空间的世界坐标）**当
        /// `localPosition` 用**（`Rect` / `Text` / `NavButton` 全走那条），而这条路成立的前提是
        /// **整棵树每个父级的 `position` 恒为零**（`BuildModeCard` 里那条注释记的就是它：
        /// `new GameObject` + `SetParent(parent, false)` ⇒ `localPosition` 出厂即零，一路到 `_root`）。
        /// ⇒ 把某个 `New` 出来的节点挪到它矩形的中心（= `MenuDraw.ApplyPxRect` 那样写位置），
        /// **它整棵子树会跟着平移**，Draft 卡那一堆绝对坐标全错。
        /// ⚠️ 所以这里**不收 `PxRect`**（收了就等于把「矩形中心」这条语义塞进来、却不能用）——
        /// 收 `(wPx, hPx)` 两个数，与 `MenuDraw.SetPxSize` 同签名。
        /// ⚠️ `SetPxSize` 自己保证**不动位置**（先存 `localPosition`、写完锚点/尺寸再放回去）
        /// ⇒ 与本条不变式不冲突（判据 → `MenuDraw.SetPxSize` 的注释）。</para>
        ///
        /// <para>判据与实读见 `MenuDraw.Node` 的注释；本文件这 **19 个 `New` 调用点**建出来的名字逐个在
        /// `bundle_scenes_scenes_mainmenuwarpforge` 里核过，**全是 `RectTransform`**
        /// （`Background` / `Navigation Panel` / `Buttons Container` / `Main Menu Navigation Button - *` /
        /// `Upper bar` / `TopBarButtons` / `Resources Bar` / `Player Profile` / `ChatPreview` / `GameModes` /
        /// `Viewport` / `Content` / `Hit` …；该场景 592 `RectTransform` / 105 裸 `Transform`，
        /// 裸的那 105 个同样只有卡框 3D 锚与粒子件）。⛔ 别写成 `AddComponent&lt;RectTransform&gt;()`。
        /// ⚠️ **（数字口径）** —— A92 那句「本文件建的这 **20 个**名字」是它自己的口径（数的是**名字**），
        /// 与本处的**调用点数 19** 不同源；⚠️ **两者都别拿去互推**（A332 复核：`grep "= New("` = 19 处调用 + 1 处定义）。
        /// ⚠️ 其中 `Hit` 两处（导航钮、模式卡）与模式卡本身**原版没有对应节点**（原版这两族靠
        /// uGUI 自己的射线、没有另建的命中区子件；模式卡是 liveop 运行时实例化的）⇒ 那三处传的是
        /// **我们自己的矩形**，已在调用点逐条标了「我们自己的」。逐条出处 → `资料/普查产出_1011/WB2_A332.md`。</para></summary>
        static Transform New(Transform parent, string name, float wPx, float hPx)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, wPx, hPx);
            return go.transform;
        }

        // ============================================================ §A332：容器节点的原版矩形（px）
        //
        // 出处（2026-10-11 现读，⛔ 不是照 §五 表抄的）：`bundle_scenes_scenes_mainmenuwarpforge` 里
        // 逐件的 `RectTransform` 字段 —— `m_AnchorMin/Max` + `m_SizeDelta` + `m_Pivot` + `m_AnchoredPosition`
        // 按整屏 1920×1080（左上原点、y 向下）逐级套出来的**绝对矩形**（父链缩放一律按 1、
        // 「全退化祖先」按整屏 —— 两条口径的完整说明与三处反证 → `资料/普查产出_1011/WB2_A332.md`）。
        // 🔴 **三处独立反证**（都与本文件调用点原有的注释逐位吻合 ⇒ 这套取数可用）：
        //   `SettingsBtn` 1803.11,4.64→1890.89,66.36 · `Player Profile` 0,0→528.65,211.07 ·
        //   `ChatPreview` 1475,85→1875,145 · `GameModes/Viewport/Content` 的内容高 945.5651（= 注释里那个 945.6）。
        // ⚠️ **只取宽高**（本文件的节点全部停在原点，见 `New` 的注释）—— 上面那些 x/y 只是取数过程的中间量。
        const float NavBtnW = 164.31f, NavBtnH = 169.68f;     // `Main Menu Navigation Button - *`（5 颗逐值相同）
        const float ModesW = 1752.9606f, ModesH = 1008.671f;  // `GameModes` 与它下面的 `Viewport`（原版同矩形）

        // ============================================================ 渲染队列

        // ⚠️ 分层用**渲染队列**、不用 z（`ImageQuad` 全是透明队列，按到相机的 3D 距离排序 ——
        //    屏幕中间的反而更近，会盖住边缘的；这个坑 `CLAUDE.md` §三 记着）
        // 🔴 2026-09-22 实测补充：**同一个队列 + z 都是 0 ⇒ 谁盖谁完全不确定**。
        //    第一版卡图用了和整屏渐变一样的 `QBg` ⇒ **Tutorial 那张卡被渐变盖住了**（Draft 那张碰巧没被盖）。
        //    所以每层都要一个**不同的**队列，差 1 也行。
        const int QBg = 2900, QCardArt = 2905, QPanel = 2910, QContent = 2920, QText = 2921, QOverlay = 2930;

        /// <summary>顶栏那面**头像盾牌框**单独一档（`QContent − 1`）。
        /// 🔴 `Player_Profile_Border` 那张图的**中心是不透明黑**（实测 RGBA (0,0,0,255)）⇒
        /// **立绘必须排在它之后**（队列更高），否则立绘被压成黑块。
        /// 原版兄弟序也是 `Highlight → Border → Image`（Image 最后 = 画在最上面）
        /// —— 实据 → `资料/说明书/04_界面UI/菜单全树.md:4888` 那一棵。
        /// ⚠️ 用「边框退一档」而不是「立绘进一档」：`QContent + 1` 就是 `QText`，
        /// 而 `Player Name` / `Player Level` 那两块**与头像框有重叠**（x 136.9~165.5 · 117.5~170.6）⇒ 会撞。
        /// 这一档只有那面盾用（`ProfileTab` / `RankedTab` 那边是各自一套 `L_*` 梯子，互不相干）。</summary>
        const int QAvatarFrame = QContent - 1;

        // ============================================================ 顶栏那一档（2026-10-11：降回**窗口之下**）
        // 🔴 **用户 2026-10-11 裁定：照原版 ⇒ 顶栏整条降到【所有窗 / 弹窗之下】**。
        //    这条**推翻了 2026-09-28 那次拍板**（那次照的是一张**二手实拍**：顶栏盖在窗上）。
        //    **四条独立判据**（全部指向「原版弹窗在顶栏【之上】」）：
        //      ① 兄弟序：`RectTransform_1540.m_Children` 里 **`3 - PopUp Holder` 排在 `Upper bar` 之后**
        //         （兄弟序在后 = 画在上）；
        //      ② 🔑 **最硬的一条**：`3 - PopUp Holder`（GO 45）挂的 **`Canvas_1075`** 是
        //         **`overrideSorting: true` + `m_ReceivesEvents: true` + `m_SortingLayerID 1054366423`**，
        //         而 `globalgamemanagers/TagManager/TagManager_3.json` 里 **1054366423 = `PopUps`（第 8 条）**、
        //         顶栏所在的根画布 **`Canvas_1077` = `Default`（第 1 条）** ⇒ **排序层索引压过 `order`**
        //         ⇒ **`PopUps` 永远在 `Default` 之上**（与兄弟序无关的另一套机制）；
        //      ③ 只有那个锚点多挂一颗专用 **`CustomRaycaster`**；
        //      ④ 名字阶梯 `10 / 5 / 15` + 选卡组弹窗两变体 `windowsPlacement = 15`。
        //    判据全文 → `资料/普查产出_1010/V4b_三件口径.md` §Q3 · `资料/普查产出_1009/查证V3_口径三件.md` §四·3。
        // 🔴 **后果（原版如此 —— 照做，⛔ 别当缺陷修）**：弹窗打开时**它自己那层压暗层盖住顶栏** ⇒
        //    顶栏那一条带子被压暗、**点它 = 关窗**（`PointerLayer.HitButton` 挑「渲染队列最大者」当赢家）。
        //    ⚠️ **连没有压暗层的窗也照样盖住顶栏** —— 那是判据②那个排序层决定的，不是缺陷。
        // 🔴 **改法**：**整条顶栏（`Upper bar` + `Player Profile` + 顶栏立绘 + `ChatPreview`）单独一档**，
        //    整体从 **3600–3604 降到 2994–2998**（旧值 → 新值，升序一一对应：
        //    `QBarPanel 3600→2994` · `QBarAvatarFrame 3601→2995` · `QBarContent 3602→2996` ·
        //    `QBarText 3603→2997` · `QBarOverlay 3604→2998`；**带内相对次序一字未改**）。
        //    **为什么落在 2994–2998**：顶栏之上紧邻的就是「窗 / 弹窗块」的最低档 ——
        //    `DailyRewardPopup.QShade` / `DailyStreakPopup.QShade` / `InboxWindow.QShade` = **3002**、
        //    `MainMenuSubmenuWindow.QPanel`（文件是 `Shell/MenuWindowBase.cs`）= **3005**，
        //    外加 `DeckRuntime.QSep` = **2999**（卡组编辑器那一套从 2999 起）
        //    ⇒ **2998 是「低过全部窗档、又不与任何既有档位相撞」的最高一个号**。
        //    ⚠️ **档位号本身是【我们自己起的梯子】**（原版没有「逐件渲染队列」这套机制 ——
        //    它靠兄弟序 + 排序层，见上面 ①②）—— 这句从第一版起就如实写着，**别改成「照抄原版」**。
        // ⚠️ **只降顶栏这一条带子**：左侧导航、模式卡、以及别处那些 `QPanel/QContent` 一律不动；
        //    尤其 `QOverlay`（2930）那几处**别一起动** —— 模式卡的边框与它的命中区用的是同一档
        //    （`BuildGameModes` 里 `:1118` / `:1137` 两处），它们要压在**模式卡**之上，与弹窗无关。
        //    顶栏降到 2994–2998 之后**仍然高于**它们（2930 / 2931）⇒ 菜单内部的相对次序没变。
        // 📌 **保留历史痕迹**（铁律 5 —— 更正痕迹不许抹掉）：2026-09-28 那一轮**是照一张二手实拍**做的
        //    （那张实拍里顶栏看得见、选卡组弹窗那两个页签条 y 35.07~107.25 **看不见**），而**当时就写在本段里的
        //    那条「反证」**——「原版 `Safe area Only Horizontal` 的 `m_Children` 里 `3 - PopUp Holder`（RT1121）
        //    排在 `Upper bar`（RT1092）之后 ⇒ 单看兄弟序，弹窗本该在顶栏之上」（出处 →
        //    `资料/主菜单_原版规格.md:62-64`）—— **一直是对的**。当年是「实况优先于解包字段」（铁律 4）
        //    压过了它，⛔ 而那张实拍**不是实况取证**；2026-10-11 用户裁定「照原版」⇒ **兄弟序 + 排序层这一侧胜出**。
        public const int QBarPanel = 2994, QBarAvatarFrame = 2995, QBarContent = 2996,
                         QBarText = 2997, QBarOverlay = 2998;

        // ============================================================ 建

        /// <summary>建整个主菜单。**自检与运行时调同一个**。</summary>
        public void Build()
        {
            _root = transform;
            for (int i = _root.childCount - 1; i >= 0; i--) DestroySafe(_root.GetChild(i).gameObject);
            MissingArt.Clear();

            // ============================================================ 🆕 2026-10-12（A384）每日重置那一拍
            //
            // 🔴 **为什么落在这一处**：原版那一拍是**后端到点下发** —— 信号 `MissionResetSignal` 全客户端
            //    **只登记不发**（`d:/2/tools/il2cpp_out/script.json` 的 `Addresses` 段 = 二进制里全部泛型实例，
            //    37 个 `Signal.Raise<T>()` 里没有它；`Register<MissionResetSignal>()` 在），
            //    客户端只有登记方 `SkullsCount.OnReset()`。我们**没有服务器** ⇒ 只能自己挑一个「最接近
            //    『后端到点下发』」的时刻来判 —— **进壳 / 回主菜单**（= 本函数；调度台裁的口径 (a)）。
            //    判据全文 → `资料/普查产出_1012/V8_判据补查.md` §A384。
            // 🔴 **放在最前面**：下面的件（顶栏红点 `DailyData.RewardsHasBadge` 等）都是**按每日状态画的**
            //    ⇒ 重置必须发生在**画之前**（放在函数末尾 = 这一帧画的是重置前的旧状态）。
            // ⚠️ **判据用本机时间**（`DailyData` 那一段里如实标注了「我们挑的」）—— 不是服务器日界。
            //    **改坏法**：删掉这一句 ⇒ `Editor/MainMenuScene.cs` 的「接线：进壳那一拍真的判了」那条红
            //    （`DailyData.DailyResetChecks` 不涨）。
            DailyData.TryDailyReset();

            BuildBackground(_root);
            BuildNavigationPanel(_root);
            BuildUpperBar(_root);
            BuildChatPreview(_root);
            BuildGameModes(_root);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Menu] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
        }

        static void DestroySafe(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { DestroyImmediate(go); return; }
#endif
            Destroy(go);
        }

        // ---- §五 A：整屏背景 = `Image(sprite=0)` + **双色渐变**（不是图、也不是 3D）----
        // 🔴 **2026-09-27 修：角度要传 188°，不是原版字段里的 82**（我们把方向画反过 —— 修前是「近乎垂直、上暗下亮」，
        //   原版是「近乎水平、左暗右亮」）。换算与实测表 → `资料/主菜单_原版规格.md` §A 那条「背景渐变的角度约定」：
        //   原版那个组件是 Asset Store 的 `UIGradient`，方向 = **`(sin θ, cos θ)`**；
        //   我们 `CardArt.Gradient` 用 `(cos θ, sin θ)` ⇒ 同一个 82 传进去差 90°；
        //   再翻 180°（我们助手的 `c1` 在 `t=0` 端，而原版亮色在**右侧**）⇒ **188**。
        //   ⚠️ **别把它记成「原版写的就是 188」** —— 原版字段就是 82，188 是换算到我们约定之后的值。
        void BuildBackground(Transform root)
        {
            // 🔴 **A332**：原版 `MainMenu/Background` 自己那一颗是 `anchor (0,0)-(1,1)` + `sizeDelta (0,0)`
            //   ⇒ 绝对矩形 = **整屏 1920×1080**（实读 RT 1089；它的父 `MainMenu` 也是整屏）。
            // 🔴 **2026-10-11（FX6）修**：实参原来写 `LayoutSpace.DesignWidth/DesignHeight` —— 那是**世界单位**
            //   （17.7778 × 10），而 `New` 的 `(wPx, hPx)` 收的是**设计像素**（`MenuDraw.SetPxSize` 内部再 ÷108）
            //   ⇒ **1/108 做了两遍**，`rect` 只剩 0.165 × 0.093 世界单位（`MainMenuScene.Run` 4 条红里的 2 条）。
            //   ✅ 改 px 字面量（与另外 16 个调用点同风格；等值常量是 `LayoutSpace.DesignPxW/DesignPxH`）。
            //   ⚠️ 紧接着下面 `ImageQuad.Create(…, LayoutSpace.DesignHeight, …)` 与 `SetAspect(DesignWidth/DesignHeight)`
            //      两处是**世界单位**的正确用法（那两条口要的就是世界单位），**没动、别顺手改**。
            var go = New(root, "Background", 1920f, 1080f);
            var quad = ImageQuad.Create(go, CardArt.Gradient(new Color(0.224f, 0.012f, 0.020f),
                                                             new Color(0.047f, 0f, 0.016f), 188f),
                                        Vector3.zero, LayoutSpace.DesignHeight,
                                        new Vector2(0.5f, 0.5f), "Gradient");
            if (quad == null) return;
            quad.SetAspect(LayoutSpace.DesignWidth / LayoutSpace.DesignHeight);
            quad.SetRenderQueue(QBg);
        }

        // ---- §五 C：左竖导航 ----
        /// <summary>🆕 **2026-10-10（A222）**：原版 5 个导航钮那**一格** `m_Colors.m_HighlightedColor` ——
        /// **浅蓝 `(0.7216981, 0.8151793, 1.0)`**（不是全库默认那个 0.9608 灰）。
        /// 出处：`bundle_scenes_scenes_mainmenuwarpforge` 的 5 个 GO `Main Menu Navigation Button - *`
        /// 上挂的 `EverguildToggle`，`m_Colors` 五颗逐值相同（`NOR` 纯白 / `PR` 0.7843137 / `SEL` 0.9607843
        /// —— 只有 HL 这一格不是默认值）。全文 → `资料/普查产出_1009/查证V3_口径三件.md` §二。</summary>
        public static readonly Color NavHoverKey = new Color(0.7216981f, 0.8151793f, 1f, 1f);

        /// <summary>当前选中的导航页（0=PLAY）。⚠️ **原版 5 个 `Selected highlight` 出厂都是 active=True**，
        /// 可见性由每个按钮上的 `Toggle`（`MB2199` / `toggleType=0`）驱动 ⇒ **语义是「选中态」**。
        /// 我们只画选中的那一个：**这是按语义的实现**，原版 `Toggle.graphic` 的取值去向没读过（如实标）。</summary>
        /// ⚠️ **实例字段，不是 static** —— static 会跨自检/跨场景残留（上一轮点到 REWARDS，下一轮
        ///    主菜单一建出来高亮就跑到第 4 个钮上）。
        public int SelectedNav = 0;

        /// <summary>换选中页（左竖导航）。**只有选中的那个画高亮**（语义同原版，见上）。</summary>
        public void SelectNav(int idx)
        {
            if (idx == SelectedNav) return;
            SelectedNav = idx;
            var old = Find("Navigation Panel");
            if (old != null) DestroySafe(old.gameObject);
            BuildNavigationPanel(_root);
        }

        void BuildNavigationPanel(Transform root)
        {
            // A332：原版 `Navigation Panel` = 191 × 1080（`anchor (0,0)-(0,1)` + `sizeDelta.x 191` ⇒ 整高）
            // 🔴 **2026-10-11（FX6）修**：第二实参原来写 `LayoutSpace.DesignHeight`（**世界单位** 10，不是 px 1080）
            //   ⇒ 高被除两次 108、`rect.height` 只剩 0.093。本行是干净的正对照：宽 `191f` 是 px ⇒ 那条断言本来就过。
            var p = New(root, "Navigation Panel", 191f, 1080f);
            // 🔴 底板：`White Square`（8×8，`m_Color` **是白的**）+ **同 GO 上的四角顶点色组件**（`MB1941`）
            //    ⇒ 只补 m_Color 补不出这块板，必须走顶点色（`ImageQuad.SetCornerColors`）
            var bg = Rect(p, "White_Square", -164.4f, 165.1f, 0.1f, 1145.9f, "Background", QPanel);
            if (bg != null)
                bg.SetCornerColors(new Color(0.019f, 0.007f, 0.007f),   // 左下 BL
                                   new Color(0.226f, 0.001f, 0.001f),   // 右下 BR
                                   new Color(0.189f, 0.005f, 0.000f),   // 右上 TR
                                   new Color(0.066f, 0.027f, 0.003f));  // 左上 TL
            // `Panel Shadow` = 亮图 × `m_Color (0,0,0,0.4667)` ⇒ **半透明纯黑**（这才是原版那条暗横条）
            Rect(p, "Smooth_background_lateral", -762.5f, 1166.2f, 511.7f, 581.7f, "Panel Shadow", QPanel,
                 new Color(0f, 0f, 0f, 0.46667f));
            // 两条分隔线：亮图 × `m_Color (0.647,0.380,0.263)` = **古铜**（`m_Type` = 1 Sliced）
            var sepTint = new Color(0.64706f, 0.38039f, 0.26275f, 1f);
            Rect(p, "40k_Separator_Fade_Sides_Vertical",   -2.5f,    0.3f,   71.0f, 1080.0f, "Separators Left", QPanel, sepTint);
            Rect(p, "40k_Separator_Fade_Sides_Vertical",  164.0f,  166.8f,   71.0f, 1080.0f, "Separators Right", QPanel, sepTint);

            // A332：原版 `Buttons Container` = 164.379 × 931.66（`sizeDelta (164.379, −148.34)` + 竖直 stretch）
            var holder = New(p, "Buttons Container", 164.379f, 931.66f);
            // 🔴 五个按钮的 y 是**按 VLG 算的**（spacing −16.35 · UpperCenter · padTop −5 · 不控子尺寸）
            //    —— §五 C 表；JSON 里它们的 pos 全是 (0,0)、5 个完全重合，**别回头去查**。
            // 字号/字色照 §七 表二：标签 `fontSize` **33**（`COLLECTION` 是 **30.45**）、`m_fontColor` **金 (0.9961,0.9294,0.7098)**
            // 🔴 **2026-10-10（A284）**：第 8 个实参 = **图标那格的高**（原来它同时当「宽」和「高」用 ⇒ Home 被画成正方形）。
            //    原版逐颗实读（`RT 1111`/`1240`/`1241`/`1242`/`1243`，五颗全部 `m_PreserveAspect = 0`）：
            //    **只有 `Home` 不是正方形** —— **156.349 × 137.435**（`apos (83.095, −71.70)` · `anchor (0,1)`
            //    ⇒ 顶边距按钮顶 **2.98**；我们传的 `iy1 = 146.4` 对按钮顶 143.4 正好是 3.0）；
            //    另外四颗是 **140 × 140**（宽 = `ix2 − ix1` = 152.8 − 12.8 = 140 —— 本来就对）。
            //    错在哪：原来 Home 的高也取 156.3 ⇒ **高多 18.865px（+13.7%）、中心低 9.45px**。
            //    判据全文 → `资料/普查产出_1009/查证V3_口径三件.md` §五·1。
            // 🔴 **2026-10-11（F2）就地订正（铁律 5）：`Shop`/`Rewards`/`Social` 的 `iy1` 各 −10px**
            //    （461.8→**451.8** · 615.1→**605.1** · 768.4→**758.4**）。原来那三个值**整列低 10px**，
            //    源头是错文档 `资料/主菜单_原版规格.md` 那三行（同一次一并订正）。
            //    🔴 **判据（自己重推过一遍，⛔ 不是照抄诊断）**：原版五颗「图标」的 RT **逐字节同一份写法** ——
            //       `RectTransform_1111/1240/1241/1242/1243.json`：`anchorMin = anchorMax = (0,1)`、
            //       `apos = (83.0949, −71.70)`、`pivot = (0.5,0.5)`、`sizeDelta = (156.349,137.435)`（`1111` = Home）
            //       / **(140,140)**（其余四颗）。⇒ **图标顶 = 钮顶 + 71.70 − 高/2** = 钮顶 **+1.70**（四颗方的一律）、
            //       Home **+2.9825**。钮顶 = 父 VLG（`MonoBehaviour` 里 `spacing = −16.350000381469727` ·
            //       `m_ChildAlignment = 1`(UpperCenter) · `m_Padding.m_Top = −5` · `ctrlH = 0`）+ 钮 RT 高
            //       `169.68` ⇒ 五颗钮顶 = **143.41 / 296.74 / 450.07 / 603.40 / 756.73**（步进 **153.33** = 169.68 − 16.35）
            //       ⇒ 图标顶 = **146.39 / 298.44 / 451.77 / 605.10 / 758.43**。
            //    ⚠️ `Collection` 那颗（298.4）**本来就是对的** —— 错只错在 `Shop` 往后那三颗，
            //    即「同一份表里前两颗照原版、后三颗 +10」。
            //    判据全文 → `资料/普查产出_1010/D1_十二条红诊断.md` §B-1。
            NavButton(holder, 0, "Home",       "40k_main_bt_play",       143.4f, 313.1f, "PLAY",       137.435f, 4.6f, 160.9f, 146.4f, 33f);
            NavButton(holder, 1, "Collection", "40k_main_bt_collection", 296.7f, 466.4f, "COLLECTION", 140.0f, 12.8f, 152.8f, 298.4f, 33f);
            // ⚠️ `COLLECTION` 传的是 **max 33**（不是原版存下来的 30.45）—— 那个 30.45 是**自适应之后的结果**，
            //    让 autosize 自己缩出来才和原版同一条路（它框里放不下，会自己缩小）。
            NavButton(holder, 2, "Shop",       "40k_main_bt_shop",       450.1f, 619.8f, "SHOP",       140.0f, 12.8f, 152.8f, 451.8f, 33f);
            NavButton(holder, 3, "Rewards",    "40k_main_bt_rewards",    603.4f, 773.1f, "REWARDS",    140.0f, 12.8f, 152.8f, 605.1f, 33f);
            NavButton(holder, 4, "Social",     "40k_main_bt_friends",    756.7f, 926.4f, "SOCIAL",     140.0f, 12.8f, 152.8f, 758.4f, 33f);
        }

        /// <param name="idx">第几个按钮（用来判「选中态」）。</param>
        /// <param name="y1">按钮顶。</param>
        /// <param name="ih">图标那格的**高**；宽由 `ix2 − ix1` 定（⚠️ **2026-10-10（A284）改名**：
        /// 原来这个位置叫 `iw`，而它**同时**被当成「宽」和「高」用 ⇒ 高被写死成 `ix2 − ix1`。
        /// 原版五颗里 **`Home` 不是正方形**（156.349 × 137.435）、其余四颗才是 140² —— 见上面调用点那段）。</param>
        /// <param name="ix1">图标左。</param>
        /// <param name="ix2">图标右。</param>
        /// <param name="iy1">图标顶（原版是「锚 (0,1) + `apos.y` − 高/2」换算出来的：`RT 1111`/`1240`–`1243`
        /// 五颗 `apos.y` 全是 **−71.70** ⇒ 140 高的那四颗 = 钮顶 **+1.70**、Home（137.435）**+2.9825**；
        /// 钮顶按父 VLG 算 —— 见调用点那段。⚠️ **2026-10-11（F2）**：原来写「都来自 §五 C 表」，
        /// 而那份表把 `Shop`/`Rewards`/`Social` 三颗**各写高 10** ⇒ 已改成照 RT 现推，别再回头引那张表）。</param>
        /// <param name="fontPx">原版 TMP 的 `m_fontSize`（画布像素）。</param>
        void NavButton(Transform parent, int idx, string name, string art, float y1, float y2, string label,
                       float ih, float ix1, float ix2, float iy1, float fontPx)
        {
            const float cx0 = 82.69f;      // 键的水平中心（原版 `Highlight` 的矩形是 -0.3..164.0 ⇒ 中心 81.85）
            var b = New(parent, "Main Menu Navigation Button - " + name, NavBtnW, NavBtnH);
            // 选中态高亮：亮底图 × `m_Color (1,0.0805,0,1)` = **正红**（只有选中的那个画）
            if (idx == SelectedNav)
                Rect(b, "40k_main_bt_selected_BW", -0.3f, 164.0f, y1, y2, "Selected highlight", QPanel,
                     new Color(1f, 0.08054f, 0f, 1f));

            var icon = Art(art);        // 先确认图在不在（不在就别画一个空按钮）
            ImageQuad iconQ = null;     // 🆕 A222：这颗**可见**的图形要交给 `WindowButton` 当色偏目标（见下）
            if (icon != null)
            {
                iconQ = ImageQuad.Create(b, icon, Center(ix1, ix2, iy1, iy1 + ih), H(iy1, iy1 + ih),
                                         new Vector2(0.5f, 0.5f), "Image");
                if (iconQ != null) { iconQ.SetAspect((ix2 - ix1) / ih); iconQ.SetRenderQueue(QContent); }
            }
            else MissingArt.Add(art);

            // `Text Background`(nametag) 的 y 偏移实证 = **+118.3**、`Badge` = **+91.7**（§五 C 表，五个按钮一致）
            Rect(b, "40k_main_bt_nametag", 9.3f, 156.2f, y1 + 118.3f, y1 + 157.7f, "Text Background", QContent);
            // ⚠️ 文案：原版 `m_text` 就是英文（`PLAY`/`COLLECTION`/…），运行时才被**本地化**替换。
            //    本地没有语言表 ⇒ **先用资产里的英文原文，不自己译**（`项目任务.md` §三 第 8 条那条口径）。
            // 🔴 原版这行 TMP 带 **`enableAutoSizing`（18→33）**，框只有 146.9px 宽 ⇒ `COLLECTION` 是**缩出来的**
            //    （实测原版它的在场字号就是 **30.45**）—— 不缩会画出框外被裁。
            var navText = Text(b, label, 9.3f, 156.2f, y1 + 118.3f, y1 + 157.7f, 6,
                               new Color(0.9961f, 0.9294f, 0.7098f), "Text", fontPx);
            // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版这一颗的 `m_fontSizeBase` **原文**。
            //    判据（本轮自己扫的原版场景 `bundle_scenes_scenes_mainmenuwarpforge`，逐颗现读）：
            //    五个导航钮的 `Text (TMP)`（`'PLAY'`/`'REWARDS'`/`'SHOP'`/`'COLLECTION'`/`'SOCIAL'`，
            //    `m_SizeDelta` 全是 **146.92 × 39.39**，与我们的框逐值相同）—— `auto[18~33]` ·
            //    **`m_fontSizeBase` 五颗全是 `24.0`**（`MonoBehaviour_1716/1749/1750/1751/1752`）。
            //    ⚠️ 与 V7 §二·3 #30 的关系：那一行记的是**全库** `[18~33]` 那族的 base 分布
            //    （24.0 ×4 / 33.0 ×3）⇒ 本条按**场景内实读**取 24（那 3 颗 33.0 不在主菜单这一族里）。
            if (navText != null) navText.SetAutoFitBox(146.92f / 108f, 39.39f / 108f, 18f, 33f, 24f);   // 原版 autosize 18→33
            // 红点：亮图 × `m_Color (0.7358,0.7358,0.7358,1)` = **中灰**（原版这就是「无内容/禁用」的灰点）
            // 🔴 **只有 REWARDS（第 4 个导航钮）有真实的通知源** —— 原版它由 `Missions.CheckNotification` 驱动；
            //    其余四个我们**没有通知源** ⇒ 按 `Hide()` 的样子 **alpha = 0**（不是画一个假的灰点）。
            Rect(b, "40K_notification_number", 117.8f, 152.8f, y1 + 91.7f, y1 + 126.7f, "Badge Highlight", QContent,
                 BadgeAlpha(idx == 3 && DailyData.RewardsHasBadge));

            // 点击区（整键）。原版每个导航钮上挂的是 `OpenWindowButton{windowToOpenPrefab, windowPayload}`：
            // `0 Main(PLAY)` 例外 —— 它开的是**场景内的** `MainMenuWindow`（正本 §三 第 1 条）。
            // A332：⚠️ **原版没有这个节点** —— 原版导航钮自己就是按钮（GO 上挂 `EverguildToggle` /
            //   `OpenWindowButton`），射线由 uGUI 按它自己的 `Graphic` 收；我们这套没有 uGUI 射线
            //   ⇒ 另建一个透明命中区。所以这里的矩形**是我们自己的**（= 下面那颗 quad 那一格：
            //   `cx0 ± 82` × `y1..y2`），⛔ 别去原版找「Hit」这个名字（那一族 5 颗子件是
            //   `Selected highlight` / `Image` / `Text Background` / `Badge Highlight`，没有 Hit）。
            var hitGo = New(b, "Hit", 164f, y2 - y1);
            var hq = ImageQuad.Create(hitGo, CardArt.Solid(),
                                      LayoutSpace.FromPixel(cx0, (y1 + y2) * 0.5f), LayoutSpace.Px(y2 - y1),
                                      new Vector2(0.5f, 0.5f), "Hit");
            if (hq != null)
            {
                hq.SetAspect((164f) / (y2 - y1));
                hq.SetTint(new Color(0f, 0f, 0f, 0f));
                hq.SetRenderQueue(QPanel);
            }
            int captured = idx;
            var wb = hitGo.gameObject.AddComponent<WindowButton>();
            wb.onClick = () => OnNavClick(captured);
            // ============================================================ 🆕 2026-10-10（A222）
            // 🔴 **色偏（悬停/选中/按下）要打在这颗【可见】的图形上** —— 那颗 `Hit` 是**另建的透明命中区**
            //    （`SetTint(0,0,0,0)`），`WindowButton.Collect()` 只收**自己子树**里的 quad
            //    ⇒ 原来色偏全打在透明那颗上、**画面上一点变化都没有**（等价于原版 `m_Transition = 0`，
            //    而原版这五颗是 **`trans = 1`(ColorTint)**）。
            // 判据（逐颗实读 `bundle_scenes_scenes_mainmenuwarpforge`）：5 个 `Main Menu Navigation Button - *`
            //    上挂的是 `EverguildToggle`，`m_Transition = 1`，`m_TargetGraphic` = **各自的直接子件 `Image`**
            //    （逐条解父链，**5/5 都在这颗 `Selectable` 自己的子树里** —— 一次也没打到「另建的命中区」上）。
            //    `m_Colors` 五颗**逐值相同**：`NOR` 纯白 · **`HL` = (0.7216981, 0.8151793, 1.0) 浅蓝** ·
            //    `PR` 0.7843137 · `SEL` 0.9607843 —— **只有 HL 这一格不是全库默认的 0.9608**
            //    ⇒ 这一颗必须逐颗覆盖（其余三格与默认同值，不用动）。
            // 判据全文 → `资料/普查产出_1009/查证V3_口径三件.md` §二。
            wb.HighlightKey = NavHoverKey;
            if (iconQ != null) wb.TintOn(iconQ);   // ⛔ 传 null 就是今天的行为（色偏打在透明那颗上）
        }

        /// <summary>
        /// 导航钮点下去。原版是 `OpenWindowButton`（`0` 走**场景内**的 `MainMenuWindow`，其余走 prefab）。
        /// 目前**只有 REWARDS 有去处**（阶段二第 2 层刚建完奖励窗）；其余如实说一声 —— **不许静默失败**。
        /// </summary>
        public void OnNavClick(int idx)
        {
            switch (idx)
            {
                case 1:   // COLLECTION —— 原版开 `Collection Menu Variant`（`资料/阶段二_卡组线_原版规格.md` §一）
                    OpenCollection();
                    break;
                case 2:   // SHOP —— 原版开 `Shop Menu Variant`（`资料/阶段二_锻造厂与战役页_原版规格.md` §十六）
                    OpenShop();
                    break;
                case 3:   // REWARDS —— 原版开 `MainMenuRewardsWindow`（`资料/日常_原版规格.md` §〇·1）
                    OpenRewards();
                    break;
                case 4:   // SOCIAL —— 原版开 `Social Submenu Variant`（= `SocialMenuWindow`）
                    // 🆕 2026-09-27：**这一条的入口是【复刻】，不是我们挑的** —— 证据链五跳（入口按钮的
                    // `OpenWindowButton.windowsToOpenPrefab` → Addressables 容器 → GO pid `354927014922018979`
                    // → 根组件 `SocialMenuWindow`）：判据全文 → `资料/普查产出_0927/多人界面_入口与调用.md` §①。
                    OpenSocial();
                    break;
                default:
                    Debug.Log($"[Menu] 左竖导航第 {idx + 1} 个钮（{NavName(idx)}）**还没接** —— " +
                              "原版开的是各自的窗口/场景，属阶段二后面的层");
                    break;
            }
        }

        static string NavName(int idx)
        {
            switch (idx) { case 0: return "PLAY"; case 1: return "COLLECTION"; case 2: return "SHOP";
                           case 3: return "REWARDS"; default: return "SOCIAL"; }
        }

        // ============================================================ 开窗入口（**按引用复用**）
        //
        // 🔴 **2026-10-06（A123）：本文件 9 个入口**（`OpenInbox` / `OpenSettings` / `OpenSocial` / `OpenChat` /
        //    `OpenLeaderboard` / `OpenProfile` / `OpenRewards` / `OpenCollection` / `OpenShop` —— **8 条走 `OpenByRef`**，
        //    只有 `OpenBattleLogPopup` 判据空、照旧新建）**改成【原版那套「按引用复用」】** —— 原来每点一次就
        //    `XxxWindow.Create(wm)` **新建一扇**，旧窗既不关也不藏 ⇒ 连点两次 = 两扇窗叠着
        //    （每扇各带一条整屏命中区 ⇒ 「点窗外关窗」谁吃到退化成枚举顺序）。
        //    `OpenCollection` / `OpenShop` 原来是「新建一扇 + `closeAll` 把上一扇收掉」⇒ 画面不叠，
        //    但**每次仍在换实例**（inactive 的旧窗越积越多）；现在与其余各条同一条路。
        //
        // **原版判据（三层，全部本地可复现）** → `资料/普查产出_1007/波6判据核查.md` §2 ·
        //   `资料/普查产出_1006/甲5_A97_A99_A104_A105.md` §二 A104（五跳证据链）：
        //   ① 缓存字段 = `WindowsManager.automaticallyLoadedWindows`
        //      （`BiDirectionalDictionary<ComponentReference<GameWindow>, GameWindow>`，**键 = prefab 引用**；
        //      签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs:170`）；
        //   ② `WindowsManager.OpenWindow` **第一件事就是查它** —— `TryGetValue` 在 `Instantiate` **之前**，
        //      命中 `jne` 直接跳复用块（实测 VA `0x180875990`）⇒ **同一扇窗点两次只有一个实例**；
        //   ③ 🔴 **命中只在「窗还开着」时发生**：`WindowsManager__CloseWindowCO.c:38-48` 关窗时会
        //      `Object.Destroy(gameObject)` + `ComponentReference.Release` + **把缓存条目删掉**
        //      ⇒ 关过之后再点，**原版走的是新建**。
        //
        // 🔴 **2026-10-11（A177 尾巴）：机制本体已收编到 `Shell/WindowsManager.cs`**
        //   （`OpenByRef` / `StillOpen` / `_openByRef` / `PrefabRef*` —— 那也正是原版那个缓存字段
        //    `automaticallyLoadedWindows` **所在的位置**）。本文件原来另有一份**逐字副本**
        //   （自己的 `_openByRef` + `StillOpen` + 自己那份 `OpenByRef`），两份缓存互不相认 ⇒
        //   「从社交窗开聊天」与「从主菜单右上角开聊天」会开出**两扇**（今天实测就是两扇；A177 尾巴修的正是它）。
        //   现在这里只剩**一条转调**。
        //   ⛔ **判据与「为什么」全部只写一处**（`WindowsManager` 那一段 —— 含「⛔ 别用『实例还在就复用』」
        //   那条）—— 别在这儿再抄第二份（两份迟早不一致）。
        //   ⚠️ 下面那几条 `Ref*` = 本文件这 8 条入口的 **prefab 根名**（与各自 `Create()` 里
        //   `new GameObject(...)` 用的那个名字同源 ⇒ 真·「按引用」）；与 `WindowsManager.PrefabRef*`
        //   **重复的那两条直接取那一份**（同一个字面量只留一个出处）。

        const string RefInbox = "Inbox Menu";
        const string RefSettings = "Main Menu Settings Window";
        const string RefSocial = "Social Submenu Variant";
        const string RefChat = WindowsManager.PrefabRefChat;             // = "ChatPanel"
        const string RefProfile = WindowsManager.PrefabRefProfile;       // = "Player Profile Window"
        const string RefRewards = "Rewards Base Submenu Variant";
        const string RefCollection = "Collection Menu Variant";
        const string RefShop = "Shop Menu Variant";
        // 排行榜那一条对**两棵** prefab（遭遇榜 / 经典榜）⇒ 键由 `LeaderboardWindow.NameOf(kind)` 给，
        // 那是「kind → prefab 根名」的唯一来源（`Create` 建 GO 用的也是它），别在这再抄一份。

        /// <summary>按 **prefab 引用**开窗 —— **原版 `automaticallyLoadedWindows` 命中就复用**的等价物。
        /// 🔴 **2026-10-11（A177 尾巴）：本方法现在只转调 `WindowsManager.OpenByRef`（唯一那一份实现）** ——
        /// 形参与语义**逐字一致**（8 个调用点一行都不用改），改的只是「谁的缓存」：原来本文件自己一份
        /// `_openByRef`、`WindowsManager` 自己一份 ⇒ **跨入口的去重失效**（同一扇窗从两条入口点开 = 两扇）。
        /// 判据与「为什么」→ `Shell/WindowsManager.cs` 的 `OpenByRef` 那一段（⛔ 别在这儿再抄第二份）。</summary>
        /// <param name="closeAll">照该入口原版 `OpenWindowButton.closeOtherMenus` 传
        /// （`OpenSocial` / `OpenRewards` / `OpenCollection` / `OpenShop` 四条是 1 ⇒ `true`）。</param>
        static T OpenByRef<T>(string prefabRef, System.Func<WindowsManager, T> create, bool closeAll = false)
            where T : GameWindow
            => WindowsManager.OpenByRef(prefabRef, create, closeAll);

        // ============================================================ 🆕 A77⑮② / A216：ESC 打在主菜单上

        /// <summary>ESC 落到**主菜单**上（= 原版 `MainMenuWindow.ESCPressed()`）——
        /// **唯一调用点 = `PointerLayer.KeyCancel` 的「一扇窗都没有」那一支**
        /// （原版那一刻 `currentWindow` 是**常驻的 `baseMenu` = `MainMenuWindow`**，而我们的主菜单
        /// **不是 `WindowsManager` 的窗** ⇒ 由这一支接）。返回「这一下有没有被用掉」。
        ///
        /// <para>🔴 **判据 = 反编译**（`d:/2/tools/decomp_full/MainMenuWindow__ESCPressed.c`，**全 body 就两句**）：
        /// `GameWindow__ESCPressed(param_1, 0); SettingsMenu__ExitGamePopup(0);`</para>
        /// <list type="number">
        /// <item>**① `base.ESCPressed()` 在这一扇上【什么都不做】** —— 实据 = 主菜单那颗窗组件的 `closeOnESC = 0`
        ///   （`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1927.json`，其
        ///   `m_Script.m_PathID = 6585963289051879589` 对上那批 monoscript 里的 `MainMenuWindow`；
        ///   同批实读 `type = 0`(Fullscreen) · `windowsPlacement = 0`(None) · `extraScaleSmallScreen = 1.0`)。
        ///   而 `GameWindow.ESCPressed` 的**第二道**门槛就是它（字段偏移 0x39）⇒ 不过 ⇒ 不关窗。
        ///   ⚠️ 第一道门槛（输入层启用）在那两个序列化的字节上都没出现 —— 那一句本来就不做任何事。</item>
        /// <item>**② `SettingsMenu.ExitGamePopup()`** = `WindowsManager.ShowPopUp("Demo/MainMenu/ExitGame",
        ///   localizeTexts: 1, closeOnEsc: **1**, primaryButton: {Text: "Demo/MainMenu/CancelButton"},
        ///   secondButton: {Text: "Demo/MainMenu/ExitButton"})`
        ///   （`SettingsMenu__ExitGamePopup.c` 逐句；三个串在 `il2cpp_out/stringliteral.json` 里逐字节实读 =
        ///   `0x4277558` / `0x4277360` / `0x4277460`；重载的形参名 →
        ///   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs:327-335` 那三个 `ShowPopUp`）。
        ///   ⚠️ 与别的调用点不同：这里 **`closeOnEsc = 1`**（那一族多数传 0）⇒ **这扇弹窗按 ESC 关得掉**。</item>
        /// </list>
        ///
        /// <para>⚠️ **两处如实标注（铁律 3）**：
        /// · **文案**：`localizeTexts = 1` 说的是「那几个串是本地化 key」，而**词条在远端语言表里**
        ///   （本地只有 key、没有英文原文）⇒ 三句中文**是我们写的**（先例 = `Shell/ShopData.cs` 的 `LegendaryWarnText`）。
        /// · **退出动作**：原版那个委托的目标方法（`DAT_1842aa360`）在 `decomp_full` /
        ///   `il2cpp_out` 的方法表里**查不到名字** ⇒ 我们按字面语义做 `Application.Quit(0)`
        ///   （先例 = `Core/PlayerBoot.cs:257`），并且**编辑器里 Quit 是空操作 ⇒ 出声**。</para></summary>
        public static bool EscapePressed()
        {
            var mm = InstanceOrFind();
            if (mm == null) return false;
            return mm.EscPressed();
        }

        /// <summary>`Instance` 没有就现找一台。
        /// 🔴 **编辑模式下 `Awake` 不跑**（只对 `[ExecuteAlways]` 的脚本跑，本类不是）
        /// ⇒ 自检里 `Instance` 可能是 null（本类原来只有 `Awake` 那一处赋值）。
        /// 写法与 `PointerLayer.Instance` 同一条：**惰性取用、不依赖任何生命周期回调**。</summary>
        static MainMenuRuntime InstanceOrFind()
        {
            if (Instance != null) return Instance;
            return Object.FindFirstObjectByType<MainMenuRuntime>();
        }

        /// <summary>见静态入口 <see cref="EscapePressed"/> 的判据。
        /// 本扇 `closeOnEsc = 0` ⇒ ① 那一跳不做，只做 ②。</summary>
        public bool EscPressed()
        {
            var wm = WindowsManager.Instance;
            if (wm == null)
            {
                // 出声（红线：不许静默失败）—— 「按了 ESC 没反应」必须能从日志里分辨
                Debug.LogWarning("[Menu] 主菜单收到 ESC，但场上没有 `WindowsManager` ⇒ 开不了「退出游戏」弹窗");
                return false;
            }
            var win = PromptPopup.Create(wm, ExitGameText, ExitGameOkText, QuitGame, ExitGameCancelText, null);
            // 🔴 原版那一刻传的是 `closeOnEsc: 1`（见 `EscapePressed` 的判据）—— `PromptPopup.Create` 的出厂值
            //    `false` 是**那一族 prefab 的值**（`GenericPromptWindow` 的 `closeOnESC = 0`），这一扇要按
            //    `ShowPopUp` 的**调用点**改过来。⛔ 别去改 `Create` 的出厂值：20 多处站点都吃它。
            win.closeOnEsc = true;
            wm.OpenWindow(win);
            Debug.Log("[Menu] 主菜单按 ESC ⇒ 开「退出游戏」弹窗"
                      + "（原版 `MainMenuWindow.ESCPressed` → `SettingsMenu.ExitGamePopup`）");
            return true;
        }

        /// <summary>退出窗的正文（原版词条 key = **`Demo/MainMenu/ExitGame`**，`localizeTexts = 1`）。
        /// 🔴 **词条在远端语言表里**，本地只有 key、没有英文原文 ⇒ 下面三句**都是我们写的**（铁律 3；
        /// 先例 = `Shell/ShopData.cs` 的 `LegendaryWarnText`，那句也是同一种处境）。</summary>
        public const string ExitGameText = "确定要退出游戏吗？";
        /// <summary>右钮文案（原版 key = **`Demo/MainMenu/ExitButton`** —— 原版把它当 `secondButton` 传）。**我们写的**。</summary>
        public const string ExitGameOkText = "退出游戏";
        /// <summary>左钮文案（原版 key = **`Demo/MainMenu/CancelButton`** —— 原版把它当 `primaryButton` 传）。**我们写的**。</summary>
        public const string ExitGameCancelText = "取消";

        /// <summary>「退出游戏」按下去做什么。
        /// 🔴 **原版那个委托的目标方法读不到名字**（`SettingsMenu__ExitGamePopup.c` 里是个数据地址
        /// `DAT_1842aa360`，`il2cpp_out` 的方法表按 RVA 查不到它）⇒ 我们按**字面语义**做 `Application.Quit(0)`
        /// （先例 = `Core/PlayerBoot.cs:257`）。⚠️ **编辑器里 `Quit` 是空操作** ⇒ 这里出声（红线：不许静默失败）。</summary>
        public static void QuitGame()
        {
            Debug.Log("[Menu] 「退出游戏」按下 ⇒ `Application.Quit(0)`"
                      + (Application.isEditor
                         ? "（⚠️ **编辑器里 `Application.Quit` 是空操作** —— 只有 build 出来的 player 里才真退）"
                         : ""));
            Application.Quit(0);
        }

        /// <summary>
        /// 开收件箱（原版 `Inbox Menu`，由顶栏 `InboxBtn` 上的 `OpenWindowButton` 开）。
        /// ⚠️ 单机没有服务器 ⇒ 里面是**空态**（原版没消息时也是这个样子），**不是没做**。
        /// 🔴 **2026-10-06（A123）：已经开着就复用那一扇**（判据 → 上面 `OpenByRef` 那段）—— 原来每次新建。
        /// </summary>
        public InboxWindow OpenInbox()
        {
            return OpenByRef(RefInbox, wm => InboxWindow.Create(wm));
        }

        /// <summary>
        /// 开设置窗（原版 `Main Menu Settings Window`，由顶栏 `SettingsBtn` 上的 `OpenWindowButton` 开）。
        /// 🔴 **2026-09-26 才有这个入口** —— 那颗齿轮从建出来起**点了没反应**（红线：不许静默失败）。
        /// 我们只建了原版五页里的三页（图像 / 音频 / 联机），见 `Shell/SettingsWindow.cs` 文件头 ③。
        ///
        /// 🔴 **2026-10-05（A104）：已经开着就【复用那一扇】，⛔ 不再新建** —— 原来这里每次都
        /// `SettingsWindow.Create(wm)`，于是**连点两次齿轮 = 两扇设置窗同时开着**、各带一条
        /// `QShade = 3130` 的整屏命中区（同一个坑的两份版本：谁吃到「点窗外」退化成枚举顺序）。
        ///
        /// **原版不会这样**（判据 = 反编译 `d:/2/tools/decomp_full/` + 实读场景 JSON）：
        ///   · 齿轮上那枚 `OpenWindowButton` 的 `windowToOpenScene` 是 **0**、只有
        ///     `windowToOpenPrefab` 填了 GUID `5a20859a…`（`bundle_scenes_scenes_mainmenuwarpforge/
        ///     MonoBehaviour/MonoBehaviour_2528.json`）⇒ 走「按引用加载 prefab」那一条（`OpenWindowButton__OpenWindow.c`
        ///     里 `windowToOpenScene == null` 那一支）；
        ///   · 那一条的 `WindowsManager.OpenWindow`（VA `0x180875990` —— 它的 `.c` **丢了**：两个同名重载
        ///     写进了同一个文件名）**第一件事就是查 `automaticallyLoadedWindows`（this+0x68）缓存**：
        ///     `0x180875A74 call 0x1815caa30`（字典 `TryGetValue`）→ `0x180875A7B jne 0x180875BF8`
        ///     （**命中 ⇒ 跳去复用**，`0x180875BF8` 起的块直接拿缓存里那个 `GameWindow` 往下走）。
        ///     **只有未命中**才 `0x180875B08 call 0x180d34fb0`（`Instantiate(prefab, GetWindowAnchor(...))`，
        ///     锚点按被加载件的 `windowsPlacement`（+0x24）取）。
        ///     缓存字段出处 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs` 的
        ///     `automaticallyLoadedWindows: BiDirectionalDictionary&lt;ComponentReference&lt;GameWindow>, GameWindow>`。
        ///   ⇒ **同一扇窗点两次只有一个实例**；第二次只是把**同一个实例**再走一遍 `OpenWindowCO` → `TryOpen`
        ///     （`GameWindow` 虚表 Slot 6）⇒ 观感是「重新铺一遍 / 回到最前」，**不是叠一扇**。
        /// ⚠️ **原写「只改了这一处」（2026-10-05/A104）—— 2026-10-06（A123）起这句不成立**：
        ///     本文件 9 条同形状的入口里 **8 条已统一走上面 `OpenByRef`**（`OpenBattleLogPopup` 判据空、照旧新建）。
        ///     判据表 → `资料/普查产出_1007/波6判据核查.md` §2。
        /// 🔴 **2026-10-06 二次订正（铁律 5）**：本方法**原来写的是「实例还在就复用」**（`SettingsWindow.Instance != null`），
        ///     与上面判据③「**还开着**才复用」**不一致** —— 我们的 `Close()` 只 `SetActive(false)`、**不销毁**
        ///     ⇒ 那样写 = **关过之后再点也是复用同一扇**（原版关窗会删缓存条目 ⇒ 原版走的是新建）。
        ///     ⇒ 调度台裁定**统一照判据③**，本方法已改走 `OpenByRef`（同一套机制、同一条出声）。
        /// ⚠️ `SettingsWindow.Instance` 那个字段**仍然由 `Create` 维护**（自检拿它认「开的是哪一扇」），
        ///     ⛔ **但别再把它当复用判据** —— 理由就是上面那一行。
        /// </summary>
        public SettingsWindow OpenSettings()
        {
            // 判据同上（原版 `automaticallyLoadedWindows` 命中就复用）：**还开着**才复用、关过就新建。
            return OpenByRef(RefSettings, wm => SettingsWindow.Create(wm));
        }

        /// <summary>
        /// 开社交窗（原版 `Social Submenu Variant` = `SocialMenuWindow`，由左竖导航第 5 键
        /// `40k_main_bt_friends` 上的 `OpenWindowButton` 开）。
        /// 🆕 2026-09-27：入口链已查实（`多人界面_入口与调用.md` §①）⇒ **照原版接线**，不是我们挑的。
        /// 🔴 **2026-10-06（A123）两处**：① 已经开着就**复用那一扇**（判据 → 上面 `OpenByRef` 那段）；
        /// ② 补 **`closeAll: true`** —— 那颗导航钮的 `OpenWindowButton.closeOtherMenus = 1`
        /// （`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2497.json`），
        /// 原来没传（同族的 `OpenCollection` / `OpenShop` 都传了 ⇒ 两个入口内部还不一致）。
        /// </summary>
        public SocialWindow OpenSocial()
        {
            return OpenByRef(RefSocial, wm => SocialWindow.Create(wm), true);
        }

        /// <summary>
        /// 开聊天窗（原版 `ChatPanel`，由 `ChatPreview.OpenChat` 开）。
        /// 🔴 主菜单右上那颗 `ChatPreview` 的 `40K_icon_menu_chat` 钮**从建出来起就没接点击** ——
        /// 这一条把它接上（原版那条链：`chatButton.onClick → OpenChat → WindowsManager.OpenWindow`，
        /// 判据 → `多人界面_入口与调用.md` §②）。
        /// 🔴 **2026-10-06（A123）：已经开着就复用那一扇**（判据 → 上面 `OpenByRef` 那段）—— 原来每次新建。
        /// ✅ **2026-10-11（A177 尾巴）就地订正**：原来这里写「另一条入口 `SocialWindow.OpenChat()` 不在白名单、
        /// 不共用这份缓存」—— **那已过期**：那条入口也收编到 `WindowsManager.OpenByRef`，而本方法同一天转调过去
        /// ⇒ 两条入口**共用同一个键 `"ChatPanel"`**（端到端断言 → `Editor/MainMenuScene.cs` 的「A177 尾巴」那一段）。
        /// </summary>
        public ChatPanel OpenChat()
        {
            return OpenByRef(RefChat, wm => ChatPanel.Create(wm));
        }

        /// <summary>
        /// 开**对局历史弹窗**（原版 `Battle Log Popup`）。
        /// 🔴 **界面里没有入口，是故意的 —— 原版的打开点查不到**：它只在 `WindowsManager` 的预载表里，
        /// 打开方式是 `OpenWindow&lt;BattleLogPopup&gt;()`，而那个泛型调用的产物缺失
        /// （普查 `对局历史_行模板与弹窗.md` §E 追过 74 处调用点 + 全 91 包按字节搜 GUID/pid）。
        /// ⇒ 我们**不编**一个入口出来；这个方法留给「将来找到真入口」或自检直接用。
        /// 要接的话接在哪 —— **等用户拍板**（记在 `项目任务.md` §三 第 18 条）。
        /// 🔴 **2026-10-06（A123）：这一条【不改成复用】—— 判据是空的**（`资料/普查产出_1007/波6判据核查.md` §2
        /// 那一行明写「原版入口查不到 ⇒ 本条判不了」）⇒ **没核过原版的东西不照改**（铁律 2：查不到就说查不到，
        /// 不许拿别的入口的判据套过来）。⇒ 仍然**每次新建**，如实留在这里等判据。
        /// ✅ **2026-10-11（A177）订正**：这一句原来写「相关残余：`Shell/BattleLogTab.cs:151` 的入口也是直调
        /// `Create`」—— 那条入口**已经收编到 `WindowsManager.OpenByRef`**；本方法**仍然每次新建**（判据空，见上一句）。
        /// </summary>
        public BattleLogPopup OpenBattleLogPopup()
        {
            var wm = WindowsManager.EnsureHost();
            var win = BattleLogPopup.Create(wm);
            wm.OpenWindow(win);
            return win;
        }

        /// <summary>
        /// 开**排行榜**（原版那一族的四棵之一）。
        /// 🔴 **入口链（复刻，不是我们挑的）**：`RankedEventWindowV2.LeaderboardButtonClick` 手上有
        /// **两颗** prefab —— `rankingPrefab`（遭遇榜）与 `rankingPrefabClassic`（经典榜），
        /// 按 `SetDivision` 传进来的 `playMode` 二选一 ⇒ **排位窗那颗钮走的是这条路**
        /// （见 `RankedEventWindow.OpenLeaderboard`），**自检以外的调用点不该有**。
        /// ⚠️ 另外两棵**在原版里没有可达入口**：轮抽榜挂在轮抽活动窗的 `AlliancesEventScorePanel` 上
        /// （我们没做轮抽模式）；嵌入版 `Ranked Leaderboard Display` **全库零引用**。
        /// ⇒ 这个方法留给自检（同 `OpenBattleLogPopup` 那条先例）。
        /// 判据 → `资料/普查产出_0927/排行榜_入口与调用.md`。
        /// 🔴 **2026-10-06（A123）：已经开着就复用那一扇**（判据 → 上面 `OpenByRef` 那段）。
        /// ⚠️ 缓存键按 **kind** 分开（`LeaderboardWindow.NameOf(kind)`）—— 原版那两颗 prefab
        /// （`RankedSkirmishLeaderboardPopup` / `RankedClassicLeaderboardPopup Variant`）是**两条引用**
        /// ⇒ 缓存里自然也是两条（换 kind 再点会开第二扇，与「一棵 prefab 一个实例」同义）。
        /// ✅ **2026-10-11（A177）订正**：原来这里写「另一条入口 `RankedEventWindow.cs:169` 不在白名单
        /// ⇒ 不共用这份缓存」—— **已过期**：那条入口（`RankedEventWindow.OpenLeaderboard`）也收编到
        /// `WindowsManager.OpenByRef`，与本法**共用同一份缓存**（`closeAll` 与否按各自那一段判据）。
        /// </summary>
        public LeaderboardWindow OpenLeaderboard(LeaderboardKind kind)
        {
            return OpenByRef(LeaderboardWindow.NameOf(kind), wm => LeaderboardWindow.Create(wm, kind));
        }

        /// <summary>
        /// 开玩家档案窗（原版 `Player Profile Window`，由**顶栏头像**上的 `OpenWindowButton` 开）。
        /// 🔴 **2026-09-27 才有这个入口** —— 头像块从建出来起**点了没反应**（跟齿轮当初一样，静默失败）。
        /// 六个页签：Profile / Avatar / Title / Battle Log / Trophies / Ranking；**出厂落在 Title 页**
        /// （原版唯一 `m_IsActive=true` 的页签根）。判据 → `资料/阶段二_多人界面_原版规格.md` §2·1。
        /// 🔴 **2026-10-06（A123）：已经开着就复用那一扇**（判据 → 上面 `OpenByRef` 那段）—— 原来每次新建。
        /// ⚠️ **`PlayerProfileWindow.CreateFor`（= 从排行榜行看**别人**的档案，`Shell/LeaderboardRow.cs`）
        /// 用的是**带玩家名的键**（`PrefabRefProfile + "|" + 名字`）⇒ 与本方法**不是同一个键**：先点一行
        /// 看 A、再开本入口，会是两扇。**那是我们挑的、那边已如实标注**（纯 prefab 引用会让「先点 A、
        /// 再点 B」静默显示 **A** 的资料）—— 判据在那边的注释里，⛔ 别照本条去「统一」它。
        /// </summary>
        public PlayerProfileWindow OpenProfile()
        {
            return OpenByRef(RefProfile, wm => PlayerProfileWindow.Create(wm));
        }

        /// <summary>开奖励窗（原版 `MainMenuRewardsWindow`）。回点导航钮那一步照原版做
        /// （`DF:MainMenuRewardsWindow__Open.c:14-16`）。
        /// 🔴 **2026-10-06（A123）两处**：① 已经开着就**复用那一扇**（判据 → 上面 `OpenByRef` 那段）；
        /// ② 补 **`closeAll: true`** —— 那颗 REWARDS 导航钮的 `closeOtherMenus = 1`
        /// （`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2496.json`）。</summary>
        public RewardsWindow OpenRewards()
        {
            var win = OpenByRef(RefRewards, wm => RewardsWindow.Create(wm), true);
            SelectNav(3);
            return win;
        }

        /// <summary>开收藏窗（原版 `Collection Menu Variant`，由左竖导航的 COLLECTION 开）。
        /// 🔴 那个钮挂的 `OpenWindowButton` 是 `closeOtherMenus = 1`
        /// （`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2494.json:16-33`）⇒ `closeAll: true`。
        /// 窗口参数实证：`type=0 Fullscreen` · `placement=5 Canvas` · `closeOnESC=1` · `extraScaleSmallScreen=1.0`。
        /// 🔴 **2026-10-06（A123）：已经开着就复用那一扇**（判据 → 上面 `OpenByRef` 那段）。
        /// 改前是「**每次新建一扇** + `closeAll` 把上一扇收掉」⇒ 画面不叠，但**每次都在换实例**
        /// （inactive 的旧窗在锚点下越积越多）。</summary>
        public CollectionWindow OpenCollection()
        {
            var win = OpenByRef(RefCollection, wm => CollectionWindow.Create(wm), true);
            SelectNav(1);
            return win;
        }

        /// <summary>开商店（原版 `Shop Menu Variant`，由左竖导航的 SHOP 开）。
        /// 🔴 窗口参数**与奖励窗不同**：`placement = 10 (World)`（奖励窗是 5 Canvas）· `closeOnESC = 1`。
        /// ⚠️ 商品数据**是我们编的**（原版在服务端）—— 见 `ShopData.cs` 文件头。
        /// 🔴 **2026-10-06（A123）：已经开着就复用那一扇**（判据 → 上面 `OpenByRef` 那段）—— 同 `OpenCollection` 那条。</summary>
        public ShopWindow OpenShop()
        {
            // 🔴 **原版这里是 `closeAll = true`** —— 主菜单那个 SHOP 钮挂的 `OpenWindowButton`
            //    `closeOtherMenus = 1`（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2495.json:32`），
            //    走 `WindowsManager.OpenWindow(win, data, closeAll: true)`（`OpenWindowButton__OpenWindow.c:33`）。
            var win = OpenByRef(RefShop, wm => ShopWindow.Create(wm), true);
            SelectNav(2);
            return win;
        }

        /// <summary>红点底图 `40K_notification_number` 的原版色 **#BCBCBC = (0.7358,0.7358,0.7358)**（正本 §二·2）。
        /// 🔴 **红点的显隐靠 alpha、不靠 `SetActive`** —— 原版 `UiBadgeNotification.Show()/Hide()` 只改
        /// 一个 float 字段（alpha 1.0 / 0）再把文本设成计数（`DF:UiBadgeNotification__Show.c` / `__Hide.c`）。
        /// 2026-09-23 前我们**五个导航钮的红点全画成不透明** ⇒ 与「出厂 `active=1`、运行时按通知亮」的原版不符。</summary>
        public static readonly Color BadgeTint = new Color(0.73585f, 0.73585f, 0.73585f, 1f);
        /// <summary>红点默认 **alpha 0**（= 原版 `Hide()` 之后的样子）。</summary>
        public static Color BadgeAlpha(bool on) { return new Color(BadgeTint.r, BadgeTint.g, BadgeTint.b, on ? 1f : 0f); }

        // ---- §五 B：顶栏 ----
        void BuildUpperBar(Transform root)
        {
            // A332：原版 `Upper bar` = 1920 × 100（`anchor (0,0)-(1,1)` + `sizeDelta.y −980` ⇒ 高 100）
            // 🔴 **2026-10-11（FX6）修**：第一实参原来写 `LayoutSpace.DesignWidth`（**世界单位** 17.7778，不是 px 1920）
            //   ⇒ 宽被除两次 108、`rect.width` 只剩 0.165。高 `100f` 是 px ⇒ 同一行的正对照。
            var bar = New(root, "Upper bar", 1920f, 100f);
            Rect(bar, "UI_Main_Upper_bar", -11.7f, 1920f, 0f, 71.3f, "Background", QBarPanel);

            // 齿轮 + 红点
            // A332：原版 `SettingsBtn` = 87.78 × 61.73（= 齿轮那一格 `UI_Settings_Icon` 的框，与下面那条 `Rect` 同值）
            var settings = New(bar, "SettingsBtn", 87.78f, 61.73f);
            // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版 `Upper bar/SettingsBtn` 那格
            //   `m_PreserveAspect = 1`（RT1560·GO496·MB2526），贴图 `UI_Settings_Icon` **179×179**
            //   塞进 87.78×61.73 的框 ⇒ 原版只画 **61.73²**（居中），我们拉伸 ⇒ **宽 1.42×**。
            var gear = Rect(settings, "UI_Settings_Icon", 1803.1f, 1890.9f, 4.6f, 66.4f, "Image", QBarContent,
                            null, true);
            // 🔴 **2026-09-26 接线**：这颗齿轮从建出来那天起**点了没反应**（连提示都没有 = 静默失败）。
            //    设置窗（原版 `Main Menu Settings Window`）属于「第 4 层」，2026-09-26 随**联机页**一起建
            //    —— 判据 → `资料/联机P2P_设计与交接.md` §3·5、`Shell/SettingsWindow.cs`。
            if (gear != null)
            {
                var hit = gear.gameObject.AddComponent<WindowButton>();
                hit.onClick = () => OpenSettings();
            }
            // ⚙️ 设置钮的红点：我们**没有通知源** ⇒ alpha 0（原版由 `UiBadgeNotification` 按通知亮）
            Rect(settings, "40K_notification_number", 1865.9f, 1890.9f, 4.4f, 29.4f, "Badge Highlight", QBarContent,
                 BadgeAlpha(false));

            // 三个顶栏按钮（位置**按 HLG 算**：spacing 9.75 · MiddleLeft）
            // A332：原版 `TopBarButtons` = 311.4 × 71.33
            var btns = New(bar, "TopBarButtons", 311.4f, 71.33f);
            // A332：原版 `InboxBtn` = 55 × 40（就是这个「框」——注释里那句「塞进 55×40 的框」）
            var inbox = New(btns, "InboxBtn", 55f, 40f);
            // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版 `Upper bar/TopBarButtons/InboxBtn`
            //   `m_PreserveAspect = 1`（RT1561·GO497·MB2529），贴图 `40K_notification` **135×105**
            //   塞进 55×40 的框 ⇒ 原版实绘 **51.43×40**，我们 55 宽 ⇒ **宽 6.5%**（轻，但同一条判据）。
            var inboxImg = Rect(inbox, "40K_notification", 425.3f, 480.3f, 15.5f, 55.5f, "Image", QBarContent,
                                null, true);
            // 🔴 **2026-09-23 接线**：原版这个钮上挂 **`OpenWindowButton`**（`windowToOpenPrefab.m_AssetGUID`
            //    已实证指向 `Inbox Menu` 根 pid `-4892976514573368526`）——**全库没有一处按名字调 `InboxWindow`**，
            //    原版是 Addressables 加载 + 虚函数 `Open()` 派发。
            if (inboxImg != null)
            {
                var hit = inboxImg.gameObject.AddComponent<WindowButton>();
                hit.onClick = () => OpenInbox();
            }
            // 红点：原版 `Inbox.CheckNotification` = **未读条数**，走 `UiBadgeNotification` 的 **alpha 补间**
            // （`Show()` 把 alpha 置 1、`Hide()` 置 0 —— **不是 `SetActive`**，见 `资料/日常_调用链_Inbox.md` C 节）。
            // 单机没有消息 ⇒ 未读 = 0 ⇒ **默认 alpha 0**。
            var inboxBadge = Rect(inbox, "40K_notification_number", 454.3f, 489.3f, 2.0f, 37.0f,
                                  "Badge Highlight", QBarContent, BadgeTint);
            if (inboxBadge != null)
                inboxBadge.SetTint(new Color(BadgeTint.r, BadgeTint.g, BadgeTint.b,
                                             DailyData.InboxHasBadge ? 1f : 0f));
            Rect(btns, "40K_icon_duel", 490.1f, 537.6f, 11.8f, 59.2f, "Challenge button", QBarContent);
            // `Feedback Button`（565.4..615.4, 10.5..60.5）出厂 `activeSelf=False` ⇒ **不建**（见文件头纪律 ③）

            BuildPlayerProfile(bar);

            // `Resources Bar`（1131.5..1802.5, −0.1..71.1）：⚠️ **原版这一格没有任何 Image**
            // （§二 表 #27 的图那一列写的是「无；+Canvas1079(嵌套)」）⇒ **不画底**。
            // 🔴 2026-09-22 自纠：第一版我在这里凭空加了一层 `40k_topmarquee_currency_display_BW`，
            //    渲染出来是**右上角一块浅灰药丸**，而原版实拍那里是空的 —— 典型的「我们自加的」（§10·3 找茬点 6）。
            // A332：原版 `Resources Bar` = 671.05 × 71.165
            // 🔴 **2026-10-13（A374）**：那 7 类资源格**建全了**（此前这里只有一个空容器 —— 见下 `BuildResourcesBar`）。
            BuildResourcesBar(bar);
        }

        /// <summary>`Player Profile`（0..528.7, 0..211.1）—— 左上角那块。
        /// 🔴 **2026-09-27 更正**：这里原来写着「**不画头像立绘**：原版那一格 `Image` 的
        /// `m_Sprite = 0`、`m_Enabled = 0`」—— **那条是错的**（读的是**序列化出厂值**，而运行期会点亮它）：
        /// · `AvatarDisplay.ChangeAvatar` 里 `set_sprite(avatarImage, 玩家头像)` + 按 `sprite != null`
        ///   `set_enabled`（`decomp_full/AvatarDisplay__ChangeAvatar.c:57,65`）；
        /// · `AvatarDisplay.Awake` 一进场就按 `sprite != null` **重算**一遍 enabled
        ///   ⇒ 出厂那个 `0` 只是「出厂时没图」的结果，**不是「永不显示」的设计**；
        /// · 没选过头像时原版回落到 `get_DefaultAvatarItem()`（**一个真·默认头像**）。
        /// 判据全文 → `资料/阶段二_多人界面_原版规格.md` · 这是本项目「**一个序列化字段 ≠ 全部情况**」的又一例。
        /// ⚠️ 顺带说清「关服实拍为什么是空盾」：那一层盾是 **`Player_Profile_Border` 的图**，
        ///   头像是在它**上面**另画的一层（我们原来没建那一层 ⇒ 永远只有盾）。</summary>
        void BuildPlayerProfile(Transform parent)
        {
            // A332：原版 `Player Profile` = 528.65 × 211.07（与上面那行小字里的 0..528.7 / 0..211.1 同源）
            var p = New(parent, "Player Profile", 528.65f, 211.07f);
            Rect(p, "40k_main_player_frame", 23.0f, 411.0f, 11.6f, 135.6f, "Background", QBarPanel);
            // 名字条底：亮图 × `m_Color (0.396,0.1925,0.3095)` = **暗紫红**（`m_Type` = **1 Sliced**）
            Rect(p, "40k_topmarquee_currency_display_BW", 25.6f, 472.1f, 14.9f, 60.5f, "Planer Name Background",
                 QBarContent, new Color(0.39623f, 0.19251f, 0.30954f, 1f));
            // 字号照 §七 表二：`Player Name` fontSize **32**（带 **autosize 10→32**）、`m_fontColor` **(0.9686,0.9137,0.7137)**
            // 🔴 **2026-09-28**：文案从写死的 `"Player Name"` 改成**真名字**（`ProfileData.PlayerName`，
            //    **全工程唯一一份**，档案窗改名窗写的就是它）。默认值见 `ProfileData.DefaultPlayerName`。
            var pn = Text(p, ProfileData.PlayerName, 136.9f, 401.9f, 13.7f, 61.7f, 8,
                          new Color(0.9686f, 0.9137f, 0.7137f), "Player Name", 32f, QBarText);
            // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `m_fontSizeBase` **原文** = **36.0**
            //    （= TMP 序列化默认值 ⇒ 原版这里**没显式设过**）。
            //    判据（本轮自己扫 `bundle_scenes_scenes_mainmenuwarpforge`）：
            //    `MonoBehaviour_1717.json` —— GO 名 `Player Name`、`'Player Name'`、
            //    `m_SizeDelta = (265, 48)`（与我们的框逐值相同）· `m_fontSize 32` · `auto[10~32]` ·
            //    **`m_fontSizeBase 36.0`**。⚠️ V7 §二·3 #32 记的是**全库**那族的分布
            //    （30.0×16 / 26.0×4 / …）⇒ 本条以**主菜单这一颗**的实读为准。
            if (pn != null) pn.SetAutoFitBox(265f / 108f, 48f / 108f, 10f, 32f, 36f);   // 原版 autosize 10→32
            // 🔴 **2026-10-07（A62 主表 #25）**：原版主菜单 `Player Name` 的 `m_TextWrappingMode = **0**`
            //   （判据 = `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1717.json` 实读：
            //    `m_TextWrappingMode=0` · `m_fontSizeMax=32`）—— 上面那句 `SetAutoFitBox` 会**无条件开折行** ⇒ 显式关掉。
            if (pn != null) pn.SetWrapping(false);

            // A332：原版 `Avatar Item Small` = 138.42 × 139.568（`anchor/pivot` 都重合、`sizeDelta` 就是这两个数）
            var av = New(p, "Avatar Item Small", 138.42f, 139.568f);
            var avBorder = Rect(av, "Player_Profile_Border", -10.0f, 165.5f, 9.0f, 139.1f, "Border", QBarAvatarFrame,
                                null, true);   // ⚠️ scl 1.25 已算进 §五 B；🔴 **最后那个 `true` = 保宽高比**（见下）
            // 🔴 **2026-09-27 修：这一格必须【保宽高比】画**（原来是拉伸的 ⇒ **宽了 1.6 倍**）。
            //    · 判据一（读字段）：原版 `Image` 的 **`m_PreserveAspect = 1`** —— 13 个战场里用这张图的
            //      Image 共 **39 个，39/39 全是 PA=1**（直读 `MonoBehaviour_4606.json:40`；同一结论早就写在
            //      `Battle/BattleDriver.cs` 的头像块注释里，那处当时已按 PA=1 修好，**主菜单这处漏了**）。
            //    · 判据二（量实拍）：原版实拍 `资料/原版参照图/Unity参照管线_0825/shots_ui/menu_full_0825.png`
            //      里量盾形框 **x 24..131 ⇒ 宽 ≈107 px**、高宽比 ≈0.94 ≈ 贴图的 `256/286 = 0.895`（**保宽高比**的特征）；
            //      我们按拉伸画出来是 **175.5 px 宽**（x −10..165.5）⇒ 明显对不上。
            //    · 保宽高比之后：实绘 = `fit(256×286 → 140.384×104.076) × 1.25 = 116.4×130.1`，**居中于原矩形中心**
            //      （⇒ 左右各内缩 29.5）—— 与实拍那个 ≈107/≈111 px 对得上 ✅。
            // 🔴 **2026-09-27 接线**：原版这块头像上挂 **`OpenWindowButton`**（开 `Player Profile Window`，
            //    `菜单全树.md` 的 `Player Profile Window` 那棵树记的入口）—— 我们以前**点了没反应**（静默失败）。
            //    命中区 = 头像整块（`-10,9 → 165.5,139.1`，就是 `Player_Profile_Border` 那张图的矩形）。
            if (avBorder != null)
            {
                var hit = avBorder.gameObject.AddComponent<WindowButton>();
                hit.onClick = () => OpenProfile();
            }
            BuildTopAvatar(av);
            // 🔴 **2026-10-11（A219①）就地订正**：这一格原来叫 **`"Icon/Player Level"`** —— 那是**一个带斜杠的
            //   【字面】节点名**（Unity 里那不是一个名字、是**路径分隔符** ⇒ `transform.Find` 会当两层路径走、
            //   永远取不到；逐字比名字的 `FindChild` 又只在整串相等时命中）。
            //   **原版判据（现读 `bundle_scenes_scenes_mainmenuwarpforge`）**：`GameObject/Player Level.json`
            //   的 `m_Name` = **`Player Level`**，其 `RectTransform`（pid 1279）的 `m_Father` = 1125 =
            //   `Player Profile` 那个 GO ⇒ 原版就是 `Player Profile` → `Player Level` **两级**，
            //   中间**没有** `Icon` 这一层（我们也不缺层，**只是名字写错了**）；整包 `m_Name` 里没有一个带 `/`。
            //   ⚠️ `资料/主菜单_原版规格.md` 的表里那个 `Icon/Player Level` 是 **dump 工具的显示串**，
            //      ⛔ 别照它改回来（那个文件自己「换算时踩到的坑」第 3 条：「名字一律不可信」）。
            //   断言 → `Editor/MainMenuScene.cs` 顶栏那一段（A219①）。
            // A332：原版 `Player Level` = **53.12²**（正方形）
            var lvl = New(p, "Player Level", 53.12f, 53.12f);
            Rect(lvl, "40k_topmarquee_currency_gold", 117.5f, 170.6f, 54.3f, 107.4f, "Icon", QBarContent);
            // `Player Level Text`：§七 表二 —— fontSize **37.2**，**autosize 18→37.2**
            var lv = Text(lvl, "-", 124.3f, 163.7f, 61.1f, 100.5f, 7, Color.white, "Player Level Text", 37.2f, QBarText);
            // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `m_fontSizeBase` **原文** = **36.89**。
            //    判据（本轮自己扫主菜单场景）：`Player Level Text`（`'-'`）两颗
            //    （`MonoBehaviour_2548.json` / `_505.json`）—— `m_fontSize 37.2` · `auto[18~37.2]` ·
            //    **`m_fontSizeBase 36.88999938964844`**（逐站表 §二·3 #29 同值）。
            //    ⚠️ base（36.89）**≠ 标称（37.2）** ⇒ 必须显式传，缺省就退回 37.2 了。
            if (lv != null) lv.SetAutoFitBox(39.4f / 108f, 39.4f / 108f, 18f, 37.2f, 36.89f);   // 原版 autosize 18→37.2
        }

        // ============================================================ ★ A374：顶栏资源计数器
        //
        // 🔴 **2026-10-13（A374）建**：`Resources Bar` 里那几颗资源格，原版是**运行时实例化**的
        //    （`ResourcesBarController.GetCounter` → `Instantiate(resourceCounterReference, container)`），
        //    我们以前**只建了一个空容器**（文件头「已知缺口」第 2 条自己记着）。下面把它建全。
        //
        // **判据全部现读，逐条带出处**（⛔ 没有一处是照印象写的）：
        //
        // ① **建表序（最多 7 项）** = `DF/GeneralMenuController__Initialize.c:51-146`：
        //    逐项 `List.Add`（`List` 容量 **6**）—— `0x47` · `0x46` · `0x3c` · `10` · `0x14` · `0`；
        //    `:147-205` 是个**条件支**：扫某个 liveops 列表、里面出现 `0x48`(energy) 才
        //    `System_Collections_Generic_List<Int32Enum>__Insert(lVar6, 0, 0x48)`（**插到 index 0 = 最前**）。
        //    枚举字面量 → `d:/2/tools/il2cpp_out/dump.cs:46139-46153`（TypeDefIndex 957 `GameCurrency`）：
        //    `gold 0` · `crystals 10` · `blackStones 20` · `raidMedals 60` · `gachaTickets 70` ·
        //    `campaignPoints 71` · `energy 72`。
        // ② **哪几颗恒显** = `DF/ResourcesBarController__.ctor.c:24-75` —— 字段 `defaultCurrencies`（`+0x48`，
        //    `readonly`、**非序列化**）被初始化成**恰好 3 项**：`0` · `10` · `0x14`。
        // ③ **显隐规则** = `DF/ResourcesBarController__Initialize.c:71-98`（逐句）：
        //    `defaultCurrencies.Contains(c) ? SetActive(true) : (GetOwnedCount(c) > 0 ? SetActive(true) : SetActive(false))`。
        // ④ **上限是【通用机制】—— ⛔ 不是「活动点特例」** = `DF/UIResourceCounter__SetQuantity.c:54-68`：
        //    `if (0 < *(int *)(param_1 + 0x38))` ⇒ `suffix = String.Format("/{0}", max)`，否则 `suffix = ""`；
        //    再 `String.Format("{0}{1}", qty, suffix)`。三个格式串已按 RVA 解出（`stringliteral.json`）
        //    = **`/{0}`** · **`{0}{1}`** · **`''`**（`DAT_1842b6438 / 184271c30 / 1842b80e0`）。
        //    那个 `+0x38` 的**来源** = `DF/ResourcesBarController__GetCounter.c:66-70`（`ICurrency` 虚表 **slot 3**
        //    ⇒ `DF/ICurrency__get_MaxAmount.c:5` **基类返回 −1**）；`dump.cs:45051-45071` 的接口表里
        //    **全工程只有 `CampaignPoints` 覆写它**（`dump.cs:18050` `private int maxAmount; // 0x58`，
        //    `DF/CampaignPoints__get_MaxAmount.c:5` 就是 `return *(int*)(this + 0x58)`）
        //    ⇒ **任何一颗币种只要它自己的上限 > 0 都会带 `/max`**，与「是哪一颗」无关。
        // ⑤ **每格的模板与尺寸** = `AF/bundle_scenes_scenes_mainmenuwarpforge/` 里那一份 prefab
        //    （`GameObject/Resource Counter Item.json` 及其三个子件的 `RectTransform` / `MonoBehaviour`，
        //     逐字段实读；每个常量旁写了它是哪个 pid 的哪个字段）。
        //
        // ⚠️ **"持有数" 这一格的来源是【我们自己的】**（原版 = `CurrencyExtensions.GetOwnedCount(c)`，
        //    数据在后端）：我们用 `Wallet.Of(记账键)`（用户 2026-09-17 定的「不做真实经济 / 资源固定 9999」
        //    ⇒ 那个包里任何没记过账的键读出来都是 `StartAmount` = 9999）。**币种 → 记账键**这张表
        //    是**我们挑的**（我们的名册按【图名】记账，原版按 `GameCurrency` 枚举）——
        //    有现成记账口的才填，没有的填 `null`（= 按 0 计 ⇒ 原版规则下就是「不显示」）。
        //
        // ⚠️ **本地还差 4 张图标**（如实，不静默）：原版小图标那一族的命名规律是
        //    `40k_topmarquee_currency_<名>`（**3/3 已解的 `Currency` SO 都是这一族** ——
        //    `Blackstone`(`type=20`) → `40k_topmarquee_currency_blackstone` ·
        //    `Crystals`(`type=10`) → `40k_topmarquee_currency_crystal` ·
        //    `Gacha tickets`(`type=70`) → `40k_topmarquee_currency_ticket`；
        //    解法和原话见 `资料/普查产出_1013/W374_资源计数器.md` §三），而**我们的 `Resources/Art/ui_menu/`
        //    只有 `40k_topmarquee_currency_gold` 与 `40K_genearl_icon_Campaign_points` 两张** ⇒
        //    另外几颗**只画药丸 + 数字、不画圆图标**，名字会进 `MissingArt`（`Build()` 末尾那条警告会点名）。
        //    ⛔ 别拿 `40k_general_icon_currency_*`（那是**大图**那一档）顶替 —— 那是另一张资产。

        // ---- 币种枚举（原版 `GameCurrency`，判据见上面 ①）----
        public const int CurGold = 0, CurCrystals = 10, CurBlackStones = 20, CurRaidMedals = 60,
                         CurGachaTickets = 70, CurCampaignPoints = 71, CurEnergy = 72;

        /// <summary>一颗资源计数器的规格。三个字段各自的判据 → <see cref="BuildResourcesBar"/> 头注释。</summary>
        public sealed class CounterSpec
        {
            public int Currency;      // 原版 `GameCurrency`
            public string Icon;       // 原版 `ICurrency.GetIcon(IconSize.Small)` 的 sprite 名；null = 本地判据没查到
            public string WalletKey;  // **我们自己的**：`Wallet` 里这一路的记账键；null = 我们没有这一路 ⇒ 按 0 计
            public int MaxAmount;     // 原版 `ICurrency.MaxAmount`（≤ 0 = 无上限，不写 `/max`）
        }

        /// <summary>**次序 = 原版 `List.Add` 序**（①）—— ⛔ 别按大小/字母重排。
        /// 第 4 列（上限）判据：`campaignPoints` = **实拍读数 `300/2000`**（`资料/普查产出_1011/WA4_A370.md:70` ·
        /// `资料/历史/会话_2026-10-11_批次2.md:132` · `资料/索引与盘点/解包资源列表清单.md:125` 那句
        /// 「顶栏货币1(2000/2000)图标」）—— ⚠️ **不是从资产字段读来的**：`CampaignPoints` 那个 SO 本地导出里没有
        /// （全 `assets_full` 扫 `smallIcon` 只有 3 份 `Currency`，没有 `CampaignPoints`）。</summary>
        static readonly CounterSpec[] CounterSpecs =
        {
            new CounterSpec { Currency = CurCampaignPoints, Icon = "40K_genearl_icon_Campaign_points",
                              WalletKey = "40K_genearl_icon_Campaign_points_big", MaxAmount = 2000 },
            new CounterSpec { Currency = CurGachaTickets,   Icon = "40k_topmarquee_currency_ticket",
                              WalletKey = null, MaxAmount = 0 },
            // ⚠️ `raidMedals`（`0x3c`）：**原版图标本地没查到**（搜过全 `assets_full` 的
            //    `*/Sprite/` 与 `*/Texture2D/` 文件名里的 `medal` / `raid` / `league` / `rank` ——
            //    只有 5 张 `40k_Achievements_icon_medal{1..5}`（成就档位图）与一堆 `*_Raid_Background`，
            //    没有 `40k_topmarquee_currency_raidmedal*`）⇒ 如实写 `null`，⛔ 不猜一个名字。
            new CounterSpec { Currency = CurRaidMedals,     Icon = null,
                              WalletKey = null, MaxAmount = 0 },
            new CounterSpec { Currency = CurCrystals,       Icon = "40k_topmarquee_currency_crystal",
                              WalletKey = "40k_general_icon_currency_crystal", MaxAmount = 0 },
            new CounterSpec { Currency = CurBlackStones,    Icon = "40k_topmarquee_currency_blackstone",
                              WalletKey = null, MaxAmount = 0 },
            new CounterSpec { Currency = CurGold,           Icon = "40k_topmarquee_currency_gold",
                              WalletKey = "40k_topmarquee_currency_gold", MaxAmount = 0 },
        };

        /// <summary>原版 ① 那条**条件支**插在最前的那一颗（`0x48` = energy）。
        /// 🔴 **判据在远端**：`GeneralMenuController__Initialize.c:147-205` 扫的是
        /// `LiveOpsManager` 手里那份 live-ops 币种表（`LoadableReference.get_Reference()` 之后读 `+0x60` 那个
        /// `Nullable&lt;int>`，与 `0x48` 比）—— 本地导出里**没有** live-ops 事件数据、后端也已关
        /// ⇒ 我们**恒 `false`**。⚠️ 它只决定「energy 那一颗在不在、是不是排在最前」，
        /// 与另外 6 颗的显隐/次序**无关**。自检夹具会把它打开一次（见 `Editor/MainMenuScene.cs` 那一段）。</summary>
        public static bool EnergyInResourcesBar = false;

        static readonly CounterSpec EnergySpec = new CounterSpec
        { Currency = CurEnergy, Icon = "40k_topmarquee_currency_energy", WalletKey = null, MaxAmount = 0 };

        /// <summary>原版 ② `ResourcesBarController..ctor` 的 `defaultCurrencies` —— **恒显**那三颗。</summary>
        public static readonly int[] DefaultCurrencies = { CurGold, CurCrystals, CurBlackStones };

        // ---- 自检夹具用的两个拨盘（口径同 `DailyData.ForceWeeklyProgressForTest` 那一族）----

        static readonly Dictionary<int, int> _ownedForTest = new Dictionary<int, int>();
        static readonly Dictionary<int, int> _maxForTest = new Dictionary<int, int>();

        /// <summary>自检夹具：拨某一颗币种的**持有数 / 上限**（传 `null` = 这一项还原）。
        /// 🔴 **「拨上限」这个口存在的理由**：那条「上限是通用机制、⛔ 不是活动点特例」**必须能换一颗币种验**
        /// （把 campaignPoints 的上限改成 500 ⇒ 文案就得跟着变成 `/500`）——
        /// 否则一个「活动点写死 `"/2000"`」的实现也能蒙过全部断言。
        /// ⛔ 生产路径不碰它（持有数来自 `Wallet`，上限来自 `CounterSpecs`）。</summary>
        public static void ForceCounterForTest(int currency, int? owned, int? max)
        {
            if (owned.HasValue) _ownedForTest[currency] = owned.Value; else _ownedForTest.Remove(currency);
            if (max.HasValue) _maxForTest[currency] = max.Value; else _maxForTest.Remove(currency);
        }

        /// <summary>把两个拨盘全还原（自检每段夹具收尾都要调，⛔ 别让夹具漏到别的段）。</summary>
        public static void ClearCounterForTest() { _ownedForTest.Clear(); _maxForTest.Clear(); }

        /// <summary>原版 ③ 的 `CurrencyExtensions.GetOwnedCount(c)` 在我们这边的等价物。
        /// ⚠️ 返回值的来源是**我们自己的** `Wallet`（见 `BuildResourcesBar` 头注释那条免责）。</summary>
        public static int OwnedOf(int currency)
        {
            int v;
            if (_ownedForTest.TryGetValue(currency, out v)) return v;
            var s = SpecOf(currency);
            return (s == null || s.WalletKey == null) ? 0 : Wallet.Of(s.WalletKey);
        }

        /// <summary>原版 ④ 的 `ICurrency.MaxAmount`。</summary>
        public static int MaxOf(int currency)
        {
            int v;
            if (_maxForTest.TryGetValue(currency, out v)) return v;
            var s = SpecOf(currency);
            return s == null ? 0 : s.MaxAmount;
        }

        /// <summary>原版 ③ 那条显隐判据（逐字对照 `ResourcesBarController__Initialize.c:71-98`）。</summary>
        public static bool ShouldShow(int currency)
        {
            for (int i = 0; i < DefaultCurrencies.Length; i++)
                if (DefaultCurrencies[i] == currency) return true;
            return OwnedOf(currency) > 0;
        }

        /// <summary>原版 ④ 那两行 `String.Format`（逐字对照反编译，含 `> 0` 这个判据）。
        /// 🔴 `max ≤ 0` ⇒ **不带后缀** —— 基类 `ICurrency.get_MaxAmount()` 返回的是 **−1**（不是 0），
        /// 拿 `!= 0` 当判据会把每一颗都写成 `x/-1`。</summary>
        public static string CounterText(int qty, int max)
        {
            string suffix = max > 0 ? string.Format("/{0}", max) : "";
            return string.Format("{0}{1}", qty, suffix);
        }

        static CounterSpec SpecOf(int currency)
        {
            for (int i = 0; i < CounterSpecs.Length; i++)
                if (CounterSpecs[i].Currency == currency) return CounterSpecs[i];
            if (currency == CurEnergy) return EnergySpec;
            return null;
        }

        // ---- 原版 prefab 的几何（`AF/bundle_scenes_scenes_mainmenuwarpforge`，逐字段实读）----
        //
        // `Resources Bar`（RT **1187**）：`anchor (1,1)` · `pivot (0.5,0.5)` · `apos (−453.0, −35.5)` ·
        //   `sizeDelta (671.05, 71.165)` ⇒ 绝对矩形 **1131.475..1802.525 × −0.0825..71.0825**。
        const float ResBarW = 671.05f, ResBarH = 71.165f;
        const float ResBarL = 1131.475f, ResBarR = 1802.525f, ResBarT = -0.0825f, ResBarB = 71.0825f;
        // `Resources Container`（RT **1427**）：`anchor/pivot (1, 0.5)` · `apos ≈ 0` ·
        //   `sizeDelta (0, 71.165)` + `ContentSizeFitter(HorizontalFit=2 PreferredSize)`（MB **2106**）
        //   ⇒ 右沿贴 `Resources Bar` 右沿、高 71.165、**宽由内容算**。
        //   它的 `HorizontalLayoutGroup`（MB **2279**）：`pad (L0,R5,T0,B0)` · `spacing 44` ·
        //   `m_ChildAlignment 5`（= `MiddleRight` ⇒ 交叉轴取 **Middle**）。
        const float ResSpacing = 44f, ResPadR = 5f;
        // `Resource Counter Item`（RT **359**）：`sizeDelta (0, 38.066)`；`HorizontalLayoutGroup`（MB **206**）：
        //   `pad (L20,R1,T0,B0)` · `spacing 0` · `m_ChildAlignment 0`（= `UpperLeft` ⇒ 交叉轴取 **Upper**
        //   ⇒ 药丸与图标都**顶对齐**在这一格的顶边上）；`ContentSizeFitter`（MB **529**）HorizontalFit=2。
        const float ItemH = 38.066f, ItemPadL = 20f, ItemPadR = 1f;
        // `Resource Bar Background`（RT **372**）：`sizeDelta (141.51, 38)`；`Image`（MB **409**）：
        //   sprite = **`40k_topmarquee_currency_display BW`** · `m_Type 1`（**Sliced**）·
        //   `m_Color (0.33019, 0.26010, 0.31567, 1)` · `m_PreserveAspect 0` · 九宫 `m_Border 15,15,15,15`
        //   （贴图 44×39 ⇒ 与 dump 的「44×39 九宫15,15,15,15」逐值吻合）；`HorizontalLayoutGroup`（MB **207**）：
        //   `pad (L29,R9,T0,B0)` · `m_ChildAlignment 3`（= `MiddleLeft` ⇒ 文字竖直居中）。
        const float PillH = 38f, PillPadL = 29f, PillPadR = 9f;
        static readonly Color PillTint = new Color(0.33019f, 0.26010f, 0.31567f, 1f);
        // `Icon`（RT **371**，GO 名就是 **`Icon`**）：`sizeDelta (60, 59)`；`Image`（MB **399**）：
        //   `m_Type 0`（Simple）· **`m_PreserveAspect 1`** · `m_Color (1,1,1,1)`。
        //   ⚠️ 它出厂那张 sprite 是 `UI_Button_Round_background`，**运行期被
        //   `GetCounter` 换成该币种的图标**（`ResourcesBarController__GetCounter.c:66-68`
        //   `Image.set_sprite(counter.icon, currency.GetIcon(Small))`；`counter.icon` = `+0x20`，
        //   prefab 里指向 MB **399**）⇒ 我们**直接画币种图标**，不画那张出厂占位图。
        const float IconW = 60f, IconH = 59f;
        // `Resource QuantityText`（RT **370**）：`sizeDelta (103.51, 38)`；TMP（MB **517**）：
        //   `m_text '-----'` · **`m_fontSize 42`** · `m_fontSizeBase 36` · `m_enableAutoSizing 1` ·
        //   `m_fontSizeMin 10` / `m_fontSizeMax 42` · **`m_TextWrappingMode 0`**（不折行）·
        //   `m_HorizontalAlignment 2`(Center) / `m_VerticalAlignment 4096`(Midline) ·
        //   **`m_characterSpacing −1`** · `m_fontColor` 白。
        //   `ContentSizeFitterMinMax`（MB **544**）：`clampWidth 1` · **`widthMin 103.51` · `widthMax 153`**
        //   ⇒ 药丸宽 = `29 + clamp(文字宽, 103.51, 153) + 9`（出厂占位串 `'-----'` 量出来正是 103.51）。
        const float QtyFontPx = 42f, QtyFontBase = 36f, QtyFontMin = 10f, QtyCharSpacing = -1f;
        const float QtyWMin = 103.51f, QtyWMax = 153f;
        // 九宫用到的贴图尺寸（`m_Border` 是按**贴图像素**量的）—— 我们导进来的那张就是 44×39。
        const float PillTexW = 44f, PillTexH = 39f;

        void BuildResourcesBar(Transform bar)
        {
            // ① `Resources Bar` 自己：**没有 Image**（判据见调用点那三行注释）⇒ 只建空节点。
            var resBar = New(bar, "Resources Bar", ResBarW, ResBarH);

            // ② 按原版那条规则挑出要显示的（**次序照原版 `List.Add` 序**，不是我们排的）。
            var shown = new List<CounterSpec>();
            foreach (var s in CounterSpecs) if (ShouldShow(s.Currency)) shown.Add(s);
            if (EnergyInResourcesBar) shown.Insert(0, EnergySpec);      // 原版 `:205` 的 `Insert(…, 0, 0x48)`

            // ⚠️ 本文件的**不变式是「中间节点停在原点、绝对坐标只写在叶子上」**（见 `New` 的注释）
            //    ⇒ `Resources Container` / `Resource Counter Item` / `Icon` 这几个中间节点一律不挪，
            //    **带图的那两个叶子节点**（`Resource Bar Background` 的九宫根、`Icon/Image` 那张 quad）
            //    才写绝对坐标。
            var container = New(resBar, "Resources Container", 0f, ResBarH);

            // ③ **先量后建**：原版每一格的宽是 `ContentSizeFitter` 按**文字 preferred 宽**算出来的
            //    （`Resource Bar Background` 的 CSF，MB 551）⇒ 必须先有 `Label` 才量得到。
            //    这一趟只建「格节点 + 文字」，量完宽度再在 ④ 里把药丸/图标按**最终矩形**一次建出来
            //    （⛔ 不先建再挪 —— 九宫那一棵是九块拼的，挪它容易漏掉子块）。
            var itemT = new List<Transform>();
            var labelL = new List<Label>();
            var textWl = new List<float>();
            var pillWl = new List<float>();
            var itemW = new List<float>();
            var noIcon = new List<int>();              // 原版图标**本地没查到**的币种（出声用）

            foreach (var s in shown)
            {
                var item = New(container, "Resource Counter Item", 0f, ItemH);   // RT 359：高 38.066
                // 量宽用的**占位**矩形（真实矩形要等量完；`Text()` 只认绝对坐标）
                var lb = Text(item, CounterText(OwnedOf(s.Currency), MaxOf(s.Currency)),
                              0f, 1f, 0f, PillH, 8, Color.white, "Resource QuantityText", QtyFontPx, QBarText);
                // 🔴 字距要**在量宽之前**设好（原版 `m_characterSpacing = −1` 会改 preferred 宽）
                if (lb != null) { lb.SetCharSpacing(QtyCharSpacing); lb.ForceRelayout(); }
                if (string.IsNullOrEmpty(s.Icon)) noIcon.Add(s.Currency);

                float nat = lb != null ? lb.WorldW * 108f : 0f;          // 我们自己的 TMP 度量（world → px）
                float textW = Mathf.Clamp(nat, QtyWMin, QtyWMax);        // 原版 `ContentSizeFitterMinMax`（MB 544）那两条 clamp
                float pw = PillPadL + textW + PillPadR;                  // 药丸宽 = 29 + 文字宽 + 9
                float iw = ItemPadL + pw + IconW + ItemPadR;             // 格宽 = 20 + 药丸 + 0(间距) + 图标 + 1

                itemT.Add(item); labelL.Add(lb); textWl.Add(textW);
                pillWl.Add(pw); itemW.Add(iw);
            }

            // 容器的宽 = 原版那条 CSF/HLG 算式（Σ格宽 + spacing×(n−1) + padRight）；
            // ⚠️ 原版 `Resources Container` 的 `sizeDelta.x` 是 **0**（宽由它自己的 CSF 算），
            //    我们这边没有布局系统 ⇒ 直接把算出来的宽度写进 `sizeDelta`（= 原版的**运行时 rect**）。
            float totalW = ResPadR + (shown.Count > 0 ? ResSpacing * (shown.Count - 1) : 0f);
            for (int i = 0; i < itemW.Count; i++) totalW += itemW[i];
            MenuDraw.SetPxSize(container, totalW, ResBarH);

            // ④ 按**最终矩形**建（左→右；**右沿贴 `Resources Bar` 右沿** —— 原版那两个锚都在 1）。
            float x = ResBarR - totalW;
            for (int i = 0; i < shown.Count; i++)
            {
                var s = shown[i];
                float iT = ResBarT + (ResBarH - ItemH) * 0.5f;           // 格在容器里竖直居中（容器 HLG 的 align = MiddleRight）
                float pL = x + ItemPadL, pR = pL + pillWl[i];            // 药丸**顶对齐格的顶边**（格的 HLG align = UpperLeft）
                MenuDraw.SetPxSize(itemT[i], itemW[i], ItemH);           // 格宽 = 它自己 CSF 算出来的（同 `Resources Container` 那条）

                // `Resource Bar Background`（RT 372：`Image` = **Sliced** 九宫 · `m_Color` 暗紫灰）
                // ⚠️ 走公共件 `MenuDraw.Nine`（`ImageQuad.CreateNineSlice`）—— 原版 `m_Type = 1` 就是 Sliced，
                //    ⛔ 别按拉伸画（44px 的图拉到 141.51 会把两端圆角糊掉）。
                var pg = MenuDraw.Nine(itemT[i], Art("40k_topmarquee_currency_display_BW"),
                                       new PxRect(pL, iT, pR, iT + PillH), new Vector4(15f, 15f, 15f, 15f),
                                       PillTexW, PillTexH, QBarContent, PillTint, true, "Resource Bar Background");

                // `Resource QuantityText`（RT 370 / TMP MB 517）—— 挂到药丸底下（原版就是 `Resource Bar Background` 的子件）
                var lb = labelL[i];
                if (lb != null)
                {
                    if (pg != null) lb.transform.SetParent(pg.transform, false);
                    var host = lb.transform.parent;
                    // 药丸的内衬：`pad (L29, R9)`，在那 38 里竖直居中（里层 HLG 的 align = MiddleLeft）
                    lb.transform.localPosition = Center(pL + PillPadL, pR - PillPadR, iT, iT + PillH)
                                               - (host != null ? host.localPosition : Vector3.zero);
                    lb.SetAutoFitBox(textWl[i] / 108f, PillH / 108f, QtyFontMin, QtyFontPx, QtyFontBase);
                    lb.SetWrapping(false);                               // 原版 `m_TextWrappingMode = 0`
                    // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版 `Resource QuantityText`
                    //   = `m_HorizontalAlignment 2`(Center) / `m_VerticalAlignment **4096**`(Midline)
                    //   （判据 = 上面 :1189-1195 那段逐字段实读，`MB 517`）。
                    //   ⚠️ 排在 `SetAutoFitBox` **之后**（它末句 `RefreshBounds` 会重摆一次；档位幂等，
                    //   放最后只为少一次重排）。
                    MenuDraw.SetVAlign(lb, Label.VAlign.Midline,
                                       new PxRect(pL + PillPadL, iT, pR - PillPadR, iT + PillH));
                }

                // `Icon`（RT 371：60 × 59 · `Image` 的 **`m_PreserveAspect = 1`**）—— 紧挨药丸右边（spacing 0）
                var icon = New(itemT[i], "Icon", IconW, IconH);
                if (!string.IsNullOrEmpty(s.Icon))
                    Rect(icon, s.Icon, pR, pR + IconW, iT, iT + IconH, "Image", QBarContent, null, true);

                x += itemW[i] + ResSpacing;
            }

            // ⑤ 出声：**原版图标本地没查到**的币种（⛔ 不静默 —— 那种格只画药丸 + 数字）。
            if (noIcon.Count > 0)
                Debug.LogWarning("[Menu] ⚠️ 顶栏有 " + noIcon.Count + " 颗币种**原版图标本地没查到**"
                                 + "（" + string.Join("、", CurrencyNames(noIcon)) + "）⇒ 只画药丸 + 数字，**不画圆图标**。"
                                 + "判据与修法 → `资料/普查产出_1013/W374_资源计数器.md` §六");
        }

        /// <summary>币种 → 名字（**只给出声/日志用**，⛔ 不是判据）。</summary>
        static string[] CurrencyNames(List<int> curs)
        {
            var a = new string[curs.Count];
            for (int i = 0; i < curs.Count; i++)
            {
                switch (curs[i])
                {
                    case CurGold: a[i] = "gold"; break;
                    case CurCrystals: a[i] = "crystals"; break;
                    case CurBlackStones: a[i] = "blackStones"; break;
                    case CurRaidMedals: a[i] = "raidMedals"; break;
                    case CurGachaTickets: a[i] = "gachaTickets"; break;
                    case CurCampaignPoints: a[i] = "campaignPoints"; break;
                    case CurEnergy: a[i] = "energy"; break;
                    default: a[i] = "cur" + curs[i]; break;
                }
            }
            return a;
        }

        // ---- 顶栏那块头像立绘（2026-09-27 建；判据 → 上面 `BuildPlayerProfile` 的更正块）----

        ImageQuad _topAvatar;
        int _topAvatarIdx = -1;

        /// <summary>顶栏立绘那一格。**由边框那一格推出来**（见 `BuildTopAvatar` 的推导）：
        /// 中心 = 边框中心 + `(2.6, −6.427)` · 尺寸 = 边框 × `(1.5776, 1.5711)` ⇒ 276.9×204.4。
        /// ⚠️ 它**比盾牌框大**（会溢出屏幕左上角），但立绘贴图的实心部分只占 43%×60% ⇒ 露出来的只有人像。</summary>
        const float TopAvatarL = -58.09f, TopAvatarR = 218.79f, TopAvatarT = -34.59f, TopAvatarB = 169.83f;

        /// <summary>把「玩家现在选的头像」画到顶栏那面盾**上面**。
        /// 🔴 **队列必须比边框高**（`QAvatarFrame` &lt; `QContent`）—— 盾的中心是不透明黑，反了就是一块黑。
        /// ⚠️ 立绘的盒子**比盾大**，这是**照原版 prefab 算的、不是我们挑的**，推导如下（2026-09-27 查实）：
        /// · prefab（`bundle_scenes_scenes_mainmenuwarpforge`）：`Image` 是**拉伸**在 `Image Container` 上
        ///   （`anchor(0,0)-(1,1)` · `sizeDelta(0,0)`），而 `Image` 的 **`m_LocalScale = 2.0`**
        ///   ⇒ 画出来 = 容器 × 2 = **(138.42×102.2)×2 = 276.84×204.4**；
        /// · 边框那一格同法算 = `(140.384×104.076)×1.25` = **175.48×130.095** —— 与我们实拍对上的那一格**逐位吻合** ✅
        ///   ⇒ 同一条推导链是可信的；
        /// · **两格的相对关系**：`Image` 中心 = 容器中心 + `(0,2.7)`、`Border` 中心 = 容器中心 + `(−2.6,−3.727)`
        ///   ⇒ 立绘中心 = 边框中心 + `(2.6, 6.427)`（prefab 是 y 向上，落到屏幕是 `−6.427`）。
        /// 🔴 **为什么不是「和边框同格」**（我 2026-09-27 第一版那么做的，**是错的**）：把两种尺寸合成出来并排看，
        ///   立绘贴图的**实心部分**（`alpha>128` 的包围盒 = 512 里的 **220×306** = 43%×60%）：
        ///   · 「同格」 ⇒ 实心只有 **75.5×77.5**，而盾的孔径是 **161.8×121** ⇒ **矮 36%**（截图里人像浮在一圈黑中间）；
        ///   · 「×2」   ⇒ 实心 **119×121.8** ⇒ **高与孔径差 0.7%**（这不是巧合：贴图那圈 40% 的透明边距就是为这个留的）。
        ///   ⇒ **×2 才是原版的意图**。
        /// ⚠️ **同样是【保宽高比】画**（`RectTex` 最后那个 `true`）：原版 `m_PreserveAspect = 1`（39/39，见 `BuildPlayerProfile` 那段）。
        ///   保宽高比之后实绘 = `fit(512² → 276.84×204.4) = **204.4×204.4**`，居中于上面那个盒子的中心。
        ///   ⇒ 合成出来并排看过：**人像正好填满盾牌**（兜帽顶到上边框、肩到侧边框、盾尖正好在人像底）——
        ///   这一版才像原版该有的样子。
        /// ✅ **几何全部有尺子，已收工**：换成保宽高比之后，盾在**原版实拍里的量测**与我们逐点吻合
        ///   （原版 左 24 · 右 131 · 顶 13 · **盾尖 y=132(x≈83)** ／ 我们 左 23 · 右 128 · 顶 13 · **盾尖 y=132(x≈83)**），
        ///   而「运行期没人改这个 RectTransform」也由反编译确认（全库零 `SetNativeSize`；`AvatarDisplay` 17 个方法逐个读过）。
        /// 📌 用户 2026-09-27 定：**观感那条不用挂待办**（「以后我觉得不舒服再说」）⇒ 这里不留 `真 Play` 指针。</summary>
        void BuildTopAvatar(Transform av)
        {
            var tex = LoadAvatar(ProfileData.AvatarArt);
            if (tex == null) return;                       // 一张都取不到 ⇒ 与出厂态一致（只剩那面盾）
            _topAvatar = RectTex(av, tex, TopAvatarL, TopAvatarR, TopAvatarT, TopAvatarB, "Image", QBarContent, true);
            _topAvatarIdx = ProfileData.AvatarIndex;
        }

        /// <summary>头像那批图（`Resources/Art/avatars/`，名字**含空格、原样传**）。
        /// 取不到 ⇒ 记进 `MissingArt`（**出声**，不静默画个白块）。</summary>
        Texture2D LoadAvatar(string art)
        {
            if (string.IsNullOrEmpty(art)) return null;
            var t = CardArt.Cosmetics(art);
            if (t == null && !MissingArt.Contains(art)) MissingArt.Add(art);
            return t;
        }

        /// <summary>玩家在档案窗改了头像 ⇒ 顶栏这一层跟着换（原版走 `PlayerAvatarDataManager.OnAvatarChanged`
        /// 那条事件；我们只有**一处**状态 `ProfileData.AvatarIndex`，**每帧比一个 int** 就够，
        /// 别为它另造一套事件机制）。没变就什么都不做。
        /// ⚠️ 批处理下 `Update` 不跑 ⇒ **自检直接调这个方法**（所以它是 public 的）。</summary>
        public void RefreshTopAvatarIfChanged()
        {
            if (_topAvatar == null || _topAvatarIdx == ProfileData.AvatarIndex) return;
            var tex = LoadAvatar(ProfileData.AvatarArt);
            if (tex == null) return;
            _topAvatar.SetTexture(tex);
            _topAvatarIdx = ProfileData.AvatarIndex;
        }

        void Update() { RefreshTopAvatarIfChanged(); }

        /// <summary>自检用：顶栏那块立绘（**一张头像图都加载不到时会是 null** —— 那种情况按出厂态处理：只剩盾）。</summary>
        public ImageQuad TopAvatar { get { return _topAvatar; } }

        // ---- §五 D：右侧聊天预览 ----
        void BuildChatPreview(Transform root)
        {
            // A332：原版 `ChatPreview` = 400 × 60（`anchor (1,1)` + `sizeDelta (400,60)` ⇒ 1475,85→1875,145）
            var p = New(root, "ChatPreview", 400f, 60f);
            Rect(p, "Closed-Chat_background", 1474.7f, 1847.0f, 85f, 145f, "Container", QBarPanel);
            // 两条消息：位置**按 Container 的 VLG 算**（pad T/B=3 · L=15，两行等高 27）；
            // 字号照 §七 表二：`fontSize` **18**（**`auto=0`，不是自适应** —— 这一处别开 autosize）、`m_fontColor` 白
            var m1 = Text(p, "<color=#00FF20>Player Name:</color> Message", 1489.7f, 1817.0f, 88f, 115f, 5,
                          Color.white, "Message Preview", 18f, QBarText);
            // 🔴 **2026-10-07（A77⑩ · 子表 E1/E2，调度台裁定选 (a)）**：原版 `Message Preview > text` 是
            //   **既不折行、也不自适应**（`MonoBehaviour_1795.json` 实读：`m_fontSize=18` · `m_enableAutoSizing=0`
            //   · **`m_TextWrappingMode=0`**）⇒ 照原版**关掉折行**。
            //   ⚠️ **订正（铁律 5）**：这行原来写「原版 **auto=0**，**只给折行宽**、不给自适应」—— 前半句对、
            //   **后半句错**：原版**没给**折行宽（它连折行都是关的），`SetWrapWidth` 是**我们主动加的**。
            //   现在留着的 `SetWrapWidth` 只为那一份**框宽**（327.3 与原版那个节点的宽一致），模式按原版关掉。
            if (m1 != null) { m1.SetWrapWidth(327.3f / 108f); m1.SetWrapping(false); }
            var m2 = Text(p, "<color=#00FF20>Player Name:</color> Message", 1489.7f, 1817.0f, 115f, 142f, 5,
                          Color.white, "Message Preview (1)", 18f, QBarText);
            // 第 2 行同上（判据 = `MonoBehaviour_1814.json`，同值）。
            if (m2 != null) { m2.SetWrapWidth(327.3f / 108f); m2.SetWrapping(false); }
            Rect(p, "40K_icon_menu_chat", 1811.0f, 1879.0f, 81.8f, 148.3f, "Button", QBarContent);
            // 🆕 2026-09-27：这颗钮**原来没接点击**（红线：不许静默失败）—— 接上，开聊天窗。
            // 原版那条链：`ChatPreview.Initialize` 把 `chatButton.onClick` 挂 `OpenChat` →
            // `WindowsManager.OpenWindow(chatWindow)`（判据 → `多人界面_入口与调用.md` §②）。
            MenuDraw.Hit(p, "ChatHit", new PxRect(1811.0f, 81.8f, 1879.0f, 148.3f), QBarOverlay, () => OpenChat());
        }

        // ---- §五 E + §九：模式卡区 ----
        /// <summary>格子的列起点与步进（§二·4 实证：`itemSize` 535×414.4 · `spacing` (20,20) · `pad L38`）⇒ **列 c 左 = 205 + 555c**。</summary>
        const float CardCol0 = 205f, CardColStep = 555f, CardW = 535f, CardH = 414.4f;
        /// <summary>🔴 **行 y 是我们挑的**：原版由 `FlexibleGridLayout` + `AdaptFlexibleGridLayoutByAspectRatio` 运行时算，
        /// JSON 里取不到绝对值（§五 E 的 `?（运行时）`，两个候选 = 内容高 945.6 时 row0 top **182.8** / 压成自然高 848.8 时 **134.4**）。
        /// 我们取 **182.8**（居中那一种）。**这一格没有原版出处，别当成复刻**。</summary>
        const float CardRow0Top = 182.8f;

        void BuildGameModes(Transform root)
        {
            // A332：原版 `GameModes` = 1752.9606 × 1008.671（`anchor (0.087,0)-(1,0.934)` ⇒ 167.04,71.33→1920,1080）
            var modes = New(root, "GameModes", ModesW, ModesH);
            // A332：原版 `Viewport` 与父件 `GameModes` **同矩形**（`anchor (0,0)-(1,1)` + `sizeDelta (0,0)`）
            var vp = New(modes, "Viewport", ModesW, ModesH);
            // A332：原版 `Content` = 74 × 945.5651（`anchor.x` 重合 + `sizeDelta.x 74`、`anchor.y` stretch 0.936）。
            //   ⚠️ **宽只有 74**：那是原版 prefab 的**模板位**（出厂 0 子、靠 `FlexibleGridLayout` 长开）
            //      ⇒ 这里照抄原版字段，⛔ 别拿「内容总宽」顶替。高 945.5651 就是本文件
            //      `CardRow0Top` 那条注释里的「内容高 **945.6**」。
            var content = New(vp, "Content", 74f, 945.5651f);

            // 原版 `Content` 出厂 **0 子**、卡全靠 liveop 数据灌（§三② B.1）。
            // 我们按已定的口径放两张（`资料/阶段二外壳_待裁决清单_0922.md` §✅）：
            //   · **Tutorial**（用户拍板「有资源就复刻」）· **Draft**（用户拍板「只做模式卡 + 点了如实提示」）
            // ⚠️ **图是按名字对上的**（`Container Image Tutorial` / `Container Image Draft`）——
            //    原版「哪个模式 → 哪张图」的映射在 **liveop 服务端**，本地查不到（§九 9·3）⇒ 这条是**我们按名取的**。
            BuildModeCard(content, "Base Game Mode Container 1x1 - Tutorial", CardCol0, CardRow0Top, CardW, CardH,
                          "Container_Image_Tutorial", "TUTORIAL", null);
            BuildModeCard(content, "Draft Game Mode Container 1x2", CardCol0 + CardColStep, CardRow0Top, CardW, 848.8f,
                          "Container_Image_Draft", "DRAFT MODE", null);

            // 🆕 **2026-09-24 用户拍板：模式卡就是「进对应模式界面」的入口** ——
            //    「是直接点击这些卡片，然后就进去这些对应模式的界面的」。
            // 🔴 **下面这三张的「模式 → 卡图 → 窗」映射是我们定的，不是复刻**：
            //    原版这张映射在 **liveop 服务端数据**里（`资料/主菜单_原版规格.md` §9·3 明写
            //    「本地 `?（查不到）`（**别自己编一套映射**）」）；而**主菜单在原版里根本没有 PLAY 钮**
            //    （只有 Home/Social/Rewards/Collection/Shop 五个导航钮）⇒ 本地入口**只能我们自己定**。
            //    卡图按名字对：练习 = `Container Image Practice 1x1` · 排位 = `Container Image Ranked` ·
            //    遭遇战**没有专属卡图**（`Practice 1x2` 也是练习的）⇒ 用全族唯一带 Skirmish 字样的
            //    `40k_main_GameMode_Skirmish`。逐条出处见 `资料/阶段二_战斗入口_原版规格.md`。
            BuildModeCard(content, "Base Game Mode Container 1x1 - Practice", CardCol0 + 2 * CardColStep, CardRow0Top,
                          CardW, CardH, "Container_Image_Practice_1x1", "PRACTICE", "practice");
            BuildModeCard(content, "Base Game Mode Container 1x1 - Skirmish", CardCol0 + 3 * CardColStep, CardRow0Top,
                          CardW, CardH, "40k_main_GameMode_Skirmish", "SKIRMISH", "skirmish");
            BuildModeCard(content, "Base Game Mode Container 1x1 - Ranked", CardCol0 + 4 * CardColStep, CardRow0Top,
                          CardW, CardH, "Container_Image_Ranked", "RANKED", "ranked");
        }

        /// <summary>模式卡点下去 = **进对应模式的界面**（用户 2026-09-24 拍板的那条路）。
        /// ⚠️ 映射是**我们定的**（见 `BuildGameModes` 上方那段）；窗还没建的那两个**出声**，不静默。</summary>
        void OpenMode(string kind)
        {
            var wm = WindowsManager.Instance;
            if (wm == null) { Debug.LogWarning("[Menu] 没有 `WindowsManager`，开不了模式窗：" + kind); return; }
            switch (kind)
            {
                case "practice":
                    var w = PracticeModePopup.Create(wm);
                    wm.OpenWindow(w);
                    Debug.Log("[Menu] 模式卡 `PRACTICE` ⇒ 开 `Practice Mode Menu`");
                    return;
                case "skirmish":
                    {
                        var win = SkirmishEventWindow.Create(wm);
                        wm.OpenWindow(win);
                        Debug.Log("[Menu] 模式卡 `SKIRMISH` ⇒ 开 `SkirmishModeEventWindow`");
                    }
                    return;
                case "ranked":
                    {
                        var win = RankedEventWindow.Create(wm);
                        wm.OpenWindow(win);
                        Debug.Log("[Menu] 模式卡 `RANKED` ⇒ 开 `RankedEventWindowV2`");
                    }
                    return;
                default:
                    Debug.LogWarning("[Menu] 模式卡 `" + kind + "` 没有对应动作（**出声**）");
                    return;
            }
        }

        /// <summary>按 §九 9·4 的「最小清单」建一张卡（1x1 与 1x2 共用；1x2 只是更高）。
        /// <paramref name="modeKind"/> 非空 ⇒ 这张卡**可点**（点它进对应模式的界面；见 `OpenMode`）。</summary>
        void BuildModeCard(Transform parent, string name, float x, float y, float w, float h, string art, string title,
                           string modeKind = null)
        {
            // A332：⚠️ **原版没有这一件** —— 原版 `GameModes/Content` **出厂 0 子**，卡是 liveop 运行时
            //   实例化的（本文件头「已知缺口」那条）⇒ 这里的矩形**是我们自己的**（= 下面 `Nine` 那一格
            //   `(x,y)→(x+w,y+h)`），⛔ 别去原版找这几个名字。
            var card = New(parent, name, w, h);

            // ① 卡图：原版是 `Background Image` 拉满 + `sizeDelta.y = +334.33`（⇒ 535×748.33）再被 **`RectMask2D` 裁到卡面**，
            //    且挂 `AspectRatioFitter(HeightControlsWidth, 1)` ⇒ 实际是 **748.33² 的正方形**，可见区 = 它**正中**的 535×414.4。
            //    🔴 **我们没有 RectMask2D 那套遮罩** ⇒ 用 **UV 裁到中间那一块**（`SetUvRect`）**等价复现**，不引入新机制。
            var tex = Art(art);
            ImageQuad cardArtQ = null;   // 🆕 A222：这张卡上**可见**的那颗图形（= 原版 `m_TargetGraphic` 指向的那一件）
            if (tex != null)
            {
                float side = h + 334.3256f;
                float u0 = (side - w) * 0.5f / side, v0 = (side - h) * 0.5f / side;
                cardArtQ = ImageQuad.Create(card, tex, Center(x, x + w, y, y + h), h / 108f,
                                            new Vector2(0.5f, 0.5f), "Background Image");
                if (cardArtQ != null)
                {
                    cardArtQ.SetAspect(w / h);
                    cardArtQ.SetUvRect(new Rect(u0, v0, 1f - 2f * u0, 1f - 2f * v0));
                    cardArtQ.SetRenderQueue(QCardArt);   // ⚠️ **不能和整屏渐变同队列**（否则谁盖谁不确定，见上面那行注释）
                }
            }
            else MissingArt.Add(art);

            // ② `TextDarkening`：贴底的纯黑带（`m_Color` **a = 0.6117647**），a(0.0037,0)-(0.9963,0) sz(−1.18×105.87)
            float bandH = 105.8657f, bandBottom = 2.6f;
            float by2 = y + h - bandBottom, by1 = by2 - bandH;
            Rect(card, null, x + 2.0f, x + w - 2.0f, by1, by2, "TextDarkening", QContent, new Color(0f, 0f, 0f, 0.6117647f));

            // ③ 标题（`Event Title`，TMP **58.8** 白 · 居中）。原版那行归 `TextDarkening` 上的
            //    **`VerticalLayoutGroup`** 排（`bundle_menus_assets_all` 实读）：
            //    `m_Padding.m_Left = 11` · `m_ChildAlignment = 3 (MiddleLeft)` · `m_Spacing = −4.2` ·
            //    `m_ChildControlWidth/Height = 0`（子女尺寸就是各自序列化值，不被布局组改）。
            //    两个孩子逐个实读：`Event Title` 高 **55.708**、`Timer With Time Description` 高 **40.729**。
            //    ⇒ 整块内容 = 55.708 + 40.729 − 4.2 = **92.237**，在 **105.866** 高的暗带里**垂直居中**
            //      ⇒ 上下各留 **6.814**；标题是**第一个孩子** ⇒ 顶在**内容最上面**：
            //      **标题顶边距暗带顶 = 6.814**、底边 = 6.814 + 55.708 = **62.522**。
            //    ⚠️ **`by2` 是暗带【底】、`by1` 是暗带【顶】**（本文件的 y 是**上到下**的像素：
            //       `by2 = y + h − bandBottom`，`y+h` 是卡底）。`titleTop = by1 + 6.8` 正好是
            //       「标题顶边距暗带顶 6.8」⇒ **这一条一直是对的**，2026-09-25 实测量到 6.80/62.5 复核过。
            float titleTop = by1 + 6.8f;
            // ⚠️ 原版 `m_text` 是占位串 `GAME MODE TITLE`，真标题运行时灌 ⇒ 这里用**模式名**（和 `Game Mode Title` 那个通用件的样例 `Draft Mode` 同口径）
            // 🔴 原版这行 TMP 带 **autosize 18→72**，框 513.7×55.7 ⇒ 让它自己缩着放进框里
            var ti = Text(card, title, x + 13f, x + 13f + 513.6959f, titleTop, titleTop + 55.708f, 8, Color.white, "Event Title", 58.8f);
            // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `m_fontSizeBase` **原文** = **36.0**。
            //    判据（本轮自己扫 `bundle_menus_assets_all`）：`Event Title`（`'GAME MODE TITLE'`，
            //    `m_fontSize 58.8` · `auto[18~72]` · 折行=1）—— **16 颗全是 `m_fontSizeBase 36.0`**
            //    （`MonoBehaviour_-1926919290004399602.json` 等）；V7 §二·3 #31 记的是全库那族
            //    （258 个，36.0×240 / 32.0×8 / …）⇒ 本条以**这一族自己**的实读为准。
            if (ti != null) ti.SetAutoFitBox(513.6959f / 108f, 55.708f / 108f, 18f, 72f, 36f);   // 原版 autosize 18→72

            // ④ `Border`：`40k_square_border` —— 原版 **Type 1 Sliced · `m_PixelsPerUnitMultiplier` 5 · `m_FillCenter` 0**，灰 **0.33962**。
            //    `m_Border` = **(13,13,13,13)**（图 64²，出自 `bundle_atlasindividual_assets_0_mainmenu/Sprite/40k_square_border.json`）
            //    ⇒ **UV 按 13px 切、角块实绘 13×5 = 65px**，中间**不画**。
            //    🔴 第一版按**整图拉伸**画 ⇒ 中间那块（图里不透明的 182 灰）把整张卡铺成了灰板 —— 这条路才是对的。
            var borderTex = Art("40k_square_border");
            if (borderTex != null)
            {
                // 🔴 **2026-10-06（A50③）：收口到公共件 `MenuDraw.Nine`** —— 原来直调
                //   `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，**拿不到 `clip` / `clipSoftness`**）。
                //   与旧代码**逐项等价**：
                //    ① **矩形** = `new PxRect(x, y, x + w, y + h)` —— 旧代码那个实参 `Center(x, x+w, y, y+h)`
                //       求的就是它的**中心**（本类 `Center` ⇒ `LayoutSpace.RectCenter`），而公共件内部是
                //       `Local(card, …)` = 同一个 `RectCenter` **减 `card.position`**
                //       ⇒ 等价要求 **`card.position == 0`**，本处成立：`card` 由本文件的 `New(parent, name)` 建
                //       （`new GameObject` + `SetParent(parent, false)` ⇒ **localPosition 出厂就是零**），
                //       一路到 `_root`（`Build()` 里 `_root = transform`，那个对象是 `Editor/MainMenuScene.cs`
                //       的 `Build()` 用 `new GameObject("MainMenu")` 建的**场景根**：无父 ⇒ 位置在原点）
                //       全是同一种零位移节点 ⇒ **整棵树的每个父级 `position` 都是零**。
                //       （这条不变式本文件到处依赖：所有器件都拿 `Center(...)` 当 `localPosition` 用；
                //         自检 `CheckAt` / `CheckAtWorld` 也是拿 `Center(...)` 当期望值比位置。）
                //    ② **尺寸** = `w / 108f` 与 `LayoutSpace.Px(w)` **同值**（`DesignHeight` = 10 ⇒ `Px` ⇒ `px/108`）。
                //    ③ **队列 = `QOverlay`** · **tint = (0.33962, 0.33962, 0.33962, 1)**（旧代码建完逐块设的就是这两样，
                //       公共件会替我们设；`GetComponentsInChildren<ImageQuad>()` 两边都不含未激活件 ⇒ 同一批子块、同一先后）
                //       · **`fillCenter = false`**（边框不填中间）· `borderOutPx = (65,65,65,65)` 原样透传 · `clip` 一律 `null`。
                //   ⇒ 原来那圈 `foreach` **删掉了**：它设的两个值与公共件内部设的**同值**，
                //     留着就是「同一条规则写两处」（将来改一处、另一处静默不动）。
                MenuDraw.Nine(card, borderTex, new PxRect(x, y, x + w, y + h), new Vector4(13, 13, 13, 13),
                              64f, 64f, QOverlay, new Color(0.33962f, 0.33962f, 0.33962f, 1f), false, "Border",
                              new Vector4(65, 65, 65, 65));
            }

            // ⑤ 倒计时那一行（`Starts in:` + 时钟 + 剩余时间）**本批不画** —— 它是 liveop 的活动倒计时，本地没有数据源。
            //    不静默：说一声。
            Debug.Log($"[Menu] 模式卡 `{name}`：倒计时那一行**没画**（`Starts in:` + 时钟 + 剩余时间 —— 原版是 liveop 活动数据，本地没有）");

            // ⑥ 点击区（**只有模式卡有**）：原版卡根上挂 `LiveopMenuContainer` + `EverguildButton`，
            //    开哪扇窗由 liveop 数据给的事件对象决定（本地查不到）⇒ 我们自己接（见 `OpenMode`）。
            if (!string.IsNullOrEmpty(modeKind))
            {
                // A332：⚠️ **原版也没有这一件**（同一族：原版卡自己挂 `EverguildButton`，没有另建的命中区子件）
                //   ⇒ 矩形**是我们自己的**（= 卡那一格 `w × h`）。
                var hitGo = New(card, "Hit", w, h);
                var q = ImageQuad.Create(hitGo, CardArt.Solid(), Center(x, x + w, y, y + h), h / 108f,
                                         new Vector2(0.5f, 0.5f), "Hit");
                if (q != null)
                {
                    q.SetAspect(w / h);
                    q.SetTint(new Color(0f, 0f, 0f, 0f));
                    q.SetRenderQueue(QOverlay + 1);      // 盖在卡之上，才吃得到点击
                }
                string kind = modeKind;
                var wb = hitGo.gameObject.AddComponent<WindowButton>();
                wb.onClick = () => OpenMode(kind);
                // ============================================================ 🆕 2026-10-10（A222）
                // 色偏要打在**这张卡上可见的那颗图形**（`Background Image`）上 —— `Hit` 是另建的透明命中区，
                // 色偏原来全落在它身上 ⇒ 画面上一点变化都没有。
                // 🔴 **判据（原版 prefab 现读，不再是推测）**：`bundle_menus_assets_all` 的
                //    `Base Game Mode Container 1x1`（GO 根，pid 与 `LiveopMenuContainer` 同级那五颗组件里）
                //    挂 `EverguildButton`（MB `4126037038486929469`）：
                //      `m_Transition = 1`（ColorTint）· **`m_TargetGraphic` = `7827625613328400445`**
                //      —— 逐跳解出来 = **`Background Image` 那颗 GO 上的 `UIParallaxImage`**
                //      （`GameObject/Background Image_241192310724640829.json` 的组件之一；
                //       该类是 `Image` 的子类 ⇒ 它就是那一格的 `Graphic`）。
                //    `m_Colors` 四格 = 全库默认那一组（`HL/SEL` 0.9607843 · `PR/DIS` 0.7843137 · `NOR` 白）
                //      ⇒ **这三张卡不用覆盖 `HighlightKey`**（与导航钮那五颗不同，逐条实读）。
                //    判据全文 → `资料/普查产出_1009/查证V3_口径三件.md` §二。
                if (cardArtQ != null) wb.TintOn(cardArtQ);
            }
        }

        // ============================================================ 诊断

        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("MainMenu：");
            sb.Append(_root == null ? "还没建" : _root.childCount + " 个顶层节点");
            sb.Append($" · 取不到的图 {MissingArt.Count} 张");
            return sb.ToString();
        }

        /// <summary>自检用：按名字找节点（`GetComponentsInChildren` 包含 inactive）。</summary>
        public Transform Find(string name)
        {
            if (_root == null) return null;
            foreach (var t in _root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }
    }
}
