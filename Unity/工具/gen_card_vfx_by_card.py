#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_card_vfx_by_card.py — 「逐卡 VFX 表」生成器（卡 id → 事件 → 本地 prefab 名）

用法（本项目惯例）：
    PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_card_vfx_by_card.py
    # 可选 --quiet   不打明细
    # 🆕 2026-10-24（执行代理 `A12`）`--check`：**只算不写**，与盘上两份产物**逐字节**比对；
    #    一致 rc=0、不同则打印差异摘要并 rc=1。对拍必须固定 `--generated-at`（否则 `generatedAt` 每次都变）。
    # 🆕 2026-10-24（`A12`）`-h` / `--help`：只打印用法并退出（rc=0），**不执行任何真活**。

只读（绝不写 d:/2/）：
  ① d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json    —— 我们的卡池（**现读 1,127 张**，id 就是原版 id；⛔ 别写死张数）
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
        · d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json
             —— **当前 build 的 `allcards` 解析产物**：1,351 张 `RawCardScript` 全字段
               （形状 `{bundle,count,cards:[{…,full_raw}]}`；判据 → 同目录 `报告.md`）。
             ⚠️ **2026-10-18（`T14`）换的**：原来用 2022 时代快照（1,195 张），我们池子里
               **168 张比它新、查不到**；换后只剩 **1 张**池内卡不在快照里。
        · d:/4/Unity/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv
             —— 卡 SO 的 `AssetGUID` → 束名 / 容器键 / **资产原名**（**6,140 个 GUID**）
               由 `工具/gen_guid_map_from_bundles.py` **直接枚举本地束的 `m_Container`** 重建。
             ⚠️ **2026-10-18（`T14`）换的**：旧表 `资料/普查产出_第二十七会话/guid_map.tsv`
               （4,985 行 / 2,372 GUID）是从 **Addressables 目录**出发的 —— 目录（2025-07-30 快照）
               里没有的 GUID **根本没被查过**，哪怕资产就在束里。实测 276 个缺口里 **272 个**
               直接枚举就解出了（SW cardassets 一处中 114 个）。
               **旧表别删**：它保留了目录「退键」那一族的解。
             本脚本**不重建**这两份，只消费。
     这两份 + `anim_address_map.json` 的 `cardanim_to_asset` = **逐卡那一跳的原版真值**
     （链路与覆盖率见下面「第 2 跳」那段更正文 + `资料/普查产出_第二十七会话/逐卡VFX_改用真值_报告.md`）。
  ⑧ `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{AbilityTrigger,AbilityEffect}.cs`
        —— 枚举名字表（签名桩）。**只为了把卡 SO 里的枚举数翻译成名字**；读不到就退回只记数字，不猜。
  ⑨ 🔴 **`d:/4/Unity/数据/游戏数据/trait_collection.json`（2026-10-23 新增，只读）**
        —— **`CardTrait` SO 的权威导出**（`工具/export_trait_collection.py` 产，77 个 `TraitPairs` /
          73 个 trait SO / 75 条 `TraitEffect`）。它给的是 `关键词 → TraitEffects[].effectAnim`
          这条**原版真值**，用来把本表 ② 那条「按构词法推出来的」`inferred-keyword` 逐条复核：
          动画名吻合的升 `authoritative`，没有依据的标 `unknown`。
        判据 → `资料/普查产出_1022/P3_M9_CardTraitCollection.md`；
        **第二个 build 交叉验证**（安卓 1.35.0 的 `sharedassets0.assets`：73/75/77 + 逐字段 0 差异）
          → `资料/普查产出_1023b/安卓存档_CardTraitSO普查.md` §三。
        ⚠️ 它**不在任何 Addressables 束里**（`guid_map_bundles.tsv` 的 `CardTrait` 0 行**是对的**）——
          数据在 player 自带的 `sharedassets0.assets` 里（PC 与安卓都是）。

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
      ⇒ **结论作废，改为**：逐卡那一跳**能拿到**，而且已经落在下面的 `conf = authoritative` 那一档。
        🔴 **2026-10-23（`T1` · Trait 全量权威化）再进一步**：旧的那三档推断**已经整档清掉**
        （`strip_unbacked_inference`）—— `conventions.confPriority` 里那些名字只为**留档**，
        **今天表里一条都不剩**（`stats.entriesByConf` 只有 `authoritative`）。

  ⇒ **所以这份表现在是四层拼起来的**，每一条都带 `conf`，**按 `conf` 过滤，别混用**
    （`_conventions.confPriority` 给了优先级顺序）：
    · `byCard[*]` conf=**`authoritative`** —— 🔴 **原版真值**：卡 SO 字段（GUID）→ 束 → 资产原名 → VFX prefab。
      **不是推断**。但 ⚠️ **两个限定**：① ~~真值快照是 2022 时代（1,195 张），**我们卡池 1,126 张里有 168 张比它新**
      ⇒ 那些卡在这一档下**查不到**（如实标，不猜）~~ 🔴 **2026-10-18（`T14`）已换源**：改用当前 build 的
      `allcards`（**1,351 张**）⇒ 只剩 **1 张**池内卡不在快照里；② `cardAnimations` 那一路的**事件名**
      自 **T21 / 2026-10-18** 起按原版 `CardAbility.trigger` 枚举归口（见 `TRIGGER_EVENT`），
      归不了口的仍保持 `ability`（= 当前不可达，宁可少播、不可错播）。
    · ~~`byCard[*]` conf=`inferred-name` / `inferred-keyword`~~ —— 🔴 **2026-10-23（`T1`）已整档删除**：
      卡 SO 里有同一条 CardAnim 的 ⇒ 判为**重复**删掉；卡 SO 与 `CardTrait` SO 都没引用的 ⇒ 判为
      **无依据、不播**（进运行时的 `notPlayed[]`）。账在 `stats.inferenceGone`。
    · ~~`byCard[*]` conf=`unknown`~~ —— 同上（5 条 `penitence` 全被判为**重复**：卡 SO 的 `ability`
      桶里本来就有那条 `PenitenceTraitTrigger`）。
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

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_card_vfx_by_card.py [--quiet] [--no-truth] [--generated-at "..."]\n    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_card_vfx_by_card.py --check --generated-at "YYYY-MM-DD HH:MM:SS"\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

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

# ---- 🔴 权威档（原版真值）的两份输入（**只读，不重建**）
# 🔴 **2026-10-18（`T14`）换源**：原来是 **2022 时代快照**（1,195 张）⇒ 我们池子里
#   **168 张比快照新、在这档查不到**；当前 build 的 `allcards` 已在本地找到（**1,351 张**）
#   ⇒ 只剩 **1 张**池内卡不在快照里（覆盖面 959/1,127 → **1,126/1,127**）。
#   判据 → `资料/普查产出_1018/allcards_1350/报告.md`（`CAB-fb8f0144…`）。
#   ⚠️ 这个文件的形状是 `{bundle,count,cards:[{…,full_raw}]}`（**卡 SO 全字段在 `full_raw`**），
#   `build_authoritative` 会自己展平（见那里的注释）。
CARD_SO_TRUTH = r"d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"
# 🔴 **2026-10-18（`T14`）新表**：旧的 `资料/普查产出_第二十七会话/guid_map.tsv`（4,985 行 / 2,372 GUID）
#   是 `_guid_resolve_probe.py map` 建的，而**它从 Addressables 目录出发** —— 目录（2025-07-30 快照）
#   里没有的 GUID **根本没被查过**，哪怕资产就在本地束里。实测：当前 build 引用的 276 个缺口 GUID，
#   **直接把束的 `m_Container` 枚举一遍就解出 272 个**（SW cardassets 一处中 114 个）。
#   ⇒ 新表 = `工具/gen_guid_map_from_bundles.py` 的产物（**6,140 个 GUID**；旧表只按缺口回填了 148 行）。
#   ⚠️ **旧表别删** —— 它保留了目录「退键」那一族的解（有些 GUID 只能走目录才解得出来）。
GUID_MAP_TSV = r"d:/4/Unity/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv"
# 🔴 **2026-10-23（`R33`）新增**：`CardTrait` SO 的权威导出（`关键词 → TraitEffect.effectAnim`）。
#   用来把 ② 那条「按 `<关键词>Trait*` 构词法推出来的」`inferred-keyword` 逐条复核。
#   判据 → `资料/普查产出_1023b/安卓存档_CardTraitSO普查.md`（安卓 1.35.0 与 PC 关服前 100% 逐字段相同）。
TRAIT_COLLECTION = r"d:/4/Unity/数据/游戏数据/trait_collection.json"
# 枚举名字表（签名桩；读不到就只记数字，不猜）
ENUM_STUBS = r"d:/2/Warpforge_code/Scripts/Assembly-CSharp"

# `conf` 的优先级（从高到低）。**消费者按这个顺序取第一个命中的**，别把推断当真值。
# 🆕 `unknown`（2026-10-23）：**判据为空**那一档（原版数据里找不到依据），排最后 ——
#   ⛔ 别把它塞到 `inferred-*` 前面（那是「我们知道它大概是什么」，`unknown` 是「我们根本不知道」）。
CONF_PRIORITY = ["authoritative", "exact", "exact-not-per-card",
                 "inferred-envcard", "inferred-name", "inferred-keyword", "unknown"]

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
# 🔴🆕 **2026-10-24（执行代理 `J1` · `C5` D14）：关键词天赋 `EffectTriggerType.OnPlay(2)` 的归置桶。**
#
# 为什么它不能进 `ability`（判据 = 全量反编译，逐句读）：
#   · `CardScript__ActivateTraitsOnSummonOrEnchantment.c` 在**召唤 / 附魔那一刻**逐条 trait 调
#     `BattleCardUI__DisplayStatusAnim`；后者只处理 `EffectTriggerType == Start(0)`（播放）
#     与 `== OnPlay(2)`（挂上持久状态框）。
#   · `BattleCardUI__DisplayTriggerAnim.c:71`（触发那一拍）**只认 `Trigger(3)` / `TriggerOnPlay(6)`**。
#   ⇒ `OnPlay(2)` **永远不在「发动技能」那一拍播**。
# 我们这一侧它的播放口 = `Core/TraitFrames.cs`（`_playBook` ← `Resources/trait_effect_bindings.json`，
#   由 `工具/gen_trait_effect_bindings.py` 从同一个 `trait_collection.json` 生成），发生在**登场**那一拍。
# 🔴 改前的缺陷：这些行同时被塞进 `ability` 桶，而 `ability` 桶是**整桶全播**的
#   ⇒ 21 张卡（`C5` 现读：AM5 · DA6 · SOR4 · SW2 · UM4）每发动一次主动技能都会顺带播一遍
#   登场框（`FlyingEffect` / `StealthEffect` / `CamouflageEffect` / `ShieldTraitEffect_Idle*`）。
# ⇒ 本桶名与 `CardPresentation/Core/VfxMap.cs` 的 `VfxMap.TraitOnPlay` **逐字一致**，
#   但**它不是事件**（没有 `EvtKind`、不在 `VfxMap.Events` 里）—— 只为「别混进 ability」而存在。
EV_TRAIT_ONPLAY = "trait_onplay"

# 🔴 **2026-10-24（执行代理 `W2` · `R4`）：`trigger_unmapped` —— 「引擎没有发射口」的触发名，**留档不播**。**
#
# 改前的缺陷（判据 = 全池现读 + `核心/VfxMap.cs` 的播放口）：
#   `trigger_event()` 只归 7 个 `AbilityTrigger` 名，**其余一律 `EV_ABILITY`** —— 而 `ability` 桶是
#   **整桶全播**的（`BattleDriver.PlaySignal` 的 `case EvtKind.Ability → VfxMap.Ability →
#   ResolveAllTimed`）⇒ 这些「别的时机」的动画会在**发动技能那一拍**播出来（错拍），
#   卡上有 `activeAbility` 时**真的会响**。
#   ⇒ 判据（同一份）也写在 `工具/gen_card_anim_order.py` 的文件头：
#     「`EvtKind.Ability` ↔ 该卡的 `activeAbility`，⛔ **不是**全部 `cardAbilities`；
#      若拿全表（含 `cardAbilities`）展开，会把出场/触发时机的动画播在『发动技能』那一拍上」。
#   本桶的语义：**原版有这个时机，但我们引擎今天发不出那个事件** ⇒ 数据**留着**（不许丢，
#   否则将来补发射口时要重新挖一遍），但**不挂任何事件**（`VfxMap` 根本不查这个名字 ⇒ 一个都不播）。
#   ⚠️ 它不是事件：⛔ 别加进 `VfxMap.Events`；⛔ 别让运行期把它并进 `ability`。
EV_TRIGGER_UNMAPPED = "trigger_unmapped"

# 🔴 **2026-10-18（T21）**：原版 `CardAbility.trigger`（`AbilityTrigger` 枚举名）→ **我们引擎真正会发的事件**。
#   以前 `cardAbilities[].cardAnimations[]` **一律归到 `ability`**（当时明写「是我们拍的」）⇒
#   `ThisCardPlayed` 那 696 条（战术/防守整族）**永远不播**（`PlayTactic` 只发 `EvtKind.Play`）。
#   这里按原版触发名归口。
#   🔴 **2026-10-24（`W2` · `R4`）就地改口径（铁律 5）**：原来最后一句是「**判据不足的一律保持
#   `ability`**」—— 那正是本条要修的缺陷（`ability` 会被整桶播）。现在三级归口：
#     ① 本表（7 个名字）→ 明确的事件；
#     ② `ABILITY_BEAT_TRIGGERS`（`ThisUnitAbilityUsed(400)`）→ `ability`（**显式**，附判据）；
#     ③ `VfxMap.IsRoutedTriggerName` 放行的名字（引擎真的会带着它发 `EvtKind.Trigger`）→ `trigger`
#        （运行期 `TryResolveTriggerPicks` 会**按名字**从 `trigger` + `ability` 两桶里挑）；
#     ④ 其余 → `trigger_unmapped`（**留档不播**）。
TRIGGER_EVENT = {
    "ThisCardPlayed": EV_PLAY,
    "Strike": EV_TRIGGER,
    "Slay": EV_TRIGGER,
    "ThisUnitDeath": EV_DEATH,
    "UnitDeathByAbility": EV_DEATH,
    "UnitDamaged": EV_HIT,
    "UnitAttack": EV_ATK_MELEE,
}

# 原版 `AbilityTrigger.ThisUnitAbilityUsed = 400` —— 它是 `activeAbility` **逐行传的虚拟时机**
# （判据 = `RuleEngine/Core/CardDef.cs:2246`「原版逐行传的虚拟时机 —— 恒 400」），
# 即「部队卡在场上发动技能」那一拍本身 ⇒ 与 `activeAbility` 同桶（`ability`）。
# ⚠️ 这是**唯一**一个按名字进 `ability` 的例外，而且必须在表里（⛔ 不许写成「其余默认 ability」）。
ABILITY_BEAT_TRIGGERS = {"ThisUnitAbilityUsed"}

# 白名单的正本（⛔ 别在这里抄第二份 —— 本仓红线：两处写同一条规则 = 迟早不一致）
VFXMAP_CS = os.path.join(os.path.dirname(os.path.dirname(OUT_RUNTIME)), "Core", "VfxMap.cs")

_ROUTED_CACHE = {}


def routed_trigger_names():
    """`VfxMap.IsRoutedTriggerName` 的白名单 = 引擎**真的会带着这个名字**发 `EvtKind.Trigger` 的那批。

    为什么在生成期**读 C# 源**而不是在 Python 里抄一张表：白名单的正本只有一份
    （`CardPresentation/Core/VfxMap.cs`），抄第二份 = 迟早不一致。
    解析法（与只读审查代理 AN1 的 `an1_bucket_matrix.py` 同一个，现读 38 个名字）：
    取 `IsRoutedTriggerName` → `CanonicalTriggerName` 之间每一行**去掉 `//` 注释后**的
    `case "…":` 标签，加上两条 `string.Equals(triggerName, KeywordTable.X, …)`。

    两条**常驻交叉校验**（不通过**直接抛**，⛔ 不许静默退化成空集 —— 那会把所有 routed 行
    误判成「未归口」，一整片动画静默不播）：
      · `MustRouteTriggerNames` 里的每个名字都必须在解析结果里（正向锁）；
      · `NoEmitTriggerNames` 里的名字**不许**在解析结果里（反向闸）。
    """
    if _ROUTED_CACHE:
        return _ROUTED_CACHE["names"]
    if not os.path.isfile(VFXMAP_CS):
        raise RuntimeError("读不到白名单正本 %s ⇒ **停手**（⛔ 不许自己猜一张表："
                           "猜错 = 整片触发动画静默不播）" % VFXMAP_CS)
    src = io.open(VFXMAP_CS, encoding="utf-8").read()

    def _array(name):
        i = src.index("static readonly string[] " + name)
        j = src.index("};", i)
        body = "\n".join(l.split("//")[0] for l in src[i:j].splitlines())
        return set(re.findall(r'"([^"]+)"', body))

    i = src.index("static bool IsRoutedTriggerName")
    j = src.index("static string CanonicalTriggerName", i)
    body = "\n".join(l.split("//")[0] for l in src[i:j].splitlines())
    names = set(m.group(1) for m in re.finditer(r'case\s+"([^"]+)"\s*:', body))
    for m in re.finditer(r'string\.Equals\(\s*triggerName\s*,\s*KeywordTable\.(\w+)', body):
        names.add(m.group(1))
    must = _array("MustRouteTriggerNames")
    noemit = _array("NoEmitTriggerNames")
    missing = sorted(n for n in must if n.lower() not in set(x.lower() for x in names))
    if missing:
        raise RuntimeError("`MustRouteTriggerNames` 里有名字不在 `IsRoutedTriggerName` 白名单：%s "
                           "⇒ 解析法过期或有编译不过的改动，**停手**" % (missing,))
    bad = sorted(n for n in noemit if n.lower() in set(x.lower() for x in names))
    if bad:
        raise RuntimeError("`NoEmitTriggerNames` 里的名字却出现在白名单：%s ⇒ 白名单自检会红，**停手**" % (bad,))
    if len(names) < 10:
        raise RuntimeError("只从 `IsRoutedTriggerName` 解析出 %d 个名字 ⇒ 解析法过期，**停手**" % len(names))
    _ROUTED_CACHE["names"] = set(n.lower() for n in names)
    _ROUTED_CACHE["raw"] = names
    return _ROUTED_CACHE["names"]


def trigger_event(name):
    """`AbilityTrigger` 枚举名 → (我们的事件名 / 桶名, 一句理由, 归类标签)。

    归类标签（供统计/断言用）：`mapped` / `ability` / `routed` / `unmapped` / `no-name`。
    ⚠️ **没有「默认落到 ability」这一档** —— 判不出来的一律 `trigger_unmapped` 并出声。
    """
    ev = TRIGGER_EVENT.get(name)
    if ev:
        return ev, "原版 `AbilityTrigger.%s` → `%s`" % (name, ev), "mapped"
    if name in ABILITY_BEAT_TRIGGERS:
        return (EV_ABILITY,
                "原版 `AbilityTrigger.%s`(400) = `activeAbility` 逐行传的虚拟时机 ⇒ 与主动技能同拍" % name,
                "ability")
    if name and name.lower() in routed_trigger_names():
        return (EV_TRIGGER,
                "原版 `AbilityTrigger.%s` ⇒ 引擎会带着这个名字发 `EvtKind.Trigger`"
                "（`VfxMap.IsRoutedTriggerName` 放行）⇒ `trigger`（运行期按名字挑这一条）" % name,
                "routed")
    if not name:
        return (EV_TRIGGER_UNMAPPED,
                "**判不出触发名**（`AbilityTrigger` 枚举名没解出来）⇒ 落 `trigger_unmapped`（留档不播）",
                "no-name")
    return (EV_TRIGGER_UNMAPPED,
            "**未归口**：原版触发 `%s`，但引擎没有带这个名字的 `EvtKind.Trigger` 发射口"
            "（`VfxMap.NoEmitTriggerNames` / 未登记）⇒ 落 `trigger_unmapped`（**留档、不参与任何播放**）；"
            "要播必须先补引擎发射口，再进 `IsRoutedTriggerName`" % name,
            "unmapped")


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
    # 🔴🆕 2026-10-24（执行代理 `J1` · `C5` D14）：`OnPlay(2)` 原来归 `ability` —— 那是**错的时机**
    #   （原版只在**召唤/附魔**那一刻挂状态框，`DisplayTriggerAnim` 那一拍只认 3/6）。
    #   改归 `trait_onplay`（理由与判据 → `EV_TRAIT_ONPLAY` 的注释）。播放口 = `Core/TraitFrames.cs`。
    "traitplay": (EV_TRAIT_ONPLAY, "EffectTriggerType.OnPlay(2)"),
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
                           # 🆕 2026-10-23：把**推导时用的那个关键词原样**记在条目上 ——
                           #   下游（`promote_keyword_traits`）要拿它去 `trait_collection.json` 里
                           #   找同一个 trait。⛔ 别从 `why` 的文案里反解（那是给人看的，不是键）。
                           "keyword": k,
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
        return {}, ("guid_map 不存在: %s（重建：`python -I -X utf8 工具/gen_guid_map_from_bundles.py --cards-only`）"
                    % rel(path))
    out = collections.defaultdict(list)
    with io.open(path, encoding="utf-8") as fh:
        for r in csv.DictReader(fh, delimiter="\t"):
            if r.get("assetGuid") and r.get("name"):
                out[r["assetGuid"]].append(r)
    return out, None


# ============================================================ 🔴 2026-10-23（特效线 `A1·R28`）
# `guid_map.tsv` 里**没有**的 GUID → **第二判据 = Addressables 二进制 catalog**。
#
# 为什么需要这一跳：`guid_map_bundles.tsv` 是「枚举各束的 `m_Container`」建的，而**有些束的容器键是短键**
#   （实测 `chaosspacemarinesemperorschildrencardassets` 的键是 `71` / `49` / `0a`…），
#   短键与 GUID 之间**只有 catalog 的 `internalId` 能对上** ⇒ 只枚举束 = 那一族 GUID 永远解不出。
#   实测（2026-10-23）：卡 SO 引用的 GUID 里**只剩 4 个**在表外，其中 **2 个**（EC57 的 attack/death 语音）
#   用 catalog 一次解出（键 `71`/`49`，AudioClip，本地束里对象在）；另 2 个（GOF2 death / BL47 summon）
#   **两个 build 的 catalog 里都没有** ⇒ 判据空（见报告）。
#
# 判据链（**复用** `工具/_guid_resolve_probe.py` 的 `lookup` —— 同一份实现，⛔ 不写第二份）：
#   GUID --catalog--> (bundle, internalId, 资源类型) --本地束 `m_Container`--> 对象 `m_Name`
# ⚠️ **只用安装版 `aa/catalog.bin`**（与本地束同 build，`buildResultHash ae516dc0…`）；
#    旧 JSON catalog（2025-07-30 快照）与本地束不是同一 build ⇒ 那条路**不当事**（见 probe 文件头）。
CAT_BIN = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/catalog.bin"
_PROBE = {"tried": False, "mod": None, "cat": None, "catErr": None}
CATALOG_HITS = []          # 本次运行靠 catalog 解出的 GUID（写进 stats，便于复查）


def _probe_module():
    """按**文件路径**加载同目录的 `_guid_resolve_probe.py`（`python -I` 不会把脚本目录放进 sys.path）。"""
    if _PROBE["tried"]:
        return _PROBE["mod"]
    _PROBE["tried"] = True
    try:
        import importlib.util
        p = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_guid_resolve_probe.py")
        spec = importlib.util.spec_from_file_location("wf_guid_resolve_probe", p)
        mod = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(mod)
        _PROBE["mod"] = mod
    except Exception as e:                      # noqa: BLE001
        PROBLEMS.append("catalog 兜底：加载 `_guid_resolve_probe.py` 失败（%s）⇒ 只按 guid_map.tsv 解" % e)
    return _PROBE["mod"]


def catalog_row(guid):
    """GUID → 与 `guid_map.tsv` **同形**的一行（解不出返回 `None`）。

    只回**类型 + 名字都拿得到、且束在本地**的命中；多个命中按 `(bundle, key, pathid)` 排序取第一条
    （确定性：同一份 catalog + 同一批束，两次跑结果逐字节相同）。
    """
    guid = (guid or "").strip().lower()
    if len(guid) != 32:
        return None
    if not os.path.isfile(CAT_BIN):
        # 没有二进制 catalog ⇒ 这一跳**没做**（`resolve_so_ref` 会据此把原因写成「第二判据不可用」，
        # ⛔ 不写成「catalog 里也没有」—— 那是没查过却当成查过）
        _PROBE["catErr"] = "安装版 `aa/catalog.bin` 不存在"
        return None
    mod = _probe_module()
    if mod is None:
        return None
    if _PROBE["cat"] is None and _PROBE["catErr"] is None:
        try:
            _PROBE["cat"] = mod.load_catalog(CAT_BIN)
        except Exception as e:                 # noqa: BLE001
            _PROBE["catErr"] = str(e)
            PROBLEMS.append("catalog 兜底：解析 `%s` 失败（%s）⇒ 只按 guid_map.tsv 解" % (CAT_BIN, e))
    cat = _PROBE["cat"]
    if cat is None:
        return None
    try:
        hits = mod.lookup(cat, guid)
    except Exception as e:                     # noqa: BLE001
        PROBLEMS.append("catalog 兜底：lookup(`%s`) 失败（%s）" % (guid, e))
        return None
    hits = [h for h in hits if not h.get("note") and h.get("name") and h.get("type")]
    if not hits:
        return None
    hits.sort(key=lambda h: (h.get("bundle") or "", str(h.get("key") or ""), h.get("pathid") or 0))
    h = hits[0]
    return {"assetGuid": guid, "bundle": h.get("bundle"), "containerKey": h.get("key"),
            "type": h.get("type"), "className": h.get("cls"), "name": h.get("name"),
            "internalId": h.get("iid"), "exact": h.get("exact"), "source": "catalog-bin"}


def resolve_so_ref(guid, guid_map, idx, want_prefab):
    """卡 SO 的一个 AssetGUID → (解析出的资产 dict, 解不出的原因)。

    三条出口：
      · `className == 'CardAnim'` → 再查 `cardanim_to_asset` 取 prefab 名（**VFX 那一族必须走到这里**）；
      · `type == 'GameObject'`   → 名字就是 prefab；
      · 其余（Sprite / Texture2D / AudioClip…）→ **资产原名本身就是结果**
        （`want_prefab=False` 的 `_assetRefs` 那一族用得上；`want_prefab=True` 时记成解不出）。
    **解不出的原因必须由调用方写进 entry 的 `unresolved` 并计数，不许静默丢。**
    """
    key = (guid or "").strip().lower()
    rs = guid_map.get(key) or []
    if not rs:
        # 🆕 第二判据：catalog + 本地束（理由见上面 `CAT_BIN` 那段注释）
        row = catalog_row(key)
        if row:
            guid_map[key] = [row]
            rs = [row]
            CATALOG_HITS.append(key)
    if not rs:
        if _PROBE.get("catErr"):
            return None, ("GUID 在 `guid_map.tsv` 里没有解出具名资产；**第二判据不可用**（%s）"
                          " ⇒ 只查了一条路，⛔ 不等于「资产不在」" % _PROBE["catErr"])
        return None, ("GUID 在两个判据下都解不出：① `guid_map.tsv` 里没有；"
                      "② 与本地束同 build 的 `catalog.bin` 里也没有这个 key"
                      "（两件事都做过 = 该资产不在这个 build 的 Addressables 里）")
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

    ⚠️ `cardAnimations` 两路的**事件名**：`cardAbilities[]` 自 **2026-10-18（T21）** 起按
    `AbilityTrigger` 枚举名归口（`trigger_event()`），**不再一律归 `ability`**；
    `activeAbility` 仍归 `ability`（它就是「部队卡在场上发动技能」）。两个枚举原样带在 entry 上。
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
        _trig_name = trig_names.get(ab.get("trigger"))
        _ev, _ev_why, _ev_cls = trigger_event(_trig_name)
        _logic = ab.get("abilityLogic") or []
        for j, lg in enumerate(_logic):
            for k, a in enumerate(lg.get("cardAnimations") or []):
                g = (a or {}).get("m_AssetGUID")
                if not g:
                    continue
                _extra = {"abilitySlot": "cardAbilities[%d].abilityLogic[%d]" % (i, j),
                          "abilityTrigger": ab.get("trigger"),
                          "abilityTriggerName": _trig_name,
                          "abilityEvent": _ev,
                          "abilityEventWhy": _ev_why,
                          # 🆕 2026-10-24（`W2` · `R4`）：归口**档**（`mapped` / `ability` / `routed` /
                          #   `unmapped` / `no-name`）。统计与断言只读这一列 ⇒ 与 `trigger_event()`
                          #   的那一次判定**同源**（⛔ 别在别处再判一遍）。
                          "abilityEventClass": _ev_cls,
                          "chosenAbility": lg.get("chosenAbility"),
                          "chosenAbilityName": eff_names.get(lg.get("chosenAbility"))}
                # 🆕 2026-10-19（W · 多候选 ③）：`randomAbilityChoice` 的**按组随机**要用的四个数。
                #   判据 = 原版 `RawCardScript__TriggerAbility.c:75-97` 逐句读：
                #     `cVar3 = *(char *)(cardAbility + 0x48)`（= `CardAbility.randomAbilityChoice`）·
                #     `lVar5 = *(longlong *)(cardAbility + 0x50)`（= `abilityLogic` 列表）·
                #     真 且 `Count > 1` ⇒ `BattleManager.GetRandomInt(0, Count)` 抽**一个** logic 播；
                #     假（或 Count ≤ 1）⇒ **按序全播**。
                #   `abilityIndex` = `cardAbilities[i]` 的下标（同一个事件里可能混着多条 CardAbility，
                #   随机要**每条各抽各的**）· `abilityLogicIndex` = 组号 j · `abilityLogicCount` = 组数。
                #   ⚠️ **组数取 `abilityLogic` 的完整长度**（不是「有 cardAnimations 的组数」）——
                #   原版抽中的组即使没有动画也占一个名额（抽中它 = 这一下没特效）。
                #   ⚠️ **只给 `randomAbilityChoice` 为真的那条 CardAbility 写**（全池现读 1 条 =
                #   `GOF12 Spanner`）—— 其余条目**不加**这四个键，免得全量件凭空胖 150 KB。
                if ab.get("randomAbilityChoice"):
                    _extra["abilityIndex"] = i
                    _extra["abilityLogicIndex"] = j
                    _extra["abilityLogicCount"] = len(_logic)
                    _extra["randomAbilityChoice"] = 1
                out.append((
                    "cardAbilities[%d].abilityLogic[%d].cardAnimations[%d]" % (i, j, k),
                    _ev, g, _extra))
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
                 "abilityEventClass": "ability",
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
    ⚠️ **同名多卡**（真值里 **311** 张卡的 `uniqueId` 是 32 位十六进制串 —— 2026-10-19 `T15` 现数；
     🔴 **不再是「同内容的第二份 dump」**：原写「259 张 · 同内容第二份」，实测①是 311、②**内容/身份都不同** ——
     117 张 `m_Name` 带 `_enchantment*` 后缀（能力生成的变体卡，`cardAbilities` 与本体不同，
     例 `e9d68c65…` = `…-Coterie of the Conceited_enchantment` ≠ 池内 `EC51`）·
     194 张是 `Z-NC` / `_oath` / `_normal` / `_tutorial` 这一族**独立卡 SO**；同名同内容的对子实测 **0**）
     ⇒ 优先取「`uniqueId` 是正常卡 id」的那一份；**仍有歧义**时全取并在条目上打 `nameJoinAmbiguous`。
    """
    if not os.path.isfile(truth_path):
        problems.append("权威档：真值快照不存在 %s ⇒ 整档跳过" % rel(truth_path))
        return {}, {"skipped": "truth snapshot missing"}
    truth = read_json(truth_path)
    # 🆕 **2026-10-18（T14）**：兼容当前 build 的 `allcards` 解析产物
    #   （`资料/普查产出_1018/allcards_1350/cards_raw.json` = `{bundle,count,cards:[{…,full_raw}]}`）。
    #   那种形态里**卡 SO 的全字段在 `full_raw`**（顶层是解析器抽出来的常用字段，不含
    #   `summonAnimDelayed` / `targetedSpellAnim` 这一族）⇒ 一律取 `full_raw`。
    if isinstance(truth, dict) and isinstance(truth.get("cards"), list):
        truth = [c.get("full_raw") or c for c in truth["cards"]]
    guid_map, err = load_guid_map(guid_map_path)
    if err:
        problems.append("权威档：" + err)
    base_guids = len(guid_map)          # 表本身的条数（不含下面 catalog 兜底补进来的）
    del CATALOG_HITS[:]                 # 同一进程里被调多次时别带上一轮的
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
        "guidMapGuids": base_guids,
        "resolvedByCatalog": len(CATALOG_HITS),
        "resolvedByCatalogGuids": list(CATALOG_HITS),
        "catalogBin": rel(CAT_BIN),
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


# ---------------------------------------------------------------- 🔴 关键词天赋 → 权威（2026-10-23 · `R33`）

# `CardTrait.EffectTriggerType` 七档（签名桩 `CardTrait.cs`）：
#   Start0 · End1 · OnPlay2 · Trigger3 · OnHand4 · FromCode5 · TriggerOnPlay6
# 我们的事件名只有 `trigger` / `ability` 两个 ⇒ 这张表**只用来校验「升权威前后事件桶不该变」**；
# ⛔ **不是**「原版语义就是这么归的」—— 原始档位照旧逐条写在 `traitTriggerType` 上（降信息，不许抹掉）。
# 🔴🆕 2026-10-24（执行代理 `J1` · `C5` D14）：`OnPlay(2)` 那一格从 `ability` 改成 `trait_onplay` ——
#   **口径本身也要跟着改**（`TRAIT_SUFFIX_EVENT` 已改）：不改这一格的话，升权威时「权威档位归到的事件
#   桶 ≠ 当前桶」会命中 `want_ev != ev` 那一支 ⇒ 全部 OnPlay 行**不升权威** ⇒ 再被
#   `strip_unbacked_inference` 当「无依据」删掉（那是错的：它们有依据，只是**不该在技能那一拍播**）。
#   判据 / 为什么不能进 `ability` → `EV_TRAIT_ONPLAY` 的注释。
TRAIT_TRIGGER_EVENT = {0: EV_TRIGGER, 2: EV_TRAIT_ONPLAY, 3: EV_TRIGGER, 6: EV_TRIGGER}
# 只在 `triggerName` 缺字时兜底把数字翻成人话（`trait_collection.json` 现在每条都带 `triggerName`）
TRAIT_TRIGGER_NAME = {0: "Start", 1: "End", 2: "OnPlay", 3: "Trigger",
                      4: "OnHand", 5: "FromCode", 6: "TriggerOnPlay"}


def load_trait_authority():
    """读 `trait_collection.json` → `{归一化 trait 名: trait dict}`（外加整份原件，写进 stats 用）。"""
    tc = read_json(TRAIT_COLLECTION)
    traits = {}
    for t in (tc.get("traits") or []):
        k = norm(t.get("name"))
        if k:
            traits[k] = t
    return traits, tc


def _keyword_of(item):
    """条目上记的**推导用关键词**（② `build_by_card` 写的 `keyword`；老条目没有 ⇒ 回空串）。"""
    return (item.get("keyword") or "").strip()


def _kw_norm(kw):
    """与 ② 那条**逐字同口径**：关键词只取第一个空格 / 冒号 / 括号之前那一段再归一化
    （`Talent: Serene Unifier` → `talent`）。⛔ 两处口径不一致 = 一半卡对不上。"""
    return norm(re.split(r"[ :(\[]", (kw or "").strip())[0])


def _effects_matching_anim(trait, anim):
    """这条 trait 里，`effectAnim`（**或它任一 `overrideByArmies` 的 effectAnim**）与 `anim` 同名的 effect。"""
    out = []
    for e in (trait.get("traitEffects") or []):
        names = [e.get("effectAnim")]
        names += [o.get("effectAnim") for o in (e.get("overrideByArmies") or [])]
        if any(n and norm(n) == norm(anim) for n in names):
            out.append(e)
    return out


def promote_keyword_traits(by_card, cards, traits, army_of_faction, idx):
    """把能对上 `CardTrait` 权威数据的 `inferred-keyword` 条目**升成 `authoritative`**。

    三条出路（逐条判，⛔ 不猜）：

    | 情况 | 处理 |
    |---|---|
    | 关键词是 trait，且这条 trait 的某条 effect 的 `effectAnim`（含阵营 override）与条目的 `cardanim` 同名 | **升 `authoritative`**（`src: cardtrait-so`）+ 带上 `traitName` / `traitTriggerName` / `playIfWarlord` / `authoritativeAnim` |
    | 关键词**不是**任何 trait（例：`penitence`） | **标 `unknown`** + 写清为什么（**判据为空**，不是「没查」） |
    | 关键词是 trait，但动画名对不上（或多个 effect 同名争不清） | **原样留在 `inferred-keyword`** + 出声（`PROBLEMS`）—— 这种情况今天 **0 条** |
    | 权威 `triggerType` 归到的事件桶 ≠ 这条现在所在的桶 | **不升** + 出声（会挪桶 ⇒ 那是另一件事，得单独裁） |

    ⚠️ **为什么升之前还要校验事件桶**：本条目的桶是「后缀 → 事件」拍的（`TRAIT_SUFFIX_EVENT`），
    而权威数据给的是 `EffectTriggerType`。两者不一致时**说明我们的构词法推断把事件也推错了**，
    这类必须人裁，⛔ 不能顺手改桶（改桶 = 换了一个播放时机，要 Unity 验收）。

    ⚠️ **`prefab` / `cardanim` 今天（2026-10-23 · 账 `新账-75②`）**会按 `overrideByArmies` **改写成变体**：
    本阵营有 override ⇒ 权威名 `applied` ≠ 原推断名时，走 `idx.prefab_of(applied)` 解出**变体的 prefab 名**
    （例：`ShieldTraitTAUPlay` → `ShieldTraitEffect_Idle TAU Variant`）并**同时**写进
    `prefab`（运行时真正 `CardEffects.Fire` 的名字）与 `cardanim`（时长 join 键 `CardAnimTime.Anim`）。
    ⛔ **三个口必须一起改** —— 只改 `authoritativeAnim` 的话运行时**照样播基础变体**
    （那正是改前的行为：6 张 Shield 卡全播 `ShieldTraitEffect_Idle`）。
    解不出 prefab（`anim_address_map.json` 里没有这条 CardAnim）⇒ **保留原 `prefab`**、
    打 `variantResolveFailed` 并 `PROBLEMS` 出声（⛔ 不猜）。
    """
    cards_by_id = {}
    for c in cards:
        cards_by_id[c.get("id")] = c
    st = {
        "source": rel(TRAIT_COLLECTION),
        "traits": len(traits),
        "inferredKeywordBefore": 0,
        "promoted": 0,
        "promotedCards": 0,
        "promotedByTrait": {},
        "markedUnknown": 0,
        "unknownRows": [],
        "leftInferred": 0,
        "leftInferredRows": [],
        "triggerTypeCorrected": [],
        "armyOverrideRows": [],
        "variantRewritten": 0,
        "variantResolveFailed": 0,
        "multiPlayBucketsBefore": 0,
        "multiPlayBucketsAfter": 0,
    }
    st["traitEffects"] = sum(len(t.get("traitEffects") or []) for t in traits.values())
    promoted_cards = set()
    by_trait = collections.Counter()

    def _is_auth(it):
        return isinstance(it, dict) and it.get("conf") == "authoritative"

    # 升之前的「同档多播桶」基数（运行时会按 `confPriority` 只留最优档、**同档 authoritative 全播**）
    for m in by_card.values():
        for lst in m.values():
            if isinstance(lst, list) and sum(1 for it in lst if _is_auth(it)) > 1:
                st["multiPlayBucketsBefore"] += 1

    for cid, m in by_card.items():
        card = cards_by_id.get(cid)
        if not card:
            continue
        for ev, lst in m.items():
            if not isinstance(lst, list):
                continue
            for it in lst:
                if not isinstance(it, dict) or it.get("conf") != "inferred-keyword":
                    continue
                st["inferredKeywordBefore"] += 1
                kw = _keyword_of(it)
                kwn = _kw_norm(kw)
                trait = traits.get(kwn)
                anim = it.get("cardanim") or ""
                # ---- ⓐ 关键词根本不是 trait ⇒ 判据为空
                if not trait:
                    it["conf"] = "unknown"
                    it["confBefore"] = "inferred-keyword"
                    it["keyword"] = kw
                    it["why"] = (
                        "⛔ **无权威依据**（2026-10-23）：`trait_collection.json`（73 个 `CardTrait` / "
                        "77 个 `TraitPairs`，安卓 1.35.0 交叉验证逐字段 0 差异）里**没有**关键词 `%s` "
                        "这个 trait ⇒「`<关键词>Trait*`」这条构词法推断（= `%s`）**升不了级**。"
                        "`prefab` 保留只为可查，⛔ 不许当权威播。"
                        % (kw, anim))
                    st["markedUnknown"] += 1
                    st["unknownRows"].append({"cardId": cid, "event": ev, "keyword": kw,
                                              "cardanim": anim, "prefab": it.get("prefab")})
                    continue
                # ---- ⓑ 关键词是 trait：找同名 effect
                effs = _effects_matching_anim(trait, anim)
                if len(effs) != 1:
                    st["leftInferred"] += 1
                    st["leftInferredRows"].append(
                        {"cardId": cid, "event": ev, "keyword": kw, "cardanim": anim,
                         "why": "权威数据里 %s" % ("找不到同名 effectAnim" if not effs else "有多条同名 effectAnim，争不清")})
                    PROBLEMS.append(
                        "关键词天赋复核：`%s` 的 `%s`（关键词 `%s`）在 `trait_collection.json` 的 trait `%s` 里%s"
                        " ⇒ **保留 `inferred-keyword`**（⛔ 不升）"
                        % (cid, anim, kw, trait.get("name"),
                           "找不到同名 effectAnim" if not effs else "有多条同名 effectAnim"))
                    continue
                e = effs[0]
                tt = e.get("triggerType")
                tn = e.get("triggerName") or TRAIT_TRIGGER_NAME.get(tt, "")
                # ---- ⓒ 事件桶校验：权威档位归到的事件必须 == 当前桶
                want_ev = TRAIT_TRIGGER_EVENT.get(tt)
                if want_ev is None or want_ev != ev:
                    st["leftInferred"] += 1
                    st["leftInferredRows"].append(
                        {"cardId": cid, "event": ev, "keyword": kw, "cardanim": anim,
                         "why": "权威 triggerType %s(%s) 归到的事件桶 != 当前桶 `%s`" % (tn, tt, ev)})
                    PROBLEMS.append(
                        "关键词天赋复核：`%s` 的 `%s` 权威 `EffectTriggerType.%s(%s)` 归到 `%s`，"
                        "而条目在 `%s` ⇒ **不升**（挪桶要人裁）"
                        % (cid, anim, tn, tt, want_ev, ev))
                    continue
                # ---- ⓓ 升权威
                applied = e.get("effectAnim")
                army = army_of_faction.get(card.get("faction"))
                for o in (e.get("overrideByArmies") or []):
                    if o.get("army") == army:
                        applied = o.get("effectAnim")
                        break
                was_tt = it.get("traitTriggerType")
                new_tt = "EffectTriggerType.%s(%s)" % (tn, tt)
                it["conf"] = "authoritative"
                it["confBefore"] = "inferred-keyword"
                it["src"] = "cardtrait-so"
                it["keyword"] = kw
                it["traitName"] = trait.get("name")
                it["traitEffectName"] = e.get("name")
                it["traitTriggerName"] = tn
                it["traitTriggerType"] = new_tt
                it["playIfWarlord"] = bool(e.get("playIfWarlord"))
                it["authoritativeAnim"] = applied
                if e.get("overrideByArmies"):
                    it["overrideByArmies"] = [
                        {"army": o.get("army"), "armyName": o.get("armyName"),
                         "effectAnim": o.get("effectAnim")}
                        for o in e["overrideByArmies"]]
                if applied and norm(applied) != norm(anim):
                    it["animDiffersFromCardanim"] = True
                    row = {"cardId": cid, "event": ev, "keyword": kw, "cardanim": anim,
                           "authoritativeAnim": applied,
                           "army": army, "armyName": card.get("faction")}
                    # 🔴 2026-10-23（账 `新账-75②`）：**改播变体** —— `prefab` / `cardanim` /
                    #   `authoritativeAnim` 三个口必须一起改（运行时 `CardEffects.Fire` 吃的是
                    #   `prefab`，时长 join 键吃的是 `cardanim`；只改一个 = 播的还是基础件）。
                    #   例：`ShieldTraitTAUPlay` → `ShieldTraitEffect_Idle TAU Variant`。
                    vprefab = idx.prefab_of(applied)
                    if vprefab:
                        row["prefab"] = vprefab
                        it["prefab"] = vprefab
                        it["cardanim"] = applied
                        it["variantPrefab"] = vprefab
                        it["inLibrary"] = in_vfx_library(vprefab)
                        row["inLibrary"] = it["inLibrary"]
                        st["variantRewritten"] += 1
                        if it["inLibrary"] is False:
                            PROBLEMS.append(
                                "变体改写：`%s` 的 `%s` 权威名 `%s` → prefab `%s`，但"
                                "**该 prefab 不在我们的效果库里**（`Assets/WarpforgeVFX/Prefabs/`）"
                                " ⇒ 运行时播不出来（照实记，⛔ 不是静默例外）"
                                % (cid, anim, applied, vprefab))
                    else:
                        it["variantResolveFailed"] = (
                            "`anim_address_map.json` 的 `cardanim_to_asset` 里没有 `%s` ⇒ "
                            "解不出变体 prefab，**保留原 `prefab=%s`**（⛔ 不猜）" % (applied, anim))
                        row["variantResolveFailed"] = True
                        st["variantResolveFailed"] += 1
                        PROBLEMS.append(
                            "变体改写：`%s` 的权威名 `%s` 在 `anim_address_map.json` 里解不出 prefab"
                            " ⇒ **保留基础件 `%s`**（这一张仍然播错，账 `新账-75②` 还开着）"
                            % (cid, applied, it.get("prefab")))
                    st["armyOverrideRows"].append(row)
                if was_tt and was_tt != new_tt:
                    # 🔴 构词法把触发档推错了的那几条（今天 = `EcstasyTraitTrigger` 10 条：
                    #    `TraitTrigger` 后缀推 `Trigger(3)`，权威是 `TriggerOnPlay(6)`）——
                    #    事件桶刚好都归 `trigger`，所以只订正档位、不挪桶。
                    st["triggerTypeCorrected"].append(
                        {"cardId": cid, "event": ev, "cardanim": anim,
                         "was": was_tt, "now": new_tt})
                it["why"] = (
                    "🔴 **权威**（2026-10-23）：关键词 `%s` → `trait_collection.json` 的 trait `%s` 的 "
                    "`TraitEffects[%s]`：`effectAnim = %s`、触发档 `%s`。"
                    "原推断（构词法 `<关键词><后缀>`）与之%s。"
                    % (kw, trait.get("name"), e.get("name"), applied, new_tt,
                       "**一致**" if not it.get("animDiffersFromCardanim")
                       else "**在动画名上不一致**（本阵营走 `overrideByArmies` ⇒ 权威名 = `%s`；"
                            "`prefab`/`cardanim` %s）"
                            % (applied,
                               ("**已改成变体** `%s`" % it["variantPrefab"])
                               if it.get("variantPrefab")
                               else "**仍为基础件**（变体解不出，见 `variantResolveFailed`）")))
                st["promoted"] += 1
                promoted_cards.add(cid)
                by_trait[trait.get("name")] += 1

    st["promotedCards"] = len(promoted_cards)
    st["promotedByTrait"] = dict(by_trait.most_common())
    for cid, m in by_card.items():
        for lst in m.values():
            if isinstance(lst, list) and sum(1 for it in lst if _is_auth(it)) > 1:
                st["multiPlayBucketsAfter"] += 1
    return st


PROBLEMS = []


# ============================================================ 🔴 2026-10-23（`T1` · Trait 全量权威化）
#  「**把兜底之类的东西都去掉**」——`inferred-*` 一条都不许留在表里。
#
#  为什么单开一步（而不是在 `build_by_card` 里就地删）：
#    · 这一步要判「**这条推断有没有权威依据**」，而权威那一档是**后**跑出来的
#      （`build_authoritative` 在 `build_by_card` 之后）—— 所以只能在两档都跑完之后再收口。
#
#  两条出路（⛔ 没有第三条，也不许「差不多就留着」）：
#    ① **卡 SO 自己引用了同一条 CardAnim**（by_card 里同 guid 的 `authoritative` 行已经存在）
#       ⇒ 这条推断是**同一件事的第二次记账**，直接删；出处记在 `duplicateOfAuthoritative` 上。
#       典型：`Lord of the Storm` —— 卡 SO 说 `Lord_of_the_storm_hit` 属于 `ThisCardPlayed`（`play` 桶，
#       `authoritative`），而按名字猜的那条把它塞进 `hit` 桶 ⇒ 那一拍原本会**多播一次**。
#    ② **没有任何权威依据** ⇒ 标 `conf: unknown` + `why` 写清「卡 SO 里没有引用这条 CardAnim」，
#       **从事件桶里拿出来**、进运行时的 `notPlayed`（消费端出声、**不播**）。
#       ⛔ 不许「保留可查就当没看见」——那正是改前的行为（`unknown` 只有一条时照样播）。


def cardanim_guid_by_name(path=GUID_MAP_TSV):
    """`CardAnim 资产名 → GUID 集合`（用于把推断行对回卡 SO 引用的那条资产）。"""
    out = collections.defaultdict(set)
    if not os.path.isfile(path):
        PROBLEMS.append("去兜底：guid_map 不在（%s）⇒ 无法判断推断行有没有权威依据" % rel(path))
        return out
    with io.open(path, encoding="utf-8") as f:
        head = f.readline().rstrip("\n").split("\t")
        try:
            i_guid, i_cls, i_name = (head.index("assetGuid"), head.index("className"),
                                     head.index("name"))
        except ValueError:
            PROBLEMS.append("去兜底：guid_map 表头不认识：%s" % head)
            return out
        for line in f:
            p = line.rstrip("\n").split("\t")
            if len(p) <= max(i_guid, i_cls, i_name):
                continue
            if p[i_cls] != "CardAnim":
                continue
            out[p[i_name]].add(p[i_guid].strip().lower())
    return out


# 🔴🆕 2026-10-24（执行代理 `A9` · 特效线「静音桶复核」第二轮）：`unbacked` 桶的**逐条证据**。
#   🆕 2026-10-24（执行代理 `A10`）：补上**余下 2 个**（`ASH86` / `AM34`）⇒ 现覆盖**全部 6 个** `unbacked` 桶。
#   🆕 2026-10-24（执行代理 `A12`）：再补 1 条 **`AM34 / Valkyrie_Hover_Dust`** —— 它落在 `_unclassified`
#      （**不在运行时 `notPlayed[]` 里**，只进全量件的 `stats.inferenceGone.unbacked[]`）；
#      证据来自 `A10` §六·1（全库唯一引用 = `AM75` 的 `play[0]`）。**同样只加文字、不改判定。**
#
#   为什么单开一张表：通用 `why` 只写「卡 SO 与 `CardTrait` SO 都没有引用它」——
#   下一个会话看不出「**到底搜过哪些字段 / 搜了多少张 / 这两件到底归谁**」，
#   于是要么再查一遍（白花一轮）、要么误当成「漏查」而按名字补回去（静默错一张）。
#
#   ⛔ **本表只【加证据文字】，不恢复任何条目**。判据（2026-10-24 现核）：
#     ① 4 张卡的卡 SO 用 **UnityPy 重读类型树**逐字段核过（116 字段 / 引用集与
#        `cards_raw.json` 的 `full_raw` **逐条相同**）⇒ 「搜过卡 SO 全部字段」不是 grep 的弱证据；
#     ② 全 24.7 万件解包资产（`d:/2/新解包资源/assets_full/`）按 **GUID + 名字**双扫；
#     ③ `CardTrait` SO（`数据/游戏数据/trait_collection.json`，73 trait / 75 TraitEffect）0 命中；
#     ④ 2022 时代那份 1,195 张卡 SO 快照（`数据/游戏数据/card_anim_order.json`）也**给同一结论**。
#   其中 2 件（`HeldrakeStrike` / `Flayed One Summon`）**在别的卡的权威桶里**——
#   挪给这 4 张 = 把别家的演出错播一遍；另 1 件的 prefab 与这张卡 `play` 的权威件**是同一件**
#   （已在 `play` 播）；剩 1 件全库零卡引用（它自己那张卡没有 `ThisCardPlayed` 能力）。
#
#   键 = `(卡 id, CardAnim 名)`；值 = 写进 `notPlayed[].why` / `stats.inferenceGone.unbacked[].why`。
UNBACKED_EVIDENCE = {
    ("AM30", "Manticore Board Down"):
        "⛔ **无权威依据、不播**（2026-10-24 `A9` 现核）：`Manticore Board Down`（CardAnim `2bb742a0…`）"
        "在**当前 build 的 1,351 张卡 SO 全 116 字段**里**零引用**（UnityPy 重读类型树逐字段核过，"
        "不是只 grep 字段名），**全 24.7 万件解包资产**里只有它自己的 CardAnim 记录引用它，"
        "`CardTrait` SO 0 命中。它落的 prefab `Artillery Manticore down 1x`（`e3fb0804…`）另由 CardAnim "
        "`Artillery Ground Big` 引用，而后者**已经在 `AM53`（play）/ `AM18`（ability）的权威桶里** ⇒ "
        "这件视觉本身没丢，⛔ 但它不是 AM30 的 `play` 件（AM30 `cardAbilities` 为空、没有 "
        "`ThisCardPlayed(0)` 能力；登场走共享 `normalSummon`）。",

    ("EC29", "HeldrakeStrike"):
        "⛔ **无权威依据、不播**（2026-10-24 `A9` 现核）：`HeldrakeStrike`（CardAnim `39dca211…`）"
        "**全库唯一**的卡 SO 引用 = **`BL53 Heldrake Strike`**（Black Legion 法术，`ThisCardPlayed(0)`）⇒ "
        "它是 BL53 的演出，挪给 EC29 = 把别家的件错播一遍。EC29 侧：卡 SO **没有近战专用字段**"
        "（`RawCardScript` 里根本没有），近战协程 `CardScript._AttackMeleeAnim_d__357__MoveNext.c` "
        "**313 行 0 处 `CardAnim`/`AddCardAnim`** ⇒ 原版近战这一拍**没有逐卡 VFX**（只有攻击音 + 位移 tween；"
        "判据全文 → `资料/普查产出_1023c/Trait_pentitence与静音桶溯源.md` §B·2）。",

    ("SAU3", "Flayed One Summon"):
        "⛔ **无权威依据、不播**（2026-10-24 `A9` 现核）：`Flayed One Summon`（CardAnim `53f3e4ca…`）"
        "**全库唯一**的卡 SO 引用 = **`SAU69 Flayed Ones Pack`**（`AbilityTrigger.Artifice(560)`）⇒ "
        "它是那张**召唤法术**的演出，**不是** Flayed One 单位自身的登场件。SAU3 的 `summonAnimDelayed` "
        "是**空 GUID**（UnityPy 类型树现读）⇒ 单位登场走共享 `normalSummon`（`Invoke Minion Card Fade *`）。",

    ("a84a5b49f0ca9144585058db6987c2a0", "Master Interromancer Hit"):
        "⛔ **无权威依据、不播**（2026-10-24 `A9` 现核）：这条按名字猜的 CardAnim "
        "`Master Interromancer Hit`（`7a4f21e3…`）**全库零卡引用**；但它落的 prefab "
        "`Psychic_Lightning_down_Intense_white`（`9d7fa450…`）**与这张卡 `play` 桶权威件 "
        "`Atk_SmiteLightning_white_Down`（`39d3e084…`）是同一件 prefab** ⇒ **视觉没丢、那一拍已经在 "
        "`play` 播**；卡 SO 也没有 `UnitDamaged(60)` 能力 ⇒ `hit` 这一拍原版没有逐卡件"
        "（保留 = 同一件播两遍）。",

    # ---- 2026-10-24 `A10`：余下两个（`A9` 那轮的 §七·1 顺手发现）
    ("ASH86", "Forewarned Board"):
        "⛔ **无权威依据、不播**（2026-10-24 `A10` 现核）：`Forewarned Board`（CardAnim `a0a1091e…`）"
        "在**当前 build 的 1,351 张卡 SO 全 116 字段**里**零引用**（UnityPy 重读类型树逐字段核过，"
        "不是只 grep 字段名：ASH86 的 5 条非空 GUID 全在 `cardAbilities` / `cardImageReference`，"
        "`targetedSpellAnim` / `nonTargetedSpellAnim` 是**空 PPtr**）；全 24.7 万件解包资产按 GUID + 名字双扫"
        "只有它自己；`CardTrait` SO 0 命中；2022 时代快照同一结论。它落的 prefab `Cosmic Serpent`（`ccef7d44…`）"
        "另由 CardAnim `CosmicSerpent`（`b277fe66…`）引用，而后者**已经在 `ASH48 Cosmic Serpent` 的 `play` "
        "权威桶里** ⇒ 这件美术本身没丢，但它是 ASH48 的演出；ASH86 的 `play` 权威件是 `Buff_Khaine`"
        "（`ed801be9…`）⇒ 补回 = 同一拍播两件。（另注：包里还有一个**只差下划线**的 prefab "
        "`Forewarned_Board`（`796d76e0…`），全库零引用、**与本条 CardAnim 不是同一件** —— 别拿它反推。）",

    ("AM34", "Valkyrie_MultiLaser_Hover_Target"):
        "⛔ **无权威依据、不播**（2026-10-24 `A10` 现核）：`Valkyrie_MultiLaser_Hover_Target`"
        "（CardAnim `7f071e26…`）**全库唯一**的卡 SO 引用 = **`AM75 Valkyrie Drop`** 的 "
        "`cardAbilities[0].abilityLogic[0].cardAnimations[1]`（`ThisCardPlayed(0)` → `play`；"
        "2022 时代快照也给同一结论）⇒ 它是 AM75 的演出，挪给 AM34 = 把别家的件错播一遍。"
        "AM34 自己的远程权威件 = `customRangedAttackParticles` → `Atk_BulletImpact_Valkyrie_Missiles`"
        "（`e4501e8d…`，prefab `BulletImpact_ValkyrieMissile`）——与猜的 prefab `Valkyrie_MultiLaser_Hover`"
        "（`3df06e24…`）**不是同一件**；卡 SO 的 116 字段里远程攻击只有这一个字段。",

    # ---- 2026-10-24 `A12`（`A10` §六·1 的顺手发现）：同一张卡的 `_unclassified` 那条也是「链到别的卡」
    ("AM34", "Valkyrie_Hover_Dust"):
        "⛔ **无权威依据、不播**（2026-10-24 `A10` 发现 · `A12` 补录）：`Valkyrie_Hover_Dust`"
        "（CardAnim `c438c0c6…`，prefab `Valkyrie Flyover Hover Dust`）**全库唯一**的卡 SO 引用 = "
        "**`AM75 Valkyrie Drop`** 的 `cardAbilities[0].abilityLogic[0].cardAnimations[0]`"
        "（`ThisCardPlayed(0)` → `play`；运行期表 `AM75.play[0]` = **authoritative**）⇒ 它是 AM75 空投演出的"
        "**首步**（第 1 步才是 `Valkyrie_MultiLaser_Hover_Target`），挪给 AM34 = 把 AM75 的整段空投演出"
        "错播一遍。AM34 的登场走共享 `normalSummon`，卡 SO 里没有别的「登场/落地」槽引用它。"
        "⚠️ 本条落在 `_unclassified`（连事件都判不出来的候选）⇒ 只进**全量件**的 "
        "`stats.inferenceGone.unbacked[]`，**不进运行时的 `notPlayed[]`**"
        "（`strip_unbacked_inference` 的 `in_runtime = not ev.startswith('_')`）—— 这是**既有设计**，"
        "⛔ 不要为了「让两边看起来一致」把它塞进运行时。",
}


def strip_unbacked_inference(by_card, cards, stats_partial):
    """把 `inferred-*` / `unknown` 从事件桶里**全部清掉**（两条出路见上面那一段）。

    返回 `(st, not_played)`：`st` 进 `stats.inferenceGone`，`not_played` 进运行时那份的 `notPlayed`。
    """
    name2guids = cardanim_guid_by_name()
    meta = {c.get("id"): (c.get("name") or "", c.get("faction") or "") for c in cards}

    st = {"before": {}, "droppedDuplicate": 0, "markedUnbacked": 0,
          "leftInferred": 0, "rows": [], "unbacked": []}
    for cid, m in by_card.items():
        # 这张卡的**权威行**覆盖了哪些 guid（含它落在哪个桶、哪个 soField）
        auth = {}
        for ev, lst in m.items():
            if not isinstance(lst, list):
                continue
            for it in lst:
                if isinstance(it, dict) and it.get("conf") == "authoritative":
                    g = (it.get("guid") or "").lower()
                    if g:
                        auth.setdefault(g, {"event": ev, "soField": it.get("soField")})
        for ev in list(m.keys()):
            lst = m[ev]
            if not isinstance(lst, list):
                continue
            keep = []
            for it in lst:
                conf = it.get("conf") or ""
                if not (conf.startswith("inferred") or conf == "unknown"):
                    keep.append(it)
                    continue
                st["before"][conf] = st["before"].get(conf, 0) + 1
                anim = it.get("cardanim") or ""
                guids = set(g for g in name2guids.get(anim, set()) if g)
                hit = None
                for g in sorted(guids):
                    if g in auth:
                        hit = (g, auth[g])
                        break
                nm, fa = meta.get(cid, ("", ""))
                # ⚠️ **真实事件桶里的**（不是 `_unclassified` / `_assetRefs`）才进运行时的 `notPlayed`
                #   —— 那两类本来就不进运行时表，记进去只会变成噪声（它们仍进全量件的 `inferenceGone`）。
                in_runtime = not ev.startswith("_")
                if hit:
                    g, where = hit
                    it["duplicateOfAuthoritative"] = {
                        "guid": g, "event": where["event"], "soField": where.get("soField")}
                    _why = ("卡 SO 引用同一条 CardAnim（guid `%s`）⇒ 它已经以 `authoritative` 记在"
                            " `%s` 桶（`%s`）；本条是按名字猜出来的**第二次记账**，删掉以免同一件播两次"
                            % (g, where["event"], where.get("soField")))
                    st["droppedDuplicate"] += 1
                    st["rows"].append({
                        "cardId": cid, "cardName": nm, "event": ev,
                        "cardanim": anim, "prefab": it.get("prefab"),
                        "confBefore": conf, "outcome": "dropped-duplicate", "why": _why})
                    if in_runtime:
                        st.setdefault("_np", []).append({
                            "id": cid, "nm": nm, "fa": fa, "ev": ev,
                            "n": it.get("prefab") or "", "a": anim,
                            "outcome": "duplicate", "why": _why})
                    continue
                # ---- ② 没有权威依据
                it["confBefore"] = conf
                it["conf"] = "unknown"
                it["why"] = (
                    "⛔ **无权威依据、不播**（2026-10-23 · `T1`）：这条是**按名字**猜出来的"
                    "（`src: %s`），而**这张卡的卡 SO 里没有任何字段引用这条 CardAnim**（`%s`），"
                    "`trait_collection.json`（73 个 `CardTrait` / 75 条 `TraitEffect`）里也没有对应的"
                    " trait effect ⇒ `inferred-*` 这一档被整批去掉，⛔ 不许当权威播。"
                    % (it.get("src") or "name-match", anim or "(空)"))
                where = "unclassified" if ev.startswith("_") else ev
                _why2 = ("按名字猜出来的动画；卡 SO 与 `CardTrait` SO 都没有引用它"
                         " ⇒ 无权威依据、不播（⛔ 不拿名字相近的顶替）")
                # 🔴 2026-10-24（`A9`）：特效线复核过的那 4 桶用**逐条证据**顶掉这句通用话
                #   （表见 `UNBACKED_EVIDENCE`；⛔ 只是把 `why` 写具体，**不恢复**条目）。
                _why2 = UNBACKED_EVIDENCE.get((cid, anim), _why2)
                st["markedUnbacked"] += 1
                st["unbacked"].append({
                    "cardId": cid, "cardName": nm, "faction": fa, "event": where,
                    "cardanim": anim, "prefab": it.get("prefab"),
                    "confBefore": conf, "why": _why2})
                if in_runtime:
                    st.setdefault("_np", []).append({
                        "id": cid, "nm": nm, "fa": fa, "ev": ev,
                        "n": it.get("prefab") or "", "a": anim,
                        "outcome": "unbacked", "why": _why2})
                continue   # ⛔ 不 keep：从事件桶里拿走
            if keep:
                m[ev] = keep
            else:
                del m[ev]

    not_played = st.pop("_np", [])
    # 复算「还有没有推断档」（必须 0）
    left = collections.Counter()
    for m in by_card.values():
        for lst in m.values():
            if isinstance(lst, list):
                for it in lst:
                    c = (it or {}).get("conf") or ""
                    if c.startswith("inferred") or c == "unknown":
                        left[c] += 1
    st["leftInferred"] = sum(left.values())
    st["leftByConf"] = dict(left)
    return st, not_played


# ---------------------------------------------------------------- 主流程


# ---------------------------------------------------------------- `--check`（只算不写）
def _wf_short(v, limit=110):
    """把任意值收成一行短串（差异摘要用；⛔ 别让它把整棵子树打出来）。"""
    try:
        s = json.dumps(v, ensure_ascii=False, sort_keys=True)
    except Exception:
        s = repr(v)
    return s if len(s) <= limit else s[:limit] + "…"


def _wf_json_diff_summary(disk_txt, new_txt, max_items=8):
    """两份 JSON 文本的结构差异 → `(最多 max_items 条摘要, 差异总处数)`。

    ⚠️ 这两份产物都是**巨大的单行/缩进文档**，逐行 diff 打出来没法看；
    这里按「路径 → 盘上 / 新算」收敛。解析失败时退回「只报字节差」。
    """
    try:
        a = json.loads(disk_txt)
        b = json.loads(new_txt)
    except Exception as e:
        return ["（JSON 解析失败，只能看字节差）：%s" % e], 1
    lines, total = [], [0]

    def _add(msg):
        total[0] += 1
        if len(lines) < max_items:
            lines.append(msg)

    def _walk(x, y, path):
        if isinstance(x, dict) and isinstance(y, dict):
            for k in sorted(set(x) | set(y), key=str):
                p = "%s.%s" % (path, k)
                if k not in x:
                    _add("%s: 盘上=<缺> / 新算=%s" % (p, _wf_short(y[k])))
                elif k not in y:
                    _add("%s: 盘上=%s / 新算=<缺>" % (p, _wf_short(x[k])))
                else:
                    _walk(x[k], y[k], p)
            return
        if isinstance(x, list) and isinstance(y, list):
            if len(x) != len(y):
                _add("%s.length: 盘上=%d / 新算=%d" % (path, len(x), len(y)))
            for i in range(min(len(x), len(y))):
                _walk(x[i], y[i], "%s[%d]" % (path, i))
            return
        if x != y:
            _add("%s: 盘上=%s / 新算=%s" % (path, _wf_short(x), _wf_short(y)))

    _walk(a, b, "$")
    return lines, total[0]


def _wf_run_check(pairs, generated_at, max_items=8):
    """`pairs = [(路径, 新算文本), …]`；逐字节比对盘上产物。返回 rc（0 = 全一致）。"""
    ok = True
    for p, txt in pairs:
        new_b = txt.encode("utf-8")
        disk_b = io.open(p, "rb").read() if os.path.exists(p) else None
        if disk_b is None:
            print("[--check] %s **盘上没有这个文件** ❌（新算 %d 字节）" % (rel(p), len(new_b)))
            ok = False
            continue
        if disk_b == new_b:
            print("[--check] %s 逐字节一致 ✅（%d 字节）" % (rel(p), len(new_b)))
            continue
        ok = False
        n = min(len(disk_b), len(new_b))
        off = next((i for i in range(n) if disk_b[i] != new_b[i]), n)
        print("[--check] %s **不同** ❌（盘上 %d 字节 / 新算 %d 字节；第一处不同 @%d）"
              % (rel(p), len(disk_b), len(new_b), off))
        try:
            disk_txt = disk_b.decode("utf-8")
        except Exception:
            print("           盘上那份不是合法 UTF-8 ⇒ 只报字节差")
            continue
        lines, total = _wf_json_diff_summary(disk_txt, txt, max_items)
        print("           结构差异 **%d 处**%s："
              % (total, "" if total <= max_items else "（只列前 %d）" % max_items))
        for l in lines:
            print("            · " + l)
    if not generated_at:
        print("ℹ️ 本次没固定 `--generated-at`（`generatedAt` 取当前时间）⇒ 该字段通常必然不同；"
              "对拍请加 `--generated-at \"YYYY-MM-DD HH:MM:SS\"`。")
    return 0 if ok else 1


# ==========================================================================================
#  🔴 2026-10-24（执行代理 `W2` · `R4`）：`cardAbilities[]` 的**三级归口账** + 桶不变量硬闸
# ==========================================================================================
def trigger_bucket_stats(by_card):
    """`cardAbilities[].cardAnimations[]` 的归口账（`mapped` / `ability` / `routed` / `unmapped`）。

    ⚠️ **只读 `abilityEventClass`** —— 那是 `trigger_event()` 那一次判定的产物
    （⛔ 不在这里按 trigger 名再判一遍：两处判同一条规则 = 迟早不一致）。
    `activeAbility` 那一支也写 `ability`（它本来就是主动技能那一拍）。
    """
    rows = collections.Counter()
    cards_of = collections.defaultdict(set)
    by_name = collections.Counter()
    cards_by_name = collections.defaultdict(set)
    for cid, m in by_card.items():
        for ev, lst in m.items():
            if not isinstance(lst, list):
                continue
            for e in lst:
                cls = e.get("abilityEventClass")
                if not cls:
                    continue
                rows[cls] += 1
                cards_of[cls].add(cid)
                if cls in ("unmapped", "no-name"):
                    nm = e.get("abilityTriggerName") or "（空）"
                    by_name[nm] += 1
                    cards_by_name[nm].add(cid)
    all_unmapped = set()
    for v in cards_by_name.values():
        all_unmapped |= v
    return {
        "rows": {k: rows[k] for k in sorted(rows)},
        "cards": {k: len(cards_of[k]) for k in sorted(cards_of)},
        "unmapped": {
            "rows": sum(by_name.values()),
            "cards": len(all_unmapped),
            "byName": {k: {"rows": by_name[k], "cards": len(cards_by_name[k])}
                       for k in sorted(by_name)},
        },
    }


def assert_no_default_ability(by_card):
    """🔴 **R4 的硬闸**：`ability` 桶里**只许**有主动技能那一拍的条目。

    判据 = `工具/gen_card_anim_order.py` 的文件头（同一份口径）：
      「`EvtKind.Ability` ↔ 该卡的 `activeAbility`，⛔ 不是全部 `cardAbilities`；
        若拿全表（含 `cardAbilities`）展开，会把出场/触发时机的动画播在『发动技能』那一拍上。」
    ⇒ `ability` 桶的每一条都必须带 `abilityEventClass == "ability"`（只有 `activeAbility` 与
    `ThisUnitAbilityUsed(400)` 这两支会写它）。

    🧨 **灭自证**：把 `trigger_event()` 的兜底改回改前那版
    （`return EV_ABILITY, "未归口（保持 `ability`…）"`）⇒ 这一条立刻抛 —— 它测的是**归口规则本身**，
    不是「表里恰好没有」。
    """
    bad = []
    for cid, m in by_card.items():
        for e in m.get(EV_ABILITY) or []:
            if not isinstance(e, dict):
                continue
            cls = e.get("abilityEventClass")
            if cls == "ability":
                continue
            bad.append((cid, e.get("abilityTriggerName"), e.get("prefab"), cls or "（没有归类标记）"))
    if bad:
        raise RuntimeError(
            "`ability` 桶里混进了 **%d 条别的时机**的动画（它们会被整桶播到「发动技能」那一拍）：%s "
            "⇒ 归口规则坏了，**停手**（判据见 `trigger_event()` 与 `gen_card_anim_order.py` 文件头）"
            % (len(bad), bad[:8]))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--quiet", action="store_true")
    ap.add_argument("--truth", default=CARD_SO_TRUTH,
                    help="原版卡 SO 全字段快照（默认 %s）" % CARD_SO_TRUTH)
    ap.add_argument("--guid-map", default=GUID_MAP_TSV,
                    help="卡 SO 的 AssetGUID → 资产名对照表（默认 %s）" % GUID_MAP_TSV)
    ap.add_argument("--no-truth", action="store_true",
                    help="跳过权威档（只重出旧那三档 + 通用链；排障用）")
    # 🆕 2026-10-20（W58-CARDFIX2）：`generatedAt` 默认是当前时间 ⇒ 两次跑不能逐字节比。
    #   加这个开关（或环境变量 `WF_GENERATED_AT`）只为**确定性复跑/对拍**，⛔ 不改默认行为。
    ap.add_argument("--generated-at", default=os.environ.get("WF_GENERATED_AT"),
                    help="固定 generatedAt（默认当前时间；复跑对拍时用）")
    # 🆕 2026-10-24（执行代理 `A12`）：**只算不写**的幂等自检开关。
    #   口径与仓里其它生成器一致（`gen_related_cards.py` / `gen_card_anim_order.py`）：
    #   算出来的**同一份文本**拿去和盘上产物**逐字节**比，一致 rc=0、不同打印差异摘要并 rc=1。
    #   ⛔ 不改默认行为：不带 `--check` 时写出去的字节与加这个开关之前逐字节相同。
    ap.add_argument("--check", action="store_true",
                    help="只算不写：与盘上两份产物逐字节比对；一致 rc=0、不同打印差异摘要并 rc=1"
                         "（对拍要固定 --generated-at）")
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

    # ---- 🔴 关键词天赋 → 权威（2026-10-23 · `R33`）：`inferred-keyword` 逐条用 `CardTrait` SO 复核
    #   为什么放在权威档**之后**：它只改 `conf == inferred-keyword` 的条目，与卡 SO 那一档不重叠；
    #   放后面才能把「升完以后同档多播桶有多少」这个运行时影响一并算出来（`stats.traitAuthority`）。
    tr_st = {"skipped": "--no-truth"}
    if not args.no_truth:
        traits, trait_tc = load_trait_authority()
        tr_st = promote_keyword_traits(by_card, cards, traits, factions_army, idx)
        tr_st["sourceGenerated"] = trait_tc.get("generated")
        _coll = trait_tc.get("collection") or {}
        tr_st["sourceCollection"] = _coll.get("m_Name")
        tr_st["traitPairs"] = len(traits)
        tr_st["cardTraitObjects"] = _coll.get("cardTraitObjectCount")
        print("[3·b/4] 关键词天赋→权威：原推断 %d 条 ⇒ **升 %d** · **unknown %d** · 仍留推断 %d"
              % (tr_st["inferredKeywordBefore"], tr_st["promoted"],
                 tr_st["markedUnknown"], tr_st["leftInferred"]))
        print("        权威源 = %s（%d 个 TraitPair / %s 个 CardTrait SO / %d 条 TraitEffect，生成于 %s）"
              % (tr_st["source"], tr_st["traitPairs"], tr_st["cardTraitObjects"],
                 tr_st["traitEffects"], tr_st.get("sourceGenerated")))
        if tr_st["triggerTypeCorrected"]:
            print("        ⚠️ 触发档**被订正** %d 条（构词法推错档、事件桶不变）：%s"
                  % (len(tr_st["triggerTypeCorrected"]),
                     " · ".join("%s %s→%s" % (x["cardanim"], x["was"], x["now"])
                                for x in tr_st["triggerTypeCorrected"][:4]) + " …"))
        if tr_st["armyOverrideRows"]:
            print("        ⚠️ 按阵营走 `overrideByArmies` 的 %d 条（**已改播变体**：%d 条 / 解不出变体 %d 条）：%s"
                  % (len(tr_st["armyOverrideRows"]), tr_st["variantRewritten"],
                     tr_st["variantResolveFailed"],
                     " · ".join("%s(%s→%s)" % (x["cardId"], x["authoritativeAnim"],
                                               x.get("prefab") or "⚠️未改")
                                for x in tr_st["armyOverrideRows"])))
        if tr_st["markedUnknown"]:
            print("        ⛔ 标 `unknown` 的 %d 条：%s"
                  % (tr_st["markedUnknown"],
                     " · ".join("%s/%s" % (x["cardId"], x["keyword"]) for x in tr_st["unknownRows"])))
        print("        ⚠️ 同档多播桶（运行时「权威同档全播」）：%d → %d"
              % (tr_st["multiPlayBucketsBefore"], tr_st["multiPlayBucketsAfter"]))
    else:
        print("[3·b/4] 关键词天赋→权威：**已跳过**（--no-truth）")

    # ---- 🔴 2026-10-23（`T1` · Trait 全量权威化）：**把 `inferred-*` / `unknown` 整档清出事件桶**
    #   必须排在权威档 + 关键词升级**之后**（判据 = 那两档跑完才知道「这条推断有没有权威依据」）。
    #   ⚠️ `--no-truth` 时**照样跑**：那时一条 `authoritative` 都没有 ⇒ 所有推断都会变成
    #      「无依据、不播」——这正是那条开关的意思（没有真值就不许播猜出来的东西）。
    inf_st, not_played = strip_unbacked_inference(by_card, cards, None)
    print("[3·c/4] 去兜底：推断/未知 %d 条 ⇒ **删重复 %d** · **无依据不播 %d** · **仍留推断 %d**"
          % (sum(inf_st["before"].values()), inf_st["droppedDuplicate"],
             inf_st["markedUnbacked"], inf_st["leftInferred"]))
    if inf_st["before"]:
        print("        改前分布：%s（按「卡 SO 有没有引用同一条 CardAnim」逐条判）" % inf_st["before"])
    for _r in inf_st["rows"]:
        print("        · 删重复：%-28s %-13s %-34s ← %s"
              % (_r["cardId"], _r["event"], _r["cardanim"], _r["why"]))
    for _r in inf_st["unbacked"]:
        print("        ⛔ 不播：%-28s %-13s %-34s → %s"
              % (_r["cardId"], _r["event"], _r["cardanim"], _r["prefab"]))
    if inf_st["leftInferred"] != 0:
        PROBLEMS.append("去兜底：**还剩 %d 条推断/未知**（%s）—— 这一档本该归零"
                        % (inf_st["leftInferred"], inf_st["leftByConf"]))

    # ---- 🔴 2026-10-24（`W2` · `R4`）：`cardAbilities[]` 的**三级归口**账 + 桶不变量硬闸 ----
    #   判据 → `trigger_event()` 的 doc（`mapped` / `ability(400)` / `routed` / `unmapped`）。
    #   ⛔ 改前这里是「其余一律 `ability`」⇒ 那些行会在**发动技能那一拍**整桶播出来。
    tb_st = trigger_bucket_stats(by_card)
    print("[3·d/4] 归口：mapped %d 行 / ability(主动技能) %d 行 / routed→trigger %d 行 / "
          "**未归口→trigger_unmapped %d 行**"
          % (tb_st["rows"].get("mapped", 0), tb_st["rows"].get("ability", 0),
             tb_st["rows"].get("routed", 0),
             tb_st["rows"].get("unmapped", 0) + tb_st["rows"].get("no-name", 0)))
    if tb_st["unmapped"]["byName"]:
        print("        未归口逐个触发名（留档、不播）：%s"
              % " · ".join("%s %d 行/%d 卡" % (k, v["rows"], v["cards"])
                           for k, v in tb_st["unmapped"]["byName"].items()))
        PROBLEMS.append(
            "归口：**%d 行 / %d 卡**的原版触发**引擎没有发射口** ⇒ 已落 `trigger_unmapped`（留档不播）：%s"
            "（要播必须先补 `EvtKind.Trigger` 发射口 + 进 `VfxMap.IsRoutedTriggerName`，判据见 "
            "`VfxMap.NoEmitTriggerNames`）"
            % (tb_st["unmapped"]["rows"], tb_st["unmapped"]["cards"],
               " · ".join(tb_st["unmapped"]["byName"].keys())))
    assert_no_default_ability(by_card)      # 🧨 归口规则坏了就停手（判据见那个方法的 doc）

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
            "🔴 **2026-10-23 现读**：真值快照 = 当前 build 的 `allcards`（**1,351 张**），"
            "我们卡池 1,127 张里**快照没有的 = 0 张**（`stats.authoritative.poolCardsWithoutTruth`）。"
            "（原文写「快照是 2022 时代 1,195 张、168 张查不到」—— 那是 `T14`/2026-10-18 换源**之前**的口径，已过期。）",
            "`cardAnimations` 那一路的**事件名**已按原版 `CardAbility.trigger` **三级归口**"
            "（2026-10-24 `W2` · `R4`；改前那句「一律归到 `ability` 是我们拍的」已作废），"
            "逐条带 `abilityTrigger` / `abilityTriggerName` / `abilityEventClass` / `chosenAbility`。",
            "🔴 **【2026-10-23 订正：本行原来那条结论是错的、已作废】** 原来写「**天赋触发**"
            "（`CardTrait.TraitEffects[].effectAnim`）仍然没做 —— 两个 build 的本地束里都没有 `CardTrait` 束 ⇒ "
            "`inferred-keyword` 那 600+ 条升不了级」。"
            "**实际是**：①「束里没有」这半句**对**（`guid_map_bundles.tsv` 里 `className == CardTrait` **0 行**"
            " **是正常的** —— 它根本不是 Addressables 资产，永远进不了那张表）；"
            "②「升不了级」这半句**错** —— 数据在 **player 自带的 `sharedassets0.assets`** 里"
            "（PC 关服前与安卓 1.35.0 **两个 build 都有**），2026-10-22 已由 `工具/export_trait_collection.py` "
            "导出成 `数据/游戏数据/trait_collection.json`（73 个 trait / 75 条 `TraitEffect` / 77 个 pair），"
            "并已**逐字段交叉验证两块 build 100% 相同** ⇒ 判据在手，"
            "**633/638 条 `inferred-keyword` 已升 `authoritative`**（见 `stats.traitAuthority`），"
            "剩 5 条 `penitence` 在 `CardTrait` 里没有对应 trait ⇒ 标 `unknown`。"
            "**错因**：把「**本地束**里没有」当成了「**原版数据**里没有」—— 只搜了 bundle / asset pack，"
            "没搜 player 自带的那三个 `.assets`（`sharedassets0` / `globalgamemanagers` / `resources`）。"
            "判据 → `资料/普查产出_1023b/安卓存档_CardTraitSO普查.md` §二·§四。",
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
         "how": "原版天赋动画的构词法（`ArmourTraitStart` / `SilenceTraitTrigger` / `VanguardTraitPlay`…）。"
                "🔴 **2026-10-23 起**：这批行**已用权威数据逐条复核** —— `CardTrait` SO 的 "
                "`TraitEffects[].effectAnim`（`数据/游戏数据/trait_collection.json`，"
                "PC 关服前与安卓 1.35.0 两个 build 逐字段相同）⇒ 动画名吻合的 **633 条升 `authoritative`**"
                "（`src: cardtrait-so`），`CardTrait` 里没有的关键词（5 条 `penitence`）标 `unknown`。"
                "⇒ 这条旁路**今天只剩 0 条仍在推断档**",
         "conf": "authoritative"},
        {"hop": "旁路·通用", "from": "共享卡预制体 `CardPrefab`", "to": "每事件的通用挂钩",
         "how": "`CardScript.normalSummon` / `landOnGroundParticles` / `attackHitSmallParticles` / "
                "`cardDestroyFX` / `rangedAttackParticlesSO` —— **判据齐**，但**不是逐卡**",
         "conf": "exact-not-per-card"},
    ]

    out = {
        "schema": "card_vfx_by_card/v1",
        "generatedBy": "d:/4/Unity/工具/gen_card_vfx_by_card.py",
        "generatedAt": args.generated_at or time.strftime("%Y-%m-%d %H:%M:%S"),
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
            # 🆕 2026-10-23（`R33`）：`CardTrait` SO 的权威导出（关键词天赋复核用）
            "traitCollection": rel(TRAIT_COLLECTION) if not args.no_truth else None,
            "enumStubs": rel(ENUM_STUBS),
        },
        "_headline": ("🔴 **2026-10-14 更正：原来写「逐卡那一跳在本地断掉」，实际是【通的】**。"
                      "卡 SO 的字段本身就是 `AssetReference`（存 GUID），GUID → 束 → 资产原名一直可解"
                      "（`guid_map.tsv` + `cardanim_to_asset`）—— 不需要那只远端 `allcards` 包。"
                      "错因：**把「载体的文件不在本地」当成了「那一跳断掉」**（只找了「卡预制体」这一个载体）。"
                      "⇒ `byCard` 里现在**有判据齐的逐卡条目**（`conf = authoritative`，"
                      "见 `stats.authoritative*`）。🔴 **2026-10-23（`T1` · Trait 全量权威化）**："
                      "旧的 `inferred-*` / `unknown` **四档已整档清掉**（`strip_unbacked_inference`）"
                      "⇒ 今天 `stats.entriesByConf` **只有 `authoritative`**；不播的那些在 "
                      "`stats.inferenceGone` 与运行时的 `notPlayed[]` 里逐条可查。"
                      "⚠️ 权威档的两个限定：① 真值快照 = **当前 build 的 `allcards`（1,351 张）**，"
                      "现读我们池子 **1,127/1,127** 都在快照里（原文「2022 时代 1,195 张 / 168 张查不到」"
                      "是 `T14` 换源前的口径，**2026-10-23 已过期**）；② `cardAnimations` 的事件名归到 "
                      "`ability` 是**我们拍的**（原版给的是枚举，已带在条目上）。"
                      "🆕 **2026-10-23**：`inferred-keyword` 那 638 条**已用 `CardTrait` SO 的权威数据逐条复核**"
                      "—— 633 条升 `authoritative`、5 条 `penitence` 无依据标 `unknown`"
                      "（见 `stats.traitAuthority`）。"),
        "_conventions": {
            "conf": {
                "authoritative": "🔴 **原版真值**（不是推断）：卡 SO 字段（GUID）→ 束 → 资产原名 → VFX prefab。"
                                 "上限受真值快照年代约束（见 `stats.authoritative.poolCardsWithoutTruth`）",
                "exact": "判据齐（本地能逐字对上）",
                "exact-not-per-card": "判据齐，但**不是逐卡**（原版每张卡共用的那条通用链）",
                "inferred-name": "~~卡名 → 同名 CardAnim（推断）~~ 🔴 **2026-10-23（`T1`）已整档删除** —— "
                                 "**今天这一档剩 0 条**（账见 `stats.inferenceGone`）",
                "inferred-keyword": "卡的关键词 → `<关键词>Trait*` 动画（推断）。"
                                    "2026-10-23 上午能对上 `trait_collection.json` 的都已升 `authoritative`"
                                    "（`src: cardtrait-so`）；**同一天（`T1`）剩下的也一并清掉** ⇒ **0 条**",
                "unknown": "⛔ **判据为空**（2026-10-23 新增）：原版数据里**找不到依据**。"
                           "🔴 **2026-10-23（`T1`）起这一档也不留在事件桶里** —— 无依据 ⇒ 不播、"
                           "写到运行时的 `notPlayed[]`（消费端逐条出声）。**今天这一档剩 0 条**",
                "inferred-envcard": "进攻卡候选名 → 环境效果 VFX（**推断**，卡身份解不出）",
            },
            "confPriority": CONF_PRIORITY,
            "events": ["play", "deploy", "attack_melee", "attack_ranged", "hit", "death",
                       "ability", "trigger",
                       # 🆕 2026-10-24（执行代理 `J1` · `C5` D14）：**不是事件**，是
                       # `EffectTriggerType.OnPlay(2)` 的归置桶（播放口 = `Core/TraitFrames.cs`，
                       # 发生在登场那一拍）。判据 → `EV_TRAIT_ONPLAY` 的注释。
                       "trait_onplay",
                       # 🆕 2026-10-24（执行代理 `W2` · `R4`）：**不是事件**，是「原版有这个时机、
                       #   但我们引擎今天**发不出**那个 `EvtKind.Trigger`」的**留档桶**
                       #   （`VfxMap` 根本不查这个名字 ⇒ 一条都不播；⛔ 别让运行期把它并进 `ability`）。
                       #   判据 → `EV_TRIGGER_UNMAPPED` 的注释。
                       "trigger_unmapped"],
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
                "cardAbilities[].abilityLogic[].cardAnimations[]": "🔴 **2026-10-24（`W2` · `R4`）起按"
                    "`CardAbility.trigger` 三级归口**（⛔ 不再一律 `ability`）：① 7 个名字 → 明确事件"
                    "（`TRIGGER_EVENT`）· ② `ThisUnitAbilityUsed(400)` → `ability`（主动技能那一拍，"
                    "`ABILITY_BEAT_TRIGGERS`）· ③ `VfxMap.IsRoutedTriggerName` 放行的名字 → `trigger`"
                    "（引擎真的会带着这个名字发 `EvtKind.Trigger`，运行期按名字挑）· ④ 其余 → "
                    "`trigger_unmapped`（**留档、不播**）。每条仍带 `abilityTrigger` / "
                    "`abilityTriggerName` / `abilityEventClass` 四个字段可逐条复核。"
                    "⚠️ 改前那句「归到 `ability` 是【我们拍的】」已作废 —— `ability` 桶会被**整桶播**到"
                    "「发动技能」那一拍，把别的时机的动画塞进去 = 错拍（判据 = `gen_card_anim_order.py` 文件头）。",
                "activeAbility.activeAbilityLogic[].cardAnimations[]": "同上（`activeAbility` 不走 `CardAbility.trigger` 那套枚举）",
            },
            "byCard_assetRefs": "`byCard[*]._assetRefs` = 卡 SO 的**非 VFX**逐卡资产绑定"
                                "（`cardImageReference` / `summonAudio` / `attackAudio` / `deathAudio`），"
                                "**不挂事件**，每条带 `assetKind`（art / audio）。只为「哪张卡对哪个原版资产」留判据。",
            "unresolved": "每条 entry 上 `prefab: null` + `unresolved: <原因>` = 原版引用了、但本地解不出。"
                          "**不许当没有**（铁律：不静默失败）。汇总见 `stats.authoritative.authoritativeUnresolved`。"
                          "⚠️ 2026-10-23（`R28`）起解不出会走**两条判据**：① `guid_map.tsv`"
                          "（枚举各束 `m_Container`）→ ② 与本地束同 build 的 `catalog.bin`"
                          "（补「短键」那一族 → 再用束内 `m_Container` 取名字）。两条都没有 = 该资产"
                          "**不在这个 build 的 Addressables 里**（判据空，⛔ 不是「没查」）。"
                          "② 解出的 GUID 记在 `stats.authoritative.resolvedByCatalogGuids`。",
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
            # 🔴 2026-10-23（`R33`）：关键词天赋复核（`inferred-keyword` → `authoritative` / `unknown`）
            "traitAuthority": tr_st,
            # 🔴 2026-10-23（`T1` · Trait 全量权威化）：**推断/未知整档清出事件桶**的账
            #   （两条出路见 `strip_unbacked_inference` 的注释：删重复 / 无依据不播）。
            #   `leftInferred` 必须 = 0；非 0 会同时进 `problems`。
            "inferenceGone": inf_st,
            # 🔴 2026-10-24（`W2` · `R4`）：`cardAbilities[]` 的**三级归口账**
            #   （`rows` / `cards` 按档；`unmapped.byName` = 留档不播的那些触发名）。
            #   断言/台账直接读这一节，⛔ 别在脚本里另算一遍。
            "triggerBuckets": tb_st,
            "notPlayed": not_played,
            "factionArmyIds": factions_army,
            "problems": PROBLEMS,
        },
    }
    # ⚠️ 先把文本算进变量、确认没错再写（本仓踩过 `io.open(p,'wb').write(<表达式>)` **先截断后求值**的坑）。
    #   `json.dump(obj, fh)` 写出来的就是 `json.dumps(obj)` 的结果（**无尾换行**）⇒ 改成先算后写**逐字节不变**；
    #   `--check` 拿的也是这同一份文本 ⇒ 「算出来的」与「写出去的」不可能分叉。
    full_txt = json.dumps(out, ensure_ascii=False, indent=1, sort_keys=False)
    if not args.check:
        with io.open(OUT_JSON, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(full_txt)
        print("[4/4] 写盘 %s（%.1f KB，耗时 %.1fs）"
              % (rel(OUT_JSON), os.path.getsize(OUT_JSON) / 1024.0, time.time() - t0))
    else:
        print("[4/4] --check：**不写盘** %s（算得 %.1f KB）"
              % (rel(OUT_JSON), len(full_txt.encode("utf-8")) / 1024.0))

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
    #   ⚠️ **`faction` 是必需的、不是装饰**：全池有 **3 组重名** —— `Terminator`（EC25 / UM103）·
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
                # 🆕 2026-10-19（W · 多候选 ③）：`randomAbilityChoice == true` 的那条 CardAbility
                #   带上「按组随机」需要的四个数（`rc` 标志 · `ai` = `cardAbilities[i]` 下标 ·
                #   `gj` = `abilityLogic[j]` 组号 · `gn` = `abilityLogic` 组数）。
                #   消费 = `Core/VfxMap.cs` 的 `ResolveAllTimed(..., rng)`（只有传 rng 才抽组；
                #   不传 = 旧行为全播 ⇒ 今天无可观测变化）。判据见 `so_refs` 那一段的注释。
                #   ⚠️ **只有 `rc == 1` 才写这四个键**（全池现读 1 条 CardAbility = `GOF12 Spanner`）；
                #   其余条目缺省 0（JsonUtility 缺字段给默认值）。
                _re = {"n": nm, "c": _e.get("conf") or "", "a": _e.get("cardanim") or ""}
                if _e.get("randomAbilityChoice"):
                    _re["rc"] = 1
                    _re["ai"] = int(_e.get("abilityIndex") or 0)
                    _re["gj"] = int(_e.get("abilityLogicIndex") or 0)
                    _re["gn"] = int(_e.get("abilityLogicCount") or 0)
                es.append(_re)
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
                 "🆕 **2026-10-19（W）**：`randomAbilityChoice == true` 的候选多带四个键 —— "
                 "`rc`(1=该 CardAbility 是「随机抽一组」) / `ai`(`cardAbilities[i]` 下标) / "
                 "`gj`(`abilityLogic[j]` 组号) / `gn`(`abilityLogic` 组数)；判据 = "
                 "`RawCardScript__TriggerAbility.c:75-97`，消费 = `VfxMap.ResolveAllTimed(..., rng)`。"
                 "只有 `rc==1` 才写（现读全池 1 条 = `GOF12 Spanner`），其余条目缺省 0。"
                 "消费口 = `CardPresentation/Core/VfxMap.cs` 的 `ByCard`（按 `confPriority` 折叠：只留最优档，"
                 "**同档 `authoritative` 全播**）。"
                 "🔴 **2026-10-23（`T1` · Trait 全量权威化）**：`es[].c` **只剩 `authoritative` 一种** —— "
                 "`inferred-*` / `unknown` 已由 `strip_unbacked_inference` **整档清出事件桶**"
                 "（删重复 / 无依据不播），改前的 22 条（17 `inferred-name` + 5 `unknown`）现在一个都不在表里。"
                 "**不播的那些去哪了**：`notPlayed[]`（每条的 `id/nm/fa/ev/n/a/why`）—— 消费端**必须出声**，"
                 "⛔ 不许静默。"
                 "🆕 **2026-10-24（`W2` · `R4`）**：`events[].ev` 里可能出现 **`trigger_unmapped`** —— 它"
                 "**不是事件**（`VfxMap` 不查这个名字 ⇒ 一条都不播），只留档「原版有这个触发、我们引擎今天"
                 "没有对应发射口」的那批行；⛔ 别让运行期把它并进 `ability`（那会把别的时机的动画播在"
                 "「发动技能」那一拍）。计数见 `stats.triggerBuckets`。"
                 "⚠️ 形状是**数组**不是字典 —— `JsonUtility` 不支持字典（`VfxMap.EnsureLoaded` 里注明了）。"
                 "🆕 **`cards[].audio`（2026-10-14 · A1817）** = **逐卡精确语音绑定**："
                 "`[{\"ev\":\"deploy|attack|death\",\"n\":\"<原版 AudioClip 名，不带扩展名>\"}]`，"
                 "来源 = 卡 SO 的 `summonAudio` / `attackAudio` / `deathAudio`（`ev` 的翻译表见脚本的 "
                 "`AUDIO_SO_FIELD_EVENT`，**只此一处**）；消费口 = `CardPresentation/Core/VoiceLines.cs` "
                 "的精确档（**精确优先、按文件名后缀猜的那一套兜底**）。"
                 "🔴 **2026-10-23 补注（铁律 5 · 新账-76 现核）**：`普查产出_1023c/审查_每卡VFX与音效.md` "
                 "把它记成「运行期无消费者 / 死字段」—— **不成立**：它只是**不在 `VfxMap.RtCard` 里**"
                 "（`JsonUtility` 会忽略 DTO 没声明的键 ⇒ 两份 DTO 各取所需，这是**故意**的），"
                 "真正的消费口是 `Core/VoiceLines.cs` 自己声明的 `RtAudio/RtCard/RtFile`"
                 "（`EnsureExactLoaded` → `TryPickExact` ← `BattleDriver.SpeakFor:5726`）。"
                 "⇒ **这条数据是活的，⛔ 别删、别当冗余剪掉**；`VfxMap.RtCard` 不吃它 ≠ 无消费者。"
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
        # 🔴 2026-10-23（`T1`）：**有原版动画、但没有权威依据、因此不播**的那些 (卡, 事件)。
        #   消费端 = `VfxMap.EnsureLoaded`（逐条 `Debug.LogWarning` 出声 + 计数给断言）。
        #   ⛔ 别把它们塞回 `cards[].events`（那会让「无依据也播」回来）。
        "notPlayedCount": len(not_played),
        "notPlayed": not_played,
        "cards": rt_cards,
    }
    # 同全量件：先算文本、`--check` 时**不写盘**（`json.dumps(..., separators=(",",":"))` 与
    # `json.dump(rt, fh, ...)` 的输出逐字节相同 ⇒ 不带 `--check` 时落盘内容不变）。
    rt_txt = json.dumps(rt, ensure_ascii=False, separators=(",", ":"))
    if not args.check:
        os.makedirs(os.path.dirname(OUT_RUNTIME), exist_ok=True)
        with io.open(OUT_RUNTIME, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(rt_txt)
        print("[4/4] 写盘 %s（%.1f KB · %d 张卡 · %d 条 · 没解析出 prefab 的 %d 条**没进来**）"
              % (rel(OUT_RUNTIME), os.path.getsize(OUT_RUNTIME) / 1024.0,
                 len(rt_cards), rt_entries, rt_noprefab)
              + "；🆕 语义与同事件不同的 %d 条也**没进来**（`remnantBodyPrefab`，见 `RT_SKIP_SOFIELDS`）"
              % rt_skip_field)
    else:
        print("[4/4] --check：**不写盘** %s（算得 %.1f KB · %d 张卡 · %d 条）"
              % (rel(OUT_RUNTIME), len(rt_txt.encode("utf-8")) / 1024.0,
                 len(rt_cards), rt_entries))
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
    # 🆕 2026-10-24（`A12`）：`--check` 在这里收口 —— 两份文本都是**本次实算**的，
    #   与盘上产物逐字节比对；一致 rc=0、不同打印差异摘要并 rc=1。**全程不写盘。**
    if args.check:
        return _wf_run_check([(OUT_JSON, full_txt), (OUT_RUNTIME, rt_txt)],
                             args.generated_at)
    return 0


if __name__ == "__main__":
    sys.exit(main())
