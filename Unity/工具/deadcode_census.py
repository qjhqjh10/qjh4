#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""deadcode_census.py —— **只读**：把账里 状态 B/C 的行批量过一遍 `deadcode_xref` 的三源判据。

产物（**默认不写盘**）：每行一条
`账文件 / 账行号 / 类 / 方法 / 判定 / 证据 / 还差什么`

═══ 用法 ═══
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/deadcode_census.py                       # 默认 = **dry**：只打计数，不写文件
    $PY d:/4/Unity/工具/deadcode_census.py --out 输出.tsv        # 显式给路径才写（唯一的写盘动作）
    $PY d:/4/Unity/工具/deadcode_census.py --selftest            # 先跑 deadcode_xref 的两个样本自证
    $PY d:/4/Unity/工具/deadcode_census.py --status C,B --limit 200

🔴 **纪律（本工程的教训）**：写盘类脚本的**默认行为必须是「什么都不做」**，真动作要**显式**参数
   （`merge_ledger_parts.py --help` 那次事故：不解析 `--help` + 不带参就真合并 ⇒ 冲掉 232 行手工修订）。

═══ 判定三档（阈值口径，⛔ 不许凑） ═══
  ① **死代码可判D** = `.text`+`il2cpp` 全扫 0 命中 ∧ `lea` 0 ∧ `decomp_full`+ILSpy 0 调用者 ∧ **账里明确写过「池内 0」**
  ② **活的要做**   = 有 `E8` 调用点 / 有 `lea`（取地址⇒委托·方法组）/ 命中共享桩
  ③ **内联可疑**   = 其余（含三条「调用者本来不是 E8」的族：参数含 `BattleAction` 的动作队列 switch ·
     参数含 `*Signal` 的委托订阅 · UI 回调（prefab `m_MethodName`））⇒ ⛔ 一律不许判 D
  **查不到 RVA** = `dump.cs` 的 `// RVA:` 为负（泛型实例/被剥体）或两个索引都没命中 ⇒ 不是死代码证据。

⚠️ `账行号` = `工具/ledger_slice.py` 的 `物理行` 口径 = `split('
')` 的 **0 基**下标
   （与编辑器/文本里的 1-based 行号差 **1**）。

⚠️ 残余风险同 `deadcode_xref.py`：内联看不见 ⇒ 「候选 D」不是「已判死」。
"""
import argparse
import importlib.util
import io
import os
import re
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

LEDGER_DIR = "d:/4/Unity/资料/完全复刻/账/"
TOOL = "d:/4/Unity/工具/deadcode_xref.py"
CARDS = ["d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json",
         "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_truth.json"]


def load_xref():
    spec = importlib.util.spec_from_file_location("dcx", TOOL)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


POOL0 = [
    re.compile(r'池内\s*(?:=|是|为)?\s*0\s*(?:张|处|个|条|实例)'),
    re.compile(r'池内[^。｜;]{0,20}?(?<![0-9])0\s*(?:张|处|个|条|实例)'),
    re.compile(r'共\s*0\s*(?:张|处|个|条)'),
    re.compile(r'命中\s*0(?![0-9])'),
    re.compile(r'今天不可观测'),
    re.compile(r'休眠机制'),
]


def pool0_evidence(row):
    txt = " ｜ ".join([row[4], row[7], row[10]])          # 判据要点 / 差异 / 备注
    for p in POOL0:
        m = p.search(txt)
        if m:
            return m.group(0).strip()[:60]
    return ""



def collect(dcx, statuses):
    cards_txt = ""
    for p in CARDS:
        if os.path.exists(p):
            cards_txt += io.open(p, encoding="utf-8", errors="replace").read()
    rows = []
    for f in sorted(os.listdir(LEDGER_DIR)):
        if not f.endswith(".tsv"):
            continue
        raw = io.open(LEDGER_DIR + f, "rb").read().decode("utf-8")
        for i, line in enumerate(raw.split("\n")):
            if not line or line.startswith("域	"):
                continue
            c = line.split("	")
            if len(c) != 11 or c[5] not in statuses:
                continue
            rows.append((f, i, c))
    return rows, cards_txt


def main():
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument("--out", default="", help="写盘路径（**不给就什么都不写** = dry 模式）")
    ap.add_argument("--status", default="B,C", help="要扫的账状态（默认 B,C）")
    ap.add_argument("--limit", type=int, default=0, help="只处理前 N 行（调试用，0 = 全量）")
    ap.add_argument("--selftest", action="store_true", help="先跑 deadcode_xref 的样本自证")
    a = ap.parse_args()
    if a.selftest:
        rc = os.system('"%s" -I -X utf8 "%s" --selftest' % (sys.executable, TOOL))
        if rc != 0:
            print("⛔ deadcode_xref --selftest 不 PASS ⇒ 停手（工具不可信，别拿它的结论）")
            return 1
    dcx = load_xref()
    statuses = tuple(s.strip() for s in a.status.split(",") if s.strip())
    rows, cards_txt = collect(dcx, statuses)
    print("# 要扫的账行：%d（状态 %s）" % (len(rows), "/".join(statuses)))
    if a.limit:
        rows = rows[:a.limit]
    script_idx = dcx.load_script_index()
    dump_idx = dcx.load_dump_rvas()
    calls = dcx.scan_text_calls()
    print("# 索引：script.json %d · dump.cs %d · 代码节目标 %d" % (len(script_idx), len(dump_idx), len(calls)))
    cache, out = {}, []
    for f, i, c in rows:
        cls, meth_sig = c[1].strip(), c[2].strip()
        meth = meth_sig.split("(")[0].strip()
        key = (cls, meth)
        if key not in cache:
            cache[key] = dcx.judge(cls, meth, script_idx, dump_idx, calls, all_variants=False)
        r = cache[key][0]
        v = r["verdict"]
        ev = ["E8=%s" % r["e8"], "lea=%s" % r.get("lea", "-"), "dec=%s" % r["dec"], "il=%s" % r["il"],
              "VA=%s" % r["va"]]
        if r["stub"] != "-":
            ev.append("STUB " + r["stub"])
        if r["why"]:
            ev.append(r["why"][:240])
        p0 = pool0_evidence(c)
        name_in_cards = bool(meth) and (meth in cards_txt)
        if name_in_cards:
            ev.append("卡数据串命中方法名")
        fam = ""
        if "BattleAction" in meth_sig:
            fam = "动作队列分派（`ResolveAction` 按动作类型 switch）"
        elif re.search(r"\w+Signal\s+\w+", meth_sig):
            fam = "Signal 委托订阅（`Signal.Raise`）"
        elif cls.endswith(("Button", "Toggle", "Slider", "Dropdown", "InputField", "Panel", "Window", "Tab"))                 or meth_sig.startswith(("OnButtonClick", "OnClick", "OnValueChanged", "OnEndEdit", "OnSubmit")):
            fam = "UnityEvent/prefab 字符串调用（`m_MethodName`）"
        if fam and v in ("候选 D", "内联可疑"):
            v, gap = "内联可疑", "调用者是「%s」⇒ 0 命中不构成死代码证据" % fam
        elif v == "候选 D":
            if p0 and not name_in_cards:
                v = "死代码可判D"
                gap = "无（四源 0 + 账述 %s）" % p0
            else:
                v = "内联可疑"
                gap = "缺「池内 0」的独立证据（账里没写）⇒ 不许判 D"
        elif v == "活的要做":
            gap = "原版有调用者/取地址引用 ⇒ 按铁律 11 要做"
        elif v == "内联可疑":
            gap = "二进制 0 直接调用点但反编译树有引用（或被内联/虚调用）⇒ 别判 D"
        elif v.startswith("桩"):
            v, gap = "活的要做", "共享桩 ⇒ 按地址归因无效"
        elif v == "查不到 RVA":
            gap = "查不到 RVA（泛型实例/被剥体）⇒ 先按泛型/转义重查，不是死代码证据"
        else:
            gap = ""
        out.append((f, str(i), cls, meth_sig, v, " ｜ ".join(ev), gap))

    from collections import Counter
    cnt = Counter(x[4] for x in out)
    print("# 分档：")
    for k, n in cnt.most_common():
        print("#   %-14s %d" % (k, n))
    if not a.out:
        print("# （dry 模式：**一个字节都没写**；要产物请显式给 `--out <路径>`）")
        return 0
    hdr = "\t".join(["账文件", "账行号", "类", "方法", "判定", "证据", "还差什么"])
    cells = []
    for x in out:
        row = []
        for s in x:
            row.append(s.replace("\t", " ").replace("\n", " ").replace("\r", " "))
        cells.append("\t".join(row))
    io.open(a.out, "wb").write((hdr + "\n" + "\n".join(cells) + "\n").encode("utf-8"))
    print("# 写出：%s（%d 行）" % (a.out, len(out)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
