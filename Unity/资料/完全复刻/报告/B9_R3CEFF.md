# B9_R3CEFF · `GetCardEffectsToShow` 项源统一 · 施工细则（报告）

> 判据型子代理 `R3CEFF`（只读）· 2026-10-27。产出 = `资料/完全复刻/判据/GetCardEffectsToShow_施工细则.md`（§一~§六）。
> 本报告只列要点、证据与没查清的部分；**施工细节全部在那份细则里**。

## 一 结论

1. 原版这条链是**四个方法**，过滤器有**两道** —— 旧判据只记了第一道：`CardDisplayWindow.DisplayCardEffects` 在 `GetCardEffectsToShow` 之后**再删 4 类**（`buffType ∈ {6 changeRace, 30 addRace, 35 daemonHost}`，以及「卡上没有任何任务附魔时的 `16 choiceEnchantment`」）。
2. `+0x108` = `EntityScript.activeEffects : List<CardEffect>`（`dump.cs:22025`）。序 = `List.Add` 序，**唯一聚合 = mark-of-chaos 按四神各一条**（`traitValue` 累加）。**印刷关键词不在里面**（在 `+0x128`）⇒ 投影必须减掉卡面基线。
3. 还有一个**整组闸**：被展示的卡若是「环境效果卡」（`spellType ∈ {210, 200}`，`RawCardScript__IsEnviromentaleffect.c`），**即使有条也整组隐藏**。池内 `spellType 210` **39 张**（全是 `type = defence`）⇒ 可达。
4. 我方**没有**统一记录列表：跨 **7 个源**合成（`_statMods` / `_traitEffects`（**今天恒空**）/ `_keywords`（无记录）/ `TempBuffs` / `ActiveEffects` / `HandEffects` / `_removedTraits`）。**`UnitState.ApplyTraitEffect` 已建好但零调用者** ⇒ 不切过去，A1/A4 做不了。
5. **今天就在错的三条**：① 关键词行缺 `Get {0}` / `Get {0} one turn` 外壳（我们印裸关键词名）；② 属性行恒用 `…OneTurn` 键（永久增益被印成"一回合"）；③ mark 族完全不出。
6. 序要真还原必须**加序戳 `Seq`**（`Auras.Recompose` 是"整份摘掉→重新加回"，不加戳光环行每次跳到队尾）；洞（空项占槽）的位置**依赖序** ⇒ 序戳是"空槽"的前置。

## 二 证据（文件:行号）

- `d:/2/tools/decomp_full/EntityScript__GetCardEffectsToShow.c:32-191`（外闸 `:40`/`:175-186`、mark 聚合 `:47-130`、排除档 `:135-141`/`:146-164`、加入 `:166-171`）
- `d:/2/tools/decomp_full/EntityScript__CreateMarkCardEffect.c:1-21`（`buffType=8` + `markOfChaosType=god`；`+0x88` 由调用点 copy）
- 🔑 **ILSpy 版可读**（比 Ghidra 更省事）：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/EntityScript.cs:2149-2497`（同一算法，变量名可读）、`CardEffectItem.cs:13-157`（**左列/右列/patch 文本逐字**）、`GameStaticData.cs:1639-1830`（**`Battle/Effect/*` 键名与拼接格式逐字**）、`CardEffectsService.cs:18-60`
- `d:/2/tools/decomp_full/CardDisplayWindow__DisplayCardEffects.c:32-105`（**第二道过滤 + 环境卡闸 + 返回值**）、`RawCardScript__IsEnviromentaleffect.c:1-12`
- `d:/2/tools/decomp_full/CardEffectsService__EnableEffects.c:17-52`（5 槽 / 空项占槽 / 槽满截断）
- `d:/2/tools/decomp_full/GameStaticData__GetEffectDesc.c`（492 行：`effectText` 优先 `:66-84`、`spellType==120` 走附魔卡描述 `:92-100`、case 表 `:130-446`、`default` 只认 45/70/85/95 `:447-492`）
- `d:/2/tools/il2cpp_out/dump.cs:22025`（`<activeEffects> // 0x108`）· `:119060-119100`（`CardEffect` 全字段）· `BuffType` 枚举 · `DefinedTrait`（`oathInAllTurns=1276` / `oathDouble=1277` / `oathTripleActivation=1278` / `notDestroyRemnant=1190` / `markOfKhorne..Tzeentch=1060..1090`）· `MarkOfChaosType`（none=0/Khorne=10/Nurgle=20/Slaanesh=30/Tzeentch=40/Random=50）
- 我方：`CardPresentation/Battle/CardDisplayWindow.cs:835-939`（`RowsOf` / `AttrTerm` 恒 OneTurn / `SetEffectRows` 的「空的根本不铸」）· `BattleDriver.cs:13080`（**唯一调用点**）· `RuleEngine/Core/UnitState.cs:1336-1364`（`GrantRecord`）`:1519-1539`（`_auraKw`）`:1637-1675`（`TempBuff`）`:1870-1934`（`StatMod` + `_statMods`，**插入序**）`:2904-3081`（`TraitEffectRecord` + `ApplyTraitEffect` + `FindTraitEffect`，**零调用者**）`:856-875`（`_removedTraits`）· `CardInstance.cs:124/139`（`HandEffects`/`ActiveEffects`）· `Enchantment.cs:71-120`（`ActiveCardEffect`）· `EffectResolver.cs:11877`（裸 `AddKeyword`）`12369-12403`（`ApplyMarkBody`）`12436/12559`（`_keywordsPact`/`PactOf`，**只记最后一种**）· `CardDef.cs:136/1016/6026`（`SpellType` / `Keywords`）
- 数据：`Assets/CardPresentation/Resources/official_loc_13lang.json`（10,862 键；`Battle/Effect/*` 22 条 + `Card_Trait/markOf*` 4 条 + `CardTraitDescription/markOf*` **全部命中**；`Card_Trait/{secretOrder,classified,quest,questClassified}` **4/4 缺**）· `cards_engine.json`（`count 1128`；`spellType` 分布含 **210×39**、`60/120` = 0）

## 三 没查清的部分

1. **`ChangeMeleeAndHealth` / `ChangeRangedAndHealth` 的「一回合」变体在官方表里不存在** —— 限时 + 该组合时原版返回什么**没核到**（`GetEffectDesc` case 1 那 175 行的 8 条组合分支只核到「按键名可辨」，没逐句到行号）。
2. **`addTrait` 行 `traitValue` 的拼接**：ILSpy 显示条件 `traitValue >= 1`、格式 `关键词名 + " " + N`（与 mark 行的 `" (xN)"` **两套格式**）；`Vanguard 1` 这种读法**没有实拍核对过**。
3. **`RowGroup`（同一次 `give` 多条合一）的粒度**：原版合并键无属性维度，但「哪些 `PayloadOp` 属同一条 `CardEffect`」需要 `GivePayload` 项 ↔ 原版 `buffEffect` 的**逐卡对账**，没做。
4. **`setHealth(9)` / `setCurrentHealth(100)` 不留痕**（`DoSetHealth` 直写 `Health`）⇒ 这两行今天出不了；要补记录。
5. **`RemovedTraits` 是 `HashSet`**（无序、无值）⇒ `removeTrait` 行只能排尾、不印值。
6. **`changeCost(2)` / `setCost(95)` / `swapStats(5)`**：前两个结构上只挂手牌（展示窗只看场上），`swapStats(5)` 池内是否可达**没查**。
7. **切 `ApplyTraitEffect` 对结算指纹的影响没实跑**（ruleprobe 属主对话/写手，本代理不跑）—— 净行为"应当"等价，但**不许在没跑 `play all dump` 前声称零差异**。
8. **池内投影条数上限没算**（是否真会 >5 条、截断支是否可达）。
9. **`secretBuff/questBuff` 的 4 条键**在 2025-07-30 老快照里有没有，**没查**。

## 四 施工清单（按文件切片，摘要；细则见 §三 S0~S7）

| 切片 | 文件 | 干什么 |
|---|---|---|
| **S0** | —— | 前置：`ApplyTraitEffect` 接线 + mark 按神计数（不切 = A1/A4 全白做） |
| S1 | 🆕 `RuleEngine/Core/EffectsToShow.cs` | `ShownEffect` + `EffectsToShow.Of(u, isPlayer, ctx)`：全族投影、按 `Seq` 排、按原版两道过滤 |
| S2 | `RuleEngine/Core/UnitState.cs`（**类尾追加**） | `NextEffectSeq()` + 三条记录的 `Seq` + 光环重算**复用旧 Seq**（`_auraSeq` 快照） |
| S3 | `EffectResolver.cs` / `Enchantment.cs` / `Aura.cs` / `RuleCore.cs`（**互不相交的子切片 a~f**） | a `ApplyOneGain` 切 `ApplyTraitEffect`；b `ApplyMarkBody` 传神；c `DoGive` 打 `RowGroup`；d 附魔 `Seq` + `SpellType` 填真值；e 光环传 source；f `HandEffect.Seq` |
| S4 | `CardPresentation/Battle/CardDisplayWindow.cs` | 新 `RowsOfEffects`（左/右列按 §1.6）+ `SetEffectRows` 改**槽位模型**（洞 / 非空计数决定整组 / 环境卡闸 / 返回 bool）；**保留**旧 `RowsOf(TempBuff)` 给 `Editor/BattleScene.cs:9719` |
| S5 | `CardPresentation/Battle/BattleDriver.cs:13080` | 1 行换项源 + 传 `isPlayer` |
| S6 | `RuleEngine/Editor/RuleEngineTest.cs` · `CardPresentation/Editor/BattleScene.cs` | 12 条断言（每条都带灭自证） |
| S7 | 🆕 `工具/ruleprobe/EffectsProbe.cs`（+`Program.cs` 一行） | `ruleprobe.sh effects <id>` 逐行读数（快内环） |

## 五 顺手发现

1. **简报里「现状用 `Badges.For + TraitFrames`」不成立**：`Badges.cs` 是**场上 7 枚徽标**（另一功能）、`TraitFrames` 是**特效框**；效果清单的项源**只有** `CardDisplayWindow.RowsOf(TempBuff)`，全仓唯一调用点 `BattleDriver.cs:13080`。
2. **旧判据 §一 有两处要就地订正**（已在细则 §1.3 写全，建议在主判据文件里同步）：① `+0x78` 不是 trigger 而是 `markOfChaosType`（该文件 §7.1 已自纠，但 §一/§四 的正文没跟着改）；② **过滤器还有第二道**（`DisplayCardEffects`），旧文件完全没记；③ 「`type2&flag0` / `type3&trait∈{4fc..}」只说对了一半 —— 这两条排除档**只对非 mark 生效**，且外闸是 `markOfChaosType ∈ {0,50} || trait==330`。
3. **`CardEffectItem.LoadEffect` 的 `isPlayer` 实参 = 被展示卡自己的 `+0x40`**（不是"看的人"）⇒ secret/quest 打码按**卡的归属方**判，不按视角。
4. **ILSpy 又丢常量**：`CardEffectsService.EnableEffects` 里 `LoadEffect(effect, isPlayer2)` 的 `isPlayer2` 是 `default`（= false），`.c` 里是实参透传 ⇒ 又一次「看到一整排 default 就回 `.c`」。
5. **Ghidra 在本文件上也印证了「别只信一份」**：`GetEffectDesc` 的 case 1~17 在 ILSpy 里是 `Indirect jump ... not resolved`（跳表丢了），但 case 3/90 那一段 ILSpy **反而可读**（`GameStaticData.cs:1792-1830`）⇒ 两份互补，**别只读一份就下结论**。
6. **`Enchantment.cs:141-143` 那句「卡表没有 spellType 列」已过期**：`cards_engine.json` 有 `spellType`，`CardDef.SpellType` 在 `CardDef.cs:136/1016` 就有值 ⇒ 附魔的 secret/quest 判定**可以从 `CardDef.SpellType` 直接取**（不必像注释说的"派生不出来"）。
7. **`CardDisplayWindow.cs:913` 的 doc 自己写着"我们按 buff 铸行、空的根本不铸 ⇒ 中间不留空洞"** —— 这条差异已被 SG4 覆盖，改完那段注释要一起更（铁律 5）。
