# A951 —— `chooseone` / `chooseeffect` 两族还要不要补东西

> **结论一句话：够，不用动。** 两种 ask 引擎都认、都结算、面板都弹，各有端到端测试。
> 建档：2026-10-18　|　来源：判据链自证第一轮（`A955`–`A960`）· 判据由调度台查清后转交执行代理整理
> 相关：`资料/普查产出_1017/W_B22_选牌ask时机.md`（`A904` / `A905` 那一轮）· `资料/选牌Choose_数据与设计.md`（2026-10-10 并入）

---

## 〇、结论

`chooseone` 与 `chooseeffect` **不是缺口**：两族的 **ask 点都进引擎**（`EffectResolver` 有 handler）、
**都结算**、**面板都弹**（走 `BattleDriver.ShowAsk` 同一条路），且**每族都有端到端测试**。
⇒ **不需要新增实现，也不需要为它们另造 `CardDef`**（理由见 §三）。

---

## 一、端到端测试（各自锚点，判据 = 运行得起来 + 断到面板/结算）

| 族 | 卡 | 测试锚点 | 覆盖到哪 |
|---|---|---|---|
| `chooseone` | **`The Fang`**（`Choose one: Deploy a Grey Hunter; Heal 4 … or Draw 2 cards`，费 2） | `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs:10543-10586`（小节标题 `// ---- 21-a 三选一`） | 面板**三族共用**（断过「没另开第三份克隆」）· 候选三项 · 选完的结算 |
| `chooseeffect`（池子 = **真卡**） | **`Exemplary Warrior`**（`Your Warlord heals 1 and chooses an effect`，费 2） | 同上 `:10587-10624`（小节标题 `// ---- 21-b`） | 三项**都是真卡**（取自 `ChooseEffectPools`） |
| `chooseeffect`（池子 = **载荷**/合成卡） | **`Hyper-adaptation`**（`Choose an effect and give it to a friendly troop`） | 同上 `:10625-10659`（小节标题 `// ---- 21-c`） | **合成卡**：源卡插图 + 选项文字 |
| `chooseeffect`（`hand` 作用域） | **`Infinite Biomorphologies`**（`Choose an effect and give it to all troops in your hand`） | 同上 `:10904-10960`（小节标题 `// ---- 21-e-④ A905`） | 面板**照常开**（`A905` 之前那条「不问了」的短路**已删**）；断「手牌只少打出的那一张」= 给的是**加成、不是换牌** |

🔴 **`A905` 那条痕迹别退回**：`hand` 那一支原来被 `ShowAsk` 的短路挡在门外
（= 引擎按 `ctx.Rng` **替玩家挑**），2026-10-17 删掉短路后**面板照常弹**
（判据 = `CardPresentation/Battle/BattleDriver.cs:4714-4724`，`ShowAsk` 的 `else if (op.Verb == "chooseeffect")` 那一支里
那段 `🔴 **2026-10-17（A905）删掉了 \`ChooseEffectIsHand(op)\` 那条短路。**`）。

> ⚠️ **顺手发现（未改，不在白名单）**：`RuleEngine/Core/EffectText.cs:4156` 把这条痕迹的落点写成
> `BattleDriver.cs:4446` —— **那个行号是错的**（`:4446` 落在 `AddQuota` 里，与 `A905` 无关）；
> 正确落点 = `:4714-4724`（由 `grep -n "A905" BattleDriver.cs` 独占命中）。**留给调度台裁**
> —— 本条 ⛔ 不自行改 `EffectText.cs`。

---

## 二、全池卡数（**近似，正则复算** —— 只是量级，别当判据）

判据 = `d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（全池 1126 张）的 **`desc` 文本正则**。

| 族 | 张数 | 卡号 |
|---|---|---|
| `chooseone`（字面 `Choose one`） | **6** | `ASH83` Craftworld Convergence · `DA51` The Rock · `EC4` Duelist's Hubris (Lucius' Talent) · `GSC6` Inscrutable Cunning · `SOR13` Hymn of Battle · `SW53` The Fang |
| └ **+`Carnifex` 归一** | **7** | `TL25` Carnifex（`Rally: Choose and gain a bonus (+2 Melee, +2 Ranged or Armour 1)` —— **没写字面 `Choose one`**，语义同族，故「归一」） |
| `chooseeffect` | **4** | `GOF_Mekaniak` Mekaniak（`Give a Kustom Job of your choice to …`，`EffectText.cs:4198` 那支）· `TL51` Hyper-adaptation · `TL53` Infinite Biomorphologies · `UM_Exemplary_Warrior` Exemplary Warrior |

复算办法（三条正则，与 `Core/EffectText.cs` 的 `TryChooseEffect` 三支**一一对应**）：
① `^(.+?)\s+and\s+chooses? an effect\s*$` · ② `^choose an effect and give it to (.+?)\s*$` ·
③ `^give\s+an?\s+kustom\s+job\s+of\s+your\s+choice\s+to\s+(.+)$` ⇒ 合计 **4** ✅。

> ⚠️ **别用 `grep chooseeffect cards_engine.json`** —— 池子里**一个 `chooseeffect` 字样都没有**：
> 字形只出现在**解析层产出的 op** 里（`desc` 写的是英文卡面原文）。按 op 名去数卡会**数出 0 张**。

---

## 三、`ChooseOptionStableId` 返回 `<无卡号>` —— **设计如此，不是缺陷**

`Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs:4272-4276`：

> ⚠️ 只有 `choosecard` 会填 `_askCands`；`chooseone` / `chooseeffect` 的候选是**合成卡**
>   （没有 `CardDef` 可指）⇒ 这里返回 `<无卡号>`（**出声**，不静默给空串）。

🔑 **这一族本来就没有 `CardDef`**：`chooseone` 的候选是**卡面自带的文本**、`chooseeffect` 的候选是
**载荷/池子里的合成项**（`ChooseEffectPools`；`EffectResolver.cs:2494` 明写「表里**没有的卡**用
`chooseeffect` ⇒ 结算层**如实报「没登记池子」**，不静默空过」）。

⇒ ⛔ **别为这两族另造 `CardDef`**：那是**用假身份换掉一条已经出声的合法路径**
（`<无卡号>` 是**出声**、不是静默），造的 `CardDef` 还会跟「卡面标题 / 立绘配对都用 `CardDef.Id`」
那条恒不相等的老坑撞上。

---

## 四、待办口径（给下一位）

- ✅ **这两族不用补实现** —— 引擎、面板、测试三样都在，`A904` / `A905` 那一轮已经收口。
- ⚠️ **唯一没单独断到的**：`Mekaniak`（③ 那支 `Kustom Job`）**没有找到它自己的端到端面板测试**
  （池子**已登记**：`RuleEngine/Core/EffectResolver.cs:2518` 的 `"Mekaniak"` 那一项）。
  这一条**本轮没查透**，记在这里供下一位裁 —— ⛔ 不要据此说它「没实现」。
- 📌 `chooseone` 的**选项呈现**（三项文字）与 `chooseeffect` 的**合成卡图文**是**两套不同的画法**，
  别把其中一套的结论套到另一套上。

---

## 五、复查坐标（下一位要重核时照这三行走）

```bash
sed -n '10543,10546p;10587,10590p;10625,10628p;10904,10910p' \
  d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
sed -n '4272,4276p' d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs
sed -n '2494,2520p' d:/4/Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs
```
