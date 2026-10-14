# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · A2002 裁决落盘（统一到官方表一侧）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_a2002.py
"""
import io

MAIN = r'D:\4\项目任务.md'

SEG = (
    " ✅✅ **2026-10-14 证据到手 ⇒ 【主对话已裁：统一到【官方表】一侧】—— 即保留天赋卡 `nameZh` 不动、改 `descZh` 里那 56 处天赋名**：\n"
    "① 🔴 **判据层级不同，不是口味之争**：天赋卡 `nameZh` 是**原版官方表产物**（`工具/gen_cards_engine.py:811-880` 两级查 ＋ `:1123` 最后一步覆盖，`zh_cards.json` 降为兜底）；"
    "而 `descZh` 那半的来源（`数据/卡牌翻译/zh_cards.json` 的 `d` / `数据/本地化/i18n/zh_CN.csv`）**是我们自译**（脚本头 `:36-58` **明写「这份 csv 不是官方产物」**）。\n"
    "② **实测「以天赋卡为准」在 75/85 上【逐字等于】「以官方为准」**：75 个官方命中的天赋名里，我方池内同名天赋卡 **71/71 的 `nameZh` 与官方 zh-CN 逐字相同（不同 0）**。"
    "样本：`Wrath of Khaine` 官方「凯恩的愤怒」↔ 我们 `descZh`「凯恩之怒」· `Storm of Silence` 官方「寂灭风暴」↔「寂静风暴」· `Vox-Hacker` 官方「黑客」↔「声讯黑客」· `Sawbonez` 官方「锯骨者」↔「锯骨大夫」。\n"
    "③ 🔴 **两套名字【会同屏】⇒ 必须统一**：卡面正文读 `descZh`（`CardPresentation/Battle/BattleDriver.cs:12553 FaceTextFull → :12565`），"
    "而天赋卡自己的标题读 `nameZh`（同文件 `:12507`）；天赋卡由 `RuleEngine/Core/RuleCore.cs:8108 SpawnTalents → :8128-8133` **每回合塞进你手牌** "
    "⇒ **`Jain Zar` 卡面印「天赋：寂静风暴」，而她每回合塞给你的那张卡标题却是「寂灭风暴」**。\n"
    "④ **本仓先例与它完全同型**：**「`Warlord` → 战将」（用户 2026-10-18 拍板）** —— 判据方向 = 「**能逐张核的印刷品 / 原版产物**」 > 「**我们的自译**」；"
    "且 **`A1762` 已立的口径**就是「**以官方为准、官方没有才留自译**」⇒ 本账只是把同一条延伸进 `descZh`。\n"
    "⑤ **代价**：**动不了解析器**（`descZh` 全仓无一处进 `Parse`）⇒ **不碰 `ruleprobe` 基线**；但要改**源**"
    "（`zh_cards.json` 的 `d` 列 或 `cardface_fixes.json` 的 `descZh` / `_manual_descZh_*`），再重生成卡表。\n"
    "🔴 **执行纪律（⛔ 三条，缺一条就静默改错）**：\n"
    "(a) **⛔ 一处一处改、别全局替换** —— 这批名字里有通用词：**「复生」在全池 `descZh` 里另出现 16 次**，但那是 `Reanimated` 关键词的「**被复生时**」"
    "（`SAU3` / `SAU71` / `SAU22` …）、**不是天赋名**，一刀切会**静默改错一片**；\n"
    "(b) **连带两处要一起复核**：`数据/卡牌翻译/zh_decks.json` 与 `Assets/RuleEngine/Resources/prebuilt_decks.json` 里也命中「寂静风暴」（**预组/教程卡组正文**）；\n"
    "(c) 🔴 **本会话刚写进去的 2 张要【反过来对齐】** —— `GOF18` 现在写的是「锯骨大夫」、`BL34` 写的是「四神宠儿」，"
    "**两个都是照「天赋卡/自译列」那一侧写的**，按本裁决应改成官方「**锯骨者**」/「**四者之选**」⇒ **`A1987` 的那两条 `descZh` 要跟着改**。\n"
    "⚠️ **只覆盖到官方命中的那 75/85**：余 **9 条**（`Angel's Wrath` / `War Howl` / `Author of the Codex` / `Catechism of Death` / `High Rune Priest` / `Refuse to Yield` 等）"
    "官方表**查不到** ⇒ 按**铁律 11 例外①**保持自译，**并在数据里写清「官方表查不到」**（⛔ 别留一个看不出理由的空）。"
    "⚠️ 那 9 条**与 `descZh` 本来就一致** ⇒ **不算「两套并存」**。\n"
    "⚠️ **⛔ 两件事别混成一件**：天赋名这 75 条**无 I2 记号**（实测 `[[ ]]`/`(( ))`/`$$ $$`/`<` 命中 **0**）⇒ **这一支不需要等 `A1763` 的渲染层**；"
    "但**若同批去换官方 `descZh` 整句**，就要过那一层。\n"
    "🔴 **方法论坑（顺手记，别再栽）**：**天赋名全部落在 `Card_Name/<32 位 GUID>` 键上**（⛔ 不是 `Card_Name/ASH74` 那种拿卡 id 当键的形态）"
    "⇒ **「官方表里有没有这个天赋名」按 id 查会得出「没有」的假结论**，只能拿**英文名**去 `by_en` 反查（`gen_cards_engine.py:814-816` **自己写着这条**）。"
    "⚠️ 另一条：`Dok's Toolz` 只在 `PackName/ORK_DoksToolz`（**一副卡包名**）命中一次 ⇒ **疑巧合，别当官方天赋名**。\n"
    "⚠️ **未查清**：那 9 条官方表里是否以**别的写法**存在（只做了精确 ＋ 忽略大小写反查、**没做模糊匹配**）；那 56 处改完后"
    "**是否有别的卡正文提到天赋名**（另有 **19** 次命中，其中 **16 次是「复生」** ＋ 1「凯恩之怒」＋ 1「空降部署」＋ 1「统御之道」＋ 1「寂静风暴」＋ 2「战斗圣歌」＋ 2「风暴领主」＋ 3「宁静统合者」）**没逐条判是不是天赋槽**。"
)


def main():
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A2002`**')]
    assert len(hits) == 1, hits
    if '主对话已裁' in lines[hits[0]]:
        print('  = A2002 已裁过，跳过')
        return
    j = hits[0]
    while not lines[j].rstrip().endswith(' |'):
        j += 1
    lines[j] = lines[j].rstrip()[:-1].rstrip() + SEG + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('OK A2002 裁决已落；总行数 %d' % len(lines))


if __name__ == '__main__':
    main()
