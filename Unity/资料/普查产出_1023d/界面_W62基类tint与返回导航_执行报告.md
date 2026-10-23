# 界面线 `W62·真机` 两处静态缺陷 + `T16` `D10` 探针 · 执行报告（执行代理 · 2026-10-23）

> 范围 = 主对话派单的三件：
> ① `W62②` 左栏 `Shadow` 的 tint **并进基类**（⛔ 不给某一扇子窗打补丁）
> ② 活动窗 `updateNavPanel` 按原版基座 `Parent Event Window` 对齐成 `1` + 「ESC 关窗 ⇒ 左竖导航灯回 `Main`」断言（含灭自证）
> ③ `T16` 的 `D10`（Toast 根高）**只做探针**：留空读数 + 出声，⛔ 不猜数值
>
> 纪律：**未跑 Unity / player / `-executeMethod`**（红线，由主对话在同步点跑）；**未动 git**；
> **未改白名单外的文件**；只跑 `TMPDIR=/tmp/wf_ui{,2} bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（两次干净读）。
> ⚠️ **断言写了但没实跑** —— 按 `CLAUDE.md` §13·6，只有主对话那次 `ShellScene.Run` / `MainMenuScene.Run` 跑绿才算数。

---

## 一、逐件：改前 → 改后 / 改在哪 / 断言 / 还差什么

### 1. `W62②` 左栏 `Shadow` 的 tint 并进基类

| 项 | 内容 |
|---|---|
| **改前** | `MainMenuSubmenuWindow.BarShadowTint` 缺省 = `null`（= 不上色 = 图本色白 ⇒ 屏上一条**白带**）；只有 `CollectionWindow` 显式覆写 `(0,0,0,0.475)` ⇒ 奖励 / 商店两扇吃 `null`，与原版不符。旧注释还写着「奖励/商店的色只读到矩形、没读到色 ⇒ 按同族推断」。 |
| **改后** | 基类缺省 = **`new Color(0f, 0f, 0f, 0.4745098f)`**（原版 `m_Color` 的 float32 现读 `0.4745098054409027` = 121/255）。四扇窗**共用这一处判据**，没有给任何子窗打补丁。 |
| **改在哪** | `Shell/MainMenuSubmenuWindow.cs` 的 `BarShadowTint`（doc + getter）· 同文件 `BuildBar` 那段 `Shadow` 注释补一句「基类默认值已并成原版色」 |
| **判据（本轮现读，⛔ 不是照截图/同族推断）** | `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "<窗>" --depth 5 --relative --no-sprite`：<br>· `Rewards Base Submenu Variant` → `Shadow 167.2 70.9 214.8 1080.0 … Simple **(0,0,0,0.475)**`<br>· `Shop Menu Variant` → `Shadow … Simple **(0,0,0,0.475)**`（未激活的 `Shadow (1)` 同值）<br>· 另用 python 扫 `bundle_menus_assets_all/MonoBehaviour/*.json`，把 `m_Color` alpha∈(0.46,0.49) 且 RGB=0 的件按 `m_GameObject` 反查名字 ⇒ 全部 `Shadow` / `Shadow (1)` 的 **`a = 0.4745098054409027`**（float32），**没有第二个值** |
| **断言** | `Editor/ShellScene.cs` 的 `Section("W62：左栏 Shadow …")`：<br>· 收藏窗（覆写）tint = `(0,0,0,0.475)` —— 不变<br>· **奖励窗（不覆写）** tint = `(0,0,0,0.4745098)`（新口径，含 🧨「基类默认改回 `null` ⇒ 白带 ⇒ 红」）<br>· **商店窗（不覆写）** tint 同值（新增；🧨「只在收藏窗那一族给 tint」⇒ 红）<br>· 顶边两条（收藏 158.64 / 奖励 70.94）不变 |
| **还差什么** | ① `CollectionWindow.BarShadowTint`（`Shell/CollectionWindow.cs:143`）现在与基类默认**等价**（`0.475` vs `0.4745098`，差 0.0005 = 亚像素，同一原版字段）⇒ 那一行覆写已多余，**不在本代理白名单**，建议后续顺手删（删了不改观感）。<br>② 奖励 / 商店的真实观感仍要**真机复验**（用户 2026-10-22 把这两页的功能挂起了，但这一处是「与原版不符」，按铁律 11 该做；主对话跑 `ShellScene.Run` 后建议进 player 各看一眼左栏）。<br>③ `RewardsScene.Run` / `ShopScene.Run` 只断 `Shadow` 的矩形（`RewardsScene.cs:1043` / `ShopScene.cs:1293`），**没有断色** ⇒ 不需要改；但它们会跟着变观感。 |

### 2. 活动窗 `updateNavPanel` 对齐基座 + 「ESC ⇒ 灯回 `Main`」断言

| 项 | 内容 |
|---|---|
| **改前** | `ParentEventWindow.ParamsUpdateNavPanel = false`（照 `Two Sides Event Window` 那份 MB `-1838579764277826042` 的 `0`）⇒ ESC 关掉活动窗后**不调** `SetMainToggle(Main)` ⇒ 左竖导航高亮**停在 `Event`**。 |
| **改后** | `ParamsUpdateNavPanel = true`（照基座 **`Parent Event Window`** 那份 MB `6349037533193994564` 的 **`1`**）。`Create()` 那一行注释同步改；文件头第 ② 条加一句「`updateNavPanel` 不取 Two Sides 那份」防两份说法打架。 |
| **改在哪** | `Shell/ParentEventWindow.cs`：窗参常量块（`ParamsUpdateNavPanel` + 其上位注释）· `Create()` 里的赋值注释 · 文件头「② 根组件」那三行 |
| **判据（原版 MB 现读）** | `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6349037533193994564.json`（`m_GameObject = -3290546486736551612` = `Parent Event Window`）= `type 0 · windowsPlacement 5 · closeOnESC 1 · **updateNavPanel 1** · extraScaleSmallScreen 1.0`；对照 `MonoBehaviour_-1838579764277826042.json`（GO `-2798723332429074938` = `Two Sides Event Window`）= `closeOnESC 0 · updateNavPanel 0`。行为判据 = `GameWindow__ESCPressed.c` 末段：第三道门槛 `updateNavPanel` 为真 ⇒ `SetMainToggle(Main)`。 |
| **断言（`Editor/MainMenuScene.cs`）** | ① 原有 T28 那节：`CheckTrue(!pw.updateNavPanel, "= 0")` **就地改成** `CheckTrue(pw.updateNavPanel, "= 1")`（铁律 5：留了更正注释）。<br>② 末尾「第 6 颗事件钮」那节在 `menu.SelectedNav == 5` 之后新增一组（见 §二）。 |
| **还差什么** | 见 §四·1：**`ParamsCloseOnEsc` 仍取 Two Sides 的 `0`**（基座那份是 `1`）—— 这是**另一条口径**、派单只裁了 `updateNavPanel` ⇒ 本代理没动。⚠️ 因此**真机上从活动承载窗按 ESC 今天仍然什么都不做**（会出声：`closeOnEsc = false`）；断言里用「夹具把 `closeOnEsc` 置 true」隔离第三道门槛（见 §二 的说明）。**要不要把 `closeOnEsc` 也对齐成 1，需要主对话/用户裁**。 |

### 3. `T16` `D10`（Toast 根高）——**只做探针**

| 项 | 内容 |
|---|---|
| **做了什么** | 在 `Editor/ShellScene.cs` 的 N20/N21/N23 Toast 那一节（`★ N23：单行根高 …` 那条断言之后，同一 `if (n >= 2)` 作用域里）加了一个 **`[D10PROBE]` 探针块**：<br>· `Debug.Log("[D10PROBE] 我们这一侧：一行 {h0:F3} px ／ 多行 {h1:F3} px（Toast 根 sizeDelta.y × 108）")`<br>· `float origRootH = 0f;   // 原版真机读数：留空（0 = 未采集）`<br>· `if (origRootH <= 0f) Debug.LogWarning("[D10PROBE] 原版真机 rect.height 未采集（读数留空）… D10 仍为 ◐ …")` |
| **⛔ 没做什么** | 没有写任何断言、没有把 `108.8638 / 86.7019` 或任何「大概是」的数字填进 `origRootH`；`ToastNotifications.cs` 的根高算法**一个字没动**（且它在白名单外）。 |
| **还差什么** | **原版读数只能在原版 player 上收**（本代理不跑 Unity/player）。探针方案（沿用 `资料/普查产出_1023c/界面线_清点报告.md:85`，这里写成可直接照做的一步）：<br>① 起 `d:/2/unity_run_ref`（普通启动必须先开 Steam）→ 主菜单；<br>② 用 SceneJumpShot 的 dump 找 `Toast Notification Controller/Holder` 下那一条 `Toast` 根的 `RectTransform`；<br>③ 读 `sizeDelta.y`（= `rect.height`，世界单位，×108 得设计 px）；<br>④ **一行 / 多行两种文案各触发一次**（原版 `Toast` 的 Text 折行 = 1，两条推导打架的焦点就在这里：`N23` 静态常量 108.8638 vs 文本驱动高）。<br>⚠️ `d:/2/Warpforge_tools/scenejumpshot/SceneJumpShot.cs` 属 `d:/2` 档案库 —— 改它要用户确认（本代理没碰）。<br>读数回来后：`ToastNotifications.cs` 的 `ToastRootHeight` 才有判据可对；本处 `origRootH` 填真值后那条 `LogWarning` 会自动闭嘴。 |

---

## 二、断言清单（含灭自证）

### 2·1 `Editor/ShellScene.cs`（W62 那节，共 7 条）

| # | 断什么 | 灭自证 / 改坏法 |
|---|---|---|
| 1 | 基类有 **virtual** `BarShadowTint` | —— |
| 2 | 基类有 **virtual** `BarShadowTop` | —— |
| 3 | 收藏窗临时补丁 `RebuildTabBarShadow` 已删（一处生产者） | 加回来 ⇒ 红 |
| 4 | 收藏窗 tint = `(0,0,0,0.475)` | 不给 tint ⇒ 浅白带 ⇒ 红 |
| 5 | 收藏窗顶边 = 158.64 | —— |
| 6 | **奖励窗（不覆写）tint = `(0,0,0,0.4745098)`** | 🧨 基类默认改回 `null` ⇒ 白带 ⇒ 红 |
| 7 | **商店窗（不覆写）tint = `(0,0,0,0.4745098)`** | 🧨 只在收藏窗那一族给 tint（= 子类打补丁）⇒ 红 |

（另：奖励窗顶边 70.94 那条保留，未改。）

### 2·2 `Editor/MainMenuScene.cs`（新增 9 条）

| # | 断什么 | 类型 |
|---|---|---|
| 1 | `ParentEventWindow.ParamsUpdateNavPanel == true`（原版基座 MB 实读 = 1） | 参数 |
| 2 | `evt.updateNavPanel == true`（`Create()` 真写进实例） | 参数 |
| 3 | `PointerLayer.InputEnabled`（ESC 第一道门槛前提） | 前提 |
| 4 | `evt.ESCPressed()` 返回 true（第三道门槛不拦关窗） | 前提 |
| 5 | **`menu.SelectedNav == 0`（`Main`）**：ESC 关活动窗 ⇒ 灯回 Main | ★★ 主断言 |
| 6 | 灭自证前提：另开一扇活动窗非 null | 前提 |
| 7 | 灭自证前提：`menu.SelectNav(5)` 后 `SelectedNav == 5` | 前提 |
| 8 | 灭自证前提：`evt2.ESCPressed()`（`closeOnEsc` 夹具 true）返回 true | 前提 |
| 9 | **灭自证：`evt2.updateNavPanel = false` ⇒ ESC 关窗后 `SelectedNav` 仍是 5（停在 `Event`）** | ★★ 灭自证 |

> 🔑 **夹具说明（断言注释里写死了）**：本条验的是 `GameWindow.ESCPressed` 的**第三道门槛** `updateNavPanel`。
> 本窗 `closeOnEsc` 仍取 Two Sides 那份的 `0` ⇒ 测试里显式 **`evt.closeOnEsc = true`**（夹具），
> **只让 `updateNavPanel` 这一个变量动**。灭自证同理：同一扇窗、同一夹具，只翻 `updateNavPanel` 一格。
> ✅ 灭自证的牙：把 `SetNavToggle(Main)` **无条件塞进 `Close()`** ⇒ 第 9 条红（灯会被拨回 0）。

### 2·3 `T16` / `D10`

**0 条断言**（有意）：只有 `[D10PROBE]` 的 1 条 `Log` + 1 条 `LogWarning`。原版读数没回来之前不该有牙。

---

## 三、主对话要跑什么

| 顺序 | 命令 | 为什么 |
|---|---|---|
| 1 | `ShellScene.Run` | W62 那节 7 条（tint 并基类）· D10 探针（`N23` 那一节）· 以及既有的 toast/黑场/窗族断言 |
| 2 | `MainMenuScene.Run` | T28 窗参改口径那 1 条 + 末尾新增 9 条（ESC ⇒ 灯回 `Main` 与灭自证） |
| 3 | 真机：`MainMenu` 起 player → 点左竖第 6 颗 `EVENT` → 活动窗里按 **ESC** | ① 看灯是否回 `HOME`/`Main`（本条的行为验收；⚠️ 见 §四·1：`closeOnEsc = 0` ⇒ **今天真机上 ESC 不会关这扇窗**，会打一条 `closeOnEsc = false` 的日志）<br>② 收藏 / 奖励 / 商店三扇左栏左缘那条**白带是否变成暗色**（原版 `(0,0,0,0.4745)`） |
| 4 | 原版 player（可选，为 `D10`）：按 §一·3 的四步收 `Toast` 根 `rect.height`（一行 / 多行） | 收回来才能把 `D10` 从 `◐` 转 `✓` |

> ⚠️ 两个宿主都会**重写自己那份场景**（`Shell.unity` / `MainMenu.unity`）—— 跑之前按 `CLAUDE.md` §二 留副本 / 跑完 `git diff --numstat` 对一眼。

---

## 四、没查清 / 判不准 / 顺手发现（都**没改**，白名单外或需裁定）

### 1. 🔴 `ParentEventWindow.ParamsCloseOnEsc = false`，而基座 prefab 是 `1`（**要裁定**）

- 现读：基座 `Parent Event Window`（MB `6349037533193994564`）= `closeOnESC = 1`；`Two Sides Event Window`（MB `-1838579764277826042`）= `0`。
- 我们这扇 = 两份 prefab 的组合（外壳取基座、两页取 Two Sides，见文件头），目前 `closeOnEsc` / `placement` 取 Two Sides。
- 后果：**真机上在活动承载窗按 ESC 什么都不做**（`GameWindow.ESCPressed` 第二道门槛直接 return，打一条日志）。
  活动代理 §四·4 那条「从活动窗按 ESC 关回主菜单时灯停在 `Event`」的描述，**在 `closeOnEsc = 0` 的前提下不可能发生** ——
  它是**静态推演**（只看了 `updateNavPanel`），不是实况。⚠️ 这一条如实记，⛔ 别当成已复现的缺陷。
- 本代理只做了派单裁定的那一半（`updateNavPanel = 1`），**`closeOnEsc` 没动**。若要完全照基座，应一并对齐成 `true`
  （判据同样是 MB 现读）；但那要**用户 / 主对话点头**（会真的改变 ESC 行为）。

### 2. `CollectionWindow.BarShadowTint` 覆写已与基类等价（多余，但白名单外）

`Shell/CollectionWindow.cs:143` 的 `(0,0,0,0.475f)` 与基类新默认 `(0,0,0,0.4745098f)` 是同一原版字段的两种写法（差 0.0005）。
删掉那行覆写 = 观感逐值不变、判据少一处转发。本代理没动（文件不在白名单）。

### 3. `D10` 探针只能报「我们这一侧」的读数

`ToastNotifications.cs`（根高的生产实现在那里）**不在本代理白名单** ⇒ 探针落在 `Editor/ShellScene.cs`。
若希望**运行时**（player 里）也能打这个读数，需要在 `ToastNotifications.ShowInternal` / `ApplyMeasuredHeight` 里加一行
`Debug.Log("[D10PROBE] …")`（或挂 `WF_TOASTPROBE` 环境变量闸，先例 → `MenuScroll` 的 `[SCROLLPROBE]`）—— **一行活，但属别人的文件**。

### 4. 顺手核对（无需动）

- `Editor/RewardsScene.cs:1043` / `Editor/ShopScene.cs:1293` 只断 `Tab Buttons/Shadow` 的**矩形**（167.18..214.81 / 70.94..1080），
  **没有断色** ⇒ 本次改色不影响它们；商店 / 奖励的矩形判据照旧成立。
- 继承面确认只有四扇：`RewardsWindow` / `ShopWindow` / `CollectionWindow` / `SocialWindow`（`grep ': MainMenuSubmenuWindow'`）。
  社交窗 `BarShadowW = 0` ⇒ **不建** `Shadow` ⇒ 基类默认改色对它零影响。

---

## 五、一句话

**三件都落了**：基类 `BarShadowTint` 默认改原版 `(0,0,0,0.4745098)`（奖励/商店不再白带）· `ParentEventWindow.ParamsUpdateNavPanel = true` + 9 条断言（含 1 条灭自证）· `D10` 加了留空+出声的 `[D10PROBE]` 探针。
**typecheck 0/0**。**断言未实跑** ⇒ 主对话跑 `ShellScene.Run` + `MainMenuScene.Run` + 真机 ESC / 左栏白带。
**两个待裁**：`ParamsCloseOnEsc`（基座 1 / 我们 0）· `CollectionWindow` 那行已多余的覆写。
