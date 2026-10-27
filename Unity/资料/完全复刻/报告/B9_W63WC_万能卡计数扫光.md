# B9_W63WC · 万能卡计数条的 `UpdateCounters` 扫光（信号那一跳）＋ `MessageContainer.curve` 换成原版 3 键

> 执行代理 `B9_W63WC` · 2026-10-27 · 有写权限
> **全程未跑 Unity · 未动 git · 未改正本/账主表/别人的报告/基线** · 白名单外一个字节没动
> 必跑两件都过了：`TMPDIR=/tmp/wf_w63 bash 工具/typecheck.sh` → **0/0** ·
> `bash 工具/ruleprobe.sh check` → **rc=0 · 输出指纹 `c9a64798…` 不变** ·
> 另跑 `play check`（越界自查）→ 仍**只有 `BL64` + `DA42` 两张**（没加第三张）
> 前提：`B9_W51SHINE2_卡组编辑窗扫光.md` §五 的「顺手发现」1、2 两条。

---

## 一、结论（两件，逐条）

| # | 任务 | 结果 |
|---|---|---|
| 1 | 🔴 **`WildcardDisplay.UpdateCounters` 也播扫光**（由**万能卡库存变动信号**驱动） | ✅ **接上了，两个宿主都接**：收藏窗（`CollectionWindow.OnWildcardsUpdated`）＋ Deck 编辑窗（`DeckRuntime.OnWildcardsUpdated`），都吃 `Inventories.Wildcards.Updated`（= 原版 `Signal.Raise(WildcardUpdateSignal)` 的本地等价物），**复用已有扫光层与既有参数组**（收藏/Deck 都用 `Params.WildcardDisplay` = 120/0.25/0.5/白/filter 6/**0.75 s**/Once），⛔ **没有新造第三组参数**。账里那条 **D 已改判 A**（补丁 `账/_parts/B9_W63WC.tsv`） |
| 2 | 🔶 **`MessageContainer.curve` 其实有完整数值** | ✅ 把 `InboxWindow` 里那个 🔶 自己设计的 `EaseOutQuad` 换成**原版 3 键 Clamp 曲线**（`AnimationCurve` 逐键照抄，⛔ 没留两套），并就地订正「曲线没解出」那句过期注释（铁律 5）。**顺带查实一件更细的事**：原版的缓动**只挂在 `DOScale` 上**，`DOFade` 没 `SetEase` ⇒ 淡入走 DOTween 默认 `Ease.OutQuad`（判据见 §二·2）⇒ 我们**两个进度分开算**（原来是同一个 `p` 同时喂两条） |

**四句话**

1. **那条 D 不成立** —— 账里写「信号回调本地可做吗」，而 `Shell/Inventory.cs` 的 `WildcardInventory.UpdateItemCollection()`
   早就把 `Signal.Raise(updateSignal)` 落成了本地 `Updated` 事件（同一句、同一个类）⇒ 订阅方零成本。按铁律 11
   （只有「原版没有 / 用户拍板」才能不做）⇒ 改 C 并**当批做掉** ⇒ A。
2. **扫光只做 `Play` 那一半** —— `UpdateCounters` 的另一半是 `SetCountersText`（刷那 4 个数字），那 4 个数
   **2026-09-28 用户已拍板恒定 `99`**（缺的是**发放源**，不是这一跳）⇒ 仍判 D，账里补了一句边界说明。
3. **诚实标注：这条信号在生产路径上今天还不会响** —— 本工程唯一动通配符库存的地方是
   `Shell/CardDetailPopup.cs` 的 `CardProgress.TrySpendWildcard`（合成扣 1 张），它**不调**
   `Inventories.Wildcards.UpdateItemCollection()`；那个文件**不在本件白名单** ⇒ 我写的是**改动请求**（§五·1），
   没越界。⇒ 今天能看到效果的路 = 自检（`RaiseWildcardSignalForTest()`）或将来接上发放源之后。
4. **`curve` 那条末键 t = 1.00219727（不是 1）** —— 原版逐字如此，照抄（`Evaluate(1) ≈ 1.00088`，
   收尾那一拍我们本来就把缩放钉回 1，与原版 tween 结束落点等价）。⛔ 别为了「整齐」把末键改成 1.0。

---

## 二、证据（文件:行号 / 资产路径 + 字段）

### 2·1 `UpdateCounters` 是谁、被谁触发、生命周期多长（🥇 机器码真体）

| 事实 | 判据 |
|---|---|
| 体 = **Play 扫光 + 刷数字**（`shinyEffect` 为空则**抛异常**） | `d:/2/tools/decomp_full/WildcardDisplay__UpdateCounters.c`：`if (*(longlong *)(param_1 + 0x48) != 0) { Coffee_UIEffects_UIEffectTweener__Play(*(longlong *)(param_1 + 0x48),1,0); WildcardDisplay__SetCountersText(param_1,0); return; }` 之后是 `FUN_1803f47a0()`（不返回 = 抛） |
| 谁把它挂上 → **`OnEnable` 里注册信号**（在 `if (currentArmy != 0)` 那道闸**之前**，无条件注册） | `WildcardDisplay__OnEnable.c` 前 3 行：`uVar1 = thunk_FUN_18042f4a0(DAT_1842aace0); UIGenericEventCatcher_SourceDelegate___ctor(uVar1, param_1, DAT_184276490, 0); Scaffold_Core_Events_Signal__Register<object>(uVar1, DAT_184272638);` ⇒ 回调 = `DAT_184276490`（= `UpdateCounters`，与 `OnDisable` 里退订用的是**同一个**常量） |
| 谁退订 → **`OnDisable`** | `WildcardDisplay__OnDisable.c`：同一个 `DAT_184276490` + `Signal__Unregister<object>` |
| 唯一发信人 = **`WildcardInventory.UpdateItemCollection`** | `d:/2/tools/decomp_full/WildcardInventory__UpdateItemCollection.c`：`Inventory<object>__UpdateItemCollection(param_1,DAT_184255f80); Scaffold_Core_Events_Signal__Raise<object>(*(undefined8 *)(param_1 + 0x30), DAT_1842705b8);`；ILSpy 版同形（`WildcardInventory.cs`：`base.UpdateItemCollection(); Signal.Raise(updateSignal);`） |
| 我们的等价物**已经存在** | `Shell/Inventory.cs:1533-1539`（`WildcardInventory.UpdateItemCollection` → `base` 之后 `if (Updated != null) Updated();`） |

### 2·2 `curve` 的逐键真值 + 它挂在谁身上

| 事实 | 判据 |
|---|---|
| 3 键、Clamp | `assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-9188051066216764495.json` 的 `curve`：`(0, 0, slope 0)` · `(0.35165453, 0.62446737, slope ±1.53013289)` · `(1.00219727, 1.0, slope ±−0.40184420)`；`m_PreInfinity = m_PostInfinity = 2`；三键 `weightedMode = 0`（权重不参与） |
| **只作用在 `DOScale`** 上 | `decomp_full/Everguild.LiveOps.MessageContainer__AnimateIn.c`：`uVar3 = …ShortcutExtensions__DOScale(transform, &start, animationTime, 0); uVar3 = FUN_180dc5860(uVar3, &local, 1, 0, …); uVar3 = TweenSettingsExtensions__SetEase<object>(uVar3, *(undefined8 *)(param_1 + 0x78), …);` —— `+0x78` = `curve`；同一 Sequence 里 `DOFade` 那条只是 `Append(DOFade(canvasGroup))`，**没有** `SetEase` |
| 淡入的 ease = **`Ease.OutQuad`** | 原版 `DOTween.dll` **v1.2.705**（`d:/2/tools/cpp2il/out_cpp2il_dev/DOTween.dll`）反编译 `DG.Tweening.DOTween`：`public static Ease defaultEaseType = Ease.OutQuad;`（`:76`，另 `Init` 复位处 `:447` 也是 `OutQuad`）；而 `DG.Tweening.Tweener` 的 `DoStartup` 把这个默认值写进每一条 tween（`_ = DOTween.defaultEaseType;` —— ILSpy 把**左值**丢了，这是本仓已知的「ILSpy 丢字段写点」那一族）。⚠️ 原版**没有**改过 `defaultEaseType`（全量反编译里 `BattleManager__StartBattleManager.c` 只有 `DOTween.Init(...)` + `SetTweensCapacity(200,10)`） |
| 缩放起点 = 目标 × **0.5** | 同一份 `.c`：`local_38 / local_30 = *(float *)(DAT_1834b2bb4)` 乘在上面（与我们 `AnimScaleFrom = 0.5` 同值） |

### 2·3 落地落点（本件改后行号）

| 件 | 行号 |
|---|---|
| 收藏窗：订阅 = `Shell/CollectionWindow.cs:6434`（`BuildWildcardShine` 首句）· 退订 = `:6337`（`Close`）· 方法 = `:6517` / `:6525` / `:6533` · 读口 `WildcardSignalHookedForTest` `:6540` · 自检发信口 `RaiseWildcardSignalForTest()` `:6545` | 同左 |
| Deck 编辑窗：订阅 = `Deck/DeckRuntime.cs:9067`（`WildcardShineAfterTab` 的 ON 支）· 退订 = `:9073`（Cosmetics 支）+ `:1196`（`OnDestroy`）· 方法 = `:9122` / `:9129` / `:9136` · 读口 `WcSignalHookedForTest` `:9142` | 同左 |
| 收件箱曲线：`Shell/InboxWindow.cs:1321`（`BuildAnimCurve`，逐键）· `:1334`（`AnimCurveEase`）· `:1337`（只读口 `AnimCurveAtForTest`）· 用它的两处 = `:971`（`ApplyRowAnim`，两个进度分开）· `:988`（`AnimTick`） | 同左 |

---

## 三、没查清的部分（「查不到 + 还差什么」）

1. **一条真机画面都没看过**（红线：不跑 Unity）⇒「库存变动那一拍，Deck 条/收藏条上**看起来**会不会重播」
   没有视觉证据。还差什么：同步点跑 `DeckScene.Run` + `CollectionScene.Run`，再按铁律 12 进 player 看一眼。
2. **`WildcardShine.shader` 在本机从未编译过**（`Resources/Shaders/WildcardShine.shader` 仍**缺 `.meta`**，上一路如实记的）
   ⇒ 我这批断言只能保证「事件通了 + `Play` 被调到 + 参数没换组」，保证不了「画面上有一道扫光」。
3. **`DOFade` 的 `endValue` / `duration` 在这份 `.c` 里读不到**：`DG_Tweening_DOTweenModuleUI__DOFade(*(undefined8 *)(param_1 + 0x80))`
   只剩接收者（cpp2il 丢实参，同「ILSpy 把实参变 default」那一族）⇒ 我沿用既有实现的口径
   （1.0 / `animationTime`）。**没查清**的是这两个数的一手判据，但**归一化后的 t 与 ease 已确证**（§2·2）。
4. **曲线末键 >1 的可见后果没验**：`Evaluate(t)` 在 t 略小于 1 时会略过 1（末尾 ≈ ×1.0009）。
   我按原版照抄、没裁 —— 真机上**理论上**看不到（0.09%），但没有帧级证据。
5. **Deck 侧有一处「层与订阅一起晚」的复合缺口**（不是本件引入的，见 §五·2）：本件把订阅绑在
   「计数条由关变开」那一拍（= 原版 `OnEnable`），而上一路把**材质挂载做成了惰性**（同一拍）⇒
   「开窗后一直停在 Cards 页、切页签之前就合成扣卡」这个路径上，原版会重播、我们什么都不做。
   修它要同时改「建期挂载」，会动 `BuildHeader` 附近 + 约 70 条账落点 ⇒ 建议单独一件。

---

## 四、改动清单

| 文件 | 动作 | 说明 |
|---|---|---|
| `Assets/CardPresentation/Shell/CollectionWindow.cs` | **+72 行**（6515 → 6587），0 行删除 | ① `BuildWildcardShine` 首句 `HookWildcardUpdate()`（+1）；② `Close()` 里 `UnhookWildcardUpdate()`（+1）；③ 类尾 70 行：判据/偏差注释 + `_wcSignalOn` / `Hook` / `Unhook` / `OnWildcardsUpdated` / 两个自检口 |
| `Assets/CardPresentation/Deck/DeckRuntime.cs` | **+51 行**（9093 → 9144），0 行删除 | ① `WildcardShineAfterTab` ON 支 `HookWildcardUpdate()`（+1）、Cosmetics 支 `UnhookWildcardUpdate()`（+1）；② `OnDestroy` **同行**追加 `UnhookWildcardUpdate()`（零行数变化）；③ 类尾 49 行（判据 + 三个方法 + 读口） |
| `Assets/CardPresentation/Shell/InboxWindow.cs` | **+41 行**（1298 → 1339），0 行删除 | ① 入场动画那段注释**就地订正**（「曲线没解出」**已过期**，去掉 🔶 标记）；② `ApplyRowAnim` 改成**两个进度**（`pFade` / `pScale`）＋ 3 处调用点同步；③ `AnimTick` 那一行拆成「淡入 OutQuad / 缩放 curve」；④ 类尾 41 行：逐键判据注释 + `AnimCurve` / `BuildAnimCurve` / `AnimCurveEase` / 只读口 |
| `资料/完全复刻/账/_parts/B9_W63WC.tsv` | 新建（表头 + **6 行** · 11 列 · LF） | `UpdateCounters` D→C→A · `OnDisable` D→A · `WildcardInventory.UpdateItemCollection` 差异更新（「0 订阅者」→ 两个订阅者 + 发放源缺口）· `SetCountersText` 仍 D 但补边界 · `MessageContainer.AnimateIn` 差异更新（曲线已换）· `WildcardDisplay.OnEnable` 补「Register 那一半」 |
| `资料/完全复刻/报告/B9_W63WC_万能卡计数扫光.md` | 新建 | 本文件 |

**验证**：补丁已用 `python -I -X utf8 工具/merge_ledger_parts.py --dry --only B9_W63WC` 预检过
⇒ **更新 6 · 新增 0 · 无变化 0**（没跑真合并 —— 主表只有调度台能写）。

**行尾**（二进制核过）：`CollectionWindow.cs` / `InboxWindow.cs` = **纯 LF**、裸 CR 0；
`DeckRuntime.cs` = **CRLF 9144/9144**、裸 CR 0（改完仍是 CRLF，没被翻）。

**账漂移预告**（同步点 `check_ledger_points --all-A --fix`）：
· `DeckRuntime.cs`：新增行全在 **:9067 之后**（最后一段），而账里引用本文件的 81 条**最大到 `:9033`** ⇒ **零漂移**；
· `CollectionWindow.cs`：`:6337` 起 +1、`:6434` 之后再 +1（含 `BuildWildcardShine` `:6430 → :6431`）；
· `InboxWindow.cs`：`:954` 起 +3（`ShineTick` `:1008 → :1012`、`PlayRowShine` `:1253 → :1257`）。

---

## 五、顺手发现（请调度台分流 —— ⛔ 我一条都没越界动手）

1. 🔴 **`UpdateCounters` 的「发放源」那一跳没人做**（真缺口，**改动请求**；文件 = `Shell/CardDetailPopup.cs`，**不在本件白名单**）：
   - 现状：全工程唯一会动通配符库存的是 `CardProgress.TrySpendWildcard(army, rarity)`（`Shell/CardDetailPopup.cs:418`），
     调用点 = `:528`（`DoCraft` 那边）与 `:2499`。它扣完数**只** `Save()`，**没有**任何广播。
   - 要做的：扣成功之后调 `Inventories.Wildcards.UpdateItemCollection()`（= 原版那条 `Signal.Raise` 的本地入口；
     我们 `WildcardInventory` 的 override 已经在做「base 重建 + 发 `Updated`」）。
     ⚠️ 两处调用点都要（`:528` 与 `:2499`），且**只在返回 `true` 时**发（失败不许发 —— 原版是服务端受理才发奖励）。
   - 判据：`WildcardInventory__UpdateItemCollection.c`（唯一 `Raise` 点）+ `CardDetailPopup` 里那两句
     「扣减的结果必须判」（既有注释）。
   - 影响：不做的话，本件接的扫光**永远只在自检里亮**（生产静默 —— 属于「该播没播」，不是「原版没有」）。
2. ⚠️ **Deck 侧「层与订阅一起晚」**（同一族，上一路的设计 + 本件的忠实映射叠加出来的）：
   上一路把 `hdr_wcbg` 的扫光材质做成**惰性**（第一次「由关变开」才挂）；本件照原版把订阅也放在同一拍。
   ⇒ 「开窗停在 Cards 页、没切过 Cosmetics 就合成扣卡」这条路上：原版 `OnEnable` 早就 `Register` 了（会重播），
   我们既无层也无订阅。**修法**：把挂载从惰性改成建期（`BuildHeader` 建完 `hdr_wcbg` 时挂一次 + 订阅），
   代价 = `DeckRuntime` 约 70 条账落点漂移（`--fix` 能修，但会与别人的漂移混在一起）⇒ 建议单独一件、单独一批做。
3. ⚠️ **收藏窗切回 Cards 页签不重播**（原版 `OnEnable` 会）：我们的四页是「只 `SetActive` 切换、不重建」
   （`CollectionTabPage.SetupOnce` 只建一次）⇒ 原版每次回到 Cards 页都 `OnEnable` → `Play`，我们只建页那一次播。
   **建议落点** = `CollectionWindow.RefreshHighlights()`（基类 `ChangeTab` 会调）里按 `CurrentTab == CollectionCards`
   `Play(true)` + 订/退信号（同一句就顺带把偏差 ① 收干净）。⚠️ 本波**没接**：会改动切页行为，
   而 `Editor/CollectionScene.cs` 里那串扫光断言（`WildcardShineRate` 从 0 起、0.5 s → 0.667…）在切页路径上
   可能被这一下重置 ⇒ **必须同一个代理连断言一起改**，否则自检会红。
4. 📌 **`Inventories.Wildcards` 背包至今没有任何条目**（`Shell/Inventory.cs:1526` 那一族）：
   数量正本仍在 `CardProgress._wild`（`army|rarity` 两维）。本件只是把「信号」这一段接通，**没有**去播种背包 ——
   播种属于「库存正本归并」那条线（`报告/C_CardProgress归并_第1轮.md`），⛔ 别在本件里顺手改口径。
5. ✅ **`WildcardShineLayer.Play` 对「已死层」是安全的**（给下一个接信号的人省一次排查）：
   `Play` → `PushRate` 只写 `_mat`，而 `PushRate` 有 `_mat != null` 守卫（材质被 `PruneDead` 销毁后是 Unity 假 null）
   ⇒ 我的 `OnWildcardsUpdated` 不需要 `StillAttached` 之类的额外守卫，也不会抛
   （判据 `Effects/UIEffectTogglerCoffeeEffects.cs` 的 `PushRate` / `Release` / `PruneDead`）。
