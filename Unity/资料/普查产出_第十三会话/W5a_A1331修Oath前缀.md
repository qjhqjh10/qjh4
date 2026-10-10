# `W5a` · 修 `A1331`（4 张卡英文 `desc` 丢了段首 `Oath ` 前缀）

> 2026-10-10 第十三会话。**执行写手代理**（本批唯一在改这几个文件的人）。
> ⛔ 没动 git · ⛔ 没改正本 · ⛔ 没跑 Unity · ⛔ 没手改 `cards_engine.json` · ⛔ 没碰别的 `.cs`。
> ✅ 判据正本 = `资料/普查产出_第十三会话/R5_A1330到A1332现核.md` 的 `A1331` 一节；成品卡图 4 张**逐张亲读**。

---

## 一、做了什么（改的是哪一列 / 哪几个键）

**只改一列 = `数据/游戏数据/cardface_fixes.json` 的 `desc` 列**（该列原有 **243** 条 → 现 **247** 条）。
落法：**在列尾追加**（与本文件既有的「新批次追加在末尾」惯例一致）。
新值一律是 **`"Oath " + 旧 desc`**（这样 `Oath ` 之后的字**逐字节照抄**，不重打一遍）。

| # | 键（卡名） | id | 原值 | 新值 | 卡图出处（亲读） |
|---|---|---|---|---|---|
| 1 | `Devastator Marine` | `UM80` | `4: Deal 5 damage to an enemy troop` | `Oath 4: Deal 5 damage to an enemy troop` | `d:/2/Warpforge部队卡片/Ultramarines/3部队/Warpforge_16_Devastator-Marine.png` —— 卡面 `Blast 2. ⟨蓝圆盘+白袍人形⟩ Oath 4: Deal 5 damage to an enemy troop` |
| 2 | `Phobos Librarian` | `UM81` | `3: Deal 2-4 damage to an enemy. If target dies, gain Shield` | `Oath 3: Deal 2-4 damage to an enemy. If target dies, gain Shield` | `…/Warpforge_17_Phobos-Librarian.png` —— `Camouflage. ⟨徽记⟩ Oath 3: Deal 2-4 damage to an enemy. If target dies, gain ⟨图标⟩ Shield` |
| 3 | `Scout Sniper` | `UM_Scout_Sniper` | `1: Deal 1 damage` | `Oath 1: Deal 1 damage` | `…/Warpforge_11_Scout-Sniper.png` —— `Camouflage. Sniper. ⟨徽记⟩ Oath 1: Deal 1 damage` |
| 4 | `Firestrike Servo Turret` | `UM74` | `3: Gain Sentry 2` | `Oath 3: Gain Sentry 2` | `…/Warpforge_10_Firestrike-Servo-Turret.png` —— `Long Range. Sentry 2. ⟨徽记⟩ Oath 3: Gain Sentry 2` |

- ✅ **4 张全部用裸卡名作键**（无同名跨阵营冲突 —— 实测这 4 个名字在全池各 1 张，全是 `Ultramarines`）。
- ✅ **卡图数字逐张核过**：`4` / `3` / `1` / `3`，与 R5 的表**逐格一致**（R5 提示「我也可能抄错」，所以这里是自己开图看的）。
- ⛔ **中文一个字没动**（`descZh` 段首本来就是 `誓言 N：`，`Loc` 的 `Card_Trait/oath` 中文列就是「誓言」，判据 ① 早就命中）。
- ⛔ **`keywords` 一个字没动**；⛔ 没在解析层兜底。
- 另加 **1 条说明条目**到同一文件的 `_manual_desc_note`：键 = `_2026-10-11_A1331_Oath前缀`（`_manual_desc_note` 18 条 → 19 条），写清根因、判据链、4 张的卡图坐标、以及两条被否掉的改法。
  ⚠️ 该键排在块尾（`_2026-10-18_*` 之后）—— 本文件的键序本来就不是严格时间序，JSON 成员序无语义。

## 二、生成（跑的命令 + 读数）

```
# ① 改前先对账，证明「生成物 = 脚本产物」⇒ 我的改动是唯一的 delta
cd d:/4/Unity/工具 && PYTHONIOENCODING=utf-8 python gen_cards_engine.py --check
  →  --check：现有 …/cards_engine.json 一致 ✅

# ② 改数据后走正规通道重生成
cd d:/4/Unity/工具 && PYTHONIOENCODING=utf-8 python gen_cards_engine.py
  →  卡面核对修正 字段 47 处 · 关键词 226 处
     卡面修正表：所有键都匹配上了 ✅          ← 4 个新键全部命中，无孤立键
     写出 …/RuleEngine/Resources/cards_engine.json  (375 KB)

# ③ 再对一次账
python gen_cards_engine.py --check → 一致 ✅
```

- ⚠️ **必须带 `PYTHONIOENCODING=utf-8`**：脚本里有 `✅`/中文，本机控制台是 **GBK**，不带会在 `print` 上抛 `UnicodeEncodeError` **崩在 --check 之前**（第一次跑就崩了，见下面「顺手发现」）。
- ℹ️ 该通道的**正常副产物**：脚本每次会重写 `d:/4/_tmp_view/card_ids_made.txt`（自造 id 清单，131 张）—— 内容不变，属正规通道的一部分。
- ✅ **`cards_engine.json` 只有这 4 处变**（拿 `git show HEAD:…` 与现文件做**逐字段对账**）：
  ```
  count 1126 / 1126 · id 集合不变 · 顶层 meta 完全相同
  field-level diffs = 4
     UM80            | desc | '4: Deal 5 damage to an enemy troop'  -> 'Oath 4: …'
     UM81            | desc | '3: Deal 2-4 damage to an enemy. …'   -> 'Oath 3: …'
     UM_Scout_Sniper | desc | '1: Deal 1 damage'                    -> 'Oath 1: Deal 1 damage'
     UM74            | desc | '3: Gain Sentry 2'                    -> 'Oath 3: Gain Sentry 2'
  ```
  ⛔ **没有人手编辑生成物** —— 4 处全是 `gen_cards_engine.py` 从 `cardface_fixes.json` 的 `desc` 列盖出来的（那行 `face_fixed.append((name,"desc",…))` 就是）。

## 三、验证（两个症状分别怎么验的）

### 症状 ① 显示：`Oath 1` 错误数字 + 正文孤儿 `N:`

**静态验，已证**。判据链：`Core/CardText.cs:213` 判据 ① = `hay.IndexOf(word, OrdinalIgnoreCase) >= 0` 就跳过（`hay` = `c.Desc`，`Battle/BattleDriver.cs:11641` 传的；`word` = `KeywordEn(key)`）。
`Core/Loc.cs:2107` 实测 = `{ "Card_Trait/oath", new Entry("誓言", "Oath") }` ⇒ `KeywordEn("oath") = "Oath"`。
新 `desc` 段首就是 `Oath` ⇒ 判据 ① 命中 ⇒ `Oath` **不再被补** ⇒ 「`Oath 1`」与「孤儿 `N:`」两个现象**同时消失**。

**离线复刻了一遍 `KeywordSegment` 的 ①②③**（判据表从 `Loc.cs` 的 62 条 `Card_Trait/*` 实读；③ 表 = `Badges.cs:334-338` 那 12 键），拿 `git show HEAD` 的旧文件与现文件各跑一遍：

```
KeywordSegment 会补的项：old = 391  new = 387   delta = 4
只在 old（= 被修掉的）: ('UM80','Oath','Oath 1') ('UM81','Oath','Oath 1')
                        ('UM_Scout_Sniper','Oath','Oath 1') ('UM74','Oath 3','Oath 3')
只在 new（= 新冒出来的）: 0 处
```
⇒ 修掉的**恰好这 4 项**、`Oath 1` 那两个假数字**没了**，**没有新副作用**。

同口径复核 R5 的一句：**「裸词（`value` 由兜底 1 来）= 3 处」—— 复现出来了**（`UM80`/`UM81`/`UM_Scout_Sniper` 的裸 `Oath`；`UM74` 那条本来就写 `Oath 3`，所以它的数字一直是对的）⇒ **改后 = 0 处**。

> ⚠️ 复刻里我数的是「会补出东西的 (卡,关键词) 对」= 391/387；R5 数的是「会补出**数字**的卡」= 52 —— **两个口径、不矛盾**，别当成对不上。

### 症状 ② 功能：`OathCost = 0`、誓约能力激活不出来

**静态验，已证**（用的是与 C# 逐字同一条正则）。新 `desc`：

| id | `^oath\s+(\d+)\s*:` 命中 | 取到的 `N` | 旧 `desc` 落哪条 |
|---|---|---|---|
| `UM80` | MATCH | **4** | `RePaid`，货币空 ⇒ `CostKindOf("")=""`（按能量算） |
| `UM81` | MATCH | **3** | 同上 |
| `UM_Scout_Sniper` | MATCH | **1** | 同上 |
| `UM74` | MATCH | **3** | 同上 |

⇒ `EffectText.CostKindOf(op.CostKind) == "oath"` 成立 ⇒ `CardDef.CollectOathOps`（`CardDef.cs:976-990`）不再 `continue` ⇒ `_oathOps` 非空、`OathCost` = **4/3/1/3**（与卡图逐张一致）。
⇒ `RuleCore.CanUseOathAbility`（`RuleCore.cs:6077`）的 `u.Card.OathOps.Count == 0 ⇒ ErrNoAbility` **不再触发**；`RuleCore.cs:4521` 的「可用动作表」也会重新列上这一项。
- ⚠️ `RuleEngine/Core/EffectResolver.cs:4728` 那句注释「`OathCost == 0` 的卡（**理论上没有**）」—— 我们查出的就是那句「理论上没有」，**补完前缀后该注释重新成立**（⛔ 但我没去改那句注释，`.cs` 不在白名单）。
- 🔴 **运行时那一半【没验】**：⛔ 不跑 Unity（红线下），所以「点一下真的能激活、真按 4/3/1/3 扣费」**没有实跑证据**。**判据 = 静态链 + 逐字正则 + 卡图**。建议调度台在收口那一趟带上 `RuleEngineTest.Run`（引擎动没动都该跑它那一条，因为这 4 张卡的行为路径变了）。

### 行尾 / diff

```
git diff --numstat -- Unity/数据/游戏数据/cardface_fixes.json
  →  7   2   Unity/数据/游戏数据/cardface_fixes.json
```
- `7 新增 / 2 修改` = 4 条 `desc` + 1 条说明 = 5 行新增，＋ 2 行**只加了一个逗号**（`"Death from Above"` 与 `"_2026-10-18_Oath裸写"` 不再是各自块的末元素）—— **正是预期形态**，不是 1655 行级别的「整篇重写」。
- ✅ **行尾没被翻**：改前 `bytes=121032 · CRLF=1654 · LF=1654`；改后 `bytes=123620 · CRLF=1659 · LF=1659`（纯 CRLF，`\r` 数与 `\n` 数相等）；`git diff --numstat` 的数字也印证。
- ⛔ **没用 `sed -i`**；改文件用 python 的 `io.open(...,'wb')` 二进制写（显式拼 `\r\n`）。`json.dumps(ensure_ascii=False)` 生成插入行，避免手写转义。
- `cards_engine.json`：`1 1`（该文件是**单行** compact JSON，无换行 ⇒ numstat 恒 1/1，**不能**用 numstat 判它的改动面，所以上面另做了逐字段对账）。`CRLF=0 · LF=0` —— 与改前一致。

### 没跑的

- ⛔ **没跑任何 Unity 自检**（红线）。**没跑秒级类型检查** —— 因为**本次一个 `.cs` 都没改**（改的是 json + 生成物），类型检查无从施加。
- ⛔ `gen_icon_plan.py` **没跑**（不在白名单）—— 见下面「顺手发现 ②」，它需要跑一次。

## 四、没查清 / 没做的部分

1. 🔴 **图标那一半【还欠】**：`card_icon_plan.json` 里这 4 张卡的**英文 `desc` 没有条目**（`UM80/UM81/UM_Scout_Sniper` 只有 `descZh` 的 `誓言 N：`；`UM74` 连 `descZh` 都没有）。
   改之前，那枚 `oath` 图标是 `KeywordSegment` 兜底补词时顺带画的（跟着错数字 `1` 一起出来）；**改完之后 `Oath` 落在正文里、判据 ① 跳过 ⇒ `CardIcons.Rewrite` 那条路也没有条目 ⇒ 英文卡面会少了那枚徽记**（卡面原版是 `⟨徽记⟩ Oath 4:`）。
   ⇒ **要做**（铁律 11）：跑一次 `工具/gen_icon_plan.py`，大概率还要在 `TOKEN_BY_CARD` 里补 4 条（`("UM80","Oath 4:")` 等）。
   ⛔ **我没做** —— `工具/gen_icon_plan.py` 与两份 `card_icon_plan.json` 都不在白名单里。**请调度台裁**。
   ℹ️ 现在英文 `desc` 里有了 `Oath 4:` 这种 token ⇒ `KEYWORD_SCAN`（`gen_icon_plan.py:579`，句首 `词+数字+冒号`）**下次一定扫得到**，但**能不能自动给出 `oath` 图**取决于锚点兜底（`anchor_answer`）—— 给不出就会进「🔴 没认出来」那一栏（**这正是该有的行为：不猜**）。
2. ⚠️ **`UM81` 的誓约正文跨了两句，第二句没进誓约能力**（详见「顺手发现 ④」）—— 需要引擎侧改法，**不在我白名单内**，没动。
3. ℹ️ **日期口径不一致**：git HEAD 的提交日期 = **2026-10-10**，但 `项目任务.md` 里已用到 **2026-10-19**、`cardface_fixes.json` 里已有 `_2026-10-18_*` / `_2026-10-20_*` 键、`R5` 这份文档自己写着 **2026-10-11**。我给说明条目起的键名 = `_2026-10-11_A1331_Oath前缀`（**跟 `A1331` 台账行/`R5` 的会话戳对齐**）。若本项目的惯例是别的日期，改键名即可（内容不受影响）。

## 五、顺手发现（⛔ 只报不改，由调度台分流）

1. 🔴🔴 **同一形状、另一枚记号 —— 修女会 4 张卡的英文 `desc` 也缺段首记号（缺的是 `☀` 信仰，不是 `Oath`）**：
   `SOR72 Adelaide the Serene`（`6: …`）· `SOR11 Crusader`（`3: …`）· `SOR23 Retributor`（`1 : …`）· `SOR24 Seraphim`（`3: …`）。
   （发现法 = 全池扫 `desc` 匹配 `^\s*\d+\s*:` —— **全池 8 处，4 处是本次的 `Oath`、另 4 处就是这 4 张修女会**。）
   - **卡图亲读 2 张**（铁律 7）：`Sorotitas/3部队/Warpforge_11_Crusader.png` 卡面 `Vanguard. 3 ☀ : Gain +1 ⟨粉拳⟩ and ⟨盾⟩ Armour 2`；`…/Warpforge_23_Retributor.png` 卡面 `1 ☀ : Deal 3 damage to a random enemy` ⇒ **那个记号是金太阳 = 信仰**（`EffectText.cs` 的 `CostKindOf` 注释明写「修女会 = 金太阳（信仰）」）。另 2 张的 `descZh` 段首就是 `N ☀：`（`SOR72` / `SOR24`），旁证同一件事。
   - ⚠️ **不是「`Oath` 丢了」**：这 4 张的 `keywords` 里**没有 `oath`**，缺的就是 `☀`/`[faith]` 这个**货币记号**。所以**与 `A1331` 同形、不同源**。
   - 🔴 **引擎后果（我判的，静态）**：`CostKindOf("") = ""` = **按能量算** ⇒ 这 4 张的付费激活会**扣能量而不是扣信仰**（卡面要的是信仰）。⚠️ **未跑验证**。
   - ⛔ **我没改这 4 张**（超出 `A1331` 白名单，任务 ⑨ 明说「别自己扩大改动面」）。**要不要立账、怎么修，请调度台裁。**
2. ⚠️ **`KeywordSegment` 的判据 ① 会多压掉一个该印的关键词** —— `UM74` 卡面印 `Long Range. Sentry 2. Oath 3: Gain Sentry 2`，而我们 **`Sentry 2.` 印不出来**：`Core/CardText.cs:213` 拿 `hay`（= 整条 `desc`）去比词 `Sentry`，而正文里 `Gain Sentry 2` 正好含这个词 ⇒ 判「正文提过了」⇒ 跳过。⚠️ 那处 `Sentry` 是效果的**宾语**、不是声明。
   ⚠️ **两处判断打架，我这边不裁**：`R5` 的表说我们印 `Long Range. Sentry 2. Oath 3. 3: …`（含 `Sentry 2.`），**与静态读代码的结果相反**；而且 R5 那一格的词序是**卡面序**、不是代码里的 canonical 排序（`CardText.cs:241`）⇒ 我怀疑那一格是照卡面写的、不是跑出来的。**要真渲一张才定得了**（我没跑 Unity）。
3. ℹ️ **`gen_cards_engine.py` 在本机控制台直接跑会崩**：不设 `PYTHONIOENCODING=utf-8` 时，`print("… ✅")` 在 GBK 控制台抛 `UnicodeEncodeError`，**崩在 `--check` 结论之前**（实测）。⇒ 这个「正规生成通道」的常规跑法建议写进 `资料/命令速查.md`（我没改文档）。
4. ⚠️ **`UM81` 的誓约能力【跨两句】，第二句不在 `_oathOps` 里**：
   `desc` = `Oath 3: Deal 2-4 damage to an enemy. If target dies, gain Shield` ⇒ `EffectText.Parse` 切成两段，`paidCost` 只在**同一段内**贴到 op 上（`EffectText.cs:1859-1860` / `:1891`）⇒ 第二段的 op `Cost = 0` ⇒ `CardDef.CollectOathOps` 只收第一段 ⇒ **激活后只造成 2-4 点伤害、不会给 `Shield`**。
   ⚠️ **全池只有这 1 张是「誓约正文跨句」的形状**（我扫的判据 = `oath\s+\d+\s*:` 之后的尾串按句切 >1 句；命中的另一张 `UM_Phobos_Lieutenant` 的第二句是 `Slay:`，那是**另一个独立触发**，不算）。
   ⚠️ **是既有缺口、不是我引入的**：改之前 `_oathOps` 整个是空的（能力完全打不出来），所以「少了 Shield」这件事**今天才可观测**。⛔ 没动（`EffectText.cs`/`CardDef.cs` 不在白名单）。⚠️ **未跑验证**。
5. ℹ️ 顺手核到 `EffectResolver.cs:4728` 那句注释（「`OathCost == 0` 的卡**理论上没有**」）**在我不用碰的文件里**，所以上面说明了「没改」。若调度台要收口，这一句值得跟着改。
