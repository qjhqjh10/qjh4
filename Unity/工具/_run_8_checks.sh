#!/bin/bash
# 自检串行跑（Unity 同工程只能一个实例）
# 2026-09-26：老八条 + 🆕 联机三条（NetSelfTest / NetBattleTest / SettingsScene）
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
unset ELECTRON_RUN_AS_NODE
PROJ="D:\4\Unity\MyGame"
run () {  # $1=入口  $2=日志名  $3=筛法
  echo "=== $(date +%H:%M:%S) 开始 $1 ==="
  "$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod "$1" -logFile "d:/4/_tmp_view/$2" >/dev/null 2>&1
  # 🔴 2026-09-28 修：原来这里写 `echo "... (退出码 $?)"` —— `$?` 前面先跑了 `$(date)`，
  #    所以它取到的是 **date 的退出码**，**恒为 0**（编译错误时也报 0 ⇒ 假绿）。
  #    必须**先存下来**再打印；并且顺带查「编译没过」那一行（Unity 编译失败时进程照样退 0/1）。
  local rc=$?
  local cc=""
  grep -q "Scripts have compiler errors" "d:/4/_tmp_view/$2" && cc="  🔴 **编译错误（这一条根本没跑）**"
  # 🔴 2026-10-03（**A61**）：**这里是全工程唯一盯「退出码」的地方** ——
  #    进程内的断言**抓不到「退出期」的段错误**（断言跑完了才崩），所以退出码只能在这儿看。
  #    139 = 128 + 11 = SIGSEGV。`BattleScene.Run` 那条是**已知的间歇段错误**
  #    （断言全绿、崩在打印「合计」之后；2026-10-01 一天 7 次里 6 次崩；2026-10-02 两跑都是 0）。
  #    ⚠️ **判据看日志末的「断言合计」，别只看退出码** —— 但也**别放过它**，它是真崩。
  local seg=""
  [ "$rc" = "139" ] && seg="  🔴 **段错误退出（139）** —— 已知间歇：**看「断言合计」判绿红，别拿退出码当判据、也别当没看见**"
  echo "    $(date +%H:%M:%S) 结束 $1 (退出码 $rc)$cc$seg"
}
run RuleEngineTest.Run  ruleengine.log
run BattleScene.Run     battle.log
run DeckScene.Run       deck.log
run ShellScene.Run      shell.log
run MainMenuScene.Run   menu.log
run RewardsScene.Run    rewards.log
run ShopScene.Run       shop.log
run CollectionScene.Run collection.log
# ---- 🆕 2026-09-26：联机三条（碰了 `Net/` 或 `BattleDriver` 的联机那一段就必须跑）----
run NetSelfTest.Run     net.log          # 传输 / 握手 / 心跳 / 掉线重连
run NetBattleTest.Run   netbattle.log    # 两个裸 context 真打一局 + 换牌定序 + 掐断重连重放
run SettingsScene.Run   settings.log     # 设置窗三页 + 联机页
# ---- 🆕 2026-10-17：卡面基座（⑨ 账上那条「`CardBaseDemo.Run` 不在本脚本里」的收口）----
#   它是**卡面 / 版面那两族**的必跑项（四档分辨率 + 悬停/让位/拖拽/落位/状态色），
#   原来只靠人手单发 ⇒ 常年漏跑。现在并入全套（🔴 仍然**串行**，同工程只能一个 Unity 实例）。
#   ⚠️ 它的另一半职责是**出截图**（`d:/4/_tmp_view/cardbase/`）—— 涉及版面时断言绿了**也要看一眼图**。
run CardBaseDemo.Run    cardbase.log     # 四档分辨率渲染 + 交互闭环（51 条断言）
echo "=== $(date +%H:%M:%S) 全部结束（老八条 + 联机三条 + 卡面基座 = 13 条）==="
