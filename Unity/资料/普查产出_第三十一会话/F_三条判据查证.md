# F · 三条「判据不齐 / 判不出」的账 —— 判据查证（只读 · 2026-10-14 第三十一会话）

> 只读查证，**没有改任何代码或正本**。行号一律现读。
> 三条 = `A1989`（`项目任务.md:547`）· `A1994`（`:555`）· `A985 尾巴⑥`（`:431`）。
> ⚠️ **路径订正（铁律 5）**：简报给的 `Unity/数据/游戏数据/cards_engine.json` **不存在**；
> 卡池真身 = **`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`**（root 是 dict：`version/source/note/count/cards`，`cards` = **1126** 张）。

---

## A1989 全池扫描

**结论：受影响卡 = 【1】张 = `GSC29 Metamorph Iconbearer`。** 全池 1126 张里
「段首 = `choose `」的段共 **54 段 / 54 张卡**（其中 **52 张**产出 `choosecard` op；
`TL20` 走 `ReChooseBonus`、`TL49`/`TL50` 走 `TryChooseEffect` 故无），
**只有 GSC29 一段在 act 之后还有残句**。
（⚠️ 反向那半也现核过：段首**不是** `choose` 的 `UM_Author_of_the_Codex`
（`Trigger the Codex ability of a friendly unit and choose a Codicil and put it in your hand`）
**也**产出了一条 `choosecard` op —— 那是走 `Finish` 的 ` and ` 尾句**递归**再进 `TryChooseCard`（「继续解析」那一向）。）

**证据（我自己扫的，不是抄账）**
- 扫描法：照 `EffectText.Split`（`RuleEngine/Core/EffectText.cs:1014` 的 `.`/`\n`/`\r`）切段 → 剥触发头
  → 段首 `choose ` → 用 `ReChooseWhat`（`:4844`）算 `what` 区 → 按 `TryChooseCard` 的 **act 表**
  （`:5066-5106`）+ `ReChooseIncCost`（`:4856`）/`ReChooseCopies`（`:4849`）算 act 区
  → **残句 = 两者之后的剩余文本**。
- 命中行：`GSC29`，`what='2-cost genestealer cults troop'`、`act='copies'`、**RESIDUE = `lower their cost by 1`**。
- 我们自己解析器的产物（`_tmp_view/w15_scan.txt:411`，2026-10-14 14:20）：
  `GSC29|…|||choosecard/pool/2-cost genestealer cults troop/copies/0//0/0//////{T:null}/{SRC:Rally: Choose a 2-cost Genestealer Cults troop and create 2 copies in your hand Lower their cost by 1};`
  ⇒ **整条卡只有 1 条 op**，`lowercost` 既不在 op 段里、也没有独立的 SRC 段。
- **对照组（同形状但尾巴剥得出来，全部安全）**：`AM4`/`UM74`/`EC_Decadent_Throne`（`Lower its cost by N` 是**下一段**）、
  `ASH41`/`AM62`/`AM52`/`BL56`/`DA40`/`TAU48`（`It costs N less` / `lower its cost by 3` 是**下一段**）——
  它们的 op 段里都带一条**独立** `lowercost` op（各自 `{SRC:…}`）。

**「同族【没扫】」那半 —— 已补扫，结论是【干净】**
- 口径 = 「段首 `choose`、后文还有 act 表**没**认下的动词」的段。按上面同一台扫描器逐段算残句 ⇒ **0 段**。
- 三个 act 表外的写法**都不是这个病**：`TL20`（`Choose and gain a bonus (…)` → `ReChooseBonus`→`TryChooseOne`）、
  `TL49`/`TL50`（`Choose an effect and give it to …` → 故意让给 `TryChooseEffect`，`EffectText.cs:5004`）。
- `act=''` 的 5 段（`AM4`/`DA42`/`SAU74`/`TAU48`/`UM74`）**段内没有第二个分句**（动作本来就在下一句）⇒ 安全。
- ⚠️ **一处必须说清的边界**：本扫描只覆盖「**段首** = choose」这一族。`TryChooseCard` 是这一族唯一
  「整段领走、不切 ` and ` 尾句」的 handler（`:5109-5121` 直接 `r.Kind=Ok; return true`）。

**判据原文（反编译）**
- **没查到「解析层顺序」这种东西** —— 原版**不解析英文卡面**：效果是**序列化数据**
  （`cardAbilities[].abilityLogic[]`，步 = `chosenAbility` 数字枚举）。
  搜过：`lower_cost` / `LowerCost` / `lower cost` 在 `d:/2/tools/decomp_full/`（25,096 个 `.c`）**0 命中**；
  文件名含 `desc|text|localiz|parser` 的类逐一看过，只有取词条的 `GetLocalizedCardDesc` 一类，**没有卡面解析器**。
- **第二判据（原版数据，只到旁证）**：`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` 的 `GSC29`
  = **2 步、都是 `chosenAbility=100`（createCardInHand）**，`buffEffect.costChange = 0`、`manaBuff.manaAmount = 0`
  ⇒ 🔴 **那份 2022 快照里【没有】降价那半步**，**不能**拿它反证「原版不降价」。
  （对照：`AM4`/`UM74`/`TAU48`/`ASH41`/`AM52`/`DA40` 的「降价」在快照里 = `chosenAbility=30`+`buffEffect.costChange = −N`
  —— 与卡面**逐个吻合**，说明快照编码是可信的，GSC29 这一处只能是**快照年代较早/该卡后被改过**。）
- **最终判据 = 成品卡图（铁律 7）** —— 2026-10-14 我**亲读**（`Read` 该 PNG）：
  `d:/2/Warpforge部队卡片/Genestealer Cult/3部队/Warpforge_31_Metamorph-Iconbearer.png`
  卡面逐字 = `Rally: Choose a 2-cost Genestealer Cults troop and create 2 copies in your hand Lower their cost by 1`
  （**没有 `and`、没有句号**）⇒ **`desc` 忠实、缺陷在解析层**，与账文结论一致。

---

## A1994 逐条判

**红的这几支共有 35 行**（判据 = `资料/普查产出_第三十会话/A1977_对账_E1E2_1014.tsv`，
现读 `判定=🔴真差异` 共 192 行；其中 `原版取值` 含 `addTrait` 的 **32 行** + `SAU27` + `GSC37` + `GOF91`）。
判定用的两份读数：① TSV 的 `第7/9/10/11 列` ② **我们自己的解析产物**
（`_tmp_view/w15_scan.txt` 的 op 段 + `cards_engine.json` 的 `desc`/`keywords` + `RuleEngine/Core/*.cs`）。

| 红的条目 | 误报 / 真缺 | 该怎么补判据 |
|---|---|---|
| `GOF10`·`GOF63`·`GOF69`·`GOF25`·`GOF70`·`GOF61`·`GOF17`·`GOF58`·`GOF92`·`GOF26`·`GOF66`（11 行，`trig=Unstable`） | **误报（配对缺口）** | 原版把 `Unstable` 的载荷**逐卡序列化**（`dmg1-3/opt90 buff{addTrait fast}`，全池 **18 张**一字不差）；我们侧是**关键词级规则** —— `RuleCore.cs:6406-6411` → `UnstableBlast()`（`:8153`），判据 = `u.Has(KeywordTable.Unstable)`。**判据补法**：把「op 家族匹配」换成「**关键词/规则级承载**匹配」（读 `ruleprobe layers` 的关键词列 + `CardDef` 的关键词→规则表，`:3784` 那张）。⚠️ **仍有一处要人工/实况裁**：原版同一载荷还带 `addTrait fast`（`DefinedTrait.fast = 40`，`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:7`），而**粉丝规则书只写「对随机单位造成 1-3 伤害」**（`规则书_中文翻译.md:221`）⇒ 这半步是「原版真有」还是「模板残留」，**本地判不了** |
| `ASH39`·`GOF31`·`DA18`(×2)·`UM60`·`TAU_Hero_of_the_Empire`·`TL_Alpha_Warrior`·`GOF72`·`GOF38`·`SAU40`·`SOR_Daemonbreaker`·`ASH_Flickerjump`·`GOF42`·`UM_Master_of_Arms`·`AM_Renowned_Bodyguard`·`GOF_Worst_Temper`·`AM22`·`GOF99`（共 18 行） | **误报（承载不同）** | 我们**已经解析出来了**，只是承载形态不同：`gain/pool///0/invulnerable`·`gain/…/vanguard and 2 quest points`·`give/…/+1 attack, blast 1 and shield`·`chooseone/pool///2/give flank to a friendly troop…` —— 载荷是**字符串**，而原版是 `buffEffect` 结构体。**判据补法**：对原版每一步的 `buffEffect{attr/trait}` 去 `GivePayload.Parse(<我们那条 op 的 Payload>)` 的**结构化结果**里找对照物（口已在：`Core/GivePayload.cs`） |
| `SAU27 Scarab Swarm`（`WhileInPlayVariable`） | **误报（承载不同）** | 我们的承载 = **静态规则字段**不是 op：`CardDef.cs:1196 MeleeEqualsHealth`（认句 `:1276`，读点 `RuleCore.cs:3169`，断言 `RuleEngineTest.cs:6243`）。**判据补法**：对账先查 `CardDef` 的静态字段表，再查 op |
| `GSC37 Patriarch`（`WhileInPlayVariable`） | **误报（承载不同）** | 同上：`CardDef.cs:1222 CostPerEnemyHandCard`（认句 `:1313`） |
| `GOF91 Beastboss on Squigosaur`（`WhileInPlay`，本轮 ①） | **误报（配对缺口）** | 🔴 **这一条我给出了非猜测的判据**：我们的光环载荷 = 引号里的 `"Slay: Gain Blood Thirst this turn"`，而引擎**有专门的承载**：`GivePayload` 的 **`PayloadOp.Embedded`** 支 + `EffectResolver.GrantEmbeddedAbilityFromAura`（`:6512`，其核心 `GrantEmbeddedCore`）；`Aura.cs:376 PayloadUnderstood` 就是它的准入闸。⇒ 判据 = **`GivePayload.Parse(载荷)` 出 `Embedded != null` ∧ `EmbeddedKeyword(载荷)` 在 `KeywordTable` 里**（具名符号判据，**不是**「带引号就算命中」那种形状猜——写手当初拒绝形状判据是对的，但它拒的**不是**这条） |
| `ASH_Path_of_the_Seer`(×1)·`SOR29`(×1)·`e01f4d54…SOR_Daemonbreaker`(×1)·`67adbc7f…ASH_Flickerjump`(×1) 的**另一半** | **疑似快照过期（非缺陷）** | 这 4 行的**原版那一步在现行卡面上没有对应文字**（例：`ASH_Path_of_the_Seer` 现行卡面 = `Ephemeral. Choose a card in your hand and return it to your deck. Draw a card. Gain 1 Spirit Stone`，而原版那步 = `dmg2-4 buff{addTrait blast}`；`GOF99` 卡面 = `Deal 1 damage to all enemies, and they lose Stealth`，原版那步 = `addTrait vanguard`）。**判据补法**：按**铁律 7** 逐张亲读 `d:/2/Warpforge部队卡片/…png` —— 卡面也没有 ⇒ 按「**2022 快照 ≠ 现行卡**」销账，⛔ 别记成「我们缺」 |

**汇总**：**35 行 = 34 行「误报」（承载不同/配对缺口，我们的机制都在，只是不在 op 列表里）+ 0 行确认的实现缺口**
+ **2 处必须单列的悬案**（Unstable 的 `addTrait fast` 半步 · 那 4 行原版步在现行卡面无对应 ⇒ 只能靠 PnP 亲读）。
`E2` 那句「32/32 都是「该卡光环层为空」」**成立**，但**不能**由此推「我们缺」——
`addTrait` 这一族本来就不走光环层（走 `gain`/`give` 载荷、关键词、或 `CardDef` 静态规则）。

🔴 **顺手发现（铁律 13·4⑧）**：这 32 行里有 **8 行的 `卡id` 列印的是 32 位 GUID**
（`9f75a6fe…`=Hero of the Empire · `8f2da557…`=TL Alpha Warrior · `09bcb1b3…`=ASH Path of the Seer ·
`e01f4d54…`=SOR Daemonbreaker · `67adbc7f…`=ASH Flickerjump · `8425b809…`=GOF Worst Temper ·
`55b8e3d2…`=UM Master of Arms · `98cd4c02…`=AM Renowned Bodyguard）
—— 这正是 **`A1991`** 那条（`卡id` 列印真值 `uniqueId`、不是我们匹配到的 id）；
本报告靠**第 11 列「我们面 · 光环」**把卡认回来。⇒ 判这条账时**必须先修仪器**，否则人工逐条判都认不出卡。

---

## A985 尾巴⑥

**结论：没做完（`A985⑥` 的 ③ 仍在）。**

**证据**
1. **A985 的原子项编号表 —— 找到了**（`git show 4d89a7d:项目任务.md:540`，那是 §29·b 里 `A985` 那一行）：
   ⑥ = **「`Battle/Cemetery/*` 19 条（**前置 = 引擎加 3 种事件** `Draw`/`AmbushExit`/`SecretOrder` + 值在远端）」**
   ⇒ 与 `A985⑥ 尾巴`（现 `资料/任务_卡组卡牌战斗.md:28`）**同一母账无误**。§四·20 说的「缺的那张子项表」就是它。
2. `A985⑥ 尾巴`（`资料/任务_卡组卡牌战斗.md:28`）现读自身分三半：
   **① `Ability.Targeted` 与 `Play` 的 6 条细分 —— ✅ 已填完**（靶向 2 + 伏击 2 + `Ability` 2，含 `BattleEvent.PlayAmbush`）；
   **② 那 19 条的文案值 —— 当时「不在这张表」（在远端）**；
   **③ 「按原版那 19 条重新分类」—— 自陈「那笔还没做」**。
3. 🔴 **③ 现读仍是【没做】**：`CardPresentation/Battle/BattleDriver.cs:6572 RefreshBattleLog()` 的
   `switch (e.Kind)` 现读只有 `Play/Deploy/Attack/Hit/Death/Return/Ability/Trigger` + `default:`
   （`:6670-6671` = `$"{who}「{card}」"` —— **无动词那一行**）⇒ **没有 `case EvtKind.Draw/AmbushExit/SecretOrder`**；
   而 `EvtKind` **已经有**这三种（`RuleEngine/Core/BattleEvent.cs` 现读 15 个成员：…`Draw, AmbushExit, SecretOrder`）。
   全仓 `ActionYouDraw`/`ActionExitAmbush`/`ActionSecretOrder` 只出现在**注释**与 `Core/Loc.cs` 里，**没有消费点**。
4. **② 那半已闭**（第三十会话）：`官方本地化_en+zh-CN.json` 现读 9,441 条，值是**本地**的，
   并且已经落进 `Core/Loc.cs:1056-1076`（`Battle/Cemetery/*` 20 条）——
   ⚠️ 但 `Loc.cs:1038-1039` **自陈「本块只是把值备好，⛔ 不是「已接」」** ⇒ **值到位 ≠ 接线到位**，
   接线本身就是 ③ 那一半。
5. **两行口径打架的裁法**：`A985 尾巴`（留痕 → `资料/历史/A表已收口_第十二会话.md:59`）曾自陈
   「✅ ③④⑥⑦⑧⑨⑩ 本会话已做」；`A985⑥ 尾巴` 仍留 ③ ⇒ **以细的那行为准**。
   （旁证：`资料/历史/交接_1018第三会话.md:73` 也把 `A985⑥` 尾巴 列在「仍开着」里；而它 `:39` 只把
   **「前置 + ①」** 记成做完。）

**还差什么（判死它需要的东西，现在都齐了）**
- 判据齐：19 条键名与语义（`BattleDriver.cs:6578-6598` 的枚举→词条表）· 19 条文案值（本地）· 落点（`RefreshBattleLog`）。
- **唯一欠的是动手**：在 `switch` 里加 `Draw`/`AmbushExit`/`SecretOrder` 三支（＋按原版改用那 20 条键），
  再把那 12 条写死中文的模板一起收口（`BattleDriver.cs:6637-6638` 自陈的另一半）。
- ⚠️ **§四·20 可以就地结案**：结论 = **没做完**（不是「判不出」）。

---

## 搜过的词 / 没查到的

- **A1989 反编译**：`lower_cost` · `LowerCost` · `lower cost`（`d:/2/tools/decomp_full/` 全量，**0 命中**）；
  文件名含 `desc|text|localiz|parser` 的类（逐一看过）；`AbilityEffect_枚举词典.tsv`（83 值，找「降费」那一档）。
- **A985 子项表**：`git log -S` 逐个试 `A985⑪`（**0 commit**）/`A985⑨`（0）/`A985⑥`（5）/`A985①`（2）
  ⇒ 最终在 **`4d89a7d:项目任务.md:540`** 取到（**按提交取，⛔ 别按行号取**）。
- **A1994**：TSV 只**流式**读了 `第7/8/9/10/11 列`（507 KB / 1821 行，⛔ 没整篇贴）；
  我们侧读数取 `_tmp_view/w15_scan.txt`（493 KB，只抽 35 行）。
