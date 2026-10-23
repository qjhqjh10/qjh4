#!/bin/bash
# arena_diff_phases.sh —— 把某一场在**多个粒子相位**下各渲一张，供 `arena_diff.py` 取中位（账 `A1615`）。
#
# 🔴 **为什么需要**：粒子区的逐像素差**主要是相位噪声** —— 同一场扫 `WF_PSSEC`（推进秒数）
#   = 0.5/2/6/8/10/12/16，那块区域与原版的差在 **2.31 ↔ 8.70 之间【振荡】、不单调**
#   ⇒ 拿**单张**预览去比，蒸汽/烟雾区每一条候选都不可信（「我们多画了一大团白烟」有一大半是尺子造的）。
#   （原委与 E2/E1 两条注释 → `Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` 的 2026-09-21/22 两段）
#
# **做法**：本脚本渲 N 个相位 → `preview_<场>__p<N>.png`，
#   `arena_diff.py` 看到这些兄弟文件就**逐像素取中位数**。
#
# ⚠️ 粒子相位**不是**「时间越长越对」—— 原版实拍是**某一个**时刻，我们复现不出那个相位；
#   所以判据只能是「**在所有相位都不一致**的地方才算结构差」。
#
# 🔴 2026-10-23 修（`T01` 施工计划 §1·3-1 / §2·2 的 **B3**）：**base 被相位覆写**。
#   `RenderPreview` 恒写同一个 `preview_<场>.png` ⇒ 跑完 p12 后 base 就是 p12 那张
#   （2026-10-23 现读 md5：b1 `base==p12` · lev `base==p16` · tau `base==p12`）
#   ⇒ `arena_diff.py:191-205` 的 `[base] + alts` 里**同一个相位占两票**，
#     而**默认相位（`WF_PSSEC=2.0`，`ArenaBuilder.cs:1309`）根本没进中位**。本脚本现在做三件事：
#     ① 跑相位**之前**把 base（默认相位那张）另存成 `preview_<场>__p2.png` ⇒ 中位能跨 **4 个不同相位**；
#        ⚠️ 但先比 md5：若 base 与某个**非 2** 相位图**逐字节相同**，说明它是上一轮覆写留下的
#        ⇒ ⛔ **不许**贴成 p2（那等于把 p12 当默认相位用），只报红 + 给出重渲命令。
#     ② 跑完相位**把 base 还原成默认相位**（与 `arena_ablate.sh:26/58/64` 同一口径：基准不许被诊断档污染）
#        ⇒ 之后 `arena_diff` 的 base / 色调表、`arena_compare.py`、`arena_imgstats.py` 读到的才是默认相位。
#     ③ 打印「本次参与中位的文件 + 各自 md5」，**重复的会点名** —— 一眼看出 base 有没有被当相位用。
#
# ⚠️ 残留（**没改**；要改得动 `arena_diff.py`，不在本件白名单）：base 与 `__p2` 是同一张图时，
#   `arena_diff.py` 仍给 **p2 两票**（p4/p8/p12 各一票）。4 个不同相位都在，只是 p2 权重更高。
#
# 用法：bash d:/4/Unity/工具/arena_diff_phases.sh battlearenaleviathan
#       bash d:/4/Unity/工具/arena_diff_phases.sh all          # ⚠️ 13 场 × N 个相位，很慢
set -u
shopt -s nullglob            # `preview_<场>__p*.png` 一个都没有时展开成空，而不是字面量
U="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
# 🔴 2026-10-23 修：原来写 `P="D:\\4\\Unity\\MyGame"`（反斜杠）+ 套一层 `env VAR=… "$U"`
#   ⇒ 实测 Unity 静默不启动（rc=0、日志都不建），而下面的 `cp` 照样成功（复制的是**旧图**）
#   ⇒ 三个「相位」其实是同一张图，`arena_diff.py` 的「多相位取中位」全是假的。
#   ⇒ 项目路径一律**正斜杠** + **直接前缀赋值**。
#   ⚠️ **2026-10-23 补注（铁律 5）**：这两处里「反斜杠单独就能让 Unity 不启动」**没坐实** ——
#      同为反斜杠的 `工具/_run_8_checks.sh:6`、`工具/arena_ablate.sh:20` 实测都能启动；
#      更可能只是 `env` 那层的锅。正斜杠本身零成本、保留；⛔ 别把「反斜杠 = 不启动」当已证因果引用。
P="D:/4/Unity/MyGame"
ARENAS="battlearena1 battlearena2 battlearena3 battlearenaaeldari battlearenaastramilitarum battlearenablacklegion battlearenadarkangels battlearenaemperorschildren battlearenagenestealers battlearenaleviathan battlearenasororitas battlearenaspacewolves battlearenatauviorla"
# 相位（= `WF_PSSEC` 推进多少真实秒）。⚠️ 别用默认那一个值，**要的是铺开**。
# （默认相位 2.0 由 base 那张提供；若 WF_PHASES 里也写了 2，`__p2` 会被这次真渲覆盖，无害。）
SECS="${WF_PHASES:-4 8 12}"
LOG="d:/4/_tmp_view/arena20"

md5of()   { md5sum "$1" 2>/dev/null | cut -d' ' -f1; }
# mtime 用**纳秒**（`%.9Y`）而不是秒（`%Y`）—— 秒级在自测里会把「同一秒内的两次写」看成一模一样
mtimeof() { stat -c %.9Y "$1" 2>/dev/null || stat -c %Y "$1" 2>/dev/null || echo 0; }

if [ "${1:-}" = "all" ]; then LIST="$ARENAS"; else LIST="${1:?用法: arena_diff_phases.sh <场|all>}"; fi
mkdir -p "$LOG"

for a in $LIST; do
  D="$P/Assets/WarpforgeArena1/arenas/$a"
  BASE="$D/preview_$a.png"
  P2="$D/preview_${a}__p2.png"
  echo "=== $a ==="
  if [ ! -f "$BASE" ]; then
    echo "  🔴 没有 base（$BASE）—— 先渲一次默认相位再跑本脚本："
    echo "     WF_ARENA=$a WF_PSFIXSEED=1 \"$U\" -batchmode -quit -projectPath \"$P\" -executeMethod ArenaBuilder.RenderPreviewFromCLI"
    continue
  fi
  base_h=$(md5of "$BASE")

  # ① base 是不是「默认相位（2.0）」那张？
  #    只靠「与已存的其他相位图逐字节比」排除「被上一轮覆写」这一种情形（别的成因查不出来，见文末 ⚠️）。
  clob=""
  for f in "$D"/preview_"$a"__p*.png; do
    s=${f##*__p}; s=${s%.png}
    [ "$s" = "2" ] && continue
    if [ "$(md5of "$f")" = "$base_h" ]; then clob="$s"; fi
  done
  if [ -n "$clob" ]; then
    # ⚠️ echo 里别写反引号 —— bash 会当成命令替换（`p12` → command not found，2026-10-23 自测踩过）
    echo "  🔴 base 与 p$clob **逐字节相同** ⇒ 它是上一轮相位覆写留下的，**不是默认相位 2.0**"
    echo "     ⛔ 本次**不**另存 __p2（否则把 p$clob 错标成默认相位）。先重渲 base 再跑："
    echo "        WF_ARENA=$a WF_PSFIXSEED=1 \"$U\" -batchmode -quit -projectPath \"$P\" -executeMethod ArenaBuilder.RenderPreviewFromCLI"
    if [ -f "$P2" ]; then
      echo "     ⚠️ 已存在的 preview_${a}__p2.png **本次没更新**（可能是旧代码渲的）—— 重渲 base 后再跑一遍才会刷新"
    fi
  else
    cp -f "$BASE" "$P2" && echo "  base（默认相位 2.0）另存 → preview_${a}__p2.png"
  fi

  # ② 逐相位渲染（判据 = **产物 mtime + md5 真的变了**，⛔ 别信 rc、也别只看 `cp` 成功）
  for s in $SECS; do
    unset ELECTRON_RUN_AS_NODE
    bm=$(mtimeof "$BASE"); bh=$(md5of "$BASE")
    # 🔴 2026-10-23 修：原来套了一层 `env VAR=… "$U"` —— 在 Git Bash 下 Unity **静默不启动**
    #   （rc=0、日志不建、预览不变）⇒ 改成 bash 的**直接前缀赋值**（实测有效）。
    WF_ARENA="$a" WF_PSFIXSEED=1 WF_PSSEC="$s" "$U" -batchmode -quit -projectPath "$P" \
        -executeMethod ArenaBuilder.RenderPreviewFromCLI -logFile "$LOG/phases_${a}_$s.log" >/dev/null 2>&1
    rc=$?
    am=$(mtimeof "$BASE"); ah=$(md5of "$BASE")
    if [ "$rc" -ne 0 ]; then
      echo "  🔴 $a 相位 $s：Unity exit=$rc —— 看 phases_${a}_$s.log；⛔ 不复制"
      continue
    fi
    if [ "$am" = "$bm" ] && [ "$ah" = "$bh" ]; then
      echo "  🔴 $a 相位 $s：**渲染没发生**（preview 的 mtime + md5 都没变）—— 看 phases_${a}_$s.log；⛔ 不复制旧图"
      continue
    fi
    cp -f "$BASE" "$D/preview_${a}__p$s.png" && echo "  $a 相位 $s → preview_${a}__p$s.png"
    if [ "$ah" = "$bh" ]; then
      echo "     ⚠️ 渲过了（mtime 变了）但图与上一张逐字节相同（该场该相位可能本来就没有动态件）"
    fi
  done

  # ③ base 还原成默认相位那张（否则下一次 arena_diff 的 base / 色调表读到的都是最后一个相位）
  if [ -z "$clob" ]; then
    cp -f "$P2" "$BASE" && echo "  base 已还原 = 默认相位（与 __p2 同图）"
  fi

  # ④ 本次参与中位的文件 —— `arena_diff.py:191` 收的正是 `[base] + preview_<场>__p*.png`
  echo "  ── 本次参与中位（arena_diff.py:191）──"
  names=(); hashes=()
  for f in "$BASE" "$D"/preview_"$a"__p*.png; do
    [ -e "$f" ] || continue
    h=$(md5of "$f")
    printf '     %-34s md5=%s\n' "$(basename "$f")" "${h:0:8}"
    names+=("$(basename "$f")"); hashes+=("$h")
  done
  n=${#names[@]}
  distinct=$(printf '%s\n' "${hashes[@]}" | sort -u | wc -l | tr -d ' ')
  dups=""
  for i in "${!hashes[@]}"; do
    for j in "${!hashes[@]}"; do
      if [ "$j" -gt "$i" ] && [ "${hashes[$i]}" = "${hashes[$j]}" ]; then
        dups="$dups ${names[$i]}==${names[$j]}"
      fi
    done
  done
  if [ -n "$dups" ]; then
    echo "     共 $n 票 · **$distinct 个不同相位**（⚠️ 重复占票：$dups）"
  else
    echo "     共 $n 票 · **$distinct 个不同相位**（无重复）"
  fi
  if [ "$distinct" -lt 3 ]; then
    echo "     🔴 不同相位 < 3 —— 候选仍基本是相位噪声（账 A1615）：先补齐相位再判"
  fi
done
echo "=== 完。现在跑：python -I -X utf8 d:/4/Unity/工具/arena_diff.py <场|all> ==="
