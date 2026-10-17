# D4 只读诊断 · 桶 E（darkpact）+ 桶 F（超采样 3 条）· SettingsScene 第四十七会话红

> 只读诊断 · 2026-10-18 · 不跑 Unity / 不改代码 / 不动 git。
> 判据源 = 工作区源码（与 `HEAD` 逐字节一致：`git diff` 对 `SettingsScene.cs` / `SettingsWindow.cs` / `Badges.cs` / `Tooltip.cs` 全空）+ `Resources/*.json` 数据件 + `ProjectSettings/QualitySettings.asset` + 反编译。
> ⚠️ **结论先摆一句**：桶 E 与桶 F 的 4 条**全是自检断言/夹具错（α）**，`Shell/SettingsWindow.cs` 与 `Core/Tooltip.cs` 的产品路径**没有**对应缺陷。分诊文档对桶 E 的描述方向写反了（它写「仍推不出」，实际是「已经推得出、断言还写着推不出」）。

---

## 桶 E — `darkpact` 的官方枚举名（1 条）

### 结论：**α（断言过期/写错）** —— 映射已经通了，断言还在断言「通不了」

**失败的那一条**（SettingsScene 5 红里的一员）：

```
d:/4/Unity/MyGame/Assets/CardPresentation/Editor/SettingsScene.cs:1452
CheckTrue(TipText.OfficialTraitNameForTest("darkpact") == null,
          "（已知缺口 · 如实记）`darkpact` 推不出官方枚举名 ⇒ 非中英档 tooltip 正文回退英文（⛔ 不猜映射）");
```

它断言的是 **`== null`**（即「推不出」）。现在的实现**返回 `"markOfChaos"`** ⇒ 断言红。
失败时打印的正是那句「推不出……」，所以**从失败文案读不出真实方向** —— 分诊文档「仍推不出官方枚举名」就是被这句文案带偏的。

### 证据（执行路径逐步）

1. `Editor/SettingsScene.cs:1449-1453` —— ⑦ 那一节的三条映射断言；`rally`（:1449）、`destroyer`（:1450）都过，只有 `darkpact` 这条红。
2. `Core/Tooltip.cs:717-723` `OfficialTraitNameForTest(key)`：`Badges.KeyOf("darkpact")` = `"darkpact"`，然后查 `_officialTraitName`。
3. `Core/Tooltip.cs:661-691` `LoadOfficialTraitMap()` 的**完整分支顺序**（这就是「别名到底进没进执行路径」的答案）：
   - :666-670 `descNames` = 官方 `CardTraitDescription/*` 的全部枚举名；`descSet` = 同一份的集合。
   - :675 **① 直取**：`descSet.Contains("darkpact")` → **false**。
   - :677 **② 归一化同名**：`NormName("markOfChaos") = "markofchaos"` ≠ `"darkpact"` → 无命中。
   - :678 → :707-714 **③ 显示名相同（`SameTraitWord`）**：
     - 英文那一半：官方 `Card_Trait/markOfChaos` = **`Dark Pact`**，而 `trait_tips.json` 的 `darkpact.en` = **`Dark Pacts`**（差一个复数）⇒ `darkpact` ≠ `darkpacts`，不中；
     - **中文那一半：官方 `Card_Trait/markOfChaos` = `黑暗契约`，`trait_tips.json` 的 `darkpact.zh` = `黑暗契约` ⇒ 逐字相同 ⇒ 命中 `markOfChaos`**（:712-713）。
   - :684-688 **④ N44 的新兜底（`Badges.OfficialTraitName`）也在路径里**，但因为 ③ 已经命中，**走不到它**。
4. `Core/Badges.cs:221-237` `OfficialTraitName`：:226 `Alias` 直查 `darkpact → markOfChaos`（别名表在 `:211`）—— 这条是**真的接进去了**（`Tooltip.cs:686` 调用），只是本例轮不到它。
5. 数据件现读：
   - `MyGame/Assets/CardPresentation/Resources/trait_tips.json` 的 `entries` 里有 `{"key":"darkpact","zh":"黑暗契约","en":"Dark Pacts","bodyZh":"…","bodyEn":"…","src":"official"}` ⇒ `_tips` 里**有**这一行（`Tooltip.cs:614-615` 按小写键收）。
   - `Resources/official_loc_13lang.json`（运行期真读的那份，`Loc.cs:2793` `Lang13ResourcePath = "official_loc_13lang"`）：`Card_Trait/markOfChaos` = `Dark Pact / 黑暗契约 / 闇の契約`；**`CardTraitDescription/markOfChaos` 在**（`Give a blessing from one of the four Chaos Gods` / `赐予混沌诸神中的四神之一的祝福` / `四つの渾沌神の一つからの祝福を与える`）⇒ `descSet` 里有它。
6. 离线复算（按上面同一套算法 + 同一份 JSON 重放）：`direct=False · norm=None · same-word=markOfChaos` ⇒ **最终 = `markOfChaos`**。
   🔴 **注意**：这不是 N44 带来的 —— **N42 落地的三级规则本身就已经能推出它**；N44 那条注释（`Tooltip.cs:647` 把 `darkpact` 列进「推不出的 9 条」）与 `SettingsScene.cs:1452` 的期望值是同一个过期前提。
7. 时序：该断言**首次出现在 commit `3990ca6`（第四十七会话收尾）**；`git show a67bb2c:…/SettingsScene.cs | grep darkpact` **零命中** ⇒ 这条断言从写下到第一次跑（第四十七会话自检 19:14–19:52）**从来没绿过**，期望值是从旧的「9 条推不出」注释抄的，不是从算法推的。

### 修法

1. `SettingsScene.cs:1452` 改成「**能推出**」：
   ```csharp
   Check(TipText.OfficialTraitNameForTest("darkpact"), "markOfChaos",
         "（映射）`darkpact` → `markOfChaos`（第 3 级：`Card_Trait/markOfChaos` 的中文列 = `trait_tips` 的 zh；N44 的 Alias 另有一份同样的映射）");
   ```
2. 建议补一条**灭自证**（字面量，不从被测实现读）：日语档 `TipText.Trait("darkpact")` 含官方日文正文 `四つの渾沌神の一つからの祝福を与える`（数据现读，见证据 5）。这条同时能钉住「非中英档不再回退英文」。
3. 注释同步（铁律 5，见「顺手发现」第 1、2 条）：`Tooltip.cs:647` 的「9 条推不出」清单、`Tooltip.cs:843-846` 的「三条都不唯一命中」文案、`任务总表.md` 的 `N44` 行。
4. ⚠️ **不要**为了让断言继续绿而去删 N44 的兜底 —— 兜底本身是对的（`concussion → concussive`、`quest → questPoints` 都靠它），只是 `darkpact` 不是它的用例。

### 没查清

无。运行期读的就是 `Resources/official_loc_13lang.json` + `Resources/trait_tips.json` 这两份，已逐值现读；算法是纯函数，离线复算与运行期等价。

---

## 桶 F — 「超采样」3 条（同一根因）

**三条红**（`SettingsScene.cs`）：

| # | 行 | 断言 |
|---|---|---|
| ⑥★ | `:2865` | `CheckTrue(!SuperSampling.Enabled, "⑥★ 换到**不允许**超采样的档 ⇒ 那颗开关被**强制清 0**")` |
| ⑥ | `:2869` | `CheckTrue(ssAfter != null && !ssAfter.gameObject.activeSelf, "⑥ …而且那一行**当场藏起来**")` |
| 收尾 | `:2881` | `CheckTrue(!SuperSampling.Enabled && !SuperSampling.Dirty, "（收尾）开关回到出厂值（关）+ 脏位清掉")` |

### 结论：**α（自检夹具算错了要点哪一行）** —— 点了**当前已选中**的那一行，`ChooseQualityOption` 正当地早退，整条链一步都没跑

### 证据（逐句）

1. `SettingsScene.cs:2854-2864`（⑥ 的准备）：
   - `:2854 SuperSampling.Set(true)` ⇒ `_enabled = true`、`_dirty = true`（`SettingsWindow.cs:5985-5989`）。
   - `:2856 ssQuality = 0;` —— **这是给超采样那层「当前画质档」注入的假值**（`SuperSampling.QualityLevelGetter`，`SettingsWindow.cs:5950 / 5962-5965`），**不是** `QualitySettings` 的真值。
   - `:2862 int qTarget = (ssQuality + 1) % Mathf.Max(1, QualitySettings.names.Length);` ⇒ `(0 + 1) % 2 = **1**`。
   - `:2861/:2863` 点画质行开下拉 → 点第 **1** 行。
2. `ProjectSettings/QualitySettings.asset`：`:7 m_CurrentQuality: 1`；档位只有两档 —— `:10 name: Mobile`（0）、`:64 name: PC`（1）。
   ⇒ `QualitySettings.GetQualityLevel() == 1`，而 `qTarget == 1` ⇒ **点的就是当前档**。
3. `Shell/SettingsWindow.cs:3828-3844` `ChooseQualityOption(i)`：
   - `:3832-3836` `if (i == QualitySettings.GetQualityLevel()) { Debug.Log("画质已经是「…」—— 不变"); return; }` ⇒ 直接返回。
   - 后面的 `:3837 ApplyQuality(i)`（→ `:4420-4428`：`SetQualityLevel` + **`SuperSampling.Apply()`**）和 `:3841 RebuildGfxRows()`（= 原版 `ConfigureSuperSamplingVisibility`）**全部没跑**。
   - ✅ **这个早退是正确行为**：原版那颗是 `TMP_Dropdown`，选中的就是当前项时 `onValueChanged` 不会触发（UGUI/TMP 的 `Set()` 同值直接返回），原版 `GraphicsTab__GraphicsQualityDropdownChange.c` 也不会被调到。**不是 β。**
4. 「不允许 ⇒ 清 0」的逻辑**在、且完好**：`SettingsWindow.cs:6006-6018` `SuperSampling.Apply()` 里 `if (!AllowedByQuality && Enabled) { _enabled = false; … _dirty 清 };`（= 原版 `QualitySettingsManager__ApplyGraphicsQuality.c:73-78`，反编译里就是 `if (allowSuperSampling(+0x2d) == 0) GameStaticData+0x124 = 0;`）；`:5970 AllowedByQuality = CurrentQualityLevel == PcQualityIndex`（`PcQualityIndex = 1`，`:5940`）。**只是这次没被调用。**
5. 三条红的因果链（同一根）：
   - ⑥★：`Enabled` 仍为 `true`（:2854 设的），没人清。
   - `:2869`：行节点是**开窗时**按 `ssQuality = PcQualityIndex`（`:2786` 设的）建的那一态；`:2856` 改 `ssQuality` 后**没有重建**（`RebuildGfxRows` 只在 `ChooseQualityOption` 里调，:3841）⇒ 节点仍 `activeSelf = true`。
   - `:2881`：`_enabled`/`_dirty` 都还是 `true`；`Apply()` 是唯一清脏位的口（`:6006-6008`），没跑；收尾只调了 `RebuildGfxRows()`（`:2877`）和 `RestoreRenderScale()`（`:2878`），**都不动这两个内存位**。
6. 反证「点行链路本身是通的」：同一文件 `:1827-1839` 那一节用 **`q0 = QualitySettings.GetQualityLevel()`** 作基算 `qWant = (q0+1)%n = 0`，点第 0 行，`qAsked == qWant` **过了**（不在 5 条红里）。⇒ 按钮、`DropdownRowHit`、`WindowButton.onClick`、`ChooseDropdown` 全没问题。
7. 时序：`:2862` 的 `qTarget = (ssQuality + 1) % …` 是 **T17（commit `09ecc15`，2026-10-17 13:05）** 新写的；T17 之前 ⑥ 只点 `QualityHit`（当时的 `CycleQuality` 循环切档）⇒ **这是 T17 重写夹具时把基算换错了**（拿「超采样门的注入值」当「当前画质档」），不是 T17 改坏了产品。

### 修法

```csharp
// :2856 保持不变：ssQuality = 0;   // 注入给超采样门（Mobile = 不允许）
int qCur = QualitySettings.GetQualityLevel();                       // = 1（PC）
int qTarget = (qCur + 1) % Mathf.Max(1, QualitySettings.names.Length);  // = 0（Mobile）
// 并改 :2859-2860 的注释：「点一个**不等于当前档**的行」
```
- 修完后：`ApplyQuality(0)` → `SuperSampling.Apply()`（`ssQuality = 0` ⇒ `AllowedByQuality = false`）清 `Enabled`/`Dirty` → `RebuildGfxRows()` 按 `RowVisible = false` 把行藏起来 ⇒ 三条一起转绿。
- 也可以更直白：`int qTarget = ssQuality;`（本例 = 0），但**必须**先判 `qTarget != QualitySettings.GetQualityLevel()`，否则在「当前档恰好就是 Mobile」的机器上会重现同一个 bug。**推荐上面的 `qCur+1` 写法**（与本文件 `:1834` 已有的口径一致）。
- ⚠️ **不要**在收尾加 `SuperSampling.ResetForTest()` 来「让 :2881 绿」：那会把「⑥ 真的没走通」掩盖掉（正是本仓「灭自证」那条红线要防的）。

### 没查清

- 没有第四十七会话 `SettingsScene.Run` 的原始 run log（本机未留），所以「当前画质档 = 1」是**两条独立旁证**推的：① `QualitySettings.asset:7` 的 `m_CurrentQuality: 1`；② 若它是 0，则 `:1839` 那条（qWant=1=当前档）也会红，而它不在 5 条红里。**两条旁证互相一致，但没有直接日志。**
- `RestoreRenderScale()` 我没有逐行读；判定它没红的依据是 `:2879` 不在失败清单里（`renderScale` 在 ⑤ 关窗后本来就是 1.0）。

---

## 顺手发现（不属上面 4 条，供调度台分流）

1. 🔴 **`Core/Tooltip.cs:647` 的「9 条推不出」清单已过期**：原文 `ability/concussion/darkpact/ferocity/huntmark/oath/pack/quest/sentry`。现读：`darkpact` 由第 3 级（中文列）命中；`concussion` 由 N44 的 `Alias` 命中；`quest` 由 `Badges.OfficialTraitName` 的专用条目 → `questPoints`（且官方 `CardTraitDescription/questPoints` **在**，见 `official_loc_13lang.json`）。**真正推不出的只剩 6 条**：`ability / ferocity / huntmark / oath / pack / sentry`（离线核过：官方 `CardTraitDescription/*` 对这几个词**零命中**）。按铁律 5 应就地订正该行 + `资料/任务线/界面线/任务总表.md` 的 `N44` 行（那行也还写着 `quest` 推不出）。
2. **`Core/Tooltip.cs:843-846` 的出声文案**仍写「直取 / 归一化同名 / 显示名相同，**三条**都不唯一命中」——现在是**四级**（多了 `Badges.Alias` 兜底，:684-688）。改文案即可，不动逻辑。
3. **`SettingsScene.cs:2872-2873` 那条「自检没有真去切工程的画质档」是空过的**：`CheckNear(GetQualityLevel(), qlBefore, …)` 在「这次点击根本没走到 `ApplyQuality`」时**同样成立** ⇒ 它证明不了链路真跑过（这次 ⑥ 就是全链没跑、它照样绿）。建议照 `:1828-1839` 那一节的写法，用 `QualitySetterOverride` 记一个 `qAsked` 并断言 `qAsked == qTarget`；这样「点了没生效」会**当场**在 ⑥ 上游红，而不是在下游表现成「超采样没清」。
4. **⑥ 里 `ssQuality` 与真实画质档是两套值**：修完后「行显隐」按注入值（0）、而下拉标签 `QualityName()` 按真值（override 挡住 `SetQualityLevel`，仍是 PC）。当前没有断言碰这处不一致，但容易让后来人误读成 bug；建议在 ⑥ 那段注释里写明「二者**故意解耦**：注入值只喂超采样那层门（`SuperSampling.QualityLevelGetter`），真值只喂 `QualitySettings`/标签」。
5. **`Editor/SettingsScene.cs:1452` 的失败文案本身有误导性**（「已知缺口 · 如实记」）—— 这次分诊文档就据此把桶 E 写反了。建议修断言时把文案一起改成「映射**成立**」，并在分诊文档里订正一句（我无权改正本）。
6. `SettingsScene.cs:2851-2852` 的注释「点目标行 ⇒ `ApplyQuality` → `SuperSampling.Apply()`」描述是对的，但**没写「目标行不能是当前选中项」** —— 这正是这次踩的坑，补一句判据可防复发。
