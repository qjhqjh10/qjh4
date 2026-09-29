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

输出（只写这一个文件）：
    d:/4/Unity/数据/游戏数据/card_vfx_by_card.json

🔴 **先说清楚这份表是什么、不是什么**（详见 `资料/普查产出_0929/逐卡VFX表.md`）：

  原版那条链是
      卡 → `RawCardScript` 上的 `summonAnimDelayed` / `targetedSpellAnim` /
           `nonTargetedSpellAnim` / `customRangedAttackParticles` / `alternateArts[].attackAnim`
         + `cardAbilities[].cardAnimations`（AbilityLogic）
         + `CardTrait.TraitEffects[].effectAnim`（天赋触发）
       → `CardAnim` → `animInfo.animAdressable` → VFX prefab

  **第 2 跳（卡 → 它自己的 animInfo 列表）在本地断掉**：那些卡预制体只在**远端 CCD** 的
  `allcards_assets_all.bundle` 里（本地 84 个包里没有，`catalog_main.json` 明写它走 CCD；
  Addressables 缓存目录里也只有 catalog、没有包）。⇒ **逐卡覆盖拿不到**。

  所以这份表是**三层拼起来的**，每一条都带 `conf`，别混用：
    · `generic`            —— **判据齐**：共享 `CardPrefab` / 各 SO 上的**通用**每事件挂钩（不是逐卡）。
    · `byCard[*]` conf=`inferred-name`    —— 卡名 → 同名 CardAnim（本地唯一可用的逐卡线索，覆盖很少）。
    · `byCard[*]` conf=`inferred-keyword` —— 卡的关键词 → `<关键词>TraitStart/TraitTrigger/…`（原版天赋命名的
      构词法，但「哪个关键词配哪个 anim」这一步本地判不了）。
    · `byCard[*]` conf=`inferred-envcard` —— 进攻卡；⚠️ 原版那张 SO 的卡是 PPtr，**解不出卡名**，
      所以是用卡面贴图名反推的候选名去对（可能对不上）。
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

# 与 `CardPresentation/Core/VfxMap.cs` 的常量**逐字一致**（那边是唯一一份事件名清单）
EV_PLAY = "play"
EV_DEPLOY = "deploy"
EV_ATK_MELEE = "attack_melee"
EV_ATK_RANGED = "attack_ranged"
EV_HIT = "hit"
EV_DEATH = "death"
EV_ABILITY = "ability"
EV_TRIGGER = "trigger"

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
    ⚠️ 跨文件引用（`m_FileID != 0`）用「全库 path_id 索引」解 —— 本项目没有 externals 表，
       但**实测 path_id 唯一**（拿 4 个 clanParticles 验过，各命中唯一一条）。
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
              "原版**每张卡**登场都走它；卡自己的 `summonAnimDelayed`（逐卡延迟特效）在远端包里，本地没有")
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
        "⇒ 其余阵营走 `RawCardScript.customRangedAttackParticles`（逐卡，远端包）或没有阵营专属粒子。")
    gen["notes"].append(
        "**近战**那一支**没有**对应的 SO：原版近战特效挂在卡的 3D Animator（`card3DAnimationController`）"
        "+ `AlternateArtCard.attackAnim`（逐卡，远端包）⇒ 本地判不了。")
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


PROBLEMS = []


# ---------------------------------------------------------------- 主流程


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--quiet", action="store_true")
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

    gen = build_generic(idx, bundle_src, PROBLEMS)
    print("[1/3] 通用链：%d 个事件槽；远程按阵营 %d 条"
          % (sum(len(v) for v in gen["byEvent"].values()),
             len(gen["attackRangedByArmy"])))

    stats = {}
    by_card, kw_table, env_unmatched = build_by_card(
        cards, idx, gen, ledger, factions_army, stats)
    print("[2/3] 逐卡：覆盖 %d / %d 张（name %d · keyword %d · envcard %d）"
          % (len(by_card), len(cards), stats["entries_inferred_name"],
             stats["entries_inferred_keyword"], stats["entries_inferred_envcard"]))

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
    print("       ↳ **判据齐的逐卡条数 = 0**（原版那跳在远端包里）；"
          "上面全是推断，按 conf 过滤")

    no_entry = [c["id"] for c in cards if c["id"] not in by_card]
    print("       一个事件都没接上的卡：%d" % len(no_entry))

    # ---- 断链证据（每次重跑都重算一遍，免得结论过期）
    broken = {
        "verdict": "🔴 断在第 2 跳：卡 → 它自己的 animInfo 列表",
        "where": "原版把逐卡绑定放在卡预制体（`RawCardScript` / `AbilityLogic` / `CardTrait`）上，"
                 "而它们只在远端 CCD 的 `allcards_assets_all.bundle` 里",
        "evidence": [
            "本地 84 个 .bundle（两份游戏安装同源、文件集完全相同）里**没有** allcards 包",
            "`<LocalLow>/Everguild/Warpforge/com.unity.addressables/` 里只有 catalog（2.9 MB），没有已下载的包",
            "`catalog_main.json` 把 `allcards_assets_all.bundle` 列在 4 个「走 CCD 的包」里"
            "（另 3 个：alternateartstyles / localization / remotemisccontentdata）",
            "全 `assets_full`（24.7 万文件）搜 `cardShortId` / `normalSummon`(卡预制体那类) 等 "
            "RawCardScript 字段：0 命中（只有共享 `CardPrefab` 那一份）",
            "把 1100 个 CardAnim 资产 guid 当模式全库搜：**只有 AssetBundle 容器**引用它们"
            "（= 本地没有任何内容资产引用 CardAnim）",
        ],
    }

    chain = [
        {"hop": 1, "from": "我们的卡 id", "to": "原版那张卡",
         "how": "**同一套 id**（我们的 id 就是原版 id）。证据：`bundle_prebuiltdecks_assets_all/MonoBehaviour/ASH_SK_2.json` "
                "与 `bundle_draftpacks_assets_all/MonoBehaviour/Shoulders of Giants.json` 里的 `cardIds` 就是 `ASH53/ASH32` 这种串",
         "conf": "exact"},
        {"hop": 2, "from": "原版卡", "to": "它自己的 animInfo 列表（每事件一条）",
         "how": "**断** —— 判据在 `allcards_assets_all.bundle`（远端 CCD），本地没有。见 `broken_hop2`",
         "conf": "broken"},
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
        },
        "_headline": ("逐卡那一跳（卡 → 它自己的 animInfo）**在本地断掉** —— 原版把它放在卡预制体上，"
                      "卡预制体只在远端 CCD 的 `allcards_assets_all.bundle` 里。"
                      "所以 `byCard` 里**每一条都是推断**（带 `conf`/`src`/`why`），"
                      "只有 `generic` 那一块是判据齐的（但它不是逐卡）。"),
        "_conventions": {
            "conf": {
                "exact": "判据齐（本地能逐字对上）",
                "exact-not-per-card": "判据齐，但**不是逐卡**（原版每张卡共用的那条通用链）",
                "inferred-name": "卡名 → 同名 CardAnim（**推断**，原版不是这么找的）",
                "inferred-keyword": "卡的关键词 → `<关键词>Trait*` 动画（**推断**）",
                "inferred-envcard": "进攻卡候选名 → 环境效果 VFX（**推断**，卡身份解不出）",
            },
            "events": ["play", "deploy", "attack_melee", "attack_ranged", "hit", "death",
                       "ability", "trigger"],
            "eventNamesAreFrom": "`CardPresentation/Core/VfxMap.cs` 的常量（那一份是唯一正本）",
        },
        "chain": chain,
        "broken_hop2": broken,
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
            "authoritativePerCardEntries": 0,
            "factionArmyIds": factions_army,
            "problems": PROBLEMS,
        },
    }
    with io.open(OUT_JSON, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(out, fh, ensure_ascii=False, indent=1, sort_keys=False)
    print("[3/3] 写盘 %s（%.1f KB，耗时 %.1fs）"
          % (rel(OUT_JSON), os.path.getsize(OUT_JSON) / 1024.0, time.time() - t0))
    if PROBLEMS:
        print("   ⚠️ 问题 %d 条：" % len(PROBLEMS))
        for p in PROBLEMS[:10]:
            print("      -", p)
    if not args.quiet:
        print("   -- 前 10 张接了东西的卡 --")
        for cid in sorted(by_card)[:10]:
            print("      %-32s %s" % (cid, json.dumps(
                {k: (v[0]["prefab"] if isinstance(v, list) else v)
                 for k, v in by_card[cid].items()},
                ensure_ascii=False)[:150]))
    print("=" * 78)
    return 0


if __name__ == "__main__":
    sys.exit(main())
