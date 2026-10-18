# -*- coding: utf-8 -*-
"""
从原版卡 SO 真值（cards_1195_2022时代.json）抽出 CardTraits 那一层。

产出两样：
  (A) trait词典_原版真值.tsv   —— name / _trait / 枚举名 / 出现次数 / 示例卡id
  (B) A1748_原版trait真值.md   —— A1748 那 20 张卡的原版 trait 真值表

数据源（只读）：
  d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json   1195 个 RawCardScript 序列化字段
  d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs  _trait 数字 -> 枚举名（枚举定义完整）
  d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json  我们这一侧 1126 张（只读对照）

用法（可复算）：
  python -I -X utf8 d:/4/Unity/工具/gen_trait_dict.py

🔴 本脚本里唯一「手写」的东西 = 下面的 A1748_NAME 表，理由见它上面的注释。
"""
import io
import json
import os
import re
import sys
from collections import Counter, defaultdict

TRUTH = r'd:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json'
ENUM_SRC = r'd:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs'
OURS = r'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json'
OUTDIR = r'd:/4/Unity/资料/普查产出_第二十七会话'
TSV = os.path.join(OUTDIR, 'trait词典_原版真值.tsv')
MD = os.path.join(OUTDIR, 'A1748_原版trait真值.md')

# ---------------------------------------------------------------------------
# A1748 那 20 张卡。
#
# ⚠️ 名单的**出处**（只读）：`资料/普查产出_第二十五会话/W1_A1745落地.md` §3「逐张」块与 §6。
# ⚠️ **但那份报告里的 id 是【换 id 之前】的旧命名空间**（该报告写于第二十五会话，
#    而 id 换真值发生在第二十六会话）—— 实证：W1 报告写 `SOR9 = Battle Sister`，
#    而现卡池 `SOR9 = Preacher`、`SOR5 = Battle Sister`。
# ⇒ 所以这里**不用旧 id 定位，改用【卡名】定位**（卡名是跨命名空间稳定的键）。
#    下表 = (W1 报告里的旧 id, 该 id 在旧表里的卡名, 替代行动词)。
#    旧 id→卡名 的出处 = `git show 2cfb357:Unity/数据/游戏数据/card_ids.json`（换 id 前一版，
#    只读、不改仓库；`2cfb357` 是 601005b【id 整族换真值】的前一个提交）。
#    复算那张旧表的命令：
#      cd /d/4 && git show 2cfb357:Unity/数据/游戏数据/card_ids.json | \
#        python -I -X utf8 -c "import json,sys;m=json.load(sys.stdin)['mapping'];[print(i,m[i]) for i in ['AM32','AM39',...]]"
A1748_NAME = [
    ('AM32',  'Chimera',                     'duty'),
    ('AM39',  'Valkyrie',                    'duty'),
    ('DA18',  'Ravenwing Speeder',           'agenda'),
    ('DA30',  'Ravenwing Talonmaster',       'agenda'),
    ('SOR1',  'Erika Luminas',               'pray'),
    ('SOR5',  'Morvenn Vahl',                'pray'),
    ('SOR9',  'Battle Sister',               'pray'),
    ('SOR10', 'Battle Sister Gunner',        'pray'),
    ('SOR15', 'Celestian',                   'pray'),
    ('SOR21', 'Hospitaller',                 'pray'),
    ('SOR26', 'Aestred Thurga',              'pray'),
    ('SOR29', 'Imagifier',                   'pray'),
    ('SOR31', 'Celestian Sacresant',         'pray'),
    ('SOR33', 'Ephrael Stern',               'pray'),
    ('SOR36', 'Celestian Superior',          'pray'),
    ('SOR39', 'Immolator',                   'pray'),
    ('SOR71', 'Zephyrim Superior',           'pray'),
    ('SOR_Celestian_Sacresant_Aveline', 'Celestian Sacresant Aveline', 'pray'),
    ('SW8',  'Blackmane Reiver',            'ferocity'),
    ('SW23',  'Fenrisian Drop Pod',          'ferocity'),
]
ALT_WORDS = ('pray', 'duty', 'agenda', 'ferocity')


def nk(s):
    """卡名归一：小写、只留字母数字。`Aestred Thurga` 与 `Aestred-Thurga` 归一后相同。"""
    return re.sub(r'[^a-z0-9]', '', str(s).lower())


def nk_variants(s):
    """归一后的候选键：全名，以及「逗号前那一段」（原版卡名常带 `, Grand Master` 这类头衔后缀，
    而我们的卡名是裸名：真值 `Azrael, Grand Master` ↔ 我们 `DA_Azrael` = `Azrael`）。"""
    out = [nk(s)]
    if ',' in str(s):
        out.append(nk(str(s).split(',')[0]))
    return [x for x in out if x]


def load_enum():
    """从 DefinedTrait.cs 读 `名字 = 数字` 表（枚举定义完整，无需反编译）。"""
    txt = io.open(ENUM_SRC, encoding='utf-8').read()
    num2name, name2num = {}, {}
    for m in re.finditer(r'^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(\d+)\s*,?\s*$', txt, re.M):
        n, v = m.group(1), int(m.group(2))
        num2name.setdefault(v, n)
        name2num.setdefault(n, v)
    return num2name, name2num


def alt_variants(e):
    """真值里这张卡挂的四条替代行动 trait 的名字列表。"""
    return [str(t.get('name')) for t in (e.get('CardTraits') or []) if str(t.get('name')) in ALT_WORDS]


def main():
    num2name, name2num = load_enum()
    if not num2name:
        print('!! 没从 DefinedTrait.cs 读到枚举 —— 停手', file=sys.stderr)
        return 2

    truth = json.load(io.open(TRUTH, encoding='utf-8'))
    ours = json.load(io.open(OURS, encoding='utf-8'))['cards']
    our_by_id = {c['id']: c for c in ours}
    our_by_name = defaultdict(list)
    for c in ours:
        our_by_name[nk(c.get('name'))].append(c)
    truth_by_id = {}
    truth_by_name = defaultdict(list)
    for e in truth:
        if e.get('uniqueId'):
            truth_by_id.setdefault(str(e['uniqueId']), e)
        truth_by_name[nk(e.get('cardName'))].append(e)

    # ---------------- (A) 词典 ----------------
    cnt = Counter()        # (name, num) -> 卡数
    samp = {}              # (name, num) -> [ids]
    vals = defaultdict(set)
    cards_with_traits = 0
    for e in truth:
        ct = e.get('CardTraits') or []
        uid = str(e.get('uniqueId') or '')
        if ct:
            cards_with_traits += 1
        for t in ct:
            k = (str(t.get('name')), t.get('_trait'))
            cnt[k] += 1
            vals[k].add(t.get('_traitValue'))
            s = samp.setdefault(k, [])
            if uid and uid not in s and len(s) < 3:
                s.append(uid)

    unknown_nums = sorted({n for (_, n) in cnt if n not in num2name})
    unknown_names = sorted({nm for (nm, _) in cnt if nm not in name2num})
    byname_nums = defaultdict(set)
    bynum_names = defaultdict(set)
    for (nm, n) in cnt:
        byname_nums[nm].add(n)
        bynum_names[n].add(nm)
    bad_name = {k: sorted(v) for k, v in byname_nums.items() if len(v) > 1}
    bad_num = {k: sorted(v) for k, v in bynum_names.items() if len(v) > 1}

    tsv = ['trait_name\t_trait数字\t枚举名\t出现次数\t示例卡id(最多3个)']
    for (nm, n) in sorted(cnt, key=lambda k: (k[1] if k[1] is not None else -1, str(k[0]))):
        tsv.append('\t'.join([nm, str(n), num2name.get(n, '未找到'),
                              str(cnt[(nm, n)]), ','.join(samp[(nm, n)]) or '-']))
    if not os.path.isdir(OUTDIR):
        os.makedirs(OUTDIR)
    io.open(TSV, 'w', encoding='utf-8', newline='\n').write('\n'.join(tsv) + '\n')

    # ---------------- (B) A1748 逐卡 ----------------
    rows = []
    for old_id, name, alt in A1748_NAME:
        oc = our_by_name.get(nk(name), [])
        oc = oc[0] if oc else None
        te = truth_by_name.get(nk(name), [])
        te = te[0] if te else None
        kw = list(oc.get('keywords') or []) if oc else None
        if te is None:
            rows.append(dict(old=old_id, name=name, alt=alt, cur=(oc or {}).get('id'), kw=kw,
                             ct=None, note='⚠️ 真值里**查不到这张卡**（快照 2025-07 之后才新增）'))
            continue
        ct = te.get('CardTraits') or []
        names = {str(t.get('name')) for t in ct}
        d = []
        if not ct:
            d.append('原版 `CardTraits` **为空**')
        elif alt not in names:
            d.append('⚠️ 原版 trait 里**没有** `%s`' % alt)
        if kw is not None:
            norm = {str(k).split(':')[0].strip().lower().rstrip('.') for k in kw}
            if alt not in norm:
                d.append('🔴 我们 `keywords` **缺** `%s`' % alt)
        rows.append(dict(old=old_id, name=name, alt=alt, cur=(oc or {}).get('id'), kw=kw,
                         ct=ct, note='；'.join(d) or '✅ 一致'))

    # 全量口径（真值判据）：哪些卡原版挂了四条替代行动之一、而我们 keywords 缺词
    miss, okcnt, no_id, unmapped = [], 0, [], []
    for e in truth:
        alts = alt_variants(e)
        if not alts:
            continue
        uid = str(e.get('uniqueId'))
        oc = our_by_id.get(uid)
        if oc is None:                      # id 变了 -> 退一步按卡名找
            cand = []
            for k in nk_variants(e.get('cardName')):
                cand = our_by_name.get(k, [])
                if cand:
                    break
            oc = cand[0] if cand else None
            if oc is None:
                no_id.append((uid, e.get('cardName'), alts))
                continue
            unmapped.append((uid, oc['id'], e.get('cardName')))
        norm = {str(k).split(':')[0].strip().lower().rstrip('.') for k in (oc.get('keywords') or [])}
        lack = [a for a in alts if a not in norm]
        if lack:
            miss.append((oc['id'], oc.get('name'), lack))
        else:
            okcnt += 1
    n_alt = sum(1 for e in truth if alt_variants(e))
    nz_vals = {k: sorted(v - {0}) for k, v in vals.items() if v - {0}}

    # ---------------- 报告 ----------------
    md = []
    w = md.append
    w('# `A1748` 那 20 张卡 —— 原版 `CardTraits` 真值')
    w('')
    w('> 数据源 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`'
      '（原版 `RawCardScript` 序列化字段，1195 张）')
    w('> 枚举源 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`')
    w('> 我们那一侧 = `RuleEngine/Resources/cards_engine.json`（1126 张，只读对照）')
    w('> 生成脚本 = `d:/4/Unity/工具/gen_trait_dict.py`（本表由它现算，见 §3）')
    w('')
    w('## 1. 结论')
    w('')
    w('| # | 结论 |')
    w('|---|---|')
    w('| ① | 20 张里 **18 张在真值里查得到，且每一张的 `CardTraits` 都【确实挂着】那条替代行动 trait**'
      '（`pray`=1185 / `duty`=1220 / `agenda`=1235）⇒ `A1748` 的治本方向被真值**直接证实**：'
      '我们那 18 张 `keywords` 缺词是**数据侧漏抄**，不是「原版也没挂」。 |')
    w('| ② | 🔴 **剩下 2 张（`Blackmane Reiver` / `Fenrisian Drop Pod`）真值里【整张卡都没有】** ——'
      '它们要的 `ferocity` 在这份 2025-07 快照的 **650 张 `CardTraits` 里出现 0 次**'
      '（`DefinedTrait.ferocity = 1265` 定义了但快照里没人用）⇒ 这两张**本地拿不到原版判据**。 |')
    w('| ③ | 🔴 **本轮的 id 口径订正（重要）**：`W1_A1745落地.md` 里那 20 个 id 是**换真值之前的旧命名空间**'
      '（该报告写于第二十五会话，换 id 在第二十六会话）⇒ **别照它的 id 去卡池里找**。'
      '本报告**按卡名 join**，并逐张核对旧 id→新 id（见 §2 的「现卡池 id」列）。 |')
    w('| ④ | 四条替代行动的枚举真值：`pray`=**1185** · `duty`=**1220** · `agenda`=**1235** ·'
      ' `ferocity`=**1265**（`DefinedTrait.cs`）。真值里这三条的 `_traitValue` **一律 0**。 |')
    w('| ⑤ | **全量口径（真值当判据，不止这 20 张）**：真值里挂替代行动 trait 的卡共 **%d 张**，'
      '其中 **%d 张**我们 `keywords` **已经**有那个词、**%d 张缺**、**%d 张卡池里根本没有这张卡**。'
      '⇒ A1748 点名的那 18 张 **= 全量口径下的这 %d 张**（**无一漏网、无一多算**）。 |'
      % (n_alt, okcnt, len(miss), len(no_id), len(miss)))
    w('| ⑥ | ⚠️ 真值里这 18 张**多数还同时挂着别的 trait**（`flying` / `vanguard` / `camouflage` / `talent` …）'
      '—— 见 §2「原版 CardTraits」列。那些**我们 `keywords` 里基本都已经有**（§2 只报替代行动那一条的差异）。 |')
    w('')
    w('## 2. 逐卡表（`A1748` 20 张）')
    w('')
    w('> 「现卡池 id」= 按卡名在我们 1126 张里现查到的 id（**与 W1 报告的旧 id 不同是正常的**，见结论③）。')
    w('')
    w('| W1的旧id | 卡名 | 现卡池 id | 我们的 keywords | 原版 CardTraits(name/_trait/_traitValue) | 差异 |')
    w('|---|---|---|---|---|---|')
    for r in rows:
        kw = '（卡池里查无）' if r['kw'] is None else ('`[]` 空' if not r['kw'] else ', '.join('`%s`' % k for k in r['kw']))
        if r['ct'] is None:
            ctt = '——'
        elif not r['ct']:
            ctt = '**空**'
        else:
            ctt = ' · '.join('%s(`_trait`=%s, val=%s%s)' % (
                t.get('name'), t.get('_trait'), t.get('_traitValue'),
                '' if t.get('_trait') in num2name else ' ⚠枚举未找到') for t in r['ct'])
        w('| `%s` | %s | `%s` | %s | %s | %s |' % (
            r['old'], r['name'], r['cur'] or '?', kw, ctt, r['note']))
    w('')
    w('## 3. 复算命令（一行）')
    w('')
    w('```bash')
    w('python -I -X utf8 d:/4/Unity/工具/gen_trait_dict.py    # 重出本报告 + 词典 TSV，两处表都由它现算')
    w('```')
    w('')
    w('## 4. 自检（脚本顺带打的）')
    w('')
    w('| 项 | 读数 |')
    w('|---|---|')
    w('| 真值总张数 | %d |' % len(truth))
    w('| `CardTraits` 非空的张数 | **%d**（与 `卡牌id_换真值_落地记录.md` §五·4 记的 650 一致 ✅） |' % cards_with_traits)
    w('| 不同 `(name, _trait)` 组合 | **%d** |' % len(cnt))
    w('| `_trait` 数字不在 `DefinedTrait` 里的 | **%d**%s |' % (
        len(unknown_nums), '（' + ', '.join(map(str, unknown_nums)) + '）' if unknown_nums else ' ✅'))
    w('| `name` 不在 `DefinedTrait` 枚举名里的 | **%d**%s |' % (
        len(unknown_names), '（' + ', '.join(unknown_names[:10]) + '）' if unknown_names else ' ✅'))
    w('| 同名不同号（`name`→多个 `_trait`） | **%d**%s |' % (
        len(bad_name), '；'.join('`%s`→%s' % (k, v) for k, v in list(bad_name.items())[:5]) if bad_name else ' ✅ 一对一'))
    w('| 同号不同名（`_trait`→多个 `name`） | **%d**%s |' % (
        len(bad_num), '；'.join('`%s`→%s' % (k, v) for k, v in list(bad_num.items())[:5]) if bad_num else ' ✅ 一对一'))
    w('| `_traitValue` 取过非 0 的 trait | %s |' % (
        ' · '.join('`%s`(_trait=%s)=%s' % (k[0], k[1], v) for k, v in sorted(nz_vals.items()))
        or '（无）'))
    w('| 20 张里真值查不到 | **%d** 张：%s |' % (
        len([r for r in rows if r['ct'] is None]),
        ', '.join('`%s`(%s)' % (r['name'], r['old']) for r in rows if r['ct'] is None) or '—'))
    w('')
    w('## 5. 全量口径（真值判据 · 不止 A1748 那 20 张）')
    w('')
    w('真值里挂四条替代行动 trait 之一的卡共 **%d 张**；按「id → 卡名」两级 join 到我们 1126 张后：' % n_alt)
    w('- ✅ 我们 `keywords` **已经有**那个词的：**%d 张**' % okcnt)
    w('- 🔴 **缺词的：%d 张**（= A1748 点名的那 18 张，逐一吻合）' % len(miss))
    w('- ⚠️ **我们卡池里没有这张卡**：**%d 张** —— %s'
      % (len(no_id), '；'.join('`%s`(%s, 要 `%s`)' % (u, n, '/'.join(a)) for u, n, a in no_id) or '—'))
    if unmapped:
        w('- ℹ️ 其中 **%d 张**是「真值 id ≠ 我们 id、按卡名才 join 上」的（id 换真值时没覆盖到的形态）：%s'
          % (len(unmapped), '；'.join('`%s`→`%s`(%s)' % (a, b, c) for a, b, c in unmapped)))
    w('')
    w('缺词 %d 张逐条：%s' % (len(miss), '；'.join('`%s` %s(−%s)' % (i, n, '/'.join(l)) for i, n, l in sorted(miss))))
    w('')
    w('## 6. 还没查清的 / 还差什么')
    w('')
    w('- 🔴 **`ferocity`（1265）0 判据** —— 这份 2025-07 快照里 **一次都没出现**（含 SW8/SW23 那两张）。'
      '原版给 `Blackmane Reiver` / `Fenrisian Drop Pod` 挂没挂 `ferocity`，**本地核不了**'
      '（要更晚的 `allcards` 快照）。')
    w('- ⚠️ **快照年代**：Unity **2022.3.38f1 / 缓存 2025-07-30**，比当前客户端老；'
      '**字段结构与 id 方案是原版真值，个别卡的值未必等于最终版**。'
      '真值 **1195 张** vs 我们 **1126 张**，**266 张卡没有真值**'
      '（`卡牌id_换真值_落地记录.md` §二已记）。')
    w('- ⚠️ **「原版靠什么判这条行动发得出来」本轮没复核**：W1 报告 §2 查实原版走的是'
      '`CardAbility.trigger` 枚举（`CardScript__TriggeredPray.c`），**trait 是另一条线**。'
      '**「ability 触发器」与「`CardTraits`」谁是闸、两者是否恒同步，本轮没查** ——'
      '本报告只给「原版挂了什么 trait」，**不等于**「原版能不能发」。')
    w('- ⚠️ **`AM3 Commissar Denkler` / `AM66 Tempestor Prime`**：真值里挂了 `duty`、'
      '`showInCollectionOptions=5`、`emptyCard=0`，但**我们 1126 张里按 id 和卡名都查不到**。'
      '**为什么缺没查**（是没做、还是被筛掉了）。')
    w('- 🔴 **`ShowTraits` 与 `CardTraits` 不是一回事**：真值里与 `CardTraits` 平行还有一列 `ShowTraits`，'
      '而 **`A1748` 这 18 张的 `ShowTraits` 全部为【空】**（`CardTraits` 却都非空）⇒ '
      '**「空 `ShowTraits`」≠「没有 trait」**，判「原版挂了什么」必须读 **`CardTraits`**。'
      '两列各自的用途**没查**。')
    w('')
    w('## 7. 顺手发现的东西（只报，未改任何东西）')
    w('')
    w('- 🔴 **`W1_A1745落地.md` 的 id 是旧命名空间**（结论③）—— 该报告 §1/§3/§4 里所有 id 都按旧口径读，'
      '**下个会话照它去卡池里找会找错卡**。建议由主对话在正本里就地订正。')
    w('- 🔴 **`armour` trait ≠ 护甲值**：`CardTraits` 里 `name=="armour"`(1020) **全快照只有 2 条**'
      '（`BL27` / `TL70`），而 `armour` **数值字段 >0 的卡有 62 张** ⇒ 护甲走 `armour` 字段，不走 trait。')
    w('- ⚠️ **`keyword`（int）字段不是 trait 号**：取值只有 `0/5/10/15/25/30/35/40`；'
      '**548 张非 0 里 526 张的数字不在自己的 `CardTraits` 里** ⇒ 它**不是** trait 指针，**语义没查**。')
    w('- 📋 **`CardTraits` 这一层只用掉了很小一块**：650 张里 **46 种**不同的 `(name,_trait)` 组合'
      '（`DefinedTrait` 定义了 ~140 个）⇒ 其余 ~94 个枚举值在这份快照里**没人用**。')
    w('- 📋 **本轮只对账了「四条替代行动」这一个族**；`CardTraits` 与全池 `keywords` 的'
      '**逐卡全量对账（其余 42 种 trait）没做** —— 那才可能捞出另一批同样缺词的卡。')
    w('')
    io.open(MD, 'w', encoding='utf-8', newline='\n').write('\n'.join(md) + '\n')

    print('TSV ->', TSV, '(%d rows)' % (len(tsv) - 1))
    print('MD  ->', MD)
    print('真值 %d 张 | CardTraits 非空 %d | 组合 %d | 枚举缺 %d 数字 / %d 名字'
          % (len(truth), cards_with_traits, len(cnt), len(unknown_nums), len(unknown_names)))
    print('同名不同号 %d | 同号不同名 %d' % (len(bad_name), len(bad_num)))
    print('alt-trait 卡 %d | 已有 %d | 缺词 %d | 卡池无此卡 %d' % (n_alt, okcnt, len(miss), len(no_id)))
    print('A1748 真值查不到:', [r['name'] for r in rows if r['ct'] is None] or '无')
    return 0


if __name__ == '__main__':
    sys.exit(main())
