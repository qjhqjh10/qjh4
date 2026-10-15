#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_card_vfx_by_card.py — 「逐卡 VFX 表」生成器（卡 id → 事件 → 本地 prefab 名）

用法（本项目惯例）：
    PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_card_vfx_by_card.py
    # 可选 --quiet   不打明细

只读（绝不写 d:/2/）：
  ① d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json    —— 我们的卡池（1126 张，id 就是原版 id）
  ② d:/4/Unity/数据/索引/anim_address_map.json                          —— CardAnim → (AssetGUID →) 本地 prefab
        （由 `工具/gen_anim_address_map.py` 生成；本脚本**不重建**它，只消费）
  ③ d:/4/Unity/数据/游戏数据/offensive_cards.json                       —— 进攻卡（= 环境效果卡）→ CardAnim → prefab
  ④ d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/
        · MonoBehaviour_1744609728290659264.json   —— **共享的卡预制体 `CardPrefab` 上的 `CardScript`**
          （原版每张卡都走的那套通用挂钩：normalSummon / landOnGroundParticles /
            attackHitSmallParticles / cardDestroyFX / rangedAttackParticlesSO…）
        · RangedAttackParticlesByArmy.json          —— 阵营 → 远程攻击粒子（CardAnim）
        · BattleAnims.json                          —— 全局动画槽（heal 有值，其余空引用）
  ⑤ d:/2/Warpforge_tools/data/vfx_wiring_j1..5.tsv —— 0824 那张「特效名 → 原版触发事件」台账（**人工语义判断**）。
      ⚠️ 实测它对**逐卡那一层的名字覆盖极差**（抽 9 个逐卡动画名，只有 1 个有行）⇒
      **只当旁证**：有行就在那条 entry 上带 `ledgerCategory`/`ledgerTrigger`，**不参与任何判定**。
  ⑥ 原 bundle 二进制（UnityPy 重读，只为了把 PPtr 的 m_PathID 反解成名字）
  ⑦ 🔴 **权威档（2026-10-14 新增，`conf = authoritative`，优先级最高）**：
        · d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json
             —— **原版 `RawCardScript` 全字段**（1,195 张卡 SO；Unity 2022.3.38f1 / 缓存 2025-07-30）
        · d:/4/Unity/资料/普查产出_第二十七会话/guid_map.tsv
             —— 卡 SO 的 `AssetGUID` → 束名 / 容器键 / **资产原名**（4,985 行 / 2,372 GUID / 26 束）
               由 `工具/_guid_resolve_probe.py map` 重建（约 8 秒）。本脚本**不重建**它，只消费。
     这两份 + `anim_address_map.json` 的 `cardanim_to_asset` = **逐卡那一跳的原版真值**
     （链路与覆盖率见下面「第 2 跳」那段更正文 + `资料/普查产出_第二十七会话/逐卡VFX_改用真值_报告.md`）。
  ⑧ `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{AbilityTrigger,AbilityEffect}.cs`
        —— 枚举名字表（签名桩）。**只为了把卡 SO 里的枚举数翻译成名字**；读不到就退回只记数字，不猜。

输出（只写这一个文件）：
    d:/4/Unity/数据/游戏数据/card_vfx_by_card.json

🔴 **先说清楚这份表是什么、不是什么**（详见 `资料/普查产出_0929/逐卡VFX表.md`）：

  原版那条链是
      卡 → `RawCardScript` 上的 `summonAnimDelayed` / `targetedSpellAnim` /
           `nonTargetedSpellAnim` / `customRangedAttackParticles` / `alternateArts[].attackAnim`
         + `cardAbilities[].cardAnimations`（AbilityLogic）
         + `CardTrait.TraitEffects[].effectAnim`（天赋触发）
       → `CardAnim` → `animInfo.animAdressable` → VFX prefab

  🔴🔴 **2026-10-14 更正（铁律 5；推翻本文件原来那句结论）**
      **原来写**：「**第 2 跳（卡 → 它自己的 animInfo 列表）在本地断掉**：那些卡预制体只在**远端 CCD** 的
        `allcards_assets_all.bundle` 里（本地 84 个包里没有，`catalog_main.json` 明写它走 CCD；
        Addressables 缓存目录里也只有 catalog、没有包）。⇒ **逐卡覆盖拿不到**。」
      **实际是**：**那一跳从来没断** —— 卡 SO 的字段本身就是 `AssetReference`（存的是 **GUID**），
        而「**GUID → 束 → 容器键 → 资产原名**」这一跳**一直可解**（`工具/gen_anim_address_map.py`
        2026-10-03 就建好了 `guid_to_asset` 10,325 条 / `cardanim_to_asset` 1,099 条），
        第二十七会话又补了 **`guid_map.tsv`**（卡 SO 字段 → 束 / 容器键 / 资产原名，4,985 行）。
        逐卡真值覆盖率实测：`customRangedAttackParticles` 484/484 · `customDeathParticles` 72/72 ·
        `remnantBodyPrefab` 73/73 · `cardAbilities[].abilityLogic[].cardAnimations` 1,359 条 / 796 张。
      **错因**：**把「载体的文件不在本地」当成了「那一跳断掉」**。当时只找了「卡预制体」这一个载体
        （`allcards_assets_all.bundle` 确实在远端 CCD，`cardCatalog` 里明写），
        **没意识到同一批绑定在卡 SO 的字段里以 GUID 形式就摆在本地**
        —— 而 GUID 反查资产名从来不需要那只远端包（走本地 84 个 `.bundle` 的 `m_Container`）。
        证据补记：`catalog_main.json` 的 **`m_InternalIds` 里 12,319/12,653 条的 internalId 就是主键 GUID 本身**，
        所以「GUID → 束」这一步连 CCD 都不用碰。
      ⇒ **结论作废，改为**：逐卡那一跳**能拿到**，而且已经落在下面的 `conf = authoritative` 那一档；
        旧的 `inferred-*` 三档**保留**（各有各的消费点），但**降级为兜底**，优先级排在 `authoritative` 之后。

  ⇒ **所以这份表现在是四层拼起来的**，每一条都带 `conf`，**按 `conf` 过滤，别混用**
    （`_conventions.confPriority` 给了优先级顺序）：
    · `byCard[*]` conf=**`authoritative`** —— 🔴 **原版真值**：卡 SO 字段（GUID）→ 束 → 资产原名 → VFX prefab。
      **不是推断**。但 ⚠️ **两个限定**：① 真值快照是 2022 时代（1,195 张），**我们卡池 1,126 张里有 168 张比它新**
      ⇒ 那些卡在这一档下**查不到**（如实标，不猜）；② `cardAnimations` 那一路的**事件名归到 `ability` 是我们拍的**
      （原版给的是 `CardAbility.trigger` 枚举，已逐条带在条目上的 `abilityTrigger`/`chosenAbility` 里）。
    · `byCard[*]` conf=`inferred-name`    —— 卡名 → 同名 CardAnim（**推断**，覆盖很少）。
    · `byCard[*]` conf=`inferred-keyword` —— 卡的关键词 → `<关键词>TraitStart/TraitTrigger/…`（**推断**；原版天赋命名的
      构词法，但「哪个关键词配哪个 anim」这一步本地判不了）。
    · `byCard[*]` conf=`inferred-envcard` —— 进攻卡；⚠️ 原版那张 SO 的卡是 PPtr，**解不出卡名**，
      所以是用卡面贴图名反推的候选名去对（可能对不上）。
    · `generic`            —— **判据齐**：共享 `CardPrefab` / 各 SO 上的**通用**每事件挂钩（**不是逐卡**）。
"""

import argparse
import collections
import io
import json
import os
import re
import sys
import time

# ---------------------------------------------------------------- 常量 / 路径

ASSETS_FULL = r"d:/2/新解包资源/assets_full"
BATTLEPREFS = "bundle_battleprefabs_vfxandmisc_assets_all"
CARD_SCRIPT_MONO = os.path.join(ASSETS_FULL, BATTLEPREFS, "MonoBehaviour",
                                "MonoBehaviour_1744609728290659264.json")
RANGED_SO_MONO = os.path.join(ASSETS_FULL, BATTLEPREFS, "MonoBehaviour",
                              "RangedAttackParticlesByArmy.json")
BATTLE_ANIMS_MONO = os.path.join(ASSETS_FULL, BATTLEPREFS, "MonoBehaviour",
                                 "BattleAnims.json")

BUNDLE_SRC_CANDIDATES = [
    r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64",
    r"d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64",
]

CARDS_ENGINE = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
ADDR_MAP = r"d:/4/Unity/数据/索引/anim_address_map.json"
OFFENSIVE = r"d:/4/Unity/数据/游戏数据/offensive_cards.json"
CARD_INDEX = r"d:/4/Unity/数据/索引/card_index.json"
LEDGER_GLOB_DIR = r"d:/2/Warpforge_tools/data"
VFX_LIB = r"d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs"   # 只读，用来查「效果库里有没有这件」

OUT_JSON = r"d:/4/Unity/数据/游戏数据/card_vfx_by_card.json"
# 🆕 **2026-10-14（第二十七会话）：运行时那一份**（`Resources.Load` 要能取到 ⇒ 必须落在 `Resources/` 下）。
#   为什么必须由**本脚本**写、而不是另写一个投影脚本：运行时那份与这份**是同一件事的两种形态**
#   （事件名 / `conf` 优先级 / 卡 id 都要**逐字一致**），两处各算一次**迟早分叉**（本仓红线）。
#   ⚠️ 它**只保留运行时用得上的字段**（`prefab` / `conf` / `cardanim` 三个，🆕 2026-10-14 起
#      另加 A1817 的逐卡精确语音 `audio`），
#      全量那份（含 `_assetRefs` 的立绘/语音引用、`soField`、`guid` 等）留在 `数据/` 下给工具用。
OUT_RUNTIME = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json"

# ---- 🔴 权威档（原版真值）的两份输入（2026-10-14 新增；**只读，不重建**）
CARD_SO_TRUTH = r"d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json"
GUID_MAP_TSV = r"d:/4/Unity/资料/普查产出_第二十七会话/guid_map.tsv"
# 枚举名字表（签名桩；读不到就只记数字，不猜）
ENUM_STUBS = r"d:/2/Warpforge_code/Scripts/Assembly-CSharp"

# `conf` 的优先级（从高到低）。**消费者按这个顺序取第一个命中的**，别把推断当真值。
CONF_PRIORITY = ["authoritative", "exact", "exact-not-per-card",
                 "inferred-envcard", "inferred-name", "inferred-keyword"]

# 卡 SO 里**带 VFX 绑定**的顶层字段 → (我们的事件名, 一句解释) —— 定义见下面 `EV_*` 之后
# 卡 SO 里**不带 VFX**、但同样是「逐卡资产绑定」的字段 → 收进 `byCard[*]._assetRefs`（**不挂事件**）

# 与 `CardPresentation/Core/VfxMap.cs` 的常量**逐字一致**（那边是唯一一份事件名清单）
EV_PLAY = "play"
EV_DEPLOY = "deploy"
EV_ATK_MELEE = "attack_melee"
EV_ATK_RANGED = "attack_ranged"
EV_HIT = "hit"
EV_DEATH = "death"
EV_ABILITY = "ability"
EV_TRIGGER = "trigger"

# 卡 SO 里**带 VFX 绑定**的顶层字段 → (我们的事件名, 一句解释)
# 事件名归口的出处：`customRangedAttackParticles` 与 `CardScript.rangedAttackParticlesSO` 是同一件事的
# 「逐卡覆盖 / 阵营档」两个来源（见 `build_generic` 的 `attackRangedByArmy`），所以归 `attack_ranged`；
# 另外两个是阵亡那一拍的两个落点（爆散粒子 / 留在格上的残骸体），归 `death`。
SO_VFX_FIELDS = [
    ("customRangedAttackParticles", EV_ATK_RANGED,
     "`RawCardScript.customRangedAttackParticles`（逐卡远程攻击粒子，覆盖 `CardScript.rangedAttackParticlesSO` 的阵营档）"),
    ("customDeathParticles", EV_DEATH,
     "`RawCardScript.customDeathParticles`（逐卡阵亡粒子）"),
    ("remnantBodyPrefab", EV_DEATH,
     "`RawCardScript.remnantBodyPrefab`（阵亡后留在格上的残骸体）"),
    ("summonAnimDelayed", EV_DEPLOY,
     "`RawCardScript.summonAnimDelayed`（逐卡登场延迟特效）"),
]
# 卡 SO 里**不带 VFX**、但同样是「逐卡资产绑定」的字段 → 收进 `byCard[*]._assetRefs`（**不挂事件**）
SO_ASSET_FIELDS = [
    ("cardImageReference", "art", "原版立绘贴图（同一 GUID 解出 Sprite + Texture2D，同名）"),
    ("summonAudio", "audio", "逐卡登场语音"),
    ("attackAudio", "audio", "逐卡攻击语音"),
    ("deathAudio", "audio", "逐卡阵亡语音"),
]
SO_ASSET_KIND = dict((f, k) for f, k, _ in SO_ASSET_FIELDS)

# 🔴 2026-10-15（账 `A2083`）：**这些 SO 字段不进【运行时】那一份**。
#
# 起因：`remnantBodyPrefab` 原来和 `customDeathParticles` **共用 `death` 这一个键**，
# 于是运行时只能二选一 —— 而这两件事在**原版是两个不同的触发时刻**：
#   · `customDeathParticles` —— **阵亡那一刻**（`CardScript.UnitDeath`，反编译实证）
#   · `remnantBodyPrefab`   —— **「转化为残骸」那一刻**（`BattleCardUI.CreateRemnantBody`，
#     调用者 `CardScript.TransformIntoRemnant`），`UnitDeath` 全程没碰它
# 实测（全量件）：`death` 档 137 条 = `customDeathParticles` 66 + `remnantBodyPrefab` 71，
# 其中 **42 张卡两者都有** ⇒ 谁进 `death` 取决于折叠顺序，**是掷硬币**。
# 而 66 条 `customDeathParticles` 解出来全是同一件 `Necrons death explosion` ——
# 它才是原版「有逐卡覆盖就用它、否则才用默认 `Card 3D Death Explosion`」的那一半
# （判别式 → `CardScript._UnitDeath_d__446__MoveNext.c:200/:233/:240/:262`）。
#
# ⇒ 运行时这一份**只留 `customDeathParticles`**；`remnantBodyPrefab` 仍在**全量件**里
#   （带 `soField`，随时可查），消费口 = `CardFeel.SpawnDeathBody`。
#   ⚠️ **逐卡残骸体今天没接**（我们走的是按关键词的 `RemnantBody3D Aeldari/Necrons`，
#      `BattleDriver.cs:12215-12223`）—— 那是**另一笔账**，⛔ 别拿这条当「已经做了」。
RT_SKIP_SOFIELDS = {"remnantBodyPrefab"}

# 🆕 **2026-10-14（A1817）**：卡 SO 里那三个**语音**字段 → 我们的**事件名**。
#   为什么要一张映射表而不是让消费者自己判 `soField`：运行时（`Core/VoiceLines.cs` 的精确档）
#   拿到的必须是**我们的事件名**（它要按 `EvtKind.Deploy/Attack/Death` 查），
#   而 `soField` 是**原版字段名** —— 两者之间**只此一处**翻译（两处各写一次迟早分叉，本仓红线）。
#   ⚠️ 这三个事件名与 `VfxMap` 的 `deploy`/`death` **同名但是另一条轴**：
#      `events` 是「播哪个 prefab」，`audio` 是「说哪句话」，⛔ 别混成一张表。
#   ⚠️ **只有这三个** —— `deathAudio` 是**语音** `VO_*`，**不是**阵亡音效
#      （`minionDeathCUE` 那一族 SFX 是另一件活，本表不收）。
AUDIO_SO_FIELD_EVENT = {
    "summonAudio": "deploy",
    "attackAudio": "attack",
    "deathAudio":  "death",
}
# 反向：我们的事件名 → 原版字段名（只为打印 / 出错信息用，别拿它当查表键）
AUDIO_EVENT_SO_FIELD = dict((v, k) for k, v in AUDIO_SO_FIELD_EVENT.items())

# `CardTrait.EffectTriggerType`（签名桩 `CardTrait.cs`）：Start0 / End1 / OnPlay2 / Trigger3 / OnHand4 / FromCode5 / TriggerOnPlay6
TRAIT_SUFFIX_EVENT = {
    "traitstart": ("trigger", "EffectTriggerType.Start(0)"),
    "traitplay": ("ability", "EffectTriggerType.OnPlay(2)"),
    "traittrigger": ("trigger", "EffectTriggerType.Trigger(3)"),
    "traittriggeronplay": ("trigger", "EffectTriggerType.TriggerOnPlay(6)"),
}
# `<关键词>_Effect` / `<关键词>_Proc` / `<关键词> Effect` —— 原版这批名字归属哪一类**判不了**，
# 单独归到 `trait_effect_unclassified`，不硬塞进 play/ability/trigger。
TRAIT_LOOSE_SUFFIX = {"effect", "proc"}
# 名字里后缀的**原样**拼法（归一化会把它压成小写，交结论时要给回原样，别让读者以为是 bug）
TRAIT_SUFFIX_DISPLAY = {"traitstart": "TraitStart", "traitplay": "TraitPlay",
                        "traittrigger": "TraitTrigger",
                        "traittriggeronplay": "TraitTriggerOnPlay"}


def in_vfx_library(prefab_name):
    """效果库里有没有这件 prefab（`Assets/WarpforgeVFX/Prefabs/<名>.prefab`）。**只读，不建不改**。

    这条很重要：表里给出的是**原版资产名**，而能不能马上播取决于它导没导进我们的库 ——
    不许静默失败，所以逐条标出来。
    """
    if not prefab_name:
        return None
    return os.path.isfile(os.path.join(VFX_LIB, prefab_name + ".prefab"))


def mark_library(gen):
    """给 `generic` 里每条加 `inLibrary`，并汇总缺口。"""
    missing = []
    for evt, lst in gen["byEvent"].items():
        for it in lst:
            ok = in_vfx_library(it.get("prefab"))
            it["inLibrary"] = ok
            if ok is False:
                missing.append("%s (事件 %s ← %s)" % (it["prefab"], evt, it.get("src")))
    for k, v in gen.get("attackRangedByArmy", {}).items():
        ok = in_vfx_library(v.get("prefab"))
        v["inLibrary"] = ok
        if ok is False:
            missing.append("%s (远程攻击 · 阵营 %s)" % (v.get("prefab"), k))
    for it in gen.get("deploySummonCandidates", []):
        it["inLibrary"] = in_vfx_library(it.get("prefab"))
    gen["notInVfxLibrary"] = missing
    return gen


def rel(p):
    return p.replace("\\", "/")


def norm(s):
    """归一化：只留小写字母数字。`Acid Spray` → `acidspray`，`Traitors' Hate` → `traitorshate`"""
    return re.sub(r"[^a-z0-9]", "", (s or "").lower())


def read_json(p):
    with io.open(p, encoding="utf-8") as fh:
        return json.load(fh)


def find_bundle_src():
    for d in BUNDLE_SRC_CANDIDATES:
        if os.path.isdir(d):
            return d
    return None


# ---------------------------------------------------------------- 索引：CardAnim → prefab / path_id → 名字


class AddrIndex:
    """消费 `anim_address_map.json`（**不重建**）。"""

    def __init__(self, path):
        j = read_json(path)
        self.cardanim = j.get("cardanim_to_asset") or {}
        self.guid = j.get("guid_to_asset") or {}
        self.by_pathid = {}
        for guid, v in self.guid.items():
            for r in [v] + (v.get("also") or []):
                pid = r.get("pathId")
                if pid:
                    self.by_pathid.setdefault(int(pid), r)

    def prefab_of(self, cardanim):
        """CardAnim 名 → 本地 prefab 名（解不出返回 None）"""
        r = self.cardanim.get(cardanim)
        if not r:
            return None
        return r.get("targetName")

    def asset_of_guid(self, guid):
        return self.guid.get((guid or "").strip().lower())

    def asset_of_pathid(self, path_id):
        return self.by_pathid.get(int(path_id))


def unitypy_names(bundle_src, bundle_dir, wanted_pathids):
    """用 UnityPy 重读原 bundle，把 m_PathID 反解成 (类型, 名字)。

    只在 `anim_address_map.json` 的 path_id 索引漏了的时候用（它只收 Addressables 容器项，
    非容器对象不在里面 —— 实测 `Card 3D Death Explosion`、`normalSummon` 就漏）。
    UnityPy 不在 / 读不动 ⇒ 返回空 dict，调用方如实记 `unresolved`，不猜。
    """
    if not wanted_pathids:
        return {}, None
    src = os.path.join(bundle_src, bundle_dir[len("bundle_"):] + ".bundle")
    if not os.path.isfile(src):
        return {}, "源 bundle 不存在: %s" % rel(src)
    try:
        import UnityPy
    except Exception as e:
        return {}, "UnityPy 不可用: %s" % e
    try:
        env = UnityPy.load(src)
        objs = {o.path_id: o for o in env.objects}
    except Exception as e:
        return {}, "UnityPy load 失败: %s" % e
    out = {}
    for pid in sorted(wanted_pathids):
        o = objs.get(pid)
        if o is None:
            continue
        nm = None
        try:
            tt = o.read_typetree()
            if isinstance(tt, dict):
                nm = tt.get("m_Name")
        except Exception:
            pass
        if not nm:
            try:
                nm = getattr(o.read(), "m_Name", None)
            except Exception:
                pass
        out[int(pid)] = {"type": o.type.name, "name": nm}
    return out, None


def resolve_pptrs(mono_json_path, idx, bundle_src, bundle_dir, fields=None):
    """把一份 MonoBehaviour dump 里的 PPtr 字段解成 (类型, 名字)。

    解析顺序（**先静态表、再 UnityPy**；两条都不中就记 unresolved，不猜）：
      1. `m_FileID == 0`（本文件内引用）或跨文件 ⇒ 先拿 m_PathID 去 `anim_address_map.json`
         的容器索引查（容器项 = Addressables 资产，大概率命中）；
      2. 漏掉的用 UnityPy 读原 bundle 补。
    ⚠️ 跨文件引用（`m_FileID != 0`）用「全库 path_id 索引」解 —— 本项目没有 externals 表。
    ⚠️ **2026-10-07 更正（铁律 5；A152 顺手发现 ② / A161 ②）**：这里原来写的是
       「但**实测 path_id 唯一**（拿 4 个 clanParticles 验过，各命中唯一一条）」——
       🔴 **那是假结论**：4 个样本恰好没撞而已。**pid 是分包局部的**，全 84 包实测**跨包同 pid 不同名**：
       GameObject **1,291** · Mesh **82** · Material **18** · Texture2D **11** · Shader **6** 条
       （来源无一例外是 `scenes_scenes_*`，每个场景包的主 CAB 都从 pid=1 重新编号）。
       **本工具今天确实不踩**，但理由要写对（两条，都是实测）：
         ① 查的 `d:/4/Unity/数据/索引/anim_address_map.json` 覆盖 82 个包（**含**那 15 个 `scenes_*`），
            而它的 `guid_to_asset` **13,432 条记录 = 13,432 个不同 pid、同 pid 不同名 0 条**
            （逐条量过）⇒ 裸 pid 反查在这个集合上今天不撞；
         ② `unitypy_names()` 只在**一个**包里建 `objs = {o.path_id: o}`（`battleprefabs_vfxandmisc_assets_all`
            ——**单 CAB**）⇒ 也不撞。
       ⇒ ⛔ 别把「path_id 唯一」当结论抄到别的工具：要跨包反查就按 **`(包, pid)`**（这张表的每条记录
       本来就带 `bundle` / `bundleFile` ⇒ 有得可用）。判据：`资料/普查产出_1006/A152_pid陷阱普查.md` B7 ·
       `资料/已知的坑.md`。
    """
    d = read_json(mono_json_path)
    out = {}
    missing = []
    for k, v in d.items():
        if fields and k not in fields:
            continue
        if not (isinstance(v, dict) and "m_PathID" in v):
            continue
        pid = int(v.get("m_PathID") or 0)
        if pid == 0:
            out[k] = {"fileId": int(v.get("m_FileID") or 0), "pathId": 0,
                      "type": None, "name": None, "unresolved": "m_PathID == 0（空引用）"}
            continue
        r = idx.asset_of_pathid(pid)
        if r:
            out[k] = {"fileId": int(v.get("m_FileID") or 0), "pathId": pid,
                      "type": r.get("type"), "name": r.get("name"),
                      "bundle": r.get("bundle")}
        else:
            out[k] = {"fileId": int(v.get("m_FileID") or 0), "pathId": pid,
                      "type": None, "name": None}
            missing.append(pid)
    lazy, err = unitypy_names(bundle_src, bundle_dir, [p for p in missing if p]) \
        if bundle_src else ({}, "找不到原 bundle 源目录")
    for k, rec in out.items():
        if rec.get("name") is None and rec.get("pathId"):
            r = lazy.get(rec["pathId"])
            if r:
                rec["type"], rec["name"] = r["type"], r["name"]
                rec["resolvedBy"] = "unitypy"
            else:
                rec["unresolved"] = "path_id 在本地所有包里都解不出"
    return out, err


# ---------------------------------------------------------------- 台账（只做旁证）


def load_ledger():
    """`vfx_wiring_j1..5.tsv`：448 行「特效名 → category/trigger」。**旁证**，不当判据。"""
    out = {}
    if not os.path.isdir(LEDGER_GLOB_DIR):
        return out, None
    files = sorted(f for f in os.listdir(LEDGER_GLOB_DIR)
                   if f.startswith("vfx_wiring_j") and f.endswith(".tsv"))
    if not files:
        return out, None
    for fn in files:
        p = os.path.join(LEDGER_GLOB_DIR, fn)
        with io.open(p, encoding="utf-8") as fh:
            for i, ln in enumerate(fh):
                if i == 0:
                    continue
                f = ln.rstrip("\n").split("\t")
                if len(f) >= 4 and f[0]:
                    out.setdefault(f[0], {"category": f[1], "trigger": f[2][:160],
                                          "hook": f[3][:120], "file": fn})
    return out, files


# ---------------------------------------------------------------- 通用链（判据齐）


def build_generic(idx, bundle_src, problems):
    """共享 `CardPrefab`（`CardScript`）+ 各 SO → 原版**逐卡无关**的每事件挂钩。"""
    gen = {"byEvent": {}, "notes": []}

    cs, err = resolve_pptrs(CARD_SCRIPT_MONO, idx, bundle_src, BATTLEPREFS)
    if err:
        problems.append("CardScript 解析: %s" % err)

    def add_event(evt, slot, conf, evidence, extra=None):
        rec = cs.get(slot) or {}
        if not rec.get("name"):
            problems.append("CardPrefab.%s 解不出（%s）" % (slot, rec.get("unresolved")))
            return
        item = {"prefab": rec["name"], "type": rec.get("type"),
                "src": "CardPrefab.%s" % slot, "conf": conf, "evidence": evidence}
        # 槽里放的是 **CardAnim**（MonoBehaviour）还是直接放 prefab（GameObject）要分清：
        # CardAnim 的话，真正实例化的是它 `animInfo.animAdressable` 指的那个 GameObject。
        if rec.get("type") == "MonoBehaviour":
            pf = idx.prefab_of(rec["name"])
            item["cardanim"] = rec["name"]
            if pf:
                item["prefab"] = pf
                item["evidence"] += "；`CardAnim` 的 `animInfo.animAdressable` → prefab `%s`" % pf
        if extra:
            item.update(extra)
        gen["byEvent"].setdefault(evt, []).append(item)

    # 召唤/登场：卡体淡入（原版分普通/传说两档）＋ 落地那一下的尘土
    add_event(EV_DEPLOY, "normalSummon", "exact",
              "共享 CardPrefab 上 `CardScript.normalSummon`（CardAnim，普通档）——"
              "原版**每张卡**登场都走它；卡自己的 `summonAnimDelayed`（逐卡延迟特效）是**另一条**，"
              "见 `byCard[*].deploy` 的 `conf = authoritative` 条目"
              "（⚠️ 2026-10-14 更正：原来这里写「在远端包里，本地没有」—— **那条结论是错的**，"
              "卡 SO 的字段本身就是 GUID，本地解得出来。见文件头那段更正）")
    add_event(EV_DEPLOY, "legendarySummon", "exact",
              "`CardScript.legendarySummon`（传说档，与上面同槽位不同档）")
    add_event(EV_DEPLOY, "landOnGroundParticles", "exact",
              "`CardScript.landOnGroundParticles`（落地尘土；旁边两个字段 timeToLand=0.2 / "
              "timeBeforeLand=0.05 就是落地时序）")
    add_event(EV_DEPLOY, "landOnGroundParticlesLegendary", "exact",
              "`CardScript.landOnGroundParticlesLegendary`")

    # 挨打：小/大两档 —— 🔴 实测原版**两个字段指向同一个对象**（见简报「坑」）
    add_event(EV_HIT, "attackHitSmallParticles", "exact",
              "`CardScript.attackHitSmallParticles` / `attackHitBigParticles` —— "
              "🔴 原版这两个字段是**同一个 pathId**（4239831775333917772），不是我们抄错")
    add_event(EV_HIT, "attackHitBigParticles", "exact", "同上（原版两档同对象）")

    # 阵亡
    add_event(EV_DEATH, "cardDestroyFX", "exact",
              "`CardScript.cardDestroyFX`（`UnitDeath` 里 `Instantiate(cardDestroyFX, 卡位置, 3D体旋转)`）")

    # 治疗（全局槽，不在我们的事件名清单里，如实带上）
    # ⚠️ `BattleAnims` 里的槽有的是 **PPtr**（`m_PathID`），`heal` 是 **AssetGUID 引用**
    #    （`m_AssetGUID`）—— 两种都得认，不然会静默漏掉有值的那一个。
    ba_raw = (read_json(BATTLE_ANIMS_MONO).get("heal") or {})
    h = {}
    if ba_raw.get("m_AssetGUID"):
        r = idx.asset_of_guid(ba_raw["m_AssetGUID"])
        if r:
            h = {"name": r.get("name"), "type": r.get("type"),
                 "guid": ba_raw["m_AssetGUID"]}
        else:
            h = {"unresolved": "guid %s 在本地容器表里查不到" % ba_raw["m_AssetGUID"]}
    else:
        h = {"unresolved": "heal 槽既没有 guid 也没有 pathId（空引用）"}
    if h.get("name"):
        gen["byEvent"]["heal"] = [{
            "prefab": h["name"], "type": h.get("type"),
            "src": "BattleAnims.heal", "conf": "exact",
            "evidence": "全局动画槽 `BattleAnims.heal`（guid 4ac621b3…）；"
                        "同表其余 12 个槽在本地全是空引用（`m_PathID == 0`）"}]
    else:
        problems.append("BattleAnims.heal 解不出: %s" % (h.get("unresolved") or h))

    # 远程攻击：按阵营（CardArmy id）
    rp, err3 = resolve_pptrs(RANGED_SO_MONO, idx, bundle_src, BATTLEPREFS)
    if err3:
        problems.append("RangedAttackParticlesByArmy 解析: %s" % err3)
    by_army = {}
    for arm in (read_json(RANGED_SO_MONO).get("rangedParticlesByClan") or []):
        army = arm.get("army")
        cp = arm.get("clanParticles") or {}
        pid = int(cp.get("m_PathID") or 0)
        if not pid:
            by_army[str(army)] = {"prefab": None, "unresolved": "clanParticles 空引用"}
            continue
        r = idx.asset_of_pathid(pid)
        if r:
            cardanim = r.get("name")
            by_army[str(army)] = {
                "cardanim": cardanim,
                "prefab": idx.prefab_of(cardanim),
                "src": "RangedAttackParticlesByArmy.rangedParticlesByClan",
                "conf": "exact",
            }
        else:
            by_army[str(army)] = {"prefab": None, "pathId": pid,
                                  "unresolved": "path_id 解不出"}
    gen["attackRangedByArmy"] = by_army

    # 登场那一族的「阵营召唤圈」：原版是按阵营分的 CardAnim（`AeldariSummon` / `Tau_Summon` /
    # `GSC Summon` / `BeastSnagga_Summon`…），它们 `animInfo.animAdressable` 才指向
    # `BlueSummonCircle` / `Tau_SummonCircle` 这些 prefab。
    # ⚠️ 「哪个 CardAnim 属于哪个阵营」**没有本地字段**（靠名字前缀）= 推断 ⇒ 这里只列清单、不硬绑阵营。
    summon = []
    for a in sorted(idx.cardanim):
        pf = idx.prefab_of(a)
        if not pf:
            continue
        if "summoncircle" in norm(pf) or ("summon" in a.lower() and "circle" in pf.lower()):
            summon.append({"cardanim": a, "prefab": pf,
                           "evidence": "`%s` 的 `animInfo.animAdressable` → `%s`" % (a, pf)})
    gen["deploySummonCandidates"] = summon
    gen["notes"].append(
        "`BlueSummonCircle` / `Tau_SummonCircle` **在本地只有 prefab、没有 CardAnim**；"
        "引用它们的是**按阵营**的 CardAnim（`AeldariSummon` / `Tau_Summon` / `Tau_Kroot_Summon`）"
        "⇒ 见 `deploySummonCandidates`。0824 台账把部署标成 `BlueSummonCircle` 是 Godot 侧的口径，"
        "不是原版字段。")
    gen["notes"].append(
        "`RangedAttackParticlesByArmy` 原版只有 **4 个阵营**（10 UM / 20 Goff / 40 Sautekh / 50 BlackLegion）"
        "⇒ 其余阵营走 `RawCardScript.customRangedAttackParticles`（逐卡；⚠️ 2026-10-14 更正："
        "原来写「远端包、本地没有」—— **错**，见 `byCard[*].attack_ranged` 的 `conf = authoritative`）"
        "或没有阵营专属粒子。")
    gen["notes"].append(
        "**近战**那一支**没有**对应的 SO：原版近战特效挂在卡的 3D Animator（`card3DAnimationController`）"
        "+ `AlternateArtCard.attackAnim`。⚠️ **2026-10-14 更正**：原来写「`attackAnim`（逐卡，远端包）⇒ 本地判不了」"
        "—— **前半句错**（不是「在远端包」的问题）；**后半句仍成立、但理由要换**："
        "`attackAnim` 在真值快照 1,195 张卡里**只有 2 张有、且这 2 条都是空引用**，"
        "`alternateArts[]` 也只覆盖 2 张卡且是 **PPtr（不是 AssetReference，本地无 externals 表）**"
        " ⇒ **这一格是「判据为空」，不是「在远端」**（铁律 11 例外①）。")
    mark_library(gen)
    return gen


# ---------------------------------------------------------------- 逐卡三层


def build_by_card(cards, idx, gen, ledger, factions_army, stats):
    by_card = {}
    # ---- ① 卡名 → 同名 CardAnim（唯一一条本地可用的逐卡线索）
    name_of_norm = {}
    for c in cards:
        n = norm(c.get("name"))
        if n and len(n) >= 4:            # 太短的名字（如 "Seize"）容易撞上无关动画
            name_of_norm.setdefault(n, []).append(c)
    anim_by_norm = collections.defaultdict(list)
    for a in idx.cardanim:
        anim_by_norm[norm(a)].append(a)

    def classify(anim):
        n = norm(anim)
        for suf, (ev, tt) in sorted(TRAIT_SUFFIX_EVENT.items(),
                                    key=lambda kv: -len(kv[0])):
            if n.endswith(suf):
                return ev, "后缀 `%s` ⇒ %s（原版天赋命名构词法）" % (
                    TRAIT_SUFFIX_DISPLAY.get(suf, suf), tt)
        stem = n
        for s in sorted(TRAIT_LOOSE_SUFFIX, key=len, reverse=True):
            if n.endswith(s):
                stem = n[:-len(s)]
                return None, "后缀 `%s` ⇒ 天赋效果，**哪一类判不了**（原版归口在 `CardTrait.TraitEffects`）" % s
        low = anim.lower()
        if "summon" in low or "spawn" in low or "deploy" in low:
            return EV_DEPLOY, "名字含 summon/spawn/deploy"
        # 近战 vs 远程按词根分（`Strike`/`Slash`/`Claw` 这一族是挥砍，`Bullet`/`Las`/`Gauss` 那一族是弹道）
        if any(k in low for k in ("strike", "slash", "slice", "sweep", "claw",
                                  "melee", "cleave")):
            return EV_ATK_MELEE, "名字含 strike/slash/slice/sweep/claw/melee/cleave"
        if any(k in low for k in ("bullet", "las", "gauss", "impact", "shot",
                                  "missile", "beam", "shot_trail")):
            return EV_ATK_RANGED, "名字含 bullet/las/gauss/impact/shot/missile/beam"
        if "damage" in low or "_hit" in low or " hit" in low or low.endswith("hit"):
            return EV_HIT, "名字含 damage/hit"
        if any(k in low for k in ("board", "battleground")):
            return EV_PLAY, "名字含 Board/Battleground（全场/落板那一下）"
        return None, "名字里没有可判读的事件词"

    n_name, n_kw, n_uncl = 0, 0, 0
    for n, cards_here in name_of_norm.items():
        anims = set()
        for a in idx.cardanim:
            na = norm(a)
            if na.startswith(n) and len(na) > len(n):
                anims.add(a)
        # 原版把 `Traitors' Hate` 写成 `Traitors' Hate Board`：归一化后是前缀；再补一条「掐掉
        # 名字里的撇号/空格后仍相等」的兜底，避免只差一个符号就漏
        for c in cards_here:
            for a in sorted(anims):        # ⚠️ 必须 sorted：anims 是 set，不排序会写出**不幂等**的表
                ev, why = classify(a)
                pf = idx.prefab_of(a)
                if ev is None:
                    if pf is None:
                        continue
                    by_card.setdefault(c["id"], {}).setdefault(
                        "_unclassified", []).append(
                        {"cardanim": a, "prefab": pf, "conf": "inferred-name",
                         "src": "name-match", "why": why})
                    n_uncl += 1
                    continue
                if pf is None:
                    continue
                rec = {"prefab": pf, "cardanim": a, "conf": "inferred-name",
                       "src": "name-match",
                       "why": "卡名 `%s` 归一化后是 CardAnim 名 `%s` 的前缀；%s"
                              % (c["name"], a, why)}
                if a in ledger:      # 台账有行就把它的 category/trigger 一起带上（旁证，不当判据）
                    rec["ledgerCategory"] = ledger[a]["category"]
                    rec["ledgerTrigger"] = ledger[a]["trigger"]
                tgt = by_card.setdefault(c["id"], {}).setdefault(ev, [])
                if rec not in tgt:
                    tgt.append(rec)
                    n_name += 1

    # ---- ② 关键词 → `<关键词>TraitStart/TraitTrigger/...`
    # ⚠️ 事件名只有 `trigger` / `ability` 两个（`VfxMap.Events` 那一份），而原版的
    #    `EffectTriggerType` 有 7 档 ⇒ 归并是**降信息**的，所以每条都把原始档位写在
    #    `traitTriggerType` 里，别让下游以为 Start(0) 和 Trigger(3) 是一回事。
    trait_anims = collections.defaultdict(dict)     # keyword_norm → {anim: (ev, suf, tt)}
    for a in idx.cardanim:
        n = norm(a)
        for suf, (ev, tt) in sorted(TRAIT_SUFFIX_EVENT.items(), key=lambda kv: -len(kv[0])):
            if n.endswith(suf) and len(n) > len(suf):
                trait_anims[n[:-len(suf)]].setdefault(ev, []).append((a, suf, tt))
                break
    kw_table = {}
    for kwn, per_ev in sorted(trait_anims.items()):
        kw_table[kwn] = {}
        for ev, anims in sorted(per_ev.items()):
            lst = []
            for a, suf, tt in sorted(anims):
                pf = idx.prefab_of(a)
                if pf:
                    lst.append({"cardanim": a, "prefab": pf,
                                "traitTriggerType": tt, "suffix": suf})
            if lst:
                kw_table[kwn][ev] = lst
    for c in cards:
        for k in (c.get("keywords") or []):
            kwn = norm(re.split(r"[ :(\[]", k.strip())[0])
            per_ev = kw_table.get(kwn)
            if not per_ev:
                continue
            for ev, lst in per_ev.items():
                for it in lst:
                    rec = {"prefab": it["prefab"], "cardanim": it["cardanim"],
                           "conf": "inferred-keyword", "src": "keyword-trait",
                           "traitTriggerType": it["traitTriggerType"],
                           "why": "卡的关键词 `%s` → 原版天赋动画名的构词法 `<关键词>%s`"
                                  "（= `%s`）" % (k, TRAIT_SUFFIX_DISPLAY.get(
                                      it["suffix"], it["suffix"]),
                                                  it["traitTriggerType"])}
                    tgt = by_card.setdefault(c["id"], {}).setdefault(ev, [])
                    if rec not in tgt:
                        tgt.append(rec)
                        n_kw += 1

    # ---- ③ 进攻卡（环境效果卡）：SO 的卡是 PPtr，**解不出卡名** ⇒ 用候选名对，命中才算
    n_env = 0
    env_unmatched = []
    try:
        off = read_json(OFFENSIVE)
    except Exception as e:
        PROBLEMS.append("offensive_cards.json 读不动: %s" % e)
        off = None
    if off:
        pool = {}
        for c in cards:
            pool.setdefault(norm(c.get("name")), []).append(c)
        for army, a in (off.get("armies") or {}).items():
            for slot in (a.get("cards") or []):
                cand = slot.get("cardName_candidate")
                pf = slot.get("prefab_name")
                if not cand or not pf:
                    env_unmatched.append({"army": army, "candidate": cand,
                                          "reason": "SO 的卡名解不出或 prefab 解不出"})
                    continue
                hit = pool.get(norm(cand))
                if not hit:
                    env_unmatched.append({"army": army, "candidate": cand,
                                          "reason": "候选名在我们的卡池里找不到同名的卡"})
                    continue
                for c in hit:
                    rec = {"prefab": pf,
                           "cardanim": slot.get("envSO_name"),
                           "conf": "inferred-envcard",
                           "src": "EnvironmentalEffectCardsSO.enviromentalEffectVFX",
                           "why": "原版 `EnvironmentalEffectCardsSO` 的卡是 PPtr（`m_FileID=4`，本地无 "
                                  "externals 表）⇒ 卡身份靠**卡面贴图名反推的候选名**（`cardName_candidate`）"}
                    tgt = by_card.setdefault(c["id"], {}).setdefault(EV_PLAY, [])
                    if rec not in tgt:
                        tgt.append(rec)
                        n_env += 1

    stats.update({"entries_inferred_name": n_name,
                  "entries_inferred_keyword": n_kw,
                  "entries_inferred_envcard": n_env,
                  "entries_unclassified": n_uncl,
                  "envcard_unmatched": len(env_unmatched)})
    return by_card, kw_table, env_unmatched


# ---------------------------------------------------------------- 🔴 权威档（原版真值，2026-10-14 新增）
#
# 为什么这一档存在：见文件头那段「2026-10-14 更正」—— 逐卡那一跳**从来没断**。
# 链路（判据齐，可逐字复核）：
#   原版卡 SO 字段（`AssetReferenceTyped<CardAnim>` / `AssetReference`，存 GUID）
#     → `guid_map.tsv`：GUID → 束名 / 容器键 / **资产原名** + `m_Script` 类名
#     → 类名 == `CardAnim` 时再跳一步：`anim_address_map.json` 的 `cardanim_to_asset`
#        （CardAnim 的 `animInfo.animAdressable` → 真正的 VFX prefab）
#     → 类名 == `GameObject` 时**它本身就是 prefab**
# 样本（可逐字复核）：`EC29 Heldrake`
#   `customRangedAttackParticles = 564af14f7004445469e0424bcce41d1d`
#   → `EC Heldrake Attack`（MonoBehaviour / `CardAnim`）→ `cardanim_to_asset` → prefab `EC Heldrake Attack`


def load_enum_stub(name):
    """从签名桩（`ENUM_STUBS/<name>.cs`）读出 `枚举数 -> 名字`。读不到返回 {}（**不猜**）。"""
    p = os.path.join(ENUM_STUBS, name + ".cs")
    if not os.path.isfile(p):
        return {}
    body = re.search(r"\benum\s+%s\s*\{(.*?)\n\}" % re.escape(name),
                     io.open(p, encoding="utf-8", errors="replace").read(), re.S)
    if not body:
        return {}
    out = {}
    for m in re.finditer(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,", body.group(1), re.M):
        out[int(m.group(2))] = m.group(1)
    return out


def load_guid_map(path):
    """`guid_map.tsv` → ({guid: [record]}, 错误串)。

    record 的字段 = `uniqueId/cardName/field/assetGuid/bundle/containerKey/type/className/name`。
    `className == 'CardAnim'` 是关键（还要再跳一步）；`type == 'GameObject'` 就是 prefab 本体。
    ⚠️ 同一 GUID 可以有多条（Sprite + Texture2D 那一族），所以值是 list。
    """
    import csv
    if not os.path.isfile(path):
        return {}, ("guid_map.tsv 不存在: %s（重建：`python -I -X utf8 工具/_guid_resolve_probe.py map`）"
                    % rel(path))
    out = collections.defaultdict(list)
    with io.open(path, encoding="utf-8") as fh:
        for r in csv.DictReader(fh, delimiter="\t"):
            if r.get("assetGuid") and r.get("name"):
                out[r["assetGuid"]].append(r)
    return out, None


def resolve_so_ref(guid, guid_map, idx, want_prefab):
    """卡 SO 的一个 AssetGUID → (解析出的资产 dict, 解不出的原因)。

    三条出口：
      · `className == 'CardAnim'` → 再查 `cardanim_to_asset` 取 prefab 名（**VFX 那一族必须走到这里**）；
      · `type == 'GameObject'`   → 名字就是 prefab；
      · 其余（Sprite / Texture2D / AudioClip…）→ **资产原名本身就是结果**
        （`want_prefab=False` 的 `_assetRefs` 那一族用得上；`want_prefab=True` 时记成解不出）。
    **解不出的原因必须由调用方写进 entry 的 `unresolved` 并计数，不许静默丢。**
    """
    rs = guid_map.get((guid or "").strip().lower()) or []
    if not rs:
        return None, ("GUID 在 `guid_map.tsv` 里没有解出具名资产"
                      "（目录里有这个 key、但本地束里没有对应容器键 —— 两个 build 的差）")
    ca = [r for r in rs if r["className"] == "CardAnim"]
    go = [r for r in rs if r["type"] == "GameObject"]
    pick = (ca or go or rs)[0]
    rec = {"assetName": pick["name"], "assetType": pick["type"],
           "assetBundle": pick.get("bundle"), "containerKey": pick.get("containerKey")}
    if pick["className"] == "CardAnim":
        rec["cardanim"] = pick["name"]
        tgt = (idx.cardanim.get(pick["name"]) or {}).get("targetName")
        if tgt:
            rec["prefab"] = tgt
        elif want_prefab:
            return rec, ("CardAnim `%s` 没有可解的 `animInfo.animAdressable`"
                         "（原版是纯计时 / 无 VFX 的占位动画，不是解错）" % pick["name"])
    elif pick["type"] == "GameObject":
        rec["prefab"] = pick["name"]
    elif want_prefab:
        return rec, ("解出的资产类型 `%s` 不是 CardAnim 也不是 GameObject（名字 `%s`）"
                     % (pick["type"], pick["name"]))
    return rec, None


def so_refs(card, trig_names, eff_names):
    """一张**原版**卡 SO → [(soField, event, guid, extra)]；`event is None` = 非 VFX 字段。

    ⚠️ `cardAnimations` 那两路的**事件名归到 `ability` 是我们拍的**（`VfxMap.Ability` = 「部队卡在场上发动技能」）：
    原版真正给的是 `CardAbility.trigger` / `AbilityLogic.chosenAbility` 两个枚举，
    **两个原样带在 entry 上**（`abilityTrigger(+Name)` / `chosenAbility(+Name)`），下游要精确到事件自己判。
    """
    out = []
    for f, ev, _why in SO_VFX_FIELDS:
        g = (card.get(f) or {}).get("m_AssetGUID")
        if g:
            out.append((f, ev, g, {}))
    for f, _kind, _why in SO_ASSET_FIELDS:
        g = (card.get(f) or {}).get("m_AssetGUID")
        if g:
            out.append((f, None, g, {}))
    for i, ab in enumerate(card.get("cardAbilities") or []):
        for j, lg in enumerate(ab.get("abilityLogic") or []):
            for k, a in enumerate(lg.get("cardAnimations") or []):
                g = (a or {}).get("m_AssetGUID")
                if not g:
                    continue
                out.append((
                    "cardAbilities[%d].abilityLogic[%d].cardAnimations[%d]" % (i, j, k),
                    EV_ABILITY, g,
                    {"abilitySlot": "cardAbilities[%d].abilityLogic[%d]" % (i, j),
                     "abilityTrigger": ab.get("trigger"),
                     "abilityTriggerName": trig_names.get(ab.get("trigger")),
                     "chosenAbility": lg.get("chosenAbility"),
                     "chosenAbilityName": eff_names.get(lg.get("chosenAbility"))}))
    aab = card.get("activeAbility") or {}
    for j, lg in enumerate(aab.get("activeAbilityLogic") or []):
        for k, a in enumerate(lg.get("cardAnimations") or []):
            g = (a or {}).get("m_AssetGUID")
            if not g:
                continue
            out.append((
                "activeAbility.activeAbilityLogic[%d].cardAnimations[%d]" % (j, k),
                EV_ABILITY, g,
                {"abilitySlot": "activeAbility.activeAbilityLogic[%d]" % j,
                 "abilityTrigger": None,
                 "abilityTriggerName": "主动技能（`activeAbility`，不走 `CardAbility.trigger` 那套枚举）",
                 "chosenAbility": lg.get("chosenAbility"),
                 "chosenAbilityName": eff_names.get(lg.get("chosenAbility"))}))
    return out


def build_authoritative(cards, idx, stats, problems, truth_path, guid_map_path, faction_army):
    """🔴 **权威档**：把「原版卡 SO 的逐卡绑定」解成我们卡池每个 id 的事件表。

    三件事必须如实记（铁律：不许静默失败）：
      ① **解不出的** → entry 上留 `unresolved`（`prefab: null`），并计数；
      ② **我们池子里、真值快照里没有的卡** → 计 `poolCardsWithoutTruth`，**不猜**；
      ③ **匹配方式**分两段（`joinKey`）：**卡 id 完全相同**（最硬）⇒ 否则**卡名归一化后相同 + 阵营相同**。

    ⚠️ **卡名匹配为什么必须带阵营闸**：实测同名跨阵营真的存在 —— 我们的 `EC_Terminator_Champion` /
    `EC_Maulerfiend`（帝皇之子）在真值里**只有** `BL29` / `BL31`（黑色军团）同名卡；
    不加阵营闸就会**把别家的动画安到自家卡上**（静默错，正是项目记过的坑）。
    ⚠️ **同名多卡**（真值里有 259 张卡的 `uniqueId` 是 32 位十六进制串，是同内容的第二份 dump）
     ⇒ 优先取「`uniqueId` 是正常卡 id」的那一份；**仍有歧义**时全取并在条目上打 `nameJoinAmbiguous`。
    """
    if not os.path.isfile(truth_path):
        problems.append("权威档：真值快照不存在 %s ⇒ 整档跳过" % rel(truth_path))
        return {}, {"skipped": "truth snapshot missing"}
    truth = read_json(truth_path)
    guid_map, err = load_guid_map(guid_map_path)
    if err:
        problems.append("权威档：" + err)
    trig_names = load_enum_stub("AbilityTrigger")
    eff_names = load_enum_stub("AbilityEffect")
    if not trig_names or not eff_names:
        problems.append("权威档：枚举名字表读不到（%s）⇒ `abilityTriggerName`/`chosenAbilityName` 会是 null"
                        "，**数字仍然是对的**" % rel(ENUM_STUBS))
    guid_like = re.compile(r"^[0-9a-f]{32}$")

    by_id, by_norm_name = {}, collections.defaultdict(list)
    for t in truth:
        if t.get("uniqueId"):
            by_id[t["uniqueId"]] = t
        by_norm_name[norm(t.get("cardName"))].append(t)

    entries = 0
    vfx_entries = 0
    asset_entries = 0
    cards_vfx, cards_any = set(), set()
    per_field = collections.Counter()
    per_event = collections.Counter()
    unresolved = collections.Counter()
    unresolved_rows = []
    no_truth = []
    join_by = collections.Counter()
    name_join_dropped = []
    name_join_ambiguous = []
    out = {}

    for c in cards:
        cid = c["id"]
        hits, joins, ambiguous = [], [], False
        if cid in by_id:
            hits.append(by_id[cid])
            joins.append("id")
        else:
            arm = faction_army.get(c.get("faction"))
            cand = [t for t in by_norm_name.get(norm(c.get("name")), [])]
            if arm is not None:
                same_faction = [t for t in cand if t.get("cardArmy") == arm]
                if cand and not same_faction:
                    name_join_dropped.append(
                        {"cardId": cid, "name": c.get("name"), "faction": c.get("faction"),
                         "truthCandidateIds": [t.get("uniqueId") for t in cand],
                         "truthCandidateArmies": [t.get("cardArmy") for t in cand]})
                cand = same_faction
            if len(cand) > 1:                      # 同名多卡：优先取「uniqueId 是正常卡 id」的那一份
                proper = [t for t in cand if not guid_like.match(t.get("uniqueId") or "")]
                if len(proper) == 1:
                    cand = proper
                else:
                    ambiguous = True
                    name_join_ambiguous.append(
                        {"cardId": cid, "name": c.get("name"),
                         "truthCandidateIds": [t.get("uniqueId") for t in cand]})
            if cand:
                hits.extend(cand)
                joins.append("name")
        if not hits:
            no_truth.append(cid)
            continue
        join_key = "+".join(dict.fromkeys(joins))
        join_by[join_key] += 1
        dest = out.setdefault(cid, {})
        for t in hits:
            for so_field, ev, guid, extra in so_refs(t, trig_names, eff_names):
                got, why = resolve_so_ref(guid, guid_map, idx, ev is not None)
                rec = {"conf": "authoritative",
                       "src": "cardSO." + so_field,
                       "soField": so_field,
                       "guid": guid,
                       "origCardId": t.get("uniqueId"),
                       "joinKey": join_key}
                if ambiguous:
                    rec["nameJoinAmbiguous"] = True
                if got:
                    rec.update({k: v for k, v in got.items() if k != "guid"})
                if extra:
                    rec.update(extra)
                if ev is None:
                    # 非 VFX 的逐卡资产（立绘 / 语音）：**资产原名本身就是结果**，不必是 prefab
                    rec["assetKind"] = SO_ASSET_KIND[so_field]
                    if why:
                        rec["unresolved"] = why
                        unresolved[why.split("（")[0]] += 1
                        unresolved_rows.append({"cardId": cid, "soField": so_field,
                                                "guid": guid, "why": why,
                                                "kind": "assetRef"})
                    dest.setdefault("_assetRefs", []).append(rec)
                    cards_any.add(cid)
                    per_field[so_field + " [assets]"] += 1
                    asset_entries += 1
                    entries += 1
                    continue
                rec.setdefault("prefab", None)
                if why:
                    rec["unresolved"] = why
                    unresolved[why.split("（")[0]] += 1
                    unresolved_rows.append({"cardId": cid, "soField": so_field,
                                            "guid": guid, "why": why,
                                            "cardanim": rec.get("cardanim")})
                rec["inLibrary"] = in_vfx_library(rec["prefab"])
                dest.setdefault(ev, []).append(rec)
                cards_vfx.add(cid)
                cards_any.add(cid)
                entries += 1
                vfx_entries += 1
                per_field[so_field] += 1
                per_event[ev] += 1

    out = {cid: m for cid, m in out.items() if any(m.values())}

    st = {
        "truthSnapshot": rel(truth_path),
        "truthCards": len(truth),
        "guidMap": rel(guid_map_path),
        "guidMapGuids": len(guid_map),
        "authoritativePerCardEntries": entries,
        "authoritativeVfxEntries": vfx_entries,
        "authoritativeAssetRefEntries": asset_entries,
        "authoritativeCards": len(cards_any),
        "authoritativeCardsVfxOnly": len(cards_vfx),
        "authoritativeBySoField": dict(per_field),
        "authoritativeByEvent": dict(per_event.most_common()),
        "authoritativeUnresolved": dict(unresolved),
        "authoritativeUnresolvedRows": unresolved_rows[:40],
        "authoritativeUnresolvedCount": len(unresolved_rows),
        "poolCardsWithoutTruth": len(no_truth),
        "poolCardsWithoutTruthIds": no_truth[:60],
        "cardsJoinedBy": dict(join_by),
        "nameJoinDroppedByFaction": name_join_dropped,
        "nameJoinAmbiguous": name_join_ambiguous,
    }
    # 🔴 `authoritativePerCardEntries` 必须挂到**主 stats** 上（旧值恒 0，是那份错结论留下的）
    stats["authoritativePerCardEntries"] = entries
    stats["authoritativeVfxEntries"] = vfx_entries
    stats["authoritativeCards"] = len(cards_any)
    stats["authoritativeCardsVfxOnly"] = len(cards_vfx)
    stats["authoritative"] = st
    return out, st


def merge_authoritative(by_card, auth):
    """权威档并进 `byCard`，**同事件下排在推断条目前面**（旧三档一条都不删）。"""
    for cid, m in auth.items():
        dest = by_card.setdefault(cid, {})
        for ev, lst in m.items():
            if ev == "_assetRefs":
                dest.setdefault(ev, []).extend(lst)
                continue
            dest[ev] = lst + dest.get(ev, [])
    return by_card


PROBLEMS = []


# ---------------------------------------------------------------- 主流程


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--quiet", action="store_true")
    ap.add_argument("--truth", default=CARD_SO_TRUTH,
                    help="原版卡 SO 全字段快照（默认 %s）" % CARD_SO_TRUTH)
    ap.add_argument("--guid-map", default=GUID_MAP_TSV,
                    help="卡 SO 的 AssetGUID → 资产名对照表（默认 %s）" % GUID_MAP_TSV)
    ap.add_argument("--no-truth", action="store_true",
                    help="跳过权威档（只重出旧那三档 + 通用链；排障用）")
    args = ap.parse_args()
    t0 = time.time()

    bundle_src = find_bundle_src()
    if not bundle_src:
        print("!! 找不到原 bundle 源目录（UnityPy 反解会降级）")

    cards = read_json(CARDS_ENGINE)["cards"]
    idx = AddrIndex(ADDR_MAP)
    ledger, ledger_files = load_ledger()
    factions_army = {}
    try:
        for f in read_json(CARD_INDEX).get("factions") or []:
            factions_army[f["name"]] = f["id"]
    except Exception as e:
        PROBLEMS.append("card_index.json 读不动: %s" % e)

    print("=" * 78)
    print("gen_card_vfx_by_card.py")
    print("  卡池        = %d 张（%s）" % (len(cards), rel(CARDS_ENGINE)))
    print("  CardAnim 表 = %d 个名字（%s）" % (len(idx.cardanim), rel(ADDR_MAP)))
    print("  台账        = %d 行（%s）" % (len(ledger), ", ".join(ledger_files or [])))
    print("  真值快照    = %s" % (rel(args.truth) if not args.no_truth else "（--no-truth：跳过）"))
    print("  GUID 对照表 = %s" % (rel(args.guid_map) if not args.no_truth else "（--no-truth：跳过）"))

    gen = build_generic(idx, bundle_src, PROBLEMS)
    print("[1/4] 通用链：%d 个事件槽；远程按阵营 %d 条"
          % (sum(len(v) for v in gen["byEvent"].values()),
             len(gen["attackRangedByArmy"])))

    stats = {}
    by_card, kw_table, env_unmatched = build_by_card(
        cards, idx, gen, ledger, factions_army, stats)
    print("[2/4] 逐卡·推断三档：覆盖 %d / %d 张（name %d · keyword %d · envcard %d）"
          % (len(by_card), len(cards), stats["entries_inferred_name"],
             stats["entries_inferred_keyword"], stats["entries_inferred_envcard"]))

    # ---- 🔴 权威档（原版真值）：优先级最高，先并进来，旧三档降级为兜底
    auth_st = {"skipped": "--no-truth"}
    if not args.no_truth:
        auth, auth_st = build_authoritative(
            cards, idx, stats, PROBLEMS, args.truth, args.guid_map, factions_army)
        by_card = merge_authoritative(by_card, auth)
        u = auth_st.get("authoritativeUnresolved") or {}
        print("[3/4] 逐卡·**权威档**：%d 条（其中 VFX %d 条 · 非 VFX 的 `_assetRefs` %d 条）/ %d 张卡"
              "（其中只有 VFX 的 %d 张）"
              % (auth_st.get("authoritativePerCardEntries", 0),
                 auth_st.get("authoritativeVfxEntries", 0),
                 auth_st.get("authoritativeAssetRefEntries", 0),
                 auth_st.get("authoritativeCards", 0),
                 auth_st.get("authoritativeCardsVfxOnly", 0)))
        print("       真值快照 %d 张卡；我们池子里**快照没有的** %d 张（那些卡落旧档）；"
              "匹配方式：%s"
              % (auth_st.get("truthCards", 0), auth_st.get("poolCardsWithoutTruth", 0),
                 auth_st.get("cardsJoinedBy")))
        if auth_st.get("nameJoinDroppedByFaction"):
            print("       ⚠️ 卡名匹配被【阵营闸】挡掉 %d 张（同名跨阵营，不许安到自家卡上）：%s"
                  % (len(auth_st["nameJoinDroppedByFaction"]),
                     [(d["cardId"], d["truthCandidateIds"]) for d in auth_st["nameJoinDroppedByFaction"]]))
        if auth_st.get("nameJoinAmbiguous"):
            print("       ⚠️ 卡名匹配仍有歧义 %d 张（已全取并打 `nameJoinAmbiguous`）：%s"
                  % (len(auth_st["nameJoinAmbiguous"]),
                     [d["cardId"] for d in auth_st["nameJoinAmbiguous"]]))
        print("       分字段：%s" % auth_st.get("authoritativeBySoField"))
        if u:
            print("       ⚠️ 解不出的 %d 条：%s" % (auth_st.get("authoritativeUnresolvedCount", 0), u))
    else:
        print("[3/4] 权威档：**已跳过**（--no-truth）⇒ 这份产物不带 `authoritative` 条目")

    # 每个事件各有多少张卡
    per_ev = collections.Counter()
    for cid, m in by_card.items():
        for ev in m:
            per_ev[ev] += 1
    print("       事件分布：%s" % dict(per_ev.most_common()))

    by_conf = collections.Counter()
    cards_by_conf = collections.defaultdict(set)
    for cid, m in by_card.items():
        for ev, lst in m.items():
            for it in (lst if isinstance(lst, list) else [lst]):
                by_conf[it.get("conf")] += 1
                cards_by_conf[it.get("conf")].add(cid)
    print("       conf 分布（条 / 张）：%s" % {
        k: "%d / %d" % (v, len(cards_by_conf[k])) for k, v in by_conf.items()})
    print("       ↳ 优先级：%s（消费者按此顺序取第一个命中的）" % " > ".join(CONF_PRIORITY))

    no_entry = [c["id"] for c in cards if c["id"] not in by_card]
    print("       一个事件都没接上的卡：%d" % len(no_entry))

    # ---- 第 2 跳的更正文（每次重跑都重算一遍，免得结论过期）
    hop2 = {
        "verdict": "✅ **第 2 跳（卡 → 它自己的 animInfo 列表）在本地【是通的】**"
                   " —— 2026-10-14 更正，推翻本文件原来那句「在本地断掉」",
        "was": "原来写「卡预制体只在远端 CCD 的 `allcards_assets_all.bundle` 里 ⇒ 逐卡覆盖拿不到」",
        "now": "卡 SO 的那些字段**本身就是 `AssetReference`（存 GUID）**，而 GUID → 束 → 容器键 → 资产原名"
               "一直可解（`guid_map.tsv` / `anim_address_map.json`），**不需要那只远端包**",
        "wrongBecause": "**把「载体的文件不在本地」当成了「那一跳断掉」**：当时只找了「卡预制体」这一个载体，"
                        "没意识到同一批绑定已经以 GUID 形式摆在卡 SO 字段里",
        "evidence": [
            "`catalog_main.json` 的 `m_InternalIds` 里 **12,319/12,653 条的 internalId 就是主键 GUID 本身**"
            " ⇒ 「GUID → 束」连 CCD 都不用碰",
            "卡 SO 逐字段覆盖率（真值 1,195 张）：`cardImageReference` 1171 · `customRangedAttackParticles` 484 ·"
            " `customDeathParticles` 72 · `remnantBodyPrefab` 73 · `summonAnimDelayed` 1 ·"
            " `cardAbilities[].abilityLogic[].cardAnimations` 1359 条 / 796 张",
            "本脚本本次实测（我们卡池 1,126 张，见 `stats.authoritative`）",
        ],
        "limits": [
            "真值快照是 **2022 时代**（1,195 张）。我们卡池 1,126 张里 **168 张比它新**"
            "（SpaceWolves / Ultramarines / DarkAngels 等后来加的内容）⇒ 那些卡在权威档下**查不到**，落旧档。",
            "`cardAnimations` 那一路的**事件名归到 `ability` 是我们拍的**（原版给的是枚举），"
            "已逐条带 `abilityTrigger` / `chosenAbility`。",
            "**天赋触发**（`CardTrait.TraitEffects[].effectAnim`）**仍然没做** —— 那份 SO 不在卡 SO 里。",
        ],
    }

    chain = [
        {"hop": 1, "from": "我们的卡 id", "to": "原版那张卡",
         "how": "**同一套 id**（我们的 id 就是原版 id）。证据：`bundle_prebuiltdecks_assets_all/MonoBehaviour/ASH_SK_2.json` "
                "与 `bundle_draftpacks_assets_all/MonoBehaviour/Shoulders of Giants.json` 里的 `cardIds` 就是 `ASH53/ASH32` 这种串",
         "conf": "exact"},
        {"hop": 2, "from": "原版卡", "to": "它自己的 animInfo 列表（每事件一条）",
         "how": "✅ **通** —— 卡 SO 字段本身是 `AssetReference`（存 GUID）⇒ `guid_map.tsv`（GUID → 束 / 容器键 / 资产原名）"
                "⇒ `cardanim_to_asset`（CardAnim → VFX prefab）。**2026-10-14 更正**：原来这里写「断」，"
                "错因见 `hop2_corrected.wrongBecause`。",
         "conf": "authoritative"},
        {"hop": 3, "from": "animInfo", "to": "animAdressable GUID",
         "how": "`animinfo_0824.json`(591) / `card_anim_map.json`(506) / `animinfo_lookup.json`(418) "
                "三张表逐条交叉校验 0 不符（`gen_anim_address_map.py` 每次重跑都会复核）",
         "conf": "exact"},
        {"hop": 4, "from": "GUID", "to": "那张 CardAnim / VFX prefab",
         "how": "`anim_address_map.json`：1099 个 CardAnim 解出 1067（全是 GameObject、全在 "
                "`bundle_battleprefabs_vfxandmisc_assets_all`）；guid→资产走各包 `m_Container`",
         "conf": "exact"},
        {"hop": 5, "from": "CardAnim 名", "to": "本地 prefab 名",
         "how": "同一张表 `cardanim_to_asset`（`targetName`）",
         "conf": "exact"},
        {"hop": "旁路·卡名", "from": "我们的卡名", "to": "同名 CardAnim",
         "how": "归一化后前缀匹配（`Acid Spray` → `AcidSpraySweepAttack_troop`）。**是推断**：原版不是这么找的，"
                "只是名字恰好对得上；事件靠后缀/词根判",
         "conf": "inferred"},
        {"hop": "旁路·关键词", "from": "卡的关键词", "to": "`<关键词>Trait*` 动画",
         "how": "原版天赋动画的构词法（`ArmourTraitStart` / `SilenceTraitTrigger` / `VanguardTraitPlay`…）；"
                "**是推断**：「哪个关键词配哪个 anim」真正判据在 `CardTrait` SO（本地 0 命中）",
         "conf": "inferred"},
        {"hop": "旁路·通用", "from": "共享卡预制体 `CardPrefab`", "to": "每事件的通用挂钩",
         "how": "`CardScript.normalSummon` / `landOnGroundParticles` / `attackHitSmallParticles` / "
                "`cardDestroyFX` / `rangedAttackParticlesSO` —— **判据齐**，但**不是逐卡**",
         "conf": "exact-not-per-card"},
    ]

    out = {
        "schema": "card_vfx_by_card/v1",
        "generatedBy": "d:/4/Unity/工具/gen_card_vfx_by_card.py",
        "generatedAt": time.strftime("%Y-%m-%d %H:%M:%S"),
        "_sources": {
            "cardsEngine": rel(CARDS_ENGINE),
            "animAddressMap": rel(ADDR_MAP),
            "offensiveCards": rel(OFFENSIVE),
            "cardPrefab": rel(CARD_SCRIPT_MONO),
            "rangedSO": rel(RANGED_SO_MONO),
            "battleAnims": rel(BATTLE_ANIMS_MONO),
            "ledger": [rel(os.path.join(LEDGER_GLOB_DIR, f)) for f in (ledger_files or [])],
            "bundleSrc": rel(bundle_src) if bundle_src else None,
            "cardSoTruth": rel(args.truth) if not args.no_truth else None,
            "guidMap": rel(args.guid_map) if not args.no_truth else None,
            "enumStubs": rel(ENUM_STUBS),
        },
        "_headline": ("🔴 **2026-10-14 更正：原来写「逐卡那一跳在本地断掉」，实际是【通的】**。"
                      "卡 SO 的字段本身就是 `AssetReference`（存 GUID），GUID → 束 → 资产原名一直可解"
                      "（`guid_map.tsv` + `cardanim_to_asset`）—— 不需要那只远端 `allcards` 包。"
                      "错因：**把「载体的文件不在本地」当成了「那一跳断掉」**（只找了「卡预制体」这一个载体）。"
                      "⇒ `byCard` 里现在**有判据齐的逐卡条目**（`conf = authoritative`，"
                      "见 `stats.authoritative*`）；旧的 `inferred-*` 三档**保留**但**降级为兜底**，"
                      "按 `_conventions.confPriority` 取第一个命中的。"
                      "⚠️ 权威档的两个限定：真值快照是 2022 时代（我们池子里 168 张比它新、查不到）；"
                      "`cardAnimations` 的事件名归到 `ability` 是**我们拍的**（原版给的是枚举，已带在条目上）。"),
        "_conventions": {
            "conf": {
                "authoritative": "🔴 **原版真值**（不是推断）：卡 SO 字段（GUID）→ 束 → 资产原名 → VFX prefab。"
                                 "上限受真值快照年代约束（见 `stats.authoritative.poolCardsWithoutTruth`）",
                "exact": "判据齐（本地能逐字对上）",
                "exact-not-per-card": "判据齐，但**不是逐卡**（原版每张卡共用的那条通用链）",
                "inferred-name": "卡名 → 同名 CardAnim（**推断**，原版不是这么找的）",
                "inferred-keyword": "卡的关键词 → `<关键词>Trait*` 动画（**推断**）",
                "inferred-envcard": "进攻卡候选名 → 环境效果 VFX（**推断**，卡身份解不出）",
            },
            "confPriority": CONF_PRIORITY,
            "events": ["play", "deploy", "attack_melee", "attack_ranged", "hit", "death",
                       "ability", "trigger"],
            "eventNamesAreFrom": "`CardPresentation/Core/VfxMap.cs` 的常量（那一份是唯一正本）",
            "authoritativeEventMapping": {
                "customRangedAttackParticles": "attack_ranged（与 `CardScript.rangedAttackParticlesSO` 是同一件事的逐卡/阵营两个来源）",
                "customDeathParticles": "death（逐卡阵亡爆散）",
                "remnantBodyPrefab": "⚠️ **【键是 `death`，但语义不是阵亡】—— 2026-10-15（账 `A2083`）"
                    "反编译实证：原版它的触发时刻是【转化为残骸】（`BattleCardUI.CreateRemnantBody` ← "
                    "`CardScript.TransformIntoRemnant`），**`CardScript.UnitDeath` 全程没碰它**"
                    "（`customDeathParticles` 才是阵亡那一拍）。二者归一档的后果 = 42 张两者都有的卡"
                    "谁进 `death` **是掷硬币** ⇒ 运行时那一份已把本字段**挡在外面**（见 `RT_SKIP_SOFIELDS`），"
                    "本全量件里**保留**（带 `soField`，随时可查）。逐卡残骸体**今天没接**（走关键词）。",
                "summonAnimDelayed": "deploy（逐卡登场延迟特效）",
                "cardAbilities[].abilityLogic[].cardAnimations[]": "⚠️ **归到 `ability` 是【我们拍的】**"
                    "（`VfxMap.Ability`＝部队卡在场上发动技能）。原版真正给的是 `CardAbility.trigger` / "
                    "`AbilityLogic.chosenAbility` 两个枚举，**两个原样带在条目上**"
                    "（`abilityTrigger` / `abilityTriggerName` / `chosenAbility` / `chosenAbilityName`）⇒ 要精确到事件自己判。",
                "activeAbility.activeAbilityLogic[].cardAnimations[]": "同上（`activeAbility` 不走 `CardAbility.trigger` 那套枚举）",
            },
            "byCard_assetRefs": "`byCard[*]._assetRefs` = 卡 SO 的**非 VFX**逐卡资产绑定"
                                "（`cardImageReference` / `summonAudio` / `attackAudio` / `deathAudio`），"
                                "**不挂事件**，每条带 `assetKind`（art / audio）。只为「哪张卡对哪个原版资产」留判据。",
            "unresolved": "每条 entry 上 `prefab: null` + `unresolved: <原因>` = 原版引用了、但本地解不出。"
                          "**不许当没有**（铁律：不静默失败）。汇总见 `stats.authoritative.authoritativeUnresolved`。",
        },
        "chain": chain,
        "hop2_corrected": hop2,
        "generic": gen,
        "byKeywordTrigger": kw_table,
        "byCard": by_card,
        "envcardUnmatched": env_unmatched,
        "stats": {
            "cardsInPool": len(cards),
            "cardsCovered": len(by_card),
            "cardsUncovered": len(no_entry),
            "cardsCoveredByEvent": dict(per_ev.most_common()),
            "entriesByConf": dict(by_conf),
            "cardsByConf": {k: len(v) for k, v in sorted(cards_by_conf.items())},
            # 🔴 2026-10-14：这个数字**原来恒 0**（因为当时以为逐卡那一跳断了）。现在它是真数。
            "authoritativePerCardEntries": auth_st.get("authoritativePerCardEntries", 0),
            "authoritativeVfxEntries": auth_st.get("authoritativeVfxEntries", 0),
            "authoritativeCards": auth_st.get("authoritativeCards", 0),
            "authoritativeCardsVfxOnly": auth_st.get("authoritativeCardsVfxOnly", 0),
            "authoritative": auth_st,
            "factionArmyIds": factions_army,
            "problems": PROBLEMS,
        },
    }
    with io.open(OUT_JSON, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(out, fh, ensure_ascii=False, indent=1, sort_keys=False)
    print("[4/4] 写盘 %s（%.1f KB，耗时 %.1fs）"
          % (rel(OUT_JSON), os.path.getsize(OUT_JSON) / 1024.0, time.time() - t0))

    # ---- 🆕 运行时那一份（`Resources/` 下；只留「播哪个 prefab」这一件事）----
    #   逐卡的事件名与 `conf` **与全量那份同源同算**（同一个 `by_card`）⇒ 不可能分叉。
    #   ⛔ 丢弃的两类：`_assetRefs`（立绘/语音引用，不是「播哪个特效」）· `_unclassified`
    #      （连事件都判不出来的候选）—— 运行时按事件查，这两类查不到、留着只增加体积。
    #   🔴 **形状是给 `JsonUtility` 用的**（它不支持字典）⇒ 一律**数组 + 短键**：
    #      `{"cards":[{"id":…, "events":[{"ev":…, "es":[{"n":prefab,"c":conf,"a":cardanim}]}]}]}`
    rt_cards, rt_entries, rt_noprefab = [], 0, 0
    rt_skip_field = 0        # 🆕 `A2083`：因语义不同而**不进运行时**的条目数（见 `RT_SKIP_SOFIELDS`）
    # 🆕 2026-10-15（账 `A2082`）：**逐卡这一档必须能按【卡名】查到** ——
    #   `BattleEvent.CardId` 里装的是 **`CardDef.Name`（英文卡名）**、**不是 id**
    #   （doc → `RuleEngine/Core/BattleEvent.cs:201`；发射点 → `RuleCore.cs:7421` 传 `u.Name`），
    #   而本表的键一直是 **id** ⇒ 消费端 `VfxMap.Resolve(evt, faction, e.CardId)` 的**逐卡分支
    #   恒不命中**（2026-10-15 实测两份：运行时表 1017 个键 **1017/1017 命中卡池 id、0/1017 命中卡名**）。
    #   ⇒ 把 `name` / `faction` 一起写进运行时那份，由 `VfxMap` 在载入时建 name→id 索引。
    #   ⚠️ **`faction` 是必需的、不是装饰**：全池有 **3 组重名** —— `Terminator`（EC25 / UM_Terminator）·
    #      `Terminator Champion`（BL29 / EC26）· `Maulerfiend`（BL31 / EC30），**各跨两个阵营**
    #      ⇒ 只有 `name + faction` 唯一。少了它就只能猜，而猜错 = 静默给错特效（红线）。
    #   ⚠️ 卡池里那 103 个 32 位十六进制 id 也在 `cards_engine.json` 里，同一套查法。
    card_meta = {}
    for _c in cards:
        _i = _c.get("id")
        if _i:
            card_meta[_i] = (_c.get("name") or "", _c.get("faction") or "")
    # 🆕 2026-10-14（A1817）：逐卡**精确语音**绑定（卡 SO 的 `summonAudio`/`attackAudio`/`deathAudio`）
    rt_audio, rt_audio_noname, rt_audio_cards = 0, 0, 0
    rt_audio_names = []          # 所有写出去的 clip 名（下面拿它跟 `Resources/Art/audio/vo/` 对账）
    for _cid in sorted(by_card):
        _evs = by_card[_cid]
        evs_out = []
        for _ev in sorted(_evs):
            if _ev in ("_assetRefs", "_unclassified"):
                continue
            _lst = _evs[_ev]
            if not isinstance(_lst, list):
                continue
            es = []
            for _e in _lst:
                if not isinstance(_e, dict):
                    continue
                # 🔴 `A2083`：语义与 `death` 不同的那一族**不进运行时**（理由见 `RT_SKIP_SOFIELDS` 那段）。
                if (_e.get("soField") or "").split("[")[0] in RT_SKIP_SOFIELDS:
                    rt_skip_field += 1
                    continue
                nm = _e.get("prefab")
                if not nm:
                    rt_noprefab += 1       # 没解析出本地 prefab 名的：**不进来**，但计进出声的那条数
                    continue
                es.append({"n": nm, "c": _e.get("conf") or "", "a": _e.get("cardanim") or ""})
            if es:
                evs_out.append({"ev": _ev, "es": es})

        # ---- 🆕 A1817：逐卡【精确】语音绑定 ----
        #   数据 = `byCard[*]._assetRefs` 里 `assetKind == "audio"` 的那三条
        #   （`summonAudio` / `attackAudio` / `deathAudio`，原版卡 SO 的字段，
        #     `工具/gen_card_vfx_by_card.py` 的 `SO_ASSET_FIELDS` 收进来的）。
        #   ⚠️ **在这之前它们一条都没进运行时**（下面 `_assetRefs` 被整类跳过）⇒ 表现层只能按
        #      文件名后缀猜「这张卡这个事件该放哪条」（`VoiceLines.ForDeploy/ForAttack/ForDeath`）。
        #   ⚠️ 解不出 `assetName` 的（GUID 在 `guid_map.tsv` 里没有）**不写进来**，但计数并出声。
        audio_out = []
        for _e in (_evs.get("_assetRefs") or []):
            if not isinstance(_e, dict) or _e.get("assetKind") != "audio":
                continue
            _evt = AUDIO_SO_FIELD_EVENT.get(_e.get("soField"))
            if not _evt:                       # 不认识的字段：不猜它归哪个事件
                continue
            _n = _e.get("assetName")
            if not _n:
                rt_audio_noname += 1
                continue
            if any(a["ev"] == _evt for a in audio_out):
                # 同名多卡（`nameJoinAmbiguous`）可能给同一个字段两条 —— 取第一条，
                # ⛔ 不静默覆盖（覆盖会让「哪一条是真的」变得不可见）
                continue
            audio_out.append({"ev": _evt, "n": _n})
            rt_audio += 1
            rt_audio_names.append(_n)

        if evs_out or audio_out:
            # ⚠️ **空的那一档不写这个键**（不是写 `[]`）：`VfxMap.EnsureLoaded` 判的是
            #   `c.events == null ⇒ continue` —— 写 `[]` 会让它对这 10 张**只有语音**的卡
            #   建出**空字典**条目，`VfxMap.CardTableCards` 跟着从 979 变 989（无声的副作用）。
            _card_out = {"id": _cid}
            # 🆕 `A2082`：卡名 + 阵营（消费端按卡名查这一档时要它；理由见上面 `card_meta` 那段）。
            #   ⚠️ **查不到卡名就【不写这个键】** —— 消费端据此走「按名查不到 ⇒ 报出来」，
            #      ⛔ 别写空串充数（空串会让 name 索引里多一条无意义的键）。
            _nm, _fa = card_meta.get(_cid, ("", ""))
            if _nm:
                _card_out["nm"] = _nm
                if _fa:
                    _card_out["fa"] = _fa
            if evs_out:
                _card_out["events"] = evs_out
            if audio_out:
                _card_out["audio"] = audio_out        # 同上：只有真有绑定才写这个键
                rt_audio_cards += 1
            rt_cards.append(_card_out)
            rt_entries += sum(len(v["es"]) for v in evs_out)
    # ---- A1817：**精确语音引用的音频在不在工程里**（`Resources/Art/audio/vo/`）----
    #   ⚠️ 这个目录是 `.gitignore` 的（原版资产不进仓库）⇒ 读不到时**如实写「没查」**，不猜。
    _vo_dir = os.path.join(os.path.dirname(OUT_RUNTIME), "Art", "audio", "vo")
    _vo_missing = None
    if os.path.isdir(_vo_dir):
        _have = set()
        for _f in os.listdir(_vo_dir):
            if _f.lower().endswith((".ogg", ".wav")):
                _have.add(os.path.splitext(_f)[0])
        _vo_missing = sorted(set(n for n in rt_audio_names if n not in _have))
        if _vo_missing:
            PROBLEMS.append(
                "逐卡精确语音：有 **%d 条**卡 SO 引用的音频**不在 `Resources/Art/audio/vo/`**"
                # 🔴 2026-10-14 就地订正（铁律 5 · 账 A1831）：原来这里写「import_original_audio.py 只按
                #   card_index.json 的引用导 ⇒ 这批卡的引用不在 index 里」—— **已不成立**：
                #   那个导入器现在**也按卡 SO 的引用面导**（`used_files` 先由它填）⇒ **本条正常应为 0 条**；
                #   非 0 就说明源包里也没有那条（真缺件），别当「索引没收录」处理。
                "（`import_original_audio.py` 现在**也按卡 SO 的引用面导** ⇒ 这条正常应为 0；非 0 = 源包里也缺）"
                "：%s" % (len(_vo_missing), " · ".join(_vo_missing[:6])))
    else:
        print("   ⚠️ `%s` 不存在（`.gitignore` 的原版资产）⇒ **没查**「精确语音引用的音频在不在」"
              % rel(_vo_dir))
    rt = {
        "_note": "🔴 **运行时那一份**（`gen_card_vfx_by_card.py` 与 `数据/游戏数据/card_vfx_by_card.json` "
                 "**同一次运行产出**，⛔ 别手改、⛔ 别另写脚本再投影一次）。"
                 "只留 `n`(prefab) / `c`(conf) / `a`(cardanim)；全量那份（含立绘/语音引用、字段出处、GUID）在 `数据/` 下。"
                 "消费口 = `CardPresentation/Core/VfxMap.cs` 的 `ByCard`（按 `confPriority` 折叠，取第一个命中的）。"
                 "⚠️ 形状是**数组**不是字典 —— `JsonUtility` 不支持字典（`VfxMap.EnsureLoaded` 里注明了）。"
                 "🆕 **`cards[].audio`（2026-10-14 · A1817）** = **逐卡精确语音绑定**："
                 "`[{\"ev\":\"deploy|attack|death\",\"n\":\"<原版 AudioClip 名，不带扩展名>\"}]`，"
                 "来源 = 卡 SO 的 `summonAudio` / `attackAudio` / `deathAudio`（`ev` 的翻译表见脚本的 "
                 "`AUDIO_SO_FIELD_EVENT`，**只此一处**）；消费口 = `CardPresentation/Core/VoiceLines.cs` "
                 "的精确档（**精确优先、按文件名后缀猜的那一套兜底**）。"
                 "⚠️ `cardCount` = **文件里的卡数**（含几张**只有语音、没有 VFX** 的卡 —— 它们只带 `audio` 键、"
                 "没有 `events` 键，`VfxMap` 会跳过；`entryCount` 只数 `events` 里的条目）。",
        "generatedBy": out.get("generatedBy"),
        "generatedAt": out.get("generatedAt"),
        "confPriority": (out.get("_conventions") or {}).get("confPriority") or [],
        "cardCount": len(rt_cards),
        "entryCount": rt_entries,
        "noPrefabCount": rt_noprefab,
        # 🆕 `A2083`：因**语义与同事件不同**而没进运行时的条目数（`remnantBodyPrefab` 那一族；
        #   它们仍在全量件里、带 `soField`）—— ⛔ 不是「解析失败」，是**有意挡在外面**。
        "skippedFieldCount": rt_skip_field,
        # 🆕 A1817：逐卡精确语音的计数（`audioCardCount` = 有语音绑定的卡数）
        "audioCount": rt_audio,
        "audioCardCount": rt_audio_cards,
        "audioNoNameCount": rt_audio_noname,
        "cards": rt_cards,
    }
    os.makedirs(os.path.dirname(OUT_RUNTIME), exist_ok=True)
    with io.open(OUT_RUNTIME, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(rt, fh, ensure_ascii=False, separators=(",", ":"))
    print("[4/4] 写盘 %s（%.1f KB · %d 张卡 · %d 条 · 没解析出 prefab 的 %d 条**没进来**）"
          % (rel(OUT_RUNTIME), os.path.getsize(OUT_RUNTIME) / 1024.0,
             len(rt_cards), rt_entries, rt_noprefab)
          + "；🆕 语义与同事件不同的 %d 条也**没进来**（`remnantBodyPrefab`，见 `RT_SKIP_SOFIELDS`）"
          % rt_skip_field)
    print("   🆕 逐卡精确语音：%d 条 / %d 张卡（解不出名字的 %d 条**没进来**）"
          % (rt_audio, rt_audio_cards, rt_audio_noname)
          + ("" if _vo_missing is None else
             ("；其中 **%d 条**引用的音频不在 `Resources/Art/audio/vo/`" % len(_vo_missing))))
    if PROBLEMS:
        print("   ⚠️ 问题 %d 条：" % len(PROBLEMS))
        for p in PROBLEMS[:10]:
            print("      -", p)
    if not args.quiet:
        print("   -- 前 10 张接了东西的卡（事件 → prefab；`authoritative` 优先）--")
        for cid in sorted(by_card)[:10]:
            m = {k: (v[0].get("prefab") if isinstance(v, list) and v else v)
                 for k, v in by_card[cid].items() if k != "_assetRefs"}
            print("      %-32s %s" % (cid, json.dumps(m, ensure_ascii=False)[:150]))
    print("=" * 78)
    return 0


if __name__ == "__main__":
    sys.exit(main())
