# RECON_卡面族 · 「卡面 / 图标 / 中文」一族的**当前磁盘状态**现核

> **只读现核**（2026-10-08 第三会话）。**没跑 Unity、没动 git、没改任何生产文件**。
> 判据一律**现读当前磁盘上的代码/数据**（不采信任何报告里的旧转述）；凡引报告处都注明「只当线索」。
> 读过的判据源：`MyGame/Assets/CardPresentation/**` · `MyGame/Assets/RuleEngine/**` ·
> `MyGame/Assets/RuleEngine/Resources/cards_engine.json` · `数据/游戏数据/{card_icon_plan,cardface_fixes,card_stats}.json` ·
> `数据/卡牌翻译/zh_cards.json` · `工具/{gen_cards_engine,gen_cardface_fixes,gen_icon_plan,gen_icon_doc}.py` ·
> `工具/_run_8_checks.sh` · `d:/4/_tmp_view/cardface_1018{,b}.log`（探针实跑日志）。
> ⚠️ **卡池实测当前 = 1126 张**（`cards_engine.json` 的 `count`），与已知一致。

---

### A965 —— 状态：✅ **已做**
- 证据：`CardPresentation/Editor/CardFaceProbe.cs:283` `int slot = chr[i].materialReferenceIndex;` · `:284` 越界 `continue`（⛔ 不落回 0 号槽）· `:285` 取 `meshes[slot].vertices`；`:282` 只认 `isVisible`。
  **全 `MyGame/Assets` 里非注释的 `meshInfo[0]` = 0 处**（`grep` 复核；只剩 `:245/:253/:256` 三条注释）。
- 判据：来自 `资料/普查产出_1018/S6_A965卡面探针取槽.md`（**只当线索**）+ 现读代码。
- 要改的落点：无（已收口）。
- 会不会被生成器覆盖：否（`.cs`）。
- 没查清的部分：本笔**回归网半空**（该文件零断言，不在 12 条自检里）⇒「绿红」这条路本来就不存在；`S6` 提的「实跑确认带 `<sprite>` 那层 `meshInfo.Length ≥ 2`」**仍未做**（要跑 Unity）。

---

### A969 —— 状态：❌ **未做**（缺陷**坐实**，根因已定位；范围比账上写的大得多）
- **根因（唯一一处）**：`CardPresentation/Core/CardIcons.cs`
  `:202-205` 先把 `tag` 包成**带 link** 的：`tag = "<link=" + linkId + ">" + "<sprite name=…>" + "</link>"`；
  `:252-253` 裸关键词那支又**再包一次**：
  ```
  string bare = tag + it.token;                                  // :252
  if (!string.IsNullOrEmpty(linkId)) bare = "<link=" + linkId + ">" + bare + "</link>";   // :253
  s = s.Replace(it.token, bare);                                 // :254
  ```
  ⇒ 产物形如 `<link=armour><link=armour><sprite name="armour"></link>护甲 1</link>`。
- **实据交叉核**（不是推演）：`d:/4/_tmp_view/cardface_1018b.log:761` `Heavy Intercessor/keywords … text='<link=armour><link=armou…'`（前 24 字符正是 `:253` 那条拼出来的）。我按 `:184-257` 逐行**离线复算**当前 `card_icon_plan.json` + 当前卡池，`Heavy Intercessor` 的 `descZh` 首段与日志**逐字吻合**。
- **影响面（复算，实测数）**：**1306 处 / 473 张卡**（判据 = 计划表里 token 是**裸词**（首字符是字母/数字、不以 `[` 开头）、非 `SpiritStone`、非「数字烘在图里」、且该图有 link id 的**全部**项）。⇒ **不是「每张带图标卡」，而是「每一次裸关键词替换」**（`Waystone.` / `Rally:` / `路标石。` / `集结：` … 全中）。方括号支（`:257`）与数字支（`:238/:240`）**只有一层 link，是对的**。
- **TMP 链接命中 / tooltip**：静态判 = **无差别** —— 两个 link **同 id** 且**嵌套**，`TmpFont.LinkAt`（`CardView.LinkAt:3451`）走 TMP `FindIntersectingLink`，返回的 id 一致 ⇒ `TipText.ByLink` 拿到的键不变、tooltip 文案不变。⚠️ **未跑 Unity 验收**（红线）。⇒ 它是**冗余标记**，**不是**静默错值；但它是下面 `A980` 四条红的**同一根因**。
- 要改的落点：`CardPresentation/Core/CardIcons.cs` 的 `Rewrite`（**只改 `:252-253` 那一支**）—— 让 `tag` 与其 link 拆开（或裸词支直接用一层的 `spriteTag + token` 再包一次 link），**别动** `:257` 与 `:238/:240`（那三处是对的）。
- 会不会被生成器覆盖：否（`.cs`）。
- 没查清的部分：TMP 在**嵌套同名 link** 下的 `FindIntersectingLink` 返回值（未跑 Unity）；`LinkAt` 是否恰好取到**最内层**（同 id ⇒ 无影响，但没实证）。

---

### A977 —— 状态：⚠️ **部分做**（10 张里 **6 张的中文卡面已经是好的**；**4 张仍缺**；`UM80` 另有文本缺失）
> 关键口径：**卡面恒走中文**（`BattleDriver.FaceTextFull:10577-10592`：`DescZh` 非空⇒中文）。全池**只有 6 张**没有 `descZh`，**这 10 张都有** ⇒ 玩家看到的是 `descZh`，**英文 `desc` 那侧的记号不影响卡面**（但引擎解析读的是 `desc`）。

| 卡 | 现 `desc`（英） | 现 `descZh` | 计划表里有没有那枚 | 中文卡面现状 |
|---|---|---|---|---|
| `SOR24 Seraphim` | `3: Gain +1 Ranged and +1 Health` | `3 ☀：获得 +1 远程攻击和 +1 生命` | ✅ `descZh '☀'→faith` | ✅ **有**（信仰） |
| `SOR23 Retributor` | `1 : Deal 3 damage…` | `1 ☀：…` | ✅ | ✅ 有 |
| `SOR11 Crusader` | `3: Gain +1 Attack and Armour 2` | `3 ☀：…` | ✅ | ✅ 有 |
| `SOR28 Dominion Superior` | `Rally: … 2 : …` | `… 2 ☀：…` | ✅ | ✅ 有 |
| `SOR43 Devout Warriors` | `Draw a troop. 2 : …` | `… 2 ☀：…` | ✅ | ✅ 有 |
| `SOR49 Trial of Suffering` | `… 2 : …` | `… 2 ☀：…` | ✅ | ✅ 有 |
| `UM80 Devastator Marine` | `4: Deal 5 damage to an enemy troop` | `4：对 1 个敌方部队造成 5 点伤害。` | ❌ **无** | ❌ 无 |
| `SOR72 Adelaide the Serene` | `6: Gain Flank and Shield…` | `6：获得侧翼和护盾…` | ❌ 无 | ❌ 无 |
| `UM81 Phobos Librarian` | `3: Deal 2-4 damage…` | `3：…` | ❌ 无 | ❌ 无 |
| `UM_Scout_Sniper Scout Sniper` | `1: Deal 1 damage` | `1：造成 1 点伤害。` | ❌ 无 | ❌ 无 |

- 🔴 **订正 A977 行两处**（现核）：① **不是 10 张全缺** —— `SOR28/43/49`（那 6 张「未核图」里的 3 张）中文里**已经有 `☀` 且有计划表项**，卡面是好的；真正还缺的 = **4 张**。② `UM80` 的「丢了 `Oath` 与 `Blast 2.`」**在卡面上不成立** —— 它的 `keywords` 就是 `['Blast 2','Oath']`，`CardText.KeywordSegment` 会把 `<图>爆裂 2。…誓言…` **补在最前面**（`FaceTextFull:10591`）。**只有英文 `desc` 字符串**里没有它们。
- 判据来源：`资料/普查产出_1018/V-BARE_裸数字全池普查.md:180`（**读成品卡图**，只当线索）+ 现读 4 份数据。
- 要改的落点：**`数据/游戏数据/cardface_fixes.json` 的 `descZh` 列**（手工维护列；既有 **118 条**先例，见 `_manual_descZh_note`）。`UM80` 若要补的是**中文正文**（`4：` 那句要不要写成 `誓言 4：`）⇒ 同列。
- 会不会被生成器覆盖：**否** —— `gen_cards_engine.py:427-477` 读它、`:816-829` **在中文表之后**应用（源码注释明写「必须在中文表写入之后，否则会被 `zh_cards.json` 盖回去」）。⚠️ **但改完必须重跑**：`python d:/4/Unity/工具/gen_cards_engine.py`（写 `cards_engine.json`；`--check` 只对账不写，`gen_cards_engine.py:979-988`）。
- 没查清的部分：**`SOR72` / `UM81` / `UM_Scout_Sniper` 那三张的卡面那枚到底是不是信仰 ☀**（`V-BARE` 抽读的是另外 4 张；这 3 张要读 `d:/2/Warpforge部队卡片/Sorotitas|Ultramarines/…` 的图）；`UM80` 的 `4:` 语义归 `A975` 族（已搬 `资料/历史/A表已收口_1018.md:19`）⇒ ⛔ 别在本条里顺手改。

---

### A978 —— 状态：✅ **已做 / 不构成缺陷**
- 证据 1（解析层）：`RuleEngine/Core/GivePayload.cs:203` 属性正则含 `might|fist|strength|weapon`；`:220` 同表；`NormAttr`（`:250-260`，注释即判据）`might/fist/strength → attack`、`weapon → ranged`；`:335` 匹配前先 `Replace("[","").Replace("]","")` ⇒ `+1 [Might]` 能命中。
- 证据 2（卡面图标层）：`数据/游戏数据/card_icon_plan.json` 里这 9 处**全部已在表**：`EC28 '[Might]'→Melee` · `SOR55 '[Might]'×2→Melee` · `GSC51 '[fist]'→Melee` · `GOF_Ferocious_Rage_Beastboss_Talent '[fist]'→Melee` · `SW63 '[fist]'→Melee` · `GSC43 '[Strength]'→Melee` · `UM87 '[strength]'→Melee` · `GOF16 '[weapon]'→**Ranged**`（与铁律 7 一致）。
- 9 处在哪几张卡（现读卡池，逐一）＝上列 8 张卡 / 9 处（`SOR55` 一处两句）。
- 要改的落点：**无**（若要做「规范化成属性词」的数据卫生，那是**新账**：必须**同时**改 `gen_icon_plan.py` 的表 + 卡文本，否则 `[Might]` 那条 token 匹配不到 ⇒ **图标丢**）。⛔ 不属本族缺账。
- 会不会被生成器覆盖：改卡文本会被覆盖（`cardface_fixes.json` 才是落点）；现况**不需要改**。
- 没查清的部分：无。

---

### A979 / A981 —— 状态：❌ **未做**（13 张的中文一条都没订正）
- **现读这 13 张的当前值**（卡池 `descZh` 与 `zh_cards.json` 的 `d` **逐字相同**，两者同步）：
  `EC1`「激励：给予一个随机友方部队 +1。…」裸 · `EC16`「残忍：获得 +1 和侧翼。」裸 · `EC58`「你的战将本回合获得 +1。…」裸 · `EC77`「给予一个友方部队 +2。」裸 · `GSC22`「…给予你的部队 +1 和 +1 生命。」裸 · `SW26`「…本回合你的单位获得 +1。…」裸 · `TAU72`「…本回合获得 +3」裸 · `TAU75`「给予 1 个友方单位本回合 +3…」裸 · `TL86`「给予所有友方部队 +4」裸 · `UM_Angel_s_Wrath`「给予 1 个友方单位 +1，…」裸 · `UM_Angels_of_Death`「…给予你的原铸仲裁者 +1。」裸 · `UM_Assault_Doctrine`「给予 1 个友方单位 +3。…」裸 · 🔴 `DA86`「给予一个敌方部队脆弱 4 与"反噬：你的对手获得 2 个**骷髅头**"」**写错**（卡面是锯齿环=任务点）。
  （英文侧**这 12 张的 `desc` 已经补好了**：`+1 Attack` / `+3 Attack` / `+1 Ranged` …；只有 `DA86` 英文已是 `2 Quest Points`。）
- 判据：13 张的「裸 / 写错」= `V-BARE` §3·3 / §7.5（**只当线索**）；**当前值**=现读 `cards_engine.json`。
- 要改的落点：**`数据/游戏数据/cardface_fixes.json` 的 `descZh` 列**。
  🔴 **A981 行写的落点（`zh_cards.json`）与既有实践不一致**：① `zh_cards.json` 自己的 `note` 写「**不要手改**」；② 该文件的 `descZh` **正是 `cardface_fixes.json` 的 `descZh` 列**（118 条既有条目，`_manual_descZh_note` 明说该列手工维护、`gen_cardface_fixes.py:179` 原样保留）—— `A6/A7/PnP` 三批的 `descZh` 修正**全走这一列**。⇒ **建议按既有实践改 `cardface_fixes.json`**（两份文档打架，**要调度台先定口径**）。
- 会不会被生成器覆盖：**否**（应用顺序在中文表之后）；**但必须重跑** `python d:/4/Unity/工具/gen_cards_engine.py`，否则卡池 `cards_engine.json` 不变、**玩家看不到**（`FaceTextFull` 印的是它）。
- 没查清的部分：12 张裸处**每张到底是近战还是远程**——`V-BARE` 只逐张读过卡图（18 拳 / 1 枪 / 2 任务点），其中 **`UM_Angels_of_Death` 是唯一一枚紫枪**；本条**没有自己再读图**（判据现成，先拿来用；若要复核走 `d:/2/Warpforge部队卡片/…`）。

---

### A982 —— 状态：❌ **未做**（抄录层没回改）
- 现读 `数据/游戏数据/card_stats.json`（1213 行）：`DA51 The Rock` = `Choose one: Gain 2; deploy a Sergeant Taaman; heal 3 to all your units`（裸）· 同族 `DA86` = `…opponent gains 2…`（裸）· `DA11` = `Slay: Gain 1`（裸）· `UM80` = `4: Deal 5 damage to an enemy troop`。卡池那侧已由 `cardface_fixes.json` 的 `desc` 列修好（`Gain 2 Quest Points` 等，逐条已核）。
- **这个文件还在被谁读**（全仓 `grep`，逐条核过）：
  `工具/gen_cards_engine.py:28`（`SRC`，**主源**）· `工具/gen_cardface_fixes.py:28`（`OLD`，**对账面**）· `工具/gen_tutorial_stages.py:67`（只读 `ocrName` 列）· `工具/zh_crosscheck.py:441`（只数行）· `工具/check_prebuilt_decks.py:345`（只出现在一句提示文本里）· `工具/import_original_art.py:199`（注释）。
  🔴 **`MyGame/Assets` 里的 9 处命中全是注释**（`CardDef.cs:3` · `CardDatabase.cs:6` · `RuleEngineTest.cs:1865/1895/1992/7283/7788/16662` …）⇒ **没有任何运行期读者**。
- 要改的落点：`数据/游戏数据/card_stats.json` 里那 3~4 行的 `desc` 列（**抄录层**）。
- 会不会被生成器覆盖：**不会**（它是 `SRC`，是**输入**）。⚠️ **但有一个真副作用**：`gen_cardface_fixes.py:27` 明写「算修正**永远和 `card_stats.json` 比**，本脚本才是幂等的」⇒ 订正后**再跑该脚本**，那几条 `desc` 修正会**从 fix 表里消失**（这是它设计上的幂等行为，不是丢东西）。⇒ 改的时候**两个生成器要同日跑一次并 `git diff` 核 fix 表没被连带删掉别的条目**。
- 没查清的部分：`UM80` 那行**要不要连 `Blast 2.`/`Oath` 一起回改**（那是**文本缺失**、不是裸数字族）—— 判据不足，**建议与 `A977` 的 `UM80` 合成一条**再动。

---

### A980 —— 状态：❌ **未做**（5 条红**逐条给出真实差异**；**不是「都是一行」**）
宿主：`CardPresentation/Editor/IconSetup.cs` 的 `Verify()`（`:224` 起）。**不在 12 条自检里**（`工具/_run_8_checks.sh` 的 12 个 `run` 里没有 `IconSetup`）⇒ 天然漏检。

| 标签 | 行 | 它期望什么 | 现在实际是什么 | 根因 |
|---|---|---|---|---|
| **②c** | `:263-265` | `Rewrite("DA44","desc","…+3 [Attack], +3 [Armor] or…")` 里出现 `<sprite name="Ranged">` | `… [Attack]` 换了、**`[Armor]` 原样留着**（计划表 `DA44` 只有 `'[Attack]'→Melee`；`[Armor]` 在**整张计划表里已零 token**，只剩 `gen_icon_plan.py:187` 的死条目 + 一条 `why` 文本） | 🔴 **夹具串过时**，**与 `<link>` 无关** —— 卡池真实 `DA44.desc` 今天是 `Give +3 [Attack], +3 Ranged or +3 Health to a friendly troop` |
| **②e** | `:275-276` | 结果含 `"<sprite name=\"remnant\">残骸。"` | `<link=remnant><link=remnant><sprite name="remnant"></link>残骸</link>。装甲 2。` | **A969**（`</link>` 夹在中间） |
| **②f** | `:279-281` | 结果含 `"<sprite name=\"Melee\">,"` | `<link=melee><sprite name="Melee"></link>,` | **A969** |
| **②f2** | `:286-287` | 结果**全等** `"<sprite name=\"faith\">：部署一个额外的战斗修女"` | `<link=faith><sprite name="faith"></link>：部署…` | **A969**（⚠️ 同标签的**第二条** `:289-291` 是**绿的**，别一起改坏） |
| **②f3** | `:296-298` | 结果**全等** `"Gain <sprite name=\"questPoints1\">"` | `Gain <link=questpoints><sprite name="questPoints1"></link>` | **A969** |

⇒ **修法**：4 条是**改断言**（把 `</link>` 算进去，或照 `:322-334` 的 **②i** 那套写法：`CountOf(<sprite …>)` + `StripTags(...).Contains(词)` 两条拆开 —— 那个写法**正是为了躲同一个坑**才写的，照着抄即可）；**②c 是改夹具**（换成卡池真实的 `DA44.Desc`）。⇒ 合计 **5 处**，不是 1 处。
- 同族「三个数打架」的现状（判据 = 现算）：
  · `工具/gen_icon_doc.py:65` —— **硬编码字符串**「`IconSetup.Verify` 报 **270/270**」（它是 `资料/卡面图标_对照与缺口.md` 的**生成器**，改它 + 重跑才算改到源头）。
  · `资料/卡面图标_现状与缺口.md:40` —— 手写「**1307/1307**」（⚠️ 同一张表 `:41` 的「全池 **1369 处 / 572 张**」也过时）。
  · **当前真值 = 1606 / 1606**：`Verify` 的数就是计划表里 `"sprite":` 的出现数 = **1606 处 / 576 张**（实测）。名字**全查得到** —— 61 个不同 sprite 名里 `Cruelty`/`Ecstasy` 与 asset 里的 `cruelty`/`ecstasy` **大小写不同**，但 `GetSpriteIndexFromName` 走 `TMP_TextUtilities.GetHashCode`，**大小写不敏感**（`TMP_SpriteAsset.cs:235-243` + `TMP_TextUtilities.cs:2237-2248`，本机 `D:/Unity/Hub/Editor/6000.3.23f1/…`）⇒ 不报缺。
- 要改的落点：`CardPresentation/Editor/IconSetup.cs` 的 `Verify()`（5 处）＋ `工具/gen_icon_doc.py:65`（1 处，+ 重跑）＋ `资料/卡面图标_现状与缺口.md:40-41`（手写）。
- 会不会被生成器覆盖：`IconSetup.cs` 否；`gen_icon_doc.py:65` 是**源头**（改它 + 重跑才生效）；`现状与缺口.md` 是手写文件、**不会被覆盖**。
- 没查清的部分：**没跑 Unity**（5 条红是**静态逐条判**，由 `资料/普查产出_1018/*` 那次实跑的 24 OK / 5 !! 佐证）；`Verify` 里 ②i/②j 那 9 条的当前绿红**没重算**。

---

## ⛔ 不建议派 / 判据不足（缺什么）

1. **`A977` 剩下那 3 张（`SOR72` / `UM81` / `UM_Scout_Sniper`）** —— 缺「卡面那枚是不是信仰 ☀」的**读图判据**。要派就**只派读图**（读 `d:/2/Warpforge部队卡片/Sorotitas|Ultramarines/…` 对应 PNG），**别顺手改数据**。
2. **`UM80` 的 `4:`** —— 归 `A975`（Oath 族，已搬 `资料/历史/A表已收口_1018.md:19`）。⛔ **不许在 `A977`/`A982` 里单开一脚**。
3. **`A981` 的落点选择（`zh_cards.json` vs `cardface_fixes.json`）** —— **两份文档打架**（A 表行 vs `_manual_descZh_note` 的既有 118 条实践）。⇒ **要调度台先定口径再派**，否则两个写手会改两处。
4. **`A982` 要不要回改抄录层** —— 现核结论是「**安全、但会动 `gen_cardface_fixes.py` 的下一次输出**」；若一并回改 `UM80` 的文本缺失，**先定 `UM80` 的判据**（同第 2 条）。
5. **「中文属性词要不要出图标」（顺手发现 1）** —— **没有判据**：原版没有中文客户端，`descZh` 是我们自译的；要做就得**先定口径**（这条族里 27 张卡今天中文面一枚记号都不画）。
6. **`CollectionScene.cs` 的 `GlyphVertsOf`（顺手发现 2）** —— 前置是「先读出它被谁调用、那处会不会出现带 `<sprite>` 的 TMP」（**没查**），且改它要跑 Unity。

## 顺手发现（都没改）

1. 🔴 **27 张卡的图标计划【只有 `desc` 字段、没有 `descZh`】**（判据 = 计划表字段集合 `== {'desc'}` **且**卡池里这张有 `descZh`；**这 27 张全有**）⇒ **卡面恒走中文**，所以**这 27 张的中文卡面一枚记号都不画**。id 清单：`AM14 AM9 AM12 AM33 AM30 BL19 BL51 DA25 DA44 DA6 DA50 DA60 DA78 EC26 GSC25 GSC51 GSC55 SAU42 SOR13 SW68 SW62 SW63 SW49 UM_Avenging_Zeal UM_Master_of_Arms UM_Righteous_Fury BL69`。例：`DA44` 的粉拳+紫枪、`SW62`「+4 近战攻击、+4 远程攻击」、`SW63`「+1 攻击」、`UM_Righteous_Fury`「+1 攻击」。
2. 🔴 **`CollectionScene.cs:310-314` 的理由句与 `A965` 当天推翻的是同一句** —— `GlyphVertsOf` 只收 `materialReferenceIndex == 0`，注释写「本工程的卡面标签只有一个字体资产 ⇒ 实际恒为 0」；而带 `<sprite>` 的 TMP **今天就有第二个材质槽**（`A965` 的判据）。⇒ 带图标的字会被**跳过**。**没查**调用点、**没跑 Unity**。
3. `工具/gen_icon_plan.py:186-187` 的 `("DA44","[Attack]")` 是活的、`("DA44","[Armor]")` 是**死条目**（卡里已无该 token；脚本 `:798-802` 自己会打「死条目」报告）—— 与 `A980 ②c` 是同一件事。
4. `UM_Phobos_Lieutenant`：计划表 `desc` 有 `'[eye icon]'→stealth`，`descZh` **没有**（中文写「获得潜行」）⇒ 中文卡面没有那枚图标（同顺手发现 1 那一族）。
5. `cardface_fixes.json` 当前列尺寸（现读）：`desc` 185 · `descZh` 118 · `keywords` 230 · `subtype` 51（+ `_manual_*` 若干）—— `A965` 记忆里写的「条目 185 条」指的是 `desc` 那一列。
