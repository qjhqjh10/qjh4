# B89-E11d · `ShellRuntime` 启动点（真入口）+ `LoadSharedDefaultValues` 逐格核

> 执行代理 `B89-E11d` · 2026-10-28 · 承接 `B89_E11c`（`报告/B89_E11c_LiveOps启动链.md`）

---

## §0 三句话答案

1. **`ShellRuntime` 加了** —— `Shell/ShellRuntime.cs:342`，追加在 `Instance = this;` 同一行（**文件行数不变 1380 → 1380**，见 §1 为什么必须这么接）。
2. **幂等怎么证**：`GameModesCheck` G1–G6 —— G1 先 `ResetBootForTest()` 把单例复位（**复位后必须还是 null**，否则下面全是自欺）→ **G2 `ShellRuntime` 先跑**（拿到实例）→ **G3 `MainMenuRuntime` 再跑 ⇒ `ReferenceEquals` 同一个实例**（= 两处都调也只建一次）→ G6 `EnsureStarted()` 连调两次同一实例。G2 里带 `catch`：**编辑器里跑不起来就显式 `Fail++` + `LogError`**，⛔ 不静默跳过。
3. **`LoadSharedDefaultValues()` 会写 `cardsTextInHandFormat`(+0x140)**（13 格里确有它），**但不会覆盖玩家存档的档位** —— 我们在那一格上**没有消费者**，玩家的档位住在 `PlayerPrefs` + `CardView.HandTextFormat`，两条链互不相交（逐格证据见 §3）。⇒ **G5（手牌文本档）在真机上仍然成立**。

---

## §1 `ShellRuntime` 为什么是一行内追加

`Shell/ShellRuntime.cs` 被账按行号引用 **19 处**（`:120 :138 :227 :341 :379 :383 :444 :647 :681 :740 :889 :1029 :1064 :1207 :1226 :1258 :1259 :1284 :1303`）——
在 `Awake` 里插一整行会把**全部 19 处**往后推。⇒ 按「零行数变化」接法，追加进已有那一行：

```csharp
void Awake()
{
    Instance = this; LiveOpsManager.EnsureStarted();   // 🆕 2026-10-28 B89-E11d：…
    _ownSceneName = gameObject.scene.name;
    DontDestroyOnLoad(gameObject);
    …
```

`git diff --numstat` = **`1 1`**（1 删 1 增，就是这一行）；文件仍 **1380 行**。

**为什么必须是它**：`ProjectSettings/EditorBuildSettings.asset` 里 `Shell.unity` 是**第 0 个场景**，且 `ShellRuntime` 自己 `DontDestroyOnLoad` ⇒ 它是**进主菜单之前**最早的生产入口；`MainMenuRuntime.Awake` 只是第二处（两条进主菜单的路都覆盖）。

---

## §2 【调度台要原话抄的那一段】这次接线的收益 = 什么 / 不包含什么

> **收益**：`LiveOpsManager.HandlerLookup` 从「**永远空**」变成「有两个 handler（`local` + `GameModes`）」。
> 也就是**只有「按原版形状取 handler」的那批代码**才有东西可取 —— 例如
> `LiveOpsManager.GetHandler<GameModes>().SetResultCount("Practice", winner)`（账 584 的 GameModes 半）、
> 以及将来 `GetHandler<Missions>()` / `<CampaignHandler>` 那一族的移植。
>
> **不包含**（⛔ 别记成「活动线修好了」）：
> ① **现有活动窗口一个都不会多显示或少显示** —— 它们读的是 `LiveOpsLocalCatalog` / `LiveOpsLocalHandler.Default`
>    （**直读**），全仓 `LiveOpsManager.GetHandler` 的命中**全部是注释里引用原版**；
> ② **活动数据本来就有** —— `LiveOpsLocalBoot.BootLocalLiveOps` 带 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`，
>    启动就把 13 份本地 SO 装进 local handler（`Shell/LiveOpsHandler.cs:1510`）；本批**没有**「登记了但 catalog 是空的」这个问题；
> ③ 本批**不跑** `BuildHandlers` / `Unpack`（那两条要服务器载荷），也不改任何 UI 的取数路径。

---

## §3 `LoadSharedDefaultValues()` 逐格核（`Shell/GameStaticData.cs:150-174`）

| 原版 `.c` 的 13 格（偏移） | 我们写了吗 | 备注 |
|---|---|---|
| `MusicVolume +0xC0` · `SoundFxVolume +0xD4` · `VoiceOversVolume +0xD0` | ✅ | 1f |
| `_disableParticles +0xD8` · `lastRegistrationShowTime +0xE0` · `notificationsOn +0xE8` | ✅ | false / 0 / false |
| `hiFPS +0x11B` | ✅ | **true**（不是 false） |
| `smallScreenUI +0x11C` | **❌ 故意不写** | 文件注释：唯一来源是 `SmallScreenUI.Enabled` |
| `useCombatAutoZoom +0x125` · `touchInput +0x11D` · `graphicsQuality +0x120` | ✅ | `DefaultTouchInput` / `isMobilePlatform` / `DefaultQualitySetting` |
| **`cardsTextInHandFormat +0x140`** | **✅ 写了** | `= DefaultCardTextsInHandOptions`（默认 0；`smallScreenUI=1` 时 = OnlyDescription(1)） |
| `lastTimeAskedForNotifications +0x138` | ✅ | 0 |

**关键结论（会不会冲掉玩家档位）：不会。** 三条现读证据：
1. 它写的是 **`GameStaticData.cardsTextInHandFormat`** 这个静态字段（`Shell/GameStaticData.cs:111`）。
2. 那一格在本仓**没有任何生产消费者**（全仓命中只有：`GameStaticData` 自己的声明/写入、`Editor/GameStaticDataCheck.cs` 断它的默认值、以及注释）。
   我们的消费源是 `Core/CardView.HandTextFormat`。
3. 玩家档位的持久源是 **`PlayerPrefs`**（`SettingsWindow.TextInHand`，键 `CardsTextInHand`），`TextInHand.Load()` 读的也是它 ——
   **不读** `GameStaticData`。⇒ 两条链**不相交**，`LoadSharedDefaultValues` 与 `ReadTextInHandFormatAtBoot` 的先后**与结果无关**
   （当前顺序「先灌默认值、后读存档」仍保留：它和原版 `LoadPlayerData` 的次序同形）。

> ⚠️ 顺带如实记一条**既有差异（不是本批造成的）**：原版那一格就是玩家档的**唯一存储**；我们把它挪到了 `PlayerPrefs`，
> `GameStaticData.cardsTextInHandFormat` 于是变成一份**没人读也没人写（除默认值外）的休眠副本**。要不要把它接成唯一源，
> 属卡面/设置线的账（`Shell/GameStaticData.cs` 本批无权限）。

---

## §4 哪半「没验到」/ 风险

- **`DontDestroyOnLoad` 在编辑模式**：G2 会在编辑模式下 `AddComponent<ShellRuntime>()` + 手动 `SendMessage("Awake")`，
  而 `Awake` 里有 `DontDestroyOnLoad(gameObject)` —— 编辑模式下它至多打一条 warning、不抛（`Awake` 其余三句都无副作用依赖）。
  **万一它抛**，G2 的 `catch` 会显式 `Fail++` + `LogError`（写清「这条没验到」），⛔ 不会让整节静默变绿。
- **`SaveCenter.StoreOn`**：批处理/自检里按设计为 false ⇒ `LegacySaveReader.ImportAll()` 那一支**不会跑**（不碰玩家真档）。
- **没验到的**：真机（player）里 `Shell.unity` 的 `Awake` 实跑 —— 那是 Unity 运行期的事，本批不跑 Unity（调度台在跑）。

---

## §5 账 / 合并顺序（要调度台做的）

- 补丁 **`账/_parts/B89_E11d.tsv`**（11 列 · 1 行 · `轮次` = `B89-E11d`）：`LiveOpsManager.get_Instance()`（A），
  落点写全**两处**调用点（`Shell/ShellRuntime.cs:342` + `Shell/MainMenuRuntime.cs:55`）。`check_patch_conflicts.py` ⇒ **本补丁 0 冲突**。
- **合并顺序**：`B89_E11c → B89_E11d`（后者的 `get_Instance()` 行是前者的超集，字典序也是 E11d 后合，天然赢）。
- **`GraphicsTab.CardsTextInHandDropdownChange(int)` 仍与 `B89_E12b.tsv` 撞键**（`GraphicsTab` 那行在 **E11c** 补丁里）⇒ **先合 E12b、后合 E11c**。
- **`PlayerDataManager.LoadSharedDefaultValues()` 与 `B89_E12.tsv` 撞键**（E12 已把它标 A，字典序 E12 赢）⇒
  我**没把这一行放进补丁**（避免覆盖 E12 那份）。**§3 那段结论请并进 E12 那一行的差异列**，原文如下，可直接粘：

  > 🆕 2026-10-28 B89-E11d 逐格核：`Shell/GameStaticData.cs:150-174` 写 **12 格**，与 `.c` 的 13 格逐条对得上（唯一没写 `smallScreenUI +0x11C`，理由 = 唯一来源 `SmallScreenUI.Enabled`，文件注释写明）。**含 `cardsTextInHandFormat +0x140`**（`= DefaultCardTextsInHandOptions`）。⚠️ 但它**不会冲掉玩家的手牌文本档**：那一格在本仓**无生产消费者**，玩家档位在 `PlayerPrefs`（`SettingsWindow.TextInHand`，键 `CardsTextInHand`）+ `CardView.HandTextFormat`，两条链不相交 ⇒ 与 `LoadSharedDefaultValues` 的调用顺序无关。既存差异（非本批造成）：原版那一格是唯一存储，我们这份是**休眠副本**。

- 🩹 **顺手修了我自己的一处补丁格式错**：`账/_parts/B89_E11c.tsv` 第 2 行（`get_Instance()`）原来只有 **10 列**（漏了 `轮次` 那一格，备注落到了轮次位）—— 已就地补成 **11 列**（`轮次= B89-E11c`）。三份补丁现已逐行复核 11 列。

---

## §6 typecheck

```
TMPDIR=/tmp/wf_B89E11d bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```

---

## §7 断言清单（`Editor/GameModesCheck.cs` · G 节）

| 断言 | 内容 | 灭自证 |
|---|---|---|
| G1 | 复位后单例是 null | ✅ 前提 |
| G2 | **`ShellRuntime.Awake` 跑过 ⇒ 单例已建** | ✅ 注掉 ShellRuntime 那句必红 |
| G3 | **两处都调 ⇒ 同一个实例**（ShellRuntime 先、MainMenuRuntime 后） | ✅ 若变成「重建」必红 |
| G4 | `GetHandler<GameModesHandler>()` 非 null | — |
| G5 | 开机读手牌文本档：`TextInHand=2 ⇒ CardView.HandTextFormat==2` | ✅ 注掉 Awake 那句必红 |
| G6 | `EnsureStarted()` 幂等 | — |
