# ⚠️ 一次性修复脚本（2026-10-01 已跑过，结果已进仓库）。
# 重新跑是**幂等**的（已经是合法局面的地方不会再改），但**只在** RuleEngineTest.cs
# 回到「有洞棋盘」状态时才有意义；`patch_manual.py` 依赖前两个脚本先跑过。
# -*- coding: utf-8 -*-
"""把自检夹具里的「有洞棋盘」一次性改成合法局面（连续无洞模型）。

v2：占位表**按 (作用域, 上下文变量)** 记账 —— `Place(ctx3, …)` 以前被正则漏掉过。

规则（只跑一遍、四个面一起改，避免重复套用）：
  · Place(X,P,S,…) / Board(X,P,S) / Players[P].Board[S] 读  → 用 P 那张映射（按 X 那张账）
  · 其它 X.Board[S] 读                                       → 该作用域各上下文映射一致才改
  · PlayCard / CanPlayCard 第 4 参（落点）                    → 施放者的映射
  · DeclareAttack 第 3 参（攻方）用 P；第 5 参（守方）用第 4 参那个玩家
  · PlayTactic / CanPlayTactic 第 4 参（目标格）              → 按卡面文案判朝向：
        enemy → 对家映射；friendly/your → 施放者映射；判不出的**不改**并列出来
  · UseAlternative / UseOathAbility 第 3 参                   → 施放者映射
"""
import re, io, json, collections

P = 'd:/4/Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs'
src = [l.decode('utf-8') for l in io.open(P, 'rb').read().split(b'\r\n')]

# ---- 战术卡文案（夹具 + 真卡）----
fx = {}
pat = re.compile(r'Tactic\(\s*"([^"]+)"')
for i, l in enumerate(src):
    m = pat.search(l)
    if m:
        blob = l[m.end():]
        if not re.search(r'"[^"]*"', blob):
            blob += (src[i + 1] if i + 1 < len(src) else '')
        mm = re.search(r',\s*"([^"]*)"', blob) or re.search(r'^\s*"([^"]*)"', blob)
        if mm:
            fx[m.group(1)] = mm.group(1)
db = {}
dbtype = {}
d = json.load(io.open('d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json', encoding='utf-8'))
for c in d['cards']:
    db[c['name']] = c.get('desc', '')
    dbtype[c['name']] = c.get('type')


def target_is_enemy(name):
    desc = fx.get(name) or db.get(name)
    if desc is None:
        return None
    t = desc.lower()
    en = any(k in t for k in ('an enemy', 'enemy unit', 'enemy troop', 'enemy warlord'))
    fr = any(k in t for k in ('friendly', 'your unit', 'your units', 'your troop', 'your warlord', 'your hand'))
    if en and not fr:
        return True
    if fr and not en:
        return False
    return None


def is_tactic(name):
    """这个卡名是不是战术卡（夹具 Tactic(...) 造的一律是；真卡查表）"""
    if name in fx:
        return True
    return dbtype.get(name) == 'tactic'


def test_bounds(s):
    meth = re.compile(r'^\s{0,8}(?:static\s+|public\s+|private\s+)*(?:void|bool|int|string|UnitState|BattleContext|List<\w+>)\s+(\w+)\s*\(')
    out = []; cur = None; st = 0
    for i, l in enumerate(s, start=1):
        m = meth.match(l)
        if m and m.group(1) != 'Run':
            if cur:
                out.append((cur, st, i - 1))
            cur = m.group(1); st = i
    if cur:
        out.append((cur, st, len(s)))
    return out


def strip_code(l):
    """去掉注释与字符串字面量 —— 否则 `$"…{x}…"` 里的花括号会被当成作用域（踩过）"""
    out = []; i = 0; n = len(l); instr = False
    while i < n:
        ch = l[i]
        if instr:
            if ch == '\\':
                i += 2; continue
            if ch == '"':
                instr = False
                out.append(' ')          # 字符串内容整体抹白
                i += 1; continue
            i += 1; continue
        if ch == '"':
            instr = True
            i += 1; continue
        if ch == '/' and i + 1 < n and l[i + 1] == '/':
            break
        if ch == '$' and i + 1 < n and l[i + 1] == '"':
            instr = True
            i += 2; continue
        out.append(ch); i += 1
    return ''.join(out)


def scopes_of(s, a, b):
    stack = []; mp = [0] * (b + 2); nxt = 1
    for i in range(a, b + 1):
        mp[i] = stack[-1] if stack else 0
        for ch in strip_code(s[i - 1]):
            if ch == '{':
                stack.append(nxt); nxt += 1
            elif ch == '}':
                if stack:
                    stack.pop()
    return mp


PLACE = re.compile(r'Place\(\s*(\w+)\s*,\s*(\d+)\s*,\s*(\d+)')
WRITE = re.compile(r'\.Board\[(\d+)\]\s*=\s*([^;]+);')
NEWCTX = re.compile(r'\b(\w+)\s*=\s*(?:Battle|ProbeBattle|BattlePool)\s*\(|\b(\w+)\s*=\s*new\s+BattleContext')

occ_all = {}
for name, a, b in test_bounds(src):
    sc = scopes_of(src, a, b)
    occ = collections.defaultdict(lambda: {0: {-1: set(), 1: set()}, 1: {-1: set(), 1: set()}})
    for i in range(a, b + 1):
        o = sc[i]; l = strip_code(src[i - 1])
        for m in NEWCTX.finditer(l):          # ctx = Battle(...) ⇒ 那份账清空
            key = (o, m.group(1) or m.group(2))
            occ[key] = {0: {-1: set(), 1: set()}, 1: {-1: set(), 1: set()}}
        for m in PLACE.finditer(l):
            cv, pl, sl = m.group(1), int(m.group(2)), int(m.group(3))
            if sl == 4:
                continue
            occ[(o, cv)][pl][-1 if sl < 4 else 1].add(abs(sl - 4) - 1)
        for m in WRITE.finditer(l):
            sl = int(m.group(1)); rhs = m.group(2).strip()
            if sl == 4 or rhs == 'null':
                continue      # ⚠️ `= null` 是**清格**（夹具在模拟离场），不该影响「初始布局」的映射
            for cv in list({k[1] for k in occ if k[0] == o}):
                for pl in (0, 1):
                    occ[(o, cv)][pl][-1 if sl < 4 else 1].add(abs(sl - 4) - 1)
    mp = {}
    for key, dd in occ.items():
        for pl, sides in dd.items():
            for sd, ixs in sides.items():
                if ixs and ixs != set(range(max(ixs) + 1)):
                    mp[(key[0], key[1], pl, sd)] = {old: r for r, old in enumerate(sorted(ixs))}
    occ_all[name] = (a, b, sc, mp)


def mapidx(mp, o, cv, pl, idx):
    if idx == 4:
        return None
    sd = -1 if idx < 4 else 1
    r = mp.get((o, cv, pl, sd), {}).get(abs(idx - 4) - 1)
    return None if r is None else 4 + sd * (r + 1)


def mapidx_any(mp, o, pl, idx):
    """不带上下文变量的读：该作用域里所有上下文一致才给值"""
    vals = set()
    for (oo, cv, ppl, sd) in list(mp):
        if oo == o and ppl == pl:
            v = mapidx(mp, oo, cv, ppl, idx)
            if v is not None:
                vals.add(v)
    return vals.pop() if len(vals) == 1 else None


def split_args(s):
    args = []; depth = 0; cs = []; instr = False
    for ch in s:
        if ch == '"':
            instr = not instr
        if not instr:
            if ch == '(':
                depth += 1
            elif ch == ')':
                depth -= 1
            elif ch == ',' and depth == 0:
                args.append(''.join(cs)); cs = []; continue
        cs.append(ch)
    if cs:
        args.append(''.join(cs))
    return args


CALL = re.compile(r'\b(PlayCard|PlayTactic|CanPlayCard|CanPlayTactic|DeclareAttack|UseAlternative|UseOathAbility'
                  r'|CanUseAbility|UseAbility|IsValidTarget|CanCollectWaystone|CollectWaystone'
                  r'|AvailableAlternative|CanStartAbility)\(')
cnt = collections.Counter(); amb = []

for name, a, b in test_bounds(src):
    A, B, sc, mp = occ_all[name]
    for i in range(a, b + 1):
        l = src[i - 1]; o = sc[i]; reps = []
        for m in PLACE.finditer(l):
            cv = m.group(1)
            new = mapidx(mp, o, cv, int(m.group(2)), int(m.group(3)))
            if new is not None and new != int(m.group(3)):
                reps.append((m.start(3), m.end(3), str(new))); cnt['Place'] += 1
        for m in re.finditer(r'Board\(\s*(\w+)\s*,\s*(\d+)\s*,\s*(\d+)\)', l):
            cv = m.group(1)
            new = mapidx(mp, o, cv, int(m.group(2)), int(m.group(3)))
            if new is not None and new != int(m.group(3)):
                reps.append((m.start(3), m.end(3), str(new))); cnt['Board(X)'] += 1
        for m in re.finditer(r'Players\[(\d+)\]\.Board\[(\d+)\]\s*(?=[^=])', l):
            new = mapidx_any(mp, o, int(m.group(1)), int(m.group(2)))
            if new is not None and new != int(m.group(2)):
                reps.append((m.start(2), m.end(2), str(new))); cnt['Players[].Board[]'] += 1
        for m in re.finditer(r'(?<!Players\[[01]\])(?<!\.Board)([A-Za-z_]\w*)\.Board\[(\d+)\]\s*(?=[^=])', l):
            new = mapidx_any(mp, o, None, int(m.group(2))) if False else None
            sl = int(m.group(2))
            vals = set()
            for pl in (0, 1):
                v = mapidx_any(mp, o, pl, sl)
                if v is not None:
                    vals.add(v)
            if len(vals) == 1:
                new = vals.pop()
                if new != sl:
                    reps.append((m.start(2), m.end(2), str(new))); cnt['X.Board[]'] += 1
        for m in CALL.finditer(l):
            fn = m.group(1); st = m.end(); depth = 1; j = st
            while j < len(l) and depth > 0:
                if l[j] == '(':
                    depth += 1
                elif l[j] == ')':
                    depth -= 1
                j += 1
            if depth != 0:
                continue
            args = split_args(l[st:j - 1]); offs = []; p2 = st
            for ar in args:
                idx = l.find(ar, p2) if ar.strip() else p2
                offs.append(idx); p2 = idx + len(ar)
            cv = args[0].strip()

            def argmap(pos, plpos, label):
                if pos >= len(args) or plpos >= len(args):
                    return
                txt = args[pos].strip(); pls = args[plpos].strip()
                if not re.fullmatch(r'\d', txt) or not re.fullmatch(r'\d', pls):
                    return
                new = mapidx(mp, o, cv, int(pls), int(txt))
                if new is None or new == int(txt):
                    return
                st2 = offs[pos] + len(args[pos]) - len(args[pos].lstrip())
                reps.append((st2, st2 + len(txt), str(new))); cnt[label] += 1

            if fn == 'DeclareAttack' and len(args) >= 5:
                argmap(2, 1, 'DeclareAttack·攻方'); argmap(4, 3, 'DeclareAttack·守方')
            elif fn in ('PlayCard', 'CanPlayCard') and len(args) >= 4:
                # ⚠️ `PlayCard` 对**战术卡**会转发给 PlayTactic ⇒ 那个槽位是**目标格**（可能在对家）
                nm = re.search(r'"([^"]+)"', args[2]) if len(args) > 2 else None
                tname = nm.group(1) if nm else None
                if tname and is_tactic(tname):
                    txt = args[3].strip(); caster = args[1].strip()
                    if re.fullmatch(r'\d', txt) and re.fullmatch(r'\d', caster):
                        en = target_is_enemy(tname)
                        pl = (1 - int(caster)) if en is True else int(caster)
                        label = 'PlayCard·战术·敌' if en is True else 'PlayCard·战术·己'
                        if en is None:
                            amb.append((name, i, int(txt), mapidx(mp, o, cv, 0, int(txt)),
                                        mapidx(mp, o, cv, 1, int(txt)), tname + '(PlayCard)', l.strip()[:88]))
                        new = mapidx(mp, o, cv, pl, int(txt))
                        if new is not None and new != int(txt):
                            st2 = offs[3] + len(args[3]) - len(args[3].lstrip())
                            reps.append((st2, st2 + len(txt), str(new))); cnt[label] += 1
                else:
                    argmap(3, 1, fn)
            elif fn in ('PlayTactic', 'CanPlayTactic') and len(args) >= 4:
                txt = args[3].strip(); caster = args[1].strip()
                if re.fullmatch(r'\d', txt) and re.fullmatch(r'\d', caster):
                    nm = re.search(r'"([^"]+)"', args[2]) if len(args) > 2 else None
                    en = target_is_enemy(nm.group(1)) if nm else None
                    pl = (1 - int(caster)) if en is True else int(caster)
                    label = 'Tactic·敌' if en is True else ('Tactic·己' if en is False else 'Tactic·未知')
                    if en is None:
                        v0 = mapidx(mp, o, cv, 0, int(txt)); v1 = mapidx(mp, o, cv, 1, int(txt))
                        if v0 != v1:
                            amb.append((name, i, int(txt), v0, v1, str(nm.group(1) if nm else '?'), l.strip()[:88]))
                    new = mapidx(mp, o, cv, pl, int(txt))
                    if new is not None and new != int(txt):
                        st2 = offs[3] + len(args[3]) - len(args[3].lstrip())
                        reps.append((st2, st2 + len(txt), str(new))); cnt[label] += 1
            elif fn in ('UseAlternative', 'UseOathAbility', 'CanCollectWaystone', 'CollectWaystone',
                        'AvailableAlternative', 'CanStartAbility') and len(args) >= 3:
                argmap(2, 1, fn)
            elif fn in ('CanUseAbility', 'UseAbility') and len(args) >= 3:
                argmap(2, 1, fn + '·自己格')
                if len(args) >= 4:                      # 第 4 参 = 目标格（朝哪一侧看卡面才知道）⇒ 两侧一致才改
                    txt = args[3].strip()
                    if re.fullmatch(r'\d', txt):
                        vals = set()
                        for pl in (0, 1):
                            v = mapidx(mp, o, cv, pl, int(txt))
                            if v is not None:
                                vals.add(v)
                        if len(vals) == 1 and vals != {int(txt)}:
                            st2 = offs[3] + len(args[3]) - len(args[3].lstrip())
                            reps.append((st2, st2 + len(txt), str(vals.pop()))); cnt[fn + '·目标格'] += 1
            elif fn == 'IsValidTarget' and len(args) >= 5:
                argmap(2, 1, 'IsValidTarget·攻方'); argmap(4, 3, 'IsValidTarget·守方')
        if reps:
            for st2, en2, new in sorted(reps, reverse=True):
                l = l[:st2] + new + l[en2:]
            src[i - 1] = l

print('改动统计:', dict(cnt), '合计', sum(cnt.values()))
print('未知朝向的战术卡（未改）:', len(amb))
for x in amb:
    print('   ', x)
io.open(P, 'wb').write(b'\r\n'.join(x.encode('utf-8') for x in src))
