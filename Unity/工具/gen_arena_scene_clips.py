#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_arena_scene_clips.py — 战场线「布景 / 机械 / 场景动画」那 **22 条 legacy clip** 的曲线表生成器。

输入（只读）：`d:/2/新解包资源/assets_full/`
  · `bundle_scenes_scenes_battlearena*/AnimationClip/*.json`  —— 13 个战场包自己的 clip（21 条）
  · `bundle_battlesharedresources_assets_all/AnimationClip/*.json` —— 共享包里的那 1 条
    （`Battle Arena 3 Animations`，被 arena3 的 `Animation_1700` 引用）
输出：
  · `d:/4/Unity/数据/游戏数据/arena_scene_clips.json`（全量件 · ✅ 可进 git）
  ⛔ **不写运行期副本**：这批曲线**目前没有消费方**（`CameraIntro.cs` / `BattleHudAnim.cs` 那两条
    重建路都还没接战场布景）。将来要接时，镜像件该落在
    `MyGame/Assets/CardPresentation/Resources/arena_scene_clips.json` —— 那个目录这一轮不归本工具管
    （文件所有权，铁律 13·3），由主对话另行安排。⛔ 别默认往那儿写。

## 判据链（逐条，2026-10-22 现核）
- **为什么是这 22 条**：`资料/普查产出_1022/J-A_战场素材与材质诊断.md` §2·2 路线① ——
  27 个战场 clip 对象里 **22 条 legacy**（`m_Legacy=true` 且 `m_MuscleClipSize=0`）曲线数据本地全有，
  可以零 Unity 地逐键 dump；另 5 条是 muscle/streamed，**只能重打包**（`m_MuscleClip` 是运行时格式，
  解不成 `.anim`），走 `工具/extract_missing_shaders.py --arena-anims`（`ARENA_ANIM_GROUPS`）。
- **22 = 13 相机 intro + 5 `* Scene Animation(s)` + `Battle Arena 3 Animations` + `Railgun BIG`
  + `Railgun Turret 1` + `Railgun Turret 2`**（13+5+1+3 = 22 个**对象**；名字只有 21 个 ——
  `Battle Arena Emperors Children Camera Intro` 这个**名字**在 emperorschildren 与 spacewolves 各有一份，
  所以**禁止按名字当键**，本文件用 `(pkg, pid)` 定位、并给每条一个 `arena` 段）。
  🔴 **对 J-A 的一处订正（铁律 5）**：J-A §2·2 路线① 的名单里写的是「`Battle Arena 2/3 Animations`」——
  **`Battle Arena 2 Animations` 不是 legacy**（`m_MuscleClipSize=3624`、`m_Legacy=false`，它是 muscle，
  和 J-A 自己 §2·2 的 5 条 muscle 名单一致）⇒ 它**不在**本文件里，本文件里的是
  **`Battle Arena 3 Animations`**（battleshared，legacy）。两个数字（22 对象 / 21 名字）不变。
- **共享包里怎么只挑 1 条**：`battlesharedresources` 的 9 条 legacy 里 8 条是 `BattleHud*`
  （属战斗 HUD 线，已由 `工具/gen_battlehud_clips.py` 导出，**⛔ 别在这里抄第二份**），
  剩 1 条 `Battle Arena 3 Animations`。另两条同包 clip `SporeArmature|LeviathanBigSpore`（muscle 30380）
  与 `Recorded`（muscle 4600）都不是 legacy ⇒ 天然排除；`Recorded` 也**不被任何战场 `Animation` 组件引用**。
- **这些 clip 被谁播**：13 场每场都有挂在 `Scenario` 子树上的 `Animation` 组件（J-A §1·4 A3 数到 49 个），
  本文件的 `usedBy` 就是按「哪个场的 `Animation.m_Animation` 指着这条 clip 的 PathID」现算的
  —— 即**不按名字、按引用**。①三处判据合起来：`m_Legacy && m_MuscleClipSize==0`（可 dump）、
  被战场 `Animation` 引用（是谁在播）、以及「总键数 / 最晚键」可与原 JSON 逐条对账。
- **曲线数据本地全有**：22 条全是 `m_Legacy=true` · `m_MuscleClipSize=0` · `m_Compressed=false` ·
  `m_CompressedRotationCurves` / `m_PPtrCurves` **全空** · `m_Events` **全空**（逐条实读）。
  ⇒ 运行期可用 `AnimationClip.SetCurve` + `Animation` 重建（同 `Battle/CameraIntro.cs` 那套）。

## 🔴 为什么把 `si` / `so`（inSlope / outSlope）也存下来
与 `gen_camera_intro_clips.py` 同一条理由：这批曲线里**大量关键帧带非零切线**
（例如 `Railgun Turret 1/2` 的位置、`Bull` 那类两键曲线、以及 `Railgun BIG` 的 1129 键旋转曲线），
`new Keyframe(t, v)`（切线 0）会把线性/自定义缓动改成 ease-in/out —— **端点仍然对得上**，
所以只验端点的断言抓不到。`weightedMode` 全是 **0（非加权）** ⇒ `inWeight`/`outWeight`
虽然有些不是 1/3（实测有 0.2386/0.2223 这种），但**不参与插值**（死值）⇒ 本文件**断言 weightedMode==0、不存权重**。
同理：22 条 clip 的所有曲线 `m_PreInfinity` / `m_PostInfinity` **全是 2（Clamp）**、
euler 曲线的 `m_RotationOrder` **全是 4（OrderZXY，与 `Quaternion.Euler` 同序）** ⇒ 断言后只记一个
`prePostInfinity: "clamp"` / 每条 euler 曲线的 `rotationOrder: 4`。

## schema（逐键 `t/v/si/so` + `path/type/property`，与 `camera_intro_clips.json` 对齐）
```
{ "schema":1, "note":"…", "source":"…", "bundles":"…",
  "clips":[
    { "name":"Railgun BIG", "arena":"battlearenatauviorla", "usedBy":["battlearenatauviorla"],
      "legacy":true, "muscleClipSize":0, "sampleRate":60.0, "wrapMode":"Loop",
      "length":18.8, "keysTotal":1140, "prePostInfinity":"clamp",
      "src":{ "pkg":"bundle_scenes_scenes_battlearenatauviorla",
              "clip":"AnimationClip_17.json", "pid":17, "cab":"CAB-….sharedAssets" },
      "curves":[
        { "type":"Transform", "property":"m_LocalRotation", "path":"",
          "keys":[ {"t":0.0,"v":[x,y,z,w],"si":[..4],"so":[..4]}, … ] },
        { "type":"GameObject", "property":"m_IsActive", "path":"Dust shake big",
          "classID":1, "keys":[ {"t":0.0,"v":[1.0],"si":[0.0],"so":[0.0]}, … ] },
        { "type":"MonoBehaviour", "property":"m_Enabled", "path":"", "classID":114,
          "script":{"file":1,"pid":8834507010450676941},
          "keys":[ … ] }
      ] }, …
  ] }
```
- `v` / `si` / `so` 的**分量数**由 `property` 决定：3 = 位置/欧拉/缩放，4 = 四元数，1 = 浮点曲线。
- `type` / `property` 的取法：
  · 变换曲线 → `type="Transform"`，`property` ∈ `m_LocalPosition` / `localEulerAnglesRaw` /
    `m_LocalRotation` / `m_LocalScale`。
    ⚠️ **`localEulerAnglesRaw` 是 Unity 运行期 `AnimationClip.SetCurve` 的绑定名**（原 JSON 里没有属性名，
    只有 `m_EulerCurves` + `m_RotationOrder`）⇒ 这一格不是从原始数据读出来的，重建时以 SetCurve 实测为准。
  · 浮点曲线 → `type` 由 `classID` 映射（1=`GameObject` · 114=`MonoBehaviour` · 137=`SkinnedMeshRenderer`），
    `property` = 原 JSON 的 `attribute` 逐字（`m_IsActive` / `m_Enabled` / `blendShape.Key 2-33587156` …），
    `script` = 原 JSON 的 `script` PPtr（`{"file":m_FileID,"pid":m_PathID}`，0/0 = 无脚本绑定）。
- `path` = 原 JSON 的 `m_Path` **逐字**（相对 `Animation` 组件所在 GameObject 的路径；`""` = 根自己）。
- `length` = **全部曲线最晚的键**（与 `gen_camera_intro_clips.py` 同口径；legacy clip 的
  `m_MuscleClip.m_StopTime` 恒为 1.0，**不能**当长度用 —— 实读）。
- ⛔ **没存**：`m_ClipBindingConstant`（泛型/绑定重映射用；这批全是 legacy、绑定由曲线条目直接给出，
  实读 `m_ClipBindingConstant` 也带着同一批 path/attribute，本文件不重复存）。

## 幂等
跑两遍逐字节相同（clip 按 `(arena, name, pid)` 排序、曲线按 position→euler→rotation→scale→float 的
固定类序 + 原序、无时间戳）。`--check` 只比对不写盘。
用法：
    python -I -X utf8 d:/4/Unity/工具/gen_arena_scene_clips.py
    python -I -X utf8 d:/4/Unity/工具/gen_arena_scene_clips.py --check
"""
import glob
import io
import json
import os
import sys

ASSETS = r"d:/2/新解包资源/assets_full"
BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
DST = r"d:/4/Unity/数据/游戏数据/arena_scene_clips.json"

SCENE_PREFIX = "bundle_scenes_scenes_"          # 场键 = 包目录名去掉这一段
SCENE_BUNDLE_GLOB = "bundle_scenes_scenes_battlearena*"
SHARED_PKG = "bundle_battlesharedresources_assets_all"
HUD_PREFIX = "BattleHud"                         # 8 条 HUD clip 归战斗 HUD 线（gen_battlehud_clips.py）
SHARED_EXPECT = "Battle Arena 3 Animations"      # 共享包里唯一该收的那条

CLIP_DIR = "AnimationClip"
ANIMATION_DIR = "Animation"

# UnityEngine.WrapMode：0=Default 1=Once 2=Loop 4=PingPong 8=ClampForever（序列化值）
WRAP_NAMES = {0: "Default", 1: "Once", 2: "Loop", 4: "PingPong", 8: "ClampForever"}
# 浮点曲线的 `classID` → 绑定类型名
CLASS_NAMES = {1: "GameObject", 114: "MonoBehaviour", 137: "SkinnedMeshRenderer"}
# 四类变换曲线：(JSON 字段, 绑定属性名, 分量数, 是否带 rotationOrder)
TRANSFORM_KINDS = [
    ("m_PositionCurves", "m_LocalPosition", 3, False),
    ("m_EulerCurves", "localEulerAnglesRaw", 3, True),
    ("m_RotationCurves", "m_LocalRotation", 4, False),
    ("m_ScaleCurves", "m_LocalScale", 3, False),
]


def _f(x):
    """JSON 里保持 float（Python 的 int 会被 Unity 的 JsonUtility 读成 float；这里统一 float）。"""
    return float(x)


def _read_json(p):
    with io.open(p, "r", encoding="utf-8") as f:
        return json.load(f)


def _vec(d, dim):
    """分量：Vector3 = x/y/z、Quaternion = x/y/z/w、标量曲线 = 裸数字。"""
    if dim == 1:
        return [_f(d)]
    return [_f(d[c]) for c in "xyzw"[:dim]]


def _keys(curve, dim):
    out = []
    for k in curve["m_Curve"]:
        out.append({
            "t": _f(k["time"]),
            "v": _vec(k["value"], dim),
            "si": _vec(k["inSlope"], dim),
            "so": _vec(k["outSlope"], dim),
        })
    return out


def scene_pkgs():
    return sorted(d for d in (os.path.basename(p) for p in glob.glob(os.path.join(ASSETS, SCENE_BUNDLE_GLOB)))
                  if os.path.isdir(os.path.join(ASSETS, d)))


def arena_key(pkg):
    return pkg[len(SCENE_PREFIX):]


def _bundle_path(pkg):
    """assets_full 的目录名是 `bundle_<bundle 文件名去后缀>` ⇒ 还原成真包路径。"""
    return os.path.join(BUNDLE_DIR, pkg.replace("bundle_", "", 1) + ".bundle")


def cab_of_clips(pkg, pids):
    """`{pid: 内层 CAB 名}` —— 从**真包**里现读（`assets_full` 的 JSON 不带 CAB 信息）。

    ⚠️ 只用来在报告里写「源包 + CAB」供复查；读不到就返回 `{}` 并出声（⛔ 不静默、也不编一个值）。
    """
    p = _bundle_path(pkg)
    if not os.path.isfile(p):
        print("[gen] ⚠ 源包不在：%s ⇒ 这一批 clip 的 `cab` 记 ?" % p)
        return {}
    try:
        import UnityPy
    except Exception as e:                                    # pragma: no cover
        print("[gen] ⚠ 没有 UnityPy（%s）⇒ `cab` 记 ?" % e)
        return {}
    env = UnityPy.load(p)
    out = {}
    for o in env.objects:
        if o.type.name == "AnimationClip" and o.path_id in pids:
            out[o.path_id] = getattr(getattr(o, "assets_file", None), "name", None)
    del env
    return out


def animation_refs(pkg):
    """`{PathID: [Animation 组件文件名, …]}` —— 该包里 `Animation.m_Animation` 指到的 clip。

    ⚠️ 只看**本包**的 `Animation/`（clip 只可能被同一场景里的 `Animation` 播）；
      `m_FileID` 在解包树里是**该包 externals 的下标**，不是「同一份 CAB」，所以这里不拿它当判据。
    """
    out = {}
    d = os.path.join(ASSETS, pkg, ANIMATION_DIR)
    if not os.path.isdir(d):
        return out
    for p in sorted(glob.glob(os.path.join(d, "*.json"))):
        j = _read_json(p)
        ptr = j.get("m_Animation") or {}
        pid = ptr.get("m_PathID") or 0
        if pid:
            out.setdefault(pid, []).append(os.path.basename(p))
    return out


def clip_files(pkg):
    return sorted(glob.glob(os.path.join(ASSETS, pkg, CLIP_DIR, "*.json")))


def want_clip(pkg, d):
    """这条 clip 收不收 —— 判据全部写在一处（别在两处各写一遍）。"""
    if not d.get("m_Legacy") or d.get("m_MuscleClipSize", 0) != 0:
        return False                       # muscle / non-legacy 只能重打包
    if pkg == SHARED_PKG:
        # 共享包：HUD 那 8 条属战斗 HUD 线（gen_battlehud_clips.py 已导出）
        return not d.get("m_Name", "").startswith(HUD_PREFIX)
    return True


def build_clip(pkg, path, d, used_by, cab):
    name = d.get("m_Name", "")
    out = {
        "name": name,
        "arena": arena_key(pkg) if pkg != SHARED_PKG else "shared",
        "usedBy": used_by,
        "legacy": bool(d.get("m_Legacy")),
        "muscleClipSize": int(d.get("m_MuscleClipSize", 0)),
        "sampleRate": _f(d.get("m_SampleRate", 60.0)),
        "wrapMode": WRAP_NAMES.get(d.get("m_WrapMode"), str(d.get("m_WrapMode"))),
        "length": 0.0,
        "keysTotal": 0,
        "prePostInfinity": "clamp",
        "src": {
            "pkg": pkg,
            "clip": os.path.basename(path),
            "pid": int(os.path.basename(path)[len("AnimationClip_"):-len(".json")]),
            "cab": cab,
        },
        "curves": [],
    }

    curves = out["curves"]
    for field, prop, dim, with_order in TRANSFORM_KINDS:
        for c in (d.get(field) or []):
            cv = c.get("curve") or {}
            assert cv.get("m_PreInfinity") == 2 and cv.get("m_PostInfinity") == 2, (name, field)
            item = {"type": "Transform", "property": prop, "path": c.get("path", "")}
            if with_order:
                item["rotationOrder"] = int(cv.get("m_RotationOrder"))
            item["keys"] = _keys(cv, dim)
            curves.append(item)

    for c in (d.get("m_FloatCurves") or []):
        cv = c.get("curve") or {}
        cid = c.get("classID")
        if cid not in CLASS_NAMES:
            raise SystemExit("[gen] 🔴 %s 的浮点曲线 classID=%r 没有映射（不许瞎标类型）" % (name, cid))
        assert cv.get("m_PreInfinity") == 2 and cv.get("m_PostInfinity") == 2, (name, "m_FloatCurves")
        s = c.get("script") or {}
        item = {"type": CLASS_NAMES[cid], "property": c.get("attribute", ""),
                "path": c.get("path", ""), "classID": int(cid),
                "script": {"file": int(s.get("m_FileID", 0)), "pid": int(s.get("m_PathID", 0))}}
        item["keys"] = _keys(cv, 1)
        curves.append(item)

    out["keysTotal"] = sum(len(c["keys"]) for c in curves)
    out["length"] = max((k["t"] for c in curves for k in c["keys"]), default=0.0)
    return out


def build_all():
    pkgs = scene_pkgs()
    if len(pkgs) != 13:
        raise SystemExit("[gen] 🔴 战场包 %d 个（预期 13）：%s" % (len(pkgs), pkgs))

    # ① 收集（保持 pkg/文件顺序，最后统一排序）
    picked = []            # [(pkg, path, 原始 dict)]
    for pkg in pkgs + [SHARED_PKG]:
        for path in clip_files(pkg):
            d = _read_json(path)
            if want_clip(pkg, d):
                picked.append((pkg, path, d))

    # ② usedBy：按「哪个场的 Animation.m_Animation 指着它的 PathID」现算
    refs = {pkg: animation_refs(pkg) for pkg in pkgs}
    shared_refs = {}
    for pkg in pkgs:
        for pid, comps in refs[pkg].items():
            shared_refs.setdefault(pid, []).append(arena_key(pkg))
    for pid in refs.get(SHARED_PKG, {}):                      # battleshared 自己的 Animation（若将来有）
        shared_refs.setdefault(pid, [])

    clips = []
    for pkg, path, d in picked:
        pid = int(os.path.basename(path)[len("AnimationClip_"):-len(".json")])
        if pkg == SHARED_PKG:
            used_by = sorted(shared_refs.get(pid, []))
        else:
            used_by = [arena_key(pkg)] if pid in refs[pkg] else []
        cabs = cab_of_clips(pkg, {pid})
        clips.append(build_clip(pkg, path, d, used_by, cabs.get(pid) or "?"))

    clips.sort(key=lambda c: (c["arena"] not in ("shared",), c["arena"], c["name"], c["src"]["pid"]))
    return {
        "schema": 1,
        "note": ("由 工具/gen_arena_scene_clips.py 生成。判据 = assets_full 的 "
                 "bundle_scenes_scenes_battlearena*/AnimationClip + bundle_battlesharedresources_assets_all。"
                 "只收 legacy（m_Legacy && m_MuscleClipSize==0）；5 条 muscle clip 走重打包"
                 "（工具/extract_missing_shaders.py --arena-anims）。🔴 切线（si/so）必须逐键保留 —— "
                 "本批大量关键帧带非零切线，new Keyframe(t,v) 会把它变成 ease-in/out（端点照样对得上）。"
                 "⚠️ 键名用 si/so（与 camera_intro_clips.json 一致；battlehud_clips.json 用的是 is/os，同义）。"),
        "source": ASSETS,
        "bundles": BUNDLE_DIR,
        "clips": clips,
    }


def _max_slope(c, prop_prefix):
    vals = [abs(x) for cur in c["curves"] if cur["property"].startswith(prop_prefix)
            for k in cur["keys"] for key in ("si", "so") for x in k[key]]
    return max(vals) if vals else 0.0


def self_check(doc):
    """落盘前的**独立**核对（期望值全部从原 JSON / 原包再读一遍，⛔ 不拿实现证明实现）。"""
    clips = doc["clips"]
    assert len(clips) == 22, "clip 数 %d != 22" % len(clips)
    assert len({c["name"] for c in clips}) == 21, "名字数 != 21（有且只有 EC 相机 intro 重名）"
    assert sum(1 for c in clips if c["arena"] == "shared") == 1, "共享包应收恰好 1 条"
    assert [c["name"] for c in clips if c["arena"] == "shared"] == [SHARED_EXPECT], \
        [c["name"] for c in clips if c["arena"] == "shared"]

    # ① 逐条：字段 / 键数 / 长度 —— 期望值从原 JSON **再读一遍**
    for c in clips:
        raw = _read_json(os.path.join(ASSETS, c["src"]["pkg"], CLIP_DIR, c["src"]["clip"]))
        assert raw["m_Name"] == c["name"], (c["name"], raw["m_Name"])
        assert c["legacy"] is True and c["muscleClipSize"] == 0, c["name"]
        assert raw.get("m_Compressed") is False, c["name"]
        for k in ("m_CompressedRotationCurves", "m_PPtrCurves", "m_Events"):
            assert not (raw.get(k) or []), (c["name"], k)
        assert c["src"]["cab"] and c["src"]["cab"] != "?", ("查不到 CAB", c["name"])
        # 键数：原 JSON 逐类求和 == 产物逐类求和
        n_raw = 0
        for field, _p, _d, _o in TRANSFORM_KINDS:
            for cv in (raw.get(field) or []):
                n_raw += len(cv["curve"]["m_Curve"])
        for cv in (raw.get("m_FloatCurves") or []):
            n_raw += len(cv["curve"]["m_Curve"])
        assert n_raw == c["keysTotal"], (c["name"], n_raw, c["keysTotal"])
        t_raw = 0.0
        for cur in c["curves"]:
            assert cur["keys"], (c["name"], cur["property"])
            ts = [k["t"] for k in cur["keys"]]
            assert ts == sorted(ts), ("键时间没递增", c["name"], cur["property"])
        t_raw = max(k["t"] for cur in c["curves"] for k in cur["keys"])
        assert abs(t_raw - c["length"]) < 1e-9, (c["name"], t_raw, c["length"])
        # weightedMode 必须全 0（不存权重的前提）；pre/post 已由 build_clip 断言
        for field in ("m_PositionCurves", "m_EulerCurves", "m_RotationCurves", "m_ScaleCurves", "m_FloatCurves"):
            for cv in (raw.get(field) or []):
                for k in cv["curve"]["m_Curve"]:
                    assert k.get("weightedMode", 0) == 0, ("weightedMode 不是 0", c["name"], field)
                if field == "m_EulerCurves":
                    assert cv["curve"].get("m_RotationOrder") == 4, (c["name"], "rotationOrder")

    # ② 抽三条逐值对账（位置/旋转/浮点各一条）
    by = {(c["arena"], c["name"]): c for c in clips}
    big = by[("battlearenatauviorla", "Railgun BIG")]
    raw = _read_json(os.path.join(ASSETS, big["src"]["pkg"], CLIP_DIR, big["src"]["clip"]))
    rot = [x for x in big["curves"] if x["property"] == "m_LocalRotation"]
    assert len(rot) == 1 and len(rot[0]["keys"]) == 1129, len(rot[0]["keys"]) if rot else "no rot"
    for a, b in zip(raw["m_RotationCurves"][0]["curve"]["m_Curve"], rot[0]["keys"]):
        assert abs(a["time"] - b["t"]) < 1e-12
        for i, ax in enumerate("xyzw"):
            assert abs(a["value"][ax] - b["v"][i]) < 1e-12
            assert abs(a["inSlope"][ax] - b["si"][i]) < 1e-12
    floats = [x for x in big["curves"] if x["type"] != "Transform"]
    assert [x["property"] for x in floats] == ["m_IsActive", "m_IsActive", "m_Enabled"], \
        [x["property"] for x in floats]
    assert [x["classID"] for x in floats] == [1, 1, 114], [x["classID"] for x in floats]
    assert floats[2]["script"] == {"file": 1, "pid": 8834507010450676941}, floats[2]["script"]

    ec = by[("battlearenaemperorschildren", "Emperor's Children Scene Animations")]
    raw = _read_json(os.path.join(ASSETS, ec["src"]["pkg"], CLIP_DIR, ec["src"]["clip"]))
    assert ec["keysTotal"] == 288, ec["keysTotal"]
    assert [len(x["keys"]) for x in ec["curves"]][:3] == [len(c["curve"]["m_Curve"])
                                                          for c in raw["m_PositionCurves"][:3]]

    bl = by[("battlearenablacklegion", "Black Legion Scene Animation")]
    raw = _read_json(os.path.join(ASSETS, bl["src"]["pkg"], CLIP_DIR, bl["src"]["clip"]))
    assert bl["keysTotal"] == 139 and abs(bl["length"] - 15.9826) < 1e-4, (bl["keysTotal"], bl["length"])
    bl_float = [x for x in bl["curves"] if x.get("classID") == 137]
    assert len(bl_float) == 11 and bl_float[0]["property"].startswith("blendShape.Key"), \
        ("Black Legion 的浮点曲线应该是 11 条 blendShape", len(bl_float))

    # ③ 三条「结构性」事实（⛔ 别按名字当键）
    ec_name = by[("battlearenaemperorschildren", "Battle Arena Emperors Children Camera Intro")]["name"]
    sw = [c for c in clips if c["arena"] == "battlearenaspacewolves"]
    assert len(sw) == 1 and sw[0]["name"] == ec_name, (sw, ec_name)
    assert sw[0]["src"]["pkg"] == "bundle_scenes_scenes_battlearenaspacewolves", sw[0]["src"]
    # 共享那条只被 arena3 引用（现算，不是抄 J-A）
    assert by[("shared", SHARED_EXPECT)]["usedBy"] == ["battlearena3"], by[("shared", SHARED_EXPECT)]["usedBy"]
    # 每场都在用自己那份 camera intro（13 场逐一）
    for c in clips:
        if c["name"].endswith("Camera Intro") or c["name"] == "Battle Arena Leviathan":
            assert c["usedBy"], ("这条相机 intro 没有任何 Animation 引用？", c["name"])

    # ④ 切线非零（灭自证：证明 si/so 不是全 0 的摆设）
    assert _max_slope(big, "m_LocalRotation") > 0.0, "Railgun BIG 旋转曲线应该有非零切线"
    assert _max_slope(by[("battlearenatauviorla", "Railgun Turret 1")], "m_LocalPosition") > 0.0
    assert _max_slope(by[("battlearenaastramilitarum", "Astra Militarum Scene Animations")],
                      "m_LocalRotation") > 0.0

    # ⑤ 打印（逐条：clip 名 / 曲线数 / 键数 / 时长 / 源包 + CAB）
    print("[gen] %-44s %-8s %-6s %-9s %-6s %s" %
          ("clip", "curves", "keys", "length", "wrap", "src pkg @ CAB"))
    for c in clips:
        print("  · %-42s %-8d %-6d %-9.3f %-6s %s @ %s"
              % (c["name"], len(c["curves"]), c["keysTotal"], c["length"], c["wrapMode"],
                 c["src"]["pkg"], c["src"]["cab"]))
    print("[gen] 自检通过：22 条 legacy（21 名字）· 键数逐条等于原 JSON · 切线已保留 · "
          "weightedMode 全 0 · pre/post 全 clamp · euler 全 ZXY")


def render(doc):
    return json.dumps(doc, ensure_ascii=False, indent=1, sort_keys=False) + "\n"


def main(argv):
    check = "--check" in argv
    doc = build_all()
    self_check(doc)
    txt = render(doc)
    if check:
        old = io.open(DST, "r", encoding="utf-8").read() if os.path.exists(DST) else None
        if old != txt:
            print("[gen] ✗ 与 %s 不一致（重跑一次不带 --check 即可写出）" % DST)
            return 1
        print("[gen] ✅ --check：%s 与现生成一致" % DST)
        return 0
    d = os.path.dirname(DST)
    if not os.path.isdir(d):
        os.makedirs(d)
    # 新文件一律 LF（CLAUDE.md：别让脚本翻行尾）
    with io.open(DST, "w", encoding="utf-8", newline="\n") as f:
        f.write(txt)
    print("[gen] 写出 %s（%d 字节）" % (DST, len(txt.encode("utf-8"))))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
