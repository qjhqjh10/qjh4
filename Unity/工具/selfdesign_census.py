# -*- coding: utf-8 -*-
"""selfdesign_census.py —— 铁律 18「🔶 自己设计」全仓普查（2026-10-26 · Q2 第 4 轮）

用途：
  扫 `Unity/MyGame/Assets/**/*.cs`，产出「哪些地方该打 `🔶 自己设计` 标记」的候选清单，
  同时核「已经打过的标记有没有写全三样（是什么 / 为什么必须自己定 / 谁定的+日期）」。

判据正本：`CLAUDE.md` 铁律 18。
  · 固定标记串 = `🔶 自己设计`（别用别的写法）
  · 必须写全三样：① 这是什么 ② 为什么必须自己定（判据在哪 / 为什么拿不到）③ 谁定的 + 日期
  · ⛔ 三种不许打：① 原版就是那样（复刻）② 只是命名/结构不同但语义一致 ③ 还没查清（写 TODO）

三条硬口径（都是本仓踩过的坑，⛔ 别改）：
  1. **二进制读**（`io.open(p,'rb')`），自己按 `\\r\\n` / `\\n` 切行 —— 文本模式会把 `\\r\\r\\n`
     折成两个换行、让行号整体偏（且静默）。本脚本另会报「有裸 CR 的文件」，因为那类文件的行号
     在任何按 `\\n` 计数的工具（grep/tail）里都会与这里不同。
  2. **只看注释**（`//` / `///` / `/* */`）。代码里的字符串字面量（`Loc.T("自定义…")` 之类）
     不算命中 —— 历史坑：注释里 237 处示例键曾让同类扫描全成假阳性。
  3. **只报「值/行为」，不报散文**：注释命中后，还要在 ±WINDOW 行内找到常量/字段/数组初始化/
     `return <字面量>` 才算「高优先候选」；否则归「低优先（散文）」，**建议不改写**。

用法：
  python -I -X utf8 d:/4/Unity/工具/selfdesign_census.py [ROOT] [选项]
    ROOT                默认 d:/4/Unity/MyGame/Assets
    --out PATH          把 markdown 写到文件（默认打到 stdout）
    --json PATH         额外落一份机器可读清单（供下一轮批量改码时按文件分组）
    --top N             §2 明细条数（默认 270）：**强词候选全部进明细**，弱词按危险度补到 N；其余进附录 A。0 = 不限
    --window N          「值邻近」的行窗口（默认 3）
    --quiet             不打印尾部统计（写文件时用）
  -h / --help          只打用法，退出 0

输出（五节 + 两个附录，对应 `判据/自设计清单.md`）：
  §1 已标注（`🔶 自己设计`）—— 逐条核三样是否写全
  §2 高优先候选（值/行为）—— 按「会不会让验收误当原版判据」排序（明细 + 附录 A 完整名单）
  §3 低优先（散文类）—— 只给数量 + 代表性例子，建议不改写
  §4 疑似误标（该是复刻 / 历史注记 / 否定式）—— 供人工排除
  §5 判不准的 + 已知热点核对表 + 跨距离候选（文件头点名、声明处没打标）

幂等：同一份输入跑两次输出逐字节相同（排序固定、不含时间戳）。
"""
import io
import os
import re
import sys
import collections

if any(a in ("-h", "--help") for a in sys.argv[1:]):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(__doc__)
    sys.exit(0)


# ---------------------------------------------------------------- 参数
VALUED = {"--out", "--json", "--top", "--window"}


def _arg(name, default=None, cast=str):
    if name in sys.argv[1:]:
        i = sys.argv.index(name)
        if i + 1 < len(sys.argv):
            return cast(sys.argv[i + 1])
    return default


def _positionals():
    """只取真正的【位置参数】—— 跳掉 `--opt VALUE` 里的 VALUE（别把输出路径当扫描根）。"""
    out, i = [], 1
    while i < len(sys.argv):
        a = sys.argv[i]
        if a in VALUED:
            i += 2
            continue
        if not a.startswith("-"):
            out.append(a)
        i += 1
    return out


ARGS = _positionals()
ROOT = ARGS[0] if ARGS else "d:/4/Unity/MyGame/Assets"
OUT = _arg("--out", None)
JSON_OUT = _arg("--json", None)
TOP = _arg("--top", 270, int)   # §2 明细表条数（0 = 不限）。切法见下面 hi_strong/hi_weak：
                               # **强词候选一律进明细**，弱词按危险度补到 TOP 条；其余进附录 A
WINDOW = _arg("--window", 3, int)
QUIET = "--quiet" in sys.argv[1:]

SKIP_DIRS = ("/Library/", "/Temp/", "/obj/", "/.git/", "/Logs/")


# ---------------------------------------------------------------- 词表
MARK = "🔶 自己设计"

# 「我们自己设计」的**强**指示词（出现即认为作者已经自认是自设计）
STRONG = [
    "我们编的", "我们编", "自己编", "我们挑的", "我们自己设计", "我们自己设计",
    "自设计", "非原版", "不是原版", "我们自己定", "我们自定", "我们自己挑",
    "我们补的", "自己定的", "自拟",
    "我们加的", "我们自己加", "我们主动",
]
# 弱指示词（单独出现时多半是散文或描述「原版是硬编码」这类事实，要配合值邻近才判）
WEAK = [
    "自定义", "硬编码", "拍脑袋", "我们挑", "我们设计", "人工指定", "我们估",
    "我们定", "近似", "兜底",
]
# 复刻侧证据（旁边有这些 ⇒ 更可能是照抄原版，⛔ 不该打自设计标记）
REPLICA = ["照抄", "照原版", "复刻", "逐句", "逐跳", "decomp", "RVA", "原版判据", "判据 =", "判据="]

# 否定式：注释在说「这**不是**我们挑的」⇒ 是复刻，⛔ 不该打标
NEGATION = ["不是我们挑的", "不是我们编的", "不是我们设计", "不是自设计", "非我们挑",
            "而不是我们挑的", "不是自己编的", "不是我们自己", "不是我们自"]
# 历史注记：注释在说「**原来**是我们挑的，现已换成原版值」⇒ 现值是复刻，⛔ 不该打标
HISTORY = ["已替换", "已改掉", "已作废", "已过期", "旧版是", "旧注释", "旧实现",
           "以前是", "原来是", "原来", "改前", "当时是", "不再是我们挑的", "不再是我们编的"]

# 「值/行为」的证据：常量 / 字段初始化 / 数组初始化 / return 字面量
VALUE_PATTERNS = [
    (re.compile(r"\bconst\b"), "const"),
    (re.compile(r"\bstatic\s+readonly\b"), "静态只读"),
    (re.compile(r"\breadonly\b[^;]*\[\]"), "只读数组"),
    (re.compile(r"\bnew\s+[\w<>,\s\.]*\s*[\[\{]"), "new 容器"),
    (re.compile(r"\breturn\s+[-+]?\d"), "return 字面量"),
    (re.compile(r"=\s*[-+]?\d+(\.\d+)?f?\s*[,;)]"), "数值赋值"),
    (re.compile(r"=\s*\""), "字符串赋值"),
    (re.compile(r"\{\s*[-+]?\d+\s*(,|\})"), "数组字面量"),
    (re.compile(r"\b(public|private|internal|protected)\s+[\w<>\[\],\.\s]+\s+\w+\s*="), "字段初始化"),
]

# 跨距离候选用：文件头出现这些词 ⇒ 这个文件里「哪些值是我们挑的」很可能写在文件头
HEADER_WORDS = ["我们编的", "我们挑的", "非原版", "不是原版", "自拟", "我们自己定", "我们补的",
                "硬编码", "我们估的", "我们挑出"]

# 标识符（跨距离候选用：从头里的自设计说明抽出名字，再去文件里找同名声明）
IDENT_RE = re.compile(r"[A-Za-z_][A-Za-z0-9_]{3,}")
# 「被声明的那个名字」= 行内 `名字 =` 里、且不是 C# 关键字的那些
DECLARED_NAME_RE = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)\s*=[^=]")
CS_KEYWORDS = set("""public private protected internal static readonly const volatile string float double int
uint long ulong short byte bool char void object class struct enum interface using namespace return new var
this base null true false override virtual abstract sealed partial get set value params out ref in is as if
else for while foreach do switch case break continue try catch finally throw default goto await async lock
when where nameof sizeof typeof checked unchecked fixed unsafe stackalloc""".split())

# 「值声明」行（跨距离候选用：只要常量/只读/带初值的字段声明）
DECL_RE = re.compile(
    r"\b(const|static\s+readonly)\b"
    r"|\breadonly\b[^;]*\[\]"
    r"|^\s*(public|private|internal|protected)\s+[\w<>\[\],\.\s]+\s+\w+\s*=\s*[\{\d\"-]"
)

# ③「谁定的 + 日期」
RE_WHO = re.compile(r"(用户|调度台|主对话|引擎线|界面线|卡牌线|工具线|战场线|特效线)\s*20\d\d-\d\d-\d\d")
RE_DATE = re.compile(r"20\d\d-\d\d-\d\d")
# ②「为什么必须自己定」
WHY_WORDS = ["判据", "拿不到", "本地没有", "远端", "服务端", "查不到", "为空", "原版没有",
             "取证", "为什么", "无对应", "没有对应", "不可达", "缺这一维"]


# ---------------------------------------------------------------- 读文件
def walk_cs(root):
    out = []
    for dp, dn, fn in os.walk(root):
        norm = dp.replace(os.sep, "/")
        if any(x in norm for x in SKIP_DIRS):
            continue
        for f in fn:
            if f.endswith(".cs"):
                out.append(os.path.join(dp, f))
    out.sort()
    return out


def read_lines(p):
    """二进制读 → 自己切行。返回 (lines, has_bare_cr)。

    行号口径 = 按 `\\n` 切（CRLF 算一个换行，与 grep/Unity 一致）；
    **裸 CR（不在 CRLF 里的 `\\r`）单独报出来** —— 那类文件在按 `\\r\\n` 切的工具里行号会不同。
    """
    b = io.open(p, "rb").read()
    bare = b.count(b"\r") - b.count(b"\r\n")
    b = b.replace(b"\r\n", b"\n")
    txt = b.decode("utf-8", "replace")
    if bare > 0:
        txt = txt.replace("\r", "\n")     # 保持「行」的直觉，但会与 grep 的行号不一致（已报出来）
    return txt.split("\n"), bare


def lex_lines(lines):
    """把每一行拆成 (code, comment)。支持 // /// /* */ 与字符串/字符字面量。

    返回两个等长列表：codes[i] / comments[i]（comments[i] 是该行的注释文本，可能为空串）。
    """
    codes, comments = [], []
    in_block = False
    for line in lines:
        code_chars, com_chars = [], []
        i, n = 0, len(line)
        in_str = None       # None / '"' / '@"' / '$"' / "'"
        while i < n:
            c = line[i]
            two = line[i:i + 2]
            if in_block:
                if two == "*/":
                    in_block = False
                    i += 2
                    continue
                com_chars.append(c)
                i += 1
                continue
            if in_str is not None:
                # 字符串内：只关心结束符与转义
                if in_str == "'":
                    if c == "\\" and i + 1 < n:
                        code_chars.append(line[i:i + 2])
                        i += 2
                        continue
                    if c == "'":
                        in_str = None
                    code_chars.append(c)
                    i += 1
                    continue
                # 常规字符串（含 @"" / $"")
                if in_str != '@"' and c == "\\" and i + 1 < n:
                    code_chars.append(line[i:i + 2])
                    i += 2
                    continue
                if c == '"':
                    if in_str == '@"':          # @"" 里 "" 是转义的引号
                        if line[i:i + 2] == '""':
                            code_chars.append('""')
                            i += 2
                            continue
                    in_str = None
                code_chars.append(c)
                i += 1
                continue
            # 普通态
            if two == "//":
                com_chars.append(line[i + 2:])
                break
            if two == "/*":
                in_block = True
                i += 2
                continue
            if c == '"':
                in_str = '@"' if (i > 0 and line[i - 1] in "@$") else '"'
                code_chars.append(c)
                i += 1
                continue
            if c == "'":
                in_str = "'"
                code_chars.append(c)
                i += 1
                continue
            code_chars.append(c)
            i += 1
        # 块注释跨行时，code 部分应当是空的 —— 上面已经把块内字符都塞进 com_chars
        codes.append("".join(code_chars))
        comments.append("".join(com_chars))
    return codes, comments


# ---------------------------------------------------------------- 单条判定
def value_near(codes, ln, win):
    """ln 是 1 基行号。返回命中的「值证据」说明（None = 附近没有值）。"""
    lo = max(0, ln - 1 - win)
    hi = min(len(codes), ln + win)
    for i in range(lo, hi):
        code = codes[i]
        if not code.strip():
            continue
        for rx, label in VALUE_PATTERNS:
            if rx.search(code):
                return "%s @%d" % (label, i + 1)
    return None


def marker_block(comments, codes, ln, span=8):
    """取 marker 所在行 + 紧随其后的连续注释行，拼成一段文本（用来核三样）。"""
    parts = [comments[ln - 1]]
    i = ln
    while i < len(comments) and i < ln + span:
        if codes[i].strip() == "" and comments[i].strip() != "":
            parts.append(comments[i])
            i += 1
        else:
            break
    return "\n".join(parts)


def check_three(text):
    miss = []
    # ① 这是什么：冒号后有实义内容（去掉 markdown 装饰后 >= 4 个非空字符）
    m = re.search(r"自己设计[^：:]*[：:](.*)", text)
    what = ""
    if m:
        what = m.group(1).split("·")[0].strip(" `*—-")
    if len(re.sub(r"[\s`*_—\-]", "", what)) < 4:
        miss.append("①这是什么")
    # ② 为什么必须自己定
    if not any(w in text for w in WHY_WORDS):
        miss.append("②为什么")
    # ③ 谁定的 + 日期
    if not RE_DATE.search(text):
        miss.append("③日期")
    elif not RE_WHO.search(text):
        miss.append("③谁定的")
    return miss


# ---------------------------------------------------------------- 已知热点
# (文件相对路径, 锚点子串, 人话说明, 该锚点「必须已有 🔶」?)
HOTSPOTS = [
    ("CardPresentation/Shell/CardDetailPopup.cs", "int[] NeedCopies",
     "CardProgress 升级消耗表 · 需要副本数（原版在服务端 CardTierConfig）", True),
    ("CardPresentation/Shell/CardDetailPopup.cs", "int[] NeedGold",
     "CardProgress 升级消耗表 · 金币（我们编的）", True),
    ("CardPresentation/Shell/CardDetailPopup.cs", "int[] Points",
     "CardProgress 升级消耗表 · 锻造点（我们编的）", True),
    ("CardPresentation/Shell/CardDetailPopup.cs", "WildcardsPerRarity = 99",
     "通配符**初始**库存（原版是服务端发放，没有本地发放源）", True),
    ("CardPresentation/Shell/CardTierConfig.cs", "public float Y_Max",
     "玩家等级曲线 4 个标量（2026-10-26 用户裁定自拟）", True),
    ("CardPresentation/Shell/CardTierConfig.cs", "tiersByRarity = new List",
     "tiersByRarity 取数结构（原版走远端 IConfigData）", True),
    ("CardPresentation/Core/CardView.cs", "HashSet<string> TacticSubtypeShown",
     "卡面兵种行的**兜底白名单**（spellType 未知时的替代判据）", True),
    ("RuleEngine/Core/CardDef.cs", "int spellType = -1",
     "我们自设计 26 张卡的 spellType = -1（= 未知，⛔ 不是 Normal）", True),
    ("RuleEngine/Data/StarterCards.cs", "StarterCards",
     "我们自己设计的 26 张卡整族（不在原版卡池里）", True),
    ("RuleEngine/Core/RuleCore.cs", "static void TrySwarmMerge",
     "虫群合并入队（「一次合并只发一条事件」vs 原版逐个移除每次广播）", True),
    ("RuleEngine/Core/RuleCore.cs", "WhenEventKind.BecomesRemnant",
     "becomesremnant 广播（我们补的，原版 TransformIntoRemnant 一条都不发）", True),
    ("RuleEngine/Core/BattleContext.cs", "PersistentEffects = new List",
     "ctx.PersistentEffects 容器（原版没有对应类型）", True),
]


# ---------------------------------------------------------------- 主流程
def main():
    files = walk_cs(ROOT)
    bare_cr = []
    marked = []        # (rel, ln, miss[], text)
    high = []          # (score, rel, ln, comment, value_why, words)
    low = []           # (rel, ln, comment, words)
    suspect = []       # (rel, ln, comment, words, why)
    total_mark = 0
    xdist = []        # 跨距离候选：文件头已声明「自设计」，声明处还没打标

    # 打分用的「危险度」：越可能被验收误当原版判据 ⇒ 越高
    DANGER = [
        ("数值赋值", 3), ("数组字面量", 3), ("const", 3), ("静态只读", 3), ("只读数组", 3),
        ("return 字面量", 3), ("字段初始化", 2), ("new 容器", 2), ("字符串赋值", 1),
    ]
    danger_of = dict(DANGER)

    for p in files:
        rel = os.path.relpath(p, ROOT).replace(os.sep, "/")
        lines, bare = read_lines(p)
        if bare:
            bare_cr.append((rel, bare))
        try:
            codes, comments = lex_lines(lines)
        except Exception as e:                     # 绝不静默：报出来
            sys.stderr.write("[warn] lex 失败 %s: %s\n" % (rel, e))
            continue

        # --- 跨距离候选：文件头写清了「哪些值是我们挑的」，但那些值的声明处还没打标。
        #     做法**不是**「头里有自设计词 ⇒ 全文件常量都算」（实测那样会刷出 3,000+ 条噪声），
        #     而是**指名道姓**：从头里那句自设计说明（±2 行）抽出**标识符**，再去文件里找同名声明。
        #     典型命中 = `CardDetailPopup` 头里写 `NeedCopies / NeedGold / Points`，值在 100 行外。
        head_ids = set()
        for i in range(min(80, len(comments))):
            com = comments[i]
            if not any(w in com for w in HEADER_WORDS):
                continue
            ctx = "\n".join(comments[max(0, i - 2):i + 3])
            head_ids.update(IDENT_RE.findall(ctx))
        head_ids -= CS_KEYWORDS
        if head_ids:
            mark_lines = [i + 1 for i, c in enumerate(comments) if MARK in c]
            for i, code in enumerate(codes):
                ln = i + 1
                if ln <= 3 or not DECL_RE.search(code):
                    continue
                # ⚠️ 只比**被声明的那个名字**，不比行内任意 token —— 否则 `public` 这种词
                #    （IDENT_RE 也认它）会让每一行 `public … = …` 都命中（实测 ItemDrawer 12 条全假）。
                names = set(DECLARED_NAME_RE.findall(code)) & head_ids
                if not names:
                    continue
                if any(abs(m - ln) <= 8 for m in mark_lines):
                    continue
                blk = "\n".join(comments[max(0, i - 4):i + 1])
                if any(w in blk for w in STRONG):
                    continue          # 声明处自己就写了 ⇒ 已在 §2
                xdist.append((rel, ln, code.strip()[:120], sorted(names)[0]))

        for idx, com in enumerate(comments):
            ln = idx + 1
            if not com.strip():
                continue
            if MARK in com:
                total_mark += 1
                blk = marker_block(comments, codes, ln)
                marked.append((rel, ln, check_three(blk), com.strip()))
                continue
            s_hit = any(w in com for w in STRONG)
            w_hit = any(w in com for w in WEAK)
            if not (s_hit or w_hit):
                continue
            r_hit = any(w in com for w in REPLICA)
            vwhy = value_near(codes, ln, WINDOW)
            words = [w for w in STRONG if w in com] + [w for w in WEAK if w in com]

            # §4 疑似误标（三档，全部进 §4 供人工排除）：
            #   ① 纯否定 —— 注释说的就是「这不是我们挑的」
            #   ② 历史注记 —— 「原来是我们挑的，现已换成原版值」
            #   ③ 只有弱词 + 复刻侧证据（照抄/判据/复刻 …），没有任何「我们编的/非原版/自设计」这类强词
            neg = [w for w in NEGATION if w in com]
            his = [w for w in HISTORY if w in com]
            if neg:
                suspect.append((rel, ln, com.strip(), words, "否定式：" + "/".join(neg[:2])))
                continue
            if his and not any(w in com for w in ("仍然", "依然", "还没换", "仍是")):
                suspect.append((rel, ln, com.strip(), words, "历史注记：" + "/".join(his[:2])))
                continue
            if r_hit and not s_hit:
                suspect.append((rel, ln, com.strip(), words, "弱词 + 复刻证据"))
                continue

            if vwhy:
                sc = danger_of.get(vwhy.split(" @")[0], 2)
                if r_hit:
                    sc -= 2          # 旁边写着「判据/照抄」的，自设计的可能性低一档
                if not s_hit:
                    sc -= 1
                high.append((sc, rel, ln, com.strip(), vwhy, words))
            else:
                low.append((rel, ln, com.strip(), words))

    # ---------------- 热点核对
    hot_rows = []
    by_rel = collections.defaultdict(list)
    for p in files:
        rel = os.path.relpath(p, ROOT).replace(os.sep, "/")
        by_rel[rel] = p
    for rel, needle, desc, need in HOTSPOTS:
        p = by_rel.get(rel)
        if p is None:
            hot_rows.append((rel, needle, desc, "⚠️ 文件不存在", need))
            continue
        lines, _ = read_lines(p)
        codes, comments = lex_lines(lines)
        hit = [i + 1 for i, c in enumerate(codes) if needle in c]
        if not hit:
            hit = [i + 1 for i, c in enumerate(comments) if needle in c]
        if not hit:
            hot_rows.append((rel, needle, desc, "⚠️ 锚点没找到（改名了？）", need))
            continue
        ln = hit[0]
        near = [i for i in range(ln - 1, len(comments)) if MARK in comments[i] and i + 1 <= ln + 30]
        near += [i for i in range(max(0, ln - 31), ln - 1) if MARK in comments[i]]
        if near:
            d = min(abs(i + 1 - ln) for i in near)
            hot_rows.append((rel, needle, desc, "✅ 已标注（最近 marker 距离 %d 行）" % d, need))
        else:
            hot_rows.append((rel, needle, desc, "❌ **未标注**", need))

    # ---------------- 去重：同一个「值」只留一条（一段 <summary> 多行会各命中一次）
    _seen_val = {}
    _high2 = []
    for row in sorted(high, key=lambda r: (r[1], r[2])):
        sc, rel, ln, com, vwhy, words = row
        key = (rel, vwhy.split(" @")[-1])
        if key in _seen_val and abs(_seen_val[key] - ln) <= 6:
            # 同一条声明、同一段注释块里更靠后的那行 ⇒ 丢掉（保留先出现的那行）
            continue
        _seen_val[key] = ln
        _high2.append(row)
    high = _high2

    # ---------------- 排序（确定性）
    high.sort(key=lambda r: (-r[0], r[1], r[2]))
    low.sort(key=lambda r: (r[0], r[1]))
    suspect.sort(key=lambda r: (r[0], r[1]))
    marked.sort(key=lambda r: (r[0], r[1]))

    # §2 明细的切法：**全部「强词」候选一律进明细**（那是可以照单打标的），
    # 弱词按危险度取前 (TOP - 强词数) 条；剩下的都进「附录 A」。
    # ⚠️ 别改成「纯按分数切 TOP 条」—— 那会把一部分强词挤到附录（只剩位置、没有原文）。
    hi_strong = [r for r in high if any(w in STRONG for w in r[5])]
    hi_weak = [r for r in high if not any(w in STRONG for w in r[5])]
    if TOP and len(high) > TOP:
        keep_weak = max(0, TOP - len(hi_strong))
        high_shown = hi_strong + hi_weak[:keep_weak]
        high_shown.sort(key=lambda r: (-r[0], r[1], r[2]))
        high_cut = len(high) - len(high_shown)
    else:
        high_shown, high_cut = high, 0

    # ---------------- 出 markdown
    L = []
    A = L.append
    A("# 🔶 自己设计 · 全仓普查清单（自动生成 · 别手工改）")
    A("")
    A("> 生成器：`python -I -X utf8 d:/4/Unity/工具/selfdesign_census.py`（幂等，可复跑）")
    A("> ⛔ **别手工改本文件** —— 它整篇是工具产物，下次重跑会覆盖。人工裁定/勾选状态写进")
    A("> `资料/完全复刻/报告/Q2_自设计普查_第4轮.md`（或另建一份 `自设计_裁定.md`）。")
    A("> 判据正本：`CLAUDE.md` **铁律 18**。固定标记串 = `🔶 自己设计`；")
    A("> 三样必须写全：**① 这是什么 · ② 为什么必须自己定 · ③ 谁定的 + 日期**。")
    A("> 口径：**只看注释**（不把 `Loc.T(\"…\")` 之类字符串字面量当命中）· **只报值/行为**（散文归 §3，建议不改写）。")
    A("> 扫描根 = `%s` · `.cs` 文件 **%d** 个 · 行 **%d** · 值邻近窗口 ±%d 行"
      % (ROOT.replace("\\", "/"), len(files), TOTAL_LINES[0] or 0, WINDOW))
    A("")
    A("## 规模一览")
    A("")
    A("| 项 | 数 |")
    A("|---|---:|")
    A("| `.cs` 文件 | %d |" % len(files))
    A("| 代码行 | %d |" % (TOTAL_LINES[0] or 0))
    A("| 已标注 `🔶 自己设计` | **%d** |" % total_mark)
    A("| §2 高优先候选（值/行为） | **%d**（强词 %d · 弱词 %d） |"
      % (len(high), sum(1 for r in high if any(w in STRONG for w in r[5])),
         sum(1 for r in high if not any(w in STRONG for w in r[5]))))
    A("| §3 低优先（散文） | %d |" % len(low))
    A("| §4 疑似误标 | %d |" % len(suspect))
    A("| §5.3 跨距离候选 | %d |" % len(xdist))
    A("| 有裸 CR 的文件 | %d |" % len(bare_cr))
    A("")
    A("## 界线：什么时候该打 `🔶 自己设计`（铁律 18 原文可操作化）")
    A("")
    A("**该打（三类）**：")
    A("")
    A("| 类 | 判据 | 本仓例 |")
    A("|---|---|---|")
    A("| **值是我们定的** | 常量/数组/阈值/默认值/权重，**原版那个数拿不到**（服务端 / 远端 config / 二进制里被剥） | 卡牌升级消耗表 · 等级曲线 4 个标量 · 通配符初始库存 |")
    A("| **原版查不到、我们挑了一个** | 有「查过、查不到」的取证（⛔ 不是「没查」） | 卡面兵种行兜底白名单 · 我们那 26 张卡的 `spellType = -1` |")
    A("| **机制/行为不同但结果等价、且这是我们主动接受的差异** | 差异本身在注释/账里写明了「原版是 X、我们是 Y」 | 「一次合并只发一条事件」 |")
    A("")
    A("**⛔ 不许打（三类）**：")
    A("")
    A("| 类 | 判据 | 本仓例 |")
    A("|---|---|---|")
    A("| 原版就是那样 | 有判据（`decomp_full/` / 解包字段 / 卡面） | 收藏窗卡组格可拖 · `RoundPrecision = 5f` |")
    A("| 只是命名/结构不同、语义一致 | 换个写法实现同一件事 | 共用件收口、partial 拆分 |")
    A("| 还没查清 | 拿不到判据但**也还没取证** | 该写 `TODO` 或「还没查清」 |")
    A("")
    A("**两档陷阱（§3/§4 就是干这个的）**：")
    A("**「原来是我们挑的、现已换成原版值」**（历史注记）与 **「这不是我们挑的」**（否定式）都**不是**自设计，")
    A("⛔ 打了会把复刻标成自设计 —— 那是**反向**误导，比不打更糟。")
    A("")
    A("**阅读顺序建议**：先 §5（已知热点核对）→ §2（要动手的）→ §4（排除掉错的）→ §3 只扫一眼 → §1 只核「缺哪样」。")
    A("")

    # ---- §1
    A("## §1 已标注（`🔶 自己设计`）")
    A("")
    if not marked:
        A("（无）")
    else:
        A("| # | 位置 | 三样 | 原文首行 |")
        A("|---:|---|---|---|")
        for i, (rel, ln, miss, com) in enumerate(marked, 1):
            st = "✅ 全" if not miss else "⚠️ 缺 **%s**" % " · ".join(miss)
            c = com.replace("|", "\\|")
            A("| %d | `%s:%d` | %s | %s |" % (i, rel, ln, st, c[:160]))
        bad = [m for m in marked if m[2]]
        A("")
        A("**结论**：%d 条标记里 **%d 条三样写全**、**%d 条缺项**%s。"
          % (len(marked), len(marked) - len(bad), len(bad),
             ("（缺项清单见上表 ⚠️ 行）" if bad else "")))
    A("")

    # ---- §2
    A("## §2 高优先候选（值/行为）—— 按「会不会让验收误当原版判据」排序")
    A("")
    A("排序键：**值证据的硬程度**（数值/数组/const > 字段初始化 > new 容器 > 字符串）"
      "− 旁边有「判据/照抄」字样则降两档。前几档最危险：**一个数字常量被当成原版判据 = 自证**。")
    A("")
    if not high_shown:
        A("（无）")
    else:
        A("**按文件分布（前 30 —— 下一轮按这个分批，一批一个文件族）**：")
        A("")
        A("| 文件 | 高优先条数 |")
        A("|---|---:|")
        cnt2 = collections.Counter(r[1] for r in high)
        for f, n in cnt2.most_common(30):
            A("| `%s` | %d |" % (f, n))
        A("")
        A("**统一标记骨架**（每一行都套这个，⛔ 三样缺一不可）—— 打在该行的**声明处**，一段只打一条，不逐行打：")
        A("")
        A("```csharp")
        A("// 🔶 自己设计（非原版）：<这是什么> · <为什么必须自己定：判据在哪 / 为什么本机拿不到> · <谁定的 + 日期>")
        A("```")
        A("")
        A("### §2 明细（%d 条 = **全部强词候选 %d 条** + 危险度最高的弱词 %d 条；块内按危险度排序）"
          % (len(high_shown), len(hi_strong),
             len(high_shown) - len([r for r in high_shown if any(w in STRONG for w in r[5])])))
        A("")
        A("**置信**列：`高` = 注释里有 `我们编的 / 我们挑的 / 非原版 / 自拟 / 自设计` 这类**强词**（作者已明确自认）；")
        A("`中` = 只有 `兜底 / 近似 / 自定义 / 硬编码` 这类**弱词** —— 逐条看一眼再决定（有的「兜底」说的是原版行为）。")
        A("")
        A("| # | 置信 | 位置 | 值证据 | 命中词 | 原文（打标就写在这个声明处） |")
        A("|---:|:--:|---|---|---|---|")
        for i, (sc, rel, ln, com, vwhy, words) in enumerate(high_shown, 1):
            c = com.replace("|", "\\|")
            conf = "高" if any(w in STRONG for w in words) else "中"
            A("| %d | %s | `%s:%d` | %s | %s | %s |"
              % (i, conf, rel, ln, vwhy, "/".join(words[:3]), c[:130]))
    A("")

    # ---- 附录 A：完整名单（**先缓冲，最后再贴**，免得夹在 §2 和 §3 中间把阅读顺序打乱）
    APX = []
    _saved_A = A
    A = APX.append
    if high_cut:
        A("## 附录 A · 完整高优先名单（其余 **%d** 条 · 按文件分组 · 只给位置）" % high_cut)
        A("")
        A("> `--top 0` 可让它们全部进 §2 明细（带原文）。本附录的用途 = **下一轮按文件批量打标时的勾选单**。")
        A("")
        _shown_keys = set((r[1], r[2]) for r in high_shown)
        rest = [r for r in high if (r[1], r[2]) not in _shown_keys]
        cur = None
        for sc, rel, ln, com, vwhy, words in sorted(rest, key=lambda r: (r[1], r[2])):
            if rel != cur:
                cur = rel
                A("")
                A("**`%s`**" % rel)
                A("")
            A("- `:%d` · %s · %s" % (ln, vwhy, "/".join(words[:2])))
        A("")
    A = _saved_A

    # ---- §3
    A("## §3 低优先（散文类）—— **建议不改写**")
    A("")
    A("判据：注释命中「我们挑的 / 我们编的 / 非原版 …」，但 **±%d 行内没有任何值/行为**" % WINDOW)
    A("⇒ 它已经用中文写清了「这是我们挑的」，读得懂；统一刷成 `🔶` 只会造成上千行 churn（铁律 18 只管「值」）。")
    A("")
    A("**共 %d 条**，按文件前 20：" % len(low))
    A("")
    if low:
        cnt = collections.Counter(r[0] for r in low)
        A("| 文件 | 条数 |")
        A("|---|---:|")
        for f, n in cnt.most_common(20):
            A("| `%s` | %d |" % (f, n))
        A("")
        A("**代表性例子（每文件取第一条，最多 15 条）**：")
        A("")
        A("| 位置 | 原文 |")
        A("|---|---|")
        seen = set()
        shown = 0
        for rel, ln, com, words in low:
            if rel in seen:
                continue
            seen.add(rel)
            A("| `%s:%d` | %s |" % (rel, ln, com.replace("|", "\\|")[:150]))
            shown += 1
            if shown >= 15:
                break
    A("")

    # ---- §4
    A("## §4 疑似误标（该是复刻 · ⛔ 不该打标记）")
    A("")
    A("判据（三档，全部进本节供人工排除）：")
    A("① **否定式** —— 注释说的就是「这**不是**我们挑的」⇒ 是复刻；")
    A("② **历史注记** —— 注释说的「**原来**是我们挑的、现已换成原版值」⇒ **现值是复刻**（指针留着没问题）；")
    A("③ **弱词 + 复刻证据** —— 注释里出现 `照抄 / 照原版 / 复刻 / 逐句 / 判据 / decomp / RVA`，")
    A("   而自设计侧**只有弱词**（`自定义 / 硬编码 / 拍脑袋 / 兜底 / 近似` …）、没有 `我们编的 / 非原版 / 自设计`。")
    A("")
    A("**共 %d 条** —— 逐条人工确认；确认是复刻的，⛔ 别打 `🔶`（铁律 18 明令）。" % len(suspect))
    A("")
    if suspect:
        cat = collections.Counter(r[4].split("：")[0] for r in suspect)
        A("| 分类 | 条数 |")
        A("|---|---:|")
        for c_, n_ in cat.most_common():
            A("| %s | %d |" % (c_, n_))
        A("")
        SUS_TOP = 80
        A("### §4 明细（前 %d 条，带原文）" % min(SUS_TOP, len(suspect)))
        A("")
        A("| 位置 | 分类 | 命中词 | 原文 |")
        A("|---|---|---|---|")
        for rel, ln, com, words, why in suspect[:SUS_TOP]:
            A("| `%s:%d` | %s | %s | %s |"
              % (rel, ln, why, "/".join(words[:3]), com.replace("|", "\\|")[:130]))
        if len(suspect) > SUS_TOP:
            A("")
            A("### §4 其余 %d 条（只给位置 · 排除时按文件勾）" % (len(suspect) - SUS_TOP))
            cur = None
            for rel, ln, com, words, why in suspect[SUS_TOP:]:
                if rel != cur:
                    cur = rel
                    A("")
                    A("**`%s`**" % rel)
                    A("")
                A("- `:%d` · %s" % (ln, why))
    A("")

    # ---- §5
    A("## §5 判不准的 / 已知热点核对")
    A("")
    A("### 5.1 已知热点（人工列出 · 必须覆盖）")
    A("")
    A("| 热点 | 锚点 | 状态 |")
    A("|---|---|---|")
    for rel, needle, desc, st, need in hot_rows:
        A("| %s | `%s` → `%s` | %s |" % (desc, rel, needle.replace("|", "\\|"), st))
    A("")
    A("### 5.2 判不准的（脚本分不清，留给人工）")
    A("")
    A("- **`自定义` / `硬编码` 这两个词多半在描述「原版是硬编码的」**（复刻事实），不是自设计 —— §3/§4 里见到别急着打标。")
    A("- **`兜底` / `近似` 既可能是「我们挑的兜底」也可能是「原版就那样」** —— 要看它后面跟的是不是一个**我们编的值**。")
    A("- **注释里的「原版没有 X ⇒ 我们补了 Y」**：如果 Y 的**行为**是原版真实存在的（只是换了载体），那就是复刻（铁律 18 ②）；")
    A("  只有当 **Y 的语义/数值是我们自己定的** 时才打标 —— 脚本无法区分，逐条人工判。")
    A("- **同一族里「原版字段有、我们没这个字段」的等价写法**（例：`Type == \"defence\"` 代替 `spellType`）——")
    A("  这是**实现风格**（铁律 18 ②），⛔ 不打标；只有当那个等价判据**本身是我们挑的**才打。")
    A("")
    A("### 5.3 跨距离候选（文件头点名说了「这个值是我们挑的」，声明处还没打标）")
    A("")
    A("判据：该文件**头 80 行**有一句含 `我们编的 / 我们挑的 / 非原版 / 自拟 / 我们补的 / 硬编码` 的注释，")
    A("从这句（±2 行）里抽出**标识符**，再去文件里找**同名**的常量/只读/带初值字段声明；")
    A("该声明附近（±8 行）没有 marker、自己也没写强词 ⇒ 进本节。")
    A("")
    A("> ⚠️ 别改成「头里有自设计词 ⇒ 全文件常量都算」—— 实测那样会刷出 **3,000+ 条噪声**")
    A("> （`我们设计的 26 张卡` 那一族会把整族文件的所有字面量都拖进来）。")
    A("")
    A("**共 %d 条**（最典型的坑：`CardDetailPopup` 的自设计说明在文件头、值在 100 行外）。" % len(xdist))
    A("")
    if xdist:
        cntx = collections.Counter(r[0] for r in xdist)
        A("| 文件 | 条数 |")
        A("|---|---:|")
        for f, n in cntx.most_common(20):
            A("| `%s` | %d |" % (f, n))
        A("")
        A("**前 60 条位置**：")
        A("")
        A("| 位置 | 头部提到的名字 | 声明原文 |")
        A("|---|---|---|")
        for rel, ln, code, nm in xdist[:60]:
            A("| `%s:%d` | `%s` | `%s` |" % (rel, ln, nm, code.replace("|", "\\|")))
    A("")
    if bare_cr:
        A("")
        A("### 5.4 ⚠️ 有裸 CR 的文件（行号可能与本表不一致）")
        A("")
        for rel, n in bare_cr:
            A("- `%s`（裸 CR %d 个）" % (rel, n))
    A("")

    L.extend(APX)          # 附录 A 贴到全文最后（见上面那段缓冲的原因）
    md = "\n".join(L) + "\n"

    if OUT:
        d = os.path.dirname(OUT)
        if d and not os.path.isdir(d):
            os.makedirs(d)
        io.open(OUT, "w", encoding="utf-8", newline="\n").write(md)
        if not QUIET:
            print("写出 %s（%d 字节）" % (OUT, len(md.encode("utf-8"))))
    else:
        try:
            sys.stdout.reconfigure(encoding="utf-8")
        except Exception:
            pass
        sys.stdout.write(md)

    if JSON_OUT:
        import json
        d = os.path.dirname(JSON_OUT)
        if d and not os.path.isdir(d):
            os.makedirs(d)
        obj = {
            "root": ROOT.replace("\\", "/"),
            "files": len(files),
            "lines": TOTAL_LINES[0] or 0,
            "marked": [{"file": r, "line": l, "missing": m} for r, l, m, _ in marked],
            "high": [{"file": r, "line": l, "score": s, "value": v, "words": w, "text": t}
                     for s, r, l, t, v, w in high],
            "low": [{"file": r, "line": l, "words": w, "text": t} for r, l, t, w in low],
            "suspect": [{"file": r, "line": l, "words": w, "why": y, "text": t}
                        for r, l, t, w, y in suspect],
            "xdist": [{"file": r, "line": l, "code": c, "name": n} for r, l, c, n in xdist],
            "hotspots": [{"file": r, "needle": n, "desc": d, "status": s} for r, n, d, s, _ in hot_rows],
            "bare_cr": bare_cr,
        }
        io.open(JSON_OUT, "w", encoding="utf-8", newline="\n").write(
            json.dumps(obj, ensure_ascii=False, indent=1, sort_keys=True))
        if not QUIET:
            print("写出 %s" % JSON_OUT)

    if not QUIET:
        sys.stderr.write("[统计] 文件 %d · 行 %d · 已标注 %d · 高优先 %d · 低优先 %d · 疑似误标 %d · 跨距离 %d · 裸CR文件 %d\n"
                         % (len(files), TOTAL_LINES[0] or 0, total_mark, len(high), len(low),
                            len(suspect), len(xdist), len(bare_cr)))


TOTAL_LINES = [0]

if __name__ == "__main__":
    # 先量一遍总行数（幂等；只读）
    for _p in walk_cs(ROOT):
        _lines, _ = read_lines(_p)
        TOTAL_LINES[0] += len(_lines)
    main()
