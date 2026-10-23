#!/bin/bash
# 战场「正路」渲染（一条件）—— 2026-10-11 第十九会话从 _tmp_view 转正（R2 查实：**预览读的是【场景】，不是 prefab**）
#
# 🔴 为什么必须 `BuildAll` 在前：
#   预览 `RenderPreviewFromCLI` 读的是 `Scenes/<场>.unity`；
#   而 `BuildArenaPrefabs` 只写 `Resources/ArenaPrefabs/<场>.prefab`。
#   两条路**不同步** ⇒ 只跑 BuildArenaPrefabs 再出图，图里**没有** prefab 的改动。
#   （2026-10-11 实测：场景里 `m_UseCustomVertexStreams: 1` = 0，prefab 里 = 27 ⇒ 那一批读数作废。）
#
# 🔴 2026-10-23 修两处「已判死」的写法（`T08` 实测过；`T01` 施工计划 §2·2 的 B2）：
#   ① `P="D:\4\Unity\MyGame"`（反斜杠）⇒ 按 `T08` 的记录是「Unity 静默不启动」（rc=0、日志都不建）。
#      🔴 **2026-10-23 就地订正（铁律 5）**：**这条因果没坐实** —— 同样写反斜杠的
#      `工具/_run_8_checks.sh:6`（每天在跑、有实测耗时表）与 `工具/arena_ablate.sh:20`（出过逐档图）
#      **都能正常启动** ⇒ 真正的杀手更可能是下面 ② 的 `env` 那层，不是反斜杠本身。
#      仍统一改成**正斜杠**（零成本、与 `arena_diff_phases.sh:38` 一致），⛔ 但别再把它当「已证实的因果」引用。
#   ② `run()` 里套一层 `env "$@" "$U"` ⇒ 同上静默失败 ⇒ 改 `export`
#      （与 `工具/arena_ablate.sh:34-42` 同一口径：那里也写明「用 export、不要用 env」）。
#   ＋ **按产物 mtime + md5 判「真渲了没」**：旧版 `echo "… exit=$?"` 里的 `$?` 取的是前面
#     `$(date +%H:%M:%S)` 的退出码（**恒 0**）⇒ 日志不建、图没更新也照样一片绿。
#     现在 **rc / mtime / md5 任一不过就报红**，最后红过就 **exit 1**（⛔ 别把「复制成功」当成功）。
set -u
U="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
P="D:/4/Unity/MyGame"
L="d:/4/_tmp_view/arena19"
ARENAS="battlearena1 battlearena2 battlearena3 battlearenaaeldari battlearenaastramilitarum battlearenablacklegion battlearenadarkangels battlearenaemperorschildren battlearenagenestealers battlearenaleviathan battlearenasororitas battlearenaspacewolves battlearenatauviorla"

md5of()   { md5sum "$1" 2>/dev/null | cut -d' ' -f1; }
# mtime 用**纳秒**（`%.9Y`）而不是秒（`%Y`）—— 秒级在自测里会把「同一秒内的两次写」看成一模一样
mtimeof() { stat -c %.9Y "$1" 2>/dev/null || stat -c %Y "$1" 2>/dev/null || echo 0; }

run() {
    local m="$1" log="$2"; shift 2
    local kv names=""
    unset ELECTRON_RUN_AS_NODE
    # 🔴 直接 `export`（⛔ 别套 `env VAR=… "$U"` —— `T08` 记录那里 Unity 静默不启动、rc 还是 0；
    #    因果见文件头 ① 的 2026-10-23 订正：真凶最可能是 `env`，这里只是换成实测有效的写法）
    for kv in "$@"; do export "$kv"; names="$names ${kv%%=*}"; done
    "$U" -batchmode -quit -projectPath "$P" -executeMethod "$m" -logFile "$log" >/dev/null 2>&1
    local rc=$?
    [ -n "$names" ] && unset $names          # 这次的开关别带到下一次调用（否则下一档是叠加态）
    echo "  [$(date +%H:%M:%S)] $m exit=$rc"
    return $rc
}

mkdir -p "$L"
echo "=== $(date +%H:%M:%S) 起 ==="
# ⓪ 13 份场景先留副本（`BuildAll` 会重写它们；判据 = 跑完 `git diff --numstat` 对一眼）
mkdir -p "$L/scene13_bak"
cp -f d:/4/Unity/MyGame/Assets/WarpforgeArena1/Scenes/battlearena*.unity "$L/scene13_bak/" 2>/dev/null
ls "$L/scene13_bak" | wc -l
# ① 清单 → 场景（这一步才是预览读的那一份）
run ArenaBuilder.BuildAll "$L/6_buildall.log"
# ② 清单 → prefab（游戏运行时读的那一份）
run ArenaBuilder.BuildArenaPrefabs "$L/6_prefabs.log"
# ③ 逐场出图（**判据 = 产物 mtime + md5 真的变了**，⛔ 不信 rc、更不信日志）
OKN=0; BADN=0; BADLIST=""
for a in $ARENAS; do
    IMG="$P/Assets/WarpforgeArena1/arenas/$a/preview_$a.png"
    bm=$(mtimeof "$IMG"); bh=$(md5of "$IMG")
    run ArenaBuilder.RenderPreviewFromCLI "$L/6_render_$a.log" "WF_ARENA=$a" "WF_PSFIXSEED=1"
    rc=$?
    am=$(mtimeof "$IMG"); ah=$(md5of "$IMG")
    if [ "$rc" -ne 0 ]; then
        BADN=$((BADN + 1)); BADLIST="$BADLIST $a(rc=$rc)"
        echo "    🔴 $a：exit=$rc —— 看 6_render_$a.log（⛔ 别当成功）"
    elif [ "$am" = "$bm" ] && [ "$ah" = "$bh" ]; then
        BADN=$((BADN + 1)); BADLIST="$BADLIST $a(没渲)"
        echo "    🔴 $a：**渲染没发生**（preview 的 mtime + md5 都没变）—— 看 6_render_$a.log；⛔ 别当成功"
    else
        OKN=$((OKN + 1))
        if [ "$ah" = "$bh" ]; then
            echo "    ⚠️ $a：渲过了（mtime 变了），但图与上一版**逐字节相同**（该场本来可能就没有动态件）"
        else
            echo "    ✅ $a：preview 已更新（md5 ${bh:0:8} → ${ah:0:8}）"
        fi
    fi
done
echo "--- 出图小结：真渲 $OKN 场 · 红 $BADN 场${BADLIST:+ ⇐$BADLIST} ---"
# ④ R2 的判别实验 E1：关掉烘焙光斑 `Barrels Light FX`，看火芯回不回来
#   ⚠️ `WF_HIDE` 那一档会写成 `hide_*.png`（`ArenaBuilder.cs:1481` 附近），**不覆盖** `preview_<场>.png`
run ArenaBuilder.RenderPreviewFromCLI "$L/6_e1_hidelight.log" "WF_ARENA=battlearena1" "WF_PSFIXSEED=1" "WF_HIDE==Barrels Light FX"
# ⑤ 场景侧自检：F2 那一改到底进没进【场景】
echo "--- 场景里 m_UseCustomVertexStreams: 1 的处数（期望 27）---"
grep -c "m_UseCustomVertexStreams: 1" d:/4/Unity/MyGame/Assets/WarpforgeArena1/Scenes/battlearena1.unity
echo "--- 13 份场景的 git diff（判「是不是整篇重写」）---"
git -C d:/4 diff --numstat -- Unity/MyGame/Assets/WarpforgeArena1/Scenes/ | head -15
echo "=== $(date +%H:%M:%S) 完（红 $BADN 场）==="
[ "$BADN" -eq 0 ] || exit 1
