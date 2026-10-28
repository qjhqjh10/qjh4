# B89-R4b · `战斗界面_HUD` + `特效动画` 执行报告（转岗后第一轮）

> **执行**：2026-10-28 · `B89-R4b`（查判据代理 `R4` 转岗为执行代理）
> **授权**：调度台 2026-10-28 转岗指令（只做 4 组文件此刻空着的 5 行 + 713；P4/P5 不做）
> **动过的文件（4 个，均在本轮白名单内）**：
> `CardPresentation/Hand/CardInteraction.cs` · `CardPresentation/Net/NetRuntime.cs` ·
> `CardPresentation/Core/CardFeel.cs` · `CardPresentation/Board/ArenaSlots.cs`
> ⛔ **未动**：`BattleDriver.cs` · `RuleEngine/Core/CardDef.cs` · `RuleEngine/Data/CardDatabase.cs` · `RuleEngine/Core/RuleCore.cs` · git · 账主表 · `d:/2`
> **产出**：本报告 + `账/_parts/B89_R4b.tsv`（**16 行**：更新 15 + 新增 1；`--dry` 已验「更新 15 · 新增 1 · 无变化 0」）
> **上一步的施工单**：`判据/B89_R4_HUD与特效施工单.md`（本报告按它执行）

---

## §0 先读这一条：**行数守恒**（避免账漂移）

`账/*.tsv` 按 `文件:行号` 引用这 4 个文件（`CardInteraction` 8 处、`NetRuntime` 5 处、`CardFeel` 15 处、`ArenaSlots` 2 处，最高引到 `CardFeel.cs:3814` / `CardInteraction.cs:1953`）。所以：

| 文件 | 动法 | 行数 |
|---|---|---|
| `CardInteraction.cs` | 349 / 650 / 813 **就地改**（1→1 行）+ 类尾**追加**说明块 | 2163 → 2193（**只在 1953 之后增行** ⇒ 引用零漂移） |
| `NetRuntime.cs` | `Update` 里插 **1 行**（`:205`）+ 类尾追加 | 247 → 390（插入点 205 **在所有引用 25/43/51/81/98/136/181/190 之后**） |
| `CardFeel.cs` | 2921 / 2927 **就地改**（1→1） | **4188 不变** |
| `ArenaSlots.cs` | 类尾追加 | 222 → 259（引用 92/98 不受影响） |

**已复核**：上述 11 个「被账引用」的行号，改完仍是原来那个符号（脚本逐行打出来对过）。

---

## §1 逐条

### 1·695 `BattleManager.CanMoveHandCard()` —— ✅ 已做（C → A）

| 项 | 内容 |
|---|---|
| 原版判据 | ILSpy `BattleManager.cs:46525-46542`：`if (playerHand.animFlag) return false; return UIstate−1 == 0;`；唯一调用点 `CardScript.cs:10313`（`OnTouchDrag`）。`animFlag` 写点 = `PlayerHand.cs:858 / 950 / 1302 / 1457` |
| 改动 | `Hand/CardInteraction.cs:349` `bool DragBlocked()` → `ReplayDragBlocked() \|\| DragOnlyBlocked() \|\| IsDoingAnim`（1→1 行）。另两处**日志措辞**就地补了第三种原因：`:650`（`AbortDrag` 的原因串）· `:813`（「拖不起来」那条 print）—— 否则闸由第三条触发时日志会**指错原因** |
| 为什么用 `IsDoingAnim` | 它就是本类 `:432 _animGate` / `:436 IsDoingAnim`（`Relayout` 开、`CardTween.RelayoutDuration` 到点关；那个时长 = 原版 `timeToPositionCard` 0.2 s）⇒ 与 `animFlag` 同义 |
| 改前 / 改后 | 改前：重排那 0.2 s 里手牌仍可被抓（`DragBlocked` 只看回放 + UIstate）。改后：这 0.2 s 内**抓不起来**（松手也不再变「拖出去打」） |
| **观测口** | ① `IsDoingAnimForTest`（`:439`）与 `BattleStateBlockedNow`（`:299`，走 `DragBlocked`）可以成对断言：显式 `Relayout` 开闸期间 `BattleStateBlockedNow == true`、闸关后 `false`。② ⚠️ **批处理里第三条恒假** —— `:1601` 那道 `if (animGate && hand != null && hand.animateRelayout)` 在批处理档（`animateRelayout == false`）**不开闸** ⇒ **批处理行为与改前逐字相同**；要观测必须显式开 `Relayout` 或真 Play |
| 🔴 附带订正 | 账 `特效动画:497` 的备注写「原版 `PlayerHand.IsDoingAnim` 全量 0 调用、`animFlag` 0 写点」——**不成立**：`IsDoingAnim()` 那个**包装**确实 0 调用者，但 `animFlag` 字段四处写、读点在 `BattleManager.cs:46532`（就在 `CanMoveHandCard` 里，**不经过**包装）。⛔ 别照那句把第三条删回去 |

### 1·761 `CardScript.OnTouchUpAsButton()`（敌方手牌 Sabotage 守卫）—— ⛔ **拒做 + 反证（零改动）**

| 项 | 内容 |
|---|---|
| 原版判据 | `decomp_full/CardScript__OnTouchUpAsButton.c:31-36`：非我方手牌只有 `spellType == 0xE6 (Sabotage)` 才可点开。落点谓词 `CardHighlight.EnemyHandCardClickable(isPlayer, hasRawCard, sabotageKind)`（`CardHighlight.cs:836`）**已建** |
| 账给的接线口 | `Hand/CardInteraction` 的轻点支（`ApplyShowInHand` 旁边） |
| **反证** | ① `CardInteraction` 只装**我方**手牌 —— 唯一喂料口 `SetCards(...)` 只被 `BattleDriver.cs:27375` 以 `_handViews` 调用（`CardBaseDemo` / `HandAssertE8` 是夹具）；② 敌方手牌在场上**只有卡背 `ImageQuad`**（`_foeHandViews`），**没有 `CardView`、没有 `CardInteraction`**；③ 「亮出敌方手牌」走的是**居中展示**（`BattleDriver.cs:21399 / 21941` 两处注释逐句写明），不是「手牌位上翻面」。⇒ 在 `CardInteraction` 里加一句 `EnemyHandCardClickable(true, …)` **恒为真** = **纯死代码**（我方恒 `isPlayer`） |
| 结论 | **零改动**，状态留 **C**，账里已追加反证。真要接：得先有「敌方手牌翻面成可点 `CardView`」这条**新链路** —— 那是 `BattleDriver` 的活（E5），且要新立一条账，⛔ 不能塞进 761 |

### 1·805 `ShowStatusWhenConnecting.GetConnectingDots()` —— ✅ 已做（C → A）

| 项 | 内容 |
|---|---|
| 原版判据 | `decomp_full/ShowStatusWhenConnecting__GetConnectingDots.c:1-38`：`dots = floor(Mathf.Repeat(t × DAT_1834b2e8c, DAT_1834b2df0))`，逐次 `String.Concat(base, ".")` |
| 🔴 本轮新读出的常量 | `工具/read_literal.py`（4B float 档，**先 `export PYTHONIOENCODING=utf-8`**）：`0x1834b2e8c = 3` · `0x1834b2df0 = 4` ⇒ **`floor(Repeat(t×3, 4))`** = 0/1/2/3 循环、**每 1/3 秒加一个点**。⚠️ 账里原写「每秒加一个点」**不准**，已就地订正 |
| 改动 | `Net/NetRuntime.cs:283 ConnectingDotsForTest(float)` · `:289 ConnectingBannerTextForTest(float)`（基串 + n 个 `.`）· `:328 TickConnectingBannerForTest(NetState, float)` · `:205` 在 `Update()` 里插 `TickConnectingBanner();` |
| 改前 / 改后 | 改前：联机只在**掉线/重连**时弹一次性窗（`Notice` / `UpdateReconnectPopup`），**没有「连接中」的常驻横幅**。改后：`NetState.Connecting` 期间屏幕中央常驻一块横幅，点数按原版节奏变 |
| **观测口** | `ConnectingDotsForTest(0)=0 · (0.34)=1 · (0.67)=2 · (1.01)=3 · (1.34)=0`（±1 边界）· `ConnectingBannerTextForTest(0)` = 基串、`(0.5)` = 基串 + 1 点 · `ConnectingBannerVisibleForTest` |
| 🔶 自设计（已打标） | **基串**：原版是**托管字符串字面量** `DAT_1842b80e0`（`read_literal` 只能读 `_DAT_` 数值，读不到托管串）⇒ 取官方本地化里**唯一**那条连接态键 `MenuLogin/Intro/Connecting`（en「Updating the data-slate…」/ zh「更新数据板…」，10,854 键现读）。**触发状态集合**：原版那个组件的启用源（场景里那个 `enabled`）没读到 ⇒ 按方法名字面语义只对 `Connecting` 亮（`Handshaking` 要不要也亮**没有判据**，选「不亮」） |

### 1·806 `ShowStatusWhenConnecting.OnGUI()` —— ✅ 已做（C → A）

| 项 | 内容 |
|---|---|
| 原版判据 | `decomp_full/ShowStatusWhenConnecting__OnGUI.c:1-80`：`GUILayout.BeginArea(rect, …)`，`rect = ((Screen.width − DAT_1834b3270) × 0.5, (Screen.height − DAT_1834b2e08) × 0.5, 400, 100)` |
| 🔴 本轮新读出的常量 | `0x1834b3270 = 400` · `0x1834b2e08 = 100`（同一次 `read_literal` 跑出来的） |
| 改动 | `NetRuntime.cs:306 ConnectingBannerRectForTest(w, h)` = `((w−400)×0.5, (h−100)×0.5, 400, 100)`（**GUI 口径：y 向下**）；真渲染 = `:334 TickConnectingBanner` → `:357 EnsureBanner`：**自己的 `ScreenSpaceOverlay` 画布**（`sortingOrder = 30000`）+ `CanvasScaler(1920×1080 / Expand)` + `Label.CreateUgui`（复用工程现成的 uGUI 文字件，⛔ 不另写建法）。宿主 `DontDestroyOnLoad` ⇒ 跨场景活着（与原版 `OnGUI` 常驻同效） |
| 改前 / 改后 | 改前：无横幅。改后：400×100 居中常驻、跨场景、压在所有窗口之上 |
| **观测口** | `ConnectingBannerRectForTest(1920,1080) == (760, 490, 400, 100)`；`(1280,720) == (440, 310, 400, 100)` |
| 🔶 自设计（已打标） | 字号 28 / 颜色白 / 文字居中（原版是 IMGUI **默认 skin**，无像素判据）；`sortingOrder = 30000`（原版 IMGUI 在最上层，具体值无判据） |
| ⚠️ 未验 | **本波禁跑 Unity** ⇒ 真渲染**没跑过**。批处理里没有帧循环，`Update` 不跑 ⇒ 只用 `TickConnectingBannerForTest` 推状态断言 |

### 1·802 `RemnantBody.DoDestroyByAttack()` —— ✅ 已做（B → A）

| 项 | 内容 |
|---|---|
| 原版判据 | ⚠️ **2026-10-28 订正（B89_RD，铁律 5）：原来这里写「`GetDeathParticlePosition` 只填 x/z，y 恒 0」—— 不成立。** 现读 `decomp_full/RemnantNecrons__GetDeathParticlePosition.c` / `RemnantAeldari__GetDeathParticlePosition.c`：两条都是 `UnityEngine_Transform__get_position` 之后 **`*param_1 = *puVar2;` + `*(undefined4 *)(param_1 + 1) = uVar1;`** = 把 **12 字节（x/y/z 三格）整份拷回**；`cpp2il/out_cpp2il_dev_ilspy/RemnantBody.cs:53-84` 的 `DoDestroyByAttack` 也是 `Vector3 deathParticlePosition = GetDeathParticlePosition();` → `Instantiate(particleDeath, position, rotation)`，三格全用。**原版没有任何一处把 y 置 0。错因 = 把「函数只有一个返回值」读成了「只填两格」。** |
| 改动 | ⚠️ **2026-10-28 订正：该行已删。** R4b 曾在 `Core/CardFeel.cs:2921` / `:2927` 同行补 `pos.y = 0f;`；现按上面的订正把两处赋值**删掉**（仍 1→1 行、`文件行数 4188` 不变），落点恢复成 `root.transform.position` / `node.position`（整份 position）。⇒ `BattleScene.Run`「落点 = `Idle/Spirit Stone Idle` 的世界坐标」与 `AnimFXCheck.Run`「残骸粒子的落点 = 残骸体根」两条断言转绿 |
| 改前 / 改后 | 改前（R4b 之后）：粒子 y 恒 0。改后（本笔）：**取残骸根 / idle 锚的整份世界坐标**（与原版同） |
| **观测口** | `SpawnRemnantDestroy` 生成件的 `transform.position` 与残骸根 / `Idle/Spirit Stone Idle` 的**世界坐标**逐格相等（容差 1e-3）；R4b 原来那条「`position.y == 0f`」的观测口**作废**（它钉的是错的判据，⛔ 别再加回来） |

### 1·713 `BattleManager.GetUnitSizeRatio(bool)` —— ⚠️ 部分（**仍 C**，差接线）

| 项 | 内容 |
|---|---|
| 🔴 判据订正 | `GetUnitSizeRatio` 在 `decomp_full` / `out_cpp2il_dev_ilspy` / `out_il2csharp` **三源都只有定义、0 调用者 = 死方法**。真消费者 = `BattleManager.GetUnitSizeInPlay(cardType, isPlayer)` 的 **else 支**：`Vector3.one × minionInPlayScale(+0x68) × (isPlayer ? 1.0 : enemyUnitsScaleRatio(+0x74))`（`decomp_full/BattleManager__GetUnitSizeInPlay.c:1-…` 全文亲读）。`0.86` 出处 = 资产真值 `assetripper_out/ExportedProject/Assets/MonoBehaviour/GlobalVariables.asset:38`；⚠️ `VarsGlobal` 的 **C# 默认 `0.93` 是死值** |
| 改动 | `Board/ArenaSlots.cs:245 MinionInPlayScale = 0.75f` · `:249 EnemyUnitsScaleRatio = 0.86f` · `:253 InPlayScale(bool enemy)` = `0.75 × (enemy ? 0.86 : 1)` |
| **仍差（所以留 C）** | 真消费者 `BattleDriver.ArenaCardScale` 的 `else` 支（按 `u.Card.Type` 分派、非部队/非督军）**还没改成调 `InPlayScale(enemy)`** —— 那是 `BattleDriver.cs`，**本波归 E5** ⇒ **待接点**（见 §5） |
| **观测口** | 算式：`InPlayScale(false) == 0.75f`、`InPlayScale(true) == 0.645f`（±1e-4）；🧨 用 `0.93` ⇒ 红。⚠️ **今天无画面观测面**：`PlayerState.Board` 是 `UnitState[]`，而 `defence` 39 张 / `enchantment` 2 张**还没进 Board** ⇒ 接上也不会有画面差 |

### 1·497（复核，未改代码，已升 A）

`BattleDriver` 里**四件全在**：`:16687 PlayPendingBerzerkTriggerAnim()` · `:16689 ShowPendingBerzerkPointer()` · `:35550 PendingBerzerkPointerSeconds = 2.0f` + `:35597 _berzerkPointerOffAt = _clock + 2.0f` · `:35621 TickPendingBerzerkPointer` · `:35633 …ForTest`。⇒ 「本仓没有这一跳」不成立，**升 A**。真残差两条 = **新账**（见 §5）。

---

## §2 typecheck 读数

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

命令：`TMPDIR=/tmp/wf_B89R4b bash d:/4/Unity/工具/typecheck.sh`，共跑 **4 遍**：
1. 首轮 0/0；
2. 补完两处日志措辞后 0/0；
3. 收尾那一遍 **编辑器 50 错** —— **全部集中在 `Assets/CardPresentation/Editor/GameStaticDataCheck.cs`**（`CS0103: 当前上下文中不存在名称"GameStaticData"`）。🔴 **那是别人的活**：该文件 `git status` 显示 **`??` 未跟踪**、mtime `06:54:19`（就在我跑的那一刻被人刚建出来、还在写）。按 `CLAUDE.md` 铁律 13·3：「错误全部集中在某个不是你负责的文件上 ⇒ 那不是你的问题」⇒ **我没碰它**；
4. 隔一会儿重跑 ⇒ 回到 **0/0**（对方写完了）。
⇒ 最终读数以第 4 遍为准：**运行时 0 / 编辑器 0**。

---

## §3 顺手发现（⛔ 都没改，交调度台分流）

1. **🔴 `read_literal.py` 必须先 `export PYTHONIOENCODING=utf-8`**：默认 stdout 是 GBK，`✅` 打不出来 ⇒ `UnicodeEncodeError`（本轮实撞）。请进坑表。
2. **🔴 `Label.CreateUgui` 是建 uGUI 文字的正路**（`Battle/Label.cs:171`，单位 = 设计 px、y 向上、自带 `RectTransform`）—— 以后任何「在非壳上下文里加一块常驻 UI」都该复用它，⛔ 别自己 `new GameObject + TextMeshProUGUI`。
3. **`CardInteraction` 的 `DragBlocked()` 一共 6 个调用点**（`:299/:349/:649/:703/:807/:883/:2004`），加闸会**同时影响**「整帧收回拖拽」（`:649 AbortDrag`）——生产上重排只在松手后起、够不到，但这是**比原版严**的一处，账里已如实写。若哪天真机看到「拖到一半被收回」，来这儿查。
4. **`Label` 的竖排/字号都按「字墨」定位**（见 `Label.cs` 头注 A712）⇒ 我用的 28 px 是 `SetFontSize` 语义，**不是 em**。以后改字号别按 0.72 折。
5. **账里 `特效动画:497` 的「`animFlag` 0 写点」是错的**（同 §1·695 那条订正）—— 它已经被复制到别处了吗？我只在 497 那一行见过；建议调度台 grep「0 写点」确认没有第二份。
6. **`ShowStatusWhenConnecting` 的基串 `DAT_1842b80e0` 是托管字符串**，`read_literal.py` 从原理上读不到（它只走 PE 节的数值字面量）。要真读，得读 `GameAssembly.dll` 的 il2cpp 元数据字符串表 —— 本轮**没做**（成本高、收益低：官方本地化里那条键与登录界面同源）。

---

## §4 没查清（⛔ 没拿猜测填空）

| 编号 | 没查清 | 还差什么 |
|---|---|---|
| U1 | **`ShowStatusWhenConnecting` 基串的字面量** | 需读 il2cpp 元数据串表（非 `read_literal` 能力范围）。当前用 `MenuLogin/Intro/Connecting` 顶（🔶） |
| U2 | **原版那个组件「什么时候 enabled」** | 场景/prefab 里控制它的那段没读到 ⇒ 只对 `Connecting` 亮（🔶），`Handshaking` 待遇未知 |
| U3 | **`PressedBtn` 节点在不在本地**（账 684/687） | 账的证据是**按名字搜**（`CLAUDE.md` 明说不算否定证据）⇒ 要按 `m_Script` pathID → monoscripts `m_ClassName` 反查重判。**未重查前 ⛔ 不判 D** |
| U4 | **`targetedSpellAnim` 全池有几张非空**（账 498 的前置） | 要逐卡读 1,351 个 SO 的 PPtr ⇒ 建议派只读代理出一张 `卡 id → PPtr/GUID` 表 |
| U5 | **`VfxMap` 的 `AttackHitBig` prefab 本地有没有**（账 516 的 ②） | 按组件/sprite 反查（⛔ 别按名字搜就下结论） |
| U6 | **`BattleCardUI` 静态块里 `PlayInvokeAnimation` 的 clip 名**（账 507） | VA 反汇编 + `.rdata` 读串 |
| U7 | **唯一还留着的 D 候选 = 490**（详见 §4·D） | 见下 |

### §4·D 关于 `特效动画:490`（`RemoveVoiceLinesControllerListeners`）

调度台不批 D，理由成立（「我们这边没有入口」≠「原版本身没有」）。**账我一行没动**（保持 C），下面按指令写清缺什么、谁能提供：

- **缺的入口**：原版 `VoiceLinesController`（`BattleManager +0xb0`）上挂着 `OnPopUpShow` / `OnPopUpHide` 两个委托，`RemoveVoiceLinesControllerListeners` 就是把它们摘掉（`decomp_full/BattleManager__RemoveVoiceLinesControllerListeners.c:10-23`）。
- **我们缺的是「语音 × 弹窗」这条订阅链**：现在语音是**按需** `Has` / `Play`（没有常驻订阅者），所以没有「摘监听器」这个动作。
- **谁能提供**：要补齐，得先让语音侧订阅**窗口系统**的「弹窗开/关」事件（`WindowsManager` 已有窗口开关，缺的是**发布这两个事件** + 语音侧订阅）。落点在 `Shell/WindowsManager.cs`（弹窗开关广播）与 `Core/VoiceLines`（订阅/退订）——**都在本波白名单外**。
- **建议下一波**：先定「语音要不要跟着弹窗静音/停播」这条**行为判据**（原版那种「弹窗期间停语音」的实际效果，本机没读到），再决定建不建这条链。⛔ 判据没定之前建订阅链 = 自己发明行为。

---

## §5 交接（含要立的新账）

### 5·1 已写进账补丁的新账（1 条）

| 域 | 原版类 | 方法 | 状态 | 等级 | 轮次 |
|---|---|---|---|---|---|
| 特效动画 | `TutorialPointer` | `StartPointer(ClickAndDragKind, Vector3, Vector3, int)` | **C** | P2 | B89-R4b |

> 内容 = **497 归 A 之后剩的两条真偏离**（按调度台指令另立、⛔ 没塞回 497）：
> ① 指针是**双点**（起点 + 拖拽终点），我们 `TutorialOverlay.SetPointer(bool, Vector3)` 是**单点口**（`TutorialOverlay.cs:1074`）⇒ 终点画不出来；
> ② 原版那颗 `TutorialPointer` **恒在**（非教程局也画），我们 `EnsureTutorialOverlay()` 只在 `Ctx.Tutorial != null` 时建。
> ⚠️ 形参类型本轮**没逐字读出**（只知道是「双点 + 时长」四参）—— 下一波接手时先按 `decomp_full/TutorialPointer__StartPointer.c:1-68` 补签名。

### 5·2 待接点（都不在本波白名单，**下一波按符号接**）

| 账行 | 在哪接（符号名） | 接什么 | 依赖 |
|---|---|---|---|
| **713** | `BattleDriver.ArenaCardScale` 的 `else` 支（现读 `:22051`） | 非部队/非督军的在场卡改成 `ArenaSlots.InPlayScale(enemy)` | `ArenaSlots.InPlayScale` 已建好；⚠️ 今天无画面观测面（`defence`/`enchantment` 还没进 Board） |
| **741** | `BattleDriver.Update` 的教程支（`Ctx.Tutorial != null`） | 补一条 `Debug.LogWarning`（B08「等玩家用主动技」，原版 `BattleManager__Update.c` B08 段唯一一条日志；**零局面影响**，调度台裁定「零局面也是真差异、要做」⇒ ⛔ 不判 D） | 等 E5 交回 `BattleDriver.cs` |
| **761** | 无（要先有「敌方手牌翻面成可点 `CardView`」这条新链路） | 见 §1·761 反证 | 新立账 + `BattleDriver` 的活 |
| **490** | `Shell/WindowsManager.cs`（弹窗开/关广播）+ 语音侧订阅 | 见 §4·D | 先定「语音跟不跟弹窗」的行为判据 |

### 5·3 账补丁清单（`账/_parts/B89_R4b.tsv`，16 行）

| # | 域 | 原版类.方法 | 新状态 |
|---|---|---|---|
| 1 | HUD | `BattleManager.CanDisplayHandCard(CardScript card)` | C → **A** |
| 2 | HUD | `CardScript.DisplayOffensiveCardInCenter(float, float)` | C → **A** |
| 3 | HUD | `CardScript.GetEnemyCardPlayedDisplayPosition()` | C → **A** |
| 4 | HUD | `CardScript.GetEnemyCardPlayedDisplayScale()` | C → **A** |
| 5 | 特效 | `AnimateUIMaterialProperty.SetMaterial(Material)` | C → **A** |
| 6 | 特效 | `CardScript.CanContinueAnim()` | C → **A** |
| 7 | 特效 | `CardScript.DissolveWholeCard(float)` | C → **A** |
| 8 | 特效 | `BattleManager.StopHandAnim(float)` | C → **A** |
| 9 | HUD | `BattleManager.CanMoveHandCard()` | C → **A**（本轮改码） |
| 10 | HUD | `CardScript.OnTouchUpAsButton()` | C（**拒做 + 反证**，已追加） |
| 11 | HUD | `ShowStatusWhenConnecting.GetConnectingDots()` | C → **A**（本轮改码） |
| 12 | HUD | `ShowStatusWhenConnecting.OnGUI()` | C → **A**（本轮改码） |
| 13 | HUD | `RemnantBody.DoDestroyByAttack()` | B → **A**（本轮改码） |
| 14 | HUD | `BattleManager.GetUnitSizeRatio(bool)` | C（**框架订正 + 已建 API，仍差接线**） |
| 15 | HUD | `BattleManager.Update()` | C（**待接点 741**，已追加） |
| 16 | 特效 | `TutorialPointer.StartPointer(…)` | **新增** C |

> 合并命令（**在没有任何 `.cs` 写手时**由调度台跑）：
> `python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B89_R4b`
> ⚠️ 合并完**把 `_parts/B89_R4b.tsv` 移进 `_parts/_merged_B89_R4b/`**（否则下次整目录合并会把后来手工修订盖回旧内容，零报错）。

### 5·4 下一波建议

1. **E5 交回 `BattleDriver.cs` 后**：先接 713 的 `else` 支（一处算式）与 741 的日志（一行），两条都极小。
2. **派只读代理**出 `targetedSpellAnim` 全池表（U4）与 `AttackHitBig` 反查（U5）—— 这两条卡着 498 / 516。
3. **`B89_R4_HUD与特效施工单.md` 的 P1~P6 里**，P1（761 已拒做 ⇒ 只剩已完成的 695）、P2、P3、P6 本轮**已消费完**；剩下 **P4（`CardDef` 两列）** 与 **P5（`RuleCore` 三处）** 仍归别人（E6 / E2b），本报告未触碰。

---

## §6 一句话总结

本轮**改码 6 条**（695 / 805 / 806 / 802 完成并升 A；713 建好 API 差接线；761 反证拒做），
**账证 8 条升 A**，**新立 1 条新账**（TutorialPointer 双点/恒在），
`typecheck 0/0`，**四份账补丁 dry-run 通过**（更新 15 · 新增 1），**零 `.cs` 行号漂移**。
