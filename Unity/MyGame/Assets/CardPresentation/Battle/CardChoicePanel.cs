// CardChoicePanel.cs — 原版那套「横向选卡面板」的**共用壳**（选牌 / 选效果 / 换牌）
//
// **为什么有它**：原版这两个面板（`ChooseCardMenu` 与 `Mulligan`）是**两份克隆** ——
// 两棵节点树**逐节点同构**：`HideChooseButton` / `HideMulliganButton` 同图 `40k_UI_bt_eye`；
// 两个文字框的 pos/size/pivot **完全相同**（`7.0,-106.5` / `1344.0,79.4` / `0.5,1.0`）；
// 连每张牌下面那颗按钮用的都是**同一张图** `UI_Button_Mulligan`（只有文案不同：`Select` / `换`）。
// 出处：`资料/选牌_数据与规格.md` §甲·四之二（那张表逐条带证据；⚠️ 更正：原来指 `资料/选牌Choose_数据与设计.md`，2026-10-10 已并入）。
// ⇒ **我们合成一份**：**只有 `ChoosePanel` 从本类派生**，**别写第三份克隆**。
//   🔴 **2026-10-16 订正（铁律 5）**：本行原来写「`ChoosePanel` **与** `MulliganPanel` 都从这个类派生」——
//   **不成立**。实据 = `grep -rn ": CardChoicePanel" MyGame/Assets` **只命中 `ChoosePanel.cs:58` 一处**，
//   而 `MulliganPanel.cs:77` 声明的是 `: MonoBehaviour`。
//   ⚠️ **那份「两棵树同构」的判据仍然成立**（所以「别写第三份克隆」这条也仍然成立）——
//   变的只是**落地形态**：换牌那扇自己实现了，没走这层壳。⇒ 迁移/复用前先现读一次继承链，别照本行。
//
// **数值出处**：
//   · 选牌 / 选效果：`资料/选牌_数据与规格.md` §丙·三 + 实况报告（⚠️ 更正：原来指 `资料/选牌与选效果面板_原版数值.md`，2026-10-10 已并入）
//     `资料/原版参照图/Unity参照管线_0825/data/panel_0914b/报告.md` §③（**运行时真值**）。
//   · 换牌：`data/runtime_ui_dump_drive_0912.tsv:437-450`。
// ⚠️ **实况与静态字段冲突时以实况为准**（铁律 4）。
//
// **卡行布局**照原版**自己的算法**（探针直接问 `CardsHorizontalLayout.GetPosition(i,n)` 问出来的，
// 不是我们推算的）—— 见 `SpacingPx` 与报告 §③那张表。
//
// **哪些是「我们挑的」**（文件里每处都单独标了）：
//   · 压暗层（原版有 `Shade.SwitchShade`，**颜色/透明度没查到**）
//   · 卡行的**纵向位置**（原版那个 anchor 节点**只有 Transform、没有 RectTransform** ⇒ 无据可查）
//   · 卡上按钮的**纵向偏移**（原版那个 prefab 是运行时实例化的，静态 dump 里没有）
//
// ─────────────────────────────────────────────────────────────────────────────
// 🔴 **2026-10-16（`T09` 批 3 · `OverHand` 组）：本件已迁到 uGUI 子画布**
//    配方与全部口径 → `资料/任务线/战斗线/任务/T09_整层换uGUI.md` **§五·补⑨ / §五·补⑩**；
//    映射那一行 → `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一（`ChoosePanel` = 原版
//    `ChooseCardMenu` @ `Safe area FrontCanvas` · **`OverHand`/order 0** · 兄弟序 5/14）。
//    ⚠️ **本类自身原版【没有对应对象】**（那一行写着「原版无此对象，是我们合成的共用壳」）⇒
//    子画布的层/order 只能照**它服务的那个原版对象**（`ChooseCardMenu`）取，**不是**另立一格。
//   · **子画布** = `HudCanvas.EnsureSubCanvas(null, "ChooseCardMenu Canvas",
//     HudCanvas.SortingLayer.OverHand, 0)` —— 原版 `ChooseCardMenu` 自身**没有 `Canvas` 组件**、
//     随 `FrontCanvas`（`OverHand`/0）渲染 ⇒ 我们给它一块**同层同序 + `overrideSorting`** 的子画布
//     （继承根 canvas 那个唯一的 `CanvasScaler`，⛔ 子画布不加 `CanvasScaler`）。
//   · 🔴 **本件因此分成两个节点**（同 `SettingsPanel` / `MultiCardDisplay` 那件「**世界壳 + 画布本体**」）：
//       ① **世界壳** = 派生类 `Create` 建的那个节点（**仍是**「世界空间 1920×1080」）——
//          装**候选卡**（`CardView` 由 `BattleDriver` 建在 `_choosePanel.transform` 下：
//          `BattleDriver.cs:5032/5076/6370/6392/6434`）与**那层整屏压暗**。
//          卡是**世界物件**（`T09` §五·补⑧「手牌与场上卡」还没迁），`SetPose` 收的是**世界坐标**
//          ⇒ 一搬上画布（局部单位变**设计 px**）那几张卡会**贴着画布中心叠成一堆**（差 108 倍、
//          **不报错**）。它**不是死件**：`Editor/BattleScene.cs` §A218 量「选牌面板根 = 整屏矩形」
//          量的就是它（`box(...)` 的**非画布那一支**）。
//       ② **画布本体** = `ChooseCardMenu`（挂在子画布下、1920×1080 **设计 px**、摆设计中心 (960,540)）
//          —— 装这一整组「图与字」：**标题 / 底条 / 圆钮 / 确认两个字 / 眼睛钮 / 每张卡下那颗钮与它的字**。
//   · 🔴🔴 **本文件里唯一【有意留在世界空间】的图 = `ChoiceShade`（整屏压暗）** —— ⛔ 别顺手把它迁上去：
//     ① 它必须画在**候选卡【后面】**（原版 `Shade.SwitchShade` 就是「面板底下的压暗」；
//        挪到卡片前面 ⇒ 那几张要挑的卡被压到 **45% 亮度**）；
//     ② **候选卡是 `MeshRenderer`（`CardView`）**、不归画布管 ⇒ 落在 `Default` 排序层/0 序，
//        而子画布是 `OverHand`（更高）⇒ **画布上的件一律画在它【上面】**
//        （判据 → `T09` §五·补⑤ ⑤「给了 `sortingOrder` ⇒ 那一扇**盖住所有还没迁的件**」）；
//     ③ ⇒ 卡片迁上画布之前，「必须压在卡片后面」的整屏件不能上画布。
//     ⚠️ 这是**临时边界**（随第 6 步「手牌 / 场上卡」那批一起消失）；`MultiCardDisplay.cs` 的
//     `mcd_mask` 是**同一条**（两处注释互相指），已写进 T09 批 3 的报告。
//     ⚠️ **`_shade` 的字段类型保持 `ImageQuad`**（有意：它**没迁**，类型上就不该装成 `HudPiece`）。
//   · ⚠️ **代价如实记**：卡仍走世界单位、画布那一组走设计 px ⇒ **非 16:9 下两者会分家**
//     （卡下那颗钮会离开它的卡）。这条与上面那条是**同一件事的两个面**，一起消失。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 「横向选卡面板」的共用壳。**只管画 + 命中 + 可见性**，选几张、选完做什么由派生类决定。
    /// </summary>
    public class CardChoicePanel : MonoBehaviour
    {
        /// <summary>挑几张：换牌是**多选**（可弃回任意张），选牌/选效果是**单选**。</summary>
        public enum PickMode { Multi, Single }

        /// <summary>
        /// 两个面板不同的那几处数值。**全部是 1920×1080 下的像素**、`y` **从上面数**。
        /// 只把两边**实测不同**的放进来；相同的（标题框、底条尺寸、圆钮尺寸、图名）写成常量。
        /// </summary>
        public struct Layout
        {
            public float TitleCx, TitleCy, TitleW, TitleH;
            /// <summary>标题字号（px）。⚠️ 实况：选牌面板 `ChooseText/Text` = **55**。</summary>
            public float TitlePx;

            public float BarCx, BarCy;                  // 底条中心（图 `40k_bt_underbutton` 577.5×63.84）
            public float PlayCx, PlayCy;                // 圆形播放钮中心（图 `40k_UI_bt_play` 80.47×79.64）
            public float EyeCx, EyeCy;                  // 眼睛中心（图 `40k_UI_bt_eye`）
            public float ConfirmCx, ConfirmCy;          // 确认文字中心

            /// <summary>卡行中心（**从上面数**）。⚠️ **这是我们挑的** —— 原版那个 anchor 节点
            /// **只有 Transform、没有 RectTransform**，卡的位置是运行时算的，**无据可查**。</summary>
            public float CardRowCy;

            public string CardBtnText;                  // 卡上那颗按钮的字（`换` / `Select`）
        }

        /// <summary>面板开着吗。开着时**吃掉点击**，别让底下的棋盘/手牌也响应。</summary>
        public bool Visible { get; private set; }

        /// <summary>点确认 → 回调「选中的下标」（升序；可能为空 = 没选）。</summary>
        public Action<List<int>> OnDone;

        public PickMode Mode { get; private set; }

        // ---- 共用常量（两个面板实测**相同**的那几处）----
        /// <summary>底条尺寸（`40k_bt_underbutton`）—— 原版 `BG` `sd=577.50×63.84`</summary>
        const float BarW = 577.5f, BarH = 63.84f;
        /// <summary>圆钮尺寸（`40k_UI_bt_play`）—— 原版 `ContinueButton` `sd=80.47×79.64`。
        /// ⚠️ 我们**只给高**（宽由贴图比例来，见 `ImgByHeight`）⇒ 实绘是 **79.64 正方**，
        /// 比原版那个 **80.47** 窄 **0.83 px（1%）** —— 这是**旧的既有偏离**（不是本批引入的），
        /// 如实记着（判据同 `MultiCardDisplay.cs` 里那条 1% 的说明；改它要动实绘尺寸、另开一件）。
        /// 🔴 **2026-10-19（`T03`·`A1327`）**：**命中**这一侧按原版 `RectTransform` 的 **80.469** 算
        /// （`PlayW`）—— uGUI 吃的是 rect 不是贴图；⛔ 别拿实绘的 79.64 去算命中（那是「画」那一侧）。</summary>
        const float PlayH = 79.64f;
        /// <summary>原版圆钮 **rect 宽** = `80.468689`（`RectTransform_3519`）。⛔ 与实绘 79.64 不是同一个数。</summary>
        const float PlayW = 80.469f;
        /// <summary>原版父 `Continue Button` 的 rect（`RectTransform_3391`）= **548.015 × 75.905**
        /// （那颗 `Image` 是 `alpha=0、sprite=null`，但 `m_RaycastTarget=1` ⇒ **吃射线**）。</summary>
        const float DoneParentW = 548.015f, DoneParentH = 75.905f;
        /// <summary>父 `Continue Button` 中心相对 **`BG` 中心**的偏移（设计 px）：
        /// 父 pos `(−396.4, 105.0)` / `BG` 相对父 `(65.19593, −5.74929)` ⇒ 父心 − BG 心 = `(−65.196, +5.749)`
        /// （Unity y 向上）；本工程口径是**左上原点 y 向下** ⇒ `dy = −5.749`。</summary>
        const float DoneParentDx = -65.196f, DoneParentDy = -5.749f;
        /// <summary>眼睛尺寸 —— 实测两边**略有不同**（78.44² vs 79.6），取 `Layout` 不给就按这个</summary>
        const float EyePx = 78.44f;

        /// <summary>补间/自检用：面板的 z。和日志面板一样推到 −0.9 一带 ——
        /// 场上的粒子在 −0.5 那层会飘到面板上面（那个坑记在 `BattleLogPanel` 的注释里）</summary>
        protected const float Z = -0.9f;

        /// <summary>🔴 **2026-10-16（`T09` 批 3）**：本扇**子画布**的名字（= 原版宿主 GO 名 + ` Canvas`）。
        /// ⚠️ 本类**只有 `ChoosePanel` 一个派生类**（2026-10-16 现核，见文件头）⇒ 常量够用；
        /// 真出现第二扇派生（原版那一扇是 `Mulligan`）时，把它改成 `protected virtual` 属性、
        /// 并由派生类各自给自己的画布名 —— ⛔ 别在两扇之间共用同一块子画布。</summary>
        protected const string SubCanvasName = "ChooseCardMenu Canvas";
        /// <summary>🔴 **2026-10-16（`T09` 批 3）**：**画布本体**那个节点的名字（**逐字 = 原版宿主 GO 名**）。</summary>
        protected const string PanelNodeName = "ChooseCardMenu";
        /// <summary>画布本体在**子画布**里的 z 档（它是子画布的**独子** ⇒ 只用于登记兄弟序）。</summary>
        const float ZBody = Z;

        protected Layout Lo;

        string _titleText = "", _confirmText = "";

        Transform _panel;                 // 画布本体（图与字都在它下面）
        ImageQuad _shade;                 // 🔴 有意**留在世界空间**（必须压在卡片后面，见文件头）
        HudPiece _bar, _play, _eye;
        Label _title, _confirm;
        readonly List<HudPiece> _cardBtns = new List<HudPiece>();

        // 🆕 2026-10-19（B4 · `N70` P0-2）：选牌/选择面板的按钮反馈（判据与换牌面板同族：
        // `Continue Button` t=1 ColorTint · `HideChooseButton` t=2 eye hover/pressed · 卡钮 t=2 Mulligan 图）。
        BattleButtonFx _fxBar, _fxEye;
        readonly List<BattleButtonFx> _fxCardBtns = new List<BattleButtonFx>();
        readonly List<Label> _cardTexts = new List<Label>();
        readonly List<CardView> _cards = new List<CardView>();
        readonly List<int> _picked = new List<int>();

        /// <summary>🆕 **2026-10-16（`T09` 批 3）**：本扇**画布本体**那个节点的 rect（自检用）。
        /// 判据 = **原版那一对锚点逐字复刻**：`ChooseCardMenu` 的 `anchor (0,0)-(1,1)` + `sizeDelta (0,0)`
        /// ⇒ 整屏 = `HudCanvas.FillParent` ⇒ `rect = **画布 rect**`（⛔ 不是写死的 1920×1080 ——
        /// `Expand` 档下 4:3 的画布是 1920×**1440**、21:9 会更宽；自检那一侧请用 `HudCanvas.RootRect.rect`
        /// 那一对数，同批 `EndPanel` / `BattleLogPanel` 那两条）。
        /// ⚠️ 拿不到（子画布没立起来）时返回 `null`。</summary>
        public RectTransform PanelRect { get { return _panel as RectTransform; } }

        // ==================================================================
        //  摆位换算
        // ==================================================================

        /// <summary>
        /// 像素 → **世界单位**（`LayoutSpace` 的约定是「可见高度恒为 10 世界单位」、1080p 下那 10 个单位
        /// 就是 **1080 px** ⇒ **1 px = 1/108 世界单位**）。
        /// ⚠️ **2026-09-14 踩到**：第一版写成 `px / (DesignHeight * 108)` = `/1080`，
        ///    于是**所有尺寸小了 10 倍** —— 4 张候选卡的间距（470 px）算出来只有 0.44 世界单位，
        ///    而卡自己宽 3.55 ⇒ **全部叠在屏幕中央**。**截图才看出来的**（断言全绿）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）—— 本函数现在【只剩卡片那一支】在用**
        /// （`U(spacingPx)` / `U(totalPx)` / `U(323f)` 三处，都是为了把 px 变成**卡片**要的世界单位）。
        /// 画布那一组（标题 / 底条 / 圆钮 / 眼睛 / 确认字 / 卡上那颗钮与字）**一律直接给设计 px**，
        /// ⛔ **别拿本函数去算它们的坐标/尺寸** —— 那会缩成 **1/108**，而画面只是「小得看不见」、
        /// **一个字都不报**（`T09` §五·补⑩ 那道硬关卡一）。</para></summary>
        protected static float U(float px) { return px * LayoutSpace.DesignHeight / 1080f; }

        /// <summary>(中心 x, 中心 y 从**上**数) → 世界坐标（**卡片那一支**用；画布那一组走 `HudCanvas.PosPx`）</summary>
        protected static Vector3 At(float cx, float cy)
        {
            return LayoutSpace.ToWorld(cx / 1920f, 1f - cy / 1080f);
        }

        /// <summary>原版 `CardsHorizontalLayout` 的**中心距**（px）。**问出来的**，不是推算的
        /// —— 出处：实况报告 §③（`GetPosition(i,n)` 直接调用，n=2 算得 502.1 与序列化字段吻合）。
        /// ⚠️ **n=4 就开始压**（原来文档写「>7 张才压」是错的，2026-09-14 晚更正）。
        /// ⚠️ 原版只测到 **n=7**；`n≥8` 按 7 的间距走 —— **这一格是外推，如实标着**。</summary>
        public static float SpacingPx(int n)
        {
            if (n <= 3) return 502.2f;
            if (n == 4) return 470.6f;
            if (n == 5) return 376.5f;
            if (n == 6) return 313.7f;
            return 268.9f;                      // n>=7（7 是实测，8+ 是外推）
        }

        /// <summary>面板里那张卡的缩放 —— 原版 `CardsHorizontalLayout.Scale = 183.41`
        /// 是「每**卡本地单位**多少像素」，而我们的世界是 1 单位 = 108 px
        /// ⇒ `183.41/108`。卡本体 `CardView` 的宽高正好等于原版 `CardUI` 的子节点 `2DCard`
        /// （`2.093×3.331`，实据见 `资料/选牌_数据与规格.md` §丙·三末；⚠️ 更正：原来指 `选牌与选效果面板_原版数值.md`，2026-10-10 已并入）。
        /// ⇒ 乘出来 = 383.8×611.0 px，与原版实测的 383.9×610.9 **对得上**。
        /// 🔴 **2026-10-16（`T09` 批 3）**：卡片仍走**世界单位** ⇒ 这个常量（`÷108` 那一档）
        /// **一个字没改**；它只在 `CardView.transform.localScale` 上用（卡是世界壳上的件）。</summary>
        public const float CardScale = 183.41f / 108f;

        // ==================================================================
        //  建
        // ==================================================================

        /// <summary>建面板。**派生类的 `Create` 里调它**（`this` 就是世界壳那个节点）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）**：本方法现在**自己**建/取子画布与画布本体
        /// （先例 = `BattleLogPanel.Build`）—— 派生类的 `Create` 只管那个**世界壳**节点
        /// （它的 rect 与出处见 `ChoosePanel.Create`）。</para>
        /// <para>⚠️ 子画布建不起来时**不静默、也不退回世界空间 quad**（半迁移态比不迁移更坏）：
        /// 出声、这一批图与字**不建**（卡片仍由调用方建在世界壳上，照旧）。</para></summary>
        protected void Build(Layout lo, PickMode mode, string title, string confirm)
        {
            Lo = lo;
            Mode = mode;
            _titleText = title ?? "";
            _confirmText = confirm ?? "";
            var root = transform;                       // 世界壳：候选卡 + 整屏压暗

            // ---- 子画布：`OverHand` / order 0（判据 = 文件头那一行，⛔ 别自己编）--------------------
            // 🔴 **子 Canvas 一个 `CanvasScaler` 都不许加**（原版全场景只有一个、在根上 ⇒ 加了会二次缩放）。
            HudCanvas.Ensure(LayoutSpace.Cam);          // 幂等（`HudAbs` 那一族会先建；这里兜底）
            // 🔴 **2026-10-16（修波 `FD` · 账 `F10`）就地订正（铁律 5）**：这里原来还写着
            //   「先 `SubCanvas(SubCanvasName)` 按名字找一次、找不到再 `EnsureSubCanvas`」，
            //   理由（写在别处）是「`EnsureSubCanvas` 不会去重」—— **那句已过期**：它早已改成**按名字幂等**
            //   （复用路径还会对齐层/order + 重排次）⇒ 那个包装**已删**，直接调它。
            var cvRt = HudCanvas.EnsureSubCanvas(null, SubCanvasName,
                                                 HudCanvas.SortingLayer.OverHand, 0);
            if (cvRt == null)
            {
                Debug.LogError("[选卡面板] `OverHand` 子画布建不起来（根 canvas 没有？）"
                             + "⇒ **本面板的图与字这一批不建**（⛔ 不退回世界空间 quad）");
                SetVisible(false);
                return;
            }
            // 画布本体：**逐字照原版那对锚点** —— `anchor (0,0)-(1,1)` + `sizeDelta (0,0)`（= 整屏 ⇒ `FillParent`）
            // ⛔ 别用 `MenuDraw.SetPxSize`（它体内是 `LayoutSpace.Px` = **÷108** ⇒ 整扇缩成 1/108、不报错）。
            // ⛔ 也别写死 `1920×1080` —— `Expand` 档下画布 rect 会更大/更高（21:9 / 4:3），写死的盖不满两边
            //    （`HudCanvas.FillParent` 的 doc；先例 = `EndPanel.Create` / `BattleLogPanel.Build`）。
            // ✅ 铺满之后本节点的**中心 = 画布中心 = 屏心** ⇒ `HudCanvas.PosPx(x, y从顶)` 交出来的那一对数
            //    仍然**正好**是本节点的局部位移（子件全是中心锚）。
            var pgo = new GameObject(PanelNodeName, typeof(RectTransform));
            pgo.transform.SetParent(cvRt, false);
            _panel = pgo.transform;
            HudCanvas.FillParent((RectTransform)_panel);
            HudCanvas.Add((RectTransform)_panel, ZBody);   // 登记兄弟序（子画布下只有它一个）

            // 压暗（**我们挑的**：原版只查到有个 `Shade.SwitchShade`，颜色/透明度没查到）
            // 🔴 **2026-10-16（`T09` 批 3）：这一件【有意留在世界空间】** —— ⛔ 别顺手迁上去，理由三条
            //    写在文件头（核心：它必须画在**卡片后面**，而卡片是 `MeshRenderer`、不归画布管，
            //    画布上的件一律画在卡片**上面**）。`MultiCardDisplay.mcd_mask` 是同一条。
            _shade = ImageQuad.Create(root, CardArt.Solid(), At(960f, 540f), U(1200f),
                                      new Vector2(0.5f, 0.5f), "ChoiceShade");
            if (_shade != null)
            {
                _shade.SetAspect(1920f / 1080f);
                _shade.SetTint(new Color(0f, 0f, 0f, 0.55f));
                _shade.transform.localPosition += new Vector3(0f, 0f, Z);
            }
            else Debug.LogError("[选卡面板] 整屏压暗（`ChoiceShade`）没建起来（`CardArt.Solid()` 取不到？）");

            // ---- 以下 = 「画布本体」那一组（图与字，全部在设计 px 上）--------------------------------
            // 🔴 **换口**：`Label.Create(...) + At(px,py)` → `HudCanvas.MakeText(...)`（**设计 px**、
            //    **相对父件中心**、y 向上 ⇒ 用 `HudCanvas.PosPx` 把「左上原点 px」折过来）；
            //    `ImageQuad.Create(...)` → `HudCanvas.MakeImageInParent(...)`（**相对父件中心**、
            //    宽高各给一个数）。⛔ **不能**用 `QuadFromTopPx`/`MakeImage` —— 那两个的 `x/y` 是
            //    **屏幕绝对 px**（锚点 = `x/1920`、`1−y/1080`），**只在 `parent` 本身是整屏矩形时才成立**。
            //  ⚠️ **同批删掉的一段**：旧那套建完还要 `foreach (…) q.transform.localPosition += (0,0,Z)`
            //     逐件把 z 拨回去（`_title`/`_confirm` 拨 `Z − 0.05`）。uGUI 档里 **z 不进 `localPosition`**
            //     （画布上看不出 z 的作用）—— 那个 z 现在**直接当建件口的 `z` 形参传**，由 `HudCanvas.Add`
            //     换算成**兄弟序**（z 大的排前面 = 画在**底下**，与旧那套**同向**）⇒ 相对先后**逐件不变**。

            // 标题（原版 `ChooseText` / `MulliganText`：`sd=1344×79.44`、`a=(.5,1)`、`ap=(7,-106.5)`）
            var tp = HudCanvas.PosPx(lo.TitleCx, lo.TitleCy);
            // 🆕 **2026-10-17（界面线 `T07` · `A1753` #15）**：原版 `ChooseText`（`Choose one card`）挂
            //   `Pragati-Regular Orange`，`_FaceColor = (0.996, 0.596, 0.118)`、顶点白 ⇒ 屏上是**橙**。
            //   改前传 (1, 0.94, 0.82)（G/B 都偏高 ⇒ 橙变米黄）。判据 = `资料/普查产出_1017/UI/R_T07_剩余四项.md` §4·2 #15。
            //   ⚠️ **落点口径**：普查报告把它记成 `Battle/ChoosePanel.cs:270` —— 真正的建件点在**本文件**
            //   （`ChoosePanel : CardChoicePanel`，标题由基类 `Build()` 建；`ChoosePanel.cs` 只放数值）。
            _title = HudCanvas.MakeText(_panel, _titleText, tp.x, tp.y, 8, new Color(0.996f, 0.596f, 0.118f),
                                        new Vector2(0.5f, 0.5f), Z - 0.05f, "ChoiceTitle", face: TmpFont.Face.LatinCardBody);
            // 中文用汉字高度，不是大写高度。⚠️ uGUI 档的单位**就是设计 px** ⇒ 直接喂 `TitlePx`
            // （旧那套要 `U(...)` 折成世界单位；⛔ 别在这儿再除 108 —— 字会小 108 倍、不报错）。
            if (_title != null) _title.SetGlyphHeight(lo.TitlePx);

            // 确认：底条 + 圆形播放钮（两边同图同尺寸）
            // ⚠️ 底条**给的是框**（577.5×63.84 = 原版 `BG` 的 `m_SizeDelta`，`PA=0` 拉满），
            //    ⛔ **不是**贴图比例 (485/83 ⇒ 只有 372.9 宽)；旧那套靠 `SetAspect(BarW/BarH)` 表达同一件事，
            //    uGUI 档宽高各给一个数 ⇒ 那一句**不再需要**（实绘尺寸与改之前逐位相同）。
            _bar = MakeImg(_panel, CardArt.Ui("40k_bt_underbutton"), "ChoiceBar",
                           lo.BarCx, lo.BarCy, BarW, BarH, Z);
            // 圆钮 / 眼睛 / 卡上那颗钮：**只给高、宽由贴图比例来**（= 旧 `ImageQuad.Create` 的口径）
            _play = ImgByHeight(_panel, CardArt.Ui("40k_UI_bt_play"), "ChoiceCircle",
                                lo.PlayCx, lo.PlayCy, PlayH, Z);
            var cp = HudCanvas.PosPx(lo.ConfirmCx, lo.ConfirmCy);
            _confirm = HudCanvas.MakeText(_panel, _confirmText, cp.x, cp.y, 6,
                                          new Color(1f, 0.92f, 0.75f), new Vector2(0.5f, 0.5f),
                                          Z - 0.05f, "ChoiceConfirmText", face: TmpFont.Face.LatinCardBody);
            if (_confirm != null) _confirm.SetGlyphHeight(33.7f);    // ⚠️ 实况 fs=33.7（不是 §三 那个 45）

            // 眼睛：收起卡片上的按钮（原版 `onClick → ToogleChooseMenuVisibility`）
            _eye = ImgByHeight(_panel, CardArt.Ui("40k_UI_bt_eye"), "ChoiceEye",
                               lo.EyeCx, lo.EyeCy, EyePx, Z);

            if (_bar == null) Debug.LogWarning("[选卡面板] 底条没建起来（`40k_bt_underbutton` 取不到？）");
            if (_play == null) Debug.LogWarning("[选卡面板] 圆钮没建起来（`40k_UI_bt_play` 取不到？）");
            if (_eye == null) Debug.LogWarning("[选卡面板] 眼睛没建起来（`40k_UI_bt_eye` 取不到？）");

            SetVisible(false);
        }

        /// <summary>建一块**给定了框**的 uGUI 图（宽高各一个数 ⇒ 拉满框，= 原版 `PA=0`）。
        /// `cxPx/cyPx` = **面板内设计 px**（左上原点）、`z` = 旧那套的层深度（据它登记兄弟序）。
        /// ⚠️ 贴图取不到 ⇒ 返回 `null`（与旧那套 `ImageQuad.Create` 同一条：它也是贴图为空即 null），
        /// ⛔ 不摆白块、也不静默。</summary>
        static HudPiece MakeImg(Transform parent, Texture2D tex, string name,
                                float cxPx, float cyPx, float wPx, float hPx, float z)
        {
            if (parent == null || tex == null) return null;
            return HudCanvas.MakeImageInParent(parent, tex, name, cxPx - 960f, cyPx - 540f, wPx, hPx, z,
                                               Color.white);
        }

        /// <summary>建一块「**按高定、宽由贴图比例来**」的 uGUI 图 —— `ImageQuad.Create(parent, tex, pos,
        /// **worldHeight**, anchor, name)` 的定宽口径（⛔ 别改成正方、也别当成 `(w,h)` 照抄）。
        /// `cxPx/cyPx` = **面板内设计 px**（左上原点）、`hPx` = 高（设计 px）。</summary>
        static HudPiece ImgByHeight(Transform parent, Texture2D tex, string name,
                                    float cxPx, float cyPx, float hPx, float z)
        {
            if (parent == null || tex == null) return null;
            float wPx = hPx * (tex.width / (float)Mathf.Max(1, tex.height));
            return HudCanvas.MakeImageInParent(parent, tex, name, cxPx - 960f, cyPx - 540f, wPx, hPx, z,
                                               Color.white);
        }

        /// <summary>改标题（`Open` 时按「正在结算的是哪张卡」传进来）。</summary>
        protected void SetTexts(string title, string confirm)
        {
            _titleText = title ?? "";
            if (!string.IsNullOrEmpty(confirm)) _confirmText = confirm;
            if (_title != null) _title.SetText(_titleText);
            if (_confirm != null) _confirm.SetText(_confirmText);
        }

        // ==================================================================
        //  开 / 关
        // ==================================================================

        public void Open(IReadOnlyList<CardView> cards, string title)
        {
            _picked.Clear();
            if (!string.IsNullOrEmpty(title)) SetTexts(title, _confirmText);
            LayOutCards(cards);
            SetVisible(true);
        }

        public void Close()
        {
            ClearCardDecor();
            SetVisible(false);
        }

        void SetVisible(bool v)
        {
            Visible = v;
            // ⚠️ `_shade` 是 `ImageQuad`（世界档那一种）、其余是 `HudPiece` —— 两种的 `gameObject` 口同形，
            //    所以这一句**一个字没改**（⛔ 不必因为迁了半批就把它们分成两行）。
            if (_shade != null) _shade.gameObject.SetActive(v);
            foreach (var q in new[] { _bar, _play, _eye })
                if (q != null) q.gameObject.SetActive(v);
            if (_title != null) _title.gameObject.SetActive(v);
            if (_confirm != null) _confirm.gameObject.SetActive(v);
            for (int i = 0; i < _cardBtns.Count; i++)
                if (_cardBtns[i] != null) _cardBtns[i].gameObject.SetActive(v);
            for (int i = 0; i < _cardTexts.Count; i++)
                if (_cardTexts[i] != null) _cardTexts[i].gameObject.SetActive(v);
        }

        /// <summary>
        /// 把候选牌摆成一行（**原版自己的间距表**），每张下面贴一颗按钮。
        /// ⚠️ **纵向位置是我们挑的**（原版那个 anchor 无 RectTransform，见文件头）。
        /// ⚠️ 按钮的**纵向偏移也是我们排的**（原版 prefab 运行时实例化、静态 dump 里没有）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）**：本方法现在**同时在两个坐标系里算同一套布局** ——
        /// **卡片**在世界壳上（世界单位，`U()` + `At()`，**一字未改**）、**卡上那颗钮与它的字**在**画布本体**上
        /// （设计 px，用 `SpacingPx(n)` 那张表与 `Lo.CardRowCy` **直接算 px**）。
        /// ⇒ 16:9 下两者**逐位重合**；别的宽高比下会分家（卡片不跟 `CanvasScaler` 缩放，见文件头那条代价）。</para></summary>
        void LayOutCards(IReadOnlyList<CardView> cards)
        {
            ClearCardDecor();
            _cards.Clear();
            if (cards == null) return;
            for (int i = 0; i < cards.Count; i++) if (cards[i] != null) _cards.Add(cards[i]);
            int n = _cards.Count;
            if (n == 0) return;

            // ---- ① 卡片那一支：世界单位（**原样**，⛔ 别换成 px）----
            float spacing = U(SpacingPx(n));
            float total = spacing * (n - 1);
            float left = At(960f, Lo.CardRowCy).x - total * 0.5f;
            float y = At(960f, Lo.CardRowCy).y;
            // ---- ② 卡上装饰那一支：设计 px（画布本体上）----
            float spacingPx = SpacingPx(n);
            float leftPx = 960f - spacingPx * (n - 1) * 0.5f;   // 第一张卡的中心（px）

            for (int i = 0; i < n; i++)
            {
                float cxPx = leftPx + spacingPx * i;

                var c = _cards[i];
                c.transform.localScale = new Vector3(CardScale, CardScale, 1f);
                c.transform.localPosition = new Vector3(left + spacing * i, y, Z);
                c.SetHighlight(CardHighlightState.Normal);

                if (_panel == null) continue;      // 子画布没立起来那一支：卡照摆、装饰不建（已出声）

                // ⚠️ 按钮位置：原版那个 prefab `CardChooseCardButtonFrame` 的 `Select` 钮在
                //    **卡中心下方 ≈323 px**（`资料/选牌_数据与规格.md` §丙·三末，×183.41 实测；⚠️ 更正：原来指 `选牌与选效果面板_原版数值.md`，2026-10-10 已并入）。
                //    2026-09-14 第一版写成「卡高 ×0.30」≈183 px ⇒ **压在卡面的文字上**（截图才发现）。
                float btnCyPx = Lo.CardRowCy + 323f;
                var q = ImgByHeight(_panel, CardArt.Ui("UI_Button_Mulligan"), "ChoiceCardBtn_" + i,
                                    cxPx, btnCyPx, 77f, Z - 0.02f);
                if (q != null) _cardBtns.Add(q);

                var t = HudCanvas.MakeText(_panel, Lo.CardBtnText, cxPx - 960f, btnCyPx - 540f, 5,
                                           new Color(1f, 0.9f, 0.7f), new Vector2(0.5f, 0.5f),
                                           Z - 0.02f + (Z - 0.05f), "ChoiceCardBtnText_" + i, face: TmpFont.Face.LatinCardBody);
                if (t != null)
                {
                    // 🔴 **2026-10-18（W6）**：语种定字号 —— 这一格现在走**原版词条**
                    //    （选牌 = `Battle/Prebattle/SelectButton` / 换牌 = `Battle/Mulligan/Replace`），
                    //    汉字与拉丁大写差 39%，写死一种就会让另一档错（`Label.SetScriptHeight` 是唯一那份判据）。
                    //    ⚠️ **2026-10-16（`T09` 批 3）**：`pxPerUnit` 由 **108 → 1** —— uGUI 档的
                    //    canvas 单位**就是**设计 px（`Label.PxPerUnit` 的 doc）；传 108 会让这行字**小 108 倍**
                    //    而**一个字都不报**。
                    t.SetScriptHeight(Lo.CardBtnText, 28f, 1f);
                }
                _cardTexts.Add(t);
            }
            BuildFeedback();
        }

        /// <summary>🆕 2026-10-19（B4 · `N70` P0-2）：建/重建按钮反馈（与换牌面板同族判据）。</summary>
        void BuildFeedback()
        {
            _fxBar = BattleButtonFx.Tint(_bar, "Continue Button");
            var eh = CardArt.MenuUi("40k_UI_bt_eye_hover");
            var ep = CardArt.MenuUi("40k_UI_bt_eye_pressed");
            _fxEye = (eh != null && ep != null)
                   ? BattleButtonFx.Swap(_eye, "HideChooseButton", _eye != null ? _eye.Texture as Texture2D : null, eh, ep)
                   : BattleButtonFx.Tint(_eye, "HideChooseButton(tint-fallback)");
            _fxCardBtns.Clear();
            var mh = CardArt.Ui("UI_Button_Mulligan_hover");
            var mp = CardArt.Ui("UI_Button_Mulligan_Pressed");
            for (int i = 0; i < _cardBtns.Count; i++)
            {
                var fx = (mh != null && mp != null)
                       ? BattleButtonFx.Swap(_cardBtns[i], "ChoiceCardBtn_" + i,
                                             _picked.Contains(i) ? mp : CardArt.Ui("UI_Button_Mulligan"), mh, mp)
                       : BattleButtonFx.Tint(_cardBtns[i], "ChoiceCardBtn_" + i + "(tint-fallback)");
                _fxCardBtns.Add(fx);
            }
        }

        /// <summary>每帧喂状态（驱动层 `HandleChoose` 里调）。</summary>
        public void TickFeedback(Vector3 world, bool held)
        {
            if (!Visible) { ResetFeedback(); return; }
            if (_fxBar == null) BuildFeedback();
            FeedFx(_fxBar, HitDone(world), held);
            FeedFx(_fxEye, HitEye(world), held);
            for (int i = 0; i < _fxCardBtns.Count && i < _cardBtns.Count; i++)
                FeedFx(_fxCardBtns[i], _cardBtns[i] != null && _cardBtns[i].Contains(world), held);
        }
        void ResetFeedback()
        {
            FeedFx(_fxBar, false, false); FeedFx(_fxEye, false, false);
            for (int i = 0; i < _fxCardBtns.Count; i++) FeedFx(_fxCardBtns[i], false, false);
        }
        static void FeedFx(BattleButtonFx fx, bool over, bool held)
        {
            if (fx == null) return;
            fx.SetHover(over); fx.SetPressed(over && held); fx.Tick();
        }
        public void SimulateFeedback(string node, bool hover, bool pressed)
        {
            if (_fxBar == null) BuildFeedback();
            BattleButtonFx fx = node == "done" ? _fxBar : node == "eye" ? _fxEye
                             : node.StartsWith("card") && _fxCardBtns.Count > 0
                               ? _fxCardBtns[Mathf.Clamp(int.Parse(node.Substring(4)), 0, _fxCardBtns.Count - 1)]
                               : null;
            if (fx == null) return;
            fx.SetHover(hover); fx.SetPressed(pressed);
        }
        public Color FeedbackKeyForTest(string node)
        {
            if (_fxBar == null) BuildFeedback();
            BattleButtonFx fx = node == "done" ? _fxBar : node == "eye" ? _fxEye : null;
            return fx != null ? fx.KeyForTest : Color.clear;
        }
        public string FeedbackTextureForTest(string node)
        {
            if (_fxBar == null) BuildFeedback();
            BattleButtonFx fx = node == "eye" ? _fxEye
                             : node != null && node.StartsWith("card") && _fxCardBtns.Count > 0
                               ? _fxCardBtns[Mathf.Clamp(int.Parse(node.Substring(4)), 0, _fxCardBtns.Count - 1)]
                               : null;
            return fx != null ? fx.TextureNameForTest : null;
        }

        void ClearCardDecor()
        {
            for (int i = 0; i < _cardBtns.Count; i++) if (_cardBtns[i] != null) Kill(_cardBtns[i].gameObject);
            for (int i = 0; i < _cardTexts.Count; i++) if (_cardTexts[i] != null) Kill(_cardTexts[i].gameObject);
            _cardBtns.Clear();
            _cardTexts.Clear();
            _fxCardBtns.Clear();
            _cards.Clear();
        }

        protected static void Kill(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        // ==================================================================
        //  选择
        // ==================================================================

        /// <summary>选中/取消一张牌。**单选**模式下选新的会顶掉旧的（原版选牌就是这个语义）。</summary>
        public void TogglePick(int i)
        {
            if (i < 0 || i >= _cards.Count) return;
            if (_picked.Contains(i)) _picked.Remove(i);
            else
            {
                if (Mode == PickMode.Single && _picked.Count > 0) _picked.RemoveAt(0);
                _picked.Add(i);
            }
            ApplyPicks();
        }

        /// <summary>被选中的下标（**升序**）。</summary>
        public List<int> Picked { get { var l = new List<int>(_picked); l.Sort(); return l; } }
        public bool IsPicked(int i) { return _picked.Contains(i); }
        public int CardCount { get { return _cards.Count; } }

        /// <summary>选中的样子：卡**置灰**（`Unplayable` 那一档）+ 按钮换成按下态那张图。
        /// 换牌那套就是靠这两处表达「这张要换掉」的。</summary>
        void ApplyPicks()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c == null) continue;
                bool m = _picked.Contains(i);
                c.SetHighlight(m ? CardHighlightState.Unplayable : CardHighlightState.Normal);
                if (i < _cardBtns.Count && _cardBtns[i] != null)
                    _cardBtns[i].SetTexture(CardArt.Ui(m ? "UI_Button_Mulligan_Pressed" : "UI_Button_Mulligan"));
                    if (i < _fxCardBtns.Count && _fxCardBtns[i] != null)
                        _fxCardBtns[i].SetNormal(CardArt.Ui(m ? "UI_Button_Mulligan_Pressed" : "UI_Button_Mulligan"));
            }
        }

        // ==================================================================
        //  交互（和设置面板/日志面板同一条路：批处理下由驱动层喂指针）
        // ==================================================================

        /// <summary>指针在哪张牌的按钮上（-1 = 没点中）
        /// 🔴 **2026-10-16（`T09` 批 3）**：`HudPiece.Contains` 走 `GetWorldCorners()` ⇒ 量的是**画出来的那个
        /// 世界四边形**、两种模式都对（`HudPiece` 文件头那条判据）⇒ 本口**一行没改**。
        /// ⚠️ 驱动喂进来的是 `CardWorldPos(i)`（**卡的中心**）—— 而那颗钮在卡心**下方 323 px**
        /// ⇒ 这一支**对卡心永远不中**（真正命中的是下面 `HandleClick` 里那条「点卡本身也能选」）。</summary>
        public int ButtonIndexAt(Vector3 world)
        {
            for (int i = 0; i < _cardBtns.Count; i++)
                if (_cardBtns[i] != null && _cardBtns[i].Contains(world)) return i;
            return -1;
        }

        /// <summary>指针在「确认」上吗（**原版三条可射线 Image 的并集**，见下）。
        ///
        /// <para>🔴🔴 **2026-10-19（`T03` · `A1327`）就地重写 —— 旧口径三处都不对**（铁律 5）：
        ///  · 旧 ①：`_bar.Contains` = **画出来那颗 quad** —— 数值上恰好 = 原版 `BG`（577.5×63.84），这半条对；
        ///  · 旧 ②：`_play.Contains` = 圆钮 —— **也对**（原版那颗 `rt=1`，且 `m_OnClick` 直绑
        ///    `ChooseCardMenu.ClickChooseDone` ⇒ 它就是「确认」本体之一；⛔ 旧判据文件说它 `rt=0` 是**错的**）；
        ///  · 旧 ③：`圆心 = 确认文字、r = 190 px` —— **我们自己加的**，纵向多认 ~147/152 px，且漏了父矩形；
        ///    **还漏了原版那颗「父 `Continue Button`」**（`Image`、`alpha=0`、`m_RaycastTarget=1`、pad 全 0
        ///    ⇒ uGUI 照样吃射线）。</para>
        ///
        /// <para>**判据 = arena2（arena1 逐值同）`ChooseCardMenu/ButtonsGroup/Continue Button` 子树，
        /// 逐节点现读 + 父链 `m_FileID=0` 全解**：
        ///  · ① 父 `Continue Button`：`548.015 × 75.905`，中心 = `(BarCx − 65.196, BarCy − 5.749)`
        ///    （父在 `ButtonsGroup` 里的 pos 减去 `BG` 相对父的 pos `(65.196, −5.749)`；pad 0）；
        ///  · ② `BG`：`577.5 × 63.84`，中心 = `(BarCx, BarCy)`（pad 0）；
        ///  · ③ 圆钮 `ContinueButton`：`80.469 × 79.636`，中心 = `(PlayCx, PlayCy)`（pad 0）；
        ///  · `ContinueText`（TMP，`rt=1`）完全落在 `BG` 内 ⇒ 并集不必再单独判它。
        ///  ⚠️ **三颗的 `m_RaycastPadding` 全 `(0,0,0,0)`** —— ⛔ **别把 `MulliganPanel` 的
        ///  `DonePad*(0,−40,0,−40)` 搬过来**（那棵是 `Mulligan/ButtonsGroup/MulliganContinueButton/Button`，
        ///  与本树**不是同一个**；搬过来会凭空多认 80 px 高）。判据全文 →
        ///  `资料/普查产出_1019/W_战斗线_T03命中区_1019b.md` §一-3。</para>
        ///
        /// <para>坐标系：世界 → `_panel` 局部（= **设计 px**、相对屏心、y 向上；`CanvasScaler(Expand)` 下
        /// 任意宽高比都是这一套）→ 折成**左上原点、y 向下**（与 `Layout.*Cy` 同帧）。
        /// ⛔ 别用 `LayoutSpace.ToPixel`（世界帧的逆，非 16:9 下与画布帧分家）。</para></summary>
        public bool HitDone(Vector3 world)
        {
            if (_panel == null) return false;                    // 面板没建出来 ⇒ 不认（⛔ 不拿 (0,0) 当命中）
            var l = _panel.InverseTransformPoint(world);
            float px = l.x + 960f, py = 540f - l.y;              // 设计 px（左上原点、y 向下）
            // ① 父 `Continue Button`（548.015×75.905 @ (BarCx−65.196, BarCy−5.749)）
            float pcx = Lo.BarCx + DoneParentDx, pcy = Lo.BarCy + DoneParentDy;
            if (Mathf.Abs(px - pcx) <= DoneParentW * 0.5f && Mathf.Abs(py - pcy) <= DoneParentH * 0.5f) return true;
            // ② `BG` 底条（577.5×63.84 @ (BarCx, BarCy)）
            if (Mathf.Abs(px - Lo.BarCx) <= BarW * 0.5f && Mathf.Abs(py - Lo.BarCy) <= BarH * 0.5f) return true;
            // ③ 圆钮（80.469×79.636 @ (PlayCx, PlayCy)）
            return Mathf.Abs(px - Lo.PlayCx) <= PlayW * 0.5f
                && Mathf.Abs(py - Lo.PlayCy) <= PlayH * 0.5f;
        }

        /// <summary>🆕 2026-10-19（`T03`·`A1327`）自检用：原版 `BG` 底条的**框中心**世界坐标
        /// （`Layout.BarCx/BarCy` 那个设计 px 点，经**画布本体**换算 ⇒ 与 `HitDone` 同一帧）。
        /// ⚠️ 与 `DoneWorldPos`（**确认文字**中心）不是同一个点（沿 x 差 69.8 px）—— 别互相顶替。</summary>
        public Vector3 BarWorldPos
        {
            get
            {
                if (_panel == null) return Vector3.zero;
                return _panel.TransformPoint(new Vector3(Lo.BarCx - 960f, 540f - Lo.BarCy, 0f));
            }
        }

        public bool HitEye(Vector3 world) { return _eye != null && _eye.Contains(world); }

        /// <summary>确认钮的**世界**位置（自检与驱动都拿它当「点确认」那一击）。
        /// 🔴 **2026-10-16（`T09` 批 3）改成读【实件】**：原来按常量算 `At(ConfirmCx, ConfirmCy)`
        /// —— 那只在**确认字本身也在世界空间**时与画面对得上。字现在在**画布**上，而「世界单位 ↔ 画布单位」
        /// 的两个轴**系数可以不同**（`CanvasScaler(Expand)` 在「屏 4:3、相机 `aspect` 钉 16:9」那种档里
        /// ⇒ x `1/108`、y `1/144`；判据 → `T09` **§七·8**）⇒ 按常量算出来的点**不再落在画出来的那个矩形里**
        /// （画面是对的、读数错 0.75 倍）。改读实件之后，「画出来的和点得到的」永远是同一个东西。
        /// ⚠️ 顺序 = 确认字 → 底条 → 老那个常量式（逐级兜底，⛔ 不许返回 `Vector3.zero` —— 那是屏心、
        /// 是个合法位置）。</summary>
        public Vector3 DoneWorldPos
        {
            get
            {
                if (_confirm != null) return _confirm.transform.position;
                if (_bar != null) return _bar.transform.position;
                return At(Lo.ConfirmCx, Lo.ConfirmCy) + new Vector3(0f, 0f, Z);
            }
        }

        /// <summary>眼睛钮的**世界**位置（读数口径同 <see cref="DoneWorldPos"/> —— 从实件读）。</summary>
        public Vector3 EyeWorldPos
        {
            get
            {
                if (_eye != null) return _eye.transform.position;
                return At(Lo.EyeCx, Lo.EyeCy) + new Vector3(0f, 0f, Z);
            }
        }

        public Vector3 CardWorldPos(int i)
        {
            return (i >= 0 && i < _cards.Count && _cards[i] != null) ? _cards[i].transform.position : Vector3.zero;
        }

        /// <summary>眼睛钮 = 把卡片上的按钮收起来看战场（原版 `ToogleChooseMenuVisibility`）。
        /// 🔴 **2026-09-17 更新**：原版**这个方法本身没被反编译**（`decomp_out*` 里没有它），
        ///    但**换牌面板那颗同款眼睛**（`MulliganManager.ToggleMulliganVisibility`）反编译了，
        ///    而且**是开关不是按住**（读 `activeInHierarchy` 取反 → `ShowMulliganElements`），
        ///    而且连**压暗层**一起收（`Shade.SwitchShade`）。
        ///    ⇒ 我们按同一套做成开关（**同一作者同一 UI 语言的推断，不是直接证据**，如实标着）。
        ///    ⚠️ 我们这里**没有把压暗层也收起来** —— 换牌面板那边补了，这一处**还没补**（原版那个方法看不到）。</summary>
        public void ToggleEye()
        {
            bool show = !(_cardBtns.Count > 0 && _cardBtns[0] != null && _cardBtns[0].gameObject.activeSelf);
            for (int i = 0; i < _cardBtns.Count; i++)
                if (_cardBtns[i] != null) _cardBtns[i].gameObject.SetActive(show);
            for (int i = 0; i < _cardTexts.Count; i++)
                if (_cardTexts[i] != null) _cardTexts[i].gameObject.SetActive(show);
        }

        /// <summary>
        /// 处理一次点击。返回 true = 被面板吃掉了（别再传给棋盘/手牌）。
        /// ⚠️ 面板开着时**点哪儿都先经过这里**（原版 `Shade` 就是这个意思）。
        /// </summary>
        public virtual bool HandleClick(Vector3 world)
        {
            if (!Visible) return false;

            if (HitDone(world))
            {
                var picks = Picked;
                if (OnDone != null) OnDone(picks);
                return true;
            }
            if (HitEye(world)) { ToggleEye(); return true; }

            int hit = ButtonIndexAt(world);
            if (hit >= 0) { TogglePick(hit); return true; }

            // 点卡本身也能选（原版是卡片上那颗按钮；**点卡这条路是我们加的**）
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i] != null && _cards[i].Contains(world)) { TogglePick(i); return true; }

            return true;      // 面板开着 → 点其它地方也吃掉
        }

        // ==================================================================
        //  自检
        // ==================================================================

        public string TitleText { get { return _title != null ? _title.Text : "<无>"; } }
        public string ConfirmText { get { return _confirm != null ? _confirm.Text : "<无>"; } }
        public bool BarHasArt { get { return _bar != null && _bar.Texture != null; } }
        public bool PlayHasArt { get { return _play != null && _play.Texture != null; } }
        public bool EyeHasArt { get { return _eye != null && _eye.Texture != null; } }
        public string BarTex { get { return _bar != null && _bar.Texture != null ? _bar.Texture.name : "<无>"; } }
        public string EyeTex { get { return _eye != null && _eye.Texture != null ? _eye.Texture.name : "<无>"; } }
        public string CardBtnTex(int i)
        {
            if (i < 0 || i >= _cardBtns.Count || _cardBtns[i] == null) return "<无>";
            return _cardBtns[i].Texture != null ? _cardBtns[i].Texture.name : "<无>";
        }
        public int CardButtonCount { get { return _cardBtns.Count; } }

        /// <summary>🆕 **2026-10-18（W6）**：第 <paramref name="i"/> 张牌下那颗钮上**写的什么**
        /// （自检用；没有那一颗时返回 null）。判据 = 原版 `Battle/Prebattle/SelectButton` /
        /// `Battle/Mulligan/Replace` 的当前语档取值（→ `Core/Loc.cs`）。</summary>
        public string CardBtnWordAt(int i)
        {
            return i >= 0 && i < _cardTexts.Count && _cardTexts[i] != null ? _cardTexts[i].Text : null;
        }

        /// <summary>第 i 张牌的中心 x（世界）—— 自检拿来验「间距照原版那张表」。
        /// ⚠️ **2026-10-16（`T09` 批 3）**：卡片仍在**世界壳**上 ⇒ 本口仍返回**世界 x**（口径未变）。</summary>
        public float CardWorldX(int i)
        {
            return (i >= 0 && i < _cards.Count && _cards[i] != null) ? _cards[i].transform.position.x : 0f;
        }

        public string Describe()
        {
            return $"选卡面板 开着={Visible} 模式={Mode} 卡 {_cards.Count} 张 选中 [{string.Join(",", Picked.ConvertAll(x => x.ToString()).ToArray())}]";
        }
    }
}
