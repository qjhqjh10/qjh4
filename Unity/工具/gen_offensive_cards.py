#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_offensive_cards.py — 「进攻卡（= 环境效果卡）数据表」生成器

用法（本项目惯例）：
    PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_offensive_cards.py

只读（绝不写 d:/2/）：
  ① d:/4/Unity/素材/Warpforge原版/游戏数据/去重定义/MonoBehaviour/EnvironmentalEffectCardsSO.json
     （= d:/2/新解包资源/assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/ 那份；
       ⚠️ 资产名比类名少一个 n）。唯一字段 enviromentalEffects[]，每阵营一条：
       {army, emptyOffensive, defaultEnviromentalEffectVFX, offensiveCards[3], defensiveCards[3]}；
       offensiveCards[i] = {offensiveCard(PPtr), forgeLevel, enviromentalEffectVFX(GUID)}
       —— **这就是「卡 → 特效 SO」的唯一绑定**。
  ② d:/2/新解包资源/assets_full/bundle_*cardanims*_assets_all/
       · MonoBehaviour/*.json —— 55 个 ScenarioEnvironmentConditionSO（13 Default + 42 效果）。
         识别靠**字段签名**（blendTime + scenarioObjects + ambientColor），不靠文件名/类名
         （`m_Script` 只是 PathID，反解不出类名）。
       · AssetBundle/AssetBundle_*.json 的 `m_Container` —— **Addressables 地址表**：
         键 = 资产 GUID，值 = {asset:{m_PathID}}。这就是 GUID → 包内对象的那一跳。
         ⚠️ **帝皇之子那两个包例外**：容器的键被**截断**成 1~3 个十六进制字符
         （`…_cardanims` 52 条：'0' '0c' '1' … 'eb4'；`…_cardassets` 278 条），
         不能直接按 GUID join —— 走 ③ 的「第二键」。
  ③ d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/catalog.bin
     （Addressables 目录，本地就有，两份游戏副本 md5 相同）。
     逐条 = [uint32 键长][键字符串]（可再有 [uint32 键2长][键2]）+ 28 字节记录；
     记录头两个 uint32 = **本 location 两把键的字符串偏移**（实测 11672/11804 条满足
     `记录[0] == 自己键的偏移`）。对**全 GUID 键**的包，键2 == 键1（同一字符串去重 ⇒ 偏移相同）；
     对**帝皇之子**那两个包，键2 才是包里真正用的短键 ⇒ **GUID → 短键 → m_Container → m_PathID**。
  ④ d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/
       · GameObject/*.json —— 43 个 `Environmental Condition*` prefab（`scenarioObjects` 指到这儿）。
       · AssetBundle/*.json —— 同上，GUID → m_PathID。
  ⑤ 两份 UnityPy 读原始 .bundle（`d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/
     StandaloneWindows64/*.bundle`）—— **只用来把 m_PathID 反解成对象名**。
     解包 JSON 里**没有对象自己的 path_id**（只在 AssetBundle 容器里出现），所以这一跳必须重读包；
     只 read() 需要的那些 path_id（battleprefabs 有 6.4 万个对象，全读很慢）。
  ⑥ d:/2/新解包资源/assets_full/bundle_*cardassets*_assets_all/Texture2D/ —— 卡面贴图（52 张，13×4）。

输出（只写这一个文件）：
    d:/4/Unity/数据/游戏数据/offensive_cards.json

几条**读不出来的**（照实写 null，不猜；判据与出处见 json 里的 note）：
  · `offensiveCard` / `emptyOffensive` 是 PPtr(m_FileID=4, m_PathID=…) —— **m_FileID=4 是外部文件**，
    解包 JSON 没有 externals 表，全库也搜不到 `*_<path_id>.json` ⇒ **卡名解不出**。
    候选名改从「`enviromentalEffectVFX` → SO 名 → 贴图名」反推，标 `cardName_candidate` + `cardName_candidate_src`。
  · `prefab_name`：SO 的 `scenarioObjects.m_AssetGUID` → battleprefabs 容器 → 名字。
    **UM `Fleet Support` 那条 GUID 不在容器里**（悬空引用）⇒ 那一条 null。

幂等：不留时间戳，重跑逐字节相同（只要解包树没变）。跑不动/对不上会直接抛错，不静默。
"""

import io
import json
import os
import re
import struct
import sys

# ------------------------------------------------------------------ 路径常量

ASSETS_FULL = r"d:/2/新解包资源/assets_full"
AA_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa"
BUNDLE_SRC = os.path.join(AA_DIR, "StandaloneWindows64")
CATALOG = os.path.join(AA_DIR, "catalog.bin")

SO_JSON_PRIMARY = (r"d:/4/Unity/素材/Warpforge原版/游戏数据/去重定义/MonoBehaviour/"
                   r"EnvironmentalEffectCardsSO.json")
SO_JSON_ASSETS_FULL = os.path.join(
    ASSETS_FULL, "bundle_duplicateassetisolationso_assets_all", "MonoBehaviour",
    "EnvironmentalEffectCardsSO.json")

OUT_JSON = r"d:/4/Unity/数据/游戏数据/offensive_cards.json"

# 阵营 id → 我们工程里的阵营名（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs`
# 逐条照抄：Ultramarines=10 Goff=20 SaimHann=30 Sautekh=40 BlackLegion=50 Leviathan=60
# TauEmpire=70 Sororitas=80 Genestealers=90 AstraMilitarum=100 DarkAngels=110
# EmperorsChildren=120 SpaceWolves=130）
ARMY_NAMES = {
    10: "Ultramarines", 20: "Goff", 30: "SaimHann", 40: "Sautekh",
    50: "BlackLegion", 60: "Leviathan", 70: "TauEmpire", 80: "Sororitas",
    90: "Genestealers", 100: "AstraMilitarum", 110: "DarkAngels",
    120: "EmperorsChildren", 130: "SpaceWolves",
}

# 每阵营的 4 张卡面贴图（`bundle_<this>cardassets_assets_all/Texture2D/`）
# ⚠️ **命名不统一、不能按 `offensive` 前缀扫**：Dark Angels / Sororitas 混在 `_strat_` 下、
#    Tau 有一张拼 `offence`、Space Wolves 用 `Default Conditions`、EC 的 Normal 在 `_strat_` 下、
#    Sororitas 目录里还有个不相干的 `Sororitas_strat_Shrineworld` ⇒ **逐个写死**，
#    运行时断言文件真的在（解包树变了要报错，别静默给空）。
# 出处：`资料/加时与冲突模式_原版规格.md:289-303` 那张表 + 本次逐目录核对。
CARD_TEXTURES = {
    "Ultramarines": "spacemarinesultramarinescardassets",
    "Goff": "orksgoffcardassets",
    "SaimHann": "aeldarisaimhanncardassets",
    "Sautekh": "necronssautekhcardassets",
    "BlackLegion": "chaosspacemarinesblacklegioncardassets",
    "Leviathan": "tyranidsleviathancardassets",
    "TauEmpire": "tauempirecardassets",
    "Sororitas": "sororitascardassets",
    "Genestealers": "genestealercultscardassets",
    "AstraMilitarum": "astramilitarumcardassets",
    "DarkAngels": "spacemarinesdarkangelscardassets",
    "EmperorsChildren": "chaosspacemarinesemperorschildrencardassets",
    "SpaceWolves": "spacemarinesspacewolvescardassets",
}
TEXTURE_FILES = {
    "SaimHann": ["Craftworld_SaimHann_offensive_Blackout",
                 "Craftworld_SaimHann_offensive_Infinity Circuit Overload",
                 "Craftworld_SaimHann_offensive_Webway Rift",
                 "Craftworld_SaimHann_offensive_Normal Conditions"],
    "AstraMilitarum": ["AstraMilitarum_offensive_Dawn Attack",
                       "AstraMilitarum_offensive_Factories Overdrive",
                       "AstraMilitarum_offensive_Planetary invasion",
                       "AstraMilitarum_offensive_Normal"],
    "BlackLegion": ["CSM_BlackLegion_offensive_Daemonic Feast",
                    "CSM_BlackLegion_offensive_Helfire Outburst",
                    "CSM_BlackLegion_offensive_Warp Storm",
                    "CSM_BlackLegion_offensive_Normal Conditions"],
    "DarkAngels": ["DarkAngels_strat_Asteroid Zone", "DarkAngels_strat_Star Orbiting",
                   "DarkAngels_strat_Void Combat", "DarkAngels_strat_Normal Conditions"],
    "EmperorsChildren": ["CSM_EmperorsChildren_offensive_Aural Hijack",
                         "CSM_EmperorsChildren_offensive_Empyric Rift",
                         "CSM_EmperorsChildren_offensive_Stimm-Vents Leak",
                         "CSM_EmperorsChildren_strat_Normal"],
    "Genestealers": ["GenestealerCults_offensive_Mining Tremors",
                     "GenestealerCults_offensive_Sump Overspill",
                     "GenestealerCults_offensive_Toxic Fumes",
                     "GenestealerCults_offensive_Normal Conditions"],
    "Sautekh": ["Necron_Sautekh_offensive_Earthquake", "Necron_Sautekh_offensive_Immortal Beams",
                "Necron_Sautekh_offensive_Solar Storm", "Necron_Sautekh_offensive_Normal Conditions"],
    "Goff": ["Ork_Goff_offensive_Dust Storm", "Ork_Goff_offensive_Night Attack",
             "Ork_Goff_offensive_Spore Cloud", "Ork_Goff_offensive_Normal Conditions"],
    "Sororitas": ["Sororitas_strat_Disrupted Sanctuary", "Sororitas_strat_Raging Storm",
                  "Sororitas_strat_Shrine Bombardement", "Sororitas_strat_Normal"],
    "SpaceWolves": ["SM_SpaceWolves_offensive_Everstorm", "SM_SpaceWolves_offensive_First Light",
                    "SM_SpaceWolves_offensive_Full moon",
                    "SM_SpaceWolves_offensive_Default Conditions"],
    "TauEmpire": ["Tau_Empire_offensive_Electro-Static Interference",
                  "Tau_Empire_offensive_Radiation Storm", "Tau_Empire_offensive_Solar Eclipse",
                  "Tau_Empire_offence_Normal Conditions"],   # ← 这张拼的是 offence
    "Leviathan": ["Tyranid_Leviathan_offensive_Acid Rain",
                  "Tyranid_Leviathan_offensive_Blazing Biomass",
                  "Tyranid_Leviathan_offensive_Sweeping Infestation",
                  "Tyranid_Leviathan_offensive_Normal Conditions"],
    "Ultramarines": ["SM_UM_offensive_Fleet Support", "SM_UM_offensive_Orbital Bombardment",
                     "SM_UM_offensive_Thunderstorm", "SM_UM_offensive_Normal Conditions"],
}
# 「不使用进攻卡」那张的**卡面名**（口径见 `资料/加时与冲突模式_原版规格.md:306`：
# 卡名恒为 `Normal Conditions`，只有 Space Wolves 叫 `Default Conditions`；
# Astra Militarum 只是文件名里省成了 `Normal`）
NORMAL_FACE = {a: "Normal Conditions" for a in TEXTURE_FILES}
NORMAL_FACE["SpaceWolves"] = "Default Conditions"

# SO 名里的效果词 ↔ 贴图名：**12/13 阵营逐一对得上**，下面 8 处对不上（原版自己就这样），
# 出处 `资料/加时与冲突模式_原版规格.md:306`、`:311-313`。
SO2TEX = {
    ("AstraMilitarum", "Factory Overdrive"): "Factories Overdrive",
    ("DarkAngels", "Orbiting"): "Star Orbiting",
    ("Goff", "Night"): "Night Attack",
    ("Goff", "Spore Clouds"): "Spore Cloud",
    ("Leviathan", "Blazing Biomatter"): "Blazing Biomass",
    ("Sororitas", "Disrupted Ceremony"): "Disrupted Sanctuary",
    ("Sororitas", "Shrine Bombardment"): "Shrine Bombardement",
    ("Ultramarines", "Aerial Clash"): "Fleet Support",
}
# **同图异名**：解包贴图名 ≠ 卡面印的名字（照图核出来的，出处同上）：
#   贴图 → 卡面
TEX_FACE_NAME = {
    "Necron_Sautekh_offensive_Immortal Beams": "Ethereal Energy",
    "SM_UM_offensive_Fleet Support": "Aerial Clash",
    "AstraMilitarum_offensive_Factories Overdrive": "Manufactorum Overdrive",
    "DarkAngels_strat_Star Orbiting": "Star Orbit",
}
# 每阵营 4 张贴图里，**哪张是「不使用进攻卡」那张**（= 文件名里带 Normal / Default Conditions）
NORMAL_TEXTURE = {a: f[-1] for a, f in TEXTURE_FILES.items()}
# SO 名前缀（剥掉之后剩下的就是效果词）
SO_PREFIX = {
    "SaimHann": "EnvironmentalCondition Saim Hann ",
    "AstraMilitarum": "EnvironmentalCondition Astra Militarum ",
    "BlackLegion": "EnvironmentalCondition Black Legion ",
    "DarkAngels": "Environmental Condition Dark Angels ",
    "EmperorsChildren": "Environmental Condition Emperor's Children ",
    "Genestealers": "EnvironmentalCondition GSC ",
    "Sautekh": "EnvironmentalCondition Necrons ",
    "Goff": "EnvironmentalCondition Orks ",
    "Sororitas": "EnvironmentalCondition Sororitas ",
    "SpaceWolves": "Environmental Condition Space Wolves ",
    "TauEmpire": "EnvironmentalCondition Tau ",
    "Leviathan": "EnvironmentalCondition Leviathan ",
    "Ultramarines": "EnvironmentalCondition Ultramarines ",
}

HUGE = 10 ** 12                     # m_PathID 的 64 位范围：超过它才当「真 path_id」
SO_SIGNATURE = ("blendTime", "scenarioObjects")


def norm(s):
    """归一化：小写 + 去掉非字母数字 —— 用来比 SO 名与贴图名。"""
    return re.sub(r"[^a-z0-9]", "", (s or "").lower())


# ------------------------------------------------------------- Addressables 目录

def parse_catalog(path):
    """catalog.bin → {guid: 第二把键}（没有第二键就是 [None]）。

    结构见文件头 ③。**只认 `记录[0] == 自己键的偏移` 的条目**（其余是别的段/别的布局，
    混进来会张冠李戴 —— 实测 11804 条里 11672 条满足）。
    """
    data = open(path, "rb").read()

    def s_at(off):
        if off < 4 or off + 1 > len(data):
            return None
        n = struct.unpack_from("<I", data, off - 4)[0]
        if 1 <= n <= 120 and off + n <= len(data) and all(0x20 <= b < 0x7F for b in data[off:off + n]):
            return data[off:off + n].decode("ascii")
        return None

    out = {}
    for m in re.finditer(rb"[0-9a-f]{32}", data):
        start = m.start()
        if data[start - 4:start] != b"\x20\x00\x00\x00":        # 键长必须是 32
            continue
        p = m.end()
        n = struct.unpack_from("<I", data, p)[0]
        if 1 <= n <= 120 and p + 4 + n <= len(data) and \
                all(0x20 <= b < 0x7F for b in data[p + 4:p + 4 + n]):
            p += 4 + n                                          # 键2 是**内联**的（少数条目）
        first, second = struct.unpack_from("<II", data, p)
        if first != start:                                      # 不是「本 location 两把键」
            continue
        out[m.group().decode("ascii")] = s_at(second)
    return out


# ------------------------------------------------------------------- 包与资产

def bundle_dir(bundle_name):
    return os.path.join(ASSETS_FULL, "bundle_" + bundle_name + "_assets_all")


def load_container(bundle_name):
    """<bundle>/AssetBundle/AssetBundle_*.json 的 m_Container → {键: m_PathID}"""
    d = os.path.join(bundle_dir(bundle_name), "AssetBundle")
    js = [f for f in sorted(os.listdir(d)) if f.endswith(".json")]
    assert len(js) == 1, (bundle_name, js)
    cont = json.load(io.open(os.path.join(d, js[0]), encoding="utf-8"))["m_Container"]
    return {k: v["asset"]["m_PathID"] for k, v in cont}


def name_index(bundle_name, want_pids):
    """m_PathID → (对象名, 类型名)（UnityPy 重读原始 .bundle，只 read 需要的那些）。

    `bundle_name` 是**去掉了 `bundle_` 前缀与 `_assets_all` 后缀**的短名
    （= `<bundle>/AssetBundle/*.json` 里 `m_Container` 那套），原始文件名要拼回去。
    """
    import UnityPy
    src = os.path.join(BUNDLE_SRC, bundle_name + "_assets_all.bundle")
    assert os.path.isfile(src), src
    env = UnityPy.load(src)
    objs = {}
    for o in env.objects:
        objs[o.path_id] = o
    out = {}
    for pid in want_pids:
        for cand in (pid, -pid):
            o = objs.get(cand)
            if o is None:
                continue
            try:
                d = o.read()
                nm = getattr(d, "m_Name", None)
            except Exception:
                nm = None
            if nm:
                out[cand] = (nm, o.type.name)
                break
    return out


def collect_sos():
    """13 个 cardanims 包里全部 ScenarioEnvironmentConditionSO（按字段签名认，不按名字）。

    返回 {m_Name: (包短名, json)}；`m_Name` 与去 `.json` 的文件名一致（断言住）。
    """
    import glob
    out = {}
    for d in sorted(glob.glob(os.path.join(ASSETS_FULL, "bundle_*cardanims*_assets_all"))):
        bn = os.path.basename(d)[len("bundle_"):-len("_assets_all")]
        for f in sorted(glob.glob(os.path.join(d, "MonoBehaviour", "*.json"))):
            j = json.load(io.open(f, encoding="utf-8"))
            if all(k in j for k in SO_SIGNATURE):
                base = os.path.basename(f)[:-len(".json")]
                assert j["m_Name"] == base, (base, j["m_Name"])
                out[j["m_Name"]] = (bn, j)
    return out


# ------------------------------------------------------------------------- 主

def main():
    # ---- ① 源头 SO
    src = json.load(io.open(SO_JSON_PRIMARY, encoding="utf-8"))
    if os.path.isfile(SO_JSON_ASSETS_FULL):
        twin = json.load(io.open(SO_JSON_ASSETS_FULL, encoding="utf-8"))
        assert twin == src, "两份 EnvironmentalEffectCardsSO.json 不一致"
    entries = src["enviromentalEffects"]
    assert len(entries) == 13, len(entries)
    for e in entries:                       # `m_FileID` 恒 4 = 外部文件（卡数据不在本地）
        assert e["emptyOffensive"]["m_FileID"] == 4, e["army"]
        for c in e["offensiveCards"]:
            assert c["offensiveCard"]["m_FileID"] == 4, e["army"]

    # ---- ② 55 个环境 SO + 它们的 scenarioObjects
    sos = collect_sos()
    assert len(sos) == 55, len(sos)

    # ---- ③ 容器（13 个 cardanims + battleprefabs）
    containers = {}
    for bn in sorted({v[0] for v in sos.values()}):
        containers[bn] = load_container(bn)
    PB = "battleprefabs_vfxandmisc"
    containers[PB] = load_container(PB)

    # ---- ④ 键 → (包, pathId) 索引
    key2loc = {}
    for bn, cont in containers.items():
        for k, pid in cont.items():
            key2loc.setdefault(k, []).append((bn, pid))
    cat = parse_catalog(CATALOG)          # GUID → 第二键（只有帝皇之子那两个包真用得上）

    plan = []                             # (阵营名, default|card, 槽序号, GUID)
    for e in entries:
        army = ARMY_NAMES[e["army"]]
        plan.append((army, "default", None, e["defaultEnviromentalEffectVFX"]["m_AssetGUID"]))
        for i, c in enumerate(e["offensiveCards"]):
            plan.append((army, "card", i, c["enviromentalEffectVFX"]["m_AssetGUID"]))

    # ---- ⑤ 读名（两趟：先 SO，再按 SO 的 scenarioObjects 读 prefab）
    names = {}

    def cand_locs(guid):
        """GUID → [(resolvedBy, bundle, pathId, 键)]：先按全 GUID 键直查，再按 catalog 第二键查"""
        out = []
        for how, key in (("container-guid", guid), ("catalog-2nd-key", cat.get(guid))):
            for bn, pid in key2loc.get(key or "", []):
                out.append((how, bn, pid, key))
        return out

    # 第 1 趟：55 条 GUID 落在哪个包哪个 pathId（只要 cardanims 的）
    so_need = {}
    so_loc = {}
    for army, kind, slot, guid in plan:
        ls = [x for x in cand_locs(guid) if "cardanims" in x[1]]
        so_loc[(army, kind, slot)] = ls
        for _, bn, pid, _ in ls:
            so_need.setdefault(bn, set()).add(pid)
    for bn, pids in so_need.items():
        names[bn] = name_index(bn, pids)

    # 第 2 趟：由 SO 名拿到 scenarioObjects → prefab 的 pathId → 名字
    # （battleprefabs 的容器整个读一遍名：43 个 `Environmental*` prefab 全在里面，
    #   读完才能算「哪些没被任何卡引用」）
    pb_need = set(containers[PB].values())
    so_names = set()
    for ls in so_loc.values():
        for _, bn, pid, _ in ls:
            nm = names[bn].get(pid) or names[bn].get(-pid)
            if nm:
                so_names.add(nm[0])
    names[PB] = name_index(PB, pb_need)

    def locate(guid):
        """GUID → [(resolvedBy, bundle, pathId, name)]（只认 SO / prefab 所在的那些包）"""
        out = []
        for how, key in (("container-guid", guid), ("catalog-2nd-key", cat.get(guid))):
            for bn, pid in key2loc.get(key or "", []):
                if bn not in names:
                    continue
                nm = names[bn].get(pid)
                if nm is None:
                    nm = names[bn].get(-pid)
                out.append((how, bn, pid, nm[0] if nm else None))
        return out

    # 悬空引用核对：SO 的 scenarioObjects GUID 在**全库**都不存在（= 原版自己指向了一个没有的资产）
    import glob as _glob
    all_keys = set()
    for d in sorted(_glob.glob(os.path.join(ASSETS_FULL, "bundle_*"))):
        fs = _glob.glob(os.path.join(d, "AssetBundle", "*.json"))
        if not fs:
            continue
        all_keys.update(k for k, _ in json.load(io.open(fs[0], encoding="utf-8"))["m_Container"])
    dangling = {}
    for n, (bn, j) in sorted(sos.items()):
        pg = j["scenarioObjects"]["m_AssetGUID"]
        if pg and pg not in all_keys:
            dangling[n] = pg
    n_bundles = len(_glob.glob(os.path.join(ASSETS_FULL, "bundle_*")))
    # 多 SO 共用同一个 scenarioObjects（原版自己复制错的痕迹）
    bypg = {}
    for n, (bn, j) in sorted(sos.items()):
        pg = j["scenarioObjects"]["m_AssetGUID"]
        if pg:
            bypg.setdefault(pg, []).append(n)
    shared_pg = {g: ns for g, ns in bypg.items() if len(ns) > 1}

    # ---- ⑥ 组装
    out = {
        "_schema": ("offensive_cards/v1 —— 进攻卡（= 环境效果卡）数据表，每阵营一组。"
                    "由 d:/4/Unity/工具/gen_offensive_cards.py 生成，可重跑。"),
        "_sources": {
            "SO": "d:/4/Unity/素材/Warpforge原版/游戏数据/去重定义/MonoBehaviour/EnvironmentalEffectCardsSO.json"
                  "（= assets_full/bundle_duplicateassetisolationso_assets_all/…，⚠️ 资产名比类名少一个 n）",
            "环境SO": "assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/*.json（55 个：13 Default + 42 效果）",
            "prefab": "assets_full/bundle_battleprefabs_vfxandmisc_assets_all/GameObject/Environmental*（43 个）",
            "卡面贴图": "assets_full/bundle_*cardassets*_assets_all/Texture2D/（52 张，13×4）",
            "GUID→包内对象": "各包 AssetBundle/AssetBundle_*.json 的 m_Container；"
                            "帝皇之子那两个包例外（键被截断）⇒ 走 catalog.bin 的第二键",
        },
        "_fields": {
            "forgeLevel": "原版字段（锻造/解锁等级档）",
            "cardRef": "原版字段 offensiveCard / emptyOffensive 的原始 PPtr（m_FileID 恒 4 = 外部文件）",
            "cardName": "**一律 null** —— 卡 PathID 解不出名字（见 _unresolved）",
            "cardName_candidate": "反推的候选卡名（不保证等于卡面印的字）",
            "cardName_candidate_src": "候选的来源",
            "envSO_GUID": "原版原文（enviromentalEffectVFX.m_AssetGUID）",
            "envSO_name": "解出的 SO 名（= 解包里 MonoBehaviour 文件名 = SO 的 m_Name）",
            "envSO_resolvedBy": "container-guid（GUID 直接是包容器键）/ catalog-2nd-key（走 catalog 第二键）",
            "prefab_name": "该 SO 的 scenarioObjects 在 battleprefabs 包里的 GameObject 名",
            "face_texture": "assets_full 里那张卡面贴图的文件名（不含 .png）",
            "face_texture_bundle": "贴图所在包",
        },
        "_unresolved": {
            "cardName": "卡 PathID → 卡名翻不出来：PPtr 的 m_FileID=4 指向外部文件，"
                        "解包 JSON 没有 externals 表；全 assets_full 也没有 `*_<path_id>.json`"
                        "（卡数据不在本地 84 个包里，原版在远端 CCD）",
            "face_texture（帝皇之子槽1/槽2）": "SO 是占位名（1 Green / 2 Fumes），与三张贴图名"
                        "（Aural Hijack / Empyric Rift / Stimm-Vents Leak）对不上 —— 名字配不出来、"
                        "也不许猜 ⇒ null，只给 face_texture_candidates。槽3 的 Empyric Rift 能配上。",
            "envSO_name": "52/52 全解出（48 条走包容器 container-guid、4 条走 catalog-2nd-key："
                          "帝皇之子的 1 Default + 3 卡）",
            "prefab_name": "39/39 全解出；另有 1 个**没被任何卡引用**的 SO（UM Fleet Support）的"
                        " scenarioObjects GUID 全库悬空，见 `unused.dangling_scenarioObjects_GUIDs`",
        },
        "armies": {},
        "unused": {},
        "notes": [],
    }

    n_null = {}
    def bump(k):
        n_null[k] = n_null.get(k, 0) + 1

    for e in entries:
        army = ARMY_NAMES[e["army"]]
        arms = {
            "armyId": e["army"],
            "cardanimsBundle": None,
            "cardassetsBundle": "bundle_%s_assets_all" % CARD_TEXTURES[army],
            "defaultEnviromentalEffectVFX": None,
            "emptyOffensiveCard": None,
            "cards": [],
            "note": None,
        }
        # default（「不使用进攻卡」时生效的那条：scenarioObjects 全空 = 不换任何东西）
        dguid = e["defaultEnviromentalEffectVFX"]["m_AssetGUID"]
        dl = [x for x in locate(dguid) if "cardanims" in x[1]]
        arms["defaultEnviromentalEffectVFX"] = {
            "guid": dguid,
            "envSO_name": dl[0][3] if dl else None,
            "envSO_bundle": dl[0][1] if dl else None,
            "envSO_pathId": dl[0][2] if dl else None,
            "envSO_resolvedBy": dl[0][0] if dl else None,
        }
        if not dl:
            bump("defaultEnvSO")
        if dl:
            arms["cardanimsBundle"] = "bundle_%s_assets_all" % dl[0][1]
        # default 的 scenarioObjects 应为空（= 无变化）
        dso = sos[dl[0][3]][1]["scenarioObjects"]["m_AssetGUID"] if dl else None
        arms["defaultEnviromentalEffectVFX"]["prefab_name"] = None
        arms["defaultEnviromentalEffectVFX"]["scenarioObjects_GUID"] = dso or ""
        arms["defaultEnviromentalEffectVFX"]["note"] = (
            "13 个 Default 的 scenarioObjects **全是空 GUID**（= 不换任何东西，只按 blendTime 补间"
            "环境光/雾）—— 这是「不使用进攻卡」时真正生效的那一条。")
        assert not dso, ("Default 环境 SO 不该有 scenarioObjects", army, dso)

        # 空卡
        ec = e["emptyOffensive"]
        arms["emptyOffensiveCard"] = {
            "cardRef": {"m_FileID": ec["m_FileID"], "m_PathID": ec["m_PathID"]},
            "forgeLevel": None,
            "cardName": None,
            "cardName_candidate": NORMAL_FACE[army],
            "cardName_candidate_src": "卡面名（「不使用进攻卡」那张 = 平静环境，卡面正文框是空的），"
                                      "出处 资料/加时与冲突模式_原版规格.md:306",
            "envSO_GUID": None, "envSO_name": None, "envSO_resolvedBy": None,
            "prefab_name": None,
            "face_texture": NORMAL_TEXTURE[army],
            "face_texture_bundle": CARD_TEXTURES[army],
            "note": "原版 `emptyOffensive` 只有 PPtr（**没有 forgeLevel、没有 envSO**）——"
                    "「不使用进攻卡」不走任何特效 SO，而是用本阵营的 defaultEnviromentalEffectVFX"
                    "（那条 scenarioObjects 为空 = 不换任何东西）。",
        }

        # 三张进攻卡
        for i, c in enumerate(e["offensiveCards"]):
            guid = c["enviromentalEffectVFX"]["m_AssetGUID"]
            loc = [x for x in locate(guid) if "cardanims" in x[1]]
            so_name = loc[0][3] if loc else None
            row = {
                "slot": i + 1,
                "forgeLevel": c["forgeLevel"],
                "cardRef": {"m_FileID": c["offensiveCard"]["m_FileID"],
                            "m_PathID": c["offensiveCard"]["m_PathID"]},
                "cardName": None,
                "cardName_candidate": None,
                "cardName_candidate_src": None,
                "envSO_GUID": guid,
                "envSO_name": so_name,
                "envSO_bundle": loc[0][1] if loc else None,
                "envSO_pathId": loc[0][2] if loc else None,
                "envSO_resolvedBy": loc[0][0] if loc else None,
                "prefab_name": None,
                "prefab_guid": None,
                "face_texture": None,
                "face_texture_bundle": CARD_TEXTURES[army],
            }
            if not loc:
                bump("envSO_name")
            # prefab
            if so_name:
                so = sos.get(so_name)
                assert so is not None, so_name
                pg = so[1]["scenarioObjects"]["m_AssetGUID"]
                row["prefab_guid"] = pg
                if pg:
                    pl = [x for x in locate(pg) if "battleprefabs" in x[1]]
                    row["prefab_name"] = pl[0][3] if pl else None
                    if not pl:
                        bump("prefab_name")
            # 贴图：SO 名剥前缀 → 效果词 → 贴图
            tex, tex_note, cand, cand_src = None, None, None, None
            if so_name:
                d = so_name
                for pfx in sorted(SO_PREFIX.values(), key=len, reverse=True):
                    if d.startswith(pfx):
                        d = d[len(pfx):]
                        break
                want = SO2TEX.get((army, d), d)
                hit = [t for t in TEXTURE_FILES[army] if t != NORMAL_TEXTURE[army]
                       and norm(t.split("_")[-1]) == norm(want)]
                assert len(hit) <= 1, (army, d, want, hit)
                if hit:
                    tex = hit[0]
                    cand = TEX_FACE_NAME.get(tex, want)
                    cand_src = ("卡面名（资料/加时与冲突模式_原版规格.md:311-313 记的「同图异名」）"
                                if tex in TEX_FACE_NAME else "贴图名（卡面未逐张核）")
                    if want != d:
                        tex_note = "SO 名「%s」≠ 贴图名「%s」（原版自己就这样）" % (d, want)
                else:
                    # 只剩帝皇之子：SO 名是占位名（1 Green / 2 Fumes），**名字对不上**，
                    # 只有 `Empyric Rift` 那张能靠名字配上 ⇒ 其余两张不硬配（不猜）
                    cand = d
                    cand_src = "SO 名（原版占位名，**不是**卡面名）"
                    tex_note = ("SO 名是原版占位名（`1 Green` / `2 Fumes`），与三张贴图名"
                                "（Aural Hijack / Empyric Rift / Stimm-Vents Leak）对不上 ⇒ "
                                "face_texture 解不出（候选见 face_texture_candidates）")
            row["face_texture"] = tex
            row["face_texture_note"] = tex_note
            if tex is None:
                row["face_texture_candidates"] = [t for t in TEXTURE_FILES[army]
                                                  if t != NORMAL_TEXTURE[army]]
            row["cardName_candidate"] = cand
            row["cardName_candidate_src"] = cand_src
            arms["cards"].append(row)

        # 每阵营的 note（三处「原版自己就乱」+ 拼写差）
        notes = []
        if army == "EmperorsChildren":
            notes.append(
                "🔴 原版自己就乱①：三张卡的 SO 是占位名 `1 Green / 1 Pink REJECTED / 2 Fumes / "
                "Empyric Rift`，只有 `Empyric Rift` 与卡名对得上；`1 Pink REJECTED` 是废件"
                "（有 SO、有 prefab，但**没有任何卡引用它**）⇒ 槽1/槽2 的卡面贴图只能给候选、不能定死。"
                "② 本阵营两个包的 Addressables 容器键被截断（键 = GUID 前缀，1~3 个字符）"
                "⇒ GUID 是走 catalog.bin 的「第二键」解出来的（envSO_resolvedBy=catalog-2nd-key）。")
        if army == "BlackLegion":
            notes.append(
                "🔴 原版自己就乱：`Void battle` 与 `Warp Storm` 两个 SO 的 scenarioObjects 指向**同一个**"
                "GUID `%s`（= prefab `Environmental Condition Black Legion Warp Storm`）—— 三条槽只用到了 "
                "`Warp Storm`，`Void battle` 这个 SO **没有任何卡引用**（疑似原版复制错）。"
                % " / ".join(shared_pg))
        if army == "Ultramarines":
            notes.append(
                "🔴 原版自己就乱：SO `EnvironmentalCondition Ultramarines Fleet Support` 的 scenarioObjects "
                "GUID `%s` **悬空**（实测不在全库任何一条 `m_Container` 里，见 "
                "`unused.dangling_scenarioObjects_GUIDs`）；而且这个 SO **没有任何卡引用它**"
                "（三条槽用的是 Orbital Bombardment / Thunderstorm / Aerial Clash）。"
                "另一处名字错位：SO 叫 `Orbital Bombardment`，prefab 叫 `Bombardment`。"
                % dangling.get("EnvironmentalCondition Ultramarines Fleet Support"))
            notes.append("⚠️ 本条线唯一 forgeLevel 第三档 = **40**（其余 12 阵营都是 45）—— 原版原文如此，照抄。")
        if army == "Sororitas":
            notes.append("⚠️ SO 名与贴图名两处对不上：`Disrupted Ceremony`↔`Disrupted Sanctuary`、"
                         "`Shrine Bombardment`↔`Shrine Bombardement`（拼写）。")
        if army == "AstraMilitarum":
            notes.append("⚠️ SO 名 `Factory Overdrive` ↔ 贴图名 `Factories Overdrive` ↔ 卡面名 "
                         "`Manufactorum Overdrive`（三种写法，出处见 _sources/贴图那张表）。")
        if army == "DarkAngels":
            notes.append("⚠️ SO 名 `Orbiting` ↔ 贴图名 `Star Orbiting` ↔ 卡面名 `Star Orbit`。")
        if army == "Sautekh":
            notes.append("⚠️ 贴图名 `Immortal Beams` ≠ 卡面名 `Ethereal Energy`（同图异名）。")
        if army == "Leviathan":
            notes.append("⚠️ SO 名 `Blazing Biomatter` ↔ 贴图名 `Blazing Biomass`。")
        if army == "Goff":
            notes.append("⚠️ SO 名 `Night` ↔ 贴图名 `Night Attack`；卡面正文**唯一**真印了 "
                         "`No Offensive Effect` 的是本阵营那张空卡。")
        arms["note"] = " ".join(notes) if notes else None
        out["armies"][army] = arms

    # ---- ⑦ 没用上的件（交叉核对：43 个 prefab / 55 个 SO）
    all_pb = {k: (names.get(PB, {}).get(v) or names.get(PB, {}).get(-v))
              for k, v in containers[PB].items()}
    referenced = set()
    for arms in out["armies"].values():
        for r in arms["cards"]:
            if r["prefab_guid"]:
                referenced.add(r["prefab_guid"])
    env_prefabs = sorted(n[0] for g, n in all_pb.items()
                         if n and n[1] == "GameObject" and "nvironment" in n[0])
    out["unused"]["prefabs_not_referenced_by_any_SO"] = sorted(
        n[0] for g, n in all_pb.items()
        if n and n[1] == "GameObject" and "nvironment" in n[0] and g not in referenced)
    out["unused"]["prefabs_total_environmental_GameObjects"] = len(env_prefabs)
    used_so = {r["envSO_name"] for arms in out["armies"].values() for r in arms["cards"]}
    out["unused"]["envSOs_not_referenced_by_any_card"] = sorted(
        n for n, (bn, j) in sos.items()
        if n not in used_so and j["scenarioObjects"]["m_AssetGUID"])  # 空 scenarioObjects = Default，不算废件
    out["notes"].append(
        "交叉核对：battleprefabs 包里 `Environmental*` 的 **GameObject %d 个**；其中**没有任何 SO 引用**的"
        "列在 `unused.prefabs_not_referenced_by_any_SO` —— 里面 `…OLD`（EC Empyric Rift / GSC Sump Overspill）"
        "是真废件，`Environmental Condition Particles Orbital` 是**公共件**（被别的 prefab 用，不直接被 SO 引用）。"
        % len(env_prefabs))
    # 悬空引用（数值都是算出来的，不写死）
    out["unused"]["dangling_scenarioObjects_GUIDs"] = [
        {"envSO": n, "scenarioObjects_GUID": g,
         "referenced_by_any_card": n in used_so} for n, g in sorted(dangling.items())]
    out["notes"].append(
        "🔴 悬空引用核对：55 个 SO 里 scenarioObjects GUID **在全部 %d 个包的 %d 条 `m_Container` 里都找不到**的"
        "有 %d 条 —— 见 `unused.dangling_scenarioObjects_GUIDs`（原版自己指向了一个本地没有的资产）。"
        % (n_bundles, len(all_keys), len(dangling)))
    out["notes"].append(
        "🔴 「进攻卡 = 环境效果卡」的判据：`BattleHud__isEmptyOffensiveCard.c` 里直接调 "
        "`EnviromentalEffectCardsSO__GetEmptyOffensiveCard`；生效链见 `资料/加时与冲突模式_原版规格.md` §2.4/§2.4b。")
    out["notes"].append(
        "⚠️ **卡槽顺序 ≠ PnP 编号顺序**：例：UM 三槽 = Orbital Bombardment / Thunderstorm / Aerial Clash，"
        "而 PnP 68/69/70 = Aerial Clash / Orbital Bombardment / Thunderstorm ⇒ 别按序号推卡名，"
        "本表按「SO 名 → 贴图名」配。")
    out["stats"] = {
        "armies": len(out["armies"]),
        "cards": sum(len(a["cards"]) for a in out["armies"].values()),
        "nulls_per_field": n_null,
    }

    # ---- ⑧ 统计（null 数**从产物里数**，不靠散落的计数器）
    def count_nulls(obj, path=""):
        if isinstance(obj, dict):
            for k, v in obj.items():
                if k.startswith("_"):
                    continue
                for x in count_nulls(v, path + "/" + k):
                    yield x
        elif isinstance(obj, list):
            for v in obj:
                for x in count_nulls(v, path):
                    yield x
        elif obj is None:
            yield path
    tally = {}
    for p in count_nulls(out):
        parts = p.split("/")
        if len(parts) > 2 and parts[1] == "armies":
            parts[2] = "*"                      # 按字段聚合，别按阵营拆开数
        q = "/".join(parts)
        tally[q] = tally.get(q, 0) + 1
    out["stats"]["nulls_per_field"] = dict(sorted(tally.items()))

    os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
    with io.open(OUT_JSON, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(out, fh, ensure_ascii=False, indent=1)
        fh.write("\n")

    # ---- ⑨ 打印
    print("=== gen_offensive_cards ===")
    print("输出：", OUT_JSON)
    print("阵营：%d   每阵营卡数：%s   总槽数：%d"
          % (len(out["armies"]), sorted({len(a["cards"]) for a in out["armies"].values()}),
             out["stats"]["cards"]))
    full = [a for a, v in out["armies"].items()
            if all(r["envSO_name"] and r["prefab_name"] and r["face_texture"] for r in v["cards"])
            and v["defaultEnviromentalEffectVFX"]["envSO_name"]]
    print("三条槽 + default 全解出的阵营：%d / 13 %s" % (len(full), "" if len(full) == 13 else
          "（差：%s）" % sorted(set(out["armies"]) - set(full))))
    print("null 计数（照产物数）：")
    for p, c in sorted(tally.items()):
        print("   %-42s %d" % (p, c))
    print("没被任何 SO 引用的 prefab（GameObject，共 %d 个环境 prefab）：%s"
          % (out["unused"]["prefabs_total_environmental_GameObjects"],
             out["unused"]["prefabs_not_referenced_by_any_SO"]))
    print("没被任何卡引用的环境 SO：", out["unused"]["envSOs_not_referenced_by_any_card"])
    for a, v in out["armies"].items():
        for r in v["cards"]:
            print("  %-16s slot%d lvl%-3s %-52s %s" %
                  (a, r["slot"], r["forgeLevel"], r["envSO_name"], r["prefab_name"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
