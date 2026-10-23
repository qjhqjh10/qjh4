#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_misc_clips.py — A2「未导出」里**剩下那 11 条 legacy clip** 的曲线表生成器
（战场 22 条已由 `gen_arena_scene_clips.py` 导出 · HUD 8 条 `gen_battlehud_clips.py` ·
升段窗 3 条 `gen_division_clips.py` · 相机 intro 13 条 `gen_camera_intro_clips.py`）。

输入（只读）：`d:/2/新解包资源/assets_full/` 下这些包的 `AnimationClip/` + `Animation/`
  · `bundle_generalgamewindows_assets_all` — B3 `Tutorial Pointer Menu`
  · `bundle_scenes_scenes_mainmenuwarpforge` — B4 `Level Up Card`
  · `bundle_duplicateassetisolation_assets_all` — B6 `Card Display Open`
  · `bundle_boosterpacks_assets_all` — B7 `Booster Open Standard` / `Booster Open Sororitas`
  · `bundle_menus_assets_all` — `SearchingOpponentCog`
  · `bundle_battleprefabs_vfxandmisc_assets_all` — `Rupturous Ruination` /
    `Astra Militarum Scene Valkyrie Dawn Attack` / `SweepBackAndForth_narrow` /
    `Dark Angels Void Combat animations` / `LightAnimationOrbit`
输出：
  · `d:/4/Unity/数据/游戏数据/misc_clips.json`（全量件 · ✅ 可进 git）
  ⛔ **不写运行期副本**：这 11 条**目前没有运行期消费者**（各自的消费方在别的线的文件里，
    见每条 `runtimeConsumer` 字段）⇒ 与 `gen_arena_scene_clips.py` 同口径，只落全量数据。
    将来要接时，镜像件该落在 `MyGame/Assets/CardPresentation/Resources/misc_clips.json`
    —— 那个目录不归本工具管（铁律 13·3 文件所有权），由主对话另行安排。

## 判据链（逐条，2026-10-23 现核）

**① 这 11 条是怎么选出来的（不是照抄 A2 清单）**
把 `assets_full` 里**全部 99 个 `AnimationClip` 对象（98 个名字）** 逐个读，按三条并集算「已经有产物」：
  (a) 工程 `.anim`：`MyGame/Assets/WarpforgeVFX/Animations/*.anim` —— 47 个名字
  (b) 数据件：`arena_scene_clips.json`(22 对象/21 名) ∪ `battlehud_clips.json`(8) ∪ `division_clips.json`(3)
      ∪ `camera_intro_clips.json`(13，是 arena 那份的子集)
  (c) 重打包包：`wf_arena_anims_*.bundle`(5 muscle + 5 controller + 9 legacy) ·
      `wf_boosters_extra`(2) · `wf_prefabs_extra`(6 clip + 1 controller)
⇒ 剩下 **19 个名字** = 8 条 muscle（重打包，7 条已进包、`Recorded` 仍无产物）+ **11 条 legacy（本文件）**。

**🔴 ② 对 A2 清单的两处订正（铁律 5）**
A2 §1·1 把 `Astra Militarum Scene Valkyrie Dawn Attack` 与 `SweepBackAndForth_narrow` 归进
「**原版 0 条曲线 ⇒ 本来就什么都不动**」。**不成立** —— A2 只看了 `m_FloatCurves`：
  · `Astra Militarum Scene Valkyrie Dawn Attack`：`m_ScaleCurves 1`（2 键）· `m_RotationCurves 1`（4 键）·
    `m_PositionCurves 1`（3 键），`m_FloatCurves 0`。
  · `SweepBackAndForth_narrow`：`m_EulerCurves 1`（3 键，0→2.0 s），其余全 0。
  两条都是 **legacy（`m_MuscleClipSize=0`）**，曲线数据本地全有 ⇒ **是真缺口，已在本文件导出**。
  ⚠️ 但两条**在原版快照里都 0 处引用**（`m_PathID` 全库搜：Animation / MonoBehaviour / AnimatorController
  三类都没有指向它们）⇒ 是**原版孤儿**，导出后只作备用（见 `orphan: true`）。

**③ `Rupturous Ruination` 也是孤儿**（A2 §三已记：`Rapturous Ruination Board` 指向的是
`Emperor's Children Environmental Condition 1`）—— 但它本身有 2 条 float 曲线（`bloomIntensity` / `lutBlend`），
导出备用。

**④ 曲线类型（classID → 绑定类型名，逐条实读，⛔ 不按印象猜）**
`1`=GameObject · `23`=Renderer（`material.*` 材质属性曲线）· `108`=Light · `114`=MonoBehaviour（带 script PPtr）
· `224`=RectTransform。认不出的 classID **直接报错停手**。
⚠️ `Level Up Card` 的 `floatProperty1.value` / `floatProperty2.vector4Value.x` 绑在
`AnimateUIMaterialProperty`（script pid `-1887485317901086766`）—— **我们工程里没有这个类**
（`gen_division_clips.py` 已记同一件事）⇒ 存下来是**真值**，重建时若要驱动得走自己的通道。

**⑤ 为什么把 `si` / `so`（inSlope / outSlope）也存下来**
同 `gen_camera_intro_clips.py` / `gen_division_clips.py`：只存 `(t, v)` 再用 `new Keyframe(t,v)` 重建，
会把每一段都变成 ease-in/out，**端点仍然对得上** ⇒ 端点断言抓不到。
本批实测有大量非零切线（例：`Booster Open Standard` 的 121 键旋转、`Level Up Card` 的 5 键缩放）。

**⑥ `length` 口径**：= **全部曲线最晚的键**（legacy clip 的 `m_MuscleClip.m_StopTime` 恒 1.0，不能当长度）。
`Level Up Card` 的 `AnimationFinished` 事件时刻（3.4833 s）与长度逐位相同 —— 自检里点名核。

## schema（与 `arena_scene_clips.json` 对齐，多加三个「这笔账归谁」的字段）
```
{ "schema":1, "note":"…", "source":"…", "bundles":"…",
  "clips":[
    { "name":"Card Display Open", "key":"B6", "pkg":"bundle_duplicateassetisolation_assets_all",
      "legacy":true, "muscleClipSize":0, "compressed":false, "sampleRate":60.0,
      "wrapMode":"Once", "length":0.3166667, "keysTotal":… , "prePostInfinity":"clamp",
      "orphan":false,
      "alreadyRepacked":null,            # 已在哪个重打包包里（有就写包名，没有 = null）
      "runtimeConsumer":"…",             # 谁播它 / 我们现在的等价实现
      "owners":[ {"pkg":…, "animation":"Animation_1608.json", "go":"Card Display Window",
                  "playAutomatically":true, "animations":1} ],
      "src":{ "pkg":…, "clip":"AnimationClip_1560242982351263260.json", "pid":…, "cab":"…" },
      "events":[ {"t":…, "function":"…"} ],
      "curves":[
        { "type":"Transform", "property":"m_LocalRotation", "path":"Card Display/Cards/CardUI",
          "keys":[ {"t":0.0,"v":[x,y,z,w],"si":[..4],"so":[..4]}, … ] },
        { "type":"RectTransform", "property":"m_AnchoredPosition.x", "path":"…", "classID":224,
          "script":{"file":0,"pid":0}, "keys":[ {"t":…,"v":[..],"si":[..],"so":[..]} ] }, … ] } ] }
```
- `v`/`si`/`so` 分量数：3 = 位置/欧拉/缩放 · 4 = 四元数 · 1 = 浮点曲线。
- `localEulerAnglesRaw` 是 Unity 运行期 `AnimationClip.SetCurve` 的绑定名（原 JSON 只有 `m_EulerCurves`
  + `m_RotationOrder`）—— 这一格不是从原始数据读的，重建时以 SetCurve 实测为准
  （同 `gen_arena_scene_clips.py` 的说明）。

## 幂等
跑两遍逐字节相同（clip 按名字升序、曲线按 position→euler→rotation→scale→float 固定类序 + 原序、无时间戳）。
`--check` 只比对不写盘。
用法：
    python -I -X utf8 d:/4/Unity/工具/gen_misc_clips.py
    python -I -X utf8 d:/4/Unity/工具/gen_misc_clips.py --check
"""
import glob
import io
import json
import os
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n    python -I -X utf8 d:/4/Unity/工具/gen_misc_clips.py\n    python -I -X utf8 d:/4/Unity/工具/gen_misc_clips.py --check\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

ASSETS = r"d:/2/新解包资源/assets_full"
BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
DST = r"d:/4/Unity/数据/游戏数据/misc_clips.json"

# clip 名 → (账本键, 原版出处包, 已在哪个重打包包, 谁播它 / 我们的等价值, 是否原版孤儿)
TARGETS = {
    "Tutorial Pointer Menu": (
        "B3", "bundle_generalgamewindows_assets_all", None,
        "两个 `Glow (1)` 预制体上的 Animation 组件（playAutomatically=true）。"
        "我们的 `Shell/TutorialPointerUi.cs` 只做两态静值（scale 1/0.7、light alpha 0/1），没有这条脉冲曲线。",
        False),
    "Level Up Card": (
        "B4", "bundle_scenes_scenes_mainmenuwarpforge", None,
        "主菜单场景 `CardUI` 上的 `Animation_622`（playAutomatically=false，靠代码 Play）。"
        "我们工程里没有「卡升级」这套演出（全工程 grep `LevelUp` 无运行期命中）。",
        False),
    "Card Display Open": (
        "B6", "bundle_duplicateassetisolation_assets_all", None,
        "13 个战场场景 + 主菜单的 `Card Display Window`（各一个 Animation 组件，playAutomatically=true）。"
        "值已逐键抄进 `Core/CardFan.cs`（OpenTime=0.3167s、槽 0–4 真值、槽 5–8 外推）⇒ 本件是**真值备份 + 自检对照**，不改运行期。",
        False),
    "Booster Open Standard": (
        "B7", "bundle_boosterpacks_assets_all", "wf_boosters_extra.bundle",
        "16 个 `Booster Pack Standard *` 外观 prefab 上的 Animation 组件（playAutomatically=true）。"
        "我们的 `BoosterPackOpenWindow` 画纯色背景、用不上；原件已在 `wf_boosters_extra.bundle`。",
        False),
    "Booster Open Sororitas": (
        "B7", "bundle_boosterpacks_assets_all", "wf_boosters_extra.bundle",
        "`Booster Pack Standard Sororitas` 上的 Animation 组件（playAutomatically=true）。同上。",
        False),
    "SearchingOpponentCog": (
        "B-extra", "bundle_menus_assets_all", None,
        "5 个 `Skull` 上的 Animation 组件（匹配窗转盘）。值已逐键搬进 `Shell/SearchingMatchPopup.CogAngleAt`"
        "（37 键 / Z 0→360° / 10.0167s）⇒ 本件是**真值备份**，不改运行期。",
        False),
    "Dark Angels Void Combat animations": (
        "C-tail", "bundle_battleprefabs_vfxandmisc_assets_all", "wf_prefabs_extra.bundle",
        "按 **assetGUID** 被环境混合组件 `ScenarioAnimationBlend` 取（原版无 pathID 引用）；"
        "运行时由 `WarpforgeAnimatorBridge` 从 `wf_prefabs_extra` 取原件。本件是数据备份。",
        False),
    "LightAnimationOrbit": (
        "C-tail", "bundle_battleprefabs_vfxandmisc_assets_all", "wf_prefabs_extra.bundle",
        "同上（GUID 引用 / `wf_prefabs_extra` 原件）。⚠️ 只有 1 条 `m_Intensity` float 曲线。",
        False),
    "Rupturous Ruination": (
        "C-tail", "bundle_battleprefabs_vfxandmisc_assets_all", None,
        "**原版孤儿**：`Rapturous Ruination Board` 指向的是 `Emperor's Children Environmental Condition 1`，"
        "全库无引用。有 2 条 float 曲线（`bloomIntensity` / `lutBlend`）⇒ 导出备用。",
        True),
    "Astra Militarum Scene Valkyrie Dawn Attack": (
        "C-tail", "bundle_battleprefabs_vfxandmisc_assets_all", None,
        "**原版孤儿**（全库 0 处 pathID 引用）。🔴 A2 原判「0 条曲线、本来就什么都不动」**不成立**："
        "它有 scale 2 键 / rotation 4 键 / position 3 键（float 0 条）⇒ 导出备用。",
        True),
    "SweepBackAndForth_narrow": (
        "C-tail", "bundle_battleprefabs_vfxandmisc_assets_all", None,
        "**原版孤儿**（全库 0 处 pathID 引用）。🔴 A2 原判「0 条曲线」**不成立**："
        "它有 1 条 euler 曲线（3 键 / 0→2.0 s）⇒ 导出备用。",
        True),
}

CLIP_DIR = "AnimationClip"
ANIMATION_DIR = "Animation"

# UnityEngine.WrapMode：0=Default 1=Once 2=Loop 4=PingPong 8=ClampForever（序列化值）
WRAP_NAMES = {0: "Default", 1: "Once", 2: "Loop", 4: "PingPong", 8: "ClampForever"}
# 浮点曲线的 `classID` → 绑定类型名（本批实读出现过的全部值；认不出就停手）
CLASS_NAMES = {1: "GameObject", 23: "Renderer", 108: "Light", 114: "MonoBehaviour",
               137: "SkinnedMeshRenderer", 224: "RectTransform"}
# 四类变换曲线：(JSON 字段, 绑定属性名, 分量数, 是否带 rotationOrder)
TRANSFORM_KINDS = [
    ("m_PositionCurves", "m_LocalPosition", 3, False),
    ("m_EulerCurves", "localEulerAnglesRaw", 3, True),
    ("m_RotationCurves", "m_LocalRotation", 4, False),
    ("m_ScaleCurves", "m_LocalScale", 3, False),
]


def _f(x):
    """JSON 里保持 float（Unity `JsonUtility` 的 float 字段）。"""
    return float(x)


def _read_json(p):
    with io.open(p, "r", encoding="utf-8") as f:
        return json.load(f)


def _vec(d, dim):
    if dim == 1:
        return [_f(d)]
    return [_f(d[c]) for c in "xyzw"[:dim]]


def _keys(curve, dim):
    return [{"t": _f(k["time"]),
             "v": _vec(k["value"], dim),
             "si": _vec(k["inSlope"], dim),
             "so": _vec(k["outSlope"], dim)}
            for k in curve["m_Curve"]]


def _bundle_path(pkg):
    """assets_full 的目录名是 `bundle_<bundle 文件名去后缀>` ⇒ 还原成真包路径。"""
    return os.path.join(BUNDLE_DIR, pkg.replace("bundle_", "", 1) + ".bundle")


def cab_of(pkg, pids):
    """`{pid: 内层 CAB 名}` —— 从**真包**里现读（assets_full 的 JSON 不带 CAB 信息）。"""
    p = _bundle_path(pkg)
    if not os.path.isfile(p):
        print("[gen] ⚠ 源包不在：%s ⇒ 这一批的 `cab` 记 ?" % p)
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


def _go_index(pkg_dir):
    """`Animation.m_GameObject`（GO 的 PathID）→ GO 名。

    assets_full 的 `GameObject/*.json` **不带 PathID 字段**（有的文件名带、有的不带）⇒
    只能从 `Transform/RectTransform.m_GameObject` + `GameObject.m_Component` 两头对上。
    """
    tfset, tf2go, tf2name = set(), {}, {}
    for folder in ("Transform", "RectTransform"):
        for f in glob.glob(os.path.join(pkg_dir, folder, "*.json")):
            b = os.path.basename(f)[:-5]
            if "_" not in b:
                continue
            try:
                pid = int(b.rsplit("_", 1)[1])
            except ValueError:
                continue
            tfset.add(pid)
            tf2go[pid] = (_read_json(f).get("m_GameObject") or {}).get("m_PathID")
    for f in glob.glob(os.path.join(pkg_dir, "GameObject", "*.json")):
        d = _read_json(f)
        for c in (d.get("m_Component") or []):
            cp = (c.get("component") or {}).get("m_PathID")
            if cp in tfset:
                tf2name[cp] = d.get("m_Name")
    out = {}
    for tf, go in tf2go.items():
        if go and tf in tf2name:
            out.setdefault(go, tf2name[tf])
    return out


_GOIDX = {}


def _go_index_cached(pkg):
    if pkg not in _GOIDX:
        _GOIDX[pkg] = _go_index(os.path.join(ASSETS, pkg))
    return _GOIDX[pkg]


def owners_of(pkg, pid):
    """「`m_Animation` 或 `m_Animations` 指着这条 clip」的全部 Animation 组件。

    🔴 **跨包**：`Card Display Open` 的 clip 在 `duplicateassetisolation`，但播它的是
    13 个战场场景包 + 主菜单包（那些 `Animation` 的 `m_FileID != 0`）⇒ 必须跨包搜。
    搜索范围 = **本包 + 全部 `bundle_scenes_scenes_*` 场景包**（其余包没有场景级 Animation 需求，
    收紧范围是为了避免 PathID 跨包撞号 —— A2 报告 §2·1 已记过这类坑）。
    """
    pkgs = [pkg] + sorted(d for d in os.listdir(ASSETS)
                          if d.startswith("bundle_scenes_scenes_") and d != pkg)
    out = []
    for pk in pkgs:
        d = os.path.join(ASSETS, pk, ANIMATION_DIR)
        if not os.path.isdir(d):
            continue
        for p in sorted(glob.glob(os.path.join(d, "*.json"))):
            j = _read_json(p)
            pids = [(j.get("m_Animation") or {}).get("m_PathID")] + \
                   [e.get("m_PathID") for e in (j.get("m_Animations") or [])]
            if pid not in pids:
                continue
            out.append({
                "pkg": pk,
                "animation": os.path.basename(p),
                "go": _go_index_cached(pk).get((j.get("m_GameObject") or {}).get("m_PathID")),
                "playAutomatically": bool(j.get("m_PlayAutomatically")),
                "animations": len(j.get("m_Animations") or []),
            })
    return out


def build_clip(name, meta, path):
    key, pkg, repacked, consumer, orphan = meta
    d = _read_json(path)
    if d.get("m_Name") != name:
        raise SystemExit("[gen] 🔴 %s 里 `m_Name` = %r" % (path, d.get("m_Name")))
    if not d.get("m_Legacy"):
        raise SystemExit("[gen] 🔴 `%s` 不是 legacy ⇒ muscle/streamed 拷不出曲线，停手。" % name)
    if d.get("m_MuscleClipSize", 0) != 0:
        raise SystemExit("[gen] 🔴 `%s` 的 `m_MuscleClipSize` = %s ⇒ muscle，停手。"
                         % (name, d.get("m_MuscleClipSize")))
    if d.get("m_Compressed"):
        raise SystemExit("[gen] 🔴 `%s` 是压缩曲线，停手。" % name)
    if d.get("m_PPtrCurves"):
        raise SystemExit("[gen] 🔴 `%s` 有 PPtrCurves（对象引用曲线），本文件不覆盖。" % name)

    pid = int(os.path.basename(path)[len("AnimationClip_"):-len(".json")])
    cur = {
        "name": name,
        "key": key,
        "pkg": pkg,
        "legacy": True,
        "muscleClipSize": 0,
        "compressed": False,
        "sampleRate": _f(d.get("m_SampleRate", 60.0)),
        "wrapMode": WRAP_NAMES.get(d.get("m_WrapMode"), str(d.get("m_WrapMode"))),
        "length": 0.0,
        "keysTotal": 0,
        "prePostInfinity": "clamp",
        "orphan": orphan,
        "alreadyRepacked": repacked,
        "runtimeConsumer": consumer,
        "owners": owners_of(pkg, pid),
        "src": {"pkg": pkg, "clip": os.path.basename(path), "pid": pid,
                "cab": cab_of(pkg, {pid}).get(pid)},
        "events": [{"t": _f(e["time"]), "function": e.get("functionName") or ""}
                   for e in (d.get("m_Events") or [])],
        "curves": [],
    }

    for field, prop, dim, with_order in TRANSFORM_KINDS:
        for c in (d.get(field) or []):
            cv = c.get("curve") or {}
            if cv.get("m_PreInfinity") != 2 or cv.get("m_PostInfinity") != 2:
                raise SystemExit("[gen] 🔴 %s/%s 的 pre/postInfinity != 2" % (name, field))
            item = {"type": "Transform", "property": prop, "path": c.get("path", "")}
            if with_order:
                item["rotationOrder"] = int(cv.get("m_RotationOrder"))
            item["keys"] = _keys(cv, dim)
            cur["curves"].append(item)

    for c in (d.get("m_FloatCurves") or []):
        cv = c.get("curve") or {}
        cid = c.get("classID")
        if cid not in CLASS_NAMES:
            raise SystemExit("[gen] 🔴 %s 的浮点曲线 classID=%r 没有映射（⛔ 不许瞎标类型）" % (name, cid))
        if cv.get("m_PreInfinity") != 2 or cv.get("m_PostInfinity") != 2:
            raise SystemExit("[gen] 🔴 %s 的 float 曲线 pre/postInfinity != 2" % name)
        s = c.get("script") or {}
        cur["curves"].append({
            "type": CLASS_NAMES[cid],
            "property": c.get("attribute", ""),
            "path": c.get("path", ""),
            "classID": int(cid),
            "script": {"file": int(s.get("m_FileID", 0)), "pid": int(s.get("m_PathID", 0))},
            "keys": _keys(cv, 1),
        })

    cur["keysTotal"] = sum(len(c["keys"]) for c in cur["curves"])
    cur["length"] = max((k["t"] for c in cur["curves"] for k in c["keys"]), default=0.0)
    return cur


def build_all():
    clips = []
    for name in sorted(TARGETS):
        key, pkg, repacked, consumer, orphan = TARGETS[name]
        paths = glob.glob(os.path.join(ASSETS, pkg, CLIP_DIR, "*.json"))
        hit = [p for p in paths if _read_json(p).get("m_Name") == name]
        if len(hit) != 1:
            raise SystemExit("[gen] 🔴 `%s` 在 %s 里命中 %d 次（预期 1）" % (name, pkg, len(hit)))
        clips.append(build_clip(name, TARGETS[name], hit[0]))
    return {
        "schema": 1,
        "note": ("由 工具/gen_misc_clips.py 生成。判据 = assets_full 的各包 AnimationClip + Animation。"
                 "🔴 切线（si/so）必须逐键保留：只存 (t,v) 重建会把每段变成 ease-in/out，端点断言抓不到。"
                 "🔴 A2 订正：`Astra Militarum Scene Valkyrie Dawn Attack` 与 `SweepBackAndForth_narrow` "
                 "**不是**「0 曲线」—— 前者有 scale/rotation/position，后者有 euler 3 键（A2 只看了 m_FloatCurves）。"
                 "⛔ 本文件没有运行期副本：这 11 条目前没有运行期消费者（见每条 runtimeConsumer）。"),
        "source": ASSETS,
        "bundles": BUNDLE_DIR,
        "clips": clips,
    }


# ---------------------------------------------------------------- 独立自检
def self_check(doc):
    clips = doc["clips"]
    names = [c["name"] for c in clips]
    assert len(clips) == 11, "clip 数 %d != 11" % len(clips)
    assert names == sorted(names), "未按名字升序"
    assert sorted(names) == sorted(TARGETS), names

    # ① 每条都 legacy / 未压缩 / 无 PPtr，且 length = 全部键最晚时刻（从产物重算）
    for c in clips:
        assert c["legacy"] is True and c["muscleClipSize"] == 0 and c["compressed"] is False, c["name"]
        assert c["length"] == max((k["t"] for cu in c["curves"] for k in cu["keys"]), default=0.0), c["name"]
        assert c["keysTotal"] == sum(len(cu["keys"]) for cu in c["curves"]), c["name"]
        assert c["curves"], "%s 一条曲线都没有（A2 说其中两条是 0 曲线 —— 正相反，别抄）" % c["name"]

    by = {c["name"]: c for c in clips}

    # ② 🔴 对 A2「0 条曲线」那两条的**反向**断言（防将来有人按 A2 把它俩删掉还能过）
    assert len(by["Astra Militarum Scene Valkyrie Dawn Attack"]["curves"]) == 3
    assert len(by["SweepBackAndForth_narrow"]["curves"]) == 1
    assert by["SweepBackAndForth_narrow"]["curves"][0]["property"] == "localEulerAnglesRaw"
    assert len(by["SweepBackAndForth_narrow"]["curves"][0]["keys"]) == 3

    # ③ 抽核端点值（从**原始 clip JSON 重读**，不读本生成器产物）
    def raw(name, finder):
        d = _read_json(os.path.join(ASSETS, by[name]["src"]["pkg"], CLIP_DIR, by[name]["src"]["clip"]))
        for c in (d.get("m_FloatCurves") or []):
            if finder(c):
                return c["curve"]["m_Curve"]
        raise AssertionError((name, finder))

    k = raw("Card Display Open", lambda c: c.get("path", "").endswith("CardUI") and c.get("attribute") == "m_AnchoredPosition.x")
    assert abs(k[-1]["time"] - 0.3166666626930237) < 1e-9, k[-1]["time"]
    k = raw("Tutorial Pointer Menu", lambda c: c.get("attribute") == "m_Color.a")
    assert len(k) == 2 and k[-1]["time"] == 1.0, k
    k = raw("SearchingOpponentCog", lambda c: False) if False else None  # euler 不在 float 里，见下
    e = _read_json(os.path.join(ASSETS, by["SearchingOpponentCog"]["src"]["pkg"], CLIP_DIR,
                                by["SearchingOpponentCog"]["src"]["clip"]))["m_EulerCurves"][0]["curve"]["m_Curve"]
    assert len(e) == 37 and abs(e[-1]["time"] - 10.016666412353516) < 1e-9, (len(e), e[-1]["time"])
    b = _read_json(os.path.join(ASSETS, by["Booster Open Standard"]["src"]["pkg"], CLIP_DIR,
                                by["Booster Open Standard"]["src"]["clip"]))
    assert len(b["m_RotationCurves"][0]["curve"]["m_Curve"]) == 121, "Booster Open Standard 旋转键数变了"

    # ④ 事件与长度自洽（`Level Up Card` 的 AnimationFinished 必须 == length）
    lv = by["Level Up Card"]
    assert [e["function"] for e in lv["events"]] == ["ChangeFrame", "AnimationFinished"], lv["events"]
    assert abs(lv["events"][-1]["t"] - lv["length"]) < 1e-9, (lv["events"][-1]["t"], lv["length"])

    # ⑤ 非零切线和四元数四分量真的存下来了（⛔ 别把「全 0 / 只存 3 分量」的旧口径抄回来）
    def max_slope(c):
        return max(abs(x) for cu in c["curves"] for k in cu["keys"] for x in (k["si"] + k["so"]))
    assert max_slope(by["Booster Open Standard"]) > 0.1, max_slope(by["Booster Open Standard"])
    rot = [cu for cu in by["Card Display Open"]["curves"] if cu["property"] == "m_LocalRotation"]
    assert rot and all(len(k["v"]) == 4 and len(k["si"]) == 4 for cu in rot for k in cu["keys"]), "四元数不是 4 分量"

    # ⑥ 归属判据（谁播它）不能被悄悄清空：有播放点的三条必须有 owners
    for nm in ("Tutorial Pointer Menu", "Level Up Card", "Card Display Open",
               "Booster Open Standard", "Booster Open Sororitas", "SearchingOpponentCog"):
        assert by[nm]["owners"], "%s 的 owners 是空的（Owner 解析坏了？）" % nm
    assert len(by["Card Display Open"]["owners"]) == 14, len(by["Card Display Open"]["owners"])
    assert by["Level Up Card"]["owners"][0]["go"] == "CardUI", by["Level Up Card"]["owners"]
    assert by["Tutorial Pointer Menu"]["owners"][0]["go"] == "Glow (1)"

    # ⑦ 孤儿三条（原版真没引用）—— 反向断言，防止将来被当成「我们漏接」
    for nm in ("Rupturous Ruination", "Astra Militarum Scene Valkyrie Dawn Attack", "SweepBackAndForth_narrow"):
        assert by[nm]["orphan"] is True and not by[nm]["owners"], nm

    print("[gen] 自检通过：11 条 legacy clip · 曲线全非空 · 端点/事件/键数抽核一致 · 四元数 4 分量 · owners 命中")
    for c in clips:
        print("  · %-46s len=%7.4f keys=%4d curves=%2d owners=%2d repacked=%s"
              % (c["name"], c["length"], c["keysTotal"], len(c["curves"]), len(c["owners"]),
                 c["alreadyRepacked"] or "-"))


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
            print("[gen] ✗ 与 %s 不一致" % DST)
            return 1
        print("[gen] ✅ --check：产物与现生成一致")
        return 0
    d = os.path.dirname(DST)
    if not os.path.isdir(d):
        os.makedirs(d)
    # 新文件一律 LF（CLAUDE.md：别让脚本翻行尾）
    with io.open(DST, "w", encoding="utf-8", newline="\n") as f:
        f.write(txt)
    print("[gen] 写出 %s（%d 字节）" % (DST, len(txt.encode("utf-8"))))
    print("[gen] clip 数 = %d · 长度 = %s" % (len(doc["clips"]),
                                             sorted(round(c["length"], 4) for c in doc["clips"])))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
