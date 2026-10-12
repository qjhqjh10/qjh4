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
#
# 🆕 **2026-10-12（`A1589`→特效线 §十四 第 4 条「表驱动收口」）加 `cull` / `zTest` / `colMask` 三组列**：
#   · 起因：`WarpforgeShaderMap.InferCull` 是**按 shader 名字白名单**给的（`mobile/particles` / `legacy shaders/particles` …），
#     而判据本来就该是「**原版 pass 状态里那一格是不是烘死的**」—— 名字白名单是**推断**，表才是**实读**。
#   · ⚠️ **包清单原来是 3 个，查不到 `Mobile/Particles/*` 与 `Legacy Shaders/Particles/*`** ——
#     它们全在 **`Warpforge_unitybuiltinassets.bundle`** 里（原版把内置 shader 的编译产物打进了这个包）。
#     这和 `工具/disasm_dxbc.py` 文件头记的那条「第一次只读了一个包 ⇒ 误报『没有』」是**同一个形状的错**。
#   · 每一格都带一个 **`XxxBaked` 标志**：`name` 不以 `_` 开头（通常是 `<noninit>`）= **烘死在 pass 里** ⇒ 可以用它覆盖；
#     `name` 是 `_Cull` / `_ZTest` 这种 = **属性驱动** ⇒ ⛔ 一律不许覆盖（材质上那份才是真值）。
import io, os, sys
import UnityPy

SHADER_BUNDLES = [
    r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\shaders_assets_all.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders_extra.bundle",
    # 🆕 2026-10-12：内置管线的编译产物（`Mobile/Particles/*` · `Legacy Shaders/Particles/*` ·
    #   `Particles/Standard Unlit` · `UI/Additive` … 全在这一个包里）
    r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\Warpforge_unitybuiltinassets.bundle",
    # 🆕 2026-10-12：`Everguild/FX/*` 与 `Shader Graphs/Fx_*` 有一部分只在这两个包里
    #   （判据同 `工具/disasm_dxbc.py` 的 `BUNDLES` 注释：不补就永远得到「命中 0 个」的假结论）
    r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\battleprefabs_vfxandmisc_assets_all.bundle",
    r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\battlesharedresources_assets_all.bundle",
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


def _sv(field, default=-1):
    """读一个 `SerializedShaderFloatValue` → (值, 是不是烘死的)。

    **判据** = `name` 不以 `_` 开头（通常是 `<noninit>`）⇒ 这一格**烘死在 pass 里**、
    材质上同名属性是死值 ⇒ 可以用表里的值覆盖它。
    `name` 是 `_Cull` / `_ZTest` / `_ColMask` 这种 ⇒ **属性驱动** ⇒ ⛔ 不许覆盖。
    """
    if field is None:
        return default, False
    try:
        val = int(round(float(getattr(field, "val", default))))
    except Exception:
        val = default
    nm = str(getattr(field, "name", "") or "")
    baked = (val >= 0) and (not nm.startswith("_"))
    return val, baked


def pass_state(st):
    """🆕 2026-10-12：`cull` / `zTest` / `colMask` 三格 + 每格的「烘死?」标志。
    返回 (cull, cullBaked, ztest, ztestBaked, colmask, colmaskBaked)。
    ⚠️ 典型实读（`Warpforge_unitybuiltinassets.bundle`）：
        `Mobile/Particles/Additive` → cull=0(烘死) · ztest=4(烘死) · colMask=15(烘死)
        `Everguild/FX/Extra Color`  → cull 的 name 是 `_Cull` ⇒ **不烘死、不许覆盖**。
    """
    cull, cull_b = _sv(getattr(st, "culling", None))
    zt, zt_b = _sv(getattr(st, "zTest", None))
    cm, cm_b = _sv(getattr(getattr(st, "rtBlend0", None), "colMask", None))
    return cull, cull_b, zt, zt_b, cm, cm_b


def main():
    rows = {}
    nopass = []
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
            cull = cull_b = zt = zt_b = cm = cm_b = -1
            pname = ""
            # 🔴 **2026-10-12 修：原来一律取 `pass0` —— 那对有些 shader 是【空 pass】。**
            #   实测 `Particles/Standard Unlit` 的 `pass0` 是 `gpuProgramID = -1`（**没有程序**）、
            #   `tags = {}`，而真正的渲染 pass 是 `pass2`（`gpuProgramID = 234460`）。
            #   照 pass0 读 = **把一堆 0 当成真值**（`cull=0`/`zTest=0`/`blend 1/0`），
            #   而那种值会被下游当成「原版就是 Cull Off / ZTest Disabled」—— **静默错**。
            #   ⇒ 改成：**取第一个「有程序」的 pass**；若它带 `LIGHTMODE=SHADOWCASTER/DEPTHONLY` 这类
            #     非颜色 pass 的标记，就继续往后找第一个不带这种标记的（找不到再用它自己）。
            cands = []
            first = None
            for s in pf.m_SubShaders:
                for pi, p in enumerate(s.m_Passes):
                    gid = int(getattr(p.m_State, "gpuProgramID", -1) or -1)
                    tags = dict(getattr(getattr(p.m_State, "m_Tags", None), "tags", None) or [])
                    nm = getattr(p.m_State, "m_Name", "") or p.m_Name or ""
                    if gid < 0:
                        continue
                    cands.append((pi, p, nm, tags))
                    if first is None:
                        first = (pi, p, nm, tags)
            picked = None
            for pi, p, nm, tags in cands:
                lm = (tags.get("LIGHTMODE") or "").upper()
                if lm not in ("SHADOWCASTER", "DEPTHONLY", "MOTIONVECTORS"):
                    picked = (pi, p, nm, tags)
                    break
            if picked is None:
                picked = first
            if picked is not None:
                pi, p, pname, _tags = picked
                src, dst, zw, md = pass_blend(p.m_State)
                cull, cull_b, zt, zt_b, cm, cm_b = pass_state(p.m_State)
                pname = (pname + f" [pass{pi}]").strip()
            rows[pf.m_Name] = (src, dst, zw, 1 if md else 0, pname,
                               cull, cull_b, zt, zt_b, cm, cm_b)
            if picked is None:
                nopass.append(pf.m_Name)
            n += 1
        print(f"{os.path.basename(path)}：{n} 个 shader")
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("shader\tsrcBlend\tdstBlend\tzWrite\tseparateBlend\tpassName"
                "\tcull\tcullBaked\tzTest\tzTestBaked\tcolMask\tcolMaskBaked\n")
        for k in sorted(rows):
            r = rows[k]
            f.write(f"{k}\t{r[0]}\t{r[1]}\t{r[2]}\t{r[3]}\t{r[4]}"
                    f"\t{r[5]}\t{1 if r[6] else 0}\t{r[7]}\t{1 if r[8] else 0}"
                    f"\t{r[9]}\t{1 if r[10] else 0}\n")
    print(f"写出 {len(rows)} 条 → {OUT}")
    if nopass:
        print(f"⚠️ {len(nopass)} 个 shader 的**任何 pass 都没有程序**（`gpuProgramID < 0`）⇒ 状态列是占位：")
        print("   " + ", ".join(sorted(nopass)[:8]) + (" …" if len(nopass) > 8 else ""))

    # ---- 再写一份 C# 表（导出器要用；**只收「写死在 pass 里」的那些**）----
    hard = {k: v for k, v in rows.items() if v[3] == 0 and v[0] >= 0 and v[1] >= 0}
    # 🆕 2026-10-12：cull 单独一张表 —— **条件与 blend 那三条不一样**：
    #   `Everguild/FX/Extra Color` 的 blend 是材质驱动（被上面剔掉）、但 cull 也要按自己的标志判。
    culls = {k: v[5] for k, v in rows.items() if v[6] and v[5] >= 0}
    lines = []
    lines.append("// OriginalShaderBlendTable.cs — **由 `工具/gen_shader_blend.py` 生成，别手改**")
    lines.append("//")
    lines.append("// 它是什么：**原版每个 shader 的 Pass0 渲染状态**（从 bundle 的序列化数据里读出来的真值）。")
    lines.append("// 为什么要它：`ArenaOriginalMaterial.InferBlendFromShaderName` 原来**按 shader 名字猜**混合模式，")
    lines.append("//   名字里没有 additive/premultiply/multiply/alpha blended/transparent 就一律当**不透明**。")
    lines.append("//   实测踩到的：`Cruelty Particle Custom` 的源 shader = `Everguild/FX/Particle Shine Custom Vertex Streams`")
    lines.append("//   （pass 里写死 `SrcAlpha→OneMinusSrcAlpha`，而名字里一个关键词都没有）⇒ 被导成不透明。")
    lines.append("//")
    lines.append("// ⚠️ **材质驱动的那些不收**（`Blend [_SrcBlend][_DstBlend]` 这种）—— 那种 pass 里读到的 `0/0` 是占位，")
    lines.append("//   真值在材质上（`Everguild/FX/Extra Color` 就是这一族）⇒ 交给调用方按材质值处理。")
    lines.append("// ⚠️ **2026-10-01：本表那条「先查表再覆盖混合」的实验在【战场那条路】上被 sweep 判定为净变差并回退** ——")
    lines.append("//   事实经过见 `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs` 的 `InferBlendFromShaderName` 注释。")
    lines.append("//   表本身没错（是实读），错在「目标 shader 已经是兜底的那一个、混合照抄原件不等于画面对」。")
    lines.append("//   ⇒ **本表今天的消费者只有一个：`WarpforgeShaderMap.InferCull`**（cull 那一格，与混合无关）。")
    lines.append("namespace WarpforgeVFX")
    lines.append("{")
    lines.append("    public static class OriginalShaderBlendTable")
    lines.append("    {")
    lines.append("        // shader 名 → { srcBlend, dstBlend, zWrite }（Unity BlendMode 的数字值）")
    lines.append("        static readonly System.Collections.Generic.Dictionary<string, int[]> Map =")
    lines.append("            new System.Collections.Generic.Dictionary<string, int[]>")
    lines.append("        {")
    for k in sorted(hard):
        src, dst, zw = hard[k][0], hard[k][1], hard[k][2]
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
    lines.append("")
    lines.append("        /// <summary>🆕 2026-10-12：原版 shader 名 → **烘死在 pass 里的 `cull` 值**（`0=Off · 1=Front · 2=Back`）。")
    lines.append("        /// 🔴 **只收 `name` 不以 `_` 开头的那一格**（= 不是属性引用、值烘死）——")
    lines.append("        ///   属性驱动的那种（`Everguild/*` 的 `_Cull`）**故意不在表里** ⇒ `TryGetCull` 返回 false、")
    lines.append("        ///   `InferCull` 就该**不覆盖**。判据与实读表 → `WarpforgeShaderMap.InferCull` 的注释。</summary>")
    lines.append("        static readonly System.Collections.Generic.Dictionary<string, int> CullMap =")
    lines.append("            new System.Collections.Generic.Dictionary<string, int>")
    lines.append("        {")
    for k in sorted(culls):
        lines.append('            { "%s", %d },' % (k.replace('"', '\\"'), culls[k]))
    lines.append("        };")
    lines.append("")
    lines.append("        /// <summary>查得到 = 这个 shader 的 cull 是**烘死的**、可以覆盖；返回 false = 属性驱动，别动。</summary>")
    lines.append("        public static bool TryGetCull(string shaderName, out int cull)")
    lines.append("        {")
    lines.append("            cull = -1;")
    lines.append("            if (string.IsNullOrEmpty(shaderName)) return false;")
    lines.append("            return CullMap.TryGetValue(shaderName, out cull);")
    lines.append("        }")
    lines.append("")
    lines.append("        /// <summary>`CullMap` 里有多少条（自检用）。</summary>")
    lines.append("        public static int CullCount { get { return CullMap.Count; } }")
    lines.append("    }")
    lines.append("}")
    with io.open(OUT_CS, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")
    print(f"写出 C# 表 {len(hard)} 条（材质驱动的 {len(rows) - len(hard)} 条已剔除）"
          f" + cull 表 {len(culls)} 条 → {OUT_CS}")

    # 🔴 **自检（不许静默）**：`InferCull` 原来按名字白名单覆盖的这几条，**必须在 cull 表里** ——
    #   否则「表驱动替掉白名单」会让那三条效果（`Screen_Gold` / `StunEffect_proc` / `StunEffect`）
    #   的「少一半粒子」缺陷**悄悄回来**。缺了就**非 0 退出**。
    # ⚠️ **2026-10-12 现核订正**：这 4 个是**单 pass、状态烘死**的，必有名。
    #   `Particles/Standard Unlit` **不在必有名里** —— 它的 `pass0` 是 **`gpuProgramID = -1`（没有程序）**，
    #   真正的渲染 pass 是 `pass2`，而那一 pass 的 `cull` 是 **`_Cull`（属性驱动）** ⇒ **不该覆盖**。
    #   （原来那条名字规则会给它 0，2026-10-02 D 那次是照 `pass0` 读的 —— 那是**空 pass 的状态**。）
    must = ["Mobile/Particles/Additive", "Mobile/Particles/Alpha Blended",
            "Legacy Shaders/Particles/Additive", "Legacy Shaders/Particles/Alpha Blended"]
    warn_names = ["Particles/Standard Unlit"]      # 只在告警里出现，不参与退出码
    missing = [m for m in must if m not in culls]
    if missing:
        # 🔴 **失败就把已经写出去的两份删掉** —— 不许留下「半好的表」
        #   （否则下一次有人跑起来会拿到缺条目的 `CullMap`，而「少一条」是**静默**的）。
        for _f in (OUT, OUT_CS):
            try:
                if os.path.exists(_f): os.remove(_f)
            except Exception as _e:
                print(f"   （删 {_f} 失败：{_e}）")
        print(f"!! 自检失败：这些名字没有 cull 实读值 → {missing}")
        print("   ⇒ 多半是包清单里缺了它们所在的 bundle（内置管线那几个在 "
              "`Warpforge_unitybuiltinassets.bundle` 里）。已把写出的一半删掉。")
        sys.exit(1)
    # `InferCull` 的名字白名单**只覆盖这三条**，表里若出现别的烘死 cull 是**意外收获**（打印出来供人核）
    extra = sorted(k for k in culls if k not in must)
    print(f"✅ cull 自检通过（{len(must)}/{len(must)} 条命中）")
    print(f"   cull 表里另有 {len(extra)} 条（名字白名单原来覆盖不到的）：{extra[:12]}{' …' if len(extra) > 12 else ''}")


if __name__ == "__main__":
    main()
