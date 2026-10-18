// WindowButton.cs — 从 `PromptPopup.cs` 拆出来的独立文件（原处是**次要类**，Unity 建不出 MonoScript）。
//
// 🔴 **为什么必须单开一个文件**（2026-10-11 · `A1506` / `A1481`）：Unity 只给**文件名 == 类名**
//   的那个类建 `MonoScript` 资产；次要类没有 MonoScript ⇒ 存场景时 `m_Script` 指不到资产 ⇒
//   Unity **当场烘一块内联 `!u!115` 存根**写进 level 文件 ⇒ player 加载时 `Position out of bounds!`
//   ⇒ `The file '.../levelN' is corrupted!` + 崩溃。判据与实测 → `资料/交接_第十七会话.md` §三 ·
//   `资料/任务_卡组卡牌战斗.md` 的 `A1506`。
//
// 通用按钮件（`PointerLayer` 派发时调它的 action）。

using UnityEngine;

namespace CardPresentation
{
    public class WindowButton : MonoBehaviour
    {
        public System.Action onClick;

        // ============================================================ 🆕 2026-10-03 悬停 / 按下（原版 UGUI 的按钮态）
        //
        // 🔴 **判据 = UGUI `Selectable.DoStateTransition` + 原版 prefab 里的实测值**（不是我们挑的）：
        //   · 原版 `bundle_menus_assets_all` 里 **1276 个按钮组件**逐个数过：`m_Transition` = 2(SpriteSwap) **630** ·
        //     1(ColorTint) **505** · 0(None) **141**。
        //     ⚠️ **2026-10-04 更正（铁律 5）**：下面这句原来写的是「`m_Colors` 全部是 UGUI 默认那组」——
        //     **不成立**：实测那 **1276 颗里 150 颗不是**（同一份数据独立复算过两遍）。
        //     分布：`EverguildToggle` 一族 **51** 颗 HL = 绿 `(0.292,0.953,0.682)` · **36** 颗浅蓝
        //     `(0.882,0.976,1)` · **19** 颗青 `(0.609,0.943,1)` · **13** 颗 `(0.6887,…)`（卡组行的两颗就在其中）· 其余零散。
        //     按 prefab 根分组最大的几族：`Give Feedback Popup menu` **41** · `Player Profile Window` **16** ·
        //     `Main Menu Settings Window` **14** · `Social Submenu Variant` 6 · `Collection Menu Variant` 5 …
        //     ⇒ **默认值只是「没被改过的那一批」**：`HighlightK` / `PressedK` 这两个常量**不能当全库判据**，
        //     要拿它比对必须**先逐颗读 `m_Colors`**（这就是本类只做「统一色偏兜底」时留下的那笔账）。
        //     卡组编辑窗里占了 2 颗：`Deck Selector Defensive Card Slot` 与 `Deck Selector Card Info button`
        //     的 HL = `(0.6887,0.6887,0.6887,1)` —— 那两颗的悬停**没走本类**，走 `DeckRuntime.RowHoverK`。
        //     ⚠️ 出处分界：**这一句不在** `按钮悬停图_普查.md` 里（那份没提 `m_Colors`）——
        //     数据出处 = `资料/普查产出_1003/卡组编辑器_按钮悬停图_普查.md` §四②。
        //     默认那一组本身是：`m_HighlightedColor = 0.9607843` · `m_PressedColor = 0.7843137` ·
        //     `m_FadeDuration = 0.1` · `m_ColorMultiplier = 1.0`。
        //   · UGUI 的 `ColorTint` = `targetGraphic.CrossFadeColor(tintColor, fadeDuration)` —— 它是
        //     **乘**在顶点色上（`canvasRenderer.SetColor`），所以「悬停 = 原色 × 0.9608」。
        //   · `SpriteSwap` 那 630 个原版**换的是同一件 graphic 的图**（`m_SpriteState.m_HighlightedSprite`，
        //     **630/630 全非空**）⇒ 那一档要换图才叫对。**本版只做了统一的色偏兜底**，
        //     逐个换图的活**已记账**（判据 = 每个按钮的 `m_SpriteState`）——
        //     ⚠️ **这是一处如实标注的偏离**（见 `项目任务.md` §三 第 29 条 A15）。
        //   · `m_Transition = 0` 的 141 个原版**悬停什么都不变** ⇒ 那些地方我们这一层也不该变色，
        //     但**本地判不出「我们的哪一颗对应原版哪一颗」** ⇒ 留作上面那条账的一部分。
        //
        // ⚠️ **批处理下没有帧循环**（`Update` 不跑）⇒ 自检要看色偏就直调 `Enter/Exit/SetInstant`：
        //    `PointerLayer.HoverAt(...)` 在非 Play 时**直接落到目标色**（照 `ImageQuad` 那一族的规矩）。

        /// <summary>指针进入 / 离开时各调一次（原版就是 UGUI 的 `IPointerEnterHandler` / `IPointerExitHandler`）。</summary>
        public System.Action onEnter, onExit;

        /// <summary>原版 `m_Colors.m_HighlightedColor`（**全库同一个值**）。</summary>
        public const float HighlightK = 0.9607843f;
        /// <summary>原版 `m_Colors.m_PressedColor`。</summary>
        public const float PressedK = 0.7843137f;
        /// <summary>原版 `m_Colors.m_FadeDuration`（秒）。</summary>
        public const float FadeSeconds = 0.1f;

        // ============================================================ 🆕 2026-10-07 A77⑮①：选中态
        //（原版 `Selectable.SelectionState.Selected`；这一段之前**整个没有** ⇒ 键盘导航「选中是真的、但看不见」）
        //
        // 🔴 **判据 = UGUI `Selectable`**（本机源码
        //   `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Selectable.cs`）：
        //   · **优先级**（那个 `currentSelectionState` 属性，逐句照抄）：
        //     `!IsInteractable()` ⇒ Disabled ＞ `isPointerDown` ⇒ Pressed ＞ `hasSelection` ⇒ Selected
        //     ＞ `isPointerInside` ⇒ Highlighted ＞ Normal。
        //     ⇒ **选中排在「悬停」之上、「按下」之下**（⛔ 不是与悬停并列，也不是在悬停之下）——
        //     所以「鼠标压着的那颗被键盘选中」时，它按 **Selected** 画、不按 Highlighted 画；
        //     而 `Release()` 之后**只要还选中着，就回不到 Normal**（这是最容易写错的一格）。
        //   · **视觉**（同文件的 `DoStateTransition`）：`ColorTint` 取 `m_Colors.m_SelectedColor`、
        //     `SpriteSwap` 取 `m_SpriteState.m_SelectedSprite`（后者为 null 时 `OverrideSprite = null` = 常态图）。
        //
        // 🔴 **原版那两个值逐个实读过**（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/`
        //   里 **1276** 个带 `m_SpriteState` 的 `Selectable`，一次扫全）：
        //   · `m_SelectedColor` = `(0.9608,0.9608,0.9608,1)` 的有 **1204/1276**（其余：51 颗 `EverguildToggle` 族绿
        //     `(0.2902,0.9529,0.6824)` · 11 颗 `(0.3686,0.8941,0.5882)` · 6 颗纯白 · 3 颗 `0.4906` · 1 颗红）。
        //     ⚠️ **它与 `m_HighlightedColor` 的默认值是同一个数**（1126 颗 HL 也是 0.9608）⇒ 默认那一族群
        //     **「选中」与「悬停」看起来一样**；**逐个读 `m_Colors` 才准**（这就是本类头部那条
        //     「`HighlightK` / `PressedK` 不能当全库判据」的账 —— 与 A15 同一笔）。
        //   · `m_SelectedSprite` **非空只有 77/1276**（其中 `m_Transition = 2` 的 71 颗）⇒ **1199 颗是 null**，
        //     而 UGUI 对 null 的处理 = 常态图 ⇒ 那 1199 颗的「选中」在**换图那一档上是零变化**。
        //     ⚠️ 我们**没有「我们这一颗 → 原版哪一颗」的映射**（A15 那笔账）⇒ 那 77 颗里的 selectedSprite
        //     **本地认不出是哪一张** ⇒ 换图那一档**不编**：`Bind` 过的钮在 Selected 态回到常态图
        //     （**与原版那 1199 颗一致**），⛔ 不拿「我们挑的一张图」顶上（铁律 3）。
        //   · 一处实证（**键盘导航真会走到的那一族**）：主菜单那 5 个导航钮的 `Selectable`
        //     （场景包 `bundle_scenes_scenes_mainmenuwarpforge`，`Main Menu Navigation Button - Home` 的
        //     MB `2098`）实读 `m_SelectedColor = 0.9607843`，而它自己的 `m_HighlightedColor` 是
        //     **`(0.7217,0.8152,1.0)`（浅蓝）** ⇒ 那颗钮上「选中」与「悬停」**画出来不一样**。

        /// <summary>**对位 UGUI `Selectable.SelectionState`**（值序照抄 `Selectable.cs` 里那个嵌套枚举）。
        /// ⚠️ 原版还多一枚 `Disabled = 4`：那一档在我们这里**不走色偏/换图**，走的是
        /// 「子树图形件换 `Everguild/UI/Greyscale` 材质」（见 `SoftDisabled` 那一段）——
        /// 本工程实测那几颗 `m_SpriteState.m_DisabledSprite` 全 null + `m_Transition = 2`
        /// ⇒ `m_Colors.m_DisabledColor`（a = 0.502）在它们身上是**死值**，⛔ 别照它做一个半透明观感。</summary>
        public enum BtnState { Normal = 0, Highlighted = 1, Pressed = 2, Selected = 3 }

        /// <summary>原版 `m_Colors.m_SelectedColor` 的**默认那一族**：`0.9607843`（**1204/1276** 颗，实测见上）。
        /// ⚠️ 与 `HighlightK` **同值**（原版就是这样定的默认色块），所以默认族群上「选中 ≈ 悬停」；
        /// 逐颗覆盖的那 72 颗（Toggle 族等）要**先读 `m_Colors` 再谈**（同 `HighlightK` 那笔账）。</summary>
        public const float SelectedK = 0.9607843f;

        bool _selected;

        /// <summary>这一刻这一颗**被选中**吗（原版 `Selectable.hasSelection`）。
        /// 改它的唯一入口 = <see cref="SetSelected"/>（= 原版 `Selectable.OnSelect/OnDeselect`）。</summary>
        public bool IsSelected { get { return _selected; } }

        /// <summary>此刻这一颗处在哪一态 —— **优先级照 UGUI `Selectable.currentSelectionState`**
        /// （Pressed ＞ Selected ＞ Highlighted ＞ Normal；`Disabled` 那一档见 `BtnState` 的注释）。
        /// 🔴 它是**当下算出来的合态**，不是「最后发生的那个事件」—— 例：鼠标压着一颗**且**它被选中 ⇒ `Pressed`；
        /// 松开之后**只要还选中着就回到 `Selected`（不是 `Normal`）**。</summary>
        public BtnState State
        {
            get
            {
                if (Pressed) return BtnState.Pressed;
                if (_selected) return BtnState.Selected;
                if (Hovered) return BtnState.Highlighted;
                return BtnState.Normal;
            }
        }

        /// <summary>🆕 A77⑮①：**把「选中」画出来** —— 唯一调用点 = `PointerLayer.Select`
        /// （= 原版 `EventSystem.SetSelectedGameObject` ⇒ 选中那一颗收 `OnSelect`、上一个收 `OnDeselect`）。
        /// 吸收层恒不选（`absorbOnly`：原版面板不是 `Selectable`，`PointerLayer.Select` 也把它按 `null` 处理）。
        /// ⚠️ 原版 `DoStateTransition` 用的是 `m_Colors.m_FadeDuration` 那 0.1s 补间 ⇒ 这里同样走 `SetTarget`。</summary>
        public void SetSelected(bool on)
        {
            if (absorbOnly) return;
            if (_selected == on) return;
            _selected = on;
            ApplyState();
        }

        /// <summary>按 `State` 摆色偏 + 换图 —— **四个事件入口（Enter/Exit/Press/Release）与 `SetSelected`
        /// 共用这一份**。原版就是「一颗 `Selectable` 只有一个状态机、`DoStateTransition` 一个出口」
        /// ⇒ 两处各写一遍必然对不上（CLAUDE.md §三那条）。</summary>
        void ApplyState()
        {
            var st = State;
            SetTarget(KeyOf(st));
            SwapTo(TexOf(st));
        }

        /// <summary>`ColorTint` 那一档要用的**颜色键**（原版 `DoStateTransition` 里从 `m_Colors` 取的那一格，
        /// 再 `targetGraphic.CrossFadeColor(...)` ⇒ **乘**在图形自己的颜色上）。
        /// 🔴 **2026-10-10（A222）就地改成 `Color`**：原来这里返回一个 `float`（只表达得了「灰键」），
        /// 而原版 `m_Colors` 是**四个 Color、逐颗可覆盖** —— 主菜单那 5 个导航钮的悬停就是**浅蓝**
        /// `(0.7217, 0.8152, 1.0)`（见 `HighlightKey`），一个标量表达不出来。
        /// `HighlightKey` / `PressedK` / `SelectedK` 都是 r=g=b 时**与旧实现逐位等价**。</summary>
        Color KeyOf(BtnState st)
        {
            switch (st)
            {
                case BtnState.Pressed: return new Color(PressedK, PressedK, PressedK, 1f);
                case BtnState.Selected: return new Color(SelectedK, SelectedK, SelectedK, 1f);
                case BtnState.Highlighted: return HighlightKey;
                default: return Color.white;              // Normal = 原色（`m_NormalColor` 实测 1198/1276 是纯白）
            }
        }

        /// <summary>`SpriteSwap` 那一档要换的图（原版 `m_SpriteState` 里对应的那一张）。
        /// 🔴 `Selected` 那一格 = **常态图** —— 它就是原版 `m_SelectedSprite` 为 null 时的行为
        /// （`OverrideSprite = null` ⇒ 画常态图），而原版 **1199/1276 颗就是 null**（见上面那段实读）。</summary>
        Texture TexOf(BtnState st)
        {
            switch (st)
            {
                case BtnState.Pressed: return _pressedTex ?? _hoverTex;
                case BtnState.Selected: return _normalTex;
                case BtnState.Highlighted: return _hoverTex;
                default: return _normalTex;
            }
        }

        /// <summary>关掉这一颗的色偏（原版 `m_Transition = 0` 的那 141 颗用得上）。</summary>
        public bool tintOnHover = true;

        /// <summary>🆕 **2026-10-10（A222）**：本颗 `m_Colors.m_HighlightedColor`。
        /// 默认 = 全库默认那一档 **0.9607843 灰**（= `HighlightK` 三通道同值）。
        /// 🔴 **逐颗覆盖的口**：原版 1276 颗里 **150 颗**的 `m_Colors` 不是默认那一组
        /// （分布与出处见类头那段 2026-10-04 的更正），而**本地没有「我们这一颗 → 原版哪一颗」的映射**
        /// （A15 那笔账）⇒ **只给有逐颗判据的站点覆盖**，⛔ 别按「看着像」批量改。
        /// 第一个用它的是主菜单那 5 个导航钮（浅蓝；判据 → `MainMenuRuntime.NavHoverKey`）。
        /// ⚠️ `Pressed` / `Selected` 两格**不给覆盖口** —— 那五颗的 `m_PressedColor` / `m_SelectedColor`
        /// 实测**就是全库默认值**（0.7843137 / 0.9607843），没有第二个值出现过；真遇到别的再开。</summary>
        public Color HighlightKey = new Color(HighlightK, HighlightK, HighlightK, 1f);

        // ============================================================ 🆕 **2026-10-10（A222）**
        // 把**这颗按钮之外**的图形也接进色偏 —— 原版那颗 `ColorTint` 打的**永远是「这颗 `Selectable`
        // 自己子树里的那颗图形」**（非空 `m_TargetGraphic` 逐条解下来 **10/10** 都在子树里，一次也没打到
        // 「另建的命中区」上）；而本类 `Collect()` 只收**自己子树** ⇒ 我们的等价物 = 「把这个 `Hit` 节点
        // 挪到按钮节点之下、并把该亮的图形件交给它」。
        // 🔴 **为什么要有这个口**：主菜单那 5 个导航钮 / 3 张模式卡的 `Hit` 是**另建的透明命中区**
        // （兄弟节点、quad 染成 `(0,0,0,0)`），可见的那颗图形**不在它的子树里** ⇒ 色偏全打在透明件上、
        // **画面上一点变化都没有**（等价于原版 `trans=0`，而原版这几颗是 `trans=1`）。
        // ⛔ **不能改成「默认也收兄弟/父节点下的件」**：本类被 **13 个文件**引用，而「另建透明命中区」
        // 那一族的写法本身是**对的**（`MenuDraw.Hit` / `AddHit` 的 46 处真调用有 1000+ 条断言盯着）
        // ⇒ 改默认会让 46 处一起动。
        // ⚠️ **不传 = 空 = 今天的行为**（老站点零影响）。
        // ⚠️ 必须在**第一次悬停之前**调用（`Collect()` 只收一次，顺带把当时的 `Tint` 记成基准色）。
        // 🔴🆕 **2026-10-13（A1725）**：这几颗**从本轮起变灰也认了**（`GrayTargets()` 的**第四组**）——
        //    改前只有色偏认得它们，置灰时它们纹丝不动（「变色认得、变灰不认」）。
        //    所以本方法的口径是「**这一组图形件**要一起变色 / 一起变灰」，两半同源
        //    （判据同 `GrayTargets()`：原版 `GetGraphicsInChildren(gameObject, 1)`）。
        /// <summary>参数 = 要跟着变色的那几颗图形（原版 `m_TargetGraphic` 的等价物）。</summary>
        public void TintOn(params ImageQuad[] extra)
        {
            _tqExtra = (extra != null && extra.Length > 0) ? extra : null;
            _tq = null;                  // `Collect()` 有「只收一次」的缓存 ⇒ 改完要让它重收
        }

        /// <summary>🆕 **2026-10-06（A94）「吸收层」专用标志** —— 这一颗**不是按钮**，是
        /// 「窗内面板吃掉这一下」的等价物（原版面板那颗 `Image` 的 `m_RaycastTarget = 1`，
        /// 而它的父链上没有任何 `IPointerClickHandler` ⇒ 那一下**什么都不做**）。
        ///
        /// <para>为真时 <see cref="Enter"/> / <see cref="Exit"/> / <see cref="Press"/> / <see cref="Release"/>
        /// / <see cref="Click"/> **全部直接返回**，于是：</para>
        /// <list type="bullet">
        /// <item>**零视觉副作用** —— 不上悬停色偏、不换图、不进 `Pressed` 态（原版面板没有 `Selectable`、
        /// 没有任何悬停/按下变化）；</item>
        /// <item>**零告警** —— `Click` 不派发（也就不会撞上「没有绑动作」那条给「**忘了绑**」用的真告警）、
        /// `Press` 也不会报「按下无图可换」（`Bind` 从不被调用、两张图都是 null）。</item>
        /// </list>
        ///
        /// <para>🔴 **它也不进键盘导航**（原版面板不是 `Selectable` ⇒ 方向键永远不该停在它上面）——
        /// 四条遍历一起跳过：`PointerLayer` 的 `SelectFirst` / `FindInDirection` / `ButtonCountForTest`
        /// 与 `Select`（按在它身上 = 原版 `DeselectIfSelectionChanged` 把选中清成 null）。</para>
        ///
        /// <para>由 `MenuDraw.Absorb` 建出来时置位 —— ⛔ **别在别处用它**。</para></summary>
        public bool absorbOnly;

        // ============================================================ 🆕 2026-10-03 A17：悬停 / 按下【换图】
        //
        // 原版一颗 `Selectable` 只有**一种** transition（UGUI `DoStateTransition`）：
        //   `ColorTint` 的 505 颗 = 变暗（上面那套色偏就是它）· `SpriteSwap` 的 **630 颗 = 换图**。
        // ⇒ **换图那一档不该再叠色偏**（`Bind` 会自动把 `tintOnHover` 关掉，不然两种行为同时上）。
        // 判据正本 = `资料/普查产出_1003/按钮悬停图_普查.md`（12 条命名规律 + 5 块逐颗表）。

        /// <summary>要换图的那一层 —— 原版 `Selectable.m_TargetGraphic`。
        /// ⚠️ **它常常不是按钮自己那层**：原版 `Generic Close Button Orange` 指子件 `Background`、
        /// `WebShop Button` 指子件 `Button Image` ⇒ 由调用方传**真正画着常态图的那个 `ImageQuad`**。</summary>
        public ImageQuad target;
        Texture _normalTex, _hoverTex, _pressedTex;
        /// <summary>绑定时那张 quad 的宽高比 —— **换图后要拉回来**（见 `SwapTo` 的注释）。</summary>
        float _targetAspect;
        /// <summary>🆕 2026-10-05：本颗的「按下无图可换」告警**只说一次**（同 `TipHovers` 那种一次性出声）。</summary>
        bool _pressedSilent;

        /// <summary>常态图 → 高亮图。表里没有的走 `&lt;常态图>_hover` 后备（普查 §一：绝大多数是这个规律）。</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> HoverNames =
            new System.Collections.Generic.Dictionary<string, string>
        {
            { "40K_settings_button",            "40K_settings_button_selected" },   // ⚠️ 不是 `_hover`
            { "40K_dropdown_field_closed",      "40K_dropdown_field_opened" },      // ⚠️ 不是 `_hover`
            { "UI_Button_Menu_Back",            "UI_Button_Menu_Back_Hover" },      // ⚠️ 大写 H
            { "UI_Button_Organe_Square_Normal", "UI_Button_Organe_Square_Hover" },  // ⚠️ 大写 H
        };

        /// <summary>常态图 → 按下图（**只有这几张存在**，其余退回高亮图；取不到按下图**不算缺图**）。</summary>
        static readonly System.Collections.Generic.Dictionary<string, string> PressedNames =
            new System.Collections.Generic.Dictionary<string, string>
        {
            { "40K_button",            "40K_button_pressed" },
            { "40k_general_bt_yellow", "40k_general_bt_yellow_pressed" },
            { "40k_bt_close",          "40k_bt_close_pressed" },
            { "40k_menu_bt",           "40k_menu_bt_pressed" },
            { "UI_Button_Mulligan",    "UI_Button_Mulligan_Pressed" },    // ⚠️ 大写 P
            { "UI_Button_Menu_Back",   "UI_Button_Menu_Back_Pressed" },   // ⚠️ 大写 P
        };

        /// <summary>**取不到的悬停图**（红线：不许静默失败）—— 每扇窗的自检断它为空。</summary>
        public static readonly System.Collections.Generic.List<string> MissingSwapArt =
            new System.Collections.Generic.List<string>();

        /// <summary>🆕 **2026-10-05（A50②）：取不到的按下图** —— `MissingSwapArt` 的**镜像记录**。
        /// 🔴 **为什么不能像 hover 那样被断成空**：原版那一档来自 `m_SpriteState.m_PressedSprite`，
        /// 实测 `bundle_menus_assets_all` 里 **1276 个带 `m_SpriteState` 的 `Selectable`**
        /// （复现：`grep -rl m_SpriteState MonoBehaviour/ | wc -l`）：
        /// `m_Transition = 2`(SpriteSwap) **630 颗 —— 悬停图与按下图 630/630 都非空**；
        /// `= 1`(ColorTint) 495 颗 · `= 0`(None) 134 颗 —— **两张都是空**（另有 10 / 7 颗带图但那一档不吃）。
        /// **全 1276 颗逐颗核过：悬停图空 ⇔ 按下图空（0 处不一致）** ⇒ **我们取不到按下图时退回高亮图**
        /// （`Press()` 里 `_pressedTex ?? _hoverTex`）**这一档多数是合法的**（本类只按常态图名推名字）。
        /// ⇒ 这份表**只出声、不当缺点断**（要断它得先有「我们这一颗 → 原版哪一颗」的映射 = A15 那笔账）。
        /// **真缺口只有一种**：连高亮图也没有 ⇒ 按下**画面什么都不变**（`Press()` 里那条告警，不许静默）。</summary>
        public static readonly System.Collections.Generic.List<string> MissingPressedArt =
            new System.Collections.Generic.List<string>();

        static string HoverNameFor(string art)
            => string.IsNullOrEmpty(art) ? null
             : (HoverNames.TryGetValue(art, out var h) ? h : art + "_hover");
        static string PressedNameFor(string art)
            => string.IsNullOrEmpty(art) ? null
             : (PressedNames.TryGetValue(art, out var p) ? p : art + "_pressed");

        /// <summary>把这一颗接到「悬停换图」上。`art` = **常态图的图名**（查表/后备用），
        /// `hoverArt` / `pressedArt` = **逐颗显式覆盖** —— 同一张常态图在不同按钮上配不同高亮图时必须用，
        /// 例：卡组四圆钮常态图都是 `UI_Button_Round_background`，高亮分别是 `back` / `deck_change` / `eye` / `back`。
        /// 🔴 **取不到高亮图就记进 `MissingSwapArt`**（自检会红），**不静默画成没反应**。</summary>
        public void Bind(ImageQuad t, string art, string hoverArt = null, string pressedArt = null)
        {
            target = t;
            if (t == null) return;                 // 没画出来的件没有图可换（调用点负责如实说明）
            _normalTex = t.Texture;
            _targetAspect = t.WorldH > 0f ? t.WorldW / t.WorldH : 0f;
            string hn = hoverArt ?? HoverNameFor(art);
            string pn = pressedArt ?? PressedNameFor(art);
            _hoverTex = CardArt.MenuUi(hn);
            _pressedTex = CardArt.MenuUi(pn);
            if (_hoverTex == null && !MissingSwapArt.Contains(art + " → " + hn))
                MissingSwapArt.Add(art + " → " + hn);
            // 🆕 2026-10-05（A50②）：**按下图的镜像记录** —— 原来只有 hover 那一条，按下图缺了**永远静默**
            //   （`MissingPressedArt` 那张表全工程 0 命中）。口径与「能不能断言」见那张表的注释。
            if (_pressedTex == null && !MissingPressedArt.Contains(art + " → " + pn))
                MissingPressedArt.Add(art + " → " + pn);
            // 原版一颗只有一种 transition ⇒ 换图那一档不再叠色偏
            tintOnHover = false;
        }

        void SwapTo(Texture tex)
        {
            if (tex == null) return;
            SetOn(target, tex);
            if (_nine != null) for (int i = 0; i < _nine.Length; i++) SetOn(_nine[i], tex);
        }

        void SetOn(ImageQuad q, Texture tex)
        {
            if (q == null) return;
            q.SetTexture(tex);
            // 🔴 **换完图必须把宽高比拉回来** —— `ImageQuad.SetTexture` 会把 `_aspect` 冲成**贴图自己的**比值，
            //    而我们这套 quad 的矩形是**按原版矩形定的**（`MenuDraw.Rect` 里那次 `SetAspect`）。
            //    2026-10-03 实测：不拉回来 ⇒ 那颗 300px 的钮 `WorldW` 变成 **329**（`SettingsScene.CheckRectS` 抓到的；
            //    **屏幕形状没变、只有逻辑宽度被改掉**，肉眼看不出来）。
            //    同族先例（同一句纪律，两处早就写了）：`BattleLogPanel:532` · `AlliancesTab:189`。
            if (_targetAspect > 0f) q.SetAspect(_targetAspect);
        }

        /// <summary>自检用：把一棵树里**所有接了换图的**按钮逐个悬停一遍，返回「没换 / 没还原」的描述
        /// （空串 = 全过），`n` = 查了几颗。⚠️ 批处理**没有帧循环** ⇒ 这里直调 `Enter/Exit`
        /// —— 它们正是 `PointerLayer.HoverAt` 会调的那两个（`onEnter/onExit` 目前**无人挂**，
        /// 所以直调没有副作用；将来谁挂了钩子，这条要改成走 `PointerLayer.HoverAt`）。</summary>
        public static string AuditHoverSwap(Transform root, out int n)
        {
            n = 0;
            if (root == null) return "";
            var bad = new System.Text.StringBuilder();
            var wasSel = DetachSelection(root);        // 🆕 A77⑮①：见 `DetachSelection`
            foreach (var wb in root.GetComponentsInChildren<WindowButton>(true))
            {
                if (wb == null || wb._hoverTex == null || wb.target == null) continue;
                n++;
                var before = wb.target.Texture;
                wb.Enter();
                if (wb.target.Texture != wb._hoverTex)
                    bad.Append("「").Append(wb.name).Append("」悬停**没换图**；");
                wb.Exit();
                if (wb.target.Texture != before)
                    bad.Append("「").Append(wb.name).Append("」离开**没还原**；");
            }
            RestoreSelection(wasSel);
            return bad.ToString();
        }

        /// <summary>🆕 **2026-10-07（A77⑮①）**：把一棵树里**当前处于选中态**的按钮**暂时摘下来**
        /// （`SetSelected(false)`），返回那一组；量完用 <see cref="RestoreSelection"/> 原样还回去。
        /// <para>🔴 **为什么必须要它**：原版的选中态**优先级在悬停之上**（`Selectable.currentSelectionState`）
        /// ⇒ 一颗**既被选中、又被悬停**的钮，`Enter()` 之后画的是 **Selected**、**不是** Highlighted。
        /// 而本类那两个审计函数问的是「悬停换图对不对」—— 拿一颗选中的钮去问，会在一个**与本条无关的原因上红**
        /// （而 `WindowsManager.OpenWindow` → `SelectFirstIn` 会给新开的窗**默认选一颗**，所以这种钮很常见）。
        /// ⇒ 口径：**审计只审它自己那一档**，选中态由 `SetSelected` 那条路单独验（`Editor/ShellScene.cs` ⑤·k）。</para></summary>
        static System.Collections.Generic.List<WindowButton> DetachSelection(Transform root)
        {
            var was = new System.Collections.Generic.List<WindowButton>();
            foreach (var wb in root.GetComponentsInChildren<WindowButton>(true))
                if (wb != null && wb.IsSelected) { was.Add(wb); wb.SetSelected(false); }
            return was;
        }

        /// <summary>把 <see cref="DetachSelection"/> 摘下来的选中态**原样还回去**（顺序照原样）。</summary>
        static void RestoreSelection(System.Collections.Generic.List<WindowButton> was)
        {
            if (was == null) return;
            for (int i = 0; i < was.Count; i++) if (was[i] != null) was[i].SetSelected(true);
        }

        /// <summary>🆕 **2026-10-05（A50②）**：把一棵树里**能按下换图的**按钮逐个**按下 → 松开**一遍
        /// （`AuditHoverSwap` 的镜像）—— 返回「按下没换图 / 松开没还原」的描述（空串 = 全过），
        /// `n` = 查了几颗（**一张图都换不出的那几颗不计入** —— 它们在 `Press()` 里另出声）。
        /// 判据 = 原版 `m_SpriteState.m_PressedSprite`（`m_Transition = 2` 的 **630 颗 630/630 全非空**，
        /// 见 `MissingPressedArt` 的注释）；**没有按下图时退回高亮图**（`Press()` 里那行 `??`）。
        /// ⚠️ **批处理没有帧循环** ⇒ 直调 `Press/Release`（就是 `PointerLayer` 在按下/抬起时调的那两个；
        /// **`onClick` 不在它们身上派发** —— 那要「按下与抬起落在同一件上」由 `PointerLayer` 判，
        /// 所以这里按一圈**不会触发任何购买/开窗**）。
        /// ⚠️ 跑完这颗按钮回到**未按下**（`Release` 按 `Hovered` 还原成常态图）。
        /// ⚠️ 与 `AuditHoverSwap` 同一条口径：比的是**调用前那张图** ⇒ 顺手能抓到
        /// 「底色被别人换掉却没 `SetNormalTex`」（松开时还原成旧图）。</summary>
        public static string AuditPressedSwap(Transform root, out int n)
        {
            n = 0;
            if (root == null) return "";
            var bad = new System.Text.StringBuilder();
            var wasSel = DetachSelection(root);        // 🆕 A77⑮①：同 `AuditHoverSwap`（选中的钮不吃 HighLighted 那一路）
            foreach (var wb in root.GetComponentsInChildren<WindowButton>(true))
            {
                if (wb == null || wb.target == null) continue;
                var want = wb._pressedTex != null ? wb._pressedTex : wb._hoverTex;
                if (want == null) continue;                  // 两张都没有 = `Press()` 那一档（那边出声）
                n++;
                var before = wb.target.Texture;
                wb.Press();
                if (wb.target.Texture != want)
                    bad.Append("「").Append(wb.name).Append("」按下**没换图**；");
                wb.Release();
                if (wb.target.Texture != before)
                    bad.Append("「").Append(wb.name).Append("」松开**没还原**；");
            }
            RestoreSelection(wasSel);
            return bad.ToString();
        }

        ImageQuad[] _nine;   // 九宫格那种「一颗按钮由 9 张小 quad 拼出来」的情形（见 BindNine）

        /// <summary>底色**由别人换掉之后**（例：设置窗页签选中态走 `SetTexture`）要调一次，
        /// 否则悬停退出时会把这一层恢复成**绑定时那一刻**的旧图。</summary>
        public void SetNormalTex(Texture t) { _normalTex = t; }

        /// <summary>九宫格按钮（`Image.Type = Sliced`）的换图 —— 原版换的是**同一个 `Image` 的 sprite**，
        /// 我们这边它被切成了 9 张小 quad ⇒ **每一张都要换**（只换中心那格 = 边框不跟着亮）。
        /// `40K_button` 与 `40K_button_hover` 实测**同尺寸 489×107**（2026-10-03 核对），uv 切分不变、只换纹理。</summary>
        public void BindNine(GameObject nine, string art, string hoverArt = null, string pressedArt = null)
        {
            if (nine == null) return;
            var qs = nine.GetComponentsInChildren<ImageQuad>();
            if (qs == null || qs.Length == 0) return;
            _nine = qs;
            Bind(qs[0], art, hoverArt, pressedArt);   // 常态图取第一张（九张同源）
        }

        /// <summary>便捷：**按钮自己就是那张图**时（`quad.gameObject.AddComponent&lt;WindowButton&gt;()` 那种写法）
        /// 直接绑自己身上那张，不必再传引用。</summary>
        public void BindSelf(string art, string hoverArt = null, string pressedArt = null)
            => Bind(GetComponent<ImageQuad>(), art, hoverArt, pressedArt);

        /// <summary>指针正压在这一颗上吗。</summary>
        public bool Hovered { get; private set; }
        /// <summary>左键正压在这一颗上吗（原版 `SelectionState.Pressed`）。</summary>
        public bool Pressed { get; private set; }

        ImageQuad[] _tq;
        Color[] _tqBase;
        /// <summary>🆕 **A222**：**这颗钮自己子树之外**、也要跟着变色的那几颗图形（`TintOn` 传进来的）。
        /// `null` = 今天的行为（只打自己子树里的 quad）。</summary>
        ImageQuad[] _tqExtra;
        /// <summary>当前 / 目标**颜色键**（原版 `m_Colors` 的那一格；`Color.white` = 原色）。
        /// 🔴 2026-10-10（A222）从 `float _k/_kTarget` 改成 `Color` —— 理由见 `KeyOf`。</summary>
        Color _k = Color.white, _kTarget = Color.white;

        /// <summary>指针进来（`PointerLayer` 唯一派发口）。</summary>
        public void Enter()
        {
            if (absorbOnly) return;         // 吸收层：原版面板没有任何悬停变化（见 `absorbOnly`）
            if (Hovered) return;
            Hovered = true;
            if (onEnter != null) onEnter();
            ApplyState();                   // 🆕 A77⑮①：走那个唯一的状态出口（选中在悬停之上，`ApplyState` 里判）
        }

        /// <summary>指针离开。</summary>
        public void Exit()
        {
            if (absorbOnly) return;         // 吸收层（与 `Enter` 配对）
            if (!Hovered) return;
            Hovered = false;
            if (onExit != null) onExit();
            ApplyState();                   // 🆕 还选中着 ⇒ 回 `Selected`（⛔ 不是无条件回 Normal）
        }

        /// <summary>左键按下（**不派发 `onClick`** —— 那只在「按下与抬起同一件」时才发生）。</summary>
        public void Press()
        {
            if (absorbOnly) return;         // 吸收层：进 `Pressed` 态会让整块面板变暗（原版没有这一档）
            Pressed = true;
            // 🆕 2026-10-05（A50②）：**按下连一张图都换不动**时出声一次 —— `SwapTo(null)` 是**静默早退**，
            //   原来这一档一点痕迹都不留（`MissingSwapArt` 只记 hover 那一路）。红线：不许静默失败。
            if (_pressedTex == null && _hoverTex == null && !_pressedSilent)
            {
                _pressedSilent = true;      // 每颗只说一次（这颗每次按都会走到这里）
                Debug.LogWarning("[WindowButton] `" + name + "`：按下**一张图都换不出来**（常态图 `"
                                 + (_normalTex != null ? _normalTex.name : "<空>")
                                 + "` 的按下图/高亮图都取不到）⇒ 这一档**静默无效**；"
                                 + "原版那一档 = `m_SpriteState.m_PressedSprite`（`trans=2` 的 630 颗**全非空**）");
            }
            ApplyState();                   // 🆕 A77⑮①：Pressed 优先级最高（> Selected > Highlighted）
            if (onDown != null) onDown();
        }

        /// <summary>左键抬起（不论抬在哪 —— 原版 `Pressed` 态在这一刻结束）。</summary>
        public void Release()
        {
            if (absorbOnly) return;         // 吸收层（与 `Press` 配对：那一边没进态，这一边也别退态）
            Pressed = false;
            ApplyState();                   // 🆕 A77⑮①：悬停⇒Highlighted、**选中⇒Selected**、都没有⇒Normal
            if (onUp != null) onUp();
        }

        public System.Action onDown, onUp;

        void SetTarget(Color key)
        {
            if (!tintOnHover) return;
            _kTarget = key;
            if (!Application.isPlaying) { _k = key; ApplyTint(); }
        }

        /// <summary>色偏补间（照原版 `m_FadeDuration = 0.1s`）。
        /// ⚠️ 只在 Play 里跑得到（批处理下 `Update` 不执行）—— 自检看的是**目标色**，见类注释。</summary>
        void Update()
        {
            if (_k == _kTarget) return;
            // 原版 `CrossFadeColor(color, m_FadeDuration)` 是**逐通道**补间 ⇒ 这里同一条形状。
            _k = Color.Lerp(_k, _kTarget,
                            Mathf.Clamp01(Time.unscaledDeltaTime / Mathf.Max(0.0001f, FadeSeconds)));
            if (Mathf.Abs(_k.r - _kTarget.r) < 0.001f && Mathf.Abs(_k.g - _kTarget.g) < 0.001f
                && Mathf.Abs(_k.b - _kTarget.b) < 0.001f) _k = _kTarget;
            ApplyTint();
        }

        /// <summary>这一颗名下**要跟着变色的 quad**。默认 = 自己这棵子树里的全部
        /// （直接挂在大图上的按钮就是这一档；`MenuDraw.Hit` 那种「另建一个透明命中区」的
        /// 子树里只有那个**透明** quad ⇒ 变色看不见 = 与原版 `m_Transition = 0` 等效）。
        /// 🆕 **2026-10-10（A222）**：`TintOn` 显式指定的那几颗**追加在后面** —— 给「另建透明命中区、
        /// 可见件在兄弟节点下」那一族用（原版 `m_TargetGraphic` 指的就是那颗可见件）。</summary>
        void Collect()
        {
            if (_tq != null) return;
            var own = GetComponentsInChildren<ImageQuad>(true);
            int ex = _tqExtra != null ? _tqExtra.Length : 0;
            _tq = new ImageQuad[own.Length + ex];
            for (int i = 0; i < own.Length; i++) _tq[i] = own[i];
            for (int i = 0; i < ex; i++) _tq[own.Length + i] = _tqExtra[i];
            _tqBase = new Color[_tq.Length];
            for (int i = 0; i < _tq.Length; i++) _tqBase[i] = _tq[i] != null ? _tq[i].Tint : Color.white;
        }

        void ApplyTint()
        {
            Collect();
            for (int i = 0; i < _tq.Length; i++)
            {
                if (_tq[i] == null) continue;
                var b = _tqBase[i];
                // 原版 `CrossFadeColor` 是**乘**在图形自己的颜色上（`CanvasRenderer.SetColor`）
                _tq[i].SetTint(new Color(b.r * _k.r, b.g * _k.g, b.b * _k.b, b.a));
            }
        }

        /// <summary>自检用：当前色偏系数（1 = 原色 · 0.9608 = 悬停 · 0.7843 = 按下）。
        /// 🔴 **2026-10-10（A222）更正读数口径**：颜色键改成 `Color` 之后，非灰键（逐颗覆盖色）
        /// 没有单一「系数」⇒ 这里返回**绿通道**。现有 5 处断言用的都是默认灰键（r=g=b）
        /// ⇒ 读数**逐位不变**（`ShellScene` 那三处 0.9608/0.7843/0.9608 照旧）。
        /// 要读整条颜色用 `TintKeyForTest`。</summary>
        public float TintKForTest { get { return _k.g; } }

        /// <summary>自检用：当前颜色键（`Color.white` = 原色）。判据 = 原版 `m_Colors` 的那一格。</summary>
        public Color TintKeyForTest { get { return _k; } }

        /// <summary>自检用：**当前贴在 `target` 上的图**（换图那一档靠它验「悬停后确实换了 / 离开换回来了」）。</summary>
        public Texture CurrentTexForTest { get { return target != null ? target.Texture : null; } }
        /// <summary>自检用：绑定的常态图 / 高亮图 / 按下图（没接换图的三者都是 null）。</summary>
        public Texture NormalTexForTest { get { return _normalTex; } }
        public Texture HoverTexForTest { get { return _hoverTex; } }
        public Texture PressedTexForTest { get { return _pressedTex; } }

        // 🔴 **没有「登记表」**（2026-09-23 撤掉）：原来想用 `OnEnable/OnDisable` 维护一张静态表让指针层扫，
        //    但**自检跑在编辑模式**，而编辑模式下这两个回调**只对 `[ExecuteAlways]` 的脚本**才跑
        //    ⇒ 自检里表恒为空（实测「场景里 13 个 `WindowButton`、登记表 0 个」）。
        //    现在 `PointerLayer` **在真有输入事件时**才 `FindObjectsByType<WindowButton>()` 扫一遍
        //    （事件很少，代价可忽略），**不依赖任何生命周期回调**。

        // ============================================================ 🆕 2026-10-05 A65②：`disabled / soft-disable` 的【变灰】观感
        //
        // 🔴 **原版两条路落到同一个东西上 —— 把图形件的材质换成 `Everguild/UI/Greyscale`**：
        //   · `EverguildButton.SoftDisable(true)`（`DF:EverguildButton__SoftDisable.c`）：
        //     `softDisabled = true` → `SetToStateActiveOrDisabled(1, …)` → `SwitchMaterial` →
        //     `EverguildButtonHelper.DoMaterialRefresh(list, /*grey=*/false)` → 每颗
        //     `EverguildButtonMaterialModifier.ToogleGreyScale = true` → `GetModifiedMaterial` 返回
        //     `EverguildButtonHelper.get_DisabledMaterial()`（= **一份静态缓存**的 `new Material(Shader.Find(<Greyscale>))`）。
        //     ⚠️ **它【不】改 `interactable`** ⇒ 钮只是**变灰，照样点得动**（这就是 A65② 的题目）。
        //   · `Selectable.interactable = false`（`DoStateTransition(4 /*Disabled*/)`）走的是**同一条**：
        //     `DF:EverguildButton__DoStateTransition.c:93-104` 算出 `bVar4 = state != 4 && !softDisabled`
        //     再调 `SetToStateActiveOrDisabled(param_1, bVar4)` ⇒ 同一次 `DoMaterialRefresh`。
        //   · 🔴 **两条都还要一个前置**：`EverguildButton.colorTintGreyOnDisable`（字段 `0x17A`）为真才会真的灰
        //     （`SoftDisable` / `SwitchMaterial` 的第一句都是 `if (*(char *)(param_1 + 0x17a) != '\0')`）。
        //     **本工程用到变灰的那两颗，真包实读全是 1**：`Edit Deck`（`bundle_menus_assets_all` GO `Edit Deck` 上的
        //     EverguildButton MB `-8697463422759302744`，`grey=1 trans=2 custom=0`）· 12 颗 `Collect`
        //     （如每日行 MB `-4755074315463813377`，`grey=1 trans=2 custom=0`）。
        //
        // 🔴 **灰成什么样（逐像素）**：sprite 属性表 = `_MainTex` / `_Color` / `_GreyScale`（默认 **1.0**），
        //   Blend = `One / OneMinusSrcAlpha`、zWrite = 0（`OriginalShaderBlendTable` 也收了这一条：
        //   `{ "Everguild/UI/Greyscale", new[] { 1, 10, 0 } }`）；算式（DXBC 反汇编）
        //   `out.rgb = lerp(col, dot(col, (0.30,0.59,0.11)), _GreyScale)`，原版**一个属性都没写** ⇒ 纯亮度灰。
        //   ⚠️ **白色灰化后还是白的**：`dot((1,1,1),(0.30,0.59,0.11)) == 1.0`。
        //   ⇒ 我们**只换 `ImageQuad`**、**不动 `Label`**：我们这些钮的 `Button Text` 全是 `Color.white`，
        //     换与不换是同一个像素。**这一条是「照算式等价的省略」，不是漏做** —— 若将来某颗钮的文案不是白的，
        //     这里就得补（`Label.SetColor` 到 `grey(c)` 即可，仍是同一算式）。
        //
        // ⚠️ **一处如实标注的偏离（被迫）**：原版是**一份**静态 `disabledMaterial` 全按钮共用
        //   （uGUI 的贴图与颜色由 `CanvasRenderer` 逐件给，材质只管 shader 与混合）；
        //   我们的贴图/颜色**存在材质上**（`ImageQuad.SetTexture` 写 `sharedMaterial.mainTexture`、
        //   `SetTint` 写 `sharedMaterial.color`）⇒ **一份共享材质装不下多张贴图** ⇒ **每个 quad 一份**。
        //   shader、`_GreyScale`（不写 = 用默认 1.0）、混合状态**与原版逐项相同**，差的只是「几份材质对象」。

        /// <summary>原版那张灰化 shader 的**内部名**（出处：`EverguildButtonStateFollower` 的
        /// `private const string EVERGUILD_UI_GREYSCALE = "Everguild/UI/Greyscale"`；真包实读同一个串）。
        /// ⛔ **不许用 `Shader.Find`** —— 它在**包起来的构建里**找不到这张 shader（本工程的既定教训），
        /// 要走随包的 `wf_shaders.bundle`（`tools/gen_shader_blend.py` 就是从那个包里读出它的混合状态的）。</summary>
        public const string GrayShaderName = "Everguild/UI/Greyscale";

        /// <summary>**取不到 shader 的按钮名**（红线：不许静默失败）—— 每扇窗的自检断它为空。
        /// 取不到时**保持原样（不变灰）**，⛔ **不拿别的灰顶替**（铁律 3：那就变成「我们挑的观感」冒充原版）。</summary>
        public static readonly System.Collections.Generic.List<string> MissingGrayArt =
            new System.Collections.Generic.List<string>();

        static bool _saidNoGrayShader;

        /// <summary>`Everguild/UI/Greyscale` 这张原版 shader 现在**取不取得到**（自检用）。
        /// 取不到 ⇒ 所有变灰都会**静默失效**（只剩一条警告），所以这一条值得单独断。</summary>
        public static bool GrayShaderAvailable
        {
            get
            {
                UnityEngine.Shader sh;
                return WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(GrayShaderName, out sh) && sh != null;
            }
        }

        /// <summary>原版 `EverguildButton.softDisabled`（`0x198`）：**只变灰，照样点得动**。</summary>
        public bool SoftDisabled { get; private set; }

        bool _interactable = true;
        /// <summary>原版 `Selectable.interactable` 的最小等价物。置假 ⇒ ① 变灰（同 `EverguildButton`
        /// 在 `DoStateTransition(Disabled)` 里做的那一下）② **`Click()` 直接返回**（原版
        /// `Selectable.OnPointerClick` 头一句 `if (!IsActive() || !IsInteractable()) return;`）。
        /// ⚠️ **悬停那半边本地没有判据**（`Selectable.OnPointerEnter` 在 UGUI 包里，不在 `d:/2/tools/decomp_full/`）
        /// ⇒ 我们**保持现状**（不可交互的钮**照旧**吃悬停换图），**如实标注、不编**。</summary>
        public bool Interactable
        {
            get { return _interactable; }
            set { _interactable = value; RefreshGray(); }
        }

        /// <summary>原版 `EverguildButton.SoftDisable(bool)`。</summary>
        public void SetSoftDisabled(bool on) { SoftDisabled = on; RefreshGray(); }

        /// <summary>该灰了吗（原版那两条路在 `DoStateTransition` 里合成的那一个布尔：
        /// `bVar4 = state != 4 &amp;&amp; !softDisabled`，取反即「灰」）。</summary>
        bool WantGray { get { return SoftDisabled || !_interactable; } }

        /// <summary>自检用：现在**真的**灰着吗（不是「想灰」—— 取不到 shader 时这里仍是 `false`）。</summary>
        public bool GrayedForTest { get; private set; }
        /// <summary>自检用：这一颗要一起变灰的 quad 数（**四组去重**：`_nine` + `target` + 本钮子树 + `TintOn` 那几颗）。</summary>
        public int GrayQuadCountForTest { get { return GrayTargets().Length; } }

        ImageQuad[] _graySavedQ;    // 上次变灰时的那一组（顺序与 `_graySaved` / `_grayMats` 对齐）
        Material[] _graySaved;      // 换灰之前每颗 quad 的材质（退出时**原样还回去**）
        Material[] _grayMats;       // 我们为每颗 quad 建的那一份

        /// <summary>要一起变灰的图形件。原版是 `GetComponentsInChildren&lt;Graphic&gt;(gameObject, includeInactive: 1)`
        /// （`DF:EverguildButtonHelper__GetGraphicsInChildren.c:33` 实读 `(gameObject, 1, …)`）——
        /// 我们这棵树里能画的东西只有 `ImageQuad`，**加上 `target`**：原版那颗 `Selectable.m_TargetGraphic`
        /// 本来就是按钮自己的图形件，在我们这里它常常是**兄弟**（`DeckInfoPopup.Hit` 那种「另建一个透明命中区、
        /// 真图在隔壁」的摆法）⇒ 只按子树灰会**一颗像素都不变**。⚠️ 取不到的件静默跳过（`Label` 见上面那段说明）。
        /// 🔴 **不缓存**（每次现算）：`Bind` 可能在挂上 `WindowButton` 之后再改 `target`，
        ///    缓存下来就会「按旧的一组灰、按旧的一组还原」（静默错一组），而这里总共只在状态切换时被调到。
        ///
        /// <para>🔴🆕 **2026-10-13（A1719）：第三组 = `_nine`（九宫格那 9 块）。** 改前这里只收
        /// 「`target` + 本钮子树」两组 —— 而**九宫格按钮的可见底图是【兄弟】**：真实站点
        /// （`Shell/GenericOptionsPanel.cs` 的 `bgo = MenuDraw.Nine(bn, …)` 与 `Hit(bn, "Hit", …)`；
        /// `Shell/AllianceMemberOptionsPopup.cs` 同形：`bgo` 与 `Hit` 都是 `bn` 下的**兄弟**）里，
        /// `WindowButton` 挂在**透明命中区**那个 `Hit` 节点上、9 块 quad 全在**隔壁** `bgo` 底下
        /// ⇒ `GrayTargets()` 只看得到 `target` 那一块（= `qs[0]`）
        /// ⇒ **九宫格钮一置灰，只灰九分之一，看着像没禁用**（正是 A1719 那条账）。
        /// 而**换图那一路（`SwapTo`）认的是 `_nine` 全部 9 块** ⇒ 灰/亮两条路在同一颗钮上认的不是同一组件。
        /// 判据仍是上面那条原版语义（`GetGraphicsInChildren(gameObject, 1)` = **按钮这一组图形件全部**）：
        /// 九宫格在原版是**一颗** `Image`，在我们这儿被切成 9 张 ⇒ 9 块同属一组，必须一起灰、一起亮。</para>
        ///
        /// <para>🔴🆕 **2026-10-13（A1725）：第四组 = `_tqExtra`（`TintOn` 指定的那几颗）。**
        /// 同一条判据（原版 `GetGraphicsInChildren(gameObject, 1)` = 按钮这一组图形件**全部**）——
        /// 而 `TintOn` 的语义正是原版 `m_TargetGraphic`：那颗**可见件常常挂在兄弟节点下**、
        /// 不在本钮（透明命中区）子树里（真实站点 → `TintOn` 的注释）。
        /// **改前「变色认得它、变灰不认」**：色偏走 `Collect()`（**含** `_tqExtra`），这里却**不收**
        /// ⇒ 那一族置灰时**纹丝不动**（一个像素都不变）。这与 **A1719**（九宫格只灰 1/9）
        /// 是**同一族**的两半 —— 「灰 / 亮两条路在同一颗钮上认的不是同一组图形件」。详见 `TintOn`。</para>
        ///
        /// ⚠️ **今天零影响**：全仓 `TintOn` **只有两处**（`Shell/MainMenuRuntime.cs` 的 5 个导航钮与
        /// 3 张模式卡），而**那两处都没有灰站点**（没有 `Interactable = false` / `SetSoftDisabled`）
        /// ⇒ 这是**潜伏**缺口、不是正在发作的缺陷 —— 所以由 `Editor/CollectionScene.cs` 的
        /// A1725 探针（自造一棵「可见件在兄弟、`TintOn` 接上」的小树）钉住它。</summary>
        ImageQuad[] GrayTargets()
        {
            var kids = GetComponentsInChildren<ImageQuad>(true);
            int ex = _tqExtra != null ? _tqExtra.Length : 0;
            int cap = kids.Length + 1 + (_nine != null ? _nine.Length : 0) + ex;
            var list = new System.Collections.Generic.List<ImageQuad>(cap);
            // 🔴 **去重**：`BindNine` 把 `target` 设成 `_nine[0]`（两边是**同一块**）⇒ 不去重就把同一块收两遍，
            //    而 `RefreshGray` 的 `_graySaved` / `_grayMats` 是**按下标**存取的 ⇒ 同一块进两组、
            //    还原时按错的下标还（静默错一组）。`TintOn` 传进来的**也可能就是 `target` / 某块 `_nine`**
            //    （`MainMenuRuntime` 那两处传的是别的节点，但**本类不假设这一点**）⇒ 第四组同样过这个口。
            void Add(ImageQuad q)
            {
                if (q == null) return;
                for (int j = 0; j < list.Count; j++) if (list[j] == q) return;
                list.Add(q);
            }
            if (_nine != null) for (int i = 0; i < _nine.Length; i++) Add(_nine[i]);   // 🆕 A1719：九宫格那 9 块
            Add(target);
            for (int i = 0; i < kids.Length; i++) Add(kids[i]);
            for (int i = 0; i < ex; i++) Add(_tqExtra[i]);                            // 🆕 A1725：`TintOn` 那几颗
            return list.ToArray();
        }

        /// <summary>按 `WantGray` 摆材质。**只有这一份实现** —— `SoftDisabled` 与 `Interactable` 都走它。</summary>
        void RefreshGray()
        {
            bool want = WantGray;
            if (want == GrayedForTest) return;          // 没变就别重做（材质对象很贵）
            var qs = GrayTargets();
            if (want)
            {
                // 🔴 一组变了（长度不等）就重做那两张表 —— 只认 `_grayMats == null` 会在
                //    「先灰过、`Bind` 之后又灰」时**按下标越界**（自检里就是一条难查的 NRE）。
                if (_grayMats == null || _grayMats.Length != qs.Length)
                {
                    UnityEngine.Shader sh;
                    if (!WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(GrayShaderName, out sh) || sh == null)
                    {
                        // 红线：**不许静默失败**，也**不拿别的灰顶替**（那样就成了「我们挑的观感」冒充原版）。
                        if (!_saidNoGrayShader)
                        {
                            _saidNoGrayShader = true;
                            Debug.LogWarning("[Button] 取不到原版灰化 shader `" + GrayShaderName + "`"
                                             + "（随包 shader bundle 在不在？见 `资料/特效还原_进度与交接.md` §三）"
                                             + " ⇒ **这颗钮不变灰**（⛔ 不拿别的灰顶替 —— 铁律 3）。"
                                             + "`SoftDisable` / `interactable=false` 的**行为**照常生效。");
                        }
                        if (!MissingGrayArt.Contains(name)) MissingGrayArt.Add(name);
                        return;
                    }
                    _graySaved = new Material[qs.Length];
                    _grayMats = new Material[qs.Length];
                    for (int i = 0; i < qs.Length; i++)
                    {
                        if (qs[i] == null) continue;
                        var mr = qs[i].GetComponent<MeshRenderer>();
                        _graySaved[i] = mr != null ? mr.sharedMaterial : null;
                        // 原版那一份**一个属性都没写** ⇒ 我们也只带过去贴图与颜色（shader 自带 `_GreyScale = 1.0`）。
                        var m = new Material(sh);
                        m.name = "UI Greyscale (" + qs[i].name + ")";
                        m.color = qs[i].Tint;
                        _grayMats[i] = m;
                    }
                }
                for (int i = 0; i < qs.Length; i++)
                {
                    if (qs[i] == null || _grayMats[i] == null) continue;
                    // 🔴 每次应用前把颜色对齐到这一颗**当下**的 tint（`SetMaterial` 只带贴图、不带颜色）；
                    //    否则「同一颗按钮先灰过、换了一批 quad 又灰」会把上一颗的颜色带过来（静默偏色）。
                    _grayMats[i].color = qs[i].Tint;
                    qs[i].SetMaterial(_grayMats[i]);     // 它会把 quad 当前的贴图带过去
                }
                _graySavedQ = qs;                        // 🔴 记下**这一组**，退出时按同一组还回去
                GrayedForTest = true;
            }
            else
            {
                if (_graySaved == null) return;
                var back = _graySavedQ ?? qs;            // 按变灰那一刻那一组还原（不是按现在这一组）
                for (int i = 0; i < back.Length && i < _graySaved.Length; i++)
                {
                    if (back[i] == null || _graySaved[i] == null) continue;
                    back[i].SetMaterial(_graySaved[i]);
                }
                GrayedForTest = false;
            }
            Debug.Log("[Button] `" + name + "` " + (GrayedForTest ? "**变灰**" : "**还原**")
                      + "（原版 `EverguildButton.SoftDisable` / `interactable=false` 都落到"
                      + "「子树图形件的材质换成 `" + GrayShaderName + "`」这一下，见 `WindowButton` 头部那段）"
                      + "；`softDisabled = " + SoftDisabled + "` · `interactable = " + _interactable + "`");
        }

        /// <summary>自检用：把一棵树里**每一颗说自己是灰的**按钮逐个核一遍 —— 返回空串 = 全过，`n` = 查了几颗。
        /// 🔴 核的是**原版那张 shader 的名字**（`Everguild/UI/Greyscale`，出处见类头部），**不是我们自己的常量**
        /// ⇒ 不是自证；`GrayedForTest == true` 而材质没换 ⇒ 报红。
        /// ⚠️ 只查「自称灰了」的那些 —— 「该灰的没灰」（取不到 shader 那条路）由 `MissingGrayArt` 兜。</summary>
        public static string AuditGrayLook(Transform root, out int n)
        {
            n = 0;
            if (root == null) return "";
            var bad = new System.Text.StringBuilder();
            foreach (var wb in root.GetComponentsInChildren<WindowButton>(true))
            {
                if (wb == null || !wb.GrayedForTest) continue;
                n++;
                var qs = wb.GrayTargets();
                for (int i = 0; i < qs.Length; i++)
                {
                    if (qs[i] == null) continue;
                    var mr = qs[i].GetComponent<MeshRenderer>();
                    var m = mr != null ? mr.sharedMaterial : null;
                    if (m == null || m.shader == null || m.shader.name != GrayShaderName)
                    {
                        bad.Append("「").Append(wb.name).Append("」的 `").Append(qs[i].name)
                           .Append("` 自称灰了，材质却是 `")
                           .Append(m != null && m.shader != null ? m.shader.name : "<没有材质>").Append("`；");
                        break;
                    }
                }
            }
            return bad.ToString();
        }

        /// <summary>点一下 —— **`PointerLayer` 唯一的派发口**。
        /// 🔴 `interactable == false` 时**直接返回**（原版 `Selectable.OnPointerClick` 的第一句），
        /// 并且**出声**：原版那一刻玩家看得见「钮是灰的」，我们若连日志都不打，就成了静默失败。</summary>
        public void Click()
        {
            // 🔴 **吸收层（`MenuDraw.Absorb` 建的）在这里就结束**：这一下**被吃掉、什么都不做**
            //    （原版：面板那颗 `Image` 是射线落点、父链上没有点击处理器）。
            //    ⚠️ 必须挡在下面那条 `_interactable` 告警**之前** —— 否则每次点面板都误报一次。
            if (absorbOnly) return;
            if (!_interactable)
            {
                Debug.LogWarning("[Button] `" + name + "` **点了不生效** —— 原版这颗钮 `interactable = false`"
                                 + "（`Selectable.OnPointerClick` 头一句就返回）⇒ 连派发都没有。"
                                 + "⚠️ 悬停那半边我们**没有判据**（见 `Interactable` 的注释）⇒ 照旧吃悬停。");
                return;
            }
            // 🆕 2026-10-18（`AUD` §三 C9）：原版 `EverguildToggle.OnPointerClick` **第一段** ——
            //   `allowClickWhenAlreadySelected == 0 && isOn` ⇒ 连按钮音一起跳过（见字段那段注释）。
            //   🔴 必须排在 `PlayClickSound()` **之前**（改前先响后 return 的那个坑就是这条要修的）。
            if (!allowClickWhenAlreadySelected && isOn != null && isOn()) return;
            PlayClickSound();
            if (onClick != null) onClick();
        }

        // ============================================================ 🆕 2026-10-17（音效线 W_G）：按钮音
        //
        // 判据（逐条 → `资料/普查产出_1017/UI/W_G_AudioLine.md`）：
        //   · 原版真正播「默认按钮音」的消费者是 `EverguildButton.PlayButtonSound`
        //     （`EverguildButton__PlayButtonSound.c`：`m_onClickSound` 有 clip 就播它；为空则
        //     `useDefaultAudioClipIfNull`(0x178) 为真时播 `SoundManager.defaultButtonSound`）与
        //     **同形的** `EverguildToggle.PlayButtonSound`（`EverguildToggle.cs` 字段逐字相同）；
        //     触发点 = 各自的 `OnPointerClick`。
        //   · 普查（`bundle_menus_assets_all` + `generalgamewindows_assets_all`，2026-10-17 现读）：
        //     `EverguildButton` **1022/1022** 颗 `useDefaultAudioClipIfNull = 1`（564 颗自身 cue 为空 ⇒ 走默认）；
        //     `EverguildToggle` **151/151** 颗为 1（18 颗走默认）。
        //     ⇒ **默认音是绝大多数钮的实际行为**，而我们的 `WindowButton` 正是这一族的替身
        //       （`EverguildButton` 与 `EverguildToggle` 在我们这边**都是** `WindowButton`）。
        //   · ⛔ **不是所有命中区都该响**：原版 `UIGenericEventCatcher`（开包窗的卡、`Tap to close`
        //     整屏层）与 `BackgroundCloseButton`（压暗层，88/88 颗）是**纯 MonoBehaviour，没有声音**。
        //     ⇒ 我们把这几种显式关掉（`playClickSound = false`）：压暗层在这里按 `MenuDraw.WasShadeHit`
        //       判（`ShadeHit` 是公共件、不在本件可改范围），开包窗的卡在那边自己关。
        //   · 🔴 **2026-10-17 就地订正（铁律 5 · 音效线 S01 写手 G）**：原版那个 `m_onClickSound`
        //     （每颗钮可以带自己的 cue，`menus` 包 424 颗有）原来这里写「**我们没接**」—— **现在已经接上了**：
        //     `WindowButton.clickCue` ← `MenuDraw.Hit(..., clickCue)` ← `GameWindow.AddHit(..., clickCue)`；
        //     逐颗配对（73 条）= `资料/普查产出_1017/UI/R3_S01按钮cue映射.md` §四·A。
        //     装配规则照原版 `EverguildButton.PlayButtonSound`：**`m_onClickSound` 有 → 播它；为空 → 默认音**。
        /// <summary>点中时要不要播 `SoundManager.defaultButtonSound`（默认**要**，照 1022/1022 那两族）。
        /// 压暗层 / 开包窗的卡 / 任何原版不是 `EverguildButton|EverguildToggle` 的命中面**置 false**。
        /// 🔴 `playClickSound = false` **优先于** `clickCue`（那一族原版根本没有按钮音）。</summary>
        public bool playClickSound = true;

        /// <summary>这颗钮**自己的**点击 cue（原版 `EverguildButton|EverguildToggle.m_onClickSound` 的 cue 名）。
        /// `null`（默认）= 走 `SoundManager.defaultButtonSound`（`WindowSounds.DefaultButtonCue`）。
        /// <para>赋值口 = `MenuDraw.Hit(..., clickCue)` / `GameWindow.AddHit(..., clickCue)` 逐层透传；
        /// 手工建钮那几处（`AddComponent&lt;WindowButton&gt;()`）直接赋值。
        /// 逐颗配对表 → `资料/普查产出_1017/UI/R3_S01按钮cue映射.md` §四·A（73 条）。</para></summary>
        public string clickCue = null;

        void PlayClickSound()
        {
            if (!playClickSound || absorbOnly) return;
            // 压暗层（原版 `BackgroundCloseButton`）**没有**按钮音 —— 它只负责触发窗自己的 `Close()`
            // （那一声由 `GameWindow.PlayCloseSound` 出）。判据见上面那段普查。
            if (MenuDraw.WasShadeHit(transform)) return;
            WindowSounds.PlayFx(clickCue ?? WindowSounds.DefaultButtonCue, $"按钮 `{name}`");
        }

        // ============================================================ 🆕 2026-10-18（音效线缺口 `AUD` §三 C9）
        //
        // 原版 `EverguildToggle__OnPointerClick.c` **第一段**（在 `base.OnPointerClick` 之前）：
        //     `if (*(char *)(this + 0x139) == 0 && *(char *)(this + 0x120) != 0) return;`
        //   = **`allowClickWhenAlreadySelected == 0 && isOn`** ⇒ **整下点击被吃掉**
        //     （`Toggle.OnPointerClick` 不派发、`onValueChanged` 不发、按钮音也不响）。
        // 全量 = **64 颗**（30 个 prefab 根，判据 → `资料/普查产出_1018/AUD_音效线_完整性复核.md` §三 C9）。
        // 🔴 我们的 `Click()` 原来**先播音、再执行 onClick**（见下面那一句次序）⇒ 即使 onClick 里自己
        //   写了「同一键再点就 return」（例如 `AchievementsMenu.Select`），**声音已经响过了**。
        // ⇒ 本段把原版那条守卫搬进 `Click()`，位置在 `interactable` 判定之后、`PlayClickSound` **之前**。

        /// <summary>原版 `EverguildToggle.allowClickWhenAlreadySelected`（字段偏移 `0x139`；**出厂 = 0**，
        /// 即「已选中时再点 = 什么都不做」）。我们这一边**缺省 true**（= 旧的「每次都派发」行为），
        /// 只有**逐站点**确认过「这一颗原版是 `allowClickWhenAlreadySelected = 0`」才置 false
        /// （判据 = prefab MB 逐颗实读，见 `isOn` 的注释与 `AUD` §三 C9）。
        /// <para>⚠️ 它**只**对 toggle/radio 类站点有意义 —— 普通按钮没有「已选中」态（`isOn` 恒 null）。</para></summary>
        public bool allowClickWhenAlreadySelected = true;

        /// <summary>「这一颗**此刻**是选中态吗」—— 原版 `EverguildToggle.isOn`（`0x120`）。
        /// 由**建钮的站点**给（同一个窗的 tab / radio 选择器共用一份状态；闭包读那个状态字段，
        /// 所以换页/换选项之后**自动跟着变**，⛔ 不需要重建钮）。
        /// <para>`null`（缺省）= 没有「已选中」概念 ⇒ <see cref="allowClickWhenAlreadySelected"/> 不生效。</para>
        /// <para>守卫语义照原版：`allowClickWhenAlreadySelected == false && isOn() == true` ⇒
        /// `Click()` 在**播音之前**整个返回（连 `onClick` 一起跳过）。</para></summary>
        public System.Func<bool> isOn;

        /// <summary>🔴 **老式的 `OnMouseUpAsButton`：在这个工程里一次也不会派发** ——
        /// ① 它要求同一物体上有 `Collider`，而本工程**零处**加过 collider；
        /// ② `ProjectSettings.asset:932 activeInputHandler = 1`（只用新 Input System）⇒ 老式 `OnMouseXxx` 不派发。
        /// 见 `项目任务.md` §三 第 15 条 **第 23 条**。**留着只为「原版是 UGUI 按钮」这条线索可查，
        /// 别再往它上面挂新逻辑**（新逻辑挂 `onClick`，由 `PointerLayer` 派发）。</summary>
        void OnMouseUpAsButton() { Click(); }

        /// <summary>自检用：批处理里没有输入事件，直调这条路（**和真点走同一个 `Click()`**）。</summary>
        public void ClickForTest() { Click(); }
    }
}