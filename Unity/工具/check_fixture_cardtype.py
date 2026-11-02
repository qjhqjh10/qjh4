#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""check_fixture_cardtype.py — 夹具 `new CardDef(...)` 没挂 `AttachCardType` 的机器闸。

## 它防的是什么（B117-R72 / B118-W7）
`CardDef.CardType` 缺省 = `GateUnknown(-1)`（`RuleEngine/Core/CardDef.cs:6610`），只有
`CardDatabase.Parse` 或夹具自己 `AttachCardType(...)` 才写它。凡「闸在 cardType 上」的
原版行为，对一张没挂 cardType 的夹具卡都是**静默跳过**（实测先例：`B117DROPPOD` 的 D3⁗ ——
`SetupDropPodStats` 第一道闸直接 return，血恒 0 而不报错）。

## 判据（6 个消费点，现读）
  ① `RuleCore.cs:3999`           `CardType == CardTypeTactic`（`CardPlayedMask` 的 Tactic 位）
  ② `RuleCore.cs:15960`          `GetCardsInCemetery` 的四档过滤 + 督军无条件排除
  ③ `UnitState.cs:3907`          `SetupDropPodStats`（`CardType == CardTypeHero`）
  ④ `UnitState.cs:3941`          `SetupFTUELowLifeInvulnerability`（`CardType == CardTypeHero`）
  ⑤ `EffectResolverPartial_B88E2.cs:727`  弃整手牌时跳过 Whispers(40)
  ⑥ `Enchantment.cs:515`         `FilterByQuestCounter`（`CardType == CardTypeWhispers`）
外加 `CardDef.IsTrapCard`（`CardType == Tactic && SpellType == Trap`）—— 它经
`CardInstance.TrapArmedOf` 落到 `ArmedTrap`。

## 规则
扫 `<root>/**/Editor/**/*.cs`：每一处 `new CardDef(`，若
  (a) 同一函数里**没有** `AttachCardType`，且
  (b) 该变量被喂进上面某个**对它真会出岔子**的消费点
⇒ 报出来。**按卡种收窄**（避免「非督军走 `SetupDropPodStats` 也报」这类噪声）：
  · `UnitStateCtor` / `低血无敌`  → 只报 **hero**（其余卡种在 `SetupDropPodStats` 里行为与 -1 相同）
  · `IsTrapCard` / `CardPlayedMask` → 只报 **tactic**（其余卡种 != Tactic，结果与 -1 相同）
  · `QuestCounter`               → 只报 **whispers**
  · `GetCardsInCemetery`         → **全卡种**（四档过滤 + 督军无条件排除，任何卡种都可能被误放行）
`[direct]` 是硬违规（退出码 1）；`[same-function]` 是候选（只打印，不置失败）。

## 用法（⛔ 不带动作参数 = 只打用法、rc=2、零写盘）
  python -I -X utf8 工具/check_fixture_cardtype.py --check [--root <Assets 目录>] [--verbose]

退出码：0 = 没有 [direct]；1 = 有 [direct]；2 = 没给动作参数 / 用法错误。
"""
import io, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_ROOT = os.path.abspath(os.path.join(HERE, '..', 'MyGame', 'Assets'))

CTRL = {'if', 'for', 'foreach', 'while', 'switch', 'catch', 'using', 'lock',
        'fixed', 'do', 'else', 'try', 'finally'}

# 触发点：(名字, 正则, 适用卡种集合)。卡种集合 = {'hero','tactic','whispers','other'} 的子集；
# None = 全卡种。
TRIGGERS = [
    ('GetCardsInCemetery', re.compile(r'\bGetCardsInCemetery\s*\('), None),
    ('IsTrapCard',         re.compile(r'\.\s*IsTrapCard\s*\('),      {'tactic'}),
    ('dropPod',            re.compile(r'\b(SetupDropPodStats|CurrentDropPodHealth|CurrentDropPodAttack)\b'), {'hero'}),
    ('低血无敌',           re.compile(r'\b(SetupFTUELowLifeInvulnerability|ShouldBecomeInvulnerableWithLowLife)\b'), {'hero'}),
    ('UnitStateCtor',      re.compile(r'\bnew\s+UnitState\s*\('),    {'hero'}),
    ('CardPlayedMask',     re.compile(r'\bCardPlayedMask\s*\('),     {'tactic'}),
    ('QuestCounter',       re.compile(r'\bFilterByQuestCounter\s*\('), {'whispers'}),
]

VARDECL = re.compile(r'([A-Za-z_][A-Za-z0-9_]*)\s*=\s*$')


def mask(text):
    """把字符串/字符/注释替换成空格（保长度），避免花括号与字符串干扰解析。"""
    out = list(text)
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == '/' and i + 1 < n and text[i + 1] == '/':
            while i < n and text[i] != '\n':
                out[i] = ' '; i += 1
        elif c == '/' and i + 1 < n and text[i + 1] == '*':
            out[i] = ' '; out[i + 1] = ' '; i += 2
            while i + 1 < n and not (text[i] == '*' and text[i + 1] == '/'):
                out[i] = ' '; i += 1
            if i + 1 < n:
                out[i] = ' '; out[i + 1] = ' '; i += 2
        elif c == '@' and i + 1 < n and text[i + 1] == '"':
            out[i] = ' '; out[i + 1] = ' '; i += 2
            while i < n:
                if text[i] == '"' and (i + 1 >= n or text[i + 1] != '"'):
                    out[i] = ' '; i += 1; break
                if text[i] == '"' and i + 1 < n and text[i + 1] == '"':
                    out[i] = ' '; out[i + 1] = ' '; i += 2; continue
                out[i] = ' '; i += 1
        elif c == '"' or (c == '$' and i + 1 < n and text[i + 1] == '"'):
            if c == '$':
                out[i] = ' '; i += 1
            out[i] = ' '; i += 1
            while i < n:
                if text[i] == '\\':
                    out[i] = ' '; i += 1
                    if i < n:
                        out[i] = ' '; i += 1
                    continue
                if text[i] == '"':
                    out[i] = ' '; i += 1; break
                out[i] = ' '; i += 1
        elif c == "'":
            out[i] = ' '; i += 1
            while i < n:
                if text[i] == '\\':
                    out[i] = ' '; i += 1
                    if i < n:
                        out[i] = ' '; i += 1
                    continue
                if text[i] == "'":
                    out[i] = ' '; i += 1; break
                out[i] = ' '; i += 1
        else:
            i += 1
    return ''.join(out)


def find_blocks(masked):
    """返回 [(open_pos, close_pos, header)]。"""
    blocks, stack, last_break = [], [], 0
    for i, c in enumerate(masked):
        if c == '{':
            stack.append((i, masked[last_break:i].strip())); last_break = i + 1
        elif c == '}':
            if stack:
                op, hdr = stack.pop(); blocks.append((op, i, hdr)); last_break = i + 1
        elif c == ';':
            if stack:
                last_break = i + 1
    return blocks


def is_method_header(h):
    h = re.sub(r'^\s*\[[^\]]*\]\s*', '', h.strip())
    if '=>' in h or '(' not in h or ')' not in h:
        return False
    m = re.match(r'([A-Za-z_][A-Za-z0-9_]*)', h)
    if m and m.group(1) in CTRL:
        return False
    if re.search(r'\b(class|struct|interface|enum|namespace|record)\b', h):
        return False
    return True


def card_kind(text, pos):
    """从 `new CardDef(...)` 的实参里判卡种：'hero' / 'tactic' / 'whispers' / 'other'。
    第 3 个顶层实参 = `type` 字符串；`subtype: "Warlord"` 也当 hero（真卡 56 个督军全是）。"""
    i = pos + len('new')  # 跳过 'new'
    while i < len(text) and text[i] != '(':
        i += 1
    i += 1
    depth = 1
    start = i
    args = []
    in_str = False
    esc = False
    while i < len(text) and depth > 0:
        c = text[i]
        if in_str:
            if esc:
                esc = False
            elif c == '\\':
                esc = True
            elif c == '"':
                in_str = False
        elif c == '"':
            in_str = True
        elif c in '([{':
            depth += 1
        elif c in ')]}':
            depth -= 1
            if depth == 0:
                args.append(text[start:i])
                break
        elif c == ',' and depth == 1:
            args.append(text[start:i])
            start = i + 1
        i += 1
    type_s = ''
    if len(args) >= 3:
        m = re.search(r'"([^"]*)"', args[2])
        if m:
            type_s = m.group(1).strip().lower()
    whole = text[pos:pos + 600]
    if type_s == 'hero' or re.search(r'subtype\s*:\s*"Warlord"', whole):
        return 'hero'
    if type_s in ('tactic', 'defence', 'enchantment', 'enchant'):
        return 'tactic'
    if type_s in ('whispers', 'whisper'):
        return 'whispers'
    return 'other'


def enclosing_function(text, masked, blocks, methods, pos):
    enc = sorted([b for b in methods if b[0] < pos < b[1]], key=lambda b: b[1] - b[0])
    if enc:
        return enc[0]
    return (0, len(text), '<whole-file>')   # 表达式体方法 / 类字段初始化：退化到整个文件


def arg_is_var(m, var):
    """触发调用 `(` 之后到配对的 `)` 之间，是否有独立的变量名。"""
    start = m.end()
    depth = 1
    i = start
    text_m = m.string
    while i < len(text_m) and depth > 0:
        if text_m[i] == '(':
            depth += 1
        elif text_m[i] == ')':
            depth -= 1
            if depth == 0:
                break
        i += 1
    seg = text_m[start:i]
    return re.search(r'(?<![A-Za-z0-9_])' + re.escape(var) + r'(?![A-Za-z0-9_])', seg) is not None


def scan(root):
    rows = []
    for dirpath, dirnames, files in os.walk(root):
        if os.path.basename(dirpath) != 'Editor':
            continue
        for fn in sorted(files):
            if not fn.endswith('.cs'):
                continue
            p = os.path.join(dirpath, fn)
            text = io.open(p, 'rb').read().decode('utf-8', 'replace').replace('\r\n', '\n')
            mk = mask(text)
            blocks = find_blocks(mk)
            methods = [b for b in blocks if is_method_header(b[2])]
            rel = p.replace('\\', '/').replace(root.replace('\\', '/') + '/', '')
            for m in re.finditer(r'new\s+CardDef\s*\(', mk):
                pos = m.start()
                ls = text.rfind('\n', 0, pos) + 1
                vm = VARDECL.search(text[ls:pos])
                var = vm.group(1) if vm else None
                op, cl, hdr = enclosing_function(text, mk, blocks, methods, pos)
                if 'AttachCardType' in mk[op:cl]:
                    continue
                line = text.count('\n', 0, pos) + 1
                kind = card_kind(text, pos)
                direct, same_fn = [], []
                for name, rx, kinds in TRIGGERS:
                    if kinds is not None and kind not in kinds:
                        continue
                    for tm in rx.finditer(mk[op:cl]):
                        if var and arg_is_var(tm, var):
                            direct.append(name)
                        else:
                            same_fn.append(name)
                if direct or same_fn:
                    rows.append(dict(file=rel, line=line, var=var or '?',
                                     fn_line=text.count('\n', 0, op) + 1,
                                     direct=sorted(set(direct)), same=sorted(set(same_fn))))
    return rows


def main(argv):
    if '--check' not in argv:
        sys.stderr.write(__doc__)
        return 2
    root = DEFAULT_ROOT
    if '--root' in argv:
        k = argv.index('--root')
        if k + 1 >= len(argv):
            sys.stderr.write('--root 缺目录\n')
            return 2
        root = os.path.abspath(argv[k + 1])
    if not os.path.isdir(root):
        sys.stderr.write('目录不存在: %s\n' % root)
        return 2

    rows = scan(root)
    direct = [r for r in rows if r['direct']]
    same = [r for r in rows if not r['direct']]

    print('[check_fixture_cardtype] root = %s' % root)
    print('[check_fixture_cardtype] 扫描：new CardDef 且同函数无 AttachCardType 且触及消费点 = %d 处'
          % len(rows))
    print('  [direct]        = %d 处（硬违规：变量出现在触发调用的实参里）' % len(direct))
    print('  [same-function] = %d 处（候选：同函数有触发调用，间接流）' % len(same))
    print()
    if direct:
        print('=== [direct] 硬违规 ===')
        for r in direct:
            print('  %s:%d  | %s | fn@%d | %s' % (r['file'], r['line'], r['var'], r['fn_line'],
                                                  ', '.join(r['direct'])))
    if same and '--verbose' in argv:
        print()
        print('=== [same-function] 候选 ===')
        for r in same:
            print('  %s:%d  | %s | fn@%d | %s' % (r['file'], r['line'], r['var'], r['fn_line'],
                                                  ', '.join(r['same'])))
    print()
    print('[check_fixture_cardtype] 结论：%s' % ('有 [direct] 违规，须补 AttachCardType' if direct else '无 [direct] 违规'))
    return 1 if direct else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
