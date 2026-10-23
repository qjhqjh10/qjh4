#!/bin/bash
# arena_diff_phases.sh —— 把某一场在**多个粒子相位**下各渲一张，供 `arena_diff.py` 取中位（账 `A1615`）。
#
# 🔴 **为什么需要**：粒子区的逐像素差**主要是相位噪声** —— 同一场扫 `WF_PSSEC`（推进秒数）
#   = 0.5/2/6/8/10/12/16，那块区域与原版的差在 **2.31 ↔ 8.70 之间【振荡】、不单调**
#   ⇒ 拿**单张**预览去比，蒸汽/烟雾区每一条候选都不可信（「我们多画了一大团白烟」有一大半是尺子造的）。
#   （原委与 E2/E1 两条注释 → `Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` 的 2026-09-21/22 两段）
#
# **做法**：本脚本渲 N 个相位 → `preview_<场>__p<N>.png`（**不动**默认那份 `preview_<场>.png`），
#   `arena_diff.py` 看到这些兄弟文件就**逐像素取中位数**。
#
# ⚠️ 粒子相位**不是**「时间越长越对」—— 原版实拍是**某一个**时刻，我们复现不出那个相位；
#   所以判据只能是「**在所有相位都不一致**的地方才算结构差」。
#
# 用法：bash d:/4/Unity/工具/arena_diff_phases.sh battlearenaleviathan
#       bash d:/4/Unity/工具/arena_diff_phases.sh all          # ⚠️ 13 场 × N 个相位，很慢
set -u
U="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
# 🔴 2026-10-23 修：原来写 `P="D:\\4\\Unity\\MyGame"`（反斜杠）⇒ **Unity 静默不启动**
#   （rc=0、日志都不建），而下面的 `cp` 照样成功（复制的是**旧图**）⇒ 三个「相位」其实是同一张图，
#   `arena_diff.py` 的「多相位取中位」全是假的。⇒ 项目路径一律**正斜杠**。
P="D:/4/Unity/MyGame"
ARENAS="battlearena1 battlearena2 battlearena3 battlearenaaeldari battlearenaastramilitarum battlearenablacklegion battlearenadarkangels battlearenaemperorschildren battlearenagenestealers battlearenaleviathan battlearenasororitas battlearenaspacewolves battlearenatauviorla"
# 相位（= `WF_PSSEC` 推进多少真实秒）。⚠️ 别用默认那一个值，**要的是铺开**。
SECS="${WF_PHASES:-4 8 12}"

if [ "${1:-}" = "all" ]; then LIST="$ARENAS"; else LIST="${1:?用法: arena_diff_phases.sh <场|all>}"; fi

for a in $LIST; do
  D="d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/$a"
  for s in $SECS; do
    unset ELECTRON_RUN_AS_NODE
    before=$(stat -c %Y "$D/preview_$a.png" 2>/dev/null || echo 0)
    # 🔴 2026-10-23 修：原来套了一层 `env VAR=… "$U"` —— 在 Git Bash 下 Unity **静默不启动**
    #   （rc=0、日志不建、预览不变）⇒ 改成 bash 的**直接前缀赋值**（实测有效）。
    WF_ARENA="$a" WF_PSFIXSEED=1 WF_PSSEC="$s" "$U" -batchmode -quit -projectPath "$P" \
        -executeMethod ArenaBuilder.RenderPreviewFromCLI -logFile "d:/4/_tmp_view/arena20/phases_${a}_$s.log" >/dev/null 2>&1
    # ⚠️ 判据 = 产物 mtime **真的变了**（⛔ 别信日志、也别只看 `cp` 成功 —— 旧版就是这样复制了旧图）
    after=$(stat -c %Y "$D/preview_$a.png" 2>/dev/null || echo 0)
    if [ "$after" = "$before" ]; then
      echo "  🔴 $a 相位 $s：**渲染没发生**（preview mtime 没变）—— 看 phases_${a}_$s.log；⛔ 不复制旧图"
      continue
    fi
    cp "$D/preview_$a.png" "$D/preview_${a}__p$s.png" \
      && echo "  $a 相位 $s → preview_${a}__p$s.png" || echo "  ⚠️ $a 相位 $s：复制失败"
  done
done
echo "=== 完。现在跑：python -I -X utf8 d:/4/Unity/工具/arena_diff.py <场|all> ==="
