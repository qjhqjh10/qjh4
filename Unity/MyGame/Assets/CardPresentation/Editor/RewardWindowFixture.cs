// RewardWindowFixture.cs — 「领奖窗」那几条**夹具把手**的**共用件**（🆕 2026-10-12 · **A450**）
//
// 🔴 **为什么单开一个文件**：这四条原来是 `Editor/RewardsScene.cs` 里的 `static`**私有**方法，
//    于是**别的自检宿主用不了** —— `Editor/ShopScene.cs` 那 5 条 A439 断言（买「商品档」⇒ 弹领奖窗）
//    要的就是它们（`资料/普查产出_1012/H15_战役节点与商店购买.md` §五·2）：
//    「要么在 ShopScene 里照抄一份（**两处写同一条规则 = 迟早不一致**），要么放共用位置」。
//    ⇒ 按后者办（本仓纪律：判据要共用一份）。
// ⚠️ **`Editor/MenuCheck.cs` 这个名字不能用**：那一笔账（`资料/阶段二_卡组线_原版规格.md:157`）
//    要收的是**另一个族**（五个宿主各自一份的 `Check` / `FindChild` / `Shoot`），⛔ 不是这四条；
//    拿它当落点会把这四条钉在一个名字下、将来收 `Check` 族时又得搬一次。
// ⚠️ **只搬了三条纯判据**：`ClickCollectAndDismiss`（「点一颗领奖钮」）**留在 `RewardsScene`** ——
//    它要靠那个宿主的 `Check` / `CheckTrue` / `ClickButtonByQuad`（前两个是**逐宿主**的计数器：
//    拿别处的 `Check` 去断，失败会**记进别人的合计**里 ⇒ 静默）。
//
// 📌 调用方：`Editor/RewardsScene.cs`（原主）· `Editor/ShopScene.cs`（A439 那 5 条）。
// ⚠️ 本文件里的方法全是**只读/关窗**，不含断言 ⇒ 谁调都不会把失败记到别人账上。

using CardPresentation;
using UnityEngine;

public static class RewardWindowFixture
{
    /// <summary>场上**还开着**的 `Reward Window`（没有 ⇒ `null`）。
    /// <para>「开着」的判据 = **`CurrentState != Closed`**，⛔ 不是「实例在不在」——
    /// `GameWindow.Close()` 只 `SetActive(false)`、实例留在锚点下（老账 A123），本工程自己还会**复用**旧实例
    /// （`Shell/RewardWindow.cs:488-498`）。</para>
    /// <para>🔴 **扫【全部】 `WindowsManager`**（不是只扫某一台）：`Build()` 里那句
    /// `WindowsManager.EnsureHost()` 一旦被换回手抄那一份，领奖窗就会落到**第二台**管理器上 ——
    /// DIAG-B §六·1/#1 那 8 条红就是这么发生的（旧写法下本宿主的 `wm2.openWindows` 里根本没有它，
    /// 于是既关不掉、也断不到）。</para></summary>
    public static RewardWindow FindOpenRewardWindow()
    {
        foreach (var w in Object.FindObjectsByType<WindowsManager>(FindObjectsSortMode.None))
            for (int i = 0; i < w.openWindows.Count; i++)
            {
                var rw = w.openWindows[i] as RewardWindow;
                if (rw != null && rw.CurrentState != WindowState.Closed) return rw;
            }
        return null;
    }

    /// <summary>场上**所有**开着的 `Reward Window` 的条数（判据同 `FindOpenRewardWindow`）。
    /// A352 那条「拍之前不许有遗留窗」用的就是它 —— ⛔ 别退回只数一台管理器。</summary>
    public static int OpenRewardWindowCount()
    {
        int n = 0;
        foreach (var w in Object.FindObjectsByType<WindowsManager>(FindObjectsSortMode.None))
            for (int i = 0; i < w.openWindows.Count; i++)
            {
                var rw = w.openWindows[i] as RewardWindow;
                if (rw != null && rw.CurrentState != WindowState.Closed) n++;
            }
        return n;
    }

    /// <summary>把场上**还开着**的领奖窗全关掉，返回关掉几扇。
    /// <para>`RewardWindow.Close()` 除了 `SetActive(false)` 还会发 `OnClose(Rewards)`
    /// （= 日常线接的 `DailyData.RefreshOpenDailyWindows`、商店线接的 `ShopTabPage.Setup`）并走
    /// `NotifyClosed` → `ShowPreviousWindow()` ⇒ 被压到 `Background` 的底窗**回到 `Open`**
    /// （这正是玩家点 `Tap To Continue` 那一下的效果，所以这里不是「替玩家作弊」，是同一条生产路径）。</para>
    /// <para>⚠️ 只关领奖窗、**不动别的窗**（⛔ 别用 `CloseAllWindows()` 代替：那会把**主壳窗**
    /// 也一起关掉（壳也在 `openWindows` 里）⇒ 后面那些「屏上只有壳」的截图拍出来是**空帧**
    /// —— `RewardsScene` §七 末尾那条 F7 订正注释记的就是这个坑）。</para></summary>
    public static int DismissRewardWindows()
    {
        int n = 0;
        foreach (var w in Object.FindObjectsByType<WindowsManager>(FindObjectsSortMode.None))
            for (int i = w.openWindows.Count - 1; i >= 0; i--)      // 倒着走：`Close()` 会把它自己摘出表
            {
                var rw = w.openWindows[i] as RewardWindow;
                if (rw == null || rw.CurrentState == WindowState.Closed) continue;
                rw.Close(); n++;
            }
        return n;
    }
}
