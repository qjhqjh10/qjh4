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
- 🔴 **2026-10-16 判据补齐（`T01b` 普查实读）—— 口径定了，但结论是「**能建，作用面为空**」**：
  · `chatMuted`（`VoiceLinesController` `+0x40`）的读点**只有 3 个、全在该类内**；`CanChat` 的**唯一调用方** = `DisplayWarlordChatMessage`
    （实据 = 它那行日志字面量 `"DisplayWarlordChatMessage failed because chatMuted"`）。
  · ⇒ 它挡的是**一条链**：对手在 `ChatPopup` 选的那 6 句 → `PUNReceiveChatMessage`（`isPlayer=0`）→ 被拦
    ⇒ **气泡 + 语音 + 「对手在聊天」提示三样一起没**。
  · **不挡**（逐条有判据）：自己的发言 · 单位语音条（走 `CanUseVOs`）· 无线电提示（`ignoreUiState=1`，连 `CanChat` 都进不去）· 脚本 bot（三处全是 `ignoreUiState=1`）。
  · **值只在本局内存**（`[SerializeField] bool @0x40`，场景序列化 0，`Initialize` 无条件写 0；`Open()` 回填读同一字段）—— **不是 PlayerPrefs、不是服务器**。
  · 🔴 **我们该关掉哪几样 = 什么都不该关**：`CardPresentation/Net/*.cs`（8 个文件）里 `chat` **0 命中** ⇒ **我们没有对手聊天这条链**；
    我们的 `_unitChat.Speak(1,…)` 只用于单位语音条 / 认输 / 开场白 —— **这三样原版都不受静音影响**。
  ⇒ 动手方式：照几何建 + 三处状态机 + 一个 per-battle bool（开设置窗时回填），**并如实声明「点它没有可见效果」**；
    ⛔ 别把 `_unitChat` 的敌方半绑上去（那会做出原版没有的行为）；将来做联机对手聊天时**必须**进这个开关。

### 2·2 `Canvas/Anim Anchors` 5 个锚点
- **节点全路径**：`BattleHud/**Canvas**/Anim Anchors` —— ⚠️ **不在 `Safe area FrontCanvas` 下**，是 `FrontCanvas` 的**兄弟**
- **几何**：父全 stretch；5 个锚一律 `aMin=aMax=(0.5,0.5)` · `piv(0.5,0.5)` · `sd(100,100)` · scale 1
  `Anchor: UI Center` ap(0,0) · `Anchor: CreateCard` ap(**885.563, 84.273**) · `Anchor: Cemetery` ap(**−1763.1, −10.765**) · `Anchor: Stratagem ShowCase` ap(529,83) · `Anchor: Next To Clock` ap(766,83)
- **谁读它**：`BattleHud.GetUIAnchor(id, isEnemy)` 线性扫 `BattleHud+0xB0` 那张表；**唯一调用方 = `AbilityLogic.GetPos`**，传的 id 是 `0xC(12)` / `0xD(13)` / `0x8C(140)` / `0x91(145)`
- ⚠️ **5 个都【没有 Image】** ⇒ 是**几何参考点、不是可见元素**（`Cemetery` 的 x=−1763.1 是**飞行终点**，别当排版错误）
- ⚠️ **运行时名字带前缀 `Anchor: `**（B2 记的是不带前缀的短名 ⇒ 按名查要连前缀）
- **出处**：`D1` §4
- 🔴 **2026-10-16 判据补齐（`T01b` 普查实读）—— 原文两条都要订正**：
  · 枚举**不叫 `BattleHud.UIAnchor`**（本地根本没有这个类型），真名 = **`PositionOption`**（`dump.cs:25161`），嵌套类叫 `UIAnimAnchors`；
  · 表 = `BattleHud.uiAnimAnchors`（`battlearena2` GO 230 的 `MonoBehaviour_5150.json`；arena1 `MonoBehaviour_4934.json` 逐行同构），**7 行**：
    `140 createCardPos → Anchor: CreateCard` · `7 cemetery → **CemeteryLogPanel**`（⚠️ **不是** `Anchor: Cemetery`）·
    `6 boardCenterUI → Anchor: UI Center` · `12 friendlyMana → PlayerMana` · `13 enemyMana → EnemyMana` ·
    `145 stratagemGeneralShowCase → Anchor: Stratagem ShowCase` · `11 nextToClock → Anchor: Next To Clock`。
  · 🔴 **`Anchor: Cemetery` 原版也没人读**（扫 arena1+arena2 全包只命中它自己的 GO；反编译里没有任何按名字取锚的代码）
    ⇒ **5 个锚里 4 个被读、1 个是死件** —— ⛔ 别把 5 个并列写。
  · ⚠️ 「唯一调用方只传 4 个 id」也不全：`AbilityLogic.GetPos` 还把 **6/7/8/10/11** 送进 `GetUIAnchor`，
    其中 **8/10 不在表里** ⇒ 原版每次 `LogError` 落回 BattleHud 中心。
- 🔴 **结论：单独建这 5 个空 RT = 死件**（我们全仓**没有 `PositionOption` 解析器**，落点走 `SlotPosition` / `HudAnchor01`；
  我们 `animinfo_lookup.json` 里那 418 条 `sp`/`ep` **就是** PositionOption，但没人消费；且实测我们这些 id **落在 5 个锚上的 = 0 个**）。
  ⇒ **要与 `PositionOption` 解析器一起做**（7 行表数据现成），⛔ 别只建空件凑数。
- 🔴🔴 **2026-10-16（本会话实测）—— 这一件【改判给 `T05`】**（不是不做，是**归属挪了**）：
  · **量出来的事实**：我们全仓的 `数据/游戏数据/animinfo_lookup.json` 那 **418 条** `AnimInfo` 里
    `startPosOption`/`endPosOption` 的实际取值分布是 ——
    **`sp`**：`0(self)` 266 · `2(abilityTarget)` 84 · `3(friendlyHero)` 32 · `5(boardCenter)` 11 ·
    `50(friendlyDeck)` 7 · `4(enemyHero)` 6 · `19(enemyHand)` 4 · **`12(friendlyMana)` 3** · `14` 2 · **`13(enemyMana)` 1** · `15` 1 · `18` 1；
    **`ep`**：`2` 237 · `0` 116 · `3` 28 · `5` 11 · `4` 7 · `50` 7 · `19` 4 · `12` 3 · `14` 2 · `13` 1 · `15` 1 · `18` 1。
  · ⇒ **`Anim Anchors` 那 5 个锚点（id `6`/`7`/`11`/`140`/`145`）在我们的数据里出现 `0` 次**；
    连整张 7 行表里被用到的也只有 **12/13 两个**（而它们指的是 `PlayerMana` / `EnemyMana` 两个 **HUD 节点**，
    不是那 5 个空锚）。⇒ **今天把它们建出来，消费者仍然只有「没有任何东西」**。
  · ⇒ **解析器的真正消费者在【动画播放那一刻】**：`self` / `abilityTarget` 这两档要**当时的卡与目标**做参数
    （`ep=2` 那 237 条就是「飞到目标身上」），而 `friendlyHero` / `boardCenter` / `nextFriendlyMinionSpot` /
    `friendlyHand` / `friendlyDeck` 要**棋盘与手牌的世界坐标口**（我们都有：`BoardWorld` / `HandRow01` / 牌堆那两块底板）。
    **那正是 `T05`（逐卡演出与特效接线）的活** —— 在 `T01` 里建 = 造一件**今天没人用的基础设施**。
  · ✅ **裁定**：**解析器 + 5 个锚点整体挪进 `T05`**（判据、7 行表、id 分布全部随行带走）；
    `T01` 这一条**只留指针**，⛔ 别在两边各留一份（铁律 6）。

### 2·3 `FrontCanvas/LoadingMenu`（进战斗的黑场）
- **节点全路径**：`…/FrontCanvas/Safe area FrontCanvas/LoadingMenu`（父 = RT_3055，**不在 `AboveShader` 下**）→ 子 `Panel`
- **组件**（`MonoBehaviour_5186.json`，GO 923）：`LoadingImage=PPtr(5187)` · **`StartFaded=1`**
- **子 `Panel`**：全 stretch · `ap(−0.01, 2.85)` · `sizeDelta(**12006.4, 10394.9**)`（序列化；⚠️ 运行时 dump 是 **12006.4 × 10061.1**，`AspectRatioFitter` 运行期算过）· `Image` sprite None `col(0,0,0,1)`（纯黑）
- **行为（2 种）**：`LoadingMenu.LoadingFade(image, down, cb)`：
  `SetActive(true)` → `α = (down==0) ? 1 : 0` → `DOFade(α, 时长)` → 回调里 **`if (down==0) SetActive(false)`**
  ⇒ **`down=0`** = 补间到全黑、**结束后自己关掉**（**进战斗走这条**，触发点 = `BattleManager._StartBattleSequence`）；**`down=1`** = 补间到透明、保持 active
  · `Awake`：`if (StartFaded == 0) return;` 否则把它 `SetActive(true)` 且 A 设成 `DAT_1834b2bb8`（**该常量读不出**）
- **出处**：`D1` §5
- 🔴 **2026-10-16 判据补齐（`T01b` 普查实读）+ 【已建】** —— 原文那两条**都要订正**：
  · 参数名是 **`show`**（`dump.cs:109207 LoadingFade(bool show, Action callback)`），**不是 `down`**；
  · `LoadingFade` 里：`SetActive(true)` → **立刻** α = `show ? 0 : 1` → `DOFade(show ? 1 : 0, 时长)`
    （**目标与刚才那个值相反**）→ 回调 `if (show) return; else SetActive(false)`；
  · ⇒ **进战斗（`show=false`）= 立刻全黑 → 补间到【透明】 → 自己关掉**。
    ⚠️ 原文「补间到全黑、结束后自己关掉」**方向反了** —— 照那个做出来是**黑屏卡住**。
  · **时长 = `1.25 s`**（实读：`LoadingFade` 里 `movss xmm2,[rip+…]` → 常量 `0x1834B2EC0`；不是寄存器里读不出）。
  · `DAT_1834b2bb8` = **`1.0f`**（`Awake` 那一档的 α）· `DAT_1834b3180` = **`0.6f`**（**读到的**，不是推断）。
  · 整段 = **进战斗先黑、1.25 秒亮起来**；触发点 = 原版 `BattleManager._StartBattleSequence`。
- ✅ **我们已建**（`BattleDriver.BuildLoadingCover` + `TickLoadingFade`，走 `AdvanceTimeline` 那个泵）：
  全屏 `CardArt.Solid()` 纯黑、**显式给渲染队列**（`BattleLogPanel.OverlayQ + 100` —— ⛔ 光给 z 不行，
  铺满屏的 quad 四角比屏幕中央那些 HUD 件**离透视相机更远**，本工程在卡组编辑踩过这一条）。
  ⚠️ **批处理/编辑器非播放态直接落到终态**（那里没有帧循环 ⇒ 淡出永不发生，留着会把整屏糊黑、
  把 `BattleScene.Run` 读回的像素全变黑）—— ⛔ **不是「不建」**，建了才量得到几何。
  断言 4 条（在 `BattleScene.cs`，**专钉方向**）：已收掉 / 起点 α=1 / 半程 α≈0.5 / 1.25 s 后 α=0 且关掉。

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

> 🔴 **2026-10-16 就地清理（铁律 6）：本节原来 5 条里有 4 条已经查清了，留着只会误导下一个会话** ——
> ①「`ChatToggle` 单机语义」②「`Anim Anchors` 的 `anchorId → 名字` 表」③「`LoadingMenu` 的 `DOFade` 时长」
> ④「两档 alpha 常量读不出」**全部已在 §2·1 / §2·2 / §2·3 的 🔴 2026-10-16 段里结案**
> （时长 = 1.25 s、两个常量 = 1.0f / 0.6f、枚举真名 = `PositionOption`、`ChatToggle` 口径 = 能建但作用面为空）。
> ⛔ 别按这一节去重查。

1. **`Alliance Panel` 两处评级的运行期尺寸**（子件序列化 0×0）—— 要真机 dump 一次。**未做**。
2. 🔴 **`A1547` —— 2026-10-16 查清：原账那句「我们一个字没实现」【不成立】（已就地订正）**，但**换出了两条新账**（见下）。
   · **查清了什么**：`BattleHud + 0x78`（`dump.cs:27213`）= **`offensiveCardTurnCardsHolder`**（`GameObject`，
     `+0x80` = `offensiveCardTurnCardsObjs` 三颗）；实读 `battlearena2` 的 `MonoBehaviour_5150.json`：
     holder 指向 GO **`CardsPlayedInTurnHolder`**，三颗子件 = **`CardsPlayedInTurn1/2/3`**
     （每个 **20×20**、间距 **9**、底对齐；holder 105.06×28.18）。
   · **可见性判据**（`BattleHud__SetOffensiveButtonVisible.c` 亲读）：
     `SetActive(+0x78, HasPlayedThirdCardInTurn(cardScript))` —— 而
     **`HasPlayedThirdCardInTurn` = 「这张卡带 `AbilityTrigger.PlayedThirdCardInTurn`(=15)」**
     （`dump.cs:20241` 的 `public const AbilityTrigger PlayedThirdCardInTurn = 15;` + 方法体里那句
     `if (*(int *)(lVar3 + 0x10) == 0xf) return 1;`）—— ⛔ **不是「玩家这回合打过三张牌」**（名字骗人）。
   · ✅ **我们这边早就建了**：`_playedPips[3]`（`40k_general_bt_yellow`、20×20、间距 9、
     `PlayedPipX0/Y0` 那对常量 = 原版那三颗的绝对位置，逐位吻合）+ 出几张亮几枚（`_cardsPlayedThisTurn`）。
     ⇒ 🔴 **原账「缺件」= 过期记录**（铁律 5）。
   · ✅ **换出来的两条「新账」—— 2026-10-16 当天又查了一步，两条都落到【铁律 11 例外②：判据在远端】**
     （**不是「不做」，是「本地拿不到判据」**，⛔ 别当成我们漏做）：
     · **触发时机查清了**：`RawCardScript__OnCardPlayedWithTarget.c:105-112`（亲读）——
       `if (BattleManager.CardsPlayedInTurn(bm) == 3) TriggerAbilities(this, 0xf, …)`
       ⇒ **打完本回合第 3 张牌时，在【刚打出的那张牌】上触发 `PlayedThirdCardInTurn` 能力**。
       同一支还无条件触发 `0x14`(=20)、有目标时触发 `10` —— 那两条是另外的账，**不在本件**。
     · 🔴 **判据为什么是空的**：这个能力**长在进攻卡的卡定义上**，而**进攻卡的卡引用在远端 CCD**
       （本仓早就记着 `cardName 0/39 解不出`，见 `Core/OffensiveCards.cs` 文件头）；
       现读 `数据/游戏数据/offensive_cards.json`：`trigger` / `ability` / `ThirdCard` **全部 0 命中**
       ⇒ **本地既没有「哪张进攻卡带这个 trigger」、也没有它的效果正文** ⇒ 归 `§29·d 等远端`。
     · ⚠️ 因此那条 HUD 闸（「选定进攻卡带 trigger 15 时才显示 holder」）**今天做不到** ——
       它要的正是上面那格数据。⛔ **别用「出过牌就显示」顶上并声称是原版口径**：我们现在那条
       （`i < _cardsPlayedThisTurn`）是**等价物**，已在代码注释里如实标着。
3. **`A1239`（`HandLabel` 补框 & 接四格）** —— ✅ **2026-10-16 已做完，「单位桥」查实**：
   判据 = 主对话现读原版 `HandArea/CardsInHandText`（两个实例逐字段相同）—— RT `sd (2.34, 0.64)` +
   自身 `scale 0.925926`，**父链 `HandArea` 是纯 `Transform`、`scale 108`** ⇒ **1 局域单位 = 100 px**；
   TMP `m_fontSize 3.0` · base 36 · `min 0.5` / `max 3.0` · auto 1 ⇒ **框与三档一律 ×100**
   = 框 **234×64 px** · auto **50~300 px**（⚠️ 不乘就得到 3 px 的字 —— 原文那条担心**成立**；
   ⚠️ base 36 在原量纲里已超上限 ⇒ 按夹过之后的 **300** 传）。
   ⚠️ **位置那半没动**（原版那颗 `activeInHierarchy=False`、祖先链算不出绝对坐标 ⇒ 位置本就是我们挑的）。
   ⛔ 原文那句「两条候选读法互相打架」**已裁** —— 正解是 **×100**（判据 = 父链那两级 scale 的乘积）。
   🔴 **2026-10-23 更正（铁律 5 · γ 专项）**：上面「**auto 50~300 px**」**是错的** —— 这颗 TMP 是**世界档 3D TMP**（原版 `MonoBehaviour_3712/3717.json` 的 `m_isOrthographic = 0`），字号受 TMP `orthographicMultiplier 0.1` 影响 ⇒ **字号四格应 ×10 = 5~30 px**，不是 ×100；**框 234×64 px 不受影响（仍然对）**。错因：只乘了父链 `scale 108 × 0.925926`，**没看 TMP 的 `orthographicMultiplier`**。实测：按 ×100 传 min=50 px 时 Noto 行盒 `50 × 1.516 = 75.80 px` ⇒ 永远缩不进 64 px 的框（与 γ 的 75.8061 逐位吻合）。判据 → `普查产出_1023b/已知γ_手牌数框高_执行报告.md`。
4. **`A1302` 同族**：逐调用点审计**已做完**（见 §八），审计出的 4 条**已修**；审计里另标了两处
   「**没核**」的边界：① 牌堆卡背那两张（`:13893/13895`）只核到「初值被 `FitDeckPile` 覆写」，
   两段式的**终值**没逐张复核 ② **位置类**偏差（`Energy Accumulation` 横向 −80.2/−81.3、
   `ManaHolder` 的 `ap(1.8,−0.5)`）**本件一个字没核** ⇒ 那是别的账（`A2102` 那一族）。

---

## 八、✅ 2026-10-16（第四轮）本轮做完的（**核销记录**）

### 8·1 `ChatToggle`「静音对手」—— **已建**

`SettingsPanel.BuildMuteChatRow` + `RefreshMuteChat` / `SetMuteChat` / `ToggleMuteChatFromPanel` / `HitMuteChat`
（接线在 `BattleDriver.SettingsClickAt`，与 Resign / Difficulty / AutoZoom / Close 同一条「抬起」链）。
判据（几何逐值、状态机三处、词条三条）见 §2·1 那两段 2026-10-16 的注释。
**如实声明**：点了**没有可见效果**（它挡的是对手聊天那条链，我们还没有）。
⚠️ 连带把**我们自己加的**「对手难度」那一行从 `y=−220` 挪到 **−155**（原档与 `ChatToggle` **重叠**，
而旧注释里「原版 `ChatToggle` 在 −174.94」是个**错值**，真值 −254.062 —— 判据见 §2·2 与 `T06` §七·1·3）。

### 8·2 `A1302` 同族（`HudImage` / `HudImageTex`）—— **逐调用点审计做完 + 查出的真缺陷已修**

审计报告 → `资料/普查产出_1016战斗/A1302_同族逐点审计.md`（41 处调用点、37 处受审，
逐条给出「贴图 px / 原版框 / 内接后应为 / 结论」，**没有一条判不出**）。
🔴 **查出 4 条真缺陷，4 条全部已修**：

| # | 件 | 原版真值 | 我们原来 | 改成 |
|---|---|---|---|---|
| 1 | `_myStoneGem` / `_foeStoneGem`（灵族灵魂石那颗宝石） | 框 **51.028×63.015**（PA=0） | 把框**宽** 51 当高传 ⇒ 实绘 41.31×51.0，**高宽各小 19%** | `+StoneGemW/H` 两个常量：传高 `63.015` + `SetAspect(51.028/63.015)` |
| 2 | `_myDeckSizePlate` / `_foeDeckSizePlate`（牌堆张数底板） | 我 **238.5×59.11** · 敌 **210×52.05**（PA=0 拉伸） | 按贴图比例画 ⇒ **窄 2.2%**（233.2 / 205.4） | `+MyDeckSizeW/FoeDeckSizeW`：各 `SetAspect` 钉回框比例 |
| 3 | 能量水晶四颗（`_energyGem` / `_energyGemEmpty` / 敌方两颗） | `Energy Player Accumulation ON\|OFF`：**77.786×80.113**（PA=0） | 传 `0.72f`（= **框宽** 77.76）⇒ **矮 2.35 px（2.9%）** | `+EnergyCrystalW/H`：传高 `80.113` + `SetAspect` |
| 4 | `_cemeteryBtn`（战斗日志钮） | 框 64.48×**64.17**（贴图 119×119 方形 + PA=1 ⇒ 按高内接） | 传 64.5（框**宽**那档）⇒ 大 0.33 px | 传 `64.17f / 108f` |

🔴 **审计顺手发现两条「依据名写错」（结论本来就对）—— 已就地订正**（铁律 5）：
· 提示条那处写「原版那颗 Image 是 `RTgt=0`（不按图的比例）」—— **`RTgt` 是 `m_RaycastTarget`**，
  真正决定「不按图比例、拉满矩形」的是 **`m_PreserveAspect = 0`**；
· 墓志钮那段末句「（119×119 → 实绘 **0.54×**）」**读不通**（0.54 不是 119 的任何相关比值）⇒ 删掉、写明实绘 = 64.17。

✅ **审计同时核过、值本来就对、⛔ 别动的那些**（防下一轮误报）：名牌 101.04（内接）· 骷髅 54.14 ·
能量大底板 480.8 · 任务点 97.7/接片 23.9 · 信仰 149.3 / 灵魂石座 116.6 · 能量底板 93.32/91.32 ·
手牌底板 65.7 · END TURN 80.4 · 牌堆底板 218.63/190.11 · 回合灯 23.4 · 已出牌 20 · 牌背（两段式）·
提示条（PA0 拉伸已显式还原）· 设置 63.87 · 六枚光点。

⚠️ **本件没跑断言**（按现行口径）：`BattleScene.Run` 里那条「34 个粒子的材质没贴图」之外，
与这几件相关的断言**下一轮跑时现看**。
