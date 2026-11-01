# B115_R1 · `WindowShots` 物品展示窗修复（`B-303`）执行报告

> 2026-11-01 · 转岗执行（原只读诊断 → 有限写权限）· 判据全文 = `判据/B115_R1_物品展示窗空容器.md`
> **结论**：`B-303` = **宿主调错入口**（`WindowShots` 用无参 `OpenItemDisplay()` ⇒ `ctx = null` ⇒ 窗照原版 `No item available` 支不铺内容）。
> 窗本身没坏；三个生产调用点都给了 `Context`。

---

## 1. 改了什么（2 个文件）

| 文件 | 行 | 改动 |
|---|---|---|
| `Assets/CardPresentation/Editor/WindowShots.cs` | `:144-161` | **新增** `static string SampleCardback()`：`CardbackTable.DefaultFor("Ultramarines")`（现读 = `Cardback_UM_Campaign_Free`）→ 退 `CardArt.CosmeticNames()[0]` → 都取不到则 `LogError` + 返回 `null`（**不静默**，那一行会照旧 rc=1） |
| 同上 | `:218-225` | 内建窗表那一行：`T(WindowsManager.OpenItemDisplay())` → `T(WindowsManager.OpenItemDisplay(ItemDisplayWindow.ForCardback(SampleCardback())))`，上方补 4 行「为什么必须给它内容」注释（带 `B-303` / `B115_R1` 出处） |
| `Assets/CardPresentation/Shell/ItemDisplayWindow.cs` | `:290-295` | **只改注释**（零行数变化，`Build()` 首句仍在 `:296`）：`// —— **一个节点都不建**` → 改成「`Open()` 不铺内容；prefab 静态子树仍在（`GameWindow__TryOpen.c:44-47` 先 `SetActive(true)`）⇒ 原版画面 = 空骨架；我们零节点（另立账）」，带 `⚠️ 2026-11-01 更正（B115_R1）` |

⛔ **没动**：`OpenItemDisplay`（不让「无参=给默认卡背」，原版 `null` 就是错误支）· 任何 `账/*.tsv` · `活状态表.md` · `CLAUDE.md` · `项目任务.md` · `Editor/{BattleScene,ShellScene,RewardsScene}.cs` · git · Unity。

## 2. 机检读数（都过）

| 检查 | 命令 | 结果 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_r1 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 聚合行闸 | `python -I -X utf8 d:/4/Unity/工具/check_aggregate_lines.py <两文件>` | **聚合行 0 · 调用 0 · 违规 0 ⇒ PASS（rc=0）** |
| 账引用面 | `grep -rc "WindowShots" Unity/资料/完全复刻/账/*.tsv` | **0**（加行前后都是 0） |
| 行数守恒 | 二进制 `b.count(b'\n')` | `ItemDisplayWindow.cs` **573 → 573**（账钉的 `:266/:288/:296/:532/:406/:191` 全原位）；`WindowShots.cs` 762 → 786（该文件不在任何账落点里） |
| 行尾 | 同上（`\r\n` / 裸 `\r`） | 两文件都是 **纯 LF · 裸 CR 0**，未翻行尾 |

## 3. 要跑的实测（⛔ 我没跑 Unity，调度台跑）

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod WindowShots.RunFromCLI -wfWinOnly item_display -logFile -
```

**看三处**：

1. 日志末行 `OK  物品展示窗 (item_display) rc=0 —— … 子件 渲染件 N / 文本 M`
   ⇒ 期望 **rc=0 / N ≥ 1 / M ≥ 3**（`LoreText` 命中时 M = 4）。
2. `d:/4/_tmp_view/shots/win/_index.tsv` **第 17 行**：`rc` 列 1 → 0，「渲染件 0 / 文本 0」消失。
3. `d:/4/_tmp_view/shots/win/item_display.png`：应有全屏压暗 + 右侧 `Panel`（Title/Description/Type）+ 底部风味条（Ultramarines 底图）+ `Item Holder` 里那张卡背。

**若仍 rc=1 的分流**：`[ItemDisplay] No item available` = ctx 仍空（看有没有同时出现 `[WinShot] 物品展示窗的样张卡背取不到…`）；
`[ItemDisplay] 卡背图取不到：…` = 那张 PNG 不在 `Resources/Art/cardbacks/`；`[CardbackTable] 取不到 Resources/Cardbacks.json` = 表没打进 Resources。

## 4. 留给调度台

1. `活状态表.md:388`（`B-303`）：把「⚠️ 未判是…」改成「**判 = 宿主调错入口**（`B115_R1` 已实现修法，待 Unity 实测）」。
2. **账里同病一句**：`账/菜单_外壳.tsv` 的 `ItemDisplayWindow.Open()` 判据列仍写「（一个节点都不建）」—— 与刚订正的注释同源，建议下次动账时一并改成「`Open()` 不铺内容（prefab 静态子树仍在）」。
3. **另一条待核（与本条无关，同批顺手发现）**：`welcome.png` 与 `tutorial.png` **逐字节相同**（md5 `8ef2438…`），而原版两个 prefab 节点数差很多（79 vs 30）⇒ 建议跑一次 `-wfWinOnly tutorial` 看有没有 `[Tutorial] 图取不到：40K_genearl_icon_Campaign points_big`，再决定是否另立账。
4. `B-303` 之外还有一条**独立保真缺口**（原版「无 item」时 prefab 静态骨架仍可见，我们是零节点）—— 判据文件 §修法二 写了做法与代价，**建议单独立账**，别混进本条。
