// SettingsWindow.cs — 主菜单设置窗（原版 `Main Menu Settings Window`，类 `SettingsMenu : GameWindowWithTabs`）
//
// ============================ 为什么这个文件现在才出现 ============================
// 它一直属于「阶段二**第 4 层**及以后，还没开」。后果是：**主菜单右上角那颗齿轮点了没反应**
//   （`MainMenuRuntime.cs:359` 只建了图、没接点击）= **静默失败**（本工程红线）。
// 2026-09-26 联机线开工，用户拍板「**联机页挂在设置窗里**，设置窗照原版建，先做 图像 / 音频 / 联机 三页」
//   ⇒ 连同窗壳一起建。判据全文 → `资料/联机P2P_设计与交接.md` §3·5。
//
// ============================ 照原版的部分（逐条有出处） ============================
// 出处 = `工具/menu_rect.py bundle_menus_assets_all "Main Menu Settings Window"` + 原始 JSON 实读。
//   · 根 `RectTransform_-7066813013973172314.json`：**`m_LocalScale = (0.9,0.9,0.9)`** ——
//     🔴 **只这一扇窗是 0.9**（`Rewards Base Submenu Variant` / `Shop Menu Variant` 都是 1）
//     ⇒ 子节点按**未缩放的矩形**摆，世界里的位置 = 屏幕中心 + 0.9 ×（矩形中心 − 屏幕中心）。
//     断言要用 `SettingsWindow.Screen(...)` 换算（别直接拿原版矩形当世界坐标 —— 那会差 11%）。
//   · `Menu Dark Background` 4574.60×2572.36，色 **(0,0,0,0.7725)**（无 sprite）。
//   · `Menu Area` [328.10,123.11]–[1602.50,966.19]：底图 `40k_popup`（Sliced，九宫格 169,160,169,160）
//     ```Mask``` [338.50,132.55]–[1592.62,956.39]；`Background fill` [510.62,132.55]–[1592.72,956.39]
//     = `40k_popup_texture` **Tiled**（`m_Type=2`，`ppuMultiplier=2.0` ⇒ 128/2 = **64 px 一格**）。
//   · `Generic Close Button` [1559.00,91.61] 75×75（`UI_Button_Round_background`）
//     + `Icon` [1568.31,101.86]–[1624.68,156.35]（`40k_bt_close`）。
//   · `Tab Buttons` 列 [328.10,123.10]–[506.52,966.19]（178.42 宽）：
//     VLG **padTop 30** · spacing 0 · align **UpperCenter** · childControlWidth 1 · childControlHeight 0
//     ⇒ 每个键 **178.42 × 157.68**，从 y = 123.10+30 = **153.10** 起往下排（第 k 个的顶边 = 153.10 + k×157.68）。
//   · `Separators` [505.07,103.11]–[507.97,986.19]（`40k_Separator_Fade_Sides_Vertical`）。
//   · 内容区 `Tab Content` [506.52,123.11]–[1538.78,966.19]；页标题 `Tab Title` [596.52,189.24]–[1538.78,259.24]
//     （**fs 55 · 左对齐**，实测 `m_HorizontalAlignment=1`）。
//   · 音频页（原版 `Media Tab`）：`Audio Settings` [596.52,**280.147**]–[1280.71,**630.947**]（自读原版实测
//     **280.146985 / 630.946972**，组高 **350.800**），内含 **3 行**、从顶往下。
//     🔴 **2026-10-05 就地更正**：原来这里写「**3 行 × 105 高**」—— **错了**。三行 `… Container` 的
//     `sizeDelta.y` 是 **105 / 106 / 106**（不相等），行顶真值 = **280.147 / 396.414 / 513.680**：
//     组高 350.800 − 三格 317 = 33.800 的余量被 VLG（`ctrlH=0` + `expandH=1`）**每格均摊 11.2667**
//     ⇒ 行顶**步进** = 行高 + 11.2667（= 116.2667 / 117.2667），**不是** 105。
//     ⚠️ 组底 630.947 **比末行底（619.680）低 11.267** —— 那截是 force-expand 的余量，**不是第四行**。
//   · 图像页（原版 `Graphics Tab`）：`Content` [506.52,267.71]–[1538.89,966.19]（VLG）；
//     `Quality Selector` 行 [551.52,267.71]–[1386.37,327.10]（下拉框到 x=952.18、**文字在框右边** [978.86…]）；
//     勾选行在 `Scroll View` [551.52,432.50]–[1538.89,954.00] 里，`Content` x 563.52–1018.73、**每行 76 高、步长 80.6**。
//   · `Debug Buttons` 那一排**不建** —— 全量反编译 `SettingsMenu__Awake.c` 里对它 `SetActive(false)`（调试层）。
//
// ============================ 🔴 我们挑的 / 查不到的（不许冒充原版） ============================
//   ① **「联机」这一页原版没有**（搜过 Online/Network/Server/Connect/Region/Ping/Multiplayer/Matchmak，
//      设置窗里一个都没有）⇒ 这一页是**新增设计**（用户 2026-09-26 的规格）。
//   ② **页签文案**：解包里那几个 `Tab Toggle Title` 的 `m_text` 是**西班牙语**（`Gráficos`/`Soporte`/`Cuenta`，
//      第 2 页是 `Multimedia`）、TMP **没挂 I2 词条** ⇒ 英文正式文案本地拿不到。我们写
//      **Graphics / Audio / Online**（英文，与工程别处「先用英文」的口径一致），**这是我们的选择**。
//   ③ **只建 5 页里的 3 页**（General / Account / Support 不建 —— 账号页整页走服务器、支持页是外链、
//      General 页是语言/退出游戏）。切到没建的页要**出声**，不静默。
//   ④ **联机页的页面标题、说明行、状态行**都是我们写的文案。
//   ⑤ 原版勾选图与下拉框底的 **sprite PathID 没解出名字**（`-5728790147372056906` / `4411787853012002210`
//      / `4198243566598287219`，UnityPy 按 PathID 反查没查到）⇒ 勾选改用原版**别处**在用的
//      `40K_toggle_on/off`，下拉框底用 `40K_button`。**这是我们的选择**，记在正本 §九。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CardPresentation.Net;

namespace CardPresentation
{
    /// <summary>设置窗的三个页（原版有五个，我们只建这三个 —— 见文件头 ③）。</summary>
    public enum SettingsTab { Graphics = 0, Audio = 1, Online = 2 }

    public class SettingsWindow : GameWindow
    {
        // ============================================================ 常量（出处见文件头）

        /// <summary>根 `m_LocalScale`。**只这一扇窗是 0.9**，别抄到别的窗上。</summary>
        public const float RootScale = 0.9f;

        public const float PopL = 328.10f, PopT = 123.11f, PopR = 1602.50f, PopB = 966.19f;
        public const float MaskL = 338.50f, MaskT = 132.55f, MaskR = 1592.62f, MaskB = 956.39f;
        public const float FillL = 510.62f, FillT = 132.55f, FillR = 1592.72f, FillB = 956.39f;
        public const float ShadeW = 4574.60f, ShadeH = 2572.36f;
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.7725f);

        public const float CloseL = 1559.00f, CloseT = 91.61f, CloseR = 1634.00f, CloseB = 166.61f;
        public const float CloseIconL = 1568.31f, CloseIconT = 101.86f, CloseIconR = 1624.68f, CloseIconB = 156.35f;

        public const float BarL = 328.10f, BarT = 123.10f, BarR = 506.52f, BarB = 966.19f;
        public const float BarPadTop = 30f;             // VLG `m_Padding.m_Top`
        public const float TabBtnH = 157.68f;           // childControlHeight = 0 ⇒ 用各自的 sizeDelta
        public const float BarSepL = 505.07f, BarSepT = 103.11f, BarSepR = 507.97f, BarSepB = 986.19f;

        public const float TabsL = 506.52f, TabsT = 123.11f, TabsR = 1538.78f, TabsB = 966.19f;
        public const float TitleL = 596.52f, TitleT = 189.24f, TitleR = 1538.78f, TitleB = 259.24f;
        public const float PageTitleFontPx = 55f;       // 实测 `Tab Title` fs=55

        // 音频页
        public const float AuL = 596.52f, AuR = 1280.71f;
        /// <summary>`Audio Settings` 组的顶 / 底。原版 `RectTransform -1942814961158094938`：
        /// `anchoredPosition.y = 89.10002136230469`、`sizeDelta.y = 350.79998779296875`
        /// （锚在 `Media Tab`（绝对 506.517765,123.105008→1538.776429,966.188992）的 (0,0.5)→(0.75,0.5)）
        /// ⇒ 绝对 **280.146985 → 630.946972**。⚠️ 旧值写的 280.15/630.95 是**四舍五入**后的。
        /// 🔴 组底**不是**末行的底（末行底 = 619.680）—— 见数组注释里的 force-expand 余量。</summary>
        public const float AuT = 280.146985f, AuB = 630.946972f;
        /// <summary>🔴 **三行的行顶**（原版真值，**逐行给**）。
        ///
        /// <para>出处 = `python 工具/menu_dump.py bundle_menus_assets_all "Audio Settings" --depth 3 --md`
        /// （该工具的 VLG 仿真带 `--verify-layout`，逐 fixture 与手算值对过；本文件这一组数也照它的算式复算过）。
        /// 原版 `Audio Settings` 挂 `VerticalLayoutGroup`：`m_Spacing = 0`、`m_Padding = 0`、
        /// `m_ChildAlignment = 0 (UpperLeft)`、**`m_ChildControlHeight = 0`**、**`m_ChildForceExpandHeight = 1`**。
        /// 三格 `sizeDelta.y = 105 / 106 / 106`（合计 317）⇒ 组高 350.799988 的余量 33.799988 由
        /// `flexible = Max(自己的, 1)` 均摊 ⇒ `fmul = 33.799988 / 3 = 11.266662`。
        /// **行顶步进 = 行高 + fmul** ⇒ 280.146985 → **396.413648** → **513.680310**。</para>
        ///
        /// <para>🔴 **别再用「`AuT` + 序号 × 一个行高」推** —— 那正是旧值（280.15 / 385.15 / 490.15）的错源
        /// （三行行高**不相等**，而且每格还要再涨 fmul）。`AuRowTops[0]` 恒等于 `AuT`（原版第一行的顶就是组顶）。
        /// 位置差：第 2 行 **−11.26**、第 3 行 **−23.53**（我们比原版偏上）。</para></summary>
        public static readonly float[] AuRowTops = { 280.146985f, 396.413648f, 513.680310f };
        /// <summary>三行各自的行高 = 原版三个 `… Container` 的 `sizeDelta.y`（**105 / 106 / 106**）。
        /// ⚠️ 与「行顶步进」（= 行高 + 11.266662）**不是一回事**，别混（`ctrlH=0` ⇒ 容器自己只有这么高，
        /// 多出来的 11.2667 是**格**里的空隙，容器贴在格的顶上）。</summary>
        public static readonly float[] AuRowHs = { 105f, 106f, 106f };
        /// <summary>行内摆法（原版，三行同值）：`Label` 的**顶** = 行顶 **− 13.5**；`… Slider` 的**中心** = 行中心 **+ 11.1**。
        /// <para>算据：`Label` 锚 `(0,0.5)→(0.5,0.5)`、pivot `(0,0.5)`、`anchoredPosition = (0,+35)`、
        /// `sizeDelta.y = 62/63/63` ⇒ 顶 = 行中心 − 35 − 高/2 ⇒（行高 105 时）280.146985+52.5−35−31 = **266.647** = 行顶 − 13.5，
        /// 行高 106 时同理（+53−35−31.5 = +(−13.5)）。
        /// `… Slider` 锚 `(0,0.5)→(1,0.5)`、pivot `(0.5,0.5)`、`anchoredPosition = (0,−11.1)`、`sizeDelta.y = 13`
        /// ⇒ 中心 = 行中心 + 11.1。**逐行实测**：343.747 / 460.514 / 577.780（= 行顶 + 63.60 / +64.10 / +64.10）。</para>
        /// 🔴 简报里那句「滑块中心在行顶 **+63.8**」是**近似**（63.6 与 64.1 的折中）——
        /// 本文件按**逐行真值** `行中心 + 11.1` 落地，别按 63.8 抄成一个常数。</summary>
        public const float AuLabelTopOff = -13.5f, AuSliderCyOff = 11.1f;
        /// <summary>三行 `Label` 各自的高（原版 `sizeDelta.y`：Music **62**、Sound effects / Voiceovers **63**）。
        /// 🔴 旧代码这里写死 `t + 62f`（三行都 62）—— 第 2、3 行各差 1px。</summary>
        public static readonly float[] AuLabelHs = { 62f, 63f, 63f };
        /// <summary>滑块轨道宽 —— 取**本页容器宽**（原版 Media 页容器撑满 684.19）。
        /// ⚠️ `WfSlider` 自己的常量 561.08 来自**战斗内**那根，别混。</summary>
        public const float AuTrackW = 684.19f;
        /// <summary>🆕 **2026-10-05（A96）**：滑块轨道的**高** —— 原版那三根 `… Slider` 的
        /// `m_SizeDelta.y = **13**`（三根同值，实读 `RectTransform_-5607048967920844890` /
        /// `3300864807740932006` / `6026471101496917926`：锚 `(0,0.5)→(1,0.5)`、`m_LocalScale=(1,1,1)`）。
        /// 🔴 **这是【未缩放】的设计值，传进 `WfSlider` 之前必须过 `RootScale`** ——
        ///   本窗根 `m_LocalScale = 0.9` 是**烘进矩形**的（见 `Screen()` 的注释），而 `WfSlider`
        ///   是按**画布 px** 画的（和 `AuTrackW` 同一条规矩）⇒ 实传 `AuTrackH * RootScale` = **11.7**。
        ///   ⛔ **别传裸 13** —— 那比原版**大 11%**（本文件头第 13-14 行那条坑）。
        /// ⚠️ 判据复核：`python 工具/menu_dump.py bundle_menus_assets_all "Audio Settings" --depth 3 --no-sprite`
        ///    → 那三行的「宽×高」列 = **11.70**（= 13 × 0.9，屏幕上就是这个数）。</summary>
        public const float AuTrackH = 13f;

        // 图像页
        public const float GfxL = 506.52f, GfxT = 267.71f;
        public const float QualL = 551.52f, QualT = 267.71f, QualR = 1386.37f, QualB = 327.10f;
        public const float QualBoxR = 952.18f;          // 下拉框右边缘
        public const float QualTextL = 978.86f;         // 框右边那行字
        public const float ChkL = 563.52f, ChkT = 432.50f, ChkR = 1018.73f;
        /// <summary>勾选行的**行高 / 步进**（**未缩放**设计 px）。
        /// 🔴 **2026-10-07（A172）精确化**：原版那一行的 `m_SizeDelta.y = 75.64099884033203`、VLG `m_Spacing 5`
        /// ⇒ 行高 **75.641**、步进 **80.641**（旧值 76 / 80.6 是四舍五入，第 3 行就攒出 0.12 px）。
        /// 步进取「行高 + spacing」，⛔ 别各写一个数（两处写同一条规则 = 迟早不一致）。</summary>
        public const float ChkRowH = 75.641f, ChkRowStep = ChkRowH + 5f;
        /// <summary>勾选行在原版 `Scroll View > Viewport > Content` 里的**第几行**（0 起）——
        /// 🔴 **2026-10-07（A172）就地订正**：旧值写的是 `Vsync 第 5 行 · FPS Limit 第 6 行`，
        /// 那是 `menu_dump` 按「7 行全在」排出来的**编辑器快照**，**运行时不是这个值**。
        /// <para>运行时排法（判据全文 → `资料/普查产出_1007/审查_A170两条前提.md` §③）：
        /// `GraphicsTab.OnSetup` 尾部那段链式 `SetActive` **无条件跑**（VA 反汇编钉死）⇒ `Hi FPS` 与
        /// `Android extra compatibility` 运行时**都不在**；`Use super sampling` 只在 **Ultra** 档出现
        /// （`allowSuperSampling` 五档表：0/0/0/0/**1**）⇒ **只有两种排法**：
        /// VeryLow–High = `Small Screen(0) · Auto Zoom(1) · Vsync(2) · FPS Limit(3)`；
        /// Ultra = 中间插一行 `Use super sampling(2)` ⇒ `Vsync(3) · FPS Limit(4)`。
        /// ⚠️ 我们照 **VeryLow–High** 那一档落（`GameStaticData__.cctor` 写 `+0x120 = 3`(= High) ⇒ 那正是出货默认档；
        /// 我们的质量档位是 Unity 的 `Mobile/PC` 两档，与它那 5 档**没有对应关系** ⇒ 不硬映射）。</para>
        /// <para>🔴 第 0 行那一片 = 原版那颗 `EverguildToggle smallScreenToggle` +
        /// `GraphicsTab__SmallScreenToggleClick.c`（同时写 `GameStaticData` 的 `smallScreenUI` / `smallUIChosenManually`）
        /// ⇒ **就是 A165 补的那颗「Small Screen UI」开关**；第 1 行 = `autoZoom`（A172 补，见 `AutoZoom` 那个类）。</para></summary>
        public const int SmallScreenRow = 0, AutoZoomRow = 1, VsyncRow = 2, FpsRow = 3;

        // ---- 图像页那一列 = 原版**真的 `Scroll View`**（`ScrollRect` + `Viewport(RectMask2D)` + VLG `Content`）----
        // 🆕 **2026-10-06（A170）**：A168 当时把第 5、6 两行**整体上移 67.34**（`GfxRowsShift`）当临时落法
        //   —— 那正是 A168/A170 以为的「这一列的滚动范围」。本件把滚动视图补上、两行回原位、`GfxRowsShift` 删掉。
        // 🔴 **2026-10-07（A172）再一次就近订正**：那个「67.34 滚动范围」**在原版并不存在** ——
        //   两种运行时排法（346.923 / 508.205）**都塞得进视口 521.5072**；`Content.m_SizeDelta.y = 300`
        //   且 CSF `m_VerticalFit = 0`（不撑高）⇒ `ScrollRect.GetBounds()` 只取 **Content 自己的矩形**
        //   ⇒ `AdjustBounds` 把内容 bounds 撑到视口大小 ⇒ `CalculateOffset` **恒 0** ⇒ 没有可停留的滚动范围。
        //   ⚠️ 精确说法（审查 §①）：Elastic 下**能抖、有橡皮筋，但停不住** —— 别写成「一动不动」。
        //
        // 🔴 结构判据 = **原版 prefab 字段逐条实读**（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`）：
        //   · `Scroll View` RT `-7750568603365769306`（父 = `Graphics Tab`，GO = `Scroll View`）：
        //     `m_AnchorMin = m_AnchorMax = (0,1)` · pivot `(0.5,0.5)` · `m_SizeDelta = (987.37, 521.5072)`；
        //     它挂的 `ScrollRect` `-4278272035212787802`：`m_Horizontal 1` · `m_Vertical 1` ·
        //     **`m_MovementType 1`(Elastic)** · `m_Elasticity 0.1` · `m_Inertia 1` · `m_DecelerationRate 0.135` ·
        //     `m_ScrollSensitivity 1`；`m_Content` = 下面那个 `Content`、`m_Viewport` = 下面那个 `Viewport`。
        //     ⚠️ 它序列化的 `m_AnchoredPosition` 是**模板位**（父级 VLG 会覆盖它）⇒ **落位后的绝对矩形**
        //     取 `menu_dump --relative` 排完的那份：`[551.52,432.50]–[1538.89,954.00]`（高 521.50）。
        //   · `Viewport` RT `6815893749579022246`：`m_AnchorMin (0,0)` / `m_AnchorMax (1,1)` ·
        //     `m_SizeDelta = (−24, 0)` · `m_AnchoredPosition (0,0)` ⇒ **左右各内缩 12、上下与 `Scroll View` 齐**
        //     （宽 **963.37**）；挂 `RectMask2D` `-6600671332037066842`：`m_Padding (0,0,0,0)` · `m_Softness (0,0)`（**硬边**）。
        //   · `Content` RT `-5664032482510274650`：`m_AnchorMin (0,1)` / `m_AnchorMax (1,1)` · pivot `(0,1)`；
        //     ⚠️ **2026-10-07 订正两处**（审查 §①）：宽 = **455.21**（不是 455.17；= 963.37 − 序列化的 508.16），
        //     而且序列化的 `m_SizeDelta.x` **只是模板值** —— 宽度其实是 **CSF `m_HorizontalFit = 2`(PreferredSize)** 撑的
        //     （我们那个 455.21 恰好等于它）。纵向那半不受影响：**CSF `m_VerticalFit = 0`**。
        //     挂 VLG `-5468938573918535770`：`m_Spacing 5` · `m_Padding 0` · `m_ChildAlignment 0`(UpperLeft) ·
        //     `m_ChildControlHeight 0` · `m_ChildForceExpandHeight 0`。
        public const float GfxScrollL = 551.52f, GfxScrollT = 432.50f, GfxScrollR = 1538.89f, GfxScrollB = 954.00f;
        /// <summary>`Viewport.m_SizeDelta.x = −24` ⇒ 左右各内缩 **12**（`RectMask2D` 的边界就在这里）。</summary>
        public const float GfxViewInset = 12f;
        public const float GfxViewL = GfxScrollL + GfxViewInset, GfxViewR = GfxScrollR - GfxViewInset;
        /// <summary>内容高 = **我们实建那 4 行**（VeryLow–High 那一档）合计 3 × 75.641 + 105 + 3 × 5 = **346.923**
        /// （**未缩放**设计 px）—— `MenuScroll` 的那一端。
        /// 🔴 **它比视口高 521.5072 矮**（欠 −174.58）⇒ `MenuScroll` 的 `ClampHi` 落到 0 ⇒ 这一列**停不住任何位移**
        /// （= 原版 `CalculateOffset` 恒 0 的等效物；`MaxOffset` 本身是**负值** −157.12 画布 px，⛔ 别拿它当「能滚多远」，
        /// 要判「能不能滚」看 `ClampHi`/`ClampLo`）；Elastic 下仍能抖/回弹（同原版）。
        /// ⛔ **A170 那个 `GfxScrollRange = 67.34` 已删**：它算的是「7 行带空格位」那个**运行时不存在的**排法。
        /// ⚠️ 传进 `MenuScroll` 前要 **× `RootScale`**（它跟 `Viewport` 一样是**画布 px** 的量，
        /// 同 `AuTrackH` 那条「别传裸 13」的口径）。⛔ 将来补 `Use super sampling` 那一行时换成 **508.205**。</summary>
        public const float GfxContentH = 346.923f;

        // ---- 图像页那一列里那几行：`Small Screen UI`(0) · `Auto Zoom`(1) · `Vsync`(2) · `FPS Limit`(3) ----
        // 全部字面量的出处 = `bundle_menus_assets_all` 实读（⛔ 都是**未缩放**的设计值，进 `Screen()` 之前的样子）：
        //   · 行 `FPS Limit`：`sizeDelta=(455.21,105)`，列 `Content` 内第 3 行（VeryLow–High 那一档）；
        //   · 行内 `Title` 框 [16,−14]–[325.6,48]（`m_AnchoredPosition`+`sizeDelta` 解出来）
        //   · 行内 `FPS Slider` 框 [266,84.2]–[757.2,97.2]（= 491.18 × 13）
        //   · 滑块本体 `MonoBehaviour_5646828332852936614.json`：`m_MinValue 0` · `m_MaxValue 2` ·
        //     `m_WholeNumbers 1`（⇒ **只有 0/1/2 三个整数值**）· `m_Direction 0`(LeftToRight)
        //   · 子件 `Background/Sliced Volume_bar_inactive 400×31 border 184,0,184,0 ppuMul 2` ·
        //     `Fill/Sliced Volume_bar_active 64×31 border 30,0,30,0 ppuMul 2` ·
        //     `Handle/Simple Volume_button 110×110 preserveAspect`（框 46.811×35.406）
        /// <summary>行高 = 原版 `FPS Limit` 的 `m_SizeDelta.y`（**105**，= 94.50 屏 px ÷ 0.9）。</summary>
        public const float FpsRowH = 105f;
        /// <summary>`Title` 在行内的框（左 16 · 顶 −14 · 宽 309.6 · 高 62）—— ⚠️ **顶是负的**（框探出行顶 14 px，
        /// 原版就是这样；上一行 `Vsync` 的框跟它重叠 9 px，但两边的**字**都在各自框里居中 ⇒ 画面上不撞）。</summary>
        public const float FpsTitleL = 16f, FpsTitleTop = -14f, FpsTitleW = 309.55f, FpsTitleH = 62f;
        /// <summary>`FPS Slider` 在行内的框：左 266 · 顶 84.2 · 宽 491.18 · 高 13。</summary>
        public const float FpsSliderL = 266f, FpsSliderTop = 84.2f, FpsSliderW = 491.18f, FpsSliderH = 13f;
        /// <summary>`Handle Slide Area` 的 `m_SizeDelta.x = −10` ⇒ 滑区宽 = 491.18 − 10 = **481.18**（手柄的行程）。</summary>
        public const float FpsTrackInset = 10f;
        /// <summary>手柄的 `m_AnchoredPosition.x` —— 🔴 `Slider` 只驱动手柄的**锚点**、**不动这个偏移**
        /// （实测：`d:/Unity/…/com.unity.ugui/Runtime/UGUI/UI/Core/Slider.cs:618-623` 只写
        /// `m_HandleRect.anchorMin/anchorMax`）⇒ 手柄中心 = 轨道左 **+ 12** + 值/2 × 481.18。
        /// ⚠️ 这根和**音频页**那三根是同一个值（`RectTransform_-6029089631055872090` 也是 11.99988）。</summary>
        public const float FpsHandleOffset = 12f;
        /// <summary>手柄**实画**边长：框 46.811×35.406 + 110×110 **方图** + `preserveAspect` ⇒ 取短边 35.406
        /// （`Image.GetDrawingDimensions` 的 else 分支：`r.width = r.height × spriteRatio`）。
        /// ⚠️ 与音频页那三根**不是**一个数（那边的手柄框只有 22.406 高 ⇒ 画 22.406）。</summary>
        public const float FpsHandleSquare = 35.406f;
        /// <summary>九宫格端帽 = `m_Border ÷ m_PixelsPerUnitMultiplier(2)`：轨道 184÷2 = **92**、填充 30÷2 = **15**。
        /// 🔴 这是**未缩放**的设计值；`NineOut` 的 `borderOutPx` 跟 `worldW` 是同一套（**画布 px**，
        /// `ImageQuad` 那句「角块不缩放，按 108 px = 1 世界单位」）⇒ **调用点必须 `× RootScale`**
        /// （同 `AuTrackH` 那条「别传裸 13」的口径）。</summary>
        public const float FpsBarCap = 92f, FpsFillCap = 15f;
        /// <summary>三个刻度（原版是 `FPS Slider` 的**子件**）：框 228.02 × 62 · 字号 42 · 居中 · 灰 (0.745)。
        /// 🔴 三个 x 是**相对行左沿**（`FPS Limit`）的 —— 原版 dump 里它们与滑块自己的 x 不同源
        /// （滑块在行内是 266.0，刻度是 163.0 / 403.9 / 640.0），⛔ 别拿滑块左沿去加。</summary>
        public const float FpsTickW = 228.02f, FpsTickH = 62f, FpsFont = 42f;
        public static readonly float[] FpsTickX = { 163f, 403.9f, 640f };
        /// <summary>三个刻度框的**顶**（行内）—— ⚠️ 第 3 个比前两个高 **4.9**（原版 `m_AnchoredPosition.y`
        /// 37.49998474 vs 32.60000229，**原版自己就不齐**）⇒ 照抄，别「顺手对齐」。</summary>
        public static readonly float[] FpsTickTop = { 27.1f, 27.1f, 22.2f };
        public static readonly string[] FpsTickName = { "30 FPS", "60 FPS", "Unlimited" };
        /// <summary>刻度上印的字（原版 TMP 的 `m_text` 是 `'30'` / `'60'` / `'Ilimitado'`（**本地化词条**）；
        /// 英文正式文案本地拿不到 —— 照文件头 ② 的口径用英文 `Unlimited`，同页签文案的处理）。</summary>
        public static readonly string[] FpsTickText = { "30", "60", "Unlimited" };
        public static readonly Color FpsTickColor = new Color(0.745f, 0.745f, 0.745f, 1f);
        public const string ArtFpsBar = "Volume_bar_inactive", ArtFpsFill = "Volume_bar_active",
                            ArtFpsHandle = "Volume_button";

        // 联机页（**这一页是我们设计的**，见文件头 ①）
        public const float OnRoleT = 280f, OnRoleB = 340f, OnRoleW = 300f, OnRoleGap = 20f;
        public const float OnLabelT = 370f, OnFieldT = 400f, OnFieldH = 60f, OnFieldW = 500f;
        public const float OnPassLabelT = 480f, OnPassT = 510f;
        public const float OnBtnT = 610f, OnBtnH = 80f, OnBtnW = 300f;
        public const float OnFillT = 610f, OnFillW = 120f;   // 【刷新】钮（IP 框右边）
        public const float OnStatusT = 730f;

        public const float FontLabel = 40f, FontSmall = 34f, FontButton = 40f;

        // 图（名字都是导入后的文件名 —— `CardArt.MenuUi` **不做空格→下划线转换**）
        public const string ArtPopup = "40k_popup";
        public const string ArtFill = "40k_popup_texture";
        public const string ArtCloseBg = "UI_Button_Round_background";
        public const string ArtCloseIcon = "40k_bt_close";
        public const string ArtSep = "40k_Separator_Fade_Sides_Vertical";
        public const string ArtTabBg = "40K_settings_button";
        /// <summary>页签**选中态**的底图。🔴 **2026-10-03 就地更正（A21）**：原来写的是 `…_selected` ——
        /// **原版选中态用的是 `_hover`**（`EverguildToggle.onSprite = 40K_settings_button_hover`，
        /// 判据 → `资料/普查产出_1003/按钮悬停图_普查.md` §一 + 块 1；同族 `PlayerProfileWindow` /
        /// `LeaderboardWindow` / `ChatPanel` / `AchievementsMenu` 四处**早就是这么写的**）。
        /// ⇒ 原版那边 `_selected` 是**悬停**图，本窗把两态配反了。</summary>
        public const string ArtTabBgSel = "40K_settings_button_hover";
        public const string ArtButton = "40K_button";
        public const string ArtToggleOn = "40K_toggle_on";
        public const string ArtToggleOff = "40K_toggle_off";
        public const string ArtInputBg = "InputFieldBackground";
        public const float PopupTexW = 359f, PopupTexH = 336f;
        public static readonly Vector4 PopupBorder = new Vector4(169f, 160f, 169f, 160f);
        /// <summary>`40k_popup_texture` 的平铺格 = 128 ÷ `ppuMultiplier 2`。</summary>
        public const float FillTilePx = 64f;
        /// <summary>`40K_button` 的九宫格 border（贴图 px）—— 同 `SettingsPanel` 投降钮那份实测值。</summary>
        public static readonly Vector4 BtnBorder = new Vector4(234f, 46f, 234f, 46f);
        public const float BtnTexW = 489f, BtnTexH = 107f;
        /// <summary>原版按钮绿（`Resign` 钮 / `OkButton` 同一支色）。</summary>
        public static readonly Color BtnGreen = new Color(0.3686f, 0.8941f, 0.5874f, 1f);
        public static readonly Color BtnGrey = new Color(0.62f, 0.62f, 0.66f, 1f);

        // 渲染队列：整窗一档（设置窗是弹窗，要盖住所有页；`Tooltip` 更高）
        public const int QShade = 3130, QPanel = 3131, QFill = 3132, QContent = 3133, QText = 3134, QOverlay = 3135;

        // ============================================================ 状态

        public SettingsTab Current { get; private set; }
        public static SettingsWindow Instance { get; private set; }

        // ---- 自检/将来接线要用的只读口（**别为了好看把它们藏起来**：自检拿不到就只能瞎猜）----
        public WfSlider[] AudioSliders { get { return _audioSliders; } }
        public Label QualityLabel { get { return _qualityLabel; } }
        public Label StatusLabel { get { return _statusLabel; } }
        public NetRole Role { get { return _role; } }
        public string Flash { get { return _flash; } }
        public Transform HostBlock { get { return _hostBlock; } }
        public Transform ClientBlock { get { return _clientBlock; } }
        /// <summary>当前显示的那一对输入框（主机块 / 客机块）。</summary>
        public MenuInputField IpField { get { return _role == NetRole.Host ? _ipField : _ipField2; } }
        public MenuInputField PwdField { get { return _role == NetRole.Host ? _pwdField : _pwdField2; } }

        /// <summary>🆕 **2026-10-05（A81）**：压暗层的**命中区**节点（「点窗外关窗」）—— 自检用
        /// （`MenuDraw.ShadeRuleOk(darkHit, qShade, qContentMin, out why)` 的 `darkHit`）。</summary>
        public Transform ShadeHit { get { return transform.Find("BackgroundHit"); } }

        readonly List<ImageQuad> _tabBgs = new List<ImageQuad>();
        /// <summary>页签底图对应的按钮（A17 换图用）—— **选中态是别人改底图的** ⇒ 换完要同步
        /// 按钮记的「常态图」，否则悬停退出会把选中态还原成未选中的图。</summary>
        readonly List<WindowButton> _tabWbs = new List<WindowButton>();
        readonly List<Transform> _pages = new List<Transform>();
        readonly List<string> MissingArt = new List<string>();

        // 联机页的控件
        MenuInputField _ipField, _pwdField;
        ImageQuad _roleHostBg, _roleClientBg;
        Transform _hostBlock, _clientBlock;
        Label _statusLabel;
        NetRole _role = NetRole.Host;
        string _flash;                                   // 最后一次操作的结果（人话）

        public static SettingsWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Main Menu Settings Window");
            var win = go.AddComponent<SettingsWindow>();
            win.type = WindowType.Popup;
            // 🔴 **2026-10-06（A154）就地订正**：这里原来是 `WindowsPlacement.Popup`(15) —— **原版写的是 5 = Canvas**。
            //   判据 = 原版那颗 MB `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2730265326332837798.json`
            //   （挂在窗体根 GO `-9019961019057471578` 上，与根 RT 的 `m_GameObject` 互校过）的 `windowsPlacement: 5`；
            //   枚举值 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/WindowsPlacement.cs`（`None=0/Canvas=5/World=10/Popup=15`）。
            //   ⛔ **别推广成「大家都该是 5」**：原版 141 个带该字段的实例里 **15(Popup) 有 96 个（68%）、
            //   5(Canvas) 只有 10 个（7%）**；我们能对上的 26 扇窗里**只有这一扇**不一致
            //   （逐窗表 → `资料/普查产出_1006/A154_A155_窗口档位与缩放.md` §①-a/①-b）。
            //   ⚠️ 我们这套窗口栈里这个档位**只决定挂在哪颗锚点下**（绘制走 `ImageQuad` 显式队列、命中走 `PointerLayer`，
            //   都不看父链）⇒ 画面零影响；但它是一处**与原版的语义偏离**（原版三颗 Holder 是**有序兄弟**、
            //   档位决定叠放与命中优先级，且只有 `3 - PopUp Holder` 额外挂了 `Canvas(overrideSorting)` + `CustomRaycaster`）。
            win.placement = WindowsPlacement.Canvas;
            win.closeOnEsc = true;
            // 🔴 **2026-10-06（A165）就地订正（铁律 5）**：这里原来是 `1f`，注释还写着「实证 1.0」—— **抄错了**。
            //   同一颗 MB（`MonoBehaviour_2730265326332837798.json`）逐字段实读写的是
            //   **`extraScaleSmallScreen: 1.2000000476837158`**（1.2 家族：`BaseOfferPopup`×21 / `RankedRewardEventWindow` /
            //   `ChangeNameWindow` / `BoosterInfoPopup` 也都是 1.2）。⇒ **小屏 UI 开**时本窗放大 **1.2**。
            //   ⚠️ 这一扇 prefab 里**没有**烤 `menuScale`（窗口根上带成品的只有 `TrophyInfoPopup` 那 3 扇）
            //   ⇒ 「不覆盖」那一条与本窗无关，直接走 `extra` 这一支。
            win.extraScaleSmallScreen = 1.2f;             // 原版 MB 原文 1.2（⛔ 不是 1.0）
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            Instance = win;
            return win;
        }

        /// <summary>扳手：**原版未缩放的矩形** → 世界里的实际矩形（根 `m_LocalScale = 0.9`）。
        ///
        /// 🔴 **2026-09-26 实测**：**不能**用「把根节点 `localScale` 设成 0.9」来复刻这个 0.9（**结论仍成立，
        ///   但理由要改，见下面那条订正**）⇒ **把 0.9 烘进每一个矩形**（本文件的画图包装函数统一先过
        ///   `Screen()`），根节点保持 scale 1。
        ///   ⚠️ 这条与工程里那句「分层要用渲染队列不能用 z」同族：**我们的量测/命中都活在 scale 1 那一帧里**。
        ///
        /// 🔴 **2026-10-06 订正（铁律 5 · A165）**：本注原来写的机制是「父节点的缩放**对它不起作用**（实测：
        ///   75px 的钮挂在 0.9 的根下，渲出来仍是 **75.0**）」—— **那半句量错了对象**：`ImageQuad.WorldW/WorldH`
        ///   只是「传进去的那个数」（`_worldH` / `_worldH × _aspect`），**本来就不含父链缩放**；所以「渲出来仍是 75.0」
        ///   量的是**代码值**，不是渲染。**真正的渲染**走 Unity 的层级世界矩阵 —— 同一个矩阵既然把位置挪了 0.9
        ///   （本注后半句自己观察到了），就**也**会把网格乘 0.9（网格是**局部空间**的：`RebuildMesh` 写的顶点是 ±WorldW/2）。
        ///   **旁证（本仓自己的代码就靠这条）**：`Battle/WfSlider.cs:158` 的填充条宽度 = 给根设 `localScale.x`；
        ///   `Battle/SkillPanel.cs:248` 直接写 `q.transform.localScale = W01(宽) / q.WorldW`（= 「让这颗 quad 的世界宽 = 目标」）；
        ///   `Battle/AttackSelector.cs:550` 的图标也是 `localScale = s`。
        ///   ⇒ **结论（烘进矩形、根保持 1）仍然照做**，但换一条站得住的理由：**我们的量测与命中都活在「scale = 1」那一帧**
        ///   （`ImageQuad.WorldW`、`LayoutSpace.PxX/PxY`、`PointerLayer.HitBoxPx` 都只读代码值/世界坐标、**不除也不乘
        ///   `lossyScale`**）⇒ 根上一带缩放，**画面会对、量出来的数全不对**（断言假绿/假红），命中区也会比画出来的小。
        ///   🔴 **这条订正由自检兜底**：`Editor/SettingsScene.cs` 的 A165 ⑤ 那条**前提断言**量的是
        ///   `MeshRenderer.bounds`（世界空间，**渲染真值**）—— 它绿 = 本条成立；它红 = 老注成立，
        ///   那时 `TransformScalerBySmallScreenUI` 得换实现（不能再往窗口根上乘）。
        ///   （A165 的小屏缩放器**就是**往窗口根上乘的 —— 它跟本窗那个 0.9 是两件事：0.9 是**固定的版面**，
        ///   小屏倍数是**运行时可变的**，运行时没法重建所有矩形的坐标。）</summary>
        public static PxRect Screen(float x1, float y1, float x2, float y2)
        {
            const float cx = 960f, cy = 540f;
            float S(float v, float c) { return c + (v - c) * RootScale; }
            return new PxRect(S(x1, cx), S(y1, cy), S(x2, cx), S(y2, cy));
        }

        public override void Open()
        {
            Build();
            OpenTab(Current);
            // 会话还没起来就先建一台（主机【保存】/ 客机【检查连接】都要它）
            NetRuntime.Ensure(transform.root);
        }

        public void CloseWindow() { Close(); }

        // ============================================================ 建

        void Build()
        {
            var root = transform;
            // ⚠️ **根节点保持 scale 1** —— 原版那个 0.9 由 `Screen()` 烘进坐标（见 `Screen` 的注释）
            for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);
            _tabBgs.Clear(); _tabWbs.Clear(); _pages.Clear(); MissingArt.Clear();

            // 1) 压暗整屏（`Menu Dark Background`：无 sprite 的纯色块）
            Node(root, "Menu Dark Background", 960f - ShadeW * 0.5f, 540f - ShadeH * 0.5f,
                 960f + ShadeW * 0.5f, 540f + ShadeH * 0.5f);
            Solid(root, "Menu Dark Background", 960f, 540f, ShadeW, ShadeH, ShadeColor, QShade);
            // 🔴 **2026-10-05（A81）**：压暗层的**点击区**（「点窗外关窗」）—— **原来零 `ShadeHit(`**
            //   （`:219-221` 只建了 4574.60×2572.36 的那层压暗底，点了什么也不发生）。
            //   判据：原版是压在 `Menu Dark Background` **自身节点**上的 `BackgroundCloseButton`，
            //   由窗口类自己挂/摘 —— `SettingsMenu__Awake.c` 的**同一段**给 `param_1[0x14]`
            //   （关窗钮的 `Button.m_OnClick`，`+0x100`）与 `param_1[0x15]`（`BackgroundCloseButton.onClick`，
            //   `+0x28`）**挂的是同一个处理函数**（虚表 `*(*param_1+0x1c0)`）；`SettingsMenu__OnDestroy.c`
            //   把背景那条摘掉。⇒ 动作照抄本窗那颗关窗钮（`:238-242` 的 `Close()`），两颗行为**必须一致**。
            //   档 = **压暗层自己那一档 `QShade`(3130)**，**严格低于**本窗内容命中区档 `QOverlay`(3135)
            //   （本窗全部内容命中区 —— 关窗钮 / 页签 / 画质下拉 / 三颗开关 / 校准钮 / 联机页各钮 /
            //   两个输入框 —— 都在 `QOverlay`）。
            //   ⚠️ **矩形与上面那层逐值相同**（同样过 `Screen()`：本窗根 scale 恒 1、那个 0.9 是烘进坐标的，
            //   见 `Screen` 的注释）—— 别拿未缩放的 `ShadeW/ShadeH` 直接建，那会比可见的压暗底大一圈。
            //   出处 → `资料/待办判据_阶段二与联机.md` §A81 · 公共件规矩 → `MenuDraw.ShadeHit` 的注释。
            {
                var sr = Screen(960f - ShadeW * 0.5f, 540f - ShadeH * 0.5f,
                                960f + ShadeW * 0.5f, 540f + ShadeH * 0.5f);
                MenuDraw.ShadeHit(root, sr, QShade, QOverlay, () => Close(), "BackgroundHit");
            }

            // 2) 弹窗本体
            var area = Node(root, "Menu Area", PopL, PopT, PopR, PopB);
            Nine(area, "Generic Popup Background", PopL, PopT, PopR, PopB, ArtPopup, PopupTexW, PopupTexH,
                 PopupBorder, QPanel, Color.white);
            // 🆕 **2026-10-06（A94）：弹窗面板底图吸收点击**。判据 = 原版 prefab
            //   `Main Menu Settings Window > Menu Area > Generic Popup Background` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读），原版 rect = 391.3,164.8→1538.2,923.6。
            //   🔴 **必须过 `Screen()`**：本窗根 scale 恒 1、原版那个 0.9 是**烘进坐标**的
            //   （见 `Screen` 的注释）⇒ `PopL..PopB` 是**未缩放**值，直接传会与画出来的面板差 11%。
            MenuDraw.Absorb(root, "AbsorbHit", Screen(PopL, PopT, PopR, PopB), QShade, QOverlay);
            Node(area, "Mask", MaskL, MaskT, MaskR, MaskB);
            Tiled(area, "Background fill", FillL, FillT, FillR, FillB, ArtFill, FillTilePx, QFill);
            Rect(area, "Separators", BarSepL, BarSepT, BarSepR, BarSepB, ArtSep, QPanel);

            // 3) 关闭钮（圆底 + 图标；**图标是钮的子节点** —— 原版就是这么套的）
            var closeN = Node(area, "Generic Close Button", CloseL, CloseT, CloseR, CloseB);
            var closeBgQ = Rect(closeN, "bg", CloseL, CloseT, CloseR, CloseB, ArtCloseBg, QContent);
            Rect(closeN, "Icon", CloseIconL, CloseIconT, CloseIconR, CloseIconB, ArtCloseIcon, QOverlay);
            // 🆕 A17：原版这一颗 `trans=2`、`m_TargetGraphic` **就是它自己**，
            // 悬停把 `UI_Button_Round_background` 换成 **`40k_bt_close_hover`**（普查 §块 4 第 11 行；
            // 实测 `Main Menu Settings Window>Menu Area>Generic Close Button`：75×75 · HL=40k_bt_close_hover）
            Hit(closeN, "Hit", CloseL, CloseT, CloseR, CloseB, QOverlay, () =>
            {
                Debug.Log("[Settings] 关闭钮");
                Close();
            }, closeBgQ, ArtCloseBg, "40k_bt_close_hover");

            // 4) 左栏三个页签 + 三页内容
            BuildTabs(area);
            _pages.Add(BuildGraphicsPage(area));
            _pages.Add(BuildAudioPage(area));
            _pages.Add(BuildOnlinePage(area));
        }

        void BuildTabs(Transform area)
        {
            var bar = Node(area, "Tab Buttons", BarL, BarT, BarR, BarB);
            // 三个键（原版这一列有 5 个，我们只建 3 个 —— 文件头 ③）
            var specs = new[]
            {
                new { Tab = SettingsTab.Graphics, Label = "Graphics", Icon = "40K_settings_button_graphics" },
                new { Tab = SettingsTab.Audio,    Label = "Audio",    Icon = "40K_settings_button_quality" },   // 原版 Media 页用的就是 quality 那张
                new { Tab = SettingsTab.Online,   Label = "Online",   Icon = "40K_settings_button_account" },   // ⚠️ 我们挑的（联机页原版没有）
            };
            for (int i = 0; i < specs.Length; i++)
            {
                float t = BarT + BarPadTop + i * TabBtnH, b = t + TabBtnH;
                var page = Node(bar, specs[i].Label, BarL, t, BarR, b);
                var bg = Rect(page, "button_bg", BarL, t, BarR, b, ArtTabBg, QContent);
                _tabBgs.Add(bg);
                // 图标 141×107、文字 155×40 —— 模板位是「布局跑之前」的，横向**按居中**摆（我们的推导，文件头）
                // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版 `Menu Area > Mask Tabs buttons >
                //   Tab Buttons > {General/Media/Account/Graphics/Support} > Icon` 全是 **PA=1 + Simple**，
                //   贴图 `40K_settings_button_*` **122×104** 塞进 141.41×106.82 ⇒ 原版实绘 **125.3×106.82**，
                //   我们 141 宽 ⇒ **宽 13%**。（⚠️ 那 6 个同尺寸候选的 pid 取不到，分不出哪一份，但尺寸一致。）
                float cm = (BarL + BarR) * 0.5f;
                Rect(page, "Icon", cm - 141f * 0.5f, t + 13f, cm + 141f * 0.5f, t + 120f, specs[i].Icon, QOverlay,
                     null, true);
                var lb = Text(page, "Tab Toggle Title", specs[i].Label, cm - 155f * 0.5f, cm + 155f * 0.5f,
                              t + 106f, t + 146f, 35f, Color.white, QText);
                var tab = specs[i].Tab;
                // 🆕 A17：原版页签是 `EverguildToggle`（`onSprite = 40K_settings_button_hover` ·
                // `offSprite = 40K_settings_button`），而 **`m_SpriteState` 的悬停图是 `…_selected`**
                // —— 正好是 `WindowButton` 那张表里的一行 ⇒ 直接按常态图名绑。
                var tabHit = Hit(page, "Hit", BarL, t, BarR, b, QOverlay, () => OpenTab(tab), bg, ArtTabBg);
                var twb = tabHit != null ? tabHit.GetComponent<WindowButton>() : null;
                if (twb != null && !_tabWbs.Contains(twb)) _tabWbs.Add(twb);
            }
        }

        /// <summary>换页。**只切 `activeSelf`，不重建**（同 `GameWindowWithTabs.ChangeTab` 的口径）。</summary>
        public void OpenTab(SettingsTab t)
        {
            Current = t;
            for (int i = 0; i < _pages.Count; i++)
                if (_pages[i] != null) _pages[i].gameObject.SetActive(i == (int)t);
            for (int i = 0; i < _tabBgs.Count; i++)
            {
                if (_tabBgs[i] == null) continue;
                // 选中的用 `_selected` 那张（原版 General 默认选中用的其实是 `_hover` —— 两态差得很少，我们用 selected）
                bool on = i == (int)t;
                var onTex = Tex(on ? ArtTabBgSel : ArtTabBg);
                _tabBgs[i].SetTexture(onTex);
                // 🔴 `SetTexture` 会把 `_aspect` 冲成**贴图自己的**比值 ⇒ 必须把「按原版矩形定的」那个比值拉回来
                //（同族先例 `BattleLogPanel:532` · `AlliancesTab:189`；漏了的话**屏幕形状不变、逻辑宽度被改掉**）
                _tabBgs[i].SetAspect((BarR - BarL) / TabBtnH);
                if (i < _tabWbs.Count && _tabWbs[i] != null) _tabWbs[i].SetNormalTex(onTex);
            }
            if (t == SettingsTab.Online) RefreshOnline();
            Debug.Log($"[Settings] 切到 `{t}` 页");
        }

        // ============================================================ 页 1：图像（原版 `Graphics Tab`）

        Transform BuildGraphicsPage(Transform area)
        {
            var page = Node(area, "Graphics Tab", TabsL, TabsT, TabsR, TabsB);
            PageTitle(page, "Graphics");

            // ① `Quality Selector`：下拉框（左） + 说明字（右）
            var row = Node(page, "Quality Selector", QualL, QualT, QualR, QualB);
            // 🔴 **2026-10-03 就地更正（A17 顺带查出的偏离）**：常态图原来画的是 `40K_button` ——
            // **原版是 `40K_dropdown_field_closed`**（实测 `Main Menu Settings Window>Menu Area>Mask Tabs buttons>
            //  Tab Buttons>Graphics Tab>Quality Selector>Quality DropDown`：**727×102 Simple**；
            //  悬停换成 `40K_dropdown_field_opened`）。普查 §块 4 第 13 行。
            var qualQ = Rect(row, "Quality DropDown", QualL, QualT, QualBoxR, QualB, "40K_dropdown_field_closed", QContent);
            _qualityLabel = Text(row, "Quality Value", QualityName(), QualL + 20f, QualBoxR - 40f, QualT, QualB,
                                 FontLabel, Color.white, QText);
            var lb = Text(row, "Quality selector text", "Quality", QualTextL, QualR, QualT, QualB,
                          FontLabel, Color.white, QText);
            if (lb != null) AlignLeft(lb, new PxRect(QualTextL, QualT, QualR, QualB));
            Hit(row, "QualityHit", QualL, QualT, QualBoxR, QualB, QOverlay, CycleQuality,
                qualQ, "40K_dropdown_field_closed");
            Debug.Log("[Settings] 图像页：这一列照原版运行时排法 —— `Small Screen UI`(第 0 行) · **`Auto Zoom`(第 1 行, A172 补)** · "
                    + "`VSync`(第 2 行) · **`FPS limit` 滑块**(第 3 行)。"
                    + "🔴 旧值（A165/A168/A170）把 `VSync`/`FPS` 摆在**第 5 / 6 行** —— 那是 `menu_dump` 按"
                    + "「7 行全在」排出来的**编辑器快照**；运行时 `Hi FPS` 与 `Android extra compatibility` **都不在**"
                    + "（`GraphicsTab.OnSetup` 尾部链式 `SetActive` 无条件跑，VA 反汇编钉死）⇒ **格位号整体提前 3 格**。"
                    + "⇒ 这一列现在**整列都塞得进视口**（内容高 346.92 < 视口高 521.51）⇒ **不裁切、也没有可停留的滚动范围**"
                    + "（= 原版：`Content.m_SizeDelta.y = 300` + CSF `m_VerticalFit = 0` ⇒ `CalculateOffset` 恒 0；"
                    + "Elastic 下能抖/回弹但停不住）。"
                    + "⛔ **没建的**：`Text In Hand Selector`（原版出厂 `m_IsActive = 0`）· `Hi FPS` · "
                    + "`Android extra compatibility`（后两个运行时被 `OnSetup` 关掉，原版也不画）；"
                    + "🔴 `Use super sampling` **没建**：原版它只在那 5 档里的 **Ultra** 出现"
                    + "（`allowSuperSampling` = 0/0/0/0/1），而我们**没有超采样能力**（URP `m_MSAA 1` / `m_RenderScale` PC=1、Mobile=0.8，"
                    + "谁也不受设置控制）⇒ 建了就是**假开关**。要补它得先做真的超采样开关 + 一份「哪档允许」的表；"
                    + "补上之后它插在 **第 2 行**、`Vsync`/`FPS` 整体 +1（= 原版 Ultra 那一档，内容高 508.205）。");

            // ② 这一列 = **真的 `Scroll View`**（原版 `ScrollRect` + `Viewport(RectMask2D)` + VLG `Content`）
            //    里面装的是那几行（**0 起**：第 0 行 = Small Screen UI · 第 1 行 = Auto Zoom ·
            //    第 2 行 = Vsync · 第 3 行 = FPS limit —— 运行时排法见 `SmallScreenRow` 那条注释）。
            //    🔴 第 0 行是 **A165** 补的（原版 `GraphicsTab.smallScreenToggle`）、第 1 行是 **A172** 补的
            //    （原版 `GraphicsTab.autoZoom`）；
            //    ⚠️ **Small Screen UI 的文案是我们写的**：原版那颗 `Label` 的 `m_text` 是**西班牙语**
            //    `'Aumentar tamaño de UI'`（= 把 UI 放大），TMP 上**没挂 I2 词条** ⇒ 英文正式文案本地拿不到
            //    （同文件头 ② 那条口径）。`Auto Zoom` 那颗原版印的就是英文 `'Auto zoom'` ⇒ 照抄。
            //    🆕 **2026-10-06（A170）**：A168 那版把第 5、6 两行**上移 67.34**当临时落法 —— 本件改成
            //    **照原版搭滚动视图**、两行回原位（`GfxRowsShift` 已删）；**A172** 再把格位号整体提前 3 格。
            //    结构 / 每一条字段的出处 → 上面那组 `GfxScroll*` 常量的注释。
            //    · `Viewport` 的矩形就是**裁切边界**（原版挂 `RectMask2D`）⇒ 存进 `_gfxClip`，
            //      画这一列的内容时**每次都带着它**（`MenuDraw` 的 `Rect`/`Nine`/`Text`/`Hit` 都吃 `clip`）。
            //    · `MenuScroll` 是**全壳唯一一份滚动实现**（`Shell/MenuScroll.cs`）—— 接线复用，别自己写偏移/夹取。
            {
                var sv = Node(page, "Scroll View", GfxScrollL, GfxScrollT, GfxScrollR, GfxScrollB);
                var vp = Node(sv, "Viewport", GfxViewL, GfxScrollT, GfxViewR, GfxScrollB);
                _gfxClip = Screen(GfxViewL, GfxScrollT, GfxViewR, GfxScrollB);
                // 🔴 **重建窗口时先撤掉上一批滚动区**（`Build()` 每次开窗都跑、子节点全删了重建，而
                //    `Owner` 是这个**窗口根**（重建时它不死）⇒ 光靠「宿主销毁」判不出旧条目已经没用）。
                //    判据与那颗地雷 → `Shell/PlayerProfileWindow.cs:594-599` / `PointerLayer.UnregisterOwnedBy`。
                PointerLayer.UnregisterOwnedBy(gameObject);
                // 内容高 **346.923** < 视口高 **521.5072** ⇒ `MaxOffset` 是**负数**、`ClampHi` 收成 0
                // ⇒ 这一列**停不住任何位移**（= 原版 `CalculateOffset` 恒 0）；Elastic 仍能抖/回弹（同原版）。
                // （`MenuScroll` 活在**画布 px** 那一帧里，同本窗其余量测口径 ⇒ 内容高要 × `RootScale`。）
                _gfxScroll = MenuScroll.TopAligned(_gfxClip.Value, GfxContentH * RootScale);
                // 原版 `ScrollRect.m_MovementType = 1`（**Elastic**：拖过头有橡皮筋、松手回弹）
                // —— `MenuScroll.Elastic` 默认 `false`（Clamped），这里是**逐处不同**的那一格，必须显式给。
                _gfxScroll.Elastic = true;
                _gfxScroll.Owner = gameObject;      // 切到别的页签 / 关窗 ⇒ 指针层跳过它
                _gfxScroll.OnChanged = RebuildGfxRows;   // ⚠️ 滚轮只改 `Offset`、**画是调用方的事**（不接 = 滚了什么都不动）
                PointerLayer.RegisterScroll(_gfxScroll); // 滚轮才会找到它
                _gfxContent = Node(vp, "Content", GfxViewL, GfxScrollT, GfxViewL + (ChkR - ChkL),
                                   GfxScrollT + GfxContentH);
            }
            // ③ 四行内容（**都建在 `Content` 底下**，按 `MenuScroll.Offset` 偏移、由 `_gfxClip` 裁）
            //    ⚠️ 序：先把档位定下来（开窗那一次**只摆值、不写帧率** = 原版 `OnSetup` 的 `SetValueWithoutNotify`），
            //    再建行 —— 建行时按 `_fpsIndex` 摆滑块（`PlaceFps`）。
            SetFpsIndex(FpsIndexOfTarget(Application.targetFrameRate), false);
            RebuildGfxRows();
            return page;
        }

        /// <summary>这一列那一份滚动区（自检读它：档位 / 视口 / 能滚多远 / 直的还是横的）。</summary>
        public MenuScroll GfxRowsScroll { get { return _gfxScroll; } }
        /// <summary>这一列的 `Content` 节点（自检量「四行在不在里头」）。</summary>
        public Transform GfxContent { get { return _gfxContent; } }

        /// <summary>这一列那一份滚动区 / 内容节点 / 裁切边界（**画布 px**）。
        /// 🔴 `_gfxClip` 非空时，这一列**所有**画出来的东西都要带着它 —— 原版那层 `RectMask2D`
        /// 对图与文字一视同仁（判据 → `MenuDraw.ClipRect` / `ClipText` 的注释）。</summary>
        MenuScroll _gfxScroll;
        Transform _gfxContent;
        PxRect? _gfxClip;

        /// <summary>这一列当前的滚动量（**未缩放设计 px**）= `MenuScroll.Offset ÷ RootScale`。
        /// 行顶统一减它 —— `Screen()` 是仿射的，所以设计坐标减 `d` = 画布坐标正好减 `0.9d`。</summary>
        float GfxScrolledPx { get { return _gfxScroll != null ? _gfxScroll.Offset / RootScale : 0f; } }

        /// <summary>重画这一列的四行（滚轮/拖拽改了偏移、或窗口重建时调）。
        /// ⚠️ 「先清再建」—— `MenuScroll.OnChanged` 会重入（同 `BattleLogTab.RebuildRows` 那条）。
        /// ⚠️ 视口外的整块**由 `MenuDraw` 自己挡掉**（`clip` 全覆盖 ⇒ 图/文字/命中区都不会建出来，
        /// 等价原版 `RectMask2D` 的渲染与射线两面）⇒ 这里**不**另写一份 `Intersects` 判断
        /// （两处写同一条规则 = 迟早不一致，CLAUDE.md §三）。
        /// 🔴 四行的**格位号 = 原版运行时那一档**（`SmallScreenRow` 那条注释里的两种排法）——
        /// ⛔ 别改动它们的顺序/序号：序号错一格，整列的 y 就错 80.641。</summary>
        public void RebuildGfxRows()
        {
            if (_gfxContent == null) return;
            MenuDraw.ClearChildren(_gfxContent);
            BuildCheckRow(_gfxContent, "Small Screen UI", SmallScreenRow, () => SmallScreenUI.Enabled, ToggleSmallScreenUI);
            // 🆕 **2026-10-07（A172）**：`Auto Zoom` —— 原版运行时**一直在**的那一行（`GraphicsTab.autoZoom`）。
            // 原版那颗 `Label` 的 `m_text` 印的就是英文 `'Auto zoom'` ⇒ 照抄（不像另两颗是西语）。
            BuildCheckRow(_gfxContent, "Auto Zoom", AutoZoomRow, () => AutoZoom.Enabled, ToggleAutoZoom,
                          () => "Auto zoom");
            BuildCheckRow(_gfxContent, "VSync", VsyncRow, () => QualitySettings.vSyncCount > 0, ToggleVsync);
            BuildFpsRow(_gfxContent);
        }

        Label _qualityLabel;

        /// <summary>这一列里的一行「方框 + 文字 + 整行命中区」。
        /// 🔴 行顶 = 原版那一列 VLG 排出来的第 `row` 格（`ChkT + row × ChkRowStep`）**减去当前滚动量**
        /// （`GfxScrolledPx` —— 现在恒 0，见 `GfxContentH` 那条）⇒ 格位号就是绝对版面。
        /// 三件都带 `_gfxClip`（原版那层 `RectMask2D`）。</summary>
        void BuildCheckRow(Transform content, string nodeName, int row, Func<bool> state, Action onClick,
                           Func<string> labelText = null)
        {
            float t = ChkT + row * ChkRowStep - GfxScrolledPx, b = t + ChkRowH;
            var n = Node(content, nodeName, ChkL, t, ChkR, b);
            var box = Rect(n, "Toggle", ChkL, t, ChkL + 119f, b, state() ? ArtToggleOn : ArtToggleOff,
                           QContent, null, false, _gfxClip);
            var lb = Text(n, "Label", labelText != null ? labelText() : nodeName, ChkL + 130f, ChkR, t, b,
                          FontLabel, Color.white, QText, _gfxClip);
            if (lb != null) AlignLeft(lb, new PxRect(ChkL + 130f, t, ChkR, b));
            Hit(n, "Hit", ChkL, t, ChkR, b, QOverlay, () =>
            {
                onClick();
                // 切完只换这一格的图与字（**别重建 quad** —— 重建会一层层叠上去，旧的那张还在）
                var tex = Tex(state() ? ArtToggleOn : ArtToggleOff);
                if (box != null && tex != null) box.SetTexture(tex);
                if (lb != null && labelText != null) lb.SetText(labelText());
            }, null, null, null, _gfxClip);
        }

        static string QualityName()
        {
            int lv = QualitySettings.GetQualityLevel();
            var names = QualitySettings.names;
            return (names != null && lv >= 0 && lv < names.Length) ? names[lv] : ("Level " + lv);
        }

        /// <summary>🔴 **自检的注入点**（产品路径下是 null ⇒ 走真 `QualitySettings`）。
        /// 为什么要它：`QualitySettings.SetQualityLevel` 在编辑器里会**把 `QualitySettings.asset` 写脏**
        /// （2026-09-26 实测：跑一次自检，`m_CurrentQuality` 被改了、文件被 Unity 重新序列化），
        /// 而**自检不该改工程设置**（同族规矩：自检不许动玩家的真存档）。</summary>
        public static Action<int> QualitySetterOverride;
        /// <summary>同上，给 `VSync` 那行用（`QualitySettings.vSyncCount` 也会写脏工程设置）。</summary>
        public static Action<int> VSyncSetterOverride;

        static void ApplyQuality(int level)
        {
            if (QualitySetterOverride != null) { QualitySetterOverride(level); return; }
            QualitySettings.SetQualityLevel(level, true);
        }
        static void ApplyVSync(int count)
        {
            if (VSyncSetterOverride != null) { VSyncSetterOverride(count); return; }
            QualitySettings.vSyncCount = count;
        }

        static string FpsText()
        {
            int f = Application.targetFrameRate;
            return f <= 0 ? "unlimited" : (f + " fps");
        }

        void CycleQuality()
        {
            var names = QualitySettings.names;
            if (names == null || names.Length == 0) { Debug.LogWarning("[Settings] 没有画质档可切"); return; }
            int next = (QualitySettings.GetQualityLevel() + 1) % names.Length;
            ApplyQuality(next);
            if (_qualityLabel != null) _qualityLabel.SetText(QualityName());
            _flash = "画质档 → " + QualityName();
            Debug.Log("[Settings] " + _flash);
        }

        void ToggleVsync()
        {
            int next = QualitySettings.vSyncCount > 0 ? 0 : 1;
            ApplyVSync(next);
            _flash = "VSync → " + (next > 0 ? "开" : "关");
            Debug.Log("[Settings] " + _flash);
        }

        /// <summary>🆕 **A165**：原版 `GraphicsTab.SmallScreenToggleClick(bool)` —— **同时**写
        /// `GameStaticData.smallScreenUI`(+0x11c) 与 `smallUIChosenManually`(+0x12e)（反编译逐句实读）。
        /// <para>⚠️ **已经开着的窗不会跟着变** —— 原版那一段在 `GameWindow.Open()` 里、只在**开窗那一刻**挂/覆盖缩放器
        /// ⇒ 点完必须说清这一点（红线：不许让玩家以为它没作用）。</para></summary>
        void ToggleSmallScreenUI()
        {
            SmallScreenUI.Set(!SmallScreenUI.Enabled);
            _flash = "Small Screen UI → " + (SmallScreenUI.Enabled ? "开" : "关");
            Debug.Log("[Settings] " + _flash + "（原版 `GraphicsTab.SmallScreenToggleClick`：同时置 "
                    + "`GameStaticData.smallScreenUI` 与 `smallUIChosenManually`）—— ⚠️ 只对**之后打开**的窗口生效："
                    + "原版那一段写在 `GameWindow.Open()` 里（→ `Shell/TransformScalerBySmallScreenUI.cs`）。");
        }

        /// <summary>🆕 **A172**：原版 `GraphicsTab.AutoZoomClick(bool)` —— **同时**写
        /// `GameStaticData.autoZoom`(+0x125) 与 `autoZoomChosenManually`(+0x12f)（反编译逐句实读，与上面那颗同形）。
        /// <para>🔴 这一格在原版是**战斗相机的自动缩放**（`BattleSettingsWindow__OnAutoZoomChanged` →
        /// `CombatAutoZoom.ResetCameraZoomUIAction`），**我们还没做那个功能** ⇒ 点它**必须出声**
        /// （红线：不许让玩家以为它有作用）。值本身照原版存下来（`AutoZoom.Set`）。</para></summary>
        void ToggleAutoZoom()
        {
            AutoZoom.Set(!AutoZoom.Enabled);
            _flash = "Auto Zoom → " + (AutoZoom.Enabled ? "开" : "关");
            Debug.LogWarning("[Settings] " + _flash + "（原版 `GraphicsTab.AutoZoomClick`：同时置 "
                    + "`GameStaticData.autoZoom` 与 `autoZoomChosenManually`）—— 🔴 **我们这套还没有那个消费者**："
                    + "原版这一格控制的是**战斗相机的自动缩放**（`CombatAutoZoom`，见 "
                    + "`BattleSettingsWindow__OnAutoZoomChanged`），我们没做该组件 ⇒ **这一格目前不产生任何效果**"
                    + "（值会记下来，等于是给将来留的开关）。");
        }

        // ============================================================ 图像页第 3 行：FPS 上限（原版 `FPS Slider`）
        //
        // 判据（2026-10-06 A168 全部实读）：
        //  ① **原版这一格是滑块，不是勾选行** —— `GraphicsTab.cs`（签名桩）`private Slider fpsLimit` +
        //     `FPSLimitValueChanged(float)`；实例 = 解包 `bundle_menus_assets_all/MonoBehaviour/
        //     MonoBehaviour_5246199328614809510.json` 的 `fpsLimit`（PathID 5646828332852936614），
        //     本体 `MonoBehaviour_5646828332852936614.json` = `m_MinValue 0` · `m_MaxValue 2` ·
        //     `m_WholeNumbers 1` · `m_Value 0` · `m_Direction 0`(LeftToRight) · `m_TargetGraphic` = 手柄那颗 Image。
        //  ② **取值集合 = {0,1,2}**（uGUI `Slider.Set`：`Mathf.Round` 后再 `Clamp(0,2)`）——
        //     刻度就是它子件上印的那三个：`30` / `60` / `Unlimited`。
        //  ③ **值怎么变成帧率**：`GraphicsTab__FPSLimitValueChanged.c` 只做
        //     `GameStaticData.FPSLimit = (int)value`（+ 置存盘脏位）；真正的映射在
        //     `PlayerDataManager__ApplySettingsOptions.c`（与 `__ApplyGraphicsQuality.c` **逐字节同形**）：
        //         iVar = FPSLimit;
        //         if (iVar != 0) { if (iVar != 1 && iVar == 2) → SetTargetFramerate(-1); else → SetTargetFramerate(60); }
        //         else → SetTargetFramerate(30);
        //     ⇒ 0→**30** · 1→**60** · 2→**−1（不限帧）** · 🔴 **其它任何非 0 值都落到 60**（⛔ 不是「非 1 就给无限」）。
        //     `PlayerDataManager__SetTargetFramerate.c` 最后一行就是 `Application.targetFrameRate = 值`。
        //  ④ 几何 / 贴图 / 端帽：全部落在上面那组 `Fps*` 常量里（逐条带出处）。
        //
        // 🔴 **为什么不复用 `Battle/WfSlider`（工程里那份公共滑块）**：它把三处**逐实例不同**的值写成了常量，
        //    而这一行与音频页那三根**每一处都不一样**：
        //      ① 渲染队列硬编码 `QSlider = 3000` —— 本窗压暗层 3130 / 面板 3131 / 内容 3133 都在它上面
        //         ⇒ 画出来会被压暗一层（音频页那三根现在就是这个状态 → 报告 §顺手发现）；
        //      ② 手柄边长硬编码 `22.406`（那是**音频页**手柄框的高）—— 这一行原版是 **35.406**（见 `FpsHandleSquare`）；
        //      ③ 手柄偏移：它按 `TravelRightPx = 10` 从轨道左端起排，原版这一行的手柄中心还多一个 **+12**。
        //    `Battle/WfSlider.cs` 不在 A168 白名单里 ⇒ 本行写**专用的一份**（只画 + 只算值）。
        //    要合并的话 = 给 `WfSlider.Create` 补 `queue / handlePx / handleOffset` 三个参数（报告里记着）。

        Transform _fpsRow, _fpsSliderRoot, _fpsFill;
        ImageQuad _fpsHandle;
        /// <summary>当前档位 = 原版 `GameStaticData.FPSLimit`（**0/1/2**，不是帧率本身 —— 帧率见 `FpsOfIndex`）。</summary>
        int _fpsIndex = 2;
        bool _fpsDragging;
        /// <summary>按在手柄上时「指针 − 手柄中心」的差（**画布 px**，与 `SetFpsFromCanvasX` 同一个坐标系）
        /// —— 原版 `Slider.m_Offset` 的等价物。按在轨道空白处时恒 0（原版 `OnPointerDown` 的 else 分支
        /// 就是 `m_Offset = Vector2.zero`）。</summary>
        float _fpsGrabPx;
        /// <summary>轨道在**原版坐标系**里的矩形（未缩放设计 px，左上原点）—— 命中 / 摆件都用它。</summary>
        float _fpsL, _fpsR, _fpsT, _fpsB;

        /// <summary>档位 → 帧率（= `PlayerDataManager.ApplySettingsOptions` 那段 if 链，逐字节照抄）。
        /// 🔴 **2 才是「不限帧」；其它任何非 0 值都给 60** —— 别写成「不是 1 就给无限」。</summary>
        public static int FpsOfIndex(int index)
        {
            if (index == 0) return 30;          // 原版：`iVar1 == 0` ⇒ `SetTargetFramerate(0x1e)`
            if (index == 2) return -1;          // 原版：`iVar1 != 1 && iVar1 == 2` ⇒ `(0xffffffff)` = −1
            return 60;                          // 原版：`iVar1 != 0` 的其余分支 ⇒ `(0x3c)`
        }

        /// <summary>反向：当前 `Application.targetFrameRate` 落在哪一档（开窗时用它把滑块摆到当前值上）。
        /// ⚠️ 30/60 之外（含 ≤0 = 不限帧、以及我们没设过的默认 0）都归到 **2 Unlimited** ——
        /// 与旧的勾选行「`targetFrameRate &lt;= 0` 就显示 unlimited」同口径。</summary>
        public static int FpsIndexOfTarget(int targetFps)
        {
            if (targetFps == 30) return 0;
            if (targetFps == 60) return 1;
            return 2;
        }

        /// <summary>这一列的**第 3 行** = `FPS Limit`（原版 `GraphicsTab.fpsLimit`，一个 3 档 Slider）。
        /// 🔴 行顶 = 原版 VLG 的第 3 格（`ChkT + 3 × ChkRowStep` = 674.42）**减去当前滚动量**（`GfxScrolledPx`，现在恒 0）。
        /// ⚠️ **A172 起它在视口里**（行顶 674.42 + 滑块底 771.62 都 < 视口底 954.00）⇒ 打开这一页就看得见滑块；
        /// A168/A170 那版摆在「第 6 格」、整根滑块落在视口下沿之外（要靠滚动才看得见）—— 那是**错的格位号**。
        /// 三件都带 `_gfxClip`：视口外的东西连 quad / 文字 / 命中区都不建（= 原版 `RectMask2D` 的两面）。</summary>
        Transform BuildFpsRow(Transform content)
        {
            float top = ChkT + FpsRow * ChkRowStep - GfxScrolledPx;
            _fpsRow = Node(content, "FPS Limit", ChkL, top, ChkR, top + FpsRowH);

            // ① 行标题（原版是 `FPS Slider` **父节点**下那颗 `Title`：框 = 行内 [16,−14]–[325.6,48]）
            //    ⚠️ 原版 TMP 的 `m_text` 是西班牙语 `'Límite de FPS'` 且**没挂 I2 词条** ⇒ 英文正式文案
            //    本地拿不到，照文件头 ② 的口径写英文（与页签 / Small Screen UI 那两处同一处理）。
            float tl = ChkL + FpsTitleL, tt = top + FpsTitleTop;
            var lb = Text(_fpsRow, "Title", "FPS limit", tl, tl + FpsTitleW, tt, tt + FpsTitleH,
                          FpsFont, Color.white, QText, _gfxClip);
            if (lb != null) AlignLeft(lb, new PxRect(tl, tt, tl + FpsTitleW, tt + FpsTitleH));

            // ② 滑块本体（原版 `FPS Slider`：行内 [266,84.2]–[757.2,97.2]）
            _fpsL = ChkL + FpsSliderL; _fpsR = _fpsL + FpsSliderW;
            _fpsT = top + FpsSliderTop; _fpsB = _fpsT + FpsSliderH;
            _fpsSliderRoot = Node(_fpsRow, "FPS Slider", _fpsL, _fpsT, _fpsR, _fpsB);

            // ②-a 轨道 `Background`：Sliced · `Volume_bar_inactive` 400×31 · border (184,0,184,0) ÷ ppuMul 2 = 92
            //     🔴 三层的**层序靠队列**（`QContent` &lt; `QText` &lt; `QOverlay` —— 轨道 &lt; 填条 &lt; 手柄），
            //     ⛔ **别靠 z**：三张图在屏幕上位置不同，透明物体按「到相机的距离」排 —— 填条中心偏左时
            //     它离相机**更远**，会被轨道盖住（`CLAUDE.md` §三 那条：分层要用渲染队列、不能用 z）。
            //     ⚠️ 整块在视口外 ⇒ `MenuDraw.Nine` **连节点都不建**（A172 起这一行在视口里 ⇒ 正常建出来；
            //     那条「视口外不建」的机制仍留着，由自检把内容人为撑高来验）。
            NineOut(_fpsSliderRoot, "Background", _fpsL, _fpsT, _fpsR, _fpsB, ArtFpsBar, 400f, 31f,
                    new Vector4(184f, 0f, 184f, 0f),
                    new Vector4(FpsBarCap * RootScale, 0f, FpsBarCap * RootScale, 0f), QContent, _gfxClip);

            // ②-b 填条那一层的**父节点**（原版 `Fill`；宽随值变 ⇒ 每次重建它的九宫格，见 `PlaceFps`）
            _fpsFill = Node(_fpsSliderRoot, "Fill", _fpsL, _fpsT, _fpsL + FpsSliderW, _fpsB);

            // ②-c 三个刻度（原版是 `FPS Slider` 的**子件**；字号 42 · 居中 · 灰 (0.745,0.745,0.745,1)）
            //     ⚠️ `FpsTickX` 是**相对行左沿**（`FPS Limit`）的，⛔ 不是相对滑块左沿 —— 原版 dump 里
            //     那三行的 x 是 `163.0 / 403.9 / 640.0`，而滑块自己在行内的 x 是 266.0（两者不同源）。
            for (int i = 0; i < 3; i++)
            {
                float x = ChkL + FpsTickX[i], y = top + FpsTickTop[i];
                Text(_fpsSliderRoot, FpsTickName[i], FpsTickText[i], x, x + FpsTickW, y, y + FpsTickH,
                     FpsFont, FpsTickColor, QText, _gfxClip);
            }

            // ②-d 手柄 `Handle`：`Volume_button`（110×110 **方图**）+ `preserveAspect` ⇒ 实画 35.406 见方
            //     ⚠️ 它**必须过 `LayoutSpace.Px`**：本窗把根那层 0.9 **烘进矩形**（见 `Screen()` 的注释），
            //       所以传进去的是**屏幕上的**边长 35.406 × 0.9 = 31.87 画布 px。
            _fpsHandle = ImageQuad.Create(_fpsSliderRoot, Tex(ArtFpsHandle), Vector3.zero,
                                          LayoutSpace.Px(FpsHandleSquare * RootScale),
                                          new Vector2(0.5f, 0.5f), "Handle");
            if (_fpsHandle != null) _fpsHandle.SetRenderQueue(QOverlay);   // 三层里的最上层（见 ②-a 那条注）

            // ②-e 命中区 = **整根轨道**。原版那条射线是这么来的：`Background` 那颗 Image 的
            //     `m_RaycastTarget = 1`，点它冒泡到父件的 `Slider`（`OnPointerDown` 的 else 分支 =
            //     跳到点的那个位置，**不是**只有拖手柄才算）。⚠️ 手柄在 2 档会探出轨道右端 19.7 px，
            //     那一小块在**我们这里**点不到（原版点得到，因为它也是 raycast 目标）—— 报告里记着。
            //     🔴 `_gfxClip`：轨道没滚进视口之前**这条命中区根本不存在**（原版 `RectMask2D` 连射线一起裁）。
            Hit(_fpsRow, "Hit", _fpsL, _fpsT, _fpsR, _fpsB, QOverlay, FpsClickAtPointer,
                null, null, null, _gfxClip);

            // ③ 按**当前档位**把滑块摆到对应档 —— ⛔ 这里**不再**从 `Application.targetFrameRate` 反查：
            //    开窗那一次的反查在 `BuildGraphicsPage`（只做一次），滚轮/重建不该把玩家的选择冲掉。
            PlaceFps(_fpsIndex);
            return _fpsRow;
        }

        /// <summary>点一下（= 原版 `Slider.OnPointerDown` 的 else 分支：**跳到点的那个位置**）。
        /// ⚠️ 批处理里 `Mouse.current` 是 null ⇒ 自检走 `SetFpsFromCanvasX`，不走这里。</summary>
        void FpsClickAtPointer()
        {
            var m = Mouse.current;
            if (m == null) return;
            SetFpsFromPointer(LayoutSpace.ScreenToWorld(m.position.ReadValue(), LayoutSpace.Cam));
        }

        /// <summary>按住拖动（= 原版 `Slider.OnDrag` → `UpdateDrag`）。⚠️ 批处理没有帧循环 ⇒ 自检直调。</summary>
        void UpdateFpsDrag()
        {
            if (_fpsSliderRoot == null || !_fpsSliderRoot.gameObject.activeInHierarchy)
            { _fpsDragging = false; return; }
            var m = Mouse.current;
            if (m == null) { _fpsDragging = false; return; }
            var wp = LayoutSpace.ScreenToWorld(m.position.ReadValue(), LayoutSpace.Cam);
            if (m.leftButton.wasPressedThisFrame)
            {
                var s = Screen(_fpsL, _fpsT, _fpsR, _fpsB);
                float px = LayoutSpace.PxX(wp.x), py = LayoutSpace.PxY(wp.y);
                // 🔴 **必须同时卡 x 和 y**：只卡 x 的话，点画质那一行（x 与轨道重叠）也会改帧率。
                //    范围 = 轨道那一块 + 手柄/端帽的余量（横向 24、纵向 16 画布 px = 手柄半高 15.9）。
                //    ⚠️ 原版的射线目标比这**宽**一点：`Background` 那张图（轨道）+ 三个刻度（TMP 默认也吃射线）；
                //    我们只按轨道那一块做命中 —— 点刻度会**落到吸收层**（不做事），但 `Update` 这一路照样会取值
                //    ⇒ 观感与原版一致（报告 §没查清 里记着这条差）。
                if (px < s.x1 - 24f || px > s.x2 + 24f || py < s.y1 - 16f || py > s.y2 + 16f)
                { _fpsDragging = false; return; }
                // 🔴 **被视口裁掉的那一截点不到**（原版 `RectMask2D` 连射线一起裁，见 `MenuDraw.ClipRect`）——
                //    这条滑块在打开这一页时整根都在视口下沿之外 ⇒ 不加这一句会在「看不见的地方」改帧率。
                if (!MenuDraw.Visible(new PxRect(px - 0.5f, py - 0.5f, px + 0.5f, py + 0.5f), _gfxClip))
                { _fpsDragging = false; return; }
                _fpsDragging = true;
                // 按在**手柄**上 ⇒ 记住那个差（原版 `m_Offset`）；按在轨道空白处 ⇒ 0
                _fpsGrabPx = 0f;
                if (_fpsHandle != null)
                {
                    float hx = _fpsHandle.transform.position.x * 108f + 960f;
                    float half = FpsHandleSquare * RootScale * 0.5f;
                    if (px >= hx - half && px <= hx + half) _fpsGrabPx = px - hx;
                }
                SetFpsFromCanvasX(px);
            }
            else if (_fpsDragging)
            {
                if (m.leftButton.isPressed) SetFpsFromCanvasX(LayoutSpace.PxX(wp.x));
                else _fpsDragging = false;
            }
            // 🔴 **拖这根滑块时不滚这一列** —— 原版是 uGUI `Slider` 自己吃掉了拖拽事件（不会冒泡到 `ScrollRect`）。
            //    我们这套里「拖拽归谁」由 `PointerLayer` 判（只看 10px 阈值，它不认识滑块）⇒ 拖滑块会顺手把整列也滚了。
            //    兜底：拖动期间每帧把这一区停掉（`MenuScroll.Stop` 清 `_dragging`，`PointerLayer` 下一帧再调
            //    `DragTo` 就是空操作，惯性/回弹也一起清掉）。
            //    ⚠️ 正解在**共用件**（让「按钮吃掉拖拽」或给 `MenuScroll` 一个 `NoDrag`）—— 那两件不在本件白名单，
            //    报告 §顺手发现 里记着。
            if (_fpsDragging && _gfxScroll != null) _gfxScroll.Stop();
        }

        /// <summary>按**世界坐标**取值（拖动 / 点击 / 自检共用的入口）。返回值变了没有。</summary>
        public bool SetFpsFromPointer(Vector3 world) => SetFpsFromCanvasX(LayoutSpace.ToPixel(world).x);

        /// <summary>按**画布 px 的 x** 取值 —— 逐句照 uGUI `Slider.UpdateDrag`：
        /// `normalized = clamp01((x − 滑区左) / 滑区宽)` ⇒ `值 = round(normalized × (max−min) + min)`（`m_WholeNumbers`）
        /// ⇒ `Clamp(0,2)`。滑区 = `Handle Slide Area`（左沿 = 轨道左沿、宽 481.18 × 0.9 画布 px）。</summary>
        public bool SetFpsFromCanvasX(float px)
        {
            if (_fpsSliderRoot == null) return false;
            var s = Screen(_fpsL, _fpsT, _fpsR, _fpsB);
            float areaW = (FpsSliderW - FpsTrackInset) * RootScale;
            float x = px - _fpsGrabPx - s.x1;
            float raw = Mathf.Clamp01(x / areaW) * (FpsMax - FpsMin) + FpsMin;
            int idx = Mathf.Clamp((int)Mathf.Round(raw), (int)FpsMin, (int)FpsMax);   // `m_WholeNumbers = 1`
            if (idx == _fpsIndex) return false;
            SetFpsIndex(idx, true);
            return true;
        }

        /// <summary>设档位（0/1/2）。`fire = true` 才真的写 `Application.targetFrameRate` ——
        /// 开窗那一次走 `fire: false`（原版 `GraphicsTab.OnSetup` 结尾那一下也是**只填值不通知**
        /// —— 反编译里是 `fpsLimit` 的一个 vtable 调用，与它旁边 `Toggle.SetIsOnWithoutNotify` /
        /// `Dropdown.SetValueWithoutNotify` 同族；⚠️ 那个调用的**参数**枚举体没解出字段偏移，见报告 §没查清）。</summary>
        public void SetFpsIndex(int idx, bool fire)
        {
            idx = Mathf.Clamp(idx, (int)FpsMin, (int)FpsMax);
            bool changed = idx != _fpsIndex;
            _fpsIndex = idx;
            PlaceFps(idx);
            if (!fire) return;
            int target = FpsOfIndex(idx);                     // = 原版 `ApplySettingsOptions` 的映射
            Application.targetFrameRate = target;             // = 原版 `PlayerDataManager.SetTargetFramerate`
            _flash = "帧率上限 → " + FpsText();
            Debug.Log($"[Settings] {_flash}（原版 `GraphicsTab.FPSLimitValueChanged` 只写 "
                    + $"`GameStaticData.FPSLimit = {idx}`；`PlayerDataManager.ApplySettingsOptions` 再映射成 "
                    + $"{target} → `Application.targetFrameRate`）" + (changed ? "" : "（值没变）"));
        }

        /// <summary>当前档位（0/1/2）= 原版 `GameStaticData.FPSLimit`（自检 / 将来接线用）。</summary>
        public int FpsIndex { get { return _fpsIndex; } }
        /// <summary>滑块那三件的父节点（自检量几何用）。</summary>
        public Transform FpsSliderRoot { get { return _fpsSliderRoot; } }

        /// <summary>按档位摆 `Fill` 与 `Handle` —— 原版 `Slider.UpdateVisuals` 是用**锚点**驱动这两件的：
        /// · `Fill`：父 = 滑块根（宽 491.18）、`anchorMax.x = 值/2` ⇒ **宽 = 值/2 × 491.18**；0 档宽 0 ⇒ 什么都不画
        ///   （原版 uGUI 那条矩形宽 0、也是空的 —— 同族先例 `Shell/AchievementsMenu.cs:304`）。
        /// · `Handle`：父 = `Handle Slide Area`（宽 481.18）、`anchorMin.x = anchorMax.x = 值/2`，
        ///   **再加上它自己那个不变的 `anchoredPosition.x = 12`** ⇒ 中心 = 轨道左 + 12 + 值/2 × 481.18。
        ///   ⚠️ 2 档时手柄中心 = 493.18 ⇒ 会探出轨道右端 19.7 px，**原版就是这样**（别「顺手」夹回来）。
        /// · 两件的**纵向中心都 = 轨道中心**（原版 Handle 的 `anchorMin.y=0 / anchorMax.y=1` ⇒ 被轨道高撑开、
        ///   再对称加高 22.406/2 ⇒ 中心不动）。</summary>
        void PlaceFps(int idx)
        {
            // 🔴 **必须把分母转成 float** —— `FpsMin/FpsMax` 是 `int`，`(idx - FpsMin) / (FpsMax - FpsMin)` 会走**整数除法**：
            //    0/2 = 0 ✓、**1/2 = 0** ✗、2/2 = 1 ✓ ⇒ **只有档 1 是错的**（Fill 宽 0 = 不画、手柄停在档 0 的位置），
            //    而档 0 / 档 2 照样对 ⇒ **一眼看不出**。2026-10-06 首次 `SettingsScene.Run` 实测就是这个（3 条红全是档 1），
            //    自检那条「档 1 的 `Fill` 宽 = 221.03」正是为它准备的。
            float n = (idx - FpsMin) / (float)(FpsMax - FpsMin);     // 0 / 0.5 / 1
            float w = n * FpsSliderW;
            if (_fpsFill != null)
            {
                MenuDraw.ClearChildren(_fpsFill);
                if (w > 0.5f)
                    NineOut(_fpsFill, "fill", _fpsL, _fpsT, _fpsL + w, _fpsB, ArtFpsFill, 64f, 31f,
                            new Vector4(30f, 0f, 30f, 0f),
                            new Vector4(FpsFillCap * RootScale, 0f, FpsFillCap * RootScale, 0f), QText,
                            _gfxClip);
            }
            if (_fpsHandle != null)
            {
                float cx = _fpsL + FpsHandleOffset + n * (FpsSliderW - FpsTrackInset);
                float cy = (_fpsT + _fpsB) * 0.5f, half = FpsHandleSquare * 0.5f;
                var s = Screen(cx - half, cy - half, cx + half, cy + half);
                // 🔴 **手柄是常驻的一条 quad**（不随值/滚动重建 —— 重建会把自检先抓住的那个引用打成空）
                //    ⇒ 它的裁切得自己来：整块在视口外就关掉、压在视口边上就缩到可见那一块并**按同一块截 uv**
                //    （`uv` 那两行与 `MenuDraw.Rect` 里的算式**同一条**，⛔ 别自己另发明一套 —— 不截 uv 会把图压扁）。
                //    为什么非做不可：**滚到底时手柄底沿（设计 y ≈ 957.2）仍在视口下沿 954.00 之外** ——
                //    原版那层 `RectMask2D` 会切掉它，不切就探出弹窗内层底（956.39）约 1px。
                var full = new PxRect(s.x1, s.y1, s.x2, s.y2);
                PxRect vis;
                if (!MenuDraw.ClipRect(full, _gfxClip, out vis))
                {
                    if (_fpsHandle.gameObject.activeSelf) _fpsHandle.gameObject.SetActive(false);
                    return;
                }
                if (!_fpsHandle.gameObject.activeSelf) _fpsHandle.gameObject.SetActive(true);
                _fpsHandle.SetWorldHeight(LayoutSpace.Px(vis.H));
                _fpsHandle.SetAspect(vis.W / Mathf.Max(1e-6f, vis.H));
                _fpsHandle.SetUvRect(new Rect((vis.x1 - full.x1) / full.W, (full.y2 - vis.y2) / full.H,
                                              vis.W / full.W, vis.H / full.H));
                _fpsHandle.transform.localPosition = MenuDraw.Local(_fpsSliderRoot, vis.x1, vis.y1, vis.x2, vis.y2);
            }
        }

        /// <summary>档位的上下限 = 原版 `FPS Slider` 的 `m_MinValue` / `m_MaxValue`（**0 / 2**，
        /// 且 `m_WholeNumbers = 1` ⇒ 只可能是 0、1、2 三档）。</summary>
        public const int FpsMin = 0, FpsMax = 2;

        // ============================================================ 页 2：音频（原版 `Media Tab`）

        Transform BuildAudioPage(Transform area)
        {
            var page = Node(area, "Media Tab", TabsL, TabsT, TabsR, TabsB);
            PageTitle(page, "Audio");

            var box = Node(page, "Audio Settings", AuL, AuT, AuR, AuB);
            var names = new[] { "Music", "Sound Effects", "Voice-overs" };
            var get = new Func<float>[] { () => WarpforgeAudio.Music, () => WarpforgeAudio.SoundFx, () => WarpforgeAudio.VoiceOver };
            var set = new Action<float>[] { WarpforgeAudio.SetMusic, WarpforgeAudio.SetSoundFx, WarpforgeAudio.SetVoiceOver };
            _audioSliders = new WfSlider[3];

            for (int i = 0; i < 3; i++)
            {
                // 🔴 **逐行查表** —— 三行行高不相等（105/106/106）、行顶步进还各加 11.2667，
                //    不能再用「`AuT` + 序号 × 一个行高」推（旧值就是这么来的，第 2/3 行偏上 11.26/23.53）。
                float t = AuRowTops[i], b = t + AuRowHs[i];
                var rowN = Node(box, names[i] + " Container", AuL, t, AuR, b);
                // 标签在上、滑块在下（原版那三行就是这个摆法）
                // 🔴 标签：原版 `Label` 锚在**行中心**（`anchoredPosition.y = +35`、pivot `(0,0.5)`、
                //    高 62/63/63）⇒ **标签顶 = 行顶 − 13.5**（三行同值，与行高无关）。
                float lt = t + AuLabelTopOff, lbB = lt + AuLabelHs[i];
                var lb = Text(rowN, "Label", names[i], AuL, AuR, lt, lbB, FontLabel, Color.white, QText);
                if (lb != null) AlignLeft(lb, new PxRect(AuL, lt, AuR, lbB));
                // 🔴 滑块本体：`WfSlider`（**工程里唯一一份**滑块实现，交互/音频接线都在那儿）
                //    ⚠️ 轨道宽与中心都按 **0.9 烘过**的值给（`WfSlider` 也是按世界尺寸画的）
                //    原版 `… Slider` 锚在**行中心**（`anchoredPosition.y = −11.1`）⇒ 中心 = 行顶 + 行高/2 + 11.1
                //    🔴 **2026-10-05（A96）**：轨道高也要一起烘 —— `AuTrackH`（原版 13）**× `RootScale`**
                //       = 11.7 画布 px。⛔ 别传裸 `AuTrackH`（那会大 11%，见 `AuTrackH` 的注释）。
                var rs = Screen(AuL, t, AuR, b);
                float cx = (rs.x1 + rs.x2) * 0.5f;
                float cy = rs.y1 + (AuRowHs[i] * 0.5f + AuSliderCyOff) * RootScale;
                var center = MainMenuSubmenuWindow.Local(rowN, cx, cy);
                int idx = i;
                _audioSliders[i] = WfSlider.Create(rowN, "vol" + i, center, get[i](), v => set[idx](v),
                                                   trackW: AuTrackW * RootScale, trackH: AuTrackH * RootScale);
            }

            var note = Text(page, "Note", "音量走 AudioMixer（与对局内设置面板同一套）", AuL, AuR, AuB + 20f, AuB + 60f,
                            FontSmall, new Color(1f, 1f, 1f, 0.6f), QText);
            if (note != null) AlignLeft(note, new PxRect(AuL, AuB + 20f, AuR, AuB + 60f));
            Debug.Log("[Settings] 音频页：原版这一页还有 `WindowMode Selector`（窗口模式）—— **没建**（不做假开关）");
            return page;
        }

        WfSlider[] _audioSliders;

        // ============================================================ 页 3：联机（**我们新增的设计**）

        Transform BuildOnlinePage(Transform area)
        {
            var page = Node(area, "Online Tab", TabsL, TabsT, TabsR, TabsB);
            PageTitle(page, "Online");

            // ① 角色：主机 / 客机（用户规格：「勾选成为主机或客机」）
            _roleHostBg = RoleButton(page, "Host", 0, NetRole.Host);
            _roleClientBg = RoleButton(page, "Client", 1, NetRole.Client);
            _role = (NetRole)Mathf.Clamp(NetConfig.Current.role, 1, 2);

            // ② 主机块 / 客机块（各自一对 IP+密码 + 自己的那个钮 —— 用户规格逐条）
            _hostBlock = BuildRoleBlock(page, NetRole.Host);
            _clientBlock = BuildRoleBlock(page, NetRole.Client);

            // ③ 状态行（**不许静默**：连接结果、掉线、重连都写在这里）
            _statusLabel = Text(page, "Status", "", TitleL, TabsR - 20f, OnStatusT, OnStatusT + 60f,
                                FontSmall, new Color(1f, 0.9f, 0.6f), QText);
            if (_statusLabel != null) AlignLeft(_statusLabel, new PxRect(TitleL, OnStatusT, TabsR - 20f, OnStatusT + 60f));

            // ④ 说明行 —— ⚠️ **这一页只剩 ~70px 高，六行塞不下** ⇒ 详细的三条路做成**点一下弹窗**。
            //    （用户 2026-09-26：「我们可能是网友需要联机」—— 这件事**必须在界面上说清楚**，
            //     光写一句「请做端口映射」等于没说。）
            var note = Text(page, "Note",
                            "这一页不是原版（原版是联网游戏，没有「当主机」这回事）。\n"
                            + "IP 直连 —— 公网怎么走 / 路由器要不要放开端口：点这一行看",
                            TitleL, TabsR - 20f, OnStatusT + 70f, OnStatusT + 140f,
                            FontSmall, new Color(1f, 1f, 1f, 0.55f), QText);
            if (note != null) note.SetWrapWidth(LayoutSpace.Px(TabsR - 20f - TitleL));
            // 点这一行 ⇒ 弹「怎么联机」（三条路写清楚）
            var noteHit = Node(page, "Note Hit", TitleL, OnStatusT + 70f, TabsR - 20f, OnStatusT + 140f);
            Hit(noteHit, "Hit", TitleL, OnStatusT + 70f, TabsR - 20f, OnStatusT + 140f, QOverlay, ShowHowToConnect);

            // ⑤ 🆕 **【测外网】**（我们加的）—— 放在动作钮**右边**那片空位上。
            //    🔴 **为什么需要它**（用户 2026-09-27 的原话：「我在 IPv6 测试网站上看得到 IPv6，
            //       你这里为什么看不到」）：**「外网看到的地址」与「本机网卡上的地址」是两件事**，
            //       而玩家只看得见前者 ⇒ 不给对照，他就会以为我们那个「没有公网 IPv6」是错的。
            //    实测判据 → `资料/联机P2P_设计与交接.md` §11·4。
            //    ⚠️ 探测**在后台线程**跑（要联网）⇒ 这里只发车，结果由 `Update` 那边印出来。
            ActionButtonAt(page, "Echo", TitleL + OnBtnW + 40f, OnBtnT, OnEchoW, "Test Public IP", () =>
            {
                _flash = "正在探测「外网看到的地址」…（几秒，不影响别的操作）";
                FlashAndLog();
                NetConfig.ProbeExternalAsync(r => { _echoResult = r; _echoReady = true; });
            });

            ApplyRoleVisibility();
            return page;
        }

        // ---- 【测外网】的结果：**后台线程写、主线程印**（别在回调里碰 Unity 对象）----
        /// <summary>那颗钮的宽度（自检要拿它对位置）。</summary>
        public const float OnEchoW = 300f;
        NetConfig.ExternalAddrs _echoResult;
        /// <summary>后台线程写完之后置位（main 线程看到它才去读 `_echoResult`）。</summary>
        volatile bool _echoReady;
        bool _echoShown;

        /// <summary>把「外网看到的」与「本机网卡上的」**摆在一起说清**（这是这个功能存在的**全部理由**）。</summary>
        string EchoText(NetConfig.ExternalAddrs r)
        {
            var local6 = NetConfig.AllLocalIPv6();
            string t = "外网看到的地址（刚探的）：\n"
                     + "· IPv4：" + (string.IsNullOrEmpty(r.v4) ? "（没探到）" : r.v4 + "　← " + r.v4From) + "\n"
                     + "· IPv6：" + (string.IsNullOrEmpty(r.v6) ? "（没探到）" : r.v6 + "　← " + r.v6From) + "\n"
                     + "本机网卡上的公网 IPv6：" + (local6.Length > 0 ? local6[0] : "没有");
            if (!string.IsNullOrEmpty(r.v6) && local6.Length == 0)
                t += "\n⚠️ **两个不一样** ⇒ 上面那个 IPv6 是【路由器的】（它在做 IPv6 NAT）：\n"
                   + "　 外面看得到它，但**别人连不到你这台机器** ⇒ IPv6 直连这条路走不了。";
            else if (!string.IsNullOrEmpty(r.v6) && local6.Length > 0)
                t += "\n✅ 两边都有公网 IPv6 ⇒ 「IPv6 直连」这条路可行（**要求对面也有**）。";
            if (!string.IsNullOrEmpty(r.detail)) t += "\n" + r.detail;
            return t;
        }

        ImageQuad RoleButton(Transform page, string label, int idx, NetRole role)
        {
            float x1 = TitleL + idx * (OnRoleW + OnRoleGap), x2 = x1 + OnRoleW;
            var n = Node(page, "Role " + label, x1, OnRoleT, x2, OnRoleB);
            var bg = Rect(n, "bg", x1, OnRoleT, x2, OnRoleB, ArtButton, QContent, BtnGreen);
            var lb = Text(n, "Text", label, x1, x2, OnRoleT, OnRoleB, FontButton, Color.black, QText);
            Hit(n, "Hit", x1, OnRoleT, x2, OnRoleB, QOverlay, () =>
            {
                _role = role;
                NetConfig.Current.role = (int)role;
                NetConfig.Save();
                ApplyRoleVisibility();
                Debug.Log($"[Settings] 联机角色 → {role}");
            });
            return bg;
        }

        Transform BuildRoleBlock(Transform page, NetRole role)
        {
            bool host = role == NetRole.Host;
            float x1 = TitleL, x2 = TitleL + OnFieldW;
            var blk = Node(page, host ? "Host Block" : "Client Block", TitleL, OnLabelT, TabsR, OnBtnT + OnBtnH);

            var ipLb = Text(blk, "IP Label", "IP address", x1, x2, OnLabelT, OnLabelT + 40f, FontLabel, Color.white, QText);
            if (ipLb != null) AlignLeft(ipLb, new PxRect(x1, OnLabelT, x2, OnLabelT + 40f));
            var pwdLb = Text(blk, "Password Label", "Password", x1, x2, OnPassLabelT, OnPassLabelT + 40f, FontLabel, Color.white, QText);
            if (pwdLb != null) AlignLeft(pwdLb, new PxRect(x1, OnPassLabelT, x2, OnPassLabelT + 40f));

            var login = NetConfig.Current;
            var ip = MenuInputField.Create(blk, "IP Field", new PxRect(x1, OnFieldT, x2, OnFieldT + OnFieldH),
                                           login.ip ?? "", "例如 192.168.1.10", ArtInputBg, QContent, QText);
            var pwd = MenuInputField.Create(blk, "Password Field",
                                            new PxRect(x1, OnPassT, x2, OnPassT + OnFieldH),
                                            login.password ?? "", "留空 = 不校验", ArtInputBg, QContent, QText);
            if (host) { _ipField = ip; _pwdField = pwd; }
            else { _ipField2 = ip; _pwdField2 = pwd; }

            if (host)
            {
                // 【刷新】：自动填本机 IP（用户规格：「IP 地址输入框右边有一个刷新按钮」）
                float fx1 = x2 + 20f, fx2 = fx1 + OnFillW;
                var rn = Node(blk, "Refresh", fx1, OnFieldT, fx2, OnFieldT + OnFieldH);
                Rect(rn, "bg", fx1, OnFieldT, fx2, OnFieldT + OnFieldH, ArtButton, QContent, BtnGrey);
                Text(rn, "Text", "Refresh", fx1, fx2, OnFieldT, OnFieldT + OnFieldH, FontSmall, Color.black, QText);
                Hit(rn, "Hit", fx1, OnFieldT, fx2, OnFieldT + OnFieldH, QOverlay, RefreshLocalIp);

                // 【保存】：记住角色/端口/密码并**开始监听**
                ActionButton(blk, "Save", OnBtnT, "Save", () =>
                {
                    var c = NetConfig.Current;
                    NetConfig.SaveAsHost(ip.Text, pwd.Text, c.port);
                    var sess = NetRuntime.Ensure()?.Reset();
                    if (sess != null && sess.StartHost(NetConfig.Current))
                        // 🔴 **把要交给朋友的那串东西直接印出来** —— 主机就这一个任务，
                        //    别让玩家自己去拼 IP 和端口（用户 2026-09-26：「主机点按钮自动填 IP，然后写密码」）
                        _flash = "✅ 主机已就绪，等着对面连进来。\n"
                               + "把这行给朋友 → " + ip.Text + " : " + NetConfig.Current.port
                               + (string.IsNullOrEmpty(pwd.Text) ? "（没设密码）" : "");
                    else _flash = "主机没起来：" + (sess != null ? sess.LastError : "NetRuntime 不在");
                    FlashAndLog();
                });
            }
            else
            {
                // 【检查连接】：点了**自动保存** IP+密码、试连、反馈成败（用户规格逐条）
                ActionButton(blk, "Check", OnBtnT, "Check Connection", () =>
                {
                    var c = NetConfig.Current;
                    NetConfig.SaveAsClient(ip.Text, pwd.Text, c.port);
                    var sess = NetRuntime.Ensure()?.Reset();
                    if (sess == null) { _flash = "NetRuntime 不在（自检里要自己建）"; FlashAndLog(); return; }
                    sess.OnCheckDone = (ok, why) => { _flash = (ok ? "✅ " : "❌ ") + why; FlashAndLog(); };
                    sess.CheckConnection(NetConfig.Current);
                    _flash = sess.StatusText;
                    FlashAndLog();
                });
            }
            return blk;
        }

        MenuInputField _ipField2, _pwdField2;

        void ActionButton(Transform page, string name, float t, string label, Action onClick)
        {
            ActionButtonAt(page, name, TitleL, t, OnBtnW, label, onClick);
        }

        /// <summary>同上，但**能指定左边距与宽度**（联机页那颗【测外网】要放在动作钮**右边**那片空位上）。</summary>
        void ActionButtonAt(Transform page, string name, float x1, float t, float w, string label, Action onClick)
        {
            float x2 = x1 + w, y2 = t + OnBtnH;
            var n = Node(page, name + " Button", x1, t, x2, y2);
            var aq = Rect(n, "bg", x1, t, x2, y2, ArtButton, QContent, BtnGreen);
            Text(n, "Text", label, x1, x2, t, y2, FontButton, Color.black, QText);
            // A17：原版 `Account Tab>Buttons/*` 那几颗同族底图（`40K_button`）都是 SpriteSwap（普查 §块 4 第 16 行，⚠️ 非同名节点）
            Hit(n, "Hit", x1, t, x2, y2, QOverlay, () => { Debug.Log($"[Settings] 点了 `{label}`"); onClick(); },
                aq, ArtButton);
        }

        int _addrIdx = -1;   // 【刷新】在多网卡之间循环：每点一次换下一个候选

        /// <summary>【刷新】：**每点一次换一个候选地址**。
        /// 🔴 **为什么不做「一次填对」**：多网卡（有线 + 无线 + VPN + 虚拟机）时**机器自己挑不准**，
        ///    而**挑错的代价是朋友连不上**。⇒ 改成**可循环**：flash 里报「第 k/n 个 · 网卡名」，
        ///    玩家一眼就认出哪块是自己的网卡，多点几下就行（输入框也**始终可手改**）。
        /// 📌 2026-09-26 之前这里走 `Dns.GetHostAddresses`，**在批处理里直接抛异常、填不出来**
        ///    —— 已改成枚举网卡（见 `NetConfig.LocalAddresses`）。</summary>
        void RefreshLocalIp()
        {
            // ⚠️ **只在这堆「能用的」里循环** —— `LocalAddresses()` 会把回环（`127.0.0.1` / `::1`）
            //    和 v6 链路本地（`fe80::`）也列出来，那些**填给对面等于没填**，不该出现在循环里。
            var all = NetConfig.LocalAddresses().FindAll(x => x.Usable);
            if (all.Count == 0)
            {
                if (_ipField != null) _ipField.SetText("127.0.0.1");
                _flash = "⚠️ 一块可用网卡都没找到 —— 只能手填地址";
                FlashAndLog();
                return;
            }
            _addrIdx = (_addrIdx + 1) % all.Count;
            var a = all[_addrIdx];
            if (_ipField != null) _ipField.SetText(a.addr);
            _flash = $"本机地址 {_addrIdx + 1}/{all.Count}：{a.Label}"
                   + (a.isV6 ? "（IPv6）" : "")
                   + (all.Count > 1 ? "　—— 再点一下换下一个" : "")
                   + (a.isVirtual
                        ? "\n⚠️ 这是「虚拟网卡」的地址（VPN / 虚拟局域网工具建的那张）。"
                        + "\n　 对面也装了同一个工具的话，直接用这个 —— 穿透由那个工具负责。"
                        : "");
            FlashAndLog();
        }

        /// <summary>「怎么联机」——**三条路写清楚**（用户 2026-09-26：「我们可能是网友需要联机」）。
        /// ⚠️ 我们**不做 NAT 穿透**，所以「公网直连」这一条**必须**借一个外部条件
        /// （公网 IPv6 / 端口映射 / 虚拟局域网工具）。**这不是偷懒** —— 打洞的第一步就要有一台
        /// **公网会合点**，而本项目**没有任何服务器**（判据 → `资料/联机P2P_设计与交接.md` §二·4）。</summary>
        void ShowHowToConnect()
        {
            // 🆕 2026-09-26：**顺带自动识别本机有没有装虚拟局域网工具**
            //    （装了就一定会有那张虚拟网卡 ⇒ 靠它认，不用问玩家）
            string vName = null;
            foreach (var a in NetConfig.LocalAddresses())
                if (a.Usable && a.isVirtual) { vName = a.nic; break; }
            // 🆕 以及**本机有没有公网 IPv6** —— 用户 2026-09-26 定了走「IPv6 直连」那条路，
            //    那他就得一眼看出自己这台够不够条件（不用去命令行敲 ping -6）。
            var v6 = NetConfig.AllLocalIPv6();

            string t =
                "三条路，从最省事开始：\n"
              + "① 同一个局域网 ⇒ 直接填主机那台机器的地址。\n"
              + "② 不在一起 ⇒ 两边装同一个虚拟局域网工具\n"
              + "　（Tailscale / ZeroTier / 蒲公英 之类），填它给的地址。\n"
              + "③ 公网直连 ⇒ 主机点【保存】时会**自动向路由器要一个端口**（UPnP）；\n"
              + "　成没成会弹一条告诉你 —— **没成**就是路由器不支持 / 关着 UPnP，\n"
              + "　那就在路由器管理页手动把那个端口转发到主机这台机器。\n"
              + "　（主机**自己**有公网 IPv6 的话填 IPv6 更省事，连映射都不用。）\n\n"
              + "⚠️ **别拿「IPv6 测试网站」当判据**：那里显示的是【**外网看到的**地址】，\n"
              + "　它有可能是**路由器的**（有些路由器在做 IPv6 NAT）⇒ 外面看得到，\n"
              + "　**但别人连不到你这台机器**。本机到底能不能被连上，看下面「本机检测」，\n"
              + "　或者点【Test Public IP】把两者摆在一起对照。\n\n"
              + "我们不做打洞（那要一台公网上的会合点 + 服务器，本项目没有）。\n\n"
              + "本机检测：\n"
              + "· 公网 IPv6：" + (v6.Length > 0
                    ? "有（" + v6[0] + "）\n  第 ③ 条路能用 —— 只要路由器放行那个 TCP 端口"
                    : "**没有** ⇒ 本机网卡上没有全局 IPv6\n"
                    + "  （⚠️ 这与「测试网站看得到 IPv6」**不矛盾** —— 那个多半是路由器的）\n"
                    + "  ⇒ 第 ③ 条只能靠**端口映射**，或者走 ① ②")
              + "\n· 虚拟局域网工具：" + (vName != null
                    ? "装了（网卡「" + vName + "」）\n  点【刷新】能切到它给的地址"
                    : "没检测到（想走 ② 就两边各装一个，Tailscale / ZeroTier 都免费）")
              + "\n· 路由器自动开端口（UPnP）：主机点【保存】时自动试 —— **成没成都会弹一条说出来**";

            var wm = WindowsManager.Instance;
            if (wm != null) { wm.ShowPopUp(t, "知道了", null); Debug.Log("[Settings] 弹了「怎么联机」"); }
            else Debug.LogWarning("[Settings] 没有 WindowsManager，弹不出「怎么联机」：\n" + t);
        }

        void ApplyRoleVisibility()
        {
            bool host = _role == NetRole.Host;
            if (_hostBlock != null) _hostBlock.gameObject.SetActive(host);
            if (_clientBlock != null) _clientBlock.gameObject.SetActive(!host);
            Debug.Log($"[Settings] 联机页显示的是「{_role}」那一块（{(_role == NetRole.Host ? "IP + 密码 + 保存 + 刷新" : "IP + 密码 + 检查连接")}）");
        }

        void RefreshOnline()
        {
            var sess = NetRuntime.Instance != null ? NetRuntime.Instance.Session : null;
            string s = sess != null ? sess.StatusText : "（会话还没建 —— 点一下 Host 的保存，或 Client 的检查连接）";
            if (_statusLabel != null) _statusLabel.SetText((_flash != null ? _flash + "\n" : "") + s);
        }

        void FlashAndLog() { RefreshOnline(); Debug.Log("[Settings] " + _flash); }

        // ============================================================ 每帧

        void Update()
        {
            // ⚠️ 批处理下 `Update` 不跑（自检自己 `Pump`）—— 这里只服务真 Play。
            // 🆕 **A168**：FPS 滑块的「按 / 拖」（原版 `Slider.OnPointerDown/OnDrag`）。
            //    ⚠️ 没走 `WindowButton.onClick`：那条路**不带落点**（`Action` 无参），而滑块要的是「点在哪」；
            //      `Hit` 那一条只负责**吃下这一下**（别让射线穿到吸收层）。批处理里自检直调 `SetFpsFromCanvasX`。
            UpdateFpsDrag();
            if (Current != SettingsTab.Online || _statusLabel == null) return;
            // 【测外网】的结果到了 ⇒ 印一次（**主线程**：回调那边只写字段、不碰 Unity 对象）
            if (_echoReady && !_echoShown) { _echoShown = true; _flash = EchoText(_echoResult); Debug.Log("[Settings] " + _flash); }
            RefreshOnline();
        }

        // ============================================================ 绘图小工具（本窗自用）
        //
        // ⚠️ 不复用 `MainMenuSubmenuWindow` 的那套：那套是**子菜单窗**（`Content Area` 在屏幕左上、
        //    左栏 165 宽贴着屏幕边）；本窗是**居中弹窗**、左栏在弹窗里（178.42 宽）。
        //    两套的矩形语义不同 ⇒ 复用得先把两边都参数化，反而更容易错。判据都在这一个文件里。

        static Transform Node(Transform parent, string name, float x1, float y1, float x2, float y2)
        {
            var s = Screen(x1, y1, x2, y2);
            return MenuDraw.Node(parent, name, new PxRect(s.x1, s.y1, s.x2, s.y2));
        }
        ImageQuad Rect(Transform p, string n, float x1, float y1, float x2, float y2, string art, int q,
                       Color? tint = null, bool keepAspect = false, PxRect? clip = null)
        {
            var s = Screen(x1, y1, x2, y2);
            return MenuDraw.Rect(p, Tex(art), new PxRect(s.x1, s.y1, s.x2, s.y2), n, q, tint, keepAspect, clip);
        }
        ImageQuad Nine(Transform p, string n, float x1, float y1, float x2, float y2, string art,
                       float tw, float th, Vector4 border, int q, Color? tint)
        {
            var t = Tex(art);
            if (t == null) return null;
            var s = Screen(x1, y1, x2, y2);
            var go = MenuDraw.Nine(p, t, new PxRect(s.x1, s.y1, s.x2, s.y2), border, tw, th, q, tint, true, n);
            return go != null ? go.GetComponentInChildren<ImageQuad>() : null;
        }
        /// <summary>九宫格（本窗用）—— 比 `Nine` 多一个 `borderOutPx`：**画出来**的端帽尺寸。
        /// 🔴 为什么需要它：原版有些 Image 带 `m_PixelsPerUnitMultiplier`，uGUI 按
        /// `m_Border ÷ ppuMultiplier` 画端帽（`Image.GenerateSlicedSprite`：
        /// `GetAdjustedBorders(border / multipliedPixelsPerUnit, rect)`，`Image.cs:1157`）
        /// —— 本页 FPS 那一行那两张是 **ppuMul 2**（184→**92**、30→**15**），而 `MenuDraw.Nine` 默认按贴图 px 原样画。
        /// ⚠️ 音频页那三根走 `Battle/WfSlider`（没传这个参数 ⇒ 端帽按 184 画）、不在本件白名单 ⇒ 另记在报告里。
        /// 宽 ≤ 0（0 档的 `Fill`，原版那条矩形宽就是 0）⇒ 不建。</summary>
        ImageQuad NineOut(Transform p, string n, float x1, float y1, float x2, float y2, string art,
                          float tw, float th, Vector4 border, Vector4 borderOut, int q, PxRect? clip = null)
        {
            var t = Tex(art);
            if (t == null || x2 - x1 <= 0.01f) return null;
            var s = Screen(x1, y1, x2, y2);
            var go = MenuDraw.Nine(p, t, new PxRect(s.x1, s.y1, s.x2, s.y2), border, tw, th, q,
                                   null, true, n, borderOut, clip);
            return go != null ? go.GetComponentInChildren<ImageQuad>() : null;
        }
        void Tiled(Transform p, string n, float x1, float y1, float x2, float y2, string art, float tile, int q)
        {
            var s = Screen(x1, y1, x2, y2);
            MenuDraw.Tiled(p, Tex(art), new PxRect(s.x1, s.y1, s.x2, s.y2), tile, q, n, null);
        }
        /// <summary>摆一段文字。🆕 `clip` 非空时**照原版 `RectMask2D` 办**（图与文字一视同仁）：
        /// 整块在视口外 ⇒ 不建；压在视口边上 ⇒ 把**渲染网格**裁到框内（`MenuDraw.ClipText` ——
        /// 裁的时机必须在**定完字号之后**，同 `MenuWindowBase.Text` 那条）。</summary>
        Label Text(Transform p, string n, string s0, float x1, float x2, float y1, float y2, float fs, Color c, int q,
                   PxRect? clip = null)
        {
            var s = Screen(x1, y1, x2, y2);
            var r = new PxRect(s.x1, s.y1, s.x2, s.y2);
            if (!MenuDraw.Visible(r, clip)) return null;
            var lb = MenuDraw.Text(p, r, s0, c, n, fs, q);
            if (lb != null && clip.HasValue) MenuDraw.ClipText(lb, clip, default(Vector2));
            return lb;
        }
        Transform Hit(Transform p, string n, float x1, float y1, float x2, float y2, int q, Action a,
                      ImageQuad target = null, string art = null, string hoverArt = null, PxRect? clip = null)
        {
            var s = Screen(x1, y1, x2, y2);
            // ⚠️ 位置实参：`MenuDraw.Hit` 在 `hoverArt` 与 `clip` 之间还有一颗 `pressedArt`（本窗不用它 ⇒ 显式 null）
            return MenuDraw.Hit(p, n, new PxRect(s.x1, s.y1, s.x2, s.y2), q, a, target, art, hoverArt, null, clip);
        }
        /// <summary>左对齐到**原版（未缩放）矩形**的左边缘 —— 内部过 `Screen()`。</summary>
        static void AlignLeft(Label lb, PxRect r)
        {
            var s = Screen(r.x1, r.y1, r.x2, r.y2);
            MenuDraw.AlignLeft(lb, new PxRect(s.x1, s.y1, s.x2, s.y2));
        }
        void Solid(Transform p, string n, float cx, float cy, float w, float h, Color c, int q)
        {
            var s = Screen(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
            MenuDraw.Rect(p, CardArt.Solid(), new PxRect(s.x1, s.y1, s.x2, s.y2), n, q, c);
        }
        void PageTitle(Transform page, string title)
        {
            var lb = Text(page, "Tab Title", title, TitleL, TitleR, TitleT, TitleB, PageTitleFontPx, Color.white, QText);
            if (lb != null) AlignLeft(lb, new PxRect(TitleL, TitleT, TitleR, TitleB));
        }
        Texture2D Tex(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = CardArt.MenuUi(name);
            if (t == null && !MissingArt.Contains(name)) { MissingArt.Add(name); Debug.LogWarning($"[Settings] 图缺了：`{name}`"); }
            return t;
        }
    }

    /// <summary>
    /// 「Auto Zoom」那一格 —— 原版 `GameStaticData.autoZoom`(+0x125) + `autoZoomChosenManually`(+0x12f) 的等价物。
    /// <para>判据 = `GraphicsTab__AutoZoomClick.c`（**同时**写那两个字段，与 `SmallScreenToggleClick` 同一形状：
    /// 一个存值、一个置「玩家手动动过」）+ `BattleSettingsWindow__OnAutoZoomChanged.c`（同一个值，战斗内那颗开关也写它）。</para>
    /// <para>🔴 **我们这套还没有消费者**（如实标注，铁律 3）：原版这个值喂的是**战斗相机的自动缩放**
    /// （`CombatAutoZoom`），我们**没做那个组件** ⇒ 值照原版存下来，但**目前不产生任何效果**；
    /// 点它必须出声（`SettingsWindow.ToggleAutoZoom` 里那条 `LogWarning`）。⛔ 别在别处再存一份。</para>
    /// <para>🔴 **两处如实标注（我们挑的，不冒充原版）**：
    /// ① **持久化** = `PlayerPrefs`（原版存的是**玩家存档**，服务器那一侧；我们没有存档系统）—— 同 `SmallScreenUI`；
    /// ② **出厂默认 = `false`**：`GameStaticData__.cctor` 里**没有**写 `+0x125` / `+0x12f` ⇒ 两者都是零初始化
    /// （对照：同一段 cctor 明写了 `+0x11c = 0`(smallScreenUI) · `+0x127 = 1`(vsync) · `+0x128 = 2`(FPSLimit) ·
    /// `+0x120 = 3`(画质档)）⇒ 我们的默认值 `0` 与原版一致。
    /// ③ **`ChosenManually` 不落盘**：原版从存档读回来，我们只做「点过就置 1」这一半
    /// （**没查清**原版还有谁读它 —— 全反编译只有写入点；同 `SmallScreenUI.ChosenManually` 那条）。</para></summary>
    public static class AutoZoom
    {
        /// <summary>我们的持久化 key（原版走玩家存档 —— 见类注释 ①）。</summary>
        public const string PrefKey = "AutoZoom";

        /// <summary>🔴 **自检注入点**：true ⇒ <see cref="Set"/> 只改内存、**不写 `PlayerPrefs`**
        /// （本工程规矩：自检不许动玩家的真设置 —— 同 `SmallScreenUI.PersistOverride`）。</summary>
        public static bool PersistOverride;

        static bool _enabled;
        static bool _loaded;

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _enabled = PlayerPrefs.GetInt(PrefKey, 0) != 0;   // 没设过 ⇒ 0 = 原版 cctor 的出厂值
        }

        /// <summary>= 原版 `GameStaticData.autoZoom`。</summary>
        public static bool Enabled { get { Load(); return _enabled; } }

        /// <summary>= 原版 `GameStaticData.autoZoomChosenManually`（由 <see cref="Set"/> 置 1）。
        /// ⚠️ 我们**不落盘**这一半（原版从存档读；见类注释 ③）。</summary>
        public static bool ChosenManually { get; private set; }

        /// <summary>= 原版 `GraphicsTab.AutoZoomClick(bool)`：**同时**写那两个字段（逐句实读）。</summary>
        public static void Set(bool on)
        {
            Load();
            _enabled = on;
            ChosenManually = true;
            if (PersistOverride) return;
            PlayerPrefs.SetInt(PrefKey, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>自检用：把内存态放回**出厂值**（`Enabled=false` / `ChosenManually=false`）。
        /// ⛔ **不动 `PlayerPrefs`**（自检跑完玩家真设置照旧）。</summary>
        public static void ResetForTest()
        {
            _loaded = true;
            _enabled = false;
            ChosenManually = false;
        }
    }

    /// <summary>
    /// 设置窗里的一个**输入框**（原版是 `TMP_InputField`；我们这套没有 uGUI 事件）。
    /// 🔴 **键盘那一层不自己写** —— 走外壳**唯一**那份文本焦点：`PointerLayer.BeginText/TypeChar/EndText`
    ///    （`Shell/PointerLayer.cs`，注释里写明「这份是给外壳/菜单用的」）。两处各写一份 = 迟早不一致。
    /// </summary>
    public class MenuInputField
    {
        public string Text { get; private set; }
        public bool Focused { get { return PointerLayer.Instance != null && PointerLayer.Instance.TextEditing && _mine; } }

        Label _label;
        string _placeholder;
        Transform _root;
        ImageQuad _bg;
        bool _mine;

        public static MenuInputField Create(Transform parent, string name, PxRect r, string initial,
                                            string placeholder, string art, int q, int qText)
        {
            var f = new MenuInputField();
            f.Text = initial ?? "";
            f._placeholder = placeholder;
            f._root = MenuDraw.Node(parent, name, r);
            var tex = CardArt.MenuUi(art);
            f._bg = MenuDraw.Rect(f._root, tex, r, "bg", q);
            f._label = MenuDraw.Text(f._root, new PxRect(r.x1 + 16f, r.y1, r.x2 - 16f, r.y2),
                                     f.Show(), Color.white, "Text", 40f, qText);
            MenuDraw.AlignLeft(f._label, new PxRect(r.x1 + 16f, r.y1, r.x2 - 16f, r.y2));
            MenuDraw.Hit(f._root, "Hit", r, q + 2, () => f.Focus());
            return f;
        }

        string Show() { return string.IsNullOrEmpty(Text) ? (_placeholder ?? "") : Text; }

        void Refresh()
        {
            if (_label == null) return;
            _label.SetText(Focused ? (Text + "_") : Show());
            _label.SetColor(string.IsNullOrEmpty(Text) ? new Color(1f, 1f, 1f, 0.45f) : Color.white);
        }

        public void Focus()
        {
            var pl = PointerLayer.Instance;
            if (pl == null) { Debug.LogWarning("[Settings] 没有 `PointerLayer` ⇒ 输入框收不到键盘"); return; }
            _mine = true;
            pl.BeginText(Text, 64, s => { Text = s; _mine = false; Refresh(); },
                         () => { _mine = false; Refresh(); },
                         s => { Text = s; Refresh(); });
            Refresh();
            Debug.Log("[Settings] 输入框获得焦点（回车确认 / ESC 取消）");
        }

        /// <summary>程序化改文本（【刷新】按钮、自检都用它 —— 批处理里没有键盘）。</summary>
        public void SetText(string s) { Text = s ?? ""; Refresh(); }
    }
}
