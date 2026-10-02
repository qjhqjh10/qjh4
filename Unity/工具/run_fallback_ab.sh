#!/bin/bash
# 「兜底路」逐族 A/B：**同一个 prefab / 同一台相机 / 同一份材质，只有 shader 不同**。
#
# 判据出处：`WarpforgeShaderMap.ForceBuiltIn` 的注释 + `资料/特效还原_进度与交接.md` §P1-a0·附四。
#   · 「走原件」= 白名单生效（正常态，`资料/比对基线/sweep_exp.tsv` 那份就是）
#   · 「走我们那份」= `WFBIND_FORCE_BUILTIN="<子串>"`（子串清单 ⇒ 单变量 A/B）
#
# 用法：bash d:/4/Unity/工具/run_fallback_ab.sh <标签> "<FORCE子串>" <目标清单文件> [改前TSV]
#   例：bash 工具/run_fallback_ab.sh dissolve "Fx_ParticleDissolve" /d/4/_tmp_view/t_fam1.txt
#
# 🔴 **口径坑（2026-10-02 自己踩了，写在这里免得再犯）**：第 4 个参数（改前 TSV）默认是
#    `_tmp_view/sweep_exp_自建2.tsv` —— 那是**`WFBIND_FORCE_BUILTIN=1`（全开）**跑出来的。
#    ⇒ 如果你这一步只强制了**一部分**名字，两边差的就**不只是**你要测的那几族，
#      还白送了「其余白名单名字退回原件」那一大块（那本来也是改善，但不是你的功劳）。
#    ⇒ **要单独衡量某几族**：两边用**同一个 FORCE 清单**各跑一趟，把改前那趟的 TSV 当第 4 个参数传进来。
#      已经备好的「全开」两趟：改前 `_tmp_view/sweep_exp_自建2.tsv` · 改后 `_tmp_view/sweep_exp_自建1002c.tsv`。
#
# ⚠️ 只跑清单里那一小批：全量 `sweep_{orig,exp}.tsv` 与取景缓存会被整份覆盖
#    ⇒ 本脚本先备份、跑完按 md5 还原（与 `run_backdrop_ab.sh` 同一套路）。
# ⚠️ **目标清单跑完立刻删掉 `_tmp_view/sweep_targets.txt`** —— 那个名字留着会让下一次
#    「全量」sweep 只扫这一小批（本工程记过这个坑，所以用 trap 自动删）。
set -u
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
BASE="d:/4/Unity/资料/比对基线"
TMP="d:/4/_tmp_view"
PY="D:/2/Warpforge_tools/py312/python.exe"
unset ELECTRON_RUN_AS_NODE

TAG="${1:?用法: run_fallback_ab.sh <标签> \"<FORCE子串>\" <目标清单文件>}"
FORCE="${2:?缺 FORCE 子串}"
TARGETS="${3:?缺目标清单文件}"
[ -f "$TARGETS" ] || { echo "!! 找不到目标清单 $TARGETS"; exit 1; }

cp "$TARGETS" "$TMP/sweep_targets.txt"
trap 'rm -f "$TMP/sweep_targets.txt"' EXIT INT TERM
echo "标签=$TAG  FORCE=$FORCE  目标 $(grep -c . "$TMP/sweep_targets.txt") 条"

echo "=== 1/4 备份全量基线 ==="
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do cp "$BASE/$f" "$TMP/fb_bak_$f"; done
md5sum "$BASE"/sweep_orig.tsv "$BASE"/sweep_exp.tsv "$BASE"/sweep_frames.tsv > "$TMP/fb_bak.md5"
cat "$TMP/fb_bak.md5"

echo "=== 2/4 跑导出侧（WFBIND_FORCE_BUILTIN=$FORCE）==="
export WFBIND_FORCE_BUILTIN="$FORCE"
"$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod EffectSweepBatch.Run \
    -logFile "$TMP/sweep_exp_fb_$TAG.log" >/dev/null 2>&1
echo "    退出码 $?"
cp "$BASE/sweep_exp.tsv" "$TMP/sweep_自建_$TAG.tsv"

echo "=== 3/4 还原全量基线 ==="
for f in sweep_orig.tsv sweep_exp.tsv sweep_frames.tsv; do cp "$TMP/fb_bak_$f" "$BASE/$f"; done
md5sum -c "$TMP/fb_bak.md5"

echo "=== 4/4 与「改前」逐效果比（|ln| 中位/均值是验收指标）==="
# 「改前」默认 = 2026-10-02 那趟**全开** FORCE 的结果（= 审计表的「自建(改后)」列）。
# ⚠️ 口径见文件头：只有**两边同一个 FORCE 清单**时，差才是「你改了的那一族」的功劳。
BEFORE="${4:-$TMP/sweep_exp_自建2.tsv}"
echo "    改前 = $BEFORE"
[ -f "$BEFORE" ] || BEFORE="$TMP/sweep_自建_$TAG.tsv"
# 只留清单里那几条，免得拿全量跟小批比
awk -F'\t' 'NR==FNR{want[$1]=1;next} FNR==1{print;next} ($1 in want)' \
    "$TARGETS" "$BEFORE" > "$TMP/fb_before_$TAG.tsv"
PYTHONIOENCODING=utf-8 "$PY" "d:/4/Unity/工具/compare_sweep.py" \
    "$TMP/fb_before_$TAG.tsv" "$TMP/sweep_自建_$TAG.tsv" \
    --orig "$BASE/sweep_orig.tsv" --top 15 --csv "$TMP/fb_ab_$TAG.csv"
echo "（逐效果 CSV → $TMP/fb_ab_$TAG.csv）"
