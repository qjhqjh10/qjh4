#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
从「卡面逐张提取」的结果里**算出**要修 `cards_engine.json` 的条目 →
`Unity/数据/游戏数据/cardface_fixes.json`（由 `gen_cards_engine.py` 读取）。

为什么这么做（用户 2026-09-13 要求）：
  卡图上的「效果文字下面那行橙色小字」= 这张卡的**字段/归类**（Infantry / Vehicle / Daemon /
  Defence / Dark Pact …）。`card_stats.json` 里这一列**串进了「卡的类型」**（Unit / Troop / Soldier），
  于是 `选/造/给一张 <字段> 卡` 全部筛错卡 —— 例如 `Hunting Wolf` 是**兽**不是兵。
  另有**关键词数值被吃掉**（卡面 `Armour 1` → 我们的 `['Armour']`）与**关键词整个缺失**。

产出只放**证据确凿**的条目：
  · `subtype`：只改「卡面明确印了那行橙字」的卡（印的是 `—`/没印 → 不动，宁缺毋滥）
  · `keywords`：只改「效果动词**之前**的声明区」里出现的关键词 ——
    效果里给别人加的那种（`adjacent units have armour 1`）**不算**，那是效果不是关键词
"""
import io, os, re, json, sys, collections

# ⚠️ Windows 控制台默认 GBK，而本脚本会打 `✅` / `🔴`（`A1633` 那条判据起）—— 不转 UTF-8 会直接抛
#    UnicodeEncodeError，而且是在**算完之后**抛，看着像「脚本坏了」。照 `restore_card_icons.py`
#    / `gen_icon_doc.py` / `gen_icon_plan.py` 那三份的同一句写（本仓 python 3.14）。
try:
    sys.stdout.reconfigure(encoding='utf-8')
except Exception:
    pass

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
BASE = ROOT
NEW  = os.path.join(BASE, 'Unity', '资料', '卡表核对_卡图提取', '_合并总表.md')
# ⚠️⚠️ **基准必须是「原始源表」，不能是生成物**（2026-09-13 踩过，写在这儿免得再踩）：
#   第一版这里指的 `cards_engine.json` —— 那是 `gen_cards_engine.py` 的**产物**，
#   已经吃过了本脚本上一轮算出来的修正。于是第二次跑时「差异」只剩 3 条，
#   写出来的修正表就把前一批 37+59 条**覆盖掉了** → 卡表跟着退回原样。
#   ⇒ 算修正**永远和 `card_stats.json`（OCR 原始源表）比**，本脚本才是幂等的。
OLD  = os.path.join(BASE, 'Unity', '数据', '游戏数据', 'card_stats.json')
OUT  = os.path.join(BASE, 'Unity', '数据', '游戏数据', 'cardface_fixes.json')
# 🆕 2026-10-12（`A1633` 的下游）：卡池 —— **只用来数「一个卡名在全池出现几次」**。
#    ⛔ **不拿它当算修正的基准** —— 基准永远是上面 `OLD` 那份**原始源表**（见那一大段警告），
#    本脚本的幂等性就靠这一条。
POOL = os.path.join(BASE, 'Unity', 'MyGame', 'Assets', 'RuleEngine', 'Resources',
                    'cards_engine.json')

# 命令行开关。本脚本原来只会「算完就写」，**没有干跑** —— 而它一跑就会**整列重写**
# `subtype` / `keywords`（手工段只保留 `_` 开头与 `desc`/`descZh`）⇒ 改它之前
# 必须能先看见「会改什么」，否则只能靠 `git diff` 事后回滚。
CHECK = '--check' in sys.argv                  # 干跑：算完打出来、**一个字节都不写**
LEGACY_BARE_KEYS = '--legacy-bare-keys' in sys.argv   # 🔴 **只为现场演示那条新判据「能红」**

# 引擎真的会按**数值**用的关键词（`UnitState.KwValue` 那一族）。
# 只收这些 —— 别的关键词带不带值不影响结算，别去动它。
NUMERIC = ['armour', 'armor', 'blast', 'shuriken', 'vulnerable', 'regeneration',
           'sentry', 'tide', 'vent', 'synapse', 'stomp', 'hunt mark', 'mob']

# 「效果动词」——声明区到这里为止。**必须包含 have/has/gain/give**：
#   `adjacent units **have** armour 1` / `friendly vehicles **have** regeneration 1`
#   是**效果**（给别人加），不是这张卡的关键词。不挡住就会把效果当关键词写进卡表。
VERBS = (r'\b(deal|gain|give|draw|heal|destroy|create|deploy|return|choose|lower|increase|reduce'
         r'|cannot|can t|duty|rally|strike|slay|backlash|talent|teleport|reanimate|put|add|take'
         r'|apply|move|look|repeat|for each|when|if|at the|each|your|this|have|has|had|gets?)\b')

# 卡面那行橙字 → 我们卡表里 `subtype` 的规范写法
SUBTYPE_CANON = {
    'infantry': 'Infantry', 'vehicle': 'Vehicle', 'drone': 'Drone', 'beast': 'Beast',
    'monster': 'Monster', 'daemon': 'Daemon', 'battlesuit': 'Battlesuit',
    'structure': 'Structure', 'character': 'Character', 'swarm': 'Swarm',
    'defence': 'Defence', 'dark pact': 'Dark Pact', 'overlord power': 'Overlord Power',
    'psychic power': 'Psychic Power', 'combat elixir': 'Combat Elixir',
    'invocation': 'Invocation', 'genomic enhancement': 'Genomic Enhancement',
    'codicil': 'Codicil', 'rune': 'Rune', 'secret': 'Secret', 'sabotage': 'Sabotage',
    'elixir': 'Elixir', 'warlord': 'Warlord',
}


# 卡名对不上：**我们的卡名 → 卡面上印的卡名**。
# 只有三张，全是**我们这边写错/写少/写多**，卡面才是准的（各配一处证据）：
#   · `Lord Kakophonist` 卡面是 `Varius, Lord Kakophonist`（全名带前缀）
#   · `Threnodic Choir Noise Marine` 卡面是 `Threnodic Noise Marine`（我们多了一个 Choir）
#   · `Sisters Repentia` 卡面是 `Sister Repentia`（我们写成了复数）
# 不建这张表的话，这三张的字段修不到 —— 自检里那条「不许有单位卡的 subtype 是类型词」会当场炸。
NAME_ALIAS = {
    "Lord Kakophonist": "Varius, Lord Kakophonist",
    "Threnodic Choir Noise Marine": "Threnodic Noise Marine",
    "Sisters Repentia": "Sister Repentia",
}
ALIAS_REV = {}

def norm(s):
    return re.sub(r'[^a-z0-9]+', '', (s or '').lower())


def strip_icons(s):
    s = re.sub(r'【[^】]*】', ' ', s or '')
    s = re.sub(r'\[[^\]]*\]', ' ', s)
    return ' '.join(re.sub(r"[^a-z0-9' ]+", ' ', s.lower()).split())


def read_new():
    rows = []
    for line in io.open(NEW, encoding='utf-8'):
        if not line.startswith('| ') or '文件名' in line:
            continue
        p = [c.strip() for c in line.rstrip('\n').strip('|').split('|')]
        if len(p) < 12:
            continue
        rows.append({'file': p[0], 'name': p[1], 'subtype': p[7], 'type': p[8], 'desc': p[10]})
    return rows


def load_name_count():
    """卡池里每个**卡名**出现几次 —— 判「裸卡名键能不能用」。

    为什么需要（`A1633`，2026-10-12）：`gen_cards_engine.py` 的 `load_cardface_fixes`
    把**不带 `/` 的键**按**卡名**查 ⇒ 一个裸键是**给所有同名卡**用的。而卡池里本来就有
    **同名跨阵营**的卡（现读 3 组：`Terminator` / `Terminator Champion` / `Maulerfiend`，
    各 2 张）⇒ 裸键只可能承载**其中一张**的值，另一张**静默套上别人那张的效果**
    （卡面不打 `*`、日志不报、脚本照样打印「命中」）。

    ⇒ 判据：**名字不唯一时，键必须写成 `阵营/卡名`**（`工具/_apply_kw_misextract.py:127`
    早就是这个写法，照它抄）。

    **口径 = 卡池**（`cards_engine.json` 的 `name` 精确计数）—— 与
    `gen_cards_engine.py` 那条「裸卡名键的唯一性」判据、以及
    `工具/_apply_kw_misextract.py:102-104` 的同一个计数**同源**（三处必须是同一把尺子，
    否则「本脚本写出来的键」会与「那条判据认的键」对不上）。
    ⚠️ 卡池读不到时退回**按源表数**（保守：可能多切几条 ⇒ 切了也不会错，只是多余），
    **并如实出声** —— ⛔ 不假装还是卡池口径。

    → `(count_of, 口径说明)`；`count_of(name)` 判的是**卡名**（不是归一化名）。
    """
    if os.path.exists(POOL):
        try:
            cards = json.load(io.open(POOL, encoding='utf-8'))['cards']
        except Exception as e:                      # noqa: BLE001 —— 读不到就退回，别炸
            print('⚠️ 读卡池失败（%s）—— 退回按源表数（保守口径）' % e)
        else:
            c = collections.Counter(x.get('name') or '' for x in cards)
            return (lambda n: c.get(n, 0)), \
               '卡池 %d 张 / %d 个名字' % (len(cards), len(c))
    else:
        print('⚠️ 找不到卡池 %s —— 退回按源表数（保守口径：可能多切几条）' % POOL)
    return None, ''


def load_src_index(rawcards):
    """源表 `ocrSrc` 的**基名** → 那一条记录；**基名不唯一的不收**（收进去就是猜）。

    用途：卡名不唯一时，把 `_合并总表.md` 的**一行**定位到**它自己那张卡**。
    为什么这个主键才对：`_合并总表.md` 的第一列就是 **PnP 文件名**，而源表 `ocrSrc`
    存的是同一张 PnP 图的完整路径（本仓口径：**主键是 PnP 文件名 / `ocrSrc`，⛔ 不是卡名**）。
    ⚠️ 只用它**定位**，⛔ 不用它**算修正** —— 那会改变「哪些行匹配得上」的口径，
    把一大批本来按卡名匹配的行漏掉（实测：合并总表 1127 行里有 9 行的基名在源表里根本没有）。
    """
    seen = collections.defaultdict(list)
    for c in rawcards:
        s = (c.get('ocrSrc') or '').strip()
        if s:
            seen[os.path.basename(s.replace('\\', '/'))].append(c)
    return {b: v[0] for b, v in seen.items() if len(v) == 1}


def main():
    new = read_new()
    # 源表是 `card_stats.json`（`{"cards":[…]}`），字段名 `subtype` / `keywords` 与生成物一致。
    # 卡名有前导空格（` Iron Priest`）—— 归一化后当键，别让它对不上。
    raw = json.load(io.open(OLD, encoding='utf-8'))
    rawcards = raw['cards'] if isinstance(raw, dict) else raw
    old = {}
    for c in rawcards:
        k = norm((c.get('name') or '').strip())
        if k and k not in old:
            old[k] = c
    for our, face in NAME_ALIAS.items():
        ALIAS_REV[norm(face)] = our

    # 🆕 2026-10-12（`A1633` 的下游）：两张小表 —— 「名字够不够唯一」与「按 PnP 图定位那一行」。
    _count_of, _cnt_src = load_name_count()
    if _count_of is None:
        _sn = collections.Counter(norm(c.get('name')) for c in rawcards)
        _count_of = lambda n: _sn.get(norm(n), 0)       # noqa: E731
        _cnt_src = '源表（归一化名）'
    by_src = load_src_index(rawcards)
    unresolved = []            # 名字不唯一、又定位不到它自己那张卡的行（**不许猜**）

    fixes = {}
    n_sub = n_kw = 0
    skipped_grant = 0

    for r in new:
        ourname = ALIAS_REV.get(norm(r['name']), r['name'])
        o = old.get(norm(ourname))
        if not o:
            continue
        # 键要用**生成器那一侧的卡名**（`gen_cards_engine.py` 里 `norm_str(name).strip()`
        # 再把连续空白并成一个空格）—— 源表里有 ` Iron Priest` 这种前导空格的。
        name = re.sub(r'\s+', ' ', (o.get('name') or '').strip())
        if not name:
            continue
        # 🔴 **键的形状**（2026-10-12，`A1633` 的下游）—— 原来这里**一律按裸卡名**算
        #    （`fixes.setdefault(name, …)`，同名卡**先到先得**）⇒ 一重跑就会：
        #      ① 把「同名跨阵营」那张卡的**裸键**写回来（值只可能是其中一张的）；
        #      ② 把上一批**手写的 `阵营/卡名` 键整列删掉**（本列由本脚本整列重写，
        #         `keep` 只保留 `_` 开头与 `desc`/`descZh`）。
        #    ⇒ 名字不唯一时改写成 `阵营/卡名`；**并且**用 `ocrSrc` 基名把这一行定位到
        #    **它自己那张卡**（否则「阵营」与「值」会来自不同的卡 —— 比裸键更糟）。
        key = name
        if _count_of(name) > 1 and not LEGACY_BARE_KEYS:
            _ent = by_src.get(os.path.basename((r['file'] or '').replace('\\', '/')))
            _fac = str((_ent or {}).get('faction') or '').strip()
            if _ent is not None and _fac and norm(_ent.get('name')) == norm(name):
                o = _ent                       # ← 值也换成**它自己那张卡**的
                name = re.sub(r'\s+', ' ', (o.get('name') or '').strip())
                key = _fac + '/' + name
            else:
                # ⛔ 定位不到就**不许猜**：留着裸键，让下面那条判据当场报红、退出码非 0、不落盘。
                unresolved.append((r['file'], name,
                                   'ocrSrc 基名在源表里找不到、或不唯一、或名字对不上'))
        ent = fixes.setdefault(key, {})

        # ---- ① 字段（卡面那行橙字）----
        cardface = (r['subtype'] or '').strip()
        if cardface and cardface != '—':
            canon = SUBTYPE_CANON.get(cardface.lower())
            if canon and norm(canon) != norm(o.get('subtype') or ''):
                ent['subtype'] = canon
                n_sub += 1

        # ---- ② 关键词：只在**声明区**（效果动词之前）找 ----
        b = strip_icons(r['desc'])
        m = re.search(VERBS, b)
        decl = b[:m.start()] if m else b
        # 效果区（声明区之后）里出现的关键词 = 效果加的，不算
        rest = b[m.start():] if m else ''

        kws = [str(x).strip() for x in (o.get('keywords') or [])]
        newkws = list(kws)
        changed = False
        for kw in NUMERIC:
            for mm in re.finditer(r'\b' + re.escape(kw) + r'\s+(\d+)', decl):
                val = mm.group(1)
                # 效果区里也出现同样的 `kw val` → 这多半是「给别人的」，不写进关键词
                if re.search(r'\b' + re.escape(kw) + r'\s+' + val + r'\b', rest):
                    skipped_grant += 1
                    continue
                idx = next((i for i, x in enumerate(newkws) if kw in x.lower()), -1)
                if idx < 0:
                    newkws.append(kw.title() + ' ' + val)
                    changed = True
                elif not re.search(r'\b' + re.escape(kw) + r'\s+' + val + r'\b', newkws[idx].lower()):
                    newkws[idx] = newkws[idx] + ' ' + val   # `Armour` → `Armour 1`
                    changed = True
        if changed:
            ent['keywords'] = newkws
            n_kw += 1

        if not ent:
            fixes.pop(key, None)      # ⚠️ 键改成 `key`（与上面 `setdefault` 同一个键）——
            # 原来写 `name`：名字不唯一时 `key` 是 `阵营/卡名`，pop 裸名会**清错一条**。

    # 🔴 **手工段一律保留**（2026-09-13 A3 修）：本脚本只会**算** `subtype` / `keywords` 两列，
    #    而这张表里还有**手工维护**的三类东西 —— `desc`（49 条效果文字修正，第三十三轮加）、
    #    以及 `_manual_*_note` 说明。原来这里直接整文件重写 ⇒ **一跑就把它们全抹掉**，
    #    而卡表重建流程里正有「重跑 gen_cardface_fixes.py」这一步（见 `项目任务.md` 第八节）——
    #    也就是说下一个人照着流程做就会**静默丢掉 49 条效果文字修正**。
    #    ⇒ 先读旧文件、把这些键原样带过去（**顺序也照旧**，人读的说明放在最前）。
    def _reorder_dict(cur, order):
        """把 `cur` 的条目按 `order`（盘上那份的键序）排；`order` 里没有的追加在末尾。

        为什么（2026-10-12，`A1633` 同批加固）：`subtype` / `keywords` 是**整列重算**的，
        而人手工改过键名 ⇒ 新算出来的插入序与盘上那份不同。不还原的话，**每次重跑都会让
        `git diff` 冒出十几行「看着像改了什么」的块移动**（实测 1 条 `keywords` 条目挪位 = 13 行），
        而**值一个都没变** —— 这正是本仓反复栽的那种噪声。还原之后重跑**逐字节幂等**。
        语义上无差别（JSON 对象无序），只影响人读 diff。
        """
        if not isinstance(order, dict) and not isinstance(order, list):
            return cur
        d = {}
        for k in order:
            if k in cur:
                d[k] = cur[k]
        for k in cur:
            if k not in d:
                d[k] = cur[k]
        return d

    keep = {}
    disk_keys = []                     # 盘上那份的**顶层键序**（写盘前按它还原，见文件末尾）
    disk_cols = {'subtype': [], 'keywords': []}   # 同上，两列**内部**的键序
    if os.path.exists(OUT):
        try:
            with io.open(OUT, 'r', encoding='utf-8') as fh:
                old = json.load(fh)
            disk_keys = list(old.keys())
            disk_cols = {c: list((old.get(c) or {}).keys()) for c in ('subtype', 'keywords')}
            # 🔴 **2026-09-16：白名单改成「顶层凡是 `_` 开头的一律带走」** —— 这个坑又踩了一次。
            #    白名单是老写法：每加一个新的说明键（`_2026-09-16_引号丢失` /
            #    `_2026-09-16_AvengingZeal属性` …）都得回来补一行，**忘了补 = 本脚本一跑就把那条
            #    说明静默删掉**。2026-09-16 实测撞到：跑一次把**两条已提交的**说明键抹了，
            #    是 `git diff` 才看出来的（脚本自己的注释里正警告过同一件事，见下面 2026-09-15 那条）。
            #    **判据**：本脚本**只会写 `subtype` / `keywords` 两列**，而那些名字**都不带下划线前缀**
            #    ⇒ 顶层凡是 `_` 开头的，一律是**人写的**，原样带走（`_note` 除外 —— 那个由本脚本写）。
            #    🆕 **2026-10-14（`A1992`）**：`desc` / `descZh` 同样是人写的（由下面那行白名单带走），
            #    而它们**也**有了「手工赢」的覆盖列 `_manual_desc` / `_manual_descZh`（见文件末尾那一支）。
            for k in old:
                if (k.startswith('_') and k != '_note') or k in ('desc', 'descZh'):
                    keep[k] = old[k]
        except Exception as e:
            print("⚠️ 读旧修正表失败（%s）—— **不敢覆盖**，请先处理它" % e)
            return 1
    else:
        # 🔴 **2026-10-12（与 `A1633` 同批加固）**：原来这里**没有 `else`** —— `OUT` 不存在时
        #    `keep` 空着往下走，脚本**照样写出一个新文件**（`desc` / `descZh` /
        #    `_manual_subtype` / `_manual_keywords` / 全部 `_` 说明键**一个都不在**）。
        #    实测（把 `OUT` 指到一个空路径跑一遍）：产出 **6325 字节**，而盘上那份是 **170812** ——
        #    **170 KB 手工维护的数据一声不响就没了**（本仓「不许静默失败」那条）。
        #    ⇒ 与上面那个 `except` 同一条口径：**不敢覆盖**。
        print("🔴 找不到旧修正表 %s —— `desc`/`descZh`/`_manual_*`/全部 `_` 说明键会**全部丢掉**，"
              "⛔ 不敢覆盖。\n"
              "   （确实要从零重来的话：先把那份文件挪走并**手工另存那些手工列**，再重跑。）" % OUT)
        return 1

    out = {'_note': '卡面逐张核对（2026-09-13）算出的修正表。来源：'
                    'Unity/资料/卡表核对_卡图提取/_合并总表.md（1118 张逐张看图抄的）。'
                    '生成脚本：Unity/工具/gen_cardface_fixes.py。由 gen_cards_engine.py 读取。'
                    '⚠️ 本文件里带 `_manual_` 前缀的说明段与 `desc` 列是**手工维护**的 —— '
                    '脚本会原样保留（见脚本里那段注释），别手删。'}
    out.update(keep)          # 手工段
    out['subtype'] = {k: v['subtype'] for k, v in fixes.items() if 'subtype' in v}
    out['keywords'] = {k: v['keywords'] for k, v in fixes.items() if 'keywords' in v}

    # 🔴 **手工覆盖**（2026-09-14 A4 批 4 加）—— 为什么必须有这一层：
    #    上面 `subtype` / `keywords` 两列是**算出来的**（源是 `_合并总表.md`），
    #    而那张表**只覆盖了 5 类卡**（部队 586 / 计策 327 / 天赋 104 / 督军 56 / 防御 39
    #    + 药剂 6 = 1118 行）—— **`6破坏卡` 这一类当年根本没被逐张核对过**。
    #    实测：`Jammed Communications` / `Cult Propaganda` 的 `subtype` 至今是 `Stratagem` / `Spell`，
    #    而**四张破坏卡的卡面橙字都是 `Sabotage`**（`d:/2/Warpforge部队卡片/Genestealer Cult/6破坏卡/`，
    #    2026-09-14 逐张开图读过）。没有源表行可改 ⇒ 只能在这里手工覆盖。
    #    ⚠️ **原来手工加进 `subtype` 列会被下一次重跑静默抹掉**（那正是本文件 160 行那段注释抱怨的坑），
    #       所以覆盖必须走**单独一列**、由脚本原样保留并**最后应用**（手工赢过算出来的）。
    # 🔴 **2026-10-14（`A1992`）：`desc` / `descZh` 也要有「手工覆盖列」** ——
    #    本脚本今天**只算** `subtype` / `keywords` 两列，`desc` / `descZh` 是**手工维护**的
    #    （靠 `keep` 原样带走，见上面那一大段）。但「带走」**不等于「手工赢」**：
    #    一旦将来有人给本脚本加一条「从 `_合并总表.md` / `卡牌完整信息库_0824.md` 重算 `desc`」
    #    的分支，那些补进 `desc` 的行会被**静默盖回**。`A1992` 实测的那个例子就是
    #    `资料/卡牌数据表/卡牌完整信息库_0824.md` **自己漏了 `Painboy Sniklaw` 的 `Talent:` 行**
    #    （效果列只到 `Rally: Heal 3 to a friendly unit.`）⇒ `A1987` 补进 `desc` 的那一行会被重算掉。
    #    ⇒ 与 `_manual_subtype` / `_manual_keywords` **同一条口径**：`_manual_desc` /
    #      `_manual_descZh` 排在**最后**应用、**手工赢过算出来的**（覆盖发生在 `out[target]` 上，
    #      而 `out['desc']` 此时已经是 `keep` 带过来的那一份 ⇒ 手工列赢）。
    #    ⚠️ **这两列不存在时行为与改前逐字节相同**（`keep.get` 拿到 `None` ⇒ 直接 `continue`），
    #      所以加它**不动任何现有卡**；`_manual_*_note` 说明照旧由人写（见文件里那几段）。
    for col, target in (('_manual_subtype', 'subtype'), ('_manual_keywords', 'keywords'),
                        ('_manual_desc', 'desc'), ('_manual_descZh', 'descZh')):
        man = keep.get(col)
        if not isinstance(man, dict):
            continue
        if not isinstance(out.get(target), dict):      # 该列整个丢了（人删过）⇒ 补一个空表再覆盖，
            out[target] = {}                           #   别 `KeyError`（同族取舍见上面那段「不敢覆盖」）
        nd = 0
        for name, val in man.items():
            if name.startswith('_'):
                continue
            if out[target].get(name) != val:
                nd += 1
            out[target][name] = val
        if nd:
            print("手工覆盖 %s：%d 张（来源见文件里 %s_note）" % (target, nd, col))
    # ------------------------------------------------------------------
    # 🔴 **判据**（`A1633` 的下游，2026-10-12）：`subtype` / `keywords` 两列里
    #    **不许出现「名字不唯一」的裸卡名键**。
    #    形态上不可能同时满足：一个裸键只服务一张卡，而名字不唯一时它服务多张
    #    ⇒ 「裸键 × 名字不唯一」同时成立就是错（与 `gen_cards_engine.py` 里那条
    #    「裸卡名键的卡名必须在全池唯一」是同一条尺子、同一份计数口径）。
    #    位置放在**写盘之前** ⇒ 报红时**一个字节都不写**（照 `gen_cards_engine.py` 的
    #    `_bad` 与 `工具/gen_icon_plan.py` 的 `A1626` 同一条口径：「非 0」必须等于「没写文件」）。
    # ------------------------------------------------------------------
    _bare_bad = sorted(set(
        k for k in list(out['subtype']) + list(out['keywords'])
        if '/' not in k and _count_of(k) > 1))
    if _bare_bad or unresolved:
        print("\n🔴 卡面修正表里有 %d 个**裸卡名键**用在【同名卡】上"
              "（一个裸键服务了多张卡 ⇒ 值只可能是其中一张的）：" % len(_bare_bad))
        for k in _bare_bad:
            print("            · %r：全池 %d 张同名卡，裸键值 = %r"
                  % (k, _count_of(k), out['keywords'].get(k, out['subtype'].get(k))))
        for fn, nm, why in unresolved:
            print("            · 定位不到：%s（卡名 %r）—— %s" % (fn, nm, why))
        print("            ⇒ 判据：**名字不唯一时，键一律写 `阵营/卡名`**"
              "（同名卡的值只能开 PnP 成品卡图逐张读，铁律 7）。")
    else:
        print("\n裸卡名键唯一性 ✅（`subtype`/`keywords` 里每个裸键的卡名在全池只出现一次；"
              "计数口径 = %s）" % _cnt_src)
    _bad = bool(_bare_bad or unresolved)

    print("字段修正 %d 张；关键词修正 %d 张；合计涉 %d 张卡" % (n_sub, n_kw, len(fixes)))
    print("（因「效果里给别人加」而跳过的：%d 处）" % skipped_grant)

    # ---- `--check`：干跑。逐列比盘上那份，把「重跑会改什么」打出来（含**键的增删**，
    #      因为本脚本会**整列重写**这两列 —— 手写的 `阵营/卡名` 键本来会被删掉）。
    if CHECK:
        disk = None
        if os.path.exists(OUT):
            disk = json.load(io.open(OUT, encoding='utf-8'))
        diffs = 0
        for col in ('subtype', 'keywords'):
            old_c = (disk or {}).get(col) or {}
            new_c = out[col]
            for op, ks in (('删', sorted(set(old_c) - set(new_c))),
                           ('增', sorted(set(new_c) - set(old_c)))):
                for k in ks:
                    diffs += 1
                    print("   %s %-9s %r = %r" % (op, col, k, (old_c if op == '删' else new_c)[k]))
            for k in sorted(set(old_c) & set(new_c)):
                if old_c[k] != new_c[k]:
                    diffs += 1
                    print("   改 %-9s %r：%r → %r" % (col, k, old_c[k], new_c[k]))
        print("\n--check：相对盘上那份，`subtype`/`keywords` 两列差 %d 处%s"
              % (diffs, "（**一致 ✅**）" if diffs == 0 else ""))
        return 0 if (diffs == 0 and not _bad) else 1

    if _bad:
        print("\n🔴 退出码 1、**一个字节都不写**（上面那条没解决之前不落盘 ——"
              "写出去的会是一张卡带着别人那张卡的效果）")
        return 1

    # **让重跑逐字节幂等**：`subtype` / `keywords` 是上面新算出来的两列，而它们在**盘上那份**里
    #    未必排在末尾（人往文件里手工加过 `_` 说明键 —— 实测 `_2026-10-12_A1633_A1634_同名卡裸键与任务点`
    #    就排在它们后面）⇒ 不还原键序的话，**每次重跑都会把这两列挪到末尾**，
    #    `git diff` 看起来像整篇重写（本仓为这类噪声栽过好几次：见 `CLAUDE.md` 环境节那些行尾事故）。
    #    盘上没有的新键仍追加在末尾。
    out['subtype'] = _reorder_dict(out['subtype'], disk_cols.get('subtype'))
    out['keywords'] = _reorder_dict(out['keywords'], disk_cols.get('keywords'))
    if disk_keys:
        _ordered = {}
        for _k in disk_keys:
            if _k in out:
                _ordered[_k] = out[_k]
        for _k in out:
            if _k not in _ordered:
                _ordered[_k] = out[_k]
        out = _ordered

    with io.open(OUT, 'w', encoding='utf-8') as fh:
        fh.write(json.dumps(out, ensure_ascii=False, indent=1, sort_keys=False))

    print("写出：%s" % OUT)
    return 0


if __name__ == '__main__':
    sys.exit(main())
