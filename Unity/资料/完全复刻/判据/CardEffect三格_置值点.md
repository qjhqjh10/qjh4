# `CardEffect` 三格（secretBuff / questBuff / originalIsPlayer）置值点 与 用途

> 判据型子代理（只读）。答：这 3 格**在哪儿置值、什么条件、谁消费、我们这边要不要建/建在哪层、有没有死的**。
> 源 = `decomp_full/CardScript__AddEffect.c` + `out_cpp2il_dev_ilspy/{CardEffect,CardScript,EntityScript,CardEffectItem,GiveHuntsMark,GiveMarkOfChaos,PlayerHand}.cs` + `dump.cs` `SpellType`/`CardEffect`。
> **一句话：三格全部 LIVE（非死），置值点都找到了。**

---

## 一、`secretBuff`(+0x90)

- **写入 = true**：`CardScript__AddEffect.c:853-854`
  ```
  lVar8 = *(param_2 + 0x88);            // cardEffect.enchantment（附魔卡 RawCardScript）
  if (*(int*)(lVar8 + 0x90) == 0x3c)     // 附魔卡的 spellType == 60 (SpellType.SecretOrder)
      *(undefined1*)(param_2 + 0x90) = 1; // cardEffect.secretBuff = true
  ```
  ⇒ **条件 = 这条效果的【附魔卡】`spellType == SpellType.SecretOrder(60)`**（`+0x90` 是 RawCardScript 的 spellType，与 `EntityScript.get_isTrapCard` 用 `+0x90==0x46(70)` 同一字段）。
- **写入 = false**：`EntityScript.RevealedSecret.c:59`（`*(lVar3+0x90)=0`，揭示那一刻清）+ `EntityScript.cs:2135`（`cardEffect4.secretBuff = false`）。
- **消费**：`CardEffectItem.cs:31` `object obj = effect.secretBuff & isPlayer;` —— **卡面「受到以下影响」那一行的显隐**（secretBuff 的效果**只给拥有者(isPlayer)看**，对面看不到）+ `EntityScript.RevealedSecret` 揭示后清。
- **LIVE**（不是死格）。

## 二、`questBuff`(+0x91)

- **写入 = true**：`CardScript__AddEffect.c:856-887`
  ```
  if (*(int*)(lVar8 + 0x90) == 0x78) {   // 附魔卡 spellType == 120 (SpellType.Quest)
      ...循环 RemoveEffect 掉其它「附魔 spellType==0x78」的旧 quest 效果...
      *(undefined1*)(param_2 + 0x91) = 1; // cardEffect.questBuff = true
      *(int*)(param_1 + 0x270) = 0;
      BattleCardUI.DisplayStatusAnim(+0x290, 0x2ee, 1);   // 播 750 状态
      CardScript.UpdateTraitIcons();
  }
  ```
  ⇒ **条件 = 附魔卡 `spellType == SpellType.Quest(120)`**；且**同一张卡上旧 quest 效果先摘**（`RemoveEffect`）。
- **消费**：`CardEffectItem.cs:41/43`（`questBuff` 门控那一行的显隐/独占）。
- **LIVE**（有 Quest 卡时激活；`SpellType.Quest=120` 在枚举里存在）。

## 三、`originalIsPlayer`(+0xA0)

- **写入**：`CardEffect.cs:106`（构造/拷贝：`cardEffect.originalIsPlayer = originalIsPlayer`）+ `CardScript.cs:14294 / 14593 / 14661`（`cardEffect.originalIsPlayer = base.isPlayer`）—— ⇒ **= 施放/挂载那一刻【施放方是不是玩家侧】**（写死在 AddEffect 时）。
- **消费（重度，多处）**：
  - **回合到期**：`CardScript.cs:7157/7180/7194`（`playerTurn == / != cardEffect.originalIsPlayer` —— `untilPlayerTurnStart` / `untilEnemyTurnStart` 那几档按「谁的回合」决定摘不摘；`PlayerHand.cs:2759` 同理 `flag4 != originalIsPlayer`）。
  - **mark 归属**：`GiveHuntsMark.cs:24` `if (cardEffect.originalIsPlayer && cardEffect.trait == DefinedTrait.huntMark)`、`GiveMarkOfChaos.cs:26` `if (cardEffect.originalIsPlayer && trait==markOfChaos …)` —— **只有「原始属于玩家侧」的 mark 效果才被算/才授**。
- **LIVE（重度）**。

---

## 四、对我们（要不要建 / 建在哪层 / 有没有死）

| 格 | 原版置值来源 | 我方现状 | 建议 |
|---|---|---|---|
| `secretBuff` | 附魔卡 `spellType==SecretOrder(60)` | 我方 `CardDef.SpellType` **有**（`cards_engine.json` 的 spellType 列） | **不必新建 per-effect 字段**：显示层可按「来源卡 `spellType==60`」派生。⚠️ 若要逐 effect 稳定，需在附魔/挂载时把这格落到记录上（`ActiveCardEffect` 加一格）。 |
| `questBuff` | 附魔卡 `spellType==Quest(120)` | 同上（`SpellType=120` 在枚举） | 同上：按 `spellType==120` 派生；注意原版还带「摘旧 quest + 播 0x2ee 状态 + UpdateTraitIcons」的副作用。 |
| `originalIsPlayer` | 施放方 isPlayer（AddEffect 那一刻） | 我方 `HandEffect.ActingUnit`（施放者）**有**；但**没存成 bool** | 读取时由 `ActingUnit` 的阵营推（`ActingUnit` 属于哪一侧）；若要稳定（施放者可能离场/被清），在挂载时记一格更稳。 |

**结论：三格全部 LIVE，无一是死的（⛔ 不能报「原版亦无」）。** 都能在我方**派生**（secret/quest ← `CardDef.SpellType`；originalIsPlayer ← `ActingUnit` 阵营），但**当前没落成 per-effect 字段** ⇒ 要做 `GetEffectsToShow`（映射判据 §一那三格「没建」）时，**建议在统一项源/挂载时把这三格固化**（否则「施放者离场后 originalIsPlayer 取不到」「按 TempBuff 读不到 spellType」）。

---

## 五、查证边界（搜过哪里）
- 写点：`grep secretBuff/questBuff/originalIsPlayer` 于 `out_cpp2il_dev_ilspy/` 全树 + `decomp_full/CardScript__*.c` 的 `+0x90/+0x91` 字节写。
- `questBuff` 的 ILSpy 里**只有读、没有 `= true` 的赋值**（只有 `CardEffectItem` 读）—— 但 **`.c` 的 `CardScript__AddEffect.c:887` 明确写 `*(param_2+0x91)=1`** ⇒ **ILSpy 丢了那处写、`.c` 有**（又一次「别只看一份」）。⇒ **`questBuff` 不是死格**。
- `secretBuff = true` 同理：ILSpy 只见 `=false`（EntityScript），`.c:854` 才是 `=1` 的真写点。
