#!/bin/bash
# arena_ablate.sh —— 对**单场**连续跑多档消融渲染（诊断开关见 ArenaBuilder.RenderPreview 头部注释）。
#
# 用法：
#   bash 工具/arena_ablate.sh <场> <标签1>=<ENV串> [<标签2>=<ENV串> ...]
# 例：
#   bash 工具/arena_ablate.sh battlearenasororitas base="" norefl="WF_NOREFL=1"
#   bash 工具/arena_ablate.sh battlearena2 mid="WF_HIDEIDX=3,4,5"
#
# 🔴 三条要点（都是踩过的）：
#   ① **一档一个独立 Unity 进程** —— `WF_NOEMIT/NOAMB/NOREFL` 改的是 `sharedMaterial`，
#      同进程里跑第二档会带着上一档的修改（代码里没看到还原）。代价是每档 ~40s 启动。
#   ② **基准会**被覆盖**：诊断档默认仍写 `preview_<场>.png`（只有 `WF_HIDE/HIDEIDX/ONLY` 换名）。
#      所以本脚本先备份基准，跑完**还原**；每档的产物另存 `_tmp_view/abl/<场>_<标签>.png`。
#   ③ **一律开 `WF_PSFIXSEED=1`** —— 不开时同一份构建两次能差 6%（正本 §三 的口径）。
#
# 出图路径以日志里的 `[Arena] 出图 → …` 为准（坑 ⑧：别猜文件名）。
set -u
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
OUTDIR="d:/4/_tmp_view/abl"
ARENA="${1:?用法: arena_ablate.sh <场> <标签>=<ENV串> ...}"; shift
PREV="d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/$ARENA/preview_$ARENA.png"
mkdir -p "$OUTDIR"
if [ ! -f "$PREV" ]; then echo "没有基准图 $PREV —— 先跑一次 RenderPreviewFromCLI"; exit 1; fi
cp -f "$PREV" "$OUTDIR/${ARENA}_baseline.png"
echo "[ablate] 基准已备份 → $OUTDIR/${ARENA}_baseline.png"

for spec in "$@"; do
  label="${spec%%=*}"; envs="${spec#*=}"
  log="d:/4/_tmp_view/abl_${ARENA}_${label}.log"
  echo "=== $(date +%H:%M:%S) $ARENA / $label :: ${envs:-（无开关）} ==="
  unset ELECTRON_RUN_AS_NODE
  # 🔴 **用 `export`、不要用 `env $envs`** —— `WF_HIDE` 的值里**带空格**（`=Background space`），
  #    不加引号会被拆词：变量只剩 `=Background`、剩下的词当成 Unity 的参数（2026-09-25 踩）。
  #    多个变量用 `;` 分隔（例：`norefl="WF_NOREFL=1;WF_NOEMIT=1"`）。
  export WF_ARENA="$ARENA" WF_PSFIXSEED=1
  names=""
  if [ -n "$envs" ]; then
    oldIFS=$IFS; IFS=';'
    for kv in $envs; do export "$kv"; names="$names ${kv%%=*}"; done
    IFS=$oldIFS
  fi
  "$UNITY" -batchmode -quit -projectPath "$PROJ" \
      -executeMethod ArenaBuilder.RenderPreviewFromCLI -logFile "$log" >/dev/null 2>&1
  echo "    退出码 $? $(date +%H:%M:%S)"
  # 这一档的开关别带到下一档（否则第二档是「上一档 + 本档」的叠加）
  [ -n "$names" ] && unset $names
  out=$(grep -h "出图 →" "$log" 2>/dev/null | tail -1 | sed 's/.*出图 → //' | tr -d '\r')
  # ⚠️ 日志里打的是**相对路径**（`Assets/WarpforgeArena1/…`，相对工程根）—— 必须补上工程前缀，
  #    否则 `[ -f ]` 判假、每档都报「没读到出图路径」（2026-09-25 踩过）。
  case "$out" in
    Assets/*|Packages/*) out="d:/4/Unity/MyGame/$out" ;;
  esac
  if [ -z "$out" ] || [ ! -f "$out" ]; then
    echo "    ⚠️ 日志里没读到出图路径（看 $log）"
    # ⚠️ 这里**也要还原基准**再 continue —— 第一版漏了，导致「拿上一档的图当基准」比了一整轮
    cp -f "$OUTDIR/${ARENA}_baseline.png" "$PREV"
    continue
  fi
  cp -f "$out" "$OUTDIR/${ARENA}_${label}.png"
  echo "    $out → $OUTDIR/${ARENA}_${label}.png"
  # 诊断档会写回基准名 ⇒ 每档跑完先还原基准，避免下一档/下次比较被污染
  cp -f "$OUTDIR/${ARENA}_baseline.png" "$PREV"
done
echo "[ablate] 基准已还原 → $PREV"
echo "[ablate] 各档产物在 $OUTDIR/ ，比法：python 工具/arena_blocks.py $ARENA --ours $OUTDIR/${ARENA}_<标签>.png"
