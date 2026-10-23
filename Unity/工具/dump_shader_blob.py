# -*- coding: utf-8 -*-
"""解开原版 shader 的编译字节码，扫出它到底绑了哪些纹理/常量。

**为什么需要它**：原版 shader 的 **HLSL 源码**（`Shader.m_Script`）确实被剥了 —— 但
**编译后的字节码在**（LZ4 压缩的 blob，解出来是 DXBC）。DXBC 的 RDEF 段里资源名是**明文**，
所以「这个 shader 到底采样哪张纹理」是能查证的，不用猜。

⚠️ 2026-09-12 更正：曾经写「源码和字节码都被剥掉了」——**那是我读错了属性**。
   UnityPy 的 `Shader.m_SubProgramBlob` 是 None，**真数据在 `Shader.compressedBlob`**
   （连同 `compressedLengths` / `decompressedLengths` / `offsets` / `stageCounts`）。
   靠这条错判，把一个本来能查清的问题（原版抓屏扭曲读哪张纹理）搁置了半天。

用法（必须 UTF-8）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
    d:/4/Unity/工具/dump_shader_blob.py "<shader 名片段>" [--grep 关键字]

例：
  dump_shader_blob.py "Particle Distortion"
      → 打印它绑定的所有纹理/常量名 + 是否含 _GrabPassTransparent / _CameraOpaqueTexture
"""
import io
import os
import re
import sys

import UnityPy

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法（必须 UTF-8）：\n  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe     d:/4/Unity/工具/dump_shader_blob.py "<shader 名片段>" [--grep 关键字]\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

sys.stdout.reconfigure(encoding="utf-8")

AA = r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64"
BUNDLES = [
    os.path.join(AA, "shaders_assets_all.bundle"),
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders.bundle",
    r"D:\4\Unity\MyGame\Assets\StreamingAssets\WarpforgeVFX\wf_shaders_extra.bundle",
]


def blob_of(shader):
    """把 Shader 的 compressedBlob 按平台/阶段切开逐块 LZ4 解压，拼成完整字节流。

    平台 4 = DX11(DXBC)，18 = 另一个变体；`compressedLengths` / `decompressedLengths`
    都是**二维**的（平台 → 阶段），别当成一维切。
    """
    raw = bytes(shader.compressedBlob)
    clens = shader.compressedLengths
    dlens = shader.decompressedLengths
    out = []
    off = 0
    for pi in range(len(clens)):
        c = clens[pi][0] if isinstance(clens[pi], (list, tuple)) else clens[pi]
        n = dlens[pi][0] if isinstance(dlens[pi], (list, tuple)) else dlens[pi]
        try:
            import lz4.block as lz4
            out.append(lz4.decompress(raw[off:off + c], uncompressed_size=n))
        except Exception as e:
            print(f"    [平台 {pi}] 解压失败：{e}")
        off += c
    return b"".join(out)


def names_in(data):
    """DXBC 的资源名/常量名是明文，直接扫 ASCII 串。"""
    ss = sorted(set(m.group(0).decode("ascii", "replace")
                    for m in re.finditer(rb"[ -~]{4,}", data)))
    return [s for s in ss if s.startswith("_") or "Texture" in s or "Camera" in s]


def grabpass_report(sh):
    """🆕 2026-10-22（W64 · 兜底审计 **B14**）：逐 pass 打 `m_GrabPass`。

    **为什么要有这个口**：B14 原来记着「原版是 Built-in GrabPass、每个物体画到那一刻抓一次」——
    那条**是错的**（铁律 5）。本函数让人**一行命令**就能复核：原版那个抓屏 shader 的每个 pass
    `m_GrabPass` 都是 `None`，真正灌 `_GrabPassTransparent` 的是原版自己的 URP Feature
    `GrabScreenFeature`（方法体 `d:/2/tools/decomp_full/GrabScreenFeature*.c`：
    GrabPass `renderPassEvent=500` 整帧一次 Blit + 紧跟一个 `RenderPass`（event **501**）重画抓屏层）。
    ⇒ 判据不靠注释、靠这个输出。
    """
    pf = getattr(sh, "m_ParsedForm", None)
    if pf is None:
        print("    （没有 m_ParsedForm）")
        return
    shaders = getattr(pf, "m_SubShaders", None) or []
    print(f"    SubShader {len(shaders)} 个：")
    for si, sub in enumerate(shaders):
        tags = getattr(sub, "m_Tags", None)
        taglist = getattr(tags, "tags", None)
        print(f"      [{si}] tags={taglist}")
        for pi, ps in enumerate(getattr(sub, "m_Passes", None) or []):
            g = getattr(ps, "m_GrabPass", None)
            gname = getattr(g, "m_GrabPassName", None) if g is not None else None
            print(f"        pass{pi} name={getattr(ps, 'm_Name', None)!r} "
                  f"m_GrabPass={gname!r} {'🔴 有 GrabPass' if gname else '（无）'}")


def main():
    needle = sys.argv[1] if len(sys.argv) > 1 else "Distortion"
    grep = None
    if "--grep" in sys.argv:
        grep = sys.argv[sys.argv.index("--grep") + 1]
    want_passes = "--passes" in sys.argv

    found = 0
    for path in BUNDLES:
        if not os.path.exists(path):
            continue
        env = UnityPy.load(path)
        for o in env.objects:
            if o.type.name != "Shader":
                continue
            try:
                sh = o.read()
            except Exception:
                continue
            pf = sh.m_ParsedForm
            name = (pf.m_Name if pf else "") or ""
            if needle.lower() not in name.lower():
                continue
            found += 1
            data = blob_of(sh)
            print(f"\n=== {name}   （{os.path.basename(path)}）")
            if want_passes:
                grabpass_report(sh)
            print(f"    字节码 {len(data)} 字节；魔数 {data[:4]!r}")
            ns = names_in(data)
            print(f"    纹理/常量名（{len(ns)}）:")
            for i in range(0, len(ns), 6):
                print("      " + "  ".join(ns[i:i + 6]))
            for probe in ("_GrabPassTransparent", "_CameraOpaqueTexture", "_CameraColorTexture",
                          "_CameraDepthTexture", "_RTHandleScale"):
                print(f"      {probe:26s} {'✅ 有' if probe.encode() in data else '—'}")
            if grep:
                print(f"    --grep {grep}: {'命中' if grep.encode() in data else '没有'}")
    if found == 0:
        print(f"没找到名字含 {needle!r} 的 shader（bundle 里 Shader 总数见下）")
    print(f"\n共命中 {found} 个")


if __name__ == "__main__":
    main()
