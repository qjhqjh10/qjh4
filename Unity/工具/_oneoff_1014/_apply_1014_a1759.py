# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 逐卡对账（A1759/A1772/A1822）盘点后落盘：
 ① A1759 行订正「没开始对账」（早开工、已产出一整轮、现读读数、切块已定）
 ② A1822 行改写（135 条里 62 条病根 = Talent: 没进解析 ⇒ 归 Talent 一族，别当第二份账）
 ③ 新账 A1977（仪器写死过期输入路径）· A1978（对账口径两处未验前提）· A1979（39 块没有施工图）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_a1759.py
"""
import io

TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
MAIN = r'D:\4\项目任务.md'

A1759_SEG = (
    " 🔴 **2026-10-14（第三十会话）就地订正（铁律 5）——「没开始对账」这句已过期**："
    "**这件事早就开工、而且已产出一整轮**。"
    "① **仪器已入仓**：`工具/_a1759_reconcile.py`（605 行）+ `工具/_a1759_report.py`（89 行），**两份都 tracked**"
    "（`git ls-files` 命中 ⇒ `A1936` 那条「仪器住在 `_tmp_view`、别人复算不了」的隐患**已解**）。"
    "② **产出物（现读）**：最新一轮 = `资料/普查产出_第二十九会话/A1937_对账_改后.tsv`（**1821 行 + 表头**）；"
    "口径 / 差异矩阵 / 根因 = `资料/普查产出_第二十八会话/A1759_逐条对账.md`；"
    "枚举词典 = `资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv`（1259 行）。"
    "③ **现读读数（1821 行现算）**：`✅920 / ⚠️367 / 🔴218 / ❓316`；子类 = "
    "`我们缺 152 · 卡不在池 316 · 粒度不同 85 · 承载不同 60 · 拆条差异 102 · 原版重复N次 76 · 我们多 37 · 原版一条含多效果 36 · 取值不同 22 · 条数不同 7 · 纯表现 7 · 纯UI 1`。"
    "🔴 **`A1759_逐条对账.md` 里那套数（🔴335 / 我们缺 269 / `buffOthers` 79 / `createCardInHand` 79 / `triggerPray` 26）【全部过期】** —— 它用的是 13:20 那份 scan（见新账 `A1977`）。"
    "④ **已办掉、⛔ 别重复做的**：`A1876`（22 条「取值不同」逐条照 PnP 成品卡图核完 ⇒ **一条都不改** —— 真值那列是 **2022 快照**、卡被再平衡过）· "
    "`A1915`（58 条 `ACT` 行加闸，🔴 256→218）· `A1937`（伪造的 `trig=Talent` 改成 `Activated`）· `A1942`/`A1947`（`Blank(c)` 丢列已修）。"
    "⑤ 🔴 **最大的一族已经悄悄修掉了**：**`Talent:` 整族现在会解析**（`RuleEngine/Core/EffectText.cs:7321 TryTalent` → `create` op）；"
    "判据 = `_tmp_view/w15_scan.txt`(14:20) 的 `SRC:Talent` **86 处** vs `our_scan.txt`(13:20) 的 **0 处**；"
    "后果读数 = `createCardInHand 我们缺` **79 → 22**、`triggerPray 我们缺` **26 → 2**。"
    "⑥ 🔴 **切块已定（2026-10-14 只读盘点产出）**：仪器块 **`E1`/`E2`/`E3`**（**串行** —— 同一份 `_a1759_reconcile.py`）· "
    "判定块 **`D1`–`D7`**（**可并行只读**，各写各的报告 md，文件零交集）· 落地块 **`F1`–`F4`**（**串行**：解析 / 结算 / 数据 / 光环各一侧）。"
    "🔑 **健康形状 = 并行判定（只读）→ 串行落地** —— 因为 `EffectText.cs`（8899 行）与 `EffectResolver.cs`（8740 行）**各自是单文件**，"
    "⛔ **不能按类型并行派「动手改」的写手**（否则六个写手抢同一个文件）。"
    "⚠️ 判定块 `D7`（**`卡不在池 316`**）**不该混进本对账口** —— 它属 `A1880`/`A970`/`A1800` 那条「取卡规则」线。"
)

A1822_SEG = (
    " 🔴 **2026-10-14 就地订正（铁律 5）**：本条原话是「`SummonCriteria` 只接 summon 分支会**静默漏 135 条**」—— 现核**要改写**："
    "那 135 条里 **62 条与「`SummonCriteria` 挂哪条分支」无关**，病根是 **`Talent:` 正文没进解析**"
    "（**现已修**：`EffectText.TryTalent` → `create` op，详见 `A1759` 那一行 ⑤）⇒ **应写成「`Talent:` 一族」**，"
    "并进 `A1772` 一起判，⛔ **别当独立第二份账**。判据 = `资料/普查产出_第二十八会话/A1759_逐条对账.md` §对账结果。"
)

A1977 = (
    "| 🆕 **`A1977`** | 🔴 **对账仪器写死了一条【过期输入路径】——「拿旧读数当现值」已经实际发生**"
    "（2026-10-14 只读盘点 + 主对话现核）：`工具/_a1759_reconcile.py:21` = `SCAN  = r'd:/4/_tmp_view/our_scan.txt'`，"
    "而本机上 `our_scan.txt` 的 mtime = **10-14 13:20**、`w15_scan.txt` = **14:20** ⇒ **它指向的是 `Talent:` 修复【之前】那一份**。"
    "**后果（已实测）**：同一件对账上 **1782 / 1878 / 1821 三套数字并存**"
    "（`资料/普查产出_第二十九会话/A1915_对账仪器加闸.md:232-243` 专门为这个做过一次「假警报」复核），"
    "而 `A1759_逐条对账.md` 里那套数**全部是旧口径**。⇒ **要做**：把 `SCAN` 指回**仓内**并**带日期命名**（⛔ 别再用 `_tmp_view` 下那份）；"
    "⚠️ **跑 `scan` 会重写基线** ⇒ 属主对话跑（= 切块表里的 `E3`，⛔ 别覆盖 `A1759_逐条对账.tsv`）。判据 = 上述三处现读。 |"
)

A1978 = (
    "| 🆕 **`A1978`** | ⚠️ **对账口径有【两处未验前提】—— 「✅」可能就是这来的**（2026-10-14 只读盘点）："
    "① **`A1915` 那道闸的信用前提没验**：闸读的是**资产侧** `RawCardScript.hasActiveAbility // 0x2A9`，"
    "而原版**运行时**查的是 `CardScript + 0x220`（`CardScript__ResolveActiveAbilityPlayed.c:23`）—— "
    "「**资产侧 → 运行时**忠不忠实」这一跳**没验**（`A1915` §二 自己写明）。"
    "② **`Talent:` 现在解成 `create` op，但原版的结构是 `chosenAbility = 100 + summonCriteria.unitsToSummon`** —— "
    "**两条路不是同一个东西**，只按字面比会**看起来一致**（真值表 = `RuleEngine/Core/RuleCore.cs:8120` + `Core/TalentCardData.cs`）。"
    "⇒ **要做**：**先把这两个前提验了，再信对账的 `✅`**。⚠️ 同族：`hasActiveAbility` 有 **3 张是 `None`**，"
    "代码靠 `c.get()` 真假值判 ⇒ **静默按 0 走**。判据 = 各条出处现读。 |"
)

A1979 = (
    "| 🆕 **`A1979`** | ⚠️ **用户点名的「逐卡真打」那 39 块【没有施工图】**（2026-10-14 只读盘点）："
    "`项目任务.md` §〇 写着「剩『按 `type → 阵营` 切 **~39 块**』」，但 `资料/可并行任务清单.md` 里**没有对应条目**"
    "（`grep` 过 `A1759|A1772|willrun|逐卡|39 块|切块`，命中全是别的行）⇒ 那句**只出现在 `项目任务.md` §〇 与几份历史件里**。"
    "⚠️ **而且它与 `A1759` 是两条【判据不同】的线**：本账要**动态结算**（`ruleprobe play`，真跑一局读数字；"
    "施工方法在 `资料/普查产出_第十四会话/逐卡效果检查_方法与边界.md:82-88`，**12 阵营 × 3 层**），"
    "而 `A1759` 是**静态**（读 JSON + 读 `scan`）⇒ **两件都做（铁律 11），⛔ 别混成一张表**。"
    "⇒ **要做**：给这 39 块补一份施工图（切块 + 每块判据 + 产出物）。⚠️ 它另有一条仍开着的大尾巴 = **`A1965`**（154 张「电池抬不到那个事件」）。判据 = 2026-10-14 现读。 |"
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
    append_seg(TASKS, 'A1759', A1759_SEG)
    append_seg(TASKS, 'A1822', A1822_SEG)
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| ') and '**`A1976`**' in l]
    assert len(hits) == 1
    lines[hits[0] + 1:hits[0] + 1] = ['\n'.join([A1977, A1978, A1979])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1977/A1978/A1979')


if __name__ == '__main__':
    main()
