# B1c · `count` 行的「筛维度」缺口落地（第八十三会话 · 完全复刻）

> 范围：`RuleEngine/Core/CountEval.cs`（**本轮唯一改动的文件**，+112/−27）。
> 由调度台 2026-10-26 裁定：「③ `BoardAnalysis.MeetsCriteria` 保持 **C**，但把 `B1` 收敛出的那个
> **19 条 count 行 / 14 张卡被 `SetGap` 挡成 null** 的真缺口接着做」。
> 判据优先级：🥇 `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**/*.cs` · 🥈 `D:/2/tools/decomp_full/**/*.c`
> ＋ 字段表 `D:/2/tools/il2cpp_out/dump.cs`。

---

## 一、五类筛维度的「值 → 语义」映射（**全部读出**）

先纠一条会带偏整段解码的事实（`dump.cs:23823-23881`，`TargetCriteria` 36 格）：

> **`+0x30` 不是 `keywordFilterOption`，是 `excludeRace`（bool）**；
> **`+0x3C` 不是 `traitsFilterOption`，是 `excludeTrait`（bool）**；
> **`+0x3D` 是 `excludeCondition`**。
> —— 那两个 `…FilterOption` 字段**在原版里根本不存在**（字段表逐行核过）。

### ① `keywordFilter`（`+0x2C`，`Keyword` 枚举）＋ `excludeRace`（`+0x30`，bool）
- **判据**：`FilterMethods__CheckIfMeetsCriteria.c:304-313`
  → `RawCardScript__IsKeyword.c`（**全文**）= `*(int*)(raw + 0x98) == k` —— **纯静态数值全等**，
  比的是 `RawCardScript.keyword`（`dump.cs:23262`）。
- **语义**：`keywordFilter == 0` ⇒ 不筛；否则
  `excludeRace == false` ⇒ **必须有**这个词 · `excludeRace == true` ⇒ **必须没有**。
- **值域**（`dump.cs:45899-45912` `Keyword`）：`5 Infantry · 10 Vehicle · 15 Monster ·
  20 Building · 25 Battlesuit · 30 Drone · 35 Beast · 40 Daemon`。
- **池内 12 行**：`10 Vehicle`（AM76×2 · DA44 · GOF5）· `5 Infantry`（SAU57×5）· `35 Beast`
  （GOF79 · GOF94 · `8f35df05…`）· `40 Daemon`（EC54×2）；`excludeRace` **全 0**。

### ② `traitsFilter`（`+0x34`，`DefinedTrait` 整数）＋ `excludeTrait`（`+0x3C`，bool）
- **判据**：`FilterMethods__MeetsTraitCriteria.c`（**全文**）——
  `Any(候选的当前 traits 列表, t => t == traitsFilter)`，再叠一条**额外守卫**：
  **候选身上有 `summonRift(730)` 且 `traitsFilter != 730` ⇒ 一律淘汰**（`:14-31` 那个
  `if (cVar3 == 0 || filter == 0x2da) {…} else return 0`）。
- **语义**：`traitsFilter == 0` ⇒ 不筛；`excludeTrait == false` ⇒ **必须有**该 trait；
  `true` ⇒ **必须没有**。
- **值域**（`dump.cs:45721-45859` `DefinedTrait`，共 130+ 项）：本池用到的两个值
  **`70 = stealth`**（ASH85）· **`1260 = huntMark`**（SW9 的**卡级** `normalAbilityBoardCriteria`）。

### ③ `currentHealthFilter`（`+0x50`，int）＋ `currentHealthFilterOption`（`+0x54`，`StatFilterOptions`）
- **判据**：`CheckIfMeetsCriteria.c:333-355` → `FilterMethods__MeetsStatCriteria.c`（**全文**）。
  实参 = `MeetsStatCriteria(候选 CurrentHealth, 载体 CurrentMeleeAttack, currentHealthFilter,
  currentHealthFilterOption, 候选 bastion 值)`；
  `CurrentHealth` 那一格 = `ObscuredInt @ +0x68`（`EntityScript__get_currentHealth.c` 全文：
  从 `+0x68..0x78` 拷 20 字节，与 `:335-339` 的 `local_48/uStack_44/…` **逐格对上**）。
- **语义**（`StatFilterOptions`）：`0 NotApplicable` ⇒ 不筛 · `1 EqualTo` = `health == f` ·
  **`2 EqualOrHigher` = `f <= health`** · **`3 EqualOrLower` = `health <= f`** · `4/5/7` 与载体比 ·
  `6` = `health + bastion <= f` · `8 RandomChance` ⇒ **原版 switch 里没有 `case 8`**、落 `return true`
  （⛔ 别按枚举名自己加一次 RNG）· `9/10/11` 比第 5 实参。
- **池内 2 行**：`AM54`（`f=10 · opt=3` ⇒ 血 ≤ 10）· `GOF105`（`f=5 · opt=2` ⇒ 血 ≥ 5）。

### ④ `filterSpecificCards`（`+0x70`，`List<RawCardScript>`）
- **判据**：`CheckIfMeetsCriteria.c:465-483` —— 逐个比 `PlayerItem.uniqueId(+0x38)` 字符串全等
  （`dump.cs:18813`）。
- **语义**：**白名单**（候选必须是其中之一）；列表空 ⇒ 不筛。⚠️ 原版是
  「**穷举完还没命中 ⇒ `return 0`**」的**全等**语义，⛔ 不是「包含/前缀」。
- **池内 1 行**：`GOF15` = `['GOF12','GOF21']`（卡面「If you control a **Spanner** or
  **Mekboy Gazmek**」—— 与这两张卡一一对应）。

### ⑤ `selectFromChooseResolution`（`+0x80`，bool）—— **判据读不出，没做**
- `dump.cs` 里**只有字段声明**；`decomp_full` 全树**没有任何消费点**（连 `+0x80` 的访问都定位不到）。
- ⇒ 按红线**不猜**：继续挡在 `CountEval.SetGap`（`countSet.selectFromChooseResolution 未实现`）。
- 池内 1 行（`ASH82`）—— 但它的 `targetsAffected = 50`（手牌/牌库族）**本身也没实现**
  ⇒ 这一行是**两层缺口**，就算补了 `selectFromChooseResolution` 也仍然判不了。

---

## 二、改了哪三处（全部在 `CountEval.cs`）

### 改动 1 · `SetGap`（`:365`）放开六格
删掉对 `KeywordFilter` / `TraitsFilter` / `CurrentHealthFilterOption` / `FilterSpecificCardIds` /
`ExcludeRace` / `ExcludeTrait` 的拦截（它们现在真能判了）。
**仍挡着**：`armyFilter` / `spellType` / `attackFilterOption` / `manaCostFilterOption` /
`rarityFilterOption` / `excludeEnchantments` / 四个布尔排除 / `selectFromChooseResolution`。
前六格 `TargetCriteriaEval` **其实已有实现**，本笔按「一次只放池内有行的类」保守处理
（这六格池内现读 **0 行**）。

### 改动 2 · 棋盘跳 + `ta=150` 跳 → 改走 `TargetCriteriaEval.Matches`（**唯一判据，没有第二份**）
新增 `BoardCandidatePasses(...)` = 一句转调
`TargetCriteriaEval.Matches(ctx, owner, set, u, null, actor, null, out gap)`。
`TargetCriteriaEval`（825 行）**早就实现了整条 36 格**：`KeywordPasses` / `TraitPasses` /
`StatGroupsPass`（四组 `MeetsStatCriteria`）/ ⑫ `filterSpecificCards` / ⑫ 附魔 / ⑭ army …
⇒ 本笔只是**把它接上**，⛔ 没有在 `CountEval` 里另写一份筛法。

⚠️ **`cond` 不在这里判**：调用方随后仍调 `ConditionEval.MeetsUnit/MeetsCard`（`CountEval` 的既有分工）
⇒ 给 `Matches` 传 `cond: null`（= `none(0)` = 放行），**不判两次**。
⚠️ `thisCard` 传 `actor`（= 这条 ability 的所有者单位）；只有 `4/5/7` 三档 stat 需要载体，
`Matches.TryStatThisCardStat` 已按档判过。

### 改动 3 · 墓地跳（`ta=60`）→ 新增 `CemeteryCandidatePasses`
墓地候选是 `BattleContext.DeadUnit`（只有 `Card` / `Owner` / `Instance`），**没有运行期状态**
⇒ 用不了 `Matches`（它收 `UnitState`）。这一支只判**静态**的那两维：
- `filterSpecificCards` → `CardDef.Id` 全等（生成器已把 PPtr 解成同一套 id）
- `keywordFilter` → **`CardDef.KeywordId == k`**（照原版 `rawCardData.keyword(+0x98)` 的**数值全等**；
  ⚠️ 这里**没有**走 `CreatePool.MatchesKind` 那套词表 —— 原版这一格是数值比较，
  两条对池内 Vehicle/Infantry/Beast/Daemon 取值等价，如实并列）。
其余（`traitsFilter` / 四组 stat）⇒ `null` + 出声（「墓地候选没有单位实体」）。

**合并旧 helper**：原来的 `IncludeUnit`（卡种 / 残骸 / 自他）已被上面两条取代 ——
它的那几格 `TargetCriteriaEval` 里都有（`CardTypePasses` / `includeRemnants` / `includeSelf` /
`excludeNotSelf`），删掉它是「判据只留一份」。

---

## 三、可达性：**18 条 / 13 张卡**从「判不了」变成「真判」

| 结果 | 行数 | 卡 |
|---|---|---|
| ✅ 现在真判 | **18** | ASH85 · AM76×2 · AM54 · DA44 · EC54×2 · SAU57×5 · GOF79 · GOF94 · `8f35df05…` · GOF105 · GOF15 · GOF5 |
| ❌ 仍判不了 | 1 | ASH82（`selectFromChooseResolution` 判据读不出 **＋** `targetsAffected=50` 本身没实现） |

---

## 四、为什么 `ruleprobe` 测不到差异（**如实标，不是没生效**）

1. **生产路径**：这 19 行的 ability trigger ∈ `{0, 46, 410, 600, None}`
   ⇒ **都不在**今天接线启用 board 闸的七族（`50/90/510/570/515/60/291`）里
   ⇒ `ActionPump.BoardLevelCriteriaAllows` **今天一次都不会跑**这些行。
2. **`ruleprobe play` 夹具**：9 张卡（AM76/EC54/SAU57/GOF15/SW9/ASH85/AM54/GOF105/GOF94）
   逐张比过，**改前 / 改后输出逐字节相同**。
3. **灭自证（关键一步）**：把本轮新接的四维在 `BoardCandidatePasses` 里**临时全部强制 `false`**
   再跑同一批卡 ⇒ **输出仍然逐字节相同** ⇒ 证明 `CountEval` 的棋盘跳在今天的夹具里
   **根本没被走到**（不是「改了没用」，是「这条路上没车」）。
   —— 那一行临时补丁已删除（`grep TEMP-PROBE` = 0）。

⇒ 本笔是**判据闭合**（把「判不了 ⇒ 退回正文正则 + 出声」变成「结构化真判」），
**今天零可观察差异**；`play check` 的 5 张差异卡与 fingerprints 都不是本笔造成（A/B 见 §五）。

**剩下要让它可观测，只有两条路**（都不在本代理白名单）：
① 把 board 闸接到更多 trigger 族（`ActionPump.AbilityLevelCriteriaAllows` 的族名单，`A3` 面）；
② 给 `ruleprobe` 加一个 `gate <卡id>` 读数口（`工具/ruleprobe/*.cs`）。

---

## 五、指纹 / typecheck（改前 vs 改后，A/B 换文件实测）

| 读数 | **改前**（`git HEAD` 版 `CountEval.cs`） | **改后** |
|---|---|---|
| `typecheck` | — | **运行时 0 / 编辑器 0** |
| `ruleprobe check` | rc=0 · 解析差异 0 行 · 哨兵失败 0 条 | **rc=0 · 解析差异 0 行 · 哨兵失败 0 条**（逐字相同） |
| `ruleprobe play check` | 指纹 `af8e817839fa1dd199d33320f6bf1b2e` · 主档 1128 张全等 · 电池 5 张卡（GSC11/GSC15/GSC25/GSC69/GSC7） | **指纹 `af8e817839fa1dd199d33320f6bf1b2e`（完全相同）** · 同样那 5 张卡 |

⇒ **本笔对结算结果零差异**（与 `B1` 那两族同一个结论）。
基线 `6e888bbe1415ee84f05ecb82ec1111d1` 早就不符，是**并发写手**造成（`RuleCore.cs` / `ActionPump.cs`
的 mtime 都晚于本代理开工；`B1` 那份报告里有同样的 A/B 隔离实证）——**请调度台按并发那批重钉**。

---

## 六、没查清的（如实标）

1. **`traitsFilter` 的 `summonRift(730)` 额外守卫仍未实现** —— 它写在
   `FilterMethods__MeetsTraitCriteria.c:14-31`（候选带 730 且 filter ≠ 730 ⇒ 一律淘汰），
   而 `TargetCriteriaEval.TraitPasses` **没有这一条**（不是本笔的文件范围）。
   池内现读 **0 条** count 行是 `traitsFilter=730`，且 `TraitNameOf` 也不认 730 ⇒ 今天由 `null` 兜住。
   **建议下一次 `TargetCriteriaEval` 的写手补这一条**（3 行）。
2. **`selectFromChooseResolution` 语义查不到**（`+0x80`，只有字段声明）。
3. `armyFilter` / `spellType` / 三组其它 stat / `excludeEnchantments` 六格**没放开**
   （`TargetCriteriaEval` 有实现，池内 0 行）—— 放开与否请调度台定；本笔按 `B1` 的保守口径只放有行的。
4. `ta=160/161/162`（`currentMana` / `currentSpiritStone` / `currentFaith`）**25 条 count 行仍判不了**
   （`CountSet` 没有这三支；`TargetCriteriaEval` 把它们当「原版这一档不筛 ⇒ true」，两处口径不一致）
   —— 这是**与筛维度无关的另一层缺口**，不在本轮范围，如实登记。

---

## 七、账
- `账/_parts/B9_B1c.tsv`（**1 行**：`BoardAnalysis.MeetsCriteria(...)`，状态保持 **C**）
  —— `merge_ledger_parts.py --dry --only B9_B1c` 报「更新 1 / 新增 0」。
- `账/_parts/B9_B1b.tsv`（**4 行**：`BroadcastCardCreated` → **D** · `BroadcastTrapResolved` → **D** ·
  `BroadcastQuestResolved` 判据出处/备注补全 · `TriggerOtherCardSpiritStone` 备注收口）
  —— ⚠️ **合并顺序**：`B9_L5.tsv` 与它在 `BroadcastQuestResolved` 上撞键，
  `check_patch_conflicts.py` 判定字典序 `B9_L5` 赢 ⇒ **请先 `--only B9_L5`、再 `--only B9_B1b`**
  （我这一份带着更全的判据：ILSpy `:27890-27919` · 六 bool 订正 · `CardScript__RemoveEffect.c`
  是**唯一**调用点 ⇒ 我们只覆盖了「督军满阈值」那一支）。
