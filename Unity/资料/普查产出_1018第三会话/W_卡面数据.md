# W_卡面数据 · A977 / A981 / A982 / ④「27 张中文卡面零图标」—— 执行写手报告

> 2026-10-08 第三会话。**只改 4 个文件**（+它们的生成产物）：`数据/游戏数据/cardface_fixes.json` ·
> `数据/游戏数据/card_stats.json` · `工具/gen_icon_plan.py` · 生成器产物 `card_icon_plan.json`（两份）/`cards_engine.json`。
> **没跑 Unity、没动 git、没改两张正本**，也没碰另两位写手的 `MyGame/Assets/CardPresentation/**`（除生成器产物）与 `RuleEngine/**` 源码。
> 判据一律 = **逐张开 PnP 成品卡图亲读**（铁律 7，本批共亲读 **17 张**）+ 卡池 `desc` / 还原表。

---

## 一、四条账的结论

| 账 | 结论 | 落点 |
|---|---|---|
| **A977** | ✅ **已改**（4 张） | `cardface_fixes.json` 的 `descZh` |
| **A981** | ✅ **已改**（13 张，其中 `DA86` 是「写错」档） | `cardface_fixes.json` 的 `descZh` |
| **A982** | ✅ **已改**（**23 行**，不是账上写的 3~4 行） | `card_stats.json` 的 `desc` |
| **④** | ⚠️ **部分改**：真缺陷是**机制层**，落点**不是**（只是）`gen_icon_plan.py` —— 见 §五 | `gen_icon_plan.py` + 3 张 `descZh` |

---

## 二、A977 —— 4 张「发动费用前缀」的中文卡面原来一枚图标都没有

**改哪儿**：`cardface_fixes.json` → `descZh` 列（新键 4 条）。

| 卡 | 当前中文 | 改成 | 判据（**照卡图**） |
|---|---|---|---|
| `UM80 Devastator Marine` | `4：对 1 个敌方部队造成 5 点伤害。` | `誓言 4：对 1 个敌方部队造成 5 点伤害。` | 卡图 `Ultramarines/3部队/Warpforge_16_Devastator-Marine.png`：首行 `⟨爆裂徽⟩Blast 2. ⟨蓝圆盘+白袍人形⟩Oath 4: Deal 5 damage…` ⇒ **是 Oath**、卡面印着词 `Oath` |
| `UM81 Phobos Librarian` | `3：对 1 个敌人造成 2-4 点伤害。若目标死亡，获得护盾。` | `誓言 3：…` | 卡图 `…/Warpforge_17_Phobos-Librarian.png`：`⟨camouflage⟩Camouflage. ⟨oath⟩Oath 3: Deal 2-4 damage…` |
| `UM_Scout_Sniper Scout Sniper` | `1：造成 1 点伤害。` | `誓言 1：造成 1 点伤害。` | 卡图 `…/Warpforge_11_Scout-Sniper.png`：`⟨camouflage⟩Camouflage. ⟨sniper⟩Sniper. ⟨oath⟩Oath 1: Deal 1 damage` |
| `SOR72 Adelaide the Serene` | `6：…若你的**能量**少于 6 点，每有 1 个敌方单位便获得 1 点**能量**。` | `6 ☀：…若你的**信仰**少于 6 点，每有 1 个敌方单位便获得 1 **☀**。` | 卡图 `Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png`：前缀那个是**金太阳**（`6☀：Gain ⟨flank⟩Flank and ⟨shield⟩Shield`，正文三处也全是金太阳）= **信仰**，不是能量 |

> 三张 `Oath` 的写法沿用全池既有约定（计划表里 `誓言 1..8：` 已有 19 处、都画 `oath` 徽记）；
> `6 ☀：` 沿用 `☀ → faith` 那条既有规则（计划表 31 处）。**`desc`（引擎解析原文）一字未动**（属 A975 族，见 §六·4）。

---

## 三、A981 —— 13 张中文按卡面订正（**⛔ 没有拿中文去改英文**）

**改哪儿**：`cardface_fixes.json` → `descZh` 列（新键 13 条）。判据：**亲读卡图** + 该卡 `desc` 早已补好的规范写法
（`_manual_desc_note` 那批：`+1 Attack` / `+3 Attack` / `+1 Ranged`）。

| 卡 id · 卡名 | 当前中文 | 改成 | 卡面（**照卡图**，阵营目录） |
|---|---|---|---|
| `EC1 Lord Kaphrael` | `…给予一个随机友方部队 +1。` | `… +1 攻击。` | `Emperor_s Children/1督军/Warpforge_01_Lord-Kaphrael.png`：`+1⟨**拳**⟩` = 近战 |
| `EC16 Slaanesh's Spawn` | `残忍：获得 +1 和侧翼。` | `残忍：获得 +1 攻击和侧翼。` | `…/3部队/Warpforge_16_Slaaneshs-Spawn.png`：`+1⟨**拳**⟩ and ⟨flank⟩Flank` |
| `EC58 Exquisite Swordsmanship` | `你的战将本回合获得 +1。…` | `…获得 +1 攻击。…` | `…/4计策/Warpforge_58_Exquisite-Swordsmanship.png`：`+1⟨**拳**⟩`（尾句 `Warlord's Melee` 是**印着词的**，不动） |
| `EC77 Xylocil` | `给予一个友方部队 +2。` | `… +2 攻击。` | `…/6药剂/Warpforge_77_Xylocil.png`：`Give +2⟨**拳**⟩` |
| `GSC22 Acolyte Leader` | `…给予你的部队 +1 和 +1 生命。` | `… +1 攻击和 +1 生命。` | `Genestealer Cult/3部队/Warpforge_22_Acolyte-Leader.png`：`give +1⟨**拳**⟩ and +1 Health` |
| `SW26 Thunderwolf Cavalry Pack Leader` | `…本回合你的单位获得 +1。` | `…获得 +1 攻击。` | `Space Wolves/3部队/Warpforge_26_Thunderwolf-Cavalry-Pack-Leader.png`：`give +1⟨**拳**⟩` |
| `TAU72 Krootox Rampager` | `侧翼。集结：本回合获得 +3` | `… +3 攻击` | `Tau/3部队/Warpforge_08_Krootox-Rampager.png`：`Rally: Gain +3⟨**拳**⟩` |
| `TAU75 Thundering Rampage` | `给予 1 个友方单位本回合 +3。…` | `… +3 攻击。…` | `Tau/4计策/Warpforge_11_Thundering-Rampage.png`：`Give +3⟨**拳**⟩` |
| `TL86 Bestial Rage` | `给予所有友方部队 +4` | `… +4 攻击` | `Tyranid/4计策/Warpforge_13_Bestial-Rage.png`：`Give +4⟨**拳**⟩` |
| `UM_Angel_s_Wrath` | `临时。给予 1 个友方单位 +1，…` | `… +1 攻击，…` | `Ultramarines/2天赋/Warpforge_06B_Angels-Wrath.png`：`Give +1⟨**拳**⟩` |
| `UM_Assault_Doctrine` | `给予 1 个友方单位 +3。…` | `… +3 攻击。…` | `Ultramarines/4计策/assault doctrine.png`：`Give +3⟨**拳**⟩` |
| `UM_Angels_of_Death` | `…给予你的原铸仲裁者 +1。` | `… +1 **远程攻击**。` | `Ultramarines/4计策/angel.png`：`Give +1⟨**紫枪**⟩` ⇒ **全 13 张里唯一一枚远程** |
| 🔴 `DA86 Hunters of Heretics` | `…“反噬：你的对手获得 2 个**骷髅头**”` | `…“反噬：你的对手获得 **2 点任务**”` | `Dark Angels/4计策/Warpforge_12_Hunters-of-Heretics.png`：引号里是 `⟨**银灰锯齿环、圈里烘着 2**⟩` = **任务点**；骷髅是 `Backlash:` 前缀那一枚（白骷髅）⇒ 原文**写错** |

- `2 点任务` 是刻意选的写法：既对得上既有 17 处 `N 点任务 → questPointsN`（数字烘在图里），也顺带让**图标被画出来**。
- 其余 12 张按英文规范写法用 `+N 攻击`（**与英文卡面同形**：英文那侧也是印词 `Attack`/`Ranged`，不是图标）。

---

## 四、A982 —— `card_stats.json` 抄录层（**实改 23 行**）

账上只写了 3~4 行，是抽样；**同类实为 183 行**（`card_stats.desc` ≠ fix 表 `desc` 的全部），
其中**「裸数字」（OCR 把图标丢了、只剩数字）这一档 = 23 行**，本批**全做**：

- **任务点 21 行**：`Aggressor` · `Inner Circle Companion` · `Intercessor` · `Master Lazarus` · `Sergeant Naaman`(DA11) ·
  `Fury of the Unforgiven` · `Grim Effigy` · `Hunt the Fallen` · `Martial Superiority` · `Master Interromancer` ·
  `Reconnaissance Mission` · `The Rock`(DA51) · `Black Knight` · `Darkshroud` · `Ravenwing Bikes` · `Ravenwing Champion` ·
  `Ravenwing Speeder` · `Asmodai` · `Watcher in the Dark` · `Hunters of Heretics`(DA86) · `Master of Repentance`
  → 一律补成 `Gain N Quest Point(s)`（与卡池**逐字同形**，取自 `cardface_fixes.json` 的 `desc` 列）。
- **信仰 2 行**：`Prayer` · `Shrineworld` → 补成 `gain 1 ☀`（同一类缺陷 —— `_manual_desc_note` 里那个例子就是 `Gain ☀2` → `Gain 2`）。
- **`UM80` 的「缺 `Blast`/`Oath`」：⛔ 不动** —— 它的 `keywords` 就是 `['Blast 2','Oath']`，
  `CardText.KeywordSegment` 会把 `〈爆裂〉爆裂 2。〈誓言〉誓言…` **补在最前面**；往 `desc` 里再写一份会**印两遍**（且属 A975 族）。

**副作用 —— RECON 那条说法是错的，已实测推翻**：
> RECON 写「订正后那几条 `desc` 修正会**从 fix 表里消失**」。**不成立**：
> `gen_cardface_fixes.py:178-180` 的 `keep` 白名单里 `desc` / `descZh` **与顶层所有 `_` 开头的键一起原样带走**，
> 而该脚本**只会重算 `subtype` / `keywords` 两列**。实跑两次（改前/改后）：
> `desc` 185→185 · `descZh` 138→138 · **0 条被删**（`git diff` 只看到我在 §二/§三 加的那几条）。

---

## 五、④「27 张中文卡面零图标」—— 判据查清了，但**真缺陷比账上大得多**

### 5·1 ✅ 已改：**中文单字关键词被正则下界挡掉**（唯一一处**确凿的机制缺陷**）
- `gen_icon_plan.py` 的 `KEYWORD_SCAN_ZH` 下界是 **`{2,8}`**，而 `_规则书关键词表.md` 里
  **`团` → `Regiment` 是单字** ⇒ 中文句首 `团：` **永远扫不到**。
- **卡图判据**：`Astra Militarum/3部队/Warpforge_14_Kasrkin.png` = `⟨骷髅头盔圆徽⟩Regiment: Deal 1 damage…`
  （**图标 + 词**）⇒ 中文侧也必须是 `⟨徽记⟩团：`。
- **改法**：`{2,8}` → `{1,8}`（贪婪匹配 + `zh_en` 闸门 ⇒ 只放行词表里的词，词表里只有 `团` 是单字）。
  **落在 `工具/gen_icon_plan.py` 的 `KEYWORD_SCAN_ZH`**（连同 13 行说明）。
- **实测新增 9 条 `团： → regiment`，全池一件不多、一件不少**：
  `AM9 AM12 AM14 AM25 AM26 AM30 AM33 AM42 AM70`（后三张 `AM25/AM42/AM70` 叫
  `Tech-Priest Enginseer / Rogal Dorn Tank / Tempestor Sergeant`，**它们本来连 descZh 计划条目都没有** ⇒ 是账外的 4 张）。

### 5·2 ✅ 顺手改（3 张，同族「中文把卡面的资源记号译成了词」，也都是 27 张名单内的）
| 卡 | 当前中文 | 改成 | 判据 |
|---|---|---|---|
| `DA25 Techmarine` | `…获得 1 点**能量**。` | `…获得 **1 点任务**。` | `desc` = `gain 1 Quest Point`（P2 批已按卡图把 `[Energy]` 订正过） |
| `DA60 Wages of Retribution` | `…你获得 1 点**能量**。` | `…**1 点任务**。` | 同上 |
| `SOR13 Hymn of Battle` | `…获得 1 点**能量**；…` | `…获得 1 **☀**；…` | `desc` = `Gain 1 ☀`；卡面那一枚是**金太阳**=信仰 |

### 5·3 ⛔ **未改（19 张）—— 判据不清在「机制」这一层，不在「卡面」这一层**
| 类 | 张数 | 卡 | 为什么停手 |
|---|---|---|---|
| **(a) `+N 近战/远程攻击` 的**属性图标** | 12（**全池实为 124 张**） | `DA44 DA6 DA50 DA78 EC26 GSC51 GSC55 SW62 SW63 SW49 UM_Master_of_Arms UM_Righteous_Fury` | 🔴 卡面（`Space Wolves/4计策/Warpforge_62_Legendary-Tenacity.png`：`Give +4⟨**拳**⟩, +4⟨**紫枪**⟩ and +4 Health`）**只印图标、不印词**；而 `CardIcons.Rewrite` 判「换掉 vs 插在词前」**只看 token 首字符**（`近` 是字母 ⇒ 只会「插在词前、词留着」）⇒ 现状机制**做不到「只留图标」**。要做得选一条：**① 中文改写成方括号记号**（`+4 近战攻击` → `+4 [攻击]`，19/7 处既有先例）或 **② 计划表加「replace」标记 + 改 `CardIcons.cs`**（**那是另一位写手的文件**）。⚠️ 直接给 `近战攻击` 加一条裸 token = **给卡面印上原版没有的词**（工程红线） |
| **(b) 方括号 ⇒ 卡面只印图标** | 2 | `SW68`（`护盾` ← `[Shield]`）· `SAU42`（`装甲 2。`，词表里是「护甲」） | 同 (a) 的机制问题（`SAU42` 只要把「装甲」改「护甲」就自动走 `护甲 2。` 那条已有规则） |
| **(c) `黑暗契约` 的图标位置** | 3 | `BL19`（`❄` 符号，中文只剩词）· `BL51`（`[Dark Pact]`）· `BL69`（`[Chaos]`） | 要先定「图标 + 词」还是「只留图标」（英文侧那张卡是方括号 ⇒ 大概只留图标），**得逐张读卡图** |
| **(d) 句中关键词** | 1 | `UM_Avenging_Zeal`（`…触发典籍时…`） | 英文是 `[Codex icon] Codex`（句中带徽记）；中文是光秃秃的「典籍」⇒ 可走 `BARE_TOKEN_BY_CARD` 的中文支（`UM84` 有先例），但**要先确认卡图那枚的位置** |
| **(e) 中文漏印整句关键词** | 1 | `GSC25 Nexos`（中文**整句没写** `Artifice:`，直接是 `直到你的下回合，你的单位获得 +1 攻击。`） | 这是**译文漏了一句**、不是图标问题 ⇒ 该补的是中文正文（拟补 `巧技：` + 该句），属「中文按卡面订正」那一族 |

**为什么建议整批发（而不是我现在就补 19 张里的 15 张）**：124 张那一类要先定口径，而补 (b)~(e) 会**同一批卡面里 icons 一半有一半没有**、更难复核；
且本批已经把计划表从 **1606 → 1625** 条，**另一位写手正在改 `IconSetup.cs` 的断言与计数** ⇒ 不宜再叠 ~170 条上去（见 §六·3）。

---

## 六、顺手发现（都没改，交给调度台分流）

1. 🔴 **`+N 近战/远程攻击` 全池 124 张都没有属性图标**（判据：`descZh` 里有 `+N 近战攻击|远程攻击|近战|远程` 且**计划表零覆盖**）。
   其中 **0 张**被任何计划条目覆盖 —— 这不是 27 张的事，是**一整类**。英文侧同理（`desc` 里写成词的 `Ranged` / `Ranged Attack` 也一个图标都不画）。
2. 🔴 **`SOR72` 的 `desc` 里那个 `6:` 没写资源名** ⇒ `EffectText` 的付费前缀解析拿不到 `faith`（它**认得 `☀` 字形**，`EffectText.cs:5898-5918`），
   于是我们这边按**能量**付费，而卡面是**金太阳（信仰）**。是「发动费用资源」的真差异，属 **A975 族（已收口）** ⇒ ⛔ 未动，仅记录。
3. 🔴 **计划表张数变了，`IconSetup.Verify` / `资料/*` 里那几个数是旧的**：现在 = **580 张 / 1625 处**
   （原来是 576 / 1606；`资料/卡面图标_现状与缺口.md:40` 写 1307/1307、`工具/gen_icon_doc.py:65` 写 270/270、`EVID_IconSetup_1018c.md` 写 1606/1606 —— **四个数**）。
4. **`gen_cardface_fixes.py` 一跑会删掉一条死键**：`subtype` 列里的 `"Bladeguard Veteran": "Infantry"`（该卡 2026-10-17 已改名成 `Bladeguard Lieutenant`，键**永远不会命中**）。
   本批按要求跑了这个脚本 ⇒ **`subtype` 50→49（-1 条）**。**是脚本自己的输出、不是丢数据**，故未手工还原（手工补回去下次跑还会被删）。
5. `cardface_fixes.json` 的 `descZh` 列 **118 → 138**；`desc` 列 **185 不动**；`keywords 230` · `_` 开头的说明段全部原样保留。
6. `工具/gen_icon_plan.py` 的 **`OUT_MD`**（`资料/卡面图标_对照与缺口.md`）在 `main()` 里**根本没被写** —— 死变量，
   那张文档的生成器其实是 `工具/gen_icon_doc.py`（两处注释容易让人以为改了这边就会更新它）。

---

## 七、跑过的命令 + 产物前后差异（条数）

```
python -I -X utf8 Unity/工具/gen_cardface_fixes.py            # 对账/重算 subtype+keywords（幂等：再跑一次仍 22/2）
python -I -X utf8 Unity/工具/gen_cards_engine.py              # 写 cards_engine.json
python -I -X utf8 Unity/工具/gen_cards_engine.py --check      # ⇒ 「一致 ✅」
python -I        Unity/工具/gen_icon_plan.py                  # 干跑（看 锚点/没认出来 报告）
python -I        Unity/工具/gen_icon_plan.py --write          # 写两份 card_icon_plan.json（连跑两次 ⇒ 幂等）
```
⚠️ **必须加 `-X utf8`**：`-I` 会**连带忽略 `PYTHONIOENCODING`**，而这三个脚本都会往 GBK 控制台打 ✅ 之类的字符
⇒ 不带 `-X utf8` 时 `gen_cards_engine.py` 会**在写完文件之后**抛 `UnicodeEncodeError`（exit 1）。**文件是好的，别被退出码骗了。**

| 产物 | 改前 | 改后 |
|---|---|---|
| `card_icon_plan.json`（两份，`cards` 逐字相同） | 576 张 / **1606** 处 | **580 张 / 1625 处**（+19：`团：`×9 · `誓言 N：`×3 · `☀`×2 · `1 点任务`×2 · `2 点任务`×1 · 同步旧池 ×2） |
| `cards_engine.json` | 1126 张 | 1126 张，**只有 20 处 `descZh` 变化**（逐条已核，`desc`/`keywords`/`subtype` 零变化） |
| `cardface_fixes.json` | `desc` 185 · `descZh` 118 | `desc` 185 · **`descZh` 138**（+20）· `subtype` 50（**-1**，见 §六·4） |
| `card_stats.json` | 1213 行 | 1213 行，**23 行 `desc`** 变化（正是 §四那 23 张） |
| 计划表里「没认出来的」/「盘上没素材的」 | 0 / 0 | **0 / 0**（没有新增缺口） |

**`git diff --numstat`（只列我改的）**：`cardface_fixes.json 22/2` · `card_stats.json 23/23` · `gen_icon_plan.py 13/2` ·
两份 `card_icon_plan.json 180/0` · `cards_engine.json 1/1`（它是**压成一行**的 JSON，整文件算一行）。

**🔴 行尾（二进制数 `b'\r\n'` vs `b'\n'`，用文本模式数会恒相等）**：
| 文件 | 改前 | 改后 |
|---|---|---|
| `cardface_fixes.json` | 1479 / 1479（**全 CRLF**） | 1499 / 1499（**仍全 CRLF**） |
| `card_stats.json` | 36003 / 36003（**全 CRLF**） | 36003 / 36003（**仍全 CRLF**） |
| `gen_icon_plan.py` | 0 / 824（**LF**） | 0 / 835（**仍 LF**） |
| `card_icon_plan.json` ×2 | 0 / 16415（**LF**） | 0 / 16595（**仍 LF**） |

⇒ **没有一处翻行尾**（没跑过 `sed -i`，全部走 Edit 工具 / python `'wb'`）。

## 八、没查清 / 停手的地方
1. **(a) 类那 124 张要不要做、用哪种机制** —— **要调度台定**（见 §五·3）；定了我可以接着把它做成一整批。
2. **(c) 那 3 张**（`BL19`/`BL51`/`BL69`）到底是「图标 + 词」还是「只留图标」—— **要读卡图**，本批没读。
3. **`GSC25 Nexos`** 的中文整句缺 `Artifice:` —— 拟补 `巧技：…`，**等口径**（属中文正文订正、不属图标计划）。
4. **没跑 Unity**（红线）：计划表新增的 19 条**没有经过 `IconSetup.Verify` 实跑验收**；改动只做了静态/离线核对。
5. **没做**：`MyGame/Assets` 侧的任何改动（含 `DA44` 的 `[Armor]` —— 按交代归另一位写手）。
