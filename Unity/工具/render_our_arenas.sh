#!/bin/bash
# 战场「正路」渲染（一条件）—— 2026-10-11 第十九会话从 _tmp_view 转正（R2 查实：**预览读的是【场景】，不是 prefab**）
#
# 🔴 为什么必须 `BuildAll` 在前：
#   预览 `RenderPreviewFromCLI` 读的是 `Scenes/<场>.unity`；
#   而 `BuildArenaPrefabs` 只写 `Resources/ArenaPrefabs/<场>.prefab`。
#   两条路**不同步** ⇒ 只跑 BuildArenaPrefabs 再出图，图里**没有** prefab 的改动。
#   （2026-10-11 实测：场景里 `m_UseCustomVertexStreams: 1` = 0，prefab 里 = 27 ⇒ 那一批读数作废。）
set -u
U="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
P="D:\4\Unity\MyGame"
L="d:/4/_tmp_view/arena19"
ARENAS="battlearena1 battlearena2 battlearena3 battlearenaaeldari battlearenaastramilitarum battlearenablacklegion battlearenadarkangels battlearenaemperorschildren battlearenagenestealers battlearenaleviathan battlearenasororitas battlearenaspacewolves battlearenatauviorla"

run() { local m="$1" log="$2"; shift 2; unset ELECTRON_RUN_AS_NODE; env "$@" "$U" -batchmode -quit -projectPath "$P" -executeMethod "$m" -logFile "$log"; echo "  [$(date +%H:%M:%S)] $m exit=$?"; }

echo "=== $(date +%H:%M:%S) 起 ==="
# ⓪ 13 份场景先留副本（`BuildAll` 会重写它们；判据 = 跑完 `git diff --numstat` 对一眼）
mkdir -p "$L/scene13_bak"
cp -f d:/4/Unity/MyGame/Assets/WarpforgeArena1/Scenes/battlearena*.unity "$L/scene13_bak/" 2>/dev/null
ls "$L/scene13_bak" | wc -l
# ① 清单 → 场景（这一步才是预览读的那一份）
run ArenaBuilder.BuildAll "$L/6_buildall.log"
# ② 清单 → prefab（游戏运行时读的那一份）
run ArenaBuilder.BuildArenaPrefabs "$L/6_prefabs.log"
# ③ 逐场出图
for a in $ARENAS; do
    run ArenaBuilder.RenderPreviewFromCLI "$L/6_render_$a.log" "WF_ARENA=$a" "WF_PSFIXSEED=1"
done
# ④ R2 的判别实验 E1：关掉烘焙光斑 `Barrels Light FX`，看火芯回不回来
run ArenaBuilder.RenderPreviewFromCLI "$L/6_e1_hidelight.log" "WF_ARENA=battlearena1" "WF_PSFIXSEED=1" "WF_HIDE==Barrels Light FX"
# ⑤ 场景侧自检：F2 那一改到底进没进【场景】
echo "--- 场景里 m_UseCustomVertexStreams: 1 的处数（期望 27）---"
grep -c "m_UseCustomVertexStreams: 1" d:/4/Unity/MyGame/Assets/WarpforgeArena1/Scenes/battlearena1.unity
echo "--- 13 份场景的 git diff（判「是不是整篇重写」）---"
git -C d:/4 diff --numstat -- Unity/MyGame/Assets/WarpforgeArena1/Scenes/ | head -15
echo "=== $(date +%H:%M:%S) 完 ==="
