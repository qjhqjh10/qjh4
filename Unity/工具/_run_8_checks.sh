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
  echo "    $(date +%H:%M:%S) 结束 $1 (退出码 $rc)$cc"
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
echo "=== $(date +%H:%M:%S) 全部结束（老八条 + 联机三条）==="
