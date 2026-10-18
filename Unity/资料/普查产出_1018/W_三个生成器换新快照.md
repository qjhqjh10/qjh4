# W · 三个派生生成器：旧快照 → 当前 build allcards（2026-10-18）

> 执行代理产出。判据 = `资料/普查产出_1018/R_卡牌线_完整ID与SO后的调整评估.md` §4 代码调整表。
> 范围：只改三个 `.py` 与它们的六份产物；⛔ 没碰 `cards_engine.json` / `card_stats.json` / 描述文本 / 任何 `.cs`。

## 1. 三个生成器各自

| 生成器 | 旧真值源 | 新真值源 | 改前 → 改后 |
|---|---|---|---|
| `工具/gen_talent_cards.py` | `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（2025-07-30 旧快照，1,195 张） | `资料/普查产出_1018/allcards_1350/cards_raw.json`（当前 build，1,351 张） | 全量件 `truthCards` **97 → 105** · `mapped` **96 → 103** · `unmapped` **1 → 2** · 运行时件 `count` **89 → 96**（+7） |
| `工具/gen_related_cards.py` | `d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1…/…/__data`（UnityPy 读，2025-07-30 旧快照） | 同上 `cards_raw.json`（**不再需要 UnityPy**，四槽 `relatedCard1..4` 在 `full_raw` 里） | 全量件 **196 张 / 240 边 → 212 张 / 266 边**；运行时件 **176 张 / 214 边 → 191 张 / 239 边**；`skipped` 统计 **14 → 27 条边**（源认不出 20 + 目标认不出 7） |
| `工具/gen_ability_anim_durations.py` | `_sources.cardSoTruth` 指 `cards_1195_2022时代.json`（**其实从没被读**，只当元数据） | 同上 `cards_raw.json`；**现在真读了**，用来判 `inTruthSnapshot` 与 `uncovered.why` | 全量件 `cards` **364 → 364** · `cardsWithTruth` **93 → 93** · `activeAnims` **160 → 160** · 运行时件 **93 → 93** · `uncovered` **13 → 13**（**数字不变**，见 §2） |

### 1·1 每个生成器的完整命令（全部用 `py312 -I -X utf8`）

```bash
# ① 相关卡（必须先跑 —— 天赋生成器读它的产物）
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_related_cards.py
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_related_cards.py --check   # 幂等自检

# ② 天赋卡（依赖 ① 的 数据/游戏数据/related_cards.json）
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_talent_cards.py

# ③ 逐卡能力动画时长（不依赖 ①②）
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_ability_anim_durations.py
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_ability_anim_durations.py --check
```

`--check` 结果：两份 `related_cards.json` **一致 ✅**；两份 `ability_anim_durations.json` **一致 ✅（全过）**。
天赋生成器无 `--check`，用「重跑后 md5 不变」验的（两份产物 md5 均 OK）。

### 1·2 代码改动（逐个）

- **`gen_talent_cards.py`**
  - `TRUTH` 换指 `cards_raw.json`，注释写清「真值源 = 当前 build allcards，2026-10-18 换 / 旧路径是 2025-07-30 快照 / ⛔ 别改回去」。
  - 新增 `rc_path(c, i)`：当前语料把 `relatedCard1..4` 放在 **`full_raw`** 下（旧快照在顶层）⇒ 两种形状都认。
  - `truth = load(TRUTH)` 后兼容 `{bundle,count,cards:[…]}` 与裸数组两种形状，形状不认识就 `SystemExit`。
  - 产物里的 `source` 文案（全量件 + 运行时件）一并更新。
- **`gen_related_cards.py`**
  - `CACHE` → `CARDS_RAW`；**删掉 UnityPy 依赖**。
  - 新增 `_Ref` / `_CardSo` 适配层：把 `cards_raw.json` 的一条卡 SO 包成下游 `getattr` 期望的形状（`.uniqueId/.cardName/.cardArmy/.m_Name/.relatedCard1..4.m_PathID`），`load_truth()` 返回 `{path_id: _CardSo}`。
  - 产物 `source` / `provenance` 文案更新；文件头「真值从哪来」「怎么落到我们池子」两节按新源改写。
- **`gen_ability_anim_durations.py`**
  - 新增常量 `CARD_SO_TRUTH`（指向 `cards_raw.json`，注释写清换源与「别改回去」）。
  - `_sources.cardSoTruth` 换成新路径。
  - `uncovered` 的 `inTruthSnapshot` 不再拿 vfx 表反推，**直接读 SO**；新增 `soActiveAnims` 字段，`why` 分三档（不在 SO / 在 SO 但 0 条 CardAnim / SO 有 N 条但 vfx 没覆盖 = 映射缺口）。
  - 文件头与 `_headline` 里「2022 快照 / UM·SW 后来的扩军」那套说法已删（现已不成立）。

## 2. 语义变化清单（同一条前后不同）

**结论：三个生成器的映射都没有「同一条前后不同」的情况 —— 全是净增。**

| 生成器 | 逐条对账结果 |
|---|---|
| 相关卡（运行时件 176 条共有 id） | **changed = 0** · removed = 0 · added = **15**：`DA75` `SW1` `SW13` `SW18` `SW2` `SW23` `SW3` `SW32` `SW42` `SW48` `SW58` `UM100` `UM87` `UM88` `a51adc8f5c311584385c3117cfd919e9` |
| 天赋卡（运行时件 89 条共有 id） | **changed = 0** · removed = 0 · added = **7**：`DA75` `SW1` `SW2` `SW3` `UM87` `UM88` `UM100`（= R 文档预告的那 7 张） |
| 逐卡能力时长 | 全量件 `cards` **逐卡逐字段完全一致**（`stats` 全等、`cards` 0 条变化、`unresolved` 0）；运行时件字节不变。**只有 `uncovered[*].why` 文案 + 新增 `soActiveAnims` + `_sources` 变了** |

抽验（判据给 SO 字段值）：

- `DA75 Master Zacharial`：SO `unitsToSummon` = `38871853…`(`Refuse to Yield`) ⇒ 天赋表/相关卡表都出这一张 ✅
- `SW1 Logan Grimnar`：天赋 = `b3dce3f1…`(`High King of Fenris`)；相关卡 SO 两槽 = `High King of Fenris` + `Tyrnak and Fenrir`，产物逐槽一致 ✅
- `SW2 Njal Stormcaller`：四槽 `High Rune Priest` / `Tempest's Wrath` / `Murderous Hurricane` / `Living Lightning` 逐槽一致 ✅
- `UM87 Roboute Guilliman`、`UM88 Valius Paxor`、`UM100 Chaplain Cassius`：逐槽一致 ✅

### 2·1 新增的「丢掉」都是**池外卡**，不是回归

- 相关卡丢弃 27 条边（旧 26 条同量级）：
  - **源认不出 14 张**（20 条边）全是**另形态**：`_EASY` / `_tutorial` / `_enchantment`（如 `Goff_war_GrukkFace-Rippa_EASY`、`16 SaimHann-Witchblade Warlock_tutorial`）。它们的**底盘卡已在池内且边一模一样** ⇒ 不丢内容。
  - **目标认不出 7 条边**全部指向同一个 `ca169f2a…` `Mark of Slaanesh (enchantment)`（`Z-NC EmperorsChildren-…_enchantment`）—— 该卡在 SO 1,351 里，**不在我们池 1,127 里**（附魔形态，非可收集）⇒ 按红线「宁可认不出」丢掉。
- 天赋表 2 张落不下 = `Witchblade Warlock (tutorial)` / `Sylar Hexscorn (tutorial)` 两张**教程形态卡的源卡**认不出；
  它们**候选表本身是对的**，而且**底盘卡 `ASH19 Witchblade Warlock` / `BL36 Sylar Hexscorn` 都在表里、候选完全相同** ⇒ 无内容损失。

## 3. 没查清 / 需要别人接的

1. **`.cs` 里的计数注释已经与产物不一致**（⛔ 本任务黑名单不许改 `.cs`，只列出来给 C# owner）：
   - `CardPresentation/Core/RelatedCards.cs:41` —— 写「**240 条边 · 196 张卡**」⇒ 现运行时件 **239 边 / 191 张卡**（全量真值 266 边 / 212 张卡）。
   - `RuleEngine/Core/RuleCore.cs:2773` —— 同一句「240 条边 / 196 张卡」。
   - `RuleEngine/Core/CardDef.cs:1240` —— 写「**85 张落表**」「真值 97 张里 12 张落不下」⇒ 现 **96 张落表**、真值 105 张、落不下 2 张（且那 2 张是教程形态，不再是「等远端 allcards」的病根）。
   - ⚠️ **这两条是本次换源之前就已经过期的**（不是本次造成，但同一片区域，顺手报）：`Core/EventTiming.cs:20`（「现读 62 张卡」）、`:268`/`:315`（「56 张卡」）、`Core/AbilityAnimDurations.cs:32`（「62 张卡 / 99 条 CardAnim」）—— 产物现读 **93 张 / 160 条**。
2. **`Mark of Slaanesh (enchantment)`（`ca169f2a…`）要不要进池** —— 属「包内多出卡」那一族（R 文档 §6.6 未裁），本脚本按红线没猜。
3. 三个生成器都**不产 `cards_engine.json` / `card_stats.json` / 描述文本**（按任务要求没碰）；`gen_talent_cards` 仍依赖 `related_cards.json` 作为 PathID→池 id 的翻译表，**两份现在同源**，不会再分叉。
4. 相关卡文件头里那句「候选只限我们自造 id 的卡」这一级**当前实际命中 0**（池 1127/1127 的 id 都已是真 `uniqueId`）—— 已在注释里写明「保留作防御」，不是缺陷。

## 4. `git diff --numstat`（只列本任务这 9 个文件）

```
36      9       Unity/工具/gen_talent_cards.py
67      24      Unity/工具/gen_related_cards.py
42      19      Unity/工具/gen_ability_anim_durations.py
96      8       Unity/数据/游戏数据/talent_cards.json
44      2       Unity/MyGame/Assets/RuleEngine/Resources/talent_cards.json
295     10      Unity/数据/游戏数据/related_cards.json
104     4       Unity/MyGame/Assets/CardPresentation/Resources/related_cards.json
28      15      Unity/数据/游戏数据/ability_anim_durations.json
```

- `Unity/MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json` **不在 diff 里 = 字节不变**（全量件的 `_headline` 改了，运行时件的 `_headline` 本来就不含那句）。
- 三个 `.py` 的 diff 量（36/9 · 67/24 · 42/19）与改动处数量吻合 ⇒ **没有整篇翻行尾**（其余未列出的文件是别的并行代理在改，与本任务无关）。
- 产物行尾：仍由生成器以 `newline='\n'` 写出（与改前一致）。

## 5. 自检 / 复现

- `gen_related_cards.py --check` → 两份产物一致 ✅
- `gen_ability_anim_durations.py --check` → 两份产物一致 ✅（全过）
- `gen_talent_cards.py` 重跑 → 两份产物 md5 不变 ✅
- 三个 `.py` 均 `py_compile` 通过；未跑 Unity（红线）。
