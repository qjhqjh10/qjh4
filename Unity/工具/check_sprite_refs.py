#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B146-S7 资源引用闸 —— 代码里引用的 sprite 名 → `Resources/Art/<子目录>/<名字>.png` 有没有这张图。

为什么：`WindowButton.cs` 引用过 `UI_Button_Organe_Square_Pressed`，而那张图当时**从没导入**
`Resources/Art` ⇒ 按下态静默不换图；运行期只有 `MissingSwapArt` 打日志、不会让断言红。
这道闸是**静态**的：改代码时就能抓出「名字写了、图没导」。

只认**已知加载口**（见 `PORTS`，每个口对应一个 `Resources/Art` 子目录）：
    CardArt.MenuUi / Ui / DeckUi / Trait / Cosmetics / Cosmetic / CosmeticSdf
    CardArt.Portrait / AltArt / OffensiveFace
    BattleDriver.HudImage（实例方法，第 2 个实参是图名，内部走 `CardArt.Ui`）
    WindowButton.HoverNames / PressedNames（两张字典里的**键和值**都是图名，按 `MenuUi` 三档查）
    CardArt._deckArtById（`TryDeckHero` 的 56 条「督军 id → 立绘资产名」，同样按 `MenuUi` 三档查）

口径：
  · **只收字符串字面量**。`nameof(...)` / 常量 / 变量 / 拼接 / 插值 ⇒ `❓ 无法静态解析`（单列，⛔ 不算缺失）。
  · 比较 **大小写不敏感**（Windows 上 `Resources.Load` 即大小写不敏感）；
    `MenuUi` 额外按导入约定把**空格换成下划线**再试一次（`UI_Main_Upper bar` → `UI_Main_Upper_bar`）。
  · 注释里的调用**不算引用**（先做 C# 词法切分，注释与字符串内容都从「代码面」抹掉）。
  · **`??` 兜底链合并**：`Ui("x") ?? MenuUi("x") ?? DeckUi("x")` 三条各自只看一条必然 miss，
    但整条表达式是好的 ⇒ 同一源码行、同一个名字里只要有一条 ✅，其余 🟠 改判 ✅（见 `merge_null_chains`）。
  · 名字来自**变量 / 数据表 / 帮助函数**（`IconName(k)` / `b.sprite` / `CardbackTable`）的
    一律是 `❓`，本闸**看不见** —— 那是假阴边界，见报告 §五。

用法：
    python -I -X utf8 d:/4/Unity/工具/check_sprite_refs.py              # 只打印摘要 + 🟠/❓ 清单
    python -I -X utf8 .../check_sprite_refs.py --verbose               # 连 ✅ 也逐条打印
    python -I -X utf8 .../check_sprite_refs.py --write                 # 落盘 TSV（默认 判据/B146_S7_资源引用闸.tsv）
    python -I -X utf8 .../check_sprite_refs.py --fail-on-missing       # 🟠 > 0 时退出码 1（给断言宿主用）

⛔ 默认**不写盘**；只有 `--write` 才写 TSV。
⛔ 不改任何源码/资产。只读扫描 + 可选落一份 TSV。
"""

import argparse
import bisect
import io
import os
import re
import sys

DEFAULT_ASSETS = r"d:/4/Unity/MyGame/Assets"
DEFAULT_ART_REL = os.path.join("CardPresentation", "Resources", "Art")
DEFAULT_TSV = r"d:/4/Unity/资料/完全复刻/判据/B146_S7_资源引用闸.tsv"

# ---------------------------------------------------------------- 取图口表
# roots: 依序尝试的 `Resources/Art` 子目录（MenuUi 是三档兜底，别的口只有一个）。
# transform: 名字 → 文件主名 的换算（None = 原样）。
PORTS = [
    # method,             roots,                         transform,   note
    ("CardArt.MenuUi",     ["ui_menu", "ui_deck", "ui"],  "menu_space"),
    ("CardArt.Ui",         ["ui"],                        None),
    ("CardArt.DeckUi",     ["ui_deck"],                   None),
    ("CardArt.Trait",      ["traits"],                    None),
    ("CardArt.Cosmetics",  ["avatars"],                   None),
    ("CardArt.Cosmetic",   ["cardbacks"],                 None),
    ("CardArt.CosmeticSdf", ["cardbacks"],                "sdf_suffix"),
    ("CardArt.Portrait",   ["cards"],                     "art_slug"),
    ("CardArt.AltArt",     ["altarts"],                   "alt_slug"),
    ("CardArt.OffensiveFace", ["offensive"],              "offensive"),
]
PORT_BY_METHOD = {m: (r, t) for (m, r, t) in PORTS}

# `Resources/Art` 下真实存在的子目录（`--list-dirs` 现读；硬编码只作为报告里的期望）
EXPECTED_DIRS = [
    "altarts", "avatars", "cardbacks", "card3d", "cards", "offensive",
    "traits", "ui", "ui_campaign", "ui_deck", "ui_menu", "vfx",
]

# 字典口：文件 → [(字典名, 引用方式)]
DICT_PORTS = [
    ("Shell/WindowButton.cs", "HoverNames"),
    ("Shell/WindowButton.cs", "PressedNames"),
]


# ---------------------------------------------------------------- C# 词法
class Lit(object):
    __slots__ = ("start", "end", "value", "kind")
    def __init__(self, start, end, value, kind):
        self.start = start
        self.end = end
        self.value = value   # None = 不能当常量用（插值/原始串里有 {}）
        self.kind = kind     # plain / verbatim / interp / raw


_RAW_RE = re.compile(r'(\$@|@\$|\$|@)?("""+)')


def _line_at(nl, pos):
    """pos 的行号（1 基）。nl = 每个换行符的下标列表（每文件建一次，避免 O(n^2)）。"""
    return bisect.bisect_left(nl, pos) + 1


def _read_plain(src, i):
    """i 指向开引号；返回 (end, value)。处理 \\ 转义。"""
    n = len(src)
    j = i + 1
    buf = []
    while j < n:
        c = src[j]
        if c == '\\' and j + 1 < n:
            nxt = src[j + 1]
            buf.append({'n': '\n', 't': '\t', 'r': '\r', '"': '"', '\\': '\\', '0': '\0'}.get(nxt, nxt))
            j += 2
            continue
        if c == '"':
            return j + 1, ''.join(buf)
        buf.append(c)
        j += 1
    return n, ''.join(buf)


def _read_verbatim(src, i):
    """i 指向开引号（`@"` 的 `"`）；`""` = 一个引号。"""
    n = len(src)
    j = i + 1
    buf = []
    while j < n:
        c = src[j]
        if c == '"':
            if j + 1 < n and src[j + 1] == '"':
                buf.append('"')
                j += 2
                continue
            return j + 1, ''.join(buf)
        buf.append(c)
        j += 1
    return n, ''.join(buf)


def lex_cs(src):
    """返回 (code, lits)。

    code: 与 src 等长的字符串，**注释与字符串内容都换成空格**（保留换行，行号不变）
          但字符串的**起始位置**保留成 `"`（这样按位置就能对上 lits）。
    lits: {起始下标: Lit}
    """
    n = len(src)
    code = list(src)
    lits = {}
    i = 0

    def blank(a, b):
        for k in range(a, b):
            if code[k] != '\n':
                code[k] = ' '

    while i < n:
        c = src[i]

        # --- 注释 ---
        if c == '/' and i + 1 < n and src[i + 1] == '/':
            j = src.find('\n', i)
            j = n if j < 0 else j
            blank(i, j)
            i = j
            continue
        if c == '/' and i + 1 < n and src[i + 1] == '*':
            j = src.find('*/', i + 2)
            j = n if j < 0 else j + 2
            blank(i, j)
            i = j
            continue

        # --- 字符字面量 'x'（避免里面的 " 被当成串） ---
        if c == "'":
            j = i + 1
            while j < n:
                if src[j] == '\\':
                    j += 2
                    continue
                if src[j] == "'":
                    j += 1
                    break
                if src[j] == '\n':
                    break
                j += 1
            blank(i, j)
            i = j
            continue

        # --- C# 11 原始串 """...""" ---
        m = _RAW_RE.match(src, i)
        if m and m.group(2).startswith('"""'):
            q = m.group(2)
            close = src.find(q, i + len(m.group(0)))
            end = n if close < 0 else close + len(q)
            body = src[i + len(m.group(0)):close if close >= 0 else n]
            kind = 'raw'
            value = None if ('{' in body and '$' in m.group(0)) else body
            lits[i] = Lit(i, end, value, kind)
            blank(i + len(m.group(0)), end)
            i = end
            continue

        # --- 字符串（可带 $ / @ 前缀） ---
        if c == '"' or (c in '$@' and i + 1 < n and src[i + 1] == '"') or \
           (c in '$@' and i + 2 < n and src[i + 1] in '$@' and src[i + 2] == '"'):
            pref = ''
            q = i
            if c in '$@':
                pref += c
                q = i + 1
                if q < n and src[q] in '$@':
                    pref += src[q]
                    q += 1
            verbatim = '@' in pref
            interp = '$' in pref
            if verbatim:
                end, value = _read_verbatim(src, q)
            else:
                end, value = _read_plain(src, q)
            kind = ('interp' if interp else 'verbatim' if verbatim else 'plain')
            if interp and '{' in value:
                value = None       # 有插值洞 ⇒ 不能当常量
            lits[i] = Lit(i, end, value, kind)
            # 起始位置保留引号，内容抹掉
            blank(q + 1, end)
            i = end
            continue

        i += 1

    return ''.join(code), lits


def _next_non_space(code, p):
    n = len(code)
    while p < n and code[p] in ' \t\r\n':
        p += 1
    return p


def _arg_span(code, open_paren):
    """open_paren 指向 '('；返回各实参在 code 里的 [start, end) 区间（顶层逗号切分）。"""
    n = len(code)
    depth = 0
    spans = []
    start = open_paren + 1
    i = open_paren
    while i < n:
        ch = code[i]
        if ch in '([{':
            depth += 1
        elif ch in ')]}':
            depth -= 1
            if depth == 0:
                spans.append((start, i))
                return spans
        elif ch == ',' and depth == 1:
            spans.append((start, i))
            start = i + 1
        i += 1
    return spans


def _arg_literal(code, lits, span, idx=0):
    """第 idx 个实参若是一个**完整**的字符串字面量 ⇒ (value, lit)；否则 (None, reason)。"""
    if idx >= len(span):
        return None, "实参不足"
    a, b = span[idx]
    p = _next_non_space(code, a)
    q = b
    while q > p and code[q - 1] in ' \t\r\n':
        q -= 1
    lit = lits.get(p)
    if lit is None or lit.end > b:
        # 不是从实参开头起的字面量
        raw = code[a:b].strip()
        if not raw:
            return None, "空实参"
        return None, _reason(raw)
    # 字面量之后必须只剩空白（否则是拼接 / 方法调用等）
    tail = code[lit.end:b].strip()
    if tail:
        return None, _reason((lit.value or '?') + tail)
    if lit.value is None:
        return None, "插值字符串（含 {…}）"
    return lit.value, None


def _reason(raw):
    if raw.startswith('nameof('):
        return "nameof(...)"
    if raw.startswith('$"') or raw.startswith('$@"') or raw.startswith('@$"'):
        return "插值字符串"
    if '+' in raw:
        return "字符串拼接"
    if raw.startswith('"'):
        return "字面量后接表达式"
    return "常量/变量/方法调用"


# ---------------------------------------------------------------- 名字 → 文件
def slug(s):
    """与 `CardArt.Slug` 逐字一致：小写；只留字母数字，其余换 `_`。"""
    out = []
    for ch in s.lower():
        out.append(ch if ch.isalnum() else '_')
    return ''.join(out)


def sanitize(s):
    """与 `CardArt.Sanitize` 逐字一致（进攻卡阵营名）。"""
    out = []
    last_us = False
    for ch in s:
        if ch.isalnum() and ord(ch) < 128:
            out.append(ch)
            last_us = False
        elif not last_us and out:
            out.append('_')
            last_us = True
    while out and out[-1] == '_':
        out.pop()
    return ''.join(out)


def load_portrait_alias(cardart_path):
    """从 CardArt.cs 的 `_portraitAlias` 表里现读别名（表改了这里自动跟上；读不到就空表）。"""
    alias = {}
    try:
        raw = io.open(cardart_path, 'rb').read().decode('utf-8', 'replace')
    except OSError:
        return alias
    i = raw.find('_portraitAlias')
    if i < 0:
        return alias
    j = raw.find('};', i)
    block = raw[i: j if j > 0 else i + 4000]
    for k, v in re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}', block):
        alias[k] = v
    return alias


def transforms_for(name, transform, alias, second=None):
    """名字 → 候选主名列表（按查找顺序）。"""
    if transform is None:
        return [name]
    if transform == "menu_space":
        cands = [name]
        sp = name.replace(' ', '_')
        if sp != name:
            cands.append(sp)
        return cands
    if transform == "sdf_suffix":
        return [name + "_sdf"]
    if transform == "art_slug":
        key = alias.get(name, name)
        return ["art_" + slug(key)]
    if transform == "alt_slug":
        return ["alt_" + slug(name)]
    if transform == "offensive":
        if second is None:
            return None
        try:
            idx = int(second)
        except ValueError:
            return None
        return [sanitize(name) + ("_empty" if idx < 0 else "_c" + str(idx))]
    return [name]


# ---------------------------------------------------------------- 扫描
CALL_RE = re.compile(r'(?<![A-Za-z0-9_])CardArt\.([A-Za-z_][A-Za-z0-9_]*)\s*\(')
HUD_RE = re.compile(r'(?<![A-Za-z0-9_])HudImage\s*\(')


def iter_cs_files(assets):
    for root, dirs, files in os.walk(assets):
        dirs[:] = [d for d in dirs if d not in ('.git', 'Library', 'Temp', 'obj')]
        for f in files:
            if f.endswith('.cs'):
                yield os.path.join(root, f)


def rel(path, base):
    try:
        return os.path.relpath(path, base).replace('\\', '/')
    except ValueError:
        return path.replace('\\', '/')


def build_index(art_dir, subdirs):
    idx = {}
    for sd in subdirs:
        d = os.path.join(art_dir, sd)
        if not os.path.isdir(d):
            continue
        names = {}
        try:
            for f in sorted(os.listdir(d)):
                if f.lower().endswith('.png'):
                    names.setdefault(f[:-4].lower(), f)
        except OSError:
            continue
        idx[sd] = names
    return idx


def find_file(idx, roots, stems):
    for sd in roots:
        names = idx.get(sd)
        if not names:
            continue
        for st in stems:
            hit = names.get(st.lower())
            if hit:
                return sd, hit
    return None, None


class Row(object):
    __slots__ = ("name", "where", "status", "expect", "note", "kind")
    def __init__(self, name, where, status, expect, note, kind):
        self.name = name
        self.where = where
        self.status = status   # ✅ / 🟠 / ❓
        self.expect = expect
        self.note = note
        self.kind = kind       # port / dict / unresolved


def scan(assets, art_dir, alias):
    subdirs = sorted([d for d in os.listdir(art_dir) if os.path.isdir(os.path.join(art_dir, d))])
    idx = build_index(art_dir, subdirs)

    rows = []
    skipped = 0
    files = sorted(iter_cs_files(assets))

    for path in files:
        try:
            raw = io.open(path, 'rb').read()
        except OSError:
            continue
        src = raw.decode('utf-8', 'replace')
        if '\ufffd' in src and b'\xef\xbf\xbd' not in raw:
            # 不是 UTF-8 的极少见文件：按 latin-1 兜底，保证不崩
            src = raw.decode('latin-1')
        code, lits = lex_cs(src)
        relp = rel(path, assets)
        nl = [k for k, ch in enumerate(src) if ch == '\n']
        row0 = len(rows)

        # --- CardArt.* 口 ---
        for m in CALL_RE.finditer(code):
            method = "CardArt." + m.group(1)
            if method not in PORT_BY_METHOD:
                continue
            roots, transform = PORT_BY_METHOD[method]
            open_p = m.end() - 1
            spans = _arg_span(code, open_p)
            name, why = _arg_literal(code, lits, spans, 0)
            second = None
            if transform == "offensive":
                second, why2 = _arg_literal(code, lits, spans, 1)
                if second is None:
                    name = None
                    why = why or why2 or "第二实参不可解析"
            line = _line_at(nl, m.start())
            where = "%s:%d" % (relp, line)
            if name is None:
                rows.append(Row("?", where, "❓", "-", why or "无法静态解析", "unresolved"))
                continue
            stems = transforms_for(name, transform, alias, second)
            if stems is None:
                rows.append(Row(name, where, "❓", "-", "第二实参不可解析", "unresolved"))
                continue
            sd, fn = find_file(idx, roots, stems)
            expect = "Resources/Art/%s/%s.png" % (roots[0], stems[0])
            if fn:
                rows.append(Row(name, where, "✅", "Resources/Art/%s/%s" % (sd, fn), "", "port"))
            else:
                rows.append(Row(name, where, "🟠", expect, "图不存在（%s 档全miss）" % "/".join(roots), "port"))

        # --- BattleDriver.HudImage（实例方法，图名是第 2 个实参） ---
        for m in HUD_RE.finditer(code):
            before = code[max(0, m.start() - 40):m.start()]
            if re.search(r'(?:HudPiece|static|void|private|public|internal)\s*$', before):
                skipped += 1           # 定义行，不是引用
                continue
            line = _line_at(nl, m.start())
            where = "%s:%d" % (relp, line)
            spans = _arg_span(code, m.end() - 1)
            name, why = _arg_literal(code, lits, spans, 1)
            if name is None:
                rows.append(Row("?", where, "❓", "-", "HudImage 第2实参：" + (why or "不可解析"), "unresolved"))
                continue
            sd, fn = find_file(idx, ["ui"], [name])
            expect = "Resources/Art/ui/%s.png" % name
            if fn:
                rows.append(Row(name, where, "✅", "Resources/Art/%s/%s" % (sd, fn), "", "port"))
            else:
                rows.append(Row(name, where, "🟠", expect, "HudImage → Art/ui 不存在", "port"))

        # --- WindowButton 两张字典 ---
        if relp.endswith("Shell/WindowButton.cs"):
            for dict_name in ("HoverNames", "PressedNames"):
                rows.extend(scan_dict(src, code, lits, nl, relp, dict_name, idx))

        # --- `??` 兜底链合并（见 merge_null_chains 的注释）---
        merge_null_chains(rows[row0:], src)

    # --- `_deckArtById` 表（`TryDeckHero` 取图的唯一落点）---
    rows.extend(scan_deck_art_table(assets, art_dir, idx))

    return rows, skipped, files, subdirs


def merge_null_chains(rows, src):
    """`CardArt.Ui("x") ?? CardArt.MenuUi("x") ?? CardArt.DeckUi("x")` —— 三级兜底链。

    这三条**各自**只有一条得中时，前两条单看必然 miss，但整条表达式是好的。
    规则：**同一源码行 + 同一个名字**里既有 ✅ 又有 🟠，且那一行含 `??` ⇒ 🟠 改判 ✅
    （`expect` 写明「同行 fallback 链上有图」）。名字不同的调用不会互相掩盖。
    """
    lines = src.split('\n')
    groups = {}
    for r in rows:
        if r.status not in ("✅", "🟠") or r.name == "?":
            continue
        try:
            line = int(r.where.rsplit(":", 1)[1])
        except ValueError:
            continue
        groups.setdefault((line, r.name), []).append(r)
    for (line, _name), rs in groups.items():
        st = set(r.status for r in rs)
        if "✅" not in st or "🟠" not in st:
            continue
        text = lines[line - 1] if 0 <= line - 1 < len(lines) else ""
        if "??" not in text:
            continue
        for r in rs:
            if r.status == "🟠":
                r.status = "✅"
                r.expect = "同行的 ?? fallback 链上有图（本条端口单独不命中）"
                r.note = "chain"


def scan_deck_art_table(assets, art_dir, idx):
    """`CardArt._deckArtById` 的 56 条「督军 id → 原版整身立绘资产名」也是代码里的字面量引用。

    `TryDeckHero` 走 `MenuUi(art.Replace(' ', '_'))`（三档兜底）⇒ 按同一口径查。
    ⚠️ 这些名字**不是** `CardArt.Xxx("...")` 形式，所以上面那趟正则扫不到，单列在这里。
    """
    out = []
    path = os.path.join(assets, "CardPresentation", "Core", "CardArt.cs")
    try:
        raw = io.open(path, "rb").read().decode("utf-8", "replace")
    except OSError:
        return out
    i = raw.find("_deckArtById")
    if i < 0:
        return out
    j = raw.find("};", i)
    block = raw[i: j if j > 0 else i + 8000]
    relp = rel(path, assets)
    nl = [k for k, ch in enumerate(raw) if ch == "\n"]
    for km in re.finditer(r'\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}', block):
        warlord, art = km.group(1), km.group(2)
        line = _line_at(nl, i + km.start())
        where = "%s:%d" % (relp, line)
        stems = transforms_for(art, "menu_space", {}, None)
        sd, fn = find_file(idx, ["ui_menu", "ui_deck", "ui"], stems)
        if fn:
            out.append(Row(art, where, "✅", "Resources/Art/%s/%s" % (sd, fn), warlord, "table:DeckArtById"))
        else:
            out.append(Row(art, where, "🟠", "Resources/Art/ui_menu/%s.png" % stems[0],
                           "督军 " + warlord + " 的整身立绘不在盘上（TryDeckHero 会退回 Portrait 并出声）",
                           "table:DeckArtById"))
    return out


def scan_dict(src, code, lits, nl, relp, dict_name, idx):
    out = []
    m = re.search(r'\b' + dict_name + r'\b', code)
    if not m:
        return out
    # 从字典声明往后找配对的 {...}
    p = code.find('{', m.end())
    if p < 0:
        return out
    depth = 0
    q = p
    while q < len(code):
        if code[q] == '{':
            depth += 1
        elif code[q] == '}':
            depth -= 1
            if depth == 0:
                break
        q += 1
    body = code[p:q + 1]
    for km in re.finditer(r'\{\s*"', body):
        # 找到这一条的起点，再按顶层逗号切两格
        s = p + km.start()              # 's' 指向这一条的 '{'
        line = _line_at(nl, s)
        # 从 's' 开始找配对的 '}'
        e = s
        depth = 0
        while e < len(code):
            ch = code[e]
            if ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
                if depth == 0:
                    break
            e += 1
        parts = _split_top(code, s + 1, e)
        if len(parts) < 2:
            continue
        key, _ = _arg_literal(code, lits, [(parts[0][0], parts[0][1])], 0)
        val, _ = _arg_literal(code, lits, [(parts[1][0], parts[1][1])], 0)
        where = "%s:%d" % (relp, line)
        for nm in (key, val):
            if nm is None:
                out.append(Row("?", where, "❓", "-", "字典 " + dict_name + " 里有非字面量", "unresolved"))
                continue
            stems = transforms_for(nm, "menu_space", {}, None)
            sd, fn = find_file(idx, ["ui_menu", "ui_deck", "ui"], stems)
            expect = "Resources/Art/ui_menu/%s.png" % stems[0]
            if fn:
                out.append(Row(nm, where, "✅", "Resources/Art/%s/%s" % (sd, fn),
                               "", "dict:" + dict_name))
            else:
                out.append(Row(nm, where, "🟠", expect,
                               "字典 " + dict_name + " → MenuUi 三档全miss", "dict:" + dict_name))
    return out


def _split_top(code, a, b):
    """在 [a,b) 里按顶层逗号切分（花括号/圆括号/方括号都算深度）。"""
    parts = []
    depth = 0
    start = a
    i = a
    while i < b:
        ch = code[i]
        if ch in '([{':
            depth += 1
        elif ch in ')]}':
            depth -= 1
        elif ch == ',' and depth == 0:
            parts.append((start, i))
            start = i + 1
        i += 1
    parts.append((start, b))
    return parts


# ---------------------------------------------------------------- 输出
def write_tsv(rows, path):
    d = os.path.dirname(path)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    lines = ["名字\t引用点\t是否找到\t候选路径/备注"]
    seen = set()
    for r in rows:
        key = (r.name, r.where, r.status)
        if key in seen:
            continue
        seen.add(key)
        note = r.expect if r.status != "🟠" else r.expect
        if r.status == "❓":
            note = r.note
        lines.append("%s\t%s\t%s\t%s" % (r.name, r.where, r.status, note))
    data = ("\n".join(lines) + "\n").encode("utf-8")
    io.open(path, "wb").write(data)


# 故意「引用一个不存在的图」的负例夹具：它们**就是要**取不到图，用来验缺图时的行为。
# （`B118W14_Assert` 验「缺件只出声一次」；`C4_Assert` 验「按下图缺 → 退回高亮图」。）
# ⛔ 这不是给真缺口开后门 —— 只有这两个名字、且在 Editor 自检文件里出现时才是夹具。
DEFAULT_IGNORE = {
    "b118w14_missing_probe",
    "40k_UI_bt_back_pressed__C4_not_here",
}


def main(argv=None):
    ap = argparse.ArgumentParser(description="B146-S7 资源引用闸（只读；--write 才落盘）")
    ap.add_argument("--assets", default=DEFAULT_ASSETS, help="Assets 根（默认 %s）" % DEFAULT_ASSETS)
    ap.add_argument("--art", default=None, help="Resources/Art 目录（默认 <assets>/CardPresentation/Resources/Art）")
    ap.add_argument("--tsv", default=DEFAULT_TSV, help="--write 时的落盘路径")
    ap.add_argument("--write", action="store_true", help="把结果写成 TSV（默认不写盘）")
    ap.add_argument("--verbose", action="store_true", help="连 ✅ 也逐条打印")
    ap.add_argument("--fail-on-missing", action="store_true", help="🟠 > 0 时退出码 = 1")
    ap.add_argument("--list-dirs", action="store_true", help="只列 Resources/Art 现读子目录后退出")
    ap.add_argument("--ignore", default="", help="额外忽略的名字（逗号分隔；用于负例夹具）")
    ap.add_argument("--no-default-ignore", action="store_true", help="连内置的负例夹具忽略表也不生效")
    ap.add_argument("--max-unknown", type=int, default=40, help="打印 ❓ 清单的上限（--verbose 全列）")
    args = ap.parse_args(argv)

    assets = os.path.abspath(args.assets)
    art_dir = args.art or os.path.join(assets, DEFAULT_ART_REL)
    if not os.path.isdir(art_dir):
        sys.stderr.write("ERROR: 找不到 Resources/Art: %s\n" % art_dir)
        return 2
    if args.list_dirs:
        for d in sorted(os.listdir(art_dir)):
            full = os.path.join(art_dir, d)
            if os.path.isdir(full):
                n = len([f for f in os.listdir(full) if f.lower().endswith('.png')])
                print("%-16s %4d png" % (d, n))
        return 0

    alias = load_portrait_alias(os.path.join(assets, "CardPresentation", "Core", "CardArt.cs"))
    rows, skipped, files, subdirs = scan(assets, art_dir, alias)
    if not subdirs:
        sys.stderr.write("ERROR: Resources/Art 下没有子目录？\n")
        return 2

    ignore = set() if args.no_default_ignore else set(DEFAULT_IGNORE)
    ignore |= set(s.strip() for s in args.ignore.split(",") if s.strip())
    ignored = [r for r in rows if r.name in ignore]
    rows = [r for r in rows if r.name not in ignore]

    ok = [r for r in rows if r.status == "✅"]
    bad = [r for r in rows if r.status == "🟠"]
    unk = [r for r in rows if r.status == "❓"]

    print("=== B146-S7 资源引用闸 ===")
    print("Assets : %s" % assets)
    print("Art    : %s  (现读子目录 %d 个)" % (art_dir, len(subdirs)))
    print("代码   : %d 个 .cs" % len(files))
    print("-----------------------------------------------------------")
    print("✅ 找到              : %4d 处 / %3d 个不同名字" % (len(ok), len(set(r.name for r in ok))))
    print("🟠 引用但无文件        : %4d 处 / %3d 个不同名字" % (len(bad), len(set(r.name for r in bad))))
    print("❓ 无法静态解析        : %4d 处" % len(unk))
    print("—  非引用(定义/注释)   : %4d 处" % skipped)
    if ignored:
        print("—  忽略(负例夹具)      : %4d 处 / %3d 个名字" % (len(ignored), len(set(r.name for r in ignored))))
    print("-----------------------------------------------------------")

    def dump(title, items, verbose_ok=False):
        if not items:
            return
        print("\n" + title)
        seen = set()
        shown = 0
        limit = 10 ** 9 if (args.verbose or verbose_ok) else args.max_unknown
        for r in items:
            key = (r.name, r.where, r.status)
            if key in seen:
                continue
            seen.add(key)
            if shown >= limit:
                continue
            shown += 1
            if r.status == "❓":
                print("  %-52s %-48s %s" % (r.name, r.where, r.note))
            elif r.status == "🟠":
                print("  %-52s %-48s %s" % (r.name, r.where, r.expect))
            else:
                print("  %-52s %-48s %s" % (r.name, r.where, r.expect))
        if len(seen) > shown:
            print("  … 还有 %d 处（加 --verbose 全列）" % (len(seen) - shown))

    dump("🟠 引用但无文件（名字 | 引用点 | 期望路径）", bad)
    dump("❓ 无法静态解析（名字 | 引用点 | 原因）", unk)
    if args.verbose:
        dump("✅ 找到（名字 | 引用点 | 实际文件）", ok)

    if args.write:
        write_tsv(rows, args.tsv)
        print("\n[write] TSV → %s" % args.tsv)
    else:
        print("\n(未写盘；需要 TSV 请加 --write)")

    if args.fail_on_missing and bad:
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
