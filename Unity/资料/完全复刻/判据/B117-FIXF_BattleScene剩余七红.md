# B117-FIXF · `BattleScene.Run` 剩余 7 红（诊断 + 修复）

> 输入：`d:/4/_tmp_view/battle.log`（`BT === 合计：10124 通过 / 7 失败 ===`，2026-11-02 14:39）。
> 红线遵守：⛔ 未跑 Unity 批处理 · ⛔ 未动 git · ⛔ 未改 `Core/CardView.cs`。
> 类型检查：`TMPDIR=/tmp/wf_fixf bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

## 〇、七条逐条判定

| # | 断言 | 判定 | 一句话 |
|---|---|---|---|
| 1 | `★ 换环境时旧实例先进入淡出（FadingCount=0）` | **β 实现缺陷（本波带着的）** | 撤旧实例用的 `BlendDuration` 是**上一次** apply 留下的值；上一次是 `instant:true` ⇒ 它是 **0** ⇒ 每个 blendable **同步回调** ⇒ `pending` 当拍归零 ⇒ 旧实例立刻销毁。 |
| 2 | `★ D6⑦（真实材质队列）… 违规 23 颗` | **γ 本波回归（`B117-G10-17` 的连带）+ β（`CardView` 那一侧）** | 「眼睛」现在会把**卡体本身** `SetActive(false)→(true)`（新行为）⇒ 卡上 7~8 颗 TMP 走 `OnEnable → UpdateMaterial` ⇒ 材质退回**没盖过章**的那份（queue 3000）。 |
| 3 | `★ 新账-83（灭自证）：非线性 easeCurve 4 件` | **α 断言过期（预期的连锁）** | `B117-WCURVE` 把曲线烘成 4 数格式（带切线）后该值 1 → **4**；判据文件 §五 已点名要改 `<= 3`。 |
| 4 | `★ B1·残余②（前提）：闸起来了` + 同组 Frozen | **α 夹具过期** | `B117-38` 把开闸判据改成 `BatchBlocksPlayerActions`（`Trigger` **不再开闸**，那正是本批要的「手感杀手 #2」修复）⇒ 夹具还在用一条裸 `Trigger` 当前提。 |
| 5 | `★ B2·偏差8：竞技场空间里那 0.2 s 落下真的在走（0.5 s → 0.6 s 位移 0.000）` | **α 取样时刻过期** | `B117-R15` 把交接 0.40 → **0.95**、落下段变成 **1.00→1.20 s**；旧取样 0.6 s 落在**悬停段**（本来就该不动）。 |
| 6 | `B117G1024 ✗ E0 …（实得 0 个）` | **α 接线位置错** | 它接在 **`CheckSavedScene` 之后**，那一句 `EditorSceneManager.OpenScene` 已把测试场景整个换掉 ⇒ 运行时实例化的战场随之销毁 ⇒ 找不到任何 `TapHitMaterialDefinition`。 |
| 7 | — | —— | 日志里只有这 7 条（无第 8 条）。 |

## 一、逐条根因与改法

### 1 · 换环境：旧实例当拍销毁

**判据（第一权威）** `d:/2/tools/decomp_full/ScenarioEnvironmentConditionSO__DisableEnvironment.c`：

```
uVar10 = (param_3 == 0) ? *(float*)(param_1 + 0x18)   // 旧 SO 的 blendTime
                        : 0;                          // param_3 = 本次 apply 的 instant
ScenarioBlendOptions___ctor(uVar7, uVar10, 0 /*direction=false*/, onComplete, filterOptions, 0);
```

而 `ScenarioEnvironmentConditionsManager__ApplyEnvironment.c` 是 `DisableEnvironment(manager.CurrentEnvironment /*旧 SO*/, manager, param_3 /*新的 instant*/)`
⇒ **撤旧的时长 = 本次 apply 的 instant × 旧 SO 的 blendTime**。

**我们的实现（改前）**：`EnvironmentApplier.BeginFadeOut` 用字段 `BlendDuration`，那是**上一次** `Apply` 写完留下的值
（本波 `B117-G10-02a` 把它改成 `BlendTimeFor(instant, …)` ⇒ 上一次是 `instant:true` 时它就变成 **0**）。
`ScenarioParticleSystemBlender.DoScenarioBlend` 有 `if (!_waitNoParticles && _left <= 0f) Complete();`
⇒ duration 0 ⇒ **同步回调** ⇒ `f.pending` 在循环里就被减到 0 ⇒ `if (f.pending == 0) FinishFadeOut(...)`
⇒ **当拍销毁**、`_fading` 里一条都没有 ⇒ `FadingCount == 0`。

**改**（`CardPresentation/Battle/EnvironmentApplier.cs`，**行数零变化**）：
`BeginFadeOut(..., float dur)` 新增一个形参；调用点传
`BlendTimeFor(instant, CurrentItem != null ? CurrentItem.blendTime : 0f)`（`CurrentItem` 此刻仍是**旧**那条，赋值在它之后），
`timeout` 与每个 blendable 的 `duration` 都改用 `dur`。

**为什么这条断言现在才会红**：本波之前 `BlendDuration = it.blendTime`（不看 instant）⇒ 即使上一次是 instant，值也是 4.0。
本波把 instant 折进 `BlendDuration`（对 `EnableEnvironment` 那一半是**对的**）时，顺手把撤环境那一半也带坏了。

**本夹具的具体值**（`Resources/EnvironmentConditions.json`）：旧 SO = `Environmental Condition Ultramarines Orbital Bombardment`，`blendTime = 4.0`
⇒ 撤旧时长 4.0 s ⇒ `FadingCount == 1` ⇒ 推 600 s 后回调到齐 ⇒ 两条都绿。

### 2 · D6⑦：23 颗渲染器落回 3000

**这是本波回归，已定位到具体那一下**：`BattleScene.cs` 的「眼睛」那一小段（`:29225/:29234`）
先按一次、再按一次 `drv.SimulateMulliganEye()`。

`B117-G10-17` 把 `MulliganPanel.SetContentHidden` 从「收卡钮 + 钮上字 + 压暗层」扩成原版的四组，
**其中多收了「卡体本身」**：`_cards[i].gameObject.SetActive(show)`。
卡体一 `SetActive(false)`，它子树里每个 `TextMeshPro` 都 `OnDisable`；再 `SetActive(true)` 时
`TextMeshPro.OnEnable`（`com.unity.ugui@…/Runtime/TMP/TextMeshPro.cs:630-668`）最后一句是 `SetAllDirty()`
→ `SetMaterialDirty()` → `UpdateMaterial()`，而 `UpdateMaterial`（同文件 `:307-320`）是：

```
if (m_renderer.sharedMaterial.GetInstanceID() != m_sharedMaterial.GetInstanceID())
    m_renderer.sharedMaterial = m_sharedMaterial;      // ← 把渲染器材质换回「TMP 记着的那份」
```

问题在 `CardView` 那一侧：`StampTmpQueue` → `StampQueue` 只写 **`renderer.sharedMaterial`（队列克隆）**，
**没写 TMP 内部的 `m_sharedMaterial`**（还是字体资产那份，`m_CustomRenderQueue = 3000`）
⇒ 只要 TMP 被**重构一次**（`OnEnable` / 字体面切换），队列就**静默退回 3000**，低于压暗层 3006。
实测症状：按两次眼睛之前 `D6⑦ = 3007..3007 / 0 违规`，之后 `3000..3007 / 23 违规`（3 张卡 × 7~8 颗 TMP）。
旁证：今晨 07:48 那份 `_tmp_view/b117_battle.log` 里这条是 **✓ 违规 0 颗**（那时 `SetContentHidden` 还不收卡体）。

**改（本次）**：
* `MulliganPanel.SetContentHidden`：**放开卡体之后立刻 `_cards[i].SetQueue(QMulliganCard)`**
  —— 与同文件 `ReapplyRowPoses` 早就写的「每一轮都重设队列（`HandLayout.Place` 会把它改回手牌号段）」同一口径。
  改动点在 `:1932` 之后（账对 `MulliganPanel.cs` 的最深落点 = `:1550` ⇒ **零漂移**）。
* `BattleScene.cs` 的 D6⑦ 断言：**把违规件逐个打出来**（`#卡下标/层名=队列`），TMP 那几颗再标一句
  「TMP 记的材质 ≠ 渲染器材质 ⇒ 下次 OnEnable 会把队列刷回」。上一版只有「违规 N 颗」，只能猜。

**还没修的更深一层（判据交给 `Core/CardView.cs` 的写手，⛔ 本代理不碰那个文件）**：
`CardView.StampTmpQueue` 应该在 `StampQueue(mr)` 之后把 TMP 的内部材质也同步过去，例如
`if (t.fontSharedMaterial != mr.sharedMaterial) t.fontMaterial = mr.sharedMaterial;`
（`fontMaterial` 的 setter 就是写 `m_sharedMaterial` + `SetMaterialDirty` ⇒ 之后任何 `OnEnable` 都不会再把它刷回 3000）。
不修它的话，**任何**「把卡上文字层收起再放开」的路径（手牌 → 场上换面、`SetData` 重建数值行…）都会复现同一症状，
面板层的 re-stamp 只是把换牌这一处补上。

### 3 · 新账-83：非线性 easeCurve 1 → 4

判据 `判据/B117-WCURVE_动画曲线斜率.md` §五 已把这条点名：`B117-WCURVE` 把非线性曲线改烘成
**4 数格式（带 in/out 切线）**后，`EaseAt(·, 0.5) != 0.5` 的 (卡,件) 从 **1 → 4**
（`Atk_Deathspinner_arc_alt` 0.6469 · `Atk_JainasMor_Throw_Return` 1.0076 · `Atk_Rokkit_Harpoon small` 0.75 · `Atk_StikkaThrow` 0.75），
同笔复算 `movers83 33 / ao83 26 / otv83 22 / arc83 21` **全部不变**。
⇒ 断言上界 `<= 3` 改成 **`<= 8`**、文案「现读 1 = Jain Zar」→「现读 4（四个名字）」。**语义没放宽**：
仍然同时钉住「⛔ 不是所有曲线都被当成非线性」（33 个 mover 里只有 4）与「⛔ 不是一条都没有」。

### 4 · B1·残余②：夹具用裸 `Trigger` 开闸

`BattleDriver.PlaySignals` 现在的开闸口是
`if (BatchBlocksPlayerActions(_signalBuf)) BeginBlockingWindow(t);`（账 `B117-38`，2026-11-03），
`BlocksPlayerActions` 的判据 = `BattleManager__WaitingForBlockingAction.c:26-40`（`transformUnit` 或动作带 `blockPlayerActions`）
⇒ **`Trigger` / 普通 `Draw` / 日志类不再锁输入**（本批有意改掉的「手感杀手 #2」）。
而 `B1·残余②` 的夹具还在用 `bctx.Emit(EvtKind.Trigger, …)` 当「演出没演完」的前提 ⇒ 闸根本没起来（第一红），
后面那条 `interaction.FrozenNow` 也就跟着红。

**改**：夹具改成 `bctx.Emit(EvtKind.Hit, bme, bws, bUnit.Name, null, null, 1)`
（`BlocksPlayerActions(Hit) = true`，时长 `HitHoldOf` = 1.0 s；本段上面「B1·偏差4」早就在同一条链上用过 `Hit`）。
两条断言的**被测对象一个字没变**：仍是「闸真起来 ⇒ `interaction.FrozenNow` 真 ⇒ 放行后真恢复」。
🧨 灭自证仍成立：把 `interaction.Frozen` 改回 `() => PlayerInputFrozen` ⇒ 第二条照旧红。

### 5 · B2·偏差8：取样落在悬停段

`B117-R15` 之后 `DeploySequence` 的五段是：飞 0→0.3 · 悬停 0.3→0.95（clip）·
**交接 0.95** · 等 0.05 · **落下 1.00→1.20** · 尾拍 1.20→1.30（`TotalTime = 1.3`）。
旧夹具在 **0.5 s 与 0.6 s** 采样「落下段还在走」⇒ 两拍都在**悬停段**（卡停在上方正等 clip），位移必然 0.000。
（另一条「0.2 → 0.5 位移 0.252」是飞行补间的尾巴，仍然成立，只把文案「落地段」改成「飞行尾段」。）

**改**：第二个取样点 `Step(0.1f)` → **`Step(0.6f)`**（0.5 → **1.1 s**，落下段 OutQuad 已走 75%），
取中点而不是 1.0/1.2 是为了容 ±0.05 s 的起播抖动；比较对象 `fLate`（0.5 s 的悬停位）≈ 落下起点，语义不变。
顺带把后面那句「推到落地（0.65 s）」的过时注释改成 **1.20 s**（循环本来按「账清掉」推，不受影响）。

### 6 · B117G1024 E0：跑在 `CheckSavedScene` 之后

`B117G1024Assert` 的 E 组要「**运行时实例化**的战场」（`ArenaRuntimeLoader` + `Resources/ArenaPrefabs/*`）。
它原来接在 `RunCore` **末尾那条聚合行**（`:46962`），而 `CheckSavedScene(tail)` 在 `:46747`
—— 那一句 `EditorSceneManager.OpenScene(ScenePath)` 会把**当场建的测试场景整个换掉**，
运行时实例化的 `Warpforge_battlearena1` 随之销毁 ⇒ `FindObjectsByType<TapHitMaterialDefinition>` = **0**。
日志顺序可复核：`[Battle] §27 战场：battlearena1 … 第 28 次载入`（`:1042894`）→ `CheckSavedScene`（`:1047721`）→ **E0 红（`:1108989`）**。

**改**：把 `B117G1024Assert.Run(); pass += …; fail += …;` 从末尾聚合行**上移到 `Debug.Log(P + $"=== 结束…")` 那一行**
（`CheckSavedScene` **之前**，同一物理行追加 ⇒ 那两行行数不变）。它不依赖 `driver`（自带临时件 + 静态常量组），
`DestroyImmediate(driver)` 之后也安全。战场 prefab 实读：`battlearena1.prefab` 里该脚本 **20 处**、宿主层按
`ArenaBuilder` 末尾整树刷成 `ArenaLayer = 8`，而 `TapParticleController.CollisionMask = 0x40000D01` 含 bit8
⇒ **E0 应为 20、E1 应可打**。

## 二、应转绿的清单

| 断言 | 预期 |
|---|---|
| `★ 换环境时旧实例先进入淡出` + 紧随的 `★ 回调到齐后旧实例被销毁` | ✅ |
| `★ D6⑦（真实材质队列）`（`违规 0 颗`） | ✅（靠 `MulliganPanel` 放开卡体后重盖队列） |
| `★ 新账-83（灭自证）：非线性 easeCurve 4 件` | ✅ |
| `★ B1·残余②（前提）：闸起来了` | ✅ |
| `★ B1·残余②：演出没演完 ⇒ 手牌层禁操作` + 灭自证那条 | ✅ |
| `★ B2·偏差8：竞技场空间里那 0.2 s 落下真的在走` | ✅ |
| `B117G1024 ✗ E0`（并顺带 E1） | ✅（E1 取决于上表第 6 条的「宿主层已刷 8」结论，若仍红会打印宿主层分布） |

⇒ `BattleScene.Run` 的 `7 失败` 预期归零。

## 三、风险 / 仍可能红的

1. **D6⑧⑨ 像素那两条**（同段）本波没动；若第八条随 D6⑦ 的修复一起变，会另报。
2. **`B2·偏差8` 的 1.1 s 取样**是「按 `DeploySequence` 常量推的」，与真机帧率无关（批处理里 `CardTween` 手推）
   —— 若下一次日志里仍读到 0.000，说明 tween 的**起播时刻**比夹具假设的晚 ≥0.1 s，那时应改成
   「先 `while (AiDeployLandingCuePending…)` 之外再探一次载体换体时刻」而不是继续猜偏移。
3. **账的落点漂移**：`BattleScene.cs` 本次 ≈ **+40 行**（都在断言/注释里）⇒ 该文件被账按行号引用的 **8 条 A 行**
   （`:32098 :36750 :37609 :38804 :41355 :45243 :46873 :46895`）会**漂移**（不是失败）。
   等「没有 `.cs` 写手」时由调度台跑一次 `check_ledger_points --all-A --fix` 即可。
   `EnvironmentApplier.cs` / `MulliganPanel.cs` 本次**零行数变化 / 改动在账最深落点之后** ⇒ 不漂。
4. ⚠️ **文件所有权**：`资料/完全复刻/文件所有权_当前.md` 里 `Editor/BattleScene.cs` 标着 **`B117-WBT`（独占）· 在飞**。
   本件是按调度台的指令改该文件里的断言；若 `B117-WBT` 仍在写同一文件，**合并要小心**（同一物理行追加）。
5. `CardView` 那一侧的更深缺陷（TMP 内部材质未同步）**未修**（白名单外），已在第 2 条里给了补丁建议。
