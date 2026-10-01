# gen_shader_blend.py — 把**原版每个 shader 的 Pass0 混合状态**导成一张表（2026-10-01 造）
#
# 为什么要它：`ArenaOriginalMaterial.InferBlendFromShaderName` 现在**按 shader 名字猜**混合模式
# （名字里含 additive/premultiply/multiply/alpha blended/transparent 才算得出，其余一律**不透明**）。
# 实测踩到的：`Cruelty Particle Custom` 的源 shader 是 `Everguild/FX/Particle Shine Custom Vertex Streams`
#   · 那份材质 JSON 里写着 `_SrcBlend: 5 / _DstBlend: 10 / _Surface: 1`，**但那是 Standard 的残留死值** ——
#     该 shader 的属性表里根本没有这三个（Unity `Material.HasProperty` 实测 false，探针 `MatBlendProbe`）；
#   · 名字里也没有那四个关键词 ⇒ 我们把它导成了 **`_SrcBlend=1/_DstBlend=0/_ZWrite=1`（不透明）**。
# ⇒ 真正说了算的是**原版 shader 自己的 pass 状态**，而它就在序列化数据里（这个工具读出来）。
#
# 用法：`PYTHONIOENCODING=utf-8 python 工具/gen_shader_blend.py`
# 产物：`数据/游戏数据/original_shader_blend.tsv` —— `shader名 \t src \t dst \t zWrite \t 材质驱动? \t pass名`
#   · src/dst 用**数字**（One=1 · Zero=0 · SrcAlpha=5 · OneMinusSrcAlpha=10 …，与 Unity `BlendMode` 同值）
#   · `材质驱动?` = 1 表示这一 pass 的混合写的是 `[_SrcBlend]/[_DstBlend]`（**由材质决定**）
#     ⇒ 那种情况下不该用这张表去覆盖，得看材质自己写的值
import io, os, sys
import UnityPy

SHADER_BUNDLES = [
    r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\shaders_assets_all.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders_extra.bundle",
]
OUT = r"D:\4\Unity\数据\游戏数据\original_shader_blend.tsv"
OUT_CS = r"D:\4\Unity\MyGame\Assets\WarpforgeVFX\Runtime\OriginalShaderBlendTable.cs"

# Unity 的 BlendMode 枚举值（和 shader 里 Blend 那一行用的数字一致）
BLEND = {"Zero": 0, "One": 1, "DstColor": 2, "SrcColor": 3, "OneMinusDstColor": 4,
         "SrcAlpha": 5, "OneMinusSrcColor": 6, "DstAlpha": 7, "OneMinusDstAlpha": 8,
         "SrcAlphaSaturate": 9, "OneMinusSrcAlpha": 10}

# 有些 dump 出来是 GL.BlendMode / BlendMode 枚举，名字可能带前缀 ⇒ 用包含匹配兜底
def num_of(v):
    if v is None:
        return -1
    s = str(v)
    if s.isdigit():
        return int(s)
    for k, n in BLEND.items():
        if k.lower() in s.lower():
            return n
    return -1


def pass_blend(st):
    """返回 (src, dst, zwrite, materialDriven)

    ⚠️ 字段长这样（`UnityPy` 解出来的 `SerializedShaderFloatValue`）：
        `srcBlend = SerializedShaderFloatValue(name='_SrcBlend', val=0.0)`
    **`name` 才是判据**：`_SrcBlend` = 这一 pass 的混合**由材质属性驱动**（`Blend [_SrcBlend]…`）；
    `<noninit>` = **写死在 pass 里**（那 `val` 就是真值）。`zWrite` 同理。
    """
    md = False
    src = dst = -1
    try:
        b = st.rtBlend0
        sv = getattr(b, "srcBlend", None)
        dv = getattr(b, "destBlend", None)
        if sv is not None:
            src = int(round(float(sv.val)))
            if str(getattr(sv, "name", "")).startswith("_"):
                md = True
        if dv is not None:
            dst = int(round(float(dv.val)))
        if bool(getattr(st, "rtSeparateBlend", False)):
            md = True
    except Exception:
        pass
    zw = -1
    try:
        zwv = getattr(st, "zWrite", None)
        zw = int(round(float(zwv.val))) if zwv is not None else -1
    except Exception:
        pass
    return src, dst, zw, md


def main():
    rows = {}
    for path in SHADER_BUNDLES:
        if not os.path.exists(path):
            print(f"（跳过不存在的包：{path}）")
            continue
        env = UnityPy.load(path)
        n = 0
        for o in env.objects:
            if o.type.name != "Shader":
                continue
            try:
                sh = o.read()
                pf = sh.m_ParsedForm
            except Exception:
                continue
            if pf is None or not pf.m_Name:
                continue
            src = dst = zw = -1
            md = False
            pname = ""
            for s in pf.m_SubShaders:
                for p in s.m_Passes:
                    src, dst, zw, md = pass_blend(p.m_State)
                    # ⚠️ 本版本 `pass.m_Name`/`m_Tags` 是**空**的，真值在 `m_State` 里
                    pname = getattr(p.m_State, "m_Name", "") or p.m_Name or ""
                    break
                if pname:
                    break
            rows[pf.m_Name] = (src, dst, zw, 1 if md else 0, pname)
            n += 1
        print(f"{os.path.basename(path)}：{n} 个 shader")
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("shader\tsrcBlend\tdstBlend\tzWrite\tseparateBlend\tpassName\n")
        for k in sorted(rows):
            src, dst, zw, md, pn = rows[k]
            f.write(f"{k}\t{src}\t{dst}\t{zw}\t{md}\t{pn}\n")
    print(f"写出 {len(rows)} 条 → {OUT}")

    # ---- 再写一份 C# 表（导出器要用；**只收「写死在 pass 里」的那些**）----
    hard = {k: v for k, v in rows.items() if v[3] == 0 and v[0] >= 0 and v[1] >= 0}
    lines = []
    lines.append("// OriginalShaderBlendTable.cs — **由 `工具/gen_shader_blend.py` 生成，别手改**")
    lines.append("//")
    lines.append("// 它是什么：**原版每个 shader 的 Pass0 混合状态**（从 bundle 的序列化数据里读出来的真值）。")
    lines.append("// 为什么要它：`ArenaOriginalMaterial.InferBlendFromShaderName` 原来**按 shader 名字猜**混合模式，")
    lines.append("//   名字里没有 additive/premultiply/multiply/alpha blended/transparent 就一律当**不透明**。")
    lines.append("//   实测踩到的：`Cruelty Particle Custom` 的源 shader = `Everguild/FX/Particle Shine Custom Vertex Streams`")
    lines.append("//   （pass 里写死 `SrcAlpha→OneMinusSrcAlpha`，而名字里一个关键词都没有）⇒ 被导成不透明。")
    lines.append("//")
    lines.append("// ⚠️ **材质驱动的那些不收**（`Blend [_SrcBlend][_DstBlend]` 这种）—— 那种 pass 里读到的 `0/0` 是占位，")
    lines.append("//   真值在材质上（`Everguild/FX/Extra Color` 就是这一族）⇒ 交给调用方按材质值处理。")
    lines.append("namespace WarpforgeVFX")
    lines.append("{")
    lines.append("    public static class OriginalShaderBlendTable")
    lines.append("    {")
    lines.append("        // shader 名 → { srcBlend, dstBlend, zWrite }（Unity BlendMode 的数字值）")
    lines.append("        static readonly System.Collections.Generic.Dictionary<string, int[]> Map =")
    lines.append("            new System.Collections.Generic.Dictionary<string, int[]>")
    lines.append("        {")
    for k in sorted(hard):
        src, dst, zw, md, pn = hard[k]
        lines.append('            { "%s", new[] { %d, %d, %d } },' % (k.replace('"', '\\"'), src, dst, zw))
    lines.append("        };")
    lines.append("")
    lines.append("        /// <summary>查得到就返回 true —— 调用方**优先用它**，查不到再退回「按名字猜」。</summary>")
    lines.append("        public static bool TryGet(string shaderName, out int srcBlend, out int dstBlend, out float zwrite)")
    lines.append("        {")
    lines.append("            srcBlend = 0; dstBlend = 0; zwrite = 0f;")
    lines.append("            if (string.IsNullOrEmpty(shaderName)) return false;")
    lines.append("            int[] v;")
    lines.append("            if (!Map.TryGetValue(shaderName, out v) || v == null || v.Length < 3) return false;")
    lines.append("            srcBlend = v[0]; dstBlend = v[1]; zwrite = v[2];")
    lines.append("            return true;")
    lines.append("        }")
    lines.append("")
    lines.append("        /// <summary>表里有多少条（自检用）。</summary>")
    lines.append("        public static int Count { get { return Map.Count; } }")
    lines.append("    }")
    lines.append("}")
    with io.open(OUT_CS, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")
    print(f"写出 C# 表 {len(hard)} 条（材质驱动的 {len(rows) - len(hard)} 条已剔除）→ {OUT_CS}")

    for probe in ("Everguild/FX/Particle Shine Custom Vertex Streams", "Everguild/FX/Extra Color",
                  "Mobile/Particles/Additive", "Everguild/FX/Particle Distortion Affect Transparents"):
        if probe in rows:
            print(f"  {probe} → {rows[probe]}")


if __name__ == "__main__":
    main()
