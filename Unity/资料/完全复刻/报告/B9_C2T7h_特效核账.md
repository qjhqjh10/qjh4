# B9 / C2T7h —— `特效动画.tsv` B 行逐条核（73 条）

> 查判据型子代理（只读 + 落账补丁）。首次系统核这份。用**方法名**钉边界。
> 产出：`判据/B行逐条核_特效.tsv`（8 列 · 73 行）· `账/_parts/B9_C2T7h.tsv`（11 列 · 73 行，**只改 差异/备注/轮次**）· 本报告。

---

## 一、结论速览

- **73 条：建议 A = 10 · B = 63。**（无「落点失效」——落点均指向我们真实文件。）
- **10 条升 A**（此前 J4FX / B8_AS 的「订正」已把缺口补上、且落点是真方法级）：
  | 方法 | 依据 |
  |---|---|
  | `AnimFxModuleMoveParticlesToTarget.GetTarget(CardScript)` | 三条空路抛异常那支已核；target 由调用方给（B8_AS 订正） |
  | `AnimFxModuleMoveParticlesToTarget.Initialize(...)` | SetTarget 相机换算已实现（UpdateTargetPosition，B8_AS 订正） |
  | `CameraVerticalFramer.CalculateFraming(...)` | `CombatAutoZoom.CalculateFraming(:445)` + `MeasureCardAreaGapWorld(:754)` 两半取景数学在（J4FX 订正） |
  | `CameraVerticalFramer.ResolutionHasChanged()` | `BattleCameraSreenSize.ResolutionHasChanged→Initialize(true)`（J4FX 订正，非缺口） |
  | `CameraVerticalFramer.Start()` | `BattleCameraSreenSize.Start→RegisterResolutionSignal()`（J4FX 订正，等价物在） |
  | `CardAnimController.RotateAnim(...)` | `BattleDriver:18443` 朝向落点两支都已实现（J4FX 订正推翻旧缺口） |
  | `CardEffect.IsEnchantment()` | `CardDef:5876 + Enchantment:163 IsEnchantmentBuffType`（已实现） |
  | `CardEffect.IsSameEffect(...)` | `EffectResolver.IsSameHandEffect(:5663)` 逐字段比（已实现） |
  | `CardEffect.SameIsPlayerEffect(...)` | `EffectResolver.IsSameHandEffect(:5681)` ActingUnit 引用相等覆盖 |
  | `CardEffect.SameTimingAsEffect(...)` | `Expire(五档 until*)+ExpireTurn` 覆盖（`:5684`） |

  ⚠️ 这几条**不是我新做的**，是**此前 J4FX/B8_AS 已订正、但状态一直停在 B** —— 本轮核出「落点真方法级 + 差异原文写已实现」⇒ 升 A。

---

## 二、🔑 63 条 B 的「数据在但没接」 vs 「数据本身缺」（你要的那个区分）

| 类别 | 条数 | 含义 / 后续动作 |
|---|---|---|
| **机制在、未逐条核** | 19 | 我方有对应演出方法（`PlayAbilityBeat`/`PlayHitFeel`/`TraitFrames.DisplayTrigger`/`AnimAnchors.Resolve`/`Card3DAnimationController.DoAnim` 等），**数据/机制都在**，只是没逐句对细节（多目标序、体型→系数表、clan 弹道表、skipSound 档）。⇒ **动作=逐条比细节**，不是补数据。 |
| **等价但落点非方法级** | 11 | 差异原文写「一致/等价」，但落点是**共用段/调用点/无同名方法**（如 `ReplaySkipAnims`→`Update` 调用点、`HasToUseLegendarySummonEffect`→`PlayAutoPlayCenterDisplay` 共用段、`CanContinueAnim`→`PlayHealFeel` 调用点、`CardEffectItem.*`→`CardDisplayWindow:851` 一段区域）。⇒ **动作=补方法级落点即可，勿硬升 A**（按你的口径）。 |
| **无落点（结构面/数据缺）** | 17 | `NestedFadeGroup*` 一整族（`AddMissingBridgeComponents/OnAlphaChanged/SubscribeToParent/...`）。【G12 现核】**组树不嵌套**（9 个组根全在同一 prefab），⇒ 原版「嵌套淡入淡出组」这套机制**在我们结构里根本不触发**。⇒ **动作=结构面问题（非「接线」）；若确认原版也非嵌套⇒可判 D（机制不适用，时间限定=结构不变）；若原版真嵌套⇒是「我们结构缺嵌套」（数据面要补）**。⛔ 不是升 A 的料。 |
| **真偏离 / 设计差异** | 6 | `不同：`（载体合一 / 粒度逐图标 vs 按块 / 无订阅对 / 换牌回牌库另一路）。⇒ **动作=确认是否接受该差异（铁律18 自己设计）或补齐**。 |
| **其他（订正带残余）** | 10 | 差异是 J4FX/AB 的订正 + 仍列残余（如 `SwarmVFX.OnAnimationEndEvent`：「5 秒尾巴已补，**仍差** onFinish 回调/…」）。⇒ **动作=按残余逐条补**。 |

**一句话**：这个域的 B **绝大多数是「机制/数据在、但落点没到方法级或细节没逐句核」**（19+11+10≈40 条），**不是数据本身缺**；真正「数据/结构缺」的只有 `NestedFadeGroup` 一族 17 条（组树不嵌套）与 6 条设计差异。

---

## 三、方法学 & 证据

- 机械核：73 条落点逐个解析 `File.cs[:line]（Method）`，`os.path.exists` 全 OK（无文件不存在）。
- A 判据：只对「落点是真 .cs 方法 + 差异原文（J4FX/B8_AS 订正）写『已实现/不是缺口』」的 10 条升 A；**data/字段/共用段落点一律不硬升**（按你的口径）。
- 读过：`CombatAutoZoom.CalculateFraming`·`BattleCameraSreenSize`·`CardDef:5876`/`Enchantment:163`·`EffectResolver.IsSameHandEffect:5663/5681/5684`·`WFModuleMoveParticlesToTarget`·`BattleDriver` 多处落点·`TraitFrames.DisplayTrigger:921`·`AnimAnchors.Resolve:154`·`Card3DAnimationController.DoAnim:208`·`CardDisplayWindow:851`·`CardView:7812`。

---

## 四、产物 & 纪律

- `判据/B行逐条核_特效.tsv`：8 列 × **73 行**（A=10/B=63，每行带「一句结论（含 数据在没接/数据缺 标签）+ 还差」）。
- `账/_parts/B9_C2T7h.tsv`：11 列 × 73 行（表头逐字抄主表；**`状态` 列不动**；只改 `差异`（追加 `｜[C2T7h 2026-10-27] …`）/ `轮次`=`B9_C2T7h` / `备注` 前缀 `[C2T7h 2026-10-27]`，原备注保留）。
  - 校验：`merge_ledger_parts.py --dry --only B9_C2T7h` → **更新 73 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → 无跨补丁重复键；纯 LF、无裸 CR、全 11 列。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。
