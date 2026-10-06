# WD2 · 领奖弹窗 8 行对齐（A493 #1–#8）

> 独占文件：`Shell/DailyStreakPopup.cs` · `Shell/DailyRewardPopup.cs`（**只动了这两个 + 本报告**）
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用只读 `git status` / `git diff --numstat` / `git diff`）· ⛔ 没改正本
> · ⛔ 没越白名单（`Shell/{MissionsTab,ChatPanel,AllianceMemberTab,SocialWindow,CampaignRewardWindow}.cs` 与 `Editor/*`、`工具/*` **全部只读**）
> ⚠️ 本件**没跑自检**（用户口径：A 表清完再跑）· ✅ 类型检查**改完立刻**跑过：`TMPDIR=/tmp/wf_wd2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
> 📌 本报告的「实测」= `工具/menu_dump.py`（本件亲跑 4 次）+ `工具/menu_rect.py`（亲跑 2 次）+ 现读源码。
> 🔴 **起点纪律**：一切以【现读代码 + 原版 dump】为准，**没有**照 `H37` 那张表的「现在怎么画的」列反推（那张列的可信度见 §四）。

---

## 一、结论

| 账 | 状态 | 一句话 |
|---|---|---|
| **A493 #1–#8（8 行对齐）** | ✅ **8 行全改完** | 6 行在 `Shell/DailyStreakPopup.cs`（`:267/269/295/304/307/407` 现读）、2 行在 `Shell/DailyRewardPopup.cs`（`:270/385` 现读）。**逐处的原版真值都是本件亲跑 dump 现读的**（不是抄 H37 的表），8 处矩形与原版**逐值相同**、只有对齐这一笔差。 |
| **#1 的「文案当节点名」** | ✅ **顺手修了** | `"Current streak:"` → **`"Current Streak"`**（**原版那颗的节点名就是它**，dump 实读）⇒ 不是「另起一个稳定名」，是**改回照原版**。全库代码里这个名字只剩 2 处、`Editor/*.cs` 断言 0 处 ⇒ **零撞车**（详见 §三）。 |
| **断言** | ⏸ **没落地（只设计，写在 §五）** | 宿主 = **`RewardsScene.Run`**（`Editor/RewardsScene.cs:906` 的 `Run()` 里那两个 `Section`）—— 🔴 **那个文件归本批 `W-E3`**（`资料/普查产出_1013/批次计划_1013.md:99`）+ A435 刀 2 甲（同文件 `:110`）⇒ **不是我的文件，按白名单「拿不准就只报不改」处理**。 |
| **H37 现状列** | ⚠️ **#1–#9 对 / #10 错** | 我核了全 10 行：#10（`ChatPanel.cs:219`）H37 写「居中」是**错的**（那个口的形参缺省 `alignLeft = true` ⇒ 当时就画 Left）；**#1–#9 成立**（含我顺带只读核的 #9）。详见 §四。 |
| 类型检查 | ✅ **0 / 0** | `TMPDIR=/tmp/wf_wd2 bash d:/4/Unity/工具/typecheck.sh` —— 运行时 **0** · 编辑器 **0**（没有一条错落在别人的文件上）。 |
| 行尾 | ✅ **没翻** | 二进制读：两个文件改前改后**都是纯 LF**（`DailyStreakPopup` CRLF 0 / LF 495；`DailyRewardPopup` CRLF 0 / LF 414）。`git diff --numstat` = **43/6** 与 **18/2**（那点增量全是判据注释）。 |

---

## 二、逐行表

**原版真值全部来自本件亲跑**（下面每行的「取自哪次 dump」那一格是**同一条命令**，只是节点不同）：

```bash
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup"  --depth 10 --no-sprite
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup" --depth 12 --no-sprite
# 交叉核对（同一份几何、另一个工具，逐值相同）：
python d:/4/Unity/工具/menu_rect.py bundle_menus_assets_all "Daily Streak Popup" --depth 5 --relative
```

列名 → 字段：`对齐=` = `menu_dump.py:609-610` 打印的 `m_HorizontalAlignment`/`m_VerticalAlignment`，
枚举表 `:100` ⇒ **`Left` = 1 · `Center` = 2 · `Right` = 4**（同 `H37` §三·1 的读法）。

| # | 文件:行（**改后**现读） | 现读到的现状（改前） | 原版真值 | 取自哪次 dump | 改后值 | 判据出处 |
|---|---|---|---|---|---|---|
| 1 | `Shell/DailyStreakPopup.cs:267`（原名实参在改前的 `:255`） | `MenuDraw.Text(p, S_CurLabel, …, "Current streak:", 70f, QText)` —— **节点名 = 文案**；**无对齐** ⇒ `Label` 把文字块**居中**放在框心 | `Streak Successful/Current Streak`：矩形 `43.0 212.4 → 533.9 295.1` · **`对齐=Left/Capline`** · `字号=70` · 文案 `'Current streak:'` · **`字距=5`** | `"Daily Streak Popup" --depth 10 --no-sprite`（本件亲跑；`menu_rect --depth 5 --relative` 给 `43.00 212.42 533.91 295.07`，逐值相同） | `"Current Streak"`（**原版节点名**）+ `MenuDraw.AlignLeft(curLbl, S_CurLabel)` | `H37` §四·1 #1（对齐一格对）+ 本件亲自 dump |
| 2 | `Shell/DailyStreakPopup.cs:269`（改前 `:256`） | `MenuDraw.Text(p, S_CurValue, …, "Current Streak Value", 80f, QText)`，无对齐 | `Streak Successful/Current Streak/Current Streak Value`：`545.9 212.4 → 583.8 295.1`（宽 **37.85**）· **`对齐=Left/Capline`** · `fs 80` · **`字距=5`** | 同上 | `MenuDraw.AlignLeft(curVal, S_CurValue)` | 同上 #2 |
| 3 | `Shell/DailyStreakPopup.cs:295`（改前 `:274`） | `MenuDraw.Text(p, S_Info, …, "Info", 36f, QText)`，无对齐 | `Streak Successful/Info`：`47.5 830.0 → 1872.5 880.0` · **`对齐=Left/Midline`** · `fs 36` | 同上 | `MenuDraw.AlignLeft(info, S_Info)` | 同上 #3（⚠️ **断签面板那颗 `Info` 是 `Center/Midline`，没动**，见下面「别一刀切」） |
| 4 | `Shell/DailyStreakPopup.cs:304`（改前 `:277`） | `MenuDraw.Text(t, S_TimerNext, …, "Next Rewards text", 36f, QText)`，无对齐 | `…/Timer/Next Rewards text`：`573.3 997.5 → 935.0 1047.5` · **`对齐=Right/Midline`**（**全窗唯一一颗右**） | 同上 | **`MenuDraw.AlignRight(nxt, S_TimerNext)`** | 同上 #4 |
| 5 | `Shell/DailyStreakPopup.cs:307`（改前 `:279`） | `MenuDraw.Text(t, S_TimerText, …, "Timer Text", 36f, QText)`，无对齐 | `…/Timer/Timer Text`：`985.0 997.5 → 1346.7 1047.5` · **`对齐=Left/Midline`** · `fs 36` | 同上 | `MenuDraw.AlignLeft(tmr, S_TimerText)` | 同上 #5 |
| 6 | `Shell/DailyStreakPopup.cs:407`（改前 `:371`） | `MenuDraw.Text(h, H_Title, …, "Window Title", 67.55f, QText)`，无对齐 | `Header With Back Button/Header Background/Window Title`：`155.0 38.0 → 534.3 120.7` · **`对齐=Left/Capline`** · `fs 67.55` · **`字距=5`** | 同上 | `MenuDraw.AlignLeft(title, H_Title)` | 同上 #6（⚠️ 与 **A475** 那颗**同族不同颗**，见「两条别混」） |
| 7 | `Shell/DailyRewardPopup.cs:270`（改前 `:264`） | `MenuDraw.Text(gc, R(D_ClaimedTex), …, "Claimed Tex", 41.95f*0.8f, QText)`，无对齐 | `…Daily Reward Popup Entry/NormalReward/Gacha Reward Claimed/Claimed Tex`：`255.1 244.2 → 428.1 276.0` · **`对齐=Left/Midline`** —— **8 份实例逐值相同**（`…:638.7…` 那一份也一样） | `"Daily Reward Popup" --depth 12 --no-sprite`（本件亲跑） | `MenuDraw.AlignLeft(claimedTx, R(D_ClaimedTex))` | 同上 #7 |
| 8 | `Shell/DailyRewardPopup.cs:385`（改前 `:370`，**F1 记的 `:342` 更早**） | `MenuDraw.Text(t, TimerText, …, "EverguildTextMeshPro (1)", 50f, QText)`，无对齐 | `Timer/EverguildTextMeshPro (1)`：`990.2 990.8 → 1454.0 1054.1` · **`对齐=Left/Capline`** · `fs 50` | 同上 | `MenuDraw.AlignLeft(tm, TimerText)` | 同上 #8 |

### 两点「别一刀切」（都写进代码注释了）

1. **#3 的 `Info` 有两颗**：改的是 **`Streak Successful` 里那颗**（原版 `Left/Midline`）；
   另一颗在 **`Streak Failed`** 面板（`Shell/DailyStreakPopup.cs` 的 `BuildFailed` 那句 `F_Info`），
   原版是 **`Center/Midline`**（同一次 dump：`47.4 583.0 → 1872.6 633.0`，`对齐=Center/Midline`）⇒ **那颗保持居中、一行没动**。
2. **#4 与 #5 同一个 `Timer` 底下、方向相反**：`Next Rewards text` = **Right**、`Timer Text` = **Left**。

### 两条「同族但不同颗」（⛔ 别据 A475/A404 去改别处）

- **#6 的 `Window Title` 与 A475 那颗不是同一颗**：A475 改的是 `Shell/LiveOpsEventWindow.cs:747` 那颗（走 `LiveOpsEventWindow.BuildHeader`）；
  本窗**自建顶栏**（走 `Shell/DailyStreakPopup.cs` 的 `BuildHeader`）⇒ **改 LiveOpsEventWindow 管不到这里**。
  ⚠️ 两颗的**原版字段恰好一样**（都是 `Left/Capline` + `字距=5` + 框左沿 155.0），**别因为值相同就以为是一处**。
- **#7/#8 的 DailyRewardPopup 与 DailyStreakPopup 是两扇窗的两份 `Timer`**：`Timer` 节点各自的 `x1` 都是 598.31、
  但**子件矩形不同**（streak：`573.3/940.0/985.0`，reward：`245.5/938.6/990.2`）⇒ 两窗的 `S_Timer*` / `TimerText` 常量**不能互相套用**。

---

## 三、`:255` 那处「文案当节点名」的修法说明

**改前**（现读，改前是 `:255`）：

```csharp
MenuDraw.Text(p, S_CurLabel, DailyData.StreakCurrentLabel(), Color.white, "Current streak:", 70f, QText);
//                                              ↑ 数据（文案）                              ↑ 节点名 = 同一句文案
```

**为什么是缺陷**（三条，前两条是原报告给的，第三条是本件加的）：

1. **节点名会随文案/语言变** —— 本仓正在中英混用期（`DailyData` 的文案全是可替换的），`StreakCurrentLabel()` 一旦改成中文，节点名当场变成中文 ⇒ 任何按名取节点的断言/夹具**静默找不到**（红线：不许静默失败）。
2. **节点名里带空格和冒号**（`' '` / `':'`）—— 对 `FindChild`（全等比较）不致命，但对 `FindPath`（`'/'` 分隔）这类路径拼接**只有坏处没有好处**。
3. 🔴 **新加的理由：原版那颗的节点名【就是 `Current Streak`】**（`menu_dump … "Daily Streak Popup" --depth 10 --no-sprite` 实读 `Streak Successful > Current Streak`；`资料/说明书/04_界面UI/菜单全树.md:7130` 也是 `Current Streak [43,212 491x83] text:'Current streak:'`）⇒ 传文案不只是「名字不稳定」，是**连节点名都没照原版**。所以修法**不是**另起一个名字，是**改回原版那个**。

**改成**：`"Current streak:"` → **`"Current Streak"`**。

**为什么可以这么改（零撞车的证据）**：

- 全库代码里 `"Current streak:"` 只剩 **2 处**：`Shell/DailyData.cs:815`（那个**返回值**，没动）与 `Shell/DailyStreakPopup.cs`（那个**节点名实参**，已改）。
- `Editor/*.cs` grep `Current streak:` / `Current Streak` ⇒ **0 命中**（本件亲跑复核，与 A493 报告 :183 的结论一致）
  ⇒ **没有任何既有断言/夹具按这个名字取节点** ⇒ 改名**不可能撞红**；反过来也说明这一族**今天零覆盖**。
- 改后名字与 `S_CurValue` 那颗（`Current Streak Value`）**不冲突**（全等比较，前者不是后者的前缀命中，且 `FindChild` 是全等）。

⚠️ **遗留一处标注不准**（⛔ 不在本件白名单，只报不改，见 §七·5）：`Shell/DailyData.cs:815` 那句注释写着「⚠️ 我们挑的」——
但原版 prefab 的文本**就是** `'Current streak:'`（dump 实读，`资料/日常_原版规格.md:488` 也记着）⇒ 它不是「我们挑的」，是**原版字面量**。

---

## 四、H37 那张表「现状」列的实测对照（哪几行不可信）

**H37 §四·1 那张 10 行表的「现在怎么画的」列**，本件**逐行现读核过**（表里第 1–8 行是我这 8 行，第 9/10 行是别人的账，**只读**）：

| H37 行 | 文件:行 | H37 写「现状」 | **实测** | 判定 |
|---|---|---|---|---|
| **1–8** | 我这 8 行 | 「居中」/「未对齐 ⇒ 框心居中」 | ✅ **对** —— 8 处**全是 `MenuDraw.Text(...)` 直调**，那个函数**根本没有 `align` 形参**（`Shell/MenuDraw.cs:1553-1556`），走的 `Label.Create(..., pivot=(0.5,0.5))` ⇒ `RefreshBounds` 把文字块**居中**摆在框心（`Battle/Label.cs:781-785` 的原话 + `:302` 注释） | ✅ 成立 |
| **9** | `Shell/MissionsTab.cs:897-898`（'Weekly Challenge'） | 「居中」 | ✅ **对**（本件顺带只读核了整条链）：`MissionsTab.Txt`（`Shell/MissionsTab.cs:341-353`）→ `_win.TextBox(...)`，`_win` = `RewardsWindow : MainMenuSubmenuWindow`（`Shell/RewardsWindow.cs:181`）→ `MenuWindowBase.TextBox`（`Shell/MenuWindowBase.cs:325`）→ `MenuDraw.TextBox`（`Shell/MenuDraw.cs:1579`）—— **整条链一个 `Align*` 都没有** ⇒ 画的是居中 | ✅ 成立 |
| **10** | `Shell/ChatPanel.cs:219`（`Placeholder`） | 「居中」 | 🔴 **错**。`ChatPanel` 自己那个 `Text(...)` 口的形参当时是 **`bool alignLeft = true`** ⇒ 「不对齐」这件事**从来没发生过**，那一处**当时就画的是 Left**。**实据**：`git diff -- Unity/MyGame/Assets/CardPresentation/Shell/ChatPanel.cs` 的**被删行**原文 = `Label Text(… float autoMin = 0f, bool alignLeft = true, …)`，而函数体里那句 `if (lb != null && alignLeft) MenuDraw.AlignLeft(lb, r);` **是真跑了的** | 🔴 **不可信**（本批 W403 已在代码里就地订正，见下） |

**错因（本件判定的、可推广的一条）**：

> H37 那个扫描器的判据是「**附近没有 `Align*` 也没有 `alignLeft: true`**」—— 它**看不到【形参的缺省值】**
> （`bool alignLeft = true` 这种「缺省就在生效」的口）。
> ⇒ **凡是走「带缺省对齐的薄包装」的调用点，H37 的现状列都会判成「居中」**，而它其实早就对齐了。

**这条错因对【我这 8 行】不适用** ⇒ 我的「现状」是真·未对齐：
这 8 处走的是 **`MenuDraw.Text`**，那个函数**一个对齐形参都没有**（不是「缺省是居中」，是**没有这个口**），
所以扫描器的盲区在这里**不存在** ⇒ 起点纪律要防的那件事（「按『我们没显式对齐』反推，而缺省就在生效」）在 #1–#8 上**没有发生**。

**交叉印证（别人已经处理了）**：本批 **W403** 把 `ChatPanel` 那个缺省**删掉改成必填**、并给 `:219` 显式补 `alignLeft: true`（**零行为变化**），
同时在 `Shell/ChatPanel.cs` 的代码注释里就地订正了 `H37:133`（本件读到时那处已是修改后的状态，`mtime 2026-10-06 09:52`）。
⇒ **H37 §四·1 第 #10 行以他那条订正为准**，本件只是**独立复核 + 给出错因**，没有改他任何东西。

---

## 五、断言清单（断什么 · 期望值来源 · 改坏法 · 落在哪个 `*Scene.Run`）

### 0. 宿主（**两件都现读定了，不是猜**）

| 项 | 值 | 怎么定的 |
|---|---|---|
| **自检宿主** | **`RewardsScene.Run`** | `Editor/RewardsScene.cs:906` 是 `public static void Run()`；本窗的两个现场都在它里面：**`Section("§四 \`Daily Reward Popup\`…")` 在 `:5692`**（`DailyRewardPopup.Create` 在 `:5693`、`wm2.OpenWindow(dr)` 在 `:5694`）· **`Section("§五 \`Daily Streak Popup\`…")` 在 `:5735`**（`:5736-5737` 建窗 + 开窗） |
| **能不能落进这个文件** | 🔴 **不能（本件没落）** | 该文件归本批 **W-E3**（`资料/普查产出_1013/批次计划_1013.md:99`：`Editor/RewardsScene.cs` = A239+A353+A390+A512 那半）+ **A435 刀 2 甲**（同文件 `:110`）。简报的白名单写的是「**只在你确定宿主、且那个文件不是别人的活时**」⇒ 它是别人的活 ⇒ **只报不改** |
| **有没有既有断言在盯这 8 个节点** | ✅ **一处都没有** | 本件亲跑复核（与 A493 报告 :183 同结论）：`Editor/*.cs` 里 grep **8 个节点名**（`Current streak:` / `Current Streak Value` / `Next Rewards text` / `Timer Text` / `Claimed Tex` / `EverguildTextMeshPro` / `Window Title`）与**8 个矩形常量**（`S_CurLabel` / `S_CurValue` / `S_Info` / `S_TimerNext` / `S_TimerText` / `H_Title` / `D_ClaimedTex` / `TimerText`）⇒ **命中全是别的窗**（`CampaignRewardWindow` 的 `Premium Panel/Timer Text` @`RewardsScene.cs:5643` · `OfferContainer` 的 `Timer/Timer Text` @`ShopScene.cs:2547` …）⇒ **改这 8 行不会让任何既有断言变红，也不会把任何既有断言变假绿** |
| **会不会打破「凡…」式断言** | ✅ **不会** | 本件**一个节点都没加**（只改了 1 个名字 + 8 处坐标），**没有新增任何 `ImageQuad`** ⇒ 简报里那条「凡 `!RectOfUnion` / 『一个 quad 都没有』式断言，任何新加一层绘制都会打破它」**不适用**。两个窗现读的既有守卫是 `CheckHoverSwap(dr.transform,…)`（`RewardsScene.cs:45-50`，审计 `WindowButton` 的悬停换图）与 `CheckAbsorbRule`/`CheckShadeRule`（档位不变量）—— 都不吃 `Label` 的位置 |

### 1. 断什么（8 条 · 期望值**一律是原版字面量**，⛔ 不写 `DailyStreakPopup.S_CurLabel` 那类被测实现自己的常量）

落点：`RewardsScene.Run()` 里**两个 `Section` 各自那一段内**（streak 段已现成有 `var succ = FindChild(ds.transform, "Streak Successful");` @`RewardsScene.cs:5785`，`succ` 就绪且已断「连胜态开着」⇒ 标签**一定量得出宽**，正好插在它后面）。

```csharp
// ★ A493 #1–#8：8 颗字的【真渲染边】= 原版框边（逐颗实读）
//   判据（本件亲跑，不是抄表）：
//     python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup"  --depth 10 --no-sprite
//     python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup" --depth 12 --no-sprite
//   ⛔ 期望值写【原版字面量】（含 ⚠️ 那两条特殊的），不许写被测实现里的 S_*/H_*/TimerText/D_ClaimedTex。
//   🔴 量法必须量 TMP 自己的 mesh（textBounds 四角过 localToWorldMatrix），⛔ **不许用 `Label.WorldW`** ——
//      它是缓存（`_tmpW`），与 A490/H37 §五 那条同因（位置与宽度会互相抵消、恒等原值）。
//      本仓现成的 `TextLeftPx/TextRightPx`（`Editor/RewardsScene.cs:427/433`）**走的正是 WorldW**
//      ⇒ 它对本账**能用但更弱**（见下面「牙口」那一段）。
System.Func<Transform, bool, float> edgePx = (t, left) => {
    var tmp = t != null ? t.GetComponentInChildren<TextMeshPro>() : null;
    if (tmp == null) return float.NaN;
    var b = tmp.textBounds; var M = tmp.transform.localToWorldMatrix;
    float r1 = float.MaxValue, r2 = float.MinValue;
    for (int c = 0; c < 4; c++) {
        var p = M.MultiplyPoint3x4(new Vector3((c % 2 == 0) ? b.min.x : b.max.x,
                                               (c < 2) ? b.min.y : b.max.y, 0f));
        float px = PxOf(p.x); r1 = Mathf.Min(r1, px); r2 = Mathf.Max(r2, px);
    }
    return left ? r1 : r2;
};

// —— 每日连登窗（`succ` 已现成）——
// #1 `Current Streak`：原版左沿 43.0（`Streak Successful/Current Streak`，Left/Capline）
var a1 = FindChild(succ, "Current Streak");
CheckTrue(a1 != null && a1.GetComponentInChildren<TextMeshPro>() != null, "（前提）`Current Streak` 量得到 TMP");
CheckNear(edgePx(a1, true), 43.0f, 1.5f,
          "★ A493#1：`Current Streak` 的真渲染左缘 = 框左沿 **43.0**（原版 `对齐=Left/Capline`）"
        + "（⛔ 不显式对齐 ⇒ 停在框心 288.46 − 文字宽/2 ⇒ 红）");
// #2 `Current Streak Value`：原版左沿 545.91（框宽仅 37.85 ⇒ ⚠️ 牙口另说，见下）
CheckNear(edgePx(FindChild(succ, "Current Streak Value"), true), 545.91f, 1.5f,
          "★ A493#2：`Current Streak Value` 的真渲染左缘 = 框左沿 **545.91**（原版 `Left/Capline`）");
// #3 `Info`（连胜面板那颗；⚠️ 断签面板那颗是 Center，别取错树）
CheckNear(edgePx(FindChild(succ, "Info"), true), 47.47f, 1.5f,
          "★ A493#3：`Streak Successful/Info` 的真渲染左缘 = **47.47**（原版 `Left/Midline`）");
// #4 `Next Rewards text`：**右**缘 = 935.0
CheckNear(edgePx(FindPath(succ, "Timer/Next Rewards text"), false), 935.0f, 1.5f,
          "★ A493#4：`Next Rewards text` 的真渲染**右**缘 = 框右沿 **935.0**（原版 `Right/Midline`，全窗唯一右）"
        + "（写成 `AlignLeft` ⇒ 左缘对、右缘差一整个文字宽 ⇒ 红）");
// #5 `Timer Text`：左缘 = 985.0
CheckNear(edgePx(FindPath(succ, "Timer/Timer Text"), true), 985.0f, 1.5f,
          "★ A493#5：`Timer Text` 的真渲染左缘 = **985.0**（原版 `Left/Midline`）");
// #6 `Window Title`：左缘 = 155.0（⚠️ 与 A475 那颗同值不同颗）
CheckNear(edgePx(FindPath(ds.transform, "Header With Back Button/Window Title"), true), 155.0f, 1.5f,
          "★ A493#6：本窗（自建顶栏）`Window Title` 的真渲染左缘 = **155.0**（原版 `Left/Capline`）"
        + "—— 与 A475 那颗（`LiveOpsEventWindow`）**同值不同颗**");

// —— 每日奖励窗（在 §四 那一段里；`e0`/`e1` 已现成 = Collected / Unlocked 那两格）——
// #7 `Claimed Tex`：原版左沿 = 255.1（该格**只有 Collected 才亮** ⇒ 必须取 Collected 那格，即现成的 `e0`）
CheckNear(edgePx(FindPath(e0, "NormalReward/Gacha Reward Claimed/Claimed Tex"), true), 255.1f, 1.5f,
          "★ A493#7：`Claimed Tex` 的真渲染左缘 = **255.1**（原版 `Left/Midline`，8 份实例同值）");
// #8 `Timer/EverguildTextMeshPro (1)`：左沿 = 990.2
CheckNear(edgePx(FindPath(dr.transform, "Timer/EverguildTextMeshPro (1)"), true), 990.2f, 1.5f,
          "★ A493#8：倒计时那颗的真渲染左缘 = **990.2**（原版 `Left/Capline`）");
```

**期望值来源**：全部是**原版 prefab 的矩形边**（本件亲跑 dump 逐颗实读，见 §二那张表的「取自哪次 dump」列），
⛔ **不是** `DailyStreakPopup.S_CurLabel` / `H_Title` / `TimerText` 这些**被测实现自己的常量**（那是同式自证）。

**改坏法**（每条都要实测一次；删掉 Shell 里那一句 `MenuDraw.AlignLeft/AlignRight`）：

| 条 | 改坏法 | 预期 |
|---|---|---|
| #1 #3 #5 #7 #8 | 删掉对应的 `MenuDraw.AlignLeft(...)` 一句 | 文字块回到框心 ⇒ 左缘右移 `(框宽 − 文字宽)/2` ⇒ **红** |
| #4 | 删掉 `MenuDraw.AlignRight(nxt, S_TimerNext)` **或**把它错写成 `AlignLeft` | 前者右缘左移、后者左缘对而右缘错 ⇒ **两种都红** |
| #2 #6 | 同上 | ⚠️ **可能仍绿**，见下一条 |
| #6 追加 | 顺带把 `H_Title` 的框左沿从 155 改掉 | 期望值是**字面量 155.0** ⇒ 红（这条挡「期望值从被测实现里读」） |

### 2. 🔴 牙口（红线：**弱断言分不出两种状态** —— 这一节必须留着）

**判据**：`Align*On` 是「把文字块**居中**摆在框心」的**修正** ——
若**该颗的文字渲染宽 ≈ 它自己的框宽**，那么「居中」与「贴左」**画出来是同一个位置**（左缘本来就 = 框左沿）
⇒ 那条断言**分不出两种状态**（改坏法照样绿）。

**本件不跑 Unity ⇒ 量不到每颗的渲染宽**，只能按原版字面量给**风险预判**（⛔ 不许当结论）：

| 条 | 框宽 | 原版那颗的宽度线索 | 预判 |
|---|---|---|---|
| #1 `Current Streak` | 490.91 | 原版带 `ContentSizeFitter(h:PreferredSize)` ⇒ **原版那边框宽 = 文字宽**；我们**没有 CSF** ⇒ 我们这边大概率窄于框 | 中 |
| **#2 `Current Streak Value`** | **37.85** | 文案 `'7'` @`fs 80` ⇒ 文字宽 ≈ 框宽 | 🔴 **高（很可能无牙口）** |
| #3 `Info` | 1825.06 | 长句 @`fs 36`，大概率窄于框 | 低 |
| #4 `Next Rewards text` | 361.69 | `'More Rewards In'` @`fs 36` | 低 |
| #5 `Timer Text` | 361.69 | `'19h 23m'` @`fs 36`（7 字符） | 低 |
| **#6 `Window Title`** | **379.36** | 原版 `sz=379.30` + `CSF h:PreferredSize？` ⇒ 那 379 大约就是**文字宽** | 🔴 **高（很可能无牙口）** |
| #7 `Claimed Tex` | 172.97 | 我们文案 `'已领取'` @`33.56pt`（3 个汉字） | 中低 |
| #8 `EverguildTextMeshPro (1)` | 463.78 | `'19h 23m'` @`fs 50` | 低 |

⇒ **交接口径**：**先按上面这段代码落 8 条，然后逐条跑改坏法**；
**哪几条仍绿 ⇒ 当场如实标注「这颗两态同形 ⇒ 本条无牙口」**（红线：不许把「没验」写成「验过了」），
并选一条出路：① 量一次我们这边的渲染宽并把**期望值改成「渲染左缘」可分辨的那一档**（例如同时断「文字块中心 ≠ 框心」）；
② 若真的两态同形，就**据实记「视觉上这一颗改与不改看不出来」**，让主对话决定这条还要不要留。

### 3. 为什么**不用** `TextLeftPx/TextRightPx`（现成的两个助手）当主判据

`TextLeftPx`（`Editor/RewardsScene.cs:427-431`）= `PxOf(lb.transform.position.x) − lb.WorldW*108*0.5f` ——
**它用的正是 `Label.WorldW`（缓存 `_tmpW`）**。对本账（纯对齐、不动字距/字号）它**不是自证**、
也**能分辨两态**（未对齐时读回的是框心，对齐时读回的是框左沿），但：
① 它读回的就是 `AlignLeftOn` **自己写进去的那个数**（`pos.x = worldLeftX − parentX + W/2`）⇒ 「实现说什么就信什么」；
② 与 A490/H37 §五 那条**同一个病灶**：改渲染宽度的东西（`SetCharSpacing` / `SetAutoFitBox` 的次序）它**看不见**。
⇒ **可以用它做快速回归，但主判据请用 §五·1 那段量 mesh 的写法**（与 A490 的修法方向一致）。

---

## 六、没查清 / 没做的

1. 🔴 **断言没落地**（见 §五·0）——`Editor/RewardsScene.cs` 不是我的文件（W-E3 + A435 刀 2 甲）。
   本件只给了**可直接粘贴的设计**（含期望值字面量、改坏法、落点行号）。**这一步不能算「已验」**。
2. **本件没跑 Unity**（按批次口径）⇒ 「对齐后画面上像不像」**没验**；`RewardsScene.Run`（全套里 ≈ 4m20s）由主对话在同步点跑。
3. **量不到 8 颗的渲染宽** ⇒ §五·2 那张「牙口」表是**预判不是结论**；哪几条真的无牙口只能靠改坏法实测。
4. **`字距=5` 只查了「本窗有没有」**（答：0 处 `SetCharSpacing`），**没查**「为什么原版这 3 颗带 5、同窗其余 4 颗不带 5」
   （是预设表还是逐颗手填 —— 本地只有 prefab 值，**没查过它的来源**）。⇒ 要补的时候先想清楚这个，别一刀切给它整个窗加 5。
5. **`Current Streak` / `Current Streak Value` 的 `ContentSizeFitter(h:PreferredSize)` 我们没建** ——
   本件只**观察到**这件事（dump 段落里两颗都带 `CSF h:PreferredSize？`），**没查**当初建窗时是不是有意省的（正本 §五 没记）。
6. **`Fill Line` 的 `scl=1.2`**（见 §七·3）：只查实了「工具印的是**布局框**、原版渲染框 = ×1.2」，
   **没查**是谁/按什么口径把那 1.2 丢掉的（是 `menu_rect.py` 的口径、还是建窗时漏了）。
7. **H37 §四·3 那 6 组「没查全」本件一行没碰**（那是**普查扩大**，不属于这 8 行；`A493` 报告已建议另立账）。
8. **没验过点阵后端那一档**（字体资产缺失时 8 处会走 A476 的出声分支、位置不动）——
   A476 已给了断言写法与造 `_tmp == null` 的办法（H37 §五），本件没接。

---

## 七、顺手发现（⛔ 一个都没在本件里改）

1. 🔴 **`DailyRewardPopup` 的 `Timer` 少了原版那颗 `EverguildTextMeshPro`（'Más Recompensas En' = `'More Rewards In'`）**。
   - 原版：`Daily Reward Popup/Timer` 底下**三颗** —— `EverguildTextMeshPro`（`245.5 990.8 → 935.0 1054.1` · **`Right/Capline`** · `fs 50`）·
     `Image`（`WF_icon_clock` 53.29²）· `EverguildTextMeshPro (1)`（`990.2 …` · `Left/Capline` · `fs 50`）。
   - 我们：`Shell/DailyRewardPopup.cs:373-386`（现读）**只建了后两颗**。
   - 三方证据：本件亲跑 dump · `资料/说明书/04_界面UI/菜单全树.md:1092`（`EverguildTextMeshPro [245,991 690x63] text:'Más Recompensas En'`）·
     `资料/日常_原版规格.md:456`（「`Timer` → TMP **fs=50** + **`WF_icon_clock`**(53.292²) + 时间 **fs=50**」= 三件）·
     `资料/说明书/04_界面UI/菜单全树_要点_第1块.md:215`（`'Más Recompensas En' + Icon + '19h 23m'`）。
   - **不是「查不到」**（正本自己就写着三件）⇒ 按铁律 11 这是**要做**的活。⛔ 本件没做（不在 8 行里、且会改这一窗的版面）。
   - ⚠️ 补的时候**别一刀切**：那颗是 **`Right/Capline`**，而 #8 那颗（我改的）是 **`Left/Capline`** —— 同一个 `Timer` 底下方向相反。
2. **`字距=5` 缺失（3 颗，都在 `DailyStreakPopup`）**：`Current Streak` · `Current Streak Value` · `Window Title`
   （同一次 dump 的 `字距=5` 列实读；同窗其余 4 颗没有这一列），本窗 `SetCharSpacing` **0 处**。
   ⚠️ **加的时候必须排在 `MenuDraw.AlignLeft` 之前** —— 字距改渲染宽、排在后面就按旧宽定位（`H37` §三·2 / A475 那个坑）。
3. **`Fill Line` 的 `scl=1.2` 没接**：`Shell/DailyStreakPopup.cs:60` 的 `S_FillLine` = **布局框** `1487.98×66.37`，
   原版画出来的是 **`1785.57×79.65`**（`menu_rect.py` 自己的行末就印着 `视觉框=×1.2 → 视觉 1785.57×79.65`；
   `资料/日常_原版规格.md:487` 6a 也记着 `scl=(**1.2**)`）；`:254` 那句 `MenuDraw.Rect(...)` **没有缩放口**（`Shell/MenuDraw.cs:1278` 的签名里没有）。
   同一颗原版还是 **`Sliced(0.434,0,0.0567,0.624) + ppuMul`**，我们走 `MenuDraw.Rect`（Simple）⇒ **两笔都未核过**（见 §六·6）。
4. **`Header With Back Button` 底下原版有两颗底图、我们只建了一颗**：
   `Header Background`（`ContentSizeFitterMinMax + HorizontalLayoutGroup`，dump 的布局解算值 `0.00 21.65 → 595.30 137.01`；
   `menu_rect.py --relative` 因为布局组没跑印的是 `0.00 … 0.00`）与 **`Header Background (1)`**（`H_Bg`，`−462.10 → 87.90`），
   **两颗的 sprite pid 相同**（`6473405944757030420`）。原版的 `Window Title` 挂在**前者**底下，我们挂在 header 根上。
   ⚠️ **未查清**：全库 grep `Header Background` 在 `资料/日常_*.md` **零命中** ⇒ **它没被记过**（是「有意省的容器」还是「漏了」判不出来）。
5. **`Current Streak Value` 的父子关系我们和原版不一样**（不是缺陷，是**会绊住路径写法**）：
   原版 = `Streak Successful/Current Streak/Current Streak Value`（**子件**），我们 = **兄弟**（都挂在 `Streak Successful` 下）。
   ⇒ 谁写断言都用 `FindChild(succ, "Current Streak Value")`，⛔ 别写 `FindPath(… "Current Streak/Current Streak Value")`（会找不到）。
6. **一处「我们挑的」标注不准**（铁律 5 那一类，低优先）：`Shell/DailyData.cs:815` 的
   `StreakCurrentLabel()` 注释写「⚠️ 我们挑的」，但原版 prefab 的文本**就是** `'Current streak:'`（dump 实读）⇒ 它是**原版字面量**。
7. **已核无恙（列出来免得下个会话再查一遍）**：8 处都走 `MenuDraw.AlignLeft/AlignRight` ⇒ **天然落到 A476 那条「点阵后端要出声」**的口上
   （`Battle/Label.cs` 的 `AlignLeftOn/AlignRightOn` 在 `_tmp == null` 时 `NoteDotAlign` 出声后返回）⇒ 本件**没有新增静默口**；
   `MenuDraw.AlignLeft(null, r)` 也有 `if (lb != null)` 挡（`Shell/MenuDraw.cs:1598-1601`）⇒ 建文字失败时不会 NRE。

---

## 八、类型检查结果

```
$ TMPDIR=/tmp/wf_wd2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

✅ **运行时 0 · 编辑器 0** —— **改完两个 `.cs` 立刻跑的**（独立 `TMPDIR`，不覆盖别人的产物）。
✅ **没有一条错落在别人的文件上**（= 不需要「重跑一次 + 如实记别人的半成品」那一步）。
✅ 行尾：两个文件改前改后**都是纯 LF**，`git diff --numstat` = `DailyStreakPopup.cs 43/6` · `DailyRewardPopup.cs 18/2`（没有整篇翻行尾）。
