#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_battlehud_clips.py — 战斗 HUD 换牌期那 **8 条 `BattleHud*` legacy clip** 的曲线表生成器。

输入（只读）：`d:/2/新解包资源/assets_full/`
  · `bundle_battlesharedresources_assets_all/AnimationClip/*.json`
      —— **8 条 clip 的唯一副本**（全库 grep `"m_Name": "BattleHud` 只命中这 8 条；
         其余 3 条同包 clip 是 `Battle Arena 3 Animations` / `SporeArmature|LeviathanBigSpore` / `Recorded`，与本条线无关）
  · `bundle_scenes_scenes_battlearena*/Animation/*.json`
      —— 13 场各一枚挂在 `BattleHud` 上的 `Animation` 组件（`m_Animations` 8 条、`m_Animation` = `BattleHudHiddenNoHand`）
输出：
  · `d:/4/Unity/数据/游戏数据/battlehud_clips.json`（全量件）

## 判据链（逐条，2026-10-22 现核）

- **入口**：`BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:113` =
  `BattleHud.ToggleWithAnimation(hud, 1, 1, 0)`（换牌收尾 **show**）；
  `_CloseBattleDoors_d__393__MoveNext.c:29` = `(0,1,1)`（战斗结束 **hide**）；
  `_TutorialStartSequence_d__592__MoveNext.c:283` = `(1,1,1)`（教程 **show**）。
  反编译 `BattleHud__ToggleWithAnimation.c:19-49` 按 `(option, useAnimation, animHand)` 三档选 8 个常量之一 ⇒
  本文件按「`useAnimation=0` = Instant 那 4 条 / `=1` = 带补间那 4 条；`animHand=1` = `NoHand` 变体」归档。
  ⚠️ `ToggleWithAnimation` 的常量 → 名字那一跳（`.cctor`）**本地方法体缺失**，本生成器**没有**从二进制里读出它；
  上面这组对应关系是**按分支结构 + 两个已知调用点**推的（详见报告 §四）。
- **13 场是不是同一套**：13 场的 `Animation` 组件引用的 8 个 `m_PathID` **逐一相同**、顺序相同，
  且 `m_FileID` 都解析到**同一个外部 CAB**（`CAB-68cc81d35f88d5ad277f2717d6e1ac25` = battlesharedresources）。
  各场 `m_FileID` 数值不同（2/3/4）只是**每个包自己的 external 表下标不同**，不是不同资产。
  ⇒ 8 条 clip **只有一份**，13 场共用；本文件给每场记 `clipPids` + 同一个内容 `hash` 作为交叉核对。
- **全是 legacy**：8 条 `m_Legacy=true` · `m_MuscleClipSize=0` · `m_Compressed=false` ·
  `m_CompressedRotationCurves/m_RotationCurves/m_ScaleCurves/m_PPtrCurves` 全空 ⇒ 曲线**本地全有**，
  运行期可用 `AnimationClip.SetCurve` + `Animation` 重建（同 `Battle/CameraIntro.cs` 那套）。

## 🔴 切线（inSlope/outSlope）也存

8 条 clip 的 **所有关键帧斜率都是 0**、`weightedMode=0`（非加权，权重不参与）——
即两条键之间的插值是 Unity 的 smoothstep（ease-in/ease-out），**不是线性**。
本文件仍逐键存 `is`/`os`（与 `gen_camera_intro_clips.py` 同 schema），
运行期必须写回 `new Keyframe(t, v, inTangent, outTangent)`；用 `new Keyframe(t,v)` 时斜率也是 0 ⇒ 等价，
但**别改成线性**（那会让滑出/滑入的手感与原版不同）。

## schema
```
{ "schema":1, "generated":"…", "source":{…}, "pathPrefix":"Canvas/BackCanvas/Safe area BackCanvas/",
  "clips":{ "<name>": { "pid":…, "legacy":true, "muscleClipSize":0, "sampleRate":60.0,
                        "wrapMode":"Once", "length":1.0, "keysTotal":…,
                        "curves":[ { "node":"LeftArea/PlayerInfo",
                                    "path":"Canvas/BackCanvas/Safe area BackCanvas/LeftArea/PlayerInfo",
                                    "type":"RectTransform", "property":"m_AnchoredPosition.x",
                                    "keys":[{"t":0.0,"v":[0.0],"is":[0.0],"os":[0.0]}, …] } ] } },
  "perArena":{ "battlearena1": { "animation":"Animation_1606.json", "animationPid":1606,
                                "gameObjectPid":239, "playAutomatically":true,
                                "defaultClip":"BattleHudHiddenNoHand",
                                "clipPids":[…8…], "clipNames":[…8…],
                                "externalFileIds":[4], "hash":"<md5>" } },
  "reference":{ "<node>|<property>": { "node":"…", "property":"…", "type":"RectTransform",
                                       "hidden":[…], "hiddenSource":"BattleHudHidden",
                                       "shown":[…],  "shownSource":"BattleHudShow" } },
  "differences":[ { "id":"…", "clips":[…], "node":"…", "property":"…", "values":{…}, "note":"…" } ] }
```
`node` = `path` 去掉公共前缀（= 运行期 rig 的相对路径）；`path` 原样保留，便于回原 JSON 核。
`length` = 全部曲线最晚的键（Instant 那 4 条 = 0.0；Hide/Show 那 4 条 = 1.0）。

## 🔴 运行期副本（`Resources/battlehud_clips.json`）—— 为什么同一份数据要写两遍

`clips` / `reference` 在原版件里是 **JSON object（字典）**，而 Unity 的 `JsonUtility`
**读不了字典**（它只映射「字段名 ↔ object 键」和数组）。运行期 `Battle/BattleHudAnim.cs` 要的是
数组，所以本生成器**同时**写一份**同内容的运行期 schema**（先例 = `gen_camera_intro_clips.py`
一次写「全量件 + `Resources/` 运行期副本」）：

```
{ "schema":1, "note":"…", "generated":"…", "pathPrefix":"…", "clipsHash":"…",
  "clips":[ { "name":"BattleHudHidden", "legacy":true, "muscleClipSize":0,
              "sampleRate":60.0, "wrapMode":"Once", "length":0.0,
              "curves":[ { "node":"…", "path":"…", "type":"RectTransform",
                           "property":"m_AnchoredPosition.x",
                           "keys":[{"t":0.0,"v":[0.0],"is":[0.0],"os":[0.0]}] } ] }, … ],
  "refs":[ { "node":"…", "property":"…", "type":"…", "hidden":[…], "shown":[…] }, … ] }
```
（`pid` / `keysTotal` 运行期用不上 ⇒ **不写进副本**；`v`/`is`/`os` 逐键保留 —— 斜率一改就不是 smoothstep 了。）
两条都与全量件**同一份源数据**（`clips` 顺序 = `CLIP_ORDER`；`refs` 按 `(node,property)` 排序）
⇒ 内容不可能分叉（`render_runtime` 逐条回比全量件）；⚠️ **全量件仍是人读/脚本核对的那一份**，字段与顺序一个都没动。

## 幂等
跑两遍逐字节相同（clip 按固定顺序、曲线按 `(node, property)` 排序、无时间戳）。
`--check` 只比对不写盘（**两份都比**）。
用法：
    python -I -X utf8 d:/4/Unity/工具/gen_battlehud_clips.py
    python -I -X utf8 d:/4/Unity/工具/gen_battlehud_clips.py --check
"""
import glob
import hashlib
import io
import json
import os
import sys

ASSETS = r"d:/2/新解包资源/assets_full"
SHARED_PKG = os.path.join(ASSETS, "bundle_battlesharedresources_assets_all")
ARENA_GLOB = os.path.join(ASSETS, "bundle_scenes_scenes_battlearena*")
ANIMATION_DIR = "Animation"
CLIP_DIR = "AnimationClip"
DST_FULL = r"d:/4/Unity/数据/游戏数据/battlehud_clips.json"
# 运行期副本（`JsonUtility` 友好：字典 → 数组，见文件头「运行期副本」那一节）
DST_RES = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/battlehud_clips.json"

# 动画路径的公共前缀（原版 `Animation` 组件挂在 `BattleHud` 上，路径相对它）
PATH_PREFIX = "Canvas/BackCanvas/Safe area BackCanvas/"

# 固定顺序 = 13 场 `Animation.m_Animations` 里的顺序（逐场现核一致）
CLIP_ORDER = [
    "BattleHudHidden",
    "BattleHudHide",
    "BattleHudShow",
    "BattleHudShowInstant",
    "BattleHudHiddenNoHand",
    "BattleHudHideNoHand",
    "BattleHudShowNoHand",
    "BattleHudShowInstantNoHand",
]
NO_HAND = set(n for n in CLIP_ORDER if n.endswith("NoHand"))
POSITION_CURVE = "m_LocalPosition"


def _f(x):
    return float(x)


def _vec(v, dim):
    if dim == 1:
        return [_f(v)]
    return [_f(v[c]) for c in "xyz"[:dim]]


def _keys(curve, dim):
    out = []
    for k in curve["m_Curve"]:
        out.append({
            "t": _f(k["time"]),
            "v": _vec(k["value"], dim),
            "is": _vec(k["inSlope"], dim),
            "os": _vec(k["outSlope"], dim),
        })
    return out


def _read_json(p):
    with io.open(p, "r", encoding="utf-8") as f:
        return json.load(f)


def _node(path):
    """原版路径去掉公共前缀 ⇒ 运行期 rig 相对路径。去掉不了就报错（⛔ 不静默）。"""
    if not path.startswith(PATH_PREFIX):
        raise SystemExit("[gen] 🔴 曲线 path 不在预期前缀下：%r" % path)
    return path[len(PATH_PREFIX):]


def _wrap_mode(v):
    return {0: "Once", 1: "Loop", 2: "PingPong", 4: "ClampForever"}.get(v, str(v))


def build_clip(p):
    d = _read_json(p)
    name = d.get("m_Name", "")
    if name not in CLIP_ORDER:
        raise SystemExit("[gen] 🔴 %s 的 m_Name=%r 不在 8 条名单里" % (p, name))

    # ---- legacy 硬前提（若不是，整条路走不通：曲线拷不出来）----
    if not d.get("m_Legacy"):
        raise SystemExit("[gen] 🔴 %s 不是 legacy clip" % name)
    if d.get("m_MuscleClipSize", 0) != 0:
        raise SystemExit("[gen] 🔴 %s 的 m_MuscleClipSize=%s（预期 0）" % (name, d.get("m_MuscleClipSize")))
    if d.get("m_Compressed"):
        raise SystemExit("[gen] 🔴 %s 是压缩 clip" % name)
    for kind in ("m_CompressedRotationCurves", "m_RotationCurves", "m_ScaleCurves",
                 "m_PPtrCurves", "m_Events"):
        if d.get(kind):
            raise SystemExit("[gen] 🔴 %s 的 %s 非空（%d 条）—— 本 schema 只覆盖 position+float"
                             % (name, kind, len(d[kind])))

    curves = []
    for c in (d.get("m_PositionCurves") or []):
        curves.append({
            "node": _node(c["path"]),
            "path": c["path"],
            "type": "Transform",
            "property": POSITION_CURVE,
            "keys": _keys(c["curve"], 3),
        })
    for c in (d.get("m_FloatCurves") or []):
        attr = c.get("attribute", "")
        if attr not in ("m_AnchoredPosition.x", "m_AnchoredPosition.y"):
            raise SystemExit("[gen] 🔴 %s 出现未覆盖的 float 曲线 attr=%r（path=%r）"
                             % (name, attr, c.get("path")))
        if c.get("classID") != 224:
            raise SystemExit("[gen] 🔴 %s 的 %s classID=%s（预期 224 = RectTransform）"
                             % (name, attr, c.get("classID")))
        curves.append({
            "node": _node(c["path"]),
            "path": c["path"],
            "type": "RectTransform",
            "property": attr,
            "keys": _keys(c["curve"], 1),
        })
    curves.sort(key=lambda c: (c["node"], c["property"]))

    length = 0.0
    for c in curves:
        for k in c["keys"]:
            if k["t"] > length:
                length = k["t"]
    return {
        "pid": int(os.path.basename(p)[len("AnimationClip_"):-len(".json")]),
        "name": name,
        "legacy": True,
        "muscleClipSize": 0,
        "sampleRate": _f(d.get("m_SampleRate", 60.0)),
        "wrapMode": _wrap_mode(d.get("m_WrapMode")),
        "length": length,
        "keysTotal": sum(len(c["keys"]) for c in curves),
        "curves": curves,
    }


def build_clips():
    by_name = {}
    for p in sorted(glob.glob(os.path.join(SHARED_PKG, CLIP_DIR, "*.json"))):
        d = _read_json(p)
        n = d.get("m_Name", "")
        if not n.startswith("BattleHud"):
            continue
        if n in by_name:
            raise SystemExit("[gen] 🔴 重名 clip：%s" % n)
        by_name[n] = build_clip(p)
    missing = [n for n in CLIP_ORDER if n not in by_name]
    extra = sorted(n for n in by_name if n not in CLIP_ORDER)
    if missing or extra:
        raise SystemExit("[gen] 🔴 clip 集合不符：缺 %s / 多 %s" % (missing, extra))
    return {n: by_name[n] for n in CLIP_ORDER}


def clips_hash(clips):
    payload = json.dumps(clips, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return hashlib.md5(payload.encode("utf-8")).hexdigest()


def build_per_arena(clips):
    pid_to_name = {c["pid"]: n for n, c in clips.items()}
    want = set(pid_to_name)
    out = {}
    for pkg in sorted(glob.glob(ARENA_GLOB)):
        if not os.path.isdir(pkg):
            continue
        key = os.path.basename(pkg)[len("bundle_scenes_scenes_"):]
        hits = []
        for p in sorted(glob.glob(os.path.join(pkg, ANIMATION_DIR, "*.json"))):
            d = _read_json(p)
            pids = [(e or {}).get("m_PathID") for e in (d.get("m_Animations") or [])]
            if len(pids) == len(want) and set(pids) == want:
                hits.append((p, d, pids))
        if len(hits) != 1:
            raise SystemExit("[gen] 🔴 %s 里「8 条 BattleHud 的 Animation」命中 %d 份（预期 1）"
                             % (key, len(hits)))
        p, d, pids = hits[0]
        default_pid = (d.get("m_Animation") or {}).get("m_PathID") or 0
        file_ids = sorted(set((e or {}).get("m_FileID") for e in (d.get("m_Animations") or [])))
        out[key] = {
            "animation": os.path.basename(p),
            "animationPid": int(os.path.basename(p)[len("Animation_"):-len(".json")]),
            "gameObjectPid": int((d.get("m_GameObject") or {}).get("m_PathID") or 0),
            "playAutomatically": bool(d.get("m_PlayAutomatically")),
            "defaultClip": pid_to_name.get(default_pid, str(default_pid)),
            "clipPids": [int(x) for x in pids],
            "clipNames": [pid_to_name[x] for x in pids],
            "externalFileIds": [int(x) for x in file_ids],
            "hash": clips_hash(clips),
        }
    if len(out) != 13:
        raise SystemExit("[gen] 🔴 场数 %d != 13" % len(out))
    return out


# ---------------------------------------------------------------------------
# differences：原版素材自带的**不对称**（不是我们实现的偏差）
# 🔴 一律**从数据里读出来**再写进条目；note 只解释「差在哪」，不改数值。
# ---------------------------------------------------------------------------

def _curve_map(clip):
    return {(c["node"], c["property"]): c for c in clip["curves"]}


def _end(map_, node, prop):
    c = map_.get((node, prop))
    if c is None:
        return None
    return c["keys"][-1]["v"][0]


def _start(map_, node, prop):
    c = map_.get((node, prop))
    if c is None:
        return None
    return c["keys"][0]["v"][0]


def build_differences(clips):
    m = {n: _curve_map(c) for n, c in clips.items()}
    diffs = []

    def add(id_, names, node, prop, values, note):
        diffs.append({
            "id": id_,
            "clips": names,
            "node": node,
            "property": prop,
            "values": values,
            "note": note,
        })

    # ① EnemyDeck.x 的「隐藏值」两套：Hidden/HiddenNoHand = −190；Hide/Show/Instant* = −217.7
    names = ["BattleHudHidden", "BattleHudHiddenNoHand", "BattleHudHide",
             "BattleHudShow", "BattleHudShowInstant", "BattleHudShowInstantNoHand"]
    vals = {}
    for n in names:
        v = _end(m[n], "RightArea/EnemyDeck", "m_AnchoredPosition.x")
        if v is None:
            v = _start(m[n], "RightArea/EnemyDeck", "m_AnchoredPosition.x")
        vals[n] = v
    add("enemy_deck_x_hidden_value_split", names, "RightArea/EnemyDeck", "m_AnchoredPosition.x", vals,
        "Hidden/HiddenNoHand 的隐藏 x = −190；Hide/Show/ShowInstant* 用 −217.7。"
        "场景里 EnemyDeck 的静态 rest x = −217.7002（13 场一致）⇒ −190 只出现在两条 Instant 的「隐藏」档。")

    # ② EnemyDeck.x 在带补间的 Show 里是**单键**（t=1.0）⇒ 一开场就跳到 −217.7，不是补间
    vals = {}
    for n in ("BattleHudShow", "BattleHudShowNoHand", "BattleHudHide", "BattleHudHideNoHand"):
        c = m[n].get(("RightArea/EnemyDeck", "m_AnchoredPosition.x"))
        vals[n] = {"keys": len(c["keys"]), "t": [k["t"] for k in c["keys"]],
                   "v": [k["v"][0] for k in c["keys"]]} if c else None
    add("enemy_deck_x_single_key_in_animated", list(vals), "RightArea/EnemyDeck",
        "m_AnchoredPosition.x", vals,
        "Show/ShowNoHand 只有 t=1.0 一个键 ⇒ 从 t=0 起就一直是 −217.7（开局瞬跳）；"
        "Hide/HideNoHand 只有 t=0.0 一个键 ⇒ 全程 −217.7。这条 x **没有补间**。")

    # ③ HideNoHand 的 SettingsBtn 起点与 Hide 不同
    vals = {}
    for n in ("BattleHudHide", "BattleHudHideNoHand"):
        vals[n] = {
            "x": _start(m[n], "RightArea/SettingsBtn", "m_AnchoredPosition.x"),
            "y": _start(m[n], "RightArea/SettingsBtn", "m_AnchoredPosition.y"),
        }
    add("hide_nohand_settingsbtn_start_split", list(vals), "RightArea/SettingsBtn",
        "m_AnchoredPosition.x/y", vals,
        "HideNoHand 的起点是 (−6.90002, −4.79999)，Hide 是 (−54, −9.19995)；"
        "而四个 Show* 的终点都是 (−54, −9.19995)。⇒ HideNoHand 的起点与其它 7 条不自洽。")

    # ④ ShowInstantNoHand 的 Left Anchor y = 17，其余都是 0
    vals = {}
    for n in CLIP_ORDER:
        v = _end(m[n], "LeftArea/Left Anchor", "m_AnchoredPosition.y")
        if v is None:
            v = _start(m[n], "LeftArea/Left Anchor", "m_AnchoredPosition.y")
        vals[n] = v
    add("left_anchor_y_17_only_in_showinstantnohand", CLIP_ORDER, "LeftArea/Left Anchor",
        "m_AnchoredPosition.y", vals,
        "只有 ShowInstantNoHand 写 17.0；其余 7 条写 0.0（ShowInstant 干脆没有这条曲线）。"
        "场景静态 rest y = 0.0（13 场一致）⇒ 17 是孤例。")

    # ⑤ ShowInstant 缺 Left Anchor.y 曲线（其余 7 条都有）
    vals = {n: ("LeftArea/Left Anchor", "m_AnchoredPosition.y") in m[n] for n in CLIP_ORDER}
    add("left_anchor_y_missing_in_showinstant", CLIP_ORDER, "LeftArea/Left Anchor",
        "m_AnchoredPosition.y", vals,
        "ShowInstant 的 float 曲线 14 条（其余 Show 类 15 条）：唯独缺 Left Anchor.y。")

    # ⑥ NoHand 变体是否带 EnemyArea.y
    vals = {n: ("UpperAnchor/EnemyArea", "m_AnchoredPosition.y") in m[n] for n in CLIP_ORDER}
    add("nohand_enemyarea_presence", CLIP_ORDER, "UpperAnchor/EnemyArea",
        "m_AnchoredPosition.y", vals,
        "Hidden/Hide/Show 的 NoHand 变体都**去掉了** EnemyArea.y；但 ShowInstantNoHand **保留**了它。"
        "⇒「NoHand = 只去掉 HandArea」这条规则在 EnemyArea 上不成立（3/4 条按它、1 条不按）。")

    # ⑦ Show 的 PlayerDeck.x 两个键都是 −190（空补间）
    c = m["BattleHudShow"].get(("RightArea/PlayerDeck", "m_AnchoredPosition.x"))
    add("show_playerdeck_x_constant", ["BattleHudShow"], "RightArea/PlayerDeck",
        "m_AnchoredPosition.x",
        {"keys": len(c["keys"]), "t": [k["t"] for k in c["keys"]],
         "v": [k["v"][0] for k in c["keys"]]} if c else None,
        "Show 的 PlayerDeck.x 两个键都是 −190 ⇒ 这条曲线是空操作（x 从不移动）。")

    # ⑧ 带补间 clip 里的单键曲线清单（键数 < 2）
    single = {}
    for n in CLIP_ORDER:
        rows = []
        for c in clips[n]["curves"]:
            if len(c["keys"]) < 2:
                rows.append({"node": c["node"], "property": c["property"],
                             "t": c["keys"][0]["t"], "v": c["keys"][0]["v"]})
        if rows:
            single[n] = rows
    add("single_key_curves", sorted(single), "-", "-", single,
        "Instant 那 4 条整条都是单键（t=0，符合「瞬切」）；带补间的 4 条里也有零星单键曲线 —— "
        "它们在整段时长内是常量，不是补间。")

    return sorted(diffs, key=lambda x: x["id"])


def build_reference(clips):
    """每一条 `(node, property)` 的**原版静止值**（hidden / shown）。

    🔴 为什么需要它：clip 里存的是**原版 HUD 组节点的绝对 `m_AnchoredPosition`**，
    而我们的 HUD 是**摊平**的（`_hudLayer` 直子 + 归一化锚点），每个件的原点是**画布**、不是原版那个组节点。
    直接把这批**绝对值**写进我们的件 = 把 HUD 搬到原版那棵树的位置上（会整体错位）。
    ⇒ 运行期应当用 **增量** `delta(t) = key(t) − shown`（见报告 §三）：
       · `shown` = `BattleHudShow` 的**末键**（该曲线不在 Show 里 ⇒ 退 `BattleHudShowInstantNoHand` 再退 `BattleHudShowInstant`）；
       · `hidden` = `BattleHudHidden` 的末键（退 `BattleHudHiddenNoHand`）。
    这两列**只从数据里取**；`shownSource`/`hiddenSource` 写明取自哪条 clip，便于核「这条曲线到底谁说了算」。
    """
    out = {}
    shown_pref = ["BattleHudShow", "BattleHudShowInstantNoHand",
                  "BattleHudShowInstant", "BattleHudShowNoHand"]
    hidden_pref = ["BattleHudHidden", "BattleHudHiddenNoHand"]
    for n in CLIP_ORDER:
        for c in clips[n]["curves"]:
            key = c["node"] + "|" + c["property"]
            row = out.setdefault(key, {
                "node": c["node"],
                "property": c["property"],
                "type": c["type"],
                "hidden": None, "hiddenSource": None,
                "shown": None, "shownSource": None,
            })
            for src in shown_pref:
                cc = _curve_map(clips[src]).get((c["node"], c["property"]))
                if cc is not None:
                    row["shown"] = cc["keys"][-1]["v"]
                    row["shownSource"] = src
                    break
            for src in hidden_pref:
                cc = _curve_map(clips[src]).get((c["node"], c["property"]))
                if cc is not None:
                    row["hidden"] = cc["keys"][-1]["v"]
                    row["hiddenSource"] = src
                    break
    return {k: out[k] for k in sorted(out)}


def build_all():
    clips = build_clips()
    per_arena = build_per_arena(clips)
    return {
        "schema": 1,
        "generated": "工具/gen_battlehud_clips.py（schema 1；无时间戳、幂等）",
        "source": {
            "clips": SHARED_PKG.replace("\\", "/") + "/" + CLIP_DIR,
            "arenas": ARENA_GLOB.replace("\\", "/"),
        },
        "pathPrefix": PATH_PREFIX,
        "clipsHash": clips_hash(clips),
        "clips": clips,
        "reference": build_reference(clips),
        "perArena": per_arena,
        "differences": build_differences(clips),
    }


# ---------------------------------------------------------------------------
# self_check：独立核对（期望值从原 JSON / 已知事实再读一遍，⛔ 不拿实现证明实现）
# ---------------------------------------------------------------------------

def self_check(doc):
    clips = doc["clips"]
    assert sorted(clips) == sorted(CLIP_ORDER), sorted(clips)
    assert len(doc["perArena"]) == 13, len(doc["perArena"])

    # ① 8 条全是 legacy / 60fps / Once，且键的 time 单调、斜率存活
    for n, c in clips.items():
        assert c["legacy"] is True and c["muscleClipSize"] == 0, n
        assert abs(c["sampleRate"] - 60.0) < 1e-9, n
        assert c["wrapMode"] == "Once", n
        assert c["curves"], n
        for cv in c["curves"]:
            ts = [k["t"] for k in cv["keys"]]
            assert ts == sorted(ts), (n, cv["node"], cv["property"], ts)
            assert len(cv["keys"]) >= 1
        # 和 = 最后键时间
        assert abs(c["length"] - max(k["t"] for cv in c["curves"] for k in cv["keys"])) < 1e-9, n

    # ② 时长：Instant 4 条 = 0.0；Hide/Show 4 条 = 1.0
    for n in CLIP_ORDER:
        want = 0.0 if "Instant" in n or n == "BattleHudHidden" or n == "BattleHudHiddenNoHand" else 1.0
        assert abs(clips[n]["length"] - want) < 1e-9, (n, clips[n]["length"], want)

    # ③ 曲线条数（原 JSON 逐条数过；⛔ 不是按名字猜）
    want_counts = {
        "BattleHudHidden": 16, "BattleHudHiddenNoHand": 14,
        "BattleHudHide": 16, "BattleHudHideNoHand": 14,
        "BattleHudShow": 16, "BattleHudShowNoHand": 14,
        "BattleHudShowInstant": 14, "BattleHudShowInstantNoHand": 15,
    }
    for n, k in want_counts.items():
        assert len(clips[n]["curves"]) == k, (n, len(clips[n]["curves"]), k)

    # ④ 隐藏/显示两端的关键值（原版终值，逐条手核过）
    hidden = {
        "LeftArea/PlayerInfo": (-240.0, -330.0),
        "LeftArea/EnemyInfo": (-240.0, 330.0),
        "RightArea/SettingsBtn": (310.0, 311.0),
        "RightArea/PlayerDeck": (-190.0, -461.0),
        "RightArea/EnemyDeck": (-190.0, 461.0),
        "UpperAnchor/EnemyArea": (None, 402.83),
        "RightArea/Right Anchor": (476.0, 0.0),
        "LeftArea/Left Anchor": (-449.0, 0.0),
    }
    hm = _curve_map(clips["BattleHudHidden"])
    for node, (x, y) in hidden.items():
        if x is not None:
            assert abs(_end(hm, node, "m_AnchoredPosition.x") - x) < 1e-4, (node, x)
        assert abs(_end(hm, node, "m_AnchoredPosition.y") - y) < 1e-4, (node, y)
    # HandArea 是 position 曲线（纯 Transform），末键 (0,-479,0)
    ha = hm[("BottomAnchor/PlayerArea/HandArea", POSITION_CURVE)]["keys"][-1]["v"]
    assert ha == [0.0, -479.0, 0.0], ha

    shown = {
        "LeftArea/PlayerInfo": (50.0, 28.0),
        "LeftArea/EnemyInfo": (50.0, -28.0),
        "RightArea/SettingsBtn": (-54.0, -9.19995),
        "RightArea/PlayerDeck": (-190.0, 0.0),
        "RightArea/EnemyDeck": (-217.7, 0.0),
        "UpperAnchor/EnemyArea": (None, 0.0),
        "RightArea/Right Anchor": (0.0, 0.0),
        "LeftArea/Left Anchor": (0.0, 0.0),
    }
    sm = _curve_map(clips["BattleHudShow"])
    for node, (x, y) in shown.items():
        if x is not None:
            assert abs(_end(sm, node, "m_AnchoredPosition.x") - x) < 1e-4, (node, x)
        assert abs(_end(sm, node, "m_AnchoredPosition.y") - y) < 1e-4, (node, y)
    ha2 = sm[("BottomAnchor/PlayerArea/HandArea", POSITION_CURVE)]["keys"][-1]["v"]
    assert ha2 == [0.0, 0.0, 0.0], ha2

    # ⑤ 斜率确实全 0（若哪天变了，这里会先炸 —— ⛔ 别默默按 0 重建）
    for n in CLIP_ORDER:
        for cv in clips[n]["curves"]:
            for k in cv["keys"]:
                assert all(abs(x) < 1e-9 for x in k["is"] + k["os"]), (n, cv["node"], cv["property"])

    # ⑥ 13 场：同一套 pid / 同一个 hash / 默认 clip 一致
    hashes = set(a["hash"] for a in doc["perArena"].values())
    assert len(hashes) == 1, hashes
    assert hashes == {doc["clipsHash"]}
    pid_order = None
    for key in sorted(doc["perArena"]):
        a = doc["perArena"][key]
        assert a["playAutomatically"] is True, key
        assert a["defaultClip"] == "BattleHudHiddenNoHand", (key, a["defaultClip"])
        assert len(a["clipPids"]) == 8, key
        if pid_order is None:
            pid_order = a["clipPids"]
        assert a["clipPids"] == pid_order, key
        assert a["clipNames"] == CLIP_ORDER, (key, a["clipNames"])

    # ⑥·b reference：每条曲线的 hidden/shown 必须与 `BattleHudHidden` / `BattleHudShow` 的末键逐位一致
    ref = doc["reference"]
    for c in clips["BattleHudShow"]["curves"]:
        row = ref[c["node"] + "|" + c["property"]]
        assert row["shown"] == c["keys"][-1]["v"], (c["node"], c["property"])
        assert row["shownSource"] == "BattleHudShow", (c["node"], c["property"], row["shownSource"])
    for c in clips["BattleHudHidden"]["curves"]:
        row = ref[c["node"] + "|" + c["property"]]
        assert row["hidden"] == c["keys"][-1]["v"], (c["node"], c["property"])
        assert row["hiddenSource"] == "BattleHudHidden", (c["node"], c["property"], row["hiddenSource"])
    # 两个专用节点：EnemyArea 的隐藏值只在带 Hand 的 Hidden 里；HandArea 的显示值是 (0,0,0)
    assert abs(ref["UpperAnchor/EnemyArea|m_AnchoredPosition.y"]["hidden"][0] - 402.83) < 1e-4, \
        ref["UpperAnchor/EnemyArea|m_AnchoredPosition.y"]
    assert ref["BottomAnchor/PlayerArea/HandArea|m_LocalPosition"]["shown"] == [0.0, 0.0, 0.0]
    assert len(ref) == 16, len(ref)

    # ⑦ 抽 3 条与原 JSON 逐键对读（防「生成器读了错文件」）
    print("[gen] 抽核 3 条（对照 assets_full 原 JSON 现读）")
    for key in ("BattleHudHidden", "BattleHudShow", "BattleHudShowInstantNoHand"):
        c = clips[key]
        raw_p = os.path.join(SHARED_PKG, CLIP_DIR, "AnimationClip_%d.json" % c["pid"])
        raw = _read_json(raw_p)
        assert raw["m_Name"] == key
        raw_pos = {(cv["path"], "m_LocalPosition"): cv["curve"] for cv in (raw.get("m_PositionCurves") or [])}
        raw_flt = {(cv["path"], cv.get("attribute")): cv["curve"] for cv in (raw.get("m_FloatCurves") or [])}
        for cv in c["curves"]:
            raw_c = raw_pos.get((cv["path"], cv["property"])) if cv["property"] == POSITION_CURVE \
                else raw_flt.get((cv["path"], cv["property"]))
            assert raw_c is not None, (key, cv["path"], cv["property"])
            assert len(raw_c["m_Curve"]) == len(cv["keys"]), (key, cv["path"])
            for a, b in zip(raw_c["m_Curve"], cv["keys"]):
                assert abs(a["time"] - b["t"]) < 1e-12
                if cv["property"] == POSITION_CURVE:
                    for i in range(3):
                        assert abs(a["value"]["xyz"[i]] - b["v"][i]) < 1e-12
                else:
                    assert abs(a["value"] - b["v"][0]) < 1e-12
        print("  · %-26s curves=%2d keys=%3d len=%s" %
              (key, len(c["curves"]), c["keysTotal"], c["length"]))

    # ⑧ differences 至少覆盖本会话查到的 6 类不对称
    ids = set(d["id"] for d in doc["differences"])
    for want_id in ("enemy_deck_x_hidden_value_split", "enemy_deck_x_single_key_in_animated",
                    "hide_nohand_settingsbtn_start_split", "left_anchor_y_17_only_in_showinstantnohand",
                    "left_anchor_y_missing_in_showinstant", "nohand_enemyarea_presence"):
        assert want_id in ids, want_id

    print("[gen] 自检通过：8 条 legacy · 13 场同一套（hash %s）· 斜率全 0 已保留 · 6 类不对称已记录"
          % doc["clipsHash"][:12])


def render(doc):
    return json.dumps(doc, ensure_ascii=False, indent=1, sort_keys=False) + "\n"


def build_runtime(doc):
    """把全量件**同一份数据**重排成 `JsonUtility` 读得动的形状（字典 → 数组）。

    ⚠️ 只是换容器，**一个数都不改**：`clips` 按 `CLIP_ORDER` 成数组、`reference` 按 key 排序成数组
    （`reference[key]` 里的 `hiddenSource` / `shownSource` 是给人核的，运行期不需要 —— 运行期只用
    `shown` 算增量 `delta(t) = key(t) − shown`，见 `Battle/BattleHudAnim.cs` 的文件头）。
    """
    refs = []
    for key in sorted(doc["reference"]):
        r = doc["reference"][key]
        refs.append({
            "node": r["node"],
            "property": r["property"],
            "type": r["type"],
            "hidden": r["hidden"],
            "shown": r["shown"],
        })
    # 运行期只需要这 6 个字段；`pid` / `keysTotal` **不写**（JsonUtility 用不上，且 `pid` 是 19 位整数）
    clip_out = []
    for n in CLIP_ORDER:
        c = doc["clips"][n]
        clip_out.append({
            "name": c["name"],
            "legacy": c["legacy"],
            "muscleClipSize": c["muscleClipSize"],
            "sampleRate": c["sampleRate"],
            "wrapMode": c["wrapMode"],
            "length": c["length"],
            "curves": c["curves"],
        })
    return {
        "schema": 1,
        "note": "运行期副本（Battle/BattleHudAnim.cs 的 JsonUtility 板）——"
                "与 `数据/游戏数据/battlehud_clips.json` 同一份数据；字典 → 数组、去掉运行期用不上的 pid/keysTotal，值不改。",
        "generated": doc["generated"],
        "pathPrefix": doc["pathPrefix"],
        "clipsHash": doc["clipsHash"],
        "clips": clip_out,
        "refs": refs,
    }


def render_runtime(src, runtime):
    """运行期副本的序列化 + **内容一致性自证**（逐条回比全量件，防「两份分叉」）。"""
    for c in runtime["clips"]:
        src_c = src["clips"][c["name"]]
        assert c["length"] == src_c["length"], c["name"]
        assert c["curves"] == src_c["curves"], c["name"]
    assert len(runtime["refs"]) == len(src["reference"]), (len(runtime["refs"]), len(src["reference"]))
    for r in runtime["refs"]:
        s = src["reference"][r["node"] + "|" + r["property"]]
        assert r["hidden"] == s["hidden"] and r["shown"] == s["shown"], r
    return json.dumps(runtime, ensure_ascii=False, indent=1, sort_keys=False) + "\n"


def _write_and_verify(path, txt):
    d = os.path.dirname(path)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    # 新文件一律 LF
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(txt)
    # 写完当场回读（防「写完不落盘 / 行尾被翻」——同 `_write` 那条纪律）
    back = io.open(path, "rb").read()
    assert back == txt.encode("utf-8"), path
    print("[gen] 写出 %s（%d 字节）" % (path, len(back)))


def main(argv):
    check = "--check" in argv
    doc = build_all()
    self_check(doc)
    txt = render(doc)
    rtxt = render_runtime(doc, build_runtime(doc))
    if check:
        bad = 0
        for path, want in ((DST_FULL, txt), (DST_RES, rtxt)):
            if not os.path.exists(path):
                print("[gen] ✗ %s 不存在" % path)
                bad = 1
                continue
            old = io.open(path, "r", encoding="utf-8").read()
            if old != want:
                print("[gen] ✗ 与 %s 不一致" % path)
                bad = 1
        if bad:
            return 1
        print("[gen] ✅ --check：两份产物（全量件 + 运行期副本）都与现生成一致")
        return 0
    _write_and_verify(DST_FULL, txt)
    _write_and_verify(DST_RES, rtxt)
    print("[gen] 8 条 · 曲线数 %s · 长度集合 %s"
          % ({n: len(c["curves"]) for n, c in doc["clips"].items()},
             sorted(set(round(c["length"], 4) for c in doc["clips"].values()))))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
