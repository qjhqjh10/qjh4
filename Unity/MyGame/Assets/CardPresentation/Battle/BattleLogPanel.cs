// ============================================================================
// BattleLogPanel.cs — 原版的「墓地 / 战斗日志」面板（`CemeteryLogPanel`）。
//
// 入口是**敌方名牌上的那颗按钮**（`ShowCemeteryBtn`，图 `40k_UI_bt_battlelog`）——
// 原版点它就把这块面板从左边缘划出来，里面一行一个动作。
//
// 原版规格（出处：`资料/战斗规格/战斗重建_0827/子代理读报_back左区_0827.md:79,232`；
// 切片尺寸取自 `Resources/Art/ui/` 里那几张 `40k_battlelog_*`）：
//   · 面板   `CemeteryLogPanel`：`LeftArea` 直子，anchor **(0,0.187)-(0,0.793)**、pivot (0,0.5)，
//            **静态 pos x = −1200（收在屏幕外）**，运行时划出来。宽 **794.1 px**、高 = (0.793−0.187)×1080 = **654.5**
//   · 压暗   `shade` 7020×4544 **黑 55%**（远大于屏幕，就是铺满）
//   · 底     `BG` 769.5×454.4，色 **(0, 0.08, 0.01, 1)**（近黑的墨绿）—— 是一个**没有 sprite 的 Image**
//   · 边框   四条 `40k_battlelog_frame_{TOP,Bottom,Left,Right}`（切片 740×68 / 740×49 / 98×601 / 66×608）
//   · 行     `CemeteryActions` 748×431.3，十行 `CemeterySliderUI`（图 `40k_battlelog_display_neutral` 653×43）
//            + `ActionText` **fs 30**（Asar，Almost White #EEEEEE）
//   · 动作类型 `CemeteryActionType`：近战/远程/出牌/技能/抽陷阱/密令/展示/路标石/伏击
//
// ⚠️ **一条如实说明（别当成原版）**：
//   ① 四条边框怎么拼的**没查实**（原版 `Frame` 节点的 863×1032.5 与面板 794.1 对不上，
//      可能是外扩的装饰边框）。我们按「四张图围住底板」拼，各图按自身比例缩到面板边上。
//   ✅ **2026-09-29 已删掉的两处「我们自己加的」**：
//     · 每行那枚 **30 px 小头像**（用户 2026-09-28 拍板「删掉、照原版」）—— 原版每行**只有文字**，
//       文字在行内从 **8.8 px** 起排（我们原来为了给它让位从 52 px 起排，现在回到了 8.8）。
//     · 「每行一张迷你卡 + `actionImage` 动作图标」**本来就不存在**（原版那三个类是**死代码**：
//       `CemeteryLogGroup`/`CemeteryLogManager`/`CemeteryLogCard` 全 `assets_full` 里没有 MonoBehaviour
//       指向它们）⇒ 这条**销账**，别再当欠账。判据 → `资料/待办判据_战场与战斗视图.md` §8b。
//   ⏭ **还欠**：**悬停行内链接 ⇒ 在面板旁弹一张整套 `CardView`**（原版 `CemeteryManager.CheckCardLink`
//      对行文字做 `FindIntersectingLink` → `GetLinkID` → `Split(',')` → `DisplayCard`）——
//      要做就得先给日志文案**加上 TMP 链接**（我们现在是纯文本）。
//
// 层级：相机看 +Z（**z 越大越远**）。这块面板要压在整个战场和 HUD 之上，
// 所以给它一组「最靠前」的 z（见下面 `Z*` 常量）—— 压暗层要比 HUD 的文字（z=0）还近。
// ============================================================================
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

namespace CardPresentation
{
    public class BattleLogPanel : MonoBehaviour
    {
        // ---- 原版尺寸（px @1920×1080；HUD 里 108 px = 1 世界单位）----
        const float PanelW = 794.1f;
        /// <summary>拉开后**面板左缘的屏幕 x** —— 原版 `CemeteryManager.finalX = 87`（收起时 `initialX = −1200`）。
        /// 🔴 2026-09-29 订正：我们原来把它贴在 x=0（偏左 87 px）。我们**不做滑动**（直接切显隐），
        /// 所以这个值就是「打开时它该在哪」。</summary>
        const float PanelOpenX = 87f;
        /// <summary>悬停弹卡时那张卡的 z（面板整组在 −4.0 一带，卡要压在行文字之上）。</summary>
        public const float ZHoverCard = -4.12f;
        /// <summary>悬停弹卡那张卡的**中心**（屏幕 px · y 向下）—— 原版 `CemeteryGroup/CardUI (1)`：
        /// 面板左缘**左 53.04** px、面板竖中线**上 20.49** px（出处 `Transform_1340.json` 的
        /// `localPos (−53.04, 20.49)` + `localScale 108`，挂在面板枢轴上 ⇒ **随面板一起动**）。
        /// 面板拉开停在 x=87 ⇒ 卡中心 x = 33.96（**贴左屏边挂出去一截**，原版数据本身就如此）。
        /// 🔴 **卡体 226.0×359.8 px**；上一轮记的 `274.72×363.81` 是 `CardUI` **根节点**、不是卡（2026-09-29 订正）。</summary>
        public static Vector2 CardCenterPx
        {
            get
            {
                return new Vector2(PanelOpenX - 53.04f,
                                   (1f - (PanelTopY01 + PanelBotY01) * 0.5f) * 1080f - 20.49f);
            }
        }
        /// <summary>悬停那张卡的**卡体**尺寸（px）—— 取自 `RectTransform_3149.json`（`2DCard` 2.0927×3.3313 ×108）。</summary>
        public const float CardBodyW = 226.0f, CardBodyH = 359.8f;

        /// <summary>悬停卡该摆的**世界坐标**（相对面板根；面板子树用的是绝对世界坐标，模块内这一减法保证换父节点也不偏）。</summary>
        public Vector3 HoverCardLocalPos(float z)
        {
            var c = CardCenterPx;
            var w = LayoutSpace.ToWorld(c.x / 1920f, 1f - c.y / 1080f);
            return new Vector3(w.x - (_root != null ? _root.position.x : 0f),
                               w.y - (_root != null ? _root.position.y : 0f), z);
        }
        const float PanelTopY01 = 0.793f, PanelBotY01 = 0.187f;   // anchor 上下沿
        const float BgW = 769.5f, BgH = 454.4f;
        const float RowW = 748f, RowH = 43.134f;
        // 🔴 **2026-09-28 订正（原来写的是 `RowH 53.92 / RowCount 8`，注「原版十行里能完整看见的是八行」——
        //    那句是错的）**：原版 `CemeteryActions` = **748.006×431.344**、`VerticalLayoutGroup`
        //    spacing **0**、align UpperCenter、**数组长度硬编码 10**（场景里就是 10 个 `CemeterySliderUI`）
        //    ⇒ **10 行 × 43.134**，行底图 `40k_battlelog_display_neutral` 原生就是 **653×43**。
        //    我们原来那个 53.92×8 = 431.36 **容器高度是对的**（所以看着没问题），但**行数与行高都错**。
        //    出处：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4589.json`（`cemeteryActions` 10 个 pid）
        //    + 运行时 dump L134-163 + `素材/Warpforge原版/UI图集/图集/battleatlasui/Sprite/40k_battlelog_display_neutral.json`。
        const int RowCount = 10;
        const float FrameTopPx = 68f;                              // 顶边框高（行从它下面开始排）

        // 🔴🔴 **2026-09-27 修（PA 普查）：四条边框原来【全是错的】** —— 逐值照原版重摆。
        //   **实据**：`menu_dump.py bundle_scenes_scenes_battlearena1 LeftArea --depth 8` 的
        //   `CemeteryLogPanel/Frame/{Frame Top,Bottom,Left,Right}`（四条都是 `Simple + preserveAspect`）：
        //     · `Frame Top`    框 **707.30×65.06**（图 `…frame_TOP` 740×68）· 中心 y(自顶) **278.67**
        //     · `Frame Bottom` 框 **707.30×46.79**（图 `…frame_Bottom` 740×49）· 中心 y(自顶) **762.82**
        //     · `Frame Left`   框 **97.65×571.24**（图 `…frame_Left` 98×601）· 中心 y(自顶) **542.20**
        //     · `Frame Right`  框 **63.05×578.65**（图 `…frame_Right` 66×608）· 中心 y(自顶) **541.27**
        //   **原来错在三处**：① 竖条一律取**面板锚高 654.5**（比原版长 **14%**）② 横条取**面板全宽 794.1**
        //   （比原版宽 **12%**）③ y 一律用 `panelCy`（与四条各自的中心差 **13~55px**）。
        //   ⇒ **面板高 ≠ 边框长**，四条各有各的框、也各有各的中心。
        // ⚠️ 下面都是**面板局部**的量：`y01` 是**自下往上**（与 `PanelTopY01` 同一套），`*Cx01` 是 x01（`0` = 面板左缘）。
        const float FrameTopBoxPx = 65.06f, FrameBotBoxPx = 46.79f;
        const float FrameTopCy01  = 0.741972f, FrameBotCy01 = 0.293685f;   // (1080 − 自顶中心)/1080
        const float FrameLPx = 571.24f, FrameRPx = 578.65f;
        const float FrameLCy01 = 0.497963f, FrameRCy01 = 0.498824f;
        const float FrameLCx01 = -18.405f / 1920f;                          // 原版左框中心在面板左缘**外** 18.4px
        const float FrameRCx01 = 763.75f / 1920f;                           // 原版右框中心（面板局部 x = 763.75）
        /// <summary>底板颜色：原版 `BG` 的 m_Color = (0, 0.08, 0.01, 1)。
        /// ⚠️ **要 `.linear`** —— 工程是线性色彩空间，直接把 0.08 喂给材质会渲染成**亮绿**
        /// （第一版就是这样，截图里是一块扎眼的绿板）。</summary>
        static readonly Color BgColor = new Color(0f, 0.08f, 0.01f, 1f).linear;
        /// <summary>压暗：原版 `shade` 黑 55%</summary>
        static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.55f);

        // ---- 层级（**z 越小越靠前**）----
        // ⚠️ 第一版把 `ZBg` 写得比 `ZRowBg` 还小 —— 于是底板压住了所有行（截图里只看得到一行）。
        // ⚠️ 第二版整组放在 −0.5、第三版 −0.9，**3D 粒子照样飘在面板上面**（截图里烟穿过行底图）——
        //    特效是摆在**棋盘格位**上的（z 跟着棋盘走，比 HUD 近得多），要盖住它得推到这个量级。
        //    这几个值只要**明显小于棋盘和粒子的 z** 就行，具体多少不影响版面（正交相机、尺寸不变）。
        const float ZShade = -4.00f;   // 压暗层：全屏压暗（连 HUD 一起暗）
        const float ZBg = -4.02f;
        const float ZFrame = -4.03f;
        const float ZRowBg = -4.04f;
        const float ZText = -4.08f;    // 文字最靠前

        static float Px(float px) { return px / 108f; }

        /// <summary>面板里所有东西都放这个渲染队列 —— **4000 = Overlay（脱离排序、最后画）**。
        /// 为什么要它：`Sprites/Default` 和粒子同在透明队列 3000，同队列下按**距离**排，
        /// 而粒子系统的排序看的是它自己的包围盒中心 —— 2026-09-13 实测把面板一路推到 z=−4，
        /// 烟**照样**穿在面板上面。见 `ImageQuad.SetRenderQueue`。</summary>
        public const int OverlayQ = 4000;

        /// <summary>一行要显示的东西（由 `BattleDriver` 从 `Ctx.ActionLog` 翻好中文再喂进来 ——
        /// **卡名→中文名那张表在驱动那边**，面板不碰数据）</summary>
        public struct Entry
        {
            /// <summary>这一行提到的**卡（英文 id / 卡名）** —— 悬停时拿它去卡池里找卡。
            /// 空 = 这行不挂链接。（原来只拿它取小头像，小头像已删。）</summary>
            public string CardId;
            /// <summary>**文字里印的那段卡名**（= `Zh(CardId)`，中文）—— 链接要包的就是它。
            /// 🔴 **`CardId` 与它不是同一串**（一个是英文 id、一个是显示名）⇒ 别拿 `CardId` 去 `Text` 里搜，
            /// 搜不到（2026-09-29 差点这么写）。</summary>
            public string LinkText;
            public string Text;       // 这一行的人话
        }

        Transform _root;              // 面板本体（显示/隐藏切它）
        ImageQuad _bg;
        ImageQuad[] _rowBgs;
        Label[] _rowTexts;
        ImageQuad _shade;
        /// <summary>最近一次喂进来的内容（`Show()` 里要拿它重刷一遍 —— 见 `SetEntries` 的注释）</summary>
        readonly List<Entry> _last = new List<Entry>();
        bool _visible;

        public bool Visible { get { return _visible; } }
        /// <summary>自检用：面板根节点是不是被显示着</summary>
        public bool RootActive { get { return _root != null && _root.gameObject.activeSelf; } }
        /// <summary>自检用：压暗层在不在</summary>
        public bool ShadeActive { get { return _shade != null && _shade.gameObject.activeSelf; } }
        /// <summary>自检用：现在几行有字</summary>
        public int FilledRows
        {
            get
            {
                int n = 0;
                if (_rowTexts != null) foreach (var t in _rowTexts) if (t != null && !string.IsNullOrEmpty(t.Text)) n++;
                return n;
            }
        }
        /// <summary>自检用：第 i 行的**可见文字**（`<link=…>` 那层壳已剥掉 —— 玩家看到的是剥掉之后那句）。
        /// ⚠️ 别拿它判「有没有链接」，那个问 <see cref="RowLinkKey"/>。</summary>
        public string RowText(int i)
        {
            string raw = (_rowTexts != null && i >= 0 && i < _rowTexts.Length && _rowTexts[i] != null)
                       ? _rowTexts[i].Text : null;
            return raw == null ? null : CardIcons.StripTags(raw);
        }
        /// <summary>自检用：第 `i` 行里那个**卡名链接**（没有 = null）。悬停弹卡就靠它。</summary>
        public string RowLinkKey(int i)
        {
            if (_rowTexts == null || i < 0 || i >= _rowTexts.Length || _rowTexts[i] == null) return null;
            var m = System.Text.RegularExpressions.Regex.Match(_rowTexts[i].Text ?? "", "<link=\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>指针压在哪一行的**卡名链接**上（没有 = null）。判据是 **TMP 自己的 `FindIntersectingLink`**
        /// —— 与卡面关键词 tooltip **同一条路**（`Label.LinkAt`），没有第二份命中逻辑。
        /// 驱动每帧问它一次（面板没显示就直接 null）。</summary>
        public string LinkKeyAt(Vector3 wp, Camera cam)
        {
            if (!_visible || _rowTexts == null) return null;
            for (int i = 0; i < _rowTexts.Length; i++)
            {
                if (_rowTexts[i] == null) continue;
                string k = _rowTexts[i].LinkAt(wp, cam);
                if (!string.IsNullOrEmpty(k)) return k;
            }
            return null;
        }

        /// <summary>第 `i` 行那个文字块的**世界坐标**（弹卡时贴着这一行摆）。</summary>
        public Vector3 RowAnchor(int i)
        {
            if (_rowTexts != null && i >= 0 && i < _rowTexts.Length && _rowTexts[i] != null)
                return _rowTexts[i].transform.position;
            return _root != null ? _root.position : Vector3.zero;
        }

        /// <summary>自检用：在第 `i` 行里**扫出一个压在卡名链接上的点**（世界坐标）。
        /// 批处理没有鼠标，而 `FindIntersectingLink` 要一个**真落在字形上**的点 ⇒ 从文字块左缘起按 4 px 步长扫一遍。
        /// 返回 false = 这一行没有链接 / 扫不到（**如实返回 false**，不编一个点）。</summary>
        public bool FindLinkProbe(int row, Camera cam, out Vector3 wp)
        {
            wp = Vector3.zero;
            if (_rowTexts == null || row < 0 || row >= _rowTexts.Length || _rowTexts[row] == null) return false;
            if (string.IsNullOrEmpty(RowLinkKey(row))) return false;
            var c0 = LayoutSpace.ToPixel(_rowTexts[row].transform.position);   // 文字块（锚点 0,0.5）左缘中点
            for (float dx = 8.8f; dx < 740f; dx += 4f)
            {
                var p = LayoutSpace.FromPixel(c0.x + dx, c0.y);
                if (!string.IsNullOrEmpty(_rowTexts[row].LinkAt(p, cam))) { wp = p; return true; }
            }
            return false;
        }
        /// <summary>自检用：第 i 行文字的**实际宽度**（世界单位）。
        /// ⚠️ 这条才是抓「字根本没画出来」的判据 —— `RowText(i)` 有值只说明**属性**设上了，
        ///    TMP 在**未激活**的对象上建不出字形时它照样有值（第一版就这么骗过去了：
        ///    断言说「1 行有字」，截图里整块板一个字都没有）。</summary>
        public float RowTextWidth(int i)
        {
            return (_rowTexts != null && i >= 0 && i < _rowTexts.Length && _rowTexts[i] != null)
                 ? _rowTexts[i].WorldW : 0f;
        }
        /// <summary>自检用：**行是不是画在底板前面**（z 越小越靠前）。
        /// ⚠️ 这条钉的是一个真踩过的坑：第一版把 `ZBg` 写得比 `ZRowBg` 小，
        ///    底板把所有行**全盖住了**，截图里只看得到一行。</summary>
        public bool RowsInFrontOfBg
        {
            get
            {
                if (_bg == null || _rowBgs == null || _rowBgs.Length == 0 || _rowBgs[0] == null) return false;
                return _rowBgs[0].transform.localPosition.z < _bg.transform.localPosition.z;
            }
        }

        public static BattleLogPanel Create(Transform root)
        {
            var go = new GameObject("BattleLogPanel");
            go.transform.SetParent(root, false);
            var p = go.AddComponent<BattleLogPanel>();
            p.Build(root);
            return p;
        }

        void Build(Transform root)
        {
            // 压暗层：铺满屏幕（原版那张 shade 是 7020×4544，**远大于屏幕**，就是铺满）。
            // 挂在根上、不跟着面板划进划出；它自己也是**点击捕获区**（点面板外面关掉）。
            _shade = ImageQuad.Create(root, CardArt.Solid(), new Vector3(0f, 0f, ZShade), 40f,
                                      new Vector2(0.5f, 0.5f), "BattleLogShade");
            if (_shade != null)
            {
                _shade.SetTint(ShadeColor);
                _shade.SetRenderQueue(OverlayQ);
                _shade.gameObject.SetActive(false);
            }

            var panel = new GameObject("CemeteryLogPanel");
            panel.transform.SetParent(root, false);
            _root = panel.transform;

            float panelCy = (PanelTopY01 + PanelBotY01) * 0.5f;    // 面板中心的 y01（pivot (0,0.5)）
            // 🔴 **2026-09-29 订正：拉开后停在 x = 87，不是 0** —— 原版 `CemeteryManager.initialX/finalX`
            //    = **−1200 / 87**（`MonoBehaviour_4491.json`），运行时 `DOAnchorPosX(rt, 87)` 划出来
            //    （`CemeteryManager__ShowCemeteryLogBtn.c:17`）。原来我们把左缘贴屏幕左缘（x=0）⇒ **整整偏左 87 px**。
            float panelCx01 = (PanelOpenX + PanelW * 0.5f) / 1920f;

            // 底板：原版是个**没有 sprite 的 Image**（色 (0,0.08,0.01)）→ 我们用白图 + tint
            var bg = ImageQuad.Create(_root, CardArt.Solid(), Vector3.zero, Px(BgH),
                                      new Vector2(0.5f, 0.5f), "LogBG");
            _bg = bg;
            if (bg != null)
            {
                bg.SetAspect(BgW / BgH);
                bg.SetTint(BgColor);
                bg.SetRenderQueue(OverlayQ);
                bg.transform.localPosition =
                    new Vector3(LayoutSpace.ToWorld(panelCx01, panelCy).x,
                                LayoutSpace.ToWorld(panelCx01, panelCy).y, ZBg);
            }

            // 四条边框 —— **逐值照原版**（每条各有各的框与中心，见上面那组常量）
            Frame("40k_battlelog_frame_TOP",    panelCx01,  FrameTopCy01, FrameTopBoxPx);
            Frame("40k_battlelog_frame_Bottom", panelCx01,  FrameBotCy01, FrameBotBoxPx);
            Frame("40k_battlelog_frame_Left",   FrameLCx01, FrameLCy01,   FrameLPx);
            Frame("40k_battlelog_frame_Right",  FrameRCx01, FrameRCy01,   FrameRPx);

            // 行：748×53.92，从上往下排（顶边框下面留一点）
            _rowBgs = new ImageQuad[RowCount];
            _rowTexts = new Label[RowCount];
            for (int i = 0; i < RowCount; i++)
            {
                float cy01 = RowCenterY01(i);
                var at = LayoutSpace.ToWorld(panelCx01, cy01);

                _rowBgs[i] = ImageQuad.Create(_root, CardArt.Ui("40k_battlelog_display_neutral"),
                                              new Vector3(at.x, at.y, ZRowBg), Px(RowH),
                                              new Vector2(0.5f, 0.5f), "LogRow" + i);
                if (_rowBgs[i] != null)
                {
                    // ⚠️ **画满整行**（748×43.134）：原版那张 `40k_battlelog_display_neutral` 是
                    //    `Simple + preserveAspect=0` ⇒ **拉伸**铺满行框（图本身 653×43）。
                    //    原来给的是 `RowH * 0.8` ⇒ 实绘只有 **598×34.5**（宽也短了 150px），2026-09-28 一并订正。
                    _rowBgs[i].SetAspect(RowW / RowH);
                    _rowBgs[i].SetRenderQueue(OverlayQ);
                }

                // 🔴 **2026-09-29：文字回到原版的起点（行内 8.8 px）** —— 原来为了给**我们自加的那枚
                //    30px 小头像**让位，从 52 px 起排（用户 2026-09-28 拍板「删掉、照原版」）。
                //    判据：原版 `ActionText` 在行内 x **8.8 … 731.8**（行 748 宽）。⇒ 面板坐标 = 行的左缘 + 8.8。
                var textAt = LayoutSpace.ToWorld(((PanelW - RowW) * 0.5f + 8.8f) / 1920f, cy01);
                _rowTexts[i] = Label.Create(_root, "", new Vector3(textAt.x, textAt.y, ZText), 3,
                                            new Color(0.93f, 0.93f, 0.93f),   // 原版 ActionText = Asar Almost White
                                            new Vector2(0f, 0.5f), "LogRowText" + i);
                if (_rowTexts[i] != null) _rowTexts[i].SetRenderQueue(OverlayQ);
            }

            panel.SetActive(false);
        }

        /// <summary>摆一条边框。`lenPx` = **原版那条的长边**（横条是框高、竖条也是框高）——
        /// 四条原版都是 `Simple + preserveAspect`，且**贴图比例 ≈ 框比例** ⇒ 按长边定高、宽由贴图比例出，
        /// 结果与原版实绘一致（Top 707.3×65.0 · Left 93.15×571.24 · Right 62.81×578.65…）。</summary>
        void Frame(string art, float x01, float y01, float lenPx)
        {
            var tex = CardArt.Ui(art);
            if (tex == null) return;
            var at = LayoutSpace.ToWorld(x01, y01);
            var q = ImageQuad.Create(_root, tex, new Vector3(at.x, at.y, ZFrame), Px(lenPx),
                                     new Vector2(0.5f, 0.5f), "LogFrame_" + art);
            if (q != null) q.SetRenderQueue(OverlayQ);
            _frames[art] = q;
        }

        /// <summary>四条边框的 quad（自检量尺寸用）。键就是原版图名（`40k_battlelog_frame_*`）。</summary>
        readonly System.Collections.Generic.Dictionary<string, ImageQuad> _frames =
            new System.Collections.Generic.Dictionary<string, ImageQuad>();

        /// <summary>一行的高度（px @1920×1080）—— 自检用。原版 **43.134**（`CemeteryActions` 431.344 ÷ 10 行）。</summary>
        public static float RowHeightPx { get { return RowH; } }
        /// <summary>行数 —— 自检用。原版**硬编码 10**（场景里就是 10 个 `CemeterySliderUI`）。</summary>
        public static int RowTotal { get { return RowCount; } }
        /// <summary>第 `i` 行的底图 quad（自检量「有没有画满整行」用）。</summary>
        public ImageQuad RowBg(int i) { return (i >= 0 && i < _rowBgs.Length) ? _rowBgs[i] : null; }

        /// <summary>某条边框的**渲染尺寸**（世界单位）—— 自检用。查不到返回 0。</summary>
        public float FrameWorldW(string art)
        { ImageQuad q; return _frames.TryGetValue(art, out q) && q != null ? q.WorldW : 0f; }

        /// <inheritdoc cref="FrameWorldW"/>
        public float FrameWorldH(string art)
        { ImageQuad q; return _frames.TryGetValue(art, out q) && q != null ? q.WorldH : 0f; }

        public void Toggle() { if (_visible) Hide(); else Show(); }

        public void Show()
        {
            _visible = true;
            if (_root != null) _root.gameObject.SetActive(true);
            if (_shade != null) _shade.gameObject.SetActive(true);
            ApplyEntries();     // ⚠️ **激活之后再刷一次** —— 未激活时 TMP 建不出字形
        }

        public void Hide()
        {
            _visible = false;
            if (_root != null) _root.gameObject.SetActive(false);
            if (_shade != null) _shade.gameObject.SetActive(false);
        }

        /// <summary>刷新内容。`entries` **新的在前**（原版就是从最新一条往下排）。
        /// ⚠️ **在面板关着的时候调这个，字是画不出来的** —— TMP 在未激活的对象上建不出字形
        /// （第一版就踩了：先 `SetEntries` 后 `Show`，打开后整块板一个字的都没有）。
        /// 所以内容存一份，`Show()` 里会**再刷一次**。</summary>
        public void SetEntries(List<Entry> entries)
        {
            _last.Clear();
            if (entries != null) _last.AddRange(entries);
            ApplyEntries();
        }

        /// <summary>真正把 `_last` 画到行上（面板**必须已经激活**）</summary>
        void ApplyEntries()
        {
            if (_rowTexts == null) return;
            for (int i = 0; i < _rowTexts.Length; i++)
            {
                bool has = i < _last.Count;
                if (_rowTexts[i] != null) _rowTexts[i].SetText(has ? Linkify(_last[i]) : "");
            }
        }

        /// <summary>把这一行里出现的**卡名**包成 TMP 链接 —— 这是原版那条「悬停卡名 ⇒ 弹一张卡」的入口
        /// （`CemeteryManager.CheckCardLink`：对行文字做 `FindIntersectingLink` → `GetLinkID` → `DisplayCard`）。
        ///
        /// 包的样式**照原版**：`<b><link="…"><u>名字</u></link></b>`（名字**加粗 + 下划线** = 那种「可点」的样子；
        /// 出处 = `CemeteryManager` 里那三个字面量 `'<b><link="1,'` / `'"><u>'` / `'</u></link></b>'`，
        /// 靠 `stringliteral.json` 解出来的）。
        /// ⚠️ **链接 ID 用的是英文卡名**（原版用的是「`0/1,动作索引`」那种**动作表下标** —— 我们没有那张表，
        /// 如实记这条差异）；可见文字用 `LinkText`（中文名）。
        /// ⚠️ 名字对不上（这行本来没提卡 / 译文不一致）就**原样返回、不硬造链接**。
        /// ⚠️ 点阵后端（没有 TMP 时）会把标签整个剥掉（`CardIcons.StripTags` 的判据是「有没有 `<`」）⇒ 安全。</summary>
        static string Linkify(Entry e)
        {
            if (string.IsNullOrEmpty(e.Text)) return e.Text;
            string show = string.IsNullOrEmpty(e.LinkText) ? e.CardId : e.LinkText;
            if (string.IsNullOrEmpty(show) || string.IsNullOrEmpty(e.CardId)) return e.Text;
            int k = e.Text.IndexOf(show, System.StringComparison.Ordinal);
            if (k < 0) return e.Text;
            return e.Text.Substring(0, k)
                 + "<b><link=\"" + e.CardId + "\"><u>" + show + "</u></link></b>"
                 + e.Text.Substring(k + show.Length);
        }

        /// <summary>第 i 行的中心 y01（`Build` 和 `SetEntries` 共用这一份）</summary>
        static float RowCenterY01(int i)
        {
            float rowTop01 = PanelTopY01 - (FrameTopPx + 8f) / 1080f;
            return rowTop01 - (i + 0.5f) * RowH / 1080f;
        }
    }
}
