# P34 · 战斗侧 UI 四笔 —— `A1301` / `A1303` / `A1304` / `A1316`

> 执行代理（本轮独占 `Editor/BattleScene.cs`）· 2026-10-19 · **一次 Unity 都没跑**（红线）· **没动 git / 两张正本 / `d:/2`**。
> ✅ 秒级类型检查 **3 趟**（`TMPDIR=/tmp/wf_p34`，全 **0 / 0**）· ✅ 改完 `git diff --numstat` 核过**行尾没被翻**
> （`WfSlider/SettingsPanel/MulliganPanel` 仍纯 LF · `CardDisplayWindow/BattleScene` 仍纯 CRLF）。
> 白名单内改的文件：`Battle/WfSlider.cs` · `Battle/SettingsPanel.cs` · `Battle/MulliganPanel.cs` ·
> `Battle/CardDisplayWindow.cs` · `Editor/BattleScene.cs`。**`MenuDraw.cs` / `trait_tips.json` 一个字没动**（理由见 §⑤）。

## ① 结论

| 笔 | 结论 |
|---|---|
| **`A1301①`** | ✅ **做完** —— 三根音量滑块手柄的 `m_RaycastPadding(−25,−25,−25,−25)` 过上了（并**就地裁了**那条「点轨道也算命中」：**它是原版行为**，见 §②·1 备注） |
| **`A1301②`** | ✅ **做完** —— 设置面板那颗叉图 −20 外扩过上了（命中框 96.37×94.50） |
| **`A1301③`** | ✅ **做完** —— 换牌「完成换牌」上下各外扩 40（命中框 577.5×143.84）；**顺手修掉同一处两个方向都错的旧口径**（下方多认 113 px / 上方漏认 40 px） |
| **`A1303`** | ⛔ **做不到（白名单）** —— 三颗 tooltip 的判定口在 `BattleDriver.TickTooltipAt` / `HitTip`（`Battle/BattleDriver.cs` 本轮**被别的包占着**）。判据已查实、改法已写到行，见 §②·4 与 §⑤·1 |
| **`A1304`** | ⛔ **做不到（白名单）** —— 对应件 = `Battle/WaitBanner.cs` 的 `_shade`（**存在**，但**一条命中路都没有**），修点与 `BattleDriver` 都不在本件白名单。🔴 **且简报那句「再往下外扩 1000 px」方向说反了**：`padB = +1000` 是**内缩**，见 §②·5 |
| **`A1316②`** | ✅ **做完** —— `Battle/CardDisplayWindow.cs` 的效果清单关键词那一行改走 `CardText.KeywordDisplay`（**中文档逐字不变**、英文档从中文变英文） |
| **`A1316①`** | ⛔ **做不到（白名单）+ 简报的修法不成立** —— 正文的读口在 `Core/Tooltip.cs` 的 `TipText.Trait`（`Core/**` 禁碰）；而 `trait_tips.json` 的 `en` 列**是英文【名】、不是正文**，读它修不出正文。见 §⑤·2 |

## ② 逐处表（一行一处；行号一律按**符号名**认）

| # | 文件:符号 | 原版值 + 资产出处（arena1） | 我们的现状（改前） | 裁定 |
|---|---|---|---|---|
| 1 | `Battle/WfSlider.cs` `Contains` / 新增 `HitHandlePadded` | 手柄 `Handle`（`MB_4502`/`MB_4077`/`MB_4984`，`go` 反查 = `Handle`/`Handle_584`/`Handle_858`）：`m_SizeDelta` **46.811×22.406**、`m_RaycastPadding` **(−25,−25,−25,−25)**、`m_RaycastTarget=1` | 全文件 **0 处** `Padded*`；`Contains` 走 `HitBand` **单矩形近似**（整条轨道宽 × `max(6, 17.203)`） | **该改（已改）**：命中框 = **96.811 × 84.406**（框宽 46.811 + 50 / 框高 **34.406** + 50 —— 运行期框高 = 滑区高 12 + 22.406，`Slider.UpdateVisuals` 把非轴那一维锚写成 0/1、`OnEnable:435` 就调它；**运行期 dump 里的 `(46.8,22.4)` 是面板未激活那一刻的序列化值**，⛔ 不是运行期值） |
| 2 | `Battle/SettingsPanel.cs` `HitClose` / 新增 `CloseIconHit` | `Generic Close Button/Close Button`（`MB_5056`，`RT_3460`）：`rect` **56.37×54.50**（stretch 锚 0.12663→0.87337 / 0.13672→0.86328 × 父件 75×75 ＋ `m_SizeDelta(0.364,0.008)`）、pad **(−20,−20,−20,−20)**、`targ=1` | `_close.Contains ∨ _closeIcon.Contains` ⇒ **没过那 20**（量的是画出来的 75² / 31.5²） | **该改（已改）**：命中框 **96.37×94.50**。⚠️ 圆底那颗（`MB_4346`）`m_RaycastTarget=**0**` ⇒ 原版**只由叉图定**（我们多的那一半被 96.37×94.50 包住 ⇒ 结果一样，留作缺图兜底） |
| 3 | `Battle/MulliganPanel.cs` `HitDone` | `MulliganContinueButton/Button`（`MB_5292`，`RT_2659`）：`rect` **577.5×63.84** @中心 (1611.45, 980.25)、pad **(0,−40,0,−40)**、`targ=1` | `_bar.Contains ∨ _play.Contains ∨ 圆(心=文字中心 1525.05,981.05, r=184.45px)` | **该改（已改）**：命中框 **577.5×143.84**。旧口径**两个方向都错**：下方多认 113 px、上方漏认 40 px（判别式见 §④）。同父的 `CircleButton`(80.47×79.64 @1763.45,980.25) 与 `Text`(368.93×62.24 @1525.05,981.05) **都在新框内** ⇒ 并集就是这一块 |
| 4 | `Battle/BattleDriver.cs` `TickTooltipAt` / `HitTip`（⛔ 白名单外） | `Milestones`（`go 496`，173.88×38 @956.3–1130.2 / 465.9–503.9）挂 `EverguildTooltipTrigger`（`MB_4941`：`text=Tips/Hud/Skulls` · `tooltipAnchor=5` · `offset=(0,90)` · `registerEvents=1`）；**可射线的子件两颗** = `MatchSkulls Icon`（`MB_5234` pad 0）**和** `MatchSkulls Score`（TMP `'x3'`） | `HitTip(_skullIcon, …)` **只覆盖骷髅图标那一颗 quad** ⇒ 悬到「x N」上**不出** tooltip | **要做，本件做不到**：宿主 = 父节点 `Milestones` 的**两颗可射线子件的并集**；同族 `ManaHolder` / `QuestPointsHolder` / `FaithHolder` / `SpiritStoneHolder` / `OvertimeIndicator`（arena1 共 **50** 颗 `EverguildTooltipTrigger`，我们 HUD 侧只覆盖 5 颗图标） |
| 5 | `Battle/WaitBanner.cs` `_shade`（⛔ 白名单外） | `WaitText/Dark Shade`（`MB_4415`，`go 990`）：`rect` **3963.53×3366**、pad **(0,+1000,0,0)**、`targ=1`、**只有 `Image` 一个组件**（没有 `BackgroundCloseButton`） | `_shade` = 整屏纯色 quad（α 0.6118，与原版同色）—— **`BattleDriver` 里对它的引用只有 `Create` / `SetVisible`，一条命中路都没有** | **要做，本件做不到**；🔴 **方向订正**：`padB=+1000` 是**内缩**（正 = 内缩）⇒ 原版那块压暗的**下沿从 1858.1 抬到 858.1**（屏幕 1080 ⇒ **最下面 221.9 px 不受它挡**，那里正是 `BottomAnchor/PlayerArea`，手牌区 helper 高 **249.9**）。⇒ 原版语义 = 「等对手时挡住上中部（连 tooltip 的 hover 也挡）、**底下手牌那一带照旧可用**」，而我们**整屏都不挡** |
| 6 | `Battle/CardDisplayWindow.cs` `RowsOf` | 关键词那一行 = 关键词自己的词条族（`Card_Trait/*`），**不是** `Battle/Effect/*` | `CardText.KeywordZh(b.Name)` = **恒中文列** ⇒ 英文档下这一行仍是中文 | **该改（已改）**：改走 `CardText.KeywordDisplay`（跟语档）。中文档逐字不变（`Card_Trait/vanguard` = `("先锋","Vanguard")`）；表索引那半边（`TipText.Trait` 查按中文名索引的规则书表）**仍用 `KeywordZh`**，一个字没动 |

## ③ 改动清单

**`Battle/WfSlider.cs`**（+93/−18）
1. 新增 `HandlePadPx = -25f`（+出处）、字段 `_handlePx`、`HitHandlePadded(...)`（**转发** `MenuDraw.PaddedHitRect`）、静态只读口 `HandleHitPx(handlePx)`。
2. `Contains` 改成**真并集**：① 轨道那一块（`_w × _h`，原版 `Background`/`Fill` 的 pad **都是 0**）② 手柄按 pad 外扩后的框。
3. 🔴 **就地订正两处旧说法**（铁律 5，同一句话两处都改了）：① 文件头「`m_TargetGraphic` = 手柄 ⇒ 点轨道不改值」② `Contains` 的 doc「点轨道也算命中是我们挑的」——**它是原版行为**（`Slider.OnPointerDown` 的 `else` 支 `UpdateDrag`；事件能到 `Slider` 上靠 `Background`(MB_4355)/`Fill`(MB_4553) 两颗 `targ=1`）；`Battle/SettingsPanel.cs` 里那条同义注释一并改。
4. `HitBand` **保留**（外壳侧 `Shell/SettingsWindow.cs` 那根 FPS 滑块还在用），doc 改成「**只剩外壳侧在用**，且那一侧**欠** pad + 真并集」。

**`Battle/SettingsPanel.cs`**（+60/−4）：新增 `CloseIconRectW/H`(56.37/54.5) + `CloseIconPadPx`(−20) + `CloseIconHit(...)` + 只读口 `CloseHitPx`；`HitClose` 改走它。

**`Battle/MulliganPanel.cs`**（+57/−6）：`BarH` `63.8f` → **`63.84f`**（一个数同时管「画多大」与「框多大」；原版 `m_SizeDelta.y` = 63.84000015258789）；新增 `DonePad*` + `BarWorldPos` / `DoneHitPx` 两个只读口；`HitDone` 改成**原版框 + pad**（走 `LayoutSpace.ToDesignPixel` 反推 = `At()` 的真逆）。

**`Battle/CardDisplayWindow.cs`**（+9/−1）：关键词那一行 `KeywordZh` → `KeywordDisplay`。

**`Editor/BattleScene.cs`**（+295/−15，**只加断言 + 改两条被本批改动作废的旧断言文本**）
5. §17 音量滑块：旧负例（「画出来的手柄右缘外 1 px ⇒ 必不中」）**已作废**（那一点现在是**该中**的）→ 重写成 A1301① 五条（见 §④）。
6. §5b·2 效果清单：新增 A1316② 两条；并把中文档那条的说明文字从「走 `KeywordZh`」改成新口径。
7. §17 开局换牌：新增 A1301③ 五条 + 一条 `Debug.Log`（`hits964` 是本文件**另一节**的局部变量 ⇒ 换牌那一节用 `Debug.Log`）。
8. E2③ 关闭钮：**原两条原地保留**（逐点算过仍绿），追加 A1301② 三条。
9. E2 row2 滑块那节的注释与 TSV 文案：「判据只此一份 = `HitBand`」改成新口径（那四条探针在新口径下照样全中）。

## ④ 断言（+ 为什么不自证）

🔑 **期望值一律由【原版字面量】当场算**（`46.811 / 12 / 22.406 / 25`、`56.37 / 54.5 / 20`、`577.5 / 63.84 / 40`），⛔ **不读** `WfSlider.HandleHitPx` / `SettingsPanel.CloseHitPx` / `MulliganPanel.DoneHitPx` —— 那三个只读口**只喂**「原版常数没被悄悄改」那一条探测器，**不是**几何断言的期望值来源。
🔑 **世界 → 我方的 px 换算各走各文件唯一那一份**：滑块/关闭钮 = 该文件自己的 `U(px)` 的倒数；换牌 = `LayoutSpace.FromPixel`（**建件那一份映射**）差出来 1 px 的世界向量 —— 与实现侧 `ToDesignPixel` **互逆**，所以**非 16:9 也成立**（⛔ 没写死 108、也没用 `PxX`）。

| # | 断什么 | 为什么**不自证 / 是判别式**（逐点**离线算过**，见下） |
|---|---|---|
| A1301① a | 手柄命中框四边各**内** 1 px 全中 | 那四点在**旧口径**下 3 点必不中（模拟：`in +x/-y/+y` old=False）⇒ 改回去就红 |
| A1301① b | 框**外** 1 px 不中 | 有界性（挡「恒 true / 命中区取成整屏」） |
| A1301① c | 🔴 **画出来的手柄右缘外 1 px 必中** | **判别式**：旧口径（量实绘 34.406 正方）在那点**必不中**（模拟 old=False） |
| A1301① d | 🔴 轨道中段**上下 10 px 必不中** | **判别式**：旧 `HitBand` 单矩形在这里**必中**（模拟 old=True）⇒ 钉住「轨道那块不过 pad、且不许回到单矩形近似」 |
| A1301① e | `HandleHitPx(34.406)` = 96.811×84.406 | 只读口探测器 |
| A1301② a | 外扩圈内（离中心 47.185 / 46.25 px）**必中** | **判别式**：旧口径（75² / 31.5²）在那点**必不中** |
| A1301② b | 框外 1 px 不中 | 有界性 |
| A1301② c | `CloseHitPx` = 96.37×94.50 | 只读口探测器 |
| A1301③ a | 新框四边各内 1 px + 中心 全中 | 逐点算过（`DoneWorldPos` = 文字中心也仍在框内 ⇒ 驱动层那条点击链没被改坏） |
| A1301③ b | 框外 1 px 不中 | 有界性（同时是**判别式**：旧那个圆在这三点上 old=True） |
| A1301③ c | 🔴 旧那个圆在**框下 119 px** 处**必不中** | **判别式**（模拟：new=False / old=True）⇒ 「多认的那半」被销掉 |
| A1301③ d | 🔴 框**右上角内侧**必中 | **判别式**（模拟：new=True / old=False）⇒ 「漏认的那半」补上 |
| A1316② a | 英文档下关键词行 = `Vanguard` | 期望串是**独立字面量** |
| A1316② b | 🔴 而且 `≠ "先锋"` | **判别式**：拿回 `KeywordZh`（恒中文口）时**两条同时红** —— 「文案与断言一起改回旧值」不可能同时满足 |

**离线复核**（一次性 python 模拟，两处都逐点对上，没进仓）：
· 滑块（值=1）：框 `x∈[234.134,330.945] y∈[±42.203]`；`in ±x/±y` new=T；`out +x/+y` new=F（且 both old=F）；`drawn+1px` new=T/old=F；`track mid ±10px` new=F/**old=T** ✓
· 换牌：新框 `[1322.7,1900.2]×[908.33,1052.17]`；`in` 全 T；`out +y/-y` new=F/**old=T**（也是判别式）、`out +x` 两边 F；`旧圆心下方 119px` new=F/old=T；`右上角` new=T/old=F ✓

## ⑤ 没查清 / 做不到的

1. 🔴 **`A1303` 本件做不到**（白名单）：修点 = `Battle/BattleDriver.cs` 的 `TickTooltipAt`（`HitTip(_skullIcon,…)` 那一列）—— 宿主应改成**父节点那两颗可射线子件的并集**（图标 quad ∪ `MatchSkulls Score` 那颗 TMP 的框；`Label` 有 `Contains`，与 `SettingsPanel._langField.Contains` 同一条路）。同族五颗（`ManaHolder`/`QuestPointsHolder`/`FaithHolder`/`SpiritStoneHolder`/`OvertimeIndicator`）要一起看。**判据已按上面 §②·4 查实**。
2. 🔴 **`A1304` 本件做不到**（白名单）：修点 = ① `Battle/WaitBanner.cs` 出一个「压暗层挡不挡这一点」的口（rect = 那颗 3963.53×3366 按 pad **(0,+1000,0,0)** 收 ⇒ 下沿抬 1000）② `BattleDriver` 在 tooltip/点击链里读它。我们**今天整屏都不挡**（`_waitBanner` 只有 `Create`/`SetVisible` 两个引用）。
3. ⚠️ **`A1316①` 做不到 + 简报送的修法不成立**：`trait_tips.json` 的 `en` 列**是英文名**（例 `{"zh":"护甲","en":"Armour","body":"所受任何来源伤害减少 X（最低 1）","line":"167"}`）⇒ 读它给不出英文**正文**。真要修需要：① 一份英文正文来源（本地有 `资料/规则书/…_英文原版.md`）② 生成器 `工具/gen_trait_tips.py` 出一列 ③ 读口 `Core/Tooltip.cs` 的 `TipText.Trait` 挑列 —— **三处都不在本件白名单**。⚠️ 另：简报写的 `数据/游戏数据/trait_tips.json` **不存在**；真件在 `MyGame/Assets/CardPresentation/Resources/trait_tips.json`，且它**自己写着「别手改」（生成物）** ⇒ 本件**一个字节没动**它。
4. ⚠️ **外壳侧那根 FPS 滑块还没过 pad**（`Shell/SettingsWindow.cs` 的 `FpsHitRectPx` → `WfSlider.HitBand`）：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-3648122393879609434.json`（`Handle_4676551694979268518`）**也是 `(−25,−25,−25,−25)`、`targ=1`** ⇒ 同一个欠账、**在外壳侧**。本件只把口径与工具备好了（`HandleHitPx` / `HitHandlePadded` 的形状），**没有改那个文件**（白名单 + 它的断言在 `Editor/SettingsScene.cs`）。
5. ⚠️ **本批改动【一条断言都没跑过】**（子代理不许跑 Unity；用户口径：活没干完不自检）⇒ §④ 那些断言的实际绿红要等主对话这一趟统一跑（**覆盖这几条的建议**：`BattleScene.Run` 一条就够 —— 四笔的宿主都在它里面；它也是那几条改动的**唯一**宿主）。
6. ⚠️ **`A964②` §五·2 那条「命中区外扩之后的先后」仍未验**：设置钮/关闭钮的命中框现在大了（关闭钮到 ±48.2/±47.25）；`HandleSettings` 里滑块 → 四颗钮的**先后没动**，但真机上「谁先吃到」要真 Play 看（本件没跑实况）。

## ⑥ 顺手发现（⛔ 一个都没改）

1. 🔴 **`Battle/CardChoicePanel.cs` 的 `HitDone` 是同一族的第二个近似**（`ChoosePanel` 继承它）：`_bar.Contains ∨ _play.Contains ∨ 圆(r=190px @确认文字中心)`。**它没有 pad 那笔账**（我另扫了两个包：arena1 的 17 个非零 pad **没有一颗**属于 `ChooseCardMenu` 那棵树；`bundle_battleprefabs_vfxandmisc_assets_all` 的 **2831 个 MB 里非零 pad = 0**）⇒ 那个圆是**我们自己加的过近似**，与 A1301③ 修掉的是同一个形状（改法可直接照搬，但那个文件不在本件白名单）。
2. ⚠️ **arena1 的 17 个非零 pad 现在只剩一颗没落地**：`MB_4415`（`WaitText/Dark Shade`，即 `A1304`）。另外 **4 颗 `targ=0`**（`FaithHolder`(`MB_4059/4319`) / `SpiritStoneHolder`(`MB_4437/4493`) 的 `(+10,10,10,10)`）**收不到射线 ⇒ pad 无从生效**（V2 §一 #9 的口径）。⇒ A1301 这一族**实质上就剩 A1304 一件**。
3. ⚠️ `MulliganPanel` 的 `BarH` 原来那个 `63.8` 是四舍五入来的（原版 63.84000015258789）；同族的 `DoneH = 62.2` 对原版 `Text` 的 62.24、`PlayH = 79.6` 对 79.64 —— **都是截断值**（差 0.04 px，各自的容差放过了）。本件只把 `BarH` 收准（它同时是命中框的基准），另两个**没动**（改它们要连带重算 `SetDoneText` 的字号口径）。
4. 📌 简报里那两处**前提与现读不符**，已在上面逐条标出：① `A1304` 的符号方向（外扩 vs 内缩）② `A1316①` 的「`body` 有 `en` 字段」其实是「行有 `en` 字段，而它是**名**」+ 那个路径不存在。
