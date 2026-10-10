# W · icon 链两笔（`A1366` + `A1371`）—— 第十四会话

> 执行代理产出。改动文件 = `工具/gen_icon_plan.py` · `工具/gen_icon_doc.py` ·
> `数据/游戏数据/card_icon_plan.json` · `MyGame/Assets/CardPresentation/Resources/card_icon_plan.json` ·
> `资料/卡面图标_对照与缺口.md`（生成物）。
> ⛔ 没跑 Unity（含 `IconSetup.Verify`）· 没动 git · 没碰 `CardIcons.cs` / `IconSetup.cs` / `RuleEngine/**`。
> 顺序执行：先 `A1366`（跑完 `gen_icon_plan.py --write`）**再** `A1371`，最后才铺一次文档。

---

## A1366 — `Stun` 那枚金螺旋（`traits/stun.png`）全池一条都没画

### 结论

✅ **做完，判据满足**。落下 60 条逐卡条目（**英文 30 张 + 中文 30 张**，同一批卡），
计划表里 `stun` 由 **0 条 → 60 条**（全池 30 张含该词的卡，每张 `desc`+`descZh` 各一条）。

🔴 **但判据链上原来缺了关键一环，不补它这 60 条里只有 30 条会生效**：`main()` 里那道
`if not toks and not syms and not kws and not res: continue` 会把**整份字段**跳过，
而裸 token 表是在它**之后**才查的 ⇒ 凡是「这份字段里只有那个裸词、别的记号一个都没有」的卡
（`DA39` `desc` = `Stun an enemy`、`DA_None_Must_Know` 同理）**整份字段根本走不到裸 token 那一支**。
实测：只加表不改这道判据 ⇒ **30 张里 15 张一条都补不上**（`AM47`/`ASH48`/`ASH51`/`ASH82`/`BL75`/
`DA39`/`DA_None_Must_Know`/`GOF40`/`GOF62`/`GOF_Stikkbomb_Boy`/`GSC78`/`TAU57`/`TL26`/`TL84`/`UM32`）。
⇒ 已把「本字段有没有裸 token 命中」并进那道判据（它本来就是「这份字段一条记号都没有」）。

### 改动清单

| 文件 | 落点（现行号） | 改了什么 |
|---|---|---|
| `d:/4/Unity/工具/gen_icon_plan.py` | `:460-535`（新增） | 新增 `_STUN_CARDS` 表（30 行，每行 = 卡 id + 判据出处 + 卡图文件名）＋ 一次生成 EN/ZH 两半的循环（`BARE_TOKEN_BY_CARD[(_cid,"Stun")]` / `[(_cid,"眩晕")]` → sprite `stun`），整块带判据链注释（①–⑥） |
| 同上 | `:644-648`（改注释） | 🔴 **铁律 5 就地更正**：`KEYWORD_PREFIX` 那段注释原写「`Stun an enemy` … **都不带图标**」—— 被 30 张卡图证伪，改成「`Stun` **恒带**」并留下更正痕；另两个原例（`Choose a Sabotage card` / 写成词的 `Ranged Attack`）如实标「仍待核」 |
| 同上 | `:979-990`（改逻辑，仅此一处行为改动） | 把裸 token 命中算到那道 `continue` **之前**（`btok`），并并进它的判据；原循环改成遍历 `btok` |
| 同上 | 文件头「产物」节 `:22-34` ＋ `OUT_MD` 那行 `:51-54`（纯注释） | 顺手更正两条错记录：① 那份 `.md` 是 `gen_icon_doc.py` 写的、本脚本只写两份 json（原来写成自己的产物）② 运行时那份 json 原来漏列 |
| `d:/4/Unity/数据/游戏数据/card_icon_plan.json` | 生成物 | `cards` **+525 行**（60 条新条目 + 所在字段）；**无删除** |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/card_icon_plan.json` | 生成物 | 同上（**两份一起写**，`cards` 数组逐字节相同） |
| `d:/4/Unity/资料/卡面图标_对照与缺口.md` | 生成物 | 逐卡明细 **2140 → 2200 行**；§一/§三 的计数行随之改 |

### 证据（都能复核）

**① 判据本体 —— 30 张一张不缺，每张都有出处**（铁律 7）

- **29 张** = `资料/卡表核对_卡图提取/_还原效果文字.md`（29 个子代理逐张看卡图抄的还原表）里
  该卡那一行的 **`Stun` 双写**（`Stun Stun` / `StunStun` = 图标 + 词），行号逐条写进了 `_STUN_CARDS`：
  `:11 ASH74` · `:12 ASH75` · `:19 ASH82` · `:31 ASH20` · `:71 ASH48` · `:74 ASH51` · `:104 AM_Hektor_Thenmann`
  · `:110 AM69` · `:161 AM47` · `:226 BL75` · `:316 DA39` · `:359 EC17` · `:437 GSC17` · `:448 GSC28`
  · `:455 GSC35` · `:460 GSC78` · `:516 SAU23` · `:581 GOF_Stikkbomb_Boy` · `:585 GOF77` · `:640 GOF96`
  · `:660 GOF39` · `:667 GOF40` · `:689 GOF62` · `:760 SOR59` · `:902 TAU57` · `:935 TL26` · `:961 TL84`
  · `:1016 UM71` · `:1058 UM32`。
- **第 30 张 `DA_None_Must_Know`** 不在那张表里（它来自 `6秘密/` 的**手机翻拍**）⇒ **本代理亲读**
  `d:/2/Warpforge部队卡片/Dark Angels/6秘密/IMG_3696.jpg`：
  卡面 = `Destroy an enemy troop.〔金棕圆底 + 白色同心螺旋〕Stun adjacent units`
  —— 那枚徽记与 `Resources/Art/traits/stun.png`（金棕圆底 + 白色同心螺旋）**同图**；
  旁证 = `资料/卡表核对_卡图提取/DarkAngels__3.md:18` 记它「金/棕圈内的白色螺旋纹（旋涡）」。
- ⇒ 位置不一而论：句首（`SOR59`）/ 句中动词（`AM47`）/ 句中**名词**（`ASH74`）/ 句尾且后面不跟冒号
  （`GOF77`）**全带**。

**② 误伤面 = 0（两层都量了）**

- **数据层**（现算 `cards_engine.json` 1126 张）：`\bStun\b` 命中 **30 张**（`desc`）· `眩晕` 命中
  **30 张**（`descZh`），**两个集合完全相同**；**每题恰好 1 处**（共 30 处）；大小写不敏感扫 `stun`
  也是这 30 张 30 处（**没有** `Stunned`/`stuns` 等更长的词）；`keywords` 里 **0 处**；`[Stun]`
  方括号记号 **0 处**。
- **渲染层**：按 `CardPresentation/Core/CardIcons.cs:175-331` **逐分支静态复刻** `Rewrite`，
  拿「改动前 / 改动后」两份计划表把全池 **1126 张 × 2 字段**各铺一遍再逐字比 ⇒
  **有差异的只有 60 个 (卡, 字段)，全部含 `stun`，涉及的卡恰好就是那 30 张**；其余一字未动。
  ⚠️ 这是**静态复刻**（不模拟 `<link>` 那层 `Badges.KeyOf`），**不是 Unity 实跑**。

**③ 输出形状 + 幂等**（同上静态复刻）：60/60 个 (卡, 字段) 都插在**词前、词留着**、
处数与原文出现次数一致；**跑两遍 = 跑一遍**（`CardIcons` 每份字段会换两遍，这条必须有）。
样例（`DA39` / `ASH74`(名词) / `SOR59`(句首) / `GOF77`(句尾) / `GOF96` / `UM71`）：

```
DA39  desc   → <nobr><sprite name="stun">Stun</nobr> an enemy
ASH74 desc   → When an enemy receives a <nobr><sprite name="stun">Stun</nobr>, deal 1 damage to it. …
GOF96 desc   → <nobr><sprite name="stomp">Stomp.</nobr> <nobr><sprite name="rally">Rally:</nobr> <nobr><sprite name="stun">Stun</nobr> an enemy and adjacent units. …
UM71  descZh → <nobr><sprite name="rally">集结：</nobr>造成 1 点伤害。<nobr><sprite name="oath">誓言 2：</nobr>使 1 个敌人<nobr><sprite name="stun">眩晕</nobr>。
```

**④ 生成器报告（`python 工具/gen_icon_plan.py`，只报告不落盘）—— 与改动前逐行比，只差两行：**

```
  按卡裸 token: 6  →  66
  有记号的卡:   655 →  670
  🔴 sprite 名盘上没有: 无          （= `IconSetup.Verify` 查的那条不变式，静态等价）
  没认出来的: 0 处 · 表里已定而锚点给另一答案: 0 处 · 逐卡表死条目: 26 条（**未变**）
```
⇒ 新条目**没有一条是死的**，也没有新增任何「认不出 / 缺图」的告警。

**⑤ 严格对账**（改动前 vs 改动后两份 json）：条目 2140 → 2200（+60）；**消失/被改的 = 0 条**；
新增 60 条 **token 全是 `Stun`|`眩晕`、sprite 全是 `stun`、涉 30 张卡**；**原有条目的 `why` 一字未改**；
两份 json **键无重复**、`cards` 数组逐字节相同、行尾都是纯 LF（0 CRLF / 20995 LF）。

**⑥ 幂等**：`gen_icon_plan.py --write` + `gen_icon_doc.py` 连跑两遍，三份产物 **md5 逐字节相同**
（`card_icon_plan.json` `51335ec9…` · Resources 那份 `8c806ef8…` · 对照文档 `6efbd209…`）。

### 还差什么

1. 🔴 **没有 Unity 实跑读数**（本批红线）。`IconSetup.Verify`（= 自检里那条「计划表里的 sprite 名
   资产里都查得到」）**没跑**；改判据（`btok` 并进 `continue`）对**别的**卡有没有副作用，
   只有上面 ④（报告逐行比对）+ ②（渲染层全池复刻比对）两条**静态**证据。**要实跑请调度台安排**。
2. ⚠️ **逐卡枚举会过期**：新卡若含 `Stun`，要回 `_STUN_CARDS` 补一行（生成器只会报**死条目**，
   报不出「漏了新卡」）。这一**整类**缝 = `A1370` 的对账，本批没碰。
3. ⚠️ `KEYWORD_PREFIX` 那段注释里另两个例子（`Choose a Sabotage card` / 写成词的 `Ranged Attack`）
   **仍是未核状态**（R7 只核了 `Stun`），注释里已如实标「仍待核」。
4. ⚠️ `A1366` 台账那句前提「`stun` 不在 `gen_icon_plan.py` 的 sprite 词表」**仍是错的**
   （词表 = `Art/traits/` 整个目录 78 个；「61 个」是**输出**口径）—— 本批**没改正本**
   （`项目任务.md` 不在白名单），留调度台处置。R7 已记同一件事（它那份顺手发现 1）。
5. ⚠️ **中文那半**的判据只能推到「英文那枚在图前、位置一一对应」—— 原版**没有中文表**
   （中文是我们自己译的），所以中文卡面**没有**可核的原版成品图。卡片条目里已如实写这句。

---

## A1371 — `资料/卡面图标_对照与缺口.md` §七「73 + 5」是硬编码

### 结论

✅ **做完，判据满足**。§七 那句「**78 张**（… **73 张** `Atlas_trait_icon_*` ＋ **5 张**
`Atlas_SpiritStone_*`）」的**后半句原来写死在源码里**，现在**三个数全是现数**，
并且**两个集合对不上时会把差集打出来**（这正是那条判据担心的「打架」，原来**没有报错的口子**）。

> **`A1371` 的判据（一句话自己概括）**：`gen_icon_doc.py` 生成 §七 时，
> 「`traits/` 有多少张」是算的、而括号里「73 张图集切片 + 5 张灵魂石切片」是**硬编码**的 ——
> 今天凑巧都等于 78，但只要 `traits/` 里多一张不是图集切片的图（或图集多一张我们没导的）
> 两边就打架且**无声**；⇒ 要把后半句也改成**现数**。
> ⚠️ 另：台账里「`stun.png` 是不是那 78 张切片之一 —— `W8` 没查」**本批查了：是**
> （`Warpforge Trait TextSprites.asset` 里有 `m_Name: Atlas_trait_icon_stun`，属那 73 张）。

### 改动清单

| 文件 | 落点（现行号） | 改了什么 |
|---|---|---|
| `d:/4/Unity/工具/gen_icon_doc.py` | `:11` · `:22-51`（新增） | `import re`；新增 `SPRITE_ASSET` / `ATLAS_TI` / `ATLAS_SS` 与 `atlas_slices()` —— 从 **TMP sprite asset** 现读图集切片**原名**、按前缀分档计数（盘上文件名是**剥掉前缀**的短名，**不能按盘上名数**） |
| 同上 | `:59-61` | `main()` 开头算 `at` / `n_atlas`（§五、§七 两处共用） |
| 同上 | `:196-198` | §五 那一条里的 `78 张图集切片` 也改成现数（**同一个常量的第二个副本**） |
| 同上 | `:214-245` | §七 那四行重写：三个数全现数；资产读不到时退回「按短名前缀现数」并**如实标注**（⛔ 不假装是图集口径）；两个集合对不上时**打差集**（两个方向都列）；「原版图集原图…78 张切片」`(:243-244)` 也改成现数 |
| `d:/4/Unity/资料/卡面图标_对照与缺口.md` | `:66` · `:2277` 等 | 生成物（§五 那句加了粗体标记 + §七 那句措辞） |

### 证据

**① 现数与硬编码值一致（= 这一改今天是 no-op，正是预期）**
`atlas_slices()` 实测 → `{'trait_icon': 73, 'spirit_stone': 5, 'total': 78}`，`short` 78 个
（与盘上 78 个 `traits/*.png` **一一对应**，只差 `Cruelty`/`Ecstasy` 两处的**大小写**）。
文档现读（`资料/卡面图标_对照与缺口.md:2277`）：

```
- 关键词/数值图标：`Resources/Art/traits/` **78 张**（`40ktraiticonatlas` **现数 78 张**切片：**73 张** `Atlas_trait_icon_*` ＋ **5 张** `Atlas_SpiritStone_*`）。
```

**② 三条新路径都打桩试过**（把 `gen_icon_doc.py` 当模块加载、`OUT` 打桩到临时文件，**没碰真文档**）

| 打桩 | 结果 |
|---|---|
| 真资产 | §七 = 「现数 78 张切片：73 张 ＋ 5 张」；**无**「对不上」告警（两个集合一致） |
| 资产路径不存在 | `atlas_slices()` → `None`；文档多出 ⚠️「上面这两个数是**按【短名前缀】现数**的…」；不报「对不上」（拿不到图集名，**不猜**） |
| 假资产（只写 2 张 `Atlas_trait_icon_*` + 1 张 `Atlas_SpiritStone_*` + 一个**短名别名**） | §七 = 「现数 3 张切片：2 张 ＋ 1 张」；🔴「**盘上与图集对不上**（盘上 78 张 / 图集 3 张切片）：盘上多的 = […77 个…]；图集里有、盘上没有的 = `['NotOnDisk']`」；**短名别名没被数进图集**（3 而非 4） |

**③ 幂等 / 行尾**：连跑两遍三份产物 md5 相同（见 `A1366` ⑥）；`gen_icon_doc.py` 与 `.md` 均纯 LF、未翻行尾
（`git diff --numstat` = `gen_icon_doc.py 64/3`、`.md 65/5`，与改动量相称）。

### 还差什么

1. ⚠️ **判据来源是「TMP sprite asset」而不是反编译/解包**：那 78 个 `Atlas_*` 名由
   `IconSetup` 从原版 bundle 复制而来（`Editor/IconSetup.cs:177,193,208-213`），
   **只在有人重跑 `IconSetup.Run` 时才会变**；若它被重建成「只有短名」，本脚本会退回兜底路
   并**如实标注**（已打桩验过那条路）。
2. 老的那句历史说明「2026-09-15 之前**只有 73 张**…」**保留**（是成立的历史，不是过期账），
   它里面的 73/5 是**当时**的数字，⛔ 没改成现数。
3. `资料/卡面图标_对照与缺口.md` 是生成物 ⇒ **本批的两处文案改动只活在生成器里**，
   下次谁手改那份 `.md` 会被覆盖（文件头已写「别手改」）。

---

## 顺手发现（⛔ 只报不改）

1. 🔴 **`资料/卡面图标_现状与缺口.md:85` 还留着那条被证伪的断言**
   「（`Stun an enemy` / `Choose a Sabotage card` / 写成词的 `Ranged Attack` **都不带**）」
   —— 与 `gen_icon_plan.py` 里我刚就地更正的那句**同源**（铁律 5 说「同一句被复制到别处的地方一起改」），
   但**它不在本批白名单**，只报。判据 = 本批 30 张卡面 + `_还原效果文字.md` 29 行双写。
2. 🔴 **`DA_None_Must_Know` 没有 900×1200 的 PnP 成品图** —— 全库按文件名搜 `None`/`Must`/`Know`
   只命中两张**别的**卡（`Sorotitas/3部队/Warpforge_27_Canoness.png` · `Ultramarines/4计策/Warpforge_32_Shall-Know-No-Fear.png`）；
   这张卡只活在 `d:/2/Warpforge部队卡片/Dark Angels/6秘密/IMG_3696.jpg`（该目录共 5 张**手机翻拍** `.jpg`，
   `资料/卡表核对_卡图提取/DarkAngels__3.md` 头部也这么记）。⇒ 凡「拿成品卡图当尺子」的活，
   这 5 张**只能降级取翻拍图**（本批就是这么核的）。
3. ⚠️ **`工具/gen_icon_plan.py:47` 的 `OUT_MD` 是死变量**（全脚本只出现这一行；
   `.md` 由 `gen_icon_doc.py` 写）—— 本批**原地加了注释说明**（没删，免得动别人的活），
   文件头「产物」那一节的两处错也已就地更正。
4. ⚠️ **`A1366` 的判据链还牵着 8 个同类词**（`R7` 的 B 类清单）：
   `vulnerable`(29 张) · `huntMark`(22) · `sabotage`(18) · `sniper`(13，**已亲读卡图证实是缺口**) ·
   `bloodThirst`(10) · `blind`(6) · `fast`(5) · `relentless`(1)。
   ⇒ 本批只做了 `Stun`；**同一个机制原因**（词出现在句中 / 句尾、后面不跟 `:`/`.`）在它们身上**一模一样**，
   而且**每做一个词都要再面对同一次那道 `continue` 判据**（本批已修，对它们同样有效）。
5. ⚠️ **`BARE_TOKEN_BY_CARD` 这个「逐卡」表可能不是这类词该待的地方** ——
   `Stun` 实测是**恒带**（30/30），即「这个词有图标」是一条**按词**成立的规则；
   按卡枚举的代价是**新卡会漏**（见 `A1366` 还差什么 2）。
   本批**按 `R7` 的落点照做**（它先写的就是逐卡），**没有**改成按词规则；
   要改再开一笔（`TOKEN_BY_RULE` 那张表就是现成的形状，且它的注释要求「写清为什么可以按规则」）。
6. ⚠️ **`traits/` 里 `Cruelty.png` / `Ecstasy.png` 是**大写开头**，其余全小写**
   （`atlas_slices()` 的短名对账要 case-insensitive 才对得上，实测差集就是这两个）。
   大小写敏感的任何「盘上名 ↔ 图集名」比对都会把它们误报成两处差集 —— 谁做 `A1370` 那条对账要当心。
7. ⚠️ **`Badges.KeyOf("stun")` 会返回 `"stun"`**（`Badges.SpriteKey` 只登记了 4 条反向映射
   `frenzied`/`rage`/`markOfChaos`/`concussive`，其余一律 `s.ToLowerInvariant()`；`Badges.cs:183-201`）
   ⇒ 运行时那枚图标外面会套一层 `<link=stun>`、悬停会去查 `stun` 这个词条。
   本批**没核**它查得到什么（`CardIcons` 属另一个写手的活），只报一句。
8. ℹ️ `DA39` 在**改动前整张卡都不在计划表里**（`desc`/`descZh` 两个字段都被那道 `continue` 跳过）
   —— `R7` 判据链的 ④ 已经写到这一条，本批实测复现了它，且**该缺点不是 `Stun` 独有**（见第 4 条）。
9. ℹ️ **`python -I` 会崩在 GBK 那条老规矩，本批复现了一次**（顺带回答 `W8` 的「顺手发现 2」）：
   `-I` = isolated mode **含 `-E`（忽略 `PYTHON*` 环境变量）** ⇒ `PYTHONIOENCODING=utf-8` **被吃掉**；
   只要那个脚本**自己不 `sys.stdout.reconfigure("utf-8")`** 又打印非 GBK 字符（`⚠` 就够），
   就 `UnicodeEncodeError: 'gbk' codec can't encode`。`gen_icon_doc.py`（`:14`）与
   `gen_icon_plan.py`（`:37`）**各自都 reconfigure 了** ⇒ 对**它们**来说 `-I` 无害（这才是 `W8`
   那次测不出问题的原因）。⇒ `资料/命令速查.md` 那条「别加 `-I`」**对「没 reconfigure 的脚本」是对的**，
   两条实测**不冲突**，别据此把那条规矩删掉。
