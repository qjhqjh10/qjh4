// SkillPanel.cs — 原版的「主动技能卡面板」（`ActiveSkillDesc`）
//
// 原版选完**主动技能**之后会弹一张卡，写清楚这个技能是什么、还能选几个目标。
// 我们以前把技能效果塞在选择器上方那行小字里（`AttackSelector` 的 `note`），现在按原版搬到这里。
//
// 出处：解包场景 `07_场景/battlearena1/`（**UI/3D 的东西不在 UI dump 里**）。
//
// ⚠️ **位置别照着 anchor 写。** 原版 RT 2911 的 anchor 是 x 0.6656–0.9658 / y 0–0.304，
//    看着在「右下」，但 `m_AnchoredPosition = (-606.11, 55.0)` 把它**拉回了水平正中**：
//      pivot 世界位 = (anchorMin + Δanchor×pivot) × 父尺寸 + anchoredPosition
//                   = (0.8156827×1920, 0.1520065×1080) + (-606.11, 55.0) = (960.0, 219.17)
//      尺寸        = (0.300212×1920, 0.304013×1080) + sizeDelta(-0.00037, -3.51) = 576.41 × 324.82
//    → 屏幕矩形 x **671.8–1248.2**、y **56.8–381.6**（1920×1080），即**底部居中**、不是右下。
//    交接文档第七节原来写的「右下 30%×30%」是错的（只看了一个字段）。
//
// 换算链（每一步都是 ×1.0，已逐级核过）：CanvasScaler `ScaleWithScreenSize` +
// `MatchWidthOrHeight=0` + 参考分辨率 1920×1080 → `scaleFactor = 屏宽/1920`；
// Canvas / Safe area FrontCanvas / FrontCanvas / BattleHud / BattlePrefab / ActiveSkillDesc
// 各级 `m_LocalScale` 全 1 → **1920 下 1 UGUI 单位 = 1 px**。
//
// 其它从原件量到的：
//   · `fadeTime = 0.2`，**纯代码插值、没有 Animator**（走 `CardTween` 的推进模式，
//     批处理自检里 `Step()` 才推得动）。`FadeOut` 用的是 `fadeTime × 一个解析不出的常数`
//   · 四个文本（原版默认值 / 字号）：`NameText`(`"Fire Arrow"`/42.35)、`CostText`(`"10"`/52.4)、
//     `DescText`(37.35)、`TargetsAvailableText`(`"0 available"`/56.45)。
//     `DescText` **左对齐**（`m_margin.x = 7.4727 px`），其余居中。
//     四个都开着自动缩放 `[4,60]` —— **序列化字号是编辑器烘的「上次结果」**，
//     运行时会按各自矩形重算。这里用「同款矩形 + 装不下就回缩」来逼近那个行为（见 `Fit`）
//   · `Lights` = **三层全铺的色片**（三个子节点 anchor 0,0–1,1 完全重叠），不是一排小灯
//   · `HideAbilityButton` = 一块**远大于面板的透明点击捕获区**（`m_Color.a = 0`、
//     `m_Type = Sliced`、`RaycastTarget = 1`），点它 → 面板收起。
//     我们这边等价的行为是「点面板外的任意地方 → `ClearSelection()` → 面板收起」
//
// ⚠️ **一处是我们挑的、不是原版的做法**：**底板**。
//    原版 `AbilityContainer` 上那个 Image **没有 sprite、颜色是纯白** ——
//    照搬会渲成一块白底压着白字（多半靠运行时别的机制压暗），所以这里用深色板。
// ═════════════════════════════════════════════════════════════════════════════
//  🔴 **2026-10-16（`T09` 批 3 · `OverHand` 组）：图/字整批从【世界空间 quad】迁到【uGUI 子画布】**
//
//  · **原版宿主**（判据 = `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一 我这一行）：
//    `ActiveSkillDesc` @ `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`
//    —— **继承 `FrontCanvas`(rm 1) · `OverHand` 层 · order 0 · 兄弟序 1/14**（出处 `RT_3104` ·
//    `RT_3055.m_Children`）。⇒ 我们落成 `HudCanvas.EnsureSubCanvas(null, SubCanvasName, OverHand, 0)`。
//  · 🔴 **双模式（主对话裁定的硬要求）**：`UseHost(host)` **传了** ⇒ 画布档；**不传**（出厂）
//    ⇒ **世界档 —— 与改之前逐位相同**。先例 = `Tooltip.UseHost(host)` / `ErrorMessageBanner.Create(…, uguiHost)`。
//    ⚠️ 宿主由 **`BattleDriver`** 在建 HUD 时登记（组件是**场景挂的**，不像别的面板有 `Create`）
//    ⇒ 那一行在**白名单外**，见报告 §三 接线清单。**没登记 = 世界档**（⛔ 不是半迁移态）。
//  · **坐标**：两档共用同一组**面板内归一化**常量（`BgRect`/`NameRect`/…）——
//    世界档走 `InPanel`（世界单位）、画布档走 `CvPos`（设计 px，见它们各自的 doc）。
//    两档在 **16:9 下逐位相同**（画布那一侧的推导 → `Battle/HudCanvas.cs` 文件头）。
//  · **退场的口**（逐项 → `T09` §五·补④ 那张映射表 ＋ §五·补⑩ 关卡 1）：
//      `ImageQuad.Create(...)` → **`HudCanvas.MakeImageInParent(...)`**（⚠️ **中心锚**，**不是**
//        `MakeImage`/`QuadFromTopPx` 那套**归一化锚** —— 子画布 rect 在 `Expand` 档下比 1920×1080 高，
//        套归一化锚会把件**静默摆到别处**）；`Label.Create(...)` → **`HudCanvas.MakeText(...)`**（**设计 px**）；
//      `localPosition += (0,0,Z)` → 建件口那个 `z` 形参（画布上 z **只用来排兄弟序**，写进
//        `localPosition` 会把件推离画布平面）。**没有** `MenuDraw.SetPxSize` / 整屏覆盖层 ⇒ 用不上
//        `HudCanvas.SetPxSize` / `FillParent`（本面板的根**不是一个整屏矩形** —— 它在世界档里只有
//        「一个点 + 一组手算尺寸」，件才是载体）。
//  · 🔴🔴 **2026-10-16（`T09` 批 3 · 主对话裁定「按原版锚点补正」）：面板那一级从【固定设计 px】
//    改成【照原版锚点现算】** —— 见 `CvAnchorMinX` 那一组常量与 `PanelCvRect`。
//    判据 = **`T09` §五·补③ 一 的 2026-10-16 就地订正**（⛔ 不是旧那版「px 常量一律固定」）：
//    **「面板那一级与它的子件，锚点要照原版 `m_AnchorMin`/`m_AnchorMax` 逐级写；只有当原版那一级
//    是【双轴点锚】时，才等价于『固定设计 px』。」** 原版 `ActiveSkillDesc` 是**双轴部分拉伸**
//    （宽 30.02% × 高 30.40%）⇒ 必须真按锚点算，否则窄屏（画布被 `Expand` 撑高）下面板**不跟着变高**
//    （4:3 下只有原版的 **≈75%**）。
//  · ⚠️ **如实记着的一条差异**（本次**没做**、按 `T09` §五·补⑩ 二「差异如实记」）：
//    ① **节点层级**：原版是「父画布 → `ActiveSkillDesc`（**面板矩形那一级**）→ 7 个件」；
//       我们画布档是「子画布 → 7 个件」—— **少了 `ActiveSkillDesc` 那一级**
//       （我们这边对应它的是**世界空间那层壳** `transform`，它两档都不装画布上的件）。
//       ⇒ 那一级的锚点由 `PanelCvRect` **在算坐标时现算**（等价的几何、少一个节点）；⛔ 没自造中间层。
//    ② ✅ **原来这里记的那条「尺寸口径 = 固定设计 px」已按上面的裁定做掉** —— 不再欠。
//  · ⚠️ **两档在 16:9 下逐位相同 —— 这句话 2026-10-16 一度只对 6 颗件成立，现已恢复为「对全部 7 颗」
//    都成立**：`_light`（原版 `Lights`）那格曾按「画布档 = 原版锚点盒 / 世界档 = `A2106③` 的冻结值
//    `(0,0,1,1)`」分档（那是两条互相咬着的要求的折中），**主对话当天裁定「按原版」⇒ 冻结值已删、
//    两档统一读 `LightRect`（原版锚点盒）**。⇒ 世界档在这一件上是**往原版方向修缺陷**
//    （原来多铺 6.78% 宽 / 15.32% 高），⛔ 不是回归；其余 6 颗件的位/尺寸两档**逐值相同**
//    （差 ≤ 0.02 px：残差全部来自 `PanelCvRect` 现算（锚点全精度）与旧那对 2 位小数常量 `PanelPxW/H` 的舍入）。
//  · ⚠️ **本件没有「整屏压暗层」**（`T09` §八·5 那个选择题在这扇上**不成立**）：原版 `HideAbilityButton`
//    是一块**远大于面板的透明点击捕获区**，我们等价的语义是「点面板外 ⇒ `ClearSelection()` ⇒ 收起」
//    （由 `BattleDriver` 那条通用路走，**不在这扇面板里**）⇒ 本扇**零射线件**，`raycastTarget` 一条都不用改。
// ═════════════════════════════════════════════════════════════════════════════
using DG.Tweening;
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    public class SkillPanel : MonoBehaviour
    {
        // ---- 面板在屏幕上的位置（归一化，y 从**底部**算，和 `LayoutSpace.ToWorld` 一致）----
        // 671.8 / 1248.2 ÷ 1920、56.8 / 381.6 ÷ 1080（算式见文件头）
        // ⚠️ **这是【世界档】的尺**，也是**16:9 下画布档锚点算出来的那一格**（自证 → `PanelPxW` 的 doc）。
        const float PanelX0 = 0.3499f, PanelX1 = 0.6501f;
        const float PanelY0 = 0.0526f, PanelY1 = 0.3533f;
        /// <summary>原版面板在**参考分辨率（1920×1080）**下的像素尺寸 —— 子节点矩形是按它折算的。
        /// 🔴 **2026-10-16（`T09` 批 3 · 锚点补正）**：它现在**只剩两个用途**：
        /// ① **归一化分母**（`DescMarginPx / PanelPxW` 那种「面板内比例」）；② **自证基准**
        /// （`PanelCvRect` 在 1920×1080 下必须算出这一对数 —— 算式见 §⑤ 报告）。
        /// ⛔ **画布档的尺不再是它**——是 `PanelCvRect` 现算出来的 `size`（跟着画布高走）；
        /// 世界档的尺是 `PanelW/PanelH`（= 屏幕比例 × 30%，见它们）。两档只在这一处换算
        /// （`BoxW/BoxH`）—— ⛔ 别在调用点自己乘。</summary>
        const float PanelPxW = 576.41f, PanelPxH = 324.82f;

        // ── 🔴 2026-10-16（`T09` 批 3 · 主对话裁定「按原版锚点补正」）：面板那一级的**锚点五元组** ──
        //  出处 = **原版 `ActiveSkillDesc`（`RT_3104`）**，`bundle_scenes_scenes_battlearena2/
        //  RectTransform/RectTransform_3104.json` **逐字段现读**（2026-10-16）：
        //    `m_AnchorMin (0.665576696395874, 0.0)` · `m_AnchorMax (0.965788722038269, 0.3040129840373993)`
        //    `m_Pivot (0.5, 0.5)` · `m_AnchoredPosition (-606.110107421875, 55.0008544921875)`
        //    `m_SizeDelta (-0.0003662100061774254, -3.509999990463257)` · `m_LocalScale (1,1,1)`
        //  ⇒ **两轴都拉伸**（x 占画布宽 **30.02%** · y 占画布高 **30.40%**）。
        //  🔑 **`_cv`（子画布）就是原版那一级的父**（`Safe area FrontCanvas`）：它四边贴合根画布
        //     ⇒ `rect.min = −画布尺寸/2`、`rect` 的宽高 = 画布的设计 px 尺寸。公式见 `PanelCvRect`。
        //  ⛔ **逐值照抄、⛔ 别改数**（这是原版真值；改一个数 = 面板当场偏）。
        const float CvAnchorMinX = 0.665576696395874f;
        const float CvAnchorMinY = 0f;
        const float CvAnchorMaxX = 0.965788722038269f;
        const float CvAnchorMaxY = 0.3040129840373993f;
        const float CvPivotX = 0.5f, CvPivotY = 0.5f;
        const float CvAnchoredX = -606.110107421875f;
        const float CvAnchoredY = 55.0008544921875f;
        const float CvSizeDeltaX = -0.0003662100061774254f;
        const float CvSizeDeltaY = -3.509999990463257f;

        /// <summary>面板所在的 z —— 越负越靠前。压在 HUD（z=0）前面、选择器（-2.0）后面。
        /// ⚠️ 画布档下它**只喂给那个世界空间的壳** `transform`（`BattleDriver` 拿壳的位置当
        /// 「指针按在面板上」那个点）—— 画布上的件**不读**它（z 只用来排兄弟序，见下一行）。</summary>
        const float ZPanel = -1.80f;
        /// <summary>面板内的 z 分层（相对面板）。色片要**盖在字上面** —— 原版就是这么叠的。
        /// 🔴 **画布档下这三个数只用来登记兄弟序**（`HudCanvas.Add`：**z 大的排前面 = 画在底下**，
        /// 语义与旧那套**同向**）⇒ `ZLight`(−0.02) 最小 ⇒ 排在最后 = **画在最上面** ✓、`ZBg`(+0.02) 最大
        /// ⇒ 画在最底下 ✓ —— 与旧那套「`_light` 最靠前、`_bg` 最靠后」逐件一致。
        /// ⛔ **别把它们写进画布件的 `localPosition.z`**（那会把件推离画布平面）。</summary>
        const float ZBg = 0.02f, ZText = 0f, ZLight = -0.02f;

        /// <summary>本工程 1 世界单位 = 108 px（**世界档那一套尺**）—— 和 `AttackSelector` / `TargetReticle` 同口径。
        /// 🔴 画布档**不用它**（canvas 单位就是设计 px ⇒ 不除 108）—— 见 `BoxW/BoxH` / `MakeLabel`。</summary>
        const float PxPerUnit = 108f;
        static float WorldOfPx(float px) { return px / PxPerUnit; }

        // ---- 各子节点的矩形（**面板内**归一化：x 从左 0 到右 1，y 从**顶部** 0 到底部 1）----
        // 出处：`07_场景/battlearena1/RectTransform/`，按「pivot 世界位 = (anchorMin + Δanchor×pivot)
        // × 父尺寸 + anchoredPosition」逐级折算到面板坐标。7 个节点的 pivot 全是 (0.5,0.5)、
        // localScale 全是 1。
        // 🔴 **2026-10-16（`T09` 批 3 · 锚点补正）**：那一级现在是**锚点驱动**的（`PanelCvRect`）
        //   ⇒ 下面这些**面板内归一化矩形 = 原版子件的「锚点盒」**（`T09` §五·补③ 一 的口径：
        //   「面板那一级与它的子件，锚点要照原版 `m_AnchorMin`/`m_AnchorMax` 逐级写」）。
        //   4 个直接子件（`RT_3104.m_Children` = 3053/3322/3314/2818）里 **3 个是拉伸**，
        //   逐值（`bundle_scenes_scenes_battlearena2/RectTransform/RectTransform_<pid>.json` 现读，
        //   `sd` 全 (0,0)、`ap` 全 (0,0)、`pivot` 全 (0.5,0.5)，只有 `TargetsAvailableText` 的
        //   `ap/sd` 是 1e−5 量级的浮点噪音 ⇒ 盒 = `aMin…aMax`）：
        //     · `AbilityContainer`(3322) `aMin (0.068690665, 0.083098173)` / `aMax (0.912609994, 0.764358878)`
        //       → 盒 = x[0.0686907, 0.9126100]、y(顶算)[0.2356411, 0.9169018] ⇒ **= `BgRect`**（差 ≤ 1e−4）
        //     · `TargetsAvailableText`(2818) `aMin (0.158, 0.764358878)` / `aMax (0.844548225, 0.929000020)`
        //       → 盒 = x[0.1580000, 0.8445482]、y(顶算)[0.0710000, 0.2356411] ⇒ **= `TargetsRect`** ✓
        //     · `Lights`(3314) `aMin (0.030818868, 0)` / `aMax (0.963017464, 0.846788108)`
        //       → 盒 = x[0.0308189, 0.9630175]、y(顶算)[0.1532119, 1.0] ⇒ **见 `LightRect`**（⚠️ 与旧值不同）
        //     · `HideAbilityButton`(3053) 是**点锚** ⇒ 我们**没建**（属 `T09` 第 4 步「输入」）
        //   ⚠️ 上面两颗（`BgRect`/`TargetsRect`）**保留 4 位小数的原值、不改成锚点的全精度** ——
        //     这是**有意**的：改全精度会让**世界档**的件动 ≤ 0.006 px（违「世界档逐位不变」那条硬要求），
        //     而 4 位小数的误差在画布档 ≤ 0.006 px（半像素以下，看不见）。判据 → 报告 §④·3。
        // ⚠️ 底板**不是铺满面板的** —— 它只占 84.4% × 68.1%，偏左上。
        static readonly Rect BgRect = new Rect(0.0687f, 0.2356f, 0.8439f, 0.6813f);
        static readonly Rect NameRect = new Rect(0.1004f, 0.2806f, 0.3295f, 0.2059f);
        // 🔴 **2026-10-10 更正（铁律 5 · `A1242①`）：这两颗原来归一化错了参考帧。**
        //    改前 `CostIconRect`/`CostTextRect` = (0.7409, 0.0829, 0.0837, 0.3057) /
        //    (0.8385, 0.0812, 0.1155, 0.3074)，而**上面那五颗**（`Bg` / `Name` / `Desc` / `Targets`）
        //    都是按**面板** `ActiveSkillDesc`(576.41×324.82) 归一化的 —— 那两颗却是按
        //    **`AbilityContainer`**(486.44×221.29，= 本文件 `BgRect` 那一颗) 归一化的
        //    （实据：`CostText` 的 x/w = `(1119.28−711.39)/486.44` = **0.83857**、`56.19/486.44` = **0.11549**，
        //     与改前那两个数**逐位相同**；`CostIcon` 的 x = `(1071.80−711.39)/486.44` = **0.74099** 同）。
        //    后果有两处，不是一处：① **位置**（`CenterOf` 摆的是错帧的心 ⇒ 屏上 x 1155.1 vs 原版 1119.3，差 35.8px）；
        //    ② **框**（`FitBox`/`Fit` 吃的宽高同源 ⇒ 66.58×99.85 vs 原版 56.20×68.40）。
        //    判据 = **现读**（`menu_dump.py bundle_scenes_scenes_battlearena1 "ActiveSkillDesc" --depth 4`，
        //    用 **`P-L` 修好之后**的尺子）：
        //      `CostText  1119.28,792.55→1175.47,860.95  56.20×68.40`（`'10'` 52.4 em，Center/Middle）
        //      `CostIcon  1071.80,792.92→1112.53,860.95  40.73×68.03`（无图 · `preserveAspect`）
        //    归一化（口径与那五颗**逐字相同**：x ÷ 面板宽、y 从**面板顶部**起算）：
        //      `(x−671.80)/576.41` · `(y−698.42)/324.82`
        //    ⚠️ `CostIcon` 自身在原版 prefab 里是 **`m_IsActive = 0`**（`menu_dump` 那一列的 `act=F`）——
        //      但它的**矩形是真的**，`_manaIcon` 就按它摆；改后 `min(w,h)` = 40.73 ⇒ 方图边长 = 原版图宽 ✓。
        static readonly Rect CostIconRect = new Rect(0.6939f, 0.2909f, 0.0707f, 0.2095f);
        static readonly Rect CostTextRect = new Rect(0.7763f, 0.2898f, 0.0975f, 0.2106f);
        static readonly Rect DescRect = new Rect(0.1009f, 0.5004f, 0.7140f, 0.3335f);
        /// <summary>= 原版 `TargetsAvailableText`(2818) 的锚点盒（见上面那一段逐值）。</summary>
        static readonly Rect TargetsRect = new Rect(0.1580f, 0.0710f, 0.6865f, 0.1646f);
        /// <summary>原版 `Lights`(RT **3314**) 那个**容器**的锚点盒 —— **两档都读它**（见下面那段更正痕）。
        /// 🔴🔴 **2026-10-16（`T09` 批 3）就地订正（铁律 5）—— `A2106③`（2026-10-15）改错了。**
        /// 它当时把这里改成 `(0, 0, 1, 1)`（全 stretch 面板），理由是
        /// 「原版三层是【全 stretch】…… 判据 → `C2`（引 `B2:74-76`）」。
        /// **错因 = 把 `Lights` 的【子件】当成了 `Lights` 自己**：
        ///   · `B2:74-76` 那三行写的是 `LightActing` / `LightAvailable` / `LightPressed` —— 它们是
        ///     `Lights` 的**三个子件**、各自 `anchor (0,0)-(1,1)`（相对 **`Lights`**，不是相对面板）；
        ///   · **`Lights` 自己那一行是 `B2:73`**，白纸黑字 `(0.031,0)/(0.963,0.847)` —— 与本节的值一致；
        ///   · `RT_3314` 现读：`aMin (0.030818868428468704, 0)` / `aMax (0.963017463684082, 0.8467881083488464)`
        ///     · `sd (0,0)` · `ap (0,0)` · `pivot (0.5,0.5)`。
        /// ⇒ 盒 = x[0.0308189, 0.9630175]、y(顶算)[0.1532119, 1.0]，**正是 `A2106③` 之前那个值**
        ///   （`(0.0308, 0.1532, 0.9322, 0.8468)`）—— 那次「订正」把一个**本来对的值改错了**
        ///   （6.8% 宽 / 15.3% 高的差，方向与它写的相反）。
        /// ⚠️ `Lights` 那个节点**自己没有任何 `Image`**（`B2:73` 组件列是 `—`），三层色片铺满它
        ///   ⇒ **画出来的范围 = 这个盒**（不可能更大）。</summary>
        static readonly Rect LightRect = new Rect(0.0308189f, 0.1532119f, 0.9321986f, 0.8467881f);
        /// <summary>⚠️ **2026-10-16（主对话裁定 · 本轮）**：这里**原来还有一颗 `LightRectWorldFrozen
        /// = (0,0,1,1)`**（「世界档冻结值」，`A2106③` 留下的），现**已删除 —— 两档统一读 `LightRect`**。
        /// <para>**为什么删**（裁定三条）：① `A2106③` 本身是把**一个本来对的值改错了**（它引的 `B2:74-76`
        /// 说的是 `Lights` 的三个**子件**，而 `Lights` 自己在 `B2:73` = `(0.031,0)/(0.963,0.847)`、
        /// 节点自身**没有 `Image`** ⇒ 画出来的范围就是那个盒）；② 「世界档逐位不变」那条规矩是
        /// **防回归**的、**不是冻结错误**的 —— 现状本身是缺陷（多铺 6.78% 宽 / 15.32% 高），
        /// 按**铁律 11**（与原版不符的全部要做）**必须改**；③ 同批先例 = `ChatPopupPanel` 世界档那 6 个
        /// z 常量的层序也是反的，裁定一模一样（**照原版改，⛔ 别拿「逐位相同」挡它**）。</para>
        /// <para>⇒ **世界档这一件也往原版方向变了**（从铺满面板 → 93.2%×84.7%）—— 这是**修缺陷**，
        /// ⛔ 不是回归。除此之外世界档仍然逐位不变（`B2` 那批常量与 `PanelW/H` 一个字没动）。</para></summary>

        /// <summary>原版 `DescText` 的 `m_margin.x = 7.4727 px` —— 左对齐的起笔位置</summary>
        const float DescMarginPx = 7.4727f;

        // 四个字号是 **em**（UGUI 的 fontSize 就是 em 大小），而我们的文案是**中文** ——
        // 一个汉字约占 1 em。所以按「汉字多高」定档（`Label.SetGlyphHeight`），
        // **不要**套 `SetCapHeight`（那是拉丁大写，只有约 0.72 em，会让字大 ~39%）。
        const float NamePx = 42.35f, CostPx = 52.4f, DescPx = 37.35f, TargetsPx = 56.45f;
        // 原版四个字的字体：Name/Cost/Desc 是 Asar-Regular SDF（PointSize 94 / CapLine 61），
        // Targets 是 Pragati-Regular SDF（95 / 60）。我们统一用中文那份 —— 反正要出汉字。

        // 🆕 **2026-10-10（A1212 块 7）**：原版那四颗 TMP 的自适应三格 —— **四颗逐值完全相同**。
        // 出处 = `资料/普查产出_第十会话/R6_包装层战斗与其余.md` §2·C（`:143/:144/:146/:147` 那四处）
        // 与 §3 的行 23–26：`bundle_scenes_scenes_battlearena1 ▸ FrontCanvas/Safe area FrontCanvas/
        // ActiveSkillDesc/AbilityContainer/{NameText,CostText,DescText,TargetsAvailableText}`
        // 四格 = `4 / 60 / 36 / 1`（`m_fontSizeMin 4` · `m_fontSizeMax 60` · `m_fontSizeBase 36` ·
        // `m_TextWrappingMode 1`）；归组 = `资料/普查产出_第十一会话/RA1212_切块表.md` 块 7。
        // ⚠️ **走 `Label.SetAutoFitBox` 那条路 ⇒ 没有 `MenuDraw.TextCore` 那三道闸**；但量纲要自己认：
        //   框宽/框高走**本档的尺寸单位**、`min/max/base` 走**原版画布 px**
        //   ⇒ 三个数**原样传、⛔ 不乘任何系数**。
        //   🔴 **2026-10-16（`T09` 批 3）就地订正（铁律 5）**：本行原来写「框宽/框高走**世界单位**
        //   （`W01/H01` 正好就是世界单位）」—— 那**只对世界档成立**：`SetAutoFitBox` 的第一个形参
        //   直接进 TMP 的 `sizeDelta`，而画布档的 `sizeDelta` 单位是**设计 px**
        //   （⇒ 世界档走 `W01/H01`、画布档走 `Wpx/Hpx`，调用点一律经 `BoxW/BoxH`）。
        //   喂错的后果**不报错**：画布档喂世界单位 ⇒ 框宽 5.34 px、字号收敛到一个小得看不见的值。
        // ⚠️ 折行 = **1** ⇒ 不用还原（`SetAutoFitBox` 内部那句 `SetWrapWidth` 顺带开的就是 `Normal(1)`）。
        const float AutoMinPx = 4f, AutoMaxPx = 60f, AutoBasePx = 36f;

        /// <summary>原版 `fadeTime`</summary>
        public const float FadeTime = 0.2f;

        /// <summary>面板底色。原版那块 Image 是**纯白无 sprite**（见文件头）—— **这块深色板是我们挑的**</summary>
        static readonly Color BgColor = new Color(0.05f, 0.06f, 0.09f, 0.86f);
        /// <summary>🔴 **2026-10-15（账 `A2106`）就地订正：原版 Name / Cost 是【白】。**
        /// 原来这里是暖色 `(1,0.96,0.86)` / `(1,0.94,0.72)` —— **未标注的偏移**（既不是照原版、也没写「我们挑的」）。
        /// 判据 → `资料/普查产出_1015战斗/C2_逐格对账_弹窗与结算转场.md`（引 `B2:68,70`）。</summary>
        static readonly Color NameColor = new Color(1f, 1f, 1f);
        /// <inheritdoc cref="NameColor"/>
        static readonly Color CostColor = new Color(1f, 1f, 1f);
        static readonly Color DescColor = new Color(0.90f, 0.92f, 0.96f);
        static readonly Color TargetsColor = new Color(0.96f, 0.90f, 0.60f);

        // ---- 原版 `Lights`：三层**全铺**色片，靠开关切换（不是一排小灯）----
        // 颜色原样照抄，alpha 0.392 也是原版的。
        // ⚠️ 交接文档之前把颜色写反了 —— 原件里 `LightActing` 是白、`LightAvailable` 才是黄绿。
        static readonly Color LightActing = new Color(1.000f, 1.000f, 1.000f, 0.392f);
        static readonly Color LightAvailable = new Color(0.925f, 1.000f, 0.000f, 0.392f);
        static readonly Color LightPressed = new Color(0.006f, 0.000f, 1.000f, 0.392f);

        /// <summary>
        /// 面板上铺哪一层色片。三态的触发条件是**从 IL2CPP dump 里查出来的**：
        ///   · **蓝 `LightPressed`** —— 指针按在面板上（`ClickDown` 点亮、`PointerExit`/`Up` 熄灭）**确定**
        ///   · **白 `LightActing`** —— 技能**正在结算**（`TryUsingActiveAbility` 成功后点亮）**确定**
        ///   · **黄绿 `LightAvailable`** —— 可用性指示（`DisableUsableLight()` 把它瞬间淡到 0）。
        ///     这一条是**从函数名推的**，原码里没有一行显式把它设成 1
        /// </summary>
        public enum Light { None, Available, Pressed, Acting }

        // ==================================================================

        /// <summary>🔴 **2026-10-16（`T09` 批 3）：类型 `ImageQuad` → `HudPiece`** —— 画布档下这三颗是
        /// **uGUI 件**（`RectTransform + Image`）。⛔ **一个件只许在一边**（要么旧 quad、要么 uGUI）——
        /// 见 `HudPiece` 文件头那条铁律。</summary>
        HudPiece _bg, _light, _manaIcon;
        Label _name, _cost, _desc, _targets;
        Texture2D _solid;
        bool _built;

        // ---- 🆕 2026-10-16（`T09` 批 3）：画布那一侧 --------------------------------------------
        /// <summary>本面板那块 **uGUI 子画布**的 `RectTransform`（`OverHand` / order 0）。
        /// <para>`null`（出厂）= **世界档** —— 与改之前**逐位相同**；非 null = **画布档**
        /// （件建在它下面、坐标 = **设计 px**）。⛔ **绝不许两边都建**（同 `HudPiece`/`Label` 那条铁律）。</para>
        /// <para>它是 `EnsureSubCanvas` 的返回值 ⇒ **四边贴合根画布**（锚 0–1、`sd` 0、枢轴居中）
        /// ⇒ **它的中心 = 画布中心、局部单位 = 设计 px**（`T09` §五·补③ 一）。</para></summary>
        RectTransform _cv;

        /// <summary>本面板那块子画布的名字（= 原版宿主名 + `" Canvas"`，见 `T09` §五·补⑩ 第 1 步）。
        /// 判据 = `T09c` §一 我这一行：`ActiveSkillDesc` @ `…/Safe area FrontCanvas`，
        /// **`OverHand` / order 0 / 兄弟序 1/14**。⛔ **只此一份**（建它、自检找它都取这里）。</summary>
        public const string SubCanvasName = "ActiveSkillDesc Canvas";

        /// <summary>本面板现在是不是**画布档**（诊断 / 断言 / 自检用；**判据只此一处**）。</summary>
        public bool UguiMode { get { return _cv != null; } }

        /// <summary>自检用：本面板那块**子画布**的 `RectTransform`（查它的层/order/`overrideSorting` 用）。</summary>
        public RectTransform CanvasRect { get { return _cv; } }

        // ── 🔴 2026-10-16（锚点补正）：画布档那块**面板矩形**的三个只读口（自检 / 断言用）──────────
        //  它们读的是 `PanelCvRect` —— **同一处判据**（`Contains` / 件位置 / 尺寸全读它），
        //  所以断言拿它去比「件的实绘矩形」是自洽的。
        //  ⚠️ **期望值要取【判据侧】**（原版 `RT_3104` 的锚点五元组 + `HudCanvas.RootRect.rect`），
        //     ⛔ 别拿本口算出来的数当期望值 —— 那是拿实现证明实现（同义反复）。

        /// <summary>画布档：面板矩形的**设计 px 尺寸**（= `(aMax−aMin) × 画布尺寸 + sizeDelta`）。
        /// ⛔ 世界档没有这一级 ⇒ 返回 `(0,0)`（别拿它当世界档的尺，那个是 `PanelW/PanelH`）。</summary>
        public Vector2 PanelCvSize { get { return UguiMode ? PanelCvRect.size : Vector2.zero; } }

        /// <summary>画布档：面板矩形中心在**子画布局部**的坐标（设计 px、中心原点、y 向上）。</summary>
        public Vector2 PanelCvCenter { get { return UguiMode ? PanelCvRect.center : Vector2.zero; } }

        /// <summary>画布档用的那把**父尺寸**（= `_cv.rect` 的宽高，设计 px；量不到时退参考分辨率
        /// 1920×1080，见 <see cref="CanvasPx"/>）。断言用它自己独立算一遍期望矩形。</summary>
        public Vector2 CanvasPxSize { get { return CanvasPx; } }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）：把本面板接到 uGUI 档。**
        /// <paramref name="host"/> = **本面板那块子画布的 `RectTransform`**
        /// （= `HudCanvas.EnsureSubCanvas(null, SubCanvasName, HudCanvas.SortingLayer.OverHand, 0)` 的返回值）。
        ///
        /// <para>🔴 **为什么必须是「建 UI 的那一方」来登记**（同 `Tooltip.UseHost` 那条）：组件是**场景挂的**
        /// （`AddComponent<SkillPanel>()`，没有 `Create`）⇒ 只有驱动层知道根画布建好了没。</para>
        ///
        /// <para>传 `null` = **子画布没建起来**：**留在世界档**（= 与改之前逐位相同）、并**出声**
        /// （⛔ 不静默 —— 那正是「一半在画布、一半在世界」那种 16:9 下看不见的半迁移态）。</para>
        ///
        /// <para>⚠️ **可以在 `Build()` 之后再调**：那时会**拆掉世界档的件、按画布档重建**
        /// （`Awake` 会先跑一次 `Build()`，而驱动层登记宿主的时刻由别人定 —— 两种次序都要能落地）。
        /// 拆旧建新是**同一个件只许在一边**那条铁律的落实，⛔ **不是**「两边都建」。</para></summary>
        public void UseHost(RectTransform host)
        {
            if (host == null)
            {
                Debug.LogError("[Battle] 技能面板：`UseHost(null)` —— 那块 `OverHand` 子画布没建起来"
                             + " ⇒ 本面板**留在世界档**（画面与改之前逐位相同，"
                             + "但非 16:9 下会与已迁到画布的同族面板分家）");
                return;
            }
            if (_cv == host) return;
            _cv = host;
            if (_built)
            {
                Debug.Log("[Battle] 技能面板：`UseHost` 排在 `Build()` 之后 ⇒ 拆掉世界档的件、按画布档重建"
                        + "（⛔ 不许两边都建）。把登记挪到第一次 `Build()` 之前可以省掉这一次重建。");
                Teardown();
                Build();
            }
        }

        /// <summary>拆掉本面板已经建出来的件 —— **只用于换档**（世界档 ↔ 画布档）。
        /// ⛔ 别拿它当「收起面板」用（收起只走 `HideImmediate`：显隐只靠 alpha，见它的 doc）。</summary>
        void Teardown()
        {
            KillGo(_bg != null ? _bg.gameObject : null);
            KillGo(_light != null ? _light.gameObject : null);
            KillGo(_manaIcon != null ? _manaIcon.gameObject : null);
            KillGo(_name != null ? _name.gameObject : null);
            KillGo(_cost != null ? _cost.gameObject : null);
            KillGo(_desc != null ? _desc.gameObject : null);
            KillGo(_targets != null ? _targets.gameObject : null);
            _bg = _light = _manaIcon = null;
            _name = _cost = _desc = _targets = null;
            if (_fade != null) { _fade.Kill(); _fade = null; }
            _pressed = _acting = false;
            ShownName = null;
            ShownTargets = 0;
            CurrentLight = Light.None;
            Alpha = 0f;
            Visible = false;
            _built = false;                 // ⇒ 下一次 `Build()` 会用**当前**档重建
        }

        static void KillGo(GameObject o)
        {
            if (o == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(o);
            else UnityEngine.Object.DestroyImmediate(o);
        }

        /// <summary>面板现在开着吗</summary>
        public bool Visible { get; private set; }
        /// <summary>正在显示哪个技能（自检断言用）。没开着是 null</summary>
        public string ShownName { get; private set; }
        /// <summary>现在显示的可选目标数（自检断言用）</summary>
        public int ShownTargets { get; private set; }
        /// <summary>现在铺的是哪层色片（自检断言用）</summary>
        public Light CurrentLight { get; private set; }
        /// <summary>现在淡到多少（0~1，自检断言用）</summary>
        public float Alpha { get; private set; }

        /// <summary>🆕 **2026-10-17（`A1212` 块 7 的断言口）**：那四颗字（0=`NameText` · 1=`CostText` ·
        /// 2=`DescText` · 3=`TargetsAvailableText`）。**只给自检读** —— 生产路径不经过它。
        /// <para>⚠️ 为什么不按节点名找：画布档下它们挂在**共用子画布** `BattleSettingsPanel`/`ActiveSkillDesc`
        /// 那一层，`Find` 只能拿第一个同名件；本类自己持有引用，直接交出去最准。</para></summary>
        public Label TextForTest(int i)
        {
            switch (i)
            {
                case 0: return _name;
                case 1: return _cost;
                case 2: return _desc;
                case 3: return _targets;
            }
            return null;
        }

        bool _pressed, _acting;
        Tween _fade;

        void Awake() { Build(); }

        public void Build()
        {
            if (_built) return;
            _built = true;
            CardArt.Load();

            if (UguiMode) BuildCanvas();
            else BuildWorld();

            HideImmediate();
            RefreshLayout();
        }

        /// <summary>世界档建件（**改之前那一版** —— ⛔ 不许在这里混进画布那一路）。
        /// ⚠️ **只有一处**与改之前不同：三颗图过一道 `HudPiece.Of(...)`（字段类型换成了适配层，
        /// 见 `HudPiece` 文件头）—— 建出来的**仍然是旧的世界空间 quad**，逐位不变。</summary>
        void BuildWorld()
        {
            _bg = HudPiece.Of(ImageQuad.Create(transform, Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "AbilityContainer"));
            _light = HudPiece.Of(ImageQuad.Create(transform, Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "Lights"));

            _name = MakeLabel("NameText", NamePx, NameColor);
            _cost = MakeLabel("CostText", CostPx, CostColor);
            // 原版 `DescText` 是**左对齐**（H=1）+ `m_margin.x = 7.4727` → 锚点在左边
            _desc = MakeLabel("DescText", DescPx, DescColor, new Vector2(0f, 0.5f));
            _targets = MakeLabel("TargetsAvailableText", TargetsPx, TargetsColor);

            // 原版那格的图标是 `manaIcon`（原件里 `costSprite` 是 null，运行时才赋）——
            // 用 HUD 那颗能量宝石那张图占位，方图居中放进它的矩形
            // ⚠️ `ImageQuad.Create` 在贴图为空时**返回 null**（`HudPiece.Of(null)` 也返 null）——
            //    各读口都有空判，照旧。
            _manaIcon = HudPiece.Of(ImageQuad.Create(transform, CardArt.Ui("UI_Energy_Eldar"), Vector3.zero, 0.2f,
                                                     new Vector2(0.5f, 0.5f), "manaIcon"));
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）画布档建件。**
        /// **位置/尺寸一概不在这里定** —— 全在 `RefreshLayout`（两档共用同一组面板内归一化常量，
        /// 判据只此一处）。这里只管「建出来 + 定层档（兄弟序）+ 定字号默认值」。
        /// <para>⚠️ **走 `MakeImageInParent`（中心锚、「相对父中心」）而不是 `MakeImage`/`QuadFromTopPx`**
        /// （那两个把 `x/y` 当**屏幕绝对 px**、锚点是归一化的）—— 见它自己的 doc：
        /// 子画布 rect 在 `Expand` 档下比 1920×1080 高 ⇒ 套错了会**静默摆到别处**（`T09` §七·10）。</para></summary>
        void BuildCanvas()
        {
            _bg = MakeCv(Solid(), "AbilityContainer", ZBg);
            _light = MakeCv(Solid(), "Lights", ZLight);

            _name = MakeLabel("NameText", NamePx, NameColor);
            _cost = MakeLabel("CostText", CostPx, CostColor);
            // 原版 `DescText` 是**左对齐**（H=1）+ `m_margin.x = 7.4727` → 锚点在左边
            _desc = MakeLabel("DescText", DescPx, DescColor, new Vector2(0f, 0.5f));
            _targets = MakeLabel("TargetsAvailableText", TargetsPx, TargetsColor);

            var gem = CardArt.Ui("UI_Energy_Eldar");
            _manaIcon = MakeCv(gem, "manaIcon", ZText);
            if (_manaIcon == null)
                Debug.LogWarning("[Battle] 技能面板：费用图标那张图 `UI_Energy_Eldar` 取不到"
                               + " ⇒ 那一格**不画**（照旧出声，不静默；⛔ 也不为此退回世界空间 quad）");
        }

        /// <summary>画布档建一块**纯色/贴图**件（尺寸随后由 `RefreshLayout` 的 `PlaceQuad` 写 ——
        /// 两档各一份实现，见它）。贴图为空 ⇒ 返回 `null`（⛔ 不静默建一块白图）。</summary>
        HudPiece MakeCv(Texture2D tex, string name, float z)
        {
            if (tex == null) return null;
            // 出厂尺寸 = 贴图本身那一档（立刻会被 `PlaceQuad` 按面板内矩形覆盖）
            return HudCanvas.MakeImageInParent(_cv, tex, name, 0f, 0f, tex.width, tex.height, z, Color.white);
        }

        /// <summary>建一颗字。两档的字号**数不同**（世界档 = px ÷ 108、画布档 = px）——
        /// 口径 → `T09` §五·补⑨ 四（⛔ 喂错**不报错**、只是小到看不见）。</summary>
        Label MakeLabel(string name, float px, Color c, Vector2? anchor = null)
        {
            var a = anchor ?? new Vector2(0.5f, 0.5f);
            if (UguiMode)
            {
                // 位置由 `RefreshLayout` 写（建的时候先落在子画布中心）
                var u = HudCanvas.MakeText(_cv, "", 0f, 0f, 1, c, a, ZText, name);
                if (u != null) u.SetGlyphHeight(px);          // ⚠️ **设计 px**（⛔ 不除 108）
                return u;
            }
            var l = Label.Create(transform, "", Vector3.zero, 1, c, a, name);
            l.SetGlyphHeight(WorldOfPx(px));
            return l;
        }

        /// <summary>1×1 纯色图。底板和色片都是纯色块 —— 原版那几个 sprite 本来就全是 null</summary>
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

        static Color WithAlpha(Color c, float a) { c.a *= a; return c; }

        float PanelW { get { return LayoutSpace.VisibleWidth * (PanelX1 - PanelX0); } }
        float PanelH { get { return LayoutSpace.VisibleHeight * (PanelY1 - PanelY0); } }

        /// <summary>面板内归一化坐标（x 从左 0 到右 1，y 从**顶部** 0 到底部 1）→ 局部坐标</summary>
        Vector3 InPanel(float x01, float y01, float z)
        {
            return new Vector3((x01 - 0.5f) * PanelW, (0.5f - y01) * PanelH, z);
        }

        Vector3 CenterOf(Rect r, float z)
        {
            return InPanel(r.x + r.width * 0.5f, r.y + r.height * 0.5f, z);
        }

        /// <summary>
        /// 按当前分辨率摆一遍（幂等）。
        /// <para><b>世界档</b>：面板是**屏幕的固定比例**（30% 宽 × 30% 高），宽高比跟着屏幕走 ——
        /// 只摆一次的话切分辨率就错位（HUD 那几个 Label 踩过一模一样的坑）。</para>
        /// <para><b>画布档</b>：🔴 **2026-10-16（锚点补正）后它【也要】重排** ——
        /// 面板矩形由 <see cref="PanelCvRect"/> 从**画布尺寸**现算，而 `Expand` 档下画布高随屏变
        /// （4:3 ⇒ 1440 单位、16:9 ⇒ 1080、21:9 ⇒ 画布宽 2560）⇒ **换了宽高比就得重跑本函数**
        /// （`CanvasScaler` 只管缩放，管不了「锚点盒随父尺寸变」这件事）。
        /// 换分辨率的既有入口 = `BattleDriver.ReanchorHud` → 本函数（那一句**照旧留着**，⛔ 别摘）。
        /// ⚠️ 它仍然幂等；同一宽高比下重跑 = 一个字节都不改。</para>
        /// </summary>
        public void RefreshLayout()
        {
            if (!_built) return;

            // 世界档：面板是**屏幕的固定比例**（30% 宽 × 30% 高），宽高比跟着屏幕走 ——
            //   只摆一次的话切分辨率就错位（HUD 那几个 Label 踩过一模一样的坑）。
            // 🔴 **2026-10-16（`T09` 批 3）**：画布档的件**不靠这一段定位**（它们跟 `PanelCvRect` 走）——
            // 🔴 **但这一句两边都留着**（⛔ 别按档跳过）—— 那个世界空间的**壳** `transform` 还要用：
            //   `BattleDriver` 拿 `SetPointer(sp.transform.position, …)` 当「指针按在面板上」那个点，
            //   而 `Contains` 画布档量的是**件真正画在哪**。两档下这个点都在命中框里：
            //   · 真正的 16:9 ⇒ **两档逐位重合**（面板中心 0.20293 vs 0.20295，差 2e−5 ≈ 0.0002 世界单位）；
            //   · 别的宽高比 ⇒ 差一个「面板随画布走」的量：**4:3 下壳在 −2.9705、面板中心在 −2.2280**
            //     （差 **0.74** 世界单位），而画布档那块面板的半高有 **1.51** 世界单位 ⇒ **仍落在框内**
            //     （`Editor/BattleScene.cs` 那条「指针按在面板上 → 铺蓝那层」靠的正是这一点）；
            //     **21:9 同理**（面板中心右移 1.84、半宽 3.56 ⇒ 壳仍在框内）。
            //   ⚠️ 如实记：**它不是「件画在哪」那个点**（画布档要那个点得走 `_cv.TransformPoint`，
            //   而批处理下画布还没排过版、那份反算不可信）—— 属「只换半边」那条已知代价。
            {
                var c = LayoutSpace.ToWorld((PanelX0 + PanelX1) * 0.5f, (PanelY0 + PanelY1) * 0.5f);
                transform.localPosition = new Vector3(c.x, c.y, ZPanel);
            }

            PlaceQuad(_bg, BgRect, ZBg);
            // ⚠️ `_light` **两档同值**（原版 `Lights` 的锚点盒）—— 2026-10-16 主对话裁定删掉了那颗
            //    「世界档冻结值」，见 `LightRect` 上面那段更正痕。
            PlaceQuad(_light, LightRect, ZLight);

            // 卡名 / 费用 / 目标数：居中放进各自的矩形
            Place(_name, NameRect, ZText);
            Place(_cost, CostTextRect, ZText);
            Place(_targets, TargetsRect, ZText);

            // 说明：**左对齐**，起笔位置 = 矩形左边 + 原版那个 margin
            // 🔴 **2026-10-16（锚点补正）**：那一格改用 `MarginX01()` —— 画布档的**面板宽**现在会随
            //   宽高比变（`Expand` ⇒ 21:9 下画布宽 2560、面板宽 768.54 px），而原版那个
            //   `m_margin.x = 7.4727` 是**文字自己的 px**（= canvas px，**不随面板缩放**）
            //   ⇒ 画布档必须按**现算的面板宽**折（用写死的 `PanelPxW` 会在 21:9 下差 2.5 px，
            //   而且会与下面 `FitBox(... - MarginW())` 那条框**对不上** —— `MarginW` 走的是固定 px）。
            Place(_desc, DescRect.x + MarginX01(),
                        DescRect.y + DescRect.height * 0.5f, ZText);

            // ⚠️ 原版四个字都开着 `m_enableAutoSizing`（min 4 / max 60），序列化字号是
            //    **编辑器烘进去的「上次结果」**。这里用「装不进矩形就整体回缩」逼近它 ——
            //    像 `TargetsAvailableText`（56.45 em）塞进 53.5 px 高的矩形，原版一定是缩过的。
            // 🆕 **2026-10-10（A1212 块 7）**：上面那句「逼近」现在**真的接上了** —— 四颗各自按
            //    `SetAutoFitBox` 开原版那套自适应（`4/60/36`，见 `AutoMinPx` 那一组常量）；
            //    框就是下面 `Fit` 用的**同一个矩形**（同一条算式，⛔ 别让它俩对不上）。
            //    ⚠️ 顺序：**先 `SetAutoFitBox`、再 `Fit`** —— 前者会重排 mesh（`ForceMeshUpdate`）并按
            //    「装得下的最大号」收敛，后者读的是**收敛之后**的 `WorldW/WorldH`
            //    ⇒ 收敛成功时它那句 `k >= 1f` 早退、**一个字节都不改**（保留它 = 兜底，不是第二套规则）。
            // 🔴 **2026-10-16（`T09` 批 3）**：框的两个量纲**按档取**（`BoxW/BoxH`）——
            //    `SetAutoFitBox` 的第一个形参是「**本标签所在空间的单位**」（`Label.SetWrapWidth` 那句
            //    `sizeDelta` 直接吃它）⇒ 世界档喂世界单位、画布档喂**设计 px**。
            //    ⛔ 喂错**不报错**：画布档喂世界单位 ⇒ 框宽 5.34 px（而不是 576 px）、字收敛到一个小得看不见的值。
            FitBox(_name, BoxW(NameRect.width), BoxH(NameRect.height));
            // 🔴 **2026-10-17（`A1226` 表 B #24）：`CostText` 的框宽改成【原版有效框】56.20。**
            //    判据 = 原版 `…/AbilityContainer/CostText` 的 `RectTransform`（伸锚）⇒ **56.20 × 68.40**
            //    （我们原来的归一化矩形量出来 66.58 × 99.85 ⇒ 宽 +10.38 / 高 +31.45）。
            //    ⚠️ **只跟宽、框高【故意不跟】**：68.40 是 Pragati 行盒 0.9474em 下的数（52.4px 的字占 49.6 < 68.4，
            //    原版不缩）；我们全仓只有 CJK 那一份字体（行盒 **1.516em**）⇒ 喂 68.40 会压到 45.1px（原版渲 52.4）。
            //    ⇒ 等 `A1024③` 按用途换面之后，框高再一起改（已在 T07 报告登记）。
            FitBox(_cost, UguiMode ? 56.20f : WorldOfPx(56.20f), BoxH(CostTextRect.height));
            FitBox(_targets, BoxW(TargetsRect.width), BoxH(TargetsRect.height));
            FitBox(_desc, BoxW(DescRect.width) - MarginW(), BoxH(DescRect.height));

            Fit(_name, BoxW(NameRect.width), BoxH(NameRect.height));
            Fit(_cost, BoxW(CostTextRect.width), BoxH(CostTextRect.height));
            Fit(_targets, BoxW(TargetsRect.width), BoxH(TargetsRect.height));
            Fit(_desc, BoxW(DescRect.width) - MarginW(), BoxH(DescRect.height));

            PlaceManaIcon();
            ApplyColors();
        }

        /// <summary>面板内归一化宽度 → **本档的尺寸单位**（世界档 = 世界单位 · 画布档 = 设计 px）。
        /// ⛔ **别在别处自己乘** —— 两档混一次就是「只换半边」（`T09` §五·补⑦ 那条）。</summary>
        float BoxW(float x01) { return UguiMode ? Wpx(x01) : W01(x01); }
        /// <inheritdoc cref="BoxW"/>
        float BoxH(float y01) { return UguiMode ? Hpx(y01) : H01(y01); }
        /// <summary>`DescText` 那个左对齐 margin，**本档的单位**（原版值是 7.4727 **px**）。</summary>
        float MarginW() { return UguiMode ? DescMarginPx : WorldOfPx(DescMarginPx); }
        /// <summary>`DescText` 那个 margin 折成**面板内归一化 x**（给 `Place(_desc, …)` 用）。
        /// <para>原版 `m_margin.x = 7.4727 px` 是**文字自己的 px**（= canvas px、**不随面板尺寸缩放**）⇒
        /// <b>画布档按现算的面板宽折</b>（⛔ 别用写死的 `PanelPxW` —— 21:9 下面板宽 768.54，
        /// 写死会让起笔位置偏 2.5 px，并与 `FitBox` 那条框对不上）。</para>
        /// <para>⚠️ **世界档那一支逐位不变**（就是原来的 `DescMarginPx / PanelPxW`）—— 它的尺是
        /// `PanelW`（屏幕比例 × 30%），改成 ÷`PanelW` 会让世界档在非 16:9 下位移（违「世界档逐位不变」）。</para></summary>
        float MarginX01()
        {
            if (!UguiMode) return DescMarginPx / PanelPxW;
            return DescMarginPx / Mathf.Max(1e-4f, PanelCvRect.width);
        }

        float W01(float x01) { return x01 * PanelW; }
        float H01(float y01) { return y01 * PanelH; }

        /// <summary>面板内归一化宽度 → **设计 px**（**画布档的尺** —— 尺本身 = 现算的面板宽）。
        /// ⛔ 别与 `W01`（世界档的尺）混。</summary>
        float Wpx(float x01) { return x01 * PanelCvRect.width; }
        /// <inheritdoc cref="Wpx"/>
        float Hpx(float y01) { return y01 * PanelCvRect.height; }

        // ── 🆕 2026-10-16（`T09` 批 3）：画布档的坐标/尺寸（**与建件那一侧成对的逆**）────────────
        /// <summary>🔴 **2026-10-16（`T09` 批 3 · 锚点补正）：画布档的【父尺寸】= 子画布自己的 rect**
        /// （设计 px = canvas 单位；`_cv` 四边贴合根画布 ⇒ 它的 `rect` 就是画布矩形）。
        /// <para>`Expand` 档下**画布宽恒 1920 单位**、**高随屏变**（4:3 ⇒ 1440、16:9 ⇒ 1080、
        /// 21:9 ⇒ 宽 2560）—— 这正是「x 轴的拉伸与固定 px 在 4:3/16:9 下数值相同、要到比 16:9
        /// 更宽的屏才分家，而 y 轴在 4:3 就立刻有差」那条的来源。</para>
        /// <para>⚠️ **退化（`rect` 量不到，批处理里画布没排过版时是 0×0）⇒ 退参考分辨率
        /// `HudCanvas.ReferenceResolution`（1920×1080）** —— 不退的话 `size = span×0 + sd`
        /// 会算出**负的/零的面板**、整块面板**当场塌掉**（静默、且只在批处理里现形）。
        /// 退档是**出声一次**（⛔ 不静默）。</para></summary>
        Vector2 CanvasPx
        {
            get
            {
                var r = _cv != null ? _cv.rect : new Rect(0f, 0f, 0f, 0f);
                if (r.width >= 1f && r.height >= 1f) return new Vector2(r.width, r.height);
                if (!_warnedDegenerateCanvas)
                {
                    _warnedDegenerateCanvas = true;
                    Debug.LogWarning("[Battle] 技能面板：子画布的 `rect` 还量不到（批处理下画布没排过版）"
                                   + " ⇒ 面板按**参考分辨率** " + HudCanvas.ReferenceResolution.x + "×"
                                   + HudCanvas.ReferenceResolution.y + " 排版（= 16:9、与改之前逐位相同）。"
                                   + "真机有渲染时不会走这一支。");
                }
                return HudCanvas.ReferenceResolution;
            }
        }
        bool _warnedDegenerateCanvas;

        /// <summary>🔴🔴 **2026-10-16（`T09` 批 3 · 主对话「按原版锚点补正」）：画布档的【面板矩形】**
        /// （`_cv` 局部 px、**中心原点、y 向上**）—— 由 `CvAnchorMinX` 那一组锚点字段**现算**。
        /// <para>**判据只此一处**：`CvPos` / `Wpx` / `Hpx` / `Contains` / `HitBoxWorldSize` 全读它
        /// （⛔ 别在别处再算一次 —— 那就是「同一个规矩的第二份写法」）。</para>
        /// <para>**算式**（uGUI 的拉伸锚）：
        /// `size = (aMax − aMin) × 父尺寸 + sizeDelta`；
        /// 锚参考点 = `父 rect.min + (aMin + (aMax−aMin) × pivot) × 父尺寸`，`anchoredPosition` 是
        /// **枢轴**相对它的偏移，而面板枢轴 = (0.5,0.5) = 中心 ⇒ 参考点 + `ap` **就是面板中心**。</para>
        /// <para>⚠️ 少了 `ActiveSkillDesc` 那一级（见文件头 ①）⇒ 本属性**就是**那一级的矩形（等价几何）。</para>
        /// <para>✅ **自证（1920×1080）**：算出 `size = 576.4067 × 324.8240`、中心 `(0.0007, −320.8321)`
        /// ⇒ 归一化矩形 x[0.3498945, 0.6501059] y[0.0525517, 0.3533147] —— 与 `PanelX0/X1/Y0/Y1`
        /// **逐位吻合**（≤ 5e−5）、与 `PanelPxW/H` 差 **≤ 0.004 px**（那两个常量是 2 位小数的舍入）。
        /// 算式全文 → 本轮报告 §④。</para></summary>
        Rect PanelCvRect
        {
            get
            {
                var c = CanvasPx;
                float spanX = CvAnchorMaxX - CvAnchorMinX, spanY = CvAnchorMaxY - CvAnchorMinY;
                var size = new Vector2(spanX * c.x + CvSizeDeltaX, spanY * c.y + CvSizeDeltaY);
                // `_cv` 四边贴合（锚 0–1、`sd` 0、pivot 居中）⇒ 它的 `rect.min = −父尺寸/2`，
                // 归一化锚点 0…1 直接映射到 `rect.min … rect.min + 尺寸`。
                var center = new Vector2(-c.x * 0.5f + (CvAnchorMinX + spanX * CvPivotX) * c.x + CvAnchoredX,
                                         -c.y * 0.5f + (CvAnchorMinY + spanY * CvPivotY) * c.y + CvAnchoredY);
                return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
            }
        }

        /// <summary>面板内归一化（x 从左 0→右 1，y 从**面板顶** 0→底 1）→ **子画布局部**
        /// （中心原点、**y 向上**、设计 px）。给字与 `localPosition` 用。
        /// <para>⚠️ 与 <see cref="InPanel"/>（世界档那一份）**不是同一个坐标系** —— 两档在 16:9 下逐位相同，
        /// 别的宽高比下：**画布档跟着画布（= 原版口径）走**，世界档跟着屏幕比例走。</para></summary>
        Vector3 CvPos(float x01, float y01)
        {
            var r = PanelCvRect;                      // 🔴 现算（判据见它自己的 doc）
            return new Vector3(r.xMin + x01 * r.width,
                               r.yMax - y01 * r.height,   // `y01` 从**面板顶**算 ⇒ 从 `yMax` 往下减
                               0f);                       // 🔴 z **恒 0**：画布上写 z 会把件推离平面
        }

        /// <summary>画布档：直接定一个件的**设计 px** 尺寸（`sizeDelta`）。
        /// ⚠️ 世界档要走 `SetWorldHeight` + `localScale.x` 那一套（见 <see cref="PlaceQuad"/>）—— 两回事。</summary>
        static void SizePx(HudPiece q, float wPx, float hPx)
        {
            if (q == null || !q.IsUgui || q.Rect == null) return;
            q.Rect.sizeDelta = new Vector2(wPx, hPx);
        }

        /// <summary>画布档：按**高**定尺寸、**保持当前宽高比**（= 世界档 `SetWorldHeight` 的对应物）。</summary>
        static void SetHeightCv(HudPiece q, float hPx)
        {
            if (q == null || !q.IsUgui || q.Rect == null || !(hPx > 0f)) return;
            var d = q.Rect.sizeDelta;
            float a = d.y > 0f ? d.x / d.y : 1f;
            q.Rect.sizeDelta = new Vector2(hPx * a, hPx);
        }

        /// <summary>把一个件摆到面板内那个**归一化点**上（`x01` 从左、`y01` 从**面板顶**算）。
        /// 两档各走各的坐标系（世界档 `InPanel` / 画布档 `CvPos`）——
        /// ⛔ **别在调用点自己写 `localPosition`**（画布上写 z 会把件推离平面）。</summary>
        void Place(Transform t, float x01, float y01, float z)
        {
            if (t == null) return;
            t.localPosition = UguiMode ? CvPos(x01, y01) : InPanel(x01, y01, z);
        }
        void Place(HudPiece q, float x01, float y01, float z) { if (q != null) Place(q.transform, x01, y01, z); }
        void Place(Label l, float x01, float y01, float z) { if (l != null) Place(l.transform, x01, y01, z); }
        void Place(HudPiece q, Rect r, float z) { Place(q, r.x + r.width * 0.5f, r.y + r.height * 0.5f, z); }
        void Place(Label l, Rect r, float z) { Place(l, r.x + r.width * 0.5f, r.y + r.height * 0.5f, z); }

        /// <summary>费用图标那一格（原版是 `manaIcon`，`m_PreserveAspect = 1`）：
        /// **方图居中放进它的矩形** —— 边长 = `min(宽,高)`，宽再按图自己的比例给（同世界档）。
        /// 判据（那个 `side`）**只此一处**，两档各走各的尺寸口。</summary>
        void PlaceManaIcon()
        {
            if (_manaIcon == null) return;
            if (UguiMode)
            {
                float side = Mathf.Min(Wpx(CostIconRect.width), Hpx(CostIconRect.height));
                SetHeightCv(_manaIcon, side);
            }
            else
            {
                float side = Mathf.Min(W01(CostIconRect.width), H01(CostIconRect.height));
                _manaIcon.SetWorldHeight(side);
            }
            Place(_manaIcon, CostIconRect, ZText);
        }

        /// <summary>把一块纯色 quad 摆进面板内那个矩形。
        /// 世界档：`ImageQuad` 是按「高 + 图的比例」定尺寸的（1×1 的图出来是正方形），
        /// 所以横向再拉一下才成矩形（原版那几块也都是非等比的 Image）。
        /// 🔴 **画布档**：尺寸 = 设计 px 直接进 `sizeDelta`（`Image.preserveAspect = 0` ⇒ 拉满矩形，
        /// 见 `MakeImageInParent`）⇒ **不需要** `SetAspect` 那一步。</summary>
        void PlaceQuad(HudPiece q, Rect r, float z)
        {
            if (q == null) return;
            if (UguiMode)
            {
                SizePx(q, Wpx(r.width), Hpx(r.height));
                Place(q, r, z);
                return;
            }
            q.SetWorldHeight(H01(r.height));
            q.transform.localPosition = CenterOf(r, z);
            q.transform.localScale = new Vector3(W01(r.width) / Mathf.Max(1e-4f, q.WorldW), 1f, 1f);
        }

        /// <summary>
        /// 🆕 **2026-10-10（A1212 块 7）**：给这四颗字接上**原版那一套自适应**（`4/60/36`）。
        /// 框宽/框高走**本档的尺寸单位**（`Label.SetAutoFitBox` 的 doc：它写的是 `sizeDelta`，
        /// 单位随档变）—— 调用点一律走 `BoxW/BoxH`（世界档 = 世界单位 · 画布档 = 设计 px），
        /// ⛔ **别在这里自己乘系数**；`min/max/base` 恒走**原版画布 px**、三个数原样传
        /// （出处见 `AutoMinPx` 那一组常量的注释）。
        /// <para>⚠️ 它会**重排 mesh** ⇒ 与本文件末尾那次裁切（如果将来加）以及 `Fit` 的次序：
        /// `SetAutoFitBox` 在前、`Fit` 在后（`Fit` 读收敛后的尺寸，见 `RefreshLayout` 里那段注）。</para>
        /// </summary>
        static void FitBox(Label l, float boxW, float boxH)
        {
            if (l == null) return;
            if (!(boxW > 0f) || !(boxH > 0f)) return;      // 退化框：不接（`SetAutoFitBox` 对 `worldH<=0` 也不写框高）
            l.SetAutoFitBox(boxW, boxH, AutoMinPx, AutoMaxPx, AutoBasePx);
        }

        /// <summary>
        /// 装不进矩形就整体回缩 —— 逼原版那个 `enableAutoSizing`。
        /// 字号正比于汉字高度，宽度也正比，所以「缩多少比例」能直接作用回去。
        /// </summary>
        /// <summary>
        /// 装不进矩形就整体回缩 —— 逼原版那个 `enableAutoSizing`。
        /// 字号正比于汉字高度，宽度也正比，所以「缩多少比例」能直接作用回去。
        ///
        /// ⚠️ **空文本必须跳过**：TMP 在字符串为空时 `textBounds` 会返回 **`4.2949673E+09`**
        ///    （`uint.MaxValue`，它内部当「无穷大」用的哨兵）。拿它当真实宽度算比例，
        ///    字号会被缩掉 1e-10 倍 —— 表现就是「面板上一个字都看不见」。
        /// ⚠️ 再加一道保险：缩得比 5% 还狠说明量出来的数不对，**宁可不缩**也别缩成零。
        /// </summary>
        static void Fit(Label l, float maxW, float maxH)
        {
            if (l == null || string.IsNullOrEmpty(l.Text)) return;
            float w = l.WorldW, h = l.WorldH;
            if (!(w > 1e-4f) || !(h > 1e-4f) || !(maxW > 1e-4f) || !(maxH > 1e-4f)) return;
            float k = Mathf.Min(1f, Mathf.Min(maxW / w, maxH / h));
            if (k >= 1f || k < 0.05f) return;
            l.SetGlyphHeight(l.GlyphHeightWorld * k);
        }

        /// <summary>面板在**屏幕上**占的那块矩形（指针在不在面板上用它判）。
        /// 🔴 **2026-10-16（`T09` 批 3）：按档取坐标系** ——
        /// 世界档 = 壳 `transform` 的局部（世界单位、原点在面板中心）；
        /// **画布档 = 子画布 `_cv` 的局部**（设计 px、原点在**画布中心**）⇒ 与「件真正画在哪」同一个变换
        /// （跟着 `CanvasScaler` 走）⇒ **任何宽高比下都咬合**。
        /// ⛔ **画布档别再用 `transform`** —— 那是**世界空间那层壳**（`BattleDriver` 还在用它交指针点），
        /// 非 16:9 下两个帧差 1.333 倍 ⇒ 会变成「看得见的地方点不到」（`T09` §五·补⑦ 那条）。
        /// 🔴 **2026-10-16（锚点补正）**：画布档的框**不再是那对固定 px 常量**，而是
        /// <see cref="PanelCvRect"/>（跟着画布高走）—— 否则 4:3 下「画出来的」会跑到「命中框」外面去。</summary>
        public bool Contains(Vector3 world)
        {
            if (UguiMode)
            {
                var p = _cv.InverseTransformPoint(world);
                var r = PanelCvRect;
                return Mathf.Abs(p.x - r.center.x) <= r.width * 0.5f
                    && Mathf.Abs(p.y - r.center.y) <= r.height * 0.5f;
            }
            var l = transform.InverseTransformPoint(world);
            return Mathf.Abs(l.x) <= PanelW * 0.5f && Mathf.Abs(l.y) <= PanelH * 0.5f;
        }

        // ---- 🆕 2026-10-18（A964① ⑥）：面板的**实绘几何**只读口（⛔ 只给自检，不给生产用）----
        //  补它的理由：上面 `Contains` 用的是 `PanelW/PanelH`（**手算**：屏幕比例 × 30%），
        //  而**画出来的**是 `_bg`（只占面板 84.4%×68.1%）与 `_light`（93.2%×84.7%）两颗 quad
        //  ⇒ 没有实绘口就**无从比**「命中区 ⊇ 实绘」。
        //  🔴 **读法必须是 `WorldW/H × lossyScale`**：`PlaceQuad` 是「先按高定尺寸、再用
        //     `transform.localScale.x` 横向拉到目标宽」⇒ **`WorldW` 是拉伸【前】的值**
        //     （`_light` 的 `WorldW` 只有 0.8468×面板高，而它真正画出来是 0.9322×面板宽）。
        //     只读 `WorldW` 会把实绘宽量小 10% —— 那是「量错」不是「原版如此」。
        //  ⛔ 本件**不改** `Contains` 的口径（改命中＝改行为，要另开一件、另配断言）；
        //     它仍然只用于 `SetPointer` 的按下态着色。
        //  🔴 **2026-10-16（`T09` 批 3）：画布档那一支读 `sizeDelta` × `lossyScale`** ——
        //     画布上的件**没有** `localScale` 那次横向拉伸（尺寸本身就是 `sizeDelta`，
        //     见 `PlaceQuad` 的画布分支），而 `sizeDelta` 的单位是**设计 px**；
        //     乘 `lossyScale`（= 画布那一跳：设计 px → 世界单位）就回到**世界单位** ——
        //     **两个口因此量的是同一件事**（`E2⑥` 那条「命中区 ⊇ 实绘」照旧成立）。
        //     ⚠️ 本件这两口**契约恒为「世界单位」**（两档都是）—— 因为它们是**跨件**比较用的
        //     （比 `Contains` 那个世界框）。⛔ 别改成 px（那会让 `E2⑥` 拿两种尺比）。

        static Vector2 DrawnWorldSize(HudPiece q)
        {
            if (q == null) return Vector2.zero;
            var s = q.transform.lossyScale;
            if (q.IsUgui)
            {
                var d = q.Rect != null ? q.Rect.sizeDelta : Vector2.zero;
                return new Vector2(d.x * s.x, d.y * s.y);
            }
            return new Vector2(q.WorldW * s.x, q.WorldH * s.y);
        }

        /// <summary>底板 `AbilityContainer` 的**实绘**世界宽 × 高。</summary>
        public Vector2 BgDrawnSize { get { return DrawnWorldSize(_bg); } }

        /// <summary>底板 `AbilityContainer` 的世界中心（**不是**面板中心 —— 它偏左上，见 `BgRect`）。</summary>
        public Vector3 BgWorldPos
        {
            get { return _bg != null ? _bg.transform.position : Vector3.zero; }
        }

        /// <summary>`Lights` 色片的**实绘**世界宽 × 高 —— 它是**画得最大**的那一层
        /// （93.2% × 84.7% 的面板，见 `LightRect`），所以 `E2` 拿它当「实绘矩形」的尺子。
        /// ⚠️ **两档现在同值**（2026-10-16 主对话裁定删掉了那颗「世界档冻结值」）。</summary>
        public Vector2 LightDrawnSize { get { return DrawnWorldSize(_light); } }

        /// <summary>`Lights` 色片的世界中心。</summary>
        public Vector3 LightWorldPos
        {
            get { return _light != null ? _light.transform.position : Vector3.zero; }
        }

        /// <summary>命中区（<see cref="Contains"/>）自己的世界宽 × 高 —— 就是那对手算的
        /// `PanelW/PanelH`（**世界单位**；⛔ 不是那两颗实绘口量的东西 —— 它是 `Contains` 用的那个框）。
        /// 探针要拿它算「多远算面板之外」（`Contains` 是绕面板中心的半边长）。
        /// 🔴 **2026-10-16（`T09` 批 3）**：画布档下 `Contains` 量的框 = **面板的设计 px 矩形**
        /// （跟着 `CanvasScaler` 缩放）⇒ 这里按**子画布那一跳**折算成世界单位
        /// （乘 `_cv.lossyScale`）—— 这样它和 <see cref="LightDrawnSize"/> / <see cref="BgDrawnSize"/>
        /// **同一把尺**（两个口都恒为世界单位），`E2⑥` 那条「命中区 ⊇ 实绘」照旧可比。
        /// 🔴 **2026-10-16（锚点补正）**：那一对 px 现在是 <see cref="PanelCvRect"/> 的 `size`
        /// （**跟着画布高走**，不再写死 `PanelPxW/H`）—— 与 `Contains` 读的**同一个矩形**。</summary>
        public Vector2 HitBoxWorldSize
        {
            get
            {
                if (UguiMode)
                {
                    var s = _cv != null ? _cv.lossyScale : Vector3.one;
                    var r = PanelCvRect;
                    return new Vector2(r.width * s.x, r.height * s.y);
                }
                return new Vector2(PanelW, PanelH);
            }
        }

        // ==================================================================
        //  显示
        // ==================================================================

        /// <summary>
        /// 弹出来。`cardName` 当技能名（原版有独立的技能名 `"Fire Arrow"`，我们还没有那个字段）、
        /// `ability` 是效果、`targets` 是**现在有几个合法目标** —— 由 `BattleDriver` 数好传进来，
        /// 面板不自己判规则。
        /// </summary>
        public void Show(string cardName, EffectSpec ability, int targets)
        {
            Build();
            ShownName = cardName;
            ShownTargets = targets;
            _acting = false;

            if (_name != null) _name.SetText(cardName);
            if (_desc != null) _desc.SetText(CardText.EffectSentence(ability));
            if (_targets != null) _targets.SetText(CardText.TargetsAvailable(targets));
            // 我们的技能**不吃能量、吃一次行动** —— 原版这一格是能量费。照实填 0，
            // 不把行动费伪装成能量费（以后技能真收能量了，这格已经接好）
            if (_cost != null) _cost.SetText("0");

            Visible = true;
            RefreshLayout();
            FadeTo(1f);
        }

        /// <summary>合法目标数变了就更新那一行和色片（判据在 `BattleDriver`，这儿只显示）</summary>
        public void SetTargetsAvailable(int n)
        {
            ShownTargets = n;
            if (_targets != null) _targets.SetText(CardText.TargetsAvailable(n));
            ApplyColors();
        }

        /// <summary>🆕 **2026-10-18（第十三轮 · G2b）**：那一行**现在印出来的字**（自检用）。
        /// 权威 = 原版词条 `Battle/HUD/TargetsAvailable`（`CardText.TargetsAvailable` → `Loc.T` +
        /// `{0}` 替换；拼法与出处见 `Core/Loc.cs` 那一块）⇒ 切语档后重设一次再读它，两档分得出。</summary>
        public string TargetsText { get { return _targets != null ? _targets.Text : null; } }

        /// <summary>指针喂进来（`down` = 这一帧按着）—— 决定要不要铺蓝色那层</summary>
        public void SetPointer(Vector3 world, bool down)
        {
            bool p = Visible && down && Contains(world);
            if (p == _pressed) return;
            _pressed = p;
            ApplyColors();
        }

        /// <summary>技能开始结算 → 铺白那层（原版 `ShowActingLight()`）</summary>
        public void SetActing()
        {
            _acting = true;
            ApplyColors();
        }

        /// <summary>
        /// 三态归一到「现在铺哪一层」。**判据只有这一份** —— 别在调用处各自决定。
        /// 优先级照原版：结算 &gt; 按下 &gt; 可用。
        /// </summary>
        void ApplyColors()
        {
            CurrentLight = _acting ? Light.Acting
                         : _pressed ? Light.Pressed
                         : (ShownTargets > 0 ? Light.Available : Light.None);

            if (_light != null)
            {
                Color c;
                switch (CurrentLight)
                {
                    case Light.Acting: c = LightActing; break;
                    case Light.Pressed: c = LightPressed; break;
                    case Light.Available: c = LightAvailable; break;
                    default: c = new Color(0, 0, 0, 0); break;
                }
                _light.SetTint(WithAlpha(c, Alpha));
            }

            if (_bg != null) _bg.SetTint(WithAlpha(BgColor, Alpha));
            if (_name != null) _name.SetColor(WithAlpha(NameColor, Alpha));
            if (_cost != null) _cost.SetColor(WithAlpha(CostColor, Alpha));
            if (_desc != null) _desc.SetColor(WithAlpha(DescColor, Alpha));
            if (_targets != null) _targets.SetColor(WithAlpha(TargetsColor, Alpha));
            if (_manaIcon != null) _manaIcon.SetTint(WithAlpha(Color.white, Alpha));
        }

        /// <summary>收起（带淡出）</summary>
        public void Hide()
        {
            if (!Visible && Alpha <= 0f) { HideImmediate(); return; }
            Visible = false;
            ShownName = null;
            FadeTo(0f);
        }

        /// <summary>
        /// 收起（不带淡出）。
        /// ⚠️ 显隐**只靠 alpha，不碰 `SetActive`** —— 踩过：一开始用 `SetActive(false)` 收起来，
        ///    结果 `Show()` 里在**还没激活**的对象上调 `SetText`，而 TMP 在未激活的 GameObject 上
        ///    `ForceMeshUpdate()` **生成不出网格**，`textBounds` 全是 0，
        ///    面板上的字全成零尺寸、一个字都看不见。
        ///    面板就这么点东西，一直开着只花几个全透明 quad，不值得为它冒这个险。
        /// </summary>
        void HideImmediate()
        {
            Visible = false;
            ShownName = null;
            ShownTargets = 0;
            _pressed = _acting = false;
            CurrentLight = Light.None;
            if (_fade != null) { _fade.Kill(); _fade = null; }
            Alpha = 0f;
            ApplyColors();
        }

        /// <summary>淡到某个 alpha。**不切 `SetActive`** —— 理由见 `HideImmediate`</summary>
        void FadeTo(float target)
        {
            if (!_built) return;
            if (_fade != null) _fade.Kill();
            _fade = DOTween.To(() => Alpha, SetAlpha, target, FadeTime)
                           .SetUpdate(CardTween.Mode)
                           // 🔴 **绑生命周期**（2026-09-16 加）：面板被销毁时还在飞的淡入淡出
                           //    没人杀的话，DOTween 安全模式会每帧记一条
                           //    `Target or field is missing/null`（真包实测 22 条，编辑器看不见）。
                           //    全工程同一条规矩 —— 见 `CardTween.Use` 的注释。
                           .SetLink(gameObject);
        }

        void SetAlpha(float a)
        {
            Alpha = a;
            ApplyColors();
        }

        /// <summary>🔴 **自检用（`T09` 批 3 · 灭自证）**：本面板的图与字**是不是都建在 uGUI 子画布上**
        /// （而不是有条漏网、退回世界空间 quad）。
        /// <para>判据 = 那几件**每一件**都是 uGUI 件、且**都挂在 `_cv`（子画布）的父链下**。
        /// 语义上它防的是本件**最坏的那一档**：一半在画布、一半在世界 —— 16:9 下逐位相同、全绿，
        /// 只有别的宽高比才露（`T09` §五·补⑦）。⛔ 别用「`HudPiece != null`」替它（那个两边都真）；
        /// ⛔ 也别用「`UguiMode`」替它（那个只看宿主登记了没，不看件建在哪）。</para>
        /// <para>⚠️ 任一**该有的**件没建出来（贴图/字体缺失）⇒ 也返 `false`，如实回答「不成立」
        /// —— `_manaIcon` 那格原版 `m_IsActive = 0`、我们本来就允许它缺，所以它**不在**这条断言里。</para></summary>
        public bool OnCanvas
        {
            get
            {
                if (_cv == null || _cv.GetComponentInParent<Canvas>() == null) return false;
                return IsUgui(_bg) && IsUgui(_light) && IsUgui(_name) && IsUgui(_cost)
                    && IsUgui(_desc) && IsUgui(_targets);
            }
        }
        static bool IsUgui(HudPiece q)
        {
            return q != null && q.IsUgui && q.Rect != null
                && q.Rect.GetComponentInParent<Canvas>() != null;
        }
        static bool IsUgui(Label l)
        {
            return l != null && l.IsUgui && l.transform is RectTransform rt
                && rt.GetComponentInParent<Canvas>() != null;
        }

        /// <summary>
        /// 自检用：面板各元素现在摆在哪、多大、有没有激活。
        /// **截图看不出「字是不是根本没生成」**，所以把数报出来。
        /// 🔴 **2026-10-16（`T09` 批 3）**：头一行加了**档位**（`世界档` / `画布档`）——
        /// 两档的件尺寸**单位不同**（世界单位 vs 设计 px），不带这一格的读数没法横向比。
        /// ⚠️ 「面板尺寸」那一格两档报的都是**本档的尺**（画布档 = 设计 px）。
        /// 🔴 **2026-10-16（锚点补正）**：画布档那一格报的是**现算的面板矩形**
        /// （<see cref="PanelCvRect"/> 的 `size`，跟着画布高走）—— ⛔ 不是那对写死的 `PanelPxW/H`
        /// （那对现在只是 1920×1080 下的期望值）。
        /// </summary>
        public string Describe()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("面板 ").Append(UguiMode ? "画布档" : "世界档")
              .Append(" 世界中心 ").Append(transform.position.ToString("F2"))
              .Append("　尺寸 ");
            if (UguiMode)
            {
                var r = PanelCvRect;
                sb.Append(r.width.ToString("F2")).Append("×").Append(r.height.ToString("F2"))
                  .Append(" px（画布 ").Append(CanvasPx.x.ToString("F0")).Append("×")
                  .Append(CanvasPx.y.ToString("F0")).Append(" · 锚点现算）");
            }
            else sb.Append(PanelW.ToString("F2")).Append("×").Append(PanelH.ToString("F2")).Append(" 世界单位");
            sb.Append("　alpha ").Append(Alpha.ToString("F2"))
              .Append("　灯 ").Append(CurrentLight);
            sb.Append("　name[").Append(One(_name)).Append("]");
            sb.Append("　cost[").Append(One(_cost)).Append("]");
            sb.Append("　desc[").Append(One(_desc)).Append("]");
            sb.Append("　targets[").Append(One(_targets)).Append("]");
            return sb.ToString();
        }

        static string One(Label l)
        {
            if (l == null) return "无";
            return "「" + l.Text + "」pos=" + l.transform.position.ToString("F2")
                 + " 尺寸=" + l.WorldW.ToString("F2") + "×" + l.WorldH.ToString("F2")
                 + " [" + l.DumpSizes() + "]"
                 + " active=" + l.gameObject.activeInHierarchy + "/" + l.gameObject.activeSelf;
        }
    }
}
