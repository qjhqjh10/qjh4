#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""extract_missing_shaders.py — 把运行时缺的原版 shader 从源 bundle 里抽出来，单独打一个小包

背景
----
工程里 `StreamingAssets/WarpforgeVFX/wf_shaders.bundle` 是 `shaders_assets_all.bundle`
的副本，只有 45 个 shader。但 958 个效果里还有一批用到了**不在那 45 个里**的
Everguild / ShaderGraph shader（影响 212 个效果），运行时解析不到就只能退回占位材质。

这些 shader 其实就在 `battleprefabs_vfxandmisc_assets_all.bundle`（74.6 MB / 64302 个对象）
里。整个包不能随游戏走，但只把 Shader 对象抽出来单独打一个小包是可以的
—— Shader 的 GPU 字节码在 `compressedBlob` 里，是内联的，不依赖别的资产。

⚠️ 两个必须注意的点（都是踩出来的）
--------------------------------
1. **代码里取不到这些 shader。** `battleprefabs` 里的 42 个 Shader 对象不在
   AssetBundle 的 m_Container 里 —— `LoadAllAssets()` 988 个结果里 0 个 Shader，
   `GetAllAssetNames()` 983 条里一条含 "shader" 的都没有。
   （`shaders_assets_all.bundle` 正相反，45 个 shader 全在容器里。）
   所以「让游戏直接加载原包」这条路是走不通的，只能抽出来重打。

2. **重打时必须保留 `AssetBundle` 对象，并重写它的 `m_Container`。**
   只留 Shader 对象、把 AssetBundle 对象一起删掉 → Unity 报
   "could not be loaded because it is not compatible with this newer version of the Unity
   runtime"（这句话极具误导性，跟版本毫无关系）。
   保留 AssetBundle 对象但不重写 m_Container（容器里还指着已删掉的 988 个对象）
   → 包能加载，但 shader 一个都暴露不出来。
   两者都做对 → 正常。

做法
----
1. 读 `wf_shaders.bundle`，得到「运行时已经有」的 shader 名（用于报告）
2. 扫 `导出报告.tsv` 的「原 shader」字段，得到「效果实际用到」的 shader 名（用于报告）
3. 打开源 bundle，只保留 Shader 对象 + AssetBundle 对象
4. 重写 AssetBundle 的 m_Container，每条指向一个保留的 shader
5. 丢掉 .resS / .resource 资源流，另存为 `wf_shaders_extra.bundle`
6. 运行时由 `WarpforgeShaderLoader` 一并加载两个包

用法
----
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/extract_missing_shaders.py"
  # 加 --check 只做体检、不写文件

⚠️ 抽出来的仍是**原版的编译字节码**，不是自建 shader。
   个人研究用没问题；要进发布版本必须换成自建替代（见交接文档的红线）。
"""
import argparse
import collections
import os
import sys

import UnityPy
from UnityPy.classes import AssetInfo
import UnityPy.classes as UClasses

PPtr = UClasses.PPtr

# ---- 路径 ----
BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
SRC_BUNDLE = os.path.join(BUNDLE_DIR, "battleprefabs_vfxandmisc_assets_all.bundle")
DEST_DIR = r"d:/4/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX"
EXISTING_BUNDLE = os.path.join(DEST_DIR, "wf_shaders.bundle")
DEST_BUNDLE = os.path.join(DEST_DIR, "wf_shaders_extra.bundle")
REPORT = r"d:/4/Unity/MyGame/Assets/WarpforgeVFX/导出报告.tsv"

# 工程自带 / 系统自带，Shader.Find 拿得到，不需要抽
BUILTIN_PREFIX = (
    "Universal Render Pipeline/", "Sprites/", "UI/", "TextMeshPro/",
    "WarpforgeVFX/", "Mobile/Particles/", "Particles/", "Legacy Shaders/", "Hidden/",
)


def shader_name(d):
    """bundle 里 Shader.m_Name 是空的，真名在 m_ParsedForm.m_Name。"""
    pf = getattr(d, "m_ParsedForm", None)
    return (getattr(pf, "m_Name", "") if pf is not None else "") or getattr(d, "m_Name", "") or ""


def collect_shader_names(bundle_path):
    env = UnityPy.load(bundle_path)
    out = {}
    for o in env.objects:
        if o.type.name != "Shader":
            continue
        try:
            d = o.read()
        except Exception:
            continue
        n = shader_name(d)
        if n:
            out.setdefault(n, o)
    return out


def used_shader_names(report_path):
    names = collections.Counter()
    with open(report_path, encoding="utf-8-sig") as f:
        for line in f:
            p = line.rstrip("\n").split("\t")
            if len(p) < 3:
                continue
            field = p[2].split("原 shader:")[-1].split("；")[0]
            for s in (x.strip() for x in field.split(",")):
                if s:
                    names[s] += 1
    return names


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只体检，不写文件")
    ap.add_argument("--out", default=DEST_BUNDLE)
    args = ap.parse_args()

    have = set(collect_shader_names(EXISTING_BUNDLE))
    print(f"[1] 运行时包 wf_shaders.bundle 已有 {len(have)} 个 shader")

    used = used_shader_names(REPORT)
    print(f"[2] 导出报告里出现过 {len(used)} 个 shader 名")

    cand = sorted(s for s in used if s not in have and not s.startswith(BUILTIN_PREFIX))
    print(f"[3] 运行时缺的候选 {len(cand)} 个，影响效果数合计 {sum(used[s] for s in cand)}")

    src = collect_shader_names(SRC_BUNDLE)
    print(f"[4] 源包 {os.path.basename(SRC_BUNDLE)} 里有 {len(src)} 个 shader")

    missing = [n for n in cand if n not in src]
    print(f"[5] 候选中源包没有的 {len(missing)} 个")
    for n in missing:
        print(f"      !! 源包无此 shader: {n}  (影响 {used[n]} 个效果)")

    if args.check:
        print("\n--check：只体检，未写文件")
        return 0

    # ---- 打开源包：只留 Shader + AssetBundle 对象 ----
    env = UnityPy.load(SRC_BUNDLE)
    bf = list(env.files.values())[0]
    sf = next(v for v in bf.files.values() if type(v).__name__ == "SerializedFile")

    kept, ab_reader, dropped = {}, None, 0
    for pid, o in list(sf.objects.items()):
        t = o.type.name
        if t == "AssetBundle":
            ab_reader = o
            continue
        if t == "Shader":
            try:
                n = shader_name(o.read())
            except Exception:
                n = ""
            if n:
                kept[pid] = n
                continue
        del sf.objects[pid]
        dropped += 1
    print(f"[6] 保留 {len(kept)} 个 shader + AssetBundle({ab_reader is not None})，丢弃 {dropped} 个对象")
    if ab_reader is None:
        print("!! 源包里没有 AssetBundle 对象 —— 打出来的包 Unity 会拒收，中止")
        return 1

    # ---- 重写 AssetBundle.m_Container ----
    # 不重写的话容器里还指着已删掉的 988 个对象，包能加载但 shader 一个都暴露不出来。
    ab = ab_reader.read()
    items = sorted(kept.items())
    ab.m_Container = [
        (n, AssetInfo(asset=PPtr(m_FileID=0, m_PathID=pid, assetsfile=sf),
                      preloadIndex=0, preloadSize=1))
        for pid, n in items
    ]
    ab_reader.save_typetree(ab)
    print(f"[7] m_Container 重写为 {len(ab.m_Container)} 条")

    # ---- 丢掉 .resS / .resource 资源流 ----
    # 这个包里 64000 多个对象（贴图/网格/粒子）的大块数据都堆在这两条流里，
    # 而我们只留了 Shader —— 它们的 compressedBlob 是内联的，用不到。
    # 不丢的话产物 45 MB（实测），丢了才是 1 MB 出头。
    gone = []
    for k in list(bf.files.keys()):
        if k.endswith(".resS") or k.endswith(".resource"):
            del bf.files[k]
            gone.append(k)
    print(f"[8] 丢弃资源流 {len(gone)} 条")

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    data = bf.save(packer="original")
    with open(args.out, "wb") as f:
        f.write(data)
    print(f"[9] 已写出 {args.out}  ({len(data)/1024:.0f} KB)")

    # kept 是 {path_id: 名字}，所以要比对值不是键
    kept_names = set(kept.values())
    need = [n for n in cand if n in kept_names]
    print(f"\n本次补上的 {len(need)} 个 shader（影响效果数）：")
    for n in sorted(need, key=lambda x: -used[x]):
        print(f"    {used[n]:5d}  {n}")
    extra = sorted(kept_names - set(cand))
    if extra:
        print(f"\n顺带捎上的 {len(extra)} 个（报告里没出现，留着备用，不影响解析优先级）：")
        for n in extra:
            print(f"           {n}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
