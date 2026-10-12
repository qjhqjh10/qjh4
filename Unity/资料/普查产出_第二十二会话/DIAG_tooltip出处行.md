# DIAG tooltip 的「出自」行

## 一、结论（2–4 行）

1. **它在**：`Core/Tooltip.cs:540`（关键词 tooltip 正文末尾拼的 `（规则书 :167）`），键 `Tips/Trait/RulebookLine`（`Core/Loc.cs:999`），
   行号值来自生成物 `Resources/trait_tips.json` 的 `line` 字段（`工具/gen_trait_tips.py:102`，源 = 规则书关键词表第 4 列）。
2. **原版没有这一行** —— 反编译 `TextTooltipController__GetTraitTooltip.c` 只出 **title + body**（两段），
   解包预制体 `Trait Tooltip` 的子节点只有 `Background / TraitImage / Line / Description / Title`，**单一正文节点、零出处节点**。
   ⇒ **这是我们自加的**（`Loc.cs:995`、`Tooltip.cs:538` 两处注释自己写着「原版没有，是我们加的」），**该删**。
3. 🔴 **同一族还有一份没被点名**：卡面四个数值 tooltip（近战/远程/生命/费用/护甲）的**值里也带出处**
   （`Loc.cs:968/:969/:971/:970/:976` 的「（规则书 :78）」「（:147）」「（:167）」）—— **同一条规则、同一类多余箭头**，一并处理。

## 二、证据

### 2.1 我们这条链（文件:行号 + 实际字符串）

| 位置 | 内容 |
|---|---|
| `Core/Tooltip.cs:540` | `string line = string.IsNullOrEmpty(t.line) ? "" : Loc.T("Tips/Trait/RulebookLine").Replace("{0}", t.line);` |
| `Core/Tooltip.cs:541` | `return "<b>" + icon + title + "</b>\n" + t.body + line;` ← 出处**无分隔符直贴正文尾** |
| `Core/Tooltip.cs:431` | `class TraitTipRow { public string zh; public string en; public string body; public string line; }` |
| `Core/Loc.cs:999` | `{ "Tips/Trait/RulebookLine", new Entry("（规则书 :{0}）", "(rulebook :{0})") }` |
| `工具/gen_trait_tips.py:102` | `"entries": [{"zh": k, "en": v["en"], "body": v["body"], "line": v["line"]} …]`（源列 = md 第 4 列「出处行号」，`:64` 取 `p[3]`） |
| `Resources/trait_tips.json` | 61 条**每条都带** `"line"`；样例：`{"zh":"护甲","en":"Armour","body":"所受任何来源伤害减少 X（最低 1）","line":"167"}` |

**实际印出来的样子**（自检里抓的原文，`BattleScene.cs:11735`）：`护甲 所受任何来源伤害减少 X（最低 1）（规则书 :167）`。
🔴 `trait_tips.json` 是**生成物**（文件自己写着「别手改」）⇒ 只能改生成器 + 重跑，⛔ 不许手改 json。

### 2.2 原版判据

- `d:/2/tools/decomp_full/TextTooltipController__GetTraitTooltip.c` —— 只凑两段：
  `uVar4 = GameStaticData__TraitNameToStringNoImage(...)`（标题）+ `uVar5 = ModifyLocalization(GameStaticData__GetTraitDesc(...))`（描述），
  再挂 `CardTraitCollection__GetTrait(...)` 的 sprite（`EverguildTraitTooltipItem` 比基础版多的就是这颗图）。**没有第三段**。
- `d:/2/新解包资源/assets_full/bundle_duplicateassetisolation_assets_all/GameObject/Trait Tooltip.json`
  （根 RectTransform `RectTransform_-848261165293194823.json`）**恰 5 个子节点**：
  `Background`(3 组件) · `TraitImage`(3) · **`Line`(3)** · `Description`(**4 = 多一个 TMP**) · `Title`(4)。
  ⇒ `Line` 与 `Background/TraitImage` **组件数相同** = 纯 `Image` 分隔线，**不是文字节点**；正文只有 `Description` 一处 ⇒ **原版没有出处行**。

## 三、改动清单（逐项 / 依赖顺序）

| # | 文件:行号 | 改什么 | 顺序 |
|---|---|---|---|
| 1 | `Core/Tooltip.cs:538-541` | 删 `line` 那两行；`return` 收成 `"<b>"+icon+title+"</b>\n"+t.body`。顺带改 `:487` 的 doc（「+ 出处行号」这句要删） | ① |
| 2 | `Core/Tooltip.cs:431` | `TraitTipRow.line` 字段 —— 与 #5 同进同出（json 若不再出 `line`，字段也没用了；`JsonUtility` 对多余 key 不报错，删字段本身不炸） | ② |
| 3 | `Core/Loc.cs:999` | 删键 `Tips/Trait/RulebookLine`（连 `:991-998` 那段注释里的 `· RulebookLine` 一条） | ② |
| 4 | `Editor/BattleScene.cs:11734-11735` | 断言 `arm.Contains(":167")` **必须改**（否则删完立刻红）——改成「有 `护甲` + 正文，且**不含** `规则书`」 | ③ |
| 5 | `Editor/BattleScene.cs:9522-9526` | `w6Keys` 数组里删 `"Tips/Trait/RulebookLine"`（否则 `Loc.HasEntry` 那 2 条红 + 英文档取不到真文案再红 1 条） | ③ |
| 6 | `工具/gen_trait_tips.py:102`（+ `:64`/`:76`/`:83-85`） | `entries` 不再输出 `"line"`；`:83-85` 的 `no_line` 警告可留（仍是「表缺列」的健康检查） | ④ |
| 7 | `Resources/trait_tips.json` | **重跑** `PYTHONIOENCODING=utf-8 python 工具/gen_trait_tips.py --write`（⛔ 别手改） | ④ |
| 8 | `Core/Loc.cs:968/:969/:971/:970/:976` | **同族第二处**：把值里的「（规则书 :78）」「（:147）」「（:167）」删掉（`Health :970` / `Armour :976` 是**裸 `（:NNN）`**，别漏）。⚠️ 这几条**只改值、不删键** ⇒ `BattleScene.cs:11670/11677/11679/11681`、`DeckScene.cs:6127` 那几条「`ShownBody == TipText.X`」**不会红** | ⑤ |
| 9 | `资料/tooltip_原版规格与实现.md:98-100`（§五） | 正本文档里「出处行号」那条描述跟着改（铁律 5：文档=当前认知） | ⑥ |

⚠️ **不影响的地方**（已核，别去改）：`BattleScene.cs:11731` 的 `TraitCount == 61`（只数条数）· `:11798-11804` 折行不变量（拿 `Trait()` 拼长串）· `:11843` 端到端比对（两边同函数）· `Loc.EntryCount >= 429` 基线（现表实 **601** 条 → 删 1~2 条仍 ≥429，三条宿主都安全）· `资料/关键词图标/_规则书关键词表.md` 的「出处行号」列**保留**（那是内部判据正本、不上屏）。
⚠️ `Tips/Trait/NoRulebookEntry`（「（规则书里没有这个词的条目）」）**不是出处**，本次**不动**。

## 四、没查清的部分

1. **没在真机上复现过那一帧**（本代理只读，未进游戏）⇒ 「用户看到的多印那一段」**是不是** `:540` 这条已由文案逐字反推 + 自检原文佐证，但没有截图坐标级实据。
2. **用户说的「关键词图标」是否也含卡面四个数值图标**（近战/远程/生命/费用）—— 若是，改动清单第 8 项就是同一次必修；若只指关键词，第 8 项仍成立（同一条规则），只是可以排后面。
3. `Loc.AllChinese()` 语料会因删键少两个汉字（`规则书`），**未跑该断言**确认无影响（该断言是「字形覆盖率」正向检查，删字不会红）。
