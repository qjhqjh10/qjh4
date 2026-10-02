#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""read_default_cardbacks.py — 从原版原始字节里读出**每阵营的默认卡背**（`DefaultCarbackByArmySO`）。

为什么要这个工具（2026-10-03 立，`项目任务.md` §三 第 29 条 **A20**）
--------------------------------------------------------------------
`CardArt.DeckCardback(id, faction)` 在「这副牌没选卡背」时要退回**该阵营的默认卡背**
（原版 `CardDeck.GetDeckCardback` → `ArmyUtilities.GetDefaultCardback(deckArmy)`）。
我们原来只有 **4 张**手挑的 `Art/cards/back_<阵营>.png`（`import_original_art.py` 的 `BACKS`），
**其余 9 个阵营取不到** ⇒ 牌堆/卡组详情窗那格空着。

那张表在原版是**一份 SO**：
  `ArmyUtilities.defaultCardBacks`（`+0x20`）→ `DefaultCarbackByArmySO`（MonoScript pathID **625**）
  → 字段 `cardbackByArmies` = `List<ArmyItem<CosmeticItemCardback>>`（13 条，army 10…130，**没有 Neutral**）。

🔴 **它不在任何 bundle 里，也不在任何「有 type tree」的导出里** —— 实测：
  · 全 `assets_full` 按字段名 `cardbackByArmies` 搜 = **0 命中**；
  · bundle / `resources.assets` / `level0` 的 MonoBehaviour 导出里都没有它；
  · `level0` 那 147 个 MonoBehaviour 里**只有 77 个**有 type tree（其余 70 个 `read_typetree` 直接抛
    「Expected to read N bytes, but only read 32」）⇒ **「grep 字段名」对它们是无效否定**（CLAUDE.md 同族坑）。
  ⇒ 只能照 `read_gradient2_level0.py` 那套办法：**按原始字节手工切**。

怎么切的（两处踩过才对上）
--------------------------
① 链路：`level0` 里的 MonoBehaviour pathID **391**（`m_Script` pathID = **3281** = `ArmyUtilities`）
   的 `+0x20` 字段 = `defaultCardBacks` → `{m_FileID = 2, m_PathID = 420}`；
   `level0` 的 externals = `[globalgamemanagers.assets, sharedassets0.assets, …]`
   ⇒ **fileID 2 = `sharedassets0.assets`**，对象 pathID **420**（同一个文件里就能找全）。
② 🔴 **MB 头不是固定 32 字节** —— `m_Name` 是**对齐后长度可变**的串：
   `[28] = 串长`、`[32 : 32+align4(串长)]` 是名字，**首字段从后面接着**。
   这份 SO 的 `m_Name = "DefaultCarbackByArmySO"`（22 字符）⇒ 数据从 **56** 开始。
   （第一版按 32 切，切出来 `计数 = 1634100548` 那种垃圾值 —— 别再用固定 32。）
   每条 = `int32 army` + `PPtr{m_FileID:i32, m_PathID:i64}`，`m_FileID = 0` = 同文件。

实测结果（2026-10-03，13/13 全部按 pathID 找到了名字、且都能对上我们那份卡背 SO 清单）：

    army 10  Ultramarines     → Cardback_UM_Campaign_Free
    army 20  Goff             → Cardback_GOF_Campaign_Free
    army 30  SaimHann         → Cardback_ASH_Campaign_Free
    army 40  Sautekh          → Cardback_SAU_Campaign_Free
    army 50  BlackLegion      → Cardback_BL_Campaign_Free
    army 60  Leviathan        → Cardback_TL_Campaign_Free
    army 70  TauEmpire        → Cardback_TAU_Campaign_Free
    army 80  Sororitas        → Cardback_SOR_Campaign_Free
    army 90  Genestealers     → Cardback_GSC_Dawn of Uprising
    army 100 AstraMilitarum   → Cardback_AM_Shield of Humanity
    army 110 DarkAngels       → Cardback_DA_The First Legion
    army 120 EmperorsChildren → Cardback_EC_Emperors Children
    army 130 SpaceWolves      → Cardback_SW_Campaign_Free

用法
----
    PYTHONIOENCODING=utf-8 python 工具/read_default_cardbacks.py          # 打表（人看）
    # 或者当模块用（`gen_cardbacks.py` 就是）：from read_default_cardbacks import read_defaults

⚠️ 只读 `d:/2`（铁律 2：那是档案库，不写）。
"""
import json
import os
import struct
import sys

DATA = 'd:/2/unity_run_ref/Warpforge_Data'
SO_PID = 420                 # sharedassets0.assets 里 DefaultCarbackByArmySO 的 pathID
ARMY_UTILITIES_PID = 391     # level0 里 ArmyUtilities 的 pathID（只用于自检这条链路）
ARMY_SCRIPT_PID = 3281       # ArmyUtilities 的 MonoScript pathID
SO_SCRIPT_PID = 625          # DefaultCarbackByArmySO 的 MonoScript pathID
CARD_SO_DIR = 'd:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour'

ARMY = {
    0: 'Neutral', 10: 'Ultramarines', 20: 'Goff', 30: 'SaimHann', 40: 'Sautekh',
    50: 'BlackLegion', 60: 'Leviathan', 70: 'TauEmpire', 80: 'Sororitas',
    90: 'Genestealers', 100: 'AstraMilitarum', 110: 'DarkAngels',
    120: 'EmperorsChildren', 130: 'SpaceWolves',
}


def first_field_offset(raw):
    """MB 头：`m_GameObject`(12) + `m_Enabled`(4) + `m_Script`(12) + `m_Name`(4 + 对齐串) → 首字段偏移。"""
    n = struct.unpack_from('<i', raw, 28)[0]
    name = raw[32:32 + n].decode('utf-8', 'replace')
    return 32 + ((n + 3) // 4) * 4, name


def _card_so_by_name():
    """那份 bundle 里的卡背 SO：名字 → 记录（用来补 rarity / uniqueId / 可复查坐标）。"""
    out = {}
    for f in os.listdir(CARD_SO_DIR):
        if not f.endswith('.json'):
            continue
        d = json.load(open(os.path.join(CARD_SO_DIR, f), encoding='utf-8'))
        if isinstance(d, list):
            d = d[0]
        if (d.get('m_Name') or '').startswith('Cardback'):
            out[d['m_Name']] = d
    return out


def read_defaults(verbose=False):
    """读 `DefaultCarbackByArmySO` → `[{'armyId', 'army', 'cardback'(SO 名), 'name'(我们的图名), 'uniqueId'}]`。

    `name` 用 `imageReference.m_SubObjectName` 去掉 `_Main`（**与 `gen_cardbacks.py` 同一把对账键**）。"""
    import UnityPy
    env = UnityPy.load(os.path.join(DATA, 'sharedassets0.assets'))
    objs = {o.path_id: o for o in env.objects}
    so = objs.get(SO_PID)
    if so is None:
        raise SystemExit('sharedassets0.assets 里没有 pathID %d' % SO_PID)
    raw = so.get_raw_data()
    off, so_name = first_field_offset(raw)
    fid, pid = struct.unpack_from('<iq', raw, 16)
    if pid != SO_SCRIPT_PID:
        raise SystemExit('pathID %d 的 m_Script = %d，期望 %d（DefaultCarbackByArmySO）'
                         % (SO_PID, pid, SO_SCRIPT_PID))
    n = struct.unpack_from('<i', raw, off)[0]
    off += 4
    by_name = _card_so_by_name()
    rows = []
    for _ in range(n):
        army, _fid, p = struct.unpack_from('<iiq', raw, off)
        off += 16
        tgt = objs.get(p)                       # m_FileID = 0 ⇒ 同文件
        nm = ''
        if tgt is not None:
            _, nm = first_field_offset(tgt.get_raw_data())
        d = by_name.get(nm) or {}
        sub = ((d.get('imageReference') or {}).get('m_SubObjectName') or '')
        if sub.endswith('_Main'):
            sub = sub[:-5]
        rows.append({
            'armyId': army,
            'army': ARMY.get(army, ''),
            'cardback': nm,                     # 原版 SO 名（可复查）
            'name': sub,                        # **我们的图名**（= Cardbacks.json 的对账键）
            'uniqueId': d.get('uniqueId') or '',
        })
    if verbose:
        print('SO %r（pathID %d）· 条目 %d 条' % (so_name, SO_PID, n))
        for r in rows:
            print('  army=%-4s %-16s → %-46s %s' % (r['armyId'], r['army'], r['cardback'], r['name']))
    return rows


if __name__ == '__main__':
    rows = read_defaults(verbose=True)
    miss = [r for r in rows if not r['name']]
    print('\n查不到的：%d 条 %s' % (len(miss), miss))
    sys.exit(1 if miss else 0)
