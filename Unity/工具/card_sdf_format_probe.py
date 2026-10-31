#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""card_sdf_format_probe.py — 卡框 SDF 贴图：「`.meta` 说要什么 / 原版是什么」的读数探针

为什么要有它
------------
第一百零八会话（`B108_A17`）把 `Resources/Art/card_sdf/` 的 105 份 `.meta` 的
`textureCompression` 改成 `0`（Uncompressed），`ArtBaker.ApplyImportSettings` 也跑过 rc=0
—— 但**没有任何人验证过「导入之后真正的 `Texture2D.format` 是不是我们要的」**。
`.meta` 只决定「下次导入用什么参数」，「导完之后引擎里到底是 RGBA32 还是 BC7」只有 Unity 知道。

本脚本做**能静态做的那一半**（三个模式，全部只读）：

| 模式 | 读什么 | 能回答 |
|---|---|---|
| `--meta` | `Resources/Art/card_sdf/*.png.meta` + PNG 头（IHDR） | 我们**声明的**生效压缩档对不对、尺寸/mip/sRGB 有没有被谁改回去 |
| `--orig` | `D:/2/unity_run_ref/.../StandaloneWindows64/*.bundle`（UnityPy） | **原版**那张贴图的 `m_TextureFormat` / `m_ColorSpace` / 尺寸 / mip |
| `--template` | `工具/import_original_card_sdf.py` 的 `render_meta()` | 「删掉 `.meta` 重跑导入脚本」会产出**什么平台块**，与盘上现状一致吗 |
| `--all` | 上面三个 + 逐文件 join | 一张表看完：原版 fmt → 模板声明 → 盘上声明 → 推定落档 |

🔴 **本脚本证明不了的一件事**（那一步只有 Unity 能答）：
**导入后真正的 `Texture2D.format`**。入口已备好：
`Unity/MyGame/Assets/CardPresentation/Editor/B109_E3_SdfFormatProbe.cs`
（`-executeMethod B109_E3_SdfFormatProbe.Run`，归调度台跑，⛔ 本脚本不碰 Unity）。

判据出处
--------
* `Unity/资料/完全复刻/判据/B108_AUD09_纹理格式.md`（R7：原版 `m_TextureFormat` 实读 + 枚举值）
* `Unity/资料/完全复刻/报告/B108_A17.md`（105 份 `.meta` 已改、`ArtBaker` 已加 `isSdf` 豁免）
* `Unity/资料/完全复刻/报告/B109_E3.md`（本脚本的产出与读数）

用法
----
    PY="D:/2/Warpforge_tools/py312/python.exe"
    "$PY" -I -X utf8 d:/4/Unity/工具/card_sdf_format_probe.py --all
    "$PY" -I -X utf8 d:/4/Unity/工具/card_sdf_format_probe.py --meta --out d:/4/_tmp_view/b109_e3/meta.tsv
    "$PY" -I -X utf8 d:/4/Unity/工具/card_sdf_format_probe.py --orig

⛔ **不带模式参数 = 只打印用法并退出**（本脚本除 `--out` 外不写任何文件）。
退出码：0 = 全部符合期望 · 1 = 有不符合 · 2 = 用法错 · 3 = 依赖/IO 缺失（读不到 bundle / UnityPy）。
"""

import argparse
import glob
import io
import os
import struct
import sys

TOOL_DIR = os.path.dirname(os.path.abspath(__file__))
if TOOL_DIR not in sys.path:
    sys.path.insert(0, TOOL_DIR)
# ⛔ 别在仓库里留 `__pycache__`：本脚本要 import 同目录的导入脚本，默认会写字节码缓存。
sys.dont_write_bytecode = True
# 复用导入脚本的常量与模板渲染（⛔ 别在这里抄第二份 FACTION / 文件名规则 —— 两处写同一条规则迟早不一致）
import import_original_card_sdf as sdf  # noqa: E402

# 原版 bundle（PC 版 6000.2.6f2，与 `assets_full/` 同一份导出源 —— 见 R7 §1·1）
BUNDLE_DIR = "D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"

# `TextureFormat` 枚举（R7 §1·3 从本机 Unity 程序集实读；这里只列本工程会碰到的几档）
TEXFMT_NAME = {
    1: "Alpha8", 3: "RGB24", 4: "RGBA32", 5: "ARGB32",
    10: "DXT1", 12: "DXT5", 24: "BC6H", 25: "BC7",
    28: "DXT1Crunched", 29: "DXT5Crunched",
}
# UnityPy 扫描时顺带认识的原版贴图名字（不在 `40k_Cardframes_*_SDF_tier*` 规则里的那两张）
ORIG_EXTRA = {
    "Noise Combined": "cardback_noise.png",
    "Card board frame SDF": "generic.png",
}
# 两张 keeper 的**原版**就是 BC7（R7 §3·1）；其余 SDF 是 RGBA32 未压缩
KEEP_FILES = {"generic.png", "card_board_frame_sdf.png"}

# `.meta` 里我们要求的硬不变量（判据见模块 docstring）
REQ_SRGB = 0
REQ_MIP = 0
REQ_NPOT = 0
REQ_TEXTYPE = 0
COMP_UNCOMPRESSED, COMP_COMPRESSED_HQ = 0, 2


# ---------------------------------------------------------------------------
# PNG 头
# ---------------------------------------------------------------------------
def png_ihdr(path):
    """返回 (w, h, bitdepth, colortype)；读不出返回 None。colortype 6 = RGBA · 2 = RGB。"""
    try:
        b = io.open(path, "rb").read(33)
    except (IOError, OSError):
        return None
    if len(b) < 33 or b[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    w, h = struct.unpack(">II", b[16:24])
    return w, h, b[24], b[25]


def implied_format(comp, colortype):
    """**推定**「这个压缩档 + 这个 PNG 颜色类型」会落成哪个 `TextureFormat`。

    ⚠️ **这是推定，不是实读** —— 真正的值只能由 Unity 给（见模块 docstring 那条）。
    `CompressedHQ` 在桌面上按 BC7/ASTC 落地（R7 §二 A/B/C 与 `B100_F13.md:83` 同口径）。
    """
    has_alpha = colortype in (4, 6) or colortype == 3
    if comp == 0:                     # Uncompressed
        return 4 if has_alpha else 3  # RGBA32 / RGB24
    if comp == 2:                     # CompressedHQ（桌面 = BC7）
        return 25
    if comp == 1:                     # Compressed（DXT；带 alpha 走 DXT5）
        return 12 if has_alpha else 10
    return None


# ---------------------------------------------------------------------------
# `.meta` 解析（只按缩进切，不引 yaml 库）
# ---------------------------------------------------------------------------
def parse_meta(path):
    """读一份 TextureImporter `.meta`。返回 dict；读不出返回 None。

    取的东西：`mipmaps` 块的 `sRGBTexture` / `enableMipMap`、顶层标量、
    以及 `platformSettings` 的每条块（`buildTarget` 为键）。
    """
    try:
        text = io.open(path, "rb").read().decode("utf-8")
    except (IOError, OSError):
        return None
    except UnicodeDecodeError:
        return None

    out = {"platform": {}}
    lines = text.splitlines()
    section = None          # 'mipmaps' / 'platformSettings' / None
    cur = None              # 当前平台块
    for ln in lines:
        if not ln.strip():
            continue
        indent = len(ln) - len(ln.lstrip())
        s = ln.strip()

        if indent == 2 and s.endswith(":"):
            section = s[:-1]
            cur = None
            continue

        if section == "mipmaps" and indent >= 4:
            if ":" in s:
                k, v = s.split(":", 1)
                out[k.strip()] = v.strip()
            continue

        if section == "platformSettings" and (indent >= 4 or s.startswith("- ")):
            if s.startswith("- "):
                cur = {}
                body = s[2:]
                if ":" in body:
                    k, v = body.split(":", 1)
                    cur[k.strip()] = v.strip()
                continue
            if cur is not None and ":" in s:
                k, v = s.split(":", 1)
                cur[k.strip()] = v.strip()
                if k.strip() == "buildTarget":
                    out["platform"][v.strip()] = cur
            continue

        # 其它 2 缩进标量（⚠️ 只认第一处出现，重复键在后面，本探针不关心）
        if indent == 2 and ":" in s and not s.startswith("- "):
            k, v = s.split(":", 1)
            k, v = k.strip(), v.strip()
            if k and v and k not in out:
                out[k] = v

    return out


def num(v):
    """把 YAML 里的 `"0"` / `"-1"` 变成 int；变不了返回 None。"""
    try:
        return int(str(v).strip())
    except (TypeError, ValueError):
        return None


# ---------------------------------------------------------------------------
# 模式 1：--meta
# ---------------------------------------------------------------------------
def klass_of(name):
    return "keeper" if name.lower() in KEEP_FILES else "sdf"


def probe_meta(dest_dir):
    """扫 `.meta`，返回 (rows, problems)。rows 是 dict 列表（直接可打 TSV）。"""
    rows, problems = [], []
    if not os.path.isdir(dest_dir):
        problems.append("目录不存在：" + dest_dir)
        return rows, problems

    for f in sorted(os.listdir(dest_dir)):
        if not f.lower().endswith(".png.meta"):
            continue
        png = os.path.join(dest_dir, f[:-5])
        m = parse_meta(os.path.join(dest_dir, f))
        if m is None:
            problems.append("meta 读不出：" + f)
            continue

        kl = klass_of(f[:-5])
        plat = m.get("platform", {})
        dp = plat.get("DefaultTexturePlatform", {})
        sp = plat.get("Standalone", {})

        d_comp = num(dp.get("textureCompression"))
        s_comp = num(sp.get("textureCompression"))
        d_max = num(dp.get("maxTextureSize"))
        s_max = num(sp.get("maxTextureSize"))

        ih = png_ihdr(png)
        w, h, _bd, ct = ih if ih else (None, None, None, None)
        imp = implied_format(d_comp, ct) if ct is not None else None

        issues = []
        if num(m.get("sRGBTexture")) != REQ_SRGB:
            issues.append("sRGB!=0")
        if num(m.get("enableMipMap")) != REQ_MIP:
            issues.append("mip!=0")
        if num(m.get("nPOTScale")) != REQ_NPOT:
            issues.append("npot!=0")
        if num(m.get("textureType")) != REQ_TEXTYPE:
            issues.append("type!=Default")
        want = COMP_COMPRESSED_HQ if kl == "keeper" else COMP_UNCOMPRESSED
        if not plat:
            issues.append("无 platformSettings")
        elif d_comp != want:
            issues.append("Default comp=%s(期望%d)" % (d_comp, want))
        if kl == "sdf" and s_comp != COMP_UNCOMPRESSED:
            issues.append("Standalone comp=%s(死值,期望0)" % (s_comp,))
        if kl == "keeper" and s_comp != 1:
            issues.append("Standalone comp=%s(keeper 盘上是1)" % (s_comp,))
        if d_max != 1024:
            issues.append("Default maxTextureSize=%s(期望1024)" % (d_max,))
        if s_max != 2048:
            issues.append("Standalone maxTextureSize=%s(盘上2048)" % (s_max,))
        if w is not None and d_max is not None and max(w, h) > d_max:
            issues.append("PNG %dx%d > max %d(会被降采样)" % (w, h, d_max))

        if issues:
            problems.append("%s: %s" % (f, " · ".join(issues)))

        rows.append({
            "file": f[:-5],
            "class": kl,
            "png_wh": "%sx%s" % (w, h) if w is not None else "?",
            "colortype": str(ct) if ct is not None else "?",
            "sRGB": m.get("sRGBTexture", "?"),
            "mip": m.get("enableMipMap", "?"),
            "npot": m.get("nPOTScale", "?"),
            "type": m.get("textureType", "?"),
            "alphaTransp": m.get("alphaIsTransparency", "?"),
            "readable": m.get("isReadable", "?"),
            "defComp": dp.get("textureCompression", "?"),
            "defMax": dp.get("maxTextureSize", "?"),
            "defFmt": dp.get("textureFormat", "?"),
            "defOver": dp.get("overridden", "?"),
            "stdComp": sp.get("textureCompression", "?"),
            "stdMax": sp.get("maxTextureSize", "?"),
            "stdOver": sp.get("overridden", "?"),
            "impliedFmt": "%s(%s)" % (imp, TEXFMT_NAME.get(imp, "?")) if imp is not None else "?",
            "verdict": "OK" if not issues else " · ".join(issues),
        })
    return rows, problems


META_COLS = ["file", "class", "png_wh", "colortype", "sRGB", "mip", "npot", "type",
             "alphaTransp", "readable", "defComp", "defMax", "defFmt", "defOver",
             "stdComp", "stdMax", "stdOver", "impliedFmt", "verdict"]


# ---------------------------------------------------------------------------
# 模式 2：--orig（回原版 bundle 实读）
# ---------------------------------------------------------------------------
def ours_name_for_orig(orig_name):
    """原版 Texture2D 名 → 我们工程里的文件名（规则**复用导入脚本的 `RE` + `FACTION`**）。"""
    if orig_name in ORIG_EXTRA:
        return ORIG_EXTRA[orig_name]
    m = sdf.RE.search(orig_name + ".png")
    if not m:
        return None
    kind, fac, tier = m.group(1), m.group(2), m.group(3)
    key = sdf.FACTION.get(fac)
    if key is None:
        return None
    return "%s%s_tier%s.png" % (key, "_strat" if kind.lower() == "stratagem" else "", tier)


def probe_orig(bundle_dir):
    """读原版 13 个 cardassets bundle + 两张散图所在包。返回 (rows, problems)。"""
    try:
        import UnityPy
    except ImportError as e:
        return [], ["UnityPy 不可用（%s）—— 用 D:/2/Warpforge_tools/py312/python.exe 跑本脚本" % e]

    bundles = sorted(glob.glob(os.path.join(bundle_dir, "*cardassets_assets_all.bundle")))
    for extra in ("duplicateassetisolation_assets_all.bundle",
                  "battleprefabs_vfxandmisc_assets_all.bundle"):
        p = os.path.join(bundle_dir, extra)
        if os.path.isfile(p):
            bundles.append(p)

    rows, problems = [], []
    if not bundles:
        return [], ["bundle 目录里没扫到任何 cardassets 包：" + bundle_dir]

    seen = set()
    for bp in bundles:
        try:
            env = UnityPy.load(bp)
        except Exception as e:                      # noqa: BLE001 —— 坏包不该中断整轮
            problems.append("读不了 %s: %s" % (os.path.basename(bp), e))
            continue
        for obj in env.objects:
            if obj.type.name != "Texture2D":
                continue
            try:
                d = obj.read()
            except Exception:                       # noqa: BLE001
                continue
            name = str(getattr(d, "m_Name", ""))
            ours = ours_name_for_orig(name)
            if ours is None or ours in seen:
                continue
            seen.add(ours)
            mip = getattr(d, "m_MipCount", None)
            if mip is None:
                mip = getattr(d, "m_MipMap", None)
            fmt = getattr(d, "m_TextureFormat", None)
            rows.append({
                "origName": name,
                "ourFile": ours,
                "w": getattr(d, "m_Width", "?"),
                "h": getattr(d, "m_Height", "?"),
                "fmt": fmt,
                "fmtName": TEXFMT_NAME.get(fmt, "?"),
                "colorSpace": getattr(d, "m_ColorSpace", "?"),
                "mip": mip if mip is not None else "?",
                "bundle": os.path.basename(bp),
            })
    rows.sort(key=lambda r: r["ourFile"])
    return rows, problems


ORIG_COLS = ["ourFile", "origName", "w", "h", "fmt", "fmtName", "colorSpace", "mip", "bundle"]


# ---------------------------------------------------------------------------
# 模式 3：--template（导入脚本「删 meta 重跑」会产出什么）
# ---------------------------------------------------------------------------
def probe_template(dest_dir):
    """对每个 PNG 调 `render_meta()`，把它的平台块与盘上 `.meta` 的平台块逐字比。

    这证明的是「**模板已把 `textureCompression` 钉死**」，⛔ 不是「Unity 导出来一定那样」。
    """
    rows, problems = [], []
    if not os.path.isdir(dest_dir):
        return [], ["目录不存在：" + dest_dir]

    def platform_lines(text):
        ls = text.splitlines()
        try:
            i = ls.index("  platformSettings:")
        except ValueError:
            return None
        out = []
        for ln in ls[i + 1:]:
            if ln.startswith("  - ") or ln.startswith("    "):
                out.append(ln.rstrip())
            else:
                break
        return out

    for f in sorted(os.listdir(dest_dir)):
        if not f.lower().endswith(".png"):
            continue
        png = os.path.join(dest_dir, f)
        meta = png + ".meta"
        try:
            t1 = sdf.render_meta(png)
            t2 = sdf.render_meta(png)
        except Exception as e:                      # noqa: BLE001
            problems.append("%s: render_meta 抛错 %s" % (f, e))
            continue
        stable = (t1 == t2)
        tblk = platform_lines(t1)
        on_disk = platform_lines(io.open(meta, "rb").read().decode("utf-8")) if os.path.isfile(meta) else None
        same = (tblk == on_disk)
        # 取 DefaultTexturePlatform 块里那条 `textureCompression`（⚠️ 别按下标硬取 —— 块字段会增删）
        comp_line = next((x for x in (tblk or []) if x.strip().startswith("textureCompression:")), None)
        tmpl_comp = comp_line.split(":", 1)[1].strip() if comp_line else "?"
        if not stable:
            problems.append("%s: render_meta 两次不一致（模板有不确定性）" % f)
        if on_disk is None:
            problems.append("%s: 盘上没有可比的 platformSettings（meta 缺失或格式不同）" % f)
        elif not same:
            problems.append("%s: 模板产出的平台块与盘上 meta 不一致" % f)
        rows.append({
            "file": f,
            "templateStable": "1" if stable else "0",
            "matchesOnDisk": "1" if same else "0",
            "tmplComp": tmpl_comp,
        })
    return rows, problems


TMPL_COLS = ["file", "templateStable", "matchesOnDisk", "tmplComp"]


# ---------------------------------------------------------------------------
# 模式 4：--all（三个一起 + join）
# ---------------------------------------------------------------------------
def probe_all(dest_dir, bundle_dir):
    rows, problems = [], []

    meta_rows, meta_problems = probe_meta(dest_dir)
    tmpl_rows, tmpl_problems = probe_template(dest_dir)
    orig_rows, orig_problems = probe_orig(bundle_dir)
    problems += meta_problems + tmpl_problems + orig_problems

    meta_by = {r["file"]: r for r in meta_rows}
    tmpl_by = {r["file"]: r for r in tmpl_rows}
    joined = []
    for o in orig_rows:
        f = o["ourFile"]
        mr = meta_by.get(f, {})
        tr = tmpl_by.get(f, {})
        # 原版的 fmt → 我们应当声明什么压缩档
        want_comp = COMP_UNCOMPRESSED if o["fmt"] == 4 else (COMP_COMPRESSED_HQ if o["fmt"] == 25 else None)
        got_comp = num(mr.get("defComp"))
        ok = (want_comp is None) or (got_comp == want_comp)
        if not ok:
            problems.append("%s: 原版 fmt=%s(%s) ⇒ 应 comp=%s，我们 Default comp=%s"
                            % (f, o["fmt"], o["fmtName"], want_comp, mr.get("defComp", "?")))
        joined.append({
            "ourFile": f,
            "origFmt": "%s(%s)" % (o["fmt"], o["fmtName"]),
            "origWH": "%sx%s" % (o["w"], o["h"]),
            "origCS": o["colorSpace"],
            "origMip": o["mip"],
            "wantComp": want_comp if want_comp is not None else "?",
            "metaComp": mr.get("defComp", "?"),
            "stdComp": mr.get("stdComp", "?"),
            "impliedFmt": mr.get("impliedFmt", "?"),
            "tmplComp": tr.get("tmplComp", "?"),
            "pngWH": mr.get("png_wh", "?"),
            "verdict": "OK" if ok else "原版 vs 我们 压缩档不一致",
        })
    # 只在 --meta 里出现、对不上原版的（`Card_board_frame_SDF` 属此类：原版那张是同一源的裁片）
    unmatched = sorted(set(meta_by) - {j["ourFile"] for j in joined})
    return joined, problems, unmatched


ALL_COLS = ["ourFile", "origFmt", "origWH", "origCS", "origMip", "wantComp",
            "metaComp", "stdComp", "impliedFmt", "tmplComp", "pngWH", "verdict"]


# ---------------------------------------------------------------------------
# 输出
# ---------------------------------------------------------------------------
def print_table(cols, rows, limit=None):
    widths = {c: len(c) for c in cols}
    for r in rows:
        for c in cols:
            widths[c] = max(widths[c], len(str(r.get(c, ""))))
    print("  ".join(c.ljust(widths[c]) for c in cols))
    print("  ".join("-" * widths[c] for c in cols))
    for r in rows[:(limit or len(rows))]:
        print("  ".join(str(r.get(c, "")).ljust(widths[c]) for c in cols))
    if limit and len(rows) > limit:
        print("  … 另有 %d 行（用 --out 存 TSV 看全）" % (len(rows) - limit))


def write_tsv(path, cols, rows):
    d = os.path.dirname(os.path.abspath(path))
    if d and not os.path.isdir(d):
        os.makedirs(d)
    with io.open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\t".join(cols) + "\n")
        for r in rows:
            fh.write("\t".join(str(r.get(c, "")).replace("\t", " ") for c in cols) + "\n")
    print("\n[out] " + path)


def main():
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument("--meta", action="store_true", help="扫我们工程里的 .meta（只读）")
    ap.add_argument("--orig", action="store_true", help="回原版 bundle 读 Texture2D 头（只读，需 UnityPy）")
    ap.add_argument("--template", action="store_true", help="导入脚本模板 vs 盘上 meta 的平台块（只读）")
    ap.add_argument("--all", action="store_true", help="上面三个 + 逐文件 join")
    ap.add_argument("--out", default="", help="把主表写成 TSV（不给就只打印）")
    ap.add_argument("--dir", default="", help="要扫的目录（默认 = 导入脚本的 DEST_DIR；只读，供自测/复跑用）")
    ap.add_argument("--limit", type=int, default=20, help="屏幕上最多打几行（默认 20；--out 不受限）")
    args = ap.parse_args()

    if not (args.meta or args.orig or args.template or args.all):
        print(__doc__)
        print("⛔ 没给模式参数 —— 只打印用法，什么都没做。")
        return 2

    dest = args.dir or sdf.DEST_DIR
    print("工程目录：" + dest)
    print("原版 bundle：" + BUNDLE_DIR)
    print("")

    problems = []
    rc = 0
    unmatched = []

    if args.meta or args.all:
        rows, p = probe_meta(dest)
        print("== --meta：我们工程里的 .meta（%d 份）==" % len(rows))
        print_table(META_COLS, rows, args.limit)
        problems += p
        if args.out and args.meta and not args.all:
            write_tsv(args.out, META_COLS, rows)
        print("")

    if args.orig or args.all:
        rows, p = probe_orig(BUNDLE_DIR)
        print("== --orig：原版 bundle 实读（%d 张）==" % len(rows))
        print_table(ORIG_COLS, rows, args.limit)
        problems += p
        if args.out and args.orig and not args.all:
            write_tsv(args.out, ORIG_COLS, rows)
        print("")

    if args.template or args.all:
        rows, p = probe_template(dest)
        print("== --template：删 .meta 重跑导入脚本会产出什么（%d 份）==" % len(rows))
        print_table(TMPL_COLS, rows, args.limit)
        problems += p
        if args.out and args.template and not args.all:
            write_tsv(args.out, TMPL_COLS, rows)
        print("")

    if args.all:
        rows, p, unmatched = probe_all(dest, BUNDLE_DIR)
        print("== join：原版 fmt → 我们声明的压缩档（%d 张）==" % len(rows))
        print_table(ALL_COLS, rows, args.limit if args.limit else None)
        problems += p
        if args.out:
            write_tsv(args.out, ALL_COLS, rows)
        print("")

    if unmatched:
        print("ℹ️ 对不上原版文件的（%d）：%s" % (len(unmatched), ", ".join(unmatched[:5])
                                          + (" …" if len(unmatched) > 5 else "")))
        print("   `Card_board_frame_SDF.png` 是 `Card board frame SDF` 的裁片（79×107），原版同属 BC7。")
        print("")

    print("🔴 **本脚本读不到的东西**：导入之后真正的 `Texture2D.format` —— 那一步只有 Unity 能给。")
    print("   入口：`-executeMethod B109_E3_SdfFormatProbe.Run`（Editor/B109_E3_SdfFormatProbe.cs）")
    print("")

    if problems:
        print("!! 共 %d 条不符合：" % len(problems))
        for x in problems[:60]:
            print("   -", x)
        if len(problems) > 60:
            print("   … 另有 %d 条" % (len(problems) - 60))
        rc = 1
    else:
        print("✅ 静态侧全部符合期望（真正的 Texture2D.format 仍需 Unity 侧探针）。")

    # 依赖/IO 缺失不算「不符合」，单独给 3（便于调度台区分）
    if any(("bundle 目录" in x) or ("UnityPy 不可用" in x) or ("目录不存在" in x) for x in problems):
        rc = 3
    return rc


if __name__ == "__main__":
    sys.exit(main())
