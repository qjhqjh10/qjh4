# -*- coding: utf-8 -*-
"""全池对账 v2：卡面独立抄录（`_还原效果文字.md`）里的 `±N Melee/Ranged/Health`
vs 我们 `desc` 里 `±N <token>` 经 `GivePayload.ReAttr` + `NormalizeAttr` 会算成的属性。

v2 修掉 v1 的两个毛病：
  ① 按**卡名**索引会撞上同名跨阵营的卡 ⇒ 改成「一名多行就跳过并单独列出」
  ② 把两类分开报：
     类A = 我们写了 token、但 token 与卡面图标不是同一个属性（token 名被 OCR 读错）
     类B = 我们写的是**裸 `+N`**（没 token）⇒ `GivePayload` 兜底当近战，卡面却是紫枪/别的
"""
import io, os, re, sys, json, collections

RESTORED = r'd:/4/Unity/资料/卡表核对_卡图提取/_还原效果文字.md'
# 🔴 2026-10-24 更正（铁律 5）：`_cards_engine_before.json` **现读已不在这台机器上**
#   （`os.path.exists('d:/4/_tmp_view/_cards_engine_before.json')` = False；它是「换官方名/换官方文案
#   **之前**」拍的一次性快照，全仓没有任何脚本会再生成它 —— 扫过 `工具/**` 与 `Unity/**` 的 `.py/.sh`，
#   只有本文件与 `资料/普查产出_*/` 的历史报告引用它）。⇒ 本脚本**照默认参数跑不起来**。
#   ⛔ 别拿当前池 `MyGame/Assets/RuleEngine/Resources/cards_engine.json` 顶上：那是**换官方之后**的
#   口径（`nameZh`/`desc` 都换过一批），与 `_还原效果文字.md` 是同一时点的前提已经不存在 ⇒ 会把
#   「文案本身变了」当成「token 抄错」。要复算得先**另留一份当时的快照**，或用 `ENGINE = <别的快照>`
#   覆盖，并在报告里写清是哪一份。
ENGINE = r'd:/4/_tmp_view/_cards_engine_before.json'

ATTR_WORDS = (r'(ranged attack|attack|ranged|health|armor|armour|melee|might|fist|strength|weapon)')


def attr_from_token(tok):
    s = re.sub(r'\s*icon\s*$', '', tok.strip().strip('[]').strip().lower()).strip()
    if s.startswith('ranged attack'): return 'ranged'
    if s in ('melee', 'might', 'fist', 'strength', 'attack'): return 'attack'
    if s in ('ranged', 'weapon'): return 'ranged'
    if s in ('armor', 'armour'): return 'armour'
    if s == 'health': return 'health'
    return None


def attr_from_word(w):
    w = w.lower()
    return {'melee': 'attack', 'attack': 'attack', 'ranged': 'ranged', 'ranged attack': 'ranged',
            'health': 'health', 'armour': 'armour', 'armor': 'armour'}.get(w)


# ---- 1. 卡面那份（同名多行 ⇒ 标歧义，不判）----
byname = collections.defaultdict(list)
with io.open(RESTORED, encoding='utf-8') as f:
    for line in f:
        if not line.startswith('| Warpforge'): continue
        cols = [c.strip() for c in line.strip().strip('|').split('|')]
        if len(cols) < 3: continue
        seq = [attr_from_word(m.group(2)) for m in
               re.finditer(r'([+-]\d+)\s*(Melee|Ranged|Health)\b', cols[2])]
        byname[cols[1]].append([s for s in seq if s])

# ---- 2. 我们那份 ----
# ⚠️ 2026-10-24：缺文件时**出声停手**（原来直接 `io.open` 抛 FileNotFoundError，看不出该换哪一份）。
if not os.path.isfile(ENGINE):
    sys.exit("🔴 读不到 `%s` —— 它是「换官方名/文案之前」的一次性快照，本机已不在，"
             "也没有脚本会重新生成（见常量处的更正注）。\n"
             "   ⇒ 要么把 ENGINE 指到一份**同期**的快照，要么在报告里写明「本轮无法复算」。"
             % ENGINE)
eng = json.load(io.open(ENGINE, encoding='utf-8'))
cards = eng['cards'] if isinstance(eng, dict) else eng

classA, classB, ambiguous, nomatch = [], [], [], 0
for c in cards:
    desc = c.get('desc') or ''
    if not desc: continue
    mine, bare = [], []
    for m in re.finditer(r'([+-]\d+)\s*(\[[^\]\n]{1,12}\]|[A-Za-z][A-Za-z ]*?)(?=[\s,.;]|$)', desc):
        num, tok = m.group(1), m.group(2)
        a = attr_from_token(tok)
        if a: mine.append(a)
        elif re.fullmatch(r'[A-Za-z]*', tok.strip()) and num:
            bare.append((num, tok.strip()))
    rows = byname.get(c.get('name'))
    if not rows: nomatch += 1; continue
    if len(rows) > 1: ambiguous.append((c.get('id'), c.get('name'), rows)); continue
    theirs = rows[0]
    if mine != theirs:
        (classA if mine else classB).append((c.get('id'), c.get('faction'), c.get('name'), mine, theirs, desc, bare))

o = io.open(r'd:/4/_tmp_view/_scan_attr_all2.txt', 'w', encoding='utf-8')
o.write('卡面表条目 %d 个唯一卡名；引擎 1126 张里对得上的 %d 张（同名歧义 %d 张，卡面表里没有 %d 张）\n'
        % (len(byname), len(byname) - len(ambiguous), len(ambiguous), nomatch))
for title, rows in (('类A：我们写了 token，但 token 与卡面图标不是同一属性', classA),
                    ('类B：我们写的是裸 +N（兜底=近战），卡面另有属性', classB)):
    o.write('\n################ %s：%d 张 ################\n' % (title, len(rows)))
    for cid, fz, nm, mine, theirs, desc, bare in sorted(rows):
        o.write('%-32s %-16s %-28s 我们%s / 卡面%s\n' % (cid, fz, nm, mine, theirs))
        o.write('     %s\n' % desc[:150])
o.close()
print('A=%d B=%d' % (len(classA), len(classB)))
