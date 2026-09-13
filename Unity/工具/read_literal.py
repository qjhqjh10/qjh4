# -*- coding: utf-8 -*-
"""读反编译 `.c` 里的 `_DAT_xxxxxxxxx` 常量（Ghidra 没跟到的只读字面量）。

**为什么要它**：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/` 那些反编译里，
数值常量的位置长这样：
    if (iVar1 == 0x50) { uVar3 = _DAT_1834b31c4; }   // ← 这个值没解析出来
`_DAT_` **不是「拿不到」**，是地址没被跟到 —— 它是**绝对虚拟地址**，基址是 PE 的 ImageBase
（这份 `GameAssembly.dll` 是 `0x180000000`）。拿 PE 节表把 RVA 映射成文件偏移，读 4 字节即可。

**已经靠它读出来的**（`资料/战斗规则与数值_出处.md` §二/§三 有表）：
    0x1834b31c4 = 240.0   `clockTimeLimit`（EventAI 模式）
    0x1834b3158 = 0.4     `CardScript.DoPushBack` 的 punch 时长
    0x1834b2dc8 = 0.3     同上，elasticity（调用里硬编码的 vibrato 是 8）
    0x1834b2bbc = 2.0     `attackStepTime × 2.0`（出手前的抬刀）里的那个 2.0
    0x1834b2df0 = 4       换牌掉线的重试等待（`_MulliganFallbackCountdown`）

⚠️ **自检办法**：读出来的 float 要**整齐**（240.0 / 0.4 / 4 这种）。
   得到 1e-38 之类的乱数 = RVA 映射错了，**别当成答案**。
⚠️ 段表每项 40 字节，字段顺序固定：`Name[8] / VirtualSize / VirtualAddress / SizeOfRawData /
   PointerToRawData` —— **VirtualSize 在 VirtualAddress 前面**（这里踩过：字段读串位，
   读出一堆乱数，还照着抄进了文档）。

用法：
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b31c4 0x1834b2bbc
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

for a in sys.argv[2:]:
    rva = int(a, 16) - base
    for name, va, vsz, rsz, ptr in secs:
        if va <= rva < va + max(vsz, rsz):
            d.seek(ptr + (rva - va))
            b = d.read(4)
            print('%-16s %-8s float %-14g int %d'
                  % (a, name.decode(errors='replace'),
                     struct.unpack('<f', b)[0], struct.unpack('<i', b)[0]))
            break
    else:
        print(a, 'NOT MAPPED')
