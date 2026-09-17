# 从 Il2CppDumper 的 script.json 生成 Ghidra 要的三份清单（Renamer/Decomp 吃「十进制地址<TAB>名字」）
import json, io

OUT = 'D:/2/tools/'
d = json.load(io.open(OUT + 'il2cpp_out/script.json', encoding='utf-8'))
methods = d['ScriptMethod']
strings = d['ScriptString']
print('ScriptMethod %d · ScriptString %d' % (len(methods), len(strings)))

with io.open(OUT + 'all_methods.txt', 'w', encoding='utf-8', newline='\n') as f:
    for m in methods:
        f.write('%d\t%s\n' % (m['Address'], m['Name']))

with io.open(OUT + 'all_strings.txt', 'w', encoding='utf-8', newline='\n') as f:
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

with io.open(OUT + 'wanted_methods.txt', 'w', encoding='utf-8', newline='\n') as f:
    for m in sel:
        f.write('%d\t%s\n' % (m['Address'], m['Name']))

print('要反编译的方法 %d 个（类：%s）' % (len(sel), ', '.join(want[:4]) + ' …'))
