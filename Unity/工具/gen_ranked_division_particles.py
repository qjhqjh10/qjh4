# -*- coding: utf-8 -*-
"""gen_ranked_division_particles.py —— 把「段位变化窗」两棵粒子树（`Particles DivisionUp` / `Particles Rank Up`）
逐节点摊成一份运行时 DTO（账 `个人资料_成就_排行:221`）。

为什么要有它（2026-10-29 · `B97_PROFILE`）：
  原版 `RankedEventDivisionChangeWindowNewSystem.ChangeParticleEffectAndSpriteRank(Image, ParticleSystem, Sprite)`
  的 ③④ 两跳要「给 `targetParticleSystem` 的 `TextureSheetAnimation` 设 sprite、并把它的 `Renderer.enabled`
  跟空判绑起来」，而那两颗 `ParticleSystem` 在序列化数据里指向的是
    · `divisionParticleSystem` = ps `1201952928742936910` = **DivisionUp ▸ Sparks Burn ▸ Promote Letter Effect**
    · `rankParticleSystem`     = ps `-6284612905696259762` = **Rank Up ▸ Sparks Burn ▸ Promote Letter Effect**
  （判据 = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-4938671765695524530.json` 的两个 PPtr）
  ⇒ 这两棵树必须先按原版建出来（今天我们只建了空容器 + `NoteNoArt`）。

**每一条都是现读**（⛔ 没有一个是猜的）：
  · 树/父链/名字/组件 = `bundle_menus_assets_all/{GameObject,Transform,RectTransform}/*.json`，按
    `Transform.m_Children` 逐级下钻（**同名子件各 2 份** —— `Sparks Burn` / `Glow Big` / `Glow below` /
    `Promote Letter Effect` / `Sparks fast` 各有两份，分属两棵树）⇒ 本工具**按 `m_Father` 链配对**，
    不按文件名后缀、不按字典序（⚠️ 文件名后缀只是导出器给同名 GO 的序号，与树无关）。
  · 粒子参数 = `ParticleSystem/ParticleSystem_<pid>.json`（79 份、`m_Name` 全是 null ⇒ **只能按 pid 反查**）。
  · 渲染器 = `ParticleSystemRenderer/<pid>.json`（renderMode / sortingOrder / pivot / 两个长度量 / 材质 pid）。
  · 材质名 = 材质 json 的 `m_Name`（材质本体常在 `bundle_duplicateassetisolation_assets_all`）。

**两件必须说清的事**（写进产物注释，也写进报告）：
  ① `Particles Rank Up` 与 `Wave Shine` / `Wave Shine R` / `Trails R` / `Trails L` 是**裸 `Transform`**
     （原版 16768 个节点里只有 258 个是裸 `Transform`，粒子件占其中一部分）；
  ② 两棵树的**根**在 1px=1 单位的画布上带自己的缩放（`Rank Up` 根 `localScale = 164.237`、
     `DivisionUp` 根 `1`）⇒ 运行时把**根**整体缩到 `1/108`（本工程 `LayoutSpace`：108 px = 1 世界单位），
     子树以下**逐字照抄**（见 `Shell/RankedDivisionParticles.cs` 文件头那一段推导）。

用法（**默认只打印用量，不写盘**）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_ranked_division_particles.py --apply
  -h / --help   只打印用法并退出（退出码 0）

产物：`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json`
"""
import io
import json
import math
import os
import re
import sys

B = r"d:/2/新解包资源/assets_full/bundle_menus_assets_all"
MAT_DIRS = [os.path.join(B, "Material"),
            r"d:/2/新解包资源/assets_full/bundle_duplicateassetisolation_assets_all/Material"]
OUT = (r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/"
       r"ranked_division_particles.json")

ROOTS = ("Particles DivisionUp", "Particles Rank Up")

USAGE = """用法（默认不写盘）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \\
      d:/4/Unity/工具/gen_ranked_division_particles.py --apply
  -h / --help   只打印本用法并退出（退出码 0）

做的事：把段位变化窗的两棵粒子树（`Particles DivisionUp` / `Particles Rank Up`）逐节点摊成
        `Resources/Art/ranked_division_particles.json`（树 + 粒子模块 + 渲染器 + 材质名）。
        只读 `d:/2/新解包资源/`，只写那一个文件。
"""


def _load(p):
    return json.load(io.open(p, encoding="utf-8"))


def _exists(kind, pid):
    return os.path.isfile(os.path.join(B, kind, "%s_%d.json" % (kind, pid)))


def _tr_field(pid):
    """一个 Transform/RectTransform pid -> (kind, json)。"""
    for kind in ("RectTransform", "Transform"):
        p = os.path.join(B, kind, "%s_%d.json" % (kind, pid))
        if os.path.isfile(p):
            return kind, _load(p)
    raise IOError("no transform %d" % pid)


def _index_gos():
    """goPathID -> {name, comps, trPid, trKind}（按 `Transform.m_GameObject` 反查，⛔ 不靠文件名）。"""
    out = {}
    gdir = os.path.join(B, "GameObject")
    for fn in os.listdir(gdir):
        try:
            d = _load(os.path.join(gdir, fn))
        except Exception:
            continue
        comps = [c["component"]["m_PathID"] for c in d.get("m_Component", [])]
        tp = None
        for c in comps:
            if _exists("RectTransform", c):
                tp = c
                break
            if _exists("Transform", c):
                tp = c
                break
        if tp is None:
            continue
        kind, t = _tr_field(tp)
        out[t["m_GameObject"]["m_PathID"]] = dict(name=d.get("m_Name"), comps=comps, tr=tp, kind=kind)
    return out


def _mm(cur):
    if not cur:
        return None
    pts = [{"t": p.get("time", 0.0), "v": p.get("value", 0.0)}
           for p in ((cur.get("maxCurve") or {}).get("m_Curve") or [])]
    return {"mode": cur.get("minMaxState", 0), "a": cur.get("scalar", 0.0),
            "b": cur.get("minScalar", 0.0), "curve": pts}


def _col(c):
    return ({"r": c["r"], "g": c["g"], "b": c["b"], "a": c["a"]} if c
            else {"r": 1.0, "g": 1.0, "b": 1.0, "a": 1.0})


def _grad(g):
    if not g:
        return None
    mg = g.get("maxGradient") or {}
    nc = int(mg.get("m_NumColorKeys", 0) or 0)
    na = int(mg.get("m_NumAlphaKeys", 0) or 0)
    ck = [{"t": mg.get("ctime%d" % i, 0.0),
           "r": (mg.get("key%d" % i) or {}).get("r", 0.0),
           "g": (mg.get("key%d" % i) or {}).get("g", 0.0),
           "b": (mg.get("key%d" % i) or {}).get("b", 0.0)} for i in range(min(nc, 8))]
    ak = [{"t": mg.get("atime%d" % i, 0.0), "a": (mg.get("key%d" % i) or {}).get("a", 1.0)}
          for i in range(min(na, 8))]
    return {"mode": g.get("minMaxState", 0), "ca": _col(g.get("minColor")),
            "cb": _col(g.get("maxColor")), "colorKeys": ck, "alphaKeys": ak}


def _mat_name(pid):
    """材质 pid -> `m_Name`（先本包、再 duplicateassetisolation）。"""
    for d in MAT_DIRS:
        p = os.path.join(d, "Material_%d.json" % pid)
        if os.path.isfile(p):
            return _load(p).get("m_Name")
    return None


def _ps_blob(ps_pid, pr_pid):
    ps = _load(os.path.join(B, "ParticleSystem", "ParticleSystem_%d.json" % ps_pid))
    pr = (_load(os.path.join(B, "ParticleSystemRenderer", "ParticleSystemRenderer_%d.json" % pr_pid))
          if pr_pid else {})
    ini = ps.get("InitialModule") or {}
    shp = ps.get("ShapeModule") or {}
    emi = ps.get("EmissionModule") or {}
    siz = ps.get("SizeModule") or {}
    rot = ps.get("RotationModule") or {}
    cm = ps.get("ColorModule") or {}
    vel = ps.get("VelocityModule") or {}
    uv = ps.get("UVModule") or {}
    noi = ps.get("NoiseModule") or {}
    trl = ps.get("TrailModule") or {}
    bursts = [{"time": b.get("time", 0.0),
               "count": (b.get("countCurve") or {}).get("scalar", 0.0),
               "cycles": b.get("cycleCount", 1),
               "interval": b.get("repeatInterval", 0.01)}
              for b in (emi.get("m_Bursts") or [])]
    mats = pr.get("m_Materials") or []
    mat_pid = None
    for m in mats:
        if m and m.get("m_PathID"):
            mat_pid = m["m_PathID"]
            break
    return {
        "length": ps.get("lengthInSec", 5.0),
        "looping": bool(ps.get("looping")),
        "playOnAwake": bool(ps.get("playOnAwake")),
        "startDelay": (ps.get("startDelay") or {}).get("scalar", 0.0),
        "simulationSpeed": ps.get("simulationSpeed", 1.0),
        "scalingMode": ps.get("scalingMode", 0),
        "maxParticles": ini.get("maxNumParticles", 1000),
        "life": _mm(ini.get("startLifetime")), "speed": _mm(ini.get("startSpeed")),
        "size": _mm(ini.get("startSize")), "size3D": bool(ini.get("size3D")),
        "color": _grad(ini.get("startColor")), "rot": _mm(ini.get("startRotation")),
        "gravity": _mm(ini.get("gravityModifier")),
        "shapeEnabled": bool(shp.get("enabled")),
        "shapeType": shp.get("type", 0), "shapeAngle": shp.get("angle", 25.0),
        "shapeRadius": shp.get("radius", -1.0), "shapeRadiusThick": shp.get("radiusThickness", 1.0),
        "shapeScaleX": (shp.get("m_Scale") or {}).get("x", 1.0),
        "shapeScaleY": (shp.get("m_Scale") or {}).get("y", 1.0),
        "shapeScaleZ": (shp.get("m_Scale") or {}).get("z", 1.0),
        "shapePosX": (shp.get("m_Position") or {}).get("x", 0.0),
        "shapePosY": (shp.get("m_Position") or {}).get("y", 0.0),
        "shapeRotX": (shp.get("m_Rotation") or {}).get("x", 0.0),
        "shapeRotY": (shp.get("m_Rotation") or {}).get("y", 0.0),
        "alignToDirection": bool(shp.get("alignToDirection")),
        "emissionEnabled": bool(emi.get("enabled")),
        "rate": _mm(emi.get("rateOverTime")), "bursts": bursts,
        "sizeOverLife": _mm(siz.get("curve")), "rotOverLife": _mm(rot.get("x")),
        "colorOverLife": _grad(cm.get("gradient")),
        "velX": _mm(vel.get("x")), "velY": _mm(vel.get("y")),
        "uvEnabled": bool(uv.get("enabled")), "uvTilesX": uv.get("tilesX", 1),
        "uvTilesY": uv.get("tilesY", 1), "uvAnimType": uv.get("animationType", 0),
        "uvFps": uv.get("fps", 30.0), "uvFrameOverTime": _mm(uv.get("frameOverTime")),
        # 🔴 `timeMode` 与 `animationType` 是**两个**字段（Lifetime/Speed/FPS 与 WholeSheet/SingleRow），
        #    别把 `animationType` 塞进 `timeMode`（本批 14 颗 `timeMode` 全 0、`animationType` 有 0 有 1）。
        "uvTimeMode": uv.get("timeMode", 0), "uvRowIndex": uv.get("rowIndex", 0),
        "uvCycles": uv.get("cycles", 1.0),
        "noiseEnabled": bool(noi.get("enabled")),
        "noiseStrength": _mm(noi.get("strength")),
        "noiseStrengthY": _mm(noi.get("strengthY")),
        "noiseStrengthZ": _mm(noi.get("strengthZ")),
        "noiseSeparateAxes": bool(noi.get("separateAxes")),
        "noiseFrequency": noi.get("frequency", 0.5),
        "noiseDamping": bool(noi.get("damping")),
        "noiseOctaves": noi.get("octaves", 1),
        "noiseOctaveMultiplier": noi.get("octaveMultiplier", 0.5),
        "noiseOctaveScale": noi.get("octaveScale", 2.0),
        "noiseQuality": noi.get("quality", 1),
        "noiseScrollSpeed": _mm(noi.get("scrollSpeed")),
        "noiseRemapEnabled": bool(noi.get("remapEnabled")),
        "noiseRemap": _mm(noi.get("remap")),
        "noisePositionAmount": _mm(noi.get("positionAmount")),
        "noiseRotationAmount": _mm(noi.get("rotationAmount")),
        "noiseSizeAmount": _mm(noi.get("sizeAmount")),
        "trailEnabled": bool(trl.get("enabled")),
        "trailMode": trl.get("mode", 0), "trailRatio": trl.get("ratio", 1.0),
        "trailLifetime": _mm(trl.get("lifetime")),
        "trailMinVertexDistance": trl.get("minVertexDistance", 0.1),
        "trailTextureMode": trl.get("textureMode", 0),
        "trailTextureScaleX": (trl.get("textureScale") or {}).get("x", 1.0),
        "trailTextureScaleY": (trl.get("textureScale") or {}).get("y", 1.0),
        "trailRibbonCount": trl.get("ribbonCount", 1),
        "trailWorldSpace": bool(trl.get("worldSpace")),
        "trailDieWithParticles": bool(trl.get("dieWithParticles", True)),
        "trailSizeAffectsWidth": bool(trl.get("sizeAffectsWidth", True)),
        "trailInheritParticleColor": bool(trl.get("inheritParticleColor", True)),
        "renderMode": pr.get("m_RenderMode", 0),
        "sortingOrder": pr.get("m_SortingOrder", 0),
        "pivotX": (pr.get("m_Pivot") or {}).get("x", 0.0),
        "pivotY": (pr.get("m_Pivot") or {}).get("y", 0.0),
        "maxParticleSize": pr.get("m_MaxParticleSize", 0.5),
        "lengthScale": pr.get("m_LengthScale", 2.0),
        "material": _mat_name(mat_pid) if mat_pid else None,
    }


def _rot_z(q):
    if not q:
        return 0.0
    return math.degrees(2.0 * math.atan2(q.get("z", 0.0), q.get("w", 1.0)))


def collect():
    gos = _index_gos()
    nodes = []

    def add(go_pid, node_id, parent_id, depth):
        g = gos[go_pid]
        t = _tr_field(g["tr"])[1]
        kind = "R" if g["kind"] == "RectTransform" else "T"
        if kind == "R":
            pos = t.get("m_AnchoredPosition") or {"x": 0.0, "y": 0.0}
            x, y, z = pos.get("x", 0.0), pos.get("y", 0.0), 0.0
        else:
            pos = t.get("m_LocalPosition") or {"x": 0.0, "y": 0.0, "z": 0.0}
            x, y, z = pos.get("x", 0.0), pos.get("y", 0.0), pos.get("z", 0.0)
        scl = t.get("m_LocalScale") or {"x": 1.0, "y": 1.0, "z": 1.0}
        ps_pid = None
        for c in g["comps"]:
            if _exists("ParticleSystem", c):
                ps_pid = c
                break
        pr_pid = None
        for c in g["comps"]:
            if _exists("ParticleSystemRenderer", c):
                pr_pid = c
                break
        node = {
            "id": node_id, "name": g["name"], "parent": parent_id, "kind": kind,
            "x": x, "y": y, "z": z,
            "sx": scl.get("x", 1.0), "sy": scl.get("y", 1.0), "sz": scl.get("z", 1.0),
            "rotZ": _rot_z(t.get("m_LocalRotation")),
            "active": bool(t.get("m_GameObject")) and (g.get("name") is not None),
            "hasPs": ps_pid is not None,
            "ps": _ps_blob(ps_pid, pr_pid) if ps_pid is not None else None,
        }
        nodes.append(node)
        for ch in (t.get("m_Children") or []):
            ct = _tr_field(ch["m_PathID"])[1]
            cg = ct["m_GameObject"]["m_PathID"]
            add(cg, "%s/%s#%d" % (node_id, gos[cg]["name"], cg), node_id, depth + 1)

    for name in ROOTS:
        cands = [p for p, g in gos.items() if g["name"] == name]
        assert len(cands) == 1, (name, len(cands))
        add(cands[0], name, "", 0)
    return nodes


def main(argv):
    # 🔴 写盘类脚本的默认行为 = 只打印用法（CLAUDE.md §三「会写盘的脚本必须带显式动作参数」）
    if "-h" in argv or "--help" in argv or len(argv) == 0:
        sys.stdout.write(USAGE)
        return 0
    apply = "--apply" in argv
    if not apply and "--dry" not in argv:
        sys.stdout.write(USAGE)
        return 2
    nodes = collect()
    doc = {"nodes": nodes}
    data = json.dumps(doc, ensure_ascii=False, indent=1) + "\n"
    nps = sum(1 for n in nodes if n["hasPs"])
    if not apply:
        print("would write %s : %d nodes (%d with ParticleSystem)" % (OUT, len(nodes), nps))
        for n in nodes:
            print("  %-46s %s  ps=%s" % (n["id"], n["kind"], n["hasPs"]))
        return 0
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "wb").write(data.encode("utf-8"))
    print("wrote %s : %d nodes (%d with ParticleSystem)" % (OUT, len(nodes), nps))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
