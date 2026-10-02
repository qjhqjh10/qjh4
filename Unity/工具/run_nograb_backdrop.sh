#!/bin/bash
# 抓屏族「系统性偏亮」的**决定性实验**：**保持新尺子（棋盘底图）不变，只关掉抓屏 Feature**。
#
# 判据出处：`EffectSweepBatch.NoGrabPass` 的注释 —— 那个 Feature 抓的是**上一帧**，
# 而扫描**每个时刻只渲一帧** ⇒ 抓屏族在扫描里采到的可能是**别的时刻**留下的拷贝。
# ⚠️ **旧结论（2026-10-01）说「关掉也不行」—— 但那是在【空场景】上试的**（没有可扭曲的内容，
#    关不关都量不出东西）。2026-10-02 换了底图尺子之后，这一族第一次**可判**
#    （42 条从「对得上」变成「亮度/密度不对」、比值 1.42–13.76、**全部导出偏亮**）
#    ⇒ **必须在新尺子上重试一次**。
#
# 分组：
#   D = 底图 + Feature **开**（= 当前基线口径）
#   E = 底图 + Feature **关**（`WFSWEEP_NOGRAB=1`）
# 判读：若 E 组的比值塌回 ~1 ⇒ 差异来自**抓屏帧语义**（扫描工具的锅）；
#       若仍 2–13× ⇒ 差异在**我们的 shader/Feature 实现**里（真缺陷，去修）。
#
# ⚠️ 只跑小批（`_tmp_view/backdrop_ab_targets.txt`）；跑完**按 md5 还原**全量基线。
set -u
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
BASE="d:/4/Unity/资料/比对基线"
TMP="d:/4/_tmp_view"
unset ELECTRON_RUN_AS_NODE

SRC_TARGETS="$TMP/backdrop_ab_targets.txt"
[ -f "$SRC_TARGETS" ] || { echo "!! 缺 $SRC_TARGETS"; exit 1; }
cp "$SRC_TARGETS" "$TMP/sweep_targets.txt"
trap 'rm -f "$TMP/sweep_targets.txt"' EXIT INT TERM
echo "目标 $(grep -c . "$TMP/sweep_targets.txt") 条"

echo "=== 1/5 备份全量基线 ==="
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do cp "$BASE/$f" "$TMP/bak_$f"; done
md5sum "$BASE"/sweep_orig.tsv "$BASE"/sweep_exp.tsv "$BASE"/sweep_frames.tsv > "$TMP/bak_baseline.md5"

run_side () {   # $1 = orig|exp   $2 = 标签
  echo "--- 跑 $1（$2）---"
  if [ "$1" = "orig" ]; then export WFSWEEP_SIDE=orig; else unset WFSWEEP_SIDE; fi
  "$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod EffectSweepBatch.Run \
      -logFile "$TMP/sweep_${1}_${2}.log" >/dev/null 2>&1
  echo "    退出码 $? —— 结果拷到 $TMP/ab_${2}_${1}.tsv"
  cp "$BASE/sweep_$1.tsv" "$TMP/ab_${2}_$1.tsv"
}

echo "=== 2/5 D 组：底图 + Feature 开 ==="
unset WFSWEEP_NOGRAB
run_side orig D
run_side exp  D

echo "=== 3/5 E 组：底图 + Feature 关（WFSWEEP_NOGRAB=1）==="
export WFSWEEP_NOGRAB=1
run_side orig E
run_side exp  E
unset WFSWEEP_NOGRAB

echo "=== 4/5 还原全量基线 ==="
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do cp "$TMP/bak_$f" "$BASE/$f"; done
md5sum -c "$TMP/bak_baseline.md5"

echo "=== 5/5 比较（D = Feature 开 · E = Feature 关）==="
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/compare_backdrop_ab.py" D E
