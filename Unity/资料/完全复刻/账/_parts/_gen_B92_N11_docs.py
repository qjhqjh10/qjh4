# -*- coding: utf-8 -*-
# B92_N11 · 铁律 5 就地订正：文档里「RankedDeckSelector.cs 已建 461 行」的过期措辞
# 原则：只在本行内追加/改写标记 —— 行数守恒、行尾不翻、原句保留（X 仍在，Y 写在标记里）
import io

E = '⚠️ 2026-10-28 更正：'
EDITS = {
 '报告/B88_E13_战役剩余.md': [
  ('**新建** `Shell/RankedDeckSelector.cs`。',
   '**新建** `Shell/RankedDeckSelector.cs`（' + E + '该文件已于 2026-10-28 删除 —— 运行期真身 = `Shell/LiveOpsEventWindow.cs` 的内联实现）。'),
  ('**12 个方法 + 3 个 lambda** 全部落地：',
   '**12 个方法 + 3 个 lambda** 全部落地（' + E + '该文件已于 2026-10-28 删除，以下内容按当时状态保留）：'),
 ],
 '报告/B88_B4_战役线.md': [
  ('是原版类名 ⇒ 无对应文件可改）。',
   '是原版类名 ⇒ 无对应文件可改）。' + E + '`Shell/RankedDeckSelector.cs` 已于 2026-10-28 删除 —— 运行期真身 = `Shell/LiveOpsEventWindow.cs` 的内联实现。'),
  ('（421 行、**零调用方**）。现核：',
   '（421 行、**零调用方**；' + E + '该文件已于 2026-10-28 删除）。现核：'),
  ('**删**（首选，避免两份语义漂移），',
   '**删**（首选，避免两份语义漂移；' + E + '已按首选执行，文件于 2026-10-28 删除），'),
  ('## 二、唯一那处代码改动（`Shell/RankedDeckSelector.cs`）',
   '## 二、唯一那处代码改动（`Shell/RankedDeckSelector.cs`；' + E + '该文件已删除，本节按当时状态保留）'),
  ('删（首选）或接线（接线点见本报告开头）；',
   '删（首选）或接线（接线点见本报告开头；' + E + '**已删**，运行期真身 = `Shell/LiveOpsEventWindow.cs` 的内联实现）；'),
  ('- `Shell/RankedDeckSelector.cs`：461 行 · `CRLF 0` · `bare CR 0`。',
   '- `Shell/RankedDeckSelector.cs`：461 行 · `CRLF 0` · `bare CR 0`。' + E + '该文件已于 2026-10-28 删除，此快照仅存档。'),
  ('本轮唯一动过的 `.cs` 是**零调用方**的 `RankedDeckSelector.cs`（无断言面）⇒',
   '本轮唯一动过的 `.cs` 是**零调用方**的 `RankedDeckSelector.cs`（无断言面；' + E + '该文件已于 2026-10-28 删除）⇒'),
 ],
 '报告/B89_R1c_战役轮抽锻造.md': [
  ('+ `Shell/RankedDeckSelector.cs`（E13 建的 421 行、**零调用方**）。',
   '+ `Shell/RankedDeckSelector.cs`（E13 建的 421 行、**零调用方**；' + E + '已于 2026-10-28 删除，运行期真身 = `Shell/LiveOpsEventWindow.cs` 的内联实现）。'),
 ],
 '报告/B91_L6.md': [
  ('⚠️ `Shell/RankedDeckSelector.cs` 那套组件**零接线**（⛔ 别把「已建」当「已接」） |',
   '⚠️ `Shell/RankedDeckSelector.cs` 那套组件**零接线**（⛔ 别把「已建」当「已接」；' + E + '该文件已于 2026-10-28 删除，真身 = `Shell/LiveOpsEventWindow.cs` 的内联实现） |'),
  ('⛔ 别把「已建」当「已接」（任务 §五 已点名，现读证实）。',
   '⛔ 别把「已建」当「已接」（任务 §五 已点名，现读证实）。' + E + '该文件已于 2026-10-28 删除（真身 = 上句那份内联实现）。'),
 ],
 '报告/B92_N9.md': [
  ('`Shell/RankedDeckSelector.cs`（461 行）**零调用方**，而**运行期真正在用的是 `Shell/LiveOpsEventWindow.cs` 的内联实现**',
   '`Shell/RankedDeckSelector.cs`（461 行）**零调用方**，而**运行期真正在用的是 `Shell/LiveOpsEventWindow.cs` 的内联实现**（' + E + '该文件已于 2026-10-28 删除，真身就是后半句那份内联实现）'),
 ],
 '判据/B90_波2_文件所有权与派活表.md': [
  ('`MainMenuWindow.cs` `RankedDeckSelector.cs` ＋新建 `TwoSidesTeamDrawer.cs`',
   '`MainMenuWindow.cs` `RankedDeckSelector.cs`（' + E + '该文件已于 2026-10-28 删除）＋新建 `TwoSidesTeamDrawer.cs`'),
 ],
}

for p, pairs in EDITS.items():
    b = io.open(p, 'rb').read()
    assert b.count(b'\r') == 0, (p, '有 CR，先停下')
    t = b.decode('utf-8')
    before_lines = t.count('\n')
    for old, new in pairs:
        n = t.count(old)
        assert n == 1, (p, old[:40], '命中', n)
        t = t.replace(old, new)
    out = t.encode('utf-8')
    assert out.count(b'\r') == 0, (p, '写回引入 CR')
    assert out.count(b'\n') == before_lines, (p, '行数变了', before_lines, out.count(b'\n'))
    io.open(p, 'wb').write(out)
    print('OK', p, '| 行数守恒', before_lines, '| 替换', len(pairs), '处')
