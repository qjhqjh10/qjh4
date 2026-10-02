#!/bin/bash
# 「换尺子」的**归因**：在**同一份资产**上，用旧口径再跑一遍全量 sweep。
#
# 为什么要它：2026-10-02 深夜那一次全量重跑**同时**动了两样东西
#   （① 尺子换成棋盘底图 ② VFX 贴图 mipmap 照原版改了 27 张），
#   验收指标从「中位 |ln| 0.196」跳到「0.010」—— **不能归给某一方**。
# 跑完这份，三段就能拆开：
#   A = 旧尺子 + 旧贴图（git 里那份老基线，README 记着中位 0.196）
#   C = **本脚本**：旧尺子 + 新贴图      ⇒ 尺子的贡献 = B vs C，贴图的贡献 = C vs A
#   B = 新尺子 + 新贴图（当前基线）
#
# ⚠️ 本脚本会**整份覆盖** `sweep_{orig,exp}.tsv` ⇒ 先备份当前（B）基线，跑完按 md5 还原。
set -u
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
BASE="d:/4/Unity/资料/比对基线"
TMP="d:/4/_tmp_view"
unset ELECTRON_RUN_AS_NODE

echo "=== 1/5 备份当前（新尺子）基线 ==="
for f in sweep_orig.tsv sweep_exp.tsv; do cp "$BASE/$f" "$TMP/B_$f"; done
md5sum "$BASE"/sweep_orig.tsv "$BASE"/sweep_exp.tsv > "$TMP/B_baseline.md5"
cat "$TMP/B_baseline.md5"

echo "=== 2/5 旧口径 orig 趟（WFSWEEP_BACKDROP=0）==="
export WFSWEEP_BACKDROP=0
WFSWEEP_SIDE=orig "$UNITY" -batchmode -quit -projectPath "$PROJ" \
    -executeMethod EffectSweepBatch.Run -logFile "$TMP/sweep_orig_C.log" >/dev/null 2>&1
echo "    退出码 $?"
cp "$BASE/sweep_orig.tsv" "$TMP/C_sweep_orig.tsv"

echo "=== 3/5 旧口径 exp 趟 ==="
"$UNITY" -batchmode -quit -projectPath "$PROJ" \
    -executeMethod EffectSweepBatch.Run -logFile "$TMP/sweep_exp_C.log" >/dev/null 2>&1
echo "    退出码 $?"
cp "$BASE/sweep_exp.tsv" "$TMP/C_sweep_exp.tsv"
unset WFSWEEP_BACKDROP

echo "=== 4/5 分析 C（旧尺子 + 新贴图）==="
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/analyze_sweep.py" 2>&1 \
    | grep -E "验收指标|全部可判定|E 亮度|Z 对得上|W 两边|C 只有|D 只有"

echo "=== 5/5 还原 B（新尺子）基线，并**把台账重新算回 B** ==="
for f in sweep_orig.tsv sweep_exp.tsv; do cp "$TMP/B_$f" "$BASE/$f"; done
md5sum -c "$TMP/B_baseline.md5"
PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/analyze_sweep.py" >/dev/null 2>&1
echo "    台账已按 B 重算"
echo "C 的组合结果留在 $TMP/C_sweep_{orig,exp}.tsv"
