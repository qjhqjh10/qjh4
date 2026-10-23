# -*- coding: utf-8 -*-
"""修上一步的落点错误：说明段被插到了 JSON 对象的**外面**（文件开头），解析报 Extra data。
本脚本把它挪进根对象里（`{` 之后第一对键值），其余一字不动。"""
import io, json, sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/_oneoff_1015_p1fix/fix_descZh_placement.py [--write]\n  说明：修上一步的落点错误：说明段被插到了 JSON 对象的**外面**（文件开头），解析报 Extra data。\n  选项：--write（脚本内按字符串匹配；一个都不带 = 默认模式）')
    _wf_help_sys.exit(0)

P = 'd:/4/Unity/数据/游戏数据/cardface_fixes.json'
b = io.open(P, 'rb').read()

head = b'"_2026-10-15_TL40_InfestationNode_descZh"'
assert b.startswith(b'"'), '文件不以引号开头，结构不是预期的样子，停手'

i = b.find(b'{\r\n "_note"')
assert i > 0, '找不到根对象的开头，停手'

prefix = b[:i].decode('utf-8')
rest = b[i:]
assert rest.startswith(b'{\r\n'), repr(rest[:6])

lines = [x for x in prefix.split('\r\n') if x.strip()]
assert len(lines) == 2, '说明段不是两行：%r' % lines
key = lines[0].rstrip(',').strip()
val = lines[1].strip().rstrip(',')
assert key.encode('utf-8') == head, key
assert '\r' not in val and '\n' not in val

newpair = (' ' + key + ': ' + val + ',\r\n').encode('utf-8')
out = b'{\r\n' + newpair + rest[3:]

if '--write' not in sys.argv:
    print('DRY-RUN：新开头 ='); print(repr(out[:140])); sys.exit(0)

io.open(P, 'wb').write(out)
b2 = io.open(P, 'rb').read()
d = json.loads(b2.decode('utf-8'))
print('WROTE %s' % P)
print('  json OK · 顶层键 %d' % len(d))
print('  行尾 CRLF=%d LF=%d' % (b2.count(b'\r\n'), b2.count(b'\n')))
print('  descZh["Infestation Node"] =', d['descZh'].get('Infestation Node'))
print('  descZh 条数 =', len(d['descZh']))
