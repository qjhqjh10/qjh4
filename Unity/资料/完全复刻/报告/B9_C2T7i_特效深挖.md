# B9 / C2T7i —— 特效深挖：① NestedFadeGroup 结构判定 ② 19 条「机制在」逐条核

> 查判据型子代理（只读 + 落账补丁）。承接 C2T7h（特效 73 条）。
> 产出：`判据/NestedFadeGroup_结构判定.md`（①，结论+整树证据）· `判据/B行逐条核_特效_续.tsv`（8 列 · 37 行）· `账/_parts/B9_C2T7i.tsv`（11 列 · 37 行）· 本报告。

---

## 一、① NestedFadeGroup 一族 —— **原版真嵌套 ⇒ 我们结构缺（要做），不是 D**

**结论（一句话）**：**原版【真嵌套】** —— 9 个 `NestedFadeGroup` 组根，每个根的子树上挂着 **4~6 个 `NestedFadeGroupSpriteRenderer/TextMeshPro` 子件**（57 个 = 9 组根 + 38 SR + 10 TMP）。⇒ **我们结构缺（要做）**，不是「机制不适用（D）」。

**推翻 `[G12 现核]「组树不嵌套」`**：G12 只查了「9 个组根**彼此的父链**」（确实互不为父，`parentChainNFG=0`）—— **没查子树**。真正的嵌套在「根 → 子孙件」这一层：

```
TraitIconContainer Right 3        [NestedFadeGroup 组根]
├─ IconBackground                 [NestedFadeGroupSpriteRenderer]
└─ Container
   ├─ Without counter
   │  ├─ TraitIcon                [SR]
   │  └─ Trait Not Active         [SR]
   └─ With counter
      ├─ TraitIcon                [SR]
      ├─ TraitCounter             [NestedFadeGroupTextMeshPro]
      └─ Trait Not Active         [SR]
```

- 全量统计（脚本现算 `m_Children` 树）：9 组根**子树内 NFG 家族子孙 = 4~6/根**（表见 `判据/NestedFadeGroup_结构判定.md` §三）。
- **差在哪**：原版每组根一份 alpha（7 个由 `BoardTraitIcon.fadeGroup` 单独驱动）→ 广播给子树 SR/TMP ⇒ **分组独立淡入**；我方 `CardView._badgeAlpha`（`ApplyBadgeAlpha`）**整卡一个标量** ⇒ 一淡全淡。
- **施工**：卡面 trait/计数几组各建「组根 alpha」+ 子树订阅；`BoardTraitIcon` 只推该组 alpha；断言「只淡某组 ⇒ 别的组 alpha 不变」。**真机可见（触发类图标淡入），非休眠机制。**
- 落账：18 条 `NestedFadeGroup*`（含 Base）判 **B / 要做（结构缺）**，**不是 D**。

---

## 二、② 19 条「机制在、未逐条核」逐条核 —— 全部仍 B，两类

读 `decomp_full` 的 `.c` 体后分两类（**0 条能升 A**）：

### A. 判据缺（7 条，`.c` 体丢失 / 空 / 缺）——**下一批需读 ILSpy `BattleManager.cs`/`CardScript.cs` 或 VA 反汇编**
| 方法 | 现象 |
|---|---|
| `ResolveUnitShake` / `ResolveAnimInfoRangeAttack` / `ResolveTraitAnim` / `MoveAndDisappearAnim` | `.c` 体是 `<>f__AnonymousType7<Int32Enum>___ctor` **匿名类型桩**（Cpp2IL 丢体） |
| `StopHandAnim` | `.c` 只有协程工厂 `.ctor`（体在 `_d__405__MoveNext`） |
| `ResolveAttackRangedAnim` | `decomp_full` **无该 `.c`**（文件缺失） |
| `VoiceLinesPopUpHid` | `.c` 体 = **`return;`（空方法）** ⇒ 原版本身无可做（我方 ApplyTutorialVisibility 承接，无需动作） |

### B. 可读未逐句（12 条，`.c` 有体，本轮未逐句对照我方方法）——**下一批逐句**
`PlayAnim` · `ShowPlayerActionAnim` · `ResolveBloodThirstAnim` · `PlayTriggerAnim` · `ResolveAttackAnimationEffects` · `ReturnToDeckAnim` · `GetCardEffectsToShow` · `VoiceLinesPopUpShowedUp`（播 `SoundAssetCollection.ChatShow` 2D 音）· `ActivateIconAnim`/`ActivateTriggerTraitAnim`（`HighlightTraitIcon`+`DisplayTriggerAnim`，**亮图标那一半未核**）· `GetAnimTransform`（`PositionOption 0x13/0x32/0x37`+switch 选 `0xd0/0xd8/0xf0/0x108`；⚠️ 我方 `AnimAnchors.Resolve` 是 `AbilityLogic.GetPos` 的复刻，**不同原版方法**，全档映射未逐一对应）· `ReceiveAttackAnim`（`GetUnitSize→GetPushBackFactor`+归一化+`ReplaySkipAnims`；⚠️ 我方 `CardFeel.PushBackMagnitude` **已实现 pushback 系数**（R10 2026-10-23 坐实），**逐格系数值未对**）。

---

## 三、方法学 & 证据

- ①：脚本读 `bundle_battleprefabs_vfxandmisc_assets_all` 的 Transform/RectTransform 树 + 57 个 NFG 家族 MonoBehaviour，逐根展开 `m_Children`；抽 `TraitIconContainer Right 3` 全树见 §一。
- ②：`decomp_full` 逐个 `.c` 判「可读 / 匿名桩丢体 / 缺失 / 空」；对可读的 3 条（`GetAnimTransform`/`ReceiveAttackAnim`/`ActivateIconAnim`）读了体并与我方方法（`AnimAnchors.Resolve`/`CardFeel.PushBackMagnitude`/`TraitFrames.DisplayTrigger`）对照。⚠️ 部分被我方方法名/表格差异挡住（如 `GetAnimTransform` ≠ `AnimAnchors.Resolve` 的原版方法），保守留 B。

---

## 四、产物 & 纪律

- `判据/NestedFadeGroup_结构判定.md`：①结论 + 整树证据 + 全量统计 + 施工方向。
- `判据/B行逐条核_特效_续.tsv`：8 列 × **37 行**（18 NestedFade[B/结构缺] + 19 MECH[B；判据缺 7 / 可读未逐句 12]）。
- `账/_parts/B9_C2T7i.tsv`：11 列 × 37 行（状态列不动；只改 差异/轮次=`B9_C2T7i`/备注前缀 `[C2T7i 2026-10-27]`，原备注保留）。
  - 校验：`merge_ledger_parts.py --dry --only B9_C2T7i` → **更新 37 · 新增 0 · 无变化 0 · 0 报错**；纯 LF、无裸 CR、全 11 列。
  - ⚠️ `check_patch_conflicts.py` 报 **2 条跨补丁重复键**，但**是别人的补丁**（`B9_B2AU2.tsv`×`B9_E4BDR2.tsv` 的 `ActiveSkillDisplay.SetupActiveSkill` / `CardScript.GetCurrentLocalizedAbilityDescription`）—— **我的 `B9_C2T7i` 无冲突**（未在列表里）。那两条请按「先输后赢」排序合。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。

## 五、下一批入口（②）
19 条里 **7 条判据缺**：读 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs` 与 `CardScript.cs` 的 `ResolveUnitShake/ResolveAnimInfoRangeAttack/ResolveTraitAnim/ResolveAttackRangedAnim/StopHandAnim` 等真体（ILSpy 有体），或 VA 反汇编 `_d__405__MoveNext`。**12 条可读**：逐句读对应 `.c` 与 `BattleDriver.Play*` / `TraitFrames.DisplayTrigger` / `CardFeel.*` 对照。本轮只做到「分类 + 3 条初对照」，未深入，如实记。
