# B111W 对账 · B 组（外壳/UI）

> 只读对账代理 · 2026-11-01 · 负责 `B-66` `B-88` `B-89` `B-116` `B-141` `B-147` `B-148` `B-149`
> 目的：查「活状态表」里标 `未开始` 的这 8 条，**当前代码是否已经做掉、只是没核销**。
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改任何已有文件 · 本文件是本轮唯一写盘。
> 路径：工程侧 `Unity/MyGame/Assets/CardPresentation/...`；工具侧 `Unity/工具/...`；资料侧 `Unity/资料/...`。

## 一览

| ID | 现读结论 | 证据（文件:行号 + 摘录） | 判定 |
|---|---|---|---|
| **B-66** | 按下态断言**已被 `B111S1_Assert` 补上**，并已挂进 `SettingsScene.RunCore()`；`SettingsScene.cs:1874` 那条本身仍只断常态（可不动） | `Editor/B111_S1_Assert.cs:107` `C(tableVal, ArtPressed, "A2 且它的值 = …")` · `:126` `Tr(tex != null, "B1 CardArt.MenuUi(\"40K_dropdown_item_press\") 取到图 …")` · 宿主 `Editor/SettingsScene.cs:5529` `B111S1_Assert.Run(); _sink.Pass += …`（在 `RunCore()` 里、合计行 `//` 之前） | ✅已做完待核销（附：行级「按下换图」运行断言仍未做） |
| **B-88** | `C9_Assert` **仍然 0 宿主**；D 组仍按「点完就翻节点」的旧时序写，**没有任何 `DismissRewardWindows()`** | `Editor/C9_Assert.cs:59` `public static void Run()`；全仓 `C9_Assert` 只有 `:26` 与 `Editor/Q2_Assert.cs:36` 两处**注释**；`:253/:264` 直调 `tab.ClaimForTest(...)`；共用关窗口在 `Editor/RewardWindowFixture.cs:64` | 🟠真没做 |
| **B-89** | 状态表点的 3 处**原文一字未改**（另发现第 4 处同族过期注释） | `Shell/CampaignRewardWindow.cs:92`「我们这一侧的领取是**同步**的」· `:904`「在 `ClaimForTest` 的 `if (…Claim…)` 支里补一句 …」· `Shell/CampaignTabPartial_C9.cs:25`「节点状态 … 仍早于两者」；对照现码 `Shell/CampaignTab.cs:1339-1346`（`Claim` 已搬进 `FinishClaim`，挂在领奖窗 `onClose` 上） | 🟠真没做 |
| **B-116** | 18 处运行时/自检提示串**全部仍是裸命令或只有模式参数**，没有一处补 `--apply`/`--check`；而脚本裸跑确实 `return 2` | `Unity/工具/import_original_art.py:1993-1998` `if not args.check and not args.apply: … return 2`；18 处逐一现读见下节 | 🟠真没做 |
| **B-141** | `CollectionWindow` 那句「幂等 ⇒ 先播种不盖盘上值」**仍是原样**，没有指向 `InventoryPartial_B112G.cs` 的半句 | `Shell/CollectionWindow.cs:902`（注释）· `:903`（`Inventories.SeedCardsAllUnlocked(...)`）；迁移本体 `Shell/InventoryPartial_B112G.cs:93` `MigrateBadCardAmounts`，挂点 `Shell/Inventory.cs:483` | 🟠真没做 |
| **B-147** | 联盟下拉项**仍只接 `MenuDraw.Hit` + `WindowButton`**，没有「按下把选中挪走」的状态；文件里连 hover/选中行字段都没有 | `Shell/AllianceMemberTab.cs:2456-2462`（建行 + `MenuDraw.Hit(...)`）· `:2476-2486` `RefreshItemArt()` 里 `_selected` 只表示「当前值」；对照已按原版实现的那半边 `Battle/SettingsPanel.cs:2819` `if (hover >= 0) _langSelRow = hover;   // = 原版 Selectable.OnPointerDown → Select()` | 🟠真没做 |
| **B-148** | `CardDisplayWindow` 侧**只有接线、没有驱动**：`LevelUpFx` 被建出来并绑好，但全仓**没有任何一处调 `LevelUpFx.DoAnim()`**；升级播放仍在 `CardDetailPopup` | `Battle/CardDisplayWindow.cs:1508` `PlayOpenAnimation(); EnsureLevelUpFx();` · `Battle/CardDisplayWindowPartial_B111F1E.cs:53-77`（创建 + 绑定，注释明写「本窗**不会自己播**它」）；驱动只剩 `Shell/CardDetailPopup.cs:2995` `LevelUpEffect.DoAnim();` | 🟠真没做 |
| **B-149** | 升级动画**仍挂在弹窗窗根**上、仍用窗根上的 `Animation.Play()` 放那支按 `2DCard/...` 绑路径的 clip；树形没变 | `Shell/CardDetailPopup.cs:2965` `LevelUpEffect = gameObject.AddComponent<CardDisplayLevelUp>()`（`gameObject` = 窗根）· `:3389` `GetComponent<Animation>()`（窗根）· `:2970` `shakeTarget = transform`；`Effects/CardDisplayLevelUp.cs:278` `myAnimation.Play()`；树形判据 `判据/升级动画scale曲线_节点映射.md` §一句话结论 | 🟠真没做 |

---

## 逐条明细

### B-66 — `SettingsScene` 只断下拉行常态、没断按下态

**原表说**（`活状态表.md:127`）：`Editor/SettingsScene.cs:1874` 只断常态 `40K_dropdown_item`、**没断按下态** ｜ 未开始 ｜ 2026-10-31 `B110_W7`。
**判据原文**（`资料/完全复刻/报告/B110_W7.md:105-109`）给的验收口径是：

```csharp
CheckTrue(CardArt.MenuUi("40K_dropdown_item_press") != null,
          "按下图 `40K_dropdown_item_press` 取得（原版 `Toggle.m_SpriteState.m_PressedSprite`）");
```

**现读**：

1. `Editor/SettingsScene.cs:1874-1875` —— 仍是常态那一条：
   `CheckTrue(r0 != null && r0.Texture != null && r0.Texture.name == "40K_dropdown_item", "行底图 = …")`。
2. **但按下态的断言已经存在**，在 `Editor/B111_S1_Assert.cs`（账 `B-63`）：
   - `:106-107` 断 `WindowButton.PressedNames` 里 `40K_dropdown_item → 40K_dropdown_item_press`；
   - `:126-130` 断 `CardArt.MenuUi("40K_dropdown_item_press") != null`，并量 **717×92**；
   - `:136-150` 还断盘上文件、`.meta`、`nPOTScale: 0`、全表每条都指得着图。
3. 它**已挂进设置窗宿主**：`Editor/SettingsScene.cs:5529` 的聚合行里有
   `B111S1_Assert.Run(); _sink.Pass += B111S1_Assert.Pass; …`，且位置在 `RunCore()`（`:595`，由 `:589` 调起）里、**合计行 `//` 之前**（不是被注释吞掉的那种）。

**判定：✅已做完待核销**（按 `B110_W7:109` 给的口径）。
**保留意见（不是缺陷，只是口径差）**：`B111S1` 断的是「图能取到 + 名字映射对」，**没有**断「按下这一行 ⇒ 行底图真的换成 `_press`」。若要把 B-66 做到行级，可直接复用现成的 `WindowButton.AuditPressedSwap`（`Shell/WindowButton.cs:439`，目前只有 `Editor/ShopScene.cs:892` 在用），不必新写。

---

### B-88 — `C9_Assert` 0 宿主 + D 组旧时序

**原表说**（`活状态表.md:173`）：`Editor/C9_Assert.cs` 当前 **0 宿主**（`grep "C9_Assert.Run"` 空）⇒ 它的 D 组假设「点完 `Unlock` 立刻翻节点」的旧时序；`B-15` 落地后接线时必须每轮补一句 `DismissRewardWindows()` ｜ 未开始。

**前置已满足**：`B-15` 已于 2026-11-01 核销（`活状态表.md:135`，`B111_S2` 落 `Shell/CampaignTab.cs:1326`）。

**现读**：

1. 宿主仍为空：全仓 `C9_Assert` 的命中只有 `Editor/C9_Assert.cs:26`（接线说明注释）与 `Editor/Q2_Assert.cs:36`（注释里提了一句），**没有任何 `.Run()` 调用**。
2. D 组仍是旧时序：`Editor/C9_Assert.cs:253` `bool ok = tab.ClaimForTest(i, CampaignData.TierBasic);`、`:264` 同款，**前后都没有 `RewardWindowFixture.DismissRewardWindows()`**（该文件全文 0 命中）。
3. 生产侧现在**不再同步翻节点**：`Shell/CampaignTab.cs:1339-1347` 把 `CampaignData.Claim(...)` 搬进局部函数 `FinishClaim()`，挂在 `RewardWindow.ShowCollected(...)` 的 `onClose` 上；`ClaimForTest` 只做「发奖 → 开窗 → return true」。
   ⇒ 直接接线大概率**会真红**（**按现码推演，本轮没跑 Unity**）：D 组下一轮读 `Claimable(i+1)` 时前驱还没翻 ⇒ 后继仍锁着，`:264` 那条「只为解锁后继」实际解锁不了；而且每轮都会**多留一扇没关的领奖窗**。
4. 关窗口是现成的：`Editor/RewardWindowFixture.cs:64` `public static int DismissRewardWindows()`（`RewardsScene` / `C3_Assert` / `B103F4_Assert` 等已在用）。

**判定：🟠真没做**。缺两件：① 选宿主接线（建议 `Editor/RewardsScene.cs` 或 `ShellScene`，参照 `RewardsScene.cs:5883` 的直调写法）；② D 组每轮 `ClaimForTest` 之后补 `RewardWindowFixture.DismissRewardWindows()`（关窗才触发 `FinishClaim`）。

---

### B-89 — `CampaignRewardWindow` / `CampaignTabPartial_C9` 的三处过期说明

**原表说**（`活状态表.md:174`）：`Shell/CampaignRewardWindow.cs` 的 `OnUnlock` 文档「更贴原版的写法是在 `ClaimForTest` 的 `if(…)` 支里补 `CloseWindow<CampaignRewardWindow>()` 并把 `Func` 收回 `Action`」与 `B-15` 同族；已扩到 3 处 ｜ 未开始。

**现读（逐处）**：

| # | 位置 | 原文（仍是这样） | 为什么过期（现码） |
|---|---|---|---|
| ① | `Shell/CampaignRewardWindow.cs:904-906` | 「**更贴原版结构**的等价写法是在 `ClaimForTest` 的 `if (…Claim…)` 支里补一句 `_win.Manager.CloseWindow<CampaignRewardWindow>()`」 | `Claim` 已不在 `ClaimForTest` 的 `if` 支里，而在 `Shell/CampaignTab.cs:1339` 的局部函数 `FinishClaim()`（挂在领奖窗 `onClose`）；`ClaimForTest` 里也没有 `if (…Claim…)` 分支 |
| ② | `Shell/CampaignRewardWindow.cs:92` | 「我们这一侧的领取是**同步**的（`Shell/CampaignTab.cs` 的 `ClaimForTest` `CampaignData.Claim(i, tier, out why) == true`）」 | 现在**不同步**：`Claim` 延到关窗那一拍（`CampaignTab.cs:1339-1344`） |
| ③ | `Shell/CampaignTabPartial_C9.cs:25` | 「节点状态（`CampaignData.Claim`）在 API 语义上仍早于两者 —— 与原版「节点状态在 `onClose`」的差别见报告」 | 该差别已被 `B-15` 消掉（`CampaignTab.cs:1339` 就是挂在 `onClose`） |

另：`Shell/CampaignRewardWindow.cs:83-90`（`OnCollect` 那个 `bool = 本次领取成不成`）现读也只剩「请求受理了」的语义 —— 与 ② 是同一条，改的时候一起改。

**判定：🟠真没做**（纯注释三处 + 第 4 处见「顺手发现」）。

---

### B-116 — 18 处提示串教人跑 rc=2 的命令

**原表说**（`活状态表.md:201`）：清单 **18 处**（非 19）；`B111_T1C` 按「会不会让人真去跑」严格重数。

**根因现读**：`Unity/工具/import_original_art.py:1987-1998` —— 「动作必须显式」：
```python
if args.check and args.apply: … return 2
if not args.check and not args.apply:
    ap.print_usage(sys.stderr)
    print('🔴 **没给动作参数 ⇒ 只打印用法并退出（一个字节都不写）**…')
    return 2
```
⇒ 任何「裸跑」或「只给 `--only-menu` / `--only-cardback-sdf`」都 **rc=2、一个字节不写**。

**18 处逐一现读（全部仍是旧写法，`--apply` 出现次数 = 0）**：

| # | 位置 | 现读串（截取） | 缺什么 |
|---|---|---|---|
| 1 | `Battle/WaitBanner.cs:358` | `取图请跑 \`工具/import_original_art.py\`` | 缺 `--apply` |
| 2 | `Battle/WaitBanner.cs:505` | 同上 | 缺 `--apply` |
| 3 | `Core/CardArt.cs:408` | `跑 \`python 工具/import_original_art.py\` 补图` | 缺 `--apply` |
| 4 | `Core/CardArt.cs:1307` | `跑 \`python 工具/import_original_art.py --only-cardback-sdf\` 补图` | 缺 `--apply`（该模式单给也 rc=2） |
| 5 | `Editor/BattleScene.cs:3065` | `（跑 \`工具/import_original_art.py\`）` | 缺 `--apply` |
| 6 | `Editor/B102RAID_Assert.cs:28` | `\`工具/import_original_art.py\` 的 \`MENU_IMAGES\` 那一行 → 跑一次导入器` | 缺 `--apply` |
| 7 | `Editor/ShopScene.cs:2456` | `\`工具/import_original_art.py --only-menu\`` | 缺 `--apply` |
| 8 | `Editor/ShopScene.cs:2461` | 同上（红字里） | 缺 `--apply` |
| 9 | `Editor/ShopScene.cs:2632` | 同上（红字里） | 缺 `--apply` |
| 10 | `Editor/ShopScene.cs:5372` | `谁跑一次 \`import_original_art.py\`` | 缺 `--apply` |
| 11 | `Editor/ShopScene.cs:5799` | 同上 | 缺 `--apply` |
| 12 | `Editor/ShopScene.cs:5879` | 同上 | 缺 `--apply` |
| 13 | `Editor/MainMenuScene.cs:16444` | `谁跑一次 \`import_original_art.py\`` | 缺 `--apply` |
| 14 | `Editor/MainMenuScene.cs:16606` | 同上 | 缺 `--apply` |
| 15 | `Shell/BoosterPackOpenWindow.cs:608` | `或图没跑 \`import_original_art.py\`` | 缺 `--apply` |
| 16 | `Shell/CollectionWindow.cs:1969` | `跑 \`工具/import_original_art.py --only-cardback-sdf\` 补` | 缺 `--apply` |
| 17 | `Shell/ItemDrawerCurrency.cs:110` | `要跑 \`工具/import_original_art.py --only-menu\`` | 缺 `--apply` |
| 18 | `Shell/ShopData.cs:195` | `\`工具/import_original_art.py --only-menu\`` | 缺 `--apply` |

**判定：🟠真没做**（18 处一处未改；改法 = 按 `报告/B111_T1C.md:121` 补 `--apply`）。

---

### B-141 — `CollectionWindow` 那句「幂等」注释要补指针

**原表说**（`活状态表.md:226`）：`Shell/CollectionWindow.cs:903` 那句注释仍然成立但**不再完整** —— 现在读盘之后还多一步「把坏行抬回基准量」，值得补一句指向 `InventoryPartial_B112G.cs` ｜ 未开始（纯注释）。

**现读**：
- `Shell/CollectionWindow.cs:901-902`（注释，⚠️ 状态表写的是 `:903`，`:903` 实际是调用行）：
  「…`SeedCardsAllUnlocked` **幂等**（只补缺条目）⇒ 先播种**不会盖掉盘上的值**。」
- 同段 `:903` `Inventories.SeedCardsAllUnlocked(_cardsState.Pool, c => CardProgress.Owned(c.Id, c.Rarity));`、`:904` `InventoryStore.Load(...)`。
- 新增的那一步确实存在：`Shell/Inventory.cs:483` `inv.RestoreAmounts(am); if (inv is CardInventory) Inventories.MigrateBadCardAmounts(am);`；
  迁移本体 `Shell/InventoryPartial_B112G.cs:93`（其文件头 `:1-22` 写明「只动坏的那几条 / 先备份 / 幂等」）。
- 但 `CollectionWindow.cs` 全文 **`B112G` / 「坏行」/「基准量」0 命中** ⇒ 指针没补。

**判定：🟠真没做**（纯注释一行，判据齐）。

---

### B-147 — 联盟下拉项没建模「按下挪走选中」

**原表说**（`活状态表.md:232`）：联盟下拉项**没有建模「按下把 EventSystem 选中挪走」这一跳**（`Selectable.OnPointerDown → Select()`）；可观察差异只出现在「按住不放」那几帧。战斗设置窗那半边已按原版实现 ｜ 未开始。

**现读**：
- `Shell/AllianceMemberTab.cs:2456-2462`：每一行 = `MenuDraw.Node("Item i")` + `MenuDraw.TextBox` + `MenuDraw.Hit(row, "Hit", …, () => Pick(idx), q, "40K_dropdown_item")`；`WindowButton` 只做 hover/press 换图，**没有选中态**。
- `:2476-2486` `RefreshItemArt()`：`SwapArt(_itemQuads[i], cur ? "40K_dropdown_item_selected" : "40K_dropdown_item")` —— `_selected` 在这里表示「当前值」，不是 EventSystem 选中。
- 全文件 **`EventSystem` / `Select()` / `isPointerDown` 0 命中**；也没有 `_selRow/_pressRow/_hoverRow` 这类字段。
- 对照「已按原版实现」的那半边：`Battle/SettingsPanel.cs:2819` `if (hover >= 0) _langSelRow = hover;   // = 原版 Selectable.OnPointerDown → Select()`，`:2784-2790` 有完整口径注释。

**判定：🟠真没做**。（不是 ⛔：这条差异是真的，只是只有「按住不放那几帧」可见。）

---

### B-148 — 「谁在展示窗里播 `Level Up Card`」仍然没有

**原表说**（`活状态表.md:233`）：`Battle/CardDisplayWindow` 里 `grep LevelUp` = 0，升级流程仍在 `Shell/CardDetailPopup` ｜ 未开始（结构问题）。

**现读**：
- 展示窗侧**已不再 0**：`Battle/CardDisplayWindow.cs:1508` 开窗那一拍调 `EnsureLevelUpFx()`；
  `Battle/CardDisplayWindowPartial_B111F1E.cs:53-77` 建 `CardDisplayLevelUp` 并把 `inheritChild` / `inheritTarget` 绑到 `Front` / `Level Up Effect`。
- **但仍是「两端到位、驱动缺」**：该 partial 的 `:39` 自己写着「本窗**不会自己播**它 —— 只把它和 `Front` / `Level Up Effect` 接上，播不播由升级流程决定」；全仓 **`LevelUpFx.DoAnim` / `LevelUpFx.Play` 0 命中**。
- 升级流程仍只跑在弹窗：`Shell/CardDetailPopup.cs:2958-2995`（`PlayUpgradeAnimation` → `LevelUpEffect.DoAnim()` 在 `:2995`）；`CardDetailPopup` 全文**不引用** `CardDisplayWindow`。

**判定：🟠真没做**（缺的是一次结构裁定 + 驱动：把升级流程搬到展示窗，或让弹窗驱动展示窗的 `LevelUpFx.DoAnim()`）。

---

### B-149 — 升级动画整体跑在「窗根」

**原表说**（`活状态表.md:234`）：我们的升级动画整体跑在「窗根」上，而 clip 路径是 `2DCard/Front` / `2DCard/Level Up Effect` / `Lighting Rays` —— 相对窗根基本对不上 ｜ 未开始（更大的结构问题）。

**现读（结构没变）**：
- 组件挂在**弹窗窗根**：`Shell/CardDetailPopup.cs:2965` `LevelUpEffect = gameObject.AddComponent<CardDisplayLevelUp>();`。
- `Animation` 也挂在**窗根**：`:3389` `var anim = fx.myAnimation != null ? fx.myAnimation : GetComponent<Animation>();`；`:3409` `fx.myAnimation = anim;`。
- 播放走窗根上的 `Animation.Play()`：`Effects/CardDisplayLevelUp.cs:278` `if (myAnimation != null) myAnimation.Play();`。
- 树的形状对不上 clip 路径：弹窗里卡格叫 `CardUI` / `CardUI (n)`（`Core/CardFan.cs:100`），`2DCard` 是**每张卡内部**的一层（`Core/CardView.cs:4260` `new GameObject("2DCard")`）⇒ 从窗根解析 `2DCard/Front` 找不到。
- 判据原文（`资料/完全复刻/判据/升级动画scale曲线_节点映射.md` §一句话结论）已写死：「我们的 `Animation` 组件挂在**窗根**上」「`2DCard/Level Up Effect` 我们树上没有那个节点」。
- 现状只是**部分被代码补间绕过**（`pulseTarget` / inherit 绑定 / fade 图），**没有**改 clip 路径解析（全仓没有对这支 clip 做 `SetCurve` 重绑：`SetCurve` 只出现在 `Battle/BattleHudAnim.cs` / `Battle/CameraIntro.cs` / `Battle/Card3DAnimationController.cs`）。

**判定：🟠真没做**。

---

## 顺手发现（⛔ 只报告、未改动）

1. **B-89 有第 4 处同族过期注释**：`Shell/CampaignTabPartial_C9.cs:10`「我们这一侧 `ClaimForTest` 只做了 `Claim` … + `ShowCollected`」—— 现读它还会先 `GrantCampaignRewards`（`:1338`），且 `Claim` 已延到关窗。既然 B-89 是纯注释活，顺手一起改更省一轮。
2. **`Shell/CampaignRewardWindow.cs:83-90`** 的 `bool = 本次领取成不成` 也已不准确（现在只代表「请求受理了」）—— 与 B-89 ② 同一条，改注释时别漏。
3. **B-116 同族但未进 18 处清单的写法**：`Core/CardArt.cs:972` `导入器：\`工具/import_original_art.py --only-cardback-sdf\`。`（文档指针，照抄同样 rc=2）。若批量改 B-116 的 18 处，建议一起加 `--apply`，避免下次又数出第 19 处。
4. **B-141 的行号漂移**：状态表写 `Shell/CollectionWindow.cs:903`，实际要补的注释在 `:902`（`:903` 是 `SeedCardsAllUnlocked` 调用行）。改的时候按内容定位，⛔ 别按行号。
5. **B-66 若要做到行级**：`WindowButton.AuditPressedSwap`（`Shell/WindowButton.cs:439`）已经存在（目前只有 `Editor/ShopScene.cs:892` 用），不需要新写一套按下态审计。
6. **B-88 接线时别只接线**：`Editor/C9_Assert.cs` 的 D 组在 B-15 之后会**真红**（后继节点不解锁 + 领奖窗堆积），接线时必须同批补 `DismissRewardWindows()`，否则「一接就红」会被误判成实现坏了。

---

## 汇总

| 判定 | 条数 | 条目 |
|---|---|---|
| ✅已做完待核销 | **1** | B-66 |
| 🟠真没做 | **7** | B-88 · B-89 · B-116 · B-141 · B-147 · B-148 · B-149 |
| ⛔判据本身错/前提不成立 | **0** | — |
| ❓查不到 | **0** | — |

> 注：`B-66` 判 ✅ 是按 `报告/B110_W7.md:105-109` 自己给的验收口径（「`SettingsScene.Run` 里补一条按下态 sprite 非空断言」）——该断言已由 `B111S1_Assert`（B-63）覆盖并挂进 `SettingsScene.RunCore()`。**行级「按下这一行 ⇒ 底图变 `_press`」** 仍无断言，若要更严可单开一条。
