# -*- coding: utf-8 -*-
"""gen_mainmenu_effects.py —— 把主菜单那 21 个「环境光效」的判据摊成一份数据文件。

为什么要有它（界面线 **R2** · 2026-10-27 B1UI）：
  账上 R2 写的是「主菜单 21 个环境光效 · 主菜单不再是静帧」。**先把判据查清再动手**（铁律 10 第 2 条：
  多层叠加的东西先做「层 × 场景 × 出现条件」表），而这张表**必须机器生成**（手抄必错，且下次复查还得重来）。
  本工具就是那张表的**正本**：逐节点给出
    · 它挂在谁的底下（父链，一路到 `Game UI`）· 沿途每一层的 `m_IsActive`
    · 每个组件的**类名**（经 `m_Script` → `bundle_Waprforge_monoscripts/MonoScript/m_ClassName`）
    · 矩形的原始字段（anchor/pos/sizeDelta/pivot/scale/rotation）
    · 粒子：**只有 enabled 的模块**（整块 JSON 原样带出）+ renderer 的模式/排序/pivot + 材质名/贴图 pid/shader 名
  ⇒ 谁要「照原版把这一层建出来」，直接读这份 JSON，⛔ 不用再翻一遍解包资源。

  🔴 **本次普查的结论（写进报告 §R2）**：21 个里 **19 个在 `Card Displayer Menu For Menu`
     （= 原版类 `CardDisplayWindow`）这一棵里，而它 `m_IsActive = false`** ⇒ 主菜单**静止时一个都没在跑**；
     另有 2 个（`Animated Loading Image` + `Cog`）**已经实现**（`Shell/LoadingMenu.cs`）。
     ⇒ 「主菜单是静帧」这个说法**不成立**，别照着它去给主菜单加一层粒子。

用法（**默认只打印用量，不写盘**）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_mainmenu_effects.py --apply
  -h / --help   只打印用法并退出（退出码 0）

产物：`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/mainmenu_effects.json`
  （放 `Resources/Art/` 是因为：① 本波白名单就是它 ② `Resources.Load<TextAsset>("Art/mainmenu_effects")` 能直接读）
"""
import io
import json
import os
import re
import sys

ASSETS = r"d:/2/新解包资源/assets_full"
BUNDLE = "bundle_scenes_scenes_mainmenuwarpforge"
MSDIR = os.path.join(ASSETS, "bundle_Waprforge_monoscripts", "MonoScript")
OUT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/mainmenu_effects.json"
# 第二份产物：**运行时 DTO**（扁平、`JsonUtility` 能吃；消费方 = `Shell/CardDisplayEffects.cs`）。
# ⚠️ 两份必须由**同一次运行**产出 —— 判据只有一份，别手工改任何一份。
OUT_RT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json"

# 账上 R2 点名的 21 个（**名字逐字照 `GameObject/*.json`**；`Swirl_297` 这类是导出器给同名 GO 加的后缀）
TARGETS = [
    "Swirl", "Swirl_297", "Swirl_304", "Swirl_314",
    "Glow Explosion Up", "Glow Explosion R", "Glow Explosion Down",
    "Side Border Left", "Side Border Right",
    "Ray Right", "Sparks R", "Sparks RL",
    "EffectElement", "EffectElement (1)", "EffectElement (2)", "EffectElement (3)", "EffectElement (4)",
    "Cog", "Craft", "Level Up Effect Down", "Animated Loading Image",
]

PS_MODULES = ["InitialModule", "ShapeModule", "EmissionModule", "SizeModule", "RotationModule", "ColorModule",
              "UVModule", "VelocityModule", "InheritVelocityModule", "LifetimeByEmitterSpeedModule",
              "ForceModule", "ExternalForcesModule", "ClampVelocityModule", "NoiseModule",
              "SizeBySpeedModule", "RotationBySpeedModule", "ColorBySpeedModule", "CollisionModule",
              "TriggerModule", "SubModule", "LightsModule", "TrailModule"]

USAGE = """用法（默认不写盘）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \\
      d:/4/Unity/工具/gen_mainmenu_effects.py --apply
  -h / --help   只打印本用法并退出（退出码 0）

做的事：把主菜单 21 个「环境光效」的判据（父链 / active / 组件类 / 矩形 / 粒子模块 / 材质）摊成
        `Resources/Art/mainmenu_effects.json`。只读 `d:/2/新解包资源/`，只写上面那一个文件。
"""


def build_index(b):
    """组件 pid -> (类型目录, 文件路径)；GO pid -> (名字, active)。类名走 MonoScript。"""
    comp = {}
    for t in sorted(os.listdir(b)):
        d = os.path.join(b, t)
        if not os.path.isdir(d) or t == "GameObject":
            continue
        for fn in os.listdir(d):
            if not fn.endswith(".json"):
                continue
            m = re.search(r"_(-?\d+)\.json$", fn)
            if m:
                comp[int(m.group(1))] = (t, os.path.join(d, fn))
    go = {}
    comp2go = {}
    for fn in os.listdir(os.path.join(b, "GameObject")):
        if not fn.endswith(".json"):
            continue
        p = os.path.join(b, "GameObject", fn)
        d = json.load(io.open(p, encoding="utf-8"))
        m = re.search(r"_(-?\d+)\.json$", fn)
        if m:
            go[int(m.group(1))] = (d.get("m_Name"), d.get("m_IsActive"), fn)
        for c in d.get("m_Component", []):
            comp2go[c["component"]["m_PathID"]] = (d.get("m_Name"), d.get("m_IsActive"), fn[:-5])
    return comp, go, comp2go


def class_of(script_pid):
    p = os.path.join(MSDIR, "MonoScript_%d.json" % script_pid) if script_pid else None
    if not p or not os.path.isfile(p):
        return "?"
    d = json.load(io.open(p, encoding="utf-8"))
    ns = d.get("m_Namespace") or ""
    return (ns + "." if ns else "") + (d.get("m_ClassName") or "?")


def build_all_index():
    """全库 pid -> 路径。⚠️ 只在**本包找不到**时才用（4600+ 目录，只做一次）。"""
    out = {}
    for t in ("Material", "Texture2D", "Shader"):
        for root in os.listdir(ASSETS):
            d = os.path.join(ASSETS, root, t)
            if not os.path.isdir(d):
                continue
            for fn in os.listdir(d):
                if not fn.endswith(".json"):
                    continue
                m = re.search(r"_(-?\d+)\.json$", fn)
                if m:
                    out.setdefault(int(m.group(1)), (t, os.path.join(d, fn)))
    return out


ALL = {}


def load_json(p):
    return json.load(io.open(p, encoding="utf-8"))


def _mm(cur):
    """`minMaxState` 曲线 -> 扁平 `{mode,a,b,curve[]}`（0 Constant / 1 Curve / 2 TwoCurves / 3 TwoConstants）。"""
    if not cur:
        return None
    pts = [{"t": p.get("time", 0.0), "v": p.get("value", 0.0)}
           for p in ((cur.get("maxCurve") or {}).get("m_Curve") or [])]
    return {"mode": cur.get("minMaxState", 0), "a": cur.get("scalar", 0.0),
            "b": cur.get("minScalar", 0.0), "curve": pts}


def _col(c):
    return {"r": c["r"], "g": c["g"], "b": c["b"], "a": c["a"]} if c else {"r": 1, "g": 1, "b": 1, "a": 1}


def _grad(g):
    """`ParticleSystemGradientMode`：0 Color / 1 Gradient / 2 TwoColors / 3 TwoGradients / 4 RandomColor。"""
    if not g:
        return None
    mg = g.get("maxGradient") or {}
    ck = [{"t": mg.get("ctime%d" % i, 0.0),
           "r": (mg.get("key%d" % i) or {}).get("r", 0.0),
           "g": (mg.get("key%d" % i) or {}).get("g", 0.0),
           "b": (mg.get("key%d" % i) or {}).get("b", 0.0)}
          for i in range(min(int(mg.get("m_NumColorKeys", 0) or 0), 8))]
    ak = [{"t": mg.get("atime%d" % i, 0.0), "a": (mg.get("key%d" % i) or {}).get("a", 1.0)}
          for i in range(min(int(mg.get("m_NumAlphaKeys", 0) or 0), 8))]
    return {"mode": g.get("minMaxState", 0), "ca": _col(g.get("minColor")), "cb": _col(g.get("maxColor")),
            "colorKeys": ck, "alphaKeys": ak}


def emit_runtime(doc):
    """把判据正本折成运行时 DTO（扁平；字段名与 `Shell/CardDisplayEffects.cs` 的 DTO 逐字对应）。"""
    import math
    parts = []
    for it in doc["items"]:
        if not it.get("particle"):
            continue
        ps = it["particle"]
        mods = ps["modules"]
        rnd = it.get("renderer") or {}
        ini = mods.get("InitialModule") or {}
        shp = mods.get("ShapeModule") or {}
        emi = mods.get("EmissionModule") or {}
        siz = mods.get("SizeModule") or {}
        rot = mods.get("RotationModule") or {}
        cm = mods.get("ColorModule") or {}
        vel = mods.get("VelocityModule") or {}
        uv = mods.get("UVModule") or {}
        rt = None
        for c in it["components"]:
            if c["type"] in ("RectTransform", "Transform"):
                rt = c.get("rect") or {}
        q = rt.get("m_LocalRotation") or {"z": 0.0, "w": 1.0}
        pos = rt.get("m_AnchoredPosition") or rt.get("m_LocalPosition") or {"x": 0.0, "y": 0.0}
        scl = rt.get("m_LocalScale") or {"x": 1.0, "y": 1.0, "z": 1.0}
        bursts = [{"time": b.get("time", 0.0),
                   "count": (b.get("countCurve") or {}).get("scalar", 0.0),
                   "cycles": b.get("cycleCount", 1),
                   "interval": b.get("repeatInterval", 0.01)}
                  for b in (emi.get("m_Bursts") or [])]
        mats = rnd.get("materials") or []
        parts.append({
            "name": it["sourceFile"],
            "parent": (it["chain"][1]["node"] if len(it["chain"]) > 1 else ""),
            "posX": pos["x"], "posY": pos["y"],
            "rotZ": math.degrees(2 * math.atan2(q["z"], q["w"])),
            "scaleX": scl["x"], "scaleY": scl["y"], "scaleZ": scl["z"],
            "length": ps["lengthInSec"], "looping": bool(ps["looping"]),
            "playOnAwake": bool(ps["playOnAwake"]),
            "startDelay": (ps.get("startDelay") or {}).get("scalar", 0.0),
            "simulationSpeed": ps.get("simulationSpeed", 1.0),
            "scalingMode": ps.get("scalingMode", 0),
            "maxParticles": ini.get("maxNumParticles", 1000),
            "life": _mm(ini.get("startLifetime")), "speed": _mm(ini.get("startSpeed")),
            "size": _mm(ini.get("startSize")), "size3D": bool(ini.get("size3D")),
            "color": _grad(ini.get("startColor")), "rot": _mm(ini.get("startRotation")),
            "gravity": _mm(ini.get("gravityModifier")),
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
            "rate": _mm(emi.get("rateOverTime")), "bursts": bursts,
            "sizeOverLife": _mm(siz.get("curve")), "rotOverLife": _mm(rot.get("x")),
            "colorOverLife": _grad(cm.get("gradient")),
            "velX": _mm(vel.get("x")), "velY": _mm(vel.get("y")),
            "uvEnabled": bool(uv.get("enabled")), "uvTilesX": uv.get("tilesX", 1),
            "uvTilesY": uv.get("tilesY", 1), "uvAnimType": uv.get("animationType", 0),
            "uvFps": uv.get("fps", 30.0),
            # 🔴 2026-10-27（B1UI）：原来漏了这条 ⇒ `Ray Right` 的 UV 播放走 Unity 默认。判据 = 原版 `UVModule.frameOverTime`
            "uvFrameOverTime": _mm(uv.get("frameOverTime")),
            "renderMode": rnd.get("m_RenderMode", 0), "sortingOrder": rnd.get("m_SortingOrder", 0),
            "pivotX": (rnd.get("m_Pivot") or {}).get("x", 0.0),
            "pivotY": (rnd.get("m_Pivot") or {}).get("y", 0.0),
            "maxParticleSize": rnd.get("m_MaxParticleSize", 0.5),
            "lengthScale": rnd.get("m_LengthScale", 2.0),
            "material": (mats[0] or {}).get("name") if mats else None,
        })

    images = []
    for it in doc["items"]:
        if it.get("particle") or it["sourceFile"] not in ("Level Up Effect Down", "Side Border Left",
                                                          "Side Border Right"):
            continue
        rt, img, glow = {}, None, None
        for c in it["components"]:
            if c["type"] in ("RectTransform", "Transform"):
                rt = c.get("rect") or {}
            if c.get("class") == "UnityEngine.UI.Image":
                img = c.get("fields") or {}
            if c.get("class") == "UIBorderGlow":
                glow = c.get("fields") or {}
        pos = rt.get("m_AnchoredPosition") or {"x": 0.0, "y": 0.0}
        size = rt.get("m_SizeDelta") or {"x": 0.0, "y": 0.0}
        col = (img or {}).get("m_Color") or {}
        images.append({
            "name": it["sourceFile"], "posX": pos["x"], "posY": pos["y"],
            "sizeX": size["x"], "sizeY": size["y"],
            "colorR": col.get("r", 1.0), "colorG": col.get("g", 1.0),
            "colorB": col.get("b", 1.0), "colorA": col.get("a", 1.0),
            "glowTime": (glow or {}).get("animTime", 1.0),
            "glowDelay": (glow or {}).get("animDelay", 0.0),
            "glowPlayOnEnable": (glow or {}).get("playOnEnable", 0),
            # 这一件**有没有** `UIBorderGlow`（★ 不能靠 `glowTime` 猜 —— 缺组件时那个默认值也是 1.0）
            "hasGlow": glow is not None})

    txt = json.dumps({"note": "runtime DTO (gen = 工具/gen_mainmenu_effects.py --apply; judge = mainmenu_effects.json)",
                      "particles": parts, "images": images}, ensure_ascii=False, indent=1)
    io.open(OUT_RT, "wb").write(txt.encode("utf-8"))
    print("写入", OUT_RT, "（%d 字节；%d 颗粒子 / %d 条图件）" % (len(txt.encode("utf-8")), len(parts), len(images)))


def main(apply):
    b = os.path.join(ASSETS, BUNDLE)
    comp, go, comp2go = build_index(b)

    def chain(pid):
        """组件 pid 往上爬：返回 [(层名, active, 组件类型)]，最外层在最后。"""
        out = []
        seen = set()
        cur = pid
        while cur and cur not in seen:
            seen.add(cur)
            if cur not in comp:
                break
            t, p = comp[cur]
            d = load_json(p)
            gname = comp2go.get(cur, (None, None, None))
            out.append({"type": t, "node": gname[0], "active": gname[1],
                        "path": os.path.relpath(p, b).replace("\\", "/")})
            f = (d.get("m_Father") or {}).get("m_PathID")
            if not f:
                # Transform 用 m_Father；RectTransform 同名字段
                break
            cur = f
        return out

    def comps_of(go_name):
        p = os.path.join(b, "GameObject", go_name + ".json")
        if not os.path.isfile(p):
            return None
        return load_json(p)

    items = []
    for name in TARGETS:
        g = comps_of(name)
        if g is None:
            items.append({"name": name, "error": "GameObject json 不存在"})
            continue
        rec = {"name": g.get("m_Name"), "sourceFile": name, "active": g.get("m_IsActive"), "layer": g.get("m_Layer"),
               "components": [], "chain": [], "particle": None, "renderer": None}
        for c in g.get("m_Component", []):
            pid = c["component"]["m_PathID"]
            t, p = comp.get(pid, (None, None))
            if not p:
                rec["components"].append({"pid": pid, "type": "?"})
                continue
            d = load_json(p)
            entry = {"pid": pid, "type": t}
            if t == "MonoBehaviour":
                entry["class"] = class_of((d.get("m_Script") or {}).get("m_PathID"))
                entry["fields"] = {k: v for k, v in d.items()
                                   if k not in ("m_GameObject", "m_Enabled", "m_Script", "m_Name")}
            elif t in ("RectTransform", "Transform"):
                entry["rect"] = {k: d.get(k) for k in
                                 ("m_AnchorMin", "m_AnchorMax", "m_AnchoredPosition", "m_SizeDelta",
                                  "m_Pivot", "m_LocalScale", "m_LocalRotation", "m_LocalPosition")}
                if not rec["chain"]:
                    rec["chain"] = chain(pid)
            elif t == "ParticleSystem":
                on = [m for m in PS_MODULES if (d.get(m) or {}).get("enabled")]
                rec["particle"] = {
                    "lengthInSec": d.get("lengthInSec"), "looping": d.get("looping"),
                    "prewarm": d.get("prewarm"), "playOnAwake": d.get("playOnAwake"),
                    "startDelay": d.get("startDelay"), "simulationSpeed": d.get("simulationSpeed"),
                    "scalingMode": d.get("scalingMode"), "enabledModules": on,
                    "modules": {m: d.get(m) for m in on},
                }
            elif t == "ParticleSystemRenderer":
                mats = []
                for mt in (d.get("m_Materials") or []):
                    pid2 = (mt or {}).get("m_PathID")
                    if not pid2:
                        mats.append(None)
                        continue
                    hit = ALL.get(pid2)
                    if not hit:
                        mats.append({"pid": pid2, "name": "?"})
                        continue
                    md = load_json(hit[1])
                    tex = None
                    for e in (md.get("m_SavedProperties", {}) or {}).get("m_TexEnvs", []) or []:
                        if isinstance(e, list) and e and isinstance(e[0], dict) and e[0].get("name") == "_MainTex":
                            tex = ((e[1] or {}).get("m_Texture") or {}).get("m_PathID")
                    mats.append({"pid": pid2, "name": md.get("m_Name"),
                                 "shaderPid": (md.get("m_Shader") or {}).get("m_PathID"),
                                 "mainTexPid": tex,
                                 "colors": md.get("m_SavedProperties", {}).get("m_Colors")})
                rec["renderer"] = {k: d.get(k) for k in
                                   ("m_RenderMode", "m_Alignment", "m_SortingOrder", "m_SortingFudge",
                                    "m_Pivot", "m_MinParticleSize", "m_MaxParticleSize",
                                    "m_NormalDirection", "m_LengthScale", "m_CameraVelocityScale",
                                    "m_VelocityScale", "m_UseCustomVertexStreams")}
                rec["renderer"]["materials"] = mats
            rec["components"].append(entry)
        items.append(rec)

    doc = {
        "note": "主菜单 21 个「环境光效」判据（生成器 = 工具/gen_mainmenu_effects.py；源 = %s）" % BUNDLE,
        "source": b,
        "targets": TARGETS,
        "items": items,
    }
    text = json.dumps(doc, ensure_ascii=False, indent=1)

    if not apply:
        print(USAGE)
        print("[dry] 21 条已解析，未写盘。目标 =", OUT, "（%d 字节）" % len(text.encode("utf-8")))
        for it in items:
            if it.get("error"):
                print("  ! %s : %s" % (it["name"], it["error"]))
                continue
            inn = [c for c in it["chain"] if c.get("active") is False]
            print("  %-24s active=%-5s 父链%s 粒子=%s" %
                  (it["name"], it["active"],
                   ("（其中 inactive：%s）" % ", ".join(str(c["node"]) for c in inn)) if inn else "",
                   "有" if it.get("particle") else "—"))
        print("\n要真写盘请加 --apply")
        return 0

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, "wb").write(text.encode("utf-8"))
    print("写入", OUT, "（%d 字节）" % len(text.encode("utf-8")))
    emit_runtime(doc)
    return 0


if __name__ == "__main__":
    argv = sys.argv[1:]
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    if any(a in ("-h", "--help") for a in argv):
        print(USAGE)
        sys.exit(0)
    if "--apply" not in argv:
        # 真扫一遍再打印预览（这样 --help 之外的默认行为也给出真实信息）
        ALL.update(build_all_index())
        main(False)
        print("（本次未带 --apply ⇒ 什么都没做）")
        sys.exit(0)
    ALL.update(build_all_index())
    sys.exit(main(True))
