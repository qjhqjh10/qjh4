# RS · 外壳族 8 条现核

> 只读现核 · 2026-10-18 · 工程根 `d:/4/Unity/MyGame/Assets/CardPresentation`
> 核的对象 = `d:/4/项目任务.md` §三第 29 条 §29·b「有编号的 27 条」里挂在**菜单/外壳族**的 8 条（`:529-538`）。
> ⛔ 本件**没跑 Unity、没动 git、没改任何 `.cs`/正本**；所有结论 = 静态实读（行号按**当时**的工作区，别人可能在写 ⇒ 锚点请按**节点名/语句**认）。

---

## 1. 一句话结论

| 判 | 条数 | 编号 |
|---|---|---|
| **仍成立（真要动手）** | **5** | `A833` · `A834` · `A840` · `A851` · `A866` |
| **成立且【前提已被推翻的问题，这轮查清了】—— 仍是真缺陷，要修** | **1** | `A867` |
| **假账（已做完 / 只差销账）** | **1** | `A852` |
| **已做完（只剩 Unity 腿）** | **1** | `A878` |

⇒ **8 条里 7 条要动手（其中 2 条只差销账/收尾）**；**没有一条是「整条都不成立」**。
⚠️ 另：`A866` 的**范围被低记了** —— 不是 2 份，是 **4 份**（见 §3.6）。

---

## 2. 逐条现核表

| 编号 | ①还成不成立 | ②判据（文件:行号） | ③要改哪些文件 | ④最小改法 | ⑤自检宿主 | 置信度 |
|---|---|---|---|---|---|---|
| `A833` | ✅ **成立**（但 LeaderboardRow 的 **`Hit` 那一条已存在** ⇒ 缺的是**视觉 mesh/文字** + **MatchLogRow 全部**） | `Shell/LeaderboardRow.cs:207-238`（`Nine`/`Text` 吃节点框）· `Shell/MatchLogRow.cs`（同形）· 现存的只有 `Editor/MainMenuScene.cs:5629-5674`（**Hit** 被截）· 缺的那族对照 = `Editor/MainMenuScene.cs:6471-6537`（`SocialPage.Text` 的 A822 断言） | `Editor/MainMenuScene.cs`（最小）或 `Editor/ShellScene.cs` | 照 A822 那一节（`TmpSpanPx` + `MenuDraw.Hit` 两条腿）给「压在视口边上的那一行」补 2~4 条：**行底 `Nine` 的渲染矩形**、**`Ranking`/`Name`/`Points` 的 TMP 顶点**被夹到视口沿；`MatchLogRow` 那一扇照抄一份 | `MainMenuScene.Run`（夹具已在 `:5629`/`:8804`）；若落 `ShellScene` ⇒ `ShellScene.Run` | 0.85 |
| `A834` | ✅ **成立**（`MenuDraw.Local` 仍读父件**当下**位置；断言里那条**析取**还留着） | `Shell/MenuDraw.cs:30-31`（`Local` = `RectCenter − PosInDesignSpace(parent)`）· `Editor/CollectionScene.cs:5764-5770`（病灶现读）· `Editor/CollectionScene.cs:5826-5832`（**析取**：明写「−217.095 = A834 彻底修法落地后那一支」）· `Shell/CollectionWindow.cs:533+` `ApplyDrawerSlide` 尾 ④ | `Shell/MenuDraw.cs`（+ 需要一个「父件设计矩形」的载体） | 让 `MenuDraw.Local` 的**减数**取「父件被写进去的那个设计矩形」而不是实时 `PosInDesignSpace(parent)`。⚠️ **判据不足**：`ViewportClip.BaseRect` **只覆盖视口节点**，而本病的父件（筛选列面板 `Card Filters`）**不是视口节点** ⇒ 还差一条裁定「非视口节点的设计矩形记在谁身上」。**别只改一半**（尺寸那一项照旧 | `CollectionScene.Run`（断言宿主）＋ 因 `MenuDraw` 是全壳共用件 ⇒ 按铁律 12「共用件先 grep 谁在用」跑 **全套 12 条** | 0.9（成立）／0.5（改法形状） |
| `A840` | ✅ **成立**（6 处生产站点**都没记 `BaseRect`**；⚠️ 实为 **7 处**） | `Shell/ViewportClip.cs:69-73`（文件头自己列出那 6 处）· `Shell/ViewportClip.cs:251`（`_hasBaseRect` 那一支）vs `:277`（`LiveDerivations++` 旧路）· 6 站点：`Shell/ShopWindow.cs:419` · `Shell/AvatarTab.cs:158` · `Shell/TitleTab.cs:131` · `Shell/LiveOpsEventWindow.cs:593` · `Shell/PracticeModePopup.cs:863`/`:1026`；**第 7 处（文件头漏列）** = `Shell/TutorialModePopup.cs:486` | `Shell/{ShopWindow,AvatarTab,TitleTab,LiveOpsEventWindow,PracticeModePopup,TutorialModePopup}.cs`（+ 视情况 `Shell/ViewportClip.cs` 的文件头注释） | 每处在 `AddComponent<ViewportClip>()` **之后**补一句 `vc.CaptureNow();`（**不能**改在 `OnEnable` 里自动抓 —— 文件头 `:72-73` 明令） | `ShopScene.Run`（ShopWindow）· `ShellScene.Run`+`MainMenuScene.Run`（AvatarTab/TitleTab/PracticeMode/Tutorial）· `RewardsScene.Run`（LiveOpsEventWindow） | 0.92 |
| `A851` | ✅ **成立**（3 处 `meshInfo[0]` 一个没改；⚠️「那几条断言用的标签是不是单槽」**这轮查到答案**） | 三处硬编码：`Editor/RewardsScene.cs:254` / `:291` / `:329`；自陈在 `:241-248`（「要改得另开一笔、并且必须先跑一次」）。调用点 8 个：`:4422/:4431/:4439/:4456`（纯拉丁 `"Warpforge Offline Rulebook"`）· `:6259`（`QuantityLabelOf` = **数量数字**）· `:6369/:6381`（同上）· `:6761`（`TmpEdgePx`，streak 那几颗**英文**字） | `Editor/RewardsScene.cs`（3 个助手） | 三处都改成按 `chr[i].materialReferenceIndex` 取槽（照 `Editor/ShellScene.cs:250` / `Editor/CollectionScene.cs:6085` 那份现成写法）；⚠️ 这是**改量法** ⇒ 改完必须真跑一次 `RewardsScene.Run`（A490 口径） | `RewardsScene.Run` | 0.9（成立）／0.75（「今天单槽」= **静态推断**，未跑 Unity） |
| `A852` | ❌ **假账 —— 6/6 早已全标** | 现读 6 处 A844 注：`Editor/RewardsScene.cs:241`/`:280`/`:322` · `Editor/MainMenuScene.cs:10696`（`CountSoftFadedTextVerts`，**不是**账上写的 10033 —— 行号已漂）· `Editor/IconSizeProbe.cs:137` · `Editor/Round1015Probe.cs:44` | 无（**只销账**） | 从 §29·b 表里删掉这一行；⛔ 不必再动 `.cs` | ——（纯文档 ⇒ 按铁律 12 一条自检都不用跑） | 0.95 |
| `A866` | ✅ **成立 —— 而且范围是 4 份不是 2 份** | 4 个 `BuildHeader`：`Shell/TutorialModePopup.cs:365`（`"Header With Back Button"`，无模式图标）· `Shell/LiveOpsEventWindow.cs:752`（`"Game Mode Header With Back Button"`，有图标）· `Shell/DailyStreakPopup.cs:503` · `Shell/EnergySinglePlayerOnlyEventWindow.cs:485`。四份建的是**同一个原版共用件**（同 sprite `WF_Campaign_Info_Background` 740×167 · 同九宫 `(335,0,395,0)` · 同 `Window Title` 67.55 + `m_characterSpacing=5`）；**常量也各抄一份**：`Shell/DailyStreakPopup.cs:116-117` 与 `Shell/LiveOpsEventWindow.cs:196-197` 逐值相同 | 新建共用件（如 `Shell/MenuWindowBase.cs` 里加静态 builder，或新开 `Shell/WindowHeader.cs`）+ 上述 4 个文件 | 抽一个 `MenuWindowBase.HeaderWithBackButton(root, hdrRect, bool withGameModeIcon, Action onBack)`（矩形/九宫/字距/自适应/对齐次序/返回钮 SpriteSwap 全收进去），4 处转调；**增量式**（一次收一份、跑一次） | 覆盖 4 扇窗的宿主：`MainMenuScene.Run`（Tutorial/Energy）· `RewardsScene.Run`（DailyStreak）· `ShellScene.Run`/`CollectionScene.Run`（LiveOps/Practice 族） | 0.9（成立）／0.85（4 份这份实读） |
| `A867` | ✅ **成立 —— 真缺陷（「前提被推翻」那半已被本轮的诊断答掉）** | **答：`Open()` 会销毁重建 Root 子节点** —— `Shell/PracticeModePopup.cs:734-745`（`Open()` → `Build()` → `for (i = childCount-1…) DestroySafe`）· `Shell/LiveOpsEventWindow.cs:342/365-369`（同形）。两处都 `RegisterScroll` 而 **全文件零 `UnregisterOwnedBy`**（`:875`/`:1040` · `:610`）。**为什么漏不掉**：`MenuScroll` 是**普通 C# 类**（`Shell/MenuScroll.cs:79`）⇒ `PruneScrolls` 的 `s == null` 永远假；`Owner = gameObject`（`:873`/`:1038`/`:606`）指的是**活下来的窗根** ⇒ 也判不出死（`Shell/PointerLayer.cs:229-236`） | `Shell/LiveOpsEventWindow.cs` · `Shell/PracticeModePopup.cs`（先；再扫同族） | 两处 `Build()` 的**第一句**补 `PointerLayer.UnregisterOwnedBy(gameObject);`（同 `Shell/InboxWindow.cs:405` / `Shell/FriendsTab.cs:241` 的现成形状） | `ShellScene.Run`+`MainMenuScene.Run`+`CollectionScene.Run`（Practice 族）· `RewardsScene.Run`（LiveOps） | 0.85 |
| `A878` | ⚠️ **已做完（代码侧)** —— 只剩「重跑一次字体资产」这条 **Unity 腿** | 语料已并：`Core/CardText.cs:642`（`foreach (var v in Loc.AllChinese()) yield return v;`）+ `Core/Loc.cs:999`；消费侧 `Editor/TmpSetup.cs:508`；**断言已在** `Editor/BattleScene.cs:8632-8638`（3 条，含「删掉那句 foreach ⇒ 当场红」的改坏法） | 无（**只有 `TmpSetup.BuildCjkFontAsset` 这一趟**） | 跑 `-executeMethod TmpSetup.BuildCjkFontAsset`（+ 那条覆盖自检），然后把这条从表里销掉 | ⛔ **不属于 12 条自检任何一条**（是 `TmpSetup` 自己的 Unity 腿） | 0.9 |

---

## 3. 逐条展开

### 3.1 `A833` —— 零断言（部分）

- **做的是**：`Shell/LeaderboardRow.Build` 里 4 颗字（`Ranking`/`Name`/`Guild Name`/`Points`）与行底 `Nine`，`clip` 恒传 `null`（`Shell/LeaderboardRow.cs:207-238` 的 `c.Clip`）⇒ 由父链上那颗 `ViewportClip` 接管（A781 起**首次上裁**，判据 = `资料/普查产出_1015/R2_A799全量表.md` §一① #4/#5/#6/#7）。`MatchLogRow` 同族（`Shell/MatchLogRow.cs`，`.Text`/`.TextSide` 各一处）。
- **现存的唯一一条**：`Editor/MainMenuScene.cs:5665-5674` 断的是**命中区** `border/Hit` 的 quad 被截短（`ey2 ≤ VpBot+0.5` 且「真被截短过」）—— **那不是**「视觉 mesh / 文字被夹到视口沿」。
- **对照物（缺的那一族长什么样）**：`Editor/MainMenuScene.cs:6471-6537` 的 A822 一节 —— 用 `ShellScene.TmpSpanPx` 量 TMP 顶点、断「**部分越界 ⇒ 建、而且 mesh 被截到框沿**」+「**只截越界那一侧**」，两态成对（不是自证）。
- **`MatchLogRow` 侧全空**：`Editor/MainMenuScene.cs:3950-4014`（档案窗那一页）与 `:8804-8911`（弹窗）只断几何/对齐/滚动，**一条裁切断言都没有**。
- ⇒ **成立**。判据齐（有现成量法与现成夹具），可派。

### 3.2 `A834` —— 过渡偏位

- **病灶现读**（`Editor/CollectionScene.cs:5764-5770`）：`MenuDraw.Local(parent, r) = RectCenter(r) − PosInDesignSpace(parent)`，减的是**父件当下**位置 ⇒ 面板停在收起位（左移 385px）时建的那一版，局部坐标里多算 385px ⇒ 面板滑回原位后整列偏 +385px。
- **今天靠 ④ 兜**：`Shell/CollectionWindow.cs` 的 `ApplyDrawerSlide` 尾「④」在滑到展开位那一拍补一次 `RebuildFilterRows`（判据 = `Editor/CollectionScene.cs:5768`）。
- **断言里那条析取还在**：`Editor/CollectionScene.cs:5826-5832` 明写两档都允许，且点名「**−217.095** = `MenuDraw.Local` 改成取记录矩形 —— **A834 那条彻底修法落地后就是这一支**」⇒ 账没销。
- **改法形状存疑（如实记）**：`A811 根治` 给的是 `ViewportClip.BaseRect`（`Shell/ViewportClip.cs:198-218`），可它**只挂在视口节点上**；本病的父件是筛选列面板 `Card Filters`（不是视口节点，`Shell/CollectionWindow.cs` 的 `_fltScroll.Owner = panel.gameObject`） ⇒ 「父件的记录矩形记在谁身上」这条**判据不足**，要先裁。A表自己写「**要做，单开一趟**」—— 与现读一致。

### 3.3 `A840` —— 6 处（实为 7 处）没框记录

- **判据就在文件头**：`Shell/ViewportClip.cs:69-73` 逐一点名 `Shell/{ShopWindow,AvatarTab,TitleTab,LiveOpsEventWindow,PracticeModePopup×2}.cs` = **6 处**，并写明「**逐位回落到旧写法**（实时反推 + `LiveDerivations` 计数）——那是**还没接上的一批**」。
- **机制实读**：`MenuDraw.Node` → `ApplyPxRect` 那一句（`Shell/MenuDraw.cs:184-189`）才写 `SetBaseRect`，而 `AddComponent<ViewportClip>()` 都在它**之后** ⇒ 那一下查表时组件还不存在、什么都没记（`Shell/ViewportClip.cs:311-317` 的 `Hang` 特意补一句也正是为这个）。生产侧 `ApplyPxRect` 只有 `MenuDraw.Node`/`MenuWindowBase.Node` 两处调用（`Shell/MenuDraw.cs:123` · `Shell/MenuWindowBase.cs:225`）⇒ 这 6 处**之后没人补记**。
- **顺手核出第 7 处**：`Shell/TutorialModePopup.cs:486` 也是「`Node` 建 + `AddComponent`」，**不在文件头那份清单里**（时间上比那份 doc 晚）。
- ⚠️ **顺带订正一条半真**：账上写「`LiveDerivations` 计数看得到」—— 该计数**全仓没有任何读者**（只在 `Shell/ViewportClip.cs:277` `++`、`:436` 声明），**没有一条断言盯着它**；「看得到」只是「字段在那儿」。

### 3.4 `A851` —— 3 处 `meshInfo[0]`

- **实读**：`Editor/RewardsScene.cs` 的 `TmpVertPx:254` · `TmpVertsAndAlpha:291` · `TmpGlyphUvW:329` 三处都 `var mi = tmp.textInfo.meshInfo[0];`，而 `chr[i].vertexIndex` 是**按材质槽**编的 ⇒ 一行里混拉丁+汉字（两个槽）时会**串到别的字的顶点上、静默**（doc 自己写在 `:241-248`，并给了对照：`Editor/ShellScene.cs` 那份同名 `TmpVertsAndAlpha` **当年故意没抄**，就是躲这个坑）。
- **账上那句「还没查那几条断言今天用的标签是不是单槽」—— 这轮查了（静态）**：8 个调用点里
  · `:4422/:4431/:4439/:4456` 量的都是 `"Warpforge Offline Rulebook"`（**纯拉丁**）；
  · `:6259/:6369/:6381` 量的都是 `ItemDrawer` 的**数量**标签（`Shell/ItemDrawer.cs:277` `NodeQuantity = "Quantity"`，内容是**数字**）；
  · `:6761` 的 `TmpEdgePx` 用在 streak 那几颗**英文**字上。
  ⇒ **今天都落在单槽**（所以未爆），但这是**静态推断、没跑 Unity**；且**缺陷是潜伏的**（哪天量一颗中英混排的字就静默读错）。
- ⇒ **成立**（要改，且改完必须真跑一次 —— A490 口径：改读数就是改量法）。

### 3.5 `A852` —— 假账（6/6 已标）

- 逐处实读那 6 条 A844 注**全在**：`Editor/RewardsScene.cs:241` / `:280` / `:322`（三处「与 `ShellScene.TmpSpanPx` 是【两条口径】、有意不收」+ 各自理由）· `Editor/MainMenuScene.cs:10696`（`CountSoftFadedTextVerts`）· `Editor/IconSizeProbe.cs:137`（`InkHeight`）· `Editor/Round1015Probe.cs:44`（`InkBoxY`）。
- 与 `资料/历史/A表已收口_1017.md:103` 的记载一致（「**6/6 全标了** …… ⇒ **只是没销账**」）。**唯一漂移**：那份把一个站点记成 `MainMenuScene.cs:10033`，现读是 `:10696`。
- ⇒ **不从 .cs 侧动手，只销账。**

### 3.6 `A866` —— 不是两份，是四份

- 四份都建的是**同一个原版共用件** `WindowHeaderWithBackButton`：根名 `Header With Back Button`（`TutorialModePopup:365` / `DailyStreakPopup:503` / `EnergySingle:485`）或 `Game Mode Header With Back Button`（`LiveOpsEventWindow:754`），结构一律是 `Header Background`（`WF_Campaign_Info_Background` 740×167、九宫 `(335,0,395,0)`、`Sliced`）→ `Window Title`（67.55 / `m_characterSpacing = 5` / auto `[18,67.55]` / base 36）→ `Header Background (1)` → `Header Back Button`（+ SpriteSwap 悬停）。
- **常量也各抄一份**：`Shell/DailyStreakPopup.cs:116-117` 与 `Shell/LiveOpsEventWindow.cs:196-197` 逐值相同（`HeaderBorder (335,0,395,0)` / `740f,167f`）。
- 账上只记了 `TutorialModePopup` + `LiveOpsEventWindow.BuildHeader` 两份 ⇒ **范围低记**（`DailyStreakPopup` / `EnergySinglePlayerOnlyEventWindow` 那两份**各有独立注释在解释「为什么与那一颗不同」** ⇒ 是四份并存，不是两句注释）。⇒ **成立，且要做的是 4 份收口。**

### 3.7 `A867` —— 前提被推翻之后，**这轮把「还缺的那一步诊断」答了**

- **前提那半**（早已推翻）：`UnregisterOwnedBy` **不是 0 个调用者** —— 生产/夹具共 11 处（`Shell/{BattleLogPopup:167, ChatPanel:564, CollectionWindow:647, FriendsTab:241, InboxWindow:405, PlayerProfileWindow:688, RewardWindow:1098, SettingsWindow:1578}.cs` + 编辑器夹具 3 处）。
- **本轮新查（原账点名要的那一步）**：「该窗 `Open()` 是否销毁重建 Root 子节点」= **会**：
  · `Shell/PracticeModePopup.cs:734-745` —— `Open()` → `Build()` → `for (int i = root.childCount-1; i>=0; i--) RewardsWindow.DestroySafe(...)`，随后 `:875` / `:1040` 各 `PointerLayer.RegisterScroll(...)`（`ArmyScroll` / `DeckScroll`，`Owner = gameObject`）；
  · `Shell/LiveOpsEventWindow.cs:342-369` 同形，`:610` `RegisterScroll(_armyScroll)`（`Owner = gameObject`，`:606`）；
  · **两处全文件 `UnregisterOwnedBy` 零命中**。
- **为什么 `PruneScrolls` 兜不住（这是「真缺陷」而不是「理论隐患」的关键）**：`MenuScroll` 是**普通 C# 类**（`Shell/MenuScroll.cs:79`）⇒ 被销毁的节点不会让它变 null ⇒ `PruneScrolls` 的 `s == null` 恒假；而 `Owner` 指的是**活下来的窗根**（`root = transform` 只是把**子件**删了）⇒ `s.Owner == null` 也恒假（`Shell/PointerLayer.cs:218-236`）。⇒ **每重开一次窗，登记表净涨 1~2 条，且旧条目还能被滚轮命中（`OnChanged` 指向已销毁的节点）。**
- **生产上确实会重开同一实例**：`Editor/MainMenuScene.cs:2927` 的注释白纸黑字——「`PracticeModePopup.Open()` = `Build()` **重建**（不依赖 `Data`）⇒ 重开安全」，`:2931/:2935/:2946` 就是连开三次的现场；`Editor/MainMenuScene.cs:4910` 对 `LiveOpsEventWindow` 一句同源。
- ⇒ **成立，且是真缺陷**（不是「判不出」）。修法 = 两处 `Build()` 首句补 `PointerLayer.UnregisterOwnedBy(gameObject);`。

### 3.8 `A878` —— 代码侧已做完，只剩 Unity 腿

- **已完成**：`Core/CardText.cs:642` 内转调 `Loc.AllChinese()`（`Core/Loc.cs:999`，遍历 `Table` 的中文列）⇒ 语料已含 `Loc` 全表；`Editor/TmpSetup.cs:508` 那句 `foreach` 就是消费点；**断言已在** `Editor/BattleScene.cs:8628-8638`（3 条：`手牌剩余` / `拖到目标上再松手` / `可用 {0}` 必须在语料里，并写了「删掉那句 foreach ⇒ 当场红」）。
- **没跑的那一半**（代码里自己也记着）：`Editor/BattleScene.cs:8627`「📌 **改完要重跑一次 `TmpSetup.BuildCjkFontAsset`**（那是 Unity 腿，本笔没跑）」；`Editor/TmpSetup.cs:498` 同句。
- ⛔ **这一条不属于任何一条自检宿主**（`TmpSetup` 是独立入口），所以「跑 12 条」发现不了它 —— 要**显式**跑 `-executeMethod TmpSetup.BuildCjkFontAsset`。

---

## 4. 顺手发现（**都没改**）

1. **`A866` 的范围低记**：同族 `BuildHeader` 是 **4 份**不是 2 份，且共用常量被抄成两份（`Shell/DailyStreakPopup.cs:116-117` vs `Shell/LiveOpsEventWindow.cs:196-197`）。收口时别只收账上那两处。
2. **`A840` 的清单少一处**：`Shell/TutorialModePopup.cs:486` 是第 **7** 个「`AddComponent<ViewportClip>` 且没记 `BaseRect`」的站点，`Shell/ViewportClip.cs:69-73` 那份文件头清单没列它 ⇒ 收口时按 `grep -rn "AddComponent<ViewportClip>"` **现扫**，别照抄那份清单。
3. **`LiveDerivations` 计数没有读者**：全仓除 `Shell/ViewportClip.cs` 自己外零命中（`:277` `++` / `:436` 声明）。账上「计数看得到」应改成「**计数在、但没有任何断言盯着**」。
4. **`A867` 的同族候选（⚠️ 未逐窗核实，只报不动）**：`Shell/DailyStreakPopup.cs`（`:253` 根子件全清 + `:373` `_trackScroll.Owner = gameObject` + **零 `UnregisterOwnedBy`**）与 `Shell/DeckSelectionPopup.cs`（`:345` 根清 + `:433` 同形 + 零 unregister）**形状与病灶一致**；**是否真漏**取决于「生产路径会不会重开同一个实例」—— 这一条我**没查透**，别当成已确认的缺陷。`Shell/CampaignRewardWindow.cs:330` / `Shell/CardDetailPopup.cs:355` / `Shell/DailyRewardPopup.cs:218` / `Shell/ImportDeckPopup.cs:142` / `Shell/DeckInfoPopup.cs:731` 也走「根清 + 重建」，但它们**不在 `RegisterScroll` 名单里** ⇒ 不在这条账上。
5. **文档行号漂移（只报）**：`资料/历史/A表已收口_1017.md:103` 把 A852 的一个站点记成 `Editor/MainMenuScene.cs:10033`，现读 = `:10696`。

---

## 5. 没查清的部分（⛔ 不拿猜测填空）

1. **`A834` 的「最小改法」形状** —— 判据不足。还差一条裁定：**非视口节点的设计矩形记在谁身上**（`ViewportClip.BaseRect` 只覆盖视口节点，而本病的父件是筛选列面板）。没有这条，「让 `MenuDraw.Local` 取记录矩形」无从落笔。
2. **`A851`「今天那 8 个调用点的标签都单槽」是静态推断** —— 没能真跑（本件不许跑 Unity），也没法从数据层证明「数字/英文串一定只占 0 号槽」。⇒ 置信度 0.75，落地时**先跑一次 `RewardsScene.Run`**再改。
3. **`A867` 的同族两扇（`DailyStreakPopup` / `DeckSelectionPopup`）** —— 只核到「形状一致 + 零 unregister」，**没核**生产路径是否重开同一实例 ⇒ 不能断言是缺陷（见 §4·4）。
4. **`A840` 那 6+1 处的节点矩形会不会在运行时被改** —— 只核了「建完没人调 `ApplyPxRect`」（生产侧 `ApplyPxRect` 只有两处调用点）；**没核**有没有别处直接改这些节点的 `localPosition`/`sizeDelta`（那种情况下 `CaptureNow` 也会记错帧，同文件头 `:72-73` 那条告警）。⇒ 落地前对这一句现扫一遍更稳。
5. **`A878` 的字体资产到底重跑没重跑** —— 我只看得到文件 mtime（`Assets/CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset`）与 git log 的日期**互相打架**，判不出「语料改完之后烘过一次」 ⇒ 按「没跑」记（宁可多留一条）。
