# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · E1/E2 后的落账：
 ① 订正 W16 报告里「基线没有重生成、三个指纹没有重钉」那句（现已过期，第三十会话现核）
 ② 新账 A1993（SOR29 英中数字不一致、引擎按英文结算）
 ③ A1978 追加（_a1759_report.py 的 hasActiveAbility=None 没修）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_e12.py
"""
import io

W16 = r'D:\4\Unity\资料\普查产出_第二十八会话\W16_对账口仪器与旧id残留.md'
MAIN = r'D:\4\项目任务.md'

W16_OLD = "### 🔴 基线：**没有重生成、三个指纹没有重钉 —— 这是有意的**（`_tmp_view/w16_attr.py` 逐行归因）"
W16_NEW = W16_OLD + "\n" + (
    ">\n"
    "> ✅ **2026-10-14 就地订正（铁律 5）：本节说的状态【已经过去了】** —— 下面第 56 行那三步，**第二十九会话已经做完**"
    "（`工具/ruleprobe/Program.cs:153` 的注释块写着「🔴 **2026-10-14（第二十九会话 · `A1908`）有意重钉**」）。\n"
    "> **2026-10-14 第三十会话现核**（判据 = 2026-10-14 现跑，⛔ 不是转述）：\n"
    "> · `工具/ruleprobe/基线/out_baseline.txt` 现读 md5 = **`6c0e41d71599bd7a42f078a50e90f614`** · mtime **10-14 18:06:54**"
    "（晚于 `_tmp_view/w15_scan.txt` 的 14:20）⇒ **已重生成**，且**已是 `Talent:` 修复之后**的口径。\n"
    "> · `bash d:/4/Unity/工具/ruleprobe.sh check` 现跑 **rc=0**：`=== 哨兵合计：失败 0 条 ===`，"
    "`[基线指纹] 卡池与基线的指纹都与钉住的一致（基线没被静默改写）` · `[输出指纹] 当次现算的解析输出与被确认的那一版一致（e3a4b082…）` "
    "⇒ **三个指纹都已重钉**。\n"
    "> ⇒ 🔴 **本节那句「没有重生成、三个指纹没有重钉」现在只【在历史意义下】成立**；⛔ **别再照它当现状**。"
    "**第三十会话的仪器块 `E3`（「把 `SCAN` 指回仓内 + 重跑 `scan`」）因此是【空账】** —— 只剩「指回仓内」那半有效（已由 `A1977` 的写手做完）。"
)

A1993 = (
    "| 🆕 **`A1993`** | 🔴 **`SOR29 Ephrael Stern` 的英文 `desc` 与中文 `descZh` 数字不一致 —— 而引擎按【英文】结算**"
    "（2026-10-14 `E1` 补区间读数时查出，**是真缺陷、不是仪器假账**）：**原版真值 = `dmg 2-3`**；"
    "我们 **英文 `desc` = `Deal 3 damage`**、**中文 `descZh` = `造成 2-3 点伤害`** ⇒ **`descZh` 与原版一致、`desc` 不一致**；"
    "而引擎**打的是 `desc`**（`BattleDriver.FaceTextFull` 只拿中文**显示**）⇒ **玩家在中文卡面上看到 2-3、实际吃 3 点**。\n"
    "⇒ **要做**（铁律 11）：**先按铁律 7 亲读 PnP 成品卡图**（`d:/2/Warpforge部队卡片/`）定「到底是 2-3 还是 3」，**再**按结论改 `desc`（或 `descZh`）—— "
    "⛔ **别只改一边**（改错那一边等于把「显示」与「结算」拆得更远）。\n"
    "⚠️ 这是 `E1` 那 **+2** 里**唯一一条真缺陷**（另一条 `GOF9` 属 `A1876` 那类「2022 真值快照 vs 现行印面」，**不改**）。"
    "判据 = `资料/普查产出_第三十会话/A1977_对账_E1E2_1014.tsv` 对应那一行 + 成品卡图。 |"
)

A1978_SEG = (
    " ⚠️ **2026-10-14 追加一处同族残留**：`工具/_a1759_report.py:26-28` 对 `activeAbility` **仍用 `aa.get(...)` 判空** ⇒ "
    "`A1978`①②记的那 3 张 `hasActiveAbility = None`（**静默按 0 走**）在这台汇总器里**没修**"
    "（`E1E2` 那一趟只改了它的 TSV 路径）。⇒ 与 `A1978` 本体一起收。"
)


def main():
    b = io.open(W16, 'rb').read()
    assert b.count(b'\r\n') == 0, 'W16 must be LF'
    t = b.decode('utf-8')
    assert t.count(W16_OLD) == 1
    io.open(W16, 'wb').write(t.replace(W16_OLD, W16_NEW).encode('utf-8'))
    print('OK W16 订正  %d -> %d' % (len(b), len(t.replace(W16_OLD, W16_NEW).encode('utf-8'))))

    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')

    def block_end(i):
        j = i
        while j < len(lines) and not lines[j].rstrip().endswith(' |'):
            j += 1
        assert j < len(lines)
        return j

    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1978`**')]
    assert len(hits) == 1
    j = block_end(hits[0])
    lines[j] = lines[j].rstrip()[:-1].rstrip() + A1978_SEG + ' |'

    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1992`**')]
    assert len(hits) == 1
    j = block_end(hits[0])
    lines[j + 1:j + 1] = [A1993]

    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('OK MAIN  A1978 追加 + A1993 已立')


if __name__ == '__main__':
    main()
