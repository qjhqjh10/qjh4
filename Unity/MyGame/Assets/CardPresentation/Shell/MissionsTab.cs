// MissionsTab.cs — 「日常」任务页（`Missions Tab`）· 三种任务卡 + 头部 + 周常
//
// ============================ 出处（唯一正本） ============================
// `资料/日常_原版规格.md` §三。**每一个矩形的锚点五元组都是原版 JSON 原文**，
// 由 `工具/menu_rect.py <pid> --cs` 机械吐出来、直接贴进来的（不手抄 —— 手抄锚点五元组出过一次静默的版面 bug）。
// 卡内版面按 `UguiRect.Child` 逐层算，**不是写死坐标**：所以卡被布局撑宽/撑窄时，内部会照原版规则重分布。
//
// 🔴 **四条纪律**：
//   ① **出厂 `activeSelf=false` 的件不建**（`title` / `ray target` / `body.description`…）
//      —— 原版是运行时按状态开的，见每行的 `// 出厂 inactive`。
//      ⚠️ **2026-10-04 更正（Y2 实读，铁律 5）**：这一行原来还把「**各 `debug_buttons`**」列在里面 ——
//      真包实读该节点的 **`m_IsActive = True`**（每日行那份 MB `-2794962970128279344`，同名 **9 个实例全是 True**），
//      `资料/日常_原版规格.md:222` 那张表最后一列也写 **T**。
//      ✅ **已查清（A75-④ 收口，铁律 5）**：原版 `MissionDebugButtons.Setup` 的**方法体就是 `SetActive(false)`**
//      （`d:/2/tools/all_methods.txt:9716`；与 `BattleAlliancePanel.CloseButtonClick` 同址 = ICF 折叠，**地址把名字对上了**）
//      ⇒ 那「两份说法」**不矛盾**（出厂 T、`Setup()` 里立刻关）；我们**仍然不建它**（它是调试件）—— **无欠账**。
//   ② **`Special Missions` 与 `Daily Missions` 都带 `localScale=1.15`** ⇒ **各自那棵子树**的位置、尺寸
//      （以及**字号**）都要**绕自己的 pivot 缩放**（两个节点的 pivot 都是 `(0,1)` = 左上角）。
//      不算这一步，两块都会比原版小一圈、还会偏左（正本 §三·1）。见 `ScaleAbout` / `_s` / `R()` / `FS()`。
//      🔴 **2026-10-06（A124）把 DM 那半边补上了** —— 此前只有 SM 做了（`DM_Scale` 声明了、一次都没用）。
//   ③ **被布局组排的子节点，矩形是「布局跑之前的模板位」**（四键、三行、里程碑 steps、`Daily Skulls`）
//      ⇒ 一律用 `UguiLayout` 按原版的 padTop / spacing / align 算，**别抄 JSON 的 pos**。
//   ④ **里程碑的图不在 Image 上、在脚本字段里**（`activeSprite`/`disabledSprite`，正本 §三·7）
//      ⇒ 照字段画，别照 Image 的 `sprite` 画（那个是空的）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>任务页。原版 `MissionsTab : WindowTabBase<MainMenuRewardsWindow>`。</summary>
    public class MissionsTab : WindowTabBase
    {
        public override WindowTabType Type { get { return WindowTabType.Missions; } }

        RewardsWindow _win;
        Transform _root;

        // ---- 出处：正本 §三（机械走链 `工具/menu_rect.py`；父链 Rewards 根 → Content Area → Tabs）----
        static readonly PxRect RootRect = new PxRect(RewardsWindow.ContentL, RewardsWindow.ContentT,
                                                     RewardsWindow.ContentR, RewardsWindow.ContentB);
        // `Missions Tab`      N(0,"Missions Tab", 0,0, 1,1, .5,.5, -0.244019,0.869873, 0.477783,1.74103)
        static readonly Vector2 MT_A0 = UguiRect.A00, MT_A1 = UguiRect.A11, MT_P = UguiRect.P50c,
                                MT_Pos = new Vector2(-0.244019f, 0.869873f), MT_Sz = new Vector2(0.477783f, 1.74103f);
        // `Normal Missions`   N(1, 0,1, 0,1, 0,0.5, 205.68,-388.55, 1519,723.8)
        static readonly Vector2 NM_A0 = UguiRect.A01, NM_A1 = UguiRect.A01, NM_P = new Vector2(0f, 0.5f),
                                NM_Pos = new Vector2(205.68f, -388.55f), NM_Sz = new Vector2(1519f, 723.8f);

        // ==================== 🔴 A106（2026-10-06）：两个子件的 x/宽 = 【布局位】，不是模板位 ====================
        // `Normal Missions` 上挂着 `HorizontalLayoutGroup`，`Special Missions` 上还挂着 `ContentSizeFitter`
        // ⇒ **两个子件在运行期会被排一遍**，位置**不等于** prefab 里写的那套模板值。我们此前钉在模板值上
        // （`SM_Sz.x=779.21` / `DM_Pos.x=898.933` / `DM_Sz.x=539.188`）= **一处真偏离**（铁律 11：要改）。
        // ⚠️ **本段（以及下面所有五元组/常量）是【设计空间】的布局值** —— 屏幕上画出来的还要各自再乘
        //    `localScale = 1.15`（两个子件都有）：`Special Missions` 那半边 2026-09-23（D7）起就在缩，
        //    `Daily Missions` 那半边 **2026-10-06（A124）** 才补上。缩放的口径 + 承重真值见下面 `DM_Scale` 那一段。
        //
        // **原版参数（全部实读，出处 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all` 的 prefab JSON）**
        //   · `Normal Missions`（GO `4469996525793424057`）HLG（MB `-8523192195473755463`）：
        //     `m_Spacing 0 · m_ChildAlignment 0(UpperLeft) · m_Padding 0` · `ctrlW 1` **`ctrlH 0`** ·
        //     `expandW 1 expandH 1` · `scaleW 1 scaleH 1` · `m_ReverseArrangement 0`
        //   · `Special Missions`（GO `8671611484050626303`）：
        //     **`ContentSizeFitter`：`m_HorizontalFit = 2 (PreferredSize)` · `m_VerticalFit = 0`**
        //     （MB `3054603845766420223`；⚠️ **值就是 2** —— `FitMode{0 Unconstrained, 1 MinSize, 2 PreferredSize}`，
        //      别照「1 = PreferredSize」写）＋ 自带 HLG（MB `5562888175203587839`）：`spacing 11.01 ·
        //      align 3(MiddleLeft) · pad 0 · ctrlW 1 ctrlH 1 · expandW 0 expandH 0 · scaleW 1 scaleH 1`
        //     ＋ `ContainerHolder.maxAmount = 4`（MB `-7417265825195092225`）
        //   · 子件的 `LayoutElement`：`Daily Skulls Mission Container`（GO `6434331890599441081`）
        //     = `minW 347.64 · prefW −1 · flexW 0`（`minH 555`）· `Daily Missions`（GO `8436681973535191737`）
        //     = **`flexW 120`**（min/pref 都 −1）· `Daily Login Container` = `minW 347.64 · prefW −1 · flexW 0`
        //   · 两个子件都 `localScale = 1.15`
        //   🔴 **2026-10-06 勘正两处**（**数与结论都不变，但成因/枚举号必须写对**，否则下一个人会照错的重算）：
        //      · 派活单把 CSF 写成 `m_HorizontalFit = 1 (PreferredSize)` —— **实测是 `2`**
        //        （`FitMode{0 Unconstrained, 1 MinSize, 2 PreferredSize}` ⇒ `1` 是 MinSize）。语义仍是 PreferredSize。
        //      · `资料/普查产出_1005/块13_工具四件.md` 件 3 写「骷髅卡的 `LayoutElement` 给出 `min = pref = 347.64`」
        //        与「`Special Missions` 的 flexible = 1」—— **实测 `prefW = −1`（⇒ uGUI 当 0 跳过）、`flexW = 0`**：
        //        347.64 只是 **`minW`**，SM 的 flexible 是**父组 `expandW = 1` 抬出来的 1**。
        //        （最终数一样 —— `totalPreferred = max(totalMin, totalPreferred)` 与 `GetChildSizes` 那两句兜住了。）
        //
        // **手算**（照 uGUI `HorizontalOrVerticalLayoutGroup.CalcAlongAxis:100-142` /
        // `SetChildrenAlongAxis:180-221` · `LayoutGroup.GetChildSizes` · `GetStartOffset` · `ContentSizeFitter:83-104`；
        // 容器宽 1519 · **按「`Special Missions` 下挂着两张卡」那一帧算** —— 就是我们这一页画的那一帧，见下面那张对照表）：
        //   · `Special Missions` 的首选宽 = **它自己那个 HLG 的总量**：可排子件 **2 张**（登录卡 + 骷髅卡）
        //     ⇒ `totalMin = 347.64×2 + 11.01 = 706.29`（两卡的 `minW` 都是 347.64；`n` 张只加 `n−1` 个间距）
        //       `totalPreferred = 0×2 + 11.01 = 11.01`（两卡的 `prefW` 都是 **−1** ⇒ uGUI 当 0 跳过）
        //       收尾一句 `totalPreferred = Mathf.Max(totalMin, totalPreferred)` 把它抬回 **706.29**
        //       ⇒ SM 的 `minWidth = preferredWidth = 706.29`（`totalFlexible = 0`：两卡的 `flexW` 都是 0）
        //   · 父组 `tot_min = tot_pref = 706.29 × 1.15 = 812.2335` · `tot_flex = (1 + 120) × 1.15 = 139.15`
        //     （SM 自己的 flexible 是 **0** —— 父组 `expandW = 1` 那条 `GetChildSizes` 把它抬到 1）
        //   · `surplus = 1519 − 812.2335 = 706.7665` ⇒ `itemFlexibleMultiplier = 706.7665 / 139.15 = 5.07917`、`minMaxLerp = 0`
        //   · `Special Missions` 那一格 = `706.29 + 1×5.07917 = 711.3692` —— **随后被它自己的 CSF 改回首选的 706.29**
        //   · `pos` 步进 = `711.3692 × 1.15 = 818.0746` ⇒ `Daily Missions` 的 x1 = `372.37 + 818.0746 = 1190.44`
        //   · `Daily Missions` 宽 = `0 + 120 × 5.07917 = 609.50`（它没有 CSF ⇒ 就是这个值）
        //   · 自证：`Daily Missions` 右边缘 = `1190.44 + 609.50 × 1.15 = 1891.37` = **`Normal Missions` 的右边缘**
        //     （布局刚好填满容器 —— 算错了这里就对不上）
        //   · **y 与高完全不变**：`ctrlH = 0` ⇒ 高来自各自的 `sizeDelta`，`align = 0(UpperLeft)` ⇒ 贴顶。
        //
        // ⛔ **为什么不挂真的 `HorizontalLayoutGroup` / `ContentSizeFitter` 组件**（2026-10-06 调度台裁定；与 A98 同一口径）：
        //   ① **`Shell` 全线没有 Canvas**（`Shell/ShellRuntime.cs:11-14`：正交相机 + 世界空间 mesh + TMP 世界空间文字）
        //      ⇒ 布局由 `CanvasUpdateRegistry`（`Canvas.willRenderCanvases`）驱动 ⇒ 挂上去的布局组件
        //      **永远不会被驱动**，是死数据；
        //   ② 更毒的是**将来一旦有 Canvas**：`ContentSizeFitter` 会按「无 rect 子件 ⇒ 首选宽 0」把这个节点的
        //      `sizeDelta.x` **悄悄改成 0**（静默失败，而且只在那一刻才现形）；
        //   ③ 全工程 **40+ 扇窗没有一处在运行时挂布局组件**，一律走手算（`Core/UguiRect.cs` 这套）—— **别开第二套**。
        //   ⇒ 所以这里落的是**手算好的数**；`Editor/RewardsScene.cs` 的 `CheckAt` 断的就是它（用的是**原版值字面量**，
        //     不是引用本文件的常量 —— 免得自证）。
        //
        // 🔴 **卡片数 → 参数（铁律 5·c：别只留一个数）** —— `Special Missions` 下**挂几张卡**，这一组数就变一次。
        //    本文件落的 = **【2 张】**那一列（我们这一页 `BuildLoginCard` + 骷髅卡**都建** ⇒ 画的就是这张）：
        //
        //    **四档全算完了**（`2026-10-06` 补上 3 / 4 张那一档 —— 与 1 / 2 张**同一条算式**，只把 `n` 代进去）：
        //
        //      挂几张卡                                   SM_Sz.x    DM_Pos.x     DM_Sz.x     Holder x1..x2         行左 / 行宽
        //      ───────────────────────────────────────────────────────────────────────────────────────────────────────────
        //      **2 张 = 登录卡 + 骷髅卡 ← 本文件用这一列**  **706.29**  **818.074**  **609.50**  **1190.44..1799.94**  **1190.44 / 609.50**
        //      1 张 = prefab 作者态（登录卡出厂 INACT）     347.64      409.0357     965.186     781.41..1746.59       781.41 / 965.186
        //      3 张                                         1064.94     1227.1134    253.814     1599.48..1853.30      1599.48 / 253.814
        //      4 张（= `maxAmount` 上限）⚠️ **放不下**       1423.59     1637.1285    **0**       **2009.50..2009.50**  **2009.50 / 0**
        //
        //    **四档的中间量**（照同一条算式，可独立复算；`n` = 卡数）：
        //      `SM_min = 347.64n + 11.01(n−1)`（两卡各 347.64 = 它们的 `minW`，`n` 张只加 `n−1` 个间距）
        //      `tot_min = tot_pref = SM_min × 1.15`（卡的 `prefW` 是 −1 ⇒ 首选=最小；`DM` 的 `minW` 也是 −1 ⇒ 0）
        //      `tot_flex = (1 + 120) × 1.15 = **139.15**`（**与 `n` 无关**：`SM` 那个 1 是父组 `expandW = 1` 抬的）
        //      `surplus = 1519 − tot_pref` ⇒ `mult = surplus / 139.15`；`minMaxLerp` 恒 0（`tot_min == tot_pref`）
        //      `SM_Sz.x = SM_min`（它自己的 CSF 把宽钉回**首选** 706.29/1064.94）·
        //      `DM_Pos.x = (SM_min + 1×mult) × 1.15`（括号里是 `SM` 被布局分的**格宽** —— 它只用来步进，
        //      CSF 随后把 `SM` 真宽改回首选项，于是两卡之间那条 `(格宽−首选项)×1.15` 的缝是**原版就有的**）·
        //      `DM_Sz.x = 120 × mult`
        //
        //      n    SM_min     tot_pref     surplus        mult          SM_Sz.x    DM_Pos.x      DM_Sz.x
        //      1    347.64     399.786      1119.214       8.043220      347.64     409.0357      965.186
        //      2    706.29     812.234      706.766        5.079170      706.29     818.074       609.500
        //      3    1064.94    1224.681     294.319        2.115120      1064.94    1227.1134     253.814
        //      4    1423.59    1637.1285    **−118.1285**  **0**         1423.59    1637.1285     **0**
        //    （n=1 / n=2 两行与**本文件已落地的常量**逐位对上 ⇒ 算式本身被这两档交叉验证过。精确值：
        //      `DM_Pos.x` = `409.035705` / `818.074585` / `1227.113403` · `DM_Sz.x` = `965.186340` /
        //      `609.500427` / `253.814423`（`float` 精度）；本表按 3~4 位取。）
        //
        //    🔴 **n=1 / 2 / 3 都「刚好填满容器」**：`DM` 右边缘 = `DM_Pos.x + DM_Sz.x × 1.15` **恒 = 1519**
        //      （= `Normal Missions` 的宽；容器在画布上 `372.37..1891.37`）—— `flexible` 把 `surplus` 分完，
        //      这个恒等式就必然成立。n=3 逐项：`DM_Pos.x = (1064.94 + 2.115120) × 1.15 = 1227.1134`、
        //      `DM_Sz.x = 120 × 2.115120 = 253.814` ⇒ `1227.1134 + 253.814 × 1.15 = 1519.00` ✔
        //    🔴 **n=4 是「放不下」那一档 —— 不是换行、也不是把卡缩窄**（照实记，⛔ 没硬凑一个数）：
        //      `tot_pref 1637.1285 > 容器 1519` ⇒ `surplus = −118.1285 ≤ 0` ⇒ uGUI 把
        //      `if (surplusSpace > 0) { … }` **整段跳过**（`itemFlexibleMultiplier` 保持 0、`pos` 保持 `padding.left`）⇒
        //      ① `SM` 拿它的**最小宽 1423.59**（画出来 `×1.15 = 1637.13` —— **比容器右边缘超出 118.13px**）；
        //      ② `DM` 拿 `0 + 120×0 = **0**` 宽、起点 `1637.13`（已在容器之外）⇒ **每日任务那一整块
        //         （卡头 + 三行）在原版那一帧里是 0 宽、根本画不出来**。
        //      ⇒ 这一档**本地判不了会不会真出现**（`ContainerHolder.TryAdd<object>` 是「逐条加、加满 `maxAmount`
        //        就返回 `false` 跳过」，挂几张由服务端任务数据定）⇒ 上表 **4 张那一行是「按同一条算式算出的
        //        预测」**，⛔ 不是实机观测。（若实机真挂 4 张，原版画面就是这么一副：右溢出 + 每日那列消失。）
        //
        //    ⚠️ **这张表的四条前提**（`2026-10-06` 逐条查过；哪条不成立，整张表都要重算）：
        //      ① **容器宽恒 1519**：`Normal Missions` 的 RT 是 `a=(0,1)` **非拉伸** + `sizeDelta.x = 1519`，
        //         节点上**只有** RT + HLG 两个组件（**没有** `ContentSizeFitter` / `LayoutElement` /
        //         `AspectRatioFitter`），且 `MissionsTab` 的全部方法里**没有一处**写 `sizeDelta`
        //         （反编译逐方法核过）⇒ 运行期没有谁改它；
        //      ② **每张卡的 `minW` 都是 347.64**：bundle 里**所有**带 `LayoutElement` 的任务卡 prefab
        //         （`Daily Login Container` · `Daily Skulls Mission Container`（3 个实例）· `… Small` ·
        //         `… Variant  Expansion Pass` · `Mission Container Vertical` · `Weekly Mission Container`）
        //         实测**一律** `minW 347.64 · prefW −1 · flexW 0 · minH 555` ⇒ 与「挂的是哪几张」无关；
        //      ③ **两个同名 `Normal Missions` GO 实例的 HLG 参数逐字段相同**（不存在「多实例不同值」那个坑）；
        //      ④ **卡最多 4 张**：`ContainerHolder.maxAmount = 4`（本件新核：`TryAdd` 在
        //         `maxAmount ≤ 已有数量` 时返回 `false`）。
        //
        //    🔴 **如果将来我们改画几张卡，这几个数（以及 `Editor/RewardsScene.cs` 里同一次布局的期望值）要一起改。**
        //    ⚠️ **「1 张」那一行是【作者态】，不是运行时真值**：`Special Missions` 挂 `ContainerHolder(maxAmount = 4)`，
        //      而 `ContainerHolder.Clear()` = `SupportMethods.DestroyAllChildren` + 逐条 `TryAdd` 重新实例化
        //      （反编译 `ContainerHolder__Clear.c:19` / `TryAdd<object>.c:28-32`）⇒ **实机挂几张卡由服务端任务数据决定**；
        //      「1 张」只是 **prefab 里存下来的那一帧**（`Daily Login Container` 出厂 `m_IsActive = 0`），
        //      「2 张」才是**我们这一页实际画的那一帧**（登录卡有意建着 —— 见 `资料/日常_画面逐项对_0923.md` §三·5）。
        //      两个数都不是「实机必然如此」；**3 / 4 张那一档现在按同一条算式算出来了**（见上表 ——
        //      ⚠️ 那是**算式预测**，不是实机观测：实机到底挂几张由服务端任务数据定，本地判不了）。
        //      🔴 **2026-10-06 更正（铁律 5）**：这一句原来写「3~4 张那种实机态**本地算不了**」——
        //      算得了（`n` 代进同一条算式即可，本件已落进上表），**算不了的只是「实机挂几张」**。
        // `Special Missions`  N(2, 0,1, 0,1, 0,1, 0,0, 706.29,556.223)   scl 1.15   ← x 是**布局位**（见上）
        static readonly Vector2 SM_A0 = UguiRect.A01, SM_A1 = UguiRect.A01, SM_P = UguiRect.P01,
                                SM_Pos = Vector2.zero, SM_Sz = new Vector2(706.29f, 556.223f);
        const float SM_Scale = 1.15f;
        // `Daily Login Container`  N(3, 0,1, 0,1, 0.5,0.5, 167,-282.5, 334,565)
        static readonly Vector2 DL_A0 = UguiRect.A01, DL_A1 = UguiRect.A01, DL_P = UguiRect.P50c,
                                DL_Pos = new Vector2(167f, -282.5f), DL_Sz = new Vector2(334f, 565f);
        // `Daily Missions`    N(2, 0,1, 0,1, 0,1, 818.074,0, 609.50,555.87)   scl 1.15  ← x/宽 **布局位**（见上）
        // 🔴 **这一改连带到每日任务那三行**：`Daily Missions Holder` 是 `a=(0,0)-(1,0.5) sz.x=0.0001` 的
        //    **拉伸子件** ⇒ 宽度跟着父件走（`Editor/RewardsScene.cs` 的 `RowRectHolderX1/X2` 已同步 1190.44/1799.94）。
        static readonly Vector2 DM_A0 = UguiRect.A01, DM_A1 = UguiRect.A01, DM_P = UguiRect.P01,
                                DM_Pos = new Vector2(818.074f, 0f), DM_Sz = new Vector2(609.50f, 555.87f);
        // ⚠️ 原版这个节点的 `localScale`（`资料/日常_原版规格.md:194` 实读：`p=(0,1) scl=(1.15,1.15)`）。
        // 🔴 **A124（2026-10-06）：它缩的是【整棵子树】** —— 与 `Special Missions` 走**同一条路**
        //    （`_s` / `_so` / `R()` / `NodeD` / `FS()`，⛔ 没有第二套）。**此前的注释写「声明了却一次没用过」
        //    是当时的实况**（`DM_Scale` 全仓只有这一处声明、零引用）⇒ 每日任务那一整块（卡头 + 三行 +
        //    行内文字/字号）画出来比原版**小 15%**。
        //   · **缩放中心 = 这个节点自己的 pivot `(0,1)` = 左上角 `(1190.44, 95.85)`**
        //     （`DM_P = UguiRect.P01` ⇒ `_so` 取的就是它，与 SM 那一处逐字同构）。
        //   · **两条承重真值**（设计值 × 1.15 = 屏幕上画出来的）：行宽 `609.50 → **700.93**` ·
        //     行高 `150 → **172.5**`。三行**靠下对齐** ⇒ 行 2 的底边 = `Daily Missions Holder` 的底边
        //     （缩放后 = `95.85 + 555.87 × 1.15 = **735.10**`）。
        //     自证：`Daily Missions` 右边缘 = `1190.44 + 609.50 × 1.15 = **1891.37**` = `Normal Missions` 的右边缘。
        //   · **字号也要乘**（`FS()`）：原版 `localScale` 缩的是**整棵子树**，TMP 的文字网格也在里头；
        //     而 prefab 里那些 `m_fontSize` 是**未缩放的原值** ⇒ 只缩框不缩字号 = 字比框小一圈
        //     （**静默** —— 自检量的是矩形，量不到这一档）。⚠️ **这条判据本仓已有先例**：
        //     `Shell/DailyStreakPopup.cs` 的 `BuildEntry`（「整组的 `scl=0.7` 已经烘进矩形里，
        //     但 TMP 的 `m_fontSize` 是**未缩放**的值 ⇒ 字号要自己乘 0.7」）。
        //   · ⚠️ **节点 marker 不进缩放**：`Daily Missions` 这一颗留在**布局矩形**上（与 `Special Missions`
        //     那个 marker 同款 —— 自检量的就是布局位）；它**以下**建出来的每一件（含行的容器节点）都过 `R()`。
        const float DM_Scale = 1.15f;
        // `Daily Missions Holder`  N(3, 0,0, 1,0.5, 0.5,0.5, 0,111.75, 0.0001,223.5)   VLG sp 18.55 align 7
        static readonly Vector2 DH_A0 = UguiRect.A00, DH_A1 = new Vector2(1f, 0.5f), DH_P = UguiRect.P50c,
                                DH_Pos = new Vector2(0f, 111.75f), DH_Sz = new Vector2(0.0001f, 223.5f);
        const float DH_Spacing = 18.55f, RowH = 150f;
        // `Weekly Mission Holder`  N(1, 0,1, 0,1, 0,0, 205.68,-918.1, 1518.99,227.51)   组件 = `ContainerHolder`
        static readonly Vector2 WM_A0 = UguiRect.A01, WM_A1 = UguiRect.A01, WM_P = UguiRect.P00,
                                WM_Pos = new Vector2(205.68f, -918.1f), WM_Sz = new Vector2(1518.99f, 227.51f);
        // `Weekly Mission`        N(2, 0,0, 1,1, .5,.5, 0,0, 0,-3.8147e-06)  组件 = `MissionContainer`
        //   🔴 **2026-10-05（块6）改正 —— 原版这一族是【两层】**，我们此前少了一层：
        //     真包 = `Missions Tab / Weekly Mission Holder / Weekly Mission`（父链 + 子件序实读：
        //     `bundle_menus_assets_all/RectTransform/RectTransform_341968686871156479.json` 的
        //     `m_Father` = `RT/RectTransform_6703300887156823807`（= `Missions Tab`）、
        //     `m_Children` = **[`RT/RectTransform_1711644035698464511`]（= `Weekly Mission`）**，
        //     这一项就是下面这组五元组）。
        //     `ContainerHolder.maxAmount = 1`（MB `2307977552517666559`）⇒ Holder 下**只挂一个** Container。
        //   ⚠️ 两层的**矩形完全相同** ⇒ 只量矩形的断言分不出「缺了 Holder」与「名字对不上」那两种状态
        //     （旧断言 `RewardsScene` 量的是矩形，所以一直是绿的）—— 现在另配了**按名字与父子关系**的断言。
        static readonly Vector2 WMC_A0 = UguiRect.A00, WMC_A1 = UguiRect.A11, WMC_P = UguiRect.P50c,
                                WMC_Pos = Vector2.zero, WMC_Sz = new Vector2(0f, -3.8147e-06f);

        /// <summary>`Daily Missions Holder` 的 `VerticalLayoutGroup`：**padTop 0 / spacing 18.55 / align 7 = LowerCenter**。
        /// 三行 150 高 + 2×18.55 = 487.1，容器 501.43 ⇒ 靠**下**对齐（正本 §三·1）。</summary>
        public static PxRect RowRect(PxRect holder, int index)
        {
            float top = holder.y2 - (RowH * 3f + DH_Spacing * 2f) + (RowH + DH_Spacing) * index;
            return new PxRect(holder.x1, top, holder.x2, top + RowH);
        }

        // ============================================================ 建

        public void SetHost(RewardsWindow win, Transform root) { _win = win; _root = root; }

        public override void Setup() { Build(); }

        public override void OnOpen() { }

        /// <summary>建整页。**自检与运行时同一条路**。</summary>
        public void Build()
        {
            if (_win == null || _root == null) return;
            for (int i = _root.childCount - 1; i >= 0; i--)
                RewardsWindow.DestroySafe(_root.GetChild(i).gameObject);

            var mt = UguiRect.Child(RootRect, MT_A0, MT_A1, MT_P, MT_Pos, MT_Sz);
            var nm = UguiRect.Child(mt, NM_A0, NM_A1, NM_P, NM_Pos, NM_Sz);
            var sm = UguiRect.Child(nm, SM_A0, SM_A1, SM_P, SM_Pos, SM_Sz);
            var dm = UguiRect.Child(nm, DM_A0, DM_A1, DM_P, DM_Pos, DM_Sz);
            var dh = UguiRect.Child(dm, DH_A0, DH_A1, DH_P, DH_Pos, DH_Sz);
            var wmh = UguiRect.Child(mt, WM_A0, WM_A1, WM_P, WM_Pos, WM_Sz);    // `Weekly Mission Holder`
            var wm = UguiRect.Child(wmh, WMC_A0, WMC_A1, WMC_P, WMC_Pos, WMC_Sz); // └ `Weekly Mission`
            // ⚠️ `Weekly Mission Holder` 的父是 **`Missions Tab`**，与 `Normal Missions` **同级**
            //    （真包实读：`RT/RectTransform_6703300887156823807.json` 的 `m_Children` 有序两项 =
            //     `527428140916741887`（`Normal Missions`）· `341968686871156479`（`Weekly Mission Holder`））。
            //    两层的矩形**完全重合**，但**必须建两个节点** —— 原版就是两层（见上面常量那一段）。

            // 容器节点（**有真实位置** —— 原版每个节点都有自己的 rect；自检要按它的位置量）
            var nmNode = RewardsWindow.Node(_root, "Normal Missions", nm);
            var smNode = RewardsWindow.Node(nmNode, "Special Missions", sm);
            var dmNode = RewardsWindow.Node(nmNode, "Daily Missions", dm);
            var dhNode = RewardsWindow.Node(dmNode, "Daily Missions Holder", dh);
            var wmhNode = RewardsWindow.Node(_root, "Weekly Mission Holder", wmh);
            var wmNode = RewardsWindow.Node(wmhNode, "Weekly Mission", wm);

            // 特殊任务区：`Special Missions` 的 `localScale = 1.15` **缩放的是整棵子树**（原版行为）
            // ⇒ 卡内每一件都要绕它的 **pivot = `(0,1)` 左上角**缩放一次。
            // 🔴 **2026-09-23 修**：原来只把**两张卡的矩形**过了 `ScaleAbout`，卡内一律用**未缩放**的 sizeDelta
            //    ⇒ 卡内每件都比原版小 15%（像素实测：骷髅卡 5 个里程碑格的**间距我们 60px、原版应是 69px**）。
            //    代码注释②当时写的就是「它子树的位置与尺寸都要绕它的 pivot 缩放」—— **意图对、实现漏了子树**。
            // **做法（本文件此后一律遵守）**：卡内按**设计空间**（未缩放）算矩形**与字号**，
            //    只在**建对象的那一刻**过 `R()` / `FS()` 换成最终值（`Draw` / `Txt` / `Txt1` / `NodeD` / `Node`+`R`）。
            _s = SM_Scale; _so = new Vector2(sm.x1, sm.y1);
            BuildLoginCard(smNode, UguiRect.Child(sm, DL_A0, DL_A1, DL_P, DL_Pos, DL_Sz));

            // `Daily Skulls Mission Container`（页内那份）尺寸是 0×0（靠 `FlexibleLayoutSizeOption` 运行时定）
            // ⇒ 用**独立预制体 `Daily Skulls Mission Container Small`** 的实尺 336×277.5（正本 §三·4）。
            BuildSkullsCard(smNode, UguiLayout.HorizontalChild(sm, 336f, 277.5f, 1, 0f, 11.01f));   // HLG sp **11.01**
            _s = 1f; _so = Vector2.zero;

            // 🔴 **每日任务区：`Daily Missions` 的 `localScale = 1.15` 同样缩的是整棵子树**
            //    （**A124（2026-10-06）**；此前只有 SM 那半边做了 —— 见上面 `DM_Scale` 那一段的判据/真值）。
            //    与 SM 那段**同一条路**：设计空间算矩形 ⇒ 建对象那一刻过 `R()` / `NodeD()` / `FS()`。
            _s = DM_Scale; _so = new Vector2(dm.x1, dm.y1);          // 绕它自己的 pivot `(0,1)` = 左上角
            BuildMissionHeader(dmNode, dm, false);                   // 卡头（`Mission Header` 那一条）
            for (int i = 0; i < 3; i++) BuildDailyRow(dhNode, RowRect(dh, i), i);
            _s = 1f; _so = Vector2.zero;

            BuildWeekly(wmNode, ScaleAbout(wm, wm.x1, wm.y1, 1f));   // 周常没有额外缩放（1.0 是显式的，便于以后改）

            // ⚠️ `Daily Missions Holder` 里的行**是布局组排的**，上面按 LowerCenter + spacing 18.55 算；
            //    这一条进了自检（`RewardsScene`），别只靠肉眼。
        }

        /// <summary>绕 (px,py) 把矩形缩放 s 倍（原版 `localScale` 的几何效果）。</summary>
        public static PxRect ScaleAbout(PxRect r, float px, float py, float s)
            => new PxRect(px + (r.x1 - px) * s, py + (r.y1 - py) * s,
                          px + (r.x2 - px) * s, py + (r.y2 - py) * s);

        // ============================================================ 子树缩放（原版 `Special Missions` / `Daily Missions` 的 localScale）
        //
        // 约定：**子树里一律按「设计空间」（未缩放）算矩形与字号**，只在建对象的那一刻过 `R()` / `FS()`。
        // 理由：原版 `localScale` 缩放的是**整棵子树**，位置与尺寸**都要**乘 —— 只缩放外框会得到
        // 「框对了、里面的东西小一圈且偏位」这种**看着像对的**错（2026-09-23 实测就是它）。

        float _s = 1f;
        Vector2 _so;

        /// <summary>设计空间矩形 → **最终（已缩放）**矩形。</summary>
        public PxRect R(PxRect r) { return _s == 1f ? r : ScaleAbout(r, _so.x, _so.y, _s); }

        /// <summary>设计空间**字号** → **最终**字号。🔴 与 <see cref="R"/> **成对**：原版 `localScale` 缩的是
        /// **整棵子树**，TMP 的文字网格也在里头；而 prefab 里那些 `m_fontSize` 是**未缩放的原值**
        /// ⇒ 只缩框不缩字号 = 「框对了、字小一圈」，而且**静默**（自检量的是矩形，量不到字号）。
        /// ⚠️ **判据是仓里已有的先例**（不是这里新发明的口径）：`Shell/DailyStreakPopup.cs` 的 `BuildEntry`
        /// ——「整组的 `scl=0.7` 已经烘进矩形里，但 TMP 的 `m_fontSize` 是**未缩放**的值 ⇒ 字号要自己乘 0.7」。
        /// ⚠️ **自适应上下限（`autoMinPx`）也要一起乘**：`MenuDraw.TextBox` 拿 `(autoMinPx, fontPx)` 当
        /// 自适应的上下限 —— 只乘上限会把区间压窄、自适应结果与原版不同。</summary>
        float FS(float fontPx) { return fontPx * _s; }

        /// <summary>画一件（`r` 是**设计空间**矩形）。</summary>
        ImageQuad Draw(Transform parent, string art, PxRect r, string name, int q,
                       Color? tint = null, bool keepAspect = false)
        {
            var f = R(r);
            return _win.Rect(parent, art, f.x1, f.x2, f.y1, f.y2, name, q, tint, keepAspect);
        }

        /// <summary>画一段字（`r` 是**设计空间**矩形，`fontPx` 是**设计空间**字号 —— 两者一起过 `R()`/`FS()`）。
        /// 🔴 **A143（2026-10-06）加了 `autoMaxPx`**：`MenuDraw.TextBox` 把自适应**上界**写死成 `fontPx`，
        /// 而原版 prefab 里 `m_fontSizeMax` **不一定等于** `m_fontSize`（实读：骷髅卡时钟行 `38` vs `30.15`）
        /// ⇒ 传了 `autoMaxPx` 就按原版那两个字段重设一次窗口（见 <see cref="FitWindow"/>）。</summary>
        Label Txt(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                  float autoMinPx = 0f, float autoMaxPx = 0f)
        {
            var f = R(r);
            var lb = _win.TextBox(parent, f, text, color, name, FS(fontPx), FS(autoMinPx));
            if (lb != null && autoMaxPx > 0f) FitWindow(lb, r, autoMinPx, autoMaxPx);
            return lb;
        }

        /// <summary>🔴 **A143（2026-10-06）：按原版 `m_fontSizeMin` / `m_fontSizeMax` 重设自适应窗口。**
        /// 用在「`m_fontSizeMax` ≠ 设计字号」的件上（`Txt` / `MenuDraw.TextBox` 的上界写死成 `fontPx`）。
        /// <para>`rect` 传**设计空间**矩形（内部过 `R()`）、`minPx`/`maxPx` 传**设计空间**字号（内部过 `FS()`）
        /// ⇒ **调用点写的字面量就是原版字段的原值**（⛔ 不预先乘 1.15；`FS()` 会乘）。</para>
        /// <para>🔴 **量纲（2026-10-06 踩过一次，写死在这里）**：`Label.SetAutoFitBox(worldW, worldH, minPx, maxPx)`
        /// 的 **`minPx/maxPx` 是【画布 px】**（`Battle/Label.cs:345-347` 写着；`MenuDraw.Text` 也是把
        /// `autoMinPx`/`fontPx` **原样**传进去的）—— ⛔ **不能再过 `LayoutSpace.Px()`**（那只给 `worldW/worldH` 用）。
        /// 踩的那次就是多除了一个 108 ⇒ 窗口变成 `[0.128, 0.469]` px，TMP 会把字压到**亚像素**、整条标签看不见
        /// （`RewardsScene` 的 SM 窗口断言当场抓住 —— 那 4 条红是**实现 bug**，不是期望值算错）。</para>
        /// <para>⚠️ `Label.SetAutoFitBox` **只改 `fontSizeMin/Max`** —— 标称字号是 `SetGlyphHeight` 定死的，
        /// 不会被它改掉（`TmpFontSize()` 明确「不含自适应结果」）⇒ 对已经建好的 label 补调一次是安全的。</para>
        /// <para>⚠️ **但它内部会连锁调 `SetWrapWidth(worldW)`**（`Label.cs:339`）⇒ 折行模式变 `Normal`、
        /// `sizeDelta.x` 被改写。现在两个调用点都走 `Txt`→`TextBox`（本来就已折行）⇒ **无差别**；
        /// 将来若给 `Txt1`（原版 NoWrap 的件）用，得先想清楚这一条 —— 原版 `counter text` 是 `m_TextWrappingMode = 0`。</para></summary>
        void FitWindow(Label lb, PxRect designRect, float minPx, float maxPx)
        {
            if (lb == null) return;
            var f = R(designRect);
            // ⚠️ `worldW/worldH` 过 `LayoutSpace.Px()`（世界单位）；`minPx/maxPx` **不过**（它俩本来就是 px）
            lb.SetAutoFitBox(LayoutSpace.Px(f.W), LayoutSpace.Px(f.H), FS(minPx), FS(maxPx));
        }

        /// <summary>建一个有矩形语义的容器节点（`r` 是**设计空间**矩形）。</summary>
        Transform NodeD(Transform parent, string name, PxRect r)
        {
            return RewardsWindow.Node(parent, name, R(r));
        }

        /// <summary>画一段**不换行**的字（原版 `m_TextWrappingMode = 0` 的那些：按钮文案、计数…）。
        /// `Txt` 走的是 `TextBox`（**限宽换行**），窄框里会把 `13/15` 拆成两行（2026-09-23 踩到）。
        /// ⚠️ `fontPx` 同 `Txt`：**设计空间**字号，过 `FS()`。</summary>
        Label Txt1(Transform parent, PxRect r, string text, Color color, string name, float fontPx)
        {
            var f = R(r);
            return _win.Text(parent, text, f.x1, f.x2, f.y1, f.y2, 4, color, name, FS(fontPx));
        }

        /// <summary>把一段字**左对齐**到设计空间矩形 `r` 的左边缘。
        /// 出处：这批 TMP 的 `m_HorizontalAlignment` 实测 **`H=1 (Left)`**
        /// （`Mission Header` 与三张卡的 `name` · 每日行的 `description`/`timer`/`progress` ·
        /// 周常的 `counter`）。⚠️ 卡片上的 `Timer` 例外，它是 `H=2 (Center)`。</summary>
        void AlignL(Label lb, PxRect r)
        {
            if (lb != null) lb.AlignLeftOn(LayoutSpace.FromPixel(R(r).x1, 0f).x);
        }

        /// <summary>把一段字**右对齐**到设计空间矩形 `r` 的右边缘（原版 `timer` 那一行的用法）。</summary>
        void AlignR(Label lb, PxRect r)
        {
            if (lb != null) lb.AlignRightOn(LayoutSpace.FromPixel(R(r).x2, 0f).x);
        }

        // ============================================================ 三种卡
        //
        // 卡内五元组出处：`工具/menu_rect.py <pid> --cs --root-size <显示尺寸>`。

        /// <summary>`Daily Mission Container`（每日任务行）· 作者尺寸 **787.973×150**，被布局撑到**容器宽**。
        /// 内部锚点按父宽重分布（实测：539.19 宽时 `description` 是 395.64 宽）。</summary>
        public void BuildDailyRow(Transform parent, PxRect row, int index)
        {
            // 卡自己建一个**有矩形语义的节点**，行内所有件挂在它下面（原版每个节点都有自己的 rect；
            // 挂在页级父节点上的话，`FindChild(行, "description")` 找不到 —— 自检当场报出来过）。
            // 🔴 **A124**：这一层也过 `R()`（`NodeD`）—— 它是 `Daily Missions` 的**后代**，在缩放里
            //    （原版 `localScale` 缩的是整棵子树）。自检量这一颗的量的是**缩放后**的矩形。
            parent = NodeD(parent, "Daily Mission Container (" + index + ")", row);
            // 整卡底：`40K_missions_display_Daily horizontal` ·
            // `MissionBackgroundHighlighter.normalColor` = (0.4941,0.5686,0.9176,1)（**可领取**时换 (1,0.6667,0.3451,1)）
            // ⚠️ 走 `Draw`（**过 `R()`**）—— 别再写成 `_win.Rect(parent, …, row.x1, …)`：
            //    那样绕开缩放，行底图会比原版小 15%、而**行内其它件却是缩放的**（版面自相矛盾，且静默）。
            Draw(parent, "40K_missions_display_Daily_horizontal", row, "Background", RewardsWindow.QPanel,
                 DailyData.RowTint(index));

            // 🔴 **`description` 与 `timer` 是互斥的**（2026-09-23 取证 · 2026-10-04 定性）：
            //    原版 `MissionInfoDisplay.DisplayRule` 是 `[Flags]` 枚举
            //    **`WhenActive = 1` · `WhenComplete = 2`**（类桩 `Assembly-CSharp/MissionInfoDisplay.cs:6-13`），
            //    实现在 `DF:MissionInfoDisplay__Initialize.c:10-27`：
            //      `show = (IsComplete() && WhenComplete) || (!IsComplete() && WhenActive)`
            //    （亲读指令流：`(rule >> 1) & IsComplete` 与 `(rule & 1) && !IsComplete` 两条或起来
            //      → `SetActive(show)`，**`if (show)` 才调虚方法 `Setup`**）。
            //    本行的 `description` 是 **1**、`timer` 是 **2** ⇒ **永远不会同时出现**；
            //    这两条 TMP 的矩形本来就是**重叠**的（`description` 136.67..532.31 · `timer` 136.68..408.77，
            //    两者都是 `H=Left`）—— 原版靠这条规则保证不打架，**我们原来两条都画 ⇒ 文字叠成一团**。
            //    🔴 **2026-10-04（A36-①）把 `IsComplete()` 的语义坐实了 =「奖励已领取/已结算」，不是「进度到顶」**
            //      （两条独立证据链 → `DailyData.DailyClaimed` 的注释）⇒ 这一行该显示哪一条：
            //        · **未领取 ⇒ `description`**（`description` 是 `WhenActive`）
            //        · **已领取 ⇒ `timer`**（`timer` 是 `WhenComplete`）
            //      ⚠️ 原来取的是「进度到顶」，**方向反了** ——「10/10 未领取」那一态原版显示的是**说明文字**，
            //      不是倒计时（`progress` 那个 `MissionCounterDisplay` 此时正显示 `Missions/Completed` ——
            //      ✅ **那一件就是 A63，2026-10-04 已经做了**，判据 + 实现在 `DailyData.DailyCounterText`
            //      与下面 `progress` 那一行；**别把 `Missions/Completed` 读成「还没做」**）。
            bool claimed = DailyData.DailyClaimed(index);

            // 🆕 **2026-10-04（A44 甲）：本行**五个** `MissionInfoDisplay` 系的件全靠这一条 `claimed` 决定画不画** ——
            //    `description`（`MissionInfoDisplay` dr1）· `Rewards`（`MissionRewardsDisplay` dr1）·
            //    `Mission Milestones Progress Bar/Progress Bar`（`MissionProgressBarDisplay` dr1）·
            //    `progress`（`MissionCounterDisplay` dr1）· `Generic UI Button` = `Collect`（`MissionInfoDisplay` dr1）·
            //    `Trash mission`（`MissionReRollButton` dr1）
            //    🔴 **「五个」是【按件】数，不是按节点数**（A44 ③ 要求带口径 —— 铁律 5·c）：上面**六个名字**是按
            //      **节点**列的；按「件」数只有**五**，因为 `Progress Bar` 与 `progress` 是**同一个容器**
            //      `Mission Milestones Progress Bar`（该容器**自己没有脚本**）下的两个子节点，**算一件**。
            //      ⇒ **五件 = 六个节点**：`description` / `Rewards` / 〔`Progress Bar` + `progress`〕 / `Collect` / `Trash mission`。
            //      ⚠️ 引用这条口径时（`项目任务.md` 的 A44 行、`RewardsScene` 的断言文案…）**要连口径一起写**，
            //      只写「五个件」会让人按节点数出 6 个、以为少了一件。
            //    ⇒ **未领取全画、已领取全藏**；`timer` 是这一行**唯一** dr=2 的件（**已领取才画**）。
            //    判据三份、互相独立：
            //      ① 反编译 `DF:MissionInfoDisplay__Initialize.c`：`show = ((dr>>1) & IsComplete) || ((dr & 1) && !IsComplete)`
            //         → `SetActive(show)`（**`if (show)` 才调虚方法 `Setup`**）—— 四个类都 `: MissionInfoDisplay`
            //         （类桩 `Assembly-CSharp/MissionRewardsDisplay.cs:3` / `MissionProgressBarDisplay.cs:4` /
            //          `MissionCounterDisplay.cs:8` 各自 `protected override void Setup`）。
            //      ② **真包 MB 实读**（`assets_full/bundle_menus_assets_all/MonoBehaviour/`，按 `m_Father` 父链认行；
            //         下表 = 独立预制体 `Daily Mission Container` 那一份，另两份实例（`Missions Tab/…` 与
            //         `Rewards Base Submenu Variant/…`）逐件同值）：
            //           `Daily Mission Container/Rewards`                                  MB 5084072559339706576 **dr=1**
            //           `Daily Mission Container/Mission Milestones Progress Bar/Progress Bar` MB -204080914756200240 **dr=1**
            //           `Daily Mission Container/Mission Milestones Progress Bar/progress`     MB 3476392019656054992 **dr=1**
            //           `Daily Mission Container/Generic UI Button`                        MB -2345300010676315952 **dr=1**
            //           （`description` MB -4812140206590256944 与 `Trash mission` MB 5779676786542647504 也各 dr=1；
            //             `timer` MB 4188647697997699280 是这一行唯一的 **dr=2**）
            //      ③ **`IsComplete()` = 「奖励已领取」**（不是「进度到顶」）—— 两条独立证据链见 `DailyData.DailyClaimed` 的注释。
            //    ⇒ 实现 = 用上面那个 `claimed` 把这**五件（= 6 个节点 —— 口径见本段开头）**包起来；
            //      **别用「进度到顶」**（1.0 版方向反了，见 `DailyData.cs:98`）。
            //    ⚠️ 藏 = 原版的 `SetActive(false)`（静默），不是我们偷懒 —— 所以不出声。

            // `description`  N(1, 0,1, 0.986689,1, .5,.5, 68.4878,-43.873, -136.374,62.253)
            var desc = UguiRect.Child(row, new Vector2(0f, 1f), new Vector2(0.986689f, 1f), UguiRect.P50c,
                                      new Vector2(68.4878f, -43.873f), new Vector2(-136.374f, 62.253f));
            // 原版那条 TMP 实测：`m_fontSize 35 · m_TextWrappingMode 1 · m_enableAutoSizing 1 · min 15` · `H=Left`
            // 显示条件 = **`!IsComplete`**（`displayRule = 1 (WhenActive)`）⇒ **未领取**才画
            if (!claimed)
                AlignL(Txt(parent, desc, DailyData.DailyDesc(index), Color.white, "description", 35f, 15f), desc);

            // `timer`  N(1, 0,1, 1,1, .5,.5, 3.13226,-43.873, -267.097,62.253)   白 α0.59 · `H=Left`
            var tim = UguiRect.Child(row, new Vector2(0f, 1f), new Vector2(1f, 1f), UguiRect.P50c,
                                     new Vector2(3.13226f, -43.873f), new Vector2(-267.097f, 62.253f));
            // 显示条件 = **`IsComplete`**（`displayRule = 2 (WhenComplete)`）⇒ **已领取**才画
            // ⚠️ 本行 MB 是 `MissionTimerDisplay`（`MissionInfoDisplay` 的派生类），`dr = 2` 是**独立复读**到的
            //    （MB `9196887547440731903`）—— 别拿 `description` 那条 `dr=1` 当通例（同一个类在不同行 dr 可以不同）。
            if (claimed)
                AlignL(Txt(parent, tim, DailyData.DailyTimer(index), new Color(1f, 1f, 1f, 0.59f), "timer", 35f, 15f), tim);

            // `Separator Line`  N(1, 0,0, 0,1, 1,0.5, 122.062,-0.0370026, 1.60199,-3.049)  无 sprite，只有色
            var sep = UguiRect.Child(row, UguiRect.A00, new Vector2(0f, 1f), new Vector2(1f, 0.5f),
                                     new Vector2(122.062f, -0.0370026f), new Vector2(1.60199f, -3.049f));
            // ⚠️ 同 `Background`：**过 `R()`**（`Draw`），不直调 `_win.Rect`。
            Draw(parent, null, sep, "Separator Line", RewardsWindow.QContent,
                 new Color(0.25f, 0.25f, 0.41f, 0.59f));

            // `Rewards`  N(1, 0,0, 0,0, 0,0, 3.05e-05,0, 126.334,150)  → `Reward Display Mission Vertical Variant`
            var rew = UguiRect.Child(row, UguiRect.A00, UguiRect.A00, UguiRect.P00,
                                     new Vector2(3.05176e-05f, 0f), new Vector2(126.334f, 150f));
            // 显示条件 = **`!IsComplete`**（`MissionRewardsDisplay : MissionInfoDisplay`、实例 `displayRule = 1`）⇒ 未领取才画
            // 🔴 **这一格画的是「这条任务自己」的奖励**（🆕 2026-10-05 **B4**）—— 与 `CollectDaily` 发的那一份**同源**：
            //    → `DailyData.DailyRewardArt(index)` / `DailyRewardText(index)`。
            //    ⛔ 别再换回「按下标查表」：那张表在第 3 行画「骷髅 ×150」、而实发的是这条任务的**金块 ×200**
            //    （图标 / 数量 / 发放三者不一致），而且**重摇换了任务之后格子里那个数不会跟着变**。
            if (!claimed)
                BuildRewardCell(parent, rew, DailyData.DailyRewardArt(index), DailyData.DailyRewardText(index),
                                index.ToString());

            // `Mission Milestones Progress Bar`  N(1, 0,0.5, 0.316,0.5, .5,.5, 98.02,-29.026, -77.3111,51.8301)
            var mmpb = UguiRect.Child(row, new Vector2(0f, 0.5f), new Vector2(0.316f, 0.5f), UguiRect.P50c,
                                      new Vector2(98.02f, -29.026f), new Vector2(-77.3111f, 51.8301f));
            // 🔴 里面**两件都吃 `displayRule = 1 (WhenActive)`**（`MissionProgressBarDisplay` / `MissionCounterDisplay`，
            //    实例实读各 dr=1）⇒ 与 `description` 同一条规则：**未领取才画**。⚠️ 外面那个
            //    `Mission Milestones Progress Bar` 节点**自己没有脚本**（纯容器），所以只需管这两个子的。
            if (!claimed)
            {
                //   └ `Progress Bar`  N(2, 0,0, 1,0.33, 0,1, 5.5,-5.5, -5.5,-5.5)  bg/fill 都是 `40k_generial_bar_*` 九宫格
                var pb = UguiRect.Child(mmpb, UguiRect.A00, new Vector2(1f, 0.33f), new Vector2(0f, 1f),
                                        new Vector2(5.5f, -5.5f), new Vector2(-5.5f, -5.5f));
                // ⚠️ **A124**：节点过 `R()`（`NodeD`）—— `BuildBar` 内部的 `f = R(r)` 用的是**同一个**矩形，
                //    两边必须一致（节点留在设计空间的话，自检量到的节点位置与画出来的条会差 15%）。
                BuildBar(NodeD(parent, "Progress Bar", pb), pb, DailyData.DailyProgress01(index));
                //   └ `progress`  N(2, 0,0.33, 1,1, 0,0, 0,-3, 0,6)  文本 `52/500` fs35 色 (1,0.77,0.33,1)
                var pt = UguiRect.Child(mmpb, new Vector2(0f, 0.33f), UguiRect.A11, UguiRect.P00,
                                        new Vector2(0f, -3f), new Vector2(0f, 6f));
                // 🆕 **A63（2026-10-04）：到顶换文案** —— 原来这一行恒写 `DailyCounter`（`52/500`），
                //   而原版 `MissionCounterDisplay__Setup.c:24-26,68-79` 在 **`currentValue >= MaxValue`**
                //   时改显示 `completedMessage`（出厂 `"Missions/Completed"`、`displayCompletedMessage=1`；MB 实读）。
                //   🔴 **判据只此一处**：`DailyData.DailyCounterText` —— 它同时管着「那个串是原版的 I2 词条【键】、
                //   本地没有语言表 ⇒ 照抄键本身 + 出声」那条口径；本行只负责画。
                AlignL(Txt1(parent, pt, DailyData.DailyCounterText(index), new Color(1f, 0.77f, 0.33f, 1f), "progress", 35f), pt);
            }

            // `Generic UI Button`  N(1, 1,0, 1,0, .5,.5, -145.3,40.7107, 254.611,56.4767)   `40K_button` 色 (1,0.53,0,1) type=1
            var btn = UguiRect.Child(row, UguiRect.A10, UguiRect.A10, UguiRect.P50c,
                                     new Vector2(-145.3f, 40.7107f), new Vector2(254.611f, 56.4767f));
            // 显示条件 = **`!IsComplete`**（`MissionInfoDisplay`、实例 `displayRule = 1`）⇒ **已领取才藏 `Collect`**。
            // ✅ 于是「藏了 Collect ⇒ 玩家领不了奖」**不成立**：领不到奖那一态（`Collectable`）它是**在**的
            //    （`CollectDaily` 只在 `St == Collectable` 时才真的发奖 —— 见 `DailyData.CollectDaily`）。
            if (!claimed)
                BuildButton(parent, btn, "40K_button", new Color(1f, 0.53f, 0f, 1f), "Collect", 35f, "Generic UI Button",
                            () => CollectThenRebuild(index), DailyData.CanCollectDaily(index));

            // `Trash mission`  N(1, 1,0, 1,0, .5,.5, -307.44,40.711, 49.104,49.368)   `40k_general_bt_yellow` 色 (1,0.77,0.33,1)
            var trash = UguiRect.Child(row, UguiRect.A10, UguiRect.A10, UguiRect.P50c,
                                       new Vector2(-307.44f, 40.711f), new Vector2(49.104f, 49.368f));

            // 🆕 2026-10-04（A23）：这颗垃圾桶**不是「删除任务」，是【重摇任务】** —— 原来这里只画了图、
            //   **没有命中区** ⇒ 玩家点了没反应（缺口记在 `项目任务.md` §三 第 29 条 A23）。
            //   判据：原版那个节点挂的是 `MissionReRollButton`（字段 `button` / `displayRule` / `reRollPopup`），
            //   点它的链 = `WindowsManager.OpenWindow(<MissionReRollPopup>, ctx)`（反编译：
            //   `MissionReRollButton.__c__DisplayClass3_0___Setup_b__0.c`；见 `资料/待办判据_阶段二与联机.md` §A23 一）。
            //   🔴 **别照着图标猜语义**（「垃圾桶」在本工程也有过别的含义）。
            //   🔴 悬停换图：原版那颗 `EverguildButton` 是 `trans=2 (SpriteSwap)`，`m_SpriteState` 实读
            //   `HL = 40k_general_bt_yellow_hover` · `P = 40k_general_bt_yellow_pressed`
            //   （`python 工具/menu_dump.py bundle_menus_assets_all "Missions Tab" --depth 6`，pid 回真包反查）
            //   ⇒ 两个图名**逐颗显式传**（不靠 `<常态图>_hover` 那条后备规律 —— 虽然这次恰好同值）。
            //
            //   🔴🔴 **显隐（2026-10-04 A36-① 修两轮：先解出「按状态」，再订正「是哪个状态」）**：
            //     完整规则 = **`(!IsComplete) && (Price.amount > 0)`**，两半的判据都在本地：
            //       · **前半（`!IsComplete` 才显示）**：`MissionReRollButton : MissionInfoDisplay`（类桩
            //         `Assembly-CSharp/MissionReRollButton.cs:4`），而基类 `displayRule` 是
            //         `[Flags]{ WhenActive = 1, WhenComplete = 2 }`、**全包 8 个实例实测全是 1 (WhenActive)**；
            //         基类 `DF:MissionInfoDisplay__Initialize.c` 的 `show = (WhenComplete && IsComplete)
            //         || (WhenActive && !IsComplete)`（亲读指令流：`(b >> 1) & IsComplete` 与
            //         `(b & 1) && !IsComplete` 两条或起来 → `SetActive(show)`，**`if (show)` 才调 `Setup`**）
            //         ⇒ **原版把「已领取」那一行的垃圾桶藏起来**。**我们照这条做**：用上面那个 `claimed`。
            //         🔴 **注意是「已领取」不是「进度到顶」** —— 1.0 版取的是后者（方向反了，等于删掉原版一个能力：
            //         「10/10 未领取」那一行原版**是给重摇的**）。判据见 `DailyData.DailyClaimed` 的注释。
            //       · **后半（`Price.amount > 0`）**：`MissionReRollButton.Setup` 末段那个整数二次查实 = `Price.amount`
            //         （`Price` 桩 = `currency` + `amount`）—— **数值本身本地读不到**（服务端下发 +
            //         `MissionData.get_RerollPrice` 是错桩）⇒ **这一半如实恒真**（不假装它判过）。
            //     判据正本 = `资料/待办判据_阶段二与联机.md` §A23 三·1；自检 = `Editor/RewardsScene.cs` 的
            //     「已领取那行的垃圾桶不建 / 点它开不开得出重摇窗 / 退回未领取那行在」三条。
            //     🔴 2026-10-04 顺手查出：这一行**还有四个件也吃同一条 displayRule**（`Rewards` = `MissionRewardsDisplay`、
            //        `Progress Bar` = `MissionProgressBarDisplay`、`progress` = `MissionCounterDisplay`、
            //        `Generic UI Button`（Collect）= `MissionInfoDisplay`，**四个实测 displayRule 全是 1**）——
            //        即原版在「已领取」那一行会把它们**一起藏起来**。
            //     ✅ **2026-10-04（A44 甲）已经照着补齐**（`if (!claimed)` 各包一处，判据见本方法开头那段）；
            //        ⚠️ 原来那句「已报调度台、别在这里顺手改」说的正是这件事 —— 现在它做完了，别再读成「还没做」。
            //   ⚠️ 隐藏是**原版行为**（静默 `SetActive(false)`），不是我们在偷懒 —— 所以这里不出声。
            //   🔴 **用 `if (!claimed)` 包住而不是提前 `return`** —— 这一行以后要再加件时，早退会**静默漏掉**。
            if (!claimed)
            {
                // ⚠️ 原版这一件是 `Simple + preserveAspect`（源图 71×71 塞进 49.104×49.368 的框）⇒ 画出来是 **49.104²**
                var trashQ = Draw(parent, "40k_general_bt_yellow", trash, "Trash mission", RewardsWindow.QContent,
                                  new Color(1f, 0.77f, 0.33f, 1f), true);
                //   └ `Image` = `40k_general_bt_yellow_delete`（`Button Text` 'X' 出厂 inactive ⇒ 不建）
                var ti = UguiRect.Child(trash, UguiRect.A00, UguiRect.A11, UguiRect.P50c,
                                        new Vector2(-1f, 0f), new Vector2(-2f, -2f));
                Draw(parent, "40k_general_bt_yellow_delete", ti, "Image", RewardsWindow.QOverlay, null, true);
                //   命中区的队列放 **`QOverlay`**（行内最高一档）⇒ 不会被同行任何件抢走
                //   （`BoosterInfoPopup.QShadeHit` 那条：同队列时 `ImageQuad` 的 z 恒为 0，谁吃到命中不可控）。
                int ri = index;
                var trashHit = _win.AddHit(parent, "Hit", R(trash), RewardsWindow.QOverlay,
                                           () => OpenReroll(ri), trashQ, "40k_general_bt_yellow",
                                           "40k_general_bt_yellow_hover", "40k_general_bt_yellow_pressed");
                if (trashHit == null)
                    Debug.LogWarning("[Missions] 第 " + (index + 1) + " 行垃圾桶的**命中区没建出来**（`AddHit` 返回 null）"
                                     + " —— 玩家会点不动它（红线：不许静默失败）");
            }
        }

        /// <summary>🆕 **A64（2026-10-04）**：点每日任务那一行的 `Collect` ⇒ 领奖，**领到了就把整页重建一次**。
        /// <para>**原版那条链（六环，逐环都有本地判据 → `资料/普查产出_1004/X2审查_A44甲.md` §一·附）**：
        /// `MissionContainer.SetChallenge` 给 `collectButton` 挂监听（`RemoveAllListeners` + `AddListener`）→
        /// 回调 = `Missions.CollectChallenge(mission, challenge, onComplete)` → 成功之后调 `onComplete`
        /// = **`MissionContainer.OnCollect`** → `WindowsManager.GetOpenWindow()` 非空 ⇒
        /// `GameWindowWithTabs.ChangeTab&lt;MissionsTab&gt;()` → `ChangeTabCO` → `WindowTabBase.TryOpenTab`（slot 7）→
        /// `OnOpen()` → **`CreateMissions()`**（三个 `ContainerHolder.Clear()` + 逐条重新实例化）= **整页重建**。</para>
        /// ⚠️ 换到的页签**正好是当前这一页**时，`ChangeTabCO` 走的是「不 `CloseTab`、但**照样调 `TryOpenTab`**」
        /// 那一支（`.c` 开头：`op_Equality(old, new)` 为真且 old 非空 ⇒ 直接调 slot 7）⇒ **同一页也会重建**。
        /// ⇒ 我们这边等价于**再 `Build()` 一次**（`Build` 既是自检入口也是运行时入口，同一份实现）。
        /// <para>🔴 **只在真的领到时重建**：没达成时原版那颗钮**根本点不动**
        /// （`MissionContainer__SetChallenge.c`：`Selectable.set_interactable(collectButton, CanCollect(challenge))`）
        /// ⇒ 不能无条件重建（`DailyData.CollectDaily` 因此返回「领没领到」）。</para>
        /// 🔴 **重建会销毁旧的整棵子树** ⇒ 重建之后任何**跨重建持有**的节点/列表句柄都作废
        /// （本仓踩过：自检里的 `rows` 列表必须重收 —— 见 `Editor/RewardsScene.cs` 那一段的注释）。
        /// <para>✅ **2026-10-05（A75①）**：另外三张卡（登录 / 骷髅 / 周常）的 `Collect` 原版走的**是同一条链**
        /// —— `Daily Login Container`（GO `-4858811403846176071` · `MissionContainer` MB `7589217681052316345`）·
        /// `Daily Skulls Mission Container`（GO `6434331890599441081` · MB `2376002841178321593`）·
        /// `Weekly Mission`（GO `-6751940224764471553` · MB `5894080996209071871`；页内那个节点的名字就是它，
        /// ⚠️ **不是** `7948715324918747914` —— 那个 pid 属于**独立 prefab 根** `Weekly Mission Container`
        /// `GO/1384672845988647690`，**不在 `Missions Tab` 树里**）—— 三个实例的 `collectButton` **都非空**
        /// （真包 MB 实读）⇒ 那三颗**同样会重建整页**。**现在四条路都接了**
        /// （见下面 `CollectThenRebuild(string, Func&lt;bool&gt;)`）。</para>
        /// </summary>
        void CollectThenRebuild(int index)
        {
            CollectThenRebuild("第 " + (index + 1) + " 行", () => DailyData.CollectDaily(index));
        }

        /// <summary>🆕 **A75①（2026-10-05）**：三张**单例卡**（登录 / 骷髅 / 周常）的 `Collect` 走的是
        /// **同一条**链 —— 真包实读 `bundle_menus_assets_all` 里共 **21 个 `MissionContainer` MB**，
        /// 本页四张卡的根各挂一个、**`collectButton` 全部非空**
        /// （每日行 MB `1982546340298365136` · 登录卡 MB `7589217681052316345` ·
        ///  骷髅卡 MB `2376002841178321593` · 周常 MB `5894080996209071871`）
        /// ⚠️ 周常那一项**别写成** MB `7948715324918747914` —— 那是**独立 prefab** `Weekly Mission Container`
        /// 的根（`GO/1384672845988647690`，其 RT `m_Father = {m_PathID: 0}`），**不在本页树里**；
        /// 页内那个节点叫 **`Weekly Mission`**、MB 是 `5894080996209071871`（两者 `collectButton` 都非空）。
        /// ⇒ 那三颗同样 `OnCollect` ⇒ **整页重建**（我们原来只在每日行那一条路上接了）。
        /// 🔴 **只在这一下真的领到时才重建**：没达成时原版那颗钮 `interactable = CanCollect()` = false，
        /// 点了连派发都没有 ⇒ `collect()` 必须回传「领没领到」。
        /// ✅ **2026-10-05（B4）起「点了连派发都没有」是字面事实** —— 那颗钮现在真的带
        /// `WindowButton.Interactable = CanCollectXxx()`（见 `BuildButton`），`Click()` 会先挡住并出声；
        /// 这一层守卫**保留**（同一份布尔、同一处算出来的，不是第二份判据）。
        /// ⚠️ 那三张卡的 `Collect` **显隐不走 `displayRule`**（它们的 `infoDisplays` 里根本没有 `Collect`
        /// 这一件、`dr` 几乎全是 `-1` = 恒可见）⇒「藏不藏它」由 `interactable` 管，**本件不动它**，
        /// 也**别**把 A44 甲「四个件同吃 `displayRule`」那条口径推广过来。</summary>
        void CollectThenRebuild(string what, System.Func<bool> collect)
        {
            if (!collect()) return;   // 未达成 / 已领过：原版那一下连派发都没有，我们什么都不做
            Debug.Log("[Missions] " + what + " 的 `Collect` **领到了** ⇒ **重建整页**"
                      + "（原版链路：`CollectChallenge` 的 `onComplete` = `MissionContainer.OnCollect` → "
                      + "`ChangeTab<MissionsTab>` → `TryOpenTab` → `OnOpen` → `CreateMissions`；"
                      + "判据 `资料/普查产出_1004/X2审查_A44甲.md` §一·附）");
            Build();
        }

        /// <summary>点垃圾桶 ⇒ 开「重摇任务」窗（原版 `MissionReRollButton` 的点击链，见上面那段注释）。
        /// 弹窗里 `Confirm` 回来时会**重建本页**（新任务要立刻看得见）。</summary>
        void OpenReroll(int index)
        {
            var mgr = _win != null ? _win.Manager : null;
            if (mgr == null)
            {
                Debug.LogWarning("[Missions] 点重摇时拿不到 `WindowsManager`（`_win.Manager` 为空）"
                                 + " ⇒ **窗开不出来**（红线：不许静默失败）");
                return;
            }
            var pop = MissionRerollPopup.Create(mgr);
            mgr.OpenWindow(pop, new MissionRerollContext { Index = index, OnRerolled = Build });
            Debug.Log("[Missions] 第 " + (index + 1) + " 行的垃圾桶 ⇒ 开 `MissionReRollPopup`"
                      + "（**重摇任务**，不是删除；原版链路见 `资料/待办判据_阶段二与联机.md` §A23）");
        }

        /// <summary>`Daily Login Bonus Container`（竖卡 334×555）。`card` 是**设计空间**矩形（见 `R`）。</summary>
        public void BuildLoginCard(Transform parent, PxRect card)
        {
            parent = NodeD(parent, "Daily Login Container", card);
            Draw(parent, "40K_missions_display_Daily_vertical", card, "Daily Login Bonus Container", RewardsWindow.QPanel);
            BuildCardHeader(parent, card, "Daily Login Bonus", DailyData.LoginTitle(), 30f, false);

            // `body.image`  N(3, 0,0, 1,1, .5,0, **0,−29**, 0,0)  → `40K_missions_icon_login bonus`
            var body = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                      new Vector2(-0.0018845f, 84.7947f), new Vector2(325f, 261.131f));
            // 🔴 **2026-09-23 修**：原来第 5 个参数传的是 `Vector2.zero`，把 `pos=(0,−29)` 丢了。
            //    判据（`工具/menu_rect.py "Daily Login Bonus Container" --depth 4 --relative` 实算）：
            //    `body` = 4.50..329.50 × **62.14..323.27**，`image` = 4.50..329.50 × **91.14..352.27** ⇒ **下移 29.00**。
            //    `pos.y` 是 **up-positive**（UGUI），所以「下移 29」写 **−29**。
            var img = UguiRect.Child(body, UguiRect.A00, UguiRect.A11, UguiRect.P50,
                                     new Vector2(0f, -29f), Vector2.zero);
            var q = Draw(parent, "40K_missions_icon_login_bonus", img, "image", RewardsWindow.QContent);
            if (q != null) { q.SetAspect(262f / 212f); }        // 图 262×212（正本 §九）

            // `footer.Rewards`  N(3, .5,.5, .5,.5, .5,.5, 0,52.108, 325,77.643)  两个奖励格
            // 该组实测 `m_ChildAlignment=4(MiddleCenter) · ctlW/H=1 · expW/H=1` ⇒ 两格**等分** 325（我们照此）
            var footer = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                        new Vector2(-1.5201f, -153.84f), new Vector2(325f, 181.86f));
            var rw = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                    new Vector2(0f, 52.108f), new Vector2(325f, 77.643f));
            // 🔴 **两格画的是「登录卡这一份奖励」**（🆕 2026-10-05 **B4**）—— 与 `CollectLogin` 发的**同源**
            //    （`DailyData.LoginRewardArt/Count`，逐格发）。⛔ 别再走「按下标查的公共表」：
            //    那张表原来在这一卡上画的是「金块 ×150 + 封印点 ×20」，而 `CollectLogin` 只发金块 ×100
            //    ⇒ 图标 / 数量 / 发放三者不一致。
            // ⚠️ **格数 = 我们自己那张表的长度**（不是照原版抄的）：原版的格数是**数据驱动**的、那份数据
            //    （PlayFab Title Data 的 `MissionsConfig`）**本地没有** ⇒ 判据全文见 `DailyData` 的
            //    「「格数」的数据源」那一段（块6 · 件②）。每一格的**币种与数量也都是我们挑的**。
            for (int i = 0; i < DailyData.LoginRewardCells; i++)
                BuildRewardCell(parent, new PxRect(rw.x1 + rw.W * 0.5f * i, rw.y1, rw.x1 + rw.W * 0.5f * (i + 1), rw.y2),
                                DailyData.LoginRewardArt(i), DailyData.LoginRewardCount(i).ToString(), i.ToString());

            // `footer.Generic UI Button`  N(3, …, 3.1692,-24.0231, 255.992,74.6201)  `40K_button` 色 (1,0.47,0.10,1)
            var btn = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(3.1692f, -24.0231f), new Vector2(255.992f, 74.6201f));
            // 🆕 **A75①**：这一颗也走 `OnCollect` ⇒ **领到就重建整页**（判据见 `CollectThenRebuild(string,…)`）。
            BuildButton(parent, btn, "40K_button", new Color(1f, 0.47f, 0.10f, 1f), "Collect", 35f, "Generic UI Button",
                        () => CollectThenRebuild("登录卡", () => DailyData.CollectLogin()), DailyData.CanCollectLogin());

            // `footer.TimerHolder`  N(3, 0,0.5, 1,0.5, .5,0, 0,-118.5, 0,57.167)   文本 'Resets in …' fs28 灰
            // 🔴 **显示条件 = 已领取**（🆕 **A75②**）：原版这一件是 **`MissionTimerDisplay` · `displayRule = 2
            //    (WhenComplete)`** ⇒ 与每日行的 `timer` 同一条规则（`description`/`timer` 互斥那条）。
            //    判据（真包实读，2026-10-05 复核）：登录卡 `MissionContainer` MB `7589217681052316345` 的
            //    `infoDisplays` 第 2 项 = MB `3730529517176468153`（**`displayRule = 2`**），
            //    其 `m_GameObject` → `GameObject/Timer_-4321384230747458887.json`；规则本体 =
            //    `DF:MissionInfoDisplay__Initialize.c:10-27`。**我们原来恒画它**（`Resets in 12h 34 m`）。
            //    ⚠️ 与每日行同源：谓词是 `IsComplete` = **奖励已领取**（不是「进度到顶」），见 `DailyData.LoginClaimed`。
            //    ⚠️ 文案照旧是 **prefab 出厂那个串** `'Resets in 12h 34 m'`（§3·3 #16）—— 原版运行期按 I2 词条本地化，
            //    本地没有语言表（`DailyData.ResetIn`）。
            if (DailyData.LoginClaimed())
            {
                var th = UguiRect.Child(footer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f),
                                        new Vector2(0f, -118.5f), new Vector2(0f, 57.167f));
                var tl = Txt(parent, th, DailyData.ResetIn(), new Color(0.5686f, 0.5686f, 0.5882f, 1f), "Timer", 28f);
                if (tl == null) Debug.LogWarning("[Rewards] 登录卡的 `Timer` 没建出来（红线：不许静默失败）");
            }
        }

        /// <summary>自检用：每日骷髅卡 `counter/icons` 那块「图标区」的**左边缘**（画布 px）。
        /// 判据 → `MissionCounterDisplay__Setup.c:51-63`（`Army == Neutral` 时那一格不显示 ⇒
        /// 这个值就是 `counter` 自己的左边缘，**没有那 60px**）。</summary>
        public static float SkullIconLeftPx { get; private set; }

        /// <summary>`Daily Skulls Mission Container Small`（336×277.5）—— 5 格里程碑 + 计数 + 领奖。
        /// ⚠️ 页内那份实例的 `body`/`progress` 尺寸与独立预制体**不同**（正本 §三·4 vs 页内实例）；
        /// 我们照**独立预制体 Small**（那套尺寸是确定的）。`card` 是**设计空间**矩形（见 `R`）。</summary>
        public void BuildSkullsCard(Transform parent, PxRect card)
        {
            parent = NodeD(parent, "Daily Skulls Mission Container", card);
            Draw(parent, "40K_missions_display_Daily_vertical", card, "Daily Skulls Mission Container", RewardsWindow.QPanel);
            BuildCardHeader(parent, card, "Daily Skulls", DailyData.SkullsTitle(), 36f, false);

            // `progress.milestones`  N(3, 0,0, 1,1, .5,.5, 0,0, ~0,~0)  → `steps` HLG **spacing 20** align 4(MiddleCenter)，每格 40×40
            var prog = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                      new Vector2(-0.0018959f, -21.313f), new Vector2(325f, 79.992f));
            var ms = UguiRect.Child(prog, UguiRect.A00, UguiRect.A11, UguiRect.P50c, Vector2.zero, Vector2.zero);
            // 🔴 **2026-09-23 修**：`steps` 的实测布局组参数是
            //    `align=4 (MiddleCenter) · sp=20 · ctlW=0 · ctlH=0 · expW=0 · expH=1`
            //    ⇒ 5 格各 40 + 4×20 = **280 宽放进 325 的容器**，`MiddleCenter` ⇒ **左右各留 22.5**。
            //    原来 `UguiLayout.HorizontalChild` **不做水平对齐**（从容器左边起排）⇒ 整排偏左 22.5px。
            float contentW = 5f * 40f + 4f * 20f;
            float padL = (325f - contentW) * 0.5f;
            for (int i = 0; i < 5; i++)
                BuildMilestone(parent, UguiLayout.HorizontalChild(ms, 40f, 40f, i, padL, 20f),
                               DailyData.SkullsStepDone(i), true);

            // `footer.Rewards`  N(3, …, -103.7,14.204, 109.25,47.433)  → `40K_missions_icon_Daily skulls` + 'x160'
            var footer = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                        new Vector2(-1.5201f, -100.83f), new Vector2(325f, 75.84f));
            var rw = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                    new Vector2(-103.7f, 14.204f), new Vector2(109.25f, 47.433f));
            // 🔴 **这一格画的是「骷髅卡这一份奖励」**（🆕 2026-10-05 **B4**）—— 与 `CollectSkulls` 发的**同源**。
            //    原来走的是按下标的公共表 ⇒ 这一卡画的是**封印点 ×20**（而 `CollectSkulls` 发 0 个骷髅）
            //    = 图标 / 数量 / 发放三者全对不上。
            // 🔴 **2026-10-05（块6）依据【已被独立审查证伪】⇒ 按铁律 3 降级为「我们挑的」**。这里原来写
            //    「判据 = 原版 prefab §3·4 #6：`CampaignPointDrawer`、count **'200'**」—— **两半都不成立**：
            //    · 原版这一格是**数据驱动**的（`MissionRewardsDisplay__Setup.c:46` 先 `DestroyAllChildren`、
            //      再按 `AvailableRewards()` 逐格 `Instantiate`），`count '200'` 只是**会被删掉的占位**；
            //    · **图更不是 prefab 给的**：该格 Image 实测 **`m_Sprite = 0`（没图）**，同级那件是同族通用的
            //      `Campaign Glow` = `40K_genearl_icon_Campaign points_big` ⇒ 真值由 `CampaignPointDrawer` 运行期画。
            //    ⇒ 下面这两个实参（骷髅图 / 200）**都是我们挑的**；判据全文见 `DailyData` 的「骷髅卡」那一段。
            BuildRewardCell(parent, rw, DailyData.SkullsRewardArt(), DailyData.SkullsRewardCount().ToString(), "1");
            // `footer.counter`  N(3, …, -79.2,150.3, 167.6,59.925)
            //   `counter` 自己也有布局组；`icons` 那条 HLG 的**两个格子**实测是
            //   `Army`（60 宽，模板占位图 `40k_DeckSelection_icon_FactionBlackLegion`）+ `skull`（65 宽）⇒ 图标区共 **125 宽**。
            // 🔴 **2026-10-03 查实并改对**（`项目任务.md` §三 第 29 条 **B1**）：
            //   真机制 = `d:/2/tools/decomp_full/MissionCounterDisplay__Setup.c:51-63` ——
            //   图 = `ArmyUtilities.GetArmyIcon(challenge.Army)`，**且 `army == Neutral(0)` 时
            //   那个 `Army` 整格 `SetActive(false)`**（HLG 会跳过它 ⇒ skull 与计数文字**整体左移 60**）。
            //   prefab 里那个 `40k_DeckSelection_icon_FactionBlackLegion` **只是模板占位**，不是真值。
            //   ⚠️ **我们这份 daily 数据里没有阵营维度**（`Army` 由服务端下发、`grep anyArmy` 只命中静态成就）
            //   ⇒ 按 **Neutral** 走 —— 也就是**不画那一格、也不给它留位**（此前是留了 60px 空槽，**是错的**）。
            var cnt = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(-79.2f, 150.3f), new Vector2(167.6f, 59.925f));
            const float armyW = 60f, skullW = 65f;
            float iconL = cnt.x1;
            string army = DailyData.SkullsArmy();                 // 我们的 mock 恒 null = Neutral
            if (!string.IsNullOrEmpty(army))
            {
                // 有阵营才画那 60 宽（图走 `DeckRuntime.FactionIcon` —— 全工程唯一一份阵营徽记）
                Draw(parent, DeckRuntime.FactionIcon(army),
                     new PxRect(iconL, cnt.y1, iconL + armyW, cnt.y2), "Army", RewardsWindow.QContent,
                     null, true);
                iconL += armyW;
            }
            else
            {
                Debug.Log("[Missions] 每日骷髅任务的 `counter/Army` 那一格**不建**（原版 `army == Neutral(0)` 时 "
                          + "`SetActive(false)`；我们这份 daily 没有阵营维度 ⇒ 走 Neutral 分支，**也不给它留 60px**）");
            }
            SkullIconLeftPx = R(new PxRect(iconL, cnt.y1, iconL, cnt.y2)).x1;   // 自检用（转成**画布 px**）
            Draw(parent, "40K_missions_icon_Daily_skulls",
                 new PxRect(iconL, cnt.y1, iconL + skullW, cnt.y2), "skull", RewardsWindow.QContent);
            // ⚠️ 计数用 `Txt1`（**不换行**）：`icons` 占掉 125 宽后剩下的框只有 ~38 宽，
            //    走 `TextBox` 会把 `x160` 折成 `x1`+`60` 两行（2026-09-23 渲染图就是这个）。
            Txt1(parent, new PxRect(iconL + skullW + 5f, cnt.y1, cnt.x2, cnt.y2),
                 DailyData.SkullsCounter(), Color.white, "counter text", 26.8f);

            // `footer.Generic UI Button`  N(3, …, 58,13.548, 187.467,80.492)  `40K_button` 色 (1,0.47,0.10,1)
            // 🆕 **A75①**：这一颗也走 `OnCollect` ⇒ **领到就重建整页**（同一条链，见 `CollectThenRebuild`）。
            var btn = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(58f, 13.548f), new Vector2(187.467f, 80.492f));
            BuildButton(parent, btn, "40K_button", new Color(1f, 0.47f, 0.10f, 1f), "Collect", 34.05f, "Generic UI Button",
                        () => CollectThenRebuild("骷髅卡", () => DailyData.CollectSkulls()), DailyData.CanCollectSkulls(),
                        12f, 44f);

            // `footer.TimerHolder`  N(3, 0,0.5, 1,0.5, .5,0, 83.55,120.34, -167.1,59.926)  时钟 + 时间
            var th = UguiRect.Child(footer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f),
                                    new Vector2(83.55f, 120.34f), new Vector2(-167.1f, 59.926f));
            BuildClockRow(parent, th, 44.74f, DailyData.ResetIn(), 30.15f, 15f, 38f);
        }

        /// <summary>`Weekly Mission`（1518.99×227.51）· 4 个 70² 里程碑 + 进度条 + `Ends in`。
        /// 🔴 **2026-10-05（块6）改名 + 去掉自造的那一层**：原版节点名是 **`Weekly Mission`**
        /// （`MissionContainer`，父 = `Weekly Mission Holder`）；我们此前建的是
        /// `Missions Tab / Weekly Mission / Weekly Mission Container` —— 外层名字是 Holder 的位置、
        /// 内层名字是**我们自己发明的**（`Weekly Mission Container` 在原版里是**另一份独立 prefab 的根**，
        /// `GO/Weekly Mission Container.json` 的 RT `m_Father = {m_PathID: 0}`，**不在 `Missions Tab` 树里**）。
        /// ⇒ 现在 `parent` 进来时**已经就是**那个 `Weekly Mission` 节点（`Build()` 里建的，矩形 = `card`），
        /// 所以这里**不再建容器层**，直接把卡的内容挂在它下面。</summary>
        public void BuildWeekly(Transform parent, PxRect card)
        {
            Draw(parent, "40K_missions_display_Weekly", card, "Weekly Mission", RewardsWindow.QPanel);

            // `header`  N(2, 0,1, 0.25,1, .5,1, 0,0, 0,55) → `name` 'Weekly Challenge' fs36
            // 🔴 **2026-09-23 补**：`header` 自带 **`Gradient2`（灰蓝那套，5 个 alpha 键）** —— 见 `HeaderGradient`
            var head = UguiRect.Child(card, new Vector2(0f, 1f), new Vector2(0.25f, 1f), new Vector2(0.5f, 1f),
                                      Vector2.zero, new Vector2(0f, 55f));
            DrawTex(parent, HeaderGradient(true), head, "header bg", RewardsWindow.QPanel);
            Txt(parent, new PxRect(head.x1, head.y1, head.x2, head.y1 + 50f),
                "Weekly Challenge", Color.white, "name", 36f);

            // `progress.Mission Progress Bar`  N(3, 0,0.5, 1,0.5, 0,0.5, 30,-9.6, -60,22.766)
            var prog = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                      new Vector2(-185.175f, -20.537f), new Vector2(1068.43f, 158.59f));
            var bar = UguiRect.Child(prog, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                                     new Vector2(30f, -9.6f), new Vector2(-60f, 22.766f));
            BuildBar(parent, bar, DailyData.WeeklyProgress01());

            // 🔴 **2026-09-23 补：`Handle` 与骑在它上面的 `counter`**
            //   实测（`工具/menu_rect.py "Weekly Mission Container" --depth 6 --relative --root-size 1518.99x227.51`；
            //   ⚠️ 那个名字是**独立 prefab 根**的名字 —— 页内节点叫 `Weekly Mission`，两者子树逐件同值）：
            //   `Mission Progress Bar` = `Handle Slide Area` = 70.10..1078.54（容器），
            //   `Handle` = 4.14 × 50.60、**无 sprite、色 (0.941,0.725,0.314,1)** ⇒ UGUI 画**一块实心矩形**；
            //   `counter` 是 **`Handle` 的子节点**（模板位 62.53 × 35.01，**底边 = Handle 顶边**）。
            //   原版 `Slider` 的值由 `MissionProgressBarDisplay` 运行时设 ⇒ **把手跟着进度走、数字骑在把手上**。
            //   ⚠️ 每日任务行那份的 `Handle Slide Area` 出厂 **`activeSelf=false`** ⇒ 那一处**不画**才对（我们没画，对）。
            float t = DailyData.WeeklyProgress01();
            const float hw = 4.141f, hh = 50.60f, cw = 62.53f, ch = 35.01f;
            float hx = bar.x1 + t * bar.W;
            var handle = new PxRect(hx - hw * 0.5f, bar.CY - hh * 0.5f, hx + hw * 0.5f, bar.CY + hh * 0.5f);
            Draw(parent, null, handle, "Handle", RewardsWindow.QContent, new Color(0.941f, 0.725f, 0.314f, 1f));
            // ⚠️ 用 `Txt1`（**不换行**）：这个框只有 62.53 宽，走 `TextBox` 会把 `13/15` 折成两行
            //    （2026-09-23 渲染图上就是 `13/` + `15`）。
            Txt1(parent, new PxRect(hx - cw * 0.5f, handle.y1 - ch, hx + cw * 0.5f, handle.y1),
                 DailyData.WeeklyCounter(), new Color(0.92f, 0.77f, 0.48f, 1f), "counter", 33.15f);

            // `Mission Milestones Progress.steps`  N(4, 0,0, 1,1, 0,0.5, 0,47, 0,0)
            //   `EverguildLayoutGroup` spacing **262.81** align 4 ⇒ 4 格 70²，从容器左边起排
            //   （4×70 + 3×262.81 = **1068.43 = 容器宽** ⇒ 对齐方式无关，左右刚好占满）
            var mp = UguiRect.Child(prog, UguiRect.A00, UguiRect.A11, UguiRect.P50c,
                                    new Vector2(0f, -56.1377f), Vector2.zero);
            var steps = UguiRect.Child(mp, UguiRect.A00, UguiRect.A11, new Vector2(0f, 0.5f),
                                       new Vector2(0f, 47f), Vector2.zero);
            for (int i = 0; i < 4; i++)
                BuildMilestone(parent, UguiLayout.HorizontalChild(steps, 70f, 70f, i, 0f, 262.81f),
                               DailyData.WeeklyStepDone(i), false);

            // `footer`（HLG sp 0）→ `Generic UI Button`  N(3, …, 3.1692,0, 294.29,74.62)
            var footer = UguiRect.Child(card, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                        new Vector2(582.18f, -29.3f), new Vector2(300.631f, 102.049f));
            var btn = UguiRect.Child(footer, UguiRect.P50c, UguiRect.P50c, UguiRect.P50c,
                                     new Vector2(3.1692f, 0f), new Vector2(294.29f, 74.62f));
            BuildButton(parent, btn, "40K_button", new Color(1f, 0.47f, 0.10f, 1f), "Collect", 44f, "Generic UI Button",
                        () => CollectThenRebuild("周常卡", () => DailyData.CollectWeekly()), DailyData.CanCollectWeekly());
            // ⚠️ 周常**没有「奖励格」这一件**（原版 `Rewards` 出厂 `activeSelf = false`，正本 §3·5 #8）
            //    ⇒ B4 改的是「每日行 / 登录卡 / 骷髅卡」三处；周常那份奖励只在 `CollectWeekly` 里（500 金块，我们挑的）。

            // `TimerHolder.Timer`  N(4, 0,0.5, 1,0.5, .5,.5, 0,31.287, 100,57.167)  'Ends in …' fs38 灰
            var th = UguiRect.Child(footer, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f),
                                    new Vector2(0f, -118.5f), new Vector2(0f, 57.167f));
            var tm = UguiRect.Child(th, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), UguiRect.P50c,
                                    new Vector2(0f, 31.287f), new Vector2(100f, 57.167f));
            Txt(parent, tm, DailyData.WeeklyEndsIn(), new Color(0.57f, 0.57f, 0.59f, 1f), "Timer", 38f);
        }

        /// <summary>`Mission Header`：标题条（`Daily Missions` 那一条）。
        /// `isSkulls` 只是文案不同（原版同一份 prefab 换了 `name`/`Refill Counter` 的文本）。</summary>
        public void BuildMissionHeader(Transform parent, PxRect hostRect, bool isSkulls)
        {
            // N(3, 0,1, 1,1, .5,1, 1.34,-2.2287, -2.6799,52.7713)
            var h = UguiRect.Child(hostRect, new Vector2(0f, 1f), UguiRect.A11, new Vector2(0.5f, 1f),
                                   new Vector2(1.34f, -2.2287f), new Vector2(-2.6799f, 52.7713f));
            // 🔴 **2026-09-23 补**：这条的底是 `Image(sprite=null, type=Sliced)` + **`Gradient2`**（紫→棕那套），
            //    见 `HeaderGradient` 的注释（`Image` 没 sprite 也会渲染成一块纯色矩形）。
            DrawTex(parent, HeaderGradient(false), h, "Mission Header bg", RewardsWindow.QPanel);
            // `name`  N(3, 0.03,0.5, 0.84,0.5, .5,.5, 0,0, ~0,50)   'Daily Missions' fs36
            var nm = UguiRect.Child(h, new Vector2(0.03f, 0.5f), new Vector2(0.84f, 0.5f), UguiRect.P50c,
                                    Vector2.zero, new Vector2(3.8147e-06f, 50f));
            // ⚠️ 原版这条实测 **`H=Left`**；居中写会和右边右对齐的 `Refill Counter` **叠在一起**
            //    （第一版渲染图上是 `Daily Mis0Disponible`）
            AlignL(Txt(parent, nm, isSkulls ? "Daily Skulls" : "Daily Missions", Color.white,
                       "name (Mission Header)", 36f, 12f), nm);
            // `info`  N(3, 1,0.5, 1,0.5, 1,0.5, -10,0, 41,41)   `40K_generic_bt_info` 41²
            var inf = UguiRect.Child(h, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                     new Vector2(-10f, 0f), new Vector2(41f, 41f));
            Draw(parent, "40K_generic_bt_info", inf, "info", RewardsWindow.QContent);
            // `Refill Counter`  N(3, 0,0.5, 1,0.5, .5,.5, -26.93,0, -53.86,50)   '0 Disponible' fs36
            var rc = UguiRect.Child(h, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), UguiRect.P50c,
                                    new Vector2(-26.93f, 0f), new Vector2(-53.86f, 50f));
            var rcT = Txt(parent, rc, DailyData.RefillText(), Color.white, "Refill Counter", 36f, 12f);
            // 🔴 **右对齐要用【缩放后】的框右边缘**（`R(rc).x2`）—— 这里原来直写 `rc.x2`（设计空间）
            //    ⇒ 每日任务那块右边缘一旦被 `localScale` 放大（A124），这段字会**比它的框短一截**、贴不到右边
            //    （静默：位置上仍在屏内、也不与左边的 `name` 相撞 ⇒ 现有断言一条都抓不到）。
            if (rcT != null) rcT.AlignRightOn(LayoutSpace.FromPixel(R(rc).x2, 0f).x);
        }

        // ============================================================ 小件

        /// <summary>卡片 `header` 的**渐变条**（原版 `Image(sprite=null)` + `Gradient2`）。
        /// 实测两套值（`bundle_menus_assets_all` 的 `_effectGradient`，逐个读出来的）：
        /// · **紫→棕**（`greyBlue=false`）：颜色 (0.247,0.188,0.380)→(0.475,0.306,0.153)，
        ///   alpha 键 **0.2706→1.0 · 0.9059→0.098**（也就是左起 27% 之前**全不透明**、到 91% 才降到 0.098）。
        ///   用在：每日登录卡 · 每日骷髅卡 · `Mission Header`（**三处同值**）。
        /// · **灰蓝**（`greyBlue=true`）：颜色**恒定** (0.227,0.286,0.325)，**5 个 alpha 键**
        ///   0 / 0.3706 / 0.5941 / 0.7176 / 0.8588 → 1.0 / 0.773 / 0.498 / 0.463 / 0.098。用在周常卡。
        /// 轴向 `_gradientType = 0` = **Horizontal**
        /// （`Gradient2.Type { Horizontal=0, Vertical=1, Radial=2, Diamond=3 }`，出处
        /// `Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/Gradient2.cs`）。
        /// 🔴 **`Image` 没有 sprite 也会渲染** —— UGUI 在 `activeSprite == null` 时回落到
        /// `Graphic.OnPopulateMesh`、画一块**纯色矩形**，再被 `Gradient2`（`_modifyVertices=1`）改顶点色
        /// ⇒ 原版每张卡的头都**有一条看得见的渐变条**；我们**一条都没画**（2026-09-23 取证）。</summary>
        static Texture2D HeaderGradient(bool greyBlue)
        {
            if (greyBlue)
                return CardArt.GradientKeys(new Color(0.227f, 0.286f, 0.325f), new Color(0.227f, 0.286f, 0.325f),
                                            0f, new float[] { 0f, 0.3706f, 0.5941f, 0.7176f, 0.8588f, 1f },
                                            new float[] { 1f, 0.773f, 0.498f, 0.463f, 0.098f, 0.098f });
            return CardArt.GradientKeys(new Color(0.247f, 0.188f, 0.380f), new Color(0.475f, 0.306f, 0.153f),
                                        0f, new float[] { 0f, 0.2706f, 0.9059f, 1f },
                                        new float[] { 1f, 1f, 0.098f, 0.098f });
        }

        /// <summary>画一块**运行时生成的**贴图（渐变走这条）。`r` 是**设计空间**矩形。</summary>
        void DrawTex(Transform parent, Texture2D tex, PxRect r, string name, int q)
        {
            if (tex == null) return;
            var f = R(r);
            var quad = ImageQuad.Create(parent, tex, RewardsWindow.Local(parent, f.x1, f.y1, f.x2, f.y2),
                                        LayoutSpace.Px(f.H), new Vector2(0.5f, 0.5f), name);
            if (quad == null) return;
            quad.SetAspect(f.W / Mathf.Max(1e-6f, f.H));
            quad.SetRenderQueue(q);
        }

        /// <summary>卡头：`header` 渐变条 + `40K_generic_bt_info`（42×42 在右）+ `name`。</summary>
        void BuildCardHeader(Transform parent, PxRect card, string what, string title, float fontPx, bool greyBlue)
        {
            // N(2, 0,1, 1,1, .5,1, 1.34,-2.2287, -2.6799,52.7713)
            var h = UguiRect.Child(card, new Vector2(0f, 1f), UguiRect.A11, new Vector2(0.5f, 1f),
                                   new Vector2(1.34f, -2.2287f), new Vector2(-2.6799f, 52.7713f));
            DrawTex(parent, HeaderGradient(greyBlue), h, what + " header bg", RewardsWindow.QPanel);
            var nm = UguiRect.Child(h, new Vector2(0.03f, 0.5f), new Vector2(0.84f, 0.5f), UguiRect.P50c,
                                    Vector2.zero, new Vector2(3.8147e-06f, 50f));
            // ⚠️ 原版这条 TMP 实测 **`H=Left`**（`m_HorizontalAlignment=1`）⇒ 标题贴左边起，**不是居中**
            // 🔴 **2026-10-06（A143）把卡头 TMP 的自适应三字段核到底了**。判据优先取**页内真值** ——
            //    `GameObject/Missions Tab.json` 的 `…/Special Missions/Daily Login Container/background/header/name`
            //    与 `…/Daily Skulls Mission Container/background/header/name`（**这才是玩家看到的那份**；
            //    见 `资料/普查产出_1006/A143_SM字号断言.md` §七）。两处**都**是：
            //      `m_enableAutoSizing = 1` · `m_fontSizeMin = **20**`（卡头两颗一致）
            //      · `m_fontSizeMax`：登录卡 **36** · 骷髅卡 **36**（各自的 `m_fontSize` 是 35.15 / 36）
            //    旁证（独立预制体，同两处 `m_fontSizeMin` 也是 20）：`MonoBehaviour/MonoBehaviour_6907930910134838868.json`
            //    · `…_4389928180546541826.json`。
            //    ✅ **对得上的两半**：① `Txt` 的第 2 个参数（`autoMinPx`）**确实对应原版 `m_fontSizeMin`**
            //      （口径出处 = `MenuDraw.Text` 的 summary「传原版那两个字段的原文即可」；反证 = 每日行
            //       `description` 那处 `Txt(…, 35f, 15f)` 与原版实测 `m_fontSizeMin 15` 吻合）；
            //       ② `Txt` 把自适应**上界**设成 `fontPx` 的口径本身没错（只是**值**要照原版填）。
            //    ✅ **本行已按原版改**：`autoMinPx` **12 → 20**（下界以前矮 8px：卡头标题在窄框里会被 TMP 压得更小）。
            //    🔴 **还没对齐的**：登录卡卡头的**设计字号/上界** —— 页内真值是 `fs 35.15 / max 36`，
            //      我们传的是 `30`（取自**独立预制体** `Daily Login Bonus Container`）。
            //      ⚠️ 同族还有一批（`count` / `Button Text` / `Timer` / `counter`）也是「照独立预制体 vs 照页内」的
            //      来源分叉 ⇒ **判据要调度台裁定**，本件只动了「两处来源一致」的那些，详见报告 §七。
            AlignL(Txt(parent, nm, title, Color.white, what + " name", fontPx, 20f), nm);
            var inf = UguiRect.Child(h, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                     new Vector2(-10f, 0f), new Vector2(41f, 41f));
            Draw(parent, "40K_generic_bt_info", inf, what + " info", RewardsWindow.QContent);
        }

        /// <summary>进度条：两张图都是**九宫格 `(4,4,4,4)`**、12×12（正本 §九）。底色 `(1,0.59,0,1)` · 填充 `(1,0.77,0.33,1)`。
        /// `r` 是**设计空间**矩形（`R()` 在这里过一次就够，九宫格内部按最终矩形算）。</summary>
        void BuildBar(Transform parent, PxRect r, float t01)
        {
            var f = R(r);
            BuildNine(parent, _win.Art("40k_generial_bar_empty"), f, 4, 12f, 12f,
                      new Color(1f, 0.59f, 0f, 1f), true);
            if (t01 <= 0.001f) return;
            var fill = new PxRect(f.x1, f.y1, f.x1 + f.W * Mathf.Clamp01(t01), f.y2);
            BuildNine(parent, _win.Art("40k_generial_bar_fill"), fill, 4, 12f, 12f,
                      new Color(1f, 0.77f, 0.33f, 1f), true);
        }

        void BuildNine(Transform parent, Texture2D tex, PxRect r, float border, float texW, float texH, Color tint, bool fillCenter)
        {
            // 缺图 ⇒ 什么都不建（`Art()` 已经记过账）。⚠️ 这一句还顺带挡掉了**返回值的语义差**：
            // `ImageQuad.CreateNineSlice` 在 `tex == null` 时**照样返回一个空根节点**（+ 一条告警），
            // 而公共件 `MenuDraw.Nine` **返回 `null`**（判据 → `资料/已知的坑.md` 2026-10-06 那条）。
            if (tex == null) return;
            // 🔴 **2026-10-06（A50③ 残留）：改走公共件 `MenuDraw.Nine`** —— 旧写法直调
            //   `ImageQuad.CreateNineSlice`（= 绕开公共件的那条路，**拿不到 `clip` / `clipSoftness`**）。
            //   与旧代码**逐项等价**（四样都别改）：
            //    ① **矩形** = `r`：旧代码那两个实参就是公共件内部要算的**同一句**
            //       （`Local(parent, r.x1…r.y2)` + `LayoutSpace.Px(r.W)` / `Px(r.H)`，一字不差）；
            //    ② **落位** = `RewardsWindow.Local(…)` 与 `MenuDraw.Local(…)` **逐字同源**
            //       （都是 `LayoutSpace.RectCenter(…) − parent.position`）⇒ 本处**不需要**
            //       「`parent.position == 0`」那条前提（与卡组编辑那处不同：那边旧代码用的是**绝对世界点**）；
            //    ③ **队列 = `QContent`** · **tint 原样传**：旧代码建完逐块设的就是这两样，公共件会替我们设；
            //       本包装的 `tint` 是**非空**形参 ⇒ 两边都设（同一条退化），且都是「先 tint 后队列」。
            //       **子块集合相同** —— 两边都用 `GetComponentsInChildren<ImageQuad>()` 那个**不含未激活件**的重载；
            //    ④ 切边：旧代码第 10 个实参给的就是**同一个** `border` ⇒ 公共件的 `borderOutPx ?? border` 同值；
            //       `name` 同为 `"Nine"`；`fillCenter` 原样传；`clip` 一律 `null`（⛔ 不顺手改观感）。
            //   ⇒ 下面那圈 `foreach` **删掉了**：它设的两个值与公共件内部设的**同值**，
            //     留着就是「同一条规则写两处」（将来改一处、另一处静默不动）。
            MenuDraw.Nine(parent, tex, r, new Vector4(border, border, border, border), texW, texH,
                          RewardsWindow.QContent, tint, fillCenter, "Nine");
        }

        /// <summary>里程碑格。🔴 图在**脚本字段**里：`activeSprite`/`disabledSprite` = `40k_missions_milestone_on`/`_off`；
        /// 色：已达成 **(28,235,26,1)** / 未达成 **(236,218,159,1)**（0–255 量级，正本 §三·7）。
        /// ⚠️ 原版这两格的 `Image` 实测 `Simple + PreserveAspect=1` ⇒ **等比**（67×66 的圆不会被拉成蛋）。</summary>
        void BuildMilestone(Transform parent, PxRect r, bool done, bool small)
        {
            var art = done ? "40k_missions_milestone_on" : "40k_missions_milestone_off";
            var col = done ? new Color(28f / 255f, 235f / 255f, 26f / 255f, 1f)
                           : new Color(236f / 255f, 218f / 255f, 159f / 255f, 1f);
            Draw(parent, art, r, "Milestone" + (done ? "_on" : "_off"), RewardsWindow.QContent, col, true);
        }

        /// <summary>奖励格 `Reward Display Mission Vertical Variant`（`MissionRewardItem`）。
        /// ⚠️ 原版这一格是 `Icon Container Drawer Variant` + 1080² 的内容做 `UIScaleToFit`；
        /// 我们画**抽屉图标 + 数量**（正本 §三·8），不引入那套缩放机制（图标按 `keepAspect` 等比放进去）。
        /// 🔴 **2026-10-05（B4）**：`art` / `countText` 改成**由调用方显式传**（原来传的是 `index`，
        /// 落在一张**按下标查**的公共表 `DailyData.RewardIcon/RewardCount` 上）。那张表**已删** ——
        /// 它和**领取**用的「任务那一份」对不上（第 3 行画骷髅 ×150、实发金块 ×200）。
        /// 现在三个调用点各自传**它自己那一份**：每日行 = 那条任务 · 登录卡 / 骷髅卡 = 各自的奖励源。
        /// `key` 只用于节点命名（`Reward &lt;key&gt;` / `count &lt;key&gt;`，名字沿用旧口径 —— 自检按名字找）。</summary>
        void BuildRewardCell(Transform parent, PxRect r, string art, string countText, string key)
        {
            // `drawerHolder`  N(…, a=(0,0)-(1,1) p=(.5,1) pos=(0,0) sz=(**−35.685, −42.369**))
            // 🔴 **2026-09-23 修**：原来这里用的是**我们自己挑的百分比**（`0.1/0.9` 与 `0.08/0.78`）——
            //    铁律 3 明令不许用「我们挑的」冒充原版。实测
            //    （`menu_rect.py "Daily Mission Container" --depth 4 --relative --root-size 539.188x150`）：
            //    格 126.334×150 里 `drawerHolder` = **17.84..108.49 × 0..107.63**（原来我们画的是 12.63..113.70 × 12..117）。
            var dh = UguiRect.Child(r, UguiRect.A00, UguiRect.A11, new Vector2(0.5f, 1f),
                                    Vector2.zero, new Vector2(-35.685f, -42.369f));
            Draw(parent, art, dh, "Reward " + key, RewardsWindow.QContent, null, true);
            // `count`  N(7, 0,0, 1,0.337, 0.5,0, 0,0.6025, 0,0)  → 文本 fs40（实算 0..126.33 × 98.85..150 ✓ 与我们一致）
            var c = UguiRect.Child(r, UguiRect.A00, new Vector2(1f, 0.337f), new Vector2(0.5f, 0f),
                                   new Vector2(0f, 0.6025f), Vector2.zero);
            Txt(parent, c, countText, Color.white, "count " + key, 40f);
        }

        /// <summary>`40K_button` 底的按钮。🔴 实测这几处的 `Image` 都是 **`m_PreserveAspect = 1`**
        /// （`40K_button` 源图 489×107；骷髅卡那个框 187.47×80.49 ⇒ 原版画出来只有 **187.47×41.02**，我们原来画满 80.49）。
        /// ⚠️ 每日行那个是 `type=Sliced`（源图 border 已是半图宽 ⇒ 等价于四象限拉伸，与整体拉伸几乎同形），
        /// 三张卡上是 `type=Simple`；**两者我们都按「等比放进框、居中」处理** —— 前者记在
        /// `资料/日常_画面逐项对_0923.md` 的「还没查清的」里。
        /// <para>🆕 **B4 起多一个 `interactable`** —— 原版这颗 `Collect` 是 `EverguildButton`，
        /// 可点性 = `CanCollect()`（判据链 → `Shell/DailyData.cs` 的 `CanCollectDaily` 那一段）。
        /// 置假 ⇒ `WindowButton.Interactable` 那两半**一起**生效：**变灰**（材质换 `Everguild/UI/Greyscale`）
        /// + **点了不派发**（原版 `Selectable.OnPointerClick` 头一句）—— 两半的判据都在 `Shell/PromptPopup.cs`。
        /// ✅ **A82（2026-10-05）把「变灰」那半核成了实据**（原版那一下要先满足
        /// `colorTintGreyOnDisable = 1` —— 不为真时 `SwitchMaterial` 的第一句就返回、**根本不灰**）：
        /// 真包 `bundle_menus_assets_all` 里 **`text == "Collect"` 的 `EverguildButton` 共 26 个，
        /// `colorTintGreyOnDisable` 26/26 全是 1**（其中 21 个挂在 GO 名 `Generic UI Button` 上；
        /// 另 2 个叫 `Generic Simplified UI Button`、1 个叫 `Collect`、2 个 GO 名没解出来）——
        /// 按 `m_Script.m_PathID == 1015376240363272691`（= `EverguildButton`）反查、再按 `text` 那个 PPtr
        /// 取 TMP 的 `m_text` 过滤，**不按名字猜**。⚠️ 与派活单上写的「12 颗」不一致（我数出 21 个同名 GO），
        /// 口径差在哪还没查清；但**「全是 1」这一条两种口径都成立**。
        /// 🔴 **灰化链亲读到底**（四环，全部在本地）：
        /// `EverguildButton__DoStateTransition.c:92-100`（`state == 4 (Disabled)` ⇒ `bVar4 = false`
        /// → `SetToStateActiveOrDisabled(0)`）→ `__SwitchMaterial.c:9`（**头一句就是 `*(char*)(this+0x17a)` =
        /// `colorTintGreyOnDisable` 那道闸**，为 0 时整段直接返回）→ `EverguildButtonHelper__DoMaterialRefresh`
        /// → `EverguildButtonHelper__get_DisabledMaterial.c`（**静态缓存一份** `new Material(Shader.Find(<那个串>))`）；
        /// 那个串逐字节实读 = **`"Everguild/UI/Greyscale"`**（`d:/2/tools/il2cpp_out/stringliteral.json` 的
        /// 地址 `0x42AB248`，正是该函数里 `DAT_1842ab248` 那一条）。
        /// 🔴 **只在这一下真的领到时才重建** 那条语义不变：`CollectThenRebuild` 仍靠返回值兜一层（同一份布尔）。
        /// 🔴 灰化是**换材质**，所以下面那句 `q.SetRenderQueue` 不是可有可无的（见那里的判据链）。</para></summary>
        void BuildButton(Transform parent, PxRect r, string art, Color tint, string label, float fontPx, string name,
                         System.Action onClick, bool interactable = true,
                         float autoMinPx = 0f, float autoMaxPx = 0f)
        {
            var q = Draw(parent, art, r, name, RewardsWindow.QContent, tint, true);
            if (q != null)
            {
                var hit = q.gameObject.AddComponent<WindowButton>();
                hit.onClick = onClick;
                // 🆕 2026-10-03 A17：原版这几颗 `Generic UI Button` 都是 SpriteSwap（普查 §块 4 第 1、3、4、5 行）
                // —— 按钮就挂在那张图上 ⇒ `BindSelf` 直接绑自己（`40K_button` → `40K_button_hover`）
                hit.BindSelf(art);
                // 🆕 B4：可点性落到按钮身上（灰 + 挡派发，见 summary）
                hit.Interactable = interactable;
                // 🔴 **A82（2026-10-05）：变灰是「换材质」，换完必须把显式队列补回去。**
                //    判据链（三段都可查；⚠️ **① / ② 记的是 A82 当时的行为** —— `SetMaterial` 后来
                //    被 A85 改成会**保留** `renderQueue` 了，见本段末尾那条 ✅）：
                //      ① `WindowButton.Interactable` 的 setter 走 `RefreshGray()`，而它换材质用的是
                //         `ImageQuad.SetMaterial`（**当时**：`Battle/ImageQuad.cs:160-165` —— 只带贴图）；
                //      ② 它建的是 `new Material(shader)` ⇒ 队列退回 **shader 自带的那一个**，而
                //         `Everguild/UI/Greyscale` 的 SubShader 标签是 `QUEUE: Transparent` = **3000**
                //         （`工具/dump_shader.py "Everguild/UI/Greyscale"` 实读，2026-10-05）；
                //      ③ 本页的**行底图在 `QPanel = 3005`**（`Shell/MenuWindowBase.cs:72`）⇒ 不补这一句，
                //         变灰那颗钮会掉到**底图之下、被自己的行底图盖住** = 画面上「按钮没了」，
                //         而 `AuditGrayLook` 那条断言**照样全绿**（它只看 shader 名，不看队列）。
                //    ⇒ 显式补回它本来就该在的那一层（`Draw` 那一次传的就是 `QContent`）。
                //    ⚠️ 这一句当初写在这里，是因为 `Shell/PromptPopup.cs` 与 `Battle/ImageQuad.cs`
                //      **不在那一批的白名单里**（切块口径，不是判据）。
                //    ✅ **2026-10-03（A85）：通用修已落地** —— `ImageQuad.SetMaterial` 现在**保留**换之前的
                //      `renderQueue`（`Battle/ImageQuad.cs:177-185`），`DeckInfoPopup` 那 8 颗同病也补了
                //      `ReassertButtonQueues()`。⇒ 原来那句话里的「已报给调度台」**已经是过去式**（那是**已做**）；
                //      下面这一句因此在正常路径上是**冗余的**，留着当「这颗钮该在哪一层」的**显式一声**
                //      （旧材质万一读不出队列时它仍然兜得住）。
                q.SetRenderQueue(RewardsWindow.QContent);
            }
            // `Button Text`  N(2, 0,0, 1,1, .5,.5, 0,0, -14,0)  → 文本 fs35 白居中
            // 🔴 **A143（2026-10-06）**：`autoMinPx`/`autoMaxPx` = 原版那两个字段的**原值**
            //    （被调用方传进来 —— 各处的 `m_fontSizeMin/Max` 不同：每日行/周常 12/44 · 登录卡 12/35 · 骷髅卡 12/44）。
            //    ⚠️ 只有**上下界在两处来源上一致**的调用点才填了值（见 `资料/普查产出_1006/A143_SM字号断言.md` §七）。
            var t = UguiRect.Child(r, UguiRect.A00, UguiRect.A11, UguiRect.P50c, Vector2.zero,
                                   new Vector2(-14f, 0f));
            Txt(parent, t, label, Color.white, name + " Text", fontPx, autoMinPx, autoMaxPx);
        }

        /// <summary>「时钟 + 时间」一行（原版 `TimerHolder`：`WF_icon_clock` + 文本）。
        /// 🔴 **A143（2026-10-06）**：`autoMinPx` / `autoMaxPx` = 原版那两个字段的**原值**
        /// （骷髅卡那颗实读 `m_fontSizeMin = 15` · `m_fontSizeMax = 38` —— **两处都对得上**：
        /// `Missions Tab` 页内实例 与 独立预制体 `Daily Skulls Mission Container Small` 都是 `15 / 38`）。
        /// `m_fontSizeMax`(38) ≠ 设计字号(30.15) ⇒ 必须走 `Txt` 的 `autoMaxPx`（`TextBox` 的上界写死成 `fontPx`）。</summary>
        void BuildClockRow(Transform parent, PxRect r, float clockPx, string text, float fontPx,
                           float autoMinPx, float autoMaxPx)
        {
            float cy = r.CY;
            float cx1 = r.x1;
            Draw(parent, "WF_icon_clock",
                 new PxRect(cx1, cy - clockPx * 0.5f, cx1 + clockPx, cy + clockPx * 0.5f), "clock",
                 RewardsWindow.QContent);
            // ⚠️ 文字用 `TextBox`（限宽 + 自适应）—— 原版 `TimerHolder` 只有 157.9px 宽，
            //    而 'Resets in 12h 34 m'（fs30.15）≈250px ⇒ 不限宽就会**压到左边的计数格上**
            //    （2026-09-23 并排看图发现；断言量的是矩形，量不到字溢出）。
            var box = new PxRect(cx1 + clockPx + 6f, r.y1, r.x2, r.y2);
            Txt(parent, box, text, new Color(0.57f, 0.57f, 0.59f, 1f), "Timer", fontPx, autoMinPx, autoMaxPx);
        }
    }
}
