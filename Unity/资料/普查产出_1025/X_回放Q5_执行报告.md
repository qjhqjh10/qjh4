# 执行代理 X · 回放 `Q5.1` / `Q5.3` 执行报告（2026-10-25）

> 任务：战斗线 `RE1` 余项 —— `C5 Q5.1`（回放加载失败弹窗）· `C5 Q5.3`（`replaysVersion` 门槛）。
> 白名单内改了两个文件：`Battle/BattleDriver.cs`（CRLF 30502/30502，**行尾未翻**）· `Battle/ReplayStore.cs`（LF）。
> ⛔ 没跑 Unity · 没动 git · 没碰正本/任务总表。

---

## 一、判据复核（第一权威 = 反编译方法体 + 字符串字面量表，逐个地址现查）

| 结论 | 证据 |
|---|---|
| 失败 → 走**同一个** callback，只是喂 `null` 数据（`errorCallback` 传 `0`） | `d:/2/tools/decomp_full/BattleLogItem__ClickReplayButton.c:44-53`（`PlayerDataManager__RecoverMatchRecording(…,0,…)`） |
| `param_2 == 0` ⇒ `WindowsManager.ShowPopUp(<键>, 1, 1, <OK 键>, 0, 0)` | `BattleLogItem.__c___ClickReplayButton_b__15_0.c:20-26`（**亲读全文**） |
| 弹窗正文键 = **`CustomErrors/ErrorLoadReplay`** | `DAT_18425d1a8` → RVA `0x425D1A8` → `d:/2/tools/il2cpp_out/stringliteral.json` = `'CustomErrors/ErrorLoadReplay'` |
| 那颗钮 = **`MainMenu/General/OK`** | `DAT_1842be718` → RVA `0x42BE718` → 同上 = `'MainMenu/General/OK'` |
| 本地化文案 | `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` → `CustomErrors/ErrorLoadReplay = ["Error loading match replay.", "加载比赛回放出错。"]`；`MainMenu/General/OK = ["OK","好的"]` |
| `1, 1` = `localizeTexts: true` / `closeOnEsc: true`，**一颗钮** | 与 `WindowsManager__ShowPopUp.c` 的形参顺序对位（`param_3`/`param_4` 各转进 `LoadPopUpAndShow`） |
| **`replaysVersion != "-1"` 才点亮回放钮** | `BattleLogItem__Initialize.c:206-208`：`System_String__op_Inequality(*(param_2 + 0x78), DAT_184288ae8)` → `Selectable.set_interactable`；`DAT_184288ae8` → RVA `0x4288AE8` → 同上 = `"-1"` |
| `param_2 + 0x78` = `BattleLogMatch.replaysVersion`，**服务端 → 客户端**，客户端**不做**本机/服务端版本比较 | `BattleLogMatch.cs:42`；`ServerVersion*.replaysVersion` 在客户端**零读取点**（只有遥测模板）→ 沿用 `C5 Q5.3` 的现核 |
| ⚠️ **原版「灰」的真相**：那颗钮 `m_Transition = 2 (SpriteSwap)`，`m_SpriteState.m_DisabledSprite` **有值**（pid `5693181797853584851`，与 `m_SelectedSprite` 同一颗） | `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-2480417760484458689.json` 实读（= `GameObject/ReplayButton.json` 的第 4 个组件）；`资料/普查产出_0927/对局历史_行模板与弹窗.md:36,102` |
| ⚠️ 但**那个 pid 反查不到名字** ⇒ 我们**拿不到原版那张禁用态图** | `资料/普查产出_0927/回放_界面真值.md:221` 已记「只查到 pid、没查到名」（那三个 pid 与我们这台的读数逐个吻合） |

---

## 二、`Q5.1` 回放加载失败弹窗 —— **已做**

### 结论
`BattleDriver.PlayReplay` 的**全部 4 条失败支路**现在都会：① `Debug.LogError` 出声；② 弹原版那扇
`CustomErrors/ErrorLoadReplay` 窗（宿主 = `WindowsManager.ShowMessagePopUp` = 原版 `MessagePopupWindow`
/ 类 `PopUpGameWindow`；2026-10-12 `A416` 已把 `ShowPopUp` 收编到它 ⇒ 与反编译那条链同形）；③ 返回 `false`、
**不进入回放**。⛔ 不走 `ErrorMessageBanner` —— 那是战斗 HUD 的过场横幅（原版 `UIMessageController`），
不是这条链路；按原版形状选的是**模态弹窗**。

### 改动清单（`Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`）
1. 新增常量与自检口（放在 `PlayReplay(string)` 之前）：
   - `public const string ReplayErrorKey = "CustomErrors/ErrorLoadReplay";`
   - `public const string ReplayErrorOkKey = "MainMenu/General/OK";`
   - `public int ReplayLoadErrorCountForTest` / `public string LastReplayLoadErrorForTest`
   - `void ReportReplayLoadFailed(string why)` —— 计数 + 留因 + `LogError` +
     `WindowsManager.Get().ShowMessagePopUp(ReplayErrorKey, ReplayErrorOkKey, null, closeOnEsc: true)`
     🔴 **2026-10-25 调度台追加（代理 `Y` 现核）**：`closeOnEsc: true` **必须传** —— 原版这一处实参就是
     `ShowPopUp(…, localizeTexts:1, closeOnEsc:1, …)`。不传的话这扇窗**出不去**：OK 那颗钮的 `onClick`
     我们传 `null`（原版同形 `onSubmit = 0`）· `PopUpGameWindow.Create` 的 `closeOnEsc` 默认 `false` ·
     压暗层 `BackgroundCloseButton.window` 是空引用 ⇒ 战斗里弹出来**没有出口**。⛔ 别删回默认 `false`。
2. `PlayReplay(string fileName)`：文件名空 / `ReplayStore.Load` 回 `null`（**缺失 · 解析失败 · 版本不认识，三种都在这一个出口**）⇒ 弹窗 + `false`。
3. `PlayReplay(ReplayRecord rec)`：`rec == null || rec.start == null` 与 `pb == null` 两处前置闸 ⇒ 弹窗 + `false`。
4. **顺手修（铁律 5 / 14）**：`_replayFt = null;` 原来排在两道前置闸**之后**，与它自己那句注释
   （「重建之前就失败的那些支路不许让自检读到上一局那一份」）**正好相反** ⇒ 挪到方法第一句。语义只会更对，无其它调用点受影响。

### 影响面 / 风险（如实标注）
- `WindowsManager.Get()` 在**战斗场景**里会 `EnsureHost()` **现建**一台 + 三个锚点（战斗场景没有壳的宿主）。
  这是**有意的** —— 原版战斗场景里 `WindowsManager` 本来就在场；`MenuTutorialFlow.cs:441` 那句
  「战斗场景凭空多出一套窗口管理器 = 静默污染」说的是**不该建时**建，我们这里是**该建**。
- ⚠️ **没跑到实况**：`PopUpGameWindow` 走 `MenuDraw` / `LayoutSpace`（战斗场景也在用同一套），
  理论上能建；但**它在战斗相机下是否画得出来，我没跑 Unity/没进游戏 ⇒ 未验**。真机那一步请务必看这一处。
- 现有自检**不会**因此多弹窗：我把 `Editor/BattleScene.cs` · `Editor/BattleMiscAssertE4.cs` 里
  **全部 17 处** `driver.PlayReplay(...)` 逐个看过 —— 传进去的 record 都先 `Check(x != null)` /
  前置闸断言过，**没有一处**会落到那 4 条失败支路。

### 没查清的部分
- 原版那两个 `1, 1` 的**确切形参名**（`WindowsManager__ShowPopUp.c` 的 `param_3/param_4` 被反编译抹了名）；
  值本身（`localizeTexts` / `closeOnEsc`）是从 `LoadPopUpAndShow` 的调用位次 + 我们的等价件 `ShowMessagePopUp`
  反推的，**不是逐字段读出来的**。这两档对我们**都可观测**：`localizeTexts` = 我们恒传**键**（`Term()` 过
  `Loc`，官方表里有这条 ⇒ 印中文/英文成品句）；`closeOnEsc` 已按原版实参**显式传 `true`**（见 §二 改动清单
  与「顺手发现 1」）。

---

## 三、`Q5.3` `replaysVersion` 门槛 —— **判据已落地，UI 接线被白名单挡住**

### 结论
原版判据坐得很实（见 §一）。我们这一侧的映射：**判据是本地录像文件名 `BattleLogData.Match.ReplayFile`**，
「没有」= `null` / 空串；原版哨兵 `"-1"` 也一并认。已在 `ReplayStore` 落成**唯一一份**判据 + 出声口。

### 改动清单（`Unity/MyGame/Assets/CardPresentation/Battle/ReplayStore.cs`）
- `public const string NoReplaySentinel = "-1";`（带完整判据注释，含「别和 `ReplayRecord.version` 混」）
- `public static bool ReplayAvailable(string replaysVersionOrFile)`
  = `!string.IsNullOrEmpty(x) && x != NoReplaySentinel`（= `C5 Q5.3` 建议的那一句）
- `public static void NoteNoReplay(string replaysVersionOrFile, string where)` —— **去重出声**（红线：不许静默）
- `public static void ResetNoReplayWarningsForTest()`

### ✅ 真正的「回放钮」—— **2026-10-25 已由代理 `Y` 落地（本条就地订正）**
回放钮的生产落点 = **`Shell/MatchLogRow.cs:288-294`**（`BattleLogRecord` 那一行的 `ReplayButton`，
由 `BattleLogTab` 与 `BattleLogPopup` 两扇窗共用）。它**不在我的白名单**（也不在黑名单）⇒ 我**没动它**。
🔴 **2026-10-25 调度台告我：代理 `Y` 已按同一判据改完**（现读 `git diff` 亲核）：
- 行里加 `bool canReplay = ReplayStore.ReplayAvailable(m.ReplayFile);`
- `canReplay == false` ⇒ **不建 `Hit` 命中区**（= 原版 `interactable = false`，真鼠标也点不到）+ `ReplayStore.NoteNoReplay(...)` 出声
- **不画灰态**（原版 `m_DisabledSprite` pid 反查不到名字 ⇒ 铁律 11 例外①），理由与我在 §三 原稿里写的一致
- `OnReplay` 读盘失败那一支：从「只 `LogWarning`」改成 **`LogError` + 弹同一扇 `ErrorLoadReplay` 窗（`closeOnEsc: true`）+ 不进回放**
- 另加两个自检口 `SuppressSceneLoadForTest` / `LastReplaySceneForTest`（批量自检不被 `LoadScene` 掀掉夹具）

⇒ **我原先给的补丁片段已作废（仅作判据留痕）**，不要再应用一遍：

```csharp
// ⛔ 已由代理 Y 落地（2026-10-25）—— 以下是**当时的**提案，别照抄第二遍
bool canReplay = ReplayStore.ReplayAvailable(m.ReplayFile);
if (!canReplay) ReplayStore.NoteNoReplay(m.ReplayFile, "对局历史行 `ReplayButton`（" + name + "）");
if (canReplay) Hit(ctx, rep, "Hit", repR, L_Hit, () => OnReplay(m), repBg, ArtBtn);
// else: 不建 Hit（灰可选，且只能标注「我们挑的」）
```


### 没查清的部分
- 原版禁用态**图名**（pid `5693181797853584851`）本地**查不到**（`回放_界面真值.md:221` 已记）。
  按铁律 11 例外①（判据是空的）记为「不做」；要做只能是我们挑的灰。

---

## 四、要给主对话补的断言（`Editor/*.cs` 全在黑名单，我一条都没写）

### 4.1 `Editor/BattleScene.cs`（回放那一节，紧跟现有 `PlayReplay` 用例）—— `Q5.1`

夹具提醒：`ReplayStore.OverrideDir` 设成临时目录（不要碰玩家真录像）；`ReplayStore.ResetNoReplayWarningsForTest()` 与
`WindowsManager` 的在场情况自己按需处理。

```
// 前提：把 wm 的「当前消息弹窗」记下来（下面三条是**三条不同的失败**，都必须弹同一扇窗）
int errBefore = driver.ReplayLoadErrorCountForTest;

// ① 文件缺失
bool okA = driver.PlayReplay("__nope_not_a_replay__.json");
Check(!okA, "Q5.1① 缺文件 ⇒ 放不出来（返回 false）");
Check(driver.ReplayLoadErrorCountForTest == errBefore + 1, "Q5.1① …而且记了一次「加载失败」");
var popA = WindowsManager.Get().popUpWindow as PopUpGameWindow;
Check(popA != null && popA.MessageKey == BattleDriver.ReplayErrorKey,
      $"Q5.1① …而且弹的是原版那扇窗（`{popA?.MessageKey}`，期望 `{BattleDriver.ReplayErrorKey}`）");
Check(popA != null && popA.PrimaryKey == BattleDriver.ReplayErrorOkKey,
      "Q5.1① …那颗钮就是 `MainMenu/General/OK`（原版 `DAT_1842be718`）");
Check(popA != null && popA.MessageShown == Loc.T("CustomErrors/ErrorLoadReplay") && popA.MessageShown != "CustomErrors/ErrorLoadReplay",
      $"Q5.1① …正文是**本地化后的**「加载比赛回放出错。」（实得 `{popA?.MessageShown}`）"
    + "｜🧨 键写错 / 没过 Loc ⇒ 这里印出键名 ⇒ 红");
Check(!driver.ReplaySessionForTest && !driver.ReplayFinishedForTest, "Q5.1① …而且**没进入回放**");

// ② 解析失败：往临时目录写一份坏 JSON
File.WriteAllText(Path.Combine(ReplayStore.Dir, "__bad__.json"), "{ this is not json");
bool okB = driver.PlayReplay("__bad__.json");
Check(!okB, "Q5.1② 解析失败 ⇒ 放不出来");
Check(driver.ReplayLoadErrorCountForTest == errBefore + 2, "Q5.1② …记了一次失败");

// ③ 版本不兼容：合法 JSON + 一个不认识的 version
File.WriteAllText(Path.Combine(ReplayStore.Dir, "__old__.json"), "{\"version\":1,\"actions\":[]}");
bool okC = driver.PlayReplay("__old__.json");
Check(!okC, "Q5.1③ 版本不认识 ⇒ 放不出来（⛔ 不硬解）");
Check(driver.ReplayLoadErrorCountForTest == errBefore + 3, "Q5.1③ …记了一次失败");
Check(driver.LastReplayLoadErrorForTest != null && driver.LastReplayLoadErrorForTest.Contains("版本"),
      $"Q5.1③ …而且原因里写明了是「读不出来（缺失/解析/版本）」那一类（实得 `{driver.LastReplayLoadErrorForTest}`）");

// 负例：正常一份录像 ⇒ **一扇都不许弹**
ReplayStore.ResetNoReplayWarningsForTest();
int errBeforeOk = driver.ReplayLoadErrorCountForTest;
bool okGood = driver.PlayReplay(playRec);      // 本节上面那份好录像
Check(okGood, "Q5.1(负例) 正常录像照旧放得出来");
Check(driver.ReplayLoadErrorCountForTest == errBeforeOk,
      "★★ Q5.1(负例) 正常录像**一扇窗都不弹**｜🧨 把弹窗挪到无条件支 ⇒ 本条红");
```

> **灭自证（两处一起改才可能蒙混）**：删 `ReportReplayLoadFailed` 里那句 `ShowMessagePopUp`、只留计数器
> ⇒ 上面那三条 `popA.MessageKey` 必红；把计数器也删 ⇒ 「记了一次失败」必红。
> 把 `ReplayErrorKey` 改成别的键 ⇒ `MessageKey` / `MessageShown` 两条同时红。

### 4.2 `Editor/MainMenuScene.cs`（对局历史那一节）—— `Q5.3`（判据/补丁**已就位**：`MatchLogRow` 由代理 `Y` 改完）

```
// 真值表（纯函数，最便宜的一条）
Check(!ReplayStore.ReplayAvailable("") && !ReplayStore.ReplayAvailable(null) && !ReplayStore.ReplayAvailable("-1"),
      "Q5.3 真值表：空 / null / \"-1\" ⇒ **没有回放**（原版 `interactable = replaysVersion != \"-1\"`）");
Check(ReplayStore.ReplayAvailable("abc") && ReplayStore.ReplayAvailable("4"),
      "Q5.3 真值表：其它任何值 ⇒ **有回放**");

// 行级：有录像的行建得出命中区；没录像的行**建不出**（= 原版 interactable=false 的等价物）
var rowWith    = MatchLogRow.Build(ctx, parent, r, rowReplayFile: "xxx.json");
var rowWithout = MatchLogRow.Build(ctx, parent, r, rowReplayFile: "");        // 夹具造一行 `ReplayFile = ""`
Check(FindChild(rowWith.transform,    "ReplayButton/Hit") != null, "Q5.3(有录像) `ReplayButton` 下**有**命中共");
Check(FindChild(rowWithout.transform, "ReplayButton/Hit") == null, "Q5.3(没录像) `ReplayButton` 下**没有**命中区 ⇒ 点不到");
```

> **灭自证**：把 `ReplayAvailable` 改成恒 `true` ⇒ 「没录像 ⇒ 没有命中区」那条红；
> 把 `MatchLogRow` 里 `if (canReplay)` 拆掉（永远建 `Hit`）⇒ 同一条红。
> ⚠️ 若还要断「灰」，就得先拍板**灰值**（原版禁用态图名查不到，见 §三）—— 那一条不能写成「原版灰」。

---

## 五、顺手发现（⛔ 都没越界改）

1. ✅ **`closeOnEsc` 那条 —— 2026-10-25 已由代理 `Y` 修好（本条就地订正）**
   - 原版这条 `ShowPopUp(…, localizeTexts:1, **closeOnEsc:1**, …)`（`WindowsManager__ShowPopUp.c` 的 `param_4`）。
   - `PopUpGameWindow.Create` / `WindowsManager.ShowMessagePopUp` 原来把 `closeOnEsc` **写死 `false`**
     （`Shell/PopUpGameWindow.cs:296`，那句「实证 `closeOnESC = 0`」只是**prefab 自己的序列化值**，
     而原版 `ConfigurePopUp` 是**调用方传的**）。**Y 已把形参加上、默认 `false`**（别的调用点逐字不变），
     我的 `ReportReplayLoadFailed` 与 `MatchLogRow.OnReplay` 两处都传 `closeOnEsc: true`。
   - ⚠️ 仍然成立的观察：这扇窗**没有别的出口**（OK 的 `onClick` = `null`，原版同形 `onSubmit = 0`；
     压暗层 `BackgroundCloseButton.window` 是空引用）⇒ **任何**复用这扇窗的新调用点都必须自己给 `closeOnEsc` 或回调。
2. **`ReplayStore.Load` 的失败原因不进结构**：三种失败（缺失 / 解析 / 版本）都由 `Load` 自己 `LogWarning`，
   但**不返回原因**，所以 `BattleDriver` 那侧只能统一写「读不出来（缺失/解析失败/版本不认识）」。
   要分得更细就得给 `Load` 加一个 `out string why` 重载（本次没做，避免动公共签名）。
3. ✅ **生产路径那一跳 —— 2026-10-25 已由代理 `Y` 补上（本条就地订正）**
   `PlayReplay(string fileName)` 目前全仓零调用点（只有定义 + 注释）⇒ 生产路径实际是
   `MatchLogRow.OnReplay` → `ReplayStore.PendingPlay` → `Start()` → `PlayReplay(rec)`。
   我报的「`MatchLogRow.OnReplay` 读盘失败只 `LogWarning`、不弹窗」**Y 已改**：现在 `LogError` +
   弹同一扇 `ErrorLoadReplay`（`closeOnEsc: true`）+ 不进回放。⇒ `Q5.1` 在玩家真实那条路上**已经看得到窗**。

---

## 六、验证

- `TMPDIR=/tmp/wf_x1 bash d:/4/Unity/工具/typecheck.sh`
  - **最终一次（补 `closeOnEsc: true` 之后）**：`运行时错误数: 0` / `编辑器错误数: 0`。
  - 中途有一次运行时 11 条错，**全部集中在 `Assets/CardPresentation/Battle/AttackSelector.cs`**
    （`_title` / `_titleText` 未定义）—— **不是本代理负责的文件**（按 `CLAUDE.md` 铁律 13·3：错误全在别人的文件上
    ⇒ 那是别人正在写的活）。随后重跑已归零；**`BattleDriver.cs` / `ReplayStore.cs` 全程零错误**。
- 行尾：`BattleDriver.cs` CRLF `30502` / LF `30502`（**未翻**，改完两次都核过）· `ReplayStore.cs` 本来就是 LF。
- ⛔ 没跑 Unity 自检、没进真机（铁律 12：那是主对话/同步点的活）。

---

## 七、需要主对话决定的 1 件事

1. ⚠️ **真机看一眼这扇窗**：`PopUpGameWindow` 走 `MenuDraw` / `LayoutSpace`，**在战斗相机下画不画得出来我没验**
   （没跑 Unity、没进游戏）。ESC 现在关得掉了（`closeOnEsc: true`），但「看不看得见」只能真机验。
   —— 原 §七 的另外两条（`MatchLogRow` 授权 · `OnReplay` 失败分支）**已由代理 `Y` 做完**，销账。

