# B107 · ShopScene 两棵「孤儿树」判据（`B103F4_Assert.RunShop` 唯一的根因）

> 只读诊断代理 `B107_D1`，2026-10-31（第一百零七会话）。
> 输入 = `d:/4/_tmp_view/final_shop.log`（13:06 那趟，`ShopScene.Run` 3197/2）· `报告/B106_W79.md` · `判据/B106_D5_Battle与Shop诊断.md`。
> 行号一律**现读**（本文件写完时复核过）。⛔ 未跑 Unity · 未动 git · 未改任何 `.cs` / `.tsv` / 正本。

---

## 〇、一句话结论

**命中表里多出来的两棵子树都不是「被摘走的页」，而是两棵【出生就在场景根】的树**：
- `#2` = 一具 **`ShopTabPage` 的内容树**：`Shell/ShopTabPage.cs:246 Setup()` 在 **`_root` 已是 Unity 假 null**（页对象已被换代重建销毁）时被再次调用 ⇒ `MainMenuSubmenuWindow.Node(null, …)`（`Shell/MainMenuSubmenuWindow.cs:382-386` → `Shell/MenuDraw.cs:167-173`，`SetParent(parent,false)` 在 **`:170`**）把 `daily shop header / Packs Scroll View / Viewport / Content / CatalogItemShopContainer_i / InfoHit` 全建在**场景根**。
- `#3` = `Editor/ShopScene.cs:5008` 的 A8 实拍夹具 `OfferContainer_Shot`，**从建出来那天起就没被销毁过**（`OfferContainer.Build(shot, …)` 在 `:5026`）。

⇒ **没有人在运行期 `SetParent` 摘它们**；问题出在「**该销毁/该失效的那一环没做**」：一格是**迟到的回调**拿着已销毁的页继续 `Setup()`，另一格是**夹具收尾漏删**。

---

## 一、现场复述（`final_shop.log:131164`）

```
[B103F4·Shop] ✗ 第 1 页第 1 件的主图 `InfoHit`：（前提）它渲出来那一块的中心命中的就是这一颗
   —— 中心 (498.1, 302.6) —— 实得 = `InfoHit` · 窗 = <不属于任何 GameWindow> · 商店页 = <不属于任何 ShopTabPage>
   · 父链 = Packs Scroll View/Viewport/Content/CatalogItemShopContainer_0/InfoHit
   ；这一点上的全部候选 = 共 3 颗：
     #1 InfoHit · 窗 = `Shop Menu Variant`（ShopWindow · Open）· 商店页 = `Card Shop Tab`（宿主活着 · Manager 非空）
        · 父链 = Window Anchors/1 - Below Upper Bar Holder/Shop Menu Variant/Content Area/Tabs/Card Shop Tab/…
     #2 InfoHit · 窗 = <不属于任何 GameWindow> · 商店页 = <不属于任何 ShopTabPage>
        · 父链 = Packs Scroll View/Viewport/Content/CatalogItemShopContainer_0/InfoHit
     #3 `raycast target` · 窗/页 同上 · 父链 = OfferContainer_Shot/General Basic Offer Container Variant Booster_avatar_cardback_title/raycast target
```

- 平局链（同队列 `QCellInfoHit = 3031` → 同 z → `IsAboveInHierarchy`）判给 **#2** ⇒ 前提红；
- 点击真落到 #2 上 ⇒ 它那颗 `InfoHit` 的闭包调 `OpenBoosterInfo`（`Shell/ShopTabPage.cs:612` 建的），页已死 ⇒ 打一条 `[Shop] 没有 WindowsManager` 早退（`ShopTabPage.cs:947-951`，日志 `:131180`）⇒ 弹窗没开 ⇒ 级联红。
- ⚠️ #2 的 `InfoHit` 闭包里**还握着那具已销毁的 `ShopTabPage` 实例**（栈帧 `ShopTabPage/<>c__DisplayClass67_0:<BuildCell>b__0`）—— 这一条同时证明「销毁的 MonoBehaviour 的 C# 方法照样能跑」，与下面 §二·1 的前提一致。

---

## 二、`#2`：谁创建、谁「摘走」、谁该销毁而没销毁

### 2·1 创建点（现读，逐行）

| 步骤 | 位置 | 说明 |
|---|---|---|
| ① 页对象 | `Shell/ShopWindow.cs:249-261 BuildPages()` | `Node(tabHolder, Pages[p].Prefab, TabsRect)` + `AddComponent<ShopTabPage>()` + `SetHost(this, pg, p)` |
| ② 内容树 | `Shell/ShopTabPage.cs:246 Setup()` | 首句 `_win.DestroyChildren(_root)`；`:254` `daily shop header`；**`:262` `Node(_root, "Packs Scroll View", ScrollRectNow)`**；`:264` `Viewport`；`:288` `Content`；`:289` `BuildGrid(content)` |
| ③ 格 + 命中区 | `Shell/ShopTabPage.cs:499` / `:612` | `Node(content, "CatalogItemShopContainer_" + i, r)`；`MenuDraw.Hit(cell, "InfoHit", …)`（**只在 `o.Type == "Booster Pack"`**，`:607`） |

**`Packs Scroll View` 这个名字全仓只有一处创建**（`grep -rn '"Packs Scroll View"' Assets` 现读：只有 `ShopTabPage.cs:262` 一处是建节点，另外两处是自检按路径找）。

⇒ 孤儿链的根 = `Packs Scroll View`、往上没有任何父 ⇒ **`Node` 的 `parent` 实参那一刻就是 null**。`Node` 里唯独能把节点落到场景根的那一句就是 `MenuDraw.cs:168-172` 的 `new GameObject(…).transform.SetParent(parent,false)`——`parent` 为 null（或 Unity 假 null）时节点就是**根物体**。

### 2·2 「摘走」= 不成立；真实动作是「页对象先被换代销毁」

- `_root` 在 `Shell/ShopTabPage.cs:63 SetHost(win, root, page)` 里只写一次，且**就是页节点自己的 Transform**（`BuildPages` 传的就是它）。
- 所以 `_root` 变成 null ⇒ 那个页节点 GameObject 已经被销毁（Unity 假 null）。**销毁者**是每次重建都跑的那条链：
  `Shell/ShopWindow.cs:150-153 Open()` → `:208 Build()` → `Shell/MainMenuSubmenuWindow.cs:547 BuildShell()` 首句 `DestroyChildren(root)` → `Shell/MenuDraw.cs:138-148 ClearChildren()`（编辑模式 `Object.DestroyImmediate`，`:145`）。
- 这个 run 里 `ShopWindow.Build()` **跑了 16 次**（`grep -n 'Packs Scroll View\` 纵向滚动' final_shop.log` = 16 条 `ShopWindow.cs:227`），其中 **6 次是「`Closed ⇒ Open` 重开」触发的整窗重建**（`final_shop.log:79880 / 81223 / 82278 / 82781 / 90938 / 91757`，栈 = `GameWindow.cs:509 TryOpen → :486 OpenByState` ← `WindowsManager.cs:673 ShowPreviousWindow` ← `GameWindow.cs:805 Close` ← **`Editor/ShopScene.cs:1110` = `bp.Close()`**，即 `ClosePackAndReopenShop` 的 W78 新写法）。**每次重建都把上一代的 5 个 `ShopTabPage` 连同它们的 `Packs Scroll View` 一并销毁。**
- 因此**不存在「从父链摘走」这一跳**：孩子没被搬走，是**父（页节点）被销毁**；而 `SetParent(null)` 的那一下发生在「**之后**某次 `Setup()`」里，因为 `_root` 读出来是假 null ⇒ `SetParent(null)` ⇒ 新建到场景根。

### 2·3 谁本该销毁/失效它们却没做（漏掉的那一环）

`Shell/ShopTabPage.cs:246 Setup()` **没有任何 `_root`/`_win` 守卫**（`git blame` 现读：`:246-250` 自 2026-10-13 一字未改）。于是只要有人对一具**已销毁的页**调一次 `Setup()`，它就安静地在场景根重建整页（页 0 的 `Setup` **全程无日志**：4 件都有主图 ⇒ 不触发 `WarnNoArt`；`ItemDrawer` 的告警是「一次一物品」的静态闸，第一代之后就沉默了）——这正是它**在日志里查不到**的原因。

**能把一具已销毁的 `ShopTabPage` 留到「迟到的一刻」的，全仓只有两处（都发生在 `ShopTabPage` 上，且都不判 `this` 死没死）**：

| # | 位置 | 形态 | 这次 run 的实际状态 |
|---|---|---|---|
| **a** | `Shell/ShopTabPage.cs:914` | `RewardWindow.ShowCollected(grants, collected => Setup())` —— 回调挂在**另一扇窗**（`RewardWindow`）的 `OnClose` 上（`Shell/RewardWindow.cs:890-895`，`:892` 先发回调再 `base.Close()`），而领奖窗比商店页活得久（关它要等 `RewardWindowFixture.DismissRewardWindows()` 或下一次 `RewardWindow.Show`） | 本 run 只关了 **4 次**领奖窗（`final_shop.log:81810 / 83570 / 83816 / 132174` 附近），**每次回调打到的都是活页**（页 1 / 页 2；栈里 `WarnNoArt` 打的是「第 2 / 第 3 页」）⇒ **这一次不是它**，但它是「机制上唯一能打到死页」的那一条 |
| **b** | `Shell/ShopTabPage.cs:790` | `ShowLegendaryConfirm(_win.Manager, () => DoBuy(captured))` —— 确认框是一扇 `PopUpGameWindow`（`Shell/PopUpGameWindow.cs:388` 把 `onClick` 接成 `_onPrimary`），用户点 OK 才跑；`DoBuy` 末尾 `:867 Setup()` | 本 run 只弹过 **1 次**确认框（`final_shop.log:80750`），自检**只 `Close()` 没点 OK**（`Editor/ShopScene.cs:2353`）⇒ 回调没触发 |

⇒ **№2 的「谁来调的那一次 `Setup()`」在本 run 的日志里【指名不到】**（见 §五）。但**机制和缺环是确定的**：页对象被换代销毁后，`Setup()` 不校验 `_root`，于是建到根；而**持有那具死页的迟到回调**（a/b）没人作废。

### 2·4 `#2` 是哪一页（可判定的部分）

- 子件位置：`InfoHit` 中心 **(498.1, 302.6)** 与 **Grid 族**逐位吻合 —— `329.76 + 168.3 = 498.06` · `127.62 + 7 + 168 = 302.62`（判据 = `Editor/ShopScene.cs:4448-4450` 那两条 `CheckNear` 的算式）。
- **Flexible 族（第 5 页 Crates）算出来是 ≈ `(567.7, 247.2)`**（`FlexScrollView` 左 330.67 + pad 50 + `CellArtBox` 中心 168.3×1.1113）⇒ **不是 Crates 页**。
- `InfoHit` 只在 `o.Type == "Booster Pack"` 时建（`ShopTabPage.cs:607-613`）。Grid 族的四页里，第 1/2 页（Daily / Items）没有 `Booster Pack`；**有 `Booster Pack` 的只有第 0 页 Cards 与第 3 页 VIP（第 3 页的 `VIP Booster Cache`）**。
- VIP 页在非 VIP 态会多建一整块 `No VIP` + `BlockInputHit`（队列 `QVipAbsorb+1 = 3037`，`ShopTabPage.cs:1176`）⇒ 那样候选会是 **4 颗**且最高的是 `BlockInputHit`。现场只有 3 颗 ⇒ 若真是 VIP 页，它建的时候 `ShopData.IsVip == true`（自检在 `Editor/ShopScene.cs:2124-2125` 收尾复原成 true）—— **不排除，但不优先**。
- **⇒ 最可能 = 第 0 页 `Card Shop Tab` 的内容树**（也正是出事的 w2 所在那一页）。

---

## 三、`#3`：A8 实拍夹具漏删（这一格是**完全确定**的）

| 项 | 现读 |
|---|---|
| 创建 | `Editor/ShopScene.cs:5008` `var shot = new GameObject("OfferContainer_Shot").transform;` |
| 往里建树 | `:5026` `OfferContainer.Build(shot, v, …, 3000, null)` —— 变体 `General Basic Offer Container Variant Booster_avatar_cardback_title`（`Variants[5]`，`:5009`），树里有 `raycast target`（`Shell/OfferContainer.cs:218 NRaycast`） |
| 本该销毁 | 同一段里**上一轮的两棵同类夹具都删了**：`:5006` `_offerScratch`（`OfferContainers_A8`，`:3768` 建）· `:5007` `_offerRuler`（`OfferRulers_A34F4`，`:3779` 建）—— **唯独 `shot` 没删** |
| 谁该删 | 就这一段自己（`:5031 Shoot(...)` 之后它再没被用过，`grep -n "\bshot\b"` = 只有 `:5008`/`:5026` 两处引用） |
| 为什么能一直活到 `:131164` | 根物体、无宿主、无人销毁；`CheckNoOrphanShopPages` 只扫 `ShopTabPage`（`Editor/B103F4_Assert.cs:579-603`）⇒ **结构性看不见它** |

它本身不决定平局（平局判给了 #2），但它是**同一像素上的第三颗**：修好 #2 之后，`ClickAt` 的中心点仍可能被它抢（它队列 = 3000 < 3031，低于 `InfoHit`；但 `IsAboveInHierarchy` 只在**同队列**时才轮得到 ⇒ 严格说它抢不了 `InfoHit`，**不过它仍是场景卫生缺陷 / 后续任何低队列断言的隐患**，按铁律 11 一起修）。

---

## 四、原版对应链（`CatalogItemContainer` / `ShopOfferContainer` / `ShopTabPage`）与我们哪里不同

> 判据 = `d:/2/tools/decomp_full/`（第一权威）。

| 环节 | 原版 | 我们 | 差异 |
|---|---|---|---|
| 页签对象何时建/毁 | `GameWindowWithTabs__Open.c`：`if (!initialized) SetupTabs();` → `GameWindow__Open`（只做小屏缩放 / 关模糊底）→ `CurrentTab.TryOpenTab()`。**`Open` 从不重建页签对象** | `Shell/ShopWindow.cs:150-168 Open()` = `Build()` + `BuildPages()` + `EnsureTabsSetup()` + `Click()` ⇒ **每次 `Closed ⇒ Open` 都 `DestroyImmediate` 掉上一代 5 个页对象、再新建 5 个** | 🔴 **这是根因的土壤**：原版「页对象与窗同寿」，我们「页对象与这一次 Open 同寿」 |
| 页内容刷新 | `ShopTab__CreateOffers.c`：`SupportMethods__DestroyAllChildren(param_1[5])` —— **只清 offer 挂点**（`Content` 一类的 mountPoint），再重建容器 | `ShopTabPage.Setup()` 清的是 **整页**（`_win.DestroyChildren(_root)`，`:248`）—— 头/滚动区/`ViewportClip` 一起重建 | 粒度更粗（但本身可接受）；**代价 = 页对象自身的 identity 在两次 `Setup` 之间会被别处整代替换** |
| 容器（`CatalogItemContainer` / `ShopOfferContainer`） | 由 `ShopTab.CreateOffers` 实例化到 mountPoint 下；销毁只经 `DestroyAllChildren(mountPoint)` 或随窗 `Object.Destroy` | 由 `ShopTabPage.BuildGrid`（`:499`）建 `CatalogItemShopContainer_i`，销毁只经 `Setup` 的 `DestroyChildren` | 语义对得上；**我们多了一个「页对象会在窗还活着时被换掉」的状态**，原版不存在该状态 |
| 窗销毁 | `WindowsManager__CloseWindowCO.c` 非 baseMenu 支：`Hide()` → `List.Remove` → `Object.Destroy(gameObject)` ⇒ **窗与页一起没**；挂在数据层的回调（`ShopStore` 刷新）不持有 UI 页引用 | `WindowsManager.ReapClosedWindows`（`WindowsManager.cs:1663-1675`）延后统一 `DestroyImmediate`；但**持页引用的闭包**（`ShopTabPage.cs:914` / `:790`）没人作废 | 🔴 **我们多了一类「窗/页死了、闭包还活着」的悬挂引用**（原版回调打的是数据对象，不是页） |

**⇒ 结论：`#2` 不是某一条 `SetParent` 写错，而是「页对象可被换代替换」× 「迟到闭包持有旧页」× 「`Setup()` 不校验 `_root`」三条叠出来的。** 只修其中一条即可止血；三条都写在这里，供 W1 按成本选。

---

## 五、没查清的部分（明说）

1. **`#2` 是「哪一次 `Setup()`」建的 —— 日志指名不到。**
   页 0 的 `Setup()` **全程静默**（无 `WarnNoArt`、`ItemDrawer` 的告警有「一次一物品」的静态闸），所以「多出来那次」在日志里没有任何一行；
   而本 run 里所有**能打出日志**的 `Setup` 调用者（`grep -A1 "CardPresentation.ShopTabPage:Setup ()"` 现读的直方图：`ShopWindow:BuildPages` / `ShopTabPage:DoBuy` / `OnOpen` / `B97W15Assert:Run` / `ShopScene:RunCore` / `<>c__DisplayClass68_6` / `<>c__DisplayClass67_*` 两个闭包）都落在**活页**上。
   ⇒ **这是「日志能不能证」的边界，不是「机制不成立」**。§二·3 的两条悬挂闭包里，(a) 本 run 实测打到的是活页、(b) 本 run 没点 OK —— **所以本轮没有一条闭包真的打到死页**，这与我「`#2` 必然出自死页 `Setup`」在日志层面**互相咬不上**。
   ✅ 一句话：**机制、缺环、修法都确定；「具体是哪一次调用」缺一条运行期判据**（§六·1 的探针一次跑就能指名）。
2. `#2` 究竟是**第 0 页 Cards**还是**第 3 页 VIP** —— 几何只排除了 Crates/Daily/Items（§二·4），这两页之间还差一条判据（VIP 页会多 `VIP Status`/`No VIP` 兄弟节点；`HitIdentity` 目前不打印兄弟）。
3. `#3` 的 `raycast target` 在本 run **没有**抢到平局（队列 3000 < 3031）；我只确认了它「该删没删」，**没验**它在别处是否抢过别的断言。

---

## 六、最小修法（可粘贴；**行数零变化**，符合铁律 18·b）

> ⚠️ 下面三处都是**就地改写同一物理行**，不新增行 —— 因为 `Shell/ShopTabPage.cs` / `Editor/ShopScene.cs` 都被 `账/*.tsv` 按 `文件:行号` 钉着。

### 6·1 `Shell/ShopTabPage.cs:248`（一行替换；**同时是「指名探针」**）

改前：
```csharp
            _win.DestroyChildren(_root);
```
改后（同一行）：
```csharp
            if (_root == null || _win == null) { Debug.LogWarning("[Shop] `ShopTabPage.Setup()` 打在一具**已销毁**的页上（`_page=" + _page + "`）⇒ 跳过（否则会以 `parent == null` 把整页建到场景根、长出孤儿命中区 —— 判据 → `资料/完全复刻/判据/B107_ShopScene孤儿子树.md`）；调用栈：\n" + System.Environment.StackTrace); return; } _win.DestroyChildren(_root);
```
- **判据**：原版不存在「页对象比窗先死」的状态（§四·第 1 行），所以这条守卫**不是**在改原版行为，是在把**不可能状态**挡在门口（并出声）。
- 这条**同时把 §五·1 的缺口补上**：下次 `ShopScene.Run` 一旦再命中，日志会直接印出**调用栈** ⇒ 一次就把 (a)/(b) 哪一条、哪一页指名。

### 6·2 `Editor/ShopScene.cs:5031`（同一行追加）

改前：
```csharp
            Shoot("05_商店_商品条目族骨架.png");
```
改后：
```csharp
            Shoot("05_商店_商品条目族骨架.png"); Object.DestroyImmediate(shot.gameObject);   // 🔴 B107_D1：夹具收尾（与上面 `_offerScratch`/`_offerRuler` 同一档；`shot` 自 `:5008` 建出来从未销毁 ⇒ 它的 `raycast target` 一直留在场景里）
```

### 6·3（可选，同一文件）断言：把「根级 `Packs Scroll View`」纳入卫生扫描

`Editor/B103F4_Assert.cs:579` 的 `CheckNoOrphanShopPages` **结构上看不见 `#2` 这种树**（它扫的是 `ShopTabPage` 组件）。建议在 `B103F4_Assert.cs` 类尾追加一个只读断言，并在 `:287` / `:355` 两个调用点旁边并排调用（`B103F4_Assert.cs` 在账里**零落点** ⇒ 这里加行不引起漂移）：

```csharp
    /// <summary>`B107_D1`：场上不许有**根级** `Packs Scroll View`（= 一具死页 `Setup()` 建到场景根的痕迹）。</summary>
    static void CheckNoRootShopTrees(CheckSink s, string when)
    {
        var all = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        int bad = 0; var names = new System.Text.StringBuilder();
        for (int i = 0; i < all.Length; i++)
        {
            var t = all[i];
            if (t == null || t.name != "Packs Scroll View") continue;
            if (t.GetComponentInParent<GameWindow>(true) != null) continue;
            bad++; if (names.Length > 0) names.Append('｜'); names.Append('`').Append(PathOf(t)).Append('`');
        }
        MenuCheck.True(s, bad == 0,
            "（卫生·" + when + "）场上**没有**根级 `Packs Scroll View`（= 死页 `Setup()` 建到场景根的孤儿树；判据 → `判据/B107_ShopScene孤儿子树.md`）"
            + (bad == 0 ? "（扫到 0 棵）" : " —— ⚠️ 抓到 " + bad + " 棵：" + names));
    }
```

---

## 七、验证口（改完怎么证 #2/#3 不再进命中表）

1. **一次 `ShopScene.Run` 就能两件事一起收**：
   - 若 §6·1 的守卫命中 ⇒ 日志出现 `ShopTabPage.Setup()` 打在一具已销毁的页上 … `调用栈：` ⇒ **当场指名**（把栈贴进本文件 §五·1 即可结案）；
   - 失败那条的文案里 `这一点上的全部候选 = 共 N 颗：…` 应变成 **共 1 颗**（只剩 w2 自己的 `InfoHit`）；`#2`/`#3` 两条父链都应消失。
2. **断言口**：`ClickByQuad` 的前提（`Editor/B103F4_Assert.cs:106`）转绿；加上 §6·3 之后，「（卫生·② 段 B 建 w2 之前）…根级 `Packs Scroll View`」也必须是 `（扫到 0 棵）`。
3. **静态复核（不跑 Unity）**：`grep -n "\bshot\b" Editor/ShopScene.cs` 仍只有 `:5008/:5026` 两处（§6·2 只是同一行追加，不新增引用）；`python -I -X utf8 工具/check_ledger_points.py --all-A` 仍 **漂移 0 / 失败 0**（三处都是同一行改写）；`bash 工具/typecheck.sh` 0/0。

---

## 八、给下一棒的「别推翻」

- ⛔ **别去改 `WindowsManager.ReapClosedWindows` 的静默豁免**：W79 已证它只解释「窗泄漏」、解释不了本条（inactive 窗进不了命中表）。
- ⛔ **别去改 `BuildPages()` 的 `tabs.Clear()`**：`Build()`→`BuildShell`→`DestroyChildren` 已经把旧页连树销毁，`BuildPages` 单跑那条路**永不触发**（W79 §一 B）。
- ⛔ **别把「原版也这样」当挡箭牌**：原版页对象与窗同寿，**根本不存在**「页比窗先死」这一态（§四）。
- ✅ 真改了 `.cs` 之后按铁律 12 只跑 **`ShopScene.Run` 一条**（改动只落在该宿主 + 其断言文件）。
