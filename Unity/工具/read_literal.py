# -*- coding: utf-8 -*-
"""读反编译 `.c` 里的 `_DAT_xxxxxxxxx` 常量（Ghidra 没跟到的只读字面量）。

**为什么要它**：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/` 那些反编译里，
数值常量的位置长这样：
    if (iVar1 == 0x50) { uVar3 = _DAT_1834b31c4; }   // ← 这个值没解析出来
`_DAT_` **不是「拿不到」**，是地址没被跟到 —— 它是**绝对虚拟地址**，基址是 PE 的 ImageBase
（这份 `GameAssembly.dll` 是 `0x180000000`）。拿 PE 节表把 RVA 映射成文件偏移，读 4 **或 8** 字节。

🔴 **宽度（4 / 8 字节）必须按上下文定，不能只看一个数**（2026-10-07 · A164）：
同一个地址**两档都能读**，选出垃圾/正确答案的是**它是什么类型**（见下面那条实例）。
本工具现在**两档都打**、各带一个「像不像字面量」的标记 —— 但那个标记**只是启发式**：
有实例两档都像（文件末尾那两条反例）⇒ 最终**要看反汇编里的上下文**。

**已经靠它读出来的**（`资料/战斗规则与数值_出处.md` §二/§三 有表）：
    0x1834b31c4 = 240.0   `clockTimeLimit`（EventAI 模式）                    ← 4 字节 float
    0x1834b3158 = 0.4     `CardScript.DoPushBack` 的 punch 时长              ← 4 字节 float
    0x1834b2dc8 = 0.3     同上，elasticity（调用里硬编码的 vibrato 是 8）      ← 4 字节 float
    0x1834b2bbc = 2.0     `attackStepTime × 2.0`（出手前的抬刀）里的那个 2.0   ← 4 字节 float
    0x1834b2df0 = 4       换牌掉线的重试等待（`_MulliganFallbackCountdown`）   ← 4 字节 float
    0x1834b32f8 = 0.0001  `EverguildLayoutGroup.set_ItemSize` 的重建阈值      ← 🔴 **8 字节 double**
        （原 C# 是「尺寸变化 > 1e-4 才 `MarkLayoutForRebuild`」；`menu_dump.py` 的 A150 段引用过它）。
        ⚠️ 这个地址按 **4 字节**读是 `float -1.889e+26` —— **看着像垃圾，别用它当判据**（A164 的实例）。

⚠️ **自检办法**：读出来的 float 要**整齐**（240.0 / 0.4 / 4 这种）。
   得到 1e-38 之类的乱数 = **两个原因之一**，要分开查：
   ① **RVA 映射错了**（段表字段读串位 —— 见下一条）；② 🔴 **宽度取错了**（本条的 `0x1834b32f8`）。
   本工具对「不像字面量」的那一档会打 ⚠️，并把另一档也打出来 ⇒ 先看另一档，再怀疑 ①。
⚠️ 段表每项 40 字节，字段顺序固定：`Name[8] / VirtualSize / VirtualAddress / SizeOfRawData /
   PointerToRawData` —— **VirtualSize 在 VirtualAddress 前面**（这里踩过：字段读串位，
   读出一堆乱数，还照着抄进了文档）。

用法：
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b31c4 0x1834b2bbc

每个地址打**两行**（`4B float/int` · `8B double/int64`），各带一个 ✅/⚠️ 标记：
    ✅ = 有限 且 0 或 1e-6 ≤ |v| ≤ 1e7（本仓读出来的字面量全在 [1e-4, 1e3]）
    ⚠️ = 落在这条带外 —— **多半**不是数值字面量（也可能只是量级偏大，标记不是判据）
    「两档都像」时本工具会明说，并把判断推给反汇编的上下文（**不替你选**）。
"""
import struct
import sys

if len(sys.argv) < 3:
    print(__doc__)
    sys.exit(2)

d = open(sys.argv[1], 'rb')
head = d.read(0x400)
pe = struct.unpack_from('<I', head, 0x3C)[0]
optsz = struct.unpack_from('<H', head, pe + 20)[0]
base = struct.unpack_from('<Q', head, pe + 24 + 24)[0]
secs, off = [], pe + 24 + optsz
while off + 40 <= len(head):
    name = head[off:off + 8].rstrip(b'\x00')
    vsz, va, rsz, ptr = struct.unpack_from('<IIII', head, off + 8)   # ⚠️ 顺序：VS 在前、VA 在后
    if ptr == 0 and va == 0:
        break
    secs.append((name, va, vsz, rsz, ptr))
    off += 40

def plausible(v):
    """这个浮点读数**像不像**代码里的字面量 —— 🔴 **启发式，不是判据**（只用来打标记，不替读者选）。

    带 = 有限 且（0 或 1e-6 ≤ |v| ≤ 1e7）。理由：本仓靠本工具读出来的字面量全落在 [1e-4, 1e3]
    （240 / 600 / 0.4 / 0.3 / 2 / 4 / 0.0001）；而「RVA 映射错」或「宽度读错」得到的典型量级是
    1e-38 / 1e-10 / 1e+17 / 1e+26。
    ⚠️ **它判不了「两个宽度都像字面量」**（实测两条反例，都落在带里）：
      · `0x1834b3158`（真值 = 4 字节 `0.4`）的 8 字节读 = **0.000170898**
      · `0x1834b2df0`（真值 = 4 字节 `4`）  的 8 字节读 = **2048**
    ⇒ 那种情况**只能看反汇编里的上下文**（这个 `_DAT_` 被赋给什么类型 / 和什么比较）
    ⇒ 所以两档**都打出来**，由人定。
    """
    if v != v or v in (float('inf'), float('-inf')):
        return False
    return v == 0.0 or 1e-6 <= abs(v) <= 1e7


def mark(v):
    return '✅ 像字面量' if plausible(v) else '⚠️ 不像字面量'


for a in sys.argv[2:]:
    rva = int(a, 16) - base
    for name, va, vsz, rsz, ptr in secs:
        if va <= rva < va + max(vsz, rsz):
            nm = name.decode(errors='replace')
            d.seek(ptr + (rva - va))
            b4 = d.read(4)
            d.seek(ptr + (rva - va))
            b8 = d.read(8)
            f4 = struct.unpack('<f', b4)[0]
            print('%-16s %-8s 4B float %-15g int %-12d %s'
                  % (a, nm, f4, struct.unpack('<i', b4)[0], mark(f4)))
            if len(b8) < 8:
                # 段尾：RVA 落点离文件末端不足 8 字节（`max(vsz, rsz)` 允许读到 rsz 之外）
                print('%-16s %-8s 8B 读不了（离这个段的文件末端只剩 %d 字节）' % (a, nm, len(b8)))
                break
            f8 = struct.unpack('<d', b8)[0]
            print('%-16s %-8s 8B double %-14g int64 %-20d %s'
                  % (a, nm, f8, struct.unpack('<q', b8)[0], mark(f8)))
            # 「两档都不像」/「两档都像」两种都**明说**（含「别拿这个表当判据」的指路）
            if not plausible(f4) and not plausible(f8):
                print('%s ⚠️ 两个宽度都不像数值字面量 ⇒ 它多半**不是数值**：可能是字符串 / 类型表'
                      '（`Il2CppType*`）/ 方法表（`MethodInfo*`）的地址 —— 见'
                      ' `资料/战斗规则与数值_出处.md` §三「`_DAT_` 有【三类】」' % a)
            elif plausible(f4) and plausible(f8):
                print('%s ⚠️ **两档都像字面量** ⇒ 选哪个看反汇编里的上下文（赋给什么类型 / 和什么比），'
                      '本工具不替你选（见文件头两条反例）' % a)
            break
    else:
        print(a, 'NOT MAPPED')
