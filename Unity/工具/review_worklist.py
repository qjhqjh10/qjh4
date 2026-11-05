#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""review_worklist.py —— 系统性审查的【工单生成器】（纯只读输入，显式动作才写盘）。

用途：把「原版有哪些方法 / 哪些从没进过账 / 哪些账绿但可疑」一次性算出来，
      让 18 片审查按**机器出的工单**跑，而不是靠代理自己找。

输入（全部只读）：
  · D:/2/tools/il2cpp_out/dump.cs                    原版全部类 × 方法（签名 + RVA）
  · D:/4/_tmp_view/wfcover/rows_final.json           类 → 域 / 是否第三方（2,770 类）
  · D:/4/Unity/资料/完全复刻/账/*.tsv                 11 列账本（状态 / 我们的落点 / 差异）
  · D:/2/tools/decomp_full/*.c                       25,096 份伪码，用来数**调用点**

输出（只在 --write 时写）：
  · D:/4/Unity/资料/完全复刻/判据/B149_审查工单.tsv
  · D:/4/Unity/资料/完全复刻/判据/B149_审查工单_汇总.md

用法（⛔ 裸跑 = 打印用法 + rc=2，什么都不做）：
  python -I -X utf8 d:/4/Unity/工具/review_worklist.py --dry      # 只打印汇总
  python -I -X utf8 d:/4/Unity/工具/review_worklist.py --check    # 与盘上产物逐字节比
  python -I -X utf8 d:/4/Unity/工具/review_worklist.py --write    # 真写

  --domains a,b,c   只算这些域（默认 = B149 的 7 域 + 其 4 个账域原名）
  --no-calls        跳过 .c 扫描（快，但没有「调用文件数」列）

退出码：0 = 成功 / --check 一致；1 = --check 不一致；2 = 用法错。
"""
import io
import json
import os
import re
import sys
import glob
import collections

DUMP = r"D:/2/tools/il2cpp_out/dump.cs"
INDEX = r"D:/4/_tmp_view/wfcover/rows_final.json"
LEDGER_DIR = r"D:/4/Unity/资料/完全复刻/账"
DECOMP_DIR = r"D:/2/tools/decomp_full"
OUT_TSV = r"D:/4/Unity/资料/完全复刻/判据/B149_审查工单.tsv"
OUT_MD = r"D:/4/Unity/资料/完全复刻/判据/B149_审查工单_汇总.md"

# B149 计划 §二：本轮审的 7 个「组」，展开成账本里的域原名
B149_DOMAINS = [
    "战斗逻辑驱动", "战斗界面/HUD", "特效动画", "卡牌面/渲染",
    "卡组编辑",
    "其他/基础设施", "泛型与启动链", "未分类/其他", "本地化",
    "音频/语音",
]

RE_NS = re.compile(r"^// Namespace: (.*)$")
RE_TYPE = re.compile(
    r"^(?:\[[^\]]*\]\s*)*"
    r"(?:(?:public|private|protected|internal|sealed|abstract|static|partial|readonly|unsafe|new)\s+)*"
    r"(?:class|struct|interface|enum)\s+([\w<>\.`]+)"
)
RE_RVA = re.compile(r"^\t*// RVA: (-?\d+|0x[0-9A-Fa-f]+) Offset")
RE_METHOD = re.compile(
    r"^\t+(?:\[[^\]]*\]\s*)*"
    r"(?:public|private|protected|internal)\s+"
    r".*?\b([\w<>\.]+)\s*\(([^()]*)\)\s*\{\s*\}\s*$"
)
RE_SYM = re.compile(r"([A-Za-z_][A-Za-z0-9_]*?)__([A-Za-z_][A-Za-z0-9_]*)")


def usage():
    print(__doc__)
    return 2


# ---------------------------------------------------------------- dump.cs
def parse_dump(path):
    """返回 {类名: [(方法名, 参数串, RVA, 是否属性访问器)]}，只取**顶层类**。"""
    out = collections.defaultdict(list)
    cur = None
    ns = ""
    last_rva = ""
    in_methods = False
    skipped_nested = 0
    with io.open(path, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            if line.startswith("// Namespace:"):
                m = RE_NS.match(line.rstrip("\n"))
                ns = m.group(1).strip() if m else ""
                cur = None
                in_methods = False
                continue
            if line.startswith("\t// "):
                tag = line.strip()
                if tag.startswith("// Methods"):
                    in_methods = True
                elif tag.startswith("// Fields") or tag.startswith("// Properties"):
                    in_methods = False
                continue
            if not line.startswith("\t"):
                if not line.strip():
                    continue
                # 顶层类型声明？
                if "// TypeDefIndex:" in line:
                    m = RE_TYPE.match(line)
                    if m:
                        cur = m.group(1)
                        in_methods = False
                        last_rva = ""
                        continue
                # 顶层非类型行（例如文件开头的 Image 列表）—— 不影响
                continue
            if not in_methods:
                continue
            s = line.strip()
            if s.startswith("// RVA:"):
                m = RE_RVA.match(line)
                last_rva = m.group(1) if m else ""
                continue
            m = RE_METHOD.match(line.rstrip("\n"))
            if m and cur:
                name = m.group(1)
                params = m.group(2).strip()
                is_acc = name.startswith("get_") or name.startswith("set_") or name.startswith("add_") or name.startswith("remove_")
                out[(ns, cur)].append((name, params, last_rva, is_acc))
                last_rva = ""
    return out


# ---------------------------------------------------------------- ledger
def method_name_of(cell):
    cell = cell.strip()
    if "(" in cell:
        cell = cell.split("(", 1)[0]
    return cell.strip().strip("`")


def load_ledger(d):
    rows = {}
    dup = 0
    for p in sorted(glob.glob(os.path.join(d, "*.tsv"))):
        dom_file = os.path.basename(p)[:-4]
        with io.open(p, "r", encoding="utf-8", errors="replace") as f:
            lines = f.read().replace("\r\n", "\n").split("\n")
        for i, l in enumerate(lines):
            if not l.strip() or i == 0:
                continue
            c = l.split("\t")
            if len(c) < 6:
                continue
            dom = c[0].strip() or dom_file
            cls = c[1].strip()
            meth = method_name_of(c[2])
            status = c[5].strip()[:1] or "?"
            landing = c[6].strip() if len(c) > 6 else ""
            diff = c[7].strip() if len(c) > 7 else ""
            key = (cls, meth)
            rows[key] = dict(dom=dom, status=status, landing=landing, diff=diff, file=dom_file, line=i + 1)
            dup += 0
    return rows


# ---------------------------------------------------------------- call sites
def scan_calls(decomp_dir, symbols):
    """数「每个符号被多少个 .c 文件引用（排除定义自己的那份）」。"""
    files = glob.glob(os.path.join(decomp_dir, "*.c"))
    counts = collections.Counter()
    for p in files:
        base = os.path.basename(p)
        stem = base[:-2] if base.endswith(".c") else base
        try:
            with io.open(p, "r", encoding="utf-8", errors="replace") as f:
                txt = f.read()
        except OSError:
            continue
        seen = set()
        for m in RE_SYM.finditer(txt):
            sym = m.group(1) + "__" + m.group(2)
            if sym in symbols and sym != stem:
                seen.add(sym)
        for s in seen:
            counts[s] += 1
    return counts, len(files)


# ---------------------------------------------------------------- main
def run(domains, do_calls):
    index = json.load(io.open(INDEX, "rb"))
    cls2dom = {}
    cls2third = {}
    for r in index:
        nm = r.get("name") or ""
        cls2dom[nm] = r.get("dom") or "未分类/其他"
        cls2third[nm] = bool(r.get("third"))
    ledger = load_ledger(LEDGER_DIR)
    dump = parse_dump(DUMP)

    # 符号集合（用于调用点扫描）：只算在范围内的类
    symbols = set()
    scope = {}
    for (ns, cls), meths in dump.items():
        # 🔴 只收「游戏自身的顶层类」：必须出现在 rows_final 索引里，且不是第三方
        #    （dump.cs 覆盖 129 个 image，mscorlib/System/Unity 的类不能算进来）
        if cls not in cls2dom:
            continue
        if cls2third.get(cls):
            continue
        dom = cls2dom.get(cls, "未分类/其他")
        if domains and dom not in domains:
            continue
        scope[cls] = dom
        for (name, params, rva, is_acc) in meths:
            symbols.add(cls + "__" + name)

    calls, nfiles = ({}, 0)
    if do_calls:
        calls, nfiles = scan_calls(DECOMP_DIR, symbols)

    rows = []
    seen = set()
    for (ns, cls), meths in dump.items():
        if cls not in scope:
            continue
        dom = scope[cls]
        for (name, params, rva, is_acc) in meths:
            if (cls, name) in seen:
                continue
            seen.add((cls, name))
            key = (cls, name)
            led = ledger.get(key)
            sym = cls + "__" + name
            ncall = calls.get(sym, 0) if do_calls else -1
            nparams = 0 if not params else params.count(",") + 1
            # 标记
            flags = []
            if led is None:
                flags.append("从未进账")
            else:
                st = led["status"]
                land = led["landing"]
                if st in ("B", "C", "E"):
                    flags.append("未对齐(%s)" % st)
                if st == "A" and (not land or not re.search(r":\d+", land)):
                    flags.append("A但落点非行号")
                if st == "A" and do_calls and ncall >= 5:
                    flags.append("A但调用点多(%d)" % ncall)
            if is_acc:
                flags.append("访问器")
            if name in (".ctor", ".cctor"):
                flags.append("构造器")
            if rva in ("-1", ""):
                flags.append("无RVA")
            if do_calls and ncall == 0 and not is_acc and name not in (".ctor", ".cctor"):
                flags.append("无外部调用")
            rows.append(dict(
                dom=dom, cls=cls, ns=ns, meth=name, nparams=nparams, rva=rva,
                kind=("访问器" if is_acc else ("构造器" if name in (".ctor", ".cctor") else "方法")),
                in_ledger="Y" if led else "N",
                status=led["status"] if led else "",
                landing=led["landing"] if led else "",
                ncall=ncall,
                flags=";".join(flags),
            ))
    return rows, nfiles, len(scope)


def md_summary(rows, nfiles, ncls, domains):
    by_dom = collections.defaultdict(lambda: collections.Counter())
    for r in rows:
        c = by_dom[r["dom"]]
        real = (r["kind"] == "方法")           # 只把真方法算进「缺」的口径
        c["all"] += 1
        c["real"] += (1 if real else 0)
        c["never"] += (1 if (r["in_ledger"] == "N" and real) else 0)
        c["A"] += (1 if r["status"] == "A" else 0)
        c["BCE"] += (1 if r["status"] in ("B", "C", "E") else 0)
        c["callable"] += (1 if r["ncall"] > 0 else 0)
        c["never_callable"] += (1 if (r["in_ledger"] == "N" and real and r["ncall"] > 0) else 0)
        c["A_noline"] += (1 if "A但落点非行号" in r["flags"] else 0)
        c["A_hot"] += (1 if "A但调用点多" in r["flags"] else 0)
    out = []
    out.append("# B149 审查工单 · 汇总（工具产物，⛔ 别手改，重跑会覆盖）\n")
    out.append("> 生成：`工具/review_worklist.py` · 扫描 `.c` %d 份 · 在范围顶层类 %d 个 · 成员 %d 条（其中真方法 %d）\n"
               % (nfiles, ncls, len(rows), sum(1 for r in rows if r["kind"] == "方法")))
    out.append("> 范围域：%s\n" % ("、".join(domains) if domains else "全部"))
    out.append("> 口径：**「从未进账」只数真方法**（属性访问器 / `.ctor` 不算缺，它们由属性/类型覆盖）。\n")
    out.append("")
    out.append("| 域 | 真方法 | 从未进账 | 未对齐(B/C/E) | 有外部调用 | **从未进账且有调用** | A但落点非行号 | A但调用点多 |")
    out.append("|---|---:|---:|---:|---:|---:|---:|---:|")
    tot = collections.Counter()
    for d in sorted(by_dom, key=lambda x: -by_dom[x]["all"]):
        c = by_dom[d]
        for k in c:
            tot[k] += c[k]
        out.append("| %s | %d | %d | %d | %d | **%d** | %d | %d |" % (
            d, c["real"], c["never"], c["BCE"], c["callable"], c["never_callable"], c["A_noline"], c["A_hot"]))
    out.append("| **合计** | **%d** | **%d** | **%d** | **%d** | **%d** | **%d** | **%d** |" % (
        tot["real"], tot["never"], tot["BCE"], tot["callable"], tot["never_callable"], tot["A_noline"], tot["A_hot"]))
    out.append("")
    out.append("**怎么用**：`从未进账且有调用` = 最该先审的（原版真会跑、账里却没有）；"
               "`A但落点非行号` = 账绿但落点是类级/间接指认（最该重判的）；`A但调用点多` 只是提示，不是缺陷。")
    return "\n".join(out) + "\n"


def main(argv):
    if not argv:
        return usage()
    act = None
    domains = list(B149_DOMAINS)
    do_calls = True
    i = 0
    while i < len(argv):
        a = argv[i]
        if a in ("--dry", "--check", "--write"):
            act = a
        elif a == "--domains":
            i += 1
            domains = [x.strip() for x in argv[i].split(",") if x.strip()]
        elif a == "--no-calls":
            do_calls = False
        elif a in ("-h", "--help"):
            return usage()
        else:
            print("未知参数：%s" % a)
            return usage()
        i += 1
    if act is None:
        return usage()

    rows, nfiles, ncls = run(domains, do_calls)
    tsv = ["域\t原版类\t方法\t参数数\tRVA\t账里有行\t账状态\t我们的落点\t调用文件数\t标记"]
    for r in sorted(rows, key=lambda x: (x["dom"], x["cls"], x["meth"])):
        tsv.append("\t".join([
            r["dom"], r["cls"], r["meth"], r["kind"], str(r["nparams"]), r["rva"],
            r["in_ledger"], r["status"], r["landing"].replace("\t", " "),
            str(r["ncall"]), r["flags"],
        ]))
    tsv_txt = "\n".join(tsv) + "\n"
    md_txt = md_summary(rows, nfiles, ncls, domains)

    if act == "--dry":
        print(md_txt)
        return 0
    if act == "--check":
        ok = True
        for p, txt in ((OUT_TSV, tsv_txt), (OUT_MD, md_txt)):
            if not os.path.exists(p):
                print("MISSING %s" % p); ok = False; continue
            cur = io.open(p, "rb").read().decode("utf-8", "replace")
            if cur != txt:
                print("DIFF %s" % p); ok = False
            else:
                print("OK %s" % p)
        return 0 if ok else 1
    # --write
    for p, txt in ((OUT_TSV, tsv_txt), (OUT_MD, md_txt)):
        d = os.path.dirname(p)
        if d and not os.path.isdir(d):
            os.makedirs(d)
        io.open(p, "wb").write(txt.encode("utf-8"))
        print("WROTE %s (%d bytes)" % (p, len(txt.encode("utf-8"))))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
