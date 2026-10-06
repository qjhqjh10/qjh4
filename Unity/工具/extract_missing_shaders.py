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
   ⚠️ **多 CAB 包**（源包内层不止一份 CAB）另有三条口径 → `repack_tree` 的函数头 + `A173`/`A230`
   （写哪一份 = `AssetBundle` 对象那一份；另一份**整组**保留 + 整组改名；**根不在那一份 ⇒ A230 起
   走跨 CAB 容器引用**（`m_FileID ≠ 0`），原来只能中止点名）。
   🔴 **`repack()`（`--arenas`/`--builtin`/默认三模式用）也已按这三条对齐**（A231②，2026-10-13）。
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

🆕 2026-10-07：`wf_prefabs_extra.bundle` 里**又多了两条 `AnimationClip`** ——
  `LightAnimationOrbit` · `Dark Angels Void Combat animations`（消费方 = 环境混合组件
  `ScenarioAnimationBlend`，它手里只有 **assetGUID**）⇒ 名字与 GUID 都从数据表来
  （`animator_controllers.json` 的 **`clipsByGuid`** 那一节，见 `clips_by_guid`），
  打的时候**按 GUID 再登记一条容器别名**（原版源包的容器键就是 GUID）。

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

🔴 **A523（2026-10-14）：本文件的重打包口**只有两个、**谁是唯一口**、冲突时听谁的 —— 就这一张表
------------------------------------------------------------------------------------------
（起因：`repack()` 与 `repack_tree()` 并存过一段时间、两套口径各自演化，A173 / A230 / A231②
 修了三轮才对齐 ⇒ ⛔ **不许再开第三个口**：第三个口 = 第三条口径 = 同一个病根。判据与逐条实据
 → `资料/普查产出_1013/W230_跨CAB与repack.md` §四。）

| 模式（`main()` 分发 · 现读） | **唯一口** | 产物 | 内层 CAB 改名 | 别份 / 流怎么裁 |
|---|---|---|---|---|
| 默认（extra） · `--builtin` · `--arenas` | **`repack()`** | `wf_shaders_extra` · `wf_builtin` · `wf_arena_*` | ⛔ **不改**（沿用源包 CAB 名 —— ⚠️ **有条件的**，见 A801 那段） | 别份**照裁**（只留它的 Shader） |
| `--prefabs` | **`repack_tree()`** | `wf_prefabs_extra` · `wf_menus_extra` · `wf_boosters_extra` · 卡包那几包 | ✅ **整组改名**（`new_base` / `new_base_1/2/…`） | 依赖树引用到的别份**整份留**（按需切流） |

· **两个口共用的只有一段**：`_write_bundle()` —— **唯一写盘口**（重写 `m_Container` + 决定留哪几个
  内层文件 + 丢流 + 落盘）。全库 `_write_bundle(` 的调用点**恰好 2 处**，就是上表那两行。
· 🔴 **两者口径冲突时以谁为准**：A173 那三条共用口径（写哪一份 CAB / 别份怎么留 / 根在别份怎么办）
  **以 `repack_tree()` 为准** —— A231② 就是拿 `repack()` **去照它对齐**的（依据见 `repack()` 函数头）。
· ⚠️ **下面两条与 `repack()` 不同，都不是欠账**（⛔ 别再当「没对齐」修一次）：
  ① 别份的裁剪（= W230 §四 说的「**唯一有意不同的一处**」；判据在 `repack()` 函数头：它的容器项**只指 Shader**）；
  ② 内层折不折名（判据在 `repack()` 函数头 + A801 那段输出 —— 那是**有条件**的，条件已写进输出）。
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
               # 🆕 **2026-10-13（A425①）**：`RewardAppearParticle` —— 领奖窗「收集」那一下的粒子。
               # 出处 = `资料/普查产出_1012/H3_领取粒子与Blink公共件.md`；原件在 **`bundle_menus_assets_all`**
               # （与 `BOOSTER_GROUPS[0].src` 同包，所以只加这一行就够）。⚠️ 加完要跑 `--prefabs`。
               "RewardAppearParticle",
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
# 🆕 2026-10-07（A192）：同一张表里还有一节 **`clipsByGuid`** —— 那两条 clip **不属于任何控制器**
#   （引用它们的是环境 SO 的 `animationsToChange[].clip`，一个 **assetGUID**）⇒ 单开一节记
#   `(guid, 名字, 源包)`。走同一张表、**同样不手写名字**；要收哪两条由表的 `source` 字段决定。
CTRL_TABLE = r"d:/4/Unity/数据/游戏数据/animator_controllers.json"

_CTRL_TABLE_CACHE = None


def _load_ctrl_table():
    """`animator_controllers.json` 读一次就缓存（`controllers_of` / `clips_by_guid` 共用一份）。

    ⚠️ 读不到时返回 `{}` 并**出声**（两条取用方都会因此什么都收不到 —— 不许静默）。
    """
    global _CTRL_TABLE_CACHE
    if _CTRL_TABLE_CACHE is None:
        try:
            with open(CTRL_TABLE, encoding="utf-8") as f:
                _CTRL_TABLE_CACHE = json.load(f)
        except Exception as e:
            print(f"[P0] ⚠️ 读不了控制器数据表 `{CTRL_TABLE}`（{e}）⇒ 控制器/动画片段那批根收不到")
            _CTRL_TABLE_CACHE = {}
    return _CTRL_TABLE_CACHE


def controllers_of(bundle_dir_name):
    """`animator_controllers.json` 里「源包 = bundle_dir_name」的控制器整条。"""
    return [c for c in _load_ctrl_table().get("controllers", [])
            if c.get("source") == bundle_dir_name]


def clips_by_guid(bundle_dir_name):
    """`animator_controllers.json` 的 `clipsByGuid` 里「源包 = bundle_dir_name」的那些片段。

    🔴 **为什么单开一节、而不是塞进 `controllers[].clips`**：这两条片段的引用方**不是控制器**，
    而是环境 SO 的 `animationsToChange[].clip`（`AssetReferenceTyped<AnimationClip>`，**按 assetGUID 取**）
    ⇒ 它们不属于任何控制器，只能按「源包 + GUID」记。

    返回 `[(clip 名, assetGUID), …]`：**名字**用来把片段收进包，**GUID** 用来登记一条**容器别名**
    —— 原版源包的容器键**就是 GUID**（实测 `bundle_battleprefabs_vfxandmisc_assets_all` 的
    `AssetBundle/AssetBundle_1.json`：988 条键全是 32 位十六进制，`58db0a1f…` → PathID
    1230949865609814630 = `LightAnimationOrbit`）⇒ 运行时 `LoadAsset<AnimationClip>(guid)`
    就是原版那条取法，**GUID→名字的映射全仓只有这一处**（消费方 `ScenarioAnimationBlend` 不抄第二份）。

    ⚠️ 这一节是**手加**的（生成器不认识它 —— 表里的 `_clipsByGuid_note` 记着这件事）。
    ✅ **2026-10-07 更正**：原来这句还写着「重跑 `gen_animator_controllers.py` 会**冲掉**它」—— **不成立了**：
    A200 起生成器**会原样保住这一节**（先读旧产物 → 逐条回原版解包树核对 → 原样写回，并打一行
    `[AC] clipsByGuid…原样保留 N 条`；真读不到时它自己出声 + 退出码 1，**不静默**）⇒ **重跑安全**。
    判据 → `资料/普查产出_1007/波9_A192收尾_验证脚本与生成器.md`（代码 = `工具/gen_animator_controllers.py:378-424`）。
    """
    out = []
    for c in (_load_ctrl_table().get("clipsByGuid") or []):
        if c.get("source") != bundle_dir_name:
            continue
        n, g = c.get("name"), c.get("guid")
        if not n:
            print(f"[P0] ⚠️ `clipsByGuid` 里有一条没写 `name`：{c}")
            continue
        if not g:
            print(f"[P0] ⚠️ `clipsByGuid` 里 `{n}` 没写 `guid` —— 片段会收进包，但**没有 GUID 别名**，"
                  f"运行时按 GUID 取不到（出声）")
        out.append((n, g))
    return out


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
    """只留 Shader 对象 + AssetBundle 对象，重写容器，另存为一个小包。返回 `{(CAB 内层名, path_id): shader 名}`。

    🔴 **A523：本函数 = 「默认（extra）· `--builtin` · `--arenas`」三个模式【唯一的口】**
       （`--prefabs` 那个口是 `repack_tree()`）；两个口的**归属表 / 冲突时听谁的 / 有意不同之处**
       —— 全在**文件头那张表**（⛔ 别在这里再抄一份）。公共尾段 = `_write_bundle`（唯一写盘口）。

    🔴 **三步都做对才行**（文件头 ⚠️2）：① 只留 Shader + AssetBundle；
    ② **必须重写 `m_Container`** —— 留着不重写的话包能加载、但**一个资产都暴露不出来**
    （`LoadAllAssets<Shader>()` 返回 0；2026-09-19 在 `Warpforge_unitybuiltinassets.bundle`
    上又实测了一次，那次容器是 **0 条**）；③ 丢掉 `.resS`/`.resource` 流。

    🔴 **A231②（2026-10-13）：本函数已按 `repack_tree()` 的 A173 口径对齐**（原来「两套并存」）：
    · **写哪一份 CAB = 装着 `AssetBundle` 对象的那一份**（原来 `next(…"SerializedFile")` = 取第一份）；
    · **shader 从【所有 CAB】收**（原来只收第一份里的 ⇒ 别的 CAB 里的 shader 被**静默漏掉**，
      而且 `run_arenas` 的 `[A3]` 是用 `collect_shader_names`（全 CAB）找的 ⇒ 两处口径还打架）；
    · 别的 CAB 里的 shader ⇒ 走 **A230 的跨 CAB 容器引用**（`m_FileID` 指进那一份）+ 那个内层文件留下，
      并**逐条出声**；⛔ 收不到 / 解不开一律出声，不许静默跳过。
    ⚠️ **与 `repack_tree()` 有意不同的一处**：**「要留下的别的 CAB」在这里也照裁**（只留它的 Shader）
    —— 本函数的容器项**只指 Shader**（shader 的字节码 `compressedBlob` 内联、不依赖别的对象），
    而 `repack_tree` 的根带一整棵依赖树、别的 CAB 必须**整份**留（见它 ③·a）。
    ⚠️ **内层文件不改名**（沿用源包的 CAB 名）：这三个模式的产物是**运行时**加载的、进程里没有源包
    ⇒ 撞不上（与 `repack_tree` 不同，见它 ③·b 那段实测）。
    🔴 **A801（2026-10-14）：这条是【有条件的】，条件已写进输出** —— ⛔ **行为一个字没改**
    （沿用既有行为），改的是「让人看得见」：落盘之后会印一行 `[6] ⚠️ **内层文件沿用源包名**…`，
    写明 **一旦源包与产物同时加载**（Unity 按内层名判「同一个包」）⇒ **整包被拒收**
    （症状 = `LoadFromFile` 返回 null + 日志一行 `another AssetBundle with the same files is
    already loaded`），以及到那时该照 `repack_tree` ③·b 改成什么名。
    📌 今天**没有**走到那条路：这三个模式的产物由 `WarpforgeShaderLoader` 在运行时加载，
    进程里不装源包（`--prefabs` 那三个模式**会**同时加载，所以它们才必须改名）。
    """
    env = UnityPy.load(src_path)
    bf = list(env.files.values())[0]

    # ---- ⓪ 🔴 A231②：**写哪一份 CAB** = 装着 `AssetBundle` 对象的那一份（与 `repack_tree()` 同口径）----
    #  判据：容器项/预加载项写的是 `PPtr(m_FileID=0, PathID)`（`_write_bundle`），而 `m_FileID == 0`
    #  = **引用方自己所在的那一份** ⇒ 只有把 shader 收进 AssetBundle 那一份，`m_Container` 才指得对。
    #  ⚠️ 旧写法「取第一份」在**全库 84 个包里碰巧总是** AB 那一份（2026-10-08 普查：15 个多 CAB 包，
    #     AB 对象 15/15 都在第一份）—— 那是运气、不是规则；同族的 A173 在 `repack_tree` 里已经因为
    #     「按 pid 取第一份」**静默收错过对象**（arena1：pid 1 在 `.sharedAssets` 里是 `PreloadData`）。
    cab_sfs = [v for v in bf.files.values() if type(v).__name__ == "SerializedFile"]
    ab_owner = [(v, pid) for v in cab_sfs for pid, o in v.objects.items()
                if o.type.name == "AssetBundle"]
    if not ab_owner:
        print("!! 源包里没有 AssetBundle 对象 —— 打出来的包 Unity 会拒收，中止")
        return None
    if len(ab_owner) > 1:
        print(f"!! 这个包里有 {len(ab_owner)} 个 AssetBundle 对象（要求恰好 1 个）⇒ 中止："
              + " · ".join(f"pid {pid} @`{v.name}`" for v, pid in ab_owner)
              + "。`_write_bundle` 只重写其中一个的容器，**另一个会带着指向已删对象的旧容器留在包里**"
                "（静默坏包）⇒ 不猜，先人工定。")
        return None
    sf = ab_owner[0][0]                        # `sf` = 要写的那一份（容器项 `m_FileID=0` 指的就是它）
    if len(cab_sfs) > 1:
        print(f"[6] ⓘ **多 CAB 包**：内层有 {len(cab_sfs)} 份 CAB（{[v.name for v in cab_sfs]}）；"
              f"AssetBundle 对象在 `{sf.name}` ⇒ **写这一份**")

    # ---- ① 收 shader：**所有 CAB 都收**（A231② 修的就是这里：旧写法只看第一份）----
    #  ⚠️ **先只读地收**（不动对象表）—— 要等知道「哪几份 CAB 会留下」再动刀，见 ③。
    shaders_by_cab = {}                        # CAB 名 → {pid: shader 名}
    for v in cab_sfs:
        for pid, o in v.objects.items():
            if o.type.name != "Shader":
                continue
            try:
                n = shader_name(o.read())
            except Exception:
                n = ""
            if n:
                shaders_by_cab.setdefault(v.name, {})[pid] = n
    kept = {(cab, pid): n for cab, d in sorted(shaders_by_cab.items())
            for pid, n in sorted(d.items())}
    if len(cab_sfs) > 1:
        print(f"[6] 按 CAB 收 shader：" + " · ".join(
            f"`{cab}` {len(d)} 个" for cab, d in sorted(shaders_by_cab.items())))

    # ---- ② 容器登记：跨 CAB 的 shader 走 `m_FileID ≠ 0`（A230），那一份的内层文件必须留下 ----
    entries, cross_cabs, unregistered = {}, {}, []
    for (cab, pid), n in sorted(kept.items()):
        fid, how = _ext_fid(sf, cab)
        if fid is None:
            unregistered.append((cab, pid, n, how))
            continue
        entries[(fid, pid)] = [n]
        if cab != sf.name:
            cross_cabs.setdefault(cab, []).append(n)
            print(f"[6] ⓘ **跨 CAB 引用**：`{n}`(PathID {pid}) @`{cab}` ⇒ 容器/预加载项 "
                  f"`m_FileID={fid}`（{how}）")
    if cross_cabs:
        print(f"[6] 🔴 **别的 CAB 里也有 shader** {cross_cabs} ⇒ 那些内层文件**要留下**"
              f"（整组：CAB + 它的 `.resS`/`.resource`；它们的对象只留 Shader）")
    if unregistered:
        print(f"[6] 🔴 **{len(unregistered)} 个 shader 登记不进容器**（这份包里的它们会缺席）—— "
              f"⛔ 不打猜的引用：")
        for cab, pid, n, how in unregistered:
            print(f"        `{n}`(PathID {pid}) @`{cab}`：{how}")
    # 同名 shader 落在**两份 CAB**里（同一份里 pid 不同、名字相同）时，容器里会出现**两条同名键**
    # ⇒ `LoadAsset(名字)` 取到哪一条由 Unity 定 —— 这里**出声**，⛔ 不静默挑。
    _dup = collections.defaultdict(list)
    for (fid, pid), names in entries.items():
        for n in names:
            _dup[n].append((fid, pid))
    for n, where in sorted(_dup.items()):
        if len(where) > 1:
            print(f"[6] 🔴 同名 shader `{n}` 登记了 {len(where)} 条容器项 {where} ⇒ "
                  f"`LoadAsset('{n}')` 取到哪一条**由 Unity 定**（本脚本不挑、也不合并）")

    keep_extra = set()
    for cab in cross_cabs:
        for k in bf.files.keys():
            if _cab_group(k) == _cab_group(cab):
                keep_extra.add(k)
    if keep_extra:
        # ⚠️ **体积口径**：整组留 = 与 `repack_tree` 的 A173 口径 2 **同一条规则**（一处规则、不是两处）；
        #    代价 = 产物变大（那几份内层文件按**未压缩**字节算）。这里**把数字打出来**，不藏着。
        sz = sum(len(bf.files[k].bytes) for k in keep_extra if hasattr(bf.files.get(k), "bytes"))
        print(f"[6] ⓘ 体积：留下的别组内层文件 {sorted(keep_extra)} 未压缩合计 {sz / 1048576:.1f} MB"
              f"（⛔ 今天**没有真例**走到这一支 —— 全库 84 个包里 0 个「shader 住在别的 CAB」；"
              f"⚖️ 「要不要连别组的流一起留」这条口径**还没定**，见 `资料/普查产出_1013/W230_跨CAB与repack.md` §六）")

    # ---- ③ 裁：**只裁「会留下的那几份」**（`sf` + 上面留下来的别份）----
    #  ⚠️ 其余 CAB 会被 `_write_bundle` **整个丢掉**（不在 `keep_extra_files` 里的内层文件一律删）
    #     ⇒ 不去动它们的对象表（动了等于白干，而且 `丢弃 N 个对象` 那个数会变得没法跟改前对）。
    keep_cabs = {sf.name} | set(cross_cabs)
    ab_reader, dropped = None, 0
    for v in cab_sfs:
        if v.name not in keep_cabs:
            continue
        keep_pids = set(shaders_by_cab.get(v.name, {}))
        for pid, o in list(v.objects.items()):
            if v is sf and o.type.name == "AssetBundle":
                ab_reader = o
                continue
            if pid in keep_pids:
                continue
            del v.objects[pid]
            dropped += 1
    print(f"[6] 保留 {len(kept)} 个 shader + AssetBundle({ab_reader is not None})，丢弃 {dropped} 个对象")
    if ab_reader is None:
        print("!! **要写的那一份**里没有 AssetBundle 对象 —— 打出来的包 Unity 会拒收，中止"
              "（正常到不了这里：⓪ 已经按「AB 对象在哪份」挑过了）")
        return None

    # ---- ④ 重写容器 + 写文件（公共尾段，与 `repack_tree` 共用）----
    _write_bundle(bf, sf, ab_reader, entries, out_path, keep_extra_files=sorted(keep_extra))

    # ---- 🔴 **A801（2026-10-14）：内层文件【不改名】这条的代价，写死在输出里** ----
    #  ⛔ **行为一个字没改**（沿用既有行为，条件与理由见函数头那一段）—— 改的是「让它看得见」：
    #    原先这条只在 docstring 里，跑完只看到 `[9] 已写出`，读输出的人**无从知道**产物沿用了源包的
    #    内层名、也不知道它什么时候会变成雷。`W230 §9·4` 记的「已写进代码输出」指的就是这一行。
    #  ⚠️ 名字取 `keep_cabs`（= ③ 段算出来的「会留下的那几份」）：`bf.files` 这时已被 `_write_bundle`
    #    裁过，不能拿它当「产物里有哪些内层文件」。
    print(f"[6] ⚠️ **内层文件沿用源包名**（⛔ 本模式【不】改名）：{sorted(keep_cabs)} —— "
          f"依据 = 这三个模式的产物是**运行时**加载、进程里没有源包 ∴ 撞不上"
          f"（`repack_tree` 那三个模式不同：`EffectExporter` 会先把 84 个源包全加载，所以它必须改名）。"
          f"🔴 **一旦源包与产物【同时加载】**，Unity 按内层名判「同一个包」⇒ **整包被拒收**"
          f"（症状：`LoadFromFile` 返回 null + 日志一行 `another AssetBundle with the same files is "
          f"already loaded`）⇒ 到那时照 `repack_tree` ③·b 把内层名改成 `CAB-<out 基名>`。"
          f"（A801 · 判据 → `资料/普查产出_1013/W230_跨CAB与repack.md`）")
    return kept


def _write_bundle(bf, sf, ab_reader, entries, out_path, keep_extra_files=()):
    """**重打包的公共尾段**（`repack` 与 `repack_tree` 共用；2026-10-01 从 `repack` 抽出来）。

    🔴 **A523（2026-10-14）：本函数是【唯一写盘口】** —— 全库 `_write_bundle(` 的调用点**恰好 2 处**
       （`repack()` / `repack_tree()`，= 文件头那张归属表的两行）⇒ ⛔ 别在别处另起一段写盘逻辑
       （「重写 `m_Container` + 留哪些内层文件 + 丢流」这三件必须只有一处实现）。

    `entries` = `{(m_FileID, path_id): [容器名, …]}` —— **一个资产可以登记多个名字**（别名），
    这样 Unity 侧 `LoadAsset(name)` 无论用裸名还是小写路径写法都能命中。
    🔴 **A230（2026-10-13）**：键从裸 `path_id` 改成 `(m_FileID, path_id)` —— 目标对象在**别的内层 CAB**
    里时 `m_FileID ≠ 0`（= `sf.externals[m_FileID-1]`，见 `_ext_fid`）；`m_FileID = 0` = **`sf` 自己那一份**。
    ⚠️ 为什么非带 `m_FileID` 不可：**pid 在两份 CAB 之间会撞号**（arena1 实测 63 个）⇒ 裸 pid 指得进
    「同 pid 的另一个对象」而且一个字都不报（这正是 A173 的缺陷形态）。
    `keep_extra_files` = 除主 SerializedFile 之外**必须保留**的内层文件（`repack_tree` 用：
    依赖树落同包另一个 CAB 时，丢掉它 = 引用断掉）。🔴 **A173** 起它是**整组**给的
    （那个 CAB + 它的 `.resS`/`.resource`，且整组已经改过名 —— 见 `repack_tree` 的 ③·a）。

    ⚠️ **三条都是实测踩出来的，缺一条就静默失败**（原始判据全部保留在下面注释里）。
    """
    ab = ab_reader.read()
    # 🔴 **`preloadIndex` 是「对 `m_PreloadTable` 的索引」—— 两张表必须一起写！**（2026-09-19 实测）
    #    容器写成 `preloadIndex=0 / preloadSize=1` 而 `m_PreloadTable` 是**空**的时候，
    #    Unity 会在 `AddAssetsToPreload`（`LoadAllAssets` 的预加载那一步）里**直接段错误** ——
    #    不是报错、是崩溃（栈：`LoadAssetWithSubAssets_Internal → ProcessAssetBundleEntries
    #    → PreparePreloadAssets → AddAssetsToPreload`）。
    #    **这个坑一直藏着**：上一个源包 `battleprefabs_vfxandmisc` 恰好带着一张 **86348 条**的
    #    预加载表，索引 0 落在界内 ⇒ 侥幸能跑；换成预加载表为空的内置包立刻现形。
    #    两种改法都实测可行：① 补齐预加载表 + 逐个索引（本脚本采用）② `preloadSize=0`。
    #    复现与全部变体见 `资料/普查产出_0919/内置shader原件_加载崩溃_实测.md`。
    ab.m_PreloadTable = [PPtr(m_FileID=fid, m_PathID=pid, assetsfile=sf) for fid, pid in entries]
    ab.m_Container = [
        (alias, AssetInfo(asset=PPtr(m_FileID=fid, m_PathID=pid, assetsfile=sf),
                          preloadIndex=i, preloadSize=1))
        for i, ((fid, pid), aliases) in enumerate(entries.items())
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
    """`m_FileID`（非 0）→ 外部文件名（同包内层 CAB / 真外部包都能认）。

    🔴 **2026-10-07（A161 ④）**：`sf` 必须是**发出这条引用的那个对象自己那份** SerializedFile ——
    双 CAB 包的两份 `externals` **逐位不同**（实测 `scenes_scenes_battlearena1` 差 17/21 条）⇒
    拿「包里的**第一个** SerializedFile（= `.sharedAssets`）」去解，报出来的**外部包名是错的**，
    排查会被带到别的包去。调用点（`[P3]` 那段）已按**宿主自己的 CAB** 取（见 `sf_by_cab`）。
    """
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


def _cab_group(name):
    """内层文件名 → 它所属的 **CAB 组名**（`<CAB>.resS` / `<CAB>.resource` 都归到 `<CAB>`）。

    🔴 **A173**：多 CAB 包里「另一份怎么留」是按**组**处理的 —— 只留 CAB、丢了它的 `.resS`，
    那个 CAB 里的贴图/网格就是「有对象、没数据」（而它不在这份名单里时**没人会报**）。
    改内层名也必须整组一起改（Unity 按内层名认「同一个包」，撞名整包被拒收）。
    ⚠️ `CAB-x` 与 `CAB-x.sharedAssets` 是**两个不同的文件**、不是一组 —— 所以只剥上面两种后缀。
    """
    for suf in (".resS", ".resource"):
        if name.endswith(suf):
            return name[: -len(suf)]
    return name


def _archive_path(name):
    """内层文件名 → `externals[].path` 的写法（`archive:/<基名>/<内层文件名>`）。

    🔴 **判据 = 原版自己的写法**（三处逐字对上，2026-10-13 现读）：
      · 兄弟 CAB：`scenes_scenes_battlearena1` 的**主 CAB** 的 externals 里那条
        `archive:/CAB-8adfc300739b4da5111d4bd3eae80365/CAB-8adfc300739b4da5111d4bd3eae80365.sharedAssets`；
      · 别的包的 CAB：`menus_assets_all` 等包写的是 `archive:/CAB-xxxxxxxx/CAB-xxxxxxxx`；
      · 资源流：`archive:/<CAB>/<CAB>.resS`（`_rename_stream_path` 的既有口径 + 本仓产物实测）。
    ⇒ **目录段 = 去掉 `.resS` / `.resource` / `.sharedAssets` 后缀的基名；文件段 = 内层文件名本身**。
    （三者都是「文件段 = 真文件名」，所以目录段按上面三种后缀剥一次就够。）
    """
    base = name
    for suf in (".resS", ".resource", ".sharedAssets"):
        if base.endswith(suf):
            base = base[: -len(suf)]
            break
    return f"archive:/{base}/{name}"


def _ext_fid(sf, cab_name):
    """目标内层 CAB 名 → 在 `sf`（= **装着 `AssetBundle` 对象那一份**）的 `externals` 里的 `m_FileID`。

    返回 `(fid, 说明)`。`fid = 0` = 就是 `sf` 自己；`fid = k > 0` = `sf.externals[k-1]` 指的那一份
    （`UnityPy/classes/PPtr.py:53-88` 的 `deref`：`external_id = m_FileID - 1`，`archive:/` 前缀 = 包内）。
    找不到既有条目时**补一条**（照 `_archive_path` 的原版写法 + 本文件既有条目的字段样式），
    并在说明里写明是**新补的**；补不出来时返回 `(None, 原因)`，由调用方**出声点名**（⛔ 不打猜的引用）。

    🔴 **A230 的证据级别**（2026-10-13 查证；⛔ 别当「Unity 已验」）：
      · **格式级**（规则本身）：`m_FileID` = `externals` 的下标 + 1，`archive:/` = 包内 —— 见上。
      · **原版数据里的先例（`m_PreloadTable`）**：`m_FileID != 0` 的条目遍布全库
        （84 个包里 30+ 个有；`menus_assets_all` 一家 3.5 万条、fid 最大 29；`shaders_assets_all` 4 条）
        ⇒ 「跨文件预加载项」这个形态**原版自己就在用**。
      · ⚠️ **但那些 preload 项指的都是【别的包】（跨 bundle），不是同包兄弟 CAB**；同包兄弟 CAB 的
        `m_FileID` 先例只在**对象引用**里见过（主 CAB → `.sharedAssets` 97 条）。
      · ⚠️ **`m_Container` 侧本地【没有先例】**：全库 84 个包 **13448 条容器项，`m_FileID` 全是 0**
        （2026-10-13 普查）⇒ 本函数写出的容器项是**新形态**，Unity 侧尚未验（A230② / A231③ 仍开）。
    """
    if cab_name == getattr(sf, "name", None):
        return 0, "本份（`m_FileID=0`）"
    want = cab_name.lower()
    exts = getattr(sf, "externals", None) or []
    for i, e in enumerate(exts):
        p = getattr(e, "path", None)
        if p is None and isinstance(e, (list, tuple)) and len(e) >= 2:
            p = e[1]
        if p is None and isinstance(e, dict):
            p = e.get("path")
        if not isinstance(p, str) or not p:
            continue
        base = p[9:] if p.startswith("archive:/") else p
        if base.rsplit("/")[-1].lower() == want:
            return i + 1, f"既有 `externals[{i}]` = `{p}`"
    # ---- 补一条：字段样式照本文件里**既有的 `archive:/` 条目**（guid 原版全是 16 个 0 字节）----
    try:
        from UnityPy.files.SerializedFile import FileIdentifier
        tmpl = next((e for e in exts
                     if isinstance(getattr(e, "path", None), str)
                     and getattr(e, "path").startswith("archive:/")), None)
        new = FileIdentifier.__new__(FileIdentifier)
        new.path = _archive_path(cab_name)
        new.temp_empty = getattr(tmpl, "temp_empty", None) if tmpl is not None else None
        new.guid = getattr(tmpl, "guid", None) if tmpl is not None else None
        new.type = getattr(tmpl, "type", None) if tmpl is not None else None
        if new.temp_empty is None:
            new.temp_empty = ""                  # `FileIdentifier.write`：version ≥ 6 时 assert 非 None
        if new.guid is None:
            new.guid = b"\x00" * 16              # `write` 里 `write_bytes(guid)` ⇒ 必须 16 字节
        if new.type is None:
            new.type = 0
        sf.externals = list(exts) + [new]        # 追加在**末尾**：既有条目的下标一个都不动
        return len(sf.externals), (f"🔴 **新补** `externals[{len(sf.externals) - 1}]` = `{new.path}`"
                                   f"（原版没有这条；写法照 `_archive_path`，字段样式照本文件既有 `archive:/` 条目）")
    except Exception as e:
        return None, f"补 `externals` 条目失败：{type(e).__name__}: {e}"


def _rename_stream_path(p, rename):
    """把 `m_StreamData.path` / `externals[].path`（形如 `archive:/<目录>/<内层文件名>`）里的**内层名换掉**。

    🔴 **两段都换**：源包写的就是 `archive:/<CAB 名>/<CAB 名>.resS`（目录段 = 那个 CAB、文件段 = 内层流名），
    而旧代码的 `p.replace(old_base, new_base)` 也是两段都换 —— 产物 `wf_prefabs_extra.bundle` 里实际就是
    `archive:/CAB-wfprefabsextra/CAB-wfprefabsextra.resS`（`_verify_prefab_bundle.py` 第 3 项按这个判
    「新名在、旧名不在」）⇒ 这里照旧口径，只把 replace 换成**按段查表**。
    ⚠️ 为什么不用字符串 `replace`：多组时**前缀会互相吃**（`CAB-x` 是 `CAB-x.sharedAssets` 的前缀）⇒ 逐段查表。
    ⚠️ 目录段只在它**确实是本包里被改过名的那个文件**时才换（真外部引用、`Library/unity default resources`
    这些一律不动）。目录段该不该跟着换**没有真 Play 证据**，这里跟的是旧代码的既有口径 + 已有产物。
    返回 `(新路径, 有没有改过)`。
    """
    d, sep, f = p.rpartition("/")
    nf = rename.get(f, f)
    nd = d
    if sep:
        # 目录段形如 `archive:/<CAB 名>` ⇒ 查表前先把 `archive:/` 前缀剥掉（否则查不到，产物里就会
        # 留下**旧的 CAB 名** —— 实测漏了这一步：改后产物比改前多 92 字节、`_verify_prefab_bundle.py`
        # 第 3 项「旧名不在」当场会红）。
        pre, tail = ("", d)
        if tail.lower().startswith("archive:/"):
            pre, tail = tail[:9], tail[9:]
        nd = pre + rename.get(tail, tail)
    if nf == f and nd == d:
        return p, False
    return (nd + sep + nf if sep else nf), True


def repack_tree(src_path, out_path, names, dry_run=False, extra_materials=(), extra_clips=(),
                extra_controllers=(), cab_name=None, extra_clip_guids=None):
    """把 `names` 这几件 GameObject **连同整棵内部依赖树**重打成一个小包。

    🔴 **A523：本函数 = `--prefabs` 那个模式【唯一的口】**（另三个模式的口是 `repack()`）；
       A173 那三条共用口径**以本函数为准**，`repack()` 是照它对齐的；两个口共用的尾段 =
       `_write_bundle`（唯一写盘口）—— 归属表 / 有意不同之处 → **文件头那张表**（⛔ 别在这儿抄）。

    🔴 **为什么要连依赖树一起**：prefab 被 Unity 实例化时会去解析材质 / 网格 / 贴图 / 控制器引用，
    少一件就表现成「某个槽是 null」（在导出侧只看到「材质是空的」，很难归因）。
    做法 = 从根 GameObject 出发，递归收所有 `m_FileID == 0` 的引用。

    🔴 **两个必须判的东西**（判错就是**静默**坏资产）：
    1. **同包内层 CAB**：引用落在同 bundle 的另一个 CAB 里时，那个 CAB **不能丢**
       ——`_write_bundle` 默认「只留一个内层文件」，这里按需追加 `keep_extra_files`。
    2. **资源流（`.resS` / `.resource`）**：贴图/网格的大块数据可能走流式存储
       （`m_StreamData.path` 非空）⇒ 丢了流就是「有对象、没数据」。这里把它们列出来并保留。

    🔴 **A173：多 CAB 包的**三条口径**（2026-10-08 定；实跑与逐条实据 → `资料/普查产出_1008/波B1_工具三件.md`）
    1. **写哪一份 CAB = 装着 `AssetBundle` 对象的那一份**（**不是**「第一份」）。判据是代码级的：
       容器项/预加载项写的是 `PPtr(m_FileID=0, PathID)`（`_write_bundle`），而 `m_FileID == 0`
       = **引用方自己所在的那一份** ⇒ 只有把根收进 AssetBundle 那一份，`m_Container` 才指得对。
       （全库 84 个包普查：15 个多 CAB 包里 AB 对象**都**在第一份 —— 所以旧写法「取第一份」
        碰巧选对了**文件**，但 pid 命名空间是错的，见下一条。）
    2. **另一份怎么留 = 依赖树引用到的其它 CAB 整组保留 + 整组改名**。整组 = 那个 CAB 连同它的
       `.resS`/`.resource`（少留一条流 = 那个 CAB 里的贴图/网格「有对象没数据」）；改名是因为 Unity
       按**内层文件名**认「同一个包」，与仍加载着的源包撞名 ⇒ 产物整包被拒收（见 `_write_bundle` 那段实测）。
       整份留着**不裁剪**（裁剪只对「写的那一份」里的对象做）。
    3. **根不在 AssetBundle 那一份里** ⇒ 🔴 **A230（2026-10-13）起【支持】了**：走**跨 CAB 容器引用**
       （`m_FileID = sf.externals 下标 + 1`，缺条目就照 `_archive_path` 的原版写法补一条 + 出声），
       那一份 CAB **整组保留**（口径 2）。**改前**这里是**中止并点名**（再往前是静默坏包）。
       ⛔ **仍然不许「按 pid 在所有 CAB 里找」**：pid 在两份 CAB 里会相撞（实测 arena1：63 个 pid
       同时存在于两份；pid 1 在 `.sharedAssets` 里是 `PreloadData`、在主 CAB 里才是 `Embers (2)`）
       ⇒ 必须由 `m_FileID` 说清「在哪一份」，这也正是 A230 要补的那半边。
       ⚠️ **证据级别**：格式级规则 + 原版 `m_PreloadTable` 的先例（30+ 包有 `m_FileID ≠ 0`，但指的全是
       **别的包**）；**`m_Container` 侧全库 13448 条 fid 全是 0（本地无先例）** ⇒ Unity 侧**仍待验**
       （A230② / A231③ 那两条没销）。判据全文 → `资料/普查产出_1013/W230_跨CAB与repack.md`。

    🔴 **返回值**：`{(CAB 内层名, PathID): 对象}`（A173 起带 CAB —— 两份 CAB 的 pid 会撞号，用裸
    pid 当键会**静默丢对象**）。调用方只用来数个数（`len()`）。
    """
    env = UnityPy.load(src_path)
    bf = list(env.files.values())[0]
    # ---- ⓪ 🔴 A173：**写哪一份 CAB** = 装着 `AssetBundle` 对象的那一份（**不按 `bf.files` 的序**）----
    #  ⚠️ `bf.files` 的序是 **`.sharedAssets` 在前**（实测 arena1：`.sharedAssets` 63 个对象 / 主 CAB 5304 个）。
    #     旧写法 `next(…"SerializedFile")` = 取第一份，在全库 84 个包里「碰巧」总是 AssetBundle 那一份
    #     （2026-10-08 普查：15 个多 CAB 包，AB 对象**全在第一份**）⇒ 它选**文件**没选错、**pid 命名空间**
    #     选错了。实测拿 arena1 主 CAB 的根 `Embers (2)`（pid=1，GameObject）进旧写法：走树那句
    #     `sf.objects.get(1)` 在 `.sharedAssets` 里命中的是 **`PreloadData`**（同 pid、**不同类型**）⇒
    #     收进去的是错的对象、真根反而不在，而且**一个字都不报**（连 `[P2] 引用断了` 都不触发，因为 pid 存在）。
    #  ⇒ 显式挑「AB 对象那一份」，再由下面 ①·d（A230 起：不在这一份里就走**跨 CAB 容器引用**）定下来
    #     —— 两条合起来才不静默。
    #  判据：`资料/普查产出_1006/A152_pid陷阱普查.md` B11 · 实跑 → `资料/普查产出_1008/波B1_工具三件.md`。
    cab_sfs = [v for v in bf.files.values() if type(v).__name__ == "SerializedFile"]
    ab_owner = [(v, pid) for v in cab_sfs for pid, o in v.objects.items()
                if o.type.name == "AssetBundle"]
    if not ab_owner:
        print("!! 源包里没有 AssetBundle 对象 —— 打出来的包 Unity 会拒收，中止")
        return None
    if len(ab_owner) > 1:
        print(f"[P1] 🔴 这个包里有 {len(ab_owner)} 个 AssetBundle 对象（要求恰好 1 个）⇒ 中止："
              + " · ".join(f"pid {pid} @`{v.name}`" for v, pid in ab_owner)
              + "。`_write_bundle` 只重写其中一个的容器，**另一个会带着指向已删对象的旧容器留在包里**"
                "（静默坏包）⇒ 不猜，先人工定。")
        return None
    sf = ab_owner[0][0]
    if len(cab_sfs) > 1:
        print(f"[P1] ⓘ **多 CAB 包**：内层有 {len(cab_sfs)} 份 CAB（{[v.name for v in cab_sfs]}）；"
              f"AssetBundle 对象在 `{sf.name}` ⇒ **写这一份**"
              f"（容器/预加载写的是 `m_FileID=0`，只能指进这一份）")

    # ---- ① 按名字找根 GameObject ----
    # 🔴 **A230（2026-10-13）**：顺手记下每个根**在哪一份 CAB** 里（①·d 要用它算 `m_FileID`），
    #   并统计「同名 GameObject 落在**不同的** CAB 里」这种情形 —— 多份同名时 `found` 只能取
    #   **遍历到的第一个**（`env.objects` 的序 = 各内层文件依次）⇒ 发现多份就出声（⛔ 不许静默挑）。
    found, found_cab, dup = {}, {}, {}
    want = set(names)
    for o in env.objects:
        if o.type.name != "GameObject":
            continue
        try:
            d = o.read()
        except Exception:
            continue
        n = _field(d, "m_Name")
        if n not in want:
            continue
        cab = getattr(getattr(o, "assets_file", None), "name", None)
        dup.setdefault(n, set()).add(cab)
        if n not in found:
            found[n] = o
            found_cab[n] = cab
    for n in names:
        if n not in found:
            print(f"[P1] 🔴 这个包里**没有** GameObject `{n}`")
    print(f"[P1] 找到 {len(found)}/{len(names)} 个根："
          + " · ".join(f"{n}(PathID {o.path_id}"
                       + (f" @`{found_cab.get(n)}`" if len(cab_sfs) > 1 else "")
                       + ")" for n, o in found.items()))
    for n, cabs in sorted(dup.items()):
        if len(cabs) > 1:
            print(f"[P1] 🔴 **同名根 `{n}` 落在 {len(cabs)} 份不同的 CAB 里**：{sorted(cabs)} ⇒ "
                  f"取的是 `{found_cab.get(n)}` 那一份（靠 `env.objects` 的序）—— ⛔ 不静默挑，"
                  f"要另一份请改名或另行指定")

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
    # 🔴 `extra_clip_guids`（🆕 2026-10-07 A192）：`{clip 名: assetGUID}` —— 给片段**再登记一条
    #   按 GUID 的容器别名**。消费方 `ScenarioAnimationBlend` 手里只有一个 assetGUID
    #   （`AssetReferenceTyped<AnimationClip>`），而**原版源包的容器键就是 GUID** ⇒ 照原样登记，
    #   运行时 `LoadAsset<AnimationClip>(guid)` 与原版同一条路，映射不必在 C# 里再抄一份。
    #   名字→GUID 的出处 = `animator_controllers.json` 的 `clipsByGuid`（见 `clips_by_guid`）。
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

    # ---- ①·d 🔴 A230（2026-10-13）：根落在**别的 CAB** 里 ⇒ **支持**（改前是中止点名 + 更早是静默坏包）----
    #  为什么原来只能中止：容器项/预加载项当时只会写 `PPtr(m_FileID=0, PathID)`，而 `m_FileID == 0`
    #  = **引用方自己所在的那一份** ⇒ 指不进别的 CAB（见 `_write_bundle`）。A230 把这一条补齐了：
    #  `_ext_fid(sf, 根所在的那份 CAB)` 给出 `m_FileID`（= `sf.externals` 的下标 + 1；本文件没有
    #  指得到那份的条目时，按 `_archive_path` 的原版写法**补一条**并出声）⇒ 容器项指得对了。
    #  ⚠️ **仍然不许「按 pid 在所有 CAB 里找」**：pid 在两份 CAB 之间会撞号（arena1 实测 63 个 ——
    #     例 pid 1 在 `.sharedAssets` 里是 `PreloadData`、在主 CAB 里才是 `Embers (2)`）⇒ 必须显式记
    #     「这个根在哪一份里」，由 `m_FileID` 说清。这正是 A230 缺的那半边。
    #  ⚠️ 那一份 CAB 必须**整组留下**（交给 ③·a 收口）：根在它里面，根的对象和依赖都得在。
    #  ⚠️ 证据级别：`m_FileID ≠ 0` 在**原版**的 `m_PreloadTable` 里遍布全库（30+ 包），但指的都是
    #     **别的包**；**`m_Container` 侧全库 13448 条 fid 全 0**（本地无先例）⇒ Unity 侧仍待验（A230②）。
    cross_roots, cross_files, unresolved = {}, set(), []
    for n, o in sorted(found.items()):
        af = getattr(o, "assets_file", None)
        if af is sf:
            continue
        cab = getattr(af, "name", None)
        if not cab:
            unresolved.append((n, None, f"PathID {o.path_id}", "取不到根的 `assets_file.name`"))
            continue
        fid, how = _ext_fid(sf, cab)
        if fid is None:
            unresolved.append((n, cab, f"PathID {o.path_id}", how))
            continue
        cross_roots[n] = (cab, fid)
        for k in bf.files.keys():
            if _cab_group(k) == _cab_group(cab):
                cross_files.add(k)
    if cross_roots:
        clash = 0
        for cab, _fid in cross_roots.values():
            other = next((v for v in cab_sfs if v.name == cab), None)
            if other is not None:
                clash += len(set(sf.objects.keys()) & set(other.objects.keys()))
        print(f"[P1] 🔴 **{len(cross_roots)} 个根在【别的 CAB】里** ⇒ 容器/预加载项走**跨 CAB 引用**"
              f"（`m_FileID ≠ 0`，A230 那条路 —— ⛔ 不是「按 pid 在所有 CAB 里找」）：")
        print(f"       要写的那一份（AssetBundle 对象所在）= `{sf.name}`")
        for n, (cab, fid) in sorted(cross_roots.items()):
            print(f"       根 `{n}`(PathID {found[n].path_id}) 在 = `{cab}` ⇒ 容器项 `m_FileID={fid}`（{how}）")
        print(f"       ⇒ 那一份 CAB **整组保留进产物**：{sorted(cross_files)}"
              + (f"（两份 CAB 有 **{clash} 个 pid 撞号** ⇒ 所以非带 `m_FileID` 不可）" if clash else ""))
        print(f"       ⚠️ 体积口径：那份**整份不裁剪**（A173 口径 2）⇒ 产物会明显变大"
              f"（实测数字见 `资料/普查产出_1013/W230_跨CAB与repack.md`）")
    if unresolved:
        print("[P1] 🔴 **有根在别的 CAB 里、而跨 CAB 容器引用登记不出来** ⇒ 中止（⛔ 不退而求其次）：")
        print(f"       要写的那一份（AssetBundle 对象所在）= `{sf.name}`")
        for n, cab, pidtxt, why in unresolved:
            print(f"       根 `{n}`({pidtxt}) 在 `{cab}`：{why}")
        print("       为什么不能将就：容器/预加载是 `PPtr(m_FileID, PathID)`，`m_FileID = 0` = **引用方"
              "自己那一份**的 pid 空间 ⇒ 不指进它那份就必指错（同 pid 的另一个对象、一个字都不报）。")
        return None

    # ---- ② 递归收内部依赖树 ----
    # 🔴 **A173**：队列元素是 **(`哪一份 CAB` 的 SerializedFile, pid)**，不是裸 pid —— 两份 CAB 各有
    #   一套 pid 命名空间（arena1 实测 63 个 pid 撞号）；`m_FileID == 0` 的语义是**引用方自己所在的那一份**
    #   （实测 arena1：抽样里 fid0 命中自己 432 条、跨份 0 条；UnityPy `PPtr.deref` 也是这么解的）
    #   ⇒ 查表必须带上宿主，否则就是 A173 那个「静默取到同 pid 的另一个对象」。
    kept, by_type, ext = {}, collections.Counter(), collections.Counter()
    per_cab = collections.Counter()          # 依赖树落在各份 CAB 里的对象数（出声用）
    # 🔴 2026-10-07（A161 ④）：CAB 名 → 它那个 SerializedFile —— `_ext_name` 解 `m_FileID` 时必须用
    #   **发出引用的那个对象自己那份**（双 CAB 包的两份 `externals` 差 17/21 条，拿错就把包名说错）。
    sf_by_cab = {}
    streamed, seen = [], set()
    # 🔴 **A230（2026-10-13）修**：队列的起点必须是**根自己所在的那一份**，不能一律用 `sf`
    #   —— 根落在别的 CAB 时（①·d 现在支持的那种），`sf.objects.get(pid)` 取到的是**同 pid 的另一个
    #   对象**（arena1：`Embers (2)` pid=1 在主 CAB，而 `.sharedAssets` 里 pid=1 是 `PreloadData`）
    #   ⇒ 收错对象、而且一个字都不报。改前这里恒为 `sf`：那时根只可能在 `sf` 里，所以看不出来。
    queue = [((getattr(o, "assets_file", None) or sf), o.path_id) for o in found.values()]
    while queue:
        host, pid = queue.pop()
        if (host.name, pid) in seen:
            continue
        seen.add((host.name, pid))
        o = host.objects.get(pid)
        if o is None:
            print(f"[P2] ⚠️ PathID {pid} 不在 `{host.name}` 里（引用断了）")
            continue
        kept[(host.name, pid)] = o
        by_type[o.type.name] += 1
        per_cab[host.name] += 1
        af = getattr(o, "assets_file", None) or host
        sf_by_cab.setdefault(getattr(af, "name", None), af)
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
                queue.append((af, rpid))     # 🔴 A173：同份引用 = **引用方自己那一份**
            else:
                # 键里带**宿主那份 CAB** —— 否则 `[P3]` 只能拿「第一个 SerializedFile」去解 fid（A161 ④）
                ext[(fid, o.type.name, getattr(af, "name", None))] += 1

    # ---- ③ 外部引用：同包内层 CAB 要保留，真外部只报告 ----
    #  ⚠️ `bf.files` 的键可能是 `CAB-xxx` / `CAB-xxx.resS` / 也可能是带 `archive:/…` 的整串
    #     ⇒ **全路径与文件名两种都比一遍，且不分大小写**（2026-10-01 第一版只比 basename 就漏了）。
    keys_lower = {k.lower(): k for k in bf.files}
    # 🔴 **A230**：①·d 收下的「跨 CAB 根」所在的那几组内层文件**必须留** —— 根就在里面。
    #   （放进 `keep_files` 里跟「依赖树引用到的别的 CAB」走同一条路：③·a 整组补齐 + 整组改名。）
    keep_files, real_ext = set(cross_files), []
    for (fid, t, cab), cnt in sorted(ext.items(), key=lambda kv: -kv[1]):
        # 🔴 A161 ④：按**宿主自己那份** SerializedFile 解 fid（`sf_by_cab` 在走树时攒的）；
        #    取不到才退回 `sf` —— 而退回这件事本身要出声（两份 CAB 的 externals 不同）。
        sf_host = sf_by_cab.get(cab)
        if sf_host is None and cab is not None:
            print(f"[P3] ⚠️ 找不到 CAB `{cab}` 对应的 SerializedFile ⇒ 这条引用退回按 "
                  f"`{getattr(sf, 'name', '?')}` 解（包名可能说错）")
        p = _ext_name(sf_host or sf, fid)
        hit = keys_lower.get(p.lower()) or keys_lower.get(os.path.basename(p).lower())
        if hit:
            keep_files.add(hit)
        else:
            real_ext.append((t, cnt, p))

    print(f"[P3] 依赖树：{len(kept)} 个对象"
          + " · ".join(f"{t}×{c}" for t, c in by_type.most_common()))
    if len(cab_sfs) > 1:
        print("[P3] ⓘ 依赖树按 CAB 分布："
              + " · ".join(f"`{c}` {n} 个" for c, n in per_cab.items())
              + f"（**要写的那一份** = `{sf.name}`）")
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

    # ---- ③·a 🔴 A173 **另一份怎么留**：`keep_files` 命中的内层文件若**不属于「要写的那一份」**，
    #   就把**整组**（该 CAB + 它的 `.resS`/`.resource`）都留下 + 后面整组改名。
    #   为什么不是「只留命中的那个文件」：少留一条流 = 那个 CAB 里的贴图/网格「有对象、没数据」；
    #   为什么必须改名：Unity 按**内层文件名**认「同一个包」，与仍加载着的源包撞名 ⇒ 产物**整包被拒收**
    #   （实测见 `_write_bundle` 那段）。旧代码对这种情况只打一句「保持原名」= **留着这个雷**。
    #  ⚠️ **位置**：放在「流」和「真外部引用」两段**都跑完之后** —— 那两段也会往 `keep_files` 里加文件
    #     （对象引用走 `ext`、流走 `m_StreamData.path`）⇒ 早算会漏掉「流带出来的那一组」，
    #     半留一组 = 有对象没数据（而它一个字都不报）。
    old_main_key = next(k for k, v in bf.files.items() if v is sf)
    extra_groups = sorted({_cab_group(k) for k in keep_files
                           if _cab_group(k) != _cab_group(old_main_key)})
    for g in extra_groups:
        for k in list(bf.files.keys()):
            if _cab_group(k) == g:
                keep_files.add(k)
    # 整组保留的那几份 CAB 里的对象，后面也要跟着改 `m_StreamData.path`（它们的流也改名了）
    extra_sfs = [v for k, v in bf.files.items()
                 if _cab_group(k) in extra_groups and type(v).__name__ == "SerializedFile"]
    if extra_groups:
        print(f"[P3] 🔴 **多 CAB：另一份也得跟着走** —— 依赖树引用到 {extra_groups} ⇒ "
              f"**整组**保留（含它的 `.resS`/`.resource`）+ **整组改名**"
              f"（沿用源包的内层名 = 与源包撞名 = Unity 判「同一个包」把产物整包拒收）")

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
    #  🔴 **2026-10-08（A173）**：上面这几条**每一组**内层文件都要做 —— 主组改成 `new_base`，
    #     依赖树引用到的**别组**改成 `new_base_1/2/…`；引用它们的 `externals[].path` 尾巴、
    #     以及那些 CAB 里对象的 `m_StreamData.path` 都要跟着改（原来只改主组，别组「保持原名」
    #     = 留着「与源包撞名 ⇒ 整包被拒收」这个雷）。
    #  ⚠️ 只在本模式做：`--arenas`/`--builtin` 那些产物是**运行时**加载的（进程里没有源包），
    #     撞不上；而且它们靠 `HarvestFromLoadedBundles` 那条兜底已经能活。
    #  📌 **同一条规矩的另外两处**（要改一起看）：`工具/extract_mirror_shaders.py` 的 `[6b]`
    #     （它的产物与壳包撞名 ⇒ 那个包整包加载失败、32 份材质退回 `URP/Unlit`）·
    #     `资料/已知的坑.md` 的「随包 bundle 里的东西取不到 ⇒ 先怀疑包整包没加载成功」那条。
    old_base = old_main_key
    # 🆕 2026-10-03：改名基名**可传参**了（原来硬编码 `CAB-wfprefabsextra`）——
    #   同一个源包会有**多个产物**（`--prefabs` 现在要打战场那件 + 卡包那批），
    #   而 Unity 按**内层文件（CAB）名**认「这是同一个包」⇒ 两个产物同名会被拒收（见上面那段）。
    new_base = cab_name or "CAB-wfprefabsextra"
    extra_rename = {g: f"{new_base}_{i}" for i, g in enumerate(extra_groups, 1)}
    rename = {}
    for k in [old_base] + sorted(keep_files):
        g = _cab_group(k)
        new_g = new_base if g == _cab_group(old_base) else extra_rename.get(g)
        if new_g is None:                                  # 理论上到不了：keep_files 里的组都该有名字
            print(f"[P3] 🔴 内层文件 `{k}`（组 `{g}`）没有改名映射 ⇒ 保持原名"
                  f"（**与源包内层名撞名就会被 Unity 拒收整包**）")
            new_g = g
        rename[k] = new_g if k == g else new_g + k[len(g):]
    # 改完之后**任何一个内层名都不该还等于源包里的名字**（撞名 = 产物整包被拒收，见上面那段实测）
    clash = sorted(set(rename.values()) & set(rename.keys()))
    if clash:
        print(f"[P3] 🔴 有内层文件改名后**与源包同名**：{clash} ⇒ 产物与源包同时加载时 Unity 会"
              f"判「同一个包」**整包拒收**；`cab_name` 别取源包的内层名。")
    if any(a != b for a, b in rename.items()):
        # ⚠️ **先把内层文件的键改掉**再做后面那些按名字找流的活（下面的 `key` 用的是新名）
        for a, b in rename.items():
            bf.files[b] = bf.files.pop(a)

        # ---- ③·b·1 🔴 A173：把**引用方**的 `externals[].path` 换成新内层名 ----
        #  为什么必须做：跨 CAB 引用（fid ≠ 0）靠 `externals[fid-1].path` 解，路径里就写着内层文件名
        #  （实测 arena1 主 CAB 的 externals 里那条 `archive:/CAB-8adf…/CAB-8adf….sharedAssets`）
        #  ⇒ 内层文件改了名而 externals 不改 = **引用断在运行时**（UnityPy `PPtr.deref` 就是按这个名字找）。
        #  ⚠️ 只改指向**本包内层文件**的那些；指向别的包的一律不动（那是「真外部引用」）。
        n_ref = 0
        for k, v in bf.files.items():
            if type(v).__name__ != "SerializedFile":
                continue
            for e in getattr(v, "externals", []) or []:
                path = getattr(e, "path", None)
                if path is None and isinstance(e, (list, tuple)) and len(e) >= 2:
                    path = e[1]
                if not isinstance(path, str) or not path:
                    continue
                newp, changed = _rename_stream_path(path, rename)
                if not changed:
                    continue
                if hasattr(e, "path") and getattr(e, "path", None) is not None:
                    e.path = newp
                elif isinstance(e, (list, tuple)) and len(e) >= 2:
                    e[1] = newp
                else:
                    e["path"] = newp
                n_ref += 1
        if n_ref:
            print(f"[P3] `externals` 里指向**自己包内层文件**的引用改了 {n_ref} 条（内层文件改名了）")

        # 先把「要改 path/offset 的对象」全读出来（**读一次、写一次**，别来回 save）
        # 🔴 A173：对象来自两处 —— ① 依赖树里那些（`kept`）② **整份保留的别的 CAB 里的全部对象**
        #   （②是新增：那些 CAB 是整份搬进产物的，它们的流也跟着改了名 ⇒ 不跟着改就是「有对象、没数据」，
        #    而这一路**原来根本没人管**）。
        objs_to_fix = list(kept.values())
        for v in extra_sfs:
            objs_to_fix += list(v.objects.values())
        entries = []          # [(o, typetree, m_StreamData, 旧path, 新path, offset, size)]
        for o in objs_to_fix:
            try:
                d = o.read_typetree()
            except Exception:
                try:
                    d = o.read()
                except Exception:
                    continue
            sd = _field(d, "m_StreamData")
            p = _field(sd, "path") if sd is not None else None
            if not p:
                continue
            # 按**这条流的宿主内层文件**换名（多 CAB 时不能一律换成 `new_base` —— 见 `_rename_stream_path`）
            newp, changed = _rename_stream_path(p, rename)
            if not changed:
                continue
            entries.append([o, d, sd, p, newp, _field(sd, "offset", 0) or 0, _field(sd, "size", 0) or 0])

        # ---- 🔴 ③·c 资源流瘦身：只留**用到的字节区间** ----
        #  为什么：`.resS` 是**整个 CAB 的公共流**（实测 211 MB 原始 / lz4 后 44 MB），
        #  而我们那 6 张纹理合起来只占 **约 224 KB** ⇒ 不裁的话产物 45 MB、白白进包。
        #  做法：按区间切片、**每段 16 字节对齐**（贴图数据惯例，避免未对齐读取）、重算 offset。
        #  ⚠️ 只有「引用同一条流的对象我们**全留着**」时才成立 —— 这里正是（其余对象都被丢了）。
        trimmed = {}
        for os_old in {os.path.basename(e[3]) for e in entries}:
            if _cab_group(os_old) in extra_groups:
                # 🔴 A173：这条流属于**整份保留**的那组内层文件 —— 那组里的对象**一个都没被丢**，
                #   所以「用到的区间我们全留着」这个瘦身前提**不成立** ⇒ 整条留、offset 也不动。
                print(f"[P3] ⚠️ 流 `{os_old}` 属于整份保留的那组内层文件 ⇒ **不瘦身**（整条留）")
                continue
            idx = [i for i, e in enumerate(entries) if os.path.basename(e[3]) == os_old]
            key = rename.get(os_old, os_old)
            if key not in bf.files or not hasattr(bf.files[key], "bytes"):
                print(f"[P3] ⚠️ 流 `{os_old}` 取不到字节，跳过瘦身（保留整条）")
                continue
            raw = bf.files[key].bytes
            buf = bytearray()
            for i in sorted(idx, key=lambda i: entries[i][5]):
                while len(buf) % 16:
                    buf.append(0)
                trimmed[i] = len(buf)
                buf += raw[entries[i][5]:entries[i][5] + entries[i][6]]
            from UnityPy.streams import EndianBinaryWriter
            w = EndianBinaryWriter()
            w.write_bytes(bytes(buf))
            # ⚠️ `bf.save()` 会读每个内层文件的 `flags`（流式标志），换掉的写器要**继承原值**
            w.flags = getattr(bf.files[key], "flags", 0)
            bf.files[key] = w
            print(f"[P3] 资源流瘦身 `{key}`：{len(raw)/1024:.0f} KB → {len(buf)/1024:.0f} KB"
                  f"（{len(idx)} 个对象用到的区间）")

        # ---- 一次写完：path 换成**新内层名**（按流的宿主挑，见 `_rename_stream_path`）+ offset 换成瘦身后的位置 ----
        n = 0
        for i, (o, d, sd, p, newp, off, size) in enumerate(entries):
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
    #  ⚠️ A173：裁剪**只裁「要写的那一份」**（`sf`）—— 别的份是**整份**搬进产物的（见 ③·a），
    #     它的对象一个都不删，它内部那些引用才还成立。
    ab_reader, dropped = None, 0
    for pid, o in list(sf.objects.items()):
        if o.type.name == "AssetBundle":
            if ab_reader is None:
                ab_reader = o
            continue
        if (sf.name, pid) in kept:          # 🔴 A173：键带 CAB（裸 pid 在两份 CAB 之间会撞号）
            continue
        del sf.objects[pid]
        dropped += 1
    if len(cab_sfs) == 1:
        print(f"[P4] 保留 {len(kept)} 个对象（丢弃 {dropped} 个），AssetBundle 对象={ab_reader is not None}")
    else:
        print(f"[P4] 保留 {len(kept)} 个对象（要写的那一份 `{sf.name}` 里 {per_cab[sf.name]} 个 / 别的份 "
              f"{len(kept) - per_cab[sf.name]} 个；写的那一份里丢弃 {dropped} 个），"
              f"AssetBundle 对象={ab_reader is not None}")
        if extra_groups:
            print(f"[P4] ⓘ 另 {len(extra_groups)} 组内层文件（{extra_groups}）**整份**进了产物"
                  f"（整组不裁剪 —— A173 的口径：多 CAB 时「另一份」整份留）")
    if ab_reader is None:
        print("!! **要写的那一份**里没有 AssetBundle 对象 —— 打出来的包 Unity 会拒收，中止"
              "（正常到不了这里：⓪ 已经按「AB 对象在哪份」挑过了）")
        return None

    # ---- ⑤ 容器登记：**裸名 + 小写路径**两种写法都登记（`LoadAsset(name)` 两种都可能被调）----
    # 🆕 2026-10-01：后缀按**对象类型**给（原来一律 `.prefab`）—— 材质根要 `.mat`，
    #   否则第二个别名是 `assets/xxx.prefab` 指着一个 Material，看着就是错的。
    #   🆕 同一晚补 `AnimationClip` → `.anim`（`extra_clips` 那条路进来的）。
    # 🔴 **A230（2026-10-13）**：键从裸 `path_id` 改成 `(m_FileID, path_id)` —— 根在**别的 CAB** 里时
    #   `m_FileID ≠ 0`（`cross_roots` 在 ①·d 算好了 = `sf.externals` 下标 + 1）。
    def _alias_ext(o):
        return {"Material": ".mat", "Shader": ".shader", "AnimationClip": ".anim",
                "AnimatorController": ".controller"}.get(o.type.name, ".prefab")
    entries = {}
    for n, o in sorted(found.items()):
        aliases = [n, "assets/" + n.lower() + _alias_ext(o)]
        # 🆕 2026-10-07（A192）：**assetGUID 也是别名** —— 原版源包的容器键就是 GUID
        #   （实测 988 条键全是十六进制 GUID），而消费方 `ScenarioAnimationBlend` 手里只有 GUID
        #   ⇒ 照原样登记，运行时按 GUID 取值 = 原版那条路。
        g = (extra_clip_guids or {}).get(n)
        if g and g not in aliases:
            aliases.append(g)
        fid = cross_roots[n][1] if n in cross_roots else 0
        entries[(fid, o.path_id)] = aliases
    if extra_clip_guids:
        print(f"[P4] 按 assetGUID 登记的容器别名 {len(extra_clip_guids)} 条："
              + " · ".join(f"{g} → `{n}`" for n, g in sorted(extra_clip_guids.items())))
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
    🆕 2026-10-07（A192）：① 里除了「控制器要用的 clip」，还收**只被环境 SO 按 GUID 引用**的两条
      （表里 `clipsByGuid` 那一节，见 `clips_by_guid`），并按 GUID 登记容器别名。
    """
    want = args.out or DEST_PREFABS
    print(f"[P0] 源包 {os.path.basename(SRC_BUNDLE)} → {want}")
    # 这个包里那些控制器要用的动画片段 —— 名字从数据表来，不手写（见 `controller_clip_names`）
    src_dir = "bundle_" + os.path.basename(SRC_BUNDLE)[:-len(".bundle")]
    clips = controller_clip_names(src_dir)
    # 🆕 2026-10-07（A192）：外加**只被环境 SO 按 GUID 引用**的那两条（同一张表的另一节）
    guid_clips = clips_by_guid(src_dir)
    clip_guids = {}
    for n, g in guid_clips:
        if n not in clips:
            clips.append(n)
        if g:
            clip_guids[n] = g
    print(f"[P0] 控制器数据表里「源包 = {src_dir}」："
          f"AnimationClip {clips} · AnimatorController {controller_names(src_dir)}")
    if guid_clips:
        print(f"[P0] 其中**按 assetGUID 引用**（表里的 `clipsByGuid` 那一节）{len(guid_clips)} 条："
              + " · ".join(f"{g} → `{n}`" for n, g in guid_clips))
    kept = repack_tree(SRC_BUNDLE, want, PREFAB_TARGETS, dry_run=args.check,
                       extra_materials=MATERIAL_TARGETS, extra_clips=clips,
                       extra_controllers=controller_names(src_dir),
                       extra_clip_guids=clip_guids)
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
