# `AG` · 四件小订正（重钉基线 + 补 4 条断言 + 两处过期注释）

> 写手：`AG` 波（白名单 = `RuleEngine/Editor/RuleEngineTest*.cs` · `CardPresentation/Core/CardIcons.cs` ·
> `RuleEngine/Core/CardDef.cs` / `RuleEngine/Core/RuleCore.cs`（**只订正注释**））。
> 来源：主对话简报（① 重钉 = 主对话已裁「重钉、不回退」）· ② = `AD_descZh两代.md` §四 · ③ = `AF_断言与订正.md` §五·1 ·
> ④ = `AD_descZh两代.md` §五·5。
> ⛔ 本笔**没跑 Unity 批处理、没跑 `ruleprobe`、没重钉指纹、没动 git、没改正本**。
> ✅ 跑过：`TMPDIR=/tmp/wf_ag bash d:/4/Unity/工具/typecheck.sh`（3 次，末次在全部改动之后）⇒ **运行时 0 错 / 编辑器 0 错**。

---

## 〇、一句话（四件各一行）

| # | 件 | 结论 |
|---|---|---|
| **①** | 重钉 `Has(penitence)` 基线 `1 → 5` | ✅ **重钉成 5**（主对话已裁），理由 + 那 4 个 id 写进注释；**另加一条「逐 id 名单锁」**（比只数个数强）；同批订正了 `:23156` 那段旧读数注释 |
| **②** | 补 `AD` §四 的 4 条断言 | ✅ **4 条全补**（`DA83` 列锁 · 全池 `armour` 列 `== 81` · `cardTraits` 无 `Frenzied`/`Rage` · 4 张 `Penitence` 的 `BodyResolves`），**每条都带判别式**；其中两条要读**卡表原文**，另起了读 `Resources.Load<TextAsset>` + `CardDatabase.FileDto` 的一段（照抄本文件既有先例，没另写 JSON 形状） |
| **③** | `CardIcons.cs:561-566` 过期注释 | ✅ 就地订正（**原句照留** + 「原写 X ⇒ 实际 Y」三小条）；现核把「六族 1327 条」逐个改成**九族 2758 条**的逐族数 |
| **④** | `CardDef.cs:4252` / `RuleCore.cs:3601` | ✅ 两处都订正（**列名 `Frenzied` ⇒ 现读 `Destroyer`**，结论不变）；⚠️ **另发现同文件第三处**（`CardDef.cs:265` 的 `>0 共 80 张` 与 `:275-280` 的「`DA83` 是列的缺陷、**已另立账**」）**也已一并订正** —— 见 §四·3，**这一处不在简报点名的两处之内**，请复核 |

**行尾**（改前 / 改后**逐文件实测**，⛔ 未用 `sed -i`，全部按**实测**行尾改）：

| 文件 | 改前 | 改后 |
|---|---|---|
| `RuleEngine/Editor/RuleEngineTest.cs` | **CRLF 29281 = LF 29281**（纯 CRLF） | **CRLF 29426 = LF 29426**（纯 CRLF）✅ |
| `CardPresentation/Core/CardIcons.cs` | **CRLF 0 / LF 733**（纯 LF） | **CRLF 0 / LF 758**（纯 LF）✅ |
| `RuleEngine/Core/CardDef.cs` | **CRLF 0 / LF 4495**（纯 LF） | **CRLF 0 / LF 4516**（纯 LF）✅ |
| `RuleEngine/Core/RuleCore.cs` | **CRLF 0 / LF 8803**（纯 LF） | **CRLF 0 / LF 8810**（纯 LF）✅ |

⇒ **本笔净增 145 + 25 + 21 + 7 = 198 行**（**全是新增注释/断言，删 0 行**）。
🔴 「简报里写 `RuleEngineTest.cs` 是 CRLF、`CardIcons.cs` 是 CRLF」—— **后半句不成立**（现核实测纯 LF），本笔按实测改的。

---

## 一、① 重钉基线：`Has(penitence)` `1 → 5`（主对话已裁：重钉，⛔ 不回退）

### 1·1 落点与改动

| 落点 | 原文 | 改成 |
|---|---|---|
| `RuleEngineTest.cs:2191-2205`（注释块） | 「`Has(penitence)` **1** · …」后面直接就是判别式那三行 | 原句**照留**，**后接**一段「🔴 2026-10-15 就地重钉（`AG` 波 · 铁律 5）：第一格 `1` → `5`」+ **错因** + **4 个 id 与逐张实据** |
| `RuleEngineTest.cs:2216-2218` | `Check(pen, 1, "★ 全池 `Has(penitence)` = 1 张（登记 `Rage` 前后必须相同 —— 多了就是前缀撞车）");` | `Check(pen, 5, "★ 全池 `Has(penitence)` = 5 张（`SOR8`/`SOR10`/`SOR21`/`SOR27`/`SOR32` —— 其中 4 张是 2026-10-15「生成器清空排除表」放行的，**逐张核过不是前缀撞车**；⛔ 改回 1 = 推翻那一笔）");` |
| `RuleEngineTest.cs:2221-2224` | （无） | 🆕 **名单锁** `CheckTrue(penIds.Count == 5 && Contains(SOR8/10/21/27/32), …)`（`penIds` 是循环里新收的 `List<string>`） |
| `RuleEngineTest.cs:2228-2250` | （无） | 🆕 `AD` §四·4：`SOR8/SOR10/SOR21/SOR27` 四张的 `Has(penitence) && BodyResolves(penitence)` |
| `RuleEngineTest.cs:23156-23168` | 那段 doc 的读数注释（`rally 86 / strike 23 / slay 9 / backlash 3 / penitence 1`） | 原句**照留**，**后接**一条订正：那几个数是**改前**读数、⛔ 不是现池账；**本次不给新值**（没跑 Unity，⛔ 不拿推算冒充实测） |

### 1·2 🔴 为什么重钉是对的（**本笔独立现核，⛔ 没只信转述**）

判据 = **真引擎自己 dump 的逐卡归一化键**（`d:/4/_tmp_view/layers_ad.jsonl`，`AD` 那笔 `ruleprobe.sh layers` 的产物，
**1127 行**）。本笔**自己把这个文件读了一遍**（⛔ 不是抄 `AD` 的结论句），数 `kwKeys` 里带该键的卡：

```
penitence   = 5  → SOR8 / SOR21 / SOR10 / SOR32 / SOR27
destroyer   = 10 → SAU9 / SAU70 / SAU3 / SAU25 / SAU68 / SAU22 / SAU72 / SAU18 / SAU32 / SAU66
unstoppable = 0  → （空）
```

⇒ **只有第一格变**（`1 → 5`），另两格**一字不动**，与 `AD` 的读数吻合。

**那 4 张是「生成器放行」带来的、不是前缀撞车** —— 逐张现核（`cards_engine.json`）：

| id | 卡名 | `keywords` | `desc`（卡面正文） |
|---|---|---|---|
| `SOR8` | Arco-Flagellant | `['Flank','Penitence']` | `Penitence: Deal 1 damage to all other units` |
| `SOR10` | Sisters Repentia | `['Penitence']` | `When this unit kills an enemy, gain 1 ☀. Penitence: Gain +2 [Attack]` |
| `SOR21` | Death Cult Assassin | `['Camouflage','Penitence']` | `Penitence: Deal 2 damage to a random enemy` |
| `SOR27` | Penitent Engine | `['Penitence']` | `Penitence: Gain Flank and Blast 2` |
| `SOR32` | Mortifier（**改前就有**） | `['Penitence']` | `Penitence: Deal 2 damage to the enemy Warlord` |

⇒ 五张**卡面/`desc` 里确实印着 `Penitence:`** ⇒ 是「该有的关键词终于进池了」，**符合「只加词不改池 ⇒ 三个数都不动」那条判别式的例外**（这里**不是**只加词：生成器把词**真加进池了**，是那条指令要的效果）。

### 1·3 每格期望值与判别式（逐格抄一遍）

| 格 | 期望值 | 判别式（**结构上不可能同时满足**） |
|---|---|---|
| `Check(pen, 5, …)` | **5** | 生成器把 `TRAIT_ENGINE_UNREGISTERED` 改回那三个词 ⇒ 那 4 张的 `Penitence` 掉出 `keywords` ⇒ 回到 1 ⇒ 红；**而「改回 1」这一种改法会让它假绿** —— 所以**不靠这一格单独挡**，靠下面的名单锁 |
| 名单锁 `CheckTrue(penIds.Count == 5 && 逐 id Contains)` | **恰好这 5 个 id** | 「数出来还是 5 张、但换成别的卡」⇒ 个数字那格**看不出来**、本格**红**（这就是加它的理由） |
| `Check(des, 10, …)` | **10**（不变） | `frenzied` 那 10 张的 `keywords` **本来就有 `Destroyer`** ⇒ 改成卡面字之后归一化同名、被 `have` 挡下 ⇒ **0 张真追加**；真出现追加 ⇒ 这个数变 20 ⇒ 红 |
| `Check(uns, 0, …)` | **0** | `SAU7` 的 `Unstoppable` 被更高层（`cardface_fixes` 的 `keywords`）换掉 ⇒ 池里 0 张；若哪天不被换掉 ⇒ 变 1 ⇒ 红 |
| 4 张 `BodyResolves` `CheckTrue(Has && BodyResolves)` | **四条全真** | 测的是**判据链的末端**：`TriggerOps("penitence") != null` 要同时满足「`penitence` 在 `RoutableTriggers`」＋「正文 `EffectText.Parse` 至少解出一条 op」。链上任一环断（例：把 `Penitence` 从 `Core/CardDef.cs:476` 的 `RoutableTriggers` 拿掉）⇒ **本格红**，而**名单锁照旧绿**（它看的是 `keywords` 那一列，不相干）；反过来把生成器排除表改回去 ⇒ **名单锁红、本格照旧绿** ⇒ 两格**各自能单独红** |

### 1·4 `TestA1618TwoParsersAgree` 现核结果（**本笔独立核的**）

**结论：✅ 不会因这 4 张变红。**

- 判据链（现读代码，`RuleEngineTest.cs:23227`）：`star` = 「`c.Has(kw)` 为真 **且** `!c.BodyResolves(kw)`」的处数。
- 原版卡那一档 `BodyResolves(kw)` = `TriggerOps(kw) != null`（`Core/CardDef.cs:2887-2895`）。
- **实测读数**（同一份 `layers_ad.jsonl`，字段 `ch.trig` = `c.TriggerTexts.Count`，`Layers.cs:199`）：

```
SOR8  ch.trig=1   SOR10 ch.trig=1   SOR21 ch.trig=1   SOR27 ch.trig=1   SOR32 ch.trig=1
```

  而这 5 张的 `desc` 里**可路由触发前缀只有 `Penitence:` 一个**（`SOR10` 另有一句 `When …`，走的是**另一条通道** `when`）⇒ 那 1 条 `_triggerText` 的键**只能是 `penitence`**
  ⇒ `TriggerOps("penitence") != null` ⇒ **`BodyResolves(penitence)` 五张全真**
  ⇒ `star` **不变**（仍为 0）。
- ⚠️ **本笔的边界（如实写）**：这条是**读 `AD` 那笔真引擎 dump 的原始字段 + 逐张读实现的判据链**得出的，
  ⛔ **没有自己再跑一次引擎**（简报明令不许跑 `ruleprobe`）⇒ 它比「只信转述」强（**原始数据是本笔自己读的**），
  但**不等于本笔跑出来过**。`A1618` 那条断言的**真宿主**仍是 `RuleEngineTest.Run`。

---

## 二、② 补 4 条断言（源 = `AD_descZh两代.md` §四，**判别式照抄并落地**）

### 2·1 `DA83` 的**列锁**（§四·1；简报点名的「首推那条」）

- **落点**：`RuleEngineTest.cs:2155-2168`（**紧跟在 `AB` 那条「有效护甲 = 1」后面**，同一个护甲块内）。
- **改了哪一行**：`AB` 那条的消息**尾部追加**一句 `⚠️ **它测的是【有效值】** —— 列那一半的回退它测不出，见下面那两格`；
  新增 `Check(da83.ArmourFromPool, 1, "★ `DA83` 的 `armour` **列** = 1（…）")`。
- **期望值 / 判别式**：**列 = 1**。
  判别式：把 `gen_cards_engine.py` 的 `fix_own_armour` 闸写回**宽**那一版（`any(re.search(r"armou?r", k) for k in keywords)`，裸 `Armour` 也算「已经有值」）
  ⇒ 列回落 **0** ⇒ **本格红**；而 `AB` 那条「有效护甲 = 1」**照旧绿**（旧口径兜底接住）—— **这就是要补它的理由**。
- **判据**：PnP 成品卡图 `d:/2/Warpforge部队卡片/Dark Angels/3部队/Warpforge_09_Ballistus-Dreadnought.png` 右下银盾印 1、关键词行印 `Armour 1.`。

### 2·2 全池 `armour` 列 > 0 的张数（§四·2）

- **落点**：`RuleEngineTest.cs:2170-2181`。
- **期望值 / 判别式**：**`== 81`**。
  ⚠️ 本笔**把 `AD` 写的「= 81（+ 下界 ≥ 80）」收成一个 `==`** —— 因为 `>= 80` 对 `AD` 自己点名的那条回退（`fix_own_armour` 写回宽版 ⇒ `DA83` 掉档 ⇒ **80**）**不敏感**，`>= 80` 会假绿。
  ⇒ 只留 `== 81`（判别式：① 列的读点被改窄 ⇒ 掉数；② `fix_own_armour` 被回退 ⇒ 81 → 80）。
- **现核**：`cards_engine.json` 里 `armour > 0` = **81**（与 `AD` §七 那句摘要「`armour` 列 >0 共 **81** 张」一致）。

### 2·3 `cardTraits` 里不许出现 `Frenzied` / `Rage`（§四·3）

- **落点**：`RuleEngineTest.cs:2253-2307`（新起一段，紧跟判 `penitence` 那块之后）。
- **实现说明**：`CardDef` **没有** `cardTraits` 这一栏、也**不保留** `keywords` 原文
  ⇒ 本段改用 **`Resources.Load<TextAsset>(CardDatabase.ResourcePath)` + `JsonUtility.FromJson<CardDatabase.FileDto>`**
  （**照抄本文件既有的 `LoadRawKeywordEntries`（`:9613-9647`）那一套**，⛔ 没另写 JSON 形状）。
  先断一句**前提**「卡表原文读得回来」—— 读不到时**红**、⛔ 不是空转放过去。
- **期望值 / 判别式**：`badTrait.Count == 0`。
  判别式：把 `gen_cards_engine.py` 的 `TRAIT_DISPLAY` 改回 Title Case（`frenzied` → `Frenzied`、`rage` → `Rage`）⇒ 本格**立刻红**。
  判据：官方表 `Card_Trait/frenzied` = `Destroyer` · `Card_Trait/rage` = `Penitence`（另有化石键 `Card_Trait/frenzied_old` = `Frenzied`）。

### 2·4 `DA83` 的 `keywords` 原文**恰好一条 `Armour 1`**（§四·1 的**原文那一半**）

- **落点**：同上段（`RuleEngineTest.cs:2288-2305`）。
- **期望值 / 判别式**：`da83Armour1 == 1`。
  判别式 ①：`fix_own_armour` 闸写回宽版 ⇒ 裸词**不再升级** ⇒ 停在裸 `Armour` ⇒ 红（而「有效护甲 = 1」那条**照旧绿** —— 裸词按「存在即真」也是 1）。
  判别式 ②：**「恰好 1 条」也是判据** —— 改成「**追加**第二条」会让卡面印成 `护甲。护甲 1。`（`CardText.KeywordSegment` 逐条比字面）⇒ 同样红。
- **现核**：`DA83.keywords = ['Armour 1']`、`armour = 1`。

### 2·5 `AD` §四·4（那 4 张 `BodyResolves` = 真）

📌 **本笔把它放在了 §二 之外、并进了重钉那一块**（`RuleEngineTest.cs:2228-2250`）——
`AD` 的原话是「**建议**把这条**并进 `A1618` 那条的读数**里」，本笔判定放在**数 `Has(penitence)` 的那一块**更好：
下个会话看到的顺序就是「**数变成 5 了** → **紧接着证明这 5 张的正文真会跑**」，而 `A1618` 那条 `star == 0` 本来就已经覆盖它。
⛔ 若主对话认为必须并进 `A1618`，说一声即可搬（本笔没动 `TestA1618TwoParsersAgree` 一个字符）。

---

## 三、③ `CardIcons.cs:561-566` 的过期注释（**先现核了原文，⛔ 没照抄转述**）

- **落点**：`CardPresentation/Core/CardIcons.cs:561-566`（原文段）→ 本笔在其**后**新加一段 `:567-591`。
- **原文现读**（逐字）：
  > `🔴 **缺口（现读）**：Modular_Description/* 官方有 **340** 条（Card_Description 里用到 **86** 种键 / **508** 处），而它在 Resources/ 下**一条都没有**（Resources/official_names.json 只有 Avatars/Titles/Achievements/Cardbacks/Event_Description/PrebuiltDecks 六族 = 1327 条；Loc 手写表里也没有）⇒ **本支今天会全部「原样留着 + 出声」**。`
- **订正写法（铁律 5：原句照留 + 「原写 X ⇒ 实际 Y」）**，三小条：
  1. **原写「六族 = 1327 条」⇒ 实际（2026-10-15 现读）= 2758 条 / 【九族】**：
     `Card_Name` **1065** · `Avatars` **419** · `Titles` **368** · **`Modular_Description` 340** ·
     `Achievements` **198** · `Cardbacks` **157** · `Event_Description` **111** · `PrebuiltDecks` **74** · `Card_Trait` **26**
     （**本笔把这份 json 逐族数了一遍**，`count` 字段自报也是 2758）⇒ **「`Resources/` 下一条都没有」不成立**。
  2. **原写「本支今天会全部『原样留着 + 出声』」⇒ 不成立**：卡池 `descZh` 里 `((Key…))` 现读 = **73 个键 / 247 处**（**本笔逐列重数过**：`desc` 列 **0**、`descZh` 列 **247**），
     这 **73 个键全部命中** `official_names.json` ⇒ 本支走**正常路**；「原样留着 + 出声」只剩「**这一条键取不到值**」那一档（键两张表都没有 / 值里的记号渲不掉 ⇒ `OfficialTerm` 为 null），而现读**那 73 个键一条都不落**在这一档里。
  3. **与它配对的那道闸也已拆**（`AC` 波）：`Loc.BulkOr` 现在判「**渲染完还有没有记号**」而不是「值里有没有记号」
     （见 `<see cref="OfficialTerm"/>` 上面那段 `AF` 的订正；实现 = `Core/Loc.cs:2697-2765`）——**这两条合起来**才是「`((Key))` 今天真的渲得出中文」。
- ⚠️ **本笔没动** `Resources/official_names.json`（那是生成物、且**不在白名单**）—— 它 `note` 字段里那句「带记号的 `Modular_Description` 值今天仍取不到（18 处 / 11 键）」**也是过期文案**（同一道闸），**只报不改**（见 §五·2）。

---

## 四、④ `CardDef.cs` / `RuleCore.cs` 因 `AD` 改动作废的注释

### 4·1 `CardDef.cs:4265-4274`（`AD` 点名的那处）

- **原文**：`//      · 全池 10 张 \`cardTraits=Frenzied\` 的卡，\`keywords\` **全部**带 \`Destroyer\`。`
- **现核**：`cards_engine.json` 里 `cardTraits` 含 `Destroyer` 的**正好 10 张** = `SAU3/9/18/22/25/32/66/68/70/72`（与 `AD` 那份名单**逐 id 相同**），**`Frenzied` / `Rage` 字面量在 `cardTraits` 列里 0 张**。
- **订正**：原句照留 + 注明「列名过期」（原写 `Frenzied` ⇒ 实际该列写 `Destroyer`；**错因** = 生成器同一天把 `TRAIT_DISPLAY` 的 `frenzied` 改成卡面字）**＋「结论（10 张、`keywords` 全带 `Destroyer`）一字不变」＋ 10 个 id**。

### 4·2 `RuleCore.cs:3602-3610`（`AD` 点名的那处）

- **原文**：`//        （卡的 \`cardTraits\` 列里写的 \`Frenzied\` 指的就是它）。`
- **订正**：原句照留 + 标注「括号里的列名过期」（原写 `Frenzied` ⇒ 现读 `Destroyer`，10 个 id 同 4·1）＋「**结论不变**：`0x2a8`(680) 就是 `DefinedTrait.frenzied`，也就是卡面那个 `Destroyer` 的 trait」。

### 4·3 ⚠️ 本笔**另发现同文件第三处**（`AD` 没报、**不在简报点名的两处之内**）

- **落点**：`CardDef.cs:265`（`>0 共 80 张`）与 `:275-280`（`DA83` 那一段）。
- **为什么它也算「因本笔（`AD` 那笔）改动作废」**：那段写着「**实测正好差了 1 张**（`DA83`）… **是列的缺陷**（生成器那一侧漏了这张，**已另立账**）…
  ⛔ 所以**不能**把这一支删掉 —— 删了这张卡会**静默少 1 点护甲**」。
  `AD` 把 `fix_own_armour` 修好之后：**那笔账当天就结了**（`DA83` 的列 = 1、`keywords` = `['Armour 1']`）
  ⇒ 「列 = 0 而 `keywords` 抠出 > 0」这一档**今天已是空集**（自检那一格现读 `onlyKw.Count == 0`）。
  ⇒ 留着的后果 = **下个会话会去找一笔已经结掉的账**（铁律 5/6 治的正是这种「两份说法打架」）。
- **本笔的动作**（**只订正注释，实现一个字没动**）：
  1. `:265` 的 `（\`A1776\`，>0 共 80 张）` → 原值照留 + `⚠️ 2026-10-15 现读 **81**，见下面那条订正`；
  2. `:280` 之后新增一段「🔴 2026-10-15（`AG` 波）就地订正（铁律 5）—— 上面这段的【依据】已经变了」：
     原句照留 + 「原写『已另立账』⇒ 那笔账**当天就结了**」+ **结论照旧、理由要换**
     （兜底那一支**仍然不能删**，但今天理由是「列的覆盖面是生成器算出来的、会不会再漏一张不由这里保证」，⛔ 不再是「`DA83` 今天正靠它」）。
- 🔴 **请主对话复核这一处**：它**超出了简报点名的两处**；若认为该走「只报不改」，`git checkout` 回退这 21 行即可，
  不影响 §一/§二/§三（那三件在**别的文件 / 别的段落**）。

---

## 五、没查清的部分（⛔ 一处都没有猜）

1. 🔴 **本笔一条 Unity 断言都没跑**（用户现行口径 + 简报明令「不许跑 Unity 批处理 / 不许跑 `ruleprobe`」）
   ⇒ §一 的 `pen`、§二 的 4 条，**期望值只到「读真引擎 dump 的原始字段 + 读实现 + 离线数数据」这一层**，
   ⛔ **不等于它们在 Unity 里已经绿过**。自检可以在下次收口时一并覆盖（本笔动的全是 `RuleEngine/` 与 `CardPresentation/Core/`，
   按覆盖面属于 `RuleEngineTest.Run` 那一条）。
2. ⚠️ **§一·4 的证据链**（`ch.trig` → 键 = `penitence`）用的是**推断的那一跳**：dump 只给条数、不给键名。
   本笔的补强是「那 5 张的 `desc` 里**可路由触发前缀只有 `Penitence:` 一个**」（逐张读过），但**不是直接看见键名**。
   ⇒ 若要 100% 直读，得给 `Layers.cs` 加一列输出键名（**本笔不动探针**）。
3. ⚠️ §二·3/2·4 那两格**依赖 `Resources.Load<TextAsset>` 在批处理下能读到卡表**
   —— 判据是「读不到时**会红**」（本笔先断了一句前提），⛔ 不是「读不到还绿」；
   ⚠️ 但**本笔没有实测过它在批处理下的行为**（同行先例 `LoadRawKeywordEntries:9624` 在跑，间接支持）。

---

## 六、顺手发现（⛔ **一条都没改**，只报）

1. ⚠️ **`RuleEngineTest.cs:2137` 那句读数注释「2026-10-15 现读 79」可能已经不准**。
   本笔用**离线复算**（`cards_engine.json` 按 `CardDef` 那段口径照算：`fp = armour>0?armour:0`、`fk = keywords 里 `Armour N` 的值`）得到：
   `agree = **81**` · `mismatch = 0` · `onlyPool = 0` · `onlyKw = 0`（DA83 修好之后 `onlyKw` 归零，与 `AD` 报的一致）。
   ⇒ 那句话里的 **79** 与本次复算的 **81** 差 2。
   ⚠️ **本笔不改**（简报：顺手发现照报）：① 「离线复算」不是「真引擎读数」；② 那条断言本身是 `CheckTrue(agree >= 70)`，**79 或 81 都是绿的**，只影响注释里的一个数。
   要真值就下次在 Unity 里跑一趟 `TestCardDatabase` 看日志。
2. ⚠️ **`Resources/official_names.json` 的 `note` 字段里有一句过期文案**（**生成物、不在白名单**）：
   > `⚠️ `Loc.BulkOr` 另有一道「值里含 `[[`/`((` 就当取不到」的闸（`Core/Loc.cs`）⇒ 带记号的 `Modular_Description` 值今天仍取不到（18 处 / 11 键，见 P3 报告）。`
   —— 那道闸 **2026-10-15（`AC` 波）已经拆了**（判据 = `Core/Loc.cs:2697-2765`）⇒ 这半句与 §三 订正的是**同一件事**。
   产地 = `工具/_loc_official/gen_official_names.py`（改它的文案要在生成器那一侧改，否则重跑就回来了）。
3. ℹ️ **`Core/Loc.cs:2630` 附近那张「逐族条数」的注释**也带一份数字（`Cardbacks/*` 157 · `PrebuiltDecks/*` 97 · `Event_Description/*` 111）
   —— 现读 `PrebuiltDecks` 是 **74**（不是 97），另两族（157 / 111）**对得上**。
   ⚠️ 本笔**没改**（`Loc.cs` **在别处白名单、本笔⛔ 不能碰**），也**没查清** 97 是从哪一版来的 ⇒ **只报**。
4. ℹ️ `AD` §三 那 **48 条 `descZh` 候选**（「中文比英文旧一代」）**仍全部待逐张开 PnP 卡图裁定** —— 与本笔无关，只是提醒它还没被派活。

---

## 七、验收读数（照抄，可复核）

```text
$ TMPDIR=/tmp/wf_ag bash d:/4/Unity/工具/typecheck.sh        （末次 = 全部改动之后）
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0

$ git diff --numstat -- <本笔四个文件>（⚠️ 数字里混着别人未提交的存量）
391  1    Unity/MyGame/Assets/CardPresentation/Core/CardIcons.cs
167  6    Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs
450  74   Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs
603  54   Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs
⇒ 本笔自己的净增量（按改前改后行数逐文件量）：+145 / +25 / +21 / +7 = +198 行，删 0 行

行尾（改后实测）
RuleEngineTest.cs    CRLF 29426 = LF 29426   （纯 CRLF，未翻）
CardIcons.cs         CRLF 0     / LF 758     （纯 LF，未翻）
CardDef.cs           CRLF 0     / LF 4516    （纯 LF，未翻）
RuleCore.cs          CRLF 0     / LF 8810    （纯 LF，未翻）

现核数据（本笔自己数的，⛔ 不是抄的）
official_names.json  = 2758 条 / 九族（Card_Name 1065 · Avatars 419 · Titles 368 · Modular_Description 340 ·
                       Achievements 198 · Cardbacks 157 · Event_Description 111 · PrebuiltDecks 74 · Card_Trait 26）
cards_engine.json    = 1127 张 · v9 · armour 列 >0 = 81 · cardTraits 含 Destroyer 的 = 10（逐 id 见 §四·1）
descZh 里 ((Key))    = 73 键 / 247 处（desc 列 0 处）· 73 键全部命中 official_names.json
layers_ad.jsonl      kwKeys：penitence 5 · destroyer 10 · unstoppable 0 · ch.trig（SOR8/10/21/27/32）= 1
```

**改了哪些文件哪些行（本笔）**：
`RuleEngine/Editor/RuleEngineTest.cs`（`:2152-2250`【重钉 + 名单锁 + `DA83` 列锁 + `armour` 列 81 + 4 张 `BodyResolves`】·
`:2253-2307`【新段：读卡表原文，`cardTraits` 与 `DA83` 的 `Armour 1`】· `:23156-23168`【旧读数注释订正】）·
`CardPresentation/Core/CardIcons.cs`（`:567-591` 新增订正段）·
`RuleEngine/Core/CardDef.cs`（`:265-266`、`:280-291`、`:4265-4274`，**全是注释**）·
`RuleEngine/Core/RuleCore.cs`（`:3602-3610`，**全是注释**）。
⛔ 未动 git · ⛔ 未改正本 · ⛔ 未跑 Unity / `ruleprobe` · ⛔ 未碰黑名单里的四个文件。
