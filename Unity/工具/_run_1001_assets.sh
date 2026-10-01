#!/bin/bash
# 2026-10-01 本批（§三 第 31 条 · 资产导入路）：
#   ① 重导那 3 个带 `AnimFXModuleChangeMaterial` 的效果（把只被模块字段引用的材质收进 binder）
#   ② 索引（导出集没变也跑一次，便宜）
#   ③ 重建效果库 —— **必跑**：`animfx_modules.json` 改了（新增 `prefabName`），它才落进 .asset
#   ④ BattleScene 自检（盯：钩子挂上 / prefabName 进库 / 相邻单位挑对 / 材质取得到）
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PY="D:/2/Warpforge_tools/py312/python.exe"
unset ELECTRON_RUN_AS_NODE
PROJ="D:\4\Unity\MyGame"
LOG=d:/4/_tmp_view
mkdir -p "$LOG"

u () {  # $1=入口  $2=日志名
  echo "=== $(date +%H:%M:%S) 开始 $1 ==="
  "$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod "$1" -logFile "$LOG/$2" >/dev/null 2>&1
  local rc=$?
  local cc=""
  grep -q "Scripts have compiler errors" "$LOG/$2" && cc="  🔴 **编译错误（这一条根本没跑）**"
  echo "    $(date +%H:%M:%S) 结束 $1 (退出码 $rc)$cc"
}

u EffectExporter.RunListed export_listed.log
echo "--- 导出器那几行 ---"
grep -a "模块材质\|EX1 .*指定 prefab 导出完成\|EX1 .*没取到" "$LOG/export_listed.log" | head -20

echo "=== $(date +%H:%M:%S) 开始 gen_effect_index.py ==="
PYTHONIOENCODING=utf-8 "$PY" d:/4/Unity/工具/gen_effect_index.py > "$LOG/gen_effect_index.log" 2>&1
echo "    $(date +%H:%M:%S) 结束 gen_effect_index.py (退出码 $?)"
tail -4 "$LOG/gen_effect_index.log"

u EffectLibraryBuilder.Run effectlib.log
echo "--- 效果库那几行 ---"
grep -a "效果库\|条\|条数\|完成" "$LOG/effectlib.log" | tail -8

u BattleScene.Run battle.log
echo "--- 自检合计 ---"
grep -a "=== 合计\|=== 结束" "$LOG/battle.log" | tail -3
grep -a "相邻特效\|prefab 名\|相邻单位挑对\|Deathspinner" "$LOG/battle.log" | head -12
echo "=== $(date +%H:%M:%S) 全部结束 ==="
