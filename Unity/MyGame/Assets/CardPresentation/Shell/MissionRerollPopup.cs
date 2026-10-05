// MissionRerollPopup.cs — 🆕 A23：每日任务行那颗「垃圾桶」开的重摇窗（原版 `MissionReRollPopup`）
//
// ============================ 出处（唯一正本） ============================
// **`资料/待办判据_阶段二与联机.md` §A23**（机制链 + `ReRollPopup Variant` 的逐节点实测表，2026-10-04 查实）。
// 复现命令（可复算本文里每一个数字）：
//     `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "ReRollPopup Variant" --depth 6`
//     （绝对矩形 · 1920×1080 · 左上原点 · y 向下）
//
// **窗口字段**（MB 实读，prefab 根 `ReRollPopup Variant` 上的 `MissionReRollPopup`）：
//   `type = 1 (Popup)` · `windowsPlacement = 15 (Popup)` · **`closeOnESC = 0`** ·
//   `extraScaleSmallScreen = 1.0` · `updateNavPanel = 0` · `useDefaultCloseSoundIfNull = 1` ·
//   `openSound` / `closeSound` = 空。
//   绑定字段（**全部运行期赋**）：`acceptButton` / `cancelButton` / `description`，
//   以及 `Price Display>Generic UI Button` 上的 `PriceDisplayButton`（`blinkEffect`/`button`/`priceDisplay`）。
//
// **图**（按 pid 反查**真包**定的名 —— 解包目录里这几个 pid 反查不到，走 `sprite_pid_map()`）：
//   `Generic Popup Background` / `Mask` = `40k_popup`（359×336 · 九宫 **(169,160,169,160)** · Sliced）·
//   `Background fill` = `40k_popup_texture`（128×128 · **Tiled** `ppuMul=2.0` ⇒ 一格 128÷2 = **64 画布 px**）·
//   两颗钮的底 = `40K_button`（489×107 · **Simple + preserveAspect** · 色 **(0.369,0.894,0.587,1)** 绿）。
//   🔴 **换图**：两颗钮的 `EverguildButton` 都是 `trans=2 (SpriteSwap)` ⇒
//   `m_SpriteState.m_HighlightedSprite = 40K_button_hover` · `m_PressedSprite = 40K_button_pressed`（实读）。
//
// ---- 点它干什么（原版链，反编译实证）----
//   `MissionReRollButton.__c__DisplayClass3_0___Setup_b__0.c`：
//     `WindowsManager.OpenWindow(<WindowsManager>.Instance, *(button+0x30), new MissionReRollPopupContext{…})`
//   🔴 **`MissionReRollPopup__Open.c` 全文（94 行）【没有一处给 `description` 赋值】**（2026-10-04 逐行复核 ——
//     原来这里写的是「给 `description` 赋值」，**那是错的**，本行就是订正痕迹）。那 94 行只做三件事：
//       ① ctx 取用 + 类型校验（不是 `MissionReRollPopupContext` ⇒ `CustomDebug.LogError` 后直接 return）；
//       ② **`cancelButton`**（字段偏移 `+0x78`）那颗钮的 `onClick` 挂上本窗的**虚方法**
//          （`UnityEvent.AddListener`；本窗只覆写了 `Open`/`Close` 两个虚方法 —— 见类桩
//          `Assembly-CSharp/MissionReRollPopup.cs` ⇒ 挂上去的就是 **`Close`**）。
//          🔴 **2026-10-04 A36-M2 订正**：这里原来写的是 **`acceptButton`** —— **错**。
//          `MissionReRollPopup__Open.c:67-73` 挂的是 `param_1[0xf]` = `+0x78` = `cancelButton`；
//          而 `+0x80` 的 `acceptButton` 是**类型 `PriceDisplayButton` 的那一颗**（类桩字段序
//          `description / cancelButton / acceptButton` 与 `__Open_b__0.c:16-18` 拿 `+0x80` 去
//          `PriceDisplayButton__set_Interactable(false)` 交叉坐实）—— 它归 ③ 管，不是 ②。
//       ③ `PriceDisplayButton.Setup(priceDisplay, price, 1, 0)` → 再把 **`priceDisplay.OnClick`** 挂到
//          `MissionReRollPopup.__c__DisplayClass3_0___Open_b__0.c`（= `Confirm` 那段）上。
//     ⇒ `description` / `cancelButton` / `acceptButton` **三个引用在 prefab 里本来就绑好了**
//     （MB `MonoBehaviour_-390430364774009725.json` 实读：`description` = pid 829309068157613187 ·
//      `cancelButton` = 3864082125636465795 · `acceptButton` = 2065195850568070275，**三个都非 0**）
//     ⇒ 原版**不需要**在 `Open` 里再赋一次。本文件照这个结构建（文案直接给 `MessageText`）。
//   `MissionReRollPopup.__c__DisplayClass3_0___Open_b__0.c`（= `Confirm`）：
//     `PriceDisplayButton.Interactable = false` → 发 `RerollChallenge`（**服务端**）。
//   `MissionReRollPopup__Close.c`：`WindowsManager.GetOpenWindow<带页签的窗>()` 拿到就
//     `GameWindowWithTabs.ChangeTab<MissionsTab>`（**换回那一页**）**再** `GameWindow.Close` —— 见本文件 `Close()` 的覆写。
//
// ---- 🔴 四条「我们挑的 / 查不到」（铁律 3：逐条标出来，不许冒充原版）----
//   ① **换一条的规则全是我们定的**（原版走服务端 `MissionEvent…RerollChallenge` + `ReRollMissionResult`）
//      —— 规则与池子在 `DailyData.RerollDaily`（那里逐条标着）。**单机没有服务器**。
//   ② **不扣钱**：价钱格（`icon` 56² scl1.2 + `text` 占位 `300,00`）**照原版外观建**，
//      但点 `Confirm` **不扣任何资源** —— 用户 2026-09-17 拍板「**不做真实经济**：资源固定 9999」。
//      ⇒ 这是铁律 11 允许的「用户明确拍板不做」那一档，**不是我们偷懒**；价钱格**没删**（删了才是偏离）。
//      ⚠️ `MissionData.get_RerollPrice` 的方法体在 `d:/2/tools/decomp_full/` 里是**错桩**（读出来是别的函数）
//      ⇒ **真价钱本地读不到**；`300,00` 是 **prefab 出厂占位**（与 `BoosterInfoPopup` 同一个），**别当真实价钱**。
//   ③ **`icon` 只有节点、不画图**：原版那一格的 `Image.m_Sprite` 出厂就是**空**，
//      运行期由 `PriceDisplay__Setup` 按 `CurrencyExtensions.ToCurrency(price.Type)` 赋**那枚货币的图标** ——
//      价钱与货币本地都读不到 ⇒ **没有判据可画**。照 `BoosterInfoPopup.foreground` 那条先例：
//      **只建节点、不画**（不拿别的货币图冒充），并在日志里出声。
//   ④ **`closeOnESC = 0`（ESC 不关这扇窗）** —— MB 原文如此。⚠️ 派单里写的是「ESC ⇒ 关窗什么都不做」，
//      与 MB 实读**相反**；这里**按原版**（工程里 `closeOnEsc` 目前只是**记录值**，
//      全工程没有任何一处消费它：`grep closeOnEsc` 只有各窗 `Create()` 里的赋值 + 断言）。
//
// ---- ⚠️ 两处我们的引擎做不到、按等效做法处理（都出声）----
//   · `Mask`（`Image` + `Mask` **`showGraphic = 0`**）：原版这一件**不渲染**（只当模板用）。
//     我们的引擎没有 stencil mask ⇒ **只建节点、不画那张 `40k_popup`**（画了会变成**双层边框**）。
//     它底下 `Background fill` 的矩形与它**完全相等** ⇒ 几何上「裁到 `Mask` 框内」是恒等操作，视觉一致。
//   · `Buttons` / `Price Display` 两处 `HorizontalLayoutGroup` 的运行期排布：
//     我们不用布局系统，**直接照布局跑完的那个矩形摆**（`icon` 那 56² 与 `text` 左边缘 1232.80 是
//     **`menu_dump.py` 的布局复算值** —— ⚠️ **不是原始 JSON 里写着的**，见 `ConfirmTextR` 的注释）；
//     `Confirm` 那一段的右边缘 = 价钱格左边缘 1160.10 − 间距 **12.5** = **1147.60** —— ⚠️ **推导值**，
//     ⚠️ 两个子件都是**零宽框**（`ContentSizeFitter` 撑着）⇒ 价钱串一变宽就会跟着变，见 `ConfirmTextR` 的注释）。
//
// ---- 🔴 2026-10-05（A59）：本窗 **8 个节点**的 x 按工具【完整 uGUI】重落（y 一个没动）----
//   起因：`工具/menu_dump.py` 的 `_child_sizes` 之前漏了 uGUI 同一个函数里的三处
//   （`HorizontalOrVerticalLayoutGroup.cs:206` 的 `childSize` · `:213` 的 `offsetInCell` ·
//   `LayoutUtility.GetFlexibleSize`）⇒ 老 dump 值**整个窗户的横坐标都是错的**。补全后两处 HLG 各变一批：
//     · 外层 `Buttons`（`ctrlW=0 expandW=1 align=4` · 容器 775.4 宽）：余额 75.4 按 flexible **平分进两个格子**
//       （每格 +37.7），子件再按 `alignmentOnAxis = 0.5` **居中在格子里**（+18.85）
//       ⇒ 两颗钮各 **外移 18.85 / 内移 18.85**（`ButtonLeft` 610→**591.15** · `Price Display` 960→**978.85**）。
//     · 内层 `Price Display`（`ctrlW=0 expandW=0 scaleW=1`）：`SetChildAlongAxisWithScale` 里
//       `anchoredPosition.x = pos + sizeDelta.x × pivot.x × scaleFactor` ⇒ **步进吃 `localScale`**
//       ⇒ `icon` 中心 1169.25→**1193.70**、`text` 左 1202.75→**1232.80**。
//   🔴 **交叉校验（不是拿同一份算式自证）**：prefab 里**存下来**的 `anchoredPosition` 就是布局跑完之后的值，
//   与 W3 模型逐值吻合 —— `icon` **33.6**（= `56×0.5×1.2`）· 内层 `Price Display` **181.25** ·
//   `ButtonLeft` **193.85**（⇒ `572.3 + 193.85 − 350×0.5 = 591.15`）· confirm `Button Text` **168.75**
//   （⇒ `978.85 + 168.75 = 1147.60`）。**旧值（610 / 960 / 1141.25 / 1169.25 …）就是漏了这三处的产物。**
//   ⚠️ 复现：`python 工具/menu_dump.py bundle_menus_assets_all "ReRollPopup Variant" --depth 12 --md`
//     （`--md` 那张表里 `icon` 那行会印 `布局 56.00×56.00 ×1.2 ⇒ 视觉 67.20×67.20` ——
//      ⚠️ 它的「绝对矩形」列仍印**未缩放**的布局框 `1165.70..1221.70`，见 A59 另开的那件 `rect_of` 死参欠账）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>开这扇窗时塞给它的东西（原版 `MissionReRollPopupContext` 的等价物）。
    /// 原版那两个字段是 `Mission` / `Challenge`（服务端对象）⇒ 我们**只带一个行号**，
    /// 另加一个「换完了叫我」的回调（宿主 `MissionsTab` 拿它重建整页）。</summary>
    public sealed class MissionRerollContext
    {
        /// <summary>要重摇的那一行（`Daily Missions Holder` 里第几行，0 起）。</summary>
        public int Index;
        /// <summary>换完之后的通知（可空 —— 自检会直调，没有宿主也能跑）。</summary>
        public System.Action OnRerolled;
    }

    /// <summary>原版 `MissionReRollPopup`（`ReRollPopup Variant`）—— 点每日任务行那颗垃圾桶开的「重摇」确认窗。</summary>
    public class MissionRerollPopup : GameWindow
    {
        // 渲染队列：**必须高于所有「页」**（奖励窗那一档最高 3014、锻造页到 3027、战役页到 3064）。
        // 3080–3099 是全工程的一段**空档**（`BoosterInfoPopup` 到 3079、练习窗从 3100 起、
        // `CardDetailPopup` 从 3105 起）⇒ 放这里既在页之上、又在别的弹窗之下。
        // ⚠️ 新增任何一页/一扇窗都要回头看这个数（`RewardsScene` 有一条断言钉住「弹窗 > 页 > 窗」）。
        public const int QBase = 3080;
        // ⚠️ `public`（2026-10-04 A47 接线批）：自检宿主要拿 `QShade`/`QHit` 核「压暗命中区档 = 压暗层那一档
        //    且严格 < 本窗内容命中区最低档」这条不变量（`MenuDraw.ShadeRuleOk`）。
        // 🔴 **2026-10-08（A220）收窄**：脚本逐常量扫全工程 `.cs` 的**外部代码引用**（限定名
        //    `MissionRerollPopup.<档>`，已剔注释）—— `QShade` **1 处** · `QHit` **2 处**
        //    （都在 `Editor/RewardsScene.cs` 那一段：`MenuDraw.CheckShadeRule` 一条 +
        //    `CheckAbsorbRule` 一条）⇒ 这两个留 `public`；同批一起放宽的另外 6 个 **外部 0 处**
        //    ⇒ 回 `const`（只有本类自用）。整段理由见 `BoosterPackOpenWindow.cs:93-103`。
        //    ⚠️ **`QBase` 不在本次收窄范围**（自 2026-09-2x 起就是 `public`，本批没放宽它；
        //    `Editor/RewardsScene.cs` 只在**一句注释**里提到它）。
        public const int QShade = QBase;       // 3080 压暗整屏
        public const int QHit = QBase + 8;     // 3088 窗内命中区（压暗层那一档 = `QShade` 的注释见下）
        const int QPanel = QBase + 1;          // 3081 窗底（九宫格）
        const int QFill = QBase + 2;           // 3082 内部平铺底纹
        const int QText = QBase + 3;           // 3083 文案
        const int QBtn = QBase + 4;            // 3084 两颗钮的底图
        const int QBtnText = QBase + 5;        // 3085 钮上的字
        const int QPrice = QBase + 6;          // 3086 价钱格的字

        /// <summary>「点窗外关窗」那个命中区的档（= 压暗层自己那一档）。
        /// 🔴 **判据 = `BoosterInfoPopup.QShadeHit` 那条**（2026-10-03 实测踩出来的）：
        /// `ImageQuad` 的世界 z 恒为 0 ⇒ **同队列时 `PointerLayer` 只能靠枚举顺序挑赢家** ⇒
        /// 压暗层会把窗内的命中区抢走。要唯一只能靠**队列分档**：
        /// 压暗层**严格低于**窗内容（`QHit`）、同时**高于它下面那一层**（奖励窗那一档最高 3014）。</summary>
        const int QShadeHit = QShade;

        // ============================================================ 真值（§A23 二 那张逐节点表）

        static readonly PxRect DarkR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        static readonly Color DarkTint = new Color(0f, 0f, 0f, 0.773f);      // `Menu Dark Background` 的色
        static readonly PxRect WindowR = new PxRect(535f, 245f, 1385f, 675f);   // **850 × 430**

        const string ArtPopup = "40k_popup";                 // 359×336 · 九宫 (169,160,169,160)
        const float PopupTexW = 359f, PopupTexH = 336f;
        static readonly Vector4 PopupBorder = new Vector4(169f, 160f, 169f, 160f);

        static readonly PxRect MaskR = new PxRect(545.4f, 254.4f, 1375.1f, 665.2f);
        const string ArtFill = "40k_popup_texture";          // 128×128 · Tiled ppuMul 2.0 ⇒ 一格 64
        const float FillTilePx = 64f;

        static readonly PxRect MsgR = new PxRect(575f, 273.8f, 1345f, 546.2f);          // 770 × 272.4
        /// <summary>`MessageText` 的原文（prefab 出厂文本；原版运行期会被 I2 词条覆盖，
        /// **词条表在远端 CCD、本地一个 value 都没有** ⇒ 照别处的做法用**出厂原文**，不自己编词）。</summary>
        public const string TxtMessage = "Discard this mission and receive a new one?";

        /// <summary>两颗钮那一行。⚠️ 它自己的框**没变**（`775.4 × 90`）—— 变的是**组里两个子件**
        /// （`expandW=1` ⇒ 余额平分进两格、子件居中在格里 ⇒ 各 ±18.85，见文件头 A59 那段）。</summary>
        static readonly PxRect ButtonsR = new PxRect(572.3f, 534f, 1347.7f, 624f);     // HLG sp 0
        /// <summary>🔴 **2026-10-05（A59）`610..960` ⇒ `591.15..941.15`**（整颗左移 18.85：
        /// `expandW=1` 把余额 75.4 平分进两格（每格 +37.7），子件再按 `align=4 (MiddleCenter)`
        /// **居中在格子里**（+18.85 = `0.5 × 37.7`）⇒ 左键 = `572.3 + 18.85`。</summary>
        static readonly PxRect CancelR = new PxRect(591.15f, 541f, 941.15f, 617f);     // **350 × 76**
        /// <summary>`ButtonLeft/Button Text`（`sz=(-26,0)` ⇒ 父框两边各收 13）—— 跟着 `CancelR` 走
        /// （🔴 A59：`623..947` ⇒ **`604.15..928.15`**，宽 **324** 没变）。</summary>
        static readonly PxRect CancelTxR = new PxRect(604.15f, 541f, 928.15f, 617f);   // `sz=(-26,0)`
        /// <summary>🔴 **2026-10-05（A59）`960..1310` ⇒ `978.85..1328.85`**（整颗右移 18.85：它是第 2 格里的子件，
        /// 第 2 格起于 `572.3 + 387.7`，子件居中在格里再 +18.85 ⇒ `572.3 + 387.7 + 18.85`）。
        /// ⚠️ 原版这一层的**子件** `Generic UI Button` 与它**同矩形**（`ctrlW=0 ctrlH=0` + 自己没被布局驱动）。</summary>
        static readonly PxRect PriceBtnR = new PxRect(978.85f, 541f, 1328.85f, 617f);  // **350 × 76**
        /// <summary>`Confirm` 那段字的**右边缘** = 价钱格左边缘 **1160.10** − `Generic UI Button` 那条 HLG 的
        /// 间距 **12.5** = **1147.60**。
        /// 🔴 **出处（2026-10-04 A36-M3 订正）**：这是 **`menu_dump.py` 跑完布局【复算】出来的值**
        /// （`python 工具/menu_dump.py bundle_menus_assets_all "ReRollPopup Variant" --depth 12 --md`），
        /// **不是原始 JSON 里写着的** —— 本行 1.0 版写「prefab `Button Text` 矩形 `1128.75..1128.75` **原始 JSON 实读**」，
        /// 与它自己下一行「⚠️ 推导值」**自相矛盾**（铁律 5）。原始 JSON 里那颗 `Button Text` 是**模板位**：
        /// `m_SizeDelta = (0,76)`；布局跑完才有的那个数是 `m_AnchoredPosition.x = 168.75`
        /// （= 父框左边缘加它，正好落在 1147.60）。
        /// 🔴 **而且它是【推导值】，别当成能直接抄的固定数**（铁律 5·c「一个值 ≠ 全部情况」）：
        /// 那一颗 `Button Text` 的**框宽是 0**（`ContentSizeFitterMinMax` 撑着）、价钱格那条内层 `Price Display`
        /// 的框宽**也是 0**（`ContentSizeFitter` 撑着）⇒ 父级 HLG（`spacing=12.5` · **`align=4 (MiddleCenter)`** ·
        /// `ctrlW=0 ctrlH=1`）把「0 + 12.5 + 0」这一小段**居中**在 350 宽的钮里 ⇒
        /// 左边缘 = **978.85** + (350 − 12.5)/2 = **1147.60**（`978.85` = 那颗钮 `Generic UI Button` 框的左边缘）。
        /// ⇒ **价钱串一变宽（运行期真价钱进来、CSF 把价钱格撑开），这两件会一起往中间收**，就不再是 1147.60 了。
        /// 我们复刻的是 **prefab 出厂那一态**（价钱 `300,00`、两个子件都还没被 CSF 撑开）——
        /// 因为真价钱与货币类型**本地读不到**（见文件头 ③），没有第二态可算。
        /// 🔴 **2026-10-05（A59）`1128.75` ⇒ `1147.60`（+18.85）**：那颗钮 `Generic UI Button` 自己没动尺寸，
        /// 但它的**框**被外层 `Buttons` 组从 `960..1310` 挪到了 `978.85..1328.85` ⇒ 里面这一段的绝对位置跟着走
        /// （这一条就是「外层一动、内层绝对坐标再变一次」那件事，`普查产出_1004/menu_dump_影响面_1005.md:111-119` 说的）。</summary>
        const float ConfirmTextR = 1147.60f;
        /// <summary>价钱格（内层 `Price Display`，`HorizontalLayoutGroup` sp **5.5** · pad 0,0,10,10）。
        /// 它自己被 `ContentSizeFitter` 撑开 ⇒ 出厂矩形宽 0；**有尺寸的是它的两个子件**。
        /// 🔴 **2026-10-05（A59）`1141.25` ⇒ `1160.10`（+18.85）**：它的**父链**（`Generic UI Button`）
        /// 被外层 `Buttons` 组从 `960..1310` 挪到了 `978.85..1328.85`（见 `PriceBtnR`），
        /// 它在父框里的相对位置没变（`anchoredPosition.x = 181.25` ⇒ `978.85 + 181.25`）。</summary>
        static readonly PxRect PriceCellR = new PxRect(1160.10f, 541f, 1160.10f, 617f);
        /// <summary>价钱格的 `icon`。🔴 **原版给的 rect 是 56²**（`m_SizeDelta = (56,0)` ⇒ 布局后框宽 56），
        /// **但那个节点的 `m_LocalScale = 1.2`（两轴，pivot `(0.5,0.5)`）** ⇒
        /// **真画出来是 67.2²、绕同一个中心放大**。这里存的就是**烘过缩放的那个矩形**：
        ///   · **中心** = `x = 1160.10 + 33.6 = 1193.70`（`y = 579` 不变）
        ///     〔`1160.10` = 父框（内层 `Price Display`）的左边缘；`33.6` = 真 uGUI 的那条
        ///      `anchoredPosition.x = pos(0) + sizeDelta.x(56) × pivot.x(0.5) × scaleFactor(1.2)`〕
        ///   · **半宽/半高** = `56 × 1.2 ÷ 2 = 33.6` ⇒ **67.2²（两轴同值）**
        ///   ⇒ `1160.10..1227.30 × 545.4..612.6`
        /// 照 `BoosterInfoPopup` 那条先例「矩形 51.88² × localScale 1.2 ⇒ 实际画出来 62.26²」（**同样保中心**），
        /// 也就是 `CampaignRewardWindow` 那条规矩「**缩放要烘进矩形，别给父设 `localScale` 再照常摆子件**」。
        /// 🔴 **2026-10-04 A36-M4 订正（这半条仍然成立）**：`icon` 的右边缘**不等于**价钱文字左边缘 ——
        /// 「两条独立算式撞在一起、可当交叉校验」那个说法**不成立**。正确的两条关系是：
        /// **左**边缘 = 父框左边缘（`1160.10` —— 放大正好绕 pivot，把 `anchoredPosition.x` 走满半个视觉宽）、
        /// 价钱文字左边缘 = **右**边缘 + HLG 间距（`1227.30 + 5.5 = 1232.80`）。
        /// ⇒ **以【保中心】为准**（缩放绕的就是 pivot ⇒ 中心才是不变量；y 轴本来也是这么算的：`579 ± 33.6`）。
        /// ⚠️ 这一格**现在没有图**（原版出厂 `m_Sprite` 空、运行期按货币赋 —— 见文件头 ③）⇒ 尺寸**量不出来**
        /// （空节点没有 `ImageQuad`），自检只能钉它的**中心**；尺寸靠这条注释 + 原始 JSON 出处。
        /// 🔴 **M5 那两个值（`1174.85` / `1213.95`）已作废** —— 它们是**中间态**（只补了 `m_ChildScale`、
        /// 没补 `flexible`/`offsetInCell`）算出来的 ⇒ **别再照它们核**；真值 = **`1193.70` / `1232.80`**，
        /// **2026-10-05（A59）已落地**（本行与 `PriceTextR`、`Editor/RewardsScene.cs` 的断言同批改完）。</summary>
        static readonly PxRect IconR = new PxRect(1160.10f, 545.4f, 1227.30f, 612.6f);   // 56² × **scl 1.2** = 67.2²（**保中心**）
        /// <summary>价钱那串字的左边缘 = **父框（内层 `Price Display`）左边缘 `1160.10`** + `56 × 1.2`（`icon` 烘过缩放后的宽）
        /// + HLG 间距 **5.5** = **1232.80**（**`menu_dump` 的布局复算值** —— 原始 JSON 里那颗 `text` 是**零宽的模板位**，
        /// 不是这个数；等价的写法 = `icon` 烘过缩放后的右边缘 `1227.30` + 5.5）。
        /// ⚠️ **2026-10-04 A36-M4**：这一行原来写「与 `icon` 烘过 1.2 之后的右边缘**恰好同值** —— 两条独立算式
        /// 撞在一起，可当交叉校验」—— **不成立**（`5.5 ≠ 5.6`，差 0.1）⇒ **别再拿它当交叉校验**。
        /// 🔴 **2026-10-05（A59）`1202.75` ⇒ `1232.80`**：两截**都**变了 ——
        /// ① 父框左边缘 `1141.25 ⇒ 1160.10`（+18.85）；② 步进 `56 ⇒ 56 × 1.2`
        /// （`scaleW=1` ⇒ 真 uGUI 的 `pos += childSize × scaleFactor + spacing`，见文件头 A59 段）。
        /// ⚠️ M5 记的 `1213.95` **已作废**（中间态的值）—— 真值 **1232.80**。</summary>
        static readonly PxRect PriceTextR = new PxRect(1232.80f, 551f, 1232.80f, 607f);
        /// <summary>价钱那格的**出厂占位串**（原版运行期按 `RerollPrice` 覆盖）。⚠️ **不是真价钱**（见文件头 ②）。</summary>
        public const string PricePlaceholder = "300,00";

        const string ArtButton = "40K_button";
        static readonly Color BtnTint = new Color(0.369f, 0.894f, 0.587f, 1f);         // 绿（两颗都一样）
        static readonly Color BtnTint2 = new Color(0.369f, 0.894f, 0.588f, 1f);        // `Generic UI Button` 的实测值

        // ============================================================ 状态

        /// <summary>最近一次开出来的那一扇（自检用）。</summary>
        public static MissionRerollPopup LastOpened { get; private set; }

        /// <summary>要重摇的那一行（0 起）。</summary>
        public int Index { get; private set; }
        /// <summary>换完之后的通知（宿主 `MissionsTab.Build`）。</summary>
        System.Action _onRerolled;

        /// <summary>取不到的图（出声用 —— 红线：不许静默失败）。</summary>
        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        // 自检要按名字找的几个节点
        public Transform WindowNode { get; private set; }
        public Transform MsgNode { get; private set; }
        public Transform CancelNode { get; private set; }
        public Transform ConfirmNode { get; private set; }
        public Transform PriceCellNode { get; private set; }
        public Transform IconNode { get; private set; }
        public Transform PriceTextNode { get; private set; }
        /// <summary>最近一次 `Confirm` 换出来的新任务描述（自检用；没换过 = null）。</summary>
        public string RerolledTo { get; private set; }

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }

        protected override void SetupData(object data)
        {
            base.SetupData(data);
            var ctx = data as MissionRerollContext;
            Index = ctx != null ? ctx.Index : 0;
            _onRerolled = ctx != null ? ctx.OnRerolled : null;
        }

        // ---------------------------------------------------------- 开

        public static MissionRerollPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("ReRollPopup Variant");
            var win = go.AddComponent<MissionRerollPopup>();
            win.type = WindowType.Popup;                       // 实证 type = 1
            win.placement = WindowsPlacement.Popup;            // 实证 windowsPlacement = 15
            // 🔴 实证 `closeOnESC = 0` —— ESC **不关**这扇窗（与 `PromptPopup` 那扇同值）。
            // ⚠️ 派单里写的是「ESC ⇒ 关窗」，与 MB 原文冲突 ⇒ **按原版**（文件头 ④）。
            win.closeOnEsc = false;
            win.extraScaleSmallScreen = 1f;                    // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        // ---------------------------------------------------------- 建

        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();

            // ① 压暗整屏（原版 `Menu Dark Background`：**无图**、只有色 (0,0,0,0.773)）
            //    它上面挂着 `BackgroundCloseButton`（原版那个组件就是「点它关窗」）
            var dark = MenuDraw.Node(transform, "Menu Dark Background", DarkR);
            MenuDraw.Rect(dark, CardArt.Solid(), DarkR, "Image", QShade, DarkTint);
            // 🆕 **2026-10-04（A47 接线批）**：改走公共件 `MenuDraw.ShadeHit` —— 它会把「压暗档 < 内容档」
            //    这条不变量现场核一遍（`QShadeHit` = `QShade` = 3080 < `QHit` = 3088）。行为一字未改。
            MenuDraw.ShadeHit(dark, DarkR, QShadeHit, QHit, () => Close(), "CloseHit");

            // ② `Window`（850×430）—— 原版这一件**没有 Image**（底在同名的子节点上）⇒ 只建节点
            WindowNode = MenuDraw.Node(transform, "Window", WindowR);
            var bg = MenuDraw.Node(WindowNode, "Generic Popup Background", WindowR);
            Nine(bg, ArtPopup, WindowR, PopupBorder, "Image", QPanel);
            // 🆕 **2026-10-06（A94）：弹窗面板底图吸收点击**。判据 = 原版 prefab
            //   `ReRollPopup Variant > Window > Generic Popup Background` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）⇒ 原版**什么都不做**。
            MenuDraw.Absorb(transform, "AbsorbHit", WindowR, QShadeHit, QHit);

            // ③ `Mask`（`Image` + `Mask` **`showGraphic = 0`** ⇒ 原版**不渲染它**）+ 它的子件 `Background fill`
            //    ⚠️ 我们的引擎没有 stencil mask ⇒ 只建节点、**不画那张 `40k_popup`**（画了会变成双层边框）；
            //       `Background fill` 的矩形与 `Mask` **完全相等** ⇒ 「裁到 Mask 框内」是恒等操作，视觉一致。
            var mask = MenuDraw.Node(bg, "Mask", MaskR);
            MenuDraw.Tiled(mask, Art(ArtFill), MaskR, FillTilePx, QFill, "Background fill");

            // ④ 文案：fs40 · auto[4~40] · 居中/居中 · **折行 = 1**（原版这条 TMP 自己就是 `MessageText` 节点）
            var msg = MenuDraw.TextBox(WindowNode, MsgR, TxtMessage, Color.white, "MessageText", 40f, 4f, QText);
            if (msg == null) Debug.LogWarning("[MissionReroll] `MessageText` 没建出来（红线：不许静默失败）");
            else MsgNode = msg.transform;

            // ⑤ 两颗钮（原版 `Buttons` 是 HLG sp 0 ⇒ 两个矩形直接照实测摆）
            var btns = MenuDraw.Node(WindowNode, "Buttons", ButtonsR);
            CancelNode = BuildButton(btns, "ButtonLeft", CancelR, CancelTxR, "Cancel", BtnTint, 324f, 76f,
                                     () => Close());
            var priceDisplay = MenuDraw.Node(btns, "Price Display", PriceBtnR);   // 原版这一层是 `PriceDisplayButton`
            ConfirmNode = BuildButton(priceDisplay, "Generic UI Button", PriceBtnR,
                                      new PxRect(ConfirmTextR, 541f, ConfirmTextR, 617f), "Confirm ", BtnTint2,
                                      0f, 0f, Confirm, rightAlign: true);
            BuildPriceCell(ConfirmNode);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[MissionReroll] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            HintGaps();
        }

        /// <summary>一颗钮：底 `40K_button`（**Simple + preserveAspect** · 绿）+ `Button Text`
        /// （fs38 · auto[12~38] · 折行 0）+ 命中区（`EverguildButton` `trans=2` ⇒ **悬停换图**）。
        /// `rightAlign` 时 `tx` 只有右边界有意义（`Confirm` 那一颗的框是**零宽**的，见 `ConfirmTextR`）。</summary>
        Transform BuildButton(Transform parent, string nodeName, PxRect r, PxRect tx, string label, Color tint,
                              float autoBoxW, float autoBoxH, System.Action onClick, bool rightAlign = false)
        {
            var n = MenuDraw.Node(parent, nodeName, r);
            // 底图：`40K_button` 489×107 塞进 350×76 ⇒ 原版 `preserveAspect` ⇒ 画 **347.3×76**（居中）
            var tex = Art(ArtButton);
            ImageQuad q = null;
            if (tex != null) q = MenuDraw.Rect(n, tex, r, "Image", QBtn, tint, true);
            // 命中区：整颗钮；`target` = 底图那块 ⇒ 悬停换 `40K_button_hover`、按下 `40K_button_pressed`
            //（两张图名都是**实读 prefab 的 `m_SpriteState`**，不是我们按 `_hover` 规律猜的 —— 恰好同值）
            MenuDraw.Hit(n, "Hit", r, QHit, onClick, q, ArtButton);

            var lb = MenuDraw.Text(n, tx, label, Color.white, "Button Text", 38f, QBtnText);
            if (lb != null)
            {
                if (rightAlign) MenuDraw.AlignRight(lb, tx);
                else if (autoBoxW > 0f && autoBoxH > 0f)
                    // 🔴 **2026-10-11（A305①）**：第 5 个实参 = 原版 `Button Text` 的 `m_fontSizeBase` **原文**。
                    //    判据（本轮自己扫 `bundle_menus_assets_all`）：`auto[12~38]` 那一族共 105 颗，
                    //    **99 颗 `m_fontSizeBase 12.0`**、另 6 颗是 `36.0` —— 但那 6 颗是**名字/奖杯字段**
                    //    （`"Player's name"` / `'Trophy Name '` / `'Marneus Calgar'`，`m_fontSize 38`、
                    //    对齐 4/2/1 各不相同），**不是按钮文案** ⇒ 本钮取 **12**（= 逐站表 §二·3 #35 那族的多数值）。
                    //    ⚠️ 这一族 `m_fontSize = 38` 而 base = 12 —— base **≠ 标称**，所以必须显式传。
                    lb.SetAutoFitBox(LayoutSpace.Px(autoBoxW), LayoutSpace.Px(autoBoxH), 12f, 38f, 12f);
            }
            return n;
        }

        /// <summary>价钱格（内层 `Price Display`）：`icon` + `text`（左边缘 **1232.80**）。
        /// 🔴 **`icon` 只有节点、不画图**（原版出厂 `m_Sprite` 空、运行期按货币赋图 —— 见文件头 ③）。
        /// ⚠️ 两个矩形的 x 都按 2026-10-05（A59）的 W3 真值（`1160.10` / `1232.80`），见 `IconR`/`PriceTextR` 的注释。</summary>
        void BuildPriceCell(Transform parent)
        {
            PriceCellNode = MenuDraw.Node(parent, "Price Display", PriceCellR);
            // 🆕 A36-④（2026-10-04）：原来这里注释写「画出来是 67.2²」、代码却只建了个 **56² 的节点且没有施加
            //   `scl 1.2`** —— **注释与实现不符**（审查发现）。现在两边统一：节点直接摆**烘过 1.2 的那个矩形**
            //   （67.2²、绕原版 56² 那个中心）⇒ 将来判据齐了、往这个节点上画图，尺寸/位置**一步到位**。
            //   ⚠️ **不给节点设 `localScale`**（虽然 `CampaignTab` 有那种写法）：`MenuDraw.Local` 用的是世界坐标差，
            //   父一带 scale 会被再乘一次（`CampaignRewardWindow` 那条「缩放要烘进矩形」的坑）。
            IconNode = MenuDraw.Node(PriceCellNode, "icon", IconR);
            // ⚠️ 原版这一格是 `BlinkGraphic` 会呼吸的那一枚货币图标 —— 我们**没有那张图**
            //    （`MissionData.get_RerollPrice` 读不到、货币类型也读不到）⇒ 留空 + 出声。
            var pl = MenuDraw.Text(PriceCellNode, PriceTextR, PricePlaceholder, Color.white, "text", 40f, QPrice);
            if (pl == null) Debug.LogWarning("[MissionReroll] 价钱那格的字没建出来（红线：不许静默失败）");
            else { PriceTextNode = pl.transform; MenuDraw.AlignLeft(pl, PriceTextR); }
        }

        // ---------------------------------------------------------- 行为

        /// <summary>关窗。🔴 **覆写：照原版 `MissionReRollPopup__Close.c` —— 先把页签换回这一页，再 `base.Close()`**。
        /// 原版那两句是：`WindowsManager.Instance.GetOpenWindow<带 Missions 页的那扇窗>()` → 非空就
        /// `GameWindowWithTabs.ChangeTab<MissionsTab>(win)`（反编译指令流：`GetOpenWindow` → `op_Implicit` 判非空
        /// → `ChangeTab`）→ 然后才 `GameWindow.Close`。
        /// ⚠️ **正常点击路径里这一步是恒等操作**（弹窗那层压暗把自己的命中区压在页之上，鼠标换不了页签），
        /// 所以它是「照原版补上、平时看不出差别」的那一类 —— 自检用**程序化换页**把它逼出来
        /// （`Editor/RewardsScene.cs`：切到 Campaign 再关窗 ⇒ 页必须自己回来），不然这条分支没人守。
        /// 🔴 走的是 `TabButtons.Click(0)`（= 左栏第 1 键 Missions）而不是直接 `ChangeTab` ——
        /// `RewardsWindow.Open()` 已经定了这条规矩：「**点一下**才是唯一的状态入口」，
        /// 直接改 `ChangeTab` 会把左栏高亮留在原来的键上（两处写同一件事 = 迟早不一致）。</summary>
        public override void Close()
        {
            var tabs = FindOpenTabbedWindow();
            if (tabs != null)
            {
                if (tabs.tabButtons != null) tabs.tabButtons.Click(0);
                else tabs.ChangeTab(WindowTabType.Missions);
                Debug.Log("[MissionReroll] 关窗前把 `" + tabs.name + "` 的页签换回 `Missions`"
                          + "（照原版 `MissionReRollPopup__Close.c`；⚠️ 平时是恒等操作，只有程序化换过页才看得出）");
            }
            base.Close();
        }

        /// <summary>当前开着的那个**带 Missions 页**的窗（原版 `WindowsManager.GetOpenWindow<带页签的窗>()` 的等价物）。
        /// ⚠️ 本工程 `WindowsManager` 没有那个泛型口（那个文件不归本件改）⇒ 自己从 **`openWindows`** 里挑
        /// （`public readonly List<GameWindow>`）。
        /// 🔴 判「是不是那一扇」的口径 = **它的 `tabs` 里真有 `Missions` 这一页** —— 不能只按「是
        /// `GameWindowWithTabs`」挑：商店窗 / 收藏窗 / 档案窗 / 社交窗**也都是**带页签的窗，
        /// 挑错了会把**别扇窗**的页签切到它自己的第 1 页（原版 `GetOpenWindow<T>()` 点名的也是**一扇具体的窗**）。
        /// 🔴 先看 `currentWindow` —— 弹窗开时它被 `ToBackground()`（**仍在表里**），是首选。</summary>
        GameWindowWithTabs FindOpenTabbedWindow()
        {
            var mgr = Manager;
            if (mgr == null) return null;
            var cur = mgr.currentWindow as GameWindowWithTabs;
            if (IsMissionsHost(cur)) return cur;
            for (int i = 0; i < mgr.openWindows.Count; i++)
            {
                var w = mgr.openWindows[i] as GameWindowWithTabs;
                if (IsMissionsHost(w)) return w;
            }
            return null;
        }

        /// <summary>这扇窗是不是「有 Missions 那一页」的窗（见 `FindOpenTabbedWindow` 的注释）。</summary>
        static bool IsMissionsHost(GameWindowWithTabs w)
        {
            if (w == null || w.CurrentState == WindowState.Closed) return false;
            for (int i = 0; i < w.tabs.Count; i++)
                if (w.tabs[i] != null && w.tabs[i].Type == WindowTabType.Missions) return true;
            return false;
        }

        /// <summary>`Confirm`（原版 `acceptButton` → `RerollChallenge`）。**换一条新任务、关窗**，
        /// 然后通知宿主重建整页。⚠️ **不扣任何资源**（用户 2026-09-17 边界②）。</summary>
        void Confirm()
        {
            RerolledTo = DailyData.RerollDaily(Index);
            Close();                       // 先关再回调（照 `PromptPopup.Choose`）—— 回调里可能再开窗
            Debug.Log("[MissionReroll] `Confirm` ⇒ 换了第 " + (Index + 1) + " 条任务：「" + RerolledTo
                      + "」（⚠️ **不扣钱**：用户 2026-09-17 拍板「不做真实经济、资源固定 9999」；"
                      + "原版这一步会扣 `RerollPrice` 并走服务端）");
            if (_onRerolled != null) _onRerolled();
        }

        GameObject Nine(Transform p, string art, PxRect r, Vector4 b, string n, int q, Color? tint = null)
        {
            var t = Art(art);
            return t == null ? null : MenuDraw.Nine(p, t, r, b, PopupTexW, PopupTexH, q, tint, true, n);
        }

        /// <summary>把「原版有、我们没做 / 读不到」的几处**当场出声**（红线：不许静默失败）。</summary>
        void HintGaps()
        {
            Debug.Log("[MissionReroll] 四件事如实标注：① `icon`（价钱左边那枚货币图标）**只建节点、不画图** —— "
                      + "原版出厂 `m_Sprite` 空、运行期按 `RerollPrice` 的货币赋（`MissionData.get_RerollPrice` "
                      + "在本地反编译里是**错桩** ⇒ 价钱与货币都读不到）；② 价钱 `" + PricePlaceholder
                      + "` 是 **prefab 出厂占位**、**不是真价钱**；③ 点 `Confirm` **不扣钱**"
                      + "（用户 2026-09-17 边界②：「不做真实经济」）；④ `Mask` 那一件原版 `showGraphic = 0` "
                      + "**不渲染** ⇒ 我们只建节点、不画那张 `40k_popup`（画了会变双层边框）。");
        }

        public string Dump()
        {
            return "MissionReroll[行 " + (Index + 1) + "] 窗 " + WindowR.W + "×" + WindowR.H
                   + " · 换到「" + (RerolledTo ?? "（还没换）") + "」 · 取不到的图 " + MissingArt.Count + " 张";
        }
    }
}
