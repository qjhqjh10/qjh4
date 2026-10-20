// ToastNotifications.cs — 外壳侧 `Toast Notification Controller` 整族（任务 / 等级 / 经验那一类飘窗）。
//
// ============================ 这是什么（以及【不是】什么） ============================
// 原版主菜单里有两个**不同的**「飘一句话」系统，⛔ 别混：
//   ① `UI Error Message Controller (MUST BE ENABLED)` = `UIMessageController`
//      —— 屏幕上方那条**错误 / 分享横幅**（5 条一池、轮转）。本仓的等效物 =
//      `Shell/MessageToast.cs` + `Battle/ErrorMessageBanner.cs`。**本文件不碰它、也不复用它。**
//   ② `Toast Notification Controller` = **本文件** —— 任务进度 / 收藏等级 / 收藏经验 / 成就那一族
//      **带图标的飘窗**。`资料/普查产出_第八会话/E7_弹窗星号与toast.md` §⑤·1 早就把这条账挂着
//      （「前者仍是**一笔没开的账**」）；本文件就是那一笔。
//
// ============================ 判据（逐句实读，不是看名字猜） ============================
// ① **场景里的节点**（`bundle_scenes_scenes_mainmenuwarpforge`）：
//    `Safe area Only Horizontal` 的第 11 个子件 = **`Toast Notification Controller`**（RT 1096，
//    a(0,0)-(1,1) sd(0,0)）→ 唯一子件 **`Holder`**（RT 1100，a(1,0)-(1,0) p(1,0)
//    apos(-21.4228515625,0) sd(634.4879760742188, 752.614013671875)）。
//    控制器自己挂 **`Canvas`（`m_SortingOrder = 900` · `m_OverrideSorting = true`）**
//    —— 比 `Tutorial highlight`(601) / `UI Error Message Controller`(1) / `ToolTipManager`(0) 都高
//    ⇒ **这是主菜单里最上面的一层**（`GameObject/Toast Notification Controller.json` +
//    `MonoBehaviour_1939.json` + `Canvas_1074.json`）。
//    `MonoBehaviour_1939` 的字段（= `ToastNotificationController`，`dump.cs:111472`）：
//      `notificationTypes` 四条 = `{0 → 491} {1 → 492} {2 → 490} {3 → 488}` ·
//      `holder = 1100` · `defaultToastNotification = 491`。
//    把四个 prefab 根对上（`RT 343=Toast` / `RT 344=Toast Collection Get XP` /
//    `RT 342=Toast Collection Level Up` / `RT 341=Mission Toast Notification`；它们在本包里的
//    组件正是 `MB 490/491/492/488`）⇒
//      **0 Achievement → `Toast`** · **1 CollectionGainXP → `Toast Collection Get XP`** ·
//      **2 CollectionLevelUp → `Toast Collection Level Up`** · **3 MissionProgress → `Mission Toast Notification`**，
//      **默认 = `Toast`**。枚举名逐字来自 `dump.cs:111548`（`ToastNotificationType`）。
// ② **6 个 prefab 根**（`m_Father = 0`，即独立 prefab 资产；`RT 341/342/343/344/351/359`）：
//      `Mission Toast Notification` 650.1010131835938×139.71299743652344 ·
//      `Toast Collection Level Up` 536.7548828125×97.9000015258789 ·
//      `Toast` 466.1357116699219×**0**（高由 `VerticalLayoutGroup`+`ContentSizeFitter` 运行时算）·
//      `Toast Collection Get XP` 536.7548828125×97.9000015258789 ·
//      `Collector Level Tooltip` 500×100（a/p 都是 (.5,.5)，`CanvasGroup` interactable=false）·
//      `Resource Counter Item` 0×38.066001892089844（a(0,0) p(.5,.5) apos(22.2996826171875,0)）。
//    🔴 **2026-10-17（`N21②③`）**：后两份**本文件不建**（没有生产路径，见下面「模板 5 / 6」）——
//      这里保留它们的参数**当知识**（原版 prefab 是真的存在），⛔ 别当成「我们还没做」的待办。
//    逐颗的「层 × 参数」表在构建代码里逐条标了（字段名 + 数值都写在注释里）。
// ③ **`ShowNotification` / `ScheduleNotification` 的原文**（`decomp_full/`）：
//    · `ToastNotificationController__ShowNotification.c`：`SingletonBehaviour.Instance`
//      非空 ⇒ `TryShowNotification(context)`；空 ⇒ **抛异常**（`FUN_1803f47a0`）。
//    · `__TryShowNotification.c`：`context.ControllerID` 非空且**注册过 handler** ⇒
//      交 handler（`RegisterNotificationHandler`，`__RegisterNotificationHandler.c` 两句）；
//      否则 ⇒ `ShowNotificationInternal`。
//    · `__ScheduleNotification.c`：把「`TryShowNotification`」包成 delegate 交给
//      `MenuActions.QueueAction` —— **推迟一拍**（不是立刻弹）。本文件用 `_scheduled` 队列等效，
//      在 `Step`/`Update` 的**开头**排空。
//    · `__ShowNotificationInternal.c` **逐句**：
//        ① `Instantiate(GetToastNotificationPrefab(type), holder)`；
//        ② `newNotification.Initialize(context)`；
//        ③ `rectTransform.ForceUpdateRectTransforms()` + `layoutGroupContentFixer.FixLayout()` +
//           `LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform)`（先把布局跑完，才量得到高）；
//        ④ `float h = rectTransform.rect.height`；
//        ⑤ `anchoredPosition = (0, -h)`（**从容器底边下方入场**）；
//        ⑥ `index = freeSpot; freeSpot++`；
//        ⑦ `DOTween.Sequence()`
//             `.Append(DOAnchorPosY(index*h + 10, 1.0s).SetEase(0x1b))`
//             `.AppendInterval(3.0s)`
//             `.Append(DOAnchorPosY(-h, 1.0s).SetEase(0x1a))`
//             `.AppendCallback(销毁 + freeSpot = min(index, freeSpot))`；
//        ⑧ 常量（`工具/read_literal.py` 从 `GameAssembly.dll` 读的 4 字节 float）：
//           `DAT_1834b2bb8 = 1.0`（每一段位移）· `DAT_1834b2da8 = 10.0`（第一个格位的底座偏移）·
//           `DAT_1834b2e8c = 3.0`（停留）· `DAT_1834b2c80 = 0x80000000`（**符号位翻转** ⇒ `-h`）。
//           `SetEase(0x1b)` / `SetEase(0x1a)` 按 DOTween `Ease` 枚举 = **27 OutBack** / **26 InBack**。
//    · `ToastNotification__Initialize.c`：`context.Text` → `text.text`；
//      `context.Icon` → `icon.sprite`；`button.onClick` 加监听 → `context.OnClick`；
//      最后 `gameObject.SetActive(true)`。
//    · `ToastNotificationMission__Initialize.c`：它不用 `context.Text` —— 文案来自
//      `MissionChallengeProgress.GetLocalizedDescription()` + `SupportMethods.ModifyLocalization`，
//      进度来自 `progress`（派给 `MissionInfoDisplay.Initialize`）；`m_IsActive` 由它自己开。
// ④ **层级 / 队列口径**：原版靠 Canvas 的 `SortingOrder = 900`（+ 排序层 `PopUps`），
//    本仓外壳是**世界空间 quad + 显式渲染队列**（`MainMenuRuntime` 的注释：
//    「档位号本身是【我们自己起的梯子】」）。⇒ 本文件取**加载黑场之下、shell 之上**那一档
//    （现网 = 加载黑场 `LoadingMenu.QPanel 4090` · 转圈件 `LoadingController.QDark/QSkull/QCog 3327/3328/3329`
//    （🔴 **2026-10-20（W58·D4）起**；原版它在 `Default` 层 ⇒ 在 toast 之下，判据 →
//    `资料/普查产出_1020/W58_界面线_联盟搜索形态判据.md` §一·4）· `BlockingOverlay 3338/3339`
//    （降级件，无运行时调用点；🔴 **2026-10-20（W58）**：由 `4070/4071` 降下来，判据 →
//    `资料/普查产出_1020/W58_界面线_BlockingOverlay收尾.md`）·
//    shell 最高 `4030`）—— 具体值见下面 `QShade` 那一段。
//    🔴🔴 **2026-10-17（本批 · `N21①`）：判据【已定】，而且它推翻了旧账【和】R 报告的两句话。**
//    **原版实读（`globalgamemanagers/TagManager/TagManager_3.json` + 两个 Canvas JSON）**：
//      · 排序层次序 = `Default(0) → Background → Elements → OverElements → Hand → HandMoving
//        → OverHand → **PopUps(1054366423)** → **Blocking(2286135553)**`；
//      · `Toast Notification Controller` 的 Canvas（`Canvas_1074.json`）= 层 **PopUps** · order **900**；
//      · `LoadingMenu` 的 Canvas（`Canvas_1081.json`）= 层 **Blocking** · order **0**
//        （`m_SortingLayerID = -2008831743` 按 int32 还原 = **2286135553** = `Blocking`）。
//    ⇒ **排序层先于 order 比较** ⇒ **原版是「加载黑场在 toast 之上」**。
//    · 旧账「黑场在 toast 之上」**是对的**；
//    · R 报告 `R_界面线剩余条目现核.md` §N21① 只比了 `m_SortingOrder`（900 > 0）、**漏了 `m_SortingLayerID`**
//      ⇒ 它那句「Toast 在黑场之上 / 我们已经同序」**不成立**（错因 = 拿两个**不同排序层**的 order 直接比）。
//    ⇒ 我们的值**跟着改**：原来 5000（压过加载黑场）是**反的**，现在落在 **4072–4076**：
//      **高过**全部 shell UI（`4030`）与转圈件（`LoadingController.QDark/QSkull/QCog = 3327/3328/3329` ——
//      原版转圈件在 `Default` 层、本来就该被 toast 盖住），**低过** tooltip（`4078`）与加载黑场（`4090`）。
//      （🔴 **2026-10-20（W58·D4）**：转圈件由 4080–4082 降到 3327–3329，本段原写「在 BlockingOverlay 4071 与
//      加载黑场 4080 之间」**已过期** —— 那两个数现在都不是转圈件的档，就地订正，铁律 5。）
//      （🔴 **2026-10-20（W58·TOASTCOMMENT）**：上句引文里的 `BlockingOverlay 4071` 这个数**也已过期** ——
//      它已由 `4070/4071` 降到 **`3338/3339`**，判据 → `资料/普查产出_1020/W58_界面线_BlockingOverlay收尾.md`；
//      引文照旧保留，别据此再推转圈件的档。）⛔ 别再按「5000 起」去推。
// ⑤ **hover 色偏（W58 · D6，2026-10-20）**：`Toast` / `Level Up` / `Get XP` 的根 `EverguildButton` 是
//    `m_Transition = 1 (ColorTint)`、`m_Colors` = 全库默认组（Highlighted `0.9608…` / Pressed `0.7843…` /
//    `m_FadeDuration 0.1`；`MonoBehaviour_504/508/513.json`）⇒ `ShowInternal` 里用
//    `WindowButton.TintOn(Background 的九宫格)` 接上去（= 原版 `m_TargetGraphic`）。
//    **`Mission` 不接**：`MB_520` 的 `m_Transition = 0 (None)` ⇒ 原版悬停不变。
// ⑥ **生命周期（W58 · D5，2026-10-20）**：原版控制器只在**主菜单场景**（13 个战场 0 处）⇒ 切场景即整棵销毁；
//    我们挂在 DDOL 壳上 ⇒ `ShellRuntime` 在切场景时调 `DisposeForSceneChange()`（清在飞 + 销毁控制器），
//    回主菜单由 `MainMenuRuntime.Build` 重建。判据与调用点 → `DisposeForSceneChange` 上方那一段。
//
// ============================ 🔴 数据源现状（如实出声，⛔ 不编数据） ============================
// · **任务进度**：有本地数据源 —— `Shell/DailyData.cs`（`DailyDesc` / `DailyCounter` /
//   `DailyProgress01` / `DailyRewardArt` / `DailyRewardText`）⇒ `ShowMission(index)` 走它。
// · **收藏经验 / 收藏等级 / 成就**：**本地没有数据源**（原版由服务端下发；`CollectorLevelData` /
//   `CollectorLevelChangeSignal` 只有类型、没有本地实例）。⇒ `ShowCollectionGainXP(amount)` /
//   `ShowCollectionLevelUp(level)` 两件**只听声、不弹**（`Debug.LogWarning` + 返回 false）；
//   要弹只能走通用 `ShowNotification(Context)` 并**由调用方提供真实文案**。
// · **成就**：原版 `Toast` 的文案是词条（`Congratulations! You've earned an achievement.`），
//   本地没有「哪个成就、什么时候给」的数据 ⇒ 不主动弹；调用方有真实成就事件时走通用口。
//
// ============================ 与消息横幅的分工（⛔ 别合并） ============================
// `Shell/MessageToast.cs` = `UIMessageController`（分享 / 错误那一族，条状横幅，屏幕上方）；
// **本文件** = `ToastNotificationController`（带图标的卡片，右下角堆叠）。
// 两者在原版是**两个类、两个节点、两套时序** —— 合并 = 两处写同一条规则。
//
// ============================ 本文件实现的边界（如实标注） ============================
// ⚠️ **1. 两条「高度运行时算」的模板**（`Toast` / `Toast Collection Get XP`）—— **`N23` 已改**：
//     原版靠 `VerticalLayoutGroup + ContentSizeFitter` 在重建那几拍算高度；本仓外壳**没有 uGUI 布局**
//     （世界空间 quad）。旧实现取的是**静态推导的常量根高**（`Toast = 12 + 89.8638 + 0 + 7 = 108.8638` ·
//     `Get XP = 0 + 86.7019 + 0 + 0 = 86.7019`，`m_ChildControlHeight = 0` ⇒ 取子件 `sizeDelta.y`，
//     两个 Text 的 `sizeDelta.y` 都是 0）。
//     🔴 **2026-10-17（`N23`）静态判定：那两个常量【只对第一轮重建成立】，不是原版运行值：**
//       `LayoutRebuilder.Rebuild` 一轮 = 计算→控制×2（`Library/PackageCache/com.unity.ugui@…/
//       Runtime/UGUI/UI/Core/Layout/LayoutRebuilder.cs:76-91`），而原版 `ShowNotificationInternal`
//       先跑 `FixLayout()`（Toast 根那颗 fixer 的 `layouts = [pid 204]`，= 根自己的 VLG，
//       `MonoBehaviour/MonoBehaviour_509.json`）再 `ForceRebuildLayoutImmediate(rectTransform)` ⇒ 共三轮；
//       Text 上挂的 `ContentSizeFitterMinMax`（`MB 538`）在第一轮就把 `sizeDelta.y` 写成**首选文本高**
//       ⇒ 第二/三轮 VLG（`m_ChildControlHeight = 0`）读到的是**子件 rect 高** ⇒
//       **根高 = padT + iconH + 文本高 + padB**，**随文案变**（`Toast ▸ Text` 折行 = 1）。
//     ⇒ 现在的实现：`ShowInternal` 里 `Fill` 之后按**本次文案现量**文本首选高、重算根高并重摆
//       （`ApplyMeasuredHeight`）；`BuildToast` / `BuildGetXP` 里那两个常量**降级成兜底**
//       （未激活 / 量不出时用，并**出声**）。判据/算式只在 `ToastRootHeight` 一处。
//     ⚠️ 仍未做：**原版真机读数**（本仓没有原版 `rect.height` 的实拍）—— 现量的口径是**uGUI 字段语义**
//       （padding + 子件高），不是从原版画面反推的。
// ⚠️ **2. `WF_UI_Generic_FancyNotification_Background` 本地 `Resources/` 里没有**
//     （只在源图 `CardPresentation/Art/原版/0_mainmenu/`，资源导入器没导）。
//     ⇒ 本文件**退回** `UI_Deck_Information_submenu_Back_opaque`（= 通用 `Toast` 那张底图，
//     同族、已导入）并**出声一次**。⛔ 没有把「缺图」说成「原版也这样」。
// ⚠️ **3. Mission toast 的运行时装配（`N20②`）—— 能静态补的已补，补不了的如实标：**
//     `ToastNotificationMission__Initialize.c` 逐句：`SetActive(true)` → `text = ModifyLocalization(
//     GetLocalizedDescription(progress))` → 对三个 `MissionInfoDisplay` 各跑一次 `Initialize(progress)`。
//     那三个的 prefab 字段现读（`MonoBehaviour/{499,500,501}.json`）**全是 `displayRule = 1`**
//     ⇒ `MissionInfoDisplay__Initialize` 的位运算 = **「只在未完成时显示」**（完成 ⇒ 三个 GameObject 全关）。
//     已按这条接进 `Fill`（见那一节的注释）。**补不了的**（数据源在服务端/运行时）：
//       · `MissionRewardsDisplay__Setup` 的 `MissionChallengeProgress.AvailableRewards()`（逐条奖励 → 实例化
//         `MissionRewardItem`）+ `LiveOpsManager` 的 `ApplyExtraRewards`；
//       · `MissionMilestonesDisplay__Setup` 的逐里程碑 `MissionMilestoneStep` 实例化（阈值来自同一份运行时奖励表）；
//       · `MissionCounterDisplay__Setup` 里 `ArmyUtilities.GetArmyIcon(armyId)` 那一支（`armyIcon` 引用在本 prefab
//         上是 null ⇒ 原版这条也不走）。
//     本文件按 prefab 的静态结构 + 字段（`drawerHolder` / `amountText` 等）把**可见层**建出来，
//     奖励图标用 `ItemDrawer.Draw(holder, item, amount, 10)` 的等效摆法（`MissionRewardItem__Setup.c`
//     第 3 句）画进 `drawerHolder`。逐项标注在各 builder 里。
// ⚠️ **4. 模板不是 `Prefab` 资产**：原版这 6 个是独立 prefab（`m_Father = 0`），
//     本仓没有从 bundle 搬 prefab 的通道 ⇒ 建在 `Templates`（**出厂 inactive**）下，
//     `Show` 时 `Instantiate` 到 `Holder`。名字**逐字照原版 prefab 根**。
//     🔴 2026-10-17（`N21②③`）：其中 2 个（`Collector Level Tooltip` / `Resource Counter Item`）**不建**（死模板）。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>外壳侧 `Toast Notification Controller` 的等效实现。判据/边界 → 文件头。</summary>
    public class ToastNotifications : MonoBehaviour
    {
        // ============================================================ 原版枚举（`dump.cs:111548`）
        public enum ToastNotificationType
        {
            Achievement = 0,
            CollectionGainXP = 1,
            CollectionLevelUp = 2,
            MissionProgress = 3,
        }

        /// <summary>原版 `ToastNotificationContext`（`dump.cs:111345`）的等效物。
        /// 字段名/语义逐条对应：`Text` · `IconArt`(= `Sprite Icon`，本仓用图名) ·
        /// `OnClick` · `ControllerId`(= `ControllerID`) · `Type`(= `NotificationType`)；
        /// 后四个是 `MissionProgress` 那一档的 `Data`（原版 `ToastNotificationMissionsData`）。</summary>
        public struct Context
        {
            public ToastNotificationType Type;
            public string Text;
            public string IconArt;
            public Action OnClick;
            public string ControllerId;
            /// <summary>文案是**词条键**时置真（原版这条链**不**查表 —— 只有任务那条走
            /// `GetLocalizedDescription`；本项是我们给调用方的方便口，默认 false = 原版行为）。</summary>
            public bool Localize;

            // ---- 仅 `MissionProgress` 用（原版 `ToastNotificationMissionsData`）----
            public bool ShowProgress;
            public float Progress01;
            public string ProgressText;      // 原版 `MissionCounterDisplay` 的 `{0}/{1}`
            public string RewardArt;         // 原版 `ItemDrawer.Draw` 的 item 图标
            public string RewardText;        // 原版 `amountText`

            public Context(string text, ToastNotificationType type = ToastNotificationType.Achievement,
                           string iconArt = null, Action onClick = null, string controllerId = null)
            {
                Type = type; Text = text; IconArt = iconArt; OnClick = onClick;
                ControllerId = controllerId; Localize = false;
                ShowProgress = false; Progress01 = 0f; ProgressText = null;
                RewardArt = null; RewardText = null;
            }
        }

        /// <summary>原版 `ToastNotificationHandler`（`dump.cs:111558`）。</summary>
        public interface IHandler { void ShowNotification(Context context); }

        // ============================================================ 原版节点名（⛔ 别改名）
        public const string NodeName = "Toast Notification Controller";
        public const string HolderNodeName = "Holder";
        public const string ParentNodeName = "Safe area Only Horizontal";

        // 6 个 prefab 根（`RT 341/342/343/344/351/359`）
        public const string MissionPrefabName = "Mission Toast Notification";
        public const string LevelUpPrefabName = "Toast Collection Level Up";
        public const string ToastPrefabName = "Toast";
        public const string GetXPPrefabName = "Toast Collection Get XP";
        // ⚠️ `Collector Level Tooltip` / `Resource Counter Item` 这两份原版 prefab **本文件不建**（`N21②③`）——
        //    原版 `notificationTypes` 四条**只指向四个 toast**，这两份没有任何生产路径 ⇒ 建了也是死树。
        //    名字/矩形留在文件头 ② 当知识，⛔ 别把它们的常量加回来当「预留」。
        /// <summary>6 个 prefab 的容器节点 —— **我们加的**（原版是 6 份独立 prefab 资产）。
        /// 出厂 inactive；`Show` 时从它下面 `Instantiate` 到 `Holder`。</summary>
        public const string TemplatesNodeName = "Templates";

        // ============================================================ 渲染队列（⚠️ 我们挑的梯子）
        // 原版这一层 = 排序层 `PopUps` + Canvas `SortingOrder 900`（**低于**加载黑场的 `Blocking` 层）。
        // 本仓外壳是显式渲染队列：shell 最高 `4030`、`BlockingOverlay` 降级遮罩 `3338/3339`（降级件，无运行时调用点；
        // 🔴 2026-10-20（W58）：由 `4070/4071` 降下来，判据 → `资料/普查产出_1020/W58_界面线_BlockingOverlay收尾.md`）、
        // **转圈件 `LoadingController.QDark/QSkull/QCog = 3327/3328/3329`（🔴 2026-10-20（W58·D4）起；原版它在
        // `Default` 层、在 toast 之下）+ 加载黑场 `LoadingMenu.QPanel 4090`** ⇒
        // 这一族取 **4072–4076**：**高过全部 shell UI 与转圈件，低于 tooltip（4078）与加载黑场（4090）**
        // （= 原版「toast 属于 PopUps、盖住 Default 层的转圈件、被 Blocking 层的 tooltip/黑场盖住」那一条）。
        // 🔴 2026-10-17（`N21①`）：原来是 5000–5004（压过加载黑场）——**和原版反了**，判据见文件头 ④。
        public const int QShade = 4072, QBg = 4073, QArt = 4074, QText = 4075, QHit = 4076;

        // ============================================================ 时序（判据 → 文件头 ③·⑧）
        public const float MoveSeconds = 1.0f;
        public const float HoldSeconds = 3.0f;
        public const float FirstSlotOffsetPx = 10.0f;

        // ============================================================ 设计常量（解包实读）
        const float ScreenW = 1920f, ScreenH = 1080f;
        // `Safe area Only Horizontal` 在主菜单里是整屏 stretch、ap=(0,0)（`主菜单_原版规格.md:64`）⇒ 原点即左上角
        const float HolderW = 634.4879760742188f;
        const float HolderH = 752.614013671875f;
        const float HolderAposX = -21.4228515625f;
        // 🔴 `HolderAposX = -21.4228515625`（`RT 1100.m_AnchoredPosition.x`）·锚点是 `(1,0)`（父右缘）
        //    ⇒ **轴心点 = 1920 + (−21.4228515625) = 1898.5771484375**。
        //    ⚠️ 2026-10-17（RV·H1）就地订正：原来写 `-` ⇒ 1941.4228（整族右移 42.85px、右缘出屏）。
        static readonly float HolderRight = ScreenW + HolderAposX;   // 1898.5771484375
        static readonly float HolderBottom = ScreenH;                // 1080

        // ---- prefab 根尺寸（`RT 341/342/343/344/351/359` 的 `m_SizeDelta`）----
        const float MissionW = 650.1010131835938f, MissionH = 139.71299743652344f;
        const float LevelUpW = 536.7548828125f, LevelUpH = 97.9000015258789f;
        const float ToastW = 466.1357116699219f;
        const float GetXPW = 536.7548828125f;                        // 出厂 sd.y = 97.9，但 VLG+CSF 会重算
        // ⚠️ 原版 `Collector Level Tooltip`（500×100）与 `Resource Counter Item`（0×38.066）的常量
        //    随那两条死模板一起删了（`N21②③`）—— 取值见文件头 ②。

        // ---- `Toast` 的 VerticalLayoutGroup（`MB 204`）----
        const float ToastPadL = 83f, ToastPadT = 12f, ToastPadB = 7f;
        // ---- `Toast Collection Get XP` 的 VerticalLayoutGroup（`MB 205`）----
        const float GetXPPadL = 83f, GetXPPadT = 0f, GetXPPadB = 0f;
        // ---- 两颗子件的 sizeDelta（`RT 355/356` · `RT 358/357`）----
        const float ToastIconW = 89.8613f, ToastIconH = 89.8638f, ToastTextW = 362.057f;
        const float GetXPIconW = 86.6998f, GetXPIconH = 86.7019f, GetXPTextW = 435.619f;

        // ============================================================ 状态
        public static ToastNotifications Instance { get; private set; }

        static readonly Dictionary<string, IHandler> _handlers = new Dictionary<string, IHandler>();
        static bool _warnedFancyBg;

        Transform _holder;
        Transform _templates;
        readonly GameObject[] _templateByType = new GameObject[4];
        readonly List<Live> _live = new List<Live>();
        readonly Queue<Context> _scheduled = new Queue<Context>();
        int _freeSpot;

        sealed class Live
        {
            public GameObject Go;
            public float HeightPx;
            public int Slot;
            public float T;
            public float TargetPx;
        }

        // ============================================================ 自检口（只读；⛔ 生产不碰）
        public bool Built { get { return _holder != null && _templates != null && _templateByType[0] != null; } }
        public Transform Holder { get { return _holder; } }
        public Transform Templates { get { return _templates; } }
        public int LiveCount { get { return _live.Count; } }
        public int ScheduledCount { get { return _scheduled.Count; } }
        public int FreeSpot { get { return _freeSpot; } }
        public int ShowCount { get; private set; }
        public ToastNotificationType LastShownType { get; private set; }
        public string LastShownText { get; private set; }

        /// <summary>某一档的模板根（自检按它找；没建过 ⇒ null）。</summary>
        public GameObject TemplateOf(ToastNotificationType type)
        {
            int i = (int)type;
            return i >= 0 && i < 4 ? _templateByType[i] : null;
        }

        // ============================================================ 建件

        /// <summary>取（没有就建）壳里那一颗。**幂等**。
        /// 父节点：`root`（或壳根）下的 `Safe area Only Horizontal`（原版 `RT 1096.m_Father = 1540`）；
        /// **找不到也出声**（见 <see cref="Build"/>）—— 实况是 `MainMenuScene` 建的 `_root` 下**没有**
        /// 那个节点（它是 `ShellRuntime` 在壳根下建的）⇒ 会落到 `root` 本身。</summary>
        public static ToastNotifications Ensure(Transform root = null)
        {
            if (IsAlive(Instance)) return Instance;
            return Build(root);
        }

        /// <summary>`Instance` 还活着吗（Unity 的假 null 覆盖「已销毁」）。
        /// ⚠️ 「已标记销毁、本帧还没销毁」的实例这里**判不出来** ⇒ 幂等判据不能只靠它，
        /// 见 <see cref="Build"/> 的注释（RV·M2）。</summary>
        static bool IsAlive(ToastNotifications t) { return t != null && t.gameObject != null; }

        /// <summary>建 `Toast Notification Controller → Holder` + 6 个 prefab 根等效物。
        /// 生产入口 = `MainMenuRuntime.Build` 里那一句（见本批报告「主对话要插的那一行」）。
        /// <para>🔴 **2026-10-17（RV·M2）本方法是【显式重建】、不是幂等**：`MainMenuRuntime.Build` 用
        /// `DestroySafe(_root 的子件)`（Play 下是**延迟** `Destroy`），旧控制器在**本帧内仍然非 null**
        /// ⇒ 只判 `Instance != null` 会拿回一颗「已标记销毁」的实例、帧末随它一起消失。
        /// 判据（为什么选「先拆再建」而不是 `IsChildOf(host)`）：主菜单重建时 `_root = transform` 不变，
        /// 旧控制器**仍然是 `_root` 的子件** ⇒ 后代关系判不出「已排队销毁」；而 `Build` 本来就是
        /// 「建场」这一个时刻调一次 ⇒ 显式重建是唯一可判、且与 `DestroySafe` 语义一致的做法。</para></summary>
        public static ToastNotifications Build(Transform root)
        {
            // ---- 旧实例先退场（可能是上一次菜单建的、已排队销毁）----
            if (Instance != null)
            {
                var old = Instance;
                Instance = null;
                if (old != null && old.gameObject != null)
                {
                    old.gameObject.SetActive(false);      // 本帧剩余时间不再吃 Update / 不再画
                    if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
                }
            }

            Transform host = root;
            if (host == null && ShellRuntime.Instance != null) host = ShellRuntime.Instance.transform;
            if (host == null && WindowsManager.Instance != null) host = WindowsManager.Instance.transform.parent;

            Transform where = host;
            bool foundSafe = false;
            if (host != null)
            {
                // 只有**直接子件**会被找到（`Transform.Find` 的语义）—— 与原版层级一致
                var safe = host.Find(ParentNodeName);
                if (safe != null) { where = safe; foundSafe = true; }
            }
            if (where == null)
                Debug.LogWarning($"[ToastNotifications] 场景里既没有壳（`ShellRuntime`）也没有 `WindowsManager` ⇒ "
                               + $"`{NodeName}` 建在**场景根**上（不跨场景）。");
            else if (!foundSafe)
                // 🔴 RV·M1：这一支**必须出声** —— 实况里 `MainMenuScene` 的 `_root` 下没有安全区节点
                //    （那是 `ShellRuntime` 在**壳根**下建的），退回是必然发生的，⛔ 别静默。
                Debug.LogWarning($"[ToastNotifications] `{(host != null ? host.name : "<null>")}` 下**没有** "
                               + $"`{ParentNodeName}`（原版 `RT 1096.m_Father = 1540`）⇒ `{NodeName}` 挂到 "
                               + $"`{where.name}` 本身。⚠️ 已知实况：`MainMenuScene` 的菜单根下没有那个节点 "
                               + "（安全区在壳根下）——这一条是**如实出声的退化**，不是原版层级。");

            var go = new GameObject(NodeName, typeof(RectTransform));
            go.transform.SetParent(where, false);
            var t = go.AddComponent<ToastNotifications>();
            Instance = t;                       // 🔴 显式登记 —— 编辑模式（自检）**不跑 `Awake`**
            t.BuildInternal();
            return t;
        }

        void BuildInternal()
        {
            // ---- 控制器根：原版 `RT 1096` = a(0,0)-(1,1) sd(0,0)（stretch 满屏）----
            var crt = (RectTransform)transform;
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = Vector2.zero;
            crt.localPosition = Vector3.zero;

            // ---- Holder：原版 `RT 1100` = a(1,0)-(1,0) p(1,0) apos(-21.4228515625,0)
            //      sd(634.4879760742188, 752.614013671875) ----
            var holderRect = new PxRect(HolderRight - HolderW, HolderBottom - HolderH, HolderRight, HolderBottom);
            _holder = Node(transform, HolderNodeName,
                           new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                           new Vector2(HolderAposX, 0f), new Vector2(HolderW, HolderH),
                           holderRect);

            // ---- 6 个 prefab 根（我们加的容器；原版是 6 份独立 prefab 资产）----
            _templates = new GameObject(TemplatesNodeName, typeof(RectTransform)).transform;
            _templates.SetParent(transform, false);
            var trt = (RectTransform)_templates;
            trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0.5f, 0.5f);
            trt.sizeDelta = Vector2.zero;
            trt.localPosition = Vector3.zero;

            // 四个 toast 模板按原版顺序建（`notificationTypes` 的顺序：0/1/2/3）
            _templateByType[(int)ToastNotificationType.Achievement]       = BuildToast(_templates);
            _templateByType[(int)ToastNotificationType.CollectionGainXP]  = BuildGetXP(_templates);
            _templateByType[(int)ToastNotificationType.CollectionLevelUp] = BuildLevelUp(_templates);
            _templateByType[(int)ToastNotificationType.MissionProgress]   = BuildMission(_templates);
            // 🔴 2026-10-17（`N21②③`）：这里原来还有 `BuildCollectorTooltip` / `BuildResourceCounter` 两棵
            //    **零消费者的死树** —— 已删（为什么删 → 下面「模板 5 / 6」那一段）。

            // 出厂不可见（原版场景里 `Holder` 下 0 子；这 6 份是 prefab 资产、不在场景里）
            _templates.gameObject.SetActive(false);
        }

        // ============================================================ 模板 1：Mission Toast Notification
        // 判据：`RT 341` 全文 + 子件 `RT 349/347/345/346/348`（逐字段见下）。
        static GameObject BuildMission(Transform parent)
        {
            var rootRect = PrefabRect(MissionW, MissionH);
            var root = Node(parent, MissionPrefabName,
                            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                            Vector2.zero, new Vector2(MissionW, MissionH), rootRect);

            // `Background Darkener`（RT 349，满铺）：Image **无 sprite**、`m_Color = (0.2827964,0.3010920,0.3867925,1)`
            MenuDraw.Rect(root, CardArt.Solid(), rootRect, "Background Darkener", QShade,
                          new Color(0.28279638290405273f, 0.3010919988155365f, 0.38679248094558716f, 1f));
            // `Background`（RT 347，满铺）：`40K_missions_display_Daily horizontal` · `m_Color = (0.4941176,0.5686275,0.9176471,1)` · Simple
            MenuDraw.Rect(root, CardArt.MenuUi("40K_missions_display_Daily_horizontal"), rootRect,
                          "Background", QBg,
                          new Color(0.4941176474094391f, 0.5686274766921997f, 0.9176470637321472f, 1f));
            // `Text`（RT 345）：a(0,1)-(0,1) p(.5,.5) ap(377.58,-42.264) sd(545.05,55.672)
            //   TMP fs 40 · 白 · Left/Middle · 折行=1（`MB 498`）
            var textRect = UguiRect.Child(rootRect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                          new Vector2(0.5f, 0.5f), new Vector2(377.58f, -42.264f),
                                          new Vector2(545.05f, 55.672f));
            var lb = Tmp(root, textRect, "Deal 500 damage to enemy units", Color.white, "Text", 40f, QText,
                         wrap: true, autoMin: 0f, autoMax: 0f, autoBase: 0f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(lb, textRect);
            MenuDraw.SetVAlign(lb, Label.VAlign.Middle, textRect);

            // `Rewards`（RT 346）：a(0,0.5)-(0,0.5) p(0,0.5) ap(6.10352e-05,0.00100708) sd(107.75,139.71)
            var rewardsRect = UguiRect.Child(rootRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                             new Vector2(0f, 0.5f), new Vector2(6.10352e-05f, 0.00100708f),
                                             new Vector2(107.75f, 139.71f));
            var rewards = Node(root, "Rewards", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                               new Vector2(0f, 0.5f), new Vector2(6.10352e-05f, 0.00100708f),
                               new Vector2(107.75f, 139.71f), rewardsRect);
            // `Reward Display Mission Vertical Variant`（RT 364）：a(0,0)-(1,1) sd(0,0)（`MissionRewardItem`）
            var variantRect = UguiRect.Child(rewardsRect, Vector2.zero, Vector2.one,
                                             new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var variant = Node(rewards, "Reward Display Mission Vertical Variant", Vector2.zero, Vector2.one,
                               new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, variantRect);
            // `drawerHolder`（RT 373）：a(0,0)-(1,1) p(.5,1) ap(-7.62939e-06,1.52588e-05) sd(-35.685,-42.369)
            //   ⚠️ 原版这里挂 `ItemDrawer.Draw(holder, item, amount, 10)`（`MissionRewardItem__Setup.c` 第 3 句）
            //      —— **运行时**才画图标；prefab 的 `Content` 缩放 = (0,0) ⇒ 静态一笔都不画。
            //      本文件在这一格里放一颗**运行时可换图**的图标（`ItemDrawer` 的等效摆法），
            //      尺寸取 `drawerHolder` 自己那块（72.065×97.341）。
            var drawerRect = UguiRect.Child(variantRect, Vector2.zero, Vector2.one,
                                            new Vector2(0.5f, 1f), new Vector2(-7.62939e-06f, 1.52588e-05f),
                                            new Vector2(-35.685f, -42.369f));
            var drawer = Node(variant, "drawerHolder", Vector2.zero, Vector2.one,
                              new Vector2(0.5f, 1f), new Vector2(-7.62939e-06f, 1.52588e-05f),
                              new Vector2(-35.685f, -42.369f), drawerRect);
            MenuDraw.Rect(drawer, CardArt.MenuUi("40k_topmarquee_currency_gold"), drawerRect, "Icon", QArt,
                          null, keepAspect: true);
            // `count`（RT 375）：a(0,0)-(1,0.337) p(.5,0) ap(0,0) sd(0,0.602545)
            //   TMP fs 40 · 白 · **Center/Midline** · autosize 20~40 base 36（`MB 555`）
            //   🔴 2026-10-17（RV·M3）就地订正：原来写的是 Left + 无自适应 ——
            //      `MonoBehaviour_555.json` 实读 `m_HorizontalAlignment = 2 (Center)`、
            //      `m_enableAutoSizing = 1` / min 20 / max 40 / base 36。`Label` 出厂就是居中
            //      ⇒ **不调 `AlignLeft`** 才与原版一致。
            var countRect = UguiRect.Child(variantRect, Vector2.zero, new Vector2(1f, 0.337f),
                                           new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 0.602545f));
            var count = Tmp(variant, countRect, "100", Color.white, "count", 40f, QText,
                            wrap: false, autoMin: 20f, autoMax: 40f, autoBase: 36f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(count, Label.VAlign.Midline, countRect);

            // `Mission Milestones Progress Bar`（RT 348）：a(0,0)-(0,0) p(0,0) ap(107.75,17.783) sd(216.689,51.8301)
            var barRect = UguiRect.Child(rootRect, Vector2.zero, Vector2.zero, Vector2.zero,
                                         new Vector2(107.75f, 17.783f), new Vector2(216.689f, 51.8301f));
            var bar = Node(root, "Mission Milestones Progress Bar", Vector2.zero, Vector2.zero, Vector2.zero,
                           new Vector2(107.75f, 17.783f), new Vector2(216.689f, 51.8301f), barRect);

            // `Progress Bar`（RT 362）：a(0,0)-(1,0.33) p(0,1) ap(5.5,-5.5) sd(-5.5,-5.5)
            var pbRect = UguiRect.Child(barRect, Vector2.zero, new Vector2(1f, 0.33f),
                                        new Vector2(0f, 1f), new Vector2(5.5f, -5.5f),
                                        new Vector2(-5.5f, -5.5f));
            var progressBar = Node(bar, "Progress Bar", Vector2.zero, new Vector2(1f, 0.33f),
                                   new Vector2(0f, 1f), new Vector2(5.5f, -5.5f),
                                   new Vector2(-5.5f, -5.5f), pbRect);
            // `Background`（RT 376，满铺）：`40k_generial_bar_empty` · Sliced · border 4 · ppuMul 2 ·
            //   `m_Color = (1,0.5925925,0,1)`
            Nine(progressBar, "40k_generial_bar_empty", pbRect, new Vector4(4f, 4f, 4f, 4f), 12f, 12f,
                 new Vector4(2f, 2f, 2f, 2f), QBg, new Color(1f, 0.5925925374031067f, 0f, 1f), "Background");
            // `Fill`（RT 361）：prefab 里 a(0,0) sd(0,0)（运行期由 `MissionProgressBarDisplay` 摆）
            //   ⇒ 我们建**满格**的填充，`Show` 时按 `Progress01` 横向缩放（左缘固定）。
            var fillRect = pbRect;
            var fill = Node(progressBar, "Fill", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f),
                            Vector2.zero, new Vector2(0f, 0f), fillRect);
            Nine(fill, "40k_generial_bar_fill", fillRect, new Vector4(4f, 4f, 4f, 4f), 12f, 12f,
                 new Vector4(2f, 2f, 2f, 2f), QArt, new Color(1f, 0.7686275243759155f, 0.3333333432674408f, 1f),
                 "FillQuad");
            // `Handle Slide Area`（RT 374）出厂 **inactive**（`m_IsActive = false`）—— 我们照建、关掉
            var handleAreaRect = UguiRect.Child(pbRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                                                new Vector2(-5f, -6.10352e-05f), new Vector2(-10f, 0f));
            var handleArea = Node(progressBar, "Handle Slide Area", Vector2.zero, Vector2.one,
                                  new Vector2(0.5f, 0.5f), new Vector2(-5f, -6.10352e-05f),
                                  new Vector2(-10f, 0f), handleAreaRect);
            // `Handle`（RT 360）：Image **无 sprite**、`m_Color = (0.9411765,0.7254902,0.3137255,1)` · Sliced ppuMul 13.32
            var handleRect = UguiRect.Child(handleAreaRect, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f),
                                            Vector2.zero, new Vector2(11.65f, 22.406f));
            var handle = Node(handleArea, "Handle", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f),
                              Vector2.zero, new Vector2(11.65f, 22.406f), handleRect);
            MenuDraw.Rect(handle, CardArt.Solid(), handleRect, "HandleQuad", QArt,
                          new Color(0.9411765336990356f, 0.7254902124404907f, 0.3137255012989044f, 1f));
            handleArea.gameObject.SetActive(false);

            // `progress`（RT 363）：a(0,0.33)-(1,1) p(0,0) ap(0,-3) sd(0,6)
            //   TMP fs 35 · `(1,0.7725491,0.3333333,1)` · Left/Middle · 占位 `52/500`（`MB 519`）
            var progRect = UguiRect.Child(barRect, new Vector2(0f, 0.33f), Vector2.one, Vector2.zero,
                                          new Vector2(0f, -3f), new Vector2(0f, 6f));
            var prog = Tmp(bar, progRect, "52/500", new Color(1f, 0.7725490927696228f, 0.3333333432674408f, 1f),
                           "progress", 35f, QText, wrap: false, autoMin: 25f, autoMax: 35f, autoBase: 36f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(prog, progRect);
            MenuDraw.SetVAlign(prog, Label.VAlign.Middle, progRect);

            // 根 = `EverguildButton`（`MB 520`：`m_Transition = 0` ⇒ **悬停不变色**）+ 整块可点
            MenuDraw.Hit(root, "Hit", rootRect, QHit, null);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ============================================================ 模板 2：Toast Collection Level Up
        // 判据：`RT 342`（536.7548828125×97.9000015258789）· 子件 `RT 353/352/354/369`。
        static GameObject BuildLevelUp(Transform parent)
        {
            var rootRect = PrefabRect(LevelUpW, LevelUpH);
            var root = Node(parent, LevelUpPrefabName,
                            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                            Vector2.zero, new Vector2(LevelUpW, LevelUpH), rootRect);

            // 根 Image：`WF_UI_Generic_FancyNotification_Background` · Sliced · border 20 · ppuMul 1.3
            //   （`m_Color = 白`）—— ⚠️ 这张图**本地 Resources 里没有**，见文件头「边界 2」
            FancyBackground(root, rootRect, QBg, "Background");

            // `Icon`（RT 353）：a(0,0.5)-(0,0.5) p(0,0.5) ap(1.61853,-2.5042) sd(86.6998,86.7019)
            //   `40k_main_collection_icon` · preserveAspect = 1
            var iconRect = UguiRect.Child(rootRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                          new Vector2(0f, 0.5f), new Vector2(1.61853f, -2.5042f),
                                          new Vector2(86.6998f, 86.7019f));
            MenuDraw.Rect(root, CardArt.MenuUi("40k_main_collection_icon"), iconRect, "Icon", QArt,
                          null, keepAspect: true);

            // `Text`（RT 352）：a(0,1)-(0,1) p(.5,.5) ap(267.58,-48.95) sd(358.523,81.6935)
            //   TMP fs 54.5 · `(0.9150943,·,·,1)` · Left/**Capline** · 折行=1 ·
            //   autosize min 28 / max 55.46 / base 40（`MB 523`）
            var textRect = UguiRect.Child(rootRect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                          new Vector2(0.5f, 0.5f), new Vector2(267.58f, -48.95f),
                                          new Vector2(358.523f, 81.6935f));
            var lb = Tmp(root, textRect, "Collector Level Up!",
                         new Color(0.9150943160057068f, 0.9150943160057068f, 0.9150943160057068f, 1f),
                         "Text", 54.5f, QText, wrap: true, autoMin: 28f, autoMax: 55.46f, autoBase: 40f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(lb, textRect);
            MenuDraw.SetVAlign(lb, Label.VAlign.Capline, textRect);

            // `Player Level (1)`（RT 354）：a(0,1)-(0,1) p(.5,.5) ap(489.5,-49.246) sd(84.5435,84.5435)
            //   `UI_Button_Round_background`
            var lvlRect = UguiRect.Child(rootRect, new Vector2(0f, 1f), new Vector2(0f, 1f),
                                         new Vector2(0.5f, 0.5f), new Vector2(489.5f, -49.246f),
                                         new Vector2(84.5435f, 84.5435f));
            var lvl = Node(root, "Player Level (1)", new Vector2(0f, 1f), new Vector2(0f, 1f),
                           new Vector2(0.5f, 0.5f), new Vector2(489.5f, -49.246f),
                           new Vector2(84.5435f, 84.5435f), lvlRect);
            MenuDraw.Rect(lvl, CardArt.MenuUi("UI_Button_Round_background"), lvlRect, "Icon", QArt, null,
                          keepAspect: true);
            // `Player Level Text`（RT 369）：a(0,0)-(1,1) p(.5,.5) ap(-3.05176e-05,0) sd(-13.722,-13.722)
            //   TMP fs 37.2 · 白 · **Center/Midline** · autosize min 18 / max 37.2 / base 36.89（`MB 505`）
            var lvlTextRect = UguiRect.Child(lvlRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                                             new Vector2(-3.05176e-05f, 0f), new Vector2(-13.722f, -13.722f));
            // 🔴 **2026-10-20（W58 · D7）**：`wrap: false` → **`true`**。
            //   原版 `MB_505`（`Player Level Text`）的 `m_TextWrappingMode = 1 (Normal)`；
            //   我们这一格原来是 `0 (NoWrap)`（RV·L1）。单/双位数字看不出差别，但字段必须与原版同值。
            var lvlText = Tmp(lvl, lvlTextRect, "-", Color.white, "Player Level Text", 37.2f, QText,
                              wrap: true, autoMin: 18f, autoMax: 37.2f, autoBase: 36.89f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.SetVAlign(lvlText, Label.VAlign.Midline, lvlTextRect);

            MenuDraw.Hit(root, "Hit", rootRect, QHit, null);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ============================================================ 模板 3：Toast（= Achievement / 默认）
        // 判据：`RT 343`（466.1357116699219×**0**）· 子件 `RT 355/356`。
        //   VLG（`MB 204`）：padding L 83 / T 12 / B 7 · spacing 0 · childAlignment 0(UpperLeft) ·
        //     **childControl 0/0** · forceExpand 0/0 ⇒ uGUI `GetChildSizes` 在 `!controlSize` 时取
        //     **子件自己的 `sizeDelta.y`**，而 `RT 356`（Text）的 `m_SizeDelta.y = 0`。
        //   🔴 **2026-10-17（`N23`）最终口径**：第一轮重建时确实 = `12 + 89.8638 + 0 + 7 = 108.8638`，
        //     但原版 `ShowNotificationInternal` 一共重建**三轮**（`FixLayout`×2 + `ForceRebuild…`×1），
        //     `ContentSizeFitterMinMax`（`MB 538`）在第一轮就把 Text 的 `sizeDelta.y` 写成**首选文本高**
        //     ⇒ 真值是 **`12 + 89.8638 + 文本高 + 7`**（随文案变）。⇒ 这里建出来的 `h` **只是兜底**
        //     （未激活/量不出时用），真高由 `ShowInternal` 现量后 `ApplyMeasuredHeight` 重摆
        //     （判据与推导 → 文件头「边界 1」/ `ToastRootHeight`）。
        static GameObject BuildToast(Transform parent)
        {
            // 量占位文案（原版 `m_text = "Congratulations!\nYou've earned an achievement. "`，fs 40，宽 362.057）
            // 🔴 **只用于「文本节点自己那个框」**（Label 按框中心摆字）；**不参与根高**（见上面 `N23`）。
            float textH = MeasureTextPx(parent, "Congratulations!\nYou've earned an achievement. ", 40f, ToastTextW);
            const float h = ToastPadT + ToastIconH + ToastPadB;      // 108.8638（= 兜底值，见上）
            var rootRect = PrefabRect(ToastW, h);
            var root = Node(parent, ToastPrefabName,
                            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                            Vector2.zero, new Vector2(ToastW, h), rootRect);

            // 根 Image：`UI_Deck_Information_submenu_Back_opaque` · Sliced · border 18 · ppuMul 1（`MB 3788653729785366146` 那一颗）
            // 参数走 `BackgroundSpec(false)`（**一处定义** —— 量高之后 `RebuildBackground` 也读它）
            {
                Texture2D btex; Vector4 bborder, bborderOut; float btexW, btexH;
                BackgroundSpec(false, out btex, out bborder, out btexW, out btexH, out bborderOut);
                if (btex == null)
                    Debug.LogWarning("[ToastNotifications] 通用 toast 底图 `UI_Deck_Information_submenu_Back_opaque` "
                                   + "取不到 ⇒ `Background` **没画**（⛔ 不静默）。");
                else
                    MenuDraw.Nine(root, btex, rootRect, bborder, btexW, btexH, QBg, Color.white, true,
                                  "Background", bborderOut);
            }

            // `Icon`（RT 355，VLG 把它摆到 padLeft/padTop；尺寸 = 自己的 89.8613×89.8638）
            //   `40k_Achievements_icon_medal1` · preserveAspect = 1
            var iconRect = new PxRect(rootRect.x1 + ToastPadL, rootRect.y1 + ToastPadT,
                                      rootRect.x1 + ToastPadL + ToastIconW, rootRect.y1 + ToastPadT + ToastIconH);
            MenuDraw.Rect(root, CardArt.MenuUi("40k_Achievements_icon_medal1"), iconRect, "Icon", QArt,
                          null, keepAspect: true);

            // `Text`（RT 356，VLG 第二行；宽 362.057）· TMP fs 40 · 白 · Left/Middle · 折行=1（`MB 507`）
            //   ⚠️ 原版这一颗的 `m_SizeDelta.y = 0`（中点 = 图标下沿）⇒ 框按**中线 = `iconRect.y2`** 摆。
            var textRect = new PxRect(iconRect.x1, iconRect.y2 - textH * 0.5f,
                                      iconRect.x1 + ToastTextW, iconRect.y2 + textH * 0.5f);
            var lb = Tmp(root, textRect, "Congratulations!\nYou've earned an achievement. ",
                         Color.white, "Text", 40f, QText, wrap: true, autoMin: 0f, autoMax: 0f, autoBase: 0f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(lb, textRect);
            MenuDraw.SetVAlign(lb, Label.VAlign.Middle, textRect);

            MenuDraw.Hit(root, "Hit", rootRect, QHit, null);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ============================================================ 模板 4：Toast Collection Get XP
        // 判据：`RT 344` · 子件 `RT 358/357`。VLG（`MB 205`）：padding L 83 / T 0 / B 0 ·
        //   **childControlHeight = 0** + `RT 357.m_SizeDelta.y = 0` ⇒ 第一轮 = `0 + 86.7019 + 0 + 0 = 86.7019`。
        //   🔴 **2026-10-17（`N23`）**：与 `BuildToast` 同一条 —— 真高 = `padT + 86.7019 + 文本高 + padB`，
        //   下面的 `h` 只是**兜底**（判据 → 文件头「边界 1」/ `ToastRootHeight`）。
        static GameObject BuildGetXP(Transform parent)
        {
            float textH = MeasureTextPx(parent, "+25 Collector points!", 55.459999084472656f, GetXPTextW);
            const float h = GetXPPadT + GetXPIconH + GetXPPadB;      // 86.7019（兜底）
            var rootRect = PrefabRect(GetXPW, h);
            var root = Node(parent, GetXPPrefabName,
                            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                            Vector2.zero, new Vector2(GetXPW, h), rootRect);

            FancyBackground(root, rootRect, QBg, "Background");

            // `Icon`（RT 358）`40k_main_collection_icon` · preserveAspect = 1
            var iconRect = new PxRect(rootRect.x1 + GetXPPadL, rootRect.y1 + GetXPPadT,
                                      rootRect.x1 + GetXPPadL + GetXPIconW, rootRect.y1 + GetXPPadT + GetXPIconH);
            MenuDraw.Rect(root, CardArt.MenuUi("40k_main_collection_icon"), iconRect, "Icon", QArt,
                          null, keepAspect: true);

            // `Text`（RT 357）· TMP fs 55.46 · `(0.9150943,·,·,1)` · Left/**Capline** ·
            //   `m_TextWrappingMode = 0`(NoWrap) · autosize min 28 / max 55.46 / base 40（`MB 514`）
            //   ⚠️ 原版这一颗 `m_SizeDelta.y = 0`（中点 = 图标下沿）⇒ 框按**中线 = `iconRect.y2`** 摆；
            //      `textH` 只用于这个框，**不进根高**（RV·H2）。
            var textRect = new PxRect(iconRect.x1, iconRect.y2 - textH * 0.5f,
                                      iconRect.x1 + GetXPTextW, iconRect.y2 + textH * 0.5f);
            var lb = Tmp(root, textRect, "+25 Collector points!",
                         new Color(0.9150943160057068f, 0.9150943160057068f, 0.9150943160057068f, 1f),
                         "Text", 55.459999084472656f, QText, wrap: false, autoMin: 28f,
                         autoMax: 55.459999084472656f, autoBase: 40f, face: TmpFont.Face.LatinCardBody);
            MenuDraw.AlignLeft(lb, textRect);
            MenuDraw.SetVAlign(lb, Label.VAlign.Capline, textRect);

            MenuDraw.Hit(root, "Hit", rootRect, QHit, null);
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ============================================================ 模板 5 / 6：已删（`N21②③`）
        // 🔴 **2026-10-17（`N21②③`）删掉了两条死模板**：`Collector Level Tooltip` 与 `Resource Counter Item`。
        // **为什么删**：原版那两份 prefab 确实存在（`RT 351` / `RT 359`），但**没有任何生产路径会用到它们** ——
        //   · `ToastNotificationController` 的 `notificationTypes` 四条只指向**四个 toast**（`MB 1939`：
        //     `{0→491}{1→492}{2→490}{3→488}`），`defaultToastNotification = 491` ⇒ 这两份**永远取不到**；
        //   · 我们这边：删之前全仓 `grep "CollectorTooltipPrefabName\|ResourceCounterPrefabName\|BuildCollectorTooltip\|BuildResourceCounter"`
        //     **命中 0 个消费者**（只在本文件里自引用）；命中的 `Resource Counter Item` 字样全在 `Shell/TopBar.cs`
        //     与 `Editor/{MainMenuScene,DeckScene}.cs` —— 那是**顶栏自己**那一族节点，与本文件无关。
        // ⇒ 留着 = 每次 `BuildInternal()` 白建两棵死树（还各带一次 `CardArt.MenuUi` 取图）。
        // ⚠️ 原版那两份 prefab 的参数**没有丢**：仍在文件头 ② 与 `资料/普查产出_1017/UI/R_界面线剩余条目现核.md` §N21。

        // ============================================================ 生产口（≡ 原版静态方法）

        /// <summary>≡ 原版 `ToastNotificationController.ShowNotification(context)`。
        /// 先走 handler 表（`TryShowNotification` 原文），没有 handler 才自己弹。</summary>
        public static bool ShowNotification(Context ctx)
        {
            var t = Ensure();
            if (t == null)
            {
                // 原版这一支是**抛异常**（`__ShowNotification.c`：`Instance == 0` ⇒ `FUN_1803f47a0`）。
                // 我们出声 + 返回 false（壳没建起来时的运行时不该被一条飘窗炸掉）。
                Debug.LogError("[ToastNotifications] `ShowNotification`：壳里没有 `ToastNotificationController` "
                             + "（也没有 `ShellRuntime` / `WindowsManager`）⇒ **这条飘窗没弹**（⛔ 不静默）。");
                return false;
            }
            if (!string.IsNullOrEmpty(ctx.ControllerId) && _handlers.ContainsKey(ctx.ControllerId))
            {
                _handlers[ctx.ControllerId].ShowNotification(ctx);
                return true;
            }
            return t.ShowInternal(ctx);
        }

        /// <summary>≡ 原版 `ScheduleNotification(context)`（`MenuActions.QueueAction` 那一跳）。
        /// 入队，下一次 `Step`/`Update` 开头排空 —— **不是立刻弹**。</summary>
        public static bool ScheduleNotification(Context ctx)
        {
            var t = Ensure();
            if (t == null)
            {
                Debug.LogError("[ToastNotifications] `ScheduleNotification`：壳里没有控制器 ⇒ **没排队、也没弹**。");
                return false;
            }
            t._scheduled.Enqueue(ctx);
            return true;
        }

        /// <summary>≡ 原版 `RegisterNotificationHandler(handler, id)`。同 id 后注册的覆盖先注册的
        /// （原版是 `Dictionary`，`FUN_181779c90` 就是 `dict[id] = handler`）。</summary>
        public static void RegisterNotificationHandler(string id, IHandler handler)
        {
            if (string.IsNullOrEmpty(id) || handler == null) return;
            _handlers[id] = handler;
        }

        /// <summary>便捷口：`ShowNotification` + 原版 `Context` 的 ctor 形参。
        /// `Show(MissionToastKind …)` 那一族 = 这里；枚举名照原版叫 <see cref="ToastNotificationType"/>。</summary>
        public static bool Show(ToastNotificationType type, string text, string iconArt = null,
                                Action onClick = null, bool localize = false)
        {
            var c = new Context(text, type, iconArt, onClick);
            c.Localize = localize;
            return ShowNotification(c);
        }

        /// <summary>便捷口：`ScheduleNotification`（推迟一拍）。</summary>
        public static bool Schedule(ToastNotificationType type, string text, string iconArt = null,
                                   Action onClick = null, bool localize = false)
        {
            var c = new Context(text, type, iconArt, onClick);
            c.Localize = localize;
            return ScheduleNotification(c);
        }

        // ---- 有本地数据源的那一条 ----

        /// <summary>任务进度飘窗（`ToastNotificationType.MissionProgress`）。
        /// 数据源 = `Shell/DailyData.cs`（本地有：`Desc` / `Progress` / `Target` / `RewardArt` / `RewardText`）。
        /// 越界 / 数据为空 ⇒ 出声 + false（⛔ 不编一条假任务）。</summary>
        public static bool ShowMission(int dailyIndex)
        {
            if (dailyIndex < 0)
            {
                Debug.LogWarning($"[ToastNotifications] `ShowMission({dailyIndex})`：下标非法 ⇒ 不弹。");
                return false;
            }
            string text = DailyData.DailyDesc(dailyIndex);
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning($"[ToastNotifications] `ShowMission({dailyIndex})`：`DailyData` 没给出文案 ⇒ 不弹。");
                return false;
            }
            var c = new Context(text, ToastNotificationType.MissionProgress);
            c.ShowProgress = true;
            c.Progress01 = DailyData.DailyProgress01(dailyIndex);
            c.ProgressText = DailyData.DailyCounter(dailyIndex);
            c.RewardArt = DailyData.DailyRewardArt(dailyIndex);
            c.RewardText = DailyData.DailyRewardText(dailyIndex);
            return ShowNotification(c);
        }

        // ---- 本地【没有】数据源的那两条（如实出声） ----

        /// <summary>收藏经验飘窗。🔴 **本地没有数据源**（原版 `CollectorLevelData` / `CollectorLevelChangeSignal`
        /// 都由服务端下发）⇒ **不弹**。要弹请走 `Show(CollectionGainXP, 真实文案, 图标)`。</summary>
        public static bool ShowCollectionGainXP(int amount)
        {
            Debug.LogWarning("[ToastNotifications] `ShowCollectionGainXP(" + amount + ")`：**本地没有玩家收藏经验"
                           + "的数据源**（原版由服务端下发，`CollectorLevelData` 本地只有类型、没有实例）⇒ "
                           + "**不弹**（⛔ 不编数值）。有真实事件时走 `Show(CollectionGainXP, 文案, 图标)`。");
            return false;
        }

        /// <summary>收藏等级提升飘窗。🔴 **本地没有数据源** ⇒ **不弹**（同上）。</summary>
        public static bool ShowCollectionLevelUp(int level)
        {
            Debug.LogWarning("[ToastNotifications] `ShowCollectionLevelUp(" + level + ")`：**本地没有玩家收藏等级"
                           + "的数据源**（原版由服务端下发）⇒ **不弹**（⛔ 不编数值）。"
                           + "有真实事件时走 `Show(CollectionLevelUp, 文案, 图标)`。");
            return false;
        }

        // ============================================================ 弹一条

        bool ShowInternal(Context ctx)
        {
            int ti = (int)ctx.Type;
            if (ti < 0 || ti >= 4 || _templateByType[ti] == null)
            {
                Debug.LogError($"[ToastNotifications] `{ctx.Type}` 没有模板 ⇒ 这条飘窗没弹（⛔ 不静默）。");
                return false;
            }

            // ① Instantiate(prefab, holder) —— 原版 `__ShowNotificationInternal.c` 第 1 句
            var go = Instantiate(_templateByType[ti], _holder, false);
            go.SetActive(true);

            // 🔴 **2026-10-17（第四十八会话 · 桶 B）：克隆体上的运行时后端引用全是 null，必须逐个重绑。**
            //   `Label._tmp/_mr` 与 `ImageQuad._mf/_mr` 都是**非序列化字段** ⇒
            //   不重绑时 `Fill → SetText` 会误走点阵分支并在 `SetTextDot` 里 NRE
            //   （ShellScene 整条崩），下一个撞的是 `ImageQuad.RebuildMesh`。
            foreach (var lb in go.GetComponentsInChildren<Label>(true)) lb.RebindAfterClone();
            foreach (var iq in go.GetComponentsInChildren<ImageQuad>(true)) iq.RebindAfterClone();

            // ② Initialize(context)
            Fill(go.transform, ctx);

            // ③④ 布局跑完再量高 —— 🔴 **2026-10-17（`N23`）：按【本次文案】现量**（不是模板常量）。
            //    判据/推导 → 文件头「边界 1」；兜底常量 = 模板建出来那两档（未激活/量不出时才用，且出声）。
            if (ctx.Type == ToastNotificationType.Achievement || ctx.Type == ToastNotificationType.CollectionGainXP)
            {
                bool fancy = ctx.Type == ToastNotificationType.CollectionGainXP;
                float padT = fancy ? GetXPPadT : ToastPadT;
                float iconH = fancy ? GetXPIconH : ToastIconH;
                float padB = fancy ? GetXPPadB : ToastPadB;
                float fallback = padT + iconH + padB;      // 108.8638 / 86.7019
                float measured = ToastRootHeight(go.transform, padT, iconH, padB, fallback);
                ApplyMeasuredHeight(go, measured, fancy);
            }
            var rt = (RectTransform)go.transform;
            float h = PxOf(rt.sizeDelta.y);

            // 根 = `EverguildButton`（原版 `ToastNotification.Initialize` 把 `context.OnClick` 挂到
            // `button.onClick`）。⚠️ 放在**量高之后**：`ApplyMeasuredHeight` 可能重建命中区。
            var hitBtn = go.GetComponentInChildren<WindowButton>(true);
            if (hitBtn != null)
            {
                hitBtn.onClick = ctx.OnClick;

                // 🔴 **2026-10-20（W58 · D6）hover 色偏（判据 → 文件头 ⑤）**：
                //   原版 `Toast` / `Toast Collection Level Up` / `Get XP` 的根 `EverguildButton`
                //   是 `m_Transition = 1 (ColorTint)`、`m_Colors` = 全库默认那一组
                //   （Normal 白 / Highlighted `0.9608…` / Pressed `0.7843…` / `m_FadeDuration 0.1`
                //   —— `MonoBehaviour_504/508/513.json`）⇒ 悬停时**根底图乘 0.9608**。
                //   `Mission` **不接**：它的 `EverguildButton` 是 `m_Transition = 0 (None)`、`target = 0`
                //   （`MonoBehaviour_520.json`）⇒ 原版悬停什么都不变。⛔ 别四颗一起补。
                //   ⚠️ 必须在**第一次悬停之前**接（`WindowButton.Collect` 只收一次、顺带记基准色）；
                //      且必须放在 `ApplyMeasuredHeight` **之后** —— 那一刻 `Background` 的九宫格可能刚被重建，
                //      之前抓的 quad 已经是旧的那一批（A222 的 `TintOn` 语义 = 原版 `m_TargetGraphic`）。
                if (ctx.Type != ToastNotificationType.MissionProgress)
                {
                    var bg = go.transform.Find("Background");
                    var bgQuads = bg != null ? bg.GetComponentsInChildren<ImageQuad>(true) : null;
                    if (bgQuads != null && bgQuads.Length > 0)
                        hitBtn.TintOn(bgQuads);
                    else
                        Debug.LogWarning($"[ToastNotifications] `{ctx.Type}` 的 hover 色偏没接上："
                                       + "`Background` 下没有 quad ⇒ 悬停时**画面不变**（⛔ 不静默）。");
                }
            }

            // ⑤⑥ 入场位置 + 格位
            int slot = _freeSpot; _freeSpot++;
            var live = new Live { Go = go, HeightPx = h, Slot = slot, T = 0f,
                                  TargetPx = slot * h + FirstSlotOffsetPx };
            Pose(live, -h);
            _live.Add(live);

            ShowCount++;
            LastShownType = ctx.Type;
            LastShownText = ctx.Text;
            return true;
        }

        void Fill(Transform root, Context ctx)
        {
            string text = ctx.Text;
            if (ctx.Localize && !string.IsNullOrEmpty(text)) text = Loc.T(text);

            switch (ctx.Type)
            {
                case ToastNotificationType.MissionProgress:
                    {
                        // 🔴 **2026-10-17（`N20②`）静态补齐的两条原版规则**（判据 → 文件头「边界 3」）：
                        //   ① 三个 `MissionInfoDisplay` 的 prefab 字段全是 `displayRule = 1`
                        //      （`MonoBehaviour/{499,500,501}.json`）⇒ `MissionInfoDisplay__Initialize` 的位运算
                        //      = **只在「未完成」时显示**（完成 ⇒ `Rewards` / `Progress Bar` / `progress` 三件全关）。
                        //      「完成」的等价物 = `progress >= maxValue`（`MissionChallengeProgress.IsComplete`
                        //      对日任务就是「进度到顶」）⇒ 用 `Progress01 >= 1`。
                        //   ② `MissionCounterDisplay__Setup`：完成 且 `displayCompletedMessage = 1`
                        //      （`MB 499` 实读）⇒ 文案 = 词条 `Missions/Completed`；否则印 `progressTextFormat
                        //      "{0}/{1}"`（= 调用方给的 `ProgressText`）。
                        //   ⚠️ **补不了的**（数据在服务端/运行时）：`MissionRewardsDisplay` 逐奖励实例化、
                        //      `MissionMilestonesDisplay` 逐里程碑实例化（阈值来自 `AvailableRewards()`）、
                        //      以及 `MissionRewardItem.ToggleAmount(amount < 3)` 那条计数显隐 —— 我们的
                        //      `Context` 只有 `RewardText`（字符串），**拿不到原版那个数值** ⇒ 仍按「有文案就显示」。
                        bool complete = ctx.ShowProgress && ctx.Progress01 >= 1f;
                        var lb = FindLabel(root, "Text");
                        if (lb != null && text != null) lb.SetText(text);
                        var icon = FindQuad(root, "Rewards/Reward Display Mission Vertical Variant/drawerHolder/Icon");
                        if (icon != null)
                        {
                            var tex = string.IsNullOrEmpty(ctx.RewardArt) ? null : CardArt.MenuUi(ctx.RewardArt);
                            if (tex != null) icon.SetTexture(tex, false);
                            icon.gameObject.SetActive(tex != null);
                        }
                        var cnt = FindLabel(root, "Rewards/Reward Display Mission Vertical Variant/count");
                        if (cnt != null)
                        {
                            bool has = !string.IsNullOrEmpty(ctx.RewardText);
                            cnt.SetText(has ? ctx.RewardText : "");
                            cnt.gameObject.SetActive(has);
                        }
                        var rewards = root.Find("Rewards");
                        if (rewards != null) rewards.gameObject.SetActive(!complete
                                                                       && (!string.IsNullOrEmpty(ctx.RewardText)
                                                                           || !string.IsNullOrEmpty(ctx.RewardArt)));

                        var bar = root.Find("Mission Milestones Progress Bar");
                        if (bar != null) bar.gameObject.SetActive(ctx.ShowProgress);
                        if (ctx.ShowProgress)
                        {
                            var pg = FindLabel(bar, "progress");
                            if (pg != null)
                            {
                                if (complete) pg.SetText(Loc.T("Missions/Completed"));
                                else if (ctx.ProgressText != null) pg.SetText(ctx.ProgressText);
                                pg.gameObject.SetActive(!complete);
                            }
                            var pbNode = bar.Find("Progress Bar");
                            if (pbNode != null) pbNode.gameObject.SetActive(!complete);
                            ApplyFill(bar, Mathf.Clamp01(ctx.Progress01));
                        }
                        break;
                    }
                case ToastNotificationType.CollectionLevelUp:
                case ToastNotificationType.CollectionGainXP:
                case ToastNotificationType.Achievement:
                    {
                        var lb = FindLabel(root, "Text");
                        if (lb != null && text != null) lb.SetText(text);
                        if (!string.IsNullOrEmpty(ctx.IconArt))
                        {
                            var icon = FindQuad(root, "Icon");
                            var tex = CardArt.MenuUi(ctx.IconArt);
                            if (icon != null && tex != null) icon.SetTexture(tex, false);
                            else if (tex == null)
                                Debug.LogWarning($"[ToastNotifications] 图标 `{ctx.IconArt}` 取不到 ⇒ 这一条沿用模板图"
                                               + "（⛔ 不静默）。");
                        }
                        break;
                    }
            }

            // 根 = `EverguildButton` 那颗 `WindowButton` 的 `onClick` 现在由 `ShowInternal` 在**量高之后**挂
            //（`N23`：`ApplyMeasuredHeight` 可能重建命中区 ⇒ 在这里挂会被换掉）。
        }

        /// <summary>`Fill`（原版 `RT 361`）：一颗 **Sliced** Image（`m_Type = 1` ·
        /// `m_PixelsPerUnitMultiplier = 2.0` ⇒ 画出来的角块 = border 4 ÷ 2 = **2px**，见 `MonoBehaviour_410.json`）
        /// —— 运行时由 `MissionProgressBarDisplay` 只改**宽度**（左缘钉在 `Progress Bar` 的左沿）。
        ///
        /// <para>🔴 **2026-10-17（`N20③`）就地改法**：原来改的是 `fill.localScale.x` —— 那会让**九宫格角块
        /// 跟着横向缩放**（进度 0.2 时角块只剩 0.4px），与原版「Sliced ⇒ 角块恒 2px」不符。
        /// 现在按目标宽度**重建那九个块**（⛔ 不碰 `localScale`）。</para>
        ///
        /// <para>**幂等**：满格宽取自 `Progress Bar` 自己的 `RectTransform.rect`（`RT 362` = 211.189px），
        /// 左/上/下沿取自**现网格**（重建只动右沿 ⇒ 第二次进来这三点仍与第一次相同）；
        /// 位置由 `MenuDraw.Nine` 按父件（`Fill` 节点）的设计坐标算 —— `Fill` 节点**不动**。</para></summary>
        void ApplyFill(Transform bar, float p)
        {
            var pb = bar.Find("Progress Bar");
            if (pb == null) return;
            var fill = pb.Find("Fill");
            if (fill == null) return;
            var pbRt = pb as RectTransform;
            if (pbRt == null) return;
            float fullW = PxOf(pbRt.rect.width);       // 满格宽（`RT 362` = 211.189 px）
            if (!(fullW > 1f))
            {
                Debug.LogWarning($"[ToastNotifications] `Progress Bar` 的 rect 宽量不出来（{fullW:F2}px）⇒ 进度条没画（⛔ 不静默）。");
                return;
            }
            float x1, y1, x2, y2;
            // ⚠️ `QuadGate.Self`：完成态下父链可能已被关掉（`displayRule`），而块自己仍 active ⇒ 用 Self 才量得到。
            if (!MenuDraw.UnionQuadRectPx(fill, MenuDraw.QuadGate.Self, true,
                                          out x1, out y1, out x2, out y2))
            {
                Debug.LogWarning("[ToastNotifications] `Progress Bar/Fill` 下没有 quad ⇒ 进度条没画（⛔ 不静默）。");
                return;
            }
            float w = Mathf.Max(1f, fullW * Mathf.Clamp01(p));
            ClearChildrenNow(fill);
            Nine(fill, "40k_generial_bar_fill", new PxRect(x1, y1, x1 + w, y2),
                 new Vector4(4f, 4f, 4f, 4f), 12f, 12f, new Vector4(2f, 2f, 2f, 2f), QArt,
                 new Color(1f, 0.7686275243759155f, 0.3333333432674408f, 1f), "FillQuad");
        }

        // ============================================================ 根高（`N23`）

        /// <summary>根高 = `padT + iconH + 文本首选高 + padB`（`N23`；推导 → 文件头「边界 1」）。
        /// 只服务 `Toast` / `Toast Collection Get XP` 两条模板（原版 VLG + CSF 那两条）。
        /// <para>**量不出**（未激活 / 字体资产缺失 / 哨兵值）⇒ **出声 + 退回模板常量**（调用方给的 `fallback`）。
        /// ⛔ 别把兜底当正常路径：正常情况下这条一次都不该响。</para></summary>
        static float ToastRootHeight(Transform root, float padT, float iconH, float padB, float fallback)
        {
            var lb = FindLabel(root, "Text");
            if (lb == null)
            {
                Debug.LogWarning($"[ToastNotifications] 根高：找不到 `{root.name}/Text` ⇒ 退回模板常量 {fallback:F2}px。");
                return fallback;
            }
            lb.ForceRelayout();                 // 文案刚在 `Fill` 里换过 ⇒ 重排一次再量（TMP 在激活态才量得准）
            float textH = PxOf(lb.WorldH);
            if (!(textH > 0f) || textH > 100000f)
            {
                // 🔴 **2026-10-17（R1 现核）**：上界原来是 **400px** —— 长文案真值 **627.51px**
                //   （`= 9.000 × 单行 69.72`）被误判成「量不出来」⇒ 退回常量 108.86（N23 红）。
                //   哨兵值是 **4.29e9** 那一档（同 `Label.UsableBounds`）⇒ 上界取 100000 足够宽松。
                Debug.LogWarning($"[ToastNotifications] 根高：文本首选高量不出来（{textH:F2}px）⇒ 退回模板常量 "
                               + $"{fallback:F2}px（这一条只该在未激活 / 字体缺失时出现）。");
                return fallback;
            }
            return padT + iconH + textH + padB;
        }

        /// <summary>按 `newH` 重摆**已实例化**的那一条（`N23`）。只动 `Toast` / `Get XP`：
        /// 根高 + 根底图（九宫格按新矩形重建）+ 图标/文字（跟着顶边上移）+ 命中区（跟着长高）。
        /// <para>⚠️ 根轴心 = **右下角**（原版 `a(1,0)/p(1,0)`）⇒ 底边不动、**向上长**；
        /// 所以子件要**上移** `ΔH`（图标/文字）或 `ΔH/2`（居中件：底图、命中区）。</para>
        /// <para>⛔ **别在这里用 `MenuDraw.Local(root, …)` 重算绝对位置** —— 实例刚 `Instantiate` 时
        /// 还没被 `Pose()` 摆到 Holder 上（`localPosition` 是模板那份），拿它当设计原点会全体偏移。
        /// 这里只做**局部平移/局部重建**（世界单位 = `LayoutSpace.Px`）。</para></summary>
        void ApplyMeasuredHeight(GameObject go, float newH, bool fancyBg)
        {
            if (go == null) return;
            var root = go.transform;
            var rt = root as RectTransform;
            if (rt == null) return;
            float oldH = PxOf(rt.sizeDelta.y);
            if (!(newH > 0f) || Mathf.Abs(newH - oldH) < 0.01f) return;

            rt.sizeDelta = new Vector2(rt.sizeDelta.x, LayoutSpace.Px(newH));
            float w = PxOf(rt.sizeDelta.x);
            float dy = LayoutSpace.Px(newH - oldH);          // 顶边上移量（世界单位）

            // ---- 底图：中心上移 ΔH/2，再按新矩形重建九宫格 ----
            var bg = root.Find("Background");
            if (bg != null)
            {
                var p = bg.localPosition; p.y += dy * 0.5f; bg.localPosition = p;
                RebuildBackground(bg, w, newH, fancyBg);
            }

            // ---- 图标 / 文字：跟着顶边走 ----
            var icon = root.Find("Icon");
            if (icon != null) { var p = icon.localPosition; p.y += dy; icon.localPosition = p; }
            var text = root.Find("Text");
            if (text != null) { var p = text.localPosition; p.y += dy; text.localPosition = p; }

            // ---- 命中区：quad 中心在 rect 中心 ⇒ 上移 ΔH/2，高度换成新根高 ----
            var hit = root.Find("Hit");
            if (hit != null)
            {
                var hrt = hit as RectTransform;
                if (hrt != null) hrt.sizeDelta = new Vector2(hrt.sizeDelta.x, LayoutSpace.Px(newH));
                var q = hit.GetComponentInChildren<ImageQuad>(true);
                if (q != null)
                {
                    var p = q.transform.localPosition; p.y += dy * 0.5f; q.transform.localPosition = p;
                    q.SetWorldHeight(LayoutSpace.Px(newH));
                    q.SetAspect(Mathf.Max(1e-4f, w) / Mathf.Max(1e-4f, newH));
                }
            }
        }

        /// <summary>按**局部矩形**（宽 `wPx` × 高 `hPx`，设计 px）重建 `Background` 那颗九宫格。
        /// 底图参数**只有一份** —— 与 `BuildToast` / `FancyBackground` 共用 <see cref="BackgroundSpec"/>。
        /// ⚠️ 用 `ImageQuad.CreateNineSlice(..., center: Vector3.zero, ...)`：`bg` 节点本身就在新矩形的中心
        /// ⇒ 子块以局部原点为中心，**不依赖** `MenuDraw.Local`（见 `ApplyMeasuredHeight` 的警告）。</summary>
        static void RebuildBackground(Transform bg, float wPx, float hPx, bool fancy)
        {
            Texture2D tex; Vector4 border, borderOut; float texW, texH;
            BackgroundSpec(fancy, out tex, out border, out texW, out texH, out borderOut);
            ClearChildrenNow(bg);
            if (tex == null) return;         // `BuildToast` / `FancyBackground` 那两条已经出过声
            var go = ImageQuad.CreateNineSlice(bg, tex, border, texW, texH, Vector3.zero,
                                               LayoutSpace.Px(wPx), LayoutSpace.Px(hPx), "Background",
                                               borderOut, true);
            if (go == null) return;
            foreach (var q in go.GetComponentsInChildren<ImageQuad>())
            {
                q.SetTint(Color.white);
                q.SetRenderQueue(QBg);
            }
        }

        /// <summary>两条模板的根底图参数（**一处定义**）：`fancy = false` ⇒ 通用 `Toast` 那张
        /// `UI_Deck_Information_submenu_Back_opaque`（border 18 · 69×63 · 画出来的角块 18）；
        /// `fancy = true` ⇒ 原版 `WF_UI_Generic_FancyNotification_Background`（border 20 · 352×116 ·
        /// 角块 15.3846），本地缺图时**退回**同一张通用底图并**出声一次**（判据 → `FancyBackground`）。</summary>
        static void BackgroundSpec(bool fancy, out Texture2D tex, out Vector4 border,
                                   out float texW, out float texH, out Vector4 borderOut)
        {
            if (fancy)
            {
                tex = CardArt.MenuUi(FancyBackgroundName);
                if (tex != null)
                {
                    border = new Vector4(20f, 20f, 20f, 20f);
                    texW = 352f; texH = 116f;
                    borderOut = new Vector4(15.384615f, 15.384615f, 15.384615f, 15.384615f);
                    return;
                }
                WarnFancyBackgroundMissingOnce();
            }
            tex = CardArt.MenuUi("UI_Deck_Information_submenu_Back_opaque");
            border = new Vector4(18f, 18f, 18f, 18f);
            texW = 69f; texH = 63f;
            borderOut = border;
        }

        // ============================================================ 时序泵（原版 DOTween.Sequence）

        /// <summary>推进 `dt` 秒。真机由 `Update` 喂；🔴 **批处理 / 编辑模式没有帧循环 ⇒ 自检必须自己调**。</summary>
        public void Step(float dt)
        {
            // `ScheduleNotification` 那一跳：`MenuActions.QueueAction` 的等效物
            while (_scheduled.Count > 0) ShowInternal(_scheduled.Dequeue());

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var l = _live[i];
                l.T += dt;
                float h = l.HeightPx;
                if (l.T < MoveSeconds)
                {
                    // 🔴 RV·H3：⛔ **别用 `Mathf.Lerp`** —— 它内部有 `Clamp01`，会把 OutBack 的
                    //    overshoot（>1）夹平（`EaseOutBack(0.75) ≈ 1.0641`）。手写不夹的线性插值。
                    float e = EaseOutBack(l.T / MoveSeconds);
                    Pose(l, -h + (l.TargetPx + h) * e);
                }
                else if (l.T < MoveSeconds + HoldSeconds)
                {
                    Pose(l, l.TargetPx);
                }
                else if (l.T < MoveSeconds * 2f + HoldSeconds)
                {
                    // 同上：InBack 的 undershoot **< 0** 也会被 `Mathf.Lerp` 夹平。
                    float e = EaseInBack((l.T - MoveSeconds - HoldSeconds) / MoveSeconds);
                    Pose(l, l.TargetPx + (-h - l.TargetPx) * e);
                }
                else
                {
                    // `.AppendCallback(销毁 + freeSpot = min(index, freeSpot))`
                    _freeSpot = Mathf.Min(l.Slot, _freeSpot);
                    _live.RemoveAt(i);
                    if (l.Go != null) Kill(l.Go);
                }
            }
        }

        void Pose(Live l, float shiftPx)
        {
            if (l.Go == null) return;
            // 模板根的轴心 = 它自己的右下角（原版 prefab 全是 a(1,0)/p(1,0)）；
            // Holder 的原点 = 它自己的右下角 ⇒ `anchoredPosition` ≡ `localPosition`。
            l.Go.transform.localPosition = new Vector3(0f, LayoutSpace.Px(shiftPx), 0f);
        }

        static void Kill(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        void Update() { Step(Time.deltaTime); }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>清掉这一颗（连带节点），`Instance` 归 null —— 只为自检「从零再建」用。</summary>
        public static void DisposeForTest()
        {
            if (Instance == null) return;
            var go = Instance.gameObject;
            Instance = null;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        // ============================================================ 切场景生命周期（W58 · D5）
        //
        // 🔴 **2026-10-20（W58 · D5）判据**：原版 `ToastNotificationController` **只存在于主菜单场景**
        //   （`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour_1939.json`；13 个 `battlearena*` 里 **0 处**）
        //   ⇒ **切场景 = 整棵控制器连同在飞的 toast 一起销毁**，不存在跨场景存活。
        //   我们这一颗建在 `DontDestroyOnLoad` 的**壳**的 `Safe area Only Horizontal` 下
        //   （`MainMenuRuntime.Build` → `ToastNotifications.Build`），壳不清它就会跨场景活着。
        //   ⇒ 下面两个口：① `ClearLive()` 只清在飞的（控制器还在）；
        //     ② `DisposeForSceneChange()` = 清在飞 + **连控制器一起销毁**（= 原版「随主菜单场景销毁」）。
        //   ⚠️ 调用点 = `ShellRuntime.LoadSceneWithFade`（切场景前）+ `ShellRuntime` 的全局
        //      `SceneManager.sceneLoaded` 兜底（覆盖不走本方法的 `LoadScene` 站点）。
        //   回主菜单时由 `MainMenuRuntime.Build` 重新 `Build` 一颗 —— 与原版「主菜单场景里有、别处没有」同构。

        /// <summary>清掉**所有在飞的 toast**（+ 排队中的 `ScheduleNotification`），格位归零；**控制器本身留着**。
        /// <para>⚠️ 只清本文件的 toast；顶部那条**错误横幅**（`MessageToast` / `UIMessageController`）是另一个类，
        /// 原版在 13 个战场里也有它 ⇒ **别在这里顺手清它**。</para></summary>
        public static void ClearLive()
        {
            if (Instance == null) return;
            Instance.ClearLiveInternal();
        }

        void ClearLiveInternal()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var l = _live[i];
                if (l.Go == null) continue;
                // 先关再删：Play 下 `Destroy` 是**帧末**才真消失，不关会多画一帧（同 `Build` 的写法）。
                l.Go.SetActive(false);
                Kill(l.Go);
            }
            _live.Clear();
            _scheduled.Clear();
            _freeSpot = 0;
        }

        /// <summary>切场景前调：清在飞 toast + **销毁控制器节点**（`Instance` 归 null）。
        /// <para>= 原版「`ToastNotificationController` 随主菜单场景一起销毁」的等效物（判据 → 上面那一段）。
        /// ⚠️ 只由 `ShellRuntime` 在切场景时调；自检要「从零再建」仍用 <see cref="DisposeForTest"/>。</para></summary>
        public static void DisposeForSceneChange()
        {
            if (Instance == null) return;
            Instance.ClearLiveInternal();
            // Play 下 `Destroy` 是**帧末**才真消失 ⇒ 先关掉整棵，保证切场景那一帧起它一帧都不再画 / 不再跑 `Update`。
            Instance.gameObject.SetActive(false);
            DisposeForTest();
        }

        // ============================================================ 缓动（DOTween `Ease.OutBack` / `InBack`）
        // 判据：`__ShowNotificationInternal.c` 的 `SetEase(0x1b)` / `SetEase(0x1a)`；DOTween 的
        // `Ease` 枚举 = `InBack 26` / `OutBack 27`（`Battle/BattleCameraSreenSize.cs` 那张表的同源）。
        // 公式 = DOTween 的标准 back 缓动（c1 = 1.70158 · c3 = c1 + 1）。
        const float BackC1 = 1.70158f;
        const float BackC3 = BackC1 + 1f;
        static float EaseOutBack(float t) { t = Mathf.Clamp01(t) - 1f; return 1f + BackC3 * t * t * t + BackC1 * t * t; }
        static float EaseInBack(float t) { t = Mathf.Clamp01(t); return BackC3 * t * t * t - BackC1 * t * t; }

        // ============================================================ 建件工具

        /// <summary>世界单位 → 设计 px（= `LayoutSpace.Px` 的反向，同一条换算，⛔ 别在别处再乘一个 108）。</summary>
        static float PxOf(float world) { return world * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight); }

        /// <summary>prefab 根的矩形：轴心（右下角）= `Holder` 的右下角（原版 a(1,0) p(1,0) ap(0,0)）。</summary>
        static PxRect PrefabRect(float w, float h)
        {
            return new PxRect(HolderRight - w, HolderBottom - h, HolderRight, HolderBottom);
        }

        /// <summary>建一个节点：**元数据照原版**（anchor / pivot / sizeDelta / anchoredPosition 都写进去），
        /// 位置写 `design` 那个**画布 px 矩形**（本仓外壳是世界空间 quad，矩形中心/轴心自己算好）。</summary>
        static Transform Node(Transform parent, string name,
                              Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 apos, Vector2 sd,
                              PxRect design)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.sizeDelta = new Vector2(LayoutSpace.Px(sd.x), LayoutSpace.Px(sd.y));
            rt.anchoredPosition = new Vector2(LayoutSpace.Px(apos.x), LayoutSpace.Px(apos.y));
            // 🔴 位置**最后写**（上面那几句会按锚点重算 `localPosition`）—— 写的是**轴心**在
            //    设计空间里的那一点（与 `MenuDraw.Local` 同一份换算）。
            float px = design.x1 + pivot.x * design.W;
            float py = design.y2 - pivot.y * design.H;
            go.transform.localPosition = MenuDraw.Local(parent, px, py, px, py);
            return go.transform;
        }

        /// <summary>清空子件 —— 先 `SetActive(false)` 再交给 `MenuDraw.ClearChildren`。
        /// 🔴 **为什么多这一下**：`ClearChildren` 在 Play 下走 `Object.Destroy`（**帧末**才真消失）——
        /// `N23` / `N20③` 那两处是「Show 时按新高度/新宽度重建」，旧块如果还画一帧，
        /// 就会看到「背景先是旧尺寸、下一帧才变」的闪。先关掉 ⇒ 旧块**当帧就不再渲染**。
        /// （批处理下 `ClearChildren` 本来就是 `DestroyImmediate`，加这一句是空操作。）</summary>
        static void ClearChildrenNow(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--) t.GetChild(i).gameObject.SetActive(false);
            MenuDraw.ClearChildren(t);
        }

        /// <summary>九宫格：`border` = 贴图像素四边（L,B,R,T）；`borderOut` = **画出来的角块**
        /// （= border ÷ `m_PixelsPerUnitMultiplier`）。贴图取不到 ⇒ 出声 + 不画（⛔ 不静默）。</summary>
        static void Nine(Transform parent, string art, PxRect r, Vector4 border, float texW, float texH,
                         Vector4 borderOut, int q, Color tint, string name)
        {
            var tex = CardArt.MenuUi(art);
            if (tex == null)
            {
                Debug.LogWarning($"[ToastNotifications] 九宫格底图 `{art}` 取不到 ⇒ `{name}` **没画**（⛔ 不静默）。");
                return;
            }
            MenuDraw.Nine(parent, tex, r, border, texW, texH, q, tint, true, name, borderOut);
        }

        /// <summary>建一段 TMP 文本。`wrap` = 原版 `m_TextWrappingMode == 1`；
        /// `autoMin > 0` = 原版 `m_enableAutoSizing`（`autoMax <= 0` ⇒ 上限 = `fontPx`）。</summary>
        static Label Tmp(Transform parent, PxRect r, string text, Color color, string name, float fontPx,
                         int q, bool wrap, float autoMin, float autoMax, float autoBase,
                         TmpFont.Face face = TmpFont.Face.Cjk)
        {
            var lb = MenuDraw.Text(parent, r, text, color, name, fontPx, q, 0f, 0f, 0f, 0f, face: face);
            if (lb == null) return null;
            if (autoMin > 0f)
            {
                lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMin,
                                 autoMax > 0f ? autoMax : fontPx, autoBase);
                // 🔴 **2026-10-20（W58 · D7）**：这里原来只在 `!wrap` 时调 `SetWrapping(false)`，
                //   `wrap == true` 时**什么都不调** —— 而 `Label.BuildTmp` 的出厂值就是 `NoWrap`
                //   （`Battle/Label.cs` 的 `BuildTmp`：`_tmp.textWrappingMode = NoWrap`）
                //   ⇒ 「自适应 + 折行」那两颗（`Level Up` 的 `Text` / `Player Level Text`）**实际仍是 NoWrap**。
                //   原版两格都是 `1 (Normal)`（`MB_523` / `MB_505`）⇒ **两态都要显式写**。
                lb.SetWrapping(wrap);
            }
            else if (wrap)
            {
                lb.SetWrapWidth(LayoutSpace.Px(r.W));
            }
            return lb;
        }

        /// <summary>量一段占位文案的高度（设计 px）。
        /// 🔴 **2026-10-17（RV·H2）：本值【不参与根高】** —— 只用于给 `Toast` / `Get XP` 的
        /// **文本节点自己那个框**定中线（原版这两颗 Text 的 `sizeDelta.y = 0`，中点钉在图标下沿；
        /// 我们用一个以该中点为心的对称框把 Label 摆对）。根高见 `BuildToast` / `BuildGetXP` 的常量。
        /// ⚠️ 用一颗**屏幕外**的临时标签 —— TMP 在没激活的父件下量不出尺寸
        /// （本仓踩过：`tmpW` 变 4.29e9），所以它建在**活的控制器**下。</summary>
        static float MeasureTextPx(Transform parent, string text, float fontPx, float wrapPx)
        {
            var r = new PxRect(-40000f, -40000f, -40000f + wrapPx, -40000f + 400f);
            var lb = MenuDraw.Text(parent, r, text, Color.white, "__toast_measure", fontPx, QText,
                                   wrapPx, 0f, 0f, 0f);
            if (lb == null) return fontPx * 1.25f;       // 极端兜底（点阵后端）—— 出声
            lb.ForceRelayout();
            float h = PxOf(lb.WorldH);
            Kill(lb.gameObject);
            if (!(h > 0f) || h > 400f)
            {
                Debug.LogWarning($"[ToastNotifications] 量占位文案高度失败（h = {h}）⇒ 退回 `fontPx × 1.25`。");
                return fontPx * 1.25f;
            }
            return h;
        }

        static Label FindLabel(Transform root, string path)
        {
            var t = root.Find(path);
            if (t == null) return null;
            var lb = t.GetComponent<Label>();
            if (lb == null) Debug.LogWarning($"[ToastNotifications] `{path}` 上没有 `Label`（⛔ 不静默）");
            return lb;
        }

        static ImageQuad FindQuad(Transform root, string path)
        {
            var t = root.Find(path);
            if (t == null) return null;
            var q = t.GetComponent<ImageQuad>();
            if (q == null) Debug.LogWarning($"[ToastNotifications] `{path}` 上没有 `ImageQuad`（⛔ 不静默）");
            return q;
        }

        /// <summary>那张缺图的底图名。判据 / 退回理由 → 文件头「边界 2」。</summary>
        const string FancyBackgroundName = "WF_UI_Generic_FancyNotification_Background";

        /// <summary>`Toast Collection Level Up` / `Toast Collection Get XP` 的根底图。
        /// 参数/退回规则**只有一份** → <see cref="BackgroundSpec"/>（这里只负责画）。</summary>
        static void FancyBackground(Transform parent, PxRect r, int q, string name)
        {
            Texture2D tex; Vector4 border, borderOut; float texW, texH;
            BackgroundSpec(true, out tex, out border, out texW, out texH, out borderOut);
            if (tex == null) return;          // 取不到（异常档）⇒ `BackgroundSpec` 的调用方已出声
            MenuDraw.Nine(parent, tex, r, border, texW, texH, q, Color.white, true, name, borderOut);
        }

        /// <summary>原版那张 fancy 底图**本地 Resources 里没有**（只在源图，导入器没导）⇒ 出声一次，
        /// 并由 <see cref="BackgroundSpec"/> 退回同族的 `UI_Deck_Information_submenu_Back_opaque`。
        /// ⛔ 没有把「缺图」写成「原版也这样」。</summary>
        static void WarnFancyBackgroundMissingOnce()
        {
            if (_warnedFancyBg) return;
            _warnedFancyBg = true;
            Debug.LogWarning($"[ToastNotifications] `{FancyBackgroundName}` 在 `Resources/Art/{{ui_menu,ui_deck,ui}}` "
                           + "里**没有**（只在源图 `CardPresentation/Art/原版/0_mainmenu/`，导入器没导）⇒ "
                           + "`Toast Collection Level Up` / `Get XP` 的根底图**退回** "
                           + "`UI_Deck_Information_submenu_Back_opaque`（同族、已导；⛔ 不静默、也⛔ 不写成原版行为）。");
        }
    }
}
