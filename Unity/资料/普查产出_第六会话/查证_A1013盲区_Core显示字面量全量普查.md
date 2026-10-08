# 查证 · `A1013-盲区` —— `Core/` 显示字面量全量普查（只读代理交件 · 第六会话）

> ⚠️ **原件说明**：本条由一个**只读代理**产出（其工具表无 Write）⇒ **它自己没能落盘**，
> 下面全文由**调度台**原样转存（只补了标题与这一行说明）。**内容一字未改。**
> **行号口径（代理原话）**：下表行号 = **它现读那一刻**（2026-10-08 本次会话）。已记 md5 供复核：
> `Core/CardText.cs` = `d51d9b184b630c227a96b28ce0fb264d` · `Core/Tooltip.cs` = `017f75ec2ff7b8bdf6c4049d4b44f5c5` ·
> `Core/CardView.cs` = `6b1d587e3b7fb1959f22d687a3d0f767`。
> 🔴 **实测已漂**：`Battle/BattleDriver.cs` 的 `_resultLabel.SetText(...)` 在它两次读之间从 `:13029` 漂到 `:13035`
> ⇒ 有人在改 `Battle/`。**`Core/` 这三件本轮未漂**。

---

## ① 总账

**扫了什么**：`Core/` 下 **42 个 `.cs`**（`ls *.cs | wc -l` = 42；另有 42 个 `.meta`，不计）。

| 分组 | 文件数 | 中文字面量（代码位，剥注释后） |
|---|---|---|
| `Core/Loc.cs` = 语言表**本体** | 1 | **416 行**（= 表内容；`new Entry(` **408** 条，其中 `MainMenu/PurchasePremium/Description` 的值跨 3 行 `:1051-1055`）⇒ **不是「待键的显示字面量」，不在普查范围** |
| `Core/FilterPanelModel.cs` = 已收口 | 1 | **0 处**（已复核：剥注释后 `"[^"]*\p{Han}` 零命中 ⇒ 表 C 那五族确实全接完了） |
| **玩家可见（①）** | **3** | **169 处** |
| **零玩家可见（②）** | **37** | 406 行（全是 `Debug.Log*` / `Watch.Mark` / 自检串 / `detail` 读数 / 诊断串） |
| 合计 | 42 | 993 行 |

🔴 **关键结论：`A1013` 点名的「其余约 37 个文件」正好 = 那个 37 —— 这 37 个文件里玩家可见的中文字面量是【0 处】。**
（42 − `Loc.cs` − `FilterPanelModel.cs` − `CardText.cs` − `Tooltip.cs` − `CardView.cs` = **37**。）

各判定档的处数（169 处的归属）：

| 档 | 处数 | 在哪 |
|---|---|---|
| **有键但没接** | **2** | `CardView.cs:1804`（1）· `CardText.cs` 的 `Phrases` 转发表那 4 条里 **1** 条真接上（见 ③） |
| **已有键、已接（只差把兜底表删掉）** | **5** | `CardText.Phrases` 里 `END TURN`/`YOUR TURN`/`ENEMY TURN`/`DRAW` + `PhraseTerms` 那 6 条转发的消费侧 |
| **表里没键** | **≈162**（含 4 条**死条目**） | `CardText.cs` 155 + `Tooltip.cs` 13 − 已有键那几条 |
| **不是玩家可见** | **406 行 / 37 文件** | 见 ② |

---

## ② 逐文件表

### 2.1 玩家可见的三个文件（① 类）

| 文件 | ①处数 | ②处数 | 判定摘要 |
|---|---|---|---|
| `Core/CardText.cs` | **157**（含 2 处不上屏但**改变卡面内容**） | 0 | 卡面/HUD 文案的**一整张中文表**（卡名/关键词/阵营/短语/效果小字），**一条都没进 `Loc`** |
| `Core/Tooltip.cs` | **13** | 9 | `TipText.*` 的 10 条数值/HUD 悬停说明 + 3 条我们自己加的括注 |
| `Core/CardView.cs` | **1** | 86 | `:1804` `CreatedByLine` 那行卡面字**唯一**一处 |

### 2.2 零玩家可见的 37 个文件（② 类，逐文件行数）

> 「②行数」= 剥掉注释后、字符在**字符串字面量里**的中文行数（含跨行拼接的续行）。
> 每一行它都**逐类追到了它的出口**：`Debug.Log/LogWarning/LogError` · `Watch.Mark`（写 `watch.log` 文件）·
> `LogError/LoadError/LastError/LastSkip`（诊断字段，只被日志读）· `SetHint`-无关的 `detail`/`Describe()`/
> `ProvenanceReport()`/`SourceOf()`（自检读数）· `Shot("01_接上")` 之类（**截图文件名**，不上屏）。

| 文件 | ②行数 | 文件 | ②行数 |
|---|---|---|---|
| `BattleAutoDrive.cs` | 85 | `UnitTweenTable.cs` | 5 |
| `CardFeel.cs` | 83 | `VoiceLines.cs` | 5 |
| `CardView.cs` | 86 | `Watch.cs` | 5 |
| `PlayerBoot.cs` | 29 | `BlobShadow.cs` | 3 |
| `CardArt.cs` | 10 | `CardbackTable.cs` | 3 |
| `ClickLog.cs` | 10 | `EnvBlendables.cs` | 3 |
| `UnitTweenRuntime.cs` | 8 | `TraitParticles.cs` | 3 |
| `CardIcons.cs` | 7 | `EnvironmentConditions.cs` | 2 |
| `EventTiming.cs` | 7 | `LayoutSpace.cs` | 2 |
| `OffensiveCards.cs` | 7 | `PragatiDigits.cs` | 2 |
| `TraitFrames.cs` | 7 | `UguiRect.cs` | 2 |
| `DraggableController.cs` | 6 | `CardEffects.cs` | 1 |
| `RelatedCards.cs` | 6 | `CostCurveDrawer.cs` | 1 |
| `TmpFont.cs` | 6 | **零中文（代码位 0 行）** | 8 |
| `WarpforgeAudio.cs` | 6 | ↓ | |
| `Tooltip.cs` | 9 | `AutoCardRotation.cs` · `Badges.cs` · `CardButtons.cs` · `CardFan.cs` · `CardHighlight.cs` · `CardTween.cs` · `CardWinBox.cs` · `PragatiDigits.Data.cs` · `TextCanvas.cs` · `VfxMap.cs` | 0 |

> ⚠️ **「不是玩家可见」的四个「要留神但已排除」的点**（都追到了出口，不是猜的）：
> - `TmpFont.cs:70/:84` `MeasureGlyph('国', "汉字")` / `MeasureGlyph('M', "拉丁大写")` —— 第二个实参只进 `:296` 的 `Debug.LogWarning`。
> - `LayoutSpace.cs:138-139` `Describe()` —— 全仓消费点 = `Core/BattleAutoDrive.cs:330` 的 `Debug.Log`。
> - `CostCurveDrawer.cs:117` `"(空)"` —— 注释自陈「自检用的读数」。
> - `CardArt.cs:755-757` / `CardFeel.cs:521` / `CardView.cs:1050-1052,4491-4555` —— `Describe()` / `ProvenanceReport()` / `OutlineDebug` / `CheckCutout*` 的 `detail`，消费点全在 `Editor/*` 的自检 `Debug.Log`。

---

## ③ ① 类【逐文件 × 逐处】表（169 处，这是写手的活）

### 3.1 `Core/CardText.cs`（157 处）

| # | 文件:行号 | 现在的中文字面量（截前 30 字） | 判定 | 证据（它进哪个上屏调用） |
|---|---|---|---|---|
| 1 | `CardText.cs:46-58, 61-73` | `Names` 表 **26 条**：余烬督军 / 拾荒者 / 壁垒 / 猎鹰 / 余烬弓手 / 老兵 / 铁甲兵 / 长弓手 / 焰唤者 / 影刃 / 攻城槌 / 战龙 / 熔岩巨像 / 潮汐督军 / 潮汐仆从 / 踏浪者 / 珊瑚卫 / 海妖 / 珊瑚弓手 / 贝壳兽 / 弩炮 / 深渊猎手 / 铁壳 / 风暴祭司 / 利维坦 / 深渊泰坦 | **原版无，需自拟** | `Name(id)` `:519` / `Name(id,nameZh)` `:535` → `CardView` 卡面名 + `DeckRuntime:2772 _deckRowName[i].SetText(...)` + `DeckEditorState:223` 卡组名 |
| 2 | `CardText.cs:82-93` | `KeywordNames` **12 条**：先锋/飞行/潜行/远射/护甲/护盾/集结/猛击/斩杀/反噬/忏悔/技能 | **表里没键**（键族=`Card_Trait/<枚举名>`，本地无值 → 见 ④） | `Keyword(norm)` `:545` → `KeywordSegment` `:253` → 卡面那一段 |
| 3 | `CardText.cs:112-173` | `KeywordZhNames` **62 条**：技能/议程/伏击/护甲/巧技/反噬/爆裂/失明/嗜血/伪装/无法攻击/典籍/伴生/震荡/残忍/黑暗契约/毁灭者/职责/狂喜/临时/信仰/迅捷/狂暴/侧翼/飞行/猎杀标记/无敌/远射/标记光/群体/誓言/兽群/忏悔/压制/祈祷/任务/集结/再生/团/残骸/破坏/哨戒/护盾/星镖/斩杀/狙击/灵魂石/潜行/激励/践踏/猛击/眩晕/虫群/突触/天赋/传送/潮涌/不稳定/起义/先锋/脆弱/路标石 | **表里没键**（同上；`Card_Trait/` 裸前缀在表②，仅 10 条特例有值） | `KeywordZh()` `:177` → `KeywordSegment()` `:264` → **卡面关键词段** |
| 4 | `CardText.cs:207, 213` | `KeywordZhAliases`：`装甲` / `爆破` | ⚠️ **不上屏、但改变卡面内容**（去重判定） | `AlreadyInHay()` `:216-223` —— 只被 `KeywordSegment:274` 读来「判已经印过没有」。**⛔ 别搬进 Loc**：它比对的是「卡面已印的中文字」，搬走会**补出重复一段**（`:208-213` 记着 5 张卡的实际翻车） |
| 5 | `CardText.cs:304-310` | 效果小字 7 条：`伤害`/`治疗`/`抽牌`/`自身`/`己方战将`/`敌方战将`/`敌方单位` | **原版无，需自拟** | `Effect()` `:554-564` → 卡面效果小字 |
| 6 | `CardText.cs:315-323` | 整句构件 9 条：`对`/`造成`/`点伤害`/`为`/`回复`/`点生命`/`抽`/`张牌`/`可选目标` | **原版无，需自拟**（⚠️ 是**碎片**，ZH/EN 语序不同 ⇒ 建键要连语序一起定，或改整句键） | `EffectSentence()` `:595-609` → `Battle/SkillPanel.cs:341 _desc.SetText(...)` = 技能卡面板 |
| 7 | `CardText.cs:343-345` | `END TURN`→结束回合 / `YOUR TURN`→你的回合 / `ENEMY TURN`→对手回合 | ✅ **已有键**（`PhraseTerms:408-410` → `Battle/HUD/{EndTurn,YourTurn,EnemyTurn}`）⇒ 这三行**已降级成兜底**（`:336-342` 自陈） | `Phrase("END TURN")` → `BattleDriver:11576/13016 _endTurnLabel.SetText` · `:12908 _turnLabel.SetText` |
| 8 | `CardText.cs:356` | `GAME OVER` → 对局结束 | **表里没键**（两表都无） | `BattleDriver:12902 who = Ctx.IsOver ? "GAME OVER"` → `:12908 _turnLabel.SetText(...)` |
| 9 | `CardText.cs:357` | `YOU WIN` → 你赢了 | **表里没键**（两表都无）——近邻 `Battle/BattleEnd/Victory` **已在表**，可否复用**待裁** | `BattleDriver:13027 r = ... ? "YOU WIN" : "YOU LOSE"` → `:13035 _resultLabel.SetText(CardText.Phrase(r))` |
| 10 | `CardText.cs:358` | `YOU LOSE` → 你输了 | 同上（近邻 `Battle/BattleEnd/Defeat` 已在表） | 同上 |
| 11 | `CardText.cs:359` | `DRAW` → 平局 | ✅ **已有键**（`PhraseTerms:402` → `Battle/BattleEnd/Draw`，表①有） | 同上 + `Shell/BattleLogData.cs:107` |
| 12 | `CardText.cs:360-363` | `HAND`→手牌 / `DECK`→牌组 / `DISC`→弃牌 / `HP`→生命 | 🔴 **表里没键 + 全仓【零消费点】⇒ 死条目**（`rg '"HAND"\|"DECK"\|"DISC"\|"HP"'` 全仓只剩这 4 行定义本身）。近邻 `Battle/HUD/{CardsInHand,CardsLeft}` **已在表** | 无（grep 实证） |
| 13 | `CardText.cs:364` | `CHOOSE ACTION` → 选择行动 | **表里没键**（两表都无） | `BattleDriver:7336 selector.Show(opts, CardText.Phrase("CHOOSE ACTION"), …)` |
| 14 | `CardText.cs:365` | `PICK A TARGET` → 选择目标 | **表里没键**（两表都无） | `BattleDriver:7488` → `SetHint(...)` → `:7832 void SetHint` → HUD 提示行 |
| 15 | `CardText.cs:366` | `NO LEGAL TARGET` → 没有合法目标 | **表里没键**（两表都无；近邻 `Battle/Tips/NoTargetAvailable` 已在表但语义是「战术卡没有合法目标」）**待裁** | 同上 |
| 16 | `CardText.cs:367-368` | `MELEE`→近战 / `RANGED`→远程 | **表里没键**；近邻 `Battle/Tips/{MeleeAttack,RangeAttack}` **已在表**（值是「近战攻击/远程攻击」= 数值格标签，不是打法名）→ **语义待裁** | `BattleDriver:7487 Phrase(what)`（`what ∈ {ABILITY,RANGED,MELEE}`） |
| 17 | `CardText.cs:375` | `ABILITY` → 技能 | **表里没键**（`:369-374` 自陈「原版 `Battle/Tips/*` 那 20 条里没有这一档」） | 同上 |
| 18 | `CardText.cs:384` | `THIS UNIT ALREADY ACTED` → 这个单位已经行动过了 | 🔴 **键已在表**（`Battle/Tips/UnitNotReady`）；`:376-381` 自陈「**它今天已经没有消费点了**」⇒ **死条目** | `BattleDriver` 已改走 `Loc.T`（见 `CardText:379 BattleDriver.UnitNotReadyTerm`） |
| 19 | `CardText.cs:385-386` | `STUNNED`→眩晕中 / `THIS UNIT CANNOT ACT`→这个单位无法行动 | **表里没键**（`:382-383` 自陈「仍然没有原版键」） | `BattleDriver:7273/7289/7312 SetHint(CardText.Phrase(...))` |
| 20 | `CardText.cs:446` | `TurnLabel` = `"第 " + n + " 回合"` | **表里没键**（`:442-443` 自陈「`Battle/HUD/*` 那 8 条里没有」） | `BattleDriver:12908 _turnLabel.SetText(CardText.TurnLabel(Ctx.Turn) + …)` |
| 21 | `CardText.cs:452-453, 460-473` | `FactionNames` **16 条**：余烬/潮汐/**中立**/极限战士/高夫兽人/赛姆汉灵族/索泰克死灵/黑色军团/利维坦泰伦/钛帝国/战斗修女/基因窃取者教派/星界军/暗黑天使/帝皇之子/太空野狼 | 14 条 **原版有键族 `Armies/<Faction>`（表① 全在）**、`Neutral` 与 `余烬`/`潮汐` **原版无，需自拟**（详见 ④） | `Faction(key)` `:505` → 卡面阵营行 + `DeckEditorState:405` 搜索 |
| 22 | `CardText.cs:297` | `string.Join(zh ? "。" : ". ")` 的 `"。"` | **不是要建的键**（句读符，跟语档走） | `KeywordSegment` 返回串 → 卡面 |
| 23 | `CardText.cs:564` | `s + "·" + t` 的 `"·"` | **不是要建的键**（分隔符） | `Effect()` → 卡面 |

### 3.2 `Core/Tooltip.cs`（13 处）

| # | 文件:行号 | 现在的中文字面量 | 判定 | 证据 |
|---|---|---|---|---|
| 24 | `Tooltip.cs:373` | `近战攻击力（规则书 :78）` | ⚠️ **原版键名可从表②照抄 = `Tips/MeleeAttackTip`**（表①无）；`Loc.cs` **里没有** ⇒ **表里没键** | `BattleDriver:13331 Tooltip.Show(TipText.Melee, …)` · `DeckRuntime:711` 同 |
| 25 | `Tooltip.cs:374` | `远程攻击力（规则书 :78）` | ⚠️ 同上，键名 = `Tips/RangedAttackTip`（表②） | `BattleDriver:13332` · `DeckRuntime:712` |
| 26 | `Tooltip.cs:375` | `受任何来源的伤害都减这么多，最低减到 1（:167）` | **表里没键**（搜两表：`ArmourTip\|ArmourTooltip` 0 命中）→ **原版无，需自拟** | `BattleDriver:13333?`（Armour 那条在同族 `Tooltip.Show`） |
| 27 | `Tooltip.cs:376` | `扣完护甲后扣生命，归零进弃牌堆（:147）` | **表里没键** → 需自拟 | `BattleDriver:13333 Tooltip.Show(TipText.Health, …)` · `DeckRuntime:713` |
| 28 | `Tooltip.cs:377` | `打出去要花的能量（规则书 :78）` | **表里没键** → 需自拟（`Battle/Tips/EnergyCost` 是「能量费用」标签，语义不同） | `BattleDriver:13334` · `DeckRuntime:714` · `Editor/DeckScene.cs:4599` |
| 29 | `Tooltip.cs:380` | `每回合恢复，用来打出手牌` | **表里没键**（两表无）→ 需自拟 | `BattleDriver:13281 HitTip(_myEnergyPlate, wp, TipText.Energy)` |
| 30 | `Tooltip.cs:381` | `本局拿到的战功骷髅数` | **表里没键**（`Ranked/Skulls` 前缀不同）→ 需自拟 | `BattleDriver:13280 HitTip(_skullIcon, …)` |
| 31 | `Tooltip.cs:382` | `暗黑天使的任务点进度（0/3）` | ⚠️ **表②有 `CardTraitDescription/questPoints`**（键族同名，语义待核） | `BattleDriver:13282 HitTip(_myQuestIcon, …)` |
| 32 | `Tooltip.cs:383` | `战斗修女的阵营资源` | ⚠️ **表②有 `CardTraitDescription/faith`** | `BattleDriver:13283 HitTip(_myFaithIcon, …)` |
| 33 | `Tooltip.cs:384` | `灵族的阵营资源；在场也算单位（1 血），点击收集` | ⚠️ **表②有 `CardTraitDescription/spiritStone`** | `BattleDriver:13284 HitTip(_myStoneIcon, …)` |
| 34 | `Tooltip.cs:478` | `title += "（" + en + "）"` | **纯括注、不需建键**（跟 `en` 拼） | `Trait(key)` `:468` 的返回值 → 关键词 tooltip 面板 |
| 35 | `Tooltip.cs:493` | `（规则书里没有这个词的条目）` | **表里没键 → 需自拟**（`:463-465` 自陈「原版一样是图标+标题+正文，查不到描述就没有正文」⇒ 这句是**我们自己加的**） | 同上 |
| 36 | `Tooltip.cs:496` | `（规则书 :" + t.line + "）` | **表里没键 → 需自拟**（⚠️ 这是**我们自己的判据行号**，原版没有） | 同上 |

### 3.3 `Core/CardView.cs`（1 处）

| # | 文件:行号 | 现在的中文字面量 | 判定 | 证据 |
|---|---|---|---|---|
| 37 | `CardView.cs:1804` | `$"由 {creatorName} 创建"` | ✅🔴 **已有键、有键但没接** —— 键 = **`Battle/HUD/CreatedBy`**，**`Core/Loc.cs:813` 已在表**（值逐字 = `由 {0} 创建` / `Created by {0}`） | `CreatedByLine()` `:1800` → `FillCreatedBy()` `:1827` → 卡面那行字。**判据链**：`:1792-1799` 自陈原版 = `GetTranslation("Battle/HUD/CreatedBy").Replace("{0}", 名字)`；表②有 `Battle/HUD/CreatedBy`；`Loc.cs:805-813` 那块注释**自己写着**「那一处在别的笔的白名单里」⇒ 现在可以收了 |

---

## ④ 原版两张表的查询结果（**两张都搜了**）

**搜法（照表 C 的口径）**
- **表①** `d:/2/新解包资源/assets_full/**/MonoBehaviour/*.json` 的 `mTerm`：全库 distinct = **488 条**（本轮**全量 dump 出来逐条看过**，不是抽样）。搜过的词：`Created|Hand|Deck|Disc|Health|GameOver|Win|Lose|Draw|Turn|Melee|Range|Abilit|Stun|Target|HP|Damage|Tip|Card_Race|Card_Rarity|Card_Trait|Armies|Battle/`。
- **表②** `d:/2/tools/il2cpp_out/stringliteral.json`（**26,507** 条）：搜过的词：`Card_Trait/` · `CardTraitDescription/` · `Card_Race/` · `Card_Rarity/` · `Tips/` · `Battle/HUD/` · `Battle/Tips/` · `Trait` · `Keyword` · `Mind` · `Tip`（不分大小写全表扫）· 裸前缀正则 `^"[A-Za-z][A-Za-z0-9_ ]{0,40}/"`（**列出全部 37 个裸前缀键族**）。
- **旁证**：`d:/2/tools/all_strings.txt`（地址+串）· `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`（枚举名）· `d:/2/tools/decomp_full/`（`GameStaticData__TraitNameToString.c`）。

**逐族结果**

| 我们的处 | 原版键 | 在哪张表 | 结论 |
|---|---|---|---|
| `CardView:1804` 由 X 创建 | **`Battle/HUD/CreatedBy`** | **表②**（表①无） | ✅ 原版有；**且 `Loc.cs:813` 已收** ⇒ 0 建键 |
| `CardText` `Phrases` 的 `DRAW`/`END TURN`/`YOUR TURN`/`ENEMY TURN` | `Battle/BattleEnd/Draw`（表**①**）· `Battle/HUD/{EndTurn,YourTurn,EnemyTurn}`（**表②**） | ①+② | ✅ 已在表 |
| `Tooltip.Melee` / `.Ranged` | **`Tips/MeleeAttackTip`** / **`Tips/RangedAttackTip`** | **表②**（表①**无**） | 键名可照抄；**值在远端 I2**（本地取不到） |
| `Tooltip.QuestPoints` / `.Faith` / `.SpiritStone` | **`CardTraitDescription/{questPoints,faith,spiritStone}`** | **表②**（表①无） | 键名可照抄；语义待核（原版是 **trait 的 tooltip**，我们是 HUD 图标 tooltip） |
| `CardText.FactionNames` 的 14 个非自造阵营 | **`Armies/<Faction>`**（`Armies/{AstraMilitarum,BlackLegion,DarkAngels,EmperorsChildren,Genestealers,Goff,Goff_short,Leviathan,SaimHann,Sautekh,Sororitas,SpaceWolves,TauEmpire,Ultramarines}` —— **14 条全在**） | **表①** | ⚠️ **键名可用**，但**载体未核**：那 14 颗 `Localize` 在**主菜单阵营选择页**的 prefab 上，**不是卡面那行字**（卡面那行字按 `CardText:459` 的注释来自 `数据/游戏数据/factions.json` 的 `cn` 字段）。**如实标「键名可用、载体待核」** |
| `CardText.FactionNames` 的 `Neutral` / `余烬` / `潮汐` | 无 | — | `Armies/` 里**没有 `Neutral`**；Ember/Tide 是我们自造阵营 ⇒ **原版无，需自拟** |
| `CardText.Names` 26 条（自造卡） | 无（表① `Card_Name/*` 只有 8 条：`EC21 EC57 GOF77 GOF91 SW2 SW57 UM100 UM88`，全是**异画/特例**，不含这 26 张） | — | **原版无，需自拟** |
| `CardText.KeywordNames`(12) + `KeywordZhNames`(62) | **键族 = `Card_Trait/<DefinedTrait枚举名>`**（`GameStaticData__TraitNameToString.c` 实读：`GetTermTranslation("Card_Trait/" + Enum.ToString(trait))`；**裸前缀 `Card_Trait/` 在表②**）。**本地两张表只有 10 条**：`Card_Trait/{chooseOne,classified,faith,quest,questClassified,questPoints,sabotage,secretOrder,trap,spiritStone}` | 表②（前缀+10 条） | ⚠️ **有键族、但 `armour`/`flying`/`rally` 这些常见词条【本地无判据】**（`Card_Trait/` 在 `assets_full` 全库 **0 命中**，见 `资料/普查产出_1018/V-OATH_Oath徽记与记号.md:164`；值在远端 I2）⇒ 键名可按 `Card_Trait/<我们的 canonical 键>` 造，**值取不到**，如实标 |
| `CardText` `Phrases` 的 `GAME OVER`/`YOU WIN`/`YOU LOSE`/`CHOOSE ACTION`/`PICK A TARGET`/`NO LEGAL TARGET`/`ABILITY`/`STUNNED`/`THIS UNIT CANNOT ACT` | **两表都无** | — | **原版无，需自拟**（或按近邻复用，**口径待裁**） |
| `CardText` 的 `TurnLabel`（第 N 回合） | 两表都无 | — | **原版无，需自拟** |
| `CardText` 效果小字/整句构件 16 条 | 两表都无（`Battle/Effect/Change*` 那族是「变化量」，不是这套） | — | **原版无，需自拟**（碎片，语序问题见 ③#6） |
| `Tooltip.Armour/.Health/.Cost/.Energy/.Skulls` | **两表都无**（也搜过 `ArmourTip\|ArmourTooltip\|SkullTip\|EnergyTip\|HealthTip\|FaithTip` = 0） | — | **原版无，需自拟** |
| `Tooltip` 的 `（规则书里没有这个词的条目）` / `（规则书 :N）` | 两表都无 | — | **原版无，需自拟** |

**搜过哪几个包/文件（铁律：报「没有」前先交代）**
`assets_full` 全库（`rg -g '*.json'` 递归，取 `"mTerm"` 488 条 distinct）· `tools/il2cpp_out/stringliteral.json`（26,507 条）· `tools/all_strings.txt` · `Warpforge_code/Scripts/Assembly-CSharp/{DefinedTrait.cs,AbilityTrigger.cs}` · `tools/decomp_full/`（`GameStaticData__TraitNameToString.c` / `SupportMethods__GetCreatedByText.c`，后者**本轮没亲读**，靠 `CardView.cs:1792-1794` 的现成判据）· `资料/普查产出_1018/V-OATH_Oath徽记与记号.md`。

---

## ⑤ 切块建议

🔴 **先看这条**：真正的活只有 **3 个文件 / 169 处**；另外 **37 个文件是「零处」**（只报「已核清」，**不派写手**）。

### 5.1 建键波（**串行，1 写手，只动 `Core/Loc.cs`**）

`Core/Loc.cs` 是全工程唯一语言表 ⇒ 铁律「一个文件一个写手」⇒ **169 处要用的新键必须一波由同一个人建完**（估 **≈139 条键**：卡名 26 + 关键词 74 + 短语 12 + 效果构件 16 + 提示 8 + 阵营 3）。⚠️ 这一波之前要先裁两件事（见 5.3）。

### 5.2 接线波（**可并行，3 写手**）

| 块 | 文件 | 处数 | 与其他块的耦合 |
|---|---|---|---|
| **A** | `Core/CardText.cs` | **157** | 单文件最大；`Phrases` 里 4 条**死条目**要顺手处置（见 5.4） |
| **B** | `Core/Tooltip.cs` | **13** | **可与 A 并行**（不同文件、无共享键）。⚠️ `TipText.Trait` `:474-475` **读** `CardText.KeywordZh/KeywordEn` ⇒ 只要 A 不改**方法签名**就无冲突；若 A 要改签名 ⇒ **必须 A 先、B 后** |
| **C** | `Core/CardView.cs` | **1** | 可与 A/B 并行，但 `CardView.cs` = **364 KB 的热点文件**（本会话 mtime 10-08 13:18）⇒ **单独一人、动手前先确认没人同时在改** |

### 5.3 开工前必须裁的两个口径（**它不裁，列出来**）

1. **卡面文字要不要进 `Loc.cs`**：`CardText.Names`(26) + `KeywordZhNames`(62) + `KeywordNames`(12) + `FactionNames`(16) = **116 处是「卡牌数据表」**，不是 UI chrome。搬进 `Loc` 有**两个硬风险**：
   - 🔴 **`CardText.AllChinese()` `:631-650` 是字体烘字的唯一语料源**（`TmpSetup` 建 CJK 字体资产用）。它 `foreach (var v in Names/KeywordNames/Phrases/FactionNames)` —— **表搬走后这几行 foreach 就空了** ⇒ 那些字**烘不进字体** ⇒ 卡面印出**方块/空白**（正是本工程点名的「静默失败」）。搬表**必须同批**把 `AllChinese()` 改成 `Loc.AllChinese()`（`:642` 已有这一条，但只覆盖 `Loc` 自己的中文列）。
   - 🔴 `CardText.Zh` (`:32-37`) = **字体闸 + 语言闸二合一**，与 `Loc.T` 是**两条并行取值路径**。搬表 = 抹掉其中一条，是**结构性改动**，不是「换字面量」。
2. **`GAME OVER`/`YOU WIN`/`YOU LOSE` 与 `NO LEGAL TARGET`/`MELEE`/`RANGED` 要不要复用近邻键**（`Battle/BattleEnd/{Victory,Defeat}` · `Battle/Tips/{NoTargetAvailable,MeleeAttack,RangeAttack}`）—— 复用 = 改文案；另立 = 多 6 条键。**只能选一种**（铁律 6）。

### 5.4 死条目顺手处置（4 条，**不是遗漏**）

`CardText.Phrases` 的 `HAND`/`DECK`/`DISC`/`HP`（`:360-363`）**全仓零消费点**（grep 实证）；`THIS UNIT ALREADY ACTED`（`:384`）`Phrases` 那侧也已无消费点（`:380-381` 自陈）。⇒ 接线时**一并决定删或留兜底**，别默默留着。

---

## ⑥ 证据（判据出处）

| 判据 | 出处 |
|---|---|
| 表 C（`A1013` 五族定案）+ 表 B3（闸门收口）+ 表 A（建键格式范本） | `d:/4/Unity/资料/普查产出_第五会话/查证_23双语键盘点.md`（`Loc.cs` 383 条那版现状核对在 `:7`） |
| **本报告的清单格式范本** | `d:/4/Unity/资料/普查产出_第四会话/施工单_双语③逐处换key_附_Shell.md` §一/§二（①处数/②处数/③处数 三列 + 逐条键名表） |
| 另一个格式范本 | `d:/4/Unity/资料/普查产出_第四会话/施工单_双语_Net整片_P6.md` §②（82 处权威表） |
| `Loc.T` 缺键**回键名本身** | `Core/Loc.cs:1683-1686` `Debug.LogWarning("[Loc] 语言表里**没有**这个词条：…")` |
| `Loc.T` 缺条目时**不能**用 `??` 兜底 | `Core/FilterPanelModel.cs` 已按表 B3 收成裸 `Loc.T`（现读零中文字面量 = 已落地） |
| `CardText` 走的是「字体闸 + 语言闸」两条 | `Core/CardText.cs:16-22` 文件头 + `:32-37` |
| `Battle/HUD/CreatedBy` 已在 `Loc.cs` | `Core/Loc.cs:813`（那块注释 `:805-813` 自陈「那一处在别的笔的白名单里」） |
| `Card_Trait/` 拼键机制 | `GameStaticData__TraitNameToString.c`（转述见 `资料/普查产出_1018/V-OATH_Oath徽记与记号.md:130-136`）+ 表②裸前缀 `Card_Trait/` |
| `Card_Trait/` 在 `assets_full` 全库 0 命中 | 同文档 `:164` 与 `:440` |

---

## ⑦ 没查清的（逐条）

1. **`Tips/MeleeAttackTip` / `Tips/RangedAttackTip` / `CardTraitDescription/{faith,questPoints,spiritStone}` 的【值】取不到** —— 它们只在 `stringliteral.json` 里以**键名**存在（代码里 `GetTermTranslation` 的实参），**值在远端 I2 表**（本地 84/91 个 bundle 里没有 `localization_assets_all`）。⇒ 它**只证了键名**，没证语义。搜过：`assets_full` 全库 `mTerm`（488 条）、`stringliteral.json`（26,507 条）、`all_strings.txt`、`资料/` 全 md。
2. **`Armies/<Faction>` 14 条到底是不是「卡面阵营行」的载体** —— 它证了这 14 条 `mTerm` 在表① 存在，但**没解出它们的 `m_GameObject → RectTransform.m_Father` 节点路径**（表 C 干过这一步；本轮没做，因为要用脚本解树）。⇒ 如实标「键名可用、载体待核」。
3. **`CardText.KeywordZhNames` 62 条**：只证到「原版键族 = `Card_Trait/<枚举名>`」，**没有逐条把我们的 canonical 键映射成原版枚举名**（`DefinedTrait.cs` 里有 `rally/stealth/slay/flying/vanguard/armour/longrange` 等，它只读了 grep 到的 7 个）。剩下的 55 条**没逐条对枚举**。
4. **`Tooltip.QuestPoints` 的 `（0/3）`** 是硬编码在文案里的（`:382`）—— 原版那个「3」是不是常量、有没有 `{0}` 占位，**没查**。
5. **`Tooltip.TipText` 的 10 条 tooltip 是不是真从 `Tips/*` 走**：`BattleDriver` 是 `Tooltip.Show(TipText.X, …)` **直接传我们自己的串**（`:13280-13334`）—— 它**没追**原版那颗 `Health/Range Attack/Melee Attack/Cost Container` 的 `EverguildTooltipTrigger` 把 key 喂给谁。只看出了 key 名。
6. **`CardText.Phrases` 的 `MELEE`/`RANGED` 与 `Battle/Tips/{MeleeAttack,RangeAttack}` 是不是同一处**（一个是「打法名」、一个是「数值格标签」）—— **没查**，标「待裁」。
7. **`CardText.cs` 的 mtime 是 2026-10-07 21:57，但文件里有 `2026-10-18` 的注释** —— 时间戳与内容日期冲突，**没追**（可能是时钟/落盘口径问题），**不影响内容判读**。
8. **`Loc.cs` mtime = 2026-10-08 23:03**（= 本次会话期间）⇒ **可能有人正在改 `Loc.cs`**。它读到的 `Loc.cs` 是那一刻的快照（`new Entry(` = 408 条）；**建键波开工前请重核条数**。

---

## ⑧ 顺手发现的

1. 🔴 **`CardView.cs:1804` 是一处「白捡的」** —— `Battle/HUD/CreatedBy` **早在 `Loc.cs:813`**（值逐字对上），`CardView.cs:1792-1799` 的注释自己写着「等 `Core/Loc.cs` 收了这条键就改走 `Loc.T(...)`，并把这两句删掉」，而 `Loc.cs:811` 又写着「那一处在**别的笔的白名单**里」。⇒ **两边都在等对方**，一条键挂了整整两个会话。
2. 🔴 **`CardText.AllChinese()` 是搬表最大的坑**（见 5.3·1）：搬走 `Names/KeywordNames/Phrases/FactionNames` 而不同批改 `:633-636` 那 4 行 `foreach` ⇒ **字体语料漏字 ⇒ 卡面印方块**，且**不报错**。
3. ⚠️ **`KeywordZhAliases`（装甲/爆破）不能搬进 `Loc`** —— 它不上屏，只参与「卡面这一段是不是已经印过」的比对；搬走会让 5 张卡（`AM13/AM28/AM34/AM37/AM54`，`:210` 记着）**重复印一遍关键词**。
4. ⚠️ **`CardText.Phrases` 有 5 条已死**（`HAND/DECK/DISC/HP` 零消费 + `THIS UNIT ALREADY ACTED` 已改走 `Loc`）—— 表里没有任何标注说它们是死的。
5. ⚠️ **`CardText` 英文档的质量比中文档差一档**：英文列只在 `FactionNamesEn`（**13 条**）里有；卡名英文档回的是 `CardDef.Name`（= 英文 id，不是成品卡名的原版英文），关键词回 `norm.ToUpperInvariant()`（`ARMOUR` 全大写，见 `:548`），只有走 `Loc.T` 的那几件会正常变。
6. 📌 **`VfxMap.cs` / `CardHighlight.cs` / `CardWinBox.cs` / `CardButtons.cs` / `CardFan.cs` / `CardTween.cs` / `TextCanvas.cs` / `PragatiDigits.Data.cs`** = **代码位零中文字面量**（全在注释里）—— `A1013` 里被怀疑的 `CardWinBox`/`CardText`/`DraggableController` 三个，前两个一个 **0 处**、一个 **157 处**。
7. 📌 **`CardText.cs:369-374` 的作者已经自己做过一轮「原版有没有键」的判定**，且有对有错（`:376-379` 就把 `:369-375` 的前一条推翻了）⇒ 接线时**别照抄那段注释的结论**，以本件的 ④ 为准。
8. 🔴 **行号漂移已在本次会话中实测到**（`BattleDriver.cs` 的 `_resultLabel.SetText` 从 `:13029` → `:13035`）⇒ 本报告 ③ 表的 `Core/` 行号可信（三件 md5 已记），但**引用 `Battle/`/`Shell/`/`Net/` 的任何行号都要重核**。
