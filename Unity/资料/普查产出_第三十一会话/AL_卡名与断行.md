# `AL` · 两件：① 8 条同族卡名 ② `desc`（英文）侧的断行

> 写手：`AL` 笔（主对话按铁律 14 从 `AH` §4·3 + §1·5 派下来的两笔）。
> ⛔ 本笔**未动 git** · **未动正本**（四份 + `资料/普查产出_第三十会话/**`）· **未跑任何 Unity 批处理**（用户口径：自检先等等）。
> ✅ 跑过：**秒级类型检查**（运行时 0 错 / 编辑器 0 错）· `python -I -X utf8 工具/gen_cards_engine.py`（**rc=0**，两次）·
> `… --check`（**一致 ✅**）· `bash 工具/ruleprobe.sh check`（读数见 §四）。
> 读数时刻 ≈ **2026-10-15 01:55–02:15**。起点核对：本笔改前的卡池 md5 = `83a37522c2c4839e52c4d9fde80a463b`
> （与 `AH_br与卡名.md` §〇 记的**改后**值逐字相同 ⇒ 起点接得上）。

---

## 〇、一句话结论（两主题各一行）

| # | 件 | 状态 | 一句话 |
|---|---|---|---|
| **一** | 8 条同族卡名 | ✅ **做完**（**8/8 全改，A 组 6 + B 组 2 结论一致**） | 🔴 **8 条全部改向官方 `Card_Name`** —— 连 `AH` 列的两条「两源打架」也是同一结论：**开 PnP 成品卡图亲读，PnP 印的与官方一致**（`Rusted Vent` 单数 / `Telepathic Domination` 带 `h`）⇒ 站我们这边的 `card_ids.json` 真值那两条**是真值自己的错字** |
| **二** | `desc` 断行 | ✅ **做完 38 条**（**但范围要与简报的「46 张」对齐清楚**，见 §2·2） | 🔴 **`\n` 本来就是 `EffectText.Split` 的分句符之一** ⇒ **插在已有 `.` 的接缝上对解析是恒等变换**；真机读数佐证：**31 张受影响卡全在 ruleprobe dump 里，`解析差异` 仍是 1 行（只有既有的 `GSC29`）** ⇒ **op 串零变化** |

**卡池**：`1127` 张 · `v9` · md5 **`83a37522c2c4839e52c4d9fde80a463b` → `392d66475d3bb852885ca2fa02280c73`**
🔬 **改动面是逐卡比出来的**（⛔ 不是推的）：两次产物逐卡 dict 比对 ⇒ **全池只有 46 张变、id 集合逐字相同**，
且每条**只有 1 个字段变** —— **`desc` 38 张 + `name` 8 张**（`id` 集合、`nameZh`/`descZh`/数值/关键词**一处未动**）：

```text
cards changed: 46        only name: 8      only desc: 38
field sets seen: [('desc',), ('name',)]     id sets identical: True
```

---

## 一、主题一：8 条同族卡名（**三源逐源现核**）

### 1·1 逐条（本笔**没改** `card_ids.json` —— 那不在白名单，见 §五·1）

| id | 改前（我们） | 官方 `Card_Name/<id>`（现读） | `card_ids.json` 真值（现读） | PnP 卡面（本笔开图） | 结论 |
|---|---|---|---|---|---|
| `BL36` | `Sylar Hexcorn` | **`Sylar Hexscorn`** | `Sylar Hexscorn` | （A 组未开图；`ocrName` 一直是 `Sylar Hexscorn`） | ✅ 改 → `Sylar Hexscorn` |
| `DA6` | `Sergeant Naaman` | **`Sergeant Taaman`** | `Sergeant Taaman` | （同上；`ocrName` = `Sergeant Taaman`） | ✅ 改 → `Sergeant Taaman` |
| `SAU38` | `Extermination Protocols` | **`Extermination Protocol`** | `Extermination Protocol` | — | ✅ 改 → `Extermination Protocol` |
| `SAU39` | `Reconstitution Protocols` | **`Reconstitution Protocol`** | `Reconstitution Protocol` | — | ✅ 改 → `Reconstitution Protocol` |
| `SOR10` | `Sisters Repentia` | **`Sister Repentia`** | `Sister Repentia` | — | ✅ 改 → `Sister Repentia`（⚠️ 见 §1·3 那段历史） |
| `TAU42` | `Experimental Drone` | **`Experimental Drones`** | `Experimental Drones` | — | ✅ 改 → `Experimental Drones` |
| `GSC64` | `Rusted Vents` | **`Rusted Vent`** | `Rusted Vents`（🔴 真值站我们） | 🖼 **`d:/2/Warpforge部队卡片/Genestealer Cult/5防御卡/Warpforge_68_Rusted-Vent.png`** —— 卡面印 **`Rusted Vent`**（单数） | ✅ 改 → `Rusted Vent` |
| `GSC49` | `Telephatic Domination` | **`Telepathic Domination`** | `Telephatic Domination`（🔴 真值站我们、两边都漏 `h`） | 🖼 **`…/Genestealer Cult/4计策/Warpforge_64_Telepathic-Domination.png`** —— 卡面印 **`Telepathic Domination`**（带 `h`） | ✅ 改 → `Telepathic Domination` |

**B 组那两条的判据（铁律 7：开图亲读，⛔ 不看文件名）**：两张图的文件名与卡面**一致**，而且
**官方 `Card_Name` 也一致** ⇒ **两票对一票** ⇒ 真值（`card_ids.json`）那两条是它自己的错字。
（`GSC64` 卡面另印着 `Defence` / 费用 `0` / 效果文字「Create a random troop with **Ambush** in your hand. It costs 2 less」，
与本笔无关，顺带核过与我们的 `desc` 逐字相同。）

### 1·2 三张表同步（**判据：改完「孤立键」必须为 0**）

一处卡名要在**三张按卡名查的表**上同步（漏一张就**静默失效**）：

| 表 | 落点 | 改了几处 |
|---|---|---|
| `数据/游戏数据/card_stats.json` | `name` 字段（**源表**） | **8 行**（`"name": "<旧>",` → `"name": "<新>",`，每处锚点**全行**、改前 `count==1` 断言） |
| `数据/卡牌翻译/zh_cards.json` | `cards` 的**键** | **8 处**（7 处改键 + 1 处 `Rusted Vents`→`Rusted Vent` 的**键名合并**，见下） |
| `数据/游戏数据/cardface_fixes.json` | **5 个列**里所有按卡名查的键 | **10 处**：`desc` 1（`Sergeant Naaman`）· `descZh` 1（`Sisters Repentia`）· `subtype` 1（`Sisters Repentia`）· `keywords` 3 + `_manual_keywords` 3（`Experimental Drone`/`Rusted Vents`/`Telephatic Domination`） |

⚠️ **`cardface_fixes.json` 比简报说的多 3 列** —— 简报只点了「键」，实际生成器 `load_cardface_fixes` 读的是
**`subtype` / `keywords` / `desc` / `descZh` 四列**（`工具/gen_cards_engine.py:527`），另有 `_manual_keywords`
（**`工具/gen_cardface_fixes.py` 会把它落回 `keywords`**）⇒ **不改就是给下一次重跑留了个定时炸弹**。
🔴 **照 `TAU51` 那次「只改 `descZh` 一个键」做会漏 4 个键**（`SOR10` 那张在 3 个列里都有）。

⚠️ **`zh_cards.json` 的 `Rusted Vent` 键原本就存在**（是个**孤儿键**，`n` = `锈蚀风口`），
`Rusted Vents` 那条 `n` = `锈蚀通风口`。本笔取**「改名 = 只动键不动值」** ⇒ **把旧键那条的值搬到新键名下、
删掉旧键**（见 §三 的行数与 `git diff` 读数）。**两个 `n` 值在产物里都不可见**（`nameZh` 由官方
`Card_Name/GSC64` = `生锈通风口` 赢），本笔逐卡比对已证 **`nameZh` 一处未动**。

### 1·3 `SOR10` 那张压在「19 条改名已回滚」上的历史（**先读后改**）

简报点名「改前必读 `工具/gen_cards_engine.py:325-345`」。读完的结论：那段写的是
**2026-09-24 我拿「PnP 卡图 + 第三方复刻网站 + 我们自己的 `zh_CN.csv`」当证据改了 19 条卡名、当天全回滚**，
根因是**那两个来源都是从 PnP 派生的、不独立**，而 `zh_CN.csv` 是**我们自己的 Godot 复刻**同步来的（非官方）。
🔴 而**本笔的判据不是那两个来源**：是 **2026-10-14 才解开的官方远端本地化包**（`数据/游戏数据/官方本地化_en+zh-CN.json`，
来自 Addressables `localization_assets_all`）**+ `card_ids.json` 的真值快照**，`SOR10` 两张票都指向 `Sister Repentia`。
⇒ **`已裁⑤`（卡名一律以官方 `Card_Name` 为准）在本条上成立，且不与那段历史冲突**（那段禁的是「只凭 PnP / 第三方改」）。
⚠️ 但**如实记**：`zh_CN.csv` 里那条键仍是 `Sisters Repentia,~,忏悔修女`（**没跟着改**，
理由同 `AH` §六·3：它是 `sync_from_d2.py` 从 `d:/warpforge/data/i18n` 同步来的派生件、本地改了下次会被盖回去）。

### 1·4 验收读数（逐条跑过）

- `卡面修正表：所有键都匹配上了 ✅` —— **没有孤立键**（这就是三表必须同步的直接证据）
- `同卡两键一致性 ✅`（裸卡名键与 `阵营/卡名` 键没有对同一列给出不同的值）
- `裸卡名键唯一性 ✅`（每个裸键的卡名在全池只出现一次）⇒ 没踩 `_bad`（`_bad` 为真 ⇒ **rc=1、一个字节都不写**）
  ⚠️ 加键前**先确认过唯一性**：改后每个新名字在全池**只有 1 张**（`Sylar Hexscorn`→`BL36` · `Sergeant Taaman`→`DA6` ·
  `Extermination Protocol`→`SAU38` · `Reconstitution Protocol`→`SAU39` · `Sister Repentia`→`SOR10` ·
  `Experimental Drones`→`TAU42` · `Rusted Vent`→`GSC64` · `Telepathic Domination`→`GSC49`）
- ✅ **8 张的 id 一处未变**（与 `AH` 的实测一致：`pick_truth_id` 名字先用 `name`、再退 `ocrName` ⇒ 改名不连带换 id）
  ⇒ **「以我方 id 为键的表」一条都不用重出**（id 集合逐字相同，`--check` 也证明产物是不动点）

---

## 二、主题二：`desc`（英文）侧的断行

### 2·1 🔴 会不会改变解析 —— **判据在 `EffectText.Split`，答案是「按本笔的插入法不会」**

`RuleEngine/Core/EffectText.cs:1004-1020`（现读）：

```csharp
foreach (var raw in desc.Split(new[] { '.', '\n', '\r' }))   // ← 换行【本来就是】分句符之一
{ string s = raw.Trim(); if (s.Length > 0) list.Add(s); }
```

⇒ 🔴 **`\n` 是句子边界**（2026-09-13 `A4` 加的，理由：卡面印不下时后一句挪到下一行且**不一定带句号**）。
**所以「随手加换行」真的会改 op 串** —— 但**本笔的插入法在结构上不可能改**：

> **只在 `desc` 里【本来就已经有 `.`】的接缝上插 `\n`**（把 `. ` 换成 `.\n`）。
> 因为 `Split` 对每一段都 `Trim()`，`Split("A. B") == Split("A.\nB") == ["A","B"]` —— **恒等**。

**两重证据**：
1. **本笔逐卡 `assert split(old) == split(new)`**（38/38 全过，脚本里硬断言，不过就抛）；
2. 🔵 **真机读数**（`ruleprobe check`）：本笔改的 38 张里 **31 张在 `工具/ruleprobe/基线/out_baseline.txt` 里有行**
   （现查 31/31 命中），而 `解析差异` 仍是 **1 行 = `GSC29|Metamorph Iconbearer`**（**改前就有的那 1 行**）
   ⇒ **op 串零变化**。`新增 172 / 消失 171` 与 `AH` 那笔逐字相同。

### 2·2 ⚠️ 范围厘清：简报的「46 张」与本笔实际动手的「38 张」

本笔按**现象本身**重新量了一遍（⛔ 不照抄 `AH` §1·5 的 46）：

```text
官方 Card_Description/<id> EN 含 <br> 的卡（在我们池里的）      97 张
  ├─ 我们 desc 已有换行                                          10 张   ⇒ 无需动（见 §2·4 逐张核过）
  └─ 我们 desc 无换行                                            87 张   ← 待判
        ├─ ✅ 能定位到接缝、已插换行                             38 张
        └─ ⛔ 无接缝可插（见下）                                 49 张
```

**为什么 87 而不是 46**：`AH` 那句「53 张里官方 EN 有 51 张带 `<br>`，`desc` 只有 7 张带换行 ⇒ 约 46 张」，
它的**分母是「`descZh` 里带 `<br>` 的 53 张」** —— 那是 `AH` 顺手看到的那一撮。现算：
`descZh` 含 `<br>` 的 53 张与「官方 EN 含 `<br>`」的 97 张**只交 51 张**（`E−Z = 46` 张是**中文侧也没有 `<br>`** 的）。
**「英文档下卡面把两段并成一行」这件事与中文侧有没有 `<br>` 无关** ⇒ **正确的范围是 87**，`46` 是子集。
（`AH` 那个 46 恰好等于 `E−Z`，数目对得上、口径不同。）

**那 49 张为什么「无接缝可插」（⛔ 不是漏做）**：官方那条 `<br>` 落在**我们 `desc` 里根本没有的那一段之后** ——
最典型的 `[[ephemeral]].<br>…` / `[[waystone]].<br>…` / `[[flying]]. [[flank]].<br>…`：
**那些关键词段我们这一代 `desc` 根本不写**（它们走 `keywords` 列、由卡面的关键词图标行渲染），
`desc` 直接从效果那句开始 ⇒ **前面没有东西可以断**。逐条看过，**没有一张是「该断而没断」**。
（同族的还有 `[[vanguard]].<br>` · `[[unstable]]<br>` · `[[tide]] 1<br>` · `[[armour]] 1. [[blast]] 5<br>` 等。）

**「官方几段」≠「我们几段」时怎么对齐的**（本笔的做法，可复核）：
把官方 EN 按 `<br>` 切成段、段内再按 `.` 切句；把我们 `desc` 按 `EffectText.Split` 的规则切句；
用 `difflib.SequenceMatcher` 在**词序列**上做 `desc` 句 → 官方句的指派（取相似度 argmax，**阈值 0.15**，低于就当「没指派」）；
**只有当相邻两句各自指派到「官方不同的段」时，才在它们之间的 `.` 接缝上插 `\n`**。
⚠️ **阈值不是装饰**：`GOF26 Stompa` 的第 4 句 `Rally: Stun a random enemy.` 在官方 EN 里**整句不存在**
（现读 `Card_Description/GOF26` = `[[armour]] 2<br>[[concussive]]<br>[[unstable]]`，**只有 3 段**），
它的最佳相似度 = **0.0** —— 不设阈值就会在 `Unstable.` 与 `Rally:` 之间**多插一刀**；
**PnP 卡图证明那一刀是错的**（见 §2·3 那张图）。

### 2·3 抽查（**开 PnP 成品卡图亲读**，铁律 7）—— 6/6 与官方 `<br>` 位置一致

| # | 卡 | PnP 文件（开图亲读） | 卡面印的 | 与官方 `<br>` | 与我们的 NEW |
|---|---|---|---|---|---|
| 1 | `ASH4 Guardian Defender` | `Aeldari/3部队/Warpforge_7_Guardian-Defender.png`（`AH` §1·2 已亲读） | `〔图〕Waystone.` ⏎ `〔图〕Shuriken 2`（第一行右边**空着一大片** ⇒ 显式断行） | `[[waystone]].<br>[[shuriken]] 2` ✔ | `Waystone.\nShuriken 2` ✔ |
| 2 | `UM8 Eliminator` | `Ultramarines/3部队/Warpforge_17_Eliminator.png` | `〔图〕Camouflage` ⏎ `〔图〕Sniper` | `[[camouflage]]<br>[[sniper]]` ✔ | `Camouflage.\nSniper` ✔ |
| 3 | `GOF26 Stompa` | `Orks/3部队/Warpforge_39_Stompa.png` | `〔图〕Armour 2` ⏎ `〔图〕Concussive` ⏎ `〔图〕Unstable 〔图〕Rally: Stun a random enemy` | `[[armour]] 2<br>[[concussive]]<br>[[unstable]]` ✔ | `Armour 2.\nConcussive.\nUnstable. Rally: Stun a random enemy.` ✔（**Unstable 与 Rally 之间不断** ✔＝阈值那条的证据） |
| 4 | `SOR50 Relics of St. Katherine` | `Sorotitas/4计策/Warpforge_60_Relics-of-St.-Katherine.png` | `Gain 3 ☀.` ⏎ `12 ☀: Draw 3 cards` | `((Gain_faith_x,3)).<br>((Faith_cost_x,12)): …` ✔ | `Gain 3 ☀.\n12 ☀: Draw 3 cards` ✔（**连句号的位置都对上**） |
| 5 | `SOR71 Adelaide the Serene` | `Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png` | `〔图〕Flying.` ⏎ `6 ☀ Gain 〔图〕Flank and 〔图〕Shield.` ⏎ `〔图〕Rally: If you have less than 6 ☀, gain 1 ☀ for each enemy unit` | `[[shield]]. [[flying]].<br>((Faith_cost_x,6)) Gain [[flank]].<br>[[rally]]: …` | `6 ☀ Gain Flank and Shield.\nRally: …` ✔（**第 1 与第 2 段在我们这代 `desc` 里是同一句** ⇒ 那两段之间的 `<br>` 落在句内部、按口径**不插** ✔） |
| 6 | `ASH64 Devoted of Khaine` | `Aeldari/4计策/Warpforge_53_Devoted-of-Khaine.png` | `Give +1 Melee Attack to your troops.` ⏎ `〔图〕Repeat this effect` | `((Give_x_melee_to_your_troops,+1)).<br>((Spirit_x,1)) ((Repeat_this_effect))` ✔ | `Give +1 [Melee] to your troops.\n1 [Spirit Stone]: Repeat this effect` ✔ |

⇒ **6/6 一致**（简报要求 3~5 张）⇒ **批量补**。

### 2·4 那 10 张「`desc` 已经有换行」的（⛔ 一张没动，但逐张核过位置对不对）

| 卡 | 官方 `<br>` | 我们 `\n` | 判读 |
|---|---|---|---|
| `ASH49` · `ASH38` · `DA41` · `SOR48` · `SOR38` · `UM12` | 1 | 1 | ✔ 一一对上（`UM12` 那条官方 `[[flank]]` 后**连句号都没有**，我们也写成 `Flank\n`，位置一致） |
| `SOR56 Beacon of Faith` | 2 | 2 | ✔ |
| `d3e99cb18b… Witchfire` | 2 | 1 | ✔ 官方第 1 刀在 `[[ephemeral]].` 之后 —— **那一段我们 `desc` 不写** |
| `65cfec7d2b… Lead the Righteous` | 2 | 1 | ✔ 同上（第 1 刀在 `[[ephemeral]].` 后） |
| `8901e35c5d… Fury of the Righteous` | 3 | 2 | ✔ 同上；官方第 4 段（`((Faith_cost_x,7)): [[stun]] the target`）在我们这一代 `desc` 里**没有**（属另一笔） |

### 2·5 逐条（38 条，`OLD → NEW`）

| id | 卡 | 改前 | 改后 |
|---|---|---|---|
| `ASH4` | Guardian Defender | `Waystone. Shuriken 2` | `Waystone.\nShuriken 2` |
| `ASH7` | Veteran Guardian | `Waystone. Shuriken 1` | `Waystone.\nShuriken 1` |
| `ASH19` | Witchblade Warlock | `Waystone. Talent: Witchfire` | `Waystone.\nTalent: Witchfire` |
| `ASH34` | Avatar of Khaine | `Armour 2. 2 [Spirit Stone]: Gain Blast 6 and Camouflage. Talent: Wrath of Khaine.` | `Armour 2.\n2 [Spirit Stone]: Gain Blast 6 and Camouflage.\nTalent: Wrath of Khaine.` |
| `ASH64` | Devoted of Khaine | `Give +1 [Melee] to your troops. 1 [Spirit Stone]: Repeat this effect` | `Give +1 [Melee] to your troops.\n1 [Spirit Stone]: Repeat this effect` |
| `9c3562ffa183ede41b1626d9c9dc1e2f` | Path of Command | `Ephemeral. Deploy a Storm Guardian` | `Ephemeral.\nDeploy a Storm Guardian` |
| `09bcb1b3b8794c4429d82d4b2e68fa1f` | Path of the Seer | `Ephemeral. Choose a card in your hand and return it to your deck. Draw a card. Gain 1 Spirit Stone` | `Ephemeral.\nChoose a card in your hand and return it to your deck. Draw a card. Gain 1 Spirit Stone` |
| `1bfe0893592c9dd40bed8d68d5d2b516` | Path of the Warrior | `Ephemeral. Give Shuriken 1 to your units this turn` | `Ephemeral.\nGive Shuriken 1 to your units this turn` |
| `ASH53` | Reclaim the Stars | `Draw 5 cards. 5 [Spirit Stone]: Reduce their cost by 5` | `Draw 5 cards.\n5 [Spirit Stone]: Reduce their cost by 5` |
| `ASH54` | Wailing Doom | `Deal 8 damage to an enemy. 2 [Spirit Stone]: Deal 4 damage to units adjacent to the target` | `Deal 8 damage to an enemy.\n2 [Spirit Stone]: Deal 4 damage to units adjacent to the target` |
| `ASH41` | Webway Gate | `Choose a troop from your deck and draw it. 1 [Spirit Stone]: It costs 2 less` | `Choose a troop from your deck and draw it.\n1 [Spirit Stone]: It costs 2 less` |
| `33eabec155f628440a426a4ba480ec24` | Wrath of Khaine | `Give +2 [Melee] and +2 [Ranged] to your units. 2 [Spirit Stone]: Repeat this effect` | `Give +2 [Melee] and +2 [Ranged] to your units.\n2 [Spirit Stone]: Repeat this effect` |
| `DA10` | Black Knight | `Flank. Slay: Gain 1 Quest Point` | `Flank.\nSlay: Gain 1 Quest Point` |
| `DA7` | Ravenwing Bikes | `Flank. Agenda: Gain 1 Quest Point` | `Flank.\nAgenda: Gain 1 Quest Point` |
| `GOF23` | Ardshell Gurk | `Armour 2. Vanguard. Talent: Follower of Gork` | `Armour 2. Vanguard.\nTalent: Follower of Gork` |
| `GOF66` | Rokkit Boy | `Unstable. Blast 3` | `Unstable.\nBlast 3` |
| `GOF6` | Stormboy | `Tide 1. Flying` | `Tide 1.\nFlying` |
| `GOF22` | Deff Dread | `Unstable. Mob: Deal 1-3 damage to a random enemy` | `Unstable.\nMob: Deal 1-3 damage to a random enemy` |
| `GOF10` | Grot Tank | `Unstable. Armour 1` | `Unstable.\nArmour 1` |
| `GOF26` | Stompa | `Armour 2. Concussive. Unstable. Rally: Stun a random enemy.` | `Armour 2.\nConcussive.\nUnstable. Rally: Stun a random enemy.` |
| `SOR71` | Adelaide the Serene | `6 ☀ Gain Flank and Shield. Rally: If you have less than 6 ☀, gain 1 ☀ for each enemy unit.` | `6 ☀ Gain Flank and Shield.\nRally: If you have less than 6 ☀, gain 1 ☀ for each enemy unit.` |
| `SOR23` | Canoness | `2 ☀: Gain Armour 1. 4 ☀: Gain +2 [Attack] and +2 [Ranged]` | `2 ☀: Gain Armour 1.\n4 ☀: Gain +2 [Attack] and +2 [Ranged]` |
| `SOR34` | Celestian Superior | `Pray: Deploy a Celestian. 6☀: Deploy a Celestian` | `Pray: Deploy a Celestian.\n6☀: Deploy a Celestian` |
| `SOR22` | Dominion Superior | `Rally: Deal 1 damage. 2 ☀: Deal 1 additional damage` | `Rally: Deal 1 damage.\n2 ☀: Deal 1 additional damage` |
| `SOR9` | Preacher | `Talent: Hymn of Battle. 5 ☀: Gain Shield` | `Talent: Hymn of Battle.\n5 ☀: Gain Shield` |
| `SOR53` | Blade of Faith | `Deal 3 damage to an enemy troop. 4 ☀: Repeat this effect` | `Deal 3 damage to an enemy troop.\n4 ☀: Repeat this effect` |
| `SOR58` | Devout Warriors | `Draw a troop. 2 ☀: Draw an additional troop` | `Draw a troop.\n2 ☀: Draw an additional troop` |
| `SOR51` | Divine Intervention | `Destroy the enemy troop with highest Attack. 8 ☀: Destroy all enemy troops` | `Destroy the enemy troop with highest Attack.\n8 ☀: Destroy all enemy troops` |
| `SOR54` | Fiery Conviction | `Give +2 [Might] to your units until your next turn. 4 ☀: Give an additional +1 [Might]` | `Give +2 [Might] to your units until your next turn.\n4 ☀: Give an additional +1 [Might]` |
| `SOR47` | Miraculous Feat | `Give [Invulnerable] Invulnerable to a friendly unit this turn. 6 ☀: Extend effect until your next turn` | `Give [Invulnerable] Invulnerable to a friendly unit this turn.\n6 ☀: Extend effect until your next turn` |
| `SOR50` | Relics of Saint Katherine | `Gain 3 ☀. 12 ☀: Draw 3 cards` | `Gain 3 ☀.\n12 ☀: Draw 3 cards` |
| `SOR55` | Trial of Suffering | `Deal 1 damage to a friendly troop and give it Armour 1. 2 ☀: Give it Armour 2 instead` | `Deal 1 damage to a friendly troop and give it Armour 1.\n2 ☀: Give it Armour 2 instead` |
| `UM27` | Assault Centurion | `Vanguard. Armour 2` | `Vanguard.\nArmour 2` |
| `UM23` | Bladeguard Lieutenant | `Armour 1. Vanguard. Codex: Heals 3` | `Armour 1. Vanguard.\nCodex: Heals 3` |
| `UM8` | Eliminator | `Camouflage. Sniper` | `Camouflage.\nSniper` |
| `UM1` | Scout | `Camouflage. Rally: Deal 1 damage to an enemy` | `Camouflage.\nRally: Deal 1 damage to an enemy` |
| `UM30` | Repulsor Executioner | `Blast 4. Rally: Deal 8 damage to an enemy troop.` | `Blast 4.\nRally: Deal 8 damage to an enemy troop.` |
| `SOR44` | Moment of Grace | `Give +2 [Attack] and +2 [Ranged] to a friendly unit this turn. 5 Energy: Draw a card` | `Give +2 [Attack] and +2 [Ranged] to a friendly unit this turn.\n5 Energy: Draw a card` |

### 2·6 ⛔ 只补了换行（**一字未改别的**）

- **措辞/数值/句数一个字都没动**：⛔ 没把官方那一代多/少的句子抄进来、⛔ 没改任何数字
  （有几张官方那句与我们的**数值不同** —— 例如 `SOR23` 官方 `Faith_cost 3/6` 而我们 `2 ☀/4 ☀` —— **一律留原地**，见 §五·3）。
- **只改 `desc`**：`descZh` / `keywords` / 数值列**一处未动**（逐卡 dict 比对已证：46 张里 38 张只有 `desc` 一个字段变）。
- **落点分两处**（谁是真源就改谁，⛔ 不改生成物以外的副本）：**38 张里 `cardface_fixes.json` 的 `desc` 20 张、
  `card_stats.json` 的 `desc` 18 张** —— 逐张比过「生效源 == 卡池值」，**0 处不符**。

---

## 三、改了哪些文件哪些行（**全部 python 字节级 `wb`，⛔ 未用 `sed -i`**）

| 文件 | 改动 | 行尾 |
|---|---|---|
| `Unity/数据/游戏数据/card_stats.json` | `name` 8 行 + `desc` 18 行 = **+26 / −26** | 改前改后 **CRLF 36003 / LF 36003（纯 CRLF）**· `1172146 → 1172163` B |
| `Unity/数据/游戏数据/cardface_fixes.json` | `desc` 20 行 + 键 10 行 = **+30 / −30** | **CRLF 1912 / LF 1912（纯 CRLF）**· `192482 → 192500` B |
| `Unity/数据/卡牌翻译/zh_cards.json` | 键 7 行 + `Rusted` 合并 4 行 = **+7 / −11** | **CRLF 4937 / LF 4937（纯 CRLF）**· `151505 → 151368` B |
| 生成物 `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` | 重跑覆盖 | 单行 JSON（`1 1`）· `728 KB` |

- 每处替换**前后都打了 `count` 并 `assert == 1`**（`rep()` / `rep_in_card()`），锚点取**全行**或**「卡名 + 整段值」**；
  `card_stats.json` 的 `desc` 锚点**先按所属卡对象切片再替换**（因为 `"desc": "Waystone. Shuriken 2"` 这种串**文件里有 2 处**，
  只按值定位会命中错的卡 —— 第一版就撞上了，已改）。
- 三份 JSON 改后都 `json.loads` 回读通过；另做了**产物↔源表逐卡回读比对**（38/38 一致）。
- ⛔ **没碰**：`工具/gen_cards_engine.py`（**只读**）· `工具/import_original_art.py` · `RuleEngine/**` · `CardPresentation/**` ·
  `card_ids.json` · `官方本地化_en+zh-CN.json` · 四份正本。
- 🗂 **改前副本**留在 `%TEMP%\al\bak\`（三个源表 + 改前卡池），要回滚拿它比。

---

## 四、验收读数（照抄，可复核）

```text
$ TMPDIR=/tmp/wf_ah2 bash 工具/typecheck.sh   ⇒ 运行时错误数: 0 · 编辑器错误数: 0   （本笔没动 .cs，照跑）
$ python -I -X utf8 工具/gen_cards_engine.py  ⇒ rc=0（跑了两次，产物逐字节相同）
  卡面修正表：所有键都匹配上了 ✅
  同卡两键一致性 ✅（裸卡名键与 `阵营/卡名` 键没有对同一列给出不同的值）
  裸卡名键唯一性 ✅（每个裸键的卡名在全池只出现一次）
  真值快照     卡池 id 能在快照里找到 1009/1127 张（与 AH/AD 那两笔逐字相同）
  卡 id        用原版 1104 张（真值索引那一层 1009）· 自造 23 张（共 1127）
  写出 …/cards_engine.json (728 KB)
$ python -I -X utf8 工具/gen_cards_engine.py --check  ⇒ rc=0 · 「一致 ✅」（幂等 = 不动点）
$ bash 工具/ruleprobe.sh check  ⇒ rc=1（**已知**：GSC29 那 1 行 + ⑦ 指纹哨兵未钉；与 AB/AD/AH 那几笔同形）
  全池 1121 张 · 解析差异 **1** 行 · 改名 **2** 行 · 新增 172 · 消失 171
  ⇒ 「改名 2 行」= 本笔 8 张里**只有 `GSC64`/`GSC49` 在这份 dump 里**（现查 8 个 id 中唯二有行）✔ 不是多出来的差异
  ⇒ 「解析差异 1 行」= **与改前逐字相同**（`GSC29|Metamorph Iconbearer`）⇒ **38 张的换行对 op 串零影响**
$ 卡池 MyGame/Assets/RuleEngine/Resources/cards_engine.json
  1127 张 · v9 · md5 83a37522c2c4839e52c4d9fde80a463b ⇒ 392d66475d3bb852885ca2fa02280c73
  desc 含换行：24 ⇒ **62**（+38）
$ 三份源表 md5（改后）：card_stats a47c50bec85e5a2d4683bf2db77087d3 ·
  cardface_fixes 28de924ee80554b12dd495148428ba60 · zh_cards 52f4741d71fb497853949ceaf3a87264
```

⚠️ **本笔【没跑】任何 Unity 批处理**（简报明写）⇒ **卡面实际渲染出来是几行，只有真机/`RuleEngineTest` 能验**。
本笔能给的最强证据是：**TMP 侧**（`AH` §1·1 已证 `<br>`→换行 与 `\n` 同粒度）+ **PnP 6/6 亲读** + **解析零变化**。

---

## 五、顺手发现（**只报，⛔ 一条都没改**）

1. 🔴 **`card_ids.json`（真值表）自己带着两处错字**：`GSC64` = `Rusted Vents` · `GSC49` = `Telephatic Domination`
   —— 与**官方 `Card_Name` 与 PnP 卡面三方**都不符（§1·1）。**本笔没改它**（不在白名单），但它**是
   `card_id_truth_index.json` 的输入** ⇒ 「从真值名反查我方卡」这条路对这两张仍会指向旧名。**要修就得改真值表**，请点名。
2. 🔴 **`cardface_fixes.json` 的 `keywords` 里有一个【重复键】**：`"Fenrisian Drop Pod"` 在同一个对象里出现 **2 次**
   （现扫；JSON 语义 = 后者覆盖前者，所以**前一条静默失效**）。**直接证据**：该文件**不是 `json.dumps` 能逐字节往复的**
   （本笔试过 `indent=1` 回写 ⇒ 重复键会被合掉、位置也会变）⇒ **本笔全部按字节改、⛔ 没用结构重写**。
   同族的还有：`card_stats.json` 有一行缩进是 **4 空格**（`:253621` 附近的 `"desc"` 行）⇒ 也证明不能结构重写。
3. ⚠️ **13 处「官方宏值 vs 我们 `desc` 数字」对不上**（现扫 `((Faith_cost_x,N))` / `((Spirit_x,N))` 那一族）——
   例：`SOR23` 官方 `3/6` vs 我们 `2 ☀/4 ☀` · `SOR53` 官方 `5` vs `4` · `SOR58` 官方 `3` vs `2` ·
   `SOR47` 官方 `5` vs `6` · `SOR48` 官方 `5` vs `4` · `SOR12` / `SOR67` / `SOR73` / `Daemonbreaker` / `Fury of the Righteous` 同理。
   本笔**一律没动**（简报点名「本笔只补断行位置、别抄官方那一代」）⇒ **这是另一笔「付费值对账」**，
   判据要另找（PnP 卡面那枚 ❂ 数字是印刷品上的字，铁律 7 可逐张开）。
4. ⚠️ **23 张的 `descZh` 也缺 `<br>`**（官方 zh-CN `Card_Description/<id>` 有、我们 `descZh` 没有）——
   例：`ASH17` · `ASH8` · `ASH14` · `ASH20` · `ASH34` · `ASH64` · `ASH35` · `SOR71` · `SOR19` · `SOR56` ·
   `UM23` · `UM22` · `UM9` · `SOR44` …（现扫 23 个 id）。⇒ **中文档下同样会并成一行**，**同一族、another 笔**。
   （⚠️ 与 §2·5 的 38 张**部分重叠** —— 那 38 张改的是 `desc`，这 23 张要改的是 `descZh`。）
5. ⚠️ `GOF26 Stompa` 的**官方 EN `Card_Description` 比 PnP 少一整句** —— 官方 = `[[armour]] 2<br>[[concussive]]<br>[[unstable]]`，
   而 **PnP 卡面印着 `Rally: Stun a random enemy`**、我们也一直有这句 ⇒ **官方那一代这张是残的**。
   （这正是简报说的「官方那句旧一代的措辞**多/少句**」那条 —— **本笔没抄它**，只补断行。）
6. ⚠️ `card_stats.json` 的 `noise_reason` 里那两条注释**仍写 `Rusted Vents(GSC68)`** —— **没跟着改**，
   理由：它同句还在引「`card_ids.json` 把 GSC68 指给 `Rusted Vents`」，**而那句现在仍然成立**（同 §五·1）⇒ 改了反而变成假陈述。
7. ℹ️ 本笔用「**只补断行、不改措辞**」这个口径时**天然放弃了**一类修复：官方 `<br>` 落在**我们这一代没有的那一段**之后
   （49 张）—— 那不是「断行」问题，是「**我们少了一整段关键词行/效果句**」，属 `A970` / 卡面线。**只报**。
8. 🔴 **全仓「按卡名查的表」重新盘了一遍**（本笔自己扫的，⛔ 不是转述）：**生成器只读 13 个源**
   （`工具/gen_cards_engine.py:30-1167` 的 `*_SRC`），其中**按卡名查的只有那三张**（`card_stats` 的 `name` 字段 /
   `zh_cards` 的键 / `cardface_fixes` 的 4 列键）—— 其余 10 个源**全是按 id 或按真值名**
   （`card_ids` / `card_id_truth_index` / `card_inventory_options` / `card_initiative` /
   `warlord_goes_second` / 官方本地化 / 宝石稀有度表 / `d:/2` 那份真值快照）⇒ **本笔不必动它们**（现核：这些表里**0 个**卡名键）。
   ⚠️ **另有 3 张「卡名当【值】、不是键」的表**（不阻塞，但**都是旧拼法的存量**，属 `CardPresentation/**` ⇒ 本笔不碰）：
   · `CardPresentation/Resources/voice_lines.json` —— 按 **id** 存、`"name"` 是**显示名副本**（`BL36` 那条仍写 `Sylar Hexcorn`；
     同表里 `VO_DA_Sergeant Naaman - ….ogg` 那些是**真文件名**，⛔ 不能改）
   · `CardPresentation/Resources/profile_cosmetics.json` —— 头像货架的 `"n"`（`Sergeant Naaman`；官方表
     `Avatars/Avatar_DA_Sergeant Naaman` 的 **en 列就是 `Sergeant Taaman`** ⇒ 我们那句是旧的）
   · `cards_engine.json` 的 `fullHeroSpriteReference`（`UI_Deck_Warlord_Sylar Hexcorn`）—— **原版美术资产名**，⛔ 不该改
9. ⚠️ `数据/游戏数据/portraits.json` 的 `heroes` 是**按督军卡名**做键（含 `Sylar Hexcorn`），值是 `res://` 死路径 ——
   **Godot 期的遗留件，全仓 0 个消费者**（现扫 `MyGame/Assets/**` 与 `工具/**` 的 `.cs`/`.py` 全无命中）⇒ 本笔未动。

---

## 六、没查清 / 交回主对话

1. 🔴 **卡面实际渲染**没验（不许跑 Unity 批处理）⇒ 建议在下次收口时拿 `CardBaseDemo.Run` 或真机
   渲 2~3 张（`UM8 Eliminator` / `GOF26 Stompa` / `SOR50`）与 PnP 并排看一次（铁律 10 第 6 条）。
2. ⚠️ **§1·2 那个多出来的 4 个键**（`subtype` / `keywords` / `_manual_keywords`）建议回写进「三张表同步」那份口径里
   （下次改名时别漏）。
3. ⚠️ **§五·1（真值表两处错字）** 与 **§五·3（13 处付费值对账）** 要不要立账，请主对话裁。
