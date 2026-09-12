// AttackSelector.cs — 原版的「攻击方式选择器」（近战 / 远程 / 主动技能）
//
// ⚠️ **本文件里的每个数值都来自解包资源实测**，不是拍的。出处：
//    · 场景 JSON：`d:/2/解包整理/07_场景/battlearena1/GameObject/Drag Attack Selector_172.json`
//      和它三个子按钮（`Select Melee Button_867` / `Select Range Button_1005` /
//      `Select Active Skill Button_760`）上的 MonoBehaviour
//    · 运行时实况：`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`
//
//    对象路径  Canvas/FrontCanvas/Safe area FrontCanvas/Drag Attack Selector
//    整条槽    621.7 × 122.2，**屏幕正中央**（两排棋盘之间那条带，y=479..601 @1080p）
//    三个按钮  118.4 × 118.8，间距 37，居中排开
//    attackType 是个位标志：1 = 近战、2 = 远程、4 = 主动技能（所以枚举值照抄）
//    选中放大  scaleMultiplierWhenSelected = 1.3
//    拖拽阈值  accumulatedDragForMinDistance = 0.085（屏高的 8.5%，1080p ≈ 92 px）
//    方向      directionToPivotPoint —— 近战 (-0.5,-1) 左下 / 技能 (0,-1.1) 正下 /
//              远程 (0.5,-1) 右下。**这就是横排的左右顺序**：近战在前、技能居中、远程在后
//    底板      Select Attack Background —— ⚠️ sizeDelta 是 780×285，但 **localScale = 0.44**、
//              显示时再横向 ×1.5 → **实际 514.8 × 125.4**。只读 sizeDelta 会做出一大块
//
// ⚠️ **别照抄 `d:/warpforge/scripts/battle.gd` 那份重实现**：它把四个按钮**叠在同一坐标**、
//    用 `visible = (_attack_type == ...)` 做互斥高亮 —— Godot 里不可见的 Button 收不到输入，
//    结果玩家根本切不到远程/技能。**以解包的原版为准**（三个独立按钮 + 黄圈 + 1.3 倍放大）。
//
// 图（`Resources/Art/ui/`，都是原版战斗 UI 图集的切片）：
//    Attack_type_button_Melee / _Ranged（按钮本体，自带底盘和金属环）
//    Attack_type_button_highlight（选中黄圈）
//    Attack_type_Generic_Foreground（**主动技能那格的占位底图** —— 原版那格是空的、运行时才赋图，
//    这张图在原版里查不到任何引用，是我们挑的。见 `IconName`）
//
// **没有这些图也能跑** —— 每条都判空，退化成纯文字（和 `CardArt` 一个路子）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 一次行动可以选哪几种「打法」。
    /// 值**照抄原版的 `attackType` 位标志**（1/2/4），这样对着场景 JSON 排查时能直接对上。
    /// </summary>
    public enum AttackKind
    {
        None = 0,
        Melee = 1,
        Ranged = 2,
        Ability = 4,
    }

    public class AttackSelector : MonoBehaviour
    {
        // ==================================================================
        //  原版实测数值（1920×1080 像素）
        // ==================================================================

        const float BarW = 621.7f, BarH = 122.2f;
        const float ButtonSize = 118.8f;
        const float ButtonSpacing = 37f;
        const float HighlightScale = 1.3f;              // scaleMultiplierWhenSelected
        /// <summary>选中黄圈（原版按钮的子对象 `Highlight`）的实测尺寸</summary>
        const float RingSize = 176.2f;

        // ⚠️ **`40K_melee_glow` / `40K_ranged_glow` 不在这里用**。
        //    我一开始以为它们是按钮的外发光 —— **错了**。它们真正的用途是
        //    **场上单位卡身上、按攻击方式点亮的高亮**：
        //    `d:/2/解包整理/08_预制体特效/战斗预制体/GameObject/Highlight_*.json`（近战，红）和
        //    `Highlight ranged_*.json`（远程，紫），挂在 `CardPrefab/Board Elements/3DBody/
        //    Base Attack Counters/Melee Attack Container`（和 `Range Attack Container`）下面。
        //    那是「**这个单位能被选中**」的反馈，**还没做** ——
        //    锚点到底在「卡面的近战/远程数值格上」还是「卡脚下」还没钉死，
        //    证据和待办写在 `资料/规则引擎_进度与交接.md` 第七节，别照猜着做。
        //    按钮本身自带底盘和金属环（`Attack type button Melee/Ranged`），不用再垫一层光。

        // `Select Attack Background`：RectTransform 的 sizeDelta 是 780×285，**但它的 localScale = 0.44**，
        // 显示时 `backgroundController`（`inputScaleModifier`/`elementsHorizontalScaleModifier` 都是 1.5）
        // 再横向放大 1.5 倍 → **实际 514.8 × 125.4**，正好罩住那排 429.2 宽的按钮。
        // ⚠️ 只看 sizeDelta 会以为是 780×285，做出来一大块（这块差点做错，别再用 sizeDelta）
        const float BgW = 780f * 0.44f * 1.5f;          // 514.8
        const float BgH = 285f * 0.44f;                 // 125.4
        const float BgDropY = 18.375f;                  // 从槽顶往下挂多少

        // 按钮上的数字角标（原版 `Select Active Skill Button/ValueText`）：
        // 只有**主动技能**那一格有，字号大（base 36 / max 89），贴在中下部
        const float BadgeDx = 0f, BadgeDy = -8f;

        /// <summary>拖多远才弹出来：屏高的 8.5%（1080p ≈ 92 px）。原版 `accumulatedDragForMinDistance`</summary>
        public const float DragThreshold01 = 0.085f;

        /// <summary>拖拽阈值换算成世界单位（屏高恒 10 世界单位）</summary>
        public static float DragThresholdWorld
        {
            get { return DragThreshold01 * LayoutSpace.VisibleHeight; }
        }

        /// <summary>设计分辨率下 1 世界单位 = 108 px（可见高恒定 10 单位 = 1080 px）</summary>
        const float PxPerUnit = 108f;
        static float W(float px) { return px / PxPerUnit; }

        // z 分层：**越负越靠前**（相机看 -Z）。整块选择器都排在卡牌前面，见 `RefreshLayout`
        const float ZBg = -2.00f, ZIcon = -2.10f, ZRing = -2.15f, ZText = -2.20f;

        /// <summary>横排顺序 —— 按原版的 `directionToPivotPoint`：近战左、技能中、远程右</summary>
        static readonly AttackKind[] Order = { AttackKind.Melee, AttackKind.Ability, AttackKind.Ranged };

        // ==================================================================
        //  状态
        // ==================================================================

        public class Option
        {
            public AttackKind Kind;
            /// <summary>能不能选（没这个攻击力/技能在冷却 → false，按钮置灰且点不动）</summary>
            public bool Enabled;
            /// <summary>
            /// 按钮上的**数字角标**（原版 `Select Active Skill Button/ValueText`：字号很大、贴在中下部）。
            /// 原版只有主动技能那一格有；近战/远程的 `buttonIconValue` 是 null。
            /// </summary>
            public string Badge;
        }

        readonly List<Option> _options = new List<Option>();
        readonly ImageQuad[] _icons = new ImageQuad[3];
        readonly ImageQuad[] _rings = new ImageQuad[3];
        readonly Label[] _captions = new Label[3];

        ImageQuad _background;
        Label _title, _note;
        string _titleText;
        string _noteText;
        bool _built;

        public bool Visible { get; private set; }
        /// <summary>指针现在压着哪个按钮（压着才放大 1.3 + 亮黄圈）</summary>
        public AttackKind Hovered { get; private set; }
        public IReadOnlyList<Option> Options { get { return _options; } }

        static int SlotOf(AttackKind k)
        {
            for (int i = 0; i < Order.Length; i++) if (Order[i] == k) return i;
            return -1;
        }

        Option Find(AttackKind k)
        {
            foreach (var o in _options) if (o.Kind == k) return o;
            return null;
        }

        // ==================================================================
        //  建 + 摆
        // ==================================================================

        void Awake() { Build(); }

        public void Build()
        {
            if (_built) return;
            _built = true;

            CardArt.Load();

            // 底板：原版那格 Image **没有 sprite**（是纯色），所以我们也用一块半透明板。
            // 它的实测矩形（514.8 × 125.4）正好罩住那排按钮，顺带把底下的单位压暗一点
            // —— 这不是装饰，是**可读性**
            _background = ImageQuad.Create(transform, Solid(), Vector3.zero,
                                           W(BgH), new Vector2(0.5f, 1f), "SelectorBackground");
            if (_background != null) _background.SetTint(new Color(0.03f, 0.04f, 0.06f, 0.62f));

            _title = Label.Create(transform, "", Vector3.zero, 3,
                                  new Color(0.86f, 0.89f, 0.94f), new Vector2(0.5f, 0.5f), "SelectorTitle");
            // 标题上面那行小字（技能效果）。**不能塞在中场的提示行上** ——
            // 提示行 y=532 正好是按钮的位置，会被按钮压住（踩过）
            _note = Label.Create(transform, "", Vector3.zero, 2,
                                 new Color(0.98f, 0.93f, 0.72f), new Vector2(0.5f, 0.5f), "SelectorNote");

            for (int i = 0; i < Order.Length; i++)
            {
                var k = Order[i];

                // 按钮本体（原版 Attack type button *，自带底盘和金属环）
                _icons[i] = ImageQuad.Create(transform, CardArt.Ui(IconName(k)), Vector3.zero,
                                             W(ButtonSize), new Vector2(0.5f, 0.5f), "Icon_" + k);

                // 选中黄圈（原版里是按钮的子对象 `Highlight`）
                _rings[i] = ImageQuad.Create(transform, CardArt.Ui("Attack_type_button_highlight"),
                                             Vector3.zero, W(RingSize),
                                             new Vector2(0.5f, 0.5f), "Highlight_" + k);

                // 按钮上的数字角标（原版 `Select Active Skill Button/ValueText`）。
                // 原版字号很大（base 36 / max 89），配上它那个半透明底图，数字是按钮的主视觉
                _captions[i] = Label.Create(transform, "", Vector3.zero, 4,
                                            new Color(1f, 0.96f, 0.80f),
                                            new Vector2(0.5f, 0.5f), "Badge_" + k);
            }

            Hide();
        }

        static string IconName(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.Melee: return "Attack_type_button_Melee";
                case AttackKind.Ranged: return "Attack_type_button_Ranged";
                // ⚠️ 主动技能那格**原版没有静态底图** —— 场景里 `m_Sprite` 是空的，
                //    运行时由 `buttonIcon` 赋（多半是每张卡自己的技能图标）。
                //    `Attack_type_Generic_Foreground` 那张图虽然名字像，但**在 battlearena1/2 里零引用**，
                //    所以下面这只是**我们挑的占位**，不是原版的做法。
                //    我们自己的卡没有专属技能图标，先用它 + 按钮上的数字角标（`Badge`）。
                default: return "Attack_type_Generic_Foreground";
            }
        }

        /// <summary>收起来。**不销毁节点** —— 一局里要反复弹，每次重建会掉帧</summary>
        public void Hide()
        {
            Visible = false;
            Hovered = AttackKind.None;
            _options.Clear();

            if (_background != null) _background.gameObject.SetActive(false);
            if (_title != null) _title.gameObject.SetActive(false);
            if (_note != null) _note.gameObject.SetActive(false);
            for (int i = 0; i < Order.Length; i++) SetSlotActive(i, null);
        }

        /// <summary>
        /// 弹出并给选项。**按 `Kind` 对应按钮，不靠下标** ——
        /// 调用方给哪几项、给不给全，都不会串位。
        /// </summary>
        /// <param name="note">标题上面再加一行小字（技能效果之类）。空就不显示</param>
        public void Show(List<Option> options, string title = null, string note = null)
        {
            Build();
            _options.Clear();
            if (options != null) _options.AddRange(options);
            _titleText = title;
            _noteText = note;
            Visible = true;
            Hovered = AttackKind.None;
            RefreshLayout();
        }

        /// <summary>
        /// 按当前分辨率把按钮摆一遍。
        /// ⚠️ 位置只在建的时候算一次的话，切到 4:3 就全错位 —— HUD 那几个 Label 踩过一模一样的坑
        /// （见 `BattleDriver.ReanchorHud`）。所以摆位独立成这一个函数。
        /// </summary>
        public void RefreshLayout()
        {
            if (!Visible) return;

            // 屏幕正中央 = 世界原点（`LayoutSpace` 的约定：可见高度恒 10、中心为 0）
            //
            // ⚠️ **z 要取负值** —— 相机看 -Z，**小的 z 靠前**。选择器是一条压在棋盘上的覆盖层
            //    （原版也是：那条带 479..601 本身就压着两排棋盘的边），必须排在所有卡牌前面。
            //    HUD 那几个图用的是 +0.3（在卡牌**后面**），这里不能照抄（踩过：标题和按钮被卡牌盖住）
            var center = Vector3.zero;

            if (_background != null)
            {
                _background.gameObject.SetActive(true);
                // 底板从**槽顶往下 18.4 px** 挂起（原版 anchor (0.5,1) + pivot (0.5,1)）
                _background.transform.localPosition =
                    center + new Vector3(0f, W(BarH * 0.5f - BgDropY), ZBg);
            }
            if (_title != null)
            {
                _title.gameObject.SetActive(!string.IsNullOrEmpty(_titleText));
                _title.SetText(_titleText);
                _title.transform.localPosition = center + new Vector3(0f, W(BarH * 0.5f + 26f), ZText);
            }
            if (_note != null)
            {
                _note.gameObject.SetActive(!string.IsNullOrEmpty(_noteText));
                _note.SetText(_noteText);
                _note.transform.localPosition = center + new Vector3(0f, W(BarH * 0.5f + 54f), ZText);
            }

            // 只排**这次真的要显示**的那几项，居中 —— 原版是 HorizontalLayoutGroup(MiddleCenter)，
            // 所以只有两个按钮时它们会往中间收，不会留着技能那个空档
            var shown = new List<AttackKind>();
            for (int i = 0; i < Order.Length; i++)
                if (Find(Order[i]) != null) shown.Add(Order[i]);

            for (int i = 0; i < Order.Length; i++) SetSlotActive(i, null);

            for (int n = 0; n < shown.Count; n++)
            {
                int i = SlotOf(shown[n]);
                var opt = Find(shown[n]);
                float x = (n - (shown.Count - 1) * 0.5f) * W(ButtonSize + ButtonSpacing) * LayoutSpace.Scale;
                var p = center + new Vector3(x, 0f, 0f);

                SetSlotActive(i, opt);
                if (_icons[i] != null) _icons[i].transform.localPosition = p + new Vector3(0f, 0f, ZIcon);
                if (_rings[i] != null) _rings[i].transform.localPosition = p + new Vector3(0f, 0f, ZRing);
                if (_captions[i] != null)
                {
                    _captions[i].SetText(opt.Badge);
                    _captions[i].transform.localPosition =
                        p + new Vector3(W(BadgeDx), W(BadgeDy), ZText);
                }

                // 不能选的置灰 —— 「这个单位现在打不了这一种」要一眼看出来
                float dim = opt.Enabled ? 1f : 0.42f;
                if (_icons[i] != null)
                    _icons[i].SetTint(new Color(dim, dim, dim, opt.Enabled ? 1f : 0.8f));
            }

            RefreshHover();
        }

        /// <summary>`opt` 传 null = 关掉这一格（三个按钮节点是**一次建满**的，之后只切显隐）</summary>
        void SetSlotActive(int i, Option opt)
        {
            bool on = opt != null;
            if (_icons[i] != null) _icons[i].gameObject.SetActive(on);
            if (_rings[i] != null) _rings[i].gameObject.SetActive(on && opt.Enabled);
            if (_captions[i] != null)
                _captions[i].gameObject.SetActive(on && !string.IsNullOrEmpty(opt.Badge));
        }

        // ==================================================================
        //  指针
        // ==================================================================

        /// <summary>喂指针世界坐标（每帧）。返回压着的那个按钮。</summary>
        public AttackKind UpdatePointer(Vector3 world)
        {
            if (!Visible) return AttackKind.None;

            var hit = AttackKind.None;
            for (int i = 0; i < Order.Length; i++)
            {
                var opt = Find(Order[i]);
                if (opt == null || !opt.Enabled) continue;
                if (_icons[i] == null || !_icons[i].gameObject.activeSelf) continue;

                // 命中按**圆形**判：图标是圆钮，用方形外接框会「还没碰上就选中」
                float half = W(ButtonSize) * 0.5f * LayoutSpace.Scale;
                var d = _icons[i].transform.localPosition - world;
                if (d.x * d.x + d.y * d.y <= half * half) { hit = opt.Kind; break; }
            }

            if (hit != Hovered) { Hovered = hit; RefreshHover(); }
            return Hovered;
        }

        /// <summary>把「压着谁」画出来：压着的放大 1.3 倍（`scaleMultiplierWhenSelected`）+ 亮黄圈</summary>
        void RefreshHover()
        {
            for (int i = 0; i < Order.Length; i++)
            {
                var opt = Find(Order[i]);
                if (opt == null) continue;

                bool hot = Hovered != AttackKind.None && opt.Kind == Hovered;
                float s = hot ? HighlightScale : 1f;
                if (_icons[i] != null) _icons[i].transform.localScale = new Vector3(s, s, 1f);
                if (_rings[i] != null && _rings[i].gameObject.activeSelf)
                {
                    _rings[i].transform.localScale = new Vector3(s, s, 1f);
                    _rings[i].SetTint(hot ? Color.white : new Color(1f, 1f, 1f, 0.28f));
                }
            }
        }

        /// <summary>指针是不是在整条槽的范围里（点在槽外 = 取消）</summary>
        public bool ContainsBar(Vector3 world)
        {
            if (!Visible) return false;
            float hw = W(BarW) * 0.5f * LayoutSpace.Scale;
            float hh = W(BarH + BgH * 0.5f) * LayoutSpace.Scale;   // 连底板一起算进去
            return Mathf.Abs(world.x) <= hw && world.y <= hh && world.y >= -hh * 2f;
        }

        /// <summary>诊断用：这一次给了哪些选项</summary>
        public string Describe()
        {
            if (!Visible) return "（收起）";
            var parts = new List<string>();
            foreach (var o in _options)
                parts.Add($"{o.Kind}{(o.Enabled ? "" : "(灰)")}"
                        + (string.IsNullOrEmpty(o.Badge) ? "" : " [" + o.Badge + "]"));
            return "[" + string.Join(" ", parts.ToArray()) + "]"
                 + (Hovered != AttackKind.None ? " 压着 " + Hovered : "");
        }

        // ---- 给自检用的 ----

        /// <summary>某个按钮在世界坐标的哪儿（没显示这一项就返回 null）</summary>
        public Vector3? ButtonWorld(AttackKind k)
        {
            int i = SlotOf(k);
            if (i < 0 || !Visible) return null;
            if (_icons[i] == null || !_icons[i].gameObject.activeSelf) return null;
            return _icons[i].transform.localPosition;
        }

        /// <summary>某个按钮当前的缩放（压着时应当是 1.3）</summary>
        public float ButtonScale(AttackKind k)
        {
            int i = SlotOf(k);
            if (i < 0 || _icons[i] == null) return 0f;
            return _icons[i].transform.localScale.x;
        }

        /// <summary>
        /// 1×1 白贴图 —— 原版底板那格没有 sprite（纯色），我们也用纯色板。
        /// ⚠️ 做成**实例字段**而不是 static：static 的 UnityEngine.Object 会跨场景存活，
        ///    场景一重载它就成了野指针（这工程在「共享材质」上踩过同一类坑）。
        /// </summary>
        Texture2D _solid;
        Texture2D Solid()
        {
            if (_solid == null)
            {
                _solid = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _solid.SetPixel(0, 0, Color.white);
                _solid.Apply();
                _solid.name = "solid_white";
            }
            return _solid;
        }
    }
}
