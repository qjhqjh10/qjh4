#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_division_clips.py — 排位「升段窗」(`RankedEventDivisionChangeWindowNewSystem`) 的
**三条动画曲线表生成器**。

输入（只读）：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`
  · `Animation/Animation_8653451351532409166.json`  —— 挂在 owner GO
    `Ranked Division Change Window` 上的那个 `Animation` 组件
  · `AnimationClip/*.json` —— 三条 **legacy** clip：`Division Config` · `Rank Up` · `Division Up`
输出：
  · `d:/4/Unity/数据/游戏数据/division_clips.json`（全量件）
  · `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/division_clips.json`（运行期副本）

## 判据链（逐条，2026-10-22 现核）
- **同一个 `Animation` 组件** `Animation_8653451351532409166`：`m_GameObject` = 根 GO
  `Ranked Division Change Window`（`m_PathID -3030737852390665906`，与窗 MB
  `MonoBehaviour_-4938671765695524530.json` 的 `m_GameObject` 同一个）；
  `m_Animations` = [`Division Up`, `Rank Up`, `Division Config`]；`m_Animation`（默认 clip）= `Division Config`；
  `m_PlayAutomatically = false`（⇒ 不会自己播，全靠代码 `Play(name)`）。
- **播放点（第一权威 = 反编译）**：
  · `RankedEventDivisionChangeWindowNewSystem__Open.c:210-211`
    → `Animation.Play("Division Config")`（**布局/复位**，紧跟在 `MilestonesStepDisplay.Show(...)` 之后）
  · `Open.c:223-240` → `delay = MilestonesStepDisplay.GetTotalAnimationTime() - 0.25`，
    起协程 `DoPromoteAnimations(delay, isDivisionChange, previousDivision)`
  · `DoPromoteAnimations_d__22.MoveNext.c` → `WaitForSeconds(delay)` → 音效 cue `Division Change` → 分叉：
      `DoRankChangeAnimations`  → `Animation.Play("Rank Up")`    · `DoDivisionChangeAnimations` → `Animation.Play("Division Up")`
    （两条字符串常量用 `d:/2/tools/il2cpp_out/stringliteral.json` 解出：
      `0x1842a6950` → `"Rank Up"`、`0x184282580` → `"Division Up"`；RVA = 地址 − 0x180000000）
- **`Division Config` 不是演出**：它只有单键（`Mission Milestones Progress` 是 0→1 的 0.2167 s 淡入），
  作用是把窗体复位到「全隐藏」的初始态。⇒ 播放语义 = 开窗即播、不参与升段/分段的分叉。
- **三条全是 legacy 曲线**（`m_Legacy = true` · `m_MuscleClipSize = 0` · `m_Compressed = false` ·
  `m_CompressedRotationCurves` 空 · 无 streamed）⇒ 曲线数据本地全有，运行期用
  `AnimationClip.SetCurve` 重建（同 `工具/gen_camera_intro_clips.py` 那条先例）。
- **曲线目标**（8 条 path，全部相对 owner GO；生成时会去 prefab 树里逐条核存在性）：
  `Division Image` · `Division Image/Rank Icon` · `Division Image/Rank Icon/Rank Icon Glow` ·
  `Text Content` · `Title` · `Mission Milestones Progress` · `Particles DivisionUp` ·
  `Particles DivisionUp/Sparks Burn` · `Particles Rank Up`。
  🔴 `Particles Rank Up` **原版有**（纯 `Transform` 根，子件 `Sparks Burn` → `Glow Big` /
  `Promote Letter Effect` / `Sparks fast` / `Wave Shine`）—— 我们那扇窗**没建**它（本次补上）。

## 🔴 为什么把 `inSlope/outSlope` 也存下来（同 `gen_camera_intro_clips.py` 的教训）
只存 `(t, v)` 再用 `new Keyframe(t, v)` 重建，会把每一段都变成 **ease-in/out**。这三条里有大量
**非零切线**（例：`Division Image` 的 5 键缩放，2.6 s→2.65 s 那一记「砸下去」的 `outSlope` 是 0.2035、
2.65 s 的 `inSlope` 却是 0 ⇒ 若按 0 切线重建，回弹的**进/出速度**都会变，端点仍对得上 ⇒ 端点断言抓不到）。
⇒ 本生成器逐键把 `si`/`so` 存进 JSON，运行期逐键写回 `new Keyframe(t, v, inTangent, outTangent)`。

## 曲线类型 → 运行期通道（`channel` 字段；判据 = `classID` + `script` PPtr 实读）
| 原版 attribute | classID | script PPtr | 原版类型 | channel |
|---|---|---|---|---|
| （ScaleCurves 无 attribute） | 4 | — | `Transform` | `scale` |
| `m_Color.a` | 114 | `350208831926335389` | `UnityEngine.UI.Image` | `alpha` |
| `m_Alpha` | 225 | — | `CanvasGroup` | `groupAlpha` |
| `m_IsActive` | 1 | — | `GameObject` | `active` |
| `ShapeModule.m_Scale.x/y` | 198 | — | `ParticleSystem` | `particleShapeScale` |
| `m_fontColor.a` | 114 | `-5411878308140533209` | TMP 文本 | `textColorAlpha` |
| `floatProperty1.value` | 114 | `-1887485317901086766` | `AnimateUIMaterialProperty` | `materialFloat` |
⚠️ `EverguildTextMeshPro` / `AnimateUIMaterialProperty` **我们工程里没有**这两个类
（现核 `grep -rn "class EverguildTextMeshPro" --include=*.cs` 与 `class AnimateUIMaterialProperty` 各零命中）
⇒ 运行期建 clip 时这两族用 `typeof(MonoBehaviour)` 占位（曲线条数照样等于 JSON 条数），
而**真正把值画出来**走我们自己的手动通道（见 `RankedDivisionChangeWindow.cs` 的 `ApplyChannel`）。

## schema（运行期 `[Serializable]` 类逐字对应）
```
{ "schema":1, "note":"…", "source":"…", "owner":"Ranked Division Change Window",
  "animation":{ "file":"…", "pid":…, "playAutomatically":false, "wrapMode":0,
                "defaultClip":"Division Config", "order":[…], "clips":[{"name":…,"pid":…}] },
  "clips":[
    { "key":"rankUp", "name":"Rank Up", "legacy":true, "sampleRate":60.0,
      "wrapMode":"Default", "loopTime":false, "length":3.9166667,
      "src":{ "pkg":"…", "clip":"AnimationClip_4418588262184220504.json", "pid":4418588262184220504 },
      "events":[ {"t":3.9166667,"function":"AnimationFinished"} ],
      "curves":[
        { "path":"Division Image", "type":"Transform", "property":"localScale.x", "channel":"scale",
          "srcAttribute":"", "classId":4, "keys":[ {"t":0.3833333,"v":0.8,"si":1.0268638,"so":1.0268638}, … ] },
        …
      ] }, …
  ] }
```
`length` = **全部曲线最晚的键**（`Rank Up` / `Division Up` 与各自 `AnimationFinished` 事件时刻逐位相同）。

## 幂等
跑两遍逐字节相同（无时间戳、顺序 = 原 JSON 顺序）。`--check` 只比对不写盘。
用法：
    python -I -X utf8 d:/4/Unity/工具/gen_division_clips.py
    python -I -X utf8 d:/4/Unity/工具/gen_division_clips.py --check
"""
import glob
import io
import json
import os
import sys

ASSETS = r"d:/2/新解包资源/assets_full"
PKG = "bundle_menus_assets_all"
DST_FULL = r"d:/4/Unity/数据/游戏数据/division_clips.json"
DST_RES = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/division_clips.json"

OWNER = "Ranked Division Change Window"
ANIMATION_FILE = "Animation_8653451351532409166.json"
ANIMATION_PID = 8653451351532409166
# 期望的三条（`m_Animations` 的顺序，逐字）
EXPECT_CLIPS = ["Division Up", "Rank Up", "Division Config"]

# 曲线类型表：classID (+ script PPtr) → (原版类型名, 运行期通道)
SCRIPT_IMAGE = 350208831926335389        # UnityEngine.UI.Image
SCRIPT_TMP = -5411878308140533209        # TMP 文本（`EverguildTextMeshPro`）
SCRIPT_MATPROP = -1887485317901086766    # AnimateUIMaterialProperty
CLASS_GO = 1
CLASS_TRANSFORM = 4
CLASS_MB = 114
CLASS_PARTICLESYSTEM = 198
CLASS_CANVASGROUP = 225

# clip 名 → 运行期键（**运行期只按 key 查**，⛔ 不按 `m_Name`）
KEYS = {
    "Division Config": "divisionConfig",
    "Rank Up": "rankUp",
    "Division Up": "divisionUp",
}


def _f(x):
    """JSON 里保持 float（Unity `JsonUtility` 的 float 字段）。"""
    return float(x)


def _read_json(p):
    with io.open(p, "r", encoding="utf-8") as f:
        return json.load(f)


def _attr_of(class_id, script_pid):
    """→ (类型名, channel)。⛔ 认不出的类型直接报错，别静默放过。"""
    if class_id == CLASS_GO:
        return "GameObject", "active"
    if class_id == CLASS_CANVASGROUP:
        return "CanvasGroup", "groupAlpha"
    if class_id == CLASS_PARTICLESYSTEM:
        return "ParticleSystem", "particleShapeScale"
    if class_id == CLASS_MB:
        if script_pid == SCRIPT_IMAGE:
            return "Image", "alpha"
        if script_pid == SCRIPT_TMP:
            return "TMP_Text", "textColorAlpha"
        if script_pid == SCRIPT_MATPROP:
            return "AnimateUIMaterialProperty", "materialFloat"
    raise SystemExit("[gen] 🔴 未登记的曲线类型 classID=%s script=%s" % (class_id, script_pid))


def _explode_scale(path, curve):
    """ScaleCurves → 3 条标量曲线（`localScale.x/y/z`）。"""
    out = []
    for i, ax in enumerate("xyz"):
        out.append({
            "path": path,
            "type": "Transform",
            "property": "localScale." + ax,
            "channel": "scale",
            "srcAttribute": "",
            "classId": CLASS_TRANSFORM,
            "keys": [{
                "t": _f(k["time"]),
                "v": _f(k["value"][ax]),
                "si": _f(k["inSlope"][ax]),
                "so": _f(k["outSlope"][ax]),
            } for k in curve["m_Curve"]],
        })
    return out


def _explode_float(path, entry):
    """FloatCurves（已经是标量）→ 1 条曲线。"""
    c = entry["curve"]
    typ, channel = _attr_of(entry.get("classID"), (entry.get("script") or {}).get("m_PathID") or 0)
    attr = entry.get("attribute") or ""
    return [{
        "path": path,
        "type": typ,
        "property": attr,
        "channel": channel,
        "srcAttribute": attr,
        "classId": entry.get("classID") or 0,
        "keys": [{
            "t": _f(k["time"]),
            "v": _f(k["value"]),
            "si": _f(k["inSlope"]),
            "so": _f(k["outSlope"]),
        } for k in c["m_Curve"]],
    }]


def build_clip(name, path):
    d = _read_json(path)
    if d.get("m_Name") != name:
        raise SystemExit("[gen] 🔴 %s 里 `m_Name` = %r，预期 %r" % (path, d.get("m_Name"), name))
    # 🔴 第 1 步：必须是 legacy（muscle/streamed 拷不出曲线 ⇒ 当场停手）
    if not d.get("m_Legacy"):
        raise SystemExit("[gen] 🔴 `%s` 不是 legacy（`m_Legacy=false`）⇒ 曲线不能这样重建，停手。" % name)
    if d.get("m_MuscleClipSize", 0) != 0:
        raise SystemExit("[gen] 🔴 `%s` 的 `m_MuscleClipSize` = %s（预期 0）⇒ muscle clip，停手。"
                         % (name, d.get("m_MuscleClipSize")))
    if d.get("m_Compressed"):
        raise SystemExit("[gen] 🔴 `%s` 是压缩曲线（`m_Compressed=true`），停手。" % name)
    for kind in ("m_PositionCurves", "m_EulerCurves", "m_RotationCurves"):
        if d.get(kind):
            raise SystemExit("[gen] 🔴 `%s` 里有 %s —— 本生成器只为「缩放 + float」写，遇到就要先补一条判断" % (name, kind))

    curves = []
    for c in d.get("m_ScaleCurves") or []:
        curves.extend(_explode_scale(c["path"], c["curve"]))
    for c in d.get("m_FloatCurves") or []:
        curves.extend(_explode_float(c["path"], c))

    length = 0.0
    for c in curves:
        for k in c["keys"]:
            if k["t"] > length:
                length = k["t"]

    events = [{"t": _f(e["time"]), "function": e.get("functionName") or ""}
              for e in (d.get("m_Events") or [])]

    return {
        "key": KEYS[name],
        "name": name,
        "legacy": True,
        "sampleRate": _f(d.get("m_SampleRate", 60.0)),
        "wrapMode": "Default" if d.get("m_WrapMode") == 0 else str(d.get("m_WrapMode")),
        "loopTime": False,          # legacy clip 的 JSON 里没有 `m_LoopTime`（= Unity 默认 false）
        "length": length,
        "src": {
            "pkg": PKG,
            "clip": os.path.basename(path),
            "pid": int(os.path.basename(path).split("_")[-1].split(".")[0]),
        },
        "events": events,
        "curves": curves,
    }


# ---------------------------------------------------------------- prefab 树（独立判据）
def _gameobject_name_by_transform():
    """Transform/RectTransform 的 `m_PathID` → GameObject 名。

    每个 GameObject JSON 的 `m_Component` 里第一颗是 Transform/RectTransform ——
    用「这个 pid 有没有对应的 `Transform_/RectTransform_` 文件」来认，⛔ 不按顺序硬取。
    """
    pkg = os.path.join(ASSETS, PKG)
    have = set()
    for folder in ("RectTransform", "Transform"):
        for p in glob.glob(os.path.join(pkg, folder, "*.json")):
            have.add(int(os.path.basename(p).rsplit("_", 1)[1].split(".")[0]))
    m = {}
    for p in glob.glob(os.path.join(pkg, "GameObject", "*.json")):
        d = _read_json(p)
        for c in d.get("m_Component") or []:
            pid = c["component"]["m_PathID"]
            if pid in have:
                m[pid] = d.get("m_Name")
                break
    return m


def _walk_prefab_paths():
    """从 owner GO 走一遍 prefab 树，返回「相对路径集合」。"""
    pkg = os.path.join(ASSETS, PKG)
    names = _gameobject_name_by_transform()

    def read_tf(pid):
        for folder in ("RectTransform", "Transform"):
            p = os.path.join(pkg, folder, "%s_%d.json" % (folder, pid))
            if os.path.exists(p):
                return folder, _read_json(p)
        return None, None

    # 根：按名字找 GameObject，再取它的 Transform/RectTransform
    root_go = os.path.join(pkg, "GameObject", OWNER + ".json")
    if not os.path.exists(root_go):
        raise SystemExit("[gen] 🔴 找不到 owner GO：%s" % root_go)
    root_pid = None
    for c in _read_json(root_go).get("m_Component") or []:
        pid = c["component"]["m_PathID"]
        _, tf = read_tf(pid)
        if tf is not None:
            root_pid = pid
            break
    if root_pid is None:
        raise SystemExit("[gen] 🔴 owner GO 上找不到 Transform/RectTransform")

    out = set()

    def walk(pid, prefix):
        _, tf = read_tf(pid)
        if tf is None:
            return
        for ch in tf.get("m_Children") or []:
            cp = ch["m_PathID"]
            nm = names.get(cp)
            if nm is None:
                raise SystemExit("[gen] 🔴 子节点 %d 反查不到 GameObject 名" % cp)
            rel = nm if not prefix else prefix + "/" + nm
            out.add(rel)
            walk(cp, rel)

    walk(root_pid, "")
    return out


def build_all():
    pkg = os.path.join(ASSETS, PKG)
    ad = _read_json(os.path.join(pkg, "Animation", ANIMATION_FILE))
    if (ad.get("m_GameObject") or {}).get("m_PathID") != -3030737852390665906:
        raise SystemExit("[gen] 🔴 `%s` 不是挂在 owner GO 上的（`m_GameObject` 对不上）" % ANIMATION_FILE)
    if ad.get("m_PlayAutomatically"):
        raise SystemExit("[gen] 🔴 `%s` 的 `m_PlayAutomatically = true` —— 与「全靠代码 Play(name)」的判据不符"
                         % ANIMATION_FILE)

    order = []
    for e in ad.get("m_Animations") or []:
        pid = e["m_PathID"]
        p = os.path.join(pkg, "AnimationClip", "AnimationClip_%d.json" % pid)
        order.append((pid, _read_json(p)["m_Name"], p))
    if [x[1] for x in order] != EXPECT_CLIPS:
        raise SystemExit("[gen] 🔴 `m_Animations` = %s，预期 %s"
                         % ([x[1] for x in order], EXPECT_CLIPS))
    default_pid = (ad.get("m_Animation") or {}).get("m_PathID")
    default_name = next(x[1] for x in order if x[0] == default_pid)

    clips = [build_clip(nm, p) for _, nm, p in order]
    clips.sort(key=lambda c: c["key"])

    return {
        "schema": 1,
        "note": ("由 工具/gen_division_clips.py 生成。判据 = assets_full/%s/Animation + AnimationClip"
                 "（三条 legacy 曲线）。🔴 切线（si/so）必须逐键保留：`Division Image` 的 5 键缩放"
                 "等段带非零 in/out 斜率，new Keyframe(t,v) 会变成 ease-in/out（端点断言抓不到）。"
                 "路径一律相对 owner GO `%s`。" % (PKG, OWNER)),
        "source": ASSETS,
        "owner": OWNER,
        "animation": {
            "file": "Animation/" + ANIMATION_FILE,
            "pid": ANIMATION_PID,
            "playAutomatically": False,
            "wrapMode": int(ad.get("m_WrapMode") or 0),
            "defaultClip": default_name,
            "order": [x[1] for x in order],
            "clips": [{"name": nm, "pid": pid} for pid, nm, _ in order],
        },
        "clips": clips,
    }


def self_check(doc):
    """落盘前的独立核对（期望值从原始 JSON / prefab 树再读一遍，⛔ 不拿实现自证）。"""
    clips = doc["clips"]
    assert len(clips) == 3, "clip 数 %d != 3" % len(clips)
    by = {c["name"]: c for c in clips}
    assert sorted(by) == sorted(EXPECT_CLIPS), sorted(by)

    # ① 三条都 legacy，长度与事件时刻自洽
    for c in clips:
        assert c["legacy"] is True, c["name"]
        assert abs(c["sampleRate"] - 60.0) < 1e-6, c["name"]
        assert c["wrapMode"] == "Default", c["name"]
        for ev in c["events"]:
            assert abs(ev["t"] - c["length"]) < 1e-9, \
                "%s: 事件 %s s 与 length %s 不等" % (c["name"], ev["t"], c["length"])
    # ② 曲线条数（逐条数出来的期望值 —— 缩放 3 分量、float 每条 1 条）
    assert len(by["Division Config"]["curves"]) == 14, len(by["Division Config"]["curves"])
    assert len(by["Rank Up"]["curves"]) == 16, len(by["Rank Up"]["curves"])
    assert len(by["Division Up"]["curves"]) == 14, len(by["Division Up"]["curves"])

    # ③ 抽核：几个「一眼能认」的端点值（从原始 clip JSON 再读一遍，不读本生成器产物）
    pkg = os.path.join(ASSETS, PKG)

    def raw(name, attr_find):
        p = os.path.join(pkg, "AnimationClip", by[name]["src"]["clip"])
        d = _read_json(p)
        for c in d.get("m_FloatCurves") or []:
            if c.get("path") == attr_find[0] and c.get("attribute") == attr_find[1]:
                return c["curve"]["m_Curve"]
        raise AssertionError((name, attr_find))

    k = raw("Division Config", ("Division Image", "m_Color.a"))
    assert k[0]["value"] == 0.0
    k = raw("Rank Up", ("Mission Milestones Progress", "m_Alpha"))
    assert k[0]["value"] == 1.0 and k[-1]["value"] == 0.0
    k = raw("Division Up", ("Particles DivisionUp/Sparks Burn", "ShapeModule.m_Scale.x"))
    assert abs(k[-1]["value"] - 1.15) < 1e-6, k[-1]["value"]
    assert abs(k[0]["value"] - 0.3) < 1e-6, k[0]["value"]

    # ④ 非零切线真的存下来了（⛔ 别把「全 0」的旧口径抄回来）
    def max_slope(c):
        return max(abs(x) for cur in c["curves"] for key in cur["keys"] for x in (key["si"], key["so"]))
    assert max_slope(by["Rank Up"]) > 1.0, max_slope(by["Rank Up"])
    assert max_slope(by["Division Up"]) > 1.0, max_slope(by["Division Up"])

    # ⑤ 🔴 每条曲线的 path 都必须在 prefab 树里真的存在（独立判据：现走 prefab 的 Transform 树）
    paths = _walk_prefab_paths()
    for c in clips:
        for cur in c["curves"]:
            assert cur["path"] in paths, \
                "%s: path `%s` 在 prefab 树里不存在（现有 %d 条路径）" % (c["name"], cur["path"], len(paths))
    # `Particles Rank Up` 必须存在（本次要补的那一颗）—— 单独点名，防止将来被「顺手删掉」还能过
    assert "Particles Rank Up" in paths, "prefab 树里没有 `Particles Rank Up`"
    assert "Particles DivisionUp/Sparks Burn" in paths, "prefab 树里没有 `Particles DivisionUp/Sparks Burn`"

    # ⑥ 缩放首键 = 0.8 且倍率 = 1.0（三条都从 0.8 起步；`Rank Up` 另有 `Rank Icon` 的 1.09033）
    want = {
        "Division Config": [("Division Image", 0.8)],
        "Rank Up": [("Division Image", 0.8), ("Division Image/Rank Icon", 1.09033)],
        "Division Up": [("Division Image", 0.8)],
    }
    for nm in EXPECT_CLIPS:
        got = {cur["path"]: cur for cur in by[nm]["curves"] if cur["property"] == "localScale.x"}
        assert sorted(got) == sorted(p for p, _ in want[nm]), (nm, sorted(got))
        for p, v in want[nm]:
            assert abs(got[p]["keys"][0]["v"] - v) < 1e-4, (nm, p, got[p]["keys"][0]["v"])
        # 每条缩放曲线的 3 个分量都必须齐（x/y/z 各一条）
        for p in got:
            comps = sorted(cur["property"] for cur in by[nm]["curves"]
                           if cur["channel"] == "scale" and cur["path"] == p)
            assert comps == ["localScale.x", "localScale.y", "localScale.z"], (nm, p, comps)

    print("[gen] 自检通过：3 条 legacy clip · 曲线 14/16/14 · 路径全部命中 prefab 树 · 非零切线已保留")
    for c in clips:
        print("  · %-16s len=%.4f keys=%3d events=%s paths=%s"
              % (c["name"], c["length"],
                 sum(len(x["keys"]) for x in c["curves"]),
                 [e["function"] for e in c["events"]],
                 sorted(set(x["path"] for x in c["curves"]))))


def render(doc):
    return json.dumps(doc, ensure_ascii=False, indent=1, sort_keys=False) + "\n"


def main(argv):
    check = "--check" in argv
    doc = build_all()
    self_check(doc)
    txt = render(doc)
    if check:
        for p in (DST_FULL, DST_RES):
            old = io.open(p, "r", encoding="utf-8").read() if os.path.exists(p) else None
            if old != txt:
                print("[gen] ✗ 与 %s 不一致" % p)
                return 1
        print("[gen] ✅ --check：两份产物与现生成一致")
        return 0
    for p in (DST_FULL, DST_RES):
        d = os.path.dirname(p)
        if not os.path.isdir(d):
            os.makedirs(d)
        # 新文件一律 LF（CLAUDE.md：别让脚本翻行尾）
        with io.open(p, "w", encoding="utf-8", newline="\n") as f:
            f.write(txt)
        print("[gen] 写出 %s（%d 字节）" % (p, len(txt.encode("utf-8"))))
    print("[gen] clip 数 = %d · 长度 = %s" % (len(doc["clips"]),
                                             sorted(round(c["length"], 4) for c in doc["clips"])))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
