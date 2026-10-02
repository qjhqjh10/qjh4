# -*- coding: utf-8 -*-
"""把原版 shader 的编译字节码（DXBC）**反汇编**出来 —— 解开「frag 到底怎么算」这个黑盒。

**为什么要它**：`工具/dump_shader_blob.py` 只读 **RDEF**（资源名是明文），读不出**算式**。
而 E 组长尾里最大的单点黑盒就是 `Everguild/FX/Extra Color` 的 **`_Color` / `IN.color` 语义**
（`WFParticlesExtraColor.shader:176` 是 `tex*_Color*IN.color`，原版那份**读不到**就定不了案）。
它 **20 个变体都带 `SHDR` 指令段**（只缺 RDEF）⇒ **指令流是能读的**。

**怎么反汇编**：不下载任何东西 —— **Windows 自带 `d3dcompiler_47.dll`**，里面就有
`D3DDisassemble`（ctypes 调 COM 的 `ID3DBlob` 取结果）。比装 RenderDoc/dxc 轻得多。

用法（必须 UTF-8）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
    d:/4/Unity/工具/disasm_dxbc.py "Extra Color" [--stage ps] [--out d:/4/_tmp_view/ec.asm]

  --stage ps|vs|all   只看某阶段（默认 all）
  --limit N           每个变体最多打多少行（默认不截断）
"""
import ctypes
import io
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

BUNDLE_DIR = "D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
# ⚠️ 2026-09-19 更正：这张表**原来声明了却没用** —— 代码走的是 `dump_shader_blob.BUNDLES`（只有 3 个包），
#    于是 `Mobile/Particles/*`、`Legacy Shaders/Particles/*`、`Particles/Standard Unlit` 一律「命中 0 个」。
#    实测它们全在 **`Warpforge_unitybuiltinassets.bundle`**（原版把内置 shader 的编译产物打进了这个包），
#    而 `Everguild/FX/{Burning Dissolve,Rays For Trail,Spiral Trail FX}` 等在 **`battleprefabs_vfxandmisc_assets_all.bundle`**。
#    —— 与 §记忆里那条「第一次只读了一个包 ⇒ 误报『没有』」是**同一个形状的错**，别再犯。
BUNDLES = ["battleprefabs_vfxandmisc_assets_all.bundle",
           "Warpforge_unitybuiltinassets.bundle",
           "wf_shaders.bundle",
           "wf_shaders_extra.bundle",
           "shaders_assets_all.bundle",
           # 🔴 **2026-09-21 补**：原来是 5 个，**漏了这个** —— 而战场那两族
           #    `Everguild/FX/Tyranids/{Pulsating Mesh, Tyranid Tentacle}` **只在这一个包里**
           #    （13 场 23 + 9 个材质）。不补就永远得到「命中 0 个」的**假结论**。
           #    —— 和文件头那条「第一次只读了一个包 ⇒ 误报『没有』」是**同一个形状的错，第三次踩**。
           #    ⚠️ 报「没有」之前，先把这个列表打出来。
           "battlesharedresources_assets_all.bundle"]

# 项目里随包走的那两个（`Assets/StreamingAssets/WarpforgeVFX/`）—— 上面按名字找不到时来这里找
PROJECT_BUNDLE_DIR = "D:/4/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX"

STAGE = {0xFFFF: "?", 0xFFFE: "vs", 0x4753: "gs", 0x4853: "hs", 0x4453: "ds",
         0x5053: "ps", 0x4353: "cs"}


def _d3d_disassemble(data):
    """→ 反汇编文本；失败返回 None。（`D3DDisassemble` + ID3DBlob 取结果）"""
    try:
        d3d = ctypes.WinDLL("d3dcompiler_47")
    except OSError:
        return None
    fn = d3d.D3DDisassemble
    fn.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_uint, ctypes.c_char_p,
                   ctypes.POINTER(ctypes.c_void_p)]
    fn.restype = ctypes.c_long
    buf = ctypes.create_string_buffer(bytes(data), len(data))
    out = ctypes.c_void_p()
    if fn(ctypes.cast(buf, ctypes.c_void_p), len(data), 0, None, ctypes.byref(out)) != 0:
        return None
    vtb = ctypes.cast(out, ctypes.POINTER(ctypes.POINTER(ctypes.c_void_p))).contents
    GetPtr = ctypes.WINFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p)(vtb[3])
    GetSize = ctypes.WINFUNCTYPE(ctypes.c_size_t, ctypes.c_void_p)(vtb[4])
    Release = ctypes.WINFUNCTYPE(ctypes.c_ulong, ctypes.c_void_p)(vtb[2])
    try:
        s = ctypes.string_at(GetPtr(out), GetSize(out)).decode("utf-8", "replace")
    finally:
        Release(out)
    return s


def split_dxbc(blob):
    """拼接流里按 `DXBC` 魔数切成一段段；返回 [(stage_hint, bytes)]。"""
    out = []
    pos = 0
    while True:
        i = blob.find(b"DXBC", pos)
        if i < 0:
            break
        if i + 28 > len(blob):
            break
        size = int.from_bytes(blob[i + 24:i + 28], "little")
        if size <= 0 or i + size > len(blob):
            pos = i + 4
            continue
        chunk = blob[i:i + size]
        # 从 SHEX/ISGN 之类看不稳，先按 chunk 里的 stage 猜（反汇编文本里也有）
        out.append((None, chunk))
        pos = i + size
    return out


def _bundle_paths(dsb):
    """要找的 bundle 全路径：先按 `BUNDLES` 里的名字在 AA 目录 / 项目的 StreamingAssets 里找，
    找不到再退回 `dump_shader_blob.BUNDLES` 那几条绝对路径（去重、保序）。"""
    out, seen = [], set()
    for n in BUNDLES:
        for d in (BUNDLE_DIR, PROJECT_BUNDLE_DIR):
            p = os.path.join(d, n)
            if os.path.exists(p) and p not in seen:
                seen.add(p)
                out.append(p)
                break
    for p in getattr(dsb, "BUNDLES", []):
        if os.path.exists(p) and p not in seen:
            seen.add(p)
            out.append(p)
    return out


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    if len(sys.argv) < 2:
        print(__doc__)
        return 1
    want = sys.argv[1]
    stage = "all"
    if "--stage" in sys.argv:
        stage = sys.argv[sys.argv.index("--stage") + 1]
    limit = 0
    if "--limit" in sys.argv:
        limit = int(sys.argv[sys.argv.index("--limit") + 1])
    outpath = None
    if "--out" in sys.argv:
        outpath = sys.argv[sys.argv.index("--out") + 1]

    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import dump_shader_blob as dsb   # 复用它的 blob_of()

    import UnityPy
    found = 0
    lines = []
    paths = _bundle_paths(dsb)

    def scan(p):
        """扫一个包，打印命中的 shader；返回命中数。"""
        nonlocal found
        n = 0
        bn = os.path.basename(p)
        env = UnityPy.load(p)
        for obj in env.objects:
            if obj.type.name != "Shader":
                continue
            try:
                sh = obj.read()
            except Exception:
                continue
            # ⚠️ 名字在 `m_ParsedForm.m_Name`，不是 `sh.m_Name`（照抄 dump_shader_blob.py）
            pf = sh.m_ParsedForm
            name = ((pf.m_Name if pf else "") or "")
            if want.lower() not in name.lower():
                continue
            found += 1
            n += 1
            print("=" * 70)
            print("Shader: %s  (bundle %s)" % (name, bn))
            try:
                blob = dsb.blob_of(sh)
            except Exception as e:
                print("  blob 解不开: %s" % e)
                continue
            segs = split_dxbc(blob)
            print("  DXBC 段 = %d" % len(segs))
            for k, (_st, seg) in enumerate(segs):
                text = _d3d_disassemble(seg)
                if text is None:
                    print("  [%d] 反汇编失败（%d 字节）" % (k, len(seg)))
                    continue
                head = text.split("\n")[:6]
                m = re.search(r"^(ps|vs|gs|hs|ds|cs)_", text, re.M)
                st = m.group(1) if m else "?"
                if stage != "all" and st != stage:
                    continue
                print("\n----- [%d] %s  %d 字节 -----" % (k, st, len(seg)))
                body = text.split("\n")
                if limit:
                    body = body[:limit]
                for l in body:
                    print("   " + l)
                    lines.append(l)
        return n

    for p in paths:
        scan(p)

    # 🔴 **2026-10-02：0 命中的兜底 —— 把 AA 目录里【全部】bundle 再扫一遍。**
    #   为什么加：上面的 `BUNDLES` 是**白名单**，而「白名单不全 ⇒ 假报『没有』」这个形状的错
    #   **已经犯过三次**（`Mobile/Particles/*` 漏过、`Tyranids/{Pulsating Mesh,Tyranid Tentacle}` 漏过、
    #   `Hidden/LUTBlender` 又漏 —— 它在 `scenes_scenes_battlearena*` 里）。
    #   与其每漏一个包补一次名单，不如让**「0 命中」自动触发一次全目录扫描** ——
    #   判据从「我记得的那几个包」变成「AA 目录里的所有包」。
    #   代价只在 0 命中时才付（正常查询第一个包就中了，一次也没多花）。
    if found == 0:
        import glob as _glob
        _seen = set(paths)
        rest = [p for p in sorted(_glob.glob(os.path.join(BUNDLE_DIR, "*.bundle"))) if p not in _seen]
        if rest:
            print("⚠️ 白名单 %d 个包 0 命中 ⇒ **再扫 AA 目录其余 %d 个包**（兜底）" % (len(paths), len(rest)))
            for p in rest:
                paths.append(p)
                scan(p)

    if outpath:
        io.open(outpath, "w", encoding="utf-8", newline="\n").write("\n".join(lines))
        print("\n已落盘：%s" % outpath)
    print("\n命中 %d 个 shader" % found)
    if found == 0:
        # 不许静默：把「搜过哪些包」打出来 —— 这条错犯过一次（只读了一个包就报「没有」）
        print("⚠️ 0 命中。搜过这 %d 个包：" % len(paths))
        for p in paths:
            print("     %s" % p)
    return 0


if __name__ == "__main__":
    sys.exit(main())
