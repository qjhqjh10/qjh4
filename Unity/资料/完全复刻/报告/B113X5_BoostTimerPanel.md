# B113X5 · `BoostTimerPanel` 该不该接线

> 会话：第一百一十三会话 · 第 2 波 · 执行代理 `X5`（2026-11-01）
> 账：`账/菜单_外壳.tsv:43-45`（`BoostMenu.HideAllBoosters` / `ShowAllBoosters` / `UpdateAllPanelsTimers`，三条同族）
> 任务前提（调度台给的）：「原裁决『判不做』站不住（原版有、我们没接线）」—— 本件就是回源核这句话。

---

## 一、原版怎么用它（逐方法 + 调用点）

### 1.1 三个类（都在 `Assembly-CSharp`，都是 `MonoBehaviour`）

| 类 | 成员 | 出处 |
|---|---|---|
| `BoostTimerPanel` | 字段 `BoostDescText`(+0x20) / `BoostTimerText`(+0x28) / `BoostItem`(+0x30)；方法 `SetDescription(string)` / `SetTimer(string)` / `Init(BoostItem)` / `UpdateTimer():bool` | `il2cpp_out/dump.cs:76740-76763` |
| `BoostMenu` | 字段 `AllBoostersPanel`(+0x20) / `BoosterPanelPrefab`(+0x28) / `infoText`(+0x30) / `_boostPanels`(+0x38) / `PanelWidth`(+0x40) / `PanelHeight`(+0x44) / `PanelSeparation=30`(+0x48)；方法 `ShowAllBoosters` / `UpdateAllPanelsTimers` / `HideAllBoosters` | `dump.cs:76713-76737` |
| `BoostManager` | `Instance` / `OnTimerUpdated`(static action) / `UpdateTimer`(private) / `SetupDefaultTimer` / `GetAllBoosters` / `AddBooster` … | `dump.cs`（`BoostManager` 段） |

### 1.2 驱动链（逐跳，带行号）

1. **`BoostMenu.ShowAllBoosters()`**（`decomp_full/BoostMenu__ShowAllBoosters.c`）
   - `.c:41-45` `AllBoostersPanel` 已在 `activeInHierarchy` ⇒ **直接 return**（幂等）。
   - `.c:46-69` **订阅**：`BoostManager.OnTimerUpdated += UpdateAllPanelsTimers`（`Delegate.Combine` 那段；ILSpy 同一句在 `BoostMenu.cs:86-92`）。
   - `.c:71` `AllBoostersPanel.SetActive(true)`。
   - `.c:74-77` `_boostPanels = new List<BoostTimerPanel>()`。
   - `.c:78-125` `Enum.GetValues(typeof(BoostItem.BoostType))` 逐类型（`XP=0 / Gold=1`），用谓词 `b.Type == type` 过滤（谓词体 = `BoostMenu.__c__DisplayClass7_0.__ShowAllBoosters_b__0`，`BoostMenu.cs:14-25`）。
     ⚠️ **循环体的「实例化 + 排布」那几句在 `.c` 里被截断**（`FUN_1803f47a0` = 不返回桩），ILSpy 版本同样残缺 ⇒ **每个类型建 1 块还是每条加成建 1 块、以及 `PanelWidth/Height/Separation` 怎么用，读不全**（如实记，⛔ 不猜）。
   - `.c:126-129` 末尾 `infoText.text = <常量串 DAT_1842b80e0>`（串本身未解）。
2. **`BoostMenu.UpdateAllPanelsTimers()`**（`BoostMenu__UpdateAllPanelsTimers.c`）
   - `.c:30-34` `AllBoostersPanel` 不在层级里 ⇒ return。
   - `.c:37-61` 遍历 `_boostPanels`，逐个 `BoostTimerPanel.UpdateTimer()`；**返回 true 的收进临时 `list`**（`.c:53-60`）。
   - `.c:63-95` 再遍历那个 `list`：`_boostPanels.Remove(panel)`（`.c:82-83`）+ `Destroy(panel.gameObject)`（`.c:88-92`）。
   - **调用时机**：不是每帧，是挂在 `BoostManager.OnTimerUpdated` 这个 **static 事件**上（见 §1.2-1）。
3. **`BoostTimerPanel.Init(item)`**（`BoostTimerPanel__Init.c`）：`Item = item`（`.c:18`）→ `BoostDescText.text = item.Type.ToString() + " Boost " + item.PercentBoost + "%"`（`.c:21-29`，字段偏移 `Type=+0x4c` / `PercentBoost=+0x48`）→ `UpdateTimer()`（`.c:30`）。
4. **`BoostTimerPanel.UpdateTimer()`**（`BoostTimerPanel__UpdateTimer.c`）：`ExpirationTime(+0x40) - DateTime.UtcNow` 三档：
   - `(float)TotalHours < 1f`：`TotalSeconds <= 0` ⇒ `"00:00"` 且**返回 1（= 到期）**；否则 ⇒ `"00:mm:ss"`（`Hours` 那格恒 0），返回 0。
   - `Days < 4` ⇒ `floor((float)TotalHours)` 当小时位 + `"mm:ss"`。
   - `Days >= 4` ⇒ `floor((float)TotalDays)` + `" d"`（天位不补零）。
5. `SetDescription` / `SetTimer` = 单句赋值（`BoostTimerPanel__SetDescription.c:7-12`、`__SetTimer.c:7-12`）。
   ⚠️ 这两份产物的**首行名不是自己**（`ChatMessageHeader__UpdateTimer` / 另两份是 `PhotonHandler___ctor`）—— 属已记录的「文件名与内容不符 / ICF 折叠」，**体语义与 ILSpy 逐字一致**（`BoostTimerPanel.cs:14-22`），不是解错。

### 1.3 🔴 那根驱动事件在原版里**永不触发**

- `BoostManager.UpdateTimer` 是 **private**，体里有 `CancelInvoke("UpdateTimer")` ⇒ 本应有人 `InvokeRepeating`；
  但 **全树 0 调用点**（`decomp_full` 只有它自己的 `.c`；`cpp2il/out_cpp2il_dev_ilspy` 里 `BoostManager` 只被 `BoostMenu.cs` 引用）。
- `BoostManager` **没有 `Awake` / `Start` / `OnEnable`**（`dump.cs` 成员表逐行核过）⇒ 没有任何东西会启动它。
- `SetupDefaultTimer`（顶栏那颗「最近到期」倒计时）同样 **0 调用点**。
- `AddBooster` 的 RVA = `0x4B33B0` = **全局共享空桩**（`ret 0`）；`IsAnyBoosterActive` / `IsBoosterOfTypeActive` 共用 `0x6FB110`（全库 640 处的高频 `return false` 桩）。

⇒ **`BoostManager.OnTimerUpdated` 在原版 build 里一次都不会 raise**，所以即使有宿主，`UpdateAllPanelsTimers` 也跑不起来。

---

## 二、我们的现状

| 件 | 状态 |
|---|---|
| `CardPresentation/Shell/BoostTimerPanel.cs`（226 行） | **在**。`BoostItem` / `BoostStore` / `BoostTimerPanel`（`Build` / `Init` / `SetDescription` / `SetTimer` / `UpdateTimer` / `Format`）逐句照 `.c`。全仓 **0 调用方**（除自身）。 |
| `BoostMenu` 那一层（宿主：`AllBoostersPanel` / `BoosterPanelPrefab` / 逐件排布） | **没建**（也没有可建的几何判据，见 §3）。 |
| `BoostManager`（顶栏倒计时 / 加成表） | **没建**（`BoostStore` 只用本地静态表顶替数据侧，恒空 + 出声一次）。 |
| 我们自己的 Boost UI | 我们的商店只有**卡包页**（`Shell/BoosterShopTab` 等价物 = `ShopWindow` + `BoosterInfoPopup`），**没有任何加成面板**。 |

---

## 三、结论 = **不接线**（判 D 维持，且把证据补到 pathID 级）

### 3.1 六条证据（前四条是复核，第五条是本轮补的缺口，第六条是本轮新读的）

| # | 事实 | 出处 / 做法 |
|---|---|---|
| ① | **原版有这些类**（所以「原版没有」是错的说法 —— 这一点调度台说得对） | `dump.cs:76713-76763` · 5 + 5 个方法的 `.c` 全在 |
| ② | **字段指纹 0 命中**：`AllBoostersPanel` / `BoosterPanelPrefab` / `PanelSeparation` / `BoostDescText` / `BoostTimerText` 在 `assets_full` 全库 **0 命中** | 控制实验（同一套 JSON 里字段名**是会序列化**的）：`"nameText"` 命中 **79 份**、`"createCounterSlider"`（只有 `BoosterInfoPopup` 用）命中 **1 份** ⇒ 这个测法是**有分辨力**的，⛔ 不是「字段名永远搜不到」 |
| ③ 🔴 **本轮补的缺口** | **pathID 级 0 引用**：`BoostMenu` = **2818** · `BoostTimerPanel` = **4135** · `BoostManager` = **3930**（UnityPy 读 `WarpforgePlayer_Data/globalgamemanagers.assets`，共 **4,453** 个 MonoScript）⇒ 全库 `assets_full`（24.7 万对象）**没有任何一个 MonoBehaviour 的 `m_Script` 指向这三个 pathID** | 见 §3.2 复现命令。**控制实验**：`BoosterInfoPopup`（pathID `-3149289102854863022`）在同一扫法下命中 `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_7872967592023106223.json` ⇒ 扫法有效 |
| ④ | **guid 级 0 引用**：`BoostMenu.cs.meta` guid `ffefe14f5cabbd2f897a8f1c533c1759`、`BoostTimerPanel.cs.meta` guid `d54da4cd8846c94cf1628433f6e657cc` 在 AssetRipper 导出的 **1,389 prefab + 17 场景** 里只有 meta 自己；方法名 `ShowAllBoosters` / `HideAllBoosters` / `UpdateAllPanelsTimers` / `SetupDefaultTimer` **0 命中**（⇒ UnityEvent 持久调用也排除） | `grep -rl <guid>` 全树 |
| ⑤ | **远端也没有**：安卓 1.35.0 addressable 的 **722 个 MonoScript**（`d:/4/_tmp_view/apk135/monoscripts.json`）里 `BoostMenu` / `BoostTimerPanel` / `BoostManager` 全不在 | 成员判定实跑 |
| ⑥ 🔴 **本轮新读** | **驱动它的那根事件永不触发**：`BoostManager.UpdateTimer` / `SetupDefaultTimer` **0 调用点**，`BoostManager` 无 `Awake/Start`，`AddBooster` = 空桩 `0x4B33B0` | 见 §1.3 |

### 3.2 复现命令（⛔ 别再用「按名字搜」判这类事）

```bash
# ① 取三个类的 MonoScript pathID（脚本写到 d:/4 下，⛔ 别写进 d:/2）
python -I -X utf8 -c "import UnityPy,json; env=UnityPy.load('d:/2/unity_run_ref/Warpforge_Data/globalgamemanagers.assets'); \
print([(o.path_id, o.read_typetree().get('m_ClassName')) for o in env.objects if o.type.name=='MonoScript' \
and o.read_typetree().get('m_ClassName') in ('BoostMenu','BoostTimerPanel','BoostManager')])"
#   实测 → BoostMenu 2818 · BoostManager 3930 · BoostTimerPanel 4135

# ② 全库扫 m_Script（多行正则；用 Grep 工具的 multiline 也行）
#    "m_Script": { "m_FileID": N, "m_PathID": (2818|3930|4135) }  ⇒ 命中 0 份
# ③ 控制实验（证明扫法有分辨力）
#    "m_Script": { "m_FileID": N, "m_PathID": -3149289102854863022 }（BoosterInfoPopup）⇒ 命中 1 份
```

### 3.3 为什么「不接线」是对的（⛔ 不是「影响小」）

- **铁律 11 例外①**：原版这条**在 build 里没有活实例** —— 组件挂不到任何对象上、驱动事件永不触发。
  接线 = 给原版永远不会出现的入口造一个入口，**反而偏离复刻**（同 `判据/B109_R5_三条待裁定.md:63` 的裁断）。
- **数据侧在远端**：`BoostItem` 继承 PlayFab 物品基类（`PlayfabItemID` / `PlayfabHoursDuration`），
  `BoostManager.InitalizeBoosterCollection` 读 `PlayerDataManager.allBoosts`（PlayFab 清单）⇒ 关服后恒空。
- **几何判据一格都没有**：`AllBoostersPanel` / `BoosterPanelPrefab` 的矩形、间距、锚点 **本地 0 实例**
  （`PanelSeparation = 30` 是**字段默认值**，不足以定版式 —— 铁律 3）。

⇒ 结论：**保持现状**（`BoostTimerPanel.cs` = 类体复刻，0 调用方；⛔ 不删 —— 2026-10-31 调度台已裁 `N25`「保留」）。

---

## 四、改动清单

| 文件 | 净行数 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Editor/B113X5_Assert.cs` | **新建 287 行** |
| `Unity/MyGame/Assets/CardPresentation/Shell/BoostTimerPanel.cs` | **净 0 行**（原地改写 5 行注释文本；`git diff --numstat` = `5/5`、`wc -l` 仍是 **226** ⇒ 账里 `:114/:85/:65/:159/:162` 的落点**一格没漂**） |
| 报告 `资料/完全复刻/报告/B113X5_BoostTimerPanel.md` | 新建 |

### 断言件做什么（`B113X5_Assert.Run()`）

- **P 组**（前提硬断言，先写前提再写结论）：定义文件在 + 原版 7 个成员逐个在 + `BoostStore` 两个读口在 + 原版 `BoostMenu__UpdateAllPanelsTimers.c` 在**且里面确实在调** `BoostTimerPanel__UpdateTimer`。
- **S 组**（反证①：我们侧零接线）：除定义件与本件外，全 `Assets/**/*.cs` 里 `BoostTimerPanel.Build(` / `new BoostTimerPanel(` / `BoostStore.` **0 处**；
  三道防恒真 —— `S4` 正控（同一台扫描器在定义文件上必须命中 > 0）· `S5` 合成串（代码行数成 1、注释行数成 0）· `S6` 语料非空。
- **D 组**（反证②：数据源恒空且出声）：空表 ⇒ 0 条；首次问 ⇒ 日志含 `[Boost]`（⛔ 不静默）；未到期保留 1 条；已过期被摘掉。`finally` 里 `ResetForTest()` 复原。
- **F 组**（面板本体仍照原版，字面量来自 `.c` 手算）：`30min→"00:30:00"` · `0→"00:00"+expired` · `-5min→"00:00"+expired` · `2h3m4s→"02:03:04"` · `5d→"5 d"` · `3d23h→"95:00:00"` · `1h→"01:00:00"`。

### 接线请求（⚠️ 不接线，但断言件需要挂宿主）

`Editor/ShellScene.cs` 的聚合行最末尾（`//` **之前**、同一物理行）：

```csharp
B113X5_Assert.Run(); _sink.Pass += B113X5_Assert.Pass; _sink.Fail += B113X5_Assert.Fail;
```

（`Editor/` 不在本代理白名单 ⇒ 由调度台派人接。）

---

## 五、顺手发现

1. 🔴 **账 `菜单_外壳.tsv:43-45` 的「③ 全 247k 对象 0 实例：序列化指纹 grep 0 命中」这条证据是站得住的**，
   但**它当时的旁证口径要补一句**：字段名在这些 JSON 里**是会出现的**（本轮控制实验：`"nameText"` 79 份、
   `"createCounterSlider"` 1 份）⇒ 0 命中 = 真的没有实例，**不是**「字段名搜不到」。建议把那行旁证换成这个更强的控制实验。
2. 🔴 **账里缺 `BoostManager` 这条**（顶栏「最近到期」倒计时，`SetupDefaultTimer` + `UpdateTimer`，
   格式与 `BoostTimerPanel` **不同**：`hh:mm` / `mm:ss` / 过期 `SetActive(false)`+`CancelInvoke`）。
   `调度台日志_107:7582` 曾建议立账，账里至今没有。**本轮补的事实**：它的两个入口 **0 调用点**、`AddBooster` 是空桩
   ⇒ 复刻价值 = 类体照抄，落点也不存在。
3. ⚠️ **`BoostManager.IsAnyBoosterActive`/`IsBoosterOfTypeActive` 共用 RVA `0x6FB110`**（全库 640 处的高频
   `return false` 桩）、`AddBooster` = `0x4B33B0` 空桩 ⇒ 原版这两个谓词**本来就恒 false**，将来若照抄要照抄这个「恒 false」。
4. 📌 `BoostTimerPanel` 文件头原是「按名字搜 `assets_full` 只 1 条命中」，本轮已改成 **pathID 级实扫结论**
   （2818/4135/3930 + 控制实验），免得下个会话再走一遍「按名字搜 ⇒ 0 命中 ⇒ 是不是没找到」。
5. 📌 `BoostItem` 字段偏移与我们的 DTO **顺序一致**（`ExpirationTime +0x40` · `PercentBoost +0x48` · `Type +0x4c`，
   从 `__Init.c` / `__UpdateTimer.c` 读出）⇒ 无需改。

## 六、尚需确认

1. **`ShowAllBoosters` 循环体读不全**（`.c` 与 ILSpy 双侧残缺）：每个 `BoostType` 建「1 块」还是「每条加成一 块」、
   `PanelWidth/Height/Separation` 怎么用、`infoText` 那个常量串是什么 —— **都拿不到**。
   既然判不接线，**本件不追**；将来若用户改口要做，这是**必须先查**的第一格（VA 级反汇编 `0x720310` 附近）。
2. **`Assets/CardPresentation/Editor/B113X5_Assert.cs` 尚未被任何宿主调用**（见 §四 接线请求），
   也**没有跑过**（本代理按纪律不跑 Unity 批处理）⇒ 首次跑绿以调度台同步点为准。
3. `typecheck`：`TMPDIR=/tmp/wf_x5 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
