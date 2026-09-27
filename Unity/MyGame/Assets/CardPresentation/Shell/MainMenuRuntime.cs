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
//   · **`Resources Container` 里的 5 个资源格** —— 原版同样是运行时实例化（`Resource Counter Item` 38.07 高）。
//   · **字号**：§二/§五 两张表**没有 TMP 的 fontSize** ⇒ 这里的字号是**按矩形高推的档位**，
//     **标注为「我们挑的」**，等补到原版字号再换（别把它当原版值）。
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
                   Color color, string name, float fontPx = 0f)
        {
            var lb = Label.Create(parent, text, Center(x1, x2, y1, y2), scale, color,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(QText);
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

        static Transform New(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

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

        // ============================================================ 建

        /// <summary>建整个主菜单。**自检与运行时调同一个**。</summary>
        public void Build()
        {
            _root = transform;
            for (int i = _root.childCount - 1; i >= 0; i--) DestroySafe(_root.GetChild(i).gameObject);
            MissingArt.Clear();

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
            var go = New(root, "Background");
            var quad = ImageQuad.Create(go, CardArt.Gradient(new Color(0.224f, 0.012f, 0.020f),
                                                             new Color(0.047f, 0f, 0.016f), 188f),
                                        Vector3.zero, LayoutSpace.DesignHeight,
                                        new Vector2(0.5f, 0.5f), "Gradient");
            if (quad == null) return;
            quad.SetAspect(LayoutSpace.DesignWidth / LayoutSpace.DesignHeight);
            quad.SetRenderQueue(QBg);
        }

        // ---- §五 C：左竖导航 ----
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
            var p = New(root, "Navigation Panel");
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

            var holder = New(p, "Buttons Container");
            // 🔴 五个按钮的 y 是**按 VLG 算的**（spacing −16.35 · UpperCenter · padTop −5 · 不控子尺寸）
            //    —— §五 C 表；JSON 里它们的 pos 全是 (0,0)、5 个完全重合，**别回头去查**。
            // 字号/字色照 §七 表二：标签 `fontSize` **33**（`COLLECTION` 是 **30.45**）、`m_fontColor` **金 (0.9961,0.9294,0.7098)**
            NavButton(holder, 0, "Home",       "40k_main_bt_play",       143.4f, 313.1f, "PLAY",       156.3f,  4.6f, 160.9f, 146.4f, 33f);
            NavButton(holder, 1, "Collection", "40k_main_bt_collection", 296.7f, 466.4f, "COLLECTION", 140.0f, 12.8f, 152.8f, 298.4f, 33f);
            // ⚠️ `COLLECTION` 传的是 **max 33**（不是原版存下来的 30.45）—— 那个 30.45 是**自适应之后的结果**，
            //    让 autosize 自己缩出来才和原版同一条路（它框里放不下，会自己缩小）。
            NavButton(holder, 2, "Shop",       "40k_main_bt_shop",       450.1f, 619.8f, "SHOP",       140.0f, 12.8f, 152.8f, 461.8f, 33f);
            NavButton(holder, 3, "Rewards",    "40k_main_bt_rewards",    603.4f, 773.1f, "REWARDS",    140.0f, 12.8f, 152.8f, 615.1f, 33f);
            NavButton(holder, 4, "Social",     "40k_main_bt_friends",    756.7f, 926.4f, "SOCIAL",     140.0f, 12.8f, 152.8f, 768.4f, 33f);
        }

        /// <param name="idx">第几个按钮（用来判「选中态」）。</param>
        /// <param name="y1">按钮顶；`iw`/`ix1`/`ix2` = 图标那格的宽与左右；`iy1` = 图标顶（都来自 §五 C 表）。</param>
        /// <param name="fontPx">原版 TMP 的 `m_fontSize`（画布像素）。</param>
        void NavButton(Transform parent, int idx, string name, string art, float y1, float y2, string label,
                       float iw, float ix1, float ix2, float iy1, float fontPx)
        {
            const float cx0 = 82.69f;      // 键的水平中心（原版 `Highlight` 的矩形是 -0.3..164.0 ⇒ 中心 81.85）
            var b = New(parent, "Main Menu Navigation Button - " + name);
            // 选中态高亮：亮底图 × `m_Color (1,0.0805,0,1)` = **正红**（只有选中的那个画）
            if (idx == SelectedNav)
                Rect(b, "40k_main_bt_selected_BW", -0.3f, 164.0f, y1, y2, "Selected highlight", QPanel,
                     new Color(1f, 0.08054f, 0f, 1f));

            var icon = Art(art);        // 先确认图在不在（不在就别画一个空按钮）
            if (icon != null)
            {
                var q = ImageQuad.Create(b, icon, Center(ix1, ix2, iy1, iy1 + iw), iw / 108f,
                                         new Vector2(0.5f, 0.5f), "Image");
                if (q != null) { q.SetAspect((ix2 - ix1) / iw); q.SetRenderQueue(QContent); }
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
            if (navText != null) navText.SetAutoFitBox(146.92f / 108f, 39.39f / 108f, 18f, 33f);   // 原版 autosize 18→33
            // 红点：亮图 × `m_Color (0.7358,0.7358,0.7358,1)` = **中灰**（原版这就是「无内容/禁用」的灰点）
            // 🔴 **只有 REWARDS（第 4 个导航钮）有真实的通知源** —— 原版它由 `Missions.CheckNotification` 驱动；
            //    其余四个我们**没有通知源** ⇒ 按 `Hide()` 的样子 **alpha = 0**（不是画一个假的灰点）。
            Rect(b, "40K_notification_number", 117.8f, 152.8f, y1 + 91.7f, y1 + 126.7f, "Badge Highlight", QContent,
                 BadgeAlpha(idx == 3 && DailyData.RewardsHasBadge));

            // 点击区（整键）。原版每个导航钮上挂的是 `OpenWindowButton{windowToOpenPrefab, windowPayload}`：
            // `0 Main(PLAY)` 例外 —— 它开的是**场景内的** `MainMenuWindow`（正本 §三 第 1 条）。
            var hitGo = New(b, "Hit");
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

        /// <summary>
        /// 开收件箱（原版 `Inbox Menu`，由顶栏 `InboxBtn` 上的 `OpenWindowButton` 开）。
        /// ⚠️ 单机没有服务器 ⇒ 里面是**空态**（原版没消息时也是这个样子），**不是没做**。
        /// </summary>
        public InboxWindow OpenInbox()
        {
            var wm = WindowsManager.EnsureHost();
            var win = InboxWindow.Create(wm);
            wm.OpenWindow(win);
            return win;
        }

        /// <summary>
        /// 开设置窗（原版 `Main Menu Settings Window`，由顶栏 `SettingsBtn` 上的 `OpenWindowButton` 开）。
        /// 🔴 **2026-09-26 才有这个入口** —— 那颗齿轮从建出来起**点了没反应**（红线：不许静默失败）。
        /// 我们只建了原版五页里的三页（图像 / 音频 / 联机），见 `Shell/SettingsWindow.cs` 文件头 ③。
        /// </summary>
        public SettingsWindow OpenSettings()
        {
            var wm = WindowsManager.EnsureHost();
            var win = SettingsWindow.Create(wm);
            wm.OpenWindow(win);
            return win;
        }

        /// <summary>
        /// 开社交窗（原版 `Social Submenu Variant` = `SocialMenuWindow`，由左竖导航第 5 键
        /// `40k_main_bt_friends` 上的 `OpenWindowButton` 开）。
        /// 🆕 2026-09-27：入口链已查实（`多人界面_入口与调用.md` §①）⇒ **照原版接线**，不是我们挑的。
        /// </summary>
        public SocialWindow OpenSocial()
        {
            var wm = WindowsManager.EnsureHost();
            var win = SocialWindow.Create(wm);
            wm.OpenWindow(win);
            return win;
        }

        /// <summary>
        /// 开聊天窗（原版 `ChatPanel`，由 `ChatPreview.OpenChat` 开）。
        /// 🔴 主菜单右上那颗 `ChatPreview` 的 `40K_icon_menu_chat` 钮**从建出来起就没接点击** ——
        /// 这一条把它接上（原版那条链：`chatButton.onClick → OpenChat → WindowsManager.OpenWindow`，
        /// 判据 → `多人界面_入口与调用.md` §②）。
        /// </summary>
        public ChatPanel OpenChat()
        {
            var wm = WindowsManager.EnsureHost();
            var win = ChatPanel.Create(wm);
            wm.OpenWindow(win);
            return win;
        }

        /// <summary>
        /// 开**对局历史弹窗**（原版 `Battle Log Popup`）。
        /// 🔴 **界面里没有入口，是故意的 —— 原版的打开点查不到**：它只在 `WindowsManager` 的预载表里，
        /// 打开方式是 `OpenWindow&lt;BattleLogPopup&gt;()`，而那个泛型调用的产物缺失
        /// （普查 `对局历史_行模板与弹窗.md` §E 追过 74 处调用点 + 全 91 包按字节搜 GUID/pid）。
        /// ⇒ 我们**不编**一个入口出来；这个方法留给「将来找到真入口」或自检直接用。
        /// 要接的话接在哪 —— **等用户拍板**（记在 `项目任务.md` §三 第 18 条）。
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
        /// </summary>
        public LeaderboardWindow OpenLeaderboard(LeaderboardKind kind)
        {
            var wm = WindowsManager.EnsureHost();
            var win = LeaderboardWindow.Create(wm, kind);
            wm.OpenWindow(win);
            return win;
        }

        /// <summary>
        /// 开玩家档案窗（原版 `Player Profile Window`，由**顶栏头像**上的 `OpenWindowButton` 开）。
        /// 🔴 **2026-09-27 才有这个入口** —— 头像块从建出来起**点了没反应**（跟齿轮当初一样，静默失败）。
        /// 六个页签：Profile / Avatar / Title / Battle Log / Trophies / Ranking；**出厂落在 Title 页**
        /// （原版唯一 `m_IsActive=true` 的页签根）。判据 → `资料/阶段二_多人界面_原版规格.md` §2·1。
        /// </summary>
        public PlayerProfileWindow OpenProfile()
        {
            var wm = WindowsManager.EnsureHost();
            var win = PlayerProfileWindow.Create(wm);
            wm.OpenWindow(win);
            return win;
        }

        /// <summary>开奖励窗（原版 `MainMenuRewardsWindow`）。回点导航钮那一步照原版做
        /// （`DF:MainMenuRewardsWindow__Open.c:14-16`）。</summary>
        public RewardsWindow OpenRewards()        {
            var wm = WindowsManager.EnsureHost();
            var win = RewardsWindow.Create(wm);
            wm.OpenWindow(win);
            SelectNav(3);
            return win;
        }

        /// <summary>开收藏窗（原版 `Collection Menu Variant`，由左竖导航的 COLLECTION 开）。
        /// 🔴 那个钮挂的 `OpenWindowButton` 是 `closeOtherMenus = 1`
        /// （`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2494.json:16-33`）⇒ `closeAll: true`。
        /// 窗口参数实证：`type=0 Fullscreen` · `placement=5 Canvas` · `closeOnESC=1` · `extraScaleSmallScreen=1.0`。</summary>
        public CollectionWindow OpenCollection()
        {
            var wm = WindowsManager.EnsureHost();
            var win = CollectionWindow.Create(wm);
            wm.OpenWindow(win, null, true);
            SelectNav(1);
            return win;
        }

        /// <summary>开商店（原版 `Shop Menu Variant`，由左竖导航的 SHOP 开）。
        /// 🔴 窗口参数**与奖励窗不同**：`placement = 10 (World)`（奖励窗是 5 Canvas）· `closeOnESC = 1`。
        /// ⚠️ 商品数据**是我们编的**（原版在服务端）—— 见 `ShopData.cs` 文件头。</summary>
        public ShopWindow OpenShop()
        {
            var wm = WindowsManager.EnsureHost();
            var win = ShopWindow.Create(wm);
            // 🔴 **原版这里是 `closeAll = true`** —— 主菜单那个 SHOP 钮挂的 `OpenWindowButton`
            //    `closeOtherMenus = 1`（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2495.json:32`），
            //    走 `WindowsManager.OpenWindow(win, data, closeAll: true)`（`OpenWindowButton__OpenWindow.c:33`）。
            wm.OpenWindow(win, null, true);
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
            var bar = New(root, "Upper bar");
            Rect(bar, "UI_Main_Upper_bar", -11.7f, 1920f, 0f, 71.3f, "Background", QPanel);

            // 齿轮 + 红点
            var settings = New(bar, "SettingsBtn");
            // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版 `Upper bar/SettingsBtn` 那格
            //   `m_PreserveAspect = 1`（RT1560·GO496·MB2526），贴图 `UI_Settings_Icon` **179×179**
            //   塞进 87.78×61.73 的框 ⇒ 原版只画 **61.73²**（居中），我们拉伸 ⇒ **宽 1.42×**。
            var gear = Rect(settings, "UI_Settings_Icon", 1803.1f, 1890.9f, 4.6f, 66.4f, "Image", QContent,
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
            Rect(settings, "40K_notification_number", 1865.9f, 1890.9f, 4.4f, 29.4f, "Badge Highlight", QContent,
                 BadgeAlpha(false));

            // 三个顶栏按钮（位置**按 HLG 算**：spacing 9.75 · MiddleLeft）
            var btns = New(bar, "TopBarButtons");
            var inbox = New(btns, "InboxBtn");
            // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版 `Upper bar/TopBarButtons/InboxBtn`
            //   `m_PreserveAspect = 1`（RT1561·GO497·MB2529），贴图 `40K_notification` **135×105**
            //   塞进 55×40 的框 ⇒ 原版实绘 **51.43×40**，我们 55 宽 ⇒ **宽 6.5%**（轻，但同一条判据）。
            var inboxImg = Rect(inbox, "40K_notification", 425.3f, 480.3f, 15.5f, 55.5f, "Image", QContent,
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
                                  "Badge Highlight", QContent, BadgeTint);
            if (inboxBadge != null)
                inboxBadge.SetTint(new Color(BadgeTint.r, BadgeTint.g, BadgeTint.b,
                                             DailyData.InboxHasBadge ? 1f : 0f));
            Rect(btns, "40K_icon_duel", 490.1f, 537.6f, 11.8f, 59.2f, "Challenge button", QContent);
            // `Feedback Button`（565.4..615.4, 10.5..60.5）出厂 `activeSelf=False` ⇒ **不建**（见文件头纪律 ③）

            BuildPlayerProfile(bar);

            // `Resources Bar`（1131.5..1802.5, −0.1..71.1）：⚠️ **原版这一格没有任何 Image**
            // （§二 表 #27 的图那一列写的是「无；+Canvas1079(嵌套)」）⇒ **不画底**。
            // 🔴 2026-09-22 自纠：第一版我在这里凭空加了一层 `40k_topmarquee_currency_display_BW`，
            //    渲染出来是**右上角一块浅灰药丸**，而原版实拍那里是空的 —— 典型的「我们自加的」（§10·3 找茬点 6）。
            var resBar = New(bar, "Resources Bar");
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
            var p = New(parent, "Player Profile");
            Rect(p, "40k_main_player_frame", 23.0f, 411.0f, 11.6f, 135.6f, "Background", QPanel);
            // 名字条底：亮图 × `m_Color (0.396,0.1925,0.3095)` = **暗紫红**（`m_Type` = **1 Sliced**）
            Rect(p, "40k_topmarquee_currency_display_BW", 25.6f, 472.1f, 14.9f, 60.5f, "Planer Name Background",
                 QContent, new Color(0.39623f, 0.19251f, 0.30954f, 1f));
            // 字号照 §七 表二：`Player Name` fontSize **32**（带 **autosize 10→32**）、`m_fontColor` **(0.9686,0.9137,0.7137)**
            var pn = Text(p, "Player Name", 136.9f, 401.9f, 13.7f, 61.7f, 8,
                          new Color(0.9686f, 0.9137f, 0.7137f), "Player Name", 32f);
            if (pn != null) pn.SetAutoFitBox(265f / 108f, 48f / 108f, 10f, 32f);   // 原版 autosize 10→32

            var av = New(p, "Avatar Item Small");
            var avBorder = Rect(av, "Player_Profile_Border", -10.0f, 165.5f, 9.0f, 139.1f, "Border", QAvatarFrame,
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
            var lvl = New(p, "Icon/Player Level");
            Rect(lvl, "40k_topmarquee_currency_gold", 117.5f, 170.6f, 54.3f, 107.4f, "Icon", QContent);
            // `Player Level Text`：§七 表二 —— fontSize **37.2**，**autosize 18→37.2**
            var lv = Text(lvl, "-", 124.3f, 163.7f, 61.1f, 100.5f, 7, Color.white, "Player Level Text", 37.2f);
            if (lv != null) lv.SetAutoFitBox(39.4f / 108f, 39.4f / 108f, 18f, 37.2f);   // 原版 autosize 18→37.2
        }

        // ---- 顶栏那块头像立绘（2026-09-27 建；判据 → 上面 `BuildPlayerProfile` 的更正块）----

        ImageQuad _topAvatar;
        int _topAvatarIdx = -1;

        /// <summary>顶栏立绘那一格。**由边框那一格推出来**（见 `BuildTopAvatar` 的推导）：
        /// 中心 = 边框中心 + `(2.6, −6.427)` · 尺寸 = 边框 × `(1.5776, 1.5711)` ⇒ 276.9×204.4。
        /// ⚠️ 它**比盾牌框大**（会溢出屏幕左上角），但立绘贴图的实心部分只占 43%×60% ⇒ 露出来的只有人像。</summary>
        const float TopAvatarL = -58.09f, TopAvatarR = 218.79f, TopAvatarT = -34.59f, TopAvatarB = 169.83f;

        /// <summary>把「玩家现在选的头像」画到顶栏那面盾**上面**。
        /// 🔴 **队列必须比边框高**（`QAvatarFrame` < `QContent`）—— 盾的中心是不透明黑，反了就是一块黑。
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
            _topAvatar = RectTex(av, tex, TopAvatarL, TopAvatarR, TopAvatarT, TopAvatarB, "Image", QContent, true);
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
            var p = New(root, "ChatPreview");
            Rect(p, "Closed-Chat_background", 1474.7f, 1847.0f, 85f, 145f, "Container", QPanel);
            // 两条消息：位置**按 Container 的 VLG 算**（pad T/B=3 · L=15，两行等高 27）；
            // 字号照 §七 表二：`fontSize` **18**（**`auto=0`，不是自适应** —— 这一处别开 autosize）、`m_fontColor` 白
            var m1 = Text(p, "<color=#00FF20>Player Name:</color> Message", 1489.7f, 1817.0f, 88f, 115f, 5,
                          Color.white, "Message Preview", 18f);
            if (m1 != null) m1.SetWrapWidth(327.3f / 108f);   // ⚠️ 这一处原版 **auto=0**，只给折行宽、不给自适应
            var m2 = Text(p, "<color=#00FF20>Player Name:</color> Message", 1489.7f, 1817.0f, 115f, 142f, 5,
                          Color.white, "Message Preview (1)", 18f);
            if (m2 != null) m2.SetWrapWidth(327.3f / 108f);
            Rect(p, "40K_icon_menu_chat", 1811.0f, 1879.0f, 81.8f, 148.3f, "Button", QContent);
            // 🆕 2026-09-27：这颗钮**原来没接点击**（红线：不许静默失败）—— 接上，开聊天窗。
            // 原版那条链：`ChatPreview.Initialize` 把 `chatButton.onClick` 挂 `OpenChat` →
            // `WindowsManager.OpenWindow(chatWindow)`（判据 → `多人界面_入口与调用.md` §②）。
            MenuDraw.Hit(p, "ChatHit", new PxRect(1811.0f, 81.8f, 1879.0f, 148.3f), QOverlay, () => OpenChat());
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
            var modes = New(root, "GameModes");
            var vp = New(modes, "Viewport");
            var content = New(vp, "Content");

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
            var card = New(parent, name);

            // ① 卡图：原版是 `Background Image` 拉满 + `sizeDelta.y = +334.33`（⇒ 535×748.33）再被 **`RectMask2D` 裁到卡面**，
            //    且挂 `AspectRatioFitter(HeightControlsWidth, 1)` ⇒ 实际是 **748.33² 的正方形**，可见区 = 它**正中**的 535×414.4。
            //    🔴 **我们没有 RectMask2D 那套遮罩** ⇒ 用 **UV 裁到中间那一块**（`SetUvRect`）**等价复现**，不引入新机制。
            var tex = Art(art);
            if (tex != null)
            {
                float side = h + 334.3256f;
                float u0 = (side - w) * 0.5f / side, v0 = (side - h) * 0.5f / side;
                var q = ImageQuad.Create(card, tex, Center(x, x + w, y, y + h), h / 108f,
                                         new Vector2(0.5f, 0.5f), "Background Image");
                if (q != null)
                {
                    q.SetAspect(w / h);
                    q.SetUvRect(new Rect(u0, v0, 1f - 2f * u0, 1f - 2f * v0));
                    q.SetRenderQueue(QCardArt);   // ⚠️ **不能和整屏渐变同队列**（否则谁盖谁不确定，见上面那行注释）
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
            if (ti != null) ti.SetAutoFitBox(513.6959f / 108f, 55.708f / 108f, 18f, 72f);   // 原版 autosize 18→72

            // ④ `Border`：`40k_square_border` —— 原版 **Type 1 Sliced · `m_PixelsPerUnitMultiplier` 5 · `m_FillCenter` 0**，灰 **0.33962**。
            //    `m_Border` = **(13,13,13,13)**（图 64²，出自 `bundle_atlasindividual_assets_0_mainmenu/Sprite/40k_square_border.json`）
            //    ⇒ **UV 按 13px 切、角块实绘 13×5 = 65px**，中间**不画**。
            //    🔴 第一版按**整图拉伸**画 ⇒ 中间那块（图里不透明的 182 灰）把整张卡铺成了灰板 —— 这条路才是对的。
            var borderTex = Art("40k_square_border");
            if (borderTex != null)
            {
                var b = ImageQuad.CreateNineSlice(card, borderTex, new Vector4(13, 13, 13, 13), 64f, 64f,
                                                  Center(x, x + w, y, y + h), w / 108f, h / 108f, "Border",
                                                  new Vector4(65, 65, 65, 65), false);
                foreach (var q in b.GetComponentsInChildren<ImageQuad>())
                {
                    q.SetTint(new Color(0.33962f, 0.33962f, 0.33962f, 1f));
                    q.SetRenderQueue(QOverlay);
                }
            }

            // ⑤ 倒计时那一行（`Starts in:` + 时钟 + 剩余时间）**本批不画** —— 它是 liveop 的活动倒计时，本地没有数据源。
            //    不静默：说一声。
            Debug.Log($"[Menu] 模式卡 `{name}`：倒计时那一行**没画**（`Starts in:` + 时钟 + 剩余时间 —— 原版是 liveop 活动数据，本地没有）");

            // ⑥ 点击区（**只有模式卡有**）：原版卡根上挂 `LiveopMenuContainer` + `EverguildButton`，
            //    开哪扇窗由 liveop 数据给的事件对象决定（本地查不到）⇒ 我们自己接（见 `OpenMode`）。
            if (!string.IsNullOrEmpty(modeKind))
            {
                var hitGo = New(card, "Hit");
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
