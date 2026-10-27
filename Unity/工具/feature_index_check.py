#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""feature_index_check.py — 「功能点验收索引」（第二层账）的**只读**机检（正式版）。

⛔ 只读：不改账、不改索引、不改任何 .cs。默认行为 = 只读检查；不带参数只打印用法。
⛔ 它证明不了「这个功能点复刻对了」—— 只证明**这张索引自身自洽、且不是凭空编的**：
   · 每个 `原版构成` 条目都能在 `账/*.tsv` 里展开成 ≥1 行；
   · `构成条数` / `状态分布` / `等级` 三列 = 由账**重算**出来的值（手写对不上就红）；
   · 每个 `我们的落点` / `验收口` 都能按名字/子串在源码里**现读**到；
   · 每个 `判据出处` 指向的**文件真的存在、行号不越界**（不是只验前缀）；
   · `判据出处` 不许指向我们自己的代码（自证闸）。

用法
====
  # ① 检查一份索引 / part
  python -I -X utf8 feature_index_check.py <索引.tsv> [选项]

  # ② 派活用：打印「域 → 账行数 → 在哪些文件」
  python -I -X utf8 feature_index_check.py --manifest [--ledger DIR]

  # ③ 写 `判据出处` 前先问它：把一个裸名/相对名解析成真实完整路径
  python -I -X utf8 feature_index_check.py --resolve-judge ContainerOfferEvent__OpenOfferContainer.c ShopStore.cs

  # ④ 自检这道闸会不会红（对一份**已知全绿**的索引做 7 类故障注入）
  python -I -X utf8 feature_index_check.py <索引.tsv> --self-test

选项
====
  --ledger DIR     账目录（默认 D:/4/Unity/资料/完全复刻/账）
  --src DIR        我们源码根（默认 D:/4/Unity/MyGame/Assets）
  --domain NAME    **按域口径**：未认领/多认领只统计该域；并检查「本块没认领别的域的账行」。
                   part 阶段必须带它 —— 否则每个 part 都打「未认领 9,26x」，无信息量。
  --strict         多认领 ≥1 直接判失败；`我们的落点` / `验收口` 为空串（忘填）也判失败。
                   **合并脚本一律带 `--strict`**；单独过 part 时可以不带（保持基线不抖）。
  --no-judge-exists  跳过「判据文件存在性」检查（应急；默认开）
  --quiet          只印失败，不印告警明细
  -h / --help      打印本说明

退出码：0 = 全过；1 = 有失败；2 = 用法错。

五道闸（判红即失败）
====================
  C1 构成可展开       token 打错 / 凭空编方法名
  C2 三列等于重算      `构成条数` `状态分布` `等级` 与账不符（账一动就红）
  C3 落点现读          `路径:行（名）`：文件在 · 行在界 · 该行逐字含那个名字
  C4 验收口现读        `路径:行（子串）`：该行逐字含那个子串；`cmd:` 脚本存在
  C5 判据现读（🆕）    `判据出处` 每个 token：① 必须带原版前缀 ② **文件逐字存在** ③ 行号不越界
                       ④ 不含我们自己的路径。**路径写错 = 失败**，并打印「应写成 …」的完整路径。
  告警              未认领账行 / 多认领账行（`--strict` 下多认领判失败）/ 落点为空串 / 无落点计数

`判据出处` 的公共映射（🆕，别让每个写手自己猜）
=============================================
  前缀 → 真实根目录：
    decomp_full/  → D:/2/tools/decomp_full
    cpp2il/       → D:/2/tools/cpp2il                 （cpp2il/out_cpp2il_dev_ilspy/…）
    assets_full/  → D:/2/新解包资源/assets_full        （bundle_ 开头也落这里）
    判据/ 账/ 报告/ → D:/4/Unity/资料/完全复刻/
    资料/         → D:/4/Unity/
    D:/… d:/…     → 原样
  路径写错时**不是靠猜**：脚本在真目录里按「同名 / 带命名空间前缀的同名」反查，给出唯一答案；
  查不到或有歧义 ⇒ 判失败（歧义也无从复核）。例：
    写 `decomp_full/ContainerOfferEvent__OpenOfferContainer.c`
    ⇒ 实际在 `decomp_full/Everguild.LiveOps.ContainerOfferEvent__OpenOfferContainer.c`
  写手可以在动手前跑 `--resolve-judge <名字>` 把结果直接抄进表。

`等级` 列可以带后缀（🆕）
=========================
  `?`   = 有 `存疑行`（与 `存疑行` 非空必须一致，机检互相校验）
  `🔶` = 本功能点含 `🔶 自己设计`（铁律 18）的构成行 —— **按基档计数**（`A🔶` 算 A），
         但比率统计里**单列**，因为「我们自己编的」不能算「照原版复刻」。
  例：`A` `A?` `C🔶` `C🔶?` 都合法。
"""
import io
import os
import re
import sys
import shutil
import collections
import tempfile

# ---------------------------------------------------------------- 常量
PROJ = r"D:/4/Unity"
LEDGER = PROJ + "/资料/完全复刻/账"
SRC = PROJ + "/MyGame/Assets"

COLS = ["功能点ID", "功能点名", "域", "优先级", "主链/边角", "原版构成", "构成条数",
        "状态分布", "等级", "我们的落点", "验收口", "跑法", "判据出处", "存疑行", "轮次", "备注"]

DOMAINS = {"收藏", "战斗界面/HUD", "战斗逻辑驱动", "卡牌面/渲染", "特效动画", "菜单/外壳",
           "卡组编辑", "商店", "锻造", "战役", "日常/奖励", "轮抽", "社交/聊天",
           "个人资料/成就/排行", "本地化", "音频/语音", "联机", "教程/引导",
           "其他/基础设施", "未分类/其他"}

# `判据出处` 的合法前缀（🥇 反编译 → 🥉 解包资源 → 判据文件）
JUDGE_OK = ("cpp2il/", "decomp_full/", "assets_full/", "bundle_", "判据/", "D:/2/", "d:/2/",
            "D:\\2\\", "d:\\2\\")
# 指向我们自己代码的（自证闸）
SELF_PAT = ("MyGame/Assets", "CardPresentation/", "RuleEngine/Core", "RuleEngine/Data")

# 前缀 → 真实根（🆕 公共映射，写手不用猜）
JUDGE_ROOTS = [
    ("decomp_full/", r"D:/2/tools/decomp_full"),
    ("cpp2il/", r"D:/2/tools/cpp2il"),
    ("assets_full/", r"D:/2/新解包资源/assets_full"),
    ("判据/", PROJ + "/资料/完全复刻/判据"),
    ("账/", PROJ + "/资料/完全复刻/账"),
    ("报告/", PROJ + "/资料/完全复刻/报告"),
    ("资料/", PROJ),
]
ASSETS_ROOT = r"D:/2/新解包资源/assets_full"
CS_ROOT = r"D:/2/tools/cpp2il/out_cpp2il_dev_ilspy"
C_ROOT = r"D:/2/tools/decomp_full"

BADGE_ORDER = ["E", "C", "B", "A"]     # 越靠前越「没完成」
# 只有这些扩展名才做「行号在界」检查（资产路径 / 二进制文件只验存在）
TEXT_EXT = (".c", ".cs", ".md", ".txt", ".tsv", ".json", ".py", ".h", ".hpp", ".cpp", ".yaml", ".yml")
# `我们的落点` 的合法「无落点」哨兵
NOPOINT = ("—", "——", "—", "-", "无", "N/A", "n/a", "NA")


def fail(lst, m):
    lst.append(m)


# ---------------------------------------------------------------- 读文件（二进制 · 行号与编辑器一致）
_lcache = {}


def read_lines(path):
    """按二进制读、自己按 \\n 切（⛔ 不用文本模式：裸 CR 会让行号整体偏）。末尾空行不算一行。"""
    if path in _lcache:
        return _lcache[path]
    try:
        with io.open(path, "rb") as f:
            data = f.read()
    except OSError:
        _lcache[path] = None
        return None
    lines = [x.rstrip(b"\r").decode("utf-8", "replace") for x in data.split(b"\n")]
    if lines and lines[-1] == "":
        lines.pop()
    _lcache[path] = lines
    return lines


# ---------------------------------------------------------------- 账
def load_ledger(d):
    """返回 rows: list of dict(域,类,方法,判据出处,状态,落点,差异,等级,轮次,备注,src=文件名行)"""
    rows = []
    if not os.path.isdir(d):
        return rows
    for fn in sorted(x for x in os.listdir(d) if x.endswith(".tsv") and not x.startswith("_")):
        p = os.path.join(d, fn)
        with io.open(p, "r", encoding="utf-8") as f:
            next(f, None)
            for i, line in enumerate(f, 2):
                c = line.rstrip("\n").split("\t")
                if len(c) != 11:
                    continue
                rows.append(dict(域=c[0].strip(), 类=c[1].strip(), 方法=c[2].strip(),
                                 判据出处=c[3].strip(), 状态=c[5].strip()[:1] or "E",
                                 落点=c[6].strip(), 差异=c[7].strip(), 等级=c[8].strip(),
                                 轮次=c[9].strip(), 备注=c[10].strip(),
                                 src="%s:%d" % (fn, i)))
    return rows


def base(m):
    return m.split("(")[0].strip()


def expand(tok, rows):
    """把 `类.*` / `类.方法` / `类.方法(全签名)` 展开成账行。返回 (rows, err)"""
    tok = tok.strip()
    if not tok:
        return [], None
    if "." not in tok:
        return [], "条目没有 `类.方法` 形状：%r" % tok
    cls, meth = tok.split(".", 1)
    cls, meth = cls.strip(), meth.strip()
    if meth == "*":
        hit = [r for r in rows if r["类"] == cls]
        return (hit, None) if hit else ([], "类不存在于账中：%s" % cls)
    if "(" in meth:
        hit = [r for r in rows if r["类"] == cls and r["方法"].replace(" ", "") == meth.replace(" ", "")]
        return (hit, None) if hit else ([], "全签名匹配不到：%s" % tok)
    hit = [r for r in rows if r["类"] == cls and base(r["方法"]) == meth]
    return (hit, None) if hit else ([], "方法不存在于账中：%s" % tok)


def grade_of(states):
    s = set(states)
    if not s:
        return "E"
    if s <= {"D"}:
        return "D"
    for b in BADGE_ORDER:            # E > C > B > A
        if b in s:
            return b
    return "A"


def dist_of(states):
    c = collections.Counter(states)
    return "·".join("%s%d" % (k, c[k]) for k in "ABCDE" if c[k])


# ---------------------------------------------------------------- 判据出处：公共映射
def abs_path(raw):
    """把 `判据出处` 里的路径写法落到真实绝对路径（不做存在性判断）。"""
    raw = raw.strip().replace("\\", "/").strip("`'\" ")
    if re.match(r"^[A-Za-z]:/", raw):
        return raw
    if raw.startswith("bundle_"):
        return ASSETS_ROOT + "/" + raw
    for pre, root in JUDGE_ROOTS:
        if raw.startswith(pre):
            return os.path.join(root, raw[len(pre):])
    return None


def to_index_style(path):
    """绝对路径 → 索引里的写法（给失败信息当「应写成 …」）。"""
    p = path.replace("\\", "/")
    for pre, root in JUDGE_ROOTS:
        r = root.replace("\\", "/")
        if p.startswith(r + "/"):
            return pre + p[len(r) + 1:]
    return p


class JudgeMap(object):
    """账/索引里的**相对名 → 真实完整路径**。懒建索引、结果记忆化。

    两种「写错」都靠它在真目录里反查：
      · `.cs` 少了/多了命名空间子目录（`cpp2il/…/Everguild.LiveOps/ShopStore.cs` 其实在根下）
      · `.c`  少了命名空间前缀（`ContainerOfferEvent__Open…` 真名带 `Everguild.LiveOps.`）
    """

    def __init__(self):
        self._cs = None
        self._c = None
        self._memo = {}

    def cs_index(self):
        if self._cs is None:
            d = collections.defaultdict(list)
            for dp, _, fns in os.walk(CS_ROOT):
                for fn in fns:
                    if fn.endswith(".cs"):
                        d[fn].append(os.path.join(dp, fn))
            self._cs = d
        return self._cs

    def c_index(self):
        if self._c is None:
            d = collections.defaultdict(list)
            for dp, _, fns in os.walk(C_ROOT):
                for fn in fns:
                    full = os.path.join(dp, fn)
                    d[fn].append(full)
                    # 注册「每个点之后的后缀」，这样丢了命名空间前缀也能反查到
                    i = fn.find(".")
                    while i >= 0:
                        suf = fn[i + 1:]
                        if len(suf) >= 8:
                            d[suf].append(full)
                        i = fn.find(".", i + 1)
            self._c = d
        return self._c

    def resolve(self, raw):
        """返回 (kind, path_or_list, hint)：kind ∈ exact / mapped / ambig / missing / nopath。"""
        raw = raw.strip().replace("\\", "/").strip("`'\" ")
        if raw in self._memo:
            return self._memo[raw]
        r = self._resolve(raw)
        self._memo[raw] = r
        return r

    def _resolve(self, raw):
        p = abs_path(raw)
        if p and os.path.exists(p):
            return ("exact", p, "")
        name = raw.split("/")[-1]
        if name.endswith(".cs"):
            cands = self.cs_index().get(name, [])
        elif name.endswith(".c"):
            cands = self.c_index().get(name, [])
        else:
            cands = []
        cands = sorted(set(cands))
        if len(cands) == 1:
            return ("mapped", cands[0].replace("\\", "/"), "应写成 `%s`" % to_index_style(cands[0]))
        if len(cands) > 1:
            return ("ambig", [x.replace("\\", "/") for x in cands],
                    "有 %d 处同名，须写完整路径：%s"
                    % (len(cands), " | ".join(to_index_style(x) for x in cands[:4])))
        return ("missing", None, "")


_JMAP = JudgeMap()


def split_path_line(s):
    """`路径:行号段` → (路径, 行号段)；盘符里的 `:` 不算分隔符。"""
    start = 2 if re.match(r"^[A-Za-z]:", s) else 0
    i = s.find(":", start)
    if i < 0:
        return s, ""
    return s[:i], s[i + 1:]


def head_of(tok):
    """去掉 `（……）` 与空白之后的散文，只留路径+行号段。"""
    s = tok.strip()
    for sep in ("（", "("):
        i = s.find(sep)
        if i > 0:
            s = s[:i]
    s = s.strip()
    if " " in s:
        s = s.split(" ")[0]
    if "\t" in s:
        s = s.split("\t")[0]
    return s.strip().strip("`'\" ")


def check_judge_token(tok, tag, fails, opts):
    """C5 单条：前缀 → 自证 → 文件逐字存在 → 行号在界。返回 True=过。"""
    t = tok.strip().strip("`'\" ")
    if not t:
        return True
    if not any(k in t for k in JUDGE_OK):
        fm = _JMAP.resolve(head_of(t))
        hint = ("（它在 `%s`，请改成带前缀的写法）" % to_index_style(fm[1])
                if fm[0] == "exact" else "")
        fail(fails, "%s 判据出处不像原版判据（缺 `decomp_full/` `cpp2il/` `判据/` 这类前缀）%s：%s"
             % (tag, hint, t[:90]))
        return False
    if any(b in t for b in SELF_PAT):
        fail(fails, "%s 判据出处指向我们自己的代码（自证）：%s" % (tag, t[:90]))
        return False
    if not opts.get("judge_exists", True):
        return True
    raw, lspec = split_path_line(head_of(t))
    if not raw:
        fail(fails, "%s 判据出处取不出路径：%s" % (tag, t[:90]))
        return False
    kind, path, hint = _JMAP.resolve(raw)
    if kind == "exact":
        pass
    elif kind == "mapped":
        fail(fails, "%s 判据路径不逐字存在：%s ⇒ %s" % (tag, t[:90], hint))
        return False
    elif kind == "ambig":
        fail(fails, "%s 判据路径有歧义：%s ⇒ %s" % (tag, t[:90], hint))
        return False
    else:
        fail(fails, "%s 判据文件不存在：%s（公共映射里也没有同名文件）" % (tag, t[:90]))
        return False
    if os.path.isdir(path):
        return True
    ext = os.path.splitext(path)[1].lower()
    if ext not in TEXT_EXT:
        return True                      # 资产 / 二进制：只验存在，⛔ 不按行号读
    lines = read_lines(path)
    if lines is None:
        fail(fails, "%s 判据文件读不了：%s" % (tag, path))
        return False
    nums = [int(x) for x in re.findall(r"\d+", lspec)]
    if nums:
        lo, hi = nums[0], nums[-1]
        if lo < 1 or hi > len(lines) or lo > hi:
            fail(fails, "%s 判据行号越界：%s（文件共 %d 行）" % (tag, t[:90], len(lines)))
            return False
    return True


# ---------------------------------------------------------------- 落点 / 验收口现读
def check_point(tok, src_root, need_text=True):
    """`文件:行（名）` → (ok, msg)。need_text=False ⇒ 只验行存在（存疑行的括号是散文）。"""
    s = tok.strip().strip("`'\" ")
    if not s:
        return False, "空条目"
    if ":" not in s:
        return False, "没有 `文件:行` 形状：%r" % s
    head, rest = split_path_line(s)
    num = ""
    for ch in rest:
        if ch.isdigit():
            num += ch
        else:
            break
    if not num:
        return False, "行号缺失：%r" % s
    ln = int(num)
    path = head.strip()
    full = path if os.path.isabs(path) else os.path.join(src_root, path)
    if not os.path.exists(full):
        alt = os.path.join(PROJ, path)
        if os.path.exists(alt):
            full = alt
        else:
            return False, "文件不存在：%s" % path
    lines = read_lines(full)
    if lines is None:
        return False, "文件读不了：%s" % path
    if ln < 1 or ln > len(lines):
        return False, "行号越界：%s（文件共 %d 行）" % (s, len(lines))
    text = lines[ln - 1]
    inner = rest[len(num):].strip()
    inner = inner.lstrip("（(").rstrip("）)").strip()
    if need_text and inner and inner.strip("`'\" ") and inner.strip("`'\" ") not in text:
        return False, "该行不含声明名/子串：%s\n        实际内容：%s" % (s, text.strip()[:110])
    return True, text.strip()[:110]


# ---------------------------------------------------------------- 等级列解析
def parse_grade(cell):
    """`A` / `A?` / `C🔶` / `C🔶?` → (base, has_selfdesign, has_q)"""
    s = cell.strip()
    sd = "🔶" in s
    s = s.replace("🔶", "").strip()
    q = s.endswith("?")
    s = s.rstrip("?").strip()
    return s, sd, q


# ---------------------------------------------------------------- 主检查
def run_check(dest, opts):
    """返回 dict(fails, warns, n, stats...)。不打印。"""
    fails, warns = [], []
    rows = load_ledger(opts["ledger"])
    with io.open(dest, "r", encoding="utf-8") as f:
        head = f.readline().rstrip("\n").split("\t")
        if head != COLS:
            fail(fails, "表头不符（应为 16 列，见 COLS）：实得 %r" % (head,))
            return dict(fails=fails, warns=warns, n=0, rows=rows)
        ids, claimed = collections.Counter(), collections.Counter()
        n = 0
        stats = dict(grades=[], selfdesign=[], nopoint=0, noacct=0, judge_n=0, point_n=0, acct_n=0)
        cls_files = None
        for lineno, line in enumerate(f, 2):
            c = line.rstrip("\n").split("\t")
            if len(c) != len(COLS):
                fail(fails, "第 %d 行列数 %d ≠ %d" % (lineno, len(c), len(COLS)))
                continue
            d = dict(zip(COLS, c))
            n += 1
            tag = "%s（第%d行）" % (d["功能点ID"] or "<无ID>", lineno)
            ids[d["功能点ID"]] += 1
            if d["域"] not in DOMAINS:
                fail(fails, "%s 域不在 %d 域内：%r" % (tag, len(DOMAINS), d["域"]))
            if d["优先级"] not in ("P0", "P1", "P2", "P3"):
                fail(fails, "%s 优先级非法：%r" % (tag, d["优先级"]))
            if d["主链/边角"] not in ("主链", "边角"):
                fail(fails, "%s 主链/边角非法：%r" % (tag, d["主链/边角"]))
            # --- 原版构成 → 展开 ---
            exp, toks = [], [t for t in d["原版构成"].split(";") if t.strip()]
            if not toks:
                fail(fails, "%s 原版构成为空" % tag)
            for t in toks:
                hit, err = expand(t, rows)
                if err:
                    fail(fails, "%s 构成条目无法展开：%s" % (tag, err))
                exp.extend(hit)
            # --- 三列必须等于重算值 ---
            if str(len(exp)) != d["构成条数"].strip():
                fail(fails, "%s 构成条数 手写=%r 重算=%d" % (tag, d["构成条数"], len(exp)))
            dist = dist_of([r["状态"] for r in exp])
            if dist != d["状态分布"].strip():
                fail(fails, "%s 状态分布 手写=%r 重算=%r" % (tag, d["状态分布"], dist))
            g = grade_of([r["状态"] for r in exp])
            gcol, selfd, has_q = parse_grade(d["等级"])
            if gcol not in ("A", "B", "C", "D", "E"):
                fail(fails, "%s 等级非法：%r" % (tag, d["等级"]))
            elif gcol != g:
                fail(fails, "%s 等级 手写=%r 重算=%r" % (tag, d["等级"].strip(), g))
            has_doubt = d["存疑行"].strip() not in ("", "—")
            if has_q != has_doubt:
                fail(fails, "%s 等级 `?` 后缀与 `存疑行` 不一致（有存疑行就要带 ?）" % tag)
            for r in exp:
                claimed[r["src"]] += 1
            stats["grades"].append(gcol)
            if selfd:
                stats["selfdesign"].append((d["功能点ID"], gcol))
            # --- 落点 / 验收口 ---
            for col in ("我们的落点", "验收口"):
                toks2 = [x for x in d[col].split(";") if x.strip()]
                if not toks2:
                    m = "%s `%s` 为空串（要么填内容，要么写 `—` 表示「确认没有」）" % (tag, col)
                    if opts.get("strict"):
                        fail(fails, m)
                    else:
                        warns.append(m)
                    if col == "我们的落点":
                        stats["nopoint"] += 1
                    else:
                        stats["noacct"] += 1
                    continue
                for t in toks2:
                    t = t.strip()
                    if t in NOPOINT:
                        if col == "我们的落点":
                            stats["nopoint"] += 1
                        else:
                            warns.append("%s `验收口` 写的是「%s」—— 本功能点没有回归探针" % (tag, t))
                            stats["noacct"] += 1
                        continue
                    if t.startswith("cmd:"):
                        if col != "验收口":
                            fail(fails, "%s `%s` 里出现 cmd：%s" % (tag, col, t))
                        script = t[4:].split()[0]
                        sp = script if os.path.isabs(script) else os.path.join(PROJ, script)
                        if not os.path.exists(sp):
                            fail(fails, "%s 验收口脚本不存在：%s" % (tag, script))
                        else:
                            stats["acct_n"] += 1
                        continue
                    ok, msg = check_point(t, opts["src"])
                    if not ok:
                        fail(fails, "%s %s：%s" % (tag, col, msg))
                    elif col == "我们的落点":
                        stats["point_n"] += 1
                    else:
                        stats["acct_n"] += 1
            # --- 跑法 ---
            if cls_files is None:
                cls_files = {}
                for root, _, files in os.walk(opts["src"]):
                    for fn in files:
                        if fn.endswith(".cs"):
                            cls_files.setdefault(fn, []).append(os.path.join(root, fn))
            for t in [x for x in d["跑法"].split(";") if x.strip()]:
                t = t.strip()
                if "-executeMethod" in t:
                    entry = t.split("-executeMethod")[1].strip().split()[0]
                    if "." not in entry:
                        fail(fails, "%s 跑法入口没有 `类.方法` 形状：%s" % (tag, entry))
                        continue
                    cls, meth = entry.split(".", 1)
                    hits = cls_files.get(cls + ".cs", [])
                    if not hits:
                        fail(fails, "%s 跑法入口类不存在：%s" % (tag, cls))
                    else:
                        body = "\n".join(read_lines(hits[0]) or [])
                        if ("void %s(" % meth) not in body:
                            fail(fails, "%s 跑法入口方法不存在：%s（在 %s）" % (tag, entry, hits[0]))
            # --- 判据出处（C5：前缀 + 自证 + 文件逐字存在 + 行号在界）---
            jd = d["判据出处"].strip()
            if not jd or jd in NOPOINT:
                fail(fails, "%s 判据出处为空" % tag)
            else:
                jtoks = [x for x in jd.split(";") if x.strip()]
                if not jtoks:
                    fail(fails, "%s 判据出处为空" % tag)
                for t in jtoks:
                    if check_judge_token(t, tag, fails, opts):
                        stats["judge_n"] += 1
            # --- 存疑行 ---
            for t in [x for x in d["存疑行"].split(";") if x.strip() and x.strip() not in NOPOINT]:
                ok, msg = check_point(t, opts["ledger"], need_text=False)
                if not ok:
                    fail(fails, "%s 存疑行：%s" % (tag, msg))
    # --- 全局 ---
    for k, v in ids.items():
        if v > 1:
            fail(fails, "功能点ID 重复 %d 次：%s" % (v, k))
        if not k.startswith("FP-"):
            fail(fails, "功能点ID 不以 FP- 开头：%s" % k)
    multi = [r for r in rows if claimed.get(r["src"], 0) > 1]
    for r in multi:
        m = "一行被 %d 个功能点同时认领：%s %s.%s" % (claimed[r["src"]], r["src"], r["类"], r["方法"][:50])
        if opts.get("strict"):
            fail(fails, m + "（`--strict` ⇒ 判失败：一行只能有一个 primary owner）")
        else:
            warns.append(m)
    dom = opts.get("domain")
    if dom:
        dom_rows = [r for r in rows if r["域"] == dom]
        unclaimed = [r for r in dom_rows if r["src"] not in claimed]
        cross = [r for r in rows if r["src"] in claimed and r["域"] != dom]
        for r in cross[:20]:
            fail(fails, "本块认领了别的域的账行（本块域=`%s`）：%s %s（域=`%s`）"
                 % (dom, r["src"], r["类"], r["域"]))
        info = dict(domain=dom, dom_rows=len(dom_rows),
                    dom_claimed=len(dom_rows) - len(unclaimed),
                    dom_unclaimed=len(unclaimed), cross=len(cross))
    else:
        unclaimed = [r for r in rows if r["src"] not in claimed]
        info = dict(domain=None, dom_rows=len(rows), dom_claimed=len(rows) - len(unclaimed),
                    dom_unclaimed=len(unclaimed), cross=0)
    return dict(fails=fails, warns=warns, n=n, rows=rows, unclaimed=unclaimed,
                multi=multi, claimed=claimed, stats=stats, info=info)


def _pct(a, b):
    return "—" if not b else "%.1f%%" % (100.0 * a / b)


def report(res, opts):
    fails, warns = res["fails"], res["warns"]
    st, info = res["stats"], res["info"]
    rows = res["rows"]
    n = res["n"]
    print("=" * 72)
    if info["domain"]:
        claimed_n = len([1 for r in rows if r["src"] in res["claimed"]])
        print("域 `%s`：账行 %d | 已认领 %d | 未认领 %d | 多认领 %d | 越域认领 %d"
              % (info["domain"], info["dom_rows"], info["dom_claimed"],
                 info["dom_unclaimed"], len(res["multi"]), info["cross"]))
        print("全库（仅信息，part 阶段不看它）：账行 %d | 本份认领 %d | 未认领 %d"
              % (len(rows), claimed_n, len(rows) - claimed_n))
    else:
        print("功能点 %d 条 | 账行 %d | 已被认领 %d | 未认领 %d | 多认领 %d"
              % (n, len(rows), len(rows) - len(res["unclaimed"]),
                 len(res["unclaimed"]), len(res["multi"])))
    # --- 比率（设计 §四 口径）---
    gs = st["grades"]
    ad = len([g for g in gs if g in ("A", "D")])
    abc = len([g for g in gs if g in ("A", "B", "C")])
    a_n = len([g for g in gs if g == "A"])
    print("比率（功能点级）：(A+D)/总 = %d/%d = %s · 真复刻率 A/(A+B+C) = %d/%d = %s"
          % (ad, len(gs), _pct(ad, len(gs)), a_n, abc, _pct(a_n, abc)))
    print("读数口：落点 %d 条 · 验收口 %d 条 · 判据 %d 条 · 无落点(`—`) %d 个功能点 · 无验收口 %d 个"
          % (st["point_n"], st["acct_n"], st["judge_n"], st["nopoint"], st["noacct"]))
    if st["selfdesign"]:
        print("🔶 自己设计：含标记的功能点 %d 条（等级 A 的 %d 条 —— 这些 A 是「我们自己编的」，"
              "⛔ 不算照原版复刻）：%s"
              % (len(st["selfdesign"]),
                 len([1 for _, g in st["selfdesign"] if g == "A"]),
                 " · ".join("%s(%s)" % (i, g) for i, g in st["selfdesign"][:8])))
    print("失败 %d | 告警 %d" % (len(fails), len(warns)))
    for m in fails[:60]:
        print("  ✗ " + m)
    if len(fails) > 60:
        print("  … 还有 %d 条失败" % (len(fails) - 60))
    if not opts.get("quiet"):
        for m in warns[:20]:
            print("  ⚠ " + m)
        if len(warns) > 20:
            print("  … 还有 %d 条告警" % (len(warns) - 20))
    if not fails and info["dom_unclaimed"]:
        if info["domain"]:
            print("  ⚠ 域 `%s` 还有 %d 条账行没被认领（part 级通过条件 = 0）"
                  % (info["domain"], info["dom_unclaimed"]))
            for r in res["unclaimed"][:15]:
                print("     %s  %s.%s" % (r["src"], r["类"], r["方法"][:60]))
        else:
            print("  ⚠ 还有 %d 条账行没被任何功能点认领（索引尚不完整，不是「全绿」）"
                  % info["dom_unclaimed"])
            for r in res["unclaimed"][:15]:
                print("     %s  %s.%s" % (r["src"], r["类"], r["方法"][:60]))
    elif not fails and info["domain"]:
        print("  ✓ 域 `%s` 未认领 = 0（part 级通过条件满足；多认领/越域见上）" % info["domain"])
    return 0 if not fails else 1


# ---------------------------------------------------------------- 自检：闸会不会红
def self_test(dest, opts):
    """对一份**已知全绿**的索引做 7 类故障注入，逐条断言被抓到。只写临时目录。"""
    base_res = run_check(dest, opts)
    if base_res["fails"]:
        print("✗ 自检基线不成立：原文件本身就有 %d 条失败，先修好再来" % len(base_res["fails"]))
        for m in base_res["fails"][:10]:
            print("  ✗ " + m)
        return 1
    with io.open(dest, "r", encoding="utf-8") as f:
        head = f.readline()
        lines = [x.rstrip("\n") for x in f.readlines()]
    if not lines:
        print("✗ 索引没有数据行")
        return 1
    tmp = tempfile.mkdtemp(prefix="fic_selftest_")
    try:
        def row(i):
            return lines[i].split("\t")

        def write(name, rows_):
            p = os.path.join(tmp, name)
            with io.open(p, "w", encoding="utf-8", newline="\n") as f:
                f.write(head)
                for r in rows_:
                    f.write("\t".join(r) + "\n")
            return p

        cases = []

        def add(title, rows_, expect, strict=False):
            p = write("t%d.tsv" % len(cases), rows_)
            o = dict(opts, strict=strict)
            r = run_check(p, o)
            hit = any(expect in m for m in r["fails"])
            cases.append((title, hit, expect, r["fails"][:2]))

        # 1 判据路径写错（缺命名空间前缀）
        r0 = [row(i) for i in range(len(lines))]
        r0[0][12] = r0[0][12] + ";decomp_full/NoSuchType__NoSuchMethod.c"
        add("判据文件不存在", r0, "判据文件不存在")
        # 2 判据路径写错（多了目录 / 少了前缀）—— 从原值里挑一个 `cpp2il/…/X.cs` 塞一层假目录
        r1 = [row(i) for i in range(len(lines))]
        m = re.search(r"cpp2il/(out_cpp2il_dev_ilspy/)?([A-Za-z0-9_.]+\.cs)", r1[0][12])
        if m:
            r1[0][12] = r1[0][12].replace(m.group(2), "FakeNamespace/" + m.group(2), 1)
            add("判据路径不逐字存在（目录错）", r1, "判据路径")
        # 3 构成条数 +1
        r2 = [row(i) for i in range(len(lines))]
        try:
            r2[0][6] = str(int(r2[0][6]) + 1)
        except ValueError:
            r2[0][6] = "999"
        add("构成条数对不上", r2, "构成条数")
        # 4 状态分布写错
        r3 = [row(i) for i in range(len(lines))]
        r3[0][7] = "A1"
        add("状态分布对不上", r3, "状态分布")
        # 5 等级写错
        r4 = [row(i) for i in range(len(lines))]
        r4[0][8] = "A" if not r4[0][8].startswith("E") else "E"
        if parse_grade(r4[0][8])[0] == parse_grade(lines[0].split("\t")[8])[0]:
            r4[0][8] = "E"
        add("等级手写与重算不符", r4, "等级")
        # 6 落点指到不含该名字的行
        r5 = [row(i) for i in range(len(lines))]
        pm = re.match(r"^([^;:]+):(\d+)", r5[0][9])
        if pm:
            r5[0][9] = pm.group(1) + ":1" + r5[0][9][pm.end():]
            add("落点该行不含声明名", r5, "该行不含声明名")
        # 7 多认领（--strict 下必须判失败）
        if len(lines) >= 2:
            r6 = [row(i) for i in range(len(lines))]
            other = r6[1][5].split(";")[0]
            r6[0][5] = r6[0][5] + ";" + other
            add("多认领（--strict）", r6, "个功能点同时认领", strict=True)
        print("=" * 72)
        print("自检：基线 %s 失败 0 ✓ | 注入 %d 类" % (os.path.basename(dest), len(cases)))
        bad = 0
        for title, hit, expect, first in cases:
            print("  %s %s   [期望抓到「%s」]" % ("✓" if hit else "✗", title, expect))
            if not hit:
                bad += 1
                for m in first:
                    print("      未命中，实得失败：%s" % m[:120])
        print("自检结果：%d/%d 被抓到" % (len(cases) - bad, len(cases)))
        return 0 if bad == 0 else 1
    finally:
        shutil.rmtree(tmp, ignore_errors=True)


# ---------------------------------------------------------------- 入口
USE = """用法（不带动作参数 = 只打印这段，什么都不做）：
  python -I -X utf8 feature_index_check.py <索引.tsv> [--domain NAME] [--strict] [--quiet]
  python -I -X utf8 feature_index_check.py --manifest [--ledger DIR]
  python -I -X utf8 feature_index_check.py --resolve-judge <名字或路径> [...]
  python -I -X utf8 feature_index_check.py <索引.tsv> --self-test
其他选项：--ledger DIR · --src DIR · --no-judge-exists · -h/--help（-h 打印完整说明）"""


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2
    a = argv[1:]
    if a[0] in ("-h", "--help"):
        print(__doc__)
        return 2
    opts = dict(ledger=LEDGER, src=SRC, domain=None, strict=False, quiet=False,
                judge_exists=True, self_test=False)
    mode, pos = "check", []
    i = 0
    while i < len(a):
        x = a[i]
        if x == "--manifest":
            mode = "manifest"; i += 1
        elif x == "--resolve-judge":
            mode = "resolve"; i += 1
        elif x == "--self-test":
            opts["self_test"] = True; i += 1
        elif x == "--strict":
            opts["strict"] = True; i += 1
        elif x == "--quiet":
            opts["quiet"] = True; i += 1
        elif x == "--no-judge-exists":
            opts["judge_exists"] = False; i += 1
        elif x in ("--ledger", "--src", "--domain"):
            if i + 1 >= len(a):
                print("%s 缺少参数值\n%s" % (x, USE)); return 2
            opts[x[2:]] = a[i + 1]; i += 2
        elif x.startswith("-"):
            print("未知参数：%s\n%s" % (x, USE)); return 2
        else:
            pos.append(x); i += 1
    if mode == "manifest":
        rows = load_ledger(opts["ledger"])
        by = collections.defaultdict(list)
        for r in rows:
            by[r["域"]].append(r["src"])
        for dom in sorted(by, key=lambda d: -len(by[d])):
            items = by[dom]
            files = collections.Counter(x.split(":")[0] for x in items)
            print("%-22s %5d 行 | %s" % (dom, len(items),
                  " · ".join("%s(%d)" % (k, v) for k, v in files.most_common())))
        return 0
    if mode == "resolve":
        if not pos:
            print("--resolve-judge 需要至少一个名字/路径\n%s" % USE); return 2
        for t in pos:
            kind, path, hint = _JMAP.resolve(t)
            if kind == "exact":
                shown = path.replace("\\", "/")
            elif kind == "mapped":
                shown = path
            elif kind == "ambig":
                shown = " | ".join(path)
            else:
                shown = "（全盘找不到同名文件）"
            mark = {"exact": "✓", "mapped": "✗", "ambig": "?", "missing": "✗"}.get(kind, "?")
            print("%s %s\n     → %s%s" % (mark, t, shown, ("  " + hint) if hint else ""))
        return 0
    if not pos:
        print("缺少索引文件\n%s" % USE)
        return 2
    dest = pos[0]
    if not os.path.exists(dest):
        print("索引文件不存在：%s" % dest); return 2
    if opts["domain"] and opts["domain"] not in DOMAINS:
        print("未知域 `%s`；现读 %d 个域：%s"
              % (opts["domain"], len(DOMAINS), " ".join(sorted(DOMAINS))))
        return 2
    if opts["self_test"]:
        return self_test(dest, opts)
    return report(run_check(dest, opts), opts)


if __name__ == "__main__":
    sys.exit(main(sys.argv))
