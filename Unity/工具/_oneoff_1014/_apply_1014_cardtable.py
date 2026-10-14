# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 卡表块盘点落账：
 ① A1773 / A1782 / A1783 / A1748 四行追加「动手前必须知道的冲突」
 ② 新账 A1980（静默陷阱三条 + 下游是一条链）· A1981（12 督军 cost 要裁）· A1982（Modular_Description 缺口 + 只换 zh 无成文口径）
 施工图 → 资料/普查产出_第三十会话/卡表块_施工设计_1014.md
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_cardtable.py
"""
import io

TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
MAIN = r'D:\4\项目任务.md'
DOC = '`资料/普查产出_第三十会话/卡表块_施工设计_1014.md`'

SEG = {}
SEG['A1773'] = (
    " 🔴 **2026-10-14（第三十会话）动手前必须先裁一处冲突（只读盘点现核）**：**12 张督军的 `cost` 与真值直接冲突** —— "
    "我们 **0** ↔ 真值 `manaCost = 2`（`Haarken` / `Nemesor Zahndrekh` / `Neurothrope` / `War Shaper` / `Terror of Vardenghast` / `Tervigon` / "
    "`Varro Tigurius` / `Jain Zar` / `Anvirr Keltoc` / `Eliac Zephyrblade` / `Medreyal Ghaelyn` / `Lhykhis`）；"
    "而我们 **2026-09-26 已结案**「56/56 无费用六边形 ⇒ 全 0 是对的」（`gen_cards_engine.py:337-356`）；"
    "真值里 `cardType=10`（Hero）60 张：**57 张 = 2**、`DA1`/`DA2`/`DA3` = **0**。⇒ **⛔ 不许整列照搬**，见新账 `A1981`。"
    "✅ 同批另一条是好消息：`STAT_FIXES` 的 **8 处 `ranged` 逐条与真值一致（0 处冲突）** ⇒ 换真值不会退步。"
    "🔴 **另一条**：现被 [整批丢弃的 66 张] 里 **42 张在真值中、多数 `spellType=200`（进攻卡）+ `manaCost=-1`** ⇒ 它们**走 `gen_offensive_cards.py` 那条线**；"
    "`Warmaster`(`manaCost=2, rarity=4`) 与 `Exemplar of Hate`(`1,2`) 则是**误丢**。"
    "⛔ **施工顺序 / 逐笔施工卡 / 会静默出错的地方 → " + DOC + "**。"
)

SEG['A1782'] = (
    " 🔴 **2026-10-14（第三十会话）实际范围盘点（只读复算）**：按「卡名 → `ocrName`」两级 join 复算那 **187 张自造 id** ⇒ "
    "**148 张能 join 上真值**（账面写 145），其中**只有 47 张的真值 id 是 `前缀+数字`**、**101 张是 32 位 GUID** "
    "⇒ 「145 张换回真值」**实际只有约 1/3 能拿到真·数字 id**，其余落进 **`A1800`**（GUID 卡）。"
    "**旁证**：`decklists.json` 里实有 **98 个 GUID 形态 id**，其中 **26 个与真值 `uniqueId` 重合**（例 `d2b16a34…` = `Primaris Impulsor (tutorial)`）"
    "⇒ **GUID 确实是游戏内 id 的合法形态**，不是「不像 id」。"
    "🔴 **同批另两笔**：`A1778`（10 缺口 + 8 督军）与 `A1783` 应当**并进本笔一起做**（`A1783` 的 6 张里 4 张已在池，见它那一行）。"
    "⛔ **置换必须一次性做完**（⛔ 别分次，本仓有先例）；⚠️ **改完要以 id 为键重跑【6 个下游生成器】**（图 / 语音 / 图标 / VFX / 预组牌 / 清单两笔）"
    "—— **是一条链、不是一个命令**，见新账 `A1980`；施工顺序 → " + DOC + "。"
)

SEG['A1783'] = (
    " 🔴 **2026-10-14（第三十会话）就地订正（铁律 5）—— ⛔ 这条不能当「新增 6 张」做**：现核那 6 张里 **4 张【已经在池】、只是名字不同**："
    "`GOF20 Veteran Stormboy` ↔ 我方 `GOF_Veteran_Flyboy`（自造 id）· `SAU4 Gauss Warrior` ↔ `SAU_Gauss_Reaper_Warrior` · "
    "`SAU6 Tesla Immortal` ↔ `SAU_Tesla_Carbine_Immortal` · `SAU8 Gauss Immortal` ↔ `SAU_Gauss_Blaster_Immortal`；"
    "**只有 `EC30` / `TL40` 我方真没有**。⇒ **按「新增」做 = 造重复卡** ⇒ **并入 `A1782` 一起做**（它是同一批卡的另一个视角）。"
    "**验收判据** = 卡池 `count` 前后差值 = **真新增数（预期 2 或 0）**。"
)

SEG['A1748'] = (
    " 🔴 **2026-10-14（第三十会话）判据盘清 ⇒ 这一笔【挂起】**：原文那 **20 张** 里已落 **18 张**（现读卡池 18/18 已在）；"
    "**剩 `SW13` / `SW31` 要 `ferocity`**，而**真值快照的 `CardTraits` 里 `ferocity` 出现 0 次** "
    "（判据 → `资料/普查产出_第二十七会话/A1748_原版trait真值.md` §6）⇒ **本地无判据** ⇒ 按铁律 11 例外①「原版判据为空」**挂起**，"
    "⛔ 别用别处的写法顶替。⚠️ 同批：`A1776` 要加的 `cardTraits` 列一旦落位，这张表就是将来补 `ferocity` 的入口。"
)

A1980 = (
    "| 🆕 **`A1980`** | 🔴 **卡表那批账的三条【静默陷阱】+ 一句结论「不是一条命令、是一条链」**（2026-10-14 只读盘点 + 主对话采用；"
    "施工图 → " + DOC + "）：\n"
    "① 🔴 **`gen_cards_engine.py` 退出码 = 不落盘**：`_bad`（**裸卡名键用在同名卡上**，`:1336-1351` ⇒ `:1478-1493`）为真时 **rc=1、一个字节都不写** "
    "⇒ 若 `A1763`/`A1748` 往 `cardface_fixes.json` 加裸键（**池里 3 组同名**：`Terminator`/`Terminator Champion`/`Maulerfiend`），**整批产物会静默停在旧版**。\n"
    "② 🔴 **`--check` 不纯只读**：`MADE_LIST` 的写在 `if args.check` **之前**（`:1468` vs `:1480`）⇒ **只想对账也会落一个文件**；"
    "且 `MADE_LIST` 落在 `d:/4/_tmp_view/card_ids_made.txt`（**不进版本控制**）。\n"
    "③ 🔴 **`CardDatabase.cs` 的 version 守卫**（`:104-116`）：**加列必须 bump 到 v9 并补守卫**，否则新列被静默读成 `0`/`null`。\n"
    "🔴 **结论**：`A1782`/`A1778`/`A1783` 改的是 `card_ids.json`，而**以 id 为键的下游产物另有 6 个生成器**"
    "（`import_original_art.py:1160` · `import_original_audio.py:82` · `gen_icon_plan.py` · `gen_card_vfx_by_card.py:106` · "
    "`gen_prebuilt_decks.py:55` · `gen_card_inventory_options.py`/`gen_card_initiative.py`）⇒ **是一次「链式重跑」，不是一个命令**。\n"
    "⚠️ 同批另三条（同文件 §五）：**id 置换后 `Resources/Art/cards/<旧id>.png` 全变孤儿、`voice_lines.json` 键失配**（**静态产物不报错**，`A1760` 那次就是「换 id 忘了哨兵」）· "
    "**`keywords` 一列现有 4 个写手会互相覆盖**（`A1748`/`A1773`/`A1776` 都碰它 ⇒ **要先定权威序**，否则复发 `A1620`/`A1633` 那两族血案）· "
    "**`descZh` 一列 3 个写手**（官方表那层插在哪必须显式写死，否则 **248 条手工 `descZh` 被静默盖掉**）。 |"
)

A1981 = (
    "| 🆕 **`A1981`** | 🔴 **【要裁】12 张督军的 `cost`：真值 `manaCost = 2` vs 我们 0 —— 「那个 2 是值还是默认哨兵」判据不足**"
    "（2026-10-14 只读盘点 + 主对话采用）：冲突面见 `A1773` 那一行；我们 **2026-09-26 已结案**「56/56 无费用六边形 ⇒ 全 0 是对的」"
    "（`gen_cards_engine.py:337-356`），真值里 `cardType=10` 60 张有 **57 张 = 2**、`DA1/2/3` = 0。"
    "🔴 **没查到消费点**：`grep decomp_full/` 后 **`ManaCost` 只命中 2 个方法**（`CardScript__ShowManaCostAura.c` 是个 12 行 `LogWarning` 桩）"
    "⇒ **本地拿不到「原版运行时怎么用这个值」的判据**。⇒ **要裁**（`A1773` 卡在这上面）；"
    "⛔ **不许拿默认值顶替，也不许推翻 2026-09-26 的结论** —— 要么补判据（反编译换关键词 / 找 `RawCardScript` 那一侧的读点），要么记成「原版判据为空」按铁律 11 例外① 处置。"
    "⛔ **`A1773` 在那之前不许整列照搬**。 |"
)

A1982 = (
    "| 🆕 **`A1982`** | ⚠️ **`A1763` 有两处缺口/口径没落**（2026-10-14 只读盘点）：\n"
    "① **`Modular_Description/*`（340 条）还没进工程** —— 它是反编译 `SupportMethods__ModifyLocalization.c:75-186` 那一支 `((Key,arg))` → `Modular_Description/<Key>` 的**必需半边** "
    "⇒ 不补，官方文案里那一族**解不出来**。\n"
    "② 🔴 **「`A1763` 只换 `descZh` 还是连 `desc` 一起换」没有成文口径** —— 账面只写 `descZh`，**但官方 EN 列同样带记号**"
    "（`Card_Description/UM64` 原文读过）；**换 `desc` = `EffectText.Parse` 全池重做**（风险量级完全不同）⇒ **要裁**。\n"
    "⚠️ 同批两处**会被 `A1763` 打掉的**（同文件 §五）：**`card_icon_plan.json` 里 `descZh` 那 1622 条会全成死条目**（token 全是我们自译写法），"
    "而 `gen_icon_plan.py` 是 **rc≠0 一个字节都不写**（`工具/gen_icon_plan.py:41-43`）⇒ **两份 json 会停在旧版、`Rewrite` 拿旧表配新文本**；"
    "且 **`CreatePool.MentionedCards` 对 `descZh` 也跑一趟**（`RuleEngine/Core/CreatePool.cs:789-830`，中文侧现读 **132**）⇒ 官方中文用 `$$ID$$` 指卡、命中数会变 "
    "⇒ **`RuleEngineTest.TestMentionedCards` 期望值要重算**。 |"
)


def append_seg(path, cid, seg):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| ') and ('**`%s`**' % cid) in l]
    assert len(hits) == 1, (cid, len(hits))
    i = hits[0]
    assert lines[i].rstrip().endswith(' |'), lines[i][:70]
    lines[i] = lines[i].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(path, 'wb').write(out.encode('utf-8'))
    print('  +%s' % cid)


def main():
    for cid in ('A1773', 'A1782', 'A1783', 'A1748'):
        append_seg(TASKS, cid, SEG[cid])
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| ') and '**`A1979`**' in l]
    assert len(hits) == 1
    lines[hits[0] + 1:hits[0] + 1] = ['\n'.join([A1980, A1981, A1982])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1980/A1981/A1982')


if __name__ == '__main__':
    main()
