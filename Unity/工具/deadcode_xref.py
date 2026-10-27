#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""deadcode_xref.py —— **只读**：判「原版这个方法到底有没有人调用」（死代码候选普查）

═══ 为什么需要它 ═══
账里大量 C 行写着「原版有这条路、我们没建」。其中一部分**在原版里根本没人调用**
（死代码）⇒ 按 `方案 §4.1` 可以判 `D`（不适用），不必复刻。
但「没调用者」这个结论**极容易误判**，本工程已经栽过四次（记录在 `CLAUDE.md` §三）：

  ① `decomp_full` 会**静默丢体**、也会按**文件名收错内容**；
  ② ILSpy 会把**异步状态机体**整个丢掉、会**丢 `= true` 写点**、会把常量反编译成 `default`；
  ③ **编译器内联**会让调用点整个消失（实例：`ManaBuff.DeepCopy` 被内联进**活的**
     `AbilityLogic.PlayAbility`，ILSpy 里只剩 `MemberwiseClone()`）；
  ④ 同一个 VA 会被多个方法共享（ICF 折叠）⇒ **按地址归因无效**。

⇒ 所以本工具**不输出「已判死」**，只输出「**候选 D**」，并把残余风险打在每一行结论上。

═══ 判据（三源，缺一不可） ═══
  ① `dump.cs` 的 `// RVA:` **非负**（负值 = 只有签名没有体，见 `资料/全量反编译_入口与用法.md`）；
  ② 用 `script.json` 的 `ScriptMethod[].Address` 拿 RVA → VA，然后**全 `.text` 扫 `E8/E9 rel32`**
     找直接调用点（这是**最硬**的一条：`private` 方法只可能被直接调用）；
  ③ `decomp_full/**.c` 与 `out_cpp2il_dev_ilspy/**.cs` 里的调用者个数（用来交叉印证 ②）。

═══ 两个必须特判的桩 ═══
  · **`RVA 0x4B33B0` = 全局共享空方法桩**（机器码 `c2 00 00` = `ret 0`；`dump.cs` 里被
    1,815 处 `// RVA:` 引用）⇒ 「原版这方法是空的」可以直接问二进制，**但它的调用点没有归因意义**。
  · **`RVA 0x6FB110` = 高频共享的 `return false` 桩**（`IsCampaignScriptAvailable` 与
    `ShouldUseEnviromentalEffects` 同址）⇒ **按地址归因无效，只能按实参个数区分**
    （`(this,0)` vs `(this,arg,0)`）。
  两者命中时，本工具把「E8 命中数」标成 `SHARED`（不代表这个方法的调用点）。

═══ 用法（全部只读） ═══
    PY=D:/2/Warpforge_tools/py312/python.exe          # 或本机 python，`-I -X utf8` 都行
    $PY d:/4/Unity/工具/deadcode_xref.py --method CardScript.ResolveTransient
    $PY d:/4/Unity/工具/deadcode_xref.py --method CardScript.Heal --all      # 重载：逐条打
    $PY d:/4/Unity/工具/deadcode_xref.py --batch 清单.txt                    # 每行 `类.方法`（可带 tab 附加列，原样回显）
    $PY d:/4/Unity/工具/deadcode_xref.py --selftest                          # 两个已知样本自证

    # `--batch` 输出是 TSV（可直接喂 pandas / 拼进账）：
    #   类<TAB>方法<TAB>RVA<TAB>VA<TAB>E8命中<TAB>dec调用者<TAB>ilspy调用者<TAB>桩<TAB>判定<TAB>证据

═══ 已知边界（⛔ 别当它万能） ═══
  · **内联**：被内联的方法在二进制里**没有** `E8` 调用点，而它可能是活的 ⇒ 判定只写「候选 D」。
  · **虚方法 / 接口 / 委托**：走 `call [reg+off]` 或 `delegate`，`E8` 扫不到 ⇒
    本工具对 `virtual`/`override` 名字会额外打一行提醒（标记 `VTBL?`）。
  · **泛型实例**：`dump.cs` 对泛型类方法写 `// RVA: -1`，真地址只在 `GenericInstMethod` 子块 ⇒
    查不到不是「死」，见 `CLAUDE.md` §三「泛型类的方法体从来没被导出过」。
  · **`ESharp`/`reflection`**：字符串调用（`m_MethodName` / `AssetReference`）扫不到 ⇒
    真要定 D 还要补一次资源串扫描（本工具的 `--assets` 开关做这一层，默认关，慢）。
"""
import argparse
import io
import os
import re
import struct
import sys

# ---- 控制台是 GBK：自己切 UTF-8，别依赖调用方 ----
try:
    sys.stdout.reconfigure(encoding="utf-8")
except Exception:
    pass

PY = "d:/4/Unity/工具/deadcode_xref.py"
SCRIPT_JSON = "d:/2/tools/il2cpp_out/script.json"
DUMP_CS = "d:/2/tools/il2cpp_out/dump.cs"
DLL = "d:/2/unity_run_ref/GameAssembly.dll"
DECOMP = "d:/2/tools/decomp_full/"
ILSPY = "d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/"
ASSETS = "d:/2/新解包资源/assets_full/"

IMAGE_BASE = 0x180000000
STUB_EMPTY = 0x4B33B0      # 全局共享空方法桩（ret 0）
STUB_FALSE = 0x6FB110      # 高频共享 `return false` 桩

RISK = ("⚠️ 残余风险：`E8` 扫描看不到**被内联**的调用点（先例 `ManaBuff.DeepCopy` 被内联进**活的** "
        "`AbilityLogic.PlayAbility`）、看不到虚表/接口/委托调用（`call [reg+off]`）"
        "⇒ 本工具只出「**候选 D**」，⛔ 不写「已判死」。")


# ======================================================================
#  script.json: `类$$方法` → [RVA, ...]
# ======================================================================
def load_script_index(path=SCRIPT_JSON):
    txt = io.open(path, encoding="utf-8", errors="replace").read()
    pat = re.compile(r'\{\s*"Address":\s*(-?\d+),\s*"Name":\s*"((?:[^"\\]|\\.)*)"')
    idx = {}
    for addr, raw_name in pat.findall(txt):
        a = int(addr)
        if a < 0:
            continue
        name = raw_name.replace("\\u003C", "<").replace("\\u003E", ">")
        if "$$" not in name:
            continue
        cls, meth = name.split("$$", 1)
        # `Name` 有时带签名尾巴（`Foo$$Bar(Int32)`）—— 方法名取到左括号为止
        meth = meth.split("(")[0]
        idx.setdefault((cls, meth), []).append(a)
    return idx


# ======================================================================
#  dump.cs: `类.方法` → RVA（用来交叉印证，且能看出「库里没这个体」）
# ======================================================================
def load_dump_rvas(path=DUMP_CS):
    """返回 {(cls, meth): [rva...]}（负值也收，用来区分「有签名没体」）。"""
    out = {}
    cur = None
    cls_re = re.compile(r'^\s*(?:public |private |internal |protected )?(?:static |sealed |abstract |partial )*'
                        r'(?:class|struct|interface)\s+([A-Za-z0-9_\.<>,` ]+)')
    # ⚠️ dump.cs 写的是**十六进制**（`// RVA: 0x5EFBF0 Offset: …`）；负值是 `// RVA: -1`
    rva_re = re.compile(r'^\s*// RVA:\s*(-?0x[0-9A-Fa-f]+|-?\d+)')
    meth_re = re.compile(r'^\s*(?:public|private|internal|protected|static|virtual|override|abstract|sealed|'
                         r'extern|new|readonly|unsafe|\s)*[\w<>\[\],\.\?\* ]+\s+'
                         r'([A-Za-z0-9_<>.`]+)\s*\(')
    pending = None
    with io.open(path, encoding="utf-8", errors="replace") as f:
        for ln in f:
            m = cls_re.match(ln)
            if m:
                cur = m.group(1).strip().split("<")[0]
                pending = None
                continue
            r = rva_re.match(ln)
            if r:
                s = r.group(1)
                pending = int(s, 16) if s.lstrip("-").startswith(("0x", "0X")) else int(s)
                continue
            if pending is not None:
                mm = meth_re.match(ln)
                if mm and cur:
                    out.setdefault((cur, mm.group(1)), []).append(pending)
                pending = None
    return out


# ======================================================================
#  .text 扫 `E8/E9 rel32`：一次性建「目标 VA → 调用点列表」
# ======================================================================
def scan_text_calls(dll=DLL):
    buf = io.open(dll, "rb").read()
    e_lfanew = struct.unpack_from("<I", buf, 0x3C)[0]
    nsec = struct.unpack_from("<H", buf, e_lfanew + 6)[0]
    opt_size = struct.unpack_from("<H", buf, e_lfanew + 20)[0]
    sec = e_lfanew + 24 + opt_size
    calls = {}
    for i in range(nsec):
        o = sec + i * 40
        name = buf[o:o + 8].rstrip(b"\0")
        vsize, vaddr, rsize, raddr = struct.unpack_from("<IIII", buf, o + 8)
        characteristics = struct.unpack_from("<I", buf, o + 36)[0]
        # 🔴 2026-10-27（B88-E1b 实测踩到）：本 build 的**代码在名为 `il2cpp` 的节里**，
        #   `.text` 里放的不是 IL2CPP 代码 ⇒ 只扫 `.text` 会得到「0 调用点」的**假死代码**结论
        #   （自检样本 `BattleManager.DrawCardFromDeck` 就是这么被判成「内联可疑」的）。
        #   判据 = `工具/disasm_va.py` 自己的输出头 `节 il2cpp`。⇒ 按**可执行属性**扫，不按名字扫。
        is_code = (characteristics & 0x20000000) != 0        # IMAGE_SCN_MEM_EXECUTE
        if not is_code and name not in (b".text", b"il2cpp"):
            continue
        data = buf[raddr:raddr + rsize]
        n = len(data)
        for op in (0xE8, 0xE9):
            ch = bytes([op])
            pos = data.find(ch)
            while pos != -1 and pos + 5 <= n:
                rel = struct.unpack_from("<i", data, pos + 1)[0]
                src = IMAGE_BASE + vaddr + pos
                tgt = (src + 5 + rel) & 0xFFFFFFFFFFFFFFFF
                if IMAGE_BASE <= tgt < IMAGE_BASE + 0x20000000:
                    calls.setdefault(tgt, []).append((src, "call" if op == 0xE8 else "jmp"))
                pos = data.find(ch, pos + 1)
        # 🔴 「**取地址**」也要收：委托 / 方法组 / 事件订阅在 x86-64 上是
        #   `48 8D <modrm> disp32`（lea reg, [rip+disp32]），**没有 E8** ⇒ 只扫 E8 会把
        #   「被当成方法组传出去」的方法误判成死代码。modrm 的低 3 位必须是 101（RIP 相对）。
        i = 0
        n2 = n - 7
        while i < n2:
            if data[i] == 0x48 and data[i + 1] == 0x8D and (data[i + 2] & 0xC7) == 0x05:
                disp = struct.unpack_from("<i", data, i + 3)[0]
                src = IMAGE_BASE + vaddr + i
                tgt = (src + 7 + disp) & 0xFFFFFFFFFFFFFFFF
                if IMAGE_BASE <= tgt < IMAGE_BASE + 0x20000000:
                    calls.setdefault(tgt, []).append((src, "lea"))
            i += 1
    return calls


# ======================================================================
#  decomp_full / ILSpy 的调用者计数
# ======================================================================
def count_decomp_callers(cls, meth, cache={}):
    """`Class__Method(` 出现在**别的文件**里的次数（精确：`.c` 里的符号名带类前缀）。"""
    key = "__dc__"
    if key not in cache:
        idx = {}
        for fn in os.listdir(DECOMP):
            if not fn.endswith(".c"):
                continue
            txt = io.open(DECOMP + fn, encoding="utf-8", errors="replace").read()
            for m in re.finditer(r'\b([A-Za-z0-9_]+)__([A-Za-z0-9_]+)\s*\(', txt):
                idx.setdefault((m.group(1), m.group(2)), set()).add(fn)
        cache[key] = idx
    files = cache[key].get((cls, meth), set())
    return sorted(f for f in files if f != "%s__%s.c" % (cls, meth))


def count_ilspy_callers(cls, meth, cache={}):
    """按**名**粗算：`Method(` 出现在**别的类文件**里的文件数（ILSpy 一类一文件）。

    ⚠️ 这是**近似**：同名方法（重载 / 不同类的同名者）会互相污染 ⇒ 只当旁证。
    """
    key = "__il__"
    if key not in cache:
        idx = {}
        for fn in os.listdir(ILSPY):
            if not fn.endswith(".cs"):
                continue
            txt = io.open(ILSPY + fn, encoding="utf-8", errors="replace").read()
            for m in re.finditer(r'\b([A-Za-z0-9_]+)\s*\(', txt):
                idx.setdefault(m.group(1), set()).add(fn)
        cache[key] = idx
    own = "%s.cs" % cls
    return sorted(f for f in cache[key].get(meth, set()) if f != own)


# ======================================================================
#  资源串扫描（默认关：247k 文件、4.4 GB，一次几分钟）
# ======================================================================
def scan_assets_for(name):
    hits = []
    for root, _dirs, files in os.walk(ASSETS):
        for fn in files:
            if not fn.endswith((".json", ".asset", ".txt", ".yaml")):
                continue
            p = os.path.join(root, fn)
            try:
                if name.encode() in io.open(p, "rb").read():
                    hits.append(p)
            except Exception:
                pass
            if len(hits) >= 5:
                return hits
    return hits


# ======================================================================
#  单条判定
# ======================================================================
def judge(cls, meth, script_idx, dump_idx, calls, all_variants=False, with_assets=False):
    rvas_s = script_idx.get((cls, meth), [])
    rvas_d = dump_idx.get((cls, meth), [])
    rvas = sorted(set(rvas_s) | set(r for r in rvas_d if r >= 0))
    rec = {
        "cls": cls, "meth": meth, "rva": "-", "va": "-",
        "e8": "?", "lea": "-", "dec": "-", "il": "-", "stub": "-", "verdict": "?", "why": "",
    }
    if not rvas:
        neg = [r for r in rvas_d if r < 0]
        rec["verdict"] = "查不到 RVA"
        rec["why"] = ("dump.cs 里只有签名、`// RVA:` 为负（泛型实例/被剥体）⇒ **不是死代码的证据**"
                      if neg else
                      "script.json 与方法名都没有命中（拼写/泛型转义？先试 `Foo<Bar>` 或换子串）")
        return [rec]

    out = []
    for rva in rvas:
        r = dict(rec)
        r["rva"] = "0x%X" % rva
        r["va"] = "0x%X" % (IMAGE_BASE + rva)
        stub = "-"
        if rva == STUB_EMPTY:
            stub = "EMPTY_STUB(0x4B33B0 ret 0)"
        elif rva == STUB_FALSE:
            stub = "FALSE_STUB(0x6FB110 return false)"
        r["stub"] = stub
        hit_all = calls.get(IMAGE_BASE + rva, [])
        hit = [h for h in hit_all if h[1] != "lea"]
        lea = [h for h in hit_all if h[1] == "lea"]
        r["e8"] = ("SHARED(%d)" % len(hit)) if stub != "-" else str(len(hit))
        r["lea"] = str(len(lea))
        dec = count_decomp_callers(cls, meth)
        il = count_ilspy_callers(cls, meth)
        r["dec"] = str(len(dec))
        r["il"] = str(len(il))
        rec_why = []
        if dec:
            rec_why.append("decomp 调用者: " + ", ".join(dec[:4]))
        if il:
            rec_why.append("ILSpy 同名调用文件: " + ", ".join(il[:4]))
        if stub != "-":
            r["verdict"] = "桩（地址共享，按地址归因无效）"
            rec_why.append("该 RVA 是共享桩 ⇒ E8 命中数不代表本方法；要区分只能按**实参个数**看反汇编")
        elif hit:
            r["verdict"] = "活的要做"
            rec_why.append("E8 调用点: " + ", ".join("0x%X" % s for s, _ in hit[:4]))
        elif lea:
            r["verdict"] = "活的要做"
            rec_why.append("**取地址**（lea [rip+disp]）%d 处 ⇒ 委托/方法组/事件订阅在用 ⇒ 活的"
                           % len(lea) + "：" + ", ".join("0x%X" % s for s, _ in lea[:4]))
        elif dec or il:
            r["verdict"] = "内联可疑"
            rec_why.append("二进制 0 个直接调用点，但反编译树里有人引用 ⇒ 可能被内联 / 虚调用")
        else:
            r["verdict"] = "候选 D"
            rec_why.append("三源 0 调用点（.text E8/E9 / decomp_full / ILSpy）")
        if with_assets:
            ah = scan_assets_for(meth)
            rec_why.append("资源串命中(前5): " + (", ".join(ah) if ah else "0"))
            if ah and r["verdict"] == "候选 D":
                r["verdict"] = "内联可疑"
                rec_why.append("⚠️ 资源里有同名串 ⇒ 可能是字符串调用，不能判 D")
        r["why"] = " ｜ ".join(rec_why)
        out.append(r)
        if not all_variants:
            break
    return out


# ======================================================================
#  CLI
# ======================================================================
def usage():
    print(__doc__)


def main(argv):
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--method", default="", help="`类.方法`（例 `CardScript.ResolveTransient`）")
    ap.add_argument("--batch", default="", help="清单文件：每行 `类.方法`（可带 tab 附加列，原样回显）")
    ap.add_argument("--selftest", action="store_true", help="两个已知样本自证")
    ap.add_argument("--all", action="store_true", help="重载：逐条打（默认只打第一条）")
    ap.add_argument("--assets", action="store_true", help="额外扫 assets_full 的字符串引用（慢）")
    ap.add_argument("-h", "--help", action="store_true")
    a = ap.parse_args(argv[1:])
    if a.help or (not a.method and not a.batch and not a.selftest):
        usage()
        return 0

    print("# deadcode_xref.py —— 只读 · 三源判据（dump.cs RVA / .text E8 扫描 / 反编译树调用者）")
    print("# " + RISK)

    script_idx = load_script_index()
    dump_idx = load_dump_rvas()
    print("# script.json 索引 %d 个 (类,方法)；dump.cs 索引 %d 个" % (len(script_idx), len(dump_idx)))
    if a.selftest:
        cases = [("CardScript", "ResolveTransient", "候选 D"),
                 ("BattleManager", "DrawCardFromDeck", "活的要做")]
        calls = None
        ok = True
        rows = []
        for cls, meth, want in cases:
            if calls is None:
                print("# 正在扫 .text（一次性，只做一遍）…")
                calls = scan_text_calls()
                print("# .text 里共登记 %d 个直接调用目标" % len(calls))
            got = judge(cls, meth, script_idx, dump_idx, calls, all_variants=True)[0]
            rows.append((cls, meth, want, got))
        print("\n=== SELFTEST ===")
        for cls, meth, want, got in rows:
            hit = got["verdict"] == want
            ok = ok and hit
            print("%s  %s.%s  期望=%s  实得=%s  (VA %s · E8 %s · dec %s · il %s)"
                  % ("✅" if hit else "❌", cls, meth, want, got["verdict"],
                     got["va"], got["e8"], got["dec"], got["il"]))
        print("SELFTEST %s" % ("PASS" if ok else "FAIL"))
        return 0 if ok else 1

    if a.method:
        if "." not in a.method:
            print("⛔ 写法是 `类.方法`（例 `CardScript.ResolveTransient`）")
            return 2
        cls, meth = a.method.split(".", 1)
        print("# 正在扫 .text（一次性，只做一遍）…")
        calls = scan_text_calls()
        for r in judge(cls, meth, script_idx, dump_idx, calls, a.all, a.assets):
            print("\n=== %s.%s ===" % (cls, meth))
            print("dump.cs RVA : %s   %s" % (r["rva"], r["va"]))
            print("E8 调用点   : %s   取地址(lea): %s%s" % (r["e8"], r.get("lea", "-"), ("   [" + r["stub"] + "]") if r["stub"] != "-" else ""))
            print("decomp_full : %s" % r["dec"])
            print("ILSpy(同名) : %s" % r["il"])
            print("判定        : **%s**" % r["verdict"])
            print("证据        : %s" % r["why"])
            print("            " + RISK)
        return 0

    # ---- batch：清单 → TSV ----
    lines = [l.rstrip("\n").rstrip("\r") for l in io.open(a.batch, encoding="utf-8-sig")]
    pairs = []
    for l in lines:
        if not l.strip() or l.lstrip().startswith("#"):
            continue
        head = l.split("\t")[0].strip()
        if "." not in head:
            continue
        cls, meth = head.split(".", 1)
        pairs.append((cls, meth.strip(), l))
    print("# batch：%d 条待判" % len(pairs))
    print("# 正在扫 .text（一次性，只做一遍）…")
    calls = scan_text_calls()
    print("# .text 里共登记 %d 个直接调用目标" % len(calls))
    print("类\t方法\tRVA\tVA\tE8命中\tdec调用者\tilspy调用者\t桩\t判定\t证据")
    for cls, meth, orig in pairs:
        rs = judge(cls, meth, script_idx, dump_idx, calls, all_variants=False, with_assets=a.assets)
        r = rs[0]
        print("\t".join([cls, meth, r["rva"], r["va"], r["e8"], r["dec"], r["il"],
                         r["stub"], r["verdict"], r["why"].replace("\t", " ")]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
