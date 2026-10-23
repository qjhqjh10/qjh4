# -*- coding: utf-8 -*-
"""dump_lutblender.py — 把 `Hidden/LUTBlender` 的**编译字节码**从**本地那份包**里解出来并反汇编。

## 为什么单开一个（而不直接用 `dump_shader_blob.py` / `disasm_dxbc.py`）

那两个工具的 `BUNDLES` 只扫 **原版 AA 目录**（`D:\\2\\...\\StandaloneWindows64`）和
`Assets/StreamingAssets/WarpforgeVFX/wf_shaders*.bundle`。而 `Hidden/LUTBlender`
**也在 `wf_arena_2.bundle` 里**（我们重打过的那份，2026-10-23 实读：
`Shader.path_id = 42`，`compressedBlob` 2027 B，含 `DXBC`）
—— 那三个名字都不在清单里 ⇒ 直接跑会「找不到」。

本脚本**只读**，不改任何东西。它复用 `disasm_dxbc.py` 的 `split_dxbc` / `_d3d_disassemble`
（Windows 自带 `d3dcompiler_47.dll`，不下载任何东西）。

## 用途（R1 的 A/B「离线臂」）

`WarpforgeVFX/LUTBlender` 是**自建**替代（`WarpforgeShaderMap.cs:289-292`：原版那份在
编辑器/批处理下渲品红）。算式是「反汇编读出来的」，本脚本就是**重跑那一步**：
把原件字节码重新反汇编，与 `WarpforgeVFX/Shaders/WFLUTBlender.shader` 的 HLSL 逐指令对照。
**不需要 Unity。**

## 用法（必须 UTF-8）

    PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
      d:/4/Unity/工具/dump_lutblender.py [--bundle <path>] [--all]

`--all` = 顺带把能找到的其它副本（13 个 `wf_arena_anims_*` / 场景包）也扫一遍并列出哪个包里有。
"""
import io
import os
import sys

import UnityPy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import disasm_dxbc as D          # 复用 _d3d_disassemble / split_dxbc

sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"d:/4/Unity"
DEFAULT_BUNDLE = os.path.join(ROOT, "MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_arena_2.bundle")
TARGET = "Hidden/LUTBlender"


def shader_named(env, name):
    for o in env.objects:
        if o.type.name != "Shader":
            continue
        try:
            d = o.read()
        except Exception:
            continue
        pf = getattr(d, "m_ParsedForm", None)
        if pf is not None and getattr(pf, "m_Name", None) == name:
            return o.path_id, d
    return None, None


def blob_of(shader):
    """`Shader.compressedBlob` → 解压后的完整字节流。
    ⚠️ `compressedBlob` 是**逐平台/逐阶段 LZ4 压缩**的（`compressedLengths` / `decompressedLengths`
       都是**二维**：平台 → 阶段）—— 直接在上面找 `DXBC` 魔数是找不到的（会得到一个假的偏移）。
       这段照抄 `工具/dump_shader_blob.py:blob_of`（同一套解压口径，别各写一份）。"""
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
            print("    [平台 %d] 解压失败：%s" % (pi, e))
        off += c
    return b"".join(out)


def dump(path):
    if not os.path.isfile(path):
        print("!! 没有这个包：%s" % path)
        return False
    env = UnityPy.load(path)
    pid, sh = shader_named(env, TARGET)
    if sh is None:
        print("   %-60s 没有 `%s`" % (os.path.basename(path), TARGET))
        return False
    print("== %s  ·  pathID=%s" % (os.path.basename(path), pid))
    blob = getattr(sh, "compressedBlob", None)
    if not blob:
        print("   compressedBlob 为空 —— 这个副本**没有编译字节码**（只有属性表/状态）")
        return True
    raw = bytes(blob)
    print("   compressedBlob = %d B（压缩）" % len(raw))
    try:
        raw = blob_of(sh)
    except Exception as e:
        print("   !! 解压失败：%r" % (e,))
        return True
    print("   解压后         = %d B · DXBC 段数（按魔数）= %d"
          % (len(raw), raw.count(b"DXBC")))
    try:
        chunks = D.split_dxbc(raw)
    except Exception as e:
        print("   !! split_dxbc 失败：%r" % (e,))
        return True
    if not chunks:
        print("   !! 解不出任何 DXBC 段")
        return True
    for i, ch in enumerate(chunks):
        data = ch[1] if isinstance(ch, tuple) and len(ch) > 1 else ch
        stage = ch[0] if isinstance(ch, tuple) and len(ch) > 0 else "?"
        print("\n   --- 段 %d (%s) · %d B ---" % (i, stage, len(data)))
        try:
            asm = D._d3d_disassemble(bytes(data))
        except Exception as e:
            print("   !! 反汇编失败：%r" % (e,))
            continue
        print(asm)
    return True


def main(argv):
    path = DEFAULT_BUNDLE
    for i, a in enumerate(argv):
        if a == "--bundle" and i + 1 < len(argv):
            path = argv[i + 1]
    ok = dump(path)
    if "--all" in argv:
        d = os.path.join(ROOT, "MyGame/Assets/StreamingAssets/WarpforgeVFX")
        print("\n== 扫本目录其余包（只看有没有、有没有字节码）==")
        for fn in sorted(os.listdir(d)):
            if not fn.endswith(".bundle"):
                continue
            p = os.path.join(d, fn)
            if os.path.abspath(p) == os.path.abspath(path):
                continue
            try:
                env = UnityPy.load(p)
            except Exception:
                continue
            pid, sh = shader_named(env, TARGET)
            if sh is None:
                continue
            blob = getattr(sh, "compressedBlob", None)
            print("   %-52s pathID=%-5s blob=%s B" % (fn, pid, len(blob) if blob else 0))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
