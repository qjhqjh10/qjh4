// ChatPopupPanel.cs — 原版 `ChatPopup`：点 `ChatButton` 弹出来的 **6 个预设台词钮**
//
// 规格出处（**逐条都是解包实测，不是照截图摆的**）：
//   `资料/语音线_原版规格与ASR管道.md` §1.7（机制/时长/颜色）与 §1.7.1（**逐节点绝对矩形**）。
//
// ── 原版链路 ────────────────────────────────────────────────────────────
//   `ChatButton`（GameObject 85，父链 `PlayerInfo → LeftArea`）上的 `EverguildButton.m_OnClick`
//    → `BattleManager.ClickChat` → `VoiceLinesController.ClickChat` → `VoiceLinesPopupSelector.Clicked`
//   `Clicked` = **开关**：开着就 `Hide()`，关着就 `SetActive(true)` + `RefreshChatLines()` + 淡入。
//   ⚠️ **原来任务表写「开面板的入口是 `EnemyInfo`」是错的** —— `EnemyInfo` 那侧 `m_Calls` 是空数组。
//
// ── 三条「跟直觉不一样」的原版事实（都在 §1.7 / §1.7.1 里有出处）────────────────
//   ① **没有水平滑动**。`Awake` 里 `outPos = originalPos − (1.5f × originalPos.x, 0)`，
//      而面板序列化 `x = 0` ⇒ 显示位与隐藏位**同为 (0, 479.81)**，那个 `DOAnchorPos` 是个**空动作**。
//      看得见的只有 `CanvasGroup.alpha` 0↔1。⇒ 我们**不做位移**。
//   ② **6 个钮的序列化位置全是 (0,0)**，真正的位置是 `Buttons` 容器上的 **`VerticalLayoutGroup`**
//      运行时排出来的（`spacing = 1.48` · `childAlignment = UpperCenter` · **不改尺寸**）。
//      ⇒ 只看序列化数据会把 6 个钮**全叠在左下角**。下表用的是**排完之后**的坐标。
//   ③ `CloseChatPopup` 那个全屏关闭区**确实隐形，但原因不是 alpha** ——
//      它挂的是 `NonDrawingGraphic`（UnityEngine.UI.Extensions，**不绘制、只吃射线**），
//      `m_Color.a` 其实是 1.0。我们工程里没有那个组件 ⇒ **用 1×1 全透明图 + raycastTarget** 等效。
//
// ── 6 个钮说什么 ────────────────────────────────────────────────────────
//   原版：`RefreshChatLines` 里**写死起点 5、按下标 +1** → `RawCardScript.GetLocalizedChatMsg(warlord, idx+5)`
//   （`SetupChatOptions.c:54,112` 与 `RefreshChatLines.c:71` 两处独立证实）。
//   ⇒ 下标 0…5 = 枚举 5…10 = `Greet / Threat / WellPlayed / Taunt / Sorry / Oops`，
//     对应我们 `VoiceLines.ForChatButton` = `greet / threat / wp / gen1 / gen2 / gen3`。
//   🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1845`/`A1804`）** —— 下面这段原来是这么写的：
//     ~~「原版的字来自 **I2 本地化词条**，而那张表**在远端 CCD、本地没有**（§1.6.1 已证）……
//       ⇒ 我们**用枚举自己的名字当标签**……**这一条是我们挑的**：本工程里没有任何一处能印出原版那 6 个真标签，
//       因为那个来源不存在于客户端」~~
//     **那个理由已经不成立**：I2 表 2026-10-14 已解开，6 条官方词条**就在我们 `Loc` 表里**
//     （`Battle/Chat/{Greetings,Threat,WellPlayed,Taunt,Sorry,Oops}` ⇒ zh 问候/威胁/打得好/嘲讽/抱歉/哎呀）。
//     ⇒ 现在走 `LabelFor(i)` = **官方词条优先**，`ButtonLabels` 那份英文枚举名**降为兜底**（缺键才用、且出声）。
//     ⚠️ 仍然成立的一条：原版**序列化下来的兜底文本 6 个钮全是 `Greetings`**（6 个 TMP 逐一确认）——
//       那是 prefab 的静态默认值，运行时被 I2 覆盖。
// ═════════════════════════════════════════════════════════════════════════════
//  🔴 **2026-10-16（`T09` 批 3 · `OverHand` 组）：7 张图 + 6 颗钮的字整批从【世界空间 quad】迁到【uGUI 子画布】**
//  · **子画布** = `SubCanvasName`（= 原版 GO 名 + `" Canvas"`）+ `HudCanvas.SortingLayer.OverHand` + order `0`
//    —— 判据 = `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一 我这一行：
//    `ChatPopup` @ `…/FrontCanvas/Safe area FrontCanvas/Unit Chat`，`(rm 继承 FrontCanvas(1),
//    ovr 继承 true, OverHand, 0, 兄弟序 **4/7** —— 7 是在 `Unit Chat` 内部数的)`，出处 `RT_2881` · `MB_4429`。
//  · 🔴 **层级差异（如实记，⛔ 本批没改）**：原版 `ChatPopup` 是 **`Unit Chat` 的子件**；我们这边
//    `ChatPopupPanel` 与 `UnitChatPanel` 是**兄弟**（`BattleDriver` 两处都挂 HUD 根）。
//    按 `T09` §五·补⑩ 二 的裁定：**本次只按「谁的 canvas 装谁」迁，⛔ 不倒嵌套**（先例 = `BattleDoors`
//    在我们这边挂在 `EndPanel` 里面、原版反过来）。⇒ 收口时 `SubCanvasRank` 里
//    `"Unit Chat Canvas"` 排在 `"ChatPopup Canvas"` **之前**（= 原版「子件画在父件之上」那一条）✓。
//  · ✅ **锚点已照原版补正（2026-10-16 · `T09` 批 3 补正 `A12`）**：本节点（`ChatPopup`）现在按原版那对的
//    `m_AnchorMin/Max` = **`(0,0)`（贴画布左下角）** + `pivot (0,0.5)` + `ap (0, 479.815)` + `sd 815.044 × 475.470`
//    （逐值现读 `battlearena2/RectTransform/RectTransform_2881.json`；`ap.y` 那一格原版是 `479.81005859375`，
//    我们取 `1080 − 面板中心 y = 479.815` ⇒ **16:9 下与改之前逐位相同**，差 0.005 px 见报告 §④）。
//    ⚠️ 改本件之前是「**中心锚** + 矩形尺寸」⇒ 非 16:9 下原版**贴着屏幕左下角**、我们贴着**画布中心**
//    （4:3 下面板上下沿差 **180 画布 px** = 135 屏 px；21:9 下左边沿差 **320 画布 px**）。现已两端一致。
//    ⇒ **子件现在按【本节点矩形的中心】给 px**（原版子件就是那么锚的：`CloseChatPopup` 的 `ap (2.45, 41.66)`、
//    `ChatButtons` 的 `ap (371, 206)` 都是相对面板中心），⛔ 不再是「相对画布中心」——
//    两者在 16:9 下同值（面板本来就在屏心），非 16:9 下**只有当子件跟着面板走才是对的**。
//  · 🔴 **世界 z 那 6 个常量【已按原版层序重排】**（既有缺陷，2026-10-16 补正 `A12` 一并修）——
//    判据与「改后谁压谁」对照表 → `ZClose` 那条 doc / 报告 §④。
//  · ✅ **几何自核（2026-10-16 现读 `battlearena2`，不是转抄报告）**：原版树是
//    `ChatPopup(2881) → CloseChatPopup(3500, ap 2.45/41.66, sd 4055.34×2114.44)` +
//    `ChatButtons(3363, anchor(0,0), ap 371/206, sd 722.66×403.13)`，而 `ChatButtons` 里那颗
//    `White Square` 底色是 `RT 2860`（中心锚、`ap (28.816, 5.615)`、`sd 557.314×301.346`）——
//    与本件 `Bg` 那条（557.31×301.35）**逐位对得上**（我按 `ChatButtons` 的 `ap` 倒推得 28.815/5.61）✓。
//    ⚠️ **那一级（`ChatButtons`/`Buttons` 容器）我们不建**：它的作用只是「排布」+「给子件当锚」，
//    而我们直接按排完的绝对 px 摆 ⇒ 等价（`ChatButton` 行本身是 `anchor (0,1)` + `piv (.5,.5)`
//    + `ap.x 278.655` + `ap.y −24 − 49.48k` + `sd 603.6×48`，逐行现读；见 `Button0` 那条）。
//  · 🔴 **本面板有可点件**。**2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e）已把输入层切成 uGUI**：
//    ① **`raycastTarget`**：原版这 6 颗钮的每一层（透明命中条 `MB 4947` · 可见底图 `MB 5202`）与
//       `CloseChatPopup`（`MB 4409`）都是 **`m_RaycastTarget = 1`**；我们现在照原版建 **6 条透明命中条**
//       （`ChatButton{1..6}_Hit`，rect = 整行 `603.6 × 48`、`m_RaycastPadding` 全 0）+ 把 `_close` 置 true。
//    ② 🔴 **事件口径（逐组件现读，不是照计划抄）**：
//       · **6 颗钮 = `IPointerClickHandler`（松手那一帧）** —— 原版 GO 940 上是
//         `UnityEngine.UI.Button`（`MB 4112`，`m_Transition=2`）+ `ChatPopupButton : EverguildButton`
//         （`MB 4652`），而 `ChatPopupButton.Initialize` 把 `ClickButtonChat` 加到**那颗 `Button` 的
//         `m_OnClick`** 上（`d:/2/tools/decomp_full/ChatPopupButton__Initialize.c`：取 `buttonConfig` 的
//         `+0x100` = `m_OnClick` → `UnityEvent.AddListener`）⇒ **onClick / 松手**。
//       · **条外关闭 = `EventTrigger` + `PointerDown`（按下那一帧）** —— `GameObject/CloseChatPopup.json`
//         → `MonoBehaviour_4913`：`m_Script` = `EventTrigger`（`UnityEngine.EventSystems`）、
//         `m_Delegates[0].eventID = **2**`（= `EventTriggerType.PointerDown`）→ `VoiceLinesPopupSelector.Hide`。
//       · 原版点中台词之后 **`ClickChat` 先 `Hide()` 再发 `OnVoiceLinePressed`**
//         （`VoiceLinesPopupSelector__ClickChat.c`）。
//    ③ **本轮是【加输入路径】、⛔ 不动既有行为**：手搓的 `PointerDownAt` / `PointerUpAt` 仍由
//       `BattleDriver.HandleChatPopup` 每帧喂（那是**驱动层**的活，不在本文件白名单）⇒ uGUI 这一侧
//       **只发事件**（`ButtonClicked` / `CloseRequested`）+ **条外关闭延到 `LateUpdate`**（避免与驱动
//       抢同一帧、让驱动先吃掉这一下）。合并配方写在报告 `W57_战斗线_T09_A3e1.md` §三。
//    ④ 命中判定**世界档仍走手搓**（`Hit`）—— uGUI 档把世界点经**本节点**的 `InverseTransformPoint`
//       折成设计 px 再比绝对矩形（与 `MulliganPanel.PxOf` 同一套；⛔ 不是 `LayoutSpace.ToPixel` 那一族
//       —— 那是**世界帧**的逆，非 16:9 下与画布帧差 1.333 倍 ⇒ 「看得见的地方点不到」）。
//    ⑤ ✅ **悬停 / 按下视觉**（2026-10-20 · `T09` 步骤 **A3-e1** 补；改之前这一档**零反馈**）——
//       原版是**两套并存、各打各的图形**（逐组件现读，判据全文 → 字段区 `_fxBtnBg` 那条 doc）：
//       **底图 `ColorTint`**（`MB 4112`：`0.9607843` / `0.7843137` / `0.1 s`）
//       ＋ **图标 `SpriteSwap`**（`ChatPopupButton.LateUpdate` 在 `leftButtonNormal/Hover/Pressed` 之间换图；
//       ⚠️ `MB 4652` 的 `m_SpriteState` **四个全 null** ⇒ ⛔ 别按 `m_SpriteState` 抄）。
//       ⇒ 每颗钮两个 `BattleButtonFx`（`_fxBtnBg` / `_fxBtnIcon`），状态由命中条上的
//       `EventTrigger`（Enter/Exit/Down/Up）喂。⛔ 只改外观：点击 / 关闭那两条路一个字没动。
// ═════════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CardPresentation
{
    /// <summary>原版 `ChatPopup` 的 6 个预设台词钮面板。**只负责显示与命中** ——
    /// 「说了什么」由 `BattleDriver` 决定（它才有 `RuleCore` 和语音表）。</summary>
    public class ChatPopupPanel : MonoBehaviour
    {
        // ==================================================================
        //  版面：**原版绝对 px，1920×1080，左上原点** —— 逐条抄自 §1.7.1
        //  ⚠️ 这几行**不要「顺手改整齐」**：它们是从 34 个 RectTransform 逐级下推算出来的，
        //     看着可疑的两个数（钮宽 603.6 > 容器 557.31 ⇒ 左右各溢出 23.15 · 6 条边不等长）
        //     **原版就是这样**。
        // ==================================================================
        static readonly Rect Panel = new Rect(0.00f, 362.45f, 815.04f, 475.47f);
        static readonly Rect Bg = new Rect(121.16f, 475.64f, 557.31f, 301.35f);   // `White Square` 染深绿
        static readonly Rect FrameTop = new Rect(130.75f, 445.23f, 517.69f, 33.92f);
        static readonly Rect FrameBottom = new Rect(131.49f, 769.06f, 517.68f, 36.54f);
        static readonly Rect FrameRight = new Rect(648.49f, 434.37f, 83.65f, 371.07f);
        static readonly Rect FrameLeft = new Rect(14.32f, 445.41f, 117.33f, 386.98f);

        /// <summary>第 0 个钮的绝对矩形；其余 5 个 **y 依次 +49.48**（= 48 高 + 1.48 间距）。</summary>
        static readonly Rect Button0 = new Rect(77.20f, 481.89f, 603.60f, 48.00f);
        const float ButtonPitch = 49.48f;

        // 钮内三块（偏移 = 绝对 − 钮原点，见 §1.7.1 那张表的最后三行）
        static readonly Rect BtnBg = new Rect(50.32f, 1.75f, 550.40f, 44.50f);    // `40k_voicelines_bt_R`
        static readonly Rect BtnIcon = new Rect(3.50f, 4.00f, 40.00f, 40.00f);    // `40k_voicelines_bt_L`
        static readonly Rect BtnText = new Rect(64.16f, 0.00f, 528.70f, 48.00f);

        /// <summary>原版底色 `White Square` 的 **`m_Color` 真值**（不是「大概深绿」）。
        /// ⚠️ 我先前记的 `(0, 0.07, 0, 1)` 是**凑整过的近似**；精确值是这三个数。</summary>
        static readonly Color BgColor = new Color(0.001554f, 0.066038f, 0.000000f, 1f);

        /// <summary>原版 `timeToOpen` / `timeToClose`，**两个都是 0.5 s**、缓动 `OutCubic`。</summary>
        public const float FadeTime = 0.5f;
        /// <summary>原版 `VoiceLinesController.CHAT_INTERACTABLE_COOLDOWN = 4f`。
        /// 冷却挂在 **`ChatButton`** 上（不是面板上）—— 由 `BattleDriver` 计时。</summary>
        public const float Cooldown = 4f;

        /// <summary>第 i 颗钮的**官方词条键**（下标 0…5 ↔ 枚举 `ChatMessage` 5…10；
        /// 判据 = `RawCardScript.GetLocalizedChatMsg(warlord, idx+5)` 起点写死 5）。
        /// 🔴 **2026-10-14（`A1845`/`A1804`）**：这 6 条**现在在我们 `Loc` 表里了**（原版 I2 表已解开）。</summary>
        public static readonly string[] ChatTerms =
            { "Battle/Chat/Greetings", "Battle/Chat/Threat", "Battle/Chat/WellPlayed",
              "Battle/Chat/Taunt", "Battle/Chat/Sorry", "Battle/Chat/Oops" };

        /// <summary>🆕 **2026-10-19（战斗线 `T17` · 账 `A1754`/`A1805`）：第 `i` 颗钮**说出来那句话**的官方词条键**
        /// = `Chat_Messages/<督军 id>-Chat<5+i>`（判据 = `RawCardScript__GetLocalizedChatMsg.c`
        /// 与 `VoiceLinesPopupSelector__SetupChatOptions.c:54,112` 的「起点写死 5、按下标 +1」）。
        /// <para>⚠️ 两道文案**别混**：钮上印的**标签** = `Battle/Chat/*`（<see cref="LabelFor"/>，这颗钮叫什么）；
        /// 本函数给的 = **原版台词文本**（显示在 `Battle/UnitChatPanel` 那条语音条上、与音频同一条）。
        /// 消费点 = `UnitChatPanel.Speak`（`evt = "Chat" + i` ⇒ <see cref="UnitChatPanel.ChatTermSuffix"/> 映到 `-Chat5…10`）。</para></summary>
        public static string LineTermFor(string warlordCardId, int i)
        {
            if (i < 0 || i >= ButtonCount) return null;
            return UnitChatPanel.ChatTermKey(warlordCardId, "Chat" + i);
        }

        /// <summary>第 i 颗钮印什么字。
        /// 🔴 **2026-10-14（`A1845`/`A1804`）**：改成**官方词条优先**（`Loc` 表里的官方 zh-CN/EN）；
        /// 取不到才退回 <see cref="ButtonLabels"/> 那一份英文枚举名，并**出声**（红线：不许静默）。
        /// ⚠️ `Loc.T` **缺键会返回【键名本身】**（不是 null/空串）⇒ 必须先判 `Loc.HasEntry`。</summary>
        public static string LabelFor(int i)
        {
            if (i < 0 || i >= ChatTerms.Length) return "";
            string key = ChatTerms[i];
            if (Loc.HasEntry(key)) return Loc.T(key);
            if (!_warnedNoEntry)
            {
                _warnedNoEntry = true;
                Debug.LogWarning("[Chat] 词条缺失，退回英文枚举名：" + key);
            }
            return ButtonLabels[i];
        }
        static bool _warnedNoEntry;

        /// <summary>钮上的标签 —— ⚠️ **现在只是【兜底】**（`LabelFor` 取不到官方词条时才用它）。
        /// 🔴 **2026-10-14 就地订正（铁律 5 · `A1845`）**：原来这一行写着「**这一行是我们挑的**，理由见文件头
        /// （原版标签来自远端 I18N，本地不存在）」—— **那个理由已不成立**：I2 表 2026-10-14 已解开，
        /// 6 条官方词条（zh 问候/威胁/打得好/嘲讽/抱歉/哎呀）就在 `Loc` 表里 ⇒ 实际印的是它们。
        /// 这一份英文枚举名只在表缺键时兜底。</summary>
        public static readonly string[] ButtonLabels =
            { "Greet", "Threat", "Well Played", "Taunt", "Sorry", "Oops" };

        // ==================================================================

        public const int ButtonCount = 6;

        // ── 🆕 2026-10-16（`T09` 批 3 · `OverHand` 组）：本面板的【渲染空间】--------------------------
        /// <summary>🔴 本面板那块 **uGUI 子画布**的名字（= 原版宿主 GO 名 + `" Canvas"`）。
        /// 判据 = `T09c` §一 我这一行（`ChatPopup` @ `…/Safe area FrontCanvas/Unit Chat`，`OverHand` / order 0）。
        /// ⛔ **只此一份** —— 调用方建它也要取这里。</summary>
        public const string SubCanvasName = "ChatPopup Canvas";
        /// <summary>本面板那块子画布的**排序层**（判据同 <see cref="SubCanvasName"/>）。</summary>
        public const int SortingLayerId = HudCanvas.SortingLayer.OverHand;
        /// <summary>本面板那块子画布的 `sortingOrder`（判据同 <see cref="SubCanvasName"/>）。</summary>
        public const int SortingOrder = 0;

        /// <summary>🔴 **本件活在哪个空间**：`false` = **世界空间 quad**（出厂档，与加本档之前**逐位相同**）·
        /// `true` = **uGUI 画布**。由 `Create(..., uguiHost)` 按「调用方有没有给宿主」定。</summary>
        bool _ugui;
        /// <summary>uGUI 档在不在（诊断 / 断言 / 自检用）。</summary>
        public bool IsUgui { get { return _ugui; } }

        /// <summary>本节点自己（画布那一级的换算基准 = `PxOf` 用的那个帧）。两种模式都是 `RectTransform`。</summary>
        RectTransform _frame { get { return (RectTransform)transform; } }

        /// <summary>**世界档专用**：本面板各层的**世界 z**（相机看 +Z ⇒ **z 越小越靠前**、z 大的画在底下
        /// —— 判据 → `SkillPanel.ZPanel` / `CardDisplayWindow:778` / `ErrorMessageBanner` 那三处注释，
        /// 与 `HudCanvas.Add` 的映射口径同向）。
        /// <para>🔴🔴 **2026-10-16（补正 `A12`）：这一份原来是【与原版反的】—— 已按原版层序重排。**
        /// **原样**是 `close 0.02 < 色块 0.04 < 框 0.05 < 钮 0.06/0.07` ⇒ 在「z 小 = 靠前」的口径下
        /// **色块（不透明深绿 `BgColor.a = 1`）压在六颗钮之上**（`Bg` 矩形 121.16..678.47 × 475.64..776.99
        /// 与六颗钮 77.2..680.8 × 481.9..777.3 几乎完全重叠；逐颗按到相机 (0,0,−20) 的欧氏距离算，
        /// 只有钮 0 的底图以 0.01 的余量侥幸在前 ⇒「六颗钮（多半还有它们的字）被那一整块深绿盖住」。
        /// 那是**既有缺陷**、不是本批引入的 —— `T09` 铁律 11「与原版不符的全部要做」⇒ 主对话裁定**一并修**。）</para>
        /// <para>**判据 = 原版 prefab 的子件序**（`battlearena2` 现读）= `ChatPopup` 的孩子
        /// `[0] CloseChatPopup`(最底) · `[1] ChatButtons` → `ChatButtons` 的孩子
        /// `[0] White Square`色块 · `[1] 四个框` · `[2] Buttons` → 每颗 `ChatButton` 行
        /// `[0] bg · [1] Text · [2] icon` ⇒ **从底到顶**是「关闭区 → 色块 → 框 → 钮(bg→字→图标)」。</para>
        /// <para>🔑 **改后谁压谁**（从前到后 = 从 z 小到 z 大 = 从画在上到下）：
        /// `钮图标(0.02) → 钮文字(0.03) → 钮底图(0.04) → 框(0.05) → 色块(0.06) → 关闭区(0.07)`。
        /// ⇒ 六颗钮**压在**色块与四个框之上 ✓（原版的层序）；`_close` 那一片全屏透明件仍在**最底下**
        /// （它 `alpha = 0`，本来就只是「吃按下那一下」，视觉上不参与）✓。
        /// ⚠️ **z 的【那一段区间】**（0.02…0.07）没变（只把 6 个值按层序重新分配，并把原来那对
        /// `ZBtnText == ZBtnIcon` 的**并列**拆成 0.03/0.02 —— 原版图标压在字之上）⇒
        /// 「本面板相对别的 HUD 件（`HudImageZ` 0.3 / `UnitChatPanel.Z` 0.25 那一档）整体在**前面**」
        /// 这条**一个字没动**（`grep` 现核：战斗 HUD 里没有别的件落在 0.02…0.07 这一段）。</para>
        /// <para>uGUI 档**不用**这一份（兄弟序另有一套键）—— 见下面 <see cref="UZClose"/>。</para></summary>
        const float ZClose = 0.07f, ZPanelBg = 0.06f, ZFrame = 0.05f,
                    ZBtnBg = 0.04f, ZBtnText = 0.03f, ZBtnIcon = 0.02f;

        /// <summary>🔴 **uGUI 档的兄弟序键** —— 判据 = **原版 prefab 的子件序**（上面 <see cref="ZClose"/>
        /// 那条 doc 的 ①；`battlearena2` 现读，逐级抄下来）：
        /// <code>
        /// ChatPopup      : [0] CloseChatPopup · [1] ChatButtons
        /// ChatButtons    : [0] BG            · [1] BGFrame     · [2] Buttons
        /// (BGFrame)      : [0] BGFrame Top   · [1] Bottom      · [2] Right · [3] Left
        /// (Buttons)      : [0..5] ChatButton …
        /// ChatButton 行  : [0] bg            · [1] Text (TMP)  · [2] button(图标)
        /// </code>
        /// **不是**上面那一份世界 z。
        /// <para>`HudCanvas.Add` 是「z **大的**排**前面** = 画在**底下**」⇒ 这里按**从底到顶**
        /// 给**递减**的键 ⇒ 逐件与原版对得上（⚠️ 同一个 `ChatButton` 行里 `Text` 在 `button` **之前**、
        /// 即**图标压在字之上** —— 那是原版如此；两者不重叠，实际看不出，但**别顺手改成"字在上"**）。</para>
        /// <para>⚠️ 为什么**另立一套键**而不是直接沿用世界档那一份：世界 z 是**浮点世界坐标**、键是
        /// **兄弟序整数**，两者单位与方向都不同（世界档 `z` 大 = 远 = 先画；`Add` 的键大 = 先画）。
        /// 🔴 **2026-10-16（补正 `A12`）**：世界那一份**已按同一套层序重排**（见 <see cref="ZClose"/>）
        /// ⇒ 两档现在**同向**（这是本该如此；原来世界档那一份与原版是反的、只在画布上纠正过）。</para></summary>
        const float UZClose = 6f, UZPanelBg = 5f, UZFrame = 4f,
                    UZBtnBg = 3f, UZBtnText = 2f, UZBtnIcon = 1f;

        /// <summary>🔴 **2026-10-20（`T09` 步骤 A3-e）：6 条【透明命中条】的兄弟序键**。
        /// <para>判据 = 原版 `ChatButton` 行**根节点**上那颗 `Image`（`MB 4660`：`sprite = null`、
        /// `m_Color.a = 0`、**`m_RaycastTarget = 1`**、`m_RaycastPadding = (0,0,0,0)`；`RT_2861`
        /// = **603.6 × 48**）—— 原版那一行**整条**都是命中面（可见底图 `MB 5043` 只是它的一部分）。</para>
        /// <para>**取 0.5**：本组现有键是 `1..6`（1 = 图标最上、6 = 关闭区最下）⇒ 取比 1 小 ⇒ **排最上**
        /// （`HudCanvas.Group.Add`：z 小的插在后面 = 画在上面）。命中条**不画**（alpha 0），
        /// 它只需要在**射线**上压过 `_close`（否则点钮会先撞上全屏关闭区）。</para></summary>
        const float UZBtnHit = 0.5f;

        class Btn
        {
            /// <summary>🔴 **2026-10-16（`T09` 批 3）：类型 `ImageQuad` → `HudPiece`**（世界 quad 或 uGUI 件，二选一）。</summary>
            public HudPiece bg, icon;
            /// <summary>🆕 **2026-10-20（`T09` A3-e）**：uGUI 档那一条**透明命中条**（= 原版 `MB 4660`
            /// 那一颗；世界档恒 `null` —— 世界档的命中仍走 `Hit` 的手搓矩形）。</summary>
            public HudPiece hit;
            public Label text;
            public Rect rect;                 // 绝对矩形（每次 RefreshLayout 重算）
        }

        readonly List<Btn> _btns = new List<Btn>();
        HudPiece _bg, _frameTop, _frameBottom, _frameLeft, _frameRight;
        HudPiece _close;                      // 全屏关闭区（等效 `NonDrawingGraphic`）
        bool _built;

        // ── 🆕 2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e）：uGUI 输入层 ────────────────────────
        //  判据与事件口径 → 文件头「本面板有可点件」那一段（逐组件现读）。
        //  ⛔ 这一层**只发事件**，不做动作：「说哪句话」归 `BattleDriver`（它才有 `RuleCore` 与语音表）。
        BattleHudButton[] _btnButtons;        // 6 颗钮的点击适配器（`IPointerClickHandler`）
        EventTrigger _closeTrigger;           // 全屏关闭区（原版 `EventTrigger` + `PointerDown`）
        bool _closePending;                   // 挂起的关闭（见 `LateUpdate` 的 doc）
        int _uguiButtonClicks;                // 6 颗钮收到几次 uGUI 点击（灭自证）
        int _uguiCloseCount;                  // 条外关闭落了几次
        int _lastUguiButton = -1;             // 最近一次 uGUI 点中的钮（-1 = 没点过）

        // ── 🆕 2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e1）：6 颗钮的**悬停 / 按下**反馈 ──────────
        //  原版是**两套并存、各打各的图形**（逐组件现读 `bundle_scenes_scenes_battlearena1`；
        //  arena2 同名件逐值相同）：
        //   · **ColorTint** —— `MB 4112` = 行根 GO 940 上的 `UnityEngine.UI.Button`，`m_Transition = 2`；
        //     `m_TargetGraphic` → `MB 5043` = 可见底图 `40k_voicelines_bt_R`（那颗不在行根上、
        //     是行根的子件）。`m_Colors` = 白 / `0.9607843` / `0.7843137` / α`0.5019608`、
        //     `m_FadeDuration = 0.1` —— 与 `BattleButtonFx` 的默认四格**逐值相同**（⛔ 别另抄一份）。
        //   · **SpriteSwap** —— `MB 4652` = `ChatPopupButton : EverguildButton`，`m_Transition = 1`，
        //     但它的 **`m_SpriteState` 四个全 null**（⛔ 别照「四张 leftButton* 就是 SpriteState」写）。
        //     真正的换图在 `ChatPopupButton.LateUpdate` 里：读 `buttonConfig`（= `MB 4112`）的选中态，
        //     给 `buttonImage`（`MB 4270` = 图标 `40k_voicelines_bt_L`）`set_sprite` 成
        //     `leftButtonNormal / Hover / Pressed / Disable`（四个字段就在 `MB 4652` 上，
        //     Normal 那张 pid `-1396804370094666590` 与 `MB 4270.m_Sprite` 是**同一张**）。
        //     ⇒ 我们用 `BattleButtonFx.Swap` 挂**图标**，四张图按名字取
        //     `40k_voicelines_bt_L` / `_hover` / `_pressed`（Disable 那张 `_blocked` 本件用不到：
        //     原版这 6 颗 `m_Interactable = 1`，冷却在**开面板那颗** `ChatButton` 上、不在这里）。
        //  ⚠️ **两套必须各写各的件**：ColorTint 打底图、SpriteSwap 打图标 —— 对同一件写两遍 = 后写盖前写。
        //  ⚠️ **淡入淡出与 ColorTint 会抢同一个 `Image.color`**：见 `ApplyButtonFadeAlpha` 那条 doc。
        BattleButtonFx[] _fxBtnBg, _fxBtnIcon;

        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：uGUI 那条路上「第 `i` 颗台词钮被点了」
        /// （松手那一帧；原版 `ChatPopupButton : EverguildButton` ⇒ `Button.onClick`）。
        /// <para>⚠️ **订阅者要做的是** `SpeakChatAt(i)`（驱动层现成的口）；**本轮没有订阅者**（白名单只到本文件），
        /// 真实点击仍由 `BattleDriver.HandleChatPopup` 那条手搓路处理 —— 合并配方 → 报告 §三。</para></summary>
        public event Action<int> ButtonClicked;

        /// <summary>🆕 **2026-10-20（`T09` A3-e）**：条外关闭**已经落下**（面板已 `Hide()`）。
        /// 目前没有订阅者 —— 留这个口是给驱动层合并时收尾（原版这一路只调 `Hide`，没有别的副作用）。</summary>
        public event Action CloseRequested;

        float _alpha;
        /// <summary>面板**开着**没有 —— **不是**「alpha > 0」。</summary>
        bool _open;
        /// <summary>正在淡出（`Hide()` 之后那一小段）。原版这时 `interactable` 已经是 false。</summary>
        bool _fadingOut;

        /// <summary>面板开着没有。🔴 **自检看这个、模态判据也用它**。</summary>
        /// ⚠️ **2026-09-18 更正**：原来写的是 `_alpha > 0.001f` —— 那样 `Show()` 之后要等一次
        ///    `Advance` 才为真，于是「刚打开就点」全部落空（自检当场抓到 4 条红）。
        ///    原版 `Clicked` 是**立刻** `SetActive(true)` + `canvasGroup.interactable = true`，
        ///    淡入只是**外观**，不影响「开没开」—— 这里照它改。
        public bool Visible { get { return _open; } }
        /// <summary>现在收不收点击。原版 `Hide()` 一开头就把 `interactable` 置 0（**不等淡出完**）。</summary>
        public bool Interactable { get { return _open && !_fadingOut; } }
        /// <summary>当前整块面板的透明度（淡入淡出中会介于 0..1）。</summary>
        public float Alpha { get { return _alpha; } }
        /// <summary>建起来了没有（图都取到了）。</summary>
        public bool Ready { get { return _built && _bg != null; } }
        /// <summary>最近一次 `SetPointer` 命中的钮（-1 = 没命中）。自检用。</summary>
        public int LastHit { get; private set; }
        /// <summary>最近一次**点击**（按下后抬起）落在第几个钮上。-1 = 没点到钮。</summary>
        public int LastClicked { get; private set; }

        /// <summary>6 个钮的绝对矩形（自检拿它比原版坐标）。</summary>
        public Rect ButtonRect(int i)
        {
            return (i >= 0 && i < _btns.Count) ? _btns[i].rect : new Rect();
        }

        /// <summary>第 i 颗钮**实际印出来的那串字**（自检用）。
        /// 🔴 **2026-10-14（`A1845`/`A1804`）**：读的是**真挂在那颗 `Label` 上的字符串**（`Label.Text`），
        /// ⛔ 不是重算一遍 `LabelFor(i)` —— 后者等于拿实现证明实现。</summary>
        public string TextAt(int i)
        {
            return (i >= 0 && i < _btns.Count && _btns[i].text != null) ? _btns[i].text.Text : "(无)";
        }

        /// <summary>🔴 **自检用（`EW3` · 灭自证口）**：第 i 颗钮**文字真正的不透明度** —— 读的是**挂在它上面的
        /// `Label.color.a`**（那个值最终就是 `TMP` 的 `_tmp.color.a`），⛔ **不是**重算一遍 `_alpha`
        /// （那等于拿实现证明实现）。
        /// <para>期望：面板淡入淡出中它必须**与 `Alpha` 同步**（`Show()` 后 0 → 1 · `HideImmediate()` 后 = 0）；
        /// 改坏了（漏掉 `SetAlpha` 里那一句）⇒ 它会**恒等于 1** 而 `Alpha` 照常变 —— 两条一比就露。</para>
        /// <para>越界 / 那件没建出来（TMP 字体缺失）⇒ `-1`（⛔ 不返 1：那与「真的满不透明」分不开）。</para></summary>
        public float TextAlphaAt(int i)
        {
            return (i >= 0 && i < _btns.Count && _btns[i].text != null) ? _btns[i].text.color.a : -1f;
        }

        public static ChatPopupPanel Create(Transform parent)
        {
            return Create(parent, null);          // 不传宿主 ⇒ **世界档**（与加本档之前逐位相同）
        }

        /// <summary>🔴 **2026-10-16（`T09` 批 3）**：`uguiHost` = **本面板那块子画布的 `RectTransform`**
        /// （由调用方建/取：`HudCanvas.EnsureSubCanvas(null, <see cref="SubCanvasName"/>,
        /// <see cref="SortingLayerId"/>, <see cref="SortingOrder"/>)`）。
        /// <para>`null` ⇒ **世界档**（= 改本件之前的行为，逐位相同）；非 null ⇒ **uGUI 档**。</para>
        /// <para>⛔ **宿主不在任何 `Canvas` 下时出声并且【不建】** —— 半迁移态比不迁移更坏：一张 `Image`
        /// 挂在没有 `Canvas` 的父链下**一个像素都不画**，而调用点会以为建成功了。</para></summary>
        public static ChatPopupPanel Create(Transform parent, Transform uguiHost)
        {
            bool ugui = uguiHost != null;
            if (ugui && uguiHost.GetComponentInParent<Canvas>() == null)
            {
                Debug.LogError("[Battle] `ChatPopupPanel.Create` 的 `uguiHost` 不在任何 `Canvas` 下"
                             + " ⇒ **本面板不建**（⛔ 不退回世界空间 quad —— 半迁移态在 16:9 下看不出来）");
                return null;
            }
            // 🔴 **2026-10-11（A218）**：根节点是 `RectTransform` + 写 `sizeDelta`。
            //    判据 = 原版同名件 `ChatPopup` 实读：`RectTransform` · `anchor (0,0) 重合` ·
            //    `pivot (0,0.5)` · **`m_SizeDelta = (815.044, 475.470)`**（`bundle_scenes_scenes_battlearena2`，
            //    2026-10-16 现读；旧记写成 `battlearena1`，两包同名件逐值相同）⇒ 就是上面那个 `Panel`
            //    的宽高（**同一份数**，⛔ 别另立一个）。
            //    🔴 **2026-10-16（补正 `A12`）：锚点现在【也】照原版写了**（原来只写了尺寸、锚点用中心锚）——
            //    `anchor (0,0)` + `pivot (0,0.5)` + `ap = HudCanvas.SetAnchorPx(…)` 算出来的 `(0, 479.815)`。
            //    改坏法：删掉下面那几行 ⇒ `Editor/BattleScene.cs` §A218「`ChatPopup` 的 rect = 815.04×475.47」红
            //    （那条断的是 `sizeDelta`，锚点改了不影响它）；
            //    锚点改回中心锚 ⇒ 非 16:9 下整块面板与子件一起偏右/偏上（4:3 偏 180 画布 px）。
            var go = new GameObject("ChatPopup", typeof(RectTransform));
            // 🔴 **2026-10-16（`T09` 批 3）**：世界档挂 `parent`、uGUI 档挂**宿主子画布**（⛔ 不是 `hudRoot`
            //    —— 那是反向补震用的，挂它下面会被补偿两次；同 `ErrorMessageBanner.Create` 那条）。
            go.transform.SetParent(ugui ? uguiHost : parent, false);
            // 尺寸/锚点：**同一份数、两种单位 + 两种锚点** ——
            // 世界档 `MenuDraw.SetPxSize`（体内 `÷108`、中心锚；⚠️ 世界档那棵树里**子件的坐标是绝对的**
            //   `localPosition`（见 `Put` 的世界那一支），所以**本节点的锚点与位置不参与排布** ⇒ 保持原样不动，
            //   免得把子件一起搬走）；uGUI 档**按原版那对锚点写**（`sizeDelta` 直接吃 px）。
            // ⛔ 别在 uGUI 档沿用 `MenuDraw.SetPxSize`（1/108、不报错）。
            if (ugui)
            {
                var prt = (RectTransform)go.transform;
                // 🔴 **2026-10-16（`G5` 收口波 · 消重）**：**点锚 + `anchoredPosition` 走共用口**
                //   （原来这里是「手写点锚那两行」+ 一个私有常量 `PanelApPx`，式子与共用口**逐位相同**）。
                //   算得 `ap = (0, 479.815)`（原版 `RT 2881` 是 `(0, 479.81005859375)` —— 差 0.0049 px，
                //   成因见下面 `PanelOriginPx` 那段）。
                //   ⚠️ 它**只碰** `anchorMin/Max` 与 `anchoredPosition` ⇒ 下一行 `pivot`、再下一行 `sizeDelta`
                //   照旧各写各的；三者是 `RectTransform` 的**独立字段**（谁先谁后不改终值，
                //   ⛔ 别误以为「改了锚点会反推 `anchoredPosition`」—— Unity 的锚点 setter 不重算它）。
                HudCanvas.SetAnchorPx(prt, PanelAnchorX, PanelAnchorY, PanelOriginPx.x, PanelOriginPx.y);
                prt.pivot = new Vector2(PanelPivotX, PanelPivotY);                         // 原版 `(0,0.5)`
                prt.sizeDelta = new Vector2(Panel.width, Panel.height);                    // 原版 `815.044 × 475.470`
            }
            else MenuDraw.SetPxSize(go.transform, Panel.width, Panel.height);
            var p = go.AddComponent<ChatPopupPanel>();
            p._ugui = ugui;                        // ⚠️ 必须在 `Build()` **之前**赋值（`Build` 里按它算单位）
            p.Build();
            return p;
        }

        void Build()
        {
            if (_built) return;
            _built = true;
            CardArt.Load();

            // 全屏关闭区先建 ⇒ 渲染顺序在**最底下**（面板主体压着它）
            //   原版这个节点是 `ChatPopup` 的**子物体**、`EventTrigger` 绑 `PointerClick → Hide`
            //   ⚠️ 我们**不接 Unity 的射线**（本工程的点击一律是「自己算 `Contains(世界坐标)`」，
            //      `ImageQuad` 上根本没有 `raycastTarget` 这个口子）⇒ 这里只要一张**全透明的图**占位，
            //      命中由下面的 `SetPointer` 按同一套矩形判 —— 等效原版那个 `NonDrawingGraphic`。
            // 🔴 **2026-10-16（`T09` 批 3）**：uGUI 档 = **一张中心锚的 `Image`**（尺寸由 `Put` 每次刷）
            //    —— `HudCanvas.MakeImageInParent` 的 `z` 形参就是**画布上那个兄弟序键**（`UZClose` = 本面板最大的键
            //    ⇒ 排最前 = 画在最底下 ✓，判据 = 原版 `ChatPopup` 的子件序 `[0] CloseChatPopup`）。
            _close = _ugui
                   ? HudCanvas.MakeImageInParent(transform, Solid(), "CloseChatPopup",
                                                 0f, 0f, 1f, 1f, UZClose, Color.white)
                   : HudPiece.Of(ImageQuad.Create(transform, Solid(), Vector3.zero, 1f,
                                                  new Vector2(0.5f, 0.5f), "CloseChatPopup"));
            if (_close != null) _close.SetTint(new Color(1f, 1f, 1f, 0f));   // 只看不画（两档同一条：uGUI 档改的是 `Image.color`）

            _frameTop = Make("BGFrame_Top", "40k_UnitChat_Background_Top", UZFrame);
            _frameBottom = Make("BGFrame_Bottom", "40k_UnitChat_Background_Bottom", UZFrame);
            _frameLeft = Make("BGFrame_Left", "40k_UnitChat_Background_Left", UZFrame);
            _frameRight = Make("BGFrame_Right", "40k_UnitChat_Background_Right", UZFrame);
            _bg = Make("BGFrame_BG", "White_Square", UZPanelBg);
            if (_bg != null) _bg.SetTint(BgColor);

            for (int i = 0; i < ButtonCount; i++)
            {
                var b = new Btn();
                b.bg = Make("ChatButton" + (i + 1) + "_bg", "40k_voicelines_bt_R", UZBtnBg);
                b.icon = Make("ChatButton" + (i + 1) + "_icon", "40k_voicelines_bt_L", UZBtnIcon);
                // 🔴 **2026-10-16（`T09` 批 3）**：`Label.Create`（世界空间）→ **`HudCanvas.MakeText`（画布）**。
                //   ⚠️ **位置是【设计 px】**（`0,0` = 面板中心；真正的位由 `Put` 在 `RefreshLayout` 里给）。
                //   ⚠️ `MakeText` 拿不到 TMP 字体时**返回 `null`**（出声、⛔ 不退化成点阵）⇒ `_btns[i].text`
                //     可能是 null（下游都有空判：`TextAt` / `Put` / `SetAlpha` 里那几处）。
                b.text = _ugui
                       ? HudCanvas.MakeText(transform, LabelFor(i), 0f, 0f, 1, Color.white,
                                            new Vector2(0f, 0.5f), UZBtnText, "ChatButton" + (i + 1) + "_Text", face: TmpFont.Face.LatinCardBody)
                       : Label.Create(transform, LabelFor(i), Vector3.zero, 1, Color.white,
                                      new Vector2(0f, 0.5f), "ChatButton" + (i + 1) + "_Text");
                // 🔴 2026-10-17（A1024③ 第 2 批）：原版 `ChatButtons/.../Text (TMP)` = Pragati。
                //    两档都过这里（uGUI 档已在 `MakeText` 里换过 ⇒ `SetFace` 同面早退，逐位等价）。
                if (b.text != null) b.text.SetFace(TmpFont.Face.LatinCardBody);
                _btns.Add(b);
            }

            // 🆕 **2026-10-20（`T09` 步骤 A3-e）**：uGUI 档装输入层（6 条命中条 + 条外关闭区）。
            //    ⚠️ 必须排在 `RefreshLayout()` **之前**建出来 —— 那一次会把 6 条命中条摆到钮矩形上。
            //    世界档不建（世界档的命中仍走 `Hit` 的手搓矩形，⛔ `ImageQuad` 没有 `raycastTarget` 这个概念）。
            if (_ugui) SetupUguiInput();

            HideImmediate();
            RefreshLayout();
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🔴 **2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e）：uGUI 输入层**
        //  判据逐条 → 文件头「本面板有可点件」②（`MB 4112/4652` 的 `Button.onClick` 与 `MB 4913` 的
        //  `EventTrigger eventID=2 = PointerDown`）。**两件事分开**（原版就是两种触发沿，见 `PointerDownAt`）：
        //    · 6 颗钮 = **松手**（`IPointerClickHandler`，挂 `BattleHudButton`）
        //    · 条外关闭 = **按下**（`EventTrigger.PointerDown`）
        //  ⛔ 本层**不改既有行为**：手搓的 `PointerDownAt`/`PointerUpAt` 仍由驱动每帧喂（合并配方 → 报告 §三）。
        // ══════════════════════════════════════════════════════════════════════════════════════

        /// <summary>建 6 条透明命中条 + 把全屏关闭区变成射线靶。**只在 uGUI 档调一次**（`Build` 里）。
        /// <para>取不到任何一件都 **`LogError`**（⛔ 不许静默半迁移 —— 那会让「看得见的钮点不动」）。</para></summary>
        void SetupUguiInput()
        {
            _btnButtons = new BattleHudButton[ButtonCount];
            _fxBtnBg = new BattleButtonFx[ButtonCount];
            _fxBtnIcon = new BattleButtonFx[ButtonCount];
            // 图标换图那三张（原版 `MB 4652` 的 `leftButtonNormal/Hover/Pressed`；见字段区那条 doc）
            var iconNormal = CardArt.Ui("40k_voicelines_bt_L");
            var iconHover = CardArt.Ui("40k_voicelines_bt_L_hover");
            var iconPressed = CardArt.Ui("40k_voicelines_bt_L_pressed");
            for (int i = 0; i < ButtonCount && i < _btns.Count; i++)
            {
                int idx = i;                       // ⚠️ 闭包捕获：⛔ 别直接捕 `i`

                // ① 视觉反馈（**两套各自打各自的图形**，判据见字段区那条 doc）——
                //    与命中条无关，先建：命中条建不起来时这两套照样该有（钮点不动 ≠ 钮不该有反馈）。
                _fxBtnBg[i] = BattleButtonFx.Tint(_btns[i].bg, "ChatButton" + (i + 1) + "_bg");
                _fxBtnIcon[i] = BattleButtonFx.Swap(
                        _btns[i].icon, "ChatButton" + (i + 1) + "_icon",
                        _btns[i].icon != null ? _btns[i].icon.Texture as Texture2D : iconNormal,
                        iconHover, iconPressed);
                if (_fxBtnBg[i] == null || _fxBtnIcon[i] == null)
                    Debug.LogError("[ChatPopup] 第 " + (i + 1) + " 颗钮的**悬停/按下反馈**没建起来"
                                 + "（底图或图标缺失）⇒ 这一颗悬停/按下时**不变色也不换图**");

                var hit = HudCanvas.MakeImageInParent(
                        transform, Solid(), "ChatButton" + (i + 1) + "_Hit",
                        0f, 0f,           // 先给 0（`RefreshLayout` 会按 `ButtonRect(i)` 重摆）
                        1f, 1f,
                        UZBtnHit, new Color(1f, 1f, 1f, 0f));   // alpha 0 = 原版那颗 `sprite = null` 的隐形条
                if (hit == null)
                {
                    Debug.LogError("[ChatPopup] 第 " + (i + 1) + " 颗钮的**命中条**没建起来"
                                 + "（`HudCanvas.MakeImageInParent` 返回 null）⇒ **这颗钮在 uGUI 上点不动**");
                    continue;
                }
                _btns[i].hit = hit;
                var btn = BattleHudButton.Attach(hit, () => OnUguiButtonClicked(idx));
                if (btn == null)
                {
                    Debug.LogError("[ChatPopup] 第 " + (i + 1) + " 颗钮的**点击适配器**挂不上"
                                 + "（见 `BattleHudButton.Attach` 自己那条日志）⇒ **这颗钮在 uGUI 上点不动**");
                    continue;
                }
                _btnButtons[i] = btn;

                // ② 悬停/按下的事件（原版 `Selectable` 的 enter/exit/down/up 四个口；
                //    挂在**整行命中条**上 —— 原版的射线靶就是行根那颗透明 `Image`，见文件头 ①）。
                //    · Exit 把 pressed 一起清掉：原版 `Selectable.OnPointerExit` 回到 Normal（不是 Pressed）。
                //    · Up 只清 pressed（指针仍在这颗钮上 ⇒ 保持 Highlighted）。
                AddTrigger(hit, EventTriggerType.PointerEnter, _ => { if (Interactable) SetButtonHover(idx, true); });
                AddTrigger(hit, EventTriggerType.PointerExit, _ => { SetButtonHover(idx, false); SetButtonPressed(idx, false); });
                AddTrigger(hit, EventTriggerType.PointerDown, _ => { if (Interactable) SetButtonPressed(idx, true); });
                AddTrigger(hit, EventTriggerType.PointerUp, _ => SetButtonPressed(idx, false));
            }

            if (_close == null)
            {
                Debug.LogError("[ChatPopup] 全屏关闭区 `CloseChatPopup` 没建起来 ⇒ **条外关闭在 uGUI 上失效**"
                             + "（手搓那条路仍在，见 `PointerDownAt`）");
                return;
            }
            if (!_close.SetRaycastTarget(true))
                Debug.LogError("[ChatPopup] 全屏关闭区 `CloseChatPopup` 置射线靶**失败** ⇒ 条外关闭在 uGUI 上失效");
            _closeTrigger = AddPointerDown(_close, OnClosePointerDown);
            if (_closeTrigger == null)
                Debug.LogError("[ChatPopup] 全屏关闭区的 `EventTrigger` **挂不上** ⇒ 条外关闭在 uGUI 上失效");
        }

        /// <summary>给一件 uGUI 图挂 **`EventTrigger` + 指定事件**（原版 `MB 4913` 的同形）。
        /// <para>⚠️ **不新写 `MonoBehaviour`**：本仓硬规矩「类名 == 文件名」（`!u!115` 根治）——
        /// 在本文件里再声明一个 `MonoBehaviour` 会破坏它；`EventTrigger` 是 uGUI 自带组件、不新增脚本资产
        /// （同 `BattleLogPanel` A3-c 的配方）。</para>
        /// <para>⚠️ 同一个 GO 上已有 `EventTrigger` 就**复用**（再 `AddComponent` 会得到两个实例、
        /// 两个都收事件 ⇒ 一次悬停写两遍）。</para></summary>
        static EventTrigger AddTrigger(HudPiece piece, EventTriggerType type,
                                       UnityEngine.Events.UnityAction<BaseEventData> cb)
        {
            if (piece == null || piece.gameObject == null) return null;
            var go = piece.gameObject;
            var trig = go.GetComponent<EventTrigger>();
            if (trig == null) trig = go.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(cb);
            trig.triggers.Add(entry);
            return trig;
        }

        /// <summary>原版全屏关闭区那一条（`MB 4913` 的 `eventID = 2 = PointerDown`）。</summary>
        static EventTrigger AddPointerDown(HudPiece piece, UnityEngine.Events.UnityAction<BaseEventData> cb)
        {
            return AddTrigger(piece, EventTriggerType.PointerDown, cb);
        }

        // ── 🆕 2026-10-20（`T09` A3-e1）：悬停 / 按下状态（**只改外观，⛔ 不碰点击/关闭**）──────

        /// <summary>第 `i` 颗钮的悬停态（`BattleButtonFx` 自带「值没变就早退」）。</summary>
        void SetButtonHover(int i, bool on)
        {
            if (_fxBtnBg != null && i >= 0 && i < _fxBtnBg.Length && _fxBtnBg[i] != null) _fxBtnBg[i].SetHover(on);
            if (_fxBtnIcon != null && i >= 0 && i < _fxBtnIcon.Length && _fxBtnIcon[i] != null) _fxBtnIcon[i].SetHover(on);
            ApplyButtonFadeAlpha();      // fx 写的是 RGB（α 恒 1）⇒ 当场把淡入淡出那一档 α 补回去
        }

        /// <summary>第 `i` 颗钮的按下态（原版 `Selectable.Pressed` = 指针在件上并按着）。</summary>
        void SetButtonPressed(int i, bool on)
        {
            if (_fxBtnBg != null && i >= 0 && i < _fxBtnBg.Length && _fxBtnBg[i] != null) _fxBtnBg[i].SetPressed(on);
            if (_fxBtnIcon != null && i >= 0 && i < _fxBtnIcon.Length && _fxBtnIcon[i] != null) _fxBtnIcon[i].SetPressed(on);
            ApplyButtonFadeAlpha();
        }

        /// <summary>把 6 颗钮的反馈**全部复位**（面板收起时用；否则指针停在某颗上再关面板，
        /// 下次打开它还挂着 Highlighted —— 原版 `SetActive(false)` 会一并清掉 `Selectable` 的状态）。</summary>
        void ResetButtonFeedback()
        {
            if (_fxBtnBg == null && _fxBtnIcon == null) return;      // 世界档：没建过
            for (int i = 0; i < ButtonCount; i++) { SetButtonHover(i, false); SetButtonPressed(i, false); }
        }

        /// <summary>推进 0.1 s 的颜色渐变（`BattleButtonFx` 的 `ColorTint` 那一半）。
        /// <para>🔴 **Play 里由 `LateUpdate` 每帧调**；**批处理没有帧循环** ⇒ 自检要推进时手动调。
        /// ⚠️ `!Application.isPlaying` 时 `BattleButtonFx` 本来就是**立刻到位**（`Retarget` 里那一条），
        /// 所以批处理不调它也不会「停在旧色」。</para></summary>
        public void TickButtonFeedback()
        {
            if (_fxBtnBg != null)
                for (int i = 0; i < _fxBtnBg.Length; i++) if (_fxBtnBg[i] != null) _fxBtnBg[i].Tick();
            if (_fxBtnIcon != null)
                for (int i = 0; i < _fxBtnIcon.Length; i++) if (_fxBtnIcon[i] != null) _fxBtnIcon[i].Tick();
        }

        /// <summary>🔴 **把淡入淡出的 `alpha` 补回 6 张钮底** —— `BattleButtonFx.Tint` 只写 RGB 那一半
        /// （它写的是「自己的基准色 × 状态色」，基准色是建件时的白、α 恒 1）。
        /// <para>**为什么必须补**：本面板的淡入淡出（`SetAlphaOn`）与 ColorTint **写的是同一个
        /// `Image.color`**，而 `LateUpdate` 跑在 `BattleDriver.Update`（`Advance` 那一跳）之后
        /// ⇒ 不补的话，`TickButtonFeedback` 会把 `alpha` 顶回 1，**面板收起时 6 颗钮的底图不跟着淡出**
        /// （画面上剩 6 条亮着的长条，静默）。</para>
        /// <para>⚠️ 只碰这 6 张底图（⛔ 别顺手调 `SetAlpha(_alpha)`：那会把 6 行字与整个面板重写一遍，
        /// 点阵后端还会每帧重烘贴图）。</para></summary>
        void ApplyButtonFadeAlpha()
        {
            if (_fxBtnBg == null) return;                              // 世界档：没有 fx，`SetAlpha` 已是对的
            for (int i = 0; i < _btns.Count && i < _fxBtnBg.Length; i++)
            {
                var q = _btns[i].bg;
                if (q == null) continue;
                var c = q.Tint;                                        // fx 刚写进去的 RGB（= 基准 × 状态）
                c.a = _alpha;
                q.SetTint(c);
            }
        }

        /// <summary>6 颗钮的 uGUI 点击（**松手那一帧**，与 `Button.onClick` 同一条链）。
        /// <para>⛔ **不做动作**（说话归驱动）：只记计数 + 发 <see cref="ButtonClicked"/>。</para>
        /// <para>闸 = <see cref="Interactable"/>：原版 `Hide()` **一开头**就把 `CanvasGroup.interactable` 置 0，
        /// 淡出途中收不到点击 ⇒ 与本件 `Interactable` 逐字同义。</para></summary>
        void OnUguiButtonClicked(int i)
        {
            if (!Interactable) return;
            _uguiButtonClicks++;
            _lastUguiButton = i;
            if (ButtonClicked != null) ButtonClicked(i);
        }

        /// <summary>条外关闭区的 **按下**（原版 `CloseChatPopup` 的 `EventTrigger.PointerDown` → `Hide`）。
        /// <para>🔴 **不在这里当场 `Hide()`** —— 见 <see cref="LateUpdate"/> 的 doc（与驱动抢同一帧 = 点一下做两件事）。</para></summary>
        void OnClosePointerDown(BaseEventData _)
        {
            if (!Interactable) return;
            _closePending = true;
        }

        /// <summary>🔴 **为什么关闭要晚到 `LateUpdate`**（与 `BattleLogPanel` A3-c 同一条，判据逐字相同）：
        /// `EventSystem.Update` 与 `BattleDriver.Update` **都是默认执行顺序**（本工程没有给这两者写
        /// `[DefaultExecutionOrder]`）。若在 `EventSystem` 的回调里**当场** `Hide()`：
        /// 驱动先跑 ⇒ 没影响（它自己已经关了）；驱动后跑 ⇒ `HandleChatPopup` 看到 `Visible == false`
        /// ⇒ **不再吃掉这一下** ⇒ 同一次按下继续落到棋盘/手牌 = **静默的「点一下做两件事」**。
        /// Unity 保证**所有 `Update` 跑完**才跑 `LateUpdate` ⇒ 两种顺序都安全：
        /// 驱动先跑 ⇒ 它自己 `Hide()`（我们的挂起项随后空转/幂等重放）；驱动后跑 ⇒ 它照旧吃掉这一下，
        /// 我们随后关（幂等）。且 `LateUpdate` 与 `Update` **同帧**，不是「延迟一帧」。</summary>
        void LateUpdate()
        {
            if (_closePending) ApplyPendingClose();
            // 🆕 2026-10-20（`T09` A3-e1）：① 推进悬停/按下那 0.1 s 的颜色渐变（Play 里才真的在动）
            // ② 把淡入淡出的 alpha 补回 6 张钮底（`BattleButtonFx` 只写 RGB 那一半，见它那条 doc）。
            TickButtonFeedback();
            ApplyButtonFadeAlpha();
        }

        /// <summary>落下挂起的关闭（**批处理没有帧循环 ⇒ `LateUpdate` 不跑**，自检/批处理里手动调一次）。
        /// 返回 true = 这一次真的落了（`false` = 没有挂起的关闭）。</summary>
        public bool ApplyPendingClose()
        {
            if (!_closePending) return false;
            _closePending = false;
            _uguiCloseCount++;
            Hide();                              // 幂等：驱动已经关过时只是把同一个淡出再写一遍
            if (CloseRequested != null) CloseRequested();
            return true;
        }

        /// <summary>建一片图（位置/尺寸由 <see cref="Put"/> 给；这里只定名字、贴图与**画布上的兄弟序键**）。
        /// 🔴 **2026-10-16（`T09` 批 3）双模式**：uGUI 档 = `HudCanvas.MakeImageInParent`（**中心锚**、设计 px，
        /// 最后那个 `uguiZ` 是**兄弟序键**）；否则与改本件之前**逐字相同**（世界档的 z 到这里**不写** ——
        /// 它由 `Put` 落到 `localPosition.z`，那一份在 `RefreshLayout` 里）。
        /// <para>⚠️ **拿不到图 ⇒ 两档都返回 `null`**（`ImageQuad.Create` 与 `MakeImageInParent` 各自空判）
        /// —— 与改本件之前**同一个行为**（⛔ 没顺手加日志，免得世界档多出噪声）。</para>
        /// <para>⚠️ uGUI 档的出厂尺寸是 `1×1`（`Put` 每次刷都会重写），**在世界档是 `0.1` 世界单位** ——
        /// 两档那个「出厂值」都只为「先建出来、随后立刻被 `Put` 覆写」而存在（`RefreshLayout` 紧跟着跑）。</para></summary>
        HudPiece Make(string name, string art, float uguiZ)
        {
            var tex = CardArt.Ui(art);
            if (_ugui)
                return HudCanvas.MakeImageInParent(transform, tex, name, 0f, 0f, 1f, 1f, uguiZ, Color.white);
            return HudPiece.Of(ImageQuad.Create(transform, tex, Vector3.zero, 0.1f,
                                                new Vector2(0.5f, 0.5f), name));
        }

        /// <summary>1×1 纯色图（关闭区用）。</summary>
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
        Texture2D _solid;

        // ==================================================================
        //  摆放：绝对 px → 世界坐标
        //  ⚠️ **每次刷新重算**：这套坐标是屏幕的固定比例，只摆一次的话换分辨率就错位
        //     （HUD 那几个 Label 踩过一模一样的坑，见 `SkillPanel.RefreshLayout` 的注释）。
        // ==================================================================

        // 🔴 **2026-10-16（`G5` 收口波 · 消重）**：这一行原来是 `const float RefW = 1920f, RefH = 1080f;`
        //   —— `RefW` **已删**（现核：只剩那个已删掉的 `PanelApPx` 算式用它了 ⇒ 帧尺寸要取
        //   `HudCanvas.ReferenceResolution.x`）。`RefH` **留着**：它是**世界档尺寸尺**的分母
        //   （`VisibleHeight / RefH`，`Place` / 钮文字 / `Hit` 三处都用）—— 那一把尺是「整条世界路径」
        //   的口径、**不属于本次消重范围**（如实记在报告 §⑤），⛔ 别把它顺手并进共用口
        //   （两件事：一个是「画布锚点的帧」，一个是「世界档尺寸的换算尺」）。
        const float RefH = 1080f;

        // ── 🔴 本节点的锚点（判据 = 原版 `battlearena2/RectTransform/RectTransform_2881.json`）──────────
        /// <summary>本节点（`ChatPopup`）的**点锚** —— 原版 `m_AnchorMin == m_AnchorMax == (0,0)`
        /// = **贴画布左下角**（⛔ 别改成 `(0.5,0.5)`：那会把面板钉在**画布中心**，
        /// 非 16:9 下与「贴左下角」差半个多出来的画布 —— 4:3 下上下沿差 180 画布 px）。
        /// ⚠️ **子件【不】用这个锚**：原版子件锚在**本节点矩形的中心**
        /// （`CloseChatPopup` 的 `ap (2.45, 41.66)`、`ChatButtons` 的 `ap (371, 206)` 都是相对面板中心量的）
        /// ⇒ 我们的子件一律 `anchor (0.5,0.5)` + **相对面板中心**的 px（`ApInPanel`）。</summary>
        const float PanelAnchorX = 0f, PanelAnchorY = 0f;
        /// <summary>本节点的枢轴 —— 原版 `m_Pivot = (0, 0.5)` = 矩形**左边中点**（⛔ 不是中心：
        /// 「枢轴在哪」决定**本节点局部坐标的原点**在哪 —— `PxOf` / `ButtonWorldPos` 靠它）。</summary>
        const float PanelPivotX = 0f, PanelPivotY = 0.5f;

        /// <summary>本节点**枢轴**在 1920×1080 帧里的位置（左上原点、y 向下）——
        /// `(Panel.x + pivotX·W, Panel.y + (1 − pivotY)·H)` = **`(0, 600.185)`**。
        /// 它同时是**本节点局部坐标的原点**（`InverseTransformPoint` / `TransformPoint` 的那个帧）。</summary>
        static readonly Vector2 PanelOriginPx = new Vector2(Panel.x + PanelPivotX * Panel.width,
                                                            Panel.y + (1f - PanelPivotY) * Panel.height);
        /// <summary>本节点**矩形中心**在 1920×1080 帧里的位置（左上原点、y 向下）——
        /// **子件的锚点基准**（原版子件的 `ap` 就是相对它量的）。</summary>
        static readonly Vector2 PanelCenterPx = new Vector2(Panel.x + Panel.width * 0.5f,
                                                            Panel.y + Panel.height * 0.5f);
        // 🔴 **2026-10-16（`G5` 收口波 · 消重）：本文件原来有个私有常量 `PanelApPx` —— 【已删】。**
        //   它是 `static readonly Vector2 PanelApPx = new Vector2(PanelOriginPx.x − PanelAnchorX*RefW,
        //   RefH*(1f − PanelAnchorY) − PanelOriginPx.y);`（式子与 `UnitChatPanel.ApPx` 逐字相同、
        //   也 = 共享段 `HudCanvas.AnchorApPx`），**已整条收进共用口** —— 现在只有 `Create` 那一处用，
        //   就地写成 `HudCanvas.SetAnchorPx(prt, PanelAnchorX, PanelAnchorY, PanelOriginPx.x, PanelOriginPx.y)`。
        //   逐值不变（`Panel` / `PanelOriginPx` / `PanelAnchor*` 一个都没动）⇒ 仍算出 **`(0, 479.815)`**
        //   （原版 `RT 2881` 是 `(0, 479.81005859375)` —— 差 **0.0049 px**：
        //   我们取「与本件改之前**逐位相同**」那一支（硬要求），差值的成因与三档算式见报告 §④）。
        //   ⛔ **别在本文件再写一遍那个算式**（本仓红线：两处写同一条规则 = 迟早不一致）。

        /// <summary>一个件的**枢轴点**（绝对设计 px、左上原点 y 向下）→ 相对**本节点矩形中心**的
        /// `anchoredPosition`（设计 px、y **向上**）。
        /// 🔑 **子件锚在【面板矩形的中心】**（= 原版子件的锚法），而**面板矩形尺寸是固定的**（`sd` 不随屏变）
        /// ⇒ 这是**刚性平移**、任何宽高比下都与「跟面板走」逐位同义。
        /// ⛔ 别再写成「相对画布中心」（那正是改本件之前那一套：面板一离开屏心，子件就跟它分家）。</summary>
        static Vector2 ApInPanel(Rect r)
        {
            float cx = r.x + r.width * 0.5f, cy = r.y + r.height * 0.5f;
            return new Vector2(cx - PanelCenterPx.x, PanelCenterPx.y - cy);
        }

        /// <summary>绝对 px 矩形（左上原点）→ **世界档**的枢轴世界坐标 + 世界高度。
        /// 🔴 **2026-10-16（补正 `A12`）位置那一半**：按**本节点的点锚**（= 贴画布左下角）摆 ——
        /// `世界位置 = ToWorld(ax, ay) + ap × s`，`ap` = 该矩形中心相对那个角的偏移、
        /// `s = LayoutSpace.Px(LayoutSpace.Scale)`（推导与「为什么乘 `Scale`」→ `UnitChatPanel.Move` 的 doc）。
        /// **16:9 下与改之前逐位相同**（`Scale = 1`）。
        /// ⚠️ **尺寸那一半（`worldH`）没动**：世界档的尺是 `VisibleHeight/RefH`（整条世界路径共用、
        /// 也是 `T09` 要淘汰的那一档）—— 改它等于动整条世界路径，超出本件（锚点）范围，如实记在报告 §⑤。</summary>
        static void Place(Rect r, out Vector3 pos, out float worldH)
        {
            // 🔴 **2026-10-16（`G5` 收口波 · 消重）**：这里原来**又内联了一遍**那个算式
            //   （`new Vector2(r.x + r.width*0.5f − PanelAnchorX*RefW, RefH*(1f − PanelAnchorY) − (r.y + r.height*0.5f))`）
            //   —— 现在与 `Create` 那一处**同一个共用口**（`r` 的中心点即「枢轴点」那个输入量）。
            float cx = r.x + r.width * 0.5f, cy = r.y + r.height * 0.5f;
            var ap = HudCanvas.AnchorApPx(cx, cy, PanelAnchorX, PanelAnchorY);
            var o = LayoutSpace.ToWorld(PanelAnchorX, PanelAnchorY);
            float s = LayoutSpace.Px(LayoutSpace.Scale);
            pos = new Vector3(o.x + ap.x * s, o.y + ap.y * s, 0f);
            worldH = r.height * (LayoutSpace.VisibleHeight / RefH);
        }

        /// <summary>把一个件摆到**绝对设计 px 矩形** `r`（左上原点、1920×1080 那一套）上，层深度 `z`。
        /// 🔴 **2026-10-16（`T09` 批 3）双模式**：
        /// <list type="bullet">
        /// <item>uGUI 档 = `anchor (0.5,0.5)`（**本节点矩形的中心**）+ `anchoredPosition = ApInPanel(r)`
        /// （**相对面板中心**的设计 px、y 向上）+ `sizeDelta` **直接吃 px**；</item>
        /// <item>世界档 = `Place(…)`（按**本节点的点锚**摆）+ `VisibleHeight/RefH` 的尺寸。</item>
        /// </list>
        /// <para>⚠️ **两档的「非等比」是同一件事**：世界档靠 `SetWorldHeight + SetAspect`（原版 `m_PreserveAspect=0`），
        /// uGUI 档靠 `preserveAspect=false` 的 `Image` + 直接给 `(w,h)` ⇒ **画出来的矩形逐位相同**
        /// （少一次乘除 ⇒ `rect` 就是原版那个矩形）。⛔ 别改成「按贴图比例」。</para>
        /// <para>🔴 **2026-10-16（补正 `A12`）**：uGUI 档原来写的是 `localPosition = PosPx(…)`（**相对画布中心**）
        /// —— 那只在「本节点恰好位于画布中心」时成立（= 改本件之前那个中心锚）。锚点改成原版的
        /// `(0,0)`（贴左下角）之后，**局部坐标的原点变成枢轴（矩形左边中点）**，
        /// `localPosition` 与「相对面板中心的偏移」**不再同值** ⇒ 必须写 `anchoredPosition`
        /// （锚点恒是 `(0.5,0.5)` = 面板矩形中心 ⇒ `anchoredPosition` 就是「相对面板中心的 px」）。</para>
        /// <para>⛔ uGUI 档**不写 `localPosition.z`** —— 画布上写 z 会把件**推离画布平面**（层深度由建件口的 `z`
        /// → 兄弟序承担）。</para></summary>
        void Put(HudPiece q, Rect r, float z)
        {
            if (q == null) return;
            if (_ugui)
            {
                var rt = q.Rect;
                if (rt == null) return;
                rt.anchoredPosition = ApInPanel(r);
                rt.sizeDelta = new Vector2(r.width, r.height);
                return;
            }
            Vector3 p; float h;
            Place(r, out p, out h);
            q.transform.localPosition = new Vector3(p.x, p.y, z);
            q.SetWorldHeight(h);
            q.SetAspect(r.width / Mathf.Max(r.height, 0.0001f));   // **非等比**（原版就不是）
        }

        /// <summary>把整块面板按当前分辨率重摆一遍。</summary>
        public void RefreshLayout()
        {
            Vector3 p; float h;
            Place(Panel, out p, out h);

            // 关闭区：原版 4055.34 × 2114.44，相对面板中心 (2.45, 41.66) —— 盖满屏幕还多出去一截
            Put(_close, new Rect(Panel.x + 2.45f - 4055.34f / 2f, Panel.y + 41.66f - 2114.44f / 2f,
                                 4055.34f, 2114.44f), ZClose);

            Put(_bg, Bg, ZPanelBg);
            Put(_frameLeft, FrameLeft, ZFrame);
            Put(_frameRight, FrameRight, ZFrame);
            Put(_frameTop, FrameTop, ZFrame);
            Put(_frameBottom, FrameBottom, ZFrame);

            for (int i = 0; i < _btns.Count; i++)
            {
                var b = _btns[i];
                // 🔴 位置按**布局组排完之后**算（`Button0.y + i × 49.48`），**不是**序列化的 (0,0)
                var r = new Rect(Button0.x, Button0.y + i * ButtonPitch, Button0.width, Button0.height);
                b.rect = r;
                // 🆕 2026-10-20（`T09` A3-e）：命中条 = **整行** `603.6 × 48`（原版 `RT_2861`），
                //    ⛔ 不是可见底图那个 `550.4 × 44.5` 的内缩框。`Put` 对 `null` 直接返回 ⇒ 世界档零影响。
                Put(b.hit, r, UZBtnHit);
                Put(b.bg, new Rect(r.x + BtnBg.x, r.y + BtnBg.y, BtnBg.width, BtnBg.height), ZBtnBg);
                Put(b.icon, new Rect(r.x + BtnIcon.x, r.y + BtnIcon.y, BtnIcon.width, BtnIcon.height), ZBtnIcon);
                if (b.text != null)
                {
                    var tr = new Rect(r.x + BtnText.x, r.y + BtnText.y, BtnText.width, BtnText.height);
                    // 🔴 **2026-10-16（`T09` 批 3）**：字那一档也一样分模式 —— uGUI 档写 `anchoredPosition`
                    //   （**相对面板中心**的设计 px；`Label` 的 `RectTransform` 锚点是 `Label.CreateCore`
                    //   定死的 `(0.5,0.5)`）、字号就是 **30 设计 px**（`SetGlyphHeight` 的参数是「本档单位」）；
                    //   世界档走 `Place`（按点锚摆）。
                    //   ⛔ 漏了它 ⇒ 画布上那 6 颗钮的字只有 **0.278 px**（30 × 10/1080）、**一个字都不报**。
                    //   🔴 **补正 `A12`**：uGUI 档原来写的是 `localPosition = PosPx(…)`（**相对画布中心**）——
                    //   本节点锚点改成原版的（贴左下角）之后那个写法会**与面板分家**，见 `Put` 那条。
                    if (_ugui)
                    {
                        var rt = (RectTransform)b.text.transform;
                        rt.anchoredPosition = ApInPanel(tr);
                        b.text.SetGlyphHeight(30f);                                            // 原版 fs30（设计 px）
                    }
                    else
                    {
                        Place(tr, out p, out h);
                        b.text.transform.localPosition = new Vector3(p.x, p.y, ZBtnText);
                        b.text.SetGlyphHeight(30f * (LayoutSpace.VisibleHeight / RefH));       // 原版 fs30
                    }
                }
            }
            SetAlpha(_alpha);
        }

        // ==================================================================
        //  开关 / 淡入淡出（原版 `Clicked` / `Show` / `Hide`）
        // ==================================================================

        /// <summary>原版 `VoiceLinesPopupSelector.Clicked`：**开着就关、关着就开**。</summary>
        public bool Toggle()
        {
            if (Visible) { Hide(); return false; }
            Show();
            return true;
        }

        public void Show()
        {
            _open = true;                     // **立刻**算开（原版 `SetActive(true)` + `interactable=true`）
            // 🔴 **2026-10-21（真缺陷 · 同 `P1-3`）**：重开 ⇒ 丢掉上一帧还没落的待关。
            //   不清的话：`OnClosePointerDown` 挂起（`_closePending = true`）之后同帧又 `Show()`，
            //   本帧末尾 `LateUpdate` → `ApplyPendingClose()` 会把**刚开的**这扇窗关掉
            //   （再发一次 `CloseRequested` ⇒ 驱动那边也当玩家关了）。
            //   同款两处：`Battle/MultiCardDisplay.cs:694` · `Battle/CardDisplayWindow.cs:1211`（本件漏了这一句）。
            _closePending = false;
            _fadingOut = false;
            gameObject.SetActive(true);
            RefreshLayout();
            _fading = true; _fadeTarget = 1f;
        }

        public void Hide()
        {
            _open = false;                    // **立刻**算关（原版一开头就把 `interactable` 置 0）
            _fadingOut = true;
            // 🔴 **2026-10-20（W57 · `T09-FIX5` · P2-8）：照原版【不撤射线靶】。**
            //   原版 `VoiceLinesPopupSelector.Hide()` 只做一句 `CanvasGroup.set_interactable(0, 0)`
            //   （判据 = `d:/2/tools/decomp_full/VoiceLinesPopupSelector__Hide.c` 第 5 行），
            //   全屏关闭区 `CloseChatPopup` 的 `Graphic` 在整个 0.5 s 淡出期间**仍吃得掉射线**
            //   （`EventSystem` 只看 `raycastTarget`，不看 `CanvasGroup.interactable`）——
            //   即「原版淡出期间仍吃点击，但 `interactable=0` ⇒ 什么都不发生」。
            //   ⛔ 别再在这里加 `raycastTarget = false`：那是 `T09-FIX3` 的「手感优先」**有意偏离**，
            //   `T09-FIX5` 按原版回滚（审查 `W57_战斗线_T09_审查.md` §一 P2-8）。
            ResetButtonFeedback();            // 🆕 2026-10-20（A3-e1）：收起时清掉悬停/按下态
            if (!gameObject.activeSelf) { _fading = false; _fadingOut = false; SetAlpha(0f); return; }
            _fading = true; _fadeTarget = 0f;
        }

        /// <summary>不播淡出直接收（**批处理里用** —— 那里没有帧循环，`Update` 不会跑）。</summary>
        public void HideImmediate()
        {
            _open = false;
            _fadingOut = false;
            _fading = false;
            ResetButtonFeedback();            // 🆕 2026-10-20（A3-e1）：同上
            gameObject.SetActive(false);
            SetAlpha(0f);
        }

        float _fadeTarget;
        bool _fading;

        void SetAlpha(float a)
        {
            _alpha = Mathf.Clamp01(a);
            SetAlphaOn(_bg); SetAlphaOn(_frameTop); SetAlphaOn(_frameBottom);
            SetAlphaOn(_frameLeft); SetAlphaOn(_frameRight);
            for (int i = 0; i < _btns.Count; i++)
            {
                SetAlphaOn(_btns[i].bg); SetAlphaOn(_btns[i].icon);
                // 🔴 **2026-10-16（`EW3` · 既有缺陷补，铁律 11）**：**钮文字也是本组的一件** ——
                //   漏掉它 ⇒ 淡入时字先满不透明、淡出时字最后消失（见下面 `SetAlphaOn(Label)` 那条判据）。
                SetAlphaOn(_btns[i].text);
            }
        }

        void SetAlphaOn(HudPiece q)
        {
            if (q == null) return;
            var c = q.Tint;
            // `White Square` 那块有**自己的颜色**，别把底色冲掉
            if (q == _bg) c = BgColor;
            c.a = (q == _bg ? BgColor.a : 1f) * _alpha;
            q.SetTint(c);      // 两档同一个口：世界档 = `ImageQuad.color` · uGUI 档 = `Image.color`
        }

        /// <summary>🔴 **2026-10-16（`EW3` · 既有缺陷补，铁律 11）**：**6 颗钮的文字也参与淡入淡出。**
        /// 与上面那个 `HudPiece` 的重载同理，只是**文字那一路的口**是 `Label.SetColor`（不是 `HudPiece.SetTint`）。
        ///
        /// <para><b>为什么该淡（原版实读，不是照直觉补的）</b>：原版 `VoiceLinesPopupSelector.Show/Hide`
        /// 的淡入淡出是 `DOFade(this.group, …, timeToOpen/timeToClose)` —— `this.group` = 本组件序列化的
        /// **`CanvasGroup`**（`battlearena2/MonoBehaviour/MonoBehaviour_4429.json` 的 `group` PPtr =
        /// `CanvasGroup_3770`），而那个 `CanvasGroup` 挂在 **`ChatPopup` 根节点自己**身上
        /// （`CanvasGroup_3770.m_GameObject.m_PathID = 1015` = 托管 `MB_4429`/`RT_2881` 的那个 GO，
        /// 即 `GameObject/ChatPopup.json`，`m_Name = "ChatPopup"`）。`CanvasGroup.alpha` 乘的是**整棵子树
        /// 每一个 `Graphic`**，而 `m_IgnoreParentGroups = false`（`CanvasGroup_3770.json`）⇒ 组内**没有**
        /// 哪一层能豁免。6 个字节点 `Text (TMP)`（`RT 3753 / 3742 / 3074 / 3050 / 2821 / 3046`，各挂
        /// `m_Color (1,1,1,1)` 的 TMP 组件）正是那棵树的后代（我逐级走 `RT 2881 → ChatButtons(3363) →
        /// Buttons(3644) → 6 × ChatButton → Text (TMP)` 现读出来的）⇒ **原版这 6 行字是跟着淡的**。</para>
        ///
        /// <para><b>两档都对</b>：`Label.SetColor` 就是两档同一个口 —— `TMP` 后端（世界档与 uGUI 档**都**走它，
        /// uGUI 档拿不到 TMP 字体时 `Create` 直接返回 null、不会退化成点阵，见 `Label.CreateCore`）
        /// 写的是 `_tmp.color`；点阵后端（只有世界档可能走到）把颜色重烘进贴图。
        /// ⇒ **世界档（宿主 `null`）与画布档（子画布）逐条同义**，⛔ 没有「只在其中一档生效」的问题。</para>
        ///
        /// <para>⚠️ **基色只取 `a`**：钮文字的基色是建的时候给的 `Color.white`（`Build` 里那两处工厂的
        /// `Color.white` 实参）⇒ 基透明恒为 `1`，与上面 `q == _bg` 那个特例同理、**不做第三次特判**。
        /// 每次**用 `_alpha` 重写 `a`**（不是「在当前值上乘」）⇒ 反复调**不会ratchet**。</para>
        /// <para>⚠️ `text == null`（TMP 字体缺失那一档）⇒ 直接返回；**那不是静默失败** —— 建它的地方
        /// （`HudCanvas.MakeText` / `Label.CreateCore`）已经出过声了。</para></summary>
        void SetAlphaOn(Label t)
        {
            if (t == null) return;
            var c = t.color;          // 基色（⛔ 别在这里写死白色 —— 只借它的 RGB）
            c.a = _alpha;
            t.SetColor(c);            // 两档同一个口：`TMP` 后端写 `_tmp.color` · 点阵后端重烘
        }

        /// <summary>推进淡入淡出。**必须由 `BattleDriver.AdvanceTimeline` 调** ——
        /// 批处理没有帧循环，`Update` 不会跑（这条坑本项目踩过）。</summary>
        public void Advance(float dt)
        {
            if (!_fading) return;
            float next = Mathf.MoveTowards(_alpha, _fadeTarget, dt / FadeTime);
            SetAlpha(next);
            if (Mathf.Abs(next - _fadeTarget) < 0.0001f)
            {
                _fading = false;
                if (_fadeTarget <= 0f)
                {
                    _fadingOut = false;
                    gameObject.SetActive(false);   // 原版 `OnComplete` 里 `SetActive(false)`
                }
            }
        }

        // ==================================================================
        //  命中
        // ==================================================================

        /// <summary>世界坐标落在面板主体上吗（**不含**全屏关闭区）。</summary>
        public bool Contains(Vector3 world)
        {
            if (!Interactable) return false;
            for (int i = 0; i < _btns.Count; i++)
                if (Hit(_btns[i].rect, world)) return true;
            return false;
        }

        /// <summary>世界坐标落在第几个钮上（-1 = 没落在任何钮上）。</summary>
        public int ButtonAt(Vector3 world)
        {
            if (!Interactable) return -1;
            for (int i = 0; i < _btns.Count; i++)
                if (Hit(_btns[i].rect, world)) return i;
            return -1;
        }

        /// <summary>世界坐标落在**绝对设计 px 矩形** `r` 里吗。🔴 **命中判据只此一处**
        /// （`Contains` / `ButtonAt` 都走它）。
        /// <para>🔴 **2026-10-16（`T09` 批 3）双模式**：uGUI 档先把世界点折成**本面板的设计 px**（`PxOf`，
        /// 见它那条 doc —— ⛔ 不是 `LayoutSpace.ToPixel` 那一族），再比绝对矩形；世界档**逐字不变**
        /// （`Place` 那条换算 + `VisibleHeight/RefH`）。</para></summary>
        bool Hit(Rect r, Vector3 world)
        {
            if (_ugui)
            {
                var q = PxOf(world);
                if (float.IsNaN(q.x)) return false;      // 面板没建出来 ⇒ 不认（⛔ 不拿 (0,0) 当命中）
                return q.x >= r.x && q.x <= r.x + r.width
                    && q.y >= r.y && q.y <= r.y + r.height;
            }
            Vector3 p; float h;
            Place(r, out p, out h);
            float w = r.width * (LayoutSpace.VisibleHeight / RefH);
            return Mathf.Abs(world.x - p.x) <= w * 0.5f && Mathf.Abs(world.y - p.y) <= h * 0.5f;
        }

        /// <summary>🔴 **自检用（补正 `A12` 的【灭自证】口）**：本节点**实际锚在哪个角**
        /// （读的是 `RectTransform.anchorMin`，⛔ 不是我们的 `PanelAnchor*` 常量）。
        /// <para>期望值取**原版 prefab 的 `m_AnchorMin/Max`**（判据侧）：`(0, 0)`（贴画布左下角）。
        /// ⚠️ 反面那条：断言要**同时断「不是 `(0.5,0.5)`」** —— 否则「改回中心锚」这一档改坏了也不红。</para>
        /// <para>世界档（没有「锚点」这回事 —— 子件坐标是绝对的 `localPosition`）⇒ `(-1, -1)`
        /// （⛔ 不返 `(0,0)`：那与真正的 `anchor (0,0)` 分不开，会让「世界档也答 (0,0)」这条假绿）。</para></summary>
        public Vector2 PanelAnchorOf
        {
            get
            {
                if (!_ugui || _frame == null) return new Vector2(-1f, -1f);
                return _frame.anchorMin;      // `Create` 把 `anchorMin`/`anchorMax` 写成同一个点锚 ⇒ 读一个就够
            }
        }

        /// <summary>🔴 **世界坐标 → 本面板的【设计 px】（左上原点、y 向下）** —— uGUI 档命中判定与自检的帧。
        /// <para>走**本节点**的 `InverseTransformPoint`（单位 = 设计 px）⇒ 与「件真正画在哪」是同一个变换
        /// ⇒ **任何宽高比下都咬合**；16:9 下与世界档那套逐位同值（差 float 舍入）。</para>
        /// <para>🔴 **2026-10-16（补正 `A12`）**：本节点局部坐标的**原点 = 枢轴**（原版 `pivot (0,0.5)`
        /// = 矩形**左边中点**）⇒ 折回 1920 帧要加 `PanelOriginPx`（= `(0, 600.185)`），
        /// ⛔ **不再是** `(960, 540)`（那是「本节点在画布中心」那个旧前提）。</para>
        /// <para>⛔ **别用 `LayoutSpace.ToPixel` / `PxX`**：那是**世界帧**的逆（分层靠 z 那一套），
        /// 而画布在非 16:9 下**两轴系数不同**（`T09` §七·8）⇒ 量出来是假数、「看得见的地方点不到」。</para>
        /// <para>世界档（或没建出来）⇒ `(NaN, NaN)`：世界档的命中走 `Hit` 里那一条老路，这条口对它无意义。</para></summary>
        public Vector2 PxOf(Vector3 world)
        {
            if (!_ugui || _frame == null) return new Vector2(float.NaN, float.NaN);
            var l = _frame.InverseTransformPoint(world);        // 面板局部（= 设计 px、y 向上、原点 = 枢轴）
            return new Vector2(PanelOriginPx.x + l.x, PanelOriginPx.y - l.y);
        }

        /// <summary>🔴 **自检用（`T09` 批 3）**：第 i 颗钮**真正画在哪**（世界坐标）。
        /// <para>🔑 **断言必须用它、⛔ 别再用 `LayoutSpace.ToWorld(ButtonRect(i)…)` 自己换算** ——
        /// 那个算的是**世界帧**的设计 px，而本面板在 uGUI 档活在**画布帧**上，非 16:9（含批处理那张
        /// `640×480`）下两者差得出来 ⇒ 自检会「拿另一帧的点去点」而**看着像面板错位**
        /// （先例 = `MulliganPanel.BarWorldPos/DoneWorldPos/EyeWorldPos` 那三个口）。</para>
        /// <para>⚠️ **它由 `rect` 算、不读 `bg` 那颗图**（那件可能因为贴图缺失而没建出来，而本口要的是
        /// 「原版那个矩形在哪」）。
        /// 🔴 **2026-10-16（补正 `A12`）**：uGUI 档那句原来是 `_frame.TransformPoint(PosPx(中心))`
        /// （= 假设本节点在画布中心）；现在换成**同一个 `PxOf` 的逆**（`面板局部 ← 设计 px`），
        /// 两档的式子由此**成对**（世界档 `Place`、画布档 `TransformPoint(局部)` ∨ `PxOf`）。</para>
        /// <para>面板没建出来 / 钮越界 / 矩形没排过 ⇒ `Vector3.zero`。</para></summary>
        public Vector3 ButtonWorldPos(int i)
        {
            if (i < 0 || i >= _btns.Count) return Vector3.zero;
            var r = _btns[i].rect;
            if (r.width <= 0f || r.height <= 0f) return Vector3.zero;      // `RefreshLayout` 还没跑过
            if (_ugui)
            {
                if (_frame == null) return Vector3.zero;
                float cx = r.x + r.width * 0.5f, cy = r.y + r.height * 0.5f;
                return _frame.TransformPoint(new Vector3(cx - PanelOriginPx.x, PanelOriginPx.y - cy, 0f));
            }
            Vector3 p; float h;
            Place(r, out p, out h);           // 世界档：与本节点点锚 + `Scale` 同一式
            return p;
        }

        /// <summary>🔴 **自检用（`T09` 批 3 的【灭自证】）**：本面板的图与字**是不是都建在 uGUI 画布上**
        /// （而不是有条漏网、退回世界空间 quad）。
        /// <para>它防的是本件最坏的那一档：一半在画布、一半在世界 —— 16:9 下逐位相同、全绿，
        /// 只有别的宽高比才露（`T09` §五·补⑦）。⛔ 别用「`HudPiece != null`」替它（那个两边都真）。</para>
        /// <para>任一件没建出来（贴图/字体缺失）⇒ 也返 `false`，如实回答「不成立」。</para></summary>
        public bool OnCanvas
        {
            get
            {
                if (!IsUguiOnCanvas(_close) || !IsUguiOnCanvas(_bg) || !IsUguiOnCanvas(_frameTop)
                    || !IsUguiOnCanvas(_frameBottom) || !IsUguiOnCanvas(_frameLeft) || !IsUguiOnCanvas(_frameRight))
                    return false;
                for (int i = 0; i < _btns.Count; i++)
                    if (!IsUguiOnCanvas(_btns[i].bg) || !IsUguiOnCanvas(_btns[i].icon)
                        || _btns[i].text == null || !_btns[i].text.IsUgui)
                        return false;
                return true;
            }
        }
        static bool IsUguiOnCanvas(HudPiece q)
        {
            return q != null && q.IsUgui && q.Rect != null && q.Rect.GetComponentInParent<Canvas>() != null;
        }

        /// <summary>
        /// 🔴 **2026-10-13（A462）：这一处是【两种不同的触发沿】**，拆成两个入口 —— ⛔ 别再把它们合并成一个
        /// `SetPointer(world, down)`（原来就是那个形状，两半被压成了同一个沿）。
        /// <para>① <b>条外关闭</b> = 那块全屏关闭区 `CloseChatPopup`。原版挂的是 `EventTrigger`，
        /// `m_Delegates[].eventID = **2**`，而 `2 = PointerDown`（判据 = 本机
        /// `com.unity.ugui/.../EventSystem/EventTriggerType.cs:24`：
        /// `PointerEnter=0 · PointerExit=1 · PointerDown=2 · PointerUp=3 · PointerClick=4`；
        /// 场景侧 = `bundle_scenes_scenes_battlearena1/GameObject/CloseChatPopup.json` →
        /// `MonoBehaviour_4913` = `EventTrigger` → `VoiceLinesPopupSelector.Hide`）
        /// ⇒ <see cref="PointerDownAt"/>：**按下那一帧就收**。</para>
        /// <para>② <b>选台词</b> = 6 颗 `ChatButton (1)..(5)` / `ChatButton`，组件是
        /// `ChatPopupButton : EverguildButton`（`ChatPopupButton.cs:7`）⇒ uGUI `Button.onClick`
        /// = `IPointerClickHandler`，**抬起那一帧**才触发 ⇒ <see cref="PointerUpAt"/>。</para>
        /// ⚠️ 命中优先级（谁压谁）**保持原样** —— 原版靠 `GraphicRaycaster` 排序，没读（WA462 §五·1）。
        /// </summary>
        /// <returns>`true` = 这一下落在**面板外**（面板已经关掉了）。命中某颗钮返回 `false`
        /// —— 按下的这一帧**什么都不做**，那句话归抬起那一帧说。</returns>
        public bool PointerDownAt(Vector3 world)
        {
            LastHit = Interactable ? ButtonAt(world) : -1;
            LastClicked = -1;
            if (!Interactable) return false;
            if (LastHit >= 0) return false;     // 命中某颗台词钮 ⇒ 按下这一帧不发声（原版 `onClick` 在抬起）
            // 面板外（或钮与钮之间的缝）⇒ 关。原版那条全屏关闭区就是干这个的。
            Hide();
            return true;
        }

        /// <summary>抬起那一帧（见 <see cref="PointerDownAt"/> 的 ②）。
        /// 返回**这一下点中的钮下标**（-1 = 没点到钮 / 面板已经关了）。</summary>
        public int PointerUpAt(Vector3 world)
        {
            if (!Interactable) { LastClicked = -1; return -1; }
            LastHit = ButtonAt(world);
            if (LastHit < 0) { LastClicked = -1; return -1; }
            LastClicked = LastHit;
            return LastHit;
        }

        // ══════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-20（W57 · 战斗线 `T09` 步骤 A3-e）：**Editor-only 只读探针**
        //  只读：不改状态、不派事件。播放器里**不存在**这些成员（#if 包住）⇒ 零运行时开销。
        //  断言侧（`Editor/BattleScene.cs`）用它们读「命中条在不在 / 射线靶开了没 / 点了几次」。
        // ══════════════════════════════════════════════════════════════════════════════════════
#if UNITY_EDITOR
        /// <summary>uGUI 输入层是不是**整组装好**了（6 条命中条 + 6 个适配器 + 关闭区射线靶 + 触发器）。
        /// 世界档恒 `false`（那一档不装输入层）。</summary>
        public bool UguiInputReadyForTest
        {
            get
            {
                if (!_ugui || _closeTrigger == null) return false;
                if (_btnButtons == null || _btnButtons.Length != ButtonCount) return false;
                for (int i = 0; i < _btnButtons.Length; i++)
                    if (_btnButtons[i] == null) return false;
                return true;
            }
        }

        /// <summary>第 `i` 颗钮的透明命中条（`null` = 世界档 / 没建出来）。</summary>
        public HudPiece ButtonHitForTest(int i)
        {
            return (i >= 0 && i < _btns.Count) ? _btns[i].hit : null;
        }

        /// <summary>第 `i` 颗钮的点击适配器（`null` = 没挂上）。</summary>
        public BattleHudButton ButtonClickComponentForTest(int i)
        {
            return (_btnButtons != null && i >= 0 && i < _btnButtons.Length) ? _btnButtons[i] : null;
        }

        /// <summary>第 `i` 颗钮的命中条是不是射线靶（原版 `MB 4660.m_RaycastTarget = 1`）。</summary>
        public bool ButtonHitRaycastTargetForTest(int i)
        {
            var p = ButtonHitForTest(i);
            if (p == null || p.Rect == null) return false;
            var img = p.Rect.GetComponent<UnityEngine.UI.Image>();
            return img != null && img.raycastTarget;
        }

        /// <summary>第 `i` 颗钮命中条的 `raycastPadding`（L,B,R,T；原版 `MB 4660` 是 **(0,0,0,0)**）。
        /// 拿不到 ⇒ 全 `NaN`（⛔ 不返 0 —— 那与「真的全是 0」分不开）。</summary>
        public Vector4 ButtonHitRaycastPaddingForTest(int i)
        {
            var p = ButtonHitForTest(i);
            if (p == null || p.Rect == null) return new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
            var img = p.Rect.GetComponent<UnityEngine.UI.Image>();
            return img != null ? img.raycastPadding : new Vector4(float.NaN, float.NaN, float.NaN, float.NaN);
        }

        /// <summary>全屏关闭区的 `EventTrigger`（`null` = 没挂上）。</summary>
        public EventTrigger CloseTriggerForTest { get { return _closeTrigger; } }

        /// <summary>全屏关闭区当前是不是射线靶（原版 `NonDrawingGraphic.m_RaycastTarget = 1`）。</summary>
        public bool CloseRaycastTargetForTest
        {
            get
            {
                if (_close == null || _close.Rect == null) return false;
                var img = _close.Rect.GetComponent<UnityEngine.UI.Image>();
                return img != null && img.raycastTarget;
            }
        }

        /// <summary>6 颗钮一共收到几次 uGUI 点击（灭自证：点一次 = 1）。</summary>
        public int UguiButtonClickCountForTest { get { return _uguiButtonClicks; } }
        /// <summary>条外关闭落了几次。</summary>
        public int UguiCloseCountForTest { get { return _uguiCloseCount; } }
        /// <summary>最近一次 uGUI 点中的钮（-1 = 没点过）。</summary>
        public int LastUguiButtonForTest { get { return _lastUguiButton; } }
        /// <summary>有没有「挂起的关闭」还没落（见 `ApplyPendingClose`）。</summary>
        public bool ClosePendingForTest { get { return _closePending; } }

        // ── 🆕 2026-10-20（`T09` 步骤 A3-e1）：悬停/按下反馈的只读探针 ─────────────────────
        /// <summary>6 颗钮的**两套反馈**都建起来了没有（ColorTint 打底图 + SpriteSwap 打图标）。
        /// 世界档恒 `false`（那一档不建输入层、也不建反馈 —— 与改本件之前逐位相同）。</summary>
        public bool ButtonFeedbackReadyForTest
        {
            get
            {
                if (!_ugui || _fxBtnBg == null || _fxBtnIcon == null) return false;
                if (_fxBtnBg.Length != ButtonCount || _fxBtnIcon.Length != ButtonCount) return false;
                for (int i = 0; i < ButtonCount; i++)
                    if (_fxBtnBg[i] == null || _fxBtnIcon[i] == null) return false;
                return true;
            }
        }

        /// <summary>第 `i` 颗钮**底图**的 ColorTint 目标色键（原版 `MB 4112.m_Colors`：
        /// 悬停 `0.9607843` · 按下 `0.7843137` · 常态白）。`null` 反馈 ⇒ `Color.clear`。</summary>
        public Color ButtonBgFeedbackKeyForTest(int i)
        {
            return (_fxBtnBg != null && i >= 0 && i < _fxBtnBg.Length && _fxBtnBg[i] != null)
                 ? _fxBtnBg[i].KeyForTest : Color.clear;
        }

        /// <summary>第 `i` 颗钮**图标**当前那张图的名字（原版 `leftButtonNormal/Hover/Pressed` 三档）。
        /// 期望：常态 `40k_voicelines_bt_L` · 悬停 `..._hover` · 按下 `..._pressed`。</summary>
        public string ButtonIconTextureForTest(int i)
        {
            return (_fxBtnIcon != null && i >= 0 && i < _fxBtnIcon.Length && _fxBtnIcon[i] != null)
                 ? _fxBtnIcon[i].TextureNameForTest : null;
        }

        /// <summary>第 `i` 颗钮的命中条上**有没有挂某个事件的 `EventTrigger` 条目**
        /// （`eventId`：`PointerEnter=0 · PointerExit=1 · PointerDown=2 · PointerUp=3`）。
        /// 原版这四个口都在 `Selectable` 上（enter/exit/down/up），我们显式挂。</summary>
        public bool ButtonTriggerBoundForTest(int i, int eventId)
        {
            var p = ButtonHitForTest(i);
            if (p == null || p.gameObject == null) return false;
            var trig = p.gameObject.GetComponent<EventTrigger>();
            if (trig == null) return false;
            for (int k = 0; k < trig.triggers.Count; k++)
                if (trig.triggers[k] != null && (int)trig.triggers[k].eventID == eventId) return true;
            return false;
        }

        /// <summary>自检口：**直接喂一次悬停/按下状态**（⛔ 不走 `EventSystem` —— 批处理里没有它）。
        /// 语义与 `EventTrigger` 那四条逐字相同（`SetButtonHover` / `SetButtonPressed` 同一份）。</summary>
        public void SimulateButtonFeedbackForTest(int i, bool hover, bool pressed)
        {
            SetButtonHover(i, hover);
            SetButtonPressed(i, pressed);
        }
#endif

        /// <summary>自检打印用。</summary>
        public string Describe()
        {
            // ⚠️ `?:` **不能直接写进字符串插值**（`:` 会被当成格式分隔符，CS8361）—— 先取出来
            string r0 = _btns.Count > 0 ? _btns[0].rect.ToString() : "-";
            return $"ChatPopup 可见={Visible} alpha={_alpha:F2} 钮={_btns.Count} 第1钮矩形=({r0})";
        }
    }
}
