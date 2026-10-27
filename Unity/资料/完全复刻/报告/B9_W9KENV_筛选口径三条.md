# B9_W9KENV · K-11 三条引擎筛选口径 —— 核验报告

> 派活包 **K-11** · 账行 `收藏:188` · `收藏:189` · `收藏:201`
> 目标文件（本次独占）：`RuleEngine/Core/TargetCriteriaEval.cs` · `CardCriteria.cs` · `CountEval.cs`
> 派活包的「现等级」在本包**已过期**（它把三条都写成 P0 待做）—— **现核三条早已是 A**（由 `B9_W6K11` 落地）⇒ 按简报口径**只做核验**；核验中查出**一格真缺口**并当天补掉（见 §一 row 189）。

---

## 一、结论（逐条）

| 账行号 | 现等级（现核） | 原版怎么算 | 我们原来怎么算 | 改成什么 | 怎么验 |
|---|---|---|---|---|---|
| `收藏:188` | **A**（`账/收藏.tsv:188` 状态列 = A · 轮次 `B9_W6K11`） | `FilterMethods.CheckIfMeetsCriteria`：先按 `targetsAffected` 走**早返 switch**（`0x0 any` / `0x14 self` / `0x1e target` / `0x28+0x46 attacker+abilityOwner` / `0x50 actingCard` / `0x5a targetIfMeetsCriteria` / `0x8c previousAbilityTargets` / `0x96 previousAbilityFiltered` / `0xc8/0xc9 referencedCards` / `0xeb warlordsTalent` / `0x64 adjacentToSelfIfMeetsCriteria`），其余档落常规 `&&` 链（`:203-408`），末尾 `if (ta != 100) return 1` + 相邻尾判（`:407-425`） | 已有全仓唯一的 36 格判据；本轮**核验**其 ta 分派与六处订正是否与 `.c` 逐句一致 | **不改**（核验通过）。唯一顺手改动不在此行（在 `TypeGatePasses`，见 189） | `.c` 逐句比对（本次现读 `:55-203` / `:276-300` / `:400-430` / `:490-514`）＋ `AbilityLogic__GetTargets.c:721-740` / `:835-843` 复核「不筛档」＋ 夹具 `_tmp_view/W6K11/probe` 85/85 ＋ `ruleprobe check` 0 差异 / 0 哨兵 ＋ `play check` rc=0 |
| `收藏:189` | **A**（`账/收藏.tsv:189` 状态 = A） | `CheckIfRawCardMeetsCriteria`（`.c:1-104`）：`ta==0 ⇒ true` → 卡种白名单四格（`0x14→+0x18` / `10→+0x16` / `0→+0x17` / **`0x28→+0x19`**）→ `spellType` → `RawCardMeetsTraitCriteria` → 四组 `MeetsStatCriteria`（卡面值）→ `filterSpecificCards`（比 `uniqueId`）→ `armyFilter` → `keyword` + `excludeRace` | 已落地 `TargetCriteriaEval.MatchesRawCard`（`CardCriteria.MatchesRawCard` 为公开口）—— **但第 ② 格只做了三格**：`type="whispers"` 时 `TypeGatePasses` 直接 `return null`（判不了），而**它自己的 doc 与 `CardTypePasses` 注释都写着「whispers 照样保留」**（= 文档与实现不符） | 补 `else if (type == "whispers") need = set.IncludeWhispers;`（判据 `.c:29-34` + 字段 `+0x19`＝`includeWhispers`，`工具/gen_cards_engine.py:2692-2693`）。**零行数变化**（同时把尾部两条 `if/return` 合成一条三元 ⇒ `:1007/:1030/:1062` 三个落点行号不动） | 新夹具 `_tmp_view/W9KENV/probe_whispers`：**9/9**（whispers 正/负/`-1` 未落 · unit/hero/tactic 回归 · **棋盘侧** `Matches`→`CardTypePasses` 同格正负）；原夹具 `_tmp_view/W6K11/probe` **85/85** 无回归；`play check` rc=0（池内 `cardType 40` = **0 张** ⇒ 零行为变化） |
| `收藏:201` | **A**（`账/收藏.tsv:201` 状态 = A） | `RawCardMeetsTraitCriteria`（`.c:1-73`）三句：① 默认 traits（`+0x230`）里有 `summonRift(730)` 且 `traitsFilter != 730` ⇒ **false**（**先于**②）· ② `traitsFilter == 0 ⇒ true` · ③ `excludeTrait ? !has : has`；`manager` / `isPlayer` 体内不读 | 已落地 `TargetCriteriaEval.RawCardMeetsTraitCriteria`（`CardCriteria.RawCardMeetsTraitCriteria` 为公开口）；读**默认** traits（`HasDefaultTrait`），与棋盘那支 `TraitPasses` 读**当前** traits 分开 | **不改**（核验通过） | `.c` 全文逐句比对（含两个谓词体：`t.trait == criteria.traitsFilter` / `t.trait == 0x2da`）；trait id 映射表现读含 330/480/730/980/1185（`TargetCriteriaEval.cs:TraitNameOf`） |

**三条总判**：`收藏:188` / `收藏:201` **核验通过、不改**；`收藏:189` 核验通过但**补了一格**（whispers，池内 0 张、零行为变化）。

### 验收口逐项

| 验收口 | 读数 |
|---|---|
| 每格一条正负夹具 | 188/189/201 三段夹具 = **32 / 32 / 14** 处调用点（＋ CountEval 2 处、其它 1 处 `Ok`），**执行 85 条、失败 0**（两处 `foreach` 各展开 3 次 ⇒ 81 处调用点跑出 85 条）；本次新增 whispers 格 **9 条** |
| `ruleprobe play all dump` 差异逐条解释 | **本批零差异**：`play check` rc=0、结算指纹与基线一致（`0766fbe1…`）⇒ 无需逐条解释（whispers 在池内 0 张，结构上不可能变） |
| 未知 ta = 0 | **成立**：① 池内 1,128 张的 criteria 行 `targetsAffected` **全部有值**（分布现测：0×329 · 10×679 · 20×362 · 30×399 · 50×45 · 51×2 · 52×3 · 55×1 · 60×36 · 65×79 · 80×23 · 90×53 · 100×17 · 106×1 · 110×17 · 113×3 · 120×68 · 130×5 · 140×7 · 150×64 · 160×21 · 161×1 · 162×5 · 180×100 · 200×1 · 201×1 · 210×2 · 220×2 · 240×2 · 245×2），**无缺失 / 无 `-1`**；② 代码侧未知 ta（`CountSetSpec.Unknown`）走 `TargetCriteriaEval.cs:103` ⇒ `null` + `gap`，**⛔ 不是静默 true** |
| `.cs` 秒级类型检查 | **0 / 0**（运行时 0 · 编辑器 0） |
| 账落点闸 | `check_ledger_points.py` 输出里**没有一行**涉及 `TargetCriteriaEval.cs` / `CardCriteria.cs` / `CountEval.cs` ⇒ 本次改动**未引入漂移/失败** |

---

## 二、证据（文件:行号）

### `收藏:188`（目标筛选总入口）

账内证据行号**逐条命中**（未漂）：

| 账里写的落点 | 现读 |
|---|---|
| `TargetCriteriaEval.cs:93`（`Matches`） | `public static bool? Matches(BattleContext ctx, int owner, CardDef.CountSetSpec set, …)` ✅ |
| `:533`（`StatGroupsPass`） | `static bool? StatGroupsPass(…)` ✅ |
| `:710`（`TraitPasses`） | `static bool? TraitPasses(UnitState u, int traitsFilter, int excludeTrait)` ✅ |
| `:880`（`CandidateAdjacentTo`） | `static bool? CandidateAdjacentTo(BattleContext ctx, UnitState thisCard, UnitState candidate)` ✅ |
| `:950`（`KeywordHas`） | `internal static bool? KeywordHas(CardDef c, int keywordFilter, out string gap)` ✅ |
| `:1007`（`TypeGatePasses`） | `internal static bool? TypeGatePasses(string type, CardDef.CountSetSpec set)` ✅（改动后仍在 1007） |

ta 分派与 `.c` 的对照（本次**现读**，不是抄账）：

| ta | 原版 `.c` | 我们的实现 | 判 |
|---|---|---|---|
| 0 | `:57` `return 1` | `:108-109` `case 0: return true` | ✅ |
| 20 / 40 / 70 | `:60`（self，`op_Equality(param_1, target)`）· `:185`（`0x46\\|0x28` ⇒ `op_Equality(target, param_1)` **直接 return**） | `:110-116` 三者同一句 `ReferenceEquals(candidate, thisCard)` | ✅ |
| 30 | `:76` 身份相等即 return（**不落**常规格） | `:117-121` 直接 return 比较结果 | ✅ |
| 80 | `:168` `return 1` | `:113-114` `if (ta == 80) return true` | ✅ |
| 90 | `:193-201` 不等 ⇒ 0；相等 ⇒ **继续** | `:122-132` | ✅ |
| 140 | `:171` `if (ta != 0x8c)` ⇒ 140 **跳过** 150 那段，落到 `:497-510` 的 `List.Contains` **直接 return** | `:133-136` `return PrevTargetsContains(...)` | ✅ |
| 150 | `:172-183` 在表里 ⇒ 落常规链 | `:137-141` | ✅ |
| 200 / 201 | `:92` / `:109`（`0xc9`） | `:144-147` `null` + `gap`（`ResolveTargets` 侧另有实现，见 `EffectResolver.cs:1433-1460`） | ✅ |
| 235 | `:126` `0xeb` | `:148-150` `null` + `gap`（天赋真值表离线拿不到） | ✅ 出声 |
| 100 | 早返 switch **没有**它；尾部 `:407` `if (ta != 100) return 1`，`:411-425` `GetAdjacentUnits` + `List.Find(x == 候选)` | `:151-165` `break` 落常规链；`:380-388` 尾判 `CandidateAdjacentTo` | ✅ |
| 120/130/160/161/162/170/190/220/230/240/245 | 建池侧 `AbilityLogic__GetTargets.c` 各自 `goto LAB_180818485`（返回池、**不逐候选筛**）—— 本次抽验 `0x78`（`:721-740` 本方督军）与 `0xf0`（`:835-843` 最低血）**均 `goto` 返回** | `:182-193` `return true` | ✅ |

「未知 ta 不冒充 true」：`:103` `if (ta == CountSetSpec.Unknown) { gap = "…未知（没落）"; return null; }`。

六处订正的复核（账内声明的行为，本次逐条回源）：

1. `ta=140` 只 `List.Contains`、`ta=150` 才落常规格 —— `.c:171-183` vs `:495-514` ✅（`:136-143` 注释与代码一致）
2. `ta=100` 尾判 = 「候选在 thisCard 的相邻里」 —— `.c:407-425` ✅（`:380-388`）
3. ⑨ keyword = `keyword(+0x98)` **数值全等** —— `KeywordHas` 取 `c.KeywordId`（`:956-957`），旧词表只作 `<0` 时的**出声**兜底（`:958-961`） ✅
4. ⑩ trait 的 summonRift 门**先于** `traitsFilter==0` —— `:1038-1039` 次序正确 ✅
5. ⑪ 血组 altValue = `GetDefaultTraitValue(0x352)`＝护甲 —— 棋盘 `:562-563` 传 `candidate.Armor` ✅
6. ① `excludeSimilarCards` 比 `rawCardData` **对象引用** —— `.c:203-215`；我们 `:218` `ReferenceEquals(thisCard.Card, candidate.Card)` ✅
7. ⑥ 卡种白名单把 `enchantment` 归 `Tactic(20)` —— 改前 `:1012` / 现 `:1013`（新插入的 whispers 行把它下移 1；尾部三元合并又把 `:1030`/`:1062` 拉回原位） ✅

### `收藏:189`（卡面级）

- 判据全文：`d:/2/tools/decomp_full/FilterMethods__CheckIfRawCardMeetsCriteria.c:1-104`（本次**全读**）。
- 四组 stat 的 altValue：攻→`+0xa8`（我们 `RangedAttack`）· 血→`GetDefaultTraitValue(0x352)`（护甲）· 费/稀有度→`0`；`MeetsStatCriteria` 第 2 实参恒 `0` ⇒ `TargetCriteriaEval.cs:1081-1102` + `RawStat`（`:1128-1134`）。
- `filterSpecificCards` 比 `uniqueId(+0x38)` ⇒ `:1103-1110`（`card.Id`）。
- `armyFilter` 的 `LogError` 后 **true** 照抄 ⇒ `:1142-1159`（`armyFilter < 1 || > 3 ⇒ true`）。
- `manager` / `isPlayer` 原版**体内不读** ⇒ 我们签名也不收 ✅。
- **本次补齐的一格**：`.c:29-34` 的四格之一 `0x28(Whispers) ⇒ +0x19`；棋盘侧同四格在 `.c:280-293`（`:290-292` 就是 whispers 那一格）。

### `收藏:201`（默认 traits）

- 判据全文：`FilterMethods__RawCardMeetsTraitCriteria.c:1-73`（本次**全读**）；谓词 `t.trait == criteria.traitsFilter(+0x34)` 与 `t.trait == 0x2da`。
- 三句次序 `.c:47-66` ⇔ 我们 `:1036-1045`：门 → `traitsFilter==0` → `excludeTrait ? !has : has` ✅
- 读**默认** traits（`+0x230`）⇔ `CardDef.HasDefaultTrait`（`Core/CardDef.cs:6438-6452`，doc 明写「只扫 `CardTraits` 这一张」） ✅
- `cardTraits` 没挂 ⇒ `null` + `gap`（⛔ 不当「没有」）✅ `:1037`

---

## 三、没查清的部分

1. **ta = 50/51/52/55/60/65/106/110/113/210 的建池侧语义**（池内有这些行）—— 我们让它们落常规 36 格链；原版 `CheckIfMeetsCriteria.c` 确实**没有**这些档的早返（所以「落常规链」与它一致），但我**没有**逐档回 `AbilityLogic__GetTargets.c` 确认这些档建池后是否真的会走到 `CheckIfMeetsCriteria`。⚠️ 这属于 `收藏:188` 的**既有口径**（`W25` 那批只对 11 个「不筛档」做了建池侧取证），**本次未新增判断、也未改动**。还差什么：逐档读 `GetTargets.c` 对应分支的出口标签。
2. **`ta=235 warlordsTalent`**：仍是 `null` + 出声（天赋真值表 `TalentCardData` 离线拿不到）—— 不是偏差，是**判据缺**。还差什么：天赋表的本地真值（或远端）。
3. **`ta=200/201` 只在 `EffectResolver.cs:1433-1460` 有实现**：我核到那条支存在且按 200/201 分流（`inPlayOnly`），但**没有**验证「所有调用路径都先经它、不会落到 `Matches` 的 `null`」。⚠️ 若将来有人新增调用点，会得到「判不了」而不是「按 referencedCards 判」。
4. **`MatchesRawCard` / `RawCardMeetsTraitCriteria` 目前在仓内 0 消费点**（`grep -rn` 除定义文件外无命中）—— 卡面级的**接线**（`EffectResolver.ChooseCardCandidates` 的 `specific` 回落支）仍是账内挂着的那条**接入请求**，归 `EffectResolver` 的写手（本波是 `S0`）。⇒ 本次补的 whispers 格没有运行时消费者，也就无法由 `play` 观测；它的验证只到夹具层。

---

## 四、改动清单

| 文件 | 改动 | 行数变化 |
|---|---|---|
| `Assets/RuleEngine/Core/TargetCriteriaEval.cs` | `TypeGatePasses`（`:1007`）补第 ② 格的 **whispers(40)** 支：`else if (type == "whispers") need = set.IncludeWhispers;`（判据 `.c:29-34`，字段 `+0x19`）；同时把尾部 `if (need == Unknown) return null;` + `return need != 0;` 合成一条三元，**保持总行数不变**（`:1007` / `:1030` / `:1062` 三个账落点行号不动） | **0**（净 0 行） |
| `资料/完全复刻/账/_parts/B9_W9KENV.tsv` | 账补丁：`收藏:189` 一行（11 列，含完整方法签名）——`差异` / `备注` 两列追加本次核验与 whispers 补齐的记录 | 新增文件 |
| `_tmp_view/W9KENV/probe_whispers/*` | 新离屏夹具（9 条），不进工程 | 新增（草稿） |

⛔ 未改：`ConditionEval.cs` / `RuleCore.cs` / `BoardAnalysis.cs` / 任何别人的文件 / 任何基线或指纹 / 账主表。

---

## 五、顺手发现

1. **账里的夹具条数与夹具文件对不上（不会导致误判，但下次核验会绕圈）**：`收藏:188` 写「81 条」、`189` 写「30 条」、`201` 写「12 条」；现读 `_tmp_view/W6K11/probe/Program.cs` 的分段是 **32 / 32 / 14 / 2**（调用点，合计 81），实际**执行 85 条**（两处 `foreach` 各展开 3 次 ⇒ +4）。⇒ 建议调度台把 `188` 的「81 条」改成「全探针 81 处调用点 / 执行 85 条」（`189`/`201` 的数字同理按分段读）。
2. **同一处「文档说已保留、代码其实没接」在 `TargetCriteriaEval.cs` 里出现了两份**（`TypeGatePasses` 的 doc `:1005` 与 `CardTypePasses` 的注释 `:657-658` 都写「whispers 照样保留」）—— 属于「注释承诺了实现」这一类静默缺口，已随本次补齐同时成立。**同类风险**：`CardTypePasses` 注释里那句「池内 type 分布现读：unit 586 · tactic 446 · hero 56 · defence 39 · enchantment 1」合计 **1,128** ✅ 与实测一致（`defence 39` + `enchantment 1` 都归 Tactic 那一格）。
3. **`CardCriteria.cs` 的两个公开口（`:299` / `:307`）目前 0 消费点** —— 与 `资料/完全复刻/待接线与排队项.md` 里那条接入请求一致；建议该行在接线完成前保持「实现 A / 接入 C」两段式记录，别把「实现已 A」当成「功能已通」。
4. `check_ledger_points.py` 的全局读数是 **A 3755 条 / 通过 3435（告警 140）/ 漂移 247 / 失败 73**（与本次改动无关；本包三个文件在输出里 **0 行**）。⇒ 调度台做重同步点时可直接用这组数对比。
