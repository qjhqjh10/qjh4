// ErrorPopupWindowController.cs — 原版同名静态类（账 157）· 2026-10-27 第四批（B88-E9d）
//
// ============================ 判据（唯一正本） ============================
// `out_cpp2il_dev_ilspy/ErrorPopupWindowController.cs:31-59`（`ShowErrorMessage` 全 60 行）+ `CustomError.cs`（枚举 29 值）。
// 原版体逐句：
//   ① `CustomDebug.LogWarning("Display Custom Error " + customError.ToString())`
//   ② `term = "CustomErrors/" + customError.ToString()`
//   ③ `text = LocalizationManager.GetTranslation(term)`
//   ④ `cloudscriptTrace` 非空 ⇒ `text = GetTranslation("CustomErrors/ErrorCloudscript") + "\n<size=3>(" + trace + ")"`
//   ⑤ `okText = GetTranslation("MenuLogin/Intro/Retry")`
//   ⑥ `WindowsManager.ShowPopUp(text, localizeTexts: false, closeOnEsc: false, okText, actionAfterError)`
//
// 🔴 **本类是一个新文件**（原版这个类在我们仓里一直没有落地）：`账 157` 的落点就是这里。
// ⚠️ **词条缺口（实读，2026-10-27）**：`CustomErrors/*` 在官方本地化表里 **27/30 命中**，
//    缺 `CustomErrors/Undefined` · `CustomErrors/CouponCodeNotFound` · `CustomErrors/CouponAlreadyRedeemed`
//    （判据 = `Resources/official_loc_13lang.json` 的 `keys` 逐个查）。缺的那几条**⛔ 不印键名**：
//    回落到 `CustomErrors/UnhandledError`（表里有）**并出声**写明是哪一条缺。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `CustomError`（判据 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CustomError.cs`，**29 个值逐条照抄**）。
    /// 🔴 **值不连续**（5 / 10 / 20 … / 195 / 200 / 201 / 202 / 203）—— ⛔ 别按 0..n 重排：
    /// 原版这一列会被 `"CustomErrors/" + ToString()` 用来拼词条键，也会被序列化到存档/日志里。</summary>
    public enum CustomError
    {
        Undefined = 0,
        UnhandledError = 5,
        InternetUnreachable = 10,
        CantLoadMainMenuObjects = 20,
        ExcededMaxSessionLength = 30,
        InsuffientTickets = 40,
        ErrorDataSync = 50,
        ErrorItemsNotGranted = 60,
        GuildPermissionError = 70,
        PlayerCantBeInvitedFullGuild = 80,
        PlayerCantBeInvitedAlreadyInGuild = 81,
        PlayerCantBeInvitedNotAccepting = 82,
        PlayerCantBeInvitedAlreadyInvited = 83,
        PendingMatch = 90,
        CloudscriptCallError = 100,
        ErrorCurrentEventOnTime = 110,
        MissingEventData = 120,
        MaxPinnedMatches = 130,
        SteamInitError = 140,
        ErrorFailedSubscription = 150,
        ErrorLoadReplay = 160,
        ErrorLoadDeck = 170,
        GenericConnectionError = 180,
        CouponCodeNotFound = 190,
        CouponAlreadyRedeemed = 191,
        RewardsAlreadyClaimed = 195,
        Unauthorized = 200,
        TwitchAccountNotLinked = 201,
        CantReferSelf = 202,
        PlayerNotFound = 203,
    }

    /// <summary>原版 `ErrorPopupWindowController`（**静态类**，判据见文件头）。
    /// <para>⚠️ 与原版的差异（如实记，⛔ 不写成原版行为）：
    /// ① 原版第 ⑥ 步走 `WindowsManager.Instance.ShowPopUp(...)`；我们**一样**走它（`Shell/WindowsManager.cs` 的
    /// `ShowPopUp(string, string, Action, string, Action)`，A416 起它就是原版那扇 `MessagePopupWindow` 的宿主）
    /// —— 只是取实例的方式是 `WindowsManager.Instance`（可能 null ⇒ **出声 + 直接跑回调**，不静默）；
    /// ② 原版 `ShowPopUp` 的第 2 实参是 `localizeTexts: false`（文本**已经**翻译好了）⇒ 我们传的是**译文**，
    /// 与 `ShowPopUp` 的「查不到词条就原样返回」口径一致。</para></summary>
    public static class ErrorPopupWindowController
    {
        /// <summary>词条前缀（原版 `"CustomErrors/" + name`）。</summary>
        public const string TermPrefix = "CustomErrors/";
        /// <summary>`cloudscriptTrace` 那一行的词条（原版第 ④ 步）。</summary>
        public const string TermCloudscript = "CustomErrors/ErrorCloudscript";
        /// <summary>OK 钮的词条（原版第 ⑤ 步）。</summary>
        public const string TermRetry = "MenuLogin/Intro/Retry";
        /// <summary>缺词条时的回落（表里有 —— 见文件头「词条缺口」）。</summary>
        public const string TermFallback = "CustomErrors/UnhandledError";

        /// <summary>`CustomError.X` ⇒ `"CustomErrors/X"`（原版第 ② 步）。</summary>
        public static string TermOf(CustomError customError) { return TermPrefix + customError.ToString(); }

        /// <summary>这一条在词条表里吗（自检/调用方可以先用它判断，免得印键名）。</summary>
        public static bool HasTerm(CustomError customError) { return Loc.HasEntry(TermOf(customError)); }

        /// <summary>= 原版 `ErrorPopupWindowController.ShowErrorMessage(CustomError, Action, string)`（**逐句**，判据见文件头）。
        /// <para>`actionAfterError` = 原版 `ShowPopUp` 那颗 OK 钮的回调（原版注释里叫 Retry）。</para></summary>
        public static void ShowErrorMessage(CustomError customError, Action actionAfterError = null,
                                           string cloudscriptTrace = "")
        {
            // ① 出声（原版 `CustomDebug.LogWarning("Display Custom Error " + name)`）
            Debug.LogWarning("[Error] Display Custom Error " + customError);

            // ②③ 取词条 —— 缺键时**回落 + 出声**（⛔ 不印 `CustomErrors/xxx` 键名）
            string term = TermOf(customError);
            string text;
            if (Loc.HasEntry(term)) text = Loc.T(term);
            else
            {
                Debug.LogWarning("[Error] 词条 `" + term + "` 在本地化表里**没有** ⇒ 回落到 `" + TermFallback
                               + "`（原版这里会印空/键名；我们出声并给一条通用错误文案）");
                text = Loc.HasEntry(TermFallback) ? Loc.T(TermFallback) : "";
            }

            // ④ cloudscriptTrace 非空 ⇒ 追加一行（原版 `"\n<size=3>(" + trace + ")"`）
            if (!string.IsNullOrEmpty(cloudscriptTrace))
                text = Loc.T(TermCloudscript) + "\n<size=3>(" + cloudscriptTrace + ")";

            // ⑤ OK 钮文案
            string okText = Loc.T(TermRetry);

            // ⑥ 弹窗（原版 `WindowsManager.ShowPopUp(text, localizeTexts:false, closeOnEsc:false, okText, actionAfterError)`）
            var wm = WindowsManager.Instance;
            if (wm != null)
            {
                wm.ShowPopUp(text, okText, actionAfterError);
                return;
            }
            // 没有管理器 ⇒ **出声 + 照跑回调**（⛔ 不静默吞掉「错误已经发生」这件事）
            Debug.LogWarning("[Error] `WindowsManager.Instance` 是 null ⇒ 错误弹窗**没弹出来**，文案 = `" + text + "`");
            if (actionAfterError != null) actionAfterError();
        }

        /// <summary>= 原版 `UIMessageController.ShowError(string, bool)` 的**等价口**（账 157 之外的顺手口，
        /// 消费者 = `DuelPopupWindow.CanPlay` 的「没这个模式的卡组」那一支）。
        /// <para>⚠️ 原版那是**内联错误条**（`UIErrorMessageItem`，5 条轮转 + 带颜色），我们**没有**那套 toast 组件
        /// ⇒ 等价物 = 「把同一句话交给我们唯一那条消息弹窗」（`WindowsManager.ShowPopUp`），并保留
        /// `localize` 这一档（真 ⇒ 先过 `Loc`）。**这不是缺功能**，是同一件事换了载体（如实记）。</para></summary>
        public static void ShowError(string termOrText, bool localize)
        {
            string text = localize ? Loc.T(termOrText) : termOrText;
            if (!localize && Loc.HasEntry(termOrText)) text = Loc.T(termOrText);   // 原版 `localize=false` 时是明文
            var wm = WindowsManager.Instance;
            if (wm != null) { wm.ShowPopUp(text, Loc.T(TermRetry), null); return; }
            Debug.LogWarning("[Error] `ShowError` 没有弹窗宿主 ⇒ 只出声：`" + text + "`（原版 `UIMessageController.ShowError`）");
        }
    }
}
