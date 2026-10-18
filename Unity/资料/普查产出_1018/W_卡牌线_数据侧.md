# W · 卡牌线【数据侧】写手报告（2026-10-18）

> 执行者 = 卡牌线数据侧写手代理。**没跑 Unity / `*.Run` / player**；跑过 = `gen_cards_engine.py`（含 `--check`）·
> `gen_card_id_truth_index.py --dry` · `ruleprobe.sh play check` · `typecheck.sh`（`TMPDIR=/tmp/wf_carddata`）。
> 逐条覆盖任务书 ①~⑰ 与批 2 的「不归我」清单。

---

## §一 逐条表

| A号 | 做了什么 / 没做 | 改动文件:行 | 判据出处 | 验证 |
|---|---|---|---|---|
| **`A2050`/`N5`** | ✅ **做了**：`official_name()` 的 `by_en` 英文名桥**加闸** —— 新增可选参 `bridge`，每次 `by_en` 命中都记进 `off_en_bridge`；调用方跑完**逐张打印**「🔴 英文名桥接 N 张」（没传 `bridge` 时函数自己 print 报警，任何调用点都逃不掉「出声」）。现读 **0 次**（1127/1127 走 id 直查） | `Unity/工具/gen_cards_engine.py:1607`（签名+gate）· `:1831`（`off_en_bridge` 列表）· `:2330`（调用点）· `:3119`（报警块） | `AUD_卡牌线_未完成盘点.md` `A2050` 行；`R_关账复核与多出卡.md:16`（by_en 桥接 0 次） | `--check` 输出「id 直查 1127 · 英文名桥 0」 |
| **`A1980`②** | ✅ **做了**：`MADE_LIST`（自造 id 清单）的写出**从诊断区挪到 `--check` 返回 + `_bad` 返回【之后】**，与卡表同生共死（只对账 / 出错都不落盘）。诊断区原位置留注释 | `gen_cards_engine.py:3425`（原位改注释）· `:3486-3495`（新写出点） | `AUD` `A1980` 行（`:3292` vs `:3315`） | `_tmp_view/card_ids_made.txt` mtime 仍 **06:56**（今天 id_made=0，任何一次运行都没碰它）· `--check` 绿 |
| **`A2011`+`A2044`** | ✅ **做了**：`card_stats.json` 的 `art` 守卫（形状照 `orphan_face_fix`/`armor_family_bad`）。判据 = `.../art/<阵营目录>/` 段 vs 本行 `faction`（13 个 cardassets 包文件名 0 重名 ⇒ 可靠）；**`noise:true` 的 10 条环境占位行跳过**；**4 条已裁「不改源表」的真错配进 `ART_KNOWN_BAD` 白名单**（只许缩小）；新增第 5 条 ⇒ **进 `_bad`、rc=1、一个字节不写** | `gen_cards_engine.py:2475-2521`（守卫）· `:2766`（返回）· `:2961`（打印）· `:3446`/`:3469`（进 `_bad`） | `R8_card_stats审计.md` §二·1/§二·2；`T02` 裁定 1（路 B） | `--check`：「`art` 一族：4 条已知错配（已裁定不改源表）之外无新增 ✅」 |
| **`A2036`** | ✅ **做了**：`note` 两处过期文案按现读改写 —— ① `armour` 列「消费者尚未切换」→ **已切换**（`CardDef.cs:297-299`，列非 0 就赢）；② `cardTraits` 那句「三个词未登记、写了会被静默丢弃」→ **2026-10-15 已登记**（`rage`→`Penitence` · `frenzied`→`Destroyer` · `unstoppable`→`Unstoppable`），并给出**现读实测**：`keywords` 含 Penitence 5 / Destroyer 10 / Unstoppable 0（`SAU7` 被 `_manual_keywords` 按卡面挡掉）。同族代码注释（`:2191` 附近）一并订正 | `gen_cards_engine.py:2740`、`:2745`、`:2191` 附近 | 现读 `CardDef.cs:297-299` + `cards_engine.json` | 重跑后 `note` 里三条新文案都在 |
| **`A1880`** | ✅ **做了**：`pick_truth_id` 把第三级 `by_name` **降为软匹配档** —— 只在 `by_official_en`/`by_tail` 完全落空时才用，并通过第二返回值把「用了它」报给调用方；调用方写进 `cascade_how = "真值索引(by_name 软匹配)"`（`id_cascade` 摘要逐条可见）。**索引生成器同步**：`gen_card_id_truth_index.py` 的 note 写明 ③ 是软匹配档 + 新增 `counts.by_name_only_keys`（现读 **52**）+ 修正 note 里早已过期的「真值源 = 2022 快照」 | `gen_cards_engine.py:909-970`、`:2008-2017`；`gen_card_id_truth_index.py`（stats/note/打印） | `AUD` `A1880` 行（`:923` 三级联）；`T01…md:60` | `gen_card_id_truth_index.py --dry` 正常：`by_name 1208（其中只有 by_name 能给候选的键 52 —— 软匹配档）`；`--check` 绿（直取 1127 · 级联 0 · 自造 0） |
| **`A1957`** | ✅ **核清：层已实现，无需改行为**：`RewriteMarkCardRefs`（`$$ID$$` → `Card_Name/<id>`）在 `RewriteMarks` ② 支已接；`Resources/official_names.json` 现有 `Card_Name/*` **1198 条**。现算：官方全表含 `$$…$$` 的描述 **189 条 / 141 个唯一 id / 363 处**（`Card_Description` 182 + `Card_Abilities` 7）；**逐 id 全都能在 `Card_Name/*` 查到（0 个解不出）**；我们卡池 `descZh` 里 **107 id / 139 处**，同样 0 解不出。**只订正了那段已过期的 docstring**（原写「116 种/151 处、Resources 下没有 Card_Name ⇒ 全部原样留着」） | `MyGame/Assets/CardPresentation/Core/CardIcons.cs`（`RewriteMarkCardRefs` docstring，2026-10-18 订正段） | `T03…md:29`；`R_卡牌线…md:60` | 静态计数（脚本口径见下）；`typecheck` 0/0 |
| **`A2117`** | ✅ **判定报告（现算，与生成器同口径：id 直查 → 官方显示名 by_en 桥）**：`Card_Name` 命中 **1127/1127** · `Card_Description` id 直查 **1122/1127**（余 5 张官方无条目）；`descZh` **带官方记号 623** · **与官方 zh 逐字相同 693** · **不同 429**（693+429=1122）。官方条目里 zh 为空/`en==zh` 的 **165**（回自译）。**需要改的只有层序注释**：`cardface_fixes.descZh` 叶子键 **251 → 279** | `gen_cards_engine.py`（层序注释 251→279，带历史留痕）· 生成的 `cards_engine.json` 现在 `descZh` 层序断言 **279/279** | `T03…md:43-47`；`R_卡牌线…md:61`（两说分母不同，已写明） | 生成器输出「人工、最高序 279/279 · 违例 0」 |
| **`A1956`** | ✅ **做了**：只改一条 —— `Card_Abilities/EffectIdolatrous` 的 zh `…获得 [[vulnerable]] 8` → `1`（en=1；同卡 `Card_Description/BL75` 中英都是 1）。`_note` 里留了订正痕。**⛔ 没有整表照搬** | `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`（1 行文件，改 1 处值 + `_note` 追加） | `T03…md:28`；AUD `A1956` 行 | 改后重读：`EffectIdolatrous` zh = `… [[vulnerable]] 1` · `_count` 仍 10854 |
| **`A2005`+`A1995`+`A2002`（同批）** | ✅ **做了 4 条**：① `AM15 Minka Lesk` desc 补 `Talent: Cadian Honour`；② `DA1 Azrael` desc 补 `Talent:` 前缀（`… Talent: Supreme Grand Master`）；③ `ASH83 Autarch` desc 补 `Waystone. ` 前缀；④ `descZh`：`PainBoy Sniklaw`「锯骨大夫」→「锯骨者」、`Abaddon the Despoiler`「四神宠儿」→「四者之选」（统一官方 `Card_Name` 的 zh）。⚠️ **GOF18 的键必须保持源表拼写 `PainBoy Sniklaw`**（首字母大写 B）—— 改成官方 `Painboy` 会**静默失配**（已实测：改错时该卡手工值整条掉了） | `Unity/数据/游戏数据/cardface_fixes.json`（`desc` +2、改 1；`descZh` 改 2；新增顶层 `_2026-10-18_A2005_A1995_A2002_desc与descZh` 留痕） | `T03…md`/`T02…md` 对应行；官方 `Card_Description/{AM15,DA1,ASH83}`、`Card_Name/{b4d6…,97e959…}` | 生成前后逐卡 diff：**desc 3 张、descZh 15 张**（见 §二）；`--check` 绿 |
| **`A2047`** | ✅ **逐条人判完成（结论：不改）**。候选 **21** 张（`desc` 含 troop 且 `descZh` 含「单位」）：**16 张是假命中**（那些「单位」对应的是英文 `unit`）；**5 张是真 `troop`→「单位」** = `GOF104`·`SW26`·`SW6`·`SW51`·`UM77`。**这 5 条逐字来自【官方中文表】**（`Card_Description/<id>` 的 zh），不是我们的译文 ⇒ **按「官方表是中文显示的第一权威」保持原样**（与 `A2049`「按官方先例」同口径），⛔ 不覆写官方值。另有 2 张（`EC1`/`UM40`）本来就是「部队」写法 | 无改动（判断记录在此） | `T03…md:25`；`desc`/`descZh` 现读 | 逐张列出 `desc` 与 `descZh` 的对照（本节） |
| **`A1827`** | ✅ **做了（逐条人判）**：换前先重建「旧名集合」= `843e78a^` 版卡池 vs 现池（改过名 725 张、其中旧名已不再被任何卡使用 700 个），扫现池 `descZh`：命中 16 张。**12 张真残留已改**（映射到**当前卡名** / 官方 `$$ID$$` 指向的卡）：ASH17 幽卫→幽冥守卫 · AM10 震荡突击兵→震荡兵 · EC33 恶魔少女→魅魔 · GSC33 新教徒混种→新血教徒 · SAU76 墓刃→墓穴之刃 · GOF4 斯鲁格小子→短铳小子 · SOR34 天界修女→洁天使 · Hymn of Battle 十字军战士→圣战士 · SW13 猎狼→**狩猎狼**（官方 `$$SW5$$`，⛔ 不是机械替换成「雷狼骑兵」）· SW23 灰猎手→灰色猎手 · SW3 战嚎→**战争嚎叫**（官方 `Card_Name/e3b573…`）· TAU28/TAU64 守护无人机→守护者无人机。**4 张不改**（逐条判过）：SOR73/SOR70 的「圣玫瑰修女」= 官方原文里的**Token 名**；Drone Companion 的「机枪无人机」与 SW13 改后的「狩猎狼」都是**子串假命中** | `Unity/数据/卡牌翻译/zh_cards.json`（7 条 `d`）＋ `cardface_fixes.json`（`descZh` 6 条，`A1842` 那批） | `T03…md:23`；`W2_卡名换官方.md:93-96`（89 个旧名/150 处）· 现算口径见本节 | 改后复扫：**真残留 0**（剩 4 条全是子串假命中）；生成后逐卡 diff 15 张 descZh 与预期逐字一致 |
| **`A1842`** | ✅ **做了（就是上面 `cardface_fixes` 那 6 条）**：Spiritseer · Cadian Standard Bearer · Alluress · Slugga Boy · Hymn of Battle · Enforcer Battlesuit。⚠️ 现读**只有 6 处**（账上 16 处是更早分母）。改前 `grep '"<卡名>":'` 逐条核过；改后 `object_pairs_hook` 全文件**重复键 = 0**；CRLF 未翻 | `cardface_fixes.json`（`descZh` 6 条） | `T03…md:24` | 重复键扫描 0 · `git diff --numstat` 12/9（不是整篇重写） |
| **`A2049`** | ✅ **已满足（无需再改）**：现读 `UM44 Armoured Support` 的 `descZh` = 「在这场比赛的剩余时间里…」—— **已经是「比赛」**（主对话裁定的那个译法已落在 `cardface_fixes.descZh`）。全池再无 `the match` 的「对局/比赛」两说 | 无 | `T03…md:27`；主对话裁定（任务书 ⑩） | 现读值逐字核对 |
| **`A2132`**（新账） | ✅ **现读已达成，无需改**：`GSC26 Aberrant Hypermorph` 现池 `armour = 2`、`keywords = ['Armour 2.','Vanguard']`；源表 `card_stats.json` 的 `armor` 仍是 **null**（⛔ 没碰源表，路 B 守住）。走的就是 `fix_own_armour`（desc = `Armour 2. Vanguard`）→ `armour` 列的 PnP 链 | 无（AUD 的「引擎 None」**已过期**，见 §五） | PnP 卡图；当前 build SO `armour=2`；现读 `cards_engine.json` | 直读现池逐字核 |
| **`A1976`** | ✅ **做了**：现算 `desc` 含 `\bCodex\b` = **29 张**（复算过），其中 `desc` **无 `Codex:`/`[Codex]` 前缀** = **5 张**（`40667e34…`/`UM40`/`UM35`/`UM113`/`UM73`），`keywords` 含 Codex = 31 张。`CardDef.cs` 注释里「20 张 / 裸写 10 张」**就地订正**（保留旧值 + 2026-10-18 订正段） | `MyGame/Assets/RuleEngine/Core/CardDef.cs:676-683`（新增 7 行订正注释） | `台账…:492`；AUD `A1976` 行 | 现算脚本 + `typecheck` 0/0 |
| **`A1849`** | ✅ **已由改名自然解决，无需别名**：`SAU38` 现名 = `Extermination Protocol`（= 官方 `Card_Name/SAU38`）；`GOF12` 现名 = `Spanner`（= 官方 `Card_Name/GOF12`）。两条 id 唯一、名字与官方逐字一致 ⇒ 0.98 / 0.82 的模糊匹配**不再有输入** | 无 | `R_卡牌线…md:69`；现读官方表 | 逐字对照 |
| **`A1982`** | ✅ **现算一次，给出唯一结论**：运行时词条口 = `Loc.cs` 的 `Card_Trait/*` **64 条** ＋ `official_names.json` 的 `Card_Trait` **27 条**。**官方全表** `[[X]]` **3508 处 / 74 种**，缺词条 **8 处 / 7 种**（`reanimate` 2 · `盔甲`/`震荡的`/`不稳定`/`集结`/`tainted`/`maintenance` 各 1）；**我们卡池**（`descZh`+`desc`，真正会渲染的输入）`[[X]]` **1079 处 / 60 种，缺词条 0 处 / 0 种** ⇒ **对玩家可见的卡面这条账已经关上**。「46 处/8 种」「56 处/8 种」都是更早分母 | `CardIcons.cs`（`RewriteMarkTraits` docstring 增订正段）+ 本节记录 | `R2_B1落账草案.md:294-299`；AUD §二 `A1982` | 现算脚本（口径写在 `CardIcons.cs` 那段里） |
| **`A2053`** | ✅ **点名部分已落地**（`579bcad` 已改 → 现池 `GOF20 = Veteran Stormboy`）。**剩余尾巴处理**：① `card_stats.json` 的 `name = "Veteran Flyboy"` = **活着的源表键**（`RARITY_UNLISTED:259`、`cardface_fixes` 的 `subtype` 键都按**改名前的 name** 查）⇒ **不能改**；② `card_ids.json` 的 `GOF20 → Veteran Flyboy` 同理（`pick_id` 按源表名 join）⇒ **保留 + 在 `note` 里加「历史键留痕」**；③ `cardface_fixes.json` 的那条 `subtype` 键**已自带旧名说明**（2026-09-15 那条），保留；④ **`OrksDeck2` 已不是 28/30**：现读 `complete: true` · 30 张 · `missing=[]` · `problems=[]`（只读核对，⛔ 没改预组数据） | `Unity/数据/游戏数据/card_ids.json`（`note` 追加一段） | `T01…md:55`；AUD `A2053` 行；现读 `prebuilt_decks.json` | 三处旧键的存在理由逐条核实（有消费者 ⇒ 保留）；`OrksDeck2` 逐字段打印 |
| **4 张 `Dark Pact of *`** | ✅ **已开 PnP 图逐张核（只记录，未改）** —— 见 §四。**结论：这 4 张 PnP 卡面【都没有印费用】**（右上角是 Dark Pact 星标，没有费用宝石/数字）⇒ 账上「卡图=2」**无法从 PnP 复现** | 无 | `d:/2/Warpforge部队卡片/Chaos/4计策/Warpforge_{16,18,20,22}_Dark-Pact-of-*.png`（已逐张 Read） | 4 张逐张亲读 |
| **`A1789`+`A1790` 余项** | ✅ **做了**：① `待办判据_第四会话.md:291` —— 保留「判据本来就空不再成立」的更正，**把过期的 `9,440/9,447` 标注为 2025-07-30 旧快照**，补现读（`en+zh` 10,854 · `Card_Name` 1,198 · `Card_Description` 命中 1,122/1,127）；② `待办判据_第六会话.md:146`（`A1103`）—— **重开登记**：官方表已落盘、`nameZh`/`descZh` 已以它为首选 ⇒ **有原版判据可核**，判据 = 官方表 + 2026-09-14 运行期 dump | `Unity/资料/待办判据_第四会话.md:291` · `Unity/资料/待办判据_第六会话.md:146` | `普查产出_第二十七会话/新发现_待立账.md:142` | `git diff --numstat` 两文件各 **1/1**（没有翻行尾、没有整篇重写） |
| **批 2（不归我）** | ⏭ **只报告、没动**：`A1963`（`BattleDriver.cs` 3 行 `maxTargets`）· `A1679`（`BattleDriver.cs:14008` 手搓 playable）· `A1674`（`CardView` 粒子队列）—— 都在战斗侧/卡面侧文件 | 无 | 任务书 ⑦ | — |

---

## §二 生成前后逐卡差异（`cards_engine.json`，1127 → 1127，id 集合 0 变化）

> 方法：`_tmp_view/carddata_1018/cards_engine.before.json`（开工前快照） vs 重跑后的现文件，**逐卡逐列**比。
> ⚠️ `cards_engine.json` 是**单行 JSON** ⇒ `git diff --numstat` 恒 `1/1`，**不能**拿它判改动量。

| 列 | 变了多少张 | 名单 |
|---|---:|---|
| `desc` | **3** | ASH83 · AM15 · DA1 |
| `descZh` | **15** | ASH17 · AM10 · BL34 · EC33 · GSC33 · SAU76 · GOF18 · GOF4 · SOR34 · `4d348cff…`(Hymn of Battle) · SW13 · SW23 · SW3 · TAU28 · TAU64 |
| `name` / `nameZh` / `keywords` / `armour` / 其余全部列 | **0** | — |

**改动 card 总数 = 18**（3 + 15，无交集）。逐条「旧 → 新」的完整值见 `_tmp_view/carddata_1018/` 的 diff 输出（本报告只列名单；正文值在上文 §一 `A2005`/`A1827` 行）。

**其它数据文件 diff 量**（`git diff --numstat`）：

| 文件 | +/- | 说明 |
|---|---|---|
| `官方本地化_en+zh-CN.json` | 1/1 | 单行 JSON，只改 1 个值 + `_note` |
| `card_ids.json` | 1/1 | 单行 `note` 追加「历史键留痕」 |
| `zh_cards.json` | 7/7 | 7 个 `d` 值（CRLF 保持） |
| `cardface_fixes.json` | 12/9 | `desc` +2/改1、`descZh` 改2、`note` 键 1 条（CRLF 保持；重复键扫描 0） |
| `cards_engine.json` | 1/1 | 单行生成物（真实改动见上表） |

---

## §三 `PinnedPlayMd5`：**没有重钉（有意）**

| 项 | 值 |
|---|---|
| 钉住常量（改前=改后） | `76ced8a686a9c316624c37415a91d58c`（**未改**） |
| 基线文件内容 md5 | `2754c07fac4227283235b3e9988fd8ad`（**未改**，git 里无改动） |
| 当次现算（**改动前**卡池） | `f3afcdbec447adcd5bc1c3ffa378d742` |
| 当次现算（**改动后**卡池） | `f3afcdbec447adcd5bc1c3ffa378d742` —— **逐字相同** |

**重钉依据 / 为什么不钉**：

1. **本批 `desc` 改动对结算指纹的影响 = 0**。我用「临时把改动前快照换进工程跑一遍、再换回」的方式实测：
   - 改动前：`play check` → 现算 `f3afcdbe…` · **①-a 60 张变** · **①-b 4 张变**；
   - 改动后：**完全相同的输出**（同一 md5、同一 60/4 名单，逐行 diff 为空）。
   ⇒ `AM15`/`DA1`/`ASH83` 这三条 `desc` 改动**没有移动任何一张卡的结算指纹**（`Talent:`/`Waystone` 都已在 `keywords` 层生效，不是新机制）。
2. **`play check` 现在 rc=1 是【改动前就存在的红】**：② 哨兵报「基线文件内容（`2754c07…`）≠ 钉住常量（`76ced8…`）」—— 基线文件在 `3251df0`（第五十会话）被重生成过、常量没跟；①-a/①-b 也有 60/4 张既有差异。
3. `PlayProbe.cs` 自己的指引写着「重钉前必须先确认这批变化是对的；为了把红抹平而重钉 = 正是它在抓的事」。这 60/4 张**不是本批造成的**，我也没有逐张归因它们 ⇒ **重钉会把这批未确认的漂移一起盖成绿**，所以**不钉**，留给主对话/专门一轮处理。
4. ⚠️ 若主对话决定现在就钉：该抄的是**基线文件 md5 `2754c07fac4227283235b3e9988fd8ad`**（`play check` 输出里会把整行打出来），与本批无关。

---

## §四 没查到 / 待主对话裁

1. **4 张 `Dark Pact of *` 的印刷费用（只记录、没改）** —— 逐张开 PnP 成品卡图亲读：

   | id | 卡名 | PnP 文件 | 卡面读数 |
   |---|---|---|---|
   | `4a48457c…` | Dark Pact of Fate | `Chaos/4计策/Warpforge_16_Dark-Pact-of-Fate.png` | **没有印费用**（右上角只有 Dark Pact 星标；文案 `Give +2 Health and Camouflage`） |
   | `9a2cadc6…` | Dark Pact of Blood | `…/Warpforge_18_Dark-Pact-of-Blood.png` | **没有印费用**（`Give +2 Melee Attack and Vanguard`） |
   | `b8e81b2f…` | Dark Pact of Excess | `…/Warpforge_20_Dark-Pact-of-Excess.png` | **没有印费用**（`Give +2 Melee Attack and +2 Ranged Attack`） |
   | `1dfc9bc7…` | Dark Pact of Resilience | `…/Warpforge_22_Dark-Pact-of-Resilience.png` | **没有印费用**（`Give +1 Health and Regeneration 1`） |

   ⇒ 账上「卡图=2 / 引擎=1 / SO=−1」里的「卡图=2」**在 PnP 上无法复现**（四张都没有费用位）。**维持引擎 `cost=1`，等主对话裁**；⛔ 没有改任何数据。

2. **`A2047` 的 5 条真 `troop`→「单位」**：来源全是**官方中文表**。我按「官方表 = 中文显示第一权威」判**不改**；如果主对话要的是「术语统一压倒官方原文」，那就需要 `cardface_fixes.descZh` 覆写这 5 张 —— **等一句裁定**。

3. **`A1880` 的索引生成器产物 `card_id_truth_index.json` 没有重跑**（它不在本代理的文件白名单里）：我只改了生成器源码（note 文案 + `by_name_only_keys` 统计），用 `--dry` 验证可跑。**需要主对话跑一次** `python -I -X utf8 Unity/工具/gen_card_id_truth_index.py` 让产物里的 note/统计跟上（不跑也不影响现在的生成器行为 —— 消费侧只读索引内容，不读 note）。

4. **`A2053` 的 `card_stats.json` 源表名 `Veteran Flyboy`**：它是**活键**（两条消费者），我**没有改**、也没法加注释（该文件没有 `_note` 位）。理由与消费者清单已写进本节与 `card_ids.json` 的 note。

---

## §五 顺手发现（分诊）

1. 🔴 **AUD 的 `GSC26` 读数过期**：`AUD_卡牌线_未完成盘点.md` §三⑥ 写「`GSC26` 引擎 = None」，现读 `cards_engine.json` 的 `GSC26` **`armour=2` / `keywords=['Armour 2.','Vanguard']`**（来源 = desc `Armour 2. Vanguard` → `fix_own_armour`）。⇒ 这条**不是缺口**，A2132 可关账。**错因**：把「源表 `armor` 列 = null」读成了「引擎 `armour` = null」。
2. 🔴 **AUD 的 Dark Pact「卡图=2」不可复现**：四张 PnP 都**没有印费用**（见 §四·1）。请主对话裁时以这个读数为准。
3. ⚠️ **`Editor/MainMenuScene.cs:3697` 的注释已过期**（不在我白名单）：它写「`Resources/prebuilt_decks.json` 里 `OrksDeck2` 的 `complete = false`（`missing = ["GOF20 x2 (miss-known)"]` · `problems = ["size:28/30"]`）」—— 现读 **`complete: true` · 30 张 · 两列全空**。**断言本身不受影响**（它比的是「替身表 4 副」），但注释会让下个会话误判。建议界面线顺手改。
4. ⚠️ **`cardface_fixes.json` 的子段键用「源表名」而不是「显示名」**：`PainBoy Sniklaw`（源表首字母 B 大写）vs 官方显示名 `Painboy Sniklaw`。今天靠 `face_fixes.get(name)`（**改名前的 `name`**）命中；谁把键改成官方拼写，那条人工值会**静默掉**（本批实测踩过一次、已回滚）。同族还有 `Veteran Flyboy`。
5. ⚠️ **`gen_cards_engine.py` 里多处「现读 N 张」是死数**（本批顺手订正了 `descZh` 叶子键 251→279 与 Codex 20→29 两处）。建议后续凡是「现读数」都写成**口径 + 指针**，别写死。
6. ℹ️ **`official_names.json` 的族已经扩到 9 族 / 3156 条**（`Card_Name` 1198 最多；`Card_Trait` 27）。`CardIcons` 里两段「本地没有」的旧 docstring 已在本批订正（`A1957`/`A1982`）。
7. ℹ️ **`play check` 的既有红**（60/4 + 基线 md5 不匹配）**与本批无关**，但**它现在是红的** —— 见 §三，建议单独立账（既不是 `A1943` 记的那批 773/345，也不是本批）。

---

## §六 验证汇总

| 验证 | 结果 |
|---|---|
| `python -I -X utf8 Unity/工具/gen_cards_engine.py --check` | **一致 ✅**；`art` 一族 4 条已知 / 0 新增；`armor` 一族 6 行一致；英文名桥 **0**；`descZh` 层序 **279/279 · 违例 0**；`keywords` 三条断言 **0 违例** |
| 生成前后逐卡 diff | **18 张**：desc 3 · descZh 15；id 集合 0 变化；其余列 0 |
| `gen_card_id_truth_index.py --dry` | 正常；`by_name_only_keys = 52`（软匹配档） |
| `bash Unity/工具/ruleprobe.sh play check` | rc=1（**改动前后逐字相同**；红 = 既有基线/常量不匹配，非本批） |
| `TMPDIR=/tmp/wf_carddata bash Unity/工具/typecheck.sh` | 运行时 **0 错** · 编辑器 **0 错** |
| 重复键扫描（`cardface_fixes.json` / `zh_cards.json` / `card_ids.json`） | **0** |
| 行尾 | 全部保持原样（`cardface_fixes`/`zh_cards` CRLF 未翻；`card_ids` LF；`cards_engine` 单行） |

---

## §七 改动文件清单（本代理）

| 文件 | 性质 |
|---|---|
| `Unity/工具/gen_cards_engine.py` | 5 项：by_en 闸 · MADE_LIST 挪位 · art 守卫 · note 文案 · by_name 软匹配档 |
| `Unity/工具/gen_card_id_truth_index.py` | 软匹配档统计 + note 订正 |
| `Unity/数据/游戏数据/cardface_fixes.json` | desc +2/改1、descZh 改 8（2 条 `A2002` + 6 条 `A1842`）、留痕键 1 |
| `Unity/数据/卡牌翻译/zh_cards.json` | 7 条 `d`（`A1827`） |
| `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` | 1 条错值 + `_note` |
| `Unity/数据/游戏数据/card_ids.json` | `note` 追加历史键留痕 |
| `Unity/MyGame/Assets/CardPresentation/Core/CardIcons.cs` | 两段过期 docstring 订正（`A1957` / `A1982`） |
| `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs` | Codex 张数注释订正（`A1976`） |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | 中文档期望值 **49/27 → 58/33**（本批 descZh 改名的必然漂移；英文 128/67 不变） |
| `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` | 重新生成 |
| `Unity/资料/待办判据_第四会话.md` · `_第六会话.md` | 各 1 行（`A1789`/`A1103`） |
| `Unity/资料/普查产出_1018/W_卡牌线_数据侧.md` | 本报告 |
