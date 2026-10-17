# W_B · `BaseEventPopup` 事件说明弹窗族（6 变体）+ 四窗 `Help` 接线

> 写手代理报告 · 2026-10-17 · 只读判据 = 原版 prefab + 全量反编译；本笔**未跑任何 Unity 自检**（铁律 12 现行口径「先复刻，别检查」），只跑了秒级类型检查。

---

## 1. 结论 + 改动文件清单

**结论：**
- 新建 `Shell/BaseEventPopup.cs`：**一个类 + 一个 `EventPopupVariant` 枚举（6 档）** = 原版 `BaseEventPopup` 那 6 个 prefab 变体（原版本身就是同一个类挂 6 个 prefab）。结构、几何、字号、对齐、显隐、窗口参数逐条照 prefab 实读。
- `LiveOpsEventWindow.BuildHelp` 由 `NotBuilt("…本地没有那份文案")` 改为 **`OpenHelp()` 真开窗**；并把「用哪份变体 / 标题词条键 / 正文词条键」开成三个虚口（缺省 = 本地拿不到 ⇒ **留空 + 出声**）。
- `Core/Loc.cs` **只追加一条** `Demo/Welcome/TapToContinue`（弹窗底部那颗 `Tap To continue` 的官方词条；不加就会在界面上真的印出键名）。
- 类型检查 **0 错 / 0 错**（运行时 + 编辑器两个程序集）。

**改动文件：**

| 文件 | 位置 | 改动 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/BaseEventPopup.cs` | **新建**（约 420 行）+ `.cs.meta` | 6 变体实现 |
| `Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsEventWindow.cs` | `:944-1001` | `BuildHelp` 接线 + `OpenHelp()` + `HelpVariant` / `EventHelpTitleTerm` / `EventHelpDescriptionTerm` / `LastHelpPopup` |
| `Unity/MyGame/Assets/CardPresentation/Core/Loc.cs` | `:2633-2646`（追加在 `Table` 末尾） | 新增 `Demo/Welcome/TapToContinue`（en `Click to continue` / zh `点击以继续`，取自官方本地化表） |

**关键行号（`Shell/BaseEventPopup.cs`）：**
`:55` 枚举 · `:76` 类 · `:216` `Create`（窗参在这里）· `:239` `Open` · `:246` `Build` · `:300` `BuildTexts` · `:362` `ResolveTerm`（「先试官方文案、没有就留空 + 出声」）· `:402` `Tick`。

---

## 2. 六个变体：原版 prefab → 我们怎么落地

判据命令（可复跑）：

```
python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<根名>" --depth 8 --md
```
根的 `BaseEventPopup` 组件字段是从各自 `GameObject/<根名>.json` → `m_Component` → `MonoBehaviour` 直接读的
（例：`Base Event Popup` 那颗 = `MonoBehaviour_-4960665813637071397.json`）。

### 2.1 共有结构（六个变体逐件相同，兄弟序 = 原版）

| # | 节点 | 绝对矩形（1920×1080） | 我们怎么画 | 判据 |
|---|---|---|---|---|
| 1 | `Menu Dark Background` | -1327.30,-746.18 → 3247.30,1826.18 | `CardArt.Solid()` 纯色，alpha 见下表；`MenuDraw.ShadeHit(..., "BackgroundCloseButton")` 点它关窗 | prefab `Image.m_Color`；`BackgroundCloseButton` 组件（`App` 里 `onClick` 空、运行期挂 `Close`） |
| 2 | `Generic Window Red Background Big` | 442.63,146.33 → 1495.37,879.67 | `UI_Deck_Information_Back` 九宫 `(L42,B363,R655,T81)` · 1100×701 · `Sliced` | 该 Sprite JSON + 我们 `Resources/Art/ui_deck/` 里同名 PNG 也是 1100×701 |
| 3 | `Event image` | 见下表 | 建节点；**本地没有事件图 ⇒ 贴图空着**（见 §4.1） | 六个 prefab 的 `Image.m_Sprite` 只有 Expiration 烤了一张（运行期一样被覆盖） |
| 4 | `Texts` | 1005,190 → 1455,770（450×580） | 空容器；三颗子件见下表 | prefab 树 |
| 4a | `Texts/TitleText` | 见下表 | `MenuDraw.Text` fs**34** · auto[20~34] · 基准 **47.73** · 折行 1 · 白 | TMP 字段逐颗实读 |
| 4b | `Texts/DescriptionText` | 见下表 | `MenuDraw.TextBox` fs**28** · auto[18~28] · 基准 **22.5** · 折行 1 · 白 | 同上 |
| 4c | `Texts/Tap To continue` | 见下表 | `MenuDraw.TextBox` fs**30** · auto[18~30] · 基准 **60** · 色 `(0.627,0.616,0.631)` · `BlinkGraphic`(1.0 / 0.5) · 词条 `Demo/Welcome/TapToContinue` | TMP + `Localize.mTerm` + `BlinkGraphic` 字段 |
| 5 | `Collider` | 0,0 → 1920,1080 | 整屏透明命中区（`MenuDraw.Hit`，档 `QHit`）⇒ **点任意处关窗** | `Image`(a=0) + `EverguildButton`；`BaseEventPopup__Open.c` 里 `trigger.onClick += Close` |

> `Menu Dark Background` 的 `BackgroundCloseButton` 也在原版里 ⇒ 两个「点外面关窗」入口我们都建了；
> 整屏 `Collider` 是**最后一个兄弟** ⇒ 在实际射线顺序里它先吃掉所有点击（原版同样如此）。

### 2.2 六个变体的差异（逐条来自 prefab 字段）

| 原版 prefab | 我们的 `EventPopupVariant` | `Event image` 矩形 / 染色 | `TitleText` | `DescriptionText` | `Tap` | 其它 |
|---|---|---|---|---|---|---|
| `Base Event Popup` | `BaseEvent` | 281.62,34.12→1140.38,892.88 · `(0.802)³,1` | 1005,190→1455,295 · **Bottom** | 1025,308.58→1435,733.58 · **Center** | 1005,730.35→1455,809.65 | 压暗 a=**0.7725**；`titleTag=0/descTag=1/imgTag=2` |
| `Event Help Popup Variant` | `EventHelp` | 同上 | 同上 | 同上 | 同上 | `titleTag=0/descTag=**12**/imgTag=2`（与上一份只差这一处 + `trigger` PPtr） |
| `Event Help Popup Small Image Variant` | `EventHelpSmallImage` | 470.67,236.67→1037.33,803.33（566.67×566.66）· 同上 | 同上 | 同上 | 同上 | `titleTag=0/descTag=12/imgTag=2` |
| `Liveop Event Help Variant` | `LiveopEventHelp` | 430.89,165.49→1103.11,837.72（672.23²）· `(0.868)³,1` | 924.14,190→1455,240.31（530.86×50.31）· **Capline** | 1005,244.46→**1467.28**,770（462.28×525.54，**溢出容器右边 12.28**）· **Left**/**Middle** | 1005,770→1455,824.75（54.75 高） | `titleTag=**5**/descTag=12/imgTag=2` ⇒ **默认给 `Help` 用** |
| `Full Screen Event Announcement Variant` | `FullScreenAnnouncement` | 0,0→1920,1080 · 白 | （Texts 关） | （Texts 关） | （Texts 关） | 压暗 a=**0.9059**；红底 **出厂 `m_IsActive=false`**；`Texts` **出厂 false**；`extraScale=**1.0**`；`imgTag=**7**` |
| `Base Expiration Popup` | `BaseExpiration` | 与 Base 同 | 与 Base 同 | 与 Base 同 | 与 Base 同 | `titleTag=**10**/descTag=**11**/imgTag=2`；prefab 烤着 `UI_Deck_Warlord_Uriel Ventris`（**不照它画**，运行期会被覆盖） |

**窗口参数**（六个 prefab 的 `BaseEventPopup` 组件逐字段实读）：
`type = 1`（Popup）· `windowsPlacement = 15`（Popup）· `closeOnESC = 1` · `updateNavPanel = 0` ·
`extraScaleSmallScreen = 1.2`（**全屏宣告 = 1.0**）。
我们在 `Create()`（`BaseEventPopup.cs:216`）里逐条写死，并在注释里标了「实证」。

**队列梯子**（`BaseEventPopup.cs:80-88`）：`QShade 3124 < QPanel 3125 < QArt 3126 < QText 3127 < QHit 3129`。
理由：必须**严格高于**被它盖住的活动窗整族（`LiveOpsEventWindow` 3104–3116），且**低于** `SearchingMatchPopup`(3130) / `PromptPopup`(3140)。

### 2.3 运行期行为（`BaseEventPopup__Open.c` 逐句 → 我们的实现）

| 原版 | 我们 |
|---|---|
| `GameWindow.Open()` | `Open()` → `Build()` |
| `image != null` ⇒ `LoadAsset(liveOps, imgTag)` → `set_sprite` | `EventImage`（public 口，缺省 null）有图才画；否则节点留着、贴图空 + 出声（`NoteNoEventImage`） |
| `titleText != null` ⇒ `GetLabel(liveOps, tag)` → `set_text` | `ResolveTerm`：先 `Loc.HasBulk`（`Event_Description/*` 在 `official_names.json` 的 bulk 表）再 `Loc.HasEntry`；**都没有 ⇒ `""` + 出声** |
| `descriptionText` 同上 | 同上 |
| `trigger.onClick += Close` | 整屏 `Collider` 的 `MenuDraw.Hit(..., () => Close())` |
| `HasAsset(liveOps, 8 OpenSound)` 才播 | 本地没有事件 ⇒ 不播（prefab 的 `openSound` 本来就是 null） |
| `Close()`：`Unload` 资产 + `GameWindow.Close()` | 没有要 unload 的资产 ⇒ 只走基类 `Close()` |

> 🔴 **「留空」不是我们的取舍，是原版的行为**：`LiveOpsAssetUtility.GetLabel` 在
> `GetEventLabel` 返回 0（该 tag 没有 `EventLabel`）时回的是**静态空串**，随后**无条件** `set_text` ——
> 也就是说原版**不会**保留 prefab 里烤的样例文案（`Welcome to Warpforge!` / 那段 Skirmish 说明）。
> 这条读数的出处：`Everguild.LiveOps.LiveOpsAssetUtility__GetLabel.c` 第一个分支 +
> `BaseEventPopup__Open.c` 的两处 `set_text` 调用。

---

## 3. `Help` 钮接线（调用链）

**原版（逐句实读）：**

```
SkirmishEventWindow__HelpButtonClick.c
RankedEventWindowV2__HelpButtonClick.c      （两份逐行同构）
  → LiveOpsAssetUtility.HasComponent(liveOps, 0x1e /*30 = EventComponentReferenceType.HelpPopup*/)
  → LiveOpsAssetUtility.GetComponentReference(liveOps, 30)        // = 一颗 GameWindow（某个 BaseEventPopup 变体）
  → 虚表 0x248（合法性判定）
  → WindowsManager.OpenWindow(component, liveOps)
```
枚举值出处：`Everguild/LiveOps/EventComponentReferenceType.cs`（`HelpPopup = 30`）。

**我们的链路：**

```
Shell/LiveOpsEventWindow.cs:964   MenuDraw.Hit(root, "HelpHit", r, QHit, () => OpenHelp())
Shell/LiveOpsEventWindow.cs:983   OpenHelp()
  → BaseEventPopup.Create(Manager, HelpVariant, EventHelpTitleTerm, EventHelpDescriptionTerm, name)
       Shell/BaseEventPopup.cs:216
  → Manager.OpenWindow(w)
Shell/LiveOpsEventWindow.cs:1001  LastHelpPopup（自检口）
```

**虚口（子类可覆写）：**

| 成员 | 缺省 | 含义 |
|---|---|---|
| `LiveOpsEventWindow.HelpVariant`（`:971`） | `EventPopupVariant.LiveopEventHelp` | 用哪份变体。原版由事件数据配；本地没有 ⇒ 取 tag 为 `WelcomeTitle/Help/ForegroundImage` 的那份 |
| `EventHelpTitleTerm`（`:975`） | `null` | 标题官方词条键。null ⇒ 留空 + 出声 |
| `EventHelpDescriptionTerm`（`:980`） | `null` | 正文官方词条键。null ⇒ 留空 + 出声 |

**「先试官方文案、没有就留空并出声」的落点** = `BaseEventPopup.ResolveTerm`（`:362`）：
先 `Loc.HasBulk(term)`（bulk = `Resources/official_names.json`，`Event_Description/*` 111 条在那一份），
再 `Loc.HasEntry(term)`（`Loc.Table`）；两处都没有 ⇒ **返回空串 + `Debug.LogWarning`**（带键名 / 节点名）。
键为 null 时那条警告带的是「哪个事件 / 哪个节点」+「liveops 事件数据在远端 CCD，本地没有」。

---

## 4. 没做的 / 没查清的 / 顺手发现

### 4.1 事件 → 文案键、事件图：**本地确实没有**（不是没找）

- 原版取法（反编译）：`GetEventLabel(liveOps, tag)` 在 `ILiveOpsEventData` 的 **`EventLabel` 列表**里按 tag 找；
  `EventLabel.RawValue` 才是词条键、`Value` 是已本地化的串。**这个列表是事件 JSON 的一部分**。
- 本地搜索证据：
  - 84 个 bundle 全列过（`d:/2/新解包资源/assets_full/bundle_*/`）：与 liveops 有关的只有
    `bundle_liveopsicons_assets_all`（11 张 sprite）与 `bundle_liveopsmenuimages_assets_all`（39 张 sprite）
    —— **全是图，没有事件 JSON / SO**；
  - 全量反编译里 `LiveOpsEventData` 的 `eventId` / `drawData` 由远端下发；`GetEventLabel` 只读内存里的列表；
  - 按要求试过的词：`EventLabel` · `eventId` · `HelpPopup` · `HasComponent(liveOps, 30)` · `Event_Description`。
- ⇒ 标题 / 正文**留空 + 出声**；`Event image` **留空 + 出声**（节点照建，与「`LoadAsset` 取不到 ⇒ `set_sprite(空)`」画面一致）。
- ⛔ 没拿 `Event_Description/Event/EventModeDesc` 之类的通用键顶（那会让玩家看到错的活动说明且没有任何报错）。

### 4.2 每扇事件窗该用哪份 prefab 变体：**判据在远端**

原版由事件的 `EventComponents.HelpPopup`（组件类别 30）指向某个 prefab。本地没有事件数据 ⇒
四扇窗统一缺省 `Liveop Event Help Variant`（理由：它的三颗 tag 是 `WelcomeTitle / Help / ForegroundImage`，
正是「活动帮助」那一套）。子类可覆写 `HelpVariant` 钉死。**「哪扇窗对应哪份」没查到**，如实记。

### 4.3 开窗音 / 关窗音

六个 prefab 的 `openSound` / `closeSound` 都是 `null`、`useDefaultCloseSoundIfNull = 1`。
原版 `Close()` 会走 `GameWindow.Close` 的默认关窗音；**我们 `GameWindow` 的关窗音整条链本来就不存在**
（老账，不在本笔范围）⇒ 本窗不播。原版 `Open()` 只在该事件有 `OpenSound`(8) 资产时才播，本地没有。

### 4.4 五个变体今天没有生产入口

`BaseEvent` / `EventHelp` / `EventHelpSmallImage` / `FullScreenAnnouncement` / `BaseExpiration`
对应原版的 `Popup(12)` / `HelpPopup(30)` / `TriggerPopup(20)` / `ExpirePopup(21)` 等事件组件槽；
本地没有事件数据 ⇒ 没有入口。**实现齐了，只缺「谁来开」**（等 liveops 数据那一轮）。
今天真正被接上的只有 `LiveopEventHelp`（经 `Help Button`）。

### 4.5 顺手发现（⛔ 未自行修改，请主对话分流）

1. **`资料/普查产出_1017/UI/U3_弹窗与小界面族.md:120` 与 `:17` 说「四扇事件窗都在，但 `Help Button` 只打日志」—— 实读 prefab 只有 2 扇有 `Help Button`。**
   判据：`menu_dump --depth 2 --no-sprite` + `grep "Help Button"`：
   `SkirmishModeEventWindow` 有（1804.47,23.73→1893,112.27 · 视觉 65.13² · `m_LocalScale 0.7357`）·
   `RankedEventWindowV2` 有（逐值相同）· `EnergySinglePlayerOnlyEventWindow` **零命中** ·
   `Ranked Boost Reward Event Window` **零命中**。我们代码侧也正好只有 `SkirmishEventWindow` / `RankedEventWindow`
   继承 `LiveOpsEventWindow` ⇒ **行为对，是文档口径偏宽**。建议就地把「四扇」改成「两扇（Skirmish / Ranked）」。
2. **`LiveOpsEventWindow.NotBuilt`（`:1307`）现在没有调用点**（唯一调用点就是被本笔换掉的 `BuildHelp`）。
   留着无害（`protected`），但可以清掉。
3. **`Event_Description/*` 111 条在 bulk 表里，`Loc.T` 查不到它** —— `Loc.T` 只查 `Table`；
   要取 bulk 必须走 `Loc.BulkOr` / `Loc.HasBulk`。本笔的 `ResolveTerm` 两处都试了，
   但**将来谁要事件文案，别写成 `Loc.T("Event_Description/…")`**（会返回键名并出声）。
4. **`TitleText` / `DescriptionText` 那颗 `Localize` 组件在 prefab 里 `m_Enabled = 0`**（标题与正文都是）。
   这解释了为什么它们的 `mTerm` 是 `MainMenu/Welcome/*` 的占位值：**文字只可能来自运行期 `set_text`**。
   顺带的判定方法：`menu_dump --md` 那行的 `⛔MULTI-COMP(3组件·1禁用)` 就是它，⛔ 别当「TMP 自己会按这个键显示」。
5. **`Base Expiration Popup` 的 `Event image` 在 prefab 里烤着 `UI_Deck_Warlord_Uriel Ventris`（2048²，`m_PreserveAspect=0`）**，
   但 `Open.c` 无条件 `set_sprite` 覆盖 ⇒ 不能照 prefab 画死（本笔按「空」处理）。

---

## 5. typecheck 结果

```
TMPDIR=/tmp/wf_b_popup bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（按铁律 12 现行口径，**没有跑任何 `*.Run` 自检**、没有进游戏、没有跑 Unity 批处理。）
