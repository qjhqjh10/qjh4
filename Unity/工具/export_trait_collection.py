#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
export_trait_collection.py — 原版 `CardTraitCollection` / `CardTrait` 那批 SO → 工程可用的 JSON

## 数据源

  d:/2/Warhammer 40k Warpforge/Warpforge_Data/sharedassets0.assets
      · `CardTraitCollection`（m_Name = "TraitsCollection"）pathID = 531
      · `CardTrait` 派生 SO 73 个 pathID = 458..530（script = CardTrait / CardTraitDuty /
        CardTraitFerocity / CardTraitOath 四类，字段布局完全相同，子类只重写 IsActive）
      · 77 条 `TraitPairs`（其中 2 条 traitConfig 为 null：berzerk / dodge —— 原版就是没配）
      · 那 8 个 HUD / 资源图标 Sprite（pathID 见 JSON 的 "spriteIds"）
      · 每个 CardTrait 的图标 Sprite（`_traitIcon`）
  d:/2/Warhammer 40k Warpforge/Warpforge_Data/globalgamemanagers.assets
      · MonoScript 表（用来把 SO 的 m_Script PPtr 反查成类名）
  .../Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/*cardanims*_assets_all.bundle
      · 15 个包：effectAnim 的 AssetReference GUID → CardAnim 语义名（AC 的 container 键就是 GUID）

## 为什么必须手工解析原始字节（不能 `obj.read()`）

这批 MonoBehaviour **没有 type tree**（本 build 里裸 player 文件那一层普遍没有）。
UnityPy 对没有 type tree 的 MonoBehaviour 调 `obj.read()` 会在 `read_typetree` 里抛
`ValueError: Expected to read 2416 bytes, but only read 48 bytes`。
⇒ 只能按 Unity 6（6000.2f2）的**固定二进制布局**从 `obj.byte_start` 读 `obj.byte_size` 字节手工走。
布局（判据：`资料/普查产出_1022/P3_M9_CardTraitCollection.md` §三）：

    +0    PPtr m_GameObject   (int32 fileID + int64 pathID)  = 12 B
    +12   int32 m_Enabled
    +16   PPtr m_Script       (int32 fileID + int64 pathID)  = 12 B
    +28   string m_Name       (int32 len + bytes + 4 字节对齐)

> ⚠️ 这是**本 build 实测**的布局，不是通用保证。换 build 要重新验
> （验法见 P3 报告 §三：拿有 type tree 的包反推 m_Script 的偏移）。

## 怎么校验

  1. **每个对象** 解析完断言 `读到的字节数 == obj.byte_size`（P3 已验 73/73 通过）——
     少读/多读都会当场炸，不会静默产出半截数据。
  2. `TraitPairs` 必须 77 条；CardTrait 对象必须 73 个；collection 对象必须是 531。
  3. 每个 `effectAnim` / `overrideByArmies[].effectAnim` 的 GUID **必须**能在
     15 个 cardanim 包里换到 CardAnim 名 —— 换不到 = 直接报错退出（⛔ 不写 null 糊过去）。
  4. 换出来的「GUID → 名」再与 P3 报告 §4.3 的表逐条对账（下表 `EXPECTED_GUID_MAP`）——
     两条独立路径（现场扫包 vs 报告抄录）必须一致，不一致就报出来。
  5. `--check` 时把算出来的 JSON 与盘上那份**逐字节**比。

判据出处：`资料/普查产出_1022/P3_M9_CardTraitCollection.md`
（数据源 / 二进制布局 / 真值表 / GUID 表 全在那份）。

## 用法

    "D:/2/Warpforge_tools/py312/python.exe" -X utf8 d:/4/Unity/工具/export_trait_collection.py
        → 写 d:/4/Unity/数据/游戏数据/trait_collection.json
    ... --check
        → 只校验 + 与盘上那份比，**不写文件**；不一致/缺失则退出码 1

⚠️ 幂等：同样的源跑两次产物**逐字节相同**。为此 `"generated"` 是**固定常量**（⛔ 别改成
   `datetime.now()`，那会让每天跑出来的文件都不一样）。所有字典按 pathID 排序输出。

⚠️ 行尾：产物一律 **LF**（`newline='\n'`）。写完会**二进制读回**数一遍 `\r\n` 与 `\n` ——
   这个工程对行尾极敏感（CLAUDE.md §二 那四次踩坑）。
⚠️ `io.open(p,'wb').write(<表达式>)` 会**先截断文件再求值** ⇒ 本脚本先把内容算进变量，
   确认无误才写。
"""
import argparse
import hashlib
import io
import json
import os
import struct
import sys

# ---------------------------------------------------------------- 路径

SRC_ASSETS = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/sharedassets0.assets"
SRC_MONOSCRIPTS = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/globalgamemanagers.assets"
AA_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
OUT_DEFAULT = r"d:/4/Unity/数据/游戏数据/trait_collection.json"

# 产物里的日期。**固定常量**，不是运行日期 —— 见文件头「幂等」那一段。
GENERATED_DATE = "2026-10-22"

# ---------------------------------------------------------------- 枚举（判据 = 反编译签名桩）

# `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`（137 行，全表照抄）
DEFINED_TRAIT_NAMES = {
    0: "none", 5: "ephemeral", 10: "rally", 20: "sneakAttack", 40: "fast", 70: "stealth",
    80: "shield", 100: "stun", 110: "syphon", 120: "backlash", 130: "jam", 140: "ward",
    145: "wardWb", 150: "cantAttack", 160: "tainted", 180: "unstoppable", 190: "coward",
    200: "poisoned", 220: "bloodThirst", 230: "dropPod", 240: "dodge", 250: "unstunnable",
    260: "slay", 280: "rage", 300: "doubleRage", 310: "invulnerable", 320: "corrupt",
    330: "markOfChaos", 340: "giantKiller", 350: "blast", 360: "berzerk", 370: "maintenance",
    380: "poisonous", 390: "flying", 400: "activeAbility", 405: "psychicAbility",
    407: "plasmaAbility", 410: "passiveAbility", 420: "whileInPlayAbility", 430: "survivor",
    440: "survivorSpent", 450: "relentless", 455: "transient", 460: "flank", 470: "sacrifice",
    480: "vulnerable", 490: "reload", 500: "notCollectable", 510: "secret", 520: "terror",
    530: "trap", 540: "secretOrder", 550: "secretIdentity", 570: "fear", 580: "meltdown",
    590: "massive", 600: "resistant", 610: "pack", 620: "psyker", 625: "reactor",
    630: "firstStrike", 640: "precognition", 650: "swarm", 660: "psychicPower",
    665: "plasmaPower", 670: "sentinel", 680: "frenzied", 700: "spikes", 710: "horns",
    720: "daemonhost", 730: "summonRift", 750: "quest", 755: "questCompleted", 760: "requiem",
    770: "landing", 780: "mutation", 790: "chaosOnly", 820: "chooseOne", 840: "resolution",
    845: "talent", 850: "bastion", 860: "mission", 880: "nonCombatant", 900: "piercing",
    910: "tide", 920: "mob", 930: "concussive", 940: "large", 950: "mekaniak", 960: "support",
    970: "pindown", 975: "blind", 980: "vanguard", 985: "camouflage", 990: "sniper",
    1000: "codex", 1010: "unstable", 1020: "armour", 1030: "regeneration", 1040: "artifice",
    1050: "remnant", 1060: "markOfKhorne", 1070: "markOfNurgle", 1080: "markOfSlaanesh",
    1090: "markOfTzeentch", 1100: "daemonicAllegiance", 1110: "strike", 1130: "combo",
    1120: "shuriken", 1140: "waystone", 1150: "synapse", 1160: "longrange", 1170: "markerlight",
    1180: "companion", 1185: "pray", 1190: "notDestroyRemnant", 1200: "ambush", 1210: "uprising",
    1220: "duty", 1225: "regiment", 1230: "teleport", 1235: "agenda", 1240: "stomp",
    1245: "stimulation", 1250: "ecstasy", 1255: "cruelty", 1260: "huntMark", 1265: "ferocity",
    1270: "dontReturnFerocity", 1275: "oath", 1276: "oathInAllTurns", 1277: "oathDouble",
    1278: "oathTripleActivation", 1280: "sentry",
}

# `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTrait.cs`（TraitStackType / EffectTriggerType）
TRIGGER_NAMES = {
    0: "Start",
    1: "End",
    2: "OnPlay",
    3: "Trigger",
    4: "OnHand",
    5: "FromCode",
    6: "TriggerOnPlay",
}
STACKABLE_NAMES = {0: "replace", 10: "add", 20: "count"}

# `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs`
ARMY_NAMES = {
    0: "Neutral",
    10: "Ultramarines",
    20: "Goff",
    30: "SaimHann",
    40: "Sautekh",
    50: "BlackLegion",
    60: "Leviathan",
    70: "TauEmpire",
    80: "Sororitas",
    90: "Genestealers",
    100: "AstraMilitarum",
    110: "DarkAngels",
    120: "EmperorsChildren",
    130: "SpaceWolves",
}

# MonoScript pathID（判据：P3 报告 §一 #2；本脚本启动时会**现场复核**这 8 条）
MONOSCRIPT_IDS = {
    1795: "CardTrait",
    3970: "CardTraitCollection",
    4115: "TraitEffect",
    4371: "CardTraitData",
    231: "CardTraitOath",
    1586: "CardTraitDuty",
    3039: "CardTraitFerocity",
    3671: "BoardTraitIcon",
}

# 目录 / 集合里的 8 个 Sprite 字段（顺序 = 序列化顺序）
SPRITE_FIELDS = (
    "sabotageSprite",
    "faithSprite",
    "ferocitySprite",
    "oathSprite",
    "questPointsSprite",
    "spiritStoneSprite",
    "meleeAttackSprite",
    "rangeAttackSprite",
)

# P3 报告 §4.3 那张表的**独立副本**，只用来和「现场扫包」的结果对账（见文件头「怎么校验」4）。
# 键 = AssetReference GUID，值 = CardAnim 的 m_Name。
# ⚠️ 它**不是**数据来源 —— 数据来源永远是现场扫包；这里只是第二条独立路径。
EXPECTED_GUID_MAP = {
    "4ae02eb9e870e964db52a85ed290696a": "Ambush Effect",
    "a4088dc3e336f124e9f555510b71d9c6": "ArmourTraitStart",
    "13a7b7da1fdb9404fbe5ccbc57514c6e": "ArtificeTraitTrigger",
    "42d0bc7d68e0230499c668e9aab43e59": "BacklashTraitTrigger",
    "ddaaf29c84bb8f842b8422885348e0fd": "BlindTraitPlay",
    "874dd68077c21cc4c86c5362b436d792": "BloodThirstGainAnim",
    "5a60dbe6e84c80742bf4f9c74b0502ed": "BloodThirstIdleAnim",
    "2b63fd560f1162e468349038c37ad642": "BloodThirstLoseAnim",
    "04b0c6e3f3eeb4b409368ee67804bcb5": "BluntImpact_Stomp_Down",
    "f91c06ce089539140a767ae1c30f08fb": "BluntImpact_Stomp_Forward",
    "cd3972f02c5f6c945b70ac4e34e5c1e7": "CamouflageTraitPlay",
    "31da34149fe75bb44a5acb577b0b0187": "CantAttackAnim",
    "9869f9d036ce2854ab95004fec1a7598": "CodexTraitTrigger",
    "3fce8ddee18e6944d9fdeef1b2199bbd": "CompanionTraitStart",
    "20e1db36c752eb5409e67119f4433f1a": "ConcussiveTraitTrigger",
    "9c0734f33ace0db44acabaf75e11f588": "CrueltyTraitTrigger",
    "a8c3f891601e9ca4c9e3b7c8cd0f37a3": "DutyAnim",
    "42abf1e9d0e87d64b8e8e91ba150ad9b": "EcstasyTraitTrigger",
    "cc61d807ec8234b3082ec764f07b4808": "Eldar_Shuriken_Short_Trait",
    "6144c1d442cbb964a85e6501399f07f2": "FastTraitStart",
    "fa13471fa8de4d74e83822e2bd49f794": "Ferocity Recall to deck",
    "0e5d56145ea269844837cb0bedeb24eb": "FerocityAnim",
    "8c379c5db12284b4b8d7353fa7f00006": "FlankTraitStart",
    "a81f347069299044481251a67e683037": "FlyingTraitPlay",
    "6a3ebaf28b9d7c74d9377de9ff5588fd": "FrenziedTraitTrigger",
    "e8dd2159956272b488aefe1901a216d6": "HuntMark Damage Anim",
    "8569ddf9cb908cd43a21b7d6d7f082c2": "HuntMark End Anim",
    "b664ea5d6484f83438a29fce3afdb989": "HuntMark Idle Anim",
    "95778df2378ded94da0aafc793216540": "HuntMark Stack Anim",
    "0a6f7af3797f19f459318feeabaf7a79": "HuntMark Start Anim",
    "ea9551018eef070429f258bf5da49f19": "InvulnerableBLTraitPlay",
    "841e49ce1bb1ab6409d1b0ca2b10308c": "InvulnerableGSCTraitPlay",
    "08b76498400033843b155abb260f5427": "InvulnerableGuardTraitPlay",
    "51fabe2a07ec80a4f9e60fd88f549f0e": "InvulnerableLeviathanTraitPlay",
    "e8e0e89495f211d4aae68ed1498feecb": "InvulnerableTAUTraitPlay",
    "5d246ebfe1414934cbc3df1e1c940b44": "InvulnerableTraitPlay",
    "8249c79fec4d44c4ca41755045dc863d": "Invulnerable_SW_TraitPlay",
    "64178449148019f43b9980567056dcab": "LongRangeAnim",
    "7e8ca71a32aa8e043af3748e0869c5ea": "LongRangeTraitEnd",
    "abe2e28b387aa9d418346dcae6475ce0": "MarkOfKhorneTraitPlay",
    "e3bd6529302157840aaa1f130fe125c3": "MarkOfNurgleTraitPlay",
    "1918dba5fd658384a91c9ed1610e9f8c": "MarkOfSlaaneshTraitPlay",
    "0e6be8dea74038b4dbbe54115b291aff": "MarkOfTzeenchTraitPlay",
    "e22d23a8ffdb4804eaab9370247c3fea": "MarkerlightTraitEnd",
    "047f05d1fc1185148b0efc43f02514d3": "MarkerlightTraitFromCode",
    "7ed23fc3db385bf4ba9621335bb0f266": "MarkerlightTraitPlay",
    "3bdfa049840f62644b563575bd42623b": "MarkerlightTraitStart",
    "2a21bd1096e20a74ab579989ab5fafff": "MobTraitTrigger",
    "e88772cb08fb73243970183f0f1dff7a": "Oath_Effect",
    "098d35eb98259de479dae6ccace2b777": "Pack OnPlay Anim",
    "58c04a0188df8f7408469d58740ffb27": "PinDownTraitPlay",
    "3a47c27d3826b534a9014c70433e7b5e": "Pray_Idle",
    "96ce731bac5181e4fa9516b352fb493f": "Pray_Start",
    "f21e512ab071d36479a4cbfda6edc6a9": "RallyTraitStart",
    "3f3a5ba316ce0ab46baf23c3a1ea9049": "RegenerationTraitTrigger",
    "d65e4c8cadde4b646a2815741bfdcf71": "RegimentAnim",
    "e162a1563a8acb94793b8a61be2a2bb6": "RelentlessTraitTrigger",
    "50b0ea0f4948557479de1e3db6c3d27e": "Sentry_Effect",
    "bba49b40661864f4c9a214ff1d282abe": "ShieldTraitECPlay",
    "3fde447d811e17743b8adea2ee0fd763": "ShieldTraitNecronPlay",
    "66b7b2b312de4914f8f788ed19f62f3f": "ShieldTraitPlay",
    "29f81690e299042488ae8032665f6f98": "ShieldTraitSororitasPlay",
    "85b4a93db566e5b459c337cd5e04232a": "ShieldTraitTAUPlay",
    "7d2ab14978e66a242bfd8ed3d85b3f1c": "ShurikenTraitFromCode",
    "7dc58e5b008d8804f8fe911400c9bd53": "Slay_Effect_target",
    "254794f776f20c7438ffee13c81bc945": "SniperTraitFromCode",
    "beab83fd5c9157642993c8febd915121": "StealthTraitPlay",
    "06c5534784474f440bfee6dcbf08360c": "StimulateTraitTrigger",
    "3d21ccdce390e1946a75a59167e6ba67": "StrikeTraitTrigger",
    "be723eb2aec4c5241a42fd4ae51a895a": "StunTraitPlay",
    "f387e5894b071f14bb79a0a59c5f87d9": "SwarmTraitFromCode",
    "f587b0eb3d6b66144b0363cd1a1d7e41": "SynapseTraitFromCode",
    "a36c3ce6d808dd84992bcf005a2fff22": "TalentTraitTrigger",
    "50728df9730796d48aedea73047a2f1d": "TeleportCardAnim",
    "5af24b17bf73eff498d5d3807dd2994c": "TideTraitStart",
    "5d7fa4738d00d154795eccdd93365c44": "UnstableTraitEnd",
    "0c1819e921598dd4d80b9fb8c79310a7": "Uprising Effect",
    "45746aadb340e664b87453c64c0518c8": "VanguardIdleAnim",
    "9bb41fe7da6e6614dbdd1c5bd2e31e31": "VulnerableTraitPlay",
}


# ---------------------------------------------------------------- 原始字节读取器

class Reader:
    """按 Unity 序列化规则走一个 bytearray：每字段前按自身对齐补齐（Align4）。"""

    def __init__(self, b):
        self.b = b
        self.p = 0

    def i32(self):
        self.p += (-self.p) % 4
        v = struct.unpack_from("<i", self.b, self.p)[0]
        self.p += 4
        return v

    def i64(self):
        v = struct.unpack_from("<q", self.b, self.p)[0]
        self.p += 8
        return v

    def f32(self):
        self.p += (-self.p) % 4
        v = struct.unpack_from("<f", self.b, self.p)[0]
        self.p += 4
        return v

    def boolean(self):
        # 序列化的 bool 是 1 字节 + 3 字节填充（P3 报告 §三）
        self.p += (-self.p) % 4
        v = self.b[self.p]
        self.p += 4
        return bool(v)

    def string(self):
        n = self.i32()
        if n <= 0:
            return ""
        v = self.b[self.p:self.p + n].decode("utf-8", "replace")
        self.p += n
        self.p += (-self.p) % 4
        return v

    def pptr(self):
        return (self.i32(), self.i64())


def read_monobehaviour_header(b):
    """MonoBehaviour 头 28 B → (m_Script PPtr, m_Name)。"""
    r = Reader(b)
    r.pptr()          # m_GameObject
    r.i32()           # m_Enabled
    script = r.pptr()  # m_Script
    name = r.string()  # m_Name
    return script, name


# ---------------------------------------------------------------- 解析器（布局见 P3 §三）

def parse_collection(raw):
    r = Reader(raw)
    r.p = 28                      # 跳过 MonoBehaviour 头
    name = r.string()
    n = r.i32()
    pairs = []
    for _ in range(n):
        pairs.append({
            "name": r.string(),
            "definedTrait": r.i32(),   # enum DefinedTrait
            "traitConfig": r.pptr(),   # PPtr → CardTrait（fileID=0 = 同文件）
        })
    sprites = {}
    for key in SPRITE_FIELDS:
        sprites[key] = r.pptr()
    if r.p != len(raw):
        raise AssertionError(
            "parse_collection 字节数不匹配: 读到 %d / byte_size %d" % (r.p, len(raw)))
    return {"m_Name": name, "pairs": pairs, "sprites": sprites, "bytesRead": r.p, "byteSize": len(raw)}


def parse_cardtrait(pid, raw):
    r = Reader(raw)
    r.p = 28
    out = {
        "pathID": pid,
        "m_Name": r.string(),
        "traitIcon": r.pptr(),
        "hasInPlayAnim": r.boolean(),   # _hasInPlayAnim
        "hasIcon": r.boolean(),         # _hasIcon
        "isInPlayIcon": r.boolean(),    # _isInPlayIcon
        "stackable": r.i32(),           # _stackable (TraitStackType)
        "aIScore": r.f32(),             # _aIScore
        "customObject": {               # AssetReferenceTyped<Object>
            "guid": r.string(),
            "subObjectName": r.string(),
            "subObjectType": r.string(),
        },
        "traitEffects": [],
    }
    n_effects = r.i32()
    for _ in range(n_effects):
        eff = {
            "name": r.string(),
            "effectAnimGuid": r.string(),      # AssetReferenceTyped<CardAnim>
            "effectAnimSubObjectName": r.string(),
            "effectAnimSubObjectType": r.string(),
            "overrideByArmies": [],
            "triggerType": None,
            "playIfWarlord": None,
        }
        n_ovr = r.i32()
        for _ in range(n_ovr):
            eff["overrideByArmies"].append({
                "army": r.i32(),
                "effectAnimGuid": r.string(),
                "effectAnimSubObjectName": r.string(),
                "effectAnimSubObjectType": r.string(),
            })
        eff["triggerType"] = r.i32()
        eff["playIfWarlord"] = r.boolean()
        out["traitEffects"].append(eff)
    if r.p != len(raw):
        raise AssertionError(
            "parse_cardtrait(%d) 字节数不匹配: 读到 %d / byte_size %d" % (pid, r.p, len(raw)))
    out["bytesRead"] = r.p
    out["byteSize"] = len(raw)
    return out


# ---------------------------------------------------------------- 取字节 / 取名字

def load_raw_monobehaviours(path):
    """把文件里**每个** MonoBehaviour 的原始字节抠出来（绕过没有 type tree 的问题）。"""
    import UnityPy
    env = UnityPy.load(path)
    raws = {}
    for obj in env.objects:
        if obj.type.name != "MonoBehaviour":
            continue
        sf = obj.assets_file
        reader = sf.reader
        pos = reader.Position
        try:
            reader.Position = obj.byte_start
            raws[obj.path_id] = reader.read(obj.byte_size)
        finally:
            reader.Position = pos
    return raws


def load_monoscript_classes(path, wanted):
    """globalgamemanagers.assets 里按 pathID 反查 MonoScript.m_ClassName（现场复核 MONOSCRIPT_IDS）。"""
    import UnityPy
    env = UnityPy.load(path)
    found = {}
    for obj in env.objects:
        if obj.type.name != "MonoScript" or obj.path_id not in wanted:
            continue
        found[obj.path_id] = obj.read().m_ClassName
    return found


def load_object_names(path, wanted_path_ids):
    """按 pathID 取任意类型对象的 m_Name（这里只用来给 Sprite 取名）。"""
    import UnityPy
    env = UnityPy.load(path)
    names = {}
    want = set(wanted_path_ids)
    for obj in env.objects:
        if obj.path_id not in want:
            continue
        try:
            names[obj.path_id] = (obj.type.name, getattr(obj.read(), "m_Name", None))
        except Exception:
            names[obj.path_id] = (obj.type.name, None)
        want.discard(obj.path_id)
        if not want:
            break
    return names


def iter_serialized_files(env):
    """`UnityPy.load()` 的返回值有两层形状：
      · 裸 .assets  → `env.files` = {name: SerializedFile}
      · .bundle     → `env.files` = {name: BundleFile}，里面再一层 `v.files` = {cab: SerializedFile}
    这里统一摊平成 SerializedFile。
    """
    for v in env.files.values():
        inner = getattr(v, "files", None)
        if isinstance(inner, dict):
            for sf in inner.values():
                yield sf
        else:
            yield v


def build_guid_map(aa_dir):
    """扫 15 个 *cardanims* 包：Addressables container 键（GUID） → (bundle 名, CardAnim m_Name)。

    判据：P3 报告 §4.3 的解析法 —— container 的键就是 GUID，值是指向 MonoBehaviour 的 PPtr，
    那个 MonoBehaviour 的 m_Name 就是 CardAnim 的资源名。
    """
    import UnityPy
    guid_map = {}
    bundles = sorted(b for b in os.listdir(aa_dir) if "cardanim" in b)
    # ⚠️ P3 报告 §4.3 写「15 个 *cardanims* 包」，本机实测 = **14 个**
    #    （`find StreamingAssets -iname "*cardanim*"` 全树 14 个，没有第 15 个）。
    #    真正的守卫不是这个计数，而是下面「用到的每个 GUID 都必须换到名字」。
    if len(bundles) != 14:
        raise AssertionError("预期 14 个 *cardanims* 包，实际 %d 个: %s" % (len(bundles), bundles))
    for bn in bundles:
        env = UnityPy.load(os.path.join(aa_dir, bn))
        for sf in iter_serialized_files(env):
            container = getattr(sf, "container", None) or {}
            if not container:
                continue
            objs = sf.objects.values() if isinstance(sf.objects, dict) else sf.objects
            names = {}
            for o in objs:
                if o.type.name != "MonoBehaviour":
                    continue
                try:
                    names[o.path_id] = o.read().m_Name
                except Exception:
                    pass
            for key, pptr in container.items():
                pid = getattr(pptr, "path_id", None)
                if pid is None and isinstance(pptr, (tuple, list)):
                    pid = pptr[1]
                guid_map[key] = (bn, names.get(pid))
    return guid_map, bundles


# ---------------------------------------------------------------- 组装

def md5_of(path):
    h = hashlib.md5()
    with io.open(path, "rb") as f:
        while True:
            chunk = f.read(1 << 20)
            if not chunk:
                break
            h.update(chunk)
    return h.hexdigest()


def resolve_anim(guid, guid_map, where):
    """GUID → CardAnim 名。换不到 = 硬失败（铁律：不许静默失败 / 不许把查不到写成猜测）。"""
    if not guid:
        return None
    hit = guid_map.get(guid)
    if not hit or not hit[1]:
        raise AssertionError("%s: effectAnim GUID %s 在 15 个 cardanims 包里换不到名字" % (where, guid))
    return hit[1]


def build_payload():
    # --- 0. 现场复核 MonoScript 表 -------------------------------------------------
    classes = load_monoscript_classes(SRC_MONOSCRIPTS, set(MONOSCRIPT_IDS))
    for pid, expect in MONOSCRIPT_IDS.items():
        got = classes.get(pid)
        if got != expect:
            raise AssertionError(
                "MonoScript pathID %d 期望 %r，实际 %r（globalgamemanagers.assets 与 P3 报告不符）"
                % (pid, expect, got))

    # --- 1. 抠原始字节 + 按 m_Script 分类 ------------------------------------------
    raws = load_raw_monobehaviours(SRC_ASSETS)
    collection_pids, trait_pids = [], []
    for pid in sorted(raws):
        script, _name = read_monobehaviour_header(raws[pid])
        cls = MONOSCRIPT_IDS.get(script[1])
        if cls == "CardTraitCollection":
            collection_pids.append(pid)
        elif cls in ("CardTrait", "CardTraitOath", "CardTraitDuty", "CardTraitFerocity"):
            trait_pids.append(pid)

    if len(collection_pids) != 1:
        raise AssertionError("预期 1 个 CardTraitCollection，实际 %d 个: %s" % (len(collection_pids), collection_pids))
    if collection_pids[0] != 531:
        raise AssertionError("CardTraitCollection 的 pathID 变了：%d（P3 报告是 531）" % collection_pids[0])
    if len(trait_pids) != 73:
        raise AssertionError("预期 73 个 CardTrait 派生对象，实际 %d 个: %s" % (len(trait_pids), trait_pids))

    collection = parse_collection(raws[collection_pids[0]])
    if len(collection["pairs"]) != 77:
        raise AssertionError("预期 77 条 TraitPairs，实际 %d 条" % len(collection["pairs"]))

    traits = {}
    for pid in trait_pids:
        traits[pid] = parse_cardtrait(pid, raws[pid])

    # --- 2. GUID → CardAnim 名（现场扫包） + 与报告 §4.3 对账 ------------------------
    guid_map, bundles = build_guid_map(AA_DIR)
    used_guids = set()
    for t in traits.values():
        for eff in t["traitEffects"]:
            if eff["effectAnimGuid"]:
                used_guids.add(eff["effectAnimGuid"])
            for ovr in eff["overrideByArmies"]:
                if ovr["effectAnimGuid"]:
                    used_guids.add(ovr["effectAnimGuid"])

    missing_expected = used_guids - set(EXPECTED_GUID_MAP)
    mismatched = {}
    for g in sorted(used_guids & set(EXPECTED_GUID_MAP)):
        derived = guid_map.get(g, (None, None))[1]
        if derived != EXPECTED_GUID_MAP[g]:
            mismatched[g] = (EXPECTED_GUID_MAP[g], derived)
    if missing_expected or mismatched:
        raise AssertionError(
            "GUID 表与 P3 报告 §4.3 不一致：报告里没有的 %s ；名字不符的 %s"
            % (sorted(missing_expected), mismatched))

    # --- 3. Sprite 名字 ------------------------------------------------------------
    sprite_pids = set()
    for t in traits.values():
        if t["traitIcon"][1]:
            sprite_pids.add(t["traitIcon"][1])
    for key in SPRITE_FIELDS:
        if collection["sprites"][key][1]:
            sprite_pids.add(collection["sprites"][key][1])
    sprite_names = load_object_names(SRC_ASSETS, sorted(sprite_pids))
    bad_type = {p: v[0] for p, v in sprite_names.items() if v[0] != "Sprite"}
    if bad_type:
        raise AssertionError("这些 PPtr 以为指向 Sprite，实际不是: %s" % bad_type)

    # --- 4. 组装 ------------------------------------------------------------------
    out_traits = []
    unresolved_custom = set()
    for pair in collection["pairs"]:
        cfg = pair["traitConfig"]
        entry = {
            "name": pair["name"],
            "definedTrait": pair["definedTrait"],
            "definedTraitName": DEFINED_TRAIT_NAMES.get(pair["definedTrait"]),
            "traitConfig": None if not cfg[1] else {"fileID": cfg[0], "pathID": cfg[1]},
            "pathID": cfg[1] if cfg[1] else None,
            "m_Name": None,
            "traitIcon": None,
            "stackable": None,
            "stackableName": None,
            "aIScore": None,
            "inPlayAnim": None,
            "hasIcon": None,
            "isInPlayIcon": None,
            "customObject": None,
            "traitEffects": None,
        }
        if cfg[1]:
            if cfg[1] not in traits:
                raise AssertionError("TraitPairs %r 指向 %d，但那个对象不是 CardTrait 派生" % (pair["name"], cfg[1]))
            t = traits[cfg[1]]
            icon_pid = t["traitIcon"][1]
            # `customObject` 是「多用途对象」（CardTrait.cs 里那个 Tooltip 写的）。
            # 它的 GUID **不是** effectAnim，但其中一部分同样落在 cardanim 包里 —— 能换到名就写上，
            # 换不到写 null 并在收尾时**点名列出**（⛔ 不静默）。判据见报告 §4.3 + P7 报告。
            co = dict(t["customObject"])
            co["name"] = None
            if co["guid"]:
                hit = guid_map.get(co["guid"])
                if hit and hit[1]:
                    co["name"] = hit[1]
                else:
                    unresolved_custom.add("%s:%s" % (t["m_Name"], co["guid"]))
            entry.update({
                "m_Name": t["m_Name"],
                "traitIcon": sprite_names.get(icon_pid, (None, None))[1] if icon_pid else None,
                "traitIconPathID": icon_pid if icon_pid else None,
                "stackable": t["stackable"],
                "stackableName": STACKABLE_NAMES.get(t["stackable"]),
                "aIScore": t["aIScore"],
                "inPlayAnim": t["hasInPlayAnim"],
                "hasIcon": t["hasIcon"],
                "isInPlayIcon": t["isInPlayIcon"],
                "customObject": co,
                "traitEffects": [],
            })
            if t["stackable"] not in STACKABLE_NAMES:
                raise AssertionError("未知 TraitStackType 值 %d（trait %r）" % (t["stackable"], t["m_Name"]))
            for eff in t["traitEffects"]:
                e = {
                    "name": eff["name"],
                    "triggerType": eff["triggerType"],
                    "triggerName": TRIGGER_NAMES.get(eff["triggerType"]),
                    "playIfWarlord": eff["playIfWarlord"],
                    "effectAnimGuid": eff["effectAnimGuid"],
                    "effectAnim": resolve_anim(eff["effectAnimGuid"], guid_map,
                                               "%s/%s" % (t["m_Name"], eff["name"])),
                    "overrideByArmies": [],
                }
                if e["triggerName"] is None:
                    raise AssertionError("未知 EffectTriggerType 值 %d" % eff["triggerType"])
                for ovr in eff["overrideByArmies"]:
                    army = ovr["army"]
                    e["overrideByArmies"].append({
                        "army": army,
                        "armyName": ARMY_NAMES.get(army),
                        "effectAnimGuid": ovr["effectAnimGuid"],
                        "effectAnim": resolve_anim(ovr["effectAnimGuid"], guid_map,
                                                   "%s/%s/ovr(%d)" % (t["m_Name"], eff["name"], army)),
                    })
                    if e["overrideByArmies"][-1]["armyName"] is None:
                        raise AssertionError("未知 CardArmy 值 %d" % army)
                entry["traitEffects"].append(e)
        if entry["definedTraitName"] is None:
            raise AssertionError("DefinedTrait 值 %d（pair %r）在 DefinedTrait.cs 的枚举里找不到"
                                 % (pair["definedTrait"], pair["name"]))
        out_traits.append(entry)

    sprite_ids = {}
    for key in SPRITE_FIELDS:
        pid = collection["sprites"][key][1]
        sprite_ids[key] = {
            "pathID": pid,
            "name": sprite_names.get(pid, (None, None))[1] if pid else None,
        }

    # 交叉校验：集合里存的 `TraitPairs.name` 字符串，应当等于同一个 int 在
    # `DefinedTrait.cs` 里的枚举名。两条来源完全独立（二进制里的 string vs 反编译枚举），
    # 现在 77/77 一致 —— 留成断言，将来哪一边变了会当场炸。
    mismatch = [(t["name"], t["definedTrait"], t["definedTraitName"])
                for t in out_traits if t["name"] != t["definedTraitName"]]
    if mismatch:
        raise AssertionError("TraitPairs.name 与 DefinedTrait 枚举名不一致: %s" % mismatch)

    return {
        "source": {
            "assetsFile": SRC_ASSETS,
            "assetsFileMd5": md5_of(SRC_ASSETS),
            "assetsFileSize": os.path.getsize(SRC_ASSETS),
            "monoScriptsFile": SRC_MONOSCRIPTS,
            "animBundlesDir": AA_DIR,
            "animBundleCount": len(bundles),
            "monoScriptPathIds": {str(k): v for k, v in sorted(MONOSCRIPT_IDS.items())},
        },
        "generated": GENERATED_DATE,
        "collection": {
            "pathID": collection_pids[0],
            "m_Name": collection["m_Name"],
            "traitPairCount": len(collection["pairs"]),
            "cardTraitObjectCount": len(traits),
            "nullTraitConfigPairs": [p["name"] for p in collection["pairs"] if not p["traitConfig"][1]],
            "unresolvedCustomObjectGuids": sorted(unresolved_custom),
            "bytesRead": collection["bytesRead"],
            "byteSize": collection["byteSize"],
        },
        "traits": out_traits,
        "spriteIds": sprite_ids,
    }


# ---------------------------------------------------------------- 落盘

def render_json(payload):
    """算成字符串（⛔ 先算完再写 —— `wb().write(<表达式>)` 会先截断文件）。"""
    text = json.dumps(payload, ensure_ascii=False, indent=2)
    return text + "\n"


def check_line_endings(path):
    """二进制读回数行尾（这个工程对 CRLF/LF 极敏感）。"""
    with io.open(path, "rb") as f:
        data = f.read()
    crlf = data.count(b"\r\n")
    lf = data.count(b"\n")
    return crlf, lf, len(data)


def main(argv=None):
    ap = argparse.ArgumentParser(
        description="导出原版 CardTraitCollection / CardTrait SO → trait_collection.json")
    ap.add_argument("--check", action="store_true",
                    help="只校验（解析 + 断言 + 与盘上那份逐字节比），不写文件")
    ap.add_argument("--out", default=OUT_DEFAULT, help="输出路径（默认 %s）" % OUT_DEFAULT)
    args = ap.parse_args(argv)

    payload = build_payload()
    text = render_json(payload)
    blob = text.encode("utf-8")

    # --- 自检读数 ---
    n_traits = len(payload["traits"])
    n_null = len(payload["collection"]["nullTraitConfigPairs"])
    n_eff = sum(len(t["traitEffects"] or []) for t in payload["traits"])
    n_ovr = sum(len(e["overrideByArmies"]) for t in payload["traits"] for e in (t["traitEffects"] or []))
    print("[parse] collection pathID=%d m_Name=%r pairs=%d objects=%d (null traitConfig: %s)"
          % (payload["collection"]["pathID"], payload["collection"]["m_Name"],
             payload["collection"]["traitPairCount"],
             payload["collection"]["cardTraitObjectCount"],
             ", ".join(payload["collection"]["nullTraitConfigPairs"])))
    print("[parse] bytesRead == byte_size: %d/%d CardTrait 对象全部通过"
          % (payload["collection"]["cardTraitObjectCount"], payload["collection"]["cardTraitObjectCount"]))
    print("[json ] traits(77 条 TraitPairs 展开)=%d  traitEffects=%d  overrideByArmies=%d  sprites=%d"
          % (n_traits, n_eff, n_ovr, len(payload["spriteIds"])))
    print("[json ] 字节数=%d  md5=%s" % (len(blob), hashlib.md5(blob).hexdigest()))
    customs = [t["customObject"] for t in payload["traits"] if t["customObject"] and t["customObject"]["guid"]]
    unres = payload["collection"]["unresolvedCustomObjectGuids"]
    print("[json ] customObject GUID：%d 条非空，%d 条换到 CardAnim 名，%d 条换不到（%s）"
          % (len(customs), sum(1 for c in customs if c["name"]), len(unres), ", ".join(unres) or "-"))

    if n_traits != 77 or n_null != 2:
        raise AssertionError("条数不符：traits=%d（应 77） null traitConfig=%d（应 2）" % (n_traits, n_null))

    if args.check:
        if not os.path.exists(args.out):
            print("[check] ✗ 盘上没有 %s" % args.out)
            return 1
        with io.open(args.out, "rb") as f:
            on_disk = f.read()
        if on_disk == blob:
            print("[check] ✓ 与盘上逐字节相同 (%d B, md5 %s)"
                  % (len(on_disk), hashlib.md5(on_disk).hexdigest()))
            return 0
        print("[check] ✗ 与盘上不同：盘上 %d B (md5 %s) vs 算出 %d B (md5 %s)"
              % (len(on_disk), hashlib.md5(on_disk).hexdigest(), len(blob), hashlib.md5(blob).hexdigest()))
        return 1

    out_dir = os.path.dirname(args.out)
    if out_dir and not os.path.isdir(out_dir):
        os.makedirs(out_dir)
    with io.open(args.out, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)

    crlf, lf, size = check_line_endings(args.out)
    if crlf != 0 or size != len(blob):
        raise AssertionError("写出的文件行尾/大小不对: CRLF=%d LF=%d size=%d（期望 CRLF=0 LF=%d size=%d）"
                             % (crlf, lf, size, blob.count(b"\n"), len(blob)))
    print("[write] %s  (%d B, LF=%d, CRLF=0)" % (args.out, size, lf))
    return 0


if __name__ == "__main__":
    sys.exit(main())
