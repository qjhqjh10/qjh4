# -*- coding: utf-8 -*-
"""把虚拟地址（VA）换成**方法名** —— 读 Il2CppDumper 的 `script.json`。

**为什么需要它**：`disasm_va.py` 出来的指令流里，`call 0x182fecc60` 这种**只知道地址、
不知道调的是谁**。而 `script.json` 的 `ScriptMethod[]` 里每个方法都带 `Address`（**十进制 RVA**）
⇒ 一次扫描就能把整条调用链的**真名**还原出来，不用再靠「大概是 get_position」猜
（`AnimFX_实现与接线.md` §11.6 栽过一次：两个浮点 helper 靠旁证反推成 `Mathf.Tan`/`Mathf.Atan2`）。

用法：
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/resolve_va.py 0x1830cb160 0x183023cb0 0x182fecc60

    # 也可以直接从 disasm 输出里喂地址（尖括号会调起 shell，注意引号）：
    $PY d:/4/Unity/工具/resolve_va.py $(...)

⚠️ `script.json` 约 **127 MB**，本脚本**一次性读进内存**（约 130 MB），可接受。
⚠️ 地址是 **RVA**（= VA − ImageBase `0x180000000`），不是文件偏移 —— 别拿 `read_literal.py`
   那套节表映射的结果来喂。
⚠️ 一个 RVA 可能对应多个重载（同名多签名）⇒ **全部打印**，别只看第一条。
"""
import json
import re
import sys

IMAGE_BASE = 0x180000000
DEFAULT_SCRIPT = 'D:/2/tools/il2cpp_out/script.json'

# Address / Name / Signature 一定连着出现，直接按这个形状切，比 json.loads 省一半内存
_PAT = re.compile(rb'\{\s*"Address":\s*(\d+),\s*"Name":\s*"((?:[^"\\]|\\.)*)"')


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    want = {}
    for a in sys.argv[1:]:
        if a.startswith('0x') or a.startswith('0X'):
            want[int(a, 16)] = a
        else:
            want[int(a)] = a          # 也接受已经是 RVA 的十进制
    # 允许显式给 VA（>= ImageBase）——统一减成 RVA
    want = {(k - IMAGE_BASE if k >= IMAGE_BASE else k): v for k, v in want.items()}

    raw = open(DEFAULT_SCRIPT, 'rb').read()
    hits = {}
    for m in _PAT.finditer(raw):
        addr = int(m.group(1))
        if addr in want:
            name = json.loads('"' + m.group(2).decode('utf-8') + '"')
            hits.setdefault(addr, []).append(name)

    for rva, orig in want.items():
        names = hits.get(rva)
        if not names:
            print('%-14s RVA %#x  —— 没有方法从这个地址开始（可能是函数中段/未登记）' % (orig, rva))
            continue
        for n in names:
            print('%-14s RVA %#x  %s' % (orig, rva, n))
    return 0


if __name__ == '__main__':
    sys.exit(main())
