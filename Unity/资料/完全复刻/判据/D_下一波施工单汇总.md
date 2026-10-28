# D 批（只读代理）四份施工单 · 调度台汇总（2026-10-28）

> 四个只读代理（D1/D2/D3/D4）均为 `Explore`（**无写权限**），产出由调度台整理落盘。
> ⚠️ **原始全文在各自的 handback 里，没有第二个副本** —— 本件是**可执行摘要**，动手前按这里给的键去账里读原行。
> ⚠️ **所有行号是 2026-10-28 现读快照**，改前用**符号名/节点名**二次定位（本仓多人并行，行号随时漂）。

---

## D1 · 高率线剩余 63 条（目标：把这 15 条线刷到 100%）

现读校数：**63 条**（B 37 · C 26）= 卡组编辑1 · 教程1 · 锻造3 · 战役4 · 轮抽4 · 社交6 · HUD8 · 特效11 · 菜单12 · 个人资料13。

**分档（D1 口径）**：**①核销 4** · **②可做 26** · **③阻塞 26** · **需裁定 7**。

### ①核销（零代码，只回账改 A）
| 键 | 依据 |
|---|---|
| 战役 721 `b__32_1` | `LiveOpsEventWindow.cs:1921` 阵营谓词**已**作第 4 实参传入 |
| 轮抽 58 `ToggleAllianceInfo` | `DraftModeWindow.cs:4178-4211` 三块实件 + `localScale=0.95581`；`DraftAssertD3.cs:1355` 断言已改 |
| 特效 507 `CardToMinionAnim` | 裁定 3 已定「原版无形态回退」，`.c:1-39` 现读确认只有 PlayInvokeAnimation |
| HUD 713 `GetUnitSizeRatio` | `BattleDriver.cs:22077` 接线**已在** |

### ②可做 26（判据齐、动作能一句话写清）
按文件：**BattleDriver 12**（458/546/364/378/537/403半/485半/524/788/187/194/208）· **RuleCore 4**（486/503/537半/194）· **DraftModeWindow+DraftEventBase 3**（114/147/6）· **LiveOpsEventWindow 2**（710/717）· **SocialData 3**（115/124/132）· **AllianceMemberTab/PanelWindow/VoiceLines 3**（100半/187/283）· **Shop 3**（507/714/131）· **MainMenuRuntime 1**（223）· **BoosterPackOpenWindow 1**（504）· **MenuSprites 等 0**。

**几条已被 D1 证明「账里理由过期」的（可直接做）**：
- 社交 100 `OnToggleEditClicked`：**`Shell/TextInputField.cs:78` 本仓已有可编辑输入框**（账里「没有输入框」过期）
- 社交 115/124/132：`SocialData.cs:68/91/130` 的静态表**就是缓存本体**，补 `GroupById`/`SetInvitations`/`BuildCurrentMember` 访问器即可（约 20 行）
- 战役 710 `Initialize(IPlayEvent)`：**`SaveKeys.cs:307/339` 的 `_ours_` 前缀免改键表**（账里「SaveKeys 不在白名单 ⇒ 跨进程会丢」不成立）⇒ 持久化 15 行可做
- 锻造 504：`Resources/Art/ui_menu/` **已导 5 张**包面图，源 16 张齐 ⇒ **只剩 11 张待导**
- 战役 717：🔴 **`allowEdit:false` 是误读** —— `DeckSelectionContext.cs:88` 第 3 实参是 `overrideDeckList=null`，**原版没这个参数** ⇒ 删该项；真残差是**没传 `currentDeck`**

### ③阻塞 26（等远端/等数据/判据空）
卡组194 · 社交187 · HUD306/709/755 · 特效195/198/521 · 菜单58/131/427/434/438/439/471/472/526 · 个人56/57/97/98/113/198/221/293/328

### 需裁定 7
轮抽6（造不造本地到期时钟）· HUD788（AI 落地视图要不要先退回手牌形态）· 特效524（非教程局建不建 overlay）· 菜单54/60（`IsNewCard` 恒 false 要不要改拥有数模型）· 个人77（无活动时隐不隐边框钮 —— **已裁：保留 Default 兜底**）· 个人332（`SetHighlightedRow` 数据源）

---

## D2 · 三笔跨文件清扫

### 活1 · 79 颗小屏字号 → **19 个站点 / 51 行**
| 文件 | 颗数 | 站点 |
|---|---|---|
| `Shell/RankedTab.cs` | 21 | `:445 :450 :554 :641 :644`（`RatingRow` 要加 `ssMaxPx` 形参，Top4=45 / AllFactions=110，⛔ 别写死一个值） |
| `Shell/AllianceMemberTab.cs` | 10 | `:1640 :1988` |
| `Shell/ProfileTab.cs` | 8 | `:722 :997 :1093 :1160 :928` |
| `Shell/LeaderboardWindow.cs` | 4 | `:758` |
| `Shell/RankedEventWindow.cs` | 3 | `:230` |
| `Shell/TrophyInfoPopup.cs` | 2 | `:383 :387` |
| `Shell/AllianceEventScorePanel.cs` · `EnergySinglePlayerOnlyEventWindow.cs` · `SkirmishEventWindow.cs` | 各1 | `:154` · `:796` · `:677` |
| **严格 no-op（⛔ 不加）** | 2 | `MainMenuRuntime.BuildGlobalRating` |
| **无对应物（⛔ 别硬造控件）** | 26 | 11 颗 `RankedDivisionInfo` 服务器评分 · 2 颗 `Highest Faction Rating` 空容器 · 6 颗 Draft Timer（明说不建） · 4 颗 Leaderboard Timer（明说不建） · 1 颗宿主弹窗未建 · 2 颗 RankedTab 子件未建 |

### 活2 · ColorTint 294 颗 → 🔴 **不是「站点没接」，是「没有可接的口」**
本仓 `WindowButton` **没有禁用 RGB 乘数机制**（`:876` 只有 `ColorTintGreyOnDisable` 闸，`:946 RefreshGray` 只换 Greyscale 材质）⇒
**先在 `WindowButton.cs` 建一格 `DisabledTint`（默认 1f）+ `RefreshGray` 里乘一次**，再派站点；否则写手只能各写一份乘色（违反「两处写同一条规则」）。
- 294 = Button 270 + Toggle 24；RGB **276 颗 0.7843137**、**18 颗 0.8019608**；`m_DisabledColor.a` **全 1.0**
- 18 颗 0.802 的规律：**基本全是「Tab 切换钮」或「整颗 army item 根钮」**
- 已核到行的前 8 个文件约 **132 颗**可先派（`MissionsTab` 48 · `SettingsWindow` 10 · `ProfileTab/RankedTab/AvatarTab` 9 · `AllianceMemberTab` 6 · `DailyRewardPopup` 11 · `CollectionWindow` 5 · `OfferContainer` ~40 · `LeaderboardRow` 3）

### 活3 · 下拉展开面四点真机验法（宿主 A 改盟设置 / 宿主 B 建盟页，双份坐标已在 D2 报告里）
几何：列表 `x1 = field.x1` · 宽 `field.宽 − 5` · 顶 `field.cy + 22` · 行高 **40.8707** · 可见 `min(选项数,14)` · 条目宽 `list.宽 − 17`。

---

## D3 · 战斗逻辑驱动 275 条（B233 · C42）

**档位**：**②可做 84 · ③阻塞（等证据/口径）140 · 需裁定 48 · 反对 3**。
🔴 **结论：主战场剩余量的大头是「证据/口径」，不是「写代码」—— 真写手活只有 84 条。**

### 块表（一次可派 8–9 个互不相交写手）
| 块 | 文件 | 条数 | 并行 | 建议波次 |
|---|---|---|---|---|
| A | `RuleCore.cs` | 72 | ❌ Chain R 第1 | 1 |
| B | `EffectResolver.cs` | 41 | ❌ Chain R 第2 | 2 |
| C | `ActionPump.cs`+`ActionQueue.cs` | 8 | ❌ Chain R 第3 | 3 |
| D | `CardDef*.cs` | 15 | ❌ Chain R 第4 | 3 |
| E | `BattleDriver.cs` | 27 | ✅（独占，内分 2 遍） | 1 |
| F | `PlayerState.cs` | 16 | ✅ | 1 |
| G1 | `UnitState.cs` | 10 | ✅ | 1 |
| G2 | `BoardSlots/ArenaSlots/BoardLayout` | 10 | ✅ | 1 |
| H | `BattleContext.cs` | 5 | ✅ | 1 |
| I | 事件/计数小件 | 17 | ✅（与 A 同波只许追加类尾） | 1 |
| J | 教程/战役/AI | 7 | ✅ | 2 |
| K | 表现小件 | 16 | ✅（⛔ 不含 BattleDriver） | 1 |
| L | 无落点/判 D/核销 | 25 | ✅ 判据组 | 2 |
| M | 工具/数据线 | 6 | ✅ | 2 |

### 🔴 ③-3 那 46 条最值钱
不是缺口，是「**没人读完**」—— 一个查判据代理一轮可把 20–30 条转 ②/核销。

### D3 的 10 条「账判错」（可直接回账）
L644（`RuleCore.cs:2262` 已发 ToHands）· L645（`B89-E26` 已追加 attackType 实参）· L1280（账自述判词过期）· L1103/L1144（账自述已修）· L1228（实现已在，缺的是 `Badges.For` 消费）· L387（池化=性能差，可判等价）· L799（账自述等价）· L1367（槽位早就在 `PlayerDataKey.VictoryCounters`）· L74（有可达卡，不是「不复刻」类 C）

### 6 条需调度台出的口径（一出一批核销 ~30 条）
共享代码段 / 方法级落点 / 中间态 / 存储形态 / 数据模型 / 原版瑕疵照抄。

---

## D4 · 断言缺口 55 条（P0 27）

按宿主：`Editor/BattleScene.cs` 5 · `Editor/MainMenuScene.cs` **21** · `Editor/DeckScene.cs` 4 · `Editor/DraftAssertD3.cs` 3 · `Editor/CollectionScene.cs` 3 · `Editor/RewardsScene.cs` 2 · `Editor/ShellScene.cs` 1 · `RuleEngine/Editor` 10 · 工具/真机 6。

**D4 纠正的 4 件事实**（⛔ 别按旧说法派）：
1. `DeckScene.cs` **调不到** `DeckRuntime` 的 private `StartRowDrag`/`DragAutoScroll` ⇒ 两个薄口必须加在**生产类 `DeckRuntime` 类尾**
2. `EnchantmentTemplateCheck` §H **已经接进** `_run_8_checks.sh`（只欠执行）
3. N4 报告里 `CheckTrue(RestoreDeck(doomed))` 在坏路径下**是错的** —— 该路径 `Save()` 仍失败 ⇒ 返回 **false**；要断的是「内存退回了」
4. `PinnedPlayMd5` **已经是 `be75a11d…`** —— 重钉已完成，剩下只是「连跑两次同值」的验证

**要新增的测试薄口**（生产类）：`DeckRuntime.UiDragDeckRowHold` / `DragAutoScrollForTest` · `BattleDriver.Get2DPosOfCardForTest` / `TooltipUpOffsetForTest` / `TraitPulseCountForTest` / `PlayTraitPulseForTest` · `Label.SmallScreenMaxPxForTest` / `SmallScreenMinPxForTest`。
⚠️ **明确不加**：`AllianceDropdownBox` 的任何 `*ForTest`（它有节点句柄，够了）。

**D4 的 12 条「不要做」**（重要）：不加 `*ForTest` 到 Dropdown · 不写 `if(x!=null)Check` · 不把坏路径的 `RestoreDeck` 断成 true · 不伪造小屏已应用 · 不在 Unity 里断 ColorTint · 不拿生产函数当期望 · 不断 `TimeEndsIn` vs `EndsIn`（官方表逐字相同）· 不给休眠 internal 闸加反射 · 不为绿放宽生产闸 · 不在 `ChatPanel.cs` 加成员（12 条 A 行按行号钉着）· 不用 `rfind('    }\n}')` 塞方法 · 不按旧行号插入。
