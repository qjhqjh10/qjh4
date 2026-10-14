# -*- coding: utf-8 -*-
"""P1（A1782/A1778/A1783/A1800）—— 算出「我们卡池里该换成哪个真值 id」的逐张判据表（只读）。

级联 join（判据 → 资料/普查产出_第三十会话/乙档六笔_判据查证.md §A1800 + 施工设计 §二·3）：
  ① 官方本地化 `Card_Name/*` 的英文值 → 它的键（键 = 真值 `uniqueId`）
  ② 真值 `m_Name` 尾巴（最后一个 `-` 之后那段）
  ③ 真值 `cardName` / `card_stats.json` 的 `ocrName` 归一化
取英文值时：**先用我们的 `name`，查不到再退 `ocrName`**。
消歧：短 id 按阵营前缀过滤；GUID 多个候选时取「`m_Name` 尾巴 == `cardName`」的那一个（基准卡），
      仍不唯一 ⇒ **不猜**、列进报告。

用法：python -I -X utf8 d:/4/Unity/工具/_oneoff_1014_p1/build_picks.py
"""
import io, json, re, collections, os, csv

ROOT = "d:/4"
POOL  = ROOT + "/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
IDS   = ROOT + "/Unity/数据/游戏数据/card_ids.json"
STATS = ROOT + "/Unity/数据/游戏数据/card_stats.json"
TRUTH = "d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json"
OFF   = ROOT + "/Unity/数据/游戏数据/官方本地化_en+zh-CN.json"
GUIDMAP = ROOT + "/Unity/资料/普查产出_第二十七会话/guid_map.tsv"
OUT   = ROOT + "/_tmp_view/p1_1014/picks.tsv"


def n(s):
    return re.sub(r'[^a-z0-9]', '', (s or '').lower())


PREFIX = {"AM": "AstraMilitarum", "ASH": "SaimHann", "BL": "BlackLegion", "DA": "DarkAngels",
          "EC": "EmperorsChildren", "GSC": "Genestealers", "GOF": "Goff", "TL": "Leviathan",
          "SAU": "Sautekh", "SOR": "Sororitas", "SW": "SpaceWolves", "TAU": "TauEmpire",
          "UM": "Ultramarines"}
P2F = {v: k for k, v in PREFIX.items()}
GUID = re.compile(r'^[0-9a-f]{32}$')


def tailof(mn):
    return (mn or '').rsplit('-', 1)[-1]


def main():
    pool  = json.load(io.open(POOL, 'r', encoding='utf-8'))['cards']
    mp    = json.load(io.open(IDS, 'r', encoding='utf-8'))['mapping']
    stats = json.load(io.open(STATS, 'r', encoding='utf-8'))['cards']
    truth = json.load(io.open(TRUTH, 'r', encoding='utf-8'))
    off   = json.load(io.open(OFF, 'r', encoding='utf-8'))['terms']

    trow = {}
    for t in truth:
        u = t.get('uniqueId') or ''
        if u:
            trow.setdefault(u, t)
    tname = collections.defaultdict(list)
    ttail = collections.defaultdict(list)
    for u, t in trow.items():
        tname[n(t.get('cardName'))].append(u)
        mn = t.get('m_Name') or ''
        if '-' in mn:
            ttail[n(tailof(mn))].append(u)

    en2k = collections.defaultdict(list)
    offhit = offmiss = 0
    for k, v in off.items():
        if not k.startswith('Card_Name/'):
            continue
        key = k.split('/', 1)[1]
        if key not in trow:
            offmiss += 1
            continue
        offhit += 1
        en2k[n(v[0])].append(key)

    st = collections.defaultdict(list)
    for s in stats:
        st[n(s.get('name'))].append(s)

    sprite = {}
    with io.open(GUIDMAP, 'r', encoding='utf-8') as f:
        for r in csv.DictReader(f, delimiter='\t'):
            if r['field'] == 'cardImageReference':
                sprite.setdefault(r['uniqueId'], set()).add(r['name'])
    artname = {}
    for s in stats:
        a = s.get('art') or ''
        if a:
            artname.setdefault(n(s.get('name')), set()).add(os.path.basename(a)[:-4])

    def base_key(cands):
        ex = [c for c in cands
              if n(tailof(trow[c].get('m_Name'))) == n(trow[c].get('cardName'))]
        if len(ex) == 1:
            return ex[0], 'tail==name'
        if len(cands) == 1:
            return cands[0], 'single'
        return None, 'ambiguous(%d)' % len(cands)

    idset = set(mp)
    selfmade = [c for c in pool if c['id'] not in idset]
    rows = []
    for c in selfmade:
        nm, fac = c['name'], c['faction']
        cand = st.get(n(nm), [])
        ocr = None
        for s in cand:
            if s.get('faction') == fac:
                ocr = s.get('ocrName')
                break
        if ocr is None and cand:
            ocr = cand[0].get('ocrName')
        pool_c = []
        srcs = []
        for tag, val in (('name', nm), ('ocr', ocr)):
            if not val:
                continue
            hits = en2k.get(n(val), [])
            if hits:
                pool_c += hits
                srcs.append('off:%s(%d)' % (tag, len(hits)))
        for tag, val in (('name', nm), ('ocr', ocr)):
            if val and n(val) in ttail:
                pool_c += ttail[n(val)]
                srcs.append('tail:%s' % tag)
        for tag, val in (('name', nm), ('ocr', ocr)):
            if val and n(val) in tname:
                pool_c += tname[n(val)]
                srcs.append('truth:%s' % tag)
        pool_c = sorted(set(pool_c))
        pre = P2F.get(fac)
        sh = [x for x in pool_c if pre and x.startswith(pre) and x[len(pre):].isdigit()]
        gd = [x for x in pool_c if GUID.match(x)]
        pick, how = None, 'none'
        if len(sh) == 1:
            pick, how = sh[0], 'short'
        elif gd:
            pick, why = base_key(gd)
            how = 'guid/' + str(why)
        elif len(sh) > 1:
            pick, why = base_key(sh)
            how = 'short-multi/' + str(why)
        sk = ''
        if pick:
            a, b = sprite.get(pick, set()), artname.get(n(nm), set())
            sk = ('OK' if (a & b) else 'DIFF') if (a and b) else 'n/a'
        rows.append({'pool_id': c['id'], 'name': nm, 'faction': fac, 'ocr': ocr or '',
                     'cands': ','.join(pool_c), 'pick': pick or '', 'how': how, 'sprite': sk,
                     'srcs': ';'.join(srcs)})

    tnames = set(n(t.get('cardName')) for t in truth)
    legacy_collide = [k for k in mp if k not in trow and not k.startswith('#') and n(mp[k]) in tnames]
    extra = []
    for c in pool:
        if c['id'] in legacy_collide:
            hits = sorted(set(en2k.get(n(c['name']), []) + tname.get(n(c['name']), [])))
            pre = P2F.get(c['faction'])
            sh = [x for x in hits if pre and x.startswith(pre) and x[len(pre):].isdigit()]
            gd = [x for x in hits if GUID.match(x)]
            pick, how = None, 'none'
            if len(sh) == 1:
                pick, how = sh[0], 'short/collide'
            elif gd:
                pick, why = base_key(gd)
                how = 'guid/collide/' + str(why)
            elif len(sh) > 1:
                pick, why = base_key(sh)
                how = 'short-multi/collide/' + str(why)
            extra.append({'pool_id': c['id'], 'name': c['name'], 'faction': c['faction'], 'ocr': '',
                          'cands': ','.join(hits), 'pick': pick or '', 'how': how, 'sprite': '',
                          'srcs': 'legacy-collide'})

    with io.open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write('pool_id\tname\tfaction\tocr\tcands\tpick\thow\tsprite\tsrcs\n')
        for r in rows + extra:
            f.write('\t'.join(r[k] for k in ('pool_id', 'name', 'faction', 'ocr', 'cands', 'pick', 'how', 'sprite', 'srcs')) + '\n')

    cnt = collections.Counter(r['how'].split('/')[0] for r in rows)
    print("① 自造 id 卡 %d 张：%s" % (len(rows), dict(cnt)))
    print("   其中 pick 为空：", [(r['pool_id'], r['name'], r['how']) for r in rows if not r['pick']])
    print("   精灵名第二钥匙：", dict(collections.Counter(r['sprite'] for r in rows if r['pick'])))
    print("   sprite=DIFF：", [(r['pool_id'], r['name'], r['pick']) for r in rows if r['sprite'] == 'DIFF'])
    print("② 撞名 legacy 持有的池内卡 %d 张：%s" % (len(extra), dict(collections.Counter(r['how'] for r in extra))))
    print("   ", [(r['pool_id'], r['name'], r['pick'], r['how']) for r in extra])
    print("官方表键：命中真值 %d / 不在真值 %d" % (offhit, offmiss))
    print("→ %s" % OUT)


if __name__ == '__main__':
    main()
