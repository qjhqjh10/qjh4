// TopBar.cs — 外壳顶栏（`Upper bar` + `Player Profile` + `Resources Bar`）的**唯一实现**。
//
// ============================ 为什么有它 ============================
// 🔴 **2026-10-17（B25）收口：本文件从「卡组编辑专用的一份复刻」升成【唯一实现】。**
//   之前的形状：`Shell/MainMenuRuntime.cs` 里有一份 `BuildUpperBar / BuildPlayerProfile /
//   BuildResourcesBar / BuildTopAvatar`（`private` 实例方法，挂在 `MainMenu.unity` 的根上 ⇒ 别处调不到），
//   而 `DeckRuntime`（卡组编辑，**独立场景** `DeckEditor.unity`、背后没有主菜单）需要**同一条顶栏**
//   ⇒ D13/D14 那一轮把主菜单那份**逐参抄了一遍**放在本文件里 ⇒ **两份并存**。
//
// 🔴 **两份并存的实际代价（已经发生了，不是"迟早"）**：D13/D14 抄漏了两处，卡组编辑那一屏**画错了**：
//   · **① 资源计数器次序**：抄的那份按 `DefaultCurrencies`（= `{gold, crystals, blackStones}`）建
//     ⇒ 画出来 **gold·crystals·blackStones**；主菜单那份按原版**建表序**建 ⇒ **crystals·blackStones·gold**。
//     判据（两条独立）：`CounterSpecs` 的 `List.Add` 序（`GeneralMenuController__Initialize.c:51-146`）
//     **＋** 用户实拍 `资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png` 右上角（左→右 = 水晶 41360 ·
//     黑石 356 · 金币 1500 ⇒ 也断得出同一个次序）。**次序反了就是反了**，两张图并排一眼可辨。
//   · **② 信箱红点**：抄的那份写死 `BadgeAlpha(false)`（恒 alpha 0）⇒ **有未读消息也永远不会亮**；
//     主菜单那份是 `DailyData.InboxHasBadge ? 1 : 0`。⇒ 已照主菜单订正。
//   ⇒ **两份就是会漂**（铁律「两处写同一条规则 = 迟早不一致」的又一个实例）—— 这就是这次收口的理由。
//
// 🔴 **收口后的分工（⛔ 别再把参数写回 `MainMenuRuntime`）**：
//   · **本文件** = 顶栏的**几何 + 结构 + 接线**（那 4 颗钮点哪儿）。
//   · **`MainMenuRuntime`** = 顶栏的**数据与入口**：资源计数器那几张表（`CounterSpecs` / `EnergySpec` /
//     `OwnedOf` / `MaxOf` / `ShouldShow` / `CounterText`，含自检拨盘）· 队列常量 `QBar*` ·
//     红点颜色 `BadgeTint`/`BadgeAlpha` · 三个开窗入口 `OpenInbox` / `OpenSettings` / `OpenProfile`。
//     ⚠️ 这些**不是**「第二份参数」：它们各自只有一处，只是**住在** `MainMenuRuntime` 里
//     （那边的注释带着逐条 `pid`/反编译出处，搬过来只会把出处与代码拆开）。
//
// ============================ 判据出处 ============================
//   · 矩形/尺寸：`bundle_scenes_scenes_mainmenuwarpforge` 逐件 `RectTransform` 实读（A332 那一轮），
//     表在 `资料/主菜单_原版规格.md` §五 + `资料/普查产出_1011/WB2_A332.md`。
//   · 资源计数器的整套（建表序 / 显隐 / 上限机制 / 逐格几何）：`资料/普查产出_1013/W374_资源计数器.md`。
//   · 队列：**复用 `MainMenuRuntime` 的 `QBar*` 常量**（`public const`）—— 它们是「顶栏整条带子
//     在所有窗之下」的那一档（2026-10-11 A283 起 `2994–2998` · **2026-10-17 F5 起 `2986–2998`**，
//     见下面那段表），本件底下的东西从 `QSep 2999` 起 ⇒ **自动成立**。
//   · 那 4 颗钮的**原版目标**（逐颗）：
//       ① `SettingsBtn` → `Main Menu Settings Window`（`MonoBehaviour_2528.json` 的 `windowToOpenPrefab`
//          GUID `5a20859a…` → 容器表 → pid `-9019961019057471578` = 那个 prefab 的根；
//          五跳实证 → `资料/普查产出_1006/甲5_A97_A99_A104_A105.md` §A104）；
//       ② `InboxBtn` → `Inbox Menu`（GUID `9aadd8e3…` → pid `-4892976514573368526`；
//          → `资料/日常_调用链_三窗.md` §三）；
//       ③ `Avatar Item Small` → `Player Profile Window`（GUID `e357bbff…`；→ `资料/日常_原版规格.md:30`）；
//       ④ `Challenge button`（`40K_icon_duel`）→ **不开窗**：原版是 `ChallengeButton`
//          （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/ChallengeButton.cs`：字段 `button` +
//          `subscriptionNotification`；组件 pid `2333` 在 `GameObject/Challenge button.json` 的组件表里），
//          `Initialize()` 按 `challengeManager.challenges.Count > 0` 显隐、`OnButtonClick()` 弹一个
//          「`Demo/FriendsMenu/ReceiveChallenge` + 模式名」的 OK/Cancel 确认框然后**把自己隐藏**
//          （`decomp_full/ChallengeButton__Initialize.c` / `__OnButtonClick.c`；三个词条串已解出）。
//       ⑤ 🆕 **2026-10-11（`A1522①` / `A1523①`）：那两颗「出厂关着」的钮（`Feedback Button` /
//          `Challenge button`）统一按【建、但出厂 `SetActive(false)`】** —— 判据与理由写在各调用点上方
//          （原版那一排**三颗**：`InboxBtn` 常显 · `Challenge button` 出厂 `True` 但运行期被
//          `ChallengeButton.Initialize()` 关掉（服务端条件）· `Feedback Button` prefab 里 `m_IsActive = false`）。
//          ⛔ 别再退回「出厂 `False` ⇒ 不建」（`MainMenuRuntime` 文件头纪律 ③ 已就地订正）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>外壳顶栏的**共用件**（唯一实现）。用法：<c>var parts = TopBar.Build(root, onMissing);</c>
    /// 之后每帧（或每次改头像后）调 <see cref="RefreshTopAvatarIfChanged"/>。</summary>
    public static class TopBar
    {
        // ---- 队列：**带子下沿/上沿只认 `MainMenuRuntime` 那两个 public const**（本件一个数也不新开；
        //      2988–2990 / 2992–2993 这五档只有本件用、`MainMenuRuntime` 那边没有对应常量 ⇒ 落在本件，
        //      但**整条梯子只有下面这一张表**，⛔ 别在宿主里再抄一份） ----
        //
        // 🔴 **2026-10-17（F5）已落地：带子放宽 + 顶栏内部按【原版兄弟序】分 8 档**（F2 开的单，F5 做的）。
        //   · 症状（`DeckScene.Run` 红）：`同一层的 UI 图没有互相压住（实测 8 处）` —— **8 对全在本件内部**
        //     （那条通用检查全库只有卡组编辑宿主有：`Editor/DeckScene.cs` 的 `TestLayout`）。
        //   · **根因（算得出来，逐对量过）**：本件有 **6 张两两相交**的图，而队列是 `int`、
        //     旧带子 `[2994,2998]` **只有 5 个整数档** ⇒ **鸽笼原理，装不下**：
        //       A 条底 `Upper bar/Background` [-11.7,1920]×[0,71.3] ·
        //       B 玩家框 `Player Profile/Background` [23,411]×[11.6,135.6] ·
        //       C 名字条 `Planer Name Background` [25.6,472.1]×[14.9,60.5] ·
        //       E 盾框 `Border`（256×286 内接）**[19.52,135.98]×[9,139.1]** ·
        //       F 立绘 `Image`（512² 内接）**[-21.86,182.56]×[-34.59,169.83]** ·
        //       G 等级图标 `Player Level/Icon` [117.5,170.6]×[54.3,107.4]
        //       —— **15 对全部相交**（A 那条带子铺满整条，与其余 5 张全交）。
        //   · ⛔ **三条绕法都排除过**：把顶栏排除出检查（那只是把主菜单/外壳上同样的隐患一起放掉）·
        //     小数队列（`SetRenderQueue` 收 `int`）· `≤0`（那是 `MainMenuScene` 那条的**跳过口**，等于排除）。
        //   · ✅ **正解（已落地）**：`MainMenuRuntime.QBarPanel` **2994 → 2986**（带子 = `[2986,2998]`）+
        //     本件各层显式分档。**为什么 2986 / 三条不变式 / E-C 次序的订正** →
        //     `MainMenuRuntime` 的 `QBarPanel` 那段长注释（⛔ 别在这儿重抄第二份）。
        //   · 🔴 **档位照【原版兄弟序】**（`python 工具/menu_dump.py bundle_scenes_scenes_mainmenuwarpforge
        //     "Upper bar" --md --depth 1` 实读 = `Background → SettingsBtn → TopBarButtons → Player Profile
        //     → Resources Bar`；`Player Profile` 再展开 = `Background` → `Background Mask`（**名字条
        //     `Planer Name Background` 挂在它下面**）→ `Player Name` → `Profile border`（出厂 inactive、
        //     `Image` 组件也被禁 ⇒ 原版画的不是这一颗）→ **`Avatar Item Small`**（活的那面盾 =
        //     `Image Container/Border` · 立绘 = 同一容器的 `Image`）→ `Player Level`）⇒ **后画者在上**：
        //       2986 条底（A）· 2987 图标（齿轮/信封/挑战/资源条药丸与图标）· 2988 红点（两颗 `Badge Highlight`）·
        //       2989 玩家框（B）· 2990 名字条（C）· 2991 盾框（E）· 2992 立绘（F）· 2993 等级图标（G）。
        //       **文字一律 `QBarText`（2997）** —— 它在带子最上沿，而且 `Label` 不参与「同层图不许压住」
        //       那条检查（也不与任何图同档）。
        //   🔴 **2026-10-17（F5）就地订正 F2 那张表的两格（铁律 5）**：表里原写「2990 盾框（E）· 2991 名字条（C）」，
        //      **与它自己引的兄弟序相反**（名字条挂 `Background Mask`（第 2 颗子件）、活的那面盾挂
        //      `Avatar Item Small`（**第 5 颗**）⇒ 盾在上）。用户实拍同一处吻合
        //      （`资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`：盾完整、暗紫名字板**从盾右边**才露出来）。
        //      ⇒ 现取 **2990 名字条 · 2991 盾框**；照 F2 原表那块暗紫板会横穿盾面 = 与原版不符。
        //   ⚠️ 本件的**相对次序一张表说清**，⛔ 别在两个宿主各写一份（B25 收口就是为了防这个）。
        //   ⚠️ **同一处不许两个写手**：每一层的档位**只**经 `Rect/Text/Nine` 那个 `q`/`queue` 形参进去一次，
        //      ⛔ 别在调用点之后再补一句 `SetRenderQueue`。
        public const int QBarPanel = MainMenuRuntime.QBarPanel;              // 2986 条底（A）· 带子下沿
        public const int QBarContent = MainMenuRuntime.QBarContent;          // 2987 图标（齿轮/信封/挑战/资源条药丸与图标）
        public const int QBarBadge = 2988;                                   // 红点（两颗 `Badge Highlight`）
        public const int QBarPlayerFrame = 2989;                             // 玩家框（B）
        public const int QBarNamePlate = 2990;                               // 名字条（C）
        public const int QBarAvatarFrame = MainMenuRuntime.QBarAvatarFrame;  // 2991 盾框（E）
        public const int QBarAvatar = 2992;                                  // 立绘（F）
        public const int QBarLevel = 2993;                                   // 等级图标（G）
        public const int QBarText = MainMenuRuntime.QBarText;                // 2997 文字

        // ============================================================ 🆕 2026-10-14（A1814）：那 4 颗钮的【命中区】
        //
        // 🔴 **原版判据（两条，本地都能读）**：
        //   · 射线那一关 = `Library/PackageCache/com.unity.ugui…/Runtime/UGUI/UI/Core/GraphicRaycaster.cs:327`
        //     `RectangleContainsScreenPoint(graphic.rectTransform, pointerPosition, eventCamera,
        //     graphic.raycastPadding)` ⇒ **射线认的是那件自己的 `RectTransform` 矩形 + 它自己的 pad**；
        //     **`m_PreserveAspect` 不在这一句里**（它只改生成的网格 ⇒ 只改**画面**）。
        //   · 逐颗的原版值 = `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_{2526,2529,2296,2531}.json`
        //     （`m_RaycastTarget = 1` · `m_PreserveAspect = 1` · 四个 pad **两两不同**）；矩形 = 同包
        //     `RectTransform/RectTransform_{1560,1561,1391,1562}.json`。**2026-10-14 逐条现读**。
        //     ✅ **独立复核过一遍**：全包 1134 个 `MonoBehaviour` 里**只有 12 条非零 `m_RaycastPadding`**，
        //     这四颗就是其中之四（另 8 条不在本件 —— 清单见报告）。
        //
        // 🔴 **我们原来错在哪**（`A1814`）：命中区是从「那一颗 `ImageQuad` 画出来多大」现算的
        //   （`PointerLayer.HitBoxPx`）⇒ 保宽高比时**画出来那块比原版布局框小**：齿轮 `87.78 × 61.73`
        //   的框里，179² 的贴图内缩之后**只画 61.73²** ⇒ **右缘少 22.7 设计 px**（左右各少 13.03 的**框**差
        //   ＋ pad 的外扩）⇒ 真机上「看着在齿轮上、点不动」（`_tmp_view/click_log.txt` 的探针 #8 只差 0.9px，
        //   而那一点在原版里**点得中**、余量 21.8px）。
        // ⇒ **两个数必须分开表达**（铁律 5·c）：`*Box` = **原版那件自己的矩形**（= 我们建它时用的那个框）、
        //   `*Pad` = 它那颗 `Image` 的 `m_RaycastPadding`；命中区 =
        //   `MenuDraw.PaddedHitRect(Box, Pad)`（**共用件** ⇒ ⛔ 别在本件自写 `±pad` 算式，符号口径与退化守卫
        //   都在那份 doc 里），再经 `PointerLayer.RegisterHitRect` 登记（⛔ 那 4 颗钮的 `WindowButton`
        //   **必须留在画出来那颗 quad 上**，别改成在子树里另挂一颗命中 quad）。
        // ⚠️ pad 分量序 = **(L, B, R, T)**、**负值 = 外扩**（`PaddedRect`：`x1+pad.x · y1+pad.w · x2−pad.z · y2−pad.y`；
        //   上式那对 y 在「y 向下」的框里正好把**上沿往上、下沿往下**推）。样板 → `Battle/BattleDriver.cs`。
        // ⚠️ **`Challenge button` / `Feedback Button` 出厂关着**（见 `Build` 里那两句 `SetActive(false)`），
        //   但**结构与命中区照原版照样登记**（原版那两颗的 `Image` 同样带 pad）—— 开着的都是原版值。
        static readonly PxRect GearBox = new PxRect(1803.1f, 4.6f, 1890.9f, 66.4f);
        static readonly Vector4 GearPad = new Vector4(-4.38f, -18.10f, -9.70f, -17.86f);
        static readonly PxRect InboxBox = new PxRect(425.3f, 15.5f, 480.3f, 55.5f);
        static readonly Vector4 InboxPad = new Vector4(-6.28f, -23.00f, -5.70f, -23.00f);
        static readonly PxRect ChallengeBox = new PxRect(490.1f, 11.8f, 537.6f, 59.2f);
        static readonly Vector4 ChallengePad = new Vector4(-4.14f, -10.10f, -5.22f, -10.40f);
        static readonly PxRect FeedbackBox = new PxRect(565.44f, 10.5f, 615.44f, 60.5f);
        static readonly Vector4 FeedbackPad = new Vector4(-4.35f, -17.90f, -5.18f, -18.30f);
        /// <summary>头像那一格（`Player_Profile_Border`）—— **本次不动它**：原版那颗 `Border` 的 `Image`
        /// `m_RaycastTarget = **0**`、pad 也全 0（2026-10-14 现读）⇒ **原版那一刻吃指针的其实不是它**
        /// （「可射线件的并集」还没查 — 铁律 3）⇒ 我们**不给它换一个数**（⛔ 不许拿一个数顶另一个；见报告「没查清」）。这一格只把矩形提成常量，好与上面四颗一个写法。</summary>
        static readonly PxRect AvatarBorderBox = new PxRect(-10.0f, 9.0f, 165.5f, 139.1f);

        /// <summary>等级徽章的**圆底图**（`A1637`）：`Player Level` 那颗 `Image` 的 `m_Sprite`
        /// = pid `2381704724431365035` = **`UI_Button_Round_background`**（深色圆 + 一圈细亮环，237²）。
        /// 判据三条（真包扫 pid · 场景 MB 2359 · `bundle_menus_assets_all` 里 4 份 `Player Level` 同 pid）
        /// → 建它的那一句上面那段注释。⚠️ **与档案窗那一格是同一张**（`Shell/ProfileTab.cs` 的
        /// `ArtRoundBtn`，值一字不差）—— 两处各有一份 `const` 是**故意的**：本工程的 `const` 不跨类转发，
        /// 而两处都把**同一个 pid/名字**写在注释里 ⇒ 任一处被改错，注释与真值立刻对不上。</summary>
        public const string ArtRoundBtn = "UI_Button_Round_background";

        /// <summary>建出来的那些件（调用方要留着它才能「头像换了跟着换」）。</summary>
        public sealed class Parts
        {
            /// <summary>`Upper bar` 那棵节点（整条带子）。</summary>
            public Transform Root;
            /// <summary>顶栏立绘（**一张头像图都取不到时是 `null`** —— 那种情况按出厂态处理：只剩盾）。</summary>
            public ImageQuad TopAvatar;
            /// <summary>立绘画的是第几号头像（`ProfileData.AvatarIndex` 的快照；`-1` = 没画）。</summary>
            public int AvatarIndex = -1;

            // ---- 🆕 B25：那 4 颗钮的命中区（自检问「点了谁」用；建不出来时是 `null`）----
            /// <summary>`Avatar Item Small` 上那面盾（= `Player_Profile_Border` 那一格）。</summary>
            public WindowButton ProfileHit;
            /// <summary>`InboxBtn`（信封）。</summary>
            public WindowButton InboxHit;
            /// <summary>`Challenge button`（`40K_icon_duel`，人形图标）。</summary>
            public WindowButton ChallengeHit;
            /// <summary>`SettingsBtn`（齿轮）。</summary>
            public WindowButton SettingsHit;
        }

        /// <param name="parent">挂在谁的下面（`DeckRuntime` / `MainMenuRuntime` 都传自己的根；那根出厂在原点、无父）。</param>
        /// <param name="onMissing">缺图回调（**出声**；`null` = 不出声）。⛔ 别在这里静默吞掉。</param>
        public static Parts Build(Transform parent, Action<string> onMissing)
        {
            var parts = new Parts();
            var bar = New(parent, "Upper bar", 1920f, 100f);
            parts.Root = bar;
            // `Background`：整条 1920 宽、高 71.3（**注意不是 100** —— 100 是容器高）
            Rect(bar, "UI_Main_Upper_bar", -11.7f, 1920f, 0f, 71.3f, "Background", QBarPanel, null, false, onMissing);

            // ---- 齿轮（右上角那一格）----
            var settings = New(bar, "SettingsBtn", 87.78f, 61.73f);
            // 原版 `m_PreserveAspect = 1`、贴图 179×179 塞进 87.78×61.73 ⇒ 实绘 61.73²（居中）
            // 🔴 **画出来那块（61.73²）≠ 原版那件自己的矩形（`GearBox` = 87.78×61.73）** ——
            //    命中区取**后者** + `GearPad`（`A1814`：两个数分开表达，判据见文件头那一整段）。
            var gear = Rect(settings, "UI_Settings_Icon", GearBox, "Image", QBarContent, null, true, onMissing);
            // ⚙️ 设置钮的红点：**没有通知源** ⇒ 画出来但 alpha = 0（原版由 `UiBadgeNotification` 按通知亮）
            Rect(settings, "40K_notification_number", 1865.9f, 1890.9f, 4.4f, 29.4f, "Badge Highlight",
                 QBarBadge, MainMenuRuntime.BadgeAlpha(false), false, onMissing);

            // ---- 信封 + 挑战（那两个顶栏钮）----
            var btns = New(bar, "TopBarButtons", 311.4f, 71.33f);
            var inbox = New(btns, "InboxBtn", 55f, 40f);
            var inboxImg = Rect(inbox, "40K_notification", InboxBox, "Image", QBarContent, null, true, onMissing);
            // 红点：原版 `Inbox.CheckNotification` = **未读条数**，走 `UiBadgeNotification` 的 **alpha 补间**
            // （`Show()` 把 alpha 置 1、`Hide()` 置 0 —— **不是 `SetActive`**）。⚠️ **照主菜单那一份**：
            // 原来本件写死 `BadgeAlpha(false)`（恒 0）⇒ 有未读也永远不亮（见文件头 ②）。
            Rect(inbox, "40K_notification_number", 454.3f, 489.3f, 2.0f, 37.0f, "Badge Highlight",
                 QBarBadge, MainMenuRuntime.BadgeAlpha(DailyData.InboxHasBadge), false, onMissing);
            // 🆕 **2026-10-17（`A1840`）**：原版这两颗 **`m_PreserveAspect = 1`**（判据 →
            //   `资料/普查产出_第二十八会话/W5_顶栏命中区.md`；我们原来写 `keepAspect = false` ⇒ 画面差
            //   0.1 px / 1.84 px）。⚠️ 它只改**画面**（`FitAspect` 内缩），命中区仍按各自 `m_RaycastPadding` 算。
            var challenge = Rect(btns, "40K_icon_duel", ChallengeBox, "Challenge button",
                                 QBarContent, null, true, onMissing);
            // 🔴 **2026-10-11（`A1523①`）：这一颗【建出来、但出厂 `SetActive(false)`】。**
            //   原版**运行期实况就是关着的**（两条互相独立的判据）：
            //     ① 运行期 dump（真·原版）`资料/原版实拍/menu_topbar_0927/runtime_ui_dump_MainMenu_Warpforge.tsv:76`
            //        = `activeSelf False` / `activeInHierarchy False`；
            //     ② 1920×1080 原生实拍该格（x 490.1..537.6）**亮度 max 30** = 条底色 ⇒ 那里**是空的**。
            //   谁关的 = `ChallengeButton.Initialize()`：按**服务端** `challengeManager.challenges.Count > 0`
            //   决定显隐（第一权威 = `d:/2/tools/decomp_full/ChallengeButton__Initialize.c`）——
            //   挑战来自 PlayFab，**本地没有挑战源** ⇒ **恒关**。
            //   判据全文 → `资料/普查产出_第十七会话/P_信箱与名牌.md`（顺手发现 ①·状态→参数表）。
            //   ⚠️ 我们原来把它建成**常显** —— 与同一排「`Feedback Button` 出厂 `False` ⇒ 不建」**自相矛盾**。
            //   主对话 2026-10-11 裁定：**两颗统一按「建、但出厂关着」**（结构与接线照原版留着，
            //   ⛔ 不删节点 —— 删了 `Editor/{MainMenuScene,DeckScene}.cs` 里按名字找它的断言会红）。
            if (challenge != null) challenge.gameObject.SetActive(false);
            // `Feedback Button`（`565.44..615.44 × 10.50..60.50` = **50×50** · sprite `40K_icon_feedback`）
            // 🔴 **2026-10-11（`A1522①`）：原版是【建了、但出厂关着】⇒ 照原版建、出厂关着**（同上面那一颗）。
            //   判据两条：① 表 `资料/主菜单_原版规格.md:96` 那一行的最后一格 = **出厂 `False`**（表 `:327`
            //   记它的矩形 `565.4..615.4, 10.5..60.5`）；② 实况 `GameObject/Feedback Button.json` 的
            //   **`"m_IsActive": false`**（同包，逐字节实读）。
            //   ⚠️ ⛔ **不接线**：原版那一颗的 `Button` / `EverguildButton` 上挂着一条**持久调用**
            //   （`MonoBehaviour_2530.json` 的 `m_OnClick.m_PersistentCalls` = `BattleManager.ClickSettings`，
            //   而且 `m_Target` 是 **null**）—— 看着是 prefab 里的**复制粘贴残留**，而它**整颗不激活**
            //   ⇒ **永远不派发** ⇒ 我们**不替它编一个动作**（本工程红线：⛔ 不把猜测写成「原版就是这样」）。
            //   📌 这一颗「原版到底干什么」= **没查清**（它恒关，无从验证）—— 如实记在此，别当成已复刻。
            //   ⚠️ 原版它**不参加 `TopBarButtons` 的 HLG**（`m_IsActive = 0` ⇒ uGUI 建 `rectChildren` 时跳过）
            //   ⇒ 它那格是**手写死值 `565.44`**：条内一重排它就与 `Challenge` 对不上了
            //   （判据 → `P_信箱与名牌.md` §「谁被 LayoutGroup 托管」那张表）。
            //   我们的实现在本件里**本来就全部写绝对坐标** ⇒ 这一点自动成立，⛔ 别改成跟着 `InboxBtn` 算。
            var feedback = Rect(btns, "40K_icon_feedback", FeedbackBox, "Feedback Button",
                                QBarContent, null, true, onMissing);   // A1840：原版 `m_PreserveAspect = 1`
            if (feedback != null) feedback.gameObject.SetActive(false);

            BuildPlayerProfile(bar, parts, onMissing);
            BuildResourcesBar(bar, onMissing);

            // ---- 🆕 B25：接线（那 4 颗钮）。⛔ 别把 `WindowsManager` 那套搬进本件 ----
            // 🔴 **为什么接线在【本件】而不是各宿主各接一份**：宿主有两个（主菜单 / 卡组编辑），
            //    接两份 = 又回到「两份会漂」（本文件头的 ①② 就是这么来的）。
            // 🔴 **派发靠 `PointerLayer`**（全壳唯一那条「真鼠标 → 界面」的路）：主菜单场景由壳建
            //    （`ShellRuntime` → `WindowsManager.EnsureHost`，壳是 `DontDestroyOnLoad` ⇒ **进卡组编辑场景时
            //    它还在**）；`PointerLayer.Instance` 自己也会**惰性现建**一台 ⇒ 两条路都到得了。
            parts.SettingsHit = Wire(gear, GearBox, true, GearPad, OpenSettings);
            // ⚠️ 这两个入口**有返回值**（那扇窗）⇒ 包一层 lambda（方法组转不了 `Action`）
            parts.InboxHit = Wire(inboxImg, InboxBox, true, InboxPad, () => MainMenuRuntime.OpenInbox());
            // 头像那一格：原版那颗 `Border` 的 pad 全 0（且 `m_RaycastTarget = 0`）⇒ **零 pad = 不登记**
            // （`Wire` 里那一句早退），命中区逐位照旧 —— 见 `AvatarBorderBox` 的注释与报告「没查清」。
            parts.ProfileHit = Wire(BorderQuad(bar), AvatarBorderBox, true, Vector4.zero,
                                    () => MainMenuRuntime.OpenProfile());
            parts.ChallengeHit = Wire(challenge, ChallengeBox, true, ChallengePad, ChallengeClicked);   // A1840：`m_PreserveAspect = 1`
            return parts;
        }

        // ============================================================ 接线

        /// <summary>给一格挂上 `WindowButton`（原版那一件上挂的是 `OpenWindowButton` / `EverguildButton`）。
        /// 建不出那格（缺图）⇒ 返回 `null`（调用方/自检能看出「这颗钮根本没画」）。
        ///
        /// <para>🆕 **2026-10-14（`A1814`）：命中区要按原版 `m_RaycastPadding` 登记**（判据 → 文件头那段）。
        /// 🔴 **两个矩形在这里分开**：<paramref name="box"/> = **原版那件自己的矩形**（= 建它时用的那个框）、
        /// 「画出来那块」= `keepAspect ? FitAspect(贴图, box) : box`（`m_PreserveAspect` 只改画面）。
        /// 命中区 = `MenuDraw.PaddedHitRect(box, pad)`（**共用件，⛔ 别自写算式**）。</para>
        /// <param name="box">**原版那一件自己的矩形**（设计 px）—— ⛔ 不是画出来那块。</param>
        /// <param name="keepAspect">它是不是保宽高比画的（原版 `m_PreserveAspect`）—— 用来算「画出来那块」。</param>
        /// <param name="pad">原版那颗 `Image` 的 `m_RaycastPadding`（UGUI 序 **L,B,R,T**，**负 = 外扩**）。
        /// 全 0 = 不登记（命中区照旧逐位不变 —— 头像那一格走的就是这一支）。</param></summary>
        static WindowButton Wire(ImageQuad quad, PxRect box, bool keepAspect, Vector4 pad, System.Action onClick)
        {
            if (quad == null) return null;
            var hit = quad.gameObject.AddComponent<WindowButton>();
            hit.onClick = onClick;
            if (pad != Vector4.zero)
            {
                var drawn = keepAspect ? FitAspect(quad.Texture, box.x1, box.x2, box.y1, box.y2) : box;
                PointerLayer.RegisterHitRect(hit, drawn, MenuDraw.PaddedHitRect(box, pad));
            }
            return hit;
        }

        /// <summary>头像那一格的命中区 = `Player_Profile_Border` 那张图的 quad
        /// （原版那块头像上的 `OpenWindowButton` 就挂在这件上；命中区 = 它自己的矩形 `-10,9 → 165.5,139.1`）。
        /// ⚠️ 走**名字深查找**而不是让 `BuildPlayerProfile` 传出来：那一份建树的返回值是 `void`，
        /// 改签名会牵动一排调用点；这一格的名字是**我们自己在上面写死的字面量**，查不到 ⇒ 接线那一条会红（不静默）。</summary>
        static ImageQuad BorderQuad(Transform bar)
        {
            var t = FindDeep(bar, "Border");
            return t != null ? t.GetComponent<ImageQuad>() : null;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = FindDeep(root.GetChild(i), name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>设置：原版 `SettingsBtn` 上那枚 `OpenWindowButton` 开 `Main Menu Settings Window`
        /// （GUID `5a20859a…`，五跳实证见文件头 ①）⇒ 我们走 `MainMenuRuntime.OpenSettings()`（**同一个入口**
        /// —— 那边还管「已经开着就复用那一扇」那条照原版的语义，⛔ 别在本件另开一份）。</summary>
        static void OpenSettings() { MainMenuRuntime.OpenSettings(); }

        /// <summary>「挑战」钮（`40K_icon_duel`）—— 🔴 **原版这一颗不开任何窗**，它是一颗**收挑战**的钮：
        /// `ChallengeButton.Initialize()` 按 `challengeManager.challenges.Count > 0` 决定显不显；
        /// 点下去 = `ShowPopUp(String.Format(词条 `Demo/FriendsMenu/ReceiveChallenge`, 挑战名, 模式词条),
        /// OK=`Demo/MainMenu/OK` / Cancel=`Demo/MainMenu/CancelButton`)`，然后**把自己 `SetActive(false)`**
        /// （`d:/2/tools/decomp_full/ChallengeButton__Initialize.c` · `__OnButtonClick.c`，三个词条串按
        /// RVA 解出 = `0x4277070` / `0x4277658` / `0x4277360`）。
        /// ⚠️ 挑战**来自服务端**（PlayFab），本地没有挑战源 ⇒ 这里**没有可做的事**。
        /// 🔴 所以这一声必须**出声**（本工程红线：点了没反应又不吭声 = 静默失败）—— 这就是那一声。</summary>
        static void ChallengeClicked()
        {
            Debug.Log("[TopBar] 「挑战」钮（`Challenge button` / `40K_icon_duel`）：原版这一颗**不开窗** —— 它是"
                      + "`ChallengeButton`，由 `challengeManager.challenges.Count > 0` 决定显不显，点下去弹一个"
                      + "「`Demo/FriendsMenu/ReceiveChallenge`」确认框（OK / Cancel）然后把自己隐藏。"
                      + "挑战来自服务端（PlayFab）⇒ **本地没有挑战源，这一下没有可做的事**"
                      + "（判据 → `decomp_full/ChallengeButton__Initialize.c` / `__OnButtonClick.c`）。");
        }

        // ============================================================ 玩家档案（左上那块）

        /// <summary>原版 `Player Profile`（0..528.7, 0..211.1）—— 左上角那块。逐参实读见
        /// `MainMenuRuntime` 那一份（本件从那边搬来时**一个数都没改**）；两处易错的判据照抄在此：
        /// · `Player_Profile_Border` **保宽高比**画（`m_PreserveAspect = 1`，13 个战场里 39/39 全 1）
        ///   —— 拉伸会**宽 1.6 倍**；
        /// · `Player Name` 的 autosize 三件套 = `(265/108, 48/108, min 10, max 32, base **36**)`
        ///   且 **`m_TextWrappingMode = 0`**（不折行）；
        /// · `Player Level Text` 的 base 是 **36.89**（≠ 标称 37.2 —— 缺省就退回 37.2 了）。</summary>
        static void BuildPlayerProfile(Transform parent, Parts parts, Action<string> onMissing)
        {
            var p = New(parent, "Player Profile", 528.65f, 211.07f);
            // 玩家框（B）：`40k_main_player_frame` 那一层（原版 `Player Profile/Background`，子件第 1 颗）
            Rect(p, "40k_main_player_frame", 23.0f, 411.0f, 11.6f, 135.6f, "Background", QBarPlayerFrame,
                 null, false, onMissing);
            // 🔴 **2026-10-11（`A1521②` · 代理 `P` 现核）：这一块【不建】—— 原版 prefab 里它是【废件】。**
        //   **四条独立判据**：① prefab `Background Mask` 的 **`m_IsActive = 0`**；
        //   ② **原版运行期 dump 5 份 / 4 个状态全都 `False`**（`资料/原版实拍/menu_topbar_0927/…` + `menu_forge_1016/` 四份）；
        //   ③ **1920×1080 原生实拍逐像素**：该处 x 432..473 是信封的亮灰，而板的 `m_Color` g=0.1925
        //      ⇒ 若画了那里必然 g>0，实测 **x 412..426 的 g 通道恒为 0** ⇒ 那里没有板；
        //   ④ **全包 1134 个 MB 里指向它的 PPtr 一个都没有** · 全量反编译 26,282 份里 `"Background Mask"` 字符串 **0 命中** ·
        //      `PlayerProfileUIController` 的 8 个字段里**没有底板那一格**。
        //   ⇒ **它是原版 prefab 里的废件**（作者做了但没启用、也没人打开）⇒ **我们不建**。
        //   🔴 **删它的直接理由**：它 x `25.6..472.1` × y `14.9..60.5`，而 `InboxBtn`（信封）是 `425.30..480.30 × 15.50..55.50`
        //      ⇒ **重叠 85.09%**（`(46.80×40)/(55×40)`），实拍上信封只剩 ~11% ⇒ 用户参考图上「信封完整可见」而我们的只剩一条窄条。
        //   ⚠️ **原版可见的「名牌底」是【另一颗】**：`Player Profile/Background`（图 `40k_main_player frame`，右沿 **411**、与信封左沿 425.30 净空 **14.30**）
        //      —— 那一颗我们**已经有**（本函数上面那句 `Rect(p, "40k_main_player_frame", …)`）。
        //   🧨 **鉴别式**：把下面这句 `Rect(..., "Planer Name Background", …)` **加回来** ⇒ `Editor/MainMenuScene.cs` 那条反向断言立刻红。
        //   （留痕：`QBarNamePlate` 常量留着不删 —— 它只是队列号，留着无副作用；⛔ 但**别再往它上面挂件**。）
            // 玩家名：`fontSize 32` · `auto[10~32]` · `m_fontColor (0.9686,0.9137,0.7137)` · **不折行**
            // （文案 = `ProfileData.PlayerName`，**全工程唯一一份** —— 档案窗改名窗写的就是它）
            var pn = Text(p, ProfileData.PlayerName, 136.9f, 401.9f, 13.7f, 61.7f, 8,
                          new Color(0.9686f, 0.9137f, 0.7137f), "Player Name", 32f, QBarText,
                          TmpFont.Face.LatinUi);   // 原版 `Upper bar/Player Profile/Player Name` = Asar（普查 §2·5）
            if (pn != null)
            {
                pn.SetAutoFitBox(265f / 108f, 48f / 108f, 10f, 32f, 36f);
                pn.SetWrapping(false);
            }

            var av = New(p, "Avatar Item Small", 138.42f, 139.568f);
            // 盾框：**保宽高比**（原版 `m_PreserveAspect = 1`，39/39 都带）—— 拉伸会宽 1.6 倍
            Rect(av, "Player_Profile_Border", AvatarBorderBox, "Border", QBarAvatarFrame, null, true, onMissing);
            BuildTopAvatar(av, parts, onMissing);

            // ============================================================ 等级徽章（头像右下那颗圆牌）
            //
            // 🔴 **`A1637`（2026-10-12 用户真机点名）就地订正（铁律 5）** —— 原来这里用的是
            //    **`40k_topmarquee_currency_gold`（一块金砖）**，那是**读错了文档**：
            //      · 错记录 = `资料/主菜单_原版规格.md:111`（与 `:342`）把 `Player Level` 那一格的
            //        `m_Sprite` 记成了 `40k_topmarquee_currency_gold`；
            //      · **真值 = `UI_Button_Round_background`**（**深色圆 + 一圈细亮环**，237×237，图集
            //        `0_GeneralUI Atlas`，`m_Border` 四角**全 0** ⇒ 不是九宫格）—— 与用户实拍里那颗
            //        「圆环 + 数字」逐形吻合（`C:/Users/qjh36/Desktop/warpforge参考图/收藏—卡组的参考图.png` 左上角）。
            //    **三条独立判据（都现核过）**：
            //      ① 场景真值：`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2359.json`
            //         （`Player Level` 那颗 `Image`，GO pid **238**）的 `m_Sprite = {m_FileID: 6,
            //         m_PathID: **2381704724431365035**}` —— **这个 pid 与名无关，先前的读法是把 pid 猜成了名字**；
            //      ② 全库扫真包（`UnityPy` 扫 `D:/2/unity_run_ref/.../StandaloneWindows64/*.bundle` **84 个**）：
            //         `path_id == 2381704724431365035` 的对象**全库只有一个** —— `Sprite/UI_Button_Round_background`
            //         （⇒ 不是跨 CAB 撞号）；`素材/Warpforge原版/UI图集/去重资源/Sprite/UI_Button_Round_background_2381704724431365035.json`
            //         那份**带 pid 后缀**的导出是第二条独立见证；
            //      ③ 交叉树：`bundle_menus_assets_all` 里 **4 份** `Player Level`（档案/排位那两棵树）
            //         的 `Image.m_Sprite` **全是同一个 pid 2381704724431365035** ⇒ 两棵树同一个圆底。
            //    ⚠️ **`40k_topmarquee_currency_gold` 是【另一张】**（`m_Rect` 90×90、无图集、单独一张 Texture2D）
            //       —— 它该出现在资源计数器那一排（金币格），⛔ 不该出现在头像右下。
            //    🧨 **改坏法**：把这一行换回 `40k_topmarquee_currency_gold` ⇒ `Editor/MainMenuScene.cs`
            //       那条「`Player Level` 用的是 `UI_Button_Round_background`」立刻红。
            //
            // 🔴 **数字从哪来（同一轮查实）**：原版那颗字**不是** prefab 里的值 —— prefab 写的是空态串 `-`，
            //    运行期由 **`PlayerProfileUIController`**（那颗组件就在同一棵树上的 `Player Profile` 节点，
            //    `MonoBehaviour_1989.json`：`showPlayerLevel = 1` · `playerLevelGO → pid 238`（= 这一颗）·
            //    `playerLevelText → pid 2548`）写入：
            //      · `PlayerProfileUIController__Initialize.c` 末段 ⇒ `SetPlayerLevel(PlayerDataManager.CurrentLevel)`；
            //      · `PlayerProfileUIController__SetPlayerLevel.c` ⇒ `playerLevelText.text = level.ToString()`；
            //      · `__OnCollectorLevelUpdate.c` ⇒ 升级信号（`CollectorLevelChangeSignal`，等级在 +0x14）里再写一次。
            //    ⇒ **数据源 = 服务器**（`PlayerDataManager.CurrentLevel`）⇒ 本地取不到 ⇒ 我们**照 prefab 的空态串 `-`**
            //      （与档案窗那一格 `Shell/ProfileTab.cs` 的 `PlayerLevelText` **同一份口径**）；
            //      ⛔ **不许自己编一个数字填上**（那会把「查不到」画成「有数据」）。判据已在报告里写明。
            var lvl = New(p, "Player Level", 53.12f, 53.12f);
            Rect(lvl, ArtRoundBtn, 117.5f, 170.6f, 54.3f, 107.4f, "Icon", QBarLevel,
                 null, false, onMissing);
            var lv = Text(lvl, "-", 124.3f, 163.7f, 61.1f, 100.5f, 7, Color.white, "Player Level Text",
                          37.2f, QBarText, TmpFont.Face.LatinCardBody);   // 原版 `Player Level Text` = Pragati
            if (lv != null) lv.SetAutoFitBox(39.4f / 108f, 39.4f / 108f, 18f, 37.2f, 36.89f);
        }

        /// <summary>顶栏立绘那一格 —— **由边框那一格推出来**（容器 ×2、中心偏 (2.6, −6.427)、保宽高比）。
        /// ⚠️ 盒子**比盾牌框大**（会溢出屏幕左上角），这是原版 prefab 算出来的，不是我们挑的
        /// （推导逐条见下，2026-09-27 查实 —— **原文在 `Shell/MainMenuRuntime.cs`，2026-10-17（B25）随实现一起搬来**）。
        /// · prefab（`bundle_scenes_scenes_mainmenuwarpforge`）：`Image` 是**拉伸**在 `Image Container` 上
        ///   （`anchor(0,0)-(1,1)` · `sizeDelta(0,0)`），而 `Image` 的 **`m_LocalScale = 2.0`**
        ///   ⇒ 画出来 = 容器 × 2 = **(138.42×102.2)×2 = 276.84×204.4**；
        /// · 边框那一格同法算 = `(140.384×104.076)×1.25` = **175.48×130.095** —— 与我们实拍对上的那一格**逐位吻合** ✅
        ///   ⇒ 同一条推导链是可信的；
        /// · **两格的相对关系**：`Image` 中心 = 容器中心 + `(0,2.7)`、`Border` 中心 = 容器中心 + `(−2.6,−3.727)`
        ///   ⇒ 立绘中心 = 边框中心 + `(2.6, 6.427)`（prefab 是 y 向上，落到屏幕是 **−6.427**）。
        /// 🔴 **为什么不是「和边框同格」**（第一版那么做的，**是错的**）：把两种尺寸合成出来并排看，
        ///   立绘贴图的**实心部分**（`alpha>128` 的包围盒 = 512 里的 **220×306** = 43%×60%）：
        ///   · 「同格」 ⇒ 实心只有 **75.5×77.5**，而盾的孔径是 **161.8×121** ⇒ **矮 36%**（截图里人像浮在一圈黑中间）；
        ///   · 「×2」   ⇒ 实心 **119×121.8** ⇒ **高与孔径差 0.7%**（这不是巧合：贴图那圈 40% 的透明边距就是为这个留的）。
        ///   ⇒ **×2 才是原版的意图**。
        /// ⚠️ **同样是【保宽高比】画**（`RectTex` 最后那个 `true`）：原版 `m_PreserveAspect = 1`（39/39，见 `BuildPlayerProfile` 那段）。
        ///   保宽高比之后实绘 = `fit(512² → 276.84×204.4) = **204.4×204.4**`，居中于上面那个盒子的中心。
        ///   ⇒ 合成出来并排看过：**人像正好填满盾牌**（兜帽顶到上边框、肩到侧边框、盾尖正好在人像底）。
        /// ✅ **几何全部有尺子，已收工**：换成保宽高比之后，盾在**原版实拍里的量测**与我们逐点吻合
        ///   （原版 左 24 · 右 131 · 顶 13 · **盾尖 y=132(x≈83)** ／ 我们 左 23 · 右 128 · 顶 13 · **盾尖 y=132(x≈83)**），
        ///   而「运行期没人改这个 RectTransform」也由反编译确认（全库零 `SetNativeSize`；`AvatarDisplay` 17 个方法逐个读过）。
        /// 📌 用户 2026-09-27 定：**观感那条不用挂待办**（「以后我觉得不舒服再说」）⇒ 这里不留 `真 Play` 指针。
        /// 🔴 队列必须比盾框高（`QBarAvatarFrame 2991 &lt; QBarAvatar 2992`）：盾的中心是不透明黑，反了就是一块黑。</summary>
        const float TopAvatarL = -58.09f, TopAvatarR = 218.79f, TopAvatarT = -34.59f, TopAvatarB = 169.83f;

        static void BuildTopAvatar(Transform av, Parts parts, Action<string> onMissing)
        {
            var tex = LoadAvatar(ProfileData.AvatarArt, onMissing);
            if (tex == null) return;                   // 一张都取不到 ⇒ 与出厂态一致（只剩那面盾）
            parts.TopAvatar = RectTex(av, tex, TopAvatarL, TopAvatarR, TopAvatarT, TopAvatarB,
                                      "Image", QBarAvatar, true);
            parts.AvatarIndex = ProfileData.AvatarIndex;
        }

        /// <summary>玩家在档案窗改了头像 ⇒ 顶栏这一层跟着换（**每帧比一个 int** 就够，别另造事件机制）。
        /// ⚠️ 批处理下 `Update` 不跑 ⇒ **自检直接调这个方法**。</summary>
        public static void RefreshTopAvatarIfChanged(Parts parts, Action<string> onMissing)
        {
            if (parts == null || parts.TopAvatar == null) return;
            if (parts.AvatarIndex == ProfileData.AvatarIndex) return;
            var tex = LoadAvatar(ProfileData.AvatarArt, onMissing);
            if (tex == null) return;
            parts.TopAvatar.SetTexture(tex);
            parts.AvatarIndex = ProfileData.AvatarIndex;
        }

        /// <summary>头像那批图（`Resources/Art/avatars/`，名字**含空格、原样传**）。
        /// 取不到 ⇒ 走 `onMissing`（**出声**，不静默画个白块）。</summary>
        static Texture2D LoadAvatar(string art, Action<string> onMissing)
        {
            if (string.IsNullOrEmpty(art)) return null;
            var t = CardArt.Cosmetics(art);
            if (t == null && onMissing != null) onMissing(art);
            return t;
        }

        // ============================================================ 资源条（右上三项 + 上限）

        // ---- 逐格几何（原版 prefab 逐字段实读；出处 → `MainMenuRuntime` 那一份的长注释）----
        const float ResBarW = 671.05f, ResBarH = 71.165f;
        const float ResBarL = 1131.475f, ResBarR = 1802.525f, ResBarT = -0.0825f;
        const float ResSpacing = 44f, ResPadR = 5f;
        const float ItemH = 38.066f, ItemPadL = 20f, ItemPadR = 1f;
        const float PillH = 38f, PillPadL = 29f, PillPadR = 9f;
        static readonly Color PillTint = new Color(0.33019f, 0.26010f, 0.31567f, 1f);
        const float IconW = 60f, IconH = 59f;

        /// <summary>图标**左沿**相对【格左沿】的偏移 —— 原版 `Icon` 的 `m_AnchoredPosition.x = -8.5`
        /// （`RectTransform/RectTransform_371.json`：`m_AnchoredPosition (-8.5, 0)` · `m_Pivot (0, 0.5)` ·
        /// `m_AnchorMin/Max (0, 0.5)` ⇒ **锚在【格左沿】的竖直中点**）
        /// ⇒ 图标左沿落在格原点**左边 8.5px**（与药丸左端**重叠**）、**竖直居中于格**。
        /// <para>🔴 **为什么它是负数、为什么格宽里【没有】图标那 60px** —— 原版 `Icon` 上挂着
        /// `LayoutElement.m_IgnoreLayout = 1`：（`MonoBehaviour/MonoBehaviour_545.json` 的
        /// `"m_IgnoreLayout": 1`；同一件 `GameObject/Icon_238.json` 的组件表 = `[RT 371 · … · Image 399 · LayoutElement 545]`，
        /// 而 `GameObject/Resource Counter Item.json` 的组件表 = `[RT 359 · … · HorizontalLayoutGroup 206 ·
        /// ContentSizeFitter 529 · LayoutGroupContentFixer 516]` ⇒ 545 与 206 **同挂在父子两件上**、
        /// 545 挂的是子件 `Icon`）。
        /// ⇒ **uGUI 建 `rectChildren` 时会跳过它** ⇒ 它 ①**不进**格的 preferred 宽（格的宽由
        /// `ContentSizeFitter.m_HorizontalFit = 2 (PreferredSize)` 驱动 ⇒ 格宽 = `pad.L20 + 药丸宽 + pad.R1`，
        /// **不含 60**），②**不被**布局摆位（`-8.5` 根本不是布局排得出来的值：布局若排它，只会给出
        /// `pad.L20 + 药丸宽 = 161.51`）。⇒ **两件事都源自同一条**，⛔ 别只改一半（把格宽改对、却仍把图标
        /// 当布局子件摆在药丸右侧 = 本轮要修的那个错）。
        /// <para>同一条语义的先例（本仓已收口）→ `资料/已知的坑.md:2085-2090`；
        /// 同族先例 → `资料/历史/阶段2第3层_逐件记录.md:47`（`Clear filters`）。</para></summary>
        const float IconOffsetL = -8.5f;
        const float QtyFontPx = 42f, QtyFontBase = 36f, QtyFontMin = 10f, QtyCharSpacing = -1f;
        const float QtyWMin = 103.51f, QtyWMax = 153f;
        const float PillTexW = 44f, PillTexH = 39f;

        /// <summary>🔴 **建表序 = 原版 `List.Add` 序**（`GeneralMenuController__Initialize.c:51-146`）——
        /// 所以这里读的是 <see cref="MainMenuRuntime.CounterSpecs"/>（`campaignPoints · gachaTickets ·
        /// raidMedals · crystals · blackStones · gold`），**⛔ 不是** `DefaultCurrencies`
        /// （那是「哪几颗**恒显**」那一条判据，不是次序）。
        /// ⚠️ 本件 D13/D14 那一版就是错在这里（按 `DefaultCurrencies` 建 ⇒ 次序整个反过来）——
        /// 见文件头 ①。改坏法：把 `CounterSpecs` 换回 `DefaultCurrencies` ⇒ 画出来变
        /// `gold·crystals·blackStones`（`Editor/DeckScene.cs` 顶栏那一段的次序断言会红）。</summary>
        static void BuildResourcesBar(Transform parent, Action<string> onMissing)
        {
            var resBar = New(parent, "Resources Bar", ResBarW, ResBarH);
            var shown = new List<MainMenuRuntime.CounterSpec>();
            var specs = MainMenuRuntime.CounterSpecs;
            for (int i = 0; i < specs.Length; i++)
                if (MainMenuRuntime.ShouldShow(specs[i].Currency)) shown.Add(specs[i]);
            // 原版 `:205` 那条条件支：`Insert(…, 0, 0x48)`（energy 插在**最前**）—— 本地恒 false
            if (MainMenuRuntime.EnergyInResourcesBar) shown.Insert(0, MainMenuRuntime.EnergySpec);
            if (shown.Count == 0) return;

            var container = New(resBar, "Resources Container", 0f, ResBarH);
            var noIcon = new List<int>();              // 原版图标**本地没查到**的币种（出声用，同主菜单那份）

            // ③ **先量后建**：原版每格的宽是 `ContentSizeFitter` 按文字的 preferred 宽算的
            //    ⇒ 必须先有 `Label` 才量得到（同 `MainMenuRuntime` 那一段）。
            var labels = new List<Label>();
            var items = new List<Transform>();
            var textWl = new List<float>();
            var pillWl = new List<float>();
            var itemW = new List<float>();
            for (int i = 0; i < shown.Count; i++)
            {
                var spec = shown[i];
                var item = New(container, "Resource Counter Item", 0f, ItemH);
                var lb = Text(item, MainMenuRuntime.CounterText(MainMenuRuntime.OwnedOf(spec.Currency),
                                                                MainMenuRuntime.MaxOf(spec.Currency)),
                              0f, 1f, 0f, PillH, 8, Color.white, "Resource QuantityText", QtyFontPx, QBarText,
                              TmpFont.Face.LatinCardBody);   // R3：原版 `Resource QuantityText` = Pragati（下面按 `WorldW` 量药丸宽）
                if (lb != null) { lb.SetCharSpacing(QtyCharSpacing); lb.ForceRelayout(); }   // 字距要在量宽之前
                if (string.IsNullOrEmpty(spec.Icon)) noIcon.Add(spec.Currency);
                float nat = lb != null ? lb.WorldW * 108f : 0f;
                float textW = Mathf.Clamp(nat, QtyWMin, QtyWMax);
                float pw = PillPadL + textW + PillPadR;
                labels.Add(lb); items.Add(item); textWl.Add(textW); pillWl.Add(pw);
                // 🔴 格宽 = **原版布局组算出来的** = `pad.L(20) + 药丸宽 + pad.R(1)` —— **不含图标那 60px**
                //    （原版 `Icon` 带 `LayoutElement.m_IgnoreLayout = 1` ⇒ uGUI 不长它；格的宽由
                //     `ContentSizeFitter.m_HorizontalFit = 2 (PreferredSize)` 驱动。判据与出处 →
                //     `IconOffsetL` 那条注释，⛔ 别在这儿重抄第二份）。
                //    ⚠️ 实测对齐：`RectTransform_359.json` 的 `m_SizeDelta.y = 38.066` = 我们的 `ItemH`；
                //    出厂那一档药丸宽 `RT_372` = 141.51 ⇒ 格宽 = 20 + 141.51 + 1 = **162.51**。
                //    改坏法：把 `IconW` 加回来 ⇒ **每格宽多 60、四条共多 240** ⇒ 容器右对齐，
                //    整条往左顶 240px（第一条药丸左沿从 1015.5 掉到 775.5 —— 自检夹具那一档持有数下）。
                itemW.Add(ItemPadL + pw + ItemPadR);
            }
            float totalW = ResPadR + (shown.Count > 0 ? ResSpacing * (shown.Count - 1) : 0f);
            for (int i = 0; i < itemW.Count; i++) totalW += itemW[i];
            MenuDraw.SetPxSize(container, totalW, ResBarH);

            float x = ResBarR - totalW;
            for (int i = 0; i < shown.Count; i++)
            {
                var spec = shown[i];
                float iT = ResBarT + (ResBarH - ItemH) * 0.5f;
                float pL = x + ItemPadL, pR = pL + pillWl[i];
                var itemT = items[i];
                MenuDraw.SetPxSize(itemT, itemW[i], ItemH);

                // 药丸底 = **九宫格**（44×39 · `m_Border 15,15,15,15` · tint 暗紫灰）
                var pg = MenuDraw.Nine(itemT, Art("40k_topmarquee_currency_display_BW", onMissing),
                                       new PxRect(pL, iT, pR, iT + PillH), new Vector4(15f, 15f, 15f, 15f),
                                       PillTexW, PillTexH, QBarContent, PillTint, true, "Resource Bar Background");
                var lb = labels[i];
                if (lb != null)
                {
                    if (pg != null) lb.transform.SetParent(pg.transform, false);
                    var host = lb.transform.parent;
                    lb.transform.localPosition = Center(pL + PillPadL, pR - PillPadR, iT, iT + PillH)
                                               - (host != null ? host.localPosition : Vector3.zero);
                    lb.SetAutoFitBox(textWl[i] / 108f, PillH / 108f, QtyFontMin, QtyFontPx, QtyFontBase);
                    lb.SetWrapping(false);
                    MenuDraw.SetVAlign(lb, Label.VAlign.Midline, new PxRect(pL + PillPadL, iT, pR - PillPadR, iT + PillH));
                }
                // 🔴 图标在药丸的【左】边 —— 而且它**不参与布局**，所以位置**由 `IconOffsetL` 直接给**
                //    （⛔ 不是 `pR`、不是 `pL`）：格内 x = `[IconOffsetL, IconOffsetL + IconW]` = **[-8.5, 51.5]**
                //    （左沿在格原点**左边** 8.5px、与药丸左端**重叠**），
                //    y = **居中于格**（59 高 vs 格高 38.066 ⇒ **上下各溢出 10.467px**）。
                //    原版实读（第一权威）：`RectTransform/RectTransform_371.json` —— `m_AnchoredPosition (-8.5, 0)` ·
                //    `m_Pivot (0, 0.5)` · `m_SizeDelta (60, 59)` · `m_AnchorMin/Max (0, 0.5)`（锚在格左沿的竖直中点）。
                //    ⚠️ `keepAspect = true` 是**对的**：同件 `Image`（`MonoBehaviour_399.json`）`m_PreserveAspect = 1`。
                //    改坏法：摆回 `[pR, pR + IconW]`（药丸**右**边）⇒ 图标跑到药丸外侧，与原版左右相反。
                var icon = New(itemT, "Icon", IconW, IconH);
                if (!string.IsNullOrEmpty(spec.Icon))
                {
                    float iconL = x + IconOffsetL, iconTop = iT + (ItemH - IconH) * 0.5f;
                    // 🔴 **2026-10-18（N57）**：原版资源格的兄弟序 = `Resource Bar Background` → `Icon`
                    //   ⇒ **图标画在药丸之上**（原版实拍里图标压住数字框左端）。我们原来与药丸同队列
                    //   `QBarContent 2987` ⇒ 药丸后画/更近，把图标右侧切掉一截。
                    //   2988（`QBarBadge`）是现有最近的空档；红点在齿轮/信封上，与本处**区域不重叠**。
                    Rect(icon, spec.Icon, iconL, iconL + IconW, iconTop, iconTop + IconH,
                         "Image", QBarBadge, null, true, onMissing);
                }
                x += itemW[i] + ResSpacing;
            }

            // ⑤ 出声：**原版图标本地没查到**的币种（⛔ 不静默 —— 那种格只画药丸 + 数字）。判据 → W374 §六
            if (noIcon.Count > 0)
                Debug.LogWarning("[TopBar] ⚠️ 顶栏有 " + noIcon.Count + " 颗币种**原版图标本地没查到**"
                                 + "（" + string.Join("、", MainMenuRuntime.CurrencyNames(noIcon)) + "）"
                                 + "⇒ 只画药丸 + 数字，**不画圆图标**。判据与修法 → `资料/普查产出_1013/W374_资源计数器.md` §六");
        }

        // ============================================================ 坐标工具（与 MainMenuRuntime 同一份换算）

        static Vector3 Center(float x1, float x2, float y1, float y2) => LayoutSpace.RectCenter(x1, y1, x2, y2);
        static float H(float y1, float y2) { return LayoutSpace.Px(y2 - y1); }

        /// <summary>空节点（`RectTransform`）+ 原版那一件的 `sizeDelta`。
        /// ⚠️ 与 `MainMenuRuntime.New` **同一条不变式**：只写尺寸、**故意不写位置** ——
        /// 本文件所有叶片都把**绝对**世界坐标当 `localPosition` 用，前提是**每个父级的 `position` 恒为零**
        /// （`SetParent(parent, false)` + 根出厂在原点）。</summary>
        static Transform New(Transform parent, string name, float wPx, float hPx)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            MenuDraw.SetPxSize(go.transform, wPx, hPx);
            return go.transform;
        }

        static Texture2D Art(string name, Action<string> onMissing)
        {
            var t = CardArt.MenuUi(name);
            if (t == null && onMissing != null) onMissing(name);
            return t;
        }

        /// <summary>`m_PreserveAspect = 1` 的**唯一实现**（本件）：把框**内缩**成贴图的比例，返回
        /// 「**画出来那块**」（⛔ 只改框，贴图一个字不动 —— 原版 `Image.GetDrawingDimensions`：
        /// 图比框宽 ⇒ 按宽定，否则按高定；与 `MenuDraw.Rect` 那条**同一条算法**）。
        ///
        /// <para>🔴 **2026-10-14（`A1814`）它有了第二个用途**：`Wire` 要用它把「画出来那块」算出来，
        /// 再拿【原版那件自己的矩形】去算命中区 —— 这两个数在保宽高比时**本来就不相等**
        /// （齿轮 87.78×61.73 的框，画出来只有 61.73²）。⛔ 别把两件事合成一个数。</para>
        /// <para>贴图比例拿不到（`tex == null` / 高 0）⇒ **原样返回**（不内缩）。</para></summary>
        static PxRect FitAspect(Texture tex, float x1, float x2, float y1, float y2)
        {
            if (tex == null || tex.height <= 0) return new PxRect(x1, y1, x2, y2);
            float sprAspect = (float)tex.width / tex.height, rectAspect = (x2 - x1) / Mathf.Max(1e-6f, y2 - y1);
            if (sprAspect > rectAspect) { float nh = (x2 - x1) / sprAspect, d = ((y2 - y1) - nh) * 0.5f; y1 += d; y2 -= d; }
            else { float nw = (y2 - y1) * sprAspect, d = ((x2 - x1) - nw) * 0.5f; x1 += d; x2 -= d; }
            return new PxRect(x1, y1, x2, y2);
        }

        static ImageQuad Rect(Transform parent, string art, float x1, float x2, float y1, float y2, string name,
                              int q, Color? tint, bool keepAspect, Action<string> onMissing)
        {
            // `art == null` = **纯色块**（原版那种「Image 没 sprite、只有 m_Color」的件）
            var tex = art == null ? CardArt.Solid() : Art(art, onMissing);
            if (tex == null) return null;
            var quad = RectTex(parent, tex, x1, x2, y1, y2, name, q, keepAspect);
            if (quad != null && tint.HasValue) quad.SetTint(tint.Value);
            return quad;
        }

        /// <summary>🆕 **2026-10-14（`A1814`）**：吃 `PxRect` 的薄包装 —— 给「**同一个框既要拿去建、又要拿
        /// 去算命中区**」的那 5 颗钮用（那 4 颗 + 头像）。⛔ 别把框写成两份字面量（「两处写同一条规则 = 迟早不一致」）。</summary>
        static ImageQuad Rect(Transform parent, string art, PxRect box, string name, int q, Color? tint,
                              bool keepAspect, Action<string> onMissing)
            => Rect(parent, art, box.x1, box.x2, box.y1, box.y2, name, q, tint, keepAspect, onMissing);

        /// <summary>`keepAspect = true` ⇒ 先把矩形**内缩**成贴图的宽高比再建（原版 `m_PreserveAspect = 1`；
        /// 算法与 `MenuDraw.Rect` 那条**同一条**）。
        /// 🔴 **2026-10-14（`A1814`）**：内缩那一段收口到 <see cref="FitAspect"/> —— 因为 `Wire` 也要算
        /// 「**画出来那块**」（命中区要用**两个数**：原版布局框 vs 画出来那块）。</summary>
        static ImageQuad RectTex(Transform parent, Texture tex, float x1, float x2, float y1, float y2, string name,
                                 int q, bool keepAspect)
        {
            if (tex == null) return null;
            if (keepAspect)
            {
                var f = FitAspect(tex, x1, x2, y1, y2);
                x1 = f.x1; x2 = f.x2; y1 = f.y1; y2 = f.y2;
            }
            var quad = ImageQuad.Create(parent, tex, Center(x1, x2, y1, y2), H(y1, y2),
                                        new Vector2(0.5f, 0.5f), name);
            if (quad != null)
            {
                quad.SetAspect((x2 - x1) / (y2 - y1));
                quad.SetRenderQueue(q);
            }
            return quad;
        }

        /// <summary>按像素矩形摆一段文字（**居中**）。`fontPx` = 原版 TMP 的 `m_fontSize`（画布像素）。
        /// ⚠️ 走 `Label.SetGlyphHeight`（工程里实测过的换算），**别自己乘 108** —— 那会大到 2.7 倍
        /// （`MainMenuRuntime.Text` 的 doc 里有那次实测）。</summary>
        static Label Text(Transform parent, string text, float x1, float x2, float y1, float y2, int scale,
                          Color color, string name, float fontPx, int queue,
                          TmpFont.Face face = TmpFont.Face.Cjk)
        {
            var lb = Label.Create(parent, text, Center(x1, x2, y1, y2), scale, color,
                                  new Vector2(0.5f, 0.5f), name);
            if (lb == null) return null;
            lb.SetRenderQueue(queue);
            // 🔴 2026-10-17（A1024③ 逐行对账）：可选字体面（默认 `Cjk` ⇒ 旧调用点零变化）。
            //    必须在 `SetGlyphHeight` 之前（字号按 `ActualFace` 的 em 换算，判据 → `MenuDraw.TextCore`）。
            if (face != TmpFont.Face.Cjk) lb.SetFace(face);
            if (fontPx > 0f) lb.SetGlyphHeight(fontPx / 108f);
            return lb;
        }
    }
}
