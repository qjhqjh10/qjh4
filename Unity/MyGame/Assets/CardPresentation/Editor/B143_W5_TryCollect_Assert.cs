// B143_W5_TryCollect_Assert.cs — 2026-11-05 · `B143-W5` · `B143-CHK05-TRYCOLLECT-POPUP`
//
// ============================ 它盯的是什么 ============================
//  原版 `FastModeBaseEvent.TryCollect` 的三段（判据 → `判据/B143_TryCollect判据.md`）：
//    ① 打云脚本**之前** `ShowPopUp("Missions/CollectingRewards", localizeTexts:true, closeOnEsc:false, …)`；
//    ② 成功支**第一句** `WindowsManager.HidePopUp` → 再 `RewardService.Collect`（**先关窗、后发奖**）；
//    ③ 失败支 `ShowErrorMessage(0xb4 = 180)`（且**不**关窗）。
//  我们的落点 = `Shell/FastModeBaseEvent.cs:438`（show）· `:457`（hide→发奖）· 失败支原 `:449`；
//  执行体 = `Shell/FastModeCollectPopup.cs`。
//
// ============================ 逐条为什么必须这么断 ============================
//  A1 术语键逐字 = `Missions/CollectingRewards`（**I2 键**，官方表命中 1 处；⛔ 不是明文）。
//  A2 宿主调用真的带 `closeOnEsc: false`（原版那个实参）；只在 doc 里写不算。
//  A3 ★**顺序**★：源码里 `FastModeCollectPopup.Show()` 出现在 `Hide()` 之前、`Hide()` 出现在
//     `OnRewardsCollected(collect)` 之前 ⇒ 钉住原版「先关后发」。
//     🧨 把 `:457` 那两个调换 ⇒ 本红；把 show 挪到发奖之后 ⇒ 本红。
//  A4 失败码 = `CustomError.GenericConnectionError`，且它 == **180**（原版 `0xb4`）。
//  A5 失败支**不**调 `FastModeCollectPopup.Hide()`（原版 lambda `b__20_1` 只弹错误窗）。
//  A6 顺序读口可用（非泛型、可清空）—— 供将来真跑一次 `TryCollect` 的宿主直接用。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主里同一物理行追加（`Editor/ShellScene.cs` 或 `Editor/RewardsScene.cs` 的汇总行）：
//      B143_W5_TryCollect_Assert.Run(); _sink.Pass += B143_W5_TryCollect_Assert.Pass; _sink.Fail += B143_W5_TryCollect_Assert.Fail;
//  ⚠️ **不需要建夹具**：本段只读源码文本 + 常量，不 `Show()`、不碰 `WindowsManager`。
//  判绿红：按行首 `B143W5TC ✓` / `B143W5TC ✗` 数（⛔ 别 `grep -c ✗`）。
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B143-W5`：`TryCollect` 的「正在领取」弹窗三段（show → hide → 发奖）+ 失败码 180。</summary>
public static class B143_W5_TryCollect_Assert
{
    const string Tag = "[B143W5TC]";
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    /// <summary>跑全部（纯文本 + 常量，可在任意宿主里跑）。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log(Tag + " ==== `B143-CHK05-TRYCOLLECT-POPUP`：正在领取弹窗 + 先关后发 + 失败码 180 ====");

        // ---- A1：术语键 ----
        Check(FastModeCollectPopup.Term == "Missions/CollectingRewards",
              "A1 ★ 弹窗文案键 = **`Missions/CollectingRewards`**（原版 `FastModeBaseEvent.cs:1876` 那个 I2 键；实得「"
            + FastModeCollectPopup.Term + "」）");

        // ---- 源码读取（两个文件）----
        string shellDir = Application.dataPath + "/CardPresentation/Shell/";
        string pPopup = shellDir + "FastModeCollectPopup.cs";
        string pEvent = shellDir + "FastModeBaseEvent.cs";
        Check(File.Exists(pPopup), "A0①（前提）读得到 `Shell/FastModeCollectPopup.cs`（" + pPopup + "）");
        Check(File.Exists(pEvent), "A0②（前提）读得到 `Shell/FastModeBaseEvent.cs`（" + pEvent + "）");
        if (!File.Exists(pPopup) || !File.Exists(pEvent)) { Debug.Log(Tag + " ==== 合计：" + _pass + " 通过 / " + _fail + " 失败 ===="); return; }
        string popupSrc = File.ReadAllText(pPopup);
        string evtSrc = File.ReadAllText(pEvent);

        // ---- A2：宿主调用带 closeOnEsc:false ----
        Check(popupSrc.Contains("ShowMessagePopUp(Term") && popupSrc.Contains("closeOnEsc: false"),
              "A2 ★ 弹窗走 `WindowsManager.ShowMessagePopUp(Term, …, closeOnEsc: false)`（原版那个实参；🧨 删掉 `closeOnEsc: false` ⇒ 本红）");

        // ---- A3：顺序 show → hide → 发奖 ----
        int iShow = evtSrc.IndexOf("FastModeCollectPopup.Show()", System.StringComparison.Ordinal);
        int iHide = evtSrc.IndexOf("FastModeCollectPopup.Hide()", System.StringComparison.Ordinal);
        int iRew = evtSrc.IndexOf("OnRewardsCollected(collect)", System.StringComparison.Ordinal);
        Check(iShow >= 0 && iHide >= 0 && iRew >= 0,
              "A3①（前提）三处调用都在源码里（show=" + iShow + " hide=" + iHide + " rewards=" + iRew + "）｜⛔ 缺一处 ⇒ 下面那条不成立");
        Check(iShow >= 0 && iHide >= 0 && iRew >= 0 && iShow < iHide && iHide < iRew,
              "A3 ★★ **先弹 → 先关 → 再发奖**（`Show` < `Hide` < `OnRewardsCollected`）—— 原版 `g__OnCollectSuccess_2` 的第一句就是 `HidePopUp`"
            + "｜🧨 把 `:457` 那两句对调（先发奖后关窗）⇒ 本红；把 `Show()` 挪到发奖之后 ⇒ 本红");

        // ---- A4：失败码 180 ----
        Check((int)CustomError.GenericConnectionError == 180,
              "A4 ★ 失败支错误码 = `CustomError.GenericConnectionError` == **180**（= 原版 `0xb4`；实得 "
            + ((int)CustomError.GenericConnectionError) + "）");
        Check(evtSrc.Contains("ShowErrorMessage(CustomError.GenericConnectionError)"),
              "A4' 失败支真的写了这句（🧨 换成别的错误码 ⇒ 本红）");

        // ---- A5：失败支不关窗 ----
        int iErr = evtSrc.IndexOf("ShowErrorMessage(CustomError.GenericConnectionError)", System.StringComparison.Ordinal);
        Check(iErr >= 0 && !LineOf(evtSrc, iErr).Contains("FastModeCollectPopup.Hide()"),
              "A5 失败支**不**调 `FastModeCollectPopup.Hide()`（原版 lambda `b__20_1` 只弹错误窗、不关「正在领取」那扇）｜🧨 在失败那行补一句 `Hide()` ⇒ 本红");

        // ---- A6：顺序读口可用 ----
        FastModeCollectPopup.ResetForTest();
        Check(FastModeCollectPopup.SequenceForTest == "",
              "A6 顺序读口可清空（`SequenceForTest` 为空；供将来真跑 `TryCollect` 的宿主直接断 `show,hide,rewards`）");

        Debug.Log(Tag + " ==== 合计：" + _pass + " 通过 / " + _fail + " 失败 ====");
    }

    /// <summary>取 `idx` 所在的那一整行（源码文本断言用；越界 ⇒ 空串）。</summary>
    static string LineOf(string s, int idx)
    {
        if (string.IsNullOrEmpty(s) || idx < 0 || idx >= s.Length) return "";
        int a = s.LastIndexOf('\n', idx); a = a < 0 ? 0 : a + 1;
        int b = s.IndexOf('\n', idx); if (b < 0) b = s.Length;
        return s.Substring(a, b - a);
    }
}
