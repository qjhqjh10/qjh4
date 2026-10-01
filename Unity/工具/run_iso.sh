#!/bin/bash
# EffectIso 逐发射器隔离 —— **两趟跑**（两侧条件互斥，必须分进程；判据与 EffectSweepBatch 同一条）：
#   · orig 趟：**加载全部 84 个源包**（只加载特效包时原版 shader 解析不到 ⇒ 原版侧渲品红、图不可判）
#   · exp  趟：**不加载任何源包**（真实运行时条件；源包在场会把 wf_shaders_extra.bundle 顶掉）
# 取景与槽位标签由 orig 趟写缓存、exp 趟读 ⇒ 两侧逐槽位可比。
#
# 产出：
#   d:/4/_tmp_view/iso/<效果>_<槽>_<标签>__orig.png / __exp.png
#   d:/4/Unity/资料/比对基线/iso_frames.tsv           （orig 趟写、exp 趟读）
#   d:/4/Unity/资料/比对基线/iso_stats_{orig,exp}.tsv （逐槽位数字：lit / sum）
# 用法：bash d:/4/Unity/工具/run_iso.sh [orig|exp|both]     （默认 both）
set -u
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
PROJ="D:\4\Unity\MyGame"
TMP="d:/4/_tmp_view"
unset ELECTRON_RUN_AS_NODE

WHICH="${1:-both}"
mkdir -p "$TMP"

run_side () {
  echo "--- EffectIso Side=$1 ---"
  if [ "$1" = "orig" ]; then export WFISO_SIDE=orig; else unset WFISO_SIDE; fi
  "$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod EffectIso.Run \
      -logFile "$TMP/iso_$1.log" >/dev/null 2>&1
  echo "    退出码 $? —— 日志 $TMP/iso_$1.log"
}

if [ "$WHICH" = "orig" ] || [ "$WHICH" = "both" ]; then run_side orig; fi
if [ "$WHICH" = "exp" ]  || [ "$WHICH" = "both" ]; then run_side exp;  fi
