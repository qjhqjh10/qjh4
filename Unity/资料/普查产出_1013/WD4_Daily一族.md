# WD4 · Daily 一族六件（A495 · A496 · A498 · A509 · A510 · A643）

> 执行写手代理 · 2026-10-13 · 第四轮「清空 A 表」
> 落地文件 = `Shell/DailyData.cs` · `Shell/DailyRewardPopup.cs` · `Shell/DailyStreakPopup.cs` ·
> `Shell/RewardWindow.cs` · `Editor/RewardsScene.cs`（**只新增一节 + 调度台追加的 A371 那一笔**）。
> ⛔ 没跑 Unity（`RewardsScene.Run` 是同步点的验收点）· ⛔ 未动 git · ⛔ 未动两张正本 · ⛔ 未越白名单。
> ⚠️ 行号一律是**本文件写作时刻的现读**；同文件有别的写手在并发增删 ⇒ 认位置请按**锚点句**。

---

## 一、结论（六件各自）

| 账 | 状态 | 一句话 |
|---|---|---|
| **A495** | ✅ **做完**（**形状与裁定的字面不同**，语义一致 —— 见 §四） | ESC 也走「先收再关」；`CloseAllWindows()`（程序性）**不收**；`DailyRewardPopup.cs` 那段「留给调度台裁」的注释**已写回裁定** |
| **A496** | ✅ **做完**（①–⑥ 全落） | 落在 `§九(a)` **之后**新开的 WD4 一节；用 A498 的新写口造「可领」那一态 |
| **A498** | ✅ **做完** | `DailyData.ForceRewardClaimedForTest(int day, bool claimed)`；H38 §⑭「不加写口」那句**在裁定下作废**，理由写进了它的 doc |
| **A509** | ✅ **做完** | 读口改成**问守卫真正问的那一位**（`!StreakRewardUnlocked(k)`）；今天逐格行为不变、去掉的是那个「或」的吞并 |
| **A510** | ✅ **做完** | `RewardWindow.Build()` 重建前补 `PointerLayer.UnregisterOwnedBy(gameObject)`（同族 7 处里原来只有这扇漏） |
| **A643** | ✅ **做完** | 新增中性口 `DailyData.MoreRewardsInText()`，**两窗都改用它**；旧名 `StreakNextRewardsText()` 降级为**一行转发** |
| 🆕 **追加件（A371）** | ✅ **做完** | `Editor/RewardsScene.cs` 周常那条 `CheckFontWindow` 期望值 **10f → 15f** + 注释按新判据重写（见 §十一） |

---

## 二、A510（同族那 6 处补的是哪一句 · 这扇窗怎么补）

**同族先例补的就是这一句**（逐处现读，形式完全一样）：

```csharp
PointerLayer.UnregisterOwnedBy(gameObject);      // 或 Root.gameObject / p.Node.gameObject
```

| 先例 | 位置（现读锚点） |
|---|---|
| `PlayerProfileWindow.Setup` | `PointerLayer.UnregisterOwnedBy(Root.gameObject);`（重建前，`:683`） |
| `InboxWindow.BuildMessageList` | `PointerLayer.UnregisterOwnedBy(gameObject);`（`RegisterScroll` 前一行，`:383`） |
| `FriendsTab` | `PointerLayer.UnregisterOwnedBy(gameObject);`（`_scroll = …` 前一行，`:220`） |
| `ChatPanel` | `PointerLayer.UnregisterOwnedBy(_root != null ? _root.gameObject : gameObject);`（`:535`） |
| `BattleLogPopup` | `PointerLayer.UnregisterOwnedBy(gameObject);`（`:162`） |
| `SettingsWindow` | `PointerLayer.UnregisterOwnedBy(gameObject);`（`:663`） |

**本窗怎么补**（`Shell/RewardWindow.cs` 的 `Build()`，`_listHolder = MenuDraw.Node(...)` 之后、
`_scroll = new MenuScroll(...)` **之前**）：

```csharp
PointerLayer.UnregisterOwnedBy(gameObject);          // ← 新增（+ 12 行注释）
_scroll = new MenuScroll(ScrollView, ScrollView.CX - MinContentW * 0.5f, ScrollView.CX + MinContentW * 0.5f)
{ Elastic = true, Inertia = true, Owner = gameObject };      // Owner 就是 gameObject ⇒ 撤得掉
```

- **为什么必须是这一句**：`MenuScroll` 是**普通 class**（非 Unity Object ⇒ `== null` **恒假**），
  `PruneScrolls()` 的两条判据（`s == null` / `Owner == null`）对它**都不成立**；
  而重建时**根不会死**（`Owner` 恒活）⇒ 旧条目永远留表、**仍会被滚轮命中**
  （`OnChanged` 指向已销毁的节点）。判据原文 → `Shell/PointerLayer.cs` 的 `UnregisterOwnedBy` 注释。
- **顺序**：撤的是**上一轮**那一份 ⇒ 必须在 `new MenuScroll(…)` **之前**（新手这一份随后才登记）。
- **实测过的那条链**（H45 §四·2）：H45 给 `RewardsScene` 补的那三句 `rw.Build()` 每档会让这扇窗多留一条
  （约 +3/次自检）；本件修的是**根**，那三句不用动。

---

## 三、A509（改取 `StreakRewardUnlocked` 之后，那条「弱改坏法」为什么就咬得住了）

**改法**（`Shell/DailyData.cs`，锚点 = `public static bool StreakRewardClaimed`）：

```csharp
// 改前：_streakClaimed[SI(i)] || SI(i) < StreakCollected        ← 「或」：位置那一半会把布尔整段吞掉
// 改后：
int k = SI(i);
if (k == StreakCollected) return !StreakRewardUnlocked(k);   // = 守卫真正问的那一位（_streakClaimed[k]）
return k < StreakCollected;                                  // 其余各格：位置口径（还没轮到 ⇒ false）
```

**为什么这就咬得住了**：改前「可领那一格」与「它前面的格」走的是**同一个表达式**——
`k < StreakCollected` 恒真就把 `_streakClaimed[k]` 那一半盖住；只要 `StreakCollected` 一改
（H45 §四·1 点出的那一档：`Force(…, false)` 之后**照样读 `true`**），读口说的和守卫说的就**不是同一件事**，
而 `Editor/RewardsScene.cs` 的起点快照 / 收工还原（`claimed0`）**全建在这个读口上**。
改后：**可领那一格直接取 `!StreakRewardUnlocked(k)`** ⇒ 两个读口在这格上**互为补**，
「力了还读真」在结构上不可能再发生。

**今天逐格行为不变**（三态逐一核过）：
- `k < StreakCollected` ⇒ 改前 `flag || true` = true；改后 true —— **同**；
- `k == StreakCollected` ⇒ 改前 `flag || false` = flag；改后 `!Unlocked` = flag —— **同**；
- `k > StreakCollected` ⇒ 改前 = `_streakClaimed[k]`（出厂恒 false）；改后 = false —— 唯一差异只在
  「用测试口硬把**还没轮到**的格置成已领」那一档，生产路径到不了（`CollectStreak` 的守卫只放行 `k == StreakCollected`）。

**唯一界面读点不动**：`DailyStreakPopup.BuildEntry` 用的是 `if (unlocked && !claimed)` 这个**合取**
⇒ 只有 `i == StreakCollected` 那一格进画面，另两档怎么读都不进画面（改前改后同一张画面）。

---

## 四、A495（接地形状 · 「只关不收」那个口 · 那条断言的前提怎么改的）

### 4·1 裁定落地成了什么形状（**与裁定的字面不同，如实标注**）

裁定原话（派单）：**覆写 `Close()` 走带收那条**，**另给一个「只关不收」的口专供 `CloseAllWindows()`**。

🔴 **字面形状落不了地，卡在白名单上**（不是判据不清）：
`Shell/WindowsManager.cs` 的 `CloseAllWindows()` 是**逐扇调虚方法 `Close()`**
⇒ 要让「程序性收口」走另一个口，**必须改 `Shell/WindowsManager.cs`**，而它**不在本件白名单**（跨文件撞车面）。
⇒ 本件改用**同族惯例**（`Shell/DailyStreakPopup.cs` 就是这个形状）：**每条生产路径自己调收口，`Close()` 保持纯关**。

落地物（`Shell/DailyRewardPopup.cs`）：

| 物 | 形状 | 谁调 |
|---|---|---|
| `public void CloseCollecting()` | `DailyData.DailyRewardAutoCollect(); Close();`（**先收再关**） | 返回钮 `onClick`（原来是内联那两句，现在改调它 ⇒ 只有一份） |
| `public override bool ESCPressed()` | `bool closed = base.ESCPressed();`（门槛全在基类）→ `if (closed && CurrentState == WindowState.Closed) DailyData.DailyRewardAutoCollect();` | `PointerLayer.KeyCancel()`（真 ESC 那条路） |
| `Close()` | **一个字没覆写** = 程序性口（`CloseAllWindows()` 走的正是它） | —— |

**可观测语义与裁定逐条一致**：ESC / 返回钮「先收再关」；`CloseAllWindows()` **不收**（有断言）。
**⚠️ 一处如实标注的次序差**：原版是「**收完再关**」（`TryCollect(action)` 的续作才是 `base.Close()`），
我们的 ESC 那一跳是**关完再收**（因为「门楣」在基类的 `ESCPressed` 里、而把「收」挂进 `Close()`
就会连 `CloseAllWindows()` 一起收）。终局逐条相同（窗关 + 进 `Wallet` + 领奖窗在最上面），
差异只在「领奖窗是在底窗回位之后才弹」这一拍。理由写在 `ESCPressed` 的 doc 里。
**⛔ 没抄第二份门槛**（`PointerLayer.InputEnabled` / `closeOnEsc` 只在基类判）——「两处写同一条规则」那条红线。

### 4·2 「点窗外」那一路：**原版这扇窗就没有**（本件顺手核清，不是缺口）

- `dump.cs` 的 `DailyRewardPopup` 字段表（`d:/2/tools/il2cpp_out/dump.cs:68853-68867`）只有六项：
  `header(0x78)` · `armySelector(0x80)` · `rewardsTrack(0x88)` · **`closeButton(0x90)`** · `trackSidePanel(0x98)` · `timerText(0xA0)`
  —— **没有任何 background/backdrop 按钮字段**（对比 `DailyStreakWindow` 有 `backgroundButton`）。
- `DailyRewardPopup__Start.c` 只接三样：那个 `UnityEvent`（走虚表槽 → `Close`，= 左下那颗圆钮）、
  `ArmySelector.OnArmySelected`、一个 `Action<object>` —— **没有背景点击**。
- 菜单全树里 `Daily Reward Popup/Menu Dark Background` 只有 **1 个 script**，
  而 `Daily Streak Popup/Menu Dark Background` 有 **3 个**（那扇才有背景关钮）。
⇒ 我们的实现（只画压暗层、不建 `ShadeHit`）**是照原版的**；裁定里「点窗外」那一格对这扇窗**是空集**。

### 4·3 夹具那边的前提（`Editor/RewardsScene.cs`）

- **一个字没改既有断言**：`§四` 末尾那句 `wm2.CloseAllWindows()` 仍然**不收**那一格（这正是选这个形状的硬理由 ——
  它排在 `§九` **之前**，若把「收」覆写进 `Close()`，`§九`「（前提）找得到一个可领的抽屉」**当场红**，
  2026-10-13 逐步核过这条链）。
- **A479⑦ 那条前提照旧**（「这一刻一格里没有可领的」）：WD4 那一节收工**显式把那一格拨回「已领」**
  （新写口 + 起点快照，见 §五）。
- **新增两条断言**把「收 / 不收」两态钉死（见 §七 的 A495-a / A495-b）。

---

## 五、A496 + A498（写口 + ①–⑥ 落在哪）

### 5·1 A498 的写口

`Shell/DailyData.cs`（放在「自检用」那一族里，`StreakClaimableDay` 之后）：

```csharp
public static void ForceRewardClaimedForTest(int day, bool claimed) { _rewardClaimed[RI(day)] = claimed; }
```

- **形状照同族**（`ForceSkullsCountForTest` / `ForceWeeklyProgressForTest` / `ForceStreakClaimedForTest`）：
  与 `RewardStateOf` **共用同一份** `_rewardClaimed[]`（⛔ 不另立一份状态）。
- 它的 doc 里**写死了那句作废**：**H38 §⑭「本件不为奖励抽屉加任何读口/写口」在本裁定下作废**，
  理由 = 它当年成立的前提（「那一格一旦被收就回不去」）已被「加写口」这条裁定取代；
  同族先例的规矩是「**两态判据不许只断得到一态**」。
- ⚠️ 静态数组 ⇒ 自检**必须还原**（WD4 一节按**起点快照**逐格还原）。

### 5·2 ①–⑥ 落在哪（**和 H42 §三那两条落法都不撞红**）

**落在 `§九` 之后、A479/A481 那一节之前**（`Editor/RewardsScene.cs` 新增的 WD4 一节里），顺序：

1. **先造态**：把 4 格 `_rewardClaimed[]` 全拨 `false` ⇒ 只有下标 2 那格回 `Unlocked`
   （`RewardsCollected=2` / `RewardCurrentValue=3` / `_rewardTarget={1,2,3,4}`）；
2. ① 前提（可领那格找得到 + 图名/数量取得出来）；
3. ② 真点返回钮（`ClickButtonByQuad`，走 `PointerLayer` 真路径）；
4. ③ `RewardStateOf == Collected` · ④ `Wallet` **+N** · ⑤ 弹了一扇领奖窗；
5. ⑥ 负例：写口置成「**已领过**」再点一次 ⇒ 窗照样关、`Collected` 不变、**一份都没再发**、没弹窗；
6. **收工还原**：按起点快照逐格拨回 ⇒ `Check(anyUnlocked, false)`（**=`A479⑦` 那条前提的原文**）。

🔴 **一条就地订正（铁律 5）**：H38 §六 ⑥ 那句「把那一格再置回 `Unlocked`」**与它自己给的两条期望值矛盾**
（`RewardStateOf == Collected` + `Wallet` 差 0 —— `Unlocked` 态再关一次本来就该**再发一份**）
⇒ 照**期望值**落地（显式置成「已领」），并在代码注释里写清。⛔ **没去改 H38 那份报告**（不在白名单）。

⚠️ **为什么不是「插在 §九(a) 之前、用新写口在收完之后拨回去」**（A表现核 §A496 那句话）：
本件白名单是「`RewardsScene.cs` **只加你这一段**」，插到 `§九(a)` 之前会**改到别人的既有语句**
（`§九(a)` 的前提断言 + `§四` 末尾那句 `CloseAllWindows()`）；插在 `§九` 之后 + 写口造态
**覆盖相同、零改动既有行**，也正是派单里那句「①–⑥ 落在 `§九(a)` **之后**」。

---

## 六、A643（中性名那个口 · 两窗怎么换的）

`Shell/DailyData.cs`：

```csharp
public static string MoreRewardsInText() { return "More Rewards In"; }   // 字面量只留这一处
public static string StreakNextRewardsText() { return MoreRewardsInText(); }   // 旧名 = 一行转发
```

- **判据**（写在 doc 里）：原版两窗**同一条 I2 词条** —— 连登窗 prefab 那颗（`Daily Streak Popup/
  Streak Successful/Timer/Next Rewards text`）是英文 `'More Rewards In'`；奖励窗那颗
  （`Daily Reward Popup/Timer/EverguildTextMeshPro`）落到了 **es** `'Más Recompensas En'`
  （同词条、两个语言 —— 铁律 5·c：一个值 ≠ 全部情况）。出处 → `资料/普查产出_1013/WD3_领奖弹窗补建.md` §四·3。
- **两窗都换**：`Shell/DailyStreakPopup.cs` 的 `Next Rewards text` 与
  `Shell/DailyRewardPopup.cs` 的 `EverguildTextMeshPro` 各改一行（调用点各带一句注释说明为什么换中性名）。
- **旧名保留为转发**（⛔ 不删）：它原来是公开口、有既有调用点风险；保留它**不产生第二份字面量**
  （「两处写同一条规则」那条红线的要求是**判据只留一处**，不是名字只留一个）。

---

## 七、断言清单（断什么 · 两态 · 改坏法 · 落点）

全部落在 `Editor/RewardsScene.cs` 新增的那一节：
`Section("🆕 WD4 · Daily 一族六件（A495 生产/程序性关窗 · A496+A498 正例 · A509 读口 · A510 滚动登记 · A643 词条）")`
（**位置 = `§九` 的收尾 `wm2.CloseAllWindows();` 之后、A479/A481 那一节的注释块之前**）。

| # | 断什么 | 两态怎么分开的 | 改坏法（哪一句 ⇒ 哪条红） | 落点 |
|---|---|---|---|---|
| A498-p | 前提：把 4 格全拨 `false` 之后，找得到一格 `Unlocked`（+ 图名/数量取得出来） | —— | 写口写不进去 ⇒ 这条先红 | WD4 一节开头 |
| A498-a | 写口 `true` ⇒ `Collected`；写口 `false` ⇒ `Unlocked` | **同一位布尔的两个方向** | 写口写成空函数 / 写错数组 ⇒ 第二条红 | 同上 |
| A509-a | `Force(d,false)` ⇒ `Unlocked==true` **且** `Claimed==false` | 与下一条合成两态 | 读口退回只问位置那一半 ⇒ 红 | A509 段 |
| A509-b | `Force(d,true)` ⇒ `Unlocked==false` **且** `Claimed==true` | 同上 | 「恒真/恒假」的实现各红一条 | A509 段 |
| A510-a | `RewardWindow.Build()` 之后再 `Build()` ⇒ `PointerLayer.ScrollCountForTest` **不变** | 两个量取（build 前 / build 后） | 删 `UnregisterOwnedBy` 那一句 ⇒ 每次 +1 ⇒ 红 | A510 段 |
| A495-a | ESC（`PointerLayer.KeyCancel()` 真路径）⇒ 窗关 **+ `Collected` + `Wallet` +N + 弹一扇领奖窗** | 与 A495-b 成对 | 删 `ESCPressed()` 覆写里那句收 ⇒ ③④⑤ 三条红 | A495 段 |
| A495-b | `wm2.CloseAllWindows()`（程序性）⇒ 窗关 **+ 那一格仍 `Unlocked` + `Wallet` 不变 + 没弹窗** | **与 A495-a 同一位状态、只换关闭入口** | 把「收」覆写进 `Close()` ⇒ 这条红（= 裁定里那个「只关不收」口的由来） | A495 段 |
| A496①–⑤ | 返回钮真点 ⇒ 窗关 + `Collected` + `Wallet` +N + 弹一扇窗 | 与 ⑥ 成对 | 删 `CloseCollecting()` 里那句收 ⇒ 红 | A496 段 |
| A496⑥ | **已领过**再关一次 ⇒ 窗照样关 + 仍 `Collected` + **一份都没再发** + 没弹窗 | 与 ①–⑤ 成对（同一位、只换“领过没”） | `DailyRewardAutoCollect` 改成「无条件收第一格」⇒ 红 | A496 段 |
| A643-a/b/c | 两窗各自画的是 `'More Rewards In'`（**手写字面量**）**且两窗逐字相同** | 两条手写字面量 + 一条跨窗相等 | 任一窗改回自己写一份字面量 ⇒ c 红；改中性口的返回值 ⇒ a/b 红 | A643 段 |
| 收工-t | 还原后 `anyUnlocked == false` · 场上无遗留领奖窗 | —— | 少还原 ⇒ 下一节 A479⑦ 的前提红 | WD4 一节末尾 |

⚠️ **去自证**：A643 的两条期望值**是手写字面量**（⛔ 不读 `DailyData.MoreRewardsInText()`）；
A510 量的是 `PointerLayer.ScrollCountForTest`（**指针层自己的登记数**，不是被测实现里的常量）；
A509 量的是两个公开读口在同一状态下的返回值，**没有一个数是从被测表里读出来的**。

---

## 八、没查清 / 没做的

1. 🔴 **A495 裁定的字面形状没落地**（「覆写 `Close()` + 只关不收的口专供 `CloseAllWindows()`」）——
   **原因 = 白名单**（要改 `Shell/WindowsManager.cs`），**不是判据不清**。语义已按同族惯例等价落地（§4·1）。
   📌 **若调度台要把字面形状补上**，改动面只有一处：`WindowsManager.CloseAllWindows()` 那一圈
   （`var d = openWindows[i] as DailyRewardPopup; if (d != null) d.CloseWithoutCollect(); else openWindows[i].Close();`
   —— 那还需要 `DailyRewardPopup` 侧再补一个 `public void CloseWithoutCollect() { base.Close(); }`）。
   ⚠️ **本件没做**，也没占那一处白名单。
2. **A495 的次序差**（关完再收 vs 原版收完再关）：已如实标注（§4·1），**没实跑 Unity**
   ⇒ 「终局逐条相同」是**推理**（窗栈/`Wallet`/领奖窗三条），不是实测。
3. **A509 改后「逐格行为不变」**：三档是**按表达式推的**，没跑 Unity（`k > StreakCollected` 且
   `Force(…, true)` 那一档**唯一有差异**，生产到不了）。
4. **`RewardWindow` 实例复用**（`Shell/RewardWindow.cs:488-498`）那条路**没验**：
   A510 那句 `UnregisterOwnedBy(gameObject)` 对「同一实例重开」和「新实例」都成立（都按 `Owner` 撤），
   但**只有真跑才算**。
5. **夹具那一节的实跑**：本次一切断言**没跑过**（按口径，`RewardsScene.Run` = 同步点验收）。

---

## 九、顺手发现（⛔ 只报不改）

1. ✅ **「点窗外」不是缺口**（已核清，供 A 表销账 / 别再开账）：原版 `DailyRewardPopup` **没有**背景关钮
   （判据 = `dump.cs` 字段表无 background 字段 + `DailyRewardPopup__Start.c` 只接三样 + 菜单全树 script 数
   1 vs 连登窗 3）。**换句说：这扇窗只有 ESC 与返回钮两条生产关闭路径。** 详见 §4·2。
2. 🔴 **本窗**（`DailyRewardPopup`）**和连登窗一样，压暗层没有命中区** ⇒ 玩家点窗外**什么都不会发生** ——
   这是**照原版**的（同 1），但它意味着「点暗处想关窗」这个直觉在这扇窗上**本来就无效**
   （与 `RewardWindow` 那扇不同，后者原版有 `closeButton` 背景关）。⛔ 别按「别的弹窗都有关钮」来一刀切。
3. ⚠️ **`Editor/RewardsScene.cs` 里 A479⑦ 那条前提的文案**现在**略陈**（它写「= §九(a) 已经把唯一那一格收掉了」，
   而本节之后还多了一句「WD4 一节已经把它拨回『已领』」）—— 断言**照样成立**，文案严格讲少了一句；
   ⛔ 本件不改别人那一行（派单「只加你这一段」），**留记在这里**。
4. ⚠️ **`RewardWindow.Build()` 的 `ScrollCountForTest` 类断言以后要注意基线取法**：`RegisterScroll` 自带
   `PruneScrolls()` ⇒ 基线必须取在**至少一次 `Build()` 之后**（本件就是这么写的），否则会把「顺手清掉的死条目」
   误判成「涨了/没涨」。
5. ℹ️ **`StreakNextRewardsText()` 现在没有生产调用点**（两窗都改用中性口了）。本件**故意保留**为转发
   （防别的并发写手还在用它）；若调度台确认全仓无引用，可另开一条销账删掉它。

---

## 十、类型检查结果

```
TMPDIR=/tmp/wf_wd4 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

- **跑过 4 次**（A-Daily 几笔之后 / 新节落地后 / 三处修正后 / 追加件 A371 之后），**每次都是 0/0**，
  **没有一条错落在别人文件上**。
- **行尾**：五个文件都是**纯 LF**，改完逐个体过 `CRLF==0`（`DailyData.cs` 1888 行 · `RewardWindow.cs` 2066 ·
  `DailyStreakPopup.cs` 619 · `DailyRewardPopup.cs` 507 · `RewardsScene.cs` 9611）；`git diff --numstat` 也没有翻行尾的迹象。

---

## 十一、🆕 追加件：A371 那条断言（2026-10-13 调度台中途追加）

**改了哪几行**（`Editor/RewardsScene.cs`，锚点 = `Section("🆕 A371/A372 里程碑格里的**阈值数字**…")` 内的
周常那一段，现读 `:2198-2208`）：

```csharp
// 改前：
// 字号窗口：原版 `Weekly Mission Milestones Step (3)/holder/text` = fs 50 · auto[10~50]；
// 周常不在 `Special Missions` 子树里（_s = 1）⇒ 不乘 1.15，就是 [10, 50]。
CheckFontWindow(wkR37(), "Step Text", 10f, 50f, "★ 周常格里的数字：自适应窗口 = 原版 `[10,50]`（这一卡无 `localScale`）");
// 改后：期望值 10f → 15f；上面两行注释按新判据重写（+ 4 行）
CheckFontWindow(wkR37(), "Step Text", 15f, 50f, "…= 原版 `[15,50]`（…判据 = `Weekly Mission Milestone T1/holder/text` = 运行期那一份）");
```

**依据**（调度台给的判据 = 同批 WM2 亲跑 `menu_dump` 复核）：
- 运行期 `Instantiate` 用的那份 = **`Weekly Mission Milestone T1` 的 `holder/text` = `auto[15.0~50.0]`**；
- `Weekly Mission Milestones Step (3)` 的 `auto[10.0~50.0]` 是**页内作者预览**，
  会被 `MissionMilestonesDisplay.Setup` 的 `DestroyAllChildren` 删掉 ⇒ ⛔ 不能当原版值（= A579 的病根）；
- 同一件事 `资料/普查产出_1013/WM1_任务页三笔.md` §七·2 记过，只是那条断言当时没跟着改。

**✅ 与实现侧已对齐（本件顺手核过，值得记一笔）**：`Shell/MissionsTab.cs` 传的正是
`Txt(..., small ? 42.2f : 50f, small ? 10f : 15f, 50f, 36f)`（`small` = 每日骷髅卡）
⇒ **每日那一支仍走 10（×1.15 = 11.5）**、**周常那一支走 15** —— 也就是说这条断言改完**与实现一致**，
不改的话下一次 `RewardsScene.Run` **必红**（调度台的判断逐条对上）。
**同节每日那条（`CheckFontWindow(sk37, "Step Text", 11.5f, 57.5f, …)`）一个字没动** ✅。

---

## 十二、改动文件清单（本件）

| 文件 | 改了什么 |
|---|---|
| `Shell/DailyData.cs` | ① A498 写口 `ForceRewardClaimedForTest`（+18 行 doc）② A509 读口 `StreakRewardClaimed` 改写（+25 行 doc）③ A643 中性口 `MoreRewardsInText()` + 旧名转发（+12 行 doc） |
| `Shell/DailyRewardPopup.cs` | A495：`CloseCollecting()` + `ESCPressed()` 覆写（+48 行 doc/注释）；返回钮改调它并**写回裁定**（原来的「留给调度台裁」整段替换）；A643 换中性口 |
| `Shell/DailyStreakPopup.cs` | A643 换中性口（+2 行注释） |
| `Shell/RewardWindow.cs` | A510：`Build()` 里补 `PointerLayer.UnregisterOwnedBy(gameObject);`（+14 行注释） |
| `Editor/RewardsScene.cs` | 新增 WD4 一节（约 180 行：A498 / A509 / A510 / A495 / A496 / A643 五组断言 + 还原）；A371 那条断言 10f→15f + 注释重写 |
