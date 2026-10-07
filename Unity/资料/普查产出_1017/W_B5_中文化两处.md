# W_B5 · 中文化两处（D5 卡面兵种行 · D6 选择卡组窗标题）

> 2026-10-17 · 写手代理 B5 · 改动只落白名单 5 个文件 · ⛔ **没跑 Unity**（断言已写好，等主对话收口时按覆盖面跑）

## ① 结论

- **D5 做完**：卡面「兵种行」（原版 `RaceText`，`CardView.SubtypeLine`）**不再是写死的英文**，改走 `Loc` 词条。
- 🔴 **键名不是我起的 —— 原版自己拼的：`Card_Race/<race>`**。三条独立判据互相印证：
  ① `GameStaticData.CardRaceToString` / `MinionRaceToString`（`d:/2/tools/decomp_full/` 方法体实读）= `GetTranslation(String.Concat(<前缀>, <race>))`，Warlord 那一支用**整条字面量**；认不出类型时**出声 + 印空串**（`"CardRaceToString undefined for "`）—— 我们 `SubtypeLine` 的 `default: return null` 就是它。
  ② 两条 `.rdata` 字面量**按 RVA 读出来了**：`0x1842cdf40` = **`"Card_Race/"`** · `0x1842ce040` = **`"Card_Race/Warlord"`**（地址 − `0x180000000` 的偏移，与 `d:/2/tools/all_strings.txt` 的表偏移 **70049600 / 70049856 逐位对上**）。
  ③ 原版 prefab 旁证：类型筛选器 `locKey.mTerm = "Card_Race/Warlord"`（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3311978293714882780.json`，`option = 10`）。
  ⇒ 任务书里建议的 `Card/Subtype/<X>` **没用**（那是自拟的，而这一族有原版的键）。
- **D6 做完**：标题改走 `Loc.T("MenuDeck/Tip/SelectDeckAgainst")`；词条**已亲自核实在原版 prefab 上**（`MonoBehaviour_4152179270747796863.json` 的 `mTerm`，它的 GO = `GameObject/Instructions 2.json`）。**中文 = 实拍「选择卡组」· 英文 = `Select deck`**；**版面一个字没动**（照 prefab 居中）。
- ⚠️ **中文那一列只有 3 个是实拍**，其余 40 个来自**我们自己的表** —— 见 §②。

## ② subtype 全表

`RuleEngine/Resources/cards_engine.json`（1126 张）：**非空 subtype 43 种**，其中**会印到卡面上的 19 种 / 722 张**（判据 `CardView.SubtypeLine` + `TacticSubtypeShown`）· 空串 10 张（`tactic`，不印）。★ = 实拍那 3 个（实拍图 = `资料/原版参照图/用户实拍_1017/卡组编辑界面参考.png`）：

| subtype | 张数 | 中文 | 出处 |
|---|---|---|---|
| Infantry | 397 | 步兵 | ★**实拍**（守护者防御小队/风暴守护者/战巫/游侠）· zh_CN.csv:6363 同值 |
| Vehicle | 135 | 载具 | ★**实拍**（武器平台）· zh_CN.csv:6365 同值 |
| Warlord | 56 | **战将** | ★**实拍**（三张督军卡那一行）· ⚠️ zh_CN.csv:15 = **督军**，见 §④ |
| Defence | 39 | 防御 | zh_CN.csv:6367（**我们译的**） |
| Monster | 18 | 怪兽 | zh_CN.csv:6369 |
| Beast | 15 | 野兽 | zh_CN.csv:6372 |
| Battlesuit | 8 | 战甲 | zh_CN.csv:6374 |
| Drone | 7 | 无人机 | zh_CN.csv:6377 |
| Psychic Power | 7 | 灵能 | zh_CN.csv:6375 |
| Combat Elixir | 6 | 战斗药剂 | zh_CN.csv:6391 |
| Daemon | 6 | 恶魔 | zh_CN.csv:6381 |
| Secret | 5 | 隐秘 | zh_CN.csv:404 |
| Dark Pact | 4 | 黑暗契约 | zh_CN.csv:378（与 `KeywordZhNames` darkpact 同值） |
| Sabotage | 4 | 破坏 | zh_CN.csv:403（与 KeywordZhNames sabotage 同值） |
| Codicil | 3 | 法典 | zh_CN.csv:6408 |
| Genomic Enhancement | 3 | 基因强化 | zh_CN.csv:387 |
| Invocation | 3 | 计策 | zh_CN.csv:6386 |
| Overlord Power | 3 | 霸主之力 | zh_CN.csv:6384 |
| Rune | 3 | 符文 | zh_CN.csv:6385 |

**现在不印的 24 个**（列全，白名单哪天长了不用回头补；**全部是我们译的** / zh_CN.csv）：`Ability 22→能力:6368 · Action 7→行动:6376 · Astra Militarum 1→星界军:6407 · Battle Ability 2→战斗能力:6389 · Card 6→卡牌:6380 · Command 6→指挥:6379 · Ephemeral 1→临时:382 · Event 17→事件:6371 · Mission 2→任务:6390 · Order 1→命令:6387 · Pact 1→契约:6410 · Relic 1→圣物:6411 · Spell 245→咒语:6364 · Stratagem 5→战术:6378 · Structure 3→建筑:6382 · Support 8→支持:325 · T'au Empire 1→钛帝国:6412 · Tactic 58→战术:6366 · Talent 1→天赋:422 · Trick 1→诡计:6413 · Upgrade 4→升级:185 · Warlord Ability 1→督军能力:6414 · mission 1→任务:6415 · spell 1→咒语:6416`

## ③ 改动清单（含断言）

1. `Core/Loc.cs`（**只加词条，机制未动**）：新增 `Card_Race/<race>` 族 **43 条**（19 会印 + 24 不印）+ 一段出处注释（原版两个方法体 · 两条 `.rdata` · 实拍坐标 · 逐条 `zh_CN.csv:<行>`）+ 文件头补「这一族也不是自拟」。行尾 LF（原样）。
2. `Core/CardView.cs`：`SubtypeLine` 的**每个出口**过一遍新增的 `RaceTerm(race)`（`Loc.HasEntry` → `Loc.T`，查不到则**原样英文 + 只出声一次**）；新增 `public const string RaceTermPrefix = "Card_Race/"` · `public string RaceText` · `public bool RaceShown`（自检读卡面用，仿 `ArmyShown` 先例）；就地更正 `subtype` 字段上那句「**我们还没有中文对照表**」。**阵营行分档（`FaceShowsArmy`）/ 分层 / 渲染队列 / 位置 / 字号 / 颜色：一个字没动。**
3. `Shell/DeckSelectionPopup.cs`：写死的 `"Select deck"` → `Loc.T(TitleTerm)`；新增 `public const string TitleTerm = "MenuDeck/Tip/SelectDeckAgainst"`（含出处）；文件头那句「`Instructions 2` 文案取不到 ⇒ 用同窗 `Instructions` 那句」**就地更正**（见 §④）。
4. **断言 D5** → `Editor/CardBaseDemo.cs` 新增 `AssertSubtypeLine()`（`Run()` 里 §4b 调）：① 19 个会印的字种**键全在**，且 `CardView.RaceTermPrefix == "Card_Race/"`（**期望值写字面量，不用被测常量**）；② 中文档 `Loc.T("Card_Race/<Infantry|Vehicle|Warlord>")` = **步兵 / 载具 / 战将**（实拍值一字不差），英文档 = `Infantry`/`Warlord`；③ 实况：**同一份 `CardData`**（`Placeholder` + `subtype="Infantry"`/`type="unit"`）建一张卡，换语言后 `SetData` 重画 ⇒ `RaceText` 分别是「步兵」/「Infantry」，且**两档必须不同**（灭自证）。先断 `TmpFont.Available` 前提，字体不在会明说是假绿。
5. **断言 D6** → `Editor/CollectionScene.cs`（`DeckSelectionPopup` 那一段开头）：① `DeckSelectionPopup.TitleTerm == "MenuDeck/Tip/SelectDeckAgainst"`（期望值写字面量）；② 中文档 `Loc.T(键)` = **选择卡组**、英文档 = `Select deck`；③ 实况：`FindChild(sel.transform, "Instructions 2")` 那颗 `Label.Text == Loc.T(键)`（先断节点在）。改坏法：`Build()` 改回写死的 `"Select deck"` ⇒ **中文档下 ③ 立刻红**。

## ④ 没查清 / 明知限度（如实记）

- 🔴 **`Warlord` 一词两说**：**实拍卡面 = 战将**（本次照它做），而我们自己中文表 `zh_CN.csv:15` 的通用词是 **督军**（全工程 78 处用「督军」，含效果文字「你的督军」）。两处**并存，本次没动后者** ⇒ **请主对话裁一次**。
- `zh_CN.csv` **是我们自己译的表、不是原版**（`资料/全量反编译复核_靠推断的清单.md` §2.2 定案）；拿它当**非实拍行**的出处只图「全工程译法一贯」，逐条出处已写进 `Loc.cs`，换权威只需改那一列。原版**英文**文案在远端 I2（84 个本地 bundle 无本地化包）⇒ 英文列照原值、不自拟。
- `zh_CN.csv` 两处小问题**照抄未擅改**：`Invocation→计策`、`Stratagem→战术`（与 `Tactic→战术` 同译）；`Warlord Ability→督军能力` 与卡面 `战将` 不一致（该值**当前不印**）。
- **D6 断言 ③ 的限度**：它比「窗上的字 == `Loc.T(键)`」，**在英文档下对「写死英文」无鉴别力**。本次**没有**强制中文档重开窗（那要动 `CollectionScene` 那段开窗流程的生命周期，风险大于收益）⇒ 如实记。
- `.cs` 原来那句「同窗 `Instructions`(act=N) 有明文 `Select deck`」**对不上**：本解包里 `GameObject/Instructions.json` 那颗 TMP 的 `m_text` 实际是 `'
'`；bundle 里 `"m_text": "Select deck"` 的两处是 **fs 64** 的另一族节点。已就地更正。
- 🔴 **顺手发现（⛔ 不在我白名单，没动）**：`Core/CardText.cs` 的 `AllChinese()`（= `Editor/TmpSetup.cs` 建字体时的**覆盖自检语料**）**只收 `CardText` 自己的表、不收 `Loc` 的中文** ⇒ `Loc` 里这 43 个新词（及此前设置窗那批）**不在缺字检查范围里**。TMP 是 Dynamic 模式、运行时仍烘得出来 ⇒ 这**不是**「字画不出来」，是**漏检**。要不要并进语料，请主对话定。
- 没查的：本批**没跑任何 Unity 自检**（红线）；断言只做了静态核对、**未实跑**。
- 另记（非我职责）：`Core/Loc.cs` 期间**被另一位写手同时改过**（多了 5 条 `MenuDeck/*`、`MenuShop/*`）。已核：**键族不相交**（我那 43 条 `Card_Race/*` 全在、无重复、无覆盖），**结果没坏**。

## ⑤ 类型检查结果（原样贴）

中间三次撞上**别人的半成品**（都是我没碰过的文件：`Battle/BattleDriver.cs` 先 2 条 `HitEye`/`ToggleLore`、后 1 条 `LeaveNetRoom`；`RuleEngine/Core/EffectResolver.cs` 7 条 `CS0841` 用了还没声明的局部），每次隔 1–3 分钟重跑即恢复 ⇒ 按 13·3 第 2 条判「不是我的问题」。**我的 5 个文件最后一次跑是 0 错**：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾复核：`CardView.cs` CRLF **5130/5130** · `CardBaseDemo.cs` CRLF **868/868** · `Loc.cs` LF 425 · `DeckSelectionPopup.cs` LF 725 · `CollectionScene.cs` LF 6805（**BOM 仍在**）—— **一个都没被翻**。
