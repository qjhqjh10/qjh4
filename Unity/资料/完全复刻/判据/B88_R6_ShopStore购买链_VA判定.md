# B88_R6 · `ShopStore.TryPurchase` 购买链 + `Store.CanPurchase` —— VA 级判定

> 会话：第八十八会话 · 查判据代理 R6（只读 + 只写本文件）
> 工具：`d:/4/Unity/工具/disasm_va.py`（capstone 直读 `D:/2/unity_run_ref/GameAssembly.dll`）·
> `D:/2/tools/il2cpp_out/{dump.cs,script.json}` · `D:/2/tools/decomp_full/` ·
> `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`（**只有这一份 Cpp2IL 产物有真方法体**）·
> `D:/2/tools/il2cpp_out/stringliteral.json`（本机未用它，改用 `script.json` 的 `ScriptString`）
> ⛔ 未改任何源码 / 账 / 正本 / `d:/2`。

---

## 〇、结论速览（三条）

| 问题 | 判定 | 判据强度 |
|---|---|---|
| **原版买的时候有没有「等待」弹窗** | **有**，且**每个购买都弹**（在 `TryPurchase` 第一句，早于任何分支） | **硬**（VA 反汇编 + ScriptString 字面量 + dev-ILSpy 源码级还原，三源一致） |
| **弹窗文案键** | **`MenuShop/WaitingPopup`**（`localizeTexts = true` ⇒ 键直接进 `LocalizationManager.GetTranslation`）。EN `Processing your purchase...` · ZH `正在处理您的购买...` | **硬**（ScriptString 地址 0x42D2BE0 实读 + `official_loc_13lang.json` 词条实读） |
| **弹窗形态** | **0 颗按钮**（两个按钮实参都是 `null`）+ **`closeOnEsc = false`** ⇒ 不可点、不可 Esc 关掉的纯「请稍候」模态 | **硬**（`ConfigurePopUp(3 参)` 会对每颗 `LiveButtons` 调 `SetActive(false)`，见 §二·3） |
| **`Store.CanPurchase`（VIP 页那个）** | **不是服务端语义** —— 是**纯本地** `DailyStore.CanPurchase`：`RequiredItem == null \|\| InventoryManager.Instance.HasItem(RequiredItem)`（= 背包里有那个道具且数量 > 0）。缺的**只是** store SO 的 `requiredItem` 这一格输入 | **硬**（`DailyStore.get_CanPurchase` 逐指令读通） |
| **分支顺序** | `ShowPopUp` → `if (offer.IsItem)` ⇒ `PurchasesManager.PurchaseItem(..., showRewardWindow: **true**)` / `else` ⇒ `offer.TryPurchaseOffer(onComplete, onError)`（`showRewardPopup = true`）。**三条回调都先 `HidePopUp(forceHide: true)` 再调调用方的回调** | 硬 |

---

## 一、`ShopStore.TryPurchase` 的分支顺序（VA + 反汇编片段）

### 1·1 方法体在哪（产物的形状先说清）

- `decomp_full/` 里**没有** `ShopStore.TryPurchase` 的 `.c` 产物，只有它的三个局部函数：
  `…ShopStore.__c__DisplayClass6_0_object__object____TryPurchase_{b__0, g__HandlePurchaseComplete_1, g__HandlePurchaseError_2}.c`。
- 真身地址（`dump.cs:134363` 的 `GenericInstMethod` 子块，**不是** 那条 `// RVA: -1`）：
  **`ShopStore<object,object>.TryPurchase` → RVA `0x1253FA0` · VA `0x181253FA0`**（泛型实例，所以主行写 `RVA: -1`）。
- 另一份**能读的源码级还原**：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/ShopStore.cs:479-575`
  —— 它把 `ShowPopUp("MenuShop/WaitingPopup", …)` 与两条分支都还原出来了（Ghidra 的 `.c` 缺失，这份是**第三源**，与反汇编逐句吻合）。

### 1·2 反汇编主干（节选，VA 逐条）

```
181253FA0  push…sub rsp,0x40                     ; 序言
181253FB2  cmp byte[rip+0x32ec2fa],0             ; 泛型静态初始化闸
181253FC4…181254024  8× lea rcx / call 1803F44F0 ; 8 个类的 class-init
18125402B  mov rbp,[rsp+0x80]                    ; 第 5 个（隐藏）实参 = 泛型 RuntimeMethod*
181254053  mov [rsp+0x60],r12 / [rsp+0x68],r15   ; 保存
181254060  call 1803F4750                        ; il2cpp_object_new(<>c__DisplayClass6_0<,>)
18125407A  call 1804B3000                        ; DisplayClass 的 .ctor（共享 0x4B3000）
181254082  je 181254281                          ; 分配失败 ⇒ 抛（1803F47A0）
18125408C  [rsi+0x10] = r14                      ; ⇦ onComplete  （对象头 0x10 + 字段 0x0）
18125409B  [rsi+0x18] = rbx                      ; ⇦ onError
1812540A6  mov rcx,[rip+0x306C39B]               ; WindowsManager 的 Il2CppClass*
1812540AD  call 181258290                        ; SingletonBehaviour<WindowsManager>.get_Instance
1812540B5  je 181254281                          ; 单例为 null ⇒ 抛
1812540BB  mov rdx,[rip+0x307EB1E]               ; ⇦ 字面量 "MenuShop/WaitingPopup"（ScriptString @ RVA 0x42D2BE0）
1812540C9  xor r9d,r9d                           ; closeOnEsc = false
1812540D1  mov r8b,1                             ; localizeTexts = true
1812540D7  mov [rsp+0x20],rbx                    ; primaryButton  = null
1812540CC  mov [rsp+0x28],rbx                    ; secondButton   = null
1812540DC  call 1808761A0                        ; ★ WindowsManager.ShowPopUp(text, localizeTexts, closeOnEsc, primaryButton, secondButton)
1812540E1  test rdi,rdi / je 181254281           ; offer == null ⇒ NullReferenceException（无其它校验）
1812540EA  mov rdx,[IShopOffer_TypeInfo]         ; RVA 0x42B0370（script.json: ScriptMetadata）
1812540F1  xor ecx,ecx                           ; ⇦ ecx = 0 = 接口方法槽位 0 = IShopOffer.get_IsItem
1812540F6  call 1800021F0                        ; ⇒ 接口派发到 offer.get_IsItem()（见 §1·3）
1812540FB  test al,al
1812540FD  jne 1812541F5                         ; IsItem == true ⇒ 走 PurchasesManager 支
--- IsItem == false 支（181254103..1812541B8）---
18125410F  … 1818A6ED0                          ; new Action<IReadOnlyList<RewardInfo>>(DisplayClass, HandlePurchaseComplete)
181254152  … 180866100  … [klass+0x60]          ; new Action(DisplayClass, <TryPurchase>b__0)      ← 不取字串的那个
181254195  mov r8d,9                            ; ⇦ 接口槽位 9 = IShopOffer.TryPurchaseOffer（与 dump.cs 的 Slot: 9 逐值吻合）
1812541B8  call qword[rax]                       ; offer.TryPurchaseOffer(onComplete, onError)      ← 内联接口解析
--- IsItem == true 支（1812541F5..18125427C）---
1812541F5  mov rcx,[rip+0x306B34C] / call 181258290 ; PurchasesManager 单例
18125422B  … 1818A6ED0  [klass+0x50]             ; new Action<RewardInfo[]>（同一颗 HandlePurchaseComplete，靠 Action<in T> 逆变）
181254257  … 1818A6ED0  [klass+0x58]             ; new Action<string>(DisplayClass, HandlePurchaseError)
18125426C  mov byte[rsp+0x20],1                  ; ⇦ showRewardWindow = true
181254277  call 180778A70                        ; PurchasesManager.PurchaseItem(offer, onComplete, onError, true)
```

### 1·3 `1800021F0` 是什么（**不是**「is 判断」，是接口派发 helper）

逐条读 `0x1800021F0`（`push rbx; sub rsp,0x20` … `jmp r8`）是一段**接口方法派发器**：
入参 `(cx = 接口内方法槽位下标, rdx = 接口 Il2CppClass*, r8 = 对象)`；它遍历 `klass->interfaces`
（`[klass+0x12e]` = 个数、`[klass+0xb0]` = 表，每项 0x10 字节）找到该接口 → 取
`((ifaceOffset + 槽位) << 4) + 0x138 + klass`（`0x138` = `Il2CppClass.vtable` 偏移，每项 16 字节
`{methodPtr, methodInfo}`）→ **尾跳**到那个方法（`jmp r8`）。
⇒ 所以 `0x1812540F6` 那一句 = **`offer.get_IsItem()`**，返回值直接落在 `al`。
**两处槽位自证**：`get_IsItem` 用 0、`TryPurchaseOffer` 用 9 —— 而 `dump.cs:133541` 的 `IShopOffer`
把 `get_IsItem` 排 `Slot: 0`、`TryPurchaseOffer` 排 `Slot: 9`，**逐值吻合**（也顺便证明了 IL2CPP 的
接口槽位**不从父接口继承的方法起算**）。

### 1·4 三条回调各自做什么（`HidePopUp(forceHide: true)` 是共有的第一句）

| 回调 | RVA / VA | 方法体（`decomp_full` 实读 + dev-ILSpy 同形） |
|---|---|---|
| `<TryPurchase>b__0` | `0xEBCBC0` / `0x180EBCBC0` | `WindowsManager.Instance.HidePopUp(forceHide:true); onError?.Invoke();`（**0 参数版**，丢弃错误字串） |
| `g__HandlePurchaseComplete\|1(IReadOnlyList<RewardInfo> rewards)` | `0xEBCC50` / `0x180EBCC50` | `HidePopUp(true); onComplete?.Invoke();` —— ⚠️ **`rewards` 实参被丢掉**，不转发 |
| `g__HandlePurchaseError\|2(string error)` | `0xEBCCC0` / `0x180EBCCC0` | `HidePopUp(true); onError?.Invoke();` —— ⚠️ **`error` 实参被丢掉** |

`WindowsManager__HidePopUp.c`（`0x875110`，2 参重载）= `if (cur != pop) { if (!force) return; if (pop==null) return; } CloseWindow(popUpWindow);`
—— 与我们 `Shell/WindowsManager.cs:1496` 的 `HidePopUp(bool force = false)` **逐句同形**。

### 1·5 分支顺序（一句话版）

```
ShowPopUp("MenuShop/WaitingPopup", localizeTexts:true, closeOnEsc:false, null, null)   ← 无条件，第一句
offer == null ? throw NRE
offer.IsItem ?  PurchasesManager.Instance.PurchaseItem(offer, onComplete, onError, showRewardWindow:true)
             :  offer.TryPurchaseOffer(onComplete, onError)                              // showRewardPopup 默认 true
成功 → HandlePurchaseComplete(rewards) = HidePopUp(true) → onComplete()
失败 → 货币支: b__0()          = HidePopUp(true) → onError()
        IAP 支: HandlePurchaseError(str) = HidePopUp(true) → onError()
```

🔴 **`TryPurchase` 里没有任何本地校验**：不查货币、不查限购、不查等级、不查是否已拥有 ——
唯一的「拒」是 `offer == null`（NRE）和单例为 null（NRE）。所有业务校验都在服务端回包里，
表现为**走到 `onError` 回调**。⇒ 我们侧「本地买」不做校验是**对的**，但**缺了这条弹窗顺序**。

---

## 二、等待弹窗：**有** / 文案键 = `MenuShop/WaitingPopup` / 形态 = 0 按钮

### 2·1 三源一致

1. **反汇编**：`1812540BB` 取 `mov rdx, qword ptr [rip+0x307EB1E]` → 目标 VA `0x1842D2BE0` / RVA `0x42D2BE0`；
   `script.json` 的 **`ScriptString` 表在该地址上的值 = `MenuShop/WaitingPopup`**（不是 TypeInfo，是字面量）。
2. **dev-ILSpy 源码级**：`out_cpp2il_dev_ilspy/Everguild.LiveOps/ShopStore.cs:518`
   → `instance.ShowPopUp("MenuShop/WaitingPopup", localizeTexts: true, closeOnEsc: false, gameWindowButton, secondButton);`
3. **词条真值**（`CardPresentation/Resources/official_loc_13lang.json`，13 语言列）：

| lang | 值 |
|---|---|
| **en** | `Processing your purchase...` |
| **zh** | `正在处理您的购买...` |
| es | `Procesando tu compra...` · ru `Ваша покупка обрабатывается...` · fr `Traitement de votre achat...` · de `Ihr Kauf wird bearbeitet...` · pt `Processando sua compra...` · it `Elaborando il tuo acquisto...` · ko `구매 처리 중...` · ja `お買い上げを処理中...` · pl `Przetwarzanie twojego zakupu...` · cs `Zpracovává se váš nákup...` · tr `Satın almanız işleniyor...` |

⇒ **这条不用「编文案」**，键和两列译文都在本地。

### 2·2 `localizeTexts = true` 的落地

`PopUpGameWindow__SetText.c`：`if (localizeTexts) text = I2_Loc_LocalizationManager.GetTranslation(text, …);`
然后 `Message.text = text`。⇒ **传的是键本身**，原版不走任何「明文」分支。

### 2·3 形态 = **一颗按钮都不显示**（这条容易做错，写清判据）

`ShowPopUp` 的 5 参重载（`0x8761A0`）第 1 句就分叉：

```c
if (primaryButton == 0)                                  // ← 我们这一档
    LoadPopUpAndShow(this, text, localizeTexts, /*GameWindowButton[]=*/0, closeOnEsc);
else if (secondButton == 0) { …新建 1 个元素的数组… }
else                        { …新建 2 个元素的数组… }
```

⇒ 按钮数组 = **null**（长度 0）。然后 `PopUpGameWindow__ConfigurePopUp(this,text,localize,array,closeOnEsc)`
（4 参版，`0x83A6E0`）第一句：

```c
if (buttons == 0 || buttons.Length == 0) { ConfigurePopUp(this, text, localizeTexts, closeOnEsc); return; }  // → 3 参重载
```

3 参重载（**`0x83A570`**，逐指令读通）：

```c
this.closeOnEsc = closeOnEsc;          // [this+0x39] = (byte)r9b
SetText(this, text, localizeTexts);    // 0x83AD30
GetUICamera(this);                     // 0x83ABF0
foreach (var b in LiveButtons)         // [this+0x80] = List<EverguildButton>
    b.gameObject.SetActive(false);     // 0x182FF25C0(SetActive, rdx=0) · 0x182FECAE0(get_gameObject)
return;
```

⇒ **0 按钮 + `closeOnEsc = false`** ⇒ 玩家点了没反应、Esc 也关不掉，只能等购买回包 / 出错后由
`HidePopUp(forceHide: true)` 收掉。**这就是「等待弹窗」的全部形态**（没有转圈动画的证据：
`LiveButtons` 之外它只写正文与 closeOnEsc，没有动任何进度控件 —— 这一格**没查到动画**，见 §五）。

### 2·4 时序（对施工很关键）

```
TryPurchase 第一句 ShowPopUp  →  （网络往返 / 服务端处理）  →  回调第一句 HidePopUp(true)  →  再跑调用方 onComplete
```
⇒ **先关等待弹窗，再开领奖窗 / 开包窗**。⛔ 别反过来（否则两扇窗抢栈顶）。

---

## 三、`Store.CanPurchase` 的判定条件（VIP 页那个）

### 3·1 先钉住「`Store` 是谁」

`VIPCardShopTab`（`dump.cs:91953`）的私有属性 `private DailyStore Store { get; }`（`get_Store` RVA `0x7A6EF0`）。
`VIPCardShopTab__CheckVIPState.c` 里那一句是**虚调用**：

```c
plVar5 = VIPCardShopTab__get_Store(this);
cVar4  = (**(code **)(*plVar5 + 0x588))(plVar5, *(undefined8 *)(*plVar5 + 0x590));   // ← Store.CanPurchase
GameObject.SetActive(this->playerIsVIPObject,    cVar4);
GameObject.SetActive(this->playerIsNotVIPObject, cVar4 == 0);
if (cVar4 == 0) return;
vipTimer.Initialize(InventoryManager.GetExpirationDate(Store.RequiredItem));
```

`0x588` 换算：`Il2CppClass.vtable` 在 `+0x138`、每项 `VirtualInvokeData` = **16 字节**
⇒ slot = `(0x588 − 0x138) / 16 = **69**`，正是 `DailyStore.get_CanPurchase` 的 `Slot: 69`（`dump.cs:133940`）。
⇒ **VIP 页读的是 `DailyStore.CanPurchase` 这个 override**，不是基类。

### 3·2 逐条：`DailyStore.get_CanPurchase`（RVA `0x8CC740` / VA `0x1808CC740`）

```c
rbx = this->data                                    // [this+0x18] = LiveOpsEvent<T>.data（类型 DailyStoreData）
if (rbx == null) throw NRE
rsi = (data->requiredItem != null)                  // [data+0x70] = LoadableReference<ObtainableItem> requiredItem
        ? LoadableReference<object>.get_Reference(data->requiredItem)   // 0x18101C220
        : null
// ── 条件 ①：requiredItem == null ⇒ 直接 return true
if (UnityEngine.Object.op_Equality(rsi, null))      // 0x182FFB790
    return true;
// ── 条件 ②：否则 —— 背包里有没有这件东西
return InventoryManager.Instance.HasItem(requiredItem);   // 单例 0x181258290 → 0x180CF7F30
```

**条件表**

| # | 条件 | 字段 / 偏移 | 出处 |
|---|---|---|---|
| ① | store 的 `requiredItem` **为 null** ⇒ `CanPurchase = true`（谁都算「可买」） | `DailyStoreData.requiredItem` `// 0x70`（`LoadableReference<ObtainableItem>`） | `dump.cs:134031` · `1808CC7BB mov rcx,[rbx+0x70]` · 判空 `1808CC7F6 call 182FFB790` → `1808CC7FF mov al,1; ret` |
| ② | 否则 = **背包里持有该道具且数量 > 0** | `InventoryManager` 单例 + `Inventory` | `InventoryManager.HasItem<T>`（`0xCF7F30`）：`item==null → false`；否则 `HasItemByID(item.Id)` |
| ②·a | `HasItemByID` 的两条实现（按 `InventoryManager+0x40` 一位布尔分叉） | `InventoryManager+0x40`（bool）· `+0x48`（旧 List）| `0xCF7C30`：`false` 支 = `Enumerable.Any(list, x => x.Id == id && 0 < x.Amount.GetValueOrDefault())`；`true` 支 = `Inventory.TryGetItem(id, out it)` 后 `0 < it.<slot3>()` |
| ②·b | 谓词真值 | `ItemInstance+0x58`（Id 字符串）· `ItemInstance+0x78`（`int?` Amount） | `decomp_full/InventoryManager.__c__DisplayClass15_0_object____HasItemByID_b__0.c`：`String.Equals(x.Id, id) && 0 < x.Amount.GetValueOrDefault()` |

**⛔ 没有的条件（逐条排除，别再往这张表上加）**：**没有**货币够不够、**没有**玩家等级、
**没有**限购次数、**没有** VIP 档位字段 —— 这个属性只回答「**背包里有没有那件东西**」。
`ShopStore.get_CanPurchase`（基类，RVA `0x4DE0E0`）是 **`mov al,1; ret`**（恒 true），
`DailyStore` 是**唯一的 override**（`grep "override bool CanPurchase" dump.cs` 只命中 `133941`）。

### 3·3 同一页的兄弟判据：`InventoryManager.GetExpirationDate<T>` **也是本地的**

`VIPCardShopTab.CheckVIPState` 的另一半 `vipTimer.Initialize(InventoryManager.GetExpirationDate(Store.RequiredItem))`：
`GetExpirationDate<object>`（RVA `0xCF6890`，`decomp_full` 有产物）逐句 =

```csharp
string id = itemRef.Id;                                     // IIdentifiable.get_Id（槽位 0）
if (string.IsNullOrWhiteSpace(id)) return DateTime.MaxValue; // 静态缓存字段
if (!Inventory.TryGetItem(id, out var item) || item == null) return DateTime.MaxValue;
return item.EphemeralState.EphemeralEndTime;                 // 接口槽位 3 → EphemeralState.get_EphemeralEndTime
```

⇒ **到期时刻同样是本地可算的**（背包里那条实例的临时状态）。所以 `ShopData.VipExpiresAt` 现在恒 `null`、
标 `🔶 自己设计` 的理由（「服务端值，本机取不到」）**只对了一半**：机制是本地，**缺的是输入 `RequiredItem`**。

### 3·4 「缺的那一格输入」到底缺在哪（本轮现核）

`requiredItem` 是 `DailyStoreData` 的序列化字段，`[SerializeField] private LoadableReference<ObtainableItem> requiredItem`。
**本地确实没有这份 SO 实例** —— 现跑（只读）：

```
cd D:/2/新解包资源/assets_full
rg -l -g '*.json' '"requiredItem"'   → 0 命中
rg -l -g '*.json'  fixedDateItems    → 0
rg -l -g '*.json'  itemCategories    → 0
rg -l -g '*.json'  itemSource        → 0
rg -l -g '*.json'  DailyStore        → 只有 globalgamemanagers/MonoScript/*.json（**脚本名**，不是 SO 实例）
```

⇒ 与旧记录一致（store SO 由远端 LiveOps 下发）。**但性质变了**：旧记录把它记成「服务端语义」，
真相是「**本地逻辑 + 一个本地没有的数据输入**」。这条差别影响怎么处置：
不是「照服务端行为猜一个」，而是「照本地公式实现 + 把 `RequiredItem` 那一格标成缺输入」。

---

## 四、我们侧现状与施工面（文件:行号）

### 4·1 现状

| 位置 | 现在做什么 | 与原版的差 |
|---|---|---|
| `Shell/ShopTabPage.cs:714` `Buy(int idx)` | 先过「传奇重复购买」确认（`NeedsLegendaryConfirm`）→ 直接 `DoBuy` | 这条**是对的**（原版 `CatalogItemContainer.TryPurchase` 也是先弹确认再进 `ShopItemContainer`） |
| `Shell/ShopTabPage.cs:753` `ShowLegendaryConfirm` | `mgr.ShowPopUp(text, Loc.T("MainMenu/General/Cancel"), null, Loc.T("MainMenu/General/OK"), onConfirm)` | ✓ |
| **`Shell/ShopTabPage.cs:764-820` `DoBuy(int idx)`** | `ShopData.Buy(_page, idx)`（**本地、同步、即时**）→ `Setup()` → 按 `Type == "Booster Pack"` 分档开箱/开领奖窗 → `return got` | ❌ **完全没有等待弹窗**（原版第一句就弹）。这是本次唯一的结构缺口 |
| `Shell/WindowsManager.cs:1359` `ShowPopUp(text, okText, onOk, cancelText, onCancel)` | 一律走 `ShowMessagePopUp`，**至少带 1 颗钮** | 没有「0 钮」档 |
| `Shell/WindowsManager.cs:1429` `ShowMessagePopUp(messageKey, primaryKey, onPrimary, secondaryKey, onSecondary, closeOnEsc = false)` | 建 `PopUpGameWindow` 并 `OpenWindow` | 多了两条能力：`primaryKey = null` 可用；`closeOnEsc` 可指定 ✓ |
| `Shell/WindowsManager.cs:1496` `HidePopUp(bool force = false)` | `if (cur != pop && !force) return; if (pop == null) return; pop.Close();` | **与原版逐句同形** ✓（原版回调传 `forceHide: true` ⇒ 我们也传 `true`） |
| `Shell/PopUpGameWindow.cs:307` `Create(...)` / `:359` `Configure` / `:380` `RefreshLiveContent` / `:422` `Build` | 1 钮版恒建那颗 `Generic UI Button`（`:517`），**永不移除** | ❌ 缺「0 钮 ⇒ 把唯一那颗 `SetActive(false)`」（= 原版 3 参 `ConfigurePopUp` 的行为） |
| `Shell/ShopData.cs:533` `IsVip`（🔶 自己设计）· `:538` `VipExpiresAt`（恒 null） | 站 `Store.CanPurchase` / `GetExpirationDate` 的位 | 判据现已查清，见 §三；`IsVip` 的**语义**应改成「`RequiredItem == null \|\| 背包里有 RequiredItem`」 |
| 读 `IsVip` 的两处 | `ShopTabPage.cs:247`（建 `VIP Status` 行）· `:273`（建非 VIP 遮罩）；生命周期 `:976/:988/:996` | 与 `CheckVIPState` 的调用点**对得上** ✓ |

### 4·2 施工面 A —— 等待弹窗（最小改动，3 处）

**① 词条常量**（放 `ShopTabPage` 类尾、紧挨现有的 VIP 那几个常量 `:926-928`，或 `ShopData` 里）：
```csharp
public const string WaitingPopupTerm = "MenuShop/WaitingPopup";   // 原版 ShopStore.TryPurchase 第一句 · EN "Processing your purchase..." / ZH "正在处理您的购买..."
```

**② `Shell/ShopTabPage.cs:764-770`**（`DoBuy` 开头）：
```csharp
string DoBuy(int idx)
{
    var wm = _win != null ? _win.Manager : null;
    // = 原版 `ShopStore.TryPurchase` 第一句 ShowPopUp(…, localizeTexts:true, closeOnEsc:false, null, null)
    if (wm != null) wm.ShowMessagePopUp(WaitingPopupTerm, null, null, null, null, closeOnEsc: false);
    else Debug.LogWarning("[Shop] 没有 WindowsManager ⇒ 弹不出原版那句「正在处理您的购买…」");
    string got = ShopData.Buy(_page, idx);            // ← 第 766 行，原样
    if (wm != null) wm.HidePopUp(true);               // = 原版回调第一句 HidePopUp(forceHide:true)，**在 Setup/开窗之前**
    Debug.Log(…);                                     // 第 767 行起原样
    Setup();                                          // 第 769 行原样
```
⚠️ **必须插在 `Setup()` 与「开箱/开领奖窗」之前**（§2·4 的时序：先关等待弹窗，再开下一扇）。

**③ `Shell/PopUpGameWindow.cs`（让它支持 0 钮）**：
- `Build()` `:517-520` 的 `out _` 改成 `out _primaryNode`（`MakeButton` 的 `node` 出参已经有了，`:575`），
  并在 `:524` 之后补一句
  `if (_primaryNode != null) _primaryNode.gameObject.SetActive(!string.IsNullOrEmpty(_primaryKey));`
- `RefreshLiveContent()` `:383/388` 同补一句（复用/刷新那一支要一致）。
- 在 `Build()` 那段注释里补判据：`PopUpGameWindow__ConfigurePopUp(3 参)`（VA `0x18083A570`）对每颗
  `LiveButtons` 调 `SetActive(false)`；`ConfigurePopUp(4 参)`（VA `0x18083A6E0`）在 `buttons == null || Length == 0`
  时**转调 3 参版**。

**⛔ 一个必须由调度台拍板的点**（我按铁律只能报，不能自行改口径）：
我们的购买是**同步即时**的 ⇒ 这三行会在**同一帧内**开窗又关窗，玩家**看不见**。
两个选项：
- **(a) 照原版结构原样接**（推荐）：行为与原版一致、断言可验（「弹窗建过 / 弹窗被收掉」），只是本地不可见。
  在注释里写明「本地购买是同步的，这一扇在同一帧开关，视觉上不可见；原版可见是因为有网络往返」。
- **(b) 额外加 1 帧延迟**让人看见 —— **这是原版没有的行为**，属于新增设计，不建议。

### 4·3 施工面 B —— `Store.CanPurchase`（把 `IsVip` 从「我们挑的」换成原版公式）

原公式（§3·2）：`RequiredItem == null || InventoryManager.Instance.HasItem(RequiredItem)`。
但 `RequiredItem` = `DailyStoreData.requiredItem`（本地没有那份 SO）⇒ **输入仍空缺**。
两条可行落法：

- **(a) 保持现状 + 把注释里的判据换成现查的**（`Shell/ShopData.cs:527-545`）：
  把「原版 = **服务端** `Store.CanPurchase`」这句（`:528`、`:535-536`、`:523-524`、`ShopTabPage.cs:907` 附近）
  就地订正为「原版 = `DailyStore.CanPurchase`（**本地逻辑**）= `RequiredItem == null || InventoryManager.Instance.HasItem(RequiredItem)`；
  本机缺的是 `DailyStoreData.requiredItem` 这一格输入（store SO 未导出，`assets_full` 0 命中）」
  —— 这是**铁律 5 的强制项**（旧记录写错，必须就地改）。
- **(b) 若调度台要真的按公式跑**：把 `IsVip` 从「静态常量」改成「`RequiredItem` 可解析时按公式算，
  否则退回本机默认 + 出声」。但这需要一份 `RequiredItem` 的替代来源（本机没有），
  大概率仍是 `🔶 自己设计`，只是理由写得更准。

⚠️ **`GetExpirationDate` 同理**（§3·3）：`ShopData.VipExpiresAt` 现在恒 `null` 的理由
（「服务端值，本机取不到」）要就地改成「**本地可算** `Inventory.GetExpirationDate(RequiredItem)`，
缺的是 `RequiredItem`；且背包里要有那条实例」。

---

## 五、没查清的部分（如实）

1. **等待弹窗有没有转圈/进度动画**：`ConfigurePopUp(3 参)` 只写 `closeOnEsc` + 正文 + 把 `LiveButtons` 全隐藏，
   **没有碰任何进度控件**；但**不能据此断言「原版绝对没有动画」** ——
   动画可能挂在 prefab 的 `Animator`/协程上（`LoadPopUpAndShow` 那一路只做了
   `Instantiate prefab → ConfigurePopUp → OpenWindow`）。**要定这一格还差**：
   ① 读 `MessagePopupWindow` prefab 上有没有 `Animator` 组件与 clip；
   ② 读 `WindowsManager._LoadPopUpAndShow_d__59__MoveNext.c` 全文（`decomp_full` 有产物，本轮未逐行读）。
2. **`offer.IsItem` 在真实商店里哪些 offer 为 true**：`IsItem` 由 `IShopOffer.AssignStore(element, store, isItem)`
   （RVA `0x8CE740`，已读到它写 `set_IsItem` 在接口槽位 1）赋值；**调用点传什么**本轮没查
   （要追 `ShopTab.RefreshOffers` / `ShopOfferContainer` 那一族）。对本次两个缺口**不影响**（两条都在 `if` 之内）。
3. **`DailyStoreData.requiredItem` 的实时真值**：本地没有 SO（§3·4 已证 0 命中），
   要拿到只能等远端 LiveOps 包 ⇒ 归「判据在远端」。
4. **`PurchasesManager.PurchaseItem` 内部**（RVA `0x778A70`）本轮只确认了签名与
   `showRewardWindow:true` 这个实参，**没读方法体**（不在两个缺口范围内）。

---

## 六、证据清单（可复核）

```
# 1) TryPurchase 反汇编
PY=D:/2/Warpforge_tools/py312/python.exe
$PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x181253FA0 200
# 2) 三个回调
$PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x180EBCC50 20   # HandlePurchaseComplete
$PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x180EBCCC0 20   # HandlePurchaseError
# 3) CanPurchase
$PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x1804DE0E0 12   # 基类：mov al,1; ret
$PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x1808CC740 80   # DailyStore override
# 4) 0 钮形态
$PY d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll 0x18083A570 60   # ConfigurePopUp(3 参)
# 5) 文案字面量（script.json 的 ScriptString / ScriptMetadata）
#    0x42D2BE0 → "MenuShop/WaitingPopup" ；0x42B0370 → "Everguild.LiveOps.IShopOffer_TypeInfo"
```

**文件 / 行号锚点**：`dump.cs:134277-134405`（DisplayClass6_0 + ShopStore）· `dump.cs:133941`（DailyStore.CanPurchase）
· `dump.cs:133541`（IShopOffer 槽位）· `dump.cs:134028-134064`（DailyStoreData）· `dump.cs:91953`（VIPCardShopTab）
· `dump.cs:109524-109560`（PopUpGameWindow 两个 ConfigurePopUp 重载）· `dump.cs:87937`（PurchasesManager.PurchaseItem）
· `decomp_full/{VIPCardShopTab__CheckVIPState, WindowsManager__ShowPopUp, WindowsManager__LoadPopUpAndShow,
WindowsManager__HidePopUp, PopUpGameWindow__ConfigurePopUp, PopUpGameWindow__SetText,
InventoryManager__HasItemByID_object_, InventoryManager.__c__DisplayClass15_0_object____HasItemByID_b__0,
InventoryManager__GetExpirationDate_object_}.c`
· `cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/ShopStore.cs:479-575`
