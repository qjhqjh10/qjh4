# W_卡面代码 —— `A969` + `A980` 执行报告（写手：卡面图标 · 代码侧）

**所有权**：本轮只动了两个文件 ——
`MyGame/Assets/CardPresentation/Core/CardIcons.cs` · `MyGame/Assets/CardPresentation/Editor/IconSetup.cs`
（`git diff --stat`：51+/13− 与 80+/17−；**行尾都是 LF、没被翻**，逐文件数过 `CRLF=0`。）
未跑 Unity（⛔ 主对话在跑批处理）；**只跑了秒级类型检查**（见文末）。

---

### A969 —— 结论：**已改**

- **改了哪**：
  · `CardPresentation.CardIcons.Rewrite`（`Core/CardIcons.cs`）——把原来**一份** `tag`
    拆成 **`spriteTag`（未包 link）/ `tag`（已包一层 link）** 两份；
    **裸关键词那一支改用 `spriteTag` 拼项**、外面**只包这一层**。
  · **幂等判据跟着换**：`s.IndexOf(tag + it.token)` → **`s.IndexOf(bare)`**（新形状本身）。
  · 顺带订正同文件两处过期注释：文件头那个例子串（`[Armor]` 已不在卡池里）、
    `Rewrite` 的「计划表 1369 处 / 方括号 195 / 裸关键词 1095 / 符号 79」。
- **判据**（原版方法体 + `_DAT_` 字面量已按 `资料/战斗规则与数值_出处.md` §三 解出来）：
  · `GameStaticData__TraitNameToString.c:75-76` = `Concat("<nobr>"(0x184237e80), 图, 词, "</nobr>"(0x1842cf8e8))`
  · 同文件 `:91-109` 外面**再包一次、且只包这一次** =
    `Concat("<link="(0x184237880), 枚举名, ">"(0x18423b778), 上一串, "</link>"(0x1842cf7e8))`
  · `ModifyLocalization.c:440-456`：`[[`(0x1842b06f0)`X]]`(0x1842b9628) → `TraitNameToString(X,true)`
  ⇒ **原版整项 = `<link={枚举名}><nobr>{图}{词}</nobr></link>`（一层）**；我们原来是两层。
- **幂等 / 两态怎么保证的**（离线复算，脚本在 `/tmp/wf_card/sim.py`）：
  · 新形状 ⇒ `SAU_Lychguard` 第二遍**逐字不变**（`②h` 绿）；旧形状（`fixed=False`）下
    `②e` 的 `<link=remnant>` 计数 = 2 ⇒ 红。
  · 🔴 **若只改形状、不改幂等判据** ⇒ 第二遍变成
    `<link=oath><sprite name="oath"><link=oath><sprite name="oath">Oath abilities</link></link>`
    —— **`②h` 与 `②i` 的幂等那条同时红**（实测复算，两态可分辨）。
- **连带改掉别处比坏形状的断言**：**没有**。`grep -rn '<link=[^>]*><link=' --include=*.cs` ⇒ **0 处**；
  `IndexOf("<link` 全仓只有 `Editor/BattleScene.cs:10771`，是「**含** link」+`withLink>100`（新形状照样成立）；
  `BattleScene.cs:10754` 的 `StripTags` 夹具本来就是一层、`gen_tutorial_stages.py:117` 的 `SNAP_STRIP` 按正则剥 —— 都无关层数。
- **影响面**（离线按 `Rewrite` 分支口径复算，与主对话给的数吻合）：**裸关键词 1306 处 / 473 张**
  （两次影响）；方括号 135 处 + 符号 59 处（整串换掉 194 处）不受影响。

### A980 ②e / ②f / ②f2 / ②f3 —— 结论：**已改（4 条断言）**

写法照 `②i`：`CountOf(<sprite…>)` 计枚数 + `StripTags()` 后查词；**都不再拿整串全等比**。
逐条的两态判别式（改坏法写得出）：

| 标签 | 新判别式 | 改坏哪里会红（离线复算过） |
|---|---|---|
| `②e` | `Melee/remnant` 枚数 1 **+ `<link=remnant>` 计数 1** + 图标在词前 + `StripTags` 有词 | 回到双层 ⇒ link 计数 2 ⇒ **红**；词被吃掉 ⇒ 红 |
| `②f` | `Melee` 枚数 1 + `StripTags` 里**没有 `Attack`** | 词留着 / 记号没换 ⇒ 红 |
| `②f2` | `faith` 枚数 1 + **`☀` 不在了** + `StripTags` 里**后面那句话还在** | 符号没吃掉 / 后文被误吃 ⇒ 红 |
| `②f3` | `questPoints1` 枚数 1 + `StripTags` 里 `Quest Point` **与那个 `1` 都没了** + `Gain` 还在 | 数字没吃掉 / 整串没换 ⇒ 红 |

- ⚠️ 实测复算：新形状下这 4 条**全绿**，且 `②d / ②g / ②h / ②i / ②j` 也全绿；`②d`/`②f3` 的实得与
  `_tmp_view/iconsetup_1020.log` **逐字吻合**（可反证我的离线复刻是对的）。

### A980 ②c（`[Armor]`）—— 结论：**夹具已改 + 新增一条「已知缺口」断言**

- **先判的那件事：是「夹具过时」，不是「该不该换」的争议。**
  · `[Armor]` / `[Armour]` **当前卡池 1126 张里 0 处**（实扫 `cards_engine.json` 的 `desc`+`descZh`）；
  · `DA44` 的**真** `desc` 已经是 `Give +3 [Attack], +3 Ranged or +3 Health to a friendly troop`
    （⇒ `工具/gen_icon_plan.py:187` 那条 `("DA44","[Armor]")` 现在是**死表项**，匹配不到任何 token）；
  · 原夹具是 C# 里硬编码的旧串 ⇒ 它测的是**一个不存在的 token**。
  ⇒ **夹具改成卡池真文本**（照 `②i`，`CardDatabase.Load()` 取 `CardDef.Desc`），不再另抄一份。
- **顺带抓到一条真缺口（`★ ②c【已知缺口 A980-c】`）**：
  · **亲读成品卡图** `D:/2/Warpforge部队卡片/Dark Angels/4计策/Warpforge_44_Ancient-Reliquary.png`
    （900×1200，我裁到 x∈[0.26,0.72]·y∈[0.70,0.80] 放大 3 倍看过）：
    `Give +3〔红圈**拳**〕, +3〔紫圈**枪**〕 or +3 Health to a friendly troop`
    —— **两枚图标、卡面上没有 `Ranged` 这个词**；
  · 而我们的 `desc` 那个位置写的是**裸词 `Ranged`**、计划表里没有对应项
    ⇒ 卡面印成 `+3 Ranged`（**少一枚枪图标**）。
  · 判据 = 铁律 7（`[Armor]`/`[Armour]` 在 **`+N Attack … +N Armour` 这个固定搭配**里就是**枪**）
    + 上面那张成品卡图；**缺口在 `工具/gen_icon_plan.py`（⛔ 不是我的文件，我没改）**。
  · 🔴 **这一条现在是红的**（红到表项补上为止）—— **是如实上报，不是回归**；
    且 `IconSetup.Verify` **不在** `_run_8_checks.sh` 那 12 条里，不会污染收口自检。
  · **要转派给 `gen_icon_plan.py` 写手的东西**：卡 `DA44` / 字段 `desc` / 那个位置
    （`+3 Ranged` 的 `Ranged`）该画成**枪图标**。⚠️ **但「怎么改」需要先定一件事**：
    计划表的**裸词支一律是「图标 + 词」**（`Rewrite` 按 `token[0]` 是不是字母判的），
    而卡图上**没有那个词** ⇒ 光加一条 `("DA44","Ranged")` 会把 `Ranged` **印在卡面上**。
    ⇒ 要么把数据改回 `[Armor]` 那种方括号占位，要么给脚本加一种「吃掉」的项。
    **这一层我没定，也没改**（⛔ 不许自己发明口径）。

### 一并订正的那处过期文档数 —— **只报告，未改**（两个文件都不归我）

- 真值 = 日志那轮是 **1606/1606**（`_tmp_view/iconsetup_1020.log:392`；`"sprite":` 计数 **1606 处 / 576 张**，我复算过）。
- `工具/gen_icon_doc.py:65` 硬编码 **`270/270`** ⇒ 错；`资料/卡面图标_现状与缺口.md:40` 写 **`1307/1307`** ⇒ 错。
  ⇒ **三个数打架，只有 1606 是真的。**
- ⚠️ **但这个数正在动**：我干活期间**另一个写手正在改 `card_icon_plan.json`**
  （`git diff` 里是 `descZh` 的新项：`团：`×9 · `[远程]`→`Ranged` 给 7 张中文卡 …）——
  我复算时已是 **1617 处**。⇒ 别把 1606 当固定值，**以现扫为准**。
  ✅ 已现核：**`DA44` 那条没被动过**（仍是只有 `["Attack"]→Melee`），所以 `②c` 的红**依然成立**。

### 🔴 顺手发现（照 13·4⑧ 报上来，我**没动**）

1. **`A965` 的病根就是 `A969`，可以销账。**
   `资料/普查产出_1018/S6_A965卡面探针取槽.md:47,141` 记 `Heavy Intercessor/keywords` 的 `t.text`
   前 24 字符是 `<link=armour><link=armou…`。我离线复算 `UM_Heavy_Intercessor` 的 `descZh`
   （`护甲 1。典籍：…`）——**旧代码前 24 字符逐字就是 `<link=armour><link=armou`**、
   **新代码前 24 字符 = `<link=armour><sprite nam`**。那份文档猜的「嵌套两层」是对的。
2. **原版那一项里还有 `<nobr>`，我们这条支没有**（另一条账）。
   `TraitNameToString.c:75-76` 的整项是 `<nobr>图+词</nobr>`；我们 `Rewrite` 这条支
   （裸关键词）**没加 `<nobr>`**，只有 `CardText.KeywordSegment` 那条路加了。
   ⇒ 差异真实存在（铁律 11：要做），但**动它会改折行**、要重跑版面自检，
   而我这轮**不能跑 Unity** ⇒ **没动**，写在这里由调度台另开账。
3. **`desc` 里「裸词占图标位」的疑似同类项有 109 张**（`desc` 含裸 `Melee`/`Ranged` 而计划表
   没有对应图标的卡）。⚠️ **我没判它们对错**：`+1 Ranged Attack` 这类多数是**卡面真印的词**
   （还原表 `_还原效果文字.md:21,36,49,63` 写的是带空格的 `Ranged Attack`），与 `DA44` 那种
   「`+3` 紧贴图标名」不同。**别把这 109 张当同一批缺陷**，要动先另开只读普查。
4. **`资料/` 里若干文档把坏形状当「现状」记着**（我没改，不是我的文件）：
   `普查产出_1018/S13_Oath候选A.md:201`、`普查产出_1018第三会话/RECON_卡面族.md:32`、
   以及本轮 `EVID_IconSetup_1018c.md:19,27,28,39,40` —— 修完后这些「实得」都过期了。

### 类型检查

`TMPDIR=/tmp/wf_card bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（改完全部内容后又跑了一次
`TMPDIR=/tmp/wf_card2`，同样 0/0；**没有出现别人半成品带来的假错**）。

### 没查清 / 停手的地方

- ⛔ **没跑 Unity** ⇒ `IconSetup.Verify` 的「实际绿红」是**离线复算**（脚本忠实复刻了 `Rewrite`
  的四个分支 + `Badges.KeyOf` + `StripTags`），**不是**真跑的结果。
  ⚠️ 复刻时我自己踩过一个坑并改正：**`stone || numInArt` 那一支末尾【没有 `continue`】**，会落到
  最后那句 `s = s.Replace(it.token, tag);` —— 第一版复刻漏了它，`②f3` 算成不换。正确复刻后
  `②d`/`②f3` 的实得与 `iconsetup_1020.log` **逐字吻合**，可反证复刻是对的。
- `②c` 那条红线**什么时候能绿**取决于 `gen_icon_plan.py` 怎么改（见上面「要转派」一节）。
