#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
check_c_return_type.py —— 反编译产物「返回值降级」常驻质量闸

背景
----
d:/2/tools/decomp_full/*.c 是 Ghidra 伪码，存在大量「文件在、返回值丢了」的情况：
例如 AI.ScoreFromCriteria 声明返回 float，.c 首行却打成 void、尾部只剩 return;。
这类文件曾骗过一整轮复刻（照它实现 = 静默丢逻辑）。战斗域抽样 66 条就命中 15 条(22.7%)，
全库 25,096 份从没扫过。本闸把「返回类型降级」抓出来。

判法
----
1) 解析 d:/2/tools/il2cpp_out/dump.cs 建索引：C名 -> (声明返回类型, 类全名, 方法名, is_void)。
   C名的构造规则（实测）：
     类全名的所有 '.' -> '_'，再拼 '__'，再拼方法名；
     方法名若以 '.' 开头（.ctor / .cctor），把首字符 '.' 换成 '_'。
   例：BattleManager.DrawCardFromDeck    -> BattleManager__DrawCardFromDeck
       AI.<>c__DisplayClass15_0.<X>b__0  -> AI_<>c__DisplayClass15_0__<X>b__0
       AI.<>c..cctor                     -> AI_<>c___cctor
2) 逐个 .c：Ghidra 会把签名折行，从【第一个非空行】起连续拼到出现 '(' 为止，
   取 [返回类型] 与 [函数名]。返回类型可能是 'undefined8 *' 这种带 '*' 的多 token。
   用【内容里的函数名】去查索引，绝不用文件名（本库 16.5% 文件名 != 内容）。
3) 比对分类：只有「.c 是 void / undefined* / 裸 undefined，而声明非 void」算【降级】。
   找不到声明 -> '未匹配'（不计降级）。

用法
----
  * 不带参（默认）：打印本说明并退出 —— 什么都不做、不写任何文件。
  * 真扫：  python -I -X utf8 check_c_return_type.py --scan [--out <路径>]
      --scan 必须显式给出；不给 --out 则 TSV 打到 stdout（绝不默认覆盖任何既有文件）。
  * 只读 d:/2（一个字节都不写/删）。

退出码：0 正常（含「有降级」）；2 用法错误。质量闸「红/绿」由消费方按降级计数自行判。
"""

import sys
import os
import re
import glob

DECOMP_DIR = r"d:/2/tools/decomp_full"
DUMP_CS = r"d:/2/tools/il2cpp_out/dump.cs"

# 声明里可能出现的修饰符（dump.cs 用这些前缀）
MODIFIERS = {
    "public", "private", "internal", "protected", "static", "virtual",
    "override", "abstract", "sealed", "extern", "unsafe", "new", "readonly",
    "async", "partial", "final", "volatile",
}

# 战斗域类名（按 dump 类全名的第一段精确比）
BATTLE_DOMAIN = [
    "BattleManager", "BattleManagerSupport", "CardScript", "RawCardScript",
    "EntityScript", "AbilityLogic", "MinionManager", "CardEffect",
    "Enchantment", "PlayerHand", "CardsHorizontalLayout", "CardView",
    "BattleDriver", "SimpleAI", "AI",
]

_UNDEF_PTR_RE = re.compile(r"^undefined\d*(\s*\*)+$")
_CLASS_RE = re.compile(r"\b(?:class|struct|enum|interface)\s+(.+?)\s*(?://|:|$)")
_NS_RE = re.compile(r"^\s*//\s*Namespace:\s?(.*)$")

# 「Ghidra 用 undefined8 * 表示结构体按值返回」的判定线索：
# 声明的返回类型是这些值类型/结构体时，undefined* 通常【不是】丢值。
_STRUCT_EXACT = {
    "Vector2", "Vector3", "Vector4", "Vector2Int", "Vector3Int",
    "Quaternion", "Color", "Color32", "Rect", "RectInt", "Bounds",
    "BoundsInt", "Matrix4x4", "Ray", "Ray2D", "Plane", "UIVertex",
    "RenderTextureDescriptor", "Hash128", "Decimal", "DateTime",
    "TimeSpan", "HandleRef", "AsyncOperationHandle", "LocalizedString",
    "FirebaseParam", "SphericalHarmonicsL2",
}
_STRUCT_PREFIX = ("ValueTuple<", "Nullable<", "KeyValuePair<",
                  "ArraySegment<", "Obscured")


def read_text(path):
    """二进制读 -> utf-8 解码（errors=replace）。避免文本模式把 \r 折掉。"""
    with open(path, "rb") as f:
        return f.read().decode("utf-8", "replace")


def split_lines(txt):
    """按 \n 切、去掉行尾 \r（兼容 CRLF / 裸 CR）。"""
    return [ln[:-1] if ln.endswith("\r") else ln for ln in txt.split("\n")]


def strip_modifiers(tokens):
    i = 0
    while i < len(tokens) and tokens[i] in MODIFIERS:
        i += 1
    return tokens[i:]


def c_name(type_full, method):
    """按实测规则构造 Ghidra C 名。"""
    type_part = type_full.replace(".", "_")
    m = method
    if m.startswith("."):
        m = "_" + m[1:]
    return type_part + "__" + m


def parse_dump(path):
    """
    返回 dict: c_name -> dict(ret=声明返回类型, cls=类全名, meth=方法名, void=bool)
    以及统计信息。
    """
    txt = read_text(path)
    lines = split_lines(txt)
    idx = {}
    collisions = 0
    cur_cls = None
    n_methods = 0
    cur_ns = ""          # 最近一条 // Namespace: 的值（顶层类用）
    last_top_ns = ""     # 最近一个【顶层类】的命名空间（嵌套类继承它）
    for raw in lines:
        s = raw.strip()
        if not s:
            continue
        # 命名空间注释行（dump.cs 把 namespace 写在 class 上面一行注释里）
        mn = _NS_RE.match(raw)
        if mn:
            cur_ns = mn.group(1).strip()
            continue
        # 类头
        if ("class " in s or "struct " in s or "enum " in s or "interface " in s):
            m = _CLASS_RE.search(s)
            if m:
                cand = m.group(1).strip()
                # 排除方法行误命中（方法行不会含 'class ' 这种；再加个保护）
                if cand and "(" not in cand:
                    if "." in cand:
                        # 嵌套类型：类名里已带外层类名（dump 用点表示嵌套），
                        # 它的 namespace 沿用最近顶层类的命名空间（dump 对嵌套类只给空注释）
                        full = (last_top_ns + "." + cand) if last_top_ns else cand
                    else:
                        # 顶层类型：命名空间取上面那条注释
                        last_top_ns = cur_ns
                        full = (cur_ns + "." + cand) if cur_ns else cand
                    cur_cls = full
                    continue
        if cur_cls is None:
            continue
        # 方法行：含 '(' 且以 '{ }' 或 ';' 结尾
        if "(" not in s:
            continue
        if not (s.endswith("{ }") or s.endswith(";")):
            continue
        # 取 '(' 之前的部分
        p = s.find("(")
        prefix = s[:p].strip()
        toks = prefix.split()
        toks = strip_modifiers(toks)
        if len(toks) < 2:
            continue
        meth = toks[-1]
        ret = " ".join(toks[:-1]).strip()
        if not ret or not meth:
            continue
        # 跳过 delegate 之类（meth 里带空格已在 toks[-1] 处理；'operator' 无）
        if "operator" in toks:
            continue
        key = c_name(cur_cls, meth)
        n_methods += 1
        rec = {"ret": ret, "cls": cur_cls, "meth": meth,
               "void": ret == "void"}
        if key in idx:
            collisions += 1
            old = idx[key]
            # 同 key 不同声明 -> 标记歧义
            if old["ret"] != rec["ret"] or old["cls"] != rec["cls"]:
                old["ambiguous"] = True
        else:
            idx[key] = rec
    return idx, {"methods": n_methods, "collisions": collisions}


def parse_c(path):
    """
    取 .c 的第一个非空行起的若干行拼成签名，返回 (funcname, rettype) 或 None。
    """
    txt = read_text(path)
    lines = split_lines(txt)
    joined = []
    for ln in lines:
        s = ln.strip()
        if not s:
            continue
        # 跳过注释行
        if s.startswith("/*") or s.startswith("//") or s.startswith("*"):
            continue
        joined.append(s)
        if "(" in s:
            break
        if len(joined) >= 8:
            break
    if not joined:
        return None
    sig = " ".join(joined)
    p = sig.find("(")
    prefix = sig[:p] if p >= 0 else sig
    toks = prefix.split()
    if len(toks) < 2:
        return None
    funcname = toks[-1]
    rettype = " ".join(toks[:-1]).strip()
    if not rettype or not funcname:
        return None
    return funcname, rettype


def body_flag(txt, decl_void):
    """非 void 声明的方法：若体内只有裸 return; 没有带值 return -> 疑似丢值。"""
    if decl_void:
        return ""
    bare = len(re.findall(r"\breturn\s*;", txt))
    valued = len(re.findall(r"\breturn\s+[^;\s]", txt))
    if bare > 0 and valued == 0:
        return "疑似丢值(体)"
    return ""


def looks_struct(decl_ret):
    """声明的返回类型看起来是不是值类型/结构体（Ghidra 会用 undefined8* 表示它按值返回）。"""
    t = decl_ret.strip()
    if t in _STRUCT_EXACT:
        return True
    for p in _STRUCT_PREFIX:
        if t.startswith(p):
            return True
    return False


def classify(c_ret, has_decl, decl_ret):
    cv = c_ret.strip() == "void"
    c_undef_ptr = bool(_UNDEF_PTR_RE.match(c_ret.strip()))
    c_bare_undef = c_ret.strip() == "undefined"
    if not has_decl:
        return "未匹配"
    dv = decl_ret.strip() == "void"
    if cv and not dv:
        return "降级_void"
    if c_undef_ptr and not dv:
        # 声明的返回是结构体/值类型 -> Ghidra 用 undefined8* 表示按值返回，通常不是丢值
        return "疑似_结构体按值" if looks_struct(decl_ret) else "降级_未定指针"
    if c_bare_undef and not dv:
        return "降级_裸undefined"
    if (not cv) and dv:
        return "异常_c非void声明void"
    if cv and dv:
        return "一致_都void"
    return "一致_都非void"


def battle_class(cls):
    if not cls:
        return False
    return cls.split(".")[0] in BATTLE_DOMAIN


def usage():
    print(__doc__)


def do_scan(out_path):
    sys.stderr.write("[check_c_return_type] 解析 dump.cs ...\n")
    idx, dstats = parse_dump(DUMP_CS)
    sys.stderr.write("[check_c_return_type] dump 方法 %d 条（同 key 冲突 %d），索引 %d 项\n"
                     % (dstats["methods"], dstats["collisions"], len(idx)))

    files = sorted(glob.glob(os.path.join(DECOMP_DIR, "*.c")))
    sys.stderr.write("[check_c_return_type] 待扫 .c 文件 %d 个 ...\n" % len(files))

    rows = []
    import collections
    cat_count = collections.Counter()
    cls_down = collections.Counter()      # 类 -> 降级数
    battle_rows = []
    parse_fail = 0
    matched = 0

    for i, fp in enumerate(files):
        if (i + 1) % 5000 == 0:
            sys.stderr.write("  已扫 %d/%d\n" % (i + 1, len(files)))
        pc = parse_c(fp)
        fname = os.path.basename(fp)
        if pc is None:
            parse_fail += 1
            rows.append((fname, "", "", "", "", "", "解析失败", ""))
            cat_count["解析失败"] += 1
            continue
        funcname, c_ret = pc
        rec = idx.get(funcname)
        has = rec is not None
        decl_ret = rec["ret"] if has else ""
        cls = rec["cls"] if has else ""
        meth = rec["meth"] if has else ""
        cat = classify(c_ret, has, decl_ret)
        cat_count[cat] += 1
        if has:
            matched += 1
        bf = ""
        if has:
            bf = body_flag(read_text(fp), rec["void"])
        rows.append((fname, funcname, c_ret, decl_ret, cls, meth, cat, bf))
        if cat.startswith("降级"):
            # 聚合成「最外层类型」：把编译器闭包类(.<...>)并回它的宿主类
            key_cls = (cls.split(".<")[0] if cls else funcname.split("__")[0])
            cls_down[key_cls] += 1
            if battle_class(cls) or (not cls and any(
                    funcname.startswith(b + "__") or funcname.startswith(b + "_")
                    for b in BATTLE_DOMAIN)):
                lost = decl_ret if decl_ret else "?"
                battle_rows.append((fname, funcname, c_ret, decl_ret, cls, meth, cat,
                                    "本应返回 %s，.c 里丢了" % lost, bf))

    # 写 TSV
    header = ("文件\t.c函数名\t.c首行返回类型\tdump声明返回类型\tdump类\tdump方法\t归类\t体标志\n")
    out = [header]
    # 排序：降级优先，其次未匹配/异常，再一致
    order = {"降级_void": 0, "降级_未定指针": 1, "降级_裸undefined": 2,
             "异常_c非void声明void": 3, "疑似_结构体按值": 4, "解析失败": 5,
             "未匹配": 6, "一致_都非void": 7, "一致_都void": 8}
    rows.sort(key=lambda r: (order.get(r[6], 9), r[0]))
    for r in rows:
        out.append("\t".join(str(x) for x in r) + "\n")
    data = "".join(out)

    if out_path:
        with open(out_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(data)
        sys.stderr.write("[check_c_return_type] TSV 已写: %s (%d 行)\n"
                         % (out_path, len(rows)))
    else:
        sys.stdout.write(data)

    # 摘要
    total = len(files)
    strict = cat_count["降级_void"] + cat_count["降级_裸undefined"]
    downgrade = strict + cat_count["降级_未定指针"]
    print("\n================ 摘要 ================")
    print("全库 .c 文件总数      : %d" % total)
    print("解析成功 / 失败       : %d / %d" % (total - parse_fail, parse_fail))
    print("匹配到 dump 声明      : %d (%.1f%%)" % (matched, 100.0 * matched / max(total, 1)))
    print("未匹配                : %d" % cat_count["未匹配"])
    print("-- 严格降级(丢值)       : %d   (= 降级_void %d + 降级_裸undefined %d)"
          % (strict, cat_count["降级_void"], cat_count["降级_裸undefined"]))
    print("-- undefined* 真可疑   : %d   (声明非结构体，需人工判)" % cat_count["降级_未定指针"])
    print("-- undefined* 疑似结构体按值(非降级): %d" % cat_count["疑似_结构体按值"])
    print("-- 降级合计(严格+可疑) : %d" % downgrade)
    for c in ["降级_void", "降级_未定指针", "降级_裸undefined",
              "异常_c非void声明void", "疑似_结构体按值", "一致_都void",
              "一致_都非void", "未匹配", "解析失败"]:
        print("   %-22s: %d" % (c, cat_count[c]))
    print("注：`疑似_结构体按值` 是 Ghidra 用 undefined8* 表示结构体按值返回，")
    print("    抽样 348/348 声明均为值类型(Vector3/Color/ObscuredXxx/ValueTuple/…)，")
    print("    一般【不是】丢值；判真实丢值请看 `降级_void`。")
    # 去重（按内容函数名）——decomp_full 有 ICF 折叠 / 文件名!=内容 的重复体
    uniq_down = len(set(r[1] for r in rows if r[6].startswith("降级") and r[1]))
    uniq_bat = len(set(r[1] for r in battle_rows))
    print("降级 文件数 %d / 去重(按内容函数名) %d  => 重复体 %d"
          % (downgrade, uniq_down, downgrade - uniq_down))
    print("战斗域 文件数 %d / 去重 %d" % (len(battle_rows), uniq_bat))
    print("⚠ decomp_full 有「文件名!=内容」(ICF 折叠/共享体)：工具一律【按内容里的函数名】判，")
    print("  但文件体本身可能不是该文件名对应的函数 —— 复刻时以【内容名】为准。")
    print("\n---- 按原版类聚合 Top 20（降级数）----")
    print("类名\t降级数")
    for cls, n in cls_down.most_common(20):
        print("%s\t%d" % (cls, n))
    print("\n---- 战斗域逐类降级数 ----")
    bcls = collections.Counter((r[4].split(".<")[0] if r[4] else "?") for r in battle_rows)
    for k, v in bcls.most_common():
        print("%s\t%d" % (k, v))
    print("\n---- 战斗域降级清单（%d 条）----" % len(battle_rows))
    print("文件\t函数名\t.c返回\tdump声明返回\tdump类\t方法\t归类\t丢的值\t体标志")
    for br in battle_rows:
        print("\t".join(str(x) for x in br))
    print("\n[完] 降级合计 %d 条；TSV = %s" % (downgrade, out_path or "stdout"))


def main(argv):
    if "--scan" not in argv:
        usage()
        return 0
    out_path = None
    if "--out" in argv:
        k = argv.index("--out")
        if k + 1 >= len(argv):
            sys.stderr.write("ERROR: --out 需要一个路径参数\n")
            return 2
        out_path = argv[k + 1]
    # 只读 d:/2 校验
    for p in (DECOMP_DIR, DUMP_CS):
        if not os.path.exists(p):
            sys.stderr.write("ERROR: 输入不存在: %s\n" % p)
            return 2
    do_scan(out_path)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
