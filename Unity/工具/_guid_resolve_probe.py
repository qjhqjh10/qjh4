# -*- coding: utf-8 -*-
"""AssetGUID -> 本地资源 解析探针（只读）。

数据源：
  1) Addressables 目录 JSON:
     C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/com.unity.addressables/catalog_main.json
     结构（ContentCatalogData，经典三段式）：
       m_InternalIds       : list[str]
       m_KeyDataString     : base64 -> [int32 keyCount] then每项 [byte type=0][int32 len][utf8] ...
       m_EntryDataString   : base64 -> [int32 entryCount] then每项 7*int32
                             (internalId, providerIndex, dependencies, depHash, ?, primaryKey, resourceType)
                             providerIndex 0 = AssetBundleProvider(束) / 1 = BundledAssetProvider(束内资产)
                             dependencies = 另一条 key 的下标（值为数字串时 = "依赖集"，见 m_BucketDataString）
       m_BucketDataString  : base64 -> [int32 bucketCount] then bucketCount 条记录
                             [int32 dataOffset][int32 entryCount][entryCount*int32 entryIndex]
                             bucket i 对应 keys[i]；给「依赖集 key」就能查出它包含哪几个「束 entry」
  2) 本地束: d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/*.bundle
     束内 AssetBundle 对象的 m_Container = [键 -> PPtr]，键就是目录里的 internalId（= 资产 GUID 的短前缀）
  3) 卡 SO 全字段: d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json

用法:
  python -I -X utf8 _guid_resolve_probe.py survey      # 目录结构 + 各字段覆盖率
  python -I -X utf8 _guid_resolve_probe.py card EC29   # 一张卡的全部 GUID 解析
  python -I -X utf8 _guid_resolve_probe.py g <guid>    # 单个 GUID
  python -I -X utf8 _guid_resolve_probe.py map         # 产出 guid_map.tsv（唯一产物）

🔴 「退键」的判据（⛔ 别按「容器键短」判 —— 这条踩过）：
  · **退键 ⟺ `containerKey` ≠ 该 GUID 在 catalog 里的 `internalId`**。
    `guid_map.tsv` 的 **`internalId` 列**就是目录给这条 GUID 的容器键，**`exact` 列** =
    `(containerKey == internalId)` ⇒ **`exact=False` 就是退过键**。
    （为什么会有退键：目录的 `internalId` 与束里的容器键对少数资产不一致 —— 例 `db92a11e…`
     目录给 `'d'`、而束里 `'d'` 是另一条 AudioClip，该资产真在 `'db9'` ⇒ 类型核对不过、退到最长前缀键。）
  · ⛔ **「容器键 ≤2 字符 = 退键」不成立**：实测 2 字符键里 **172/201 是非退键**；
    按「≤2 字符」判 = **真阳 30 / 假阳 171 / 假阴 26**。出处 →
    `d:/4/Unity/资料/普查产出_第三十一会话/AI_guid退键.md` §1.2（`AA_A1770.md:177` 那句是**线索不是判据**）。
  · ⚠️ **2026-10-15 补**：本探针原先**只算了 `exact` 没落盘** ⇒ 下游（`gen_card_vfx_by_card.py` 等）
    拿着 TSV 也**看不出退没退键**。现已把 `internalId` / `exact` 两列写进 TSV ⇒
    **重建时（`map`）必须让这两列一起落**，⛔ 别再退回 9 列（账见 `AI_guid退键.md` §倾向·2）。
"""
import sys, os, io, json, base64, struct, collections

CAT = r'C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/com.unity.addressables/catalog_main.json'
CARDS = r'd:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json'
BUNDLES = r'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
OUTDIR = r'd:/4/Unity/资料/普查产出_第二十七会话'

GUID_FIELDS = [
    'cardImageReference', 'summonAudio', 'attackAudio', 'deathAudio',
    'customRangedAttackParticles', 'customDeathParticles', 'remnantBodyPrefab',
    'whileInPlayAnim', 'targetedSpellAnim', 'nonTargetedSpellAnim', 'summonAnimDelayed',
    'attackAnim',
]


# ---------------------------------------------------------------- catalog
def load_catalog(path=CAT):
    d = json.load(io.open(path, encoding='utf-8'))
    kb = base64.b64decode(d['m_KeyDataString'])
    n = struct.unpack_from('<i', kb, 0)[0]
    keys, o = [], 4
    for _ in range(n):
        t = kb[o]; o += 1
        if t == 0:
            l = struct.unpack_from('<i', kb, o)[0]; o += 4
            keys.append(kb[o:o + l].decode('utf-8')); o += l
        else:
            raise RuntimeError('unknown key type %d' % t)
    eb = base64.b64decode(d['m_EntryDataString'])
    ne = struct.unpack_from('<i', eb, 0)[0]
    ents = [struct.unpack_from('<7i', eb, 4 + 28 * i) for i in range(ne)]
    bb = base64.b64decode(d['m_BucketDataString'])
    ni = (len(bb) - 4) // 4
    ints = struct.unpack_from('<%di' % ni, bb, 4)
    buckets, i = [], 0
    while i < len(ints):
        off, cnt = ints[i], ints[i + 1]
        buckets.append(ints[i + 2:i + 2 + cnt]); i += 2 + cnt
    return dict(keys=keys, ents=ents, ids=d['m_InternalIds'],
                rtypes=d['m_resourceTypes'], buckets=buckets)


def key_index(c, s):
    try:
        return c['keys'].index(s)
    except ValueError:
        return None


def bundle_files_of_entry(c, ei):
    """束 entry -> 束名（m_InternalIds 里的路径）"""
    e = c['ents'][ei]
    return c['ids'][e[0]]


def resolve_entry(c, ei):
    """一个 entry -> dict(internalId, rtypes, bundleFiles[])"""
    e = c['ents'][ei]
    iid = c['ids'][e[0]]
    # 1) internalId 本身是束路径 -> 就是个束 entry
    if iid.startswith('{') and iid.endswith('.bundle'):
        return dict(iid='', bundles=[iid], rtypes=[], entry=ei)
    # 2) 依赖集：dependencies 指向的 key 若是数字串 -> 用 bucket 展开成束 entry
    bundles = []
    depkey = c['keys'][e[2]] if 0 <= e[2] < len(c['keys']) else None
    if isinstance(depkey, str) and depkey.lstrip('-').isdigit():
        for be in c['buckets'][e[2]]:
            bundles.append(bundle_files_of_entry(c, be))
    elif isinstance(depkey, str) and depkey.endswith('.bundle'):
        bundles.append(depkey)
    rt = c['rtypes'][e[6]] if e[6] < len(c['rtypes']) else None
    rtn = rt.get('m_ClassName') if isinstance(rt, dict) else str(rt)
    return dict(iid=iid, bundles=bundles, rtypes=[rtn], entry=ei)


def resolve_guid(c, ki):
    out = []
    for ei in c['buckets'][ki]:
        out.append(resolve_entry(c, ei))
    return out


# ---------------------------------------------------------------- local bundles
_LOCAL = {}
_LOCAL_FILES = None


def local_bundle_path(name_with_hash):
    """目录里的束名（带哈希 / 带 {RuntimePath} 前缀）-> 本地文件

    ⚠️ 目录 JSON 的 m_InternalIds 里有**已知的字符串损坏**（实测
    'chaosspacemarinesemperirschildrencardanims_assets_all.bundle'，
    而 catalog_main.bin / 本地束名都是 '...emperorschildren...'）⇒ 精确匹配失败时用 difflib 兜底。
    """
    global _LOCAL_FILES
    if _LOCAL_FILES is None:
        _LOCAL_FILES = [f for f in os.listdir(BUNDLES) if f.endswith('.bundle')]
    b = os.path.basename(name_with_hash.replace('\\', '/'))
    if b.endswith('.bundle') and len(b.split('_')[-1]) == 39:      # xxx_<32hex>.bundle
        b = b.rsplit('_', 1)[0] + '.bundle'
    p = os.path.join(BUNDLES, b)
    if os.path.exists(p):
        return p
    import difflib
    m = difflib.get_close_matches(b, _LOCAL_FILES, n=1, cutoff=0.85)
    return os.path.join(BUNDLES, m[0]) if m else None


_SCRIPTS = None


def script_class(pathid):
    """monoscripts 束: MonoScript PathID -> 类名"""
    global _SCRIPTS
    if _SCRIPTS is None:
        import UnityPy
        p = os.path.join(BUNDLES, 'Waprforge_monoscripts.bundle')
        _SCRIPTS = {}
        if os.path.exists(p):
            for o in UnityPy.load(p).objects:
                if o.type.name == 'MonoScript':
                    try:
                        _SCRIPTS[o.path_id] = o.read_typetree().get('m_ClassName')
                    except Exception:
                        pass
    return _SCRIPTS.get(pathid)


def load_bundle(path):
    if path not in _LOCAL:
        import UnityPy
        e = UnityPy.load(path)
        cont = collections.defaultdict(set)
        for o in e.objects:
            if o.type.name == 'AssetBundle':
                for k, v in o.read_typetree()['m_Container']:
                    cont[k].add(v['asset']['m_PathID'])
        names = {}
        for o in e.objects:
            if o.type.name == 'AssetBundle':
                continue
            try:
                t = o.read_typetree()
            except Exception:
                continue
            if isinstance(t, dict) and 'm_Name' in t:
                nm = t['m_Name']
                cls = None
                if o.type.name == 'MonoBehaviour' and isinstance(t.get('m_Script'), dict):
                    cls = script_class(t['m_Script'].get('m_PathID'))
                names[o.path_id] = (o.type.name, nm, cls)
        _LOCAL[path] = (cont, names)
    return _LOCAL[path]


def lookup(c, guid):
    """GUID -> [(bundle, containerKey, type, className, name)]（只留类型对得上的）

    `internalId` = 目录（catalog）给这条 GUID 的容器键；`exact = (containerKey == internalId)`
    ⇒ **`exact=False` ⟺ 退过键**（见文件头「退键的判据」，⛔ 别按容器键长度判）。
    """
    ki = key_index(c, guid)
    if ki is None:
        return []
    hits = []
    for r in resolve_guid(c, ki):
        exp = set(x for x in r['rtypes'] if x)
        for bf in r['bundles']:
            p = local_bundle_path(bf)
            if not p:
                hits.append(dict(bundle=os.path.basename(bf), key=r['iid'], iid=r['iid'],
                                 type=None, name=None,
                                 cls=None, note='本地无此束'))
                continue
            cont, names = load_bundle(p)
            # 候选键：束内所有「GUID 的前缀」键（同一束里 'd' 可能指别的资产、'db9' 才是目标）
            ks = sorted([k for k in cont if guid.startswith(k)], key=lambda k: (k != r['iid'], -len(k)))
            if not ks:
                hits.append(dict(bundle=os.path.basename(p), key=r['iid'], iid=r['iid'],
                                 type=None, name=None,
                                 cls=None, note='束里无匹配键'))
                continue
            for k in ks:
                got = []
                for pid in cont[k]:
                    ty, nm, cls = names.get(pid, (None, None, None))
                    if exp:
                        # 类型核对：MonoBehaviour 比 m_ClassName，其余比 Unity 类型名
                        # 目录里的 m_ClassName 带命名空间（UnityEngine.Texture2D），UnityPy 的 type.name 不带
                        if cls is not None:
                            ok = cls in exp
                        elif ty is not None:
                            ok = ty in exp or ('UnityEngine.' + ty) in exp
                        else:
                            ok = False
                        if not ok:
                            continue
                    # `exact` = 用的键就是目录给的 internalId ⇒ 没退键（`iid` 一并带出，供 TSV 落盘）
                    got.append(dict(bundle=os.path.basename(p), key=k, iid=r['iid'], type=ty, name=nm,
                                    cls=cls, pathid=pid, exact=(k == r['iid'])))
                hits.extend(got)
                if got and exp:
                    break          # 已经有一个键能给出符合类型的对象，就不再往短键退
                if got and not exp and k == r['iid']:
                    break
    return hits


def card_field_guids(card):
    """字段 -> GUID；空的 AssetReference 记 ''，PPtr 记 'PPTR:a/b'
    走整张卡（含 cardAbilities[].abilityLogic[].cardAnimations[] 这类嵌套引用）。"""
    res = collections.OrderedDict()

    def walk(o, p):
        if isinstance(o, dict):
            if 'm_AssetGUID' in o:
                res.setdefault(p, o.get('m_AssetGUID') or '')
            elif 'm_PathID' in o and set(o) <= {'m_FileID', 'm_PathID'}:
                res.setdefault(p, 'PPTR:%s/%s' % (o.get('m_FileID'), o.get('m_PathID')))
            else:
                for k, v in o.items():
                    if isinstance(v, (dict, list)):
                        walk(v, p + '.' + k if p else k)
        elif isinstance(o, list):
            for i, v in enumerate(o):
                walk(v, p + '[%d]' % i)
    for f in GUID_FIELDS:
        v = card.get(f)
        if isinstance(v, dict):
            if 'm_AssetGUID' in v:
                res[f] = v.get('m_AssetGUID') or ''
            elif 'm_PathID' in v:
                res[f] = 'PPTR:%s/%s' % (v.get('m_FileID'), v.get('m_PathID'))
    for f in ('cardAbilities', 'activeAbility', 'alternateArts'):
        if card.get(f):
            walk(card[f], f)
    return res


def main():
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'survey'
    c = load_catalog()
    print('keys=%d entries=%d buckets=%d internalIds=%d' %
          (len(c['keys']), len(c['ents']), len(c['buckets']), len(c['ids'])))
    if cmd == 'g':
        for h in lookup(c, sys.argv[2]):
            print(h)
    elif cmd == 'card':
        cards = json.load(io.open(CARDS, encoding='utf-8'))
        key = sys.argv[2]
        for card in cards:
            if key in (card.get('uniqueId'), card.get('cardName'), card.get('m_Name')):
                print('CARD', card.get('uniqueId'), card.get('cardName'), card.get('m_Name'))
                for f, g in card_field_guids(card).items():
                    print('  %-28s %s' % (f, g))
                    if not g or g.startswith('PPTR'):
                        continue
                    for h in lookup(c, g):
                        print('      -> [%s] key=%s %s %s %s%s' % (
                            h['bundle'], h['key'], h['type'] or '', h.get('cls') or '',
                            h['name'] or '', '  ⚠' + h['note'] if h.get('note') else ''))
                break
    elif cmd == 'survey':
        cards = json.load(io.open(CARDS, encoding='utf-8'))
        dg = collections.defaultdict(set)
        ncard = collections.Counter()
        npptr = collections.Counter()
        keyset = set(c['keys'])
        for card in cards:
            for f, g in card_field_guids(card).items():
                if g and g in keyset:
                    dg[f].add(g); ncard[f] += 1
                elif g.startswith('PPTR') and not g.endswith('0/0'):
                    npptr[f] += 1
        print('%-28s %8s %10s %10s %8s' % ('field', 'cards', 'distinct', 'inCatalog', 'pptr!=0'))
        for f in GUID_FIELDS:
            print('%-28s %8d %10d %10d %8d' % (f, ncard[f], len(dg[f]),
                                               sum(1 for g in dg[f] if g in keyset), npptr[f]))
        # 字段取值形态
        forms = collections.defaultdict(collections.Counter)
        for card in cards:
            for f in GUID_FIELDS:
                v = card.get(f)
                if isinstance(v, dict):
                    forms[f]['AssetRef' if 'm_AssetGUID' in v else 'PPtr'] += 1
                    if 'm_AssetGUID' in v and v.get('m_AssetGUID'):
                        forms[f]['非空'] += 1
                    if 'm_PathID' in v and v.get('m_PathID'):
                        forms[f]['PathID!=0'] += 1
        print('--- 取值形态 ---')
        for f in GUID_FIELDS:
            print('%-28s %s' % (f, dict(forms[f])))
    elif cmd == 'map':
        cards = json.load(io.open(CARDS, encoding='utf-8'))
        # GUID -> 名字（缓存）
        gcache = {}
        rows = []
        for card in cards:
            for f, g in card_field_guids(card).items():
                if not g or 'PPTR' in g:
                    continue
                if g not in gcache:
                    gcache[g] = lookup(c, g)
                for h in gcache[g]:
                    if h.get('note'):
                        continue
                    rows.append((card.get('uniqueId'), card.get('cardName'), f, g,
                                 h['bundle'], h['key'], h['type'], h.get('cls'), h['name'],
                                 h.get('iid'), h.get('exact')))
        os.makedirs(OUTDIR, exist_ok=True)
        fp = os.path.join(OUTDIR, 'guid_map.tsv')
        # 🔴 列定义（⛔ 别删 `internalId` / `exact` 两列 —— 2026-10-15 补，判据见文件头「退键的判据」）：
        #   containerKey = 束里实际用的容器键 · internalId = catalog 给这条 GUID 的键
        #   ⇒ **exact = (containerKey == internalId)**；`exact=False` ⟺ **退键**。
        #   原先只有 9 列（没这两列）⇒ 下游看不出退没退键，只能回目录比（账 = AI_guid退键.md）。
        hdr = ('uniqueId\tcardName\tfield\tassetGuid\tbundle\tcontainerKey\ttype\tclassName\tname'
               '\tinternalId\texact\n')
        with io.open(fp, 'w', encoding='utf-8', newline='') as fh:
            fh.write(hdr)
            for r in rows:
                fh.write('\t'.join('' if x is None else str(x) for x in r) + '\n')
        nexact = sum(1 for r in rows if r[-1] is False)
        print('wrote', fp, len(rows), 'rows;', len(gcache), 'distinct guids;',
              '退键(exact=False)', nexact, '行')


if __name__ == '__main__':
    main()
