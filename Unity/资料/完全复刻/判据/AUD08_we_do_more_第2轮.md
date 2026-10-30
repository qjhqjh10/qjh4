# AUD08 第 2 轮 —— 反向审查「我们多做的」·「运行时状态恒不成立」族

> 只读查判据代理 `B103_A08` · 2026-10-30 · 第一百零三会话 · 波 1（第二棒）
> 上承：`判据/AUD08_we_do_more.md` + `.tsv`（第一轮，**已读、未重做**）
> 配套逐行清单：`判据/AUD08_we_do_more_第2轮.tsv`（7 列：账行 / 原版类 / 方法 / 原版为何不可达（出处）/ 我们做了没（文件:行号）/ 判档 / 建议）
> ⚠️ 本代理**只读**：未改任何 `.cs` / 账 / 正本 / `d:/2`；写盘仅本 `.md` 与配套 `.tsv`。

---

## 〇 一句话结论

- 第一轮已验证「**资产侧死**」（0 实例 / 死类）这一族**全覆盖**，本轮按它自己指出的入口做**第二族**：
  **「运行时状态恒不成立」**——某字段在**某个具体 prefab/场景实例**里恒 null / 恒 false / 恒 0 / `m_PathID=0`
  （铁律 5·c「一个值 ≠ 全部情况」的反面：**这一个实例上它恒不成立**）。
- 判据族命中 **220 行 D**（21 个账文件）；其中 **57 行我们侧有落点**（另有 163 行 `我们的落点 = —`，
  按定义不可能是「多做」，只登记不计入）⇒ **本轮判定 = 这 57 行**。
- 结论：**3 条「多做了（可见·判不准）」**（全部是 `个人资料` 的**边框层/边框钮恒显示**）、
  **12 条「多做了（休眠·当前不可见）」**（其中 **3 条是本轮新增的宿主**）、**2 条判不准**、**40 条不是多做**。
- 与第一轮一样：**没有发现「一进游戏就比原版多画一块」的已确证件**；唯一可能可见的是边框家族（半条判据读不到，调度台已裁保留）。

---

## 一 覆盖面（账的边界 —— ⛔ 别把「判完 57」当成「判完 D 档」）

| 项 | 数 |
|---|---|
| D 档总行数（21 个账文件求和，2026-10-30 现读） | **9,641** |
| 本轮判据族（运行时状态恒不成立）命中 | **220** |
| 其中我们侧**有落点**（= AUD08 只能出现在这里） | **57** |
| 其中我们侧**落点 = —**（我们没做 ⇒ 不可能是多做） | 163 |
| 本轮判定 | **57**（见 TSV 逐行） |
| 判定结果：多做了（可见·判不准） | **3** |
| 判定结果：多做了（休眠·当前不可见） | **12** |
| 判定结果：判不准 | **2** |
| 判定结果：不是多做 | **40** |
| **未覆盖** | 其余 **9,421 行 D**：P3 服务器类（≈944+）· 纯编辑器工具（≈78）· 「decomp 0 调用点」桶（≈500+，多为内联假象，见第一轮 §一）· 编译器生成物 |

**「判据族」怎么圈出来的**（可复跑，⛔ 别另发明口径）：
对每个账文件逐行取 11 列，筛 `状态 == D`，再在 **判据要点 + 差异 + 备注** 三列里正则命中下面任一条：

```
恒 null|恒 false|恒 空|恒 0|永远 null|永远 false|恒为|恒不|恒返回|恒 early|恒空转|全 null
|例外①|判据为空|m_PathID = 0|pid=0|m_IsActive = 0
```

「有落点」= 我们的落点列非空、且不以 `— / - / 无 / 无。 / （无 / （不 / 不复刻 / （不需要 / 未定位` 开头、不含「查不到」。
⚠️ **故意没收 `0 命中`**：那是被项目反复证伪的弱否定证据（序列化数据不含字段名，铁律 2 / §「判资产在不在 ⛔ 不能按名字搜」）；
若把它收进来，候选会从 220 涨到 275，其中约 55 行是**假候选**（例如 `ReflectionExtensions` 那一族我们**有活实现**）。

---

## 二 判定

### A 多做了（可见 · 判不准）—— 3 条，**同一根**

| 账行 | 件 | 我们做了什么 | 原版为何（可能）没有 |
|---|---|---|---|
| `个人资料_成就_排行:56` | `AvatarBorderController.Initialize` | `Shell/AvatarTab.cs:335` 画「已装备那条」边框 | 该类**零实例零引用**（死类）；但**同 prefab 的 `Border` Image 序列化了 `Player Profile Border` 精灵** ⇒ 「画不画」两边都有，差在**显隐闸** |
| `个人资料_成就_排行:77` | `AvatarTab.FindActiveAvatarBorder` | `Shell/ProfileData.cs:1378` 两遍遍历 + **`Default` 兜底** ⇒ **恒非 null** | 原版尾部 `return 0`：无活动边框时返回 null ⇒ `AvatarTab.cs:628 ApplyBorderLayer()` 与 `:676` 的钮**都隐藏**；我们**恒显示** |
| `个人资料_成就_排行:98` | `CosmeticItemAvatarBorder.IsParentEventActive` | `Shell/ProfileData.cs:1429 IsBorderUsable`（同一层等价物） | 同上（同一处偏离的第二个落点） |

**本轮新拿到的硬证据（补台账上缺的那一格）**：
`d:/2/tools/assetripper_out/ExportedProject/Assets/Player Profile Window.prefab` —— 4 个 `Border` GameObject 的
`Image.m_Sprite` **全部** `fileID: 21300000, guid: e367be22b6999d44ba0b25d6893eb937`，
该 guid 落到 `Assets/Player Profile Border.asset`（= **Default** 边框图）。
⇒ **原版 prefab 里边框层带着 Default 图**，所以「我们画 Default 边框」本身**不是**多做；
**多出来的只可能是**「原版在无活动边框时把这一层/这颗钮关掉，我们没关」。

**为什么仍判「判不准」而不是「多做」**：台账已记「`Default` 那档 `parentEvent` 为空 ⇒ 原版 `IsParentEventActive` 抛空引用
⇒ **无活动时到底返回 null 还是把钮留在 prefab 态**这半条判据读不到」。本代理复核后**同意**：
`CosmeticItemAvatarBorder__IsParentEventActive.c` 的 null 分支确实是抛（对照 `__get_ParentEvent.c` 是 null 安全的），
所以「原版到底会不会在遍历到 `Default` 时抛」取决于**背包列表里有没有 Default** —— 这要看运行时，静态拿不到。
**唯一解法（留给调度台派真机）**：起原版 → 打开档案窗（头像页）→ dump `Avatar Tab/Selected Item Panel/Border` 与
那颗边框钮的 **activeSelf**。两者都是 `True` ⇒ 我们是复刻、本条销账；`False` ⇒ 我们把「恒不成立」做成了可见件，按原版关掉。

> 调度台 2026-10-28 已裁「保留 Default 兜底」，本轮**不改这个裁定**，只把 AUD08 视角补齐并给出关闭条件。

### B 多做了（休眠 · 当前不可见）—— 12 条

| 账行 | 件 | 为什么不可见 | 与第一轮的关系 |
|---|---|---|---|
| `日常_奖励:732/733/734` | **`SurvivalEventWindowEnemyMarker`**（`Shell/SurvivalEventWindowEnemyMarker.cs:66/74/82`） | 原版 0 实例、本机无生存事件窗与 prefab；我们建了宿主但**运行时 0 调用方**（只有 `Editor/ShellScene.cs:10985` 自检调 `Bind`） | 🆕 **本轮新增**（第一轮没列） |
| `日常_奖励:159` | `EventBadge.ShowNotification` | 只由 `EventBadgeController.BuildBadge` 创建（`EventBadgeController.cs:70 AddComponent<EventBadge>()`），而那个控制器运行时零调用方 | 第一轮 #3 的**同一宿主**，本轮补到方法级 |
| `日常_奖励:160` | `EventBadgeController.Awake` | 同上 | 第一轮 #3 |
| `日常_奖励:20/21/22` · `菜单_外壳:43` | `BoostTimerPanel.Init/SetDescription/SetTimer` · `BoostMenu.HideAllBoosters` | `BoostMenu` prefab 本地不存在 ⇒ 无宿主 | 第一轮 #2 |
| `菜单_外壳:54/60` | `ToggleNewCardBadge`（`Core/CardView.cs:15139`） | 视图逐句对得上，但**「这张是不是新卡」的数据没有**（`CardProgress` 只有 levels/owned/wildcards）+ **调用点没有** ⇒ 徽标恒不显示 | 🆕 本轮新增登记（属「没做完」，非「做多了」） |
| `个人资料_成就_排行:328` | `UIRankingListController.ChangeSeasonRankingView`（`Shell/LeaderboardWindow.cs:931`） | 我们侧 `AllowChangeSeason` **恒 false** ⇒ 生产路径不可达，只有自检走 | 🆕 本轮新增登记 |

> 共同点与第一轮一致：**休眠件不会让画面变多，但谁一接就多**。建议这 4 族在**声明处**打
> `⏸ 原版不可达`（`SurvivalEventWindowEnemyMarker` 是新的那一个），并配一条「不被运行时 Build」的断言。

### C 判不准 —— 2 条

1. `战斗界面_HUD:796` `RawCardScript.HasAlternativeArtStyles()` —— 第一轮判不准 #2，**结论不变**（随 LiveOps 配表裁定）。
2. `个人资料_成就_排行:113` `LeaderboardConfig.GetLeaderboardDefinition` —— 数据源在服务器（P3）；
   我们的 `Shell/LeaderboardData.cs:223 LocalScore` 是**方案 §四 b 档已裁的本地模拟** ⇒ 不算多做，只是把「判 D 的理由」记清。

### D 不是多做 —— 40 条

四类，每类都有代表行（详见 TSV）：

| 类 | 例 | 为什么不算 |
|---|---|---|
| **我们确实没做那一支** | `战斗界面_HUD:145/150/158`（`vanguardFrame`/`codexFrame`/`reflectedFrame` 恒 null）· `战斗逻辑驱动:388`（引用语义我们没建，已裁「永久 B」） | 落点是「—（原版零行为）」或「永久 B」，**没有可见物** |
| **载体不同 / 数据源不同** | `收藏:95-100`（`CardsCollection` 我们走 JSON 表）· `收藏:168-173`（`CollectionManager` 我们静态表）· `本地化:2`（无 Addressables） | 原版的「集合/收包」在我们这里换成等价数据层，**行为面一致** |
| **等价实现（闸一致，两边都空转）** | `收藏:336-339`（`UpgradableCardFilter` 我们 `f.Upgradable` ⇒ 恒空）· `社交:349`（`ChatPreview` 我们恒不显示）· `战斗逻辑驱动:410`（`ctx.CampaignStage` 我们**零写点** ⇒ 与原版同为 null）· `日常:65/66/69/70`（`ChallengeSerial*` 我们只涨，本地模拟已裁） | 原版恒不成立的地方，我们也恒不成立 |
| **P3 显式理由** | `联机:117`（`DoesPlayerDataAcceptInvites` 我们好友页恒空态） | 服务器数据本地无源，方案 §四 P3 口径 |

**本轮独立复核的实例证据（不只看台账文字）**：
`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/BattleAnims.json` 现读 ——
`reflectedPrefab / stealthPrefab / codexActivePrefab / plasmaEnergyGainAnim / psykerAnim / reactorAnim /
sentinelUnitAnim / sentinelDeckAnim / psychicEnergyGainAnim / reflectedCreationAnim / reflectedDestroyedAnim`
**11 个引用全部 `m_PathID = 0`** ⇒ 台账 `战斗界面_HUD:145/158/161` 那三行「恒 null」**成立**，且我们侧确实没做（落点 —）。

---

## 三 两轮合起来的 AUD08 结论

| 轮 | 覆盖族 | 判定数 | 多做（可见） | 多做（休眠） | 判不准 |
|---|---|---|---|---|---|
| 第 1 轮 | 资产侧死（0 实例 / 死类）· 237 行 / 114 类 | 5 + 2 | 0 | 4（+1 已接线无消费者） | 2 |
| 第 2 轮 | 运行时状态恒不成立（实例字段 null/false/0）· 220 行 | 57 | **0 已确证**（3 条判不准，可能可见） | 12 | 2 |

⇒ **AUD08 这一族到此可以收口**：两个方向都做过，唯一还有敞口的是
① 边框家族（需真机 activeSelf，一条）；
② 两条 P3 判不准（配表/服务器数据回来才能定）。

---

## 四 附录 · 顺手发现

1. 🔴 **`assets_full/bundle_menus_assets_all` 没有 `Image/` 目录** —— 想在离线包里读「某颗 Image 用哪张精灵」是读不到的。
   ✅ 正解 = **`d:/2/tools/assetripper_out/ExportedProject/Assets/<X>.prefab`** 的 YAML（`Image.m_Sprite` 带 guid），
   再用 `.meta` 反查图名。本轮靠这条路才拿到「`Border` 恒有 Default 图」这条硬证据。
   ⛔ 别据此再写「主菜单包没导出 Image ⇒ 拿不到」。
2. **`BattleAnims.json` 有 11 个字段全是 `m_PathID = 0`** —— 这是本 build「一大批战斗表现帧恒不存在」的总闸。
   凡是依赖它们的 **frame/prefab 淡入淡出、反射框、潜行框、codex 框、等离子/灵能/反应堆的增益动画、哨兵动画**，
   **原版这一格都是空转**。建议将来做战斗表现时把这张表当「**负面清单**」用：谁要在这上面加东西，必须先说明为什么原版没有它。
3. **台账 `个人资料:77`/`:98` 的状态列已判 D，但两行的「差异」列写的是「残差只剩一条 / 待调度台裁」** ——
   状态与正文其实一致（D + 已裁保留），但读起来像未结案。建议统一写成
   「D（调度台 2026-10-28 裁：保留 Default 兜底；AUD08 视角 = 可能的可见多做，解法 = 真机 activeSelf）」，一句话归拢。
4. **「休眠件」现在有 5 族**（TipPrebattle · BoostTimerPanel/BoostMenu · EventBadgeController/EventBadge ·
   **SurvivalEventWindowEnemyMarker（新）** · `ToggleNewCardBadge` 视图）。建议调度台建**一张统一的休眠件登记表**，
   并在每族的类头打 `⏸ 原版不可达`，否则「谁一接就多」这件事会随波次不断增加。
5. 🔴 **AUD09 交叉发现的一条「可见的多做」**：`Battle/TutorialOverlay.cs:254-266` 的教学箭头贴图是
   **`🔶 自己设计（非原版）`**，注释自己写着「**原版两颗 `SpriteRenderer` 的 `m_Sprite` 是真·空引用，原版本来就不显示箭头**」。
   ⇒ 原版那一格恒不成立，我们画了两根箭头（教程局里看得见）。按铁律 18·0（自设计只允许服务器相关）**需调度台裁**；
   ⚠️ 但要先排除「`m_Sprite` 空是因为远端 Addressables 没下发」（同 `BattleAnims` 11 个 `pid=0` 那一族）。
   详见 `判据/AUD09_资源落地_第2轮.md` §四。
