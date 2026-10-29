# B97_FXC — 执行代理报告（第九十七会话 · 波 1，2026-10-29）

> 任务：清零 `账/特效动画.tsv`（5 B + 1 C）· `账/收藏.tsv`（2 B）· `账/战斗界面_HUD.tsv`（1 B）= **9 条**。
> 判据来源 = `判据/B96_小域19_分诊.md`（先读）+ 回源三份（`decomp_full` / `assets_full` / 本仓源码）。
> 红线：⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改两份正本 · ⛔ 未碰别人的文件。

---

## 一、一句话结论

| 结果 | 条数 | 账行 |
|---|---:|---|
| **→ A（本轮真落地）** | **2** | `特效动画:403` · `特效动画:524` |
| 保持 B/C（**有根因，且根因在别人的文件 / 是口径决定 / 是结构批**） | **7** | 195 · 492 · 503 · 521 · 317 · 318 · 364 |

**为什么不是 9**：其中 **3 条**（503 / 521 / 364）的**非做不可的那一半**落在别名下或需要跑 Unity 验收；**2 条**（317 / 318）是**账的档位口径**问题（B96_L4 已明写「⛔ 别派人补实现」，要调度台二选一）；**2 条**（195 / 492）的残差本身就是另一条账的**下游**（195 等 521 的投影；492 等 `UnitTweenSO` 的数据列）。
⇒ 本代理把能在自己名下做完的**全部做完**，其余逐条给了**可直接照做的补丁或施工单**（见 §四）。

---

## 二、本轮改动清单（文件级）

| 文件 | 改了什么 | 行数变化 |
|---|---|---|
| `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs` | **类尾追加** `SetMatCap(Texture2D, Texture2D)` + `MatCapProp` / `CardImageProp` / `MatCapSlotsWritten` / `MatCapNoSlotCalls` / `ResetMatCapDiagnostics` | **+68 行（全在类尾，无账落点被推动）** |
| `CardPresentation/Battle/BattleDriver.cs` | ①`:25079` 接住 `Play(...)` 返回值（**同一行**） ②`:25090-25092` 注释就地改写（零行数） ③`:25093-25099` 旧的「不换材质」块 → 材质交接调用（**7 行 → 7 行**） ④`:35474-35475` 那条字段注释就地改写（零行数） ⑤`:16949` `EnsureTutorialOverlay` 里**同一行**追加 `SetupCursor` 调用 ⑥ 类尾追加 `SwarmMergeMatCapWrites` / `ApplySwarmCardMaterials` | **+32 行（全在类尾，≤ 38050；账里对 `BattleDriver.cs` 的最大引用是 `:37228` ⇒ 零漂移）** |
| `CardPresentation/Battle/TutorialOverlay.cs` | ①`:1529-1533` 那句「只有 Space Marine 一对导进去了」**已过期**，就地订正（**5 行 → 5 行**）② 类尾追加 `CursorArts`（11 条）/ `CursorArtForTest` / `CursorArtCountForTest` / `SetupCursor(int army)` / `_cursorGlowWarned` | **+82 行（全在类尾；账里对该文件的最大引用是 `:1585` ⇒ 零漂移）** |
| **新建** `CardPresentation/Editor/B97_FXC_Assert.cs`（+`.meta`） | 15 条断言（§三） | 新文件 |

**零行数变化**的地方都验过：改完 `\r\n`/`\n` 计数与改前一致；`BattleDriver.cs` 38085 行 = 38053 + 32（全部来自类尾），中间行号**全数未动**。

---

## 三、断言（`B97_FXC_Assert.cs`）

| 组 | 条 | 钉的是什么 | 灭自证 |
|---|---:|---|---|
| **A（账 524）** | 10 | 11 条表的命中/不命中（120/130/0/-1 全不命中）· 只有 `DarkAngels` 无 Glow · **21 张图真的在 `Art/ui_menu/`** · 图名都以 `Cursor_` 开头 | 删任意一张 `Cursor_*.png` 的 import ⇒ A4 红；给 120/130 硬塞一条 ⇒ A2 红 |
| **B（账 403）** | 15 | `SetMatCap` 写进 1 个槽 · `_MatCap`/`_CardImage` 两格都读到真图 · 换成了**新材质实例** · 无该槽的 shader 上返 0 且被计数 · 空入参返 0 不炸 · 四档 matcap 是四张不同的图 | 把 `new Material(src)` 改成直接写 `src.SetTexture`（污染 binder 共享缓存那份）⇒ **B2 红**；删 `HasProperty` 闸 ⇒ B3 红 |

**宿主（调度台接）**：`Editor/BattleScene.cs:46630` 那一行**同一行**追加（零行数变化）：
```csharp
 B97_FXC_Assert.Run(); pass += B97_FXC_Assert.Pass; fail += B97_FXC_Assert.Fail;
```
位置 = 该行 `Debug.Log(P + $"=== 合计：{pass} 通过 / {fail} 失败 ===")` **之前**（与 `B94X2_Assert` / `B94Y5_Assert` 同一批）。

---

## 四、9 条逐条结论

### 4.1 `特效动画:403` `SwarmVFXController.Initialize(CardScript, Action)` → **A（本轮清零）**

**原版第三档（材质交接）判据**（三段都现读）：
1. `decomp_full/SwarmVFXController__Initialize.c:20-22`：`Renderer.GetMaterial(this+0x20, 0)` → `CardMaterialHelper.ConfigureMaterial(card, mat)`。
2. `decomp_full/CardMaterialHelper__ConfigureMaterial.c:24-58`：正文**只有两条 `SetTexture`** —— `_CardImage` = `RawCardScript.get_cardSprite().texture`（sprite 为空则跳过）、`_MatCap` = `matcapTexturesByCardTier[clamp(CardTier, 0, len-1)]`。
3. **运行时那两个槽确实存在**：本 prefab 的材质由 `WarpforgeEffectBinder` 在 `Awake` 里按**原版 shader 名**重建，`Everguild/Cards/Card Swarm Effect` 在 `WarpforgeShaderMap` 的 `UseOriginal` 白名单里（`WarpforgeShaderMap.cs:191`，原件带编译字节码在 `wf_shaders.bundle`），属性表含 `_CardImage` / `_MatCap`。
   ⚠️ prefab 里那份 `.mat` 是**降级占位**（`URP/Unlit`，两个槽都没有）⇒ **编辑器里没跑 binder 时 `HasProperty` 为 false 是正常的**（已在代码注释与账里写死，防下一人误判「判据不成立」）。
   原始材质 json 实读：`bundle_battleprefabs_vfxandmisc_assets_all/Material/Material_-4693801760186323879.json` 的 `m_TexEnvs` 同时列了 `_CardImage`(空) 与 `_MatCap`(有图)。

**落地**：
- `WarpforgeEffectPlayer.SetMatCap(Texture2D matcap, Texture2D cardImage = null)`（`WarpforgeEffectPlayer.cs:921`）：
  遍历 `GetComponentsInChildren<Renderer>(true)` → 逐槽 `HasProperty` 判定 → **复制材质**（`new Material(src)`）再 `SetTexture` → `renderer.sharedMaterials = arr`。
  🔴 **必须复制**：binder 建出来的材质走 `WarpforgeEffectBinder.Cache`（静态、键 = 材质名|shader，**全效果共用**）⇒ 原地写会把贴图串到所有同时活着的同类特效上（同 `CardView.ApplyRemnantCardMaterial` 的 `A1967` 理由）。
- `BattleDriver.ApplySwarmCardMaterials`（`BattleDriver.cs:38067`）：取数口 = `CardArt.Card3DMatcap(CardProgress.Level(v.Data.artId))` + `v.BodyArtTexture`（**与 `CardView.ConfigureRemnantMaterials:2854-2868` 同一条**）。
- 调用点 = `PlaySwarmMergeFeel` 的 `②·c`（`BattleDriver.cs:25093`），把 `Play(...)` 的返回值接住（`:25079`）。
- 旧的「**查不到判据，不换材质**」注释与 `Debug.Log` **已就地改写**（`B96` §十 第 5 条点名的那处「两份说法打架」）。

**如实仍记的三条偏离**（⛔ 别当已完成）：
1. 落点 = **存活那格**（原版挂在被并掉那张；事件只带存活格）。
2. 「藏体 1.2 s 再销毁」那半拍**无等价载体**（被并那张已 `RemoveAt`）。
3. `onFinish` 回调 与 `SafeExit` 看门狗仍缺（账 405 的 D 依据）。

**没查清 / 没做**：真机未验（本代理不跑 Unity）。`SetMatCap` 在**真机**上应写到 1 个槽（`Card 3d Swarm Merge`），若日志出现「一个槽都没写进去」⇒ binder 没解析到那个 shader（看 `WarpforgeEffectBinder.DescribeFailures()`）。

---

### 4.2 `特效动画:524` `TutorialPointer.StartPointer(...)` → **A（本轮清零）**

**残差（上一轮只剩下的那一条）= 阵营档**：原版 `BattleManager__SetupDataDependentElements.c` → `TutorialPointer.SetupCursor(deckWarlord.cardArmy)`。
判据链：
- `decomp_full/TutorialPointer__SetupCursor.c:20-60`：`GetCursorForArmy(army, out normal, out glow)`；**返回 false ⇒ 直接 `return`，一个 Image 都不换**；命中才 `pointerImage.set_sprite(normal)` + `lightImage.set_sprite(glow)`。
- `decomp_full/CursorByArmySO__GetCursor.c` / `CursorByArmySO.ArmyCursor__TryGetItem.c`：按 `ArmyCursor.army(+0x10)` 命中，`+0x18` = 本体、`+0x20` = Glow。
- **那张 SO 本身本地没解出来**，但 `bundle_armycursors_assets_all/Sprite/` 现读 **21 张**（`Cursor_<阵营>_01` / `…Glow`）⇒ **图名就是阵营名**（B96_L4 也已这么判）。
- 🔴 **就地订正一条过期记述**：账与 `TutorialOverlay.cs` 都写着「21 张里只有 Space Marine 那一对 import 进了 `Resources`」—— **不成立**。现读 `Resources/Art/ui_menu/` 有 **21 张**（11 阵营各一对；`Cursor_Dark Angels` 没有 Glow，与原版 bundle 同形）。

**落地**：
- `TutorialOverlay.SetupCursor(int army)`（`TutorialOverlay.cs:1639`）+ `CursorArts` 11 条表（`:1606`）+ `CursorArtForTest` / `CursorArtCountForTest`（`:1622`）。
- 表里没有（`Neutral(0)` / `EmperorsChildren(120)` / `SpaceWolves(130)` / 认不出的 `-1`）⇒ **`return false`、一个都不换**（= 原版 `GetCursorForArmy` 返 false 那条 `return`；⛔ 不回落成 Space Marine）。
- `DarkAngels(110)` 原版**没有 Glow 图** ⇒ 只换本体、`light` 保持序列化默认值，并**出声一次**（⛔ 不猜一张顶替）。
- 调用点 = `BattleDriver.EnsureTutorialOverlay`（`BattleDriver.cs:16949`，**同一行**追加）：`_tutOverlay.SetupCursor(ArmyIcons.Id(_myFaction))` —— 等价于原版 `SetupDataDependentElements` 起手那一跳。

**没查清**：`CursorByArmySO` 的 army↔图 对应**没有 SO 原文**（只有图名）；`Chaos_01 → BlackLegion`、`Eldar_01 → SaimHann` 这类**一对多族里的取舍**是按「原版只有 11 对、13 阵营里恰好 2 个族没有那一对图」推的。若将来解出那张 SO，**以 SO 为准**，本表要让位。真机未验。

---

### 4.3 `特效动画:503` `CardScript.ActivateTraitParticlesFromCodeInTarget(...)` → **保持 B（补丁已给，未动代码）**

**为什么本代理不写代码**：口已建（`TraitParticles.PlayInTarget`，`TraitParticles.cs:194`），**缺的是引擎侧那一格信号** —— 生产者 `RuleEngine/Core/RuleCore.cs:9749-9777` 在 `B97_RC` 名下（简报红线），单做消费端 = 死代码；且加事件会动 `ctx.Events`/`Fingerprint`，必须整批做 + 重钉基线。

**调用链自证**：
`RuleCore.KillUnit`（`huntMarks > 0` 那一段）→ 原版 `CardScript__TriggerOnMinionDeath.c:77` 调 `deadCard.ActivateTraitParticlesFromCodeInTarget(huntMark, hero)` →
`…InTarget.c:70-75` 取件用 **`this`（死者=触发者）的 `cardArmy`**、粒子挂 **`targetCard`（敌督军）** →
`ResolveTraitAnim(anim, targetCard, targetCard, targetCard)` 三锚点全是目标 →
`BattleManager._ResolveTraitAnim_d__465__MoveNext.c:112-118` 把它交给 `DelayedVFXPlay(..., actingCard, targetCard, ...)`。

**可粘贴补丁（三处，必须同一批）**：

**(a) `RuleEngine/Core/BattleEvent.cs` — 在 `EvtKind` 枚举里加一格**（放在 `Trigger` 之后）：
```csharp
        /// <summary>🆕 2026-10-29（B97_FXC · 账 特效动画:503）：**「某单位身上有几层某 trait」这一刻的快照**。
        /// 生产者 = `RuleCore.KillUnit` 的猎杀标记支（原版 `CardScript__TriggerOnMinionDeath.c:77`
        /// 的 `ActivateTraitParticlesFromCodeInTarget(huntMark, hero)`）；消费 = 表现层
        /// `TraitParticles.PlayInTarget(trait, 敌方督军, 死者, …)`。
        /// `CardId` 装 **trait 名**（同本文件既有约定：关键词放 `CardId`），`Amount` 装层数。</summary>
        TraitStacked,
```

**(b) `RuleEngine/Core/RuleCore.cs:9750-9752` — 在 `if (huntMarks > 0) {` 里、`int marks = huntMarks;` 之后插一行**：
```csharp
                    ctx.Emit(EvtKind.TraitStacked, p, slot, "huntMark", amount: marks, instanceId: u.InstanceId);
```
⚠️ **坑（本工程实测过）**：`Emit` 的第 4 个位置实参装的是**关键词本身**（不是 `e.Keyword`）—— 别按 `e.Keyword` 过滤（会数出 0）。这里同时确认 `Emit` 的 `amount` 形参名（`RuleCore.cs` 里 `Emit(…, amount: …)` 已有先例，照抄它的调用形状）。

**(c) `CardPresentation/Battle/BattleDriver.cs` — 在 `EvtKind.Death` 那一支旁加**（`PlaySignal` 的 switch 里，`case EvtKind.Death:` 之后追加）：
```csharp
                case EvtKind.TraitStacked: PlayTraitParticlesInTarget(e); break;
```
类尾追加：
```csharp
        /// <summary>原版 `CardScript.ActivateTraitParticlesFromCodeInTarget(trait, targetCard)`
        /// （`…InTarget.c:1-86`）：粒子挂**目标**、取件按**触发者**（死者）自己的卡阵营。
        /// 生产者 = 引擎 `RuleCore.cs` 的猎杀标记支（`TraitStacked` 事件，`Amount` = 层数）。</summary>
        void PlayTraitParticlesInTarget(BattleEvent e)
        {
            if (e == null || e.Player < 0 || !BoardSpec.IsValid(e.Slot)) return;
            if (string.IsNullOrEmpty(e.CardId)) return;
            var dead = ViewAt(e.Player, e.Slot);
            var hero = ViewAt(1 - e.Player, BoardSpec.WarlordSlot);   // 原版 `GetHero(死者主人的对面)`
            if (dead == null || hero == null) return;
            if (!AnimGate.CanContinue(dead)) return;
            TraitParticles.PlayInTarget(e.CardId, hero.transform, dead.transform, "huntMark×" + e.Amount);
        }
```
⚠️ `ViewAt(1-e.Player, WarlordSlot)` 的座位口径要按本仓「座位是绝对编号」确认（`NetBattle.cs:10` 已写死）；`BoardSpec.WarlordSlot` 常量名照实际。

**待接线**：上面 (a)(b) 在 `B97_RC` 名下 ⇒ **由调度台转派**。(c) 我可以随时补（`BattleDriver.cs` 本代理已持有）。

---

### 4.4 `特效动画:521` `EntityScript.GetCardEffectsToShow()` → **保持 B（S0 已做，残差在 `UnitState.cs`）**

**就地订正一条过期记述（铁律 5）**：账里「`UnitState.ApplyTraitEffect` 零调用者 ⇒ `TraitEffects` 恒空」**不成立** —— `EffectResolver.ApplyOneGain` 现调它（`RuleEngine/Core/EffectResolver.cs:12065`），另有 `:12586`（`markOfChaos`）与 poisoned 两个生产者。
**真残差 = S2 序戳 `UnitState.Seq`（全仓 grep 零命中）+ S3 三处写入点 + S4 投影/消费（`CardDisplayWindow.RowsOf`）**。
⛔ **本轮未动代码**：`UnitState.cs` 归 `B97_US`（简报红线），且会动引擎事件/指纹。

**待接线**：施工细则 §三 的 S1–S7 是现成施工单（S1 `EffectsToShow.cs` 已完成、S0 已完成）⇒ 下一批只需 **S2/S3 + S4**，建议派给 `UnitState.cs` 的写手，或调度台把它与 `EffectsToShow.cs`（无主）一并给我。

---

### 4.5 `特效动画:195` `CardEffectItem.LoadEffect(...)` → **保持 B（本行自身零可做项）**

复核结论：①（两掩码今天恒 false）· ③（`DisplaySourceName`）· ④ 的 `what = ""`（mark 支原版就是空串）**均已落地**；**残差只剩可达性** —— `TempBuff.buffType ∈ {8,17}` 今天**无生产者**（`EntityScript.CreateMarkCardEffect` 的数据形状在 `RuleEngine/Core/EffectResolverPartial_B88E2.cs:347`，**零调用点**）。
⇒ 它是 `特效动画:521` 的**下游**：521 的 mark 投影把行灌进 `TempBuff` 后自动可达。**本行不需要单独的人**。

---

### 4.6 `特效动画:492` `BattleManager.ResolveAnimInfoRangeAttack(...)` → **保持 C（唯一残差 = `UnitTweenSO` 数据列）**

复核逐条（`_d__468__MoveNext.c:1-149` 现读）：

| 步 | 原版 | 我们 | 判定 |
|---|---|---|---|
| ① | `WaitForSeconds(startDelay(+0x28))` | `ScheduleAction(vfxAt + shkPlace.StartDelay)` | 已照 |
| ② | `DelayedVFXPlay` 起协程后**同帧** `screenShake`（`CanPlay` → `Play`） | `if (e.Ranged)` ⇒ `PlayBeatShake` | 已照 |
| ③a | `animInfo+0x78` 的 `List<AnimatorClipInfo>` ⇒ 逐条 `ResolveUnitShake` | `SchedulePieceUnitShakes` / `ScheduleUnitShakes` | 已照 |
| ③b | `animInfo+0x80` 的 `UnitTweenSO` ⇒ `BuildSequence` +（`so+0x18` 为真时）`WaitForCompletion` | **无** | ⛔ **残差** |
| ④ | `ResolveAnimAnimationClip` | `Card3DAnimationController` | 已有等价 |
| ⑤ | `waitForAnim` ⇒ `WaitForSeconds(GetAnimDuration())` | **以「延长时间轴队尾」实现**：`EventTiming.AttackVfxTailAt` + `CardAnimClips.RangedChainDuration`（用逐件 `AnimInfo.GetAnimDuration()` 真值） | 等价（⛔ 别再补一条 `WaitForSeconds`，会等两遍） |

**为什么没做**：`Resources/cardanim_clips.json` 没有「AnimInfo → `UnitTweenSO` 名」这一列，且**`UnitTweenSO__BuildSequence` 的方法体本地还没逐行读过** ⇒ 不知道它 tween 的是哪个 transform 的什么属性，照做只能猜。
**施工单（三步）**：① `工具/gen_cardanim_clips.py` 加一列（`UnitTweenSO` 名 + `waitForCompletion` 位）；② 重生成 `Resources/cardanim_clips.json` + 工作副本；③ `BattleDriver.ScheduleAttackPresentation` 在 ③ 的 else 支排等价 Sequence。**做之前必须先读 `decomp_full/UnitTweenSO__BuildSequence.c`**。

---

### 4.7 `收藏:317` / `收藏:318` → **保持 B（口径决定，⛔ 无代码可做）**

复核：两条的等价实现**已内联在真实方法里** —— `DeckEditorState.cs:568 VisibleCards()` / `CollectionWindow.cs:2074 FilteredCosmoNames()` / `CollectionWindow.cs:2857 ApplyFilter()`。**没有任何接线/字段/分支缺**。
挡住 A 的**只有账的档位口径**（原版是 lambda，B95 定的口径要求「落点须能指到真实同名方法」）。
**要调度台二选一**：(a) 放宽口径 ⇒ 这 2 条 + `账/泛型与启动链_补读.tsv:137/138` 共 **4 条**一起升 A；(b) 维持 B。
⛔ **别为凑 A 建一个没人调的空壳方法**（那才是死代码）。

---

### 4.8 `战斗界面_HUD:364` `ChooseCardMenu.AutoProcessDone()` → **保持 B（录像结构批）**

复核**仍未做**，理由**不是「困难」**：前置件 = 录像头必须带**候选集**。我们的回放是纯引擎重放（`MsgAction.picks/pickIds` 直接喂 `ctx.ChoosePicks`）⇒ **面板根本不开**，`Setup` 的 Replay 支结构上走不到。
要复刻得**一场三改**：① `ReplayStore.ReplayRecord` 加 `askCands`（每次 ask 的候选 id + 选中下标，按动作序对齐）② 写点 = `ShowAsk`（真摆出候选时快照，纯新增字段、`version` 不用 +1）③ 回放侧 `ApplyReplayActionAt` 在第 i 条**之前**摆面板 + `SelectCard(target)` + 2 s 后 `ProcessChooseDone` 等价收口。
⚠️ `ReplayRecord` 与联机/自检共用 ⇒ 要连带 8 条以上回放断言与 `TickReplayPump` 分叉位一起改，**而本代理不跑 Unity** ⇒ 改完无法验、且会推翻「回放不产生面板输入」这条现有不变量。
⇒ 需调度台按「录像结构批」（`ReplayStore` + `BattleDriver` 回放泵 + `CardChoicePanel`）一次性排期。

---

## 五、账补丁

`账/_parts/B97_FXC.tsv` —— **11 列 · 首行表头 · `方法` 列带完整签名 · 9 行**（LF）：
`403=A` · `524=A` · `195=B` · `492=C` · `503=B` · `521=B` · `317=B` · `318=B` · `364=B`。
`python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py` 现跑：**✅ 没有跨补丁重复键（1 份补丁）**。

---

## 六、待接线 / 需要调度台动作

1. **断言宿主**：`Editor/BattleScene.cs:46630` 同一行、`Debug.Log(… 合计 …)` 之前追加 `B97_FXC_Assert.Run(); pass += …; fail += …;`（零行数变化）。
2. **`503` 的 (a)(b) 两处补丁**（`BattleEvent.cs` / `RuleCore.cs`）—— 归 `B97_RC`，需转派或授权。
3. **`317`/`318` 的口径二选一**（4 条一起升 A 还是维持 B）。
4. **`521` 的 S2/S3**（`UnitState.cs`）与 **`492` 的 `UnitTweenSO`**（工具线 + 先读 `.c`）—— 请排进下一波。
5. **`364` 的录像结构批**。

---

## 七、没查清 / 边界

- **真机未验**（红线：本代理不跑 Unity）。`403` 的 `SetMatCap` 在真机上是否真写到 1 个槽、`524` 换图后画面是否正确 —— **都只有断言级证据**。
- `CursorByArmySO` **SO 原文本地没解出来**；`524` 的表是按图名落的（见 §4.2 的风险说明）。
- `UnitTweenSO__BuildSequence` 的**方法体没读**（`492` 所以没动）。
- `503` 补丁里 `ctx.Emit` 的 `amount:` 形参名要按 `RuleCore.cs` 既有调用形状现读一次再用（我按同文件既有先例写的）。
- 本代理**只写了** `账/_parts/B97_FXC.tsv` 这一份账补丁，**没碰主表**。
