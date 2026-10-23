# 读原版 shader 的完整 SerializedShader：属性表 / 渲染状态 / pass tags
# 以及**用它的材质**的属性数值（这才是灌进自建 shader 的东西）
#
# 为什么要这个：原版是 ShaderGraph shader，**HLSL 源码被剥了**（`Shader.m_Script` 是 null），
# 能拿到的是序列化出来的属性表 + pass 渲染状态。
# ⚠️ **但编译字节码没被剥** —— 它在 `Shader.compressedBlob` 里（LZ4 压缩的 DXBC，
#    资源名是明文）。那份走另一个工具：`工具/dump_shader_blob.py`。
#    （曾经错写成「源码和字节码都被剥掉了」，那是因为只看了 `m_SubProgramBlob`。）
# **动手改/写自建 shader 之前先跑它** —— 属性名、类型、默认值、混合/深度/剔除状态全在里面。
#
# 用法（必须 UTF-8，否则中文输出会乱码）：
#   PYTHONIOENCODING=utf-8 PYTHONUTF8=1 D:/2/Warpforge_tools/py312/python.exe \
#     d:/4/Unity/工具/dump_shader.py "<shader 名片段>" [材质名片段]
#   例：dump_shader.py "Particle Distortion" "Distort"
#       → 打印 shader 的 14 个属性 + 3 个 pass 的状态，以及 battleprefabs 包里名字含
#         "Distort" 的材质各自的实际数值
import sys, os, UnityPy

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/dump_shader.py [<参数1>] [<参数2>]\n  选项：无自定义选项（不带参数 = 默认模式）\n  位置参数：脚本内读 sys.argv[1..2]（缺省/必填以脚本自身判断为准）')
    _wf_help_sys.exit(0)

SHADERS_BUNDLES = [
    r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\shaders_assets_all.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders_extra.bundle",
]
VFX_BUNDLE = r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\battleprefabs_vfxandmisc_assets_all.bundle"

NEEDLE = sys.argv[1] if len(sys.argv) > 1 else "Particle Distortion"
MATNEEDLE = sys.argv[2] if len(sys.argv) > 2 else "Distort"

TYPE = {0: "Color", 1: "Vector", 2: "Float", 3: "Range", 4: "Texture", 5: "Int"}

# 🆕 2026-10-21（`T17`·本笔）：**混合状态要能看出「色与 alpha 分开寻址」**（`Blend A B, C D`）。
# 起因：`WFMatcap.shader` 文件头当年据本工具的输出判成「原版 `separateBlend=False`」——
#   **那个结论是错的**：原版 pass0 是单行四参数 `Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]`
#   （判据 = 反编译方法体，见该文件头 2026-10-21 那条）。错因有两条，都在本工具的输出里：
#   ① `st.rtSeparateBlend`（ShaderLab 的 `SeparateBlend` 开关）**全库都是 False**，它**不等于**
#      「没有分开的 alpha 混合」—— 真正的 alpha 因子记在 `rtBlend0.srcBlendAlpha/destBlendAlpha`；
#   ② 原来把 `rtBlend0` 整个 `vars()` 打出来，`srcBlend=…val=0.0` 在**属性驱动**时（name=`_SrcBlend`）
#      那个 `0.0` 是**占位不是真值**，肉眼很容易读成 `Blend Zero Zero`。
# ⇒ 现在补一行**解码后**的 `blend:` + 一行**还原出的 ShaderLab**；`separateAlpha(inEffect)` 是**推导值**：
#    alpha 两侧与色两侧的「驱动名不同」或「烘死值不同」就算分开寻址。
# 数值→名字取自 Unity 公开枚举（BlendMode / BlendOp），与 `_dump_shaders_batch.py` 同一张表。
BLEND = {0: "Zero", 1: "One", 2: "DstColor", 3: "SrcColor", 4: "OneMinusDstColor",
         5: "SrcAlpha", 6: "OneMinusSrcColor", 7: "DstAlpha", 8: "OneMinusDstAlpha",
         9: "SrcAlphaSaturate", 10: "OneMinusSrcAlpha"}
BLENDOP = {0: "Add", 1: "Sub", 2: "RevSub", 3: "Min", 4: "Max"}


def prop_lines(pi):
    # UnityPy 有时把 m_PropInfo 直接展成 props 列表，有时包一层 m_Props
    props = pi.m_Props if hasattr(pi, "m_Props") else pi
    out = []
    for p in props:
        dv = [getattr(p, f"m_DefValue_{i}_") for i in range(4)]
        line = f"    {p.m_Name:24s} {TYPE.get(p.m_Type, p.m_Type):8s} flags={p.m_Flags} def={dv}"
        dt = p.m_DefTexture
        if dt is not None:
            line += f"  defTex={getattr(dt,'m_TextureName',None)!r}/{getattr(dt,'m_DefaultName',None)!r}"
        if p.m_Description:
            line += f"  desc={p.m_Description!r}"
        out.append(line)
    return out


def tagmap(t):
    return dict(getattr(t, "tags", t))


def _blend_side(fv, pdef):
    """`SerializedShaderFloatValue` → (显示串, 驱动名或 None, 真值或 None)。

    - 驱动名 = 不以 `<noninit>` 开头（原版大量是 `_SrcBlend` 这种**材质属性驱动**）；
      此时 `val` 是**占位**，真值在 shader 属性表的默认值里，有就一并标出来。
    - `<noninit>` ⇒ 这一格**烘死在 pass 里**，`val` 就是真值。
    """
    nm = str(getattr(fv, "name", "") or "")
    raw = getattr(fv, "val", None)
    if nm and nm != "<noninit>":
        s = f"[{nm}"
        if nm in pdef:
            d = int(round(float(pdef[nm])))
            s += f"={d}:{BLEND.get(d, d)}"
        return s + "]", nm, None
    try:
        v = int(round(float(raw)))
    except Exception:
        return str(raw), None, None
    return f"{v}:{BLEND.get(v, v)}", None, v


def _op_side(fv):
    nm = str(getattr(fv, "name", "") or "")
    if nm and nm != "<noninit>":
        return f"[{nm}]"
    try:
        v = int(round(float(getattr(fv, "val", 0))))
    except Exception:
        return str(getattr(fv, "val", None))
    return BLENDOP.get(v, str(v))


def blend_line(st, pdef):
    """🆕 2026-10-21：解码后的混合状态 + `separateAlpha(inEffect)` 推导 + 还原的 ShaderLab。

    `pdef` = shader 属性表里 `Float/Range` 的默认值（`名 → 默认值`）；驱动名的真值靠它。
    """
    b = st.rtBlend0
    src, src_n, src_v = _blend_side(b.srcBlend, pdef)
    dst, dst_n, dst_v = _blend_side(b.destBlend, pdef)
    sa, sa_n, sa_v = _blend_side(b.srcBlendAlpha, pdef)
    da, da_n, da_v = _blend_side(b.destBlendAlpha, pdef)
    # 「分开寻址」的判据（三条任一）：序列化开关为真 · 两侧驱动名不同 · 两侧烘死值不同。
    # ⚠️ 只看 `rtSeparateBlend` 会误判 —— 该字段全库 False，而 Matcap 这类单行四参数写法
    #    （`Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]`）同样是分开寻址。
    sep = (bool(getattr(st, "rtSeparateBlend", False))
           or (src_n != sa_n) or (src_v != sa_v) or (dst_n != da_n) or (dst_v != da_v))
    s = (f"      blend: src={src} dst={dst} op={_op_side(b.blendOp)}"
         f" | srcA={sa} dstA={da} opA={_op_side(b.blendOpAlpha)}"
         f" | colMask={int(round(float(getattr(b.colMask, 'val', 0))))}"
         f"  separateBlend(序列化)={getattr(st, 'rtSeparateBlend', None)}"
         f"  separateAlpha(inEffect)={sep}")
    four = f"      BlendShaderLab: Blend {src} {dst}"
    if sep:
        four += f", {sa} {da}"
    return s + "\n" + four


def state_line(st, pdef=None):
    b = st.rtBlend0
    return (f"      rtBlend0={vars(b) if hasattr(b, '__dict__') else b} "
            f"separateBlend={st.rtSeparateBlend}\n"
            + (blend_line(st, pdef or {}) + "\n" if pdef is not None else "") +
            f"      zWrite={st.zWrite} zTest={st.zTest} zClip={st.zClip} cull={st.culling} "
            f"alphaToMask={st.alphaToMask} lighting={st.lighting} fogMode={st.fogMode}\n"
            f"      offset=({st.offsetFactor},{st.offsetUnits}) gpuProgramID={st.gpuProgramID}")


def dump_shader(sh):
    pf = sh.m_ParsedForm
    print("=" * 78)
    print(f"SHADER {pf.m_Name!r}")
    print(f"  关键字全集({len(pf.m_KeywordNames)}) = {list(pf.m_KeywordNames)}")
    print(f"  ShaderRequirements = {getattr(pf, 'm_ShaderRequirements', None)}")
    print(f"  CommonParameters = {getattr(pf, 'm_CommonParameters', None)}")
    _props = pf.m_PropInfo.m_Props if hasattr(pf.m_PropInfo, "m_Props") else pf.m_PropInfo
    # 🆕 属性驱动型混合状态（`Blend [_SrcBlend] …`）的真值 = shader 自己声明的默认值。
    pdef = {}
    for p in _props:
        if getattr(p, "m_Type", None) in (2, 3) and getattr(p, "m_Name", None):
            try:
                pdef[p.m_Name] = p.m_DefValue_0_
            except Exception:
                pass
    print(f"  属性（{len(_props)} 个）:")
    for l in prop_lines(pf.m_PropInfo):
        print(l)
    for si, s in enumerate(pf.m_SubShaders):
        print(f"  SubShader[{si}] lod={s.m_LOD} tags={tagmap(s.m_Tags)}")
        for p in s.m_Passes:
            print(f"    Pass {p.m_Name!r} type={p.m_Type} tags={tagmap(p.m_Tags)}")
            print(f"      platforms={p.m_Platforms} progMask={p.m_ProgramMask} "
                  f"nameIndices={list(p.m_NameIndices)}")
            print(state_line(p.m_State, pdef))


mat_total = 0
mat_hit = 0


def dump_mat(m, path):
    global mat_hit
    mat_hit += 1
    print(f"  --- 材质 {m.m_Name!r}  ({path})")
    print(f"      keywords = {list(m.m_ShaderKeywords)}")
    print(f"      customRenderQueue = {m.m_CustomRenderQueue}")
    sp = m.m_SavedProperties
    for name, val in sp.m_Floats:
        print(f"      float  {name:24s} = {val}")
    for name, val in sp.m_Colors:
        print(f"      color  {name:24s} = ({val.r:.4f},{val.g:.4f},{val.b:.4f},{val.a:.4f})")
    for name, val in sp.m_TexEnvs:
        t = val.m_Texture
        src = f"pathID={t.m_PathID}" if t else "None"
        print(f"      texenv {name:24s} = {src} scale=({val.m_Scale.x},{val.m_Scale.y}) "
              f"offset=({val.m_Offset.x},{val.m_Offset.y})")


shader_ids = set()
for path in SHADERS_BUNDLES:
    if not os.path.exists(path):
        continue
    print(f"\n### 打开 {os.path.basename(path)}")
    env = UnityPy.load(path)
    for o in env.objects:
        if o.type.name != "Shader":
            continue
        try:
            sh = o.read()
        except Exception:
            continue
        pf = sh.m_ParsedForm
        if pf is None or NEEDLE.lower() not in (pf.m_Name or "").lower():
            continue
        shader_ids.add(o.path_id)
        dump_shader(sh)

print(f"\n### 用它的材质（在 {os.path.basename(VFX_BUNDLE)} 里找 {MATNEEDLE!r}）")
env = UnityPy.load(VFX_BUNDLE)
for o in env.objects:
    if o.type.name != "Material":
        continue
    try:
        m = o.read()
    except Exception:
        continue
    mat_total += 1
    sh = m.m_Shader
    if sh is None or sh.m_PathID not in shader_ids:
        continue
    if MATNEEDLE.lower() not in (m.m_Name or "").lower():
        continue
    dump_mat(m, o.path_id)

print(f"\n（该包里材质共 {mat_total} 个，名字命中 {mat_hit} 个）")
