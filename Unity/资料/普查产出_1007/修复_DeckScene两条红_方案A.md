# 修复：DeckScene 那 2 条红（方案 A —— 抽屉加自检滚动口）

> 执行代理 · 2026-10-07 · **白名单内作业（改了 2 个文件，共 36 行新增、0 行删除）**
> 日志（只读）：`d:/4/_tmp_view/deck.log` · 修法出处 = `资料/普查产出_1007/修复_折行断言两条红.md` §3·2 表里那个 **A（推荐）**
> 类型检查：`TMPDIR=/tmp/wf_fix2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完跑了两遍，都 0）

---

## 一、结论

**做完了。**

按方案 A 落地：`DeckRuntime` 加**一个**自检口 `UiScrollFilters(dy)`（照 `UiScrollPool` 三兄弟的形状，**唯一代价 = 新增一个写口**），
探针那个块改成 **滚到底读 Cost/Type → 滚回 0 读开关/Rarity**，四族 `WrappingMode` 判据与期望值**一个字没改**。

⛔ **没动**建库行为（`:2937` 那句裁切照旧）｜⛔ 没把断言改弱（没走方案 C）｜⛔ 没动别人的节。

四条 `Check(…WrappingMode, 期望)` **现在四条都真的会执行**（此前只有两条；另两条被前置红吞掉、**一次都没跑过**）。

---

## 二、证据

### 2·1 `Deck/DeckRuntime.cs` —— 新增的那一个口（`:2224-2242`）

```csharp
        /// <summary>🆕 2026-10-07（A92 的自检口）：滚**筛选抽屉**（`_fltScroll`）—— **这是自检口，不是生产路径**
        /// （生产那条是 `HandleScroll` 里筛选那一支的滚轮，本口子照抄它算 `max` 的那一句）。
        /// `dy > 0` = 往下滚（符号同 <see cref="UiScrollPool"/>）；夹在 `[0, 内容高 − 抽屉高]`。
        /// **返回夹完之后的新滚动位** —— 自检靠它把「读完整回 0」钉成一条断言，
        /// 而**不必再开第二个只读口**（滚 `1e6` 会停在 `ContentHFor(State) − FltH`，出参就是这个数）。
        /// 🔴 **为什么夹在这里、不夹进 `RefreshFilters`**：`RefreshFilters` 是**建库路径**（本次一个字不动），
        ///   而 `_fltScroll` 眼下只有 `HandleScroll` 那一句 `Clamp` 定了范围（另一个写点是 `ClearFilters` 的归 0）
        ///   ⇒ 照抄那一句，语义一致。
        /// 🔴 **它不改任何建库行为**：`RefreshFilterCells` 里「滚出面板的不建」那句裁切照旧 ——
        ///   本口子只是把自检挪到**看得见那些格子的滚动位**上（本窗 `Factions()` 恒 13 ⇒ Army 行 550 高
        ///   ⇒ `ContentH = TypeTop 1239.02 + 150 = 1389.02` > 可见 924.1 ⇒ 可滚 464.92；
        ///   而 Cost/Type 两族首格在滚到 0 时的绝对 y = **1230.02 / 1470.02**，都在可见带
        ///   `[FltY, FltY+FltH] = [156, 1080.1]` **外** ⇒ 恒被那句 `continue` 跳过、连标签一起不登记）。</summary>
        public float UiScrollFilters(float dy)
        {
            float max = Mathf.Max(0f, FilterPanelModel.ContentHFor(State) - FltH);
            _fltScroll = Mathf.Clamp(_fltScroll + dy, 0f, max);
            RefreshFilters();     // 建库路径本身一字未改：重建 = `ClearFilterCells` + 只建可见带内的格子
            return _fltScroll;
        }
```

**逐条对照方案 A 的要求：**

| 要求 | 落地 |
|---|---|
| 照 `UiScrollPool` 的形状 | 同一簇（`:2222-2223` 是 `UiScrollPool`/`UiScrollDeck`，本口紧接其后）· 同一套语义（`dy > 0` = 往下滚）· 同样自己不看状态、直接改字段后重建 |
| 已有更合适的口就用它 | **查过，没有** —— `_fltScroll` 全仓写点只有三处（`:2707` `HandleScroll` · `:2801` `ClearFilters` · 本口），**没有任何自检口**（`UiScrollPool`/`UiScrollDeck`/`UiScrollCosmetics` 三口都在，独缺这一个） |
| 只加**这一个**口 | 只新增 **1 个成员**（**+20 行**）· **不另开只读口** —— 末态读回靠**返回值**，见 2·3 |
| ⛔ 别动 `:2917` 那句裁切 | **一个字没动**（现 `:2937`，内容逐字相同：``if (b.y2 < FltY \|\| b.y1 > FltY + FltH) continue;      // 滚出面板的不建``） |
| ⛔ 别动任何建库行为 | `RefreshFilters` / `RefreshFilterCells` / `FilterPanelModel` **全未出现在本次 diff 里**（见 §四 `git diff --unified=0` 的 hunk 清单） |
| 注释写明「这是自检口，不是生产路径」 | 第一个 `🔴`/`<summary>` 第一行就有 |

**为什么夹在这里而不是 `RefreshFilters` 里**：`RefreshFilters` 是**建库路径**（白名单里那句「别动任何建库行为」罩着它）；
而 `_fltScroll` 的范围定义只有 `HandleScroll:2707` 那一句
`float max = Mathf.Max(0f, FilterPanelModel.ContentHFor(State) - FltH); _fltScroll = Mathf.Clamp(_fltScroll - step, 0f, max);`
⇒ 口子**照抄这两行**（只把 `- step` 换成 `+ dy`，因为口子的 `dy` 就是「往下滚多少」）。**范围与生产路径同一个定义式。**

### 2·2 `Editor/DeckScene.cs` —— 那个块（现在 `:2211-2246`）

```csharp
                // ---- A62 #5：四族筛选格标签的折行（**只有费用桶折行**）----
                // 🔴 **2026-10-07（A92）：必须先【滚到底】才读得到费用桶/类型这两族** ——
                //   `RefreshFilterCells` 里那句「滚出面板的不建」（`if (b.y2 < FltY || b.y1 > FltY + FltH) continue;`）
                //   把**没进可见带**的格子连标签一起跳过（标签的登记在同一个 `continue` 之后）。
                //   本窗 `Factions()` 恒 13 ⇒ Army 行 550 高 ⇒ `CostTop 1009.02 / TypeTop 1239.02`
                //   ⇒ 滚到 0 时这两族首格的绝对 y = **1230.02 / 1470.02**，都在可见带
                //   `[FltY, FltY+FltH] = [156, 1080.1]` **外**（`$owned` 在 235 · `$rar:common` 在 975 ⇒ 那两条过得去）
                //   ⇒ 旧写法在这里拿到的**恒是 null** —— 那两条真判据（类型 `0` / 费用桶 `1`）**一次都没执行过**，
                //   所以「今天实际设成了几档」此前**没有读数**。
                //   做法 = 照本文件既有的 `UiScrollCosmetics(1e6f) → 读 → (−1e6f)` 那个形状：
                //   **滚到底读 Cost/Type → 滚回 0 读开关/Rarity**；四族的 `WrappingMode` 判据与期望值**一个字没改**。
                //   ⚠️ 建库行为一个字没动（那句裁切照旧）—— 自检口见 `DeckRuntime.UiScrollFilters`（**它是自检口，不是生产路径**）。
                float fltMax = _rt.UiScrollFilters(1e6f);      // 滚到底（口子里按 `HandleScroll` 那份 max 夹住）
                CheckTrue(fltMax > 0f, $"（前提）筛选抽屉**滚得动**（滚到底 = {fltMax:F2}px > 0）");
                var typeLb = _rt.UiFilterCellLabel("$type:" + FilterPanelModel.TypeKeys[0]);
                CheckTrue(typeLb != null && typeLb.CanRenderChinese, "（前提）类型那族的标签在且是真 TMP");
                if (typeLb != null && typeLb.CanRenderChinese)
                    Check(typeLb.WrappingMode, 0, "★ 类型族 = 原版 **`折行=0`**");
                var costLb = _rt.UiFilterCellLabel("$cost:" + FilterPanelModel.CostBuckets[0].Lo);
                CheckTrue(costLb != null && costLb.CanRenderChinese, "（前提）费用桶那族的标签在且是真 TMP");
                if (costLb != null && costLb.CanRenderChinese)
                    Check(costLb.WrappingMode, 1,
                          "★ 费用桶 = 原版 **`折行=1`**（四族里**唯一**折行的那一族 —— 一刀切成 false 会把它改错）");
                // 读完整回 0 —— **这不只是收尾，是后面同一宿主里所有断言的前提**（`_fltScroll` 会留在库里）。
                Check(_rt.UiScrollFilters(-1e6f), 0f,
                      "★ 读完**滚回 0**（末态滚动位 = 0；不回 0 会把后面读抽屉的断言全污染）");
                var ownedLb = _rt.UiFilterCellLabel("$owned");
                CheckTrue(ownedLb != null && ownedLb.CanRenderChinese, "（前提）`$owned` 那格的标签在且是真 TMP");
                if (ownedLb != null && ownedLb.CanRenderChinese)
                    Check(ownedLb.WrappingMode, 0, "★ 开关行 `'Owned only'` = 原版 **`折行=0`**（关着）");
                var rarLb = _rt.UiFilterCellLabel("$rar:" + FilterPanelModel.RarityKeys[0]);
                CheckTrue(rarLb != null && rarLb.CanRenderChinese, "（前提）稀有度那族的标签在且是真 TMP");
                if (rarLb != null && rarLb.CanRenderChinese)
                    Check(rarLb.WrappingMode, 0,
                          "★ 稀有度 `'Legendary'` = 原版 **`折行=0`** —— ⛔ 别按 `LabelCenter` 反推："
                          + "这一族是 `LabelRight`，照中心取反会**静默漏掉它**（旧口径就是这么漏的）");
```

**顺序**（照要求 4 那一档写的「滚到底读 Cost/Type → 滚回 0 读开关/Rarity」）：
`滚到底(1e6)` → 前提「滚得动」 → **类型族**判据 → **费用桶**判据 → **滚回 0** + 前提「末态 = 0」 → **开关行**判据 → **稀有度**判据。

> ⚠️ 实测确认过「滚到底之后 Rarity/开关真的会滚出去」不是必须的（算下来 `$rar:common` 在滚到底时仍在带内，`$owned` 会出去）——
> **仍然按裁定把四族都放进这个块**：这样就算 `Factions()` 的档数将来变了（Army 行高跟着变）、或那句裁切将来被按铁律 11 补成正版口径，
> 这个块的成立条件也只依赖「**末行那两族在滚到底时进来**」这一条（那一条是**行高无关**的，见 §四的推演）。

### 2·3 四条 `WrappingMode` 断言都在、期望值没变

| # | 键（表达式逐字保留） | `Check( …, 期望 )` | 现在在 `DeckScene.cs` | 改前 |
|---|---|---|---|---|
| 1 | `"$type:" + FilterPanelModel.TypeKeys[0]` | `0` | `:2228` | `:2225`（同一个字面量） |
| 2 | `"$cost:" + FilterPanelModel.CostBuckets[0].Lo` | `1` | `:2232` | `:2229`（同一个字面量） |
| 3 | `"$owned"` | `0` | `:2240` | `:2215`（同一个字面量） |
| 4 | `"$rar:" + FilterPanelModel.RarityKeys[0]` | `0` | `:2244` | `:2219`（同一个字面量） |

四条**源码逐字未改**（只挪了位置：1、2 从「块末」挪到「滚到底之后」，3、4 从「块首」挪到「滚回 0 之后」）。
判别力未降级：读的还是**渲染出来的真 `Label`**（`Label.WrappingMode`），⛔ **没走方案 C**（没改读模型 `Cell.LabelWrap`）。

**「不是被 `if` 拦住」的证明**：四条外面只有**同一条**前置守卫 `if (x != null && x.CanRenderChinese)`（原有写法，未改），
而它上面的 `CheckTrue(x != null && x.CanRenderChinese, "（前提）…")` 现在会绿（§四推演）⇒ `if` 为真 ⇒ **`Check` 真的执行**。
另注：`Check` 里那个 `if` 是 `Check<T>` 自己的实现（`:79-87`），四条原来也是这么写的。

### 2·4 「末态滚动位 = 0」那条断言（要求 3）

`Editor/DeckScene.cs:2235-2236`：

```csharp
                Check(_rt.UiScrollFilters(-1e6f), 0f,
                      "★ 读完**滚回 0**（末态滚动位 = 0；不回 0 会把后面读抽屉的断言全污染）");
```

- **读的是真值**：`UiScrollFilters` 返回的是**夹完之后的 `_fltScroll` 字段本身**（`DeckRuntime.cs:2241`），
  不是探针自算的一个数。
- **它同时是收尾动作**：`-1e6f` 经 `Mathf.Clamp(…, 0f, max)` 必然落到 **正好 `0f`**（`max ≥ 0`）⇒ 断言成立 + 状态复位一次做完。
  `Check<float>` 比的是 `EqualityComparer<float>.Default.Equals`，`0f == 0f` 精确相等（本文件既有先例：`:2054` `Check(_rt.PoolScrollPx, 0f, …)`）。
- **前提意义**：这一节后面还有 3 组读抽屉的断言（页签名牌 `:2248-2258` · 卡背抽屉 `:2260-2269` · 收尾两句 `:2270-2275`），
  它们的可见性都以「`_fltScroll = 0`」为默认态 ⇒ 不滚回去就是污染。
  另注：`UiSetTab` 只归零 `_deckScroll`（`:2791`），**不管 `_fltScroll`** ⇒ 必须由这个块自己钉住。

---

## 三、没查清的部分（⛔ 不猜）

1. **「我们实际设成了几档」仍然没有读数** —— 本轮**没跑 Unity**（铁律 12），所以那两条 ★ 判据**执行结果未知**。
   静态上应当绿（§四），但**绿没绿要等调度台复跑 `DeckScene.Run`**。
   ⚠️ 别把 §四的推演读成「已验过」。
2. **`WrappingMode` 读到 −1 的可能性**：约定是点阵后端返 −1。本窗既有的 6 条 `WrappingMode` 断言（页签 3 · 面板输入框 1 · `$owned` 1 · `$rar` 1）
   在 `deck.log` 里**全是绿的** ⇒ 本宿主是真 TMP 后端；但那两条新执行的**没跑过**，不排除它们的 `Label` 走了别的分支。
   前置守卫会把这种情况变成「前提红」而不是「判据红」—— **真出现了请按前提红处理，别去改期望值**。
3. **`UiScrollFilters` 滚到底之后，`_fltHit`（点击区）里会多出 Cost/Type 那 11 格** —— 它们此时**在屏内**（abs y 765–1055 与 955–1055）。
   §四推演里没有依赖 `_fltHit` 的断言；但**这个副作用只在本口子跑过之后存在**，而口子结束会把滚动位归 0、
   下一个会重建抽屉的动作（关抽屉 / 换 tab）会把它清掉。**没有实测**（没有 Unity 跑），只做了静态核对。
4. **`$rar` 那条断言的消息与读到的格子对不上**（顺手发现，**我没改**，见 §五·2）。

---

## 四、改动清单 + 「改后为什么应当绿」的静态推演

### 4·1 逐文件

| 文件 | 改了什么 | `git diff --numstat`（**本会话快照 · 含本轮别的写手的未提交改动**） | 我这一笔 |
|---|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs` | 新增自检口 `UiScrollFilters`（`:2224-2242`） | `132  10`（改前 `112  10`） | **+20 / −0** |
| `Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs` | 重写 A62 #5 那个块（`:2211-2246`） | `173  0`（改前 `157  0`） | **+16 / −0** |

**`git diff --unified=0` 的 hunk 清单（证明没碰别处）：**

- `DeckRuntime.cs`：唯一属于我的是 `@@ -2211,4 +2222,23 @@` 里那段纯新增（19 行，紧接 `UiScrollDeck` 之后）。**其余 hunk 全是本轮别的写手的**（改前就在）。
- `DeckScene.cs`：只有两个 hunk（`@@ -1056,0 +1057,45 @@` = A77 ⑭ 的节 · `@@ -2103,0 +2149,128 @@` = **含我的块**）。
  我的那一笔在第二个 hunk 内、且**只有新增、没有删除**（`173 → 0 removed` 这个数改前改后都是 0）。

**行尾核对（二进制数，改完立刻做）：**

| 文件 | 改前 | 改后 | 判定 |
|---|---|---|---|
| `Deck/DeckRuntime.cs` | `CRLF=3839 / LF=3839` | `CRLF=3859 / LF=3859` | ✅ 未翻（`CRLF == LF` ⇒ 全部换行都是 CRLF） |
| `Editor/DeckScene.cs` | `CRLF=2416 / LF=2416` | `CRLF=2432 / LF=2432` | ✅ 未翻 |

用的是 **Edit 工具**（不是 `sed -i`、不是 python 文本写）；改完各数了一次。

**类型检查**：`TMPDIR=/tmp/wf_fix2 bash d:/4/Unity/工具/typecheck.sh`
（独立 `TMPDIR` 已带）⇒ `运行时错误数: 0` / `编辑器错误数: 0`。两遍都 0（改口子后一遍、订正注释后一遍）。

### 4·2 「改后为什么应当绿」的静态推演（逐步可核）

**① 滚到底之后，Cost / Type 的首格**必然**进可见带 —— 而且**与 Army 行高无关**（这是本修法稳的根）

- `FltY = 156`、`FltH = 924.1`（`DeckRuntime.cs:196`）⇒ 可见带 abs y = `[156, 1080.1]`
- 内容总高 `ContentH = TypeTop + RowTypeH`，而 `RowTypeH = 150`（`FilterPanelModel.cs:47`）、`TypeContentTop = 50`（`:198`）、`TypeIconInsetY = 25`（`:199`）
- 滚到底 ⇒ `_fltScroll = ContentH − FltH` ⇒ **Type 首格 `Bg` 的 abs y₁ = `FltY + (TypeTop + 50 + 25) − (TypeTop + 150 − FltH)` = `156 + 924.1 − 75` = `1005.10`**
  （`TypeTop` 被消掉了 ⇒ **本窗 Army 13 档算出来的 1239.02 只是代入值，不是成立条件**）
  裁切判据：`b.y1 > 1080.1`？`1005.10 > 1080.1` = 否 ⇒ **不裁**（余量 75px）✅
- 同理 **Cost 首格 `Bg`**：`= FltY + (CostTop + 65) − (CostTop + 230 + 150 − FltH)` = **`765.10`**；`y₂ = 830.10` ⇒ **不裁** ✅
- 两格的 `Bg` 都不裁 ⇒ `:2974` `_fltCellLabels[c.Key] = lb;` **走得到** ⇒ `UiFilterCellLabel("$type:hero")` / `("$cost:1")` 非 null

**② 真 TMP 那一半**：这两族的 `Label` 与 `$owned`/`$rar` **同一条建法**（同一个 `Label.Create` + `SetAutoFitBox`，`DeckRuntime.cs:2955-2982`），
而后两条在本宿主里 `CanRenderChinese` 实测为真（`deck.log:9432` / `:9459`）⇒ 前提守卫为真 ⇒ **`Check` 执行**。

**③ 期望值那一半（读渲染侧的实现）**：`DeckRuntime.cs:2982` `lb.SetWrapping(c.LabelWrap == 1);`，
而 `FilterPanelModel.cs` 里四族逐条写死：`Cost LabelWrap = 1`（`:401`）· `Type LabelWrap = 0`（`:421`）· 开关 `0`（`:352`）· 稀有度 `0`（`:384`）。
再核 `SetWrapping(bool)` → `WrappingMode` 的映射在本宿主里已经被验过：页签 0/1 期望 `1` **实测绿**（`deck.log:9528 / :9555`），
页签 2 期望 `0` **实测绿**（`:9582`）⇒ 两条映射都成立。
⇒ 四条期望 `0 / 1 / 0 / 0` 都应成立。**（这是推演，不是读数；见 §三·1。）**

**④ 新增两条断言的成立性**：
- `fltMax > 0f`：`ContentH 1389.02 − 924.1 = 464.92 > 0` ✅（`Factions()` 恒 13 有断言钉着：`Editor/DeckScene.cs:515`、实测 `deck.log:473`）
- 末态 `= 0f`：`Clamp(≤0 的数, 0f, max)` ⇒ **恰好 `0f`** ✅

**⑤ 断言计数会怎么变**（供调度台对数）：
- 2 条「（前提）…在且是真 TMP」：✗ → ✓
- 2 条 ★ `WrappingMode`：**此前根本没被调用**（被 `if` 挡掉）⇒ 现在**首次进总数**
- 2 条我新加的前提/收尾（`滚得动` · `末态 = 0`）
⇒ DeckScene **失败 2 → 0**（若实现侧正如 ③ 所推）；**断言总数 +4**，通过数 +6。
⚠️ 别用「总数没变」当判据 —— **总数本来就会涨**。

---

## 五、顺手发现（⛔ 我没改，交调度台分流）

1. **`Editor/DeckScene.cs:2244-2246` 那条稀有度断言读的是 `$rar:common`，消息里却写 `'Legendary'`**
   —— `FilterPanelModel.cs:218` `RarityKeys = { "common", "rare", "epic", "legendary", "special" }` ⇒ `RarityKeys[0] = "common"`，
   渲染出来的标签是 `Common` 不是 `Legendary`（`RarityNames[0] = "Common"`，`:220`）。
   **实质判据没错**（同一循环里 5 档的 `LabelWrap` 都是 `0`，读哪档都一样），**是消息写错了格子**。
   ⛔ 我**没动消息**（动它就改了断言文案，超出「只许动这个块里的滚动」这条线）。
   修法很简单：要么把键改成 `$rar:legendary`（`Array.IndexOf`/`RarityKeys[3]`），要么把消息里的 `'Legendary'` 改成 `'Common'`。**请调度台裁。**
2. **`DeckRuntime.cs:3110` 还有第二处 `lb.SetWrapping(c.LabelWrap == 1);`**（`_cosmoFltLabels` 那条路 = 卡背抽屉）。
   与本件无关（卡背抽屉不滚，`:3059` 已记「内容 628 < 抽屉 924 ⇒ 不用滚动」），**只是记录这一族有两个建点**，
   将来若给卡背抽屉也加滚动，**两处要一起看**。
3. **`m_TextWrappingMode = 7` 那条 warning（`deck.log:9308`）是【负例】**（`DeckScene.cs:2186-2188` 的「传一个不存在的档(7) ⇒ 不改」），
   **不是异常、与本件无关** —— 记一笔免得下一个会话看到 warning 就回头查。

---

## 六、给调度台的一句话

**方案 A 已落地（2 文件 / +36 行 / 0 删除 / 行尾未翻 / 类型检查两遍全 0）**；
四条 `WrappingMode` 判据的**期望值一个字没改、四条现在都会真执行**，末态滚动位有断言钉住 `0f`；
**请复跑 `DeckScene.Run`**（本件只碰 `DeckScene` 这一条宿主，`grep` 过没有共用件改动 ⇒ 按铁律 12 只跑这一条即可），
顺带把 §五·1 那条「消息说 Legendary、读的却是 Common」分流。
