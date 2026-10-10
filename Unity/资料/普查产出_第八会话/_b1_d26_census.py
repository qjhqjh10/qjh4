# -*- coding: utf-8 -*-
"""D26 普查：把全仓 `.cs` 里所有 `rule_core` 的出现逐条列出来并分类。

只读源码，只写它**自己**的输出文件 —— ⚠️ 旧 docstring 写的 `/tmp/d26_census.txt` **是错的**，
它一直写的是 `资料/普查产出_第八会话/_b1_d26_census.txt`（2026-10-11 `A1307` 就地订正）。
⚠️ **`ROOTS` 只有 `RuleEngine` / `CardPresentation` 两个 `.cs` 根** ⇒ 它**看不见 `资料/**/*.md`**
（文档侧要另扫；2026-10-11 有一轮把这个「只扫 `.cs`」误读成「这活是纯文档、不撞写手」）。

🔴 **2026-10-11（`A1307`）分类器改了三处 + 另加一档** —— 旧版**系统性假阳性**：
实测报「没降级 + 没用强词」**96** 处，逐处现读后**真余量只有 2** 处
（判据全文 = `资料/普查产出_第十二会话/V7_D26余量.md` §①）：
  ① **词表补「要上下文才成立」的降级语** —— `那是错的` / `我们抄窄了` / `抄窄` /
     `没有权威依据` / `零依据` / `订正`。旧 `WEAK` 里没有这几个 ⇒ 那些行整行被算进「没降级」桶。
  ② **行级子串匹配改成 ±3 行窗口**（`WINDOW`）—— 声明句**断行**时（`BattleContext.cs:7` 是前半句、
     「我们自己的…」落在 `:8`），逐**行**匹配看不见下一行的降级语。
  ③ **加「本文件头有没有口径声明」那一档**（`SCOPE` / `HEADN`）—— 文件头若已交代过
     「凡引 `rule_core.gd:<行>` 的，按『我们上一版当时的做法』读」，则该文件内**所有**引用都算已覆盖。
     96 → 2 里最大的一块（72 处）就是这一档。
  ④ 🆕 **另加一档【与文件头声明打架】**（`BAD_RE` / `contradicts()`）：文件头声明
     「那份 `.gd` **不是原版**」，而某一行仍写「**原版** `rule_core.gd:NNNN`」⇒ 它会被 ③ 那一档
     **整个吞掉**，可它**正是要改的那种**（`A1305` 那 2 处就是这么漏掉的）⇒ 这一档
     **不受文件头档豁免**，单独列在输出最前面。
     ⛔ 判据**不是**「行里出现『原版』两个字」—— 那会把「原版反编译」「原版 prefab」「原版资源」
     全捞成假阳性；判据是 **`原版` 紧挨着 `rule_core`**（中间只许夹几个修饰符号）。
"""
import io, os, re, collections

ROOTS = ['RuleEngine', 'CardPresentation']
BASE = 'D:/4/Unity/MyGame/Assets'
OUT = 'D:/4/Unity/资料/普查产出_第八会话/_b1_d26_census.txt'

# 旧词表（第八会话那一版原样冻结）—— 只用来复现「旧口径」那个读数，好做改前/改后对照
WEAK0 = ['我们自己的', '上一版复刻', '上一版 Godot', '我们上一版', '不是原版',
         '旁证', '非权威', '非原版', '更正', '那份 .gd', '那份 `.gd`', 'gd 只是']
# 🆕 A1307 ① 补：要上下文才成立的降级语（旧词表里没有 ⇒ 整行被算进「没降级」桶 = 假阳性来源）
WEAK = WEAK0 + ['那是错的', '我们抄窄了', '抄窄', '没有权威依据', '零依据', '订正']
STRONG = ['判据', '权威', '规格书', '照抄', '参考实现', '出处', '语义来源', '基准', '语义照']

# 🆕 A1307 ③ 文件头「口径声明」⇒ 该文件内**所有**引用都算已覆盖（这是 96 → 2 里最大的一块）
SCOPE = ['口径', '旁证', '非权威', '不能当原版', '不是原版', '不是权威', '我们上一版']
HEADN = 40      # 声明都写在文件头，只看前 40 行
WINDOW = 3      # 🆕 A1307 ② 降级语的有效半径 = 本行 ±3 行
# 🆕 A1307 ④ 「原版」紧挨 `rule_core` = 与文件头声明自相矛盾的那种写法
BAD_RE = re.compile(r'原版\s*[`「（(：:]*\s*rule_core')


def weak_at(lines, i):
    """本行 ±WINDOW 行内有没有降级语（A1307 ②）—— 声明句断行时才算得对。"""
    return any(any(w in lines[k] for w in WEAK)
               for k in range(max(0, i - WINDOW), min(len(lines), i + WINDOW + 1)))


def scope_decl(lines):
    """本文件头有没有一句「怎么读 `rule_core.gd` 引用」的口径声明（A1307 ③）。"""
    return any(any(s in lines[k] for s in SCOPE)
               for k in range(0, min(len(lines), HEADN)))


def contradicts(l):
    """本行与文件头声明打架吗（A1307 ④）。⛔ 不受文件头档豁免：那不是「已覆盖」，是「要改」。"""
    if '原来写' in l:
        return False    # 「原来写「原版 rule_core…」」= 订正痕迹里的【引述】，不是本行主张
    return bool(BAD_RE.search(l))


files, rows = {}, []
for root in ROOTS:
    for dp, dn, fn in os.walk(os.path.join(BASE, root)):
        for f in fn:
            if not f.endswith('.cs'):
                continue
            full = os.path.join(dp, f)
            rel = full.replace(BASE + os.sep, '').replace(os.sep, '/')
            try:
                txt = io.open(full, 'r', encoding='utf-8', newline='').read()
            except Exception:
                continue
            lines = txt.split('\n')
            files[rel] = lines          # 留一份上下文，供 ±3 行窗口 / 文件头档用
            for i, l in enumerate(lines):
                if 'rule_core' in l:
                    rows.append((rel, i, l.rstrip()))

out = []
out.append('TOTAL %d' % len(rows))
c = collections.Counter(r[0] for r in rows)
out.append('FILES %d' % len(c))

old_kind = collections.Counter()        # 旧口径（行级 + WEAK0）—— 与历史读数可比
cat = collections.Counter()             # 新分档
hit = []
for rel, i, l in rows:
    lines = files[rel]
    old_kind[(any(k in l for k in WEAK0), any(k in l for k in STRONG))] += 1
    if contradicts(l):
        k = '!'
    elif weak_at(lines, i):
        k = 'B'
    elif scope_decl(lines):
        k = 'H'
    elif any(s in l for s in STRONG):
        k = 'S'
    else:
        k = 'R'
    cat[k] += 1
    if k in ('!', 'S', 'R'):
        hit.append((k, rel, i + 1, l))

out.append('旧口径分布（行级 + 旧词表 WEAK0）: %s' % dict(old_kind))
out.append('旧口径「没降级 + 没用强词」= %d   ← 旧版报出来的那个数（系统性高估，见文件头 ①②③）'
           % old_kind[(False, False)])
out.append('新分档: %s' % dict(cat))
out.append('新口径【真残留】= %d（S + R）·【与文件头声明打架】= %d（!）· 已覆盖 = %d（B + H）'
           % (cat['S'] + cat['R'], cat['!'], cat['B'] + cat['H']))
out.append('档位： ! 与文件头声明打架（优先要改） · B 本行 ±3 行内有降级语 · H 文件头有口径声明')
out.append('       · S 没降级但用了强词 · R 其余真残留')
out.append('')
out.append('== 逐文件 ==')
for f, n in sorted(c.items(), key=lambda x: -x[1]):
    out.append('%4d  %s' % (n, f))
for title, key in (('① 与文件头声明打架的行（优先要改）', '!'),
                   ('② 没降级 + 用了强词的行', 'S'),
                   ('③ 其余真残留（没降级、也没用强词）', 'R')):
    group = [h for h in hit if h[0] == key]
    out.append('')
    out.append('== %s（%d 处） ==' % (title, len(group)))
    for k, rel, n, l in group:
        out.append('%s:%d | %s' % (rel, n, l))

io.open(OUT, 'w', encoding='utf-8', newline='\n').write('\n'.join(out))
print('ok', len(rows), len(files), dict(cat))
