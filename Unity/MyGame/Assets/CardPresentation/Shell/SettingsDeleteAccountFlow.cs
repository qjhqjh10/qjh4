// SettingsDeleteAccountFlow.cs — 原版 `AccountTab.InactivateAccount()` + `AccountTab.DisplayDeleteAccountMessage(...)`
// 的**客户端那一半**（删号确认弹窗）。
//
// ============================ 为什么单开一个文件 ============================
// `SettingsWindow.cs` 里在账里还有 3 条落点（`:6218` / `:6329` / `:6344`）**排在 `SettingsWindow` 类之后**
// （`AutoZoom` / `SuperSampling` / `AcInput` / `MenuInputField` 四个平级类）⇒ 往 `SettingsWindow` 类里**加任何一行**
// 都会把它们整体推走（`check_ledger_points` 报漂移）。本笔按 `CLAUDE.md` 铁律 18·b 的精神（**零行数变化**）
// 把新增逻辑放**新文件**，`SettingsWindow.cs` 里只把那一颗钮的回调**原地换成一行**。
// ⚠️ 本类**不是** `SettingsWindow` 的一部分（没有 partial 化），只吃它的**公开**表面：`Manager` / `AcLocalNote`。
//
// ============================ 出处（第一权威 = 反编译方法体）============================
// · `AccountTab__DisplayDeleteAccountMessage.c`（🥇 `d:/2/tools/decomp_full/`）：
//   · `I2_LocalizationManager__GetTranslation(param_3)` = **确认钮**文案；`param_4` = **取消钮**文案；
//   · 两个 `EverguildButton.SourceDelegate___ctor(...)` 分别绑 `b__0`（带闭包 ⇒ 调 `OnConfirm(true)`）
//     与 `b__27_1`（静态 ⇒ `OnConfirm(false)`）；
//   · `System_String__IsNullOrEmpty(param_4)` 为真 ⇒ **取消钮传 null**（= 单钮那一档）；
//   · 收尾 `WindowsManager__ShowPopUp(text=param_2, …, 确认钮, 取消钮, 0)`。
// · `AccountTab.InactivateAccount()`（🥈 `out_cpp2il_dev_ilspy/AccountTab.cs:540-606`）：
//   `text = string.Format(I2("MainMenu/Notification/DeleteAccountConfirm"), playerName)`；
//   `DisplayDeleteAccountMessage(text, "MainMenu/Settings/ButtonLabel/DeleteAccount",
//                                "MainMenu/Settings/ButtonLabel/Back", ok => { …PlayFab… })`。
//   ⇒ 三个键现读全部在 `Resources/official_loc_13lang.json`（10,862 键）里。
//
// ============================ 我们只做哪一半（如实记）============================
// 🔴 `ok == true` 之后那一段（`ShowPopUp("MainMenu/Notification/DeleteAccountWait")` →
//    `PlayerDataManager.DeactivateAccountProcess` → PlayFab CloudScript → 本地清档 → 退出游戏）
//    **整条都在服务器**（PlayFab 账户域，P3）⇒ 本类**不假装**「正在删除」、也**不**弹
//    `DeleteAccountWait` / `DeleteAccountEndDesc`（那两句是**服务器给的结局**，本地弹出来 = 骗玩家）。
//    ⇒ 确认之后只出一句**本地模拟说明**（`SettingsWindow.AcLocalNote`）+ 一条日志，⛔ 不静默。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>账号页「Delete account」那一颗钮的**客户端流程**（原版 `AccountTab` 的两个方法）。
    /// 宿主 = <see cref="SettingsWindow"/>（本类无实例状态）。</summary>
    public static class SettingsDeleteAccountFlow
    {
        // ---- 官方词条（原版 `Localize.mTerm` 原文；`Resources/official_loc_13lang.json` 现读逐条命中）----
        public const string TermConfirmText = "MainMenu/Notification/DeleteAccountConfirm";
        public const string TermConfirmButton = "MainMenu/Settings/ButtonLabel/DeleteAccount";
        public const string TermCancelButton = "MainMenu/Settings/ButtonLabel/Back";

        /// <summary>原版 `AccountTab.InactivateAccount()` 客户端那一半：算文案 → 交
        /// <see cref="DisplayDeleteAccountMessage"/>。⚠️ 原版是 `string.Format(译文, playerName)`；
        /// 现读该词条**没有 `{0}`**（`string.Format` 对多余实参是空操作）⇒ 我们照抄这个调用形状。</summary>
        public static void InactivateAccount(SettingsWindow win)
        {
            if (win == null) { Debug.LogWarning("[Settings] `InactivateAccount` 收到 null 宿主 ⇒ 什么都没做。"); return; }
            string fmt = Loc.T(TermConfirmText);
            string text = string.Format(fmt, ProfileData.PlayerName);
            DisplayDeleteAccountMessage(win, text, TermConfirmButton, TermCancelButton, ok =>
            {
                if (!ok) return;                       // 取消 ⇒ 什么都不发生（窗已经关掉了，见下）
                Debug.LogWarning("[Settings] 「Delete account」已确认 —— 原版接下来打 PlayFab 删号"
                                 + "（`DeactivateAccountProcess` → CloudScript），**我们在本地模拟**："
                                 + SettingsWindow.AcLocalNote);
                ShowLocalNote(win, "Delete account");
            });
        }

        /// <summary>原版 `AccountTab.DisplayDeleteAccountMessage(confirmationText, confirmationPopupButtonString,
        /// cancelPopupButtonString, OnConfirm)`：两钮消息弹窗（`cancelPopupButtonString` 空 ⇒ **单钮**）。
        /// <para>宿主 = <see cref="WindowsManager.ShowPopUp"/>（= 原版 `WindowsManager.ShowPopUp` 的收编口，
        /// 见 `Shell/WindowsManager.cs` 那条 doc：两钮那一档 = `popupWindowTwoButtons`）。</para>
        /// <para>⚠️ 原版 `/Manager` 为空时**没有这一支**（原版 `SingletonBehaviour.Instance` 恒有）——
        /// 我们这侧建窗可以先于 `Manager` ⇒ **出声**退回页内说明，⛔ 不静默吞掉。</para></summary>
        public static void DisplayDeleteAccountMessage(SettingsWindow win, string confirmationText,
            string confirmButtonTerm, string cancelButtonTerm, System.Action<bool> onConfirm)
        {
            if (win == null) { Debug.LogWarning("[Settings] `DisplayDeleteAccountMessage` 收到 null 宿主。"); return; }
            string okText = string.IsNullOrEmpty(confirmButtonTerm) ? null : Loc.T(confirmButtonTerm);
            // 原版：取消钮的译文串**空** ⇒ 那颗钮根本不给（`ShowPopUp` 的 `secondButton` 传 null）
            string cancelText = string.IsNullOrEmpty(cancelButtonTerm) ? null : Loc.T(cancelButtonTerm);

            var mgr = win.Manager;
            if (mgr == null)
            {
                Debug.LogWarning("[Settings] `DisplayDeleteAccountMessage`：没有 `WindowsManager` ⇒ 弹窗开不出来。");
                ShowLocalNote(win, "Delete account");
                return;
            }
            // 🔴 顺序照 `WindowsManager.ShowPopUp` 的既有约定：**先 Close 再回调**（`onOk` / `onCancel` 各一支）。
            mgr.ShowPopUp(confirmationText, okText,
                          () => { if (onConfirm != null) onConfirm(true); },
                          cancelText,
                          cancelText == null ? (System.Action)null
                                             : (System.Action)(() => { if (onConfirm != null) onConfirm(false); }));
        }

        /// <summary>`Manager` 不可用 / 删号动作是服务器那半 ⇒ 出一句**本地模拟说明**的弹窗（用户看得见），
        /// 同时写一条 warning（日志里也看得见）。文案 = `SettingsWindow.AcLocalNote`（**只有一份**，⛔ 别抄第二份）。</summary>
        static void ShowLocalNote(SettingsWindow win, string what)
        {
            string note = what + " " + SettingsWindow.AcLocalNote;
            if (win.Manager != null) win.Manager.ShowPopUp(note, Loc.T("MainMenu/General/OK"), null);
            else Debug.LogWarning("[Settings] " + note);
        }
    }
}
