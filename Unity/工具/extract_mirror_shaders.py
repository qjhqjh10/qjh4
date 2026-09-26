#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""extract_mirror_shaders.py — 把**平面反射那两台 Blit shader**从 `globalgamemanagers.assets` 打进载体包

背景（判据全文 → `资料/战场13场_逐场对账_0920.md` §一 ①-j **结论八**）
------------------------------------------------------------------
战场地板用了 `Everguild/FX/Floor Planar Reflections *` 那一族，它们在**屏幕 UV** 上采
全局贴图 `_ReflectionMap`；原版这张贴图不是直接渲完就用，而是
`Mirror.ExecuteCommand` 里 **`SetGlobalTexture` → `CommandBuffer.Blit(…, material)` → 再 `SetGlobalTexture`**
—— 中间那两级 Blit 用的就是本脚本要抽的这两台 shader（名字从 `stringliteral.json` 按地址解出来的）：

  · `Hidden/Everguild/PlanarReflectionsBlit`            （`globalgamemanagers.assets` path_id **74**）
  · `Hidden/Everguild/GrainyBlurForPlanarReflections`   （path_id **98**）

我们原来只做了「双线性降采样 + 升采样当模糊」，**一级 Blit 都没有** ⇒ 反射比原版亮
（实测 `aeldari` 1.016 → **1.041**、`tauviorla` 1.028 → **1.032**，见结论八）。

为什么必须重打、不能直接用
--------------------------
1. 这两台 shader **只在 `globalgamemanagers.assets` 里** —— 那是个**裸 SerializedFile**，
   不是 AssetBundle，**运行时根本加载不了**（Unity 只能从 bundle 取资产）。
2. 逐个实读过我们所有的载体包（`wf_shaders` 45 / `wf_shaders_extra` 42 / `wf_builtin` 15 /
   `wf_arena_shaders` 7），**都没有这两台**。

做法（与 `extract_missing_shaders.py` 同一套路，只换了「源」与「怎么把对象搬进去」）
------------------------------------------------------------------------------
`extract_missing_shaders.py` 的源是**包**：它直接在包里删对象、改容器。这里源是**裸文件**，
对象得**搬**进载体 —— UnityPy 没给现成 API，逐字段搬会踩两个坑（都实测过）：

🔴 **坑 1：`ObjectReader.type_id` 是「源文件类型表里的下标」，搬过去必须改。**
   载体包里 `Shader` 的 `type_id = 24`、而源文件里不是 —— 不改的话写出来的对象头指到别的类型，
   **产物能加载、但那两个对象读不出来**（症状极具误导性：`LoadAllAssets<Shader>()` 里就是少两个名字）。
   ⇒ 从载体的类型表里查 `class_id == 48`（= Shader，**不是 83**）的那一项，取它的下标。
🔴 **坑 2：`path_id` 要自己发新号。** 载体的 id 是**负数 int64**（`big_id_enabled`），
   直接 `max+1` 会跟文件既有的编号风格不一致；实测取 `max+1` 可用（正数也在合法区间内），
   但**别去撞已有的号**。

产物
----
`Assets/StreamingAssets/WarpforgeVFX/wf_arena_mirror.bundle`
—— 名字**故意落在既有的通配 `wf_arena_*.bundle` 里**（`WarpforgeShaderLoader.ArenaBundlePattern`）
⇒ **加载器一行都不用改**。
⚠️ 与 `extract_missing_shaders.py --arenas` **互不覆盖**：那个工具按源包名出产物
（`wf_arena_2` / `wf_arena_tauviorla` …），不会产出 `wf_arena_mirror`。
**两个工具都要能各自重跑**（重跑 `--arenas` 不会带走这个包里的两台 shader）。

用法
----
  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" \\
    "d:/4/Unity/工具/extract_mirror_shaders.py" [--check]

  --check 只体检（读源、读载体、报能不能搬），不写文件。
"""
import os
import sys

import UnityPy
from UnityPy.classes import PPtr, AssetInfo

sys.stdout.reconfigure(encoding="utf-8")

# 源：原版**裸** SerializedFile（不是包）。用 `unity_run_ref` 那份真文件。
SRC = r"D:/2/unity_run_ref/Warpforge_Data/globalgamemanagers.assets"
# 载体**外壳**：拿已有小包当壳（要它那个 AssetBundle 对象 + 类型表）。
# ⚠️ 壳里的东西**全部会被丢掉**，只借结构 —— 所以借哪个包都行，这里借最小的那个。
SHELL = r"D:/4/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_arena_shaders.bundle"
DEST = r"D:/4/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_arena_mirror.bundle"

WANT = (
    "Hidden/Everguild/PlanarReflectionsBlit",
    "Hidden/Everguild/GrainyBlurForPlanarReflections",
)

SHADER_CLASS_ID = 48   # ClassIDType.Shader（⚠️ **不是 83**，实测）


def _name(d):
    try:
        return (d.m_ParsedForm.m_Name if d.m_ParsedForm else "") or ""
    except Exception:
        return ""


def _serialized_file(bf):
    return next(v for v in bf.files.values() if type(v).__name__ == "SerializedFile")


def main():
    check = "--check" in sys.argv

    src_env = UnityPy.load(SRC)
    src_sf = next(iter(src_env.files.values()))
    picked = {}
    for o in src_sf.objects.values():
        if o.type.name != "Shader":
            continue
        n = _name(o.read())
        if n in WANT:
            picked[n] = (o.path_id, len(bytes(o.read().compressedBlob)))
    print(f"[1] 源 {os.path.basename(SRC)}：要的两台找到 {len(picked)}/{len(WANT)}")
    for n in WANT:
        print(f"      {'✅' if n in picked else '❌'} {n}"
              + (f"  path_id={picked[n][0]}  blob={picked[n][1]} 字节" if n in picked else ""))
    if len(picked) != len(WANT):
        print("!! 源里没找齐 —— 中止（别猜着往下走）")
        return 1

    env = UnityPy.load(SHELL)
    bf = list(env.files.values())[0]
    sf = _serialized_file(bf)
    shader_tid = next(i for i, t in enumerate(sf.types) if t.class_id == SHADER_CLASS_ID)
    ab_reader = next(o for o in sf.objects.values() if o.type.name == "AssetBundle")
    print(f"[2] 壳 {os.path.basename(SHELL)}：类型表 {len(sf.types)} 项，"
          f"Shader 的 type_id = {shader_tid}，AssetBundle 对象在（pid={ab_reader.path_id}）")

    if check:
        print("[--check] 只体检，不写文件。")
        return 0

    # ---- 清空壳里的原有资产（只留 AssetBundle 对象）----
    dropped = 0
    for pid in [p for p, o in sf.objects.items() if o.type.name != "AssetBundle"]:
        del sf.objects[pid]
        dropped += 1
    print(f"[3] 清掉壳里的 {dropped} 个资产对象（只留 AssetBundle）")

    # ---- 搬对象（两个坑见文件头）----
    nxt = max([p for p in sf.objects.keys()] or [0]) + 1
    items = []
    for n in WANT:
        o = None
        for cand in src_sf.objects.values():
            if cand.type.name != "Shader":
                continue
            if _name(cand.read()) == n:
                o = cand
                break
        assert o is not None, n
        o.type_id = shader_tid          # 🔴 坑 1
        o.assets_file = sf
        o.path_id = nxt                 # 🔴 坑 2
        sf.objects[nxt] = o
        items.append((nxt, n))
        print(f"[4] 搬入 pid={nxt}  {n}")
        nxt += 1

    # ---- 重写容器 + 预加载表（两张表必须一起写：preloadIndex 是对 m_PreloadTable 的下标，
    #      只写容器不写预加载表会在 AddAssetsToPreload 里**直接段错误**，见
    #      `extract_missing_shaders.py` 的那段注释与 `资料/普查产出_0919/内置shader原件_加载崩溃_实测.md`）----
    ab = ab_reader.read()
    ab.m_PreloadTable = [PPtr(m_FileID=0, m_PathID=pid, assetsfile=sf) for pid, _ in items]
    ab.m_Container = [
        (n, AssetInfo(asset=PPtr(m_FileID=0, m_PathID=pid, assetsfile=sf),
                      preloadIndex=i, preloadSize=1))
        for i, (pid, n) in enumerate(items)
    ]
    if getattr(ab, "m_IsStreamedSceneAssetBundle", False):
        ab.m_IsStreamedSceneAssetBundle = False
    ab_reader.save_typetree(ab)
    print(f"[5] 容器 {len(ab.m_Container)} 条 / 预加载表 {len(ab.m_PreloadTable)} 条")

    # ---- 丢掉多余内层文件（判据：产物只剩 1 个内层文件）----
    sf_key = next(k for k, v in bf.files.items() if v is sf)
    for k in list(bf.files.keys()):
        if k != sf_key:
            del bf.files[k]
            print(f"[6] 丢弃内层文件 {k}")

    os.makedirs(os.path.dirname(DEST), exist_ok=True)
    data = bf.save(packer="original")
    with open(DEST, "wb") as f:
        f.write(data)
    print(f"[7] 已写出 {DEST}  ({len(data)/1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
