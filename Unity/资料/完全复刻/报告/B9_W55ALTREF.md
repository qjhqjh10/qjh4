# B9_W55ALTREF — `activeAbilityTextReferenceId` / ability 级 `targetsAvailableCounter` 真值

> 查判据代理 · 2026-10-27 · **只读**（未改任何 `.cs` / `.py` / `.json` / 账 / 正本；未跑 Unity；未动 git）
> **主交付物 = `资料/完全复刻/判据/activeAbilityTextReferenceId_真值.md`**（本文件只是摘要，细节一律看那份）

---

## 一 结论

1. **派活包的前提有一条不成立**：`activeAbilityTextReferenceId` 全池**只有 3 张卡非空**
   （`SOR30`→`SOR30_new` · `SOR25`→`SOR25_new` · 一张**不在池**的 GUID 卡→`AM68`），
   ⇒ **「34 条拼不出来」里那 30 条 `Effect<名>` 一条都不来自这个字段**。
   它们来自**另一条链**：`abilityLogic[].buffEffect.effectText`（或 `handBuff.cardEffect.effectText`）
   → `GameStaticData.GetEffectDesc` → 键 = `Card_Abilities/<effectText>`（`GameStaticData.cs:1772`）。
   那 30 条里 **29 条有主（29 张池内卡）+ `EffectBoltPistol` 是孤儿键（0 张卡引用）**。
2. 补 `activeAbilityTextReferenceId` 能救的**只有 2 张池内卡**（`SOR30` / `SOR25` 的技能面板正文，今天是**旧版文本**）。
3. **同族缺列里有一条更值钱的**：`cardDescriptionTextReferenceId`（非空 44 / **池内 15**）
   ⇒ 其中 **10 张池内卡今天卡面印的是旧版正文**（`TL66` `SOR24` `SOR30` `SOR39` `UM72` `SOR47` `TL24` `TL44` `UM57` `UM68`）。
   剩下 5 张恰好已在 `OfficialFaceTextWhitelist` 里、走我们自己的 `desc`（= 新版语义），表面正常。
4. **ability 级 `targetsAvailableCounter`：原版有这一格，但 1,351 张 100% 全 0**
   ⇒ 今天 `ShouldShowAbilityTargetsCounter` 恒 false **与原版一致**，**不需要补数据**、也**不该**给生成器加这条路径
   （`criteria_carries_targets` 闸会把它全丢，只会多一列「永远 null」）。

## 二 证据（`文件:行号`）

| 事 | 出处 |
|---|---|
| 字段声明 + 偏移 | `d:/2/tools/il2cpp_out/dump.cs:23329` `activeAbilityTextReferenceId // 0x248`；`:23330` descRef `+0x250`；`:23335` targetingRef `+0x278`；`:23341` 卡级 `targetsAvailableCounter // 0x298`；`:23764` **ability 级 `targetsAvailableCounter // 0x40`** |
| 键 = `Card_Abilities/` + (altRef ?: uid) + suffix | `RawCardScript.cs:2374-2405`（`term` `:2396`）· `:2408-2445`（`term` `:2411`）· `:2259-2311`（`term2` `:2309`） |
| `((` 短路支（id 里带 `((` ⇒ 直接当正文） | `RawCardScript.cs:2384-2389` · `:2298-2303` —— **现数据 0 张卡命中**（6 个 `*ReferenceId` 字段逐字段扫过） |
| `Card_Abilities/Ability_Name_*` | 官方表 **0 条** ⇒ `GetLocalizedAbilityName`（`:2362-2364`）现 build 恒缺键 |
| `Effect<名>` 的真链 | `CardEffect.cs:12` → `GameStaticData.cs:1639 GetEffectDesc` → `:1772 term = "Card_Abilities/" + effectText` → `:1778-1782` **缺键原样返回 effectText** |
| 卡面正文的消费者 | `BattleDriver.cs:27991-28025`（`FaceTextFull`，四处 `"Card_Description/" + c.Id`：`:28013/28015/28017/28025`）；白名单 `:27975` = `{ASH48, SAU58, SAU15, TAU65, AM72, SOR25}` |
| 卡级 `targetsAvailableCounter` 已落 | `CardDef.cs:2273`（`role == "avail"` / `AvailSet`）；生成器 `gen_cards_engine.py:2948` `add(-3, -1, "avail", …)`（**只有卡级这一条**） |
| ability 级 0 张（独立旁证） | `BattleDriver.cs:35015-35029` 那段注释（早前另一路得出的「0 张」） |

**三源核验**：① 缓存 `cards_raw.json` ② 本次从 `__data` 二进制重新 `read_typetree()`（`w55/fresh`）③ `export_allcards.py --check`
⇒ **1,351 张 / 卡片·字段级非空数 / 顶层键集 三项全部一致，0 差异**。

## 三 没查清的部分

1. **`effectText` 这条链我们引擎/界面侧的对应物**没查（只知道原版怎么取键、缺键怎么兜底）。
2. **`SOR66`（Armorium Cherub）无出路**：`hasActiveAbility=1`、无 `Card_Abilities/SOR66`、`altRef` 也空 ⇒ 原版走到 `throw`（`:2443`）。补列救不了，且「原版会不会真崩」无第二证据。
3. **`Card_Targeted/*` 这条链我们整条没有**（`targetingTextReferenceId` 池内 **51/1,128** 非空；原版消费点 = `BattleManager._ChooseUnitTarget(_d__780).MoveNext`，ILSpy `BattleManager.cs:1784`）。我们 `.cs` 里 `Card_Targeted` **0 命中**。⇒ 是**真缺**还是**别处做过了**，没查。
4. **白名单 `AM72` 疑似应为 `UM72`**：真正有 descRef 的是 `UM72`，`AM72` 现读 descRef 为空。是笔误还是另一张卡 —— **没查，留给调度台裁**。
5. **`EffectBoltPistol` 是孤儿键**（1,351 张 0 引用），给谁用的没查。

## 四 该怎么改（可施工规格 · 本次一个字没改）

- **生成器**：`A1799_FIELDS`（`gen_cards_engine.py:3428`）追加两行 —— `activeAbilityTextReferenceId`（string，缺值 `""`）与 `cardDescriptionTextReferenceId`（string，缺值 `""`）。**会动池指纹 ⇒ 只能在调度台的无写手窗口做**。
- **`CardDef` / `CardDatabase`**：两个同名公有字段，**追加到类尾**（⛔ 别插中间，会推下游行号 ⇒ `check_ledger_points` 漂移）；**不加 `[Obsolete]`**（这两列有真消费者）。
- **技能面板（2 张）**：`SkillPanel` 的四个取键点统一走 `AbilityTextId(c) = ActiveAbilityTextReferenceId ?? Id`。
- **卡面正文（10 张，更要紧）**：`BattleDriver.FaceTextFull` 的四处 `"Card_Description/" + c.Id` 改成 `+ DescKey(c)`。
- **ability 级 `targetsAvailableCounter`：不加列、不改代码**，把「恒 any」当判据写死并附指针。
- **验收口**：数据侧 = 重生成后两列非空数应 = **2 / 15**（池口径）；`SOR30` 面板应含 `Heal 3`、不含 `armour 1`；`SOR25` 应含两次 `+1`；10 张卡面正文应 ≠ 旧版键的值。宿主 `BattleScene.Run`。

## 五 顺手发现

1. **同一条缺列正在让 10 张池卡的卡面正文印错**（§一·3 那张名单，逐卡新旧对照在判据文件 §5·3）—— 这条比派活包原定的「2 张技能面板」影响大 5 倍，建议**同一批一起做**。
2. **`cardDescriptionTextReferenceId` 还兼任 `Card_Name/<ref>` / `Card_FullName/<ref>` 的名字兜底**（`RawCardScript.cs:2150` / `:2224`）—— 这是「GUID 卡靠它把身份指回正规卡」的机制（现读的 44 条里 29 条是 GUID 卡指向短码 id）。补列时要意识到它**不止是描述**。
3. **`Effect<名>` 缺键时原版会把 id 原样印到屏幕上**（`GameStaticData.cs:1778-1782`）—— 与铁律「不许静默失败」方向相反的原版行为，⛔ 别以为是回空串。
4. **`cards_engine.json` 里 `effectText` / `buffEffect` / `handBuff` / `cardEffect` 全部 0 命中** ⇒ 整个「buff/效果标题」数据面我们一条都没有（不只是键拼不出）。
