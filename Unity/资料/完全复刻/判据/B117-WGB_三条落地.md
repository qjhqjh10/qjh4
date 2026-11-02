# B117-WGB · 三条判据落地报告（执行代理，2026-11-02）

> 判据正本 = `判据/B117-GB_三条判据.md`（本报告**不重抄**判据，只记「改了什么 / 读数 / 还差什么」）。
> 覆盖：`B117-10`（`UM64` 目标格）· `B117-18`（`ASH15` 语音 orphan）· `B117-35`（拖拽硬吸附）。

---

## 一、改了什么（逐条 `文件:行号`）

| 条目 | 文件 | 落点 | 改动 |
|---|---|---|---|
| `B117-10` | `RuleEngine/Core/EffectText.cs` | **`:10382`**（判据注释追加在同一物理行 `:10380`） | 「几个」那行追加 `\|\| (!string.IsNullOrEmpty(spec.NameFilter) && spec.Side == "own")` ⇒ `Count = 0` |
| `B117-18` | `CardPresentation/Core/VoiceLines.cs` | **`:147-151`**（`Has`）+ **类尾 `:924` 起新建 `HasSoRow`** | `Has` 从「表里有这张卡」改成「**至少一条卡 SO 归属行**」 |
| `B117-35` | `CardPresentation/Hand/CardInteraction.cs` | **`:987-1010`**（`UpdateDrag` 的跟手 + 倾斜两块） | 换成原版两档：`d2 < 0.01` 或 `> 1000` ⇒ 硬吸附；否则 `pos += (target−pos) × (10.8 × dt)`（**不 clamp**）。硬吸附档 `rx/ry` 写 0 |

### 1 · `B117-10`（`UM64 Angels of Death` 不该点目标格）

```csharp
// 改前 :10382
if (pluralOwn || t.StartsWith("all ") || t.Contains(" all ") || t.StartsWith("each ")) spec.Count = 0;
// 改后 :10382
if (pluralOwn || t.StartsWith("all ") || t.Contains(" all ") || t.StartsWith("each ") || (!string.IsNullOrEmpty(spec.NameFilter) && spec.Side == "own")) spec.Count = 0;
```

- 一并拿到两件事：① `Count = 0` ⇒ `PickTarget` 跳过（它只放行 `Count == 1`）⇒ **不再要求点格**；
  ② 结算走 `ResolveTargetsCore` 的 `Count == 0` 支 ⇒ `AddSide` 全池 + `ApplyTargetFilters` 的 `NameFilter` 筛
  （`EffectResolver.cs:2327`）= **全部**己方原铸仲裁者，与原版 `targetsAffected = 10 (allFiltered)` 同义。
- 影响面：**现池只动 `UM64` 一张**（`SAU52` 本来就是 `Count = 0`）。实测见 §三。
- ⚠️ 判据文件里的**次选（更宽）**那条 —— `EffectResolver.cs:2058` 的「真正的闸 = `card.Targeted != 0`」—— **本批没做**，
  那 7 张「文本像要选目标但 `targeted = 0`」要**逐卡核 `ta`** 才敢改。⇒ **另开一条活**。

### 2 · `B117-18`（`ASH15` 的部署语音是 orphan ⇒ 语音钮不建）

`Has()` 现在 = 「这张卡至少有一条 `IsSoAttributed` 行」（`so` 非空 或 `src == "cardSO"`），
与 `TryPick` 的 `soOnly`（`:733`）**同一份判定**。

- ⛔ **没有改 `voice_lines.json`**（那条行 `src` 留空是 `工具/import_original_audio.py` 的正确标法）。
- 覆盖到的调用点都不用另改（全走 `Has`）：`Core/CardButtons.cs:32-33` · `CardDisplayWindow.cs:1205` · `CardDetailPopup.cs:2701`。
- **行数守恒**：`Has` 那 6 行原样换 6 行；判据本体挪到**类尾 `HasSoRow`** —— 因为 `Has` 之后还有
  **6 条账落点**（`VoiceLines.cs:423 / :428 / :433 / :437 / :655 / :686`），在 `Has` 里展开会让它们整体漂。

### 3 · `B117-35`（拖拽跟手 = 原版两档）

```csharp
var delta = target - prevPos;
float d2 = delta.sqrMagnitude;
bool snap = d2 < 0.01f || d2 > 1000f;
if (snap) t.position = target;
else t.position = prevPos + delta * (10.8f * dt);   // ⚠️ 原版不 clamp ⇒ ⛔ 别用 Vector3.Lerp
```

- 倾斜那一段在硬吸附档写 `rx = ry = 0f`（原版把 `rotationTarget` 拷成静态 `Vector3`）。
- ⚠️ **推断强度**：那个静态度在 il2cpp 运行时静态区、文件里读不到，按「与 ctor 的 `targetPosDuringAnim` 同源」
  **推断 = `Vector3.zero`**。**未 100% 钉死**（要运行时探针读那个静态度才能坐实）。
- 顺带**就地订正两条过期注释**（同物理行）：`CardInteraction.cs:1060` / `:1077` 原来写着「上面那句 `Lerp(…)`」——
  现在跟手已经不是 `Lerp` 了，照旧文会让下一个人以为没改。

---

## 二、typecheck

```
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```

- 独立 `TMPDIR=/tmp/wf_wgb`（避免与并发写手互相覆盖输出）。
- 两个**新建**的断言文件已确认在 `wf_csc_editor.rsp` 的源清单里（`gen_csc_rsp.py` 会扫 `Assets/` 补新文件）。
- ⚠️ 第一次跑时运行时程序集报了 **7 条 `CardChoicePanel.cs` 的 `CS0103 _contentHidden`**（不是本批文件，属 `B117_G10_14`）；
  第二次跑已消失（对方修好了）。**本批三个文件在那一次里也一条错都没有。**

---

## 三、ruleprobe 读数（改前 / 改后）

### ① `bash 工具/ruleprobe.sh play UM64`

| | 改前 | 改后 |
|---|---|---|
| 出牌 | **`play=18`（`ErrNoTargetAvailable`）** | **`play=0`（OK）** |
| 摘要 | 「打不出去」 | 「打出去了吗: **是**（照卡面应付 **6** 能）」 |
| 实得变化 | **0 条** | **3 条**（`✚ 落场/生成 P0[1]/[2]/[7] → UM6 2/3/0/3 {flank1}`） |
| 结束码 | `rc=1`（因为出牌被拒） | `rc=0` |

⚠️ **Codex 那半条（`+1 Ranged`）在探针读数里看不到，这是量具限制、不是修没生效**：
`PlayProbe` 的夹具能量固定 **20**（`PlayProbe.cs:45 FixtureEnergy = 20`），
而 `Codex:` 的判据是「**结算那一刻能量 = 0**」（`EffectResolver.cs:14723` 的 `energyzero` 支）⇒ 20 − 6 = 14 ⇒ 条件不成立。
本批的断言里**把能量设成 `= 卡费`**（6）来验这一半（见 §四 B2/B3）。

### ② `bash 工具/ruleprobe.sh check`（解析对拍）

```
=== 全池 1121 张 · 解析差异 1 行 · 改名 0 行 · 新增 0 · 消失 0 ===
⚠️ 解析差异（前 1 张）：UM64|Angels of Death
哨兵合计：失败 1 条   →  ✗ [输出指纹]
     现算 = f937e54dc6a7b79dedc5d6a656e271da · 钉住 = ebdac77915a81673bf44114062061ee9
```

- **逐卡归因 = 只有 `UM64`**（与判据预测一致）。
- `[输出指纹]` 那条红是**预期内的**：它盯的就是「解析输出有没有离开被确认过的那一版」。
  🔴 **本代理没有重钉**（`工具/ruleprobe/Program.cs` 的 `PinnedParseMd5` 属调度台）。⇒ 调度台确认后手改成 `f937e54dc6a7b79dedc5d6a656e271da`。

### ③ `bash 工具/ruleprobe.sh play check`（结算对拍）

| | 改前 | 改后 |
|---|---|---|
| 结算指纹 | `d6c68354baf04bca2f725aa0111817b5` | **`a8b7eac3884b94b0df3e8e6ba6d53577`** |
| ①-a 逐卡差异 | 无 | **只有 `UM64`** |
| ①-b 电池差异 | 无 | **只有 `UM64`**（基线里它那一格记的是「电池没起步 · rc=ErrNoTargetAvailable」） |
| rc | 0 | **1**（等重钉） |

🔴 **本代理没有重钉基线**（`PinnedPlayMd5` / `out_play_baseline.txt` 属调度台）。

---

## 四、断言（新建两个文件，**均未接线、未跑**）

| 文件 | 段 | 条数 | 覆盖 |
|---|---|---|---|
| `Assets/RuleEngine/Editor/RuleEngineTest_B117WGB.cs` | A 解析面 6 条 + B 结算面 6 条 | **12** | `B117-10` |
| `Assets/CardPresentation/Editor/B117_WGB_Assert.cs` | A 语音 7 条 + B 拖拽 8 条 | **15** | `B117-18` + `B117-35` |

### 接线（调度台做 · ⛔ 本代理没碰任何宿主）

```csharp
// RuleEngineTest.cs 的 Run()（本波归 B117_WRED-RE）
Guard("RuleEngineTest_B117WGB", RuleEngineTest_B117WGB.Run);
_pass += RuleEngineTest_B117WGB.Pass; _fail += RuleEngineTest_B117WGB.Fail;

// Editor/BattleScene.cs 的聚合行（本波归多个 B117_G10_* 写手 ⇒ 由调度台统一追加）
Guard("B117_WGB_Assert", B117_WGB_Assert.Run); _pass += B117_WGB_Assert.Pass; _fail += B117_WGB_Assert.Fail;
```

### 灭自证（结构上不可能同时满足）

| 条目 | 判别式 |
|---|---|
| `B117-10` | `PickTarget(ops) == null` **且** 那条 op 的 `Count == 0`；同时 `PlayTactic(targetSlot=-1)` 必须 OK。改回 `Count=1` ⇒ 三条一起红 |
| `B117-10` | B3「台上**每一个**原铸仲裁者都 +1 远程」+ 前提「台上 ≥ 2 个」；`Count=1` 时只有一个吃到 ⇒ 红 |
| `B117-18` | 先断「`ASH15` 的 `deploy` 行**还在表里**」（`LineRowCount == 1`），再断 `Has == false` —— 改回 `ContainsKey` ⇒ 两条不可能同时为真 |
| `B117-35` | 0.5 世界单位的位移这一帧走 **0.09**（0.18 系数），并显式断「**不等于** 0.14175」（旧公式 0.2835） |
| `B117-35` | 远跳必须**位置 == target**（旧公式只走 28.35%）；硬吸附档 `rx/ry` 归零（不归零 ⇒ dy=100 会到 20°） |

### ⚠️ 未跑

本代理**不许**跑 Unity 批处理（简报红线），所以两个文件**一条断言都没执行过** ——
⛔ **别当成已验收**。接线后需要跑：`RuleEngineTest.Run`（`B117-10` / `B117-18` 的数据面）+
`BattleScene.Run`（`B117-18` 顺带：那三条用 `Has` 做「跳过没语音的卡」的循环）+ `CardBaseDemo.Run`（拖拽那组）。

---

## 五、没做 / 没查清

1. **`EffectResolver.cs:2058` 那条更宽的闸（`card.Targeted != 0`）没做** —— 7 张卡要**逐卡核 `ta`**，
   `Smothering Decree` 那种「a friendly unit」若原版确实是 `target = 30`，去掉 pick 会变成「自动打第一个」= 另一类错。
   ⇒ **另开一条活**（判据文件 §GB-1 次选那段有全套清单与告诫）。
2. **`UM64` 的 `abilityBuffs` 数值差**（`ab1` = `+1 melee +1 ranged`，卡面只印 `+1 [Ranged]`）—— 判据文件已标「没查清」，
   本批**没动数值**。
3. **硬吸附档那个静态 `Vector3` 的身份**只有「推断 = zero」，没有运行时实证（要探针读 il2cpp 静态区）。
4. `B117-18` 的 `Has` 影响面：全表只有 `ASH15` 一张卡会翻（`voice_lines.json` 642 张里 641 张至少有一条 SO 归属行）。
   但 `Editor/BattleScene.cs:15383 / 21931 / 22317` 三条用 `Has` 做「跳过没语音的卡」的循环**没有跑过**
   （宿主要 Unity）⇒ 建议调度台在接线那一批里带一次 `BattleScene.Run`。

---

## 六、账落点守恒（自查）

| 文件 | 改动前 | 改动后 | 结论 |
|---|---|---|---|
| `RuleEngine/Core/EffectText.cs` | 11,226 行 | 11,226 行 | ✅ 零行数变化（判据注释**追加在同一物理行**，没有插行） |
| `CardPresentation/Hand/CardInteraction.cs` | 2,250 行（**CRLF**） | 2,250 行（**CRLF**） | ✅ 零行数变化 + 行尾未翻（`git diff --numstat` = 20/20） |
| `CardPresentation/Core/VoiceLines.cs` | 923 行 | **947 行** | ✅ `Has` 那 6 行原样换 6 行；新增的 24 行全在**类尾**（最大账落点 `:686` 之后）⇒ 无漂移 |

`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` 的本批三个文件：**0 条漂移 / 0 条失败**
（同一跑里的 `漂移 56 / 失败 7` 指向的是别人的在飞文件 —— `CardChoicePanel.cs` / `MultiCardDisplay.cs` /
`ErrorMessageBanner.cs` / `VoiceLines.cs` 的**旧有**注释行告警，均非本批落点）。
