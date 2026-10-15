# `F1` — `RuleEngineTest.Run` 那 24 条红：逐条改完（第三十二会话 · 动手）

> 判据 = `资料/普查产出_第三十二会话/D_24条红诊断.md`（208 行，**已整篇读完**）。
> 诊断的分类 **α 23 · β 0 · γ 0 · δ 1**。**实现缺陷一条都没改**（诊断说没有）—— 改的全是
> **期望值 / 卡名字面 / 夹具 / 注释里的过期数**。
> 独占文件：`Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`（只改它）。
> 改前副本：`d:/4/_tmp_view/RuleEngineTest.cs.bak_a1`。
> ⚠️ **卡池现读 md5 `6e8c5786…` ≠ 诊断那趟的 `3f3c0e4f…`** —— 逐条**都按现池重核过**
> （差集 = 9 个 `descZh` 字段，与英文侧/卡名侧无关；`desc`/卡名/张数**一个都没动**）。

---

## 一、逐条（24 条）

### 1. `RuleEngineTest.cs:2896` —— 战术卡张数（`cov.Cards`）
类别：(α)
改了什么：`Check(cov.Cards, 445, …)` → **`446`**，并在上面补一行注释（成因 = `Infestation Node`
（`TL40`·`tactic`·2 费）按 `工具/gen_cards_engine.py:488` 的 `STATS_EXCEPTIONS` 新收进池）。
现池实测 `type=='tactic'` = **446** ✓。
🧨 那句跟着改了吗：**否**（原文没有 🧨 行；判别力 = 这个数本身）。

### 2. `RuleEngineTest.cs:1608-1612` —— 反例三「自己挨一次伤害 ⇒ `Prayed` 复位」
类别：(δ) 夹具前提不成立
改了什么：**照诊断的改法 ①**（改夹具、不动引擎）：在 `ApplyDamage` **之前**插
`CheckTrue(c.Has(KeywordTable.Shield))` + `c.RemoveAll(KeywordTable.Shield);`，
并把消息补成「这一下**没有被盾挡下**」。成因（夹具卡的 `Pray` 正文就是 `Pray: Gain Shield`
⇒ 祈完带盾 ⇒ `ApplyDamage` 在盾那一支 `return 0`）写成 5 行注释留在原地。
🧨 那句跟着改了吗：**是**（上面 1598 行那句「删掉 `ApplyDamage` 里那句 … ⇒ 本格红」**原样保留**
且仍然成立 —— 摘盾之后这一下真的打进血里了）。

### 3. `RuleEngineTest.cs:15372-15382` —— §④「督军卡费用」
类别：(α)
改了什么：整块重写。旧：`Check(bad.Count, 0, "★ 督军卡**全部 0 费**…这 9 张逐张开图核过…")`。
新：三条 —— `Check(other.Count, 0)`（只有 `2`/`0` 两种取值）·
`Check(two.Count, 47)`（**有真值的 = 2**）· `Check(zero.Count, 9)`（**真值快照里没有的保持 0**，
并把 9 个名字列出来）。判据链写成注释 = `gen_cards_engine.py:1663-1683`（`A1773`）＋
`BasicCardUI__SetRawCardData.c:51,81,84`（显示侧藏容器）。**「这 9 张逐张开图核过」那句就地处分：**
现读那 9 张**恰恰是没有真值**那一档，不是「图上没印」。
🧨 那句跟着改了吗：**是**（三条各配 🧨：写第三个取值 ⇒ 红 · 撤真值那一支 ⇒ 47 退回 0 ⇒ 红 ·
给 9 张凭空写费用 ⇒ 红）。

### 4 / 5. `RuleEngineTest.cs:15521` · `:15579` —— 真卡 `Telephatic Domination`
类别：(α)（卡名改正 · 同一根因两处）
改了什么：**全文 12 处** `Telephatic` → **`Telepathic`**（含两处 `FindByName`、两处 `HandIdx`
按名查、以及注释/消息）；另把 `GSC_Telephatic_Domination`（2 处）改成现读 id **`GSC49`**。
现读池里那张 = `GSC49 Telepathic Domination`（`tactic`·8 费）；判据 = PnP 文件名
`Genestealer Cult/4计策/Warpforge_64_Telepathic-Domination.png`（铁律 7）。
🧨 那句跟着改了吗：**否**（原文没有 🧨；改用精确名之后，名字写错就会查不到 ⇒ 天然有判别力）。

### 6 / 7. `RuleEngineTest.cs:6059-6063` —— `Sergeant Taaman`：查不到 + `a.Why.Contains`
类别：(α)（第 7 条是第 6 条的**级联** —— `Ok==true` 时 `Why` 是 `null` ⇒ 空引用）
改了什么：**整块反向**。旧：`CheckTrue(!a.Ok, "…查不到")` + `CheckTrue(a.Why.Contains("Sergeant Naaman"), …)`。
新：`CheckTrue(a.Ok, "…查得到（池里那张已按 PnP 卡名改对）")` +
`CheckTrue(a.Cards.Count == 1 && a.Cards[0].Name == "Sergeant Taaman", …)`。
**空引用那一句整句删掉**（不是「先判空」—— 池算出来时根本没有「最近似名」可说），
并在注释里写明「那个抄写差已经不存在」＋`CreatePool.cs:49-50` 的 `Why` 语义。
🧨 那句跟着改了吗：**是**（新增「把池里那张改回 `Sergeant Naaman` ⇒ 上面那行与本行一起红」）。

### 8-12. `RuleEngineTest.cs:11710-11725` —— 5 张 Saim-Hann 督军费用
类别：(α)（与第 3 条同族）
改了什么：`CheckTrue(c != null && c.Cost == 0, …)` → **`== 2`**；块头注释整段重写
（「卡面没有费用六边形」**仍成立**，但它**不是数据取值的判据**）。
🧨 那句跟着改了吗：**是**（每条消息末尾加「撤掉 `gen_cards_engine.py` 里 `hero` 那一支 ⇒ 退回 0 ⇒ 本行红」）。

### 13. `RuleEngineTest.cs:11726-11739` —— 「督军里 cost 非 0 的应 ≤ 9 张」
类别：(α)（第 3 条的第二半）
改了什么：`CheckTrue(nonZero <= 9, …)` → **`CheckTrue(nonZero == 47 && nTwo == 47 && nOther == 0, …)`**，
即「正好 47 张、且**全是 2**」；消息里点名判据出处，并写明「`≤ 9` 那道闸在 `A1773` 落地那天就作废」。
🧨 那句跟着改了吗：**是**（「有人写第三个取值 / 撤真值那一支 ⇒ 红」）。

### 14. `RuleEngineTest.cs:6499` —— `Psychophage` 的天赋名
类别：(α)（`70f4147` 给 `desc` 补了 `Talent: ` 前缀）
改了什么：**这一格反过来钉**：`CheckTrue(phage.TalentName == null, …)` →
**`CheckTrue(phage.TalentName == "Feeding Frenzy", …)`**（现读 desc = `Talent: Feeding Frenzy. Rally: …`，
`CardDef.cs:1677` 那句抽出来的就是它）。
**反面另立一格**（诊断点名要补）：真卡 **`GOF87 Zodgrod Wortsnagga`**
（`keywords:["Mob","Talent"]` · `desc = Deploy a Grot or a Snakebite Grot at random.`）
⇒ `CheckTrue(zz.TalentName == null, …)` —— 两条路都不该产出天赋名
（`ExtractTalent` 找不到 `Talent:`；`Mob`/`Talent` 都被 `KeywordTable.Normalize` 认掉）。
⚠️ 为什么不换夹具：`GOF87` 与 `Psychophage` **同形**（都是「一个真关键词 + `Talent`」），拿真卡更有说服力。
🧨 那句跟着改了吗：**是**（两条各一句：「拆掉 `ExtractTalent` 的 `Talent:` 识别 ⇒ 红」·
「删掉 `CollectTalent` 里 `KeywordTable.Normalize(item)` 那道 `continue` ⇒ 红」）。

### 15. `RuleEngineTest.cs:6524` —— `Sylar Hexcorn` → `Abaddon's Chosen`
类别：(α)（卡名改正）
改了什么：`"Sylar Hexcorn"` → **`"Sylar Hexscorn"`**（现读 `BL36 Sylar Hexscorn`；PnP
`Chaos/1督军/Warpforge_5_Sylar-Hexscorn.png`）；消息里的卡名与后半句（`Abaddons Chosen` 无撇号）**照旧成立**。
🧨 那句跟着改了吗：**是**（「卡名改回 `Sylar Hexcorn` ⇒ 精确匹配落空 ⇒ 红」）。

### 16. `RuleEngineTest.cs:8491` —— `Experimental Drone`（单数 → 复数）
类别：(α)（卡名改正）
改了什么：`"Experimental Drone"` → **`"Experimental Drones"`**（3 处：查池、断言消息、`Debug.Log` 文案；
现读 `TAU42 Experimental Drones`，PnP `Tau/4计策/Warpforge_64_Experimental-Drones.png`）。
**Drone 那半不动**（7 张照旧）。
🧨 那句跟着改了吗：**是**（「改回单数 ⇒ 查不到 ⇒ 红」）。

### 17. `RuleEngineTest.cs:19060` —— （前提·数据侧事实）`Aun'Va` 的 desc
类别：(α)（`70f4147` 补前缀）
改了什么：`av.Desc == "Ethereal Supreme"` → **`av.Desc == "Talent: Ethereal Supreme"`**，
并就地写明「这一格**不再走 ②·b**、改由**规则②**（剥 `Talent:` 段）负责」。
顺带把卡 id 自述 `TAU1` → **`TAU2`**。**下面 `rel.Count == 0` 那条不动**（现读仍成立、仍有判别力）。
🧨 那句跟着改了吗：**是**（「拆掉剥 `Talent:` 段那一支 ⇒ `Ethereal Supreme`（池里真有这张卡）
被当成正文点名 ⇒ 红」）。

### 18 / 19. `RuleEngineTest.cs:19264` · `:19271` —— ⑤「被点到的卡数 / 总处数」
类别：(α)（同一根因两次读数）
改了什么：`Check(distinct.Count, 64, …)` → **`66`**；`Check(mentions, 125, …)` → **`127`**。
**两处注释一起订正**（诊断点名「同一句话写了两遍」）：②·b 头段的「**3 张**裸写 · 各 −3 ·
`128/67 → 125/64`」→「**现读只剩 1 张**（`DA1 Azrael`）· `127 / 66`」，并列出三条 `desc` 字面
（`TAU2` = `Talent: Ethereal Supreme` · `BL34` = `Talent: Chosen of the Four` ·
`DA1` = `…Supreme Grand Master`）。算式 `68−1(S13)−1` / `129−1−1` 与**第二趟自检的实得值
`66 / 127` 逐字吻合**。顺带订正 (2)(3) 两格的 id 自述 `DA3`→`DA1` · `BL1`→`BL34`（诊断「顺手发现 ②」）。
🧨 那句跟着改了吗：**是**（新增「把 `StripTalentSegments` 的**裸写那一半**停掉 ⇒ 回到 68/129 ⇒
这两行一起红」—— 正是 `CreatePool.cs:836-838` 记的复算方法）。

### 20 / 21. `RuleEngineTest.cs:19558-19584` —— ④「①（点名）+ ③（那一族）去重后仍是 4 张」
类别：(α)（**⓪ 真值优先把这一格短路了**）
改了什么：**主卡从真卡 `BL7 Khorne Berzerker` 换成另一张真卡 `EC36 Chosen of Slaanesh`**
（`Give a Dark Pact of Excess to a friendly troop`）—— 它也**在池里**，但**真值表里没有键**
（现读同族还有 `EC9/EC20/EC41/EC38/EC60/2621d428…` 六张，都是后加的帝皇之子那批）。
期望值仍是 **4 / 4** ⇒ 这一格**又真的在测去重**。
另加两条「前提」：① 主卡在真值表里**没有键**（⓪ 不短路）；② **契约名真的出现在本卡正文里**
（⇒ ① 与 ③ 撞的是同一张、去重才有东西可去 —— 这一条只用 `Desc` 与契约名两个数据字面，**不经文本管线**）。
⛔ **没用夹具 `CardDef` 顶**（诊断的改法②）：夹具 `FromOriginalPool == false` ⇒
`BattleDriver.FaceTextFull` **早退**成 `DescribeKeywords(c)`（判据 `BattleDriver.cs:12720`）
⇒ ① 那一支根本喂不进正文、去重照样测不到。**这一条写进注释了。**
🧨 那句跟着改了吗：**是**（「把 `AddUnique` 换成 `list.Add` ⇒ ① 与 ③ 各给一次 ⇒ 实得 5 ⇒ 红」·
前提两条也各带 🧨）。

### 22. `RuleEngineTest.cs:19592-19630` —— ⑤ 「只有英文提到、中文没提的卡 = 0」
类别：(α)（`descZh` 换代成**官方记号**）
改了什么：中文那半的判据从**字面 `黑暗契约`** 改成「**契约那一族的记号**」：把字段里的
`_` 抹掉后按 `markof` + 邪神名认（`markofchaos` / `markofkhorne` / `markoftzeentch` /
`markofnurgle` / `markofslaanesh`）—— 两种写法都覆盖：
`[[markOfChaos]]`（官方驼峰 token）与 `((Gain_a_mark_of_chaos))`（官方未译占位符里的下划线写法）。
**现池实测：`en` 41 / `zh` 41、两个差集都是 0**（改前 `onlyEn` = 29、`onlyZh` = 0）。
⛔ 不是「把数改大」：两边仍各自独立扫全池、仍要求两个差集都是 0。
🧨 那句跟着改了吗：**是**（`onlyEn` 那条：「把 `dpMarks` 那一族删掉 ⇒ 中文只命中 12 张 ⇒ 实得 29 ⇒ 红」）；
`onlyZh` 那条（原本没有 🧨）也补了反向的那句。

### 23. `RuleEngineTest.cs:19789` —— 「`unitsToSummon` 多于一条的正好 1 张」
类别：(α)（`BL36` 已落位进池）
改了什么：`Check(multi, 1, …)` → **`Check(multi, 2, …)`**（`BL23` · `BL36`）。
**消息里的旧说法删掉**：原文「另外两张（`BL36` 与它的教程版）本卡自己落不进我们卡池」——
现读 `talent_cards.json` 的 `entries` 89 条里，`talents` 多于一条的**正好两条**（就是这两条），
**文件里没有第三条**（诊断说的那份 `cards`/`candidates` 结构现读不存在 ⇒ 不照抄那句话，
只写我**现读得到**的）。🧨 那句跟着改了吗：**是**（「把 `TalentCardData.Get` 换成『只留第一条』⇒ 实得 0 ⇒ 红」）。

### 24. `RuleEngineTest.cs:18283` —— 真卡 `Sisters Repentia` 的监听器
类别：(α)（卡名改正 · 复数 → 单数）
改了什么：`"Sisters Repentia"` → **`"Sister Repentia"`**（现读 `SOR10 Sister Repentia`；PnP
`Sorotitas/3部队/Warpforge_19_Sister-Repentia.png`；`Data/CardDatabase.cs:364-367` 是**精确匹配**）。
监听器那半（`WhenTriggers.Count == 1`）**不动**。
🧨 那句跟着改了吗：**是**（「改回复数 ⇒ 精确匹配落空 ⇒ 红」）。

### 顺带（不在 24 条里，纯注释/文案，同一批隐患）
`SOR10 Sisters Repentia`（`:2212`）· `Sisters Repentia` 三处注释（`:8357`/`:18221`/`:18401`）→ 单数；
`:20254` 的 `BL36 Sorcerer` / `BL5 Sylar Hexcorn` → **`BL23 Sorcerer` / `BL36 Sylar Hexscorn`**（id 原来写反了）；
`:13356` 的 445/445 **保留原数**、只在后面补一句「（2026-10-15 现读 **446/446**）」（那句引的是**改前**的报表读数，直接改数字会把历史改成假的）。

---

## 二、机器复核

- `bash d:/4/Unity/工具/typecheck.sh`：**运行时错误数: 0 / 编辑器错误数: 0**（照抄，四次改动各跑一次，末次 0/0）。
- `git diff --numstat`：`289	113	Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`
  （净 +176 行 ≈ 全是注释与断言文案；**远小于 29602** ⇒ 行尾没被翻）。
- 行尾（二进制读 `io.open(p,'rb')`）：**改前 `crlf=29426` / `lf=29426` → 改后 `crlf=29602` / `lf=29602`**
  （两数恒等 ⇒ 全篇 CRLF，与改前一致）。
  ⚠️ 全程用 **Python 二进制读 → `replace` → 先算进变量再 `write`**，每次替换都打了 `old-occurrences`
  计数（**全部 = 1** 才动手）；没用 `sed -i`、没用 `io.open(p,'wb').write(<表达式>)`。

## 三、没查清 / 没做的

1. **23 条的「实得」没法自己复算**（⛔ 不跑 Unity / `ruleprobe`）。22 条的期望值我按**现池数据**独立核过
   （卡名存在性、张数、`desc` 字面、`talent_cards.json`/`related_cards.json` 结构、`zh` 记号族 41/41）；
   **#18/#19 的 `66/127` 是照诊断那趟的实得值**（算式 `68−1−1` / `129−1−1` 与它吻合，但**我没有独立复算**）。
2. **卡池在诊断那趟之后又变过**（`3f3c0e4f` → `6e8c5786`，差集 = 9 个 `descZh` 字段）。
   我核过**这 9 处对中文计数没有影响**：其中唯一带中文卡名子串的是 `SOR39 Prayer` 的**旧**值
   （`正在祈祷` 里有 `祈祷`，而那正是 `SOR39` **自己**的中文名）—— 而 `CreatePool.PickNamed:1073`
   **把 self 排除**掉了 ⇒ 那一处本来就不计数。其余 8 处新旧值里**没有任何** `nameZh` 子串（脚本现扫）。
   ⚠️ 但**我核不了**输出里 `⑤·b` 那三个中文数（`mentionsZh` 现钉 60 等）会不会因此漂 ——
   那要跑一次 `RuleEngineTest.Run` 才知道。
3. **#6/#7 丢掉的那半覆盖**：原来那格还兼着钉「**报出最近似的名字**」这一条
   （`CreatePool.cs:271-272` 的 `Why += "；最接近的是…"`）。现读**没有任何一条断言**正面钉它了
   （`:6066-6073` 那条只钉 `FindNearMiss == null` 的**反例**）。要补的话得构造一个
   「池里没有、但编辑距离 ≤ 2」的用例 —— ⛔ 我没自己发明（改坏法会静默：`bestDist` 初值 3、
   平票取先遇到的，选谁不稳）。**建议调度台裁**：要么接受、要么单独派一条。
4. **#20/#21 的「去重真的生效」在中文档下仍然弱**：`Find` 的 ① 那一支喂的是
   `FaceTextFull`（`CardText.Zh == true` 时喂 `DescZh`）。若语档是中文、而 `EC36` 的中文正文里
   没有中文卡名，① 会空手而归、这一格就退化成「只测 ③」。我**没有**为此再加断言
   （加「① 必须命中」会引入一条可能新红的断言，而批处理模式下 `CardText.Zh` 的实际取值我没验）。
   ⚠️ 已在注释里写明「夹具换真卡的原因」，但这半**如实留空**。
5. **#22 的口径还没和 `CreatePool.MentionsDarkPact` / `IconSetup.Verify` 收口**（见下）。

## 顺手发现（超出本任务，供调度台分流）

1. 🔴 **同一个「记号族」判据现在有三处各写一份**：`CreatePool.MentionsDarkPact` 的中文兜底
   （`CreatePool.cs:1105-1112`，**仍只认 `黑暗契约` 字面**）· 本次 `RuleEngineTest` ⑤ 的本地判据 ·
   `A2045` 那条（`IconSetup.Verify` 的 `ZhGaps`）。诊断「顺手发现 3」建议**一起改、别各改各的** ——
   我这次只能在 `RuleEngineTest.cs` 里改，**另两处没动**（不在白名单，且 `IconSetup.cs` 是**别人正在改的文件**）。
2. ⚠️ **`CreatePool.cs:267-269`** 的注释还举着 `Sergeant Taaman`（实体卡）↔ 数据里写 `Sergeant Naaman`
   当例子 —— **那个抄写差已经不存在了**（同 #6）。同理 `CreatePool.cs:836-848` 那段
   「`128/67 → 125/64`」的读数也是**改前口径**（现读 127/66）。**两处都在 `CreatePool.cs`，我没动。**
3. ⚠️ `RuleEngineTest.cs:2968` 的 `$"完全解析 {cov.Full}/448…"` 里那个 **448 是写死的显示串**
   （不是断言期望值）；战术卡张数 +1 之后它可能已经对不上 `cov.SegTotal` 的真实值。**只提醒，没改**
   （改了也不知道现值是多少 —— 要跑一次才知道）。
4. ℹ️ **诊断里「`talent_cards.json` 的 `cards`/`candidates` 长度 > 1 的 3 条」现读不成立**：
   该文件现读的键是 `entries`（list of `{id, talents}`），**没有 `candidates` 这个键**，
   且 `talents` 多于一条的**只有 2 条**。我按**现读**写断言文案（#23）。
5. ℹ️ 本次**没有**跑 Unity / `ruleprobe` / `_run_8_checks.sh`（红线），只跑了 `typecheck.sh`（廉价、且是
   唯一挡「编不过」的）。⇒ **24 条是否真全绿，要等调度台在同步点跑一次 `RuleEngineTest.Run` 才能确认。**
