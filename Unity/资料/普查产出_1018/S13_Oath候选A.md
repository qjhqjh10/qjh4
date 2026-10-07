# S13 · Oath 候选 A（裸写 + 只修 3 张）

> 写手 S13（2026-10-18）。**只改白名单里的文件**；没跑 Unity、没动 git、没碰两张正本与别的 `.md`（生成器产物除外，见 §3）。
> 裁定按调度台的口径：**选候选 A**（保持裸写、只修那 3 张）。**候选 B（全族记号化）没做** —— 理由见 §0·四。

---

## 0. 先说三处「与简报字面不一致」的地方（都是机械原因，不是口径分歧）

### 0·一 🔴 `TOKEN_BY_CARD` 补 `("UM89","Oath")` **这条路走不通**（三条独立理由，逐条实测）

| # | 事实 | 依据 |
|---|---|---|
| ① | `TOKEN_BY_CARD` 的键**只能是方括号 token** —— 它只在 `for m in toks`（`TOK = re.compile(r"\[([^\[\]]{1,24})\]")`）的匹配循环里被查，裸词永远不会命中 | `gen_icon_plan.py:708`（`for m in toks:`）· `:714`（`TOKEN_BY_CARD.get((cid, key))`）· `:668`（`TOK` 定义） |
| ② | 就算把查询扩到裸词，**token 写 `"Oath"` 也画不出来**：`CardIcons.Rewrite` 按 token **长度降序**换（`"Oath 1:"` 7 字符 > `"Oath"` 4 字符），长的先换完，短的再进来时撞上它的**幂等守卫** `s.IndexOf(tag + it.token) >= 0` ⇒ **整条被跳过** | `Core/CardIcons.cs:177-186`（排序）· `:249`（守卫） |
| ③ | 同理 `("UM84","Oath")` 反而**能用**（那张卡没有 `Oath N:`）—— 但这会让三张卡写法不一致 | 仿真实测见下 |

**仿真读数**（把 `Rewrite` 逐句移植成 Python；**是仿真、不是实跑** —— 红线不跑 Unity）：

| 变体（`UM89` 的 `desc`） | `<sprite name="oath">` 出现次数 | 判 |
|---|---|---|
| 计划表 = `Oath 1:` + **`Oath`**（简报给的形状） | **1** | ⛔ 首句画不出来 |
| 计划表 = `Oath 1:` + **`Oath abilities`**（本批采用） | **2** | ✅ |
| 计划表 = 只有 `Oath 1:`（删掉补的那条） | **1** | ⛔ 回到缺陷 |

⇒ 采用**同族的新表 `BARE_TOKEN_BY_CARD`**（`gen_icon_plan.py:275-299`），键 `(卡 id, 裸 token)`，
token 取 **`"Oath abilities"`**（能在文本里唯一指到那枚徽记、且比 `"Oath 1:"` 长）。
**为什么不塞进 `TOKEN_BY_CARD`**：那张表的语义是「**方括号占位 token** 画哪张图」，
塞裸词进去会得到一条**看着在干活、实际什么都不做**的条目（本工程红线：不许静默失败）。

### 0·二 🔴 `UM84` 也必须补一条计划表（简报没列，但不补就是回归）
`UM84` 的徽记原来**唯一**来源就是那个方括号 token。`desc` 改成裸写之后，
不补计划表 ⇒ 它从「有徽记、没词」变成「**没徽记、有词**」—— 还是不符合原版。所以三张卡一起补。

### 0·三 🔴 **卡面实际显示的那一侧（中文 `descZh`）也补了三条**
**卡面走的是中文**：`BattleDriver.FaceTextFull`（`BattleDriver.cs:10577`）`bool zh = !string.IsNullOrEmpty(c.DescZh);`
> `zh = !string.IsNullOrEmpty(c.DescZh);` ⇒ 这三张卡**卡面印的全是 `descZh`**
⇒ 只补英文那半，**玩家看到的卡面照旧少一枚徽记**（这正是本件要修的缺陷本身）。
故 `BARE_TOKEN_BY_CARD` 里另有三条中文（token `"誓言能力"`）。
⚠️ 这**超出简报逐字范围**（简报只点了 `desc` / 两句），但同根因、同一张表、判据同一份；
若调度台不要，删掉那 3 行 + 重跑两个生成器即可，与其余改动无耦合。

### 0·四 为什么不做候选 B（全族记号化）
① **方向是反的**：原版卡面 = 「徽记 + 词」，我们的裸写**已经**产出「徽记 + 词」（`V-OATH` §2.4 25/25 亲读）。
② **会把誓约能力静默弄死**：`[Oath] N:` ⇒ `NormalizeIconPrefix` 归一成 `Oath: N:` ⇒ `ReOathPaid`（要求 `oath\s+(\d+)`）**失配** ⇒ `OathCost=0`、`OathOps` 空、卡面也不打 `*`（`V-OATH` §7.2 第 2 行）。
③ 断言挡不住：`RuleEngineTest.cs:12559-12562` 那条用的是**夹具字符串**（不读池），改了池它照样绿。

---

## 1. 改了什么（逐处：文件:行号 · 改前 → 改后 · 理由 · 证据）

### 1·1 `数据/游戏数据/cardface_fixes.json`（**数据**，正规通道）

| 处 | 改前 | 改后 |
|---|---|---|
| `:203`（`desc` 列新增一条） | （无 `Chaplain Cassius`） | `"Chaplain Cassius": "Friendly Oath abilities apply an additional time.\nTalent: Catechism of Death"` |
| `:35`（`_manual_desc_note` 新增 `_2026-10-18_Oath裸写`） | —— | 改动说明 + 判据 + 配套改动（照本表既有格式） |

- **理由**：原 `desc` = `Friendly [Oath] abilities apply an additional time.\n[Talent]: Catechism of Death`。
  两个 token 都是**方括号**（= 数据管线把「图标 + 那个词」压成一个 token）⇒ `CardIcons.Rewrite` 走
  「**整串换掉**」那一支（`CardIcons.cs:257`）⇒ **词 `Oath` 与 `Talent` 都从卡面上消失**，只剩两枚孤零零的图标。
- **证据（本批作者亲看整卡 · 铁律 7 · 不是转述）**：
  `d:/2/Warpforge部队卡片/Ultramarines/3部队/Warpforge_20_Chaplain-Cassius.png` —— 卡面实物读作
  `Friendly 〔蓝圆盘 + 白袍人形〕Oath abilities apply an additional time.` / `〔纸卷〕Talent: Catechism of Death`
  ⇒ 徽记在**句中**、紧贴词 `Oath`；`Talent` 前那枚是**纸卷**。**两个词都印着**。
- **⛔ 没改上游**：`card_stats.json`（OCR 源）一个字没动 —— 本表是覆盖列，`gen_cards_engine.py:818-820` 会盖掉它。
- **引擎行为不变（只影响显示）**：`CardDef.MatchStaticBattleRule:772` 比较前本来就 `Replace("[","").Replace("]","")`
  ⇒ `OathDouble` 两版都 `true`；`CardDef.ExtractTalent:1115` 也把 `[talent]` 归一成 `Talent`
  ⇒ 天赋名两版都是 `Catechism of Death`（默认规则：`HasTalentWord` 那道闸对本卡是 `keywords=[]`，两版一致）。

### 1·2 `工具/gen_icon_plan.py`（**工具**）

| 处 | 改前 → 改后 |
|---|---|
| `:128-134` | 删 `("UM84", "[Oath]"): ("oath", …)`；原地留注释说明**为什么作废** + 指向新表 |
| `:235-237` | 删 `("UM84", "[Talent]"): ("talent", …)`（同根因） |
| `:275-299` | **新增** `BARE_TOKEN_BY_CARD`（6 条：3 张卡 × 英/中各一条，**每条带卡图证据**） |
| `:756-763`（`main()` 内、字段循环里） | **新增**：按卡裸 token 命中就进计划表（`_btok not in text` ⇒ 同一张表同时管 `desc` 与 `descZh`） |
| `:799-800` | 死条目自检**扩到新表**：`list(TOKEN_BY_CARD) + list(BARE_TOKEN_BY_CARD)` |

- 删那两条的**必要性**：`desc` 改裸写后 `[Oath]` / `[Talent]` 在文本里**已不存在** ⇒ 留着就是
  「永远不命中的死条目」（脚本自己在 `:801-802` 有这条自检，会把它打出来，但**不会**自动删）。
- 新表 6 条的 token 与证据（摘要）：

| 键 | token | 证据（卡面那一处） |
|---|---|---|
| `("UM84","Oath abilities")` | `Oath abilities` | `Ultramarines/3部队/Warpforge_20_Chaplain-Cassius.png`：**句中**那枚（`Friendly 〔徽记〕Oath abilities …`） |
| `("UM89","Oath abilities")` | `Oath abilities` | `…/Warpforge_25_Ferren-Areios.png`：**首句**那枚（卡面共 **2** 枚） |
| `("UM_Vico_Therbeus","Oath abilities")` | `Oath abilities` | `…/Warpforge_12_Vico-Therbeus.png`：**首句**那枚（卡面共 **2** 枚） |
| 同上三张 × `"誓言能力"` | `誓言能力` | 同上（中文侧；`descZh` 里写的就是「誓言能力」） |

### 1·3 `MyGame/Assets/CardPresentation/Editor/IconSetup.cs`（**断言落点**）
- `:313-378` **新增**一段（②i / ②j）：断言矩阵见 §4。插在原有 ②h 之后、③ 真渲之前。

**没碰的东西**：`RuleEngine/Core/*`（另有写手在改）· `CardIcons.cs` · `BattleDriver.cs` · 两张正本 · git · `资料/关键词图标_现状与总表.md`（调度台自己做那一格订正）。

---

## 2. `[Talent]:` 同根因那一处

- **查了什么**：`UM84` 同卡第二句 `[Talent]: Catechism of Death`。**修了**（与 `[Oath]` 同一处 `desc`、同一个改动）。
- **为什么能修**：
  - 改裸写后，**不需要人工补计划表** —— `KEYWORD_SCAN`（`gen_icon_plan.py:414-415`）要求
    「句首 / `. ` / `; ` 之后 + 词 + `:`」，而 `…an additional time.` + `\n` + `Talent:` **正好命中**
    ⇒ 生成器自己产出 token `"Talent:"` → `talent`（`talent.png` 在盘上，`IconSetup` ② 那条全查得到）。
  - 引擎侧两版都认：`CardDef.ExtractTalent:1115` 归一 `[talent]`；`StartsAnotherThing:1196` 的
    `low.StartsWith("talent:")` 对裸写同样成立 ⇒ **行为不变**。
- **判据**：卡面那枚是**纸卷**图标 + 紧跟词 `Talent`（亲读卡图，见 §1·1）。
- 断言已配（`IconSetup.cs:372-376`，②j"）。

---

## 3. 生成器重跑读数（逐条：只有哪几处变 · 张数 · descZh · 守卫）

重跑顺序 = 数据链顺序：`gen_cards_engine.py` → `gen_icon_plan.py --write` → `gen_icon_doc.py`。

### 3·1 `cards_engine.json`（用**解析后逐字段 diff** 核，文件是一整行、看 `git diff` 没用）

| 指标 | 读数 |
|---|---|
| 卡池张数 | **1126 → 1126**（`version` 6 不变） |
| 差异单元格 | **恰好 1 处**：`UM84 desc`：`Friendly [Oath] abilities apply an additional time.\n[Talent]: Catechism of Death` → `Friendly Oath abilities apply an additional time.\nTalent: Catechism of Death` |
| `descZh` | **0 处变化**（✅ 没被带坏） |
| 其余 1125 张 | 逐字段（id/name/type/cost/attack/health/ranged/keywords/desc/descZh/faction/rarity/subtype/nameZh）**全等** |

### 3·2 `card_icon_plan.json`（**两份一起写**：`数据/游戏数据/` + `MyGame/Assets/CardPresentation/Resources/`）

- 有记号的卡：**576 → 576**；字段级差异**恰好 6 处**（全部是我改的）：

| 卡 | 字段 | 改前 token | 改后 token |
|---|---|---|---|
| `UM84` | desc | `[Oath]` / `[Talent]` | **`Oath abilities`** / **`Talent:`** |
| `UM84` | descZh | `天赋：` | `天赋：` + **`誓言能力`** |
| `UM89` | desc | `Oath 1:` | `Oath 1:` + **`Oath abilities`** |
| `UM89` | descZh | `誓言 1：` | `誓言 1：` + **`誓言能力`** |
| `UM_Vico_Therbeus` | desc | `Oath 1:` | `Oath 1:` + **`Oath abilities`** |
| `UM_Vico_Therbeus` | descZh | `誓言 1：` | `誓言 1：` + **`誓言能力`** |

- 两份产物的 `cards` **逐项相等**（diff = 只有 `note` 那一行，**本来就是两段不同的说明文字**，见 `gen_icon_plan.py:43-46,808-813`）。

### 3·3 `资料/卡面图标_对照与缺口.md`（`gen_icon_doc.py`）

- 头部计数：`1601 处` → **`1606 处`**；四类：`167 方括号 / 1348 裸关键词 / 86 符号`
  → **`165 / 1355 / 86`** —— 三个数**逐条对得上我的改动**（−2 方括号 = `UM84` 那两条；+7 裸 = 6 条新表 + 1 条 `Talent:`）。
- ⚠️ **另有一段与本次无关的改动**：本文件这次 `git diff` 共 **32 加 / 27 删**，其中
  **我这一件 13 行**（6 条新行 + 2 条计数行 + `UM84` 那两行的替换），其余 **46 行**是
  **9 张卡改了 id** 造成的**既有落后** —— 旧 id 行被删、新 id 行被加：
  删 `DA_Ravenwing_Ballistus_Dreadnought` / `DA_Sergeant_Naaman` / `GSC_Acolyte_Iconward` /
  `SOR_Simulacrum_Imperialis` / `SOR_Sisters_Repentia` / `TAU_Aunshi_Ethereal` / `TAU_Stormsurge_Battlesuit` /
  `UM_Master_Tactician` / `UM_Storm_Speeder_Hailstrike`，
  加 `DA83` / `DA11` / `GSC1` / `SOR19` / `SOR30` / `TAU34` / `TAU45` / `UM2` / `UM72`。
  **判据**：这些 id 在
  **我动手之前**的 `card_icon_plan.json` 备份里**已经是新 id**（`DA83` 在、`DA_Ravenwing_Ballistus_Dreadnought` 不在）
  ⇒ 这份 doc **本来就落后于计划表**，重新生成只是**追平**。要最小 diff 的话只回滚这一个文件即可（它不被任何代码读）。

### 3·4 生成器自带的守卫 / 断言

| 生成器 | 读数 |
|---|---|
| `gen_cards_engine.py` | `卡面修正表：所有键都匹配上了 ✅`（无孤儿键）· `中文名 1126/1126` · `中文效果 1120/1126`（阈值同前）· 卡池 **1126** |
| `gen_icon_plan.py` | `🔴 sprite 名盘上没有: 无` · `没认出来的（0 处，**不猜**）` · `表里已定、锚点给出另一个答案（0 处）` · `按卡裸 token: 6` · **退出码 0** |
| 死条目自检 | **34 条**，逐条看过：**没有一条是 `UM84` / `UM89` / `UM_Vico_Therbeus`**（即我这 6 条新条目全命中，删掉的两条也没留死键）。那 34 条是**既有的**（全是被改名 token 顶掉的老键） |

### 3·5 引擎离线探针（`ruleprobe.sh check`，**旁证、不是验收**）

```
=== 全池 1120 张 · 解析差异 6 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
⚠️ 解析差异（前 6 张）：GOF_Da_Red_Waaagh|Da Red Waaagh · GOF53|Get'em ladz! · GOF50|Tide of Muscle
                      · GOF_Krumpaklaw|Krumpaklaw · UM84|Chaplain Cassius · UM85|Catechism of Death
```

- 6 行里**只有 `UM84` 是我的**；另 5 行是**本批并行写手**的未提交改动（`_2026-10-18_GOF四张图标被吃掉` + `_2026-10-18_UM85两处裸加号`，`cardface_fixes.json:31-34`）。
- **逐字比对 `UM84` 那一行的第二列**（解析产物）：
  - 基线：`UM84|Chaplain Cassius|Friendly [Oath] abilities apply an additional time,[Talent]: Catechism of Death||`
  - 现在：`UM84|Chaplain Cassius|Friendly Oath abilities apply an additional time,Talent: Catechism of Death||`
  - ⇒ 只有**文本列**变了，**解析列两侧都是空的**（本卡不进 `EffectText`，`OathDouble` 那一族走 `MatchStaticBattleRule`）⇒ **引擎结算未变**，这一行属「预期变化、不是回归」。

---

## 4. 新增断言（逐条：钉什么 · 判别式 · 落点 · 要哪个 Run）

落点：`MyGame/Assets/CardPresentation/Editor/IconSetup.cs:313-378`（`IconSetup.Verify()` 内，②h 之后）。
**`IconSetup` 不在 13 条自检里** ⇒ **这一组要调度台手跑**（命令见 §6）。

| # | 行 | 钉什么 | 🧨 改坏哪里会让它红 |
|---|---|---|---|
| ②i-a | `:355-358` | `UM84` `desc`：`<sprite name="oath">` 恰好 **1** 枚，**徽记索引 < 词索引**，且 `StripTags` 后仍含 `Oath abilities` | ① `desc` 改回 `[Oath]`（计划表跟着重跑）⇒ 词被吃掉 ⇒ 红；② 删掉 `BARE_TOKEN_BY_CARD` 那条 ⇒ 徽记 0 枚 ⇒ 红 |
| ②i-b | `:359-360` | `UM84` `descZh`：**1** 枚，且 `StripTags` 后仍含 `誓言能力` | 删掉 `("UM84","誓言能力")` ⇒ 0 枚 ⇒ 红 |
| ②i-c | `:361-363` | 上面两条**幂等**（`Rewrite(x) == Rewrite(Rewrite(x))`） | 给裸词那条去掉幂等守卫（例如把 `CardIcons.cs:249` 那行删掉）⇒ 第二遍插第二枚 ⇒ 红 |
| ②i-d | `:345-363` ×3 张 | `UM89` / `UM_Vico_Therbeus`：`desc` 与 `descZh` 各 **2** 枚（卡面亲读读数 = 2） | ① 删掉补的那条 ⇒ **1** ⇒ 红；② token 写成短的 `"Oath"` ⇒ 被同卡 `"Oath 1:"` 按长度降序 + 幂等守卫挡掉 ⇒ **1** ⇒ 红 |
| ②j | `:373-376` | `UM84` 的 `Talent:` = **纸卷 + 词**（`<sprite name="talent">` 在，且 `StripTags` 后含 `Talent:`） | `desc` 改回 `[Talent]:`（计划表重跑）⇒ 词被整串换掉 ⇒ 红 |

**两条自己给自己上的规矩**：
1. 🔴 **判别式不是「渲出来是什么」** —— 全部只查 `Rewrite` 的**输出文本**（它决定卡面正文，且可离线复算）；
   我没有写任何关于像素/渲染结果的断言（本批**没真渲过**）。
2. 🔴 **`want` 是亲读读数、不是常量巧合**：三张卡的 `want = 1 / 2 / 2` 直接来自成品卡图（§1·1 证据）。

⚠️ **关于简报给的字面形式 `含 "<sprite name=\"oath\">Oath"`**：**它在当前 `CardIcons` 下不可能成立** ——
2026-09-21 起裸关键词那条把**图标与词分别**包了 `<link>`（`CardIcons.cs:203-205` 与 `:250-253`），
`<sprite name="oath">` 后面紧跟的是 **`</link>`**、不是那个词（实测字符串：
`<link=oath><link=oath><sprite name="oath"></link>Oath abilities</link>`）。
⇒ 拆成 **(a) 徽记在（数次数）+ (b) 词也留着（`StripTags` 后查词）+ (c) 徽记在词前面**，语义相同、且真的能过。

---

## 5. 类型检查读数 + 行尾核对（原样贴）

```
$ TMPDIR=/tmp/wf_s13 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

```
文件                                                          CRLF     LF     size
Unity/数据/游戏数据/cardface_fixes.json                        1458   1458    95434   （**全 CRLF，原样保留**）
Unity/MyGame/Assets/CardPresentation/Editor/IconSetup.cs          0    481    29483   （纯 LF，原样）
Unity/工具/gen_icon_plan.py                                       0    824    55670   （纯 LF）
Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json        0      0   383683   （单行）
Unity/数据/游戏数据/card_icon_plan.json                           0  16415   672032
Unity/MyGame/Assets/CardPresentation/Resources/card_icon_plan.json 0 16415   671968
Unity/资料/卡面图标_对照与缺口.md                                  0   1711   527259
```

- **改法**：全部用 **Edit 工具**（⛔ 没用 `sed -i` / python 文本写）；改完逐个数过 `\r\n` vs `\n`
  —— `cardface_fixes.json` 1458/1458（**每一行都是 CRLF，没有混行尾**）、其余照原样。
- `git diff --numstat`：`cardface_fixes.json 13/2` · `gen_icon_plan.py 52/3` · `IconSetup.cs 67/0` ·
  `cards_engine 1/1` · `card_icon_plan ×2 31/6` · `卡面图标_对照与缺口.md 32/27` —— **都远小于文件行数**（没有整篇翻行尾）。

---

## 6. 🔴 要调度台做的

1. **手跑 `IconSetup`**（它**不在**那 13 条里）：
   `-executeMethod IconSetup.Verify`（**别用 `Run`** —— `Run` 会先 `Build()` 重建 sprite asset 资产；
   `Verify` 只跑检查。批处理下 `Verify` 的 `Finish` 会用退出码表达成败）。
2. 🔴 **跑之前先知道：这个宿主现在本来就有 5 条旧断言是红的**（**不是本批造成的**，见 §7·1）。
   请只按**行首标记**与我新加的 `★ ②i` / `★ ②j` 文案看结果 —— 我这几条是新增，应该全绿。
3. **要不要并排比**：本件**没真渲**（红线）。若要过铁律 10 第 6 条那一关，建议渲 `UM84` / `UM89` 两张
   与对应 PnP 成品图并排（`CardFaceProbe` 有现成路），看「徽记 + 词」是不是各两枚/一枚。
4. **探针基线**：`ruleprobe check` 现在 6 行差异（我 1 行 + 并行写手 5 行）。等两批都落地后**一起**重建基线
   （判据 = §3·5 已逐行看过：只有文本列变）。
5. 若不要中文侧那 3 条（§0·三）：删 `gen_icon_plan.py:296-298` 三行 → 重跑两个生成器 → 重新生成 md。

---

## 7. 顺手发现（**都没改**）

### 7·1 🔴 `IconSetup.Verify` 里 **5 条旧断言现在是红的**（与本批无关，且**没人会发现**，因为它不在 13 条里）
`Rewrite` 在 2026-09-21 给裸关键词/符号包了 `<link>`（`CardIcons.cs:203-205,250-253`）之后，
几条**写于那之前**的「紧贴字符串」断言就对不上了。**把 `Rewrite` 逐句移植成 Python 复算**（仿真、没跑 Unity）：

| 断言 | 行 | 现在为什么红 |
|---|---|---|
| ②c | `:263-265` | 要求 `da44` 同时含 `Melee` 与 **`Ranged`**，而计划表里 `DA44` 的 `desc` **只剩 `[Attack]`→Melee**（`("DA44","[Armor]")` 已是**死条目**，见生成器死条目清单） |
| ②e | `:275-276` | 查 `<sprite name="remnant">残骸。` —— 实际是 `<sprite name="remnant">` **`</link>`** `残骸。` |
| ②f | `:279-281` | 查 `<sprite name="Melee">,` —— 实际是 `<sprite name="Melee">` **`</link>`** `,` |
| ②f2 | `:286-287` | 整串**相等**比较 `sun == "<sprite name=\"faith\">：…"` —— 实际前面还有 `<link=faith>` |
| ②f3 | `:296-298` | 同上：`qp == "Gain <sprite name=\"questPoints1\">"` —— 实际是 `Gain <link=questpoints><sprite name="questPoints1"></link>` |

②d / ②g / ②h 复算**是绿的**（它们只查 `<sprite …>` 在不在、或次数）。
⇒ 这 5 条的修法都是一行（改成 `Contains("<sprite name=\"X\">")` + 单独查词，或补 `<link=…>`），
**但我没动** —— 超出本件范围，且那段代码不是我该改的口径。**请调度台分流**。

### 7·2 🟡 生成器里有一条**写死的**数字
`gen_icon_doc.py` 生成的 `资料/卡面图标_对照与缺口.md:10` 印着
「`IconSetup.Verify` 报 **270/270** 计划表里的 sprite 名都查得到」—— 这个数是**硬编码在脚本里的字符串**，
而 `IconSetup.Verify` ② 实际数的是计划表里 `"sprite": "…"` 的出现次数（**现在是 1606**）。
另 `资料/卡面图标_现状与缺口.md:40` 又写「**1307/1307**」—— **两处文档三个数**。**没改**（不在白名单）。

### 7·3 🟡 `CardDef.cs` 的两处注释现在稍微过时（**没改**，文件不归我）
`CardDef.cs:720` / `:738` 的注释仍写「`Friendly [Oath] abilities apply an additional time.`（`UM84`）」。
分句判据本身**两版都认**（`:772` 去方括号），所以只是注释里的**示例文本**过时，不是逻辑问题。
另 `RuleEngineTest.cs:10387/10441-10449` 那条 `[Talent]:` 用例是**夹具字符串**（不读池）⇒ 不随本改动变红，可留。

### 7·4 🟡 `资料/卡面图标_对照与缺口.md` 与计划表**曾经对不上**（本批顺带追平，见 §3·3）
根因是 9 张卡改了 id 之后**没重跑 `gen_icon_doc.py`**。建议把「改了 id/token 就重跑 doc」写进那条链的说明。

---

## 8. 没查清的部分

1. **没跑 Unity**（红线）⇒ §0·一 / §4 / §7·1 的「跑出来是什么」全部是**对 `CardIcons.Rewrite` 的逐句移植仿真**，
   不是执行结果。仿真脚本在会话临时目录（`wf_s13_sim.py`），**不入库**；真值要等 `IconSetup.Verify` 那一跑。
2. **`descZh` 有没有引擎侧副作用** —— 只沿用了 `V-OATH` §7.4 的结论（入口吃英文 `Desc`），**没穷举全仓谁读 `DescZh`**。
3. **`oath.png` 与原版 `oathSprite` 是不是同一枚** —— 沿用 `V-OATH` §2.3 的「同形同色」肉眼判读，**没有**逐像素 NCC 读数。
4. **`UM84` 的中文侧该不该也给 `誓言能力` 加徽记** —— 我的判据是「卡面走 `DescZh`」+ 同族 24 张的中文写法都有徽记；
   **原版没有中文**（原版客户端无中文表），所以这是**我们自己的翻译怎么排徽记**的问题，**没有原版判据**（口径由调度台定）。
5. **`BARE_TOKEN_BY_CARD` 的 token 长度是否还有别的坑** —— 我只对这三张卡推演了「长 token 优先 + 幂等守卫」；
   **没有**全池普查「有没有哪张卡的裸 token 会挡住另一条」。
