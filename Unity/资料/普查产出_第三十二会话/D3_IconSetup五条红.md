# D3 —— `IconSetup.Verify` 五条红逐条判定（2026-10-15 · 只读诊断，未跑 Unity）

日志 = `d:/4/_tmp_view/iconsetup_verify.log`（`^ICONSETUP !!` 共 **5** 行，与末行「有 5 条没过」一致）。
判定用到的**只看得到、不能改**的东西：现盘计划表 / `git show HEAD:<计划表>`（读，未动 git）/ 卡池
`RuleEngine/Resources/cards_engine.json`（HEAD 与工作区**逐字段比过**）/ `gen_icon_plan.py` 的 HEAD↔工作区 diff。

---

## 一、逐条判定（5 条）

### 1. `IconSetup.cs:351` —— ②e「裸关键词：图标插在词前面 · 词留着 · `<link>` 只包一层」
类别：**(α) 断言夹具过期**（触发者 = 本会话的**计划表重生成**，R5 `gen_icon_plan.py --write`）
判据：
- `IconSetup.cs:350`　`string ly = CardPresentation.CardIcons.Rewrite("SAU23", "descZh", "残骸。装甲 2。");`
- 现盘计划表：**`SAU23` 只有 `desc` 字段，没有 `descZh` 字段**（HEAD 版有：`[("残骸。","remnant"),("装甲 2。","armour"),("先锋。","vanguard")]`）
- `Core/CardIcons.cs:233-235`　`if (!_plan.TryGetValue(cardId, out fields) || fields == null) return text;` / `if (!fields.TryGetValue(field, out items) || items == null) return text;`
该照哪个判据改：**改夹具，不改实现**。实得 `ly` == 原文 ⇒ 四个子句全不成立。根因两条：①现池
`SAU23.descZh = '[[remnant]]。[[armour]] 2。[[vanguard]]'` **带官方记号** ⇒ `gen_icon_plan.py:2828-2831`
整字段跳过（该字段运行时走 `CardIcons.cs:227` 的 `RewriteMarks` 分流、**根本不查计划表**）；②那句
`残骸。装甲 2。` 是**旧世代手写中文**，早就不在池里。⇒ 照 ②i/②c 的写法**从卡池取真文本**、并**换一张
走计划表那一代的卡**（现池可选：`ASH77|descZh` = `路标石。潜行。①：获得侧翼。`，计划表已有
`路标石。`→waystone / `潜行。`→stealth；若坚持测 `remnant`，用 `SAU41|descZh` = `复生一个友方残骸。`，
它的 token 是**裸词** `残骸` ⇒ 第 3 条判据要从 `残骸。` 放宽到 `残骸`，否则 `IndexOf("残骸。")` 恒 −1）。
置信度：**高**

### 2. `IconSetup.cs:380` —— ②f2「圈码 token（`①`）同样被吃掉」
类别：**(α) 断言夹具过期**（同一触发者）
判据：
- `IconSetup.cs:379`　`string circ = CardPresentation.CardIcons.Rewrite("ASH80", "descZh", "①：给一个友方部队");`
- 现盘计划表：**`ASH80` 只有 `desc` 字段**（HEAD 版有 `[('①','SpiritStone_1'),('护盾','shield')]`）
- 现池 `ASH80.descZh = '[[waystone]]。当你收集到灵魂石时，获得[[shield]]。((Spirit_x,1)) 从你的卡组中选择一张牌并抽取它'`（**带记号** ⇒ 整字段跳过）
该照哪个判据改：同 ①。夹具那句 `①：给一个友方部队` 是手写的；**换成池里真带 `①` 的卡**：`ASH83|descZh`
= `①：给予你的所有部队 +1 [攻击]、+1 [远程]和 +1 生命`，计划表现读有 `('①','SpiritStone_1')`（该字段**不带记号**、
走计划表）⇒ 两条子句（`①` 没了 + 出现 `SpiritStone_1`）都能站住。
置信度：**高**

### 3. `IconSetup.cs:404` —— ②g「换两遍和一遍结果相同（幂等）」
类别：**(α) 断言夹具过期**（同一触发者）
判据：
- `IconSetup.cs:399-401`　`Rewrite("GOF101","descZh","给予一个友方单位[践踏]。如果其已经拥有[践踏]，改为本回合给予其 +2[攻击]。")` 再喂一遍
- 现池 `GOF101.descZh = '给一个友方单位[[stomp]]。如果它已经有[[stomp]]，则本回合改为给它((Melee_attack_icon_x,+2))'`（**带记号**）
- 现盘计划表 `GOF101` 只有 `desc`：`[('[Stomp]','stomp')×2, ('[Attack]','Melee')]`（HEAD 版的 `descZh` 是 `[('[践踏]','stomp')×2, ('[攻击]','Melee')]`）
该照哪个判据改：`g1` 未被替换（`[践踏]` 一处也没换）⇒ `CountOf(g1,"stomp")==2` 不成立。
**两条路，选一条并写清**：(A) 把夹具换成**走计划表那一代**的真文本 —— `GOF101|desc`（现读
`Give [Stomp] Stomp to a friendly unit. If it already has [Stomp] Stomp, give it +2 [Attack] this turn instead`）
⇒ `stomp`×2 + `Melee`×1 与幂等**同时**成立；(B) 若一定要测中文那半，就该**按 `RewriteMarks` 那一支的
期望值**写（`[[stomp]]` 两处），并另记一条账（那是另一条链的判据）。⚠️ 无论哪条，先给这一族加一条
「**夹具今天还走得到这一支吗**」的前提判据（照 ⑦·前提 `A1627` 的写法）—— 否则下次数据换代它又会
「静默空转」（现在 ②h 就是空转绿：`ly` 没被换过，`Rewrite(ly)==ly` 恒真）。
置信度：**高**

### 4. `IconSetup.cs:533` —— ④a「英文画了的每一枚徽记、中文也画」
类别：**(β) 实现缺陷**（`gen_icon_plan.py` 的中文段首扫描不认 `<br>`）；**暴露者 = 本会话的计划表重生成**
（不在简报列的 5 处之内，是第 6 处；简报的知识块里提到了它 = R5）
判据：
- `Editor/IconSetup.cs:981-1001`　`if (!sawZhField) continue;`（本会话补的那一跳）+ `ZhGaps` 谓词 `:972-1009`
- `工具/gen_icon_plan.py:2152`　`KEYWORD_SCAN_ZH = re.compile(r"(?:^|[。；,，]\s*)([一-龥]{1,8})(?:\s*(\d+))?\s*(?:([：。])|(?=$))")`
  —— 前导分隔符**只有** `^` / `。` / `；` / `,` / `，`，**没有 `<br>`**
- 现池 `ASH34.descZh = '护甲 2。<br>②：获得爆裂 6 和伪装。<br>天赋：凯恩之怒。'`（`descZl` 侧 `<br>` 共 **60 张卡**、`desc` 侧 **0 张**）
该照哪个判据改：**改生成器，⛔ 不许往 `KnownZhGaps` 里加**（④c + 铁律 11/14）。把 `<br>`/`<br/>`/`<br />`
收进 `KEYWORD_SCAN_ZH` 的前导（与 `Core/CardText.cs:447-453` 的 `EndsWithLineBreak` 同口径），重跑
`gen_icon_plan.py --write` ⇒ 那两条回来 ⇒ ④a 归零（我已离线复算：`ZhGaps` 现读恰好 = 这 2 条，
去掉 `sawZhField` 那一跳是 769 条，**没有第三类**残留）。⚠️ 重跑前注意：`SOR71` 已有裸词 `集结`，
新正则会再产出 `集结：` ⇒ 撞生成器自己那条「同字段内 token 互相包含 = 0」自检，要一并让位。
置信度：**高**（数据链完整；未跑 Unity，故「重跑后 ④a 归零」是按离线复算预测，不是实跑读数）

### 5. `IconSetup.cs:565` —— ④b【灭自证】把 19 条「改回去」⇒ ④a 必须认出来
类别：**(α) 夹具过期**（判据的**形式**仍然成立，坏的是那 19 条的**前提**）
判据：
- `IconSetup.cs:1146-1153`　`RevertControl = { "UM74|oath", …, "UM96|invulnerable" }`（19 条）
- `IconSetup.cs:1143`　「⚠️ 与真表的关系：这 19 条今天**都在**真计划表的 `descZh` 里（两笔都落地了）」← **现读不成立**
- `IconSetup.cs:565`　`Check(removed == RevertControl.Length && missed.Count == 0, …)`
实测（我把两份计划表都数了一遍）：**HEAD 版 6/19 · 现盘 5/19**（现盘在库的是
`DA82|invulnerable` `SW29|invulnerable` `SW18|invulnerable` `UM88|invulnerable` `UM96|invulnerable`
⇒ `removed=5`、`caught=5`，与日志逐字一致）。⇒ **本会话之前它就是红的**（HEAD 也只有 6/19），
**不是本会话的回归**（本会话只让它多丢 1 条：`GOF21|mob`）。
该照哪个判据改：**把那 19 条换成「从现计划表推导」**，别留手写清单（手写清单一定会随数据换代烂掉——
这次就是）。推荐写法：扫全表取「`desc` 与 `descZh` **两个字段都非空**、且同一 `sprite` 两边都有」的
`(卡,sprite)` 对，取前 N 条（或全取）当控制集，抽掉副本里 `descZh` 的那些条目 ⇒ 要求 `ZhGaps(broke)`
**全部**报出来。**保留** `removed == 控制集长度` 这条前提检查（它就是今天把「夹具过期」判红的那个东西）。
⚠️ 语义上**不要**降级成「抽任意一条 descZh 条目」——那会放过「谓词恒绿」这一档；控制集必须**同时**满足
「`desc` 侧有、`descZh` 侧有」（今天 19 条里 14 条连字段都不在，抽掉等于没抽）。
置信度：**高**

---

## 二、④a 那 2 条真缺口的深挖（`ASH34|talent` · `UM23|codex`）

**一句话：真缺口，不是残余假阳。** 两条都指向同一个缺陷：**中文段首声明写在 `<br>` 之后时，
计划表抽不出来 ⇒ 中文卡面不画那一枚**。

**中文那半边到底画没画（按今天的两条渲染路各走一遍）**
1. 计划表那一路（`CardIcons.Rewrite`）：`ASH34.descZh` / `UM23.descZh` **都不带官方记号**
   （`HasOfficialMarks` 为假）⇒ 走 `CardIcons.cs:227` 下面的**计划表**分支 ⇒ 计划表没这条 = **不画**。
   实测计划表：`ASH34|descZh` 4 条（护甲/②/伪装/爆裂）· `UM23|descZh` 2 条（护甲/先锋）——
   少的正好是 `天赋：` / `典籍：`。
2. `KeywordSegment` 那一路（声明行）：本会话的 `CardText.IsDeclarationAt` 补了「`<br>` 收尾也算段首」
   ⇒ `DeclaredInBody(descZh,"天赋")` 由**假变真** ⇒ `KeywordSegment` **不再补**那一枚。
   ⇒ **两侧叠加 = 中文卡面这一枚彻底没有**（改前是「声明行里多印一枚」，改后是「一枚都没有」）。
   ⚠️ 注意方向上这条改动**本身是对的**（卡面确实印两遍了），它只是**假定正文那一枚还在**（见
   `CardText.cs:431-441` 的注释：那段 `我们印 …〔天赋〕天赋：凯恩之怒。` 是拿**重生成之前**的计划表读的）。
   修 `KEYWORD_SCAN_ZH` 之后这两件事才自洽。

**计划表为什么抽不出来（根因，可复算）**
`KEYWORD_SCAN_ZH`（`gen_icon_plan.py:2152`）的句首前导 = `^` / `。；,，`；而这两处声明的前一个字符是
`>`（`…伪装。<br>天赋：…` / `先锋。<br>典籍：…`）⇒ 正则不认。**对照实验（同文件内）**：
`GOF76.descZh = '践踏。\n天赋：它们越大……'` / `SW24.descZh = '友方载具拥有再生 1。\n集结：抽 1 张载具。'`
—— 用 `\n` 断行的**都抽得出来**（计划表里真有 `天赋：`→talent / `集结：`→rally），用 `<br>` 的抽不出来。
⇒ 根因就是 **`\n` 在集合里、`<br>` 不在**。

**面有多大（现池实测）**
全池 `descZh` 里 `<br>` 后紧跟中文声明的**只有 3 处**：`ASH34|天赋` · `UM23|典籍` · `SOR71|集结`
（`SOR71` 因为另有裸词 `集结` 条目兜着，中文那一枚照画 ⇒ **不构成缺口**）⇒ 真正失画的**就这 2 张**。
（`desc` 侧 `<br>` = **0 张**，所以本会话那条 `<br>` 改动**只可能**影响这 3 处中文。）

**该修什么**：`gen_icon_plan.py` 的 `KEYWORD_SCAN_ZH` 收 `<br>`（一处正则，同 `EndsWithLineBreak` 口径）
+ 重跑 `--write`。**不该修什么**：⛔ 不要把这 2 条写进 `KnownZhGaps`（④c 会红，且那正是「把新缺口当基线」）；
⛔ 不要只改卡池中文文本去迁就正则。

---

## 三、分类汇总 + 要动手修的清单

| # | 条目 | 类别 | 修哪里 |
|---|---|---|---|
| 1 | `IconSetup.cs:351` ②e | (α) 夹具过期 | `.cs` 换卡/换真文本（`ASH77` 或 `SAU41`）+ 前提判据 |
| 2 | `IconSetup.cs:380` ②f2 圈码 | (α) 夹具过期 | `.cs` 换成 `ASH83|descZh` 真文本 |
| 3 | `IconSetup.cs:404` ②g | (α) 夹具过期 | `.cs` 换成 `GOF101|desc`（计划表那一代）或改成按 `RewriteMarks` 写期望值 |
| 4 | `IconSetup.cs:533` ④a | **(β) 生成器缺陷** | `gen_icon_plan.py:2152` 收 `<br>` + 重跑 `--write` |
| 5 | `IconSetup.cs:565` ④b | (α) 夹具过期 | `.cs`：`RevertControl` 改成**从计划表推导** |

分类计数：**(α) 4 条 · (β) 1 条 · (γ) 0 条 · (δ) 0 条**。
- **没有一条是本会话那 5 处改动「做错了」**：`.cs` 侧本会话只加了 `sawZhField` 一跳（**纯插入 13 行、0 删**）
  和 `IsDeclarationAt` 的 `<br>` 一支；五条红的**共同触发者 = 本会话的计划表重生成**（R5 `--write`，
  = 简报那 5 处之外的**第 6 处**）。
- 判别证据（可复核）：**①~③ 与 ⑤ 在 HEAD 的计划表上都是绿的** —— HEAD 版计划表分别有
  `SAU23|descZh` 三条 / `ASH80|descZh` 两条 / `GOF101|descZh` 三条；④b 那 19 条当时有 6 条（仍红，
  **所以 ⑤ 是本会话之前就红的**）。前提是「盘上那份旧计划表**本来就是假的**」——它写于 `descZh`
  还在旧世代时（R5 已证：现池重跑不出来它）。
- 🔴 **④a 那 2 条与 ①②③ 不是一回事**：前者是**真缺陷**（卡面真少一枚，必须修）；后者是**夹具跟不上
  数据换代**（实现没坏，是断言喂了旧世代的文本）。⛔ 别把 ④a 也当成「改夹具就完事」。

---

## 四、没查清 / 判不了的

1. **没跑 Unity**（简报红线）⇒ 上面关于 `Rewrite` / `KeywordSegment` / `HasOfficialMarks` 行为的结论
   **全部来自读源码 + 读数据 + 日志本身**，没有实跑读数。特别是「修完 `KEYWORD_SCAN_ZH` ⇒ ④a 归零」
   是**离线复算的预测**，不是实跑。
2. **`RewriteMarks` 那一支没独立复核**：本会话 459 个 `descZh` 字段（+66 张整卡）被跳过，
   其正当性依赖「那批字段运行时由 `RewriteMarks` 画、不查计划表」这条前提（判据 `CardIcons.cs:227`
   我读了、成立）；但**「`RewriteMarks` 真的把那几枚画出来了」我没有逐张验**（那是 R5 的结论，我转述）。
   若这条前提有洞，④a 的 `sawZhField` 那一跳会**把真缺口一起藏掉**——这是 ④a 今天**最大的残余风险**。
3. **④a 只看「两侧不对称」**：两侧都没画的（如 `ASH42` 的 `Blast`/`Camouflage`）它看不见；
   `<br>` 那一类里若有「英文侧也没有对应 sprite」的卡（⇒ 中文也少一枚，但英文同样少），
   本条**不会报**——我按形状扫出只有那 3 张、且另一侧情形已逐一核过，但**没有拿 PnP 卡图逐张复验**。
4. **`UM23` 的 PnP 坐标没找到**：池里 `UM23 = 'Bladeguard Lieutenant'`，`Ultramarines/3部队/` 下
   只有 `Warpforge_23_Bladeguard-Ancient.png` / `_34_Bladeguard-Veteran.png` 等，**没有 Lieutenant**
   ⇒ 我**没能**给 `UM23|codex` 附一张亲读的成品卡图（`ASH34` 那张有：`Aeldari/3部队/Warpforge_42_Avatar-of-Khaine.png`，
   `CardText.cs:439` 已引用）。判定改用「英文 `desc` 画了 ⇒ 中文也要画」（④a 自己的口径）。
5. `工具/gen_icon_plan.py` 那 6 个字段差异（`GSC45/56/65` 的 `破坏→Sabotage`、`SOR71` 的 `集结：→集结`）
   我核了**结果是对的**（`Sabotage` 才真的出现在现池文本里、`破坏` 不在），但**没查产生它的那支规则**，
   也没查「还有没有别的隐藏漂移」（R5 用自己的同池 baseline 说没有，我只核了这 6 条 + 卡池 diff 9 条）。

---

## 顺手发现

1. **`IconSetup.cs:537` 那句话打错了数**：`"；**已知缺口 " + gaps.Count + " 条**（`KnownZhGaps`…）"`
   —— `gaps.Count` 是**总缺口数**（今天 2），而 `KnownZhGaps` 是**空表**（`Length = 0`，④c 现读 0/0）
   ⇒ 日志读起来像「有 2 条已被基线表收编」。简报里 ④a/`④a 已知缺口清单` 两行的疑问就是这么来的。
   建议就地改成「**总缺口 N 条（其中在基线表里的 M 条 / 新缺口 K 条）**」。
2. **②h 现在空转绿**：`Check(ly == Rewrite("SAU23","descZh", ly), "②h 裸关键词那条也幂等")` —— `ly` 根本没被
   换过 ⇒ 恒真。它与 ①②③ 同源（都吃 SAU23 的那份旧世代夹具），**修 ②e 时要一起修**，否则会留下一条
   「绿着但什么也没测」的断言（同 `A1627` 那条「前提判据」要治的病）。
3. **②c 的 id 与文案对不上**：代码用 `Rewrite("DA39","desc", da.Desc)`，文案写「②c 方括号记号换掉
   （**DA44** 真文本…）」。注释段里通篇说 `DA44/desc`、`DA39`。这两个号都好用，但**同一个断言里两个
   号混着写**，下一个会话很容易照文案去卡池找 DA44 而找不到（`d:/4` 里 `DA44` 是本仓的卡 id 还是
   PnP 图号？见 `card_ids.json` 那条老账）。低优先，但建议统一。
4. **`IconSetup.cs:1143` 那句必须就地订正**（铁律 5）：「这 19 条今天都在真计划表的 `descZh` 里」
   —— 现读 **5/19**（HEAD 版 6/19）。连同 ④b 的修法一起改，别只改代码不改注释。
5. **`gen_icon_plan.py` 的自检面本身没有「用户可见面」**：`跳过的字段（官方记号）` 只报一条总数，
   而今天的损失（`ASH34|talent` / `UM23|codex`）恰恰出现在「**没被跳过的**那 413 个 `descZh` 字段」里
   —— 建议加一条「非记号字段里 `<br>` + 声明 的处数」计数（现读 3），让它下次自动现形。
6. 本会话的 `<br>` 改动**方向上只减不增**（`CardText.cs:441` 自己写了），**但它消失的那一枚必须有
   「另一条路画得到」的前提** —— 这次的教训是「两条链各改一半」，建议在 `CardText.cs:431-441` 那段注释里
   补一句指针：**「正文那一枚由计划表画 ⇒ 前提是 `KEYWORD_SCAN_ZH` 也认 `<br>`」**（否则下一个会话
   会再照那段注释得出「删掉重复的那一枚就对了」的结论，而结果是 0 枚）。
