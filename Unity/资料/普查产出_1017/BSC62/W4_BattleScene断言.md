# W4 · Editor/BattleScene.cs 断言修复（62 条红里我这一份 α/δ）

> 唯一改动文件 = `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（纯 CRLF，未翻行尾）。
> 本轮**不跑 Unity**（含 `BattleScene.Run`）；只跑秒级类型检查（0 错）。行号是**改后**的。
> 判据三份只读诊断：`BSC62/R1_面板字族.md` · `R2_量纲与字框.md` · `R3_位置层序命中卡体.md`。

## 一、逐条处理表

| 日志行号 | 判类 | 原样 | 改成什么 | 判据（搜哪一串 / 文件:行号） | 状态 |
|---|---|---|---|---|---|
| **簇 A1 · 已迁 uGUI 件仍用世界尺 `×108`** | | | | | |
| 270103–270487（`A1192` 21 条） | α | `Label.FontSizeToPx(lb1192.FontSizeMin/Max/Base)`（**静态**世界档口） | `lb1192.FontSizeToPxNow(...)`（实例·按档口），`BattleScene.cs:22797-22799` | 搜 `FontSizeToPxNow(lb1192`；`Label.cs:801-810` 的 doc 明写这族必须换本口；同文件 `RW1192` 已按 `IsUgui` 分档 | 改 |
| 128425（ChatToggle **位置**） | α | `mWant = sp.transform.position + (188.002/108, −254.062/108)`（旧世界尺 `/108`） | **px 直比**：`|mcx−188.002|<0.05 && |mcy+254.062|<0.05`（`mcx/mcy` 本就吐回 px），`BattleScene.cs:11022` | 搜 `mcx - 188.002f`；`SettingsPanel.MuteChatRectPx`（`_panel.InverseTransformPoint` ⇒ 面板 px）；同族 `:11069`「直接给设计 px」 | 改 |
| 146504 / 146516（Hug） | α | `lab0.WorldW * 108f`（已迁件又乘 108） | `unitPx0 = errBan.IsUgui ? 1f : 108f; lab0.WorldW * unitPx0`，`BattleScene.cs:15436` | 搜 `unitPx0 = errBan.IsUgui`；生产侧 `ErrorMessageBanner.cs:649` 同式 | 改（⚠️ 残值没对上，见 §五·1） |
| 129427（A921 语言名） | α | `wlbl.WorldW * 108f` | `wlbl.IsUgui ? wlbl.WorldW : wlbl.WorldW*108f`，`BattleScene.cs:11380` | 搜 `wlbl.IsUgui`；`Label.cs:9-10`「uGUI 档单位 = 设计 px」 | 改 |
| **簇 A2 · 读口与件不同路** | | | | | |
| 145751 / 145775 / 145799（探针） | α | 对照探针走 `Label.Create`（世界档，TMP 默认 `sizeDelta.x=20`） | 改 `Label.CreateUgui(HudCanvas.RootRect, …)`（与那 4 颗**同路**），`BattleScene.cs:14992` | 搜 `CreateUgui(HudCanvas.RootRect`；日志「框宽 200.0000 = 新建一条 Label 的 20.0000」 | 改 |
| **簇 A3 · 读口丢分支 / 找错根** | | | | | |
| 146432（分层） | α | 只断 `ImageQuad.RenderQueue`（uGUI 无渲染队列 ⇒ 读回哨兵 `2147483647`/−1） | **按档**：uGUI 断**兄弟序**（文字 idx > 底条 idx）/ 世界档断队列 4001·4002，`BattleScene.cs:15310` | 搜 `errBan.IsUgui` 那段③；`Label.cs:205-219`（uGUI 恒 −1）；`ErrorMessageBanner.cs:544,553` | 改 |
| 54492（重开 TurnLabel ×0） | α | `driver.GetComponentsInChildren<Transform>()` 找 HUD（**根画布是根对象** ⇒ 数不到） | 再补一次 `HudCanvas.RootRect.GetComponentsInChildren<Transform>()`，`BattleScene.cs:9455` | 搜 `HudCanvas.RootRect.GetComponentsInChildren`；`HudCanvas.cs:103`（无 `SetParent`）·`BattleDriver.cs:16342-16361` | 改 |
| 12727（AmbushExit 落点） | α | 期望用 `pBoard.SlotPosition(WarlordSlot)`（2D 行线点） | `pBoard.DropTargetWorld(BoardLayout.WarlordSlot)`（卡心投影），`BattleScene.cs:1024` | 搜 `DropTargetWorld(BoardLayout.WarlordSlot)`；`BattleDriver.cs:12351`（`PlaySignal` 已喂它）·`BoardLayout.cs:504-514` | 改 |
| **簇 C · 题设 / 名单 / 阈值过期** | | | | | |
| 145823（`HandLabel` 名单） | α | `hudNoBox = {TurnLabel,TurnClock,ResultLabel,HandLabel}` | 去掉 `HandLabel`（`A1239` 已给它补框+四格），`BattleScene.cs:14980` | 搜 `var hudNoBox`；`BattleDriver.cs:14327-14337`（`_handLabel.SetAutoFitBox(...)`） | 改 |
| 65838（提示条阈值） | α | `HintWidth < 1920*0.9`（旧自建 `HintLabel` 那套） | 门槛换**横幅自身框宽** `< 2450.1299f`，`BattleScene.cs:9743` | 搜 `2450.1299f`；`BattleDriver.cs:1044`（`TipWpx`）·`:14501-14506` | 改（⚠️ 证据强度见 §五·2） |
| 50166（缩放地板） | α | `ContentWidthPx <= availPx`（地板生效时不可能成立） | `<= max(availPx, 0.45×自然总宽)`（地板夹住时按地板宽），`BattleScene.cs:8682-8685` | 搜 `floorTotalPx`；`MultiCardDisplay.cs:213`（`MinShrink`）·`:519-530` | 改 |
| 34089 / 34101（阵亡三拍·① ） | α | `Step(DeathHoldBeforeFx + 0.02)`（=0.52，漏「抖」那段） | `Step(DeathDissolve(false) + DeathHoldBeforeFx + 0.02)`（=0.72），`BattleScene.cs:6527` | 搜 `DeathDissolve(false) + CardFeel.DeathHoldBeforeFx`；`CardFeel.cs:1357-1367`（`Append(shake)→AppendInterval(0.5)→AppendCallback`） | 改 |
| 138886（阵亡三拍·② 消散） | α | 同上 | 同上，`BattleScene.cs:13545` | 同上 | 改 |
| 136571（卡背翻面前半段） | α | `Goto(dq, sideT*0.5, true)`（**前跳不触发** `InsertCallback(0f)`） | `CardTween.Advance(sideT*0.5)` → `Check` → `CardTween.Advance(sideT*0.5+0.01)`，`BattleScene.cs:13153-13155` | 搜 `CardTween.Advance(sideT`；`CardFeel.cs:1725`（`InsertCallback(0f)`）·`CardTween.cs:76`（`ManualUpdate` 真推进） | 改 |
| 14468（两行不叠） | α | `eBot > pTop`（与它自己也在验的 218.4px 卡高 / 211px 行距**自相矛盾** ⇒ 必叠 7.4px） | 改断**行心分明**（敌行心 > 我方行心），并注明卡框本就各叠 ~7.4px，`BattleScene.cs:1614-1617` | 搜 `两行**行心**不叠`；`BattleScene.cs:138 EnemyLineY`·`:153 BoardCardHeightPx` | 改（⚠️ 见 §五·3） |
| 143383（排序层 ID 灭自证） | α | `PopUps > OverHand > Hand`（**层名哈希不按数值有序**） | 改断 `Default==0 && Blocking<0 && 其余 8 个两两不同`，`BattleScene.cs:14778-14789` | 搜 `slDistinct`；`HudCanvas.cs:462-473`（9 层哈希值） | 改 |
| 254844（设置钮找错根） | α | `Settings.transform.Find("settings_skip_tutorial")`（`Find` 不递归、钮在 `_panel`） | `Settings.PanelRect.Find(...)`，`BattleScene.cs:20719` | 搜 `PanelRect.Find("settings_skip_tutorial`；`SettingsPanel.cs:696`（`PanelRect`）·`:872`（建在 `_panel`） | 改 |
| 270043（名字块左缘） | α | `PxX(PosInDesignSpace(...).x)`（画布档又 `×108+960` ⇒ −72887） | 画布档 `designX + DesignPxW/2`（= −683.77+960 = 276.23），世界档照旧 `PxX`，`BattleScene.cs:22684-22687` | 搜 `designX985`；`LayoutSpace.cs:241`（`PxX`） | 改 |
| **簇 D · 预期红（`EW9` 预告）** | | | | | |
| 268924 / 269146（E2 命中取样） | α | 取样点 `0.45×Height`（上沿外） | `0.40f`（落在 `HitHalfH=1.42565` 内），`BattleScene.cs:22133` 等 | 搜 `0.40f * CardView.Height` / `0.40f, out e964`；`CardFan.cs:152-153`·`CardView.cs:4488-4494`。⛔ **不改实现** | 改 |
| **簇 B · pile 默认隐藏（T13 按住才亮）** | | | | | |
| 145299 / 145429（pile 文字块残差） | α | 直读 `textBounds`（节点关着 ⇒ 哨兵 4.29e9） | 读前 `driver.SetCardHoldForTest(true)` + `ApplyTutorialVisibilityForTest()`，断完还原，`BattleScene.cs:14954` | 搜 `SetCardHoldForTest(true);` 那一簇；`BattleDriver.cs:9987-9996`（`CardHoldPeek`） | 改 |
| 15710（张数文字宽 0.00） | α | `PileLabelWorldW > 1f`（隐藏 ⇒ `_tmpW`=1e-4） | 同上钉开关，`BattleScene.cs:2177` | 搜 `drv.SetCardHoldForTest(true)`；`PileLabelWorldW` doc | 改 |
| **簇 δ · 前提不成立** | | | | | |
| 45859（槽 1 卡面建出来了） | δ | 窗里只有 1 张卡 ⇒ `SlotView(1)==null` ⇒ 前提恒红 | 把该判别式（命中区**跟卡转**）**搬到 `Master of Arcana` 那节**（一定 ≥2 格），原位留指针；`BattleScene.cs:1493x`（搬入点 ~`:8520`） | 搜 `命中区**跟卡转**` / `f1rot`；`CardDisplayWindow` MoA 天赋=4 张池子 | 改 |
| 其他已顺手处理的单条 | | | | | |
| 51963（BattleMusic 切 High） | α | `CurrentLevel == LevelHigh`（`Tick` 先 `StepFade` 再判档 ⇒ 单 tick 只**起**淡变） | 读**淡变目标** `TargetVolume(High) > 0.99 && TargetVolume(Low) < 0.01`，`BattleScene.cs:9169` | 搜 `TargetVolume(BattleMusic.LevelHigh)`；`BattleMusic.cs:158-170,239-252` | 改（⚠️ 见 §五·4） |

## 二、按簇小结

- **A1（世界尺 → 按档口）**：`A1192` ×21 一处口换掉清 21 条；Hug ×2 + 语言 ×1 各按 `IsUgui` 取因子。**核心判据** = `Label.FontSizeToPxNow`（按本标签所在空间）+ uGUI 档 `WorldW` 已是设计 px。
- **A2（探针同路）**：一处改 `CreateUgui` 清 3 条。
- **A3（读口丢分支/找错根）**：分层按档（uGUI 读兄弟序）、HUD 计数补根画布、落点改 `DropTargetWorld`。
- **C（题设/名单/阈值）**：名单去掉 `HandLabel`；提示条门槛改横幅自有框宽；缩放地板改成 `≤max(可用,地板)`；阵亡步长补 `DeathDissolve`；卡背改真推进；两行改断行心；排序层改断唯一性；设置钮改 `PanelRect`；名字块按档折 px。
- **B（pile 隐藏）**：三处读前钉 `CardHoldForTest`。
- **D（E2）**：取样 `0.45→0.40`（两条）。
- **δ**：判别式从「手牌窗」搬到「MoA 窗」（前提恒成立）。

## 三、预计由别的写手解的红（⛔ 我没动）

| 日志行号 | 归属 | 谁解 |
|---|---|---|
| 18289 | 牌堆回合灯 x01（`HudPiece.Anchor01()` uGUI 支丢 `anchoredPosition`） | W2（`Battle/HudPiece.cs`） |
| 18161 | 敌方名牌左缘（`EnemyPlatePos01` 走 `ToNormalized(localPosition)`） | W3（`BattleDriver.cs`） |
| 143872 | 称号文字中心（`TitlePosPx` 未照 `PlateTextPosPx` 改 `anchorMin`） | W3（`BattleDriver.cs`） |
| 15662 | SDF z > 卡背 z（读回端） | W2（`HudPiece`）·按简报「不由我改断言」 |
| 136547 | 卡背尺寸（`SetBackVisible` 内接） | W5（`Core/CardView.cs`） |
| 20915 | 底片位置（`PlaceMarkers` 重复判据） | W5（`Board/BoardLayout.cs`） |
| 128413 · 128437 · 128461 · 128473 · 128497 · 128533 · 128557 | ChatToggle 7 条 β（底图 art 取图口错） | W1（`Battle/SettingsPanel.cs`） |

> ⚠️ 128425（ChatToggle **位置**，α）**归我**，已按 §一 改成 px 直比；它当前还被「底图取不到」掩盖，修好 art 后才见真章。

## 四、类型检查结果

- `TMPDIR=/tmp/wf_w4 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（改完即跑，多轮）。
- `git diff --numstat` = **249 / 91**（接近不了文件行数 ⇒ 未翻行尾）；`CRLF==LF==25806`（纯 CRLF 保住）。

## 五、没能查清 / 没能验的（⛔ 不猜）

1. 🔴 **146504/146516 的残值仍对不上（R2 §四·1 那条没解决）**：去掉 `×108` 后，断言读 `lab0.WorldW = 1110.29px`，而底条 `BarSizeOf` = **405.09px** ⇒ `|405 − (1110+240)| = 945` **仍红**。根因二选一（**我判不了**，需一条「同一拍同时打印 `it.text.WorldW` 与断言 `lab0.WorldW`」的探针）：① 断言与生产读的**不是同一时刻的值**；② `ErrorMessageBanner` 底条**建在陈旧 `WorldW` 上**（那是 β，不在我文件）。**我没跑 Unity，判不了**。
2. **65838 证据强度**：只查到「**原版字段** `TipText` 框宽 = 2450.1299 **> 屏 1920**」；**没查到**「原版横幅文本在屏内」的证据 ⇒ 本断言只断「文本 ≤ **自己的框**」，⛔ 不断「≤ 屏宽」。
3. **14468**：R3 §四·2 说「原版行卡是否真用满卡高」**它没查到** ⇒ 我只断「行心分明」，把「卡框各叠 ~7.4px」如实写进消息，⛔ 没断「不叠」。
4. **51963**：R3 §四·3 说 α/β 取决于「档位该在哪一拍落地」。我按 R3 的指法读**淡变目标**（姊妹条口径）；若后续确认「单 tick 就该 `.level` 落地」，那是**实现**侧（`BattleMusic.Tick` 的步序），不是断言。
5. **136571**：结论依赖「DOTween `Goto(t,true)` 不触发 t=0 `InsertCallback`」这一行为假设（R3 未在本仓实测）。我按 R3 换成 `CardTween.Advance`（真 `ManualUpdate`，跨回调必触发）。
6. 🔴 **R2 说的「还剩 8 处 `ToPixel` + 4 行 `×108f`」**：**报告没给逐条行号**，且它们**当前不红**（16:9 下恰好约掉）。我**只改了与那 3 条红直接相关的** `*108f`（Hug ×2 行 + 语言）。其余 `ToPixel`/`×108f` 里**大多数对世界档件是合法的**（CardView / HandLayout / 战场那个 `cs.x*108f`…）⇒ 按红线「卡住就停手、不发明口径」**未动**，留给一次专门普查。
7. **本批一条都没跑 `BattleScene.Run`**（红线）⇒ 以上 49 条**只验了「编得过 + 读口/题设按判据改对」，没验「跑起来变绿」**。

## 六、顺手发现（⛔ 没动手）

1. **`Label.cs:806` 的 doc 数说大了**（R1 也报了）：原文「恒差 **108** 倍」实为 **10.8 倍**（`FontSizeToPxNow` uGUI 档也 `×UguiFontScale=10`）。⛔ `Label.cs` 不在我白名单，只记一笔。
2. **`ErrorMessageBanner.BarSizeOfParts:859-874` 对 uGUI 口径可疑**：它把 `q.transform.localPosition`（画布 **px**）与 `q.WorldW`（**世界单位**）混进同一并集、再 `×108`。模板那条恰好对（`localPosition=0`，退化成一维），但**位上有偏的件**可能读出可疑值 —— 疑与 §五·1 的 405.09 同源。**没核清、没动**。
3. **`HudPiece.Anchor01()` uGUI 支恒回 `anchorMin`**（W2 的活）：凡「先认锚点再 `NudgeWorld`」的读数都受影响 —— 本轮只暴露 `18289`，建议一次收口。
4. **`BattleDriver.SettingsClickAt` 末尾恒 `return true`**（R1 报过）：让该链的 `hitM` 不是有效判别项；`A6` 的真判别是 `sp.MuteChatOn`（断言已写对）。
