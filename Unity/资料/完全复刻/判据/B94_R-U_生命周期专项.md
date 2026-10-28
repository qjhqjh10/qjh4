# B94 · R-U · 生命周期方法落点专项体检

> 只读账务代理 R-U（第九十四会话 · 波 7）。**只产本文件；未改任何源码 / 账 / 正本，未跑 Unity，未动 git。**
> 数据源 = `账/*.tsv` **现读（2026-10-29）**；原版体读法：① `D:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/<类>.cs`（优先，含真体 + `// RVA:`）② `D:/2/tools/decomp_full/<类>__<方法>.c` ③ 泛型走 `D:/2/tools/decomp_generic_trial/out_b94/`。
> **取活口径**：`方法` 列命中 `OnEnable|OnDisable|Awake|OnDestroy|OnOpen|OnClose|Start|Reset` + 括号。正文分两组：**①B/C 行（21 条，其中 1 条是正则误中）** · **②A 行且 `备注` 含「异名但等价」（22 条，本批核心）**。
> 与 `B94_R-S_落点错配_第9批.md` 的分工：R-S 判的是**它自己那 50 条抽样**里的 8 条生命周期；本文件是**全表生命周期专项**，把 R-S 那 8 条一并交叉复核（**发现 R-S #7 的证据有一处错**，见 §二·A #13）。

---

## §一 汇总

### 一·A　B/C 档生命周期行（21 条命中，20 条真生命周期）

> 全部**没有**一条带「异名但等价」（该句 327 处**全在 A/D 行**，见 §三）。这 20 条是 B93/B94 各批**已经单独核过**的，落点行号 **20/20 命中现读**。

| 档 | 条数 | 行号 |
|---|---:|---|
| **真等价**（行为已投影，B 只因资源/装载层/宿主未接线） | **10** | 商店:95 · 日常:160 · 日常:226 · 日常:621 · 日常:732 · 日常:774 · 未分类:178 · 泛型:578 · 轮抽:114 · 轮抽:147 |
| **无等价物**（原版体 = 解绑 / 释放远端资产 / 挂不存在的单例 ⇒ 本仓无对应物） | **8** | 商店:12 · 日常:53 · 日常:229 · 日常:232 · 日常:612 · 日常:617 · 日常:709 · 社交:15 |
| **指到绑定点** | **2** | 商店:13 · 日常:94 |
| **空桩**（原版体空 ⇒ 应判 D） | **0** | — |
| **非生命周期（正则误中）** | 1 | 日常:55 `ChallengeManager.TryChallengeMatchStart(...)` —— 名字里含 `Start(` 被误抓，**不是** Unity 生命周期 |

> 结论：**B/C 这一档现在是干净的** —— 8 条「无等价物」的 `我们的落点` 列已经写成「无对应物 / 空跳 / 投影」，没有一条还挂「等价」。这是 R-S 反映的病灶**已经被 B93/B94 后续批次修在 B/C 档上**的证据。

### 一·B　A 档 + 「异名但等价」的生命周期行（22 条，本批核心）

| 档（按现读真值） | 条数 | 行号 |
|---|---:|---|
| **真等价（成立，可留 A）** | **8** | 卡组编辑:40 · 卡组编辑:219 · 商店:124 · 商店:198 · 商店:301 · 收藏:2 · 收藏:340 · 收藏:344 |
| **指到绑定点（应改落点，A 可留）** | **5** | 卡组编辑:213 · 卡组编辑:266 · 商店:186 · 商店:190 · 收藏:176 |
| **无等价物（应改落点，⛔ 不再写「异名但等价」）** | **7** | 卡组编辑:43 · 卡组编辑:150 · 卡组编辑:265 · 商店:185 · 商店:189 · 商店:197 · 收藏:175 |
| **空桩（应 A→D）** | **2** | 商店:247 · 商店:248 |

- **错配/误标率 = 14 / 22 = 64%**（13 条明确错 + 1 条边界 `商店:197`）。
- 其中 **8 条 R-S 第 9 批已经抓出**（`商店:185/186/189/190` · `商店:247/248` · `收藏:175/176`）—— **但账里一个字都还没改**（R-S 也是只读）。
- **R-S 没覆盖的 10 条里，本文件新查出 5 条错配**：`卡组编辑:43 / 150 / 213 / 265 / 266`；另 4 条成立（`:40 / :219 / 商店:124 / 收藏:2 / :340 / :344` 中的对应项）、1 条边界订正（`商店:197` 的证据错，见 §二·A #13）。

### 一·C　`异名但等价` 这句话的分布

| 项 | 现读 |
|---|---|
| 全账命中 | **327 处**（A **326** · D **1**） |
| 按文件 | `卡组编辑.tsv` **142** · `收藏.tsv` **107** · `商店.tsv` **78** |
| 其中落在**生命周期方法**上 | **22 处**（见一·B）⇒ **只占 6.7%** |
| 其余 305 处 | 绝大多数是**数据/装配方法**（`Initialize` 18 · `FilterItem` 7 · `FillToggleList` 7 · `Refresh` 6 · `GetCollection` 5 · `SetCell` 3 · `AddCard` 2…）—— 这类「异名但等价」**多数是真的**（R-S 在 50 条抽样上实测真等价率 76–88%） |

---

## §二 逐条

> 列：`账行 | 原版类.方法 | 现落点(现读) | 档位 | 证据（两侧） | 该怎么改账`。
> 原版体证据格式：`il2csharp <文件>:<行号>（RVA …）` 或 `.c:<行>`。

### 二·A　A 档 + 「异名但等价」（22 条）

**1 · 卡组编辑:40 · `CollectionDeck.Awake()` · `CollectionWindow.cs:5259 BuildDeckCell` · ✅ 真等价（该层不建）**
- 原版 `il2csharp CollectionDeck.cs:34-39`（RVA `0x6DA030`）**全篇一句**：`this.deckDifficulty.gameObject.SetActive(false);`。
- 我们 `BuildDeckCell`(:5259) → `MenuDraw.DeckCell(... gameMode: info.GameMode)`，**没传 `difficulty/showDifficulty`** ⇒ `MenuDraw.cs:3513` 的 `if (showDifficulty && difficulty.HasValue)` **恒假** ⇒ 收藏页的卡组格**从不画难度角标**。可观察结果与原版「Awake 把它关掉」一致。
- 改账：**可留 A**，但落点建议收到 `Shell/MenuDraw.cs:3513`（`showDifficulty` 缺省 false 那一闸），并在 `差异` 写明「我们不是 SetActive(false)，是**不建那一层**（同一可观察结果）」。

**2 · 卡组编辑:43 · `CollectionDeck.OnDestroy()` · `CollectionWindow.cs:5259 BuildDeckCell` · ❌ 无等价物（不是「异名但等价」）**
- 原版 `il2csharp CollectionDeck.cs:126-143`（RVA `0x6DA710`）：`if (CardDeck != null) ScriptableObjectsCollectionsUtilities.Instance.ReleaseGameModeIcon(gamemode)` —— **Addressables 引用释放**。
- 我们：`Resources` 直取、**无 Addressables 引用计数** ⇒ **没有可释放的东西**；格随 `RebuildDeckCells` 重建丢弃（那是销毁，不是释放）。
- 改账：`我们的落点` → **「（无等价物）原版 = `ReleaseGameModeIcon`（Addressables 释放）；本仓 Resources 直取、无引用计数 ⇒ 无对象可释放」**；`备注` 把「异名但等价」改成「无等价物」。

**3 · 卡组编辑:150 · `DeckInfoControls.OnDisable()` · `DeckInfoPopup.cs:941 ApplyControlStates` · ❌ 错配（落点指到了另一个原版方法）**
- 原版 `il2csharp DeckInfoControls.cs:133-144`（RVA `0x70D600`）= **纯解绑**：7 颗钮 `RemoveClickListeners()` + `toggleDrawerButton.onValueChanged.RemoveAllListeners()`（绑定在 `Initialize`，`:78-108` 有 8 处 `AddListener`）。
- 我们 `ApplyControlStates()` 的文件内 doc **自己写着**「原版 `Initialize` 末尾那三条「接线层」—— 唯一实现」（`:928`）⇒ 它是 **`Initialize`** 的等价物，**不是 `OnDisable`**。
- 绑定点（可指）：`Shell/DeckInfoPopup.cs:1571`（`Btn:{Practice,Edit,Select} Deck` 的 `Hit` 闭包）+ `:1675`（`Opt:{Share,Share On Chat,Delete,Duplicate,Switch Deck Info}` 的 `Hit` 闭包）。
- 改账：`我们的落点` → **「（无等价物）纯解绑；绑定处 = `:1571` / `:1675` 的一次性闭包（随 `Build()` 重建，无 per-cell 监听可摘）」**；`备注` 删掉「异名但等价」。

**4 · 卡组编辑:213 · `DeckSelectionTabController.Awake()` · `DeckSelectionPopup.cs:372 Create` · ❌ 错配（Create 只是工厂，不含 Awake 的任何一件）**
- 原版 `il2csharp DeckSelectionTabController.cs:23-96`（RVA `0x713C60`）五件：① 用 `CollectionManager.DeckCollection.practiceDecks` 过滤→`Select`→过滤→`OrderBy(difficulty).ThenBy(army)` 建 `botDecks`（`:55-78`）② `prebuiltDecksToggle.onValueChanged += ShowPrebuiltDecks`（`:79-81`）③ `ownDecksToggle.onValueChanged += ShowOwnDecks`（`:82-84`）④ `randomDeckButton.onClick += RandomizeDeck`（`:85-87`）⑤ `searchBar != null ⇒ onValueChanged += …`（`:88-93`）。
- 我们 `Create(:372-386)` 只 `new GameObject → AddComponent → 赋 type/placement/字断 → AttachToAnchor`，**不建表、不绑任何监听**（`AttachToAnchor`(`WindowsManager.cs:498`) 也不调 `Open()`）。真正的对应物分散在：`Build()`(`:470`，页头/随机钮/列表) · `:596 Hit("RandomHit", PickRandom)` · `:763 Hit("TabHit_…", SwitchTab)` · `Shown()(:432)` 读 `PrebuiltDecks.Tab`；搜索框**照原版不建**（`:779`）。
- 改账：`我们的落点` → **「（无单点等价物）建窗 = `Create:372`，但原版 Awake 的五件事落在 `Build():470` / 随机钮 `:596` / 页签 `:763` / 列表 `Shown():432`（数据表 = `PrebuiltDecks.Tab`）；搜索框照原版不建」**；状态建议 A→B（多方法覆盖，方案 §4.1）。

**5 · 卡组编辑:219 · `DeckSelectionTabController.Start()` · `DeckSelectionPopup.cs:397 Open` · ✅ 真等价**
- 原版 `il2csharp :123-127`（RVA `0x714A10`）**全篇一句**：`this.ShowPrebuiltDecks(true);`。
- 我们 `Open()`(`:397`) 首句 `OwnDecks = false;`（= 落在「预组卡组」页，doc 自己写明这一跳），再 `Build()`。⇒ 逐句同义。**可留 A。**

**6 · 卡组编辑:265 · `ImportDeckPopup.OnDisable()` · `ImportDeckPopup.cs:314 RefreshInputText` · ❌ 错配（落点是「画输入框那行字」）**
- 原版 `il2csharp ImportDeckPopup.cs:48-63`（RVA `0x714F90`）= **纯解绑**：`closeButton.m_OnClick` / `backgroundCloseButton.m_OnClick` / `importButton.m_OnClick` / `inputField.m_OnValueChanged` 四处 `RemoveListener`。
- 我们 `RefreshInputText()`(:314) = 重建 `Input Text` 那个 `Label`（占位符/正文 + 对齐 + 字号）；与解绑无关。
- 改账：`我们的落点` → **「（无等价物）纯解绑；本窗按钮是 `Hit` 闭包（`:167` 背景 / `:258` Ok / `:282` Close），随节点销毁」**；`差异` 里那条「K3 改成取最后一行非空」与本方法无关（那是 `TryImport`），应移走。

**7 · 卡组编辑:266 · `ImportDeckPopup.OnEnable()` · `ImportDeckPopup.cs:314 RefreshInputText` · ❌ 错配（应指 `Open` + 绑定处）**
- 原版 `il2csharp :25-46`（RVA `0x715150`）= 建 4 处监听 + `inputField.text = ""` + `errorMsg.SetActive(false)` + `importButton.interactable = false`。
- 我们：`Open()`(`:148`) = `_text = ""; _error = ""; Build();` ⇒ 清空输入 + 隐藏错误（`Build` 里 `:228` 画空 `_error`）；绑定在 `Build`：`ShadeHit(:167)` / `OkHit(:258)` / `CloseHit(:282)`。
- ⚠️ **附带缺口**：原版那句 `importButton.interactable = false`（空输入时不可点）**我们没做**（`OkHit` 无 interactable 闸，靠 `TryImport` 里报错兜）。判据 = 本文件 `:148/:258` 现读无该赋值。
- 改账：`我们的落点` → **`Shell/ImportDeckPopup.cs:148（Open：清输入 + 清错误）+ :258/:282/:167（三处绑定）（等价实现 = 原版 OnEnable 的建监听 + 清态；⚠️ `interactable=false` 那一半未做，记缺口）`**；状态建议 A→B。

**8 · 商店:124 · `GeneralOfferPopupDrawerBase.DoReset()` · `BaseOfferPopup.cs:725 Build` · ✅ 真等价（机制差）**
- 注意：本行 `原版类` 是**抽象基类**，`il2csharp GeneralOfferPopupDrawerBase.cs:36` = `public abstract void DoReset();`（**基类无体**）。真体在两个子类：`GeneralOfferPopupDrawer.cs:161`（RVA `0x791510`）= 逐个 `gameObject.SetActive(false)` + `ReleaseAssets()`；`CarouselOfferPopupDrawer.cs:122`（RVA `0x7FE040`）= `ReleaseAssets()` + `Destroy(gameObject)` + 清 list。调用点 `BaseOfferPopup.cs:91`（`OverrideVisualRewards` 里 `DoReset(); DrawRewards(...)`）。
- 我们 `BaseOfferPopup.Build(:725)` = `ClearChildren` + 全表重建（`OverrideVisualRewards` 等价路也走重建）⇒ 可观察 = 旧抽屉全消失。**可留 A**，但落点/判据出处应改指**子类真体**（现在引的是基类抽象声明 `:47`，那是「没有体」的地方）。

**9 · 商店:185 · `PuchasePremiumArmyContainer.OnDisable()` · `PurchasePremiumWindow.cs:459 ApplyArmyInfo` · ❌ 无等价物（R-S #3）**
- 原版 `.c:15-31` = `toggle.onValueChanged.RemoveListener(Action<bool>)` + `ArmyUtilities.ReleaseMediumBackGround(army)`（R-S 已读）。
- 我们 `ApplyArmyInfo(:459)` = **按当前选中重刷右栏**（那是 `PuchasePremiumArmyInfo.Initialize` 的可见半），与「退订 + 释放底图」无关。
- 改账：见 R-S §三 #1（落点 → 「（无等价物）…」；`差异` 改成「旧落点把右栏内容重刷当成了本方法的实现」）。

**10 · 商店:186 · `PuchasePremiumArmyContainer.OnEnable()` · `ApplyArmyInfo` · ❌ 应指绑定点（R-S #4）**
- 原版 `…__OnEnable.c:12-25` = 只有一句 `toggle.onValueChanged.AddListener(Action<bool>)`（该 lambda `DAT_1842b7e28` **无导出产物**）。
- 改账：落点 → `Shell/PurchasePremiumWindow.cs:762（Hit → SelectArmy(idx)）/ :767（cWb.isOn = () => _selectedArmy == idx）`（R-S §三 #2）。

**11 · 商店:189 · `PuchasePremiumArmyInfo.OnDisable()` · `ApplyArmyInfo` · ❌ 无等价物（R-S #5）**
- 原版 `…__OnDisable.c:12-20` = `purchaseButton(0x38).m_OnClick.RemoveListener(PurchaseButtonClick)`（纯解绑）。
- 改账：落点 → **「（无等价物）纯解绑；绑定处 = `:834`」**（R-S §三 #2）。

**12 · 商店:190 · `PuchasePremiumArmyInfo.OnEnable()` · `ApplyArmyInfo` · ❌ 应指绑定点（R-S #6）**
- 原版 `…__OnEnable.c:12-20` = `purchaseButton.m_OnClick.AddListener(PurchaseButtonClick)`。
- 改账：落点 → `Shell/PurchasePremiumWindow.cs:834（priceHit = MenuDraw.Hit(…, () => PurchaseButtonClick())）`（R-S §三 #2）。

**13 · 商店:197 · `PurchasePremiumWindow.OnDisable()` · `GameWindow.cs:714 Close` · ⚠️ 边界：R-S 判「真等价」，体却是**纯解绑**（且 R-S 的证据写错）**
- 原版 `il2csharp PurchasePremiumWindow.cs`（RVA `0x83D850`）= `closeButton.m_OnClick.RemoveListener(CloseButtonClick)`；`CloseButtonClick()`(`:51-54`) = `this.Close()`。`.c:14-27` 与之一致（同一个 `+0x90 → +0x100` 的 UnityEvent 上 `RemoveListener`）。
- 🔴 **R-S #7 的证据有误**：它写「摘的回调就是开场补间的 `OnComplete`（设 interactable）」——**不成立**。`_OnEnable_b__11_0`(`:395-398`) = `canvasGroup.interactable = true`，那是 `DOFade(...).OnComplete(...)` 的回调，**OnDisable 从不摘它**；摘的是 `CloseButtonClick`。
- 判定：体 = 纯解绑 ⇒ 按 R-S 自己在 `商店:185` 用的口径应为**无等价物**；`Close()` 是「OnDisable 的触发时机」不是「体」。**建议**：`我们的落点` → 「（无等价物）纯解绑；关窗动作本身 = `GameWindow.Close():714`（那是触发时机）」，`差异` 订正 R-S 那句证据。

**14 · 商店:198 · `PurchasePremiumWindow.OnEnable()` · `PurchasePremiumWindow.cs:360 Open` · ✅ 真等价（机制差）**
- 原版 `il2csharp`（RVA `0x83D8F0`）/ `.c:20-40` = `closeButton` 加监听 + `canvasGroup.alpha=0` + `interactable=false` + `DOFade(1,0.3)` + `localScale=(0.8,0.8,0.8)` + `OnComplete(interactable=true)`。
- 我们 `Open(:360)` → `Build()` → `StartPop()`(`:933`) = 同一段淡入 + 0.8→1（常量 `0.3`/`0.8` 已注在文件头）。**可留 A**（R-S #8 同判）。

**15 · 商店:247 · `ShopItemContainer.OnDisable()` · `OfferContainer.cs:1257 SetSlotActive` · ❌ 空桩（应 A→D，R-S #11）**
- 原版 `il2csharp ShopItemContainer.cs` = **`// RVA: 0x4B33B0 Slot: 14`** + `protected virtual void OnDisable() { }` —— `0x4B33B0` = **全局空方法桩 `ret 0`**（本文件现读亲验）。`ShopItemContainer` 是 `abstract`，具体子类 `CatalogItemContainer` **不 override**。
- 改账：见 R-S §三 #3（状态 A→D；落点 → 「（无等价物）原版体是空桩」；`差异` 写明「旧账把本仓自己的格子显隐当成了这个方法的复刻」）。

**16 · 商店:248 · `ShopItemContainer.OnEnable()` · `SetSlotActive` · ❌ 空桩（应 A→D，R-S #12）**
- 同上（`RVA 0x4B33B0 Slot: 15`，体 `{ }`）。改账同 R-S §三 #3。

**17 · 商店:301 · `ShopTab.OnOpen()` · `ShopTabPage.cs:238 Setup` · ✅ 真等价**
- 原版 `il2csharp ShopTab.cs`（RVA `0x799590 Slot: 8`）= `sub_1804b33b0(this,0)`（基类调用被折成空桩 `0x4B33B0`）+ `this.RefreshOffers()`。
- 我们 `Setup(:238)` = 清根 → 页头 → `BuildGrid` 重拉报价。同职责。**可留 A**。

**18 · 收藏:2 · `AlternateArtCardCollectionTab.Awake()` · `CollectionWindow.cs:2324 BuildStylesPage` · ✅ 真等价（机制差）**
- 原版 `il2csharp AlternateArtCardCollectionTab.cs:26-35`（RVA `0x6CC6B0 Slot: 4`）= 基类 Awake（折成 `0x4B33B0`）+ `leftStyleButton.m_OnClick.AddListener(_Awake_b__7_0)` + `rightStyleButton.m_OnClick.AddListener(_Awake_b__7_1)`。
- 我们 `BuildStylesPage(:2324)` 里 `:2332 BuildStyleArrow(…, () => ChangeStyle(-1))` / `:2333 (…, () => ChangeStyle(+1))`（注释已写明「按位置接线：左颗=上一个」）⇒ 建页 + 绑左右箭头。**可留 A**，落点建议收到 `:2332/:2333`。

**19 · 收藏:175 · `CollectionScreen.OnDisable()` · `MainMenuRuntime.cs:1030 RepaintNavHighlight` · ❌ 错配（R-S #30）**
- 原版 `il2csharp CollectionScreen.cs`（RVA `0x6DB660`）**全篇一句**：`closeButton.m_OnClick.RemoveListener(BackButtonClick)`（本文件现读亲验）。
- 我们 `RepaintNavHighlight(:1030)` = 刷主菜单左竖导航高亮 —— 那是 `CollectionScreen.Open()` 里 `NavigationPanelController.SetToggle(Collection)` 的等价物（对到了另一个原版方法）。
- 改账：见 R-S §三 #8（落点 → `Shell/CollectionWindow.cs:3948` 关窗钮绑定处；`差异` 写明旧落点点错方法）。

**20 · 收藏:176 · `CollectionScreen.OnEnable()` · `RepaintNavHighlight` · ❌ 应指绑定点（R-S #31）**
- 原版 `il2csharp`（RVA `0x6DB700`）= `closeButton.m_OnClick.AddListener(BackButtonClick)`。改账同 R-S §三 #8。

**21 · 收藏:340 · `WhereToGetCardsSection.Awake()` · `WhereToGetCardsPopup.cs:288 Build` · ✅ 真等价（应指绑定点）**
- 原版 `il2csharp WhereToGetCardsSection.cs:22-27`（RVA `0x6CB400`）= `toggle.onValueChanged.AddListener(SetVisibility)`。
- 我们 `Build(:288)` 建每个扇区时 `:544 MenuDraw.Hit(toggleNode, "Hit", …, () => ToggleSection(v))`，`ToggleSection(:597)` 的 doc 写着「= 原版 `WhereToGetCardsSection.SetVisibility`」。⇒ **真等价**，但落点应收到 **`:544`（绑定点）/ `:597`（SetVisibility）**，别停在 `:288 Build`。

**22 · 收藏:344 · `WhereToGetCardsSection.Reset()` · `WhereToGetCardsPopup.cs:281 Show` · ✅ 真等价（机制差）**
- 原版 `il2csharp`（RVA `0x6CB9D0`）= 遍历 `currentCards` 逐个 `Destroy(gameObject)` → 清 list（`_version++` / `_size=0` / `Array.Clear`）→ 复位滚动/尺寸。
- 我们 `Show(:281)` = `_ctx = FillDef(ctx); Build();`，`Build` 头 = `ClearChildren` + 清全部缓存引用 ⇒ 旧卡全销毁、按新 ctx 重建。**可留 A**（R-S #33 同判）。

### 二·B　B/C 档生命周期（20 条真生命周期 + 1 条误中）

> 这一档**不需要改账**（8 条「无等价物」的落点列已经写成「无对应物/空跳」，没有「异名但等价」）。这里只把 20 条的档位钉死，供以后复查。

**商店（3 条）**
- **商店:12 · `BaseOfferPopup.OnDisable` · 无对应物 · 无等价物** — 原版 `.c:13-27`：`priceDisplayButton==null ⇒ return`（**无 LogError**）否则 `RemoveListener(ProcessPurchase)`。我们按钮走 `MenuDraw.Hit` 闭包、随节点销毁 ⇒ **无订阅可摘**。*（B 正确）*
- **商店:13 · `BaseOfferPopup.OnEnable` · `Shell/BaseOfferPopup.cs:1011 / :1197` · 指到绑定点** — 原版 `.c:15-36`：null ⇒ `LogError("Offer popup missing priceDisplayButton")` + return；否则 `AddListener(ProcessPurchase)`。我们 `:1011` = 价签 `MenuDraw.Hit → ProcessPurchase` + 同一条缺按钮 LogError、`:1197` = `PriceDisplayButtonPresent` 读口。⇒ 事件两半（绑定 + 缺件出声）都落地。*（B 正确；这是 `商店:12/:13` 这一对里唯一该「指绑定点」的那半）*
- **商店:95 · `GachaCratePanel.Awake` · `GachaEventWindow.cs:1352 BuildCrateVideo / :1372 OnCrateVideoStarted` · 真等价（机制差）** — 原版 `il2csharp GachaCratePanel.cs:31-42`（RVA `0x684090`）：`videoPlayer.enabled=false` + `videoBackground.SetActive(false)` + `started += _Awake_b__9_0` + `targetCamera = Camera.main` + `openCrateButton.OnClick.AddListener(OpenCrateButtonClick)`（五句）。我们五句都在；**B 的真实原因 = `Resources/Art/videos/gacha_crate_opening` 那个 clip 没导入**（源 `bundle_menus_assets_all/VideoClip/Gacha Crate Opening.mp4` 5,141,522 B）⇒ `clip==null ⇒ started` 不触发。*（B 正确，卡资源）*

**日常/奖励（9 条）**
- **日常:53 · `ChallengeManager.Start` · `ChallengeManager.cs:103` · 无等价物（有替代物）** — 原版 `il2csharp :26-35`（RVA `0x7E5ED0`）**全篇 = 往 `ChatGlobalManager` 单例的 `RemovePracticeMatchCallback(+0xa0)` / `AcceptedPracticeMatchCallback(+0xa8)` 各赋一个委托**。本机无那台单例（PlayFab 私聊通道）。我们 `Start()` 做的是「红点 provider 登记 + `ChallengeEngine.Attach`」并**出声说明挂不上**，替代物 = `ChallengeData.PendingPracticeMatches` 本地队列。⇒ 不是同一生命周期行为。建议 `差异` 把那句「（等价）」改成 **「无等价物 + 替代物」**（现在还不算错，只是口径偏软）。*（B 正确）*
- **日常:94 · `DailyRewardPopup.Start` · `DailyRewardPopup.cs:1012 WireUp` · 指到绑定点** — 原版 `il2csharp`（RVA `0x6DD160`）= 4 处 AddListener：`closeButton` · `armySelector.OnArmySelected += SetArmy` · `rewardsTrack.OnCollect += _Start_b__6_0` · `timerText.OnRunOutOfTime += +0x1B0`。我们四跳 = `:1012 WireUp ③` + `BuildTimer ④（ToFocus）` + `BuildSideBar ①` + `BuildArmyCell ②`。⇒ 指到绑定点；B 因「同一 `文件:行` 覆盖多方法 ⇒ 方案 §4.1 只能 B」。*（B 正确）*
- **日常:160 · `EventBadgeController.Awake` · `EventBadgeController.cs:39/:47/:66` · 真等价（机制差）** — 原版 `il2csharp :14-…`（RVA `0x5AB4A0 Slot: 4`）= 基类 Awake + 遍历 `LiveOpsManager.GetAllEvents(false)` 过滤后逐个建 badge。我们 `:39 Awake → :47 EnsureBadges → :66 BuildBadge`。**B 的原因**：prefab 缺席（三重否定已做）+ 数据侧恒 0。*（B 正确）*
- **日常:226 · `ExpansionPassEventTab.Reset` · `ExpansionPassEventWindow.cs:2753 / :2841` · 真等价（机制差）** — 原版 `il2csharp :116-125`（RVA `0x6BDBC0`）= `LogWarning` + `Unload(event,1)` + `getCardsButton.m_OnClick.RemoveAllListeners()`（三句）。我们 `ReleasePlan` 三句逐句投影（`_getCardsClickBound` 承载摘监听）。**B 因无 LiveOps 装载层（`Unload` 空跳）**。*（B 正确）*
- **日常:229 · `ExpansionPassMissionsTab.OnClose` · `ExpansionPassEventWindow.cs:2878` · 无等价物** — 原版 `il2csharp :168`（RVA `0x6BDE20`）= 基类 `OnClose`（`0x4B33B0` 空桩）+ `ReleaseEventAssets(事件)`。释放远端资产，本仓无装载层 ⇒ 无对象可释放。*（B 正确，落点已写成「投影 / 远端本地不加载」）*
- **日常:232 · `ExpansionPassMissionsTab.Reset` · `:2881` · 无等价物** — 原版 `il2csharp :174`（RVA `0x6BE4B0`）**全篇一句**：`ReleaseEventAssets(事件)`。同上。*（B 正确）*
- **日常:612 · `RaidEventTab.Reset` · `RaidEventWindow.cs:1527 / RaidEvent.cs:1111` · 无等价物** — 原版 `il2csharp :98`（RVA `0x6C1F10`）= `LogWarning` + `Unload(event,1)` +（`headerController!=null` ? `Unload(event,5)` : **抛空引用** `FUN_1803f47a0`）。我们只出声 + 投影两态，执行侧无 `LiveOpsAssetUtility.Unload`。*（B 正确；⚠️ `B94_B4` 已把「header==null 跳过」订正成「抛 NRE」）*
- **日常:617 · `RaidHeaderController.OnDestroy` · `RaidEventWindow.cs:1592 / RaidEvent.cs:1021` · 无等价物** — 原版 `il2csharp :140`（RVA `0x6C23B0`）**全篇一句**：`Unload(event,5)`。同上。*（B 正确）*
- **日常:621 · `RaidOrArmyReleaseMenuContainer.OnEnable` · `RaidEvent.cs:1158 / :1161` · 真等价（机制差）** — 原版 `il2csharp :141`（RVA `0x80F380`）**全篇一句**：`this.RefreshProgressBar();`。我们 `RaidOrArmyReleaseMenuContainerLogic` + `RefreshesOnEnable()` 已投影。**B 因宿主 `LiveopMenuContainer` 那一半在 `MainMenuRuntime.cs:3993`（未接线）+ 本地 0 份在期事件**。*（B 正确）*
- **日常:709 · `SinglePlayerOnlyEnergyEventWindow.OnDestroy` · `EnergySinglePlayerOnlyEventWindow.cs:1396` · 无等价物** — 原版 `il2csharp :146`（RVA `0x690070`）= `ReleaseEventAssets(+0x70)`。无装载层。*（B 正确）*
- **日常:732 · `SurvivalEventWindowEnemyMarker.Awake` · `SurvivalEventWindowEnemyMarker.cs:66` · 真等价（逐句）** — 原版 `il2csharp :14`（RVA `0x6C8360`）= `HiddenEnemyVisual.SetActive(true); RevealedEnemyVisual.SetActive(false);` 两句；我们 `:66-70` **逐句同形**。**B 因视觉件本地 0 命中 + 整条生存事件窗线没建（无调用方）**。*（B 正确）*
- **日常:774 · `TwoSidesEvent_OnGoingTab.OnOpen` · `ParentEventWindow.cs:1191 BuildOnGoingPage` · 真等价（建树半）** — 原版 `il2csharp :25-…`（RVA `0x692630 Slot: 8`）= 基类 + `header.Initialize` + `timerDisplay.InitializeWitLocKey` + 两队伍 `teamDrawers[i].Initialize` + 比分求和 + 奖励面板 + 回退钮。我们 `BuildOnGoingPage(:1191)` 建树半逐件；**数据半（队伍名/队长/比分/里程碑）= 远端 `TwoSidesEventData`**。*（B 正确）*

**未分类 / 泛型（2 条）**
- **未分类:178 · `ConfigProvider.OnDestroy` · `ConfigProvider.cs:156` · 真等价（逐句）** — 原版 `out_b94/ConfigProvider_object_.OnDestroy__16826e0.c` 现读：`if (config(+0x20) != 0) { <cache>.Clear(2); }`。我们 `:156-159` = `if (config != null) ConfigCache.Clear(typeof(T));`。*（B 正确；`B94_Y6` 已把旧判 D 推翻）*
- **泛型:578 · `ConfigProvider<T>.OnDestroy` · 同一落点 `ConfigProvider.cs:156` · 真等价（逐句）** — 同上（泛型产物 `ConfigProvider<object>__OnDestroy` 体一致）。*（B 正确）*

**社交 / 轮抽（3 条）**
- **社交:15 · `AllianceInvitationEntry.OnDestroy` · 无对应物 · 无等价物** — 原版 `il2csharp :267`（RVA `0x7C5620`）= `joinButton.m_OnClick.RemoveAllListeners()` + `rejectButton.m_OnClick.RemoveAllListeners()`（纯解绑）。本仓按钮闭包、随节点销毁。*（B 正确；`B92_V1`/`B94-R-L` 已把落点改成「无对应物·零可观察」）*
- **轮抽:114 · `DraftModeMenuPayStateTimed.OnOpen` · `DraftModeWindow.cs:4281 BuildTimedFamily` · 真等价（机制差，gated）** — 原版 `il2csharp :12-…`（RVA `0x82CF20 Slot: 8`）= 校验 `TimedDraftData` + `armySlider.SetProgress/SetArmies` + `allianceScorePanel.Initialize` + `eventImageSelector.Initialize(drawData.EventAssets)`。我们 `BuildTimedFamily` 三件已建（`:4311 BuildTimedArmyBar = SetProgress + SetArmies`），**B 因 `TimedDraftLocal.Enabled` 默认关 + 排行榜/Premium 价/矩形 🔶 本地模拟**。*（B 正确）*
- **轮抽:147 · `DraftModeWarlordSelectionStateTimed.OnOpen` · `DraftModeWindow.cs:4281` 的 `:4299-4305` · 真等价（机制差，gated）** — 原版 `il2csharp :17-…`（RVA `0x82FE90 Slot: 8`）= 标题 `GetLabel(Title)` + `GetRandomWarlords()` 按 `cardArmy` `GroupBy` + `UICardSelector.Initialize`。我们按 `specificArmies` 过滤 `_warlords` 再 `RebuildWarlordCards`；**标题幅面未接**。*（B 正确）*

**误中（1 条，非生命周期）**
- **日常:55 · `ChallengeManager.TryChallengeMatchStart(IPlayEvent, bool)`** — 名字里含 `Start(`，被正则抓到；**不是 Unity 生命周期**（原版 251 行 = 组 `RoomOptions` + `MatchMakerManager.FindMatch` = 服务器匹配 P3）。本文件不判它。

---

## §三 「异名但等价」这句话还剩多少处（核心产出）

### 三·1 计数（现读 2026-10-29）

```
全账命中 = 327 处   （A 326 · D 1）
  卡组编辑.tsv 142 · 收藏.tsv 107 · 商店.tsv 78
其中落在生命周期方法（OnEnable/OnDisable/Awake/OnDestroy/OnOpen/OnClose/Start/Reset）上 = 22 处
其余 305 处 = 数据/装配/查询方法（Initialize 18 · FilterItem 7 · FillToggleList 7 · Refresh 6 · GetCollection 5 · SetCell 3 · AddCard 2…）
```

- **22 处生命周期 = 本批判死的对象**（详见 §二·A）。
- **305 处非生命周期**：抽样与结构都表明**多数成立**（`AddCard→TryAdd` / `AddDefensiveCard→TryAdd` / `CardCopiesInDeck` 这类是真正的「同名异形」等价；R-S 50 条抽样真等价率 **76–88%**）。⇒ **这句话本身不是毒药，是「用在生命周期/解绑/释放方法上时不可信」**。

### 三·2 逐条判断（22 条生命周期，按档）

| # | 账行 | 原版类.方法 | 现落点 | 真值档 | 依据（本批核） |
|---:|---|---|---|---|---|
| 1 | 卡组编辑:40 | CollectionDeck.Awake | BuildDeckCell | ✅真等价（该层不建） | `deckDifficulty.SetActive(false)` ↔ showDifficulty 恒 false |
| 2 | 卡组编辑:43 | CollectionDeck.OnDestroy | BuildDeckCell | ❌无等价物 | `ReleaseGameModeIcon`（Addressables）无对应物 |
| 3 | 卡组编辑:150 | DeckInfoControls.OnDisable | ApplyControlStates | ❌错配（那是 Initialize 的等价物） | 体=纯解绑；绑定点 :1571/:1675 |
| 4 | 卡组编辑:213 | DeckSelectionTabController.Awake | Create | ❌错配（Create 只是工厂） | 五件事在 Build:470/:596/:763/Shown:432 |
| 5 | 卡组编辑:219 | DeckSelectionTabController.Start | Open | ✅真等价 | `ShowPrebuiltDecks(true)` ↔ `OwnDecks=false` |
| 6 | 卡组编辑:265 | ImportDeckPopup.OnDisable | RefreshInputText | ❌错配（那是画字） | 体=纯解绑 |
| 7 | 卡组编辑:266 | ImportDeckPopup.OnEnable | RefreshInputText | ❌应指绑定点 | Open:148 + :258/:282/:167（缺 interactable 闸） |
| 8 | 商店:124 | GeneralOfferPopupDrawerBase.DoReset | Build | ✅真等价（机制差） | 子类体=清理抽屉；Build 全重建；⚠️出处应指子类 |
| 9 | 商店:185 | PuchasePremiumArmyContainer.OnDisable | ApplyArmyInfo | ❌无等价物（R-S #3） | 体=退订+释放底图 |
| 10 | 商店:186 | PuchasePremiumArmyContainer.OnEnable | ApplyArmyInfo | ❌应指绑定点（R-S #4） | `toggle.onValueChanged.AddListener` ↔ :762/:767 |
| 11 | 商店:189 | PuchasePremiumArmyInfo.OnDisable | ApplyArmyInfo | ❌无等价物（R-S #5） | 体=纯解绑 → 绑定处 :834 |
| 12 | 商店:190 | PuchasePremiumArmyInfo.OnEnable | ApplyArmyInfo | ❌应指绑定点（R-S #6） | `AddListener` ↔ :834 |
| 13 | 商店:197 | PurchasePremiumWindow.OnDisable | GameWindow.Close | ⚠️边界（R-S 判真等价，体=纯解绑） | `RemoveListener(CloseButtonClick)`；R-S 证据错 |
| 14 | 商店:198 | PurchasePremiumWindow.OnEnable | PurchasePremiumWindow.Open | ✅真等价（机制差） | 加监听+淡入 ↔ Open→StartPop |
| 15 | 商店:247 | ShopItemContainer.OnDisable | SetSlotActive | ❌空桩（应 D，R-S #11） | RVA 0x4B33B0 `{ }` |
| 16 | 商店:248 | ShopItemContainer.OnEnable | SetSlotActive | ❌空桩（应 D，R-S #12） | RVA 0x4B33B0 `{ }` |
| 17 | 商店:301 | ShopTab.OnOpen | Setup | ✅真等价 | 基类空桩 + RefreshOffers ↔ Setup 重拉 |
| 18 | 收藏:2 | AlternateArtCardCollectionTab.Awake | BuildStylesPage | ✅真等价 | 左右箭头 AddListener ↔ :2332/:2333 |
| 19 | 收藏:175 | CollectionScreen.OnDisable | RepaintNavHighlight | ❌错配（R-S #30） | 体=摘关窗钮监听；那是 Open() 的另一句 |
| 20 | 收藏:176 | CollectionScreen.OnEnable | RepaintNavHighlight | ❌应指绑定点（R-S #31） | `AddListener(BackButtonClick)` ↔ :3948 |
| 21 | 收藏:340 | WhereToGetCardsSection.Awake | Build | ✅真等价（应收到 :544/:597） | `toggle.onValueChanged += SetVisibility` |
| 22 | 收藏:344 | WhereToGetCardsSection.Reset | Show | ✅真等价（机制差） | 销毁全卡+清表 ↔ Show→Build 重建 |

**统计**：✅真等价 **8** · 指到绑定点 **5** · 无等价物 **7** · 空桩 **2** · ⚠️边界 **1** ⇒ **14/22 该改（64%）**。

### 三·3 唯一的 D 行（`商店:130`）
- `InAppOfferContainer.OnInitialize(IShopOffer)` · 状态 **D** · 备注仍带旧句「2026-10-25 G 复核：**异名但等价**」+ 后接「2026-10-28 B93_E7 现核：A→D（真钱 IAP 容器…）」。
- 判定：**该句已被后面的 A→D 覆盖，不构成误导**，但建议把「异名但等价」那半句删掉（一行两种说法 = 铁律 6 说的「两份说法打架比没有更糟」）。

---

## §四 判不了的（差什么）

| # | 账行 | 差什么 |
|---:|---|---|
| 1 | 商店:186（PuchasePremiumArmyContainer.OnEnable） | 原版挂的 lambda `DAT_1842b7e28` **没有导出产物** ⇒ 只知道「挂了 `Action<bool>`」，**判不出它具体做什么**。本文件只能判「落点 `ApplyArmyInfo` 不是它」，改账指向绑定处 `:762/:767`（那里是谓词 + 派发，职责相同）。 |
| 2 | 轮抽:114 / 轮抽:147 | 三个输入（`LeaderboardManager.GetCachedLeaderboard` 的阵营总分 / `TimedDraftData.specificArmies` / `Premium` 价）**都在服务端**；本地只有 `TimedDraftLocal` 的 🔶 模拟值 ⇒ 「行为等价」只能给到「结构等价（gated 默认关）」，**实测不了**。 |
| 3 | 日常:621（RaidOrArmyReleaseMenuContainer.OnEnable） | 宿主那一半（主菜单活动入口格）在 `Shell/MainMenuRuntime.cs:3993`，**不在本代理白名单**、也没接线 ⇒ 只能判「逻辑层等价、未接线」，**跑不到实况**。 |
| 4 | 商店:124（DoReset，抽象基类行） | 本行 `原版类` 是抽象基类、判据出处指向抽象声明（无体）。真体在 `GeneralOfferPopupDrawer` / `CarouselOfferPopupDrawer` 两个子类（体不同：一个 `SetActive(false)+Release`，一个 `Destroy+清 list`）。**该行到底代表哪一支**，账里没说 ⇒ 本文件按「会走 `GeneralOfferPopupDrawer`」判（调用点 `BaseOfferPopup.cs:91` 取的是 `GetComponent<GeneralOfferPopupDrawer>()`），**若其实要代表 Carousel 那一支需重判**。 |

---

## §五 给调度台

### 五·1 这类行为什么容易配错（病根，逐条带证）

1. **2026-10-25 那批复核是「按功能词配最近的一颗方法」**，不是按方法体配。证据：22 条生命周期里 6 条（`商店:185/186/189/190` · `收藏:175/176`）备注**一字不差**都是「落点 X() 就是本方法行为的唯一实现（收尾/开/关 = X）」，而 X 全是**内容刷新/显隐**方法，原版体却是 `Add/RemoveListener`。
2. **本仓根本没有「UnityEvent 订阅层」** —— 按钮全是 `MenuDraw.Hit` 一次性闭包，随 `Build()`/节点销毁一起没。**所以任何 `OnEnable/OnDisable = Add/RemoveListener` 的方法，在本仓永远不存在「方法级等价物」**；正确写法只有「（无等价物）」或「指到绑定点 `:行`」。
3. **本仓也没有 Addressables 装载层** —— 所有 `Release*`（`ReleaseGameModeIcon` / `ReleaseMediumBackGround` / `ReleaseEventAssets` / `LiveOpsAssetUtility.Unload`）**都没有对象可释放** ⇒ 同样是「无等价物」，不是「等价」。
4. **这句话自带结论**（「就是……唯一实现」），后来每次重跑（B93/B94）都只改别的列、**没回头复核这句话**，于是一路留到 A 档。
5. **这句话躺在 `备注` 列（第 11 列）—— 全仓没有任何工具读它**。`check_ledger_points.py` 现读 `备注`/`异名` 命中 **0** ⇒ 它**永远进不了闸**。

### 五·2 闸能不能查出来？——**不能（按设计）**

- `工具/check_ledger_points.py` 文件头现读第 5-7 行明写：**「⛔ 本闸只把「落点形态」摆出来，判据仍要人读 —— 它证明不了「这一行实现了那个原版方法」」**。
- 它能查的：`文件:行号` 在不在、越界、**A 行落点那行像不像声明**、B 行落点漂移/注释行（`WARN_BFAIL/BDRIFT/ONCOMMENT`，**只报不计失败、不进 `--fix`**）。
- ⇒ 对「落点行是**一个真实存在、名字也叫得上的方法**」这种情况（本次 22 条全部如此），闸**一定绿**。**这不是闸的 bug，是它的声明边界**；语义等价必须靠人/代理读两侧代码。
- **⚠️ 已知同类盲点**（CLAUDE.md §三 记过）：`check_ledger_points` **不评估落点括号里嵌的第二个 `文件:行`** ⇒ 若把「正确落点」写在括号里、主落点仍写错方法，闸也绿。

### 五·3 建议（给调度台，⛔ 本代理不改）

1. **优先改这 14 条**（顺序 = 收益/风险）：
   - **空桩 → D（2 条）**：`商店:247 / :248`（`RVA 0x4B33B0`，已亲验）。**这是唯一「原版本身没行为、我们却写成复刻了」的硬错**，最该先改。
   - **错配（5 条）**：`卡组编辑:150 / :213 / :265 / :266`、`收藏:175 / :176`、`商店:185/186/189/190`。
   - **无等价物（7 条）**：`卡组编辑:43`、`商店:197`、`收藏:175`，其余并入上条。
   - **边界（1 条）**：`商店:197`（同时订正 R-S #7 的证据错：摘的是 `CloseButtonClick`，不是 tween `OnComplete`）。
2. **落点要收细的 3 条（不改档、只改落点）**：`卡组编辑:40` → `MenuDraw.cs:3513`；`收藏:340` → `WhereToGetCardsPopup.cs:544/:597`；`收藏:2` → `CollectionWindow.cs:2332/:2333`；`商店:124` → 子类真体（`GeneralOfferPopupDrawer.cs:161`）。
3. **口径升级（不改内容）**：`日常:53` 的「等价物 = ChallengeData 本地队列」建议改写成 **「无等价物（本机无 ChatGlobalManager）+ 替代物 = …」**，与 `商店:185` 那批口径一致。
4. **防复发（三条机器判据，建议加进日后的 B/C 复核清单）**：
   - ⓐ **凡 `方法` 命中生命周期正则 + 备注含「异名但等价」⇒ 一律当「未核」重新读 `.c`/il2csharp**（本批 22 条里 14 条错，命中率 64%）。
   - ⓑ **凡原版体只有 `AddListener/RemoveListener/RemoveAllListeners` ⇒ 只能写「（无等价物）」或「指到绑定点 `文件:行`」，⛔ 不许写「等价」**。
   - ⓒ **凡原版体只有 `Release*/Unload` ⇒ 判「无等价物」**（本仓无 Addressables 装载层），并在 `差异` 写明「Resources 直取、无引用计数」。
   - ⓓ **凡 `RVA` 等于 `0x4B33B0`（空桩）或 `0x6FB110`（`return false` 桩）⇒ 直接判 D**，⛔ 不要给它配落点。
5. **这条经验值得写进交接**：`B94_R-S` 第 9 批 §五·1 的结论（「异名但等价」在生命周期上系统性不可信）**本文件在全表上复现并量化**：**22 条里 14 条该改（64%）**，而且 **R-S 只看到 8 条**（它那 50 条抽样的命中）；**剩下 5 条错配（全在 `卡组编辑.tsv`）是本文件新查出的**。⇒ 以后别只信抽样，**生命周期行要全表扫**。

---

*（本文件由只读代理 R-U 产出，未改任何源码 / 账 / 正本；所有 `文件:行号` 均为 2026-10-29 现读。）*
