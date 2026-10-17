# W1 · SettingsPanel 静音行取图口修复

## 一、改了什么（文件:行号 · 改前 → 改后）

**唯一文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/SettingsPanel.cs`（纯 LF，`b.count(b'\r\n')=0`）。

`BuildMuteChatRow`（`:1554`）里两张底图的取图口：

| 行 | 改前 | 改后 |
|---|---|---|
| `:1556` | `var offTex = CardArt.Ui("40K_toggle_off");` | `var offTex = CardArt.Ui("40K_toggle_off") ?? CardArt.MenuUi("40K_toggle_off") ?? CardArt.DeckUi("40K_toggle_off");` |
| `:1557` | `var onTex  = CardArt.Ui("40K_toggle_on");` | `var onTex  = CardArt.Ui("40K_toggle_on")  ?? CardArt.MenuUi("40K_toggle_on")  ?? CardArt.DeckUi("40K_toggle_on");` |

- 两行上方加了 5 行注释，写明根因 + 先例出处 + `Volume_button` 为何不动。
- `:1558` `CardArt.Ui("Volume_button")` **未动**（它取得到，见二·4）。
- `git diff --numstat` = **7 / 2**（只加不删语义），**未翻行尾**（文件仍纯 LF）。

## 二、判据（每条带出处）

1. **`CardArt.Ui` 只查 `ui/`**：`Core/CardArt.cs:424-428`（`Get(Root + "ui/" + name)`）；`Root = "Art/"`（`:18`）。
2. **`CardArt.DeckUi` 只查 `ui_deck/`**：`Core/CardArt.cs:569-573`。
3. **`CardArt.MenuUi` 内部已含三级**：`:591` = `ui_menu/ ?? ui_deck/ ?? ui/` ⇒ 上面那条链第三档 `DeckUi` 其实冗余（保留是为**逐字照先例**，见五·2）。
4. **两张图真的只在 `ui_deck/`**（实查，非只信报告）：`find` 命中
   `Resources/Art/ui_deck/40K_toggle_off.png` · `…_on.png`；**`Resources/Art/ui/` 下 0 命中**；
   `ui_menu/` 下 0 命中。（`Volume_button.png` 反查：命中 `Resources/Art/ui/Volume_button.png`。）
5. **先例 = 三级兜底**：`Shell/AlliancePanelWindow.cs:759-770` 的 `Art()` = `CardArt.Ui(name) ?? CardArt.MenuUi(name) ?? CardArt.DeckUi(name)`（现读逐字）⇒ 本改**照抄那个写法**，未发明第四种兜底。
6. **红从哪来**：日志 `d:/4/_tmp_view/battlescene_run_1217.log` —— `settings_mute_bg_off/on` 的
   `[Settings]…贴图取不到 ⇒ 这一件不建` 出现在 `:2782/2799 · 128141/128156 · 128186/128201 · 185366/185381`
   （共 8 条 = 2 图 × 4 轮建）。**全程无 `settings_mute_check_*` 的同类告警** ⇒ 只有两张底图缺、勾图不缺。

## 三、类型检查结果

```
TMPDIR=/tmp/wf_w1 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```
**0 错，无别人的错混入。**（⛔ 未跑任何 Unity 自检 —— 白名单/红线要求只跑秒级类型检查。）

## 四、没能验的部分（⛔ 不猜）

1. **「两张底图在批处理自检里量到非零尺寸」未实测** —— 按红线不跑 Unity。**推断**（未验）：修后
   `offTex/onTex != null` ⇒ `Img(...)` 不再走 null 分支 ⇒ `_muteOffBg/_muteOnBg` 建出 ⇒
   `MuteChatRectPx` 能返 `true`。**真值待攒批验收时跑 `BattleScene.Run`。**
2. **A2（日志 128425 · `ChatToggle` 位置断言）我不保证随本改变绿** —— 诊断（R1 §聚类 2）判它是 **α**、
   自身仍红（`MuteChatWorldPos()` 用 `px/108f` 而批处理画布系数是 `1/144`），归 `Editor/BattleScene.cs`、
   **由别的写手修**。本改只是**去掉它被掩盖的前提**（bg 不再 null）。
3. **`40K_toggle_*` 的「正确归属目录」是工程约定、我判不了** —— `ui/` vs `ui_deck/` 两手都有先例
   （`AlliancePanelWindow.Art()` 三级兜底 · `CardArt.DeckUi` 存在）。⇒ 按简报**优先照先例**（只改代码取图口），
   **未挪图**（挪目录 = 动资源布局，风险更大，不在本活范围）。

## 五、顺手发现（⛔ 没动手）

1. `SettingsPanel.cs:1558` 的 `chkTex` 仍是**裸 `CardArt.Ui`** —— 现**取得到**（`ui/Volume_button.png` 在），
   故**不是缺陷**、未动。⚠️ 但同文件里这已是**仅剩**的裸 `Ui` 取图口（语言框 `:1072/1084/1253…` 早已走 `MenuUi`）；
   若日后有人把 `Volume_button` 也归并去重批次，会**静默不建**——留作提示，非本活。
2. `MenuUi`（`CardArt.cs:591`）**已含 `ui_deck/`** ⇒ 本改链里的 `?? CardArt.DeckUi(...)` **冗余**（无副作用）。
   为**逐字对齐先例** `AlliancePanelWindow.Art()` 而保留 —— 若调度台想精简，两处可一并去掉第三档。
3. **`CardArt.Ui(name)` 对「去重资源」批次的图恒 null** —— R1 §四·1 已提，本活再证一次。建议全仓
   `grep 'CardArt.Ui('` 普查有没有别的裸 `Ui` 喂了去重批次名字（**静默不建、只留一行 warning**）。**未动手。**
4. R1 §四·3（`BattleDriver.SettingsClickAt` 末尾恒 `return true`）**复核成立**，**未动**（不在白名单）。
