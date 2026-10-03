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

--prefabs 模式（2026-10-01 加）
------------------------------
同样是「源包里的对象 Unity 枚举不出来 ⇒ 必须重打」，这次搬的是 **prefab**：
`Card 3D Death Explosion`（阵亡爆散体）与 `Vanguard Frame Animated VAT`（`vanguardFrame` 状态框）
**在包里、但不是 addressable**（被卡预制体字段引用）⇒
`GetAllAssetNames()`（983 条）与 `LoadAllAssets<GameObject>()`（965 个）**两条都不含**。

与前三个模式的两处不同：
1. **要连整棵依赖树一起搬**（材质/网格/贴图/控制器）—— 只搬根 GameObject 的话，
   导出侧看到的是「材质是空的」，很难归因；
2. **资源流/同包 CAB 可能不能丢** —— 贴图/网格的大数据可能走 `.resS` 流式存储。
   `repack_tree` 会自己判并把要留的内层文件交给 `_write_bundle(keep_extra_files=…)`。
3. **内层 CAB 要改名、资源流要瘦身**（2026-10-01 实测的两条）：
   · **不改名 Unity 直接拒收** —— 沿用源包 CAB 名时日志是
     `another AssetBundle with the same files is already loaded`，包根本没加载上；
   · `.resS` 是**整个 CAB 的公共流**（这件实测 **201.6 MB**），而我们用到的那 6 张纹理
     只占 **219 KB** ⇒ 按区间切片 + 每段 16 字节对齐 + 重算 `offset`，产物 **45 MB → 459 KB**。

用法：
  python 工具/extract_missing_shaders.py --prefabs            # 打 wf_prefabs_extra.bundle + 卡包那两包
  python 工具/extract_missing_shaders.py --prefabs --check    # 只体检（找根 + 走树 + 报告），不写文件
Unity 侧接着跑 `EffectExporter.RunListed`（它现在**同时扫源包目录与 StreamingAssets/WarpforgeVFX/**），
再跟一次 `EffectLibraryBuilder`。判据 → `资料/待办判据_战场与战斗视图.md` 末节第 9 条 · `资料/已知的坑.md` 同名那条。

🆕 2026-10-03：`--prefabs` **同时**打下面三包（见 `BOOSTER_GROUPS` 的注释）——
  · `wf_prefabs_extra.bundle` —— 战场那两件（原有行为，一字未改）
  · `wf_menus_extra.bundle`    —— `Booster Pack Open Window` / `Booster Info Popup` +
                                 **4 个开卡包粒子 prefab**（`Boosterpack Open Card Rarity 1..4`）+ 8 条开卡包 clip
  · `wf_boosters_extra.bundle` —— **卡包外观 prefab**（`Booster Pack Standard` + 15 阵营/扩展变体 +
                                 `Booster Pack All Armies Variant`）+ 2 条 clip（备着，我们的窗口还没用）
Unity 侧接 `BoosterPackExporter.Run`（→ `Assets/CardPresentation/Effects/`）再跟一次 `EffectLibraryBuilder`。
判据 → `资料/阶段二_商店_原版规格.md` §五·三。
🔴 **2026-10-03 实测更正**：那 4 个粒子 prefab **在 `menus_assets_all`、不在 `boosterpacks_assets_all`**
   —— 按包名猜归属会错（第一次实跑四连红）。逐条实据见 `BOOSTER_GROUPS` 上面的注释。
⚠️ 三件的 **CAB 基名必须互不相同**（Unity 按内层名认「同一个包」，撞名会被拒收）—— 已各自指定。

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

# 🔴 **2026-10-03 补**（同 `import_original_art.py` 顶上那一句）：**stdout 必须是 UTF-8**。
#    本脚本从 `[P3] 🔴 **真外部引用**` 那一行起会打 emoji，而 Windows 上 `python x.py > log.txt`
#    时 Python 按 **GBK** 开 stdout ⇒ 撞到 emoji 就 `UnicodeEncodeError` **整脚本崩掉**
#    （实测：`--prefabs --check > /tmp/log` 死在 `repack_tree` 的 `[P3] 真外部引用` 那行）。
#    在真终端里跑不出这个错（终端是 UTF-8）⇒ 这正是「换个重定向方式就莫名其妙崩」的那类坑。
try:
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
except Exception:
    pass

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

# `--prefabs` 模式的源/目标（2026-10-01 加）
# 两件「原版真在用、但不是效果根、又不在 addressables 容器里」的 prefab
# （判据 → `资料/已知的坑.md` 的「GetAllAssetNames() 只吐容器里的资产」那条）：
#   · `Card 3D Death Explosion` —— 阵亡爆散体（挂 `CardScript.cardDestroyFX`）
#   · `Vanguard Frame Animated VAT` —— `vanguardFrame` 那个状态框
PREFAB_TARGETS = ["Card 3D Death Explosion", "Vanguard Frame Animated VAT"]
# 🆕 2026-10-01：**只被 JSON 数据引用的材质**（没有任何 Unity 对象引用它们 ⇒ 依赖树走不到、
#   容器里也没有）⇒ 必须当**根**收进来 + 登记容器项，否则 Unity 侧一条路都取不到。
#   判据 → `资料/普查产出_1001/资产导入路三件_侦察.md` §①（含探针 `EffectExporter.ProbeModuleMaterials`）。
#   ⚠️ 另两张卡材质（`Card 3d Dissolve Blend Image Ambush` / `Card 3d Stealth`）**在容器里**，
#     按容器键就能取，不用重打 —— 见 `数据/游戏数据/module_material_sources.tsv`。
MATERIAL_TARGETS = ["Vanguard_Frame VAT Dissolve"]
DEST_PREFABS = os.path.join(DEST_DIR, "wf_prefabs_extra.bundle")

# 🆕 2026-10-03：**卡包那两扇窗**（`项目任务.md` §三 第 29 条 **A7**）走同一条重打路。
# 判据 → `资料/阶段二_商店_原版规格.md` **§五·三**「`Booster Pack Open Window`」那一节。
#
# 🔴 **2026-10-03 实测更正（源包归属原来记错了，第一次实跑 `[P1] 这个包里没有 GameObject` 四连红）**：
#   那 4 个粒子 prefab **不在** `boosterpacks_assets_all`，**在 `menus_assets_all`**。
#   实据（只读普查，`UnityPy` 扫两个包的全部 `GameObject` 的 `m_Name` + `Transform.m_Father`）：
#     · `menus_assets_all.bundle`（16768 个 GameObject / **313 个根**）里 6 件**全中、且都是根**：
#         `Booster Pack Open Window`(Transform 1342405282582379555) · `Booster Info Popup`(5823516886071648943)
#         `Boosterpack Open Card Rarity 1`(4179873410099752137) · `2`(6344549431542765525)
#         `3`(-216069053023463151) · `4`(-5965722982508144977)
#     · `boosterpacks_assets_all.bundle`（367 个 GameObject / **17 个根**）里 **0 个** `Boosterpack Open Card Rarity *`
#       —— 它的 17 个根是**卡包外观 prefab**：`Booster Pack Standard` + 15 个阵营/扩展变体
#       + `Booster Pack All Armies Variant`（另有 2 条 clip `Booster Open Standard` / `Booster Open Sororitas`）。
#     ⇒ **看包名猜归属 = 会错**（`boosterpacks` 这个名字太像了）；判据只能是**打开包按 `m_Name` 找**。
#
# ⚠️ 两件的 **CAB 基名必须不同**（Unity 按内层名认「同一个包」，同名会被拒收）。
BOOSTER_SRC = os.path.join(BUNDLE_DIR, "boosterpacks_assets_all.bundle")
MENUS_SRC = os.path.join(BUNDLE_DIR, "menus_assets_all.bundle")
BOOSTER_GROUPS = [
    # ① `menus`：两扇窗 + 4 个粒子 prefab + 8 条 clip（**本该 4 件粒子就在这里**）
    dict(
        src=MENUS_SRC,
        out=os.path.join(DEST_DIR, "wf_menus_extra.bundle"),
        cab="CAB-wfmenusextra",
        roots=["Booster Pack Open Window", "Booster Info Popup",
               "Boosterpack Open Card Rarity 1", "Boosterpack Open Card Rarity 2",
               "Boosterpack Open Card Rarity 3", "Boosterpack Open Card Rarity 4"],
        # 名字出自 `Booster Pack Open Window` 的 MB 字段（windowAnimation / backgroundAnimation /
        # CardInBoosterPack.cardAnimation）+ §五·三 点名的 `OpenCardbacks`；逐条判据见
        # `Assets/WarpforgeArena1/Editor/BoosterPackExporter.cs` 的 `ClipNames`。
        # 实测：`menus_assets_all` 一共 12 条 clip，这 8 条**全库唯一、无重名**。
        clips=["Booster Window Open", "Booster Window Close",
               "Booster Window - Background Shake On Open",
               "Booster Opening - Card Idle", "Booster Opening - Card Open Normal",
               "Booster Opening - Card Open Rare", "Booster Opening - Card Open Legendary",
               "OpenCardbacks"],
    ),
    # ② `boosterpacks`：**卡包外观 prefab**（原版 `BoosterPackOpenWindow.Initialize` 会把 SO 的
    #    `visualPrefab` 实例化到 `boosterPackAnchor` 上 —— 那一件就是这里面的一个）。
    #    ⚠️ **我们的窗口目前不用它们**（`Shell/BoosterPackOpenWindow.cs` 画的是整屏纯色背景），
    #       导出来是为了「完全复刻」那一趟有原件可用（铁律 11：先记录、不因复杂而回避）。
    dict(
        src=BOOSTER_SRC,
        out=os.path.join(DEST_DIR, "wf_boosters_extra.bundle"),
        cab="CAB-wfboostersextra",
        roots=["Booster Pack All Armies Variant",
               "Booster Pack Standard",
               "Booster Pack Standard Aeldari SaimHann",
               "Booster Pack Standard Astra Militarum",
               "Booster Pack Standard Chaos SM Black Legion",
               "Booster Pack Standard Emperors Children",
               "Booster Pack Standard Genestealers",
               "Booster Pack Standard Necrons Sautekh",
               "Booster Pack Standard Orks Goff",
               "Booster Pack Standard Orks Goff Expansion",
               "Booster Pack Standard Sororitas",
               "Booster Pack Standard Space Marines Dark Angels",
               "Booster Pack Standard Space Marines Space Wolves",
               "Booster Pack Standard Space Marines Ultramarines",
               "Booster Pack Standard Space Marines Ultramarines Expansion 1",
               "Booster Pack Standard TauEmpire",
               "Booster Pack Standard Tyranids Leviathan"],
        clips=["Booster Open Standard", "Booster Open Sororitas"],
    ),
]

# 🆕 2026-10-01 晚：**动画片段**（`AnimationClip`）也走同一条路。
# 为什么：片段是**控制器的依赖**、不是资产根 ⇒ `LoadAllAssets<AnimationClip>()` 看不到它
#   （那个 API 只按**容器/预加载表**走，判据 → `资料/已知的坑.md`）
#   ⇒ 症状是 `EffectExporter` 出声「已加载的包里没有名为 `Card Explosion` 的 AnimationClip」，
#     两个爆散体的控制器建出来了、**动作却是空的**（跟没导一样）。
# ⚠️ **不手写名字**：从 `数据/游戏数据/animator_controllers.json` 里取
#   「源包 = 本包」的那些控制器要用的 clip（那张表由 `工具/gen_animator_controllers.py` 生成）。
CTRL_TABLE = r"d:/4/Unity/数据/游戏数据/animator_controllers.json"


def controllers_of(bundle_dir_name):
    """`animator_controllers.json` 里「源包 = bundle_dir_name」的控制器整条。"""
    try:
        with open(CTRL_TABLE, encoding="utf-8") as f:
            t = json.load(f)
    except Exception as e:
        print(f"[P0] ⚠️ 读不了控制器数据表 `{CTRL_TABLE}`（{e}）⇒ 控制器/动画片段那批根收不到")
        return []
    return [c for c in t.get("controllers", []) if c.get("source") == bundle_dir_name]


def controller_clip_names(bundle_dir_name):
    """那批控制器引用到的全部 clip 名。"""
    out = []
    for c in controllers_of(bundle_dir_name):
        for n in c.get("clips", []):
            if n and n not in out:
                out.append(n)
    return out


def controller_names(bundle_dir_name):
    """那批控制器自己的名字 —— 也要当**根**登记，运行时才 `LoadAsset<RuntimeAnimatorController>(名字)` 取得到。
    🔴 为什么运行时要取原件而不是用我们建的工程 `.controller`：见 `WarpforgeAnimatorBridge.cs` 头部
      （工程里那份的**曲线是空的** —— muscle 格式落不了盘）。"""
    return [c["name"] for c in controllers_of(bundle_dir_name) if c.get("name")]


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
    items = sorted(kept.items())
    _write_bundle(bf, sf, ab_reader, {pid: [n] for pid, n in items}, out_path)
    return kept


def _write_bundle(bf, sf, ab_reader, entries, out_path, keep_extra_files=()):
    """**重打包的公共尾段**（`repack` 与 `repack_tree` 共用；2026-10-01 从 `repack` 抽出来）。

    `entries` = `{path_id: [容器名, …]}` —— **一个资产可以登记多个名字**（别名），
    这样 Unity 侧 `LoadAsset(name)` 无论用裸名还是小写路径写法都能命中。
    `keep_extra_files` = 除主 SerializedFile 之外**必须保留**的内层文件（`repack_tree` 用：
    依赖树落同包另一个 CAB 时，丢掉它 = 引用断掉）。

    ⚠️ **三条都是实测踩出来的，缺一条就静默失败**（原始判据全部保留在下面注释里）。
    """
    ab = ab_reader.read()
    ab.m_PreloadTable = [PPtr(m_FileID=0, m_PathID=pid, assetsfile=sf) for pid in entries]
    ab.m_Container = [
        (alias, AssetInfo(asset=PPtr(m_FileID=0, m_PathID=pid, assetsfile=sf),
                          preloadIndex=i, preloadSize=1))
        for i, (pid, aliases) in enumerate(entries.items())
        for alias in aliases
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
    # ⚠️ **这条对 shader 成立、对 prefab 不一定**：shader 的大块数据是**内联**的
    #    （`compressedBlob`）⇒ 流用不到；不丢的话产物 45 MB（实测），丢了才是 1 MB 出头。
    #    但**贴图/网格可能真的走流式**（`m_StreamData.path` 非空）⇒ 那种必须由调用方
    #    通过 `keep_extra_files` 保留（`repack_tree` 会自己判，见它 [P3] 那几行）。
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
    gone, kept_extra = [], []
    for k in list(bf.files.keys()):
        if k == sf_key:
            continue
        if k in keep_extra_files:          # `repack_tree`：依赖树落在同包另一个 CAB 里
            kept_extra.append(k)
            continue
        kind = "资源流" if (k.endswith(".resS") or k.endswith(".resource")) else "多余的 CAB"
        del bf.files[k]
        gone.append((kind, k))
    print(f"[8] 丢弃内层文件 {len(gone)} 条（留 `{sf_key}`）："
          + ", ".join(f"{kind}" for kind, _ in gone)
          + (f"；**按需保留** {len(kept_extra)} 条：{kept_extra}" if kept_extra else ""))

    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    data = bf.save(packer="original")
    with open(out_path, "wb") as f:
        f.write(data)
    print(f"[9] 已写出 {out_path}  ({len(data)/1024:.0f} KB)")
    return entries


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


def _field(d, name, default=None):
    """`read()` 出来的对象与 `read_typetree()` 出来的字典都可以用同一种写法取值。"""
    if isinstance(d, dict):
        return d.get(name, default)
    return getattr(d, name, default)


def _ptr_ids(v):
    """把一个 PPtr 归一成 `(m_FileID, m_PathID)`（字典形态与对象形态都认）。"""
    if isinstance(v, dict):
        return v.get("m_FileID", 0), v.get("m_PathID", 0)
    return getattr(v, "m_FileID", 0), getattr(v, "m_PathID", 0)


def _collect_refs(v, out, depth=0):
    """把任意 `read()` / `read_typetree()` 结果里的 PPtr 全收出来（递归字典/列表/对象）。

    ⚠️ 只跟着**结构**走，不 `read()` 子对象 —— 否则一棵 prefab 树会把整包都读进内存。
    """
    if depth > 24:
        return
    if isinstance(v, PPtr):
        out.append(v)
        return
    if isinstance(v, dict):
        if "m_PathID" in v and "m_FileID" in v:
            out.append(v)
            return
        for x in v.values():
            _collect_refs(x, out, depth + 1)
        return
    if isinstance(v, (list, tuple, set)):
        for x in v:
            _collect_refs(x, out, depth + 1)
        return
    d = getattr(v, "__dict__", None)
    if isinstance(d, dict):
        for x in d.values():
            _collect_refs(x, out, depth + 1)


def _ext_name(sf, fid):
    """`m_FileID`（非 0）→ 外部文件名（同包内层 CAB / 真外部包都能认）。"""
    try:
        e = sf.externals[fid - 1]
    except Exception:
        return f"<fid {fid}>"
    p = getattr(e, "path", None)
    if p is None and isinstance(e, (list, tuple)) and len(e) >= 2:
        p = e[1]
    if p is None and isinstance(e, dict):
        p = e.get("path")
    return str(p)


def repack_tree(src_path, out_path, names, dry_run=False, extra_materials=(), extra_clips=(),
                extra_controllers=(), cab_name=None):
    """把 `names` 这几件 GameObject **连同整棵内部依赖树**重打成一个小包。

    🔴 **为什么要连依赖树一起**：prefab 被 Unity 实例化时会去解析材质 / 网格 / 贴图 / 控制器引用，
    少一件就表现成「某个槽是 null」（在导出侧只看到「材质是空的」，很难归因）。
    做法 = 从根 GameObject 出发，递归收所有 `m_FileID == 0` 的引用。

    🔴 **两个必须判的东西**（判错就是**静默**坏资产）：
    1. **同包内层 CAB**：引用落在同 bundle 的另一个 CAB 里时，那个 CAB **不能丢**
       ——`_write_bundle` 默认「只留一个内层文件」，这里按需追加 `keep_extra_files`。
    2. **资源流（`.resS` / `.resource`）**：贴图/网格的大块数据可能走流式存储
       （`m_StreamData.path` 非空）⇒ 丢了流就是「有对象、没数据」。这里把它们列出来并保留。
    """
    env = UnityPy.load(src_path)
    bf = list(env.files.values())[0]
    sf = next(v for v in bf.files.values() if type(v).__name__ == "SerializedFile")

    # ---- ① 按名字找根 GameObject ----
    found = {}
    for o in env.objects:
        if o.type.name != "GameObject":
            continue
        try:
            d = o.read()
        except Exception:
            continue
        n = _field(d, "m_Name")
        if n in names and n not in found:
            found[n] = o
    for n in names:
        if n not in found:
            print(f"[P1] 🔴 这个包里**没有** GameObject `{n}`")
    print(f"[P1] 找到 {len(found)}/{len(names)} 个根："
          + " · ".join(f"{n}(PathID {o.path_id})" for n, o in found.items()))

    # ---- ①·b 🆕 2026-10-01：按名字找**只被数据引用的 Material** ----
    # 为什么需要：`Vanguard_Frame VAT Dissolve` 这类材质**没有任何 Unity 对象引用它**
    #   （引用它的是 AnimFX 的**模块字段**，那活在 JSON 里、不在包里）⇒ 从任何 GameObject
    #   出发的依赖树都走不到它；它又不在容器里 ⇒ Unity 侧三条路（`GetAllAssetNames` /
    #   `LoadAllAssets` / `LoadAsset(名字)`）**一条都拿不到**（探针实测，见
    #   `资料/普查产出_1001/资产导入路三件_侦察.md` §①）。
    #   ⇒ 把它当**根**收进来 + 登记一条容器项，`LoadAsset<Material>(名字)` 才有得取。
    #
    # ---- ①·c 🆕 2026-10-01 晚：**动画片段**（`extra_clips`）走同一套 ----
    # 为什么：`AnimationClip` 是**控制器的依赖**、不是根 ⇒ `LoadAllAssets<AnimationClip>()`
    #   **看不到它**（那个 API 只按**容器/预加载表**走，判据 → `资料/已知的坑.md`）
    #   ⇒ `EffectExporter` 会出声「已加载的包里没有名为 `Card Explosion` 的 AnimationClip」。
    #   修法同材质：当根收进来 + 登记容器项。
    for want_names, type_name in ((extra_materials, "Material"), (extra_clips, "AnimationClip"),
                                  (extra_controllers, "AnimatorController")):
        if not want_names:
            continue
        want_set = set(want_names)
        hits = {}
        for o in env.objects:
            if o.type.name != type_name:
                continue
            try:
                d = o.read()
            except Exception:
                continue
            n = _field(d, "m_Name")
            if n in want_set and n not in hits:
                hits[n] = o
        for n in want_names:
            if n not in hits:
                print(f"[P1] 🔴 这个包里**没有** {type_name} `{n}`")
        print(f"[P1] 找到 {len(hits)}/{len(want_names)} 个 {type_name} 根："
              + " · ".join(f"{n}(PathID {o.path_id})" for n, o in hits.items()))
        found.update(hits)          # 并进 found ⇒ 一起走依赖树、一起登记容器项

    if not found:
        return None

    # ---- ② 递归收内部依赖树 ----
    kept, by_type, ext = {}, collections.Counter(), collections.Counter()
    streamed, seen, queue = [], set(), [o.path_id for o in found.values()]
    while queue:
        pid = queue.pop()
        if pid in seen:
            continue
        seen.add(pid)
        o = sf.objects.get(pid)
        if o is None:
            print(f"[P2] ⚠️ PathID {pid} 不在这个 SerializedFile 里（引用断了）")
            continue
        kept[pid] = o
        by_type[o.type.name] += 1
        try:
            d = o.read_typetree()
        except Exception:
            try:
                d = o.read()
            except Exception:
                continue
        sd = _field(d, "m_StreamData")
        p = _field(sd, "path") if sd is not None else None
        if p:
            streamed.append((o.type.name, _field(d, "m_Name", ""), p))
        refs = []
        _collect_refs(d, refs)
        for r in refs:
            fid, rpid = _ptr_ids(r)
            if not rpid:
                continue
            if fid == 0:
                queue.append(rpid)
            else:
                ext[(fid, o.type.name)] += 1

    # ---- ③ 外部引用：同包内层 CAB 要保留，真外部只报告 ----
    #  ⚠️ `bf.files` 的键可能是 `CAB-xxx` / `CAB-xxx.resS` / 也可能是带 `archive:/…` 的整串
    #     ⇒ **全路径与文件名两种都比一遍，且不分大小写**（2026-10-01 第一版只比 basename 就漏了）。
    keys_lower = {k.lower(): k for k in bf.files}
    keep_files, real_ext = set(), []
    for (fid, t), cnt in sorted(ext.items(), key=lambda kv: -kv[1]):
        p = _ext_name(sf, fid)
        hit = keys_lower.get(p.lower()) or keys_lower.get(os.path.basename(p).lower())
        if hit:
            keep_files.add(hit)
        else:
            real_ext.append((t, cnt, p))
    print(f"[P3] 依赖树：{len(kept)} 个对象"
          + " · ".join(f"{t}×{c}" for t, c in by_type.most_common()))
    print(f"[P3] 这个包有 {len(bf.files)} 个内层文件：" + ", ".join(sorted(bf.files.keys())[:12])
          + ("…" if len(bf.files) > 12 else ""))
    if keep_files:
        print(f"[P3] 同包内层文件要一并保留：{sorted(keep_files)}")
    if real_ext:
        print(f"[P3] 🔴 **真外部引用** {len(real_ext)} 类（不在本包内 → 靠 Unity 从**已加载的其它源包**里解析；"
              "解析不到就是空引用）：")
        for t, cnt, p in real_ext[:20]:
            print(f"        {t:24s} ×{cnt:<5d} → {p}")
    if streamed:
        print(f"[P3] 🔴 **走资源流的对象** {len(streamed)} 个（丢了流就是「有对象没数据」）：")
        for t, n, p in streamed[:20]:
            print(f"        {t:24s} {n[:40]:40s} → {p}")
        # 流是内层文件（`<CAB>.resS` / `<CAB>.resource`）⇒ **必须保留**，否则贴图/网格只有壳。
        for _, _, p in streamed:
            hit = keys_lower.get(p.lower()) or keys_lower.get(os.path.basename(p).lower())
            if hit:
                keep_files.add(hit)
            else:
                print(f"        🔴 找不到对应的内层文件：{p}（重打包后这份数据会丢）")
        print(f"[P3] 资源流要保留：{sorted(k for k in keep_files)}")

    if dry_run:
        print("[P4] --check：只体检，未写文件")
        return kept

    # ---- ③·b 🔴 **给内层文件改名** —— 不改名 Unity 直接拒收（2026-10-01 实测踩到）----
    #  现象：`EffectExporter.RunListed` 先把 84 个源包全加载（跨包引用要靠它们解析），
    #        随后 `AssetBundle.LoadFromFile(我们的包)` 返回 **null**，日志里是
    #        「The AssetBundle '…\wf_prefabs_extra.bundle' can't be loaded because another
    #          AssetBundle with the same files is already loaded.」
    #        ⇒ 包根本没进 `packs`，按名字/枚举**都取不到**，而日志只多一行警告。
    #  根因：Unity 按**内层文件（CAB）名**认「这是同一个包」—— 我们的产物沿用了源包的 CAB 名。
    #        （`资料/已知的坑.md` 那条「CAB 同名互斥」是同一件事，那次是 bundle 名撞、这次是 CAB 名撞。）
    #  改法：主 CAB 与它的 `.resS`/`.resource` 整组改名，**同时把纹理 `m_StreamData.path`
    #        里的 `archive:/<旧CAB>/…` 一起改掉**（不改就是「有对象、数据找不到」）。
    #  ⚠️ 只在本模式做：`--arenas`/`--builtin` 那些产物是**运行时**加载的（进程里没有源包），
    #     撞不上；而且它们靠 `HarvestFromLoadedBundles` 那条兜底已经能活。
    #  📌 **同一条规矩的另外两处**（要改一起看）：`工具/extract_mirror_shaders.py` 的 `[6b]`
    #     （它的产物与壳包撞名 ⇒ 那个包整包加载失败、32 份材质退回 `URP/Unlit`）·
    #     `资料/已知的坑.md` 的「随包 bundle 里的东西取不到 ⇒ 先怀疑包整包没加载成功」那条。
    old_base = next(k for k, v in bf.files.items() if v is sf)
    # 🆕 2026-10-03：改名基名**可传参**了（原来硬编码 `CAB-wfprefabsextra`）——
    #   同一个源包会有**多个产物**（`--prefabs` 现在要打战场那件 + 卡包那批），
    #   而 Unity 按**内层文件（CAB）名**认「这是同一个包」⇒ 两个产物同名会被拒收（见上面那段）。
    new_base = cab_name or "CAB-wfprefabsextra"
    rename = {}
    for k in [old_base] + sorted(keep_files):
        if k == old_base:
            rename[k] = new_base
        elif k.startswith(old_base + "."):
            rename[k] = new_base + k[len(old_base):]
        else:
            print(f"[P3] ⚠️ 内层文件 `{k}` 不在主 CAB 名下，保持原名")
            rename[k] = k
    if any(a != b for a, b in rename.items()):
        # ⚠️ **先把内层文件的键改掉**再做后面那些按名字找流的活（下面的 `key` 用的是新名）
        for a, b in rename.items():
            bf.files[b] = bf.files.pop(a)

        # 先把「要改 path/offset 的对象」全读出来（**读一次、写一次**，别来回 save）
        entries = []          # [(o, typetree, m_StreamData, 旧path, offset, size)]
        for o in kept.values():
            try:
                d = o.read_typetree()
            except Exception:
                try:
                    d = o.read()
                except Exception:
                    continue
            sd = _field(d, "m_StreamData")
            p = _field(sd, "path") if sd is not None else None
            if not p or old_base not in p:
                continue
            entries.append([o, d, sd, p, _field(sd, "offset", 0) or 0, _field(sd, "size", 0) or 0])

        # ---- 🔴 ③·c 资源流瘦身：只留**用到的字节区间** ----
        #  为什么：`.resS` 是**整个 CAB 的公共流**（实测 211 MB 原始 / lz4 后 44 MB），
        #  而我们那 6 张纹理合起来只占 **约 224 KB** ⇒ 不裁的话产物 45 MB、白白进包。
        #  做法：按区间切片、**每段 16 字节对齐**（贴图数据惯例，避免未对齐读取）、重算 offset。
        #  ⚠️ 只有「引用同一条流的对象我们**全留着**」时才成立 —— 这里正是（其余对象都被丢了）。
        trimmed = {}
        for os_old in {os.path.basename(e[3]) for e in entries}:
            idx = [i for i, e in enumerate(entries) if os.path.basename(e[3]) == os_old]
            key = rename.get(os_old, os_old)
            if key not in bf.files or not hasattr(bf.files[key], "bytes"):
                print(f"[P3] ⚠️ 流 `{os_old}` 取不到字节，跳过瘦身（保留整条）")
                continue
            raw = bf.files[key].bytes
            buf = bytearray()
            for i in sorted(idx, key=lambda i: entries[i][4]):
                while len(buf) % 16:
                    buf.append(0)
                trimmed[i] = len(buf)
                buf += raw[entries[i][4]:entries[i][4] + entries[i][5]]
            from UnityPy.streams import EndianBinaryWriter
            w = EndianBinaryWriter()
            w.write_bytes(bytes(buf))
            # ⚠️ `bf.save()` 会读每个内层文件的 `flags`（流式标志），换掉的写器要**继承原值**
            w.flags = getattr(bf.files[key], "flags", 0)
            bf.files[key] = w
            print(f"[P3] 资源流瘦身 `{key}`：{len(raw)/1024:.0f} KB → {len(buf)/1024:.0f} KB"
                  f"（{len(idx)} 个对象用到的区间）")

        # ---- 一次写完：path 换成新 CAB 名 + offset 换成瘦身后的位置 ----
        n = 0
        for i, (o, d, sd, p, off, size) in enumerate(entries):
            newp = p.replace(old_base, new_base)
            if isinstance(sd, dict):
                sd["path"] = newp
                if i in trimmed:
                    sd["offset"] = trimmed[i]
            else:
                sd.path = newp
                if i in trimmed:
                    sd.offset = trimmed[i]
            try:
                o.save_typetree(d)
                n += 1
            except Exception as e:
                print(f"[P3] 🔴 改 `m_StreamData` 失败（{o.type.name}）：{type(e).__name__}: {e}")
        print(f"[P3] 内层文件改名 {len(rename)} 条（避免与源包 CAB 撞名）：{rename}；"
              f"顺带改了 {n} 处 `m_StreamData`")
        keep_files = {rename[k] for k in keep_files}

    # ---- ④ 只留依赖树 + AssetBundle 对象 ----
    ab_reader, dropped = None, 0
    for pid, o in list(sf.objects.items()):
        if o.type.name == "AssetBundle":
            if ab_reader is None:
                ab_reader = o
            continue
        if pid in kept:
            continue
        del sf.objects[pid]
        dropped += 1
    print(f"[P4] 保留 {len(kept)} 个对象（丢弃 {dropped} 个），AssetBundle 对象={ab_reader is not None}")
    if ab_reader is None:
        print("!! 源包里没有 AssetBundle 对象 —— 打出来的包 Unity 会拒收，中止")
        return None

    # ---- ⑤ 容器登记：**裸名 + 小写路径**两种写法都登记（`LoadAsset(name)` 两种都可能被调）----
    # 🆕 2026-10-01：后缀按**对象类型**给（原来一律 `.prefab`）—— 材质根要 `.mat`，
    #   否则第二个别名是 `assets/xxx.prefab` 指着一个 Material，看着就是错的。
    #   🆕 同一晚补 `AnimationClip` → `.anim`（`extra_clips` 那条路进来的）。
    def _alias_ext(o):
        return {"Material": ".mat", "Shader": ".shader", "AnimationClip": ".anim",
                "AnimatorController": ".controller"}.get(o.type.name, ".prefab")
    entries = {o.path_id: [n, "assets/" + n.lower() + _alias_ext(o)]
               for n, o in sorted(found.items())}
    _write_bundle(bf, sf, ab_reader, entries, out_path, keep_extra_files=keep_files)
    return kept


def run_prefabs(args):
    """`--prefabs` 模式：把**非 addressable** 的 prefab（+ 依赖树）重打成我们自己的小包。

    🔴 **为什么必须重打**：这些件是「原版真在用、但不是资产根、又不在 addressables 容器里」的
    prefab ⇒ Unity 侧 `GetAllAssetNames()`（只吐容器）与 `LoadAllAssets<GameObject>()`
    （只吐可加载的资产根）**都枚举不到**，按名字 `LoadAsset` 也拿不到。
    判据 → `资料/已知的坑.md` 的「`GetAllAssetNames()` 只吐容器里的资产」那条。

    两批（2026-10-03 起）：
      ① **战场**那两件 → `wf_prefabs_extra.bundle`（本函数原来的那一段，行为一字未改）；
      ② **卡包两扇窗那批**（`BOOSTER_GROUPS`）→ `wf_boosters_extra.bundle` + `wf_menus_extra.bundle`
         （判据 → `资料/阶段二_商店_原版规格.md` §五·三）。
    ⚠️ `--out` 只作用于 ①；② 的目标名写在 `BOOSTER_GROUPS` 里（一个源包一个产物，不能合并）。
    """
    want = args.out or DEST_PREFABS
    print(f"[P0] 源包 {os.path.basename(SRC_BUNDLE)} → {want}")
    # 这个包里那些控制器要用的动画片段 —— 名字从数据表来，不手写（见 `controller_clip_names`）
    src_dir = "bundle_" + os.path.basename(SRC_BUNDLE)[:-len(".bundle")]
    clips = controller_clip_names(src_dir)
    print(f"[P0] 控制器数据表里「源包 = {src_dir}」："
          f"AnimationClip {clips} · AnimatorController {controller_names(src_dir)}")
    kept = repack_tree(SRC_BUNDLE, want, PREFAB_TARGETS, dry_run=args.check,
                       extra_materials=MATERIAL_TARGETS, extra_clips=clips,
                       extra_controllers=controller_names(src_dir))
    if kept is None:
        return 1

    # ---- 🆕 2026-10-03：卡包那两扇窗（`BOOSTER_GROUPS`）----
    for g in BOOSTER_GROUPS:
        if not os.path.isfile(g["src"]):
            print(f"[P0] 🔴 源包不在，跳过这一组：{g['src']}")
            continue
        print(f"\n[P6] 卡包组：源 {os.path.basename(g['src'])} → {g['out']}")
        print(f"[P6] 根 GameObject {g['roots']} · 额外 AnimationClip {g['clips']}")
        k2 = repack_tree(g["src"], g["out"], g["roots"], dry_run=args.check,
                         extra_clips=g["clips"], cab_name=g["cab"])
        if k2 is None:
            print(f"[P6] 🔴 这一组没打出来（根一件都没找到？）—— 上面 [P1] 那几行会点名是哪一件")
            return 1
        print(f"[P6] ✓ {os.path.basename(g['out'])} 打出 {len(k2)} 个对象（含整棵依赖树）")

    if args.check:
        return 0
    print(f"\n[P5] 完成。Unity 侧：")
    print(f"     `EffectExporter.RunListed` / `BoosterPackExporter` 都会同时扫 "
          f"`{DEST_DIR}` 里的包，按名字取。")
    return 0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只体检，不写文件")
    ap.add_argument("--builtin", action="store_true",
                    help="改打内置管线包：源 Warpforge_unitybuiltinassets.bundle → 目标 wf_builtin.bundle")
    ap.add_argument("--arenas", action="store_true",
                    help="改打**战场 shader 包**：源 battlesharedresources_assets_all.bundle → 目标 wf_arena_shaders.bundle")
    ap.add_argument("--prefabs", action="store_true",
                    help="改打**非 addressable 的 prefab 包**：源 battleprefabs_vfxandmisc_assets_all.bundle "
                         "→ 目标 wf_prefabs_extra.bundle（含整棵依赖树）")
    ap.add_argument("--out", default=None,
                    help="默认 extra 模式写 wf_shaders_extra.bundle；--builtin 模式写 wf_builtin.bundle")
    args = ap.parse_args()
    if args.out is None:
        args.out = (DEST_ARENA if args.arenas
                    else (DEST_BUILTIN if args.builtin
                          else (DEST_PREFABS if args.prefabs else DEST_BUNDLE)))

    if args.arenas:
        return run_arenas(args)
    if args.builtin:
        return run_builtin(args)
    if args.prefabs:
        return run_prefabs(args)

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
