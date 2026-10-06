#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_env_blendables.py —— 把「环境混合组件」（blendable）抽成旁挂 JSON。

为什么要它（判据 → `项目任务.md` §三 第 30 条「4 环境」· `资料/加时与冲突模式_原版规格.md`）：
  原版换环境那条链是 `ApplyEnvironment` → `EnableEnvironment`：
    ① 雾/环境光补间
    ② `scenarioObjects` prefab 实例化 → 对它子树里每个 `IScenarioEnvironmentBlendeable` 调 `DoScenarioBlend(opts)`
    ③ 再对【场景里已登记的那些】调一遍（方向 = `SO.defaultScenarioObjectsState`）
  这族组件原版有 **9 个实现类**（我们的 `CLASSES` 现在**全收了**）：
  · 2026-10-06 战-A 收的 5 个（`ScenarioParticleSystemBlender` 62 · `…Toggler` 36 · `…MaterialFader` 23 ·
    `…ParticleSpawnerBlender` 4 · `…GenericObjectToggler` 3）
  · 🆕 2026-10-07 波9离线（A136）收的另 4 个（`FlareScenarioToggler` 6 · `ScenarioAnimationBlend` 2 ·
    `ScenarioGenericMaterialBlend` 1 · `TauCannonAnimationStopper` 2，**11 个实例、全在场景侧**）
  ⇒ 合计 **139**（= 128 + 11）。

🔴 **按类名分辨，不按字段猜**：按「有 `particleSystems` 就算 Toggler」会命中 `AnimFXModule*` ——
   实测 `battleprefabs_vfxandmisc` 里 **314 个假阳性**。类名来自 `Waprforge_monoscripts` 的 `m_ClassName`。

产物：`数据/游戏数据/env_blendables.json`
  { "_schema", "_sources", "prefabs": { "<prefab 根名>": [...] }, "scene": { "<场>": [...] }, "stats", "_unresolved" }
  每条 = { "cls", "owner", "ownerLeaf", ["ownerPos"], "fields", "targets": [{path/name, leaf, kind, [pos]}] }
  · **prefab 侧**：`path` = 相对 prefab 根的层级路径（我们导入的 prefab 保层级 ⇒ 运行时按路径找）
  · **场景侧**：我们的战场是 `ArenaBuilder` 按清单【平铺】建的（没有父链）⇒ 给 **名字 + 世界位置**，
    运行时按「同名 + 最近」匹配（与 `ArenaBuilder.ApplyShadowSidecar` 同一套做法）

📌 **记号：本文件里 `<…>` 一律是「占位符」记号、不是 XML 标签**（`<i>` = 0 基下标 · `<层>` = `sounds`/`exitSounds` ·
   `<键>` = 一条摊平键 · `<点号键>` = 模块自己的字段名）。Python 文档串不受 XML 解析约束，所以这里一直这么写。
   ⛔ **但别把这套记号抄进 C# 的 `///` XML 注释** —— 裸尖括号会被当标签、顶出 **CS1570**
   （2026-10-16 就是这么发生过一次；当天 C# 那 6 条已清零）。C# 侧现在的写法是**中文**：
   `modules.第 i 个模块.cameraShakes[j].*` —— 见 `WarpforgeVFX/Runtime/WFModuleScreenShake.cs:216,221` 与
   `WFSceneModuleScreenShake.cs:69,72`，那两处各带一句防回退说明。
   ⚠️ **产物里那段 `_schema`（`main()` 里，约 `:1592` 起）也带这套记号 —— 有意保留**：它是**写进
   `数据/游戏数据/env_blendables.json` 的字面量**，改了就等于改产物（盘上那份得重跑生成器才同步）⇒ ⛔ 别单独动它。

判据来源（原始 bundle，不用解包 JSON —— 解包出来的 GameObject 是按名命名的，**pid 丢了、解不了 PPtr**）：
  · prefab 侧 `<AA>/battleprefabs_vfxandmisc_assets_all.bundle`
  · 场景侧 `<AA>/scenes_scenes_<场>.bundle`（13 个）
  · 类名表 `<AA>/Waprforge_monoscripts.bundle`
  · `AA` = `d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64`

自检（写盘前跑，结果进 `_unresolved`）：
  · prefab 侧：每个 prefab 名在我们 `MyGame/Assets/WarpforgeVFX/Prefabs/<名>.prefab` 里在不在；
    每个目标 `leaf` 在该 `.prefab` 文本里有没有 `m_Name: <leaf>`
  · 场景侧：owner/target 的名字在该场 `battlearena<场>_manifest.json` 的 `go` 名字表里在不在
  · 🆕 2026-10-07（A135④）**材质补间那一层**（只有 `ScenarioGenericMaterialBlend` 有）两条：
    ① 每个 `blendProps` 名字都必须在**那件原版 Material** 的序列化属性表里（判据 = bundle 里的 Material 资产）；
    ② 每个 `blendProps` 名字也必须在**我们这场 arena 清单**里那个网格的 `subMats[].props[].k` 里
       —— 即「我们重建的那份材质真的带这个属性」（`_Blend` 就是这么核的）。

用法：PYTHONIOENCODING=utf-8 python 工具/gen_env_blendables.py            # 写盘
      PYTHONIOENCODING=utf-8 python 工具/gen_env_blendables.py --check    # 只扫不写
"""
import argparse
import io
import os
import json
import re
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
import UnityPy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_arena_negscale as N            # 复用同一套矩阵算式（判据只留一处）
# 🆕 2026-10-12（A393）：场景侧那 5 条要建出来，得知道「宿主我们有没有」「哪些子件要改挂回去」——
#   那两件事的判据**已经在 `gen_arena_groups.py` 里**（同一个原版包读法 + 同一套 no-scale 世界链 +
#   同一套 `ArenaBuilder` 闸门对读）⇒ 直接复用那个模块（本仓铁律 6：同一条规则别写两遍）。
#   ⚠️ 它 import 时不跑 `main()`（有 `if __name__ == '__main__'` 守卫）。
import gen_arena_groups as G

ROOT = 'd:/4/Unity'
AA = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
PREFABS = os.path.join(ROOT, 'MyGame/Assets/WarpforgeVFX/Prefabs')
ARENAS = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas')
ARENA_PREFABS = os.path.join(ROOT, 'MyGame/Assets/Resources/ArenaPrefabs')
OUT = os.path.join(ROOT, '数据/游戏数据/env_blendables.json')
FLAT = os.path.join(ROOT, 'MyGame/Assets/Resources/EnvBlendables.json')

PREFAB_BUNDLE = 'battleprefabs_vfxandmisc_assets_all.bundle'
MONO_BUNDLE = 'Waprforge_monoscripts.bundle'
ALL_ARENAS = N.ALL_ARENAS

# 类 → (目标字段列表, 该字段的目标类型标签, 要一并记下的字段)
CLASSES = {
    'ScenarioParticleSystemBlender': ([('particleSystems', 'ps')], ['forceEnableEmittersOnEnable', 'notifyFinishWhenNoParticles']),
    'ScenarioParticleSystemToggler': ([('particleSystems', 'ps')], ['priority']),
    'ScenarioMaterialFader':         ([('renderers', 'renderer')],
                                      ['fadeOnEnable', 'isInDefaultScenario', 'restoreOriginalMaterialsOnFadeOut',
                                       'restoreOriginalMaterialsOnFadeIN', 'disableOnFadeOut']),
    'ScenarioGenericObjectToggler':  ([('gameObjects', 'go')], ['isInDefaultScenario']),
    'ScenarioParticleSpawnerBlender': ([('areaSpawners', 'spawner'), ('controllers', 'controller')], []),
    # 🆕 2026-10-07 波9离线（A136）：原版 `IScenarioEnvironmentBlendeable` 的**另 4 个实现类**
    #   —— 共 **11 个实例**，**全部在场景侧**（逐 bundle 数过：`资料/普查产出_1006/战A_第3_4条.md`「顺手发现 A」）。
    #   类名 / 字段名 / 偏移的判据 = `d:/2/tools/il2cpp_out/dump.cs`（各 TypeDefIndex 写在文件末尾那段注释里）；
    #   方法体逐句读过（件名写在 `Battle/ScenarioBlendables.cs` 各方法的注释里）。
    'FlareScenarioToggler':          ([('lensFlare', 'flare', 'single')], []),
    'ScenarioAnimationBlend':        ([('myAnimation', 'animation', 'single')], ['autoPlayWhenChange']),
    'TauCannonAnimationStopper':     ([('lookAtConstrains', 'lookat'),
                                       ('animationComponent', 'animation', 'single'),
                                       ('cannon', 'transform', 'single'),
                                       ('particleSystems', 'ps'),
                                       ('animFXController', 'animfx', 'single')], []),
    # ⚠️ `ScenarioGenericMaterialBlend` 的目标藏在**嵌套 `[Serializable]` 结构体数组**里
    #   （`RendererMaterialBlender[] renderersBlend`）⇒ 上面那条通用路解不了，走 `NESTED_TARGETS`。
    'ScenarioGenericMaterialBlend':  ([], ['fadeOnEnable', 'isInDefaultScenario']),
}

# 🆕 2026-10-07 波9离线（A136）：**该组件自己**的小数字段 → 走 `Item.floats`。
#   为什么不并进上面那个 `extra`（那个写 `Item.fields`）：那套的 `v` 是 **int**
#   —— `blendTime = 0.3` 会被截成 `0`（**静默错**，本仓红线），`finalRotation` 是 Vector3 更装不下。
#   Vector3 会拆成 `finalRotation.x/y/z` 三条（与 `TARGET_FIELDS` 的 `boxSize` 同一套写法）。
FLOAT_FIELDS = {
    'ScenarioAnimationBlend':       ['blendTime', 'filterCode'],
    'TauCannonAnimationStopper':    ['finalRotation'],   # 原版 +0x40（Vector3）
    'ScenarioGenericMaterialBlend': ['filterCode'],
}

# 🆕 2026-10-07 波9离线（A135④）：目标藏在**嵌套结构体数组**里的类。
#   `ScenarioGenericMaterialBlend.renderersBlend[]` 每条 =
#     `{renderer(PPtr), customMaterial(PPtr), propertiesToBlend(string[])}`
#   —— 数组元素**不是 PPtr**（没有 `m_PathID`）、外面还包着一层对象引用，
#   所以 `collect()` 那条「`for t in d[fname]: t['m_PathID']`」的通用路解不了它。
#   值 = (数组字段名, 元素里指目标的那个键, 元素里要带走的字符串数组键, 元素里的引用键)
NESTED_TARGETS = {
    'ScenarioGenericMaterialBlend': ('renderersBlend', 'renderer', 'propertiesToBlend', 'customMaterial'),
}

# 🆕 2026-10-06 战-A：**目标组件自己**的序列化字段 —— 只对这张表里的类收，随目标一起进旁挂
#   （落点 = `Target.fields`；字段名**照原版**，别改写）。
#   判据 = `d:/2/tools/il2cpp_out/dump.cs` 里那个类的字段表（名字 + 偏移 + 类型）：
#     `ParticleSystemAreaSpawner` TypeDefIndex 1104 —— boxSize(0x20 Vector3) · particleSystemPrefab(0x30) ·
#       maxPoolSize(0x38) · useAutomaticSpawn(0x3C) · spawnRate(0x40) · chances(0x44)（**恰好这 6 个序列化字段**，
#       `totalPoolItems`/`m_Pool`/`collectionChecks` 是运行时的，不在里面）
#     `ParticleSystemAreaSpawnerController` 1107 —— spawnRate(0x28) · startOnEnable(0x2C)
#       ⚠️ 它真正的主体是 `particleSystemAreaSpawners[]`（每条 = 引用 + weight + chances 的嵌套结构），
#       **这一层没收**（要收得再给 `Target` 加一层嵌套类型）—— 实测全库 0 个实例用到 controller（4 条 blender
#       的 `controllers` 全是空数组）⇒ 记在这里当已知缺口，别当「收全了」。
#   Vector3 会拆成 `boxSize.x/y/z` 三条；**对象引用**（`particleSystemPrefab`）写成 `s` = 落点的层级路径。
TARGET_FIELDS = {
    'ParticleSystemAreaSpawner': ['boxSize', 'particleSystemPrefab', 'maxPoolSize',
                                  'useAutomaticSpawn', 'spawnRate', 'chances'],
    'ParticleSystemAreaSpawnerController': ['spawnRate', 'startOnEnable'],
    # 🆕 2026-10-11（A196）：`TauCannonAnimationStopper` 那两种目标组件**自己**的序列化字段。
    #   为什么必须收（否则运行时不建）：`LookAtConstrainWIP.Update()` 在 `target == null` 时
    #   **每帧告警**、而且组件唯一的作用就是照着 `target` 转向 ⇒ 没有 `target` 就不该建；
    #   `AnimFXController` 更硬：默认 `preventDestroy = false` + `destroyTime = 4f` 会让 `OnEnable`
    #   **当场排定 4 秒后的自毁**，而原版 4 个实例**全是 `true`** ⇒ 拿不到 `preventDestroy` 就不该建。
    #   判据 = 签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{LookAtConstrainWIP,AnimFXController}.cs`
    #          ＋ `d:/2/tools/decomp_full/LookAtConstrainWIP__Update.c` / `AnimFXController__{OnEnable,ctor}.c`
    #          ＋ 场景 4+4 个实例（`bundle_scenes_scenes_battlearenatauviorla/MonoBehaviour/`）。
    #   `target` 是 PPtr ⇒ 走那句「对象引用 → `s` = 落点的层级路径」（`pack_fields` 里本来就有）。
    #   ✅ **2026-10-12（A340）`AnimFXController` 的 `sounds` / `exitSounds` / `modules` 三层【已收】**
    #      —— 原来这里写着「仍然没收」（它们是数组、元素还带外部资产引用，`pack_fields` 解不了）。
    #      现在另开了一个 packer（`pack_animfx_defs`，照 `pack_controller_defs` 那套摊平进 `TargetField`）；
    #      cue 那条**跨包**引用（`AudioCue` 在 `soundcollection_assets_all` 里）走 `CueNames`
    #      解成 cue 名（= `animfx_sounds.json` 的键）。下面这 3 个字段仍然是 `pack_fields` 那条路。
    'LookAtConstrainWIP': ['target', 'lockXAxis', 'lockYAxis', 'lockZAxis', 'upVector', 'rotationOffset'],
    'AnimFXController': ['preventDestroy', 'destroyTime', 'exitDestroyTime'],
}

# 🆕 2026-10-07 波9批二（A137）：**不被任何 blendable 引用**的 `ParticleSystemAreaSpawner` / `…Controller`。
#   全库实测 **28 + 3** 个实例（逐 bundle 数过，**全在** `battleprefabs_vfxandmisc`，场景侧 0 个），
#   其中被 `ScenarioParticleSpawnerBlender` 引用的只有 **4 个** —— 那 4 条走 `CLASSES` 那条路
#   （2026-10-06 战-A 做的），**这里必须排除**：同一个 pid 收两遍 ⇒ 运行时同一个对象上建两次组件。
#   ⇒ 本节收的是**剩下 24 条 spawner + 3 条 controller**，进旁挂的 `standalone` 一节，
#     运行时由 `EnvironmentApplier` 在环境 prefab 实例化之后**照建**（同一个 `MakeSpawner`）。
#   ⚠️ 那 24 条里只有 **16** 条 `useAutomaticSpawn = 1`（自启）；另 **8** 条 `= 0`，是**被那 3 个
#     controller 驱动**的（controller 直接调它们的 `SpawnParticle()`，不看 `useAutomaticSpawn`）
#     —— 「24 条自启」这个说法**不准确**，真值已在报告里逐条列了。
STANDALONE_CLASSES = ('ParticleSystemAreaSpawner', 'ParticleSystemAreaSpawnerController')

# 🆕 2026-10-12（A393）：**场景侧**、**不被任何 blendable 引用**的组件 —— 与上面那个 `standalone`
#   （A137，prefab 侧）是同一类缺口，只是这一族住在 13 个 `scenes_scenes_*` 包里。
#   为什么它们是「缺口」：原版这些组件只是场景里一个**普通 MonoBehaviour**（自己 `OnEnable`／靠帧循环），
#   既不归 blendable 管、也不归 `ParticleSystemAreaSpawner` 那族管 ⇒ 我们原来**连账都没有**。
#   全库实测（15 个 `scenes_scenes_*` 包全扫，判据 → `资料/普查产出_1012/W3_AnimFX旁挂.md` §七·1 与
#   本件报告 §二）：**7 个 `AnimFXController`**，其中 2 个（tauviorla 两个 `Railgun turret`）是
#   `TauCannonAnimationStopper.animFXController` 的目标 ⇒ 走 `CLASSES` 那条路（由 A340 建），
#   这里收的是**剩下 5 个**。
#   ⚠️ 排除是按 **pid 引用关系**做的（`Bundle.referenced_pids`），**不是**按名字/场次写死的名单。
SCENE_STANDALONE_CLASSES = ('AnimFXController',)

#   `…Controller` 的主体是 `particleSystemAreaSpawners[]`（每条 = 引用 + weight + chances 的嵌套结构）。
#   它不是 PPtr 数组（外面包着一层普通 `[Serializable]` 类）⇒ `pack_fields` 那条通用路解不了，
#   在 `pack_controller_defs` 里摊平：引用 → `spawner.<i>`（`s` = 落点的层级路径）·
#   `weight.<i>` / `chances.<i>`（`f`）。**用现成的 `TargetField` 形制装**：不新开一层嵌套类型
#   （`Core/EnvBlendables.cs` 那个类型不在本件白名单里；键名带下标、运行时按 0..N-1 顺序读、读不到就停）。
CONTROLLER_ARRAY = 'particleSystemAreaSpawners'

# 🆕 2026-10-12（A340）：`AnimFXController.sounds[i]` / `exitSounds[i]` 那 5 个**数值**字段
#   （`sound` 那条引用单独解，见 `pack_animfx_defs`）。判据 = 签名桩
#   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/PlaySoundOnTime.cs` ＋
#   `d:/2/tools/decomp_full/PlaySoundOnTime__{ctor,Update,Reset}.c`（偏移：`time` +0x10 · `sound` +0x18 ·
#   `is2d` +0x20 · `repeat` +0x21 · `loops` +0x24 · `timeInterval` +0x28）。
ANIMFX_SOUND_FIELDS = ('time', 'is2d', 'repeat', 'loops', 'timeInterval')

# 🔴 `Target.fields` 里**哪些键的 `s` 是「对象路径」**（要在 prefab / 清单里按名字查存在性）。
#   🆕 2026-10-12（A340）：A340 之前 `s` **一律**是路径 ⇒ `check_prefab_side` 不必挑键；
#   A340 收进来的 `sounds.<i>.sound`（= **cue 名**）与 `modules.<i>`（= **模块类名**）都不是对象名
#   ⇒ 不挑的话它们会被当成「prefab 里没有这个 GameObject」**误报**（`norm()` 还会把它们当路径切）。
FIELD_S_IS_PATH = ('particleSystemPrefab', 'target')

# 🆕 2026-10-12（A340）：cue（原版 `AudioCue`）住在这个包里 —— `sounds[i].sound` 那条
#   `{m_FileID: 12, …}` 指的 CAB 实测就在它里面（`CAB-62d1945b7005c115fb946f0d264a205b`）。
CUE_BUNDLE = 'soundcollection_assets_all.bundle'

# 🆕 2026-10-14（W10/A394）：`AnimFXModule*` 字段里那些 `presetSO`（原版 `CameraShakePresetSO`）
#   住在这个包里 —— 判据与 `工具/gen_shake_presets.py` 的 `SRC_DIR` **是同一个包**
#   （那边按解包产物 `assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/Shake *.json` 读，
#    这边要从**原版包**里按 `(CAB, pid)` 解引用 ⇒ 得知道包名）。
#   实测（tauviorla 那颗屏震模块）：`manualTriggerCameraShakes[0].presetSO` 的 `m_FileID: 20`
#   → `externals[19]` = `CAB-da1bb534be23f0576b2df09a41d87665`，就在这个包里；
#   pid `-8043713765525655674` 的 `m_Name` = `Shake Earthquake`。
PRESET_BUNDLE = 'tweenandshakes_assets_all.bundle'


def load_script_names():
    """`m_Script` 的 pathID → 类名（全 84 个包共用同一张表）。"""
    out = {}
    for o in UnityPy.load(os.path.join(AA, MONO_BUNDLE)).objects:
        if o.type.name != 'MonoScript':
            continue
        try:
            d = o.read_typetree()
        except Exception:
            continue
        out[o.path_id] = d.get('m_ClassName') or d.get('m_Name')
    return out


class ExtraCABNames(object):
    """🆕 2026-10-14（W10/A394）：**跨包**引用 → 那个对象的解析器**基类**
    （`CueNames` / `PresetRefs` 共用这一份 —— 本仓铁律 6：同一条规则只写一遍）。

    为什么要有它：`Bundle` 只装**一个**包的对象图（见 `Bundle.__init__` 的 `by_cab`），
    而有些引用指向**别的包**里的 CAB（`m_FileID` 查出来是个 `CAB-xxx`，`by_cab` 里没有它）
    ⇒ 只能把那个包**按需**读进来，另建一张 `(CAB 名, pid) → ObjectReader` 的表。
    ⚠️ 键里**一定带 CAB 名**：pid 只在**同一份 CAB 内**唯一
    （本仓踩过「按 pid 全库反查 ⇒ 静默拿错」，见 `Bundle.__init__` 那段）。
    ⚠️ 解不出**一律出声、不猜**（红线：不许静默失败）。"""

    def __init__(self, bundle_file, label, missing_why, shape_key=None, shape_why=''):
        self.bundle_file = bundle_file
        self.label = label              # 出声时点名用（'cue' / '屏震 preset'）
        self.missing_why = missing_why  # 包不在时那句「解不出什么」
        self.shape_key = shape_key      # 该有的形状键（拿到了名字但形状不对 ⇒ 多半引用解错了）
        self.shape_why = shape_why
        self._by_cab = None
        self._name = {}
        self.warnings = []

    def _ensure(self):
        if self._by_cab is not None:
            return
        self._by_cab = {}
        p = os.path.join(AA, self.bundle_file)
        if not os.path.isfile(p):
            self.warnings.append('%s 包不在：%s —— %s' % (self.label, p, self.missing_why))
            return
        print('读 %s（%s 名解析，第一次遇到引用才读）…' % (self.bundle_file, self.label))
        env = UnityPy.load(p)
        for _o, bf in env.files.items():
            for cab, sf in getattr(bf, 'files', {}).items():
                objs = getattr(sf, 'objects', None)
                if isinstance(objs, dict):
                    self._by_cab[cab] = objs

    def obj_of(self, cab, pid):
        """`(CAB 名, pid)` → `ObjectReader`；找不到返回 `None`。
        **静默**（不记 `warnings`）—— 出声由调用方做（它才知道「这个引用是谁的、解不出会少什么」）。"""
        self._ensure()
        return (self._by_cab.get(cab) or {}).get(pid) if cab else None

    def name_of(self, cab, pid):
        """`(CAB 名, pid)` → `(类型名, m_Name)`；解不出返回 `(None, None)`（**不猜**，原因记进 `warnings`）。
        同一个 `(CAB, pid)` 只算一次、也只报一次。"""
        key = (cab, pid)
        if key in self._name:
            return self._name[key]
        out = (None, None)
        o = self.obj_of(cab, pid)
        if o is None:
            self.warnings.append('%s 引用（CAB `%s` · pid %s）在 `%s` 里找不到 —— 这个名字解不出来（不猜）'
                                 % (self.label, cab, pid, self.bundle_file))
        else:
            try:
                d = o.read_typetree()
            except Exception as e:                       # noqa: BLE001
                d = None
                self.warnings.append('%s（CAB `%s` · pid %s）的 typetree 读不出：%s' % (self.label, cab, pid, e))
            if isinstance(d, dict):
                nm = d.get('m_Name') or None
                if nm and self.shape_key and self.shape_key not in d:
                    # 名字拿到了、但**形状不对** ⇒ 出声：多半引用解错了
                    self.warnings.append('CAB `%s` pid %s 的 `m_Name` = `%s`，但它**没有 `%s`**'
                                         ' —— %s' % (cab, pid, nm, self.shape_key, self.shape_why))
                out = (o.type.name, nm)
        self._name[key] = out
        return out


class CueNames(ExtraCABNames):
    """🆕 2026-10-12（A340）：`AnimFXController.sounds[i].sound`（原版 `AudioCue`）→ **cue 名**
    （本类 2026-10-14（W10/A394）把「读别的包」那一段挪进 `ExtraCABNames`，行为一字未改）。

    cue 是什么：`soundcollection_assets_all` 包里一个**带 `clipList` 的 MonoBehaviour**，
    它的 `m_Name` 就是 cue 名 —— 与 `工具/import_original_sfx.py` 写 `animfx_sounds.json` 时
    用的键**同一个**（那边 `key = d.get("m_Name")`、认的形状就是 `clipList`）
    ⇒ 这里解出来的名字能直接被 `WarpforgeVFX.WFSoundBank.TryGetCue` 拿到
    （实测 tauviorla 那 3 条：`Railgun Turret` / `Railgun Turret Far` / `Railgun Huge`，**3/3 已在表里**）。

    为什么不能走 `Bundle.resolve_ref`：那条引用**跨包** ——
    `m_FileID: 12` 指的是那份 CAB 的 `externals[11]`，而那个 CAB 住在这个包里，
    `Bundle` 只装一个包的对象图 ⇒ 解不了（实测返回 `(None, None)`）。"""

    def __init__(self):
        ExtraCABNames.__init__(self, CUE_BUNDLE, 'cue',
                               '`AnimFXController` 的 `sounds`/`exitSounds` 解不出 cue 名',
                               'clipList', '不像 cue（`import_original_sfx.py` 认的就是这个键），要复核')

    def name_of(self, cab, pid):
        """`(CAB 名, pid)` → **cue 名**（= 基类那个 `(类型, 名)` 的第二个）；解不出返回 `None`。"""
        return ExtraCABNames.name_of(self, cab, pid)[1]


class PresetRefs(ExtraCABNames):
    """🆕 2026-10-14（W10/A394）：`AnimFXModuleScreenShake.{cameraShakes|manualTriggerCameraShakes}[i].presetSO`
    （原版 `CameraShakePresetSO`）→ 资产名。

    为什么要跨包解析：那条引用在**场景包**里的 `m_FileID` 指向别的 CAB，
    那份 CAB 住在 `tweenandshakes_assets_all.bundle` 里 —— 与 `工具/gen_shake_presets.py` 的
    `SRC_DIR`（`d:/2/新解包资源/assets_full/bundle_tweenandshakes_assets_all/MonoBehaviour/Shake *.json`）
    **同一个包**（本件亲核：tauviorla 那颗模块的 `m_FileID: 20` → `externals[19]` →
    `CAB-da1bb534be23f0576b2df09a41d87665`，就在这个包里；pid `-8043713765525655674` = `Shake Earthquake`）。
    形状键取 `preset` —— 那就是这个 SO 的字段名（判据 = 同一个包里的
    `assets_full/.../Shake Earthquake.json` 与 `gen_shake_presets.py` 的读法），没有它 ⇒ 出声（多半解错了）。"""

    def __init__(self):
        ExtraCABNames.__init__(self, PRESET_BUNDLE, '屏震 preset',
                               '`AnimFXModuleScreenShake.*.presetSO` 解不出名（那条屏震不会播）',
                               'preset', '不像 `CameraShakePresetSO`，要复核')

    def asset_ref(self, cab, pid):
        """`(CAB 名, pid)` → `@asset:<类型名>:<名>`（与 `工具/dump_animfx.py` 的 `ptr_ref` **同一套编码**
        —— 运行时 `WarpforgeVFX.WFModuleDef.SplitRef` 认的就是它）；解不出返回 `None`（**不猜**）。"""
        tn, nm = ExtraCABNames.name_of(self, cab, pid)
        if not nm or not tn:                               # ⛔ 不拿兜底类型名顶替 —— 那就是猜
            return None
        return '@asset:%s:%s' % (tn, nm)


class Bundle(object):
    """一个 bundle 的对象图：读一遍 typetree，建 名字/父子 索引。"""

    def __init__(self, path):
        self.env = UnityPy.load(path)
        self.tt = {}
        for o in self.env.objects:
            try:
                self.tt[o.path_id] = o.read_typetree()
            except Exception:
                self.tt[o.path_id] = None
        # 🆕 2026-10-07（A135④）：解 `customMaterial` 那条 `{m_FileID, m_PathID}` 要用的两张表。
        # 🔴 **`m_FileID` 是【引用者所在那份 CAB】的 externals 下标**（判据 → A152 / `_probe_deckinfo` 的 A127②）：
        #    `scenes_*` 的 15 个包是**双 CAB**（主 CAB + `.sharedAssets`），**同名 pid 在两份里可能是不同的东西**
        #    ⇒ 「按 pid 全库反查」会**静默拿错**。所以先把「谁在哪份 CAB」和「每份 CAB 的 externals」都记下来。
        self.cab_ext = {}      # CAB 名 -> [external path]
        self.by_cab = {}       # CAB 名 -> {pid: ObjectReader}
        self.pid_cabs = {}     # pid -> set(CAB 名)
        for _outer, _bf in self.env.files.items():
            for _cab, _sf in getattr(_bf, 'files', {}).items():
                objs = getattr(_sf, 'objects', None)
                if not isinstance(objs, dict):
                    continue
                self.cab_ext[_cab] = [getattr(x, 'path', '') or ''
                                      for x in (getattr(_sf, 'externals', None) or [])]
                self.by_cab[_cab] = objs
                for _pid in objs:
                    self.pid_cabs.setdefault(_pid, set()).add(_cab)
        self.warnings = []     # collect() 期间攒下的「出声」（`main()` 合进 `_unresolved`）
        # 🆕 2026-10-12（A340）：cue 名解析器（`CueNames`）—— **由 `main()` 接上**，
        #   跨包那条引用（`AnimFXController.sounds[i].sound`）只能靠它解（见 `CueNames` 的注释）。
        self.cues = None
        # 🆕 2026-10-14（W10/A394）：屏震 preset 名解析器（`PresetRefs`）—— 同样由 `main()` 接上，
        #   解 `AnimFXModuleScreenShake.*.presetSO` 那条跨包引用（见 `PresetRefs` 的注释）。
        self.presets = None
        self.go_name, self.tr, self.tf_of_go = {}, {}, {}
        for pid, d in self.tt.items():
            if not isinstance(d, dict):
                continue
            if 'm_Component' in d:
                self.go_name[pid] = d.get('m_Name')
            if 'm_Father' in d and 'm_Children' in d:
                self.tr[pid] = d
                g = (d.get('m_GameObject') or {}).get('m_PathID')
                if g:
                    self.tf_of_go[g] = pid

    def cab_of(self, pid):
        """pid 在哪份 CAB。**多份都有时取对象数最多的那份并出声**（不许静默挑一个）。"""
        cabs = self.pid_cabs.get(pid)
        if not cabs:
            return None
        if len(cabs) == 1:
            return next(iter(cabs))
        best = max(cabs, key=lambda c: len(self.by_cab.get(c) or {}))
        self.warnings.append('pid %s 在 %d 份 CAB 里都有（%s）—— 取对象数最多的 `%s`；'
                             '这条引用**没钉死**，要复核' % (pid, len(cabs), '、'.join(sorted(cabs)), best))
        return best

    def ref_cab_in(self, ref, cab):
        """`{m_FileID, m_PathID}` 这条引用落在**哪份 CAB**（名字；解不出返回 `None`），
        引用者那份 CAB **由调用方直接给**（已经算过的不必按 pid 再反查一次）。
        `m_FileID == 0` ⇒ 引用者**自己那份** CAB；`> 0` ⇒ 引用者那份 CAB 的 `externals[m_FileID-1]`
        （**下标从 1 起**：`externals[0]` 对应 `m_FileID: 1`）。没 pid ⇒ `None`。

        🆕 2026-10-14（W10/A394）：从 `ref_cab` 里抽出来 —— `pack_module_fields` 已经知道
        模块在哪份 CAB（`ref_cab` 算出来的），对模块**内部**那些引用再按 pid 反查一次会多打一条
        「pid 在多份 CAB 里都有」的**假警告**。判据只此一份，`ref_cab` 转调本函数。"""
        fid = (ref or {}).get('m_FileID') or 0
        if not (ref or {}).get('m_PathID'):
            return None
        if fid == 0:
            return cab
        ext = self.cab_ext.get(cab) or []
        if fid > len(ext):
            return None
        return (ext[fid - 1] or '').replace('\\', '/').rstrip('/').split('/')[-1]

    def ref_cab(self, ref, owner_pid):
        """同上，但引用者只给 **pid**（`ref_cab_in` 的 `cab` 由它反查）。
        🆕 2026-10-12（A340）：`resolve_ref` 与跨包那条路（`CueNames`）**共用这一份**
        —— cue 的引用指向**别的包**里的 CAB，`by_cab` 里没有它，只能把 CAB 名拿去别处查。"""
        return self.ref_cab_in(ref, self.cab_of(owner_pid))

    def resolve_ref(self, ref, owner_pid):
        """`{m_FileID, m_PathID}` → `(类型名, 名字)`。
        `m_FileID == 0` ⇒ 同文件；`> 0` ⇒ **引用者自己那份 CAB** 的 `externals[m_FileID-1]` 指向的 CAB。
        解不出返回 `(None, None)`（**不猜**）。"""
        pid = (ref or {}).get('m_PathID')
        if not pid:
            return None, None
        objs = self.by_cab.get(self.ref_cab(ref, owner_pid))
        if objs is None:
            return None, None                  # 跨包引用 —— 本件这条路上没有（真遇到就如实返回 None）
        o = (objs or {}).get(pid)
        if o is None:
            return None, None
        try:
            d = o.read_typetree()
        except Exception:
            return o.type.name, None
        if isinstance(d, dict) and d.get('m_Name'):
            return o.type.name, d['m_Name']
        # `Material` 这类没有 `m_Name` 的走 `m_SavedProperties`；**名字不在树里** ⇒ 只回类型名
        return o.type.name, None

    def material_of(self, ref, owner_pid):
        """`customMaterial` 那条引用 → `(资产名, 它自己的属性名集合)`；解不出 `(None, None)`。"""
        tname, mname = self.resolve_ref(ref, owner_pid)
        if tname != 'Material':
            return None, None
        pid = (ref or {}).get('m_PathID')
        fid = (ref or {}).get('m_FileID') or 0
        if fid == 0:
            objs = self.by_cab.get(self.cab_of(owner_pid))
        else:
            ext = self.cab_ext.get(self.cab_of(owner_pid)) or []
            objs = self.by_cab.get((ext[fid - 1] or '').replace('\\', '/').rstrip('/').split('/')[-1]) if fid <= len(ext) else None
        o = (objs or {}).get(pid)
        if o is None:
            return mname, None
        try:
            d = o.read_typetree()
        except Exception:
            return mname, None
        sp = (d or {}).get('m_SavedProperties') or {}
        names = set()
        for sec in ('m_TexEnvs', 'm_Floats', 'm_Colors', 'm_Ints'):
            for e in (sp.get(sec) or []):
                if isinstance(e, (list, tuple)) and e and isinstance(e[0], str):
                    names.add(e[0])
        return mname, names

    def chain(self, go_pid):
        """GameObject pid → [(名字, 世界位置)]，根在前。"""
        tpid = self.tf_of_go.get(go_pid)
        out, guard = [], 0
        while tpid and guard < 300:
            t = self.tr.get(tpid)
            if not t:
                break
            g = (t.get('m_GameObject') or {}).get('m_PathID')
            pos = self.world_pos(tpid)
            out.append((self.go_name.get(g, '?'), pos))
            nxt = (t.get('m_Father') or {}).get('m_PathID')
            tpid = nxt if nxt else None
            guard += 1
        out.reverse()
        return out

    def world_pos(self, tpid):
        M4 = [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]
        chain, cur, guard = [], tpid, 0            # ⚠️ 键是 **int** pid（UnityPy 给的）——
        while cur in self.tr and guard < 300:      #    别照抄 `gen_arena_negscale` 那句 `str()`，
            t = self.tr[cur]                       #    那边是按文件名建的**字符串**索引，转字符串会让链整条不走
            lp = t.get('m_LocalPosition') or {}
            lq = t.get('m_LocalRotation') or {}
            ls = t.get('m_LocalScale') or {}
            chain.append(((lp.get('x', 0.0), lp.get('y', 0.0), lp.get('z', 0.0)),
                          (lq.get('x', 0.0), lq.get('y', 0.0), lq.get('z', 0.0), lq.get('w', 1.0)),
                          (ls.get('x', 1.0), ls.get('y', 1.0), ls.get('z', 1.0))))
            nxt = (t.get('m_Father') or {}).get('m_PathID')
            cur = nxt if nxt else None
            guard += 1
        for (p, q, s) in reversed(chain):
            M4 = N._mul(M4, N._local_matrix(p[0], p[1], p[2], q, s))
        return [round(M4[0][3], 4), round(M4[1][3], 4), round(M4[2][3], 4)]

    def target_owner(self, pid):
        """目标组件 pid → (它的 GameObject pid, 类型名)；拿不到返回 (None, None)。"""
        d = self.tt.get(pid)
        if not isinstance(d, dict):
            return None, None
        g = (d.get('m_GameObject') or {}).get('m_PathID')
        return g, None

    def target_fields(self, pid):
        """目标**组件自己**的序列化字段（只收 TARGET_FIELDS 里列的那几个类）。

        🆕 **2026-10-11（A192③）**：这里**统一**把 `ParticleSystemAreaSpawnerController` 的
        **嵌套那一层**（`particleSystemAreaSpawners[]`）也摊平带上（`spawner.N` / `weight.N` / `chances.N`）。
        · 原来只有 `collect_standalone` 那条路调了 `pack_controller_defs`，
          **blendable 引用的那条（`collect`）没调** ⇒ 万一某条 `ScenarioParticleSpawnerBlender.controllers`
          非空，建出来的 controller 会**一条 spawner 都没配上**（运行时只 `LogWarning`、不生成粒子）。
        · 收在这里 = **两条路共用一份**（本仓铁律 6）；`collect_standalone` 也改成转调本函数，
          产物**逐字节不变**（同一份 `pack_fields` + 同一个顺序）。
        · ⚠️ 实测今天 **4 条 blender 的 `controllers` 全是空数组** ⇒ 这一改**目前不改变任何数据**
          （`--check` 前后逐字节相同）；补它是**把两条路的判据补齐**，不是修一条已发生的缺陷。
        🆕 **2026-10-12（A340）**：`AnimFXController` 的**三层**（`sounds` / `exitSounds` / `modules`）
          也收在这里（`pack_animfx_defs`，跨包的 cue 名走 `self.cues`）。"""
        d = self.tt.get(pid)
        if not isinstance(d, dict):
            return []
        cn = self.scripts.get((d.get('m_Script') or {}).get('m_PathID'))
        out = self.pack_fields(d, TARGET_FIELDS.get(cn))
        if cn == 'ParticleSystemAreaSpawnerController':
            out += self.pack_controller_defs(d.get(CONTROLLER_ARRAY) or [])
        if cn == 'AnimFXController':
            out += self.pack_animfx_defs(d, pid)
        return out

    def self_fields(self, d, cn):
        """🆕 2026-10-07（A136）：**这个组件自己**的小数字段（走 `Item.floats`，见 `FLOAT_FIELDS`）。"""
        return self.pack_fields(d, FLOAT_FIELDS.get(cn))

    def pack_fields(self, d, names):
        """把 `names` 里那几个字段打包成 `TargetField` 那套形状（`k` / `f` / `s`）。
        值一律照原值、不做任何换算；`Vector3` 拆三轴；对象引用写成 `s` = 落点的层级路径；
        **普通字符串字段**（`filterCode` 那种）也写 `s` = 原值本身。
        ⚠️ `Target.fields` 与 `Item.floats` **共用这一份** —— 别写第二遍（本仓铁律 6）。"""
        if not names:
            return []
        out = []
        for f in names:
            v = d.get(f)
            if isinstance(v, dict) and 'm_PathID' in v:          # 对象引用（当前只有 particleSystemPrefab）
                rp = (v or {}).get('m_PathID')
                if not rp:
                    continue                                     # 空引用（原版就有）—— 跳过，别当缺口
                g, _ = self.target_owner(rp)
                ch = self.chain(g) if g else []
                if ch:
                    out.append({'k': f, 's': '/'.join(n for n, _ in ch)})
            elif isinstance(v, dict) and 'x' in v:               # Vector3 → 三轴（原版一个字段，这里拆三条）
                for ax in ('x', 'y', 'z'):
                    out.append({'k': '%s.%s' % (f, ax), 'f': float(v.get(ax, 0.0) or 0.0)})
            elif isinstance(v, bool):
                out.append({'k': f, 'f': 1.0 if v else 0.0})
            elif isinstance(v, (int, float)):
                out.append({'k': f, 'f': float(v)})
            elif isinstance(v, str) and v:                       # 普通字符串字段（`filterCode`）
                out.append({'k': f, 's': v})
        return out

    def pack_controller_defs(self, arr):
        """🆕 2026-10-07（A137）：`ParticleSystemAreaSpawnerController.particleSystemAreaSpawners[]`
        摊平成 `TargetField` 三条一组（见 `CONTROLLER_ARRAY` 那段注释）。"""
        out = []
        for i, e in enumerate(arr or []):
            if not isinstance(e, dict):
                continue
            ref = e.get('particleSystemAreaSpawner') or {}
            rp = ref.get('m_PathID')
            if rp:
                g, _ = self.target_owner(rp)
                ch = self.chain(g) if g else []
                if ch:
                    out.append({'k': 'spawner.%d' % i, 's': '/'.join(n for n, _ in ch)})
            for k in ('weight', 'chances'):
                v = e.get(k)
                out.append({'k': '%s.%d' % (k, i), 'f': float(v) if isinstance(v, (int, float)) else 0.0})
        return out

    def pack_animfx_defs(self, d, owner_pid):
        """🆕 2026-10-12（A340）：`AnimFXController` 的**三层** —— `sounds[]` / `exitSounds[]` / `modules[]`。

        为什么不能走 `pack_fields`：这三层**都是数组**，元素还分两种它不认的形状 ——
        `PlaySoundOnTime`（嵌套 `[Serializable]`，且 `sound` 是一条**跨包**对象引用）与
        `List<AnimFXModuleBase>`（组件引用）。切法照 `pack_controller_defs`：**摊平进 `TargetField`**
        （`k` 带下标 · 数值走 `f` · 字符串走 `s`），**不新开 DTO** —— `Core/EnvBlendables.cs` 那两个
        查找器（`GetF` / `GetS`）本来就能读这个形状，运行时读侧一行都不用改（那个文件在本件白名单外）。

        键（`<i>` = 0 基下标 —— **尖括号是占位符记号、不是 XML 标签**，⛔ 别抄进 C# 的 `///`（会 CS1570，
        写法与出处见文件头那条「记号」）；这套键名与 `资料/普查产出_1011/W9_A196_A210_A211.md` §五·3 给的一致）：
          `<层>.count`    = 原版这层的**条数**（`f`）。**缺这个键 = 旁挂没收这一层**（运行时据此出声），
                            与「原版本来就是空的」分开 —— 后者写 `0`。
          `<层>.<i>.sound`= **cue 名**（`s`）。`''` = 原版那条就是空引用（留档，运行时不播）。
          `<层>.<i>.soundUnresolved` = **解不出 cue 时**的标记（`f` = 1；运行时据此出声点名到第几条，
                            见 `pack_animfx_defs` 里那段「为什么要有这个键」）。
          `<层>.<i>.{time,is2d,repeat,loops,timeInterval}` = `PlaySoundOnTime` 的另外 5 个字段（`f`）。
          `modules.<i>`   = 模块的**类名**（`s`）。原版是 `List<AnimFXModuleBase>`；场景侧那条线
                            （`WarpforgeVFX.WFSceneModuleFactory`）照这个名字**真建组件**。
          🔴 🆕 **2026-10-14（W10/A394）**：`modules.<i>.<点号键>` = **模块自己**的序列化字段
                            （见 `pack_module_fields`；由 `WFSceneModuleScreenShake.Configure` 读）。
                            这是 A394 那轮留下的**唯一欠账**的收口（原来只有类名、模块两条轨道全空）。

        🔴 **cue 名必须走 `m_FileID` 外部引用解析**（`sound` 指向的 `AudioCue` 住在
           `soundcollection_assets_all` 里，不在本包的 `by_cab` 里，见 `CueNames`）。
           解不出 ⇒ **这条 `sound` 键不写 + 出声**（由运行时那条「没有 `sound` 键」的路点名），
           ⛔ **不拿空串顶替**（那就把「解不出」伪装成「原版就是空的」= 静默错，本仓红线）。"""
        out = []
        for arr in ('sounds', 'exitSounds'):
            rows = [e for e in (d.get(arr) or []) if isinstance(e, dict)]
            out.append({'k': '%s.count' % arr, 'f': float(len(rows))})
            for i, e in enumerate(rows):
                ref = e.get('sound') or {}
                pid = ref.get('m_PathID')
                if not pid:
                    # 原版就有空引用这一类（`pack_fields` 那条同款注释）⇒ **留一条空记录**：
                    #   运行时能把它与「这条键根本没写下来」（= 解不出）分开，前者静默、后者出声。
                    out.append({'k': '%s.%d.sound' % (arr, i), 's': ''})
                else:
                    cab = self.ref_cab(ref, owner_pid)
                    if self.cues is None:
                        self.warnings.append('`%s` 第 %d 条的 cue 解不出来：这个 `Bundle` 没接 `CueNames`'
                                             '（`Bundle.cues` 是 None —— `main()` 忘了接）' % (arr, i))
                        nm = None
                    else:
                        nm = self.cues.name_of(cab, pid)
                        if not nm:
                            self.warnings.append('`%s` 第 %d 条的 `sound` 解不出 cue 名（CAB `%s` · pid %s）'
                                                 ' —— 这一条**不写 `sound` 键、改写 `soundUnresolved`**'
                                                 '（不拿空串顶替：那就把「解不出」伪装成「原版就是空的」）'
                                                 % (arr, i, cab, pid))
                    if nm:
                        out.append({'k': '%s.%d.sound' % (arr, i), 's': nm})
                    else:
                        # 🔴 **`soundUnresolved` 这个标记键是给运行时的**：`TargetField` 没有「有没有这个键」
                        #    那一问（`Core/EnvBlendables.cs` 的 `GetS` 对「键不在」与「值是空串」都回 `""`），
                        #    而这两档的含义相反 ⇒ 解不出时**显式留一条 `1`**，运行时据此出声点名到第几条。
                        #    （与 `EnvironmentApplier.HasField` 是同一个用途；那个是 `private`、本件改不了它。）
                        out.append({'k': '%s.%d.soundUnresolved' % (arr, i), 'f': 1.0})
                for f in ANIMFX_SOUND_FIELDS:
                    v = e.get(f)
                    if isinstance(v, bool):
                        out.append({'k': '%s.%d.%s' % (arr, i, f), 'f': 1.0 if v else 0.0})
                    elif isinstance(v, (int, float)):
                        out.append({'k': '%s.%d.%s' % (arr, i, f), 'f': float(v)})
        mods = [m for m in (d.get('modules') or []) if isinstance(m, dict)]
        out.append({'k': 'modules.count', 'f': float(len(mods))})
        for i, m in enumerate(mods):
            mp = m.get('m_PathID')
            # 按**引用落点那份 CAB** 取对象（不拿 pid 全库反查 —— pid 只在同一份 CAB 内唯一）
            mo_cab = self.ref_cab(m, owner_pid)
            o = (self.by_cab.get(mo_cab) or {}).get(mp)
            cn = None
            md = None
            if o is not None:
                try:
                    md = o.read_typetree()
                except Exception:                          # noqa: BLE001
                    md = None
                if isinstance(md, dict):
                    cn = self.scripts.get((md.get('m_Script') or {}).get('m_PathID'))
            if cn:
                out.append({'k': 'modules.%d' % i, 's': cn})
                # 🆕 2026-10-14（W10/A394）：**模块自己**的字段也打进旁挂（键 `modules.<i>.<点号键>`）
                #   —— 没有这一跳，建出来的模块两条轨道永远是空的（A394 的唯一欠账）。
                if isinstance(md, dict):
                    out += self.pack_module_fields(md, i, mo_cab)
            else:
                self.warnings.append('`AnimFXController` 第 %d 个模块解不出类名（pid %s）—— 不猜，不写这条'
                                     % (i, mp))
        return out

    def ref_value(self, ref, owner_cab):
        """🆕 2026-10-14（W10/A394）：一条**对象引用** → `@node:<组件>:<路径>` / `@asset:<类型>:<名>`
        （与 `工具/dump_animfx.py` 的 `ptr_ref` **同一套编码** —— 运行时 `WFModuleDef.SplitRef` 认的就是它）。

        同包先查 `by_cab`；查不到**整条交给 `self.presets`**（跨包：`presetSO` → `tweenandshakes_assets_all`，
        那条路带形状检查 + 出声，见 `PresetRefs`）。解不出返回 `None`（**不猜**；
        调用方写一条 `<键>Unresolved` 标记出声，⛔ 不拿空串顶替）。"""
        pid = (ref or {}).get('m_PathID')
        if not pid:
            return None
        cab = self.ref_cab_in(ref, owner_cab)
        o = (self.by_cab.get(cab) or {}).get(pid)
        if o is None:
            if self.presets is None:
                self.warnings.append('一条对象引用（CAB `%s` · pid %s）不在本包里，而这个 `Bundle` '
                                     '**没接跨包解析器**（`Bundle.presets` 是 None —— `main()` 忘了接）'
                                     '⇒ 解不出名字（不猜）' % (cab, pid))
                return None
            return self.presets.asset_ref(cab, pid)
        try:
            d = o.read_typetree()
        except Exception:                                  # noqa: BLE001
            return None
        if not isinstance(d, dict):
            return None
        g = (d.get('m_GameObject') or {}).get('m_PathID')
        if g:
            # 组件 → **节点引用**（`@node:` 后面那两段与 `ptr_ref` 逐字一致：组件类型 + 层级路径）
            ch = self.chain(g)
            if not ch:
                return None
            return '@node:%s:%s' % (o.type.name, '/'.join(n for n, _ in ch))
        nm = d.get('m_Name')
        if not nm:
            return None
        return '@asset:%s:%s' % (o.type.name, nm)

    def pack_module_fields(self, md, i, mo_cab):
        """🆕 2026-10-14（W10/A394）：**模块自己**的序列化字段 → 一串 `TargetField`，
        键 = `modules.<i>.<点号键>`（摊平，照 `pack_controller_defs` / `pack_animfx_defs` 那一套；
        `<i>`/`<点号键>` 是**占位符记号、不是 XML 标签** —— 说明见文件头那条「记号」，
        ⛔ 别把这行照抄进 C# 的 `///`（裸尖括号 = CS1570））。

        🔴 **点号键的语法必须与 `数据/游戏数据/animfx_modules.json` 逐字一致**
        （数组下标带方括号：`manualTriggerCameraShakes[0].presetSO`）—— 因为运行时那侧
        `WFModuleDef.CountList("cameraShakes")` 数的就是 `cameraShakes[n]` 这个**前缀**、
        `WFModuleScreenShake.ReadList` 按 `key[i].<字段>` 取值（见
        `WarpforgeVFX/Runtime/WFEffectModule.cs` 与 `WFModuleScreenShake.cs`）。
        ⛔ **别在这里换一套键名**（换成 `manualTriggerCameraShakes.0.presetSO` 之类）—— 那会让
        `ReadList` **一条都读不到**，而且是静默的（建出来一个空壳模块，本仓红线）。

        值：数值/布尔 → `f`（`float()`；C# 那侧 `TargetField.f` 是 `float`，回读时
        `GetInt` 拿到的是 `"0"`/`"1"` 这种整数字面量 —— 见 `ScenarioBlendables.BuildAnimFxModules`）；
        字符串 → `s`；**对象引用** → `s` = `@node:`/`@asset:`（见 `ref_value`）。
        解不出的引用**不拿空串顶替**：写一条 `<键>Unresolved = 1`（与 `sounds.<i>.soundUnresolved`
        同一口径），运行时据此点名出声。

        空数组**一条键都不写**（原版 `cameraShakes = []` 就是这样 ⇒ `CountList` = 0 ⇒ 空数组；
        与「生成器没收」的区别由「有没有别的 `modules.<i>.*` 键」体现）。"""
        out = []
        pre = 'modules.%d.' % i

        def walk(v, key):
            if isinstance(v, dict):
                if 'm_PathID' in v:                        # 对象引用
                    if not v.get('m_PathID'):
                        return                             # 空引用（原版就有）—— 跳过，别当缺口
                    got = self.ref_value(v, mo_cab)
                    if got:
                        out.append({'k': key, 's': got})
                    else:
                        out.append({'k': key + 'Unresolved', 'f': 1.0})
                        self.warnings.append('`AnimFXController` 模块字段 `%s` 解不出引用（%s）—— '
                                             '不写值、写 `Unresolved` 标记（不猜）'
                                             % (key, json.dumps(v, ensure_ascii=False)))
                    return
                for k2, v2 in (v or {}).items():
                    if k2.startswith('m_'):                # `m_GameObject` / `m_Enabled` / `m_Script` / `m_Name`
                        continue                           #   —— 不是模块的「字段」，别混进来
                    walk(v2, key + '.' + k2)
                return
            if isinstance(v, (list, tuple)):
                for j, v2 in enumerate(v):
                    walk(v2, '%s[%d]' % (key, j))
                return
            if isinstance(v, bool):                        # ⚠️ 必须在 int 之前（bool 是 int 的子类）
                out.append({'k': key, 'f': 1.0 if v else 0.0})
            elif isinstance(v, (int, float)):
                out.append({'k': key, 'f': float(v)})
            elif isinstance(v, str) and v:
                out.append({'k': key, 's': v})

        for k2, v2 in (md or {}).items():
            if k2.startswith('m_'):
                continue
            walk(v2, pre + k2)
        return out

    def collect_standalone(self, scripts):
        """🆕 2026-10-07（A137）：**所有** `STANDALONE_CLASSES` 实例（不限于「是某个 blendable 的目标」），
        按根分组 → `{根名: [条目]}`。与 `collect` 的差别只有两条：
          ① 不需要有个 blendable 宿主（这一批**没有任何组件引用它们**，它们自己 `OnEnable` 起循环）；
          ② 目标 = **组件自己那个 GameObject**（`kind` 沿用 `spawner` / `controller` 两个字面量，
             与 blender 那条路的 `Target.kind` 同一套 —— 运行时建的就是同一个 `MakeSpawner`）。
        🔴 被 `ScenarioParticleSpawnerBlender` 引用的那些**排除**（那 4 条由 `collect` 那条路建，
           收两遍 = 同一个对象上建两次组件）。"""
        self.scripts = scripts
        refd = set()                                     # blender 的 `areaSpawners` / `controllers`
        for pid, d in self.tt.items():
            if not isinstance(d, dict) or 'm_Script' not in d:
                continue
            if scripts.get((d.get('m_Script') or {}).get('m_PathID')) != 'ScenarioParticleSpawnerBlender':
                continue
            for f in ('areaSpawners', 'controllers'):
                for r in (d.get(f) or []):
                    p = (r or {}).get('m_PathID')
                    if p:
                        refd.add(p)
        by_root = {}
        for pid, d in self.tt.items():
            if not isinstance(d, dict) or 'm_Script' not in d or 'm_GameObject' not in d:
                continue
            cn = scripts.get((d.get('m_Script') or {}).get('m_PathID'))
            if cn not in STANDALONE_CLASSES or pid in refd:
                continue
            ch = self.chain((d['m_GameObject'] or {}).get('m_PathID'))
            if not ch:
                continue
            path = '/'.join(n for n, _ in ch)
            # 🆕 2026-10-11（A192③）：转调 `target_fields` —— 嵌套那一层（`particleSystemAreaSpawners[]`）
            #   现在**收在那一处**（两条路共用一份），这里不再自己拼一遍（本仓铁律 6）。
            #   产物逐字节不变：同一个 `pack_fields` + 同一个「先字段、后嵌套」的顺序。
            fields = self.target_fields(pid)
            by_root.setdefault(ch[0][0], []).append({
                'cls': cn,
                'owner': path, 'ownerLeaf': ch[-1][0], 'ownerPos': ch[-1][1],
                'fields': {},                            # 这个组件自己没有 `Item.fields` 那套要记的
                'targets': [{'path': path, 'leaf': ch[-1][0], 'pos': ch[-1][1],
                             'kind': 'spawner' if cn == 'ParticleSystemAreaSpawner' else 'controller',
                             'fields': fields}],
            })
        # 排序只为「同一个 bundle 反复跑出来的文件逐字节一样」（diff 稳）；运行时不依赖这个顺序
        #   —— 它自己先建 spawner 再建 controller（controller 要引用前者的组件）。
        for _r in by_root:
            by_root[_r].sort(key=lambda i: (i['cls'], i['owner']))
        return by_root

    def referenced_pids(self, scripts):
        """🆕 2026-10-12（A393）：**所有 blendable 实例的字段里引用到的** pid 集合。

        用途 = `collect_scene_standalone` 的排除依据：一个 pid 若已经被某条 blendable 收走了
        （`CLASSES` 里那些目标字段指向它），就不该再进 standalone 那一节 ——
        **同一个对象上建两次组件**（与 A137 排掉「被 blender 引用的那 4 条 spawner」是同一条理由）。"""
        out = set()
        for pid, d in self.tt.items():
            if not isinstance(d, dict) or 'm_Script' not in d:
                continue
            cn = scripts.get((d.get('m_Script') or {}).get('m_PathID'))
            if cn not in CLASSES:
                continue
            for spec in CLASSES[cn][0]:
                fname = spec[0]
                vals = [d.get(fname)] if (len(spec) > 2 and spec[2] == 'single') else (d.get(fname) or [])
                for t in vals:
                    p = (t or {}).get('m_PathID') if isinstance(t, dict) else None
                    if p:
                        out.add(p)
        return out

    def collect_scene_standalone(self, scripts):
        """🆕 2026-10-12（A393）：**场景侧** `SCENE_STANDALONE_CLASSES` 的全部实例（不限于「是某个
        blendable 的目标」），按根分组 → `{根名: [条目]}`。与 `collect_standalone` 的差别只有两条：
          ① 没有 blendable 宿主 —— 这些组件自己就是「被建」的那个东西；
          ② 目标 = **组件自己那个 GameObject**（`kind` 沿用 `animfx`，与 `CLASSES` 那两条同一种 kind
             ⇒ 运行时**同一个 `MakeAnimFx`** 建它们，不另写一份建法）。
        🔴 被 blendable 引用的那些**排除**（见 `referenced_pids`）。

        额外收两个**不属于组件**的开关（旁挂键 `enabled` / `goActive`）—— 原版这 5 个里有 3 个根本
        不跑，不记下来就会建出「原版不跑、我们跑」的假象：
          · `enabled`  = 组件自己的 `m_Enabled`（`battlearena3` 两个 `Lightning_Green` 是 **0**）；
          · `goActive` = **沿父链与过**的 `activeInHierarchy`（tauviorla 的 `Big Gun Effect` 是 **0**）。
        两者的判据都是原版场景包的序列化字段（`m_Enabled` / `m_IsActive`），不是我们挑的默认值。"""
        self.scripts = scripts
        refd = self.referenced_pids(scripts)
        by_root = {}
        for pid, d in self.tt.items():
            if not isinstance(d, dict) or 'm_Script' not in d or 'm_GameObject' not in d:
                continue
            cn = scripts.get((d.get('m_Script') or {}).get('m_PathID'))
            if cn not in SCENE_STANDALONE_CLASSES or pid in refd:
                continue
            go = (d['m_GameObject'] or {}).get('m_PathID')
            ch = self.chain(go)
            if not ch:
                continue
            path = '/'.join(n for n, _ in ch)
            # 沿父链与 `m_IsActive`（与清单生成器那边同一个口径：**父关着 = 自己也关着**）
            active = True
            tpid = self.tf_of_go.get(go)
            guard = 0
            while tpid and guard < 300:
                t = self.tr.get(tpid)
                if not t:
                    break
                g2 = (t.get('m_GameObject') or {}).get('m_PathID')
                dd = self.tt.get(g2)
                if not isinstance(dd, dict) or not dd.get('m_IsActive', True):
                    active = False
                nxt = (t.get('m_Father') or {}).get('m_PathID')
                tpid = nxt if nxt else None
                guard += 1
            fields = [{'k': 'enabled', 'f': 1.0 if d.get('m_Enabled') else 0.0},
                      {'k': 'goActive', 'f': 1.0 if active else 0.0}]
            fields += self.target_fields(pid)
            by_root.setdefault(ch[0][0], []).append({
                'cls': cn,
                'owner': path, 'ownerLeaf': ch[-1][0], 'ownerPos': ch[-1][1],
                'fields': {},
                'targets': [{'path': path, 'leaf': ch[-1][0], 'pos': ch[-1][1],
                             'kind': 'animfx', 'fields': fields}],
            })
        for _r in by_root:
            by_root[_r].sort(key=lambda i: (i['cls'], i['owner']))
        return by_root


    def collect(self, scripts, want_roots=None):
        """→ {根名: [条目]}。want_roots = None 表示全收。"""
        self.scripts = scripts
        by_root = {}
        for pid, d in self.tt.items():
            if not isinstance(d, dict) or 'm_Script' not in d or 'm_GameObject' not in d:
                continue
            cn = scripts.get((d.get('m_Script') or {}).get('m_PathID'))
            if cn not in CLASSES:
                continue
            ch = self.chain((d['m_GameObject'] or {}).get('m_PathID'))
            if not ch:
                continue
            root = ch[0][0]
            if want_roots is not None and root not in want_roots:
                continue
            tgt_fields, extra = CLASSES[cn]
            targets = []
            for spec in tgt_fields:
                # 三元组 = 这个字段是**单个引用**（不是数组）；见 `CLASSES` 里 `'single'` 那几处
                fname, kind = spec[0], spec[1]
                vals = [d.get(fname)] if (len(spec) > 2 and spec[2] == 'single') else (d.get(fname) or [])
                for t in vals:
                    if not isinstance(t, dict):
                        continue
                    tp = (t or {}).get('m_PathID')
                    if not tp:
                        continue                       # 空引用（原版就有）—— 跳过，别当缺口
                    g, _ = self.target_owner(tp)
                    tch = self.chain(g) if g else []
                    if not tch:
                        continue
                    te = {'path': '/'.join(n for n, _ in tch),
                          'leaf': tch[-1][0], 'pos': tch[-1][1], 'kind': kind}
                    tf = self.target_fields(tp)
                    # ⚠️ 空就不写这个键：128 条里只有那 4 条 spawner 目标有字段，
                    #    否则每一条 target 都多一行 `"fields": []`，把 diff 全淹掉。
                    if tf:
                        te['fields'] = tf
                    targets.append(te)
            # 🆕 2026-10-07（A135④）：**嵌套结构体数组**里的目标（`RendererMaterialBlender[]`）
            nt = NESTED_TARGETS.get(cn)
            if nt:
                targets.extend(self.collect_nested(d, pid, nt))
            entry = {
                'cls': cn,
                'owner': '/'.join(n for n, _ in ch),
                'ownerLeaf': ch[-1][0],
                'ownerPos': ch[-1][1],
                'fields': {k: d.get(k) for k in extra if k in d},
                'targets': targets,
            }
            fl = self.self_fields(d, cn)
            if fl:
                entry['floats'] = fl
            by_root.setdefault(root, []).append(entry)
        return by_root

    def collect_nested(self, d, owner_pid, spec):
        """`NESTED_TARGETS` 那一条：数组字段 `arr` 的每个元素里，`refkey` 指目标、
        `propskey` = 要带走的字符串数组、`matkey` = 那条引用的 Material（记录它的**资产名**，
        并**就地核**「`propskey` 里的每个名字都真的在那个 Material 的属性表里」）。"""
        arrkey, refkey, propskey, matkey = spec
        out = []
        rows = d.get(arrkey) or []
        for el in rows:
            if not isinstance(el, dict):
                continue
            ref = el.get(refkey)
            tp = (ref or {}).get('m_PathID')
            if not tp:
                continue
            g, _ = self.target_owner(tp)
            tch = self.chain(g) if g else []
            if not tch:
                continue
            te = {'path': '/'.join(n for n, _ in tch),
                  'leaf': tch[-1][0], 'pos': tch[-1][1], 'kind': 'renderer'}
            props = [p for p in (el.get(propskey) or []) if isinstance(p, str) and p]
            if props:
                te['blendProps'] = props
            mname, mprops = self.material_of(el.get(matkey), owner_pid)
            if mname:
                te['customMaterial'] = mname
            if not mname:
                self.warnings.append('%s 的 `%s` 解不出资产名（引用 = %s）—— 如实记「解不出」，别猜'
                                     % (arrkey, matkey, json.dumps(el.get(matkey), ensure_ascii=False)))
            elif mprops is not None:
                miss = [p for p in props if p not in mprops]
                if miss:
                    self.warnings.append('原版 Material `%s` 的属性表里**没有**这些要补间的名字：%s'
                                         % (mname, '、'.join(miss)))
            out.append(te)
        return out


POS_MATCH_TOL = 0.05      # 「同一个对象」的位置容差 —— 与 `ArenaBuilder.SameObject` 同一档（判据只留一处）


def group_node_paths(arena):
    """`<场>_groups.json` 里**已经被 A191 建出来**的那些节点路径（没有那份旁挂 ⇒ 空集）。

    为什么要它：`nodes[]`/`reparent[]` 问的是「这个对象我们工程里有没有」—— 而 A191 那批
    `Scenario` / `Battle Arena Tau Viorla Baked` 这种【分组节点】**不在清单里**、是
    `ArenaBuilder.ApplyGroupNodes` 照 `<场>_groups.json` 现建的 ⇒ 只看清单会把它们误判成「没有」，
    于是**白建一遍**（空节点盖在真节点旁边，判据就散了）。"""
    p = os.path.join(ARENAS, arena, '%s_groups.json' % arena)
    if not os.path.isfile(p):
        return set()
    try:
        g = json.load(io.open(p, encoding='utf-8'))
    except Exception:                                             # noqa: BLE001
        return set()
    out = set()
    for n in (g.get('nodes') or []):
        if isinstance(n, dict) and n.get('path'):
            out.add(n['path'])
    return out


def scene_standalone_build(arena, items, manifest, groups_paths=None):
    """🆕 2026-10-12（A393）：这 5 条场景侧组件**要在我们工程里真的建出来**还差什么。

    两条：
      ① `nodes[]` = 宿主（或它的祖先）我们工程里**没有** ⇒ 要新建的节点（浅→深 · **原版 local TRS**
         —— 与 `gen_arena_groups` 同一个口径，建的时候直接写下去，不引入第二套坐标）；
      ② `reparent[]` = 原版挂在宿主**下面**、而我们**已经建出来**（但按平铺摆在各自的位置上）的对象
         —— 运行时改挂回宿主下。**为什么非要它**：`RocketTrail` 那颗组件唯一的作用就是
         `destroyTime = 6` **秒后销毁自己**，而原版那一销毁**连带 6 个子件粒子一起消失**
         （`BigExplosion`/`Smoke`/`Embers`/`Twinkle`/`Fire Small`/`Launch Smoke`，实测全是循环粒子）
         ⇒ 不改挂的话我们销毁的只是个空壳（**静默**地少一半效果，本仓红线）。
         🔴 **只在「这颗组件真的会销毁宿主」时才收**（`m_Enabled = 1` ∧ `activeInHierarchy` ∧
         `preventDestroy = 0`）—— 另外 4 个（两个 `Lightning_Green` 组件是关的、`Big Gun Effect` 的 GO
         是关的、`Railgun BIG (1)` 是 `preventDestroy = 1`）**永远不会销毁任何东西** ⇒ 对它们改挂
         只是白白动树（A191 那条「原版真有分层」是另一件账，不在这里顺手做）。

    🔴 **判据只留一处**：整个函数**复用 `gen_arena_groups`** —— 同一个原版包读法（`G.Scene`）、
       同一套 **no-scale 世界链**（`G.Scene.world_of`，判据见那个方法的 docstring）、
       同一套「`ArenaBuilder` 闸门会不会真把它建出来」的对读（`G.is_built` / `G.quality_off`）。
       ⛔ 别在这里另写一份。
    🔴 **`nodes[]` 只补「祖先链 + 宿主自己」，不展开整棵子树**（与 A191 那条 `wanted_paths` 的
       范围**有意不同**）：A191 要子树是因为那条 clip 的路径要落在真节点上；这里只要宿主存在就够，
       展开子树会凭空多出几十个空节点（实测 `Big Gun Effect` 有 22 个子件、`RocketTrail` 有 6 个）。
       ⚠️ 这条差异**清楚记在报告里**，别当成两处不一致。
       ⚠️ **2026-10-13 更正（A595 / W-T-a 实测）**：上面那句「与 A191 那条 `wanted_paths` 的范围**有意不同**」
       **在 A345 之后不再成立** —— `wanted_paths` 的范围**已不是**「只到 A191 那几个宿主」，而是**全覆盖**
       （判据 → `资料/普查产出_1013/WA345Ta_战场全树生成侧.md` §七·2）。**本函数的做法本身照旧成立**
       （行为零影响），只是那个**对比前提作废**、⛔ 别再把「与 A191 不同」当成理由。"""
    if not items:
        return {'root': arena, 'nodes': [], 'reparent': []}
    groups_paths = groups_paths or set()
    sc = G.Scene(arena)
    qoff = G.quality_off(arena)
    tally = {'psTexNoMesh': 0, 'psInactive': 0, 'psQuality': 0, 'psRenderNone': 0, 'meshQuality': 0}
    built_pts = []                       # [(归一化名, pos[3])] —— **真的会被 `ArenaBuilder` 建出来**的清单条目
    for kind in ('meshes', 'particles'):
        for e in (manifest.get(kind) or []):
            if not e.get('go'):
                continue
            if G.is_built(arena, e, kind, qoff, tally):
                built_pts.append((G.norm(e['go']), e.get('pos')))
    for key in ('light', 'camera'):      # 灯 / 相机**一道闸门都没有**（无条件建，见 `gen_arena_groups.is_built`）
        v = manifest.get(key) or {}
        if isinstance(v, dict) and v.get('name'):
            built_pts.append((G.norm(v['name']), v.get('pos')))

    def is_built_at(name, pos):
        """「这个对象（名字 + 位置）**真的**会被建出来吗」。
        ⚠️ **必须连位置一起比**：同名对象在一场里不止一个（实测 `Embers` 在 arena2 有 3 个、
        `Lightning barrel` 在 tau 有 5 个），只比名字会把**别人**当成它 —— 改挂就会挂错对象。
        容差与 `ArenaBuilder.SameObject` 同一档（`POS_MATCH_TOL`）。"""
        n = G.norm(name)
        for (bn, bp) in built_pts:
            if bn != n:
                continue
            if pos is None or bp is None:
                return True
            if max(abs(bp[i] - pos[i]) for i in range(3)) <= POS_MATCH_TOL:
                return True
        return False

    nodes, reparent = [], []
    seen_nodes = set()
    for it in items:
        try:
            g = G.go_at_path(sc, it['owner'])
        except KeyError as e:                    # `G.Scene.chain` 的兜底（⛔ 不是「已知会炸」）
            # ⚠️ 2026-10-12（A419）订正：这句注释原来写「`G.Scene.chain` 碰上 `RectTransform` 会炸」——
            #   **那个根因已经修掉**（`gen_arena_groups.Scene` 现在把 `RectTransform` 也收进索引：
            #   实测 tauviorla 1377 个 GO 逐个调 `chain()`，「90 个抛 KeyError」→ **0 个**）。
            #   守卫**照留**：`chain` 仍可能因别的原因 `KeyError`（比如某个 transform 的 typetree 读不出
            #   ⇒ 它不在索引里）⇒ 现在这是「**万一**读不了就出声、这条不进 nodes」，不是「已知会炸」。
            print('  ⚠️ `gen_arena_groups.Scene` 读不了这条路径（%s）—— 这条不进 nodes：%s' % (e, it['owner']))
            continue
        if g is None:
            print('  ⚠️ 原版场景里找不到这条路径：%s' % it['owner'])
            continue
        ch = sc.chain(g)
        paths = ['/'.join(n for n, _, _, _ in ch[:i]) for i in range(1, len(ch) + 1)]
        wpos = {}
        for ps in paths:
            gg = G.go_at_path(sc, ps)
            if gg is None:
                continue
            w, _, _ = sc.world_of(gg)
            wpos[ps] = [round(x, 6) for x in w]
        # ---- ① 祖先链 + 宿主自己：只补我们**没有**的 ----
        for i in range(len(ch)):
            leaf = ch[i][0]
            if paths[i] in groups_paths:
                continue                       # A191 的 `_groups.json` 已经把它建出来了
            if is_built_at(leaf, wpos.get(paths[i])):
                continue                       # 已建 ⇒ 运行时按「名字 + 最近位置」对得上，不新建
            if paths[i] in seen_nodes:
                continue                       # 同一场里两条 entry 共用同一个祖先节点
            seen_nodes.add(paths[i])
            ppath = paths[i - 1] if i > 0 else ''
            nodes.append({'path': paths[i], 'name': leaf,
                          'localPos': [round(x, 6) for x in ch[i][1]],
                          'localRot': [round(x, 7) for x in ch[i][2]],
                          'localScale': [round(x, 6) for x in ch[i][3]],
                          'parent': ppath, 'parentPos': wpos.get(ppath, [])})
        # ---- ② 只有「这颗组件真会销毁宿主」时，它的子件才需要改挂回去（见 docstring） ----
        tf = ((it.get('targets') or [{}])[0].get('fields')) or []
        fv = dict((f['k'], f.get('f', 0.0)) for f in tf if 'k' in f)
        if not (fv.get('enabled', 0.0) and fv.get('goActive', 0.0) and not fv.get('preventDestroy', 1.0)):
            continue
        for c in sc.children_of(g):
            try:
                cch = sc.chain(c)
            except KeyError:
                continue
            if not cch:
                continue
            cname = cch[-1][0]
            cw, _, _ = sc.world_of(c)
            cwp = [round(x, 6) for x in cw]
            if not is_built_at(cname, cwp):
                continue                       # 原版关着 / 被闸门挡掉 ⇒ 不在我们树里，没什么可改挂的
            if G.norm(cname) == G.norm(it['ownerLeaf']):
                continue                       # 防呆：别把自己挂到自己下面
            reparent.append({'name': cname, 'pos': cwp,
                             'parent': it['owner'], 'parentPos': wpos.get(it['owner'], [])})
    return {'root': arena, 'nodes': nodes, 'reparent': reparent}


def prefab_names_on_disk():
    out = {}
    for fn in os.listdir(PREFABS):
        if fn.endswith('.prefab'):
            out[fn[:-len('.prefab')]] = os.path.join(PREFABS, fn)
    return out


def norm(s):
    """名字归一化：Unity 的 YAML 会把**带尾随空格/特殊字符**的名字写成 `m_Name: 'Flames '`
    （实测 `Daemonic Feast` 那个 prefab 里就是这么写的）⇒ 比对前先去掉引号与首尾空白。
    ⚠️ 原版的名字**真的有尾随空格**，所以只在**比对**时归一，别去改原值。"""
    return (s or '').strip().strip("'").strip('"').strip()


def check_prefab_side(by_root, unresolved):
    """① prefab 在不在工程里 ② 每个目标的 leaf 名在不在那份 .prefab 文本里（按归一化名比）。"""
    disk = prefab_names_on_disk()
    missing_files, missing_names = [], []
    n_ok = 0
    for root, items in sorted(by_root.items()):
        p = disk.get(root)
        if not p:
            missing_files.append(root)
            continue
        try:
            txt = io.open(p, encoding='utf-8', errors='replace').read()
        except Exception as e:
            missing_files.append('%s（读不了：%s）' % (root, e))
            continue
        names = set(norm(m) for m in re.findall(r'^\s*m_Name:\s*(.+?)\s*$', txt, re.M))
        miss = [t['leaf'] for it in items for t in it['targets'] if norm(t['leaf']) not in names]
        # 🆕 目标组件自己引用的那条**对象**（`ParticleSystemAreaSpawner.particleSystemPrefab` ·
        #    `LookAtConstrainWIP.target`）也得在这件 prefab 里 —— 我们是**运行时**拿它当模板/目标的
        #    （原版那条引用指的就是实例里的那个对象），不在就连它都找不到。
        # 🔴 2026-10-12（A340）**改了判据**：原来这里是「**所有** `s` 都是对象路径」（那时确实如此）；
        #    A340 之后 `s` 还装 **cue 名**（`sounds.<i>.sound`）与**模块类名**（`modules.<i>`）
        #    ⇒ 只查 `FIELD_S_IS_PATH` 那几个键，否则 `Railgun Turret`（cue 名）会被当成
        #    「prefab 里没有这个 GameObject」**误报**。
        for it in items:
            for t in it['targets']:
                for f in (t.get('fields') or []):
                    if f.get('k') not in FIELD_S_IS_PATH:
                        continue
                    s = f.get('s') or ''
                    if s and norm(s.split('/')[-1]) not in names:
                        miss.append(s.split('/')[-1])
        if miss:
            missing_names.append('%s: %s' % (root, '、'.join(sorted(set(miss))[:6])))
        else:
            n_ok += 1
    if missing_files:
        unresolved.append('perfab 不在工程里（%d）：%s' % (len(missing_files), '、'.join(missing_files)))
    if missing_names:
        unresolved.append('prefab 里找不到这些目标名（%d 件）：%s'
                          % (len(missing_names), ' ； '.join(missing_names[:8])))
    return n_ok


def arena_prefab_names(arena):
    """该场 arena prefab 里所有 GameObject 名（归一化）。
    🔴 **这才是运行时真正会去找的那棵树**（`ArenaRuntimeLoader` 实例化的就是它）——
    清单只是「建 prefab 的输入」，而 `Sun flare` / `Directional Light` 这类**不是网格也不是粒子**的
    对象**只在 prefab 里、不在清单里**（`ArenaBuilder` 是分开建它们的）。
    所以「目标在不在」的判据 = prefab ∪ 清单；prefab 没建出来时只退回清单（并出声）。"""
    p = os.path.join(ARENA_PREFABS, arena + '.prefab')
    if not os.path.isfile(p):
        return None
    txt = io.open(p, encoding='utf-8', errors='replace').read()
    return set(norm(m) for m in re.findall(r'^\s*m_Name:\s*(.+?)\s*$', txt, re.M))


def check_scene_side(by_arena, unresolved, missing_targets):
    """场景侧**只查目标**：我们的战场是平铺建的，`Scenario/Particles` 这种**分组节点不在清单里**
    （实测 13/13 场都报 owner 缺失，那是预期，不是缺口）。targets 全在清单里才算过。
    🆕 2026-10-07（A135④）：带 `blendProps` 的目标再核一条 —— **那件网格在我们 arena 清单里
    重建出来的材质，真的带这些属性名**（`_Blend` 就是这么核的）。
    🆕 2026-10-07：存在性判据从「只在清单里」改成 **prefab ∪ 清单**（见 `arena_prefab_names`）。"""
    n_ok = 0
    owners_absent = []
    prop_bad = []
    for arena, items in sorted(by_arena.items()):
        mp = os.path.join(ARENAS, arena, arena + '_manifest.json')
        if not os.path.isfile(mp):
            unresolved.append('场景侧：没有 %s' % mp)
            continue
        d = json.load(io.open(mp, encoding='utf-8'))
        have = set()
        props_of = {}                 # 归一化 go 名 -> 该对象所有子材质里出现过的属性名
        for k in ('meshes', 'particles'):
            for e in (d.get(k) or []):
                if not e.get('go'):
                    continue
                g = norm(e['go'])
                have.add(g)
                s = props_of.setdefault(g, set())
                for sm in (e.get('subMats') or []):
                    for p in (sm.get('props') or []):
                        if p.get('k'):
                            s.add(p['k'])
                for p in (e.get('props') or []):
                    if p.get('k'):
                        s.add(p['k'])
        pn = arena_prefab_names(arena)
        if pn is None:
            print('  （信息）%s 的 arena prefab 没建出来 ⇒ 存在性只按清单判（偏严）' % arena)
        else:
            have |= pn
        miss, ow = [], []
        for it in items:
            if norm(it['ownerLeaf']) not in have:
                ow.append(it['ownerLeaf'])
            for t in it['targets']:
                if norm(t['leaf']) not in have:
                    miss.append(t['leaf'])
                    # 🔴 **已声明的缺口**：目标对象我们工程里**真的没有**（不是没找到）。
                    #    与 `_unresolved` 分开记 —— 后者是「本该对得上却对不上」。
                    #    运行时那一条会**出声**（`EnvironmentApplier` 的解析器返回 null 时会报）。
                    missing_targets.append({
                        'arena': arena, 'cls': it['cls'], 'kind': t['kind'],
                        'leaf': t['leaf'], 'wantedPath': t['path'],
                        'why': '我们工程里没有这个 GameObject（原版的【分组节点】/带 Animation 的父节点；'
                               '我们的战场是 `ArenaBuilder` 按清单**平铺**建的，只建网格/粒子/灯/相机/太阳耀斑）',
                    })
                for p in (t.get('blendProps') or []):
                    if p not in props_of.get(norm(t['leaf']), set()):
                        prop_bad.append('%s：`%s` 上要补间的 `%s` 不在我们清单重建出的材质属性里'
                                        % (arena, t['leaf'], p))
        if ow:
            owners_absent.append('%s(%d)' % (arena, len(ow)))
        if miss and not prop_bad:
            pass                                     # 已进 `missing_targets`，下面统一出声
        elif prop_bad:
            pass                                     # 已进 `prop_bad`
        else:
            n_ok += 1
    if prop_bad:
        unresolved.append('🔴 材质补间的属性名对不上（%d）：%s' % (len(prop_bad), ' ； '.join(prop_bad)))
    if owners_absent:
        print('  （信息）owner 分组节点不在平铺清单里的场：%s —— 预期如此，运行时改挂到目标上'
              % '、'.join(owners_absent))
    return n_ok


def check_scene_standalone(scene_stan, build_by_arena, unresolved, declared):
    """🆕 2026-10-12（A393）`sceneStandalone` 一节的自检（三条）：

      ① 每条目标的**宿主**在不在 —— 判据 = **arena prefab ∪ 清单 ∪ 我们自己要新建的 `nodes[]`**。
         三者都不在 ⇒ 进 `unresolved`（**本该对得上却对不上**）。
      ② `nodes[]` 的父路径：必须在 arena prefab ∪ 清单里，或者是**这条清单里更浅的一个 node**
         （`nodes[]` 按浅→深排 ⇒ 运行时逐个建得出来）。否则 ⇒ `unresolved`。
      ③ `reparent[]` 的名字：必须在 arena prefab ∪ 清单里（生成时已按「名字 + 位置都是真会建出来的」
         筛过，这里再核一遍名字 —— 防的是「筛过了但还是写了个压根不存在的名字」）。

    返回「宿主**原样就在**我们树里」的条数（`stats.scene_standalone_ok`）——
    其余那些靠 `nodes[]` 现场建（**不是缺口**，是「原版有、我们要照建」，记进 `_sceneStandaloneBuild`）。"""
    n_ok = 0
    for arena, items in sorted(scene_stan.items()):
        have = arena_prefab_names(arena)
        if have is None:
            print('  （信息）%s 的 arena prefab 没建出来 ⇒ 场景侧 standalone 存在性只按清单判（偏严）' % arena)
            have = set()
        mp = os.path.join(ARENAS, arena, arena + '_manifest.json')
        if os.path.isfile(mp):
            d = json.load(io.open(mp, encoding='utf-8'))
            for k in ('meshes', 'particles'):
                for e in (d.get(k) or []):
                    if e.get('go'):
                        have.add(norm(e['go']))
            for k in ('light', 'camera'):
                v = d.get(k) or {}
                if isinstance(v, dict) and v.get('name'):
                    have.add(norm(v['name']))
        b = build_by_arena.get(arena) or {'nodes': [], 'reparent': []}
        node_names = set(norm(n['name']) for n in b['nodes'])
        node_paths = set(n['path'] for n in b['nodes'])
        gpaths = group_node_paths(arena)
        for it in items:
            if norm(it['ownerLeaf']) in have or norm(it['ownerLeaf']) in node_names:
                n_ok += 1
            else:
                unresolved.append('🔴 场景侧 standalone：`%s`（%s）的宿主既不在 prefab/清单里、'
                                  '也没进 `nodes[]` ⇒ 运行时建不出来：%s'
                                  % (arena, it['cls'], it['owner']))
        for n in b['nodes']:
            p = n.get('parent') or ''
            if not p:
                continue                                    # 父 = 场根，运行时一定有
            leaf = p.split('/')[-1]
            if norm(leaf) in have or p in node_paths or p in gpaths:
                continue
            unresolved.append('🔴 场景侧 standalone：`%s` 要新建的节点 `%s` 的父 `%s` 既不在树里、'
                              '也不是本清单里更浅的一个 node ⇒ 运行时只能退到场根（出声）'
                              % (arena, n['path'], p))
        for r in b['reparent']:
            if norm(r['name']) not in have:
                unresolved.append('🔴 场景侧 standalone：`%s` 要改挂的 `%s` 不在 prefab/清单里'
                                  ' ⇒ 这条改挂会落空（运行时出声）：%s' % (arena, r['name'], arena))
        if b['nodes'] or b['reparent']:
            declared.append({
                'arena': arena,
                'entries': [it['owner'] for it in items],
                'nodes': [n['path'] for n in b['nodes']],
                'reparent': [r['name'] for r in b['reparent']],
                'why': '原版这 5 条场景侧 `AnimFXController` **不归任何 blendable 管**（见 `SCENE_STANDALONE_CLASSES`），'
                       '我们原来连账都没有。宿主里 4 个的 GameObject 我们工程里**没有** —— 两个原因（判据 = '
                       '`工具/gen_arena_groups.py` 的 `is_built` 对读）：① `RocketTrail` 清单里 `renderMode = 5 (None)` '
                       '（原版根本不画它）、② `Lightning_Green` / `Big Gun Effect` 那一支被「原版关着」那道闸挡掉。'
                       '⇒ 运行时由 `ScenarioBlendableFactory.BuildSceneAnimFx` 照 `nodes[]` **现场建**'
                       '（**不是「有意不做」**，是这一类缺口本来就要补齐）。',
                'fix': '运行时 `ScenarioBlendableFactory.BuildSceneAnimFx`（待接线：调用点见本件报告）',
            })
    return n_ok


def check_standalone(stan_root, unresolved, gaps):
    """🆕 2026-10-07（A137）`standalone` 一节的自检（四条）：
      ① 根在不在我们工程里 —— 不在的进 **`_standaloneMissing`（已声明的缺口）**，不是 `_unresolved`；
      ② 宿主对象（`ownerLeaf`）在那件 `.prefab` 文本里在不在；
      ③ `particleSystemPrefab` 那条引用**两条路至少通一条**：prefab 子树里（同 blender 那 4 条）
         或**工程里另一件独立 prefab**（原版这条引用指的就是 bundle 里另一个 prefab 根 —— 实测 10/24 是这种）；
      ④ controller 的 `spawner.<i>` 必须在**同一个根**的 standalone 条目里（否则运行时要的那颗找不到）。
    """
    disk = prefab_names_on_disk()
    n_ok, no_host, no_tpl, no_ref = 0, [], [], []
    for root, items in sorted(stan_root.items()):
        if root not in disk:
            gaps.append({
                'root': root,
                'n': len(items),
                'clss': sorted(set(i['cls'] for i in items)),
                'leaves': sorted(i['ownerLeaf'] for i in items),
                'why': '这件 prefab 我们工程里没有 —— 🔴 **2026-10-11 记录订正（A211，铁律 5）**：'
                       '原来这里写的是「**有意没导**」，**那是误记**。真因 = `EffectExporter.Run()` 的'
                       '「效果根」过滤器（`!childOf && GetComponentsInChildren<ParticleSystemRenderer>().Length > 0`）'
                       '把它**静默挡掉** —— 实测这件子树里 `ParticleSystemRenderer` = **0 个**'
                       '（工具 `工具/a210_a211_gap.py` 每次重跑都会重算这条）。'
                       '⚖️ 调度台已裁「**要导**」⇒ 已加进 `EffectExporter.ListedPrefabs`、走 `RunListed()` 那条正规通道。'
                       '⚠️ 那两处旧判据（`资料/战场场景线_交接.md:289` 与 `资料/普查产出_0929/进攻卡_数据表.md:30`）'
                       '也已在同一天就地订正。',
                'fix': 'EffectExporter.RunListed（已把根名加进 ListedPrefabs）',
            })
            continue
        txt = io.open(disk[root], encoding='utf-8', errors='replace').read()
        names = set(norm(m) for m in re.findall(r'^\s*m_Name:\s*(.+?)\s*$', txt, re.M))
        own = set(i['owner'] for i in items)
        bad = 0
        for it in items:
            if norm(it['ownerLeaf']) not in names:
                no_host.append('%s`%s`' % (root, it['ownerLeaf']))
                bad += 1
                continue
            for t in it['targets']:
                for f in (t.get('fields') or []):
                    s = f.get('s') or ''
                    if not s or f.get('k') != 'particleSystemPrefab':
                        continue
                    leaf = norm(s.split('/')[-1])
                    if leaf not in names and leaf not in disk:
                        no_tpl.append('%s`%s`→`%s`' % (root, it['ownerLeaf'], leaf))
                        bad += 1
            if it['cls'] == 'ParticleSystemAreaSpawnerController':
                for t in it['targets']:
                    for f in (t.get('fields') or []):
                        k = f.get('k') or ''
                        if k.startswith('spawner.') and f.get('s') not in own:
                            no_ref.append('%s`%s` 的 `%s`=%s 不在本根的 standalone 里'
                                          % (root, it['ownerLeaf'], k, f.get('s')))
                            bad += 1
        if bad == 0:
            n_ok += 1
    if no_host:
        unresolved.append('standalone：这些宿主对象在 prefab 里找不到（%d）：%s'
                          % (len(no_host), '、'.join(no_host[:8])))
    if no_tpl:
        unresolved.append('standalone：这些 `particleSystemPrefab` **两条路都不通**（%d）：%s'
                          % (len(no_tpl), '、'.join(no_tpl[:8])))
    if no_ref:
        unresolved.append('standalone：controller 引用的 spawner 不在同一根里（%d）：%s'
                          % (len(no_ref), '、'.join(no_ref[:8])))
    return n_ok


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='只扫不写')
    a = ap.parse_args()

    scripts = load_script_names()
    print('类名表 %d 条' % len(scripts))
    # 🆕 2026-10-12（A340）：cue 名解析器（`AnimFXController.sounds/exitSounds` 指的那些
    #   `AudioCue` 在**别的包**里 ⇒ 得单独一份，见 `CueNames`）。**按需**：第一次真遇到 cue 才读那个包。
    cues = CueNames()
    # 🆕 2026-10-14（W10/A394）：屏震 preset 名解析器（`AnimFXModuleScreenShake.*.presetSO` 那条跨包引用，
    #   见 `PresetRefs`）。同样**按需**：第一次真遇到 presetSO 才读那个包。
    presets = PresetRefs()

    # ---- prefab 侧 ----
    print('读 %s …' % PREFAB_BUNDLE)
    pb = Bundle(os.path.join(AA, PREFAB_BUNDLE))
    pb.cues = cues
    pb.presets = presets
    by_root = pb.collect(scripts)
    print('  prefab 侧：%d 个根带 blendable' % len(by_root))
    # 🆕 2026-10-07（A137）：同一份 Bundle 上再走一遍 —— **不被 blendable 引用**的
    #   `ParticleSystemAreaSpawner` / `…Controller`（全库实测 24 + 3，全在这个包里）。
    stan_root = pb.collect_standalone(scripts)
    n_stan_sp = sum(1 for v in stan_root.values() for i in v if i['cls'] == 'ParticleSystemAreaSpawner')
    n_stan_ct = sum(1 for v in stan_root.values() for i in v if i['cls'] == 'ParticleSystemAreaSpawnerController')
    print('  standalone：%d 个根 / %d 条（spawner %d + controller %d）'
          % (len(stan_root), n_stan_sp + n_stan_ct, n_stan_sp, n_stan_ct))

    # ---- 场景侧 ----
    by_arena = {}
    arena_warn = {}                        # 场 -> 那个 Bundle 攒下的「出声」（解引用解不出等）
    stan_scene = []                        # 🆕 A137：场景侧**不该有**这一族（实测 0 个）—— 有就出声
    scene_stan = {}                        # 🆕 A393：场景侧**不被任何 blendable 管**的组件（按场）
    for arena in ALL_ARENAS:
        bp = os.path.join(AA, 'scenes_scenes_%s.bundle' % arena)
        if not os.path.isfile(bp):
            print('  跳过 %s（没有 bundle）' % arena)
            continue
        sb = Bundle(bp)
        sb.cues = cues                     # 🆕 A340：这 4 个 `AnimFXController` 全在场景侧（tauviorla）
        sb.presets = presets               # 🆕 A394：场景侧那颗屏震模块的 `presetSO` 也是跨包引用
        got = sb.collect(scripts)
        by_arena[arena] = got.get('Scenario', [])       # 战场内容都在根节点 `Scenario` 下
        # 🆕 2026-10-12（A393）：**场景侧、不被任何 blendable 引用**的组件（同一份 Bundle 上再走一遍）
        ss = sb.collect_scene_standalone(scripts)
        if ss:
            scene_stan[arena] = [i for v in ss.values() for i in v]
        arena_warn[arena] = sb.warnings
        print('  %-32s %d 个组件' % (arena, len(by_arena[arena])))
        # 🆕 A137：场景侧**不该有**这一族 —— 有就点名（运行时只接了 prefab 侧那条路）
        for r, v in sb.collect_standalone(scripts).items():
            if v:
                stan_scene.append('%s/%s（%d 条）' % (arena, r, len(v)))

    unresolved = list(pb.warnings) + list(cues.warnings) + list(presets.warnings)
    #   🆕 2026-10-14（W10/A394）：`presets.warnings` —— 屏震 preset 名解不出的那几条也要出声
    #     （与 A340 的 `cues.warnings` 同一个理由）
    missing_targets = []
    stan_gaps = []
    ok_p = check_prefab_side(by_root, unresolved)
    ok_s = check_scene_side(by_arena, unresolved, missing_targets)
    ok_stan = check_standalone(stan_root, unresolved, stan_gaps)
    if stan_scene:
        unresolved.append('🔴 场景侧出现了 standalone spawner（%d 处）：%s —— 运行时只接了 prefab 侧那条路，'
                          '要收得先给 `EnvironmentApplier` 加场景侧的解析'
                          % (len(stan_scene), '、'.join(stan_scene)))
    for arena in sorted(arena_warn):
        for w in arena_warn[arena]:
            unresolved.append('场景侧 %s：%s' % (arena, w))
    # 🆕 2026-10-12（A393）：场景侧 standalone 那一节的 build 数据（要新建哪些节点 / 哪些子件改挂回去）
    scene_stan_build = []
    for arena in sorted(scene_stan):
        mf_p = os.path.join(ARENAS, arena, arena + '_manifest.json')
        if not os.path.isfile(mf_p):
            unresolved.append('🔴 场景侧 standalone：`%s` 的清单不在（找不到 %s）⇒ 建不出来' % (arena, mf_p))
            continue
        mfd = json.load(io.open(mf_p, encoding='utf-8'))
        try:
            scene_stan_build.append(scene_standalone_build(arena, scene_stan[arena], mfd,
                                                           group_node_paths(arena)))
        except KeyError as e:                    # `G.Scene.chain` 的兜底（A419 之后不再是「RectTransform 那个根因」）
            unresolved.append('🔴 场景侧 standalone：`%s` 算 build 数据时 `gen_arena_groups.Scene` 抛了 '
                              'KeyError(%s)（`chain` 顺着 `m_Father` 走、这一步读不出来）'
                              ' ⇒ 这一场没算出来，要复核' % (arena, e))
    build_by_arena = dict((b['root'], b) for b in scene_stan_build)
    stan_declared = []
    n_ss_ok = check_scene_standalone(scene_stan, build_by_arena, unresolved, stan_declared)
    n_items = sum(len(v) for v in by_root.values()) + sum(len(v) for v in by_arena.values())
    stats = {
        'prefab_roots': len(by_root),
        'prefab_items': sum(len(v) for v in by_root.values()),
        'prefab_roots_ok': ok_p,
        'scene_arenas': len(by_arena),
        'scene_items': sum(len(v) for v in by_arena.values()),
        'scene_arenas_ok': ok_s,
        'by_class': {},
        # 🆕 2026-10-07（A137）：`standalone` 一节（**不被 blendable 引用的** spawner / controller）
        'standalone_roots': len(stan_root),
        'standalone_roots_ok': ok_stan,
        'standalone_items': n_stan_sp + n_stan_ct,
        'standalone_spawners': n_stan_sp,
        'standalone_controllers': n_stan_ct,
        'standalone_missing_roots': len(stan_gaps),
        # 🆕 2026-10-12（A393）：**场景侧**不被任何 blendable 管的组件（`sceneStandalone`）
        'scene_standalone_arenas': len(scene_stan),
        'scene_standalone_items': sum(len(v) for v in scene_stan.values()),
        'scene_standalone_ok': n_ss_ok,
        'scene_standalone_nodes': sum(len(b['nodes']) for b in scene_stan_build),
        'scene_standalone_reparent': sum(len(b['reparent']) for b in scene_stan_build),
    }
    for v in list(by_root.values()) + list(by_arena.values()):
        for it in v:
            stats['by_class'][it['cls']] = stats['by_class'].get(it['cls'], 0) + 1
    print('条目 %d · 按类 %s' % (n_items, stats['by_class']))
    print('standalone：%d 个根（%d 个对上）/ %d 条 = spawner %d + controller %d；缺根 %d'
          % (len(stan_root), ok_stan, n_stan_sp + n_stan_ct, n_stan_sp, n_stan_ct, len(stan_gaps)))
    print('🆕 sceneStandalone（A393）：%d 场 / %d 条（宿主名字在 prefab∪清单里的 %d 条 —— 真正「建出来了没有」'
          '由 `nodes[]` 决定）· 要新建节点 %d 个 · 要改挂回宿主下 %d 个'
          % (len(scene_stan), stats['scene_standalone_items'], n_ss_ok,
             stats['scene_standalone_nodes'], stats['scene_standalone_reparent']))
    for it in [it for v in scene_stan.values() for it in v]:
        print('   · %s' % it['owner'])
    for g in stan_gaps:
        print('  （信息）**已声明缺口** 根 `%s`（%d 条：%s）—— %s'
              % (g['root'], g['n'], '、'.join(g['leaves']), g['why']))
    for g in stan_declared:
        print('  （信息）**场景侧 standalone**（A393）：场 `%s` 有 %d 条 —— 要新建节点 %s · 改挂 %s'
              % (g['arena'], len(g['entries']), '、'.join(g['nodes']) or '（无）',
                 '、'.join(g['reparent']) or '（无）'))

    if missing_targets:
        seen = []
        for m in missing_targets:
            s = '%s（%s）' % (m['leaf'], m['cls'])
            if s not in seen:
                seen.append(s)
        print('（信息）**已知缺口** %d 条：这些目标对象我们工程里没有 ⇒ 运行时那条会出声、不生效。'
              '要收得先补 `ArenaBuilder` 的建场（本批不在名单内）：%s'
              % (len(missing_targets), '、'.join(seen)))
    if unresolved:
        print('⚠️ 未对上：')
        for u in unresolved:
            print('   -', u)
    else:
        print('✅ 两侧自检全过（`_unresolved` 空；已知缺口见上面那条「（信息）」与 `_missingTargets`）')

    if a.check:
        return 0

    out = {
        '_schema': 'env_blendables/1 —— 环境混合组件（IScenarioEnvironmentBlendeable）的旁挂表；'
                   'prefabs=43 件环境 prefab（路径相对 prefab 根）· scene=13 场战场（名字+世界位置，'
                   '因为我们的战场是平铺建的）· 每个 target 的 `fields` = **那个目标组件自己的**序列化字段'
                   '（`k` 原版字段名 · `f` 数值 · `s` 引用型字段落点的层级路径；Vector3 拆 x/y/z 三条。'
                   '当前有 `ParticleSystemAreaSpawner`（6 个）· `ParticleSystemAreaSpawnerController`（2 个）·'
                   '🆕 2026-10-11（A196）`LookAtConstrainWIP`（6 个：target/lockXAxis/lockYAxis/lockZAxis/'
                   'upVector/rotationOffset）· `AnimFXController`（3 个：preventDestroy/destroyTime/exitDestroyTime）'
                   ' —— 见 gen 脚本的 TARGET_FIELDS）'
                   '· 🆕 2026-10-12（A340）`AnimFXController` 的**三层**也在 `fields` 里（摊平、键带下标）：'
                   '`sounds.count` / `exitSounds.count` / `modules.count` = 原版那层的条数（**缺键 = 旁挂没收这一层**）· '
                   '`sounds.<i>.sound` = **cue 名**（跨包解出来的 `AudioCue` 名 = `animfx_sounds.json` 的键；'
                   '空串 = 原版那条就是空引用）· `sounds.<i>.soundUnresolved` = 1 表示那条 cue **解不出来**'
                   '（运行时据此出声）· `sounds.<i>.{time,is2d,repeat,loops,timeInterval}` = '
                   '`PlaySoundOnTime` 的其余字段 · `modules.<i>` = 模块**类名**（运行时据此**真建组件**：'
                   '`WarpforgeVFX.WFSceneModuleFactory`）'
                   '· 🔴 🆕 2026-10-14（W10/A394）`modules.<i>.<点号键>` = **模块自己**的序列化字段'
                   '（键的语法与 `数据/游戏数据/animfx_modules.json` **逐字一致**：数组下标带方括号；'
                   '对象引用写成 `@node:`/`@asset:`；**解不出的引用**写一条 `<键>Unresolved` = 1、'
                   '⛔ 不写空串 —— 与 `sounds.<i>.soundUnresolved` 同一口径）'
                   '· 🆕 2026-10-07：`Item.floats` = **组件自己**的小数字段（同上形制；`Field.v` 是 int、'
                   '装不下 `blendTime 0.3` 与 `finalRotation` 那种 Vector3）· `Target.blendProps` / '
                   '`Target.customMaterial` = 原版 `RendererMaterialBlender.propertiesToBlend` / '
                   '`customMaterial`（只有 `ScenarioGenericMaterialBlend` 用）'
                   '· 🆕 2026-10-07（A137）`standalone` = **不被任何 blendable 引用**的 '
                   '`ParticleSystemAreaSpawner` / `…Controller`（全库 24 + 3；形制与 `prefabs` **完全一样**，'
                   '每个条目**只有一个 target = 组件自己那个 GameObject**，`kind` 仍是 `spawner` / `controller`；'
                   '6 个（controller 是 2 个 + 摊平的 `spawner.<i>`/`weight.<i>`/`chances.<i>`）字段在 '
                   '`targets[0].fields`）—— 运行时按**同一套 `MakeSpawner`** 建，**不是** blendable'
                   '· 被 blendable 引用的那 4 条**不收进** `standalone`（收了会在同一个对象上建两次）'
                   '· 🆕 2026-10-12（A393）`sceneStandalone` = **场景侧**不被任何 blendable 管的 '
                   '`AnimFXController`（全库 7 个里除了 tauviorla 那 2 个炮塔、剩 **5** 个；排除按 **pid 引用关系**、'
                   '不是写死的名单）。形制与上面几节**完全一样**（`EnvBlendables.Group`，每个条目**只有一个 '
                   'target = 组件自己那个 GameObject**、`kind` 仍是 `animfx` ⇒ 运行时**同一个 `MakeAnimFx`** 建它们），'
                   '只多两条**不属于组件本身**的开关：`targets[0].fields` 里的 `enabled`（组件 `m_Enabled`）'
                   '与 `goActive`（**沿父链与过**的 `activeInHierarchy`）—— 原版这 5 个里有 3 个根本不跑，'
                   '不记下来就会建出「原版不跑、我们跑」的假象'
                   '· 🆕 2026-10-12（A393）`sceneStandaloneBuild` = 上面那一节**要真的建出来**还差什么：'
                   '`nodes[]`（宿主/祖先我们工程里没有 ⇒ 要新建的节点，浅→深 · **原版 local TRS**）· '
                   '`reparent[]`（原版挂在宿主下面、我们已建出来的对象 ⇒ 改挂回宿主下；`RocketTrail` 那颗的 '
                   '`destroyTime = 6` 一旦生效就要**连带 6 个子件粒子一起消失**）'
                   '—— ⚠️ `nodes[]` **只补祖先链 + 宿主自己、不展开子树**'
                   '（⚠️ 2026-10-13 更正（A595）：这里原来写「与 A191 那条 `wanted_paths` 的范围**有意不同**」'
                   '—— A345 之后 `wanted_paths` 已放宽到**全覆盖**，那个对比前提作废；本做法照旧。'
                   '理由写在 `scene_standalone_build` 的 docstring 里）',
        '_sources': {
            'prefab_bundle': PREFAB_BUNDLE, 'scene_bundles': 'scenes_scenes_<场>.bundle',
            'class_names': MONO_BUNDLE + ' 的 MonoScript.m_ClassName',
            'aa': AA,
            'standalone_scope': 'A137：这一族全库 28 + 3 个实例**全在** `%s` 里（2026-10-07 用独立探针'
                                '扫过 aa 下全部 84 个 bundle，场景侧 0 个）—— 本脚本只扫它已经在读的'
                                '「prefab 包 + 13 个场景包」，够用；场景侧另有一道「出现了就出声」的闸' % PREFAB_BUNDLE,
        },
        'prefabs': by_root,
        'scene': by_arena,
        # 🆕 2026-10-07（A137）：**不被 blendable 引用**的 spawner / controller（形制同 `prefabs`）
        'standalone': stan_root,
        # 🆕 2026-10-12（A393）：**场景侧**不被任何 blendable 管的组件（形制同 `standalone`）
        'sceneStandalone': dict((a, v) for a, v in sorted(scene_stan.items())),
        'sceneStandaloneBuild': scene_stan_build,
        'stats': stats,
        '_unresolved': unresolved,
        # 🆕 2026-10-07（A136）：**已声明**的目标缺口 —— 旁挂里指着的对象我们工程里真的没有
        #   （原版的【分组节点】：`Sun flare` 那种有、`Railgun Turret 1` / `Battle Arena Dark Angels baked`
        #    那种没有）。与 `_unresolved`（「本该对得上却对不上」）**分开记**，运行时那一处会出声。
        #   要补 = 改 `ArenaBuilder` 的建场（会给既有断言带出连锁）⇒ 单开一件，见波9离线报告。
        '_missingTargets': missing_targets,
        # 🆕 2026-10-07（A137）：`standalone` 里那些**整件 prefab 我们没导**的根 —— 同样是**已声明**的缺口
        #   （`Particles Orbital` 是原版的公共件，原版把它内联进别的 prefab，我们有意不单独导）
        #   ⇒ 与 `_unresolved`（「本该对得上却对不上」）分开记，运行时根本不会实例化那件 prefab。
        '_standaloneMissing': stan_gaps,
        # 🆕 2026-10-12（A393）：`sceneStandalone` 那些**宿主我们工程里没有**的条目 —— 同样是**已声明**的
        #   缺口（原版有、要照建）。⚠️ **与 `_missingTargets` 分开记**：那张表是 `gen_arena_groups.py`
        #   的输入（它会把整棵子树展开成空节点）⇒ 把这几条塞进去会**改变 A191 已验收的那两份 groups 旁挂**。
        #   这几条走运行时那条路（`sceneStandaloneBuild.nodes[]`）。
        '_sceneStandaloneMissing': stan_declared,
    }
    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(
        json.dumps(out, ensure_ascii=False, indent=1))
    print('写出 %s' % OUT)

    # ---- 摊平版：`JsonUtility` 读不了字典，只认固定字段 + 数组（与 EnvironmentConditions.json 同一套做法）----
    def flat_tfield(f):
        """目标组件的一条字段：`k` 恒有；数值走 `f`、引用型走 `s`（见 `TARGET_FIELDS` 那段注释）。"""
        o = {'k': f['k']}
        if 'f' in f:
            o['f'] = f['f']
        if 's' in f:
            o['s'] = f['s']
        return o

    def flat_item(it):
        o = {
            'cls': it['cls'], 'owner': it['owner'], 'ownerLeaf': it['ownerLeaf'],
            'ownerPos': it['ownerPos'],
            'fields': [{'k': k, 'v': 1 if v is True else (0 if v is False else int(v))}
                       for k, v in (it.get('fields') or {}).items()],
            'targets': [flat_target(t) for t in it['targets']],
        }
        # 🆕 2026-10-07（A136）：本组件自己的**小数字段**（`Field.v` 是 int，装不下 0.3 / Vector3）
        if it.get('floats'):
            o['floats'] = [flat_tfield(f) for f in it['floats']]
        return o

    def flat_target(t):
        o = {'path': t['path'], 'leaf': t['leaf'], 'kind': t['kind'], 'pos': t['pos']}
        if t.get('fields'):
            o['fields'] = [flat_tfield(f) for f in t['fields']]
        # 🆕 2026-10-07（A135④）材质补间那一层（只有 `ScenarioGenericMaterialBlend` 有）
        if t.get('blendProps'):
            o['blendProps'] = t['blendProps']
        if t.get('customMaterial'):
            o['customMaterial'] = t['customMaterial']
        return o

    flat = {
        'prefabs': [{'root': r, 'items': [flat_item(i) for i in items]}
                    for r, items in sorted(by_root.items())],
        'scene': [{'root': a, 'items': [flat_item(i) for i in items]}
                  for a, items in sorted(by_arena.items())],
        # 🆕 2026-10-07（A137）：`standalone` 一节 —— **形制与上面两组完全一样**（`EnvBlendables.Group`），
        #   所以运行时读它不用再定义一份 DTO（`EnvironmentApplier` 直接用 `EnvBlendables.Group[]` 接）。
        #   ⚠️ 它**不进** `EnvBlendables.ForPrefab` —— 那些条目不是 blendable，混进去会让工厂对它们出声。
        'standalone': [{'root': r, 'items': [flat_item(i) for i in items]}
                       for r, items in sorted(stan_root.items())],
        # 🆕 2026-10-12（A393）：场景侧那一节 —— **形制与上面三组完全一样**（`EnvBlendables.Group`），
        #   所以运行时读它不用再定义一份 DTO（与 `standalone` 同一个做法）。
        #   ⚠️ 同理**不进** `EnvBlendables.ForArena`（那些条目不是 blendable）。
        'sceneStandalone': [{'root': r, 'items': [flat_item(i) for i in items]}
                            for r, items in sorted(scene_stan.items())],
        # 🆕 2026-10-12（A393）：上面那一节**要真的建出来**还差什么（要新建的节点 / 要改挂回去的对象）。
        #   `JsonUtility` 只认固定字段 ⇒ 这两张表也用**固定字段的数组**（键名与 `gen_arena_groups.py`
        #   的 `nodes[]`/`targets[]` 同一套，但**只在「祖先链 + 宿主自己」这一小段上**，见
        #   `scene_standalone_build` 的 docstring）。
        'sceneStandaloneBuild': [{
            'root': b['root'],
            'nodes': [{'path': n['path'], 'name': n['name'],
                       'parent': n['parent'], 'parentPos': n['parentPos'],
                       'localPos': n['localPos'], 'localRot': n['localRot'],
                       'localScale': n['localScale']} for n in b['nodes']],
            'reparent': [{'name': r['name'], 'parent': r['parent'],
                          'pos': r['pos'], 'parentPos': r['parentPos']} for r in b['reparent']],
        } for b in scene_stan_build],
    }
    io.open(FLAT, 'w', encoding='utf-8', newline='\n').write(
        json.dumps(flat, ensure_ascii=False, indent=1))
    print('写出 %s（摊平版，%d prefab + %d 场 + standalone %d 根 / %d 条 + sceneStandalone %d 场 / %d 条）'
          % (FLAT, len(flat['prefabs']), len(flat['scene']),
             len(flat['standalone']), n_stan_sp + n_stan_ct,
             len(flat['sceneStandalone']), stats['scene_standalone_items']))
    return 0


if __name__ == '__main__':
    sys.exit(main())
