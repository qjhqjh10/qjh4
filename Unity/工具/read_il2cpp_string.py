#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""read_il2cpp_string.py — 解 `DAT_1842xxxxx` 这类 **il2cpp metadata usage 槽** 里的字符串。

**为什么要它**：反编译 `.c` 里有两族 `DAT_`，长得一样、读法完全不同（2026-10-28 B92_N4 §5 点出）：

| 形状 | 是什么 | 谁读得了 |
|---|---|---|
| `DAT_1834xxxxx`（`.rdata`/`.text`） | **裸数值常量**（float / double / int） | `工具/read_literal.py` |
| `DAT_1842xxxxx`（`.data`） | 🔴 **il2cpp metadata usage 槽**：8 字节，`0xA0000000 \\| 编码下标` | **本工具** |

槽里存的是**编码过的元数据下标**，不是字面量；`工具/read_literal.py` 读它会得到
`-1.08e-19` 那种垃圾（那正是「看着像读不出来」的假象）。字符串本体在
`Warpforge_Data/il2cpp_data/Metadata/global-metadata.dat` 的 **string literal 表**里。

解码链（三步，逐步可核）
------------------------
1. **VA → 槽值**：`RVA = VA − ImageBase(0x180000000)`，按 PE 节表换成文件偏移，读 **8 字节**
   （节表字段顺序 `Name[8] / VirtualSize / VirtualAddress / SizeOfRawData / PointerToRawData`
   —— **VirtualSize 在前**，见 `read_literal.py` 文件头那条踩过的坑）。
2. **槽值 → 类型 + 下标**：取**低 32 位**，`type = v >> 29`、`idx = v & 0x1FFFFFFF`。
   `type == 5` ⇒ **StringLiteral**（1=TypeInfo 2=Il2CppType 3=MethodDef 4=FieldInfo 6=MethodRef）
   —— 只有 5 归本工具管，其余如实报「不是字符串」。
3. 🔴 **下标 → 表下标：`entry = idx >> 1`**（本 build 实测；见下「凭什么这么移位」）。

`global-metadata.dat` 的取法：头 4 字节 sanity `0xFAB11BAF`、紧跟 `version`（本 build = 31），
之后依次 `stringLiteralOffset / stringLiteralSize / stringLiteralDataOffset / stringLiteralDataSize`。
每个条目 = `{ uint32 length; uint32 dataIndex }`（16 字节 → 8 字节一项），
字符串 = `data[stringLiteralDataOffset + dataIndex : +length]`（**不是 NUL 结尾**）。

🔴 **凭什么把 idx 右移一位**（2026-10-28 实测，不是猜）
--------------------------------------------------------
同一槽位按「不移位」和「右移 1 位」各解一遍，拿 **ILSpy 反编译体里出现过的字面量集合**
当判据（`out_cpp2il_dev_ilspy/<类>.cs` 的同一方法体，C# 转义反转义后比集合）：

| 解 法 | 命中字面量 | 命中方法 |
|---|---|---|
| `entry = idx` | **2** 条 | 2 / 6767 |
| `entry = idx >> 1` | **3271** 条 | **1500 / 6767** |

且 `entry = idx` 解出来的都是**别的函数**的字符串（`'Fulah'` / `'Cheerful'` —— 表里真有这些词，
所以「解出来是个像样的英文」**不等于解对了**）。`--selftest` 把 5 条已知真值钉死。
⚠️ 这是**按本 build 的实测规律**：换 build 先跑 `--selftest`；它红了说明规律变了，⛔ 别硬套。

用法
----
    python -I -X utf8 d:/4/Unity/工具/read_il2cpp_string.py DAT_1842baac0 0x184271c70
    python -I -X utf8 d:/4/Unity/工具/read_il2cpp_string.py --search "Assets\\\\"
    python -I -X utf8 d:/4/Unity/工具/read_il2cpp_string.py --selftest
    --dll PATH / --md PATH  换文件（默认见下）·  --limit N  --search 时最多列几条（默认 30）

  · **不带参数 ⇒ 只打印本用法、退出码 2**（本工具**纯只读、一个字节都不写**）。
  · `-h/--help` ⇒ 打印用法、退出码 0。
  · 退出码：0 = 全部解出；1 = 有解不出的（token 不在已映射段 / 类型不是 5 / 表越界）；
    2 = 用法错 / 文件打不开 / metadata 头不对。

`--search TEXT` 是**反查**：在 string literal 表里找含 TEXT 的条目，并扫 `GameAssembly.dll` 的
**整个 `.data` 段**，列出哪些槽（VA）指向它 —— 反过来找「某个字符串在代码里是哪个 `DAT_`」。
块头 4 字节槽的判定：`(v >> 29) == 5 且 ((v & 0x1FFFFFFF) >> 1) == entry`。

配套说明文档：`d:/4/Unity/资料/工具/read_il2cpp_string.md`（用法 + 已解出的实例 + 机制未考证的那部分）。
"""
import os
import re
import struct
import sys

DEFAULT_DLL = r"D:/2/unity_run_ref/GameAssembly.dll"
DEFAULT_MD = r"D:/2/unity_run_ref/Warpforge_Data/il2cpp_data/Metadata/global-metadata.dat"

META_SANITY = 0xFAB11BAF
TYPE_CN = {
    0: "Invalid(未初始化)",
    1: "TypeInfo",
    2: "Il2CppType",
    3: "MethodDef",
    4: "FieldInfo",
    5: "StringLiteral",
    6: "MethodRef",
}

# `--selftest` 的 5 条已知真值（真值出处 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/
#   SceneInitializer.cs` 里 `GetUnityFormatStacktrace` 的方法体：`StackTrace.Split("\n")` /
#   `.Contains("Assets\\")` / `.Split(":")` / `.Split(" in ")` / `String.Format(template, 5 参数组)`）
SELFTEST = [
    (0x1842baac0, "\n"),
    (0x184271c70, "Assets\\"),
    (0x1842c7cf8, ":"),
    (0x184238728, " in "),
    (0x184268c80, '{0} in <a href="{1}" line="{2}">{3}:{4}</a>'),
]
# 反向自检：`entry = idx` 这种错解在同一个槽上会解出这些（表里真有、但**不是这个函数**用的）
SELFTEST_WRONG = {
    0x184271c70: "Fulah",
    0x1842c7cf8: "Cheerful",
}

USAGE = __doc__


def _u8(b):
    return b.decode("utf-8", "replace")


def load_pe(path):
    """返回 `(sections, image_base)`；sections 每项 = `(name, va, vsize, rawsize, rawptr)`。"""
    data = open(path, "rb").read()
    if data[:2] != b"MZ":
        raise ValueError("不是 PE 文件（缺 MZ）: " + path)
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        raise ValueError("不是 PE 文件（缺 PE 签名）: " + path)
    nsec = struct.unpack_from("<H", data, pe + 6)[0]
    optsz = struct.unpack_from("<H", data, pe + 20)[0]
    base = struct.unpack_from("<Q", data, pe + 24 + 24)[0]
    secs = []
    off = pe + 24 + optsz
    for i in range(nsec):
        o = off + i * 40
        name = data[o:o + 8].rstrip(b"\x00").decode(errors="replace")
        # ⚠️ 字段顺序：VirtualSize 在 VirtualAddress 前面（read_literal.py 文件头那条踩过的坑）
        vsz, va, rsz, ptr = struct.unpack_from("<IIII", data, o + 8)
        secs.append((name, va, vsz, rsz, ptr))
    return data, secs, base


def va_to_off(secs, base, va):
    rva = va - base
    for name, sva, vsz, rsz, ptr in secs:
        if sva <= rva < sva + max(vsz, rsz):
            return name, ptr + (rva - sva)
    return None, None


class Metadata(object):
    """`global-metadata.dat` 的 string literal 表（只读）。"""

    def __init__(self, path):
        self.data = open(path, "rb").read()
        if len(self.data) < 24:
            raise ValueError("metadata 文件太小: " + path)
        self.sanity, self.version = struct.unpack_from("<Ii", self.data, 0)
        if (self.sanity & 0xFFFFFFFF) != META_SANITY:
            raise ValueError("metadata sanity 不对：读到 0x%08x，应为 0x%08x"
                             % (self.sanity & 0xFFFFFFFF, META_SANITY))
        (self.so, self.ss, self.sdo, self.sds) = struct.unpack_from("<iiii", self.data, 8)
        if min(self.so, self.ss, self.sdo, self.sds) < 0:
            raise ValueError("metadata 头的 stringLiteral 四个字段有负数 ⇒ 版本不兼容（读到 version=%d）"
                             % self.version)
        self.count = self.ss // 8
        self._cache = {}

    def literal(self, entry):
        """表下标 → 字符串（越界 / 解不了 ⇒ None）。"""
        if entry in self._cache:
            return self._cache[entry]
        s = None
        if 0 <= entry < self.count:
            ln, di = struct.unpack_from("<II", self.data, self.so + entry * 8)
            if ln <= 1 << 20 and di + ln <= self.sds:
                try:
                    s = self.data[self.sdo + di: self.sdo + di + ln].decode("utf-8")
                except UnicodeDecodeError:
                    s = None
        self._cache[entry] = s
        return s

    def find(self, needle):
        """含 needle 的全部表下标（顺序扫，26k 条很快）。"""
        out = []
        for i in range(self.count):
            s = self.literal(i)
            if s is not None and needle in s:
                out.append(i)
        return out


def parse_token(a):
    """`DAT_1842baac0` / `0x1842baac0` / `1842baac0` ⇒ int。"""
    t = a.strip().strip("`,;（）()")
    t = re.sub(r"^(?:DAT_|0x)", "", t, flags=re.I)
    try:
        return int(t, 16)
    except ValueError:
        return None


def resolve(tok, dll, secs, base, md):
    """解一个 token。返回 `(ok, 说明行, 详情dict)`。"""
    va = parse_token(tok)
    if va is None:
        return False, "%-16s 不是地址（读不出 hex）" % tok, {}
    name, fo = va_to_off(secs, base, va)
    if fo is None:
        return False, "0x%-12x 不在任何已映射段（ImageBase=0x%x）" % (va, base), {}
    raw = dll[fo:fo + 8]
    if len(raw) < 8:
        return False, "0x%-12x 在 %s 段尾，不足 8 字节" % (va, name), {}
    v = struct.unpack("<Q", raw)[0]
    lo = v & 0xFFFFFFFF
    typ = lo >> 29
    idx = lo & 0x1FFFFFFF
    head = "0x%-12x %-8s 槽值=0x%016x type=%d(%s) idx=%d" % (
        va, name, v, typ, TYPE_CN.get(typ, "未知"), idx)
    if typ != 5:
        return False, head + "  ⇒ ⛔ 不是字符串字面量（本工具只解 type=5）", {"type": typ, "idx": idx}
    entry = idx >> 1
    s = md.literal(entry)
    if s is None:
        return False, head + " entry=%d ⇒ 表里取不到（表 %d 条，越界或非 UTF-8）" % (
            entry, md.count), {"type": typ, "idx": idx, "entry": entry}
    return True, head + " entry=%d ⇒ %r" % (entry, s), {"type": typ, "idx": idx, "entry": entry, "s": s}


def scan_slots(dll, secs, base, entry):
    """反查：`.data` 段里哪些 4 字节槽指向 entry（返回 VA 列表）。"""
    hits = []
    for name, sva, vsz, rsz, ptr in secs:
        if name not in (".data", "il2cpp"):
            continue
        blob = dll[ptr:ptr + min(vsz, rsz)]
        for o in range(0, len(blob) - 3, 4):
            v = struct.unpack_from("<I", blob, o)[0]
            if (v >> 29) == 5 and ((v & 0x1FFFFFFF) >> 1) == entry:
                hits.append(base + sva + o)
    return hits


def main(argv):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    if any(a in ("-h", "--help", "/?") for a in argv):
        print(USAGE)
        return 0
    if not argv:
        print(USAGE)
        return 2

    dll_path, md_path, needle, limit = DEFAULT_DLL, DEFAULT_MD, None, 30
    toks = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a in ("--dll", "--md", "--search", "--limit"):
            if i + 1 >= len(argv):
                print("参数 %s 后面缺值" % a)
                return 2
            v = argv[i + 1]
            if a == "--dll":
                dll_path = v
            elif a == "--md":
                md_path = v
            elif a == "--search":
                needle = v
            else:
                try:
                    limit = int(v)
                except ValueError:
                    print("--limit 要整数")
                    return 2
            i += 2
            continue
        if a == "--selftest":
            return selftest(dll_path, md_path)
        if a.startswith("--"):
            print("不认识的参数：%s（-h 看用法）" % a)
            return 2
        toks.append(a)
        i += 1

    for p in (dll_path, md_path):
        if not os.path.isfile(p):
            print("找不到文件：%s" % p)
            return 2
    dll, secs, base = load_pe(dll_path)
    md = Metadata(md_path)
    print("= GameAssembly: %s（ImageBase=0x%x，%d 段）" % (dll_path, base, len(secs)))
    print("= metadata    : %s（version=%d，string literal %d 条 @0x%x，data @0x%x）"
          % (md_path, md.version, md.count, md.so, md.sdo))

    if needle is not None:
        ents = md.find(needle)
        print("\n-- 表里含 %r 的条目：%d 条%s" % (needle, len(ents),
                                          "（只列前 %d）" % limit if len(ents) > limit else ""))
        for e in ents[:limit]:
            slots = scan_slots(dll, secs, base, e)
            shown = ", ".join("0x%x" % s for s in slots[:4])
            print("  entry=%-6d %-58r  槽(VA)=%s%s"
                  % (e, md.literal(e), shown or "（.data 里没有槽指向它）",
                     " …共 %d 个" % len(slots) if len(slots) > 4 else ""))
        if not ents:
            print("  （没有）—— ⚠️ 注意 C# 源码里的 `\\\\` 在 shell 里要写成 `\\\\\\\\`，"
                  "或改用不含反斜杠的子串")
        return 0

    if not toks:
        print("\n没有给地址（-h 看用法）")
        return 2
    bad = 0
    print()
    for t in toks:
        ok, line, _d = resolve(t, dll, secs, base, md)
        print(("  " if ok else "XX ") + line)
        if not ok:
            bad += 1
    print("\n合计：%d 条 / 解出 %d / 解不出 %d" % (len(toks), len(toks) - bad, bad))
    return 0 if bad == 0 else 1


def selftest(dll_path, md_path):
    """5 条已知真值 + 2 条「错解必然解错」的反例。全绿 ⇒ 本 build 的规律没变。"""
    if not (os.path.isfile(dll_path) and os.path.isfile(md_path)):
        print("找不到 DLL 或 metadata：%s / %s" % (dll_path, md_path))
        return 2
    dll, secs, base = load_pe(dll_path)
    md = Metadata(md_path)
    ok = bad = 0
    print("= selftest（判据 = ILSpy `SceneInitializer.GetUnityFormatStacktrace` 方法体里的真值）=")
    for va, want in SELFTEST:
        got_ok, line, d = resolve("0x%x" % va, dll, secs, base, md)
        good = got_ok and d.get("s") == want
        print(("PASS  " if good else "FAIL  ") + line + ("" if good else "   （应为 %r）" % want))
        ok, bad = (ok + 1, bad) if good else (ok, bad + 1)
    print("\n-- 反例：把 `entry = idx >> 1` 换成 `entry = idx` 必然解出**别的**字符串 --")
    for va, wrong in sorted(SELFTEST_WRONG.items()):
        v = struct.unpack("<Q", dll[va_to_off(secs, base, va)[1]:][:8])[0]
        idx = (v & 0xFFFFFFFF) & 0x1FFFFFFF
        got = md.literal(idx)
        good = got == wrong and got != dict(SELFTEST)[va]
        print(("PASS  " if good else "FAIL  ") + "0x%x  entry=idx=%d ⇒ %r（应 = %r）" % (va, idx, got, wrong))
        ok, bad = (ok + 1, bad) if good else (ok, bad + 1)
    print("\nPASS %d / FAIL %d" % (ok, bad))
    return 0 if bad == 0 else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
