# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 数据侧六条落账：
 ① A1987 已办 + 两处订正 + 名单不全
 ② A1993 已办
 ③ A1992 前提只对一半
 ④ 新账 A2002（中文天赋名两套口径）· A2003（gen_icon_plan.py 待重跑）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_data6.py
"""
import io

TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
MAIN = r'D:\4\项目任务.md'

A1987_SEG = (
    " ✅ **2026-10-14 已办（数据侧写手 + 主对话采用）** —— **只有这 6 张卡变了、一个邻卡都没碰**"
    "（全池逐卡 diff 断言过）。**逐张 改前 → 改后**（现读 `cards_engine.json`）：\n"
    "· `SOR9 Preacher` `desc`：`5 ☀: Gain Shield` → **`Talent: Hymn of Battle. 5 ☀: Gain Shield`** · `keywords` → `[\"Talent: Hymn of Battle\"]`\n"
    "· `TAU2 Aun'Va` `desc`：`Ethereal Supreme` → **`Talent: Ethereal Supreme`** · `keywords` → `[\"Talent: Ethereal Supreme\"]`\n"
    "· `TL_Psychophage` `desc`：`Rally: …` → **`Talent: Feeding Frenzy. Rally: …`**\n"
    "· `GOF18 PainBoy Sniklaw` `desc`：**`Rally: Heal 3 to a friendly unit. Talent: Sawbonez`** · `descZh`：**`集结：治疗一个友方单位 3 点生命值。天赋：锯骨大夫。`**\n"
    "· `BL34 Abaddon` `desc` → **`Talent: Chosen of the Four`** · `descZh` → **`天赋：四神宠儿`** · `keywords` → `[\"Talent: Chosen of the Four\"]`\n"
    "🔴 **同一次【推翻了两条】（写手现核）**：(a) **`GOF18` 的段次序** —— 我在 `D2_造卡族判定.md` §三·A 里写的是 `Talent: Sawbonez. Rally: …`（Talent 在前），"
    "而**卡图亲读 + `Unity/资料/卡表核对_卡图提取/_合并总表.md:654` 两处都是 `Rally:` 在上、`Talent:` 在下** ⇒ **以 Rally 在前为准**；"
    "(b) **`GOF18` 的中文天赋名不是「锯骨」而是「锯骨大夫」**（`数据/本地化/i18n/zh_CN.csv:3436` = `天赋：锯骨大夫`；`zh_cards.json` 的 `Sawbonez.n` 同值）。\n"
    "🔴 **名字名单不全（写手顺手查出，本笔只做了原名单那 6 张）**：`_2026-09-15_A7天赋名` 那张 5 张名单（战斗圣歌/至尊以太/进食狂潮/**卡迪亚荣誉**/**至高大师**）**本笔只接了 3 张**；"
    "另 **`AM15 Minka Lesk`**（`desc = Friendly Infantry costs 1 less.`、`descZh` 早就有 `天赋：卡迪亚荣誉`）与 **`DA_Azrael Azrael`**（`desc` 已含**裸名** `… Supreme Grand Master`、应补 `Talent:` 前缀）**都没接** ⇒ **要补**。\n"
    "🔴 **`keywords` 那三条不是可选项（写手现核，⚠️ 我简报写成了「变多了才收」= 错的）**：`desc` 一有 `Talent:` 头，`CardDef.CollectTalent`（`:1607-1608`）就**提前 return** "
    "⇒ 裸名 `Hymn of Battle`/`Chosen of the Four`/`Ethereal Supreme` **永远留在 `KeywordTable.Dropped`**，而 `RuleEngineTest.cs:2034` 断的正是 **`Dropped.Count == 0`**（硬断言）"
    "⇒ **不改就会红**；修法 = 改写成**能归一化**的 `Talent: <名>`（`Normalize` 切冒号取头 = `talent`），**对 `_keywords` 的值零影响**。\n"
    "⚠️ **收口尚欠两件**：① **`ruleprobe` 的 ⑦ `PinnedParseMd5` 现在红了**（`e3a4b082…` → 现算 `79c93e35…`；解析差异 0 → **6 行**、正好这 6 张；`Source 列` op 1420 → **1425**、`unparsed` 53 → 51；**rc 1**）—— **待收口统一重钉**，⛔ 别单独重；"
    "② **`工具/gen_icon_plan.py` 待重跑**（⇒ 新账 `A2003`）。\n"
    "⚠️ **同批还有一条口径要裁** ⇒ 新账 **`A2002`**（池里中文天赋名与天赋卡自己的 `nameZh` 大面积不一致）。"
)

A1993_SEG = (
    " ✅ **2026-10-14 已办（数据侧）**：`desc` 由 `Pray: Deal 3 damage to an enemy troop. …` 改成 **`Pray: Deal 2-3 damage to an enemy troop. …`**"
    "（`descZh` 一字未动 —— 它本来就是对的）。**同一次**：`ruleprobe` 解析差异 0 → **6 行**（正好含 `SOR29`）、⑦ `PinnedParseMd5` 红 ⇒ **待收口统一重钉**。"
    "⚠️ **前置账 `A1992` 的前提只对了一半**（见它那一行）。"
)

A1992_SEG = (
    " ✅ **2026-10-14 已做（写手同时做了生成路口径 + md 那一行）—— 🔴 但【前提只对了一半】（写手现核，铁律 5）**：\n"
    "① **`gen_cardface_fixes.py` 读的是 `Unity/资料/卡表核对_卡图提取/_合并总表.md`、⛔ 不是 `卡牌完整信息库_0824.md`**，"
    "而且它**只算 `subtype`/`keywords` 两列、【从不重算 `desc`】**（`desc`/`descZh` 由 `keep` 白名单原样带走）⇒ **那条通路今天不会盖掉我们补的 `desc`**；"
    "② 🔴 **但那份 md 的缺陷是【成片】的**：全表 1128 行里「行内提到 `Talent`」的 **77 行**，其中 **5 行的效果列根本没有 `Talent`**"
    "（`Minka Lesk` / `Azrael` / `Preacher` / `Aun'Va` / `Psychophage`）；再加**连 `Talent` 这个词都没出现**的 `Painboy Sniklaw` 与 `Abaddon the Despoiler` ⇒ **至少 7 行** "
    "⇒ **「从这份 md 重算 `desc`」这件事本身危险**（也说明只补一行治不了根）。\n"
    "⇒ **本账按「已做」收，但理由要换成【防将来】而不是「现在就在盖」**；⚠️ 那 7 行的**全表普查/批修【没做】**（写手在做的时候如实留了）。"
    "**已做的两处**：生成路口径（`_manual_desc`/`_manual_descZh` 两列纳入手工覆盖、排在最后应用）＋ `卡牌完整信息库_0824.md:622` 那行补 `Talent: Sawbonez`。"
)

A2002 = (
    "| 🆕 **`A2002`** | ⚠️ **【要裁】池里的中文天赋名（`descZh` 的 `天赋：X`）与天赋卡自己的 `nameZh` 大面积不一致 —— 两条口径要先裁一条，再全池一起改**"
    "（2026-10-14 数据侧写手顺手查出）：`descZh` 里 **91 条** `天赋：` 用的是**我们的自译名**，而**天赋卡现在的 `nameZh` 已换成官方表值**。"
    "例：`Jain Zar`（`天赋：寂静风暴` vs 天赋卡 `寂灭风暴`）· `Avatar of Khaine`（`凯恩之怒` vs `凯恩的愤怒`）· `Biophagus`（`实验性生物恐怖` vs `实验生物惧妖`）· "
    "`Clamavus`（`声讯黑客` vs `黑客`）· 本批两张也踩在同一个坑上（`锯骨大夫` vs 天赋卡 `锯骨者`；`四神宠儿` vs 天赋卡 `四者之选`）。\n"
    "⇒ **要做**：**先裁**「以哪一边为准」（⛔ 两套并存比没有更糟，铁律 6），**再全池一起改**。"
    "⚠️ 判据提示：**中文那半的来源不是官方表**（`zh_CN.csv` / `zh_cards.json` **都是我们自译的**）⇒ 若走「以天赋卡 `nameZh` 为准」，那是**官方表**、更硬。"
    "⚠️ 相关：`A1762`/`A1763`（换官方 `descZh` 那一批）会**顺带**碰到这一族 ⇒ **建议并进那批一起裁**。判据原文 → `资料/普查产出_第三十会话/`。 |"
)

A2003 = (
    "| 🆕 **`A2003`** | 🔴 **`工具/gen_icon_plan.py` 待重跑 —— 本批 6 张新出现的 `Talent:` / `天赋：` 两种 token 现在【画不出徽记】**"
    "（2026-10-14 数据侧写手如实报；⚠️ 它**不在该写手白名单** ⇒ 没动）：现读 `CardPresentation/Resources/card_icon_plan.json` —— "
    "`SOR9` / `GOF18` / `TL_Psychophage` 的 `desc` 侧**只有 `Rally:`/`Pray:`/`☀`/`[Attack]` 这些行、没有 `Talent:` 行**；`BL34` **整张卡不在表里**；"
    "`TAU2` 只有 `descZh` 的 `天赋：` 一行、缺 `desc` 的 `Talent:` 行 ⇒ **这 5 张的天赋徽记画不出来**。"
    "⚠️ **不是退步**（改前也画不出来）⇒ **要做**：重跑 `工具/gen_icon_plan.py`（⚠️ 它有 **rc≠0 一个字节都不写** 的口径，见 `工具/gen_icon_plan.py:41-43`）。"
    "✅ 旁证（写手另核）：`CardIcons.Rewrite` 是按 token 逐个 `Replace`、本批**没删掉任何既有 token**（`☀`/`Shield`/`Rally:`/`Pray:`/`[Attack]` 全在原位）⇒ **不会弄丢别的图标**。"
    "⚠️ 与 `A1763` 的 `P3`（数据层）**同一处**，可合并做。判据原文 → 同上。 |"
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
    for cid, seg in (('A1987', A1987_SEG), ('A1993', A1993_SEG)):
        append_seg(MAIN, '| 🆕 **`%s`**' % cid, seg)
    append_seg(TASKS, '| 🆕 **`A1992`**', A1992_SEG)
    b = io.open(MAIN, 'rb').read()
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1998`**')]
    assert len(hits) == 1
    j = hits[0]
    while not lines[j].rstrip().endswith(' |'):
        j += 1
    lines[j + 1:j + 1] = ['\n'.join([A2002, A2003])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A2002/A2003')


if __name__ == '__main__':
    main()
