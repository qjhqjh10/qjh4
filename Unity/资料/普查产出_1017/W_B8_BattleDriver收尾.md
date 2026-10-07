# W_B8 · 战斗侧收尾（眼睛钮最后一截 · A849 · A850）

**日期** 2026-10-17 · **写手代理** B8 · **白名单** = `Battle/BattleDriver.cs` · `Battle/CardDisplayWindow.cs` · `Editor/BattleScene.cs` ＋ 本文件。
**没跑 Unity**（断言只写不跑）；每个 `.cs` 改完都立刻跑了秒级类型检查（结果见 §④）。

## ① 眼睛钮的最后一截 —— ✅ 落地

| | |
|---|---|
| **原版** | 战斗版 `CardDisplayWindow.showCardTextButton` = `{m_PathID:0}`（null）· **13/13 竞技场**；`ToggleCardState` 在全量反编译里**只有定义、零调用点**（上一轮 W_B4 核过，本轮沿用未重查） |
| **我们** | `BattleDriver.HandleDisplayWindowClick` 里还挂着最后一句调用：`if (_cardDisplay.HitEye(wp)) { _cardDisplay.ToggleLore(); return true; }`；`CardDisplayWindow` 上三个桩（`HitEye` 恒 false · `ToggleLore` 空桩 · `LoreVisible` 恒 true） |
| **改成什么** | 那句**删掉**；三个桩连同它们的内部落点（`ContainsPointer` 里的 `\|\| HitEye`、`Build`/`Show` 里两处 `LoreVisible = true`）**从类型上删干净**；`HandleDisplayWindowClick` 的落点从「四处」改回**三处** |
| **判据出处** | `Battle/CardDisplayWindow.cs` 文件头 §A860（13 份 bundle 逐份实读的字段值 ＋ 节点面复核 ＋ `ShowCard.c:85-96` / `CardSwapFinished.c:20-45` / `Open.c:72-83` 三处 `!= null` 守卫）· `Editor/BattleScene.cs` §A860 |

**grep 自证（全仓 `d:/4/**/*.cs`）**：`_cardDisplay.HitEye` / `_cardDisplay.ToggleLore` / `cdw.HitEye` / `cdw.LoreVisible` **零命中**。
`HitEye|ToggleLore|LoreVisible` 剩下的只有两类：① **注释**（我写的「这里原来是什么」—— `CardDisplayWindow.cs:35/559/730` · `BattleDriver.cs:5143` · `BattleScene.cs:5996`）；
② **另外三个类上的同名成员**（`Battle/MulliganPanel.cs:332` · `Battle/CardChoicePanel.cs:338` · `Shell/CardDetailPopup.cs:870`）—— 那几扇窗**原版本来就有**眼睛钮，⛔ **别顺着这次一起删**。
`Core/CardButtons.HasTextButton` 与 `CardWinBox.EyeCx` **没变成孤儿**（`Shell/CardDetailPopup.cs:893` / `:366` 还在用）。

**断言**：`Editor/BattleScene.cs` 原来那条 `Check(!cdw.HitEye(...))` 会**编不过** ⇒ **换口**成同义的**真判据** `Check(!cdw.ContainsPointer(眼睛钮那一格))`
（＋前提 `Check(!cdw.HitVoice(那一格))`，防「语音钮挪过去」造成假绿），配上原有的「子树里没有 `Show Card Text` 节点」那条 ⇒ 两种坏法（建回来 / 建回来又接进地界）分辨得出。

## ② A849 · `BattleDriver.SetLayer` 单向 —— ❌ **不是隐患**（**刻意不改**）

**我们**：全仓唯一调用点 `BattleDriver.cs:7295`（`SyncBoard` 内 `if (use3DBoard) v.SetLayer(ArenaSlots.ArenaLayer);`）。**原版**：没有对应物（3D 落点是我们自己的机制，判据 = `ArenaSlots`）。
1. **那处调用什么条件下发生**：`SyncBoard` 每次同步都走 —— **新建**的卡视图（同一支里的 `CardView.Create`）与**已有的**都跑，条件只有 `use3DBoard` 一个。
2. **层被改走之后有没有别的路径改回来**：**没有**。`RefreshAll` 只在 `use3DBoard == true` 时**重复**设成 `ArenaLayer`（幂等）；关掉 3D 时**一处都不设回 `Default`**。
3. **⇒ 那为什么仍不是隐患**：**那半条不可达**。`driver.use3DBoard` 的写点全仓**只有三处**（现读）—— `Editor/BattleScene.cs:14641`（= `BuildScene`，在**建任何卡视图之前**按 `boardCam != null` 一次成型）
   ＋ `:12510` / `:12522`（本自检自己翻转，而且**翻回来了**）。运行期没有任何东西会翻它。
4. ⛔ **也刻意没去「补另一半」**（写成 `SetLayer(use3DBoard ? ArenaLayer : 0)`）：`CardView.SetLayer` 是**递归**的（`Core/CardView.cs:673-683`），而 `RefreshAll` 每次都跑
   ⇒ 会把模块自己挂在卡视图子物体上的层**逐次抹平** —— 那是一个**新的、静默的**缺陷，比这条不可达的隐患更糟。

**真正该防的是「那道门被摘掉」**：在「没有 3D 相机」的兜底构建里（`arenaRend == 0` ⇒ 退回烘图 ⇒ `boardCam == null` ⇒ `use3DBoard` 恒 false，`Editor/BattleScene.cs:14594-14628`）
那一档**可达** —— 门一摘，**所有场上卡都落到 `ArenaLayer`**，而那一层**没有任何相机画**（HUD 相机把它剔出 `cullingMask`，`BattleScene.cs:14614`）⇒ **整盘卡静默消失**（`use3DBoard` 自己的 doc 就写着「最坏的一种失败」）。
⇒ 新增**判别式断言**（`Editor/BattleScene.cs` §让位预览 ⑤ 那块内，`:12544`）：**3D 关着时「新建」的卡视图不许挂 `ArenaLayer`**。
- 夹具：往**空槽 2** 塞一个新 `UnitState`（左半场 {3} → {2,3}，仍「连续无洞」）⇒ `SyncBoard` 按**身份**（`byUnit`）找不到视图 ⇒ 真走 `CardView.Create` 那一支（**复用旧视图是验不到这一条的**）；用完当场把那格还回去。
- 🧨 **判别式**：摘掉 `if (use3DBoard)` ⇒ **只有这一条红**（上面那条「3D 开着 ⇒ `ArenaLayer`」**照样绿** —— 两条路给的是同一个层数）。

## ③ A850 · 联机局没接「离开房间」 —— ✅ 落地

**原版逐跳**（全量反编译，2026-10-17 现读，`d:/2/tools/decomp_full/`）：

| 跳 | 出处 | 做什么 |
|---|---|---|
| 1 | `BattleManager__LeaveBattle.c:38` | `if (BattleNetworkManager.Instance != null)` —— 有联机管理器才走 |
| 2 | `:39` | `if (state != 100)` —— **已经离开过就不再来一遍** |
| 3 | `:40` | `CustomDebug.LogWarning` —— **出声**，不是静默 |
| 4 | `:46` | `BattleNetworkManager.LeaveBattleRoom(force: 0)` |
| 5 | `BattleNetworkManager__LeaveBattleRoom.c:13-15` | `Log` → **`state = 100`**（所以第 2 跳是「一次性」闸） |
| 6 | `:35-44` | `BattleManager.IsNetworkedGame()` 为假就 `return`；`force == 0` ⇒ `NetworkCustomManager.RemoveRoomAfterLeaving()` |
| 7 | `:46-49` | `NetworkCustomManager.LeaveRoom()` → `NetworkCustomManager__LeaveRoom.c:22` `PhotonNetwork.LeaveRoom` = **真的退出那个房间** |
| — | `BattleManager__LeaveBattle.c:58` | 上面这一串**全在** `LoadScene("MainMenu Warpforge")` **之前** |

**我们**：`LeaveBattle()` 原来**只有** `LoadScene` ⇒ 对面收不到任何东西（= 账目原文）。
**改成什么**：`BattleDriver.cs` 的 `LeaveBattle()` 在 `_leaveCount++` 之后加一句 `LeaveNetRoom();`，新方法 `:4801`：`_net == null` 直接 return（= 第 1 跳的另一支）
→ `_net.Session == null` **出声警告**（不静默）→ `s.State == NetState.Off` return（= 第 2 跳）→ `s.Close(true, "对面离开了这一局")`（= 第 5~7 跳：捎一句 `bye` ＋ 关台；`Net/NetSession.cs:412`，**不新增协议消息**）→ `_leaveRoomCount++` 并出声。
⇒ **只动 `BattleDriver.cs` 一个文件**，`Net/**` 与 `Editor/Net*.cs` **一个字节没碰**，联机那四条口径也一条没动。

**断言**（`Editor/BattleScene.cs:5549-5614`，接在「结算后的出口」那节之后、第 8 节之前）：夹具 = `driver.Begin(null, null, 20261017)`
（**必须**：出口闩 `_leaving` **只在 `Begin` 里清**，`BattleDriver.cs:2150`；不清则整节走空 = **假绿**）＋ 一个**只记账的传输层探针** `RecordingTransport`
（`BattleScene.cs:15186`，**不是第二条实现**，真实现仍是 `Net/TcpTransport.cs`）＋ `NetSession` / `NetBattle.Attach(driver, sess, …)` / `sess.EnterBattle(...)`（= 生产路径那两步）；收尾 `finally` 里 `AttachNet(null)` 并把 `LobbyHandled` 还原。
四条断言：① `LeaveRoomCount +1` ② **`NetKind.Bye` 恰好交到传输层 1 条**（**判别式**）③ `sess.State == Off` ④ 反例：再走一次出口**不重复发**。
🧨 **判别式为什么成立**：② 与 ① **不同源** —— ① 数的是我们自己的计数（实现侧），② 看的是**线路上的帧**（对面正是靠这一句才知道我们走了）；「只关本地 socket / 只记一笔账」两处一起改 ⇒ **只有 ② 红**。
**⚠️ 宿主说明**：这条链的**端到端**验法（真 socket、真对面）本该在 `Editor/NetBattleTest.cs`（联机那套 `PumpUntil2` 夹具），但**它不在本件白名单** ⇒ 我把它放在 `Editor/BattleScene.cs`（`LeaveBattle` 的宿主）＋ 传输层探针。
**建议主对话**：若要按「联机那套写法」再补一条**跨真 socket** 的（两台 `NetSession`，主机走一次 `LeaveBattle`、客机断言收到 `bye`），**请另派一个能动 `Editor/Net*.cs` 的写手**。

## ③′ 顺手发现的（白名单外 / 只记账，未改）

1. 🔴 **对面「收得到、看不到」**：`NetSession` 收到 `bye` 会 `SetState(Closed, why)` 再回调 `OnClosed`，而 **`OnClosed` 在生产侧零接线**（全仓只有定义 `Net/NetSession.cs:51` ＋ 三处 `Invoke`；`OnPeerLost` 同病）
   ⇒ 我们这一跳把通知送到了**协议层**，但对面**没有任何界面反应**（静默）。落点在 `Net/` 或 `BattleDriver` 订阅 `_net.Session.OnClosed` —— **原版那半边的判据没查到**（Photon 的 `OnLeftRoom` / `OnPlayerLeftRoom` 在 vendored 库里）⇒ ⛔ 没自己发明 UI。
2. **`LeaveBattle` 不是唯一的离场路**：场景被卸载（`OnDestroy`）那条路**不说 `bye`** ⇒ 对面只能靠 10 s 心跳超时发现（`NetSession.SilentTimeoutMs = 10000`）。原版另有一条 `BattleNetworkManager.LeaveBattle`（放弃匹配那条）—— **我们没接，也没查该不该接**。
3. **`Editor/BattleScene.cs:12505-12506` 两处行号引用已过期**（不是本件引入的，我没动）：写的「`PoseFor` 的 `else` 支（`:6972`）与 `SetLayer` 的门（`:7099`）」，现读是 `BattleDriver.cs:7166` / `:7295`。
4. **两份文档还写着「三个桩暂时留着」**（不在白名单，未改）：`资料/普查产出_1017/W_B4_战斗详情窗.md` §「没查清的」第 1 条（`:67`）· `资料/历史/A表已收口_1017_第九轮.md:176` 的「⚠️ 还欠」那格。⇒ **A849 / A850 两行也该由主对话结账。**

## ④ 类型检查（原样贴）

```
$ TMPDIR=/tmp/wf_b8 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⚠️ **中途有一次**跑出 `运行时错误数: 7`，**7 条全在 `Assets/RuleEngine/Core/EffectResolver.cs`（5350-5353 的 `CS0841`）** —— 那**不是本件的文件**（白名单外、一个字节没碰过），
是**别的写手当时正在写的半成品**（与 `W_B5` 报告里记的那次撞车同源）；等 2 分半重跑 ⇒ **0 / 0**。**本件这三个文件全程没出过一条错。**

## ⑤ 没查清 / 还欠

1. **新断言没跑**（红线：不许跑 Unity）⇒ 那四条（＋A849 那条）的真红绿，**要主对话在同步点跑 `BattleScene.Run` 才知道**；我做的只有静态核对（类型检查 ＋ 逐行读回）。
2. A850 的**端到端**（真 socket / 真两台机）还没验 ⇒ 建议另派能动 `Editor/Net*.cs` 的写手（见 ③ 的宿主说明）。
3. `OnClosed` 零接线那一条的**原版半边判据没查到** ⇒ 「对面该看到什么」还没定，别猜。
