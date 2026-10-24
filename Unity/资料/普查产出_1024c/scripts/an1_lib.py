# -*- coding: utf-8 -*-
"""AN1 只读审查 —— 公共装载器（2026-10-24）。

只读：不改任何代码 / 数据 / 场景。所有路径都**现读**，不写死张数。
用法（其它脚本 import）：`python -I -X utf8` 运行；本文件不放 `工具/`。
"""
import io, json, os, glob, sys

ROOT = "d:/4/Unity"
ASSETS = ROOT + "/MyGame/Assets"
ASSETS_FULL = "d:/2/新解包资源/assets_full"

# ---- 路径 ----
P_CARDS_ENGINE = ASSETS + "/RuleEngine/Resources/cards_engine.json"
P_ORIG_VFX    = ROOT + "/数据/游戏数据/card_vfx_by_card.json"
P_RT_VFX      = ASSETS + "/CardPresentation/Resources/card_vfx_by_card.json"
P_CARDANIM_CLIPS = ROOT + "/数据/游戏数据/cardanim_clips.json"
P_RT_CARDANIM_CLIPS = ASSETS + "/CardPresentation/Resources/cardanim_clips.json"
P_ANIM_ADDR   = ROOT + "/数据/索引/anim_address_map.json"
P_ANIMFX_MOD  = ROOT + "/数据/游戏数据/animfx_modules.json"
P_RT_ANIMFX_SOUNDS = ASSETS + "/CardPresentation/Resources/animfx_sounds.json"
P_VOICE       = ASSETS + "/CardPresentation/Resources/voice_lines.json"
P_PREFAB_DIR  = ASSETS + "/WarpforgeVFX/Prefabs"
P_EFFECT_REPORT = ASSETS + "/WarpforgeVFX/效果库报告.tsv"
P_LEDGER      = ROOT + "/资料/特效还原台账.tsv"
P_ABILITY_DUR = ASSETS + "/CardPresentation/Resources/ability_anim_durations.json"
P_ABILITY_DUR_FULL = ROOT + "/数据/游戏数据/ability_anim_durations.json"


def jload(p):
    with io.open(p, encoding="utf-8") as f:
        return json.load(f)


def load_cards():
    return jload(P_CARDS_ENGINE)


def load_orig_vfx():
    return jload(P_ORIG_VFX)


def load_rt_vfx():
    return jload(P_RT_VFX)


def load_anim_addr():
    return jload(P_ANIM_ADDR)


def load_animfx():
    return jload(P_ANIMFX_MOD)


def load_cardanim_clips(full=True):
    return jload(P_CARDANIM_CLIPS if full else P_RT_CARDANIM_CLIPS)


def load_prefab_names():
    """我们工程里 `WarpforgeVFX/Prefabs/` 下的 prefab 名（不含扩展名）。"""
    return set(os.path.splitext(os.path.basename(p))[0]
               for p in glob.glob(P_PREFAB_DIR + "/*.prefab"))


def load_effect_report():
    """`效果库报告.tsv` → {效果名: 行 dict}。判定列 Z=对得上 / W=两边全程空 / 其它=异常。"""
    import io as _io
    rows = [l.rstrip("\n").split("\t") for l in _io.open(P_EFFECT_REPORT, encoding="utf-8")]
    hdr = rows[0]
    out = {}
    for r in rows[1:]:
        if len(r) < len(hdr):
            r = r + [""] * (len(hdr) - len(r))
        d = dict(zip(hdr, r))
        out[d["效果名"]] = d
    return out


def load_ledger():
    """`资料/特效还原台账.tsv` → {效果名: 行 dict}（判定列 A-E/W/Z）。"""
    import io as _io
    rows = [l.rstrip("\n").split("\t") for l in _io.open(P_LEDGER, encoding="utf-8-sig")]
    hdr = rows[0]
    out = {}
    for r in rows[1:]:
        if len(r) < len(hdr):
            r = r + [""] * (len(hdr) - len(r))
        d = dict(zip(hdr, r))
        out[d["效果名"]] = d
    return out


def cards_of_faction(pool, faction):
    return [c for c in pool["cards"] if c.get("faction") == faction]


def rt_events_by_card(rt):
    """运行期表 → {id: {ev: [entry,...]}}"""
    out = {}
    for c in rt["cards"]:
        ev = {}
        for e in c.get("events") or []:
            ev[e["ev"]] = e.get("es") or []
        out[c["id"]] = ev
    return out


def orig_events_by_card(orig):
    """原版表 → {id: {ev: [entry,...]}}（条目带 soField/src）。"""
    out = {}
    for cid, v in orig["byCard"].items():
        ev = {}
        for e in v.get("events") or []:
            ev[e["ev"]] = e.get("es") or []
        out[cid] = ev
    return out


def original_prefab_exists(prefab_name, addr=None):
    """原版 `assets_full` 里有没有这个名字的 GameObject dump。

    判据 = `anim_address_map.cardanim_to_asset[*].targetName`（CardAnim 的落点）+
    `guid_to_asset` 的 `type == GameObject`。**不是**磁盘全盘 grep（太慢且会误命中子物体名）。
    """
    if addr is None:
        addr = load_anim_addr()
    for k, v in addr["cardanim_to_asset"].items():
        if v.get("targetName") == prefab_name:
            return v
    for g, v in addr["guid_to_asset"].items():
        if v.get("type") == "GameObject" and v.get("name") == prefab_name:
            return v
    return None
