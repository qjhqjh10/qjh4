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


def main():
    needle = sys.argv[1] if len(sys.argv) > 1 else "Distortion"
    grep = None
    if "--grep" in sys.argv:
        grep = sys.argv[sys.argv.index("--grep") + 1]

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
