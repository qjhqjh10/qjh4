#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""check_ledger_points.py — 「完全复刻」账本 **A 档落点** 的机检（默认纯只读）。

⛔ **本闸只把「落点形态」摆出来，判据仍要人读** —— 它证明不了「这一行实现了那个原版方法」，
   只能证明「这一行是个像样的声明 / 非空数据行」。⛔ **不做「全仓唯一同名方法」兜底**
   （那会把「落点写错文件」洗成通过）。

四类判法（2026-10-25 系统性加固；**互斥**，优先级 = 失败 > 漂移 > 告警 > 通过）
----------------------------------------------------------------------------
1) **失败**（退出码 1）—— 真的错。任一条即算：
   · 落点里没有 `<文件>:<行号>`；文件不存在；行号越界；
   · 那一行**既不像声明也不是非空数据行**（注释行 / 空行 / 语句行 / 只有 `{` `}` 的行）；
   · 括号里的名字**能确定**（见下节），但文件里根本没有这份声明；
   · 括号里的名字**和那一行声明的名字明显对不上**
     （例：`…:764（Refresh 方法）`，而 764 行声明的是 `SelectArmy` ⇒ 失败）。
2) **漂移**（**不算失败**，退出码不变）—— 名字找得到、但行号是旧快照：
   · 唯一命中且 ≠ 账里的行号 ⇒ 漂移；`--fix` 可改，
     ⛔ **目标行是 getter / `*ForTest` 时不自动改**（历史事故：`--fix` 把漂移改到一个属性上）；
   · 多条声明命中、且账里那行**不在**其中 ⇒ 「歧义」漂移，⛔ 一律不自动改。
3) **告警**（**不算失败**，退出码不变；`--warn-getters` 印明细，默认只印计数）—— 候选，**必须人读**：
   · **GETTER**：落点那一行是一行式属性 getter / 表达式体属性
     （`public X Y { get { return KwValue(…); } }`）。
     有些原版方法**本身就是**属性 / 取值器（`get_Rarity` / `IsFlying`）⇒ ⛔ **不许一刀切判错**。
   · **FORTEST**：落点（或括号里的名字）是 `*ForTest` 一类**只转发 / 供自检**的成员。
   · **SELFREF**：那一行**既声明又调用了同名方法**
     （`void SetHint(string s) { SetHint(s, false); }`）⇒ 那是转发口，不是实现。
   · **PROSE**：括号是纯散文（提不出标识符）/ 含多个标识符 / 该行声明名取不出来
     ⇒ 名字**没法对**，只提示、⛔ 不判失败。
   ⚠️ **「告警」是「通过」之下的子集**：合计里的 **通过数包含告警行**
     （告警只说明「这条通过得可疑」，不改变通过/失败的判法）。
4) **通过** —— 上面三条都不沾。

括号里的名字怎么取（**第 4 条盲点：散文括号不验同名**的修法）
--------------------------------------------------------------
**先看括号前面有没有名字**（账里 36 行是这个形状，实测 2026-10-25）：
`…:642 ShowError(text, …)` / `…:750 Show（关窗钮 + 全屏透明 _bgClose）`
—— 行号后面紧跟一个标识符、再紧跟 `(` / `（` ⇒ **它就是声明名**，
比那一行声明的是什么（`decl_name_at`）；对不上就是**失败**。
有它的时候括号里的内容只是散文 / 参数，⛔ 不再拿括号里的词当名字（否则 `_bgClose` 会被误判）。

括号前面没有名字时，取落点里**第一个**括号组，剥掉反引号 / 引号，然后：
  · 整组就是一个标识符 ⇒ **严格**按它比（`（Dispatch）` / `` （`Dispatch`） ``）；
  · 含中文标点 / 散文 ⇒ 按「非 ASCII 词字符」切词，取其中的 ASCII 标识符
    （`（Refresh 方法）` ⇒ `Refresh`；中英文标点都算分隔）：
      - **恰好 1 个** ⇒ **严格**比 —— 和**那一行声明的名字**（`decl_name_at`）比，
        对不上就是**失败**（这就是第 4 条盲点那种写法）；
      - **2 个以上** ⇒ 只**告警**（PROSE）—— 散文里提到多个名字，机器分不清指哪个，⛔ 不判失败；
      - **0 个** ⇒ 只**告警**（PROSE）。
  · ⚠️ 短 token（<3 字符，如 `x` / `f`）在散文里当噪声丢掉，别拿它判失败。
  · 声明名取不出来（`decl_name_at` 返回 None）⇒ 退回「只验那行像不像声明」，不判失败。

栈内其他两条加固（同一天 5 个代理各自撞到的盲点）
------------------------------------------------
  · **「一行内含声明 + 调用」**：`void Awake() { Build(); }` 曾被算成「`Build` 的声明」
    ⇒ `find_decl_candidates` 现在要求**名字在本行第一个 `{` 之前**
    （`{` 之后是该成员的实现体，里面出现的同名标识符是**调用**）。
  · **调用行被当成声明**：`public void UiScroll(float dy) { …; RefreshDeckList(); }`
    —— 同一个闸门一并挡掉（同上「`{` 之后」判定），不再需要按名字猜。

`--fix` 只改写「唯一命中 + 目标行不是 getter / `*ForTest` / **能解析成声明**」的漂移行号（默认不写）。
目标行解析不出声明名时，漂移理由里会打 **`NEED-HUMAN`** 并**跳过**（不写），请人读。

🔴 **`.cs` 一律按二进制读**（`read_src_lines`）：文本模式的 universal newlines 会把**裸 `\\r`** 当换行
   ⇒ 文件里只要有一个 `\\r\\r\\n`，后面所有行号整体偏移（实测 `DeckRules.cs` 偏 +7 ⇒ 15 行账被改错且闸变绿）。

用法
----
    python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py [账本...] [--all-A] [--fix] [--root DIR] [--warn-getters]

`--warn-getters`（2026-10-25 加，**只报、不算失败、退出码不变**）
------------------------------------------------------------------
把四类告警（GETTER / FORTEST / SELFREF / PROSE）的**明细**打出来（默认只在合计行给计数 + 按账分布）。
名字保留是为了兼容旧命令行。

**为什么单开一档**：形态闸原有判据把「一行式成员」当合法声明，于是
`BattleDriver.cs:6290`（`SettingsBtnHitPx` 的 `get { return new Vector2(…); }`）
这种**纯 getter** 也能当 A 的落点 —— 而它显然不是「实现那个原版方法」。
2026-10-25 的 HUD 域复查里，**大量旧落点**都是这一类。

⛔ **只报候选，判据仍要人读**：有些原版方法本身就是属性 / 取值器（例如
`get_Rarity`、`IsFlying`），它们的落点**就该**是 getter ⇒ **不能一刀切判错**。
本档只把「A 行的落点是个 getter」这件事摆出来，是人复核的入口，不是结论。

退出码：0 = 无失败（漂移 / 告警都不算）；1 = 有失败；2 = 账本路径给错。

回归自测：`python -I -X utf8 d:/4/Unity/工具/check_ledger_points_selftest.py`
"""
import glob
import io
import os
import re
import sys

LEDGER_DIR = r"D:/4/Unity/资料/完全复刻/账"
DEFAULT_ROOTS = [
    r"D:/4/Unity/MyGame/Assets",
    r"D:/4/Unity/MyGame/Assets/CardPresentation",
    r"D:/4/Unity",
]
NL = chr(10)
REF = re.compile(r"([\w\-./\\]+\.(?:cs|json|tsv|asset)):(\d+)")
DECL_TAIL = re.compile(r"\)\s*(?:\{|=>|//|where\b|:|$)")
MODS = ("public", "private", "protected", "internal")
STMT = ("return ", "var ", "new ", "if ", "while ", "for ", "foreach ", "switch ", "case ",
        "else ", "else{", "using ", "lock ", "try", "catch", "finally", "throw ", "await ",
        "yield ")

# 告警的四种形态（**都只报、不算失败**）
WARN_GETTER = "GETTER"
WARN_FORTEST = "FORTEST"
WARN_SELFREF = "SELFREF"
WARN_PROSE = "PROSE"
WARN_TITLE = {
    WARN_GETTER: "落点那一行是属性 getter（原版可能就是属性 ⇒ 人读）",
    WARN_FORTEST: "落点是 `*ForTest` 自检 / 转发口（不是实现）",
    WARN_SELFREF: "那一行既声明又调用了同名方法（转发口，不是实现）",
    WARN_PROSE: "括号是散文 ⇒ 名字没法对（只提示、不判失败）",
}

_IDENT = re.compile(r"[A-Za-z_]\w*")
_ATTR = re.compile(r"^(?:\[[^\]]*\]\s*)+")
_NOT_DECL_HEAD = re.compile(
    r"^(?:get|set|add|remove|return|throw|await|yield|case|default|if|else|for|foreach|while|do|"
    r"switch|using|lock|try|catch|finally)\b")


def find_file(rel, roots):
    rel = rel.replace("\\", "/")
    for c in [rel] + [os.path.join(r, rel) for r in roots]:
        if os.path.isfile(c):
            return c
    return None


def looks_like_decl(line):
    s = line.strip()
    if not s:
        return False, "空行"
    if s.startswith("//") or s.startswith("*") or s.startswith("/*"):
        return False, "注释行"
    # 只有括号 / 分隔符的行（`{` / `}` / `};` / `);`）不是声明
    if re.fullmatch(r"[{}\(\);,\s]+", s):
        return False, "只有括号/分隔符（不是声明）"
    if re.match(r"^[}\])]*\s*(?:else|catch|finally)\b", s):
        return False, "语句块续行（else/catch/finally，不是声明）"
    if not ("(" in s or "=>" in s or "{" in s):
        return False, "既无 `(` 也无 `=>`/`{`（不像声明）"
    if s.endswith(";") and s.startswith(MODS):
        return True, ""
    if "(" in s and ")" in s and not DECL_TAIL.search(s):
        first = s.index("(")
        if s.startswith(MODS) and "{" in s[:first]:
            return True, ""
        return False, "`)` 后接的是语句（像函数中间的一行）"
    return True, ""


def decl_name_at(line):
    """从一行 `.cs` 里取**被声明的成员名**；拿不准 ⇒ None（宁可不判，⛔ 不猜）。

    认：方法 / 属性 / 表达式体属性 / 字段 / class·struct·interface·enum·record。
    不认：语句行、`get`/`set` 续行、成员访问（`a.B(`）、只有 `{` 的续行。
    """
    s = line.strip()
    if not s or s.startswith("//") or s.startswith("*") or s.startswith("#"):
        return None
    s = re.split(r"\s//", s)[0].strip()          # 行尾注释（保守：只砍 ` //`）
    s = _ATTR.sub("", s).strip()                  # 前置特性 [Foo]
    if _NOT_DECL_HEAD.match(s):
        return None
    cut = len(s)
    for tok in ("{", "=>", ";"):                  # 成员名一定在这些之前
        i = s.find(tok)
        if 0 <= i < cut:
            cut = i
    head = s[:cut].strip()
    eq = head.find("=")                           # `=` 之后是初始化表达式（`=>` 已在上面切掉）
    if eq >= 0:
        head = head[:eq].strip()
    if not head:
        return None
    m = re.search(r"\b(class|struct|interface|enum|record)\s+(\w+)", head)
    if m:
        return m.group(2)
    if "(" in head:
        pre = re.sub(r"<[^<>]*>", "", head[:head.index("(")].rstrip()).rstrip()
        if re.search(r"\.\s*[A-Za-z_]\w*$", pre):  # `a.B(` = 调用，不是声明
            return None
        m = re.search(r"([A-Za-z_]\w*)$", pre)
        # `RefreshDeckList()` 这种**裸调用行**：名字前面什么都没有（没有返回类型 / 修饰符）⇒ 不是声明
        if not m or m.start() == 0:
            return None
        return m.group(1)
    if re.search(r"\.\s*[A-Za-z_]\w*$", head):     # `a.B` = 成员访问
        return None
    m = re.search(r"([A-Za-z_]\w*)\s*$", head)
    return m.group(1) if m else None


def paren_idents(land):
    """取落点里**第一个**括号组，返回 `(names, mode)`。

    mode = 'exact'（整组就是一个标识符）| 'prose'（散文 / 多个 / 提不出）| 'none'（没括号组）。
    散文里 <3 字符的 token 当噪声丢掉（`x` / `f` 这种不该拿它判失败）。
    """
    st = re.search(r"[（(]", land)
    if not st:
        return [], "none"
    en = re.search(r"[）)]", land[st.end():])
    if not en:
        return [], "none"
    g = land[st.end():st.end() + en.start()].strip().strip("`'\" ").strip()
    if re.fullmatch(r"[A-Za-z_]\w*", g):
        return [g], "exact"
    return [t for t in _IDENT.findall(g) if len(t) >= 3], "prose"


def head_name(land, m):
    """`…:642 ShowError(text, …)` / `…:750 Show（…）` —— 名字写在**括号之前**的那种写法。

    账里 36 行是这个形状（实测 2026-10-25）。`m` = `REF.search(land)` 的 match。
    ⛔ 只认「行号后面紧跟的标识符 + 紧跟 `(` / `（`」，别把散文里的词当名字。
    """
    if not m:
        return None
    mm = re.match(r"\s*`?([A-Za-z_]\w*)`?\s*[（(]", land[m.end():])
    return mm.group(1) if mm else None


def decl_name_in(land):
    """兼容旧签名：**恰好一个**可严格比较的名字 ⇒ 返回它，否则 None。"""
    names, mode = paren_idents(land)
    return names[0] if (mode == "exact" or (mode == "prose" and len(names) == 1)) else None


def find_decl_candidates(lines, name):
    """列出所有「名字处于**声明语境**」的行号（1-based）。

    🔴 **2026-10-25 修的三类假阳性**（A 实测抓到，⚠️ 都会让 `--fix` 把落点改得更糟）：
      ① 一行式成员里**调用**了同名方法：`public int Survivor { get { return KwValue(x); } }`
         —— 它「看起来像声明」，但 `KwValue` 是**被调用的**。
      ② 任何 `return KwValue(…)` / `x => KwValue(…)` / `a = KwValue(…)` 这种**表达式里的调用**。
      ③ **一行内含声明 + 调用**：`void Awake() { Build(); }` /
         `void SetHint(string s) { SetHint(s, false); }`
         —— 名字在 `{` **之后**就是那个成员**实现体里的调用**，不是声明。
     闸门（三条都做）：
      · 名字前面不能是 `return` / `=>` / `=` / `.` / `(` / `,`（那是使用处不是声明处）；
      · 名字不能落在 `{` 之后（`{` 起是本行那个成员的实现体）；
      · 名字不能落在花括号之后的一行式成员体里（该行 `{` 之后还有 `get` / `=>`）。
    """
    if not name:
        return []
    pat = re.compile(r"\b" + re.escape(name) + r"\s*(?:\(|<|\{|;|=)")
    out = []
    for i, l in enumerate(lines, 1):
        t = l.strip()
        if t.startswith("//") or t.startswith("*"):
            continue
        mm = pat.search(l)
        if not mm:
            continue
        ok, _why = looks_like_decl(l)
        if not ok:
            continue
        before = l[:mm.start()]
        # ① 声明语境：名字前面不能是「使用」的引导符
        if re.search(r"(?:return|=>|=|\.|\(|,)\s*$", before):
            continue
        lb = l.find("{")
        # ③ 名字必须在**本行第一个 `{` 之前** —— `{` 之后是这个成员的实现体，
        #    里面出现的同名标识符是**调用**（`void Awake() { Build(); }`）。
        if lb >= 0 and mm.start() > lb:
            continue
        # ② 不能在一行式成员的**体内**（`{` 之后、且这一行里有 get/=>）
        if lb >= 0 and mm.start() > lb and ("get" in l[lb:] or "=>" in l[lb:]):
            continue
        if t.startswith(STMT):
            continue
        # ④ **这一行必须真的声明了这个名字**（2026-10-25 R1 加）：
        #    `CleanupDeaths(ctx, owner, slot, killer, …` 这种**跨行调用**（本行没有 `)`）
        #    会被 `looks_like_decl` 放行（它只在「有 `(` 也有 `)`」时才查 DECL_TAIL）⇒ 假候选。
        #    判据 = `decl_name_at` 取不出同名 ⇒ 不是声明（它本来就拒裸调用：名字在行首 ⇒ None）。
        if decl_name_at(l) != name:
            continue
        out.append(i)
    return out


def find_decl_by_name(lines, name):
    """唯一命中才返回行号；0 处或多处 ⇒ None（多义交给 `check_file` 报「歧义」）。"""
    c = find_decl_candidates(lines, name)
    return c[0] if len(c) == 1 else None


def self_reference(line, name):
    """`void SetHint(string s) { SetHint(s, false); }` —— 本行既声明又调用了同名方法。"""
    lb = line.find("{")
    if lb < 0:
        return False
    return re.search(r"\b" + re.escape(name) + r"\s*\(", line[lb:]) is not None


# ---------------------------------------------------------------------------
#  告警候选：一行式属性 getter / 表达式体属性（**只报候选，判据仍要人读**）
# ---------------------------------------------------------------------------
_GETTER_BLOCK = re.compile(
    r'^\s*(?:\[[^\]]*\]\s*)*'
    r'(?:(?:public|private|protected|internal|static|virtual|override|sealed|abstract|new|unsafe|extern|partial)\s+)*'
    r'[\w<>\[\],\.\? ]+?\s(\w+)\s*\{[^\n]*\bget\b[^\n]*\breturn\b[^\n]*\}\s*(?://.*)?$')
_GETTER_EXPR = re.compile(
    r'^\s*(?:(?:public|private|protected|internal|static|virtual|override|sealed|abstract|new|unsafe|extern|partial)\s+)*'
    r'[\w<>\[\],\.\? ]+?\s(\w+)\s*=>\s*\S.*;\s*(?://.*)?$')


def one_line_getter(lines, idx):
    """`lines[idx]`（0 基）如果是「属性 getter」⇒ 返回属性名，否则 None。

    认三种形状（**都只报候选**）：
      ① 一行式 getter：`public X Y { get { return …; } }`
      ② 表达式体属性：`public X Y => …;`
      ③ **getter 体那一行**：`            get { return new Vector2(…`  ← 账里最常见的形状
         （名字不在本行 ⇒ 往上最多 30 行找 `public static Vector2 SettingsBtnHitPx` 那样的属性声明）

    ⚠️ 表达式体**方法**（`public int F() => 1;`）不报：名字后面跟 `(` 不跟 `=>`。
    ⚠️ 普通方法体里的 `get` 局部变量之类不会以 `get` 开头，不会误报。
    """
    if idx < 0 or idx >= len(lines):
        return None
    line = lines[idx]
    for rx in (_GETTER_BLOCK, _GETTER_EXPR):
        m = rx.match(line)
        if m:
            return m.group(1)
    if re.match(r'^\s*get\b', line):
        for j in range(idx - 1, max(-1, idx - 31), -1):
            m = re.match(r'^\s*(?:(?:public|private|protected|internal|static|virtual|override|sealed|abstract|new|unsafe|extern|partial)\s+)+'
                         r'[\w<>\[\],\.\? ]+?\s(\w+)\s*(?://.*)?$', lines[j])
            if m:
                return m.group(1)
    return None


# ---------------------------------------------------------------------------
#  目标行「是不是声明」的最终闸 —— `dn_at` 同款正则（照 `工具/verify_annot.py`）
# ---------------------------------------------------------------------------
# 🔴 **2026-10-25 第八十会话批次 1（R1）加**：Q3 报告投诉「工具报的『实际在 N 行』有 18/79
#    落在 `///` 注释 / `{` / `}` / 空行 / 另一个方法上 ⇒ 直接 `--fix` 会改到错的位置、而且闸变绿」。
#    实测当前版本（同日加固后）已经没有那 18 条了，但 `find_decl_candidates` 仍能接受
#    「字段 + 初始化器」这类**解析不出方法名的行**（`static readonly int[] T = new int[]{…};`）
#    ⇒ `--fix` 照样会写上去。所以把「必须能解析成声明」做成最后一道闸：解析不出 ⇒ NEED-HUMAN，不写。
#    判据口径 = `verify_annot.py` 的 `dn_at`（名字后面紧跟 `(` 或 `{`），两处工具**必须同一口径**。
_DN_AT = re.compile(
    r'\s*(?:(?:public|private|protected|internal|static|sealed|override|virtual|async|abstract|'
    r'extern|new|unsafe|partial)\s+)*[\w<>\[\],\.\? ]+?\s(\w+)\s*[\({]')


def dn_at_name(line):
    """`verify_annot.py` 的 `dn_at` 同款：能解析成「声明名 + `(`/`{`」⇒ 返回名字，否则 None。

    ⛔ 拿不准就返回 None —— 调用方按「不是声明」处理（NEED-HUMAN），**不猜**。
    """
    m = _DN_AT.match(line)
    return m.group(1) if m else None


def check_file(path, roots, show_pass=False, do_fix=False):
    """返回 `(total, fails, drifts, rewrites, getters, warns)`。

    `fails`  = (账, 类.方法, 落点, 原因)          —— 真的错
    `drifts` = (账, 类.方法, 落点, 原因)          —— 找到了、行号变了（不算失败）
    `rewrites` = (行下标, 新落点)                 —— 只有 do_fix 且唯一命中时才有
    `getters` = (账, 类.方法, 落点, getter 名)    —— 旧接口（= warns 里 GETTER 那一档）
    `warns`  = (账, 类.方法, 落点, 档位, 说明)    —— 候选，**不算失败**
    """
    base = os.path.basename(path)
    rows = [l.rstrip(NL) for l in io.open(path, encoding="utf-8", newline="") if l.strip()]
    if not rows:
        return 0, [], [], [], [], []
    hdr = rows[0].split("\t")
    try:
        i_status, i_land = hdr.index("状态"), hdr.index("我们的落点")
        i_cls, i_mtd = hdr.index("原版类"), hdr.index("方法")
    except ValueError:
        return 0, [(base, "?", "?", "表头缺列（需要 状态/我们的落点/原版类/方法）")], [], [], [], []

    total = 0
    fails, drifts, rewrites, getters, warns = [], [], [], [], []
    for ri in range(1, len(rows)):
        c = rows[ri].split("\t")
        if len(c) <= max(i_status, i_land, i_cls, i_mtd):
            continue
        if c[i_status].strip() != "A":
            continue
        total += 1
        who = "%s.%s" % (c[i_cls], c[i_mtd])
        land = c[i_land].strip()

        def warn(kind, why):
            """同一行同一种告警只记一次。"""
            key = (ri, kind)
            if key in seen_warn:
                return
            seen_warn.add(key)
            warns.append((base, who, land, kind, why))

        seen_warn = set()

        r = REF.search(land)
        if not r:
            fails.append((base, who, land, "落点里没有 `文件:行`"))
            continue
        rel, ln = r.group(1), int(r.group(2))
        full = find_file(rel, roots)
        if full is None:
            fails.append((base, who, land,
                          "文件不存在（试过 原样 / Assets / Assets/CardPresentation / Unity）：" + rel))
            continue
        try:
            lines = read_src_lines(full)
        except Exception as e:
            fails.append((base, who, land, "读不了文件：" + str(e)))
            continue
        is_cs = os.path.splitext(full)[1].lower() == ".cs"

        # ---- 告警候选**先**记（⛔ 一条都不影响失败判定）----
        #   必须放在「按名重定位 / 像不像声明」之前 —— 否则 `（Foo）` 找不到声明会 `continue`，
        #   而账里最常见的正是 `get { return …` 那一行（它本来就会被判不合格）。
        if is_cs and 1 <= ln <= len(lines):
            g = one_line_getter(lines, ln - 1)
            if g:
                getters.append((base, who, land, g))
                warn(WARN_GETTER, "getter `%s`（原版可能本来就是属性 ⇒ 人读，⛔ 不算失败）" % g)

        names, mode = paren_idents(land)
        hname = head_name(land, r)
        strict_name = hname or (names[0] if (mode == "exact" or (mode == "prose" and len(names) == 1)) else None)

        if is_cs and mode == "prose" and not hname and len(names) != 1:
            warn(WARN_PROSE, "括号是散文（提取到 %d 个标识符%s）⇒ 名字没法对，只提示" %
                 (len(names), "" if not names else "：" + ", ".join(names[:4])))
        if is_cs and ((strict_name or "").endswith("ForTest") or any(t.endswith("ForTest") for t in names)):
            warn(WARN_FORTEST, "落点/括号里是 `*ForTest` 自检转发口（不是实现）")

        # ---- 名字核对（三种来源，⛔ 只用其中一种）----
        #   ⚠️ **数据文件（.json/.tsv/.asset）不做名字核对**：它们只要求那一行非空。
        if not is_cs:
            pass
        elif hname:
            # 名字写在括号**之前**（`…:642 ShowError(text, …)`）：拿它比那一行声明的名字。
            dn = decl_name_at(lines[ln - 1]) if (is_cs and 1 <= ln <= len(lines)) else None
            if dn is not None and dn != hname:
                fails.append((base, who, land,
                              "行号后写的是 `%s`，但那一行声明的是 `%s`（名字对不上）" % (hname, dn)))
                continue
        elif mode == "exact" and strict_name:
            cands = find_decl_candidates(lines, strict_name)
            if not cands:
                fails.append((base, who, land,
                              "括号里写了声明名 `%s`，但文件里找不到这份声明" % strict_name))
                continue
            if len(cands) > 1:
                # 🔴 **多义一律不自动改**（`--fix` 保守）：只报，把判断交给人。
                #    典型：同名方法有多处声明（重载 / 另一个类里同名）。
                if ln in cands:
                    pass                     # 账里写的那一行**本身**就是候选 ⇒ 通过
                else:
                    drifts.append((base, who, land,
                                   "歧义（`%s` 有 %d 处声明：%s）—— **不自动改**，请人工确认"
                                   % (strict_name, len(cands), ", ".join(map(str, cands[:6])))))
                    if show_pass:
                        print("AMBIG| %-22s | %-52s | %s 有 %d 处声明"
                              % (base, who[:52], strict_name, len(cands)))
                    continue
            else:
                got = cands[0]
                if got != ln:
                    note = fix_block_reason(lines, got)
                    drifts.append((base, who, land, "漂移（实际在 %d 行；账里写 %d）%s" % (got, ln, note)))
                    if do_fix and not note:
                        rewrites.append((ri, land[:r.start(2)] + str(got) + land[r.end(2):]))
                    if show_pass:
                        print("DRIFT| %-22s | %-52s | 账 %d -> 实际 %d"
                              % (base, who[:52], ln, got))
                    continue
                ln = got
        elif mode == "prose" and len(names) == 1:
            # 第 4 条盲点：`…:764（Refresh 方法）` —— 只验「那行像声明」不验名字会全绿。
            #   这里按**那一行声明的名字**比；取不出来 ⇒ 退回「只验形态」，⛔ 不判失败。
            dn = decl_name_at(lines[ln - 1]) if (is_cs and 1 <= ln <= len(lines)) else None
            if dn is not None:
                if dn != names[0]:
                    fails.append((base, who, land,
                                  "括号里写的是 `%s`，但那一行声明的是 `%s`（名字对不上）"
                                  % (names[0], dn)))
                    continue
            else:
                warn(WARN_PROSE, "括号里是 `%s`，但取不出那一行声明的名字 ⇒ 只验形态" % names[0])
        elif mode == "prose" and len(names) > 1 and is_cs and 1 <= ln <= len(lines):
            dn = decl_name_at(lines[ln - 1])
            if dn is not None and dn not in names:
                warn(WARN_PROSE, "括号散文里提到 %s，都不等于那一行声明的 `%s`（只提示、不判失败）"
                     % (", ".join(names[:4]), dn))

        if ln < 1 or ln > len(lines):
            fails.append((base, who, land, "行号越界（文件只有 %d 行）" % len(lines)))
            continue
        if is_cs:
            ok, why = looks_like_decl(lines[ln - 1])
            if not ok:
                fails.append((base, who, land, "%s：`%s`" % (why, lines[ln - 1].strip()[:60])))
                continue
            dn = decl_name_at(lines[ln - 1])
            if dn:
                if self_reference(lines[ln - 1], dn):
                    warn(WARN_SELFREF, "`%s` 这一行既声明又调用了同名方法 ⇒ 转发口，不是实现" % dn)
                if dn.endswith("ForTest"):
                    warn(WARN_FORTEST, "落点声明的就是 `%s`（自检/转发口，不是实现）" % dn)
        elif not lines[ln - 1].strip():
            fails.append((base, who, land, "数据文件那一行是空的"))
            continue
        if show_pass:
            print("OK   | %-22s | %-52s | %s" % (base, who[:52], land[:70]))
    return total, fails, drifts, rewrites, getters, warns


def read_src_lines(path):
    """🔴 **按二进制读再切行**（2026-10-25 第八十会话批次 1 · R1 加）。

    文本模式（`io.open(..., encoding=...)`）会做 **universal newlines**：把**裸 `\\r`** 也当换行
    ⇒ 文件里只要有一个 `\\r\\r\\n`，它后面的行号就整体偏大。实测踩过：`RuleEngine/Core/DeckRules.cs`
    有 7 个裸 `\\r` ⇒ 该文件行号**整体偏 +7**，`--fix` 会把 15 行账「修」成错行、**而且闸变绿**。
    ⇒ 这里只把 **`\\r\\n`** 归成 `\\n`，裸 `\\r` 留在行内（与编辑器/`git diff` 的行号口径一致）。
    """
    raw = io.open(path, "rb").read().decode("utf-8", "ignore")
    return raw.replace("\r\n", "\n").split("\n")


def fix_block_reason(lines, ln):
    """`--fix` 该不该拒绝改到这一行 ⇒ 返回理由（空串 = 可以改）。

    🔴 历史事故：`--fix` 曾把 `KwValue` 的漂移「修正」到一个**属性 getter** 上（392），
       真值在 938；`*ForTest` 转发口同理。
    🔴 **2026-10-25（R1）第三道闸**：目标行必须能被 `dn_at_name`（= `verify_annot.py` 的 `dn_at`）
       解析成**声明**；解析不出（注释行 / `{` / `}` / 空行 / 字段带初始化器 …）⇒ `NEED-HUMAN`，
       **不写**。否则闸会「变绿」而落点其实是错的（Q3 投诉：18/79 落在注释行 / 别的方法上）。
    """
    if ln < 1 or ln > len(lines):
        return ""
    g = one_line_getter(lines, ln - 1)
    if g:
        return "（⚠️ 该行是 getter `%s`：--fix 不自动改，请人读）" % g
    dn = decl_name_at(lines[ln - 1]) or ""
    if dn.endswith("ForTest"):
        return "（⚠️ 该行是自检/转发口 `%s`：--fix 不自动改）" % dn
    got = dn_at_name(lines[ln - 1])
    if got is None:
        return "（🔴 NEED-HUMAN：目标行解析不出声明名（不是声明）：--fix 跳过、请人读 `%s`）" \
               % lines[ln - 1].strip()[:50]
    if dn and got != dn:
        return "（🔴 NEED-HUMAN：目标行解析出的名字是 `%s`，与那一行声明的 `%s` 不一致：--fix 跳过）" \
               % (got, dn)
    return ""


def apply_fixes(path, rewrites):
    rows = [l.rstrip(NL) for l in io.open(path, encoding="utf-8", newline="") if l.strip()]
    i_land = rows[0].split("\t").index("我们的落点")
    for ri, new_land in rewrites:
        c = rows[ri].split("\t")
        c[i_land] = new_land
        rows[ri] = "\t".join(c)
    # 🔴 先写临时文件再原子替换：`io.open(p,'wb').write(表达式)` 会**先截断**再求值，
    #    表达式一抛异常整份账就空了（CLAUDE.md §二 那条踩过的坑）。
    data = (NL.join(rows) + NL).encode("utf-8")
    tmp = path + ".tmp"
    io.open(tmp, "wb").write(data)
    os.replace(tmp, path)


def main():
    # 🔴 `--root DIR` 的 **DIR 不是账本**：先把它剔掉，否则会被当成一个账本路径 ⇒ 直接返回 2
    #    （2026-10-25 自测抓到：`--root` 一直是坏的，只是没人用过）。
    root_val = None
    if "--root" in sys.argv:
        _i = sys.argv.index("--root")
        if _i + 1 < len(sys.argv):
            root_val = sys.argv[_i + 1]
    args = [a for a in sys.argv[1:] if not a.startswith("--") and a != root_val]
    show_pass = "--all-A" in sys.argv
    do_fix = "--fix" in sys.argv
    warn_getters = "--warn-getters" in sys.argv
    roots = list(DEFAULT_ROOTS)
    if root_val:
        roots = [root_val] + roots

    if not args:
        files = sorted(f for f in glob.glob(os.path.join(LEDGER_DIR, "*.tsv"))
                       if not os.path.basename(f).startswith("_"))
    else:
        files = []
        for a in args:
            # 2026-10-25：账名带不带 `.tsv` 都收（任务书里两种写法都出现过，之前只认带 .tsv 的）
            cands = [a] if a.endswith(".tsv") else [a, a + ".tsv"]
            hit = None
            for c in cands:
                if os.path.isfile(c):
                    hit = c
                    break
                p = c if os.path.isabs(c) else os.path.join(LEDGER_DIR, c)
                if os.path.isfile(p):
                    hit = p
                    break
            if hit:
                files.append(hit)
            else:
                print("找不到账本：" + a)
                return 2

    ga = gf = gd = gw = 0
    all_f, all_d, all_g, all_w = [], [], [], []
    print("= 账本 A 档落点机检（判法见文件头：失败 / 漂移 / 告警 / 通过）="
          + ("  [--fix：会改写漂移行号]" if do_fix else "")
          + ("  [--warn-getters：印告警明细]" if warn_getters else ""))
    for f in files:
        total, fails, drifts, rewrites, getters, warns = check_file(f, roots, show_pass, do_fix)
        if total == 0:
            continue
        if do_fix and rewrites:
            apply_fixes(f, rewrites)
        ga += total
        gf += len(fails)
        gd += len(drifts)
        gw += len(warns)
        all_f.extend(fails)
        all_d.extend(drifts)
        all_g.extend(getters)
        all_w.extend(warns)
        print("%s %-22s A=%-4d 通过=%-4d 漂移=%-3d 告警=%-3d%s 失败=%d"
              % ("OK" if not fails else "XX", os.path.basename(f), total,
                 total - len(fails) - len(drifts), len(drifts), len(warns),
                 "（已就地改正）" if (do_fix and rewrites) else "", len(fails)))

    if all_d:
        print(NL + "---- 漂移（行号是快照；不算失败。加 --fix 可就地改正）----")
        for f, who, land, why in all_d:
            print("%-22s | %-52s | %-70s | %s" % (f, who[:52], land[:70], why))
    if all_f:
        print(NL + "---- 失败明细（文件 | 类.方法 | 落点 | 失败原因）----")
        for f, who, land, why in all_f:
            print("%-22s | %-52s | %-70s | %s" % (f, who[:52], land[:70], why))

    if warn_getters:
        print(NL + "---- 告警（候选，**只报、不算失败**；判据仍要人读）----")
        if not all_w:
            print("（无）")
        else:
            for kind in (WARN_PROSE, WARN_GETTER, WARN_FORTEST, WARN_SELFREF):
                rows_k = [w for w in all_w if w[3] == kind]
                if not rows_k:
                    continue
                print("  · [%s] %s —— %d 条" % (kind, WARN_TITLE.get(kind, ""), len(rows_k)))
                for f, who, land, k, why in rows_k:
                    print("    %-22s | %-52s | %-70s | %s" % (f, who[:52], land[:70], why))
            print(NL + "按账分布（告警合计 %d 条）：" % len(all_w))
            dist = {}
            for f, who, land, k, why in all_w:
                dist[f] = dist.get(f, 0) + 1
            for f, n in sorted(dist.items(), key=lambda kv: -kv[1]):
                print("  %-24s %4d" % (f, n))
            print("  %-24s %4d" % ("合计", len(all_w)))

    print(NL + "合计：A %d 条 · 通过 %d（其中告警 %d 条）· 漂移 %d · 失败 %d%s"
          % (ga, ga - gf - gd, gw, gd, gf, "（漂移已就地改正，请复核 git diff）" if do_fix else ""))
    return 0 if gf == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
