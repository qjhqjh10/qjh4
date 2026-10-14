# -*- coding: utf-8 -*-
"""给新加的 `TL40 Infestation Node` 补 `descZh`（卡面中文效果文字）。

落点 = `Unity/数据/游戏数据/cardface_fixes.json` 的 `descZh` 列 —— 本仓**手维护**的中文订正通道
（`_note` 写明「带 `_manual_` 前缀的说明段与 `desc` 列是手工维护的」；`gen_cards_engine.py:1218-1220`
把这一列**放在中文表之后**应用 ⇒ 一定生效；口径见 `MEMORY` 的「中文订正落 `cardface_fixes.json` 的 `descZh` 列」）。
⛔ 不写 `zh_cards.json`：那份 `note` 写着「不要手改」（是 `_tmp_zhcards_g*` 的合并产物）。

值 = **官方本地化原样**：`官方本地化_en+zh-CN.json` 的 `Card_Description/TL40[1]`
     （en 那一半同表 `[0]`，已落进卡池的 `desc`，见 `gen_cards_engine.STATS_EXCEPTIONS`）。
⚠️ 本文件是 **CRLF**（CRLF==LF==1904）—— 用 `wb` 写、只做定点替换，不整篇重排。
"""
import io, sys, json

P = 'd:/4/Unity/数据/游戏数据/cardface_fixes.json'
raw = io.open(P, 'rb').read()
txt = raw.decode('utf-8')
assert txt.count('\r\n') == txt.count('\n'), '本文件本该是纯 CRLF，现核不符合，停手'

anchor = '"descZh": {\r\n'
assert txt.count(anchor) == 1, 'anchor count = %d' % txt.count(anchor)

entry = ('  "Infestation Node": "对一支友方军队治疗 2 点生命值，并在你的手中创建它的一个副本。",\r\n')
assert 'Infestation Node' not in txt, '已经有这条了，别再插一次'

out = txt.replace(anchor, anchor + entry)

note_anchor = '"descZh": {'
# 记一条来源说明（与文件里既有的 `_日期_主题` 说明段同形）
note_key = '"_2026-10-15_TL40_InfestationNode_descZh",'
assert txt.count(note_key) == 0
note = (note_key + '\r\n'
        '  "2026-10-15（P1 收尾笔 · `A1783` 真新增的那一张）：`TL40 Infestation Node` 是**新进池**的卡，'
        '`zh_cards.json` 里它的 `d` 是空串 ⇒ 卡面中文效果本来会退成英文。'
        '这里补的**不是我方自译，是官方本地化原样**：'
        '`Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `Card_Description/TL40`（en/zh 两条）。'
        '⚠️ 英文 `desc` 那一半走的是 `gen_cards_engine.STATS_EXCEPTIONS`（`desc` 列本身没动）。",\r\n')
out = note + out

if '--write' not in sys.argv:
    print('DRY-RUN：将插入')
    print('  ', entry.strip())
    print('  ', note.strip()[:120], '...')
    sys.exit(0)

io.open(P, 'wb').write(out.encode('utf-8'))
print('WROTE', P)
d = json.load(io.open(P, encoding='utf-8'))
print('json 解析 OK · descZh 条数 =', len(d['descZh']))
print('  Infestation Node ->', d['descZh'].get('Infestation Node'))
b = io.open(P, 'rb').read()
print('行尾 CRLF=%d LF=%d' % (b.count(b'\r\n'), b.count(b'\n')))
