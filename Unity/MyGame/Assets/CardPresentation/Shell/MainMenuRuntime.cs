// MainMenuRuntime.cs — 主菜单（阶段二第 1 层）的**唯一建界面处**
//
// 🔴 **2026-10-17（B25）一处例外**：**顶栏那一条带子**（`Upper bar` + `Player Profile` + `Resources Bar`，
//    含那 4 颗钮的点击接线 = 齿轮 / 信封 / 头像 / 挑战）**建在共用件 `Shell/TopBar.cs` 里** ——
//    卡组编辑那一屏（独立场景 `DeckEditor.unity`）要的是**同一条顶栏**，而**两份一定会漂**
//    （实测漂过：资源计数器次序反了 · 信箱红点恒 alpha 0，判据 → `TopBar.cs` 文件头）⇒ 收口到那边。
//    本文件里那句 `TopBar.Build(...)` 只是**调用点**；顶栏的**数据**（`CounterSpecs` 一族 + 三个开窗入口）
//    仍然住在本文件（它们的注释带着逐条 pid / 反编译出处）。
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
//   ③ **出厂 `activeSelf=False` 的一律不建**（`Profile border` / `Tutorial highlight` …
//      —— 原版是运行时按状态开的，我们还没实现那套状态就先别摆一个常显的假件）。
//      🔴 **2026-10-11（`A1522①` / `A1523①`）就地订正（铁律 5）：顶栏那两颗是【例外】——【建、但出厂关着】。**
//        触发它的事实 = 同一排三颗的处理**自相矛盾**：`InboxBtn` 常显 · `Challenge button` 我们建成**常显**
//        （原版运行期实况是**关**的）· `Feedback Button` 我们**根本不建**（原版是**建了、出厂关着**）。
//        ⇒ 主对话裁定：**两颗统一「建出来 + `SetActive(false)`」**（结构 / 队列 / 名字照原版留全，
//        ⛔ 不删节点 —— 删了 `Editor/{MainMenuScene,DeckScene}.cs` 里按名字找它的断言会红）。
//        判据（逐颗）与落地 → 共用件 `Shell/TopBar.cs`（那一排建在那边）· 判据全文 →
//        `资料/普查产出_第十七会话/P_信箱与名牌.md`。⛔ 别把这两颗退回「不建」。
//      ⚠️ **其余那些出厂 `False` 的件（`Profile border` / `Tutorial highlight` 等）照旧不建** ——
//        本条只给**顶栏那两颗**开口子（它们有「原版实况 = 关着」+「结构照原版留全」两条判据）。
//
// ⚠️ **已知缺口（本批没做，别当已完成）**：
//   · **模式卡**（`GameModes/Content` 里那些 535×414 的卡）—— 原版出厂 **0 子**、全靠 liveop 数据灌，
//     要先读 `bundle_menus_assets_all` 里卡 prefab 的内部结构 ⇒ 下一批。
//   · ~~**`Resources Container` 里的 5 个资源格**~~ ✅ **2026-10-13（A374）已建** —— 🔴 **2026-10-17（B25）
//     建树搬进了共用件 `Shell/TopBar.cs` 的 `BuildResourcesBar`**（原版建表序 / 常显 3 颗 / 显隐规则 /
//     上限机制 / 逐格几何都有出处）；本文件只剩**那几张数据表**（`CounterSpecs` 一族）。
//     ⚠️ **还差 4 张币种小图标**（本地 `Resources/Art/ui_menu/` 只导了 2 张）—— 那几颗只画药丸 + 数字，
//     名字会进 `MissingArt`（**`TopBar.BuildResourcesBar` 末尾**那条警告点名）。判据与修法 → `资料/普查产出_1013/W374_资源计数器.md` §六。
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

        void Awake() { Instance = this; GameStaticData.LoadSharedDefaultValues(); LiveOpsManager.EnsureStarted(); ReadTextInHandFormatAtBoot(); }   // 账 460：原版 `PlayerDataManager.LoadPlayerData` 在**读玩家存档之前**先调 `LoadSharedDefaultValues()`（13 次写入的次序/默认值见 `Shell/GameStaticData.cs`）；这里 = 我们的等价调用点。⚠️ 它只把**默认值**灌进 `GameStaticData` 的静态量 —— 音量 / smallScreenUI 等有独立存档的那几格**不会**被冲掉（判据见那个文件头）

        void Start()
        {
            // 按 Play 的入口 —— **和自检是同一条路**（本工程的规矩）
            if (_root == null) Build(); IngestPracticeResultsAtMenuStart();   // 账 584/585：把上一场练习局吃进战绩
        }

        void OnDestroy() { UnhookMenuRebuildBroadcasts(); if (Instance == this) Instance = null; }   // 🆕 B89-E30：摘掉 `L412`/`L477` 那两条静态订阅（否则静态事件攒着已销毁的实例）

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
        ///    `fontSize` 到实际字形之间还差一层字体资产的换算，**别自己乘 108**。
        ///
        /// <para>🆕 **2026-10-19（A1205）：补上 `wrapPx` / `autoMinPx` / `autoMaxPx` / `autoBasePx` 四个形参**
        /// —— 本方法此前**一个 autosize 形参都没有** ⇒ 走本层的调用点**不是「忘了传」是「没地方传」**
        /// （本文件 `NavButton` / `BuildModeCard` 那两处只能自己再补一句 `SetAutoFitBox`）。
        /// 四个新形参**全默认 `0`** ⇒ **既有调用点一字不改、行为逐位不变**。</para>
        ///
        /// <para>🔴 **契约取哪一档 —— 照 `A1195` 那条裁定「从原版那颗 TMP 真有的组合倒推」**：
        /// 本方法今天**没有一处**要接自适应（`R5_包装层菜单族.md` §2·D 两行 = `ChatPreview` 那两条消息，
        /// 原版 `m_enableAutoSizing = 0`、折行 `0`）⇒ **「原版组合」倒推不出形状**。
        /// ⇒ 取 **`Shell/SettingsWindow.cs` 的 `Text`（`A1181`）那一档：两个开关拆开**
        /// （`wrapPx` = **折行开关** · `autoMinPx` = **自适应开关**），理由是它**严格更宽**
        /// —— `A1181` 能表达「**折行 = 0 且开着自适应**」这个**原版真有的组合**，
        /// 而 `Shell/MenuWindowBase.cs` 的 `Text`（`A1173`）那条闸（折行与自适应共用一个 `wrapPx` 开关）
        /// **表达不了**；本方法今天无人传这四个形参 ⇒ 选宽的一档**不会**造成任何偏离。
        /// ⛔ **这不是「把两个漏斗改成一个样」**（`A1195` 明令禁止的那件事）—— 本方法**自成一档**，
        /// 与那两个漏斗各自的契约**并存**；`Shell/SettingsWindow.cs` 那一档的契约原文
        /// （含「为什么它不能和 `TextCore` 那档统一」）→ 它的方法头，⛔ 别在这儿抄第二份。</para>
        ///
        /// <para>🔴 **量纲**：`wrapPx` / `autoMinPx` / `autoMaxPx` / `autoBasePx` 与本方法的 `fontPx` **同一档**
        /// （= 原版 TMP 的**设计 px**）⇒ 内部与 `fontPx` 同路走 `LayoutSpace.Px()`；
        /// 而 `SetAutoFitBox` 的后三个实参按它的量纲注**不过** `Px()`（同 `MenuWindowBase.Text:337`）。
        /// ⚠️ **本方法没有 `MenuDraw.TextCore` 那条 `fontPx &gt; autoMinPx` 的闸**（走 `Label.SetAutoFitBox`
        /// 的这条路本来就没有）⇒ 若将来给某站传了 `autoMinPx &gt;= fontPx`，TMP 会把字**放大到下限**
        /// —— **那不是本方法的 bug、是那一站的值**（同 `A1208` 的口径：先查值、别先加闸）。</para></summary>
        Label Text(Transform parent, string text, float x1, float x2, float y1, float y2, int scale,
                   Color color, string name, float fontPx = 0f, int queue = QText,
                   float wrapPx = 0f, float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f,
                   TmpFont.Face face = TmpFont.Face.Cjk)
        {
            var lb = Label.Create(parent, text, Center(x1, x2, y1, y2), scale, color,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(queue);        // 🔴 顶栏那几段文字要跟着整条带子走（传 `QBarText`；2026-10-11 起那条带子**降到窗之下**）
            // 🔴 **2026-10-17（A1024③）**：可选字体面 —— 默认 `Cjk` ⇒ 旧调用点零行为变化。
            //    ⚠️ **次序**：`SetFace` 必须在 `SetGlyphHeight` 之前（字号按 `ActualFace` 的 em 换算）。
            if (face != TmpFont.Face.Cjk) lb.SetFace(face);
            if (fontPx > 0f) lb.SetGlyphHeight(fontPx / 108f);
            // 🆕 **A1205**：自适应 + 折行两条路（逐字照 `Shell/SettingsWindow.cs:4741-4748` 那一档的算法，
            //    只是落在裸 `Label` 上 —— 本方法没有 `MenuDraw.Text` 那条路可走）。
            //    `fitW > 0` 才动：`autoMinPx <= 0` 且 `wrapPx <= 0` ⇒ `fitW = 0` ⇒ **一个分支都不进 = 旧行为逐位不变**。
            float fitW = wrapPx > 0f ? wrapPx : (autoMinPx > 0f ? x2 - x1 : 0f);
            if (fitW > 0f)
            {
                lb.SetWrapWidth(LayoutSpace.Px(fitW));
                if (autoMinPx > 0f)
                    // 上限的兜底 `autoMaxPx <= 0 ⇒ fontPx` 与 `MenuDraw.TextCore` / `Text` 同义（A333）
                    lb.SetAutoFitBox(LayoutSpace.Px(fitW), LayoutSpace.Px(y2 - y1), autoMinPx,
                                     autoMaxPx > 0f ? autoMaxPx : fontPx, autoBasePx);
            }
            // 🔴 原版 `折行 = 0` 那一档要还原：上面 `SetWrapWidth` **无条件**把它开成 `Normal`
            //    （`TmpFont.SetWrapWidthRect` 原文）—— 必须在**定完字号之后**、且本方法没有后续重排（无裁切）。
            if (autoMinPx > 0f && wrapPx <= 0f) lb.SetWrapping(false);
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
        // 📌 `QCardArt`(2905) 现为**历史档**（模式卡图已改走 `QCardArtAboveNav` = 2912，见下）；
        //    保留它只为让下面那条「不能和整屏渐变同队列」的来历可读，⛔ 不要再往它上面挂可见件。
        const int QBg = 2900, QCardArt = 2905, QPanel = 2910, QContent = 2920, QText = 2921, QOverlay = 2930;

        /// <summary>模式卡**卡图**的实际队列 = `QPanel + 2`（**2912**）。
        /// 🔴 **2026-10-24（`D1` 诊断）**：卡图原来用 `QCardArt`(2905)，而左竖导航的投影
        /// `Navigation Panel/Panel Shadow` 在 `QPanel`(2910) 且 rect 横跨 x **−762.5..1166.2** ⇒
        /// **影子画在 col0 小冲突 / col1 排位两张卡图上面**（用户报的「淡黑色透明层」）。
        /// 判据 = 用户自己的原版实拍 `桌面/warpforge参考图/主界面参考图.png`（正确比例 0.7405）：
        ///   影子本该落在第 392..444 行，那里**没有单行台阶**；而我们渲染出的 y=582 单行跳变 **+23.3 灰阶**。
        /// 对照 = 收藏窗「选择游戏模式」页同款卡队列 **3032**，量出来 0.99~1.00 干净 ⇒ **纯队列问题**。
        /// ⛔ **只抬卡图**：投影的 rect/tint/贴图/队列**一律不动**（原版值，`MainMenuScene.cs:801/:14761` 钉着）。
        /// 2911 已被 `QNavHighlight` 占用 ⇒ 从 2912 起取（2912..2919 空）。</summary>
        const int QCardArtAboveNav = QPanel + 2;

        /// <summary>顶栏那面**头像盾牌框**单独一档（`QContent − 1`）。
        /// 🔴 `Player_Profile_Border` 那张图的**中心是不透明黑**（实测 RGBA (0,0,0,255)）⇒
        /// **立绘必须排在它之后**（队列更高），否则立绘被压成黑块。
        /// 原版兄弟序也是 `Highlight → Border → Image`（Image 最后 = 画在最上面）
        /// —— 实据 → `资料/说明书/04_界面UI/菜单全树.md:4888` 那一棵。
        /// ⚠️ 用「边框退一档」而不是「立绘进一档」：`QContent + 1` 就是 `QText`，
        /// 而 `Player Name` / `Player Level` 那两块**与头像框有重叠**（x 136.9~165.5 · 117.5~170.6）⇒ 会撞。
        /// 这一档只有那面盾用（`ProfileTab` / `RankedTab` 那边是各自一套 `L_*` 梯子，互不相干）。</summary>
        const int QAvatarFrame = QContent - 1;

        /// <summary>左竖导航的**选中态高亮**（`Selected highlight`）单独一档 = `QPanel + 1`。
        ///
        /// <para>🔴 **2026-10-11（A1521⑥）：为什么必须单开一档 —— 它原来是【同队列】，而实测没画出来。**
        /// 档位原来传的是 `QPanel`，**与导航底板 `Navigation Panel/Background` 同一个数（2910）**
        /// （底板在 `BuildNavigationPanel` 里就是 `QPanel`）⇒ 同队列 + 两件 z 都是 0
        /// ⇒ **谁盖谁完全不确定**（本文件 `QBg` 那段 2026-09-22 的实测结论；`Editor/MainMenuScene.cs`
        /// 也写着同一条：「同队列时谁盖谁由『到相机的距离』定」）。</para>
        ///
        /// <para>🔴 **实测（逐像素量的，不是目测）**：拿真机实拍 `_tmp_view/play17/S1_menu.png`
        /// 与用户参考图 `C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png` 并排量**选中那颗（PLAY）**
        /// 与**未选中四颗**的同位置条带 ——
        /// 我们：左缘条带 `(97,29,6)` vs `(95,25,8)`（**逐点几乎相同**）· 右缘条带 `(129,42,30)` vs `(119,13,3)`
        /// （后者那点差是**图标**：PLAY 的图标 156.3 宽、比其余四颗的 140 宽）；
        /// 原版：`(116,14,2)` vs `(46,9,8)`（**2.5 倍**）。
        /// 再拿高亮本该有的颜色 `40k_main_bt_selected_BW`（71²，灰 148、alpha 145–255）× tint `(1,0.0805,0,1)`
        /// ≈ `(148,44,0)` 全图扫一遍：PLAY 那颗整块里**只命中 47 个像素（0.1%）**、且都聚在图标刀身上
        /// ⇒ **高亮一个矩形都没画出来**（等价于原版那一格 `en=0`）。</para>
        ///
        /// <para>**档位判据 = 原版兄弟序**（`Navigation Panel` 的子件序：`Background`(1) → `Panel Shadow`(2)
        /// → `Separators L/R`(3,4) → `Buttons Container`(5)；而 `Selected highlight` 是**按钮的第 1 颗子件**）
        /// ⇒ 高亮必须在**底板 / 阴影 / 分隔线之上**，在按钮自己的 `Image` / `Text Background` /
        /// `Badge Highlight`（`QContent` 2920）与文字（`QText` 2921）**之下**
        /// ⇒ `QPanel + 1`（2911）是这一段里唯一的空档。</para>
        ///
        /// <para>⚠️ **工程里同一件事有先例**：`DeckRuntime` 的页签高亮（**同一张图**）也是单开一档压在侧栏底板之上
        /// —— `QPanel 3004 &gt; QSide 3000`（`Deck/DeckRuntime.cs`）。
        /// ⚠️ **诚实标注**：改这一档是照「兄弟序 + 同队列不确定」推的，**改完没有跑过 Unity、没法当场验收**
        /// （执行代理不许跑批处理）—— 请把这条记成「已改、待实拍复核」。</para></summary>
        const int QNavHighlight = QPanel + 1;
        /// <summary>🔴 **2026-10-18（第五十一会话 · 真机「商店开着就点不动左栏」）**：**导航命中区**的队列。
        /// 判据（`_tmp_view/click_log.txt` 点击 #3 实证）：这一点上候选命中是
        /// `q=3130 BackgroundHit`（当前窗口的整屏压暗命中靶）与 `q=2910 Hit/Navigation Panel/…`，
        /// **前者吃掉后者** ⇒ 打开任何一扇子窗后，左栏五颗全点不动（退出商店的唯一路径断了；
        /// 原版 `Shop Menu Variant` **没有** `Shared/Close Button` —— 全库只有收藏窗有，所以只能靠左栏）。
        /// ⇒ 导航命中区单独抬到 **3156**（现读空闲槽；**3160 是 `PlayerProfileWindow.QPageBase`**，⛔ 别用）：
        /// 高于窗口那族压暗命中（3002–3140），低于档案窗页内容（3160+）、弹窗/提示层（最低 3310）与加载黑场
        /// （`LoadingMenu.QPanel = 4090`）。🔴 **2026-10-20（W58 · 遗留 2）订正**：本行原写「加载黑场（4071）」——
        /// **4071 是 `BlockingOverlay` 降级件最上面那一格（该件现已降到 3339），不是黑场**；黑场 = 4090。
        /// ⚠️ 只抬**透明命中区**（`Hit`），可见图标/文字/高亮的队列一律不动 ⇒ 画面逐像素不变。
        /// <para>🔴 **2026-10-19（界面线 `N117` · 用户拍板「方案 C：按窗类型分」）：它现在是一个【动态档】**——
        /// 非模态窗（商店/收藏/日常/社交…）开着时仍是 `QNavHit`；**模态窗**（详情窗/确认弹窗/奖励弹窗…）
        /// 成为最上面那一扇时，这五颗命中区整体降到 <see cref="QNavHitBlocked"/>（低于全工程**最低**的窗压暗层
        /// `DailyRewardPopup.QShade = 3002`）⇒ 左栏那一片的点击落到**模态窗的压暗层**上（原版：弹窗的
        /// `Menu Dark Background` 带 `m_RaycastTarget = 1`，而它在 PopUp Holder / 排序层 `PopUps` 里永远压过左栏）。
        /// 「哪些窗算模态」的唯一判据 = <see cref="GameWindow.BlocksLeftNav"/>（基类按原版 prefab 的两个字段算）。
        /// 刷新点 = `WindowsManager.OpenWindow` / `NotifyClosed` / `CloseAllWindows` / `SetBaseMenu` 四处
        /// （见 <see cref="RefreshNavHitQueue"/>）。</para></summary>
        const int QNavHit = 3156;

        /// <summary>🔴 **2026-10-19（`N117`）**：模态窗在场时，左栏命中区降到这一档。
        /// 取 **3001** 的理由：它必须**严格低于全工程最低的窗压暗层** = `DailyRewardPopup.QShade` /
        /// `DailyStreakPopup.QShade` / `InboxWindow.QShade` 的 **3002**（这三扇是 `placement=15` 的模态窗）；
        /// 同时**不撞**任何窗口内容档（窗那一族从 3002 起）。
        /// ⚠️ **数值撞档（如实记）**：`Deck/DeckRuntime.cs` 的 `QDoneHl` / `QCurveBg` 也是 3001 ——
        /// 那是**卡组编辑那一屏**（独立场景），与主菜单左栏**不会同屏**（且卡组编辑开窗时左栏已被 `Toggle` 收起）。
        /// ⛔ 别再往上抬：抬到 3002 就与最低的压暗层**同档**（同队列谁吃命中退化成枚举顺序 —— 本项目踩过三次）。</summary>
        const int QNavHitBlocked = 3001;
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
        //    （`BuildGameModes` 里 `本文件` / `本文件` 两处），它们要压在**模式卡**之上，与弹窗无关。
        //    顶栏降到 2994–2998 之后**仍然高于**它们（2930 / 2931）⇒ 菜单内部的相对次序没变
        //    （2026-10-17 F5 把下沿放宽到 2986，高得更多，这条更宽松 —— 见下）。
        // 📌 **保留历史痕迹**（铁律 5 —— 更正痕迹不许抹掉）：2026-09-28 那一轮**是照一张二手实拍**做的
        //    （那张实拍里顶栏看得见、选卡组弹窗那两个页签条 y 35.07~107.25 **看不见**），而**当时就写在本段里的
        //    那条「反证」**——「原版 `Safe area Only Horizontal` 的 `m_Children` 里 `3 - PopUp Holder`（RT1121）
        //    排在 `Upper bar`（RT1092）之后 ⇒ 单看兄弟序，弹窗本该在顶栏之上」（出处 →
        //    `资料/主菜单_原版规格.md:62-64`）—— **一直是对的**。当年是「实况优先于解包字段」（铁律 4）
        //    压过了它，⛔ 而那张实拍**不是实况取证**；2026-10-11 用户裁定「照原版」⇒ **兄弟序 + 排序层这一侧胜出**。
        // ============================================================ 🆕 2026-10-17（F5）：下沿放宽 + 顶栏内部再分 8 档
        // 🔴 **触发**：`DeckScene.Run` 那条通用检查（「同一层的 UI 图没有互相压住」，
        //    即 `Editor/DeckScene.cs` 里 `CheckTrue(overlaps == 0, "同一层的 UI 图没有互相压住…")` 那条）实测 **8 处**，而 **8 对全在顶栏内部**。
        //    **根因**：顶栏有 **6 张两两相交**的图（A 条底 · B 玩家框 · C 名字条 · E 盾框 · F 立绘 · G 等级图标），
        //    而渲染队列是 `int`、旧带子 `[2994,2998]` **只有 5 个整数档** ⇒ **鸽笼原理，装不下**。
        //    ⛔ 三条绕法都排除过：把顶栏排除出检查（= 把主菜单/外壳同样那 8 处一起放掉）·
        //    小数队列（`SetRenderQueue(int)`）· `≤0`（那是 `Editor/MainMenuScene.cs` 里那句 `if (q.RenderQueue <= 0) continue;` 的**跳过口**）。
        // 🔴 **改法（一行修好主菜单 + 外壳两处 —— 那 8 处重叠在那边同样存在、只是没人查）**：
        //    ① **下沿放宽**：`QBarPanel 2994 → 2986` ⇒ 带子 = **`[2986, 2998]`**；
        //    ② **顶栏内部按原版兄弟序分 8 档**（2986–2993）—— 完整表 + 逐档归类 →
        //       `Shell/TopBar.cs` 的队列常量上方（⛔ 别在第二个地方再抄一份）。
        //    ⚠️ **为什么下沿可以放到 2986**：`[2986,2993]` **两边都空着** —— 主菜单自己的内容最高到
        //    `QOverlay = 2930`（本文件那条队列梯子 —— `QBg`/`QCardArt`/`QPanel`/`QContent`/`QText`/`QOverlay` 那一行常量）、卡组编辑那屏最低是 `DeckRuntime.QAreaBg = 2980`
        //    （声明在 `Deck/DeckRuntime.cs` 的 `QAreaBg`）。**上沿 `QBarOverlay 2998` 不动** ⇒ 两条不变式照旧：
        //    `QBarOverlay(2998) < DailyStreakPopup.QShade(3002) < MainMenuSubmenuWindow.QPanel(3005)`、
        //    以及 `QBarOverlay(2998) < DeckRuntime.QSep/QLowest(2999)`（`Editor/DeckScene.cs` 里
        //    `CheckTrue(MainMenuRuntime.QBarOverlay < DeckRuntime.QLowest, …)` 那条）。
        //    ✅ 主菜单那条「顶栏件必须落在 `[QBarPanel, QBarOverlay]`」（`Editor/MainMenuScene.cs` 里
        //    `inBarBand` / `wrong` 那两条断言）
        //    用的是**这两个常量本身** ⇒ **自动跟着放宽，一个字没改**。
        // 🔴 **2026-10-17（F5）就地订正 **F2 那张表里的两档次序**（铁律 5）**：表里原写
        //    「2990 盾框（E）· 2991 名字条（C）」—— 与它**自己引的**原版兄弟序**正好相反**。
        //    现读（`python 工具/menu_dump.py bundle_scenes_scenes_mainmenuwarpforge "Player Profile" --md --depth 5`）：
        //    `Player Profile` 子件序 = `Background` → `Background Mask`（**名字条 `Planer Name Background`
        //    挂在它的下面**）→ `Player Name` → `Profile border`（⚠️ 出厂 **inactive**、`Image` 组件也被禁
        //    ⇒ 原版画的不是这一颗）→ **`Avatar Item Small`**（活的那面盾 = 它的 `Image Container/Border`）
        //    → `Player Level` ⇒ **盾在名字条【之后】= 画在它上面**。用户实拍的同一处吻合
        //    （`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`：盾完整，那块暗紫名字板**从盾的右边**
        //    才露出来、没有横穿盾面）⇒ 现取 **2990 名字条（C）· 2991 盾框（E）**。
        // 🔴 **2026-10-11 就地订正（铁律 5 · `A1521②` · 代理 `P` 四条独立判据）**：上面那句「参考图上那块暗紫名字板」
        //   **记错了** —— 现在主对话拿 `卡组编辑界面参考.png` 放大重看过：**那一处是灰白花边条**（`40k_main_player frame`），
        //   **没有暗紫板**。原版那块板（`Planer Name Background`）的父件 `Background Mask` **出厂 `m_IsActive = 0`** ⇒
        //   **原版一个像素都不画它**（四条判据 → `Shell/TopBar.cs` 的 `BuildPlayerProfile` 那段，⛔ 不在这里抄第二份）。
        //   ⇒ **我们不建它**（同日已删）⇒ 上面那对 2990/2991 的**次序**从此只是**冗余的常量**，留着无害、⛔ 别再往上挂件。
        public const int QBarPanel = 2986, QBarContent = 2987, QBarAvatarFrame = 2991,
                         QBarText = 2997, QBarOverlay = 2998;

        // ============================================================ 建

        /// <summary>建整个主菜单。**自检与运行时调同一个**。</summary>
        public void Build()
        {
            _root = transform; BadgeWiring.RegisterDefaults();   // 🔴 2026-10-26（B8_BN）：**红点 provider 的登记点补在这一条生产入口上**（零新增行）。原来只有 `Shell/ShellRuntime.cs:768`（壳）与 `Editor/RewardsScene.cs:3539`（自检）各调一次 ⇒ **主菜单这条独立建场路一个 provider 都没有**：`TopBar` / 左栏那几颗红点挂在 `UiBadgeNotification` 上、`Recount()` 读的是 `UiBadgeNotificationManager` 的空表 ⇒ `CampaignBadge`/`ForgeBadge` **恒 0**（`Editor/MainMenuScene.cs` 那三条红：Campaign 期望 1、锻造期望 3、以及「Notifications 里有 CampaignBadge 那一栏」）。`RegisterDefaults()` 自带 `ProviderCount > 0` 幂等闸 ⇒ 与壳那条路并存无副作用（⛔ 别改成「先清空再登记」：provider 表是全局的、清掉会让 `[PlayerBoot]` 之外的宿主丢档）。

            // 🆕 **2026-10-17（界面线 T16 / `N4`）**：`Safe area All` 归**壳**所有（`ShellRuntime` 建的那一颗），
            //    而原版里它是 `MainMenuWindow` 的孩子（`RT1086.m_Father = 1087`）⇒ 上一轮构建把它搬进了**本场景**这一棵。
            //    ⚠️ 不先收回去，下面那轮清场会连它一起销毁；Play 下 `Destroy` **延迟到帧末**（当场还非 null）⇒
            //    新挂上去的模式卡会跟着它一起消失（**静默**）。收回去 = 本轮重新挂（见下面「安全区」那一节）。
            var shell = ShellRuntime.Instance;
            // 🆕 **2026-10-21（W59）**：自检/批处理里 `Awake` 不跑 ⇒ `ShellRuntime.Instance` 恒 null（同族坑 →
            //   `GameWindow.SetBake` 那段注释），可「场上有一台壳」这一档**必须走有壳那一支** —— 外壳常驻件挂在
            //   壳的 `safeH` 下（缺陷 B / 「回主菜单不许累积旧件」都只在这一支里）。⚠️ 真·独立场景档
            //   （`MainMenu.unity` 直起 / 空场景自检）场上**没有壳** ⇒ 这一句仍是 null，行为逐位不变。
            if (shell == null) shell = UnityEngine.Object.FindFirstObjectByType<ShellRuntime>();
            bool rebuilding = _root.childCount > 0;          // 同一棵树上重来一次（上一轮的件还在）
            if (shell != null)
            {
                shell.ReclaimSafeZones();
                // 🆕 **2026-10-21（W59 · 缺陷 B 的恢复半边）**：进战斗时 `ShellRuntime` 把左栏 / 顶栏 / 聊天
                //    这三件关掉了（原版战斗场景里没有它们，判据 → `ShellRuntime.ApplyMenuChrome` 那段）。
                //    ⇒ **回主菜单、在重建菜单树之前先放回**。⛔ 别删：删了就是「进过一次战斗，左栏再也不回来」。
                //    ⚠️ 位置在 `ReclaimSafeZones()` **之后**（先把安全区收回壳根，再动它下面的孩子）；
                //      也必须在下面那轮重建**之前**（`BuildNavigationPanel` 会再建一套新的，旧的那一套放不放回都不影响画面，
                //      但状态必须是对的 —— 「关着」留到下次进战斗就没人看得见那是谁关的）。
                ShellRuntime.ApplyMenuChrome(true, "回主菜单");
                // 🆕 **2026-10-21（W59 · 顺手发现 ①）**：把**上一轮**留在壳 `Safe area Only Horizontal` 下的常驻件清掉。
                //   为什么必须清：下面 `BuildNavigationPanel` / `TopBar.Build` / `BuildChatPreview` 全是**无条件 `New`**，
                //   而上面那轮 `_root` 清场**只清主菜单场景自己的孩子** —— 这三件挂在**壳**的 `safeH` 下
                //   （壳 DDOL、跨场景活着）⇒ 不清就会「菜单 ↔ 战斗每往返一次 +1 套」（视觉完全重叠看不出来，
                //   但旧件上的 `WindowButton` 闭包指的是**已销毁的** `MainMenuRuntime`，而且是纯泄漏）。
                //   🧨 改坏法：删掉这三句 ⇒ `Editor/ShellScene.cs` 的「W59（续）」那一节红（第二次 `Build` 后每类变 2 个）。
                //   ⚠️ 只清 `ShellRuntime.MenuChromeNames` 里那三个名字（`LoadingMenu` / `Window Anchors` 等⛔ 别扫）。
                int staleChrome = ShellRuntime.ClearStaleMenuChrome(shell.SafeAreaOnlyHorizontal);
                if (staleChrome > 0)
                    Debug.Log($"[Menu] 清掉上一轮遗留的壳常驻件 {staleChrome} 件"
                            + "（左栏 / 顶栏 / 聊天预览 —— 它们挂在壳的 `Safe area Only Horizontal` 下、不随主菜单场景卸载；"
                            + "不清就每回一次主菜单多一套）");
                // ⚠️ 上一轮建在 `Safe area All` 下的模式卡**不跟着 `_root` 那轮清场**（它们随安全区节点被搬进了壳根）
                //    ⇒ 这里补一刀；否则重建后会**多出一整套**模式卡（静默）。
                var oldSafeA = shell.SafeAreaAll;
                if (rebuilding && oldSafeA != null)
                    for (int i = oldSafeA.childCount - 1; i >= 0; i--) DestroySafe(oldSafeA.GetChild(i).gameObject);
            }

            for (int i = _root.childCount - 1; i >= 0; i--) DestroySafe(_root.GetChild(i).gameObject);
            MissingArt.Clear();

            // 🔴 2026-10-11 修（见账 `A1494`）：**主菜单场景自己的相机要重新 `Apply` 一次**。
            //   为什么必须：`LayoutSpace.Cam` 是**静态字段**，而壳那台 `Main Camera` 是**场景根件**
            //   （**不在** `Shell` 的 `DontDestroyOnLoad` 层级里 —— `ShellRuntime.Awake` 只 DDOL 了 `Shell` 自己）
            //   ⇒ 切到本场景（**单场景加载**）时它被销毁，`Cam` 指向一台**已销毁**的相机
            //   ⇒ 屏幕→世界换算**全部退化成世界原点** ⇒ `PixelOfDesign` 恒等于画布正中 **(960, 540)**
            //   ⇒ **命中永远落在屏幕正中那一件上**（实测：四个完全不同的位置点下去，命中都是中间那张模式卡）。
            //   同族先例：进战场时 `BattleDriver` 也这么做；这条坑本来就记在 `资料/已知的坑.md`
            //   （「`LayoutSpace.Cam` 是静态字段 … 场景里要有个 `Start()` 重新 `Apply(cam)`」）。
            LayoutSpace.Apply(Camera.main);

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

            // 🔴 **2026-10-12（`A1629`）：原版 `MainMenuWindow` 那一棵 = 【背景 + 模式卡】** —— 判据（逐级解 `m_Father` 实读）
            //   → `Shell/MainMenuWindow.cs` 的文件头 ③：`Main  Canvas` → `MainMenu`(pid 38) →
            //   **`MainMenu`(pid 7 = 那一扇)** ├ `Background` └ `Safe area All/GameModes/…`（模式卡）；
            //   而 `Upper bar`(顶栏) · `Navigation Panel`(左导航) · `ChatPreview` 是**兄弟**、不跟着它被藏。
            //   ⇒ 我们照这条把「背景 + 模式卡」建进这一颗子节点，**顶栏/左栏/聊天面板不跟着它被藏**。
            //   🆕 **2026-10-17（T16 / `N4`）就地订正**：本段原写「顶栏/左栏/聊天面板仍旧挂在 `_root` 下」——
            //      现在它们挂在 `Safe area Only Horizontal` 下（原版树如此；见下一节），**仍然不是这一扇的孩子**。
            //   ⚠️ 多插的这一层**位置恒为零**（`New` = `SetParent(parent,false)` + 只写尺寸）——
            //      本文件那条不变式（所有件把绝对世界坐标当 `localPosition` 用）因此逐位不变。
            var menuWin = New(_root, "MainMenu", LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);

            // ============================================================ 🆕 2026-10-17（T16 / `N4`）安全区接线
            //
            // 原版树（`bundle_scenes_scenes_mainmenuwarpforge`，逐级解 `m_Father` **实读**；索引 → `主菜单_原版规格.md:53,64-68`）：
            //   `MainMenu`(pid 38) ├ `MainMenu`(pid 7 = 主菜单那一扇) ├ `Background`(1089)
            //                                                    └ **`Safe area All`(RT1086) → `GameModes`(1122) → `Viewport`(1093) → `Content`(1107)**
            //                     └ **`Safe area Only Horizontal`(RT1540)** → `Navigation Panel`(1103) · `ChatPreview`(1169) ·
            //                       `1 - Below Upper Bar Holder`(1095) · `Upper bar`(1092) · `Borders`(1115) ·
            //                       `2 - Canvas Holder Above upper bar`(1109) · `3 - PopUp Holder`(1121) · `LoadingMenu`(1098) ·
            //                       `UI Error Message Controller`(1206) · `TooltipManager`(1557) ·
            //                       `Toast Notification Controller`(1096) · `Loading Controller`(1083)
            // ⇒ **顶栏 / 左栏 / 聊天 / Toast 挂在 `Safe area Only Horizontal` 下；模式卡在 `Safe area All` 下**。
            //
            // 我们这边分两档（**同一套结构**，⛔ 不是两份实现）：
            //   ① **运行期有壳**（正常进主菜单）：用壳那两个常驻节点 —— 三 Holder 已经在
            //      `Safe area Only Horizontal` 下（`ShellRuntime.Build` 干的），本函数把
            //      `Safe area All` **搬进这一扇**（原版就是它的孩子 ⇒ 跟着 `MainMenuWindow.ToBackground` 一起隐藏）。
            //   ② **独立场景**（单开 `MainMenu.unity` / 自检，没有壳）：就地建两个同名节点 + 一棵本地 `UISafeArea`，
            //      层级与 ① 逐级一致（`Safe area All` 在 `MainMenu` 那一扇下、`Safe area Only Horizontal` 在根下）。
            // ⚠️ 判据在**原版树**里（上面逐个 pid 点过名）；U1 那句「包住模式卡」只是索引，不是唯一来源。
            Transform safeH, safeA;
            if (shell != null)
            {
                safeH = shell.SafeAreaOnlyHorizontal;
                safeA = shell.SafeAreaAll;
                if (safeA != null) safeA.SetParent(menuWin, false);
                // 存盘的 `MainMenu.unity` 里烘着一棵「独立场景档」的 `UISafeArea`（它的两个 zone 刚随上面清场没了）
                // ⇒ 这一档用**壳**那一份，⛔ 别留两个经理各写一遍（写的是同一组值，但会让人以为有两套口径）。
                var localMgr = _root.GetComponent<UISafeArea>();
                if (localMgr != null)
                {
                    Debug.Log("[Menu] 本场景里烘着一棵本地 `UISafeArea`（独立场景档）—— 有壳的这条路上用**壳**那一份 ⇒ 移除本地这一棵");
#if UNITY_EDITOR
                    if (!Application.isPlaying) DestroyImmediate(localMgr); else
#endif
                    Destroy(localMgr);
                }
            }
            else
            {
                safeH = New(_root, "Safe area Only Horizontal", LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
                safeA = New(menuWin, "Safe area All", LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
                var mgr = _root.GetComponent<UISafeArea>();
                if (mgr == null) mgr = _root.gameObject.AddComponent<UISafeArea>();
                mgr.zones = new[]
                {
                    new UISafeArea.Zone { rectTransform = safeA, applyWidth = true, applyHeight = true  },
                    new UISafeArea.Zone { rectTransform = safeH, applyWidth = true, applyHeight = false },
                };
            }
            if (safeH == null || safeA == null)
            {
                // 出声（红线：不许静默失败）—— 「安全区没接上」必须能从日志里分辨
                Debug.LogWarning("[Menu] 安全区节点取不到（`Safe area All` / `Safe area Only Horizontal`）⇒ "
                               + "本轮顶栏/左栏/聊天/模式卡**照样建**，但父链退回菜单根（安全区不生效）");
                safeH = safeH != null ? safeH : _root;
                safeA = safeA != null ? safeA : menuWin;
            }

            BuildBackground(menuWin);
            HookMenuRebuildBroadcasts(); BuildNavigationPanel(safeH);   // 🆕 B89-E30（账 `L412`/`L477`）：先挂两条重建跳（摘订在 `OnDestroy`，见类尾）
            // 🔴 **2026-10-17（B25）：顶栏改成调共用件 `Shell/TopBar.cs`** —— 本文件原来那份
            //    `BuildUpperBar / BuildPlayerProfile / BuildResourcesBar / BuildTopAvatar` **已删**。
            //    为什么：卡组编辑（独立场景 `DeckEditor.unity`）要**同一条顶栏**，D13/D14 那一轮把本文件
            //    这四个方法**逐参抄**了一份给 `TopBar` ⇒ **两份并存**，而两处写同一条规则 = 迟早不一致
            //    （实测已经不一致两处：资源计数器**次序反了** + 信箱红点**恒不亮**，判据 → `TopBar.cs` 文件头）。
            //    ⇒ 现在**几何/结构/接线只有 `TopBar` 一处**；本文件只剩**数据与入口**
            //    （`CounterSpecs` 那几张表 · `OpenInbox/OpenSettings/OpenProfile` · `QBar*` · 红点色）。
            //    ⛔ 别把参数再写回本文件（那等于把两份漂移重新种回来）。
            _topBar = TopBar.Build(safeH, NoteMissingArt);
            BuildChatPreview(safeH);
            // 🆕 **2026-10-17（`A1141`）**：原版 `Toast Notification Controller → Holder`
            //   （任务 / 等级 / 经验飘窗那一族；**与 `MessageToast` 是两套东西**，⛔ 别混）。
            //   判据 = `资料/主菜单_原版规格.md:64-74,237-238` ＋ `ToastNotificationController__*.c`
            //   ＋ 场景 `bundle_scenes_scenes_mainmenuwarpforge` 的 `RT 1096/1100`。建在
            //   `Safe area Only Horizontal` 下（原版兄弟序：`TooltipManager` 之后、`Loading Controller` 之前）。
            //   ⚠️ 不插也能工作（`ShowNotification` 会 `Ensure()` 自建）—— 插上是为了「菜单建场时节点就在」。
            // ⚠️ `ToastNotifications.Build` 自己会去 `root` 的**直接子件**里找 `Safe area Only Horizontal`
            //    （原版 `RT1096.m_Father = 1540`）⇒ 传 `safeH` 的父件（独立场景 = 菜单根；运行期 = 壳根），
            //    ⛔ 别传 `safeH` 本身（那会找不到、退回并刷一条「没有安全区」的假告警）。
            ToastNotifications.Build(safeH.parent != null ? safeH.parent : _root);
            BuildGameModes(safeA);

            // 🆕 **2026-10-12（`A1629`）**：把这一棵登记成**常驻主菜单**（原版 `MainMenuWindow.Initialize()` 末尾
            //   那两句 `SetBaseMenu(this)` + `OpenWindow(this)`）—— 判据全文 → `Shell/MainMenuWindow.cs` 的文件头。
            //   🔴 **放在 `BuildGameModes` 之后、`MissingArt` 汇总之前**：那一跳会 `OpenWindow`（= 提前台 + `Open()`），
            //   而它的 `Open()` 是空的（内容由本函数建）⇒ 必须在**树建完之后**再登记，否则「开出来的是一棵空树」。
            RegisterMainMenuWindow(menuWin);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Menu] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");

            // ============================================================ 🆕 2026-10-18（A855）「编辑完卡组离场」的回程
            //
            // 🔴🔴 **2026-10-14（`A1132` 第 4 步）本段【降级】（铁律 5：就地改口径，⛔ 不留第二份）**：
            //    **本段现在只管【独立场景】那一条腿**（`DeckRuntime.HostWindow == null` =
            //    直起 `-wfscene DeckEditor` / 批处理里裸建 `DeckRuntime`）—— 只有那条腿的 `BackToMenu()`
            //    才是「切 `MainMenu` 场景」，回程才只能由**到达端**（= 本函数）做。
            //    🔴 **被窗托管那条腿（第 3 步起那三处生产入口）【不再经过本段】**：那一边
            //    `BackToMenu()` 走 `DeckRuntime.LeaveByHostWindow()` —— **关窗、⛔ 不切场景**，
            //    意图与库缓存**都在关窗那一拍就地消费**（含「只 `ChangeTab`、⛔ 不再 `OpenCollection()`」
            //    的理由 → 那个方法的 doc）。
            //    ⇒ **两条腿各消费一次**：被托管那条先把意图 `TakePendingReturn()` 掉，
            //    本段即便将来又被跑到，拿到的也是 `None`（`Take*` = **先取后清**）⇒ **不会重复开窗**。
            //    ⛔ **别把本段删掉**：直起那条路仍然全靠它。
            //
            // 🔴 **为什么落在本函数**：`DeckRuntime.BackToMenu()` 只切场景（`LoadScene("MainMenu")`），
            //    「回来之后开哪扇窗、落哪一页」必须由**到达端**做 —— 那正是本函数。
            //    ⛔ **放 `Start()` 不行**：本仓规矩「按 Play 的入口和自检**走同一条路**」，而 `AddComponent` 不触发 `Start()`。
            // 🔴 **按来路回**（用户 2026-10-18 拍板）：来源由**入口**写
            //    （🔴 **2026-10-12（`A1697`）就地订正（铁律 5）**：本行原来写「全仓**仅有**的两处」——
            //     **现在是三处**：`CollectionWindow.GoEdit` · 同文件 `GoNewDeck` · `LiveOpsEventWindow.CreateDeckInMode`），
            //    本函数**只消费**：
            //      · `Collection` ⇒ **开收藏窗 + 落到卡组页**（下面那一段）；
            //      · `LiveOpsEvent` ⇒ **本次复刻不了**，三条理由（铁律 11 要求「不做」必须写清为什么）：
            //        ① 原版是**同窗换页**（`SelectDecksTab.CreateDeck` 紧接着打开 `DeckEditingWindow`），
            //           而我们的来路是**独立场景** ⇒ 想「回来时重新打开那扇事件窗」得先有**事件数据**；
            //        ② 那数据随 **PlayFab 下发**，本地只有**兜底版本**（`LiveOpsEventListener` + 本地 `Resources`）
            //           ⇒ 关服状态下无从复刻；
            //        ③ 这正是铁律 11 允许「不做」的**第 ② 类情形**（**判据是空的 / 本地没有**），
            //           ⛔ **不是**「影响小、不做」那一类措辞。
            //        ⇒ 意图照记（将来事件数据能复刻时，只需**在这里补那一跳**），**今天那一跳不写** —— 这笔账还开着。
            //      · `None` ⇒ **什么都不做**（⛔ 「无条件开收藏窗」= 把「按来路回」做成假的）。
            var ret = CollectionData.TakePendingReturn();
            // 🔴 **2026-10-18（A855）本波补上缺的那一环：作废那份进程级库缓存。**
            //    **没有它，下面开出来的收藏页画的还是【编辑前】那一份** —— 真根因与判据逐条写在
            //    `CollectionData.InvalidateLibrary` 的注释里（缓存 = `static DeckLibrary _lib`，
            //    而编辑器写盘走的是 `DeckLibrary.Load()` 另开的实例；
            //    列表 = `CollectionWindow.cs` 里 `MenuDraw.DeckCell(…, CollectionData.DeckAt(i), …)` 那一处
            //    → `CollectionData.DeckAt` → `Lib.Decks[i]`）。
            //    🔴 **两个来源都作废**（`Collection` 与 `LiveOpsEvent`）—— **两条来路都能改卡组、都写盘**：
            //    事件窗那条今天只是「**不开窗**」（理由见下），但它下次被玩家打开时读的是同一份缓存
            //    ⇒ 漏掉它 = 从事件窗编辑完、之后进收藏页仍看到旧的那一版（静默）。
            //    ⛔ **无条件调是不行的**：正常进主菜单（`Source == None`）也会白读一次盘 ——
            //    判据就是「这一趟有没有回程意图」（`None` = 没有）。
            if (ret.Source != DeckExitSource.None) CollectionData.InvalidateLibrary();
            if (ret.Source == DeckExitSource.Collection)
            {
                // ⚠️ 落页走**显式**的 `ChangeTab`，⛔ **不靠** `CollectionWindow.Open()` 里那句 `Click(0)`
                //    那个「第 0 页恰好是卡组页（`visualTypes[0] = CollectionDecks`）」的**巧合** ——
                //    页序一改，它就会静默落错页（这正是本件要一个显式出处的原因）。
                var tab = ret.Tab;
                if (tab == WindowTabType.None)
                {
                    tab = WindowTabType.CollectionDecks;      // 兜底，但**出声**（不许静默失败）
                    Debug.LogWarning("[Menu] 回程意图只写了来源 `Collection`、没写页签 ⇒ 兜回卡组页"
                                     + "（写意图那一处该补 `WindowTabType.CollectionDecks`）");
                }
                OpenCollection().ChangeTab(tab);
            }

            // ============================================================ 🆕 2026-10-18（E01 · 新手引导线）
            // **菜单侧 FTUE（原版 `TutorialManager` 的 `FlowTutorialStage` 链）** 的挂载点。
            // 判据（第一权威 = 反编译）：`TutorialManager.Initialize` 在主菜单那一层初始化之后
            //   `PlayTutorial()` → `DelayedPLayTutorial` 找**第一个未完成且未满足**的 stage → `Execute()`。
            // ⇒ 我们这一跳放在**主菜单整棵树建完之后**（= 原版「主菜单初始化完成」那一刻）。
            // 🔴 **只在运行时挂**（`Attach` 内部判 `Application.isPlaying`）—— 自检是编辑器非播放跑同一份
            //   `Build()`，多挂一个带指针/聚光灯的节点会污染既有断言，而且会写 `PlayerPrefs`。
            // ⚠️ 指针对准的是**控件节点**（`Navigation Panel/…` / 奖励窗左栏页签 / 模式卡），⛔ 不是坐标 ——
            //   目标窗会动（阶段表 `MenuTutorialStageDef` 的 `Target/TargetInWindow` 就是这两个口）。
            MenuTutorialFlow.Attach(this);
            // 🆕 **2026-10-19（E01 收尾 · 第二套系统）**：原版 `MainTutorialManager`（`MainTutorialState` + 13 键指针）
            //   的等价实现 —— 与上面那条链**并存**、各跑各的；同样只由 `-wfftue` / `AutoStart` 启动。
            //   判据 → `Shell/MenuTutorialManager.cs` 文件头（逐方法对照 + 本地等价物清单）。
            MenuTutorialManager.Attach(this);
            // 🆕 **2026-10-26（B9 · P9 #430）**：≡ 原版 `MainMenuMission.Initialize()` 末尾那一跳
            //   （`MainMenuMission__Initialize.c:11-12` 的尾调 `TrySchedulePopup()`，Slot 74）——
            //   **进主菜单时若有可领的奖励/连登 ⇒ 自动弹那一扇窗**（原版是排进 `MenuActions`，
            //   且只在当前窗恰好是 `MainMenuWindow` 时开火 ⇒ 这里也一样）。
            //   ⚠️ 本行**同时**承担 `MainMenuMission.Dispose()` 那一跳（撤上一轮的句柄）——
            //   见 `DailyData.TrySchedulePopup()` 的第一句，理由写在那里。
            //   ⏱ 位置 = **整棵树建完之后**（原版「主菜单初始化完成」那一刻；与上面两条教程链同一拍）。
            DailyData.TrySchedulePopup();
        }

        /// <summary>🆕 **2026-10-12（`A1629`）**：**常驻主菜单那一扇**（= 原版 `MainMenuWindow`）。
        /// 本文件建它、`Shell/MainMenuWindow.cs` 定义它；`Build()` 每次重建时**换一扇新的**（旧的那一棵
        /// 随 `Build()` 开头的清场一起没了）。自检按它读 `activeSelf`。</summary>
        public static MainMenuWindow Window { get; private set; }

        /// <summary>建好了但**还没等到 `WindowsManager`** 的那一扇 —— 见 <see cref="RegisterMainMenuWindow"/>。</summary>
        static MainMenuWindow _pendingBaseMenu;
        /// <summary>那一扇所属的**本棵树根**（`ApplyBaseMenu` 是静态的，判「是不是同一棵树」要用它）。</summary>
        static Transform _pendingRoot;

        /// <summary>🆕 **2026-10-12（`A1629`）**：把「背景 + 模式卡」那一棵登记成**常驻主菜单**。
        /// <para>逐句对位原版 `MainMenuWindow.Initialize()` 末尾那两句
        /// （`d:/2/tools/decomp_full/MainMenuWindow__Initialize.c`：`SetBaseMenu(this)` → `OpenWindow(this)`）。</para>
        /// <para>三个字段**照那一颗 MB 的实读值**填（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/
        /// MonoBehaviour_1927.json`，`m_Script.m_PathID = 6585963289051879589` = `MainMenuWindow`）：
        /// `type = 0`(Fullscreen) · `windowsPlacement = 0`(**None** = 不挂 Holder) · `closeOnESC = 0`。
        /// ⚠️ 三个都**必须显式赋** —— 本仓那两枚哨兵（`UnsetType` / `UnsetPlacement`）会**出声报错**
        /// （`AttachToAnchor` 的两层体检），漏一个就在日志里多一条假缺陷。</para>
        /// <para>⚠️ `AttachToAnchor` 在 `None` 上是**早退**（`GetWindowAnchor(None)` 返回 null、不出声）——
        /// 这一扇**留在主菜单场景自己那棵树里**，这正是原版的样子。</para>
        /// <para>🔴 **为什么登记要分期（这一档是【我们挑的】，如实标注铁律 3）**：原版那一刻
        /// `WindowsManager.Instance` 一定在（那台是常驻件）。我们这边分两种情况：
        /// ① **运行期**（真 Play / player）：实例可能已被切场景拆掉（`A1494③` 那条拆除循环）⇒ 走
        ///    `WindowsManager.Get()`（取不到就现建一台，=`EnsureHost`，主菜单场景本来也该有它）；
        /// ② **编辑期**（`BuildAndSaveScene` 建场 / 自检）：**⛔ 不能调 `Get()`** —— 那会把
        ///    `WindowsManager` / `Window Anchors` / `PointerLayer` 三个物体**建进当前场景、随后被存进
        ///    `MainMenu.unity`**（今天那个场景里这三样**逐个数出来都是 **0 个**：`grep -c WindowHolder
        ///    Assets/CardPresentation/Scenes/MainMenu.unity` = 0、`WindowsManager` = 0、`Window Anchors` = 0；
        ///    ⚠️ 与 `项目任务.md` 里 `A1494④` 那句「`MainMenu.unity` 里带着 3 个 `WindowHolder` 且
        ///    `placement = None`」**不符** —— 那三条报错的真身是**我们自己在运行时 `MakeHolder` 里
        ///    `AddComponent` 触发 `OnEnable`、那时 `placement` 还是默认 `None`**（→ `EnsureHost` 里
        ///    「现建了一台 + 三个锚点」那句**排在三条报错之后**，顺序可证）；已记进报告，⛔ 别照那句去改场景）。
        ///    ⇒ 编辑期先挂进 `_pendingBaseMenu`，等 `WindowsManager.EnsureHost` 真建出那台时由
        ///    <see cref="OnWindowsManagerReady"/> 回来认领（自检里第一次 `Get()` 就是这个时刻）。</para></summary>
        void RegisterMainMenuWindow(Transform menuWin)
        {
            if (menuWin == null) return;
            var win = menuWin.gameObject.GetComponent<MainMenuWindow>();
            if (win == null) win = menuWin.gameObject.AddComponent<MainMenuWindow>();
            win.placement = WindowsPlacement.None;     // 原版实读 = 0（不挂 Holder）
            win.type = WindowType.Fullscreen;          // 原版实读 = 0
            win.closeOnEsc = false;                    // 原版实读 = 0（ESC 走覆写：叫出「退出游戏」弹窗）
            WindowsManager.AttachToAnchor(win);

            Window = win;
            _pendingBaseMenu = win;
            _pendingRoot = _root;
            var wm = WindowsManager.Instance;          // ⛔ 别在这儿用 `Get()`（理由见上面 ②）
            if (wm == null && Application.isPlaying) wm = WindowsManager.Get();
            if (wm != null) ApplyBaseMenu(wm);
        }

        /// <summary>`WindowsManager.EnsureHost` 把那台建出来之后回来认领 —— 见
        /// <see cref="RegisterMainMenuWindow"/> 的分期理由（**没有待认领的就什么都不做**）。</summary>
        internal static void OnWindowsManagerReady(WindowsManager wm)
        {
            if (wm != null && _pendingBaseMenu != null) ApplyBaseMenu(wm);
        }

        /// <summary>真正那一跳（原版 `SetBaseMenu(this)` + `OpenWindow(this)`，**顺序不能反**）。
        /// ⚠️ **幂等闸**（我们加的）：运行期这条链会**连着走两次** —— `RegisterMainMenuWindow` 里
        /// `Get()` 已经触发过 `EnsureHost` 的认领，`Get()` 返回之后那一句又会认领一次。
        /// 没有这道闸的话第二次 `SetBaseMenu` 会把刚加进去的那一条**清表**掉再重加（可观测行为相同、
        /// 但日志会打两遍、而且中间有一瞬 `openWindows` 是空的）。</summary>
        static void ApplyBaseMenu(WindowsManager wm)
        {
            if (wm == null || _pendingBaseMenu == null) return;
            if (wm.BaseMenu == _pendingBaseMenu && wm.openWindows.Contains(_pendingBaseMenu)) return;   // 已认领
            // 🔴 **「一山不容二虎」闸**（我们加的，如实标注铁律 3）：原版那一刻场上有**且只有**一颗
            //   `MainMenuWindow`。本仓自检里有几处夹具会**另起一棵树**再 `Build()` 一次
            //   （`Editor/MainMenuScene.cs` 的 A855 那三条走的就是 `new GameObject` + `AddComponent<MainMenuRuntime>()`
            //   + `Build()`）—— 那些树**不是**当前那一棵，不该把场上的 `baseMenu` 抢过去。
            //   判据 = 「已经在册的那一扇**是不是本棵树的后代**」：是 ⇒ 那是同一棵树的重建（`Build()` 先把旧孩子
            //   销毁，那一刻旧的还没被真正销毁、字段也还指着它）⇒ 照旧登记；不是 ⇒ 夹具/别的树 ⇒ 让位。
            var existing = wm.BaseMenu;
            if (existing != null && _pendingRoot != null && !existing.transform.IsChildOf(_pendingRoot))
            {
                Debug.Log("[Menu] 场上已经有另一棵树的常驻主菜单（`" + existing.name + "`）⇒ 本棵**不抢** `baseMenu`"
                          + "（自检夹具/别的树就是这一档；原版那一刻只有一颗 `MainMenuWindow`）");
                return;
            }
            wm.SetBaseMenu(_pendingBaseMenu);
            wm.OpenWindow(_pendingBaseMenu);
            Debug.Log("[Menu] 常驻主菜单已登记：`baseMenu` = `" + _pendingBaseMenu.name + "`（`type=Fullscreen` · "
                      + "`placement=None` · 内容 = 背景 + 模式卡 —— 原版 `MainMenuWindow.Initialize` 那两句）");
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
        //   原版那个组件是 Asset Store 的 `UIGradient`。🔴 **2026-10-25 就地订正（铁律 5 · 代理 D 现核）**：原来这里写「原版方向是 (sin,cos)、我们助手是 (cos,sin) ⇒ 差 90°」——**「差 90°」这个说法不准确**。实际：① `RotationDir(θ)` 返回的是 **`(cosθ, sinθ)`**（`UIGradientUtils__RotationDir.c:9-11`）；② **有效渐变轴**才是 **`(sinθ, cosθ)`**（`UIGradient__ModifyMesh.c:148-149` + `LocalPositionMatrix.c:20-27` 用它做投影）；③ 我们 `CardArt.Gradient` 的最大变暗方向是 `(cos φ, sin φ)` ⇒ 要还原同一张图，换算是 **φ = 90° − θ + 180°（镜像 + 反色序；180° 来自 `c1` 所在端与原版亮端相反）**。⚠️ **数值 188 是对的**（90 − 82 + 180 = 188）—— 这次**只订正理由、不动数值**。
        //   ⚠️ 旧措辞（留痕，⛔ 别照抄）：曾写「原版方向 `(sinθ,cosθ)`、我们 `(cosθ,sinθ)` ⇒ 差 90°，再翻 180°」——
        //   那两步**分开看都不对**（`RotationDir` 本身就是 `(cos,sin)`，有效轴才是 `(sin,cos)`）。
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
                                                             new Color(0.047f, 0f, 0.016f), 188f,
                                                             1080f / 1920f),   // 整屏 1920×1080 ⇒ h/w（原版 CompensateAspectRatio）
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

        // ============================================================ 🔴 **`A1638`（2026-10-12）：左竖导航「点亮第 i 颗」的【唯一共用口】**
        //
        // 原版那一层 = `NavigationPanelController`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/NavigationPanelController.cs`），
        // 本仓的等价物**一直都在**（本文件的 `SelectNav`），只是以前只有裸魔数 `SelectNav(1/2/3)` 三个调用点
        // ⇒ 现在收成一个**照原版命名的口**，四个入口都走它（两处写同一条规则 = 迟早不一致）。
        //
        // **原版逐点对位**（第一权威 = `d:/2/tools/decomp_full/`）：
        //   · `NavigationPanelController.SetToggle(NavigationPanelToggleType type)`（`__SetToggle.c`）：
        //     `GetToggle(type)` 找到那颗 `NavigationPanelToggle` → 它的 `Toggle.isOn == false` 时
        //     `SetIsOnWithoutNotify(toggle, true)` ⇒ **语义 = 「把第 i 颗点成选中」**（幂等：已经是选中的不动）。
        //   · **四个调用点**（全库就这四个，`grep -rl NavigationPanelController__SetToggle`）：
        //       ① 收藏 `CollectionScreen__Open.c:14`   → `SetToggle(nav, 1)`
        //       ② 商店 `ShopWindow__Open.c`（末段）      → `SetToggle(nav, 2)`
        //       ③ 奖励 `MainMenuRewardsWindow__Open.c`  → `GetToggle(nav,3)` + `SetIsOnWithoutNotify(true)`（等价）
        //       ④ 活动 `BaseParentEventWindow__Open.c`  → `SetToggle(nav, 5)`
        //   · **`Main`(0) 与 `Social`(4) 【没有】`SetToggle` 调用点** —— 那两颗靠**导航钮自己的 `ToggleGroup`**
        //     （`m_AllowSwitchOff = 0` = 单选）在**点击**时点亮 ⇒ 我们对位的是 `OnNavClick` 里那一句
        //     （那是**另一条机制**，⛔ 别把它和 `SetToggle` 混成一条）。
        //   · `SetMainToggle(GameWindow)`（`__SetMainToggle.c`）—— **只有在 `GameWindow.ESCPressed` 的
        //     第三道门槛 `updateNavPanel` 为真时才被调到**（`__ESCPressed.c` 末段），语义 = 「关窗回主菜单
        //     ⇒ 灯回到 `Main`」（反编译体里那个 `GameWindow` 形参**没被读到** ⇒ 语义由调用点判；如实标）。
        //     ⇒ 本仓落在 `Shell/WindowsManager.cs` 的 `GameWindow.ESCPressed`；值的四个来源（原版 `= 1` 的窗
        //       = Collection / Inbox / Shop / Social）写在 `updateNavPanel` 那一格。

        /// <summary>原版 `NavigationPanelToggleType`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/NavigationPanelToggleType.cs`，
        /// **值逐条照抄**：`Main=0 · Collection=1 · Shop=2 · Rewards=3 · Social=4 · Event=5`）。
        /// <para>🆕 **2026-10-23（活动线 `L01`/`L03` 收尾）：第 6 颗 `Event` 已经建出来了** ——
        /// `BuildNavigationPanel` 末尾那颗 **`Event Navigation Button`**（原版 prefab 真名）⇒
        /// `SetNavToggle(Event)` 现在真的会点亮它；点它 ⇒ `OnNavClick(5)` → `WindowsManager.OpenEventHub(Parent)`。
        /// ⚠️ **原版那一颗是「每个 live 事件一颗」**（`NavigationPanelController.BuildEventButtons` 按服务端
        /// 🔶 自己设计（非原版）：只建一颗「活动」总入口钮 · 原版按服务端事件列表每事件一颗，本地没有事件列表 · TODO(谁定的)｜原注：事件 `Instantiate`）；本机**没有事件列表** ⇒ 本仓建**一颗**总入口钮（如实标，⛔ 不是原版的一对一映射）。</para></summary>
        public enum NavigationPanelToggleType { Main = 0, Collection = 1, Shop = 2, Rewards = 3, Social = 4, Event = 5 }

        // ============================================================ 🔴 **2026-10-24（用户口径）：左竖导航暂时只留两颗**
        //
        // 用户原话：「**游戏里左边导航栏那里按钮，保留游戏和收藏两个按钮及其界面，下面的商店、奖励、活动等按钮隐藏，
        // 暂时不需要。**」
        // ⇒ `PLAY`(0) / `COLLECTION`(1) **显示**；`SHOP`(2) / `REWARDS`(3) / `SOCIAL`(4) / `EVENT`(5) **隐藏**。
        //
        // 🔴 **隐藏 = 不显示 + 点不到，⛔ 不是删功能**（铁律 11 仍然成立：这是用户拍板的【暂时】口径）：
        //   · **节点照建**（几何 / 文案 / 图标 / 红点全在，`menu.Find(name)` 照样取得到 ⇒ 既有几何断言基线一条都不用动），
        //     建完只 `SetActive(false)` ⇒ 画面上不出现（`ImageQuad` 是自带 `MeshRenderer` 的普通 GO，
        //     GameObject 不活就**根本不渲染**，不存在「自己那套批处理绕过 activeSelf」这种事）；
        //   · **命中区 `Hit` 一律不建**（`NavButton` 里命中那一段整段跳过）⇒ 没有 `WindowButton`
        //     ⇒ `PointerLayer.AllButtons()`（`FindObjectsByType<WindowButton>`，默认不收 inactive）**收不到它**
        //     ⇒ **真鼠标点不到、方向键也走不到**。
        //     ⛔ 只 `SetActive(false)` 不够（历史坑「藏起来了却点得到」→ `资料/真Play待验清单.md` E12）；
        //        本仓的命中是**另建的透明 quad + `WindowButton`** ⇒ **不建它 = 物理上不存在**，这才是硬保证。
        //   · **空位保留**（不上移）：步进仍 153.33、`Shop` 仍在 y=450.1。原版没有「只显示两颗」这一档，
        //     这是用户口径下的取舍；**保留空位 = 改动最小、且既有几何/步进断言基线一条都不用动**。
        //   · **开窗代码一个字都不动**：`OnNavClick` 那 6 个 `case` 全留着，
        //     `OpenShop` / `OpenRewards` / `OpenSocial` / `WindowsManager.OpenEventHub` 照样能从别处调到。
        //
        // 🔴 **恢复办法（一个开关）**：把 `OnlyPlayAndCollection` 改成 `false` ⇒ 六颗全回来（命中区也一起建）。
        public const bool OnlyPlayAndCollection = true;

        /// <summary>第 <paramref name="idx"/> 颗导航钮**这一版显示吗**（`PLAY`/`COLLECTION` 恒显示，见上）。
        /// <para>🔴 **生产（`NavButton` / `SelectNav`）与自检共用这一处** —— ⛔ 别在自检里再写一份 `idx &lt; 2`
        /// （两份迟早不一致；恢复开关一翻，两边一起跟着变才对）。</para>
        /// <para>⚠️ 越界（`idx &lt; 0` 或 `&gt;= 6`）返回 `false`：那是**调用点写错**，由
        /// `LightNavToggle` / `SelectNav` 各自的越界分支出声，⛔ 不要在这里吞掉。</para></summary>
        public static bool NavItemShown(int idx)
        {
            if (idx < 0 || idx >= 6) return false;
            return idx < 2 || !OnlyPlayAndCollection;
        }

        /// <summary>**点亮左竖导航第 i 颗** —— 本仓的 `NavigationPanelController.SetToggle`（判据见上面那一段）。
        /// <para>**为什么是 static**：调用点散布在别的类 / 别的场景（`GameWindow.ESCPressed` 在
        /// `Shell/WindowsManager.cs`，而卡组编辑、对战那些场景**没有** `MainMenuRuntime` 实例）——
        /// 与 `OpenInbox` / `OpenSettings` / `OpenProfile` 那三条改成 `static` 是同一个理由。
        /// **没有实例时是「无事可做」，不是失败**（那些场景本来就没有左竖导航）⇒ 不出声、直接返回。</para>
        /// <para>⚠️ **不要在这里写「第几颗是什么」的映射** —— 映射就是 `NavigationPanelToggleType` 本身
        /// （值照原版），⛔ 别再写一张 `switch`。</para></summary>
        public static void SetNavToggle(NavigationPanelToggleType type)
        {
            // 🔴 **2026-10-12（第二十三会话 `D-M1` 诊断）：改走 `InstanceOrFind()`**（原来直读 `Instance`）。
            //   为什么：`Instance` 唯一赋值点在 `Awake`，而本类**没有 `[ExecuteAlways]`** ⇒ **编辑模式（自检）里恒 null**
            //   ⇒ `SetNavToggle` 第一句就 `return` ⇒ 自检那条「恰好某一颗亮」永远红的（且是**假红**：实现没坏、是取不到实例）。
            //   同文件 `EscapePressed`（`本文件` 附近）**早就走 `InstanceOrFind()`**，这里当时漏了 ⇒ 两处写法不一致。
            //   ⚠️ **运行期零可观测差异**：没有主菜单的场景里 `FindFirstObjectByType` 照样返回 null ⇒ 仍「无事可做」。
            var rt = InstanceOrFind();
            if (rt == null) return;                  // 不在主菜单场景 ⇒ 没有左竖导航可点（正常，不是失败）
            rt.LightNavToggle(type);
        }

        /// <summary>`SetNavToggle` 的实例那半边（越界时**出声**：`Event` 那颗本仓没建）。</summary>
        void LightNavToggle(NavigationPanelToggleType type)
        {
            int idx = (int)type;
            if (idx < 0 || idx >= _navBtns.Count)
            {
                // ⛔ 不许静默失败：调用点写错 / 面板还没建全时**说清楚是哪一颗、实际有几颗**。
                // 🆕 2026-10-23：**第 6 颗（`Event`）已经建出来了**（`BuildNavigationPanel` 末尾那颗
                //   `Event Navigation Button`）⇒ 这一段现在是**真·越界**的诊断，不再承担「第 6 颗没有」的解释。
                Debug.LogWarning("[Menu] `SetNavToggle(" + type + ")`（= 第 " + (idx + 1) + " 颗）"
                                 + " —— 左竖导航现在只有 " + _navBtns.Count + " 颗"
                                 + "（`Main/Collection/Shop/Rewards/Social/Event`）⇒ **这一下不点任何一颗**");
                return;
            }
            SelectNav(idx);
        }

        // ---- 🆕 **2026-10-11（`A1522④`）：换高亮【只动高亮那一颗】，⛔ 不再重建整块 `Navigation Panel`** ----
        // 为什么非改不可（两条，都是实测）：① 原来 `SelectNav` 是「销毁整个面板 → 重新建」；
        //   一旦它被**点击**触发（本次要接的就是这条），面板连同**正在执行 `onClick` 的那颗钮**
        //   一起被 `DestroyImmediate` 掉（自检是批处理、`Application.isPlaying == false` ⇒ 立即销毁）；
        //   ② 更硬的一条：自检里 `var nav = menu.Find("Navigation Panel")`（`Editor/MainMenuScene.cs` 的 `var nav = menu.Find("Navigation Panel")`）
        //   那个引用**后面还有 5 处要用**（`CardPresentation/Editor/MainMenuScene.cs` / `CardPresentation/Editor/MainMenuScene.cs` / `CardPresentation/Editor/MainMenuScene.cs` / `CardPresentation/Editor/MainMenuScene.cs` / `CardPresentation/Editor/MainMenuScene.cs`）——
        //   面板一被销毁，那些断言全变成「节点不在」。⇒ 本方法**只重建高亮那一颗**。
        readonly List<Transform> _navBtns = new List<Transform>();
        readonly List<Vector2> _navBands = new List<Vector2>();
        /// <summary>导航钮「选中态高亮」的**默认**色 —— 原版 `Main Menu Navigation Button` prefab 的
        /// `Selected highlight/Image.m_Color` 实读 = (1, 0.0805, 0, 1)（正红）。
        /// 🆕 **2026-10-23（活动线收尾）**：**第 6 颗不是这个色** —— 它自己那份 prefab
        /// （`Event Navigation Button`）实读 = **(0.67, 0.132, 0.0853, 1)**（暗红）⇒ 逐颗记在
        /// <see cref="_navHiTints"/>，⛔ 别拿默认色一刀切（那会把原版两套色抹成一套）。</summary>
        static readonly Color NavHiTintDefault = new Color(1f, 0.08054f, 0f, 1f);
        readonly List<Color> _navHiTints = new List<Color>();

        // ============================================================ 🆕 2026-10-19（N117）左栏命中区的模态闸
        //
        // 五颗导航钮的**透明命中区 quad**（`NavButton` 里那一颗 `Hit`）。模态窗在场时整组降到
        // `QNavHitBlocked`、不在场时回到 `QNavHit` —— 见 `QNavHit` 那段与 `RefreshNavHitQueue`。
        readonly List<ImageQuad> _navHitQuads = new List<ImageQuad>();

        /// <summary>🔴 **2026-10-19（`N117`）**：按「最上面那一扇窗」刷新左栏命中区的档。
        /// <para>模态 ⇒ <see cref="QNavHitBlocked"/>（低于所有窗压暗层 ⇒ 点击被压暗层吃掉、**左栏不可点**）；
        /// 非模态 / 无窗 ⇒ <see cref="QNavHit"/>（保持 2026-10-18 的「商店开着也能点左栏」，**不回退**）。</para>
        /// <para>判据只有一处：<see cref="GameWindow.BlocksLeftNav"/>（基类照原版 prefab 的
        /// `windowsPlacement` / `type` 两个字段算）。**幂等**，可在任何时刻重复调。</para>
        /// <para>调用点（`Shell/WindowsManager.cs` 四处）：`OpenWindow` 末尾 · `NotifyClosed` 末尾 ·
        /// `CloseAllWindows` 末尾 · `SetBaseMenu` 末尾。⚠️ 本类自己的 `BuildNavigationPanel` 末尾也调一次
        /// （重建左栏后旧 quad 没了，档要重新落）。</para></summary>
        void RefreshNavHitQueue()
        {
            if (_navHitQuads.Count == 0) return;      // 左栏还没建（或这一场不是主菜单场景）⇒ 无事可做
            var wm = WindowsManager.Instance;          // ⛔ 不用 `Get()`：刷新不该顺手现建一台窗口管理器
            var top = wm != null ? wm.TopWindow : null;
            int q = (top != null && top.BlocksLeftNav) ? QNavHitBlocked : QNavHit;
            for (int i = _navHitQuads.Count - 1; i >= 0; i--)
            {
                var hq = _navHitQuads[i];
                if (hq == null) { _navHitQuads.RemoveAt(i); continue; }   // Unity 假 null（面板重建/场景换过）
                hq.SetRenderQueue(q);
            }
        }

        /// <summary>窗口层调的那一跳（`WindowsManager` 四处）。**找不到主菜单 ⇒ 什么都不做**（正常：
        /// 卡组编辑/战场那种没有左栏的场景）。
        /// <para>⚠️ **刷新「场上所有」主菜单实例**，不是只刷 `InstanceOrFind()` 那台 ——
        /// 自检里会另起一棵树再 `Build()` 一次（`Editor/MainMenuScene.cs` 的 A855 那三条），
        /// 只刷「第一台」会让**另一棵**的左栏停在旧档上（静默、且只在多树宿主里现形）。
        /// 运行期只有一台 ⇒ 这条等价于只刷一台。</para></summary>
        internal static void RefreshNavHitForTopWindow()
        {
            var all = Object.FindObjectsByType<MainMenuRuntime>(FindObjectsSortMode.None);
            if (all != null && all.Length > 0)
            {
                for (int i = 0; i < all.Length; i++)
                    if (all[i] != null) all[i].RefreshNavHitQueue();
                return;
            }
            // `FindObjectsByType` 默认不收 inactive ⇒ 整棵树此刻不活时兜底用注册的那台（真 Play 有 `Awake`）。
            var rt = Instance;
            if (rt != null) rt.RefreshNavHitQueue();
        }

        /// <summary>自检读口：左栏命中区**这一刻**的档（-1 = 左栏还没建）。</summary>
        public int NavHitQueueForTest
        {
            get
            {
                for (int i = 0; i < _navHitQuads.Count; i++)
                    if (_navHitQuads[i] != null) return _navHitQuads[i].RenderQueue;
                return -1;
            }
        }

        /// <summary>自检读口：`QNavHit` / `QNavHitBlocked` 的真实取值（期望值别在自检里抄字面量）。</summary>
        public static int QNavHitForTest { get { return QNavHit; } }
        public static int QNavHitBlockedForTest { get { return QNavHitBlocked; } }

        /// <summary>换选中页（左竖导航）。**只有选中的那个画高亮**（语义同原版，见上）。
        /// 🔴 **2026-10-11（`A1522④`）就地改法（⛔ 别退回「销毁整块面板重建」）**：本方法只撤掉旧的高亮那一颗、
        /// 在**新选中的按钮**底下重建一颗 —— 面板本体 / 五颗按钮 / 图标文字**一个都不动**
        /// （理由见上面那两行；`Editor/MainMenuScene.cs` 还钉着「`Selected highlight` 全树只有 1 个」）。
        /// ⚠️ 与原版**已知的一档差异**（如实记）：原版那颗高亮由 `Toggle.graphic` + `toggleTransition = 1`
        ///    **淡入淡出**驱动、且 5 颗**树里都在**；我们是「**建一颗 / 撤一颗**」的硬开关
        ///    （判据 → `资料/普查产出_第十八会话/B_三条判据挖掘.md` 问题 C 第 2 / 7 条）。</summary>
        public void SelectNav(int idx)
        {
            // ⚠️ **两档要分开**（`A1638` 现核）：「面板还没建」(`Count == 0`) 与「调用点越界」
            //    (`idx` 超出既有颗数) 是**两件事** —— 前者是**正常**（`Build()` 跑之前的任何调用，
            //    例如 `WindowsManager` 那台宿主先醒），后者是**调用点写错了**。混成一档会在正常路径上刷假告警。
            if (_navBtns.Count == 0) return;                 // 面板还没建 ⇒ 没有高亮可点（不是失败）
            if (idx < 0 || idx >= _navBtns.Count)
            {
                // ⛔ 不许静默：越界就什么都不改，并且**说一声**（调用点写错时能一眼看出来）
                Debug.LogWarning("[Menu] `SelectNav(" + idx + ")` —— 导航钮只有 " + _navBtns.Count
                                 + " 颗 ⇒ **这一下不改高亮**（调用点越界了？）");
                return;
            }
            // 🔴 **2026-10-24（用户口径）：被隐藏的那四颗 —— 灯不动。**
            //   为什么不能照挪：`SelectedNav` 一挪，本方法会把**可见那颗**的高亮撤掉、再往一棵
            //   `SetActive(false)` 的子树里重建 ⇒ 画面上变成「左栏一颗都不亮」（而且没有任何报错）。
            //   为什么不是告警：调用点**仍然合法** —— `OpenShop/OpenRewards/OpenSocial` 开窗后照旧调
            //   `SetNavToggle(自己的那一颗)`，`GameWindow.ESCPressed` 也可能要 `SetMainToggle`；
            //   隐藏这几颗是**有意**的，所以这是「有意不做事」，不是失败（判据见 `OnlyPlayAndCollection` 那一段）。
            if (!NavItemShown(idx)) return;
            if (idx == SelectedNav)
            {
                // 🔴 **`A1638`：幂等**（原版 `SetToggle` 逐字就是幂等的 —— `m_IsOn` 为真时什么都不做）。
                //    但**早退之前要先确认那一颗的高亮真的在**：`SelectedNav` 只是个数，
                //    高亮是树里一颗**节点** —— 两者会脱钩（面板重建、节点被别的路 `DestroyImmediate`、
                //    或「选中态没变但节点没了」那类静默坏法）⇒ 只比数就会**永远不补画**。
                var cur = _navBtns[idx];
                if (cur != null && cur.Find("Selected highlight") == null) RepaintNavHighlight(idx);
                return;
            }
            SelectedNav = idx;
            // 🆕 2026-10-17（T16 / `N4`）：从**左栏**那棵子树找（运行期它在壳的安全区节点下、不是 `_root` 的后代）；
            //    `_navPanel` 还没建时才退回老的根递归（自检夹具那一档）。
            var old = _navPanel != null ? FindIn(_navPanel, "Selected highlight") : Find("Selected highlight");
            if (old != null) DestroySafe(old.gameObject);
            RepaintNavHighlight(idx);
        }

        /// <summary>给第 <paramref name="idx"/> 颗导航钮画上选中态高亮（亮底图 × `m_Color (1,0.0805,0,1)` = 正红）。
        /// **建树那条路与换页那条路共用这一处**（两处各写一遍 = 迟早不一致）。
        /// ⚠️ 档位 `QNavHighlight`（= `QPanel + 1`）：原来与导航底板**同队列**（2910）⇒ 谁盖谁不确定、
        ///   实测高亮没画出来；判据与实测读数 → `QNavHighlight` 那段注释。</summary>
        void RepaintNavHighlight(int idx)
        {
            var b = _navBtns[idx];
            if (b == null) return;                       // 面板还没建（`BuildNavigationPanel` 没跑过）
            var band = _navBands[idx];                   // 这一颗的 (顶, 底) —— 建树时记下来的
            var tint = (idx >= 0 && idx < _navHiTints.Count) ? _navHiTints[idx] : NavHiTintDefault;
            Rect(b, "40k_main_bt_selected_BW", -0.3f, 164.0f, band.x, band.y, "Selected highlight",
                 QNavHighlight, tint);
        }

        void BuildNavigationPanel(Transform root)
        {
            // 🆕 2026-10-11（`A1522④`）：面板要重建时（重新进主菜单）把「换高亮」那张表也清掉 ——
            //    它就是 `SelectNav` / `RepaintNavHighlight` 的取件口（见 `_navBtns` 那段）。
            _navBtns.Clear(); _navBands.Clear(); _navHiTints.Clear();
            _navHitQuads.Clear();                 // 🆕 N117：旧面板的命中区随重建一起作废（见 RefreshNavHitQueue）
            // 🔴 **2026-10-24（用户口径）**：`SelectedNav` 是**实例字段**（跨 `Build()` 保留）。若它停在一颗
            //   **被隐藏**的编号上（恢复开关从 false 翻回 true、或某个夹具直接写过这个字段），下面
            //   `NavButton` 的「`idx == SelectedNav` 才画高亮」就**一颗都画不出来**，而 `SelectNav` 又会因
            //   隐藏早退 ⇒ **永远没有高亮**（静默）。⇒ 建树之前先把它拉回一颗显示着的编号（`Main`）。
            if (!NavItemShown(SelectedNav)) SelectedNav = 0;
            // A332：原版 `Navigation Panel` = 191 × 1080（`anchor (0,0)-(0,1)` + `sizeDelta.x 191` ⇒ 整高）
            // 🔴 **2026-10-11（FX6）修**：第二实参原来写 `LayoutSpace.DesignHeight`（**世界单位** 10，不是 px 1080）
            //   ⇒ 高被除两次 108、`rect.height` 只剩 0.093。本行是干净的正对照：宽 `191f` 是 px ⇒ 那条断言本来就过。
            var p = New(root, "Navigation Panel", 191f, 1080f);
            // 🆕 **2026-10-17（T16 / `N4`）**：这里必须**把引用留下来** —— 运行期左栏挂在**壳**的
            // `Safe area Only Horizontal` 下（跨场景），不再是 `_root` 的后代 ⇒ 下面 `Find("Navigation Panel")`
            // 那条惰性兜底会落空（`SetNavPanelVisible` / `NavPanelXForTest` 全靠 `_navPanel`）。
            _navPanel = p;
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
            // 🔴 **2026-10-11（界面复刻）：第 7 个实参【不再传英文原文，改传 Loc 键名】** ——
            //   原版这五颗 GO 上各挂一颗 I2 `Localize`（`mTerm` 见下），屏上的字是**运行时查表**得到的；
            //   我们原来直接把资产里的英文 `m_text` 写死 ⇒ 中文档永远出不来英文（`A1493②` 那条）。
            //   键名与中文出处逐条记在 `Core/Loc.cs` 的 E1 那一节（⛔ 别在这里再抄一份）。
            NavButton(holder, 0, "Home",       "40k_main_bt_play",       143.4f, 313.1f, "MainMenu/NavigationPanel/HomeButton", 137.435f, 4.6f, 160.9f, 146.4f, 33f);
            NavButton(holder, 1, "Collection", "40k_main_bt_collection", 296.7f, 466.4f, "MainMenu/NavigationPanel/Collection", 140.0f, 12.8f, 152.8f, 298.4f, 33f);
            // ⚠️ `COLLECTION` 传的是 **max 33**（不是原版存下来的 30.45）—— 那个 30.45 是**自适应之后的结果**，
            //    让 autosize 自己缩出来才和原版同一条路（它框里放不下，会自己缩小）。
            NavButton(holder, 2, "Shop",       "40k_main_bt_shop",       450.1f, 619.8f, "MainMenu/NavigationPanel/Shop",       140.0f, 12.8f, 152.8f, 451.8f, 33f);
            NavButton(holder, 3, "Rewards",    "40k_main_bt_rewards",    603.4f, 773.1f, "MainMenu/NavigationPanel/Rewards",    140.0f, 12.8f, 152.8f, 605.1f, 33f);
            // ⚠️ 第五颗：**键名是 `…/Friends`**（原版 `mTerm` 原文，⛔ 不是 `…/Social`）—— 屏上英文才是 `SOCIAL`。
            NavButton(holder, 4, "Social",     "40k_main_bt_friends",    756.7f, 926.4f, "MainMenu/NavigationPanel/Friends",    140.0f, 12.8f, 152.8f, 758.4f, 33f);

            // ============================================================ 🆕 **2026-10-23（活动线 `L01`/`L03` 收尾）：
            // **第 6 颗 = 「事件钮」**（原版 `NavigationPanelController.BuildEventButtons` 运行期
            //   `Instantiate(eventButtonPrefab)` 建的那一颗；`NavigationPanelToggle.toggleType = 5`）。
            //
            // 🔴 **判据（全部现读，⛔ 不是「按五颗的样子猜」）**：
            //   ① **prefab 真名 = `Event Navigation Button`** —— 不是 `Main Menu Navigation Button - Event`
            //      （`bundle_menus_assets_all/GameObject/Event Navigation Button`，GO pid `1412792127474277301`；
            //      反查链：`NavigationPanelController.eventButtonReference.m_AssetGUID =
            //      ae2560f5eb7d1634d8fc917de4f80369` → `AssetBundle_1.json` 的 `m_Container` → 该 pid）⇒ 节点名照它。
            //   ② **几何逐值同五颗**（`menu_dump bundle_menus_assets_all --rt -7272436173638493259` 现读）：
            //      根 **164.31 × 169.68** · `Selected highlight` 0..164.31 / 0..169.68 ·
            //      `Image` **13.1..153.1 / 1.7..141.7（140×140）** · `Text Background` 9.6..156.6 / 118.3..157.6 ·
            //      `Badge Highlight` 118.1..153.1 / 91.7..126.7。⇒ 位置由 `Buttons Container` 的同一条 VLG
            //      （spacing −16.35 · padTop −5 · 钮高 169.68）决定：**第 6 颗顶 = 143.41 + 5×153.33 = 910.06**
            //      ⇒ 本行取 **910.0 / 1079.7**（与前五颗同一套取整）；图标顶 = 钮顶 + 1.70 = **911.7**。
            //   ③ **`Selected highlight` 的色与五颗不同**（⚠️ 别一刀切）：这份 prefab 实读
            //      `m_Color = (0.67, 0.132, 0.0853, 1)`（暗红），而 `Main Menu Navigation Button` 是
            //      `(1, 0.0805, 0, 1)`（正红）⇒ 走 `hiTint` 逐颗传。
            //   ④ **图标 = liveops 资产 `40k_main_bt_event`**（`OpenEventButton.SetButtonInfo`：
            //      `Image.sprite = LoadAsset(ev, 0)`，用的就是事件的 `Icon`）。
            //      🔴 **2026-10-24（C3 判据 + 本轮落地）**：原来这里写「本机没有可加载的 sprite ⇒ 留空」——
            //      判据其实一直在本地：切好的 PNG 在 `d:/2/Warpforge_tools/data/ui_extract/
            //      liveopsicons_assets_all_sprites/Sprite/40k_main_bt_event.png`（135²、PPU 100、Simple、border 0），
            //      已登记进 `工具/import_original_art.py` 的 `MENU_IMAGES` 并导到
            //      `Resources/Art/ui_menu/40k_main_bt_event.png` ⇒ **照原版把这张图画出来**。
            //      ⚠️ **原版这颗的 `Image.m_Color` 是 (0.7358490, …, 1) 灰**（不是白）—— 见下面 `iconTint`。
            //   ⑤ **`OpenEventButton.closeOtherMenus = 0`**（MB 实读）⇒ 点它**不关**别的窗
            //      （我们的 `OpenEventHub` 也没传 `closeAll`，两边一致）。
            //   ⑥ **文字**：原版由 `TryGetLabel(ev,4) ?? TryGetLabel(ev,0)`（= 活动标题）填，本机没有事件实例
            //      ⇒ 用 **`EVENT`** 兜底（**我们挑的**，⛔ 不是原版文案；与其它五颗同样全大写）。
            //      `MainMenu/NavigationPanel/*` 本地官方词条表只有 5 条（`HomeButton/Collection/Shop/Rewards/Friends`），
            //      **没有 `…/Event`** ⇒ 不能走 `Loc.T`（缺键会印一整串键名到界面上）。
            //   ⑦ ⚠️ **一处有意偏离**：原版没有在期事件时 `ParentEventHandler.BeforeRefresh` 会把这一颗
            //      `SetActive(false)`；本机没有 liveops 事件源、而验收口径是「**入口必须点得开**」
            //      ⇒ **恒显示**（与 `ParentEventWindow` 「没有在期事件也建事件钮」那条同口径）。
            //   ⑧ 🔴 **图标色 = 原版插槽的灰**（C3「顺手发现 ①」）：`Event Navigation Button/Image` 的
            //      `m_Color = (0.7358490228652954, …, 1)`（现读 `MonoBehaviour_-1269977380222428235.json`），
            //      而通用 `Main Menu Navigation Button/Image` 是 **(1,1,1,1) 白**；`SetButtonInfo` **只设 sprite、
            //      不写 color** ⇒ 原版事件图标**天生比五颗暗 26%**。照它传 `iconTint`（`WindowButton` 的色偏是
            //      「基准色 × 状态键」，见 `WindowButton.Collect()` ⇒ 悬停/按下也在灰底上乘，与原版 `CrossFadeColor` 同义）。
            // 🆕 **2026-10-27（B88_J2）**：按原版 `OpenEventButton.SetButtonInfo` 补 **title + 倒计时** 两件。
            //   判据 = `menu_dump bundle_menus_assets_all "Event Navigation Button" --depth 6`（本轮现读）：
            //     · `Text (TMP)`（= title 那一行）fs24 / base24 / auto[18~33] / Center·Capline / 色 (0.996,0.929,0.71)
            //     · `Timer`（`TimerDisplay`）**局部** 848.3−876.9 = −28.6 … 1069.8−876.9 = 192.9 /
            //       236.7−80.1 = 156.6 … 261.7−80.1 = 181.6（221.56 × 25.00）
            //     · 其子 `Timer Text`（prefab 出厂 INACT）fs **26.35** / base **30** / auto[18~28] / Center·Capline / 白
            //       prefab 样例串 = **`5d 20h 15m`**（⇒ 显示格式 `Xd Yh Zm`）
            //   title 取值 = 原版 `TryGetLabel(ev,4) ?? TryGetLabel(ev,0)`（4 = NavigationBarTitle）；
            //   取不到 ⇒ 保留原来的 `EVENT` 兜底（**我们挑的**，原版是空态）。
            BuildEventNavButtons(holder);        // 🆕 B89-E27（账 `L474`）：原版 `BuildEventButtons` = 每个在期 Parent 一颗；口径/几何/兜底全在类尾同名方法

            // 🆕 **2026-10-19（N117）**：六颗建完（⚠️ **2026-10-24 起只有显示着的那几颗有命中区** ——
            //   被隐藏的四颗整段跳过，见 `OnlyPlayAndCollection`），按「当前最上面那扇窗」落一次档
            //   （重建左栏之后旧 quad 已经销毁 ⇒ 上面那几句 `_navHitQuads.Clear()` 把它们丢了，
            //    不在这里补一次的话会停在**新建时的默认档** = `QNavHit` —— 模态窗开着时就是错的）。
            RefreshNavHitQueue();
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
        /// <param name="iconTint">🆕 **2026-10-24（活动线 · 事件钮图标）**：图标那格的**基准色**（原版
        /// `Image.m_Color`）。默认 `null` = 不染色（材质的 `color` 就是白，五颗导航钮那一档）。
        /// 🔴 为什么必须在这里传、不能建完再改：`WindowButton.Collect()` 把**当时的 `Tint` 记成基准色**，
        /// 色偏是「基准色 × 状态键」⇒ 在 `TintOn`/首次悬停**之前**写好，悬停/按下才会在灰底上乘
        /// （与原版 `CrossFadeColor` 同义）。判据 → `Event Navigation Button/Image` 的
        /// `m_Color = (0.7358490, 0.7358490, 0.7358490, 1)`（`bundle_menus_assets_all` 现读）。</param>
        void NavButton(Transform parent, int idx, string name, string art, float y1, float y2, string locKey,
                       float ih, float ix1, float ix2, float iy1, float fontPx,
                       string nodeName = null, string label = null, Color? hiTint = null,
                       Color? iconTint = null, System.DateTime? timerEndUtc = null)
        {
            var b = New(parent, nodeName ?? ("Main Menu Navigation Button - " + name), NavBtnW, NavBtnH);
            // 🆕 **2026-10-11（`A1522④`）：把「这一颗 + 它的 (顶,底)」记进表** —— `SelectNav` 换高亮时
            //    只撤旧的那一颗、在新选中的这一颗底下重建（⛔ 面板本体不重建，理由见 `SelectNav` 的注释）。
            while (_navBtns.Count <= idx) { _navBtns.Add(null); _navBands.Add(Vector2.zero); _navHiTints.Add(NavHiTintDefault); }
            _navBtns[idx] = b; _navBands[idx] = new Vector2(y1, y2);
            _navHiTints[idx] = hiTint ?? NavHiTintDefault;    // 🆕 2026-10-23：逐颗色（第 6 颗 = 0.67/0.132/0.0853）
            // 选中态高亮：亮底图 × `m_Color (1,0.0805,0,1)` = **正红**（只有选中的那个画）
            // 🔴 **2026-10-11（A1521⑥）**：档位由 `QPanel` 改成 `QNavHighlight`（= `QPanel + 1`）——
            //   原来与导航底板**同队列**（2910）⇒ 谁盖谁不确定，**实测高亮没画出来**。判据与实测读数
            //   全在 `QNavHighlight` 那段注释里（原版兄弟序 + 两张图逐点量的对照 + `DeckRuntime` 的先例）。
            if (idx == SelectedNav) RepaintNavHighlight(idx);

            // 🆕 **2026-10-23（活动线收尾）**：`art` 传**空** = 「这颗 prefab 的 `Image` 槽本来就是空的」那一档。
            // 🔴 **2026-10-24 更正（铁律 5）**：原版第 6 颗 `Event Navigation Button` 的 `Image` **不是空槽** ——
            //   它的 `m_Sprite = PPtr(FileID 3, PathID -7836703677562955936)`，落在 `0_GeneralUI Atlas` 上，
            //   是一张**占位图**；运行期 `OpenEventButton.SetButtonInfo` 再换成活动的 `Icon`
            //   （`LoadAsset(ev,0)`）。⇒ 本件已按真图 `40k_main_bt_event` 接上；`art` 传空这条路留给
            //   「真的没有图」的调用点（**不记账**进 `MissingArt`：那是原版的空态，不是「图取不到」）。
            var icon = string.IsNullOrEmpty(art) ? null : Art(art);   // 先确认图在不在（不在就别画一个空按钮）
            ImageQuad iconQ = null;     // 🆕 A222：这颗**可见**的图形要交给 `WindowButton` 当色偏目标（见下）
            if (icon != null)
            {
                iconQ = ImageQuad.Create(b, icon, Center(ix1, ix2, iy1, iy1 + ih), H(iy1, iy1 + ih),
                                         new Vector2(0.5f, 0.5f), "Image");
                if (iconQ != null)
                {
                    iconQ.SetAspect((ix2 - ix1) / ih); iconQ.SetRenderQueue(QContent);
                    // 🆕 2026-10-24：基准色**必须在这里写**（`WindowButton.Collect()` 稍后把它记成基准色，
                    //   见形参 `iconTint` 的注释）。五颗导航钮不传 ⇒ 保持材质的白。
                    if (iconTint.HasValue) iconQ.SetTint(iconTint.Value);
                }
            }
            else if (!string.IsNullOrEmpty(art)) MissingArt.Add(art);
            else
            {
                // 原版第 6 颗的 `Image` prefab 上是**占位图**（`0_GeneralUI Atlas`），运行时再换活动图标
                // ⇒ 那一颗已按真图接上；走到这里只表示「调用点没给 art」⇒ **节点照建、不出图**
                // （⛔ 不是「把节点删掉」，也不是「拿别处的图冒充」）。
                var img = New(b, "Image", ix2 - ix1, ih);
                img.localPosition = Center(ix1, ix2, iy1, iy1 + ih);
            }

            // `Text Background`(nametag) 的 y 偏移实证 = **+118.3**、`Badge` = **+91.7**（§五 C 表，五个按钮一致）
            // ⚠️ 这两块矩形**同时**是命中区并集的组成部分（下面 `hitR`）⇒ 只写一份、两处共用，
            //    别在命中那一段再手抄一遍（抄了改一处忘一处 = 静默错）。
            var textBgR = new PxRect(9.3f, y1 + 118.3f, 156.2f, y1 + 157.7f);
            Rect(b, "40k_main_bt_nametag", textBgR.x1, textBgR.x2, textBgR.y1, textBgR.y2, "Text Background", QContent);
            // 🔴 **2026-10-11（界面复刻 · A1493②）：文案走 `Loc.T(locKey)`** —— 原版这五颗 GO 上各挂一颗 I2
            //   `Localize`，屏上的字是**运行时查表**得到的；我们原来直接把资产里的英文 `m_text`（`PLAY`/…）
            //   写死 ⇒ **中文档永远出英文**。键名与中文出处逐条记在 `Core/Loc.cs` 的 **E1** 那一节。
            //   ⚠️ `Loc.T` 缺键时返回的是**键名本身**（不是空串）⇒ 界面上一眼能看出是「查不到」，不会静默。
            // 🔴 原版这行 TMP 带 **`enableAutoSizing`（18→33）**，框只有 146.9px 宽 ⇒ `COLLECTION` 是**缩出来的**
            //    （实测原版它的在场字号就是 **30.45**）—— 不缩会画出框外被裁。
            // 🔴 **A1024③（2026-10-17）换面**：主菜单场景这一棵 **0 颗 Asar**（判据 = `menu_dump …
            //    bundle_scenes_scenes_mainmenuwarpforge "Game UI" --depth 14` 现读：导航 `Text (TMP)`、
            //    `ChatPreview/text`、模式卡 `Event Title` / `Timer Description` / `Timer Text`、
            //    `Position #` / `Position Value` / `Individual rating value` 全是 **Pragati**）⇒ 本文件全走 `LatinCardBody`。
            //    ⚠️ 场景里确有 Asar 的 `Title`/`Player Name` 等，但那些是**别的窗**（卡牌详情/顶栏）的件，不属本文件。
            Text(b, label ?? Loc.T(locKey), textBgR.x1, textBgR.x2, textBgR.y1, textBgR.y2, 6,
                               new Color(0.9961f, 0.9294f, 0.7098f), "Text", fontPx,
                               // 🔴 **2026-10-17（`A1233`）**：原来这里是「先 `Text`、再自己补
                               //    `navText.SetAutoFitBox(146.92/108, 39.39/108, 18, 33, 24)`」——
                               //    收口进本方法自己的形参（同一件事只写一处）。
                               //    四格判据（原版 `bundle_scenes_scenes_mainmenuwarpforge` 五颗逐颗现读）：
                               //    `m_SizeDelta 146.92 × 39.39` · `auto[18~33]` · `m_fontSizeBase = 24.0`
                               //    （`MonoBehaviour_1716/1749/1750/1751/1752`）。
                               //    ⚠️ 框高按本行矩形的 `157.7 − 118.3 = 39.4`（原版 TMP 自己那份 sd.y = 39.39，
                               //       差 0.01px —— 两条边分别来自文案条与 TMP 两个节点，这里不拆第二颗节点）。
                               wrapPx: 146.92f, autoMinPx: 18f, autoMaxPx: 33f, autoBasePx: 24f,
                               face: TmpFont.Face.LatinCardBody);

            // 🆕 **2026-10-27（B88_J2）**：`Timer` 那一格（原版 `OpenEventButton.timer` = `TimerDisplay`）。
            //   几何/字号判据见 EVENT 那颗调用点上面那段（`menu_dump … "Event Navigation Button"` 本轮现读）。
            if (timerEndUtc.HasValue) { BuildNavEventTimer(b, timerEndUtc.Value); RegisterEventNavTimer(b, timerEndUtc.Value); }   // 账 549：登记进按秒泵
            // 🔴 **2026-10-11（A305①）**：上面第 5 个数据实参 = 原版这一颗的 `m_fontSizeBase` **原文**。
            //    判据（本轮自己扫的原版场景 `bundle_scenes_scenes_mainmenuwarpforge`，逐颗现读）：
            //    五个导航钮的 `Text (TMP)`（`'PLAY'`/`'REWARDS'`/`'SHOP'`/`'COLLECTION'`/`'SOCIAL'`，
            //    `m_SizeDelta` 全是 **146.92 × 39.39**，与我们的框逐值相同）—— `auto[18~33]` ·
            //    **`m_fontSizeBase` 五颗全是 `24.0`**（`MonoBehaviour_1716/1749/1750/1751/1752`）。
            //    ⚠️ 与 V7 §二·3 #30 的关系：那一行记的是**全库** `[18~33]` 那族的 base 分布
            //    （24.0 ×4 / 33.0 ×3）⇒ 本条按**场景内实读**取 24（那 3 颗 33.0 不在主菜单这一族里）。
            // 红点：亮图 × `m_Color (0.7358,0.7358,0.7358,1)` = **中灰**（原版这就是「无内容/禁用」的灰点）
            // 🔴 **只有 REWARDS（第 4 个导航钮）有真实的通知源** —— 原版它由 `Missions.CheckNotification` 驱动；
            //    其余四个我们**没有通知源** ⇒ 按 `Hide()` 的样子 **alpha = 0**（不是画一个假的灰点）。
            var badgeR = new PxRect(117.8f, y1 + 91.7f, 152.8f, y1 + 126.7f);
            var badgeQuad = Rect(b, "40K_notification_number", badgeR.x1, badgeR.x2, badgeR.y1, badgeR.y2, "Badge Highlight", QContent,
                 BadgeAlpha(false));   // 底色出厂 alpha 0（原版 `Awake` 第一句）；真正的显隐/计数交给下面那一层
            // 🆕 **2026-10-25（复刻第 1 轮 · 代理 H）**：红点接上原版那套「通知 → 聚合器 → 徽记」。
            //   判据（逐份现读 `bundle_scenes_scenes_mainmenuwarpforge` 的 `notificationDefinitions`）：
            //     Home **空**（永不亮）· Collection `CollectionBadge` · Shop `ShopBadge`+`InventoryBadge` ·
            //     Rewards `RewardsBadge`（useFilter=1 / parentId=""）· Social `SocialBadge`。
            //   ⚠️ 原来这里是一句 `BadgeAlpha(idx == 3 && DailyData.RewardsHasBadge)`（静态 bool + 瞬切 alpha）
            //      ⇒ 没有**计数**、没有**补间**、没有**类型继承**（`MissionsBadge : RewardsBadge` 那条）。
            //   详细判据/差异 → `Shell/BadgeNotifications.cs` 文件头。
            BadgeWiring.Attach(badgeQuad, badgeR, b, QText, BadgeTint,
                               NavBadgeDefs(idx), "Nav[" + idx + "]");   // 账 546：EVENT 那颗运行期补一条（= 原版 `AddNotificationType`）

            // 点击区（**原版口径 = 可射线件的并集**，⛔ 不是整颗钮高）。
            // 原版每个导航钮上挂的是 `OpenWindowButton{windowToOpenPrefab, windowPayload}`：
            // `0 Main(PLAY)` 例外 —— 它开的是**场景内的** `MainMenuWindow`（正本 §三 第 1 条）。
            // A332：⚠️ **原版没有这个节点** —— 原版导航钮自己就是按钮（GO 上挂 `EverguildToggle` /
            //   `OpenWindowButton`），射线由 uGUI 按它自己的 `Graphic` 收；我们这套没有 uGUI 射线
            //   ⇒ 另建一个透明命中区。⛔ 别去原版找「Hit」这个名字（那一族 5 颗子件是
            //   `Selected highlight` / `Image` / `Text Background` / `Badge Highlight`，没有 Hit）——
            //   **但「可点范围」照原版可射线件的并集算**（下面这一段）。
            //
            // 🔴 **2026-10-22（W62-D2 · 真机缺陷）就地改口径：原来那颗 quad 高 = 整钮高 169.68**
            //   （`NavBtnH`），而相邻钮步进只有 **153.33**（= 169.68 − 原版 VLG `m_Spacing −16.35`，
            //   `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1973.json`）
            //   ⇒ **每对相邻钮的命中区叠 16.4 px**。两颗队列都是 `QNavHit`、`ImageQuad.z` 又恒 0
            //   ⇒ 平局落回「先收集者赢」，而实测那是**前兄弟** ⇒ **点「奖励」的上沿命中「商店」**
            //   （真机日志 `_tmp_view/click_log.txt:8604-8611`：画布 (73.5, 615.0) 同时压在
            //    Shop / Rewards 两颗命中区上，Shop 赢；Rewards 那颗钮的顶边就是 603.4）。
            //
            // **原版判据（逐字段实读；判据全文 = `资料/普查产出_1022/W62_缺陷诊断.md` §D2）**：
            //   · 钮根**没有 Graphic**（`RectTransform_1247/1245` 只挂 `EverguildToggle` / `OpenWindowButton`）；
            //   · 可射线件 = 图标 `Image` **140×140** + `Text Background` **146.92×39.39** + `Badge Highlight` **35×35**
            //     （`MonoBehaviour_2119/2121` 等：`m_RaycastTarget = 1`、`m_RaycastPadding` **全 0**）；
            //     —— TMP `Text` 的矩形与 `Text Background` **同一格** ⇒ 并集不多一块。
            //   · 并集 ⇒ 每颗只跨 **1.7…157.7**（钮底 12 px 死区），相邻两颗只叠 **2.615 px**，
            //     且重叠带里**后兄弟（Rewards）在上层必赢**（uGUI 遍历序，见 `PointerLayer` 的平局判据）。
            // ⇒ 命中区 = 这三块的 `MenuDraw.Union`（全工程唯一一份；⛔ 别在别处再写一份并集算式）。
            // ============================================================ 🔴 **2026-10-24（用户口径）：这一颗被隐藏时的收尾**
            //
            // 判据与恢复办法 → 上面 `OnlyPlayAndCollection` 那一段。**走到这里时子件已经全建好了**
            // （图标 / 文案条 / 文案 / 红点都在 ⇒ 自检的几何断言照样量得到、`menu.Find` 照样取得到），
            // 只差最后两件事：
            //   ① **整颗 `SetActive(false)`** ⇒ 不显示（`ImageQuad` 自带 `MeshRenderer`，GO 不活就根本不渲染）；
            //   ② **命中区一颗都不建**（下面 `MenuDraw.Hit` 那一段整段跳过）⇒ 没有 `WindowButton`
            //      ⇒ `PointerLayer` 的按钮表里根本没有它 ⇒ **真鼠标点不到、方向键也走不到**。
            // ⛔ 别把这里改成「建完命中区再 SetActive(false)」：那是历史坑「藏起来了却点得到」的形状
            //    （`资料/真Play待验清单.md` E12）—— 命中的存在与否必须与可见性一致。
            if (!NavItemShown(idx))
            {
                b.gameObject.SetActive(false);
                Debug.Log("[Menu] 左竖导航第 " + (idx + 1) + " 颗（" + name + "）按 2026-10-24 用户口径**隐藏**："
                        + "节点照建（`SetActive(false)`，几何/文案/图标都在）、**命中区不建**（真鼠标点不到）"
                        + " —— 恢复 = 把 `MainMenuRuntime.OnlyPlayAndCollection` 改成 `false`");
                return;
            }

            var hitR = MenuDraw.Union(
                new PxRect(ix1, iy1, ix2, iy1 + ih),        // 图标 `Image`（140×140；Home 137.435）
                textBgR,                                    // `Text Background`（+ 同格的 TMP `Text`）
                badgeR);                                    // `Badge Highlight` 35×35
            int captured = idx;
            var hitGo = MenuDraw.Hit(b, "Hit", hitR, QNavHit, () => OnNavClick(captured));
            var wb = hitGo != null ? hitGo.GetComponent<WindowButton>() : null;
            if (hitGo != null)
            {
                // 🆕 2026-10-18：命中区队列见 `QNavHit`（子窗开着时左栏也要能点）；
                // 🆕 2026-10-19（N117）：交给 `RefreshNavHitQueue` 按模态窗升降。
                var hq = hitGo.GetComponentInChildren<ImageQuad>(true);
                if (hq != null) _navHitQuads.Add(hq);
            }
            if (wb == null)
            {
                // 不许静默失败：`MenuDraw.Hit` 只在 `CardArt.Solid()` 取不到时才不建 quad
                // （那种情况它自己会 LogWarning）；这里再如实说一句「这颗钮点不动」。
                Debug.LogError($"[Menu] 导航钮 `{name}` 的命中区没建出来 ⇒ 这一颗**真鼠标点不动**");
                return;
            }
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C9）**：原版这五颗导航钮是 `EverguildToggle` +
            //   **`allowClickWhenAlreadySelected = 0`** ⇒ 再点**当前**那一颗时连按钮音一起跳过
            //   （`EverguildToggle__OnPointerClick.c` 第一段）。行为面与 `SelectNav` 已有的
            //   「`idx == SelectedNav` 幂等」同向；这里把**声音**也一起挡在派发之前。
            wb.allowClickWhenAlreadySelected = false;
            wb.isOn = () => SelectedNav == captured;
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
            CloseOutgoingNavWindowOnToggleOff(idx);      // 🆕 B89-E27（账 `L550` · 原版 `OpenWindowButton/OpenEventButton.ToggleValueChange(false)`）；口径见类尾那个方法
            switch (idx)
            {
                case 0:   // PLAY / Home —— 原版开**场景内**的 `MainMenuWindow`
                    // 🔴 **2026-10-17（界面线 `T12`）：原来这里落 `default`、只打一句「还没接」** ⇒
                    //   从收藏/商店/奖励/社交窗里点 PLAY **回不到主菜单**（`U1` 普查判定 高）。
                    // 判据 = `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2556.json`
                    //   （`OpenWindowButton{windowToOpenScene = MB1927(MainMenuWindow), closeOtherMenus = 1}`）
                    //   ＋ `OpenWindowButton__OpenWindow.c`：`+0x40` 那个 byte **直接**喂给
                    //   `WindowsManager.OpenWindow(..., closeAll)` ⇒ 等价物 = 打开常驻主菜单 + 关掉其余窗。
                    OpenHome();
                    break;
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
                case 5:   // EVENT —— 🆕 2026-10-23（活动线 L01/L03 收尾）：原版第 6 颗 = liveop 事件钮
                    // 判据：`OpenEventButton__ButtonClick.c` → `OpenEventButton__OpenWindow.c` →
                    //   `WindowsManager.OpenWindow(GetComponentReference(ev, 10 /*MainWindow*/), ev)`
                    //   ＋ `BaseParentEventWindow__Open.c` 自己那句 `SetToggle(nav, 5)`（本仓 = 下面那句
                    //   `SetNavToggle(5)`，值序与 `NavigationPanelToggleType.Event = 5` 逐值相同）。
                    // 🔴 **`ev → 哪扇窗`**：本机没有 liveops 事件实例 ⇒ 走唯一共用口 `OpenEventHub`，
                    //   按 `EventWindowKind` 开对应那扇窗；那颗钮 ↔ **`ParentEventWindow`**（= 原版
                    //   `Parent` 事件的 `MainWindow`，映射已核，见 `WindowsManager.OpenEventHub` 的 doc）。
                    // ⚠️ `closeOtherMenus = 0`（MB 实读）⇒ **不关**别的窗（`OpenEventHub` 也没传 `closeAll`）。
                    WindowsManager.OpenEventHub(ParentEventWindow.EventWindowKind.Parent);
                    break;
                default:
                    Debug.Log($"[Menu] 左竖导航第 {idx + 1} 个钮（{NavName(idx)}）**还没接** —— " +
                              "原版开的是各自的窗口/场景，属阶段二后面的层");
                    break;
            }
            // ============================================================ 🔴 **2026-10-11（`A1522④`）**
            // **点哪一颗，选中态高亮就移到哪一颗** —— 这一句是本条账的落点。
            //
            // 判据（第一权威 = 反编译 + prefab 字段，落到原版上；全文 → `资料/普查产出_第十八会话/
            //   B_三条判据挖掘.md` 问题 C）：原版那五颗挂的是 **`EverguildToggle : Toggle`**
            //   （`bundle_scenes_scenes_mainmenuwarpforge/GameObject/Main Menu Navigation Button - Home.json`
            //    的组件表里**没有 `Button`**；MB `2098` 的 `m_Script` `-2618094383086163561` ⇒ `EverguildToggle`），
            //   而 `m_Group → 2616` = **`ToggleGroup`（唯一字段 `m_AllowSwitchOff = 0`）= 单选**
            //   ⇒ **点别的钮【一定】会把高亮移过去**（组里只允许恰好一颗 `isOn`）。
            //   点击链也证实：`OpenWindowButton__Awake.c:69-75` 判到 `m_Button` 是 `Toggle`
            //   ⇒ 订的是 `onValueChanged`（`Button` 那一支才走 `m_OnClick`）。
            //
            // 🔴 **我们原来的形状（= 本条要修的真缺陷）**：`OnNavClick` **自己从不改选中态** ——
            //   只有 `OpenCollection` / `OpenShop` / `OpenRewards` 三条**在开窗之后顺带** `SelectNav(idx)`
            //   ⇒ **PLAY（第 0 颗）与 SOCIAL（第 4 颗）点了高亮纹丝不动**（`OpenSocial` 没有那一句）。
            //   ⇒ 现在**统一收到这一处**：那三句改走共用口 `SetNavToggle`（`A1638` 收口），
            //   走到这里再被 `SelectNav` 的「`idx == SelectedNav` 早退」吃掉 —— 留着无害，⛔ **别顺手删**（删了「开窗入口被单独调用」时就不换高亮了）。
            // ⚠️ 与**悬停色偏**是两回事，⛔ 别混：`EverguildToggle__OnPointerEnter.c:3` 在 `isOn == true`
            //   时**直接 return**（已选中的那颗连 hover 都不走），`m_HighlightedColor = (0.7217,0.8152,1.0)`
            //   是**独立的一层**（我们的 `NavHoverKey`）——「hover 亮」不等于「被选中」。
            // 🔴 **`A1638`（2026-10-12）**：这一句改走**共用口** `SetNavToggle`（下面那一段的定义）
            //   —— 本仓 `idx` 的取值与 `NavigationPanelToggleType` **逐值同序**（0..4），所以这一跳是**恒等**、
            //   零行为变化；改它只为「四个入口都走同一个口」，⛔ 不是为了改行为。
            SetNavToggle((NavigationPanelToggleType)idx);
        }

        // ============================================================ 🆕 2026-10-17（界面线 T12）：左栏收起 / 放回
        //
        // 原版 = `NavigationPanelController.Toggle(bool option, bool withAnimation)`，逐句见
        // `d:/2/tools/decomp_full/NavigationPanelController__Toggle.c`：
        //   · `gameObject.activeSelf == option` ⇒ **动画取消**（直接切）；
        //   · 先 `DOTween.Kill` 上一条 tween；
        //   · **藏**：起点 = `originalAnchorPosition`（`Awake` 抓的展示位）→ `DOAnchorPos(hiddenPosition, animationTime)`
        //         → OnComplete `SetActive(false)`；**不动画** ⇒ 直接写 `hiddenPosition` + `SetActive(false)`；
        //   · **放**：先写 `hiddenPosition` → `SetActive(true)` → `DOAnchorPos(originalAnchorPosition, animationTime)`。
        // 字段真值（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1955.json` 实读）：
        //   `hiddenPosition = (−1162.77001953125, 0)` · `animationTime = 0.30000001192092896`。
        // ⚠️ **`originalAnchorPosition` 不是序列化值** —— 它是 `Awake` 里抓的；我们对应「建树那一刻的 localPosition」。
        // ⚠️ 缓动 = **DOTween 的默认 `OutQuad`**（`DOAnchorPos` 没显式给 ease；同 `Core/Tooltip` 那条 fade 的口径）。
        //   这是我们照库默认值写的，**不是从原版数据里量出来的**（如实标）。
        const float NavHiddenX = -1162.77001953125f;    // 设计 px（`hiddenPosition.x`）
        const float NavToggleTime = 0.30000001192092896f;

        Transform _navPanel;
        bool _navHasShown;
        float _navShownX;                 // = 原版 `originalAnchorPosition.x`（本地单位）
        bool _navTweening, _navHideAtEnd;
        float _navFromX, _navToX, _navT;

        /// <summary>把左竖导航【收起/放回】（原版 `NavigationPanelController.Instance.Toggle(show, animate)`）。
        /// 调用点（判据 = `U1` 普查 §二 左栏那两行）：
        /// · 卡组编辑窗 `DeckEditingWindow.TryOpen.c:139` 收起 · `Close` 放回；
        /// · 排位窗 `RankedEventWindow__OnEnable.c:72` 收起；
        /// · 回到主菜单 `MainMenuWindow.ToFocus.c` 放回（本仓由那些窗的 `Close()` 承担）。
        /// ⚠️ 没有 `MainMenuRuntime` 时（非壳宿主）**静默跳过**：那一档「左栏」根本不存在，不是失败。</summary>
        public static void SetNavPanelVisible(bool show, bool animate)
        {
            var inst = InstanceOrFind();
            if (inst == null) return;
            inst.SetNavPanelVisibleInternal(show, animate);
        }

        void SetNavPanelVisibleInternal(bool show, bool animate)
        {
            if (_navPanel == null) _navPanel = Find("Navigation Panel");
            if (_navPanel == null)
            {
                Debug.LogWarning("[Menu] `Navigation Panel` 不在树里 ⇒ `SetNavPanelVisible(" + show + ")` 这一下落空（出声，不静默）");
                return;
            }
            if (!_navHasShown) { _navShownX = _navPanel.localPosition.x; _navHasShown = true; }
            float hiddenX = LayoutSpace.Px(NavHiddenX);
            // 原版首句：已经处在目标可见态 ⇒ **动画取消**（直接切，不播那 0.3 s）
            if (_navPanel.gameObject.activeSelf == show) animate = false;
            _navTweening = false;                                  // = 原版 `DOTween.Kill`
            if (!show)
            {
                if (!animate)
                {
                    SetNavX(hiddenX);                              // 原位版：写 hiddenPosition
                    _navPanel.gameObject.SetActive(false);
                    return;
                }
                _navPanel.gameObject.SetActive(true);
                SetNavX(_navShownX);                               // 原版：起点 = originalAnchorPosition
                BeginNavTween(hiddenX, hideAtEnd: true);
            }
            else
            {
                SetNavX(hiddenX);                                  // 原版：先写 hiddenPosition
                _navPanel.gameObject.SetActive(true);
                if (!animate) { SetNavX(_navShownX); return; }
                BeginNavTween(_navShownX, hideAtEnd: false);
            }
        }

        void SetNavX(float x)
        {
            if (_navPanel == null) return;
            var lp = _navPanel.localPosition;
            lp.x = x;
            _navPanel.localPosition = lp;
        }

        void BeginNavTween(float toX, bool hideAtEnd)
        {
            _navFromX = _navPanel.localPosition.x;
            _navToX = toX;
            _navT = 0f;
            _navHideAtEnd = hideAtEnd;
            _navTweening = true;
        }

        /// <summary>每帧推左栏那条补间（批处理没有帧循环 ⇒ 自检里要直调；真机由 `Update` 推）。</summary>
        void TickNavPanelTween() { TickNavPanel(Time.unscaledDeltaTime); }

        void TickNavPanel(float dt)
        {
            if (!_navTweening) return;
            if (_navPanel == null) { _navTweening = false; return; }
            _navT += dt;
            float k = NavToggleTime > 0f ? Mathf.Clamp01(_navT / NavToggleTime) : 1f;
            float e = 1f - (1f - k) * (1f - k);                    // DOTween 默认 OutQuad
            SetNavX(Mathf.Lerp(_navFromX, _navToX, e));
            if (k < 1f) return;
            _navTweening = false;
            if (_navHideAtEnd) _navPanel.gameObject.SetActive(false);
        }

        /// <summary>自检用：左栏补间是不是还在跑（批处理里 `Update` 不跑，要手动 `Tick`）。</summary>
        public bool NavPanelTweeningForTest { get { return _navTweening; } }

        /// <summary>自检用：左栏面板的当前 x（本地单位；不在树里 ⇒ `NaN`）。</summary>
        public float NavPanelXForTest
        {
            get
            {
                var p = _navPanel != null ? _navPanel : Find("Navigation Panel");
                return p != null ? p.localPosition.x : float.NaN;
            }
        }

        /// <summary>自检用：推一帧左栏补间（与真机 `Update` 走同一个 `TickNavPanel`）。</summary>
        public void TickNavPanelForTest(float dt) { TickNavPanel(dt); }

        static string NavName(int idx)
        {
            switch (idx) { case 0: return "PLAY"; case 1: return "COLLECTION"; case 2: return "SHOP";
                           case 3: return "REWARDS"; case 4: return "SOCIAL";
                           case 5: return "EVENT";       // 🆕 2026-10-23：第 6 颗（`Event Navigation Button`）
                           default: return "?"; }
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
        //      签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs`）；
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
        ///   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs` 那三个 `ShowPopUp`）。
        ///   ⚠️ 与别的调用点不同：这里 **`closeOnEsc = 1`**（那一族多数传 0）⇒ **这扇弹窗按 ESC 关得掉**。</item>
        /// </list>
        ///
        /// <para>⚠️ **两处如实标注（铁律 3）**：
        /// · **文案**：`localizeTexts = 1` 说的是「那几个串是本地化 key」，而**词条在远端语言表里**
        ///   （本地只有 key、没有英文原文）⇒ 三句中文**是我们写的**（先例 = `Shell/ShopData.cs` 的 `LegendaryWarnText`）。
        /// · **退出动作**：原版那个委托的目标方法（`DAT_1842aa360`）在 `decomp_full` /
        ///   `il2cpp_out` 的方法表里**查不到名字** ⇒ 我们按字面语义做 `Application.Quit(0)`
        ///   （先例 = `Core/PlayerBoot.cs` 里那句 `Application.Quit(0)`），并且**编辑器里 Quit 是空操作 ⇒ 出声**。</para></summary>
        public static bool EscapePressed()
        {
            var mm = InstanceOrFind();
            if (mm == null)
            {
                // 🆕 **2026-10-18（`N67`）出声**：本行原来是**静默 `return false`** —— 真机上「ESC 按了没反应」
                //   在这一支与「ESC 根本没进进程」之间**日志上完全一样**（N67 那次就是零日志）。
                //   ⚠️ 这一支只在「场上一扇窗都没有、且当前场景没有常驻主菜单」时走到（卡组编辑 / 对战场景）。
                Debug.Log("[Menu] ESC 落到「主菜单」这一支，但场上**没有 `MainMenuRuntime`** ⇒ 什么都不做"
                          + "（当前场景不是主菜单、也没有窗可关 —— 这是**正常路径**，不是缺陷）。");
                return false;
            }
            return mm.EscPressed();
        }

        /// <summary>`Instance` 没有就现找一台。
        /// 🔴 **编辑模式下 `Awake` 不跑**（只对 `[ExecuteAlways]` 的脚本跑，本类不是）
        /// ⇒ 自检里 `Instance` 可能是 null（本类原来只有 `Awake` 那一处赋值）。
        /// 写法与 `PointerLayer.Instance` 同一条：**惰性取用、不依赖任何生命周期回调**。
        /// 🆕 **2026-10-12 改成 `public`**：自检（`Editor/MainMenuScene.cs`）要拿它断「解析到的是本台」——
        ///   ⛔ 别再让调用方各自写一遍 `Instance ?? FindFirstObjectByType`（两处写同一条规则 = 迟早不一致）。</summary>
        public static MainMenuRuntime InstanceOrFind()
        {
            if (Instance != null) return Instance;
            return Object.FindFirstObjectByType<MainMenuRuntime>();
        }

        /// <summary>见静态入口 <see cref="EscapePressed"/> 的判据。
        /// 本扇 `closeOnEsc = 0` ⇒ ① 那一跳不做，只做 ②。</summary>
        public bool EscPressed()
        {
            var wm = WindowsManager.Get();     // 🔴 2026-10-11：走**统一读口**（原来直接读 `Instance` ⇒ 切场景后它是 null ⇒ 模式卡点不动）
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

        // ---- 🆕 2026-10-18（第四会话 · 双语③ 波 1 · P2）：退出窗那三句**改走语言表** ----
        // 🔴 **2026-10-18（波 1b · A1026 裁决）：三条全部【改指原版键】`Demo/MainMenu/*`** ——
        //   键名 = 原版 `I2.Loc.Localize.mTerm` 原文（`d:/2/tools/il2cpp_out/stringliteral.json` 里各 1 条），
        //   且与本文件下面那段老注释**逐字吻合**。波 1 当时用的是自拟键（`MainMenu/Settings/ExitGame/*` +
        //   `MainMenu/General/Cancel`），那只是因为原版键**当时不在 `Loc.Table` 里**；
        //   波 0b 已把这三条建进表 ⇒ 调度台裁定改回原版键名。**值逐字相同 ⇒ 两语档都零变化。**
        //   ⚠️ **两张表都搜过才叫查过**：只搜 `assets_full` 会得出「原版没有」的假结论（波 0 当年就是这么漏的）。
        const string lkExitConfirm = "Demo/MainMenu/ExitGame";
        const string lkExitOk      = "Demo/MainMenu/ExitButton";
        const string lkExitCancel  = "Demo/MainMenu/CancelButton";

        /// <summary>退出窗的正文（原版词条 key = **`Demo/MainMenu/ExitGame`**，`localizeTexts = 1`）。
        /// 🔴 **词条在远端语言表里**，本地只有 key、没有英文原文 ⇒ 下面三句**都是我们写的**（铁律 3；
        /// 先例 = `Shell/ShopData.cs` 的 `LegendaryWarnText`，那句也是同一种处境）。
        /// 🆕 2026-10-18（双语③ P2）：**改成读语言表**（值逐字 = 原来那句，中文档界面不变）。
        /// 🆕 2026-10-18（波 1b · A1026）：**键名改成原版那颗**（`Demo/MainMenu/ExitGame`）——
        /// 波 0b 已把这三条原版键连同**我们写的两列文案**一起收进 `Core/Loc.cs` ⇒ 上面那句
        /// 「本地只有 key、没有原文」**只对【原版发行版自带的那份语言表】成立**，对我们自己的表不成立。
        /// ⚠️ **是属性、不是 `const`**：`Loc.T` 不是编译期常量；而且属性**每次现读** ⇒
        /// 换了语言之后**新弹**的窗当场就是新语言（`const`/`static readonly` 会在初始化那一刻冻死）。</summary>
        public static string ExitGameText { get { return Loc.T(lkExitConfirm); } }
        /// <summary>右钮文案（原版 key = **`Demo/MainMenu/ExitButton`** —— 原版把它当 `secondButton` 传）。**我们写的**。</summary>
        public static string ExitGameOkText { get { return Loc.T(lkExitOk); } }
        /// <summary>左钮文案（原版 key = **`Demo/MainMenu/CancelButton`** —— 原版把它当 `primaryButton` 传）。**我们写的**。</summary>
        public static string ExitGameCancelText { get { return Loc.T(lkExitCancel); } }

        /// <summary>「退出游戏」按下去做什么。
        /// 🔴 **原版那个委托的目标方法读不到名字**（`SettingsMenu__ExitGamePopup.c` 里是个数据地址
        /// `DAT_1842aa360`，`il2cpp_out` 的方法表按 RVA 查不到它）⇒ 我们按**字面语义**做 `Application.Quit(0)`
        /// （先例 = `Core/PlayerBoot.cs` 里那句 `Application.Quit(0)`）。⚠️ **编辑器里 `Quit` 是空操作** ⇒ 这里出声（红线：不许静默失败）。</summary>
        public static void QuitGame()
        {
            Debug.Log("[Menu] 「退出游戏」按下 ⇒ `QuitApplication()`"
                      + "（= 原版 `PlayerDataManager.QuitApplication`"
                      + "：② `LeaveRoom` ③ 等 0.5s 再 `Application.Quit`）"
                      + (Application.isEditor ? "（⚠️ 编辑器里 `Quit` 是空操作）" : ""));
            QuitApplication();
        }

        /// <summary>
        /// 开收件箱（原版 `Inbox Menu`，由顶栏 `InboxBtn` 上的 `OpenWindowButton` 开）。
        /// ⚠️ 单机没有服务器 ⇒ 里面是**空态**（原版没消息时也是这个样子），**不是没做**。
        /// 🔴 **2026-10-06（A123）：已经开着就复用那一扇**（判据 → 上面 `OpenByRef` 那段）—— 原来每次新建。
        /// </summary>
        /// 🔴 **2026-10-17（B25）改成 `static`**：顶栏那四颗钮的接线收进了共用件 `Shell/TopBar.cs`，
        /// 而卡组编辑（独立场景 `DeckEditor.unity`）**没有 `MainMenuRuntime` 实例** ⇒ 这三个入口
        /// （`OpenInbox` / `OpenSettings` / `OpenProfile`）必须能被**没有实例**的地方调到。
        /// 它们本来就不碰实例状态（体内只有一句 `OpenByRef(...)`）⇒ 改 `static` 是**零行为变化**。
        /// ⚠️ 左竖导航那三条（`OpenSocial` / `OpenRewards` …）**仍是实例方法** —— 它们会顺带
        /// `SelectNav(idx)` 改导航钮的选中态；顶栏这四颗**不碰导航**。
        /// ⛔ 别把这句「顶栏钮开哪一扇窗」的映射在 `TopBar` 里再写一遍（那边只**转调**这三个入口）。
        /// ⚠️ 自检里原来写成 `menu.OpenInbox()`（实例语法）—— 改成 `MainMenuRuntime.OpenInbox()`
        /// （C# 不允许用实例访问静态成员），语义一个字没变。
        public static InboxWindow OpenInbox()
        {
            var win = OpenByRef(RefInbox, wm => InboxWindow.Create(wm));
            // 原版 `InboxWindow` 那颗 prefab 的 `updateNavPanel = 1`（`Shell/InboxWindow.cs` 文件头逐字段实读）
            // ⇒ 从收件箱按 ESC 出来（回到主菜单）时，左竖导航的灯回到 `Main`。⚠️ 它**不点亮任何一颗**
            //（信封是顶栏那颗钮，不是左竖导航的）。
            MarkUpdatesNavPanel(win);
            return win;
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
        public static SettingsWindow OpenSettings()
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
            var win = OpenByRef(RefSocial, wm => SocialWindow.Create(wm), true);
            // ⚠️ **这里【不】点第 5 颗** —— 原版全库**没有** `SetToggle(nav,4)` 的调用点
            //（`grep -rl NavigationPanelController__SetToggle` 只有 4 个：收藏 1 / 商店 2 / 奖励 3 / 活动 5）
            // ⇒ `SOCIAL` 那颗亮起来靠的是**导航钮自己的 `ToggleGroup`**（点它那一下），
            // 对位的是本文件 `OnNavClick` 末尾那一句，⛔ 别在这儿补一句「顺手也点一下」。
            MarkUpdatesNavPanel(win);                             // 原版这颗窗 `updateNavPanel = 1`
            return win;
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
        /// ✅ **2026-10-11（A177）订正**：这一句原来写「相关残余：`Shell/BattleLogTab.cs` 里那个入口也是直调
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
        /// （见 `RankedEventWindow.LeaderboardButtonClick`），**自检以外的调用点不该有**。
        /// ⚠️ 另外两棵**在原版里没有可达入口**：轮抽榜挂在轮抽活动窗的 `AlliancesEventScorePanel` 上
        /// （我们没做轮抽模式）；嵌入版 `Ranked Leaderboard Display` **全库零引用**。
        /// ⇒ 这个方法留给自检（同 `OpenBattleLogPopup` 那条先例）。
        /// 判据 → `资料/普查产出_0927/排行榜_入口与调用.md`。
        /// 🔴 **2026-10-06（A123）：已经开着就复用那一扇**（判据 → 上面 `OpenByRef` 那段）。
        /// ⚠️ 缓存键按 **kind** 分开（`LeaderboardWindow.NameOf(kind)`）—— 原版那两颗 prefab
        /// （`RankedSkirmishLeaderboardPopup` / `RankedClassicLeaderboardPopup Variant`）是**两条引用**
        /// ⇒ 缓存里自然也是两条（换 kind 再点会开第二扇，与「一棵 prefab 一个实例」同义）。
        /// ✅ **2026-10-11（A177）订正**：原来这里写「另一条入口 `Shell/RankedEventWindow.cs` 不在白名单
        /// ⇒ 不共用这份缓存」—— **已过期**：那条入口（`RankedEventWindow.LeaderboardButtonClick`）也收编到
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
        public static PlayerProfileWindow OpenProfile()
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
            SetNavToggle(NavigationPanelToggleType.Rewards);      // 原版 `MainMenuRewardsWindow__Open.c:14-16`（点亮第 3 颗 = REWARDS）
            return win;
        }

        /// <summary>回主菜单（左栏第 1 颗 PLAY/Home）。🔴 **2026-10-17（界面线 `T12`）新接**。
        /// <para>原版判据：那颗钮挂 `OpenWindowButton{windowToOpenScene = MainMenuWindow, closeOtherMenus = 1}`
        /// （`MB_2556.json:16-33`），而 `OpenWindowButton__OpenWindow.c` 把 `closeOtherMenus`（+0x40）
        /// **直接**传给 `WindowsManager.OpenWindow(win, data, closeAll)` ⇒ 等价物 =
        /// <c>OpenWindow(BaseMenu, null, closeAll: true)</c>。</para>
        /// <para>⚠️ 与 `ShowMainMenu()` 的区别：那个是原版 `WindowsManager.ShowMainMenu()`（`closeAll: false`，
        /// 用在「关掉上面那扇窗、回常驻主菜单」）；**这一颗钮是 `closeOtherMenus = 1`** ⇒ 必须 `closeAll: true`。</para></summary>
        public void OpenHome()
        {
            var wm = WindowsManager.Get();
            if (wm == null || wm.BaseMenu == null)
            {
                // 红线：不许静默失败 —— 「点了 PLAY 没反应」必须能从日志分辨
                Debug.LogWarning("[Menu] 点了 PLAY/Home，但 `WindowsManager.BaseMenu` 还没登记（场上没有常驻主菜单）"
                               + " ⇒ 这一下**回不到主菜单**（不是没接：`OpenHome` 已接，前提不成立）");
                return;
            }
            wm.OpenWindow(wm.BaseMenu, null, true);
            Debug.Log("[Menu] PLAY/Home ⇒ 回常驻主菜单（`OpenWindow(BaseMenu, closeAll: true)`，原版 `closeOtherMenus = 1`）");
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
            SetNavToggle(NavigationPanelToggleType.Collection);   // 原版 `CollectionScreen__Open.c:14`（点亮第 2 颗 = COLLECTION）
            MarkUpdatesNavPanel(win);                             // 原版这颗窗 `updateNavPanel = 1`（ESC 关掉 ⇒ 灯回 Main）
            return win;
        }

        /// <summary>把原版 `GameWindow.updateNavPanel`（字段偏移 **0x3A**）那一格写上 —— 语义见
        /// `Shell/WindowsManager.cs` 里那一格的 doc：**ESC 关掉这扇窗时把左竖导航回点到 `Main`**。
        /// <para>🔴 **为什么由【入口】写、而不是各窗的 `Create()` 里写**（如实标注，铁律 3）：
        /// 逐窗那一格照例是跟着 `type` / `placement` / `closeOnEsc` 一起写在**各窗的 `Create()`** 里，
        /// 而**这一轮 `Shell/CollectionWindow.cs` 有另一个写手**（不许同时两个写手改同一个文件）
        /// ⇒ 这一格先落在**入口**（与 `SetNavToggle` 那一句挨着，仍然是**一处定义、一处赋值**）。
        /// ⛔ **将来收口时把这一句搬进对应窗的 `Create()`**（那时删掉本方法的所有调用点即可，语义不变）。</para>
        /// <para>⚠️ 原版 `= 1` 的窗**四个**（逐窗实读 prefab）：`Collection` · `Inbox` · `Shop` · `Social`；
        /// 其余全是 `0`（本仓 `GameWindow.updateNavPanel` 的缺省值就是 `false`，与之一致）。</para></summary>
        static void MarkUpdatesNavPanel(GameWindow win)
        {
            if (win != null) win.updateNavPanel = true;
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
            SetNavToggle(NavigationPanelToggleType.Shop);         // 原版 `ShopWindow__Open.c` 末段（点亮第 3 颗 = SHOP）
            MarkUpdatesNavPanel(win);                             // 原版这颗窗 `updateNavPanel = 1`
            return win;
        }

        /// <summary>红点底图 `40K_notification_number` 的原版色 **#BCBCBC = (0.7358,0.7358,0.7358)**（正本 §二·2）。
        /// 🔴 **红点的显隐靠 alpha、不靠 `SetActive`** —— 原版 `UiBadgeNotification.Show()/Hide()` 只改
        /// 一个 float 字段（alpha 1.0 / 0）再把文本设成计数（`DF:UiBadgeNotification__Show.c` / `__Hide.c`）。
        /// 2026-09-23 前我们**五个导航钮的红点全画成不透明** ⇒ 与「出厂 `active=1`、运行时按通知亮」的原版不符。</summary>
        public static readonly Color BadgeTint = new Color(0.73585f, 0.73585f, 0.73585f, 1f);
        /// <summary>红点默认 **alpha 0**（= 原版 `Hide()` 之后的样子）。</summary>
        public static Color BadgeAlpha(bool on) { return new Color(BadgeTint.r, BadgeTint.g, BadgeTint.b, on ? 1f : 0f); }

        /// <summary>导航钮序号 → 原版那条接线表里的键（`BadgeWiring.DefinitionsFor` 用）。
        /// 0 PLAY/Home · 1 COLLECTION · 2 SHOP · 3 REWARDS · 4 SOCIAL · 5 EVENT（原版 EVENT 那颗**没有**红点）。</summary>
        static string NavBadgeKey(int idx)
        {
            switch (idx)
            {
                case 0: return "Home";
                case 1: return "Collection";
                case 2: return "Shop";
                case 3: return "Rewards";
                case 4: return "Social";
                default: return "None";
            }
        }

        // ---- §五 B：顶栏 —— 🔴 **2026-10-17（B25）整段已删，实现在 `Shell/TopBar.cs`** ----
        //
        // 这里原来是 `BuildUpperBar` / `BuildPlayerProfile`（以及下面 A374 那一带的 `BuildResourcesBar`、
        // 更下面的 `BuildTopAvatar`）四个 `private` 实例方法。它们与 `TopBar` 里那份**逐字重复** ⇒ 收口到共用件。
        // ⛔ **别把参数写回来**：顶栏的几何/结构/接线**只有 `TopBar` 一处**（本文件只留数据与入口）。
        // 判据、为什么收口、以及收口时修掉的**两处真实偏离**（资源计数器次序反了 · 信箱红点恒不亮）
        // → `Shell/TopBar.cs` 的文件头（那一版是 2026-10-17 B25）。

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
        //    数据在后端）：我们用 `Wallet.Of(记账键)`（用户 2026-09-17 定的「不做真实经济 / 资源固定 2000」（用户 2026-10-24 从 9999 改）
        //    ⇒ 那个包里任何没记过账的键读出来都是 `StartAmount` = 2000）。**币种 → 记账键**这张表
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

        /// <summary>一颗资源计数器的规格。三个字段各自的判据 → `Shell/TopBar.cs` 的
        /// `BuildResourcesBar`（**建树已搬去那边**，2026-10-17 B25；本文件只留这张表）。</summary>
        public sealed class CounterSpec
        {
            public int Currency;      // 原版 `GameCurrency`
            public string Icon;       // 原版 `ICurrency.GetIcon(IconSize.Small)` 的 sprite 名；null = 本地判据没查到
            public string WalletKey;  // **我们自己的**：`Wallet` 里这一路的记账键；null = 我们没有这一路 ⇒ 按 0 计
            public int MaxAmount;     // 原版 `ICurrency.MaxAmount`（≤ 0 = 无上限，不写 `/max`）
        }

        /// <summary>**次序 = 原版 `List.Add` 序**（①）—— ⛔ 别按大小/字母重排。
        /// 🔴 **2026-10-17（B25）改成 `public`**：建表（几何 + 建树）的实现在共用件 `Shell/TopBar.cs`，
        /// 它读的就是这张表；⛔ **别再在别处抄第二张**（`TopBar` 原来是按 `DefaultCurrencies` 建的，
        /// 次序整个反了 —— 判据 → `TopBar.cs` 文件头 ①）。
        /// 第 4 列（上限）判据：`campaignPoints` = **实拍读数 `300/2000`**（`资料/普查产出_1011/WA4_A370.md:70` ·
        /// `资料/历史/会话_2026-10-11_批次2.md:132` · `资料/索引与盘点/解包资源列表清单.md:125` 那句
        /// 「顶栏货币1(2000/2000)图标」）—— ⚠️ **不是从资产字段读来的**：`CampaignPoints` 那个 SO 本地导出里没有
        /// （全 `assets_full` 扫 `smallIcon` 只有 3 份 `Currency`，没有 `CampaignPoints`）。</summary>
        public static readonly CounterSpec[] CounterSpecs =
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

        /// <summary>同上，energy 那一颗的规格（🔴 **2026-10-17（B25）改 `public`**：`TopBar` 那条
        /// `Insert(0, EnergySpec)` 要用它 —— 建表在那边，规格在这里，⛔ 别两边各写一份）。</summary>
        public static readonly CounterSpec EnergySpec = new CounterSpec
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
        /// ⚠️ 返回值的来源是**我们自己的** `Wallet`（见 `TopBar.BuildResourcesBar` 那条免责）。</summary>
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

        // ---- 原版 prefab 的逐格几何（`Resources Bar / Resources Container / Resource Counter Item /
        //      Resource Bar Background / Icon / Resource QuantityText` 那一整串实读值）----
        // 🔴 **2026-10-17（B25）已整段搬进 `Shell/TopBar.cs`**（连同 `BuildResourcesBar` 的实现）。
        //    本文件**只留数据**：下面那几张表（`CounterSpecs` / `EnergySpec` / `DefaultCurrencies` /
        //    `OwnedOf` / `MaxOf` / `ShouldShow` / `CounterText` / `CurrencyNames`）与自检拨盘。
        //    ⛔ 别把几何常量写回本文件（那正是这次收口要消掉的那种「两份」）。

        /// <summary>币种 → 名字（**只给出声/日志用**，⛔ 不是判据）。
        /// 🔴 **2026-10-17（B25）改 `public`**：那一句「原版图标本地没查到」的**出声**跟着建表一起搬进了
        /// `Shell/TopBar.cs`（`BuildResourcesBar` 末尾），它要拿这份名字去打日志。</summary>
        public static string[] CurrencyNames(List<int> curs)
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

        // ---- 顶栏那块头像立绘（2026-09-27 建）—— 🔴 **2026-10-17（B25）实现搬进 `Shell/TopBar.cs`** ----
        //
        // 本文件只剩**两个口**（都是自检 / 运行时在用的 public API，**签名与语义一个字没改**）：
        //   · `RefreshTopAvatarIfChanged()` —— 每帧比一个 int（原版那条 `PlayerAvatarDataManager.OnAvatarChanged`
        //     事件的等价物）；批处理下 `Update` 不跑 ⇒ **自检直接调它**。
        //   · `TopAvatar` —— 自检读那一格（一张头像图都加载不到时是 `null`，**那不是「没建」**）。
        // 推导（为什么盒子比盾大 · 为什么必须保宽高比 · 队列为什么必须比边框高）**全文搬进了**
        // `Shell/TopBar.cs` 的 `BuildTopAvatar` / `BuildPlayerProfile`（判据与实现同处，⛔ 别在这儿再抄一份）。

        /// <summary>顶栏那一棵（`TopBar.Build` 建出来的那些件；`AvatarIndex` 的快照在 `Parts` 里）。</summary>
        TopBar.Parts _topBar;

        /// <summary>缺图登记（`TopBar` 那条路的回调 —— 与 `Art()` 用**同一张表**，不另开第二本账；
        /// ⚠️ 去重口径也照 `Art()`：同一个名字只记一次）。</summary>
        void NoteMissingArt(string n)
        {
            if (!string.IsNullOrEmpty(n) && !MissingArt.Contains(n)) MissingArt.Add(n);
        }

        /// <summary>玩家在档案窗改了头像 ⇒ 顶栏这一层跟着换（原版走 `PlayerAvatarDataManager.OnAvatarChanged`
        /// 那条事件；我们只有**一处**状态 `ProfileData.AvatarIndex`，**每帧比一个 int** 就够，
        /// 别为它另造一套事件机制）。没变就什么都不做。
        /// ⚠️ 批处理下 `Update` 不跑 ⇒ **自检直接调这个方法**（所以它是 public 的）。
        /// 🔴 **2026-10-17（B25）：本体已收进 `TopBar.RefreshTopAvatarIfChanged`** —— 卡组编辑那一屏调的是
        /// **同一个函数**（`DeckRuntime.UiRefreshTopAvatar`）；这里只转调，签名与语义不变。</summary>
        public void RefreshTopAvatarIfChanged() { TopBar.RefreshTopAvatarIfChanged(_topBar, NoteMissingArt); }

        /// <summary>🔴 **2026-10-25（H · 复刻第 2 轮 · P1）**：顶栏资源数字跟着 `Wallet` 刷（变了才写）。
        /// 判据与「为什么只刷字不重算药丸宽」→ `Shell/TopBar.RefreshCountersIfChanged` 的 doc。
        /// ⚠️ 批处理下 `Update` 不跑 ⇒ **自检直接调这个方法**（所以它是 public 的，与头像那条同口径）。</summary>
        public void RefreshTopBarCountersIfChanged() { TopBar.RefreshCountersIfChanged(_topBar); }

        void Update()
        {
            RefreshTopAvatarIfChanged();
            RefreshTopBarCountersIfChanged();
            // 🆕 2026-10-25：资源数字滚动（原版 `UIResourceCounter.SetQuantity(..., animated=1)` 的 0.5 s 递增）
            TopBar.StepCounters(_topBar, Time.deltaTime);
            TickNavPanelTween();
            RefreshDraftModeCard(); TickChatPreviewTween(); StepEventNavTimers();
        }

        /// <summary>自检用：顶栏那块立绘（**一张头像图都加载不到时会是 null** —— 那种情况按出厂态处理：只剩盾）。</summary>
        public ImageQuad TopAvatar { get { return _topBar != null ? _topBar.TopAvatar : null; } }

        // ---- §五 D：右侧聊天预览 ----
        void BuildChatPreview(Transform root)
        {
            // A332：原版 `ChatPreview` = 400 × 60（`anchor (1,1)` + `sizeDelta (400,60)` ⇒ 1475,85→1875,145）
            var p = New(root, "ChatPreview", 400f, 60f);
            _chatPreviewContainer = Rect(p, "Closed-Chat_background", 1474.7f, 1847.0f, 85f, 145f, "Container", QBarPanel);
            // 两条消息：位置**按 Container 的 VLG 算**（pad T/B=3 · L=15，两行等高 27）；
            // 字号照 §七 表二：`fontSize` **18**（**`auto=0`，不是自适应** —— 这一处别开 autosize）、`m_fontColor` 白
            var m1 = Text(p, "<color=#00FF20>Player Name:</color> Message", 1489.7f, 1817.0f, 88f, 115f, 5,
                          Color.white, "Message Preview", 18f, QBarText, face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-07（A77⑩ · 子表 E1/E2，调度台裁定选 (a)）**：原版 `Message Preview > text` 是
            //   **既不折行、也不自适应**（`MonoBehaviour_1795.json` 实读：`m_fontSize=18` · `m_enableAutoSizing=0`
            //   · **`m_TextWrappingMode=0`**）⇒ 照原版**关掉折行**。
            //   ⚠️ **订正（铁律 5）**：这行原来写「原版 **auto=0**，**只给折行宽**、不给自适应」—— 前半句对、
            //   **后半句错**：原版**没给**折行宽（它连折行都是关的），`SetWrapWidth` 是**我们主动加的**。
            //   现在留着的 `SetWrapWidth` 只为那一份**框宽**（327.3 与原版那个节点的宽一致），模式按原版关掉。
            if (m1 != null) { m1.SetWrapWidth(327.3f / 108f); m1.SetWrapping(false); _chatPreviewRow1 = m1; }
            var m2 = Text(p, "<color=#00FF20>Player Name:</color> Message", 1489.7f, 1817.0f, 115f, 142f, 5,
                          Color.white, "Message Preview (1)", 18f, QBarText, face: TmpFont.Face.LatinCardBody);
            // 第 2 行同上（判据 = `MonoBehaviour_1814.json`，同值）。
            if (m2 != null) { m2.SetWrapWidth(327.3f / 108f); m2.SetWrapping(false); _chatPreviewRow2 = m2; }
            var chatIcon = Rect(p, "40K_icon_menu_chat", 1811.0f, 1879.0f, 81.8f, 148.3f, "Button", QBarContent); _chatPreviewIcon = chatIcon != null ? chatIcon.transform : null;
            // 🆕 2026-09-27：这颗钮**原来没接点击**（红线：不许静默失败）—— 接上，开聊天窗。
            // 原版那条链：`ChatPreview.Initialize` 把 `chatButton.onClick` 挂 `OpenChat` →
            // `WindowsManager.OpenWindow(chatWindow)`（判据 → `多人界面_入口与调用.md` §②）。
            MenuDraw.Hit(p, "ChatHit", new PxRect(1811.0f, 81.8f, 1879.0f, 148.3f), QBarOverlay, ChatPreviewClicked);
            _chatPreview = p;
            // ============================================================ 🔴 `A1639`（2026-10-12 用户真机点名）
            // **出厂【关上】** —— 原版这一块是「**有聊天消息才画**」，既不是「永远画」、也不是「只在主菜单画」。
            //
            // **判据（`d:/2/tools/decomp_full/` 逐句读出来的，四条互相独立）**：
            //   ① `ChatPreview__OnBeforeInitialization.c`：`icon.localScale = (0,0,0)` ·
            //      `messageContainer.fillAmount = 0` · 每一条 `ChatPreviewMessage` 的正文 `set_enabled(false)`
            //      ⇒ **出厂视觉上就是不可见的**；
            //      ⚠️ 那颗 GO 在场景里 **`m_IsActive = true`**（`GameObject/ChatPreview.json` 实读）
            //      —— 原版**不是**靠 `SetActive(false)` 按掉它，而是靠「缩放 0 + 填充 0」；
            //   ② `ChatPreview__Show.c`：`SetActive(true)` + `DOScale(icon, 1).From(0)` +
            //      `DOFillAmount(container, 1).From(0)` + `DOFade(container, 1).From(0)`
            //      ⇒ `Show()` 的语义就是「**从看不见淡进来**」；
            //   ③ `Show()` **全库只有两个调用点**：`__OnInitializationComplete.c`（**仅当** `receivedMessages` 已为真）
            //      与 `__WaitAndAnimate.c`（**仅当该频道来了 ≥1 条消息**，且先 `remove_OnChatUpdate` 再 `Show`）；
            //      而 `__UpdatePreviews.c` 里那个 `receivedMessages`（字段 `0x40`）**只在 `messages.Count >= 1`
            //      且频道匹配时**才置 1 ⇒ **没有消息 ⇒ 永远走不到 `Show()`**；
            //   ④ `ChatPreviewMessage__Start.c`：每条预览行**在 `Start()` 里自己 `set_enabled(false)`**（同①）；
            //   ⑤ 🔴 **读了就收起来**：`ChatPreview__OpenChat.c` + 它那条 `OnComplete` 回调
            //      （`ChatPreview__<OpenChat>b__10_0.c`）—— 先把预览**动画收回去**
            //      （`DOScale(icon, 0)` / `DOFillAmount(container, 0)` / `DOFade(container, 0)`），
            //      回调里**第一句就是 `gameObject.SetActive(false)`**，然后才 `WindowsManager.OpenWindow(chatWindow)`。
            //   ⇒ **结论 = 「有消息才画，读了就收」**（既不是「只在主菜单画」、也不是「永远画」）。
            //      🔑 **这一条同时解释了用户那两张实拍为什么不一样**（`主界面参考图.png` 23:30 有、
            //      `收藏—卡组的参考图.png` 23:31 没有，**只差一分钟**）：那 1 分钟里他**点了一下**那块预览
            //      （= 去读了那条消息）⇒ 预览自己 `SetActive(false)` 了，与「收藏窗盖住它」无关。
            //      ⚠️ 顺带否掉一个**很自然的错觉**：「开别的窗它就没了」—— **原版没有这条逻辑**：
            //      `ChatPreview` 是 `Safe area Only Horizontal` 的**最后一个孩子**（兄弟序在
            //      `1 - Below Upper Bar Holder` / `2 - Canvas Holder Above upper bar` / `3 - PopUp Holder`
            //      **三个 Holder 之后**）⇒ 结构上它**压在所有窗之上**（逐级解 `m_Father` + 兄弟序实读），
            //      而且 `bundle_menus_assets_all` 里 8 个 `m_OverrideSorting = 1` 的 Canvas
            //      **一个都不属于收藏窗** ⇒ 窗也盖不住它。⛔ 别照「开窗就藏它」去实现（那是我们发明的口径）。
            //
            // ⇒ **照原版 = 我们今天不该画它**：本仓**没有聊天数据源**（用户 2026-09-26 口径「网络聊天功能暂时不做」·
            //   `Shell/ChatPanel.cs` 文件头 ⑤：没有服务器、也没有 `ChatGlobalManager` 的对等物）
            //   ⇒ 「没有消息」是恒真的 ⇒ 它**永远不该出现**。而它恒亮着的时候，那两条占位串正好压住
            //   收藏窗顶部「导入卡组 / 创建卡组」之间那道缝（旧账 `A1553`）。
            //
            // ⚠️ **这不是「把件删掉」**：原版那一棵**结构在、GO 也是 active 的**；我们只是照原版
            //   「没有消息就不可见」把它按下去。**唯一差别**（如实标，铁律 3）：原版靠 `scale/fill` 那一套 tween，
            //   本仓改用 `activeSelf` —— 理由两条：本仓没有 DOTween 那套淡入；`ImageQuad` 也没有 `enabled` 那一档
            //   （禁组件在画面上什么都不做）。
            // 🔴 **唯一能把它点回来的口 = `SetChatPreviewVisible(bool)`**（紧跟本方法的那一个）——
            //   ⛔ 别在别处再写一句 `SetActive`（两处写同一条规则 = 迟早不一致）。
            SetChatPreviewVisible(false); UpdateChatPreviews();
        }

        /// <summary>点了那块聊天预览 —— 本仓的 `ChatPreview.OpenChat()`。
        /// <para>原版那一条分两拍（`ChatPreview__OpenChat.c` + 它的 `OnComplete` 回调
        /// `ChatPreview__&lt;OpenChat&gt;b__10_0.c`，逐句读出来的）：
        /// ① 把预览**动画收回去**（`DOScale(icon, 0)` / `DOFillAmount(container, 0)` / `DOFade(container, 0)`）；
        /// ② 回调**第一句 `gameObject.SetActive(false)`**，然后 `WindowsManager.OpenWindow(chatWindow)`。</para>
        /// <para>⚠️ 我们没有 DOTween 那条 tween ⇒ 直接落成「**先收起、再开窗**」（顺序照原版；
        /// 第一拍那个「动画」的**终态**就是第二拍那句 `SetActive(false)`，本仓只保留终态）。</para></summary>
        void ChatPreviewClicked()
        {
            SetChatPreviewVisible(false);      // = 原版 `OnComplete` 里的第一句 `SetActive(false)`
            OpenChat();                        // = 原版 `OnComplete` 里紧接着的 `WindowsManager.OpenWindow(chatWindow)`
        }

        /// <summary>`ChatPreview`（主菜单右上那块）—— 只用来做显隐（`A1639`）。</summary>
        Transform _chatPreview;

        /// <summary>主菜单那块「聊天预览」的**唯一显隐口** = 本仓的 `ChatPreview.Show()`。
        /// 出厂**关**，判据（四条）→ `BuildChatPreview` 末尾那一段。
        /// <para>⚠️ 我们没有聊天消息源 ⇒ 今天**没有任何生产调用点**；留着它是因为「关」这件事必须有一个
        /// **唯一的开关**（自检要验状态，见 `Editor/MainMenuScene.cs` 的 `A1639` 那一条），
        /// 而不是散在各处的 `SetActive`。将来真接了聊天源，在那条「收到 ≥1 条消息」的钩子上调它即可。</para></summary>
        public void SetChatPreviewVisible(bool on)
        {
            if (_chatPreview == null) return;
            if (_chatPreview.gameObject.activeSelf != on) _chatPreview.gameObject.SetActive(on);
        }

        /// <summary>那块「聊天预览」现在**看得见没有**（自检用）。
        /// ⚠️ ⛔ **不许拿「节点找得到」当可见** —— `Find` / `GetComponentsInChildren(true)` **含 inactive**，
        /// 那样写两种状态下都会绿（本会话已经踩过）⇒ 判据只能是 `activeSelf`。</summary>
        public bool ChatPreviewVisible { get { return _chatPreview != null && _chatPreview.gameObject.activeSelf; } }

        // ---- §五 E + §九：模式卡区 ----
        /// <summary>格子的列起点与步进（§二·4 实证：`itemSize` 535×414.4 · `spacing` (20,20) · `pad L38`）⇒ **列 c 左 = 205 + 555c**。</summary>
        const float CardCol0 = 205f, CardColStep = 555f, CardW = 535f, CardH = 414.4f;
        /// <summary>**1×2 档**的卡高 = `2 × 414.4 + 20`（两行 + 一行间距）= **848.8**（判据 = §二·4 的
        /// `FlexibleGridLayout rows=2 columns=20 itemSize 535×414.4 spacing (20,20)` ⇒ 1×2 跨两行）。
        /// 🔴 **2026-10-11（界面复刻）**：**五张模式卡一律用这一档。** 理由两条 ——
        ///   ① **用户 2026-10-11 点名的缺陷原话**：「原版是**满高大立绘卡、三张等大对齐**；我们是灰底板 +
        ///      中间一小块图，且**尺寸基线不齐**、中间那张下坠、第四张被切」；
        ///   ② **参考图实测**（`C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png`，1568×950）：
        ///      三张可见的卡 y `150..880`（**730/950 × 1080 = 830**）、宽 `420/1568 × 1920 = 514`
        ///      ⇒ **514×830 ≈ 535×848.8 = 1×2** ✓，且第四张在右缘被切（**原版就是横滚、切是对的**）。
        /// ⚠️ **原版「哪个模式用 1×1 / 哪个用 1×2」是 liveop 服务端数据**（本地查不到，§九 9·3 明写
        /// 🔶 自己设计（非原版）：五张模式卡统一取 1×2 档 · 原版哪个模式用哪档是 liveop 服务端数据 · TODO(谁定的)｜原注：「别自己编一套映射」）⇒ 我们这五张选哪一档**本来就是我们自己定的**；选 1×2 是为了与实拍同形。
        /// ⛔ `CardH`（1×1）**别删** —— 原版那一档真实存在（`Reward Event Container 1x1` / `1x1 Claim`），
        /// `BuildModeCard` 两条路都留着，将来接 liveop 数据时要按数据选档。</summary>
        const float CardHTall = 848.8f;
        /// <summary>**行 y（1920×1080 设计坐标）= 182.8**。
        /// 🔴 **2026-10-19（`A1635` · 铁律 5 就地订正）**：原文写「行 y 是我们挑的 / 这一格没有原版出处」——
        /// **已过期**：182.8 现在**能从原版 prefab 字段 + uGUI 布局语义推出来**，并被用户实拍复核：
        ///   · `Content`（RT 1107）：anchor y `0..0.936`、sizeDelta.y `1.449`、pivot `(0,.5)`、apos.y `0.724`
        ///     ⇒ 1920×1080 下矩形 **134.42..1080（高 945.58）**（§五 E）；
        ///   · `FlexibleGridLayout`（MB 1961，挂在 `Content` 上）：`rows=2 · columns=20 · itemSize 535×414.4 ·
        ///     spacing (20,20) · padding T0/B0 · m_ChildAlignment=3（MiddleLeft）`
        ///     ⇒ 网格自然高 `2×414.4+20 = 848.8`，**在 Content 里垂直居中**；
        ///   · ⇒ `row0 top = 134.42 + (945.58 − 848.8) / 2 = 182.81`。
        ///   · 另一个候选 **134.4** 要求「网格顶对齐 / Content 压成自然高」，与 MB1961 的 `MiddleLeft` 矛盾。
        /// 🔴 **实拍复核**（`C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png`，1503×883，用户 2026-10-17 确认
        /// 为关服前最后的主界面）：横向锚点 = 四列卡左沿 218.5/629.5/1040.5/1452.5 ↔ 设计 205/760/1315/1870
        /// ⇒ 缩放 **0.7405**、设计 x=0 ↔ 屏 x≈66.6（左侧黑边外）；导航标签 5 行间距 113.5 ↔ 设计 153.33
        /// ⇒ 纵向缩放 **0.740**、设计 y=0 ↔ 屏 y≈0（上下无黑边；底部那条是游戏自己的暗红背景 RGB≈(34,1.5,4.4)，不是黑边）。
        /// 实拍里卡顶 y≈149、卡底（亮边）y≈845、col3 两张 1×1 的分界 y≈490/505。
        /// ⚠️ **实拍的游戏窗口不是 16:9**（≈1430×883，aspect≈1.62）：原版 `AdaptFlexibleGridLayoutByAspectRatio.ModifyFlexibleLayout`
        /// 会把 `itemSize.y` 乘 `max(1, (16/9)/aspect)`（反编译；常量 `1.7777778`/`1.0` 读自 GameAssembly.dll）
        /// ⇒ 实拍下 itemH≈454.9、网格高≈929.8、中心对齐预测 row0 top≈148.5px（实测 149）、卡底≈844.6（实测≈845）
        /// ⇒ **模型与实拍逐项吻合**；回到 16:9 即上式的 **182.8**。
        /// ⚠️ 我们固定用 1920×1080 这一档（16:9）：非 16:9 时原版绝对行 y 会随 aspect 变，本常量不跟着变。</summary>
        const float CardRow0Top = 182.8f;

        // ============================================================ 🔴 2026-10-17（界面线 T12）：模式卡横向滚动
        //
        // 原版 `GameModes` 是一个 **`ScrollRect(horizontal=1, vertical=0, movementType=Elastic,
        // elasticity 0.1, decelerationRate 0.135)`**，`Content` 自然宽 **11134px**（20 列；
        // `资料/主菜单_原版规格.md:173,415`），而视口宽只有 **1752.96**（`GameModes` 与 `Viewport` 同矩形）。
        // 我们摆了 4 列（内容右端 **2405**）⇒ 第 4 列有 **485px 在屏外、滚轮/拖拽都到不了**
        // （`U1` 普查判 高；「练习/教学」那一列 90% 看不见）。
        //
        // 落地 = 现成的 `MenuScroll`（全壳唯一一份滚动实现；横向 + 惯性/回弹/拖拽都已有）：
        //   · `Viewport` = `GameModes` 的矩形 **x[167.04, 1920] y[71.33, 1080]**（`ModesW/H` 反推）；
        //   · 内容两端 = `CardCol0`（205）→ 最后一列右缘 **2405**；
        //   · 偏移变了 ⇒ **重建模式卡**（`OnChanged = RebuildModeCards`），与 `ForgeTab` 那两处同一条路。
        // ⚠️ **裁切**：本文件**没有 `ViewportClip` 节点**（那套要求节点自己带矩形位置，而本文件的树
        //   全部停在原点、拿绝对 px 当 localPosition）⇒ 裁切**按 `MenuDraw` 的显式 `clip` 形参**传
        //   （`Nine` / `ClipText` 都吃），卡图则用 **UV 窗收缩**（与它现有的「UV 裁到卡面」同一套算法）。
        static readonly PxRect ModeViewport = new PxRect(167.04f, 71.33f, 1920f, 1080f);
        /// <summary>内容右端（= 第 4 列右缘 `205 + 3×555 + 535`）。</summary>
        const float ModeContentX2 = CardCol0 + 3 * CardColStep + CardW;

        Transform _modeContent;
        MenuScroll _modeScroll;
        float _modeOffsetPx;                     // 当前滚到的偏移（设计 px）
        System.Action _placeAllCards;            // 第一次摆放与滚动重建共用这一份

        // ============================================================ 🆕 2026-10-24（`E10`）：轮抽模式卡的 `State` 条
        // 原版那一件 = `Draft Game Mode Container 1x2/State`（`DraftMenuContainer.stateGO` + `victoriesCounter`），
        // 由 `DraftMenuContainer.OnDraftStateChange → DoOperationsOnDraftState` 在**每次轮抽状态变化**时刷新。
        // 我们这边没有那套事件：数据在 `DraftModeWindow` 的静态一局存档里 ⇒ **主菜单每帧比一次**
        // （`RefreshDraftModeCard`，见 `Update()`）—— 这就是「回壳后不必再点一次模式卡」的那条修复。
        Transform _draftStateNode;               // 最近建出来的那张轮抽卡的 `State`
        Label _draftVictories;                   // 里面那颗 `Victories` 字（`{胜场}/{轮数}`）
        PxRect _draftVicTxR;                     // 它那一格的矩形（换字之后要重新左对齐）
        bool _draftStateShown;                   // 上次应用过的可见性（省得每帧重设）
        int _draftWinsShown = -1;                // 上次应用过的胜场

        /// <summary>自检读口：轮抽模式卡的 `State` 节点（没建 ⇒ `null`）。</summary>
        public Transform DraftStateNode { get { return _draftStateNode; } }
        /// <summary>自检读口：`State` 里那颗 `Victories` 字。</summary>
        public Label DraftVictoriesLabel { get { return _draftVictories; } }
        /// <summary>自检读口：`State` 条**现在是不是显示着**（= 原版那一格的 `activeSelf`）。</summary>
        public bool DraftStateVisible { get { return _draftStateNode != null && _draftStateNode.gameObject.activeSelf; } }
        /// <summary>自检读口：`Victories` 那颗字现在的文本（没建 ⇒ `null`）。</summary>
        public string DraftVictoriesText { get { return _draftVictories != null ? _draftVictories.Text : null; } }

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
            // 🔴 **2026-10-17（A853 收口）**：这一格原来传的是 `null` ⇒ 第六步**不建 `Hit`**⇒ 点不动。
            //    原版点这张卡 = 开「教程模式窗」（`Tutorial Mode Menu` / `TutorialModePopup : GameWindow`）——
            //    链路 = `LiveopsMenuContainer.OnClick()` → `LiveOpsEvent.OpenWindow()` → 按**事件数据**里的
            //    `drawData.EventComponents[MainWindow]` 开窗；**全游戏唯一**消费 `TutorialEvent` 的窗就是它。
            //    （用户 2026-10-17 拍板走 A：接 `"tutorial"` ⇒ 开那扇窗，⛔ 不是只弹一句「暂无服务器」。）
            //    ⚠️ 教程事件那份数据**本地任何形态都没有**（随 PlayFab userInfo 下发）⇒ 原版关服时这张卡
            //    根本不存在；受影响的两处（标题栏文案 / 各关的副标题与描述）在 `TutorialModePopup` 里逐条标了。
            // ============================================================ 🆕 **2026-10-11（界面复刻）：
            //   【两行网格排布】—— 原来五张卡**一律摆在 `CardRow0Top` 那一行**上，`rows=2` 的第 2 行永远空着。
            // 🔴 **判据（参考图放大实测，不是推断）**：`C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png`
            //   （1503×883）**右缘那张被切的卡**放大看清了 —— 那是**两张 1×1 上下叠在一列**（上「练习」、下「教学」，
            //   两张之间有一条 20px 的缝）⇒ **原版就是 `FlexibleGridLayout rows=2` 的两行排布**：
            //   **1×2 独占一整列；1×1 一列放两张**（上 = row0、下 = row1）。
            //   ⇒ 与 §二·4 那条 `rows=2 columns=20 itemSize 535×414.4 spacing (20,20)` 完全自洽
            //     （`848.8 = 2×414.4 + 20` 也正好是「两行 + 一道缝」）。
            // ⚠️ **别再退回「一律 1×2」** —— 我 2026-10-11 先试过那一版（以为参考图三张等大 ⇒ 全 1×2），
            //   **被参考图自己推翻**：那张图里 **1×2 与「两张叠起来的 1×1」是同时存在的**。
            //   ⇒ `CardH`（1×1）这一档**必须留着**，两张都走。
            // 排布规则照 uGUI `FlexibleGridLayout`：**逐张填格子**；1×2 吃完一整列并复位到 row0。
            // ⚠️ 我们这五张**哪张是 1×1 / 哪张是 1×2**【部分】是我们定的**（原版由 liveop 服务端数据决定，
            //   本地查不到 ⇒ §九 9·3 明写「别自己编一套映射」，但本地入口本来也只能我们自己定，
            //   见下面那两段老注释）。**有三条有原版出处（prefab 名）**：`Draft Game Mode Container **1x2**` ·
            //   `Ranked Game Mode Container **1x2**` · `Skirmish Ranked Game Mode Container **1x2**`
            //   （🔴 **2026-10-11（A1521⑦）就地订正**：这一段原来写「只有 `Draft` 那一条有原版出处」——
            //   现读 `bundle_menus_assets_all/GameObject/` 三个名字逐字如此 ⇒ 那句话已过期）；
            //   剩下 `Tutorial` / `Practice` 两张 `1×1` 是我们按 `Base Game Mode Container 1x1` 那一族取的。
            // ---- 排布：**逐张填格子（first-fit）· 不留洞** ----
            // 🔴 **2026-10-17（界面线 `T12`）：整段搬进 `_placeAllCards` 委托** —— 横向滚动靠「偏移变了重建内容」
            //   （`MenuScroll.OnChanged`），而**第一次摆放与滚动重建必须是同一份代码**（两份迟早不一致）。
            //   `used` 因此从外面搬进来：每次调用都要一张新表。
            _modeContent = content;
            _modeOffsetPx = 0f;
            _placeAllCards = () =>
            {
            // 🆕 2026-10-24（E10）：每次重排都会把卡全销毁重建 ⇒ 先把 `State` 条那两个句柄清掉
            // （否则滚动后 `RefreshDraftModeCard` 会去改一棵已经销毁的树）。
            _draftStateNode = null; _draftVictories = null; _draftWinsShown = -1;
            const float ColStep = CardColStep, RowStep = CardH + 20f;   // 行距 = 卡高 + `spacing (20,20)` 的 20
            const int MaxCols = 8;                                      // 本菜单最多摆 8 列（现在用满 4 列）
            var used = new bool[MaxCols, 2];                            // [列, 行] 这一格占没占
            // 🔴 **2026-10-11（A1521⑦）排布从「手写死列/行号」改成【按卡片顺序 first-fit】**：
            //   `1×2` 找**整列都空的**列（一次占满上下两格）· `1×1` 找**还有空格的**列（同列内先 row0 后 row1）；
            //   **已有列遍历完才开新列** ⇒ **不会留洞**。
            //   🧨 **改坏法（就是这一轮的起因）**：把某个 `1×1` 手写到一个新列上 ⇒ 前一列下半格空着
            //   （`Tutorial` 那一列原来就是：它独占 col0/row0、**row1 永远是空的**，
            //   而用户参考图 `C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png` 里**看不出这种洞**）。
            //   ⚠️ **原版真正的换列规则【没读到】**（原版 `GameModes/Content` 出厂 0 子、卡由 **liveop 事件数据**
            //   在运行时实例化；`FlexibleGridLayout` 本地只拿到 `rows=2 columns=20 itemSize 535×414.4 spacing (20,20)`
            //   这一组字段）⇒ **这一条是照用户参考图排的**（那上面看不出洞），**⛔ 别写成「原版就是这样」**。
            //   ⚠️ 而且五张卡**各自的尺寸档是「这次活动的配置」、不是模式的固有属性** —— 旁证：同一个 `Practice`
            //   的 `Base Game Mode Container 1x1` 与 `Base Game Mode Container 1x2` **两个 prefab 都在**
            //   （`bundle_menus_assets_all/GameObject/`）⇒ ⛔ 别把「某个模式 = 某个尺寸」写进任何表。
            System.Action<string, string, string, float, string> Put = (nm, art, title, h, kind) =>
            {
                bool tall = h > CardH + 1f;                  // `1×2` = `CardHTall`（848.8）· `1×1` = `CardH`（414.4）
                int col = -1, row = 0;
                for (int i = 0; i < MaxCols && col < 0; i++)
                {
                    if (tall) { if (!used[i, 0] && !used[i, 1]) { col = i; row = 0; } }
                    else if (!used[i, 0]) { col = i; row = 0; }
                    else if (!used[i, 1]) { col = i; row = 1; }
                }
                if (col < 0)                                 // 排不下就**出声**（红线：不许静默）
                {
                    Debug.LogWarning("[Menu] 模式卡 `" + nm + "` 在这张网格里排不下（最多 " + MaxCols + " 列）");
                    return;
                }
                used[col, row] = true;
                if (tall) used[col, 1] = true;
                BuildModeCardForCard(content, nm, CardCol0 + col * ColStep - _modeOffsetPx, CardRow0Top + row * RowStep,
                                     CardW, h, art, title, kind);   // 🆕 B89-E27（`L411`/`L415`）：按「这一格归哪个在期活动」喂标题+到期时刻（见类尾同名方法）
            };
            // 🔴 **2026-10-13 就地改序（铁律 5，取代 `A1522⑤` 那一版）：五张卡的 `Put` 次序按【用户原版实拍】重排。**
            //   判据 = 用户原版客户端实拍 `C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png`（1503×883），
            //   逐列放大核过（放大件 `d:/4/_tmp_view/refcrop/` —— ⚠️ `_tmp_view` 在 `.gitignore` 里、是**临时件**，
            //   可复查的坐标是上面那张实拍本身）：
            //     · col0 = **小冲突**（`40k_main_GameMode_Skirmish`）
            //     · col1 = `Container_Image_Ranked`（与我们的那张**一模一样**，已并排比过）
            //     · col2 = `Container_Image_Draft`（同上）
            //     · col3 = **两张 `1×1` 上下叠**（上「练习」· 下「教学」，中间一道 20px 缝）
            //   ⇒ **原版次序 = 小冲突 · 排位 · 选秀 · 练习 · 教学** —— 三张 `1×2` 在前、两张 `1×1` 在最后一列。
            //   ⚠️ **本条取代 `A1522⑤`**：那一版只把 `Practice`/`Tutorial` 的**上下**换了，三张 `1×2` 的列位
            //     仍与实拍不符（那一版画出来是 col0「练习/教学」· col1 Draft · col2 Skirmish · col3 Ranked）
            //     —— 错因 = 只照那张图**右缘那一列**读出来的次序改，没核左起三列。
            //   ✅ **判据自洽（不跑引擎就能核）**：本函数 first-fit 逐张填格 ⇒ 按上面次序 `Put`，
            //     三张 `1×2` 各独占 col0/col1/col2，两张 `1×1` first-fit 落到 **col3 row0/row1**
            //     ⇒ 与实拍**逐列相同**。⛔ **不要动 first-fit 算法**。
            //   🧨 **改坏法**：把任意两个 `Put` 换回去（或把某个 `1×2` 改档）⇒ 列位变更、
            //     `Editor/MainMenuScene.cs` 那五条 `CheckCard`（**逐卡钉死 x / 高 / 行顶**）里对应的会红。
            Put("Base Game Mode Container 1x2 - Skirmish", "40k_main_GameMode_Skirmish",
                Loc.T("MainMenu/GameModes/Skirmish"), CardHTall, "skirmish");                   // col0（1×2 · 独占整列）
            Put("Base Game Mode Container 1x2 - Ranked", "Container_Image_Ranked",
                Loc.T("MainMenu/GameModes/Ranked"), CardHTall, "ranked");                       // col1（1×2 · 独占整列）
            // 🔴 **2026-10-17（A789 收口 · 用户拍板「按建议执行」）**：这一格原来传的是 `null` ⇒
            //    `BuildModeCard` 的第六步**不建 `Hit`**（命中区只在 `modeKind` 非空时建）⇒ **这张卡点不动**，
            //    而 2026-09-22 裁决（`资料/阶段二外壳_待裁决清单_0922.md:29`，选项 (b)）要的正是
            //    「**只做模式卡 + 点了如实提示「暂无服务器」**」—— 前半句（卡）早在 09-24 就建好了，
            //    **缺的是后半句**。现补 `"draft"` 让 `OpenMode` 接住（见那里的 `case "draft"`）。
            //    ⚠️ 当年 A789 之所以挂起，是因为候选名 `Menu Demo [3740]` 被当成了 PathID 去找 ——
            //    它其实是 `资料/说明书/04_界面UI/菜单全树.md` 的**行号**（且已漂到 `:3750`），
            //    而那张「模式卡」的真身是 `Draft Game Mode Container 1x2` —— **本行这一件**。
            Put("Draft Game Mode Container 1x2", "Container_Image_Draft",
                Loc.T("MainMenu/GameModes/Draft"), CardHTall, "draft");                         // col2（1×2 · 独占整列）
            // 🆕 **2026-09-24 用户拍板：模式卡就是「进对应模式界面」的入口** ——
            //    「是直接点击这些卡片，然后就进去这些对应模式的界面的」。
            // 🔴 **这五张的「模式 → 卡图 → 窗」映射是我们定的，不是复刻**：
            //    原版这张映射在 **liveop 服务端数据**里（`资料/主菜单_原版规格.md` §9·3 明写
            //    「本地 `?（查不到）`（**别自己编一套映射**）」）；而**主菜单在原版里根本没有 PLAY 钮**
            //    （只有 Home/Social/Rewards/Collection/Shop 五个导航钮）⇒ 本地入口**只能我们自己定**。
            //    卡图按名字对：练习 = `Container Image Practice 1x1` · 教学 = `Container Image Tutorial` ·
            //    排位 = `Container Image Ranked` · 选秀 = `Container Image Draft` ·
            //    遭遇战**没有专属卡图**（`Practice 1x2` 也是练习的）⇒ 用全族唯一带 Skirmish 字样的
            //    `40k_main_GameMode_Skirmish`。逐条出处见 `资料/阶段二_战斗入口_原版规格.md`。
            // 🔴 **2026-10-11（A1521⑦）尺寸档 `1×1` → `1×2`**（判据 = 原版 **prefab 名**
            //   `Skirmish Ranked Game Mode Container 1x2` / `Ranked Game Mode Container 1x2` /
            //   `Draft Game Mode Container 1x2` —— `bundle_menus_assets_all/GameObject/` 里逐字如此；
            //   同族还有 `Base Game Mode Container 1x2`）⇒ 三张 `1×2` 各**独占一整列**。
            //   **第二条独立理由（结构上的）**：「不留洞」要求下，卡片 5 张、其中 `1×1` 有 2 张 ⇒
            //   若三张 `1×2` 里任一张退回 `1×1`，总格数变**奇数**，两行网格里**必然留一个洞**
            //   ⇒ 「尺寸档照 prefab 名」与「不留洞」两条判据**同时指向 `1×2`**。
            //   ⚠️ 这是**改名**（`1x1 → 1x2`，节点名与尺寸档同步）、不是只改尺寸；
            //   `Tutorial` / `Practice` 这两张 `1×1` 是**我们按 `Base Game Mode Container 1x1` 那一族取的**
            //   （原版「哪个模式用哪一档」由 liveop 服务端数据决定、本地查不到）。
            // ⚠️ **这两张的上下次序 = 用户参考图实拍**（那上面右缘一列「**练习在上、教学在下**」、
            //   中间一道 20px 缝）—— ⛔ 别把两个 `Put` 换回去：换了 `CheckCard` 那两条行顶期望值就会红。
            Put("Base Game Mode Container 1x1 - Practice", "Container_Image_Practice_1x1",
                Loc.T("MainMenu/GameModes/Practice"), CardH, "practice");                       // col3 / row0（1×1 · 上）
            Put("Base Game Mode Container 1x1 - Tutorial", "Container_Image_Tutorial",
                Loc.T("MainMenu/GameModes/Tutorial"), CardH, "tutorial");                       // col3 / row1（1×1 · 下）
            };                                   // ← `_placeAllCards` 委托结束
            _placeAllCards();                    // 第一次摆放

            // ---- 🆕 2026-10-17（界面线 T12）：横向滚动（原版那条 `ScrollRect` 的等价物）----
            //   判据 = `主菜单_原版规格.md:173`（horizontal=1 / vertical=0 / Elastic / .1 / .135）
            //   ＋ `:415`（Content 20 列、自然宽 11134px）。
            //   ⚠️ 我们的内容只有 4 列（2405），所以能滚的只有 **485px** —— 那正是「练习/教学那一列
            //   在屏外」这一条缺陷的宽度（第 4 列 x 1870..2405，屏宽 1920）。
            // 🔴 **2026-10-17（`N29`）**：原版 `MainMenu` 的 `Safe area All/GameModes` ScrollRect
            //   `m_ScrollSensitivity = **1.0**`（`bundle_scenes_scenes_mainmenuwarpforge` 现读）。
            // 🔴🔴 **2026-10-18（`N66`）就地改：最后一参 `1f → 120f` —— 这个 120 是【我们挑的】，不是原版值。**
            //   原版那条是 **horizontal-only**（`m_Horizontal = 1 / m_Vertical = 0`）⇒ **纵向滚轮在原版
            //   本来就不参与**（uGUI `ScrollRect.OnScroll` 只把量喂给「允许的那根轴」）⇒ 原版这一段靠**拖拽**；
            //   `m_ScrollSensitivity = 1.0` 是**横向**的灵敏度，⛔ 不是我们这条列表「一格滚轮走多少」的判据。
            //   照 1.0 传进来的后果：`MenuScroll.NotchK = 1 / 120` ⇒ 一格滚轮（`WHEEL_DELTA = 120`）只走 **1 px**，
            //   滚完整个行程要 **几百格**（实测等于「滚不动」）。
            //   ⇒ 给一个可用步长 **120 px / 格**（= 一屏 1080 的 1/9）：本文件几何的行程 =
            //     `ModeContentX2 − ModeViewport.x2` = 2405 − 1920 = **485 px** ⇒ **485 ÷ 120 ≈ 4 格到底**
            //     （⚠️ 派单里写「447 px」与这里按 `ModeContentX2` 算出的 485 差 38 px —— 按本文件实际几何记 485，
            //      两者都 ≈ 4 格，不影响这条改动）。
            //   ⚠️ **拖拽一点没动**（`MenuScroll.BeginDrag / DragTo` + `PointerLayer` 那条 10px 阈值照旧）；
            //      ⛔ 别把这个 120 写成「原版 `m_ScrollSensitivity`」—— 原版值是 1.0、且那是横向的。
            _modeScroll = new MenuScroll(ModeViewport, CardCol0, ModeContentX2, 120f);
            _modeScroll.Owner = gameObject;
            _modeScroll.OnChanged = RebuildModeCards;
            PointerLayer.RegisterScroll(_modeScroll);
        }

        /// <summary>滚动偏移变了 ⇒ 重建模式卡（`MenuScroll.OnChanged` 的约定；与 `ForgeTab` 那两处同一条路）。
        /// ⚠️ 只重建 `Content` 的子件 —— 页面 / 视口节点一个都不动（`MainMenuScene` 有引用它们后面的断言）。</summary>
        void RebuildModeCards()
        {
            if (_modeContent == null || _placeAllCards == null) return;
            for (int i = _modeContent.childCount - 1; i >= 0; i--) DestroySafe(_modeContent.GetChild(i).gameObject);
            _modeOffsetPx = _modeScroll != null ? _modeScroll.Offset : 0f;
            _placeAllCards();
        }

        /// <summary>自检用：模式卡滚动区（批处理没有滚轮事件 ⇒ 直调 `Wheel/ScrollBy`，与真滚同一条）。
        /// ⚠️ 滚动会**重建**卡节点 ⇒ 取 `CardTransformAt` 之类引用之前要用最新的。</summary>
        public MenuScroll ModeScroll { get { return _modeScroll; } }

        /// <summary>🆕 **2026-10-25（菜单 C）**：原版 `MainMenuWindow.ToggleScroll(bool)`（`:388`）——
        /// 直接 `scrollList.enabled = option`（`MainMenuWindow__ToggleScroll.c`）。
        /// 我们的等价物 = `MenuScroll.Enabled`（`PointerLayer` 的命中/滚轮/惯性都看它）。</summary>
        public void ToggleScroll(bool option)
        {
            if (_modeScroll != null) _modeScroll.Enabled = option;
        }

        /// <summary>自检读口：主菜单横向滚动区当前是否可滚。</summary>
        public bool ScrollEnabledForTest { get { return _modeScroll != null && _modeScroll.Enabled; } }

        /// <summary>模式卡点下去 = **进对应模式的界面**（用户 2026-09-24 拍板的那条路）。
        /// ⚠️ 映射是**我们定的**（见 `BuildGameModes` 上方那段）；窗还没建的那两个**出声**，不静默。</summary>
        void OpenMode(string kind)
        {
            var wm = WindowsManager.Get();     // 🔴 2026-10-11：走**统一读口**（原来直接读 `Instance` ⇒ 切场景后它是 null ⇒ 模式卡点不动）
            if (wm == null) { Debug.LogWarning("[Menu] 没有 `WindowsManager`，开不了模式窗：" + kind); return; }
            switch (kind)
            {
                // 🆕 2026-10-17（A853 收口）：**这是唯一一处【真的按原版开窗】的模式卡** ——
                //   原版这张卡开哪扇窗由 liveop 事件数据给，但「Tutorial 开的就是 `Tutorial Mode Menu`」
                //   这一条**全游戏唯一**（唯一消费 `TutorialEvent` 的窗），所以它不是我们编的映射。
                //   ⚠️ 窗里那颗 `Play` 钮**打不进教程战斗**（战斗侧三件缺口）—— 逐条 → `TutorialModePopup` 文件头。
                case "tutorial":
                    {
                        // 🔴 **2026-10-19（`N83`）**：改走 `WindowsManager.OpenByRef`（**已开着就复用**）——
                        //   原来每次都 `Create` 一扇新的，而模式窗是 `type=Popup` ⇒ `OpenWindow` 对旧窗只
                        //   `ToBackground()`（只写状态、**不 `SetActive(false)`**，与原版 `GameWindow.ToBackground`
                        //   逐字相同）⇒ **连开 N 次台上有 N 扇**（内容相同所以看不出来）。判据/先例 →
                        //   `WindowsManager.OpenByRef` 那段 doc（原版 `automaticallyLoadedWindows` 命中复用）。
                        var win = WindowsManager.OpenByRef("Tutorial Mode Menu", w => TutorialModePopup.Create(w));
                        Debug.Log("[Menu] 模式卡 `TUTORIAL` ⇒ 开 `Tutorial Mode Menu`（原版 `TutorialModePopup`；"
                                + "这条映射是**原版的**，不是我们挑的 —— 唯一消费 `TutorialEvent` 的窗就是它）");
                    }
                    return;
                case "practice":
                    // 🔴 `N83`：同上（已开着就复用，⛔ 不再每次新建）。
                    WindowsManager.OpenByRef("Practice Mode Menu", w => PracticeModePopup.Create(w));
                    Debug.Log("[Menu] 模式卡 `PRACTICE` ⇒ 开 `Practice Mode Menu`");
                    return;
                case "skirmish":
                    {
                        WindowsManager.OpenByRef("SkirmishModeEventWindow", w => SkirmishEventWindow.Create(w));
                        Debug.Log("[Menu] 模式卡 `SKIRMISH` ⇒ 开 `SkirmishModeEventWindow`");
                    }
                    return;
                case "ranked":
                    {
                        WindowsManager.OpenByRef("RankedEventWindowV2", w => RankedEventWindow.Create(w));
                        Debug.Log("[Menu] 模式卡 `排位` ⇒ 开 `RankedEventWindowV2`");
                    }
                    return;
                case "draft":
                    // ============================================================ 🆕 2026-10-24（D2）：**真开轮抽窗**
                    // 原文（留痕，⛔ 别照它改回去）：本支 2026-10-17（`A789` 收口）只做
                    //   「模式卡 + 点了如实提示「暂无服务器」」—— 因为当时轮抽整条线还没开工。
                    // 🔴 **2026-10-24 起那半句过期**：轮抽的数据层（`Shell/DraftPacks.cs`，882 包）与
                    //   免费/常规那扇窗（`Shell/DraftModeWindow.cs` = 原版 `Draft Mode Menu Demo`）都已建好
                    //   （规格 → `资料/普查产出_1024b/S3_轮抽窗原版规格.md`）⇒ **点卡就开窗**。
                    //   ⚠️ 仍然成立的是：原版这张卡「开哪扇窗」由 liveop 事件数据给（`资料/主菜单_原版规格.md`
                    //   §9·3 本地 `?`）—— 我们这条映射是**本工程自己定的**（免费/常规那一扇；限时版不做）。
                    //   ⛔ 旧的「暂无服务器」文案（键 `MainMenu/GameModes/DraftOffline`）**不删**（别处/历史断言还认它），
                    //   只是本支不再用它。
                    {
                        WindowsManager.OpenByRef(DraftModeWindow.RootName, w => DraftModeWindow.Create(w));
                        Debug.Log("[Menu] 模式卡 `选秀` ⇒ 开 `" + DraftModeWindow.RootName + "`"
                                + "（原版 `Draft Mode Menu Demo`；⚠️ **这张卡开哪扇窗是我们定的** —— 原版由 liveop 事件数据给）");
                    }
                    return;
                default:
                    Debug.LogWarning("[Menu] 模式卡 `" + kind + "` 没有对应动作（**出声**）");
                    return;
            }
        }

        // ============================================================ 🆕 **2026-10-19（`A1493④`）：模式卡图那条
        //   【常驻位置视差】 —— 原版 `UIParallaxImage` 的复刻。**
        //
        // ---- 原版是什么（逐条有出处，本轮自己查的）----
        //  ① 组件 = **`UIParallaxImage : Image`**（`Assembly-CSharp` 里独立一个类，
        //     `d:/2/tools/decomp_full/UIParallaxImage__{ctor,OnEnable,Update,UpdateParallax,get_materialForRendering}.c`）。
        //     **它只有一个自己的序列化字段** `useParallax`（bool，ctor 里出厂写 1；字段偏移 0x120），
        //     另两个私有字段是运行时造的：`Material`（0x128）与那个属性的 **id**（0x130）。
        //  ② `OnEnable`：把 `Image.material` 复制一份当自己的实例材质；
        //     `Update`：`useParallax` 为真且 `Graphic.canvas` 非空时调 `UpdateParallax`。
        //  ③ `UpdateParallax`（逐句照字节码）：
        //       `cam = canvas.worldCamera` → `p = transform.position` →
        //       `sp = RectTransformUtility.WorldToScreenPoint(cam, p)` →
        //       `material.SetVector(id, (sp.x / Screen.width, sp.y / Screen.height, 0, 0))`
        //     ⇒ 写进去的是**这件在屏幕上的归一化位置**。
        //  ④ 那个属性 id 来自 `Shader.PropertyToID("<字面量>")`；`Shader Graphs/UI Container Main Menu`
        //     （本件用的材质 = **`Main Menu Container Parallax`**，`bundle_menus_assets_all/Material/Material_3583370424645672959.json`）
        //     的 `m_PropInfo` **只有三个**（`_MainTex` · `_Parallax` · `_ScreenPosition`）⇒ 那个字面量是
        //     `_ScreenPosition`（`_Parallax` 在材质里有非零存量 `(0.2,0,0,0)` ⇒ 它是**常数**、不是被写的那个）。
        //  ⑤ **算式（本轮解出来了）**：原版 HLSL 源码被剥了，但**编译字节码在**（`Shader.compressedBlob`），
        //     用本机 `D3DCompiler_47.dll` 的 `D3DDisassemble` 反汇编之后，**四个顶点程序逐字相同**的最后两行是：
        //       `add r0.xy,  cb2[1].zwzz, l(-0.5, -0.5, 0, 0)`      ← cb2 = `UnityPerMaterial`
        //       `mad o5.xy,  cb2[1].xyxx, r0.xyxx, v3.xyxx`          ← o5 = 传给像素着色器的 `_MainTex` UV；v3 = 网格 UV
        //     而**四个像素程序一个字的视差都没有**（只 `sample_b t0, v5.xy`）⇒ **整条视差在顶点着色器里**，即
        //       **`uv' = uv + _Parallax.xy * (_ScreenPosition.xy - 0.5)`**。
        //     · `cb2` 声明为 **2 个寄存器**、只有 `[1]` 被读，且 `.xy` 与 `.zw` **分别**取用 ⇒ 那两个属性是
        //       **两个 Vector2 打包进同一个寄存器**（`.xy = _Parallax`、`.zw = _ScreenPosition`；
        //       材质里 `_Parallax = (0.2, 0, 0, 0)` 正好是「x 有强度、y = 0」的 Vector2）。
        //     ⚠️ **这一条是【推断】，不是读出来的字段**（没有 RDEF 反射块可读；`m_PropInfo` 只给类型不给宽度）。
        //       反证两条：若 `.xy` 是 `_ScreenPosition`，则 `_Parallax`（0.2,0,0,0）**整条用不上**、
        //       而位移会变成 `-0.5 × 屏幕位置`（最大 ~0.5 UV ≫ 可见窗 0.715 宽，画面会整块跑掉）；
        //       若 `cb2[1]` 整个是其中某一个，另一个属性同样用不上、而 `UIParallaxImage` 存在就没意义。
        //       ⇒ 只有「两个 Vector2 打包」这一读法**两个属性都用得上、量级也合理**。
        //  ⑥ **`_Parallax = (0.2f, 0f)`** ⇒ **只有水平位移、垂直恒为 0**（判据 = 那个材质里存的值）。
        //
        // ---- 我们怎么落 ----
        //   🔴 **我们这一层不引 shader**（原版那条靠「每帧往材质里 `SetVector`」；`ImageQuad` 的 UV 是**烘进网格**的，
        //     而**盘子里的模式卡不动**、`_ScreenPosition` 只跟「这件在屏幕上的位置」有关 ⇒
        //     **建卡时算一次、把位移加进 UV 矩形，与原版逐位等价**）。
        //   ⚠️ 位移**不会**把 UV 顶出 [0,1]：可见窗是 `[0.1425, 0.8575]`（宽 0.715），
        //     五张卡的 `du ∈ [-0.066, +0.098]` ⇒ 最坏 `[0.0765, 0.9555]` 仍在图内 ⇒ 不会踩到贴图的 wrap 模式。
        //   ⚠️ **`Screen.width` 那一格我们直接取设计宽**：本工程是 `CanvasScaler` 式的 1920×1080 设计空间
        //     （`LayoutSpace.DesignPxW/DesignPxH`）⇒ `屏幕x / 屏宽` 恒等于 `设计x / 1920`，与真实分辨率无关。
        //   ⚠️ **垂直那一半恒为 0**（判据 = `_Parallax.y = 0`），所以「Unity 屏幕 y 自下而上、我们自上而下」这件事
        //     在本条上**不产生任何差别**。
        //   ✅ **验收法（铁律 10 第 6 条）**：把 `00_主菜单_无弹窗.png` 与用户参考图 `主界面参考图.png` 的
        //     **同一张模式卡并排渲一次**看卡图有没有横向错开 —— 断言量不出「像不像」。
        //     **改坏法**：把这个 `du` 去掉 ⇒ 五张卡又回到「卡图正中」，与参考图逐张错开的那一档不同。

        /// <summary>原版 `Main Menu Container Parallax` 材质里 `_Parallax` 的 **x** 分量（`_Parallax = (0.2, 0, 0, 0)`，
        /// `bundle_menus_assets_all/Material/Material_3583370424645672959.json` 的 `m_SavedProperties.m_Colors`）。
        /// ⚠️ 同一份里 `m_Floats` 也有一条 `_Parallax = 0.005` —— 那是**内置 Standard shader 的残留死值**
        /// （本 shader 的属性表里 `_Parallax` 是 **Vector**，见 `Shader_5261144965713594700.json` 的 `m_PropInfo`）
        /// ⇒ ⛔ **别拿 0.005 那个数**（CLAUDE.md §三「原版材质上带着内置 Standard shader 的残留值」那条）。</summary>
        public const float ModeCardParallaxX = 0.2f;

        /// <summary>模式卡图那条视差在 **U** 上要加多少（原版 `uv' = uv + _Parallax.xy * (_ScreenPosition.xy - 0.5)`
        /// 里的 `_Parallax.x * (_ScreenPosition.x - 0.5)`）。
        /// <paramref name="cardCenterPx"/> = 这张卡**中心的设计像素 x**（= 传给 `BuildModeCard` 的 `x + w/2`，
        /// 也就是原版 `Background Image` 那颗 RT 的屏幕位置；原版取的是它的 `transform.position`）。
        /// ⚠️ 与真实分辨率无关（`屏幕x / Screen.width` ≡ `设计x / 1920`，见上）。</summary>
        static float ModeCardParallaxDu(float cardCenterPx)
        {
            return ModeCardParallaxX * (cardCenterPx / LayoutSpace.DesignPxW - 0.5f);
        }

        // ============================================================ 🆕 2026-10-24（`E10`）：轮抽卡 `State` 条几何
        // 判据 = `bundle_menus_assets_all` 的 `Draft Game Mode Container 1x2` **现读**
        //   （`python 工具/menu_dump.py bundle_menus_assets_all "Draft Game Mode Container 1x2" --depth 6 --md`）：
        //   · `State`                535 × 109.52 · anchor(0.5,0) pivot(0.5,0) apos(0.00012207, **108.4**)
        //                            ⇒ 距卡底 108.4 ⇒ 卡内左上原点（y 向下）**顶 = 848.8 − 108.4 − 109.52 = 630.88**
        //   · `Victory Counter Icon`  97.453 × 103.327 · anchor(0,1) pivot(0.5,0.5) apos(57.4, −57.861)
        //                            ⇒ 卡内 **x 8.67..106.12 · y 637.07..740.40**（下沿与 `State` 对齐）
        //                            图 = `Rank Skull` 128² · Simple · **preserveAspect** · 色 (1,1,1,1)
        //   · `Victories`（**是图标那颗的子件**）484.688×143.9 × `m_LocalScale` **0.7181**
        //                            ⇒ **视觉 348.05 × 103.33**；apos.x 225.915（相对图标中心）
        //                            ⇒ 卡内 **x 109.29..457.37**、与图标同高
        //                            字 `'2/12'` · fs **99.69** · auto[18~99.69] · base **36** · Left/**Capline** · 折行 1 · 白
        // ⚠️ `'2/12'` 是**预制体的样板串**（那一场活动的 `matches.Length = 12`）⇒ 真值由
        //   `DraftMenuContainer.SetVictoryCounters` 的 `String.Format("{0}/{1}", CurrentStage, draftRounds)` 现写
        //   （格式串 = `stringliteral.json` 的 RVA `0x426de28` = `'{0}/{1}'`；`CurrentStage` = 胜场，
        //     指令流 `get_CurrentStage` = `[event+0x20]+0x48` = `numberOfMatchesWon`）⇒ ⛔ 别印 12。
        // ⚠️ `DraftStateTop` 只是**文档锚点**（`New(...)` 按本文件不变式**不写位置**，坐标都由 `Center(...)`
        //    直接给绝对值）—— 别以为它没被读就是漏了。
        const float DraftStateTop = 630.88f, DraftStateH = 109.52f;
        const float DraftVicL = 8.67f, DraftVicT = 637.07f, DraftVicW = 97.453f, DraftVicH = 103.327f;
        const float DraftVicTxL = 109.29f, DraftVicTxT = 637.07f, DraftVicTxW = 348.05f, DraftVicTxH = 103.33f;
        const float DraftVicTxPx = 99.69f;

        /// <summary>按 §九 9·4 的「最小清单」建一张卡（1x1 与 1x2 共用；1x2 只是更高）。
        /// <paramref name="modeKind"/> 非空 ⇒ 这张卡**可点**（点它进对应模式的界面；见 `OpenMode`）。</summary>
        void BuildModeCard(Transform parent, string name, float x, float y, float w, float h, string art, string title,
                           string modeKind = null, PxRect? clip = null, System.DateTime? timerEndUtc = null,
                           string timerLocKey = null)
        {
            // 🔴 **2026-10-17（界面线 `T12`）：视口裁切。** 横向滚动之后卡片会跨过视口左缘
            //   （`GameModes` 的 x1 = 167.04，左边就是左竖导航）—— 不裁的话，卡的暗带 / 边框 / 标题
            //   （队列 2920/2930/2921 **高于**导航底板 2910）会压在导航上。判据 = 原版视口上的 `RectMask2D`
            //   （`主菜单_原版规格.md:173` 那一族）。整张卡在视口外 ⇒ **连节点都不建**（同 `MenuDraw` 四条）。
            float cx1 = x, cx2 = x + w;
            if (clip.HasValue)
            {
                cx1 = Mathf.Max(cx1, clip.Value.x1);
                cx2 = Mathf.Min(cx2, clip.Value.x2);
                if (cx2 - cx1 <= 0.01f) return;
            }
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
                // 🆕 **2026-10-19（`A1493④`）**：这一格再叠上原版那条**常驻位置视差**（判据与算式 → 上面
                //   `ModeCardParallaxDu` 那一整段）。⚠️ 只挪**采样窗**，窗**宽**（`1 - 2*u0`）一个字没动。
                // 🔴 **2026-10-17（`T12`）**：视口裁切**同时缩网格与 UV 窗** —— 横向缩多少、UV 就缩多少
                //   （否则图会被拉扁）。视差 `du` 仍按**整张卡**的中心算（原版取的是那颗 RT 的位置）。
                float fL = (cx1 - x) / w, fR = (x + w - cx2) / w;
                float uvW = 1f - 2f * u0;
                float du = ModeCardParallaxDu(x + w * 0.5f);
                cardArtQ = ImageQuad.Create(card, tex, Center(cx1, cx2, y, y + h), h / 108f,
                                            new Vector2(0.5f, 0.5f), "Background Image");
                if (cardArtQ != null)
                {
                    cardArtQ.SetAspect((cx2 - cx1) / h);
                    cardArtQ.SetUvRect(new Rect(u0 + du + fL * uvW, v0, uvW * (1f - fL - fR), 1f - 2f * v0));
                    cardArtQ.SetRenderQueue(QCardArtAboveNav);   // ⚠️ 必须高于左竖导航投影（`QPanel` 2910），否则影盖在卡图上（见常量注释）
                }
            }
            else MissingArt.Add(art);

            // ② `TextDarkening`：贴底的纯黑带（`m_Color` **a = 0.6117647**），a(0.0037,0)-(0.9963,0) sz(−1.18×105.87)
            float bandH = 105.8657f, bandBottom = 2.6f;
            float by2 = y + h - bandBottom, by1 = by2 - bandH;
            // 🔴 **2026-10-17（`T12`）**：纯色带照视口裁（与上面卡图同一对 `cx1/cx2`；
            //   它队列 2920 **高于**导航底板 2910 ⇒ 不裁会压到导航上）。
            if (cx2 - cx1 > 0.01f)
                Rect(card, null, Mathf.Max(x + 2.0f, cx1), Mathf.Min(x + w - 2.0f, cx2), by1, by2,
                     "TextDarkening", QContent, new Color(0f, 0f, 0f, 0.6117647f));

            // ③ 标题（`Event Title`，TMP **58.8** 白 · 居中）。原版那行归 `TextDarkening` 上的
            //    **`VerticalLayoutGroup`** 排（`bundle_menus_assets_all` 实读）：
            //    `m_Padding.m_Left = 11` · `m_ChildAlignment = 3 (MiddleLeft)` · `m_Spacing = −4.2` ·
            //    `m_ChildControlWidth/Height = 0`（子女尺寸就是各自序列化值，不被布局组改）。
            //    🔴 **2026-10-17（`N10`）用 `menu_dump.py --relative` 现读绝对矩形，订正了旧算式（铁律 5）**：
            //      旧写法按「两个孩子 55.708 + 40.729 − 4.2 = 92.237、上下各留 6.814」算标题顶 ——
            //      **错因 = 漏了 `childForceExpandHeight = 1`**：VLG 会把余量**均分进每个孩子的槽**（各 +3.4095），
            //      文本框在自己的槽里再垂直居中 ⇒ 实测（`Base Game Mode Container 1x1` 与
            //      `Ranked/Skirmish … 1x2` 三份逐值相同）：**标题顶距暗带顶 = 3.41**、倒计时行顶 = **61.73**；
            //      `Draft … 1x2`（`TextDarkening` 里**只有标题**）⇒ 标题顶 = (105.866 − 55.708)/2 = **25.08**。
            //    ⚠️ 原版 `m_text` 是占位串 `GAME MODE TITLE`，真标题运行时灌 ⇒ 这里用**模式名**。
            // 🔴🔴 **2026-10-25（`X4` · 真机 F1「模式卡底部印 `结束于：--`」）：这一行不再画。**
            //   判据三条（逐条读过，⛔ 不是"看着像"）：
            //     ① **有活动**：用户实拍 `C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png`
            //        两张卡印的是 `结束于：🕐 2d 2h 25m`（**真剩余时长**）；
            //     ② **到期**：`d:/2/tools/decomp_full/TimerDisplay__UpdateTimer.c` —— 剩余 `<= 0` 那一支把
            //        **`Timer Text` 那颗 GameObject `SetActive(false)`**（字段 `timer` = `0x20`，与
            //        `d:/2/Warpforge_code/Scripts/Assembly-CSharp/TimerDisplay.cs` 的字段序吻合）⇒ **原版从不印 `--`**；
            //        `--` 是我们早先为「本地无数据」发明的兜底串，⛔ 不是原版文案。
            //     ③ **这个模式本来就没有倒计时**：原版有 `1x2 No Timer` 变体 —— `TextDarkening` 里**只剩
            //        `Event Title`**（`资料/主菜单_原版规格.md:577`）。
            //   ⇒ 本地拿不到 liveop 的到期时刻（服务端数据）⇒ 走③：**整行不建**，标题回到「只剩一个孩子」
            //     那条 VLG 口径。真值现读（`工具/menu_dump.py bundle_menus_assets_all
            //     "Draft Game Mode Container 1x2" --depth 4 --md`）：暗带顶 **1396.00** ·
            //     `Event Title` 顶 **1421.08** ⇒ **25.08**（= (105.866 − 55.708)/2，与
            //     `childForceExpandHeight=1` 自洽；`Base 1x1` + 两个倒计时变体是 **3.41**）。
            //   ⚠️ 将来接上 liveop 数据 ⇒ 给 `modeTimerEnd` 赋值即可恢复 ⑤ 那一整块（代码留着没删）。
            System.DateTime? modeTimerEnd = timerEndUtc;     // 🆕 B89-E27（`L411`/`L415`）：由调用点按「这一格归哪个在期活动」喂进来（类尾 `ModeCardOwnerEvent`）；没有归属 ⇒ null ⇒ 走 `1x2 No Timer` 变体
            bool hasTimerRow = modeTimerEnd.HasValue;        // 有数据源才建这一行（见上面 ③）
            float titleTop = by1 + (hasTimerRow ? 3.41f : 25.08f);
            // ⚠️ 原版 `m_text` 是占位串 `GAME MODE TITLE`，真标题运行时灌 ⇒ 这里用**模式名**（和 `Game Mode Title` 那个通用件的样例 `Draft Mode` 同口径）
            // 🔴 原版这行 TMP 带 **autosize 18→72**，框 513.7×55.7 ⇒ 让它自己缩着放进框里
            var ti = Text(card, title, x + 13f, x + 13f + 513.6959f, titleTop, titleTop + 55.708f, 8, Color.white, "Event Title", 58.8f,
                          // 🔴 **2026-10-17（`A1233`）**：原来这里是「先 `Text`、再自己补
                          //    `ti.SetAutoFitBox(513.6959/108, 55.708/108, 18, 72, 36)`」—— 收口进本方法形参。
                          //    四格判据 = 原版 `Event Title`（`'GAME MODE TITLE'`，`m_fontSize 58.8` ·
                          //    `auto[18~72]` · 折行 1 · 框 513.6959×55.708）。
                          wrapPx: 513.6959f, autoMinPx: 18f, autoMaxPx: 72f, autoBasePx: 36f,
                          face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `m_fontSizeBase` **原文** = **36.0**。
            //    判据（本轮自己扫 `bundle_menus_assets_all`）：`Event Title`（`'GAME MODE TITLE'`，
            //    `m_fontSize 58.8` · `auto[18~72]` · 折行=1）—— **16 颗全是 `m_fontSizeBase 36.0`**
            //    （`MonoBehaviour_-1926919290004399602.json` 等）；V7 §二·3 #31 记的是全库那族
            //    （258 个，36.0×240 / 32.0×8 / …）⇒ 本条以**这一族自己**的实读为准。
            // 🔴 **2026-10-11（界面复刻）：标题改成【左对齐】** —— 原版那颗 `Event Title` 的 TMP 是
            //   **`m_HorizontalAlignment = 1 (Left)`**（只读判据代理 2026-10-11 现读 MB 实测；
            //   ⚠️ **`资料/主菜单_原版规格.md:554` 写它「居中」是错的**，同族的
            //   `m_VerticalAlignment` 那一族值也不是「Top=0/Middle=1」——原版实际用
            //   `Top=256 / Middle=512 / Bottom=1024 / Baseline=2048 / Geometry=4096 / Capline=8192`）。
            //   我们原来没设 ⇒ `Label` 出厂把它**居中**摆 ⇒ 与参考图（`主界面参考图.png` 里模式名贴着
            //   卡的左缘）对不上。⚠️ **必须排在 `SetAutoFitBox` 之后**（`AlignLeft` 是「整块平移」，
            //   而 autofit 会重排、重排后宽度才定）—— 本仓踩过「`SetAutoFitBox` 排在 `AlignLeft` 前
            //   ⇒ 对齐被抹掉」那条坑（见 `d4-work-rules` 的 10-13 收口三条）。
            MenuDraw.AlignLeft(ti, new PxRect(x + 13f, titleTop, x + 13f + 513.6959f, titleTop + 55.708f));
            // 🔴 **2026-10-17（`T12`）**：标题**也吃视口裁切** —— 它队列 2921、高于导航底板 2910。
            //   `MenuDraw.ClipText` 是公共件里那条「按裁切框逐字改 alpha」的口（与窗内文字同一份实现）。
            //   ⚠️ 只在**真的被裁到**时挂：没裁时挂上等于多一颗 `ClippedTextGuard`（无行为差异、但没必要）。
            if (ti != null && clip.HasValue && (cx1 > x + 0.01f || cx2 < x + w - 0.01f))
                MenuDraw.ClipText(ti, clip, Vector2.zero);

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
                //       · **`fillCenter = false`**（边框不填中间）· `clip` 一律 `null`。
                //   ⇒ 原来那圈 `foreach` **删掉了**：它设的两个值与公共件内部设的**同值**，
                //     留着就是「同一条规则写两处」（将来改一处、另一处静默不动）。
                // 🔴🔴 **2026-10-11 就地订正（铁律 5）—— `borderOutPx` 原来写的是 `13 × 5 = 65`，方向反了。**
                //   `m_PixelsPerUnitMultiplier` 的语义是**除**（**越大边越细**，Unity 文档：A higher value
                //   makes the borders appear smaller）⇒ 画出来的角块 = **`m_Border ÷ ppuMul = 13 ÷ 5 = 2.6 px`**。
                //   同一个口径的正例在 `资料` 里记着（左栏 `Highlight`：`30 ÷ 0.92 = 32.61`）。
                //   🔴 **实测症状（2026-10-11 真机实拍，量出来的）**：卡面本身是 **535×414.4 ✓ 正确**、
                //   卡图也是满卡的，但一圈 **65px 厚的灰边**把外圈全盖住了 —— 可见的画心 =
                //   `535−2×65 = 405` × `414.4−2×65 = 284.4`，与实拍量到的「卡图 409×289」**逐值吻合**
                //   （`_tmp_view/ui_1019/00_crop_card.png`；方差扫描 bbox = 545×385 截图 px）。
                //   ⇒ 看起来就是「灰底板 + 中间一小块图」，而原版是**满幅立绘 + 一条 hairline 灰边**。
                //   判据 → `bundle_menus_assets_all` 的 `Base Game Mode Container 1x1` 的 `Border` 那一颗
                //   （`40k_square_border` · `m_Border (13,13,13,13)` · `m_PixelsPerUnitMultiplier 5` ·
                //     `m_FillCenter 0` · 色 `0.33962`）。
                MenuDraw.Nine(card, borderTex, new PxRect(x, y, x + w, y + h), new Vector4(13, 13, 13, 13),
                              64f, 64f, QOverlay, new Color(0.33962f, 0.33962f, 0.33962f, 1f), false, "Border",
                              new Vector4(13f / 5f, 13f / 5f, 13f / 5f, 13f / 5f), clip);   // = 2.6px（13 ÷ ppuMul 5）
            }

            // ④·b 排位卡那一排「段位印玺」（原版 `Rating ▸ Mission Milestones Progress ▸ steps ▸ RankedSealStep ×N`）。
            //    ⛔ **只给排位卡** —— 通用卡树（`Base Game Mode Container 1x1`/`1x2`/`No Timer`/`Claim`…）
            //       里**没有这一排**，`Skirmish` 那棵 `Skirmish Ranked Game Mode Container 1x2` 里**也没有**
            //       （逐档 dump 核过）⇒ 闸口就是 `modeKind == "ranked"`，**别按「像不像」扩散到别的卡**。
            //    判据 / 父子链 / 显隐口径 / 颗数兜底口径 → `BuildRankedSealRow` 上方那一大段（⛔ 不在这里抄第二份）。
            if (modeKind == "ranked" && cx1 <= x + 0.01f && cx2 >= x + w - 0.01f) BuildRankedSealRow(card, x, y);

            // ⑤ 倒计时那一行（`Timer With Time Description`：`Starts in:` + 时钟 + 剩余时间）
            //    🔴 **2026-10-17（`N10`）从「不画」改成「照原版画」** —— 判据 = `资料/主菜单_原版规格.md:560-565`
            //    ＋ `menu_dump.py --relative` 现读原版 prefab（`Base 1x1` / `Ranked 1x2` / `Skirmish 1x2` 三份）：
            //      · 行框 **424.7056 × 40.729**；**行顶 = 暗带顶 + 61.73**
            //        （`Base 1x1`：367.33 − 305.60 · `Ranked/Skirmish 1x2`：802.13 − 740.40，两份逐值相同）；
            //      · `Timer Description`（`Starts in:`）：fs **42.95** · auto[18~44] · 基准 45 · 白 · 左/Midline；
            //      · `Clock Icon`：**40.7607 × 40.7604** · sprite `WF_icon_clock for shader` · 色 (0.7372,0.7372,0.7372)；
            //      · `Timer Text`：**198.9886 × 45.5207** · fs **40** · auto[10~40] · 基准 30 · 同灰。
            //    ⚠️ 三段宽度里 `Starts in:` 那一段是**反推**出来的：原版 `Timer Description` 的 `sizeDelta.x = 0`
            //    + `ContentSizeFitter PreferredSize` ⇒ 它等于字宽；行宽 424.7056 − 时钟 40.7607 − 时间 198.9886
            //    = **184.9563**（三段首尾相接，与 `Clock Icon`/`Timer Text` 的 `anchor (1,.5)` 链自洽）。
            //    🔴 **数据源 = liveop 活动到期时刻，本地没有** ⇒ ⛔ 编日期不行、印 `--` 也不行（原版从不印
            //    `--`，见 ③ 那三条）⇒ 这一行**不建**；`modeTimerEnd` 一旦有值就整块照原版建回来。
            //    🔴 **`Draft` 卡**原本就没有这一行（原版 `Draft Game Mode Container 1x2` 的 `TextDarkening`
            //    里**只有 `Event Title`**，Event Title y 765.48 = 暗带顶 + **25.08**）—— 现在全部模式卡都走
            //    这条口径，⛔ 别给 Draft 单独补一行（与它自己的变体矛盾）。
            //    🔴 **2026-10-25（`X4`）现行口径**：这一行**只在 `hasTimerRow` 为真时建**（= 真有 liveop 到期
            //    时刻）。本地没有那个数据源 ⇒ **整行不建**、标题走「只剩一个孩子」的 25.08（判据三条见 ③ 上方）。
            //    ⚠️ 这一整块**留着**：将来接上 liveop 数据只需给 `modeTimerEnd` 赋值，几何/字号/词条全在这里。
            if (hasTimerRow)
            {
                float rowTop = by1 + 61.73f;
                const float timerRowH = 40.729f;
                const float descWMax = 184.9563f, clockW = 40.7607f, timerW = 198.9886f;
                float tx0 = x + 13f;
                // 🔴 **2026-10-18（N61）**：原版 prefab 里那颗 TMP 的**出厂占位**是 `Starts in:`，但运行期
                //   `TimerDisplay.InitializeWitLocKey(end, locKey)` 会把它换成**调用方给的词条**
                //   （`TimerDisplay__InitializeWitLocKey.c` → `I2 Loc.GetTranslation(locKey)` → `SetTimerDescriptionText`）。
                //   中文原版实拍（桌面参考图 `主界面参考图.png`）显示的是 **「结束于：」** ⇒ 用官方
                //   `Event_Description/General/EndsIn`（EN `Ends in:` / ZH `结束于：`），⛔ 别再写死英文。
                //   宽度也照原版 `CSF PreferredSize` 现量（原来 184.9563 是从英文串反推的定值）。
                // 🔴 **2026-10-28（B90-N2 现核 → 同日接通）**：原版这一句是
                //   `timer.InitializeWitLocKey(endDate, playEvent.GetTimeDescriptionTextKey())`
                //   （`LiveopMenuContainer__OnInitialize` 现读）—— 词条**由事件状态决定**，三档 =
                //   `Event_Description/General/TimeStartsIn` / `TimeEndsIn` / `TimeFinished`，**空串 = 这一档不显示前缀**。
                //   ✅ 现在由调用点把 `evt.GetTimeDescriptionTextKey()` 经 `timerLocKey` 传进来
                //   （实现 = `Shell/LiveOpsState.cs` 文件尾的 partial 段，与 `Shell/LiveOpsEvent.cs:1208` 逐字同键）
                //   ⇒ ⛔ **别在本文件再写一份三档映射**（同一条规则写第二份 = 迟早不一致）。
                //   `timerLocKey == null` 时用的 `EndsIn` 只是**不可达兜底**：没有归属活动时 `end` 为 null
                //   ⇒ 这一整行压根不建（`hasTimerRow`）。
                string timerDescKey = timerLocKey ?? "Event_Description/General/EndsIn";
                var timerDesc = Text(card, string.IsNullOrEmpty(timerDescKey) ? string.Empty : Loc.T(timerDescKey),
                                     tx0, tx0 + descWMax, rowTop, rowTop + timerRowH, 8,
                                     Color.white, "Timer Description", 42.95f, QText,
                                     0f, 18f, 44f, 45f, face: TmpFont.Face.LatinCardBody);
                float descW = descWMax;
                if (timerDesc != null)
                {
                    timerDesc.ForceRelayout();
                    float natW = timerDesc.WorldW * 108f;
                    if (natW > 1f) descW = Mathf.Clamp(natW, 0f, descWMax);
                    MenuDraw.AlignLeft(timerDesc, new PxRect(tx0, rowTop, tx0 + descW, rowTop + timerRowH));
                    // 视口裁切：与标题同一条口（队列 2921 > 导航底板 2910，不裁会压到导航上）。
                    if (clip.HasValue && (cx1 > x + 0.01f || cx2 < x + w - 0.01f))
                        MenuDraw.ClipText(timerDesc, clip, Vector2.zero);
                }
                var clockTex = Art("WF_icon_clock_for_shader");
                float clockX1 = tx0 + descW, clockX2 = clockX1 + clockW;
                float clockY1 = rowTop - 0.02f, clockY2 = rowTop + 40.7404f;
                if (clockTex != null)
                {
                    // 视口裁切照卡图那条路：**缩网格 + 缩 UV 窗**（⛔ 只缩网格会把图标拉扁）。
                    float cL = clip.HasValue ? Mathf.Max(clockX1, cx1) : clockX1;
                    float cR = clip.HasValue ? Mathf.Min(clockX2, cx2) : clockX2;
                    if (cR - cL > 0.01f)
                    {
                        var cq = ImageQuad.Create(card, clockTex, Center(cL, cR, clockY1, clockY2),
                                                  (clockY2 - clockY1) / 108f, new Vector2(0.5f, 0.5f), "Clock Icon");
                        if (cq != null)
                        {
                            cq.SetAspect((cR - cL) / (clockY2 - clockY1));
                            cq.SetUvRect(new Rect((cL - clockX1) / (clockX2 - clockX1), 0f,
                                                  (cR - cL) / (clockX2 - clockX1), 1f));
                            cq.SetTint(new Color(0.7372f, 0.7372f, 0.7372f, 1f));
                            cq.SetRenderQueue(QText);
                        }
                    }
                }
                else MissingArt.Add("WF_icon_clock_for_shader");
                float timerX1 = clockX2, timerY1 = rowTop - 2.40f, timerY2 = rowTop + 45.5207f;
                var timerLabel = Text(card, TimerDisplay.TextFor(modeTimerEnd, "", "模式卡 " + name,
                                                                "liveop 活动没有到期时刻（本地没有该数据源）"),
                                      timerX1, timerX1 + timerW, timerY1, timerY2, 8,
                                      new Color(0.737f, 0.737f, 0.737f, 1f), "Timer Text", 40f, QText,
                                      0f, 10f, 40f, 30f, face: TmpFont.Face.LatinCardBody);
                if (timerLabel != null)
                {
                    MenuDraw.AlignLeft(timerLabel, new PxRect(timerX1, timerY1, timerX1 + timerW, timerY2));
                    TimerDisplay.Attach(timerLabel, () => modeTimerEnd, "", "模式卡 " + name,
                                        "liveop 活动没有到期时刻（本地没有该数据源）");
                    if (clip.HasValue && (cx1 > x + 0.01f || cx2 < x + w - 0.01f))
                        MenuDraw.ClipText(timerLabel, clip, Vector2.zero);
                }
            }
            else
            {
                // 出声一次（⛔ 不静默失败）：这一行的**数据源**确实没有 ⇒ 界面按原版 `1x2 No Timer` 变体处理
                // （⚠️ **不画** —— 原版从不印 `--`，判据三条见 ③ 上方）。
                TimerDisplay.WarnUnknownOnce("模式卡 " + name,
                    "liveop 活动没有到期时刻（本地没有该数据源）⇒ 这一行**不建**（原版 `1x2 No Timer` 变体；"
                  + "⛔ 不印 `--`、也不编日期）");
            }

            // ⑤·b 🆕 2026-10-24（`E10`）：**只有轮抽卡**有那条 `State` 条（`DraftMenuContainer` 的 `stateGO` +
            //    `victoriesCounter`）。⛔ 别扩散到别的卡：`Base 1x1/1x2 · Ranked · Skirmish` 那几棵树的
            //    `TextDarkening` 里**都没有**这一件（逐档 dump 核过；`Ranked/Skirmish` 有的是另一族
            //    `Rating ▸ … RankedSealStep` 排，闸口在 `BuildRankedSealRow` 那一段）。
            //    判据/几何 → 本文件 `DraftStateTop` 那一整段注释。
            if (modeKind == "draft") BuildDraftStateBar(card, x, y, w, clip, cx1, cx2);

            // ⑥ 点击区（**只有模式卡有**）：原版卡根上挂 `LiveopMenuContainer` + `EverguildButton`，
            //    开哪扇窗由 liveop 数据给的事件对象决定（本地查不到）⇒ 我们自己接（见 `OpenMode`）。
            if (!string.IsNullOrEmpty(modeKind))
            {
                // A332：⚠️ **原版也没有这一件**（同一族：原版卡自己挂 `EverguildButton`，没有另建的命中区子件）
                //   ⇒ 矩形**是我们自己的**（= 卡那一格 `w × h`）。
                var hitGo = New(card, "Hit", w, h);
                // 🔴 **2026-10-17（`T12`）**：命中区也照视口裁 —— 否则滚到左边的那段卡会**盖着导航钮吃点击**。
                var q = ImageQuad.Create(hitGo, CardArt.Solid(), Center(cx1, cx2, y, y + h), h / 108f,
                                         new Vector2(0.5f, 0.5f), "Hit");
                if (q != null)
                {
                    q.SetAspect((cx2 - cx1) / h);
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
                // 🆕 **2026-10-19（`A1493④`）：这一格【不再改任何东西】—— 原版那一次悬停动态已经在手上。**
                //   本会话把原版那颗 `EverguildButton` 的四个 `m_Colors` 与 `m_FadeDuration` 逐值对过一遍：
                //     · `m_HighlightedColor / m_SelectedColor = 0.9607843` · `m_PressedColor = 0.7843137` · `m_NormalColor = 白`
                //       —— 正是 `WindowButton.HighlightK / SelectedK / PressedK` 的那三个**缺省值**，
                //       而本处**没有**覆盖 `HighlightKey`（只有主菜单那 5 个导航钮覆盖成浅蓝）⇒ **默认即正确**。
                //     · `m_FadeDuration = 0.1` —— 正是 `WindowButton.FadeSeconds`。
                //   ⇒ **色偏那一半：判据齐、实现齐，一个字都不用改**（改坏法：把 `wb.TintOn(cardArtQ)` 删掉 ⇒
                //     色偏落回那颗透明 `Hit` 上、画面上没有任何变化）。**本条 `A1493④` 真正的缺口只剩下面那条视差**
                //     （见 `ModeCardParallaxDu`）—— 另一条查证结论同时成立：**没有位移 / 缩放 / 换图 / Animator**
                //     （`m_Transition` 就是 `1`(ColorTint)，`m_SpriteState` 那几格不参与；卡上也没有 `Animator`）。
            }
        }

        // ============================================================ 🆕 2026-10-24（`E10`）：轮抽卡 `State` 条
        //
        // 原版那一件是 `Draft Game Mode Container 1x2/State`（`DraftMenuContainer.stateGO`），
        // 由 `DraftMenuContainer.OnDraftStateChange → DoOperationsOnDraftState` 在**每一次轮抽状态变化**时刷。
        // 我们这边没有那套事件（一局的数据在 `DraftModeWindow` 的静态存档里、卡是主菜单建的）
        // ⇒ 用「主菜单每帧比一次」（`RefreshDraftModeCard`，挂在 `Update`）等价实现 ——
        // 这正是「回壳后必须再点一次模式卡才看得到进度」那条缺陷的修法。

        /// <summary>建轮抽卡的 `State` 条（原版 `stateGO` + `victoriesCounter`）。
        /// 结构照 prefab：`State` → `Victory Counter Icon`（`Rank Skull` 128²·preserveAspect）→ `Victories`（字）。
        /// ⚠️ **`Victories` 是图标那颗的子件**（原版 `m_Father` 就是它）—— 别挂到 `State` 上。
        /// 几何/字号/文案口径 → 本文件 `DraftStateTop` 那一整段注释（逐字段现读）。</summary>
        void BuildDraftStateBar(Transform card, float x, float y, float w, PxRect? clip, float cx1, float cx2)
        {
            var stateGo = New(card, "State", 535f, DraftStateH);
            var iconGo = New(stateGo, "Victory Counter Icon", DraftVicW, DraftVicH);

            float ix1 = x + DraftVicL, ix2 = ix1 + DraftVicW, iy1 = y + DraftVicT, iy2 = iy1 + DraftVicH;
            var skull = Art("Rank_Skull");
            if (skull != null)
            {
                // 视口裁切与卡图/时钟图标同一条路：**缩网格 + 缩 UV 窗**（⛔ 只缩网格会把图标拉扁）。
                float cL = clip.HasValue ? Mathf.Max(ix1, cx1) : ix1;
                float cR = clip.HasValue ? Mathf.Min(ix2, cx2) : ix2;
                if (cR - cL > 0.01f)
                {
                    var iq = ImageQuad.Create(iconGo, skull, Center(cL, cR, iy1, iy2), (iy2 - iy1) / 108f,
                                              new Vector2(0.5f, 0.5f), "Image");
                    if (iq != null)
                    {
                        iq.SetAspect((cR - cL) / (iy2 - iy1));
                        iq.SetUvRect(new Rect((cL - ix1) / (ix2 - ix1), 0f, (cR - cL) / (ix2 - ix1), 1f));
                        iq.SetRenderQueue(QText);      // 压着卡图(2912)与暗带(2920)之上、边框(2930)之下
                    }
                }
            }
            else MissingArt.Add("Rank_Skull");

            _draftVicTxR = new PxRect(x + DraftVicTxL, y + DraftVicTxT,
                                      x + DraftVicTxL + DraftVicTxW, y + DraftVicTxT + DraftVicTxH);
            // 折行 1 · auto[18~99.69] · base 36 · Left/**Capline**（判据 → `DraftStateTop` 那段注释）。
            // ⚠️ `wrapPx` 传**视觉宽 348.05**（原版那颗 484.688 × scale 0.7181），不是布局宽。
            _draftVictories = Text(iconGo, "", _draftVicTxR.x1, _draftVicTxR.x2, _draftVicTxR.y1, _draftVicTxR.y2,
                                   8, Color.white, "Victories", DraftVicTxPx, QText,
                                   wrapPx: DraftVicTxW, autoMinPx: 18f, autoMaxPx: DraftVicTxPx, autoBasePx: 36f,
                                   face: TmpFont.Face.LatinCardBody);
            if (_draftVictories != null)
            {
                // ⚠️ `AlignLeft` 必须排在 `Text(...)` 之后（它在内部先 `SetAutoFitBox` 重排、重排完宽度才定
                //    —— 本仓踩过「`SetAutoFitBox` 排在 `AlignLeft` 前 ⇒ 对齐被抹掉」那条坑）。
                MenuDraw.AlignLeft(_draftVictories, _draftVicTxR);
                MenuDraw.SetVAlign(_draftVictories, Label.VAlign.Capline, _draftVicTxR);
                if (clip.HasValue && (cx1 > x + 0.01f || cx2 < x + w - 0.01f))
                    MenuDraw.ClipText(_draftVictories, clip, Vector2.zero);
            }

            _draftStateNode = stateGo;
            RefreshDraftModeCard(true);
        }

        /// <summary>把 `State` 条刷成**当前那一局**的进度（原版 `SetVictoryCounters` +
        /// `DoOperationsOnDraftState` 的本地等价物）。文案 = `String.Format("{0}/{1}", 胜场, draftRounds)`
        /// （格式串证据 → 本文件 `DraftStateTop` 那段注释）。
        /// <para>⚠️ 读口用的是 `DraftModeWindow.RunForTest` —— 它是**唯一的静态**读口（实例属性
        /// `Victories` 在窗关掉之后拿不到，`LastOpened` 可能指向已销毁的窗）。本类白名单不允许改
        /// `DraftModeWindow.cs` ⇒ 先借它；主对话合并时建议在那边加一个生产名（如 `CurrentRun`）后
        /// **只改这一行**。</para></summary>
        public void RefreshDraftModeCard(bool force = false)
        {
            if (_draftStateNode == null || _draftVictories == null) return;
            var run = DraftModeWindow.RunForTest;
            int wins = run != null ? run.matchesWon : 0;
            int rounds = DraftPacks.RoundsPerDraft;
            bool show = DraftStateBarVisible(run);
            if (!force && show == _draftStateShown && wins == _draftWinsShown) return;
            _draftStateShown = show;
            _draftWinsShown = wins;
            _draftStateNode.gameObject.SetActive(show);
            _draftVictories.SetText(string.Format("{0}/{1}", wins, rounds));
            // ⚠️ **只在显示时重排左对齐**：TMP 在对象未激活时量不出宽高（本仓踩过 —— `textBounds` 会是垃圾）
            //    ⇒ 先 `SetActive`、再换字、再对齐。对齐必须排在 `SetText` 之后（换字会重排）。
            if (show) MenuDraw.AlignLeft(_draftVictories, _draftVicTxR);
            Debug.Log("[Menu] 轮抽模式卡 `State` 条：" + (show ? "显示" : "隐藏")
                    + " · `" + wins + "/" + rounds + "`"
                    + "（原版 `SetVictoryCounters` 的 `{0}/{1}` 口径：阶段 = 胜场、分母 = `draftRounds`）");
        }

        /// <summary>原版 `DraftMenuContainer.DoOperationsOnDraftState` 的**可见性判据**（本地等价物）。
        /// <para>反编译（`decomp_full/DraftMenuContainer__DoOperationsOnDraftState.c`）：取 `event.DraftState`；
        /// `state &lt; 0x15 &amp;&amp; ((0x108420 &gt;&gt; state) &amp; 1)` ⇒ `stateGO.SetActive(true)` 并写胜场字，
        /// 否则 `SetActive(false)`。掩码 `0x108420` 的位 = **5 / 10 / 15 / 20**
        /// （SelectWarlord / SelectPack / OngoingRun / Finished）—— Intro(0) / Pay(2) / Collected(22) **不显示**。</para>
        /// <para>我们的状态从**静态存档**推（存档里没有 `draftState` 那一格）：收尾支 / 无战将支 / 有战将支
        /// 三条与 `DraftModeWindow.UpdateState` 同源。⚠️ **有一处有意不同**：`UpdateState` 把
        /// 「`joinState != Joined`」也路由到 `SelectWarlord(5)`，而那一档**原版落的是 Pay 页**
        /// （E3 的 `ResumeGroup③` 已记：落页看的是**存着的** `DraftState`）⇒ 这里要求
        /// `joinState == Joined` **且已入**（`payState != NotPaid`）才算 `SelectWarlord`；没报名一律藏。</para></summary>
        public static bool DraftStateBarVisible(DraftRunSave run)
        {
            if (run == null) return false;                         // 没有进行中的一局（原版 save == null）
            bool finished = run.joinState == (int)DraftJoinState.Finished
                            || run.matchesLost >= DraftModeWindow.MaxTries;
            if (finished) return !run.rewardsCollected;            // Finished(20) 显示 · Collected(22) 藏
            if (!string.IsNullOrEmpty(run.warlordId)) return true; // SelectPack(10) / OngoingRun(15)
            return run.joinState == (int)DraftJoinState.Joined
                   && run.payState != (int)DraftPayState.NotPaid;  // SelectWarlord(5) 显示；Intro(0)/Pay(2) 藏
        }

        // ============================================================ 🆕 2026-10-11：排位卡那一排「段位印玺」
        //
        // 🔴 **判据**（`资料/普查产出_第十七会话/Q_卡底小圆图标.md` —— 只读代理逐 pid 直读
        //    `bundle_menus_assets_all` 的 GO/RT/MB + `d:/2/tools/decomp_full/` 反编译 + 参考图逐像素量；
        //    ⚠️ 本轮**没有**重查、**没有**跑 Unity）：
        //
        //   **父子链**（只有排位族的两棵 `1x2` 树有这一排）：
        //     `Ranked Game Mode Container 1x2` → `Rating` → `Mission Milestones Progress` → `steps`
        //       → `RankedSealStep ×N` ─┬ `Empty`（Image · sprite `Rank Skull Empty` · 128² · preserveAspect）= `emptyMark`
        //                               └ `Fill` （Image · sprite `Rank Skull`       · 128² · preserveAspect）= `checkMark`
        //   ⛔ **通用树里没有** —— `Base Game Mode Container 1x1` / `1x2` / `1x2 No Timer` / `1x1 Claim` /
        //      `1x1 Claim AutoCollect` / `1x2 Raid or Army Release` / `Reward Event 1x1` 逐档 dump 过、一条都没有。
        //   ⛔ **`Skirmish` 那张卡也不加** —— `Skirmish Ranked Game Mode Container 1x2` 的
        //      `RankedMenuContainer` **字段表与排位卡逐字相同**，但**这一排不在它的树里**（已逐档 dump 核过）。
        //   ⛔ **别和 `Draft` 卡上那颗 `Victory Counter Icon` 搞混** —— 它 sprite **也是** `Rank Skull`，
        //      但在 `Draft Game Mode Container 1x2` 的 `State` 子件下、是**单颗**胜场计数，不是这一排。
        //
        // 🔴 **显隐：原版走的是【组件级 `Behaviour.set_enabled`】，⛔ 不是 `GameObject.SetActive`**
        //    （赋值点 = `MilestoneStep__Toggle.c`）：
        //      `checkMark.enabled = on` · `emptyMark.enabled = on ? alwaysShowEmptyMark : true`
        //    本卡 prefab 里 **`alwaysShowEmptyMark = 0`** ⇒ **达成的只画金色、未达成的只画灰色**。
        //    ⚠️ 原版**两个子件的 GO `m_IsActive` 恒为 True**，改的只是那两个 `Image` 组件的 `enabled`；
        //       而我们的 `ImageQuad` **没有 `enabled` 这一档**（它只在 `Create` 里铺一次 mesh、没有 `Update`
        //       ⇒ 禁组件在画面上等于什么都没做）⇒ **我们的显隐口只能落在 GO 上**（见 `SealToggle`）。
        //       **这是有意不同、不是抄漏**：效果等价（「只画一颗」），代价 = 子件 GO 的 `activeSelf` 与原版相反
        //       ⇒ **自检要查它，必须用 `GetComponentsInChildren<…>(true)`（含 inactive）取件，
        //          而且「该亮的那颗」要【同时】断 `activeSelf == true`**（本仓最贵的那一课：
        //          含 inactive 的查找 ⇒ 找得到 ≠ 显示得出来）。
        //
        // 🔴 **颗数与点亮数在【远端】—— 建这一排时拿不到真值（本活的难点）**：
        //     颗数 = `RankedDivisionV2.TotalSeals`（0x54）· 点亮数 = `RankedDivisionV2.SealCount`（0x50），
        //     由 `RankedFastMode__GetDivisionByPoints.c` 用 `ConfigManager.GetConfig<>()`（**远端配置**）
        //     构造 ⇒ **本地推不出参考图里那个「4」**。
        //     ⚠️ 参考图那排是 **4 颗、全灰** ⇒ **推断** `TotalSeals = 4` / `SealCount = 0`
        //        （**也不能排除**视频压暗把金色压成灰 —— 这一条是推断、**不是判据**，如实标在这里）。
        //     ⇒ **本活的口径**：建 `RankedSealFallbackCount` 颗、全部按**未点亮（灰）**画，
        //        并且 `Build` 现场 `Debug.Log` **如实出声**（红线：⛔ 不许静默、⛔ 不许假装是原版数据）。
        //
        // 🔴 **同一父件下的【另一支】`Global Rating` —— 2026-10-11（`A1524`）【已建】**（原来是「只做印玺这一支」）：
        //     `Rating` 下那两支**互斥**，门控 = 原版 `IRankedEvent.get_IsLegendaryElo()`（**运行期布尔**，
        //     ⛔ **不是**「排名档位」推算）。判据（第一权威 = 反编译方法体，第十八会话只读代理现核）：
        //       · `RankedMenuContainer__RefreshContentDisplay.c:16-27` —— `playEvent == null` 那一支
        //         = `SetActive(milestonesStepDisplay, false)` + `ToggleLegendaryRankInfo(false)`
        //         ⇒ **没有活跃 ranked 事件 ⇒ 两支都关**；
        //       · 同文件 `本文件` —— `X = rankedEvent.IsLegendaryElo` ⇒
        //         `SetActive(milestonesStepDisplay, X == 0)` **＋** `ToggleLegendaryRankInfo(X)`
        //         ⇒ **一条判据同时决定两支、而且方向相反**；
        //       · `RankedMenuContainer__ToggleLegendaryRankInfo.c:8-24` —— 末句就是
        //         `SetActive(ratingDisplay.gameObject, option)` ⇒ `Global Rating` 的开关落在这一句上。
        //     `X` 的真身：账上那个「读不到名字的泛型方法」`FUN_1800021f0(3, IRankedEvent, event)` 已用
        //       `工具/disasm_va.py` 反出来 —— 它是 **il2cpp 的接口方法调用器**（第一参 = 接口方法下标），
        //       下标 3 = `get_IsLegendaryElo`（`dump.cs:130514-130570` 的 `IRankedEvent` 成员序；
        //       交叉验证：下标 8 = `GetDivisionByPoints`）。
        //     ⚠️ **两支在 prefab 里 `m_IsActive` 全是 `true`**（`bundle_menus_assets_all/GameObject/`
        //        `Global Rating_-4564886699666007538.json` 与 `Mission Milestones Progress.json`）
        //        ⇒ **互斥 100% 在运行时代码里** ⇒ ⛔ 序列化默认值当不了判据（铁律 5·c），**必须自建门控**（见下）。
        //     📌 判据全文 → `资料/普查产出_第十八会话/B_三条判据挖掘.md` 问题 B。
        //
        // 🔴 **本地落哪一支 —— 我们的选择 + 三条理由（⛔ 不许静默）**：
        //   原版三态 = 「传奇 ⇒ 关印玺行 / 开大段位图＋名次」·「非传奇 ⇒ 反之」·「**无活跃 ranked 事件 ⇒ 两支都关**」。
        //   本地**永远拿不到活跃 ranked 事件**（事件数据随 PlayFab 下发），而原版 prefab 出厂两支都 active
        //   ⇒ **严格照「无事件」那一态，本卡一支都不该显示**。**我们不取那一态**，理由：
        //     ① **唯一能拿到的「原版在这一屏上长什么样」的实况证据是用户参考图**
        //        （`C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png`）—— 那张图上**排位卡带着印玺行**
        //        （骷髅那排）⇒ 那正是「**有活跃事件 + 非传奇**」那一态；
        //     ② 整个 `GameModes` 的内容（哪五张卡 / 谁开哪扇窗 / 尺寸档）**本来就是本地按参考图重建的**
        //        （liveop 数据本地没有，见 `BuildGameModes` 那两段）—— 单挑这一支去「严格照无事件」并不自洽；
        //     ③ 印玺行是 `A1521⑧` 已收口、带一整套断言的实现，把它关掉 = **可见回退**。
        //   ⇒ **默认落「非传奇」那一态**（印玺行开 / `Global Rating` 关），并且 `ApplyRankedRatingBranch` **现场出声**。
        //   ⚠️ 这一格是**我们定的**（原版由远端事件数据决定）—— ⛔ 别写成「原版就是这样」（铁律 3/11）。
        // ⚠️ 另插一条容易踩的：`legendaryRankDivisionInfoObjects` 名字唬人，**不是印玺那一排** ——
        //    实读只指向 `Position #` 与 `Position Value` 两颗（那**仍是 `Global Rating` 那一支**）。
        //
        // ⚠️ **几何复核**：六个卡内坐标用**归一化比值**验过一遍（避开「卡被竖向拉高」这个未定缩放）——
        //    参考图那排实测卡内 **0.790~0.863**，`steps` 带的设计值 **0.759~0.861** ⇒ **吻合**。

        /// 🔶 自己设计（非原版）：印玺排兜底颗数 · 原版来自远端 `RankedDivisionV2.TotalSeals` · TODO(谁定的)｜原注：<summary>那一排的**兜底颗数**。🔴 **这不是原版常数** —— 原版来自远端 `RankedDivisionV2.TotalSeals`
        /// （由 `ConfigManager.GetConfig<>()` 构造），本地拿不到（判据 → 本段上方）。
        /// ⚠️ **自检别拿这个常量当期望值**（那是同式自证）—— 期望值写死 `4` 并把上面那条判据引上。</summary>
        public const int RankedSealFallbackCount = 4;

        /// <summary>兜底点亮数（参考图那 4 颗**全灰** ⇒ 0）。🔴 同样是**我们挑的兜底** ——
        /// 原版来自玩家进度的 `RankedDivisionV2.SealCount`（远端 / 存档），本地拿不到。</summary>
        public const int RankedSealFallbackLit = 0;

        /// <summary>排位卡 `Rating` 底下那两支的**互斥门控** —— 照原版 `RankedMenuContainer.RefreshContentDisplay`
        /// / `ToggleLegendaryRankInfo` 那两行（判据 → 本段上方那段长注释，⛔ 不在这里抄第二份）。
        /// 三态里的 <see cref="None"/> = 原版「**没有活跃 ranked 事件**」那一态 ⇒ **两支都关**。
        /// ⚠️ 留 <see cref="None"/> 是为了**把原版三态写全**（将来接上 `IRankedEvent` 时只改一个赋值点），
        /// ⛔ 不是为了今天用它 —— 本地到不了那一态（我们没有事件数据，选择理由见上方）。</summary>
        public enum RankedRatingBranch
        {
            /// <summary>两支都关（原版：`playEvent == null`）。</summary>
            None = 0,
            /// <summary>印玺行（`Mission Milestones Progress`）—— 原版 `IsLegendaryElo == false` 那一支。</summary>
            Milestones = 1,
            /// <summary>大段位图 + 名次（`Global Rating`）—— 原版 `IsLegendaryElo == true` 那一支。</summary>
            GlobalRating = 2,
        }

        /// <summary>本地这一屏排位卡落哪一支。**默认 = 参考图那一态**（非传奇 ⇒ 印玺行开 / `Global Rating` 关）
        /// —— 选择与三条理由写在上面那段长注释里，⛔ 别静默改这个默认值。
        /// <para>🔴 **2026-10-28 调度台裁定（账 `个人资料 270` / `ToggleLegendaryRankInfo`）**：本地落
        /// **「关」那一支**（本地无名次/ranked 事件数据 ⇒ 等价于原版 `position == 0` / `playEvent == null` 的关），
        /// 并且**必须出声**（`ApplyRankedRatingBranch` 那条 `Debug.Log`）—— ⛔ **别为了让画面「更丰富」选开**。</para>
        /// ⚠️ **实例字段，不是 `static`** —— 同 <see cref="SelectedNav"/>（static 会跨自检 / 跨场景残留）；
        /// 自检要验另一支时：在拿到的 `MainMenuRuntime` 实例上设这个字段、再走同一条建树路（⛔ 别读它当真值断 ——
        /// 那是同义反复，期望值该写**原版那两行判据**）。</summary>
        public RankedRatingBranch RankedBranch = RankedRatingBranch.Milestones;

        /// <summary>排位卡 `Rating` 那一格的**卡内**绝对矩形（判据 = 原版 RT 逐级套出来的「布局后绝对矩形」——
        /// 原版 RT 原文照抄在下面那几条，便于任何人自己重推，⛔ 别只信这六个数）：
        /// <code>
        /// `Rating`                      a(.5,0)-(.5,0) p(.5,0) ap(0, **106.4005**) sz(**494.61 × 109.52**)
        ///   ⇒ 底 = 848.8 − 106.4005 = 742.40 · 顶 = 742.40 − 109.52 = 632.88（卡高 848.8 = `CardHTall`）
        /// `Mission Milestones Progress` a(0,1)-(0,1)（挂 `Rating`）p(.5,.5)
        ///   ap(**247.3059**, **−54.7622**) sz(**494.61 × 86.51**)
        ///   ⇒ 中 y = 632.88 + 54.7622 = 687.64 ⇒ **644.39 ~ 730.90**；x 与 `Rating` 同（20.2 ~ 514.81）
        /// </code>
        /// ⚠️ 两处都**不是**「直接读一个字段」——它们是从父链 `m_Father` 一级级套出来的绝对矩形。
        /// 🔴 **提到类级**是让 `BuildRankedSealRow`（印玺那一支）与 `BuildGlobalRating`（另一支）**共用同一份**
        /// —— ⛔ 别在两处各写一份（两处写同一条规则 = 迟早不一致）。</summary>
        const float RatingL = 20.20f, RatingR = 514.81f, RatingT = 632.88f, RatingB = 742.40f;
        const float MmpT = 644.39f, MmpB = 730.90f;

        /// <summary>排位卡上那一排「段位印玺」（原版 `Rating ▸ Mission Milestones Progress ▸ steps`）。
        /// <paramref name="x"/> / <paramref name="y"/> = 这张卡的**左上角**（本文件 `BuildModeCard` 的口径、y 向下）
        /// —— 方法里那六个数是**卡内**坐标，加上这两个才变成绝对设计 px。
        /// 判据 / 父子链 / 显隐口径 / 兜底口径 → 本方法上方那一大段（⛔ 不在这里抄第二份）。</summary>
        void BuildRankedSealRow(Transform card, float x, float y)
        {
            // ---- 卡内绝对矩形 = **类级那两条 `const`**（`RatingL/RatingR/RatingT/RatingB` · `MmpT/MmpB`）——
            //      原版 RT 原文与逐级推导照抄在那两条的 doc 里（⛔ 别在这里抄第二份；提到类级是为了让
            //      `BuildGlobalRating` 那一支共用同一份）。
            // 单颗几何：`steps` 上那颗 `HorizontalLayoutGroup` = spacing **3.2** · `m_ChildAlignment` **3 (MiddleLeft)**
            // · pad 0 · ctrlW/H **1** · expandW/H **0**；每颗的 `LayoutElement` preferred **100 × 100**（高被布局组
            // 压到父件高 86.51）⇒ **节距 = 100 + 3.2 = 103.2**、第 0 颗**贴左缘**。
            const float StepW = 100f, StepGap = 3.2f, StepPitch = StepW + StepGap;

            var rating = New(card, "Rating", RatingR - RatingL, RatingB - RatingT);                 // 494.61 × 109.52
            var mmp = New(rating, "Mission Milestones Progress", RatingR - RatingL, MmpB - MmpT);   // 494.61 × 86.51
            // `steps`（原版 `a(0,0)-(1,1)` + `sz(0×0)` = **全拉伸锚点**、真实矩形 = 父件 494.61 × 86.51）；
            // 我们的 `New` 只写 `sizeDelta`、**表达不了拉伸锚点** ⇒ 这里写**真实矩形**（那一格的实际大小）。
            // ⚠️ 别把这两个数读成「原版 `steps` 的 `sizeDelta`」—— 原版那颗是 `0×0`。
            var steps = New(mmp, "steps", RatingR - RatingL, MmpB - MmpT);

            // ============================================================ 🆕 **2026-10-11（`A1524`）**
            // **`Rating` 下的【另一支】`Global Rating` + 那两支的互斥门控**。
            // 判据 / 原版三态 / 本地落哪一支的三条理由 → 本方法上方那段长注释（⛔ 不在这里抄第二份）。
            // 🔴 **为什么建在这里（印玺那两张图的早退【之前】）**：这一支与骷髅图无关 ——
            //    放在早退之后的话，两张骷髅图一旦都取不到，`Global Rating` 就跟着建不出来了（那是两件事）。
            var globalRating = BuildGlobalRating(rating, x, y);
            ApplyRankedRatingBranch(mmp, globalRating);

            // 两张图（原版是**本地固定的两张切图**，各 128×128）：
            //   `Empty` = `emptyMark`（灰 `Rank Skull Empty`）· `Fill` = `checkMark`（金 `Rank Skull`）
            var texEmpty = Art("Rank_Skull_Empty");
            var texFill = Art("Rank_Skull");
            // 🔴 取不到就**出声**、并且那一颗**不画**（红线：⛔ 不许静默、⛔ 不许拿别的图顶替）。
            if (texEmpty == null)
                Debug.LogWarning("[Menu] 排位卡那排「段位印玺」：`Rank_Skull_Empty` 取不到 ⇒ **未达成（灰）的印玺一颗都不画**"
                               + "（应在 `Resources/Art/ui_menu/Rank_Skull_Empty.png`；⛔ 不拿别的图顶替）");
            if (texFill == null)
                Debug.LogWarning("[Menu] 排位卡那排「段位印玺」：`Rank_Skull` 取不到 ⇒ **已达成（金）的印玺一颗都不画**"
                               + "（应在 `Resources/Art/ui_menu/Rank_Skull.png`；⛔ 不拿别的图顶替）");
            if (texEmpty == null && texFill == null) return;      // 两张都没有 ⇒ 这一排建不出来，别往下建一排空节点

            for (int i = 0; i < RankedSealFallbackCount; i++)
            {
                float sx1 = x + RatingL + i * StepPitch;
                // 节点名 `RankedSealStep <i>` —— ⚠️ **这个名字是我们定的**：原版出厂只有**一个模板**
                // `RankedSealStep Animation Disabled`，运行时是 `Instantiate` 出来的克隆（名字带 `(Clone)`）
                // ⇒ **原版没有「`RankedSealStep 0`」这个名字**，我们的编号只为可读 / 可定位。
                var step = New(steps, "RankedSealStep " + i, StepW, MmpB - MmpT);
                // `Empty` / `Fill` 铺满那一颗（原版 `a(0,0)-(1,1)` + `preserveAspect`）；128² 的方图**内接**
                // 进 100 × 86.51 的格 ⇒ **实绘 86.51 × 86.51、水平居中**（走 `RectTex` 的 `keepAspect` 那条路）。
                // ⚠️ 队列 `QContent`（2920）：夹在卡图（`QCardArtAboveNav` 2912）与边框（`QOverlay` 2930）之间；
                //    它和 `TextDarkening`（也是 `QContent`）**矩形不相交**（印玺行卡内 644.39~730.90 vs 暗带
                //    740.33~846.20）⇒ 同档**不会**产生「谁盖谁」的不确定；`Empty`/`Fill` 两颗**互斥**
                //    （`SealToggle` 保证**恰好一颗亮**）⇒ 同档也不冲突。
                var empty = texEmpty != null
                    ? RectTex(step, texEmpty, sx1, sx1 + StepW, y + MmpT, y + MmpB, "Empty", QContent, true) : null;
                var fill = texFill != null
                    ? RectTex(step, texFill, sx1, sx1 + StepW, y + MmpT, y + MmpB, "Fill", QContent, true) : null;
                // 逐颗点亮（原版 `MilestonesStepDisplay__Initialize.c`：
                // `DestroyAllChildren(stepsHolder)` → 逐颗 `Instantiate` → `MilestoneStep.Toggle(step, i < 已完成数, …)`）
                _rankedSealPairs.Add(new RankedSealPair { Empty = empty, Fill = fill });   // B92-N4c：点亮改走共用件（见下）
            }
            MilestonesStepDisplay.Initialize<RankedSealPair>(RankedSealFallbackLit, _rankedSealPairs.Count, _rankedSealPairs.ToArray(), null, null, p => p, (p, filled, i, ps, pa) => SealToggle(p.Empty, p.Fill, filled), null, false, false);
            // 🔴 **出声**（红线）：这一排的颗数 / 点亮数**原版来自远端**，本地的 4 / 0 是**我们挑的兜底**。
            Debug.Log("[Menu] 排位卡那一排「段位印玺」（原版 `Rating ▸ Mission Milestones Progress ▸ steps`）："
                    + "建了 " + RankedSealFallbackCount + " 颗 · 点亮 " + RankedSealFallbackLit + " 颗。"
                    + "🔴 原版的**颗数 = `RankedDivisionV2.TotalSeals`、点亮数 = `SealCount`**，由"
                    + "`ConfigManager.GetConfig<>()`（**远端配置** + 玩家存档）构造 ⇒ **本地推不出来**；"
                    + "这里的 " + RankedSealFallbackCount + " / " + RankedSealFallbackLit + " 是**我们挑的兜底**"
                    + "（参考图那排是 4 颗全灰 —— ⚠️ 那是**推断**，视频压暗也可能把金色压成灰）⇒ ⛔ 不是原版数据。");
        }

        /// <summary>`Rating` 下那一支 `Global Rating`（原版 MB `AllianceRatingDisplay`：大段位图 + 名次）。
        /// 返回**容器节点**（调用方拿它过门控 ⇒ 关 / 开整支）。
        ///
        /// <para>🔴 **几何从原版 RT 的【锚点 / `anchoredPosition` / `sizeDelta` / `LayoutElement`】取**，
        /// ⛔ **不照抄「布局组跑之后」那一组 x** —— 这一支挂着一个 `HorizontalLayoutGroup`
        /// （`spacing 0` · `m_ChildAlignment 4 (MiddleCenter)` · `ctrlW/H 1` · `expandW/H 0`），
        /// 它会把子件**按内容整体居中**重排；而 `工具/menu_dump.py` 对它这一族的主轴**自己标了「算不准」**
        /// （子件里有 TMP 文字 ⇒ 首选宽要字体度量；实测 `Position Value` 在工具里量出的宽就是 **0**）。
        /// 实测命令：`python 工具/menu_dump.py bundle_menus_assets_all --rt 7063492927571073550 --depth 4 --md`
        /// （父件 = `Ranked Game Mode Container 1x2`；卡内坐标 = 表里绝对值 − 卡左上角 `(−267.50, 655.60)`）。
        /// ✅ **两条腿互证**（`--no-layout` 的锚点算法 vs 带布局那一次）：两个图标与 `Position #` 的 x / 宽
        /// **逐值相同** ⇒ 它们的 `LayoutElement` preferred = `sizeDelta`（`Main Icon` 81×84 ·
        /// `Secondary Icon` 60×57.356 · `Position #` 宽 **41**）。</para>
        ///
        /// <para>🔴 **「居中」是怎么落的**：原版是「内容整体居中」，而内容宽 =
        /// `41 + 名次串宽 + 81 + 分数串宽` ⇒ **两段字串的宽运行期才知道** ⇒ 走本仓同族那一套
        /// 「**先建、再量 `Label.WorldW`、最后摆**」（先例 = `Shell/TopBar.cs` 的 `BuildResourcesBar`，
        /// 那里同样是「布局组 + 文本宽」这件事）。
        /// ⚠️ **`Shell/RankedTab.cs` 对【同一个组件】取的是「按左对齐摆」那一档**（那里的原话是
        /// 「居中量要用字体度量 ⇒ 我们按左对齐摆」）—— 本处取**居中**，因为量宽这条路本仓已经有现成的；
        /// ⛔ **两处都别写成「原版是左对齐」**（原版是 `align 4` 居中），差异如实记在这两处。</para>
        ///
        /// <para>`Secondary Icon` 在 prefab 里 **`m_IsActive = false`**，而 uGUI 建 `rectChildren` 时
        /// **跳过 inactive** ⇒ 它**不参与那个 HLG**、位置就是它自己的锚点值 ⇒ 不并进上面那条内容宽。
        /// 照原版**建出来 + `SetActive(false)`**（同本文件纪律 ③ 那两颗顶栏钮的处理）。</para>
        ///
        /// <para>🔴 **本地拿不到那两段文字**（名次 / 单场 elo 都是玩家数据，原版由
        /// `RankedMenuContainer.RefreshContentDisplay` 灌进去）⇒ 走**全工程唯一那个空值占位**
        /// `RankedTab.ScoreEmpty`（同族先例 → `Shell/AllianceMemberTab.cs` 的 `ScoreEmpty`：「⛔ 不自己发明、
        /// 也不留一个原版没有的空白」），并且**出声**（⛔ 不静默 —— 红线）。</para></summary>
        Transform BuildGlobalRating(Transform rating, float x, float y)
        {
            // 容器：与 `Rating` 同矩形（原版 `a(0,0)-(1,1)` **拉伸锚点** ⇒ 真实矩形 = 父件 494.61 × 109.52）；
            // 我们的 `New` 只写 `sizeDelta`、表达不了拉伸锚点 ⇒ 写**真实矩形**（同 `steps` 那条处理）。
            var gr = New(rating, "Global Rating", RatingR - RatingL, RatingB - RatingT);

            // 那一竖的中线（**绝对**设计 px）：五颗里四颗的 `ap.y` 都是 **−54.7622**（`Main Icon` 是 −54.7623）
            // ⇒ 中 y = `RatingT` + 54.7622 = 卡内 687.64。
            float midY = y + RatingT + 54.7622f;
            float stripL = x + RatingL, stripR = x + RatingR;             // 这一支那一带（绝对）
            // 逐件尺寸 —— 判据 = 原版 RT / `LayoutElement`（见 doc 的「两条腿互证」），⛔ 别改成从别处量
            const float HashW = 41f, HashPx = 66.6f;                      // `Position #`（无自适应：`fontSize == base == 66.6`）
            const float MainW = 81f, MainH = 84f;                         // `Main Icon`
            const float SecW = 60f, SecH = 57.356f;                       // `Secondary Icon`（出厂 F）
            const float NumPx = 78f, NumBase = 31.38f;                    // 两段数字：字号 78 · base 31.38
            const float RowH = MmpB - MmpT;                               // 行高（= `Mission Milestones Progress` 那一带）

            // ① 两段数字**先建、后量**（临时框宽只用于量，⛔ 不是版面值）
            var posVal = Text(gr, RankedTab.ScoreEmpty, stripL, stripR, midY - NumPx * 0.5f, midY + NumPx * 0.5f,
                              8, Color.white, "Position Value", NumPx, QContent, face: TmpFont.Face.LatinCardBody);
            var indVal = Text(gr, RankedTab.ScoreEmpty, stripL, stripR, midY - NumPx * 0.5f, midY + NumPx * 0.5f,
                              8, Color.white, "Individual rating value", NumPx, QContent,
                              face: TmpFont.Face.LatinCardBody);
            float wPos = LabelW(posVal), wInd = LabelW(indVal);
            // 内容宽 = `#` + 名次串 + `Main Icon` + 分数串（`Secondary Icon` 出厂关着 ⇒ **不进布局**）
            // ⚠️ 名次那两颗**只在 `RankedPositionKnown` 时进布局** —— 原版 `TogglePositionDisplay(option)` 把它们
            //    `SetActive(option)`，而 uGUI 的 HLG 会**跳过 inactive**（inactive 不进 `rectChildren` 的尺寸）⇒
            //    关着时内容宽就只剩 `Main Icon` + 分数串、并且**整体重新居中**（判据 → 本方法上方 doc 与
            //    `RankedMenuContainer__TogglePositionDisplay.c`）。
            float total = (RankedPositionKnown ? HashW + wPos : 0f) + MainW + wInd;
            float left = (stripL + stripR) * 0.5f - total * 0.5f;         // 内容整体居中（原版 align 4 MiddleCenter）
            if (total <= 1f)
            {
                // ⛔ **不许静默**：量不出字宽（TMP 不在激活链上 / 没量到）⇒ 退回**左对齐**并说一声。
                Debug.LogWarning("[Menu] 排位卡 `Rating ▸ Global Rating`：两段数字**量不出宽**（合计 " + total
                               + "px）⇒ 这一行退回**左对齐**摆。⚠️ 原版是按内容**居中**（HLG `align 4`）；"
                               + "受影响的是**位置**、不是内容（对照 → `Shell/RankedTab.cs` 那条左对齐档）。");
                left = stripL;
            }

            // ② `Position #` —— 原版 `m_HorizontalAlignment = Center` / `m_VerticalAlignment = Midline`
            //    ⇒ **居中摆是对的**（不是左对齐）；无自适应 ⇒ 只给字号、不给 autofit 框。
            //    （句柄留着 `TogglePositionDisplay` 用 —— 原版那两颗是 `legendaryRankDivisionInfoObjects` 的成员。）
            var hashLb = Text(gr, "#", left, left + HashW, midY - HashPx * 0.5f, midY + HashPx * 0.5f, 8, Color.white,
                 "Position #", HashPx, QContent, face: TmpFont.Face.LatinCardBody);

            // ③ 大段位图（`Main Icon`）—— 与 `Secondary Icon` 同一张 `40k_UI_icon_ranked_Skirmish`（128²）
            var rankTex = Art("40k_UI_icon_ranked_Skirmish");
            if (rankTex == null)
                Debug.LogWarning("[Menu] 排位卡 `Rating ▸ Global Rating`：`40k_UI_icon_ranked_Skirmish` 取不到 ⇒ "
                               + "**大段位图与 `Secondary Icon` 都不画**（⛔ 不拿别的图顶替）；两段数字照画。");
            float iconL = left + (RankedPositionKnown ? HashW + wPos : 0f);
            if (rankTex != null)
                RectTex(gr, rankTex, iconL, iconL + MainW, midY - MainH * 0.5f, midY + MainH * 0.5f,
                        "Main Icon", QContent, true);                 // 原版 `m_PreserveAspect = 1`（128² 内接进 81×84）

            // ④ 名次串 / ⑤ 分数串 —— 原版两颗都是 `m_HorizontalAlignment = Left` + **折行 1** + 自适应：
            //    `Position Value` `auto[18 ~ 78]` · `Individual rating value` `auto[20 ~ 78]`，
            //    两颗 `m_fontSizeBase` 都是 **31.38**（实读）。
            //    ⚠️ 顺序**必须**是 `SetAutoFitBox` → `MenuDraw.AlignLeft`（`AlignLeft` 是「整块平移」，
            //    而 autofit 会重排、重排后宽度才定 —— 本仓踩过「autofit 排在 AlignLeft 前 ⇒ 对齐被抹掉」）。
            if (posVal != null)
            {
                posVal.SetAutoFitBox(wPos / 108f, RowH / 108f, 18f, NumPx, NumBase);
                MenuDraw.AlignLeft(posVal, new PxRect(left + HashW, midY - RowH * 0.5f,
                                                      left + HashW + wPos, midY + RowH * 0.5f));
            }
            if (indVal != null)
            {
                indVal.SetAutoFitBox(wInd / 108f, RowH / 108f, 20f, NumPx, NumBase);
                MenuDraw.AlignLeft(indVal, new PxRect(iconL + MainW, midY - RowH * 0.5f,
                                                      iconL + MainW + wInd, midY + RowH * 0.5f));
            }

            // ⑥ `Secondary Icon` —— **出厂 `m_IsActive = false`**、且**不参与那个 HLG**（inactive 被跳过）
            //    ⇒ 位置 = 它自己的锚点值：`a(0,1) p(0,0.5) ap(85.2704, −28.678)` ⇒ 卡内
            //    **105.47..165.47 × 632.88..690.24**（实读互证：`--no-layout` 与带布局那两次**逐值相同**）。
            if (rankTex != null)
            {
                var sec = RectTex(gr, rankTex, stripL + 85.2704f, stripL + 85.2704f + SecW,
                                  y + RatingT, y + RatingT + SecH, "Secondary Icon", QContent, true);
                if (sec != null) sec.gameObject.SetActive(false);
            }

            // 原版 `RankedMenuContainer.Refresh` 里紧跟的一句：`TogglePositionDisplay(position != 0 && 符号一致)`
            // （作用点 = `Position #` / `Position Value` 两颗；判据 `RankedMenuContainer__TogglePositionDisplay.c`
            //  = `foreach (go in legendaryRankDivisionInfoObjects) go.SetActive(option)`）。
            _rankedPosHash = hashLb != null ? hashLb.transform : null;
            _rankedPosValue = posVal != null ? posVal.transform : null;
            TogglePositionDisplay(RankedPositionKnown);

            // 🔴 **出声**（红线）：这一段的数据本地一份都没有 —— 如实说，⛔ 别让画面看起来像「有数据」。
            Debug.Log("[Menu] 排位卡 `Rating ▸ Global Rating`（原版 `AllianceRatingDisplay`）：**结构建了**，"
                    + "但**名次 / 单场 elo 两段数据本地一份都没有**（原版由 `RankedMenuContainer.RefreshContentDisplay` 灌）"
                    + "⇒ 两段都画**空值占位** `" + RankedTab.ScoreEmpty + "`。"
                    + "名次那一对（`Position #` / `Position Value`）按原版 `TogglePositionDisplay("
                    + (RankedPositionKnown ? "true" : "false") + ")` 摆 —— 本地无名次数据 ⇒ 落原版 `position == 0` 那一支（**两颗关着**）。"
                    + "⚠️ 这一支**默认关着**（本卡默认落「非传奇」那一态）—— 判据与选择理由 → `BuildRankedSealRow` 上方那段长注释。");
            return gr;
        }

        /// <summary>量一颗 `Label` **画出来的**宽（设计 px）。⚠️ 先 `ForceRelayout()`（同 `TopBar.BuildResourcesBar`）。
        /// ⚠️ **对象必须在激活链上** —— TMP 在没激活时量不出尺寸（本仓踩过：`textBounds` 会变成垃圾值，
        /// 见 `CLAUDE.md` §三「面板画出来了、字不在」那三条）。量不到就返回 **0**，由调用方**出声**（⛔ 不静默）。</summary>
        static float LabelW(Label lb)
        {
            if (lb == null) return 0f;
            lb.ForceRelayout();
            return lb.WorldW * 108f;
        }

        /// <summary>**两支的互斥门控** —— 照原版 `RankedMenuContainer` 那两行：
        /// `SetActive(milestones, IsLegendaryElo == 0)` ＋ `ToggleLegendaryRankInfo(IsLegendaryElo)`
        /// （后者末句 = `SetActive(ratingDisplay, option)`）⇒ **一条判据同时决定两支、方向相反**。
        /// 三态（<see cref="RankedRatingBranch"/>）与本地为什么默认落「非传奇」→ `BuildRankedSealRow` 上方那段长注释。
        /// ⚠️ **两支都建成节点再按门控关开**（⛔ 不是「只建该显示的那一支」）—— 照原版结构，
        /// 而且 `Editor/MainMenuScene.cs` 那几条按名字找 `steps` / `RankedSealStep` 的断言**含 inactive**
        /// （`FindChild`）⇒ 关着也找得到；自检要断「该亮哪一支」就**逐颗断 `activeSelf`**（⛔ 别只断「找得到」）。</summary>
        void ApplyRankedRatingBranch(Transform milestones, Transform globalRating)
        {
            bool showMilestones = RankedBranch == RankedRatingBranch.Milestones;
            bool showGlobal = RankedBranch == RankedRatingBranch.GlobalRating;
            if (milestones != null && milestones.gameObject.activeSelf != showMilestones)
                milestones.gameObject.SetActive(showMilestones);
            if (globalRating != null && globalRating.gameObject.activeSelf != showGlobal)
                globalRating.gameObject.SetActive(showGlobal);
            // 🔴 **出声**：这一档是**我们定的**（原版由远端 `IRankedEvent.IsLegendaryElo` 决定），⛔ 别静默。
            Debug.Log("[Menu] 排位卡 `Rating` 两支的门控（原版 `IRankedEvent.IsLegendaryElo`）：本地落 **"
                    + RankedBranch + "** ⇒ `Mission Milestones Progress`（印玺行）"
                    + (showMilestones ? "**开**" : "关") + " · `Global Rating`（大段位图＋名次）"
                    + (showGlobal ? "**开**" : "关") + "。"
                    + "🔴 原版三态 = 传奇 / 非传奇 / **无活跃 ranked 事件（两支都关）**；本地**永远拿不到活跃事件**"
                    + "（数据随 PlayFab 下发）⇒ 我们按**用户参考图那一态**（有事件 + 非传奇）落，理由三条写在"
                    + "`BuildRankedSealRow` 上方那段长注释里 —— ⛔ 这一格不是原版数据。");
        }

        /// <summary>原版 `MilestoneStep__Toggle.c` 那两行的**唯一一份**实现 ——
        /// `checkMark.enabled = on` · `emptyMark.enabled = on ? alwaysShowEmptyMark : true`
        /// （本卡 prefab 的 `alwaysShowEmptyMark = 0` ⇒ **达成只画金色、未达成只画灰色**）。
        /// 🔴 `public static` 是**有意**的：将来接上远端 `RankedDivisionV2` 的 `TotalSeals` / `SealCount` 时，
        /// 「按进度点亮」这一头**只该有这一个口**（两处各写一遍 = 迟早不一致，CLAUDE.md §三）。
        ///
        /// <para>⚠️ **与原版唯一的不同 = 显隐落在哪一层**：原版改的是那两个 `Image` **组件的 `enabled`**
        /// （两个子件 GO 恒 active），我们改的是这两个 `ImageQuad` 的 **GO `activeSelf`**
        /// —— 因为我们的 `ImageQuad` 没有 `enabled` 那一档（禁组件在画面上什么都不做，见本段上方那段）。
        /// ⇒ **自检要查它，必须把这颗 GO 找得到**（`GetComponentsInChildren<…>(true)`，含 inactive）
        /// **并且【同时】断 `activeSelf`** —— 只断「找得到 / 有 `ImageQuad`」在两种状态下都会绿。</para>
        ///
        /// <para>⚠️ 出厂两颗**同时在 `activeSelf = true` 上**（照原版「两个子件 GO 恒 active」）——
        /// 所以第 ① 步「未达成就把灰的点亮」不是多余的：**没 `Toggle` 过的那一颗照原版是亮着的**。</para></summary>
        public static void SealToggle(ImageQuad empty, ImageQuad fill, bool lit)
        {
            const bool alwaysShowEmptyMark = false;                  // 判据 = 本卡 prefab 的 `alwaysShowEmptyMark = 0`
            SetQuadVisible(fill, lit);                               // `checkMark.enabled = on`
            SetQuadVisible(empty, lit ? alwaysShowEmptyMark : true); // `emptyMark.enabled = on ? alwaysShowEmptyMark : true`
        }

        /// <summary>`ImageQuad` 的显隐口 —— **只能落在 GO 上**（`ImageQuad` 自己没有 `enabled` 这一档）。
        /// ⚠️ 先比 `activeSelf` 再 `SetActive`（与 `Shell/CollectionWindow.cs` 那几条同形，省一次无谓的 `SetActive`）。</summary>
        static void SetQuadVisible(ImageQuad q, bool on)
        {
            if (q != null && q.gameObject.activeSelf != on) q.gameObject.SetActive(on);
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
        public Transform Find(string name) { return FindIn(_root, name); }

        /// <summary>在任意一棵子树里按名字找（含 inactive）。
        /// 🔴 **2026-10-17（T16 / `N4`）新开这一支**：运行期左栏/顶栏/聊天挂在**壳**的
        /// `Safe area Only Horizontal` 下（跨场景）⇒ 它们**不在 `_root` 的后代里**，`Find` 看不到 ——
        /// 凡是「从某个已知节点往下找」的地方一律走这里，⛔ 别拿 `_root` 那一条兜底。</summary>
        public static Transform FindIn(Transform t, string name)
        {
            if (t == null) return null;
            foreach (var c in t.GetComponentsInChildren<Transform>(true))
                if (c.name == name) return c;
            return null;
        }

        // ============================================================ 🆕 2026-10-27（C5 批 · 账 347/348/352）
        // 🔴 纪律：这一段**追加在类尾**（本文件被账按行号引用 ⇒ 只允许末尾追加）。
        Label _chatPreviewRow1, _chatPreviewRow2;
        Transform _chatPreviewIcon;
        ImageQuad _chatPreviewContainer;
        float _cpT;                 // `Show()` 已经跑了多久（秒）
        bool _cpTween;

        /// <summary>= 原版 `ChatPreview.Show()`（**逐句**，判据 `out_cpp2il_dev_ilspy/ChatPreview.cs`）：
        /// `SetActive(true)` → `DOScale(icon, 1, 0.6).From(0).SetEase(OutBack)` ·
        /// `DOFillAmount(container, 1, 0.4)` · `DOFade(container, 1, 0.2).From(0).SetDelay(0.2)`。
        /// <para>🆕 **2026-10-27（C5 · 账 347）**：本仓没有 DOTween ⇒ 用 `Update` 里的帧插值
        /// （同 `TickNavPanelTween` 那一套；`TickChatPreviewTween` 每帧调 <see cref="StepChatPreview"/>）。
        /// ⚠️ **两处如实差异**：① `DOFillAmount` 在本仓**没有对等物**（`ImageQuad` 不是 `Image`、没有
        /// `fillAmount` 那一档）⇒ 那一跳**不做**、只出声一次；② `Ease.OutBack` 的 overshoot 用 DOTween
        /// 库默认值 **1.70158**（那是库常量，不是这张 prefab 的字段）。</para>
        /// <para>⚠️ **批处理里没有帧循环** ⇒ 自检要手动步进：`StepChatPreview(dt)`（先例：粒子要手动 `Simulate`）。</para></summary>
        public void ShowChatPreview()
        {
            SetChatPreviewVisible(true);
            _cpT = 0f; _cpTween = true;
            if (_chatPreviewIcon != null) _chatPreviewIcon.localScale = Vector3.zero;      // `From(0)`
            if (_chatPreviewContainer != null) SetQuadTint(_chatPreviewContainer, 0f);      // `From(0)` 的 alpha
            if (_saidChatFill == false)
            {
                _saidChatFill = true;
                Debug.LogWarning("[ChatPreview] `DOFillAmount(container, 1, 0.4)` **没有对等物** —— 本仓 `ImageQuad` "
                               + "不是 `Image`、没有 `fillAmount` 那一档 ⇒ 这一条 tween 不做（缩放与淡入照做；⛔ 不假装做了）。");
            }
        }

        /// <summary>预览出场动画的**手动步进**口（`Update` 每帧调；自检也调它 —— 批处理没有帧循环）。</summary>
        public void StepChatPreview(float dt)
        {
            if (!_cpTween) return;
            _cpT += dt <= 0f ? 0f : dt;
            const float scaleDur = 0.6f, fadeDur = 0.2f, fadeDelay = 0.2f;
            float ts = Mathf.Clamp01(_cpT / scaleDur);
            if (_chatPreviewIcon != null) _chatPreviewIcon.localScale = Vector3.one * EaseOutBack(ts);
            float ta = _cpT <= fadeDelay ? 0f : Mathf.Clamp01((_cpT - fadeDelay) / fadeDur);
            if (_chatPreviewContainer != null) SetQuadTint(_chatPreviewContainer, ta);
            if (_cpT >= scaleDur) { _cpTween = false; if (_chatPreviewIcon != null) _chatPreviewIcon.localScale = Vector3.one; if (_chatPreviewContainer != null) SetQuadTint(_chatPreviewContainer, 1f); }
        }

        /// <summary>出场动画还在跑吗（自检读口）。</summary>
        public bool ChatPreviewTweening { get { return _cpTween; } }

        void TickChatPreviewTween() { if (_cpTween) StepChatPreview(Time.deltaTime); }

        bool _saidChatFill;
        /// <summary>DOTween `Ease.OutBack`（overshoot 1.70158 = 库默认值）：`1 + c3·(t−1)³ + c1·(t−1)²`。</summary>
        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        /// <summary>`ImageQuad` 的 alpha 档（保留原 tint 的 RGB）。本仓没有 `DOFade`，这是它的终态写法。</summary>
        static void SetQuadTint(ImageQuad q, float a)
        {
            if (q == null) return;
            var c = q.Tint; c.a = a; q.SetTint(c);
        }

        /// <summary>= 原版 `ChatPreview.UpdatePreviews(string channel, List&lt;ChatMessageData&gt; messages)`
        /// （**逐句**，判据 `out_cpp2il_dev_ilspy/ChatPreview.cs` 的 `UpdatePreviews`）：
        /// ① 频道必须等于本组件那个频道（本仓 = `Global`）；② `messages.Count &lt; 1` ⇒ **什么都不做**
        /// （连 `Show()` 都走不到 —— 所以预览保持出厂不可见）；③ `TakeLast(previews.Count)` 取最后 2 条，
        /// 逐条 `ChatPreviewMessage.Set(playerName, GetMessage())`。
        /// <para>⚠️ **调用点**：原版挂在 `ChatGlobalManager` 的 `OnChatUpdate`（服务器推送；本地没有）
        /// ⇒ 我们的等价时机 = **每次打开主菜单建完预览之后一次**（`BuildChatPreview` 末尾那句）。</para></summary>
        public void UpdateChatPreviews()
        {
            var all = SocialData.ChatMessages;
            var picked = new List<SocialData.ChatMessage>();
            for (int i = all.Count - 1; i >= 0 && picked.Count < 2; i--)
                if (all[i] != null && all[i].Channel == "Global") picked.Insert(0, all[i]);
            if (picked.Count == 0) return;      // 原版：没有消息 ⇒ 永远走不到 `Show()`
            if (_chatPreviewRow1 != null) SetPreviewRow(_chatPreviewRow1, picked[0].Sender, SocialData.Text(picked[0]));
            if (picked.Count > 1 && _chatPreviewRow2 != null)
                SetPreviewRow(_chatPreviewRow2, picked[1].Sender, SocialData.Text(picked[1]));
            ShowChatPreview();      // = 原版 `Show()`（显出 + 出场动画；⚠️ 既有自检用的 `SetChatPreviewVisible` 语义不变）
        }

        /// <summary>= 原版 `ChatPreviewMessage.Set(string player, string message)`（**逐句**，
        /// 判据 `out_cpp2il_dev_ilspy/ChatPreviewMessage.cs`）：
        /// `message.enabled = !IsNullOrEmpty(message)`；文本 =
        /// `"&lt;color=#" + ToHtmlStringRGB(nameColor) + "&gt;" + player + ":&lt;/color&gt; " + message`。
        /// <para>⚠️ **两处如实差异**：① 原版 `nameColor` 是 prefab 序列化字段（实读 `(0,1,0.1254902,1)`
        /// ⇒ `#00FF20`），本仓拿不到那个字段 ⇒ **常量照抄实读值**；② 本仓 `Label` 没有 `enabled` 这一档
        /// （同 `SetQuadVisible` 那条注释）⇒ `message.enabled` 等价 = **节点 `activeSelf`**。
        /// ⚠️ 顺序：**先写字再开节点**（TMP 在未激活时量不出尺寸 —— 与 `Shell/ChatPanel.cs` 那条同族坑）。</para></summary>
        public static void SetPreviewRow(Label lb, string player, string message)
        {
            if (lb == null) return;
            bool has = !string.IsNullOrEmpty(message);
            if (has)
            {
                string rgb = ColorUtility.ToHtmlStringRGB(new Color(0f, 1f, 0.1254902f, 1f));
                lb.SetText("<color=#" + rgb + ">" + (player ?? "") + ":</color> " + message);
            }
            if (lb.gameObject.activeSelf != has) lb.gameObject.SetActive(has);
        }

        // ============================================================ 活动钮：title + 倒计时（原版 `OpenEventButton.SetButtonInfo`）
        //
        // 判据（🥇 `menu_dump bundle_menus_assets_all "Event Navigation Button" --depth 6` · 2026-10-27 现读）：
        //   · 根 `Event Navigation Button` 字段表：`badgeNotification, button, buttonIcon, closeOnToggleChangeToFalse,
        //     closeOtherMenus, label, labelHolder, **overIcon**, **timer**, **timerHolder**` —— 即原版这一颗
        //     有 **title(label)** / **overIcon** / **timer+timerHolder** 三件由 `SetButtonInfo` 运行期灌值。
        //   · `Timer`（`TimerDisplay`）局部 **−28.6,156.6 → 192.9,181.6**（221.56 × 25.00）
        //   · 其子 `Timer Text`（出厂 INACT）fs **26.35** / base **30** / auto[18~28] / Center·Capline / 白，
        //     prefab 样例串 **`5d 20h 15m`**
        //   · `Over Image`（= overIcon 的落点）901.1,92.9 → 1018.9,210.8（117.86²）—— **本机无该 sprite 资产**
        //
        // 🔶 自己设计（非原版）：① 剩余时间的**格式串**（`Xd Yh Zm`）· 原版走 `TimeHelper.FormatTimeSpan`，
        //   而 ILSpy 那一跳是 `NoteDecompilerIssue("Method not found @181F58920")`（体没解出）·
        //   判据只有 prefab 的样例串 `'5d 20h 15m'` ⇒ 按它定形 · 调度台 2026-10-27 授权（B88_J2 第 3 批）
        //   ② 倒计时**只在建树那一刻算一次**（本壳没有按秒刷新的泵；原版 `TimerDisplay.Update` 每
        //   `targetFrameRate` 帧刷一次）⇒ 会话跨过整分钟时屏上会停旧值 —— 已在下面出声。
        // ⚠️ 追加在**类尾**（上面那些行号被账引用）。

        /// <summary>读「当前在期 Parent 活动」的 **title** 与 **结束时刻**（原版 `SetButtonInfo` 的输入）。
        /// 没有在期活动 ⇒ `title = null` / `endUtc = null`（调用点保留 `EVENT` 兜底，与原版空态同义）。</summary>
        internal static void TryGetEventNavInfo(out string title, out System.DateTime? endUtc)
        {
            title = null;
            endUtc = null;
            var now = System.DateTime.UtcNow;
            LiveOpsLocalEvent ev; string why;
            if (!LiveOpsLocalCatalog.TrySelectActive(LiveOpsLocalKind.Parent, now, out ev, out why))
            {
                Debug.Log("[Menu] 活动钮 title/倒计时：**没有在期 Parent 活动**（" + why
                        + "）⇒ title 保留 `EVENT` 兜底、不建倒计时（⛔ 不编活动文案）");
                return;
            }
            // 原版 `TryGetLabel(ev,4) ?? TryGetLabel(ev,0)`（4 = NavigationBarTitle、0 = Title）。
            LiveOpsLocalLabel l;
            string term = null;
            if (ev.TryGetLabel(4, out l)) term = l.Term;
            else if (ev.TryGetLabel(0, out l)) term = l.Term;
            if (!string.IsNullOrEmpty(term)) title = LiveOpsLocalCatalog.TermText(term);
            if (string.IsNullOrEmpty(title))
                Debug.LogWarning("[Menu] 活动钮 title：在期活动 `" + (ev != null ? ev.EventId : "?")
                               + "` 的 label 4/0 都取不到词条 ⇒ 保留 `EVENT` 兜底（原版是空态）");
            if (ev != null && ev.EndUtc > now) endUtc = ev.EndUtc;
            // overIcon：原版 `LoadAsset(ev, 9)` → `Over Image`（117.86² @901.1,92.9）。本机没有这批活动图 ⇒ 出声、**留 C**。
            Debug.LogWarning("[Menu] 活动钮 **overIcon 未建**（原版 `SetButtonInfo` 的另一半 = `LoadAsset(ev,9)` → "
                           + "`Over Image` 117.86² @901.1,92.9）：本机 liveops 图标集里没有 over-icon 那一张 ⇒ "
                           + "**如实出声、不拿 Icon 顶上**（这一格仍记 C）");
        }

        /// <summary>建 `Timer`（`TimerDisplay`）那一格 + `Timer Text`。判据见本节头。
        /// <para>⚠️ 我们**不建 `Timer` 容器节点**，直接把这颗字摆在按钮的同一坐标空间里
        /// （原版是 `Timer` 容器 + `Timer Text` 子件；本壳的建树是扁平 quad ⇒ 少一层不影响画面，
        /// 但 `Timer` 那层若将来要挂 `TimerDisplay` 组件再补）。</para></summary>
        void BuildNavEventTimer(Transform button, System.DateTime endUtc)
        {
            const float Tx1 = -28.6f, Tx2 = 192.9f, Ty1 = 156.6f, Ty2 = 181.6f;   // = 绝对 − 根 (876.9, 80.1)
            Text(button, NavTimerText(endUtc), Tx1, Tx2, Ty1, Ty2, 6, Color.white, "Timer Text",
                 26.35f, wrapPx: Tx2 - Tx1, autoMinPx: 18f, autoMaxPx: 28f, autoBasePx: 30f,
                 face: TmpFont.Face.LatinCardBody);
            Debug.Log("[Menu] 活动钮倒计时已建：到期 = " + endUtc.ToString("u") + " ⇒ 屏上 `"
                    + NavTimerText(endUtc) + "`（🔶 建树时算一次，本壳没有按秒刷新的泵）");
        }

        /// <summary>剩余时间串（格式来自 prefab 样例 `5d 20h 15m`；🔶 见本节头「自己设计 ①」）。</summary>
        internal static string NavTimerText(System.DateTime endUtc)
        {
            var left = endUtc - System.DateTime.UtcNow;
            if (left.TotalSeconds <= 0) return "0m";
            int d = (int)left.TotalDays, h = left.Hours, m = left.Minutes;
            if (d > 0) return d + "d " + h + "h " + m + "m";
            if (h > 0) return h + "h " + m + "m";
            return m + "m";
        }

        // ============================================================ 类尾：`AdaptFlexibleGridLayoutByAspectRatio`
        //   **在我们的空间模型里是恒等变换** —— 为什么**不建**这个组件（B88-A3 · 2026-10-28）
        //
        // 原版那个组件（`mainmenuwarpforge` 场景 **唯一 1 处**挂接 = MB 1962，宿主 GameObject pathID **28** =
        // `GameModes/Content`，其 `FlexibleGridLayout` = MB 1961：`rows=2 columns=20 itemSize 535×414.4
        // spacing (20,20)` —— 与本文件 `CardCol0/CardColStep/CardW/CardH` 那一组**逐字段相同**）
        // 四个方法体的**全部效果**是一句话：
        //     `itemSize = 原始 itemSize × s`，其中 `s = max(1, (16/9) / (Screen.width/Screen.height))`
        //     （`Transition…` 不适用；判据 = `decomp_full/AdaptFlexibleGridLayoutByAspectRatio__{OnEnable,
        //     ResolutionHasChanged,ModifyFlexibleLayout,OnDisable}.c` 四个体逐句；常量 `1.7777778` / `1.0`
        //     读自 `GameAssembly.dll` 的 `0x1834b32d0` / `0x1834b2bb8`，重建阈值 `1e-4` = `0x1834b32f8`（8 字节 double））。
        //     `OnEnable` 另外做三件：① `originalItemSize == Vector2.zero`（Unity `Vector2==` 的 1e-5² 判据）时
        //     把当前 `itemSize` 缓存成原始值；② `Signal.Register<ScreenResolutionChangeSignal>`；
        //     ③ 立刻算一次。`OnDisable` 只退订。⇒ **没有任何跟「分辨率无关」之外的语义**。
        //
        // 为什么在我们这儿是恒等：
        //   · 原版画布是 uGUI `CanvasScaler(1920×1080, **Expand**)` ⇒ 参考空间 = `1920 × 1080·s`（s<1 的窄屏变高）
        //     ⇒ 它必须把 itemSize 也乘 s，**才能让卡占屏幕高度的比例恒定**（16:9: 414.4/1080 = 0.3837；
        //     aspect 1.62: 454.9/1185 = 0.3837 —— 同一个数）；
        //   · 我们的设计空间是 **高度钉死 1080 设计 px**（`Core/LayoutSpace.cs`：可见高恒 = `DesignHeight`，
        //     可见宽 = `DesignHeight × 屏幕宽高比`）⇒ 设计 px → 物理 px 的斜率**两轴都是 `Screen.height/1080`**，
        //     与宽高比无关 ⇒ `CardH = 414.4` **已经是**「占屏高 0.3837」那一档，**再乘 s 就多乘了一次**；
        //   · 宽度同理：原版卡宽占**画布宽**的比例 = `535·s / (1080·s·aspect)` = `535/(1080·aspect)`，
        //     而我们是 `535 / (1080·aspect)` —— **同一个式子**；21:9 那侧（s 被 max 夹成 1）也一致
        //     （原版画布此时长在宽上：`1080·aspect × 1080`）。
        //   ⇒ 4 条（`OnEnable` / `ResolutionHasChanged` / `ModifyFlexibleLayout` / `OnDisable`）的**等价实现
        //     = `CardW/CardH/CardHTall/CardRow0Top` 那组常量本身**（`CardRow0Top` 的 doc 已用用户实拍
        //     aspect≈1.62 逐项复核过：预测 148.5 / 卡底 844.6 vs 实测 149 / ≈845）。
        //
        // ⚠️ **残留的那一格不属于这个组件**：窄屏时我们的**可见宽度**变窄（4:3 ⇒ 只看得到 1920 设计 px 里的
        //   1440），右边那两列要滚才看得到；原版同一时刻画布宽仍是 1920、整排都在屏上。那是
        //   `Core/LayoutSpace.cs` 里 `A2092/A2098` 记着的**全局画布模型差**（钉高 vs 钉宽），与本组件无关。
        // ⛔ **别为了「照原版多个组件」再建一个每次都乘 s 的适配器** —— 那会在窄屏把卡放大 1/s 倍、
        //   与 `Editor/MainMenuScene.cs` 的 5 条 `CheckCard`（`205 + 555c / 535 / 414.4 / 848.8 / 182.8`）冲突，
        //   而**画面反而更不像原版**。

        // ============================================================ 启动链的「开机读档」（🆕 2026-10-28 B89-E11c）
        /// <summary>开机读一次「手牌文本」档（原版 `GameStaticData.cardsTextInHandFormat`）。
        /// <para>🔴 **为什么必须由启动链补**（判据两份，都已现读）：
        /// ① 写点 `decomp_full/GraphicsTab__CardsTextInHandDropdownChange.c:35-41` 只做两件事 ——
        ///    `GameStaticData(+0x140) = value`、`(+0x144) = 1`、打一条 `LogWarning` 然后 **return**
        ///    ⇒ **它不重刷任何卡面**；
        /// ② 读点 `decomp_full/BasicCardUI__SetCardTextInHand.c:10-14` = **每张卡自己被建出来时**读 `+0x140`
        ///    并 `CardTextsController.ToggleSimplifiedView(..., +0x140, ...)` ⇒ 原版是「卡面到处自己来拿」。
        /// 我们的等价物 `Core/CardView.HandTextFormat` 是**全局静态、只在 `SetHandTextFormat(...)` 时重刷**
        /// （`CardView.cs:5118`）⇒ 没有「卡自己来拿」那条路，所以**开机必须读一次并应用一次**，
        /// 否则玩家上次选的档要等他自己开一次设置窗才恢复（= 开机/重进游戏后手牌文本退回全文）。</para>
        /// <para>返回读到的档（0 = 全文 · 1 = 仅描述 · 2 = 仅卡名），⛔ 越界由 `TextInHand` 自己夹并出声。</para></summary>
        public static int ReadTextInHandFormatAtBoot()
        {
            int v = SettingsWindow.TextInHand.Value;     // 读档（首次读会走 `TextInHand.Load()`）
            CardView.SetHandTextFormat(v);              // 再应用一次（幂等）：不依赖 `Load()` 的副作用，开机这条路自成一体
            Debug.Log("[MainMenu] 开机读一次手牌文本档：`cardsTextInHandFormat` = " + v
                    + "（0=全文 / 1=仅描述 / 2=仅卡名；判据 `BasicCardUI__SetCardTextInHand.c` 读 +0x140）");
            return v;
        }

        // ============================================================ 类尾（B89-E27 · 账 `L474` / `L411` / `L415` / `L550`）
        //   ⚠️ **一律追加在类尾**：本文件被账按 `文件:行号` 引用，新成员插进中间会让下游行号整体漂移
        //   （CLAUDE.md 那条「新成员一律追加到类尾」）。

        /// <summary>**在期 Parent 活动表**（= 原版 `NavigationPanelController.BuildEventButtons` 循环的那一份）。
        /// <para>判据：① 类必须是 `Parent`（原版这一颗钮由 `ParentEventHandler` 建）；② `GetState(now) == Active`
        /// （`LiveOpsState.IsCurrentlyActive` —— ⛔ 别用 `IsActive()`，它连 `Finished` 也算 true，见
        /// `LiveOpsHandler.SelectInTable` 的 doc）；③ `Selectable`（`enabled &amp;&amp; uploadToLive`，同一处）。
        /// 排序 = `priority` 降序、同权取 `StartMs` 早的（🔶 自己设计：原版循环序没读到，取一个确定的序保证可复现 ——
        /// 调度台 2026-10-28 授权，B89-E27）。</para>
        /// <para>⚠️ 本机 13 份活动 SO 的日期全在 2025 年 ⇒ **今天这个函数恒返回空表**，调用点因此走「静态 EVENT 一颗」
        /// 那条兜底。要让循环真的跑起来，只能给 `LiveOpsLocalCatalog` 加在期数据（或用 `LiveOpsClock.NowProvider`
        /// 把「现在」拨进某场活动）。</para></summary>
        internal static List<LiveOpsLocalEvent> ActiveParentEventsForNav(System.DateTime utc)
        {
            var list = new List<LiveOpsLocalEvent>();
            var table = LiveOpsLocalCatalog.Events;
            for (int i = 0; i < table.Count; i++)
            {
                var e = table[i];
                if (e == null || e.Kind != LiveOpsLocalKind.Parent) continue;
                if (!e.IsCurrentlyActive(utc)) continue;
                if (!e.Selectable) continue;
                list.Add(e);
            }
            list.Sort((a, b) =>
            {
                int p = b.Priority.CompareTo(a.Priority);
                return p != 0 ? p : a.StartMs.CompareTo(b.StartMs);
            });
            return list;
        }

        /// <summary>某个事件在导航钮/模式卡上的标题 = 原版 `TryGetLabel(ev, 4 /*NavigationBarTitle*/) ??
        /// `TryGetLabel(ev, 0 /*Title*/)`（判据：`OpenEventButton__SetButtonInfo` 那一跳，见
        /// <see cref="TryGetEventNavInfo"/> 同族实现）。取不到词条 ⇒ **null**（调用点各自兜底）。</summary>
        internal static string EventNavTitleOf(LiveOpsLocalEvent ev)
        {
            if (ev == null) return null;
            LiveOpsLocalLabel l;
            string term = null;
            if (ev.TryGetLabel(4, out l)) term = l.Term;
            else if (ev.TryGetLabel(0, out l)) term = l.Term;
            if (string.IsNullOrEmpty(term)) return null;
            return LiveOpsLocalCatalog.TermText(term);
        }

        /// <summary>导航钮那颗的文案：取不到事件标题 ⇒ 保留 **`EVENT`** 兜底（**我们挑的**，原版是空态；
        /// 与「没有在期活动」那条同一档，见 `BuildNavigationPanel` 里 ⑥ 的注释）。</summary>
        internal static string EventNavTitleOrFallback(LiveOpsLocalEvent ev)
        {
            var t = EventNavTitleOf(ev);
            return string.IsNullOrEmpty(t) ? "EVENT" : t;
        }

        /// <summary>**建左竖导航那颗「事件钮」**（= 原版 `NavigationPanelController.BuildEventButtons(ParentEventHandler)`，
        /// 账 `L474`）。判据：原版在 `BuildEventButtons` 里对**每个在期 Parent 活动** `Instantiate(eventButtonPrefab)` 一颗
        /// ⇒ 我们这里按 <see cref="ActiveParentEventsForNav"/> 逐颗建。
        /// <para>几何（第 i 颗，i 从 0 起）：顶 = `143.41 + (5 + i) × 153.33` · 高 = `169.70` · 图标顶 = 钮顶 + `1.70`。
        /// 出处：原版 `Navigation Panel/Buttons Container` 的 VLG（`m_Spacing = −16.35`、钮高 169.68 ⇒ 步进 153.33；
        /// 第 6 颗顶 = 143.41 + 5×153.33 = 910.06，与原来写死的 910.0 同一格 —— 逐值见上面调用点那段长注释）。</para>
        /// <para>⚠️ **在期活动 0 颗时仍建一颗静态 `EVENT`** —— 那是本文件 ⑦ 那条**有意偏离**（本机没有活动源，
        /// 验收口径要求「入口必须点得开」），⛔ 别删、也别改成「没有活动就不建」。</para></summary>
        void BuildEventNavButtons(Transform holder)
        {
            var evNavItems = ActiveParentEventsForNav(LiveOpsClock.NowUtc);
            if (evNavItems.Count == 0)
            {
                string evNavTitle; System.DateTime? evNavEnd;
                TryGetEventNavInfo(out evNavTitle, out evNavEnd);
                NavButton(holder, 5, "Event", "40k_main_bt_event",
                          910.0f, 1079.7f, null,
                          140.0f, 12.8f, 152.8f, 911.7f, 24f,
                          nodeName: "Event Navigation Button",
                          label: string.IsNullOrEmpty(evNavTitle) ? "EVENT" : evNavTitle,
                          hiTint: new Color(0.67f, 0.132f, 0.0853f, 1f),
                          iconTint: new Color(0.7358490f, 0.7358490f, 0.7358490f, 1f),
                          timerEndUtc: evNavEnd);
            }
            else
            {
                for (int i = 0; i < evNavItems.Count; i++)
                {
                    float eny1 = 143.41f + (5f + i) * 153.33f;
                    var ev = evNavItems[i];
                    NavButton(holder, 5 + i, "Event", "40k_main_bt_event",
                              eny1, eny1 + 169.70f, null,
                              140.0f, 12.8f, 152.8f, eny1 + 1.70f, 24f,
                              nodeName: "Event Navigation Button",
                              label: EventNavTitleOrFallback(ev),
                              hiTint: new Color(0.67f, 0.132f, 0.0853f, 1f),
                              iconTint: new Color(0.7358490f, 0.7358490f, 0.7358490f, 1f),
                              timerEndUtc: ev.EndUtc > LiveOpsClock.NowUtc ? ev.EndUtc : (System.DateTime?)null);
                }
                Debug.Log("[Menu] 事件钮按原版 `BuildEventButtons` 建了 " + evNavItems.Count + " 颗（在期 Parent 活动）"
                        + " —— 第 i 颗顶 = 143.41 + (5+i)×153.33；判据 = 原版 `NavigationPanelController__BuildEventButtons.c`");
            }
            Debug.Log("[Menu] 事件钮的图标已接：`40k_main_bt_event`（135²，PPU 100，Simple）"
                    + " · 染色照原版插槽 `m_Color` = 0.7358490 灰（比五颗导航钮的白图标暗 26%）"
                    + " —— 判据 = `bundle_liveopsicons_assets_all/Sprite/40k_main_bt_event.json` + "
                    + "`Event Navigation Button/Image` 的 `MonoBehaviour_-1269977380222428235.json`（现读）。");
        }

        /// <summary>= 原版 `LiveopMenuContainer.OnInitialize(LiveOpsEvent playEvent)` + `SetText(LiveOpsEvent)` 的
        /// **数据那半边**（账 `L411` / `L415`）：先问「这一格归哪个在期活动」（<see cref="ModeCardOwnerEvent"/>），
        /// 有归属 ⇒ 标题取该事件的 `TryGetLabel(4) ?? TryGetLabel(0)`、倒计时取该事件的 `EndUtc`；没有归属 ⇒
        /// 两个都保持调用点原来那份（标题 = 我们手挑的那张表；到期 = `null` ⇒ `BuildModeCard` 走原版
        /// `Base Game Mode Container 1x2 No Timer` 变体，⛔ 不编日期、不印 `--`）。</summary>
        void BuildModeCardForCard(Transform parent, string nm, float x, float y, float w, float h, string art,
                                  string title, string kind)
        {
            var owner = ModeCardOwnerEvent(nm, LiveOpsClock.NowUtc);
            // 🔴 标题走**容器自己那条**（`LiveopMenuContainer.SetText` = `GetLabel(ev, Title(0))`，**没有**导航条的
            //    `NavigationBarTitle(4)` 回退；判据 `LiveopMenuContainer.cs:35-43`）—— 原来借用 `EventNavTitleOf`
            //    （4 ?? 0）会把「只有导航条标题」的活动印成卡名，与原版不同（铁律 5 就地订正）。
            string cardTitle = owner != null ? (EventCardTitleOf(owner) ?? string.Empty) : title;
            System.DateTime? end = owner != null && owner.EndUtc > LiveOpsClock.NowUtc ? owner.EndUtc : (System.DateTime?)null;
            // 🆕 B90-N2：原版 `LiveopMenuContainer.OnInitialize` 在拿到 baseData 之后**还会换这张卡的图**
            //   （`backgroundImage.sprite = LoadAsset(baseData, MainMenuContainerBackground)`，
            //    判据 → 类尾 `EventContainerArtOf`）。有归属 ⇒ 用事件的图；取不到 ⇒ 保持建树时那张手挑图。
            string cardArt = owner != null ? (EventContainerArtOf(owner, nm) ?? art) : art;
            // 倒计时**前缀词条**同样来自事件（原版 `InitializeWitLocKey(end, evt.GetTimeDescriptionTextKey())`，
            // 三档 `TimeStartsIn`/`TimeEndsIn`/`TimeFinished`，空串 = 不显示前缀）—— 正本 → `Shell/LiveOpsState.cs` 那份实现。
            BuildModeCard(parent, nm, x, y, w, h, cardArt, cardTitle, kind, ModeViewport, end,
                          owner != null ? owner.GetTimeDescriptionTextKey() : null);
        }

        /// <summary>`EventComponents[MainMenuContainer]`（= 35）的 **GUID → 容器 prefab 根名**。
        /// <para>解析法（本地可解，已实测，⛔ 不是猜的）：`bundle_menus_assets_all/AssetBundle/AssetBundle_1.json`
        /// 的 `m_Container` 给「AssetReference GUID → `m_PathID`」，再按 `GameObject/*.json` 的 `m_Component`
        /// 反查根名（与 `判据/B88_P1两条判据.md` §3.3 同一套两步法；那份表解的是弹窗那 6 个 GUID，这里是**容器**那一个）。</para>
        /// <para>🔴 **B89-E27 本轮实读**（三步，可复跑）：GUID `b28a4a686d3cc4366af8977a6ab69b5e` →
        /// `m_PathID 541940618620306034`（`AssetBundle_1.json` 的 `m_Container`）→ 该 pid 是 GameObject 本体，
        /// 它的组件 `RectTransform_7695131988882162290` → 唯一含该组件的 `GameObject/Base Game Mode Container 1x2
        /// Raid or Army Release.json` ⇒ **根名 = `Base Game Mode Container 1x2 Raid or Army Release`**。
        /// ⚠️ 本机 13 份 SO 里**只有 2 份**带 `MainMenuContainer` 组件（`ExpansionUM1` / `SWRelease` 两个 Parent），
        /// 且**指向同一个 GUID** ⇒ 表里就这一行。⛔ 别把「我们那 5 张手挑的模式卡」硬塞进来。</para></summary>
        static readonly Dictionary<string, string> MainMenuContainerPrefabByGuid = new Dictionary<string, string>
        {
            { "b28a4a686d3cc4366af8977a6ab69b5e", "Base Game Mode Container 1x2 Raid or Army Release" },
        };

        // 🔶 自己设计（非原版）：模式卡 ↔ 事件的**归属**用注入口给 —— 原版的归属是**服务端**下发的
        //   `LiveopsMainMenuData`（每张容器卡自带自己的事件；`LiveopsMainMenuData` 那三格见 `Shell/LiveOpsEvent.cs:1044`），
        //   本机只有「事件 → 容器 prefab」这一半（`EventComponents[MainMenuContainer]`），**没有「我们这 5 张卡
        //   各归哪场活动」**（那 5 张是我们手挑的、已如实标注）⇒ 生产路径靠 `MainMenuContainerPrefabByGuid` 真判，
        //   断言路径用这个口把「某张卡属于某场活动」直接给进去，用来证明 `hasTimerRow` 那一整块**是活的**。
        //   调度台 2026-10-28 授权（B89-E27 · 账 `L411`/`L415`），⛔ 别在生产装配里赋值。
        /// <summary>自检口：`(卡节点名) → 归属事件`；返回非 null 就顶替数据判据。生产恒 `null`。</summary>
        public static System.Func<string, LiveOpsLocalEvent> ModeCardOwnerOverrideForTest;

        /// <summary>**这一张模式卡归哪场在期活动**（= 原版 `LiveopMenuContainer.OnInitialize(LiveOpsEvent playEvent)`
        /// 的入参是怎么来的）。判据链（本地能走的那一半）：
        /// ① 我们的卡节点名 = 「原版 prefab 根名 [+ ` - <模式>` 后缀]」（见 `_placeAllCards` 那五个 `Put`）；
        /// ② 事件侧：`EventComponents[MainMenuContainer(35)]` 的 GUID → <see cref="MainMenuContainerPrefabByGuid"/>；
        /// ③ 两者相等 ⇒ 这张卡由该事件驱动（标题/倒计时/两张图都取该事件的）。
        /// <para>⚠️ **返回值是 null 时不是失败**：原版就有 `1x2 No Timer` 变体（`TextDarkening` 里只剩 `Event Title`），
        /// 我们那 5 张手挑的卡今天全都落这一档（本机没有指向它们的活动）⇒ 调用点保持「不建这一行」。</para></summary>
        internal static LiveOpsLocalEvent ModeCardOwnerEvent(string cardNodeName, System.DateTime utc)
        {
            var hook = ModeCardOwnerOverrideForTest;
            if (hook != null)
            {
                try { return hook(cardNodeName); }
                catch (System.Exception ex) { Debug.LogError("[Menu] `ModeCardOwnerOverrideForTest` 抛异常：" + ex); }
            }
            if (string.IsNullOrEmpty(cardNodeName)) return null;
            int cut = cardNodeName.IndexOf(" - ", System.StringComparison.Ordinal);      // 去掉我们加的 ` - Skirmish` 那种后缀
            string prefab = cut > 0 ? cardNodeName.Substring(0, cut) : cardNodeName;
            var list = ActiveParentEventsForNav(utc);
            for (int i = 0; i < list.Count; i++)
            {
                var comps = list[i].Components;
                if (comps == null) continue;
                for (int c = 0; c < comps.Length; c++)
                {
                    if (comps[c].Key != (int)LiveOpsComponentRef.MainMenuContainer) continue;
                    string name;
                    if (MainMenuContainerPrefabByGuid.TryGetValue(comps[c].Value ?? "", out name) && name == prefab)
                        return list[i];
                }
            }
            return null;
        }

        /// <summary>`OpenWindowButton` / `OpenEventButton` 的 **`closeOnToggleChangeToFalse`**（逐颗现读，⛔ 不是统一 1）。
        /// <para>本轮实读（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/*.json` + `bundle_menus_assets_all`
        /// 的 `Event Navigation Button`）：Home `MB_2556` · Collection `MB_2496` · Shop `MB_2495` · Rewards `MB_2494` ·
        /// Social `MB_2497` · 事件钮（3 份 MB）—— **六颗全是 `1`**。序列仍是**逐颗一张表**：这一格原版本来就是
        /// 逐 prefab 序列化的，将来若某颗读出来是 0，只改这一格即可。</para></summary>
        static readonly bool[] NavCloseOnToggleChangeToFalse = { true, true, true, true, true, true };

        /// <summary>`OpenWindowButton.closeOtherMenus`（同一批 MB 现读）：Home/Collection/Shop/Rewards/Social **全 1**、
        /// 事件钮 **0**（`OpenEventButton` 的 3 份 MB 都是 0）。</summary>
        static readonly bool[] NavCloseOtherMenus = { true, true, true, true, true, false };

        /// <summary>= 原版 `OpenWindowButton.ToggleValueChange(bool)` / `OpenEventButton.ToggleValueChange(bool)`
        /// （体已读：`decomp_full/OpenWindowButton__ToggleValueChange.c` · `…OpenEventButton__ToggleValueChange.c`
        /// —— `option == 0` 且 `closeOnToggleChangeToFalse(+0x41 / +0x29)` 且 `currentWindow(+0x48 / +0x70)` 非空
        /// ⇒ 虚调它的 `Close()`；`option != 0` ⇒ `OpenWindow()`）。
        /// <para>🔴 **这是 `L550` 的落点**。机制（照原版）：这 6 颗是同一个 `ToggleGroup`（`m_AllowSwitchOff = 0`）
        /// ⇒ 点第 j 颗时**第 i 颗被关掉**（`isOn: true → false`）⇒ 那颗的 `onValueChanged` 走 `option == false` 支
        /// ⇒ `closeOnToggleChangeToFalse = 1` ⇒ **关掉它自己那扇窗**。所以「关窗」是由**出去的那一颗**驱动的，
        /// ⛔ 不是由进来的那一颗的 `closeOtherMenus` 驱动的。</para>
        /// <para>⚠️ **我们只补真正缺的那一半**：0..4 那五颗 `closeOtherMenus = 1` ⇒ 进来的那颗自己
        /// `OpenWindow(..., closeAll: true)` 已经把上一扇收掉了（净效果与原版相同，见 `OpenShop`/`OpenCollection`
        /// 的 doc）⇒ 再先关一次只会白走一遍 `ShowPreviousWindow`（把常驻主菜单开/关一轮）。
        /// 缺的是**事件钮那一路**（`closeOtherMenus = 0`）：原来开事件窗时上一扇**不关**，原版会关。</para>
        /// <para>⛔ 不许改成「点已选中的那一颗 ⇒ 关窗」：原版 `EverguildToggle.OnPointerClick` 首段
        /// `!allowClickWhenAlreadySelected(0x139) &amp;&amp; isOn(0x120) ⇒ return`（**整下点击被吃掉**，连
        /// `Toggle.OnPointerClick` 都不跑）⇒ 点当前那一颗**什么都不发生**（我们 `WindowButton` 同一口径）。</para></summary>
        void CloseOutgoingNavWindowOnToggleOff(int nextIdx)
        {
            int prev = SelectedNav;
            if (prev < 0 || prev == nextIdx) return;
            if (prev >= NavCloseOnToggleChangeToFalse.Length) return;
            if (!NavCloseOnToggleChangeToFalse[prev]) return;
            if (nextIdx >= 0 && nextIdx < NavCloseOtherMenus.Length && NavCloseOtherMenus[nextIdx]) return;
            if (!NavItemShown(prev)) return;
            var wm = WindowsManager.Get();
            if (wm == null) return;
            var top = wm.TopWindow;
            if (top == null || top == wm.BaseMenu) return;      // 常驻主菜单不是那五颗/事件钮开的窗 ⇒ 不归这一条管
            // ⚠️ **如实记的近似**：原版关的是「那颗钮**自己**开的窗」（`currentWindow` 字段），我们这里取「最上面那扇」。
            //   最上面那扇是**弹窗**时（提示框/确认框压在导航窗上）⇒ **不关**（此时关掉它会关错对象）；真正的
            //   per-nav 窗口账要等 L434 那批把 `LiveopMenuContainer.currentWindow` 建出来再补。
            if (top.type == WindowType.Popup) return;
            Debug.Log("[Menu] 换导航钮：第 " + (prev + 1) + " 颗的 `closeOnToggleChangeToFalse = 1` ⇒ 关掉它开的那扇 `"
                    + top.name + "`（原版 `OpenWindowButton__ToggleValueChange.c` 的 `option == 0` 支）");
            top.Close();
        }

        // ============================================================================================
        //  🆕 B89-E30（账 `L412` / `L477`）：**两条「重建」跳**（追加在类尾，⛔ 别插到中间去）
        //
        //  · `L412` `LiveopMenuContainer.OnLocalize()` —— 原版换语言时由 `LocalizationManager` 的
        //    `OnLocalizeCallback` 打到每颗活动容器上，容器**自己**把 title / 倒计时重设一遍。
        //    我们这侧：左栏 6 颗导航（`BuildNavigationPanel` 里字是 `Loc.T` 现查的）与模式卡标题/时间
        //    （`_placeAllCards` 里同样是现查）**都在设置窗后面活着**，所以换语言之后要**重建**这两处。
        //    广播口 = `Core/Loc.LanguageChanged`（`SetLanguage` 真改了值才发）。
        //  · `L477` `NavigationPanelController.OnEventsRefreshed()` —— 原版活动刷新之后重建活动钮。
        //    我们这侧的注入口一直在（`Shell/LiveOpsFilter.cs` 的 `LiveOpsParentEventRefresh.OnEventsRefreshed`，
        //    `ParentEventHandler.BeforeRefresh` 在「全部不在期」时调它），但**从来没有真订户**
        //    ⇒ 今天它只会打一条「本仓没有等价物」的告警。这里把它接上。
        //
        //  🔴 为什么落在**本类**而不是 `LiveOpsFilter.cs`：那两份数据（导航面板 / 模式卡内容）都是**本类实例的**，
        //    静态钩子里拿不到别的宿主 ⇒ 按「谁拥有那棵树谁订」订（形态照 `Battle/BattleHudLanguage.cs`：
        //    `-=`/`+=` 幂等订 + 销毁时摘）。⛔ 别改成「静态事件里满地找实例」。
        //  ⚠️ **批处理里 `Awake` 不跑** ⇒ 只能挂在 `Build()`（自检与 Play 走同一条路），所以订阅点在
        //    `BuildNavigationPanel` 那一行（同一行两个语句，为的是**不动下游账落点行号**）。
        // ============================================================================================

        /// <summary>把换语言那条订阅订上（**幂等**：先摘后挂，重建菜单不会攒出第二份）。
        /// <para>⚠️ **只订 `Loc.LanguageChanged`**：`L477` 那条（活动刷新）走的是
        /// `LiveOpsParentEventRefresh.OnEventsRefreshed` 的**静态默认实现**
        /// （`Shell/LiveOpsFilter.cs` 的 `OnEventsRefreshedDefault` → 本类的
        /// <see cref="RebuildNavForLiveOpsRefresh"/>）—— 那是个**单值 Action**，在这里 `+=`
        /// 会与默认实现串成两条 ⇒ **一次刷新重建两遍**。⛔ 别再往这里加。</para></summary>
        void HookMenuRebuildBroadcasts()
        {
            Loc.LanguageChanged -= OnLanguageChangedRebuild;
            Loc.LanguageChanged += OnLanguageChangedRebuild;
        }

        /// <summary>摘掉换语言那条订阅（`OnDestroy` 那一跳）。⚠️ **不置空整个事件** —— 别的宿主可能也订过。</summary>
        void UnhookMenuRebuildBroadcasts()
        {
            Loc.LanguageChanged -= OnLanguageChangedRebuild;
        }

        /// <summary>账 `L412`：换语言 ⇒ 重建左栏 + 模式卡标题/时间。
        /// <para>⚠️ 本方法**不判断「语言有没有变」** —— 那道闸在 `Loc.SetLanguage`（值没变就早退、不发广播）。</para></summary>
        void OnLanguageChangedRebuild()
        {
            bool nav = RebuildNavPanel("换语言");
            bool cards = RebuildModeCardsForLanguage();
            _languageRebuilds++;
            Debug.Log("[Menu] 换语言 ⇒ " + (nav ? "左栏重建 1 次" : "左栏**没建**（菜单树不在）")
                    + " + " + (cards ? "模式卡标题/时间重建 1 次" : "模式卡**没建**")
                    + "（原版 `LiveopMenuContainer.OnLocalize` 一路；重建后的字全部由 `Loc.T` 现查）");
        }

        /// <summary>账 `L477`：活动刷新 ⇒ 重建左栏（原版 `NavigationPanelController.OnEventsRefreshed`）。
        /// <para>🔴 **调用方 = `Shell/LiveOpsFilter.OnEventsRefreshedDefault`**（那个钩子的静态默认实现，
        /// 调时才用 `Instance` 解析到本对象）⇒ ⛔ 别再在本类里 `+=` 订阅一次（会重建两遍）。</para>
        /// <para>⚠️ 原版还带一条「把导航点回主菜单」的跳，那一条走同一个钩子对象的 `SelectMainTab`
        /// （`LiveOpsParentEventRefresh` 里已经有默认实现），⛔ 不在本方法里再做一遍。</para></summary>
        public void RebuildNavForLiveOpsRefresh()
        {
            bool nav = RebuildNavPanel("LiveOps 活动刷新");
            _liveOpsNavRebuilds++;
            if (nav)
                Debug.Log("[Menu] 活动刷新 ⇒ 左栏重建 1 次（原版 `NavigationPanelController.OnEventsRefreshed`；"
                        + "第 6 颗活动钮按当前在期 Parent 事件重建，见 `BuildEventNavButtons`）");
        }

        /// <summary>把左栏整棵重建（= 原版 `NavigationPanelController` 那两条「刷新后重建」的落地）。
        /// <para>⚠️ **Play 下旧面板是 `Destroy`（帧末才真销毁）** ⇒ 同一帧里场上会短暂有两套同名节点；
        /// `_navPanel` 已指向新的那一套（`SetNavPanelVisible` / `NavPanelXForTest` 读的都是它）。
        /// 这与 `Build()` 自己那轮「先清后建」同一条语义（那里也是 `DestroySafe`）。</para>
        /// <returns>`false` = 菜单树还没建（或取不到父件）⇒ **已出声**，什么都没做。</returns></summary>
        bool RebuildNavPanel(string why)
        {
            if (_navPanel == null)
            {
                Debug.LogWarning("[Menu] " + why + "：要重建左栏，但 `Navigation Panel` 还没建（菜单树不在这一场）⇒ 跳过（⛔ 不静默）");
                return false;
            }
            var parent = _navPanel.parent;
            if (parent == null)
            {
                Debug.LogWarning("[Menu] " + why + "：`Navigation Panel` 没有父件 ⇒ 重建不了（节点树被摘过？）");
                return false;
            }
            var old = _navPanel.gameObject;
            _navPanel = null;
            DestroySafe(old);
            BuildNavigationPanel(parent);      // 内部会重建 6 颗钮的命中区（末尾 `RefreshNavHitQueue`）
            return true;
        }

        /// <summary>模式卡那一族换语言：标题 / 倒计时都是 `Loc.T` + 归属活动**现查**的
        /// （见 `_placeAllCards` 与 `BuildModeCardForCard`）⇒ 最省的一步就是**照滚动重建那条路整片重摆**。
        /// <returns>`false` = 模式卡内容还没建。</returns></summary>
        bool RebuildModeCardsForLanguage()
        {
            if (_modeContent == null || _placeAllCards == null) return false;
            RebuildModeCards();                // 销毁 Content 子件 → `_placeAllCards()`（与滚轮那条同一个口）
            return true;
        }

        int _languageRebuilds, _liveOpsNavRebuilds;

        /// <summary>自检读口：换语言重建跑过几次（账 `L412`）。
        /// 用法：`Loc.LanguageChanged` 有人订 ⇒ 先断订户 ≥ 1，再 `Loc.SetLanguage(另一种)` ⇒ 本数 +1。</summary>
        public int LanguageRebuildsForTest { get { return _languageRebuilds; } }
        /// <summary>自检读口：活动刷新重建跑过几次（账 `L477`）。</summary>
        public int LiveOpsNavRebuildsForTest { get { return _liveOpsNavRebuilds; } }

        // ============================================================ 类尾（B90-N2 · 账 `个人资料 271`）
        //   ⚠️ **一律追加在类尾**：本文件被账按 `文件:行号` 引用，新成员插进中间会让下游行号整体漂移。

        /// <summary>名次（`Position #` / `Position Value`）那两颗**要不要显示**。
        /// <para>= 原版 `RankedMenuContainer.Refresh` 里算 `TogglePositionDisplay` 入参的那一格
        /// （`option = currentStats.position != 0 &amp;&amp; 符号一致`）。</para>
        /// <para>🔴 **本地恒 `false`**：名次号是玩家数据（服务器），本地一份都没有 ⇒ 恰好走原版
        /// 「`position == 0` ⇒ 两颗关着」那一支（⛔ 不是「我们选了 0」，是数据不可得 —— 已出声）。
        /// ⚠️ 留成**实例字段**只为自检能证明「两颗**会**跟着开」（置 `true` 后走同一条建树路）。</para></summary>
        public bool RankedPositionKnown;

        Transform _rankedPosHash, _rankedPosValue;

        /// <summary>= 原版 `RankedMenuContainer.TogglePositionDisplay(bool option)`（**逐句**，
        /// 判据 → `d:/2/tools/decomp_full/RankedMenuContainer__TogglePositionDisplay.c`）：
        /// `foreach (go in legendaryRankDivisionInfoObjects) go.SetActive(option);`
        /// <para>⚠️ 那两个成员**实读只指向 `Position #` 与 `Position Value`**（`legendaryRankDivisionInfoObjects`
        /// 名字唬人，**不是**印玺那一排，也不是某个叫 `Legendary` 的节点）。</para>
        /// <para>⚠️ 原版 `ToggleLegendaryRankInfo` 也动这一对（**原版就是重复的**）⇒ 那一条走
        /// `ApplyRankedRatingBranch` 对整支 `Global Rating` 的 `SetActive`（父关子藏）⇒ ⛔ 别「优化」成一份。</para></summary>
        public void TogglePositionDisplay(bool option)
        {
            if (_rankedPosHash != null && _rankedPosHash.gameObject.activeSelf != option)
                _rankedPosHash.gameObject.SetActive(option);
            if (_rankedPosValue != null && _rankedPosValue.gameObject.activeSelf != option)
                _rankedPosValue.gameObject.SetActive(option);
        }

        /// <summary>模式卡（原版 `LiveopMenuContainer`）那张卡的标题 = 原版
        /// `LiveOpsAssetUtility.GetLabel(liveOpsEvent, EventLabelReferenceType.Title /* = 0 */)`，
        /// 判据 → `out_cpp2il_dev_ilspy/LiveopMenuContainer.cs:35-43`（`SetText`）·
        /// `Everguild/LiveOps/EventLabelReferenceType.cs:5`（`Title = 0`）。
        /// <para>🔴 **与 <see cref="EventNavTitleOf"/> 的区别是【没有 `NavigationBarTitle(4)` 回退】** ——
        /// 那一条属于 `OpenEventButton.SetButtonInfo` / 导航钮（另一个入口，见 `NavigationPanelController.cs:160` 的 `BuildEventButtons`）。
        /// 原来这两条共用一个函数 ⇒ 「只有导航条标题、没有 Title」的活动会把导航条文案印到模式卡上（与原版不同）。</para>
        /// <para>取不到 ⇒ **null**（原版 `title.text = null` ⇒ 那行是空的，⛔ 不拿我们的手挑卡名去顶）。</para></summary>
        internal static string EventCardTitleOf(LiveOpsLocalEvent ev)
        {
            if (ev == null) return null;
            LiveOpsLocalLabel l;
            if (!ev.TryGetLabel(0, out l)) return null;
            if (string.IsNullOrEmpty(l.Term)) return null;
            return LiveOpsLocalCatalog.TermText(l.Term);
        }

        // ---------------------------------------------------------------- 🆕 B90-N2：模式卡的事件图（账 `菜单 411`）

        static readonly HashSet<string> _containerArtWarned = new HashSet<string>();

        /// <summary>`MainMenuContainerBackground` 的 **GUID → 已导入的图名**。
        /// <para>只收本机真的解出来、且**已导进** `Resources/Art/ui_menu/` 的那两张。判据（现读）：
        /// `判据/B9_有登记无实物_全量.tsv:1906` = `93cda5b0…` → `liveopsmenuimages_assets_all/UM Guilliman Container 1x2`
        /// · `:1951` = `ad9725ac…` → `…/Space Wolves Container 1x2`；两张都 1024²、`Sprite/*.json` 的
        /// `m_Rect` = 整图（⇒ **不裁**），原图已拷成 `Resources/Art/ui_menu/{UM_Guilliman_Container_1x2,Space_Wolves_Container_1x2}.png`
        /// （源 = `assets_full/bundle_liveopsmenuimages_assets_all/Texture2D/*.png`，**只读**，⛔ 没动 `d:/2`）。</para>
        /// <para>⚠️ 这张表**本该并进** `Shell/OfferArt.cs` 的 `KnownEventArt`（全工程唯一那一份「GUID → 图名」表）——
        /// 那个文件不在本代理白名单，暂落在这里；合并时一并搬过去（⛔ 别在两处长期并存）。</para></summary>
        static readonly Dictionary<string, string> MainMenuContainerArtByGuid = new Dictionary<string, string>
        {
            { "93cda5b09dc454964bd61a51c4cc2606", "UM_Guilliman_Container_1x2" },
            { "ad9725ac046204b7fbdee84edc37f65c", "Space_Wolves_Container_1x2" },
        };

        /// <summary>= 原版 `LiveOpsAssetUtility.LoadAsset(baseData, EventAssetReferenceType.MainMenuContainerBackground)`
        /// 那一跳（`out_cpp2il_dev_ilspy/LiveopMenuContainer.cs:88-104`：先写 `backgroundImage.sprite`，
        /// 紧接着写 `foregroundImage.sprite`）。返回**已导入的图名**；`null` ⇒ 调用点保持建树时那张手挑图。
        /// <para>⚠️ **不许静默**：三种取不到各出声一次（活动没有这一格资产 / GUID 没登记 / 图名导了但 `Resources` 里没有）。</para>
        /// <para>⚠️ `foreground` 那一格本地**一份都没有**（13 份 SO 的 `EventAssets` 逐条现读）⇒ 只出声、不画（⛔ 不编图）。</para></summary>
        internal static string EventContainerArtOf(LiveOpsLocalEvent ev, string cardName)
        {
            if (ev == null) return null;
            var bg = ev.FindAsset((int)LiveOpsAssetRef.MainMenuContainerBackground);
            if (bg == null)
            {
                if (_containerArtWarned.Add("none:" + ev.EventId))
                    Debug.Log("[Menu] 模式卡 `" + cardName + "` 归属活动 `" + ev.EventId + "`：没有 `MainMenuContainerBackground`"
                            + " 这一格资产 ⇒ 保持建树时那张手挑卡图（原版此处 `LoadAsset` 拿不到、sprite 也不会被换）。");
                return null;
            }
            string n = null;
            if (!string.IsNullOrEmpty(bg.Guid)) MainMenuContainerArtByGuid.TryGetValue(bg.Guid, out n);
            if (string.IsNullOrEmpty(n)) n = OfferContainer.ArtForAsset(bg);      // 中央表（`Shell/OfferArt.cs`）
            if (string.IsNullOrEmpty(n))
            {
                if (_containerArtWarned.Add("guid:" + ev.EventId + ":" + (bg.Guid ?? "<null>")))
                    Debug.LogWarning("[Menu] 模式卡 `" + cardName + "` 的 `MainMenuContainerBackground` GUID `"
                                   + (bg.Guid ?? "<null>") + "` 查不到已导入的图名 ⇒ 保持手挑卡图（⛔ 不拿别的图顶替）。");
                return null;
            }
            if (CardArt.MenuUi(n) == null)
            {
                if (_containerArtWarned.Add("tex:" + n))
                    Debug.LogWarning("[Menu] 模式卡事件图 `" + n + "` 不在 `Resources/Art/ui_menu/` ⇒ 退回手挑卡图"
                                   + "（导入口：`assets_full/bundle_liveopsmenuimages_assets_all/Texture2D/<原名>.png`）。");
                return null;
            }
            if (ev.FindAsset((int)LiveOpsAssetRef.MainMenuContainerForeground) == null
                && _containerArtWarned.Add("fg:" + ev.EventId))
                Debug.Log("[Menu] 模式卡 `" + cardName + "`：原版同一跳还会换 `MainMenuContainerForeground`"
                        + "（`LiveopMenuContainer.cs:97-104`），本地活动数据里**没有这一格** ⇒ 不画前景层（⛔ 不编图）。");
            if (_containerArtWarned.Add("ok:" + ev.EventId))
                Debug.Log("[Menu] 模式卡 `" + cardName + "` 改用归属活动 `" + ev.EventId
                        + "` 的 `MainMenuContainerBackground` = `" + n + "`（原版 `LiveopMenuContainer.OnInitialize` 那一跳）。");
            return n;
        }

        // ============================================================ 🆕 2026-10-28（B92-N4 批）
        // 🔴 纪律：追加在类尾（本文件被账按行号引用 —— 既有落点最高到 `:4036` 一带，本段全在其后 ⇒ 零漂）。
        // ⛔ 也别在这里加 `using`：加一行 `using` 会把**整份文件**行号 +1，账里几十条落点一起漂。

        /// <summary>= 原版 `PlayerDataManager.QuitApplication()`（**逐跳**，判据
        /// `decomp_full/PlayerDataManager__QuitApplication.c` ＋ `…__FullyQuitApplication.c`，0.5 秒常量
        /// `DAT_1834b2bb4` 实读 = `0.5f`）。
        /// <para>原版三跳：① 若 `helpshiftManager` 非空 ⇒ 调它的**空方法**（`RVA 0x4B33B0` = 全局共享空桩
        /// ⇒ **什么都不做**，这一跳永远不需要实现）；② `photonManager.LeaveRoom()`（离开联机房间）；
        /// ③ `StartCoroutine(FullyQuitApplication())`（= 等 0.5 秒再 `Application.Quit()`）。</para>
        /// <para>⚠️ **残差一跳（如实记）**：② 的对位原语是 `BattleDriver.LeaveNetRoom`（`private`，
        /// `Battle/BattleDriver.cs:12833`）与 `NetSession.Close(say:false, …)`，两者都在本件白名单之外
        /// ⇒ 这里**只能出声**（红线：⛔ 不许静默略过）。等 Net/战斗线给一个公开包装后，把
        /// <see cref="QuitApplication"/> 里那句警告换成那一行调用即可（别的都不用动）。</para>
        /// <para>⚠️ 没有宿主实例时（例如卡组编辑那一屏没有 `MainMenuRuntime`）⇒ 出声并**直接退**
        /// （原版那条协程挂在常驻单例上，本仓没有对应常驻物 ⇒ 只能如实降级，⛔ 不假装等过 0.5 秒）。</para></summary>
        public static void QuitApplication()
        {
            // ① 原版 `helpshiftManager` 那一跳 = 空桩（RVA 0x4B33B0 `ret 0`）⇒ 不做任何事。
            // ② `photonManager.LeaveRoom()` —— 本件白名单外，出声。
            bool inRoom = false;
            try { inRoom = NetRuntime.Instance != null && NetRuntime.Instance.Session != null
                         && NetRuntime.Instance.Session.State != CardPresentation.Net.NetState.Off; }
            catch { /* 联机层未初始化 ⇒ 当作没有房间 */ }
            if (inRoom)
                Debug.LogWarning("[Menu] 退出前应「先离开联机房间」（原版 `QuitApplication` 第②跳 "
                               + "`photonManager.LeaveRoom()`）—— 这一跳的落点 `BattleDriver.LeaveNetRoom` / "
                               + "`NetSession.Close` 都在本件白名单外 ⇒ **本批没做**（如实出声，⛔ 不是静默略过）");
            // ③ `StartCoroutine(FullyQuitApplication())`
            var host = Instance != null ? Instance : InstanceOrFind();
            if (host != null && host.isActiveAndEnabled)
            {
                host.StartCoroutine(host.FullyQuitApplication());
                return;
            }
            Debug.LogWarning("[Menu] 找不到 `MainMenuRuntime` 宿主 ⇒ 走不了「等 0.5 秒再退」那条协程"
                           + "（原版 `StartCoroutine(FullyQuitApplication())`）—— 直接退");
            Application.Quit(0);
        }

        /// <summary>= 原版 `PlayerDataManager.FullyQuitApplication()`（**逐句**，判据
        /// `decomp_full/PlayerDataManager__FullyQuitApplication.c`：状态机 `MoveNext` 体 =
        /// `yield return new WaitForSeconds(DAT_1834b2bb4)`（实读 = **0.5f**）⇒ `Application.Quit()`）。</summary>
        public System.Collections.IEnumerator FullyQuitApplication()
        {
            Debug.Log("[Menu] `FullyQuitApplication`：等 0.5 秒再 `Application.Quit()`"
                    + "（原版 `PlayerDataManager.FullyQuitApplication`，`DAT_1834b2bb4` = 0.5f）"
                    + (Application.isEditor ? "（⚠️ **编辑器里 `Application.Quit` 是空操作** —— 只有 build 出来的 player 里才真退）" : ""));
            yield return new WaitForSeconds(0.5f);      // 原版 `d__450.MoveNext` 的那一跳
            Application.Quit();
        }

        // ============================================================ 🆕 2026-10-28（B92-N4c 批）
        // 🔴 纪律：追加在类尾（本文件被账按行号引用 —— 既有落点最高到 `:4249`，本段全在其后 ⇒ 零漂）。

        /// <summary>排位卡那一排「段位印玺」的一颗（`Empty` / `Fill` 两枚已建好的 quad）。
        /// <para>🆕 2026-10-28（B92-N4c）：它让那一排的「逐颗点亮」能走**共用件**
        /// <see cref="MilestonesStepDisplay.Initialize"/>（= 原版 `MilestonesStepDisplay.Initialize` 的算法核）——
        /// 原来那颗循环里手写了一遍 `SealToggle(empty, fill, i &lt; 已完成数)`，与
        /// `RankedDivisionChangeWindow.ApplySeals` 各写一份。⚠️ 我们的「prefab」是**烘死的绝对框 + 已建好的 quad**，
        /// 不是 Unity prefab ⇒ 传进去时 `instantiate` 用恒等委托、建件/销毁仍留在调用点。</para></summary>
        sealed class RankedSealPair
        {
            public ImageQuad Empty, Fill;
        }

        readonly List<RankedSealPair> _rankedSealPairs = new List<RankedSealPair>();

        // ============================================================ 账 546 / 549 · 活动钮：红点类型登记 + 倒计时按秒泵
        //   （2026-10-28 · B93_D3；⚠️ **一律追加在类尾** —— 本文件挂着 70 处 `文件:行号` 落点）
        //
        // 【账 546 的判据 —— `decomp_full/OpenEventButton__Initialize.c`（84 行，逐句现读）】
        //   `this+0x68 = liveOps;` → 绑 toggle/catcher 的监听 → **若 liveOps != null**：
        //     `t = 反射取 liveOps 的「通知类型」那一格` → `UiBadgeNotification.AddNotificationType(this+0x60, t)`
        //   → `SetButtonInfo()`。
        //   ⇒ 我们缺的就是**中间那一句**：活动钮的 `badgeNotification` 在**运行时**被补上一条通知类型
        //     （prefab 里那 6 颗导航钮的 `notificationDefinitions` 是**烘死**的，只有 Home/Collection/Shop/Rewards/Social
        //      五条 —— 见 `Shell/BadgeNotifications.cs` 文件头那张表 ⇒ EVENT 那颗**出厂没有红点定义**，
        //      它靠 `OpenEventButton.Initialize` 这一句在运行时补）。
        //
        // 【账 549 的三条残差里，本段收掉的是第 ② 条「倒计时只在建树那一刻算一次」】
        //   原版 `TimerDisplay.Update` 按帧刷（`targetFrameRate` 那一档）；我们原来建一次就定死 ⇒ 跨过整分钟
        //   屏上会停旧值。现在改成**按秒泵**（`StepEventNavTimers`，由 `Update()` 那条同一帧泵带起来 ——
        //   零新增行：那句改成了一行内联调用）。
        //   ⚠️ 仍是 🔶：① **剩余时间格式串**（`Xd Yh Zm`，原版 `TimeHelper.FormatTimeSpan` 体未解，判据只有 prefab
        //   样例串 `5d 20h 15m`）；③ **overIcon**（`LoadAsset(ev,9)`）本机无图 ⇒ 照旧出声、不建。

        struct EventNavTimer { public Transform Button; public System.DateTime EndUtc; }
        static readonly List<EventNavTimer> _eventNavTimers = new List<EventNavTimer>();
        static float _eventNavTimerAcc;

        /// <summary>把一颗活动钮的倒计时登记进按秒泵（`NavButton` 里那句一行内联调用）。
        /// ⚠️ 不持有 `Label` 引用：`Build()` 会 `MenuDraw.ClearChildren` 重建整颗钮 ⇒ 泵里按名字
        /// （`Timer Text`）现查、查不到就**静默摘掉**那一格（那是重建后的旧节点，不是错误）。</summary>
        static void RegisterEventNavTimer(Transform button, System.DateTime endUtc)
        {
            if (button == null) return;
            _eventNavTimers.Add(new EventNavTimer { Button = button, EndUtc = endUtc });
        }

        /// <summary>按秒推进那一排倒计时（`Update()` 每帧调，内部自节流到 1 Hz）。
        /// 判据 = 原版 `TimerDisplay.Update`（同一套「把剩余时间写进 `Timer Text`」的语义）。</summary>
        static void StepEventNavTimers() { StepEventNavTimers(Time.deltaTime); }

        /// <summary>按秒泵的真身（`dt` 由调用方给 —— 自检可以一次推几秒，⛔ 不用等帧）。</summary>
        static int StepEventNavTimers(float dt)
        {
            if (_eventNavTimers.Count == 0) return 0;
            _eventNavTimerAcc += dt;
            if (_eventNavTimerAcc < 1f) return 0;
            _eventNavTimerAcc = 0f;
            int updated = 0;
            for (int i = _eventNavTimers.Count - 1; i >= 0; i--)
            {
                var it = _eventNavTimers[i];
                if (it.Button == null) { _eventNavTimers.RemoveAt(i); continue; }   // 重建过的旧节点 ⇒ 摘掉
                var node = it.Button.Find("Timer Text");
                var lb = node != null ? node.GetComponent<Label>() : null;
                if (lb == null) { _eventNavTimers.RemoveAt(i); continue; }
                string want = NavTimerText(it.EndUtc);
                if (lb.Text != want) { lb.SetText(want); updated++; }   // 只在真的变了才写（TMP 每帧刷是纯浪费）
            }
            return updated;
        }

        /// <summary>活动钮的红点定义：原版那 6 颗导航钮里 EVENT 那颗**出厂没有定义**，由
        /// `OpenEventButton.Initialize` 运行期 `AddNotificationType` 补一条（判据见上）。
        /// <para>🔶 自己设计（非原版）：**通知类型本身**在本地没有对等物 —— 原版那个 `Type` 由**远端活动数据**
        /// 给（`OpenEventButton__Initialize.c` 里那句反射取值），本机 13 份活动 SO 里没有那一格 ⇒ 我们定义
        /// `EventNavBadge` 一类，数据源 = 本地活动目录里**在期且可选中**的 Parent 事件（每场一条）。
        /// 为什么必须自定：判据在远端 liveops 数据里，关服后本地拿不到 —— 按铁律 18·0 这一格属于「与服务器有关」
        /// ⇒ 允许本地模拟 · 调度台 B93 批次 2026-10-28 授权。</para></summary>
        static List<BadgeNotificationDefinition> NavBadgeDefs(int idx)
        {
            var defs = BadgeWiring.DefinitionsFor(NavBadgeKey(idx));
            if (idx >= 5)
            {
                EnsureEventNavBadgeProvider();
                defs.Add(new BadgeNotificationDefinition(typeof(EventNavBadge)));
            }
            return defs;
        }

        static EventNavBadgeProvider _eventNavBadgeProvider;

        /// <summary>登记「活动钮红点」的 provider（幂等）。</summary>
        static void EnsureEventNavBadgeProvider()
        {
            if (_eventNavBadgeProvider != null) return;
            _eventNavBadgeProvider = new EventNavBadgeProvider();
            UiBadgeNotificationManager.Register(_eventNavBadgeProvider);
        }

        /// <summary>给**活动窗左栏那几颗事件页签**用的同一条口（`Shell/ParentEventWindow.CreateEventTab` 里那句
        /// `AddNotificationType` 的等价物）—— 原版活动钮与事件页签取的是**同一个**「活动数据的通知类型」
        /// （`OpenEventButton__Initialize.c` / `BaseParentEventWindow__CreateEventTab.c` 同一句反射取值）
        /// ⇒ provider 只登记一次、两边共用（⛔ 别在 `ParentEventWindow` 再建一条）。</summary>
        internal static void EnsureEventNavBadgeProviderForTabs() { EnsureEventNavBadgeProvider(); }

        /// <summary>自检用：摘掉活动钮红点 provider（生产不收）。</summary>
        public static void DetachEventNavBadgeProviderForTest()
        {
            if (_eventNavBadgeProvider == null) return;
            UiBadgeNotificationManager.Unregister(_eventNavBadgeProvider);
            _eventNavBadgeProvider = null;
        }

        /// <summary>自检口：第 <paramref name="idx"/> 颗导航钮的红点定义里有没有 `EventNavBadge`
        /// （= 原版 `OpenEventButton.Initialize` 那句运行期 `AddNotificationType`）。</summary>
        public static bool NavBadgeHasEventBadgeForTest(int idx)
        {
            var defs = NavBadgeDefs(idx);
            for (int i = 0; i < defs.Count; i++)
                if (defs[i] != null && defs[i].notificationType == typeof(EventNavBadge)) return true;
            return false;
        }

        /// <summary>自检口：把一颗「假活动钮」（带 `Timer Text` 子件的节点）登记进按秒泵。</summary>
        public static void RegisterEventNavTimerForTest(Transform button, System.DateTime endUtc)
        {
            if (button == null) return;
            _eventNavTimers.Add(new EventNavTimer { Button = button, EndUtc = endUtc });
        }

        /// <summary>自检口：推一次按秒泵（返回真的被改写的字数 —— 用它断「泵是活的」而不是只断纯函数）。</summary>
        public static int StepEventNavTimersForTest(float dt) { return StepEventNavTimers(dt); }

        /// <summary>自检口：剩余时间串（`NavTimerText` 是 `internal`，编辑器是另一个程序集 ⇒ 要一条公开转发）。</summary>
        public static string NavTimerTextForTest(System.DateTime endUtc) { return NavTimerText(endUtc); }


        /// <summary>主菜单起来时把「上一场练习局」的结算吃进战绩（= 原版 `PracticeEvent.OnMatchFinished`
        /// 在服务器回调里做的那件事的本地等价物；账 584/585）。数据源 = `BattleLogData.All`。
        /// <para>⚠️ 为什么挂在**主菜单 Start**：练习局打完会切回主菜单（`BattleDriver` 那侧写对局记录），
        /// 那一刻正好是原版「服务器回调到了」的对应时刻；⛔ 不每帧扫（`Update` 里那条泵只管倒计时）。</para></summary>
        static void IngestPracticeResultsAtMenuStart()
        {
            var h = LiveOpsManager.GetHandler<GameModesHandler>();
            if (h == null)
            {
                Debug.LogWarning("[Menu] 练习战绩回灌：取不到 `GameModesHandler`（`LiveOpsManager.GetHandler<GameModesHandler>()` = null）"
                               + " ⇒ 这一轮没有回灌（出声，不静默）");
                return;
            }
            int n = h.IngestPracticeResults();
            Debug.Log("[Menu] 练习战绩回灌：吃了一 " + n + " 场（账 584/585 · 数据源 = `BattleLogData.All`）");
        }
    }

    // ============================================================ 活动钮红点（类型 + provider）
    // 🔶 自己设计（非原版）类型：原版这条通知类型由**远端活动数据**给（`OpenEventButton__Initialize.c` 那句反射取值），
    //    关服后本地拿不到 ⇒ 我们定义一类，数据源 = 本地活动目录里的在期 Parent 事件 · 调度台 2026-10-28 授权（账 546）。
    /// <summary>「有在期活动」那条红点（活动钮 / 活动窗左栏的事件钮共用一类 —— 原版这两处取的是**同一个**
    /// 「活动数据的通知类型」，见 `OpenEventButton__Initialize.c` 与 `BaseParentEventWindow__CreateEventTab.c`）。</summary>
    public sealed class EventNavBadge : BadgeNotification { }

    /// <summary>活动钮红点的数据源：**本地活动目录**里每一场「在期且可选中」的 Parent 事件发一条。
    /// ⚠️ 13 份 SO 今天全部过期 ⇒ 恒 0 条（= 红点恒隐藏）；这不是「写死 false」，判据 = `LiveOpsLocalCatalog`
    /// 的 `IsCurrentlyActive` + `Selectable`（同一处口径见 `MainMenuRuntime.ActiveParentEventsForNav`）。</summary>
    public sealed class EventNavBadgeProvider : INotificationProvider
    {
        public System.Type NotificationType { get { return typeof(EventNavBadge); } }
        public IEnumerable<INotificationProvider> ChildProviders { get { return null; } }

        public IEnumerable<BadgeNotification> CheckNotification()
        {
            var evs = MainMenuRuntime.ActiveParentEventsForNav(LiveOpsClock.NowUtc);
            for (int i = 0; i < evs.Count; i++) yield return new EventNavBadge();
        }
    }
}
