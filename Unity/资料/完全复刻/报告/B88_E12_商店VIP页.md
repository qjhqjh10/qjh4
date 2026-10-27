# B88 · E12 —— 商店 VIP 页（整条垂直线）

> 执行代理 `E12` · 2026-10-27/28 · 波 · **整条做通**（不是半建）
> ⛔ 未跑 Unity / `-executeMethod` · 未动 git · 未改 `CLAUDE.md` / `项目任务.md` / 账主表 / 正本。

---

## 〇 一句话结论

`商店.tsv` 里 VIP 那 5 条 C 行**全部处理完**：**4 条升 A、1 条升 B（带证据，驱动值在服务端）**，
外加**第 6 条 `_DebugGrantVIP` 维持 D**（页建好了，调试钮仍按 D 不建 —— 原版玩家面也看不到它）。
VIP 页现在是**第 4 个可达页签**：左栏第 4 颗键 → `WindowTabType.ShopVip(13)` → `Card Shop VIP Tab Variant` 页，
页内 **VIP / 非 VIP 两条分支都实现且都可走**（`ShopData.SetVipForTest(bool)` 可切）。

---

## 一 四件门槛的落地（逐条对 J1 的裁定）

| J1 查出的门槛 | 本批处置 | 落点 |
|---|---|---|
| ① `WindowTabType` 没有 `ShopVip` | ✅ **已有**（另一个代理已在本批白名单外的那一行加好，我直接用了；⛔ 我没有动 `RewardsWindow.cs`） | `Shell/RewardsWindow.cs:89` `ShopVip = 13,` |
| ② 左栏只有 3 颗键 | ✅ 加第 4 颗（VIP），母版顺位挪到第 5 颗；`visualTypes` 同步 | `Shell/ShopWindow.cs:87-92`（键表）· `:120`（`visualTypes`） |
| ③ `Store.CanPurchase`（服务端 ⇒ 查不到） | ✅ 按「查不到 ⇒ 本地兜底 + **出声**」：`ShopData.IsVip`（`🔶 自己设计`，取「是 VIP」）+ 到期时刻恒 `null` ⇒ 倒计时 `--` + `TimerDisplay` 出声一次 | `Shell/ShopData.cs:533` `IsVip` · `:538` `VipExpiresAt` · `:541` `VipExpiryUnknownReason` · `:545` `SetVipForTest` |
| ④ 页内容（同构复用 `ShopTabPage`） | ✅ 第 4 个 `PageSpec` + `Offers(3)` + `Type` 返 `ShopVip` + VIP 专属两段 | `Shell/ShopData.cs:131`（`PageSpec`）· `:251`（`Offers`）· `:554`（`_vip` 货架）· `Shell/ShopTabPage.cs:896+` |

---

## 二 判据（全部现读，⛔ 没有一处是截图估的）

**甲、几何 —— prefab 字段**
`工具/menu_rect.py bundle_menus_assets_all "Card Shop VIP Tab Variant" --size 1752.83x1009.06 --relative --cs`
（本页 prefab 的 `m_Father = 0`，量法与另外三页**同一套**：相对 `Tabs` 左上角量，再统一 +(167.17, 70.94)）
＋ `工具/menu_dump.py bundle_menus_assets_all "Card Shop VIP Tab Variant"`（组件/字号/对齐/九宫/颜色）。

| 节点 | 相对值（`Tabs` 左上角） | 绝对设计 px | 关键字段 |
|---|---|---|---|
| `VIP Status` | 720.72,12.10 → 1032.11,60.99 | **887.89,83.04 → 1199.28,131.93** | 锚 `(0.5,1)-(0.5,1)` · `pos (0,−12.1)` · 311.40×48.89 |
| `VIP Icon` | 652.36,2.37 → 720.72,70.72 | **819.54,73.31 → 887.89,141.66** | `40K_icon_card_VIP` 512² · `Simple + preserveAspect` · 68.353² |
| `VIP Timer` | 727.62,12.10 → 949.18,60.99 | 随 `Yes VIP` 字宽走（见下） | 锚 `(1,0.5)` of `Yes VIP` · `pos (+6.8999,0)` · 221.56×48.889 |
| `Block Input` | `a(0,0)-(1,1)` | = `Packs Scroll View` | 无 sprite · 色 `(0,0,0,0.2196)` · `m_RaycastTarget = 1` |
| `Shadow drop` | `sizeDelta (−716.733,−798.19)` | **688.12,526.72 → 1561.63,680.90** | `40k_Smooth shadow background` 102² · 九宫 50,50,50,50 · 色 `(0,0,0,0.7647)` |
| `No VIP`（遮罩里的字） | `a/pivot` 都居中 · `sizeDelta (0,112.064)` | **547.78 → 659.84**（高 112.064） | `fs 51.11` · 折行 1 · h=Left/v=Midline(4096) · 色白 |
| `tooltip trigger` | — | **1136.98,579.57 → 1187.99,628.05** | `40k_generic_bt_info` · `m_RaycastPadding (−15,−15,−15,−15)`（负 = 外扩） |

⚠️ **两处宽度本地量不出**（原版是 `ContentSizeFitter(h = PreferredSize)`，`sizeDelta.x = 0`）：
① `Yes VIP` 那颗字 → 我们**先建字、量渲染宽、再摆 `VIP Timer`**（与 `BuildTimeCounter` 同一口径，`ShopTabPage.cs:1010-1031`）；
② 遮罩那行字 → 给一个**以遮罩中心为中心**的 800 宽框（`Label` 出厂按框中心摆 ⇒ 视觉等价），**高与 y 用原版值**（`ShopTabPage.cs:951`）。

**乙、生命周期 —— 反编译方法体**（`d:/2/tools/decomp_full/VIPCardShopTab__*.c` + `cpp2il/out_cpp2il_dev_ilspy/VIPCardShopTab.cs`）

| 原版方法 | 体 | 我们 |
|---|---|---|
| `OnOpen` | `RefreshOffers(); CheckVIPState();` ＋ 调试钮三与闸 ＋ `vipTimer.OnRunOutOfTime += VipTimerOnOnRunOutOfTime` | `OnOpen()` → `Setup()`（= 刷报价 + 重判态）；订阅在 `BuildVipStatus` 里挂（`:976` / `:1057-1060`） |
| `OnClose` | `vipTimer.OnRunOutOfTime -= VipTimerOnOnRunOutOfTime;` | `:982` 同一句 |
| `ToFocus` | `RefreshOffers(); CheckVIPState();` | `:988` 重跑 `Setup()` |
| `VipTimerOnOnRunOutOfTime` | `CheckVIPState();` | `:996` |
| `CheckVIPState` | `can = Store.CanPurchase` ⇒ `playerIsVIPObject.SetActive(can)` ＋ `playerIsNotVIPObject.SetActive(!can)` ＋ `can` 时 `vipTimer.Initialize(GetExpirationDate(RequiredItem))` | `:1003`（VIP 态建状态行）/ `:1067`（非 VIP 态建遮罩）；门在 `Setup` 的 `:247` / `:273` |

🔴 **那两个 GameObject 的身份 = 现解 prefab 的 PPtr**（⛔ 不是按名字猜 —— 树里有**两个**同名 `No VIP`）：
`playerIsVIPObject` = `-8306817185179266151` = **`daily shop header/VIP Status/Yes VIP`**；
`playerIsNotVIPObject` = `-252537654527552615` = **`Packs Scroll View/No VIP` 那整块遮罩**。
⇒ 头部那个同名 `No VIP`（`VIP Status/No VIP`）**不是它**：出厂 `m_IsActive = 0`，且全包**没有任何 MonoBehaviour 引用它**
⇒ 原版自己也不画 ⇒ **我们不建**（否则会造出一个原版永远不显示的件）。

**丙、文案 —— 官方本地化表**（`CardPresentation/Resources/official_loc_13lang.json`，三个键都在，EN/ZH 两列都非空）

| 键 | en | zh |
|---|---|---|
| `MenuShop/VIPCardStore/YouAreVIPHeader` | `You are VIP. Time Remaining:` | `您是 VIP。剩余时间：` |
| `MenuShop/VIPCardStore/YouAreNotVIP` | `You have to become VIP to access this store` | `您必须成为 VIP 才能访问此商店` |
| `MenuShop/VIPCardStore/YouAreNotVIPTooltip` | `Stay consistent with your daily streak and you’ll gain VIP status.` | `坚持每日连胜，您将获得 VIP 身份。` |
| `MenuShop/VIPCardStore/Title`（页签） | `VIP Card store` | `VIP 张牌商店` |

⚠️ **prefab 头部那颗 `No VIP` 的静态 `m_text` 拼错成 `acces`**，遮罩里那颗是 `access` —— 两颗**同一条 mTerm**
（`MenuShop/VIPCardStore/YouAreNotVIP`）⇒ 运行期都由 I2 表供给，**拼写差异只存在于静态默认值**，我们照表取值。

**丁、`CheckTimer`（「本页要不要刷新倒计时」）**
`VIPCardShopTab : CardShopTab` 且**不 override `CheckTimer`** ⇒ 继承 `CardShopTab` 的 **9 点规则** ⇒ **true**。
⇒ `ShopTabPage.PageShowsTimer` 的 `_page == 0` 改成 `_page == 0 || _page == VipPage`（`:887`），
`PageSpec.TimerAsText = true`（本页 `TimeCounter` 的第一颗子件在 prefab 里就是 TMP `Refreshes in:`，实读）。

**戊、调试钮（维持 D）**
`DebugGrantVIPButton` 就是页头那颗 `Reset`（文案 `Grant VIP (1 day)`；PPtr `-5385996312866024551` → GO `Reset`）。
`OnOpen` 的闸 = `!EnvironmentHelper.IsPlayerFacingEnv && SettingsDebugButtons.DebugUIEnabledUI && Store.RequiredItem != null`
⇒ **玩家面恒假** ⇒ 原版玩家看不到它 ⇒ **不建**（账 330 维持 D）。

---

## 三 改动清单

| 文件 | 改了什么 | 行数变化 |
|---|---|---|
| `Shell/ShopTabPage.cs` | ① `Type` 第 4 页返 `ShopVip`（**同一行改写**）② `Setup()` 两处**行内追加**（`:247` 建 VIP 状态行 / `:273` 建非 VIP 遮罩）③ `PageShowsTimer` 纳入 VIP（同一行）④ 类尾新增 VIP 整段（常量 / 队列 / 7 个方法 / 3 个读口） | `git diff --numstat` = **+257 / −12**；**A 档落点 0 漂移**（新增全落在 `:827` 之后，其余是同行改写） |
| `Shell/ShopData.cs` | ① `Pages` 加第 4 个 `PageSpec`（含判据注释）② `Offers` 加 `case 3` ③ 类尾：`IsVip` / `VipExpiresAt` / `VipExpiryUnknownReason` / `SetVipForTest` / `_vip` 货架 ④ `Dump()` 那段过期注释就地订正（`6/10` → `7/12`，"没有断言" → 有） | **+53 / −2** |
| `Shell/ShopWindow.cs` | ① 键表加第 4 颗（VIP，母版顺位 → 第 5）② `visualTypes` 加 `ShopVip` ③ `NotifyNotBuilt` 里"三页 prefab" → 四页（同行）④ 文件头"三个页签"那句补一句 VIP 变体（同行） | **+9 / −3** |
| `Resources/Art/ui_menu/40K_icon_card_VIP.png` | **新建**（原版 `bundle_boosterpacks_assets_all/Sprite/` 原图拷入，316,832 B） | 新文件 |
| `Resources/Art/ui_menu/40k_Smooth_shadow_background.png` | **新建**（原版 `bundle_duplicateassetisolation_assets_all/Sprite/` 原图拷入，3,923 B；空格 → 下划线，本仓惯例） | 新文件 |
| `资料/完全复刻/账/_parts/B88_E12.tsv` | 账补丁 6 行（5 条 VIP C 行 + 1 条 D 行备注更新），**11 列表头** | 新文件 |
| `资料/完全复刻/报告/B88_E12_商店VIP页.md` | 本文件 | 新文件 |

> 两张 PNG **没有 `.meta`** —— 与 `工具/import_original_art.py` 的既有做法一致（它只拷 PNG，`.meta` 由 Unity 首次导入生成）。
> ⚠️ 若想统一导入设置，下次开 Unity 后跑一次 `ArtBaker.ApplyImportSettings`（两张都是 UI 小图，不跑也不影响取图）。

---

## 四 验收读数

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_s88e12f bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** ✅ |
| `merge_ledger_parts.py --dry --only B88_E12` | **更新 6 · 新增 0 · 无变化 0**（未写盘） |
| `check_ledger_points.py 商店` | A=118 · 通过 106 · **漂移 12** · 告警 0 · **失败 0** |
| 那 12 条漂移**全部是本批造成的行号位移**（`ShopData.cs` 8 条：+7/+8 行；`ShopWindow.cs` 4 条：+6 行） | ⛔ **我没有跑 `--fix`**（另有 `.cs` 写手在飞；按纪律只能由调度台在同步点串行跑一次） |

---

## 五 ⚠️ 预期红（`Editor/ShopScene.cs` 不在本批白名单，本批**未动**）—— 请在同步点前处理

🔴 **其中第 1 条不是"红"而是【崩溃】**：`ShopScene.Run` 会在那里抛 `IndexOutOfRangeException` 并**掀掉整条 Run**。

| # | 位置 | 现状 | 应改成 | 后果 |
|---|---|---|---|---|
| 1 | `Editor/ShopScene.cs:1440-1447` | `tabTerms` 只有 **3** 项，而循环是 `for (i < ShopData.Pages.Length)` | 数组补第 4 项 `"MenuShop/VIPCardStore/Title"` | **不补 ⇒ `tabTerms[3]` 越界 ⇒ 整条 `ShopScene.Run` 抛异常中止** |
| 2 | `:1379` | `Check(CountByPrefix(bar,"ShopTabButton_"), 4, …)` | **5**（4 页 + 母版） | 红 |
| 3 | `:1382` | `Check(CountVisible(bar,"ShopTabButton_"), 3, …)` | **4** | 红 |
| 4 | `:1383` | 断言 `ShopTabButton_3` 是母版且关着 | 改成 **`ShopTabButton_4`**（第 4 颗现在是 VIP 页键、可见） | 红 |
| 5 | `:2077` | `dump439.Contains("有奖励表 6/10 件")` | **`7/12`**（VIP 页 2 件里 1 件填了 `Grants`） | 红 |
| 6 | `:1470` / `:1474` | 渲染断言循环 `for (i < 4)`、`Check(tabN, 4, …)` | 建议 **5**（把母版那颗也量进去）—— ⚠️ 不改也只是**少覆盖一颗**，不会红 | 不红 |
| 7 | `:4360-4372` 实拍段 | 只拍 `Cards/Daily/Items` 三张 | 建议补 `Click(3)` + `Shoot("04_商店_VIP.png")` | 不红（只是少一张实拍） |

> 📌 除了 1～5 之外，**§「页签切换」那个循环（`:1533` 起）对第 4 页会自动生效且预期全绿** —— 已逐条核过：
> `Check(win.CurrentTab, (WindowTabType)(10 + p))` 对 p=3 恰好是 **13 = `ShopVip`**；
> `daily shop header` / `TimeCounter` / `Packs Scroll View` 三处矩形与前 3 页**逐位相同**（同一套常量）；
> `Line` 不建、`RefreshText` 在（`TimerAsText = true`）、格数 = `Offers(3).Length = 2`、两种购买路都在。

---

## 六 没查清的部分（如实记，⛔ 没有拿猜测填空）

1. **VIP 态的**真值 `Store.CanPurchase`** —— 服务端（`DailyStore` 的 store SO 一份都没导出；VIP 是账号态、在 PlayFab）。
2. **VIP 到期时刻** —— `InventoryManager.GetExpirationDate(Store.RequiredItem)`，入参 `RequiredItem` 来自 `DailyStoreData`，
   本地没有那份 SO ⇒ 恒 `null` ⇒ 倒计时 `--`。
3. **VIP 页的货架内容** —— 原版是 `VIPCardShopTab` 自己那份 `DailyStore` 的报价，同样在服务端 ⇒ `_vip` 那 2 件是**我们编的**
   （与另外三页同一处置：本文件头「下面是**我们编的**」一并覆盖）。
4. **`tooltipAnchor = 17` 在 9 项目表里是 `TopCenter`** —— 已核（`Core/Tooltip.cs:539`），不是未知值。
5. 两处 **CSF 量出来的宽度**（`Yes VIP` / 遮罩里那行字）**本地量不出** —— 处置见 §二·甲 的 ⚠️。

---

## 七 顺手发现（只报，⛔ 没越界改）

1. 🔴 **`Editor/ShopScene.cs` 那条 `tabTerms` 会越界**成了整条 Run 的**崩溃点**（见 §五 #1）——
   这是"加一页"必然踩到的地方，建议以后**把期望表写成按 `PageSpec` 派生**（`Pages[i].Label` 以外的文案也进 `PageSpec`），
   免得每次加页都要改自检。
2. **`ShopData.Dump()` 那句注释过期了两处**（"`6/10`" 与"`Editor/ShopScene.cs` 只是 `Log` 它、没有断言"）——
   已就地订正（铁律 5）。
3. **`Shell/ShopWindow.cs` 文件头"本地只有三页 prefab"** 之类的话也已随本批更新（同行业、零行数变化）。
4. ⚠️ **本批期间 `Shell/RankedDeckSelector.cs` 一度出现 9 条 `CS0246`（`PlayerDeck` 找不到）** ——
   **不是本批文件**（E10 的件，正在写）；几分钟后复跑已回到 **0/0**。记在这里只为"复跑时若又见到，先看清是谁的文件"。
5. **`RewardsWindow.cs` 的 `WindowTabType` 枚举成员是别人加的**（`ShopVip = 13`）—— 本批**一个字没动**那个文件。

---

## 八 自检复跑：`ShopScene.Run` 2 条红 ⇒ 判定与修复（2026-10-28）

### 8·1 判定 = **(α) 断言过期**（不是 (β) 实现缺陷，也不是 (γ) 本批回归）

| 判 | 结论 | 证据 |
|---|---|---|
| (β) 实现缺陷？ | **否** | 我们的 `MaxOffset` 自带夹是**设计**：`Shell/MenuScroll.cs:303` `MaxOffset { get { return Mathf.Max(0f, ContentX2 + ExtraPadHi - ViewHi); } }`，而 `:300-302` 那段注释写明 A269 那次订正的正是「**行为一直是对的，错的是这个读数**」（空列表会给 −778.79）。与 uGUI `ScrollRect`「内容不满视口 ⇒ 不可滚」同一条。 |
| (γ) 本批回归？ | **否** | 本批没改任何滚动算法或常量（`Packs Scroll View` / `GridLayoutGroup` / `MenuScroll` 一字未动）；出问题的是**新加的第 4 页**，前 3 页读数与改前逐位相同。 |
| (α) 断言过期？ | **是** | `Editor/ShopScene.cs:1775-1776` 的期望写的是**没夹的** `wantH − 视口高`，隐含前提「每一页的内容都比视口高」。这个前提被 2026-10-27 新增的 VIP 页打破。 |

**算术核对（差异量 470.38 为什么这么整齐）**：`GridLayoutGroup cell 475 / 2 列`，视口高 = 1080 − 127.62 = **952.38**。

| 页 | 件数 | 行数 | 内容高 | 内容高 − 视口高 |
|---|---:|---:|---:|---:|
| Cards | 4 | 2 | 957 | **+4.62**（旧三页就靠这 4.62px 可滚） |
| Daily | 3 | 2 | 957 | +4.62 |
| Items | 3 | 2 | 957 | +4.62 |
| **VIP（新）** | **2** | **1** | **482** | **−470.38** ← 正是报出来的那个数 |

⇒ 旧期望 −470.38、实得 **0**（`Mathf.Max(0f, …)` 夹出来的），差的 470.38 = 952.38 − 482 **一分不差**。
第二条红是同一个根因：`MaxOffset` = 0 ⇒ `SetOffset(0)` 不动 ⇒ 那一格停在**顶对齐**的内容行位，而断言期待「贴视口底」。

### 8·2 改了什么（只动 `Editor/ShopScene.cs` 的两条 + 紧邻注释）

| 位置 | 旧断言钉的 | 新断言钉的 |
|---|---|---|
| `:1775`（+ `wantRange` 与一条 `if (wantRange <= 0f)`） | 「可滚范围 = 内容高 − 视口高」（**没夹**） | ① `MaxOffset == max(0, 内容高 − 视口高)`（**带夹**，判据 = `MenuScroll.cs:303`）；② 内容比视口矮时**另断一条**「范围必须是 0（`TopAligned` 不滚）」—— ⛔ 不是「非空就算过」：前 3 页仍要求 4.62，VIP 页仍要求 0 |
| `:1786`（底对齐那条） | 「滚到底 ⇒ 最后一格贴着视口底」（写死 `y2 − CellH`） | 分两档：**内容比视口高** ⇒ 仍贴视口底（前 3 页，期望值**与旧式逐位相同**）；**内容比视口矮** ⇒ 顶对齐，最后一格在 `ScrollView.y1 + 7 + row×475`（VIP 页 = 134.62..609.62） |

行号变化：`Editor/ShopScene.cs` **+18 / −0**（两处只加不删；前 3 页的期望数值**一个都没变**）。
⚠️ `资料/完全复刻/账/泛型与启动链_补读.tsv` 的**备注列**里有一句「唯一调用点是自检 `Editor/ShopScene.cs:1897-1901`」——
那只是**备注**（不是落点列，机检不查它），加了 18 行之后它现在指向 **1915-1919**。⛔ 我没改账主表，请调度台顺手订正。

### 8·3 验收（**按调度台要求：本代理不跑 Unity**）

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_s88e12b bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** ✅ |
| 要跑的那一条（**请调度台串行跑**） | `unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod ShopScene.Run -logFile d:/4/_tmp_view/shop2.log` |
| 期望 | **2 失败 → 0 失败**（其余读数不变：上一条跑是 2486 通过 / 2 失败） |
| 若仍红，先看这两条 | ① `可滚范围 = max(0, …)` 是否报「实得 ≠ 4.62（= 957 − 952.38）」⇒ 那才是真回归；② VIP 页那条新断言「内容 482 比视口 952 矮 ⇒ 范围必须是 0」—— 它**专为「把 `MaxOffset` 改成不夹」这种改坏法**准备（不夹 ⇒ 实得 −470.38 ⇒ 红） |

### 8·4 顺手发现（只报）

- `Editor/ShopScene.cs:1820` 附近那条 `CheckNear(gs.Offset, gs.MaxOffset, 0.5f, "…随后 SmoothDamp **回弹到上界 4.62**")`
  是**通过**的（比的是活值 `gs.MaxOffset`），但标签里那个 **4.62 是页特定的** —— VIP 页上是 0。建议以后把标签改成「回弹到上界（`gs.MaxOffset`）」。
- 同段 `:1781` 那条「在顶时最后一格的格底**被视口裁住**」在 VIP 页上是**真空过**（那一格 134.62..610.62 全在视口内，根本没被裁）—— 标签只对前 3 页成立。
