# B106 · `CampaignRewardWindow` 按钮上 4 颗 `Localize` 的 term 与换语言行为（判据）

> 只读核实代理 `B106_R13` · 2026-10-31 · 未改任何源码 / 账 / 正本 / 所有权表；未跑 Unity；未动 git。
> 上游 = `B106_W39`（`报告/B106_W39.md` §⑥·1：这 4 颗 `mTerm` 是「模板残留 `MainMenu/RankedWindow/ChangeDeck`」）。
> 本件回答的唯一问题：**切语言时它们到底会不会把文字重写成 `Change Deck`**。

---

## 〇、一句话结论

**会 —— 这 4 颗 `Localize` 不是空壳。** 序列化 `m_Enabled = 1`、`LocalizeOnAwake = 1`、`mTerm = MainMenu/RankedWindow/ChangeDeck`，
目标是**同一 GameObject 上的 `TextMeshProUGUI`**；原版切语言走
`LocalizationManager.CurrentLanguage = x → SetLanguageAndCode(..., Force=false) → LocalizeAll(false) → DoLocalizeAll(false) → 每颗 Localize.OnLocalize(false)`。
只要那一刻该节点 `enabled && activeInHierarchy`，TMP 就被写成该 term 的译文（en `Change Deck` / zh `更换卡组`）。
**且 `CampaignRewardsWindow` / `CampaignUnlockButton` 没有任何语言变更回调**，切完不会自己改回来。

⚠️ 两种**不显现**的情形（见 §五）：节点当时 inactive（窗已关）⇒ `OnLocalize` 在 `activeInHierarchy` 处直接 return；重新开窗时
`TryOpen` 是 **先 `SetActive(true)`（触发 OnEnable→写 Change Deck）再 `Open()`**（触发 `SetBaseButton/SetPremiumButton` 写回 `Claimed`/`Claim`/点数）
⇒ 打开后是语义文字。

---

## 一、四个节点 + 四颗 `Localize` 的序列化字段（全读）

**所有权确认**（两份独立源互证，结论一致）：

- `assets_full`（当前解包）：`bundle_menus_assets_all/GameObject/…`（按 `RectTransform.m_Father` 上溯到根 `RectTransform_2439992069526822022` = `Campaign Reward Window`）——
  该子树里 `mTerm == MainMenu/RankedWindow/ChangeDeck` 的 `Localize` **恰好 4 颗，`m_Enabled` 全 = 1**。
- AssetRipper：`d:/2/tools/assetripper_out/ExportedProject/Assets/Campaign Reward Window.prefab` —— 同 4 颗（`:3394` · `:3592` · `:5341` · `:5539`），`m_Enabled: 1`。
- ⚠️ **同 bundle 另有 2 颗 `ChangeDeck` 的 `Localize` 是 `m_Enabled = 0`**（`MonoBehaviour_-3108892347353388847.json` / `-3601798609980413743.json`），
  上溯根 = `RectTransform_-5629550096730085167`，其 GameObject 文件 = **`GameObject/Debug Reward Window.json`** ⇒ **不属于本窗，别把它算进来**。

| # | 节点（原版路径，相对 `Campaign Reward Window`） | `Localize` pathID（assets_full） | `m_Enabled` | `LocalizeOnAwake` | `mTerm` | TMP pathID（同 GameObject） | TMP 序列化文本 |
|---|---|---|---|---|---|---|---|
| 1 | `…/Base Rewards/…/Unlock Button/Claimed Text` | `2788745424070941830` | **1** | **1** | `MainMenu/RankedWindow/ChangeDeck` | `8176012911259524230` | **`Change Deck`** |
| 2 | `…/Base Rewards/…/Unlock Button/Point Count` | `-8950847721322668922` | **1** | **1** | 同上 | `-4261334616534705018` | `100` |
| 3 | `…/Premium Rewards/…/Unlock Button/Claimed Text` | `-5237237741608854394` | **1** | **1** | 同上 | `3423181592951037062` | **`Change Deck`** |
| 4 | `…/Premium Rewards/…/Unlock Button/Point Count` | `-6558756384336536442` | **1** | **1** | 同上 | `-8183754553003893626` | `100` |

四颗的其余字段**逐颗一致**（`MonoBehaviour_<pathID>.json` 现读）：
`mTermSecondary = ""` · `PrimaryTermModifier = 0` · `SecondaryTermModifier = 0` · `TermPrefix/TermSuffix = ""` ·
`AlwaysForceLocalize = 0` · `AllowParameters = 1` · `AllowLocalizedParameters = 1` ·
`LocalizeEvent.m_PersistentCalls.m_Calls = []` · `LocalizeCallBack.Target = {0}`/`MethodName = ""` ·
`mLocalizeTarget = {\"m_FileID\":0,\"m_PathID\":0}` · `mLocalizeTargetName = "I2.Loc.LocalizeTarget_TextMeshPro_UGUI"`。

- 节点名字里的 `Claimed Tex`（少了 `t`）只是 assets_full 的**文件名截断**（`GameObject/Claimed Text_-4053815522695537530.json` 与 `Claimed Text_7435200019736534150.json` 两份都存在，
  前者父链 = `Premium Rewards`、后者 = `Base Rewards`；`GameObject/Claimed Tex*.json` 是别的 prefab 的同名节点）—— **不是原版节点名**。
- `Point Count` 两颗在 prefab 里 `m_IsActive: 0`（默认灰），`Claimed Text` 两颗 `m_IsActive: 1`；运行时由 `CampaignUnlockButton.ToggleTexts` 切换。
- 「`ChangeDeck` term 是模板残留」在这里有**第二条硬证据**：`Claimed Text` 的 **TMP 序列化文本本身就是 `Change Deck`**（`MonoBehaviour_3423181592951037062.json` 的 `m_text`，`m_fontSize = 35`）。
  这 4 颗是从 Ranked 窗的 Change Deck 按钮整组拷过来的。

---

## 二、`m_Target` 指向哪个组件

- **序列化里 `mLocalizeTarget`（真正的目标对象引用）是 null**（`m_PathID: 0`）；真正生效的是 **`mLocalizeTargetName = "I2.Loc.LocalizeTarget_TextMeshPro_UGUI"`**（类型名）。
- 运行时 `I2.Loc.Localize.FindTarget()` 按**描述符类型名**匹配：
  `Localize.cs`（`out_il2csharp/Assembly-CSharp/I2/Loc/Localize.cs:842-950`）里，名字非空分支 =
  遍历 `LocalizationManager.mLocalizeTargets`，比较 `mLocalizeTargetName` 与 `desc.GetTargetType().FullName`
  （反编译在 `:890-905` 处对 `GetTargetType()` 的返回值再取一个 `+0x168` 的无参 string 访问器 = `Type.FullName`）。
  ⇒ `LocalizeTarget_TextMeshPro_UGUI` 的 `FullName` 正好是 `"I2.Loc.LocalizeTarget_TextMeshPro_UGUI"` ⇒ **匹配成功**。
  旁证：本 bundle **901 颗 `Localize` 里 899 颗**的 `mLocalizeTargetName` 都是这个全名，2 颗是 `I2.Loc.LocalizeTarget_UnityStandard_Prefab`
  —— 全库都是全名，说明运行时/序列化两侧用的就是 `GetTargetType().FullName`。
- `CanLocalize` / `CreateTarget`（`I2.Loc/LocalizeTargetDesc_Type.cs:11-53`）：`CanLocalize = cmp.GetComponent<T>() != null`，
  `CreateTarget` 建 `LocalizeTarget<TextMeshProUGUI>` 并把 **`mTarget = cmp.GetComponent<TextMeshProUGUI>()`**（同一 GameObject）。
  ⇒ 这 4 颗的目标 = **它们自己那个 `Claimed Text` / `Point Count` 节点上的 `TextMeshProUGUI`**（脚本 pathID `7477354737935883349` = `TMPro.TextMeshProUGUI`，
  已用 `bundle_Waprforge_monoscripts/MonoScript_7477354737935883349.json` 反查类名确认；其组件 JSON 有 `m_text/m_fontAsset/m_fontSize…` 全 TMP 字段）。
- `DoLocalize`（`I2.Loc/LocalizeTarget_TextMeshPro_UGUI.cs:112-253`）：先比 `mTarget.text != mainTranslation`，**不等才写**，末段间接调用 = `TMP_Text.set_text`。

---

## 三、I2 `Localize` 的重取时机 + 语言切换链（含所有闸）

**版本 = I2 Localization `2.8.13 f1`**（`LocalizationManager.GetVersion()`，`out_cpp2il_dev_ilspy/I2.Loc/LocalizationManager.cs:566-574`）。

### 3·1 三个入口

`I2.Loc/Localize.cs`（`out_cpp2il_dev_ilspy`）：

| 入口 | 行 | 做什么 |
|---|---|---|
| `Awake()` | `:196-204` | `UpdateAssetDictionary()` → `FindTarget()` → `if (LocalizeOnAwake) OnLocalize()`（`Force=false`） |
| `OnEnable()` | `:206-209` | **无条件** `OnLocalize(false)` |
| 语言切换 | 见 3·2 | `DoLocalizeAll` 遍历**场景里全部** `Localize`（含 inactive 对象）逐个调 `OnLocalize(Force)` |

> `Localize` **自己不订阅任何语言事件**（`OnLocalizeEvent`/`SetLanguage` 在 `Localize.cs` 里 0 命中）—— 全靠 `LocalizationManager` 反向扫全场。

### 3·2 语言切换的调用链（`LanguageSelector` → TMP）

1. `LanguageSelector.OnLanguageChanged(int)`（`Assembly-CSharp/LanguageSelector.cs:32-45`）：
   `LocalizationManager.CurrentLanguage = <下拉项>`；若在战斗中再额外 `BattleManager/BattleHud.BroadcastChangedLanguage()`。
2. `LocalizationManager.CurrentLanguage` setter（`out_il2csharp/.../LocalizationManager.cs:37-59`）：
   `SetLanguageAndCode(supported, code, true, false)` —— 第 4 个实参 **`Force = false`**。
3. `SetLanguageAndCode`（同文件 `:320-356`，末句 `LocalizationManager.LocalizeAll(Force);`）：
   `Force` 直落 `LocalizeAll`。
4. `LocalizeAll`（同文件 `:2504-2523`）：`Application.isPlaying` ⇒ **起协程**（`mLocalizeIsScheduledWithForcedValue |= Force`，此处为 false），下一帧执行 `DoLocalizeAll(false)`。
5. `DoLocalizeAll`（同文件 `:2532-2561`）：`Resources.FindObjectsOfTypeAll<Localize>()`（**含未激活对象**）→ 循环 `OnLocalize(Force)`。

### 3·3 `OnLocalize(Force=false)` 的全部闸（`out_cpp2il_dev_ilspy/I2.Loc/Localize.cs:237-290`）

```
if (!Force) {
    if (!enabled) return;                                   // :262  ← 行为组件的 m_Enabled（这里 4 颗都 = 1）
    if (gameObject == null) return;
    if (!gameObject.activeInHierarchy) return;              // :272  ← 唯一的显隐闸：场景中是否真的激活
}
if (string.IsNullOrEmpty(CurrentLanguage)) return;
if (AlwaysForceLocalize == false && !Force && !HasCallback()) {
    if (LastLocalizedLanguage == CurrentLanguage) return;   // :286  ← 「这语言已经取过一次」就跳过
}
… GetFinalTerms (取 mTerm) → GetTranslation → mLocalizeTarget.DoLocalize(...)
```
`AlwaysForceLocalize = 0`、无 `LocalizeCallBack`、`LocalizeEvent` 无监听 ⇒ 除了 `enabled` / `activeInHierarchy` / `LastLocalizedLanguage` 之外**没有别的闸**。
切语言时字典里 4 颗的 `LastLocalizedLanguage` 是旧语言（或 null）⇒ **与 `CurrentLanguage` 不等 ⇒ 不会被这条挡**。

⚠️ 还有一条**不是语言切换**的 `Force=true` 路径：`LanguageSourceData.Awake()`（`I2.Loc/LanguageSourceData.cs:505-510`）与 Google 同步完成（`:2855`）
会 `LocalizeAll(Force: true)` —— 那条**不**过 enabled/active 闸。它只在源加载/远端刷新时发生（开机阶段，本窗还不存在 / 或远端刷新，罕见）。

---

## 四、代码会不会在语言切换后把文字改回来

**不会。**

- `CampaignUnlockButton`（`out_cpp2il_dev_ilspy/CampaignUnlockButton.cs`）：
  - `SetAsFreeClaim` `:82-90` / `SetAsClaimed` `:92-100`：`ToggleTexts(false)` 后 `claimedText.text = claimKey / claimedKey`（`LocalizedString` 隐式转 string = **当场**按当前语言取译文）。
  - `OnEnable` `:48-53` **只** `button.m_OnClick.AddListener(OnClick)`；`OnDisable` 摘监听；**没有任何语言回调**。
- 这两个方法**唯一的调用者** = `CampaignRewardsWindow.SetBaseButton()` / `SetPremiumButton()`
  （`out_il2csharp/Assembly-CSharp/CampaignRewardsWindow.cs:201-240` / `:257-300`），
  而它们**唯一的调用者** = `CampaignRewardsWindow.Open()`（同文件 `:72-143`，`:141-142`）。
- `CampaignRewardsWindow` 的 `OnEnable/OnDisable`（`:52-70`）只挂/摘 `closeButton`；**没有** `OnLanguageChanged` / 语言事件 / 刷新开关。
  `GameWindow` 里 language/`OnLocalize` **0 命中**（`Assembly-CSharp/GameWindow.cs`）。
- `LanguageSelector` 只向 `BattleManager` / `BattleHud` 广播（`LanguageSelector.cs:37-44`）—— **和本窗无关**。
- ⇒ 切语言之后没有任何路径会重跑 `SetAsClaimed/SetAsFreeClaim`（除非**关窗再开**：`Open()` 会重跑）。

---

## 五、综合：切一次语言后，那 4 颗实际显示什么

### 5·1 状态矩阵

| 切语言那一刻该节点的状态 | 结果 |
|---|---|
| **`enabled` 且 `activeInHierarchy`**（窗开着 / 或被"送后台"但仍 active —— 见 5·2） | **被写成 `Change Deck`**（en；zh `更换卡组`、de `Deck ändern`…见 5·3）。原来屏幕上是什么（`Claimed` / `Claim` / 点数 / 空）**都被覆盖**。 |
| 节点 inactive（窗已 `Hide()` 成 `SetActive(false)`；或这是另一态那颗 `Point Count`） | `OnLocalize` 在 `:272` 早退 —— **不改**。之后节点被激活时 `OnEnable` 会先写 `Change Deck`，**同一调用里**紧接着 `SetAsClaimed/SetAsFreeClaim` 用官方词条写回 `Claimed`/`Claim` ⇒ **稳态正确**。 |
| 窗**关着**（`Hide()` = `SetActive(false)`，`GameWindow.cs:218-235`），切完再打开 | `TryOpen`（`GameWindow.cs:108-143`）顺序 = `SetupData` → **`:123 gameObject.SetActive(true)`**（此处 OnEnable 写 `Change Deck`）→ **`:125 Open()`**（`SetBaseButton/SetPremiumButton` 写回语义文字）⇒ **最终正确**。 |

> 关键顺序判据：`TryOpen` 里 **`SetActive(true)` 在 `Open()` 之前**（`GameWindow.cs:123` vs `:125`），
> 所以「打开时的那一下 Change Deck」一定会被 `Open()` 覆盖掉 —— **开窗永远不会停在 Change Deck**。

### 5·2 「送后台」这条特殊路径（会让错误**留在屏幕上**）

- 弹窗窗口被顶到后台时走 `WindowsManager.OpenWindowCO`（`Assembly-CSharp/WindowsManager.cs:427-445`）：
  `if (window.type == WindowType.Popup) currentWindow.ToBackground();`（`:441`）。
- 而 `GameWindow.ToBackground()`（`GameWindow.cs:252-255`）**只把状态改成 `Background`，不 `SetActive(false)`**
  （对照：`MainMenuWindow.ToBackground` 会自己再 `SetActive(false)`，`MainMenuWindow.cs:119-127` —— 但本窗**没有覆写**）。
  ⇒ 被顶到后台的 `Campaign Reward Window` **仍 `activeInHierarchy = true`** ⇒ 此时切语言，`DoLocalizeAll` 会**真的重写**它。
- 返回时 `WindowsManager.ShowPreviousWindow()`（`WindowsManager.cs:607-660`）对它是
  `UnHide()`（`gameObject.SetActive(true)`，已是 active ⇒ 无 OnEnable）+ `TryOpen()`（状态是 `Background` 不是 `Closed`
  ⇒ 走 `else` 支，只 `ToFocus()`，**不调 `Open()`**）⇒ **不会重跑 `SetClaimed`** ⇒ 屏幕上就留着 `Change Deck`。

### 5·3 词条值（`MyGame/Assets/CardPresentation/Resources/official_loc_13lang.json` 现读）

| key | en | zh | de | ja | ko |
|---|---|---|---|---|---|
| `MainMenu/RankedWindow/ChangeDeck` | **`Change Deck`** | **`更换卡组`** | `Deck ändern` | `デッキを変更` | `덱 변경` |
| `Rewards Menu/Claimed`（代码稳态值） | `Claimed` | `已认领` | — | — | — |
| `MainMenu/General/Claim`（代码稳态值） | `Claim` | `索取` | — | — | — |

⇒ 所以严格说：**英文档看到的是 `Change Deck`**；中文档看到的是 **`更换卡组`**（同一个 term 的当前语言译文），不是字面 `Change Deck`。

### 5·4 可达性（能不能真的看到）

- 本地**唯一**的语言切换 UI = `LanguageSelector`（`Assembly-CSharp/LanguageSelector.cs`），它挂在
  **`Assets/Main Menu Settings Window.prefab`**（脚本 guid `a4cd5e9f…`；全 `ExportedProject` 的 `.prefab/.unity` 里**只有这一处**用 LanguageSelector；
  I2 自带的 `SetLanguage`/`SetLanguageDropdown` **全库 0 处**）以及登录界面的 `LoginManager`。
- 两者的窗口都是 `WindowType.Popup`：`Campaign Reward Window` = `type: 1` / `windowsPlacement: 15`（`Campaign Reward Window.prefab:1213-1214`）；
  `Main Menu Settings Window`（`SettingsMenu`）= `type: 1` / `windowsPlacement: 5`（`Main Menu Settings Window.prefab:6260-6261`）。
- 本窗还带一块**全屏可射线**的暗底 `Menu Dark Background`：`m_SizeDelta = 4574.6 × 2572.36`、`m_RaycastTarget: 1`、`m_Color.a = 0.77`
  （`Campaign Reward Window.prefab:1275-1303`），上面挂 `BackgroundCloseButton`（脚本 guid `90b73a…`，`:1319-1330`）⇒ **点它 = 关窗**。
- ⇒ **「切语言时本窗 active」这条路径要在 UI 上成立，需要能在本窗压着的时候打开/操作 Settings**。本代理**没有运行时证据**证明可达或不可达
  （静态看 `windowsPlacement` 15 > 5、且有全屏射线暗底，**倾向不可达**；但 `windowsPlacement` 的语义与遮挡关系没有逐行核实）。
- 另一条**客观存在但罕见**的触发 = 远端本地化源刷新（`LanguageSourceData.Awake()` 的 `LocalizeAll(Force:true)`，`LanguageSourceData.cs:505-510`），它**绕过 enabled/active 闸**。

---

## 六、还差什么判据

1. **运行时实拍**：切语言瞬间本窗是否 `activeInHierarchy`、切完屏幕上到底印什么 —— **本地判不了**（要跑起原版并进战役页领到奖励；`d:/2/unity_run_ref` 无存档、战役页进不去）。
2. **`windowsPlacement` 的真实语义**（是否 = Canvas sortingOrder / 层级）与「本窗压着时设置钮能不能点到」—— 需要读 `WindowsManager` 的窗口摆放实现或运行时验证，本件**未逐行核**。
3. **描述符注册时机**（`LocalizeTarget_TextMeshPro_UGUI` 的静态构造何时跑）—— 未逐行证；但**全库 901 颗 Localize 都用同一全名且菜单本地化可用**，已反证运行期能找到目标。
4. **原版运行时 I2 源是否与本地 `official_loc_13lang.json` 同值** —— 那份表来自官方 `I2Languages`（本地解出），一般就是同一份；未在运行时比对。
