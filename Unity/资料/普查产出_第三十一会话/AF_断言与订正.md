# `AF` · 三件订正/补断言（动手 · 2026-10-15）

> 写手：`AF` 波（白名单 = `CardPresentation/Core/CardIcons.cs` · `RuleEngine/Core/RuleCore.cs` · 断言宿主 `CardPresentation/Editor/BattleScene.cs`）。
> 来源：① `AC_Loc记号闸.md` §五（4 条 ready-to-paste 断言）· ② `AC` §七·1（`CardIcons.cs` 过期注释）· ③ `AB` 顺手报（`RuleCore.cs` destroyer 判据订正）。
> ⛔ 本笔**没跑 Unity 批处理、没跑 `ruleprobe`、没重钉指纹、没动 git、没改正本**。
> ✅ 只跑了**秒级类型检查**（`TMPDIR=/tmp/wf_ac2 bash d:/4/Unity/工具/typecheck.sh`）。

---

## 〇、一句话

| # | 结论 |
|---|---|
| ① | ✅ 四条 + 判别式**已插进** `CardPresentation/Editor/BattleScene.cs` 的 `Run()`（新段 **②·D**，`:12727-12805`），并**另加**「全池」那一扫（见 §一·5 的说明与理由） |
| ② | ✅ `CardPresentation/Core/CardIcons.cs:502-515` 已就地订正（保留原句 + 「原写 X ⇒ 实际 Y」） |
| ③ | ✅ `RuleEngine/Core/RuleCore.cs:3565-3607` 已就地订正；**`AB` 的三条论据本笔逐条现核、条条成立** |

- `typecheck`（运行时 + 编辑器两个程序集）：**0 错 / 0 错**。
- 行尾：`BattleScene.cs` **纯 CRLF 未翻**（CRLF 23363 = LF 23363）· `CardIcons.cs` / `RuleCore.cs` **纯 LF**（CRLF 0）。
- `git diff --numstat`：`BattleScene.cs` **82/2**（其中 `@@ -12726,0 +12727,80 @@` = 本笔那 80 行；另一处 2 行是别人的）· `CardIcons.cs` **366/1**（本笔净 **+9**，其余是别人已落的改动）· `RuleCore.cs` **443/74**（本笔净 **+29**，其余同上）。

---

## 一、① 四条断言插进自检宿主

### 1·1 落点：`CardPresentation/Editor/BattleScene.cs` `Run()` 的 **②·D** 段（`:12727-12805`）

- 🎯 **它是活宿主**：`BattleScene.Run` 在 `工具/_run_8_checks.sh` 的全套 12 条里（`public static void Run()` 起 `:320`，本段在它体内，**无条件执行**）。
- 🔴 **为什么换到这儿**（`AC` 原稿只给 ④ 指定了本节、①②③ 只列了候选）——**两条理由**：
  1. **① 那一格需要 sprite asset 是活的**。本文件 ②·B 那一段（`:12710-12725`）已经在跑
     `CardIcons.Rewrite(c.Id, "desc", c.Desc)`、并用 `Check(withLink > 100)` 钉着它 —— 等于
     **本节自带一条「sprite asset 活着」的前置闸**。放到不渲卡面的宿主（如 `RuleEngineTest`）里，
     `CardIcons.SpriteAsset` 若取不到，① 的「那张图在场」会**假红**（不是缺陷）。
  2. `Loc.Current` 在本节**已经是中文档**（15c 段 `:12592-12593` 摆的、`:12787` 才放回）⇒ ③ 的中文期望值**天然成立**，不必再 `SetLanguage`（那会写盘，还得配 `PersistOverride`）。
- ✅ 语档收尾**照旧**：本段插在 `:12787` 的 `Loc.RestoreForTest(langBefore15c)` **之前**，没有改动那一步。

### 1·2 逐格（期望值 · 判别式）—— ⛔ 请逐格核

⓪ **摆基线**（两行，动手前）：
```csharp
Check(Loc.Current == AvailableLanguages.Chinese, "（前提）本段跑在中文档上（③ 的中文期望值只在这里成立）—— 现得 " + Loc.Current);
Loc.ResetBulkWarnedForTest();   // 只清去重表，⛔ 不动 PlayerPrefs
```

**① 正路 —— 拆闸前被整条挡掉的值，现在必须渲染出来，且一个记号都不许留**
- 调用：`Loc.BulkOr("Modular_Description/Stun_an_enemy", "<FALLBACK>")`
- 期望（**三个条件同时成立**，`&&` 连起来）：
  1. `!= "<FALLBACK>"`
  2. 串里 `[[` **0 个**（`IndexOf("[[") >= 0` 取反）
  3. 串里**有** `<sprite name="Atlas_trait_icon_stun">`
- **判别式**：「原样返回」结构上不可能同时满足 2 与 3（`RewriteMarks` 恒返回非空串也骗不过 3）。
- 现读真值（离线演算台，见 §一·5）：`使一个敌方目标 <link=stun><nobr><sprite name="Atlas_trait_icon_stun"><b>眩晕</b></nobr></link>`

**② 判别式（灭自证）—— 同一口对「渲染不掉」的那条必须回兜底**
- 调用：`Loc.BulkOr("Modular_Description/Duplicitous", "<FALLBACK>")`
- 期望：`== "<FALLBACK>"`
- **①② 不可能被同一份实现同时满足**：「一律渲染」让 ② 红、「一律回兜底」让 ① 红。
- ⚠️ **② 的前提**（已写进代码注释）：`Card_Trait/tainted` 与 `Atlas_trait_icon_tainted` **两边都缺**（现读）。
  将来谁把 `tainted` 补上，这条要**换一个同类值**（同类候选 `Modular_Description/Maintenance_x`），⛔ 不是删断言。

**③ 不带记号的值逐字不变（拆闸不许动这条路）**
- 调用：`Loc.BulkOr("Achievements/Types/Battle", "<F>")`
- 期望：`== "战斗"`
- 🔴 **这一格的期望值我改过 `AC` 原稿一个字**：原稿写的是 `"对战"`，后面自己批了「← 值以现读表为准」；
  **现读 `CardPresentation/Resources/official_names.json` 的 `Achievements/Types/Battle` ⇒ `en="Battle" / zh="战斗"`** ⇒ 按批注改成「战斗」。
  ⛔ 这不是「自己发明期望值」，是**照 `AC` 自己指定的判据取数**。

**④ 卡面那一层：`descZh` 过 `CardIcons.Rewrite` 之后不许再出现 `((` / `[[` / `$$`**
- 调用：`CardIcons.HasOfficialMarks(CardIcons.Rewrite(c.Id, CardIcons.DescZhField, c.DescZh))`
- **两格**：
  - **（前提）** 全池 `descZh` **带官方记号**的卡数 `faceMarked` `> 100`（现读 **559**）——没有这一条，「0 张残留」是**空转**；
  - **主格** 全池残留 `faceResidual == 0`（现读 **0**）。
- **另照抄 `AC` 的「单张点名」那一条**：卡池里那 **`Howling Banshee`**（真 id = **`ASH15`**，现读）
  - 前提格：`HasOfficialMarks(它的 descZh)` 为真（现读 `[[waystone]]。[[flank]]。[[rally]]：((Stun_an_enemy))`）
  - 主格：`!HasOfficialMarks(CardIcons.Rewrite(hb.Id, CardIcons.DescZhField, hb.DescZh))` —— **拆闸前这条必红**
  - 落点：`:12793-12803`

### 1·3 ⛔ 抄不进去 / 改了写法的格子（逐条给理由）

| `AC` 原稿 | 本笔落地 | 为什么 |
|---|---|---|
| `CheckTrue(...)` / `Check(...)` 混用 | 一律用本宿主的 `Check(bool, string)` | `Editor/BattleScene.cs` **没有 `CheckTrue`**（它是个局部函数 `void Check(bool ok, string msg)`，`:398`） |
| ⓪ `Loc.SetLanguage(AvailableLanguages.Chinese)` | 改成**断一句前提** `Check(Loc.Current == Chinese, …)` | 本节 15c 段**本来就摆成了中文档**（`:12592-12593`）；`SetLanguage` 会**写 `PlayerPrefs`**，用它反而要额外压 `PersistOverride`。⛔ 没动 `⓪` 的第二行（`ResetBulkWarnedForTest`） |
| ③ 期望值 `"对战"` | `"战斗"` | 见上（数据现读 = `战斗`） |
| ④ `CardIcons.Rewrite("228", CardIcons.DescZhField, descZhOf("228"))` | 按**卡名**在池里取那一张（真 id `ASH15`），并**另加全池那一扫** | ① `descZhOf` 这个助手**不存在**；② 「228」在 `AC` §二 那张名单里是**序号**（同列 `0 Unhallowed Gifts`），而 `Rewrite` 的第 1 个形参要的是**卡 id** —— 两者不是一回事；③ 单张贴「`== false`」在「池里一张带记号的卡都没有」时会**假绿**（空转）⇒ 补「带记号的张数 > 100」当前提 |
| ④ `Rewrite("228", "desc", …)`（`AC` §二 的写法） | 用 `CardIcons.DescZhField` | ④ 的输入是 **`descZh`**；写 `"desc"` 会让 `RewriteMarks` 收到 `zh=false`（取英文列子词条）—— 与「卡面中文那一列」不是同一路 |
| ② 落点建议 | 与 ①③④ **同段**（②·D） | ①② 是一对判别式，**分开两处会失去「不可能同时绿」这个性质**的可见性 |

### 1·4 每格都要「先摆回基线」这一条怎么落的

- 本段是**纯文本层**、不建场景，唯一的跨格残留是 **②/③ 之前那两条会往 `_warnedBulk` 里记账**（只影响出声次数，不影响返回值）⇒ `⓪` 那一次 `ResetBulkWarnedForTest()` 就是摆基线。
- ④ 的两格**自带前提格**（`faceMarked > 100` / `HasOfficialMarks(descZh)` 为真），不依赖上一格的残留。

### 1·5 期望值是怎么核出来的（⛔ 没跑 Unity，如实说明手法）

`AC` §二的「离线演算台」是**按 C# 四支照抄成 py**。本笔**另写了一份独立演算台**（`%TEMP%/wf_af/replica2.py`，**不在仓内**）
跑仓内那份**真数据**，四个数的读数：

```
hand entries 731 | bulk entries 2758 | sprites 78 | has stun: True | has tainted: False
--- CUR = zh (CardIcons.Rewrite 那一路的 zh 恒 true) ---
descZh 带记号: 559
Rewrite 后仍残留记号: 0
1) BulkOr(Modular_Description/Stun_an_enemy) = '使一个敌方目标 <link=stun><nobr><sprite name="Atlas_trait_icon_stun"><b>眩晕</b></nobr></link>'
2) BulkOr(Modular_Description/Duplicitous)  = '<FALLBACK>'
3) BulkOr(Achievements/Types/Battle)        = '战斗'
4) Howling Banshee id = ASH15 | descZh = '[[waystone]]。[[flank]]。[[rally]]：((Stun_an_enemy))'
   Rewrite = '<link=waystone>…<b>路标石</b>…</link>。<link=flank>…<b>侧翼</b>…</link>。<link=rally>…<b>集结</b>…</link>：使一个敌方目标 <link=stun>…<b>眩晕</b>…</link>'
```
⚠️ **这份演算台是「照抄」，不是我们那份 C# 本体** —— 它只用来**核期望值**，不等于那条断言在 Unity 里已经绿过。
（`AC` §八·1 那条「没有端到端画面证据」的缺口**继承未变**。）
⚠️ 上面那行读到的 `sprites 78` 是**去重后的名字数**（`SpriteExists` 就是按名字查的，这才是有效集合）；
`trait_textsprites.json` 的**记录数**是 **87** —— 两个数的关系见 §五·2。

---

## 二、② `CardIcons.cs` 过期注释就地订正

- **落点**：`CardPresentation/Core/CardIcons.cs:502-515`（`OfficialTerm` 的 doc，原 `:502-506`）。
- **原句**（后半句，⚠️ 前半句「`Loc.BulkOr` 用的是当前语言」**仍成立、一字没动**）：
  > 「…而且它自己有一道「值是未渲染记号 ⇒ 当取不到」的闸（`Core/Loc.cs` 那段 doc 写着「要真接上，先做 `A1763` 的记号渲染层」）—— 那道闸**留给调度台裁**（`Loc.cs` 不在本笔白名单里），本层只如实吃它的结果。」
- **订正写法（铁律 5 格式：保留原句 + 「原写 X ⇒ 实际 Y」）**：`原写「那道闸还在、留给调度台裁」＝实际它当天（AC 波）已经拆了`；
  新判据 = 「**渲染完还有没有记号**」；并加一条**对本层的实际影响**：`Loc.BulkOr(term, null)` 现在**会回一个已经渲染过的串**
  （可能是 `&lt;link=…&gt;&lt;nobr&gt;&lt;sprite …&gt;&lt;b&gt;词&lt;/b&gt;&lt;/nobr&gt;&lt;/link&gt;`），本层照旧 `QuoteSprites` + 原样吃它即可。
- ⚠️ `AC` §七·1 的原话是「前半句仍成立、后半句已过期」—— **本笔现读逐字核对：成立**。
- 实现那一侧的指针写成 `Core/Loc.cs:2697-2765`（现读：`:2697` 是新 doc 起、`:2765` 是 `return v;` 之后的 `}`）· 现数指针 → `AC_Loc记号闸.md`（⛔ 不在注释里抄第二份数）。

---

## 三、③ `RuleCore.cs` destroyer 判据订正

- **落点**：`RuleEngine/Core/RuleCore.cs:3565-3607`（原来那一段在 `:3565-3578`）。
- **先现核 `AB` 的三条**（⛔ 没照抄转述）——**条条成立**：

| `AB` 说 | 现核结果 |
|---|---|
| 那一支**就在** `BattleManager__IsValidAttackTarget.c:207` | ✅ `grep -n "0x2a8"` ⇒ **只有 `:207` 一处**：`EntityScript__HasCurrentTrait(param_2, 0x2a8, 0)` |
| trait 用的是枚举值 `0x2a8` | ✅ `0x2a8` = **680**；`DefinedTrait.cs:69` = `frenzied = 680` |
| 「grep 不到名字」不能当「原版没有」 | ✅ 同一支紧接着 `:208` 调 `BattleManager__CheckIfAttackKillsTarget(param_1, param_2, param_3, 0)`（该函数文件在 `decomp_full/` 里**确实存在**，99 行） |

- **额外现核（本笔补的两条，用来钉「0x2a8 就是毁灭者」）**：
  1. `EntityScript__HasVanguardTrait(param_3, param_2, …)`（`:189`）与 `CardScript__IsDefender(param_3, param_2, …)` 把
     **`param_3` = 目标、`param_2` = 攻方** 钉死了（本文件 `:177-181` 的 Flying 那一段也是这个分工）⇒ `:207` 查的是**攻方**，与我们的 `attacker.Has(...)` 同侧。
  2. `Card_Trait/frenzied` 的**官方词条 en 列 = `Destroyer`**（`Resources/official_names.json`）；我们手写表 `Core/Loc.cs:2532` 同键 = 「毁灭者」；`Core/CardDef.cs:4247` 早就记过同一条。
- **订正写法**：把原记那一整段（含它的每一条论据）**用 `~~…~~` 整体圈起来标成「已推翻」**，再接
  「🔴 2026-10-15（`AF` 波）就地订正（铁律 5）：原写「判据查不到」⇒ 实际判据**就在**反编译里」+ 逐条实据。
- 🔑 **`AB` 要写进注释的那条坑，已写**：
  > **IL2CPP 的 C dump 里【永远不会有】枚举成员名** —— 它把 `DefinedTrait.frenzied` 烘成裸字面量 `0x2a8`。
  > ⇒ 「`grep -rn -i "destroyer" decomp_full/` ⇒ 0 命中」**不能当「原版没有」的证据**。
  > **正确找法**：先拿枚举值那个**数**去反查（在 `DefinedTrait.cs` 里找到成员 = 多少），再拿那个数去 grep 反编译。
- ⛔ **实现一个字没动**（`if (attacker.Has(KeywordTable.Destroyer)) { … }` 那 10 行原样）；要不要照
  `CheckIfAttackKillsTarget` 改写 = **另一笔**（已在账上），注释里也写死了这一句。

---

## 四、没查清的部分（⛔ 没猜）

1. 🔴 **四条断言【没有在 Unity 里跑过】** —— 用户现行口径是「别全量自检」（`CLAUDE.md` 铁律 12 第十七会话），
   且本笔被明确告知「**不许跑 Unity 批处理**」。⇒ 期望值只到「离线演算台 + 逐条读实现」这一层。
2. ⚠️ ① 与 ④ 都**依赖 sprite asset 是活的**。判据是「若它不活，这两条会**红**而不是**假绿**」：
   - `CardIcons.Rewrite` 在 `!Available` 时**原样返回**（`CardIcons.cs:212`）⇒ ④ 的 `faceResidual` 会 > 0 ⇒ 红；
   - `Loc.BulkOr` 那条渲染路走 `RewriteMarks`（**不经** `Available`），asset 空时 `SpriteExists` 恒 false
     ⇒ ① 的第 3 个条件不成立 ⇒ 红。
   ⇒ 两条都是**响亮失败**，不存在「资源没了还绿」的静默路。**但我没有实际验证过 `Resources.Load<TMP_SpriteAsset>` 在批处理下的行为**（本笔不许跑）。
3. ⚠️ **`faceMarked > 100` 这个阈值是「不许空转」的下限**（不是钉死 559）—— 卡池数据（P3）还在动，钉死会平白变红。
   ⛔ 但**主格（残留 0）是真判据**，别因为阈值松就把它当软断言。

---

## 五、顺手发现（⛔ 一条都没改）

1. 🔴🔴 **`CardPresentation/Core/CardIcons.cs:561-566` 那段 doc 已经【明确指出错误】**（与②同一类、同一文件、铁律 5）：
   它写 `Modular_Description/*` 官方有 340 条「**而它在 `Resources/` 下一条都没有**」
   「`Resources/official_names.json` 只有 `Avatars/Titles/Achievements/Cardbacks/Event_Description/PrebuiltDecks` **六族 = 1327 条**」
   ⇒ **现读 `official_names.json` = 2758 条 / 九族**，其中 `Modular_Description` **340** · `Card_Name` **1065** · `Card_Trait` **26**。
   **这正是本笔 ① 成立的前提**（`AC` 那波就是把值渲染出来的）⇒ 那段 doc 的同一条结论
   「**本支今天会全部「原样留着 + 出声」**」**今天不成立**。
   ⛔ 本笔**按简报要求「顺手发现照报、不许顺手改」没动它** —— 但它与②是同一处文件、同一类缺陷，
   **建议紧接着派一笔**（改法同②）。
2. ✅ **排查掉一个担心（不是缺陷）—— 但我自己先数错过一次，一并留痕**：
   我（`AF`）一度按「**去重后的名字**」数出 `trait_textsprites.json` 只有 **78** 个字形，以为 `AC` 的 87 是错的。
   **再数一次（按【记录】数）**：`characters` = **87 条**（= `Atlas_trait_icon_*` **82** + `Atlas_SpiritStone_*` **5**，与 `AC` 逐字吻合）
   —— 那 87 条里只有 **78 个不重名**（7 个名字各出现 2–3 次、多出 9 条；`glyphs` 又是另一维 **96** 条）。
   ⇒ `AC` 的 87 · `CardIcons.cs:641`「87 个字符名」· `IconSetup.cs:11`「87 个字符 / 96 个字形」**全对**。
   ⚠️ **对本笔断言的影响**：`CardIcons.SpriteExists` 是**按名字**查的 ⇒ 有效集合是那 **78 个名字**
   （我的演算台用的就是它，`has stun: True / has tainted: False` 逐条核过）——**两条都不受这个计数影响**。
   （判据命令：`python -I -X utf8 -c "…len(sp['characters']) / len(set(...))…"`，数据 `d:/4/Unity/数据/游戏数据/trait_textsprites.json`。）
3. ✅ **简报里「`CardIcons.cs` 是 CRLF」这一条不成立**（现核）：`CardIcons.cs` **CRLF 0 / LF 733 = 纯 LF**；
   而 `RuleEngineTest.cs` 确实 **CRLF**（CRLF 29281 = LF 29281）。本笔按**实测**行尾改的
   （`BattleScene.cs` 纯 CRLF · 另两个纯 LF），改完**当场复量**、没翻。

---

## 六、typecheck

```
$ TMPDIR=/tmp/wf_ac2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（另跑了 `WF_DOC=1` 那一档看 XML doc：新增的三处**一条 CS1570 都没带**——
`CardIcons.cs` **不在**那份按文件汇总的告警表里；`BattleScene.cs` 那 4 条是**既有**的
（本笔插的整段是 `//` 普通注释，不是 `///`）。⚠️ 这一档的两个读数按脚本自己的规矩**必须一起报**：
编辑器 XML doc 格式类 **79** 条 · CS1591 **614** 条 —— **都是既有存量，不是本笔引入**。）
