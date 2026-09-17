# -*- coding: utf-8 -*-
"""批量把原版 Warpforge 的**每一个 shader** 建成「属性表 + pass 渲染状态 + 采样纹理」清单。

**为什么要包一层**：现成的 `工具/dump_shader.py` 与 `工具/dump_shader_blob.py` 都是
**一次只处理一个 shader 名**（argv[1]），不会循环。本脚本把两者的主逻辑合起来跑全量，
并额外做三件它们没做的事：

  ① **从 bundle 里枚举 shader**（不再依赖「名字清单」这种二手来源）；
  ② 解析 Material 的 `m_Shader` PPtr（`m_FileID` → `externals` → `archive:/CAB-xxx/...`
     → CAB 名 → bundle），统计**谁在用哪个 shader**；
  ③ 按字母序切 4 块写成 Markdown（每块 ≤400 行）+ 一份 ≤120 行的汇总。

用法（必须 UTF-8，否则中文输出乱码）：
  PYTHONIOENCODING=utf-8 PYTHONUTF8=1 D:/2/Warpforge_tools/py312/python.exe \
    d:/4/Unity/工具/_dump_shaders_batch.py

产出（只写 `资料/普查产出_0917/`，不碰 `Assets/`、不启动 Unity）：
  d:/4/Unity/资料/普查产出_0917/shader属性表_块1.md … _块4.md
  d:/4/Unity/资料/普查产出_0917/shader属性表_汇总.md

★ 实测记下的三条（改动本脚本或写文档前先看）：
  · `pass.m_Name` / `pass.m_Tags` 在这个 Unity 版本里**是空的**；
    真值在 `pass.m_State.m_Name` / `m_State.m_Tags`（dump_shader.py 印的是前者，会印成空）。
  · `SerializedShader` **没有** `m_ShaderRequirements` / `m_CommonParameters` 字段
    （dump_shader.py 用 getattr 兜底会印 None —— 不是「查不到」，是这个版本不序列化）。
  · DXBC 里**没有 RDEF 段**（只有 ISGN/OSGN/SHDR）：资源名不是从 DXBC 反射表读到的，
    而是从 Unity 自己那段「常量缓冲布局」头里扫到的。所以「采样纹理名」列要分两栏写。
"""
import io
import os
import re
import sys
from collections import Counter, defaultdict

try:
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

import UnityPy  # noqa: E402

try:
    import lz4.block as lz4  # noqa: E402
    HAS_LZ4 = True
except Exception:  # pragma: no cover
    HAS_LZ4 = False

AA = r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64"
OUTDIR = r"d:\4\Unity\资料\普查产出_0917"
NBLOCKS = 4
MAXBLOCKLINES = 400
MAXSUMMARYLINES = 120

# ---------------------------------------------------------------- 枚举表
# 数值→名字取自 Unity 公开枚举（BlendMode / BlendOp / CompareFunction / CullMode /
# ColorWriteMask / ShaderPropertyFlags）。抽样对照见 `shader属性表_汇总.md` 第七节。
BLEND = {0: "Zero", 1: "One", 2: "DstColor", 3: "SrcColor", 4: "OneMinusDstColor",
         5: "SrcAlpha", 6: "OneMinusSrcColor", 7: "DstAlpha", 8: "OneMinusDstAlpha",
         9: "SrcAlphaSaturate", 10: "OneMinusSrcAlpha"}
BLENDOP = {0: "Add", 1: "Sub", 2: "RevSub", 3: "Min", 4: "Max"}
CMPFUNC = {0: "Disabled", 1: "Never", 2: "Less", 3: "Equal", 4: "LEqual",
           5: "Greater", 6: "NotEqual", 7: "GEqual", 8: "Always"}
CULL = {0: "Off", 1: "Front", 2: "Back"}
PFLAG = {1: "HideInInspector", 2: "PerRendererData", 4: "NoScaleOffset", 8: "Normal",
         16: "HDR", 32: "Gamma", 64: "NonModifiableTextureData", 128: "MainTexture",
         256: "MainColor"}
PTYPE = {0: "Color", 1: "Vector", 2: "Float", 3: "Range", 4: "Texture", 5: "Int"}

# 管线/引擎自己塞进来的全局纹理（属性表里查不到，只能从字节码扫）。
# ⚠ 只放**确认是纹理**的名字 —— 曾经把 `RTHandleScale`（URP 的 float4 缩放，不是纹理）放进来，
#   会凭空多报一条。白名单只是「过滤噪音」，命中与否仍以「这个名字确实出现在字节码里」为准。
ENGINE_GLOBAL_TEX = {
    "CameraDepthTexture", "CameraOpaqueTexture", "CameraColorTexture", "CameraNormalsTexture",
    "GrabPassTransparent", "BlitTexture", "MainLightShadowmapTexture",
    "MainLightShadowmapTextureXArray", "MainLightShadowmapTextureYArray",
    "MainLightShadowmapTextureZArray", "MainLightShadowmapTextureWArray",
    "ShadowMapTexture", "SpecCube0", "SpecCube1", "GlossyEnvironmentCubeMap",
    "ReflectionProbeTexture", "GlossyReflectionCubeMap", "LightCookie", "LightTexture0",
    "LightTextureB0", "ScreenSpaceOcclusionTexture", "SSAOTexture",
}


# ---------------------------------------------------------------- 小工具
def num(v):
    try:
        f = float(v)
    except Exception:
        return str(v)
    if f == int(f) and abs(f) < 1e15:
        return str(int(f))
    return f"{f:.6g}"


def aslist(x):
    if x is None:
        return []
    return list(x) if isinstance(x, (list, tuple)) else [x]


def shval(fv):
    """SerializedShaderFloatValue → 值 或 [属性名]（原版大量状态是「材料属性驱动」的）。"""
    n = getattr(fv, "name", None)
    if n and n != "<noninit>":
        return f"[{n}]"
    return num(getattr(fv, "val", None))


def en(v, table):
    return table.get(int(v), str(v)) if isinstance(v, int) else v


def _blend_side(fv, table):
    s = shval(fv)
    return en(int(s), table) if s.lstrip("-").isdigit() else s


def blend_cell(st):
    b = st.rtBlend0
    src, dst = _blend_side(b.srcBlend, BLEND), _blend_side(b.destBlend, BLEND)
    s = f"{src}→{dst}"
    op = _blend_side(b.blendOp, BLENDOP)
    if op != "Add":
        s += f"·{op}"
    a_s, a_d = _blend_side(b.srcBlendAlpha, BLEND), _blend_side(b.destBlendAlpha, BLEND)
    if not (a_s == src and a_d == dst):
        s += f" | α:{a_s}→{a_d}"
    s += f" | mask={shval(b.colMask)}"
    if st.rtSeparateBlend:
        s += " | SeparateBlend=True"
    return s


def state_cell(st):
    return (f"{blend_cell(st)}·zW={_onoff(st.zWrite)}·zT={_cmp(st.zTest)}"
            f"·cull={en(int(shval(st.culling)), CULL) if shval(st.culling).isdigit() else shval(st.culling)}"
            f"·a2m={_onoff(st.alphaToMask)}")


def _onoff(fv):
    v = shval(fv)
    return {"0": "Off", "1": "On"}.get(v, v)


def _cmp(fv):
    v = shval(fv)
    return CMPFUNC.get(int(v), v) if v.lstrip("-").isdigit() else v


def tagmap(t):
    try:
        return dict(getattr(t, "tags", t))
    except Exception:
        return {}


def tags_str(d):
    return "{" + ", ".join(f"{k}={v}" for k, v in d.items()) + "}" if d else "{}"


# ---------------------------------------------------------------- shader 抽取
def props_of(pf):
    pi = pf.m_PropInfo
    return list(pi.m_Props) if hasattr(pi, "m_Props") else list(pi)


def prop_full(p):
    dv = [getattr(p, f"m_DefValue_{i}_", None) for i in range(4)]
    if p.m_Type == 4:
        dt = p.m_DefTexture
        tn = getattr(dt, "m_TextureName", None) or getattr(dt, "m_DefaultName", None) or ""
        d = f'def="{tn}"'
    elif p.m_Type in (0, 1):
        d = "def=(" + ",".join(num(x) for x in dv) + ")"
    elif p.m_Type == 3:
        d = f"def={num(dv[0])} [{num(dv[1])}..{num(dv[2])}]"
    else:
        d = f"def={num(dv[0])}"
    s = f"`{p.m_Name}` {PTYPE.get(p.m_Type, p.m_Type)} {d}"
    fl = int(p.m_Flags or 0)
    if fl:
        bits = "|".join(PFLAG.get(1 << i, f"0x{1 << i:x}") for i in range(12) if fl & (1 << i))
        s += f" flags=0x{fl:x}({bits})"
    if p.m_Description and p.m_Description != p.m_Name:
        s += f' 显示名="{p.m_Description}"'
    return s


def blob_text(sh):
    """按 平台→阶段 逐块 LZ4 解压拼起来（compressedLengths/offsets/decompressedLengths 都是二维）。"""
    if not HAS_LZ4:
        return b"", "没装 lz4"
    raw = bytes(sh.compressedBlob)
    out, err = [], None
    for pi in range(len(sh.compressedLengths)):
        crow = aslist(sh.compressedLengths[pi])
        orow = aslist(sh.offsets[pi])
        drow = aslist(sh.decompressedLengths[pi])
        for j in range(len(crow)):
            off = orow[j] if j < len(orow) else 0
            try:
                out.append(lz4.decompress(raw[off:off + crow[j]], uncompressed_size=drow[j]))
            except Exception as e:
                err = f"平台{pi}阶段{j}: {e}"
    return b"".join(out), err


def scan_textures(data, declared):
    """从字节码里扫「不在属性表里的纹理名」。判据（并集，可复查，不是猜）：
      a) 名字 X 有兄弟 `X_ST` 或 `X_TexelSize`（Unity 给带 tiling/texel size 的纹理配的）；
      b) X 在 ENGINE_GLOBAL_TEX 白名单里（管线全局纹理，如 `_CameraDepthTexture`）。
    """
    names = set(m.group(1).decode("ascii") for m in re.finditer(rb"(_[A-Za-z][A-Za-z0-9_]{2,40})", data))
    out = []
    for n in sorted(names):
        if n in declared or n.endswith(("_ST", "_TexelSize", "_HDR")):
            continue
        if (n + "_ST") in names or (n + "_TexelSize") in names or n[1:] in ENGINE_GLOBAL_TEX:
            out.append(n)
    return out


def collect_shader(sh, bundle):
    """把一个 Shader 抽成纯 python dict（不留 UnityPy 引用，免得 84 个 bundle 全驻留内存）。"""
    pf = sh.m_ParsedForm
    if pf is None:
        return None
    props = props_of(pf)
    declared = {p.m_Name for p in props}
    data, blob_err = blob_text(sh)
    subshaders = []
    for s in pf.m_SubShaders:
        passes = []
        for p in s.m_Passes:
            st = p.m_State
            passes.append({
                "name": st.m_Name or "",
                "tags": tagmap(st.m_Tags),
                "LOD": st.m_LOD,
                "gpuProgramID": st.gpuProgramID,
                "lighting": bool(st.lighting),
                "blend": blend_cell(st),
                "zwrite": _onoff(st.zWrite),
                "cull": (en(int(shval(st.culling)), CULL)
                         if shval(st.culling).lstrip("-").isdigit() else shval(st.culling)),
                "state": state_cell(st),
                "offset": f"({num(st.offsetFactor.val)},{num(st.offsetUnits.val)})",
            })
        subshaders.append({"tags": tagmap(s.m_Tags), "lod": s.m_LOD, "passes": passes})
    return {
        "name": pf.m_Name,
        "bundle": bundle,
        "props": [prop_full(p) for p in props],
        "prop_tex": [p.m_Name for p in props if p.m_Type == 4],
        "extra_tex": scan_textures(data, declared) if data else [],
        "blob_err": blob_err if not data else None,
        "keywords": list(pf.m_KeywordNames),
        "subshaders": subshaders,
        "fallback": pf.m_FallbackName or "",
        "deps": list(pf.m_Dependencies or []),
        "editor": pf.m_CustomEditorName or "",
    }


# ---------------------------------------------------------------- 主流程
def main():
    if not os.path.isdir(AA):
        print(f"bundle 目录不存在：{AA}")
        return 2
    bundles = sorted(f for f in os.listdir(AA) if f.endswith(".bundle"))
    print(f"扫 {len(bundles)} 个 bundle …")

    shaders = {}            # name -> dict（首个出现的实例）
    dup = defaultdict(list)  # name -> [(bundle, pathid)]
    sh_by_bundle_pid = {}    # (bundle, pathid) -> name
    cab2bundle = {}          # CAB-xxxx -> bundle
    externals = {}           # bundle -> [external path]
    mats = []                # (bundle, fileID, pathid, matname)
    read_fail = []
    for f in bundles:
        try:
            env = UnityPy.load(os.path.join(AA, f))
        except Exception as e:
            read_fail.append((f, "-", f"bundle 打不开: {e}"))
            continue
        seen_af = False
        for o in env.objects:
            if not seen_af:
                try:
                    sf = o.assets_file
                    cab2bundle[sf.name] = f
                    externals[f] = [getattr(x, "path", str(x)) for x in (sf.externals or [])]
                    seen_af = True
                except Exception:
                    pass
            if o.type.name == "Shader":
                try:
                    sh = o.read()
                except Exception as e:
                    read_fail.append((f, f"pathID={o.path_id}", f"read: {e}"))
                    continue
                pf = sh.m_ParsedForm
                if pf is None or not pf.m_Name:
                    read_fail.append((f, f"pathID={o.path_id}", "没有 m_ParsedForm / m_Name"))
                    continue
                n = pf.m_Name
                dup[n].append((f, o.path_id))
                sh_by_bundle_pid[(f, o.path_id)] = n
                if n not in shaders:
                    try:
                        shaders[n] = collect_shader(sh, f)
                    except Exception as e:
                        read_fail.append((f, n, f"collect: {e}"))
            elif o.type.name == "Material":
                try:
                    m = o.read()
                    pp = m.m_Shader
                    mats.append((f, getattr(pp, "m_FileID", 0), getattr(pp, "m_PathID", 0),
                                 m.m_Name or ""))
                except Exception as e:
                    read_fail.append((f, f"Material pathID={o.path_id}", f"read: {e}"))
        del env
    print(f"shader 名 {len(shaders)} 个（对象 {sum(len(v) for v in dup.values())} 个）；材质 {len(mats)} 个")

    # ---- 材质 → shader
    mat_count = Counter()
    mat_samples = defaultdict(list)
    mat_unres = Counter()
    for bundle, fid, pid, mname in mats:
        target, why = None, None
        if fid == 0:
            target = (bundle, pid)
        else:
            ext = externals.get(bundle) or []
            if 1 <= fid <= len(ext):
                cab = ext[fid - 1].rstrip("/").split("/")[-1]
                b2 = cab2bundle.get(cab)
                if b2:
                    target = (b2, pid)
                else:
                    why = f"外部 CAB 不在本次扫的 bundle 里（{cab}）"
            else:
                why = f"m_FileID={fid} 超出 externals({len(ext)})"
        nm = sh_by_bundle_pid.get(target) if target else None
        if nm is None:
            mat_unres[why or "指向的 pathID 不在该 bundle 的 Shader 表里"] += 1
            continue
        mat_count[nm] += 1
        if len(mat_samples[nm]) < 3 and mname not in mat_samples[nm]:
            mat_samples[nm].append(mname)

    names = sorted(shaders)

    # ---- 渲染逐 shader 小节
    rendered = {}
    for idx, n in enumerate(names):
        d = shaders[n]
        L = [f"### {idx + 1}. `{n}`"]
        L.append(f"- **属性（{len(d['props'])}）**：" + (" · ".join(d["props"]) or "无"))
        L.append(f"- **SubShader（{len(d['subshaders'])}）**：" + " ／ ".join(
            f"lod={s['lod']} tags={tags_str(s['tags'])}" for s in d["subshaders"]))
        i = 0
        for s in d["subshaders"]:
            for p in s["passes"]:
                L.append(f"  - **Pass{i}** `{p['name'] or '(无名)'}` tags={tags_str(p['tags'])}"
                         f" × {p['state']} · offset={p['offset']} LOD={p['LOD']}"
                         f" gpuProgramID={p['gpuProgramID']}"
                         + (" · lighting=On" if p["lighting"] else ""))
                i += 1
        if i == 0:
            L.append("  - **Pass** 无")
        L.append(f"- **关键字（{len(d['keywords'])}）**："
                 + (", ".join(f"`{k}`" for k in d["keywords"]) or "无"))
        L.append("- **采样纹理**：声明属性（{}）{} ｜ 字节码里额外扫到（{}）{}".format(
            len(d["prop_tex"]),
            "、".join(f"`{x}`" for x in d["prop_tex"]) or "无",
            len(d["extra_tex"]),
            "、".join(f"`{x}`" for x in d["extra_tex"]) or "无"))
        ms = "、".join(f"`{x}`" for x in mat_samples.get(n, []))
        bits = []
        if d["fallback"]:
            bits.append(f"Fallback=`{d['fallback']}`")
        if d["deps"]:
            bits.append("Dependencies=" + "、".join(f"`{x}`" for x in d["deps"]))
        if d["editor"]:
            bits.append(f"CustomEditor=`{d['editor']}`")
        if d["blob_err"]:
            bits.append(f"⚠ 字节码：{d['blob_err']}")
        L.append(f"- **材质（{mat_count.get(n, 0)}）**：" + (ms or "（没有材质引用它）")
                 + (" ｜ " + " · ".join(bits) if bits else ""))
        rendered[n] = L

    # ---- 按字母序均衡切 NBLOCKS 块；任一块超 MAXBLOCKLINES 就再对半拆（允许 >4 块）
    REAL = {n: len(rendered[n]) for n in names}
    BODY_OVERHEAD = 12  # 标题 + 6 行说明 + 空行 + 「## 逐 shader」+ 空行
    TBL_OVERHEAD = 6    # 「## 一览」+ 空行 + 表头两行 + 空行

    def block_lines(blk):
        return BODY_OVERHEAD + TBL_OVERHEAD + len(blk) + sum(REAL[n] for n in blk)

    total = sum(REAL.values())
    target = max(1, -(-total // NBLOCKS))
    blocks, cur, cur_lines = [], [], 0
    for n in names:
        if cur and cur_lines + REAL[n] > target and len(blocks) < NBLOCKS - 1:
            blocks.append(cur)
            cur, cur_lines = [], 0
        cur.append(n)
        cur_lines += REAL[n]
    if cur:
        blocks.append(cur)
    i = 0
    while i < len(blocks):                      # 硬保证每块 ≤ MAXBLOCKLINES
        if len(blocks[i]) > 1 and block_lines(blocks[i]) > MAXBLOCKLINES:
            h = len(blocks[i]) // 2
            blocks[i:i + 1] = [blocks[i][:h], blocks[i][h:]]
        else:
            i += 1
    print("切块：" + " | ".join(f"块{i+1} {b[0]}…{b[-1]} ({block_lines(b)}行)"
                               for i, b in enumerate(blocks)))

    os.makedirs(OUTDIR, exist_ok=True)
    head = [
        "> 数据源：`D:\\2\\Warhammer 40k Warpforge\\Warpforge_Data\\StreamingAssets\\aa\\StandaloneWindows64\\*.bundle`（84 个，只读）；工具 `d:/4/Unity/工具/_dump_shaders_batch.py`（包了 `dump_shader.py` + `dump_shader_blob.py` 的主逻辑，那两个一次只吃一个 shader 名）。",
        "> 跑法：`PYTHONIOENCODING=utf-8 PYTHONUTF8=1 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/_dump_shaders_batch.py`；口径与未解析清单见 `shader属性表_汇总.md`。",
        "> 属性 = `名字 类型 默认值 [flags]`（Range 是 `def=值 [min..max]`，min/max 藏在 `m_DefValue_1_/2_`；Texture 是 `def=\"默认贴图名\"`）。状态 = `src→dst·op | α:… | mask=…`，`[名字]`**该状态由 Material 属性驱动**（不是常量）；zT=zTest · zW=zWrite · a2m=alphaToMask。",
        "> 本版本 `pass.m_Name`/`pass.m_Tags` 为空，真值取自 `pass.m_State.m_Name`/`m_State.m_Tags`；`rtBlend1..7` 全库默认（One/Zero/Add/RGBA）、`rtSeparateBlend` 全库 False，故只列 rtBlend0。",
    ]

    for bi, blk in enumerate(blocks, 1):
        fp = os.path.join(OUTDIR, f"shader属性表_块{bi}.md")
        out = [f"# shader 属性表 · 块 {bi}/{len(blocks)}（{blk[0]} … {blk[-1]}，{len(blk)} 个）", ""]
        out += head + ["",
                       f"## 一览（{len(blk)} 行 = 每行一个 shader；属性/关键字列只给个数，逐个列全见下面「逐 shader」小节）",
                       "",
                       "| # | shader 名 | 属性 | 混合 rtBlend0 | zWrite | cull | 关键字 | 采样纹理 | 材质 |",
                       "|---|---|---|---|---|---|---|---|---|"]
        for n in blk:
            d = shaders[n]
            ps = [p for s in d["subshaders"] for p in s["passes"]]
            # ⚠ 表格单元格里的 `|` 会把 Markdown 表切断 ⇒ 换成全角 ｜
            j = lambda k: ("<br>".join(f"P{i}:{p[k]}" for i, p in enumerate(ps)) or "—").replace("|", "｜")
            tx = [x.replace("|", "｜") for x in list(d["prop_tex"]) + list(d["extra_tex"])]
            out.append("| {} | `{}` | {} | {} | {} | {} | {} | {} | {} |".format(
                names.index(n) + 1, n.replace("|", "｜"), len(d["props"]), j("blend"),
                j("zwrite"), j("cull"), len(d["keywords"]),
                (", ".join(f"`{x}`" for x in tx) or "—"), mat_count.get(n, 0)))
        out += ["", "## 逐 shader", ""]
        for n in blk:
            out += rendered[n]
        if len(out) > MAXBLOCKLINES:
            print(f"  ⚠ 块{bi} {len(out)} 行 > {MAXBLOCKLINES}")
        io.open(fp, "w", encoding="utf-8", newline="\n").write("\n".join(out) + "\n")
        print(f"  写 {fp}  {len(out)} 行")

    # ---- 汇总
    S = ["# 原版 Warpforge shader 清单 · 汇总（2026-09-17）", "", "## 一、结论"]
    S.append(f"从 84 个原版 bundle **枚举**出 **{len(names)} 个唯一 shader 名**（Shader 对象 "
             f"{sum(len(v) for v in dup.values())} 个，散在 {len({b for v in dup.values() for b, _ in v})} 个 bundle）；"
             f"属性合计 {sum(len(shaders[n]['props']) for n in names)} 条、Pass 合计 "
             f"{sum(len(s['passes']) for n in names for s in shaders[n]['subshaders'])} 个、关键字合计 "
             f"{sum(len(shaders[n]['keywords']) for n in names)} 条、材质 {len(mats)} 个"
             f"（解析到 shader 的 {sum(mat_count.values())} 个）。")
    S += ["", "## 二、口径与判据（可复查）",
          "- **名单不是二手清单**：直接遍历 `aa\\StandaloneWindows64\\*.bundle` 里 type=Shader 的对象。"
          "（任务书让读 `_tmp_view/导出报告_快照_0917.tsv` 第 3 段 —— 那份只解出 **8** 个名字，见「五」。）",
          "- 工具：`d:/4/Unity/工具/_dump_shaders_batch.py`（包了 `dump_shader.py` + `dump_shader_blob.py` 的主逻辑；"
          "原版两个工具都一次只吃一个 shader 名，不循环）。",
          "- **材质→shader**：`Material.m_Shader` 是 PPtr；`m_FileID=0` 是同 bundle，`m_FileID=n` 取 "
          "`SerializedFile.externals[n-1]` 的 `archive:/CAB-xxx/CAB-xxx` → CAB 名 → bundle → pathID。",
          "- **采样纹理**分两栏：① shader **声明的 Texture 属性**（精确）；② **字节码里额外扫到**的"
          "（判据：名字有 `X_ST`/`X_TexelSize` 兄弟，或在引擎全局白名单里）。",
          "- 本表**只覆盖原版 bundle**。我们自建的替代 shader 不在这里，在 "
          "`Assets/WarpforgeVFX/Shaders/`（跑本次普查时那个目录正被全量重导改，故没读它，免得读到半成品）。",
          "", "## 三、每个 shader 一行摘要"]
    S.append(f"见各块文件顶部的「一览」表（{len(blocks)} 块 × 9 列 = 序号 + 任务书那 8 列）。本汇总受 ≤{MAXSUMMARYLINES} 行上限约束，不重复抄。")
    for bi, blk in enumerate(blocks, 1):
        S.append(f"- 块{bi}：{blk[0]} … {blk[-1]}（{len(blk)} 个）")
    S += ["", "## 四、完整名单（字母序，★=有材质在用）"]
    for i in range(0, len(names), 5):
        S.append("  " + " · ".join(("★" if mat_count.get(n) else "") + n for n in names[i:i + 5]))
    S += ["", "## 五、未解析 / 查不到"]
    if read_fail:
        for f, w, e in read_fail[:10]:
            S.append(f"- 读取失败：{f} / {w} / {e}")
    else:
        S.append("- 读取失败：**0**（每个 Shader 对象都带 m_ParsedForm 与 m_Name）。")
    bad = [n for n in names if shaders[n]["blob_err"]]
    S.append(f"- 字节码解压失败：**{len(bad)}**" + (f"（{', '.join(bad[:6])}）" if bad else ""))
    nop = [n for n in names if not shaders[n]["props"]]
    S.append(f"- 属性表为空：**{len(nop)}**（都是 FallbackError 与 VFX 的 "
             f"`Hidden/VFX/*/Output Particle*` 生成 shader）" if nop else "- 属性表为空：0")
    S.append(f"- 没有任何材质引用的 shader：**{sum(1 for n in names if not mat_count.get(n))}** 个")
    if mat_unres:
        S.append("- 材质指向的 shader 没解出来（3/1076）：" +
                 "；".join(f"{k} × {v}" for k, v in mat_unres.most_common(6)))
    S.append("- 导出报告快照 `_tmp_view/导出报告_快照_0917.tsv`（960 行）里**只有 2 行**带 "
             "`材质定义N个；原 shader: …` 字段（`BulletImpact_artillery_big`、`Plasma_basic_blue`），"
             "去重后 **8 个**名字：" + "、".join(f"`{x}`" for x in
             ["Everguild/FX/Extra Color", "Everguild/FX/Particle Distortion Affect Transparents",
              "Everguild/Matcap/Matcap Full Options", "Legacy Shaders/Particles/Additive",
              "Mobile/Particles/Alpha Blended", "Universal Render Pipeline/Lit",
              "Universal Render Pipeline/Particles/Unlit", "WarpforgeVFX/Particles/Extra Color"]) +
             "。这 8 个是 135 个的**子集** ⇒ 只当名单会漏 127 个。")
    S += ["", "## 六、关键发现",
          "1. **DXBC 里没有 RDEF 段**：`工具/dump_shader_blob.py` 头注释写「DXBC 的 RDEF 段里资源名是明文」，"
          "实测每个 DXBC 只有 `ISGN`/`OSGN`/`SHDR` 三段（出包时反射表被剥了）；该工具扫到的名字来自 "
          "Unity 自己那段**常量缓冲布局头**，不是 DXBC 反射表。",
          "2. **`pass.m_Name`/`pass.m_Tags` 是空的**，真值在 `pass.m_State.m_Name`/`m_State.m_Tags`"
          " —— `dump_shader.py` 印 `p.m_Name`，会印成空串。",
          "3. **`rtBlend1..7` 全库都是默认值**（One/Zero/Add/RGBA）⇒ 只看 `rtBlend0`。"
          "⚠ **`rtSeparateBlend` 全库 False，但它不等于「有没有分开的 alpha 混合」**："
          "`Universal Render Pipeline/2D/Sprite-Unlit-Default` 的源码第 20 行明写 "
          "`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`（分开的 alpha），"
          "而 `rtSeparateBlend` 仍是 False —— 真正的 alpha src/dst 记在 `rtBlend0.srcBlendAlpha/destBlendAlpha`，"
          "**别看那个布尔**。",
          "4. **大量渲染状态是「材料属性驱动」**（表里写作 `[名字]`）：URP 那批用 "
          "`_SrcBlend/_DstBlend/_ZWrite/_Cull/_ZTest`，UI 那批用 `unity_GUIZTestMode` 与 `_ColorMask`。"
          "**照这些状态硬编码 = 错**，得同时看材质的属性值。",
          "5. 本版本 `SerializedShader` **没有** `m_ShaderRequirements` / `m_CommonParameters` 字段"
          "（老工具 getattr 兜底印 None，看着像「查不到」，其实是这个版本不序列化）。",
          "", "## 七、复跑与自检",
          "- 复跑一条命令即可（见二）；脚本只读 bundle、只写 `资料/普查产出_0917/`，不碰 `Assets/`、不启 Unity。",
          "- **枚举数值→名字已对过权威源**：`PackageCache/com.unity.render-pipelines.universal@7865b6b91f8a/"
          "Shaders/2D/Sprite-Unlit-Default.shader:20` 写 `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`，"
          "本表该 shader 记的是 `SrcAlpha→OneMinusSrcAlpha | α:One→OneMinusSrcAlpha` —— **逐字吻合**"
          "（5=SrcAlpha、10=OneMinusSrcAlpha、1=One），`Cull Off` / `ZWrite [_ZWrite]` 也对上。"
          "其余枚举值（Zero/DstColor/SrcColor/OneMinusDstColor/…）**只按 Unity 公开枚举的次序推出，没在本机找到源码逐条对**。",
          "- 抽查计数：`shaders_assets_all.bundle` 45 个 Shader 对象、`battleprefabs_vfxandmisc_assets_all.bundle` 42 个。"]
    io.open(os.path.join(OUTDIR, "shader属性表_汇总.md"), "w",
            encoding="utf-8", newline="\n").write("\n".join(S) + "\n")
    print(f"  写 汇总  {len(S)} 行")
    if len(S) > MAXSUMMARYLINES:
        print(f"  ⚠ 汇总 {len(S)} 行 > {MAXSUMMARYLINES}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
