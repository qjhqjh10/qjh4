# 交件 · `Core双语-第一步`（`Core/` 玩家可见中文接语言表 · Tooltip 13 处 + CreatedBy 1 处）

> 写手：第六会话 · 动手代理（白名单：`Core/{Loc,Tooltip,CardView}.cs`）
> 产出时间：2026-10-08。**判据 = `资料/普查产出_第六会话/查证_A1013盲区_Core显示字面量全量普查.md`**
> （下称「普查」）§③3.2 / §③3.3 / §④ / §⑧。
> ⛔ **本笔没跑任何 Unity 自检**（用户本轮口径：待办没做完不跑）；✅ 跑了**秒级类型检查**（`TMPDIR=/tmp/wf_core1`）。

---

## ① 结论

1. **`Tooltip.cs` 的 13 处逐处处理完**：10 条 `TipText` 数值/HUD 文案**全部改走 `Loc.T`**，
   2 条「我们自己加的括注」`Loc.T` 化，1 条（`:494` 的全角括注）**按简报不建键**（但见 §⑤-3：简报给的理由**不成立**）。
2. **12 条新键**建进 `Core/Loc.cs`（块 = `:720-785`）。**键名与原版出处逐条写进那一块的头**。
3. 🔴 **10 条的键名与原版对上了** —— 判据不是 `mTerm`、也不是代码字面量，而是原版那颗
   **`EverguildTooltipTrigger` 的 `text` 字段**（另两条载体对本族**都是 0 命中**，只搜那两条 = 无效否定）。
   全库（`assets_full` 4.4 GB）`"text": "Tips/…"` **只有 14 个 key**，逐条计数已写进 `Loc.cs`。
4. 🔴 **简报里有 8 条判定被现读推翻**（全部有实据，见 §②/§⑤）：`Health` / `Cost` 简报说「原版无、需自拟」，
   实际**原版有键**；HUD 那 5 条简报说自拟（其中 3 条给了 `CardTraitDescription/*`），
   实际原版有**更贴的那一族** `Tips/Hud/*`。
5. **`CardView.cs:1804` 那处白捡的接了** —— 并**按调度台补的 `A985④` 加了 `Loc.HasEntry` 那一跳 + 出声**
   （见 §⑦）。**界面上的字不变**（词条值与原来那两句逐字相同）。
6. **中文档零变化**（脚本逐字节核过，§④）：10 条数值/HUD 的 ZH 列 = 改前字面量；两条括注同样。
   英文档从「悬停出来还是中文」变成印英文。
7. 类型检查 **0/0**；四个文件**行尾一个都没翻**（`Loc`/`Tooltip` 仍纯 LF、`CardView`/`BattleScene` 仍纯 CRLF）。

---

## ② 逐处表（`文件:行号 | 改前 | 键名 | 值来源`）

> 行号 = **本笔改完之后**现读。`Tooltip.cs` 的 `TipText` 那一段因插了注释**整体下移 8 行**
> （普查里的 `:373-384` → 现在 `:381-400`）；**`:494`/`:513`/`:518` 与普查的 `:478`/`:493`/`:496` 逐一对应**。

| # | 文件:行号（改后） | 改前字面量 | 键名 | 值来源 |
|---|---|---|---|---|
| 1 | `Core/Tooltip.cs:381` | `近战攻击力（规则书 :78）` | **`Tips/MeleeAttackTip`** | **键名照原版**（152 颗触发器）· **值两列自拟**（远端 I2） |
| 2 | `Core/Tooltip.cs:382` | `远程攻击力（规则书 :78）` | **`Tips/RangedAttackTip`** | 键名照原版（152 颗）· 值两列自拟 |
| 3 | `Core/Tooltip.cs:386` | `受任何来源的伤害都减这么多，最低减到 1（:167）` | `Tips/ArmourTip` | 🔴 **键名自拟**（原版**没有**护甲 tooltip，全库 14 个 key 里没有 Armour）· 值两列自拟 |
| 4 | `Core/Tooltip.cs:387` | `扣完护甲后扣生命，归零进弃牌堆（:147）` | **`Tips/HealthTip`** | 🔴 **键名照原版**（152 颗；**简报说「原版无、需自拟」⇒ 现读推翻**）· 值两列自拟 |
| 5 | `Core/Tooltip.cs:388` | `打出去要花的能量（规则书 :78）` | **`Tips/CostTip`** | 🔴 键名照原版（**305 颗**；同上推翻简报）· 值两列自拟 |
| 6 | `Core/Tooltip.cs:396` | `每回合恢复，用来打出手牌` | **`Tips/Hud/PlayerEnergyCount`** | 🔴 键名照原版（13 颗；**简报说需自拟 ⇒ 推翻**）· 值两列自拟 |
| 7 | `Core/Tooltip.cs:397` | `本局拿到的战功骷髅数` | **`Tips/Hud/Skulls`** | 🔴 键名照原版（13 颗 · 挂 `Milestones`；**简报说需自拟 ⇒ 推翻**）· 值两列自拟 |
| 8 | `Core/Tooltip.cs:398` | `暗黑天使的任务点进度（0/3）` | **`Tips/Hud/PlayerQPCount`** | 🔴 键名照原版（13 颗；**简报给的是 `CardTraitDescription/questPoints`** —— 那是**卡面 trait 的描述**，不是 HUD 图标那条）· 值两列自拟 |
| 9 | `Core/Tooltip.cs:399` | `战斗修女的阵营资源` | **`Tips/Hud/PlayerFaithCount`** | 🔴 同上（简报给的是 `CardTraitDescription/faith`） |
| 10 | `Core/Tooltip.cs:400` | `灵族的阵营资源；在场也算单位（1 血），点击收集` | **`Tips/Hud/PlayerSpiritStoneCount`** | 🔴 同上（简报给的是 `CardTraitDescription/spiritStone`） |
| 11 | `Core/Tooltip.cs:513` | `"（规则书里没有这个词的条目）"` | `Tips/Trait/NoRulebookEntry` | 🔴 **自拟**（**我们加的**：原版查不到描述时**只有图标 + 标题**、没有正文） |
| 12 | `Core/Tooltip.cs:518` | `"（规则书 :" + t.line + "）"` | `Tips/Trait/RulebookLine`（`{0}` = `t.line`） | 🔴 **自拟**（行号是**我们自己的判据行**，原版没有） |
| 13 | `Core/Tooltip.cs:494` | `title += "（" + en + "）"` | ⛔ **不建键**（照简报：分隔符/括注，跟语档走） | — ⚠️ 简报这条**理由不成立**，见 §⑤-3 |
| — | `Core/CardView.cs:1820`（原 `:1804`） | `$"由 {creatorName} 创建"` / `$"Created by {creatorName}"` | **`Battle/HUD/CreatedBy`**（`Core/Loc.cs:813` **早在表**） | **原版键**（`SupportMethods__GetCreatedByText.c`：`GetTranslation(键).Replace("{0}", 名字)`）· **0 建键** |

**为什么 #4/#5/#6-#10 敢推翻简报**（三条独立实据，都指向同一处）：
1. **原版字段**：`assets_full` 全库 `grep -rho '"text": "Tips/[^"]*"'` ⇒ **只有那 14 个 key**，
   逐条计数 = 挂了几颗触发器（Health/Ranged/Melee 各 152、Cost 305、HUD 各 13）。
2. **两个字段逐字吻合**（= 键与挂点是同一处）：`bundle_staticgeneralassets_assets_all/MonoBehaviour/
   MonoBehaviour_-4253307515847882232.json` 里 `text = Tips/HealthTip` + `tooltipAnchor = 10` +
   `offset.x = 73.05`，而我们 `Battle/BattleDriver.cs:13339` 写的正是
   `Tooltip.Show(TipText.Health, at, 10, new Vector3(73.05f / TipPx, 0f, 0f))`；
   `Cost`(10 / 52.6) · `Melee`(15 / −49.33) · `Ranged`(15 / −53.54) 同样逐字对上。
3. **既有的规范文档**：`资料/tooltip_原版规格与实现.md` §一 的挂点表 + §一 那条「🔴 一条**更正**」
   （它自己写着「`Tips/HealthTip` … **不是预制体名、是 I2 本地化 key**」）。

⚠️ **值（显示串）仍然取不到** —— 远端 I2 表 ⇒ 两列仍标**自拟**（与普查 §⑦1 一致）。

---

## ③ 新键清单（12 条 · 全在 `Core/Loc.cs:720-785`）

| 键名 | ZH 列 | EN 列 | 原版有没有（判据） |
|---|---|---|---|
| `Tips/MeleeAttackTip` | 近战攻击力（规则书 :78） | Melee Attack (rulebook :78) | ✅ 有（`text` 字段 · 152 颗） |
| `Tips/RangedAttackTip` | 远程攻击力（规则书 :78） | Ranged Attack (rulebook :78) | ✅ 有（152 颗） |
| `Tips/HealthTip` | 扣完护甲后扣生命，归零进弃牌堆（:147） | Health is lost after armour; at zero the card goes to the discard pile (:147) | ✅ 有（152 颗；anchor 10 / 73.05 与我们的调用点逐字吻合） |
| `Tips/CostTip` | 打出去要花的能量（规则书 :78） | Energy you must spend to play this card (rulebook :78) | ✅ 有（305 颗） |
| `Tips/ArmourTip` | 受任何来源的伤害都减这么多，最低减到 1（:167） | Reduces damage from any source by this much, to a minimum of 1 (:167) | ❌ **原版无**（`Armour Container` 无触发器）⇒ **键名 + 两列全自拟** |
| `Tips/Hud/Skulls` | 本局拿到的战功骷髅数 | Skulls earned in this match | ✅ 有（13 颗 · 挂 `Milestones`） |
| `Tips/Hud/PlayerEnergyCount` | 每回合恢复，用来打出手牌 | Refills every turn; spend it to play cards from your hand | ✅ 有（13 颗） |
| `Tips/Hud/PlayerQPCount` | 暗黑天使的任务点进度（0/3） | Dark Angels quest point progress (0/3) | ✅ 有（13 颗） |
| `Tips/Hud/PlayerFaithCount` | 战斗修女的阵营资源 | The Battle Sisters faction resource | ✅ 有（13 颗） |
| `Tips/Hud/PlayerSpiritStoneCount` | 灵族的阵营资源；在场也算单位（1 血），点击收集 | The Aeldari faction resource; it also counts as a unit (1 health) on the board - click it to collect | ✅ 有（13 颗） |
| `Tips/Trait/NoRulebookEntry` | （规则书里没有这个词的条目） | (no entry for this term in the rulebook) | ❌ **原版无**（**我们加的**） |
| `Tips/Trait/RulebookLine` | （规则书 :{0}） | (rulebook :{0}) | ❌ **原版无**（行号 = 我们的判据行） |

> **ZH 列 = 改前那几句逐字**（脚本对 `git show HEAD:` 的旧字面量逐字节比过，全 SAME）；
> **EN 列 = 我们照 ZH 译的**（⛔ 无 CJK / 无全角，脚本核过）。
> 🔴 **键名照原版 ≠ 值照原版**：那 10 条原版键的**显示串在远端 I2**，两列都仍是我们写的（`Loc.cs` 那块头上写明）。

---

## ④ 验证

**类型检查（秒级 · `TMPDIR=/tmp/wf_core1 bash d:/4/Unity/工具/typecheck.sh`）**
- 改前 baseline：`运行时错误数: 0` / `编辑器错误数: 1` —— 那 1 条在
  **`Assets/CardPresentation/Editor/EndPanelProbe.cs(106,17)` CS0161**，**不是本笔的文件**（别人的半成品）。
- 改后：`运行时错误数: 0` / `编辑器错误数: 0`（那一半成品也编过了）。

**行尾（二进制数 `\r\n` vs `\n`，⛔ 没用 `sed -i`、⛔ 没用文本模式写）**

| 文件 | 改前 | 改后 |
|---|---|---|
| `Core/Loc.cs` | CRLF 0 / LF 1908 | **CRLF 0 / LF 1975** |
| `Core/Tooltip.cs` | CRLF 0 / LF 500 | **CRLF 0 / LF 522** |
| `Core/CardView.cs` | CRLF 5350 / LF 5350 | **CRLF 5376 / LF 5376**（**纯 CRLF，没翻**） |
| `Editor/BattleScene.cs` | CRLF 19631 / LF 19631 | **CRLF 19631 / LF 19631**（同上） |

**`git diff --numstat`（相对 HEAD）**
```
28      2       Unity/MyGame/Assets/CardPresentation/Core/CardView.cs
232     7       Unity/MyGame/Assets/CardPresentation/Core/Loc.cs
37      15      Unity/MyGame/Assets/CardPresentation/Core/Tooltip.cs
538     9       Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
```
⚠️ `BattleScene.cs` 那个 **538/9** 里**绝大多数是【上一会话未提交的改动】**（本会话开工前它就已经是 `M`）。
**本笔只占一个 hunk**：`@@ -10952,7 +11010,24 @@`（7 个 hunk 里剩下的 4641 / 8650 / 10305 / 11353 / 15544 都不是本笔）。

**键存在性 / 文案（脚本自检，可复跑 —— 对应验收标准 3）**
- `Loc.cs` 表内条目数：**425**（⛔ 普查写 408、本会话开工前重核 = **413** ⇒ 期间有人加过 5 条；**+12 条本笔的 = 425**）。
- `Tooltip.cs` 里出现的 `Loc.T/HasEntry/EnOf` 键：**12 个**，**不在表里的 = 0 条**。
- `CardView.CreatedByTerm = "Battle/HUD/CreatedBy"` ⇒ **在表内 = True**。
- 12 条新键：ZH/EN 两列**都非空**、EN 列**含 CJK/全角字符 = 0**。
- **中文档零变化**：10 条数值/HUD 的 ZH 列与 `git show HEAD:…/Tooltip.cs` 的旧字面量**逐字节相同**；
  `Tips/Trait/NoRulebookEntry` 相同（14 字）；`Tips/Trait/RulebookLine` = 旧 `"（规则书 :" + t.line + "）"`
  的两段 wrapper 中间夹 `{0}`，**逐字节相同** ⇒ 中文档渲染出来的字一模一样。

---

## ⑤ 没查清 / 停手的

1. 🔴 **那 10 条原版键的【值】取不到**（原版显示串在**远端 I2 表**）⇒ 只证到键名，**没证到值**。
   两列一律自拟，并在 `Loc.cs` 那一块头上如实标。搜过：`assets_full` 全库 `mTerm`（488 条，`Tips/` **0 条**）、
   `tools/il2cpp_out/stringliteral.json`（26,507 条，`Tips/` **0 条**）、触发器 `text` 字段（14 条 key，无 value）。
2. 🔴 **`Tips/Hud/*` 原版【敌我各一条键】**（`Player…Count` / `Opponent…Count`），而我们的消费点
   （`Battle/BattleDriver.cs:13287-13290`）把**同一个串同时用在双方图标上** ⇒ 本批只取 `Player*Count` 一条、
   两列写**对双方都成立的中性说法**。**要拆成真正的两条得改 `BattleDriver`（不在本笔白名单）⇒ 停手交了。**
3. 🔴 **`:494` 的全角括注：简报给的理由（「分隔符/句读符**跟语档走**」）现读【不成立】** ——
   `Core/CardText.cs` 的 `KeywordZh` **没有语言闸**（`if (string.IsNullOrEmpty(...)) return null;` 直接查表返回中文），
   而 `TipText.Trait` 是 `zh = KeywordZh(key)` ⇒ **英文档下标题照样是「护甲（Armour）」**。
   ⇒ 就算把括注建了键，也只治好一个标点、治不好「英文档下标题是中文」那个**根**；而那个根在
   `Core/CardText.cs`（**本批禁区**）⇒ 本笔**照简报不建键**，把这一条**留给你裁**
   （要修就得动 `CardText` 那条语言闸，属 `A1084` 第二步那一笔）。
4. ⚠️ **`Tips/Hud/PlayerQPCount` 的 `（0/3）` 是硬编码在我们旧文案里的** —— 原版那个「3」是不是常量、
   有没有 `{0}` 占位，**没查**（普查 §⑦4 也标着没查）⇒ ZH 列**原样保留**旧文案，没敢动。
5. ⚠️ **「原版 `Armour Container` 真的没有 tooltip」** —— 本笔核了两条：① `assets_full` 全库
   `text` 字段里 **0 个 Armour**；② `资料/tooltip_原版规格与实现.md` §一 的挂点表（判据 = `子代理读报_2dcard_0827.md:95`）。
   **没有再跑原版实况**（真 Play 攒着）。
6. ⚠️ 普查记的两个 md5 **本会话开工时复核过、都对得上**（`Tooltip.cs` = `017f75ec…`、
   `CardView.cs` = `6b1d587e…`）⇒ **这两个文件本批没漂**；行号也与普查逐条对上（`:373-384/478/493/496`）。
   `Loc.cs` **漂了**（408 → 413，见 §④）。

---

## ⑥ 顺手发现的

1. 🔴 **`Core/Tooltip.cs` 的 `TipText.Armour` 是全仓【零消费点】**（`grep -rn "TipText.Armour"` = 0 命中；
   `BattleDriver`/`DeckRuntime` 那两处 tooltip 分派里**都没有护甲那一档**）——
   这与「**原版 `Armour Container` 本来就没有 tooltip**」自洽（`BattleDriver.cs:13255-13256` 明写着「我们也不给它做」，
   `Editor/BattleScene.cs` 还有一条「**护甲上没有 tooltip**」的断言）。
   ⇒ 本笔仍给它建了键（同一族五条形状一致 + 不静默），**但它是个死键**（如实记，别当成「原版有」）。
2. 🔴 **`BattleDriver.cs:13287-13290` 的敌我用同一个串**（见 §⑤-2）—— 原版是
   `Tips/Hud/{Player,Opponent}{Energy,Faith,SpiritStone,QP}Count` 八条，我们压成了四条。
   这是**真差异**（不是本笔造成的），建议进 A 表、由能改 `Battle` 的那一笔做。
3. 🔴 **`CardText.KeywordZh` 没有语言闸**（见 §⑤-3）⇒ **英文档下关键词 tooltip 的标题是中文**
   （`TipText.Trait` 用的就是它）。这不是本笔引入的，但**本批把它照出来了**（因为我给它旁边那两句接了语言表）。
4. 📌 **`Editor/BattleScene.cs` 的 §W6 键清单（`:8874-8898`）没收本笔这 12 条键** ——
   它是一张**手写清单**（不是遍历整表），所以本笔的键**不会被它核到**。
   ⇒ **建议**：下一波「断言宿主」把 `Tips/Trait/NoRulebookEntry` / `Tips/Trait/RulebookLine`（这两条**已上屏**）
   补进去；`Tips/*` + `Tips/Hud/*` 那 10 条被 `Tooltip.ShownBody == TipText.X` 那几条间接覆盖着，可选。
   ⛔ 本笔**没动它**（没红，且不在白名单的口径内）。
5. 📌 **`Loc.cs` 的「`new Entry(` 条数」不是稳定判据** —— 普查写 408、我开工时现读 413（**5 条在我开工前就被别人加了**）。
   下次引用这张表**一律写「现读」**，别抄数字。
6. 📌 **本族「报原版没有」的坑**：`Tips/*` 只在**触发器组件的 `text` 字段**里，`mTerm` 与代码字面量**两路全 0 命中**
   —— 只搜那两路会得出**无效否定**（普查 §④ 把 `Health/Cost/Energy/Skulls` 记成「原版无」就是这么来的）。
   已写进 `Loc.cs` 那一块的头（含可复跑的 `grep` 命令）。

---

## ⑦ 调度台中途补的 `A985④`（`CardView.CreatedByLine` 的 `HasEntry` 那一跳）**已按口径做完**

`A985④` 原话：「`CardView.CreatedByLine` 是【惰性的】—— 它硬拼、**没有 `Loc.HasEntry` 这一跳** ⇒
`Loc.cs` 加了条目**界面一个字都不会变**」。三件事逐条落地：

| 要求 | 落地 | 位置 |
|---|---|---|
| ① 走 `Loc.T("Battle/HUD/CreatedBy")`（`{0}` 用 `Replace`） | ✅ `Loc.T(CreatedByTerm).Replace("{0}", creatorName)` | `Core/CardView.cs:1819-1820` |
| ② **补 `Loc.HasEntry` 那一跳 + 出声**（键在表用词条、不在退回硬拼，⛔ 不静默） | ✅ `if (Loc.HasEntry(CreatedByTerm)) …` 否则 `Debug.LogWarning` + **退回原来那两句硬拼** | `Core/CardView.cs:1819-1845`（出声去重位 = `:1801 static bool _warnedCreatedByKey`） |
| ③ 订正 `:1792-1799` 那段过期注释（铁律 5） | ✅ 「📌 等 `Core/Loc.cs` 收了这条键…」已就地改成「✅ 2026-10-08 已接上…（原话已兑现，就地删掉）」 | `Core/CardView.cs:1806-1813` |

**「出声」为什么带去重**：`CreatedByLine` 由 `FillCreatedBy` **每张卡每次刷新**调（手牌 / 场上 / 展示窗都走它）
⇒ 逐次出声会把日志刷爆（同 `Core/Loc.cs` 的 `_warnedMissing` 那条理由）⇒ 用 `_warnedCreatedByKey`
**只出声一次**（`Debug.LogWarning`，措辞含「补词条 → `Core/Loc.cs`」）。
**顺手加了 `public const string CreatedByTerm = "Battle/HUD/CreatedBy";`**（`:1796`）——
照同族 `BattleDriver.HandCountTerm` / `MulliganPanel.DoneTerm` 那种 `…Term` 常量的形状，
**只为不把字面量写两遍**，也顺手给了自检一个钩子（`Loc.HasEntry(CardView.CreatedByTerm)`）。

---

## 摘要（≤300 字）

`Tooltip.cs` 13 处按普查逐处处理完：10 条数值/HUD 文案 + 2 条自加括注改走 `Loc.T`，1 条全角括注按简报不建键；
`CardView.cs` 的 `CreatedByLine` 接上早在表的 `Battle/HUD/CreatedBy`，并按调度台补的 `A985④` 加了
`Loc.HasEntry` 那一跳（键不在就退回硬拼 + 一次性 `LogWarning`，⛔ 不静默）、订正了过期注释。
`Loc.cs` 新建 **12 条键**（`:720-785`），**中文档零变化**（旧字面量逐字节核过）。

🔴 **推翻简报 8 条**：本族键名唯一判据是原版 `EverguildTooltipTrigger.text` 字段
（`mTerm`/代码字面量两路全 0 命中）。全库仅 14 个 key ⇒ `Health`/`Cost` **原版有键**（简报说自拟），
HUD 那 5 条的键是 **`Tips/Hud/*`**（简报给的 `CardTraitDescription/*` 是卡面 trait 的描述，不对口）。
`ArmourTip` 键名自拟（原版无护甲 tooltip，且我们零消费点）。值仍在远端 I2 ⇒ 两列全自拟。

验证：类型检查 **0/0**；四文件行尾**一个没翻**；键存在性脚本 **缺键 0**；`git diff --numstat` 已列。
未做/待裁：敌我 HUD 分键（要改 `BattleDriver`）· `:494` 括注背后 `CardText.KeywordZh` 无语言闸（禁区）。
