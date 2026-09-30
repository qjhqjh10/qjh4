#!/bin/bash
# _build_battle_scenes.sh —— 建「对战那条链上要用的场景与 prefab」（串行）
#
# 🔴 **2026-09-30（§27 架构）之后这个脚本的内容变了**（文件名保留 —— 引用它的地方太多）：
#   以前：13 份 `Battle_<场>.unity` 各跑一次 `BattleScene.BuildAndSaveScene`（`WF_ARENA=<场>`）。
#   现在：战场是**运行时实例化**的（`ArenaRuntimeLoader` + `Assets/Resources/ArenaPrefabs/<场>.prefab`）
#        ⇒ 只有**一份** `Battle.unity`，`WF_ARENA` 对存盘路径不再有影响。
#   判据与施工图 → `资料/§27架构_施工图.md`
#   ⚠️ 另有**独立战场场景** `Assets/WarpforgeArena1/Scenes/<场>.unity`（出图/量图用那条链，
#      `ArenaBuilder.BuildAll` 建）—— 那个**没变**，仍然是 13 份。
#
# ⚠️ 同工程同时只能跑一个 Unity 实例 ⇒ 必须串行（本脚本就是串行）。
# ⚠️ 批处理下 `BattleScene.Run` 退出时的 Segmentation fault 是已知的（间歇性）—— **判据看日志末句**，不看退出码。
set -u
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
LOG=d:/4/_tmp_view

unset ELECTRON_RUN_AS_NODE

echo "=== $(date +%H:%M:%S) ① 13 件战场 prefab ==="
"$UNITY" -batchmode -quit -projectPath "$PROJ" \
  -executeMethod ArenaBuilder.BuildArenaPrefabs -logFile "$LOG/arenaprefabs.log" >/dev/null 2>&1
echo "    退出码 $?  $(date +%H:%M:%S)"
grep -E "§27 战场 prefab" "$LOG/arenaprefabs.log" | tail -1

echo "=== $(date +%H:%M:%S) ② 唯一那份 Battle.unity ==="
"$UNITY" -batchmode -quit -projectPath "$PROJ" \
  -executeMethod BattleScene.BuildAndSaveScene -logFile "$LOG/battle_scene.log" >/dev/null 2>&1
echo "    退出码 $?  $(date +%H:%M:%S)"
grep -E "对战场景已存|§27 ——" "$LOG/battle_scene.log" | tail -2

echo "=== $(date +%H:%M:%S) 全部建完（13 prefab + 1 场景）==="
