# B9_W52LAND · 账落点口径（§4.1 例外）+ 前置升 A

> 代理：B9_W52LAND（2026-10-27）· 执行型
> 上游：`判据/战斗逻辑驱动_B行重裁.md`（W48BROW · §三 可执行清单 + §五 顺手发现①）
> 裁定：调度台 2026-10-27 —— `§4.1` 加「纯取值器可用字段/属性/嵌套类型声明当落点，只报告警不计失败」的例外。

---

## 结论

1. **工具已改**：`工具/check_ledger_points.py` 新增「成员级落点」判定（`MEMBER` 告警）——
   `public/private/…` 开头的**字段 / 属性 / 嵌套类型**声明算**合法但只报告警**，⛔ 不计失败；
   注释行 / 空行 / 语句中间（局部变量语句）**仍判失败**（用「必须以访问修饰符开头」挡掉局部变量）。
   自带回归自检 **77/77 PASS**；全表只读跑通、**无 Python traceback**。
2. **方案正本已改**：`资料/完全审查与完全复刻_方案.md` §4.1 就地加了这条例外（写在「纯属性 getter 行不得当方法落点」那段**之后**），只动这一节。含防滥用两条。
3. **升 A 实际落地 10 条**（不是我简报里的 11 条）：
   - F7 的 3 条真方法落点（280 / 326 / 380）—— **用现读行号**（`ApplyDamage` 已从 8431 → **8436**，见下）。
   - F6 本地化 **7 条**（1221 / 1229 / 1230 / 1232 / 1233 / 1234 / 1239）。
   - 🔴 **`1241 GetLocalizedTargetted()` 未升 A**（第 8 条本地化）：**key 存在但【无消费者】**——
     我们代码全树 **0 处** `Card_Targeted/*`（`grep -rn "Card_Targeted" Assets` = 0），且 `BattleDriver.cs:7959-7961`
     明写「tooltip 省略」（卡表无 `targetedText` 列）。按 §4.1「方法级落点」它**没有等价实现** ⇒ 升 A 会造**假 A**。
     本件只把它记在「没查清/建议」，⛔ 未改它（保持 B）。
4. **账补丁**：`账/_parts/B9_W52LAND.tsv`（10 行，11 列，LF）。机检 **10/10 通过 · 0 失败 · 0 漂移**；
   `merge_ledger_parts.py --dry` 试合 ⇒ **更新 10 / 新增 0 / 无变化 0**（10 个键各命中恰好 1 行）。

### mini15 前后读数（自检要求）

命令 = `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py <fixture>`
> 读数快照：`ApplyDamage@8436`（`RuleCore.cs` 是 W43 活文件，行号会漂；若 `ApplyDamage` 再漂，AFTER-修 那一行会多出 1 条**漂移**、通过数不变）。

| 阶段 | fixture | A 条 | 通过 | 告警 | 漂移 | 失败 |
|---|---|---:|---:|---:|---:|---:|
| **BEFORE**（HEAD 版工具，未改） | `_tmp_view/w48/mini15.tsv`（W48 原样） | 15 | **1** | 6 | 0 | **14** |
| AFTER（新工具，landings 未改） | `_tmp_view/w48/mini15.tsv` | 15 | 12 | 17 | 0 | 3 |
| **AFTER-修**（新工具，3 条方法行改成现读落点） | `_tmp_view/w48/mini15_after.tsv` | 15 | **14** | 14 | 0 | **1** |

> ⚠️ W48 报告记的是 BEFORE「通过 0 / 失败 15」；本机现跑 HEAD 版工具是「通过 1 / 失败 14」（工具在 W48 之后又加固过，那 1 条是 `DealMultiDamageVariable` 的散文括号只告警）。
> **简报预期「通过 3 / 告警 12 / 失败 0」的实际回收**：
> - **「3 条真方法落点那三个要过」—— 达成**：280/326/380 三条在 AFTER-修 里是**干净通过**（无告警，因为落点是真方法声明）。
> - **「12 条非方法行变告警」—— 11/12 达成**：11 条字段/属性/嵌套类型行变 `MEMBER` 告警；告警总数 14 是因为其中 3 条（premulligan 族）**另有**一条 `PROSE` 多标识散文告警叠加。
> - **「失败 0」—— 差 1 条**：失败的是 **L156 `AddRemoveFromCemetery`**，它**不是**字段/属性行，而是「**无落点**」行 —— 落点整格是散文「未定位……`EffectResolver.cs:4555` 的 `DeadCandidates`」，那个 `EffectResolver.cs` 是**裸文件名缺目录**（真身 `RuleEngine/Core/EffectResolver.cs`）且 `:4555` 是 `{` 行 ⇒ **file-not-found**。这条例外**不该**覆盖它（它本来就没有等价物/落点），故仍失败是**正确**的；它属 W48 §三「F7 的 12 条字段/无对象」里的「**无对象**」那一条。

---

## 证据（文件:行号）

### A. 3 条真方法落点（现读核过 —— `RuleCore.cs` 正被 W43 改，行号为**本次现读**）

| 账行 | 新落点（现读） | 核验 |
|---|---|---|
| **280** | `RuleEngine/Core/RuleCore.cs:8436（ApplyDamage）` | 8436 行 = `public static int ApplyDamage(BattleContext ctx, UnitState u, int dmg, string source,` ✅（原 8431，因 W43 在文件上方插行漂到 8436） |
| **326** | `RuleEngine/Core/RuleCore.cs:5374（IsValidTarget）` | 5374 行 = `public static int IsValidTarget(BattleContext ctx, int p, int atkSlot, int tgtP, int tgtSlot,` ✅ |
| **380** | `RuleEngine/Core/RuleCore.cs:1662（BeginTurn）（等价实现 = XOR 选值在 RuleCore.cs:1754）` | 1662 = `public static void BeginTurn(BattleContext ctx)` ✅；XOR 选值现读在 **1754** = `p.MaxEnergy = (p == ctx.Players[ctx.SecondSeat] ? ctx.Vars.startingManaSecond : ctx.Vars.startingMana)` ✅（账里旧 `:1738` 已漂，本件在 `差异` 列一并把 `1738→1754` 改准） |

🔴 **`RuleCore.cs` 是 W43 的活文件**：本件读的过程中它从 **15065 行 → 15153 行**、`ApplyDamage` **8431 → 8436**。
⇒ 补丁里写的是**现读行号**，但**行号随时会再漂**；真正防漂的是第一括号里的**裸声明名**（`（ApplyDamage）`），
`check_ledger_points.py` 会按名重定位（漂移不算失败）；`--fix` 对多行签名的方法行会给 NEED-HUMAN、不自动改。

### B. 7 条本地化落点（term 族 key 现读在官方 13 语言表 · 取词路径现读用 Loc）

落点统一 `CardPresentation/Core/Loc.cs:3421（T）`（3421 = `public static string T(string key)` ✅）。
每行核到的 term 族 key 数（`Resources/official_loc_13lang.json` 的 `keys` 现读，共 10,862 键）：

| 账行 | 原版方法 | term 族 | key 数 | 我们的取词路径（现读） |
|---|---|---|---:|---|
| **1221** | `GetCardNameTerm()` | `Card_Name/*` | **1198** | `CardText.Name`→`TermOr`→`Loc`（`CardText.cs:847` 建 `Card_Name/<id>`） |
| **1229** | `GetLocalizedAbilityDesc(string)` | `Card_Abilities/*` | **120** | `SkillPanel.AbilityRaw` **直调 `Loc.T(key)`**（`SkillPanel.cs:2231`） |
| **1230** | `GetLocalizedAbilityDescRemovingSiege(string)` | `Card_Abilities/*` | **120** | `SkillPanel.AbilityRemovingSiege`（`SkillPanel.cs:2202`）按原版 4 个 `[[reload]]` 变体剥除 |
| **1232** | `GetLocalizedCardDesc()` | `Card_Description/*` | **1261** | `BattleDriver.FaceTextFull` 用 `Loc.OfficialCurrent("Card_Description/"+c.Id)`（`BattleDriver.cs:28013`） |
| **1233** | `GetLocalizedCardDescWithLineBreak()` | `Card_Description/*` | **1261** | 同上 desc 源 + 渲染层折行（原版 = `GetLocalizedCardDesc()` + `AppendLine()`，见 `decomp_full/RawCardScript__GetLocalizedCardDescWithLineBreak.c`） |
| **1234** | `GetLocalizedCardName(string overrrideLanguage)` | `Card_Name/*` | **1198** | `CardText.Name(RuleEngine.CardDef c)` 用 `Loc.Lang13Current(CardNameTermPrefix+c.Id)`（`CardText.cs:893`） |
| **1239** | `GetLocalizedFlavourText()` | `Warlord_Lore/*` | **1157** | `Loc.Lore(cardId)`（`Loc.cs:3346`）走 `LoreTerm("Warlord_Lore/"+id)`；2 个现读消费点（`CardDisplayWindow.cs:1714` / `CardDetailPopup.cs:1251`） |

> ⚠️ **落点精度**：1229/1230 的实际取词口**就是 `Loc.T`**（SkillPanel 直调）；1232/1233 的实际取词口是**兄弟方法** `Loc.OfficialCurrent`（`Loc.cs:3084`），1239 是 `Loc.Lore`（`Loc.cs:3346`）。
> 简报统一指定 `Loc.cs:3421 T()`，本件照办（`Loc.T` 三档兜底确实能解析这五族官方键）；**若调度台要落到「真实调用点」的精确一行**，可把 1232/1233 改指 `Loc.cs:3084（OfficialCurrent）`、1239 改指 `Loc.cs:3346（Lore）`（都已核过是真实声明名），见「顺手发现 ①」。

### C. L1241 为什么不升 A（证据）

- `grep -rn "Card_Targeted" Assets --include=*.cs` ⇒ **0 命中**（全仓；只有 `official_loc_13lang.json` 数据里有 31 条 `Card_Targeted/*` 键）。
- `cards_engine.json` 的卡字段里**没有** `targetedText` / `targetingTextReferenceId`（现读 `cards[0].keys()`）。
- `BattleDriver.cs:7959-7961` 现读原文：「**tooltip 省略**：原版的文案来自 `RawCardScript.targetedText`，我们卡表没有这一列 ⇒ 不编一句自造文案」。
⇒ `GetLocalizedTargetted()` 在我们侧**没有实现**（不是「字段直读」，是「根本没接」）。升 A = 假 A。**建议下一批按 F6 复核判 B→C**（把 `Card_Targeted/*` 接进 tooltip，或判 D 说明原版该文案的触发条件在本 build 是否可达）。

---

## 没查清的部分

1. **`GetLocalizedCardName` 的 `overrrideLanguage` 形参**：原版支持「强制指定语言」；我们 `CardText.Name` 只按**当前语档**取，**没有**「忽略当前语档、强制某语言」的入口。本件按「净结果 = 当前语言下取到正确卡名」判 A，但**「override 指定语言」这一档是否原版有实际调用点**没查（需要查 `LocalizationManager.GetTranslation` 的 `overrideLanguage` 实参在 `RawCardScript` 各调用点是否为 null）。**还差**：`RawCardScript__GetLocalizedCardName.c` 里 `overrideLanguage` 的来源链。
2. **`GetLocalizedCardDescWithLineBreak` 的完整体**：`decomp_full` 那份到 `AppendLine` 之后是「Indirect jump 未能恢复」，**是不是只 AppendLine、还是 AppendLine 后再接能力描述**没读全。本件按「同一 `Card_Description/` 源 + 折行」判 A；若真后续还接别的段，需回来核。
3. **`ApplyDamage` 现读行号会继续漂**（W43 在改 `RuleCore.cs`）：补丁写的是 8436，合表时可能又要 `--fix` 或按名重定位。**这不是「查不到」，是活文件**。

---

## 改动清单

| 文件 | 改了什么 |
|---|---|
| `工具/check_ledger_points.py` | ① 加 `WARN_MEMBER` 告警类 + `WARN_TITLE` + 打印顺序 `_WARN_PRINT_ORDER`；② 新增 `member_decl_kind()`（识别 `public/private/…` 开头的字段/属性/嵌套类型声明；局部变量语句因无访问修饰符仍返回 None）；③ `find_decl_candidates()` 接受成员声明、并把名字匹配后缀加 `$`（处理 `public int Count` 这类 `{` 在下一行的属性块）；④ `check_file()` 最终闸：`looks_like_decl` 失败时再试 `member_decl_kind`，命中则记 `MEMBER` 告警、放行；⑤ 文件头 + `--warn-getters` 文档改准（四类→五类告警）。 |
| `资料/完全审查与完全复刻_方案.md` | §4.1 在「纯属性 getter 行不得当方法落点」段之后**新增一段 blockquote**：纯取值器可用字段/属性/嵌套类型声明当落点、只记 MEMBER 告警；防滥用两条；⛔ 别放宽到注释/空行/语句中间；不许加转发方法绕闸。**只动 §4.1 这一处。** |
| `账/_parts/B9_W52LAND.tsv` | 新建。10 行（280/326/380 + 1221/1229/1230/1232/1233/1234/1239），状态改 `A`、`我们的落点` 改新落点、`备注` 追加 B9_W52LAND 2026-10-27 升 A 说明（380 的 `差异` 列顺带把 `RuleCore.cs:1738 → 1754` 改准）。11 列、LF。 |

⛔ **未动**：`Assets/**`（本件不碰代码）· 账主表 · 其他代理的报告/基线 · 其他 `_parts/*`。

---

## 顺手发现

1. 🔴 **落点精度（口径，供调度台定夺）**：简报统一要求 `Loc.cs:3421 T()`，但**三族的实际取词口是兄弟方法**——
   `Card_Description`（1232/1233）现读走 `Loc.OfficialCurrent`（`Loc.cs:3084`），`Warlord_Lore`（1239）现读走 `Loc.Lore`（`Loc.cs:3346`）。
   `Loc.T` 的官方全表兜底确实**能**解析这些键（五族都在 `official_loc_13lang.json`），所以不算「没实现」；但按 §4.1「方法级落点」，**更精确的落点**应是那两个兄弟方法。本件按简报照办（统一 T），这里如实报出来 —— 是否改精确，请示下。
2. 🔴 **`RuleCore.cs` 行号活漂（影响所有指向它的 A 行）**：W43 正在改，本件读的过程中文件 +88 行、`ApplyDamage` +5 行。
   ⇒ 任何「写死 `RuleCore.cs:<行>`」的账都可能下一次就漂。**建议**：这条线的 A 行落点尽量靠**裸声明名**重定位，`--fix` 遇到多行签名方法行会 NEED-HUMAN（本件核过：不会乱改）。
3. 🟡 **全表只读跑通对照（HEAD 工具 vs 本件工具，同一快照隔离）**：
   `通过 2896→2898 · 告警 181→183 · 漂移 766→768 · 失败 121→117`（**−4 失败 · +2 漂移 · +2 告警 · 0 新增失败**）。4 条原失败 A 行去向：
   - 2 条 `IsAnyVoiceLinePlaying()`（`UnitChatPanel.cs:369（IsSpeaking）`）⇒ 现在按名重定位到**真声明 `public bool IsSpeaking`（现读 :393）**，报**正确漂移**（`--fix` 因方法块跨行给 NEED-HUMAN，不自动改）。
   - `CompleteReassembleAnim()`（`CardFeel.cs:3289-3300（Reassemble(…)）`）⇒ 3289 现读是 `public const float ReassembleTime = 0.1f;`（**const 字段**）⇒ `MEMBER` 告警放行。⚠️ 落点首行其实是常量、真方法 `Reassemble` 在 3291 起 ⇒ **该行落点宜再精确**（不是本件范围）。
   - `SameTimingAsEffect(…)`（`CardInstance.cs:172（HandEffect …）`）⇒ 172 = `public sealed class HandEffect`（**嵌套类型**）⇒ `MEMBER` 告警放行。
   ⇒ 变化全部可解释、无「把错的洗成对的」；但也说明 **`MEMBER` 只负责「放行 + 告警」，精度仍要人读**。
4. 🟡 **`WARN_MEMBER` 与 `WARN_PROSE` 会叠加**：同一行（如 premulligan 族「无落点 —— …字段 `preMulligan`」散文）既有散文括号告警、又有成员落点告警 ⇒ mini15 里 11 个成员行报出 14 条告警明细（3 条是叠加）。看合计时别把「告警条数」当成「行数」。
5. 🟡 **`AddRemoveFromCemetery`（账 156）是「无对象」行，落点写成了 `EffectResolver.cs:4555`（裸名缺目录 + `{` 行）** —— 它本来就该留 B（原版那一跳只清重入闸、我们同步结算无该状态）。**下一批 F7 收尾时**这条要按「无对象」处置（要么补一个真实等价落点，要么按 §4.1 D 口径判），⛔ 别指望 `MEMBER` 例外救它。

---

## 交接给下一个代理（本件已收尾，无需续做）

- 工具、文档、补丁**都已落盘并各自验过**（selftest 77/77、补丁机检 10/10、merge --dry 10/0/0）。
- ⏭ **下一批（不在本波）**：F4/F3 转 C · F5 的 D 二分 · F13 的三条调试串判 D · **L1241 判 B→C**（本件报告 §证据 C 给了判据）。
- ⛔ **别改**：`RuleCore.cs`（W43）· `SkillPanel.cs`/`CardDisplayWindow.cs`（W44）—— 本件只是**读**它们取现读行号。
