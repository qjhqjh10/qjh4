# `R7` · `official_names.json` 的 `note` 过期文案 —— 改生成器 + 重跑（动手 · 2026-10-15）

> 写手：`R7` 波。**白名单** = `工具/_loc_official/gen_official_names.py` · 它产出的
> `MyGame/Assets/CardPresentation/Resources/official_names.json` · 本报告。
> ⛔ 没跑 Unity · 没动 git · 没改两张正本 · 没改任何 `.cs` · 没碰「别的写手在动」的那 5 个文件。
> 触发：`资料/普查产出_第三十二会话/R3_卡牌线施工单.md` §12（现读确认过期）+ 账 `A2028`。
> 判据：同文件 §12 给的**逐字替换文本**（:270 → :272）+ `资料/普查产出_第三十一会话/AC_Loc记号闸.md` §二·1。

---

## 一、改了什么

### 1·1 动手改的文件

`工具/_loc_official/gen_official_names.py` **第 85-86 行**（`NOTE` 元组最后两句）：

- **旧**（改前 `count(旧) == 1`，全仓只此一处做**产地**）：
  `"印之前要过记号渲染层（\`A1763\`）。⚠️ \`Loc.BulkOr\` 另有一道「值里含 \`[[\`/\`((\` 就当取不到」的闸"` +
  `"（\`Core/Loc.cs\`）⇒ 带记号的 \`Modular_Description\` 值今天仍取不到（18 处 / 11 键，见 P3 报告）。"`
- **新**（逐字照 `R3_卡牌线施工单.md:272`，**含它那两个反引号**）：
  `"印之前要过记号渲染层（\`A1763\`）。⚠️ 值带记号时 \`Loc.BulkOr\` 会先过 \`CardIcons.RewriteMarks\`；"` +
  `"渲完仍留着记号才回兜底 + 出声（判据 → 资料/普查产出_第三十一会话/AC_Loc记号闸.md）。"`

🔑 **反引号的裁定依据**：任务简报里那段「要改成」是**去掉了反引号**的转述（同一段里「原文」也被去掉了反引号），
而**上游施工单 `R3_卡牌线施工单.md:272` 是带反引号的原文**（`\`Loc.BulkOr\`` / `\`CardIcons.RewriteMarks\``）⇒ **按施工单逐字落**。
⛔ 两个标识符**没有**加粗、路径**没有**反引号 —— 与施工单一致。

### 1·2 行尾与 diff

| 检查 | 读数 |
|---|---|
| `gen_official_names.py` 行尾 | 改前 `crlf 0 / lf 214` → 改后 **`crlf 0 / lf 214`**（**纯 LF 未翻**） |
| 文件字节 | 13221 → **13215**（−6，只有那两句换掉） |
| `git diff --numstat`（脚本） | **`2  2  Unity/工具/_loc_official/gen_official_names.py`** |
| `git diff --numstat`（产物） | **`1  1  Unity/MyGame/Assets/CardPresentation/Resources/official_names.json`**（单行 json，改 1 行） |

**改前留了副本**：`d:/4/_tmp_view/gen_official_names.py.bak_r7` · `d:/4/_tmp_view/official_names.json.bak_r7`。

---

## 二、跑生成器

### 2·1 命令与返回码

```
python -I -X utf8 "d:/4/Unity/工具/_loc_official/gen_official_names.py"            # 干跑：rc=0
python -I -X utf8 "d:/4/Unity/工具/_loc_official/gen_official_names.py" --write     # 落盘：rc=0
```
- 干跑（改文案前）与 `--write`（改文案后）**两条的族计数逐字相同**（合计 **2758**）。
- `--write` 自报：`落盘 d:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_names.json`。
- ✅ **「生成器写的就是这个路径」已现核**：脚本 `:39` `OUT = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_names.json"`（**唯一**写它的地方）。

**依赖（读脚本头认全，⛔ 一次性列在这里，下次别再找）** —— 三个源**跑前跑后都干净**（`git status` 对这三个路径空）：

| 源 | 用途 | 跑前 md5 |
|---|---|---|
| `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` | `w4_lib.official_pairs()` 的全部词条 | `1631fcf315db2d8fe5e66d89d91e45a4` |
| `MyGame/Assets/CardPresentation/Core/Loc.cs` | `card_trait_complement()` 现算补集（正则扫 `"Card_Trait/…"`） | `d5807095351f8af11467f5ed00b4a2a9` |
| `MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json` | `PrebuiltDecks/*` 按英文名 join | `14568726178b3b63a914256f7ee086ea` |
| `工具/_loc_official/w4_lib.py`（同目录助手） | 读上表 / 正则 | `df1ff33ec375442b97197dd77c0497a3` |

⚠️ **生成器不读 `cards_engine.json`**（那是别的写手正在动的文件，没被牵到）。

### 2·2 产物读数（改前 → 改后）

| 项 | 改前 | 改后 |
|---|---|---|
| 路径 | `MyGame/Assets/CardPresentation/Resources/official_names.json`（**未变**） | 同左 |
| md5 | `a05888704b37f2583c80d83720a98edf` | **`d7dbc61497e476bfaf17147575c63cbb`** |
| 字节 | 260953 | **260947**（−6） |
| 行尾 | `crlf 0 / lf 0`（**单行、无尾换行**） | **`crlf 0 / lf 0`**（同） |
| `count` 字段 / `len(entries)` | 2758 / 2758 | **2758 / 2758** |

**每族条数（改前 → 改后，逐族零差异）**：

| 族 | 改前 | 改后 | | 族 | 改前 | 改后 |
|---|---|---|---|---|---|---|
| `Card_Name` | 1065 | 1065 | | `Event_Description` | 111 | 111 |
| `Avatars` | 419 | 419 | | `PrebuiltDecks` | 74 | 74 |
| `Titles` | 368 | 368 | | `Card_Trait` | 26 | 26 |
| `Modular_Description` | 340 | 340 | | `Cardbacks` | 157 | 157 |
| `Achievements` | 198 | 198 | | **合计** | **2758** | **2758** |

### 2·3 逐字段 diff（脚本跑的，⛔ 不是手抄）

脚本：`d:/4/_tmp_view/r7_scripts/r7_diff.py`（读两个 json，比顶层每键 + 逐条 entries）。

| 顶层键 | 结果 |
|---|---|
| `version` | `1` == `1` → **同** |
| `count` | `2758` == `2758` → **同** |
| `source` | `数据/游戏数据/官方本地化_en+zh-CN.json` → **同** |
| `note` | **变**（唯一一处；新旧文本见 §一·1 与本报告末尾逐字引用） |
| `entries` | 2758 → 2758 · **逐条按下标比 = 全部逐字相同**（`ea == eb` 为真，连**键序**都没动） |

⇒ ✅ **验收标准 ④ 达成：除 `note` 外逐字不变，`entries` 零差异**（没有退步、没有别的写手改过源数据）。

### 2·4 文案与产物一致性（验收标准 ③）

用 `re` 把脚本里 `NOTE = ( ... )` 那个元组**求值**出来，与产物 `note` 字段比：**`True`（逐字一致）**。
⇒ 产物是**新生成器真跑出来的**，不是手改 json 的结果（这一点是关键 —— 手改 json 下次重跑就回来）。

### 2·5 新文案「与当前实现一致」的现核（验收标准 ①）

新句逐成分对实现（`Core/Loc.cs:2718-2765` 现读）：

| 文案里的说法 | 实现 | 判 |
|---|---|---|
| 值带记号时 `Loc.BulkOr` 会先过 `CardIcons.RewriteMarks` | `:2742` `if (CardIcons.HasOfficialMarks(v))` → `:2749` `rendered = CardIcons.RewriteMarks(v, zhCol);` | ✅ |
| 渲完仍留着记号才回兜底 + 出声 | `:2753` `if (CardIcons.HasOfficialMarks(rendered))` → `:2756-2757` `Debug.LogWarning("[Loc] …渲染不出来的官方记号…")` → `:2760` `return fallback;` | ✅ |
| `CardIcons.RewriteMarks` 确实存在、是公开口 | `Core/CardIcons.cs:468` `public static string RewriteMarks(string text, bool zh)`（同文件 `:448` `public static bool HasOfficialMarks(string text)`） | ✅ |
| 判据指针 `资料/普查产出_第三十一会话/AC_Loc记号闸.md` | 文件在（20900 B）· §二·1 那张三路表就是本条口径 | ✅ |

🔴 **旧文案为什么是错的（留痕）**：它说「`Loc.BulkOr` 另有一道『值里含 `[[`/`((` 就当取不到』的闸 ⇒ 18 处 / 11 键今天仍取不到」——
那道闸 **2026-10-15 `AC` 波已拆**（判据 `AC_Loc记号闸.md` §〇/§二·3），拆后卡面残留记号 = **0 处 / 0 张**。错因 = **文案写的是拆闸前的实现**、闸拆了没回头改生成器。

---

## 三、没查清 / 没做的

1. ⚠️ **`note` 之外还有没有过期文案** —— `R3` §12「没查清的」那一格把这条留给了下游。本笔**只逐句核了 `note` 剩下的 6 句**（结论见 §四·1），**`entries` 的 2758 条值一个字都没核**（那是数据，不是文案；`R3` 也没核）。⇒ **本条仍未关闭**，只是范围没扩大。
2. ⚠️ **`note` 里「（所以本产物只补手写表里没有的）」这句措辞偏宽** —— 严格说只有 `Card_Trait/*` 是**按 `Loc.cs` 现算的补集**，其余 7 族是**整族落**。今天**零实际影响**（脚本头 `:52` 说 `Card_Name/*` 与手写表 26 条键空间不相交），但严格讲这句只对 `Card_Trait` 成立。**本笔没改它**（不在施工单给的替换范围内，⛔ 不自己发明口径）。
3. ⛔ **没跑**：Unity 批处理 · `ruleprobe` · `typecheck`（本笔**一个 `.cs` 都没改**）· 任何自检。
4. ⛔ **没动**（不在白名单，只报不改）：`工具/_loc_official/w4_verify.py` —— 见 §四·2。

---

## 四、顺手发现（⛔ 一条都没改，供调度台分流）

1. **`note` 其余 6 句逐句现核：5 句成立、1 句措辞偏宽**（偏宽那句见 §三·2）。成立的是：
   「来源 = `官方本地化_en+zh-CN.json`」（= 产物 `source` 字段，现读一致）· 「`Loc.HasEntry` 排在它之前」（`CardIcons.OfficialTerm` 现读 = 手写表优先 → 数据面）· 「`BulkOr` 跟语档、缺键出声」（`:2732-2737` 按 `Current` 取、`:2725-2726` 缺键 `LogWarning` 按键去重）· 「值里可能带 I2 记号」（现数见下条）·「印之前要过记号渲染层（`A1763`）」（层已在 `CardIcons`）。
2. ℹ️ **两个「带记号条数」不是打架，是两把尺子**：生成器自打 **99 条**（= `zh` 含 `[[` **55** ∪ `en` 含 `<sprite` **44**），而 `AC_Loc记号闸.md` §一/§2·2 记的是 **56 / 55**（= **`en` 列**含 `[[` 的 56 条 · `zh` 列 55 条）。⇒ **99 与 56 不矛盾**；⚠️ 但生成器那行打印的**口径本身不对称**（只查 `en` 的 `<sprite>`、不查 `zh` 的 `<sprite>`）。**只是诊断打印**，不进产物、零行为影响。
3. 🔴 **`工具/_loc_official/w4_verify.py` 已过期（2026-10-14 的一次性核对脚本，硬编码期望值）** —— 本笔顺手跑了一次（**只读**），`合计：通过 35 · 失败 1`，那 1 条红是 **`① Loc.cs 表`：`new Entry(` 现读 732、脚本期望 720**（`Loc.cs` 后来长过）。同族 `⑥ official_names.json` 那行印的是 `Avatars 234/469`，而**产物今天 `Avatars` = 419**（本笔现数）⇒ ⑥ 的期望也旧了。⚠️ 这两条**都不是本笔造成的**：本笔改前/改后 `entries` **逐字相同**，⑥ 那行读数在任何一次本笔动作前后都一样。⇒ **建议**：把这个脚本标成「一次性、期望值已过期」，或让它现算期望值（⛔ 本笔不动它，不在白名单）。
4. ℹ️ **`18 处 / 11 键` 这句旧文案在别处还有 4 份**（都在 `资料/` 的**历史/引述**上下文里，**不是**「仍在主张」）：
   `普查产出_第三十一会话/AG_重钉与订正.md:224`（引旧文案）· `普查产出_第三十二会话/R3_卡牌线施工单.md:266`（引旧文案作对照）· `普查产出_第三十二会话/R2_B1落账草案.md:213`（`A2028` 的落账行，本身就写着「要改生成器那一侧」）· `资料/历史/交接_第三十一会话.md`。另 `Core/Loc.cs:2698` / `Core/CardIcons.cs:584` / `Editor/BattleScene.cs:12728` 都是**「原来…现在…」的对照写法**（✅ 正确，不是过期文案）。⇒ **本笔只改了「产地 + 产物」两处**，其余按「历史引述」留原样。

> **逐字留档**（改后 `note` 的最后两句，与产物 `note` 字段一致）：
> `⚠️ 值带记号时 \`Loc.BulkOr\` 会先过 \`CardIcons.RewriteMarks\`；渲完仍留着记号才回兜底 + 出声（判据 → 资料/普查产出_第三十一会话/AC_Loc记号闸.md）。`
