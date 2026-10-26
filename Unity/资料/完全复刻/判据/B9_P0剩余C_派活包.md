# B9 · P0 四域剩余 C 63 条 · 派活包（按我们的目标文件切片）

> 判据代理 `K1P0`（第八十四会话「完全复刻」）· 2026-10-26 · **只读核**（未改 `.cs` / 未改主账表 / 未改资产 / 未跑 Unity / 未动 git）
> 配套：逐条施工单 = `判据/B9_P0剩余C_施工单.tsv`（12 列 × 63 行）· 结论与证据 = `报告/B9_K1P0_P0剩余C施工单.md`
> 覆盖域：`账/战斗界面_HUD.tsv` 25 · `账/卡组编辑.tsv` 25 · `账/收藏.tsv` 9 · `账/卡牌面_渲染.tsv` 4（`状态 == C`，现读）

---

## §0 用法（四句话）

1. 🔴 **`账行号` 带域名前缀**（`HUD:` / `卡组:` / `收藏:` / `卡面:`）—— 四个域之间有重号（例：`HUD:148` 是 `ShowAvailableTargets`，`卡组:148` 是 `WarlordInfoButtonClick`），**别只按数字认**。
2. **一个包 = 一组文件互不相交的落点 = 一个写手**；包内按「先 P0 / 再 P1 / 最后 P2」做，做完一条在施工单里勾掉。
3. 🔴 **`Battle/BattleDriver.cs` 全项目只有 K-01 一个写手**：其它包凡需要动它（调用点 / 接线 / 开局装配），一律在报告里写成「改动请求」交给 K-01，⛔ 不要自己改。
4. 每包做完先跑 `bash 工具/typecheck.sh`（0/0），再按「本包验收口」跑 `ruleprobe` / 对应宿主；⛔ 写手不跑全量 Unity 自检（调度台在同步点统一跑）。

---

## §1 包总表

| 包名 | 独占文件 | 条数 | 独占? | 前置依赖 | 包内条目（账行号） | 本包验收口（摘要） |
|---|---|---|---|---|---|---|
| **K-01** | `CardPresentation/Battle/BattleDriver.cs` | **20** | ✅ **独占（第一瓶颈）** | 逐条不同（见 §2·K-01） | HUD:20·92·93·364·378·537·548·702·720·744 · 卡组:48·49·50·51·52·54·194 · 卡面:21·27·120 | `ruleprobe play check` 指纹逐条解释；真机三条（回放选牌 2s · 结算 2.0s 自动离房 · 抬牌只那张 tip） |
| **K-02** | `CardPresentation/Core/CardArt.cs` | **2** | ✅ 独占 | HUD:101 的调用点在 K-01 | HUD:101 · 收藏:203 | 牌堆 SDF = 卡背派生；销毁格后 `ReleasedCount` 增量 = 格数且图仍可显示 |
| **K-03** | `CardPresentation/Core/CardView.cs` | **5** | ✅ 独占 | 卡组:148 的调用点在 K-07 | HUD:108·148·153·189 · 卡组:148 | 传说溶解材质名；抬牌描述尾部 `desc+<i>(名字)</i>`；残骸 body3D 三条件；切语言卡面字变 |
| **K-04** | `Core/CardHighlight.cs` + `Hand/CardInteraction.cs` | **5** | ✅ 独占 | 三个调用点在 K-01（出手位 = BoardCardTouchDown / BoardPress） | HUD:682·683·684·686·687 | 按住 0.4s 弹卡面 / 0.2s 松手走轻点；轻点我方开技能面板；PressedBtn 0.4s 熄灭 |
| **K-05** | `CardPresentation/Core/CostCurveDrawer.cs` | **2** | ✅ 独占 | 无 | 卡组:71·131 | 费用曲线逐行 0.2s 长值 / 0.4s 渐变；instant 档一帧到位 |
| **K-06** | `Deck/DeckEditorState.cs` + `Editor/DeckScene.cs` | **1** | ✅ 独占 | 错误码枚举在 RuleEngine（跨包协调） | 卡组:8 | 满编 + 同名同时越限 ⇒ 错误码 = `MaxCopiesReached`；`DeckScene.Run` 绿 |
| **K-07** | `CardPresentation/Shell/PracticeModePopup.cs` | **2** | ✅ 独占 | 沿用现成 3 颗 HitOn | 卡组:134·138 | 点督军名开详情；卡背 5 连轮播 + Rewind |
| **K-08** | `Shell/ItemDrawer.cs` + `工具/import_original_sfx.py` | **2** | ✅ 独占 | 无 | 卡组:75 · 收藏:273 | 抽屉点击一声（`UI Click 1`）；造 PrebuiltDeck 夹具点开详情（**挂接已取证 = C；当前 build 0 条发放物 ⇒ 做完今天看不见效果，时间限定**） |
| **K-09** | `Shell/PrebuiltDecks.cs` + `工具/gen_prebuilt*.py` | **4** | ✅ 独占（BattleDriver 接线交 K-01） | 卡组:284 是 285/286 的前置 | 卡组:284·285·286·290 | bot 池条数 = 原版 `Type==Bot`；对手牌表 = 某副预组；`IsValid` 负例 |
| **K-10** | `Shell/DraftModeWindow.cs` + `Shell/BoosterPackAnim.cs` | **6** | ✅ 独占 | 卡组:305 是 299/302/303 的前置；shiny 依赖 K-13 | 卡组:293·299·300·302·303·305 | 开包 0.175s 阶梯 + 选包钮禁用；选包飞右栏 + 淡出销毁；关包 0.4s 淡出 |
| **K-11** | `RuleEngine/Core/TargetCriteriaEval.cs` + `CardCriteria.cs` + `CountEval.cs` | **3** | ✅ 独占（引擎侧） | 无 | 收藏:188·189·201 | 每格一条正负夹具；`ruleprobe play all dump` 差异逐条解释；未知 ta = 0 |
| **K-12** | `Battle/ErrorMessageBanner.cs` + `Deck/DeckRuntime.cs` | **1** | ✅ 独占 | 无 | 卡组:325 | 加不合法卡 ⇒ 屏幕错误横幅（文案 = DeckError 词条） |
| **K-13** | `Shell/CollectionWindow.cs` + `CardPresentation/Shaders/` | **1** | ✅ 独占 | `dump_shader_blob.py "UIEffect"` 已通 | 收藏:348 | 野牌格扫光；参数逐项 = W64 表；贴图缺则出声并记「待 external 解析」 |
| **K-14** | `Shell/CampaignTab.cs` + `Shell/ForgeTab.cs` | **1** | ✅ 独占 | 两窗已有 `Badge Highlight` 常量可对齐 | 收藏:15 | 战役 / 锻造页 13 颗阵营格各有徽记、收藏页没有 |
| **K-15** | `Battle/CardDisplayWindow.cs` + `RuleEngine/Core/GameplayVariables.cs` | **4** | ✅ 独占 | **数值未读**：6 个展示偏移要先从解包实例取 | HUD:750·755·756·758 | 敌方 / 进攻展示位 = 手牌锚 + 偏移（两档）；加载态描述取 `-load` 词条 |
| **K-16** | `Core/CardView.cs`（**与 K-03 同文件 ⇒ 串行**）+ 新 SoftMask 插件目录 | **1** | ⚠️ **与 K-03 抢同一文件** | **必须等 K-03 收工** | 卡面:55 | 大卡态描述底板软遮罩出现 / 退出恢复 `material=null` |
| **K-17** | `Shell/Inventory.cs` + `Core/LoadableRef.cs`（新建） | **1**（另一条待裁） | ✅ 独占 | 无 | 收藏:247（C）· 收藏:244（**建议 D**，待裁） | 建 `LoadableRef<T>` + 一行转发；夹具三档（=amount / −1 / +1） |
| **K-18** | 教程线（无本批文件） | **1** | — | 等教程线的本地入参 + 父链 scale | HUD:306 | **不派**：离线 build 恒不可达 |

**合计**：63 条 = 16 个可派包（K-01…K-15 并行 · K-16 需串行在 K-03 之后）+ **K-17（现派 1 条：`收藏:247`；`收藏:244` 建议 D、待调度台裁）** + **K-18（挂起，不派）**。
> 🔴 **三源取证已补（2026-10-26 K1P0，见报告 §6）**：`收藏:244` 三源 + C# 调用点**全 0**（可判 D）· `收藏:247` **有 3 处 C# 活调用**（撤销原「建议 D」，保持 C）· `卡组:75` **挂接存在**（`ItemDrawerConfig.asset` 登记 + 两个 offer prefab 各挂 1 个 `DeckAndCardbackDrawer`）⇒ 按裁定「要做，P2」，时间限定 = 当前 build 0 条发放物。

---

## §2 各包明细

### K-01 `Battle/BattleDriver.cs` · 20 条 · ✅ 独占（第一瓶颈）
- **序**：① **P0 两条**：`卡组:48`（开局 CardTiers = `CardProgress.Level(id)−1`）→ `HUD:92/93`（战斗侧异画，判据与 deck 层同一件）；② **P1 四条**：`卡组:52/54`（开局玩家元数据 + 反序列化装配）· `卡组:194`（MoveToDeck 等返回时长 → SetupInDeck）· `HUD:744`（Codex/Oath 两枚角标）· `HUD:537/548`（savedMana 接线）；③ 其余 P2。
- **跳过**：`HUD:92` 本身无独立工作量（并入 93）；`HUD:378` 与 `HUD:364` 是同一处（一起改）。
- **改动请求入口**（别的包会往这里堆）：K-02 的牌堆 SDF 调用点 · K-04 的三处 board 按下/按住/松手调用点 · K-09 的 `ResolveDeck` 改 bot 预组 · K-15 的 offensive 展示入口 · K-03 的角标层写口（反向）。
- **验收口**：`bash 工具/ruleprobe.sh play check`（指纹变化逐条解释）；`play all dump` 只在该包相关局变化；真机三条：回放遇 `choose` 面板弹 + 2.0 s 自动确认 · 结算 2.0 s 自动离房 · 悬停手牌只有那张弹 tip。
- **风险**：🔴 `HUD:537/548` 动 `ManaCarry` 会改能量曲线（全池指纹）；`HUD:702` 动结算链会碰联机（必须再跑联机三件：`NetSelfTest` / `NetBattleTest` / `SettingsScene`）；`卡组:48` 与「卡牌面 #63 GetCardTier → A」**不是同一条**（见 §6）。

### K-02 `Core/CardArt.cs` · 2 条 · ✅ 独占
- **序**：① `收藏:203`（P0，最小：只加 `ReleaseModeIcon()` + 一个调用口）→ ② `HUD:101`（`DeckCardbackSdf`）。
- **要点**：`CardArt.Release` / `PathOf` / `Retain` **已经存在**（B8_BF 2026-10-26 落地）；`收藏:203` 缺的只是「模式图标那一族被 `IsPerItemArt` 挡掉」。⛔ **别把 `ui_menu/` 加进 `PerItemArtPrefix`** —— 那会连带放 HUD / 难度角标 / 阵营图标，原版只放默认模式图标。
- **验收口**：销毁一批卡组格 ⇒ `CardArt.ReleasedCount` 增量 = 格数；随后模式图标仍能显示；`PerItemArtPrefix` 仍不含 `ui_menu/`。

### K-03 `Core/CardView.cs` · 5 条 · ✅ 独占
- **序**：① `HUD:108`（传说溶解材质，一行判定）→ ② `HUD:153`（残骸原卡 3D 卡身，三条件）→ ③ `HUD:148`（描述写口，供手牌抬牌用）→ ④ `HUD:189`（`RefreshLocalizedText`）→ ⑤ `卡组:148` 的详情窗依赖在 K-07。
- **要点**：加层/改层前先 `grep` 谁会遍历 `_layers`（整卡着色会静默盖掉新层）。
- **行号漂移**：账记 `BeginDissolve :6961`、`SetRemnantBody :2496-2522/:2632`；本轮现读 `BeginDissolve :6986`、`SetRemnantBody :2537` ⇒ **按符号名认**。

### K-04 `Core/CardHighlight.cs` + `Hand/CardInteraction.cs` · 5 条 · ✅ 独占
- **序**：`HUD:682`（按下记时刻+置标志）→ `HUD:683`（离开清标志）→ `HUD:686`（按住 > 0.3 s 弹卡面）→ `HUD:684`（抬指开技能面板）→ `HUD:687`（PressedBtn 0.4 s）。
- 🔴 **最容易做错的一条**：我们手牌那 0.2 s「长按不开窗」是**另一个类**（原版 `CardScript.OnTouchUpAsButton.c:37`），与 `ActiveWarlordAbility.Update` 的 0.3 s「按住就弹」是两回事 ⇒ **⛔ 别改手牌那 0.2 s，也别合并常量**。
- 三个调用点在 `BattleDriver`（`BoardCardTouchDown` / `BoardPress`）⇒ 写成改动请求给 K-01。

### K-05 `Core/CostCurveDrawer.cs` · 2 条
- `卡组:71` + `卡组:131` 是**同一处**（`Build` 里逐行补间）；一起改一次。补间用现有时间泵，⛔ 不新开协程。
- 断言要同时读「终值」与「补间已挂」（批处理下补间不推进）。

### K-06 `Deck/DeckEditorState.cs` + `Editor/DeckScene.cs` · 1 条
- `卡组:8`：把「整副满」闸挪到最后（原版 `.c:166`）+ 槽内同卡错误码改 `MaxCopiesReached`；**同批改 `Editor/DeckScene.cs` 的夹具断言**（现读 :3711 附近写的就是原版 .c:166 口径）。
- ⚠️ 这是**唯一会碰 Unity 宿主文件**的包 ⇒ 调度台要安排「跑 `DeckScene.Run` 时没有别的批处理在跑」。

### K-07 `Shell/PracticeModePopup.cs` · 2 条
- `卡组:134`（第 4 颗命中区）+ `卡组:138`（卡背 5 连轮播）—— 两条同文件，一起做；`卡组:148` 的详情窗调用也落在这（但判据写口在 K-03）。
- 命中区一律走 `MenuDraw.PaddedRect`（原版 = 可射线件并集 + `m_RaycastPadding`）。

### K-08 `Shell/ItemDrawer.cs` + `工具/import_original_sfx.py` · 2 条
- `收藏:273`（把 `UI Click 1` 收进 `EXTRA_CUES` 重跑导入）→ `卡组:75`（PrebuiltDeck 抽屉点击接线）。
- **`卡组:75` 三源取证已补（2026-10-26）**：① prefab 的 `m_MethodName`/`m_OnClick`/`m_Delegates` 全 0（**结构上不需要** —— 点击走 `ItemDrawer<T> : IPointerClickHandler`），但 `General Basic Offer Container Variant Deck_cardback_avatar.prefab` 与同名 Popup prefab **各挂 1 个 `DeckAndCardbackDrawer`（DeckDrawer 子类）** ② 场景无引用 Deck Drawer prefab（唯一 PrebuiltDeck 场景引用 = `Intro.unity:7792 starterDeck`）③ SO：`ItemDrawerConfig.asset:201-214` 有 `PrebuiltDeck → drawerReference 7ea5632e…` ⇒ **挂接存在 = C**。
- **时间限定**：PrebuiltDeck 实例 225 个，只有 2 个被 Demo `DeckInfo` SO、1 个被 starterDeck 引用，**无任何商店/奖励 SO 引用** ⇒ 本地 0 条 `ObtainableItem` 形式的发放；原版这类发放走远端目录 ⇒ **做完今天看不见真实效果**（夹具必须自己造）。

### K-09 `Shell/PrebuiltDecks.cs` + 生成器 · 4 条
- **序**：`卡组:284`（botDecks 分表，前置）→ `卡组:285` / `卡组:286`（一条过滤链两个入口，共用一份实现）→ `卡组:290`（`IsValid`，独立）。
- 🔴 `285/286` 建议**升 P0**：玩家每局都看得见对手牌表；现凑 ≠ 预组。
- 🔴 原版 `GetDeckByDifficulty` 用**未过滤表长度**取随机下标再 `ElementAtOrDefault` —— **原版 bug，照抄不修**（注释写死）。
- `BattleDriver.ResolveDeck` 的接线写成改动请求给 K-01。

### K-10 `Shell/DraftModeWindow.cs` + `BoosterPackAnim.cs` · 6 条
- **序**：`卡组:305`（缓存 startingPos，前置）→ `卡组:299`（OpenAnim 五件）→ `卡组:302` + `303`（影子副本 + 飞行）→ `293`（关包淡出）→ `300`（预览音效）。
- 时长常量已读：`0.175` 阶梯 · `0.4` 缩放/淡出 · `0.3` 放大 · `0.2` 间隔。
- shiny 依赖 K-13 的 shader ⇒ 缺资源时只做状态位并出声。

### K-11 `RuleEngine` 三件 · 3 条 · P0（引擎侧）
- `收藏:188`（目标筛选总入口，按 ta 分派）· `189`（卡牌级 stat/rarity/army/filterSpecific/excludeEnchantments）· `201`（traits/excludeTrait + summonRift 门）。
- 🥇 **已有一半地基**：`RuleEngine/Core/TargetCriteriaEval.cs` 自称「全仓唯一那份 36 格判据」；`CountEval.cs` 文件头已收 `FilterMethods__CheckIfMeetsCriteria.c` 的分段（含 `:210-232` 的 friendly/enemy 淘汰）。
- 施工法：**先建 ta→分支表（一格一行）→ 与现有实现逐格比对 → 补缺格 → 每格一条正负夹具**；未知 ta 必须出声。
- 验收口：`bash 工具/ruleprobe.sh play all dump` 差异逐条解释；`play check` 的哨兵（⑦输出指纹）不可被重生成基线洗绿。

### K-12 `Battle/ErrorMessageBanner.cs` + `Deck/DeckRuntime.cs` · 1 条
- `卡组:325`：给 `ErrorMessageBanner` 加「无战斗上下文也能弹」的入口 → `DeckRuntime.Say` 里加一行（两个 `Say` 出口 `TryAddCard` / `BeginCardDragAt` 已就位）。

### K-13 `Shell/CollectionWindow.cs` + `CardPresentation/Shaders/` · 1 条
- `收藏:348`：**已裁定忠实移植 Shiny、⛔ 不做加法 quad 近似**；`dump_shader_blob.py "UIEffect"` 已通（44.6 万字节 + 41 常量）；`_TransitionRotation 139°` 很可能烘在 `_TransitionTex_ST` ⇒ 先确认再决定是否传参；贴图在 `externals[14]`。

### K-14 `Shell/CampaignTab.cs` + `Shell/ForgeTab.cs` · 1 条
- `收藏:15`：两页各挂 13 颗阵营徽记（Collection 支已对）；两窗已有 `Badge Highlight` 常量与队列可对齐。
- ⚠️ `账/战役.tsv` 把 `ArmySelector.Initialize` 判 A（落点 `CampaignTab.cs:791`）⇒ **别当重复行**；跨账矛盾已由调度台转 N1。

### K-15 `Battle/CardDisplayWindow.cs` + `RuleEngine/Core/GameplayVariables.cs` · 4 条
- **前置（硬）**：`HUD:750/756/758` 都要原版 `globalVars` 的 6 个标量 + 两档 y（`+0x18/+0x1c/+0x20/+0x3c/+0x40/+0x44`、`+0x50/+0x54/+0x58`）—— **值还没从解包实例读出来**，先读再写（⛔ 别自己挑）。
- `HUD:755` 独立（SkillPanel 加载态两支，不依赖偏移数值）。
- `HUD:756` 与 `758` 是**同一个公式**两种偏移 ⇒ 抽一处（⛔ 别写两份）。

### K-16 `Core/CardView.cs`（SoftMask）· 1 条 · ⚠️ 与 K-03 抢文件
- **必须排在 K-03 收工之后**（同一文件只能一个写手）。体量大：搬 `UnityEngine.UI.Extensions.SoftMaskScript` + 材质 + `ChangeState(Big)` 那一路 + `_MaskEffectStrength` tween。建议单开一件。

### K-17 `Shell/Inventory.cs` + `Core/LoadableRef.cs` · **现派 1 条（`收藏:247`）** · 另一条待裁
- 🔴 **三源取证补完（2026-10-26 K1P0）**：
  - **`收藏:244` `GetOwnedCount<T>(LoadableReference<T>)` ⇒ 建议 D**：prefab `m_MethodName`/`m_OnClick`/`m_Delegates` **0**（对照：245 个 prefab 有 `m_MethodName`、860 个 MonoBehaviour JSON 有它 ⇒ 搜法有效）· 17 个场景 **0** · SO / `ItemDrawerConfig.asset` **0** · **C# 调用点也 0**（全 29 处 `GetOwnedCount` = 1 处无参 + 28 处 `(T item)`，无一处传 `LoadableReference`）。时间限定 = 本 build。另有结构论证：泛型方法**不可能**被 UnityEvent 绑定（`InventoryManager` 不是 `UnityEngine.Object`；inspector 也绑不了开放泛型）。
  - **`收藏:247` `HasEnough<T>(LoadableReference<T>, int)` ⇒ 保持 C（撤销「建议 D」）**：**3 处活调用** —— `CurrencyCondition__Validate.c:14` · `CardServices__CheckForCardUpgrade.c:88` · `UpgradePanel__CheckUpgradeRequirements.c:106`；三处实参都是 `*(x + 0x10)`，而 `UpgradePanel:140` 把同一字段喂给 `LoadableReference.get_Reference` 再当 `Currency` 用 ⇒ 实参确为 `LoadableReference<Currency>`。
- **施工**：① 建最小 `Core/LoadableRef<T>`（三格照原版 `LoadableReference`：`genericObjectReference` / `targetId` / `reference`）；② `Shell/Inventory` 门面加 `HasEnough<T>(LoadableRef<T>, int)` = `GetOwnedCount(tid) - amount >= 0`；③ 三处调用方都在别人的域（升级 / 改名 / 条件校验）⇒ 本批只做件、不动调用方。
- **验收口**：持有 = amount ⇒ true、amount−1 ⇒ false、amount+1 ⇒ true；TargetId 空 ⇒ 出声（⛔ 不许静默 true）。

### K-18 `HUD:306`（教程指向物）· **不派**
- 整条闸的数据源 = LiveOps `TutorialEvent`（随 PlayFab userInfo 下发），离线 build 恒不可达 ⇒ 建出来永远不显示、也无法验收。挂教程线，等它有「教程在跑」的本地入参 + 父链 scale 实读。

---

## §3 最小可交付包（MVP · 1 个写手串行，不必等整包）

> 目标：**1 条 P0 + 3 条低成本 P2**，文件互不相交、无前置、约 1–1.5 人日，调度台可**立刻**放一路人进去。

| 序 | 包 | 账行号 | 方法 | 施工要点（一句话） | 验（最小） | 估时 |
|---|---|---|---|---|---|---|
| 1 | K-02 | 收藏:203 | `GameModesIconCollection.Release` (P0) | `CardArt.ReleaseModeIcon()` 只放默认模式图标 + 销毁格那一跳调它；⛔ 别扩 `PerItemArtPrefix` | 销毁一批格 ⇒ `ReleasedCount` +N，图仍显示 | 40m |
| 2 | K-05 | 卡组:71 + 131 | `DeckCostQuantityRowDrawer.Initialize` / `DeckEnergyCostDrawer.Initialize` (P2) | `CostCurveDrawer.Build` 逐行加 0.2 s 值补间 + 0.4 s 色补间 | 行 fill 从 0 长到目标值 / instant 档一帧到位 | 2h |
| 3 | K-06 | 卡组:8 | `CardDeck.CanAddCard` (P2) | 「整副满」闸挪到最后 + 槽内同卡错误码改 `MaxCopiesReached`（同批改夹具） | 两闸同真 ⇒ 错误码 = MaxCopiesReached | 2h |
| 4 | K-08 | 收藏:273 | `ItemDrawer.OnPointerClick` (P2) | 把 `UI Click 1` 收进 `EXTRA_CUES` 重跑导入（`PlayInspectSound` 已留口） | 点抽屉一声；`false` 档不开窗 | 1h |

**MVP 验收口（合并）**：`bash 工具/typecheck.sh`（0/0）→ `CardArt` 读数（`ReleasedCount` 增量 + 图仍显示）→ `DeckScene.Run` 绿 → `_tmp_view` 里一声点击音 + 曲线补间真机看一眼。

---

## §4 并发波次建议（并发上限 10，别贴满）

| 波次 | 可同时开工的包 | 说明 |
|---|---|---|
| **第 0 波（1 路）** | **§3 MVP** | 不等任何人；1–1.5 人日 |
| **第 1 波（8 路）** | **K-11**（引擎 criteria，P0）· **K-01**（BattleDriver，20 条，最长）· **K-03**（CardView）· **K-04**（CardHighlight/CardInteraction）· **K-09**（PrebuiltDecks + 生成器）· **K-10**（DraftMode）· **K-07**（PracticeModePopup）· **K-15**（CardDisplayWindow） | 八路**文件互不相交**（K-04/K-09/K-15 对 `BattleDriver` 的需求一律走改动请求给 K-01） |
| **第 2 波（4 路）** | **K-02**（CardArt）· **K-12**（ErrorMessageBanner/DeckRuntime）· **K-13**（Shaders/CollectionWindow）· **K-14**（Campaign/Forge） | 与第 1 波也不冲突，可在第 1 波后期插入（上限内） |
| **第 3 波（串行 1 路）** | **K-16**（SoftMask，必须等 **K-03** 收工） | 同文件只能一个写手 |
| **待裁 / 挂起** | **K-17**（现派 `收藏:247` 一条；`收藏:244` 待裁 D）· **K-18**（挂教程线） | K-17 的 `247` 可直接派（挂接已取证：3 处 C# 活调用）；`244` 等调度台判 D；K-18 等教程线 |

**硬约束复述**：
- 🔴 **`Battle/BattleDriver.cs` = 单写手（K-01）**；K-02/K-04/K-09/K-15 的接线一律写成改动请求。
- 🔴 **`Core/CardView.cs` 两个写手会撞车**（K-03 与 K-16）⇒ K-16 串行。
- ⚠️ **K-06 是唯一碰 Unity 宿主（`Editor/DeckScene.cs`）的包** ⇒ 它的 `DeckScene.Run` 由调度台在全工程无其它批处理时跑。

---

## §5 P3 候选表（服务器 / 账户 / 云后端）

> ⛔ **本表不判 D** —— D 是调度台的裁定权。这里只给**证据 + 时间限定**。
> 红线复述：**我们自己的 P2P 联机（`Net/*`）· 本地存档（`SaveCenter`/`SaveKeys`/`LegacySaveReader`）· 本地战斗引擎（`RuleEngine/**`）· 一切「客户端 UI + 服务端数据」的 UI 部分** —— **一律不判 D**。

| 账行号 | 方法 | P3 证据（代码/资产行号） | 时间限定 | 可以照做的部分（不判 D） |
|---|---|---|---|---|
| 卡组:52 | `DeckAndWarlordData.SetPlayerData` | 载荷字段里 `eloRating` / `allianceRating` / `allianceBadge{featuredTrophy, level}` 来源是玩家账户数据（`PlayerBattleData` 由匹配/服务端装配）；.c 逐字段偏移已读出 | 本 build 离线状态下这几格**无数据源**（联机局只有玩家名） | **开局数据装配这一段照做**（字段齐、缺则出声）；UI（HUD 头像/卡背/称号）照做 ⇒ 不判 D |
| 卡组:49 | `DeckAndWarlordData.GetSerializedData` | 原版 `SerializedPlayerDeckData` 是**发给服务器的载荷**（反序列化侧 卡组:54 同族） | 同上 | **客户端侧序列化 + round-trip + 出声「不发」**即算复刻；⛔ 不接网络发送 |
| 卡组:50 | `DeckAndWarlordData.GetSerializedDeckData` | 同上（同一份 DTO 的字段清单） | 同上 | 同上 |
| HUD:306 | `CardDisplayWindow.SetupTutorialObj` | 闸 ②③ = `LiveOpsManager.GetHandler<GameModes>()` + `GetActiveEvent<TutorialEvent>()` ⇒ 事件随 **PlayFab userInfo** 下发（`Shell/TutorialModePopup.cs:29` 已记） | 本 build 离线 ⇒ `GetActiveEvent` 恒空 ⇒ 恒不可达 | **展示窗显隐逻辑 + 三条 TMP 照做**（挂教程线）；判据不可及，不判 D |

**说明**：其余 59 条**都不属于 P3**（纯客户端表现/引擎/数据本地化）；`卡组:285/286` 的 `IPlayEvent currentEvent` 虽与活动数据有关，但**过滤链本身用本地预组表就能照做**⇒ 不列 P3。

---

## §6 与旧账/旧裁定的冲突（派活前请调度台看一眼；证据全文见报告 §5）

1. 🔴 **`HUD:686` 的「语义相反」是拿两个类比的**：手牌 0.2 s 门 = 原版 `CardScript.OnTouchUpAsButton.c:37`（长按**不**开窗，我们已实现且正确）；棋盘 0.3 s = `ActiveWarlordAbility.Update`（按住**就**弹）。⇒ 施工**只补棋盘那一半**，⛔ 别动 `CardInteraction.LongPressNoDisplaySeconds`。
2. 🔴 **两个同名 `GetCardTier` 别混**：`卡组:48` = `DeckAndWarlordData.GetCardTier`（开局 `CardTiers` 字典，未命中 ⇒ `Tier1`）；调度台日志「#63 GetCardTier → A」讲的是**另一个**（按原卡键取档）。⇒ 别拿那条 A 裁定去关 `卡组:48`。
3. ⚠️ **`收藏:203` 的账文已过期**：账写「我们无句柄层、`CardArt._cache` 只增不减」，实际 `CardArt.Release` / `PathOf` / `Retain` / `MenuDraw.ReleaseArtUnder` **已在**（B8_BF 2026-10-26）。⇒ 账需就地订正（铁律 5），剩余缺口只有「模式图标那一族被 `IsPerItemArt` 挡掉」。
4. ✅ **`收藏:244/247` 已结（三源取证，2026-10-26 K1P0）**：`收藏:244` 三源 + C# 调用点**全 0** ⇒ **建议 D（时间限定 = 本 build）**；`收藏:247` **有 3 处 C# 活调用** ⇒ **保持 C，撤销原「建议 D」**。账原「两个重载都 0 外部调用点」这句**是错的**（只数了 `GetOwnedCount<`，没数 `HasEnough`）。
5. ✅ **`卡组:75`（DeckDrawer）已结**：**挂接存在 = C**（`ItemDrawerConfig.asset:201-214` 的 `PrebuiltDeck` 条目 + 两个 `Deck_cardback_avatar` offer prefab 各挂 1 个 `DeckAndCardbackDrawer`）；`调度台日志:1904` 那句「⛔ 不应该做」违反铁律 11，已由调度台就地订正为「做完今天看不见效果（时间限定：当前 build 0 条发放物）」。
   🔴 **方法论**：**0 个 UnityEvent ≠ 没挂接** —— 本例点击走 `IPointerClickHandler`，prefab 里本来就不该有 `m_MethodName`；判「没人引用」必须查「组件实例在不在 prefab 里」，不只是 `m_PersistentCalls`。
6. ⚠️ **`收藏:15` 跨账**：`账/战役.tsv` 把 `ArmySelector.Initialize` 判 A（落点 `CampaignTab.cs:791`），而本行要的是「Campaign/Forge 两支的每阵营徽记」。⇒ 不是重复行，但合并时要写清分工。
7. ⚠️ **行号漂移**（本轮现读 vs 账）：`CardView.BeginDissolve` :6986（账 :6961）· `SetRemnantBody` :2537（账 :2496-2522/:2632）· `SkillPanel.Show` :1535（一致）。⇒ 写手**按符号名认**，别按账里的行号跳。
