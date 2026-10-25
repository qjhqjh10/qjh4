#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成「完全审查 + 完全复刻」的代码覆盖账骨架。

用途：从 ILSpy 反编译产物（D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs）
解析出**每个顶层类的每个方法**，按域写成 TSV（一行一方法），状态一律先填 `E 未读`，
`判据出处` 预填 `cpp2il/<类>.cs:<行号>`（省掉代理自己找行的功夫）。

⚠️ **重跑会重建骨架**，但已加了**合并保护**：老文件里凡「状态 != E」或「判据要点 / 我们的落点」非空的行，
按 (类, 方法) 原样保留（见 main() 里的 keep 逻辑）。**已经填好的账不会被清回 E。**
⚠️ 但**并发仍然危险**：若某个代理正把整份 TSV 读进内存再写回，它写回时会盖掉这次重建新增的行。
⇒ **纪律：有代理正在填账时，⛔ 不要跑本脚本**（只有调度台在批次之间跑）。

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_replica_ledger.py [--dry]

产物：
    d:/4/Unity/资料/完全复刻/账/<域>.tsv      （11 列，见 COLS）
    d:/4/Unity/资料/完全复刻/账/_未分类.tsv
    d:/4/Unity/资料/完全复刻/账户索引.md
"""
import io
import json
import os
import re
import sys

SRC = r"D:/2/tools/cpp2il/out_cpp2il_dev_ilspy"
ROWS = r"D:/4/_tmp_view/wfcover/rows_final.json"
OUTDIR = r"D:/4/Unity/资料/完全复刻/账"
INDEX = r"D:/4/Unity/资料/完全复刻/账户索引.md"

COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

DOMAINS = [
    "收藏", "战斗界面/HUD", "战斗逻辑驱动", "卡牌面/渲染", "特效动画",
    "菜单/外壳", "卡组编辑", "商店", "锻造", "战役", "日常/奖励", "轮抽",
    "社交/聊天", "个人资料/成就/排行", "本地化", "音频/语音", "联机",
    "教程/引导", "其他/基础设施", "未分类/其他",
]

# 方法声明：访问修饰符 + 修饰符 + 返回类型 + 名字(参数)
RE_METHOD = re.compile(
    r"^\s*(?:public|private|protected|internal)\s+"
    r"(?:(?:static|virtual|override|abstract|sealed|async|extern|unsafe|new|partial)\s+)*"
    r"(?P<ret>[\w\.<>\[\],\?\s]+?)\s+"
    r"(?P<name>[\w<>\.]+)\s*\((?P<params>[^)]*)\)"
)
RE_TYPE = re.compile(r"\b(class|struct|interface|enum)\s+(?P<name>[\w<>\.`]+)")


class Scanner:
    """逐字符扫描，跟踪花括号深度 / 类栈 / 字符串与注释（只为了算深度，不建 AST）。"""

    def __init__(self):
        self.depth = 0
        self.classes = []       # [(name, body_depth)]
        self.in_block_comment = False
        self.in_str = False
        self.in_verbatim = False
        self.in_char = False
        self.esc = False
        self.pending_type = None

    def feed(self, line, lineno, sink):
        # 类型声明要在扫描花括号**之前**认出：`class Foo {` 这种写法 `{` 在同一行
        if not self.in_block_comment and not self.in_str:
            m = RE_TYPE.search(line)
            if m:
                self.pending_type = m.group("name")
        i, n = 0, len(line)
        while i < n:
            c = line[i]
            if self.in_block_comment:
                if c == "*" and i + 1 < n and line[i + 1] == "/":
                    self.in_block_comment = False
                    i += 2
                    continue
                i += 1
                continue
            if self.in_str:
                if self.in_verbatim:
                    if c == '"':
                        if i + 1 < n and line[i + 1] == '"':
                            i += 2
                            continue
                        self.in_str = self.in_verbatim = False
                    i += 1
                    continue
                if self.esc:
                    self.esc = False
                elif c == "\\":
                    self.esc = True
                elif c == '"':
                    self.in_str = False
                i += 1
                continue
            if self.in_char:
                if self.esc:
                    self.esc = False
                elif c == "\\":
                    self.esc = True
                elif c == "'":
                    self.in_char = False
                i += 1
                continue
            # 正常代码
            if c == "/" and i + 1 < n:
                if line[i + 1] == "/":
                    break                       # 行注释，本行结束
                if line[i + 1] == "*":
                    self.in_block_comment = True
                    i += 2
                    continue
            if c == "@" and i + 1 < n and line[i + 1] == '"':
                self.in_str = self.in_verbatim = True
                i += 2
                continue
            if c == '"':
                self.in_str = True
                self.esc = False
                i += 1
                continue
            if c == "'":
                self.in_char = True
                self.esc = False
                i += 1
                continue
            if c == "{":
                self.depth += 1
                if self.pending_type is not None:
                    # 泛型类名截到 `<` 之前：`DraftEventBase<TS, TD>` → `DraftEventBase`
                    self.classes.append((self.pending_type.split("<")[0], self.depth))
                    self.pending_type = None
                i += 1
                continue
            if c == "}":
                self.depth = max(0, self.depth - 1)
                while self.classes and self.classes[-1][1] > self.depth:
                    self.classes.pop()
                i += 1
                continue
            i += 1
        # pending_type 由行首识别、由 `{` 消费；本行没出现 `{` 时它会留到下一行

    def cur_class(self):
        return self.classes[-1][0] if self.classes else None

    def cur_body_depth(self):
        return self.classes[-1][1] if self.classes else None


def parse_file(path):
    """返回 [(方法显示名, 行号)]，只取直接属于（非编译器生成的）类的成员方法。"""
    out = []
    sc = Scanner()
    with io.open(path, "r", encoding="utf-8", errors="replace") as f:
        for lineno, line in enumerate(f, 1):
            body_depth = sc.cur_body_depth()
            cls = sc.cur_class()
            if cls is not None and body_depth == sc.depth and not cls.startswith(("_003C", "<")):
                m = RE_METHOD.match(line)
                if m:
                    name = m.group("name")
                    decl = line[:m.start()]
                    # 排除表达式体属性 / 字段初始化 / 运算符 / 构造函数
                    if ("=>" not in line[:m.end()] and " = " not in decl
                            and name != cls and name not in ("if", "for", "foreach", "while", "switch", "using", "lock", "catch", "return")):
                        params = m.group("params").strip()
                        # 参数只留类型，去掉默认值，压缩空格
                        sig = []
                        if params:
                            for p in params.split(","):
                                p = p.split("=")[0].strip()
                                sig.append(re.sub(r"\s+", " ", p))
                        out.append((name + "(" + ", ".join(sig) + ")", lineno))
            sc.feed(line, lineno, None)
    return out


def main():
    dry = "--dry" in sys.argv

    rows = json.load(io.open(ROWS, "r", encoding="utf-8"))
    dom_of = {}
    third_of = set()
    for r in rows:
        nm = (r.get("full") or r.get("name") or "").split("<")[0].split(".")[-1]
        if not nm:
            continue
        if r.get("third"):
            third_of.add(nm)
            continue
        dom_of.setdefault(nm, r.get("dom") or "未分类/其他")

    files = []
    for root, _dirs, fns in os.walk(SRC):
        for fn in fns:
            if fn.endswith(".cs"):
                rel = os.path.relpath(os.path.join(root, fn), SRC).replace("\\", "/")
                files.append(rel)
    files.sort()
    buckets = {d: [] for d in DOMAINS}
    n_methods = 0
    n_files = 0
    n_third = 0
    unmatched = []

    for rel in files:
        fn = rel.rsplit("/", 1)[-1]
        cls = fn[:-3]
        if cls.startswith(("_003C", "<", "--f__")) or cls in ("AssemblyInfo", "<Module>"):
            continue
        dom = dom_of.get(cls)
        if dom is None:
            if cls in third_of:
                n_third += 1
                continue          # 第三方 / 后端 SDK：不是复刻对象，不入账
            dom = "未分类/其他"
            unmatched.append(cls)
        if dom not in buckets:
            dom = "未分类/其他"
        ms = parse_file(os.path.join(SRC, rel))
        n_files += 1
        for mname, lineno in ms:
            buckets[dom].append((cls, mname, "%s:%d" % (rel, lineno)))
            n_methods += 1

    print("解析文件 %d 个 / 方法 %d 条 / 跳过第三方类 %d 个 / 未匹配域的类 %d 个" % (n_files, n_methods, n_third, len(unmatched)))
    for d in DOMAINS:
        print("  %-14s 方法 %5d" % (d, len(buckets[d])))
    if unmatched[:10]:
        print("  未匹配示例:", ", ".join(unmatched[:10]))

    if dry:
        return 0

    os.makedirs(OUTDIR, exist_ok=True)
    for d in DOMAINS:
        safe = d.replace("/", "_")
        p = os.path.join(OUTDIR, safe + ".tsv")
        # ⚠️ 重跑会重建骨架，但**不许抹掉代理已经填的内容**：
        #    老文件里凡「状态 != E」或「判据要点 / 我们的落点」非空的行，按 (类, 方法) 原样保留。
        keep = {}
        if os.path.exists(p):
            with io.open(p, "r", encoding="utf-8") as f:
                for line in f:
                    cells = line.rstrip("\n").split("\t")
                    if len(cells) < len(COLS) or cells[0] == COLS[0]:
                        continue
                    if cells[5] != "E" or cells[4] or cells[6]:
                        keep[(cells[1], cells[2])] = cells
        with io.open(p, "w", encoding="utf-8", newline="\n") as f:
            f.write("\t".join(COLS) + "\n")
            for cls, mname, src in sorted(buckets[d]):
                old = keep.get((cls, mname))
                if old:
                    f.write("\t".join(old) + "\n")
                else:
                    f.write("\t".join([d, cls, mname, src, "", "E", "", "", "", "", ""]) + "\n")
        if keep:
            print("  保留已填行 %d 条 → %s" % (len(keep), safe + ".tsv"))

    # 索引
    lines = ["# 代码覆盖账 · 账户索引", "",
             "> 生成：`工具/gen_replica_ledger.py`（可重跑）· 状态五档见 `资料/完全审查与完全复刻_方案.md` §四",
             "> 账文件：`资料/完全复刻/账/<域>.tsv`（11 列，一行一方法）。**骨架别改列结构，只填内容。**", "",
             "| 域 | 方法数 | 账文件 | C（该做没做） | A（已对齐） | E（未读） |",
             "|---|---:|---|---:|---:|---:|"]
    for d in DOMAINS:
        safe = d.replace("/", "_")
        lines.append("| %s | %d | `账/%s.tsv` | — | — | %d |" % (d, len(buckets[d]), safe, len(buckets[d])))
    lines += ["", "**合计**：%d 条方法 / %d 个类文件。" % (n_methods, n_files), ""]
    with io.open(INDEX, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))
    print("已写", OUTDIR, "与", INDEX)
    return 0


if __name__ == "__main__":
    sys.exit(main())
