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
//   · ~~压暗层（原版有 `Shade.SwitchShade`，颜色/透明度没查到）~~ —— 🔴 **2026-10-25（审查代理 `AU2` · 铁律 5）
//     已订正：两样**都查得到**（`Shade.onAlphaLevel = 0.8` + 底图 `m_Color (0,0,0,0)`/`no sprite`/`rt=0`，
//     3 形参重载 ⇒ 硬切）⇒ 已改成 <see cref="ShadeAlpha"/>（原来是 0.55）。
//     🔴 **2026-10-25（`M1` 追加单）**：它现在**转发** `MulliganPanel.ShadeAlpha`（按工程色彩空间取值：
//     原版 γ 口径 0.8 / 我们 Linear = 0.945）⇒ ⛔ 别再把这行写成「= 0.8」。⛔ 本件不再有这一条。
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
//
// ─────────────────────────────────────────────────────────────────────────────
//  🔴 **2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e）：输入层换 uGUI**（判据逐组件现读，⛔ 不是照计划抄）
//   · **每张卡下那颗 `Select` 钮**：原版 `CardChooseCardButtonFrame` 的 `selectButton` PPtr →
//     `Generic Simplified UI Button_updated`（`bundle_battlesharedresources_assets_all`）——
//     `Image.m_RaycastTarget = 1` · `m_RaycastPadding = (0,0,0,0)` · 组件是 **`EverguildButton`**
//     （`: Button` ⇒ `IPointerClickHandler`，**松手**）。我们用 `BattleHudButton` 挂**同一张图**。
//   · **确认 = 原版三条可射线 Image 的【并集】**（`A1327` 已把 `HitDone` 按它重写，本波只是把同一块几何
//     变成真射线面）：① 父 `Continue Button`（`RT_3196` = **548.015 × 75.905**、`MB_4413`：`alpha=0`、
//     `sprite=null`、**`m_RaycastTarget=1`**、pad 0）② `BG`（`RT` 577.500183 × 63.84、`MB` rt=1、pad 0）
//     ③ 圆钮 `ContinueButton`（`RT_3324` = **80.468689 × 79.635559**、`MB_5075` rt=1、pad 0）。
//     ②③ 落在 `Continue Button` 的**子树**里 ⇒ 点击沿父链冒泡到父那颗 `Button`；父自己也有 `Button`
//     （`MB_4262`，`m_OnClick` 空 = 运行时 `AddListener`）。圆钮那颗的 `m_OnClick` **直接绑
//     `ChooseCardMenu.ClickChooseDone`**（`MB_4565`）⇒ 它就是确认本体之一。
//   · **眼睛**：`HideChooseButton`（`RT_3051` = **78.44²**、`MB_4921` rt=1 **PA=1**、pad 0），
//     `Button.m_OnClick` → `ChooseCardMenu.ToogleChooseMenuVisibility`（`MB_4813`）。
//   · 🔴 **本轮是【加输入路径】、⛔ 不动既有行为**：`HandleClick` / `HitDone` / `HitEye` / `ButtonIndexAt`
//     与驱动 `HandleChoose` 那条每帧手搓路**一个字没改**（那在 `BattleDriver`，不在本文件白名单）。
//     uGUI 这一侧**只发事件**（`CardButtonClicked` / `DoneClicked` / `EyeClicked`），**不自己调
//     `TogglePick` / `ToggleEye` / `OnDone`** —— 两条路同时动同一个状态 = 一次点击翻两次（= 什么都没发生）。
//     ⇒ 事件**当前没有订阅者**；合并配方（驱动订阅 + 删手搓路）写在报告 `W57_战斗线_T09_A3e1.md` §三。
//   · ⚠️ **「点卡本身也能选」那一支留在世界侧**（那是**我们加的**，原版只有卡上那颗钮）——
//     `CardView` 是 `MeshRenderer`、不在画布上，uGUI 射线够不着它（第 5 步 `Physics2D` 的活）。
//
//  🔴 **2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e1 / A3-e1d）：三处按原版收口**（判据逐组件现读）
//   · ① **卡钮 rect**：`Generic Simplified UI Button_updated`（`bundle_battlesharedresources_assets_all`）
//     的 `RectTransform_-9013717219094471892.m_SizeDelta = 1.0863 × 0.4224`（卡本地单位）
//     × `CardsHorizontalLayout.Scale = 183.41 px/单位` ⇒ **199.24 × 77.47 px**；那颗 `Image`
//     （`MB 4178237357239393068`）`m_PreserveAspect = **0**` ⇒ **拉伸铺满**。
//     改前我们走 `ImgByHeight(..., 77f)`（宽 = 高 × 贴图比例 410/124）⇒ **254.6 × 77**，宽了 **27.8%**
//     —— 已按原版 rect 改；判据全文 → `CardBtnW/CardBtnH` 那条 doc。
//   · ② **选中是【单向】的**：`SelectCardButtonFrame__SelectButtonOnClick.c` = `ChooseCardMenu.SelectCard`
//     → `Card2DController.ToggleHighlight(1)` → `Selectable.set_interactable(false)`（**那颗钮自己**）
//     ⇒ 同一张**收不到第二次点击**；换选另一张时才由 `ChooseCardUI.ToggleSelected(false)`
//     （`set_interactable(!selected)`）恢复上一颗。我们原来的 `TogglePick` 是**真 toggle**
//     （再点 = 取消）—— 已按 `PickMode.Single` 改单向（`Multi` 不动，见 `TogglePick` 的 doc）。
//   · ③ 🔴 **选中之后的卡钮露 Normal 图**（⛔ 不是 `_Pressed`，A3-e1d 改）：同一条链把钮
//     `set_interactable(false)`，而 prefab `m_SpriteState.m_DisabledSprite = 0` ⇒ `DoSpriteSwap(null)`
//     ⇒ 回落到 Image 自己的 `UI_Button_Mulligan`。判据全文 → `CardBtnArtName` 那条 doc。
//
//  🔴 **2026-10-20（W57 · 战斗线 `T09` · 确认钮那一波）：确认钮「开面板禁用 → 选中后启用」**
//   · 原版 `ChooseCardMenu.Setup` → `set_interactable(chooseButton, **0**)`
//     （`d:/2/tools/decomp_full/ChooseCardMenu__Setup.c:58`，同处 `:56` 先 `SetActive(true)`）；
//     `ChooseCardMenu.SelectCard` → `set_interactable(chooseButton, **1**)`（`ChooseCardMenu__SelectCard.c:29`）。
//     全类只有这两处写它 ⇒ **本场不会退回禁用**（单选单向、没有取消；换选仍是 `set_interactable(1)`）。
//   · `chooseButton` = prefab 的 **`ChooseCardMenu/ButtonsGroup/Continue Button`**（GO 名逐字；
//     `EverguildButton`，序列化 `m_Interactable = 1`）—— `+0x30` 的指认按字段序现核，
//     全文 → `_confirmInteractable` 那条 doc（字段起点 `0x20` 由 `+0x38` 的 `Localize.set_Term` 定死）。
//   · 空选点确认：`ClickChooseDone` 首句 `selectedCard == null ⇒ return`（`…ClickChooseDone.c:13-18`）。
//   · 落地 = `_confirmInteractable` + `SetConfirmInteractable`：手搓路（`HandleClick`）**吃掉但不提交**、
//     uGUI 路（`OnUguiDone`）**不计数不派事件**、悬停反馈也不给（原版 `!IsInteractable` 不进
//     `Highlighted/Pressed`）。`Multi`（换牌那一族的样子）**不变** —— 原版那扇恒可交互
//     （`Mulligan*.c` 里 `set_interactable` 零命中）。
//   · ⚠️ **只做了行为那一半**：原版禁用时那四颗 `EverguildButtonStateFollower` 会换灰度材质
//     （`get_DisabledMaterial` / `DoMaterialRefresh`），我们没有那支 shader ⇒ 见报告「遗留」。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

        /// <summary>🔴 **2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e1）：卡下那颗 `Select` 钮的
        /// 【原版节点 rect】（设计 px）—— 判据是 prefab 现读，不是推的**。
        /// <para>判据链（`bundle_battlesharedresources_assets_all`，逐文件现读）：
        /// ① `CardChooseCardButtonFrame` 的 `selectButton`（`MB 7927933141705382700`）
        ///    → `-5173003275250738388`（`EverguildButton`），同 GO 的
        ///    `RectTransform_-9013717219094471892.m_SizeDelta` = **1.086300015449524 × 0.42239999771118164**
        ///    （**卡本地单位**，`m_LocalScale = 1`、中心锚、`m_Pivot = (.5,.5)`）；
        /// ② 换算尺 `CardsHorizontalLayout.Scale` = **183.41 px / 卡本地单位**
        ///    （`CardsHorizontalLayout__get_Scale.c` 读 `+0x78`；同一条尺在 `GetPosition.c` 里是
        ///    `spacing = lossyScale.x × 字段 +0x80` —— 我们那张实测表 `SpacingPx(n≤3) = 502.2`
        ///    ⇒ `+0x80 = 2.74`、`2.74 × 183.41 = 502.5` ⇒ 这把尺就是 px/卡本地单位）；
        /// ③ ⇒ **1.0863 × 183.41 = 199.24 px** 宽、**0.4224 × 183.41 = 77.47 px** 高。</para>
        /// <para>🔴 **改前错在哪**：我们走的是 `ImgByHeight(..., 77f)` = 「高 77、宽按贴图比例」，
        /// 而 `UI_Button_Mulligan` 是 **410×124** ⇒ 宽 = `77 × 410/124 = 254.6` —— 那是**贴图的原始比例**，
        /// ⛔ **不是**原版节点 rect（宽了 **27.8%**）。原版那颗 `Image`（`MB 4178237357239393068`）
        /// 的 `m_PreserveAspect = **0**` ⇒ **拉伸铺满 rect**，所以这里必须显式给宽高。
        /// </para>
        /// <para>⚠️ 实绘与命中用的是**同一块**（`_cardBtns[i]` 既是画出来的件、又是 `ButtonIndexAt`
        /// 量 `Contains` 的那一件）⇒ 两套判据不分叉；改这一次两边一起变。</para></summary>
        const float CardBtnW = 199.24f, CardBtnH = 77.47f;

        /// <summary>🔴 **2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e1c）：卡钮中心在【卡心下方】的
        /// 距离（设计 px）= **322.618** —— 按 prefab 的**节点 rect** 改，⛔ 不是布局间距。
        /// <para>判据（`bundle_battlesharedresources_assets_all`，逐组件现读）：
        /// `CardChooseCardButtonFrame` 的 `Select` 钮
        /// `RectTransform_-9013717219094471892.m_AnchoredPosition.y = **−1.7589999437332153**`
        /// （卡本地单位；`m_LocalScale = 1` · 中心锚 · `m_Pivot = (.5,.5)`）⇒ `× 183.41`（同 `CardBtnW` 那把尺）
        /// = **−322.6182 px**。父 `CardChooseCardButtonFrame` 自己的 RT（`-8126607206803678420`）=
        /// `2.3463 × 3.3198` · `m_AnchoredPosition = (−5.1501, **0**)` · 中心锚 + 中心 pivot ⇒
        /// **父中心就是卡心**，所以这一条就是「钮心相对卡心」的 y。</para>
        /// <para>⚠️ 改前写死 `323f`（2026-09-14 估算「≈323」）⇒ 差 **0.382 px**；本批按 prefab 改，
        /// 卡行 / 间距（`SpacingPx` / `CardRowCy`）**一个字没动**。
        /// ⛔ **别拿这一条去套换牌面板**：那一颗是另一个 prefab（`CardMulliganButtonFrame` /
        /// `Change Card Button`，`ap.y = −1.7513`、尺寸 `1.30 × 0.33`、框 `2.3335 × 4.167`）
        /// ⇒ `MulliganPanel.BtnBelowCardPx` 那一族是**另一套数**。</para></summary>
        const float CardBtnBelowCardPx = 322.618f;

        /// <summary>补间/自检用：面板的 z。和日志面板一样推到 −0.9 一带 ——
        /// 场上的粒子在 −0.5 那层会飘到面板上面（那个坑记在 `BattleLogPanel` 的注释里）</summary>
        protected const float Z = -0.9f;

        /// <summary>整屏压暗的不透明度 = **原版 `Shade.onAlphaLevel`**（`battlearena2/FrontCanvas/ShadePanel`
        /// 的 `MonoBehaviour_4598.json`：`"onAlphaLevel": 0.800000011920929` · `"defaultTimeToSwitch": 0.5`）。
        /// <para>🔴 **2026-10-25（审查代理 `AU2`）**：原来这里是**行内字面量 `0.55f`**、且注释写着
        /// 「颜色/透明度没查到 ⇒ 我们挑的」。**判据其实一直在**（同一份值 `MulliganPanel.ShadeAlpha` 2026-10-22
        /// 就落地了）—— 选牌与换牌在原版**共用同一个 `ShadePanel`**（`BattleManager.shade`，`+0x230`；
        /// 调用点 `ChooseCardMenu__ShowChooseCardElements.c` / `MulliganManager__ShowMulliganElements.c:16`）
        /// ⇒ 两处**必须同值**。⚠️ 3 形参重载 ⇒ `useAnim=false` ⇒ **硬切**，所以本件不做淡入。</para>
        /// <para>底图是 `Image.m_Color = (0,0,0,0)` + `m_Sprite = null`、`m_RaycastTarget = 0`
        /// ⇒ **黑 + 只改 α + 不吃射线**（我们这颗是世界 quad，本来也不进输入链 ✓）。</para>
        /// <para>🔴 **2026-10-25（`M1` 追加单 · 主对话裁定「做」）：本件【不再自己写死 0.8】**。
        /// 原版那 0.8 的语义是「**显示空间**透过率 0.2」，而**原版工程是 Gamma、我们是 Linear** ——
        /// 世界 quad 在线性光里混 ⇒ 同一个 0.8 在我们这里显示空间只剩 ≈0.48（背景亮一倍）。
        /// 换牌面板（`MulliganPanel.ShadeAlpha`）2026-10-25 已按色彩空间换算（Linear 下 = 0.945），
        /// **选牌与换牌在原版就是同一个 `ShadePanel`** ⇒ 这里**直接转发同一个值**，
        /// ⛔ 别在选牌这边抄第二份数（抄了就会一边对一边错，而且没人发现）。
        /// 推导 / 残差表 / 切 Gamma 后怎么回 0.8 → `MulliganPanel.ShadeAlphaOriginalGamma` 那条 doc；
        /// 判据与实拍 → `资料/普查产出_1025/M1_换牌背景亮度.md`。</para></summary>
        public static float ShadeAlpha { get { return MulliganPanel.ShadeAlpha; } }

        /// <summary>压暗层的**渲染队列** = **3006** · 候选卡的 = **3007** —— 直接复用
        /// `MulliganPanel.QMulliganShade` / `QMulliganCard`（**判据只此一处**，⛔ 别在这里写第二个 3006）。
        ///
        /// <para>🔴 **2026-10-25（审查代理 `AU2`）：这两格是本件补的 —— 改前两颗都在默认队列 3000。**
        /// 病灶（与 `M1`/`D6` 那次**同一族**）：压暗层是**世界空间 `ImageQuad`**、候选卡是**世界空间
        /// `MeshRenderer`**，两者**队列相同（3000）、z 也相同（`Z = −0.9`）** ⇒ 先后只剩「到相机的距离」
        /// 这一个含糊的次序（压暗层中心 = 屏心，比任何一张卡都更靠近相机 ⇒ 实测语义上**压在卡上面**），
        /// 而战场几何里队列 **3001/3003/3004/3005** 的那 18 件又会反过来盖住 3000 的卡
        /// （队列优先于 z）。</para>
        /// <para>**原版怎么保证的**：`ShadePanel` 与 `ChooseCardMenu` 都是 `FrontCanvas` 的子节点，
        /// 而 `ShadePanel` 是**更早的兄弟**（`FrontCanvas` 的 `m_Children` 里 `ShadePanel` 在
        /// `Safe area FrontCanvas` 之前）⇒ 压暗**先画**、面板与卡**后画** ⇒ **卡永不被压暗**。
        /// 我们两颗都在世界空间 ⇒ 用队列把这条「先画/后画」显式写死：
        /// 压暗 **3006**（高于战场全部几何，见 `MulliganPanel.QMulliganShade` 的逐场计数）
        /// ＜ 卡 **3007**（＜ `CardFan.QCardBase` 3009 ⇒ 「点卡放大」那扇窗照样压在卡上）。</para></summary>
        public const int QChoiceShade = MulliganPanel.QMulliganShade;   // 3006
        /// <summary>见 <see cref="QChoiceShade"/>（同一对常量，必须成对改）。</summary>
        public const int QChoiceCard = MulliganPanel.QMulliganCard;     // 3007

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

        // ── 🆕 2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e）：uGUI 输入层 ──────────────────────
        //  判据 → 文件头「输入层换 uGUI」那一段（原版逐组件现读）。
        //  ⛔ 这一层**只发事件**，不做动作（见文件头最后一条：两条路同时动状态 = 一次点击翻两次）。
        HudPiece _doneParentHit;          // 原版父 `Continue Button`（alpha=0 的透明命中面，画在 bar/play 之下）
        BattleHudButton _btnBar, _btnPlay, _btnDoneParent, _btnEye;
        readonly List<BattleHudButton> _btnCardButtons = new List<BattleHudButton>();
        int _uguiCardClicks, _uguiDoneClicks, _uguiEyeClicks;   // 灭自证计数
        int _lastUguiCard = -1;

        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：uGUI 那条路上「第 `i` 张卡下的 `Select` 钮被点了」
        /// （松手那一帧；原版 `EverguildButton` ⇒ `Button.onClick`）。
        /// <para>⚠️ 订阅者要做的是 `TogglePick(i)`（**本轮没有订阅者** —— 白名单只到本文件；
        /// 手搓路 `HandleClick` 仍是唯一动作方）。合并配方 → 报告 §三。</para></summary>
        public event Action<int> CardButtonClicked;
        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：uGUI 那条路上「确认被点了」（原版三条可射线 Image 的并集）。
        /// ⚠️ 订阅者要做的是 `Picked` → `OnDone(picks)`；本轮没有订阅者（同上）。</summary>
        public event Action DoneClicked;
        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：uGUI 那条路上「眼睛被点了」
        /// （原版 `HideChooseButton` → `ToogleChooseMenuVisibility`）。⚠️ 订阅者要做的是 `ToggleEye()`。</summary>
        public event Action EyeClicked;

        // 🆕 2026-10-19（B4 · `N70` P0-2）：选牌/选择面板的按钮反馈（判据与换牌面板同族：
        // `Continue Button` t=1 ColorTint · `HideChooseButton` t=2 eye hover/pressed · 卡钮 t=2 Mulligan 图）。
        BattleButtonFx _fxBar, _fxEye;
        readonly List<BattleButtonFx> _fxCardBtns = new List<BattleButtonFx>();
        readonly List<Label> _cardTexts = new List<Label>();
        readonly List<CardView> _cards = new List<CardView>();
        readonly List<int> _picked = new List<int>();

        /// <summary>🔴 **2026-10-20（W57 · 战斗线 `T09`）：确认钮此刻可不可交互** —— 原版
        /// `ChooseCardMenu.chooseButton.interactable`（那颗 `EverguildButton`；prefab 是
        /// `ChooseCardMenu/ButtonsGroup/Continue Button`，本 GO 名逐字 = **`Continue Button`**，
        /// 序列化 `m_Interactable = 1`）。
        /// <para>判据（逐句现读反编译 + prefab 字段）：
        /// · 开面板 = `ChooseCardMenu.Setup` ⇒ `set_interactable(chooseButton, **0**)`
        ///   （`d:/2/tools/decomp_full/ChooseCardMenu__Setup.c:58`；同处 `:56` 先 `SetActive(true)`）；
        /// · 选中一张卡 = `ChooseCardMenu.SelectCard` ⇒ `set_interactable(chooseButton, **1**)`
        ///   （`ChooseCardMenu__SelectCard.c:29`）；
        /// · **全类只有这两处写它**（`grep -n set_interactable ChooseCardMenu*.c` ⇒ 2 命中）
        ///   ⇒ 「开面板禁用 → 选中第一张启用 → 本场不再退回禁用」（单选单向、没有取消）。
        /// · `+0x30` 的字段映射现核：同一方法里 `I2.Loc.Localize.set_Term` 落在 `+0x38`
        ///   （`Setup.c:48`）= 第 4 个序列化字段 `titleTextLocalize` ⇒ 字段起点 = `0x20`
        ///   ⇒ `+0x30` = 第 3 个 = `chooseButton`（`EverguildButton : Button : Selectable`，与
        ///   `set_interactable` 的类型自洽）；`OnEnable/OnDisable` 在 `+0x30` 上取 `+0x100`
        ///   那个 `UnityEvent` 加监听（= `Button.onClick`），同一指认。
        /// · 空选点确认：`ChooseCardMenu.ClickChooseDone` 第一句就是
        ///   `selectedCard == null ⇒ return`（`ChooseCardMenu__ClickChooseDone.c:13-18`）。
        /// </para>
        /// <para>⚠️ **`Multi` 档不受此约束**（原版 `ChooseCardMenu` 只有单选这一档；换牌那一扇
        /// `MulliganPanel` 自己实现、不走本类，而且 `Mulligan*` 反编译里**一处 `set_interactable` 都没有**
        /// —— 换牌确认钮本来就恒可交互）⇒ `Multi` 保持旧行为「恒可交互」。</para></summary>
        bool _confirmInteractable = true;

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
        /// （当前实际调用点只有两处：`U(1200f)` = 压暗层、`U(SpacingPx(n))` = 卡片间距，
        /// 都是为了把 px 变成**卡片**要的世界单位；⚠️ 2026-10-20 现核 —— 旧注释里那处 `U(323f)` **早已不存在**，
        /// 卡钮现在直接在画布上给设计 px）。
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

            // 压暗（🔴 **2026-10-25（审查代理 `AU2` · 铁律 5）**：这里原文写「**我们挑的**：原版只查到有个
            //    `Shade.SwitchShade`，颜色/透明度没查到」—— **不成立，两样都有判据**：
            //      · `battlearena2` 的 `FrontCanvas/ShadePanel` 那是**同一个**压暗件（`BattleManager.shade`，偏移 `+0x230`），
            //        选牌与换牌**共用它**（`ChooseCardMenu__ShowChooseCardElements.c` 末尾那句
            //        `Shade__SwitchShade(BattleManager.shade, param_2, 0, 0)`；换牌那侧同一条 →
            //        `MulliganManager__ShowMulliganElements.c:16`）；
            //      · 它的 `Shade` 组件（`MonoBehaviour_4598.json`）：`onAlphaLevel = **0.8**` · `defaultTimeToSwitch = 0.5`；
            //        底下那颗 `Image`（`MonoBehaviour_4413.json`）：`m_Color = (0,0,0,**0**)`（**黑，α 由组件写**）、
            //        `m_Sprite = null`、`m_RaycastTarget = **0**`（⇒ **不吃射线**，正好与「我们这颗世界 quad 本来就
            //        不参与输入」一致）；
            //      · 调用是 **3 形参重载** ⇒ `useAnim = false` ⇒ **硬切**（没有淡入），α 取 `onAlphaLevel`。
            //    ⇒ 我们这颗取 `ShadeAlpha`（原来是 0.55 —— 那才是「我们挑的」）。判据与「换牌那层同源」的落地
            //      见 `MulliganPanel.ShadeAlpha`（同一份 `onAlphaLevel`）。
            //    🔴 **2026-10-25（`M1` 追加单）**：`ShadeAlpha` 已改成**转发** `MulliganPanel.ShadeAlpha`
            //      —— 那个值按工程的色彩空间换算过（原版 γ 口径 0.8 / 我们 Linear = 0.945；
            //      同一个 0.8 在我们这里是「线性 ×0.2」，换成显示空间只剩 ≈0.48 ⇒ 背景亮一倍）。
            //      ⛔ 别在本件再出现第二个 α 字面量。
            // 🔴 **2026-10-16（`T09` 批 3）：这一件【有意留在世界空间】** —— ⛔ 别顺手迁上去，理由三条
            //    写在文件头（核心：它必须画在**卡片后面**，而卡片是 `MeshRenderer`、不归画布管，
            //    画布上的件一律画在卡片**上面**）。`MultiCardDisplay.mcd_mask` 是同一条。
            _shade = ImageQuad.Create(root, CardArt.Solid(), At(960f, 540f), U(1200f),
                                      new Vector2(0.5f, 0.5f), "ChoiceShade");
            if (_shade != null)
            {
                _shade.SetAspect(1920f / 1080f);
                _shade.SetTint(new Color(0f, 0f, 0f, ShadeAlpha));
                _shade.SetRenderQueue(QChoiceShade);   // 🔴 2026-10-25：压在战场之上、候选卡之下（见 QChoiceShade）
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

            // 🆕 **2026-10-20（`T09` 步骤 A3-e）**：装 uGUI 输入层（确认三条并集 + 眼睛）。
            //    ⚠️ 必须排在 `SetVisible(false)` **之前** —— 那一句会把这一组的件全部关掉（含新建的透明父件）。
            //    每张卡下那颗钮在 `LayOutCards` 里逐张装（它们是每次 `Open` 重建的）。
            BuildUguiInput();

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

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🔴 **2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e）：uGUI 输入层**
        //  判据逐条 → 文件头「输入层换 uGUI」（`MB_4413/5075/4921` 都是 `m_RaycastTarget=1` + pad 0；
        //  卡钮 = `Generic Simplified UI Button_updated` 的 `Image` rt=1/pad 0 + `EverguildButton`）。
        //  ⛔ **只发事件，不做动作**（两条路同时动状态 = 一次点击翻两次 ⇒ 看起来「点了没反应」）。
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>把「确认三条 + 眼睛」变成射线靶（`Build()` 最后调一次）。
        /// <para>⚠️ 子画布没立起来（`_panel == null`）⇒ 图与字那一批本来就没建（`Build` 已出声）⇒ 整段跳过。</para></summary>
        void BuildUguiInput()
        {
            if (_panel == null) return;

            // ②③ 的**节点 rect / PA** 先对齐原版（⚠️ 这是**命中那一侧**，⛔ 与实绘尺寸不是同一个数）：
            //   · 圆钮：`RT_3324` = **80.468689 × 79.635559**、`MB_5075.PA = 0`。
            //     我们原来按贴图比例给的是 **79.64²**（既有 1% 偏离，文件头 `PlayH` 那条记着「改它要动实绘、
            //     另开一件」）⇒ 本波**只改 rect**（uGUI 的射线吃 rect、不吃贴图），`preserveAspect = true`
            //     让**实绘仍是 ~79.64²**（方图内接进 80.469×79.636 ⇒ 79.636²，与改前差 0.004 px）。
            //   · 眼睛：`RT_3051` = **78.44²**、`MB_4921.PA = **1**`。我们的 rect 已经是 78.44²（256² 方图）
            //     ⇒ 只把 `preserveAspect` 补成原版那一个（方图 + 方框，实绘逐位不变）。
            if (_play != null && !_play.SetNodeRect(PlayW, PlayH, true))
                Debug.LogError("[选卡面板] 圆钮的**原版节点 rect** 没写成（80.469×79.636）⇒ uGUI 命中会比原版窄 0.83 px");
            if (_eye != null && !_eye.SetNodeRect(EyePx, EyePx, true))
                Debug.LogError("[选卡面板] 眼睛的**原版节点 rect** 没写成（78.44²）⇒ uGUI 命中与 `HitEye` 分叉");

            // ① 原版父 `Continue Button`：`alpha=0`、`sprite=null`、**rt=1**、pad (0,0,0,0)
            //    判据 = `RT_3196` / `MB_4413`（`bundle_scenes_scenes_battlearena1` 与 arena2 逐值相同）。
            //    它是「确认」可点区里**最大的那一块**（548.015 × 75.905），但**不画**。
            //    ⚠️ 兄弟序取 `Z + 0.02`（**比 bar/play 的 `Z` 大** = 排在它们**之后** = 画在**底下**）——
            //    原版这一个是 `BG` / 圆钮的**父件**（子件画在父件之上）；两者重叠时谁收射线都
            //    发同一个 `DoneClicked`，这个先后只为与原版同构（`HudCanvas.Group.Add`：z 大的排前面）。
            float pcx = Lo.BarCx + DoneParentDx, pcy = Lo.BarCy + DoneParentDy;
            _doneParentHit = HudCanvas.MakeImageInParent(_panel, CardArt.Solid(), "ContinueButtonHit",
                                                         pcx - 960f, pcy - 540f,
                                                         DoneParentW, DoneParentH, Z + 0.02f,
                                                         new Color(1f, 1f, 1f, 0f));
            if (_doneParentHit == null)
                Debug.LogError("[选卡面板] 原版父 `Continue Button`（透明命中面）没建起来"
                             + "（`CardArt.Solid()` 取不到？）⇒ **确认区少了 548×75.9 那一块**");

            _btnDoneParent = AttachChoiceButton(_doneParentHit, "Continue Button", OnUguiDone);
            _btnBar = AttachChoiceButton(_bar, "BG", OnUguiDone);
            _btnPlay = AttachChoiceButton(_play, "ContinueButton", OnUguiDone);
            _btnEye = AttachChoiceButton(_eye, "HideChooseButton", OnUguiEye);
        }

        /// <summary>把一件 uGUI 图挂成 `BattleHudButton`，并把 `raycastPadding` 写成**原版那个值**（全 0）。
        /// <para>⚠️ 件是 null（贴图缺失）⇒ 出声并返回 null —— ⛔ 不静默（那会让钮「看得见但点不动」）。</para>
        /// <para>⚠️ `m_RaycastPadding = (0,0,0,0)` 是**原版四个可射线件共同的实测值**
        /// （`MB_4413` 父 Continue Button · `MB_5075` BG · `MB_4921` 眼睛）；这里显式写一遍
        /// 是为了让「命中区 = 件 rect + 各自 padding」这条口径**在代码里看得见**（默认值也是 0）。</para></summary>
        BattleHudButton AttachChoiceButton(HudPiece piece, string originalNode, Action act)
        {
            if (piece == null)
            {
                Debug.LogError("[选卡面板] `" + originalNode + "` 那一件没建出来 ⇒ **它不参与 uGUI 命中**");
                return null;
            }
            var b = BattleHudButton.Attach(piece, act);
            if (b == null)
            {
                Debug.LogError("[选卡面板] `" + originalNode + "` 挂不上点击适配器"
                             + "（见 `BattleHudButton.Attach` 自己那条日志）⇒ **它在 uGUI 上点不动**");
                return null;
            }
            if (!piece.SetRaycastPadding(0f, 0f, 0f, 0f))
                Debug.LogError("[选卡面板] `" + originalNode + "` 的 `raycastPadding` 没写成（件不是 uGUI？）");
            return b;
        }

        /// <summary>uGUI 那条路上「确认」被点了（**松手**）。⛔ **不调 `OnDone`** —— 见文件头最后一条。
        /// <para>🔴 **2026-10-20（W57 · `T09`）**：确认钮不可交互时**连计数都不加、不派事件** ——
        /// 原版 `Button.OnPointerClick` 在 `!IsActive() || !IsInteractable()` 时直接 `return`
        /// （本机 `com.unity.ugui/Runtime/UGUI/UI/Core/Button.cs` 的 `Press()`）。我们这颗
        /// `BattleHudButton` 不是 `Selectable`、没有 `interactable` 字段 ⇒ 闸设在这一层。</para></summary>
        void OnUguiDone()
        {
            if (!Visible || !_confirmInteractable) return;
            _uguiDoneClicks++;
            if (DoneClicked != null) DoneClicked();
        }

        /// <summary>键盘确认：**空格 / 回车 = 点「继续」**。
        /// <para>**判据（第一权威）**：`ChooseCardMenu__Update.c` ——
        /// <c>if (Input.GetKeyDown(0x20 /*Space*/) || Input.GetKeyDown(0xd /*Return*/)) {
        ///      if (buttonsGroup.activeSelf) ClickChooseDone(); }</c>
        /// 而 `ClickChooseDone` 自己**只在一张都没选时什么都不做**（`ChooseCardMenu__ClickChooseDone.c:3-6`：
        /// `if (selectedCard != null) … ProcessChooseDone()`）⇒ 等价于我们这里先过
        /// `_confirmInteractable`（= 原版 `Selectable.interactable`，只在选中后才为真）。</para>
        /// <para>🔴 **2026-10-25（执行代理 `B` · 完全复刻第 1 轮）**：原来这台机器上**没有**键盘确认
        /// （`grep KeyCode` 全类 0 命中）⇒ 原版这条输入路径缺失。落在本类 `Update` 里是照原版
        /// （原版也写在 `ChooseCardMenu.Update`），⛔ 不要在 `BattleDriver` 再写第二份。</para>
        /// <para>⚠️ 换牌那扇**不走本类**（它自己实现，键盘在 `BattleDriver.HandleMulligan` 里）——
        /// 那边判据同为「空格/回车 = 完成」，但门控看面板可见性，见那段注释。</para></summary>
        void Update()
        {
            if (!Visible) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.spaceKey.wasPressedThisFrame && !kb.enterKey.wasPressedThisFrame) return;
            OnUguiDone();     // 内含 Visible / interactable 闸；未选牌时**什么都不做**（同原版）
        }

        /// <summary>uGUI 那条路上「眼睛」被点了。⛔ **不调 `ToggleEye`**（同上）。</summary>
        void OnUguiEye()
        {
            if (!Visible) return;
            _uguiEyeClicks++;
            if (EyeClicked != null) EyeClicked();
        }

        /// <summary>uGUI 那条路上「第 `i` 张卡下的 `Select` 钮」被点了。⛔ **不调 `TogglePick`**（同上）。</summary>
        void OnUguiCardButton(int i)
        {
            if (!Visible) return;
            _uguiCardClicks++;
            _lastUguiCard = i;
            if (CardButtonClicked != null) CardButtonClicked(i);
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
        //  🆕 2026-10-25（`E` · 账 `战斗界面_HUD` 366/369/375）：**选牌面板的倒计时**
        // ==================================================================
        //  判据（🥇 ILSpy `ChooseCardMenu.cs`，逐句）：
        //   · `Setup(cards, actingCardId, timerCounter, target)`（`:559`）：`timerCounter` = **ClockManager 的
        //     倒计时剩余秒** —— 调用点 `BattleManager.cs:25350` 传的是 `clockManager.counter`（环境卡那一支
        //     `BattleManager.cs:12811` 传 **0** = 无倒计时）。`timerCounter <= 1` ⇒ 起 `EndSelectionWithDelay`
        //     （`:651`；协程体 `:108-155`）：**先等 0.5 s** → 没选就随机选一张 → 写 `0:{ss:00}` →
        //     `timerCounter < 3` ⇒ `ProcessChooseDone()`。
        //   · `CountdownTrigger(counter)`（`:797`）：倒计时每走一秒由 `BattleManager.CountdownTrigger` 转发 ——
        //     **没选就随机选一张**（`UnityEngine.Random.Range(0, size)`）→ 确认钮文案 `$"0:{counter:00}"` →
        //     `counter < 3` ⇒ `ProcessChooseDone()`。
        //   · `SetTimer(secondsLeftCounter)`（`:1224`）：**只**写那句文案（同一个格式串）。
        //  ⚠️ 我们没有协程（批处理无帧循环）⇒ 0.5 s 那一档改成「驱动每帧喂 `TickOpenDelay`」，
        //     语义不变（**只在 `secondsLeft <= 1` 时才起作用**，同原版那道 `timerCounter <= 1` 闸）。
        //  ⚠️ **随机选是 UI 侧的 `UnityEngine.Random`**（原版就是）—— ⛔ 别改成引擎那套 `System.Random(seed)`：
        //     那不是同一条链（引擎那套管的是对局可复现，这条是「玩家超时、UI 替他挑」）。
        //  ⚠️ 这三个方法**不含驱动接线**：谁在什么时候喂秒数由驱动定（`BattleDriver.TickClock` 那一段，
        //     A 域）；本件只保证「喂进来的秒数怎么表现」与原版逐句一致。
        int _timerLeft = -1;
        bool _openDelayDone;
        float _openedAt;

        /// <summary>自检用：最近一次喂进来的倒计时秒数（`-1` = 还没喂过）。</summary>
        public int TimerLeftForTest { get { return _timerLeft; } }

        /// <summary>自检用：确认钮**这一刻**显示的字（`SetTimer(7)` 之后应形如 `0:07`；关面板后还原成真文案）。</summary>
        public string TimerTextForTest { get { return _confirm != null ? _confirm.Text : null; } }

        /// <summary>原版 `ChooseCardMenu.SetTimer(secondsLeftCounter)`：确认钮文案 `0:{ss:00}`。</summary>
        public void SetTimer(int secondsLeft)
        {
            _timerLeft = secondsLeft;
            if (_confirm != null) _confirm.SetText("0:" + Mathf.Max(0, secondsLeft).ToString("00"));
        }

        /// <summary>原版 `ChooseCardMenu.CountdownTrigger(counter)`：没选就随机选一张 → 更新文案 →
        /// `counter &lt; 3` ⇒ 直接确认。**每倒计时一秒调一次**（驱动侧）。
        /// <para>返回 true = 这一下**触发了自动确认**（`OnDone` 已被调）。</para></summary>
        public bool CountdownTrigger(int secondsLeft)
        {
            if (!Visible) return false;
            if (_picked.Count == 0 && _cards.Count > 0)
                TogglePick(UnityEngine.Random.Range(0, _cards.Count));   // 原版：Random.Range(0, _chooseCardUis._size)
            SetTimer(secondsLeft);
            if (secondsLeft < 3) return ConfirmNow();
            return false;
        }

        /// <summary>原版 `EndSelectionWithDelay(timerCounter)` 的 0.5 s 延迟那一档
        /// （只在**开面板时** `timerCounter <= 1` 才起）。驱动每帧喂一次即可；`secondsLeft > 1` ⇒ 什么都不做。
        /// <para>返回 true = 这一下**触发了自动确认**。</para></summary>
        public bool TickOpenDelay(int secondsLeft)
        {
            if (!Visible || _openDelayDone || secondsLeft > 1) return false;
            if (Time.timeSinceLevelLoad - _openedAt < 0.5f) return false;
            _openDelayDone = true;
            return CountdownTrigger(secondsLeft);
        }

        /// <summary>等价于**玩家按了确认**（原版 `ProcessChooseDone` 的出口）：走同一个 `OnDone` 回调。
        /// ⛔ 不绕开 `OnDone` 去直接改引擎 —— 那条链（`BattleDriver.OnChooseDone`）才是唯一的提交口。
        /// 返回 false = 没有可提交的东西（面板没开 / 一张都没选 / 没人订阅）。</summary>
        public bool ConfirmNow()
        {
            if (!Visible || _picked.Count == 0) return false;
            var cb = OnDone;
            if (cb == null) return false;
            cb(Picked);
            return true;
        }

        // ==================================================================
        //  开 / 关
        // ==================================================================

        public void Open(IReadOnlyList<CardView> cards, string title)
        {
            _picked.Clear();
            // 🆕 2026-10-25（`E`）：倒计时那几个状态每开一次都复位（原版 `Setup` 是每次重新算）
            _openedAt = Time.timeSinceLevelLoad;   // 0.5 s 延迟那一档从**这一刻**起算
            _openDelayDone = false;
            _timerLeft = -1;
            if (!string.IsNullOrEmpty(title)) SetTexts(title, _confirmText);
            LayOutCards(cards);
            // 🔴 **2026-10-20（W57 · `T09`）**：原版**每次** `Setup` 都把确认钮置
            //    `interactable = false`（判据 → `_confirmInteractable` 那条 doc）⇒ 每次开面板
            //    都回到「没选牌时点不了继续」。**0 张候选是原版没有的档**（那边 `Setup` 总会带
            //    至少一张）—— 这时保持可用 = 旧行为，⛔ 别让面板永远关不掉（防卡死的保底，
            //    **不是**原版判据）。
            SetConfirmInteractable(Mode != PickMode.Single || _cards.Count == 0);
            SetVisible(true);
        }

        /// <summary>改确认钮的可交互状态（原版 `Selectable.set_interactable`）。
        /// <para>🔴 **2026-10-20（W57 · `T09`）**：只在**两处**调 —— `Open`（置 `false`）与
        /// `TogglePick` 首次选中（置 `true`）。⛔ 别在 `Close` / 眼睛开合 / 换选里再改：
        /// 原版 `ChooseCardMenu` 全类只有 `Setup` 与 `SelectCard` 两处写它。</para>
        /// <para>⚠️ **视觉上的「灰掉」这一波没做**：原版 `Continue Button` 子树四颗件
        /// （父 / `BG` / `ContinueText` / `ContinueButton`）各挂一颗 `EverguildButtonStateFollower`
        /// （`m_Script -9096086197743689236`，无序列化字段），`interactable = false` 时它会
        /// 换 `EverguildButtonHelper.get_DisabledMaterial` 造出来的**灰度材质**
        /// （反编译 `EverguildButtonStateFollower__Disabled.c` → `DoMaterialRefresh`；
        ///  `EverguildButtonHelper__get_DisabledMaterial.c` 自己 `Shader.Find` 再造 Material）。
        /// 我们这边没有那支 shader/材质、也不在白名单 ⇒ 只关「行为」那一半，
        /// 灰阶观感记在报告「没查清/遗留」里（⛔ 不拿一个近似的乘色冒充原版）。</para></summary>
        void SetConfirmInteractable(bool on)
        {
            _confirmInteractable = on;
        }

        public void Close()
        {
            ClearCardDecor();
            SetVisible(false);
        }

        void SetVisible(bool v)
        {
            Visible = v;
            // 🆕 2026-10-25（`E`）：关面板时把确认钮文案**还原**成真文案（否则下次开面板会先露出
            //   上一次的 `0:02`，直到下一次 `SetTimer`）—— 原版每次 `Setup` 都重写那颗字，等价。
            if (!v && _confirm != null) _confirm.SetText(_confirmText);
            // ⚠️ `_shade` 是 `ImageQuad`（世界档那一种）、其余是 `HudPiece` —— 两种的 `gameObject` 口同形，
            //    所以这一句**一个字没改**（⛔ 不必因为迁了半批就把它们分成两行）。
            if (_shade != null) _shade.gameObject.SetActive(v);
            // 🆕 2026-10-20（`T09` A3-e）：`_doneParentHit`（原版父 `Continue Button` 的透明命中面）
            //    也是这一组的件 ⇒ 跟着一起显隐（关着时 uGUI 收不到射线 = 原版 `m_IsActive = false`）。
            foreach (var q in new[] { _bar, _play, _eye, _doneParentHit })
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
                // 🔴 **2026-10-25（审查代理 `AU2`）：候选卡的渲染队列显式抬到 3007**
                //   —— 与压暗层 `QChoiceShade`(3006) **成对**（只改一边 ⇒ 回到「卡被压暗」或「压暗被卡盖住」）。
                //   同时把战场里 3001/3003/3004/3005 那 18 件几何甩在下面（队列优先于 z）。
                //   ⚠️ 这几张卡由调用方在收场时 `Kill` 掉（`ClearChooseViews` / `ClearEnvPickViews`）
                //      ⇒ **不需要还原**（换牌面板那边是复用手牌卡，所以它才要还队列）。
                c.SetQueue(QChoiceCard);
                c.SetHighlight(CardHighlightState.Normal);

                if (_panel == null) continue;      // 子画布没立起来那一支：卡照摆、装饰不建（已出声）

                // ⚠️ 按钮位置：原版那个 prefab `CardChooseCardButtonFrame` 的 `Select` 钮在
                //    **卡中心下方 322.618 px**（`m_AnchoredPosition.y = −1.7589999437332153 × 183.41`；
                //    判据全文 → `CardBtnBelowCardPx` 那条 doc）。
                //    2026-09-14 第一版写成「卡高 ×0.30」≈183 px ⇒ **压在卡面的文字上**（截图才发现）。
                //    🔴 **2026-10-20（W57 · `T09` A3-e1c）**：原来取整写死 `323f`（多 0.382 px），
                //    现按 prefab **节点 rect** 改到 `CardBtnBelowCardPx`；⛔ 布局算法一字未动。
                float btnCyPx = Lo.CardRowCy + CardBtnBelowCardPx;
                // 🔴 **2026-10-20（W57 · `T09` A3-e1）：尺寸按原版节点 rect**（199.24 × 77.47），
                //    ⛔ 不再是 `ImgByHeight(..., 77f)` —— 那是「按贴图比例定宽」= 254.6 那个错值的来源。
                //    判据全文 → `CardBtnW/CardBtnH` 那条 doc。
                var q = MakeImg(_panel, CardArt.Ui("UI_Button_Mulligan"), "ChoiceCardBtn_" + i,
                                cxPx, btnCyPx, CardBtnW, CardBtnH, Z - 0.02f);
                if (q != null) _cardBtns.Add(q);
                // 🆕 **2026-10-20（`T09` A3-e）**：每张卡下那颗钮 = 原版 `Generic Simplified UI Button_updated`
                //    （`Image.m_RaycastTarget = 1` · pad (0,0,0,0) · 组件 `EverguildButton` ⇒ **松手**）
                //    ⇒ 挂 `BattleHudButton`；**只发 `CardButtonClicked`**，⛔ 不在这里 `TogglePick`。
                //    ⚠️ `int idx = i`：C# 的 `for` 变量是**同一个变量**，闭包直接捕 `i` 会全部指到最后一张。
                int idx = i;
                _btnCardButtons.Add(AttachChoiceButton(q, "CardButton[" + i + "]", () => OnUguiCardButton(idx)));

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
                                             // 选中态露哪张图 → 唯一判据 `CardBtnArtName`（⛔ 别在这另写一份）
                                             CardArt.Ui(CardBtnArtName(i)), mh, mp)
                       : BattleButtonFx.Tint(_cardBtns[i], "ChoiceCardBtn_" + i + "(tint-fallback)");
                _fxCardBtns.Add(fx);
            }
        }

        /// <summary>每帧喂状态（驱动层 `HandleChoose` 里调）。
        /// 🔴 **2026-10-20（W57 · `T09`）**：确认钮不可交互时**不给悬停/按下反馈** ——
        /// 原版 `Selectable` 在 `interactable = false` 下根本不进 `Highlighted/Pressed` 状态
        /// （本机 `Selectable.DoStateTransition` 的分支）。⛔ 只遮确认那一路，眼睛与卡钮不动。</summary>
        public void TickFeedback(Vector3 world, bool held)
        {
            if (!Visible) { ResetFeedback(); return; }
            if (_fxBar == null) BuildFeedback();
            FeedFx(_fxBar, _confirmInteractable && HitDone(world), held);
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
            _btnCardButtons.Clear();   // 🆕 2026-10-20（`T09` A3-e）：与 `_cardBtns` 同生同灭
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

        /// <summary>选中/取消一张牌。**单选**模式下选新的会顶掉旧的（原版选牌就是这个语义）。
        /// <para>🔴 **2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e1）：`Single` 改成【单向】—— 再点同一张
        /// 什么都不做**（改前是真 toggle：再点 = 取消选中）。判据逐句现读：
        /// `SelectCardButtonFrame__SelectButtonOnClick.c` = `ChooseCardMenu.SelectCard(card)`
        /// → `Card2DController.ToggleHighlight(1)` → `UnityEngine.UI.Selectable.set_interactable(false)`
        /// —— 那颗钮**自己被置成不可交互** ⇒ 同一张的钮**收不到第二次点击**；
        /// 换选另一张时由 `ChooseCardUI.ToggleSelected(false)`（`set_interactable(!selected)`）
        /// 把上一颗重新启用（`ChooseCardMenu__SelectCard.c` 里那个 `foreach … if (card != selected)
        /// ToggleSelected(false)`）。⇒ 原版**没有**「再点一次取消」这条路。</para>
        /// <para>⚠️ 只改 `Single`：`Multi`（换牌那一族）本来就是「再点取消」，而且换牌那扇自己实现了、
        /// 不走本类（文件头那条 2026-10-16 订正）。</para></summary>
        public void TogglePick(int i)
        {
            if (i < 0 || i >= _cards.Count) return;
            if (_picked.Contains(i))
            {
                if (Mode == PickMode.Single) return;      // 单向：已经选中的那一张不再响应（见 doc）
                _picked.Remove(i);
            }
            else
            {
                if (Mode == PickMode.Single && _picked.Count > 0) _picked.RemoveAt(0);
                _picked.Add(i);
                // 🔴 **2026-10-20（W57 · `T09`）**：原版 `SelectCard` 选中一张就把确认钮启用
                //    （`ChooseCardMenu__SelectCard.c:29` 的 `set_interactable(chooseButton, 1)`）。
                //    **只加不撤** —— 原版没有「再点取消」，换选另一张也还是同一句 `set_interactable(1)`。
                if (Mode == PickMode.Single) SetConfirmInteractable(true);
            }
            ApplyPicks();
        }

        /// <summary>被选中的下标（**升序**）。</summary>
        public List<int> Picked { get { var l = new List<int>(_picked); l.Sort(); return l; } }
        public bool IsPicked(int i) { return _picked.Contains(i); }
        public int CardCount { get { return _cards.Count; } }

        /// <summary>第 `i` 张卡那颗钮该用哪张图 —— **唯一的判据**（`ApplyPicks` 与 `BuildFeedback` 都走它，
        /// ⛔ 别在两处各写一份）。
        /// <para>🔴 **2026-10-20（W57 · `T09` A3-e1d）：`Single` 选中之后露【Normal】图**（⛔ **不是** `_Pressed`）。
        /// 判据（逐句现读反编译，`d:/2/tools/decomp_full/`）：
        /// `SelectCardButtonFrame__SelectButtonOnClick.c` = `ChooseCardMenu.SelectCard(card)` →
        /// `Card2DController.ToggleHighlight(1)` → `UnityEngine.UI.Selectable.set_interactable(false)`
        /// （**那颗钮自己**）；而 prefab `Generic Simplified UI Button_updated` 的 `EverguildButton`
        /// （`MB -5173003275250738388`）是 `m_Transition = 2`（`SpriteSwap`）· `m_SpriteState.m_DisabledSprite = 0`
        /// （null）⇒ UGUI `Selectable.DoStateTransition(Disabled)` → `DoSpriteSwap(null)` →
        /// `image.overrideSprite = null`（本机 `com.unity.ugui/.../Selectable.cs:1094-1099`，2026-10-20 现读）
        /// ⇒ **回落到 Image 自己的 Normal**（`UI_Button_Mulligan`，PathID `3819842738995756391`，改选另一张时
        /// `ChooseCardUI.ToggleSelected(false)` 把上一颗 `set_interactable(true)` 恢复）。</para>
        /// <para>⇒ 我们的等价物 = **Normal 图 + `SetDisabled(true)`**（`Swap` 档禁用时显 `_nt`，
        /// 悬停/按下都不再换图 = 原版的不可交互）。</para>
        /// <para>⚠️ `Multi`（换牌那一族的样子；本类的 `Multi` 档）**没有** `interactable = false` 这条判据
        /// ⇒ 保持原样：选中 = `UI_Button_Mulligan_Pressed`。</para></summary>
        string CardBtnArtName(int i)
        {
            bool pressedLook = _picked.Contains(i) && Mode != PickMode.Single;
            return pressedLook ? "UI_Button_Mulligan_Pressed" : "UI_Button_Mulligan";
        }

        /// <summary>选中的样子：卡**置灰**（`Unplayable` 那一档）+ 卡钮换图。
        /// 🔴 **2026-10-20（W57 · `T09` A3-e1d）**：`Single` 选中后卡钮露 **Normal 图**（原版
        /// `interactable = false` + 无 `m_DisabledSprite` ⇒ 回落 Normal；判据 → `CardBtnArtName` 那条 doc）；
        /// `Multi`（换牌那一族的样子）仍是 `_Pressed`。</summary>
        void ApplyPicks()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c == null) continue;
                bool m = _picked.Contains(i);
                c.SetHighlight(m ? CardHighlightState.Unplayable : CardHighlightState.Normal);
                if (i < _cardBtns.Count && _cardBtns[i] != null)
                    _cardBtns[i].SetTexture(CardArt.Ui(CardBtnArtName(i)));
                if (i < _fxCardBtns.Count && _fxCardBtns[i] != null)
                {
                    _fxCardBtns[i].SetNormal(CardArt.Ui(CardBtnArtName(i)));
                    // 🔴 **2026-10-20（W57 · `T09` A3-e1）**：原版选中之后这颗钮 `interactable = false`
                    //    （判据 → `TogglePick` / `CardBtnArtName` 那两条 doc）—— 等价物 = 反馈进 **Disabled**：
                    //    悬停/按下都不再变化（`BattleButtonFx.Retarget` 里 Disabled 优先级最高）。
                    //    `Swap` 档禁用时显示 `_nt` = `CardBtnArtName(i)` 那张（`Single` 选中后即 Normal）。
                    _fxCardBtns[i].SetDisabled(m && Mode == PickMode.Single);
                }
            }
        }

        // ==================================================================
        //  交互（和设置面板/日志面板同一条路：批处理下由驱动层喂指针）
        // ==================================================================

        /// <summary>指针在哪张牌的按钮上（-1 = 没点中）
        /// 🔴 **2026-10-16（`T09` 批 3）**：`HudPiece.Contains` 走 `GetWorldCorners()` ⇒ 量的是**画出来的那个
        /// 世界四边形**、两种模式都对（`HudPiece` 文件头那条判据）⇒ 本口**一行没改**。
        /// ⚠️ 驱动喂进来的是 `CardWorldPos(i)`（**卡的中心**）—— 而那颗钮在卡心**下方 322.618 px**
        /// （🔴 2026-10-20 `T09` A3-e1c 现核：旧值 323 已按 prefab 的 `m_AnchoredPosition.y = −1.759 × 183.41` 改）
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
                // 🔴 **2026-10-20（W57 · `T09`）**：没选牌时确认钮**不可交互**（原版 `Setup` 置
                //    `interactable = false`，判据 → `_confirmInteractable` 那条 doc）——
                //    这一击**吃掉但不提交**：原版那颗 `Button` 收不到 `OnPointerClick`；
                //    而且即使收到，`ClickChooseDone` 也会在 `selectedCard == null` 时 `return`
                //    （`ChooseCardMenu__ClickChooseDone.c:13-18`）。
                if (_confirmInteractable)
                {
                    var picks = Picked;
                    if (OnDone != null) OnDone(picks);
                }
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

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e）：**Editor-only 只读探针**
        //  只读：不改状态、不派事件。播放器里**不存在**这些成员（#if 包住）⇒ 零运行时开销。
        // ══════════════════════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
        /// <summary>🔴 **2026-10-20（W57 · `T09`）**：确认钮此刻可不可交互 —— 原版
        /// `ChooseCardMenu.chooseButton.interactable`。期望值取自**反编译**（⛔ 不是本口自己）：
        /// `Open` 之后、**没选任何卡** ⇒ `false`；选中一张 ⇒ `true`；换选另一张 ⇒ `true`。
        /// 判据 → `_confirmInteractable` 那条 doc。
        /// <para>配套断言（交给主对话派进 `Editor/**`）：`Open` 后 — ⇒ `SimulateChooseDone()` 后
        /// 面板**仍开着**（`Visible == true`）且 `Picked` 为空；`SimulateChoosePick(0)` 后 ⇒ 本口
        /// 翻 `true`；再 `SimulateChooseDone()` ⇒ 面板关闭。</para></summary>
        public bool ConfirmInteractableForTest { get { return _confirmInteractable; } }

        /// <summary>uGUI 输入层是不是**整组装好**了（确认三条 + 眼睛；卡钮按当前 `Open` 的张数逐张判）。
        /// 子画布没立起来 ⇒ `false`（那一档图与字本来就没建）。</summary>
        public bool UguiInputReadyForTest
        {
            get
            {
                if (_panel == null || _btnBar == null || _btnPlay == null
                    || _btnDoneParent == null || _btnEye == null) return false;
                if (_btnCardButtons.Count != _cardBtns.Count) return false;
                for (int i = 0; i < _btnCardButtons.Count; i++)
                    if (_btnCardButtons[i] == null) return false;
                return true;
            }
        }

        /// <summary>原版父 `Continue Button` 那块透明命中面（`null` = 子画布没立起来）。</summary>
        public HudPiece DoneParentHitForTest { get { return _doneParentHit; } }
        public HudPiece BarPieceForTest { get { return _bar; } }
        public HudPiece PlayPieceForTest { get { return _play; } }
        public HudPiece EyePieceForTest { get { return _eye; } }
        public HudPiece CardBtnPieceForTest(int i)
        {
            return (i >= 0 && i < _cardBtns.Count) ? _cardBtns[i] : null;
        }

        public BattleHudButton BarClickComponentForTest { get { return _btnBar; } }
        public BattleHudButton PlayClickComponentForTest { get { return _btnPlay; } }
        public BattleHudButton DoneParentClickComponentForTest { get { return _btnDoneParent; } }
        public BattleHudButton EyeClickComponentForTest { get { return _btnEye; } }
        public BattleHudButton CardClickComponentForTest(int i)
        {
            return (i >= 0 && i < _btnCardButtons.Count) ? _btnCardButtons[i] : null;
        }

        // ── 🆕 2026-10-20（`T09` 步骤 A3-e1）：卡钮 rect / PA / 反馈 ────────────────────────
        /// <summary>第 `i` 张卡下那颗 `Select` 钮的**节点 rect（设计 px）**。原版 = **199.24 × 77.47**
        /// （判据 → `CardBtnW/CardBtnH` 那条 doc）；`SizePx` 对 uGUI 件读的就是 `sizeDelta`。
        /// 越界 / 没建出来 ⇒ `(-1,-1)`（⛔ 不返 0 —— 那与「真的 0 尺寸」分不开）。</summary>
        public Vector2 CardButtonSizeForTest(int i)
        {
            var p = CardBtnPieceForTest(i);
            return p != null ? p.SizePx : new Vector2(-1f, -1f);
        }

        /// <summary>第 `i` 张卡那颗钮的 `Image.preserveAspect` —— 原版 `MB 4178237357239393068.m_PreserveAspect
        /// = **0**` ⇒ 期望 **`false`**（拉伸铺满 rect，⛔ 不是按贴图比例内接）。
        /// 拿不到 ⇒ `null`（⛔ 不返 false：那与「真的是 false」分不开）。</summary>
        public bool? CardButtonPreserveAspectForTest(int i)
        {
            var p = CardBtnPieceForTest(i);
            if (p == null || p.Rect == null) return null;
            var img = p.Rect.GetComponent<UnityEngine.UI.Image>();
            return img != null ? (bool?)img.preserveAspect : null;
        }

        /// <summary>第 `i` 张卡那颗钮的视觉反馈对象（读 `TextureNameForTest` / `KeyForTest`）。
        /// 🔴 **原版那颗钮 = `m_Transition = 2` = `SpriteSwap`**（⛔ **不是** ColorTint —— Unity
        /// `Selectable.Transition` 的枚举是 `None=0 / ColorTint=1 / SpriteSwap=2 / Animation=3`，
        /// 见本机 `com.unity.ugui/Runtime/UGUI/UI/Core/Selectable.cs:133-152`）。
        /// <para>判据（`bundle_battlesharedresources_assets_all`，逐组件现读）：
        /// `Generic Simplified UI Button_updated` 的 `EverguildButton`（`MB -5173003275250738388`）：
        /// `m_Transition = 2` · `m_SpriteState.m_HighlightedSprite = 4884069812420410470`
        /// （= `UI_Button_Mulligan_hover`）· `m_PressedSprite = 1985575716887326321`
        /// （= `UI_Button_Mulligan_Pressed`）· Normal = `3819842738995756391`（= `UI_Button_Mulligan`）。
        /// `m_Colors` 是 Unity **默认四格**（白 / 0.9607843 / 0.7843137 / α0.502 ·
        /// `m_FadeDuration 0.1`）⇒ **t=2 下不生效**；`customColorTintTransitions = 0` ⇒
        /// `EverguildButton__DoStateTransition.c` 那段自定义着色也跳过（它第一句仍调 base
        /// `Selectable.DoStateTransition` ⇒ 换图照常）。</para>
        /// <para>⚠️ 本类 `BuildFeedback()` 用的正好是那三张图（`BattleButtonFx.Swap`）——
        /// **与原版同族同图**，不是偏离。选中之后 `interactable = false`
        /// （我们的等价物 = `SetDisabled(true)`）。`null` ⇒ 没建（面板没摆卡 / 子画布没立起来）。</para></summary>
        public BattleButtonFx CardButtonFxForTest(int i)
        {
            return (i >= 0 && i < _fxCardBtns.Count) ? _fxCardBtns[i] : null;
        }

        // ── 🆕 2026-10-20（`T09` 步骤 A3-e1d）：选中态「露哪张图」 ─────────────────────────────
        /// <summary>第 `i` 张卡**被选中之后**，那颗钮当前露的是不是原版的 **Normal 图**
        /// （`UI_Button_Mulligan`，PathID `3819842738995756391`）。
        /// <para>期望值取自**原版 prefab / 反编译**（⛔ 不是本类的 `CardBtnArtName()` —— 拿实现证明实现 = 自证）：
        /// `SelectCardButtonFrame__SelectButtonOnClick.c` 把钮 `set_interactable(false)`，而
        /// `m_SpriteState.m_DisabledSprite = 0` ⇒ `DoSpriteSwap(null)` ⇒ 回落到 Image 自己的 Normal
        /// （判据全文 → `CardBtnArtName` 那条 doc）。</para>
        /// <para>返回 `true/false` = 选中卡钮当前贴图是/不是 Normal；`null` = 这一张没选中、或钮没建出来
        /// （⛔ 不返 `false`：那与「真的露了错图」分不开）。</para></summary>
        public bool? SelectedSpriteIsNormalForTest(int i)
        {
            if (i < 0 || i >= _cardBtns.Count || _cardBtns[i] == null) return null;
            if (!_picked.Contains(i)) return null;
            var t = _cardBtns[i].Texture;
            return t != null && t.name == "UI_Button_Mulligan";
        }

        /// <summary>选中卡钮**当前**贴图名（调试用；没选中 / 钮没建出来 / 没贴图 ⇒ `null`）。
        /// 读的是**画出来的那一件**的实际贴图（`HudPiece.Texture`），不是任何字段。
        /// 配 <see cref="SelectedSpriteIsNormalForTest"/> 用：它为 `false` 时看这里到底露了哪张。</summary>
        public string SelectedSpriteNameForTest(int i)
        {
            if (i < 0 || i >= _cardBtns.Count || _cardBtns[i] == null) return null;
            if (!_picked.Contains(i)) return null;
            var t = _cardBtns[i].Texture;
            return t != null ? t.name : null;
        }

        // ── 🆕 2026-10-20（`T09` 步骤 A3-e1c）：卡钮**纵向偏移**（只读，配合 A3-e1b 那组 rect 探针）──────
        /// <summary>布局实际用的「卡心 → 钮心」下移量（设计 px）。原版 = **322.618**
        /// （`RectTransform_-9013717219094471892.m_AnchoredPosition.y = −1.7589999437332153 × 183.41`；
        /// 判据全文 → `CardBtnBelowCardPx` 那条 doc）。与实现**同源**（同一个常量）⇒
        /// Editor 断言拿它比「没被写回 323」时，改实现会连带改这里（想防自证就再断一次原版算式）。</summary>
        public static float CardBtnBelowCardPxForTest { get { return CardBtnBelowCardPx; } }

        /// <summary>第 `i` 张卡那颗钮的**中心 y（面板内设计 px，左上原点）**；没建出来 ⇒ `NaN`
        /// （⛔ 不返 0：那与「真的落在 y=0」分不开）。上游 = `LayOutCards` 的
        /// `Lo.CardRowCy + CardBtnBelowCardPx`（`MakeImageInParent` 写的是 `anchoredPosition.y = −yFromTop`
        /// ⇒ 这里 `+ 540` 换回左上原点那一帧）。</summary>
        public float CardBtnDesignYForTest(int i)
        {
            var p = CardBtnPieceForTest(i);
            if (p == null || p.Rect == null) return float.NaN;
            return p.Rect.anchoredPosition.y + 540f;
        }

        /// <summary>uGUI 点击计数（灭自证：点一次 = 1；**手搓路不经过这里**）。</summary>
        public int UguiCardClickCountForTest { get { return _uguiCardClicks; } }
        public int UguiDoneClickCountForTest { get { return _uguiDoneClicks; } }
        public int UguiEyeClickCountForTest { get { return _uguiEyeClicks; } }
        public int LastUguiCardForTest { get { return _lastUguiCard; } }

        /// <summary>读一件 uGUI 图的 `Image.raycastTarget`（原版期望：确认三条 · 眼睛 · 卡钮全是 **true**）。</summary>
        public static bool RaycastTargetOfForTest(HudPiece p)
        {
            if (p == null || p.Rect == null) return false;
            var img = p.Rect.GetComponent<UnityEngine.UI.Image>();
            return img != null && img.raycastTarget;
        }

        /// <summary>读一件 uGUI 图的 `Image.raycastPadding`（L,B,R,T；原版这几个件全是 **(0,0,0,0)**）。</summary>
        public static Vector4 RaycastPaddingOfForTest(HudPiece p)
        {
            if (p == null || p.Rect == null) return new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
            var img = p.Rect.GetComponent<UnityEngine.UI.Image>();
            return img != null ? img.raycastPadding : new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
        }
#endif

        public string Describe()
        {
            return $"选卡面板 开着={Visible} 模式={Mode} 卡 {_cards.Count} 张 选中 [{string.Join(",", Picked.ConvertAll(x => x.ToString()).ToArray())}]";
        }
    }
}
