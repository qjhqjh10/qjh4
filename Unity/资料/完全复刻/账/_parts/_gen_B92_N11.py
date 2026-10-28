# -*- coding: utf-8 -*-
# B92_N11：把 战役.tsv 里 5 条落点有缺陷的 RankedDeckSelector 行做「落点规范化」(判据/B92_N7_落点规范化_范本.md)
# 只改第 6 列(我们的落点) / 第 7 列(差异)，其余列逐字照抄。行尾 CRLF + UTF-8 BOM（补丁格式）。
import io, os

SRC = r'd:/4/Unity/资料/完全复刻/账/战役.tsv'
DST = r'd:/4/Unity/资料/完全复刻/账/_parts/B92_N11.tsv'

raw = io.open(SRC, 'rb').read()
text = raw.decode('utf-8-sig')
assert b'\r' not in raw, '主表为何有 CR？'
lines = text.split('\n')
if lines and lines[-1] == '':
    lines = lines[:-1]
assert len(lines) == 127, len(lines)
assert lines[0].split('\t') == ['域','原版类','方法','判据出处','判据要点','状态','我们的落点','差异','等级','轮次','备注']

NEW_LANDING = {
108: 'CardPresentation/Shell/LiveOpsEventWindow.cs:350（InArmyDeckList）· 等价实现 = 合法优先排序 `FirstDeckInArmy` / 合法性 `DeckIsLegal` / 套数 `ArmyDeckCount` / 序号 `ShownDeckOrdinal`（同文件，散文不给行号）',
109: 'CardPresentation/Shell/LiveOpsEventWindow.cs:829（BuildDeckTexts）· 无卡组二分支 = `OnNoDeckButtonClick`、文案两档 = `NoDeckMessage`（同文件，散文不给行号）',
111: 'CardPresentation/Shell/LiveOpsEventWindow.cs:2098（OnCreateDeckButtonClick）· 空态钮的委托挂在 `BuildDeckButtons` 里（同文件，散文不给行号）',
119: 'CardPresentation/Shell/LiveOpsEventWindow.cs:829（BuildDeckTexts）· 等价实现 = `ShownWarlord`（同文件属性）的取值：预组取 `CollectionData.Card(pre.heroId)`、自建取 `CollectionData.Warlord(DeckIndex)` —— 我们 `PlayerDeck.WarlordId` **就是**那张督军卡，不需要在卡表里找',
121: 'CardPresentation/Shell/LiveOpsEventWindow.cs:1887（OpenDeckSelection）· 谓词 = 该调用 `DeckSelectionPopup.Create` 的第 4 个实参（同文件，散文不给行号）',
}

DIFF_PATCH = {
108: ' ｜[B92_N11 2026-10-28] 落点规范化（判据/B92_N7_落点规范化_范本.md）：一个落点列只留一个 `文件:行号`，其余同名方法降为散文（不带行号）。本行落点自 B88_B4 起就是运行期真身 `Shell/LiveOpsEventWindow.cs`；`Shell/RankedDeckSelector.cs`（B88_E13 建的重复实现、零调用方）本轮删除，本行不受影响。',
109: ' ｜[B92_N11 2026-10-28] 落点规范化：旧格里的 `:2085` / `:433` 已漂（现读 `OnNoDeckButtonClick` = :2088 · `NoDeckMessage` = :437）⇒ 按 N7 范本只留一个 `文件:行号`、其余改为方法名散文。',
111: ' ｜[B92_N11 2026-10-28] 落点规范化：落点列原写「钮 :903」（裸行号、已漂到 :906）⇒ 降为散文（那颗空态钮的委托在 `BuildDeckButtons` 里）。',
119: ' ｜[B92_N11 2026-10-28] 落点规范化：散文里的裸行号 `ShownWarlord(:631)` 已漂（现读 :635）⇒ 去掉行号只留属性名。',
121: ' ｜[B92_N11 2026-10-28] 就地上订正（铁律 5）：B90-N6 那句「落点 `Shell/RankedDeckSelector.cs`」已过期 —— 落点自 B88_B4 起就是 `Shell/LiveOpsEventWindow.cs`；`Shell/RankedDeckSelector.cs`（重复实现、零调用方）本轮删除。另：落点列里的裸行号 `:1920` 降为散文（谓词 = 该调用第 4 个实参）。',
}

# row 111 的差异正文里有一句把【我们那份重复实现】当"先例"，删文件后会悬空 —— 就地换成本类在用的先例
OLD_111 = '，先例 = `Shell/RankedDeckSelector.cs:317`'
NEW_111 = '，同族先例 = 本类 `OnGoToStoreButtonClick` 的 `OpenByRef`；⚠️ 原记的 `Shell/RankedDeckSelector.cs` 重复实现已于 2026-10-28 删除'

out_rows = [lines[0]]
for n in sorted(NEW_LANDING):
    c = lines[n-1].split('\t')
    assert len(c) == 11, (n, len(c))
    assert c[1] == 'RankedDeckSelector' and c[2].endswith(')'), (n, c[1], c[2])
    old_landing = c[6]
    c[6] = NEW_LANDING[n]
    if n == 111:
        assert OLD_111 in c[7], 'row111 差异原文变了？'
        c[7] = c[7].replace(OLD_111, NEW_111)
    c[7] = c[7] + DIFF_PATCH[n]
    assert len(c) == 11
    out_rows.append('\t'.join(c))
    print('row', n, '|', c[2])
    print('  旧落点:', old_landing[:100])
    print('  新落点:', c[6][:100])

data = ('\r\n'.join(out_rows) + '\r\n').encode('utf-8-sig')
io.open(DST, 'wb').write(data)

# 回读校验（读->写->再读，11 列全等）
chk = io.open(DST, 'rb').read()
assert chk.startswith(b'\xef\xbb\xbf'), 'BOM 丢了'
assert chk.count(b'\r\n') == len(out_rows) and chk.count(b'\n') == len(out_rows), '行尾不是纯 CRLF'
back = chk.decode('utf-8-sig').split('\r\n')
if back and back[-1] == '':
    back = back[:-1]
assert len(back) == len(out_rows)
for i, (a, b2) in enumerate(zip(out_rows, back)):
    assert a.split('\t') == b2.split('\t'), ('列不一致', i)
print('OK 写出', DST, len(out_rows), '行（含表头）')
