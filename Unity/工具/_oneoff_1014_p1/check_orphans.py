# -*- coding: utf-8 -*-
"""P1 收口 · 逐张对拍：id 置换之后，**静态产物里按旧 id 写的那些文件/键**都要点出来。

判据（施工设计 §五·4 / §五·6）：改 id 之后
  · `Resources/Art/cards/art_<旧 id>.png` 全变孤儿（**静态产物不报错**，所以必须逐张核）
  · `voice_lines.json` / `card_icon_plan.json` / `card_vfx_by_card.json` / `card_inventory_options.json` /
    `card_initiative.json` / `related_cards.json` / `talent_cards.json` / `prebuilt_decks.json` /
    `ability_anim_durations.json` 的**键**要么已按新 id 重出、要么就是缺口
  · 卡池每张卡都要有图（`art_<新 id>.png`），逐张点出来

用法：python -I -X utf8 d:/4/Unity/工具/_oneoff_1014_p1/check_orphans.py
"""
import io
import json
import os

B = "d:/4/_tmp_view/p1_1014/bak/"
ART = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/cards"
POOL = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"


def load_ids(p):
    return [x for x in io.open(p, encoding='utf-8').read().split('\n') if x]


def keys_of(path, key):
    d = json.load(io.open(path, 'r', encoding='utf-8'))
    v = d[key]
    if isinstance(v, list):
        out = []
        for x in v:
            if isinstance(x, dict):
                out.append(x.get('id') or x.get('cardId') or x.get('card') or x.get('cardId'))
            else:
                out.append(str(x))
        return [str(x) for x in out if x]
    return list(v.keys())


def main():
    pool = json.load(io.open(POOL, 'r', encoding='utf-8'))['cards']
    ids = [c['id'] for c in pool]
    idmap = json.load(io.open('d:/4/_tmp_view/p1_1014/idmap.json', 'r', encoding='utf-8'))
    new_of = set(idmap.values())
    old_of = set(idmap)

    files = sorted(f for f in os.listdir(ART) if f.endswith('.png'))
    have = set(f[4:-4].lower() for f in files if f.startswith('art_'))
    print("== ① 卡图（`art_<id>.png`）==")
    print("  盘上 %d 张 · 卡池 %d 张" % (len(have), len(ids)))
    miss = [c for c in pool if c['id'].lower() not in have]
    print("  🔴 卡池里有、盘上没有的：%d" % len(miss))
    for c in miss[:20]:
        print("     ", c['id'], c['faction'], c['name'])
    orph_new = sorted(x for x in new_of if x.lower() not in have)
    print("  🔴 这次换出来的新 id 里、没图的：%d %s" % (len(orph_new), orph_new[:10]))
    orph_old = sorted(x for x in old_of if x.lower() in have)
    print("  ⚠️ 旧 id 的图还留在盘上（孤儿）：%d" % len(orph_old))
    for x in orph_old[:20]:
        print("     ", x, '->', idmap[x], '(新 id 有图：%s)' % (idmap[x].lower() in have))

    print("\n== ② 按 id 建键的产物 ==")
    tables = [
        ('voice_lines.json', "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/voice_lines.json", 'cards'),
        ('card_icon_plan.json', "d:/4/Unity/数据/游戏数据/card_icon_plan.json", 'cards'),
        ('card_icon_plan(runtime).json', "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_icon_plan.json", 'cards'),
        ('card_vfx_by_card.json', "d:/4/Unity/数据/游戏数据/card_vfx_by_card.json", 'byCard'),
        ('card_inventory_options.json', "d:/4/Unity/数据/游戏数据/card_inventory_options.json", 'map'),
        ('card_initiative.json', "d:/4/Unity/数据/游戏数据/card_initiative.json", 'map'),
        ('related_cards.json', "d:/4/Unity/数据/游戏数据/related_cards.json", None),
        ('talent_cards.json', "d:/4/Unity/数据/游戏数据/talent_cards.json", None),
    ]
    for lab, path, key in tables:
        if not os.path.exists(path):
            print("  %-28s ⚠️ 文件不在 %s" % (lab, path))
            continue
        d = json.load(io.open(path, 'r', encoding='utf-8'))
        if key is None:
            ks = list(d.keys())
            # 这两份的表头是 note/version… 之类，只有形如卡 id 的键才算
            ks = [k for k in ks if not k.islower()]
        else:
            ks = keys_of(path, key)
        s = set(ks)
        stale = sorted(x for x in old_of if x in s)
        fresh = sorted(x for x in new_of if x in s)
        print("  %-28s 键 %4d · 旧 id 残留 %3d · 新 id 命中 %3d/%d"
              % (lab, len(s), len(stale), len(fresh), len(new_of)))
        if stale[:5]:
            print("       旧 id 残样：", [(x, idmap[x]) for x in stale[:5]])


if __name__ == '__main__':
    main()
