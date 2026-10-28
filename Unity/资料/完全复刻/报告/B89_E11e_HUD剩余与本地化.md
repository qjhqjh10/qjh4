# B89-E11e · `战斗界面_HUD` 剩余可做项 + `本地化` 域复核

> 执行代理 `B89-E11e` · 2026-10-28 · 承接 `B89_E11d`

---

## §0 「我能做的」清单（先列，再逐条交代）

`ledger_slice --domain 战斗界面_HUD --status B,C` 现读 **24 行**（B 4 · C 20）。按我这一轮的文件权限（`Battle/SkillPanel.cs` · `Battle/OfflineCards.cs` · `Battle/VfxRuntime.cs` · `Net/NetRuntime.cs` · `Net/NetSession.cs` · 本地化相关）逐条对落点：

| 行 | 方法 | 落点 | 我能做吗 | 结论 |
|---:|---|---|---|---|
| 805 | `ShowStatusWhenConnecting.GetConnectingDots()` | `Net/NetRuntime.cs` | ✅ | **已被 `B89-R4b` 做掉** ⇒ 本轮只复核 + 订正（§2） |
| 806 | `ShowStatusWhenConnecting.OnGUI()` | `Net/NetRuntime.cs` | ✅ | 同上（§2） |
| 1–19 | `ActiveSkillDisplay.*`（19 行） | `Battle/SkillPanel.cs` | — | **全是 A**（已实现），无活 |
| 20 / 306 / 364 / 378 / 684 / 687 / 693 / 695 / 709 / 713 / 741 / 750 / 755 / 756 / 757 / 759 / 761 | 其余 C 行 | **`Battle/Drag…` / `BattleDriver` / `RuleEngine` / `SkillPanel` 之外的类** | ❌ | 落点全在 ⛔ 别人的文件（`Battle/BattleDriver.cs` = E10e/E5e · `RuleEngine/**` = R1d/R5c/E6d · `Core/CardHighlight.cs` = E5e）⇒ 按令**只记待接点，不动手** |
| 537 / 689 / 704 / 712 / 802 | B 行 | `BattleDriver` / 已做 | ❌/— | 同上（802 已做） |

**`Battle/OfflineCards.cs` · `Battle/VfxRuntime.cs` 不存在**（`ls` 现读）⇒ 那两条线在本仓没有文件可改。

> ⇒ HUD 这一域**没有剩余的可做项**：24 行里 20 行已 A、4 行 B 全落在 `BattleDriver`/`RuleEngine`。唯一有增量的是 805/806 的**订正**。

---

## §2 HUD 805 / 806：R4b 已实现，本轮做的是**订正**

**复核结论**：`Net/NetRuntime.cs` 里 `TickConnectingBanner`（`:205`，`Update` 每帧调）→ `ApplyConnectingBanner`（`:340`）→ `ConnectingBannerTextForTest`（`:289`）/ `ConnectingDotsForTest`（`:283`），**常驻叠加画布**（`:353`），不走 `Notice` 队列 —— 与 `.c` 的语义一致（常驻 + 每秒变的点数）。

**我自己独立复读了两条数值判据**（`工具/read_literal.py`，4B float 档，`PYTHONIOENCODING=utf-8`）：
`0x1834b2e8c = 3f` · `0x1834b2df0 = 4f` ⇒ `dots = floor(Mathf.Repeat(t × 3, 4))` ∈ {0,1,2,3}（**每 1/3 秒加一个点**，4 态循环）。

🔴 **两处订正（已改代码，判据都是 ILSpy 原文）**：
1. **基串**：原版是**硬编码英文** `"Connecting"`（`ShowStatusWhenConnecting.cs:32` `string text3 = "Connecting" + text;`），
   **不本地化**。R4b 取的是 `Loc.T("MenuLogin/Intro/Connecting")`（当时标了 🔶「读不到托管串」）—— 但 ILSpy 里那句**是可读的字面量** ⇒ 已改回 `const string ConnectingBaseString = "Connecting"`。
2. **点分隔符**：原版每次拼 `" ."`（**空格 + 点**，`DAT_1842c63a8`，ILSpy `:22`）；R4b 拼的是 `'.'` ⇒ 已改成 `" ."`。

**另补一条判据**（原来是「不知道什么时候关」）：原版 `OnGUI` 末尾 `if (PhotonNetwork.inRoom) enabled = false;` ⇒ **进房间就自动关**；
我们的 `ShouldShowConnectingBanner` 只对 `NetState.Connecting` 亮，**等价** ⇒ 原来标 🔶 的「显示条件」其实有判据，可以降级。

**观测口**：`NetRuntime.ConnectingDotsForTest(secondsSinceLoad)`（纯函数）· `ConnectingBannerTextForTest(...)` · `ConnectingBannerRectForTest(w,h)` · `TickConnectingBannerForTest(st, t)` + `ConnectingBannerVisibleForTest`（批处理里没有帧循环 ⇒ 由用例显式推一拍）。

⚠️ **仍如实记的差异**：① 原版 IMGUI 自绘，我们 uGUI 叠加画布（账里早已判「不算偏差」）；② 字号 / 颜色 / 对齐仍是 🔶 我们挑的（IMGUI 默认 skin 无像素判据）。

---

## §3 `本地化` 域 6 条 D 的复核（**没有能补的代码**，补的是证据）

现读 10 行：A 4（`GetTranslationWithParameters` / `LanguageSelector.Initialize` / `OnLanguageChanged` / `ResetLanguagesDropdown`）· **D 6**。逐条结论：

| 行 | 方法 | 复核结论 | 证据（本轮补进差异列） |
|---:|---|---|---|
| 2 | `AddressableLocalizationLibrary.AssetCompletedLoading` | **D 成立** | 原版 = Addressables 增量加载（`LoadAssetAsync<LanguageRemoteAsset>` + 完成后装 I2 表并释放句柄）；本工程**没有 Addressables**，官方 13 语言表是**一次性**装载（`Core/Loc.cs:2984` `Resources.Load<TextAsset>("official_loc_13lang")`）⇒ 例外①「原版依赖的框架本机没有」。⚠️ 缺口如实记：**「按 assetKey 增量 + 句柄释放」这层生命周期没有替代物** |
| 3 | `AddressableLocalizationLibrary.GetLocalHandle` | **D 成立** | 同上：体 = `Addressables.LoadAssetAsync<LanguageRemoteAsset>(location)`（ILSpy `:34-45`），纯取句柄 |
| 6 | `LanguageSelector.OnDestroy` | **D 成立**（已有证据） | 我们的语言行是按名重建的窗口内节点、点击走 `WindowButton.onClick` 闭包，随列表树销毁 ⇒ 没有「残留监听」这个面（B87-R24 判据，本轮未推翻） |
| 9 | `LocalizationKeyChangerByPlatform.Awake` | **D 成立，但原备注只写了一半** | 本轮按 `.c` 现读补全：`if (!GameStaticData.touchInput) return;` → 取 **prefab 上 Inspector 配的** `altTermForTouch` → `GetComponent<Localize>()` → `loc.Term = alt`。⇒ ① 本工程**没有 `Localize` 组件体系**（我们用 `Loc.T(key)`，没有可改 Term 的组件）② `touchInput` 在桌面恒 false（**原版桌面版也是空转**）。**主因是①**，原来只写了② |
| 10 | `LocalizedMessage.GetLocCodes` | **D 成立**（已有证据） | 0 调用点（两源交叉）+ 挂在远端消息 DTO 上（P3） |
| 11 | `TranslationService.Translate` | **D 成立**（已有证据） | 调云翻译并回填（P3） |

⇒ **`本地化` 没有「键缺/没接」这一类的可补项**；4 条本就没证据的 D 本轮补齐（其中 #9 的判据被订正为「主因是没有 `Localize` 组件」）。

---

## §4 顺手发现（⛔ 没改别人的文件）

1. **`战斗界面_HUD` 里 20 条 C 的落点全在 `BattleDriver.cs`**（+ `RuleEngine/**`）—— 这与我上一轮看到的模式一致：**HUD 域事实上已经被「文件归属」锁死**，不是「没人做」。建议调度台把 E10e/E5e 的批次里点名这几条（741 `BattleManager.Update` 尤其显眼，它多半是一个大杂烩）。
2. **`ShowStatusWhenConnecting` 的「显示条件」原来被标 🔶** —— 其实有判据（`if (PhotonNetwork.inRoom) enabled = false;`）。同类「标了 🔶 但 ILSpy 里就有字面量」的情况值得再扫一遍（托管字符串字面量 `read_literal.py` 读不到，但 **ILSpy 文本里能直接看到**）。

---

## §5 账 / 合并顺序

补丁 **`账/_parts/B89_E11e.tsv`**（11 列 · 5 行 · `轮次` = `B89-E11e`）：
`战斗界面_HUD` 805 / 806（C→A，含本轮订正）· `本地化` 2 / 3 / 9（D 维持，证据补全）。

⚠️ **合并顺序**：805 / 806 与 **`B89_R4b.tsv`（第 12/13 行）撞键**，按字典序 R4b 会赢；但**本轮在代码里改了基串与点分隔符**（R4b 那份写的是「基串 = Loc 键」）⇒ **请先合 `R4b`、后合 `E11e`**。
`本地化` 那 3 行**无冲突**。

---

## §6 typecheck

```
TMPDIR=/tmp/wf_B89E11e bash d:/4/Unity/工具/typecheck.sh
```
**我的文件 0 错**（`Net/NetRuntime.cs` 一行都没报）。当前 8 条运行时错误**全部落在 `RuleEngine/**`**
（`BattleContext.cs` ×3 · `Enchantment.cs` ×2 · `RuleCore.cs` · `CardDatabase.cs` · `SimpleAI.cs`）—— R1d/R5c/E6d 正在写的文件，⛔ 不是我的白名单，未动。
⇒ **要拿干净 0/0 读数，需要那几家存盘后由调度台重跑一次**；编辑器程序集因此连带 `CS0006`（缺 WFCheck.dll），不代表我的编辑器侧有问题。
