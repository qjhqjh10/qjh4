# W · 界面线写手代理 —— 红 B 收口 + `N116` + `A1488` 现核

> 写手代理 · 2026-10-18 · **没跑 Unity / `*.Run` / player**（按铁律 12 现行口径）· 没动 git 写操作 · 没改正本。
> 判据来源：诊断报告 `资料/普查产出_1018/DIAG_red_B_Menu_Rewards_Collection.md`（逐条现核）·
> 原版 prefab（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 的 `RectTransform/`）·
> 参考图 `C:/Users/qjh36/Desktop/warpforge参考图/卡组编辑界面参考.png` · 我们最新实拍 `_tmp_view/deck/deck_editor.png`。
> 类型检查（自己的 `TMPDIR=/tmp/wf_ui`）：**最终 0 / 0**（过程中有一次 `Shell/CampaignTab.cs(745,21)` 的
> `CS0815` —— 那是**别的代理当时正在写的文件**，隔一会儿重跑即 0，与本次改动无关）。

---

## §一 逐条表

| 项 | 改了什么（文件:行） | 判据 | typecheck |
| --- | --- | --- | --- |
| **A·滚动 1/6** 排行榜·阵营选择 | `Editor/MainMenuScene.cs:6879` 期望 `1f → 120f`；上方注释订正为「120 是我们挑的、不是原版值」 | 用户 2026-10-18 拍板保留 120（铁律 11 记「待将来评估的偏离」）；原版 = `D_N29` 的 1.0 | 0 |
| **A·滚动 2/6** 领奖窗 | `Editor/RewardsScene.cs:2231` `1f → 120f`（+ 同段注释） | 同上（原版 `Reward Window/Content/Scroll View` = 1.0，`D_N29:49`） | 0 |
| **A·滚动 3/6** 锻造页·阵营条 | `Editor/RewardsScene.cs:3467` `1f → 120f` | 同上（原版 `Forge Army Selector` = 1.0；MB `-8481382965408668617` 等） | 0 |
| **A·滚动 4/6** 锻造页·奖励轨道 | `Editor/RewardsScene.cs:3469` `1f → 120f` | 同上（原版 `Rewards Scroll View` = 1.0，`D_N29:48`） | 0 |
| **A·滚动 5/6** 战役页·阵营条 | `Editor/RewardsScene.cs:5260` `1f → 120f` | 同上（原版 `Campaign Army Selector` = 1.0；MB `1901330447545511309` 等） | 0 |
| **A·滚动 6/6** 战役页·轨迹 | `Editor/RewardsScene.cs:5262` `1f → 120f` | 同上（原版 `Campaign Track` = 1.0，`D_N29:47`） | 0 |
| **A·doc 订正** `MenuScroll.NotchPx` | `Shell/MenuScroll.cs:99-114`：把「**唯一一处**不是原版值的传参」订正为「**120 是我们挑的、不是原版值（原版 1.0 px/格）、用户 2026-10-18 拍板保留**；现共 **8 处**显式实参 = 主菜单模式卡 `MainMenuRuntime.cs:2206` ＋ 7 处：`LeaderboardWindow.cs:461` · `RewardWindow.cs:1121` · `ForgeTab.cs:399/443` · `CampaignTab.cs:208/372` · `DailyRewardPopup.cs:257`」 | 铁律 5（原 doc 过期）；逐处 `grep -n 120f` 现核 | 0 |
| **B1** `DeckInfoPopup` 命中区档 | `Editor/CollectionScene.cs:2777` 字面量 `3123 → 3129`（保留「⛔ 不写常量名」的灭自证写法）；同段 `:2772-2776` 写明十档梯子出处 | 实现 `Shell/DeckInfoPopup.cs:207-209`（`QDIHit = 3129`）；语义「命中区 > 压暗层 `QDI`(3120)」不变 | 0 |
| **B1·残留文案** `3123` | `Editor/CollectionScene.cs:96`（`HitZFront` doc）· `:2789`（9 颗命中区同档）· `:3619`（`QDIHit` 严格高于 `QDI`）· `:3695` / `:4207`（Warlord Image 命中区所在层）· `:5702` / `:5714`（A11 前置清扫文案）—— 全部 `3123 → 3129`。⚠️ `Shell/DeckInfoPopup.cs:96` **已无 3123**（十档重排那批已经改过）；该文件剩下的 3123 全是 **`QDIPanel`(3123)**（`:195/:207/:1323/:1453`），**是对的、未动** | 同上；`grep -rn "QDIHit.*3123"` 现核只剩订正注本身 | 0 |
| **B2** 收藏卡格中心 y | `Editor/CollectionScene.cs:4607` 期望 `347.9 → 328.76`，文案改成「**卡块**中心 y = 格心 347.9 − 张数条 38.272/2」 | `Shell/CollectionWindow.cs:225`（`CardsCounterH`）· `:1091`（按 345.728 反解缩放）· `:1137`（`CardsCardRect` 位姿）· `:1170`（`CardsCells` 存的是 `CardView.transform`）；实得 328.76 逐位吻合 | 0 |
| **B3** `★ A1212/A1226 Cost` 框高 | `Editor/CollectionScene.cs:9238` `boxH: 47.88f → 49.66f`；`:9228-9237` 写全原版链 | 原版 RT 现读：`GridLayoutGroup.cellSize.y = 58` ⇒ `Background` 高 58（RT `6569929413234690472`，anchors y 0..1 · sd.y 0）⇒ `Cost Image` 高 58+6 = **64**（RT `4517749637251500456`，anchors y 0..1 · sd.y **6**）⇒ `Cost` TMP 高 `0.776 × 64 = **49.664**`（RT `-3142486381871265368`，anchors y **0.112→0.888** · sd.y≈0）。47.88 = `RM` 表 B 的 55.7 行高那一支（`DeckRuntime.cs:254` `RowCostS = 61.7 = 55.7+6`）—— 张冠李戴 | 0 |
| **B4** `★ A1212/A1226 Count` 框宽 | `Editor/CollectionScene.cs:9312` `boxW: 44f → 22.87f`；`:9277-9295` 重写那段「不可比 / 不为凑 22.87 改几何」的过期口径 | 原版 `LayoutElement.m_MinWidth = 22.87`（MB `-8067466461408032344`）；同 GO 的 `Count` RT `-327133057421637208` 的 `m_SizeDelta = (0,0)` ⇒ 框宽由 `Text fill` 的 `HorizontalLayoutGroup` 撑开 | 0 |
| **B5（新账 `A2133`，假绿）** `Count` 框高 | **实现**：`Shell/DeckInfoPopup.cs:1988` 补 `boxH: 49.88f`（+ `:1977-1987` 注释）；**断言**：`Editor/CollectionScene.cs:9312` `boxH: 58f → 49.88f` | 原版 `Count` RT `-327133057421637208`：anchors y **0.07 → 0.93**（跨度 **0.86**）· `m_SizeDelta.y = 0` ⇒ `58 × 0.86 = **49.88**`；它是 `Text fill` 的布局子件（`childControlHeight=1 · childForceExpandHeight=1`）⇒ 高 = 父内高。节点仍居行心（锚跨度中心 0.5）⇒ 只补 `boxH`、不动摆位 | 0 |
| **C（`N116`）** 卡片详情窗残骸 | **实现**：`Shell/CollectionWindow.cs:3928-3943` —— `OpenCardDetail` **新建之前**把 `LastCardDetail` 收掉：`Close()`（走记账；已 `Closed` 的按原版首句早退）+ `DestroySafe`（批处理 `DestroyImmediate`）。**断言**：`Editor/CollectionScene.cs:8756`（前计数）/ `:8822-8833`（后计数，`FindObjectsInactive.Include`）—— 5 次开关只许多 **1** 扇 | 原版 `WindowsManager__CloseWindowCO.c` = `Hide()` → `List.Remove` → `Destroy`（+ 缓存释放）；我们原先这条链**从不 Destroy**（`GameWindow` 文件头也如实记着）。⛔ 没改成「复用单实例」（`CardDetailPopup` 还有别的宿主，生命周期不归本文件） | 0 |
| **E（可选）** `T09Probe` 三段探针 | `Editor/T09Probe.cs:92-129` 新增**阶段 C**：对同一批最多 100 张卡分别量 `CardText.Name` / `FaceTextFull` / `ArtKey`，再量整段 `ToCardData`，各报 total/avg/max + 「未归因(整段−三段)」 | 纯插桩：调用次序/实参与 `Battle/BattleDriver.cs:14113-14134` 逐字相同；**不改任何行为** | 0 |

> ⚠️ **5 条折行红（#1/#2/#3/#10/#11）本代理一个字没动** —— 根因在 `Battle/Label.cs` 的 `ReplayFitForFace`，
> 由另一路修（派单已写明）。

---

## §二 关键 diff 说明

1. **滚动灵敏度（6 条断言 + 1 处 doc）**
   - 只改**期望值与文案**，**没有改实现**：7 处 `120f` 实参（`LeaderboardWindow.cs:461` · `RewardWindow.cs:1121` ·
     `ForgeTab.cs:399/443` · `CampaignTab.cs:208/372` · `DailyRewardPopup.cs:257`）与 `MainMenuRuntime.cs:2206` 都在 HEAD 里已落地。
   - 每处注释都写明「**120 是我们挑的、不是原版值（原版 `m_ScrollSensitivity = 1.0`）；2026-10-18 用户拍板保留；
     按铁律 11 记为待将来评估的偏离**」。
   - `MenuScroll.cs` 的 doc 里那句「唯一一处」是 W16 之前的事实，已按铁律 5 就地订正为 8 处并留更正痕。

2. **`Count` 的框高（`A2133`）—— 实现 + 断言一起改**
   - 改前：`Txt(...)` 不传 `boxH` ⇒ `fitH = y2 − y1 = RowH = 58`；断言也钉 58 ⇒ **两边一起错、所以是绿的**（假绿）。
   - 改后：传 `boxH: 49.88f`（原版锚跨度 0.86 × 58）⇒ `SetAutoFitBox` 把 TMP 的 `sizeDelta.y` 写成 49.88，
     断言钉 49.88。节点位置未动（`SetVAlign(Midline)` 用的是显式传进去的行矩形，与框高无关）。
   - ⚠️ `Count` 的**宽**没改实现（22.87 由 P1 批已落地），只把断言从 44 跟到 22.87。

3. **`N116`（卡片详情窗）**
   - 修法刻意选「**下一次新建之前销毁上一扇**」而不是「关窗时销毁」：`GameWindow.Close` 是共用件（不在本代理文件所有权内），
     而 `CollectionWindow.OpenCardDetail` 是**本文件开的唯一入口** ⇒ 在这里收口零外溢。
   - 效果：任意次开关后场上最多 **1 扇**（最后那扇、保留原版「关掉只是不显示」那一态）。
   - 新增断言用「前后计数差 = +1」，不依赖「场上总共有几扇」（其他宿主/别的段落可能已经开着详情窗）。

---

## §三 `A1488` 逐条现核结果（判据 = `任务线/界面线/任务/T01_卡组编辑窗.md` §2·4 + 参考图 + 最新实拍）

> 方法：**逐条先现核「现在是否仍成立」**再决定改不改。参考图 = `桌面/warpforge参考图/卡组编辑界面参考.png`；
> 我们最新实拍 = `_tmp_view/deck/deck_editor.png`（2026-10-18 22:24，晚于本轮代码）。
> 我把两边的同一张卡/徽记放大到 3–9 倍并排比过（裁图在 `_tmp_view/a1488_check/`，可复查）。

| 条目 | 现核结果 | 证据 |
| --- | --- | --- |
| **⑥ 卡池列数（原版 6 / 我们 4）** | **已闭** | `Deck/DeckRuntime.cs:794` `Cols = floor(1589.8 ÷ 262.5) = 6`；实拍一屏 **6 列**（`_tmp_view/deck/deck_editor.png`） |
| **⑦ 界面语言是英文** | **已闭** | 实拍整屏中文（顶栏「玩家123」、页签「牌牌/卡组信息/美容品」、页脚「完成 30/30」）；属双语线，已在官方 zh-CN 表并入后落地 |
| **⑧ 卡名压在立绘上（原版在立绘下方的独立区域）** | **不成立 ⇒ 已闭**（当年基线图拿错，T01 自己已注明） | 参考图本人卡 `伊莱亚克·泽菲尔刃`：卡名同样**压在立绘上**（约卡高 55% 处，白字 + 暗晕），不是独立区域；我们实拍同形 |
| **⑨ 缺「阵营行」** | **已闭** | 实拍卡名下方有**橙色阵营行**（`萨姆-罕`），与参考图 `萨缪尔` 同位同色 |
| **⑩ 缺「张数/上限」那一行** | **已闭** | 实拍每张卡下方有深红计数条（`2/2` · `1/1`），左栏列表每行也有 `x2`（`DeckRuntime.cs:4222` `"x" + n`） |
| **⑪ 右上稀有度计数只有 3 个且数值不对** | **已闭** | `DeckRuntime.cs:1606-1644` 建 **4** 颗图标（common/rare/epic/legendary）+ 4 个数字；数值恒 `99` = **用户 2026-09-28 拍板**（`DeckRuntime.cs:134` 的注释与 `:4661`），**不是缺陷**（原版是 `WildcardDisplay` 的库存直出，单机没有发放源） |
| **⑫ 费用徽记位置/样式（原版右上蓝色圆 / 我们绿色六边形）** | **不成立 ⇒ 已闭** | 把参考图第 4 张卡（`守护者防御小队`）右上角放大 **9×**：原版就是**青色八边形 + 白字**（`_tmp_view/a1488_check/ref_badge4.png`），与我们实拍同一形状（`our_badge.png`）；英雄卡上方那枚是**卡框自带的纹章**，两边都有。T01 那句是低分辨率下的误读 |
| **A1525① 一张卡印两行、份数印两遍** | **已闭** | `Deck/DeckRuntime.cs:4047` 的 `DeckEntries()`：`:4058-4072` 按 `Id` 去重（督军/防御卡先记进 `seen`） |
| **A1525② 列表不按费用排序** | **已闭** | `DeckRuntime.cs:4076-4084`：费用升序 → 显示名（`StringComparison.CurrentCulture`）→ `Id` 序数（全序） |
| **A1525③ 行名居中（应左对齐）** | **已闭** | `DeckRuntime.cs:4206` 每次刷新 `AlignLeftOn(LayoutSpace.FromPixel(RowNameX,0).x)`（2026-10-20 那批落地） |

> **结论：`A1488` ⑥–⑫ 与 `A1525` ①②③ 在本轮现核下全部已闭，`Deck/DeckRuntime.cs` 一个字没改。**
> ⚠️ 唯一「看着像差异」但判不了的一条：卡底那颗菱形宝石我们偏青、参考图偏金 —— 它属于**卡框自带纹章**
> （`Core/CardView.cs`，不在本代理所有权），且很可能随稀有度/阵营变色 ⇒ **未改，也未立账**（见 §四）。

---

## §四 没查清 / 顺手发现（分诊）

1. **【没查清】`Count` 的「真布局宽」≈ 83.16px**（诊断 §三·1 的推导值）：原版 `Count` 的 rect 是**布局组驱动**
   （anchors 0,0 · `m_SizeDelta=(0,0)` · `LayoutElement.m_MinWidth=22.87 / preferredWidth=0 / flexibleWidth=-1`），
   父 `Text fill` 挂 `HorizontalLayoutGroup`（`ctrlW=1 · expandW=1 · align=3 MiddleRight`）。
   按 uGUI 布局算法推出的 ≈83.16 **没有实机量过**（`menu_dump.py` 对 TMP 的 pref 是 `LG_UNK`）
   ⇒ 本轮**没有据此改宽度**（断言仍钉 22.87 = `m_MinWidth`，与实现一致）。要钉死需真机 `GetPreferredValues`。
2. **【待主对话落盘】`RM_原版TMP框尺寸.md` 表 B #38 需要就地订正**（铁律 5）：
   55.7 行高是 **`Deck Editing Menu`** 那一支（`DeckRuntime.cs:254`）；**`Deck info Popup` 是 58 格 ⇒ `Cost` 高 49.66**。
   该文件不在本代理文件所有权内 ⇒ 只记在这里，**没动**。
3. **【已就地订正】`Shell/DeckInfoPopup.cs:96`**：派单说那里有一处 `3123` 残留 —— 现读**已经没有了**
   （十档重排那批顺手改过）。该文件剩下的 `3123` 全是 **`QDIPanel` 自己的档**（`:195/:207/:1323/:1453`），**正确、未动**。
4. **【顺手发现 · 未立账】收藏/编辑窗卡底那颗菱形宝石色相**（我们偏青、原版偏金）：
   属 `Core/CardView.cs`（本代理无所有权），且可能随稀有度/阵营变色（我们实拍那张 `司战` 与参考图那张
   `伊莱亚克·泽菲尔刃` 的稀有度/卡种都不同）⇒ **判不了**，建议主对话决定是否派卡面线核一次。
5. **【过程记录】类型检查的瞬时假错**：中途一次 `Shell/CampaignTab.cs(745,21) CS0815` —— 那是**别的代理正在写的文件**
   （`git status` 里它确实被改着），隔 30 秒重跑即 0 / 0。按铁律 13·3 未去动别人的文件。
6. **【口径提示】5 条折行红**（`Label.ReplayFitForFace`）本批未动，由另一路修；修好后
   `MainMenuScene` / `RewardsScene` 那 5 条应自然回绿，与本批 6 条灵敏度断言**互不影响**。

---

## §五（新增）音效线转来的 5 条「钮没建」缺口

> 派单：`W_音效线_缺口修复.md` §三 + `AUD_音效线_完整性复核.md` §五·4。
> 纪律同前：**没跑 Unity / 自检 / player**；只跑秒级类型检查（`TMPDIR=/tmp/wf_ui2`）——**最终 0 / 0**。
> 每条都先回原版 prefab **逐字段**核「节点名 / rect / 文字 / cue」，再落代码；查不到的写「没查到」并列出判据。

| # | 条 | 原版判据（pid / 字段，逐条现核） | 我们改了什么（文件:行） | 断言 | typecheck |
| --- | --- | --- | --- | --- | --- |
| **1** | `Player Profile Window ▸ Ranking ▸ {Current,Highest} Rank ▸ LeaderboardButton`（×2，应播 **Positive**） | ① rect（`档案窗_Profile页.md` §A 表 = `menu_dump` 实读）：Current `418.18,189.56→709.13,242.79` · Highest `811.95,189.56→1102.90,242.79`（290.95×53.23，**各自水平居中**、y 同值）· `Button Text` 框内缩 13.33/5.22/14.27/5.21。② `EverguildButton` MB `3930931442171673138`（Current）/ `-2929059975558170062`（Highest）：`m_onClickSound` = pid **8549838806721213354** = **`ButtonClick Positive`** · `useDefaultAudioClipIfNull=1` · `m_Interactable=1` · `m_OnClick.m_Calls` **空**。③ **`m_IsActive = 0`**（GO 实读），且 `档案窗_Profile页.md` §C·10 记「**查不到激活点**」（搜过的词全列着） | **节点与文案原本就有**（`ProfileTab.BuildRankCard`），本批补三件：**rect 改回原版逐值**、**命中区**、**Positive cue** —— `Shell/ProfileTab.cs:645`（rect/Node/Nine）· `:658-665`（注释+`Hit(...)`）· `:604-620`（`OpenLeaderboard()`） | **没加新断言**（见 §六·1 说明） | 0/0 |
| **2** | `Inbox Menu ▸ Content ▸ {Reset Button, … Claim Button}`（应播 **Positive** ×2） | ① `Reset Button` = `[399,126] 142×71` · `Button Text` = 字面量 **'DEBUG RESET'**（fs 23.2 · auto[10~40] · base 12 · Center/Capline · 折行 0）· MB `7902057683169346354` cue = **Positive** · `m_OnClick` 空。② `Claim Button`（`Message Display ▸ Content ▸ Scroll View ▸ Viewport ▸ Content ▸ Claim Button` 容器 `[306,883] 959×216` · 钮 `[663,910] 245×68` · `Button Text` `[675,917] 221×54` = I2 键 **`MainMenu/General/Claim`**（`m_text='Claim'` · fs40 · auto[10~40] · base 12 · 折行 0 · Capline））· MB `8813341034201514802` cue = **Positive** · `m_OnClick` 空。③ **两颗在运行时都不可达**：`InboxWindow__Open.c` 首句 `resetButton(0x90).SetActive(false)`（字段偏移 `dump.cs:129414`）；`DebugReset` 全量反编译**零调用者**；`Message Display` 空态整块关（我们没有消息源） | **建壳 + 命中 + cue**：`Shell/InboxWindow.cs:69-73`（rect 常量）· `:333-353`（Claim 链 + Nine + Text + `SetVAlign(Capline)` + `Hit`）· `:376-395`（Reset 同形，建完 `SetActive(false)`）· `:534/:545`（两个 onClick **如实出声**） | **没加新断言**（同上） | 0/0 |
| **3** | `RankedEventWindow ▸ DeckSelectionContinueButton ▸ Continue Button`（应播 **`MenuStartGame`**） | 该钮只存在于 **`RankedEventWindow`（v1）** prefab（`菜单全树.md:10316`：`DeckSelectionContinueButton [1081,913 833x50] ▸ Continue Button [1403,913 453x50]`（`Text 'Battle'`））；R3 `:261` 的 `MenuStartGame` 就是这一颗。🔴 **我们实现的是 `RankedEventWindowV2` + `SkirmishModeEventWindow`**（`Shell/RankedEventWindow.cs:1` / `Shell/LiveOpsEventWindow.cs:1`）—— 它们的对应件是 **`To Battle Button`**（V2 树 `:13991`），R3 `:203` 记它的 cue = **`ButtonClick Positive`**，我们**已建且已按 Positive 播**（`Shell/LiveOpsEventWindow.cs:961`） | **不适用 ⇒ 未改**（v1 整扇窗我们没实现；在 V2 里凭空加一颗 v1 的钮 = 违反铁律 3）。如果主对话要建 **v1 `RankedEventWindow`**，那是**整窗级**的一笔 | — | 0/0 |
| **4** | `EnergySinglePlayerOnlyEventWindow ▸ Reward Progress Panel ▸ … ▸ Score Bar Line Level 1..5 ▸ Chest`（×5，应播**默认音**） | `Chest` GO 五件一组（RT/Image/**Button/EverguildButton**/Modifier），`m_onClickSound` = **空 PPtr** + `useDefaultAudioClipIfNull = 1` ⇒ 默认 `ButtonClick`；`m_Transition = 1 (ColorTint)`（⛔ 不是 SpriteSwap）。动作 = `GenericScoreBarLine.Initialize` 挂的 lambda（`GenericScoreBarLine.__c__DisplayClass6_0___Initialize_b__0.c`）= **`RewardService.Preview(reward)`** | **补 5 个命中区**：`Shell/EnergySinglePlayerOnlyEventWindow.cs:559`（`MenuDraw.Hit(row, "ChestHit", chestRect, QHit, …)`，**不传 cue = 默认音**、不传 target = ColorTint 无换图）· `:469`（`OnChestClick(level)` 出声：本地没有奖励对象/预览窗） | **没加新断言** | 0/0 |
| **5** | `Rewards Base Submenu Variant ▸ … Missions Tab ▸ {Daily Login Container, Daily Skulls Mission Container, Weekly Mission} ▸ raycast target`（×3，应播 **Purchase Click**） | 三份容器 prefab 的**第一个子件**都叫 `raycast target`：anchors `(0,0)-(1,1)` · `m_SizeDelta=(0,0)`（**铺满容器**）· `Image.m_RaycastTarget = 1` · `m_Sprite = 0`（隐形）；兄弟 `background` 的 `m_RaycastTarget = **0**`（⇒ 卡面任何地方都落到它身上）；`EverguildButton.m_onClickSound` = pid **8111810622317061932** = **`Purchase Click`**（R3 `:64`；clip `UI PurchaseClick`）。动作 = `MissionContainer.SetChallenge` 的 lambda（`…DisplayClass9_0…SetChallenge_b__0.c`）：`AssetLocator.GetAsset<…>` 取**远端配置**里的任务信息窗 prefab 再开窗 | **补 3 个命中区**：`Shell/MissionsTab.cs:900 / 1062 / 1251`（三处调用）· `:1572`（`AddContainerHit`：档 = `QContent − 1` = **3009**，高于底板 3005、低于所有内容命中 3010+）· `:1579`（`OnMissionContainerClick` 出声：远端配置/prefab 本地没有） | **没加新断言** | 0/0 |

## §六 本批的说明 / 没查清

1. **为什么没加断言**：这 5 条全在 `Shell/*` 的**建窗**路径上，而对应断言要挂在 `Editor/MainMenuScene.cs`（档案/收件箱/活动窗）与 `Editor/RewardsScene.cs`（任务页）里、且要先摸清各宿主的取节点姿势；**本轮不跑 Unity** ⇒ 写没跑过的路径断言 = 给同步点埋假红。⇒ 节点名/rect/cue 全部按原版落好，断言留给主对话在同步点按下面这些**稳定节点名**加：
   · 档案页：`FindDeep(pp.transform,"Ranking")` 下两棵 `LeaderboardButton`，各 `FindChild(lb,"Hit").GetComponent<WindowButton>().clickCue == WindowSounds.PositiveCue`（⚠️ 两棵 `lb.gameObject.activeSelf == false`，**量 rect 用 `GetComponent<RectTransform>()`**，别用 `activeInHierarchy` 判存在）。
   · 收件箱：`FindChild(content,"Reset Button")`（`activeSelf == false`）· `FindDeep(_display,"Claim Button")` 两颗同名（外层容器 + 内层钮）。
   · 活动窗：`Score Bar Line Level k` 下 `ChestHit`×5（`WindowButton.clickCue == null` = 默认音）。
   · 任务页：三张卡下各一颗 `raycast target`（`clickCue == WindowSounds.PurchaseCue`）。
2. **`Purchase Click` 的 clip 仍是「待补」**（R3 `:64` 标 `❌ 待补`）—— 本批只接 cue 名（与商店那几处同一条常量）；**音频文件本身属音效线**。
3. **原版两颗「不可达钮」的处置**（第 1/2 条）：`m_IsActive=0` / `Open()` 首句关 —— 我们**照原版不激活**（激活 = 多出原版看不到的钮，属铁律 3 的反面）。命中与音已接，若主对话判定要「先让他能点」⇒ 每处只需把 `SetActive(false)` 换成 `true`（档案页那两行 / 收件箱那一行）。
4. **`Count`/`Cost` 那批（§一）不受本批影响**：本批只动 `Shell` 的 4 个窗口文件，没有碰 `DeckInfoPopup` / `CollectionWindow`。
5. **`Claim Button` 链里 `Viewport` / `Content` 的框是「照形状」**（原版 prefab 里是 `0×0` / `2×0` 的作者态）—— 已在 `InboxWindow.cs:339-343` 注释里如实标，⛔ 别当原版数值引用。

## §七 两处一行级收尾（2026-10-18）

1. **`Shell/SettingsWindow.cs:4500-4512`（`ToggleSmallScreenUI`）**：`SmallScreenUI.Set(on)`（**落盘/广播之后**）补
   `int costTierRedone = CardView.RefreshAllCostTiers();`（+ 一句出声）。判据 = 卡面侧第二波 `A1549` 尾巴
   （费用格两档只由 `SmallScreenUI` 切，而档位是建卡时烘死的 `CostAt01`；原版每次
   `CardTextCountersController.Toggle` 都按当时 `GameStaticData.smallScreenUI` 重算）。
   ⚠️ **`Core/CardView.cs` 的 doc 里那句「⏭ 接线待办：`SettingsWindow` … 补一句」现已完成** —— 该文件不在本代理
   所有权内 ⇒ **没改那行注释**，留给主对话按铁律 5 订正。
2. **`Editor/MainMenuScene.cs:3694-3703`**：`OrksDeck2` 的数据注释按铁律 5 就地订正 ——
   原来写 `complete = false`（`missing=["GOF20 x2 (miss-known)"]` · `problems=["size:28/30"]`），
   **实际 `complete = true` · `cardIds` 30 张 · `missing/problems` 皆空**（`RuleEngine/Resources/prebuilt_decks.json`
   2026-10-18 现读：103/103 副全 complete）；错因 = 数据侧已关账、注释没跟着走。
   连带订正「改坏法：换回 `tab` ⇒ 红」—— **今天暂时不红了**（`Tab == All`），结构理由不变（仍用 `All`）。
   typecheck：**0 / 0**。
