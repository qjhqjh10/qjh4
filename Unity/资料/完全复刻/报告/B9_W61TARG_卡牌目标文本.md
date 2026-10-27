# B9_W61TARG —— `RawCardScript.GetLocalizedTargetted()`（`Card_Targeted/*` 目标提示文案）

> 账行 = `资料/完全复刻/账/战斗逻辑驱动.tsv:1241`（改前 **B**、`我们的落点` = `—`）。
> 本件产出 = ① `Core/Loc.cs` 的取键口（+118 行、类尾）② 灭自证自检 ③ 两条**改动请求**（落点在别人文件）④ 账补丁。
> ⛔ 未碰 `Battle/BattleDriver.cs`（W57）· `RuleEngine/**` · `Resources/official_loc_13lang.json` · 任何 `*.unity`。

---

## 一、结论

### 1. 原版这条是「**必有**」，不是可选 —— 但只在**单位卡**那一条路上

- **唯一活调用点**（三源合判：`out_cpp2il_dev_ilspy` 全树 / `decomp_full` 全树 / `assets_full` 无字符串引用）：
  `BattleManager._ChooseUnitTarget_d__780__MoveNext`。方法体里它被当成 **tooltip 的正文**用：
  `EverguildTooltipManager.Show(battleTooltip, 标题 = ""（空串）, 正文 = 本方法, 卡位世界坐标)`，
  紧接着 `ShowPotentialTargets(actingCard, 1)`（点亮候选），点完 / 取消就 `StopTracking` + `Hide`。
- **进入闸**（`BattleManager__NeedToChooseMinionTarget.c:12-18`）：
  `isPlayer ∧ cardType == 0（单位卡） ∧ targeted ∧ matchType != 0xA0（教程/回放）`。
- ⛔ **战术 / Whispers 卡那条选目标支不显示这句**：`BattleManager__Update.c` 全篇
  `grep "EverguildTooltipManager__Show"` = **0 命中**（那一支只有准星 + 溶卡 + 取消回手）。
  ⇒ 本次接线**必须只对单位卡**，别顺手给 196 张 `targeted` 非单位卡也弹一条原版没有的提示。
- ⇒ 结论：**该复刻**，且它是「请点目标」的浮动提示，**不是卡面、不是技能面板**
  （`Battle/SkillPanel.cs` 是 `ActiveSkillDesc` 那条，与本条无关 ⇒ 本件**没改它**，理由见 §四）。

### 2. 数据侧「有」—— 上一路判「没有」是**查错了文件**

| 事实 | 现读 |
|---|---|
| `cards_engine.json` 有这一列吗 | **没有**（上一路 `B9_W52LAND` 查的是它 ⇒ 结论「我们卡表没有这一列」在**那个文件**上是对的） |
| 真值在哪 | **`资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `full_raw.targetingTextReferenceId`**（1,351 张卡 SO） |
| 非空张数 | **51 张 / 18 种取值**（其余 1,300 张是空串） |
| 真正会弹的 | 「单位卡(`cardType==0`) ∧ `targeted==1`」= **50 张 / 17 种取值** |
| 官方键 | `Card_Targeted/*` **31 条** ⇒ **17 条可达 · 14 条在原版数据里没有任何卡引用** |
| `uniqueId` 回退支 | 「单位卡 ∧ targeted ∧ ref 空」= **0 张** ⇒ 本 build **0 次命中**（但原版有这一支，照抄） |

### 3. 本件落地了什么 / 还差什么

- ✅ 落地：`Core/Loc.cs` 类尾新增 4 个成员 —— `TargetedTermPrefix` / `TargetedKey` / `TargetedText` /
  `HasTargetedText` / `CheckTargetedText`（key 拼法两支**逐字照抄**；取值走 `Loc.T` 三档兜底；
  记号渲染按仓内口径交给 `CardIcons.RewriteMarks`，⛔ 本口不重复实现）。
- ⏳ 还差两处，**都在别人的文件里**（见 §四「改动请求」）：
  ① 卡表出 `targetingTextReferenceId` 列（`工具/gen_cards_engine.py` + `RuleEngine/Core/CardDef.cs`）；
  ② `BattleDriver.TryBeginPlayTargetPick` 进/出「等点目标」态时 `Tooltip.Show/Hide`（W57 正占该文件）。

---

## 二、证据（文件:行号）

### 2·1 原版方法体（逐句）

`cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:2448-2458`（**这份有真方法体**；`decomp_full` 那份
`RawCardScript__GetLocalizedTargetted.c:1-31` 是同一份体，但**返回值被反编译丢了**，只剩 `void`）：

```csharp
public string GetLocalizedTargetted()
{
    string text = (string.IsNullOrEmpty(targetingTextReferenceId) ? uniqueId : targetingTextReferenceId);
    string term = "Card_Targeted/" + text;
    string translation = LocalizationManager.GetTranslation(term, FixForRTL: true, 0,
                            ignoreRTLnumbers: true, applyParameters, localParametersRoot,
                            overrideLanguage, allowLocalizedParameters);   // applyParameters = default = false
    return SupportMethods.ModifyLocalization(translation);
}
```

- 字段 `targetingTextReferenceId` = `RawCardScript` 的 **public string**
  （签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:183`；`.c` 里是 `param_1 + 0x278`，与 `+0x38`=uniqueId 并列）。
- 与兄弟方法的关系（**同一处拼接的另一段，不是同一条链**）：
  `GetLocalizedCardDesc`（`Card_Description/`，`:2317`）· `GetLocalizedAbilityDesc`（`Card_Abilities/`，`:2374`）·
  `GetLocalizedFlavourText`（`Warlord_Lore/`，`:2351`）· `GetLocalizedAbilityName`（`Card_Abilities/Ability_Name_`，`:2362`）。
  形状完全同构（`ref 非空 ? ref : uniqueId`），**只有键前缀与消费点不同**。

### 2·2 唯一调用点（tooltip 正文）

`decomp_full/BattleManager._ChooseUnitTarget_d__780__MoveNext.c`：

| 行 | 做了什么 |
|---|---|
| `:34-37` | `transform.position` → `StartTrackingFrom(卡位, 3)` |
| `:39` | `ClockManager.UnpauseForAnim()` |
| `:43` | **`uVar6 = RawCardScript__GetLocalizedTargetted(lVar4)`** |
| `:44-55` | `Get2DPosOfCard(卡)` + 加一个小偏移（`DAT_1834b3400`） |
| `:60-61` | `EverguildTooltipManager__Show(inst, battleManager+0x158 /*battleTooltip*/, **空串（标题）**, uVar6, &pos, 0, 0)` |
| `:62` | `ShowPotentialTargets(card, 1)` |
| `:71-75` | 等 `UIstate == 9`（`+0x290`） |
| `:84-91` | `StopTracking` + `EverguildTooltipManager__Hide` |

`out_cpp2il_dev_ilspy/RawCardScript.cs:2448` 之外，全树另一处命中 = `BattleManager.cs:1784`（同一段，ILSpy 版）。

### 2·3 进入闸（为什么只有单位卡）

`decomp_full/BattleManager__NeedToChooseMinionTarget.c:12-18`：
`if (*(char *)(card+0x40) /*isPlayer*/) { if (get_cardType(card) == 0) { if (get_targeted(card)) return matchType != 0xa0; } } return false;`

### 2·4 数据（现读）

```
$ python -c "…cards_raw.json 的 full_raw.targetingTextReferenceId…"
总卡 1351 · 非空 51 · 不同值 18
targeted=0 ref空 cardType=0   554      targeted=1 ref空 cardType=20  207
targeted=0 ref空 cardType=10   66      targeted=1 ref有 cardType=0    50
targeted=0 ref空 cardType=20  473      targeted=0 ref有 cardType=0     1  ← SAU24 Command Barge
```

**17 条可达键 × 卡数**（`Card_Targeted/` 前缀省略）：

| ref | 卡数 | ref | 卡数 | ref | 卡数 |
|---|---:|---|---:|---|---:|
| `chooseTarget` | 16 | `deal1Dmg` | 5 | `deal2Dmg` | 4 |
| `deal3Dmg` | 4 | `stunEnemy` | 5 | `heal2` | 3 |
| `giveMarkerlight2` | 2 | `giveMarkerlight3` | 2 | `blindEnemy` | 1 |
| `deal4Dmg` | 1 | `deal5Dmg` | 1 | `deal6Dmg` | 1 |
| `deal8Dmg` | 1 | `giveFlank` | 1 | `giveVulnerable` | 1 |
| `reanimate` | 1 | `returnToHand` | 1 | | |

**14 条孤儿键**（原版数据里有词条、**没有任何卡引用** ⇒ 不是我们的缺口）：
`buffHealth2` `buffStats11` `cantAttackUntilNextTurn` `damageHealOrBuff` `damageOrBuff` `damageOrHeal`
`destroyFriendlyInfantry` `destroyTroop` `giveCantAttack` `giveMarkerlight1` `giveMarkerlight5`
`nerfAttack2` `stunEnemyTroop` `stunEnemyVehicle`。

**现读 17 条真值（zh 列）** —— 其中 **14 条是 `((…))` 参数化记号**，必须再过一层：

| 键 | zh 原样 | 渲染后（`CardIcons.RewriteMarks` 之后） |
|---|---|---|
| `chooseTarget` | `选择一个目标` | 同左（无记号） |
| `returnToHand` | `返回手牌` | 同左 |
| `reanimate` | `复活一支友方 [[remnant]]` | 复活一支友方 ⟪残余⟫图标 |
| `deal2Dmg` | `((Deal_x_damage,2))` | `Modular_Description/Deal_x_damage` = `造成 {0} 点伤害` → **造成 2 点伤害** |
| `deal3Dmg`…`deal8Dmg`、`deal1Dmg` | `((Deal_x_damage,N))` / `((Deal_1_damage))` | 同理（`Deal_1_damage` 是不带参数的独立词条） |
| `stunEnemy` | `((Stun_an_enemy))` | `使一个敌方目标 [[stun]]` → 词 + 图标 |
| `blindEnemy` | `((Blind_an_enemy))` | `使一个敌方目标 [[blind]]` |
| `heal2` | `((Heal_x_to_another,2))` | `恢复 {0} 点生命值` → **恢复 2 点生命值** |
| `giveFlank` / `giveMarkerlight2` / `giveMarkerlight3` / `giveVulnerable` | `((Give_trait,[[flank]]))` 等 | `送出 {0}` → 送出 ⟪侧翼⟫/⟪标记灯⟫ n |

> ✅ **端到端链已核**：`Loc.T` 取到原样串 → `CardIcons.RewriteMarkModular`（前缀 `Modular_Description/`，
> `Core/CardIcons.cs:779-800`）→ 再被 `RewriteMarkTraits` 收掉 `[[…]]`（顺序 `① (( )) → ② $$ $$ → ③ [[ ]] → ④ (N)`，
> `Core/CardIcons.cs:647-657`，那里的 doc 明写「上一支的产物由下一支继续处理，是原版的设计」）。
> 五个 `Modular_Description/*` 词条与四个 `Card_Trait/*` 词条在 `official_loc_13lang.json` 里**现读全部命中**。

### 2·5 我们改前是什么样

- `grep -rn "Card_Targeted" MyGame/Assets --include=*.cs` = **0 命中**（改前）—— 只有数据侧 31 条键、没有消费者。
- `Battle/BattleDriver.cs:7959-7961` 现读：「**tooltip 省略**：原版的文案来自 `RawCardScript.targetedText`，
  **我们卡表没有这一列** ⇒ 不编一句自造文案」。
  🔴 **这句有两处不成立**（下次谁碰那个文件请顺手改）：① 字段名不是 `targetedText`（全库不存在），
  是 **`targetingTextReferenceId`**；② **数据有**（在 `cards_raw.json` 的 `full_raw` 里，只是没进 `cards_engine.json`）。

---

## 三、没查清的部分

1. **`applyParameters = false` 的语义**：原版 `GetTranslation(..., applyParameters: false, ...)`。
   我按「I2 不在这一层展开 `((…))`，靠下一步 `ModifyLocalization` 展开」来处理 ——
   `ModifyLocalization.c:75-173` 的**结构**确实是「`Contains("((")` ∧ `Contains("))") → 正则匹配 → `Split(',')`
   → `Concat(前缀, parts[0])` → `GetTermTranslation` → 用 `parts[1]`/`parts[2]` 替换两个占位符」，
   与仓内既有判据（`CardIcons.RewriteMarkModular` 的 doc：前缀 = `Modular_Description/`、占位符 = `{0}`/`{1}`）一致；
   **但那几个字面量常量本身没读到**（`DAT_1842xxxxx` 是 IL2CPP metadata usage 槽，静态读是 0 ⇒ `工具/read_literal.py`
   也不适用，它只读数值常量）。**还差**：把 `0x184237f88`（前缀）/`0x184265d10`（`{0}`）/`0x184272af0`（`{1}`）
   在**运行中** dump 一次（SceneJumpShot mod 可加探针）。
   ⚠️ 影响面：**只有「前缀到底是 `Modular_Description/` 还是别的」这一条**；而它已被我们现有实现（用了同一份 `.c` 作判据、
   且今天在卡面正文上跑得好）交叉证实。
2. **tooltip 的锚点/偏移**：原版 `Show(..., &pos, 0, 0)` 的 pos = `Get2DPosOfCard(卡)` 加一个小偏移
   （`local_10 = local_20 + *(float *)(tooltipGo+0x20) * DAT_1834b3400`）。那个偏移量没解（`DAT_1834b3400` 是 float 常量、
   可读但没读）。**不影响「有没有」**，只影响「弹在卡的正中还是偏上一点」。
3. **`EverguildTooltipManager.Show` 第 6/7 个实参（`0, 0`）** 是什么（大概率是 anim / 时长类），没查。
4. **`targetingText` 字段**（与 `targetingTextReferenceId` 并列的那个 string）：1,351 张**全空串** ⇒ 原版没有卡用它。
   ⛔ 别把它当成「另一个来源」。

---

## 四、改动清单

### 4·1 已改（本件白名单内）

| 文件 | 改了什么 |
|---|---|
| `MyGame/Assets/CardPresentation/Core/Loc.cs` | **类尾追加 118 行**（改前 3,736 行 → 现读 3,854 行；**零行数变化**规则不破：追加点在**类尾之后没有落点**，`Loc.cs` 既有账行 `:3421`/`:3084`/`:3346` 全部不动）。新增 `TargetedTermPrefix`(:3770) · `TargetedKey`(:3776) · `TargetedText`(:3788) · `HasTargetedText`(:3797) · `CheckTargetedText`(:3814)。 |
| `账/_parts/B9_W61TARG.tsv` | 新建。1 行（`战斗逻辑驱动.tsv:1241`），状态 `B`（原值，理由见 §4·3）、`我们的落点` 由 `—` 改为真落点。 |
| 本报告 | 新建。 |

⛔ **`Battle/SkillPanel.cs` 一个字没改** —— 原版这条文案的宿主是战斗 tooltip，**不是 `ActiveSkillDesc`**（§一·1）。
给 SkillPanel 加它 = 造一个原版没有的界面元素。

### 4·2 改动请求 ①（数据侧 · 卡表出列）

**要什么**：让 `CardDef` 带上 `targetingTextReferenceId`。

- 生成器 `工具/gen_cards_engine.py`：加一列 `targetingTextReferenceId`，源 =
  `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `full_raw.targetingTextReferenceId`（1,351 张、51 张非空）。
  ⚠️ **只从 `full_raw` 取、⛔ 别递归扫整卡**（`cards_raw.json` 每卡两份同构技能树，`CLAUDE.md` 已记「统计翻倍」那条坑；
  本列在 `full_raw` 顶层、不在技能树里，所以不受影响，但同一份文件要读对）。
- `RuleEngine/Core/CardDef.cs`：属性 `public string TargetingTextReferenceId { get; private set; } = "";`
  + 构造形参**追加在参数表末尾**（`string targetingTextReferenceId = ""`），照 `specialAttackScoring` 那条先例
  （`CardDef.cs:1006-1007`）。
- ⚠️ **重生成 `cards_engine.json` 会动 `play check` 指纹** ⇒ 必须由**调度台在没有并发账写手时**做（本件不做）。

### 4·3 改动请求 ②（表现侧 · 弹 / 收 tooltip）

落点：`CardPresentation/Battle/BattleDriver.cs`（**W57 正占 ⇒ 本件不碰**）。

- **进**：`TryBeginPlayTargetPick` 里设完 `_awaitPlayTargetCard / _awaitPlayTargetDef / …` 之后
  （现读 `:8105-8109`，**且只在 `unitCard` 那一支**），
  ```csharp
  Tooltip.Show(CardIcons.RewriteMarks(Loc.TargetedText(def.Id, def.TargetingTextReferenceId), CardText.Zh),
               card.transform.position);          // 原版 anchor = 0（居中）
  ```
  （`CardText.Zh` = `TmpFont.Available && Loc.Current == Chinese`，`Core/CardText.cs:38-43` —— 与卡面正文
  「中文档但字体缺失 ⇒ 回英文列」**同一条闸**，⛔ 别在这里另写一个 `Loc.Current == AvailableLanguages.Chinese`。）
- **出**（三处真出口，`_awaitPlayTargetCard = null` 且之前是「在等」）：
  `:8231`（回合易主 / 对局结束）· `:8260`（点定目标、照常开打）· `:8280`（取消）⇒ 各加一行 `Tooltip.Hide();`。
  ⚠️ `:8122` 是「合法目标只有它自己 ⇒ **根本不进等待态**」，那里**不弹**也**不用收**。
- ⚠️ **只对单位卡**（`unitCard == true`）—— 原版战术卡那一支全篇没有 tooltip（§一·1）。
- ⚠️ `Tooltip.Show` 已经自带「文案没变就不重排版」+「`TooltipsSuppressed` 期间不弹」（`Core/Tooltip.cs:307-310`），
  ⛔ 别在调用点再判一遍。
- 顺手：把 `BattleDriver.cs:7959-7961` 那段注释改准（字段名 + 「数据其实有，只是没进 `cards_engine.json`」），
  ⛔ 不是删掉它。

### 4·4 断言（灭自证）

**位置**：`Editor/BattleScene.cs` 的 `BattleScene.Run`（Loc 断言族，现读 `:7247` 一带；本件白名单外 ⇒ 只给片段）。

```csharp
// 🔴 2026-10-27（B9_W61TARG）：`Card_Targeted/*` 链的灭自证检查（返回空列表 = 全过）
var tgtBad = Loc.CheckTargetedText();
int tgtKeys = Loc.Lang13Keys(Loc.TargetedTermPrefix).Length;
Check(tgtKeys > 0, "（前提）官方 13 语言表里 `Card_Targeted/*` 至少有一条键");
Check(tgtBad.Count == 0,
      "`Card_Targeted/*`（" + tgtKeys + " 条键）逐条由 `Loc.TargetedText` 取到词条、且与 `Loc.T(键)` 逐字相同"
    + (tgtBad.Count == 0 ? "" : " ｜ 失败 " + tgtBad.Count + " 条：" + string.Join(" / ", tgtBad.ToArray())));
```

**为什么这几条是「灭自证」而不是自证**（`CheckTargetedText` 里逐条写明）：

| 条目 | 内容 | 谁改坏了它会红 |
|---|---|---|
| ① | `TargetedKey("ZZTest","")` = `Card_Targeted/ZZTest`；`TargetedKey("ZZTest","deal3Dmg")` = `Card_Targeted/deal3Dmg` | 删掉「ref 压过 uniqueId」那一支（原版 `:2450` 三元）|
| ② | 官方表**每一条** `Card_Targeted/*`：`TargetedText("__no_such_card_id__", ref)` **逐字等于** `T(键)`、非空、**不是键名** | 桩（回 id / 回常量 / 不查表）；31 条 31 个不同值 ⇒ 常量桩必红 |
| ③ | 反向对照：**本地不存在的 ref 必须取不到**（`HasTargetedText` = false），真实 ref 必须取得到 | 把 `HasTargetedText` 写成恒 `true` / 恒 `false` |

**改前必红（实测，不是推理）** —— 用同一份数据模拟三版实现跑 ②：

| 实现 | ② 失败条数 |
|---|---|
| **本件这版**（`Card_Targeted/` + (ref 非空 ? ref : uniqueId)） | **0 / 31** |
| 删掉 ref 那支（只用 `uniqueId`） | **31 / 31** |
| 桩（直接返回 ref） | **31 / 31** |

复算脚本口径 = `official_loc_13lang.json` 的 `zh` 列（缺则 `en`、再缺回键名 —— 与 `Loc.T` 的档位一致）。

⚠️ `CheckTargetedText` **不会**让 `Loc.MissingCount` +1（只走已存在的键）⇒ 可以安全地放进
`BattleScene.cs:7233-7247` 那种「`MissingCount` 前后比对」的窗口里。

---

## 五、顺手发现

1. 🔴 **上一路 `B9_W52LAND` 的「我们卡表没有这一列」是「查错了文件」**（不是查得不够深）：
   它查 `cards_engine.json`（解析视图），而真值在 `cards_raw.json` 的 **`full_raw`** 里 —— 同一份 dump 的另一个字段。
   这与 `CLAUDE.md` §三「翻过一个镜像目录就写『本地没有』= 一定会错」同族。
   ⇒ **建议的口径**：报「某列没有」时，至少要把 `cards_engine.json` / `cards_raw.json(full_raw)` / `cards_truth.json` **三个都点一遍**
   （三者分工已写在 `CLAUDE.md`：`cards_truth` = 效果树、`cards_engine` = trigger/trait 面、`cards_raw.full_raw` = **原版 SO 全字段**）。
2. 🟡 **`SAU24 Command Barge` 有 `ref=deal3Dmg` 但 `targeted=0`** ⇒ 它永远不会弹这条 tooltip。
   将来若有人按「有 ref 就该弹」写闸，会把这张卡多弹一条 ⇒ **闸要判 `targeted`，⛔ 别判 ref 非空**。
3. 🟡 **14 条孤儿键**（§2·4）—— 原版数据里有词条但没卡引用。若将来核对「31 条是不是该更多/更少」，
   这 14 条是**原版自己**的余额，⛔ 别当成我们的缺口去补卡。
4. 🟡 **`BattleDriver.cs:7959-7961` 的注释有两处事实错误**（字段名错 + 「数据没有」其实有）。
   按铁律 5 应当场改，但那个文件 W57 正占 ⇒ 已写进 §4·3 最后一条。
5. 🟢 **`Loc` 里已有 `Lang13Keys(prefix)`（`:3114`）这个「按前缀枚举官方键」的口**，
   本件的自检直接复用它 ⇒ 断言里**没有硬编码任何键表**（键表变了自检跟着变，不会烂）。
   建议以后同类「某族键是否都取得到」的断言都走这个口，别再抄一份键清单进断言。

---

## 交接给下一个代理（本件已收尾）

- 代码、断言、账补丁、报告**都已落盘**；`TMPDIR=/tmp/wf_w61 bash 工具/typecheck.sh` = **0 / 0**。
- ⏭ 要把它真正「弹出来」只需做 §4·2 + §4·3 两条改动请求（都是别人的文件），然后把 §4·4 那三行接进 `BattleScene.Run`。
- ⛔ **别改**：`CardPresentation/Battle/BattleDriver.cs`（W57）· `RuleEngine/Core/DeckRules.cs` / `Deck/DeckEditorState.cs`（W56）·
  `RuleEngine/Core/{BattleEvent,EffectResolver,RuleCore}.cs`（W54）· `Deck/DeckRuntime.cs`（W51）。
- ⛔ **别重生成** `cards_engine.json`（会动 `play check` 指纹、且有并发账写手）。
