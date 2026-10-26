# B8_BN —— 外壳 / 主菜单 / 设置 三个宿主的红（13 条）

日期 2026-10-26 · 执行代理 `B8_BN`
输入日志（先读）：`_tmp_view/wf_ShellScene.Run.log`（2055 通过 / 6 失败）· `_tmp_view/wf_MainMenuScene.Run.log`（6 条 `✗`，无汇总行）· `_tmp_view/wf_SettingsScene.Run.log`（1561 / 1）。

> ⛔ 本代理**没跑 Unity**（三份宿主都会 `NewScene → SaveScene` 覆盖存盘）。`TMPDIR=/tmp/wf_b8bn bash d:/4/Unity/工具/typecheck.sh` = **0 / 0**。

---

## 一、逐条：定性 + 证据 + 改了什么

### ShellScene（6 条 → 一个根因 + 两个夹具坑）

#### A. A743 三态（态一 / 态三 / 态四）—— **γ 本会话回归**（共同根因）

| 断言 | 实测 | 期望 |
|---|---|---|
| 态一（压边值） | `aMaxX = 571.10`（跨度 x `318.33..571.10`） | `MsgList.x2 = 728.28` |
| 态三（节点框切到字中间） | `c3 = 429.32` | `cutX = 444.71` |
| 态四（形参赢 · 真·未裁值） | `d3 = 684.38` | `> x2 + 50 = 778.28` |

**读数自证同一根因**（三个数**逐位**=「全尺寸值绕 `x = 413.93` 缩放 ×0.5」）：
`571.10 = 413.93 + (728.28 − 413.93)×0.5` · `429.32 = 413.93 + (444.71 − 413.93)×0.5` · `684.38 = 413.93 + (954.83 − 413.93)×0.5`（`954.83` = 10-24 那一版绿日志里态四的读数）。
⇒ 被量的那段字**被整块缩到 0.5**。

**根因**：`Shell/InboxWindow.cs` 的 `StartAnimateIn()`（2026-10-25 · `N17` 新加，提交 `bb0f3d34`）在建完行之后**先把每一行摆到入场起点**：`ApplyRowAnim(i, 0f)` ⇒ `node.localScale = AnimScaleFrom`（= **0.5**）、底图 `Tint.a = 0`。
推进它的是 `Tick(dt)`（`AnimTick`），而 `Update()` 在**批处理/编辑模式里不跑** ⇒ 行**永久停在起点**。
（对照证据：10-24 21:19 的 `20261024_u1_shell.log` 同一段全绿 —— 态一 728.28 / 态三 475.50 / 态四 954.83，那时还没有 `StartAnimateIn`。）

**改了什么**（`Shell/InboxWindow.cs`，**零新增行**：就地改写第 946 行那一条 `if`）：
```csharp
if (_animPlayed || !Application.isPlaying) { for (…) ApplyRowAnim(i, 1f); return; }
```
= 非 Play 直接落到终态（`localScale = 1` / 底图 alpha 满）。
**同族口径（不是我编的）**：`Shell/CollectionWindow.cs:1337`「生产路径用 `Application.isPlaying`」· `Shell/DraftModeWindow.cs:2548` `if (!Application.isPlaying) { node.localScale = Vector3.one; return; }`。
⚠️ `_animT` 初值 `-1f` ⇒ 这条早退之后 `AnimTick` 仍恒早退，不会「半播」。
⚠️ 生产（Play）行为**一个字没变**：真跑时动画照播（0.5 → 1 + 淡入）。

#### B. `⑥ （前提）格子里有命中钮` / `⑥ （前提）排序列里也有命中钮` —— **δ 夹具少给了一个实参**

证据：`Shell/MenuDraw.cs` 的 `DeckCell` 把**整块命中钮**包在 `if (onClick != null)` 里（「点击区」那一段：`if (onClick != null) { … AddComponent<WindowButton>() … }`）。
而夹具（`Editor/ShellScene.cs:10415/10432`）原来传的 `onClick` 就是 **`null`** ⇒ 这一格**根本没有 `Hit`/`WindowButton`** ⇒ 断言必然红。
生产两个调用点**都传非空**：`Shell/CollectionWindow.cs:5286` `() => { … SelectDeck(idx); }` · `Shell/DeckSelectionPopup.cs:964` `() => UpdateInfo(pick)`。

**改了什么**：两处 `null` → `() => { }`（照生产形状）。⛔ 没改断言、没放宽。

#### C. `⑦ （前提）顶栏建出来了（TopBar.LastBuilt.Root）` —— **δ 夹具前提不成立（这场里根本没有顶栏）**

证据：`TopBar.LastBuilt` **只在 `TopBar.Build` 里赋值**（`Shell/TopBar.cs:216/223`），而 `TopBar.Build` 全仓只有两个调用点：
`Shell/MainMenuRuntime.cs:552`（`MainMenuRuntime.Build`）与 `Shell/DeckRuntime.cs:1522`（`BuildTopBar`，首句 `if (HostWindow != null) return;`）。
本宿主的 `Build()` 只造 `ShellRuntime`（`Editor/ShellScene.cs:800/823`），**全场一处都没调** ⇒ `LastBuilt` 恒 `null`。
⇒ 那条「前提」红，且下面 **5 条被 `if (bar != null …)` 静默跳过**（本会话第三次撞到「前提不成立就静默跳过」）。

**改了什么**：夹具**自己建一条**（走生产同一个口 `TopBar.Build`；`Editor/MainMenuScene.cs` 的 A374 也是另开一台实例的写法），跑完
`TopBar.LastBuilt` 还回去 + `DestroyImmediate`。**没动任何期望值**。

### MainMenuScene（6 条 → 两个根因）

#### D. 三条红点（`CampaignBadge 那一栏没登记` · `Campaign 计数 0（期望 1）` · `锻造计数 0（期望 3）`）—— **β 生产接线缺口**

证据：`BadgeWiring.RegisterDefaults()` 全仓只有两处调用 —— `Shell/ShellRuntime.cs:768`（壳）与 `Editor/RewardsScene.cs:3539`（那个宿主的自检）。
**`MainMenuRuntime.Build()` 这条生产入口没调** ⇒ 主菜单独立建场（`MainMenu.unity` 直起 / 本宿主）时 provider 表是空的 ⇒
`CampaignNotificationProvider` / `ForgeNotificationProvider` 从未登记 ⇒ `UiBadgeNotificationManager.Notifications` 里没有 `CampaignBadge` 那一栏、计数恒 0。
`Shell/BadgeNotifications.cs:405-442` 两个 provider 的口径本身是对的（Campaign 一战役一条；Forge = 全阵营 `ToCollect` 总数 = 夹具独立数出来的 3）。

**改了什么**（`Shell/MainMenuRuntime.cs:403`，**零新增行**：把调用追加在既有那一行上）：
`_root = transform; BadgeWiring.RegisterDefaults();`
幂等闸在 `RegisterDefaults` 自己身上（`if (ProviderCount > 0) return;`）⇒ 与壳那条路并存无副作用。

#### E. 三条（`…亮的那一个正是 PairIndex = 0 那一格` · `㉒④ 内容高 6381（实得 657）` · `可滚范围 5840.21（实得 116.21）`）—— **δ 夹具还原不全**（同一根因）

证据：657 = `159×4 + 21`（**4 行**）· 116.21 = `657 − 540.79` ⇒ 卡组列停在**筛选态**（6 对 + 首格 = **7 格 ⇒ 4 行**）。
而同一时刻 `pw.ListedDeckCount == 78`（**过**了就说明字段 `ArmyIndex` 已经是 −1）⇒ **列表与字段不一致**。
来路 = 2026-10-26 另一代理（`B8_AQ`）新加的那段「**点**一格阵营」夹具（`Editor/MainMenuScene.cs:3942-3983`）：
它走真 `PickArmy`（⇒ 卡组列**被按那一个阵营重建**，7 格），收尾却只**直接写回三个字段** + `ArmyScroll.OnChanged()` ——
而 `ArmyScroll.OnChanged` 只回调 `RebuildArmyCells`（`Shell/PracticeModePopup.cs:1740`：`ArmyScroll.OnChanged = () => RebuildArmyCells(filters);`），**⛔ 不重建卡组列**（建它只有 `RebuildDeckRows`，生产入口 = `PickArmy → SelectDefaultDeck` / `PickDeck` / `PickPair`）。
⇒ 后 5 条全都被这个「列表停在筛选态 + 选中态留在『你的卡组』那一格」带崩（`Highlight 恰好亮 1 个` 那条因为按 `DeckRows.Count` 动态比，仍绿）。

**改了什么**（`Editor/MainMenuScene.cs:3983-3991`，**+9 行**）：还原字段之后补一句 `pw.RebuildDeckListForTest();`（现成的自检口 = `RebuildDeckRows(_deckHolder)`，与生产同一条路）。⛔ 没动任何期望值。

### SettingsScene（1 条）

#### F. `…真的写进了两条 mixer 参数（VolumeFX 与 VolumeJingles 同值、都是 −8.6dB）` —— **δ 前提在本环境不成立（编辑器里 mixer 读不回来）**

证据：实得 `Music=1.00(0.0dB) · FX=0.37(0.0dB) · Jingles(0.0dB) · Voices=1.00(0.0dB)` —— 四路 **dB 全是 0.0**（不是 `(**)`，说明 `GetFloat` 成功、只是读到 0）。
`Core/PlayerBoot.cs:283-285` 的注释里有**实测结论**：「`AudioMixer.SetFloat` 在**编辑器（非 Play）里是空操作**（实测『设 −20 → 读回 0.00』，而音频子系统本身是活的 48kHz/Stereo）⇒ **只有真包能验**」；
同一文件 `DumpAudioSummary()` 专门为此在 player 里打一行 `[PlayerBoot·音频] **读回往返**`。
本宿主是 `-executeMethod` 批处理（`Application.isPlaying == false`）⇒ **dB 值这一档在任何情况下都读不回来**。

**改了什么**（`Editor/SettingsScene.cs:3759-3779`，+13 行）：**按环境两档**断 ——
· `Application.isPlaying`（真包/Play）：原样要求 `FX=0.37(-8.6dB)` 与 `Jingles(-8.6dB)`；
· 批处理：断 `WarpforgeAudio.ParamsOk`（= `VolumeFX` / `VolumeMusic` / `VolumeVoices` / **`VolumeJingles`** 四个暴露参数名在 mixer 里都认得 —— `Dump()` 打出的是 `(0.0dB)` 而不是 `(**)`，与它一致），并把「为什么这一档只断这个」写进断言文案。
⛔ 不是放宽：**值那一档在环境允许时才成立**，同一个数由 player 那条 `[PlayerBoot·音频] **读回往返**` 盯。
⚠️ **如实记（这一档的牙口缺口）**：批处理这一档**抓不到**「删掉 `SetSoundFx` 里那句 `Write("VolumeJingles", _fx)`」。
  有牙的补法（**在 `Core/WarpforgeAudio.cs`，不在本代理白名单 ⇒ 只提请求**）：给 `Write()` 加一个 per-param 的「最后一次写入值」读口（照本仓 `MenuDraw.TextClipReapplied` 那种自检计数器的先例），批处理那一档就能断「`VolumeJingles` 收到的 == `VolumeFX` 收到的」。

---

## 二、有没有一个共同根因

**没有全局唯一根因；13 条分属 4 个独立成因**，但每个成因内部是**一个**：

| 成因 | 归属 | 条数 | 定性 |
|---|---|---|---|
| ① 行入场动画在无帧循环环境下把行**永久留在 0.5 倍**（`InboxWindow.StartAnimateIn`） | 2026-10-25 `N17` 引入 | **3**（A743 三态） | **γ 回归** |
| ② `DeckCell` 的命中钮只在 `onClick != null` 时建，夹具传 `null` | 夹具 | 2（⑥ 两条前提） | **δ** |
| ③ `TopBar.LastBuilt` 在 ShellScene 恒 null（这场不建顶栏） | 夹具 | 1（⑦ 前提，**连带 5 条被静默跳过**） | **δ** |
| ④ 主菜单这条生产入口没登记红点 provider | 生产 | 3 | **β** |
| ⑤ 阵营点击夹具只还原字段、不重建卡组列 | 夹具（今日 `B8_AQ`） | 3 | **δ** |
| ⑥ `AudioMixer.SetFloat` 在编辑器里是空操作 | 环境 | 1 | **δ** |

🔴 **「⑦ 顶栏」那一条确实是级联源**：`⑦` 那条红把**它后面 5 条**（overlay 抬高 / 还原 / 落位）全静默跳过了 —— 修改后它们才真跑。

---

## 三、改了哪些行（新增行数 + 位移）

| 文件 | 改动 | 新增行 | 位移 |
|---|---|---|---|
| `Shell/InboxWindow.cs` | `StartAnimateIn` 首条 `if` 加 `\|\| !Application.isPlaying`（就地改写） | **0** | **0** |
| `Shell/MainMenuRuntime.cs` | `_root = transform;` 那行追加 `BadgeWiring.RegisterDefaults();` | **0** | **0** |
| `Editor/MainMenuScene.cs` | 3983-3991：注释 8 行 + `pw.RebuildDeckListForTest();` | **+9** | 3982 之后 **+9** |
| `Editor/ShellScene.cs` ⑥ | 两处 `null` → `() => { }`（含注释） | +7 | 10415 之后 +6 / 10432 再 +1 |
| `Editor/ShellScene.cs` ⑦ | 夹具顶栏（`TopBar.Build` + 还原 + 销毁） | +15 | 10468 之后 +15（净 **+22**） |
| `Editor/SettingsScene.cs` | mixer 读回按环境两档 | +13 | 3759 之后 +13 |

**账锚点影响（现扫 `资料/完全复刻/账/`）**：
· `Editor/ShellScene.cs`：33 条锚点，**最大行号 < 10380** ⇒ 本次 +22 行（全在 10412 之后）**不移任何既有锚点**。
· `Editor/SettingsScene.cs`：**没有** > 3758 的锚点 ⇒ 不影响。
· `Editor/MainMenuScene.cs`：受影响的既有锚点 = **`6829`（×2）· `15834` · `17355` · `17410`（×2）**，各 **+9**
  （⚠️ 这些锚点**本来就已经被 `B8_AQ` 那段未提交的 +57 行顶偏了** ⇒ 本次只是再 +9，交给调度台在重同步点统一 `--fix`，本代理没碰主表）。
· `Shell/InboxWindow.cs` / `Shell/MainMenuRuntime.cs`：零行变化。

行尾：五份文件实测 **纯 LF / 裸 CR = 0**（二进制数出来的），没被翻。

**没写账补丁**：这 13 条不是账里的状态变更（都是「实现/夹具与断言不一致」），没有一条 A 行因为我这次改动而改档 ⇒ 按「只在真改了某条账的状态时才写」的口径，`_parts/B8_BN.tsv` **未创建**。

---

## 四、没查清的部分（如实）

1. **`⑦` 夹具是本场第一次在 `ShellScene` 里跑 `TopBar.Build`** —— 它在 `MainMenuScene` 里跑得通（A374 的另一台实例），本代理**没法跑 Unity 复验**。若它在 ShellScene 抛错，**整个宿主会挂**（风险最大的一条，优先级建议：重跑时先看 `ShellScene` 日志里 `[Shell] ⑦ （前提）顶栏建出来了` 那一行）。它读的是 `ProfileData` / `CardTierConfig` / `Wallet` / `MainMenuRuntime.CounterSpecs`（全是静态、且本宿主已在用 `DailyData`）。
2. **Settings 那一档的牙口缺口**（见 §一·F）：要真在批处理里断「`Write("VolumeJingles", …)` 没被删」，需要 `Core/WarpforgeAudio.cs` 加一个 per-param 写入口（不在白名单）⇒ 已作为请求记在此，未实现。
3. **`MainMenuScene` 的红点计数依赖 `ForgeData` / `CampaignData` 的当前状态**（夹具独立数出 3 / 1）。若这两个静态数据被别的段改过，登记之后计数会跟着数据走 —— 那是**正确**行为，但本代理没法在批处理里复验「登记完就是 3 和 1」，只能靠它们本来就被判据独立数过。
4. 没查：`InboxWindow` 那 0.5 倍除了 A743 之外**还有没有别的下游**（我把 `Editor/*.cs` 里对 Inbox 行的读数扫了一遍，只有 A743 那四处量文字网格；但 `Play` 里的截图/其他宿主的运行期视觉不在本代理可查范围）。

---

## 五、重跑单（串行；本代理不跑 Unity）

| 宿主 | 期望 |
|---|---|
| `ShellScene.Run` | **0 失败**；断言合计从 `2055/6`（2061 条）变成 **≈2070 条 / 0 失败**（⑥ 的 4 条 hover + ⑦ 的 5 条此前被静默跳过，现在真跑） |
| `MainMenuScene.Run` | **0 失败**（6 条全清；条数不变） |
| `SettingsScene.Run` | **0 失败**（1562 条 / 0；条数 +1，因为多了一个分支判断不改条数 —— 实为原来的 1 条改成 1 条） |

重跑时**重点看两处新跑起来的断言**：`ShellScene` 的 `⑦ overlay 打开 ⇒ 有一批件被抬高`（夹具顶栏是否建得出来）与 `MainMenuScene` 的 `★ 锻造红点计数`（provider 登记是否真的按数据算到 3）。
