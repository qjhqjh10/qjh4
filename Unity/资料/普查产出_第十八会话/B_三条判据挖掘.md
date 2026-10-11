# B · 三条判据挖掘（只读）

> 只读普查代理产出 · 2026-10-18 · ⛔ 未改任何代码 / 未跑 Unity / 未动 git。
> 结论一律落在**原版**（解包资源 + 全量反编译 + 运行时实况 dump），⛔ 没拿我们的实现当判据。

---

## 问题 A：信箱 vs 名牌，谁压谁

- **结论（一句话）**：**原版主菜单里没有「名牌压住信箱」这回事** —— 那块 446.5 宽的名字条
  （`Planer Name Background`）**整支不激活**（父件 `Background Mask` 是 `RectMask2D` 且 `m_IsActive = false`），
  全库代码 **0 引用**；原版可见的那块名牌底是**另一颗** `Player Profile/Background`（`40k_main_player frame`，
  宽 388，右沿 **411.0**，距 `InboxBtn` 左沿 **425.30** 净空 **14.30**）。⇒ 该动的是**名牌（不建它）**，不是 `InboxBtn`。
  ⚠️ **兼报：这条账已经是【过期账】—— 2026-10-11 就修完了**（见下「顺手发现 ①」）。

- **证据**（逐条）：
  1. `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/GameObject/Background Mask.json`
     —— `"m_IsActive": false`；其组件（`menu_dump` 实读）= **`RectMask2D`**（字段 `m_Padding` / `m_Softness`）。
  2. 同包 `GameObject/Planer Name Background.json` —— `m_IsActive: true`，但**父件不激活** ⇒ 不画。
  3. **运行时实况**（另一条独立腿）：`d:/4/Unity/资料/原版实拍/menu_topbar_0927/runtime_ui_dump_MainMenu_Warpforge.tsv:80-81`
     —— `Player Profile/Background Mask` = `False/False`；`…/Background Mask/Planer Name Background` = `True/**False**`。
  4. **兄弟序**（铁律「谁压谁读 `m_Children` 兄弟序」）：
     `bundle_scenes_scenes_mainmenuwarpforge/RectTransform/RectTransform_1092.json`（`Upper bar`）
     `m_Children = [1101 Background, 1560 SettingsBtn, **1120 TopBarButtons**, **1125 Player Profile**, 1187 Resources Bar]`
     ⇒ `Player Profile` 排在 `TopBarButtons` **之后** ⇒ uGUI 里**后画 = 压在上面**。
     （`Player Profile` 自己的 `m_Children = [1104 Background, 1116 Background Mask, 1091 Player Name, 1094 Profile border, 1219 Avatar Item Small, 1279 Player Level]`）
  5. **可射线件**（铁律：可点区 = 可射线件的**并集** + 各自 `m_RaycastPadding`，**负 = 外扩**）：
     · `InboxBtn`（GO 组件 RT `1561` / CanvasRenderer `1016` / **Image MB2529** / **Button MB2029** / MB2349 / MB1925 / MB1926）
       —— **Image** `m_RaycastTarget: 1`，`m_RaycastPadding = {x:-6.28, y:-23, z:-5.7, w:-23}`（**四边全外扩**），
       `m_Type: 0`(Simple)，`m_PreserveAspect: 1`，sprite = `40K_notification`；
       —— **Button** `m_Transition: 1`(ColorTint)，`m_TargetGraphic → 2529`（= 那颗 Image），`m_OnClick.m_Calls: []`。
     · 名字条那块 **Image MB1934**：`m_RaycastTarget: 1`，`m_RaycastPadding` 全 `0`，`m_Type: 1`(Sliced)，
       `m_Color = (0.3962, 0.1925, 0.3095, 1)`（暗紫）⇒ **它既是画上去的那块，也是会吃掉点击的那块**。
  6. **重叠率**：`Planer Name Background` = x `25.6..472.1` × y `14.9..60.5`（446.5×45.6）；
     `InboxBtn` = x `425.30..480.30` × y `15.50..55.50`（55×40）
     ⇒ 交叠 `(472.1−425.3)×(55.5−15.5) = 46.8×40` ÷ `55×40` = **85.09%**（与账上「85%」逐位吻合）。
  7. 可见名牌底（活的）仍在：`Player Profile/Background`（Image `40k_main_player frame`）= x `23.0..411.0` × y `11.6..135.6`（388×124），
     `activeSelf/activeInHierarchy` 全 `True`（dump `:79`）。
- **我取的是哪一份实例 / 凭什么**：
  · 只在 `bundle_scenes_scenes_mainmenuwarpforge` 里取 —— 该包 `Upper bar`（RT `1092`）是**全场景唯一**同名 GO，
  父链 = `Game UI/Main Canvas/MainMenu(外)/Safe area Only Horizontal`（同包 `RectTransform_1540.json` 是父，
  `m_Children` 13 项含 `Upper bar`/`Navigation Panel`/`ChatPreview`…）。
  · 交叉腿 = 运行时 dump 的 path 列逐行对得上（`:66-93` 的 pos/size/activeSelf 与 JSON 逐值一致）⇒ 不是我推的，是实况。
- **还差什么**：无（三条腿：prefab 字段 / 兄弟序 / 运行时实况 dump，互相独立且一致）。
  ⚠️ 一个**没查**的点（不影响裁定）：原版是否存在**某个状态**把 `Background Mask` 打开 —— 我已按铁律 5·c 追了
  「谁在运行时改它」：`d:/2/tools/all_strings.txt` 里 `Background Mask` / `Planer Name` **各 0 命中**，
  `d:/2/tools/decomp_full/`（25,096 份）里两份**各 0 文件命中**，`PlayerProfileUIController`（`dump.cs:109452`）
  的 8 个字段里**没有底板那一格** ⇒ 判据指向「作者做了没启用、也没人打开」。**这是「按现有三条腿全空」，不是「确证永远不激活」。**

---

## 问题 B：Rating 下两支的显示条件

- **结论**：互斥条件 = **原版 `IRankedEvent` 上的一个布尔 `get_IsLegendaryElo()`**，写在 `RankedMenuContainer` 里，不是「排名档位」推算：
  - `X = rankedEvent.IsLegendaryElo`
  - `X == true`（传奇）⇒ `Mission Milestones Progress` **`SetActive(false)`**；`Global Rating` **`SetActive(true)`**
    （并同开 `Position #` / `Position Value`）；再 `AllianceRatingDisplay.Initialize(rating − LegendaryElo, 段位图 sprite, …, true)`
  - `X == false` ⇒ `Mission Milestones Progress` **`SetActive(true)`**（并 `MilestonesStepDisplay.Initialize(current, total, null, hasToDoSealsAnim, hasToDoSealsAnim)` 印玺动画）；
    `Global Rating` **`SetActive(false)`**（`Position #` / `Position Value` 同步关）
  - **没有活跃 ranked 事件**（`playEvent == null`）⇒ **两支都关**
- **证据**（逐条，全部机器码/字段级）：
  1. `d:/2/tools/decomp_full/RankedMenuContainer__RefreshContentDisplay.c:16-27`
     —— `plVar7 = thunk_FUN_18042e360(playEvent, DAT_1842ad4f0)`（接口强转）；null 分支：
     `SetActive(milestonesStepDisplay.gameObject, false)` + `ToggleLegendaryRankInfo(false)`。
  2. 同文件 `:29-36` —— `cVar4 = FUN_1800021f0(3, DAT_1842ad4f0, plVar7);`
     `SetActive(milestonesStepDisplay.gameObject, cVar4 == 0);` `ToggleLegendaryRankInfo(cVar4);`
     ⇒ **一条判据同时决定两支，且方向相反**。
  3. `RankedMenuContainer__ToggleLegendaryRankInfo.c:8-24` —— 先 `foreach (legendaryRankDivisionInfoObjects) go.SetActive(option)`，
     再 **`SetActive(ratingDisplay.gameObject, option)`** ⇒ `Global Rating` 的开关就在这里。
  4. `dump.cs:105377-105396`（`RankedMenuContainer` 字段表，带偏移）：
     `milestonesStepDisplay // 0x68` · `legendaryRankDivisionInfoObjects // 0x70` ·
     `rankedPositionValue // 0x78` · `ratingDisplay // 0x80`；属性 `currentRankedEvent : IRankedEvent`。
  5. `RankedMenuContainer__get_currentRankedEvent.c` 用的就是**同一个 `DAT_1842ad4f0`** 去强转，而该属性声明为 `IRankedEvent`
     ⇒ **`DAT_1842ad4f0` = `IRankedEvent`**（钉死了后面那个「下标」属于谁）。
  6. **`FUN_1800021f0` 的真身**（这是账上「读不到名字的泛型方法」）——用现成工具反出来：
     `D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x1800021f0 30`
     ⇒ 它**不是业务方法、是 il2cpp 的【接口方法调用器】**：读 `[obj+0x12e]` 接口表长度、扫 `[obj+0xb0]` 接口表
     找 `klass == rdx` 的那项，取方法指针、`this = 第三参` 跳过去。
     ⇒ **第一参 = 接口方法下标，第二参 = 接口类型，第三参 = 对象**。（同族的 `FUN_180002280(N, IFACE, obj, arg)` = 带 1 个实参的同款。）
  7. `dump.cs:130514-130570`（`IRankedEvent` 接口成员，含 Il2CppDumper 打印的 `Slot`）：
     `0 SeasonEndDate` · `1 SeasonNumber` · **`2 get_LegendaryElo() → int`** · **`3 get_IsLegendaryElo() → bool`** ·
     `4 IsRanked` · `5 MaxEloCap` · `6 GetBaseData` · `7 ToggleRankedMode` · **`8 GetDivisionByPoints(long)`** · `9 GetCurrentRankedDivision`。
     **交叉验证下标编号 = 接口声明序**：`AllianceRatingDisplay__Initialize.c` 里用 `FUN_180002280(8, IFACE, event, points)`
     取回的对象，其 `[3]`(0x18)/`[4]`(0x20) 被当作 `RankedDivision.DivisionNumber`/`DivisionLabel`
     （`dump.cs:130545-130548`）⇒ **下标 8 = `GetDivisionByPoints`** ⇒ 2 = `LegendaryElo`、3 = `IsLegendaryElo` 一并坐实。
  8. 语义自洽两条：`X` 被当 `char` 布尔用（slot 3 返回 bool）且与「传奇段位 → 关印玺行 / 开大段位图 + 名次」完全自洽；
     `AllianceRatingDisplay.Initialize` 里 `FUN_1800021f0(2, …)` 的结果被当**阈值** `if (iVar6 < iVar8)`（`LegendaryElo`）用。
  9. **两支在 prefab 里 `m_IsActive` 都是 `true`**（`bundle_menus_assets_all/GameObject/Global Rating_-4564886699666007538.json`
     与 `…/Mission Milestones Progress.json`）⇒ **互斥 100% 在运行时代码里**，⛔ 序列化默认值当不了判据（铁律 5·c）。
  10. 另有一条**自我隐藏**分支（这条**不适用**于排位卡）：`AllianceRatingDisplay__Initialize.c:25-32`
      —— `if (param_4 != 0 && param_2 == 0) { SetActive(false); return; }`；排位卡这条调用**传 `param_4 = 0`**
      （`RankedMenuContainer__RefreshContentDisplay.c:125`）⇒ 不走。
- **我取的是哪一份实例 / 凭什么**：全库（所有 `bundle_*/GameObject/`）里 GO 名恰为 **`Rating`** 的只有 **2 个**，
  都在 `bundle_menus_assets_all`，父链分别是
  `Rating ← Skirmish Ranked Game Mode Container 1x2` 与 `Rating ← Ranked Game Mode Container 1x2`。
  两者**子树逐字段完全相同**（`menu_dump --rt` 两份输出一字不差）、挂的是**同一个 `RankedMenuContainer`**
  ⇒ 不存在「谁权威」问题。**并且落点是钉死的（不是推的）**：`RankedMenuContainer` 实例
  （`MonoBehaviour_4921922172160219662.json`）里 `milestonesStepDisplay → 1470769935273728526`、
  `ratingDisplay → -537402948850378226`，二者正是 `Rating`#B（RT `7063492927571073550`）的两个子件
  （`Mission Milestones Progress` GO / `Global Rating` GO）。
  ⚠️ 另有 **`MainRating`** 一族（父链含 `Player Profile Window` / `RankedEventWindow` / `Ranked Division Info`）
  也各挂一对同名子件 —— 那些**不是账上这条**（账上点名父件是 `Rating`），且它们的 `Mission Milestones Progress` 挂的是
  另一个类 `MissionMilestonesDisplay`（`dump.cs:61785`），**别混进来**。
- **读不出来的部分 + 差什么**：`AllianceRatingDisplay.Initialize` 的 `param_4/5/6` 三个 bool 我只读出**分支走向**
  （`param_5==0` 走「value.ToString()」、否则走 `string.Format(模板, 段位标签, …)`；`param_6 & param_5` 参与门控），
  其**字符串模板** `DAT_18426aff0` 等字面量**没解**（那要 `stringliteral.json` 换算，属文案细节）。
  **与「哪一支显示」无关**，不影响裁定。

---

## 问题 C：导航高亮是不是 Toggle / 怎么移动

- **结论**：**是 Toggle，不是 Button**（`EverguildToggle : Toggle`，**没有** `Button` 组件），
  而且它挂在一个 **`ToggleGroup`（`m_AllowSwitchOff = 0` = 单选）**里
  ⇒ **点别的钮一定会把高亮移过去**（互斥），高亮件 = 该 Toggle 的 **`graphic`** = 子件 **`Selected highlight`**（`toggleTransition = 1` ⇒ **Fade**）。
  另有**独立的一层 hover 色偏**（`m_Transition = 1` 的 `m_HighlightedColor`），且 **已经选中的那颗 hover 时不亮**。
- **证据**：
  1. `bundle_scenes_scenes_mainmenuwarpforge/GameObject/Main Menu Navigation Button - Home.json`
     —— 组件 = RT `1123` · MB `2098` · MB `2556` · MB `2199` · MB `2128` ⇒ **没有 Button**（`OpenWindowButton.button` 指的就是那颗 Toggle）。
  2. **MB2098 = `EverguildToggle`**（`m_Script` `-2618094383086163561`，`dump.cs:112970` 明写 `EverguildToggle : Toggle`）：
     `m_Transition: 1`(ColorTint) · `m_TargetGraphic → 1972` · **`toggleTransition: 1`** · **`graphic → 1947`** ·
     **`m_Group → 2616`** · **`onValueChanged.m_PersistentCalls.m_Calls: []`** · **`m_IsOn: 1`** ·
     `allowClickWhenAlreadySelected: 0` · `colorTintOnValueChange: 0` · `changeSpriteOnValueChange: 0`（后两条 = 自己那套配色/换图**全关**）。
  3. **MB2616**（挂在 `Buttons Container`，`GameObject/Buttons Container.json`）=
     **`ToggleGroup`，唯一字段 `m_AllowSwitchOff: 0`** ⇒ **恰好一个 on**（radio）。
  4. MB2199 = `NavigationPanelToggle`（`toggle → 2098`，**`toggleType: 0` = `Main`**）；MB2556 = `OpenWindowButton`
     （`button → 2098`、`windowToOpenScene → 1927`、`closeOtherMenus: 1`、`closeOnToggleChangeToFalse: 1`）；
     MB2128 = `GameObjectUniqueIdentifier`，`id = "HomeMenuButton"`。
  5. **偏移自证**：`dump.cs:927429-927438` `Toggle.graphic // 0x108` · `m_Group // 0x110` · `onValueChanged // 0x118` · `m_IsOn // 0x120`
     ⇒ 与 `EverguildToggle__OnPointerEnter.c:3`（读 `+0x120`）、`OpenWindowButton__Awake.c:73`（用 `plVar1[0x23]`=`0x118`）**逐位吻合**。
  6. **点击 → 开窗 → 关上一个**：`OpenWindowButton__Awake.c:69-75` 判到 `m_Button` 是 `Toggle` ⇒
     `AddListener(Toggle.onValueChanged, ToggleValueChange)`（另一支判 `Button` 时用的是 `plVar1[0x20]` = `Button.m_OnClick // 0x100`）；
     `OpenWindowButton__ToggleValueChange.c`：`true ⇒ OpenWindow`、`false ⇒ closeOnToggleChangeToFalse 则关窗`。
     ⇒ 点 B 时，ToggleGroup 会把 A 的 `isOn` 置 false ⇒ **A 的窗自己关**。
  7. **高亮件**：`MonoBehaviour_1947.json` = Image（sprite `503137736542995528` = dump 里 `Selected highlight` 那颗，
     tint `(1, 0.0805, 0, 1)`），**`m_RaycastTarget: 0`** ⇒ 高亮件不参与射线。
  8. **运行时还有一条腿**：`NavigationPanelController__SetMainToggle.c:44-52` —— 逐颗 `Toggle.set_isOn(toggle, toggleType == Main)`
     （进主菜单时把「Main」那颗点亮、其余灭）；`NavigationPanelController__SetToggle.c` —— `if (!isOn) SetIsOnWithoutNotify(true)`。
  9. **hover 别当选中**：`EverguildToggle__OnPointerEnter.c:3` —— `if (!allowClickWhenAlreadySelected && isOn) return;`
     ⇒ **已选中的钮连 hover 过渡都不走**；`Selectable.m_HighlightedColor = (0.7216981, 0.8151793, 1.0)`（浅蓝）是**独立那层**。
  10. 出厂初始态（`menu_dump` 实读）：Home `isOn=1`，其余四颗 `isOn=0` ⇒ **PLAY 出厂就是亮的**（与我们「恒在 PLAY」现象同源，
      但**原版点了会换**）。

---

## 搜过哪几个包 / 哪些词

| 包 / 文件 | 扫了什么 | 命中 |
|---|---|---|
| `bundle_scenes_scenes_mainmenuwarpforge`（GO 697 · MB 1134 · RT 592） | 按名取 `Upper bar` / `Background Mask` / `Planer Name Background` / `InboxBtn` / `Player Profile` / `TopBarButtons` / `Background` / `Main Menu Navigation Button - *` / `Buttons Container` / `Navigation Panel`；**按 pid 直读** MB2529/2029/1925/1926/1090/1934/1091/1104/1116/2098/2556/2199/2128/1973/2616/1947/1972 | 全中（详见上） |
| `bundle_menus_assets_all`（GO 16768 · MB 35014 · RT 16510） | GO 名 `Rating` / `Global Rating` / `Mission Milestones Progress*` / `MainRating`；MB 按 pid `4921922172160219662` | `Rating` **2 个**；`Global Rating` 8 个；`Mission Milestones Progress*` 15 个 |
| **全部** `bundle_*/GameObject/`（逐包 ls + grep） | 词：`Global Rating` · `Mission Milestones Progress` · `AllianceRatingDisplay` · `^Rating(_\|\.json)` | 只有 `bundle_menus_assets_all` + `bundle_scenes_scenes_mainmenuwarpforge` 命中 |
| `d:/2/tools/decomp_full/`（25,096 份） | 词：`AllianceRatingDisplay`（13 文件）· `MilestonesStepDisplay`（4 文件）· `MilestonesStepDisplay__Initialize`（**仅自身 1 处**）· `FUN_1800021f0`（**几万处**，是通用接口调用器）· `"Background Mask"`（**0**）· `Planer Name`（**0**）· `NavigationPanelController` / `EverguildToggle` / `OpenWindowButton` 全族 | 见上 |
| `d:/2/tools/all_strings.txt` | 词：`Background Mask` · `Planer Name` · `40k_topmarquee_currency_display` · `40k_main_player frame` · `TopBarButtons` · `InboxBtn` | **全 0**（⇒ 无 `transform.Find("<名字>")` 这类按名引用） |
| `d:/2/tools/il2cpp_out/dump.cs`（42 MB） | 类：`RankedMenuContainer` `IRankedEvent` `EverguildToggle` `Toggle` `NavigationPanelToggle` `PlayerProfileUIController` `MilestonesStepDisplay` `AllianceRatingDisplay` `RankedDivision` | 全中（带字段偏移） |
| `GameAssembly.dll`（反汇编） | `disasm_va.py 0x1800021f0`（30 条） | 接口方法调用器，已定性 |
| `d:/4/Unity/资料/原版实拍/menu_topbar_0927/runtime_ui_dump_MainMenu_Warpforge.tsv` | 行 `:66-95`（顶栏全族） | `Background Mask` = `False/False` 等 |

---

## 顺手发现

1. 🔴 **`A1522②` 是【过期账】—— 这条已经修完了，账上那行的措辞与落地打架。**
   `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/TopBar.cs:243-262` 已明写「**这一块【不建】**」并把
   `Rect(..., "Planer Name Background", …)` **删掉**（注释日期 **2026-10-11**、依据 `A1521②`，四条独立判据：
   prefab `m_IsActive=0` · 原版运行期 dump 5 份全 False · **1920×1080 实拍逐像素** · 全包 PPtr 0 引用）；
   配套反向断言在 `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs:1392-1395`
   （`FindChild(prof, "Planer Name Background") == null`，鉴别式写在 `:1392`）。
   而 `d:/4/Unity/资料/任务_卡组卡牌战斗.md:217`（`A1522` 那一行）仍写「**⇒ 等裁：动名牌还是动 `InboxBtn`，没有独立判据**」
   —— **该行应核销 / 改写**（`A1522` 的 ①③④⑤ 仍开着，②该划掉）。这一条属铁律 6/5 的「两份说法打架」。
   ⚠️ 顺带：`A1522⑤`（卡片上下次序）与本条无关，别一起动。
2. **`Rating` 两支在 prefab 里 `m_IsActive` 全是 `true`** ⇒ 我们复刻这两支时**必须自己用一个布尔门控**，
   ⛔ 不能靠「出厂一支本来就关着」。门控的判据已给出（`IRankedEvent.IsLegendaryElo`）。
3. ⚠️ **原版 `ToggleLegendaryRankInfo` 与 `TogglePositionDisplay` 遍历的是【同一个数组】**
   （`legendaryRankDivisionInfoObjects` = `Position #` / `Position Value` 两颗，已按 PPtr 解出）：
   `ToggleLegendaryRankInfo(X)` 在传奇分支、`TogglePositionDisplay(position > 0)` 在非传奇分支。
   ⇒ 这是一处**两个方法动同一组对象**的写法（非传奇分支里 `position<=0` 会再关一次）。
   **报上来由调度台分流** —— 看着像冗余/可互相打架，但**我没有判据说它是缺陷**，复刻时照抄即可。
4. `Player Name`（TMP）是 `Player Profile` 的**直接子件**、**不在 `Background Mask` 里面** ⇒
   那块 `RectMask2D` 只罩名字**底图**，不罩名字**文字**（写复刻规格时别顺手把文字挂进去）。
5. 名字条底图 `40k_topmarquee_currency_display BW` 的 `m_Color = (0.39623, 0.19251, 0.30954, 1)`（暗紫）
   —— 与 `A1523③` 里「它是通用面板底、不是名字条专用板」互证（`GenericOptionsPanel.cs:118` 等 5 处）。
6. `InboxBtn` 的可点区**比它的矩形大**：`m_RaycastPadding = (-6.28, -23, -5.7, -23)` ⇒
   屏幕矩形 `425.30..480.30 × 15.50..55.50` 外扩成 **`419.02..486.00 × (−7.5)..78.5`**。
   （铁律「负 = 外扩」；做命中区/断言时别只量 55×40。）
7. `NavigationPanelController__Awake.c` 里：进主菜单时**先把整个面板 `SetActive(false)`**（`0x50` 存原 `anchoredPosition`），
   再按 liveop 建事件钮 —— 面板的**出场是滑入动画**（`Toggle(bool, bool)` + `hiddenPosition` + `animationTime`）。
   我们若做「面板恒显示」，那是**偏离**（记为观察，未判缺陷——本次没查它多久滑出来）。
