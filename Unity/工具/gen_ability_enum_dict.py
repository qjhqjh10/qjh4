# -*- coding: utf-8 -*-
"""
gen_ability_enum_dict.py  —— 从原版卡 SO 真值里，扫出 abilityLogic 一族（以及它上下紧邻的
CardAbility / BoardCriteria / ActiveAbility / 卡根）用到的**每一个枚举字段**，
把「真值里实际出现过的取值」翻成枚举名。

判据（全只读）：
  ① 真值        d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json
  ② 枚举/字段定义 d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs（签名桩：枚举/字段完整）

做法：
  A. 解析 Assembly-CSharp 里**全部** `public enum X {...}` → {枚举名: {值: (成员名, 文件:行)}}
  B. 解析相关类的字段表 `public <Type> <name>;`                → {类名: [(字段, 类型, 文件:行)]}
  C. 把真值里每个 AbilityLogic / CardAbility / BoardCriteria / ActiveAbility / 卡根
     递归走一遍，统计 (路径, 字段, 取值) 出现次数
  D. 输出 TSV：结构体<TAB>字段<TAB>value<TAB>name<TAB>出现次数<TAB>出处
     结构体列 = `类型名@路径`（例 `TargetCriteria@multiplier`），保证同一结构体在不同位置分开列。

用法：python -I -X utf8 d:/4/Unity/工具/gen_ability_enum_dict.py [--out TSV]
"""
import io
import json
import os
import re
import sys
from collections import Counter, OrderedDict, defaultdict

STUB_DIR = r"d:/2/Warpforge_code/Scripts/Assembly-CSharp"
TRUTH = r"d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json"
OUT_DEFAULT = r"d:/4/Unity/资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv"

# 要解析字段表的类（同名 .cs）
STRUCT_FILES = [
    "AbilityLogic", "TargetCriteria", "DamageCriteria", "HealCriteria",
    "CardEffect", "SummonCriteria", "HandEffect", "ManaBuff",
    "CardAbility", "BoardCriteria", "ActiveAbility", "DeckCriteria",
]
# 卡根：RawCardScript : PlayerItem : ObtainableItem —— 三个文件的字段合并成合成结构体 CardSO
CARD_SO_FILES = ["RawCardScript", "PlayerItem", "ObtainableItem"]

# 结构体内部，哪些字段是子结构体（要递归）；列表字段不递归
NESTED = {
    "AbilityLogic": ["targetCriteria", "multiplier", "damageCriteria",
                     "healCriteria", "buffEffect", "summonCriteria",
                     "handBuff", "manaBuff"],
    "HandEffect": ["cardEffect", "targetCriteria"],
    "CardAbility": ["actingCardCriteria", "targetCriteria", "boardCriteria",
                    "multiCountCriteria"],
    "BoardCriteria": ["countCriteria"],
    "ActiveAbility": ["targetCriteria", "targetsAvailableCounter"],
    "DeckCriteria": ["countCriteria"],
    "CardSO": ["cardTargetCriteria", "targetsAvailableCounter",
               "choicesDefinition", "normalAbilityBoardCriteria"],
    "TargetCriteria": [],
    "DamageCriteria": [],
    "HealCriteria": [],
    "CardEffect": [],
    "SummonCriteria": [],
    "ManaBuff": [],
}


# ---------------------------------------------------------------- A. 枚举表
ENUM_RE = re.compile(r"^\s*public\s+enum\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{?\s*$")
MEMBER_RE = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,?\s*$")
BARE_RE = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*,?\s*$")


def parse_enums():
    out = {}
    for fn in sorted(os.listdir(STUB_DIR)):
        if not fn.endswith(".cs"):
            continue
        lines = io.open(os.path.join(STUB_DIR, fn), encoding="utf-8",
                        errors="replace").read().split("\n")
        i = 0
        while i < len(lines):
            m = ENUM_RE.match(lines[i])
            if m:
                members, auto, j = {}, 0, i + 1
                while j < len(lines) and not lines[j].strip().startswith("}"):
                    mm = MEMBER_RE.match(lines[j])
                    if mm:
                        members[int(mm.group(2))] = (mm.group(1), j + 1)
                        auto = int(mm.group(2)) + 1
                    else:
                        mb = BARE_RE.match(lines[j])
                        if mb and "=" not in lines[j] and "(" not in lines[j]:
                            members[auto] = (mb.group(1), j + 1)
                            auto += 1
                    j += 1
                out.setdefault(m.group(1), {"file": fn, "members": members})
                i = j
            i += 1
    return out


# ------------------------------------------------------- B. 结构体字段表
FIELD_RE = re.compile(r"^\s*public\s+([A-Za-z_][A-Za-z0-9_\.]*?)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:\{|;|=)")


def parse_fields(fname):
    path = os.path.join(STUB_DIR, fname + ".cs")
    if not os.path.exists(path):
        return []
    lines = io.open(path, encoding="utf-8", errors="replace").read().split("\n")
    out, seen = [], set()
    for idx, s in enumerate(lines):
        t = s.strip()
        if t.startswith("[") or t.startswith("//") or t.startswith("public class") \
           or t.startswith("public abstract class") or t.startswith("public enum"):
            continue
        m = FIELD_RE.match(s)
        if not m:
            continue
        typ, name = m.group(1).strip(), m.group(2).strip()
        if "<" in typ or "[" in typ or name in seen:
            continue
        seen.add(name)
        out.append((name, typ, "%s.cs:%d" % (fname, idx + 1)))
    return out


def build_structs():
    st = OrderedDict((s, parse_fields(s)) for s in STRUCT_FILES)
    merged, seen = [], set()
    for f in CARD_SO_FILES:
        for name, typ, src in parse_fields(f):
            if name not in seen:
                seen.add(name)
                merged.append((name, typ, src))
    st["CardSO"] = merged
    return st


# --------------------------------------------------------- C. 走真值
def walk(obj, struct, structs, enums, counter, extra, path):
    """把 obj 当 struct 走一遍；path 是这个结构体实例的路径名"""
    fields = dict((n, t) for n, t, _ in structs.get(struct, []))
    for fname, ftype in fields.items():
        if not isinstance(obj, dict) or fname not in obj:
            continue
        val = obj[fname]
        if ftype in enums:
            counter[("%s@%s" % (struct, path), fname, val)] += 1
        elif ftype in structs:
            if isinstance(val, dict):
                walk(val, ftype, structs, enums, counter, extra,
                     path + "." + fname)
    if isinstance(obj, dict):
        for k in obj:
            if k not in fields:
                extra[(struct, k)] += 1


def main():
    out_tsv = OUT_DEFAULT
    if "--out" in sys.argv:
        out_tsv = sys.argv[sys.argv.index("--out") + 1]

    enums = parse_enums()
    structs = build_structs()
    with io.open(TRUTH, encoding="utf-8") as f:
        cards = json.load(f)

    counter, extra = Counter(), Counter()
    n_inst = Counter()                 # 各根的实例数
    chosen_by_root = defaultdict(Counter)   # 根路径 -> chosenAbility 计数

    for c in cards:
        # 1) 卡根（CardSO）
        walk(c, "CardSO", structs, enums, counter, extra, "cardRoot")
        # 2) cardAbilities[]：CardAbility + 其 abilityLogic[]
        for i, ca in enumerate(c.get("cardAbilities") or []):
            n_inst["cardAbilities[]"] += 1
            walk(ca, "CardAbility", structs, enums, counter, extra,
                 "cardAbilities")
            for j, al in enumerate(ca.get("abilityLogic") or []):
                n_inst["cardAbilities[].abilityLogic[]"] += 1
                walk(al, "AbilityLogic", structs, enums, counter, extra,
                     "cardAbilities.abilityLogic")
                chosen_by_root["cardAbilities"][al.get("chosenAbility")] += 1
        # 3) activeAbility / alternativeAbility
        for key in ("activeAbility", "alternativeAbility"):
            aa = c.get(key)
            if isinstance(aa, dict):
                n_inst[key] += 1
                walk(aa, "ActiveAbility", structs, enums, counter, extra, key)
                for al in (aa.get("activeAbilityLogic") or []):
                    n_inst[key + ".activeAbilityLogic[]"] += 1
                    walk(al, "AbilityLogic", structs, enums, counter, extra,
                         key + ".activeAbilityLogic")
                    chosen_by_root[key][al.get("chosenAbility")] += 1

    # ------------------------------------------------ 输出 TSV
    rows, miss = [], []
    for (spath, fname, val), cnt in sorted(counter.items()):
        ftype = dict((n, t) for n, t, _ in
                     structs.get(spath.split("@")[0], [])).get(fname, "?")
        mem = enums.get(ftype, {}).get("members", {}).get(val)
        if mem:
            name, line = mem
            src = "%s.cs:%d" % (ftype, line)
        else:
            name, src = "未找到", "枚举 %s 无此值（定义见 %s.cs）" % (
                ftype, enums.get(ftype, {}).get("file", "?"))
            miss.append((spath, fname, ftype, val, cnt))
        rows.append((spath, fname, val, name, cnt, src))

    lines = ["结构体\t字段\tvalue\tname\t出现次数\t出处"]
    lines += ["%s\t%s\t%d\t%s\t%d\t%s" % r for r in rows]
    data = "\n".join(lines) + "\n"
    tmp = data.replace("\n", "\r\n").encode("utf-8")
    with io.open(out_tsv, "wb") as f:
        f.write(tmp)

    # ------------------------------------------------ 控制台摘要
    print("== 实例数 ==")
    for k, v in n_inst.items():
        print("  %-40s %d" % (k, v))
    print()
    print("== 覆盖 ==  (路径,字段) 组合 %d ; 取值行 %d"
          % (len(set((s, f) for s, f, v in counter)), len(rows)))
    print()
    print("== 未找到 ==")
    for p, f, t, v, n in miss:
        print("   %s . %s = %d  (%s)  x%d" % (p, f, v, t, n))
    if not miss:
        print("   （无）")
    print()
    print("== EXTRA KEY（真值里有、字段表里没有；列表字段属正常） ==")
    for k, v in extra.items():
        print("   %-14s %-24s %d" % (k[0], k[1], v))
    print()
    print("== chosenAbility：cardAbilities vs activeAbility（对现有词典自检）==")
    ca, aa = chosen_by_root["cardAbilities"], Counter()
    for k in ("activeAbility", "alternativeAbility"):
        aa.update(chosen_by_root[k])
    tot_ca = sum(ca.values())
    print("  cardAbilities[] 合计 %d ; active/alternative 合计 %d ; 总 %d"
          % (tot_ca, sum(aa.values()), tot_ca + sum(aa.values())))
    for v in sorted(set(ca) | set(aa), key=lambda x: -((ca[x]) + (aa[x])))[:12]:
        nm = enums["AbilityEffect"]["members"].get(v, ("?%s" % v, 0))[0]
        print("   %-26s %-4s  ca=%-4d act=%d" % (nm, v, ca[v], aa[v]))
    print()
    print("TSV ->", out_tsv)


if __name__ == "__main__":
    main()
