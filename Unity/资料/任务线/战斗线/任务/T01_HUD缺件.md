# T01 · 战斗 HUD 缺件 —— 按原版补齐

> **状态**：🔴 **未完成**（4 小件可动手 · 1 小件动不了 · 2 小件已判「不建」）
> **关联 A 编号**：`A2109`（主体）· `A1547` · `A1239` · `A1302` · `A1104` · `A1140`
> **判据全集** → `资料/普查产出_1015战斗/D1_A2109八条真缺_逐条判据.md`

---

## 一、要做什么

原版战斗 HUD 上有几件**我们完全没有**（或位置/几何不对）。做完之后：
**玩家能看到原版那些提示与面板**（静音开关、进战斗黑场过场、教程锚点），而不是「原版有、我们这边一片空」。

⚠️ **本件里有两小件已判「不建 = 与原版一致」**（见 §六 表），⛔ **别再去做**。

---

## 二、判据（逐条带出处）

**统一口径**：几何一律**从 `assets_full/bundle_scenes_scenes_battlearena2/` 现读**
（⚠️ ⛔ **别用 `battlearena1`** —— 它的 2D 全树是截断版、缺的正好在 HUD）。
**父链 `m_LocalScale` 逐跳乘**，⚠️ **但 `BattleHud/Canvas`（RT 2952）那一跳必须跳过**：它序列化是 `(0,0,0)`、由 Canvas 接管。

### 2·1 `BattleSettingsPanel/ChatToggle`（静音对手）
- **节点全路径**：`…/FrontCanvas/Safe area FrontCanvas/BattleSettingsPanel/**Volume Sliders**/ChatToggle`（⚠️ 父是 `Volume Sliders`，不是面板根）
- **几何**：`anchorMin=anchorMax=(0,1)` · `piv(0.5,0.5)` · `ap(350.675, −454.87)` · `sizeDelta(701.35, 54.9)`
- **子件**：`Background`(`40K_toggle_off`, ap(190,0), sd 180.8×56.8) → `Checkmark`(`Volume_button`, ap(−32.9,−0.8), sd 57.9×46.2) → `OffText 'OFF'` fs30；
  `Background On`(`40K_toggle_on`, 同 sd) → `Checkmark`(ap(34.1,−0.8)) → `OnText 'ON'` fs30；`Label 'Mute opponent'` fs42 align=1
- **状态机**：`BattleSettingsWindow.OnMuteChatChanged(bool)`：`+0xD0(isOn)=v` · **`+0xD8(muteChatToggleOn).SetActive(v)`**（⚠️ 打的是**ON 那半张图**，不是整行）· `+0xE0(voiceLinesController).+0x40(chatMuted)=v`；`Open()` 里按 `chatMuted` 回填三处
- **显隐**：场景初值 `True`；**全量反编译搜 `ChatToggle` 0 命中**、`BattleSettingsWindow` 的 25 个字段里没有它 ⇒ **没有任何赋值点单独开关它** ⇒ 可见性完全跟父面板
- **出处**：`D1` §2（RT 现读 + 运行时 dump `arena2:1087-1092` + `BattleSettingsWindow__OnMuteChatChanged.c`）
- ⚠️ **还差一条口径**：原版静音位在 `VoiceLinesController.chatMuted`，**我们没有那一位** ⇒ **先定「单机里静音存哪、对谁生效」**再动手。

### 2·2 `Canvas/Anim Anchors` 5 个锚点
- **节点全路径**：`BattleHud/**Canvas**/Anim Anchors` —— ⚠️ **不在 `Safe area FrontCanvas` 下**，是 `FrontCanvas` 的**兄弟**
- **几何**：父全 stretch；5 个锚一律 `aMin=aMax=(0.5,0.5)` · `piv(0.5,0.5)` · `sd(100,100)` · scale 1
  `Anchor: UI Center` ap(0,0) · `Anchor: CreateCard` ap(**885.563, 84.273**) · `Anchor: Cemetery` ap(**−1763.1, −10.765**) · `Anchor: Stratagem ShowCase` ap(529,83) · `Anchor: Next To Clock` ap(766,83)
- **谁读它**：`BattleHud.GetUIAnchor(id, isEnemy)` 线性扫 `BattleHud+0xB0` 那张表；**唯一调用方 = `AbilityLogic.GetPos`**，传的 id 是 `0xC(12)` / `0xD(13)` / `0x8C(140)` / `0x91(145)`
- ⚠️ **5 个都【没有 Image】** ⇒ 是**几何参考点、不是可见元素**（`Cemetery` 的 x=−1763.1 是**飞行终点**，别当排版错误）
- ⚠️ **运行时名字带前缀 `Anchor: `**（B2 记的是不带前缀的短名 ⇒ 按名查要连前缀）
- **出处**：`D1` §4
- ⚠️ **还差**：`anchorId → 锚点名` 的对应表（枚举 `BattleHud.UIAnchor` 的取值表**本地没有**）；以及「**我们的哪段演出要用它**」—— 原版那 4 个 id 走的是 `AbilityLogic` 的**技能/召唤起点**。

### 2·3 `FrontCanvas/LoadingMenu`（进战斗的黑场）
- **节点全路径**：`…/FrontCanvas/Safe area FrontCanvas/LoadingMenu`（父 = RT_3055，**不在 `AboveShader` 下**）→ 子 `Panel`
- **组件**（`MonoBehaviour_5186.json`，GO 923）：`LoadingImage=PPtr(5187)` · **`StartFaded=1`**
- **子 `Panel`**：全 stretch · `ap(−0.01, 2.85)` · `sizeDelta(**12006.4, 10394.9**)`（序列化；⚠️ 运行时 dump 是 **12006.4 × 10061.1**，`AspectRatioFitter` 运行期算过）· `Image` sprite None `col(0,0,0,1)`（纯黑）
- **行为（2 种）**：`LoadingMenu.LoadingFade(image, down, cb)`：
  `SetActive(true)` → `α = (down==0) ? 1 : 0` → `DOFade(α, 时长)` → 回调里 **`if (down==0) SetActive(false)`**
  ⇒ **`down=0`** = 补间到全黑、**结束后自己关掉**（**进战斗走这条**，触发点 = `BattleManager._StartBattleSequence`）；**`down=1`** = 补间到透明、保持 active
  · `Awake`：`if (StartFaded == 0) return;` 否则把它 `SetActive(true)` 且 A 设成 `DAT_1834b2bb8`（**该常量读不出**）
- **出处**：`D1` §5
- ⚠️ **还差**：`DOFade` 的**时长**（实参在寄存器里、`.c` 读不出）⇒ 只能用我们的节奏并**如实标 ➖ 我们挑的**。

### 2·4 墓地日志「行内链接 ⇒ 悬停弹出整套卡」
- ✅ **其实早已建**（`BattleLogPanel.Linkify()` 在写 `<b><link="…"><u>名</u></link></b>`，且 `ApplyEntries()` 真在用）
  ⇒ 🔴 **`C2` 记的「半件 / `FindLinkProbe` 恒失败」已过时**，⛔ 别再去重写已经有的东西。
- **原版行为**（`CemeteryManager.CheckCardLink` / `ClickCardLink`）：`TMP_TextUtilities.FindIntersectingLink` 命中 ⇒
  取 `GetLinkID()` → `Split(',')` → `Int32.Parse(list[1])` 当 **`CemeteryAction` 列表下标** → 看 `list[0]`：
  **悬停**版 ⇒ `DisplayCard(该动作缓存的两张卡, 0)`；**点击**版 ⇒ 取 `CardUniqueId` → `BattleManager.DisplayCard(card, 1, 0, 0)`
  · 未命中 ⇒ `SetActive(悬停卡, false)`
  · **标签格式**：`<b><link="1,<动作下标>"><u>名字</u></link></b>`
- **我们差的只有两半**：① 「悬停命中 ⇒ 弹卡」那一跳没接（`LinkKeyAt` → 弹 `CardView`）；② 链接 ID 用的是**英文卡名**（原版是 `1,<动作下标>`，**我们没有那张动作表**）
- **出处**：`D1` §7

### 2·5 其余几条（判据见 D1 §6 / 各自账）
`Alliance Panel` 两处评级（**今天动不了**：缺评级数据源 + 子件尺寸运行期算）· `A1547` `SetOffensiveButtonVisible` · `A1239` `HandLabel` 补框 · `A1302` `HudAbs/HudImage` 不做 `PreserveAspect` ⇒ 该类图全偏 · `A1104` 换牌两行文字写死字号（原版 `autoSizing(18~55)`）· `A1140` toast 钉绝对屏坐标、不走安全区。

---

## 三、我们现状

| 件 | 我们 |
|---|---|
| `ChatToggle` | **没建**（`Battle/SettingsPanel.cs:748` 自认） |
| `Anim Anchors` 5 | **一个都没建**（`grep` 只命中 `BattleDriver.cs:13511` 一条**注释**） |
| `LoadingMenu` | **没建**（`Battle/` 与 `Shell/` 全仓 grep **0 命中**） |
| 墓地日志链接 | ✅ **已建**（`Battle/BattleLogPanel.cs:832` `Linkify` · `:801` `ApplyEntries` · `:316` `RowLinkKey` · `:326` `LinkKeyAt`） |
| `Alliance Panel` 两处评级 | **没建**（`Shell/AlliancePanelWindow.cs` 文件头 ② 自认） |

---

## 四、怎么改（按顺序）

1. **`ChatToggle`** —— ⚠️ **先定口径**（§2·1 末尾那条），再照几何建；状态机三处一起（`isOn` / **只切 ON 那半张图** / `chatMuted`）。
2. **`LoadingMenu`** —— 建覆盖层 + `LoadingFade(down)` 两态；时长**用我们的**并标 `➖ 我们挑的`；接到进战斗那条路。
3. **墓地日志链接那一跳** —— `LinkKeyAt` 命中 ⇒ 弹 `CardView`（照原版「悬停弹、点也弹」两条）。链接 ID 格式**先不动**（没有动作表）。
4. **`Anim Anchors` 5 个** —— ⚠️ **先回答「我们哪段演出要用它」**，否则建出来是死件（原版那 4 个 id 走 `AbilityLogic` 的技能/召唤起点）。**答得上再建。**
5. `A1302` / `A1104` / `A1140` / `A1547` / `A1239` 各自单做（判据见各自账）。

⚠️ **动手前先读 `A2108`**（旧文档引的 `BattleDriver.cs` 行号**整体偏移**）⇒ **引行号前一律现核**。

---

## 五、做完了怎么自检

**按现行口径 = 只跑秒级类型检查**（`bash d:/4/Unity/工具/typecheck.sh`），⛔ 不跑 `BattleScene.Run`。
将来统一验收时：进游戏 → `Shell` / `Battle` → 看这几件**在不在、位置对不对**（`工具/wf_click.py` 真点）。

---

## 六、⛔ 本件里**不要做**的两条（已裁定「不建 = 与原版一致」）

| 件 | 为什么不做 |
|---|---|
| `BattleSettingsPanel/Match Skulls` 行 | 外层 `m_IsActive=false` + **全量反编译零赋值点** ⇒ **原版正式版恒不显示**（⚠️ B2/C2 写的「排位/骷髅模式才开」**没有判据**，⛔ 别照抄） |
| 设置窗 `Debug Buttons` 四颗 | `BattleSettingsWindow.Awake` 里**无条件 `SetActive(false)`**、且是**唯一写入点** ⇒ **原版从不显示**。⭐ 它**不是**顶层 `DebugButtons` 那一棵（那是另一件，也是 inactive），两条不打架 |

---

## 七、还差什么 / 判不准的

1. **`ChatToggle` 单机语义**（静音存哪、对谁生效）—— 判据在原版 `VoiceLinesController.chatMuted`，**我们没那一位**。
2. **`Anim Anchors` 的 `anchorId → 名字` 表** —— 枚举取值本地没有。
3. **`LoadingMenu` 的 `DOFade` 时长** —— 寄存器传参、读不出。
4. **两档 alpha 常量** `DAT_1834b2bb8` / `DAT_1834b3180` —— 读不出（提示条那件已改用序列化值，见 `T01` 归档记录）。
5. **`Alliance Panel` 两处评级的运行期尺寸**（子件序列化 0×0）—— 要真机 dump 一次。
