# B8_BF —— 图缓存的释放缺口（`CardArt._cache` 只增不减）

> 工作项：C 档「补等价释放」，一次覆盖 3 条账行（`未分类_其他.tsv` **:201 / :221 / :304**）。
> 白名单内改动：`Core/CardArt.cs` · `Shell/MenuDraw.cs` · `账/_parts/B8_BF.tsv` · 本报告。
> 收尾 `typecheck`（`TMPDIR=/tmp/wf_b8bf`）：**运行时 0 / 编辑器 0**。

---

## ① 结论

**1. 原版释放的到底是什么 —— Addressables 句柄（缓存组里的一条），不是「销毁贴图」。**

三份方法体（🥇 `d:/2/tools/decomp_full/`，**三份的第一行函数名都与文件名一致** ⇒ 避开了「文件名与内容不符」那 16.5% 的坑）：

| 原版方法 | 方法体做什么 | 出处 |
|---|---|---|
| `CosmeticItem.UnloadReferences()` | `imageReference`(+0x40) 非空 ⇒ `AssetReferenceTyped<Sprite>.Release(...)`；为空 ⇒ **不返回的抛出出口** | `CosmeticItem__UnloadReferences.c:2-16`（放句柄在 `:9-13`，抛出在 `:14-15`） |
| `CursorByArmySO.ReleaseArmyCursor(army)` | 遍历表（长度 `+0x18`、元素基址 `+0x20`），命中 `+0x10 == cardArmy` ⇒ cursor(`+0x18`) 与 highlight(`+0x20`) **两个**句柄各 Release 一次 | `CursorByArmySO__ReleaseArmyCursor.c:2-39`（`:28-32`） |
| `FlavourTextSO.ReleaseFlavorBackground(army)` | 遍历表找 `+0x10 == army` 的项，命中 ⇒ flavorBg(`+0x18`) Release；随后**无条件** `CustomDebug.LogWarning`（在循环外） | `FlavourTextSO__ReleaseFlavorBackground.c:2-57`（`:50-51` 放句柄、`:28-32` 告警） |

**「引用计数」这一步是判据、不是推断**：三处调的实参都是 `(handle, 1, 0, …)`，对上的签名是

```
// 🥈 d:/2/tools/il2cpp_out/dump.cs:135633
public void Release(AddressableCacheGroup cacheGroup = 1, bool releaseAll = False) { }
// 🥈 dump.cs:45274-45281 → Undefined=0, General=1, Persistent=2
```

⇒ 原版 = **从 `AddressableCacheGroup.General` 里放掉「这一条」**，`releaseAll = false`（**不是全清**），
而且 `AssetReferenceTyped<TObject> : AssetReference`（`dump.cs:135596`）⇒ 贴图不会因为放一次就消失。
（`CosmeticItem.imageReference` 的类型 = `AssetReferenceTyped<Sprite>`，🥈 `CosmeticItem.cs:10`；放句柄那一句 🥈 `:246`。）

**2. 我们的等价物 —— `CardArt._cache` 就是我们的「句柄表」，放掉一条 = `Release`。**

- 全工程**没有** Addressables：图一律 `Resources.Load` 进 `CardArt._cache`（`Core/CardArt.cs` 的 `Get`），
  那份字典**只增不减**（原缺陷）。
- 新增 **`CardArt.Release(string path)`**（`CardArt.cs:1153-1181`）：把这一条强引用从 `_cache` 里放掉、**不销毁贴图**
  （⛔ 全仓 `Resources.UnloadAsset` 零处；`UnloadUnusedAssets` 只在三份 Editor 探针里）。
- 调用口 **`MenuDraw.ReleaseArtUnder(Transform)`**（`MenuDraw.cs:3614-3632`），由 **`MenuDraw.ClearChildren`**
  在**每个即将销毁的子树**上调用（`:143` 那一行行尾，**零新增行**）。
  ⇒ 时机等价：原版是「对象被卸载那一刻」（`ItemDrawer.OnDestroy` / 各 `OnDisable` 一类收尾），
  我们是「子树被销毁那一刻」（先例 = `Shell/ItemDrawerLeash.cs` 用同一招接 `ItemDrawer.OnDestroy`）。
- 释放范围**收敛在「逐项美术」族**（`CardArt.IsPerItemArt`，`CardArt.cs:1131`）：
  `cards/art_*`（立绘）· `cardbacks/`（卡背装饰品）· `avatars/`（头像装饰品）· `altarts/`（异画）·
  `ui/flavourbg_*`（风味底图）· `ui_menu/Cursor_*`（光标两图）。共享件（卡框 / SDF / `traits/` / HUD 图 / `card3d/`）
  **不放** —— 原版那三个方法也没碰它们。

**3. 三条账行全部升 A**（`未分类_其他.tsv:201 / 221 / 304`），0 条改判 D。
**G3 只到「观测量已定义」**：原版侧可推（每次调用该组条目 −1 / `ReleaseArmyCursor` 一次两个 / flavorBg 一个），
我们侧读数**没有测**（子代理禁跑 Unity），已在账里写明「读数待同步点」。

---

## ② 逐条证据（G0–G8）

| 账行 | 原版方法 | 判据出处 | 我们落点（方法级） | 机制差异（G2） | 观测量（G3） |
|---|---|---|---|---|---|
| 201 | `CosmeticItem.UnloadReferences()` | `decomp_full/CosmeticItem__UnloadReferences.c:9-15` 🥇 + `dump.cs:135633 / 45274-45281` 🥈 | `CardArt.cs:1153-1181（Release）（等价实现 = CardArt.Release）` | 原版 = `Release(cacheGroup General(1), releaseAll false)` 放**这一条**；我们 = 从 `_cache` 放掉**这一条**强引用（同样不销毁贴图、同样不是全清）。空句柄那一档：原版抛（不返回），我们出声 + `false` | `CardArt.CacheCount` / `LiveCacheCount` / `ReleasedCount`（原版侧 −1/次；我们侧待同步点读） |
| 221 | `CursorByArmySO.ReleaseArmyCursor(CardArmy)` | `decomp_full/CursorByArmySO__ReleaseArmyCursor.c:28-32` 🥇 | `MenuDraw.cs:3614-3632（ReleaseArtUnder）（等价实现 = MenuDraw.ReleaseArtUnder）` | 原版按 **CardArmy** 遍历表、命中就把两个句柄各放一次；我们遍历**即将销毁的子树**、按路径族命中后逐条放（「遍历 + 命中即放」照抄，键从「阵营」换成「子树」） | `CardArt.CacheCount`（本族本机 2 张） |
| 304 | `FlavourTextSO.ReleaseFlavorBackground(CardArmy)` | `decomp_full/FlavourTextSO__ReleaseFlavorBackground.c:18-57` 🥇 | `CardArt.cs:1153-1181（Release）（等价实现 = CardArt.Release）` | 找项 + 放句柄两侧一致；原版循环外那句**无条件** `LogWarning` 不看结果（它自己的噪声）⇒ **不照抄**，我们只在 `Release` 落空时出声 | `CardArt.CacheCount`（风味底图 13 张） |

- **G1**：三段落点都逐句读过（`Release` 1153-1181 · `ReleaseArtUnder` 3614-3632）。
- **G5（边界）**：空路径（出声 + false）· 负缓存条目（value==null 不放，`Get` 故意缓存它免得反复 IO）·
  租约在身（拒绝 + 出声）· 三种拒绝路径都不抛异常。
- **G7**：`typecheck` 0/0；`check_ledger_points.py` 对 `CardArt.cs` / `MenuDraw.cs` **0 漂移 0 失败**；
  补丁自检 `A=3 通过=3 漂移=0 告警=0 失败=0`。
- **共享落点说明**（口径 §4.1 允许）：201 与 304 共用 `CardArt.cs:1153-1181`，段落里各自写清了
  它**如何分别满足**两条（一条是任意装饰品图、一条是 `ui/flavourbg_*`）。

---

## ③ 改动清单

| 文件 | HEAD → 现在 | 新增 | 位置 |
|---|---|---|---|
| `CardPresentation/Core/CardArt.cs` | 1081 → **1214** 行（+133） | `Get` 里 **+1 行**（`_pathOf` 反查表）；类尾 **+132 行**（释放层：判据头注 + `CacheCount/LiveCacheCount/ReleasedCount` + `PathOf` + `IsPerItemArt` + `PerItemArtPrefix` + `Release` + `Retain/Unretain/IsRetained` + 三个静态字段） | `Release` = **1153-1181**；`IsPerItemArt` = **1131**；`PathOf` = **1120** |
| `CardPresentation/Shell/MenuDraw.cs` | 3597 → **3635** 行（+38） | `ClearChildren` 里 **0 行**（调用接在既有 `:143` 行尾）；类尾 **+38 行**（判据头注 + `ReleaseArtUnder` + `_artWalk` 缓冲） | `ReleaseArtUnder` = **3614-3632** |

**为什么 `ClearChildren` 那里「零新增行」**：这两个 `.cs` 被账按行号引用；
`MenuDraw.cs:2197 / 3180 / 3310` 有 4 条 A 行落点 —— 在 `:143` 插一行会让它们**全部漂移**。
所以把调用**接在同一行的行尾**。改完复核：`check_ledger_points` 对这两个文件 **0 漂移**（不靠 `--fix`）。

**影响哪些窗口**：`MenuDraw.ClearChildren` 全仓 **74 处调用 / 45 个文件**，其中与这三条原版方法直接对应的是
`Shell/ItemDisplayWindow.cs:251`（= 装饰品展示窗，`CosmeticItem` 那一档的活调用点）·
`Battle/CardDisplayWindow.cs:1339/1679`（= 卡牌展示窗，风味底图 + 立绘）·
`Shell/ItemDrawer.cs:1960`（抽屉刷新）· `Shell/TutorialPointerUi.cs`（光标两图所在子树）。
其余（`CollectionWindow` / `ShopWindow` / `DraftModeWindow` / 各 popup / 三个 Editor 自检宿主…）走同一条路。

**为什么安全（逐条）**：
1. 只放 `CardArt.IsPerItemArt` 那一族；**共享件一律不动**（卡框/SDF/HUD/traits/card3d）。
2. 贴图**不销毁**（`Release` 只 `_cache.Remove`，全仓无 `Resources.UnloadAsset`）⇒ 同一张图仍挂在别的活节点上时**照常显示**。
3. 只处理**即将销毁**的子树（`ClearChildren` 正是那个时刻），不是「全清缓存」—— 与原版 `releaseAll = false` 同形。
4. 全仓**没有**任何断言依赖 `_cache` 的条数（`CardArt.Describe()` 的「已加载 N 张」没有消费者，已 grep 复核）⇒ 不会撞自检。

---

## ④ 护栏（「不能释放正在用的图」）

按强度分四层，从「结构上不可能」到「显式闸」：

1. **结构层（最硬）**：`Release` **只放我们这一份强引用，从不销毁资产**。
   `ImageQuad` 持有 `_tex` 且材质持有 `mainTexture` ⇒ 还挂在屏上的图**一定有活的引用**，
   放掉缓存条目**不可能**造成白块 / 丢图。这正是原版 `AssetReference.Release` 的语义（计数减一，最后一个走了才卸载）。
2. **范围层**：只放「逐项美术」族（`IsPerItemArt`），且**只放正在被销毁的子树里**那一份
   （`ReleaseArtUnder` 走的是 `ClearChildren` 传进来的子树，不是整场扫）。
3. **显式闸（🔶 自己设计，非原版）**：`CardArt.Retain(path)` / `Unretain(path)` / `IsRetained(path)` ——
   已知「正在显示、必须留住」的图先 `Retain`，`Release` 遇租约**拒绝并出声**。
   ⚠️ **如实说**：今天**没有生产调用点**（要不要 Retain 取决于将来某个「显示期间不放」的调用方），
   所以它现在是一条**备用**闸，**不是**当前生效的保证 —— 当前生效的是第 1、2 层。
   （代码里已按铁律 18 打 `🔶 自己设计（非原版）`，写明「原版没有对应物」+ 调度台 2026-10-26 定。）
4. **负缓存不进释放**：`Get` 缓存 `null`（「找不到就别每次重试 IO」），那些不是句柄 ⇒ `Release` 直接返回 false，不删条目。
   此外 `Backdrop` 与 `Solid()` **根本不进 `_cache`** ⇒ 结构上不可能被 `Release` 碰到
   （`Solid()` 是每颗命中区/压暗层在用的，这一条有实际意义）。

---

## ⑤ 没查清的部分

1. 🔴 **我们侧读数没测**：`CardArt.CacheCount / LiveCacheCount / ReleasedCount` 是**运行时**量，
   子代理禁跑 Unity ⇒ G3 只到「观测量已定义 + 原版侧推导」。**要读数得先有宿主读它**（见 §⑦ 请求 2）。
2. 🔴 **`AssetReferenceTyped<TObject>.Release` 的方法体本机没有**：
   `decomp_full` 里 `Everguild.Addressables.*` 只有 3 个文件（`ReferenceList` 的 ctor/`LoadReferences`），
   `cpp2il/out_cpp2il_dev_ilspy` 里也没有这一份。⇒ 「从 General 缓存组里放一条」是从
   **签名（`dump.cs:135633`）+ 三处调用实参 `(handle,1,0,…)` + 枚举（`dump.cs:45274-45281`）** 读出来的，
   **不是**从方法体读出来的。真要看体得走 VA 反汇编（RVA `0x1287D30`）。
3. **`Everguild.Addressables.ReferenceList.LoadReferences`** 只存在 3 个 `__c` 相关文件，主体没读 ——
   它可能是「批量 Load/Release」的另一条路（如果原版有「按列表放」的入口，我们的 `ReleaseArtUnder` 与它更近）。
4. **`Art/cards/back_*`（阵营默认卡背）我没放进逐项族**：B8_AT 提到原版 `BattleManager.ReleaseCardbacks` 是
   `CosmeticItem.UnloadReferences` 的活调用点之一；我们这张「默认卡背」是共享件还是逐项件，**没核**。
   （保守起见按共享件处理 = 不放；若核出应放，往 `PerItemArtPrefix` 加一行 `cards/back_` 即可。）
5. **`ImageQuad.Texture` 之外的第二类持有者**没穷举：我只按 `ImageQuad` 收图；
   若某处把 `CardArt` 的贴图塞进了 `Material`/`Sprite` 而不建 quad（例如烘焙、合批、`RawImage`），
   那些子树清空时**不会被收**（漏放，不是错放）。当前 `MenuDraw` 那几条建图路径都产出 quad。

---

## ⑥ 顺手发现

1. **光标美术只有 1 个阵营**：`Resources/Art/ui_menu/` 里只有 `Cursor_Space_Marine_01` 与 `…01Glow` 两图，
   原版 `CursorByArmySO` 的表覆盖多阵营；而且我们**没有任何「按阵营切光标」的代码**
   （唯一用它的是 `Shell/TutorialPointerUi.cs`，走 `CardArt.MenuUi`）。
   ⇒ 若要做「按阵营光标」，那是**另一个工作项**（导入 12 族 + 一条按阵营选图的接线），按铁律 11 记账、⛔ 不当作 D。
2. **账里现有 189 条漂移 / 0 失败**（`check_ledger_points` 全量现读），全部来自**别的代理并发改 `.cs`**
   （`CardView.cs` / `NetBattle.cs` / `RuleCore.cs` / `EffectResolver.cs` / `AiScoring.cs` …），**没有一条**落在
   `CardArt.cs` / `MenuDraw.cs`。`--fix` 需要等「没有 `.cs` 写手」的窗口，且先归一裸 CR（本仓现读 0 个裸 CR 文件）。
3. **自检期间会多一次 `Resources.Load`**：`MenuDraw.ClearChildren` 在三个 Editor 宿主里也被用来清临时节点
   （`Editor/{ShellScene,RewardsScene,MainMenuScene}.cs`）⇒ 自检期间缓存会「放 → 重载」，功能无影响、只是开销。
4. **`CardArt.Describe()` 的数字会变小**（「已加载 N 张」按 `_cache` 非空条数算）；全仓**没有**断言解析这个串
   （`grep "CardArt.Describe"` = 0 命中，`grep "已加载"` 只命中无关文案）⇒ 不会撞自检。

---

## ⑦ 给调度台的请求

1. **合并补丁** `账/_parts/B8_BF.tsv`（`--only B8_BF_`；3 行 A：`未分类_其他.tsv` 的 201 / 221 / 304）。
   补丁自查：11 列 × 4 行（含表头）、LF、键与主表一致、`check_ledger_points` = 3/3 通过、`check_patch_conflicts` 无冲突。
2. **同步点给一条读数口**：目前**没有**宿主读 `CardArt.CacheCount / LiveCacheCount / ReleasedCount`。
   要补 G3 的「我们侧数值」，需要在某个 `Editor/*Scene.cs` 里加两三行
   （例：先把 `CollectionWindow` 建起来 → 记 `CacheCount` → `ClearChildren` 整个内容根 → 再记 `CacheCount`，断言「降了且 `ReleasedCount` 同步涨」）。
   这要 `Editor/` 写手，**不在我白名单** ⇒ 请派给对应代理或调度台自己加。
3. **光标 12 族**（见 §⑥1）请立账（导入 + 按阵营接线），与本工作项分开。
4. **189 条既有漂移的 `--fix`** 请排在「无 `.cs` 写手 + 补丁已合并完」的窗口跑一次（并先 `check_patch_conflicts`）。
