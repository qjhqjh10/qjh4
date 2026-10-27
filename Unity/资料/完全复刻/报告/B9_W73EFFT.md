# B9_W73EFFT — `effectText` 链 查判据 报告

> 只读代理 · 2026-10-27 · 产出 = `资料/完全复刻/判据/effectText链_判定.md`（本文只是摘要）

## 结论

1. **链已钉死**：`CardEffect.effectText(+0x28)` → `GameStaticData.GetEffectDesc` → `term = "Card_Abilities/" + effectText` → `GetTranslation` → `ModifyLocalization` → **缺键时 `return effectText`（把 id 原样印屏）**。`GetEffectDesc` 全树**只有两个调用点**，都在 `CardEffectItem.LoadEffect`（`：107` / `：153`），即**卡面「受到以下影响」右列**那一格。
2. **真值 = 38 行 / 31 个值 / 31 张卡 / 全在池**。挂在 `cardAbilities[].abilityLogic[]` 的 `buffEffect`（33）与 `handBuff.cardEffect`（5）；`activeAbility` / `alternativeAbility` / 所有 `*ScoringCriteria` **0 条**（全树 8,620 个 `CardEffect` 实例，只有这 38 个非空）。
3. **键可达性 31/31 全中**（en+zh 都非空）⇒ **一条都不会印 id**。`Card_Abilities/*` 120 条的完整归属 = **本链 31 + 批 A 86 + 真没用上 3**（`EffectBoltPistol` 真孤儿 · `SOR25`/`SOR30` 被 `_new` 遮蔽），31+86+3 = 120 ✅。
4. **我方现状（三源逐个点）**：`cards_engine.json` **0**；`cards_truth.json` **38（全在！）**；`cards_raw.json.full_raw` 是原始来源。
   🔴 **这是既有记录要更正的一条**：说「这条链整条没有」只对**运行时表**成立 —— **数据抽取早就做完了**（`gen_cards_truth.py` 的 `compact_buff` 已抽，38/38 与 SO 逐条一致）。真缺口是 **「没进运行时表 + 没有运行期载体 + 没有消费端」**，比「整条没有」轻一档。
5. **展示面的槽已经建好了**：`CardDisplayWindow.cs` 的 5 个 `EnchanterText` + 5 个 `EffectText` 槽、排版字号全照原版，**只差喂真实文本**；`SkillPanel.cs:2183-2255` 的 `AbilityRaw/AbilityMarked/AbilityTerm` 就是现成的「键 → 官方词条 → `ModifyLocalization`」三件套（现在是私有静态）。

## 证据（文件:行号）

| 事 | 出处 |
|---|---|
| `effectText` 字段 | `dump.cs:119068`（`CardEffect.effectText // 0x28`）· `buffEffect // 0x50` / `handBuff // 0x60`（`dump.cs:19471/19473`） |
| 键拼法 + 缺键印 id | `GameStaticData__GetEffectDesc.c:64` / `:66` / `:75` / `:76` / **`:80`** |
| 前缀常量已解析 | `DAT_1842cd940` → RVA `0x42CD940` → `stringliteral.json` = `"Card_Abilities/"` |
| 唯一两个 GetEffectDesc 调用点 | `CardEffectItem__LoadEffect.c:107` / `:153` |
| 「谁给的」三档 | `CardEffectItem__LoadEffect.c:116` / `:135` / `:127`（写 `""`） |
| 展示链 | `CardDisplayWindow__ShowBattleCard.c:28` → `__DisplayCardEffects.c:34,69-72` → `EntityScript__GetCardEffectsToShow.c:34`（遍历 `+0x108`） |
| `handBuff` 施加点 + `isHandBuff=true` | `AbilityLogic__PlayAbility.c:1393-1394` / **`:1405`** / `:1414` / `:1428`（🔴 ILSpy 丢了 `= true`，只有 `.c` 有） |
| 8 条 bt2/bt3 **也进 `+0x108`**（本次补读） | `CardScript__AddEffect.c:331-417`（case 1/2/10）· `:461-498`（case 3）· 未处理档 `caseD_7` 只 `LogWarning` 不追加（`:767`） |
| 我方 0 命中 / truth 38 | `cards_engine.json` 0 · `cards_truth.json` 38（`abilities[].logic[].buff.effectText`）· `three_src.py` |
| 我方展示端项源 | `CardDisplayWindow.cs:851 RowsOf(TempBuff)` · `BattleDriver.cs:13080` 唯一调用点 |
| 已有取词三件套 | `SkillPanel.cs:2183-2255`（`AbilityRaw/AbilityMarked/AbilityTerm`） |
| 已消费的 1 个键 | `BattleDriver.cs:13645` + `EffectResolver.cs:7762`（硬编码 `GSC55` → `EffectDisplayCardsAddToDeck`） |

## 没查清的部分

1. **运行期项源模型不统一**（硬阻塞，非本轮可解）—— 见 `判据/CardEffectsToShow_判据.md` §7.2（`+0x108` 被我们拆成 4~5 套异构模型）。`EffectText` 挂哪一格取决于那条投影先落地。**但数据列与 `CardDef` 那半可以并行先做。**
2. **`GetEffectName` 第二档**（`Card_Name/<effectName>` 取不到时去 `fullCardCollection` 按 `Predicate` 反查）—— `<>c__DisplayClass232_0` 没展开。现数据 16 条第一档全命中，今天走不到。
3. **官方表 `EffectIdolatrous` en/zh 数字打架**（EN `vulnerable 1` / ZH `vulnerable 8`，31 条里唯一一条）。复刻应照哪一列 **未查清**（要查那张附魔卡的实值，或由调度台裁定）。
4. **同卡多挂点会不会叠出多条展示行**（`GSC51`×2 / `TL47`×3 / `TAU51`×5）：本文只证明「值相同」，叠加行为属 `CardEffectsToShow` 的面，未复判。

## 该怎么改（施工规格 —— 只写，一个字没改）

- **生成器** `工具/gen_cards_engine.py`：新增 `abilityEffectTexts: [{ab, lg, src, text, name}]`（`src` = `buff`/`hand`，**必须有**，`TAU51` 的同一 `(ab,lg)` 两条同值不同挂点）；沿用既有 `{ab,trigger,lg}` 列族；只写 38 条非空；**bump `version`（现 34）**。
- **`CardDef.cs`**：`IReadOnlyList<AbilityEffectText> AbilityEffectTexts` + `AbilityEffectTextsVersioned`（三态）+ `EffectTextOf(ab,lg,isHand)`。
- **运行期载体**：`ActiveCardEffect`（29 行）· `UnitState.TempBuff`（6 行）各加 `EffectText`/`EffectName`；**`changeCost`/`addTrait` 那 8 行**依赖统一投影（§七·1）。
- **消费端**：`CardDisplayWindow.RowsOf` 的 `What` —— 有 `EffectText` 走 `AbilityTerm("Card_Abilities/"+text)`，没有回落现状（＝原版 `effectText` 为空那一支，不是我方发明）；把 `SkillPanel` 三件套**提成共用件**（连字体闸一起搬）；左列 `Who` 按原版三档（`effectName` → 附魔卡名 → 空）。
- **验收 5 条**：列里 38 行/31 distinct · 逐条 `Loc.T != key` · `EffectBoltPistol` 不在列里 · `EffectTextOf` 命中/未命中 · 开窗后 `EffectWhat(0)` 等于该键词条。

## 顺手发现

1. 🔴 **既有说法要更正**：「`effectText` 这条链整条没有」—— 数据在 `cards_truth.json` 里**38/38 齐**，缺的是接线。（已写进判据 §四·1。）
2. 🔴 **`ILSpy` 又丢一处 `= true` 写点**：`CardEffect.isHandBuff` 在 `out_cpp2il_dev_ilspy` 全树**只有读、没有写**，真写点是 `AbilityLogic__PlayAbility.c:1405` 的 `*(…+0xa8) = 1`。与 `secretBuff` / `questBuff` 同族（第三次）。
3. **同一个偏移号 `+0x28` 在两处含义不同**：`CardEffect+0x28 = effectText(string)` vs `CardEffectItem+0x28 = effectText(TextMeshProUGUI)` —— 读 `.c` 时极易串，判据已标。
4. **`effectName` 的 4 个值（TL47/TL49/TL50/TAU51）全在池**，且 `Card_Name/<id>` 全命中 —— 左列可直接拿 `Card_Name/` 前缀解决，不必走第二档。
5. **官方表自身 bug**：`EffectIdolatrous` EN=1 / ZH=8（`diffnum.py` 全 31 条比对，仅此一条）。
6. **120 条 `Card_Abilities/*` 三分类闭合**（31/86/3 = 120），`EffectBoltPistol` 是真孤儿、`SOR25`/`SOR30` 是被 `_new` 遮蔽（不是孤儿）。
