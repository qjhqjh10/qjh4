#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把原版的**太阳镜头耀斑**（URP `LensFlareComponentSRP` + `LensFlareDataSRP`）生成成 C# 表。

## 为什么有这个东西

原版 13 个战场里有 **6 个**挂着一个叫 `Sun flare` 的对象（URP 的 `LensFlareComponentSRP`），
**我们一个都没建** —— 这就是「高光全缺」里那半个太阳（原版 arena1 最亮处 = 日盘 **246**，
我们同位置只有 **204**，而且**根本没有那个带光芒的盘子**）。原版还有两个驱动脚本
`FlareScenarioToggler` / `CameraLensFlareIntenistyAnimation`（本轮**没接**，见交接文档）。

## 数据在哪（**全部原样照抄，没有一处是我们挑的**）

- 耀斑定义：`d:/2/解包整理/08_预制体特效/共享资源/MonoBehaviour/Sun_Flare_1_1192636038419116098.json`
  （`LensFlareDataSRP`，**7 个元素**；`elements` 里每个键名**就是 C# 的序列化字段名**）
- 贴图 6 张：`…/共享资源/Texture2D/{1,2,3,7,8,9}.png`
  —— **PathID → 文件名**这一步只能读原始包（解包目录按名字存），
  用的是 `工具/bundle_dump_objects.py battlesharedresources_assets_all --pat <PathID>`：
  `8327771005907127376→1 · 7107957222782238990→2 · -7674915500502597667→7 · 1871533782998597690→3 …`
  （见脚本里的 `TEX_MAP`）
- 每个战场那条组件的值：`d:/2/解包整理/07_场景/<场>/{GameObject/Sun flare.json, Transform, MonoBehaviour}`
- ⚠️ **坐标口径 = 世界坐标**（清单里的网格/粒子用的也是世界坐标；我们靠 `Arena3D` 整体 −100 对齐）
  ⇒ 这里是**沿父链累加**之后的 `pos`，不是 `m_LocalPosition`。

## 产物

1. `Assets/WarpforgeArena1/flares/textures/*.png`（6 张，从 d:/2 拷）
2. `Assets/WarpforgeArena1/flares/Sun Flare 1.asset` —— **由 C# 建**（`ArenaBuilder.BuildSunFlareAsset`），
   本脚本只生成它的数据源
3. `Assets/WarpforgeArena1/Editor/SunFlareData.gen.cs` —— **自动生成，不要手改**

用法：`PYTHONIOENCODING=utf-8 python 工具/gen_sunflare_cs.py`（幂等，可重跑）
"""
import io
import json
import os
import shutil

SCENE_BASE = r"D:/2/解包整理/07_场景"
FLARE_JSON = r"D:/2/解包整理/08_预制体特效/共享资源/MonoBehaviour/Sun_Flare_1_1192636038419116098.json"
TEX_SRC = r"D:/2/解包整理/08_预制体特效/共享资源/Texture2D"
ROOT = r"D:/4/Unity/MyGame/Assets/WarpforgeArena1"
OUT_TEX = os.path.join(ROOT, "flares/textures")
OUT_CS = os.path.join(ROOT, "Editor/SunFlareData.gen.cs")

ARENAS = ["battlearena1", "battlearena2", "battlearenaaeldari",
          "battlearenaastramilitarum", "battlearenaleviathan", "battlearenatauviorla"]

# PathID → 贴图文件名（用 `工具/bundle_dump_objects.py` 从 battlesharedresources_assets_all 读出来的）
TEX_MAP = {
    8327771005907127376: "1.png",
    7107957222782238990: "2.png",
    -7674915500502597667: "7.png",
    1871533782998590690: "3.png",
    -2916709754569864774: "9.png",
    1024102961458855566: "8.png",
}

F2 = lambda v: f"{float(v):.6f}f"


def vec2(d):
    return f"new Vector2({F2(d['x'])}, {F2(d['y'])})"


def col(d):
    return f"new Color({F2(d['r'])}, {F2(d['g'])}, {F2(d['b'])}, {F2(d['a'])})"


def curve(d):
    ks = ", ".join(f"new Keyframe({F2(k['time'])}, {F2(k['value'])}, {F2(k['inSlope'])}, {F2(k['outSlope'])})"
                   for k in d["m_Curve"])
    pre, post = int(d.get("m_PreInfinity", 2)), int(d.get("m_PostInfinity", 2))
    return (f"new AnimationCurve(new Keyframe[] {{ {ks} }}) "
            f"{{ preWrapMode = (WrapMode){pre}, postWrapMode = (WrapMode){post} }}")


def grad_parts(d):
    """返回 (colorKeys 表达式, alphaKeys 表达式, GradientMode 表达式) —— 三处共用一份。"""
    ck, ak = [], []
    for i in range(int(d.get("m_NumColorKeys", 0))):
        k = d[f"key{i}"]
        ck.append(f"new GradientColorKey(new Color({F2(k['r'])}, {F2(k['g'])}, {F2(k['b'])}, 1f), "
                  f"{float(d[f'ctime{i}']) / 65535.0:.6f}f)")
    for i in range(int(d.get("m_NumAlphaKeys", 0))):
        k = d[f"key{i}"]
        ak.append(f"new GradientAlphaKey({F2(k['a'])}, {float(d[f'atime{i}']) / 65535.0:.6f}f)")
    return (f"new GradientColorKey[] {{ {', '.join(ck)} }}",
            f"new GradientAlphaKey[] {{ {', '.join(ak)} }}",
            f"(GradientMode){int(d.get('m_Mode', 0))}")


def grad(d):
    cks, aks, mode = grad_parts(d)
    return f"new Gradient {{ colorKeys = {cks}, alphaKeys = {aks}, mode = {mode} }}"


def tex_gradient(d):
    cks, aks, mode = grad_parts(d["m_Gradient"])
    size = int(d.get("<textureSize>k__BackingField", 4))
    return (f"new TextureGradient({cks}, {aks}, {mode}, "
            f"(ColorSpace)({int(d.get('colorSpace', -1))}), {size})")


# 元素：dump 键 → (C# 成员名, 转换器)
ELEM = [
    ("visible", "visible", lambda v: "true" if v else "false"),
    ("position", "position", F2),
    ("positionOffset", "positionOffset", vec2),
    ("angularOffset", "angularOffset", F2),
    ("translationScale", "translationScale", vec2),
    ("ringThickness", "ringThickness", F2),
    ("hoopFactor", "hoopFactor", F2),
    ("noiseAmplitude", "noiseAmplitude", F2),
    ("noiseFrequency", "noiseFrequency", lambda v: str(int(v))),
    ("noiseSpeed", "noiseSpeed", F2),
    ("shapeCutOffSpeed", "shapeCutOffSpeed", F2),
    ("shapeCutOffRadius", "shapeCutOffRadius", F2),
    ("m_LocalIntensity", "localIntensity", F2),
    ("uniformScale", "uniformScale", F2),
    ("sizeXY", "sizeXY", vec2),
    ("allowMultipleElement", "allowMultipleElement", lambda v: "true" if v else "false"),
    ("m_Count", "count", lambda v: str(int(v))),
    ("preserveAspectRatio", "preserveAspectRatio", lambda v: "true" if v else "false"),
    ("rotation", "rotation", F2),
    ("tintColorType", "tintColorType", lambda v: f"(SRPLensFlareColorType){int(v)}"),
    ("tint", "tint", col),
    ("tintGradient", "tintGradient", tex_gradient),
    ("blendMode", "blendMode", lambda v: f"(SRPLensFlareBlendMode){int(v)}"),
    ("autoRotate", "autoRotate", lambda v: "true" if v else "false"),
    ("flareType", "flareType", lambda v: f"(SRPLensFlareType){int(v)}"),
    ("modulateByLightColor", "modulateByLightColor", lambda v: "true" if v else "false"),
    ("distribution", "distribution", lambda v: f"(SRPLensFlareDistribution){int(v)}"),
    ("lengthSpread", "lengthSpread", F2),
    ("positionCurve", "positionCurve", curve),
    ("scaleCurve", "scaleCurve", curve),
    ("seed", "seed", lambda v: str(int(v))),
    ("colorGradient", "colorGradient", grad),
    ("m_IntensityVariation", "intensityVariation", F2),
    ("positionVariation", "positionVariation", vec2),
    ("scaleVariation", "scaleVariation", F2),
    ("rotationVariation", "rotationVariation", F2),
    ("enableRadialDistortion", "enableRadialDistortion", lambda v: "true" if v else "false"),
    ("targetSizeDistortion", "targetSizeDistortion", vec2),
    ("distortionCurve", "distortionCurve", curve),
    ("distortionRelativeToCenter", "distortionRelativeToCenter", lambda v: "true" if v else "false"),
    ("m_FallOff", "fallOff", F2),
    ("m_EdgeOffset", "edgeOffset", F2),
    ("m_SideCount", "sideCount", lambda v: str(int(v))),
    ("m_SdfRoundness", "sdfRoundness", F2),
    ("inverseSDF", "inverseSDF", lambda v: "true" if v else "false"),
    ("uniformAngle", "uniformAngle", F2),
    ("uniformAngleCurve", "uniformAngleCurve", curve),
]

# 组件：dump 键 → (C# 字段名, C# 类型, 转换器)
COMP = [
    ("intensity", "intensity", "float", F2),
    ("maxAttenuationDistance", "maxAttenuationDistance", "float", F2),
    ("maxAttenuationScale", "maxAttenuationScale", "float", F2),
    ("distanceAttenuationCurve", "distanceAttenuationCurve", "AnimationCurve", curve),
    ("scaleByDistanceCurve", "scaleByDistanceCurve", "AnimationCurve", curve),
    ("attenuationByLightShape", "attenuationByLightShape", "bool", lambda v: "true" if v else "false"),
    ("radialScreenAttenuationCurve", "radialScreenAttenuationCurve", "AnimationCurve", curve),
    ("useOcclusion", "useOcclusion", "bool", lambda v: "true" if v else "false"),
    ("useBackgroundCloudOcclusion", "useBackgroundCloudOcclusion", "bool", lambda v: "true" if v else "false"),
    ("environmentOcclusion", "environmentOcclusion", "bool", lambda v: "true" if v else "false"),
    ("useWaterOcclusion", "useWaterOcclusion", "bool", lambda v: "true" if v else "false"),
    ("occlusionRadius", "occlusionRadius", "float", F2),
    ("sampleCount", "sampleCount", "uint", lambda v: f"{int(v)}u"),
    ("occlusionOffset", "occlusionOffset", "float", F2),
    ("scale", "scale", "float", F2),
    ("allowOffScreen", "allowOffScreen", "bool", lambda v: "true" if v else "false"),
    ("volumetricCloudOcclusion", "volumetricCloudOcclusion", "bool", lambda v: "true" if v else "false"),
]


def load_scene_transforms(arena):
    base = os.path.join(SCENE_BASE, arena)
    tf = {}
    d = os.path.join(base, "Transform")
    if not os.path.isdir(d):
        return None, None
    for f in os.listdir(d):
        if not f.startswith("Transform_") or not f.endswith(".json"):
            continue
        pid = int(f[10:-5])
        tf[pid] = json.load(io.open(os.path.join(d, f), encoding="utf-8"))
    return tf, base


def world_pos(tf, pid):
    x = y = z = 0.0
    while pid and pid in tf:
        p = tf[pid]["m_LocalPosition"]
        x += p["x"]; y += p["y"]; z += p["z"]
        pid = tf[pid].get("m_Father", {}).get("m_PathID")
    return x, y, z


def find_go(base, name):
    d = os.path.join(base, "GameObject")
    f = os.path.join(d, name + ".json")
    if os.path.exists(f):
        return json.load(io.open(f, encoding="utf-8"))
    return None


def main():
    # ---- 1) 贴图 ----
    os.makedirs(OUT_TEX, exist_ok=True)
    for fn in sorted(set(TEX_MAP.values())):
        src = os.path.join(TEX_SRC, fn)
        if not os.path.exists(src):
            raise SystemExit(f"缺贴图：{src}")
        dst = os.path.join(OUT_TEX, fn)
        if not os.path.exists(dst) or os.path.getsize(dst) != os.path.getsize(src):
            shutil.copyfile(src, dst)
            print(f"拷贴图 {fn}")

    # ---- 2) 耀斑定义 ----
    flare = json.load(io.open(FLARE_JSON, encoding="utf-8"))
    els = flare["elements"]
    print(f"耀斑元素 {len(els)} 个")

    # ---- 3) 六场各自的那条组件 ----
    specs = []
    for a in ARENAS:
        tf, base = load_scene_transforms(a)
        go = find_go(base, "Sun flare") if tf else None
        if go is None:
            print(f"  {a}: 没有 Sun flare，跳过")
            continue
        pids = [c["component"]["m_PathID"] for c in go["m_Component"]]
        tpid = pids[0]
        loc = tf[tpid]["m_LocalPosition"]
        wx, wy, wz = world_pos(tf, tpid)
        rot = tf[tpid]["m_LocalRotation"]
        comp = None
        for pid in pids[1:]:
            f = os.path.join(base, "MonoBehaviour", f"MonoBehaviour_{pid}.json")
            if os.path.exists(f):
                j = json.load(io.open(f, encoding="utf-8"))
                if "intensity" in j and "maxAttenuationDistance" in j:
                    comp = j
        if comp is None:
            print(f"  {a}: 找不到 LensFlareComponentSRP，跳过")
            continue
        specs.append((a, (wx, wy, wz), (loc["x"], loc["y"], loc["z"]), rot, comp))
        print(f"  {a}: 世界=({wx:.2f},{wy:.2f},{wz:.2f}) local=({loc['x']:.2f},{loc['y']:.2f},{loc['z']:.2f}) "
              f"intensity={comp['intensity']}")

    # ---- 4) 生成 C# ----
    L = []
    L.append("// ─────────────────────────────────────────────────────────────────────────────")
    L.append("// 自动生成 —— **不要手改**！生成器：`工具/gen_sunflare_cs.py`")
    L.append("//")
    L.append("// 原版 6 个战场挂着 `Sun flare`（URP `LensFlareComponentSRP` + `LensFlareDataSRP`），")
    L.append("// 我们一个都没建过 —— 这就是「高光全缺」里那半个太阳。数据全部照抄原版资源，")
    L.append("// 没有一处是我们挑的（出处见生成器头注释）。")
    L.append("// ─────────────────────────────────────────────────────────────────────────────")
    L.append("using System.Collections.Generic;")
    L.append("using UnityEngine;")
    L.append("using UnityEngine.Rendering;")
    L.append("using UnityEngine.Rendering.Universal;")
    L.append("")
    L.append("public static class SunFlareData")
    L.append("{")
    L.append('    public const string TexDir = "Assets/WarpforgeArena1/flares/textures";')
    L.append("")
    L.append("    /// <summary>原版 `Sun_Flare_1`（PathID 1192636038419116098），7 个元素。</summary>")
    L.append("    public static LensFlareDataElementSRP[] Elements() => new[]")
    L.append("    {")
    for i, e in enumerate(els):
        L.append(f"        // ---- 元素 {i}：位置 {e['position']:.3f} · 缩放 {e['uniformScale']:.2f} · "
                 f"强度 {e['m_LocalIntensity']:.2f} · 贴图 {TEX_MAP.get(e['lensFlareTexture']['m_PathID'], '?')} ----")
        L.append("        new LensFlareDataElementSRP")
        L.append("        {")
        for key, member, fn in ELEM:
            if key not in e:
                continue
            L.append(f"            {member} = {fn(e[key])},")
        tex = TEX_MAP.get(e["lensFlareTexture"]["m_PathID"])
        L.append(f'            lensFlareTexture = FlareTex("{tex}"),')
        L.append("        },")
    L.append("    };")
    L.append("")
    L.append("    /// <summary>每个战场**那一条组件**的原版值。`pos` 是**世界坐标**（沿父链累加过）。</summary>")
    L.append("    public struct Spec")
    L.append("    {")
    L.append("        public string arena;")
    L.append("        public Vector3 pos;")
    L.append("        public Quaternion rot;")
    for _k, member, ctype, _f in COMP:
        L.append(f"        public {ctype} {member};")
    L.append("    }")
    L.append("")
    L.append("    public static readonly Spec[] Specs =")
    L.append("    {")
    for a, w, loc, rot, c in specs:
        L.append(f'        new Spec {{ arena = "{a}", '
                 f"pos = new Vector3({F2(w[0])}, {F2(w[1])}, {F2(w[2])}), "
                 f"rot = new Quaternion({F2(rot['x'])}, {F2(rot['y'])}, {F2(rot['z'])}, {F2(rot['w'])}),")
        for key, member, ctype, fn in COMP:
            if key in c:
                L.append(f"            {member} = {fn(c[key])},")
        L.append("        },")
    L.append("    };")
    L.append("")
    L.append("    static Dictionary<string, Texture> _tex = new Dictionary<string, Texture>();")
    L.append("    static Texture FlareTex(string file)")
    L.append("    {")
    L.append("        if (_tex.TryGetValue(file, out var t)) return t;")
    L.append("#if UNITY_EDITOR")
    L.append("        t = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>(TexDir + \"/\" + file);")
    L.append("        if (t == null) Debug.LogWarning($\"[SunFlare] 找不到耀斑贴图 {file}\");")
    L.append("#endif")
    L.append("        _tex[file] = t;")
    L.append("        return t;")
    L.append("    }")
    L.append("}")
    os.makedirs(os.path.dirname(OUT_CS), exist_ok=True)
    io.open(OUT_CS, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    print(f"写 {OUT_CS}（{len(L)} 行）· 6 场里命中 {len(specs)} 场")


if __name__ == "__main__":
    main()
