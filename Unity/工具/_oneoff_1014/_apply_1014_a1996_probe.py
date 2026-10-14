# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · A1996 现核结果（主对话亲自核的，比查证代理报的更进一层）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_a1996_probe.py
"""
import io

MAIN = r'D:\4\项目任务.md'

SEG = (
    " 🔴🔴 **2026-10-14 主对话现核 —— 比上面更进一层：那一列在【判据上已经失效】**：\n"
    "· 现读 `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（1126 张）的 `initiative` 分布 = **`{20: 955, -1: 171}`** —— "
    "**955 张全部是 `20`**、171 张是「真值里查不到这张卡」。\n"
    "· 现读 `数据/游戏数据/card_initiative.json` 的元数据，它**自己就记着** `counts = {pool: 1126, mapped: 955, unknown: 171, byValue: {-1: 171, 20: 955}}`，"
    "`invariant ②` 写着「`-1` = **真值里查不到这张卡**（快照比现池旧），⛔ 别当 `0`(Undefined)」；`provenance` 指向真值导出。\n"
    "· ⇒ 🔴 **后果**：用它判先后手的 `FirstSeatRules.ByInitiative` 会因为**所有卡同值而永远平手**，**必然退到下一档**"
    "（`card_initiative.json` 的 `invariant ③` 自己写着「缺值(`-1`)不比 ⇒ 退回原版 ③④⑤，即掷硬币」）"
    "⇒ **这一层规则实际上是空的**（不是「值抄错了」，是「**整列没有区分度**」）。\n"
    "· 🔴 **强烈指向「快照未填」**：`Initiative` 枚举有 **5 档**（`Undefined 0 / Low 10 / Medium 20 / High 30 / VeryHigh 40`）—— "
    "**若全都是 Medium，那另外 4 档的存在就没有意义**。⚠️ **但这是推断、不是判据**，**未证**。\n"
    "· ⚠️ **⛔ 别急着改**：现在**没有任何判据**能说「每张该是几」（本地只有这一份真值）。**要做**的**不是**改值，而是**先查「原版这一列到底该不该逐卡不同」** —— "
    "入口 = ① 反编译里 `RawCardScript.initiative(+0x??)` 的**读写点**（谁设、谁读、`FirstSeat` 那一族怎么用它）；"
    "② 找**第二份来源**做交叉（卡图 / 别的导出 / 官方文案）；③ 若两者都没有 ⇒ 按**铁律 11 例外①**「原版判据为空」如实记，⛔ 不许自己编一套值。"
)

def main():
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1996`**')]
    assert len(hits) == 1, hits
    j = hits[0]
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    lines[j] = lines[j].rstrip()[:-1].rstrip() + SEG + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('OK A1996 现核结果已补（尾行 L%d）' % (j + 1))


if __name__ == '__main__':
    main()
