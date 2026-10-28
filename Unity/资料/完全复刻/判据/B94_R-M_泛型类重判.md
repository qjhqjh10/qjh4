# B94 · R-M · 泛型类账行重判（11 个泛型类 · 用**真方法体**）

> 只读取证代理 **R-M**（第九十四会话 · 波 1.5）。**未改任何源码 / 账 / 正本**。
> 判据源 = **`D:/2/tools/decomp_generic_trial/out_b94/`（210 个 `.c`，本次新导出）** + `INDEX.tsv` +
> `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/*.cs`（字段名/字面量）+ `D:/2/tools/decomp_full/` +
> `D:/2/Warpforge_code/Scripts/Assembly-CSharp/`（签名桩）+ 我们的实现 `D:/4/Unity/MyGame/Assets/CardPresentation/`。
> ⚠️ 本文件里**凡没标「读体核过」的，都是没读体或没读完** —— ⛔ 别把它当已闭合。

---

## §〇 一句话结论

**这 172 条里，真正的「判据未闭合」只剩 5 处**（`GetMessageContent` 语言回退、`PlayEvent.MakeOpponentInfo` 的两格 getter、
`b__9_0` 转调地址、`GetPossibleWarlords` 的三个字段名、`ConfigProvider.UnpackInternal` 的缓存口）；
**其余全部可用真体闭合**。**推翻 5 条旧判据**（其中 2 条是「我们以为方法体只有一个云脚本 / 已做完了」），
**11 条 D 判据的依据说错**（把「抽象方法」「本地逻辑」当成了「服务端」），
**12 组「同键两账状态打架」**（跨 `轮抽.tsv` ↔ `泛型与启动链_补读.tsv` 等）。

---

## §一 汇总

### 1·1 覆盖

| 项 | 数 |
|---|---:|
| 命中 11 个类的账行 | **172** |
| 去重后 (类,方法) 键 | **105**（含签名写法差异，真方法 ≈ 97） |
| 分布在 7 个账文件 | 泛型与启动链_补读 76 · 日常_奖励 40 · 轮抽 32 · 社交_聊天 9 · 商店 6 · 未分类_其他 5 · 个人资料_成就_排行 4 |
| **同键两账状态打架** | **12 组**（见 1·3） |
| 本轮**读了真体并写进本文件**的方法 | 52 |

### 1·2 按类

| 类 | 账行 | 其中 B/C | **可升 A** | **需改实现** | **只订正判据** | 判不了/还差 |
|---|---:|---:|---:|---:|---:|---:|
| **DraftEventBase** | 58 | 6 | 4（GetBaseData / GetBaseSave / GetMatchmakingFilter / get_CurrentStage） | 5（GetEventDeck → PickWarlord → ValidateEventDeck → Validate → UpdateState） | 9 | 1（b__9_0） |
| **PlayEvent** | 43 | 1 | 2（GetBaseData / GetBaseSave） | 2（MakeOpponentInfo 两格 / SetCustomMatchOptions） | 4 | 1（MakeOpponentInfo 残差 getter） |
| **BaseInboxEvent** | 17 | 0 | 9（其余全 A） | **5**（GetTitle / GetMessage / GetMessageContent / ClaimRewards / OnComplete） | 3 | 0 |
| **GameNewsEvent** | 11 | 0 | 5 | 1（SelectEvent 本地半句） | 2（GetTitle/GetMessage 是 **abstract**） | 0 |
| **ShopStore** | 9 | 1（C） | 5 | 1（RemoveFinishedOffer 的 `IsActive` 维） | 2 | 0 |
| **SignalEventTrigger** | 8 | 2 | **4（Register / Unregister 全员升 A）** | 0 | 0 | 0 |
| **ScoringEvent** | 7 | 0 | 3 | 0 | 1（PurchaseMilestone **abstract**） | 0 |
| **RankedEventBase** | 7 | 0 | 3 | 1（GetDivisionByPoints = 接口默认实现） | 1 | 0 |
| **ConfigProvider** | 6 | 0 | 0 | **3（Initialize / UnpackInternal / OnDestroy）** | 3 | 0 |
| **ReferenceList** | 4 | 0 | 0 | 2（LoadReferences / op_Implicit） | 2 | 0 |
| **MultiKeyLiveOpsHandler** | 2 | 0 | 0 | 0 | 2（ExtractKey **abstract**） | 0 |

> 「可升 A」= 真体证实了账里的判据 **且** 我们的落点逐句等价。
> 「需改实现」= 真体证明原版有一块本地行为我们没有（或做得不一样）。
> 「只订正判据」= 行为已对/本就无体，只要把账里的判据文字换掉。

### 1·3 🔴 同键两账状态打架（12 组，必须一并裁定）

| 键 | 一处 | 另一处 | 本轮裁定 |
|---|---|---|---|
| `ShopStore.GetBaseData()` | A `商店.tsv:292` | D `补读.tsv:33` | **A**（纯取值器，见 §二·5） |
| `PlayEvent.GetBaseData()` | A `日常_奖励.tsv:555` | D `补读.tsv:241` | **A**（同上） |
| `PlayEvent.OnMatchFinished(...)` | D `日常_奖励.tsv:564` | A `补读.tsv:246` | **A**（真体有本地 `LossStreak` 读写；云脚本另算） |
| `PlayEvent.SetCustomMatchOptions(...)` | A `日常_奖励.tsv:568` | D `补读.tsv:252` | **B**（真体是本地写 Photon 房间属性，我们**没有**等价物） |
| `SignalEventTrigger.Register(Action)` | B `日常_奖励.tsv:702` | A `补读.tsv:261` | **A**（真体已导出，逐句吻合） |
| `SignalEventTrigger.Unregister()` | B `日常_奖励.tsv:704` | A `补读.tsv:264` | **A**（同上） |
| `ConfigProvider.Initialize(Dictionary)` | D `未分类.tsv:177` | A `补读.tsv:576` | **A**（真体 = 一行转调；我们 `Install` 只差 `Unpack` 那一跳） |
| `DraftEventBase.OnMatchFound(string)` | A `补读.tsv:748` | D `轮抽.tsv:19` | **A**（基类调用是本地；云脚本那一句另记） |
| `DraftEventBase.GetBaseData()` | D `补读.tsv:755` | B `轮抽.tsv:9` | **A**（纯取值器，我们有 `Data`） |
| `DraftEventBase.GetBaseSave()` | D `补读.tsv:756` | A `轮抽.tsv:10` | **A**（纯取值器，`LiveOpsEvent.GetBaseSave` 已有） |
| `DraftEventBase.GetMatchmakingFilter(...)` | A `补读.tsv:758` | D `轮抽.tsv:13` | **A**（纯串函数，我们已按真体重写） |
| `DraftEventBase.RefundRun(...)` | D `补读.tsv:764` | A `轮抽.tsv:26` | **A**（真体：非付费档 `onComplete(空表)`；我们的实现逐句等价） |

> 🔴 **这批「打架」全部来自「泛型体导不出来时，两条线各按 ILSpy 猜了一次」**。
> 真体到手的今天，**一律以本文件的裁定为准**，并把**另一处同键行改写/标失效**（⛔ 别留两套）。

### 1·4 🔴 被真体推翻的 5 条旧判据（铁律 5：就地订正的正文在 §二）

1. **`BaseInboxEvent.GetTitle` / `GetMessage`**：账里写「就是 `GetMessageContent().title`」「三处 `[[…]]` 替换在出厂表里已做完」
   —— **错**。真体在**运行时**做 3 次 `String.Replace`（奖励名 / 玩家名 / 联盟名）。我们 `InboxData.Title/Body` **一次都没做**。
2. **`DraftEventBase.PickWarlord`**：账里（`B93_N3`）写「全文只有一句云脚本」—— **错**。真体**先本地写 `eventData.<0x88> = warlord.Reference`** 再打云脚本。
3. **`DraftEventBase.ValidateEventDeck`**：账写「只校验卡都在本活动内」—— **太薄且漏了关键点**：真体把 `validateOwnership` **硬编码成 `false` 传给基类**（形参被忽略），另加一条 `(save.selectedCards.Count - 1) <= eventData.<0x90>.Count` 守卫。
4. **`PlayEvent.MakeOpponentInfo` 的「key 4 / key 1」**：极易被读成 **`PlayerDataKey` 枚举值**（4=`GlobalChatBanned` / 1=`ProfileVersion`，明显不合理）。
   实为 **IL2CPP 泛型虚调用助手 `0x1800021F0` 的 vtable 槽号**（slot 4 → bool、slot 1 → int）。
5. **`ConfigProvider.UnpackInternal` / `OnDestroy` 判 D（「服务端」）** —— **错**。真体是 **`new T()` → `titleData.TryGetValue(T.Key)` → `JsonWrapper.DeserializeObjectWithType<T>` → 缓存 → `Debug.Log`**，
   **全程本地**，一个后端调用都没有。

### 1·5 🔴 4 个方法**本来就是抽象/空**（⛔ 别当「没解出来」去补实现）

| 方法 | 状态 | 判据 |
|---|---|---|
| `DraftEventBase.OnMatchProcessSuccess` · `PlayEvent.OnMatchFinished` · `PlayEvent.SetCustomMatchOptions` | **空体**（`RVA 0x4B33B0` 全局空桩，体 = `return;`） | `out_b94/*__4b33b0.c` 三份全文 `void X(void){return;}` |
| `SignalEventTrigger.ValidateSignal` | **abstract** | ILSpy `SignalEventTrigger.cs` / 实现在 `AddTagTrigger` / `WildcardTrigger` / `ItemReceivedSignal` … |
| `ScoringEvent.PurchaseMilestone` | **abstract** | ILSpy `ScoringEvent.cs:47`；实现在 `ExpansionPassEvent` / `RaidEvent` / `SinglePlayerScoringEvent` |
| `MultiKeyLiveOpsHandler.ExtractKey` | **abstract** | ILSpy `MultiKeyLiveOpsHandler.cs:6` |

---

## §二 逐条（按类；`判据` 一栏是**读真体写出来的**，⛔ 不是「见文件」）

> 记法：`R` = 本轮真体路径 `D:/2/tools/decomp_generic_trial/out_b94/<文件>`（省略前缀）。
> 「账现状」= 账里现在写的。⚖️ = 冲突项已按 §一·3 裁定。

---

### 2·1 SignalEventTrigger（4 方法 / 8 行）— **全员可闭合**

**产物**：`SignalEventTrigger_object_.Register__12549a0.c` · `Unregister__1254b90.c` · `TryTrigger__1254a40.c`
（另一份 `SignalEventTrigger___Il2CppFullySharedGenericType_*` 是同方法的共享泛型变体，语义一致）

#### `Register(Action callback)` — 账 `日常_奖励.tsv:702`[B] · `补读.tsv:261`[A]
**真体逐句**（`R/SignalEventTrigger_object_.Register__12549a0.c`，全文 12 行）：
```csharp
void Register(Action callback) {
    this.callback = callback;                                   // ★ 存 this+0x18（写屏障 FUN_1803f3730）
    Action a = new Action(this, <MethodInfo +0x18/+0x28>);      // ★ 方法组 = this.TryTrigger
    Signal.Register<T>(a, <MethodInfo +0x30>);                  // ★ ★ 目标重载 = Register<T>(Action<T>)
}
```
- 🔑 **「挂在哪条泛型重载」这个残差没了**：委托类型是 `System_Action<object>`（= `Action<T>`）⇒ **`Register<T>(Action<T>)`**，不是 `Register<T>(Action)`。
- 🔑 MethodInfo 槽 `+0x30` 是 `Register` 的 token（G1 已知：`Unregister` 用 `+0x40`）。

**账现状**：判据要点写的是「三条旁证 + 具体泛型重载没读到」—— 三条旁证**全部被真体证实**（次序、形状、目标 API）。
**我们实现**：`Shell/LiveOpsFilter.cs:561` = `this.callback = callback; LiveOpsSignal.Register<T>(TryTrigger);` —— **逐字等价**。
**⇒ 结论：`日常_奖励.tsv:702` B → **A**；`补读.tsv:261` A 保持。**残差清零**，账里「还差 -readOnly 泛型重导」可划掉（已重导）。

#### `Unregister()` — 账 `日常_奖励.tsv:704`[B] · `补读.tsv:264`[A]
**真体逐句**（`R/SignalEventTrigger_object_.Unregister__1254b90.c`，全文 10 行）：
```csharp
void Unregister() {
    Action a = new Action(this, <MethodInfo +0x18/+0x28>);      // 同一个 this.TryTrigger
    Signal.Register<T>(a, <MethodInfo +0x40>);                  // ★ 首行是 Signal__Register（ICF 折叠）
}
```
- 🔑 **ICF 折叠已由 G1 钉死**：体里调的是 `Signal__Register`，但 **MethodInfo 槽位 `+0x40`** 才是真身份 ⇒ 语义是 `Signal.Unregister<T>(TryTrigger)`。
- ⚠️ **判据只认槽位**（`+0x30` = Register / `+0x40` = Unregister），⛔ 别看函数名。

**⇒ 结论：`日常_奖励.tsv:704` B → **A**。** 我们 `LiveOpsFilter.cs:569` = `LiveOpsSignal.Unregister<T>(TryTrigger);` 等价。

#### `TryTrigger(T signal)` — 账 `日常_奖励.tsv:703`[A] · `补读.tsv:262`[A]
**真体逐句**（`R/SignalEventTrigger_object_.TryTrigger__1254a40.c`，全文 9 行）：
```csharp
void TryTrigger(T signal) {
    if (!ValidateSignal(signal)) return;      // ★ vtable +0x198（抽象 ValidateSignal）
    if (this.callback != null) this.callback();   // ★ this+0x18 非空才 invoke
}
```
**账现状**：「先校验后回调」✓ **一致**。**我们**：`LiveOpsFilter.cs:577` 逐句同形。**⇒ A 保持。**

---

### 2·2 ConfigProvider（3 方法 / 6 行）— **D 判据有三条是错的**

**产物**：`R/ConfigProvider_object_.Initialize__16826b0.c` · `UnpackInternal__1682730.c` · `OnDestroy__16826e0.c` · `get_Key__1682930.c`

#### `Initialize(Dictionary<string,string> titleData)` — 账 `补读.tsv:576`[A] · `未分类.tsv:177`[D]
**真体逐句**（全文 6 行）：
```csharp
void Initialize(Dictionary<string,string> titleData) {
    this.<0x20> = Unpack(titleData);      // ★ 一行转调 protected virtual Unpack（vtable +0x1d8）
}
```
**账现状**：D 那条写「服务端」—— **错**；A 那条落点 `Shell/LiveOpsFilter.cs:721（Install）` = `Current = config`，**只做了赋值、没做 `Unpack` 那一跳**。
**我们实现**：`EventGroupConfig.Install`（`LiveOpsFilter.cs:721`）是**我们自己的注入口**（🔶 已标），不是 `ConfigProvider<T>` 的等价物。
**⇒ 结论：`未分类.tsv:177` D → **A**（改判据：本方法无后端）；`补读.tsv:576` A **保留但落点要补注**「`Install` 只等价于『把已解好的配置装进来』，`Unpack` 那一跳见下条」。**

#### `UnpackInternal(Dictionary<string,string> titleData)` — 账 `补读.tsv:577`[D] · `未分类.tsv:179`[D]
**真体逐句**（`R/…UnpackInternal__1682730.c`）：
```csharp
T UnpackInternal(Dictionary<string,string> titleData) {
    this.<0x20> = ReflectionHelper.New<T>();                       // ★ 先 new 一个默认实例
    if (titleData != null) {
        string key = this.Key;                                     // ★ get_Key = new T() 后读接口 slot 0
        if (titleData.TryGetValue(key, out string json)) {          // ★ FUN_181776150 = Dictionary.TryGetValue
            this.<0x20> = JsonWrapper.DeserializeObjectWithType<T>(json);   // ★ 反序列化覆盖
            <cache>.Write(1);                                      // ★ 缓存写（FUN_180002340 slot 1）
            return this.<0x20>;
        }
    }
    Debug.Log(<前缀> + typeof(T).Name);                             // ★ 未命中：只打一行日志，返回默认实例
    return this.<0x20>;
}
```
**⇒ 结论：两条 D → **B（可本地复刻）**。**
我们**没有** `ConfigProvider<T>` 这个泛型基（全仓 grep `ConfigProvider` 只命中 `CardTierConfig.cs` / `RankedData.cs` 等**具体**配置类）。
**判据一句话版**：「从 TitleData 字典里按 `T.Key` 取 JSON → `DeserializeObjectWithType<T>` → 缓存；取不到就 `Debug.Log` + 留默认实例」。
**还差**：缓存口 `FUN_180002340(slot 1/2, …)` 的宿主类未定（`+0x28` 那个静态字段的类型没解）—— **不影响行为复刻**（用我们的 `SaveCenter` 顶即可，但要标 🔶）。

#### `OnDestroy()` — 账 `补读.tsv:578`[D] · `未分类.tsv:178`[D]
**真体逐句**（全文 6 行）：
```csharp
void OnDestroy() { if (this.<0x20> != null) <cache>.Clear(2); }   // ★ 有解过配置才清缓存（slot 2）
```
**⇒ 结论：两条 D → **B**。** 判据 = 「配置实例非空 ⇒ 清那本缓存」。

---

### 2·3 ReferenceList&lt;T&gt;（2 方法 / 4 行）— **两条 D 都是错的（纯本地懒加载）**

**产物**：`R/ReferenceList_object_.LoadReferences__1223290.c` · `op_Implicit__12234c0.c` · `get_Elements__1223480.c`

#### `LoadReferences()` — 账 `未分类.tsv:513`[D] · `补读.tsv:605`[D]
**真体逐句**：
```csharp
void LoadReferences() {
    var src = this.<0x10>;                                     // 引用列表（字段）
    this.<0x18> = src.Where(<b__4_0 谓词>).ToList();            // ★ Enumerable.Where + ToList
}
```
**判据一句话**：「`elements = 引用列表.Where(λ).ToList()`」——**没有任何 Addressables 异步加载**，就是个 LINQ 筛选。

#### `op_Implicit(ReferenceList<T>) → List<T>` — 账 `未分类.tsv:512`[D] · `补读.tsv:606`[D]
**真体逐句**：
```csharp
static implicit operator List<T>(ReferenceList<T> list) {
    if (list.<0x18> == null) list.LoadReferences();    // ★ 懒加载：没算过就先算
    return list.<0x18>;
}
```
（`get_Elements()` 同形：`if (<0x18> == null) LoadReferences(); return <0x18>;`）

**⇒ 结论：4 行 D 全 → **B**（可本地复刻）。** 我们 `Core/LoadableRef.cs` 有 `LoadableReference<T>` 的 `op_Implicit`（`:234` / `:253`），
但**没有 `ReferenceList<T>` 这层「列表 + 懒筛」**。落点文件 `Core/LoadableRef.cs` 现在归 **V1**（占用）。

---

### 2·4 BaseInboxEvent（9 方法 / 17 行）— **5 处要改实现**

**产物**：`R/BaseInboxEvent_object__object_.{GetTitle,GetMessage,GetMessageContent,GetRewards,IsRead,RedeemableAt,StartDate,ClaimRewards}__*.c`
+ `_ClaimRewards_g__OnComplete_7_0__15897f0.c`

#### 🔴 `GetTitle()` — 账 `补读.tsv:618`[A] · `社交_聊天.tsv:180`[A] — **旧判据错**
**真体逐句**（`.c` 控制流 + ILSpy `BaseInboxEvent.cs:152-215` 拿字面量）：
```csharp
string GetTitle() {
    var first = GetRewards()?.FirstOrDefault();                  // ★ FirstOrDefault<RewardInfo>
    string rewardName = first?.Item != null ? first.Item.<vtable+0x1A8>() : "";   // ★ 奖励物名（无奖励 ⇒ 空串）
    var c = GetMessageContent();
    if (c != null && c.title != null) {
        string t  = c.title.Replace("[[RewardName]]", rewardName);
        string t2 = t.Replace("[[PlayerName]]", PlayerDataManager.singletonManager.playerName);
        return t2.Replace("[[AllianceName]]", AlliancesManager.Instance.CurrentPlayerAllianceName);
    }
    // NRE
}
```
**账现状（错）**：「就是 `GetMessageContent().title`」「`[[RewardName]]/[[PlayerName]]/[[AllianceName]]` 三处替换**在出厂表里已做完**」。
**我们实现**：`Shell/InboxData.cs:207 Title()` = `GetMessageContent(m).Title`，**一个 Replace 都没有**，
而且 `:206` 的注释明写「⛔ 不再第二处解」—— **那条注释本身就是错的**。
**⇒ 结论：需改实现 + 判据订正。** 三处替换**必须**在运行时做（玩家名 / 联盟名是**动态**的，烘进表里不可能对）。

#### 🔴 `GetMessage()` — 账 `补读.tsv:619`[A] · `社交_聊天.tsv:177`[A] — **同上**
**真体逐句**（ILSpy `BaseInboxEvent.cs:218-…`）：与 `GetTitle` **同构**，只是把 `.title` 换成 `.message`（三处 `Replace` 一样）。
**我们**：`InboxData.cs:213 Body()` 同样没做替换。**⇒ 需改实现 + 订正。**

#### `GetMessageContent()` — 账 `补读.tsv:620`[A] · `社交_聊天.tsv:178`[A] — **原判据太薄**
**真体逐句**（`R/…GetMessageContent__1588fe0.c`）：
```csharp
LocalizedMessage GetMessageContent() {
    string lang = I2.LocalizationManager.CurrentLanguageCode;      // ★ 当前语言
    var list = this.<0x18>.<0x78>;                                // ★ List<LocalizedMessage>
    var hit = list.Find(x => x.languageCode == lang);              // ★ 按语言找
    if (hit != null) return hit;
    return list.Find(x => x.languageCode == <默认语言>);            // ★ 找不到退默认语言
}
```
**账现状**：「取消息内容」—— 完全没写**按语言找 + 回退**。
**我们**：`InboxData.cs:GetMessageContent(Msg)` 只从**单一** `TitleText/TitleTerm` 取，**没有多语言条目、没有回退**。
**⇒ 结论：需改实现（多语言条目 + 语言回退）+ 判据订正。**
⚠️ 单语言数据下**行为等价**（只有一种语言时 `Find` 恒命中）⇒ **可先记账、不阻塞**，但**判据要写真**。

#### `GetRewards()` / `IsRead()` / `RedeemableAt()` / `StartDate()` — 4 条 **[A] 一致**
| 方法 | 真体 | 账 | 我们 |
|---|---|---|---|
| `GetRewards()` | `return this.<0x18>.<0x80> ?? Array.Empty<RewardInfo>();` | 「Msg.Rewards」✓ | `InboxData.cs:223 HasReward` ✓ |
| `IsRead()` | `return this.<0x20>.Openned;` | 「读已读位」✓ | `InboxData.cs:231` ✓ |
| `RedeemableAt()` | 返回**静态** `DateTime`（`MinValue`） | 「基类默认 MinValue」✓ | `InboxData.cs:245` ✓ |
| `StartDate()` | `return this.<0x18>.<0x28>.<0x10>;` | 「Msg.StartMs」✓ | `InboxData.cs:238` ✓ |
**⇒ 4 条 A 保持。**

#### `ClaimRewards()` — 账 `社交_聊天.tsv:176`[A] — **判据太薄**
**真体逐句**：
```csharp
void ClaimRewards() {
    payload = new { id = this.<0x18>.<vtable+0x188>() };          // 事件/消息 id
    PlayfabWrapper.GenericCloudScriptHandler(0x3F2 /*1010*/, payload, <OnComplete 闭包>, null);
}
```
**⇒ 纯云脚本**。我们的落点 `InboxData.cs:301 TryClaim` 写的是「云脚本发奖 → Wallet.Grant …」—— 实现侧没问题，
但**账要写清**：`ClaimRewards` 本身**一句本地逻辑都没有**，本地等价物**全在回调**（见下条）。

#### `OnComplete(CollectedRewardsResult rewards)`（`<ClaimRewards>g__OnComplete|7_0`）— 账 `社交_聊天.tsv:184`[A] · `补读.tsv:625`[A]
**真体逐句**（`R/…_ClaimRewards_g__OnComplete_7_0__15897f0.c`）：
```csharp
void OnComplete(CollectedRewardsResult rewards) {
    this.<0x20>.<0x30> = TimeHelper.ServerTime;                        // ★ 存档 claimedAt = 服务器时间
    if (rewards != null) {
        RewardService.Collect(rewards.<0x10>, true, false, false, true, false, true, false);   // ★ 7 个 bool 旗标
        GameAnalytics.RecordTransaction(<可选 payload>, 2, rewards.<0x10>, 0, GetID());
    }
}
```
**账现状**：「逐条发奖 + 开领奖窗 + 落盘」—— 方向对，但**漏了 `claimedAt = ServerTime` 与 7 个旗标**。
**⇒ 判据太薄，订正即可（实现侧只要确认 `claimedAt` 写的是**服务器时间**而不是本地时间）。**

---

### 2·5 GameNewsEvent（7 方法 / 11 行）

**产物**：`R/GameNewsEvent_object__object_.{GetBaseData,GetBaseSave,SelectEvent,IsRead,StartDate}__*.c`
（`IsRead`/`StartDate` 的产物地址与 `BaseInboxEvent` **完全相同** ⇒ ICF 折叠，体一样）

| 方法 | 真体 | 账 | 结论 |
|---|---|---|---|
| `GetBaseData()` | 纯取值器：`return (TData)`〈共享字段读取器〉`(this, 〈字段句柄〉)` + 型检查（与 `ShopStore` / `PlayEvent` 同一段共享体，ICF） | [A] | **A 保持**；我们 `GameNewsEvent.cs:98` = `Data` ✓ |
| `GetBaseSave()` | 同上（同一段共享体） | [A] | **A 保持**；`GameNewsEvent.cs:101` ✓ |
| `IsRead()` | `return persistence.Openned`（**`new` 遮蔽**，不是 override） | [A] | **A 保持**；`GameNewsEvent.cs:107` ✓ |
| `StartDate()` | `return persistence.<0x28>.<0x10>` | [A] | **A 保持** ✓ |
| `SelectEvent()` | 见下 | [A] | **需补本地半句** |
| 🔴 `GetTitle()` | **`abstract`**（ILSpy `GameNewsEvent.cs:56`） | [A] | **判据订正**：无体，实现在具体子类 |
| 🔴 `GetMessage()` | **`abstract`**（`GameNewsEvent.cs:58`） | [A] | 同上 |

> ⚠️ 这也解释了为什么 `INDEX.tsv` 里 `GameNewsEvent.GetMessage/GetTitle` **没有产物** —— **不是因为没解出来，是因为它们是抽象方法**。

#### `SelectEvent()` — 账 `日常_奖励.tsv:286`[A] · `补读.tsv`（无）
**真体逐句**（`R/GameNewsEvent_object__object_.SelectEvent__1a65eb0.c`）：
```csharp
void SelectEvent() {
    var p = this.<0x20>;
    if (p.Openned) return;                            // ★ 已读 ⇒ 什么都不做
    p.<0x38> = TimeHelper.ServerTime;                  // ★ 写「首次打开时刻」
    UiBadgeNotificationManager.Refresh<InboxBadge>();   // ★ 徽记重算
    var d = this.<0x18>;
    if (d != null) {
        if (d is LocalInboxMessageData && d.<0x90> != 0) return;
        payload = new { id = d.<vtable+0x188>() };      // GetID()
        PlayfabWrapper.GenericCloudScriptHandler(0x3F3 /*1011*/, payload, null, null, null);
    }
}
```
**我们**：`GameNewsEvent.cs:146 SelectEvent()` 走 `base.SelectEvent()` + 日志。**本地三句**（`Openned` 早退 / 写时刻 / 刷新徽记）在不在，
**必须逐句核 `Shell/LiveOpsEvent.cs:1326` 的 `base.SelectEvent()`**（本代理**没核**那个文件）。
**⇒ 结论：需核+可能需补；判据是上面 5 句。** 落点文件 `GameNewsEvent.cs` **空闲**。

---

### 2·6 ScoringEvent（4 方法 / 7 行）

| 方法 | 真体 | 账 | 结论 |
|---|---|---|---|
| `GetBaseData()` | 纯取值器 | [A] | **A 保持**；`ScoringEvent.cs:220` ✓ |
| `GetState(milestone)` | 见下 | [A] | **判据可加「三档常量」** |
| `IsMilestoneCollected(m)` | 见下 | [A] | **判据可加「背包/货币两支」** |
| 🔴 `PurchaseMilestone(...)` | **`abstract`**（ILSpy `ScoringEvent.cs:47`） | [A] | **判据订正**：体在 `ExpansionPassEvent` / `RaidEvent` / `SinglePlayerScoringEvent` |

#### `GetState(ScoringMilestone milestone)`
**真体逐句**（`R/ScoringEvent_object__object_.GetState__1244840.c`）：
```csharp
int GetState(m) {
    if (m == null) return 0;                        // ★ Locked
    if (this.<vtable+0x578>(m)) return 3;            // ★ IsMilestoneCollected ⇒ Collected(3)
    if (ScoringMilestone.IsUnlocked(m, this.<vtable+0x558>())) return 2;  // ★ PlayerScore ⇒ Unlocked(2)
    return 0;                                        // ★ Locked
}
```
**账/我们**：`ScoringEvent.cs:15-22` 文件头已写「三档 `Locked / Unlocked / Collected`」✓ **一致**，**常量 0/2/3 可补进账**。

#### `IsMilestoneCollected(m)`
**真体逐句**（`R/…IsMilestoneCollected__12448c0.c`）：
```csharp
bool IsMilestoneCollected(m) {
    var r = m.<vtable+0x188>().First();               // ★ 里程碑第一件奖励
    var item = r.<vtable+0x208>();                    // ★ .Item
    if (item is <A 型>) return InventoryManager.Instance.HasItem(item);          // 背包查
    if (item is <B 型>) return <货币/数量判定>;                                   // 货币查
    …
}
```
**我们**：`ScoringEvent.cs:173-197` 已写「要问背包」+「本机查不到就一律 false 并出声」✓ 方向一致。
**⇒ 判据可补「两支：背包 / 货币」。**

---

### 2·7 ShopStore（6 方法 / 9 行）

**产物**：`R/ShopStore_object__object_.{GetBaseData,GetBaseSave,GetPurchaseCount,Initialize,RemoveFinishedOffer,TryPurchase}__*.c`

| 方法 | 真体 | 账 | 结论 |
|---|---|---|---|
| `GetBaseData()` | 纯取值器：`return (TData)` **〈共享字段读取器〉(this, 〈字段句柄〉)** + 型检查（编译产物长得像 `AuthenticationValues.get_AuthGetParameters`，那是 Ghidra 的误命名）⇒ **不是 `this.<偏移>`** | `商店:292`[A] / `补读:33`[D] | **A**（§一·3） |
| `GetBaseSave()` | 纯取值器 | [A] | **A 保持** |
| `GetPurchaseCount(id)` | 读本地计数表 | [A] | **A 保持**（需逐句核我们那份，本代理未读） |
| `Initialize()` | 长循环：`Shops.GetAllOffers(handler)` → 逐条 `LoadableReference.Reference != null` 才建店；含 `do/while` 跳过空引用 | [A] | **判据太薄**（账写「JSON 解出来」）；落点 `ShopData.cs` 是**静态表** ⇒ ⚠️ 行为差异要记 |
| `TryPurchase(offer, onComplete, onError)` | 云脚本（`0x?`） | [A] | **A 保持**（购买本身走服务端） |

#### 🔴 `RemoveFinishedOffer()` — 账 `商店.tsv:296`[**C**]
**真体逐句**（`R/ShopStore_object__object_.RemoveFinishedOffer__1253e50.c`，**与 `_tmp_view/decomp_b91` 那份一致**）：
```csharp
void RemoveFinishedOffer() {
    if (this.offers == null) return;                       // (NRE in practice)
    for (int i = offers.Count - 1; i >= 0; i--) {           // ★ 倒序（避免删后跳位）
        if (!offers[i].IsActive())                          // ★ 接口虚分派 `IsActive()`
            offers.RemoveAt(i);
    }
}
```
- `IsActive()` = `LiveOpsEvent.IsActive()` = `!(ActivationType == OneTime && save.IsCompleted) && baseSave.IsActive`。
**账现状**：5 句已解 ✓ **一致**。**我们**：`ShopData.cs:247 Offers` 静态表，**没有 `IsActive` 这一维**。
**⇒ 结论：C 保持。** 真缺口 = 报价表缺「时间窗 / one-time 完成位」；⛔ 别拿 `Available == 0` 顶（`B93_N5` 已钉）。
落点 `ShopData.cs` 归 **S1**（占用）。

---

### 2·8 RankedEventBase（4 方法 / 7 行）

| 方法 | 真体 | 账 | 结论 |
|---|---|---|---|
| `GetArmyState(...)` | ICF 折叠成 `return 0;`（`<>f__AnonymousType31__GetHashCode`）⇒ 基类恒 `None` | [A]「基类恒 None」 | **A 保持** ✓ |
| `GetBaseData()` | 纯取值器 | [A] | **A 保持** ✓ |
| `ToggleRankedMode(bool)` | 见下 | [A] | **A 保持** ✓ |
| 🔴 `GetDivisionByPoints(long)` | **`out_b94` 无产物；真身 = `IRankedEvent` 的【接口默认实现】**（`decomp_full/Everguild.LiveOps.IRankedEvent__GetDivisionByPoints.c`）。逐句：`var ev = this ?? GameModes.GetActiveEvent<IRankedEvent>(); if (ev != null) ev.<slot 8>(points);` | [A] | **判据订正 + 需改实现** |

#### `ToggleRankedMode(bool isRanked)`
**真体逐句**（`R/…ToggleRankedMode__1185570.c`）：
```csharp
void ToggleRankedMode(bool isRanked) {
    this.IsRanked = isRanked;                              // ★ vtable +0x7e8（属性 setter）
    GameStaticData.unrankedMultiplayer = !isRanked;         // ★ 静态 bool 写反值
    PlayerDataManager.SavePlayerData();                    // ★ 落盘
}
```
**账**：「三句 `IsRanked = x; GameStaticData.unrankedMultiplayer = !x; SavePlayerData();`」✓ **逐字一致**。
**我们**：`RankedEventBase.cs:196` + `RankedData.ToggleRankedMode`（`Shell/RankedData.cs:345`）。**⇒ A 保持。**

#### `GetDivisionByPoints(long points)` — 见上表
**⇒ 需改实现**：落点应是「取当前排位活动 → 转调它的 `GetDivisionByPoints`」，不是自己算。
`RankedEventBase.cs` 归 **X1**（占用）。

---

### 2·9 PlayEvent（24 方法 / 43 行）

**产物**：`R/PlayEvent_object__object_.*__*.c`（26 份）

#### 已读真体并**确认判据**的（A 保持）
| 方法 | 真体要点 |
|---|---|
| `GetBaseData()` | 纯取值器：`return (TData)`〈共享字段读取器〉`(this, 〈字段句柄〉)`（`DraftEventBase` 那一版就是转调它）。**A** |
| `GetBaseSave()` | 同上（`DraftEventBase` 那一版转调它）⇒ **A** |
| `get_Handler()` | `return this.<0x10>;`（+ 型检查）⇒ **A** |
| `get_ShowInMainMenu()` | `return base.get_ShowInMainMenu() && base.get_EventState() == 2;` ⇒ **A** |
| `get_UseMatchMaking()` | ICF 折叠 = `return true;`（`BaseAddressableLibrary__ValidateLibraryLoad`）⇒ **基类恒 true** ⇒ **A**（建议把「恒 true」写进账） |
| `CancelMatch()` | 建 `LastOpponentInfo` + `CustomDebug.Log` + 云脚本 ⇒ **A（本地那半句有）** |
| `CheckPreviousMatch()` | `CustomDebug.LogWarning` + `MenuActions.QueueAction` + `new RankedMatchResult` ⇒ **A** |
| `GetEventDeck(CardArmy)` | 含 `CardDeck.op_Inequality` + `CardDeck.GameMode` 判断 ⇒ **A** |
| `GetEventDeck()` | 同上 + `DeckUtility.ValidateDeck` ⇒ **A** |
| `ValidateEventDeck(deck, validateOwnership)` | `DeckUtility.ValidateDeck` + `GameStaticData.CheckHiddenCardsInDeck` ⇒ **A** |
| `StartMatch()` | `WindowsManager.ShowPopUp` + `DeckUtility.HasDefensiveCards` ⇒ **A** |
| `StartMatchNoCheck(deck)` | `GameModes.set_CurrentPlayingEvent` + `AlliancesManager.IsInGroup` ⇒ **A** |
| `SetDefaultDeck(deck, upload)` | 云脚本（`CloudHandle`）+ `DamageSignal` ⇒ **A（本地半边 + 云脚本半边）** |
| `b__17_0()` | `CustomDebug.Log` ⇒ **A** |
| `b__26_0(deck)` | `CardDeck.GameMode` + `DeckUtility.ValidateDeck` ⇒ **A** |
| `ChangeState(...)` / `Initialize()` / `MatchFinished(...)` / `OnMatchDraw/OnMatchLost/OnMatchWon()` | **无外部调用（纯状态赋值/回调）** ⇒ 与账「状态机 + 回调」一致 ⇒ **A** |
| `OnMatchFinished(...)` | **空体**（`0x4B33B0` 全局空桩）⇒ 账 [D]「原版方法体为空」**✓ 正确** |
| `UploadDefaultDeck()` | 云脚本 `200` + `IsNullOrEmpty(deck.<0x38>)` 守卫 ⇒ **D 正确** |
| `SetCustomMatchOptions(...)` | **空体**（`0x4B33B0`）⇒ 账 `日常:568` [A] 与 `补读:252` [D] **都对**：原版**没有可观测行为**。**⇒ 统一判 D 并写明「空体」，两处同键要并成一条** |

#### 🔴 `MakeOpponentInfo(MatchData matchData)` — 账 `日常_奖励.tsv:561`[B]
**真体逐句**（`R/PlayEvent_object__object_.MakeOpponentInfo__11273c0.c` + ILSpy `PlayEvent.cs:871-…` 拿字段名）：
```csharp
LastOpponentInfo MakeOpponentInfo(MatchData matchData) {
    var r = new LastOpponentInfo();
    r.<0x10> OpponentId     = matchData.opponentId;                                    // ★ 守卫 matchData != null
    r.<0x18> OpponentName   = matchData.enemyRawBattleData.deckName;                    // ★ 守卫非 null
    r.<0x20> PlayerHeroId   = matchData.playerRawBattleData.deckWarlord.uniqueId;        // ★ 守卫非 null
    r.<0x28> OpponentHeroId = matchData.enemyRawBattleData.deckWarlord.uniqueId;         // ★ 守卫非 null
    r.Result = 0;                                                                       // ★ 恒 0
    r.<0x58> RankedGame     = <虚调用 slot 4>(…, classRef) != 0;                        // ★ bool
    r.<0x44..0x4B> Season   = new int?((<虚调用 slot 1>(…)))                            // ★ Nullable<int>
    return r;
}
```
- 🔑 **`key 4 / key 1` 不是 `PlayerDataKey`**！`0x1800021F0` 是 **IL2CPP 泛型虚调用助手**（`FUN_1800021f0(slot, class, obj, …)`，见 `decomp_full/Extensions__Merge.c` 同款用法）⇒ **它是 vtable 槽号**。
- 那两个虚调用挂在 `thunk_FUN_18042e360(this, classRef)` 的返回对象上；该 helper 本代理**没认出**是不是 `get_Handler()`（`get_Handler` 的体是 `return this.<0x10>`，对不上）⇒ **残差仍在**。
- 账说「四格 id + Result = 0 逐格对上」✓ **一致**；「头像不是这个类的字段」✓ **正确**（`LastOpponentInfo` 12 格无头像）。
**⇒ 结论：B 保持**；**判据订正**：把「key 4 / key 1」改写成「vtable 槽 4 / 槽 1（经 `0x1800021F0` 分派）」，并写明**不是 PlayerDataKey**。
落点 `Shell/PlayEvent.cs:557`，归 **S2**（占用）。

---

### 2·10 DraftEventBase（33 方法 / 58 行）— **最大一块**

**产物**：`R/DraftEventBase_object__object_.*__*.c`（33 份）+ 21 个 lambda（`INDEX` 里标「重复：decomp_full 已有」的一律过滤）

#### A. 纯取值器 / 无外部调用 ⇒ 判据一致
| 方法 | 真体 | 账 | 结论 |
|---|---|---|---|
| 🔴 `GetBaseData()` | 纯取值器：`return (TData)` **`base.GetBaseData()`**（体里转调 `0x1126CC0` = `PlayEvent.GetBaseData`，再做一次型检查） | `补读:755`[D] / `轮抽:9`[B] | **→ A**（§一·3）；我们 `DraftEventBase.cs:346-352` **文档就是「走基类再判型」**、实现 `return Data` ✓ |
| 🔴 `GetBaseSave()` | 纯取值器：`return (TSave)` **`base.GetBaseSave()`**（转调 `0x1126D60` = `PlayEvent.GetBaseSave`） | `补读:756`[D] / `轮抽:10`[A] | **→ A**；`LiveOpsEvent.cs:752 GetBaseSave` ✓ |
| `OnMatchProcessSuccess(result)` | **空体**（`0x4B33B0`） | 两处 [D] | **D 保持 ✓**（三源一致已由 `B91_P4` 做过） |
| `get_DraftState()` | ICF 折叠 = `return this.<0x28>;` | [A] | **A** ✓ |
| `get_CurrentStage()` | `return this.<0x20>.<0x48>;` | （账无此行） | 建议**补一行**（纯取值器） |
| `get_EventPlayMode()` | ICF 折叠 = `return 7;`（`System_Int16__GetTypeCode`）⇒ **基类恒 7** | （账无此行） | 建议**补一行**（原版 bug 级事实：基类恒 7，子类各自覆盖） |

#### B. 🔴 `GetMatchmakingFilter(currentElo, tryCount, gamesLost)` — 账 `轮抽.tsv:13`[D] / `补读.tsv:758`[A]
**真体逐句**（`R/…GetMatchmakingFilter__1810840.c`）：
```csharp
string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost) {
    if (this == null || this.<0x20> == null) throw;              // NRE
    int won = this.<0x20>.<0x48>;                                 // ★ save.numberOfMatchesWon
    int n   = tryCount % 3;                                       // ★ 半径
    string s = $"C1 < {won + 2*n} AND C1 > {won - 2*n}";          // ★ C1 = 胜场窗口
    s += " AND C2 != \"" + PlayerDataManager.singletonManager.notMatchWithPlayer + "\" ";   // ★ 引号+尾空格
    if (!FeatureConfig.ShouldDisableNewMatchMaking())
        s += $" AND C5 == {gamesLost}";                            // ★ C5
    return s;
}
```
- 🔑 **`currentElo` 确实零读点**：产物只有 `this + 3 个形参槽`、**没有 MethodInfo 槽**（`param_2` 从未被解引用；若它是 MethodInfo，方法体必然要读它取泛型实参）⇒ `param_2 = currentElo`（未用），`param_3 = tryCount`，`param_4 = gamesLost`。**B91_P4 的读法成立。**
**账现状**：判据要点**完全正确**；但**状态 D 已过时**（`备注` 自己说「A3 本轮正按新体重写」—— 已重写）。
**我们实现**：`Shell/DraftEventBase.cs:172-185` **逐句等价**（含 `" AND C2 != \""` 的引号与尾空格、`C5` 条件）。
**⇒ 结论：`轮抽.tsv:13` D → **B**（消费者是 PlayFab 匹配器 = P3，但**方法本身是纯串函数、已 100% 复刻**）；若调度台按「纯本地函数已闭环」判，可直接给 **A**。**
`补读.tsv:758` 的 A **保留**。⛔ 两处必须收敛成一条。

#### C. 🔴 `PickWarlord(warlord, onSuccess, onFail)` — 账 `轮抽.tsv:24`[B] — **旧判据错**
**真体逐句**（`R/…PickWarlord__1813050.c`）：
```csharp
void PickWarlord(RawCardScript warlord, Action onSuccess, Action onFail) {
    this.<0x20>.<0x88> = warlord.Reference;                       // ★★ 本地先写：eventData.<0x88> = 战将引用
    payload = new { id = GetID(), warlord = this.<0x20>.<0x88> };  // ★ 再把刚写进去的那格打包
    PlayfabWrapper.GenericCloudScriptHandler(0x369 /*873*/, payload, onSuccess, onFail);
}
```
**账现状（错）**：`B93_N3` 写「ILSpy 全文只有一句云脚本 ⇒ 校验/写存档在服务器」—— **漏了 `eventData.<0x88>` 这一句本地写**。
**我们**：`DraftModeWindow.ChooseWarlord`（`:3247`）只存 `_warlordId` + 刷表现，**没有**等价的那格本地写（我们的数据模型里是 `_run` 而不是 `eventData.<0x88>`）。
**⇒ 结论：B 保持（云脚本那一半确实在服务器），但判据必须订正，且要补一句「本地先写 selectedWarlord」 —— 若我们的模型已有等价格，则这一半算 A。**

#### D. `ValidateEventDeck(deck, validateOwnership)` — 账 `轮抽.tsv:33`[B] — **原判据太薄且漏关键点**
**真体逐句**（`R/…ValidateEventDeck__1814330.c`）：
```csharp
bool ValidateEventDeck(CardDeck deck, bool validateOwnership) {
    if (this == null) throw;
    if (!base.ValidateEventDeck(deck, /* ★ 硬编码 */ false)) return false;   // ★ 形参 validateOwnership 被忽略！
    if (this.<0x20> == null || this.<0x20>.<0x90> == null) throw;
    int need = this.<0x18>.<0x98>.Count - 1;                       // ★ 已选卡数 − 1
    return need <= this.<0x20>.<0x90>.Count;                        // ★ 不得超过事件允许的卡数
}
```
**账现状**：「只校验卡都在本活动内」—— **漏了 ① `validateOwnership` 恒传 false；② `Count-1 <= 上限` 这条守卫**。
**我们**：`DraftPacks.TryBuildClosedDeck`（`:405`）只校验「4 包 / 12 张」⇒ **不是同一件事**（缺 `Count-1` 那条与 `validateOwnership=false` 的语义）。
**⇒ 结论：B 保持；判据订正 + 需改实现。**

#### E. 🔴 `GetPossibleWarlords()` — 账 `轮抽.tsv:14`[B] — **「体本机读不出」已解除**
**真体逐句**（`R/…GetPossibleWarlords__18109d0.c`，16 KB）：
```csharp
List<RawCardScript> GetPossibleWarlords() {
    // ── 候选源三分支 ──
    if (save.<0xb8> != null && save.<0xb8>.Count >= save.<0xa0>)
        src = save.<0xb8>.Where(<b__20_2/+0x190 谓词>);                       // ① 用存档里的「指定战将表」
    else if (save.<0xc0> != null && save.<0xc0>.Count >= 1)
        src = <Manager>.Instance.<0x20>.Where(w => <阵营集合>.Contains(w.cardArmy));  // ② 用「指定阵营」过滤全部战将
    else
        src = <Manager>.Instance.<0x20>;                                       // ③ 不筛，全部
    var result = src.ToList();   // （②③ 走 Where→ToList；① 走 new List(Where(...))）
    // ── 倒序三重淘汰 ──
    for (int i = result.Count - 1; i >= 0; i--) {
        var w = result[i];
        if (w.<0x4c> == 10)          { result.RemoveAt(i); continue; }   // ★ 淘汰 A（某枚举 == 10）
        if (w.<vtable+0x2a8>())      { result.RemoveAt(i); continue; }   // ★ 淘汰 B（某 bool 属性）
        if (w.IsAlternateArt())      { result.RemoveAt(i); continue; }   // ★ 淘汰异画
    }
    // ── 黑名单 ──
    if (save.<0xd0> != null)
        foreach (var r in save.<0xd0>) result.Remove(r.Reference);        // ★ 剔除黑名单战将
    return result;
}
```
**账现状**：`B93_N3` 写「方法体本机读不出」「三个集合的字段名解不出」—— **前半句已解除**（真体在手）；
后半句**仍然成立**：`save.<0xb8>/<0xc0>/<0xa0>/<0xd0>` 与 `w.<0x4c>`、`<vtable+0x2a8>` 的**字段名没解**。
**可选字段（按 `DraftEventSaveGame` 的声明序 + `DraftEventData` 的四格）**：`selectableWarlords` / `selectedPacks` / `selectedCards` / `selectedWarlord`；`specificWarlords(+0xB8)/specificArmies(+0xC0)/guaranteedArmies(+0xC8)/blacklistedWarlords(+0xD0)` —— ⚠️ **这是候选，⛔ 未证**。
**⇒ 结论：B 保持**（判据已从「读不出」升级为「读得出、只剩字段名」）；**建议**：把 `DraftEventBase` 的**类型字段表**从 `dump.cs` 再钉一遍（一次就够）。

#### F. 🔴 `GetEventDeck()` — 账 `轮抽.tsv:12`[B]
**真体逐句**（`R/…GetEventDeck__1810280.c`）：
```csharp
CardDeck GetEventDeck() {
    if (this.<0x38> != null) {                                     // ★ 已有缓存 deck
        this.<0x38>.<0x40> = eventData.<0x88>(=selectedWarlord 引用解出);
        this.<0x38>.<0x58> = eventData.<0x98>.Where(<b__24_0/b__24_1>).ToList();
    } else {
        var d = new CardDeck(selectedWarlord, eventData.<0x98>.Where(...).ToList(), <…>);
        d.<0x28> = eventData.<0xa8>;                                // ★ 卡组名
        d.<0x70> = (int?)7;                                         // ★ 恒 7（Nullable<int>）
        this.<0x38> = d;
    }
    if (string.IsNullOrEmpty(this.<0x38>.<0x28>)) {                  // ★ 名字空 ⇒ 补卡背
        int army = warlord != null ? warlord.<0x2c> : 10;
        this.<0x38>.<0x28> = ArmyUtilities.GetDefaultCardback(army).<0x38>;
    }
    return this.<0x38>;
}
```
**账现状**：「把已选卡拼成出战卡组」—— 太薄。**我们**：`DraftPacks.BuildClosedDeck`（`:394`）只做「4 包 → 12 张」。
**⇒ 结论：B 保持；判据太薄（要写：缓存复用 / 两个更新点 / `(int?)7` / 卡背兜底）。**

#### G. 🔴 `Validate(TS save)` — 账 `轮抽.tsv:32`[A] / `补读`（无）
**真体逐句**（`R/…Validate__18143f0.c`）：
```csharp
bool Validate(TS save) {
    if (save == null || save.<0x90> == null) throw;
    if (save.<0x90>.Count > 0) {                       // ★ 选过东西
        if (save.<0x98>.Count == 0) {                  // ★ 但卡表是空的 ⇒ 修复
            var cards = GetCardsFromPacks(save);
            SaveCards(this, save, cards, save.<0x10>, null, null);
            CustomDebug.LogWarning("<前缀>" + save.<0x10> + "<后缀>");
        }
    }
    return true;                                        // ★ 恒 true
}
```
**账**：「`Validate(TS save)`」太薄。**我们**：`DraftModeWindow.cs:786 LoadRunFromDisk` → `DraftRunRepair.Validate`（`:4382`）。
**⇒ 结论：A 可能**过度**（这是「存档修复」不是「校验」）——**判据订正为「修复：选过卡但卡表空 ⇒ 用包重建 + 警告」**，实现要核 `DraftRunRepair.Validate` 是否同形。落点 `DraftModeWindow.cs` 归 **S4**。

#### H. 🔴 `UpdateState()` — 账 `轮抽.tsv:31`[A] / `补读`（无）
**真体逐句**（`R/…UpdateState__1814090.c`，jumptable 未展开但主控制流可读）：
```csharp
void UpdateState() {
    var data = this.<0x20>;  var save = this.<0x18>;
    // ── 先判「是否已经走到终态」──
    if (data.<0x30> == 2) goto final;
    if (save.<0x90> <= data.<0x4c>) goto final;
    if (save.<0x88>.Count <= data.<0x48>) goto final;
    // ── 中间态 ──
    if (data.<0x88>(selectedWarlord) == null) {
        data.<0x98>.Clear();                      // ★ 清空已选卡
        this.<0x38> = null;                       // ★ 清 deck 缓存
        SetState((data.<0x30> == 1 && data.<0x78> != 0) ? 5 : 0);
    } else if (data.<0x90>.Count < save.<0x98>) SetState(10);
      else                                      SetState(15);   // ★ OngoingRun
    return;
final:
    CustomDebug.Log(…);
    SetState(data.<0x70> ? 22 : 20);              // ★ 终态 20 / 22
}
```
**账**：一行「状态机」。**我们**：落点 `DraftModeWindow.cs`（需核）。
**⇒ 结论：判据太薄，必须展开成 5 个状态常量（0 / 5 / 10 / 15 / 20 / 22）。**

#### I. 其余已读真体（**判据一致或太薄，不改实现**）
| 方法 | 真体要点 | 账 | 结论 |
|---|---|---|---|
| `Expire(onComplete)` | 关窗 → `if (DraftState < 5 && !AlliancesManager.HasJoinedEvent(id)) base.Expire(**onComplete**)` → 云脚本 `0x36D`（`DraftState == 0x16`）否则 `Log`+`onComplete()` | `轮抽:6`[B] | **B 保持**（可达性缺口）；🔴 **判据小订正：账里写 `base.Expire(null)`，真体传的是 `onComplete`**（`*(lVar2+0x18)` 就是 `param_2`） |
| `SelectEvent()` | `if (DraftState == 0x16) 云脚本 0x443 {id} else cb.Invoke()` | `轮抽:29`[A] | **A**，判据太薄 |
| `ResetCollectedDraft(onComplete)` | 同 `SelectEvent` 的形状（`0x16` ⇒ 云脚本 `0x443`；否则 `onComplete()`） | `轮抽:27`[A] | **A**，判据太薄 |
| `CollectRewards(onSuccess,onError)` | **纯云脚本** `0x366 {id}` | 两处 [D] | **D 正确**（用户 2026-10-24「轮抽不做奖励」+ 纯服务端） |
| `RefundRun(onComplete,onError)` | `if (save.IsPremium) 云脚本 0x445 {id} else onComplete(Array.Empty)` | `轮抽:26`[A] / `补读:764`[D] | **A**（我们的 `DraftEventBase.cs:242` **逐句等价**，含「非付费档回空表」） |
| `PayEventEntrance(is10X,onS,onE)` | `payload = {id, is10X, Where(GetRandomWarlords())}` → 云脚本 `0x367` | 两处 [D] | **D 正确**（纯服务端 + 不做真实经济） |
| `FreeEventEntrance(onS,onE)` | 云脚本 `0x368` | `轮抽:8`[A]/`补读:761`[A] | **A**（本地半句 = 回调分工） |
| `FinishRun(onFinish)` | 云脚本 `0x36D` + `CustomDebug.Log` | `轮抽:7`[A]/`补读`[A] | **A** |
| `SaveCards(...)` | 云脚本 | `轮抽:28`[A] | **A** |
| `OnMatchFinished(won,result,cb)` | `GameModes.LossStreak` 读+写 + 云脚本 | `轮抽:18`[A]/`补读:246`[A] | **A**（**本地连败计数**这一半要核） |
| `OnMatchWon()` | `PlayerDataManager.UploadOnlineDemoData()` | `轮抽:21`[A]/`补读:249`[A] | **A** |
| `OnMatchFound(name)` | `base.OnMatchFound(name)` + 云脚本 `0x36B {id, eventDeck.<0x58>}` | `轮抽:19`[D] / `补读:748`[A] | **A**（基类那句是本地：`CustomDebug.Log`） |
| `GetRandomWarlords()` | `RandomSeedProvider.InitSeed(save.<0x18>, **5**, eventData.<0x40>)` → `GetPossibleWarlords()` → `IsPremium` 分支 | `轮抽:16`[A] | **A**；🔑 **种子 = 存档字段 + 盐 5 + 事件字段**，不是随机 |
| `PickPack(pack,...)` | `PlayerDataManager.fullCardCollection` + `PrebuiltPack.GetCardLibrary` + 云脚本 | `轮抽:23`[A] | **A**（本地那两句要核） |
| `ReRollPack(onS,onF)` | 云脚本 `0x45C` | `轮抽:25`[A] | **A** |
| `SetCustomMatchOptions(options, elo)` | 见下（**本地写 Photon 房间属性**） | 两处 [D] / `补读:759` 已改 B | **B**（我们**没有**等价物） |
| `b__9_0()` | 转调 `0x1810171C0`（ILSpy 也 `Method not found`） | `轮抽:36`[D] | **判不了** —— 差「`0x1810171C0` 是谁」 |
| `b__19_1` / `b__20_1` | lambda（`wl => …` / `warlord => 阵营集合.Contains(warlord.cardArmy)`） | [A] | **A 保持** |
| `LoadEventData` / `GetCardsFromPacks` / `GetRandomPack` / `FinishRun` | 已读，**无外部调用**（纯状态/LINQ） | [A] | **A 保持** |

#### `SetCustomMatchOptions(options, currentELO)`（DraftEventBase）
**真体逐句**（`R/…SetCustomMatchOptions__1813cb0.c`）：
```csharp
void SetCustomMatchOptions(RoomOptions options, int currentELO) {
    base.SetCustomMatchOptions(options, currentELO);              // ★ 基类是空体（0x4B33B0）
    int won = this.<0x20>.<0x48>;                                  // numberOfMatchesWon
    int lossStreak = LiveOpsManager.GetHandler().LossStreak;        // GameModes.LossStreak
    if (currentELO <= <静态 int @+0x264> && won < 3) currentELO = 0;
    var ht = options.CustomRoomProperties;
    ht["C1"] = won;                                                // 键名 DAT_18429e1f8
    ht[<键>] = currentELO;                                          //      DAT_18429e6d0
    ht[<键>] = lossStreak;                                          //      DAT_18429e7c8
    options.CustomRoomPropertiesForLobby = new[]{"C1", <..>, <..>, <键>, <键>};   // ★ 恰好 5 个键
}
```
**⇒ 结论：`补读.tsv:759` 的 D→B 裁定**正确**（真体纯本地）；`轮抽.tsv:30` 应同步改 B/合并。** 我们**没有**等价物（`Net/NetRuntime` 的房间参数是另一套）。
落点 `NetRuntime.cs` **空闲**。

---

### 2·11 轮抽.tsv 剩余「账太薄」项（不改判据主体，补文字即可）
| 账行 | 现状 | 建议补写 |
|---|---|---|
| `轮抽:9` GetBaseData | 「类型化数据」 | 「纯取值器：`return (TData)base.GetBaseData()`（+ 型检查），无副作用」 |
| `轮抽:10` GetBaseSave | [A] | 「纯取值器：`return (TSave)base.GetBaseSave()`（+ 型检查）」 |
| `轮抽:17` LoadEventData | [A] | 「从 (data, save, handler) 三件把本局状态装回来」 |
| `轮抽:34/35` b__20_1 / b__19_1 | [A] | 写清 lambda 体（见 §2·10 E） |

---

## §三 可直接派的施工单（按**落点文件**分组）

> 判「空闲」的判据 = `资料/完全复刻/文件所有权_当前.md`（**2026-10-29 波 1**：F1–F5 / S1–S4）+ 简报里的波 1.5 名单（X1 / X2 / Y1 / Z1 / V1）。
> ⚠️ **波 1 与波 1.5 有重叠**（`DraftEventBase.cs` / `GameModesHandler.cs` / `MainMenuRuntime.cs` 同时在 **S4** 和 **X1` 名下）——
> **调度台必须先裁决归谁**，⛔ 别两头派。

| # | 落点文件 | **现在能派吗** | 该做什么（判据 → 本文件哪一节） | 涉及账行 |
|---|---|---|---|---|
| **D1** | `CardPresentation/Shell/InboxData.cs` · `BaseInboxEvent.cs` | ✅ **空闲** | ① `GetTitle`/`GetMessage` 加**三处运行时 `Replace`**（`[[RewardName]]`=首个奖励 `Item` 名 / `[[PlayerName]]`=`PlayerDataManager.playerName` / `[[AllianceName]]`=`AlliancesManager.CurrentPlayerAllianceName`）；② `GetMessageContent` 加**语言条目 + 回退**；③ `OnComplete` 的 `claimedAt` 用**服务器时间** | 社交:176–184 · 补读:618–625 |
| **D2** | `CardPresentation/Shell/GameNewsEvent.cs` | ✅ **空闲** | 核/补 `SelectEvent` 的**本地三句**（`Openned` 早退 / `p.<0x38>=ServerTime` / `Refresh<InboxBadge>`）；把 `GetTitle`/`GetMessage` 标成 **abstract** | 日常:281–287 · 补读:179–182 |
| **D3** | `CardPresentation/Shell/ScoringEvent.cs` | ✅ **空闲** | 判据补常量（`Locked=0 / Unlocked=2 / Collected=3`）；`PurchaseMilestone` 标 **abstract** | 日常:692–695 · 补读:258–260 |
| **D4** | `CardPresentation/Net/NetRuntime.cs` | ✅ **空闲** | 补 `DraftEventBase.SetCustomMatchOptions` 的等价：3 个房间属性（`C1`=胜场 / ELO / 连败）+ **5 键** `CustomRoomPropertiesForLobby`；`currentELO<=<静态阈值> && won<3 ⇒ 0` | 轮抽:30 · 补读:259 |
| **D5** | `CardPresentation/Shell/LiveOpsEventWindow.cs` | ✅ **空闲** | 需先核有无活落点（2 行账指向它） | 见账 |
| **S2** | `CardPresentation/Shell/PlayEvent.cs` | ⛔ **S2 占用** | `MakeOpponentInfo`：核四格 id 名 + 把「key 4/1」改成「vtable 槽 4/1」；补 `PlayEvent.SetCustomMatchOptions` **空体**事实 | 日常:561 / 564 / 568 · 补读:241/246/252 |
| **S2** | `CardPresentation/Shell/LiveOpsFilter.cs` | ⛔ **S2 占用** | ① 新建 `ConfigProvider<T>`（`Initialize`/`UnpackInternal`/`OnDestroy`，判据见 §2·2）；② `SignalEventTrigger` 注释把「仍是推的」**删掉**（真体已证 `Register<T>(Action<T>)`） | 未分类:177–179 · 补读:576–578 · 日常:702–705 · 补读:261–264 |
| **S1** | `CardPresentation/Shell/ShopData.cs` | ⛔ **S1 占用** | `RemoveFinishedOffer`：给报价表加 `IsActive` 维（时间窗 + one-time 完成位），⛔ 别拿 `Available==0` 顶 | 商店:296 |
| **S4/X1** | `CardPresentation/Shell/DraftModeWindow.cs` · `DraftPacks.cs` · `DraftEventBase.cs` | ⛔ **S4 + X1 重叠，先裁决** | ① `GetEventDeck`（缓存复用 / `(int?)7` / 卡背兜底）；② `GetPossibleWarlords`（三分支 + 三重淘汰 + 黑名单）；③ `PickWarlord`（**本地先写 selectedWarlord**）；④ `ValidateEventDeck`（`base(deck,**false**)` + `Count-1 <= 上限`）；⑤ `Validate` = 存档修复；⑥ `UpdateState` 五状态常量；⑦ `Expire` 的 `base.Expire(onComplete)`（**不是 null**） | 轮抽:5–36 · 补读:742–767 |
| **X1** | `CardPresentation/Shell/RankedEventBase.cs` | ⛔ **X1 占用** | `GetDivisionByPoints` = **接口默认实现**（取当前排位活动 → 转调它的同名方法），不是自己算 | 个人资料:212 |
| **V1** | `CardPresentation/Core/LoadableRef.cs` | ⛔ **V1 占用** | 补 `ReferenceList<T>`（`LoadReferences` = `Where().ToList()`；`op_Implicit`/`Elements` = 懒加载） | 未分类:512–513 · 补读:605–606 |
| **新件** | `CardPresentation/Shell/MultiKeyLiveOpsHandler.cs` | ✅ 可新建（但**不必**） | `ExtractKey` 是 **abstract** ⇒ 按铁律 11 例外①，**记账即可**，不必建 | 日常:533 · 补读:238 |

**建议的派活顺序**（判据已闭合、文件空闲的在前）：
`D1(改动最大、影响玩家可见文本)` → `D3` → `D2` → `D4` → `S2 的 ConfigProvider`（S2 交回后）→ `S1 的 IsActive` → `S4/X1 的轮抽六件`。

---

## §四 还差什么（判不了 / 未闭合的，逐条给「差什么」）

| # | 项 | 差什么 | 怎么补（成本） |
|---|---|---|---|
| G1 | `DraftEventBase.GetPossibleWarlords` 的 `save.<0xb8>/<0xc0>/<0xa0>/<0xd0>`、`w.<0x4c>`、`w.<vtable+0x2a8>` | **字段名**（体已完全可读） | 从 `dump.cs` 的 `DraftEventSaveGame` / `DraftEventData` / `RawCardScript` 逐字段算偏移。⚠️ **注意 `RawCardScript` 有继承链**，偏移要按最底层算。**成本：小（读表即可）** |
| G2 | `PlayEvent.MakeOpponentInfo` 的 `RankedGame` / `Season` | 那个 helper `thunk_FUN_18042E360(this, classRef)` 返回**哪个对象**（虚槽 4/1 挂在它上面） | 反汇编 `0x18042E360`；或读 `LastOpponentInfo` 的 12 格字段名反推。**成本：中** |
| G3 | `DraftEventBase.b__9_0` 转调的 `0x1810171C0` | 该地址是哪个方法 | `decomp_full` 无、ILSpy `Method not found` ⇒ 要么 Ghidra 再导一次（只读），要么按调用点画像猜（⛔ 不许猜）。**成本：小（一次导出）** |
| G4 | `ConfigProvider` 的缓存宿主（`FUN_180002340(1/2, classRef)`） | 那个静态类是谁 | 读 `classRef` → `monoscripts` 反查类名。**成本：小** |
| G5 | `BaseInboxEvent.GetMessageContent` 的「默认语言」来源（`**(class+0xb8)`） | 是 `I2` 的默认语言还是硬编码 | 读那个静态字段的字面量。**成本：小** |
| G6 | `IRankedEvent.GetDivisionByPoints` 的**准确语义**（`FUN_180002280(8, class, ev, this)` 的第 4 实参是 `points` 还是 `this`） | IL2CPP 泛型虚分派助手的实参顺序 | 反汇编 `0x18002280`。**成本：中** |
| G7 | `DraftEventBase.Validate` / `UpdateState` / `GetEventDeck` 里 `save.<0x90>/<0x98>` 与 `data.<0x88>/<0x90>/<0x98>/<0xa0>/<0xc8>` 的映射 | 同上（字段名） | 与 G1 同一趟解决 |

**本轮没做、但值得下一波补的**：
- `INDEX.tsv` 里标 **`nofn`** 的那 1 条（`DraftEventBase.<>c.<GetRandomPack>b__18_1`，tail-call thunk）—— **不是缺口**（thunk 无独立语义）。
- `INDEX.tsv` 里 **85 行**标「重复：decomp_full 已有同名产物」的嵌套 lambda 类 —— **已按简报过滤，未花时间**。
- `PlayEvent` 还有 3 个方法本代理**只按调用符号分类、没逐句读**：`GetEventDeck()` 的第二份产物（`__1126e00` vs `__1127000`，同名重载，需按参数个数区分）、`OnMatchFound`、`StartMatch`。⚠️ **同址重名 = 必须按参数区分**（CLAUDE.md 已记 `dump.cs` 同址误判的坑）。

---

### 附 · 本文件的可复跑性
- 全部「真体逐句」都能用 `D:/2/tools/decomp_generic_trial/out_b94/<INDEX.tsv 的第 4 列>` 逐字复核。
- ⚠️ **按 `INDEX.tsv` 找文件，⛔ 别按首行函数名找**（137/210 首行是 `FUN_`、56 条首行是别人的名字，**体都是完整的**）。
- ⚠️ `out_b94` 里 `.<>c` / `__c__` 的嵌套 lambda 产物**全部**与 `decomp_full` 重复（除 `_GetPossibleWarlords_b__20_2/b__20_3` 两个是**同址 ICF**，内容一样）。
