// B94B5_Assert.cs — 商店族（`账/商店.tsv`）第九十四会话 · 波 2 `B94_B5` 的断言
//
// ============================ 它盯的是什么 ============================
// 本波在 `Shell/` 里**真改的**只有两处（其余 24 条是回源逐句复核 + 状态升级，账见
// `资料/完全复刻/报告/B94_B5.md`，⛔ 别在这里抄第二份）：
//   A `CampaignTab.ApplyPremiumState` 的**越界早退**（账 `商店:36`）—— 原版
//     `CampaignPremiumPointsPanel__ChangeState.c:11-13`：第 1 句先写 `currentState`(+0xa8)，
//     然后 `if (param_2 != 2) { if (param_2 != 1) { if (param_2 != 0) return; … } }`
//     ⇒ **0/1/2 之外一件都不动**（原来我们把它当成「非 state 2」⇒ 会换底图 + 关扫光）。
//   B `GachaEventWindow.OnCratePurchaseComplete` + `CratePurchaseHook`（账 `商店:102` / `商店:98`）——
//     原版 `<OpenCrateButtonClick>b__14_0`（服务器回包那两句：`crateRewards = rewards; crateOpenned = true;`）。
//
// ============================ 怎么用（宿主）============================
// `B94B5_Assert.Run();`
//   + 宿主里两行（照 `ShopFamilyAssertB93S1` / `ShopFamilyAssertB94S1` 的先例）：
//     `_sink.Pass += B94B5_Assert.Pass; _sink.Fail += B94B5_Assert.Fail;`
//   · **不跑 Unity 批处理**、不建持久资产；临时结点在收尾 `DestroyImmediate`。
//   · 不依赖 `WindowsManager`（两个被测组件都能裸 `AddComponent` + `Build()` 建树）。
//
// ============================ 🔴 灭自证（两态 / 翻面法写进文案）============================
//   · A5：先 `ApplyPremiumState(2)`（= Silver）再 `ApplyPremiumState(7)`（越界）——
//         **有那道早退 ⇒ 仍是 Silver**；把闸删掉 ⇒ 越界落进「state != 2」那一档 ⇒ Gold ⇒ A5 红。
//   · A6：再把 0 打一次 ⇒ Gold —— 保证那道闸**没有**把正常的 0/1 两档一起挡掉（防「一刀切早退」）。
//   · B9/B10：`OnCratePurchaseComplete` 只写 `crateRewards`、漏写 `crateOpened` ⇒ 状态机永远停在
//         state 1 ⇒ B10 红；反过来把两句顺序写反不影响这两条，但 B8 之前的 `CrateOpened == false` 兜住「提前置真」。
//   · B2 / B5b–B5d（🆕 2026-10-29 执行代理 `B97_SHOP2` 改写）：`BuildPage` 装了默认 hook（🔶 本地开箱奖励表）⇒
//         「收摊」那一支改用**显式置 null** 造状态。删掉 `BuildPage` 末尾那个 `InstallLocalCratePurchaseHook()` ⇒ B2 红；
//         把默认 hook 改成「只出声、不回调 `onComplete`」⇒ B5c/B5d 红（`crateOpened` 不会变真）。
using CardPresentation;   // `GachaEventWindow` / `CampaignTab` / `CampaignData` 都在这个命名空间里
using UnityEngine;

/// <summary>执行代理 `B94_B5`：商店线波 2 的两处代码改动（Premium 三态越界早退 / 开箱回包回调）。
/// <para>⛔ **期望值全是字面量**（`2` / `7` / `Silver` / `true` / `false`），**不从被测实现里读** ——
/// 否则就是「拿我们的实现证明我们的实现」。</para></summary>
public static class B94B5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B5] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_B5 商店线波 2：Premium 三态越界早退 / 开箱回包回调");
        SectionA_越界早退();
        SectionB_开箱回包回调();
    }

    // ------------------------------------------------------------ A `ChangeState` 的 0/1/2 之外早退
    static void SectionA_越界早退()
    {
        MenuCheck.Section(_sink, "A `CampaignPremiumPointsPanel.ChangeState`：0/1/2 之外只写 `currentState`、一件都不动");

        var go = new GameObject("B5_Campaign");
        var tab = go.AddComponent<CampaignTab>();
        try
        {
            // 前提（硬检查）：裸组件还没跑过 `BuildPremiumPanel` ⇒ 读口是 -1（不是 0/1/2 里的任何一个）
            C(tab.PremiumPanelState, -1, "A1 前提：裸组件还没 `BuildPremiumPanel` ⇒ `_premState = -1`");

            tab.ApplyPremiumState(2);
            C(tab.PremiumPanelState, 2, "A2 ★ `ApplyPremiumState(2)` ⇒ `currentState = 2`（`AlreadyCollected`）");
            C(tab.PremiumBackgroundArt, CampaignTab.PremBgSilver,
              "A3 ★ 同档 ⇒ 底图 = `collectedSprite`（Silver）—— 与 `ShopFamilyAssertB93S1` B6 同一个口，本条只做后续两条的前提");

            tab.ApplyPremiumState(7);
            C(tab.PremiumPanelState, 7,
              "A4 ★★ 越界 state 7 ⇒ **`currentState` 照样被写**（原版第 1 句 `*(int*)(this+0xa8) = param_2` 在分支之前）");
            C(tab.PremiumBackgroundArt, CampaignTab.PremBgSilver,
              "A5 ★★ 越界 ⇒ 底图**不动**（原版 `if (param_2 != 2) return;` 早于 `set_sprite`）。🧨 删掉那道早退 ⇒ 这里变 Gold ⇒ 红");

            tab.ApplyPremiumState(0);
            C(tab.PremiumBackgroundArt, CampaignTab.PremBgGold,
              "A6 ★ 两态对照：`ApplyPremiumState(0)` ⇒ Gold（防「一刀切早退」把正常 0/1 两档也挡掉）");
            tab.ApplyPremiumState(1);
            C(tab.PremiumPanelState, 1, "A7 两态对照：state 1 ⇒ 正常落档（`PremiumNotPurchased`）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ------------------------------------------------------------ B 开箱回包回调（`b__14_0` 的两句）
    static void SectionB_开箱回包回调()
    {
        MenuCheck.Section(_sink, "B `GachaCratePanel.<OpenCrateButtonClick>b__14_0`：回包到 ⇒ `crateRewards` + `crateOpenned`");

        var go = new GameObject("B5_Gacha");
        var win = go.AddComponent<GachaEventWindow>();
        try
        {
            bool built = false;
            try { win.Build(); built = true; }
            catch (System.Exception e)
            {
                Tr(false, "B0 前提：`GachaEventWindow.Build()` 不能抛（实际抛了 " + e.GetType().Name + "：" + e.Message + "）");
            }
            if (!built) return;

            C(win.CrateOpened, false, "B1 前提：还没开箱 ⇒ `crateOpened = false`");
            // 🔴 2026-10-29（执行代理 `B97_SHOP2` · 账 `商店:102`）**就地改写**：`BuildPage` 现在**装默认 hook**
            //   （🔶 本地开箱奖励表，调度台 2026-10-29 裁定）⇒ 改前那四条「生产路径 hook 恒 null ⇒ 收摊」已过期。
            //   现在改成**两态对照**：① 显式把 hook 置 null ⇒ 「本地没有 `CurrentOffer`」那一支仍在（B3–B5）；
            //   ② 恢复默认 hook ⇒ 整条链真的走完（B5b–B5d）。下面 B6 起照旧（自检注入的 hook 优先级更高）。
            Tr(win.CratePurchaseHook != null,
               "B2 ★★ `Build()` 装上了**默认 hook**（= 原版 `CurrentOffer.TryPurchaseOffer` 那一跳的 🔶 本地替身）"
             + "｜🧨 把 `BuildPage` 末尾那个 `InstallLocalCratePurchaseHook()` 删掉 ⇒ 本条红（改前这里是「恒 null」）");
            win.CratePurchaseHook = null;                       // 造出「本地没有 `CurrentOffer`」那一档
            Tr(win.OpenCrateButtonClick() == false,
               "B3 ★ 没有 hook（= 本地没有 `CurrentOffer`）⇒ 返回 false（收摊，⛔ 不假装开箱）");
            C(win.CrateOpened, false, "B4 同上一档：`crateOpened` 仍是 false");
            C(win.CrateFlowState, -1, "B5 同上一档：**没有**起 `CrateAnimationFinished`（原版第 ⑥ 句只在成功时 StartCoroutine）");

            Tr(win.InstallLocalCratePurchaseHook(true),
               "B5b 两态对照：`InstallLocalCratePurchaseHook(replace: true)` ⇒ 真装上了（返回 true）");
            Tr(win.OpenCrateButtonClick(),
               "B5c ★★ 有默认 hook ⇒ 第 ⑤ 句「发出去了」⇒ 返回 true（改前这一条恒 false）");
            C(win.CrateOpened, true,
               "B5d ★★ 默认 hook **同步**回包（🔶 本地表）⇒ `crateOpened = true`（原版第 2 句）");

            System.Action<CampaignData.RewardSpec[]> captured = null;
            win.CratePurchaseHook = cb => captured = cb;
            Tr(win.OpenCrateButtonClick(),
               "B6 ★★ 有 hook ⇒ 第 ⑤ 句算「发出去了」⇒ 返回 true（走原版链）");
            Tr(captured != null, "B7 ★★ 前提：hook 拿到的 `onComplete` 就是那个回调（不是空参）");
            C(win.CrateFlowState, 0, "B8 第 ⑥ 句起了状态机（state 0）");

            if (captured != null) captured(new CampaignData.RewardSpec[0]);
            C(win.CrateOpened, true,
              "B9 ★★ 回包回调 ⇒ `crateOpened = true`（原版第 2 句 `crateOpenned = 1`）。🧨 只写 `crateRewards` 漏写这一格 ⇒ 本条红");

            Tr(win.CrateFlowStep(1000f), "B10 ★ 回包到了 ⇒ 推足时长（`clip.length × 0.85`）⇒ 状态机推到终点（`gacha_crate_opening.mp4` 已导入 ⇒ state 0 有等待，`0f` 合法地推不动）");
            C(win.CrateFlowState, -1, "B11 ★ 终点：状态机复位（原版 state 2 收尾）");
            C(win.VideoBackgroundActive, false, "B12 收尾第 3 句：`videoBackground.SetActive(false)`");
        }
        finally
        {
            try { PointerLayer.SetInputEnabled(true); } catch { }   // 第 ④ 句在 B6 那一档把输入关了 ⇒ 无论成败都还回去
            Object.DestroyImmediate(go);
        }
    }
}
