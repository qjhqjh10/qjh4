#!/bin/bash
# 抓屏扭曲那一族的 A/B：**扫描时把 GrabPassTransparentFeature 关掉**，看这一族的数会不会变可解释。
#
# 判据出处：`资料/普查产出_0918/E组根因_B3.md` §三 第 4 条 ——
#   「`EffectSweepBatch.RenderAt()` 每个时刻只渲一帧，而 Feature 设计上抓上一帧 ⇒
#     扫描里抓到的可能是**另一个时刻**的拷贝 ⇒ 要判这一族，先把 Feature 在扫描进程里关掉」。
#
# ⚠️ 只跑**小批**（`_tmp_view/sweep_targets.txt` 里那 100 来条抓屏族）：
#    全量 `sweep_{orig,exp}.tsv` 与取景缓存会被整份覆盖 ⇒ 本脚本先备份、跑完按 md5 还原。
set -u
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
BASE="d:/4/Unity/资料/比对基线"
TMP="d:/4/_tmp_view"
unset ELECTRON_RUN_AS_NODE

echo "=== 1/5 备份全量基线 ==="
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do
  cp "$BASE/$f" "$TMP/bak_$f"
done
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

echo "=== 2/5 A 组：Feature 开着（现状）==="
unset WFSWEEP_NOGRAB
run_side orig A
run_side exp  A

echo "=== 3/5 B 组：Feature 关掉（WFSWEEP_NOGRAB=1）==="
export WFSWEEP_NOGRAB=1
run_side orig B
run_side exp  B
unset WFSWEEP_NOGRAB

echo "=== 4/5 还原全量基线 ==="
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do
  cp "$TMP/bak_$f" "$BASE/$f"
done
md5sum -c "$TMP/bak_baseline.md5"

echo "=== 5/5 比较（python）==="
"D:/2/Warpforge_tools/py312/python.exe" "$TMP/compare_grab_ab.py"
