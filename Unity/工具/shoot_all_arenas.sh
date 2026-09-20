#!/bin/bash
# shoot_all_arenas.sh — 逐个战场跳进原版游戏截图（2026-09-20 用户要求「所有场景都进行对比」）
#
# 做法：改 SceneJumpShot 的 cfg（bundle 名 + 输出目录）→ 启动原版 → **等输出目录里多出一张 shot_*.png**
# → 杀进程 → 下一场。
# ⚠️ **不猜场景名** —— 输出文件名是游戏内场景名（`shot_Battle_Arena_Sororitas.png` 这种），
#    各场的名字不一定是我们 bundle 名去掉前缀，所以判据用「文件数变多」而不是「出现了某个名字」。
# ⚠️ 一次只跑一个游戏实例（别和 Unity 批处理并发）。
# 产物：`D:/4/Unity/资料/原版实拍/arena_0920/`
set -u
OUT="D:/4/Unity/资料/原版实拍/arena_0920"
CFG="D:/2/unity_run_ref/UserData/sjs_dump_cfg.txt"
PY="D:/2/Warpforge_tools/py312/python.exe"
ARENAS="battlearena1 battlearena2 battlearena3 battlearenaaeldari battlearenaastramilitarum battlearenablacklegion battlearenadarkangels battlearenaemperorschildren battlearenagenestealers battlearenaleviathan battlearenasororitas battlearenaspacewolves battlearenatauviorla"

mkdir -p "$OUT"
for a in $ARENAS; do
  echo "=== [$a] $(date +%H:%M:%S) ==="
  before=$(ls -1 "$OUT"/shot_*.png 2>/dev/null | wc -l)
  PYTHONIOENCODING=utf-8 "$PY" -c "
import io
io.open(r'$CFG','wb').write(('scenes_scenes_$a.bundle\n12\n$OUT\n\n').encode('utf-8'))
"
  taskkill //IM Warpforge.exe //F >/dev/null 2>&1
  sleep 2
  ( cd /d/2/unity_run_ref && DOTNET_ROOT="C:/Program Files/dotnet" ./Warpforge.exe >/dev/null 2>&1 & )
  ok=0
  for i in $(seq 1 55); do
    sleep 3
    now=$(ls -1 "$OUT"/shot_*.png 2>/dev/null | wc -l)
    if [ "$now" -gt "$before" ]; then ok=1; break; fi
  done
  if [ $ok = 1 ]; then echo "    → 出图（$((now-before)) 张）"; else echo "    → 超时没出图"; fi
  taskkill //IM Warpforge.exe //F >/dev/null 2>&1
  sleep 3
done
echo "=== 全部完成 $(date +%H:%M:%S) ==="
ls -1 "$OUT"/*.png 2>/dev/null | wc -l
