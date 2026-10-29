// NetRoomExit.cs — 「离开联机房间」的**公开静态包装**（账 `其他:460` 第②跳）
//
// ============================ 为什么要有这个文件 ============================
// 原版 `PlayerDataManager.QuitApplication()` 三跳（判据 `decomp_full/PlayerDataManager__QuitApplication.c`）：
//   ① `helpshiftManager` 非空 ⇒ 调它的空方法（`RVA 0x4B33B0` 全局共享空桩 `ret 0` ⇒ 什么都不做）
//   ② `photonManager.LeaveRoom()`      ← **本文件就是这一跳的落点**
//   ③ `StartCoroutine(FullyQuitApplication())`（0.5 秒 → `Application.Quit()`；`DAT_1834b2bb4` 实读 = 0.5f）
//
// `Shell/MainMenuRuntime.QuitApplication()` 要调第②跳，但对位原语两个都是**进不去**的：
//   · `BattleDriver.LeaveNetRoom`（`Battle/BattleDriver.cs`，private 实例方法，需要 `BattleDriver` 实例）
//   · `NetSession.Close`（public，但要先拿到**当刻那个** `NetSession`）
// 而退出应用时手上没有 `BattleDriver` 实例 ⇒ 这里按「`NetRuntime.Instance.Session`」这条路给一个公开口。
//
// ============================ 为什么不复制一份规则 ============================
// 🔴 本仓红线「两处写同一条规则 = 迟早不一致」。所以这里**不重写** `BattleDriver.LeaveNetRoom` 的语义
//   （`force` 那支的 `RemoveRoomAfterLeaving`、`_leaveRoomCount` 计数都留在那边，⛔ 别搬过来）——
//   本类只做三件**最小**的事：拿会话 → 判「已经离开过就不再走一遍」→ 调**同一个原语** `NetSession.Close`。
//   那两句状态守卫与原版 `BattleNetworkManager.LeaveBattleRoom.c` 的 `if (state != 100)` 同形，
//   与 `BattleDriver.LeaveNetRoom` 里那两行**逐字同口径**（⛔ 改一边要同时看另一边）。
//
// ============================ 判据出处 ============================
//   · `decomp_full/PlayerDataManager__QuitApplication.c` —— 三跳 + ② 是 `photonManager.LeaveRoom()`
//   · `decomp_full/PlayerDataManager__FullyQuitApplication.c` / `PlayerDataManager._FullyQuitApplication_d__450__MoveNext.c`
//     —— 0.5 秒常量与 `Application.Quit()`
//   · `NetSession.Close(bool say = true, string reason = null)` —— 本仓已有的「捎一句 bye 并关台」原语
//     （`reason` 是**词条键**语义，见 `NetSession.Close` 的 doc；⛔ 别传裸串）
namespace CardPresentation.Net
{
    /// <summary>「离开联机房间」的公开静态口（账 `其他:460` 第②跳的落点）。
    /// <para>⚠️ 只给**没有 `BattleDriver` 引用**的地方用（典型 = 退出应用、回主菜单）。
    /// 对局内的「离开」仍走 `BattleDriver.LeaveNetRoom`（那条还管 `force` 与计数）。</para></summary>
    public static class NetRoomExit
    {
        /// <summary>当刻有在跑的对局会话 ⇒ 给对面捎一句 `bye` 并关台，返回 <c>true</c>；
        /// 没有会话 / 已经离开过 ⇒ 什么都不做，返回 <c>false</c>（**调用方必须出声**，⛔ 别静默吞掉）。
        /// <para><paramref name="reasonKey"/> = 词条键（由收侧按自己的语言取词），与
        /// `BattleDriver.LeaveNetRoom` 传的是**同一个键** `Settings/Online/Wire/PeerLeftMatch`。</para></summary>
        public static bool LeaveRoomIfAny(string reasonKey)
        {
            var rt = NetRuntime.Instance;
            if (rt == null) return false;                  // 联机层没起来（单机局）⇒ = 原版 `Instance != null` 的另一支
            var s = rt.Session;
            if (s == null) return false;                   // 没有会话 ⇒ 没有房间可离
            if (s.State == NetState.Off) return false;     // = 原版 `if (state != 100)`：已经离开过就不再走一遍
            s.Close(true, reasonKey);
            UnityEngine.Debug.Log("[Net] 离开房间（公开口 `NetRoomExit.LeaveRoomIfAny`）：已给对面捎一句 `bye` 并关台"
                                + "（原版 `PlayerDataManager.QuitApplication` 第②跳 `photonManager.LeaveRoom()`）");
            return true;
        }
    }
}
