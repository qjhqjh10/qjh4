#!/bin/bash
# _build_battle_scenes.sh —— 13 份 `Battle_<场>.unity` 各重建一次（串行）。
#
# 为什么要单独有这么一条：战斗场景是**内嵌**战场内容的（`BattleScene.BuildScene` 调
# `ArenaBuilder.BuildContent`，根节点 `"Warpforge_" + mf.scene`）⇒
# **改了 `ArenaBuilder` / 重跑了 `BuildAll` 之后，13 份战斗场景都要跟着重打**
# （只跑一次 `BattleScene.BuildAndSaveScene` 只覆盖 `WF_ARENA` 指的那一场）。
#
# ⚠️ 同工程同时只能跑一个 Unity 实例 ⇒ 必须串行（本脚本就是串行）。
# ⚠️ 批处理下 `BattleScene.Run` 退出时的 Segmentation fault 是已知的 —— **判据看日志末句**，不看退出码。
set -u
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
LOG=d:/4/_tmp_view
ARENAS="battlearena1 battlearena2 battlearena3 battlearenaaeldari battlearenaastramilitarum \
battlearenablacklegion battlearenadarkangels battlearenaemperorschildren battlearenagenestealers \
battlearenaleviathan battlearenasororitas battlearenaspacewolves battlearenatauviorla"

unset ELECTRON_RUN_AS_NODE
for a in $ARENAS; do
  echo "=== $(date +%H:%M:%S) 建战斗场景 $a ==="
  WF_ARENA=$a "$UNITY" -batchmode -quit -projectPath "$PROJ" \
    -executeMethod BattleScene.BuildAndSaveScene -logFile "$LOG/battle_$a.log" >/dev/null 2>&1
  echo "    退出码 $?  $(date +%H:%M:%S)"
done
echo "=== $(date +%H:%M:%S) 13 份战斗场景全部重建完 ==="
