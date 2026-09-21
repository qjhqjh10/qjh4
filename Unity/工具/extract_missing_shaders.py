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
  # 加 --builtin 改打**内置管线包** `wf_builtin.bundle`（见下）

--builtin 模式（2026-09-19 加）
------------------------------
同样是「源包没有容器 ⇒ Unity 枚举不出来 ⇒ 必须重打」，只是换了个源包：
`Warpforge_unitybuiltinassets.bundle`（106 KB，**m_Container 也是 0 条**，实读）里有
**15 个 Unity 内置管线的老 shader** —— `Mobile/Particles/*` · `Legacy Shaders/Particles/*` ·
`Particles/Standard Unlit` · `UI/Default` · `Sprites/Default` 等。
这 8 个被我们自建近似顶了很久，理由「Built-in 老 shader 在 URP 工程里渲染不了」
**从没实测过**（`项目任务.md` ⛔ 行 ④）。原件打得出来 ⇒ 就不必再自建。
实测：`LoadAllAssets<Shader>()` 在**原始拷贝**上是 **0 个**（容器空），重打之后 15 个全在。
⚠️ 判据在 `BuiltinShaderProbe.Run`（挂上去渲一次，量 lit / 洋红占比）+ 白名单断言
`ShaderResolveProbe.Run`。

⚠️ 抽出来的仍是**原版的编译字节码**，不是自建 shader。
   ✅ **2026-09-19 更正**：这里原写「要进发布版本必须换成自建替代（见交接文档的红线）」——
   **那条红线已于 2026-09-18 由用户取消**（本项目是个人学习用途，原版美术/语音/文本/shader
   字节码一律照用）。⚠️ 若将来真要对外发布，这一点要重新评估。
"""
import argparse
import collections
import json
import os
import sys

import UnityPy
from UnityPy.classes import AssetInfo
import UnityPy.classes as UClasses

PPtr = UClasses.PPtr

# ---- 路径 ----
REPO = "d:/4"                       # 本工程仓库根（`工具/` 与 `Unity/` 都在这下面）
BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
SRC_BUNDLE = os.path.join(BUNDLE_DIR, "battleprefabs_vfxandmisc_assets_all.bundle")
DEST_DIR = r"d:/4/Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX"
EXISTING_BUNDLE = os.path.join(DEST_DIR, "wf_shaders.bundle")
DEST_BUNDLE = os.path.join(DEST_DIR, "wf_shaders_extra.bundle")
REPORT = r"d:/4/Unity/MyGame/Assets/WarpforgeVFX/导出报告.tsv"

# `--builtin` 模式的源/目标（2026-09-19 加）
BUILTIN_SRC = os.path.join(BUNDLE_DIR, "Warpforge_unitybuiltinassets.bundle")
DEST_BUILTIN = os.path.join(DEST_DIR, "wf_builtin.bundle")

# `--arenas` 模式的源/目标（2026-09-21 加）
# 🔴 为什么单开一个包：13 个**战场网格**的材质里，`Everguild/FX/Tyranids/Pulsating Mesh`（23 个）
#    与 `Everguild/FX/Tyranids/Tyranid Tentacle`（9 个）**只在这一个包里** ——
#    `wf_shaders.bundle`（= `shaders_assets_all.bundle` 的副本）里没有它们，而它是
#    `WarpforgeShaderLoader` 的主包 ⇒ 运行时取不到，那 32 个材质只能退回 `URP/Unlit`
#    （症状：利维坦的肉不搏动、触手是冻住的棍子）。
#    其余 5 族（`Unlit Wind` / `Unlit UV scroll` / `Unlit shadows receiver` /
#    `Floor Planar Reflections Grainny` / `FX/Vortex`）已经在主包里，不用重复抽。
ARENA_SRC = os.path.join(BUNDLE_DIR, "battlesharedresources_assets_all.bundle")
DEST_ARENA = os.path.join(DEST_DIR, "wf_arena_shaders.bundle")

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


def repack(src_path, out_path):
    """只留 Shader 对象 + AssetBundle 对象，重写容器，另存为一个小包。返回 {path_id: 名字}。

    🔴 **三步都做对才行**（文件头 ⚠️2）：① 只留 Shader + AssetBundle；
    ② **必须重写 `m_Container`** —— 留着不重写的话包能加载、但**一个资产都暴露不出来**
    （`LoadAllAssets<Shader>()` 返回 0；2026-09-19 在 `Warpforge_unitybuiltinassets.bundle`
    上又实测了一次，那次容器是 **0 条**）；③ 丢掉 `.resS`/`.resource` 流。
    """
    env = UnityPy.load(src_path)
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
        return None

    # ---- 重写 AssetBundle.m_Container ----
    # 不重写的话容器里还指着已删掉的那批对象，包能加载但 shader 一个都暴露不出来。
    #
    # 🔴 **`preloadIndex` 是「对 `m_PreloadTable` 的索引」—— 两张表必须一起写！**（2026-09-19 实测）
    #    容器写成 `preloadIndex=0 / preloadSize=1` 而 `m_PreloadTable` 是**空**的时候，
    #    Unity 会在 `AddAssetsToPreload`（`LoadAllAssets` 的预加载那一步）里**直接段错误** ——
    #    不是报错、是崩溃（栈：`LoadAssetWithSubAssets_Internal → ProcessAssetBundleEntries
    #    → PreparePreloadAssets → AddAssetsToPreload`）。
    #    **这个坑一直藏着**：上一个源包 `battleprefabs_vfxandmisc` 恰好带着一张 **86348 条**的
    #    预加载表，索引 0 落在界内 ⇒ 侥幸能跑；换成预加载表为空的内置包立刻现形。
    #    两种改法都实测可行：① 补齐预加载表 + 逐个索引（本脚本采用）② `preloadSize=0`。
    #    复现与全部变体见 `资料/普查产出_0919/内置shader原件_加载崩溃_实测.md`。
    ab = ab_reader.read()
    items = sorted(kept.items())
    ab.m_PreloadTable = [PPtr(m_FileID=0, m_PathID=pid, assetsfile=sf) for pid, _ in items]
    ab.m_Container = [
        (n, AssetInfo(asset=PPtr(m_FileID=0, m_PathID=pid, assetsfile=sf),
                      preloadIndex=i, preloadSize=1))
        for i, (pid, n) in enumerate(items)
    ]
    # 🔴 **2026-09-21 补：必须把「流式场景包」这个标志清掉。**
    #    从**战场场景包**（`scenes_scenes_battlearena*.bundle`）抽出来的产物会带着
    #    `m_IsStreamedSceneAssetBundle = true`，而 Unity 对这种包**拒绝 `LoadAllAssets`**：
    #    报 `This method cannot be used on a streamed scene AssetBundle.`
    #    ⇒ 包里明明有 shader 名字，运行时**一个都捞不到**（实测：arena2 与 tauviorla 的 10 处
    #    悄悄退回 `URP/Unlit`；症状是「陶的发电机不流动、tauviorla 的地板不反射、arena2 的水是死图」）。
    #    ⚠️ 这个异常原来被 `WarpforgeShaderLoader.LoadShadersFrom` 的 `catch { }` **吞掉了**
    #    （已改成报警）—— 「包里查得到、运行时取不到」先看这条。
    if getattr(ab, "m_IsStreamedSceneAssetBundle", False):
        ab.m_IsStreamedSceneAssetBundle = False
        print("[7b] 清掉 `m_IsStreamedSceneAssetBundle`（场景包带出来的，不清则 LoadAllAssets 抛异常）")
    ab_reader.save_typetree(ab)
    print(f"[7] m_Container 重写为 {len(ab.m_Container)} 条，m_PreloadTable 补齐 {len(ab.m_PreloadTable)} 条")

    # ---- 丢掉 .resS / .resource 资源流 ----
    # 大块数据（贴图/网格/粒子）都堆在这两条流里，而我们只留了 Shader
    # —— 它们的 compressedBlob 是内联的，用不到。
    # 不丢的话产物 45 MB（实测），丢了才是 1 MB 出头。
    #
    # 🔴 **2026-09-21 补：除了资源流，还要把「装着 shader 的那个 SerializedFile 之外的
    #    所有内层文件」一起丢掉。**
    #    战场场景包里有 **多个 CAB**（实测 `scenes_scenes_battlearena2.bundle` 有 4 个内层文件：
    #    2 个 CAB + 2 个资源流）。只清第一个 CAB 的话，**第二个 CAB 还在，并且引用着已被丢弃的
    #    资源流** ⇒ Unity 侧 `LoadAllAssets<Shader>()` **抛异常**，而 `WarpforgeShaderLoader`
    #    那边是 `catch { }` ⇒ **一个 shader 都捞不到、而且一声不响**（实测症状：包里有名字、
    #    运行时却报「取不到」，13 场里 arena2 与 tauviorla 的 10 处退回 URP/Unlit）。
    #    判据：产物 `bf.files` 应该**只剩 1 个**内层文件（能用的 `wf_arena_shaders.bundle` 就是 1 个）。
    sf_key = next(k for k, v in bf.files.items() if v is sf)
    gone = []
    for k in list(bf.files.keys()):
        if k == sf_key:
            continue
        kind = "资源流" if (k.endswith(".resS") or k.endswith(".resource")) else "多余的 CAB"
        del bf.files[k]
        gone.append((kind, k))
    print(f"[8] 丢弃内层文件 {len(gone)} 条（留 `{sf_key}`）："
          + ", ".join(f"{kind}" for kind, _ in gone))

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    data = bf.save(packer="original")
    with open(out_path, "wb") as f:
        f.write(data)
    print(f"[9] 已写出 {out_path}  ({len(data)/1024:.0f} KB)")
    return kept


def run_builtin(args):
    """`--builtin` 模式：把内置管线那 15 个 shader 重打成 `wf_builtin.bundle`。"""
    src = collect_shader_names(BUILTIN_SRC)
    print(f"[B1] 源包 {os.path.basename(BUILTIN_SRC)} 里有 {len(src)} 个 shader：")
    for n in sorted(src):
        print("      " + n)
    if args.check:
        print("\n--check：只体检，未写文件")
        return 0
    kept = repack(BUILTIN_SRC, args.out)
    if not kept:
        return 1
    print(f"\n[B2] 重打完 {len(kept)} 个 shader —— 逐个名字见上（[B1] 那份就是同一次实读）")
    return 0


def run_arenas(args):
    """`--arenas` 模式：把**战场网格实际用到、而运行时包里没有**的原版 shader 抽出来（2026-09-21 加）。

    🔴 **为什么要数据驱动、不能写死一个源包**：这些 shader 散在**好几个**包里 ——
    实测 `Tyranids/Pulsating Mesh` / `Tyranid Tentacle` 在 `battlesharedresources_assets_all`，
    而 `Tau Generator Energy` / `Floor Planar Reflections … Vertex color shadow mask` 在
    **`scenes_scenes_battlearenatauviorla`**、`Simple Fake Water` 在 **`scenes_scenes_battlearena2`**
    —— 都是**战场自己的场景包**。只认一个源包就会漏（初版就是这么漏掉 10 处的）。

    做法：① 读 13 场的清单，收集 `meshes` 里出现的所有 `shader` 名；② 减掉运行时三个包里已有的；
    ③ 在「13 个战场场景包 + battlesharedresources」里找这些缺失的名字落在哪个包；
    ④ **每个有份的源包打一个包**（`repack` 是「留全部 shader」，所以一个源包一个产物）。
    Unity 侧按 `wf_arena_*.bundle` 通配加载（见 `WarpforgeShaderLoader`）。
    """
    import glob as _glob
    arena_dirs = os.path.join(REPO, "Unity/MyGame/Assets/WarpforgeArena1/arenas")
    # ① 清单里实际用到的 shader 名
    used = collections.Counter()
    for mf_path in _glob.glob(os.path.join(arena_dirs, "*", "*_manifest.json")):
        try:
            with open(mf_path, encoding="utf-8-sig") as f:
                mf = json.load(f)
        except Exception as e:
            print(f"      !! 清单读不了 {os.path.basename(mf_path)}: {e}")
            continue
        for m in (mf.get("meshes") or []):
            for s in [m] + list(m.get("subMats") or []):
                n = (s or {}).get("shader")
                if n:
                    used[n] += 1
    print(f"[A1] 13 场清单里用到的原版 shader：{len(used)} 个名字")

    # ② 运行时已有的
    have = set()
    for name in ("wf_shaders.bundle", "wf_shaders_extra.bundle", "wf_builtin.bundle",
                 "wf_arena_shaders.bundle"):
        p = os.path.join(DEST_DIR, name)
        if os.path.isfile(p):
            have |= set(collect_shader_names(p))
    miss = sorted(n for n in used if n not in have)
    print(f"[A2] 运行时还缺的 {len(miss)} 个（影响 {sum(used[n] for n in miss)} 个材质槽）：")
    for n in miss:
        print(f"      {used[n]:4d}  {n}")
    if not miss:
        print("      （一个都不缺）")
        return 0

    # ③ 找它们落在哪个包
    srcs = [_glob.glob(os.path.join(BUNDLE_DIR, "scenes_scenes_battlearena*.bundle"))
            + [ARENA_SRC]]
    srcs = sorted(set(sum(srcs, [])))
    where = {}          # 源包 → 它里面有份的名字
    for p in srcs:
        names = set(collect_shader_names(p))
        hit = [n for n in miss if n in names]
        if hit:
            where[p] = sorted(hit)
    for p, hit in where.items():
        print(f"[A3] {os.path.basename(p)} 里有 {len(hit)} 个：{hit}")

    still = [n for n in miss if not any(n in v for v in where.values())]
    if still:
        print(f"[A4] 🔴 **没有任何源包里有** {len(still)} 个：{still}（这些要继续掉兜底）")

    if args.check:
        print("\n--check：只体检，未写文件")
        return 0

    # ④ 一个有份的源包打一个产物
    for p, hit in where.items():
        base = os.path.basename(p)
        if base == os.path.basename(ARENA_SRC):
            slug = "shared"
        else:
            slug = base.replace("scenes_scenes_battlearena", "").replace(".bundle", "")
        out = os.path.join(DEST_DIR, f"wf_arena_{slug}.bundle")
        print(f"\n[A5] {base} → {os.path.basename(out)}")
        if not repack(p, out):
            return 1
    print("\n[A6] 完成。Unity 侧按 `wf_arena_*.bundle` 通配加载。")
    return 0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只体检，不写文件")
    ap.add_argument("--builtin", action="store_true",
                    help="改打内置管线包：源 Warpforge_unitybuiltinassets.bundle → 目标 wf_builtin.bundle")
    ap.add_argument("--arenas", action="store_true",
                    help="改打**战场 shader 包**：源 battlesharedresources_assets_all.bundle → 目标 wf_arena_shaders.bundle")
    ap.add_argument("--out", default=None,
                    help="默认 extra 模式写 wf_shaders_extra.bundle；--builtin 模式写 wf_builtin.bundle")
    args = ap.parse_args()
    if args.out is None:
        args.out = (DEST_ARENA if args.arenas
                    else (DEST_BUILTIN if args.builtin else DEST_BUNDLE))

    if args.arenas:
        return run_arenas(args)
    if args.builtin:
        return run_builtin(args)

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

    kept = repack(SRC_BUNDLE, args.out)
    if not kept:
        return 1

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
