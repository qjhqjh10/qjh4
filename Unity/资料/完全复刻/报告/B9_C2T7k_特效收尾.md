# B9 / C2T7k —— 特效收尾（ReceiveAttackAnim 系数表核出 A；其余可读待逐句）

> 查判据型子代理（只读 + 落账补丁）。承接 C2T7j（VoiceLinesPopUpHid 已升 A）。
> 产出：`判据/B行逐条核_特效_续3.tsv`（8 列 · 18 行）· `账/_parts/B9_C2T7k.tsv`（11 列 · 18 行）· 本报告。

---

## 一、本轮实质结论

### ✅ 升 A：`ReceiveAttackAnim` —— pushback 系数表**逐格对上**
- 原版（ILSpy `SupportMethods.cs:1977`，`.c` 体丢但 ILSpy 有）：
  `GetPushBackFactor(UnitSize) = medium→1.0f / big→1.5f / _→0.5f`；`GetUnitSize(m) = m<=3?0 : m<=7?2 : 3`。
- 我方 `CardFeel.PushBackMagnitude(attackerMeleeAttack)`：`size=(<=3?0:<=7?2:3)`，`size==2?1.0 : size==3?1.5 : 0.5` ⇒ **三档逐格相同**。
- 方向 = `normalize(受击方−攻击方)`（我方 `CardFeel.PushBackDir`）· 幅度 ×`UnitsToOurs`=1.6865（J1 已逐场证 `MinionArea.lossyScale=(1,1,1)`）· `DOPunchPosition(punch, 0.4, 8, 0.3)`（我方 PushBackDuration/Vibrato/Elasticity 同值）。
- ⇒ **`ReceiveAttackAnim` 升 A**（系数值 + 换算 + 方向 + punch 参数全对上）。

### 🔴 `GetAnimTransform` —— 确认**不同原版方法**（落点可疑），给对照
- 原版（`.c` 全档实读）`BattleManager.GetAnimTransform` 按 **BattleManager 自己的字段** 选锚点：
  | PositionOption | 原版取 |
  |---|---|
  | 0 self | actingCard+0x290 |
  | 1,2 cardTarget/abilityTarget | targetCard+0x290 |
  | 3 friendlyHero | +0xc0 |
  | 4 enemyHero | +0x200 → `GetEffectAnchor()` |
  | 5 boardCenter | +0x1d0（裸 Transform） |
  | 0xe(14) nextFriendlyMinionSpot | +0xe0 |
  | 0x13(19) enemyHand | isPlayer? +0xd8 : +0xd0 |
  | 0x32(50)/0x37(55) friendlyDeck/enemyDeck | isPlayer? +0xf0 : +0x108（两者互取反） |
- 我方 `AnimAnchors.Resolve`（落点）= **`AbilityLogic.GetPos` 的复刻**（它 doc 引的是 `GetPos` 行号，36 档走 ability anchor/GetUIAnchor/GetHero…），**不是 `GetAnimTransform`**。
- ⇒ **落点可疑**：`AnimAnchors.Resolve` 与 `GetAnimTransform` 是**两条不同原版方法**。是否「同一 PositionOption 取到同一节点」需按上表 BM 字段逐档证；证不出就是**未实现 GetAnimTransform**。→ **B（留作下一批：逐档证锚点是否重合，或独立复刻 GetAnimTransform）**。

### 其余 16 条：**可读、但本轮未逐句**（给足入口，下一批机械逐句）
- **6 条协程（真体在各 MoveNext，已定位）**：`ResolveUnitShake`→`BattleManager.cs:12233 _d__472`（我方 `CardFeel.DoUnitShake`；pushback 互证已引 `_d__472__MoveNext.c:139-147`）· `ResolveAnimInfoRangeAttack`→`_d__468` · `ResolveTraitAnim`→`:24562`+b__ · `MoveAndDisappearAnim`→`CardScript.cs:12688`+b__ · `StopHandAnim`→`:21319`+`_d__405` · `ResolveAttackRangedAnim`→`_d__360`（clan 表按 cardArmy：`GetRangedClanParticles(cardArmy)`）。
- **10 条 .c 可读**：`PlayAnim`(282行) · `ShowPlayerActionAnim`(638) · `ResolveBloodThirstAnim`(131) · `PlayTriggerAnim` · `ResolveAttackAnimationEffects`(123) · `ReturnToDeckAnim` · `GetCardEffectsToShow`(196) · `VoiceLinesPopUpShowedUp`(ChatShow 音) · `ActivateIconAnim`(亮图标半) · `ActivateTriggerTraitAnim`(高亮半)。

---

## 二、诚实交代（覆盖）
本轮**只把 `ReceiveAttackAnim` 系数表逐格核出（A）** 与 **`GetAnimTransform` 出对照并确认为不同方法（B）**；**6 条协程的 MoveNext 体 + 10 条可读 .c 本轮未逐句**（给了精确行号入口）。原因：本轮聚焦「两条重点」+ 把「判据到底在不在」钉死（都在，ILSpy/`.c` 可读）。**剩下的是机械逐句活**，按 §三 入口直接做即可（不需要再查判据在不在）。

## 三、下一批入口（照行号直接逐句）
1. 6 协程 MoveNext（行号见 §一末）。
2. GetAnimTransform：按 §一 的 BM 字段表逐档证锚点是否 = `AnimAnchors.Resolve`。
3. 10 条可读 .c：逐句对 `BattleDriver.Play*` / `TraitFrames.DisplayTrigger` / `CardFeel.*`。

## 四、方法学 & 纪律
- 关键实读：ILSpy `SupportMethods.cs:1977 GetPushBackFactor` · `CardFeel.PushBackMagnitude/UnitSizeOf/UnitsToOurs/PushBackDir` · `BattleManager__GetAnimTransform.c` 全档 · `AnimAnchors.Resolve` 全档。
- `判据/B行逐条核_特效_续3.tsv`：8 列 × 18 行（A=1/B=17）。
- `账/_parts/B9_C2T7k.tsv`：11 列 × 18 行（状态列不动；只改 差异/轮次=`B9_C2T7k`/备注前缀 `[C2T7k 2026-10-27]`，原备注保留）。校验：`merge --dry --only B9_C2T7k` → **更新 18 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → 无跨补丁重复键；纯 LF、无裸 CR、全 11 列。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。
