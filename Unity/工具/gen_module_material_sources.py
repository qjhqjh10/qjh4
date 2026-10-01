# -*- coding: utf-8 -*-
"""gen_module_material_sources.py — 「材质名 → 它在哪个源包 / 容器 GUID」这张表。

**为什么需要它**（2026-10-01 探针实测，判据 → `资料/普查产出_1001/资产导入路三件_侦察.md` §①）：
`EffectExporter` 原来取材质只有一条路 —— `AssetBundle.LoadAsset<Material>(名字)`。
探针（`EffectExporter.ProbeModuleMaterials`）实测这条路**对这个 build 是坏的**：

    GetAllAssetNames() 吐的是**容器键**（这里就是 GUID），**不是资产名**
      ⇒ `LoadAsset<Material>("Card 3d Stealth")` = **null**
      ⇒ `LoadAsset<Material>("<容器 GUID>")` = **成功**
    `battlesharedresources` 的 `LoadAllAssets<Material>()` = 59 个，两张卡材质**都在里面**
    `Vanguard_Frame VAT Dissolve` **没有容器键**（非 addressable）—— 但它在**我们重打的**
    `wf_prefabs_extra.bundle` 里（`Vanguard Frame Animated VAT` 的依赖）

⇒ 所以要先知道**去哪找**：本脚本把这 3 张（以及将来任何 `module_materials.tsv` 里的材质）
逐张定位，写出 `数据/游戏数据/module_material_sources.tsv`：`材质名 \t 源包文件 \t 容器GUID(可空)`。

输入：`数据/游戏数据/module_materials.tsv`（由 `工具/gen_animfx_modules.py` 生成）
产出：`数据/游戏数据/module_material_sources.tsv`
用法：
  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_module_material_sources.py
"""
import io
import os
import sys

import UnityPy

sys.stdout.reconfigure(encoding="utf-8")

ROOT = r"d:/4/Unity"
IN_TSV = os.path.join(ROOT, "数据/游戏数据/module_materials.tsv")
OUT_TSV = os.path.join(ROOT, "数据/游戏数据/module_material_sources.tsv")

# 源包目录（原版随包）+ 我们自己重打的小包（非 addressable 的那批只在这里）
AA = r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64"
EXTRA = os.path.join(ROOT, "MyGame/Assets/StreamingAssets/WarpforgeVFX")


def read_wanted():
    if not os.path.exists(IN_TSV):
        print("没有 %s —— 先跑 工具/gen_animfx_modules.py" % IN_TSV)
        return []
    want = []
    for line in io.open(IN_TSV, encoding="utf-8"):
        line = line.rstrip("\n").rstrip("\r")
        if not line or line.startswith("#"):
            continue
        t = line.find("\t")
        if t <= 0:
            continue
        name = line[t + 1:].strip()
        if name and name not in want:
            want.append(name)
    return want


def main():
    want = read_wanted()
    if not want:
        return 1
    print("要定位的材质 %d 张：%s" % (len(want), ", ".join(want)))

    packs = []
    for f in sorted(os.listdir(AA)):
        if f.endswith(".bundle"):
            packs.append((f, os.path.join(AA, f), False))
    for f in sorted(os.listdir(EXTRA)) if os.path.isdir(EXTRA) else []:
        if f.endswith(".bundle"):
            packs.append((f, os.path.join(EXTRA, f), True))
    print("待扫包 %d 个（含重打的小包）" % len(packs))

    # 材质名 → 候选 [(源包文件, 是不是重打的小包, 容器 GUID 或 "")]
    cand = {}
    left = set(want)
    for fname, path, is_extra in packs:
        try:
            env = UnityPy.load(path)
        except Exception as e:
            print("  ! 读不动 %s: %s" % (fname, e))
            continue
        # ① 容器键 → PathID（本 build 的 `LoadAsset` 是按**键**找的）
        cont = {}
        for o in env.objects:
            if o.type.name != "AssetBundle":
                continue
            try:
                ab = o.read()
            except Exception:
                continue
            for k, v in ab.m_Container:
                try:
                    cont[v.asset.m_PathID] = k
                except Exception:
                    pass
        # ② 材质对象
        # ⚠️ **不能用「找到就移出待查集」那套** —— 同一张材质会在**多个包**里出现
        #    （原包 + 我们重打的小包），而「该去哪个」正是本脚本要判的（见下面挑法）。
        #    2026-10-01 第一版就是这么写错的：`Vanguard_Frame VAT Dissolve` 在源包里先被找到、
        #    于是重打小包那一份被跳过了，输出指向了原包 —— 而原包里它根本**加载不出来**。
        want_set = set(want)
        for o in env.objects:
            if o.type.name != "Material":
                continue
            try:
                m = o.read()
            except Exception:
                continue
            nm = getattr(m, "m_Name", "") or ""
            if nm not in want_set:
                continue
            cand.setdefault(nm, []).append((fname, is_extra, cont.get(o.path_id, "")))
            left.discard(nm)

    # 挑法（2026-10-01 探针实测的规则）：
    #   · **有容器键的优先** —— `LoadAsset<Material>(键)` 实测可用；
    #   · 都没键（非 addressable）⇒ **优先重打的小包** —— 那种材质在原包只是依赖、不是可加载根
    #     （实测 `battleprefabs` 的 `LoadAllAssets<Material>()` = **0**），只有在重打的小包里
    #     它才是真正的对象 ⇒ 靠 `LoadAllAssets<Material>()` 按名字捞。
    rows = {}
    for nm in want:
        cs = cand.get(nm) or []
        if not cs:
            continue
        withkey = [c for c in cs if c[2]]
        if withkey:
            rows[nm] = withkey[0]
        else:
            extra = [c for c in cs if c[1]]
            rows[nm] = extra[0] if extra else cs[0]
        kind = "容器键" if rows[nm][2] else ("重打小包·按名字捞" if rows[nm][1] else "源包·按名字捞")
        print("  ✓ %-40s → %s（%s）" % (nm, rows[nm][0], kind))
    for nm in sorted(left):
        print("  ✗ 没找到：%s" % nm)

    with io.open(OUT_TSV, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("# 材质名\t源包文件\t容器GUID  —— 由 工具/gen_module_material_sources.py 生成，别手改\n")
        fh.write("# 用途：`EffectExporter.FindMaterialInPacks` 按这张表**直接去对的包**取。\n")
        fh.write("# ⚠️ 本 build 的 `AssetBundle.LoadAsset<T>(名字)` 是坏的（`GetAllAssetNames()` 吐的是容器键）\n")
        fh.write("#    ⇒ **第 3 列非空**：按键取（`LoadAsset<Material>(键)`，实测可用）\n")
        fh.write("#    ⇒ **第 3 列为空**（非 addressable，如 `Vanguard_Frame VAT Dissolve`）：\n")
        fh.write("#       去第 2 列那个包里 `LoadAllAssets<Material>()` 按名字捞 —— 这就是为什么它指向\n")
        fh.write("#       `wf_prefabs_extra.bundle`（我们重打的小包）而不是原包：原包里它只是依赖、不是可加载根。\n")
        fh.write("# 判据 → 资料/普查产出_1001/资产导入路三件_侦察.md §①\n")
        for nm in want:
            if nm in rows:
                fh.write("%s\t%s\t%s\n" % (nm, rows[nm][0], rows[nm][2]))
    print("写出 %s（%d 条）" % (OUT_TSV, len(rows)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
