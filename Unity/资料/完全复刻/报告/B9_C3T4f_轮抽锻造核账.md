# B9_C3T4f · 轮抽 / 锻造 线 —— B 行核账

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 范围：`轮抽.tsv`（15 B）+ `锻造.tsv`（34 B）= **49 条**（现读：轮抽 167 行 A93/B15/C6/D53；锻造 118 行 A61/B34/C2/D21）。
> ⚠️ **本轮只做核账**（判 A/B/落点失效/判据缺），**不动 `我们的落点` 列**（落点重钉由 `R2T3B4` 负责，两边靠方法名区分）。
> 产出：`判据/B行逐条核_轮抽锻造.tsv`（8 列 × 49）· `账/_parts/B9_C3T4f.tsv`（11 列 × 49，只改 `差异`/`备注`/`轮次`）· 本报告。

## 一、结论分布

| 建议等级 | 条数 | 说明 |
|---|---|---|
| **A（等价）** | **7** | `差异` 明写「等价/一致」且落点能指到真实方法（已逐个 grep 验过方法存在） |
| **B（真偏离 / 类级命中）** | **15** | 落点是方法但过宽（类级命中）或有实打实缺口 |
| **落点失效** | **27** | 落点无 file:line / 描述性 / 属性 / `—`，非方法级 ⇒ 待 R2T3B4 重钉 |
| 判据缺 | 0 | 49/49 原版体都在 `decomp_full`/ILSpy |

## 二、7 条 A（已验方法存在）

| # | 域 | 原版方法 | 落点（现读） |
|---|---|---|---|
| 18 | 锻造 | `BoosterPackOpenWindow.AnimationEndEvent()` | `BoosterPackOpenWindow.cs:900 Advance`（`public void Advance(float dt)`） |
| 19 | 锻造 | `BoosterPackOpenWindow.CardOpened(CardInBoosterPack)` | `:860 FinishFlip`（`void FinishFlip(int i)`） |
| 20 | 锻造 | `BoosterPackOpenWindow.CloseAnimaFinishEvent()` | `:425 CloseFromTap`（`public void CloseFromTap()`） |
| 21 | 锻造 | `BoosterPackOpenWindow.Initialize(List<RawCardScript>, BoosterPackVisualItem)` | `:384 Create` / `:433 Build`（均真方法） |
| 22 | 锻造 | `BoosterPackOpenWindow.ToggleCardOpening(bool)` | `:756 OnSlotClick`（`public void OnSlotClick(int i)`） |
| 43 | 锻造 | `ForgeHandler.SetupEvents(List<Forge>)` | `ForgeData.cs:100 static ForgeData()`（静态构造建表，等价） |
| 48 | 锻造 | `UpgradeForgeLevel.OnReset()` | `ForgeTab.cs Refresh()`（真方法在 `:801`） |

## 三、15 条 B

**轮抽（8 条，全是「类级命中」或数据缺口）**：#4 `DraftModeMenuDemo.Awake`、#5 `DraftModeMenuDemo.OnDestroy`、#7 `DraftModeMenuPayStateDemo.OnClose`、#8 `…OnDisable`、#9 `DraftModeSelectPacksStateDemo.OnDestroy`、#10 `DraftModeWarlordSelectionStateDemo.OnDestroy`、#12 `DraftRunDisplay.OnDestroy` —— 落点都指 `DraftModeWindow.Close:1024` / `Build:1121`（**同一个方法覆盖多个原版方法**，类级命中，待拆方法级）；#13 `DraftRunDisplay.PerformBadgeAnimations` —— 代码逐句对齐，但 **`Draft Mode Loss Jingle` 不在 `animfx_sounds.json`（461 条）**、`import_original_sfx.py` 的 `EXTRA_CUES` 也没有 ⇒ 败场那格会弹、那声不响（收进去即可转 A）。

**锻造（7 条，真缺口）**：
- **#23 `ToggleTextHelper`**：落点 `:199 DiscoverText`/`:202 CloseText` 是属性（非方法），语义闸已判「落点无对应实现 ⇒ 退 B」。
- **#29 `CardDisplayWindowOnCloseWindow`**：`:769 OpenCardDetail` 现成，但**打开详情窗后不回灌状态**（翻牌流程不等它）。
- **#32 `GetContentByRarity`**：`:887 StartShakes` 现成，但**稀有度→内容表未逐档核**。
- **#40 `TryCraftingInternal`** 🔴：`:2380 TryCraft`/`:2479 DoCraft`/`:2431 PlayCraftVfx` 现成；**一处真差异** —— 原版 `DoVFX()` 在分支**之后无条件执行**（请求一发出就播），我们按 2026-10-24 裁定「失败不播」只在成功时播 ⇒ **需调度台裁定**是否改回原版时序。
- **#41 `CollectRewards`**：`ForgeData.cs:170 Claim(army,level)` 逐级领，**无「一次全领」入口**。
- **#46 `ToggleCollectReward`**：`BindBlink`/`_readyRoot` 现成，但**特效位置固定在轨道、不跟目标格**。
- **#49 `UpgradeForgeLevel.OnTrigger`**：`_blink.Tick` 是「可领持续脉冲」，不是原版「收到信号播一次」。

## 四、27 条落点失效（待 R2T3B4 重钉；本轮只记结论）

- **轮抽 7**：#1 `TryGetQuote`（落点 `_quoteEnd+QuoteTermForWins` 无 file:line）、#2 `Validate`（描述性）、#3 `DraftMenuContainer.OnDestroy`（`—`）、#6 `GetFreeButtonInteractable`（描述性；**数据不可及**：活动时间窗在服务端，我们恒可点）、#11 `ChooseQuote`（描述性；按五键反推档位、非原版归一算法）、#14 `DraftWins.OnInitialize`（`—`）、#15 `DraftWins.OnReset`（`—`）。
- **锻造 20**：#16 `SetRefreshCallback`、#17 `BoosterPackBackground.Initialize`、#24 `WaitAnimationForClose`、#25 `BoosterPackVisualItem.Initialize`、#26 `StartCardAnimationEvent`、#27 `BoosterShopTab.OnOpen`、#28 `TryOpenContainerArmy`、#30 `EnableCardBackAnimEvent`、#31 `FinishedOpeningAnimEvent`、#33 `RelaseCardback`、#34 `ToggleInteractions`、#35 `TryGetContentByRarity`、#36 `TryHighlightCardback`、#37 `UiColliderOnEnter`、#38 `UiColliderOnExit`、#39 `WaitForSpinAnimationEnd`、#42 `GetMaxLevelForge`（实现是 `ForgeData.cs:34 public const int MaxLevel = 50` **常量字段**，机检不认）、#44 `ForgeRewardSelector.Initialize`（`:764` 是语句，类级命中）、#45 `RewardCollected`（同）、#47 `ForgeWindowTab.OnClose`（`—`）。
- 原因多为：落点停在**文件级/描述性/时间线/卡背表**，没指到具体方法声明。

## 五、顺手发现（账数据问题）

- **#14 `DraftWins.OnInitialize` / #15 `DraftWins.OnReset` 的 `差异` 列内容是孤字「我」**、**#47 `ForgeWindowTab.OnClose` 的 `差异` 是「原」** —— 明显是**被截断/写坏**的账数据（正文本应是「原版订阅/注销信号…」之类）。建议修主表（主表只有调度台写）。

## 六、验证与残余（如实）

- 7 条 A 的落点方法**已 grep 验存在**（`Advance/FinishFlip/CloseFromTap/Create/Build/OnSlotClick/static ForgeData/Refresh`）。
- 判 A/B 主要依据行内 `差异`（历轮代理逐句读过原版体写下的「等价/一致/差」）+ 本轮方法存在性抽验；**未对 49 条全部重读原版全文**（体量）。
- 补丁 `状态` 全 B 不动、`轮次`=B9_C3T4f、`我们的落点` **0 改**（已机检确认）、11 列无坏行；`check_patch_conflicts.py` → 无跨补丁重复键（6 份补丁）。
- 生成器脚本：`d:/4/Unity/_tmp_view/gen_c3t4f.py`。
