// FastModeCollectPopup.cs — 2026-11-05 · `B143-W5` · 活状态行 `B143-CHK05-TRYCOLLECT-POPUP`
// ============================ 它补的是什么 ============================
//  原版 `FastModeBaseEvent<TS,TD>.TryCollect` 在打云脚本**之前**先弹一个「正在领取」模态，
//  成功支的**第一句**再把它关掉 —— 我们原来同步落地、没有那扇窗。
//
// ============================ 判据（一手，2026-11-05 现读）============================
//  · `out_cpp2il_dev_ilspy/FastModeBaseEvent.cs:1794`（**真体**）里的 `:1876`：
//      `instance.ShowPopUp("Missions/CollectingRewards", localizeTexts: true, closeOnEsc: false, primaryButton, secondButton);`
//    —— 文案是 **I2 术语键**（不是明文）、`localizeTexts: true`、`closeOnEsc: false`。
//  · 成功支 = lambda `_003CTryCollect_003Eg__OnCollectSuccess_007C2`（`decomp_full/…_g__OnCollectSuccess_2.c`）：
//    **第一句 `WindowsManager.HidePopUp(instance, 1, 0)`** → 再 `RewardService.Collect(…)` → 再 `GameAnalytics.RecordTransaction(0x5d,…)`
//    ⇒ **先关窗、后发奖**（顺序是判据，⛔ 别反）。
//  · 失败支 = lambda `_003CTryCollect_003Eb__20_1`（`decomp_full/FastModeBaseEvent.__c_object__object____TryCollect_b__20_1.c`）：
//    `ErrorPopupWindowController.ShowErrorMessage(0xb4 = 180, …)` ⇒ 我们 `CustomError.GenericConnectionError = 180`（`Shell/ErrorPopupWindowController.cs:50`）已对号；
//    ⚠️ 失败支**不**调 `HidePopUp`（原版就这样）⇒ 那扇「正在领取」窗留着，由错误窗盖住。
//  · I2 键 `Missions/CollectingRewards` **在官方本地化表里**（命中 1 处）⇒ 不是我们自造的词条。
//  · 本仓宿主 = `WindowsManager.ShowMessagePopUp(messageKey, …)`（= 原版 `MessagePopupWindow` / 类 `PopUpGameWindow`，
//    2026-10-12 `A416` 收编；它吃**术语键** ⇒ 与 `localizeTexts: true` 同义）。
//
// ============================ ⚠️ 三处判据**未闭合**（⛔ 别编版式）============================
//  ① `_003CTryCollect_003Eb__3` 的体读不出来（`decomp_full` 里那份**文件名与内容不符**：首行是
//     `UnityEngine_UIElements_SortColumnDescriptions__OnDescriptionChanged`，体里 jumptable 失败）——
//     只能按签名 `(IReadOnlyList<RewardInfo>)` 判成「拿到奖励列表之后那一跳」，本仓等价物 = `OnRewardsCollected`。
//  ② 原版 `ShowPopUp` 的两个按钮实参在 cpp2il 里是两个 `default` ⇒ **有没有 OK 钮没钉死**；
//     我们这一版按「无钮」（`primaryKey/secondaryKey = null`）落，并如实记。
//  ③ 错误码 180 的**文案**（`CustomErrors/*` 的哪一条）没逐条核；本地失败支结构性不可达（存档落盘才失败）。
//
// ============================ 自检读口 ============================
//  `SequenceForTest` 按**发生顺序**记 token（`show` / `hide` / `rewards`）⇒ 断言能钉「先关后发」；
//  本类**不依赖** `WindowsManager` 存在（批处理里常常没有）：拿不到实例 ⇒ **仍然记 token**，只少一次真实弹窗。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>「正在领取」弹窗的三个动作 + 顺序读口。**非泛型**（原版那个方法在泛型类里，
    /// 但自检不该为了读一个 token 去构造泛型实参）。</summary>
    public static class FastModeCollectPopup
    {
        /// <summary>原版 `TryCollect` 里那句 `ShowPopUp` 的**术语键**（`out_cpp2il_dev_ilspy/FastModeBaseEvent.cs:1876`）。
        /// ⛔ 别改成明文：官方表里它是 I2 键（`Missions/CollectingRewards`）。</summary>
        public const string Term = "Missions/CollectingRewards";

        static readonly List<string> _seq = new List<string>();

        /// <summary>自检读口：按发生顺序拼起来的 token 串（`show,hide,rewards`）。
        /// 期望（成功支）= **`show,hide,rewards`**（= 原版「先关后发」）；🧨 把两句对调 ⇒ `show,rewards,hide`。</summary>
        public static string SequenceForTest { get { return string.Join(",", _seq.ToArray()); } }

        /// <summary>自检用：清空顺序记录（⛔ 不动任何生产状态）。</summary>
        public static void ResetForTest() { _seq.Clear(); }

        /// <summary>= 原版 `TryCollect` 进云调用**之前**那句 `ShowPopUp(...)`。
        /// 文案键 + `closeOnEsc: false` 照原版；两个按钮实参原版是 `default` ⇒ 这里传 `null`（见文件头 ②）。</summary>
        public static void Show()
        {
            _seq.Add("show");
            var wm = WindowsManager.Instance;
            if (wm == null)
            {
                // ⛔ 不静默：原版没有「没有 WindowsManager」这条分支（批处理/单测里才会有）
                Debug.LogWarning("[FastMode] 领取奖励：`WindowsManager.Instance` 取不到 ⇒ 「正在领取」弹窗这一跳**没弹**"
                               + "（顺序 token 仍记了 `show`，别把它当成「弹窗真的出现过」）");
                return;
            }
            wm.ShowMessagePopUp(Term, null, null, null, null, closeOnEsc: false);
        }

        /// <summary>= 原版 `g__OnCollectSuccess_2` 的**第一句** `WindowsManager.HidePopUp(...)`。
        /// 🔴 必须在发奖**之前**调（判据见文件头）。</summary>
        public static void Hide()
        {
            _seq.Add("hide");
            var wm = WindowsManager.Instance;
            if (wm == null) return;
            wm.HidePopUp();
        }

        /// <summary>= 原版成功支「发完奖」那一步之后的顺序标记（原版是 `RewardService.Collect(...)`，
        /// 我们本地等价 = 宿主把奖励交出去）。**只记 token**，不碰任何状态。</summary>
        public static void MarkRewarded() { _seq.Add("rewards"); }
    }
}
