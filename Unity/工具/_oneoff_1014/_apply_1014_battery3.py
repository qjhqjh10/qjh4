# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 补建 A1999/A2000/A2001（设计文件里点了、正本里没建）+ A1965 指向设计文件
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_battery3.py
"""
import io

MAIN = r'D:\4\项目任务.md'
TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
DOC = '`资料/普查产出_第三十会话/A1965_电池步设计.md`'

A1999 = (
    "| 🆕 **`A1999`** | ⚠️ **对账那台「按根因分格」的 `EcCause` 现在【有两格是空的】⇒ `A1965` 改完之后「电池抬不到那个事件」会变成【唯一】一格，"
    "①-c 那张表会彻底失去鉴别力**（2026-10-14 `A1965` 设计查证顺手查出；⚠️ `工具/ruleprobe/PlayProbe.cs:649-664` **自己已自述**那两格空着）"
    "⇒ **不该等它烂掉再修**。⇒ **要做**：把 `EcCause` 改成按 **`TriggerTexts` / `WhenTriggers` 的头名**分格（**引擎公开面就有**，⛔ 不用另造一套）。"
    "🔴 **建议与 `A1965` 同批做** —— 否则「改了电池 = 没改量具」。判据原文 → " + DOC + " §七·1。 |"
)

A2000 = (
    "| 🆕 **`A2000`** | 🔴 **`TL16` 疑似真缺陷（未定）**（2026-10-14 `A1965` 设计查证顺手查出）：卡面 `Strike: Return this card to your deck and reduce its cost by 3` "
    "—— 两发攻击**都 `rc=0`**（`atkM=0, atkR2=0`），而**第二发能进 `steps`** 说明「打出后这一格还有单位」⇒ **「回牌库」没生效**；"
    "且 **`raw == ctrl == 26`**（⇒ **不是「事件没到」，是「到了却没做」**）。⇒ **要做**：先钉死是不是真缺陷"
    "（判据只到这一步 —— 写手**没有**跑探针，未定）。⚠️ 与 `A2001` 同族（都是「够到了却没响」）。判据原文 → " + DOC + " §七·2。 |"
)

A2001 = (
    "| 🆕 **`A2001`** | 🔴 **【与 `A1965` 是两件事】桶 C 那 27 张里，≈10 张是「事件【够到了】却没响」**（2026-10-14 `A1965` 设计查证顺手查出）："
    "`GOF18`（`Rally: Heal 3`）· `TAU67`（`Rally: Deploy a Kroot Hound`）· `DA5`（`Rally: Heal 2`）· `EC17`/`SOR70`/`TAU10`"
    "（`At the end of your turn…`，而**电池确实跑了两个来回**）· `BL32`/`UM2`/`GSC35`/`SW22` —— 这 10 张 **`raw == ctrl`** "
    "⇒ **事件本该被驱动、读数却一条不差**。🔴 **`A1965` 治的是「电池够不到」，本笔治的是「够到了却没响」—— ⛔ 别混成一笔**。"
    "**可能方向（都没定、⛔ 不许当结论）**：目标解析需要显式 `chosen` 而探针没给 ⇒ 静默 no-op；或 `atturn` 信封没接消费点"
    "（`RuleEngine/Core/EffectText.cs:4137` 是唯一的 `Verb=\"atturn\"` 写入点）。⇒ **要做**：先钉根因（写手**没跑探针**，未下结论）。判据原文 → " + DOC + " §六。 |"
)

A1965_SEG = (
    " 🔴 **2026-10-14 动手前的施工图已落**（只读设计查证 + 主对话采用）→ **" + DOC + "**。要点：\n"
    "· **现状 6 步**（`RunEventPass :759-858`）＋ **实测基线**（`out_play_baseline.txt` 的 `steps` 列**一字不差**）："
    "`play=0, oath=14, end0=0, end1=0, draw=1, abil=14, atkM|atkR=0, atkR2|atkM2=0, kill(dmg)=999` —— **`14 = ErrNoAbility`** ⇒ **①④ 对这批一张都没生效**；两发攻击**都 rc=0**。\n"
    "· **154 张的类型分布**（Codex **25** / ThisCardPlayed **15** / Artifice **14** / Uprising **7** / MarkOfChaos 6 / Slay·Teleport·UnitAttack 各 5 …）＋ **逐类「再加一步什么」＋ 反编译判据 ＋ 我们侧落点**。\n"
    "· **步子设计**：主电池**多 6 步**（`selfChip` · `chip2` · `tactic` · `enemyTactic` · `troop2` · `friendDies` · `collect`）＋ **3 个夹具变体**（`Energy=card.Cost` / 牌库抽到再打出 / 只留被测卡），四遍 `net` **取并集**；**期望翻转 ≈80 张**。\n"
    "· **风险**：`E|` 段 **586 行全变** ⇒ 必须**重生成 ＋ 重钉** `PlayProbe.PinnedPlayMd5`；**RNG 分叉是唯一真风险**（两局手牌数会错开）。\n"
    "· 🔴 **验收判据（前两条可机器断言）**：① **`net > 0 ⇒ raw != ctrl`**（`net` 是集合差、`b` 是只清 `desc` 的替身 ⇒ 只有卡自己正文跑了才会冒 net）；"
    "② **逐张人核 `netFull`**（每条变化必须指回它自己的卡面）；③ 灭自证（`EventBatterySelfCheck` 扩到每个新步骤）；④ **反例哨兵**（故意挑 3 张不接的、必须仍 = 0）；"
    "⑤ ⛔ **不许只看「0 的条数变少了」**（①-c 要同时打「转正几张 / 新增几张 crash / `raw==ctrl` 却 `net>0` 几张」三个数）。\n"
    "⚠️ **同批三笔新账**：`A1999`（`EcCause` 分格失效，**建议同批**）· `A2000`（`TL16` 疑似真缺陷）· `A2001`（**10 张「够到了却没响」—— 与本笔不是一件事**）。"
)


def append_seg(path, head_prefix, seg):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith(head_prefix)]
    assert len(hits) == 1, (path, head_prefix, len(hits))
    j = hits[0]
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    assert j < len(lines)
    lines[j] = lines[j].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(path, 'wb').write(out.encode('utf-8'))
    print('  +%s (尾行 L%d)' % (head_prefix, j + 1))


def main():
    append_seg(TASKS, '| 🆕 **`A1965`**', A1965_SEG)
    b = io.open(MAIN, 'rb').read()
    lines = b.decode('utf-8').split('\n')
    assert not [l for l in lines if l.startswith('| 🆕 **`A1999`**')], 'A1999 行已存在'
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1998`**')]
    assert len(hits) == 1, hits
    j = hits[0]
    while not lines[j].rstrip().endswith(' |'):
        j += 1
    lines[j + 1:j + 1] = ['\n'.join([A1999, A2000, A2001])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1999/A2000/A2001')


if __name__ == '__main__':
    main()
