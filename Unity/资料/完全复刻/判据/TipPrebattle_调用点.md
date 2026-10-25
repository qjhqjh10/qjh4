# `TipPrebattle`（战前提示条）调用点取证

> **产出**：F 判据代理（第 2 轮），2026-10-25。只读，⛔ 没改 `.cs` / 资产。
> **对应实现**：`MyGame/Assets/CardPresentation/Battle/TipPrebattle.cs`（392 行，E 写的）· 报告 `资料/完全复刻/报告/F_TipPrebattle_实现.md`
> **问题**：原版**谁触发**、**什么时候显示**、**传什么参数**。

---

## 一、结论（三句）

1. **「谁触发」= 查不到实例**：当前 build 的**全部 84 个 bundle + 4 个基础文件**里，`TipPrebattle` 的 MonoBehaviour 实例 **0 个**。它只有 **MonoScript**（类被编进了 assembly），没有挂载点。⇒ **原版调用时机拿不到，⛔ 不许猜。**
2. **「传什么参数」= 已查出，且与原实现不符**：原版签名是 **`DisplayTips(PlayerDataManager playerData)`**，不是文案池；文案由 `playerData.GetRandomTip()` → 本地化键 **`PrebattleTips/<key>`** 解析。
3. **「什么时候显示」= 有一条可执行判据（但不是原版的挂载时机）**：`Tips` 是**按 `maxHeroLevel` 分档**的 key 列表；`DisplayTip` 是**一次性**的（`animFlag` 为假才启动，`showingTip` 决定 `OpenTip` 还是 `RenewTip`）；点一下换下一条；`HideTips` 只在 `gameObject.activeSelf` 时收起。⚠️ **原版文案池 `PrebattleTips/*` 在官方本地化表里 0 条** ⇒ 这个功能在当前 build 里连文案数据都没有。

---

## 二、搜证记录（「没有」必须自证 —— 铁律 2 / 方案 §三·2）

### 2.1 搜过的位置与关键词

| # | 位置 | 关键词 | 命中 |
|---|---|---|---|
| 1 | `D:/2/新解包资源/assets_full/`（**84 bundle / 24.7 万文件 / 247,612 对象 / 905 fail**，`_stats.json` + `extract.log`） | `TipPrebattle`（明文） | **1** = `globalgamemanagers/MonoScript/MonoScript_1941.json`（`m_Name:"TipPrebattle"` / `m_ClassName:"TipPrebattle"` / `m_AssemblyName:"Assembly-CSharp"`）—— **只有 MonoScript，没有实例** |
| 2 | 同上 | `openBgWidth` · `timeToOpenBg` · `showingTip` · `deactivateDelay`（4 个**独有字段名**） | **0** |
| 3 | 同上 | `DisplayTips` · `HideTips` · `OnTouchUpAsButton` · `GetNextTip` · `DeactivateTip`（5 个方法名 —— **UnityEvent 的 `m_MethodName` 是落盘的字符串**） | **0** |
| 4 | 同上 | `TipText` / `prebattle` / `pre-battle`（GameObject 名） | 只有一堆 `TipText*.json`（战斗内提示的文本节点），**无战前提示条** |
| 5 | `D:/2/tools/assetripper_out/ExportedProject`（AssetRipper · 1,314 prefab + 场景） | `TipPrebattle.cs` 的 guid `d6ed88de9263baf2c8627d87dd014349` | **1**（只有它自己的 `.meta`）⇒ 导出的 prefab 里也没有引用 |
| 6 | `资料/普查产出_第二十七会话/guid_map.tsv`（4,998 行 = 卡 → 资源 guid） | `TipPrebattle` | **0**（这张表只覆盖卡面资源，本来不含 UI 组件） |
| 7 | 全量反编译（26,282 方法体 / 2,294 类） | `TipPrebattle` | 只在自己文件里；**无 C# 调用点** |
| 8 | `数据/游戏数据/官方本地化_en+zh-CN.json`（**10,854 条**） | `PrebattleTips` 前缀 | **0** ⚠️ **决定性**（见 §三） |

### 2.2 🔑 对照实验（证明上面的「0」不是「搜法不对」）

同样的搜法去打**同族、确实在用**的 `BattleTipController`（战斗内提示）：

| 关键词 | 命中 |
|---|---|
| `tipsGameObject`（独有字段） | **多个** —— `bundle_scenes_scenes_battlearena*/MonoBehaviour/MonoBehaviour_44xx/48xx.json` |
| `startText`（独有字段） | **多个** —— 同上 |

⇒ **MonoBehaviour 的字段名在 assets_full 里是落盘的、搜得到**；`TipPrebattle` 全 0 是**真没有实例**，不是搜法问题。

### 2.3 覆盖度与漏口（如实写）

- **已覆盖**：安装版 `Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/` 的 **84 个 bundle = 100%**（`ls | wc -l` = 84，`assets_full/bundle_*` = 84）；外加 `globalgamemanagers` / `globalgamemanagers.assets` / `level0` / `level1` / `resources.assets` / `sharedassets0.assets`。
- **有 905 个对象导出失败**（`extract.log`：`resources.assets` 616 · `sharedassets0.assets` 182 · `level0` 73 · 其余零散）。13 个战场场景 + menus **全部 `fail=0`** ⇒ **UI prefab 那一侧没有漏口**。失败集中在基础资源（纹理类），与 UI 组件无关。
- **没覆盖**：**远端 Addressables 包**（已关服、本地没有）与**玩家存档/PlayFab user data**。这两处正是 `PlayerDataManager.tips` 的疑似来源（见 §三·3）。

---

## 三、已解出的数据链（**可执行判据**）

### 3.1 参数与文案来源（`.c` / ILSpy 原文）

```
TipPrebattle.DisplayTips(PlayerDataManager playerData)          // ILSpy TipPrebattle.cs:460
    this.playerData = playerData;
    DisplayTip(GetNextTip());

TipPrebattle.GetNextTip()                                        // :467
    if (playerData != null) {
        string key = playerData.GetRandomTip();                  // ← 注意：不是「文案」，是 key
        string term = "PrebattleTips/" + key;
        translation = LocalizationManager.GetTranslation(term, FixForRTL:true, …);
        for (int n = 5; n >= 0; n--) {                           // 最多重抽 6 次
            if (!IsNullOrEmpty(translation) && translation != newTip) break;   // 避开**上一条**
            key = playerData.GetRandomTip();  term = "PrebattleTips/" + key;  translation = GetTranslation(...);
        }
        return translation;
    }
```

**要点**：
- 参数是 **`PlayerDataManager`**，**不是**字符串列表/池。文案由**本地化系统**解析。
- 去重键是**字段 `newTip`（上一条）**，不是「本轮已出现过」。
- 抽 **6 次**（`obj = 5` 且 `obj >= 0` ⇒ 6 次）；全失败也返回最后那一条（可能是空串）。

### 3.2 抽签的数据结构（`Tips.cs` / `PlayerDataManager.cs:5102`）

```csharp
[Serializable] public class Tips {
    public int maxHeroLevel;      // ← 按「最高督军等级」分档
    public List<string> tips;     // ← 每项是一个本地化 key 后缀
}
// PlayerDataManager:
private List<Tips> tips;          // :5102 —— 全文件只有 1 个读点（GetRandomTip），类内**无写点**（⚠️ ILSpy 会丢方法体，见下面 §3.3 的更正）
```

`GetRandomTip()`（`PlayerDataManager.cs:13566`）反编译**损坏**（`NoteDecompilerIssue("Method not found @180F3FA70")`），但骨架可读：遍历 `tips` 挑一个档（用 `maxHeroLevel` 与当前等级比较，记 `obj2`），再从该档的 `List<string>` 里 `UnityEngine.Random.Range(0, size)` 取一条。
⚠️ **「挑哪一档」的确切比较式没解出来**（`Method not found @180F3FA70` 那个辅助方法）。⇒ **这是本文件里唯一一处「读不到」**，别拿它当已定案。

### 3.3 `tips` 从哪来（为什么本地找不到）

- `PlayerDataManager.tips`（`:5102`，`private List<Tips>`）**在类内没有任何赋值点**。
  🔴 **2026-10-25 更正（铁律 5 · 存档体系那一轮）**：上一版这里写「只能来自存档/远端」—— **那个措辞过头了**。准确的三条事实是：
  ① 它是 **`private` 且没有 `[SerializeField]`**（上下文 `:5096-5106` 只有它是 private）⇒ **prefab 序列化不了**；
  ② 类内**无属性包装、无 setter** ⇒ 不是「属性里被赋值」；
  ③ 但 **ILSpy 会丢方法体**（`GetRandomTip` 本身就有 `Method not found @180F3FA70`）⇒ **「没有写点」与「写点被反编译丢了」分不开**。
  ⇒ 结论降级为 **「来源未证」**：可能来自远端存档，也可能来自一个被丢失的方法。要定案得走 `decomp_full` 的 `.c` 按偏移找，或运行时探针。
- 配套证据：官方本地化表里 **`PrebattleTips/*` 0 条**（§二·1 第 8 行）—— 这条**不受上面的更正影响**，仍是硬事实。
  ⇒ **文案池在客户端里不存在**，只能来自远端下发。

### 3.4 交互形态（从方法形状反推，**可执行**）

| 事实 | 出处 | 说明 |
|---|---|---|
| `OnTouchUpAsButton(Vector2 originalTouchPosition)` 是 **private**，签名带 `Vector2` | ILSpy `:502` | 不是 `Button.onClick`（那是无参 `void`），是 **EventTrigger / TouchUp 类事件**的回调 ⇒ 只能由 **UnityEvent 持久监听器**或 `SendMessage` 调到 |
| `DisplayTip` 里 `animFlag` 为真时**直接丢弃** | `:435-448` | 动画进行中再来的 tip **被丢掉**，不排队 |
| `showingTip` 决定 `OpenTip` / `RenewTip` | `:443-447` | 第一次打开 vs 换条 |
| `HideTips` 只做 `if (gameObject.activeSelf)` | `:511-538` | 宿主**不 active 就什么都不做**（连协程都不启） |
| `DeactivateTip` 的等待时长**没解出来**（`Unmanaged memory load: [… +34]`） | ILSpy `:583` | E 已暴露成 `deactivateDelay` 字段 —— 这个处理**对** |

---

## 四、原版 vs 我们的实现（E 那一版）—— 三处要说清

| 项 | 原版 | E 的实现 | 判定 |
|---|---|---|---|
| `DisplayTips` 签名 | `DisplayTips(PlayerDataManager playerData)` | `DisplayTips(IReadOnlyList<string> pool)` | ⚠️ **不同**，但**已经如实标注**（`TipPrebattle.cs:173` 的注释写着「原版 :460 `DisplayTips(PlayerDataManager)`」）⇒ **合理取舍，不算错**。⛔ 只要求**别把「池」说成原版口径** |
| 文案去重 | 与**字段 `newTip`（上一条）**比，最多重抽 6 次 | **✅ 已对齐**（`TipPrebattle.cs:184-204`：`for i=0..5` 共 6 次 · `if (text == newTip) continue` · 前缀 `"PrebattleTips/" + raw` 查 `Loc.HasEntry`） | **唯一不同**：原版 6 次后返回最后那条（可能是空串），我们返回 `null` + `Debug.LogWarning` —— 这是**我们的红线（不静默失败）优于原版**，合理 |
| 分档 | `Tips.maxHeroLevel` **按等级分档** | 平铺 `IReadOnlyList<string>` | ⚠️ 我们**丢了分档维度**。若将来真接文案，**照 `Tips` 的结构**（等级 → key 列表）比平铺更像原版。这是一条**新增判据**，E 那份报告里没有 |

---

## 五、给接线方（E / 下一批）的可执行结论

### 5.1 能确定照做的
- **`DisplayTip` 是「一次性、不排队」的**：`animFlag` 为真时的新 tip **丢弃**（`:435-448`）—— 别加队列、别叠。
- **`HideTips` 先判 `activeSelf`**（`:513`）—— 宿主未激活时是**空操作**。
- **点一下换下一条**（`OnTouchUpAsButton`，`:502`）—— 接在**提示条自身**的点击区上，不是全局。
- **抽 6 次避重（对上一条）**（`:467-500`）。

### 5.2 只能我们自己定的（⛔ 别写成「原版如此」）
- **什么时候 `DisplayTips`**：原版**无实例、无调用点、无文案池** ⇒ **判据缺**。可选的语义锚点（只是语义，不是原版）：**进战场、倒计时/换牌开始之前**（因为类名 `Prebattle` = 战前，且它是 `SpriteRenderer + TextMeshPro` 的**世界空间**条，不是 UGUI 面板）。
- **文案从哪来**：原版 `PrebattleTips/<key>` 本地 **0 条** ⇒ 我们只能自造。**必须**在注释里写「文案源 = 自定；原版本地化键 `PrebattleTips/*` 在本 build 为 0 条」。
- **分档**：若要自造池，照 `Tips{ maxHeroLevel, List<string> }` 的结构（**这是原版唯一可确定的数据形状**）。

### 5.3 建议的 D / C 判定（给裁决用，**不是我拍板**）
- 若把它当**一整件功能**判：倾向 **D**，理由 = ① **无 MonoBehaviour 实例**（84 bundle 全搜 + 对照实验有效）② **无文案数据**（`PrebattleTips/*` 0/10,854）③ 我们有 `BattleTipController` 的等价物（`BattleDriver.cs:1885-1923` 的抬头队列，`A2109` / `W63` 已逐条对齐）。⇒ **替代语义覆盖的是「战斗中的提示」，不覆盖「战前提示条」** —— 这一点要写进备注（D 规则要求说明替代是否覆盖）。
- 若按铁律 11 判 **C**（要做）：那也只做「显示一条自造文案的条」，**调用时机是我们定**，并且**必须声明白**。

---

## 六、还没查清的部分（如实写）

1. **跑一次原版实况**没做 —— 用 `SceneJumpShot` 跳 `battlearena1` dump UI 树，看有没有一个 `SpriteRenderer + TextMeshPro` 的条（名字未必叫 TipPrebattle）。这是**唯一能一锤定音**的办法（铁律 4）。本代理没跑（起 player 属重操作，留给调度台统一安排）。
2. `PlayerDataManager.GetRandomTip()` 里「按 `maxHeroLevel` 挑档」的确切比较式 —— **反编译丢了方法**（`Method not found @180F3FA70`），需要 VA 反汇编或运行时探针。
3. 远端 Addressables 包 / PlayFab user data 里是否有 `tips` 数据 —— **本地没有，无法判**。
4. `DeactivateTip` 的 `WaitForSeconds` 实参 —— 反编译丢（E 已按 `timeToOpenBg * 0.5f` 猜，并暴露字段，处理**合理**）。

---

## 七、一句话交接

> **`TipPrebattle` 的调用时机在原版里是拿不到的**（不是「我没找到」—— 84 bundle 全搜 + 对照实验 + 文案池 0 条，三路互证）。
> 但**参数与文案链**已经解出来了：`DisplayTips(PlayerDataManager)` → `GetRandomTip()` → `PrebattleTips/<key>`，且 `Tips{maxHeroLevel, List<string>}` 是原版唯一可确定的数据形状。
> 接线时**必须**把「时机自定 / 文案自造」写在注释里，⛔ 不许当成原版行为。
