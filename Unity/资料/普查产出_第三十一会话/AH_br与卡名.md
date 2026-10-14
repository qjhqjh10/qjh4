# `AH` · 两件真缺陷：① `descZh` 里的 `<br>` ② `TAU51` 卡名多一个尾巴 `t`

> 写手：`AH` 笔（`AD` 顺手查出来、主对话按铁律 14 插队的两笔）。
> ⛔ 本笔**未动 git** · **未动正本**（四份 + `资料/普查产出_第三十会话/**`）· **未跑任何 Unity 批处理**（用户口径：自检先等等）。
> ✅ 跑过：**秒级类型检查**（运行时 0 错 / 编辑器 0 错）· `python -I -X utf8 工具/gen_cards_engine.py`（**rc=0**，两次）·
> `… --check`（**一致 ✅**）· `bash 工具/ruleprobe.sh check`（读数见 §五）。
> 读数时刻 ≈ **2026-10-15 01:45–02:05**。

---

## 〇、一句话结论（两主题各一行）

| # | 件 | 状态 | 一句话 |
|---|---|---|---|
| **一** | `descZh` 里的 `<br>` | ✅ **做完**（但**不是**简报写的那件） | 🔴 **`<br>` 根本不会「原样印出来」** —— 是 **TMP 自己的解析器**把它换成**换行**，而卡面走的就是 TMP。**`AD` 那条「会原样印出来」的判据不成立**（错因 = 只 grep 了我们自己的代码）。⇒ 真正的缺陷是**`AD` 为此绕开**：`ASH48` 那半句被写成了 `。`（**少一个换行**），**已改回官方写法 `。<br>`** |
| **二** | `TAU51` 卡名 | ✅ **做完** | 三源一致（官方 `Card_Name/TAU51` · `card_ids.json` 真值映射 · PnP 卡面亲读）都写 **`Saviour Protocols`**，我们多一个尾巴 `t`。落点 = `card_stats.json` 的 `name` 字段（**只有这个字段错**，同一行的 `ocrName`/`subtitle` 一直是对的）⇒ 已改正并同步两张按卡名查的表 |

**卡池**：`1127` 张 · `v9` · md5 改前 **`64c21c901ade424b34ad0d0baf0298af`** → 改后 **`83a37522c2c4839e52c4d9fde80a463b`**
（改前那个 md5 与 `AD_descZh两代.md` §七 记的**逐字相同** ⇒ 起点核对了）。
🔬 **改动面是逐卡比出来的**（⛔ 不是推的）：两次产物逐卡 dict 比对 ⇒ **全池只有 2 张变、id 集合逐字相同**：

| 卡 | 变的是哪个字段 | 改前 → 改后 |
|---|---|---|
| `ASH48 Cosmic Serpent` | `descZh` | `…需要灵魂石的能力。②重复此效果` → `…需要灵魂石的能力。**<br>**②重复此效果` |
| `TAU51`（`Saviour Protocols`） | `name` | `Saviour Protocolst` → `Saviour Protocols` |

---

## 一、主题一：`<br>` 现在会印成什么（**实测/判据**，⛔ 不是常识）

### 1·1 一句话：**它是一个换行，不会印出那四个字符**

`<br>` **根本不经过我们的代码** —— 是 **TMP（TextMeshPro）自己的文本预处理器**把它换成一个换行字符。
判据（全部现读，可复核）：

| # | 判据 | 位置 | 读到什么 |
|---|---|---|---|
| ① | **TMP 的主解析路** | `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs:2277-2296` | `PopulateTextProcessingArray()`（`ParseInputText()` 调的）里 `if (c == '<' && m_isRichText)` 之后 `case MarkupTag.BR:` ⇒ 写一个 `elementType = TextCharacterElement, length = 4, unicode = 10` 的字符元素，`readIndex += 3` ⇒ **换成 LF** |
| ② | **字面量表的定义** | 同目录 `TMP_RichTextTagsCommon.cs:133` | `BR = 2256,   // <br> Line Feed (LF) \u000A` |
| ③ | 第二处（与①同语义，`<style>` 定义内部） | `TMP_Text.cs:2969` | `InsertTextStyleInTextProcessingArray()` 里 `case MarkupTag.BR: charBuffer[writeIndex].unicode = 10;` |
| ④ | **我们的卡面确实走 TMP** | `CardPresentation/Battle/BattleDriver.cs:12717-12757`（`FaceTextFull`）→ `CardPresentation/Core/CardView.cs:2340-2341`（`Fill(..., d.keywords, …)`） | 卡面那行效果文字挂的是 `TextMeshPro _keywords`（`CardView.cs:648`），`Fill` 里就是 `t.text = text;` ⇒ **原样交给 TMP**，中间没有任何一道会动 `<br>` 的加工 |
| ⑤ | **`richText` 没被关** | 全仓 `grep richText` = **0 命中**；`TmpFont.NewText`（`Core/TmpFont.cs:333-360`）不设它 | TMP 默认 `richText = true` ⇒ ① 那一路是**活的** |

### 1·2 原版口径：**原版也一样**（第一权威 = 全量反编译）

- 🔴 **`d:/2/tools/decomp_full/` 里 `<br>` 命中 0 处**（`grep -rn '<br>'` 全库 0）⇒ 原版 C# 侧**根本不处理它**。
- 原版唯一那道文本加工是 `SupportMethods.ModifyLocalization(string)`（四支：`((Key,a,b))` / `$$ID$$` /
  `[[枚举名]]` / `(N)`，逐支出处见 `Core/CardIcons.cs:399-437` 那段文件头）—— **四支里没有一支碰 `<br>`**
  ⇒ 它的产物照样带 `<br>` 进 TMP ⇒ **原版语义 = 换行**。
  （旁证，**本笔现读**：官方表 `数据/游戏数据/官方本地化_en+zh-CN.json` 共 `9441` 条，含 `<br>` 的值 **172** 条；
  `Card_Description/*` 共 `1098` 条、含 `<br>` 的 **103** 条（其中 EN 列 102）。
  ⚠️ 本笔**不引用** `资料/交接_第三十一会话.md:326` 那个 `153` —— 那是别人量的另一把尺子，口径没写清。）
- 🔴 **铁律 7 人眼判据（成品卡面印的就是两段）**，两张亲读：
  - `d:/2/Warpforge部队卡片/Aeldari/3部队/Warpforge_7_Guardian-Defender.png` —— 卡面印
    `〔图〕Waystone.` / `〔图〕Shuriken 2` **两行**，而**第一行右边还空着一大片** ⇒ 是**显式断行**、
    不是折行兜出来的。（同一张卡的 `descZh` = `[[waystone]]。<br>[[shuriken]] 2`）
  - `d:/2/Warpforge部队卡片/Aeldari/4计策/Warpforge_52_Cosmic-Serpent.png` —— `of all your troops.`
    之后另起一行 `② Repeat this effect`（同样，上一行右边有空白）。

### 1·3 那么**真正的**缺陷是什么：`AD` 的「绕开」写错了

`AD` 因为判成「渲染不出来」，给 `ASH48` 补中文时**故意用了 `。` 而不是官方那个 `<br>`**
（官方 `Card_Description/ASH48_new` = `…troops.<br>((Spirit_x,2)) ((Repeat_this_effect))`）。
⇒ 卡面会把那两段**并成一行**（少一个换行），与官方写法、与 PnP 卡面（两段）都不符。
**本笔已改回 `。<br>`** —— 卡面那道工序本来就认得它。

### 1·4 🔴 **`<br>` 唯一真会「丢」的地方（如实报，本笔不改）**

`CardIcons.StripTags`（`Core/CardIcons.cs:165-169`）的正则是 `</?[a-zA-Z][^>]*>` ——
**`<br>` 会被它整段删掉、且不补换行**（现算：`StripTags("触发…。<br>②重复此效果")` = `"触发…。②重复此效果"`）。
用到它的三处：`Label.SetTextDot`（点阵兜底后端）· `CardView.cs:5711-5716`（卡面点阵兜底）·
`BattleLogPanel.RowText:307`（**自检用**的「可见文字」口）。**为什么不改**：
① 点阵后端本来就不认识任何标签、也**画不出断行**（`Core/TextCanvas.cs:150-196` 的 `DrawWrapped` 只按空格贪心折行，
`Draw` 对查不到字形的字符**只跳格不留痕** ⇒ 就算把 `<br>` 换成 `\n` 也只是多一个空格宽的空档，**不是换行**）；
② 那三处里 `RowText` 是**自检口**（判「玩家看到的那句」），`SetTextDot`/`5716` 是 **TMP 取不到时的兜底**；
③ 原版没有对应的东西可照（原版只有 TMP 一条路）⇒ 照铁律 2「查不到就别照着猜」，**留原地 + 出声**。

### 1·5 ⚠️ 顺带发现（**只报，⛔ 没改**）：`desc`（英文）那一侧同样少了这些断行

现算：`descZh` 里带 `<br>` 的 **53 张**（改前 52 + `ASH48`）里，**官方 EN 有 51 张也带 `<br>`**，
而我们的 `desc`（PnP 那一代）**只有 7 张**带换行（`ASC64` 那类剩下的写成 `. ` 或干脆接在一行）。
⇒ **英文档下这 46 张的卡面会把两段并成一行**（`. ` 分隔），而 PnP 印的是两行。
判据同 §1·2 那两张卡图。**为什么本笔不动**：简报点名「⛔ 别一次改一大批」，而且 `desc` 那一列的分段写法
本身要与 PnP 逐张核（52 张 × 人工），属 `A970` / 卡面线；**本笔只立账**。

---

## 二、主题一：改了哪些文件哪些行

| 文件 | 位置 | 改动 |
|---|---|---|
| `Unity/数据/游戏数据/cardface_fixes.json` | `descZh` 对象里 `"Cosmic Serpent"` 那条 | `…需要灵魂石的能力。②重复此效果` → `…需要灵魂石的能力。<br>②重复此效果`（**只加了 4 个字节 `<br>`**） |
| 同上 | 顶层第 1 个键 | 新增说明键 `_2026-10-15_AH_br与卡名`（判据链 / 那条更正 / 卡名三源 / `art` 字段为什么不跟着改） |
| `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | ②·D 块之后、②·C 之前 | 新增 **②·E** 一段（+94 行）：三条断言（见 §三） |
| 生成物 `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` | 重跑覆盖 | `ASH48.descZh` 变更（与 `TAU51.name`，见 §四） |

- 行尾：`cardface_fixes.json` 改前 **CRLF 1911 / LF 1911**（纯 CRLF）→ 改后 **CRLF 1912 / LF 1912**（**仍纯 CRLF**，+1 行 = 那条说明键），
  `190648 → 192482` 字节（+1834 = 4（`<br>`）− 1（`Protocolst`→`Protocols`）+ 1831（说明键））
- `BattleScene.cs` 改前 **CRLF 23363 / LF 23363**（纯 CRLF）→ 改后 **CRLF 23457 / LF 23457**（**仍纯 CRLF**），`1978260 → 1985281` 字节
- 🔴 全部用 **python 字节级 `wb` 拼接**（⛔ 不是 `sed -i`）· 每处替换前后**都打了计数并 assert == 1**、行尾**逐次数出来对**（见 §五 日志）
- 三份 JSON 都用 `json.loads` 回读校验通过

---

## 三、主题一：断言（含判别式）——落在 `CardPresentation/Editor/BattleScene.cs` 的 **②·E**

⚠️ **落点为什么选这里**：① 这段的宿主 `BattleScene.Run` **就是渲卡面的那一个**（②·B / ②·D 已经在这里证过
`CardIcons` 的 sprite asset 与 TMP 字体**是活的**）；② `RuleEngine/Editor/RuleEngineTest*.cs` 在**别人手上**
（简报点名不许碰），而 `AH` 这两笔的判据一条在渲染层、一条是卡池数据 —— 放这里两条都够得着。

| # | 断言 | 判别式（**结构上不可能同时满足**） |
|---|---|---|
| **前提** | 卡池 `descZh` 里带 `<br>` 的卡 **> 20**（现 53 张） | 少了这一条，下面那条「一处都不许少」在全池没有 `<br>` 时**也会绿 = 空转** |
| **①** | **全池** `descZh` 过 `CardIcons.Rewrite` 之后，`<br>` 的处数**一处不许少**（改前/改后逐个报，首例点名） | 谁往链子里加一刀把 `<br>` 剥掉/转义（`&lt;br&gt;`）/折行时掐断它 ⇒ 处数掉 ⇒ 红；谁把「一律剥掉」那类加工搬上卡面这条路 ⇒ 红 |
| **②** | 拿**真的 TMP** 量 `lineCount`：`"AB"` = 1 · `"A<br>B"` = 2 · 且 `"A<br>B"` 与 `"A\nB"` **同粒度**（都 2） | `<br>` 被剥掉/转义/掐断 ⇒ `lineCount` 回落成 1 ⇒ **② 与 ① 一起红**（两条不可能被同一份实现同时满足）；谁把 TMP 的富文本关了 ⇒ ② 红（`<sprite>` 那批也一起红） |
| **③** | `TAU51` 的 `Name` 必须是 `Saviour Protocols`；**且全池不许再出现 `Saviour Protocolst`** | 把拼写写回带 `t` 的那一版 ⇒ 第 2 条立刻红（两条不可能同时成立） |

**② 的建法**（照 `Core/TmpFont.MeasureGlyph` 那一处，逐项相同）：世界空间 `new GameObject { hideFlags = HideAndDontSave }`
+ `AddComponent<TMPro.TextMeshPro>()` + `font = TmpFont.Font` + `fontSize = 100f` + **`textWrappingMode = NoWrap`**
（⇒ 行数**只可能**由 `<br>`/`\n` 这种显式断行造成、折行不参与）+ `ForceMeshUpdate()` ⇒ 读 `textInfo.lineCount`，
收尾 `DestroyImmediate`。⚠️ **不显式设 `richText`** —— 要断的就是**默认那一档**（原版也没关它）；
⚠️ `TextWrappingModes` 是 `TMPro` 下的**顶层枚举**（`TMP_Text.cs:100`），本文件没有 `using TMPro;` ⇒ 写成 `TMPro.TextWrappingModes.NoWrap`
（**第一版写成裸名 ⇒ 类型检查报 `CS0103`，已修**）。

> 🔴 **本笔【没跑】这三条断言** —— 简报明写「⛔ 不许跑 Unity 批处理」（用户口径：自检先等等）⇒
> **它们只过了类型检查、还没在真宿主里跑过**。本笔已把三类可能的假红逐个排除（都算过）：
> ① 卡池 53 张 `<br>` 全部**有官方记号或走我们自造那张表**，而**没有任何一个计划表 token 与 `<br>` 有交集**
> （现扫 874 张计划、**0 条**）；② `NoWrap` ⇒ `lineCount` 不受版面宽度影响；③ `TAU51` 现读 `name` = `Saviour Protocols`、
> 全池 `Saviour Protocolst` **0 张**。⇒ 交调度台在下次收口时**实跑一次**确认。

---

## 四、主题二：`TAU51` —— 三源读数 + 同族清单

### 4·1 三源（**逐源现核，⛔ 不是转述**）

| 源 | 怎么读的 | 读到什么 |
|---|---|---|
| **官方本地化表** | `数据/游戏数据/官方本地化_en+zh-CN.json` 的 `Card_Name/TAU51`（现读） | `["Saviour Protocols", "救世协议"]` ⇒ **`Saviour Protocols`**；另有 `PackName/TAU_SaviourProtocols` = 同一个名字（**没有** `_new` 键） |
| **原版真值（游戏侧）** | `数据/游戏数据/card_ids.json` 的 `mapping["TAU51"]`（现读；该表 note 自称「整表换成【原版真值】判据 = 原版卡 SO 的 `uniqueId` → `cardName`」） | `Saviour Protocols` ⇒ **`Saviour Protocols`** |
| **PnP 成品卡面** | `d:/2/Warpforge部队卡片/Tau/4计策/Warpforge_63_Saviour-Protocols.png`（**开图亲读**，铁律 7） | 卡面印 **`Saviour Protocols`**（费用 4、紫枪 2 档的数值与卡池吻合） |
| **我们的源表** | `数据/游戏数据/card_stats.json` 里 `name` = `Saviour Protocolst` 那条 | 🔴 **只有 `name` 这一个字段错**；**同一行的 `ocrName` 与 `subtitle` 都是 `Saviour Protocols`** |

🔑 **为什么卡 id 一直是 `TAU51`（没跟着错）**：`card_stats.json` 的 `name` 只是**显示名**；
id 那一层走 `pick_truth_id`（`工具/gen_cards_engine.py:739-777`），候选 = ①官方英文名 ②真值 `m_Name` 尾巴 ③真值 `cardName`，
**名字先用 `name`、再退 `ocrname`** ⇒ `name` 查不中时**落到 `ocrName` = `Saviour Protocols`** ⇒ 真值索引
`card_id_truth_index.json` 的 `by_official_en/by_tail/by_name["saviourprotocols"]` = `["TAU51", …]` ⇒ 命中 `TAU51`。
⇒ **改 `name` 不会连带换 id**（现测：改后 `TAU51` 的 `id` 仍是 `TAU51`）。

### 4·2 落点与同步（一张表都不能漏）

| 文件 | 改动 | 为什么必须跟着改 |
|---|---|---|
| `Unity/数据/游戏数据/card_stats.json` | `"name": "Saviour Protocolst",` → `"Saviour Protocols",`（**L27905**，全文件该串 2 处，**锚点给全行**故只命中 1 处） | **源表**；生成器的 `name` 列就取它 |
| `Unity/数据/卡牌翻译/zh_cards.json` | 键 `"Saviour Protocolst"` → `"Saviour Protocols"` | 生成器按**卡名**查它（官方表查不到时的兜底）⇒ 不改就是**孤立键** |
| `Unity/数据/游戏数据/cardface_fixes.json` | `descZh` 对象里那条键改名 | **同一张按卡名查的表** ⇒ 不改 ⇒ `AD` 补的那条中文**静默失效**、且生成器会打「键没匹配到任何卡」 |

**改完的验收读数**（逐条跑过）：
- `卡面修正表：所有键都匹配上了 ✅`（**没有孤立键** —— 这就是 §4·2 那两张表必须同步的直接证据）
- `裸卡名键唯一性 ✅（每个裸键的卡名在全池只出现一次）` ⇒ 没踩 `_bad`（`_bad` 为真 ⇒ **rc=1、一个字节都不写**）
  ⚠️ 加键前**先确认过唯一性**：现读 `card_ids.json` 里 `Saviour Protocols` 有**两个** `uniqueId`
  （`TAU51` 与 32 位 GUID `fdacb4e3…`）—— 但那个 GUID **不在我们卡池里**（现核），改名后全池仍**只有 1 张**叫这名字。
- `descZh 层序 断言：cardface_fixes.descZh（人工、最高序）最终生效 255/255 条 · 违例 0 处 ✅`
- 池：**1127 张不变** · `id` 集合逐字相同 · **只有 2 张有字段变化**（§〇 那张表）
- `python -I -X utf8 工具/gen_cards_engine.py --check` ⇒ **`一致 ✅`（幂等）**

### 4·3 同族清单（**只列，⛔ 本笔一张都没改**）

现扫全池 1127 张：有官方 `Card_Name/<我方 id>` 条目的 **1009 张**里，**卡名逐字不同的 69 条**。
按「差几个字母」筛出**与 `TAU51` 同形状**（去掉大小写/标点/空格后**编辑距 ≤ 2**）的 **8 条**：

| id | 我们 | 官方 `Card_Name` | 编辑距 | `card_ids.json`（真值） | 判读 |
|---|---|---|---|---|---|
| `BL36` | Sylar Hexcorn | Sylar Hexscorn | 1 | **Sylar Hexscorn** | 真值**站官方**、我们少一个 `s` |
| `DA6` | Sergeant Naaman | Sergeant Taaman | 1 | **Sergeant Taaman** | 真值**站官方**、`N`/`T` 一个字母 |
| `SAU38` | Extermination Protocols | Extermination Protocol | 1 | **Extermination Protocol** | 真值**站官方**、我们多一个 `s` |
| `SAU39` | Reconstitution Protocols | Reconstitution Protocol | 1 | **Reconstitution Protocol** | 真值**站官方**、我们多一个 `s` |
| `SOR10` | Sisters Repentia | Sister Repentia | 1 | **Sister Repentia** | 真值**站官方**、我们多一个 `s` ⚠️ 但这一张在 `gen_cards_engine.py:330` 那段「19 条改名已回滚」里被点名过 ⇒ **改之前先读那段** |
| `TAU42` | Experimental Drone | Experimental Drones | 1 | **Experimental Drones** | 真值**站官方**、我们少一个 `s` |
| `GSC64` | Rusted Vents | Rusted Vent | 1 | **Rusted Vents** | ⚠️ **真值站我们**、官方少一个 `s` ⇒ **两源打架，需人裁** |
| `GSC49` | Telephatic Domination | Telepathic Domination | 2 | **Telephatic Domination** | ⚠️ **真值站我们**（两边都把 `h` 漏了）⇒ 官方那条像是官方自己的错字 ⇒ **需人裁** |

（`TAU51` 原本也在这张表里，已按已裁⑤ 改掉。）

**另 61 条不是「字母差」**（本笔不逐条列，属 `A970` 那条线）：绝大多数是
**标点/大小写/所有格**（`Leviathans Tendrils` vs `Leviathan's Tendrils` · `GOF34 Prophet of the Waaagh` vs `…Waaagh!` ·
`AM11 Shock Trooper Sgt` vs `Sgt.` · `SOR37 Triumph of Saint Katherine` vs `Triumph of St. Katherine` …），
少数是**结构性差异**（`AM24 Bullgryn Bone’ead` vs 官方 **`B`** —— 那个值明显是坏值，正是
`load_official_cardname` 里 `bad` 那一档要挡的 `AM24 = "u"` 同族；`EC14 Lord Kakophonist` vs `Varius, Lord Kakophonist`；
`GSC3 Acolyte Iconward` vs `Iconward Malak Vorenth` …）。**这些要动，得先立口径**（已裁⑤ 说「以官方为准」，
但 `gen_cards_engine.py:325-345` 那段又写着「⛔ 别再照 PnP 卡图改卡名」+「改名前先拿 `zh_CN.csv` 精确查」）
⇒ **本笔按「别一次改一大批」停手，只把同形状的 8 条列在这里等派活**。

---

## 五、验收读数（照抄，可复核）

```text
$ TMPDIR=/tmp/wf_ah bash 工具/typecheck.sh     ⇒ 运行时 0 错 / 编辑器 0 错
   （第一版 1 错：`Assets/CardPresentation/Editor/BattleScene.cs(12863,42): error CS0103: 当前上下文中不存在名称"TextWrappingModes"`
     ⇒ 改成 `TMPro.TextWrappingModes.NoWrap` 后 0 错）

$ python -I -X utf8 工具/gen_cards_engine.py   ⇒ rc=0
  卡面修正表：所有键都匹配上了 ✅
  裸卡名键唯一性 ✅（每个裸键的卡名在全池只出现一次）
  descZh 层序  断言：cardface_fixes.descZh（人工、最高序）最终生效 255/255 条 · 违例 0 处 ✅
  护甲值列     armour 列 >0 共 81 张 · 取 PnP 那一层 81 · 真值兜底 0        （与 AD 那笔逐字相同）
  关键词铺底   真值 CardTraits 只补空：命中 124 张 · 追加 139 个 trait      （与 AD 那笔逐字相同）
$ python -I -X utf8 工具/gen_cards_engine.py --check  ⇒ rc=0 · 「一致 ✅」（幂等）
$ bash 工具/ruleprobe.sh check                ⇒ rc=1（**已知**：GSC29 那 1 行 + ⑦ 指纹哨兵未钉；**与 AB/AD 那两笔逐字相同**）
  全池 1121 张 · 解析差异 1 行 · 改名 0 行 · 新增 172 · 消失 171
  ⇒ 🔴 如实报：本笔**改了卡名**，但这一栏仍是 **0 行**（本笔**没重钉指纹**，rc=1 的成因与本笔无关）
$ 卡池 MyGame/Assets/RuleEngine/Resources/cards_engine.json
  1127 张 · v9 · md5 64c21c901ade424b34ad0d0baf0298af ⇒ 83a37522c2c4839e52c4d9fde80a463b
  descZh 含 `<br>` 的卡：52 ⇒ 53（+ASH48）·  `desc` 含 `<br>`：0/0
```

**改了哪些文件**（`git diff --numstat` 读数，⚠️ 里面混着**别人未提交的存量**，本笔只占下列几处）：
`数据/游戏数据/card_stats.json`（`1 1`）· `数据/卡牌翻译/zh_cards.json`（`1 1`）·
`数据/游戏数据/cardface_fixes.json`（本笔 +3 行，对 HEAD 的 `28 1` 里含 `AD` 的 27/1）·
`MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（本笔 **+94 行**；对 HEAD 的 `176 2` 里含 `AF` 的 ②·D 那一段）·
生成物 `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`1 1`，该文件是**单行 JSON**）。
**行尾**：改前改后**都是纯 CRLF**（`CRLF == LF`，逐个数出来）✅ · ⛔ 未用 `sed -i` · ⛔ 未动 git · ⛔ 未跑 Unity 批处理。

---

## 六、顺手发现（**只报，⛔ 一条都没改**）

1. 🔴 **`AD_descZh两代.md` 的「顺手发现③」是错的** —— 已按**铁律 5 就地订正**
   （`资料/普查产出_第三十一会话/AD_descZh两代.md`，在原文那条下面加了带日期的更正块、**保留更正痕迹**，
   并把「立账：`<br>` 需要一个渲染层」那句**判为作废**）；
   **同一条里的 `TAU51` 那一格（§五·2「没查清谁对，只报」）也补了结案指针**。
   ⚠️ 全仓这一句**只有那一个副本**（现扫 `资料/**`：`原样印出来` + `<br>` 的组合只命中它）；
   `G_卡表P0.md:220-221` 写的是**对的**（「`<br>` TMP 认、我们的 `StripTags` 会剥掉」）⇒ 不必改。
2. ⚠️ `card_stats.json` 的 `art` 字段**仍带旧拼法**（`…Tau_Empire_strat_Saviour Protocolst.png`）——
   **没跟着改**，理由：生成器**整列丢弃它**（`工具/gen_cards_engine.py:537` 的 `KEEP` 注释明写
   「`art`/`voice`/`ocrSrc`/`face`/`factionId`/`decks`/`tier` 全部丢掉」），而且它是个 `res://`（Godot 期）
   的死路径、美术按 **id** 落盘（`import_original_art.py`）⇒ 改它零收益、只会多两处 diff。如实记在这里。
3. ⚠️ `数据/本地化/i18n/zh_CN.csv` 的卡名键**也带那个尾巴 `t`**（`:2208`、`:3862`、`:5056`、`:6250`，
   另有 `Talent:`/`Companion N:` 那三族派生键）。**没改**，理由：它不是生成器的输入、**也没有任何消费点**
   （全仓只有代码注释引用它），而且它是 `工具/sync_from_d2.py:97` 从 **`d:/warpforge/data/i18n`（我们自己的
   Godot 复刻）同步来的**派生件 ⇒ 本地改了下次同步会被盖回去。**要修得修源**。
4. ⚠️ `数据/卡牌翻译/_tmp_zh_build_g7.py` 里也有那个旧拼法（现扫 1 处）—— 那是当年生成 `zh_cards.json` 的
   临时脚本，**不在任何链上**，未动。
5. 🔴 **`desc`（英文）那一侧同样缺断行**（§1·5，53 张里 46 张）—— **本笔立账未改**。
6. ℹ️ `descZh` 里除 `<br>` 外还有**另外三种标签**（现扫全池）：`<link=…>`+`<sprite name=…>`（GSC 那一族的
   `sabotage` 等，共 11 张）· `<nobr>`/`</nobr>`（`SAU44`/`SOR29`/`SOR37`/`SOR49` 等）——
   都是**官方中文表自己的写法**（我们 `RewriteMarks` 只产出 `<link>`/`<nobr>`/`<b>`/`<sprite>` 这几种，
   而这批是**原文里就带着的**）。**不在本笔范围**，只报：它们会不会原样印出来、官方那一代对不对得上，
   是**另一笔**（`A1982` 那条 `((…))` 缺口同族）。

---

## 七、没查清 / 交回主对话

1. 🔴 **那三条断言没在真宿主里跑过**（简报明写不许跑 Unity 批处理）—— 本笔只过了类型检查。
   已逐条排除假红的可能（§三那段），但**仍需调度台在下次收口时实跑 `BattleScene.Run` 确认**。
2. ⚠️ **同族 8 条卡名要派活才动**（§4·3）—— 其中 `GSC64` / `GSC49` **两源（官方 vs 真值）打架**，
   按铁律 13·8① 该由**主对话裁**，代理不猜。`SOR10` 那一张还压在 `gen_cards_engine.py:325-345`
   那段「19 条改名已回滚」的历史上，**改之前必须先读那段**。
3. ⚠️ **`desc` 侧那 46 张的断行**（§1·5）没查清该照 PnP 逐张改还是照官方 `<br>` 批量补 ——
   **口径要主对话裁**（两者可能不一致：我们 `desc` 是 PnP 那一代、官方表是 `_new` 那一代）。
4. ⚠️ `card_stats.json` 的 `art` / `zh_CN.csv` / `_tmp_zh_build_g7.py` 三处旧拼法按 §六·2/3/4 的理由留着，
   **若主对话认为要一并清干净，请点名**（本笔按「最小改动面」停手）。
