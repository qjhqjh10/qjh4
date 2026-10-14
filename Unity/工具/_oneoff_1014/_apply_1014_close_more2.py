# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 核销：把 6 条【已完成/已判非缺陷】的账整行搬进收口档，并给 A1987 的残余立新账 A2005。
搬走的是：A650(§29·c) · A1964/A1597(任务文件) · A1993/A1969/A1996(§29·b)
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_close_more2.py
"""
import io

MAIN = r'D:\4\项目任务.md'
TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
ARC = r'D:\4\Unity\资料\历史\A表已收口_第三十会话.md'

# (文件, 行首特征, 备注)
MOVE = [
    (MAIN,  '| ✅✅ ~~`A650`~~', 'A650 结案：原版自己就没有（反编译零引用）'),
    (TASKS, '| ✅ **`A1964`**',  'A1964 已办并已验证（断言落 BattleScene.cs 的 20-b 节 + 自检 2896/0）'),
    (TASKS, '| 🆕 **`A1597`**',  'A1597 可核销（62 处夹具逐处读数完成、无一处把请求格当落点）'),
    (MAIN,  '| 🆕 **`A1993`**',  'A1993 已办（SOR29 的 desc 改成 Deal 2-3 damage）'),
    (MAIN,  '| 🆕 **`A1969`**',  'A1969 不是缺陷（人造分叉负例，故意报出来的）'),
    (MAIN,  '| 🆕 **`A1996`**',  'A1996 不是缺陷（原版这一列本来就是常量 20，我们与它逐字一致）'),
]

A2005 = (
    "| 🆕 **`A2005`** | ⚠️ **`A1987` 那批的【残余三条】**（2026-10-14 数据侧写手 + 主对话裁决后拆出；`A1987` 本体已核销）：\n"
    "① **`AM15 Minka Lesk` 也缺 `Talent:` 那一行**（同型同源）—— 现读 `desc = Friendly Infantry costs 1 less.`（`descZh` 早有 `天赋：卡迪亚荣誉`），"
    "而转录表 `Unity/资料/卡表核对_卡图提取/_合并总表.md:124` = `Friendly Infantry costs 1 less. 〔书〕Talent: Cadian Honour` ⇒ **英文 `desc` 缺 `Talent: Cadian Honour`**；"
    "② **`DA_Azrael Azrael`** —— 现读 `desc` 已含**裸名** `… Supreme Grand Master`（**无 `Talent:` 前缀**），转录表 `:266` 是 `〔书〕Talent: Supreme Grand Master` ⇒ **要补前缀**；\n"
    "③ 🔴 **本会话刚写进去的 2 条中文名要【反过来对齐】**（按 `A2002` 的裁决 = 统一到官方表）：\n"
    "　· `GOF18 PainBoy Sniklaw`：`descZh` 现写 `天赋：锯骨大夫` → 应改官方 **`锯骨者`**；\n"
    "　· `BL34 Abaddon`：`descZh` 现写 `天赋：四神宠儿` → 应改官方 **`四者之选`**。\n"
    "⚠️ **同批要长个心眼**：`_2026-09-15_A7天赋名` 那张 5 张名单（战斗圣歌/至尊以太/进食狂潮/**卡迪亚荣誉**/**至高大师**）**只接了 3 张** ⇒ 本账的 ①② 就是漏的那两张。"
    "⚠️ 改完 `desc` 会动解析器 ⇒ **与 `A1763` 那批一起排**（⛔ 别单独改，否则要再动一次 `PlayProbe.PinnedPlayMd5`）。判据原文 → `资料/普查产出_第三十会话/`。 |"
)


def block_range(lines, i):
    j = i
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    assert j < len(lines), '没有行尾'
    return i, j


def main():
    rows = []
    for path, head, note in MOVE:
        b = io.open(path, 'rb').read()
        assert b.count(b'\r\n') == 0, path
        lines = b.decode('utf-8').split('\n')
        hits = [i for i, l in enumerate(lines) if l.startswith(head)]
        assert len(hits) == 1, (path, head, len(hits))
        i, j = block_range(lines, hits[0])
        rows.append((path, i, j, '\n'.join(lines[i:j + 1]), head, note))
        print('  找到 %s (%s L%d-%d)' % (head.split('`')[1] if '`' in head else head, path.split('\\')[-1], i + 1, j + 1))

    # 建 A2005（紧随 A2004）
    b = io.open(MAIN, 'rb').read()
    lines = b.decode('utf-8').split('\n')
    if '**`A2005`**' not in b.decode('utf-8'):
        h = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A2004`**')]
        assert len(h) == 1
        _, j = block_range(lines, h[0])
        lines[j + 1:j + 1] = [A2005]
        io.open(MAIN, 'wb').write('\n'.join(lines).encode('utf-8'))
        print('  +A2005')

    # 按 (path, i) 从后往前删
    byp = {}
    for path, i, j, _, _, _ in rows:
        byp.setdefault(path, []).append((i, j))
    for path, spans in byp.items():
        b = io.open(path, 'rb').read()
        assert b.count(b'\r\n') == 0, path
        lines = b.decode('utf-8').split('\n')
        for i, j in sorted(spans, reverse=True):
            del lines[i:j + 1]
        out = '\n'.join(lines)
        assert '\x00' not in out and '\r\n' not in out
        io.open(path, 'wb').write(out.encode('utf-8'))
        print('  从 %s 删了 %d 段' % (path.split('\\')[-1], len(spans)))

    # 追加进收口档
    a = io.open(ARC, 'rb').read()
    assert a.count(b'\r\n') == 0
    at = a.decode('utf-8')
    add = ('\n> ✅ **2026-10-14（第三十会话）再收 6 条**（本会话后半段办完/判非缺陷的）：'
           + ' · '.join('`%s`' % r[4].split('`')[1] if '`' in r[4] else r[4] for r in rows) + '\n')
    lines2 = at.split('\n')
    sep = [i for i, l in enumerate(lines2) if l.strip() == '|---|']
    assert len(sep) == 1
    k = sep[0] + 1
    body = '\n'.join(r[3] for r in sorted(rows, key=lambda x: (x[0], x[1])))
    lines2[k:k] = [body]
    at2 = '\n'.join(lines2)
    # 在表头补一句说明
    at2 = at2.replace('> ⬇️ 下面 **11 行全部来自**', '> ⬇️ 下面 **17 行全部来自**')
    if '再收 6 条' not in at2:
        at2 = at2.replace('| 编号（原行原文） |', add + '\n| 编号（原行原文） |')
    io.open(ARC, 'wb').write(at2.encode('utf-8'))
    print('  收口档已追加 %d 行' % len(rows))


if __name__ == '__main__':
    main()
