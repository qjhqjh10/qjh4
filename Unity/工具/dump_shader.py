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

SHADERS_BUNDLES = [
    r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\shaders_assets_all.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders_extra.bundle",
]
VFX_BUNDLE = r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\battleprefabs_vfxandmisc_assets_all.bundle"

NEEDLE = sys.argv[1] if len(sys.argv) > 1 else "Particle Distortion"
MATNEEDLE = sys.argv[2] if len(sys.argv) > 2 else "Distort"

TYPE = {0: "Color", 1: "Vector", 2: "Float", 3: "Range", 4: "Texture", 5: "Int"}


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


def state_line(st):
    b = st.rtBlend0
    return (f"      rtBlend0={vars(b) if hasattr(b, '__dict__') else b} "
            f"separateBlend={st.rtSeparateBlend}\n"
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
    print(f"  属性（{len(_props)} 个）:")
    for l in prop_lines(pf.m_PropInfo):
        print(l)
    for si, s in enumerate(pf.m_SubShaders):
        print(f"  SubShader[{si}] lod={s.m_LOD} tags={tagmap(s.m_Tags)}")
        for p in s.m_Passes:
            print(f"    Pass {p.m_Name!r} type={p.m_Type} tags={tagmap(p.m_Tags)}")
            print(f"      platforms={p.m_Platforms} progMask={p.m_ProgramMask} "
                  f"nameIndices={list(p.m_NameIndices)}")
            print(state_line(p.m_State))


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
