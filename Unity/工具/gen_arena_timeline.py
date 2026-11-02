#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_arena_timeline.py — 13 战场里**原版 Timeline（PlayableDirector）**的运行时重建表。

【为什么需要它】（战场线 `T09·Timeline 地堡门`；判据 → `判据/R2_R3_现状与判据.md` §4·
`报告/B105_R14.md` §四·1）
  原版 `battlesharedresources` 的 `Bunker Door Parent` 上挂着
  `PlayableDirector 7845524703085758649`（`m_InitialState 1` Playing · `m_WrapMode 1` Loop ·
  `m_InitialTime 15.0`），`PlayableAsset = Leviathan Bunker Door Animation`（5 条 track）。
  ⇒ **原版进这张图地堡门会自动循环播**。
  我们的 arena prefab 是**从清单重建**的，树里**没有** PlayableDirector，`Bunker_door` 是静态网格。
  `Recorded`（那条 muscle clip）虽然早已重打进 `wf_arena_anims_shared.bundle`，**但没人绑定**。

  ⇐ 本工具把「原版那条 Timeline 的**全部序列化数据**」抽成一张 JSON，
     由运行时 `WarpforgeVFX/ArenaTimelinePlayer.cs` **照着它重建**（TimelineAsset 在运行时创建，
     因为 muscle clip 落不成工程 `.anim` —— 与 `WarpforgeAnimatorBridge` 同一条判据）。

【只读输入】（⛔ 不读 `assets_full` 的 JSON —— 那些**不含 type tree 的 pid↔对象**关系，
  正是这里要按 PathID 连线的东西；两份数据源同源，数值已逐字段对过）
  · `d:/2/.../StreamingAssets/aa/StandaloneWindows64/scenes_scenes_<场>.bundle`
    —— 13 场各自的 `PlayableDirector`（**暴露引用在这里被解析成真 GameObject**）
  · 同目录 `battlesharedresources_assets_all.bundle`
    —— `TimelineAsset` / 5 条 track / `ControlPlayableAsset` / `Recorded`（`m_InfiniteClip`）

【什么算「同一类 track」】按 `m_Script.m_PathID`（本 build 的 monoscript）：
  · `5412775887553941483` = `AnimationTrack`
  · `3769382341615217048` = `ControlTrack`
  · `6081512769699668255` = `ControlPlayableAsset`
  · `5038369675663816281` = `TimelineAsset`
  未登记的类型**出声跳过**（⛔ 不猜）。

【输出】
  · `d:/4/Unity/数据/游戏数据/arena_timelines.json`（全量件 · 进 git）
  · `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/arena_timelines.json`（运行期副本）

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_arena_timeline.py --check   # 只比对、不写盘（rc 1 = 有差异）
    python -I -X utf8 d:/4/Unity/工具/gen_arena_timeline.py --write    # 真写
    （不带动作参数 = 打印用法并退出，rc 2 —— 见 CLAUDE.md「会写盘的脚本」那条）
"""
import argparse
import glob
import io
import json
import os
import sys

BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
SHARED_BUNDLE = "battlesharedresources_assets_all.bundle"
SCENE_BUNDLE = "scenes_scenes_%s.bundle"
DST = r"d:/4/Unity/数据/游戏数据/arena_timelines.json"
DST_RUNTIME = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/arena_timelines.json"

SCRIPT_TIMELINE = 5038369675663816281
SCRIPT_ANIM_TRACK = 5412775887553941483
SCRIPT_CONTROL_TRACK = 3769382341615217048
SCRIPT_CONTROL_ASSET = 6081512769699668255


def usage():
    print(__doc__)


def load_tt(bundle_path):
    """{path_id: (type_name, typetree)} —— 只读，不写盘。"""
    import UnityPy
    env = UnityPy.load(bundle_path)
    out = {}
    for o in env.objects:
        try:
            out[o.path_id] = (o.type.name, o.read_typetree())
        except Exception:
            out[o.path_id] = (o.type.name, None)
    return out


def scene_index(tt):
    """场景包里的 GameObject / Transform 表 + `chain(gopid)`（与 gen_arena_scene_anims.Scene 同口径）。"""
    go = {p: d for p, (k, d) in tt.items() if k == "GameObject" and isinstance(d, dict)}
    tr = {p: d for p, (k, d) in tt.items()
          if k in ("Transform", "RectTransform") and isinstance(d, dict)}
    go2tr = {}
    for p, d in tr.items():
        go2tr.setdefault((d.get("m_GameObject") or {}).get("m_PathID"), []).append(p)

    def chain(gopid):
        cur = (go2tr.get(gopid) or [None])[0]
        parts, guard = [], 0
        while cur and guard < 128:
            d = tr[cur]
            g = go.get((d.get("m_GameObject") or {}).get("m_PathID"))
            parts.append(g.get("m_Name") if g else "?")
            cur = (d.get("m_Father") or {}).get("m_PathID")
            guard += 1
        parts.reverse()
        return "/".join(parts)

    def path_of(pid):
        """PPtr 可能指 GameObject、也可能指**组件**（原版 `m_SceneBindings.value` 指的就是
        `Bunker_door` 上那顆 `Animator`）⇒ 组件先退到它的 `m_GameObject` 再取路径。"""
        if not pid:
            return ""
        kind, d = tt.get(pid) or (None, None)
        if kind == "GameObject":
            return chain(pid)
        if isinstance(d, dict):
            g = (d.get("m_GameObject") or {}).get("m_PathID")
            if g:
                return chain(g)
        return ""

    return chain, path_of


def script_pid(d):
    return (d.get("m_Script") or {}).get("m_PathID")


def go_name(tt, gopid):
    """timeline 资产包里的 GO 名（`Bunker Door Parent` 这类，track 自己 m_GameObject=0，用不到）。"""
    d = tt.get(gopid)
    if d and d[0] == "GameObject" and isinstance(d[1], dict):
        return d[1].get("m_Name")
    return None


def clip_length(tt, pid):
    d = tt.get(pid)
    if not d or not isinstance(d[1], dict):
        return None
    mc = d[1].get("m_MuscleClip") or {}
    stop = mc.get("m_StopTime")
    return round(float(stop), 6) if stop is not None else None


def build_arena(arena, shared):
    scene_path = os.path.join(BUNDLE_DIR, SCENE_BUNDLE % arena)
    if not os.path.isfile(scene_path):
        return None, ["场景包不存在：%s（先确认这场有没有 PlayableDirector）" % scene_path]
    scene = load_tt(scene_path)
    chain, path_of = scene_index(scene)
    warn = []

    directors = []
    for pid, (kind, d) in sorted(scene.items()):
        if kind != "PlayableDirector" or not isinstance(d, dict):
            continue
        asset_pid = (d.get("m_PlayableAsset") or {}).get("m_PathID")
        tl = shared.get(asset_pid)
        if not tl or not isinstance(tl[1], dict):
            warn.append("PlayableDirector %d 的 PlayableAsset %s 在 %s 里找不到"
                        % (pid, asset_pid, SHARED_BUNDLE))
            continue
        tl_tree = tl[1]
        director_path = path_of((d.get("m_GameObject") or {}).get("m_PathID"))

        # 暴露引用（exposedName → 场景里的 GameObject 路径）
        refs = {}
        for pair in ((d.get("m_ExposedReferences") or {}).get("m_References") or []):
            if not isinstance(pair, (list, tuple)) or len(pair) != 2:
                continue
            name, ptr = pair[0], pair[1]
            gopid = (ptr or {}).get("m_PathID")
            if gopid:
                refs[str(name)] = path_of(gopid)

        # 场景绑定（track → GameObject 路径；⚠️ value 可能指组件，`path_of` 会退到它的 GO）
        bindings = {}
        for b in (d.get("m_SceneBindings") or []):
            key = (b.get("key") or {}).get("m_PathID")
            val = (b.get("value") or {}).get("m_PathID")
            if key and val:
                bindings[key] = path_of(val)

        tracks = []
        for tp in (tl_tree.get("m_Tracks") or []):
            tpid = (tp or {}).get("m_PathID")
            t = shared.get(tpid)
            if not t or not isinstance(t[1], dict):
                warn.append("track %s 在 %s 里找不到" % (tpid, SHARED_BUNDLE))
                continue
            td = t[1]
            sp = script_pid(td)
            rec = {"name": td.get("m_Name"), "trackPid": tpid}

            if sp == SCRIPT_ANIM_TRACK:
                rec["kind"] = "animation"
                rec["animatorPath"] = bindings.get(tpid, "")
                if td.get("m_Clips"):
                    rec["kind"] = "animation-clips"      # 本 build 没有；真出现就出声
                    warn.append("Animation Track %s 用的是 m_Clips 而不是 m_InfiniteClip "
                                "⇒ 本工具没实现这一档，先别用" % td.get("m_Name"))
                iclip = (td.get("m_InfiniteClip") or {}).get("m_PathID")
                c = shared.get(iclip)
                rec["clip"] = c[1].get("m_Name") if c and isinstance(c[1], dict) else None
                rec["clipDuration"] = clip_length(shared, iclip)
                rec["applyOffsets"] = td.get("m_ApplyOffsets")
                rec["trackOffset"] = td.get("m_TrackOffset")
                rec["matchTargetFields"] = td.get("m_MatchTargetFields")

            elif sp == SCRIPT_CONTROL_TRACK:
                rec["kind"] = "control"
                clips = []
                for c in (td.get("m_Clips") or []):
                    apid = (c.get("m_Asset") or {}).get("m_PathID")
                    a = shared.get(apid)
                    if not a or not isinstance(a[1], dict) or script_pid(a[1]) != SCRIPT_CONTROL_ASSET:
                        warn.append("ControlTrack `%s` 的 clip 资产 %s 不是 ControlPlayableAsset"
                                    % (td.get("m_Name"), apid))
                        continue
                    ad = a[1]
                    exp = ((ad.get("sourceGameObject") or {}).get("exposedName"))
                    clips.append({
                        "display": c.get("m_DisplayName"),
                        "start": round(float(c.get("m_Start") or 0.0), 6),
                        "duration": round(float(c.get("m_Duration") or 0.0), 6),
                        "clipIn": round(float(c.get("m_ClipIn") or 0.0), 6),
                        "timeScale": round(float(c.get("m_TimeScale") or 1.0), 6),
                        "exposedName": str(exp) if exp is not None else "",
                        "target": refs.get(str(exp), ""),
                        "active": int(ad.get("active") or 0),
                        "postPlayback": int(ad.get("postPlayback") or 0),
                        "updateParticle": int(ad.get("updateParticle") or 0),
                        "updateDirector": int(ad.get("updateDirector") or 0),
                        "updateITimeControl": int(ad.get("updateITimeControl") or 0),
                        "searchHierarchy": int(ad.get("searchHierarchy") or 0),
                        "particleRandomSeed": int(ad.get("particleRandomSeed") or 0),
                        "directorOnClipEnd": int(ad.get("directorOnClipEnd") or 0),
                    })
                rec["clips"] = clips

            else:
                warn.append("track `%s` 的类型 script=%s 未登记 ⇒ 跳过（⛔ 不猜）"
                            % (td.get("m_Name"), sp))
                continue
            tracks.append(rec)

        directors.append({
            "path": director_path,
            "playableAsset": tl_tree.get("m_Name"),
            "initialState": int(d.get("m_InitialState") or 0),      # 1 = Playing
            "wrapMode": int(d.get("m_WrapMode") or 0),              # 1 = Loop
            "updateMode": int(d.get("m_DirectorUpdateMode") or 0),  # 1 = GameTime
            "initialTime": round(float(d.get("m_InitialTime") or 0.0), 6),
            "durationMode": int(tl_tree.get("m_DurationMode") or 0),  # 0 = BasedOnClips
            "tracks": tracks,
        })
    return directors, warn


def main(argv=None):
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--write", action="store_true")
    ap.add_argument("--check", action="store_true")
    args, _ = ap.parse_known_args(argv)
    if not (args.write or args.check):
        usage()
        return 2

    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

    arenas = sorted(os.path.basename(p)[len("scenes_scenes_"):-len(".bundle")]
                    for p in glob.glob(os.path.join(BUNDLE_DIR, "scenes_scenes_battlearena*.bundle")))
    shared = load_tt(os.path.join(BUNDLE_DIR, SHARED_BUNDLE))
    doc = {"schema": 1,
           "note": "原版战场 Timeline（PlayableDirector）的运行时重建表。"
                   "由 工具/gen_arena_timeline.py 从原版 StreamingAssets 包直读，别手改。",
           "arenas": []}
    total_dir = total_track = total_clip = 0
    for arena in arenas:
        directors, warn = build_arena(arena, shared)
        for w in warn:
            print("  ⚠️ %s：%s" % (arena, w))
        if not directors:
            continue
        for d in directors:
            total_dir += 1
            total_track += len(d["tracks"])
            total_clip += sum(len(t.get("clips") or []) for t in d["tracks"])
        doc["arenas"].append({"key": arena, "directors": directors})

    text = json.dumps(doc, ensure_ascii=False, indent=1) + "\n"
    print("PlayableDirector %d 个 · track %d 条 · Control clip %d 个 · 涉及 %d 场"
          % (total_dir, total_track, total_clip, len(doc["arenas"])))

    if args.check:
        bad = 0
        for p in (DST, DST_RUNTIME):
            if not os.path.isfile(p):
                print("🔴 缺产物：%s" % p)
                bad += 1
                continue
            cur = io.open(p, encoding="utf-8").read()
            if cur != text:
                print("🔴 与现产物不一致：%s（重跑 --write）" % p)
                bad += 1
            else:
                print("✅ 一致：%s" % p)
        return 0 if bad == 0 else 1

    for p in (DST, DST_RUNTIME):
        os.makedirs(os.path.dirname(p), exist_ok=True)
        io.open(p, "w", encoding="utf-8", newline="\n").write(text)
        print("已写 %s" % p)
    return 0


if __name__ == "__main__":
    sys.exit(main())
