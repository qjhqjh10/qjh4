# B141_W129_AllowClick — 成就页 `allowClickWhenAlreadySelected`（`B141-ALLOWCLICK`）

执行写手：`B141-W66`（第一百二十八会话 · 2026-11-04）· 批次 `B141` · 波次 `W129`
输入：`报告/B141_W129_HoverRest.md` §❓2（W60 只读发现，未改）
落点：`Shell/AchievementsMenu.cs`（改）· `Shell/PlayerProfileWindow.cs` / `Shell/MainMenuSubmenuWindow.cs`（只读复核，零改动）
断言：`Editor/B141_W66_Assert.cs`（新建，16 条）

---

## 一、结论（一句话）

**W60 的发现成立**：原版成就页 5 颗 `Achievement Type Toggle` 的 `allowClickWhenAlreadySelected` 全是 **1**，
同窗左栏 6 颗才是 **0**；我们原来是 **false（= 0 的语义）**，已改成 **true**，
并就地订正 `AchievementsMenu.cs` 里那两句反了的注释；`Select()` 里的「同值早退」也按原版回调体删掉（原版**没有**那一段）。

---

## 二、判据（逐颗现读，只读）

命令（现读、可复跑，脚本是 W60 留下的只读工具）：

```bash
python -I -X utf8 d:/4/_tmp_view/b141_w60/list_toggles.py \
  "d:/2/tools/assetripper_out/ExportedProject/Assets/Player Profile Window.prefab"
```

现读输出（`allowClick` = `EverguildToggle.allowClickWhenAlreadySelected`，字段偏移 `0x139`）：

| 节点族 | 节点 | `m_IsOn` | `allowClick` |
| --- | --- | ---: | ---: |
| **成就页** `Menu Area/Tab  Area/Tab Content/Trophies Tab/buttons/*` | `Achievement Type Toggle` | 0 | **1** |
| 同上 | `Achievement Type Toggle (1)` | 0 | **1** |
| 同上 | `Achievement Type Toggle (2)` | 0 | **1** |
| 同上 | `Achievement Type Toggle (3)` | 0 | **1** |
| 同上 | `Achievement Type Toggle (4)` | 0 | **1** |
| **左栏** `Menu Area/Tab Buttons/*` | `Profile Button` | 0 | **0** |
| 同上 | `Avatar Button` | 0 | **0** |
| 同上 | `Title Button` | 0 | **0** |
| 同上 | `Battle Log Button` | 0 | **0** |
| 同上 | `Trophies` | 0 | **0** |
| 同上 | `Ranked` | 0 | **0** |

⇒ **成就页 5/5 颗 = 1 · 左栏 6/6 颗 = 0**（两族**同一个 prefab**，正好互为反证；⛔ 别再拿其中一族去代表另一族）。

### 回调判据（决定 `Select()` 要不要留同值闸）

`d:/2/tools/decomp_full/AchievementsMenu.__c__DisplayClass6_0___Awake_b__0.c`：

```c
UnityEngine_UI_ToggleGroup__NotifyToggleOn(lVar1, *(undefined8 *)(param_1 + 0x18), 0, 0);
*(undefined4 *)(*(longlong *)(param_1 + 0x20) + 0x58) = *(undefined4 *)(param_1 + 0x10);   // filter = type
AchievementsMenu__Refresh(*(longlong *)(param_1 + 0x20), 0);                              // Refresh()
```

**体里没有「`filter == type` 就 return」那一段** ⇒ 原版再点**当前**分类 = 重放回调 + 按钮音 + 重画。
我们的等价物必须是「`allow = true` + `Select()` 不设同值闸」两件**同时**成立；只改一件都会静默少一半行为
（只改 `allow` ⇒ 有声音没重画；只删闸 ⇒ 有重画没声音，因为守卫仍在前一档就吃掉了点击）。

---

## 三、改动（逐处）

### 1. `Shell/AchievementsMenu.cs`（本批唯一改代码的文件）

| 处 | 改前 | 改后 |
| --- | --- | --- |
| `BuildTabs()` 页签赋值（现 `:279`） | `tabWb.allowClickWhenAlreadySelected = false;` + `tabWb.isOn = () => _filter == captured;` | `tabWb.allowClickWhenAlreadySelected = true;`（`isOn` 不再设 —— 见下） |
| 该处注释（`:272-275`） | 「原版这一族 `allowClickWhenAlreadySelected = 0`…」 | 订正为「**写反了**：成就页 5/5 = 1、左栏 6/6 = 0」，带判据工具与回调 `.c` 出处 |
| `Select()`（`:289`） | `if (_filter == type) return;  // 原版 allowClickWhenAlreadySelected = 0` | 删掉早退，换成判据注释（原版 `b__0` **无**同值闸） |

为什么 `isOn` 一起删：`WindowButton.Click()` 的守卫是
`if (!allowClickWhenAlreadySelected && isOn != null && isOn()) return;`
—— `allow == true` 时 `!allow…` 为假，**整条短路**，`isOn` 永远不会被求值（断言 B6 专门钉这一点）。
留着它只会让下一个人以为「这里还有一个已选中判定」。

### 2. `Shell/PlayerProfileWindow.cs`（✅ 只读复核 · 零改动）

`:420` 仍是 `_btnWb[i].allowClickWhenAlreadySelected = false;`，且处在**同一条循环赋值**里
（`for i < Tabs.Length`，无按 index 特判）⇒ 左栏**每一颗** = false，与原版 6/6 颗 0 一致。
⚠️ 运行期实际只建 **5 颗**（`Ranked` 那一颗按用户 2026-10-24 口径 `SetActive(false) + continue`）——
prefab 族 = 6 颗，运行期 5 颗，两件事都记下，⛔ 别把「运行时 5」当成「原版 5」。

### 3. `Shell/MainMenuSubmenuWindow.cs`（✅ 只读复核 · 零改动）

`:907` 仍是 `tabWb.allowClickWhenAlreadySelected = false;`（奖励/商店/收藏/社交四扇窗左栏共用这条）——
原版这几族同属「左栏 = 0」那一类，**没被本批顺手改掉**（断言 A6 钉住）。

### 4. 行数变化

`git diff --numstat`：`10 10 Unity/MyGame/Assets/CardPresentation/Shell/AchievementsMenu.cs`
⇒ **增 10 / 删 10，净 0 行**（注释块 10 行 → 10 行；`Select()` 的早退 1 行 → 判据注释 1 行）。
账锚点逐条复核仍在原位：`:160`（勋章档注释）· `:231`（`BuildTabs`）· `:320`（`RebuildCells`）·
`:356`（`BuildCell`）· `:634`（`OnOpen`）。行尾：**LF**（`\r\n` = 0），未被翻。

---

## 四、断言（`Editor/B141_W66_Assert.cs`，16 条）

- **A1~A7（源码文本 / 静态表，8 条）**：成就页赋值 = true 且**零处** `= false`；`Select()` 里**没有**同值早退；
  左栏两支仍是 false；颗数 = `ProfileData.TypeOrder.Length` 4（原版枚举 4 值）/ `PlayerProfileWindow.Tabs.Length` 6。
- **B1~B7（运行时探针，8 条）**：新实例缺省 true；`allow=false + isOn=true` **不派发**；
  **🧨 灭自证** = 同一颗、同一个 `isOn`，只把 `allow` 翻成 true ⇒ 立刻派发；
  `allow=true` 连点两次派发两次（成就页语义）；未选中时 false 照常派发；
  `allow=true` 时 `isOn` **0 次被调用**；`absorbOnly` / `Interactable` 保持缺省（证明「0 次」只来自这条守卫）。
- **灭自证的关键**：B2/B3 用**同一个对象、同一个 `isOn`**，唯一变量是 `allow`
  ⇒ 若有人把 `Click()` 的守卫删掉（两态都派发）或改成恒拦截（两态都不派发），两条必有一红。
- 未接宿主（本文件不碰聚合）：调度台按文件头「怎么接」同一物理行追加
  `B141_W66_Assert.Run(); pass += ...Pass; fail += ...Fail;`。

## 五、验证

- `TMPDIR=/tmp/wf_b141_w66 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 · 编辑器错误数 0**
  （注：`工具/` 在 `d:/4/Unity/` 下，不是 `d:/4/工具/`）。
- 未跑 Unity（红线）⇒ 断言未执行，条数以代码为准（14）。

---

## 六、❓ 没做 / 顺带发现（只记，未改）

1. ⚠️ **`Shell/WindowButton.cs:1248` 的举例已过期**：那段注释写「即使 onClick 里自己写了『同一键再点就 return』
   （例如 `AchievementsMenu.Select`），声音已经响过了」—— 本批删掉了 `Select()` 的同值早退，
   这个举例**现在不成立**（那段注释的道理本身仍然成立，只是例子该换）。
   ⛔ 不在本包白名单（`WindowButton.cs`），留给音效线/该类写手。
2. ❓ **按钮音这一半没验**：本批只验了「派发闸」（`onClick` 是否被调）。
   「`allow = false` 时连按钮音一起跳过」需要 A/V 宿主（`WindowSounds.PlayFx` 真播）——
   探针里 `playClickSound = false` 绕开了它，报告中如实标「未验声音」。
3. ❓ **可观测性**：`allow = true` 后，再点当前分类会**重建格子**（`RebuildCells` 先清再建，内容同）。
   画面结果与改前肉眼相同（同一批格子），**唯一可观测差异是按钮音重放** ⇒ 真机验收要**听声音**，
   截图比不出来（见 ❓2）。
4. ✅ 已确认**不需要**改：`MainMenuScene.cs` 既有夹具只点「未选中的分类」（tab1 → Battle），
   不会因删早退而变红；全仓无其它地方给成就页签设 `allowClickWhenAlreadySelected`。
