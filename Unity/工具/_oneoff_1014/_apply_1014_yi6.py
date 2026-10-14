# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 乙档六笔裁决落进正本（判据原文 → 资料/普查产出_第三十会话/乙档六笔_判据查证.md）
 + 三条新账 A1983/A1984/A1985 + A1962 再补一例。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_yi6.py
"""
import io

TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
MAIN = r'D:\4\项目任务.md'
DOC = '`资料/普查产出_第三十会话/乙档六笔_判据查证.md`'

SEG = {}

SEG['A1258'] = (
    " ✅ **2026-10-14 判据已查实（只读查证）⇒ ② 那半【可以直接结掉】**："
    "**原版【不回退】，也不该回退** —— 取消发生在**消耗之前**。`bm + 0x3bc` 是 **`thisTurnActionCounter`**"
    "（**网络同步计数器，不是攻击次数**）；真正的攻击配额 `EntityScript.attacksThisTurn(+0x48)` 的**全部写点**里，"
    "**`CancelAttack.c` / `_PrepareAttack` 一个字都不碰**。我方 `AttacksThisTurn++` / `Exhausted=true` 排在取消之后"
    "（`RuleCore.cs:4149-4150`）⇒ **形状与原版一致** ⇒ 把 `RuleCore.cs:5212-5218` 那句「没查清」改成已核。"
    "**① 附魔层的建法判据齐**（两条并列 list `+0x108`/`+0x110` + 五个入口：挂 / 取 / 花 / 结算 / 开局；**存续不按回合清**）。"
    "🔑 **`HasCancelAttackAbility` 开头先调 `HasCancelAttackEffect`** ⇒ 补 ① **不必另开口**。判据原文 → " + DOC + " §`A1258`。"
)

SEG['A1597'] = (
    " ✅ **2026-10-14 逐处读数已完成 ⇒ 本笔【可核销】**：现读同类夹具 = **62 处**"
    "（⚠️ 账里 `W4` 记的「20 处」是 2026-10-11 的快照，**三个会话后长到 62**）；"
    "**25 处 req == 落点 · 35 处 req ≠ 落点**（其中 **5 处根本不摆人**）"
    "⇒ 🔴 **没有任何一处把「请求格」当落点断言**（凡按格号断言的 16 处，要么用 `SlotOf(ctx,0,\"卡名\")` 的**安全写法**、"
    "要么断的**就是真实落点**且与模型吻合）。判据 = `Core/BoardSlots.cs:139-142`（落到 `min(IndexOf(请求格), 该侧现有单位数)`）。"
    "判据原文 → " + DOC + " §`A1597`。⚠️ 同批查出一处**真缺陷** → 新账 `A1984`（有洞夹具）。"
)

SEG['A1690'] = (
    " ⚠️ **2026-10-14 判据查实 ⇒ 机制坐实、但【今日不可达】**：全池 1126 张里 `desc` 同时含 `When/Whenever/After/Every time` 与 `adjacent` 的"
    "**只有 2 张**（`SW25 Long Fang` 已被 `RuleEngineTest.cs:17916-17950` 端到端钉住 · `SW43 Venerable Dreadnought` 的 `HasBareAdjacent` 为假）"
    "⇒ **两张都不在本账射程内**。机制 = `CardDef.FinishWhenSentence`（`Core/CardDef.cs:2528`）把**正文**（逗号后那段）交给 `EffectText.Parse`，"
    "而那道闸判的是 `low.StartsWith(\"when \")`（`Core/EffectText.cs:1479`）—— **正文不带该前缀** ⇒ 事件层**永不触发 ⑤**、静默落 "
    "`else t.Anchor = AdjacentAnchor.Self`（`:1488`）；🔴 **同一个句子两层结论打架**：`IsFullyParsed(整句)` 判**半懂**（`:1449-1454`），"
    "而**真正被执行的**那批 op（`whenTriggers[].Ops`）锚点是 `Self`、**没有任何降级标记**。"
    "⇒ **判据不足 —— 必须人造夹具才能坐实**（5 步夹具 + 摆位 + 期望读数 → " + DOC + " §`A1690`）。"
    "⛔ **`ruleprobe.sh seg` 只看覆盖率层、看不出事件层**，**不能单独当判据**。"
)

SEG['A1426'] = (
    " ✅ **2026-10-14 判据查实：`targetsAffected` ≠ `playersAffected`【不同义】** —— 两个不同枚举、两条正交轴"
    "（前者选**打哪个池子/哪张卡**，后者选**哪个玩家**），且在同一个 `abilityLogic` 上**并存 1622/1622 条** ⇒ **结构上排除别名**；"
    "⚠️ **数字陷阱**：`10` 在 `TargetsAffected` = `allFiltered`、在 `PlayersAffected` = `Self`；`20` 前者 = `self`、后者 = `Enemy` "
    "⇒ ⛔ **跨表按数字判读必错**。🔴 **同一次就地订正（铁律 5）：账上那句「`targetsAffected` 只在 `deckCriteria/countCriteria` 下命中」"
    "是【假结论】** —— 实为 **43 条 base 路径**（现核复现），而那两条 1192 条**全是默认 0**。"
    "**「另一半」的答案**：`chosenAbility=40 (buffSelf)` 配的是 **`targetsAffected=20 (self)`**（`buffSelf` 178 条里 170 条 = self）"
    "⇒ **我方 `Subjectless`「有施放者⇒自己」有原版数据背书**；另有第二族 `buffOthers` + `allFiltered` + `friendlyUnits=1`（153 条）"
    "⇒ **判据 = `chosenAbility` + `targetCriteria` 合看，`playersAffected` 完全不参与**。判据原文 → " + DOC + " §`A1426`。"
    "⚠️ 还差一小块：真值 1195 ↔ 我池 1126 的**逐卡对齐表没建**（现在只有族级结论 + 3 张单卡实证）。"
)

SEG['A1779'] = (
    " ✅ **2026-10-14 根因查实 = (c)「join 键错配」，(a)(b) 都不是**：`AM3`/`AM66` **一直在我们卡池里**"
    "（`AM_Commissar_Elan` / `AM_Hektor_Thenmann`），只是**卡名与真值 `cardName` 不一致** ⇒ `pick_id`（`工具/gen_cards_engine.py:1020`）"
    "按名换 id 落空 ⇒ 走自造 id ⇒ 「按 `AM3`/`AM66` 查」与「按卡名查」**两头都查不到**；"
    "**两条 `hasStats:true`、不在那 81 张里** ⇒ ⛔ 不是「压根没做」也不是被筛掉。"
    "**缺的督军全名单 = 11 条对不上、其中 8 条其实在池**（`AM3` · `AM66` · `BL36 Sylar Hexscorn`（**少一个 c**）· `DA1`/`DA2`/`DA3` · `GSC3` · `GSC66`）；"
    "**真缺 3 条** = `Marneus Calgar` / `Uriel Ventris` / `Sylar Hexscorn` 的 **`(tutorial)` 专用副本 SO**（我们一律只收正式那张）。"
    "**最小方向（判据齐、⛔ 未动手）**：把生成器里**已有的官方两级表** `load_official_cardname().by_en`（英文名 → id）接进 `:1020` 作第二路 "
    "⇒ **零数据改动救 5 条**（`DA1/2/3` · `GSC66` · `AM66`）；剩 3 条加显式别名（`AM3` / `BL36` 拼错 / `GSC3`）。"
    "⚠️ **`AM3` 数值冲突（join 修好后会立刻暴露）**：真值 `maxHealth 35 / armour 0`，而 **PnP 卡图与 `card_stats.json` 都是 `30 / 护甲 2`** "
    "⇒ 按**铁律 7 以 PnP 成品卡图为尺**。⚠️ `AM3` 卡名要不要换成官方 `Commissar Denkler`/「政委登克勒」是**口径决定**。"
    "判据原文 → " + DOC + " §`A1779`。"
)

SEG['A1800'] = (
    " ✅ **2026-10-14 join 规则查实（只读复算）**：187 张 = **106 张原版 32 位 GUID 卡 · 58 张原版短 id 卡 · 23 张原版快照里根本没有**"
    "（⚠️ 那 **23 张全是 Ultramarines**）；**级联 join 规则** = ① **官方 `Card_Name/*` 的英文值 → 它的键（键就是原版 `uniqueId`）—— 164/187 一次到位、键唯一** "
    "② 真值 `m_Name` 尾部（`<Army>_war_<名>` 取 `-` 后那段）9 张 ③ `card_stats.json` 的 `ocrName` 归一化 31 张（与 ① 重叠）；"
    "取英文值时**先用我们 `name`、查不到再退 `ocrName`**。⛔ **一句话：「名对上」不是必需要件，「官方表的英文值对上」才是那把万能钥匙。**"
    "🔴 **第一根因不是「卡名对不上」，是 `card_ids.json` 里 0 个 GUID 形键**（现核：`mapping` **1058 键 · GUID 形 0**）"
    "⇒ 原版那 **259 张 GUID 卡**的 `uniqueId` **一条都进不了这张表**、`pick_id` 必然返回 `None`；"
    "🔴 **第二根因** = 保留了 **123 行旧表残留**（同名重复，`AM20`/`DA65` 在真值 1195 里**根本不存在**）⇒ 与真值行撞名 ⇒ `pick_id` 按「不猜」放弃"
    "（**19 张明明有原版短 id 却拿不到**）；**放行规则 = 候选里若只有一个落在真值 1195 内，取那一个**。"
    "判据原文 → " + DOC + " §`A1800`。"
)

A1983 = (
    "| 🆕 **`A1983`** | 🔴 **取消攻击那条路上，原版还有一条【我们没落】的前置闸**（2026-10-14 只读查证顺手查出，属 `A1258` 族）："
    "`CardScript__CancelAttack.c:35` —— `if (cardState(+0x228) != 3 && != 0x11) { LogWarning; return; }`，"
    "即「**只有处于『正在攻击』状态才取消得掉**」（状态 `3` / `0x11` 之外一律警告并返回）。⇒ **要做**（铁律 11）："
    "给我们的取消路径补这道状态闸；⚠️ **先核** `cardState` 那两个枚举值各是什么状态（`dump.cs` 里 `CardState` 一族）。"
    "判据原文 → `资料/普查产出_第三十会话/乙档六笔_判据查证.md` §顺手发现 4。 |"
)

A1984 = (
    "| 🆕 **`A1984`** | 🔴 **一处自检夹具是【有洞棋盘】⇒ 它走的是 degraded 分支、不是模型的插推规则**（2026-10-14 `A1597` 逐处读数时查出，"
    "属 `A1692` 族）：`Assets/RuleEngine/Editor/RuleEngineTest.cs:28078-28079`（`TestA1574SwarmSubject`）用 `Place(s3)` + `Place(s1)` "
    "⇒ 左侧下标 `{0,2}`、**1 号下标是洞** ⇒ `BoardSlots.IsContiguous` 假 ⇒ 下面 `:28084` 的 `PlayCard(..., 2)` 走 **degraded 分支**"
    "（「请求哪格写哪格」）并**打一条 `[BoardSlots]` LogError**。⇒ **要做**：**建议改成 `Place(s2)`**（或去掉那一份夹具）—— "
    "否则这条断言验的不是我们以为的那条路径。⚠️ 与 `A1971`（21 次有洞棋盘 LogError）同族。"
    "判据原文 → 同上 §`A1597`。 |"
)

A1985 = (
    "| 🆕 **`A1985`** | ⚠️ **`Core/BattleEvent.cs:181` 那条注释引【引错了对象/偏移】**（2026-10-14 只读查证顺手查出）：它写的是「`CardEffect +0x90 == 0x3c`」，"
    "实际读的是 **`ActiveEnchantment.enchantmentCard(+0x10)` 那张 `RawCardScript` 的 `+0x90`**（= `SpellType`）—— **宿主对象写错了**；"
    "且同处另两条**扫的不是同一个 list**（`HasSecretOrders` 扫 `+0x110` = `activeEnchantments`，`AI__ValueOfEnchantments` 扫 `+0x108` = `activeEffects`）。"
    "⇒ **要做**（铁律 5：就地订正那三句注释）；⚠️ 判据 = `d:/2/tools/il2cpp_out/dump.cs` 的 `EntityScript` / `BattleManager` 段"
    "（⛔ **别以 `decomp_full/ActiveEnchantment__.ctor.c` 为准 —— 它的文件名与内容对不上**）。"
    "判据原文 → 同上 §顺手发现 3。 |"
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
    for cid in ('A1258', 'A1597', 'A1690', 'A1426', 'A1779', 'A1800'):
        append_seg(TASKS, cid, SEG[cid])
    append_seg(TASKS, 'A1962',
               " ⚠️ **2026-10-14 再添一例（只读查证顺手撞见）**：`d:/2/tools/decomp_full/ActiveEnchantment__.ctor.c` 的**内容是匿名类型 ctor**、"
               "**不是 `ActiveEnchantment` 的构造函数** ⇒ 那一族**要以 `d:/2/tools/il2cpp_out/dump.cs` 为准**（本轮第 3 次撞到「文件名与内容对不上」）。")
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| ') and '**`A1982`**' in l]
    assert len(hits) == 1
    lines[hits[0] + 1:hits[0] + 1] = ['\n'.join([A1983, A1984, A1985])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1983/A1984/A1985')


if __name__ == '__main__':
    main()
