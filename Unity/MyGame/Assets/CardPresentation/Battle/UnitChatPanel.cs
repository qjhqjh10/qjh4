// UnitChatPanel.cs — 原版**单位语音条**（`Unit Chat/PlayerChatDisplay` · `EnemyChatDisplay`）
//
// 规格出处（2026-09-17 复核过原始 JSON，**并更正了权威表的两处标错**）：
//   父链 `FrontCanvas/Safe area FrontCanvas/Unit Chat`(MB `UnitsVoiceLinesPanel`)
//     ├ PlayerChatDisplay  x[12.61,661.38] y[643.50,880.00]   648.77×236.50（贴屏幕左侧、**我方的在下**）
//     └ EnemyChatDisplay   x[12.61,661.38] y[173.50,410.00]   同尺寸（**敌方的在上**）
//   一个气泡里的四层（相对气泡左上角，px）：
//     · Background  铺满气泡         图 `40k_voicelines_radio`（766×280，`m_Type=0` 纯拉伸）
//     · 卡图        中心 (104.5,113.15) 尺寸 144.46×157.46
//                   —— 原版是 `Mask`(RectMask2D) + `CardImage` 176×176（**画得比遮罩大、被裁**），
//                      ⚠️ 我们**不裁**，直接画成遮罩那个尺寸（差别只在边缘，如实记着）
//     · wave        中心 (435.5,68.65) 尺寸 387.95×62.60 · 色 (0.6988,0.9811,0.9265)
//                   图 `40k_voicelines_radio_wave equalizer`（708×96，**非等比拉伸**）
//     · ChatText    区域 (188.5,93.7)–(631.6,175.4) · 原版 TMP **fontSize 33、纯白**
//                   ⚠️ 2026-09-25 补全：这层原版是 `m_TextWrappingMode=1` + `m_enableAutoSizing=1`（**10~33**）
//                   + `H=1 (Left)`；**框 440.9×81.7 px**。我们原来只给了「一个点」+ 全工程默认的 `NoWrap`
//                   ⇒ 61% 的台词出框（`项目任务.md` §三 第 12 条 第 2 项）。逐值见 `TextW/TextH` 的注释。
//
// 🔴 **原版那层「波形」是一个频谱等化器**（`AudioWaveEqualizer`：`spectrumModifier=15` /
//    `speedChange=10`，每帧用 `GetSpectrumData` 扰动材质）。⚠️ **2026-09-17 二次核查更正**：
//    那个 shader **在本地**（`d:/2/解包整理/11_着色器/shaders/Shader_2656665607827278157.json`），
//    但 **`m_Name` 是空的、也没有可读的字节码** ⇒ 只能读到它的**属性表**（7 个：
//    `_MainTex` / `_Noise` / `_XOffsets` / `_MovementRange` / `_WaveScaleRemap` /
//    `_Noise1ScaleAndSpeed` / `_Noise2ScaleAndSpeed`），**运算逻辑拿不到**。
//    ⚠️ 场景那个材质上还挂着一堆 `_WaveScale1/2/3` / `_Movement` / `_Resolution` / `_Parallax` ——
//    **那些是内置 Standard shader 的残留值、当前 shader 根本不读**（`CLAUDE.md` 那条「原版材质带残留」的坑）。
//    ⇒ 🔴 **2026-10-21 就地订正（铁律 5）**：本段原来写「**我们这里画的是静态图**」—— **已过期**。
//    现在有一个**等价近似**的自写 shader `Resources/VoiceWave.shader`（UV 空间 sin 位移采样 y），
//    幅度由 C# 端按原版公式（前 16 bin ×15 → clamp01 → 10×dt 平滑）写 **原版属性名 `_MovementRange`**。
//    仍然如实标着的差异：**位移的 UV 量纲是我们的**（`_WaveScale=0.22`），原版
//    `Shader Graphs/Equalizer` 的数学拿不到 ⇒ 形状不保证逐像素一致。
//
// 🔴🔴 **2026-10-16（修波 `FD` · 账 `G1-4`）就地订正（铁律 5）**：本段原来那句「**查不到的（所以是我们挑的）**」
//    下面两句**今天都不成立**（这是同一句错话的**第 5 份**，也是**最后一份**没有更正痕的）：
//    · ❌ 原句「原版**什么时机播哪一条**没有任何代码或数据（对账表 §三·〇 :127）」——
//      ① **判据有**（「查不到」已被推翻）：`RawCardScript__GetChatSound.c` 的 `switch(param_2)` 把
//         **枚举号 → 字段偏移**钉死了（`2→hurryChatSound` @`0x1A8` · `3→iCantDoThat` @`0x1B0` ·
//         `5…10→greet/threat/wellPlayed/gen1/gen2/gen3` …，取不到落 `defaultChatSound` @`0x188`），
//         调用点**两处独立证实**（`VoiceLinesPopupSelector__SetupChatOptions.c:54,112` / `__RefreshChatLines.c:71`）；
//      ② **行号也错**：真落点是 `资料/战斗UI_原版对账表.md:**112**`
//         （`§三·〇 :127` 是**「回放条」**那条的判据指引行）—— `G4`/`G1` 各自现核过、逐字一致。
//         全过程 → `资料/语音线_原版规格与ASR管道.md` · `语音线A1768_报告.md`。
//    · ❌ 原句「**「事件 → 后缀」的对应是我们定的**（见 `VoiceLines.ForDeploy`）」—— **不成立**：
//      文件名只给**后缀集合**，「**哪个事件取哪一条**」是**原版真值**（见上 ①），不是我们定的。
//    ✅ **仍然成立的那一半（是真话，别一起删）** = **下面紧接着那两行「文本层」**：
//      原版**文件名只给后缀集合、给不了台词文本** ⇒ 「显示文件名里的台词 / 只剩事件后缀时显示卡名」
//      **是【我们挑的】**（如实标着，不是判据）。原文保留在下面。
//    🔴🔴 **2026-10-19（战斗线 `T17`）就地订正（铁律 5 · 账 `A1754`/`A1805`）：下面那两行「文本层」【已过期】。**
//      · 原文：「普通单位那条台词原文**就在文件名里**……督军那种只有事件后缀的**没有文本可用**」
//        —— **不成立**：原版**显示的文字**来自官方 I2 词条 `Chat_Messages/*`（2026-10-14 已解出、在本地表里，
//        中英两列都有），出处 = `RawCardScript__GetLocalizedSummonChat.c`（`Chat_Messages/<卡id>`）与
//        `RawCardScript__GetLocalizedChatMsg.c`（`Chat_Messages/<战将id>-Chat<枚举号>`）。
//      · 现在的口径：**官方词条优先**（`Speak` 里 `ChatTermKey` → `Loc.T`），`voice_lines.json` 里
//        「从音频文件名抠的英文」**降为兜底**（官方表缺键时用，⛔ 不自译）；两者都没有才显示卡名。
//      · 下面那段原文**留着当留痕**（它描述的兜底形状今天仍然存在，只是不再是第一来源）。
//    文本层：普通单位那条台词原文**就在文件名里**（`VO_…_<卡名> - <台词> (配音演员).ogg`），
//    我们把它显示出来；督军那种只有事件后缀的（`…_attack.ogg`）**没有文本可用** ⇒ 退而显示**卡名**（我们的选择）。
// ═════════════════════════════════════════════════════════════════════════════
//  🔴 **2026-10-16（`T09` 批 3 · `OverHand` 组）：三颗气泡的图与字整批从【世界空间 quad】迁到【uGUI 子画布】**
//  · **子画布** = `SubCanvasName`（= 原版 GO 名 + `" Canvas"`）+ `HudCanvas.SortingLayer.OverHand`
//    + order `0` —— 层/order **照原版那一行**：`资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一
//    我这一行 = `Unit Chat` @ `BattleHud/Canvas/FrontCanvas/Safe area FrontCanvas`，
//    `(rm 继承 FrontCanvas(1), ovr 继承 true, OverHand, 0, 兄弟序 **2/14**)`，出处 `RT_3280`（`battlearena2`）。
//  · **画布上那一级** = 本节点自己（原版 `Unit Chat` 的 RT **逐字段**：`anchor (0,0)-(1,1)` · `pivot (.5,.5)` ·
//    `sd (0,0)` ⇒ **铺满父件**）⇒ uGUI 档用 `HudCanvas.FillParent`（⛔ **不是** `MenuDraw.SetPxSize(1920,1080)`
//    —— 那个体内是 `LayoutSpace.Px` = **÷108**，会把整块缩成 1/108，而画面只是「小得看不见」、不报错）。
//  · 🔴 **双模式**（与 `ErrorMessageBanner.Create(..., uguiHost)` / `Tooltip.UseHost(host)` 同一套口径）：
//    `Create(Transform, Transform)` 的 `uguiHost == null` ⇒ **世界档**（与改本件之前**逐位相同**：
//    位置走 `LayoutSpace.ToWorld`、尺寸 ÷108、字走 `Label.Create`）；传了宿主（= 由调用方
//    `HudCanvas.EnsureSubCanvas(null, SubCanvasName, OverHand, 0)` 建/取的那块子画布）⇒ 才走画布。
//    ⛔ **不许两边都建**（半迁移态在 16:9 下看不出来、别的宽高比下两者分家 —— `T09` §五·补⑦）。
//  · 🔑 **单位**：画布那一侧「1 单位 = 1 设计 px」（`HudCanvas` 文件头那条推导）⇒ 面板内坐标 =
//    **绝对设计 px、左上原点 y 向下**（`Move` 收的那两个数）；尺寸**直接吃 px**。
//    ⛔ 世界单位喂进建件口会**小 108 倍、且不报错**。
//  · ✅ **锚点已照原版逐颗补正（2026-10-16 · `T09` 批 3 补正 `A12`）**：三颗气泡（+ 面板内四个子件）
//    现在**锚在画布的那个角/边上**，与原版 `m_AnchorMin/Max` 一致 —— 见 `Slot` 的注释（逐值判据）。
//    🔴 **原版的锚点真值**（2026-10-16 现读 `battlearena2`，`RectTransform/RectTransform_{3231,3375,3439}.json`）：
//    `PlayerChatDisplay` `aMin=aMax (0,0)` + `pivot (0,0)` + `ap (12.61328125, 200)` ·
//    `EnemyChatDisplay` `aMin=aMax (0,1)` + `pivot (0,0)` + `ap (12.61328125, −410)` ·
//    `RadioChat` `aMin=aMax (1,1)` + `pivot (1,1)` + `ap (−25, −126)`（三颗 `sd = 648.774 × 236.505`）。
//    ⇒ 原版是「**贴着各自那个屏幕角、按固定 px 内缩**」；**父件一拉伸（4:3 / 21:9）它就跟着那个角走**。
//    ⚠️ 改本件之前是「绝对设计 px + **中心锚**」（= 贴着**画布中心**偏移）⇒ 4:3 下三颗气泡上下沿各差
//    **180 画布 px**（135 屏 px）、21:9 下 x 一律差 **320 画布 px**（16:9 逐位相同）。
//    ✅ 现在按 `Slot.ax/ay` 逐颗锚上（`Move` 是**唯一**的摆位处）；⚠️ **世界档一并改**（它那套也是错的：
//    位置按「设计帧拉伸」摆，同样偏 180/320 画布 px）—— 两档现在都按「锚点 + 该角偏移」摆，
//    **16:9 下与改之前逐位相同**（算式见本报告 §④）。
//  · ⚠️ **仍然如实记着的差异（原版 vs 我们）**：面板内**子件**（`Mask`/`Background`/`wave`/`ChatText`）
//    在原版里是**气泡的子件**、锚在**气泡**上（`Mask`/`wave` 是气泡中心锚 `ap (−219.895, 5.1)` / `(111.11, 49.6)`、
//    `Background` 是气泡左下角锚 `ap (324.387, 118.252)`、`ChatText` 是双轴拉伸 `aMin (0.29062,0.258234)`
//    `aMax (0.97362,0.603646)`）；我们这边它们是**气泡的兄弟**（都挂在面板节点下）。
//    ⇒ 本件用的等效写法是「**锚在【气泡那一颗的角】上、偏移 = 气泡内偏移**」—— 因为**气泡不会改变尺寸**
//    （`sd` 固定），两者的矩形**逐位相同**（推导见 `HudCanvas.AnchorApPx` 的 doc）。⛔ 没顺手把嵌套倒过来
//    （`T09` §五·补⑩ 二那条裁定：「只按谁的 canvas 装谁迁、⛔ 不倒嵌套」）。
//  · 🔴 **本面板【没有】可点件**（全仓 `UnitChatPanel` 上没有 `Contains` / `Hit` / `*Hit`）⇒ 不牵动
//    `T09` 第 4 步（输入），也**没有 `Hit` 判据要退**（对照：`ChatPopupPanel` 那 6 颗钮有）。
//    🔴🆕 **2026-10-20（W57 · `T09` A3-e2）就地订正（铁律 5）**：本行原写「**没有 `raycastTarget` 要翻**」——
//    **不成立**。2026-10-20 现读 `bundle_scenes_scenes_battlearena1` 的 `Unit Chat` 整棵子树：
//      · 三颗气泡（`PlayerChatDisplay` / `EnemyChatDisplay` / `RadioChat`）**都没有** `Button` /
//        `EventTrigger` / `IPointer*` 处理器；气泡根上的 `VoiceLineBox` 也**不含任何指针接口**
//        （`d:/2/tools/decomp_full/` 里 `VoiceLineBox*` 零 `Pointer/Click/Drag` 文件）⇒ **确实没有点击动作**。
//      · 但原版这些**图与字本身是射线靶**：`Background` 的 `Image.m_RaycastTarget = 1` ·
//        `Mask/CardImage` 的 `Image.m_RaycastTarget = 1` · `wave` 的 `Image.m_RaycastTarget = **0**` ·
//        `ChatText` 的 `TextMeshProUGUI.m_RaycastTarget = 1`（三颗气泡逐值同）。
//        ⇒ 原版「**挡射线但不响应**」（点气泡什么都不发生，也不会落到它后面的东西上）。
//    ⇒ 本波照原版把这三层的 `raycastTarget` 补上（**只补挡射线、不挂任何处理器**），判据 → `MakeBubble` 那段。
//    ⚠️ `Unit Chat` 同一份 prefab 里还有 `ChatPopup`（`CloseChatPopup` + 6 颗 `ChatButton`）——
//    那是 **`ChatPopupPanel.cs` 的范围**（另一份文件、另一批任务），**本文件一个字没碰**。
// ═════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    public class UnitChatPanel : MonoBehaviour
    {
        // ---- 原版实测（1920×1080，y 从上算）----
        // 🔴 **2026-10-16（`G5` 收口波 · 消重）**：这里原来有一对 `const float RefW = 1920f, RefH = 1080f;`
        //   —— **已删**（现核：全文件只有那个私有的 `ApPx` 用它们，而 `ApPx` 已改走共用口）。
        //   要用帧尺寸一律取 `HudCanvas.ReferenceResolution`（同一对 `(1920, 1080)`）
        //   —— ⛔ 别在本文件再写一遍那个字面量（本仓铁律：同一个数只留一处）。
        const float BubbleW = 648.77f, BubbleH = 236.50f;
        const float PortraitW = 144.46f, PortraitH = 157.46f, PortraitCx = 104.5f, PortraitCy = 113.15f;
        const float WaveW = 387.95f, WaveH = 62.60f, WaveCx = 435.5f, WaveCy = 68.65f;
        const float TextLeft = 188.5f, TextCy = 134.55f;    // 文本区左沿 / 中线（相对气泡左上角）
        static readonly Color WaveTint = new Color(0.6988f, 0.9811f, 0.9265f);
        static readonly Color TextColor = Color.white;
        /// <summary>原版 `ChatText` 的 TMP 字号（em）。英文文案按**拉丁大写高度** ≈ 0.72 em 折算。</summary>
        const float OrigFontPx = 33f;

        // ---- 原版 `ChatText` 那个**文本框**（🔴 2026-09-25 补：以前只给了「一个点」，没有框）----
        // 出处：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_3894.json`
        //   （ChatText 的 TMP 组件；RT 3566 / GO 306）+ 权威表 `资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md:261`。
        //   · RT 锚点 anchor(0.2906,0.2582)-(0.9736,0.6036)、**绝对矩形 x[201.2,642.1] y[738.2,819.9]**
        //     ⇒ **440.9 × 81.7 px**（相对气泡左上角：左沿 188.59、中线 135.55）。
        //   · `m_TextWrappingMode = 1`（自动换行）· `m_enableAutoSizing = 1` · `m_fontSizeMin = 10` · `m_fontSizeMax = 33`
        //   · `m_HorizontalAlignment = 1 (Left)` · `m_VerticalAlignment = 512 (Middle)`
        //   · `m_fontSize = 33`（= `m_fontSizeMax`，即**一行放得下时就是 33**）
        // 🔴 **不照抄它的后果（2026-09-22 用户看实战指出）**：我们建文本时只给了**一个点**、
        //    而 `Label` 给 HUD 单行标签硬写了 `NoWrap` ⇒ 实测 491 条台词里 **298 条（61%）画出框外**，
        //    最长那条超出 ≈648 px（框宽的 1.47 倍）。见 `项目任务.md` §三 第 12 条 第 2 项。
        const float TextW = 440.9f, TextH = 81.7f;
        const float TextMinPx = 10f;

        /// <summary>HUD 图那一层的 z（和 `ReplayBar` / `BattleDriver.HudImageZ` 一致）。
        /// ⚠️ **同一 z 的透明 quad 谁压谁是不确定的**（`BattleDriver.HudDecorZ` 那段注释踩过）
        /// ⇒ 这里**按原版的兄弟序显式分层**：卡图 → 气泡底 → 波形 → 文字（z 越大越远）。</summary>
        const float Z = 0.25f;
        const float ZPortrait = Z + 0.03f;      // 原版 `Mask`(卡图) 在最底下，被气泡底那层半透屏盖着
        const float ZBg = Z;
        const float ZWave = Z - 0.01f;
        const float ZText = Z - 0.02f;

        // ── 🆕 2026-10-16（`T09` 批 3 · `OverHand` 组）：本面板的【渲染空间】--------------------------
        /// <summary>🔴 本面板那块 **uGUI 子画布**的名字（= 原版宿主 GO 名 + `" Canvas"`，见 `T09` §五·补⑩ 第 1 步）。
        /// 判据 = `T09c` §一 我这一行（`Unit Chat` @ `…/Safe area FrontCanvas`，`OverHand` / order 0 / 兄弟序 2/14）。
        /// ⛔ **只此一份** —— 调用方建它（`HudCanvas.EnsureSubCanvas(null, SubCanvasName, OverHand, 0)`）也要取这里。</summary>
        public const string SubCanvasName = "Unit Chat Canvas";
        /// <summary>本面板那块子画布的**排序层**（判据同 <see cref="SubCanvasName"/>）。</summary>
        public const int SortingLayerId = HudCanvas.SortingLayer.OverHand;
        /// <summary>本面板那块子画布的 `sortingOrder`（判据同 <see cref="SubCanvasName"/>）。</summary>
        public const int SortingOrder = 0;

        /// <summary>🔴 **本件的图与字现在活在哪个空间**：`false` = **世界空间 quad**（出厂档，与加本档之前
        /// **逐位相同**）· `true` = **uGUI 画布**。由 <see cref="Create(Transform, Transform)"/> 按
        /// 「调用方有没有给宿主」定，⛔ 出厂**默认世界档**（`BattleDriver` 那一处不传 ⇒ 行为一个字不变）。</summary>
        bool _ugui;
        /// <summary>uGUI 档在不在（诊断 / 断言 / 自检用）。</summary>
        public bool IsUgui { get { return _ugui; } }

        /// <summary>px → **本档的单位**（世界档 = 世界单位 ÷108 · uGUI 档 = 设计 px ×1）。
        /// ⛔ 只给「按 px 给字号/框」的口用（`Label.SetGlyphHeight` / `SetAutoFitBox` 那两个包装）。
        /// 尺寸口**别**经过它 —— 世界档那几条 `hPx / 108f` 的式子**原样留着**（逐位不变）。</summary>
        float U(float px) { return _ugui ? px : px / 108f; }

        class Bubble
        {
            /// <summary>🔴 **2026-10-16（`T09` 批 3）：类型 `ImageQuad` → `HudPiece`** —— 这一族现在是
            /// 「世界空间 quad **或** uGUI `RectTransform+Image`」二选一（见 `HudPiece` 文件头那条铁律）。</summary>
            public HudPiece bg, portrait, wave;
            /// <summary>音波材质（`CardPresentation/VoiceWave`）—— `Advance` 每帧写 `_MovementRange`（原版属性名）。</summary>
            public Material waveMat;
            public Label text;
            public AudioSource audio;
            public float left;
            public bool Visible { get { return bg != null && bg.gameObject.activeSelf; } }
            public void SetActive(bool on)
            {
                if (bg != null) bg.gameObject.SetActive(on);
                if (portrait != null) portrait.gameObject.SetActive(on);
                if (wave != null) wave.gameObject.SetActive(on);
                if (text != null) text.gameObject.SetActive(on);
            }
        }

        Bubble _player, _enemy;
        /// <summary>🆕 2026-10-18（`A940` 尾账 · 聊天五档）：**第三颗气泡 `RadioChat`**
        /// （原版 `UnitsVoiceLinesPanel/Unit Chat/RadioChat`）。
        /// 🔴 **判据 = 序号 2**：`VoiceLinesController__ShowRadioMessage.c` →
        ///    `UnitsVoiceLinesPanel__ShowChatBox(panel, **2**, data)`；而
        ///    `UnitsVoiceLinesPanel__GetChatBox.c` 里 `param_2 == 0 ⇒ +0x20` / `== 1 ⇒ +0x28` / `== 2 ⇒ +0x30`
        ///    ⇒ 三颗气泡按 0/1/2 编号。另两颗 = `DisplayChatBox` 的 `card+0x40`（谁说的）⇒ 0/1。
        /// 位置：运行时 dump 实读 `…/Unit Chat	RadioChat	RT	-25.0,-126.0	648.8,236.5	anchor 1,1 · pivot 1,1`
        ///    （`资料/原版参照图/Unity参照管线_0825/data/panel_0914/runtime_ui_dump_drive.tsv:430`）⇒
        ///    **右上角贴着屏幕右边 25 px、顶上往下 126 px**（与另两颗都在左侧不同 —— 它是「电台」）。
        ///    ⚠️ 场景树里那一份 `[-674,1206 649x237]` 是**布局前**的序列化位（`(inactive)`），别拿它当位置。</summary>
        Bubble _radio;
        float _left;                 // 还剩多少秒（三边的气泡共用一个倒计时 —— 原版是三条独立的，
                                     // 但我们同一时刻只会显示一条：对手说话时我没在说）
        Bubble _current;

        /// <summary>一颗气泡的**左上角**（屏幕 px · y 自顶）—— `left` 自左缘、`top` 自上缘。
        /// ⚠️ 三颗的子件（`Mask`/`Background`/`wave`/`ChatText`）在原版里 `anchoredPosition` **逐字相同**
        ///    （`324.4,118.3` / `111.1,49.6` / `-219.9,5.1` / `-2.5,-5.0`），而 uGUI 的 `anchor (0,0)`
        ///    指的是**父矩形左下角**（与父的 pivot 无关）⇒ 三颗**共用同一套相对常量**，只是整体平移。
        ///    ⇒ `MakeBubble` 收一个 `left/top` 就够了，别为哪一颗另立一套。
        /// <para>🔴 **2026-10-16（`T09` 批 3 补正 `A12`）：`ax/ay` = 原版那一颗的【点锚】**
        /// （`m_AnchorMin == m_AnchorMax`，逐值现读 `battlearena2/RectTransform/RectTransform_{3231,3375,3439}.json`）：
        /// 点锚只管「尺寸固定」，**位置由「锚在父件的哪个角/边」定** —— 锚在角上则父件一拉伸它就跟着那个角走
        /// （原版真值 → 文件头那段）。⛔ **别一律用「中心锚 + 固定 px」**（那等价于锚在画布中心，
        /// 4:3 下三颗上下沿各差 180 画布 px、21:9 下 x 差 320 画布 px）。
        /// ⚠️ 原版三颗的 `pivot` 分别是 `(0,0)` / `(0,0)` / `(1,1)`（= 各自那个角）；
        /// 我们这边四个子件**都按自己的枢轴点摆**（`HudCanvas.AnchorApPx` 与 pivot 无关），**枢轴取建件口那个**
        /// （图 = 中心、字 = `Label` 固定中心）⇒ 同一份 `(left, top, ax, ay)` 两档通用。</para></summary>
        struct Slot { public float left, top, ax, ay; }
        static readonly Slot SlotPlayer = new Slot { left = 12.61f, top = 643.50f, ax = 0f, ay = 0f };
        static readonly Slot SlotEnemy  = new Slot { left = 12.61f, top = 173.50f, ax = 0f, ay = 1f };
        /// <summary>🆕 `RadioChat`：右上角贴屏（出处 = `RadioChat` 那一行的运行时 dump，见 `_radio` 的注释）。
        /// `left = (1920 − 25) − 648.8 = 1246.2`、`top = 126`；**锚点在右上角 `(1,1)`**（原版 `pivot` 也是 `(1,1)`）。
        /// ⚠️ 运行时 dump 给的是**左上角坐标**（`-25.0,-126.0` 那一栏是 `m_AnchoredPosition`，
        /// 逐位与 `RectTransform_3439.json` 的 `(−25, −126)` 一致 ⇒ 同一份判据）。</summary>
        static readonly Slot SlotRadio  = new Slot { left = 1246.2f, top = 126.0f, ax = 1f, ay = 1f };
        // 🆕 **2026-10-20（W58 · `T09` C3）**：上面这三行 + 那组 `BubbleW/H` / `Portrait*` / `Wave*` / `Text*`
        //   常量 = **画布档的全部几何**（点锚 + 常量 `anchoredPosition`）。⇒ `RefreshLayout()` 的画布支
        //   已早退（见它自己的 doc）；世界档仍要重摆。⛔ 别把「分辨率相关」的算式加回 `Move` 的画布支。

        public bool Ready { get { return _player != null && _player.bg != null; } }
        /// <summary>自检用：现在亮着的是哪一侧（`-1` = 都没显示；`2` = `RadioChat`）。
        /// ⚠️ 顺序固定 0 → 1 → 2（同一时刻只有一颗亮着，见 `Speak` 开头的 `HideAll()`）。</summary>
        public int ShownSide
        {
            get
            {
                if (_player != null && _player.Visible) return 0;
                if (_enemy != null && _enemy.Visible) return 1;
                if (_radio != null && _radio.Visible) return 2;
                return -1;
            }
        }
        /// <summary>自检用：`RadioChat` 那一颗建出来了没有（原版那颗在场景里是 `(inactive)`）。</summary>
        public bool HasRadio { get { return _radio != null && _radio.bg != null; } }
        /// <summary>自检用：条上写的字</summary>
        public string ShownText
        {
            get { return _current != null && _current.text != null ? _current.text.Text : "<无>"; }
        }
        /// <summary>自检用：现在这条台词那块**文字本体**。
        /// 🔴 断言必须比**渲染真值**（`WorldW`/`WorldH`/`FontPxNow`/`LineCount`），
        ///    不能拿我们自己的常量自证 —— `CLAUDE.md` §二 `AutoFitBox` 那条教训。</summary>
        public Label TextLabel { get { return _current != null ? _current.text : null; } }
        /// <summary>自检用：这一条是谁的（卡 id / 事件 / 音频名）</summary>
        public string LastCardId { get; private set; }
        public string LastEvent { get; private set; }
        public string LastClipName { get; private set; }
        /// <summary>自检用：音频**取到了没有**（批处理里放不出声，但能断言「挂上去了」）</summary>
        public bool LastClipLoaded { get; private set; }
        /// <summary>🆕 2026-10-19（`T17`）自检用：这一条台词用的**官方词条键**
        /// （`null` = 原版没有这一档 / 没有卡 id ⇒ 走的 voice_lines 兜底）。</summary>
        public string LastChatTerm { get; private set; }
        /// <summary>🆕 2026-10-19（`T17`）自检用：这一条台词的文本**是不是官方词条给的**
        /// （`false` = 官方表缺这条键 ⇒ 回 `voice_lines.json` 的英文兜底）。</summary>
        public bool LastChatFromOfficial { get; private set; }

        /// <summary>**现在有没有语音正在播** —— 原版 `VoiceLinesController.IsAnyVoiceLinePlaying()`。
        /// 🔴 **这是原版插播的闸门**：`BattleTipController.NotifyCantDoAction` 只在**没有语音在播**时才插播
        /// （见 `资料/语音线_原版规格与ASR管道.md` §1.3）⇒ **不打断正在说的那句**。
        /// ⚠️ 判据用 `audio.isPlaying` 而不是「气泡还在不在」—— 气泡有**最短停留 2 秒**，
        ///    比音频活得久（`GetCurrentClipLength = max(2, clip.length + δ)`）。</summary>
        public bool IsSpeaking
        {
            get
            {
                var c = _current;
                return c != null && c.audio != null && c.audio.isPlaying;
            }
        }
        /// <summary>自检用：还剩几秒</summary>
        public float TimeLeft { get { return _left; } }
        /// <summary>自检用：图都取到了没有（两张原版图任一缺失就报 false）</summary>
        public bool HasArt
        {
            get
            {
                return _player != null && _player.bg != null && _player.bg.Texture != null
                    && _player.wave != null && _player.wave.Texture != null
                    && _player.portrait != null;
            }
        }

        /// <summary>自检用：气泡的世界尺寸（原版 648.77×236.50 px）</summary>
        public Vector2 BubbleWorldSize
        {
            get
            {
                if (_player == null || _player.bg == null) return Vector2.zero;
                return new Vector2(_player.bg.WorldW, _player.bg.WorldH);
            }
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）：`BubbleWorldSize` 的【本档单位】版 —— 自检该用它。**
        /// <para>uGUI 档读 `sizeDelta`（= **设计 px**，与建件那一侧同一把尺）；世界档 = `WorldW/H × 108`
        /// （**与旧那条逐位相同**）。</para>
        /// <para>⛔ **别再用 `BubbleWorldSize × 108f` 去比字面 px** —— uGUI 档下画布从「设计 px」到「世界单位」
        /// 的系数**随屏变、而且两轴还能不同**（批处理实测 x 1/108 · y 1/144 ⇒ `×108` 读出的是假数，
        /// 而**画面其实是对的**；`T09` §七·8）。</para></summary>
        public Vector2 BubbleSizePxOf(int side)
        {
            var b = BubbleFor(side);
            if (b == null || b.bg == null) return Vector2.zero;
            if (_ugui) return HudCanvas.SizePx(b.bg.Rect);
            return new Vector2(b.bg.WorldW * 108f, b.bg.WorldH * 108f);
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）：某一颗气泡的【中心】读回成「本档的设计 px」（左上原点、y 向下）。**
        /// <para>uGUI 档走 `HudCanvas.CenterPx`（锚点 + `anchoredPosition` 那一份的逆 ——
        /// **它读的是【枢轴点】的位置**；本件的底图枢轴就是中心 ⇒ 读回的就是中心 ✓）；</para>
        /// <para>世界档走 `LayoutSpace.ToDesignPixel`（`FromPixel` 的**真逆**，见它那条 doc）。</para>
        /// <para>⚠️ 两档读的都是**真正摆在哪**，不是我们的常量 —— 断言的期望值取**原版 prefab 的矩形**（判据侧）。</para>
        /// <para>🔴 **2026-10-16（补正 `A12`）两档读回去的【帧】不同，非 16:9 下会分家**（**这是既有的差异、
        /// 本件没改**，如实记下免得下次当成回归）：uGUI 档的 `HudCanvas.CenterPx` 是**纯锚点逆**、
        /// 拿参考分辨率 `1920×1080` 算 ⇒ **与分辨率无关**（4:3 下也报 16:9 那一对数）；
        /// 世界档的 `ToDesignPixel` 用**实测** `VisibleWidth` ⇒ 报的是**真实落点**。
        /// 两档的**期望值来源（原版 prefab 那一对数）是一样的** ⇒ 断言按 uGUI 档那套写即可
        /// （见 `T09_UnitChat_报告.md` §③·3）；要在世界档上量「真落点」得走
        /// `BubbleCenterWorld` / 自己按 `LayoutSpace` 换算。</para>
        /// <para>那颗没建出来（或没建出底图）⇒ `(NaN, NaN)`（⛔ 不返 `(0,0)`：那与「真的在屏心」分不开）。</para></summary>
        public Vector2 BubbleCenterPx(int side)
        {
            var b = BubbleFor(side);
            if (b == null || b.bg == null) return new Vector2(float.NaN, float.NaN);
            if (_ugui) return HudCanvas.CenterPx(b.bg.Rect);
            return LayoutSpace.ToDesignPixel(b.bg.transform.position);
        }

        /// <summary>🔴 **自检用（`T09` 批 3 补正 `A12` 的【灭自证】口）**：某一颗气泡**实际锚在哪个角**
        /// （读的是那件底图的 `RectTransform.anchorMin`，⛔ 不是我们的 `Slot` 常量）。
        /// <para>期望值取**原版 prefab 的 `m_AnchorMin/Max`**（判据侧）：`0 → (0,0)` · `1 → (0,1)` · `2 → (1,1)`
        /// （出处 → `Slot` 的注释）。⚠️ 反面那条：**断言必须同时断「不是 `(0.5,0.5)`」**
        /// —— 否则「把锚点改回中心锚」这一档改坏了也不红（灭自证要求）。</para>
        /// <para>世界档（没有 `RectTransform` 锚点这回事）/ 那颗没建出来 ⇒ `(-1, -1)`（⛔ 不返 `(0,0)`：
        /// 那与真正的 `anchor (0,0)` 分不开，会让「世界档也答 (0,0)」这条假绿）。</para>
        /// <para>⚠️ 世界档的「锚」体现在 `localPosition` 上（`Move` 的世界那一支），当前**没有**读回口 ——
        /// 要断世界档的角锚得另开口（本轮没加，如实记在报告 §⑤）。</para></summary>
        public Vector2 BubbleAnchorOf(int side)
        {
            var b = BubbleFor(side);
            if (b == null || b.bg == null || !_ugui) return new Vector2(-1f, -1f);
            var rt = b.bg.Rect;
            if (rt == null) return new Vector2(-1f, -1f);
            return rt.anchorMin;       // `Move` 把 `anchorMin`/`anchorMax` 写成同一个点锚 ⇒ 读一个就够
        }

        /// <summary>🆕 **2026-10-20（W58 · `T09` C3）**：某一颗气泡底图的 `anchoredPosition`
        /// （= 「贴着那个角、按**固定 px** 内缩」的那一格；画布档的 `Move` 只写这一个位置量）。
        /// <para>它**不含画布尺寸** ⇒ 4:3 / 16:9 / 21:9 三档下**逐值相同**（断言 → `Editor/BattleScene.cs`
        /// 的 `T09-C3` 块）。期望值取**原版 prefab 的三元组**（`ap` + `sd` + `pivot`）换算出的中心偏移，
        /// ⛔ 别读 `Slot` / `BubbleW` 反推（那是被测实现自己的常量）。</para>
        /// <para>世界档（没有 `RectTransform` 锚点）/ 没建出来 ⇒ `(NaN, NaN)`（⛔ 不返 `(0,0)`：
        /// 那与「真的落在锚点原点」分不开）。</para></summary>
        public Vector2 BubbleAnchoredPosOf(int side)
        {
            var b = BubbleFor(side);
            if (b == null || b.bg == null || !_ugui || b.bg.Rect == null)
                return new Vector2(float.NaN, float.NaN);
            return b.bg.Rect.anchoredPosition;
        }

        /// <summary>🆕 **2026-10-20（W58 · `T09` C3）灭自证用**：把一颗气泡底图的 `anchoredPosition`
        /// 平移 <paramref name="delta"/>（px、y 向上）。调用方测完必须**平回去**。
        /// <para>用途：证明**画布档的 `RefreshLayout()` 不再写几何** —— 平移后调它一次，
        /// 位置必须还停在那儿；若它被改回「无条件 `Place(...)`」，这一刀会被写回常量。</para>
        /// <para>世界档（没有 `RectTransform`）/ 没建出来 ⇒ `false`（⛔ 不静默假装挪过）。</para></summary>
        public bool NudgeBubbleAnchoredPosForTest(int side, Vector2 delta)
        {
            var b = BubbleFor(side);
            if (b == null || b.bg == null || !_ugui || b.bg.Rect == null) return false;
            b.bg.Rect.anchoredPosition += delta;
            return true;
        }

#if UNITY_EDITOR
        // ── 🆕 2026-10-20（W57 · `T09` A3-e2）：uGUI 射线靶的 Editor-only 只读探针 ────────────────
        //  ⚠️ 只读、且**播放器构建里不存在**（`#if UNITY_EDITOR`）；断言写在 `Editor/BattleScene.cs`
        //     （本波口径 = 只做不验 ⇒ 本件不写断言）。
        //  ⚠️ 期望值（断言侧独立写死，⛔ 别读我们自己的常量）：
        //     底图 `RT=1` · 卡图 `RT=1` · 波形 `RT=0` · 台词 TMP `RT=1`（判据 → `MakeBubble` 那段）。

        /// <summary>某一颗气泡的某一件（`part`：**0=底图** · **1=卡图** · **2=波形**；其余值 ⇒ `null`）。
        /// 没有这一颗 / 这一件没建出来 ⇒ `null`。</summary>
        public HudPiece BubblePieceForTest(int side, int part)
        {
            var b = BubbleFor(side);
            if (b == null) return null;
            if (part == 0) return b.bg;
            if (part == 1) return b.portrait;
            if (part == 2) return b.wave;
            return null;
        }

        /// <summary>某一颗气泡某一件的 `Image.raycastTarget`：**−1 = 没有这件** · `0/1` = 实际值。
        /// <para>⛔ 别把「没有这件」折成 `false` —— 那与「建出来了但没开射线」分不开（灭自证要求）。</para></summary>
        public int BubbleRaycastTargetForTest(int side, int part)
        {
            var p = BubblePieceForTest(side, part);
            if (p == null || !p.IsUgui || p.gameObject == null) return -1;
            var img = p.gameObject.GetComponent<Image>();
            return img == null ? -1 : (img.raycastTarget ? 1 : 0);
        }

        /// <summary>某一颗气泡的台词标签（没有这一颗 / 没建出来 ⇒ `null`）。</summary>
        public Label BubbleTextForTest(int side)
        {
            var b = BubbleFor(side);
            return b != null ? b.text : null;
        }

        /// <summary>某一颗气泡台词的 TMP `raycastTarget`：**−1 = 没有 TMP** · `0/1` = 实际值。</summary>
        public int BubbleTextRaycastTargetForTest(int side)
        {
            var t = BubbleTextForTest(side);
            var tmp = t != null ? t.GetComponentInChildren<TMP_Text>(true) : null;
            return tmp == null ? -1 : (tmp.raycastTarget ? 1 : 0);
        }
#endif

        /// <summary>🔴 **自检用（`T09` 批 3 的【灭自证】）**：三颗气泡的**图与字是不是都建在 uGUI 画布上**
        /// （而不是有条漏网、退回世界空间 quad）。
        /// <para>它防的是本件**最坏的那一档**：一半在画布、一半在世界 —— 16:9 下逐位相同、全绿，
        /// 只有别的宽高比才露（`T09` §五·补⑦）。⛔ 别用「`HudPiece != null`」替它（那个两边都真）。</para>
        /// <para>任一件没建出来（贴图/字体缺失）⇒ 也返 `false`，如实回答「不成立」。</para></summary>
        public bool OnCanvas
        {
            get { return OnCanvasOf(_player) && OnCanvasOf(_enemy) && OnCanvasOf(_radio); }
        }
        static bool OnCanvasOf(Bubble b)
        {
            if (b == null) return false;
            return IsUguiOnCanvas(b.bg) && IsUguiOnCanvas(b.portrait) && IsUguiOnCanvas(b.wave)
                && b.text != null && b.text.IsUgui;
        }
        static bool IsUguiOnCanvas(HudPiece q)
        {
            return q != null && q.IsUgui && q.Rect != null && q.Rect.GetComponentInParent<Canvas>() != null;
        }

        public static UnitChatPanel Create(Transform parent)
        {
            return Create(parent, null);          // 不传宿主 ⇒ **世界档**（与加本档之前逐位相同）
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）**：`uguiHost` = **本面板那块子画布的 `RectTransform`**
        /// （由调用方建/取：`HudCanvas.EnsureSubCanvas(null, <see cref="SubCanvasName"/>,
        /// <see cref="SortingLayerId"/>, <see cref="SortingOrder"/>)`）。
        /// <para>`null` ⇒ **世界档**（= 改本件之前的行为，逐位相同）；非 null ⇒ **uGUI 档**。</para>
        /// <para>⛔ **宿主不是画布（或其祖先里有 Canvas）时出声并且【不建】** —— 半迁移态比不迁移更坏：
        /// 一张 `Image` 挂在没有 `Canvas` 的父链下**一个像素都不画**，而调用点会以为建成功了。</para></summary>
        public static UnitChatPanel Create(Transform parent, Transform uguiHost)
        {
            bool ugui = uguiHost != null;
            if (ugui && uguiHost.GetComponentInParent<Canvas>() == null)
            {
                Debug.LogError("[Battle] `UnitChatPanel.Create` 的 `uguiHost` 不在任何 `Canvas` 下"
                             + " ⇒ **本面板不建**（⛔ 不退回世界空间 quad —— 半迁移态在 16:9 下看不出来）");
                return null;
            }
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版同名件 `Unit Chat` 实读：`RectTransform` · `anchor (0,0)-(1,1)` ·
            //    `sizeDelta (0,0)` ⇒ **绝对矩形 (0,0)-(1920,1080)**（`bundle_scenes_scenes_battlearena1`，
            //    2026-10-11 现读；它挂 `UnitsVoiceLinesPanel`）⇒ 整屏矩形（两个气泡按绝对 px 摆在屏内）。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「单位语音条根 = 整屏矩形」红。
            // 🔴 **2026-10-16（`T09` 批 3）**：上面那条**是【世界档】的**（`MenuDraw.SetPxSize` 体内 `÷108`）；
            //    uGUI 档走 **`HudCanvas.FillParent`**（同一组锚点、但单位 = 设计 px）。
            var go = new GameObject("UnitChatPanel", typeof(RectTransform));
            if (ugui)
            {
                go.transform.SetParent(uguiHost, false);
                HudCanvas.FillParent((RectTransform)go.transform);
            }
            else
            {
                go.transform.SetParent(parent, false);
                MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            }
            var p = go.AddComponent<UnitChatPanel>();
            p._ugui = ugui;                        // ⚠️ 必须在 `Build()` **之前**赋值（`Build` 里按它算单位）
            p.Build();
            return p;
        }

        void Build()
        {
            // 我方在下（y 643.5 起）、敌方在上（y 173.5 起）—— 和原版一致
            _player = MakeBubble("PlayerChatDisplay", SlotPlayer);
            _enemy  = MakeBubble("EnemyChatDisplay",  SlotEnemy);
            // 🆕 2026-10-18：第三颗 —— `RadioChat`（右上角贴屏；原版场景里是 `(inactive)`，
            //   只有 `RadioMessage(90)` 那一档才亮）。出处见 `_radio` / `SlotRadio` 的注释。
            _radio  = MakeBubble("RadioChat",         SlotRadio);
            HideAll();
        }

        Bubble MakeBubble(string name, Slot at)
        {
            var b = new Bubble();
            b.left = at.left;
            float topPx = at.top;
            var radio = CardArt.Ui("40k_voicelines_radio");
            var waveTex = CardArt.Ui("40k_voicelines_radio_wave_equalizer");
            if (radio == null || waveTex == null)
                Debug.LogWarning($"[UnitChat] 语音条图没取到（radio={radio != null} wave={waveTex != null}）"
                               + " —— 跑 `工具/sync_battle_ui_art.py` 同步");

            b.bg = Quad(at, b.left + BubbleW * 0.5f, topPx + BubbleH * 0.5f, radio, BubbleH, "chat_bg_" + name, ZBg);
            // 卡图先拿一张**占位**建起来（原版是运行时 `set` sprite）—— 说话时再 `SetTexture` 换成那张卡的立绘
            b.portrait = Quad(at, b.left + PortraitCx, topPx + PortraitCy, CardArt.Solid(), PortraitH,
                              "chat_card_" + name, ZPortrait);
            // 🔴 **2026-10-21（第六十会话 · 真机缺陷：头像被底板挡住）**：立绘 PNG 的 alpha 是
            //    **角色抠图**（`art_*.png` 81% 透明），按默认 UI 材质画会把 81% 透出底板 ⇒ 看着像「头像被挡」。
            //    原版这一格挂 `Card Image Overflow` 材质（完整插图）；我们用等价的 `ArtOpaque`
            //    （只取 RGB、alpha 当 1，见 `CardView.OpaqueMaterial`）。每颗气泡一份材质（别共享改纹理）。
            if (b.portrait != null)
            {
                var sh = Shader.Find("CardPresentation/ArtOpaque");
                if (sh != null) b.portrait.SetMaterial(new Material(sh));
                else Debug.LogWarning("[UnitChat] 找不到 `CardPresentation/ArtOpaque` shader ⇒ 头像仍按抠图 alpha 画（会透底板）");
            }
            b.wave = Quad(at, b.left + WaveCx, topPx + WaveCy, waveTex, WaveH, "chat_wave_" + name, ZWave);
            if (b.wave != null) b.wave.SetTint(WaveTint);
            // 🔴 **2026-10-21（第六十会话）**：音波底图只是一条直线，动态来自原版 `AudioWaveEqualizer`；
            //    给它换 `CardPresentation/VoiceWave`（UV 位移），幅度由 `Advance` 按频谱写（0 = 直线）。
            if (b.wave != null)
            {
                var waveSh = Shader.Find("CardPresentation/VoiceWave");
                if (waveSh != null)
                {
                    b.waveMat = new Material(waveSh);
                    b.waveMat.mainTexture = waveTex;
                    b.waveMat.SetColor("_Color", Color.white);
                    b.wave.SetMaterial(b.waveMat);
                }
                else Debug.LogWarning("[UnitChat] 找不到 `CardPresentation/VoiceWave` shader ⇒ 音波仍是直线");
            }
            // 🔴 2026-10-20（W58 · `T09-C3` 生产修复）：**三片的「宽 = 高 × 原版比例」在建件处就写一次**。
            //   uGUI 档的 `Quad`（`HudCanvas.MakeImageInParent`）只按**贴图比例**给宽度
            //   （`wPx = hPx × tex.w / tex.h` ⇒ 气泡读回 646.996 px），而 `RefreshLayout` 的画布支
            //   按设计**早退**（锚点/偏移全是常量、与画布尺寸无关，见它的 doc）⇒ 原来唯一那句
            //   `SetAspect` 在 `Place()` 里，画布档**永远走不到** ⇒ 宽度停在贴图比例而不是原版
            //   `648.774 × 236.505`（差 1.778 px = 0.27%）。这里补一次与 `Place` **同源**的常量，
            //   幂等；世界档照旧由 `Place` 在每次 `RefreshLayout` 时写（值相同、逐位不变）。
            //   ⛔ 别改成撤掉 `RefreshLayout` 的早退 —— C3 的「幂等空转」两条断言（⑤⑥）要它不写几何。
            if (b.bg != null) b.bg.SetAspect(BubbleW / BubbleH);
            if (b.portrait != null) b.portrait.SetAspect(PortraitW / PortraitH);
            if (b.wave != null) b.wave.SetAspect(WaveW / WaveH);

            // 文本区原版是 anchor(0.29062,0.25823)-(0.97362,0.60365) 的**拉伸矩形**、
            // 对齐 `H=1(左) V=512(中)` ⇒ 我们按**左对齐**摆（轴心 x=0），y 取那条的中线。
            // 🔴 **2026-10-16（`T09` 批 3）**：`Label.Create`（世界空间）→ **`HudCanvas.MakeText`（画布）**。
            //   ⚠️ **单位是【设计 px】**：喂世界单位会**小 108 倍**、而画面只是「小得看不见」、不报错
            //     （`T09` §五·补⑨ 四）。`z` 那个形参就是旧那句「世界档设 `localPosition.z`」里那个 z 的落点
            //     （画布上 z **只用来排兄弟序**，写进 `localPosition` 会把件推离画布平面）。
            //   ⚠️ `MakeText` 拿不到 TMP 字体时**返回 `null`**（`Label.CreateUgui` 出声、⛔ 不退化成点阵）
            //     ⇒ 下面保留 `!= null` 空判。
            //   🔴 **2026-10-16（补正 `A12`）**：两档都先建在 `(0,0)`、再由 `Move` 按**本颗气泡的锚点**摆
            //     —— 摆位（含锚点）**只此一处**（⛔ 别在这里再写一遍坐标）。
            Label t;
            if (_ugui)
            {
                t = HudCanvas.MakeText(transform, "", 0f, 0f, 4, TextColor, new Vector2(0f, 0.5f),
                                       ZText, "chat_text_" + name, face: TmpFont.Face.LatinCardBody);
            }
            else
            {
                t = Label.Create(transform, "", Vector3.zero, 4, TextColor,
                                 new Vector2(0f, 0.5f), "chat_text_" + name);
            }
            // 🔴 2026-10-17（A1024③ 第 2 批）：原版 `Unit Chat/.../ChatText` = Pragati（两档都过这里；
            //    uGUI 档已在 `MakeText` 换过 ⇒ 同面早退）。必须在下面 `SetGlyphHeight` **之前**。
            if (t != null) t.SetFace(TmpFont.Face.LatinCardBody);
            Move(t, b.left + TextLeft, topPx + TextCy, ZText, at.ax, at.ay);
            // 原版是 TMP `fontSize 33`（**em 口径**）—— 见 `Speak` 里那段更正说明（别按大写折）。
            // 🔴 **2026-10-16（`T09` 批 3）**：走 `U()`（世界档 = `33/108` 世界单位、uGUI 档 = **33 设计 px**）。
            //   ⛔ 漏了它 ⇒ 画布上的字号只有 **0.3 px**、**一个字都不报**。
            if (t != null) t.SetGlyphHeight(U(OrigFontPx));
            b.text = t;

            // ── 🆕 2026-10-20（W57 · `T09` A3-e2）：uGUI 射线（原版真值，2026-10-20 现读 arena1）──────
            //  三颗气泡逐值同（`PlayerChatDisplay` / `EnemyChatDisplay` / `RadioChat`）：
            //   · `Background` 的 `Image.m_RaycastTarget = 1`
            //   · `Mask/CardImage` 的 `Image.m_RaycastTarget = 1`（原版那张卡图挂在 `RectMask2D` 下、被裁）
            //   · `wave` 的 `Image.m_RaycastTarget = **0**`（频谱条不吃射线 —— 我们保持出厂 false）
            //   · `ChatText` 的 `TextMeshProUGUI.m_RaycastTarget = 1`
            //  ⇒ 原版「**挡射线但不响应**」：整棵气泡子树没有 `Button` / `EventTrigger` / `IPointer*` 处理器
            //     （`VoiceLineBox` 也没有；判据 → 文件头「A3-e2 就地订正」那段 + 报告 §二）。
            //  ⚠️ 只对 uGUI 档生效：世界档没有 `raycastTarget` 这回事，且文件头的承诺是
            //     「世界档与加本档之前**逐位相同**」⇒ ⛔ 别在世界档上调（会连报四条 LogError）。
            if (_ugui)
            {
                if (b.bg != null && !b.bg.SetRaycastTarget(true))
                    Debug.LogWarning("[UnitChat] 气泡底图 `" + b.bg.name + "` 的 raycastTarget 没写进去（原版 = 1）");
                if (b.portrait != null && !b.portrait.SetRaycastTarget(true))
                    Debug.LogWarning("[UnitChat] 卡图 `" + b.portrait.name + "` 的 raycastTarget 没写进去（原版 = 1）");
                var chatTmp = b.text != null ? b.text.GetComponentInChildren<TMP_Text>(true) : null;
                if (chatTmp != null) chatTmp.raycastTarget = true;
                else Debug.LogWarning("[UnitChat] 台词 TMP 取不到 ⇒ 它不吃射线（原版 = 1；无处理器、无视觉差）");
            }

            b.audio = gameObject.AddComponent<AudioSource>();
            b.audio.playOnAwake = false;
            b.audio.spatialBlend = 0f;            // 2D（原版也挂在 UI 上）
            b.audio.volume = 0.85f;               // ⚠️ 这个系数是**我们自己定的**（原版逐 source 的音量没查到）
            // 🆕 2026-09-19：挂到 `Voices` 通道 ⇒ 受设置面板那根「语音」滑块控制（原版就是这么分的）
            // 🆕 2026-10-21（`新账-63` 诊断）：`WF_VOICE_NO_MIXER=1` 时**不挂 Voices 组**，
            //   用来判定「Play 后立刻 isPlaying=false」是不是 mixer 组造成的。生产默认不变。
            if (System.Environment.GetEnvironmentVariable("WF_VOICE_NO_MIXER") == "1")
                b.audio.outputAudioMixerGroup = null;
            else
                b.audio.outputAudioMixerGroup = WarpforgeAudio.VoicesGroup;
            return b;
        }

        /// <summary>一片图：位置 = 面板内**设计 px**（左上原点、y 向下）＝ 该件的**枢轴点**、
        /// `hPx` = 画出来的**高**、**宽由贴图比例来**（= 旧 `ImageQuad.Create(…, worldH, …)` 的语义；
        /// 要盖比例的建完自己调 `SetAspect` —— 本件那三片就是这么干的）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）双模式**：`_ugui` ⇒ `HudCanvas.MakeImageInParent`；否则与改本件之前
        /// 同样的建法（`ImageQuad.Create` + `÷108`）。**位置两档都交给 `Move`**（⛔ 不在这里写坐标 ——
        /// 摆位分支只此一处）。</para>
        /// <para>🔑 **为什么用 `MakeImageInParent` 而不是 `QuadFromTopPx`/`MakeImage`**：后两个是**归一化锚**
        /// （`x/1920`、`1 − y/1080`）—— **只有当 `parent` 本身是整屏矩形**时才等于「相对父中心」；
        /// 而本节点是 `FillParent`，`Expand` 档下（4:3 / 21:9）它比 1920×1080 高/宽 ⇒ 套那两个会把件
        /// **静默摆到别处**（`T09` §五·补⑩ §七·10；同批 `MulliganPanel.Img` 先例）。
        /// ⚠️ 建件口那个 `(0,0)` 是**占位**（`Move` 紧接着覆盖 `anchoredPosition`）。</para></summary>
        HudPiece Quad(Slot at, float xPx, float yPxTop, Texture2D tex, float hPx, string goName, float z)
        {
            if (tex == null) return null;
            HudPiece q;
            if (_ugui)
            {
                float wPx = hPx * ((float)tex.width / Mathf.Max(1, tex.height));
                q = HudCanvas.MakeImageInParent(transform, tex, goName, 0f, 0f, wPx, hPx, z, Color.white);
            }
            else
            {
                q = HudPiece.Of(ImageQuad.Create(transform, tex, Vector3.zero, hPx / 108f,
                                                 new Vector2(0.5f, 0.5f), goName));
            }
            Move(q, xPx, yPxTop, z, at.ax, at.ay);
            return q;
        }

        // 🔴 **2026-10-16（`G5` 收口波 · 消重）：本文件原来有个私有的 `static Vector2 ApPx(…)` —— 【已删】。**
        //   它的式子 `ap = (xPx − ax·1920, 1080·(1 − ay) − yPxTop)` **整条收进了共用口**
        //   `HudCanvas.AnchorApPx`（落盘那一半 = `HudCanvas.SetAnchorPx`，两处调用点已改走它）。
        //   ⛔ **别在本文件再写一遍这个算式**（本仓红线：两处写同一条规则 = 迟早不一致）。
        //   下面这条推导原样留着 —— 它解释的是**为什么**该这么算，不是第二份实现：
        //   · 锚点 `(ax, ay)` 指的是**父矩形**（= 画布）的那个角，在 1920×1080 帧里正好落在设计 px
        //     `(ax·1920, (1−ay)·1080)` ⇒「枢轴点落在设计 px `(xPx, yPxTop)`」⇔ `ap = 目标点 − 锚点` ✓（两轴都是减法）。
        //   · **与 pivot 无关** —— 式子只管「枢轴点摆在哪」，所以图（中心枢轴）与字（`Label` 的枢轴）
        //     **共用同一个 `(xPx, yPxTop, ax, ay)`**，两档两枢轴都通用。
        //   · 🔴 **为什么外层子件能挂在【气泡那一颗的角】上**：原版这四个子件是**气泡的子件**
        //     （锚在气泡上：中心锚 / 气泡左下角锚 / 双轴拉伸），而**气泡自己的矩形尺寸是固定的**
        //     （`sd` 不随拉伸变）⇒「锚在气泡上的固定偏移」==「锚在气泡那个角上的固定偏移」**逐位同义**。
        //     ⛔ 反过来说：**若原版那一级是【拉伸】的（尺寸会变），这条就不成立**（那要真嵌套或按比例算）。
        //   · ⚠️ `1080·(1 − ay)` 那一项：`ay = 1`（锚在顶边）⇒ 偏移自**上沿**算；`ay = 0` ⇒ 自**下沿**算。

        /// <summary>把一个**图**件摆到面板内**绝对设计 px**（左上原点、y 向下）＝ 该件的**枢轴点**。
        /// 🔴 **摆位（含锚点）的分支只此一处** —— ⛔ 别在 `Place`/`Quad` 里各写一遍
        /// （本仓红线：两处写同一条规则 = 迟早不一致）。
        /// <para>**uGUI 档**：走共用口 `HudCanvas.SetAnchorPx(rt, ax, ay, xPx, yPxTop)`
        /// （= 点锚 + `anchoredPosition = HudCanvas.AnchorApPx(…)`）
        /// （⛔ 不写 `localPosition`：锚点一旦不是父件中心，两者就**不再同值**；⛔ 也不写 `localPosition.z`
        /// —— 画布上写 z 会把件**推离画布平面**，层深度由建件口那个 `z` → 兄弟序承担）。
        /// 🔴 **2026-10-20（W58 · `T09` C3）：这一支【就是】「写死的归一化锚点」** ——
        /// `ax/ay` = 原版 `m_AnchorMin/Max`（`Slot` 里逐颗写死）、`xPx/yPxTop` 由
        /// `BubbleW/H` / `Portrait*` / `Wave*` / `Text*` 的常量算出 ⇒ `anchoredPosition` 是一个**常量**、
        /// **不读任何画布尺寸**。⇒ 换宽高比**不需要重排**（`RefreshLayout` 的画布支已早退），
        /// 4:3 / 16:9 / 21:9 三档 `CenterPx`/`SizePx` 逐值相同。</para>
        /// <para>**世界档**：`LayoutSpace.ToWorld(ax, ay)`（= 那个角在**可见区**里的世界坐标）
        /// `+ HudCanvas.AnchorApPx(…) × s`，`s = LayoutSpace.Px(LayoutSpace.Scale)` —— **这就是「贴着那个角、按固定 px 内缩」**
        /// 在世界空间里的写法（`Scale` = 窄屏系数，16:9 ⇒ 1 ⇒ 与改本件之前**逐位相同**）。
        /// 🔴 为什么乘 `Scale` 而不是写死 `1/108`：原版那一套的 px 是**画布 px**，画布 px→屏 px 的系数
        /// 是 `CanvasScaler` 的 `scaleFactor = min(W/1920, H/1080)`；而世界这边 `LayoutSpace.Scale`
        /// **恒等于它**（逐值推导 → `资料/普查产出_1016并行/T09_UnitChat_锚点与层序补正_报告.md` §④·三）——
        /// 不乘的话，4:3 下偏移会比原版**大 1.333 倍**（而 16:9 下看不出）。
        /// ⚠️ 世界档**尺寸**仍是老的 `÷108` 尺（`hPx/108`）—— 那是**整条世界路径**的尺
        /// （T09 要淘汰的那一档），**超出本件（锚点）范围**，如实记在报告 §⑤。</para></summary>
        void Move(HudPiece q, float xPx, float yPxTop, float z, float ax, float ay)
        {
            if (q == null) return;
            if (_ugui)
            {
                // 🔴 2026-10-16（`G5` 收口波 · 消重）：这一支改用共用口 —— 它体内就是
                //    「`anchorMin = anchorMax = (ax, ay)` + `anchoredPosition = AnchorApPx(…)`」，
                //    与本处原来那三句**逐位等价**（`q.Rect == null` 的空判它自带一个）。
                HudCanvas.SetAnchorPx(q.Rect, ax, ay, xPx, yPxTop);
                return;
            }
            var ap = HudCanvas.AnchorApPx(xPx, yPxTop, ax, ay);
            var o = LayoutSpace.ToWorld(ax, ay);
            float s = LayoutSpace.Px(LayoutSpace.Scale);
            q.transform.localPosition = new Vector3(o.x + ap.x * s, o.y + ap.y * s, z);
        }

        /// <summary>同上，给**字**那一档（`Label` 不是 `HudPiece`）—— 算式同走共用口
        /// `HudCanvas.AnchorApPx` / `SetAnchorPx`。⚠️ `Label` **没有 `Rect` 口**（2026-10-16 现核：
        /// `Label.cs` 里没有 `public RectTransform …` 这样的成员）⇒ 画布档自己 cast
        /// （`(RectTransform)t.transform`；`HudCanvas.MakeText` → `Label.CreateUgui` 建的本来就是 `RectTransform`）。</summary>
        void Move(Label t, float xPx, float yPxTop, float z, float ax, float ay)
        {
            if (t == null) return;
            if (_ugui)
            {
                HudCanvas.SetAnchorPx((RectTransform)t.transform, ax, ay, xPx, yPxTop);
                return;
            }
            var ap = HudCanvas.AnchorApPx(xPx, yPxTop, ax, ay);
            var o = LayoutSpace.ToWorld(ax, ay);
            float s = LayoutSpace.Px(LayoutSpace.Scale);
            t.transform.localPosition = new Vector3(o.x + ap.x * s, o.y + ap.y * s, z);
        }

        /// <summary>分辨率变了要不要重算 —— **画布档【不用】**（几何全是写死的归一化锚点）；
        /// 世界档仍按 `LayoutSpace` 的实测可见区摆 ⇒ 要重算。
        /// <para>🔴 **2026-10-20（W58 · `T09` C3）：画布档早退。** 理由 = `Move` 的画布支写的是
        /// 「点锚 `(ax, ay)` + `anchoredPosition = AnchorApPx(常量)`」——**一个画布尺寸都不读**
        /// （`ax/ay` 是原版 `m_AnchorMin/Max`，`ap` 全由 `Slot`/`BubbleW/H`/`Portrait*`/`Wave*` 的常量算出）
        /// ⇒ 4:3 / 16:9 / 21:9 下三颗气泡的 `CenterPx`/`SizePx` **逐值相同**（断言 → `Editor/BattleScene.cs`
        /// 的 `T09-C3` 块）。世界档的 `LayoutSpace.ToWorld(ax, ay)` 用的是**实测可见区** ⇒ 换分辨率必须重摆。</para>
        /// <para>⚠️ **调用点仍在 `BattleDriver.ReanchorHud`**（本批白名单外）⇒ 生产路径上它现在是**幂等空转**
        /// （画布档一行不改）。删那一句要等 `BattleDriver` 的下一波；在那之前**别把它改回「无条件重摆」**
        /// —— 那会让这件的几何来源又有两份（`Move` 的锚点 + 这里每次重算）。</para></summary>
        public void RefreshLayout()
        {
            if (_ugui) return;                 // 画布档：锚点常量 ⇒ 无分辨率相关项（见上面 doc）
            Place(_player, SlotPlayer, BubbleW, PortraitW, PortraitH);
            Place(_enemy,  SlotEnemy,  BubbleW, PortraitW, PortraitH);
            Place(_radio,  SlotRadio,  BubbleW, PortraitW, PortraitH);
        }

        void Place(Bubble b, Slot at, float bubbleW, float pw, float ph)
        {
            if (b == null) return;
            float left = at.left, topPx = at.top;
            Move(b.bg, left + bubbleW * 0.5f, topPx + BubbleH * 0.5f, ZBg, at.ax, at.ay);
            if (b.bg != null) b.bg.SetAspect(BubbleW / BubbleH);        // 原版 `m_Type=0` 纯拉伸
            Move(b.portrait, left + PortraitCx, topPx + PortraitCy, ZPortrait, at.ax, at.ay);
            if (b.portrait != null) b.portrait.SetAspect(pw / ph);
            Move(b.wave, left + WaveCx, topPx + WaveCy, ZWave, at.ax, at.ay);
            if (b.wave != null) b.wave.SetAspect(WaveW / WaveH);        // 图是 708×96 ⇒ 这里就是原版那次非等比拉伸
            Move(b.text, left + TextLeft, topPx + TextCy, ZText, at.ax, at.ay);
        }

        // ==================================================================
        //  🆕 **2026-10-19（战斗线 `T17` · 账 `A1754`/`A1805`）：台词文本的【官方来源】= `Chat_Messages/*`**
        //
        //  原版判据（`d:/2/tools/decomp_full/`，逐句实读）：
        //   · **出场/召唤**：`RawCardScript__GetLocalizedSummonChat.c` = `GetTranslation("Chat_Messages/" + <卡 id>)`
        //     （`param_1 + 0x38`；`+0x168` 非空时用它 —— 两个都是原版的 id 字段）。
        //   · **聊天（督军）**：`RawCardScript__GetLocalizedChatMsg.c` = `GetTranslation("Chat_Messages/" + <战将 id>
        //     + "-Chat" + 枚举号)`；枚举 `ChatMessage { Intro=0, MirrorMatch=1, Bored=2, ICantDoThat=3, Concede=4,
        //     Greet=5, Threat=6, WellPlayed=7, Taunt=8, Sorry=9, Oops=10 }`
        //     （`Warpforge_code/Scripts/Assembly-CSharp/ChatMessage.cs` 逐条照抄）。
        //     `VoiceLinesPopupSelector__SetupChatOptions.c:54,112` **写死起点 5、按按钮下标 +1**
        //     ⇒ 6 颗钮 = `-Chat5` … `-Chat10`（两处独立证实）。
        //   · 官方表 `Chat_Messages/*` **中英两列都在本地**（`数据/游戏数据/官方本地化_en+zh-CN.json`；
        //     2026-10-18 并入后现读 1,736 条）⇒ **不需要自译**（`A1754` 那句「要自译 1844 条」**已过期**）。
        //   · 取值走 `Loc.T`（三档：手写 `Table` → 数据面 bulk → 官方全表；缺键返回**键名本身** + 出声）。
        //  ⚠️ `voice_lines.json` 的英文（从音频文件名抠的）**保留当兜底** —— 那也是原版英文原文，不是我们译的；
        //     `attack` / `death` / 教程脚本那几档**原版没有聊天词条**（普通单位只有一条出场台词），
        //     ⇒ 那些事件仍走兜底（`ChatTermSuffix` 返回 `null`）。
        // ==================================================================

        /// <summary>官方聊天词条的键前缀（拼法逐字照 `RawCardScript.GetLocalizedSummonChat/GetLocalizedChatMsg`）。</summary>
        public const string ChatTermPrefix = "Chat_Messages/";

        /// <summary>事件名 → 原版 `ChatMessage` 枚举号的**键后缀**；`null` = 原版**没有**这一档聊天词条
        /// （`Attack` / `Death` / 教程脚本那几档 —— 原版只有「出场台词」与「督军聊天」两类）。
        /// <para>⚠️ 键名里的 `evt` 是本工程的字符串（调用点在 `BattleDriver`）：
        /// `Deploy`（出场）· `Intro`/`Concede`/`CantDo`/`Hurry`（督军那四档）· `Chat0…Chat5`（6 颗聊天钮）。</para></summary>
        public static string ChatTermSuffix(string evt)
        {
            if (string.IsNullOrEmpty(evt)) return null;
            switch (evt)
            {
                case "Deploy":  return "";        // 裸卡 id（原版 `GetLocalizedSummonChat`）
                case "Intro":   return "-Chat0";  // ChatMessage.Intro = 0
                case "Mirror":  return "-Chat1";  // ChatMessage.MirrorMatch = 1
                case "Hurry":   return "-Chat2";  // ChatMessage.Bored = 2
                case "CantDo":  return "-Chat3";  // ChatMessage.ICantDoThat = 3
                case "Concede": return "-Chat4";  // ChatMessage.Concede = 4
            }
            if (evt.StartsWith("Chat", System.StringComparison.Ordinal))
            {
                int idx;
                if (int.TryParse(evt.Substring(4), out idx) && idx >= 0 && idx < 6)
                    return "-Chat" + (5 + idx);   // Greet=5 … Oops=10（起点 5 写死，判据见上面那段）
            }
            return null;
        }

        /// <summary>这一句的**官方词条键**（`null` = 原版没有这一档 / 没有卡 id）。
        /// <paramref name="overrideSuffix"/> 非 null 时**压过**按事件推出来的后缀 —— 只给
        /// 「同一事件有两条原版键」那一档用（今天只有开场白：普通 `Intro` = `-Chat0`、同督军对局 `Mirror` = `-Chat1`）。</summary>
        public static string ChatTermKey(string cardId, string evt, string overrideSuffix = null)
        {
            if (string.IsNullOrEmpty(cardId)) return null;
            string suffix = overrideSuffix ?? ChatTermSuffix(evt);
            return suffix == null ? null : ChatTermPrefix + cardId + suffix;
        }

        /// <summary>取这一句的**官方文本**；取不到 ⇒ `null`（`term` 仍给出查询用的键）。
        /// <para>⚠️ **缺键不再喊第二遍**：`Loc.T` 自己会 `Debug.LogWarning`（按键去重）并返回键名本身 ⇒
        /// 本口只把「返回的是键名」翻译成 `null`，调用方回 `voice_lines` 兜底（⛔ 不自译）。</para></summary>
        public static string OfficialChatText(string cardId, string evt, string overrideSuffix, out string term)
        {
            term = ChatTermKey(cardId, evt, overrideSuffix);
            return OfficialChatTextOfTerm(term, out term);
        }

        /// <summary>🆕 2026-10-19（`W57` · `T17/T18`）：**已知完整官方键**时取文本
        /// （定制开场白两档拼出来的是 `Chat_Messages/<me>-Chat0-<foe>` 这种，不再走 `cardId + 后缀` 的拼法）。
        /// 口径与 <see cref="OfficialChatText"/> 完全一致：缺键 / 空 ⇒ `null`（⛔ 不自译）。</summary>
        public static string OfficialChatTextOfTerm(string fullTerm, out string term)
        {
            term = fullTerm;
            if (string.IsNullOrEmpty(fullTerm)) return null;
            string v = Loc.T(fullTerm);
            return string.IsNullOrEmpty(v) || v == fullTerm ? null : v;
        }

        /// <summary>
        /// 说一句。<paramref name="side"/> = **0 我方 / 1 敌方 / 2 `RadioChat`**（编号判据 → `_radio` 的注释）。
        /// <paramref name="artKey"/> = 立绘键（`BattleDriver.ArtKey`，**判据只此一处**）；
        /// <paramref name="text"/> 为空时退回显示 <paramref name="fallbackName"/>（卡名）。
        /// <paramref name="clip"/> 为 null = 这张卡没语音（那就不该调用本方法）。
        /// ⚠️ **唯一例外（2026-10-20 · `T18`）**：开场白的定制音 `customSound` **取不到**时，
        /// 调用方仍会调本方法（`clip = null`）把气泡/文字照原版显示出来 —— 原版
        /// `DisplayWarlordRegularChatMessage` 在 clip 为 null 时照样 `ShowChatBox`。
        /// 那一档的「为什么没声」由 `CardChatIntro.CustomSound` **出声一次**（⛔ 不静默）。
        /// <paramref name="chatSuffix"/> 非 null 时压过「按事件推的后缀」（只有开场白那档会传，见 <see cref="ChatTermKey"/>）。
        /// <paramref name="chatTermOverride"/> 🆕 2026-10-19（`W57` · `T17/T18`）：**完整官方键**（非 null 时压过
        /// 上面那条「`cardId + 后缀`」的拼法）—— 定制开场白两档是 `Chat_Messages/<me>-Chat0-<对手 id 或阵营名>`，
        /// 后缀里带着对手那一段（见 <see cref="CardChatIntro"/>）。
        /// <para>🆕 2026-10-19（`T17`）：显示文本**官方词条优先**（`Chat_Messages/*`），
        /// `text`（voice_lines 的英文）降为兜底 —— 判据见上面那一大段。</para>
        /// </summary>
        public void Speak(int side, string cardId, string evt, string artKey, string fallbackName,
                          string text, AudioClip clip, string clipFile = null, string chatSuffix = null,
                          string chatTermOverride = null)
        {
            var b = BubbleFor(side);
            if (b == null) return;

            HideAll();                                  // 同时只显示一条（另一侧说话就把这条顶掉）
            _current = b;
            _warnedNotPlaying = false;                  // 🆕 2026-10-21（新账-63）：每条气泡各自报一次
            _warnedZeroSpectrum = false;
            _left = Duration(clip);
            LastCardId = cardId; LastEvent = evt; LastClipName = clipFile;
            LastClipLoaded = clip != null;
            string chatTerm;
            // 🆕 2026-10-19（`W57` · `T17/T18`）：定制开场白两档拼出来的是**完整键**（带对手/阵营那一段），
            //   不能再按 `<cardId>+后缀` 拼 ⇒ 这一档直接给整键；其余调用点仍走 `chatSuffix`。
            string official = chatTermOverride != null
                ? OfficialChatTextOfTerm(chatTermOverride, out chatTerm)
                : OfficialChatText(cardId, evt, chatSuffix, out chatTerm);
            LastChatTerm = chatTerm;
            LastChatFromOfficial = official != null;

            if (b.portrait != null)
            {
                var tex = CardArt.Portrait(artKey);
                if (tex == null) tex = CardArt.PortraitByName(fallbackName);
                if (tex != null)
                {
                    b.portrait.SetTexture(tex);
                    b.portrait.SetAspect(PortraitW / PortraitH);   // 换图会带进贴图比例，拽回原版矩形
                }
            }
            // 🔴 **先亮起来，再量尺寸** —— TMP 在**非激活**对象上量不出尺寸（`textBounds` 是垃圾值），
            //    而 `Label.SetTextTmp` / `SetAutoFitBox` 两次都要量（`CLAUDE.md` §三 那条坑）。
            //    同一帧内 `SetActive` + `SetText` **不会闪出上一句** —— 渲染发生在整个方法返回之后。
            b.SetActive(true);

            if (b.text != null)
            {
                // 🆕 2026-10-19（`T17`）：**官方词条优先**（`Chat_Messages/*`）→ voice_lines 的英文 → 卡名。
                //   三个来源都不是我们译的：官方两列是原版 I2 表；voice_lines 的 text 是从原版音频文件名抠的英文。
                string shown = official ?? (string.IsNullOrEmpty(text) ? (fallbackName ?? "") : text);
                b.text.SetText(shown);
                // 字号：**照原版那个 `m_fontSize = 33`**。
                // 🔴 2026-09-25 更正：原来英文走的是 `SetCapHeight(33 × 0.72 / 108)`（按「拉丁大写 ≈ 0.72 em」折），
                //    两处都不对 —— ① TMP 的 `m_fontSize` **就是 em 大小**（原版 33 就是 33 px 的 em），
                //    按大写折等于把 em 当成 0.72 em 用；② 我们字体实测 `Wcap/Wglyph = 0.0779/0.0948 = **0.822**`，
                //    0.72 这个常数本来就不对。两处叠起来 ⇒ 我们最大只到 **28.9 px**，比原版小 12%。
                //    现在中英**一律按 em** 给（`SetGlyphHeight(px/108)` 就是「em = px」）。
                // 🔴 **2026-10-16（`T09` 批 3）**：那一句改成 `U(…)` —— 世界档仍是 `33/108`（逐位不变），
                //    uGUI 档是 **33 设计 px**（画布单位就是设计 px）。
                b.text.SetGlyphHeight(U(OrigFontPx));
                // 逐行左对齐（原版 `m_HorizontalAlignment = 1`）—— 折行后短行靠左，不是居中
                b.text.SetAlignLeft();
                // 🔴 **台词不许溢出**：限宽折行 + 字号自适应，逐值照原版那个框（见 `TextW/TextH` 的出处）。
                //    顺序要紧：**先**把字号按「最大 33」定好（上面那行），`SetAutoFitBox` 才拿得到
                //    正确的上限（它取的是本类自己算的字号，见那个方法的注释）。
                // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版台词那颗 TMP 的 `m_fontSizeBase` **原文**。
                //    判据（原版实读，13 个战场各 3 颗）：`/…battlearena*/…` 的 `'Here goes a chat line'`
                //    = `m_fontSize 33` · `auto[10~33]` · **`m_fontSizeBase 36.0`**（40 颗里 **39 颗是 36.0**、
                //    1 颗是 12.0 —— 逐站表 §二·3 #33）。⚠️ 36 **≠ 标称 33** ⇒ 必须显式传
                //    （缺省 = 标称 33，那是旧行为）。
                // 🔴 **2026-10-16（`T09` 批 3）**：**框（前两个实参）走 `U()`**（世界档 = `÷108`、逐位不变；
                //    uGUI 档 = **设计 px** —— `SetAutoFitBox` 内部拿它写 TMP 的 `sizeDelta`，喂世界单位会让框
                //    变成 1/108、字号收敛到一个荒谬的小值，**不报错**）。
                //    ⚠️ **`minPx`/`maxPx`/`basePx` 那三个【不换】** —— 它们本来就是**像素**口径
                //    （`SetAutoFitBox` 的 doc 与 `MulliganPanel.PromptFs*` 先例逐字相同）。
                b.text.SetAutoFitBox(U(TextW), U(TextH), TextMinPx, OrigFontPx, 36f);
            }

            // ⚠️ 用 `clip=` + `Play()` 而不是 `PlayOneShot` —— 后者的声音**停不掉**
            //    （`Stop()` 管不到 one-shot），而我们要在气泡收起来时把话掐掉
            if (clip != null && b.audio != null)
            {
                b.audio.clip = clip;
                b.audio.Play();
                // 🔴 2026-10-21（`新账-63` · 第二层诊断）：Play 的**当场**状态只打一次 ——
                //   用来区分「length=0 / 没加载 / 对象未激活 / Play 后立刻停」。⛔ 别每句都打（刷屏）。
                if (!_loggedClipState)
                {
                    _loggedClipState = true;
                    float vv = 999f;
                    if (b.audio.outputAudioMixerGroup != null && b.audio.outputAudioMixerGroup.audioMixer != null
                        && !b.audio.outputAudioMixerGroup.audioMixer.GetFloat("VolumeVoices", out vv)) vv = 999f;
                    Debug.Log($"[UnitChat] Play 当场：{clip.name} · len={clip.length:F3}s"
                              + $" · loadState={clip.loadState} · isPlaying={b.audio.isPlaying}"
                              + $" · audioEnabled={b.audio.enabled} · goActive={b.audio.gameObject.activeInHierarchy}"
                              + $" · vol={b.audio.volume:F2} · mixer={(b.audio.outputAudioMixerGroup != null ? b.audio.outputAudioMixerGroup.name : "null")}"
                              + $" · VolumeVoices={(vv > 900f ? "-" : vv.ToString("F1") + "dB")}"
                              + $" · listenerVol={AudioListener.volume:F2} · listenerPause={AudioListener.pause}"
                              + $" · listeners={UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length}"
                              + $" · dspRate={AudioSettings.outputSampleRate} · speakerMode={AudioSettings.speakerMode}");
                }
            }
        }

        /// <summary>
        /// 显示多久 = **原版真值**：`VoiceLinesController.GetCurrentClipLength`（反编译
        /// `VoiceLinesController__GetCurrentClipLength.c:9-33`）：
        /// <code>
        /// clip == null            → 2.0
        /// 否则                    → Mathf.Max(2.0, clip.length + 0.3)
        /// </code>
        /// 两个常量都是从 `GameAssembly.dll` 现读的（`DAT_1834b2bbc = 2.0` · `DAT_1834b2dc8 = 0.3`；
        /// RVA = VA − ImageBase(0x180000000)，见 `资料/战斗规则与数值_出处.md` §三）。
        /// 🔴 **2026-10-21 就地订正（铁律 5）**：本方法原来写 `clamp(len,1,4)+0.4` 并自认
        /// 「这是我们挑的」—— **判据是有的**（就是上面那条），所以那句「我们挑的」不成立。
        /// 差别很实：原版对**短 clip**（本工程 1778 条语音里中位数只有 0.41 s）**不会缩短气泡**，
        /// 一律至少 **2.0 s**；我们原来 0.25 s 的 clip 只显示 1.4 s。
        /// 另：原版 `MAX_AUDIO_DURATION = 2` 是**音频侧**的截断，不是气泡时长 —— ⛔ 别再把两者混为一谈。
        /// </summary>
        static float Duration(AudioClip clip)
        {
            if (clip == null) return 2.0f;
            return Mathf.Max(2.0f, clip.length + 0.3f);
        }

        public void HideAll()
        {
            if (_player != null) _player.SetActive(false);
            if (_enemy != null) _enemy.SetActive(false);
            if (_radio != null) _radio.SetActive(false);
            _current = null;
            _left = 0f;
        }

        /// <summary>🆕 2026-10-20（W58 · G 组）：把三颗气泡挂的语音也**停掉**。
        /// <para>`HideAll` 按既有口径**只收气泡、不碰音频**（见 `BattleDriver.CloseTutorialChatBoxes`
        /// 那段注释：原版 `CloseChatBox` 是否停音没核到 ⇒ 别改 `HideAll` 的语义）。而「新一局 = 新场景」
        /// 这一档必须把上一局的语音收掉（原版换场景连 AudioSource 一起没了），否则上一局的台词会串到
        /// 下一局开场（还会挡住 `BattleDriver.TickIntroMonologue` 的「等上一条播完」闸）。</para>
        /// <para>调用点只有 `BattleDriver.Begin`（本局账不跨局那一族）—— ⛔ 别拿它替换 `HideAll`。</para></summary>
        public void StopVoice()
        {
            if (_player != null && _player.audio != null) _player.audio.Stop();
            if (_enemy != null && _enemy.audio != null) _enemy.audio.Stop();
            if (_radio != null && _radio.audio != null) _radio.audio.Stop();
        }

        /// <summary>三颗气泡按原版那三个序号取（`0 = PlayerChatDisplay` · `1 = EnemyChatDisplay`
        /// · `2 = RadioChat`）。认不出的序号 ⇒ null + **出声**（⛔ 不静默，也别顺手落到某一颗上 ——
        /// 那会把「电台」的话说成某一边督军的）。</summary>
        Bubble BubbleFor(int side)
        {
            if (side == 0) return _player;
            if (side == 1) return _enemy;
            if (side == 2) return _radio;
            Debug.LogWarning($"[UnitChat] 气泡序号 `{side}` 认不出（原版只有 0/1/2 三颗："
                           + "`PlayerChatDisplay` / `EnemyChatDisplay` / `RadioChat`，"
                           + "判据 `UnitsVoiceLinesPanel__GetChatBox.c`）⇒ 这一句**不显示**。");
            return null;
        }

        /// <summary>自检用：某一颗气泡的**世界中心**（`0/1/2`，见 <see cref="BubbleFor"/>）。
        /// 那颗没建出来 ⇒ `Vector3.zero`。</summary>
        public Vector3 BubbleCenterWorld(int side)
        {
            var b = BubbleFor(side);
            return b != null && b.bg != null ? b.bg.transform.position : Vector3.zero;
        }

        /// <summary>走表（`BattleDriver.AdvanceTimeline` 里每帧推，批处理靠 `Step` 推）。</summary>
        public void Advance(float dt)
        {
            if (_current == null) return;
            // 🔴 2026-10-21（`新账-63` · 第三层诊断）：每 **0.08 s** 打一次「气泡在场时的真实状态」——
            //   原来 0.4 s 太稀：本工程语音中位数只有 **0.41 s**，0.4 s 的采样点**整个落在播放窗口之外**
            //   （上一轮真机日志里 4 个 tick 只有 1 个 `playing=True`，且那一个时刻 `_spectrum` 还是 null）。
            //   定位完可以删。
            if (Time.unscaledTime - _lastDiagAt > 0.08f)
            {
                _lastDiagAt = Time.unscaledTime;
                float mA = _current.waveMat != null ? _current.waveMat.GetFloat("_MovementRange") : -999f;
                float ss = 0f;
                if (_spectrum != null) for (int i = 0; i < 16; i++) ss += _spectrum[i];
                var a0 = _current.audio;
                // 🔴 2026-10-21 第四层诊断：真机实测「`isPlaying=True`、`aTime` 在走，频谱却恒 ≈0」
                //   ⇒ 需要区分「**source 专有 API 取不到**」与「**整条音频链路没输出**」：
                //   同一时刻再读一次 `AudioListener.GetSpectrumData`（全场混音），两者对比即判。
                if (_lspec == null) _lspec = new float[64];
                AudioListener.GetSpectrumData(_lspec, 0, FFTWindow.Hamming);
                float ls = 0f; for (int i = 0; i < 16; i++) ls += _lspec[i];
                Debug.Log($"[UnitChat] tick playing={(a0 != null && a0.isPlaying)}"
                          + $" aTime={(a0 != null ? a0.time : -1f):F3} aLen={(a0 != null && a0.clip != null ? a0.clip.length : -1f):F3}"
                          + $" load={(a0 != null && a0.clip != null ? a0.clip.loadState.ToString() : "-")}"
                          + $" vol={(a0 != null ? a0.volume : -1f):F2} mute={(a0 != null && a0.mute)}"
                          + $" waveMat={(_current.waveMat != null)} waveAmp={_waveAmp:F3} matAmp={mA:F3}"
                          + $" specSum16={ss:F6} lisSum16={ls:F6} left={_left:F2}");
            }
            // 🆕 2026-10-21：音波幅度 —— **逐行照原版 `AudioWaveEqualizer.Update`**
            //   （`d:/2/tools/decomp_full/AudioWaveEqualizer__Update.c:29-67`）：
            //     · 每帧 `GetSpectrumData(src, spectrum, 0, 2 /*Hamming*/, 0)` —— **原版不判 `isPlaying`**，
            //       只要 `audioSource != null` 就读（没播时频谱天然是 0，靠平滑自然落到 0）；
            //     · 前 16 bin 求和 × `spectrumModifier`(15) → clamp01 → 按 `deltaTime * speedChange`(10) 平滑；
            //     · 写 `Material.SetFloat`（属性名在 `DAT_184235590`）。
            //   两个字段值来自原版场景对象：`bundle_scenes_scenes_battlearena1/MonoBehaviour/
            //   MonoBehaviour_4115.json` 的 `spectrumModifier = 15.0` / `speedChange = 10.0`。
            if (_current.audio != null && _current.waveMat != null)
            {
                if (_spectrum == null) _spectrum = new float[64];
                _current.audio.GetSpectrumData(_spectrum, 0, FFTWindow.Hamming);
                float sum = 0f;
                for (int i = 0; i < 16; i++) sum += _spectrum[i];
                float target = Mathf.Clamp01(sum * 15f);
                // 🔴 不许静默失败：语音在播、频谱却全 0 ⇒ 音波会是直线（真机 `新账-63` 就是这现象）。
                //   只报一次，够定位「是频谱取不到」还是「音频根本没播」。
                if (target <= 0f && _current.audio.isPlaying && !_warnedZeroSpectrum)
                {
                    _warnedZeroSpectrum = true;
                    Debug.LogWarning("[UnitChat] 语音在播（`isPlaying=1`）但 `GetSpectrumData` 前 16 bin 全 0"
                                     + " ⇒ 音波线不会动。查：AudioSource 是否被 mixer 组静音 / 平台是否禁了频谱。");
                }
                _waveAmp = Mathf.Lerp(_waveAmp, target, Mathf.Clamp01(10f * dt));
                // 🔴 属性名 = **原版那个 `_MovementRange`**（名字来自 `DAT_184235590` 的字符串，
                //   RVA 0x4235590；材质默认值 0.00591）。我们原来起名 `_Amplitude` 并在这里乘 0.22 ——
                //   现在量纲收进 shader 的 `_WaveScale`，这里写的是**原版 0..1 的原值**。
                _current.waveMat.SetFloat("_MovementRange", _waveAmp);
            }
            // 🔴 `新账-63` 的另一半：气泡在显示、音频**却没在播**（clip=null / Play 失败）⇒ 也出声一次。
            //   别把「没声」和「频谱全 0」混成同一个现象（两者的修法不同）。
            else if (_current.audio != null && !_current.audio.isPlaying && !_warnedNotPlaying)
            {
                _warnedNotPlaying = true;
                var a = _current.audio;
                var cl = a.clip;
                // 🔴 2026-10-21（`新账-63` · 第二层诊断）：把「为什么没播」一次打全 ——
                //   长度/加载状态/组件开关/所在对象激活/mute/音量。首轮只报了 clip 名，定位不到是
                //   「clip 长度 0」「没加载」「对象未激活」还是「Play 失败」。
                Debug.LogWarning($"[UnitChat] 气泡在显示但没有音频在播（clip={(cl == null ? "null" : cl.name)}"
                                 + $" · length={(cl != null ? cl.length : -1f):F3}s"
                                 + $" · loadState={(cl != null ? cl.loadState.ToString() : "-")}"
                                 + $" · audioEnabled={a.enabled} · goActive={a.gameObject.activeInHierarchy}"
                                 + $" · mute={a.mute} · vol={a.volume:F2}）"
                                 + " ⇒ 音波线不会动（这一条台词本来就没声？还是 Play 失败？）。");
            }
            _left -= dt;
            if (_left > 0f) return;
            if (_current.audio != null && _current.audio.isPlaying) _current.audio.Stop();
            HideAll();
        }

        float _waveAmp;
        float[] _spectrum;
        float[] _lspec;                 // 🔴 2026-10-21 第四层诊断：全场混音频谱（与 source 专有 API 对比）
        // 🔴 2026-10-21（`新账-63`）：两种「音波不会动」的原因各只报一次，避免每帧刷屏。
        bool _warnedZeroSpectrum;
        bool _warnedNotPlaying;
        bool _loggedClipState;
        float _lastDiagAt;
    }

    // ==================================================================
    //  🆕 2026-10-19（`W57` · `T17/T18`）：**原版定制开场白两档**（精确对局 / 按阵营）
    // ==================================================================
    //  判据（`d:/2/tools/decomp_full/` 逐句；全量推导 → `资料/普查产出_1019/W_战斗线_开场白数据_1019b.md` §1·C）：
    //   · `ChatManager__GetChatMessage.c`：`idx < 2` 且「**不是同督军对局** 或 **isPlayer**」时：
    //       ① `GetCustomIntro(me, foe)` —— 遍历 `me.customIntroChatData`，
    //          `entry.opponentWarlord == GetChatId(foe, true)` 命中
    //          → `Chat_Messages/<GetChatId(me,true)>-Chat0-<GetChatId(foe,true)>`；
    //       ② 否则 `GetCustomIntroByArmy(me, foe.army)`（`entry.army == foe.army`）
    //          → `Chat_Messages/<GetChatId(me,true)>-Chat0-<CardArmy 枚举名>`；
    //       ③ 否则 `RawCardScript.GetLocalizedChatMsg(me, idx)` = `Chat_Messages/<chatId>-Chat0`（兜底）。
    //   · **同督军对局**：玩家那一侧仍走上面这条链（通常落到 ③）；**AI 那一侧** `param_1 = 1`
    //     → `Chat_Messages/<id>-Chat1`（`MirrorMatch`），⛔ 不进定制档。
    //   · `ChatManager__IsPlayerFirstIntro.c`：只看**玩家自己的精确档** —— 命中返回 `entry.talksFirst(+0x20)`；
    //     未命中（含只命中按阵营档 / 没有数据）⇒ **1（玩家先说）**。
    //   · `GetChatId(card) = chatMessagesReferenceId(+0x168) ?: customIntroReferenceId(+0x170, 仅 param_2!=0) ?: uniqueId(+0x38)`。
    //  ⚠️ **数据 = `Resources/card_chat_intro.json`**（49 张运行时卡 · 197 键；生成器 `工具/gen_card_chat_intro.py`）。
    //     ⛔ **别拿「官方表里有没有这个键」当判据** —— 原版的闸门是 **SO 列表**（这就是那份数据）；
    //     官方表里多出来的键是**死键**（数据报告 §3·A 逐张裁过）。
    //  ⚠️ `hasText == false`（官方表缺原文；运行时卡上 5 条）⇒ 调用方回 `voice_lines` 英文兜底 + 出声，⛔ **不自译**。
    //  ⚠️ `talksFirst` 是**逐对手**的（57 张精确档卡里 31 张逐条不同），顶层那个是「全都一致时才给值」的摘要
    //     ⇒ 这里只读**逐条**那份（顶层不参与判据）。
    public static class CardChatIntro
    {
        /// <summary>`Resources.Load` 用的名字（文件 = `Resources/card_chat_intro.json`）。</summary>
        public const string ResourcePath = "card_chat_intro";

        /// <summary>一条开场白（精确档 / 按阵营档都收成这一个形状）。</summary>
        public struct Entry
        {
            /// <summary>完整官方键（`Chat_Messages/…`）。</summary>
            public string Key;
            /// <summary>官方表里有没有原文（false ⇒ 回 voice_lines 英文兜底 + 出声，⛔ 不自译）。</summary>
            public bool HasText;
            /// <summary>谁先说（原版 `CustomIntroChatData +0x20`）；**-1 = 这一档没有这个字段**（按阵营档）。</summary>
            public int TalksFirst;
            /// <summary>🆕 2026-10-20（战斗线 `T18`）：原版 `entry.customSound` 的
            /// `AssetReference.m_AssetGUID`（`CustomIntroChatData.cs:10` / `CustomIntroByArmyChatData.cs:10`）。
            /// 空串 = 这一档没有定制音 ⇒ 回落卡自己的 `GetChatSound(Intro)`（现有 `VoiceLines` 那一链）。</summary>
            public string SoundGuid;
            /// <summary>🆕 2026-10-20：`soundGuid` 解出的 AudioClip 资产名（生成器用
            /// `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` 解；加载路径 =
            /// `Resources/Art/audio/vo/<soundAsset>`）。**空串但 `SoundGuid` 非空 = 解不出**
            /// ⇒ 调用方出声并回落到无音（⛔ 不许拿默认音顶替）。</summary>
            public string SoundAsset;
        }

        sealed class OppEntry { public string key; public bool hasText; public int talksFirst = -1; public string soundGuid; public string soundAsset; }
        sealed class ArmyEntry { public string key; public bool hasText; public string soundGuid; public string soundAsset; }
        sealed class CardEntry
        {
            public string id;
            public string chatId;
            public Dictionary<string, OppEntry> byOpponent;
            public Dictionary<string, ArmyEntry> byArmy;
        }

        static bool _loaded;
        static Dictionary<string, CardEntry> _byId;
        static Dictionary<string, string> _chatIdOverrides;
        static string _lastError;
        static readonly HashSet<string> _warnedMissing = new HashSet<string>();

        /// <summary>表载进来了没有（没载 = 资源不在；**不静默**：<see cref="LastError"/> 有话说）。</summary>
        public static bool Loaded { get { EnsureLoaded(); return _byId != null && _byId.Count > 0; } }
        /// <summary>表里几张卡（运行时件 = 49）。</summary>
        public static int Count { get { EnsureLoaded(); return _byId != null ? _byId.Count : 0; } }
        /// <summary>载入 / 解析失败的原因（`null` = 一切正常）。</summary>
        public static string LastError { get { EnsureLoaded(); return _lastError; } }

        /// <summary>自检用：清掉「缺原文」那条警告的去重表（⛔ 不动已载的表）。</summary>
        public static void ResetWarningsForTest() { _warnedMissing.Clear(); }

        /// <summary>表里有这张卡（按短 id，例如 `AM1`）。</summary>
        public static bool HasCard(string id)
        {
            EnsureLoaded();
            return _byId != null && !string.IsNullOrEmpty(id) && _byId.ContainsKey(id);
        }

        /// <summary>某张卡的**精确档对手数** / 按阵营档数（自检对账用；表里没有这张卡 ⇒ 0）。</summary>
        public static int OpponentEntries(string id)
        {
            EnsureLoaded();
            CardEntry c;
            return _byId != null && !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out c) && c != null && c.byOpponent != null
                 ? c.byOpponent.Count : 0;
        }
        public static int ArmyEntries(string id)
        {
            EnsureLoaded();
            CardEntry c;
            return _byId != null && !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out c) && c != null && c.byArmy != null
                 ? c.byArmy.Count : 0;
        }

        /// <summary>这张卡的 **chatId**（原版 `GetChatId`：`chatMessagesReferenceId ?: customIntroReferenceId ?: uniqueId`）。
        /// 运行时件里 49 张的 `chatRef/introRef` 全空 ⇒ 现读恒等于卡 id；`chatIdOverrides` 以后非空时以它为准。</summary>
        public static string ChatIdOf(string cardId)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(cardId)) return cardId;
            string v;
            if (_chatIdOverrides != null && _chatIdOverrides.TryGetValue(cardId, out v) && !string.IsNullOrEmpty(v)) return v;
            CardEntry c;
            if (_byId != null && _byId.TryGetValue(cardId, out c) && c != null && !string.IsNullOrEmpty(c.chatId)) return c.chatId;
            return cardId;
        }

        /// <summary>拼一条官方键：`Chat_Messages/<chatId><suffix>`（`suffix` = `-Chat0` / `-Chat0-<对手>` …）。</summary>
        public static string TermFor(string cardId, string suffix)
        {
            if (string.IsNullOrEmpty(cardId) || suffix == null) return null;
            string id = ChatIdOf(cardId);
            return string.IsNullOrEmpty(id) ? null : UnitChatPanel.ChatTermPrefix + id + suffix;
        }

        /// <summary>原版那一对（说话人 × 对手）的**定制开场白**：① 精确档 → ② 按阵营档。
        /// 都没命中 ⇒ `false`（调用方用 <see cref="TermFor"/> 拼 ③ 兜底 `-Chat0`）。
        /// <para>⚠️ <paramref name="foeArmy"/> 是 `CardArmy` 的**枚举名字符串**（`"Ultramarines"` 等）——
        /// 我们卡池的 `faction` 与它逐一相同（现读 1,127/1,127）⇒ 不需要自带枚举表。</para></summary>
        public static bool TryResolve(string speakerId, string foeId, string foeArmy, out Entry entry)
        {
            EnsureLoaded();
            entry = default(Entry);
            CardEntry c;
            if (_byId == null || string.IsNullOrEmpty(speakerId) || !_byId.TryGetValue(speakerId, out c) || c == null)
                return false;
            string foeChatId = ChatIdOf(foeId);
            if (c.byOpponent != null && !string.IsNullOrEmpty(foeChatId))
            {
                OppEntry op;
                if (c.byOpponent.TryGetValue(foeChatId, out op) && op != null && !string.IsNullOrEmpty(op.key))
                {
                    entry = new Entry { Key = op.key, HasText = op.hasText, TalksFirst = op.talksFirst,
                                        SoundGuid = op.soundGuid, SoundAsset = op.soundAsset };
                    return true;
                }
            }
            if (c.byArmy != null && !string.IsNullOrEmpty(foeArmy))
            {
                ArmyEntry a;
                if (c.byArmy.TryGetValue(foeArmy, out a) && a != null && !string.IsNullOrEmpty(a.key))
                {
                    entry = new Entry { Key = a.key, HasText = a.hasText, TalksFirst = -1,
                                        SoundGuid = a.soundGuid, SoundAsset = a.soundAsset };
                    return true;
                }
            }
            return false;
        }

        /// <summary>原版 `ChatManager.IsPlayerFirstIntro`：**只查玩家自己的精确档**；命中返回 `entry.talksFirst`，
        /// 未命中（含只命中按阵营档 / 没有数据）⇒ **1 = 玩家先说**。
        /// @return `1` = 玩家先说 · `0` = 对手先说。</summary>
        public static int PlayerTalksFirst(string playerId, string foeId)
        {
            EnsureLoaded();
            CardEntry c;
            if (_byId == null || string.IsNullOrEmpty(playerId) || !_byId.TryGetValue(playerId, out c) || c == null)
                return 1;
            if (c.byOpponent != null)
            {
                string foeChatId = ChatIdOf(foeId);
                OppEntry op;
                if (!string.IsNullOrEmpty(foeChatId)
                    && c.byOpponent.TryGetValue(foeChatId, out op) && op != null && op.talksFirst >= 0)
                    return op.talksFirst != 0 ? 1 : 0;
            }
            return 1;
        }

        /// <summary>官方表**缺原文**时喊一声（每条键只喊一次；⛔ 不自译）。</summary>
        public static void WarnMissingTextOnce(string key)
        {
            if (string.IsNullOrEmpty(key) || !_warnedMissing.Add(key)) return;
            Debug.LogWarning("[Chat] 原版这一对开场白**没有原文**（`" + key + "`）⇒ 按原版缺键处理："
                           + "回 `voice_lines` 的英文兜底 + 出声（⛔ 不自译；判据 → "
                           + "`资料/普查产出_1019/W_战斗线_开场白数据_1019b.md` §1·B / §3·C）");
        }

        // ---- 🆕 2026-10-20（战斗线 `T18`）：`customSound`（原版 `ChatManager.GetChatSoundAsset`） ----

        static readonly HashSet<string> _warnedSound = new HashSet<string>();

        /// <summary>表里带 `soundGuid` 的条目数（运行时现读 = 197 / 197 —— **每一条都有定制音**）。</summary>
        public static int EntriesWithSound { get; private set; }
        /// <summary>`soundGuid` 非空、但 `soundAsset` 空（生成器解不出 GUID）的条目数 —— 正常应为 0。</summary>
        public static int SoundUnresolvedEntries { get; private set; }
        /// <summary>自检用：累计有多少次「该播定制音却拿不到」（解不出 / 资源不在）。⛔ 不是总数统计，是失败计数。</summary>
        public static int SoundLoadFailures { get; private set; }
        /// <summary>最近一次定制音拿不到的人话原因（`null` = 没发生过）。</summary>
        public static string LastSoundError { get; private set; }

        /// <summary>自检用：清掉「定制音拿不到」的去重表与计数器（⛔ 不动已载的表）。</summary>
        public static void ResetSoundWarningsForTest()
        {
            _warnedSound.Clear();
            SoundLoadFailures = 0;
            LastSoundError = null;
        }

        /// <summary>
        /// 取这一条开场白的**定制音**（原版 `ChatManager__GetChatSoundAsset.c` 的 customSound 分支：
        /// `entry.customSound(+0x18) != 0` ⇒ `AssetReferenceTyped&lt;AudioClip&gt;.Load`）。
        /// <para>加载口 = **语音线同一条** `Resources/Art/audio/vo/&lt;soundAsset&gt;`（`VoiceLines.ClipRoot`，
        /// ⛔ 不另开一套播放器；播放仍走 `UnitChatPanel` 气泡上那颗 `AudioSource`，已挂 `Voices` 组）。</para>
        /// <para>返回 `null` 的三种情形：① `soundGuid` 空（这一档**没有**定制音 ⇒ 调用方回落
        /// `VoiceLines` 那一链，**不算失败**）；② GUID 解不出资产名；③ 资源不在。
        /// ②③ 都会**出声一次**（按 GUID 去重）并计入 <see cref="SoundLoadFailures"/> ——
        /// ⛔ **不许静默**、⛔ **不许拿默认音顶替**（宁可没声，不可播错）。</para>
        /// </summary>
        public static AudioClip CustomSound(string soundGuid, string soundAsset)
        {
            if (string.IsNullOrEmpty(soundGuid)) return null;
            if (string.IsNullOrEmpty(soundAsset))
            {
                Fail(soundGuid, null, "GUID 解不出资产名（生成器 `工具/gen_card_chat_intro.py` 的 "
                                    + "`soundAsset` 为空 —— 查 `unresolvedSoundGuids`）");
                return null;
            }
            string name = System.IO.Path.GetFileNameWithoutExtension(soundAsset);
            var clip = Resources.Load<AudioClip>(VoiceLines.ClipRoot + name);
            if (clip == null)
            {
                Fail(soundGuid, soundAsset, "`Resources/" + VoiceLines.ClipRoot + name + "` 取不到"
                                          + "（跑 `工具/import_original_audio.py` 补齐）");
                return null;
            }
            return clip;
        }

        static void Fail(string guid, string asset, string why)
        {
            SoundLoadFailures++;
            LastSoundError = "guid=`" + guid + "` asset=`" + (asset ?? "") + "`：" + why;
            if (_warnedSound.Add(guid))
                Debug.LogWarning("[Chat] 定制开场白音效**取不到** —— " + LastSoundError
                               + " ⇒ 这一句**回落到无音**（气泡照显示；⛔ 不猜成默认音、⛔ 不静默）");
        }

        // ---- 载入 ------------------------------------------------------

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                _lastError = "`Resources/" + ResourcePath + ".json` 载不到 —— 跑一次 "
                           + "`python 工具/gen_card_chat_intro.py`；开场白退回旧口径"
                           + "（`Chat_Messages/<卡 id>-Chat0`，⛔ 不静默）";
                Debug.LogWarning("[Chat] " + _lastError);
                return;
            }
            try
            {
                var root = new Json(ta.text).Read() as Dictionary<string, object>;
                if (root == null) throw new FormatException("根不是对象");
                _byId = new Dictionary<string, CardEntry>();
                _chatIdOverrides = new Dictionary<string, string>();
                EntriesWithSound = 0;
                SoundUnresolvedEntries = 0;

                object o;
                if (root.TryGetValue("chatIdOverrides", out o))
                {
                    var d = o as Dictionary<string, object>;
                    if (d != null)
                        foreach (var kv in d)
                            if (kv.Value is string && !string.IsNullOrEmpty((string)kv.Value))
                                _chatIdOverrides[kv.Key] = (string)kv.Value;
                }

                var cards = root.TryGetValue("cards", out o) ? o as List<object> : null;
                if (cards != null)
                {
                    foreach (var it in cards)
                    {
                        var cd = it as Dictionary<string, object>;
                        if (cd == null) continue;
                        var card = new CardEntry
                        {
                            id = Str(cd, "id"),
                            chatId = Str(cd, "chatId"),
                            byOpponent = new Dictionary<string, OppEntry>(),
                            byArmy = new Dictionary<string, ArmyEntry>(),
                        };
                        if (string.IsNullOrEmpty(card.id)) continue;

                        object sub;
                        var opps = cd.TryGetValue("introByOpponent", out sub) ? sub as Dictionary<string, object> : null;
                        if (opps != null)
                            foreach (var kv in opps)
                            {
                                var od = kv.Value as Dictionary<string, object>;
                                if (od == null) continue;
                                string key = Str(od, "key");
                                if (string.IsNullOrEmpty(key)) continue;
                                string sg = Str(od, "soundGuid");
                                string sa = Str(od, "soundAsset");
                                card.byOpponent[kv.Key] = new OppEntry
                                {
                                    key = key,
                                    hasText = Bool(od, "hasText"),
                                    talksFirst = Int(od, "talksFirst", -1),
                                    soundGuid = sg,
                                    soundAsset = sa,
                                };
                                CountSound(sg, sa);
                            }

                        var armies = cd.TryGetValue("introByArmy", out sub) ? sub as Dictionary<string, object> : null;
                        if (armies != null)
                            foreach (var kv in armies)
                            {
                                var ad = kv.Value as Dictionary<string, object>;
                                if (ad == null) continue;
                                string key = Str(ad, "key");
                                if (string.IsNullOrEmpty(key)) continue;
                                string sg = Str(ad, "soundGuid");
                                string sa = Str(ad, "soundAsset");
                                card.byArmy[kv.Key] = new ArmyEntry { key = key, hasText = Bool(ad, "hasText"),
                                                                      soundGuid = sg, soundAsset = sa };
                                CountSound(sg, sa);
                            }

                        _byId[card.id] = card;
                    }
                }
                if (_byId.Count == 0)
                    _lastError = "`" + ResourcePath + ".json` 里一张卡都没有（生成器跑了没有？）";
            }
            catch (Exception e)
            {
                _lastError = "`" + ResourcePath + ".json` 解析失败：" + e.Message;
                _byId = null;
                Debug.LogWarning("[Chat] " + _lastError);
            }
        }

        static string Str(Dictionary<string, object> d, string k)
        {
            object o;
            return d != null && d.TryGetValue(k, out o) ? o as string : null;
        }
        static bool Bool(Dictionary<string, object> d, string k)
        {
            object o;
            return d != null && d.TryGetValue(k, out o) && o is bool && (bool)o;
        }
        static int Int(Dictionary<string, object> d, string k, int fallback)
        {
            object o;
            if (d == null || !d.TryGetValue(k, out o) || !(o is double)) return fallback;
            return (int)(double)o;
        }

        /// <summary>载入时数「带定制音 / GUID 解不出」的条目（给自检对账；⛔ 不参与播放判定）。</summary>
        static void CountSound(string soundGuid, string soundAsset)
        {
            if (string.IsNullOrEmpty(soundGuid)) return;
            EntriesWithSound++;
            if (string.IsNullOrEmpty(soundAsset)) SoundUnresolvedEntries++;
        }

        /// <summary>极简 JSON 读取器（只读；对象 → `Dictionary&lt;string,object&gt;`、数组 → `List&lt;object&gt;`）。
        /// <para>⚠️ 为什么不用 `JsonUtility`：本表的两个档位是**动态键的对象**
        /// （`introByOpponent` 的键 = 对手 chatId、`introByArmy` 的键 = 阵营枚举名），
        /// `JsonUtility` **不支持字典**（`Loc` 那份是生成器拆成并行数组才绕过去的）。
        /// 数据文件已定、⛔ 不许改 ⇒ 只能在这里读。格式由我们自己的生成器产出（`indent=1`、`ensure_ascii=False`）。</para></summary>
        sealed class Json
        {
            readonly string _s; int _i;
            public Json(string s) { _s = s ?? ""; _i = 0; }

            public object Read()
            {
                Skip();
                var v = Value();
                Skip();
                if (_i < _s.Length) throw new FormatException("JSON: 尾部多余内容 @" + _i);
                return v;
            }

            void Skip() { while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++; }

            void Lit(string lit)
            {
                if (_i + lit.Length > _s.Length
                    || string.CompareOrdinal(_s, _i, lit, 0, lit.Length) != 0)
                    throw new FormatException("JSON: 期望 `" + lit + "` @" + _i);
                _i += lit.Length;
            }

            object Value()
            {
                Skip();
                if (_i >= _s.Length) throw new FormatException("JSON: 意外结束");
                char c = _s[_i];
                if (c == '{') return Obj();
                if (c == '[') return Arr();
                if (c == '"') return Str();
                if (c == 't') { Lit("true"); return true; }
                if (c == 'f') { Lit("false"); return false; }
                if (c == 'n') { Lit("null"); return null; }
                return Num();
            }

            Dictionary<string, object> Obj()
            {
                var d = new Dictionary<string, object>();
                _i++;                        // '{'
                Skip();
                if (_i < _s.Length && _s[_i] == '}') { _i++; return d; }
                while (true)
                {
                    Skip();
                    string k = Str();
                    Skip();
                    if (_i >= _s.Length || _s[_i] != ':') throw new FormatException("JSON: 期望 ':' @" + _i);
                    _i++;
                    d[k] = Value();
                    Skip();
                    if (_i >= _s.Length) throw new FormatException("JSON: 对象未结束");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; return d; }
                    throw new FormatException("JSON: 期望 ',' 或 '}' @" + _i);
                }
            }

            List<object> Arr()
            {
                var a = new List<object>();
                _i++;                        // '['
                Skip();
                if (_i < _s.Length && _s[_i] == ']') { _i++; return a; }
                while (true)
                {
                    a.Add(Value());
                    Skip();
                    if (_i >= _s.Length) throw new FormatException("JSON: 数组未结束");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; return a; }
                    throw new FormatException("JSON: 期望 ',' 或 ']' @" + _i);
                }
            }

            string Str()
            {
                Skip();
                if (_i >= _s.Length || _s[_i] != '"') throw new FormatException("JSON: 期望字符串 @" + _i);
                _i++;
                var sb = new System.Text.StringBuilder();
                while (_i < _s.Length)
                {
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (_i >= _s.Length) break;
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            {
                                if (_i + 4 > _s.Length) throw new FormatException("JSON: \\u 不完整");
                                int code = 0;
                                for (int k = 0; k < 4; k++)
                                {
                                    char h = _s[_i + k];
                                    int dv = h >= '0' && h <= '9' ? h - '0'
                                           : h >= 'a' && h <= 'f' ? h - 'a' + 10
                                           : h >= 'A' && h <= 'F' ? h - 'A' + 10 : -1;
                                    if (dv < 0) throw new FormatException("JSON: \\u 十六进制非法");
                                    code = (code << 4) | dv;
                                }
                                _i += 4;
                                sb.Append((char)code);
                                break;
                            }
                        default: throw new FormatException("JSON: 未知转义 \\" + e);
                    }
                }
                throw new FormatException("JSON: 字符串未结束");
            }

            object Num()
            {
                int start = _i;
                if (_i < _s.Length && (_s[_i] == '-' || _s[_i] == '+')) _i++;
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (char.IsDigit(c) || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') { _i++; continue; }
                    break;
                }
                double d;
                if (_i == start
                    || !double.TryParse(_s.Substring(start, _i - start),
                                        System.Globalization.NumberStyles.Float,
                                        System.Globalization.CultureInfo.InvariantCulture, out d))
                    throw new FormatException("JSON: 数字非法 @" + start);
                return d;
            }
        }
    }
}
