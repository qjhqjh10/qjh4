// ShowEventArmiesPanel.cs — 活动窗里「可玩阵营」那一行图标（原版 `ShowEventArmiesPanel` 的等价物）
//
// ============================ 出处（唯一正本）============================
// 原版类 = `ShowEventArmiesPanel : MonoBehaviour`（`dump.cs` TypeDefIndex 1648 之后同族；
//   ILSpy 全文 `cpp2il/out_cpp2il_dev_ilspy/ShowEventArmiesPanel.cs:8-111`）
//   · 字段：`armiesReferenceObject`(Image) · `anchor`(Transform) · `currentArmyObjects`(List<Image>)
//   · `Initialize(CardArmy[] cardArmies)`（体 46 行）：
//       ① `if (currentArmyObjects.Count > 0) Reset();`
//       ② `armiesReferenceObject.gameObject.SetActive(true);`
//       ③ `foreach (army in cardArmies) { var img = Instantiate(armiesReferenceObject, anchor);
//            currentArmyObjects.Add(img);
//            img.sprite = ArmyUtilities.GetArmyIcon(army); }`
//       ④ `armiesReferenceObject.gameObject.SetActive(false);`
//   · `Reset()`：逐个 `Destroy(currentArmyObjects[i].gameObject)` + 清表。
//   （②④ 那对 SetActive **不是多余**：`Instantiate` 出来的克隆继承模板的 `activeSelf`
//     ⇒ 模板先开、克隆才是活的；建完把模板关掉。⛔ 别省成「模板恒关」。）
//
// **唯一调用点** = `SinglePlayerOnlyEnergyEventWindow.Open()`（`SinglePlayerOnlyEnergyEventWindow__Open.c:114`）：
//   它先取「与该事件同目标」的奖励事件们（`RewardEventHandler.GetEvents().Where(e => e.IsTargetTo(ev))`），
//   把它们的 `Army` 收成数组交给本面板。⇒ 这一行图标 **只属于能量活动窗**。
//
// ============================ 我们这一侧（如实标注边界）============================
// 🔴 **本组件已建、但未接线**（真缺口，不是省事）：唯一原版调用点在
//   `Shell/EnergySinglePlayerOnlyEventWindow.cs`（**D2 的文件，本代理不许动**）
//   ⇒ 接线 = 在那个窗口 `Open()` 里调一行 `_armiesPanel.Initialize(armies)`（含 `Build` 一次）。
//   本文件把 **Build + Initialize + Reset** 三件一起给出来，就是为了让那一行接线成立即可。
//
// 几何全部来自 **prefab 现读**（不是我们挑的）：
//   `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Army Selector Panel" --depth 5`
//   （2026-10-28 现跑，祖先 = `EnergySinglePlayerOnlyEventWindow` 0,0→1920,1080）：
//   · `Army Selector Panel`  0.0,606.7 → 680.8,903.7（680.75×296.98，本身没有图）
//   · `Factions Title`       37.8,606.7 → 601.4,661.4（563.61×54.68）TMP `'Factions'` fs 44.65 · base 36 ·
//                            auto[18~45.87] · Left/Midline · 折行 1 · 挂 `Localize`（`MainMenu/Factions`）
//   · `Army Selector`        37.8,666.7 → 643.0,803.2（605.22×136.50）ScrollRect h=1 v=0 · elasticity 0.1 ·
//                            decel 0.135 · `m_RaycastTarget=1`；挂 `ShowEventArmiesPanel`
//   · `Viewport`             同上矩形 · `RectMask2D`
//   · `Army Content`         CSF h:PreferredSize + HLG（spacing 0 · align 0 UpperLeft · pad 0 ·
//                            ctrlW 0 · ctrlH 1 · expandW 1 · expandH 1）⇒ 子件从左上起、每颗 **136.50 宽**
//   · `ArmyItemReference`    136.50×136.50（Image + `AspectRatioFitter` 高控宽 ⇒ 正方形；模板**没有图**）
//   · `Faction instructions` 37.8,813.7 → 643.0,895.2（605.22×81.47）TMP fs 42.95 · base 36 · auto[18~72] ·
//                            Left/Top · 折行 1 · 挂 `Localize`（`Event_Description/ScoringEvent/PlayWithFactions`）
//
// ⚠️ **未复刻的一小块（如实记）**：`Army Selector` 那颗 `ScrollRect` 是**横向可滚**（h=1 v=0，
//   13 个阵营 ⇒ 10 颗会溢出到视口外）。本文件的 `Build` **只摆位 + 不外露**（`Viewport` 那颗节点建出来当裁切锚），
//   **没有滚动输入**（我们这一层的滚动件 `MenuScroll` 目前只接了纵向）。⇒ 记 B：滚动交互缺，其余齐。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ShowEventArmiesPanel` —— 活动「可玩阵营」图标行（每个阵营一颗 136.5² 的徽记）。
    /// <para>行为逐句照原版 `Initialize` / `Reset`（见文件头）；`Build` 那一半是**原版 prefab 的静态四节点**
    /// （几何/prefab 现读，⛔ 不是我们挑的版式）。</para></summary>
    public sealed class ShowEventArmiesPanel
    {
        // ---- 几何（prefab 现读，绝对画布 · 1920×1080 · 左上原点 · y 向下）----
        public const float PanelL = 0f, PanelT = 606.69f, PanelR = 680.75f, PanelB = 903.67f;
        public const float TitleL = 37.77f, TitleT = 606.69f, TitleR = 601.38f, TitleB = 661.37f;
        public const float ScrollL = 37.77f, ScrollT = 666.69f, ScrollR = 642.99f, ScrollB = 803.20f;
        public const float InstrL = 37.77f, InstrT = 813.66f, InstrR = 642.99f, InstrB = 895.13f;
        /// <summary>一颗徽记的边长 —— `ArmyItemReference` 在 `AspectRatioFitter(高控宽)` 之后 = **136.50²**。</summary>
        public const float ItemSide = 136.50f;
        /// <summary>`Army Content` 的 HLG `spacing = 0`（现读）。</summary>
        public const float ItemSpacing = 0f;

        /// <summary>原版 `armiesReferenceObject`（模板 `Image`）—— 我们没有实例化语义，用它当「有没有建过」的闩。</summary>
        Transform _reference;
        /// <summary>原版 `anchor`（= `Army Content`）。</summary>
        Transform _anchor;
        /// <summary>原版 `currentArmyObjects`。</summary>
        readonly List<ImageQuad> _currentArmyObjects = new List<ImageQuad>();
        /// <summary>与 `_currentArmyObjects` 一一对应的 sprite 名（自检读口用；原版没有这一格，它只留 `Image`）。</summary>
        readonly List<string> _arts = new List<string>();
        /// <summary>建的时候定下的图队列档（`Initialize` 建子件要用同一档）。</summary>
        int _qArt = 3111;
        /// <summary>建出来的那棵（`Build` 之前是 null）。</summary>
        Transform _root;

        /// <summary>原版 `currentArmyObjects.Count`（自检读口）。</summary>
        public int Count { get { return _currentArmyObjects.Count; } }
        /// <summary>第 `i` 颗的 sprite 名（没有 ⇒ `null`；自检读口 —— ⛔ 只用来对「这一颗是不是那个阵营的徽记」）。</summary>
        public string ItemSpriteName(int i)
        {
            return (i >= 0 && i < _arts.Count) ? _arts[i] : null;
        }
        /// <summary>第 `i` 颗的节点（自检量矩形用）。</summary>
        public Transform Item(int i)
        {
            if (i < 0 || i >= _currentArmyObjects.Count) return null;
            var q = _currentArmyObjects[i];
            return q != null ? q.transform : null;
        }

        /// <summary>建那棵静态四节点（`Army Selector Panel` ▸ 标题 / 滚动视口 / 内容 / 说明），并记住
        /// `anchor`（= `Army Content`）。⚠️ 原版这些节点在 prefab 里，本方法只是**把同一份几何摆出来**。
        /// <para>`qArt` / `qArt1` / `qText` 由调用方给（各窗的队列档不同）—— 缺省取 LiveOps 那一档。</para></summary>
        public static ShowEventArmiesPanel Build(Transform parent, int qArt = 3111, int qArt1 = 3112, int qText = 3114)
        {
            var p = new ShowEventArmiesPanel();
            p._qArt = qArt;
            var root = MenuDraw.Node(parent, "Army Selector Panel", new PxRect(PanelL, PanelT, PanelR, PanelB));

            // `Factions Title`（TMP 'Factions'，挂 `Localize` ⇒ `MainMenu/Factions`）
            MenuDraw.Text(root, new PxRect(TitleL, TitleT, TitleR, TitleB), Loc.T("MainMenu/Factions"),
                          Color.white, "Factions Title", 44.65f, qText,
                          TitleR - TitleL, 18f, 45.87f, 36f, face: TmpFont.Face.LatinCardBody);

            // `Army Selector`（ScrollRect 那一颗：本层只是**节点的宿主**，滚动件未接，见文件头）
            var sel = MenuDraw.Node(root, "Army Selector", new PxRect(ScrollL, ScrollT, ScrollR, ScrollB));
            var vp = ViewportClip.Hang(sel, "Viewport", new PxRect(ScrollL, ScrollT, ScrollR, ScrollB), Vector4.zero, Vector2Int.zero); p._viewport = vp; p._selector = sel;
            // `Army Content`（CSF h + HLG）：模板宽 = 一颗；真宽由子件撑开（下面 `Initialize` 只管加子件）
            var content = MenuDraw.Node(vp != null ? vp.transform : sel, "Army Content", new PxRect(ScrollL, ScrollT, ScrollL + ItemSide, ScrollB));
            // `ArmyItemReference` —— 模板本身**没有图**（`<无图>` 现读）；留着它，`Initialize` 用它的尺寸与层级
            var refQ = MenuDraw.Rect(content, CardArt.Solid(), new PxRect(ScrollL, ScrollT, ScrollL + ItemSide, ScrollB),
                                     "ArmyItemReference", qArt, new Color(1f, 1f, 1f, 0f));
            if (refQ != null) refQ.gameObject.SetActive(false);      // 原版建完就 `SetActive(false)`
            p._reference = refQ != null ? refQ.transform : null;
            p._anchor = content;

            // `Faction instructions`（TMP，挂 `Localize` ⇒ `Event_Description/ScoringEvent/PlayWithFactions`）
            var instr = MenuDraw.TextBox(root, new PxRect(InstrL, InstrT, InstrR, InstrB),
                                         Loc.T("Event_Description/ScoringEvent/PlayWithFactions"), Color.white,
                                         "Faction instructions", 42.95f, 18f, qText, 72f, 36f,
                                         face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(instr, new PxRect(InstrL, InstrT, InstrR, InstrB));

            p._root = root;
            if (p._anchor == null)
                Debug.LogWarning("[Event] `Army Selector Panel` 的 `Army Content` **没建出来** ⇒ 下面 `Initialize` 无处挂图标");
            return p;
        }

        /// <summary>原版 `Initialize(CardArmy[])`：先清旧的，再按顺序一颗一颗摆徽记（`GetArmyIcon(army)`）。
        /// <para>`Neutral(0)` / 认不出的阵营 ⇒ `ArmyIcons.Texture` 返回 `null` ⇒ **这一颗不建**（原版那一支
        /// `sprite = null` 的等价物：屏幕上是空白格 —— 我们连格都不建，并在日志里说清是哪一档）。</para></summary>
        public int Initialize(int[] armies)
        {
            // 原版 `ShowEventArmiesPanel.Initialize(CardArmy[])` 七句（判据 → 文件头）：
            // ① `Count > 0 ⇒ Reset()` ② 模板 `SetActive(true)` ③ 逐阵营 `Instantiate(模板, anchor)` +
            //    `sprite = ArmyUtilities.GetArmyIcon(army)` ④ 模板 `SetActive(false)`。
            // 本方法**不再自己摆** —— 摆位 / 裁切 / 滚动全在类尾 `RebuildItems`（唯一一份实现）。
            // 我们这侧的三处等价：
            //   · `Instantiate` → `MenuDraw.Rect`（本壳没有 prefab 实例）；
            //   · `GetArmyIcon(army)` → `ArmyIcons.Texture/IconName`（映射唯一一份 → `Core/ArmyIcons.cs`）；
            //   · ②④ 那一对 `SetActive` → 本壳的模板**出厂就关着**，而 `MenuDraw.Rect` 建出来的子件是「活」的
            //     （不像 `Instantiate` 那样继承模板的关）⇒ 这里不需要「先开」那句；⛔ 别照抄成「模板恒开」，
            //     那会在空表时留下**一颗空壳**在图里（而且断言只数 `Count` 时看不出来）。
            // ⚠️ **缺图那一档**：原版 `sprite = null` 时 `Image` 仍挂着（屏幕上是空白格）；
            //    我们 `ArmyIcons.Texture` 返 null（`Neutral` / 未收录）⇒ **那一颗不建**，并出声说清是哪一档。
            // 🔴 **布局与滚动**（2026-10-28 · B93-E29）：
            //    · `Army Content` 的 HLG 是 `spacing 0 / align UpperLeft`，每格 **136.501**，
            //      第 i 格 x = `ScrollL + i × 136.501`（prefab 现读，`menu_dump --depth 14`）；
            //    · 外面那颗 `Army Selector` 是**横向 ScrollRect**（`h=1 v=0` · Elastic · decel 0.135），
            //      `Viewport` 带 `RectMask2D` ⇒ 滚出视口的格子**要被裁掉**（不是「摆到屏幕外」）；
            //    · 我们这层走全壳唯一那份 `MenuScroll`（`Vertical = false` = 横向）⇒ 内容坐标先 `Shift` 再摆；
            //      可滚范围 = `[ScrollL, ScrollL + n × 136.501]`，由 `MenuScroll` 的极值自己夹。
            //    ⛔ **别为了「13 颗摆得下」去缩格/改几何** —— 那是伪造布局；原版就是滚。
            //    ⚠️ 滚动区是**懒建**的（`EnsureScroll`）：批处理/断言里 `PointerLayer` 不存在也照建，
            //      只是没登记 ⇒ 自检用 `Scroll.ScrollBy(...)` 直调（同 `MenuScroll` 文件头的用法③）。
            if (_currentArmyObjects.Count > 0) Reset();
            if (_anchor == null) { Debug.LogWarning("[Event] `ShowEventArmiesPanel` 没有 `Army Content` ⇒ 图标行**建不出来**"); return 0; }
            _armies = armies == null ? new int[0] : (int[])armies.Clone();
            RebuildItems();
            Debug.Log("[Event] 活动可玩阵营图标行 = " + _currentArmyObjects.Count + " 颗（原版 `ShowEventArmiesPanel.Initialize`）");
            return _currentArmyObjects.Count;
        }

        /// <summary>原版 `Reset()`：把每一颗都 `Destroy` 掉并清表。</summary>
        public void Reset()
        {
            for (int i = 0; i < _currentArmyObjects.Count; i++)
            {
                var q = _currentArmyObjects[i];
                if (q == null) continue;
                RewardsWindow.DestroySafe(q.gameObject);
            }
            _currentArmyObjects.Clear();
            _arts.Clear(); _armies = new int[0]; if (_scroll != null) { _scroll.ContentX2 = ScrollL; _scroll.SetOffset(_scroll.Offset); }
        }

        // ============================================================ 🆕 2026-10-28（B93-E29）· 横向滚动 + 重建
        //
        // 判据（这一段全部现读，⛔ 别去别处找第二份）：
        //  · `Army Selector` 那颗 `ScrollRect`：`m_Horizontal = 1 / m_Vertical = 0` · `m_MovementType = 1 (Elastic)` ·
        //    `m_Elasticity = 0.1` · `m_Inertia = 1` · `m_DecelerationRate = 0.135`（prefab 现读：
        //    `menu_dump.py bundle_menus_assets_all "EnergySinglePlayerOnlyEventWindow" --depth 14 --relative`）。
        //  · `Viewport` 是 `RectMask2D`（真掩码）⇒ 滚出视口的格子要被裁掉。
        //  · 🔴 **「`MenuScroll` 只接纵向」这条旧记录不成立**：`MenuScroll` 本身就是**双轴**的
        //    （`Vertical` 缺省 **false = 横向**，`Shift` / `MinOffset` / `MaxOffset` / `ViewLo·Hi` 全按
        //    `Vertical` 选轴；`LeftAligned` 就是这个模式）⇒ 这里**不缺能力，缺的是接线**。
        //    ⛔ 别为此去改 `Shell/MenuScroll.cs`（共用件，多扇窗在用）。
        //  · ⚠️ `m_ScrollSensitivity` **没现读到**（`D_N29_滚动灵敏度逐视图.md` 里只有练习窗/教程窗的
        //    `Army Selector = 50`，**不是这一台**）⇒ 用 `MenuScroll.DefaultNotchPx` 并**出声**
        //    （⛔ 不拿 50 顶替 —— 铁律 5·c「同一个名字不是同一台」）。

        /// <summary>当前这一行图标对应的阵营表（自检读口；原版没有这一格 —— 它只有 `currentArmyObjects`）。</summary>
        int[] _armies = new int[0];
        /// <summary>`Army Selector` 那一颗（原版 `ScrollRect` 宿主节点）。</summary>
        Transform _selector;
        /// <summary>`Viewport` 那一颗（原版 `RectMask2D` ⇒ 我们这份 `ViewportClip`）。</summary>
        ViewportClip _viewport;
        /// <summary>横向滚动区（原版 `Army Selector` 上的 `ScrollRect` 等价物）。**懒建**（`EnsureScroll`）。</summary>
        MenuScroll _scroll;
        /// <summary>重建中（`OnChanged` → `RebuildItems` 的递归闸）。</summary>
        bool _rebuilding;
        /// <summary>本组件的滚动区（自检读口 —— 原版那颗 `ScrollRect`）。</summary>
        public MenuScroll Scroll { get { return _scroll; } }
        /// <summary>原版 `ShowEventArmiesPanel.anchor`（= `Army Content`）。</summary>
        public Transform Anchor { get { return _anchor; } }
        /// <summary>原版 `armiesReferenceObject` 的父节点（= `Army Selector`）。</summary>
        public Transform Selector { get { return _selector; } }
        /// <summary>建出来的那棵根（`Army Selector Panel`）。</summary>
        public Transform Root { get { return _root; } }

        /// <summary>按当前阵营表重建图标行（原版 `Initialize` 的 ②③④；滚动偏移变了也走这里）。
        /// <para>🔴 **为什么整行重建而不是挪节点**：`MenuDraw.Rect` 建出来的 `ImageQuad` 的矩形就是它自己的
        /// rect（本壳没有 uGUI 的 `anchoredPosition` 那一层），挪节点要自己写一份「内容坐标 → 屏幕坐标」——
        /// 那就是第二份 `MenuScroll.Shift`（CLAUDE.md §三）。重建一次最省事，也与 `MenuScroll` 文件头
        /// 用法③「偏移变化 ⇒ 宿主重建内容」同一条规矩。</para></summary>
        void RebuildItems()
        {
            if (_rebuilding) return;
            _rebuilding = true;
            var keep = _armies;                        // `Reset()` 会清表 ⇒ 先把它按住（重建用的是「当前这一张表」）
            float keepOffset = _scroll != null ? _scroll.Offset : 0f;   // 偏移也要按住（`Reset` 会把它夹回 0）
            try
            {
                Reset();                                   // 原版 ①
                _armies = keep;                            // `Reset` 会把 `_armies` 清掉（它是对外那个「清空」口）⇒ 在这里还原
                if (_anchor == null) return;
                var scroll = EnsureScroll();
                float contentX2 = ScrollL + _armies.Length * (ItemSide + ItemSpacing);
                if (scroll != null)
                {
                    scroll.ContentX1 = ScrollL;
                    scroll.ContentX2 = contentX2;
                    scroll.SetOffset(keepOffset);          // 还原滚动位置；格数变少 ⇒ `SetOffset` 自己夹回新范围
                }
                for (int i = 0; i < _armies.Length; i++)
                {
                    var tex = ArmyIcons.Texture(_armies[i]);
                    if (tex == null)
                    {
                        Debug.LogWarning("[Event] 可玩阵营 `" + _armies[i] + "`（第 " + i + " 个）**没有徽记图**"
                                       + "（`ArmyIcons.IconName` 未收录 / `CardArmy.Neutral`）⇒ 那一颗不建");
                        continue;
                    }
                    float x1 = ScrollL + i * (ItemSide + ItemSpacing);
                    var r = new PxRect(x1, ScrollT, x1 + ItemSide, ScrollB);   // 内容坐标
                    if (scroll != null) r = scroll.Shift(r);                   // → 屏幕坐标（只加偏移、不裁）
                    var q = MenuDraw.Rect(_anchor, tex, r, "Army Item " + i, _qArt, null, true);
                    if (q == null)
                    {
                        Debug.LogWarning("[Event] 可玩阵营第 " + i + " 颗**没建出来**（`MenuDraw.Rect` 返回 null）");
                        continue;
                    }
                    _currentArmyObjects.Add(q);
                    _arts.Add(ArmyIcons.IconName(_armies[i]));
                }
            }
            finally { _rebuilding = false; }
        }

        /// <summary>懒建那颗横向滚动区（原版 `Army Selector ▸ ScrollRect`）。
        /// ⚠️ `Owner` **必须非空**（`PointerLayer.PruneScrolls` 把 `Owner == null` 当死条目直接删）。
        /// ⚠️ `ClipNode` **必须宿主显式给**（`MenuScroll` 的注释：沿 `Owner` 往上找**恒找不到**）。</summary>
        MenuScroll EnsureScroll()
        {
            if (_scroll != null) return _scroll;
            if (_root == null) return null;
            _scroll = new MenuScroll(new PxRect(ScrollL, ScrollT, ScrollR, ScrollB), ScrollL, ScrollL)
            {
                Vertical = false,                       // 原版 `m_Horizontal = 1 / m_Vertical = 0`
                Elastic = true,                         // 原版 `m_MovementType = 1 (Elastic)`
                Owner = _root.gameObject,
                ClipNode = _viewport,
            };
            // 偏移变了 ⇒ 整行按新偏移重建（`MenuScroll` 文件头用法③）。
            _scroll.OnChanged = () => { if (!_rebuilding) RebuildItems(); };
            // ⚠️ 只在 Play 里登记：`PointerLayer.Instance` 的惰性现建在批处理/编辑器里会**留下一个游离 GameObject**
            //   （`DontDestroyOnLoad` 那一支被 `isPlaying` 挡掉了，但 `new GameObject` 照样执行）——
            //   而 `ShellScene.Run` 一类宿主会 `SaveScene()`，那个游离物会**被写进场景**。
            //   批处理里本来也没有指针输入 ⇒ 登记没有意义；自检直调 `Scroll.ScrollBy(...)` 不受影响。
            if (Application.isPlaying) PointerLayer.RegisterScroll(_scroll);
            Debug.Log("[Event] `Army Selector` 接了**横向**滚动（原版 `ScrollRect`：h=1 v=0 · Elastic · decel 0.135）；"
                    + "⚠️ 那一台的 `m_ScrollSensitivity` **没现读** ⇒ 本区用缺省 `DefaultNotchPx` = "
                    + MenuScroll.DefaultNotchPx + " px/格（⛔ 不是原版值，⛔ 别拿练习窗的 50 顶替）");
            return _scroll;
        }
    }

    /// <summary>「指向某一事件的奖励函数」的最小替身 —— 只为让 `RewardEvent.IsTargetTo` 有一条**可构造**的
    /// 正例（原版那两个类 `EventScoringFunction` / `GenericEventFunction` 在本工程**不存在**，
    /// 只有 `IRewardFunctionTarget` 这个等价接口；`MultiplyRewardsFunction` 是原版的「两者都不是」那一档）。
    /// <para>🔶 自己设计（非原版）：<这是宿主/自检用的替身> · <为什么必须自己定：原版那两类是 `ScriptableObject`
    /// 派生、带云脚本名，本机没有对应资产、也没有它们的序列化数据；而判据面只需要 `TargetEventId` 一格> ·
    /// <调度台 2026-10-28 裁定（B93-E29 接线所需）>。</para>
    /// <para>⛔ 不要把它当成「原版有一个这样的类」—— 它只回答「这条奖励事件指向哪个 id」。</para></summary>
    public sealed class RewardFunctionForEvent : IRewardFunction, IRewardFunctionTarget
    {
        /// <summary>原版 `EventScoringFunction.TargetEvent` / `GenericEventFunction.Args.Last()` 报出的目标事件 id。</summary>
        public string TargetEventId { get; set; }
        /// <summary>原版 `CloudScriptFunction`：本替身没有云脚本 ⇒ null（⛔ 不编名字）。</summary>
        public string CloudScriptFunction { get { return null; } }
        /// <summary>原版 `Preview(evt, rewards)`：替身原样返回（它只用于 `IsTargetTo`，不参与奖励计算）。</summary>
        public IEnumerable<RewardInfo> Preview(ILiveOps evt, IEnumerable<RewardInfo> rewards)
        {
            return rewards != null ? rewards : (IEnumerable<RewardInfo>)new RewardInfo[0];
        }
    }
}
