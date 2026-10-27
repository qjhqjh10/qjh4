# B9 / C2T7j —— 特效 19 条「机制在」往下推一层（换 ILSpy 数据源 + 逐句）

> 查判据型子代理（只读 + 落账补丁）。承接 C2T7i 的 19 条。
> 产出：`判据/B行逐条核_特效_续2.tsv`（8 列 · 19 行）· `账/_parts/B9_C2T7j.tsv`（11 列 · 19 行）· 本报告。

---

## 一、🔴 最重要的订正：C2T7i 判的「7 条判据缺」**是误判 —— ILSpy 有真体**

C2T7i 只看 `.c` 就把 7 条判成「判据缺」。本轮**换 `out_cpp2il_dev_ilspy/`** 一读，**它们全有真体**（只是形态是协程 / 1 行）：

| 方法 | `.c` 假象 | ILSpy 真体（位置） |
|---|---|---|
| `VoiceLinesPopUpHid` | `return;`（看似空） | **`BattleManager.cs:19532`：`private void VoiceLinesPopUpHid(){ isAnyPopUpOpen = false; }`**（就 1 行，`.c` 把体吞了） |
| `ResolveUnitShake` | `<AnonymousType7>___ctor` 桩 | `BattleManager.cs:12233 _d__472 MoveNext`（协程真体） |
| `ResolveAnimInfoRangeAttack` | 同上 | 主 `:24691` = kickoff，真体 `_d__468 MoveNext` |
| `ResolveTraitAnim` | 同上 | `:24562 IEnumerator ResolveTraitAnim` + b__0/1/2 回调 |
| `MoveAndDisappearAnim` | 同上 | `CardScript.cs:12688 IEnumerator MoveAndDisappearAnim` + b__0/1/2 |
| `StopHandAnim` | 只有 `.ctor` | `BattleManager.cs:21319 IEnumerator StopHandAnim` + `_d__405 MoveNext` |
| `ResolveAttackRangedAnim` | **`.c` 缺失** | `CardScript.cs:4080 _d__360 MoveNext` 真体 |

⇒ **「判据缺」标签作废，改「可读·未逐句」**（真体在 `_d__NNN.MoveNext`，给了行号作下一批入口）。这正是本项目「⛔ 别只看 `.c` 就说读不出」那条教训的又一例。

---

## 二、结论：A = 1 · B = 18

### 唯一升 A
- **`VoiceLinesPopUpHid()`** —— 原版真体 = `isAnyPopUpOpen = false;`（ILSpy 实读，`.c` 的 `return;` 是假象）；我方 `BattleDriver.ApplyTutorialVisibility` 管的正是同一 `isAnyPopUpOpen` flag ⇒ **等价**。

### 两条「重点」的明确结论（你点名的）
- **`GetAnimTransform` —— 确认是不同原版方法，⛔ 不能当同一条**（`.c` 全档实读）：
  原版 = `BattleManager.GetAnimTransform(PositionOption, actingCard, targetCard)`，按 `PositionOption` 选 **BattleManager 自己的字段**：`case 0→actingCard+0x290`、`case 1/2→targetCard+0x290`、`case 3→+0xc0`、`case 4→+0x200`(GetEffectAnchor)、`case 5→+0x1d0`、`case 0x13→isPlayer?+0xd8:+0xd0`、`case 0x32/0x37→+0xf0/+0x108`、`case 0xe→+0xe0`…
  我方 `AnimAnchors.Resolve` 是 **`AbilityLogic.GetPos` 的复刻**（另一条方法，入参也是 PositionOption 但取的是另一套锚点）⇒ **不是本方法**。要升 A 得**按上面 BattleManager 字段全档重对**（下一批）。
- **`ReceiveAttackAnim` —— pushback 系数**：原版 `GetUnitSize(攻击方) → GetPushBackFactor(...)` + `normalize(受击方−攻击方)` + `ReplaySkipAnims`。⚠️ **`GetPushBackFactor.c` 体丢**（只有 `if param==2/3` 无返回）⇒ 系数表需 **ILSpy `SupportMethods.cs`** 读。我方 `CardFeel.PushBackMagnitude` **实现路径在**（R10 2026-10-23 坐实），**逐格系数值未对**（下一批：读 ILSpy 系数表 + 逐格比 `PushBackMagnitude`）。

### `ResolveAttackRangedAnim`（可读，clan 表）
真体（`CardScript.cs:4080 _d__360 MoveNext`）= `HasCustomRangedAttackParticles() ? GetRangedAttackParticles() : rangedAttackParticlesSO.GetRangedClanParticles(cardArmy).animInfo` → `ResolveAnimInfoRangeAttack(...)` + `WaitForSeconds(AttackStepTime)`。⇒ **clan 弹道表 = 按 `cardArmy` 查**；我方 `PlayAbilityBeat` 远程链未逐句对这张表（下一批）。

### 其余 14 条 B（可读，逐句下一批）
`PlayAnim`(282行) · `ShowPlayerActionAnim`(638行) · `ResolveBloodThirstAnim`(131行) · `PlayTriggerAnim` · `ResolveAttackAnimationEffects`(123行) · `ReturnToDeckAnim` · `GetCardEffectsToShow`(196行) · `VoiceLinesPopUpShowedUp`(ChatShow 2D 音) · `ActivateIconAnim`(HighlightTraitIcon+DisplayTriggerAnim，亮图标半) · `ActivateTriggerTraitAnim`(高亮半) · `ResolveUnitShake`/`ResolveAnimInfoRangeAttack`/`ResolveTraitAnim`/`MoveAndDisappearAnim`/`StopHandAnim`（真体在 MoveNext）—— 均给了 `.c/ILSpy` 行号作下一批入口。

---

## 三、方法学 & 证据
- 换数据源：`out_cpp2il_dev_ilspy/BattleManager.cs` / `CardScript.cs`（真体/状态机）+ `decomp_full/*.c`（可读体）。
- 关键实读：`VoiceLinesPopUpHid`(`:19532`) · `ResolveAttackRangedAnim`(`_d__360 MoveNext` 真体) · `GetAnimTransform`(全档 .c) · `GetPushBackFactor.c`(体丢→需 ILSpy `SupportMethods.cs`)。

---

## 四、产物 & 纪律
- `判据/B行逐条核_特效_续2.tsv`：8 列 × **19 行**（A=1/B=18，每行带具体下一步）。
- `账/_parts/B9_C2T7j.tsv`：11 列 × 19 行（状态列不动；只改 差异/轮次=`B9_C2T7j`/备注前缀 `[C2T7j 2026-10-27]`，原备注保留）。
  - 校验：`merge_ledger_parts.py --dry --only B9_C2T7j` → **更新 19 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → **无跨补丁重复键**（现 `_parts` 只剩我的 1 份，C2T7i 已合并归档）；纯 LF、无裸 CR、全 11 列。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。

## 五、下一批入口（都很具体了）
1. **6 条协程**：读 `_d__472`/`_d__468`/`ResolveTraitAnim:24562`/`MoveAndDisappearAnim:12688`/`StopHandAnim`/`_d__360` 的 `MoveNext` 逐句。
2. **`GetAnimTransform`**：按 §二 的 BattleManager 字段全档重对（与 `AnimAnchors.Resolve` 是两条方法）。
3. **`ReceiveAttackAnim`**：ILSpy `SupportMethods.cs` 读 `GetPushBackFactor` 系数表逐格比 `CardFeel.PushBackMagnitude`。
4. 其余 7 条可读：逐句读 `.c` 对照 `BattleDriver.Play*` / `TraitFrames.DisplayTrigger`。
