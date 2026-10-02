#!/bin/bash
# 「换尺子」A/B：**旧口径（纯色背景）vs 新口径（有内容/有深度的棋盘底图）** 在同一批效果上比一遍。
#
# 判据出处：`EffectSweepBatch.UseBackdrop` 的注释（为什么空场景量不出抓屏族与软粒子族）。
#
# ⚠️ 只跑 **`_tmp_view/sweep_targets.txt` 里那一小批**：
#    全量 `sweep_{orig,exp}.tsv` 与取景缓存会被整份覆盖 ⇒ 本脚本先备份、跑完按内容还原。
# ⚠️ **取景缓存（`sweep_frames.tsv`）两边通用**：本轮的改动**没动取景数学**，所以缓存仍然有效，
#    但小批会把缓存覆写成「只有这十几条」⇒ 必须还原，否则下一次全量 exp 趟读不到取景。
# ⚠️ **目标清单从 `_tmp_view/backdrop_ab_targets.txt` 临时拷成 `sweep_targets.txt`**，
#    跑完**立刻删掉** —— 那个名字留着会让下一次「全量」sweep **只扫这一小批**
#    （本工程记过这个坑；旧脚本 `run_grab_ab.sh` 是「记得自己删」，本脚本改成自动删）。
set -u
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
BASE="d:/4/Unity/资料/比对基线"
TMP="d:/4/_tmp_view"
unset ELECTRON_RUN_AS_NODE

SRC_TARGETS="$TMP/backdrop_ab_targets.txt"
if [ ! -f "$SRC_TARGETS" ]; then
  echo "!! 缺 $SRC_TARGETS（目标清单）—— 先写它再跑"; exit 1
fi
cp "$SRC_TARGETS" "$TMP/sweep_targets.txt"
trap 'rm -f "$TMP/sweep_targets.txt"' EXIT INT TERM
echo "目标 $(grep -c . "$TMP/sweep_targets.txt") 条"

echo "=== 1/5 备份全量基线 ==="
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do cp "$BASE/$f" "$TMP/bak_$f"; done
md5sum "$BASE"/sweep_orig.tsv "$BASE"/sweep_exp.tsv "$BASE"/sweep_frames.tsv > "$TMP/bak_baseline.md5"
cat "$TMP/bak_baseline.md5"

run_side () {   # $1 = orig|exp   $2 = 标签
  echo "--- 跑 $1（$2）---"
  if [ "$1" = "orig" ]; then export WFSWEEP_SIDE=orig; else unset WFSWEEP_SIDE; fi
  "$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod EffectSweepBatch.Run \
      -logFile "$TMP/sweep_${1}_${2}.log" >/dev/null 2>&1
  echo "    退出码 $? —— 结果拷到 $TMP/ab_${2}_${1}.tsv"
  cp "$BASE/sweep_$1.tsv" "$TMP/ab_${2}_$1.tsv"
}

echo "=== 2/5 A 组：旧口径（WFSWEEP_BACKDROP=0）==="
export WFSWEEP_BACKDROP=0
run_side orig A
run_side exp  A

echo "=== 3/5 B 组：新口径（棋盘底图，默认开）==="
unset WFSWEEP_BACKDROP
run_side orig B
run_side exp  B

echo "=== 4/5 还原全量基线 ==="
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do cp "$TMP/bak_$f" "$BASE/$f"; done
md5sum -c "$TMP/bak_baseline.md5"

echo "=== 5/5 比较 ==="
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/compare_backdrop_ab.py"
