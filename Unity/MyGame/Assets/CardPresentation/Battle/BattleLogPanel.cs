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
// ✅ **四条边框怎么拼 —— 2026-10-07（A113）已查实**。原来这里写的是「**没查实**（原版 `Frame` 节点的
//   863×1032.5 与面板 794.1 对不上，可能是外扩的装饰边框）」—— **那不是矛盾**：两者是**父子两个节点的
//   两个 `m_SizeDelta`**（794.06897 = 面板自己的；863.0×1032.47009 = 子节点 `Frame` 自己的，
//   而 `Frame` 是个**纯容器、没有任何 Image**），四条边框锚在**容器的左中点**上，所以容器比面板大本来就正常
//   （左框中心伸到面板左缘**外** 18.4 px、右框中心在面板局部 763.75 px）。完整字段链 → 下面常量区那一整段。
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
        /// 🔴 2026-09-29 订正：我们原来把它贴在 x=0（偏左 87 px）。
        /// 🔴 **2026-10-16 再订正（铁律 5）**：本行原文写「我们**不做滑动**（直接切显隐）」—— **已过期**：
        /// **滑动已经做了**（`DOAnchorPosX(rt, −1200 ⇒ 87, 0.3 s)`，实现与断言见本文件 `:548-557` 与
        /// `Editor/BattleScene.cs` 的 `Run` 里那条 `DOAnchorPosX` 滑窗断言）⇒ 这个值就是**滑完停在哪儿**。</summary>
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
        /// <summary>面板的竖直锚定区间（自下往上）—— **原版精确值**（`RectTransform_2936.json` 的
        /// `m_AnchorMin.y` = 0.18734702 / `m_AnchorMax.y` = 0.79271716）。
        /// 🔴 **2026-10-07（A113 追加）订正**：原来写的是四舍五入的 `0.187 / 0.793`（面板高 654.48 vs 真值
        /// **653.7998**、面板中心自顶 550.80 vs 真值 **550.7653**）。吃这两个数的只有下面
        /// `PanelCenterTopPx` / `CardCenterPx` —— BG 与行已改成直接锚原版各自的 rect，不再经过这里。</summary>
        const float PanelTopY01 = 0.79271716f, PanelBotY01 = 0.18734702f;

        /// <summary>面板**竖直中心**的自顶 px —— 原版精确值 `(1 − (0.18734702 + 0.79271716)/2) × 1080` = **550.7653**。
        /// 四条边框 / BG / 行在原版里都是「面板局部、**向上为正**」的量，换算到屏幕一律过它。</summary>
        static float PanelCenterTopPx { get { return (1f - (PanelTopY01 + PanelBotY01) * 0.5f) * 1080f; } }

        const float BgW = 769.5f, BgH = 454.4f;
        // 🔴 **2026-10-07（A113 追加）：`BG` 原来摆在【面板正中】，原版不是** —— 逐字段重摆。
        //   原版 `BG`（`RectTransform_2944.json`，= `GameObject/BG`，面板的直子）：anchor **(0, 0.5)**（面板左中）
        //     · anchoredPosition **(365.98679, 11.69600)**（y 向上为正）· sizeDelta **(769.54388, 454.39761)**
        //     · pivot (0.5, 0.5) · `m_LocalScale` (1,1,1)
        //   ⇒ 中心在**面板局部** x = **365.98679**、自顶 y = 550.7653 − 11.69600 = **539.06934**
        //   ⇒ 比面板中心 397.035 **偏左 31.05 px**、比面板竖直中心**偏上 11.70 px**。
        //   （左缘 = 365.98679 − 769.54388/2 = **−18.785**，几乎正落在左框中心 **−18.4024** 上
        //     —— 「底板被左框压住左边一截」就是这么来的。）
        //   判据双份：上面的场景序列化 + 运行时 `…/panel_0914/runtime_ui_dump_drive.tsv:136`（`365.99/11.7`，一致）。
        const float BgCx = 365.98679f, BgCyTopPx = 539.06934f;

        const float RowW = 748f, RowH = 43.134f;
        // 🔴 **2026-09-28 订正（原来写的是 `RowH 53.92 / RowCount 8`，注「原版十行里能完整看见的是八行」——
        //    那句是错的）**：原版 `CemeteryActions` = **748.006×431.344**、`VerticalLayoutGroup`
        //    spacing **0**、align UpperCenter、**数组长度硬编码 10**（场景里就是 10 个 `CemeterySliderUI`）
        //    ⇒ **10 行 × 43.134**，行底图 `40k_battlelog_display_neutral` 原生就是 **653×43**。
        //    我们原来那个 53.92×8 = 431.36 **容器高度是对的**（所以看着没问题），但**行数与行高都错**。
        //    出处：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4589.json`（`cemeteryActions` 10 个 pid）
        //    + 运行时 dump L134-163 + `素材/Warpforge原版/UI图集/图集/battleatlasui/Sprite/40k_battlelog_display_neutral.json`。
        const int RowCount = 10;
        // 🔴 **2026-10-07（A113 追加）：行也不在面板中线上** —— 原来那句「面板中心 + 顶边框下留 8」**是我们挑的**，
        //   原版行容器自带完整 rect。逐字段重摆：
        //     `CemeteryActions`（`RectTransform_2935.json`，= `GameObject/CemeteryActions`，面板的直子）
        //     anchor **(1, 0.5)**（面板**右中**）· anchoredPosition **(−426.29999, 24.95000)** · pivot (0.5, 0.5)
        //     · sizeDelta **(748.00598, 431.34399)** · `m_LocalScale` (1,1,1)
        //   ⇒ 中心 x = 794.06897 − 426.29999 = **367.76898**（自面板左缘）、自顶 y = 550.7653 − 24.95000 = **525.81534**
        //   ⇒ 原来 x 偏右 **29.27 px**、y 偏上 **10.54 px**（第一行中心：原版自顶 **331.71** vs 我们 **321.13**）。
        //   判据双份：上面的场景序列化 + 运行时 `runtime_ui_dump_drive.tsv:142`（`−426.3,25.0`，一致）。
        //   ⚠️ 顺手作废两个旧量：`FrameTopPx = 68`（那是**贴图原生高**、也不是框高 65.056）与那个「留 8」——
        //     **都已删掉**：行的排布本来就由 `CemeteryActions` 自己的 rect 决定，不需要拿顶边框当基准。
        const float RowsCx = 367.76898f, RowsCyTopPx = 525.81534f;
        /// <summary>行容器 `CemeteryActions` 的高（原版 `m_SizeDelta.y` **431.34399** = 10 × `RowH`）。
        /// 容器顶边自顶 px = `RowsCyTopPx` − 它的一半。</summary>
        const float RowsH = 431.34399f;
        /// <summary>行内**文字左缘**的 x（px）—— 原版 `ActionText`：anchor (0,0)-(1,1)（**stretch 满行**）
        /// · sizeDelta **(−25, 0)** ⇒ 实宽 723 · pivot (0.5, 0.5) · anchoredPosition **(2.5, 0)**
        /// ⇒ 左缘 = **15.0**，两种算法同值：① 中心 = 锚点区间中心 374.003 + 2.5 = 376.503，左缘 = 376.503 − 723/2；
        /// ② uGUI `offsetMin.x = anchoredPosition.x − sizeDelta.x × pivot.x = 2.5 + 12.5`。
        /// 判据双份：场景 `RectTransform_3429.json`（10 个 `ActionText` 各一份、值全同，`2.5 / −25 / stretch / pivot 居中`）
        /// + 运行时 `runtime_ui_dump_drive.tsv:144`（同值）。
        /// 🔴 **2026-10-07（A113 追加）：2026-09-29 记的那个 `8.8` 是【面板局部 x】，不是行内 x** ——
        ///   两者其实指**同一处**：行内 15.0 = 面板局部 15.0 + 行左缘(−6.234) = **8.766** ✓
        ///   （`8.766 + 723 = 731.766` 也与当时记的「…731.8」吻合）。
        ///   错的是**标注的坐标系**，以及我们代码照着它当「行内偏移」用 ⇒ **偏了 6.2 px**。现已按行内 **15.0** 落地。</summary>
        const float TextInsetPx = 15.0f;

        // ============================================================================================
        // 四条边框怎么拼 —— 🔴 **2026-10-07（A113）已查实**。原来这里留着一句
        // 「原版 `Frame` 的 863×1032.5 与面板 794.1 对不上 ⇒ 没查实」—— **那不是矛盾**，
        // 它们是**父子两个节点**，「对不上」只是因为只读了其中一个字段：
        //
        //   · **794.06897** = `CemeteryLogPanel` 自己的 `m_SizeDelta.x`（`m_SizeDelta.y = 0`，
        //     因为它是**竖直 stretch** 锚定：anchorMin.y 0.18734702 / anchorMax.y 0.79271716
        //     ⇒ 高 = 0.60537014×1080 = **653.7998**，不由 sizeDelta 给）。pivot (0, 0.5)、
        //     收起位 `m_AnchoredPosition.x = −1200`（见上面 `PanelOpenX` 那条）。
        //   · **863.0 × 1032.47009** = **子节点 `Frame` 自己的 `m_SizeDelta`** —— 而 `Frame`
        //     是个**纯容器**：它身上只有一个空 `EventTrigger`（`m_Delegates: []`）+ 一个
        //     CanvasRenderer，**没有任何 Image**，不画东西。它的尺寸**不表示任何视觉边界**，
        //     只是原版作者给四条边框用的**锚定基准**。
        //
        // **完整字段链**（逐文件实读，都在 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`）：
        //   ① 面板   `RectTransform/RectTransform_2936.json`（`GameObject/CemeteryLogPanel.json` 的组件）
        //      anchor y [0.18734702, 0.79271716] · anchoredPosition (−1200, 0) · sizeDelta (794.06897, 0) · pivot (0, 0.5)
        //   ② 容器   `RectTransform/RectTransform_3258.json`（= `GameObject/Frame_881.json`；**注意有第二个
        //      同名 `Frame`（PathID 2595 / `Frame.json`），它的 `m_Children` 是空的 —— 不是这一个**）
        //      anchor (0, 0.5) · anchoredPosition (365.9, 12.6) · sizeDelta (863.0, 1032.47009) · pivot (0.5, 0.5)
        //      ⇒ `AnchorMin == AnchorMax == (0, 0.5)` 的意思是**锚在父矩形的左中点**，于是
        //        容器左中点在**面板局部** = (365.9 − 863/2, 12.6) = (**−65.6**, 12.6) px
        //   ③ 四条   `RectTransform_3233/2915/3150/3005.json` —— anchor 同样全是 (0, 0.5)，
        //      **以②的左中点为原点**（这才是「容器比面板大」的用处）；pivot (0.5, 0.5)。
        //   面板中心自顶 px = (1 − (0.18734702 + 0.79271716)/2) × 1080 = **550.7654**
        //   ⇒ 四条的中心（**面板局部 px**：x 自面板左缘、y 自屏幕顶）
        //      · `Frame Top`    anchoredPos (446.02460,  259.5)     框 707.302×65.056  ⇒ ( 380.4246, 278.6654)
        //      · `Frame Bottom` anchoredPos (446.02606, −224.65202) 框 707.302×46.795  ⇒ ( 380.4261, 762.8174)
        //      · `Frame Left`   anchoredPos ( 47.19763,   −4.03401) 框  97.651×571.236 ⇒ (−18.4024, 542.1994)
        //      · `Frame Right`  anchoredPos (829.35010,   −3.1)     框  63.049×578.654 ⇒ ( 763.7501, 541.2654)
        //   ✅ 与 2026-09-27 那轮 `menu_dump` 实读的「自顶中心 278.67 / 762.82 / 542.20 / 541.27」
        //      **逐条吻合**（≤0.005 px）—— 两条独立的路（静态字段链 / 运行时 dump）互证。
        //
        // 🔴 **顺着这条链查出上一版两个真错（本次一并修掉）** —— 上一版把「面板局部 px」当**屏幕**归一化用了：
        //   ① `Frame Left/Right` 的 x 直接写 `−18.405/1920` / `763.75/1920` ⇒ 落点是**屏幕** x = −18.4 / 763.75，
        //      而面板拉开后左缘在 **87** ⇒ 两条竖框**整整偏左 87 px**（`PanelOpenX` 2026-09-29 从 0 改成 87 时
        //      没跟着挪 —— 横条因为用的是 `panelCx01` 所以自动跟着挪了，口径混用才漏掉这一处）。
        //   ② `Frame Top/Bottom` 用的是**面板中心** 397.0345 ⇒ 比原版的 380.425 **偏右 16.61 px**。
        //   ⇒ 下面统一成**一个口径**：`Frame()` 收**屏幕 px**（= `PanelOpenX + 面板局部 x`），不再混。
        //
        // 四条原版都是 `Image.m_Type = 0 (Simple)` + **`m_PreserveAspect = 1`** + `m_Color` 全 1 + `m_RaycastTarget = 0`
        //   （`MonoBehaviour_4218/4482/4547/5178.json`，`m_Script` PathID `350208831926335389` = `UnityEngine.UI.Image`；
        //    每条的另一个 MB `4037/4707/4743/4249` 是空 `EventTrigger`）⇒ **等比内接进那个框、居中**。
        //   `ImageQuad.FitHeight` 就是那条 uGUI `Image.GetDrawingDimensions` 算法（谁受限按谁定），
        //   而 pivot 是 (0.5,0.5) ⇒ 内接后**中心不变**。
        // ⚠️ 贴图原生尺寸（`bundle_atlasindividual_assets_battleatlasui/Sprite/*.json` 的 `m_Rect`）与工程里
        //   `Resources/Art/ui/` 那四张 PNG **逐张相同**（740×68 / 740×49 / 98×601 / 66×608）⇒ 比例一致。
        // ============================================================================================
        /// <summary>四条边框的**框**（原版 `m_SizeDelta`，px）—— 内接之前的那一个矩形。</summary>
        const float FrameTopW = 707.302f, FrameTopH = 65.056f;      // RT_3233
        const float FrameBotW = 707.302f, FrameBotH = 46.795f;      // RT_2915
        const float FrameLeftW = 97.651f, FrameLeftH = 571.236f;    // RT_3150
        const float FrameRightW = 63.049f, FrameRightH = 578.654f;  // RT_3005
        /// <summary>四条边框中心的**面板局部 px**（x 自面板左缘、y 自屏幕顶）。出处见上面整段字段链。</summary>
        const float FrameTopCx = 380.4246f, FrameTopCy = 278.6654f;
        const float FrameBotCx = 380.4261f, FrameBotCy = 762.8174f;
        const float FrameLeftCx = -18.4024f, FrameLeftCy = 542.1994f;
        const float FrameRightCx = 763.7501f, FrameRightCy = 541.2654f;
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
            /// <summary>🆕 2026-09-29：**这一行的底板用哪一张** —— 原版有三张同名变体
            /// （`40k_battlelog_display_{player,enemy,neutral}`，三张**都是 653×43**）。
            /// 我们原来**三张都画 neutral**。判据（原版出处）→ `RowArt` 上面那段注释；
            /// **赋值点**在 `BattleDriver`（`Entry.Side` 由它按 `BattleEvent.Player` 填）。</summary>
            public RowSide Side;
        }

        /// <summary>日志行底板的三种变体（原版三张同名 sprite）。</summary>
        public enum RowSide { Neutral = 0, Player = 1, Enemy = 2 }

        // 🔴 **2026-09-29 查实：判据拿到了，而且我们的做法与原版一致。**
        // 原版画底板的地方**全 Cemetery 族只有一处**：`CemeteryManager__AddActionToCemetery.c:178-185` ——
        //   `sprite = actingCardIsPlayer ? playerActionBg(+0x38) : enemyActionBg(+0x40)`。
        // · `actingCardIsPlayer` = `CemeteryAction` 偏移 **`0x18`**（`dump.cs:119181`）；
        //   `CemeteryManager.playerActionBg // 0x38` / `enemyActionBg // 0x40`（`dump.cs:37051/37053`）；
        //   被写的是 `cemeteryActions[i].cemeteryCardImage`（`+0x20`）= **行底板**。
        // · 同一个 bool 在 `CemeteryLogGroup__DisplayAction.c` 里就是 `bool isPlayer`（决定卡的朝向），语义 = **行动者阵营**。
        // · 序列化交叉验证：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4491.json:62,66` 里
        //   `playerActionBg` → `40k_battlelog_display_player`、`enemyActionBg` → `40k_battlelog_display_enemy`。
        // · **`neutral` 那一张代码里从不被赋值** —— 它只是场景里那 10 行 `Image` 的**预制体默认 sprite**
        //   （运行期 dump `runtime_ui_dump_Battle_Arena_1.tsv:143-161` 十行全是 neutral，就是「还没填过」）。
        // · ⚠️ 顺带更正一条老疑虑：`CemeteryLogGroup.SetActionImage` 确实是空壳（RVA `0x4B33B0`，
        //   与 **1992** 个方法共用 = il2cpp 空方法合并体），但**它不是画底板的方法** ⇒
        //   「三张底板是同一个被剥的方法画的」这个担心**不成立**。
        // ⇒ 我们按 `BattleEvent.Player` 落地 = **与原版同一条判据**。两处延伸（**不是错**，如实标）：
        //   ① `e.Player < 0 → Neutral` 是**我们的兜底**（原版没有「未知行动者」这一支）；
        //   ② 原版 `ClearActions` 不把行重置回 neutral，我们 `has == false → Neutral` 只是**观感相同**。
        /// <summary>行底板对应的原版图名。</summary>
        public static string RowArt(RowSide s)
        {
            switch (s)
            {
                case RowSide.Player: return "40k_battlelog_display_player";
                case RowSide.Enemy:  return "40k_battlelog_display_enemy";
                default:             return "40k_battlelog_display_neutral";
            }
        }

        Transform _root;              // 面板本体（显示/隐藏切它）
        ImageQuad _bg;
        ImageQuad[] _rowBgs;
        Label[] _rowTexts;
        ImageQuad _shade;
        /// <summary>最近一次喂进来的内容（`Show()` 里要拿它重刷一遍 —— 见 `SetEntries` 的注释）</summary>
        readonly List<Entry> _last = new List<Entry>();
        bool _visible;

        public bool Visible { get { return _visible; } }        /// <summary>自检用：面板根节点是不是被显示着</summary>
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
        /// <summary>自检用：第 i 行底板用**哪张图**（原版三张变体之一；行不存在返回 ""）。</summary>
        public string RowBgName(int i)
        {
            if (_rowBgs == null || i < 0 || i >= _rowBgs.Length || _rowBgs[i] == null) return "";
            var t = _rowBgs[i].Texture;
            return t != null ? t.name : "";
        }

        /// <summary>自检用：第 i 行的**可见文字**（`&lt;link=…>` 那层壳已剥掉 —— 玩家看到的是剥掉之后那句）。
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

        // ============================================================================================
        //  🆕 2026-10-18（`A940` 尾账 `Z2`/`Z3`）：**「点第 N 行」那个交互**
        //
        //  🔴🔴 **2026-10-18（`G9`）判据【就地订正】（铁律 5）—— 这一段的原判据引用的是【死代码】。**
        //    上一轮（`G4`）把判据写成 `d:/2/tools/decomp_full/CemeteryLogManager__ClickCemeterySlider.c`。
        //    本轮按「读那个方法体、别猜」去读它之后，顺着它把**整条链**核了一遍，结论是
        //    **`CemeteryLogManager` 那一族在发行版里【根本没有实例】**，四条独立证据：
        //      ① 全库（`d:/2/新解包资源/assets_full/`，**24.7 万文件**）扫它独有的字段名
        //         `cemeteryGroup` / `centralCemetery` —— **0 命中**（父容器 `CemeteryLogPanel` 的六个组件里
        //         也没有它：`RectTransform_2936` · `CanvasRenderer_2084` · **`MonoBehaviour_4491`** ·
        //         `Canvas_2579` · `MonoBehaviour_4317`(menuScale) · `MonoBehaviour_4637`(Canvas)）；
        //      ② `CemeteryLogPanel` 上那颗 **`MonoBehaviour_4491` 是另一个类 `CemeteryManager`**
        //         （字段 `cemeteryActions[]`/`playerActionBg`/`enemyActionBg`/`initialX`/`finalX`/`shade`/
        //          `showingCardUI`，与 `dump.cs:37046-37069` 的 TypeDefIndex **720** 逐格吻合），
        //         而 `ClickCemeterySlider` 是 **`CemeteryLogManager`（TypeDefIndex 766，`dump.cs:38986`）**——
        //         **两个类**，后者在场景里没有实例（`CemeteryLogPanel` 那六个组件里没有它）；
        //      ③ 行节点 `CemeterySliderUI` 的序列化 uGUI 事件（`MonoBehaviour_5060.json`）指向
        //         **`CemeteryLogSlider.OnSliderChanged` / `OnDragEnd`** —— 而 `CemeteryLogSlider`
        //         在元数据里**只有 `Setup` / `GetIndex` / `.ctor` 三个方法**（`dump.cs:39049-39066`），
        //         全库（`dump.cs` 26 万行 + `decomp_full`）**搜不到这两个名字**
        //         ⇒ **序列化事件指向两个【已不存在】的方法**（这条链是残留）；
        //      ④ 素材侧也没有配套物：`battlearena1_sprite_map.json` 的 96 条里
        //         日志面板相关的只有 `40k_UI_bt_battlelog` / `40k_battlelog_display_{player,enemy,neutral}`
        //         / 四条 `40k_battlelog_frame_*` —— **没有任何「选中行高亮」的图**。
        //
        //  ✅ **发行版里真正跑的是另一条（`CemeteryManager`，同一个面板上那个组件）**：
        //    · `CemeteryManager__DisplayCemeteryActions.c` = **整块面板的开/关**（`shade.SwitchShade`
        //      + `DOLocalMoveX(initialX +0x48 → finalX +0x4C)`），不是「点行弹卡」；
        //    · **点行弹卡那件事的活判据 = `CemeteryManager__ClickCardLink.c`**（`CheckCardLink` 是它的悬停版）：
        //      遍历 10 颗 `cemeteryActions[]`（`+0x30`）× 每颗的 TMP（`CemeteryLogSlider.cemeterySliderText`
        //      `+0x28`）做 **`TMP_TextUtilities.FindIntersectingLink`**（**与本类 `LinkKeyAt` 同一条路**）
        //      → `GetLinkID()` → `Split(',')` → 用 `split[0]`（`int.Parse`）取 `newCemeteryActionList[index]`
        //      （`+0x68`）→ 按 `split[0]` 与两个字面量比 → `BattleCardManager.GetCardFromUniqueId(uid)`
        //      → **`BattleManager__DisplayCard(card, 1)` + `HideCemeteryLogBtn`**；
        //    · `DisplayCard` 最终落在 **`showingCardUI`（`+0x58`，一颗 `BasicCardUI`）** 上
        //      —— 也就是场景里那唯一一颗 `CemeteryGroup/CardUI (1)`（`Transform_1340` 的
        //      `localPos (−53.04, 20.49)` / `localScale 108`）⇒ **一张卡**，正是我们这边那颗。
        //
        //  🔴 **我们落地的边界（如实记，⛔ 别当成「原版就这样」）**：
        //    · **弹的是【一张】卡** —— 这与**发行版的活判据（`ClickCardLink`）一致**；
        //      上一轮那套「点一行摆三张（`centralCemetery`/`upperCemetery`/`lowerCemetery`）+
        //      整摞跟着行 y 走 + 选中行高亮」出自**死类 `CemeteryLogManager`**，
        //      ⛔ **发行版里不会发生**（证据见上四条）⇒ **不做**（判据推翻，不是「嫌麻烦」）。
        //      ⚠️ 若将来要照那个死类补：层叠偏移**已经实读出来了** ——
        //      `CemeteryLogGroup__CreateTargetCard.c`（+ `GetCemeteryCardLocalPos.c`）里就是
        //      `localPosition = { x = index * 4.0 + 6.5, y = 0, z = 0 }`（主卡在 `(0,0,0)`，index = 0/1），
        //      两个常量在 `GameAssembly.dll` 常量池实读 = **`4.0`**（VA `0x1834b2df0`）与 **`6.5`**（`0x1834b31c0`）；
        //      但那两个数落在**哪个坐标系**（`CemeteryLogGroup` 的 lossyScale 本地取不到 ⇒ 6.5 是 **px** 还是
        //      6.5×108 = 702 px）**判据不足**，⛔ 别硬填。
        //    · **命中区 = 整行底板的真实矩形**（`RowAt`），而**活判据是「行内文字上的那个链接」**
        //      （`FindIntersectingLink`）—— 这是**我们比原版宽**的一处（如实记）：点行的空白处在我们这边
        //      也会弹那张卡，原版只认链接。**理由**：批处理里喂不了真鼠标，而「点行」这条入口是
        //      上一轮建的（`TryClickLogRow`）；要收窄成「只认链接」得改 `TryClickLogRow` 与它那几条断言，
        //      ⛔ 不在本笔（`G9`）的范围里 ⇒ 记成账。
        //    · **没有滑动动画**：原版 `DisplayCemeteryActions` 那次开/关面板是 `DOLocalMoveX`；
        //      我们面板那一段**有滑动**（见 `PanelOpenX` 的注释），「点行」这一条**没有**补间。
        //    · **没有「点中的那一行高亮」**：证据④（没有那张图）+ 活判据里也没有这件事
        //      ⇒ `SelectedRow` 只记账、不画，**这是照原版的**（上一轮记成「缺一件」的那半句作废）。
        // ============================================================================================

        /// <summary>**指针压在哪一行上**（= 哪一颗 `CemeterySliderUI`）。`-1` = 不在任何一行上。
        /// ⚠️ 命中区就是**行底板那颗 quad 的真实矩形**（`WorldW/WorldH`）—— 与本类画出去的东西同源，
        ///    不另立一套常量。面板没显示 ⇒ 恒 `-1`。</summary>
        public int RowAt(Vector3 wp)
        {
            if (!_visible || _rowBgs == null) return -1;
            for (int i = 0; i < _rowBgs.Length; i++)
            {
                var q = _rowBgs[i];
                if (q == null || !q.gameObject.activeSelf) continue;
                var d = wp - q.transform.position;
                if (Mathf.Abs(d.x) <= q.WorldW * 0.5f && Mathf.Abs(d.y) <= q.WorldH * 0.5f) return i;
            }
            return -1;
        }

        /// <summary>自检用：第 `i` 行底板的**世界中心**（批处理没有鼠标 ⇒ 拿它当点击/悬停点喂进去）。
        /// 行不存在 / 没画出来 ⇒ false（**如实返回 false**，不编一个点）。</summary>
        public bool RowCenterWorld(int i, out Vector3 wp)
        {
            wp = Vector3.zero;
            if (_rowBgs == null || i < 0 || i >= _rowBgs.Length || _rowBgs[i] == null) return false;
            wp = _rowBgs[i].transform.position;
            return true;
        }

        /// <summary>最近一次**真的点中**的那一行（原版死类 `CemeteryLogManager.cemeterySliderActionDisplayed`，`+0x70`）。
        /// `-1` = 还没点过。⚠️ 只记账，⛔ 不画 —— 🔴 **2026-10-18（`G9`）订正**：原注写「原版靠 `cemeteryGroup`
        /// 挪位表示，我们没有那一件」，那句把「发行版的做法」说错了：`cemeteryGroup` 只活在**没有实例的死类**里
        /// （证据 → 本文件上面那段 `Z2`/`Z3` 的大注释），**发行版里点行不产生任何行高亮**
        /// （素材侧也没有那张图）⇒ 「不画」**是照原版的**，不是缺口。</summary>
        public int SelectedRow { get; private set; }

        /// <summary>记下「点中了第 `i` 行」（原版死类 `ClickCemeterySlider` 里那句
        /// `cemeterySliderActionDisplayed = index`）。⚠️ **闸门不在这里** —— `hideCemetery` 那条在
        /// `BattleDriver`（原版也是先过 `CanShowCemetery` 才走到这一句）。</summary>
        public void SelectRow(int i) { SelectedRow = i; }

        /// <summary>第 `i` 行那一条动作提到的**卡（英文 id / 卡名）** —— 点行要弹的就是它。
        /// 空 = 这一行没提卡 / 行是空的（那就**不弹**，⛔ 不弹一张空卡）。</summary>
        public string RowCardKey(int i)
        {
            if (i < 0 || i >= _last.Count) return null;
            return _last[i].CardId;
        }

        /// <summary>点中第 `i` 行时，那张卡该摆的**世界坐标**（相对面板根）。
        ///
        /// 🔴 **判据（死类那份，如实标）**：`CemeteryLogManager.ClickCemeterySlider` 把 `cemeteryGroup`（`+0x40`）
        ///    的 **y 挪到点中那一行的 y**（x 不动）；而卡组自己相对面板是 `CardUI (1)`：面板左缘
        ///    **左 53.04** px、**竖中线** 上 20.49 px（见 <see cref="CardCenterPx"/> 的字段链）
        ///    ⇒ 换成「跟着行走」就是 **屏幕 x = `PanelOpenX − 53.04` · 自顶 y = 那一行的中心 − 20.49**。
        /// ⚠️ 🔴 **2026-10-18（`G9`）如实标这一处的性质**：那个 `cemeteryGroup` **只活在死类里**
        ///    （发行版的 `CemeteryManager` 没有这个字段，全库 0 命中）⇒ 「卡跟着行走」**不是发行版的行为**；
        ///    发行版 `CemeteryManager.DisplayCard` 只是把固定位的 `showingCardUI`（`CardUI (1)`）`SetActive(true)`
        ///    —— 那正是 <see cref="HoverCardLocalPos"/> 那一档（y 钉在面板竖中线）。
        ///    **我们保留「跟行走」**只是沿用上一轮建的入口（改动会牵动它那几条断言）⇒ 记成账；
        ///    ⛔ 别把这里当成「原版也这样」。
        /// ⚠️ 与 <see cref="HoverCardLocalPos"/> 的**唯一**差别就是 y 跟不跟行；两条路画的都是同一个
        ///    `CardUI` ⇒ 共用卡体尺寸，别各写一个。</summary>
        public Vector3 RowCardLocalPos(int row, float z)
        {
            if (_rowBgs == null || row < 0 || row >= _rowBgs.Length || _rowBgs[row] == null)
                return HoverCardLocalPos(z);                 // 行取不到 ⇒ 退回原口径（出声由调用方负责）
            float rowCyTop = LayoutSpace.ToPixel(_rowBgs[row].transform.position).y;
            float cxPx = PanelOpenX - 53.04f;                // 与 `CardCenterPx` 同一个 x
            var w = LayoutSpace.ToWorld(cxPx / 1920f, 1f - (rowCyTop - 20.49f) / 1080f);
            return new Vector3(w.x - (_root != null ? _root.position.x : 0f),
                               w.y - (_root != null ? _root.position.y : 0f), z);
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
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    ⚠️ **这一层是我们自己的**（原版没有它）：它把「左区那一级（原版 `Safe area BackCanvas`
            //    下的 `LeftArea`，实读 RT · `sizeDelta (0,0)` ⇒ 铺满整块）」与我们那张**铺满全屏**的压暗层
            //    （`shade`，原版 7020×4544）收在一起 ⇒ 矩形取**屏矩形 1920×1080**（= 它真正占的那一块）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「日志面板根 = 屏矩形」红。
            var go = new GameObject("BattleLogPanel", typeof(RectTransform));
            go.transform.SetParent(root, false);
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
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

            var panel = new GameObject("CemeteryLogPanel", typeof(RectTransform));
            panel.transform.SetParent(root, false);
            // 🔴 **2026-10-11（A218）**：面板节点是 `RectTransform` + 写 `sizeDelta` —— 判据 = 原版同名件
            //    `CemeteryLogPanel` 自己的 rect：`m_SizeDelta = (794.06897, **0**)` + 竖直 stretch 锚
            //    （`anchorMin.y 0.18734702 / anchorMax.y 0.79271716`）⇒ **高由锚点给**、不由 `sizeDelta` 给
            //    ⇒ 实际高 = `(0.79271716 − 0.18734702) × 1080 = 653.7998`（宽取本文件那个 `PanelW`，
            //    它就是这个 794.06897 的四舍五入，本文件全部几何都用它 —— ⛔ 别在这儿另立一个数）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「`CemeteryLogPanel` 的 rect」红.
            MenuDraw.SetPxSize(panel.transform, PanelW, (PanelTopY01 - PanelBotY01) * LayoutSpace.DesignPxH);
            _root = panel.transform;

            // 底板：原版是个**没有 sprite 的 Image**（色 (0,0.08,0.01)）→ 我们用白图 + tint。
            // 🔴 **2026-10-07（A113 追加）位置照原版** —— 它**不在面板中线上**（算式与出处见 `BgCx/BgCyTopPx`）。
            //    ⚠️ 顺带作废：原来的 `panelCy / panelCx01`（面板中心）**已删** —— 它只喂过这几件，
            //    现在底板/边框/行各自锚自己那条原版 rect，不需要「面板中心」这个中间量了。
            var bgAt = LayoutSpace.ToWorld((PanelOpenX + BgCx) / 1920f, 1f - BgCyTopPx / 1080f);
            var bg = ImageQuad.Create(_root, CardArt.Solid(), new Vector3(bgAt.x, bgAt.y, ZBg), Px(BgH),
                                      new Vector2(0.5f, 0.5f), "LogBG");
            _bg = bg;
            if (bg != null)
            {
                bg.SetAspect(BgW / BgH);
                bg.SetTint(BgColor);
                bg.SetRenderQueue(OverlayQ);
            }

            // 四条边框 —— **逐值照原版**（框 + 中心逐条来自 prefab 字段链，见上面那整段字段链注释）。
            // x 一律用 `PanelOpenX + 面板局部 px` ⇒ **屏幕 px**（同一个口径，不再混）。
            Frame("40k_battlelog_frame_TOP",    PanelOpenX + FrameTopCx,   FrameTopCy,   FrameTopW,   FrameTopH);
            Frame("40k_battlelog_frame_Bottom", PanelOpenX + FrameBotCx,   FrameBotCy,   FrameBotW,   FrameBotH);
            Frame("40k_battlelog_frame_Left",   PanelOpenX + FrameLeftCx,  FrameLeftCy,  FrameLeftW,  FrameLeftH);
            Frame("40k_battlelog_frame_Right",  PanelOpenX + FrameRightCx, FrameRightCy, FrameRightW, FrameRightH);

            // 行：748×43.134（`RowH` —— 2026-09-28 已订正），**位置照原版行容器 `CemeteryActions`**
            //（🔴 2026-10-07 A113 追加：原来那句「面板中心 + 顶边框下留 8」**是我们挑的** —— 判据见 `RowsCx/RowsCyTopPx`）
            _rowBgs = new ImageQuad[RowCount];
            _rowTexts = new Label[RowCount];
            for (int i = 0; i < RowCount; i++)
            {
                float cy01 = RowCenterY01(i);
                var at = LayoutSpace.ToWorld((PanelOpenX + RowsCx) / 1920f, cy01);

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

                // 文字起点 = **行左缘 + 行内 15.0 px**（原版 `ActionText` 的 `offsetMin.x`）。
                // 🔴 **2026-10-07（A113 追加）订正**：原来写的是 `(PanelW − RowW)/2 + 8.8`（= 面板局部 31.85）——
                //    那个 8.8 是**面板局部 x** 被标成了「行内 x」，而且我们还叠了一层「面板居中」的假设
                //    ⇒ 比原版（面板局部 **8.766**）**偏右 23.08 px**。详见 `TextInsetPx`。
                var textAt = LayoutSpace.ToWorld(
                    (PanelOpenX + RowsCx - RowW * 0.5f + TextInsetPx) / 1920f, cy01);
                _rowTexts[i] = Label.Create(_root, "", new Vector3(textAt.x, textAt.y, ZText), 3,
                                            new Color(0.93f, 0.93f, 0.93f),   // 原版 ActionText = Asar Almost White
                                            new Vector2(0f, 0.5f), "LogRowText" + i);
                if (_rowTexts[i] != null) _rowTexts[i].SetRenderQueue(OverlayQ);
            }

            panel.SetActive(false);
            // 出厂 = **收在屏幕外**（原版 `initialX = −1200`）⇒ 第一次点开是从屏幕外**划进来**的。
            _slideT = 0f;
            ApplySlide();
            SelectedRow = -1;      // 🆕 Z2：出厂「还没点过任何一行」（别让它默认成 0 = 看着像点过第 1 行）
        }

        /// <summary>摆一条边框。坐标 = **屏幕 px**：`cxPx` 自屏幕左缘、`cyPx` 自屏幕顶
        /// （= `PanelOpenX` + 面板局部 px，出处见上面那整段字段链）。
        /// `boxW/boxH` = 原版那一条的**框**（`m_SizeDelta`）。
        ///
        /// 🔴 原版四条都是 `Simple + m_PreserveAspect = 1` ⇒ **等比内接进框、居中**
        /// （pivot (0.5,0.5) ⇒ 内接后中心不变）。而 `ImageQuad.Create` 只吃「高」、宽 = 高 × 贴图比例
        /// ⇒ **不能直接把框高喂进去**：「宽受限」的那条（Top）会宽出 0.69 px。
        /// 所以先过一道 `ImageQuad.FitHeight(boxW, boxH, 贴图比例)`（= uGUI `Image.GetDrawingDimensions`
        /// 那条算法：图比框宽就按宽定），再把内接后的高交给 `Create` ⇒ 实绘 = 原版实绘，逐 px 一致：
        ///   · Top    **宽受限** ⇒ 707.302 × 64.995   · Bottom **高受限** ⇒ 706.700 × 46.795
        ///   · Left   **高受限** ⇒  93.147 × 571.236  · Right  **高受限** ⇒  62.814 × 578.654
        /// ⚠️ 上一版喂的是「框高 ±0.01」（65.06 / 46.79 / …）⇒ Top 实绘成 **707.99×65.06**
        ///    （宽出框 0.69 px，而它本该是**宽受限**的那条），而当时的断言盯的正是这个自算值
        ///    （「≈707.9」）—— 等于拿我们自己的常量证明我们自己的常量。现在断言盯原版实绘值。</summary>
        void Frame(string art, float cxPx, float cyPx, float boxW, float boxH)
        {
            var tex = CardArt.Ui(art);
            if (tex == null) return;
            var at = LayoutSpace.ToWorld(cxPx / 1920f, 1f - cyPx / 1080f);
            float sprAspect = tex.height > 0 ? tex.width / (float)tex.height : 1f;
            float hPx = ImageQuad.FitHeight(boxW, boxH, sprAspect);
            var q = ImageQuad.Create(_root, tex, new Vector3(at.x, at.y, ZFrame), Px(hPx),
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

        /// <summary>某条边框**实绘中心**的屏幕 px（x 自屏幕左缘、y 自屏幕顶）—— 自检用。
        /// ⚠️ 量的是**真实几何**（quad 的 transform），不是我们存下来的常量 —— 上一版把竖框摆偏 87 px、
        /// 横框摆偏 16.6 px，就是因为没人量过这个（当时只量了尺寸）。
        /// 面板滑动时它跟着动 ⇒ 断言要在**滑到位之后**问（`FinishSlideForTest()` 之后
        /// `_root.localPosition.x = 0`，读数就是原版坐标）。
        /// 查不到返回 (−9999, −9999)（不用 `Vector2.zero` —— 那是屏幕左上角，是个合法值）。</summary>
        public Vector2 FrameCenterPx(string art)
        {
            ImageQuad q;
            if (!_frames.TryGetValue(art, out q) || q == null) return new Vector2(-9999f, -9999f);
            return LayoutSpace.ToPixel(q.transform.position);
        }

        /// <summary>`BG`（底板）**实绘中心**的屏幕 px（x 自屏幕左缘、y 自屏幕顶）—— 自检用。
        /// 同上：量的是 quad 的真实 transform。🔴 原版 `BG` **不在面板中线上**（判据见 `BgCx/BgCyTopPx`）——
        /// 上一版摆的是面板正中（x 偏右 31 px），当时没有任何断言看得见。
        /// 查不到返回 (−9999, −9999)。</summary>
        public Vector2 BgCenterPx
        {
            get
            {
                return _bg != null ? LayoutSpace.ToPixel(_bg.transform.position)
                                   : new Vector2(-9999f, -9999f);
            }
        }

        // ==================================================================
        //  滑动（原版 `CemeteryManager`：`DOAnchorPosX(rt, initialX = −1200 ⇒ finalX = 87, 0.3 s)`）
        //  判据（逐行读过 `CemeteryManager__ShowCemeteryLogBtn.c`）：开 = `:17`、合 = `:51`——
        //  两条**都先** `TweenExtensions.Complete(+0x98)`（把飞行中的补间**一把推到底**）再起新的；
        //  时长 `DAT_1834b2dc8` = **0.3f**（`GameAssembly.dll` 浮点池实读，与 `CardFeel` 同一只常量）；
        //  调用点是 DOTween 快捷式、**没有 `SetEase`** ⇒ 默认缓动 **OutQuad**。
        //  ⚠️ 压暗层**不跟着滑**：原版在开/合那一刻直接 `shade.SetActive(±1)`（不是补间）——
        //     我们的 `_shade` 本来就建在 `_root` 之外，照旧。
        //  ⚠️ 批处理没有帧循环 ⇒ 自检用 `AdvanceSlideForTest` / `FinishSlideForTest` 手动推。
        // ==================================================================
        const float PanelClosedX = -1200f;   // 原版 `initialX`（收在屏幕外）
        const float SlideDur = 0.3f;         // `DAT_1834b2dc8`
        float _slideT = 1f;                  // 0 = 收在 −1200 · 1 = 拉开到 +87
        int _slideDir;                       // 0 = 停着 · +1 = 正拉开 · −1 = 正收起
        float _slideOffsetX;                 // 我加在 `_root.localPosition.x` 上的位移（世界单位）

        /// <summary>自检用：滑动进度（0 = 收 · 1 = 开）。</summary>
        public float SlideT { get { return _slideT; } }

        /// <summary>自检用：面板**左缘当前所在的屏幕 x（px）**。`_root` 的子树用的是绝对世界坐标、
        /// 位移全记在 `_root.localPosition.x` 上 ⇒ 反推回来即可（不动任何几何）。</summary>
        public float PanelLeftX
        {
            get
            {
                float w = LayoutSpace.ToWorld(PanelOpenX / 1920f, 0.5f).x + _slideOffsetX;
                return (w / LayoutSpace.VisibleWidth + 0.5f) * 1920f;
            }
        }

        public void Toggle() { if (_visible) Hide(); else Show(); }

        public void Show()
        {
            _visible = true;
            if (_root != null) _root.gameObject.SetActive(true);
            if (_shade != null) _shade.gameObject.SetActive(true);
            SnapSlide();                // 原版：飞行中的补间先 `Complete()`
            _slideDir = +1;
            ApplyEntries();             // ⚠️ **激活之后再刷一次** —— 未激活时 TMP 建不出字形
        }

        public void Hide()
        {
            _visible = false;
            SnapSlide();
            _slideDir = -1;
            if (_shade != null) _shade.gameObject.SetActive(false);
            ApplySlide();
            // `_root` **不在这里关** —— 要等它滑回收起位（`TickSlide` 里关），否则「啪」地一下消失。
        }

        void Update() { TickSlide(Time.unscaledDeltaTime); }

        /// <summary>自检用：手动推滑动（批处理没有帧循环）。</summary>
        public void AdvanceSlideForTest(float dt) { TickSlide(dt); }

        /// <summary>自检用：把正在走的滑动**一把推到底**（= 原版那个 `Complete()`）。</summary>
        public void FinishSlideForTest()
        {
            if (_slideDir > 0) _slideT = 1f;
            else if (_slideDir < 0) { _slideT = 0f; if (_root != null) _root.gameObject.SetActive(false); }
            _slideDir = 0;
            ApplySlide();
        }

        void TickSlide(float dt)
        {
            if (_slideDir == 0 || _root == null) return;
            _slideT = Mathf.Clamp01(_slideT + _slideDir * (dt / SlideDur));
            ApplySlide();
            if (_slideDir > 0 && _slideT >= 1f) _slideDir = 0;
            else if (_slideDir < 0 && _slideT <= 0f) { _slideDir = 0; _root.gameObject.SetActive(false); }
        }

        /// <summary>原版 `ShowCemeteryLogBtn` 开头那一下：**在飞的补间先 `Complete()`**（推到底、不反向）。</summary>
        void SnapSlide()
        {
            if (_slideDir > 0) _slideT = 1f;
            else if (_slideDir < 0) _slideT = 0f;
            _slideDir = 0;
            ApplySlide();
        }

        /// <summary>把进度画到 `_root.localPosition.x` 上（DOTween 默认缓动 = OutQuad）。</summary>
        void ApplySlide()
        {
            if (_root == null) return;
            float u = 1f - _slideT;
            float xPx = Mathf.Lerp(PanelClosedX, PanelOpenX, 1f - u * u);
            _slideOffsetX = LayoutSpace.ToWorld(xPx / 1920f, 0.5f).x
                          - LayoutSpace.ToWorld(PanelOpenX / 1920f, 0.5f).x;
            var p = _root.localPosition;
            _root.localPosition = new Vector3(_slideOffsetX, p.y, p.z);
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

                // 🆕 2026-09-29：**底板按「谁做的动作」换**（原版三张变体）。
                // ⚠️ 取不到图就**保持上一次的**（别 `SetTexture(null)` 把底板刷没 —— 和
                //    `AttackSelector.ApplyOptionIcons` 同一条纪律）。
                if (_rowBgs != null && i < _rowBgs.Length && _rowBgs[i] != null)
                {
                    var want = CardArt.Ui(RowArt(has ? _last[i].Side : RowSide.Neutral));
                    if (want != null && !ReferenceEquals(_rowBgs[i].Texture, want))
                    {
                        _rowBgs[i].SetTexture(want);
                        // 🔴 **必须再拉一次比例** —— `ImageQuad.SetTexture`（`:78`）会把 `_aspect`
                        //    重设成**贴图自身**的宽高比，把建的时候那次 `SetAspect(RowW/RowH)` 冲掉
                        //    （2026-09-29 实测：换完图行底图从 **748** 缩到 **690.1**，被自检那条
                        //     「画满整行宽 748」当场抓到）。三张变体虽然都是 653×43，但**依赖这一点是脆的**。
                        _rowBgs[i].SetAspect(RowW / RowH);
                    }
                }
            }
        }

        /// <summary>把这一行里出现的**卡名**包成 TMP 链接 —— 这是原版那条「悬停卡名 ⇒ 弹一张卡」的入口
        /// （`CemeteryManager.CheckCardLink`：对行文字做 `FindIntersectingLink` → `GetLinkID` → `DisplayCard`）。
        ///
        /// 包的样式**照原版**：`<b>&lt;link="…"><u>名字</u>&lt;/link></b>`（名字**加粗 + 下划线** = 那种「可点」的样子；
        /// 出处 = `CemeteryManager` 里那三个字面量 `'<b>&lt;link="1,'` / `'"><u>'` / `'</u>&lt;/link></b>'`，
        /// 靠 `stringliteral.json` 解出来的）。
        /// ⚠️ **链接 ID 用的是英文卡名**（原版用的是「`0/1,动作索引`」那种**动作表下标** —— 我们没有那张表，
        /// 如实记这条差异）；可见文字用 `LinkText`（中文名）。
        /// ⚠️ 名字对不上（这行本来没提卡 / 译文不一致）就**原样返回、不硬造链接**。
        /// ⚠️ 点阵后端（没有 TMP 时）会把标签整个剥掉（`CardIcons.StripTags` 的判据是「有没有 `&lt;`」）⇒ 安全。</summary>
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

        /// <summary>第 i 行的中心 y01（`Build` 和 `SetEntries` 共用这一份）。
        /// 行容器 `CemeteryActions` 的**顶边**在面板局部自顶 `RowsCyTopPx − RowsH/2`，行从那里往下排
        /// （🔴 2026-10-07 A113 追加：原来拿「面板顶 − 顶边框高 − 8」当基准 —— 那是**我们挑的**）。</summary>
        static float RowCenterY01(int i)
        {
            float rowTopPx = RowsCyTopPx - RowsH * 0.5f;
            return 1f - (rowTopPx + (i + 0.5f) * RowH) / 1080f;
        }
    }
}
