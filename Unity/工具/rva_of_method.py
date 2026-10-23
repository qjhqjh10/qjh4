# -*- coding: utf-8 -*-
"""按【方法名】查 VA / RVA —— 走 `script.json` 的 `Address` 列（= RVA），一步到位。

**为什么需要它**（2026-10-11 · `A1509③`）：
`d:/2/tools/decomp_full/` 里那 **26,282 个方法体**只是全量的一部分 —— 相当多的方法
**在 `.c` 里【没有产物】**（Ghidra 没跟到 / 被内联 / 是泛型实例）。过去遇到这种，
本仓一律记成「**基类方法体本地缺失 / 查不到**」，然后**挂起**。
现在有路了：`script.json` 的 `ScriptMethod[].Address` **就是 RVA**（VA = `0x180000000 + Address`），
拿 VA 直接喂 `工具/disasm_va.py` 就能读指令流（`capstone`，x86-64）。

**实测例**（2026-10-11 现跑，逐条吻合）：
    $PY d:/4/Unity/工具/rva_of_method.py "CollectionDisplay\u003Cobject\u003E$$GetItemCount"
    → Address=22983888  RVA=0x15EB4D0  VA=0x1815EB4D0
    $PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x1815EB4D0 18
    → sub rsp,0x28 / mov rax,[rcx+0x58] / test rax,rax / mov eax,[rax+0x18] / ret
      ⇒ `return filteredCollection.Count`，**没有 +1**（这是 `A1503④`「原版出不来空位格」的判据）

用法：
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/rva_of_method.py <方法名片段> [最多几条=12] [script.json 路径]
    # 名称里的 `<` `>` 在 script.json 里是 `\\u003C` `\\u003E` —— 本工具**两种写法都认**
    # （直接写 `CollectionDisplay<object>$$GetItemCount` 也行）

自检办法（别信印象）：先查一个**你知道它在 `decomp_full/` 里有 `.c`** 的方法，
把打印出来的 VA 喂 `disasm_va.py`，**头几条应当是合法函数序言**（`push` / `sub rsp,??`）。
出现 `(bad)` 或乱操作数 = RVA 映射错了。

⚠️ 反汇编是**线性**的：遇到跳转不会跟过去，跨界后会出现垃圾指令，**以 `ret` 为界自己判断**。
⚠️ `script.json` 有 **127 MB** ⇒ 全量读一遍要几秒（本工具就是这么读的，够用）。
"""
import io
import re
import sys

# 🔴 **控制台是 GBK**（Windows 默认）⇒ 直接 print 中文/符号会 `UnicodeEncodeError` 崩掉，
#    或者打出乱码。本工具**自己把 stdout 切到 UTF-8**，不依赖调用方记得加 `-X utf8`。
## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n    PY=D:/2/Warpforge_tools/py312/python.exe\n    $PY d:/4/Unity/工具/rva_of_method.py <方法名片段> [最多几条=12] [script.json 路径]\n    # 名称里的 `<` `>` 在 script.json 里是 `\\u003C` `\\u003E` —— 本工具**两种写法都认**\n    # （直接写 `CollectionDisplay<object>$$GetItemCount` 也行）\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

try:
    sys.stdout.reconfigure(encoding='utf-8')
except Exception:
    pass

DEFAULT_JSON = 'd:/2/tools/il2cpp_out/script.json'
DISASM = 'd:/4/Unity/工具/disasm_va.py'
BASE_VA = 0x180000000


def encode_name(s):
    """把 `<>` 换成 script.json 里的 `\\u003C` `\\u003E` 写法；再转义正则元字符。"""
    return re.escape(s).replace('<', r'\\u003C').replace('>', r'\\u003E')


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    pat_text = sys.argv[1]
    limit = int(sys.argv[2]) if len(sys.argv) > 2 else 12
    path = sys.argv[3] if len(sys.argv) > 3 else DEFAULT_JSON

    txt = io.open(path, encoding='utf-8', errors='replace').read()
    pat = re.compile(r'\{\s*"Address":\s*(\d+),\s*"Name":\s*"(' + encode_name(pat_text) + r'[^"]*)"')
    hits = pat.findall(txt)
    if not hits:
        print('⛔ 没命中。已搜过：%s（关键词 %r）' % (path, pat_text))
        print('   ⚠️ 报「没有」之前先确认拼写 —— 类名里的泛型要写成 `Foo<Bar>`，')
        print('      方法名在 script.json 里的形状是 `类$$方法`（不是 `.`）。')
        return 1
    print('# %s  →  命中 %d 条（显示前 %d）' % (path, len(hits), min(limit, len(hits))))
    for addr, name in hits[:limit]:
        a = int(addr)
        print('Address=%-12d  RVA=0x%08X  VA=0x%09X  %s'
              % (a, a, BASE_VA + a, name.replace('\\u003C', '<').replace('\\u003E', '>')))
    print('\n下一步：%s D:/2/unity_run_ref/GameAssembly.dll <VA> <条数>' % DISASM)
    return 0


sys.exit(main())
