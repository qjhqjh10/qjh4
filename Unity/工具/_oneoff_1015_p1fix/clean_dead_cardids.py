# -*- coding: utf-8 -*-
"""P1 收尾 · ④-a：清掉 `card_ids.json` 里 **7 行已死的旧表残留**（`EC72`–`EC77` · `TL81`）。

判据（2026-10-15 现核，逐条给出）：
 ① 那 7 张卡在 P1 那批**已拿到真值 id**（GUID），本表里**同名的 GUID 行都在**
    （`5a8a11b2…`=Antrak Silk · `70199b6f…`=Heliotrophos · `6e6d1400…`=Quail · `92157999…`=Shivversplint ·
     `4eec3b13…`=Skorflense · `aa45079b…`=Xylocil · `468db99b…`=Feeding Frenzy）⇒ 桥没断。
 ② **零引用**：`数据/游戏数据/decklists.json`（原版 236 副）逐 id 现核 —— 7 个都不在
    （`cardIds` ∪ `heroId` 全集里没有）；`Unity/数据/` 整树 `grep` 也只有本表自己提到它们；
    原版卡组 SO（`d:/2/解包整理/01_卡牌/卡组数据/MonoBehaviour/`）`grep` **一个文件都没有**。
 ③ 留着**有害**：`gen_cards_engine.load_ids()` 会为「Antrak Silk」建出 `[('EC72',…),(GUID,…)]` 两条候选，
    而 `pick_id()` 只认「前缀+纯数字」⇒ 万一真值索引那一层失效，它会**静默退回 EC72**这个**已不是池内 id** 的旧值。
⚠️ 本文件是**纯 CRLF**（CRLF==LF==1322）—— 只做定点行删除，不整篇重排。
"""
import io, json, sys

P = 'd:/4/Unity/数据/游戏数据/card_ids.json'
b = io.open(P, 'rb').read()
txt = b.decode('utf-8')
assert txt.count('\r\n') == txt.count('\n'), '本文件本该是纯 CRLF，现核不符合，停手'

DEAD = ['EC72', 'EC73', 'EC74', 'EC75', 'EC76', 'EC77', 'TL81']

lines = {k: [ln for ln in txt.split('\r\n') if ln.strip().startswith('"%s":' % k)] for k in DEAD}
for k, v in lines.items():
    print('  %-5s -> %r' % (k, v))
    assert len(v) == 1, '行不唯一：' + k

for k in DEAD:
    ln = lines[k][0]
    tok = ln + '\r\n'
    assert txt.count(tok) == 1, '锚点不唯一：%s' % k
    txt = txt.replace(tok, '')

# 数字就地改（本表自己的账：1317 → 1310 行 · 旧表残留 123 → 116）
subs = [('旧表里真值没有的 123 条', '旧表里真值没有的 116 条'),
        ('1317 行（933 真值短 id + 259 真值 GUID + 123 旧表残留 + 2 个',
         '1310 行（933 真值短 id + 259 真值 GUID + 116 旧表残留 + 2 个')]
for a, c in subs:
    assert txt.count(a) == 1, '锚点不唯一/不存在：%r' % a
    txt = txt.replace(a, c)

TAIL = ('🆕 2026-10-15（P1 收尾笔 · 账 `A1783`/`A1780` 的收尾）：**删掉 7 行已死的旧表残留**'
        '（`EC72`–`EC77` · `TL81`）—— 那 7 张卡 P1 那批已拿到真值 GUID（本表同名的 GUID 行仍在 ⇒ 桥没断），'
        '而 `decklists.json`（236 副逐 id 现核）与原版卡组 SO **零引用**；'
        '留着的唯一后果是 `pick_id()` 在真值索引失效时**静默退回**这 7 个已非池内 id 的旧值。'
        '⇒ 本表 1317 → **1310** 行 · 旧表残留 123 → **116**（上面两处数字已就地改）。')
assert txt.count('"\r\n}') == 1, '找不到 note 结尾'
txt = txt.replace('"\r\n}', TAIL + '"\r\n}')

if '--write' not in sys.argv:
    print('DRY-RUN，未写盘')
    sys.exit(0)

io.open(P, 'wb').write(txt.encode('utf-8'))
b2 = io.open(P, 'rb').read()
j = json.loads(b2.decode('utf-8'))
print('WROTE %s' % P)
print('  mapping %d 行（原 1317）· CRLF=%d LF=%d' % (len(j['mapping']), b2.count(b'\r\n'), b2.count(b'\n')))
print('  7 个旧 id 还在吗：', {k: (k in j['mapping']) for k in DEAD})
