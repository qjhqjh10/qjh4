# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · A1996 改判（不是缺陷：原版这一列本来就是常量 20）+ 新账 A2004
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_initiative.py
"""
import io

MAIN = r'D:\4\项目任务.md'

A1996_SEG = (
    " ✅✅ **2026-10-14 判据查实 ⇒ 【改判：这不是缺陷】—— 原版这一列本来就是常量 `20`，我们与原版【逐字一致】。"
    "上面那段「强烈疑似快照未填」的推断【被推翻】（铁律 5，留痕）**：\n"
    "① 🔴 **原版真的读这一列、并比大小定先手（高者先手），但它读的是【督军那张卡】** —— 唯一读点 = "
    "`d:/2/tools/decomp_full/SupportMethods__GetPlayerGoesFirstWithInitiative.c:60,63`："
    "`if (*(int *)(enemyWarlord + 300) < *(int *)(playerWarlord + 300)) return true;`（`300 = 0x12C`），"
    "而两个 warlord 取自 `*(MatchData+0x50/+0x58) + 0x18` **⇒ 是 SO 对象本身**；日志文案实证 = `\"Player goes first? player initiative\"` + `\" enemy initiative\"`。\n"
    "② 🔴 **唯一写点 = 构造函数**：`RawCardScript__.ctor.c:16` = `*(param_1 + 300) = 0x14`（**默认就是 20**）"
    "—— **全仓没有第二个功能读点、也没有任何运行时写点/`set_initiative`** ⇒ **值只来自 SO 序列化数据**。"
    "（字段：`dump.cs:23287` `public Initiative initiative; // 0x12C`，**全 `dump.cs` 只此一处**；枚举 `dump.cs:45547-45556` = `Undefined 0 / Low 10 / Medium 20 / High 30 / VeryHigh 40`。）"
    "⚠️ **上一轮搜不到的根因**：`decomp` 里偏移写成**十进制 `+ 300`**，`grep -il 0x12c` **全库 0 命中**（已逐个看过 `+ 300)` 的 20 个文件，除本读点与上面那个写点外全是别的类型）。\n"
    "③ 🔴 **独立复核（不是只信那份导出）**：用 UnityPy 直接重读原包 `远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1…/__data`（1,199 对象 = 1,195 MB ＋ 3 MonoScript ＋ 1 AB）"
    "⇒ **`initiative` 分布 = `{20: 1190, 30: 2}`**；**3 个没有这个键的对象是 `AlternateArtCard`（`AA_HB_…Azrael` / `AA_HB_…Abaddon`）与 `AddressablesBuildNumber` —— 它们本来就没这个字段**（不是「缺数据」）。"
    "**按 `cardType` 拆：`Hero(10)` 60 张 = `58×20 ＋ 2×30`**（`Marneus Calgar (tutorial)` / `Uriel Ventris (tutorial)`）；`Minion(0)` 540 · `Tactic(20)` 592 **全 20**。"
    "⇒ **原版真局里也【必然平局】、落到下一级（掷硬币）** ⇒ **「整列同值」不是导出缺陷、也不只是「快照没填」——原版这列本来就是常量**。\n"
    "④ **先后手原版逐跳（5 级）**：`SupportMethods.GetPlayerGoesFirstWithInitiative` → ① 模式特例（`matchType==0x50` 直接 `true`；`0x82/0x8c/0xbe` 取关卡字段）→ "
    "② **比督军 initiative** → ③ 平局 → `forceFirstPlayer@0x64` → ④ `playerIdGoesFirst@0x48` 与 `PlayerDataManager+0x328` 比串（联机确定性）→ ⑤ `playerGoesFirstRandomInt@0xE8`；"
    "另一条 `BattleManager__GetPlayerGoesFirst` 在模式特例之后**直接掷硬币**。\n"
    "⑤ **交叉源两条，都倾向「不该编值」**：**PnP 三张不同阵营督军卡面**（`Marneus Calgar` / `Ramatek the Cruel` / `Jain Zar`）**只有近战/远程/生命、没有先后手记号**；"
    "**官方本地化表**里 `initiative|先攻|先手` 只命中 **3 条背景故事散文**（`Warlord_Lore/SAU65`/`TL42`/`UM85`）、**没有任何游戏词条** ⇒ 界面上从不展示。\n"
    "⑥ ⇒ **我们是对的**：`RuleEngine/Core/FirstSeatRules.cs:54-93` 的方向与逐行语义**与原版 `:60,:63` 一致**（高者先手；平局 → `null` 交给下一级、**⛔ 不偏置**）；"
    "`BattleDriver.cs:3022-3027` 让「调用方显式指定」优先，理由是**联机/重连/放录像必须复现同一先手**（已就地写明）。"
    "🔴 **⇒ 本笔【可核销】**；**⛔ 不许为了「让先后手有差别」去编一套先攻分布**（判据不是空的 —— **判据就是「常量 20」**）。\n"
    "🔴 **限制（铁律 5·c）**：**只覆盖 2025-07-30 那一个快照** —— 现版（Unity 6）客户端的卡 SO 在本机**不存在**（`assets_full` 91 个包里**没有 allcards**；card 系只有 `bundle_*cardassets_assets_all`，里面 `MonoBehaviour/` 数 = **0**）"
    "⇒ 若原版后来给督军差异化过，**本地判不了** ⇒ ⛔ **别宣称「任何版本都是 20」**。"
)

A2004 = (
    "| 🆕 **`A2004`** | ⚠️ **`initiative` 那 171 张 `-1` 里，至少 4 张督军的真值【就在那份快照里】—— 是 id 匹配没命中，不是真值缺**"
    "（2026-10-14 `A1996` 查证顺手查出，与 `A1779` **同族** = join 键错配）：快照里有 `Asmodai, Master Interrogator`(`DA3`, 20) · "
    "`Belial, Master of the Deathwing`(`DA2`, 20) · `Azrael, Grand Master`(`DA1`, 20) · `Sylar Hexscorn`(`BL36`, 20)，"
    "而我们叫 `DA_Asmodai` / `DA_Belial` / `DA_Azrael` / `BL_Sylar_Hexcorn` ⇒ **是 `card_inventory_options.json` 的 id 匹配没命中**"
    "（我们的名字被截短、**且 `Hexcorn` ≠ `Hexscorn` 拼写差一个 s**）⇒ **属 `A1798` 那条链**。\n"
    "⚠️ **同批三处措辞/数字要改**（都现核过，铁律 5）：① `RuleEngine/Core/CardDef.cs:178` 与 `CardPresentation/Battle/BattleDriver.cs:3025` 都写「**我们池子里这 56 位督军全是 20**」"
    "—— **实读 = `42×20 ＋ 14×−1`** ⇒ 应改「**已知的 42 张全是 20；14 张 `-1`**（SpaceWolves 整族 ＋ 上面那 4 张 DA/BL）」；"
    "② `工具/gen_card_initiative.py` 头注写「缺这个键 **3 行**」—— 实读那 3 行是 **`AlternateArtCard`（该类根本没这个字段）＋ `AddressablesBuildNumber`** ⇒ **措辞该从「缺」改成「不是卡」**。\n"
    "⇒ **要做**（铁律 11）；⚠️ 顺带：原版 `SupportMethods…WithInitiative:41-53` **每次调用都打一行日志** ⇒ 可用 `d:/2/远端缓存_旧profile_1013/Everguild/Player.log` **反查真局实况**。"
    "判据原文 → `资料/普查产出_第三十会话/`（本会话报告）。 |"
)


def main():
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1996`**')]
    assert len(hits) == 1, hits
    j = hits[0]
    while not lines[j].rstrip().endswith(' |'):
        j += 1
    lines[j] = lines[j].rstrip()[:-1].rstrip() + A1996_SEG + ' |'
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A2003`**')]
    assert len(hits) == 1
    k = hits[0]
    while not lines[k].rstrip().endswith(' |'):
        k += 1
    lines[k + 1:k + 1] = [A2004]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('OK A1996 改判 + A2004 已立；总行数 %d' % len(lines))


if __name__ == '__main__':
    main()
