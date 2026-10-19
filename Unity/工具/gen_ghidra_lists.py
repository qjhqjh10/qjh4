# 从 Il2CppDumper 的 script.json 生成 Ghidra 要的三份清单（Renamer/Decomp 吃「十进制地址<TAB>名字」）
# 加 `--dry` = 只跑计算、打印读数、**零写盘**（默认行为不变；`A1648` 同族口径）。
import json, io, sys

# 🔴 `--dry` = 只跑计算、打印读数、**零写盘**。默认（不带它）行为与以前完全一样。
DRY = '--dry' in sys.argv[1:]


class _DrySink(object):
    """`--dry` 时的假文件：吃掉写入、不建文件。"""
    def __enter__(self):
        return self

    def __exit__(self, *a):
        return False

    def write(self, s):
        return len(s)


def _out(path):
    return _DrySink() if DRY else io.open(path, 'w', encoding='utf-8', newline='\n')

OUT = 'D:/2/tools/'
d = json.load(io.open(OUT + 'il2cpp_out/script.json', encoding='utf-8'))
methods = d['ScriptMethod']
strings = d['ScriptString']
print('ScriptMethod %d · ScriptString %d' % (len(methods), len(strings)))

with _out(OUT + 'all_methods.txt') as f:
    for m in methods:
        f.write('%d\t%s\n' % (m['Address'], m['Name']))

with _out(OUT + 'all_strings.txt') as f:
    for s in strings:
        v = s.get('Value', '')
        if not isinstance(v, str):
            continue
        f.write('%d\t%s\n' % (s['Address'], v.replace('\n', ' ').replace('\t', ' ')))

# 反编译名单：AI 类 + AnimFX 全族（+ 上一轮列的那 19 个类）
want = [l.strip() for l in io.open(OUT + 'classes_wanted.txt', encoding='utf-8').read().splitlines() if l.strip()]
sel = []
for m in methods:
    nm = m['Name']
    for c in want:
        if nm.startswith(c + '$$'):
            sel.append(m)
            break

with _out(OUT + 'wanted_methods.txt') as f:
    for m in sel:
        f.write('%d\t%s\n' % (m['Address'], m['Name']))

print('要反编译的方法 %d 个（类：%s）' % (len(sel), ', '.join(want[:4]) + ' …'))
if DRY:
    print('干跑（未写盘）：all_methods.txt / all_strings.txt / wanted_methods.txt；要落盘请去掉 --dry')
