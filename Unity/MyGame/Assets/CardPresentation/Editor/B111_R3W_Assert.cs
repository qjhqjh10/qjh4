// B111_R3W_Assert.cs — 第 111 会话第 1 波执行代理 `B111_R3W`（2026-11-01）的断言
//   N137 `ShopData.ResetForTest()` 不再删**默认目录**的存档（只在自检注入 `OverrideDir` 时删临时档）
//   N122 `BattleDriver.TakePendingPlayMode()` = 读一次就清；`ShellRuntime.LoadSceneWithFade` 的**切场失败路径**靠它丢弃残留通道
//   N128 `MenuTutorialFlow.AttachForTest(menu)` 绕过 `Attach` 的 `isPlaying` 闸（非播放态也能拿到实例）
//
// ============================ 判据（施工单）============================
//   N137 → `判据/B111_R3_重跑普查与功能点.md` 第三节 N137 · `Shell/ShopData.cs:503`（改后 `:509` 是条件删档）
//   N122 → 同上第三节 N122 · `Shell/ShellRuntime.cs:171`（批处理 finally）/:219（播放态 catch）· `Battle/BattleDriver.cs:13005`
//   N128 → 同上第三节 N128 · `报告/B106_W22.md §⑥` · `Shell/MenuTutorialFlow.cs` 类尾 `AttachForTest`
//
// ============================ 🔴 灭自证（怎么才能红）============================
//   · N137-A2：把 `ResetForTest` 里的 `if (!string.IsNullOrEmpty(OverrideDir)) DeleteStoreForTest();`
//     改成**永不删**（去掉整句）⇒ 注入了 OverrideDir 的那一份不会被删 ⇒ A2 红。
//   · N122-A3：把 `TakePendingPlayMode` 改成「只读不清」⇒ A3（读完仍非 null）红。
//   · N128-A2：把 `Attach` 的闸改回 `if (!Application.isPlaying) return null;`（去掉 `&& !_allowNonPlayWrite`）
//     ⇒ `AttachForTest` 也拿不到实例 ⇒ A2 红。
//
// ============================ 怎么跑 / 怎么接（⚠️ 本代理不动宿主）============================
//   单独跑（编辑模式/批处理都行）：
//     unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//       -projectPath "D:\4\Unity\MyGame" -executeMethod B111_R3W_Assert.Run -logFile -
//   接宿主（**任何**主菜单 / 商店宿主可跑；本件自建自拆，不依赖宿主已开着什么窗）：
//     **同一物理行**末尾追加（行数零变化）：
//       B111_R3W_Assert.Run(); _sink.Pass += B111_R3W_Assert.Pass; _sink.Fail += B111_R3W_Assert.Fail;
//   ⚠️ 本件会**清一次 `ShopData` 的背包/限购内存态**（`ResetForTest` 的既定行为）⇒ 建议在「商店那一节」最前面跑；
//      `MenuTutorialFlow` 的静态 `Instance` 收尾清回 null（批处理下本来就没有真实例）。
using System.IO;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B111_R3W`：N137（ResetForTest 不删默认档）/ N122（pendingPlayMode 读一次就清）/ N128（AttachForTest 绕过 isPlaying 闸）。</summary>
public static class B111_R3W_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111R3W] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B111_R3W · N137 / N122 / N128");
        N137();
        N122();
        N128();
    }

    // ------------------------------------------------------------ N137
    static void N137()
    {
        MenuCheck.Section(_sink, "N137 `ShopData.ResetForTest` 只在注入 `OverrideDir` 时删临时档（不碰默认目录）");

        string prevDir = ShopData.OverrideDir;
        string tmp = Path.Combine(Path.GetTempPath(), "b111r3w_shop_" + System.Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tmp);
            ShopData.OverrideDir = tmp;
            string storePath = ShopData.StorePath;                 // = tmp/shop.json
            string sibling = Path.Combine(tmp, "keep-me.txt");
            File.WriteAllText(storePath, "{\"version\":1,\"rows\":[]}");
            File.WriteAllText(sibling, "keep");
            Tr(File.Exists(storePath), "A1 前提：注入目录下先放一份 `shop.json`（`StorePath` = " + storePath + "）");

            ShopData.ResetForTest();

            Tr(!File.Exists(storePath), "A2 ★ 注入了 `OverrideDir` ⇒ `ResetForTest` 仍删这一份（保留自检「连盘一起清」的行为）"
                + "｜🧨 改成永不删（去掉 `if (!string.IsNullOrEmpty(OverrideDir))`）⇒ 本行红");
            Tr(File.Exists(sibling), "A3 只删 store 那一份、不误删同目录其它文件（`Delete` 只删 `StorePath`）");
        }
        finally
        {
            try { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); } catch { /* 清理失败不红 */ }
            ShopData.OverrideDir = prevDir;
            ShopData.ResetForTest();      // 回到「无注入」态（不碰默认目录）
        }

        // B 段：无注入 ⇒ 不删、不抛（真 Play 下也不再删 —— 由源码那句 `if (!string.IsNullOrEmpty(OverrideDir))` 保证；
        //   批处理下 `InventoryStore.StoreOn == false`，默认目录本来就读/删不到 ⇒ 这里只能断「不抛」。
        string prev2 = ShopData.OverrideDir;
        ShopData.OverrideDir = null;
        bool threw = false;
        try { ShopData.ResetForTest(); } catch (System.Exception) { threw = true; }
        ShopData.OverrideDir = prev2;
        Tr(!threw, "B1 ★ 无 `OverrideDir` ⇒ `ResetForTest` 不抛（不再删默认目录）"
            + "｜🧨 把条件删档改回无条件 `DeleteStoreForTest()` ⇒ 真 Play 会删玩家真档（批处理观察不到，见文件头）");
    }

    // ------------------------------------------------------------ N122
    static void N122()
    {
        MenuCheck.Section(_sink, "N122 `BattleDriver.TakePendingPlayMode` 读一次就清（切场失败路径消费的口）");

        var prev = BattleDriver.PendingPlayMode;
        try
        {
            BattleDriver.SetPendingPlayMode(GameMode.Tutorial);
            C(BattleDriver.PendingPlayMode, (GameMode?)GameMode.Tutorial, "A1 前提：`SetPendingPlayMode(Tutorial)` 之后读得到 Tutorial");
            var taken = BattleDriver.TakePendingPlayMode();
            C(taken, (GameMode?)GameMode.Tutorial, "A2 ★ `TakePendingPlayMode` 返回之前那个值");
            C(BattleDriver.PendingPlayMode, (GameMode?)null, "A3 ★★ 读一次就清 —— `ShellRuntime` 的**切场失败路径**（`:171` finally / `:219` catch）靠这一句丢弃残留通道"
                + "｜🧨 改成只读不清 ⇒ 本行红");
        }
        finally
        {
            if (prev == null) BattleDriver.TakePendingPlayMode();
            else BattleDriver.SetPendingPlayMode(prev.Value);
        }
    }

    // ------------------------------------------------------------ N128
    static void N128()
    {
        MenuCheck.Section(_sink, "N128 `MenuTutorialFlow.AttachForTest` 绕过 `Attach` 的 `isPlaying` 闸");

        GameObject menuGo = null, flowGo = null;
        // 先清静态实例：避免复用到别的自检留下的（可能已被销毁的）那一个
        MenuTutorialFlow.ResetInstanceForTest();
        try
        {
            menuGo = new GameObject("B111R3W_Menu");
            var menu = menuGo.AddComponent<MainMenuRuntime>();
            Tr(menu != null, "A0 前提：建得到一个 `MainMenuRuntime`");

            var viaAttach = MenuTutorialFlow.Attach(menu);
            C(viaAttach, (MenuTutorialFlow)null, "A1 ★ 非播放态 `Attach` 返回 null（`isPlaying` 闸仍在）");

            var f = MenuTutorialFlow.AttachForTest(menu);
            Tr(f != null, "A2 ★★ 非播放态 `AttachForTest` 返回非 null（绕过 `isPlaying` 闸）"
                + "｜🧨 把 `Attach` 的闸改回只看 `isPlaying`（去掉 `&& !_allowNonPlayWrite`）⇒ 本行红");
            if (f != null)
            {
                C(MenuTutorialFlow.Instance, f, "A3 `AttachForTest` 之后 `Instance == 返回实例`");
                Tr(f.IsActive == false, "A4 测试口把 `_active` 置 false（不自动 `StartFlow` ⇒ 不写 PlayerPrefs）");
                C(f.StageIndex, -1, "A5 测试口把 `_index` 置 -1（干净起点）");
                flowGo = f.gameObject;
            }
        }
        finally
        {
            if (flowGo != null) UnityEngine.Object.DestroyImmediate(flowGo);
            if (menuGo != null) UnityEngine.Object.DestroyImmediate(menuGo);
            MenuTutorialFlow.ResetInstanceForTest();
        }
        C(MenuTutorialFlow.Instance, (MenuTutorialFlow)null, "B1 收尾：`ResetInstanceForTest` 把静态 `Instance` 清回 null");
    }
}
