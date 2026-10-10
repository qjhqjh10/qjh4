# W · `A1400` —— 卡面徽记链【同类第 9 个词】`Invulnerable`（`traits/invulnerable.png`）

> 执行代理产出（2026-10-10）。改动文件 = **`工具/gen_icon_plan.py`** ·
> **`数据/游戏数据/card_icon_plan.json`** · **`MyGame/Assets/CardPresentation/Resources/card_icon_plan.json`** ·
> **`资料/卡面图标_对照与缺口.md`**（后三份是**生成物**）。
> 落点 = 照 `A1366`（`W_icon链两笔.md`）/ `A1376`（`W_图标族8词.md`）：**只补表、判据一字未动**。
> ⛔ 没跑 Unity（含 `IconSetup.Verify`）· 没动 git · 没碰 `CardPresentation/**/*.cs` · 没碰 `RuleEngine/**` ·
> 没碰 `工具/ruleprobe/**` · **没改** `CLAUDE.md` / `项目任务.md` / `资料/卡面图标_现状与缺口.md`（都不在白名单，只报）。

---

## 零、一句话结论

✅ **做完，四条验收全过**：**28 条**新条目（14 张卡 × `desc`/`descZh`，**一条不少**、**每张都有判据出处**）·
**误伤面两层都是 0** · 生成器报告与改动前**只差 2 行**（全是预期）· **幂等 + 行尾没翻**。
`A1366` 修好的那道 `continue` 判据**对这 14 张确实生效**（实测：28 条里 **10 条**落在改前**整份字段一条记号都没有**的卡上）。

---

## 一、逐卡清单（14 张 × 2 字段 = 28 条，**每条都带出处**）

`token` 一律是**卡面真印着的那个词**（英文 `Invulnerable` / 中文 `无敌`），sprite 一律 `invulnerable`，
运行时走 `CardIcons.Rewrite` 的**裸关键词那一支**（插在**词前**、词留着、外面包 `<link>` ＋ `<nobr>`）。

| 卡 id | 卡名（我们数据） | 字段 | token | sprite | 判据出处（逐张抄录行号 / 卡图文件名） |
|---|---|---|---|---|---|
| `ASH21` | Nuadhu Fireheart | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:32`（`Aeldari/3部队/Warpforge_21_Nuadhu-Fireheart.png`） |
| `AM36` | Renowned Bodyguard | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:147`（`Astra Militarum/2天赋/Warpforge_36_Renowned-Bodyguard.png`） |
| `BL60` | Daemonic Pact | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:248`（`Chaos/4计策/Warpforge_60_Daemonic-Pact.png`）**＋本代理亲读** |
| `DA_Rites_of_Penance` | Rites of Penance | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `DarkAngels__3.md:11` **＋本代理亲读** `Dark Angels/6秘密/IMG_3698.jpg`（**不在**还原表里） |
| `DA82` | Chaplain Gabutheron | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:280`（`Dark Angels/3部队/Warpforge_08_Chaplain-Gabutheron.png`） |
| `GSC47` | Perfect Ambush | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:470`（`Genestealer Cult/4计策/Warpforge_47_Perfect-Ambush.png`） |
| `GSC58` | Open Insurrection | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:481`（`Genestealer Cult/4计策/Warpforge_58_Open-Insurrection.png`） |
| `SOR38` | Saint Celestine | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:739`（`Sorotitas/3部队/Warpforge_38_Saint-Celestine.png`） |
| `SW18` | Fyrri Askar | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:784`（`Space Wolves/3部队/Warpforge_18_Fyrri-Askar.png`）**＋本代理亲读** |
| `SW29` | Arjac Rockfist | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:795`（`Space Wolves/3部队/Warpforge_29_Arjac-Rockfist.png`） |
| `TL61` | Apex Predator | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:982`（`Tyranid/4计策/Warpforge_61_Apex-Predator.png`） |
| `UM88` | Terminator Captain | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:1052`（`Ultramarines/3部队/Warpforge_24_Terminator-Captain.png`） |
| `UM52` | Humanity's Shield | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:1102`（`Ultramarines/4计策/Warpforge_52_Humanitys-Shield.png`） |
| `UM96` | Shall Know No Fear | desc / descZh | `Invulnerable` / `无敌` | `invulnerable` | `_还原效果文字.md:1092`（`Ultramarines/4计策/Warpforge_32_Shall-Know-No-Fear.png`）**＋本代理亲读** |

### 判据本体（铁律 7：卡面判据的最终真相 = PnP 成品卡图）

- **主判据 = `资料/卡表核对_卡图提取/_还原效果文字.md` 的【双写】** —— 卡面上「**徽记 + 紧跟那个词**」
  那张表写成 `Invulnerable Invulnerable`（`ASH21 :32` / `SW18 :784` / `SW29 :795`）或
  `InvulnerableInvulnerable`（其余 12 行）= **图标 + 词**。**凡出现该词的 15 行、15/15 全部双写**。
- **第 16 张 `DA_Rites_of_Penance` 不在那张表里**（它来自 `Dark Angels/6秘密/` 的**手机翻拍**，
  同 `DA_None_Must_Know`）⇒ **本代理亲读** `d:/2/Warpforge部队卡片/Dark Angels/6秘密/IMG_3698.jpg`：
  `Give +3〔红棕圆 + 白拳〕, +3〔紫圆 + 白枪〕 and 〔深蓝圆 + 白骷髅 + 带缺口横条〕Invulnerable
   to a friendly unit until your next turn`。
- **徽记与盘上图并排核过**（本代理亲读 5 张成品图）：
  `Chaos/4计策/Warpforge_60_Daemonic-Pact.png` · `Space Wolves/3部队/Warpforge_18_Fyrri-Askar.png` ·
  `Ultramarines/4计策/Warpforge_32_Shall-Know-No-Fear.png` · `Aeldari/3部队/Warpforge_44_Wraithknight.png` ·
  `Sorotitas/4计策/Warpforge_52_Miraculous-Feat.png`（后两张是**排除依据**，见 §二）
  ＋ `Dark Angels/6秘密/IMG_3698.jpg`。
  **形状 = 深/蓝紫圆 + 白色骷髅 + 一条带缺口的白色横条**，与
  `MyGame/Assets/CardPresentation/Resources/Art/traits/invulnerable.png` **同图**。
- ⇒ **恒带 16/16**：词在**句首**（`Give Invulnerable …`）· **系表语**（`Your Warlord becomes Invulnerable`）·
  句中（`Friendly units with Pack have Invulnerable during your turn`）**全带**，不分位置、不分词性。
- ⚠️ **中文那半（`无敌`）的判据只能推到「英文那枚在图前、位置一一对应」** —— 原版**没有中文表**
  （原版客户端**一个中文串都没有**），那份中文是我们自己译的 ⇒ **中文卡面没有可核的原版成品图**。
  14 张的 `descZh` 写法**完全一致**（都写 `无敌`、没有第二种译法），逐张核过。

### 落点（`工具/gen_icon_plan.py`）

| 文件 | 落点 | 改了什么 |
|---|---|---|
| `d:/4/Unity/工具/gen_icon_plan.py` | `:799-911`（**新增 113 行**；紧接 `_FAMILY8` 那个循环之后、`GAP_BY_CARD` 之前） | 新增 `_INVULNERABLE`（14 行逐卡表：卡 id / 判据出处 / 卡图文件名）＋ 一次生成 EN/ZH 两半的循环（`BARE_TOKEN_BY_CARD[(cid,"Invulnerable")]` / `[(cid,"无敌")]` → sprite `invulnerable`），整块带判据链注释 ①–⑦ |
| `d:/4/Unity/数据/游戏数据/card_icon_plan.json` | 生成物 | **+28 条 / 0 删除 / 0 修改** |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_icon_plan.json` | 生成物 | 同上；**两份 `cards` 数组逐字节相同**（`note` 那一行本来就不同，是设计） |
| `d:/4/Unity/资料/卡面图标_对照与缺口.md` | 生成物 | 逐卡明细 **2348 → 2376 行**；§一/§四 的计数行随之改（3 行） |

---

## 二、排除与冲突

### ① `ASH44` / `SOR52` 怎么排除的（**必须排除，否则画两枚**）

- 这两张的 `desc` 里那个词**已经被画过了** —— 走的是**方括号记号**那条路
  （`gen_icon_plan.py:128-129` 的 `("ASH44","[Invulnerable]")` / `("SOR52","[Invulnerable]")`
  → sprite `invulnerable`，`descZh` 侧是 `[无敌]`）。
- 它们的原始 `desc` 是 **`[Invulnerable] Invulnerable` 这种【双写】**：
  - `ASH44` = `3 [Spirit Stone]: Gains [Invulnerable] Invulnerable until your next turn`
  - `SOR52` = `Give [Invulnerable] Invulnerable to a friendly unit this turn. 6 [Energy]: Extend effect until your next turn`
- **方括号那一支是「整串换掉」**（词被吃掉）⇒ 那一枚图标**已经出现**；再补一条**裸 token**
  会在紧跟的那个词前**再插一枚 = 卡面画两枚**（静默、只在这两张上现形）。
- **亲读两张成品图各只有一枚**：
  `Aeldari/3部队/Warpforge_44_Wraithknight.png` = `〔圆形:三叶绿〕Shuriken 8. 〔绿圈3〕. Gains〔骷髅徽〕Invulnerable`；
  `Sorotitas/4计策/Warpforge_52_Miraculous-Feat.png` = `Give〔骷髅徽〕Invulnerable to a friendly unit this turn`。
- ⇒ `_INVULNERABLE` 表里**不列**这两张；**16 张 − 2 = 14 张**，× 2 字段 = **28 条**（与 `A1400` 台账一致）。

### ② 子串冲突：**按 `CardIcons.Rewrite` 的语义枚举了一遍，本批这一侧为 0**

判据 = `CardPresentation/Core/CardIcons.cs:175-329`（`Rewrite`）。关键语义三条：
条目按 **token 长度降序**、同长按 `CompareOrdinal` · 裸关键词那一支是
**`s = s.Replace(it.token, bare)`（纯子串替换、`Ordinal` 区分大小写）** · 幂等守卫是
`bare` 已在结果里就跳过。

**(a) 正向（`Invulnerable` 会不会被别的词吃掉）—— 全池扫过，0：**

| 检查 | 结果 |
|---|---|
| 有更长的英文词含它吗（`[A-Za-z]*invulnerab[A-Za-z]*`，大小写不敏感） | **0 处** —— 全池**只有 `Invulnerable` 这一种写法** |
| `keywords` 数组里有它吗 | **0 处** |
| 有更长的中文词含 `无敌` 吗 | 中文没有词分隔符，**改成查「同字段里有没有别的 token 含 `无敌`」⇒ 0 处** |

**(b) 反向（`Invulnerable` 会不会吃掉别人）—— 全池枚举「同字段内一个 token 是另一个的子串」，0 处：**

14 张目标卡的 `desc`/`descZh` 逐字段列过（`ASH21` 的 `Strike:` · `BL60` 的 `Vulnerable 2` ·
`DA82` 的 `Armour 2.`/`Teleport:`/`Agenda:`/`[Attack]` · `DA_Rites_of_Penance` 的 `[Melee]`/`[Ranged]` ·
`SOR38` 的 `Rally:` · `SW29` 的 `Hunt Mark`/`Rally:` · `TL61` 的 `[Ranged]` ·
`UM88` 的 `Armour 2.`/`Oath 4:` · `UM96` 的 `Oath 8:`/`[Attack]`/`[Ranged]`；中文侧同理）
—— **没有一组互为子串**；**全池**（717 张）跑同一个检查也是 **0 处**。
`Invulnerable`(12 字) 在 14 张目标卡的字段里**都是最长或并列最长的**，**总是先被换**，
它换出来的 `bare` 里只有**小写** `invulnerable`（图名 + link id），**不含任何大写开头的词**
⇒ 后续短 token 匹配不上它插进去的东西。

**(c) 🔴 与 `A1376` 的 `BL60 Vulnerable 2` 特例安全共存（实测，不是推断）：**

- `Invulnerable`(12) 与 `Vulnerable 2`(12) **等长** ⇒ 落到 `CompareOrdinal`：
  `"Invulnerable"` < `"Vulnerable 2"`（`I`=0x49 < `V`=0x56）⇒ **`Invulnerable` 先换**。
- `Rewrite` 是 `Ordinal`（**区分大小写**）—— `Invulnerable` 里那半截是**小写** `vulnerable`，
  大写 `Vulnerable` **匹配不上** ⇒ 轮到 `Vulnerable 2` 时**只命中剩下那一处**。
- 实测渲染层全池逐字比对：`BL60` 那一处**只动该动的两处**、无误伤（见 §三 ②）。
  改动后 `BL60 desc` =
  `Give <link=invulnerable><nobr><sprite name="invulnerable">Invulnerable</nobr></link> to your Warlord
   until your next turn and give <link=vulnerable><nobr><sprite name="vulnerable">Vulnerable 2</nobr></link>
   to all enemy troops`，
  亲读卡图 `Chaos/4计策/Warpforge_60_Daemonic-Pact.png` = `Give〔骷髅徽〕Invulnerable … and give〔盾+闪电徽〕Vulnerable 2 …` —— **两枚都在、都对位**。
- ⚠️ **顺带更正 `A1376` ④ 那句话的一个细节**（**结论不改**）：它写「token 写 `Vulnerable` 会把
  `Invulnerable` 里那半截一起换掉」—— 按 `Rewrite` 的语义**那不可能**（`Ordinal` 区分大小写）。
  **它选 `Vulnerable 2` 仍然是对的**（该卡内唯一、数字本来就在同一行），**本批没改它**
  （改了要重走一遍验证、收益为 0）。更正痕写进了 `gen_icon_plan.py` 的新块 ④。

### ③ `invulnerable` 与 `vulnerable` 是**两个不同的 sprite**（⛔ 没复用）

| sprite | 盘上文件 | 形状 | 本次用在哪 |
|---|---|---|---|
| `invulnerable` | `Art/traits/invulnerable.png`（8341 B） | **深蓝圆 + 白骷髅 + 带缺口横条** | **本批**（14 张） |
| `vulnerable` | `Art/traits/vulnerable.png`（7727 B） | 深蓝圆 + 白盾 + 蓝闪电 | `A1376`（`Vulnerable` 那批） |

两者各有自己的图集切片：`Warpforge Trait TextSprites.asset` 里 `m_Name: Atlas_trait_icon_invulnerable`
（与 `…_vulnerable` 分别是两条）。

---

## 三、读数

### ① 生成器报告（`python 工具/gen_icon_plan.py`，只报告不落盘）—— 与改动前逐行比**只差 2 行**

基线在**改脚本之前**就跑过一次（`_tmp_a1400/report_before.txt`），同一个 `cards_engine.json`
（md5 `7699a5a0172b6906c77fe3c642b11c51`，`--write` 前后各核一次、**没变**）。

```
  按卡裸 token:  209  ->  237     （+28，恰好 = 新条目数）
  有记号的卡:    712  ->  717     （+5 = 改前**整张卡都不在计划表里**的那 5 张：AM36/GSC47/GSC58/SW18/UM52）
  🔴 sprite 名盘上没有: 无         （不变 ——`IconSetup.Verify` 查的那条不变式，这里是静态等价）
  没认出来的: 0 处 · 表里已定而锚点给另一答案: 0 处 · 锚点自动对齐: 1 处（SOR42，**不变**）
  逐卡表死条目: 26 条              （**未变** —— 28 条新条目**一条都不是死的**）
```

### ② 误伤面 = 0（**两层都量了**）

**数据层**（现算 `cards_engine.json` 1126 张）：

- `Invulnerable` 命中 **16 张 / 18 处**（`ASH44`/`SOR52` 各 2 处，其余 14 张各 1 处）；
  中文 `无敌` 命中**完全相同的 16 张 / 18 处**（两个集合**逐张相同**）。
- 没有更长的词含它（**0 处**）· 更长的中文词含 `无敌`（按 token 集合查，**0 处**）·
  `keywords` 里 **0 处** · `[Invulnerable]`/`[无敌]` 方括号**只有 `ASH44`/`SOR52` 各 2 条**（已排除）。
- **同字段内 token 互为子串：全池 0 处。**

**渲染层**：按 `CardPresentation/Core/CardIcons.cs:175-329` 的 `Rewrite` **逐分支静态复刻**
（含 `Badges.KeyOf` 那一层），把「改动前 / 改动后」两份计划表对全池 **1126 张 × 2 字段**各铺一遍再逐字比：

```
改动前能铺出结果的 (卡,字段): 1413   改动后: 1423   （+10 = 那 5 张"改前整卡不在表里"的 desc+descZh）
有差异的 (卡,字段): 28
  其中【含新 sprite(`invulnerable`)】的: 28
  🔴 不含新 sprite 的（= 误伤）: 0
差异涉及卡数: 14   （= 新增条目涉及的 14 张，恰好一致）
desc 差异: 14   descZh 差异: 14
每处差异里新 sprite 出现次数分布: {1 次: 28}      （**没有一处插了两枚**）
```

⚠️ 这是**静态复刻**（不模拟 `<link>` 那一层的运行时 `Badges.KeyOf` 取值），**不是 Unity 实跑**。

**样例（改动后，英文 / 中文各一）**：

```
ASH21 desc   → <link=strike><nobr><sprite name="strike">Strike:</nobr></link> Give <link=invulnerable><nobr><sprite name="invulnerable">Invulnerable</nobr></link> to adjacent troops this turn
ASH21 descZh → <link=strike><nobr><sprite name="strike">猛击：</nobr></link>本回合给予相邻部队<link=invulnerable><nobr><sprite name="invulnerable">无敌</nobr></link>
UM52  desc   → Your Warlord becomes <link=invulnerable><nobr><sprite name="invulnerable">Invulnerable</nobr></link> until your next turn
UM52  descZh → 你的战将变为<link=invulnerable><nobr><sprite name="invulnerable">无敌</nobr></link>，直到你的下个回合。
SW18  desc   → Friendly units with Pack have <link=invulnerable><nobr><sprite name="invulnerable">Invulnerable</nobr></link> during your turn
DA_Rites_of_Penance desc → Give +3 <link=melee><sprite name="Melee"></link>, +3 <link=ranged><sprite name="Ranged"></link> and <link=invulnerable><nobr><sprite name="invulnerable">Invulnerable</nobr></link> to a friendly unit until your next turn
BL60  desc   → （见 §二 ③）
```

**幂等（渲染层）**：全池 1126 张 × 2 字段，`Rewrite` **应用两遍 == 一遍**，
**改前 / 改后都是 0 个 (卡,字段) 不同**（这一条必须有 —— `SetData` 会把同一份数据再喂卡面一次）。

### ③ 严格对账（改动前 vs 改动后两份 json）

- 条目 **2348 → 2376**（**+28**）；按 **(卡,字段,token) 去重后 2331 → 2359**。
- **消失 0 条 · 被改 0 条** ⇒ 原有条目的 `why` **一字未动**。
- 新增 28 条 **token 全是 `Invulnerable`(14) / `无敌`(14)、sprite 全是 `invulnerable`**、涉 **14 张卡**。
- 🔴 **`A1366` 修好的那道 `continue` 判据对这 14 张确实生效（实测、不是假设）**：
  28 条里有 **10 条**落在「**改动前整份字段一条记号都没有**」（= 旧判据会 `continue` 跳过）的
  **10 个 (卡,字段) / 5 张卡**（`AM36` `GSC47` `GSC58` `SW18` `UM52` 的 `desc`+`descZh`）里 ——
  若那道判据没修，这 10 条**一条都不会出现**（静默）。另 18 个 (卡,字段) 改动前已在表里、只是条目变多。
- 两份 json **键无重复**（`object_pairs_hook` 全文件验过）· **`cards` 数组逐字节相同**
  （`md5` = `bda4052194f0ba3e4131719836a38863`，只有 `note` 那一行本来就不同）。
- **每一条新增的 `why` 里都带逐张出处**（`_还原效果文字.md:NN` 或 `DarkAngels__3.md:11`＋亲读 `.jpg` ＋卡图文件名）。

### ④ 幂等 / 行尾 / 增删量

- **幂等**：`gen_icon_plan.py --write` + `gen_icon_doc.py` **连跑两遍**，三份产物 **md5 逐字节相同**：

  | 产物 | md5 |
  |---|---|
  | `数据/游戏数据/card_icon_plan.json` | `60250a39d5fb0a4bfd4de4c755225981` |
  | `Resources/…/card_icon_plan.json` | `c85b0784b0e300ef1c2c2bd056eef4a2` |
  | `资料/卡面图标_对照与缺口.md` | `a6a0d333ad9c267f83439fb20fb6fc5c` |

- **行尾**（**二进制**读出来数的，不是 `file` 的显示）：五个文件**全是纯 LF、0 个 CRLF** ——
  `gen_icon_plan.py` 0/1502 · 两份 json 各 0/22585 · `.md` 0/2481 · `gen_icon_doc.py` 0/285（**未改动**）。
  改 `.py` 用的是 Edit 工具（**没上 `sed -i`**）。
- **本批增删**（vs 本批开始前的工作副本）：`gen_icon_plan.py` **+114 / −0** ·
  两份 json 各 **+28 条**（行数各自 +245）· `.md` **+34 / −3**（3 行是 §一/§四 的计数行）。
- **`cards_engine.json` 全程没变**：`--write` 前后各核一次 md5 = `7699a5a0…`（另一个写手在动它，
  所以特意核了两遍；**若它之后再变，计划表要在同步点重跑一次**）。

---

## 四、我没做的 / 判不了的（如实）

1. 🔴 **没有 Unity 实跑读数（本批红线）。** `IconSetup.Verify`（自检里那条「计划表里的 sprite 名
   资产里都查得到」）**没跑** ⇒ 「`invulnerable` 这个 sprite 名在 TMP sprite asset 里有字形」
   这一条**只有静态证据**：① 生成器报 `🔴 sprite 名盘上没有: 无`；
   ② 盘上 `Art/traits/invulnerable.png` 在（8341 B）；③ `Warpforge Trait TextSprites.asset` 里
   有 `m_Name: Atlas_trait_icon_invulnerable`（本代理 grep 过）。
   **要实跑请调度台安排。**
2. ⚠️ **中文那半的判据只能推到「英文那枚在图前、位置一一对应」** —— 原版**没有中文表**，
   中文卡面**没有可核的原版成品图**。每条中文条目里都如实写了这句。
3. ⚠️ **逐卡枚举会过期**（`A1366`/`A1376` 同款缝，本笔不碰）：新卡若含 `Invulnerable`，
   要回 `_INVULNERABLE` 补一行；生成器只报**死条目**、**报不出「漏了新卡」**。那整类缝 = **`A1370`**。
4. ⚠️ **`A1378`（该不该改成「按词规则」）本笔没改** —— 按简报照 `A1366`/`A1376` 的落点做（逐卡枚举）。
   给它的输入：`Invulnerable` **恒带 16/16**（含排除的那 2 张）⇒ 是可以改成按词的那一类。
5. ⚠️ **`DA_Rites_of_Penance` 这一张的判据降级了**：它**没有 900×1200 的 PnP 成品图**，
   只有 `Dark Angels/6秘密/IMG_3698.jpg`（**手机翻拍**，且该文件自带一条 `x1` 小字与四角 `+` 形标记
   = 游戏内详情视图的照片，版心比 PnP 窄）。判据等级低于其余 13 张，如实标在 `why` 里。
6. ⚠️ **`资料/卡面图标_现状与缺口.md:85` 那句仍是被证伪的**（`Choose a Sabotage card` 那一半，
   `A1376` 已就地更正 `gen_icon_plan.py` 里同一句、`.md` 那份不在白名单只报）—— **本批照旧没改**。
7. ⚠️ **`资料/普查产出_第十四会话/W_图标族8词.md` 里的两处本文更正（§二 ③ 的"过度保守"那句、
   §四 1 的"15/15"口径）本笔没回改那份报告**（**不是我的文件**），只写在本报告与 `gen_icon_plan.py` 里。
   两份说法**不矛盾**（那边是结论、这边是复核），但**下一个会话若只看那份**会以为 `Vulnerable` 那条
   取法是被迫的 —— 留调度台裁。

---

## 五、顺手发现（⛔ 只报不改，一条一句话 + 出处）

1. 🔴 **`CardIcons.cs` 头注释那组数字在我改之前就已经是旧的**：它写「计划表 **2121 处 / 654 张**；
   **628 方括号 / 1321 裸关键词 / 689 整串换掉 / 111 数字烘在图里**」，而**改前实测**计划表 =
   **712 张 / 2348 处**（`661 方括号 / 1590 裸关键词 / 97 符号`），**改后** 717 / 2376（`661 / 1618 / 97`）。
   出处 → `Unity/MyGame/Assets/CardPresentation/Core/CardIcons.cs:155-163` vs
   `资料/卡面图标_对照与缺口.md:9` 与 `:27`。⛔ 那个文件不在本批白名单（**有别的写手正在动它**）。
2. ⚠️ **`DarkAngels__3.md:19-20` 把两枚图标记成了形状名**（`【图标:骷髅】` / `【图标:枪】`）——
   它自己写了「按形状记的、不猜名字」。**本批亲读确认**前者就是 `invulnerable`（深蓝圆 + 白骷髅 +
   带缺口横条）⇒ **那不是缺口**，只是当时手上没有名字表。出处 → `资料/卡表核对_卡图提取/DarkAngels__3.md:19-20`
   ＋ `Dark Angels/6秘密/IMG_3698.jpg`。
3. ⚠️ **`A1376` 报告 §四 1 写「`Invulnerable` 恒带 15/15」** —— 本批复核实为 **16/16**（16 张卡各 1 处裸词）。
   `15` 是**还原表覆盖数**、不是**全池张数**，两个口径都对，但两个数放在一起容易被读成矛盾。
   出处 → `资料/普查产出_第十四会话/W_图标族8词.md:80,167` vs 本报告 §一。
4. ℹ️ **`A1376` ④ 那句「写 `Vulnerable` 会把 `Invulnerable` 里那半截一起换掉」** = **过度保守**
   （`Rewrite` 的 `Replace` 是 `Ordinal`、区分大小写）。结论（取 `Vulnerable 2`）不受影响。
   出处 → `资料/普查产出_第十四会话/W_图标族8词.md:50-52` ＋ `Core/CardIcons.cs:323`。
5. ℹ️ **`Dark Angels/6秘密/` 那 5 张 `.jpg` 是一族「没有 PnP 成品图」的卡**
   （`Conv 3 the Circle` / `None Must Know` / `Obscure Ritual` / `Rites of Penance` / `Smothering Decree`）——
   凡「拿成品卡图当尺子」的活，这一族**只能降级取翻拍**（`A1366` 已记过 `DA_None_Must_Know` 一例）。
   出处 → `ls d:/2/Warpforge部队卡片/Dark Angels/6秘密/`。
6. ℹ️ **本批临时脚本留在 `d:/4/_tmp_a1400/`**（`sim_rewrite.py` = `Rewrite` 的逐分支静态复刻、
   `cmp_plan.py` = 计划表严格对账、`conflicts.py` = 子串冲突枚举、`report_before/after.txt` = 基线）。
   下一次再做这类词可以**直接复用** `sim_rewrite.py`（用法：`python -I sim_rewrite.py <plan.json> <out.json>`）。
   ⛔ 它不是工程产物、不在任何正本里，调度台要清就清。
