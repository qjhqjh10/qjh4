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
// ⚠️ **两条如实说明（别当成原版）**：
//   ① 原版每行画的是**迷你卡**（`CemeteryLogCard : CardScript`，还有 `actionImage` 动作图标）——
//      我们这版先做「小头像 + 一行字」，**没做迷你卡和动作图标**（图标资源没导出，卡片以后能补）。
//   ② 四条边框怎么拼的**没查实**（原版 `Frame` 节点的 863×1032.5 与面板 794.1 对不上，
//      可能是外扩的装饰边框）。我们按「四张图围住底板」拼，各图按自身比例缩到面板边上。
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
        const float PanelTopY01 = 0.793f, PanelBotY01 = 0.187f;   // anchor 上下沿
        const float BgW = 769.5f, BgH = 454.4f;
        const float RowW = 748f, RowH = 53.92f;
        const int RowCount = 8;                                    // 原版十行里能完整看见的是八行
        const float FrameTopPx = 68f;                              // 顶边框高（行从它下面开始排）
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
        const float ZThumb = -4.06f;
        const float ZText = -4.08f;    // 文字最靠前

        static float Px(float px) { return px / 108f; }

        /// <summary>面板里所有东西都放这个渲染队列 —— **4000 = Overlay（脱离排序、最后画）**。
        /// 为什么要它：`Sprites/Default` 和粒子同在透明队列 3000，同队列下按**距离**排，
        /// 而粒子系统的排序看的是它自己的包围盒中心 —— 2026-09-13 实测把面板一路推到 z=−4，
        /// 烟**照样**穿在面板上面。见 `ImageQuad.SetRenderQueue`。</summary>
        const int OverlayQueue = 4000;

        /// <summary>一行要显示的东西（由 `BattleDriver` 从 `Ctx.ActionLog` 翻好中文再喂进来 ——
        /// **卡名→中文名那张表在驱动那边**，面板不碰数据）</summary>
        public struct Entry
        {
            public string CardId;     // 用来取小头像（没有就不画）
            public string Text;       // 这一行的人话
        }

        Transform _root;              // 面板本体（显示/隐藏切它）
        ImageQuad _bg;
        ImageQuad[] _rowBgs;
        Label[] _rowTexts;
        ImageQuad[] _rowThumbs;
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
        /// <summary>自检用：第 i 行的文字</summary>
        public string RowText(int i)
        {
            return (_rowTexts != null && i >= 0 && i < _rowTexts.Length && _rowTexts[i] != null)
                 ? _rowTexts[i].Text : null;
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
                _shade.SetRenderQueue(OverlayQueue);
                _shade.gameObject.SetActive(false);
            }

            var panel = new GameObject("CemeteryLogPanel");
            panel.transform.SetParent(root, false);
            _root = panel.transform;

            float panelCy = (PanelTopY01 + PanelBotY01) * 0.5f;    // 面板中心的 y01（pivot (0,0.5)）
            float panelCx01 = PanelW * 0.5f / 1920f;               // anchor x = 0 → 左缘贴屏幕左缘

            // 底板：原版是个**没有 sprite 的 Image**（色 (0,0.08,0.01)）→ 我们用白图 + tint
            var bg = ImageQuad.Create(_root, CardArt.Solid(), Vector3.zero, Px(BgH),
                                      new Vector2(0.5f, 0.5f), "LogBG");
            _bg = bg;
            if (bg != null)
            {
                bg.SetAspect(BgW / BgH);
                bg.SetTint(BgColor);
                bg.SetRenderQueue(OverlayQueue);
                bg.transform.localPosition =
                    new Vector3(LayoutSpace.ToWorld(panelCx01, panelCy).x,
                                LayoutSpace.ToWorld(panelCx01, panelCy).y, ZBg);
            }

            // 四条边框：围着底板摆（每张按自身比例缩到面板边上）
            Frame("40k_battlelog_frame_TOP",    panelCx01, PanelTopY01, true);
            Frame("40k_battlelog_frame_Bottom", panelCx01, PanelBotY01, true);
            Frame("40k_battlelog_frame_Left",   0f, panelCy, false);
            Frame("40k_battlelog_frame_Right",  PanelW / 1920f, panelCy, false);

            // 行：748×53.92，从上往下排（顶边框下面留一点）
            _rowBgs = new ImageQuad[RowCount];
            _rowTexts = new Label[RowCount];
            _rowThumbs = new ImageQuad[RowCount];
            for (int i = 0; i < RowCount; i++)
            {
                float cy01 = RowCenterY01(i);
                var at = LayoutSpace.ToWorld(panelCx01, cy01);

                _rowBgs[i] = ImageQuad.Create(_root, CardArt.Ui("40k_battlelog_display_neutral"),
                                              new Vector3(at.x, at.y, ZRowBg), Px(RowH * 0.8f),
                                              new Vector2(0.5f, 0.5f), "LogRow" + i);
                if (_rowBgs[i] != null)
                {
                    _rowBgs[i].SetAspect(RowW / RowH);
                    _rowBgs[i].SetRenderQueue(OverlayQueue);
                }

                // 文字从面板左边留个位置（小头像占 30 px 宽）往后排
                var textAt = LayoutSpace.ToWorld(52f / 1920f, cy01);
                _rowTexts[i] = Label.Create(_root, "", new Vector3(textAt.x, textAt.y, ZText), 3,
                                            new Color(0.93f, 0.93f, 0.93f),   // 原版 ActionText = Asar Almost White
                                            new Vector2(0f, 0.5f), "LogRowText" + i);
                if (_rowTexts[i] != null) _rowTexts[i].SetRenderQueue(OverlayQueue);
            }

            panel.SetActive(false);
        }

        /// <summary>摆一条边框。`horizontal` = 横条（顶/底），按面板宽缩放；竖条按面板高缩放。</summary>
        void Frame(string art, float x01, float y01, bool horizontal)
        {
            var tex = CardArt.Ui(art);
            if (tex == null) return;
            float aspect = tex.height > 0 ? tex.width / (float)tex.height : 1f;
            float worldH = horizontal ? Px(PanelW / aspect) : Px((PanelTopY01 - PanelBotY01) * 1080f);
            var at = LayoutSpace.ToWorld(x01, y01);
            var q = ImageQuad.Create(_root, tex, new Vector3(at.x, at.y, ZFrame), worldH,
                                     new Vector2(0.5f, 0.5f), "LogFrame_" + art);
            if (q != null) q.SetRenderQueue(OverlayQueue);
        }

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
                if (_rowTexts[i] != null) _rowTexts[i].SetText(has ? _last[i].Text : "");

                // 小头像：有卡就画一张方图（原版这里是迷你卡，见文件头说明①）
                if (_rowThumbs[i] != null) { DestroyImmediate(_rowThumbs[i].gameObject); _rowThumbs[i] = null; }
                if (has && !string.IsNullOrEmpty(_last[i].CardId))
                {
                    // 战斗事件里带的是**卡名**（`BattleEvent.CardId`），这里按名反查 id（见 `PortraitByName`）
                    var portrait = CardArt.PortraitByName(_last[i].CardId);
                    if (portrait != null)
                    {
                        var at = LayoutSpace.ToWorld(30f / 1920f, RowCenterY01(i));
                        var q = ImageQuad.Create(_root, portrait, new Vector3(at.x, at.y, ZThumb),
                                                 Px(40f), new Vector2(0.5f, 0.5f), "LogRowIcon" + i);
                        if (q != null) { q.SetAspect(1f); q.SetRenderQueue(OverlayQueue); _rowThumbs[i] = q; }
                    }
                }
            }
        }

        /// <summary>第 i 行的中心 y01（`Build` 和 `SetEntries` 共用这一份）</summary>
        static float RowCenterY01(int i)
        {
            float rowTop01 = PanelTopY01 - (FrameTopPx + 8f) / 1080f;
            return rowTop01 - (i + 0.5f) * RowH / 1080f;
        }
    }
}
