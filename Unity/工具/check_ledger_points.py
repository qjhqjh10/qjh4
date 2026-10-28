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
     （例：`…:764（Refresh 方法）`，而 764 行声明的是 `SelectArmy` ⇒ 失败）；
   · 🆕 **落点列里第二处及以后的 `文件:行`** 也按上面几条判（见下面「三个盲点」①）。
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
   · **MEMBER**（🆕 2026-10-27 · B9_W52LAND · 方案 §4.1 例外）：落点是 **字段 / 属性 / 嵌套类型**
     声明（纯取值器等价 / 结构等价 —— 不是「没复刻」，是「复刻成字段直读」）⇒ **合法但只报告警**，
     ⛔ **不计失败**。理由：为了过闸去加一个无行为的转发方法 = 「为工具写代码」，比一条告警更糟。
     防滥用两道（见 `member_decl_kind`）：① 必须以访问修饰符开头（局部变量语句没有 ⇒ 仍失败）；
     ② `decl_name_at` 必须解析得到名字（拿不准 ⇒ 不判，⛔ 不猜）。注释/空行/纯括号/语句中间仍失败。
     🆕 2026-10-28（B93_E4）：①**放宽到隐式 private**（没有修饰符 = C# 默认 private），
     但只认「文本上不可能是局部语句」的形状（类型 / 带 `{` 的属性 / 表达式体属性 /
     **下一行是 `{`** 的名字行属性）；裸字段 `int X;` 仍不认（与局部变量同形）。
   · **SECOND**（🆕 2026-10-28 · B93_E4 · 盲点①）：落点列里**第二处及以后的 `文件:行`**
     （= 证据指针，全库 A 行 551 条这种形状）的候选：文件找不到（多为**原版源码引用**，如
     `CardDeck.cs:647` / `dump.cs:19631`）/ 短名歧义 / 声称的名字不在那一行 ⇒ 只提示、⛔ 不算失败。
     其中「**行号越界 / 那一行是空的**」且文件是**直接解析到**的 ⇒ 判**失败**（客观破碎）。
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

2026-10-28（B93_E4）补的三个盲点（判据 = `报告/F2_落点失败收尾.md` §④）
--------------------------------------------------------------------------
  · **① 落点列里「第二处及以后」的 `文件:行` 现在也机检**（`check_extra_refs`）。
    原来 `REF.search(land)` 只取**第一处**，写在括号/散文里的第二、三处
    （`A.cs:123（X）· B.cs:456（Y）· :789（Z）`，全库 A 行 **551 条**是这种形状）
    **从来没被机检过** ⇒ 那里写成注释行 / 指向不存在的文件或行号，闸照样绿（**空转绿灯**）。
    现在每一处都过「文件在 / 行号不越界 / 那行像声明 / 括号里写的名字与那行声明一致」四关；
    ⛔ **次要落点不产生 `--fix` 改写**（`--fix` 的重定位语义一个字没动），
    「名字不在这一行」按**漂移**报（带 `（次要落点）` 前缀），人读。
    ⚠️ 只取**紧跟该处 `:行` 之后、到下一处 `:行` 之前**的那一段当它的括号
    （否则会把别人的括号算到自己头上）。
  · **② 带基类列表的 `class X : Y` 现在认得出**（`find_decl_candidates` 的候选正则补了 `:`）。
    原来名字后面跟的是 `:`、模式只认 `( < { ; = $` ⇒ `public sealed class Foo : Bar { }`
    **当不了落点**（会被判「文件里找不到这份声明」）。F2 §④2 点名的就是这条。
  · **③ 隐式 private / 一行式 getter 不再被判「不像声明」**（`member_decl_kind` 去掉「必须有访问修饰符」的前置闸）。
    原来 `bool TutorialAllowsCardDisplay`（`{` 在下一行，见 `BattleDriver.cs:17310`）、
    `int Count => KwValue(3);` 这类**合法成员声明**会被判「既无 `(` 也无 `=>`/`{`」/「`)` 后接的是语句」⇒ **失败**。
    现在它们按「成员级声明」放行，并只报 **MEMBER / GETTER 告警**（⛔ 仍不算失败）。
    🔴 **两条故意不认**（防「放宽到随便什么都算声明」）：
      a) **隐式 private 的裸字段**（`int X;` / `int X = 5;`）—— 它与**方法体里的局部变量文本完全同形**，
         放宽就等于让局部变量也算声明；实测全库 A 行 **0 条**落在这种行上（2026-10-28）。
      b) **名字行属性必须「下一行是 `{`」**（真属性体的形状）才认 —— 否则多行调用
         （`SetThing\n  (a, b);`）会被当成属性。
    局部语句 / 注释 / 空行 / 纯括号行**仍然失败**（`_STMT_HEAD` + 上面两条闸）。

`--fix` 只改写「唯一命中 + 目标行不是 getter / `*ForTest` / **能解析成声明**」的漂移行号（默认不写）。
目标行解析不出声明名时，漂移理由里会打 **`NEED-HUMAN`** 并**跳过**（不写），请人读。

🔴 **`.cs` 一律按二进制读**（`read_src_lines`）：文本模式的 universal newlines 会把**裸 `\\r`** 当换行
   ⇒ 文件里只要有一个 `\\r\\r\\n`，后面所有行号整体偏移（实测 `DeckRules.cs` 偏 +7 ⇒ 15 行账被改错且闸变绿）。

用法
----
    python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py [账本...] [--all-A] [--fix] [--root DIR] [--warn-getters]

`--warn-getters`（2026-10-25 加，**只报、不算失败、退出码不变**）
------------------------------------------------------------------
把六类告警（SECOND / GETTER / FORTEST / SELFREF / PROSE / MEMBER）的**明细**打出来
（默认只在合计行给计数 + 按账分布）。
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

# 告警的形态（**都只报、不算失败**）
WARN_GETTER = "GETTER"
WARN_FORTEST = "FORTEST"
WARN_SELFREF = "SELFREF"
WARN_PROSE = "PROSE"
# 🔴 2026-10-27（B9_W52LAND · 方案 §4.1 例外）：落点是**字段 / 属性 / 嵌套类型**声明
#   （纯取值器等价 / 结构等价）⇒ 合法但只报告警、⛔ 不计失败。理由：为了过闸去加一个
#   无行为的转发方法 = 「为工具写代码」，比一条告警更糟。防滥用两条见 `member_decl_kind`。
WARN_MEMBER = "MEMBER"
# 🆕 2026-10-28（B93_E4 · 盲点①）：落点列里**次要落点**（第二处及以后的 `文件:行`）的候选
#   —— 文件找不到（含原版源码引用）/ 声称的名字不在那一行。⛔ 不算失败（判法见 `check_extra_refs`）。
WARN_SECOND = "SECOND"
WARN_TITLE = {
    WARN_GETTER: "落点那一行是属性 getter（原版可能就是属性 ⇒ 人读）",
    WARN_FORTEST: "落点是 `*ForTest` 自检 / 转发口（不是实现）",
    WARN_SELFREF: "那一行既声明又调用了同名方法（转发口，不是实现）",
    WARN_PROSE: "括号是散文 ⇒ 名字没法对（只提示、不判失败）",
    WARN_MEMBER: "落点是字段/属性/嵌套类型声明（纯取值器/结构等价 ⇒ 只报、不算失败）",
    WARN_SECOND: "次要落点（第二处及以后的 `文件:行`）的候选：找不到文件 / 名字不在那一行",
}
# 告警打印顺序里 `MEMBER` 排在 `PROSE` 之前（结构性问题，人复核时先看它）。
_WARN_PRINT_ORDER = (WARN_SECOND, WARN_MEMBER, WARN_PROSE, WARN_GETTER, WARN_FORTEST, WARN_SELFREF)
_MEMBER_KIND_CN = {"field": "字段", "property": "属性", "type": "嵌套类型"}

_IDENT = re.compile(r"[A-Za-z_]\w*")
# 🔴 2026-10-26：`decl_name_at` 取「紧贴 `(` 的第一个标识符」时要排除这些——它们不是声明名。
#   起因：C# 7 元组返回 `public static (A a, bool b) M(` 旧写法读成 `static`（假失败，实测 1 条）。
_NOT_A_NAME = {
    "public", "private", "protected", "internal", "static", "readonly", "const", "sealed",
    "partial", "virtual", "override", "abstract", "extern", "unsafe", "async", "new",
    "return", "throw", "await", "yield", "typeof", "nameof", "sizeof", "default", "checked",
    "unchecked", "if", "else", "for", "foreach", "while", "do", "switch", "case", "using",
    "lock", "try", "catch", "finally", "in", "out", "ref", "params", "this", "base",
}
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


# 🆕 2026-10-28（B93_E4）：**文件名兜底索引**（只在「按四个根找不到」时才用，见 `resolve_ref_file`）。
#   起因：账里次要落点大量写**短名**（`GameStaticData.cs` / `MenuDraw.cs` / `BattleContext.cs` /
#   `AnimAnchors.cs` / `WindowButton.cs`），它们确实在 `Assets/**` 下、但不在四个根的**根目录**上 ⇒
#   直接判「文件不存在」是**假失败**（CLAUDE.md 铁律：报「不存在」之前先做模糊匹配）。
_SRC_INDEX = None


def _src_index():
    """`basename → [全路径…]`（只建一次；**只走 `Assets` 一棵根**）。

    ⚠️ 不能把 `DEFAULT_ROOTS[:2]` 都走一遍：第二个根 `Assets/CardPresentation` 是第一个根的
       **子树** ⇒ 同一个文件会被收两次 ⇒ 每个短名都变成「歧义」（2026-10-28 实测 26 条假失败）。
    """
    global _SRC_INDEX
    if _SRC_INDEX is None:
        idx = {}
        root = DEFAULT_ROOTS[0]
        if os.path.isdir(root):
            for dirpath, _dirnames, filenames in os.walk(root):
                for fn in filenames:
                    if fn.endswith((".cs", ".json", ".tsv", ".asset")):
                        p = os.path.join(dirpath, fn)
                        idx.setdefault(fn, {})[os.path.normcase(p)] = p.replace("\\", "/")
        _SRC_INDEX = {k: list(v.values()) for k, v in idx.items()}
    return _SRC_INDEX


def resolve_ref_file(rel, roots):
    """`(全路径 or None, 失败时用的说明, 是不是【直接】找的)`。

    先按四个根直接找（与第一处落点**完全同一套**，`exact=True`）；找不到才用**同名文件**兜底，
    且**只有恰好唯一一个**才算找到（`exact=False` —— 兜底是猜测，调用方要把「越界 / 空行」
    降级成告警：账里这个位置**经常是原版源码引用**，`GameStaticData.cs:2444` 指的是原版那个
    2444 行，而我们的同名文件只有 227 行 ⇒ 拿它判「越界」是假红）。多个同名 ⇒ 如实报歧义，⛔ 不猜。
    """
    full = find_file(rel, roots)
    if full is not None:
        return full, "", True
    # ⚠️ **小写比较**：本机文件系统不区分大小写（CLAUDE.md 记过这个坑），
    #    账里写 `Core/EffectResolver.cs`、真身是 `RuleEngine/Core/EffectResolver.cs`。
    want = ("/" + rel.replace("\\", "/").lstrip("/")).lower()
    hit = [c for c in _src_index().get(os.path.basename(rel.replace("\\", "/")), [])
           if c.lower().endswith(want)]
    if len(hit) == 1:
        return hit[0], "", False
    if len(hit) > 1:
        return None, "短名歧义（%d 个同名文件：%s …）⇒ 请写全路径" % (len(hit), ", ".join(hit[:3])), False
    return None, "文件不存在（四个根 + Assets/** 同名兜底都没找到）", False


def name_lines(lines, name):
    """`name` 作为**标识符**出现在哪些行（1-based）。⛔ 用 `\\b` 卡边界，别让 `Init` 命中 `InitX`。"""
    pat = re.compile(r"\b" + re.escape(name) + r"\b")
    return [i for i, l in enumerate(lines, 1) if pat.search(l)]


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
        # 🔴 2026-10-26 修：**C# 7 元组返回类型**会把第一个 `(` 当成形参表。
        #   实测 `public static (ControlledSeedState state, bool found) GetSeedState(string id)`
        #   旧写法读出的是修饰符 `static` ⇒ 该行被判「声明名对不上」= 假失败。
        #   ⇒ 改成「取**第一个**紧贴 `(` 的**非关键字**标识符」：
        #     · 元组返回里 `static (` 被关键字表挡掉，下一个就是真名；
        #     · 形参默认值里的调用（`void F(int x = Mathf.Max(1,2))`）排在真名**之后**，取第一个不会误取。
        cands = [mm for mm in re.finditer(r"([A-Za-z_]\w*)\s*\(", re.sub(r"<[^<>]*>", "", head))
                 if mm.group(1) not in _NOT_A_NAME]
        if not cands:
            return None
        m = cands[0]
        pre = re.sub(r"<[^<>]*>", "", head)[:m.start()]
        if re.search(r"\.\s*$", pre):                 # `a.B(` = 调用，不是声明
            return None
        # `RefreshDeckList()` 这种**裸调用行**：名字前面什么都没有（没有返回类型 / 修饰符）⇒ 不是声明
        if not pre.strip():
            return None
        return m.group(1)
    if re.search(r"\.\s*[A-Za-z_]\w*$", head):     # `a.B` = 成员访问
        return None
    m = re.search(r"([A-Za-z_]\w*)\s*$", head)
    return m.group(1) if m else None


# 🔴 2026-10-27（B9_W52LAND · 方案 §4.1 例外）：「成员级落点」的识别
#   落点不一定是方法 —— **纯取值器（getter 等价 / 直接读字段）** / **结构等价** 的等价物
#   在我们侧可以是**字段 / 属性 / 嵌套类型**声明。这类允许当落点，但只报告警、⛔ 不算失败。
#   ⚠️ 防滥用（两道，缺一不可）：
#     ① 必须以**访问修饰符**开头（`public/private/protected/internal`）—— 方法体内的局部变量
#        语句（`int x = 5;`）没有访问修饰符 ⇒ 仍按「语句中间」判失败；注释/空行/纯括号行
#        也不会有访问修饰符 ⇒ 仍失败（本函数返回 None，由 `looks_like_decl` 判失败）。
#     ② `decl_name_at` 必须能解析出名字（拿不准 ⇒ None，⛔ 不猜）。
_MEMBER_MOD = re.compile(r"^(?:public|private|protected|internal)\b")
_TYPE_DECL = re.compile(r"\b(class|struct|interface|enum|record)\s+\w+")
_STMT_HEAD = re.compile(
    r"^(?:return|throw|var|new|if|else|for|foreach|while|do|switch|case|default|"
    r"break|continue|goto|yield|await|using|lock|try|catch|finally|get|set|add|remove)\b")
_PROP_TAIL = re.compile(
    r"^(?:public|private|protected|internal)"
    r"(?:\s+(?:static|readonly|const|sealed|virtual|override|abstract|new|unsafe|extern|volatile|partial))*"
    r"\s+[\w<>\[\],\.\?]+\s+\w+$")
# 🆕 2026-10-28（B93_E4 · 盲点③）：**隐式 private** 的成员声明（没有访问修饰符 = 默认 private），
#   以及**一行式 getter / 表达式体属性**（它们可能被 `looks_like_decl` 判成「`)` 后接的是语句」）。
#   ⛔ 只认「文本上不可能是方法体里的局部语句」的形状：表达式体属性 / 带 `{` 的属性 /
#      **下一行是 `{`** 的名字行属性 / 类型声明。裸字段 `int X;` 与局部变量同形 ⇒ 故意不认（见文件头盲点③）。
_MODS = (r"(?:public|private|protected|internal|static|readonly|const|sealed|virtual|override|"
         r"abstract|new|unsafe|extern|volatile|partial)")
_EXPR_PROP = re.compile(r"^" + _MODS + r"*\s*[\w<>\[\],\.\?]+\s+(\w+)\s*=>\s*\S.*;$")
_NAME_ONLY = re.compile(r"^" + _MODS + r"*\s*[\w<>\[\],\.\?]+\s+(\w+)\s*$")
_PROP_BODY = re.compile(r"^" + _MODS + r"*\s*[\w<>\[\],\.\?]+\s+(\w+)\s*\{")


def _next_code_line(lines, idx):
    """`lines[idx]` 之后**第一条非空非注释**行（0 基 `idx`）；没有 ⇒ 空串。"""
    if lines is None or idx is None:
        return ""
    for j in range(idx + 1, min(len(lines), idx + 6)):
        t = lines[j].strip()
        if t and not t.startswith("//") and not t.startswith("*") and not t.startswith("#"):
            return t
    return ""


def member_decl_kind(line, lines=None, idx=None):
    """落在「成员级声明」时的种类：`'field'` / `'property'` / `'type'`；不是成员声明 ⇒ `None`。

    ⛔ 只管**非方法**成员（方法由 `looks_like_decl` 正常判通过、不加这条告警）。
    ⛔ 不负责注释行 / 空行 / 语句 —— 那三类在下面都返回 `None`（仍由 `looks_like_decl` 判失败）。

    🆕 2026-10-28（B93_E4 · 盲点③）：**不再要求必须有访问修饰符** —— C# 里没有修饰符的类成员
        = `private`（隐式 private），它和「局部变量」的唯一区别是**在不在类体里**，
        而本函数只看一行。所以改成「按形状认，且只认**不可能是局部语句**的形状」：
          · 表达式体属性 `[mods] Type Name => expr;`（局部语句里 `Name =>` 前面不会有类型）
          · 带 `{` 的属性 `[mods] Type Name {`（局部语句没有 `{` 体）
          · 名字行属性 `[mods] Type Name` + **下一行是 `{`**（传 `lines`/`idx` 才判；不传 ⇒ 不认）
          · 类型声明 `[mods] class/struct/interface/enum/record Name`
        🔴 **裸字段 `int X;` 仍不认**（与 `int x = 5;` 局部变量文本完全同形），
           要认它必须换判据（数花括号深度），那会给「字符串里带 `{`」这种行制造静默偏移 —— 不做。
    """
    s = line.strip()
    if not s or s.startswith("//") or s.startswith("*") or s.startswith("/*") or s.startswith("#"):
        return None
    s = re.split(r"\s//", s)[0].strip()           # 行尾注释（与 decl_name_at 同口径）
    if not s or _STMT_HEAD.match(s):
        return None                                # 局部语句 / 语句头 ⇒ 不是成员（与修饰符无关）
    # ① 表达式体属性（含一行式 getter）：`public int Count => KwValue(3);` / `int Count => …;`
    m = _EXPR_PROP.match(s)
    if m and decl_name_at(line) == m.group(1):
        return "property"
    if "(" in s:
        return None                                # 方法 / 表达式体方法 —— 交给 looks_like_decl
    if _TYPE_DECL.search(s):
        return "type" if decl_name_at(line) else None   # `[mods] sealed class Foo`（`{` 常在下一行）
    if decl_name_at(line) is None:
        return None                                # 拿不准就不判（⛔ 不猜）
    if s.endswith(";"):
        # 🔴 盲点③ 明确**不放宽**的一格：裸字段（隐式 private 的 `int X;`）与局部变量同形。
        return "field" if _MEMBER_MOD.match(s) else None
    if _PROP_TAIL.fullmatch(s):
        return "property"                          # `public int Count`（`{` 在下一行）
    # ② 隐式 private 的名字行属性：`bool X` 且**下一行**是 `{`（真属性体）——
    #    不加「下一行是 `{`」这道闸，多行调用（`SetThing` 换行 `(a, b);`）会被误认成属性。
    m = _NAME_ONLY.match(s)
    if m and decl_name_at(line) == m.group(1) and _next_code_line(lines, idx).startswith("{"):
        return "property"
    # ③ 带 `{` 的名字行属性：`int X {`（正常路径由 `looks_like_decl` 放行，这里是兜底）
    m = _PROP_BODY.match(s)
    if m and decl_name_at(line) == m.group(1):
        return "property"
    return None


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
    # `$` 收尾：字段/属性块的名字可能**就在行尾**（`public int Count` 的 `{` 在下一行）——
    #   2026-10-27（B9_W52LAND）补，否则 `（Count）`/`（Board）` 这类成员落点永远「找不到声明」。
    # `:` —— 2026-10-28（B93_E4 · 盲点②）补：**带基类列表的类型声明**
    #   `public sealed class Foo : Bar { }`（名字后面跟 `:`）原来**匹配不上** ⇒ 被判「找不到这份声明」。
    #   安全性由下游 `decl_name_at(l) != name` 兜住：`case Foo:`（STMT 头挡）/ 三元 `a ? Foo : Bar;`
    #   （`decl_name_at` 取到的是 `Bar`）都不会变成候选。
    pat = re.compile(r"\b" + re.escape(name) + r"\s*(?:\(|<|\{|;|=|:|$)")
    out = []
    for i, l in enumerate(lines, 1):
        t = l.strip()
        if t.startswith("//") or t.startswith("*"):
            continue
        mm = pat.search(l)
        if not mm:
            continue
        ok, _why = looks_like_decl(l)
        # 🔴 2026-10-27（方案 §4.1 例外）：字段 / 属性 / 嵌套类型声明也算候选（--fix 仍不自动改，
        #   见 `fix_block_reason` 的 `dn_at_name` 闸 —— 非方法行解析不出 ⇒ NEED-HUMAN）。
        # 2026-10-28（B93_E4）：把 `lines`/`i-1` 传下去，否则「隐式 private 的名字行属性」
        #   （`bool X` + 下一行 `{`）在这里认不出来。
        if not ok and member_decl_kind(l, lines, i - 1) is None:
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


def check_extra_refs(land, roots, skip_key=None):
    """落点列里**第二处及以后**的 `文件:行` 的机检（2026-10-28 · B93_E4 · 盲点①）。

    🔴 **修的是什么**：`check_file` 原来只 `REF.search(land)` 取**第一处**带文件名+行号的引用。
    而账里大量落点是 `A.cs:123（X）· B.cs:456（Y）· :789（Z）` 这种**多处**写法
    （全库 A 行 **551 条**，2026-10-28 实测）—— 第二处及以后**从来没被机检**，
    指向空行 / 不存在的文件 / 越界行号 / 早已失效的行，闸**照样绿**（= 空转绿灯，F2 报告 §④ 记的就是这个）。

    🔴 **它和「第一处」的判法【故意不同】**（2026-10-28 实测定了口径）：
      第一处 = **落点**（必须是「像样声明」那一行）；
      第二处及以后 = **证据指针**（`调用点 / 唯一写点 / 别名的声明 / 调度台裁定注释 / 原版源码引用`
      都合法，语义上本来就不是「声明」）。拿「必须是声明」去量它们，实测 **300 条假失败 / 551 行**
      （注释说明行、`WireHeaderComponent(root);` 这种调用行全被误判）⇒ ⛔ 不这么干。

    ⇒ 判定分两档（**只有客观破碎才判失败**）：
      **失败**（这一处确实坏了，任何语义下都不成立）：
        · 文件**解析得出来**但**行号越界**（`> len`）；
        · 文件解析得出来、行号在范围内、但**那一行是空的**（空行永远不是证据）。
      **告警 `SECOND`**（候选，人读；⛔ 不算失败）：
        · 文件**找不到**（含「短名歧义」）—— 账里这个位置**经常是原版源码引用**
          （`CardDeck.cs:647-671` / `dump.cs:19631` / `out_il2csharp/.../X.cs:108`），
          它们本来就不可能解析到 `Assets/**` ⇒ 判失败会变成假红；
        · 这一处**声称了名字**（`:行 Foo(…` / `（Foo）` / `` （`Foo`） `` / 散文里恰好 1 个标识符，
          <3 字符的噪声 token 不算）但**那一行没有它** —— 行号可能已漂（`SetTip` 账写 925、实际 926），
          也可能散文指的是**外层方法名**（`（BeginTurnCore，= 兑现后写 0）`），机器分不清 ⇒ 只提示。

    文件解析：先按四个根找（与第一处**同一套**）；找不到再按**文件名兜底** —— 账里大量短名
      （`GameStaticData.cs` / `MenuDraw.cs` / `BattleContext.cs`）在 `Assets/**` 下**唯一**，
      按 CLAUDE.md「报『不存在』之前先做模糊匹配」的口径，兜底到**恰好唯一**的那个才算找到。

    ⚠️ 三处保守：
      · **只取 `:行` 之后、下一处 `:行` 之前**的那一段当它自己的括号 —— 否则会把别人的括号算到
        自己头上（`A.cs:1 · B.cs:2（Foo）` 里 `Foo` 属于 B，不是 A）。
      · **`--fix` 一个字都不动**：次要落点在 `--fix` 眼里不存在（`--fix` 的重定位语义不变）。
      · 只认 `文件名:行`：像 `· :789` 这种**省略文件名、靠上下文**的写法不在这里检（无法可靠消歧）。
    """
    fails, warns = [], []
    ms = list(REF.finditer(land))
    if len(ms) < 2:
        return fails, warns
    seen = set()
    if skip_key:
        seen.add(skip_key)
    for i, m in enumerate(ms):
        if i == 0:
            continue
        nxt = ms[i + 1].start() if i + 1 < len(ms) else len(land)
        tail = land[m.end():nxt]
        rel, ln = m.group(1), int(m.group(2))
        key = (rel.replace("\\", "/"), ln)
        if key in seen:
            continue
        seen.add(key)
        tag = "%s:%d " % (rel, ln)
        full, note, exact = resolve_ref_file(rel, roots)
        if full is None:
            warns.append((rel, ln, tag + note + "（可能是原版源码引用 ⇒ 只提示）"))
            continue
        try:
            lines = read_src_lines(full)
        except Exception as e:
            warns.append((rel, ln, tag + "读不了文件：" + str(e)))
            continue
        if ln < 1 or ln > len(lines):
            msg = tag + "行号越界（文件只有 %d 行）" % len(lines)
            if exact:
                fails.append((rel, ln, msg))
                continue
            warns.append((rel, ln, msg + "（**短名兜底**解析到 %s，可能是原版源码引用 ⇒ 只提示）"
                          % full.replace("\\", "/")))
            continue
        if not lines[ln - 1].strip():
            msg = tag + "那一行是空的（空行不是证据 ⇒ 行号大概率已漂）"
            if exact:
                fails.append((rel, ln, msg))
                continue
            warns.append((rel, ln, msg + "（**短名兜底**解析到 %s，可能是原版源码引用 ⇒ 只提示）"
                          % full.replace("\\", "/")))
            continue
        # 名字只从**这一处自己的尾巴**里取（见 docstring 第一条保守）
        mm = re.match(r"\s*`?([A-Za-z_]\w*)`?\s*[（(]", tail)
        hname = mm.group(1) if (mm and len(mm.group(1)) >= 3) else None
        names, mode = paren_idents(tail)
        strict = hname or (names[0] if (mode == "exact" or (mode == "prose" and len(names) == 1))
                           else None)
        if not strict:
            continue                         # 没声称名字 ⇒ 只过上面三条客观关
        where = name_lines(lines, strict)
        if ln in where:
            continue                         # 名字就在这一行 ⇒ 过
        warns.append((rel, ln, tag + "声称的名字 `%s` 不在这一行（%s）⇒ 行号可能已漂、"
                                    "或散文指的是外层方法：人读"
                      % (strict, ("它出现在 %s" % "/".join(map(str, where[:5])))
                         + ("…共 %d 处" % len(where) if len(where) > 5 else "")
                         if where else "整个文件里都没有这个标识符")))
    return fails, warns


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
                # 🔴 2026-10-27（B9_W52LAND · 方案 §4.1 例外）：纯取值器 / 结构等价的落点
                #   可以是**字段 / 属性 / 嵌套类型**声明 ⇒ **合法但只报告警**（⛔ 不计失败）。
                #   ⚠️ 注释行 / 空行 / 语句行 / 纯括号行在这条路里仍返回 None ⇒ 仍然失败。
                #   2026-10-28（B93_E4 · 盲点③）：传 `lines`/`ln-1`，好让「隐式 private 的名字行属性」
                #   （`bool X` + 下一行 `{`，实测 `BattleDriver.cs:17310`）不再被判「不像声明」。
                kind = member_decl_kind(lines[ln - 1], lines, ln - 1)
                if kind is None:
                    fails.append((base, who, land, "%s：`%s`" % (why, lines[ln - 1].strip()[:60])))
                    continue
                warn(WARN_MEMBER, "落点是%s声明（纯取值器/结构等价 ⇒ 只报、不算失败；方案 §4.1 例外）"
                     % _MEMBER_KIND_CN.get(kind, kind))
            else:
                dn = decl_name_at(lines[ln - 1])
                if dn:
                    if self_reference(lines[ln - 1], dn):
                        warn(WARN_SELFREF, "`%s` 这一行既声明又调用了同名方法 ⇒ 转发口，不是实现" % dn)
                    if dn.endswith("ForTest"):
                        warn(WARN_FORTEST, "落点声明的就是 `%s`（自检/转发口，不是实现）" % dn)
        elif not lines[ln - 1].strip():
            fails.append((base, who, land, "数据文件那一行是空的"))
            continue
        # ---- 🆕 2026-10-28（B93_E4 · 盲点①）：落点列里**第二处及以后**的 `文件:行` 也要机检 ----
        #   原来只查了 `REF.search` 的第一处 ⇒ 后面的（全库 551 条 A 行有）是**空转绿灯**。
        #   ⛔ 次要落点**从不产生 `--fix` 改写**（`--fix` 的重定位语义一个字没动），只报漂移 / 失败。
        ex_fails, ex_warns = check_extra_refs(
            land, roots, skip_key=(rel.replace("\\", "/"), int(r.group(2))))
        for _rel, _ln2, _why in ex_fails:
            fails.append((base, who, land, "（次要落点）" + _why))
        for _rel, _ln2, _why in ex_warns:
            # ⚠️ 直连 `warns`（不走 `warn()`）：它的去重键是 `(行, 档位)`，会把同一行里
            #    第二、三处次要落点的明细合并成一条 —— 这里每一条都要看得见。
            warns.append((base, who, land, WARN_SECOND, "（次要落点）" + _why))
        if show_pass and not ex_fails:
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
            for kind in _WARN_PRINT_ORDER:
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
