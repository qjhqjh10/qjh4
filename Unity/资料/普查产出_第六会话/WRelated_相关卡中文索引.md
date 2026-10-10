# WRelated · §19「相关卡」中文索引（第六会话 · 动手写手交件）

> 白名单只两个文件：`RuleEngine/Core/CreatePool.cs` · `CardPresentation/Core/RelatedCards.cs`。
> **两个都改了**，白名单之外**一个字都没动**（不跑 Unity、不动 git、不改正本、不改 `RuleEngineTest.cs`）。

---

## ① 结论（逐条一句话）

1. ✅ **中文档下「相关卡」现在按中文卡名匹配了** —— 新加了第二张索引 `NormCjk(NameZh) → 卡名`，并在 `MentionedCards` 里加了「中文那一趟」，两趟按**在文本里的位置**合并。
2. 🔴 **简报第 1 条的前提被现读推翻（重要）**：**照字面做 `Norm(c.NameZh)` 索引是一个彻底的空操作** —— `Norm` 只留 `a-z0-9`，**汉字一个都不留**，实测 **1126/1126** 张卡的 `Norm(nameZh)` **全是空串**，而 `BuildNameIndex` 与 `MentionedCards` 都有 `k.Length == 0 ⇒ 跳过` ⇒ 键全空、一条都查不到、**还不出声**。必须先造一套**保留汉字**的归一化（`NormCjk`）。
3. 🔴 **简报第 2 条的一半被现读推翻**：`RelatedCards.cs:43`（点名那一支）**照做了**（按语档取 `Desc`/`DescZh`）；**但 `:56-62` 的 `phrases`（池子那一支）我没改、也不该改** —— `PoolFromPhrase` 是**英文句法解析器**，喂 `DescZh` 会让这一支**在中文档下整个消失**（真回归）。判据见 §③·D。
4. ✅ **英文档行为逐字不变** —— 英文那一趟的匹配逻辑一个字没动，且中文那一趟在无汉字文本上被**严格等价地**短路掉。**已用真代码跑出来**：128 处 / 67 张，与 `RuleEngineTest` 的 10 个既有期望数**全部吻合**（§③·E）。
5. ✅ **中文档（改后）：132 处 / 67 张**（改前拿英文文本算是 128 处 / 67 张）—— 6 张卡各多 1 处、2 张卡各少 1 处，**每一条都能逐条解释**（§③·C）。
6. ✅ 秒级类型检查 **0 错 / 0 错**；两个文件行尾**都是纯 LF，改前改后一致**（没翻）。
7. ⚠️ **没补断言**（`RuleEngineTest.cs` 被别的写手占用）⇒ 建议的断言写在 §⑦。

---

## ② 改动清单

### A. `Unity/MyGame/Assets/RuleEngine/Core/CreatePool.cs`（LF，+192 / −23）

| 位置（改后行号） | 改前 | 改后 |
|---|---|---|
| `:522-575`（新增，接在 `Norm` 之后） | **无** | 新增「汉字那一套归一化」注释块 + 三个成员：`IsCjk`（`:541`，`c >= '\u4e00' && c <= '\u9fff'`）· `HasCjk`（`:546`）· **`NormCjk(s, srcIndex = null)`**（`:559`，小写 + 留 `a-z0-9` **外加汉字**；可选出参填「归一化后的第 k 个字 ← 原串第几个」） |
| `:600` | **无** | 新增字段 `static Dictionary<string, string> _nameIndexCjk;`（中文名索引，值 = **`c.Name`** 而非 `NameZh`，理由写在注释里：回池取实体共用 `PickNamed`） |
| `:604` | **无** | 新增字段 `static int _nameIndexCjkMaxLen;`（扫描上界，实测 15） |
| `:610` | **无** | 新增 `const int MinCjkNameChars = 2;`（全池最短中文名就是 2 个字，64 张） |
| `:614-648` `BuildNameIndex` | 只建英文一张 `idx`，`if (c == null \|\| string.IsNullOrEmpty(c.Name)) continue;` | **并排再建 `idxCjk`**（只收 `HasCjk(c.NameZh)` 的卡），并把 `_nameIndexCjk` / `_nameIndexCjkMaxLen` 一起赋值。⚠️ 英文那张的**判据与取值一字未变**（只是把 `continue` 展开成 `named` 判断，语义等价 —— 见 §③·E 的实跑复核） |
| `:707-792` `MentionedCards` | 单趟：`words = StripTalentSegments(text).Split(...)`，一个 `for` 循环，每命中就 `outp.Add` | **两趟**：① 英文那趟**匹配逻辑一字未改**（只多了「算每个词在剥离后原串里的起点 `wpos[]`」，因为要按位置合并）；② 🆕 中文那趟（`HasCjk(s)` 短路闸 → `NormCjk` + 逐位置**最长优先**匹配）；两趟都走 `AddMention` 按位置插入。返回 `hitDefs` |
| `:797-806`（新增） | **无** | `static void AddMention(List<int> pos, List<CardDef> hitDefs, int at, CardDef c)` —— 按位置升序**插入**、同位置先到先排、`Id` 相同只收一次。用插入而非末尾 `sort` 的原因写在 doc 里：**英文那趟本来就是升序 ⇒ 结果与从前逐字相同** |

### B. `Unity/MyGame/Assets/CardPresentation/Core/RelatedCards.cs`（LF，+33 / −1）

| 位置（改后行号） | 改前 | 改后 |
|---|---|---|
| `:52`（原 `:43`） | `CreatePool.MentionedCards(pool, card, card.Desc, out why, max)` | `string mine = TextForLang(card.Desc, card.DescZh);` → `CreatePool.MentionedCards(pool, card, mine, out why, max)` |
| `:66-75`（原 `:55-62`） | 只有 `var phrases = …`，无注释 | 🔴 **只在上面加了 8 行注释，代码一行未改**（`phrases` 仍然喂英文）—— 写清为什么不能跟着语档走（英文解析器 + 产出与语言无关 + `TalentName` 本来就从英文 `Desc` 抽） |
| `:125-128`（新增） | **无** | `static string TextForLang(string en, string zh)` → `return (CardText.Zh && !string.IsNullOrEmpty(zh)) ? zh : en;` —— **逐字照** `BattleDriver.FaceTextFull`（`:11134-11135`） |
| `:16-21`（文件头 doc） | ①只有一句「效果文本里点名的卡」 | 补一句：喂哪份文本**按当前语档取**（见 `TextForLang`）+ ⚠️ 另两支**不**跟语档走 |

---

## ③ 证据

### A. 现读行号（判据出处）

| 事实 | 出处 |
|---|---|
| `Norm` 只留 `a-z0-9` | `CreatePool.cs:510-520`（`if ((c >= 'a' && c <= 'z') \|\| (c >= '0' && c <= '9')) sb.Append(c);`） |
| 建索引时**空键被跳过** | 改前 `CreatePool.cs:603`（`if (k.Length == 0 \|\| idx.ContainsKey(k)) continue;`）；`MentionedCards` 里同一句在改前 `:710` |
| 中文那支的 parse 是英文句法 | `EffectText.Parse`（`RuleEngine/Core/EffectText.cs:989`）；`grep` 该文件：**110 处非注释中文全是「描述生成」的字面量**，解析侧**零**中文分支 |
| `FilterChoose` 只认英文 | `CreatePool.cs:290-310`（剥 `" cards"` / `" card"` / `"friendly "` / `"your "` / `"non-legendary "` / `"non-ephemeral "`） |
| `TalentName` 来自**英文 `Desc`** | `RuleEngine/Core/CardDef.cs:1012` `TalentName = ExtractTalent(Desc);`（裸写那支在 `:1026`） |
| 「按语档取文本」的既有口径 | `CardPresentation/Battle/BattleDriver.cs:11134-11135`：`bool zh = CardText.Zh && !string.IsNullOrEmpty(c.DescZh); string body = zh ? c.DescZh : c.Desc;` |
| `CardText.Zh` 的定义 | `CardPresentation/Core/CardText.cs:36`：`TmpFont.Available && Loc.Current == AvailableLanguages.Chinese` |
| 两扇窗都走这一份判据 | `Shell/CardDetailPopup.cs:439` · `Battle/CardDisplayWindow.cs:649` |

### B. 全池摸底（python 直读 `RuleEngine/Resources/cards_engine.json`，1126 张）

| 量 | 值 |
|---|---|
| 卡池张数 | **1126**（`count` 字段与数组长度一致） |
| `nameZh` 非空 | **1126 / 1126** |
| 其中**纯汉字**（一个字母数字都没有） | **1126**（混字母数字 **0** · 纯 ASCII **0**） |
| 2 个字的 `nameZh` | **64** 张（`总督`/`先知`/`幽卫`/`猎鹰`…） |
| `Norm(nameZh)` 为**空串**的 | 🔴 **1126 / 1126** |
| `NormCjk(nameZh)` 长度分布 | `2..15`（`2`×64 · `3`×124 · `4`×520 · `5`×191 · `6`×113 · …） |
| `descZh` 缺失 | **6** 张；`descZh` 含汉字 **1120 / 1120**（非空的全都含） |
| `desc` 含汉字 | **0** 张（⇒ 中文那趟在英文档**必然空跑**） |

### C. 🔴 改前 / 改后（**这是本次的行为变更**）

**复现办法**：§③·E 那个 harness（**真跑的是编译出来的 RuleEngine 代码，不是模型**）。口径 = 把 8 张上限去掉看全集。

| | 总处数 | 被点到的卡数 |
|---|---|---|
| **改前**（今天：两个语档都喂英文 `Desc`） | **128** | **67** |
| **改后 · 英文档**（仍喂 `Desc`） | **128**（🟢 逐字不变） | **67**（🟢 逐字不变） |
| **改后 · 中文档**（改喂 `DescZh`） | **132** | **67** |

**逐条差异（8 张卡，全都能解释）**：

| 卡 | 英文文本 | 中文文本 | 差异 |
|---|---|---|---|
| `Flayed One` | `When Reanimated, gain Fast.` | `被复生时，获得迅捷。` | **+`Reanimate`** —— 中文里「复生」既是关键词又是卡名，词边界分不开（**已知的松**） |
| `Gauss Reaper Warrior` | `When Reanimated, …` | `被复生时，…` | **+`Reanimate`** —— 同上 |
| `Immortals Phalanx` | `When reanimated, deploy a copy…` | `被复生时，部署此部队的一个复制。` | **+`Reanimate`** —— 同上 |
| `Lokhust Heavy Destroyer` | `When Reanimated, deal 3 damage…` | `被复生时，对一个随机敌人造成 3 点伤害。` | **+`Reanimate`** —— 同上 |
| `Burna Boy` | `…control a Spanner or Mekboy Gazmek…` | `…控制一个兽人扳手或技师小子加兹梅克…` | **+`Ork Spanner`** ✅ **真赚**：英文写的是 `Spanner`（少了 `Ork`，全等匹配落空），中译写的是**卡名逐字**「兽人扳手」 |
| `Awakened Obelisk` | `…add Extermination Protocol to your hand` | `…将一张歼灭协议加入你的手牌。` | **+`Extermination Protocols`** ✅ **真赚**：英文是单数 `Protocol`（`Singular` 只剥尾 `s`，剥不出），中译写的是**卡名逐字** |
| `Azrael` | `… Supreme Grand Master`（**裸写天赋名**） | `…天赋：至高大师` | **−`Supreme Grand Master`** ✅ **中译反而更对**：英文**没有** `Talent:` 前缀 ⇒ 规则②剥不掉 ⇒ 假命中；中文写了 `天赋：` ⇒ 规则②生效 |
| `Aun'Va` | `Ethereal Supreme`（**整条就是裸写天赋名**） | `天赋：至尊以太` | **−`Ethereal Supreme`** —— 同上（中译更对） |

⇒ **口径小结**：6 处新增里 **2 处是真赚**（中译写了卡名逐字）、**4 处是「中文关键词与卡名同词」的松**；2 处减少是**中文侧把规则②真正落实了**（英文侧是假命中）。**没有一处是漏。**

**`MinCjkNameChars` 的敏感性（实测）**：`2` ⇒ 132 处 / 67 张 · `3` ⇒ 111 / 62 · `4` ⇒ 92 / 50 ⇒ 取 **2**。

### D. 🔴 为什么 `phrases`（池子那一支）**没**跟着语档走

- `PoolFromPhrase`（`CreatePool.cs:752`）的两条来路都认**英文**：① `EffectText.Parse` 取 `choosecard` 的 `ChooseWhat`（只认 `Choose a …`）· ② `"a random "` / `"random "` 前缀。中文文本**两条都不满足** ⇒ `what == null` ⇒ 返回 `null`。
- 下游 `FilterChoose`（`:281-310`）也只剥**英文**后缀、按**英文** subtype（`SubtypeIn`）与**英文**阵营名筛。
- ⇒ 喂 `DescZh` = 中文档下这一支**整个消失**（真回归）；而**这一支产出的是 `CardDef` 列表、与语言无关**，喂英文**不丢任何东西**。
- ⚠️ `TalentName` 也**只能是英文**（`CardDef.cs:1012` 从英文 `Desc` 抽）⇒ 连它一起「按语档取」根本不成立。
- ⚠️ **诚实标注**：这一条是**读代码推的**，我**没能实跑**（见 §⑤·2）。

### E. ✅ **真代码实跑复核**（不是模型、不是推测）

`工具/typecheck.sh` 编出来的 `WFCheck.dll` 就是全项目运行时程序集 ⇒ 另写一个 `Harness.exe` 引用它，**直接调真的 `CreatePool.BuildNameIndex` / `CreatePool.MentionedCards`**，卡池用 1126 张真卡的 `CardDef` 逐张构造（不碰 `CardDatabase.Load`，因为它要 Unity）。**跑出来的原样输出**：

```
A. EN desc   : mentions=128 distinct=67   (test wants 128 / 67)
   Shock Trooper          = 9 (want 9) OK
   Dark Pact of Excess    = 8 (want 8) OK
   Necron Warrior         = 4 (want 4) OK
   Grey Hunter            = 4 (want 4) OK
   Chosen                 = 0 (want 0) OK
   Abaddons Chosen        = 3 (want 3) OK
   Chosen of the Four     = 1 (want 1) OK
   Chosen of Slaanesh     = 1 (want 1) OK
   Reanimate              = 8 (want 8) OK
B. ZH descZh : mentions=132 distinct=67
   Path of Command      EN=[Storm Guardian ]  ZH=[Storm Guardian ]
   Spiritseer           EN=[Wraithguard ]  ZH=[Wraithguard ]
   Burna Boy            EN=[Mekboy Gazmek ]  ZH=[Ork Spanner Mekboy Gazmek ]
   Awakened Obelisk     EN=[]  ZH=[Extermination Protocols ]
   Azrael               EN=[Supreme Grand Master ]  ZH=[]
   Aun'Va               EN=[Ethereal Supreme ]  ZH=[]
   Flayed One           EN=[]  ZH=[Reanimate ]
   Wave Serpent         EN=[]  ZH=[]
   Ursula Creed         EN=[Shock Trooper ]  ZH=[Shock Trooper ]
D. Reanimate via ZH descZh = 12  (english path = 8)
E. english-doc order spot check (first 3 of 8):
   Drone Companion -> Gun Drone Guardian Drone Marker Drone
```

**A 段那 10 个数是 `RuleEngineTest.cs:16066-16140` 里写死的既有期望值**（`128` / `67` / `9` / `8` / `4` / `4` / `0` / `3` / `1` / `1` / `8`）—— 真代码**全部吻合** ⇒ ① 英文那一趟**没有回退**；② 这个 harness **可信**，所以 B 段的中文数（132 / 67）也**可核**。
（另一条独立复核：先写了一份 python 复刻，**也对上了全部 10 个期望数**，两路结果一致。）

---

## ④ 验证

**① 秒级类型检查**（`TMPDIR=/tmp/wf_related bash d:/4/Unity/工具/typecheck.sh`，原样两行）：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（最后一次改动是**注释**，改完又跑了一遍，仍是 `0 / 0`。没有出现「全集中在白名单之外的文件上」的假错。）

**② 行尾判定（二进制读：`b.count(b'\r\n')` vs `b.count(b'\n')`）**

| 文件 | 改前 | 改后 | 判定 |
|---|---|---|---|
| `RuleEngine/Core/CreatePool.cs` | `CRLF=0 · LF=1221 · 74574 B` | `CRLF=0 · LF=1390 · 86262 B` | ✅ **纯 LF，未翻** |
| `CardPresentation/Core/RelatedCards.cs` | `CRLF=0 · LF=105 · 6790 B` | `CRLF=0 · LF=137 · 10207 B` | ✅ **纯 LF，未翻** |

（全程只用 Edit 工具 + 一次 python `wb`（先算完内容、断言命中 1 处、**再** `write`）；**没用过 `sed -i`**、没碰文本模式 `open(...,'w')`。）

**③ `git diff --numstat`**（只读命令；⛔ 没做任何 git 写操作）

```
33	1	Unity/MyGame/Assets/CardPresentation/Core/RelatedCards.cs
192	23	Unity/MyGame/Assets/RuleEngine/Core/CreatePool.cs
```

数字远离文件行数（1390 / 137）⇒ 没有整篇重写、没有再翻行尾。

**④ 没跑的**：**一条 Unity 自检都没跑**（红线）。因此「中文档下 `CardText.Zh` 在运行期真为 true」这件事**没有在 Unity 里证实过**，只有代码判据（§③·A 最后两行）。

---

## ⑤ 没查清 / 停手的部分

1. **没跑 Unity** ⇒ 三件事**没验**：① 各宿主的自检实际是绿是红（尤其 `RuleEngineTest.Run` · `BattleScene.Run` · `CollectionScene.Run`）；② 两扇详情窗在**运行期**`CardText.Zh` 是不是真的为 `true`（要 `TmpFont.Available` **且** `Loc.Current == Chinese` 两条同时成立）—— 若不成立，中文那趟**不会**生效，那是**设计如此**（回英文），但我**没在 Unity 里看过**；③ 铁律 10⑥ 的「抽一张真卡和 PnP 成品并排比」（本次改的是**取哪份文本**，不是版面，但仍属没做）。
2. ⚠️ **`PoolFromPhrase` 喂中文到底会怎样 —— 我没能实跑**，判据是**读代码**（§③·D）。⇒ 因此我**没有**改 `phrases`。要 100% 落实，加一条断言即可：`CreatePool.PoolFromPhrase(pool, "<一句真 descZh>", false, out what, out why)` 必须返回 `null` 且 `why != null`（期望值来源：随便挑一句真 `descZh`，逐字写进断言）。
3. **`复生`/`Reanimate` 那 4 处松**：我判它是「中文里关键词与卡名**同词**，判据层面分不开」⇒ **没有**为它加任何特例（加特例就等于自己发明口径）。**若用户认为该排除**，需要一条**新判据**（例如「关键词名与卡名冲突时以关键词优先」），**我没查到原版怎么做** —— 搜过的地方：`D:/2/tools/decomp_full/`（反编译）只对到「原版走卡上自带的 `relatedCard1..4` 字段、值在服务端」，**判不出**文本匹配该不该排除同名词。
4. **`MatchCardName`（引擎「按卡名指目标」那条路）仍然纯英文** —— 我**没**给它接中文索引。判据：那条路唯一的输入是 `EffectText.Parse` 的产物，而 parse 只吃英文 `Desc`（`EffectText.cs:7319` 调 `MatchCardName`），**今天没有任何中文消费者**。⚠️ 若将来引擎要吃 `DescZh`，这里会是一个**静默缺口**（键全空 ⇒ 一条都匹配不到、不报错）。
5. **`HasCjk` 只覆盖基本区**（`U+4E00–U+9FFF`）—— 全池 `nameZh` 实测只用到基本区，但没有**穷举**证明「将来也不会有扩展区字」；用了扩展区字的卡会被**静默漏掉**（`NormCjk` 会把它当分隔符剥掉）。
6. ~~**`CardDef.NameZh` 的重名风险没穷举**：`NormCjk` 键「先出现的赢」，而实测 1120 个 `nameZh` **互不相同**（0 组重名）⇒ 今天没有这个问题；**换卡池数据后要重核**。~~
   🔴 **2026-10-18 之后·第六会话就地订正（铁律 5，由 `引擎线三小件` 查出、主对话独立复核）**：
   **原来写 X**：「实测 1120 个 `nameZh` **互不相同**（**0 组重名**）」—— **错**。
   **实际是 Y**：**6 组重名 / 12 张卡**（主对话独立脚本复核一致：`cards 1126 · distinct nameZh 1120 · dup groups 6 · cards in dup 12`）。
   六组逐条（⚠️ 中文列在 GBK 控制台会乱码，这里按**英文名**列）：
   · `Dire Avenger` / `Vindicator`（中译撞车）
   · `Bladeguard Veteran` / `Bladeguard Ancient`（中译撞车）
   · `Company Veteran` / `Company Ancient`（中译撞车）
   · 🔴 `Terminator Champion` / `Terminator Champion`（**英文名也相同** ⇒ 真的重条）
   · 🔴 `Maulerfiend` / `Maulerfiend`（同）
   · 🔴 `Terminator` / `Terminator`（同）
   **错因 Z**：那句只数了「**不同的 `nameZh` 有几个**」，没数「**同一个 `nameZh` 被几张卡用**」。
   ⇒ **对建议稿的影响**：**第 (6) 条照抄会红** —— `NormCjk` 索引是「**先出现的赢**」⇒ 那 3 张**查不回自己**
   （`引擎线三小件` 实测 `notSelf = 3`）。⇒ **必须拆成两条**：**(a) 不变量**（`noKey == 0`）＋ **(b) 重名实测数**（`3`）。
   ⚠️ 那 3 组「**英文名也相同**」的**是真的重条**，不是中译问题 ⇒ **该另开一账**（卡池数据层面的重复），**别当成翻译任务**。

---

## ⑥ 顺手发现的（⛔ 一个字都没改）

1. 🔴 **英文 `desc` 里「裸写天赋名」那一批没被规则②挡住**（§③·C 的 `Azrael` / `Aun'Va`）—— 英文那趟把**天赋名当成了正文点名**（假命中）。`CardDef` 其实**已经有**裸写天赋名的能力（`ExtractBareTalentName`，`CardDef.cs:1092`，`:1026` 在用）⇒ 规则②可以顺手也认它。**建议单开一条 A 表项**（涉及 `CreatePool.StripTalentSegments`，会影响 `RuleEngineTest` 的 128/67 两个期望数 ⇒ 要连断言一起改）。
2. `CardPresentation/Editor/CollectionScene.cs:5290` 注释写「全池 **129** 处点名」—— **陈旧**：`RuleEngineTest.cs:16130-16140` 在 2026-10-18 已订正为 **128**（成因：`UM84 Chaplain Cassius` 的 `[Talent]:` 改成裸写 `Talent:` ⇒ 规则②第一次真正生效）。建议就地改 128，或改写成「见 `RuleEngineTest` 那条」以免再飘。
3. `CreatePool.Norm` 对汉字**恒退化**（全剥），而它叫 `Norm` 且是 `public` —— 这个形状会**给下一个人挖坑**（我自己差点照简报的字面做）。本轮只在注释里写清，**没有**改它的签名/可见性。
4. `BattleScene.cs:6377` 的注释「相关卡算得出几张取决于这张牌」与本次改动方向一致：第一张手牌若正好是那 6 张之一，格数可能从 1 变 2 —— 但那条断言是 `SlotCount >= 1`（`:6379`），**不会红**。
5. 两条宿主里跟「相关卡」有关的样例卡是 `Path of Command`（`BattleScene.cs:6375-6398` · `CollectionScene.cs:5250-5300`）与 `Master of Arcana`（池子那支）—— **两张都实测不受本次改动影响**（前者中英文都命中 `Storm Guardian`；后者走池子那支、一行没动）⇒ **这批改动不需要配对改断言**。

---

## ⑦ 建议的断言（本次**没**补，因为 `RuleEngineTest.cs` 被别的写手占用）

**首选宿主**：`RuleEngine/Editor/RuleEngineTest.cs` 的 `TestMentionedCards`（`:16007`，它本来就是这条判据的宿主；`CheckTrue` / `Check` 现成）——⚠️ **该文件本轮被另一个写手占用，要排期，别抢**。
**备选宿主**：`CardPresentation/Editor/CollectionScene.cs`（`:5220-5260` 已有 `Path of Command` 的相关卡那一段，且有 `CreatePool` 可见）。

### 建议加 5 条（期望值**怎么定**逐条写清）

**(1) 用户原话那例子 —— 中文档版**（最强的一条，期望值**不来自被测代码**）
```csharp
var relZh = CreatePool.MentionedCards(pool, poc, poc.DescZh, out whyZh, 8);
CheckTrue(relZh 里有 Storm Guardian, "★ 中文档下 `Path of Command` 的相关卡里也有 `Storm Guardian`");
```
期望值来源：**两条都能拿 `cards_engine.json` 手工核** —— `Path of Command` 的 `descZh` 逐字是「临时。部署 1 个风暴守护者」，而 `Storm Guardian` 的 `nameZh` 逐字是「风暴守护者」。**不是**从 `MentionedCards` 算出来的。

**(2) 顺序合并（灭自证用；结构上不可能「两边一起改回去」还绿）**
```csharp
var zhB = new CardDef("FxZhB", "Beta",  "unit","x","common","Test",1,1,1,0,null, nameZh:"贝塔",   subtype:"Infantry");
var zhA = new CardDef("FxZhA", "Alpha", "unit","x","common","Test",1,1,1,0,null, nameZh:"阿尔法", subtype:"Infantry");
var src = new CardDef("FxZhSrc","FxZhSrc","unit","贝塔 then Alpha","common","Test",1,1,1,0,null, nameZh:"源", subtype:"Infantry");
var rel = CreatePool.MentionedCards(new List<CardDef>{zhA, zhB, src}, src, src.Desc, out why, 8);
CheckTrue(rel.Count == 2 && rel[0].Name == "Beta" && rel[1].Name == "Alpha",
          "★ 按【出现顺序】合并：文本里「贝塔」在前 ⇒ 它必须排第一（把两趟简单拼起来会变成 Alpha 在前）");
```
期望值来源：**文本字面**（「贝塔」就是先出现的那个）⇒ 独立。这条专挡「把中文那趟的结果直接追加到英文结果后面」那种实现。

**(3) 长名优先（规则① 的中文版）**
```csharp
var grot   = new CardDef("FxGrot",  "Grot",           "unit","x","common","Test",1,1,1,0,null, nameZh:"地精",     subtype:"Infantry");
var sbGrot = new CardDef("FxSbGrot","Snakebite Grot", "unit","x","common","Test",1,1,1,0,null, nameZh:"蛇咬地精", subtype:"Infantry");
var src2   = new CardDef("FxZhSrc2","FxZhSrc2","unit","部署一个蛇咬地精","common","Test",1,1,1,0,null, nameZh:"源2", subtype:"Infantry");
var rel2 = CreatePool.MentionedCards(new List<CardDef>{grot, sbGrot, src2}, src2, src2.Desc, out why, 8);
CheckTrue(rel2.Count == 1 && rel2[0].Name == "Snakebite Grot",
          "★ 中文也守规则①：卡面写的是「蛇咬地精」⇒ 命中 `Snakebite Grot`，**不许**被更短的「地精」抢走");
```
期望值来源：**文本字面**（写的是「蛇咬地精」不是裸「地精」）⇒ 独立。

**(4) 钉住「为什么不能用 `Norm`」这条事实**（任何人改回去会立刻红）
```csharp
CheckTrue(CreatePool.Norm("幽卫") == "", "★ `Norm` 把汉字全剥掉（全池 1126 张 `Norm(nameZh)` 都是空串）⇒ 中文索引必须另用 `NormCjk`");
```
期望值来源：`Norm` 的**纯函数定义**（`CreatePool.cs:510-520` 只留 `a-z0-9`）⇒ 独立、可手工推。

**(5) 回归钉（沿用既有 ⑤ 的形状）**
```csharp
// 英文档：一个数都不许动 —— 这 2 条现有 ⑤ 已经有，别改
Check(mentions, 128, "…（英文档，本次改动不许动它）");
Check(distinct.Count, 67, "…（英文档）");
// 🆕 中文档新钉 3 个数（⚠️ 与本文件既有那 2 条同一性质：钉的是【本轮实测数】）
Check(mentionsZh, 132, "★ 中文档（喂 `DescZh`）全池总处数 —— 2026-10-18 用独立 harness 实测");
Check(distinctZh.Count, 67, "★ 中文档被点到的卡数");
Check(reanimateViaZh, 12, "★ 中文档 `复生`(Reanimate) 被点名 12 次（英文是 8 —— 差在「关键词与卡名同词」4 处）");
```
⚠️ **诚实标注**：`132 / 67 / 12` 是**本轮跑出来的实测数**（§③·E），**不是**从别的判据推出来的 —— 与既有 `128 / 67` 同一性质（那条的注释原话就是「钉**我们实测**的这个数」）。**别**把它们写成「由 `MentionedCards` 现算」的形式（那是自证）。

**(6) 可选 · 结构不变量**（挡「`Norm` 版索引」那种静默空表）
```csharp
foreach (var c in pool) if (!string.IsNullOrEmpty(c.NameZh))
    CheckTrue(CreatePool.MatchZhNameForTest(c.NameZh) == c.Name, "★ 每张有中文名的卡，用它的中文名都能查回自己（键非空 ⇒ 索引不是空表）");
```
需要 `CreatePool` 出一个 `internal`/`public` 的测试用查名字口（现在**没有** —— `_nameIndexCjk` 是私有的）⇒ 这条要**动 `CreatePool` 的可见性**，**我没做**，留作建议。

---

## 摘要（≤300 字）

「相关卡」中文索引做完。**简报第 1 条的前提被现读推翻**：`Norm` 只留 `a-z0-9`、**汉字全剥**，实测 1126/1126 张卡的 `Norm(nameZh)` **都是空串** ⇒ 照字面建 `Norm(NameZh)` 索引是**彻底的空操作**（键全空、静默查不到）。故另造 `NormCjk`（留汉字）建**第二张索引**，并在 `MentionedCards` 里加「中文那一趟」，与英文那趟**按位置合并**；英文那趟**匹配逻辑一字未改**。`RelatedCards.cs:52` 改成按语档取 `Desc`/`DescZh`（照 `FaceTextFull`）。**`phrases`（池子那支）故意没改**：`PoolFromPhrase` 是英文句法解析器，喂中文会让该支在中文档**整个消失**（真回归），且其产出与语言无关。量化：改前 128 处/67 张 → 改后中文档 **132/67**、英文档 **128/67 逐字不变**（用真编译代码跑出，10 个既有期望值全吻合）。类型检查 0 错，两文件纯 LF 未翻。未跑 Unity。

---

> ⚠️ **2026-10-10 后续订正（铁律 5）**：本文里凡指向 `RelatedCards.TextForLang` 的锚点（以及「相关卡印的字比卡面少一段关键词」这个说法）**已被 `A1405` 取代** —— `TextForLang` **已被删除**、`RelatedCards.Find` 现在直接走 `BattleDriver.FaceTextFull(card).body`；而「两扇窗印的字少一段」**这个前提本来就不成立**（它们一直走 `FaceTextFull`）。判据与读数 → `资料/普查产出_第十四会话/W_A1405相关卡文案.md`。
