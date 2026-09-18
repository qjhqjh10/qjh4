# -*- coding: utf-8 -*-
"""按虚拟地址反汇编 `GameAssembly.dll` 的指令流（x86-64）。

**为什么需要它**：反编译出来的 `.c` **看不到浮点实参** —— 经 VA 直接调用的 helper
（`Mathf.Tan` / `Mathf.Atan2`）在 Ghidra 里没有签名，实参留在 **XMM 寄存器**里没实体化成
C 参数（例：`AnimFXModuleScaleByTarget__ChangeShapeAngle.c` 里 `FUN_180486da0()` 一个参数都没有）。
那种「数据流只活在寄存器里」的地方，**只有读指令流才能定**。

**与 Ghidra 的关系**：Ghidra 工程里也有这些指令，但 headless 出 listing 要另写脚本、还会写文件；
本工具**不动那个工程**（删了要重花几小时分析），纯 Python 直读 PE。

用法：
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/disasm_va.py <dll> <VA> [条数=60]

    # 例：AnimFXModuleScaleByTarget.ChangeShapeAngle
    $PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x180668E00 80

⚠️ 依赖 `capstone`（2026-09-18 装进 `py312`，5.0.9）。缺了就
   `$PY -m pip install capstone`（`CLAUDE.md` 铁律 8 允许自行安装）。
⚠️ 自检办法（别信印象，同 `read_literal.py`）：反汇编出来的头几条应当是**合法的函数序言**
   （`push`/`sub rsp,??` 之类），出现 `(bad)` 或乱操作数 = RVA 映射错了。
⚠️ 段表每项 40 字节，`VirtualSize` 在 `VirtualAddress` **前面**（这里踩过，别改顺序）。
⚠️ 工具看不到「没被 Ghidra 跟到的间接跳转目标」—— 反汇编是线性的，遇到跳转不会跟过去，
   跨越函数边界后会出现垃圾指令，**以 `ret` 为界自己判断**。
"""
import struct
import sys

try:
    from capstone import Cs, CS_ARCH_X86, CS_MODE_64
except ImportError:
    sys.exit('缺 capstone：%s -m pip install capstone' % sys.executable)


def load_sections(path):
    d = open(path, 'rb')
    head = d.read(0x400)
    pe = struct.unpack_from('<I', head, 0x3C)[0]
    optsz = struct.unpack_from('<H', head, pe + 20)[0]
    base = struct.unpack_from('<Q', head, pe + 24 + 24)[0]
    secs, off = [], pe + 24 + optsz
    while off + 40 <= len(head):
        name = head[off:off + 8].rstrip(b'\x00').decode(errors='replace')
        vsz, va, rsz, ptr = struct.unpack_from('<IIII', head, off + 8)  # ⚠️ VS 在前、VA 在后
        if ptr == 0 and va == 0:
            break
        secs.append((name, va, vsz, rsz, ptr))
        off += 40
    return d, base, secs


def rva_to_off(base, secs, va):
    rva = va - base
    for name, sva, vsz, rsz, ptr in secs:
        if sva <= rva < sva + max(vsz, rsz):
            return ptr + (rva - sva), name
    return None, None


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        return 2
    path = sys.argv[1]
    va = int(sys.argv[2], 16)
    n = int(sys.argv[3]) if len(sys.argv) > 3 else 60

    d, base, secs = load_sections(path)
    off, sec = rva_to_off(base, secs, va)
    if off is None:
        print('NOT MAPPED: %#x (ImageBase %#x)' % (va, base))
        return 1

    d.seek(off)
    code = d.read(n * 16)
    print('# %s  VA %#x  RVA %#x  节 %s  文件偏移 %#x  base %#x'
          % (path.split('/')[-1], va, va - base, sec, off, base))

    md = Cs(CS_ARCH_X86, CS_MODE_64)
    md.detail = False
    count = 0
    for ins in md.disasm(code, va):
        print('%X  %-24s %s %s'
              % (ins.address, ins.bytes.hex(' ').ljust(24), ins.mnemonic, ins.op_str))
        count += 1
        if count >= n:
            break
    print('# 共 %d 条' % count)
    return 0


if __name__ == '__main__':
    sys.exit(main())
