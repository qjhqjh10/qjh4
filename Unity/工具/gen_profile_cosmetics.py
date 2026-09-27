#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_profile_cosmetics.py — 从原版 **装饰品定义数据**里抽出档案窗要用的两张清单。

为什么要它（2026-09-27，玩家档案窗的 Title 页 / Avatar 页）：
    原版这两页的列表来自**服务器**（`PlayerDataManager.fullCosmeticCollection.OfType<CosmeticItemTitle>()`
    / `PlayerAvatarDataManager.GetAllItems<T>()`），本地没有存档。
    但**定义数据（ScriptableObject）在本地是全的**：470 个头像 + 462 个称号
    （`素材/Warpforge原版/装饰品/定义数据/MonoBehaviour/`）—— 那不是「存档」，是**游戏自己的资产清单**。
    ⇒ 把它抽成一张运行时读得到的 JSON，两页就有真列表可列（用户 2026-09-27 拍板：**照填**）。

🔴 **显示名是我们拼的**（原版真名在远端 I2 语言表里，本地只有 key）：
    · 头像：`nameTextReference`（形如 `Avatar_UM_Attack Bike`）**去掉 `Avatar_<阵营>_` 前缀** ⇒ `Attack Bike`；
      它是空的话退回 `m_Name` 走同一条规则。
    · 称号：462 条的 `nameTextReference` **全是空串** ⇒ 只能拿 `m_Name`（`Title_UM_Premium_1`）
      去掉 `Title_`、下划线换空格 ⇒ `UM Premium 1`。
    ⚠️ **这条要写进游戏里的注释**：名字不是原版文案，是**从资源名反推的**。

用法：
    python 工具/gen_profile_cosmetics.py            # 生成
    python 工具/gen_profile_cosmetics.py --check    # 只统计，不写

产出：`MyGame/Assets/CardPresentation/Resources/profile_cosmetics.json`
      （短键：`id` / `n` 名字 / `a` 阵营 id / `art` 头像图名）
"""
import argparse
import io
import json
import os
import re
import sys

try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

SRC = 'd:/4/Unity/素材/Warpforge原版/装饰品/定义数据/MonoBehaviour'
OUT = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/profile_cosmetics.json'
# 头像图（导入器要用同一个目录）：`d:/4/Unity/素材/Warpforge原版/装饰品/头像/Texture2D/`
PIDSUF = re.compile(r'_\d{10,}$')       # `名_<长数字>.json` = AssetStudio 给的重复拷贝，跳过

# 🔴 **变体头像的 SO 名 ≠ 图名**（实测：469 条里只有 281 条同名）。
#    SO 里真正的链接是 `imageReference.m_AssetGUID`，而 GUID → 资产要两步：
#      ① **AssetBundle 的 `m_Container`** 是 `GUID → [PathID, …]`（实测一个 GUID 对 2 个 pid：Sprite + Texture2D）
#      ② `头像/Sprite/<名>_<pid>.json` 的**文件名后缀就是 pid** ⇒ 拿它反查名字
#    （`头像/Texture2D/` 那份**没有**带 pid 的文件名，所以只能用 Sprite 那份 —— 两者名字一致。）
BUNDLE_MANIFEST = 'd:/2/新解包资源/assets_full/bundle_cosmeticavatarsimages_assets_all/AssetBundle'
SPRITE_DIR = 'd:/4/Unity/素材/Warpforge原版/装饰品/头像/Sprite'

# ---- 🆕 成就（2026-09-27 第二轮才找到：**文件名是 `ACH##`，不是 `Achievement*`**）----------
# `AllAchievements.json` = `AchievementsList` 本体（`achievements: [102 个引用]`），
# 102 个 `ACH<n> <名字>.json` = 每个成就一个 SO。
# 🔴 **上一轮报「本地没有成就清单」是错的** —— 错因就是**只按类名找文件**（`eventId` 是 `Ach_N`）⇒
#    搜 `achievement`/`achiev` 必然 0 命中。**这一条已写进 `档案窗_Trophies页.md` §C3 的订正块。**
# 数据格式：`challenge` 是 **`SerializeReference`（`references.RefIds` 里按 `rid` 取）**，不是内联对象。
ACH_SRC = 'd:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/MonoBehaviour'
ACH_NAME = re.compile(r'^ACH\d+\s+')


def guid_to_name():
    """`AssetGUID → 头像图名`（**两级**：bundle 清单的 GUID→pid，再 Sprite 文件名里的 pid→名）。"""
    pid2name = {}
    for fn in os.listdir(SPRITE_DIR):
        if not fn.endswith('.json'):
            continue
        m = re.search(r'_(-?\d{10,})\.json$', fn)
        if m:
            pid2name[m.group(1)] = fn[:m.start()]
    g2n, n_hit = {}, 0
    if os.path.isdir(BUNDLE_MANIFEST):
        for fn in sorted(os.listdir(BUNDLE_MANIFEST)):
            if not fn.endswith('.json'):
                continue
            try:
                d = json.load(io.open(os.path.join(BUNDLE_MANIFEST, fn), encoding='utf-8'))
            except Exception:
                continue
            for entry in d.get('m_Container', []):
                try:
                    guid, info = entry[0], entry[1]
                    pid = str(info['asset']['m_PathID'])
                except Exception:
                    continue
                if pid in pid2name:
                    g2n.setdefault(guid, pid2name[pid])
                    n_hit += 1
    print('GUID→图名：%d 条（Sprite 侧 pid 表 %d 条）' % (len(g2n), len(pid2name)))
    return g2n


def strip_prefix(name, prefix):
    """去掉 `Avatar_UM_` / `Title_` 这类前缀，剩下的换下划线为空格。"""
    s = name
    if s.startswith(prefix):
        s = s[len(prefix):]
        # 头像还有一节阵营缩写（`UM_` / `AM_` / `Ork_` …）—— 只去第一节
        if prefix == 'Avatar_':
            parts = s.split('_', 1)
            if len(parts) == 2 and 1 <= len(parts[0]) <= 4:
                s = parts[1]
    return s.replace('_', ' ').strip()


def load(kind, prefix, g2n=None):
    g2n = g2n or {}
    out, seen = [], set()
    for fn in sorted(os.listdir(SRC)):
        if not fn.endswith('.json') or not fn.startswith(prefix):
            continue
        base = fn[:-5]
        if PIDSUF.search(base):          # 重复拷贝，跳过
            continue
        # `Avatar_WF_*` 是**占位图不是可选头像**（`Warpforge Empty` / `Warpforge Splash`）——
        # 普查报告点过名（`资料/普查产出_0927/` 那批）。列出来会多两个假头像。
        if kind == 'avatar' and base.startswith('Avatar_WF_'):
            continue
        try:
            d = json.load(io.open(os.path.join(SRC, fn), encoding='utf-8'))
        except Exception:
            continue
        uid = d.get('uniqueId') or ''
        if not uid or uid in seen:
            continue
        seen.add(uid)
        ref = (d.get('nameTextReference') or '').strip()
        nm = strip_prefix(ref, prefix) if ref else strip_prefix(base, prefix)
        if not nm:
            nm = base
        e = {'id': uid, 'n': nm, 'a': d.get('cardArmy', 0)}
        if kind == 'avatar':
            # 🔴 图名**优先走 `imageReference.m_AssetGUID` 反查**（变体头像的 SO 名与图名不同名，
            #    实测 469 条里 188 条对不上）—— 反查不到才退回 SO 名。
            g = (d.get('imageReference') or {}).get('m_AssetGUID') or ''
            e['art'] = g2n.get(g) or base
        out.append(e)
    # 稳定排序：先阵营、再名字（自检要能按序断）
    out.sort(key=lambda e: (e['a'], e['n'].lower()))
    return out


def load_achievements():
    """102 个 `ACH*.json` → `{id, n, t, c, v[], q[]}`。

    · `n` 显示名 = `m_Name` 去掉 `ACH<n> ` 前缀（`ACH1 Slay the Warlord` → **`Slay the Warlord`**）
      —— **这是原版资产里的真字符串**，不是我们编的（与称号/头像那两批的处理不同，那两批只能反推）。
    · `t` = `achievementType`（位标志：1 Battle / 2 Collection / 4 Victories / 8 Account）。
    · `c` = `challenge.id`（例 `"Damage To Warlord"`）；`v` = 各档 `targetValue`（**真阈值**）；
      `q` = 各档奖励数量（`rewards[].rewards[0].quantity`，就是 `Achievements/Points` 那个数）。
    """
    out = []
    if not os.path.isdir(ACH_SRC):
        print('⚠️ 找不到成就目录 %s' % ACH_SRC)
        return out
    for fn in sorted(os.listdir(ACH_SRC)):
        base = fn[:-5] if fn.endswith('.json') else None
        if not base or PIDSUF.search(base) or not ACH_NAME.match(base):
            continue
        try:
            d = json.load(io.open(os.path.join(ACH_SRC, fn), encoding='utf-8'))
        except Exception:
            continue
        ch = d.get('challenge') or {}
        rid = ch.get('rid')
        node = None
        for r in (d.get('references') or {}).get('RefIds', []):
            if r.get('rid') == rid:
                node = r
                break
        data = (node or {}).get('data') or {}
        rw = data.get('rewards') or []
        out.append({
            'id': d.get('eventId', ''),
            'n': ACH_NAME.sub('', base),
            't': d.get('achievementType', 0),
            'c': data.get('id', ''),
            'v': [r.get('targetValue', 0) for r in rw],
            'q': [((r.get('rewards') or [{}])[0]).get('quantity', 0) for r in rw],
        })
    # 排序：先类型（Battle→Collection→Victories→Account 与位标志同序），再 eventId 里的数字
    def key(e):
        m = re.search(r'(\d+)', e['id'])
        return (e['t'], int(m.group(1)) if m else 0)
    out.sort(key=key)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true')
    args = ap.parse_args()

    av = load('avatar', 'Avatar_', guid_to_name())
    ti = load('title', 'Title_')
    ac = load_achievements()
    print('成就 %d 条' % len(ac))
    if ac:
        import collections
        print('  类型分布（1Battle/2Collection/4Victories/8Account）：',
              dict(sorted(collections.Counter(e['t'] for e in ac).items())))
        print('  档位数分布：', dict(sorted(collections.Counter(len(e['v']) for e in ac).items())))
        print('  例：', ' / '.join('%s[t%d, %d档 %s]' % (e['n'], e['t'], len(e['v']), e['v'][:2]) for e in ac[:2]))
    # 图名对不上 PNG 的（反查失败的）——**报出来**，别让它静默变成没立绘的格子
    pngs = set(f[:-4] for f in os.listdir('d:/4/Unity/素材/Warpforge原版/装饰品/头像/Texture2D')
               if f.endswith('.png'))
    noart = [e for e in av if e['art'] not in pngs]
    print('头像 %d 条' % len(av))
    print('  ⚠️ 图名对不上 PNG 的：%d 条%s' % (len(noart), ('  例：' + ' / '.join(e['art'] for e in noart[:3])) if noart else ''))
    print('称号 %d 条' % len(ti))
    for k, v in (('头像', av), ('称号', ti)):
        if v:
            print('  %s 例：%s' % (k, ' / '.join(e['n'] for e in v[:4])))
    # 阵营分布（检查有没有「空阵营」—— 普查报告提醒过 `TAU`/`Tau` 两套拼写要合并）
    import collections
    print('  头像阵营分布：', dict(sorted(collections.Counter(e['a'] for e in av).items())))
    print('  称号阵营分布：', dict(sorted(collections.Counter(e['a'] for e in ti).items())))

    if args.check:
        return 0
    doc = {'avatars': av, 'titles': ti, 'achievements': ac,
           '_note': '名字是从资源名反推的（原版真名在远端 I2 语言表）；生成器 工具/gen_profile_cosmetics.py'}
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    io.open(OUT, 'w', encoding='utf-8', newline='').write(
        json.dumps(doc, ensure_ascii=False, separators=(',', ':')))
    print('写好 %s（%.1f KB）' % (OUT, os.path.getsize(OUT) / 1024.0))
    return 0


if __name__ == '__main__':
    sys.exit(main())
