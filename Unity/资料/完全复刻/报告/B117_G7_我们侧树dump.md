# B117_G7 — 「我们侧」运行时 UI 树 dump（NodeDump）

> 状态：**代码已落盘、`typecheck 0/0`；Unity 批处理由调度台统一跑**（本轮 ⛔ 未跑 Unity，见铁律 12 / 15 条「同工程只能一个实例」）。
> 文件：`Unity/MyGame/Assets/CardPresentation/Editor/NodeDump.cs`（**新文件，未改任何已有运行时 `.cs`**）。

---

## 一、它解决什么

`工具/prefab_layer_diff.py` 能比「Unity YAML ↔ 15 列运行时 TSV」，但那份 dump 一直是**原版自己**的
（`SceneJumpShot` mod → `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_*.tsv`）。
我们这边——外壳（主菜单/左栏）· 收藏卡池 · 卡组编辑 · 战斗 HUD · 战斗大图详情窗——**全是运行时 `.cs` 建的**
（`CardPresentation/Scenes/Battle.unity` 只有 31 个 GameObject）⇒ **没有任何可机读的树**。
`NodeDump` 把我们**运行时建出来的窗口树**按**同一套 15 列**吐成 TSV。

---

## 二、入口用法

```text
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod NodeDump.RunFromCLI \
  -wfDumpWindow <slug> -wfDumpOut "d:/4/Unity/_tmp_view/nodedump/<slug>.tsv" -logFile -
```

| slug | 建的是哪一棵树 | 走的生产 API |
|---|---|---|
| `shell` | 主菜单窗 + 左竖导航（出厂态，不开任何弹窗） | `MainMenuScene.BuildForWindowShots()`（只建不存） |
| `collection_pool` | 收藏窗 · 卡牌页 | `MainMenuRuntime.InstanceOrFind().OpenCollection()` |
| `deck_editor` | 卡组编辑窗 | `DeckScene.BuildForWindowShots()`（内部把 `DeckStore.OverridePath` 指到**临时档**，不碰真卡组） |
| `battle_hud` | 战斗整棵树（含 HUD 画布 / 棋盘 / 手牌） | `NewScene` → `BattleScene.BuildScene(out driver, …)` → **三件 `Build()` + `driver.Begin(Ember, Tide, 20260911)` + 推一段补间**（逐句照 `BattleScene.RunCore:646-652`） |
| `card_display_window` | 战斗大图详情窗 + 卡体 | `CardDisplayWindow.Create(null)` → `Show(真卡)` |

**写盘纪律（照本工程铁律）**：`-wfDumpWindow` **或** `-wfDumpOut` 缺任何一个 ⇒ **只打印用法、`EditorApplication.Exit(2)`、零写盘**；
slug 不认识同样 rc=2。真写盘只在「建场成功 + dump 完成」之后发生（中途异常不落半份文件）。
`EditorApplication.Exit` 只在 `Application.isBatchMode` 下调用（⛔ 不会从菜单里把编辑器关掉）。

---

## 三、15 列对照表（权威 = `SceneJumpShot.cs:41-97` 的 `DumpGo`）

| # | 列（真实含义） | 原版代码 | 我们的实现 | 备注 |
|---|---|---|---|---|
| 0 | `path` | `path` 参数 | 同 | 🔴 **父节点的全路径**，不是自己的；根行 `path == name` |
| 1 | `name` | `go.name` | 同 | |
| 2 | `activeSelf` | `go.activeSelf` | 同 | `True`/`False` |
| 3 | `activeInHierarchy` | `go.activeInHierarchy` | 同 | |
| 4 | `rect` | `RT` / `TF` | 同 | `RectTransform` 有无 |
| 5 | `anchoredPosition` | `F1` | 同 | 只在 `RT` 行有值 |
| 6 | `sizeDelta` | `F1` | 同 | |
| 7 | `anchorMin` | `F3` | 同 | |
| 8 | `anchorMax` | `F3` | 同 | |
| 9 | `pivot` | `F3` | 同 | |
| 10 | `TXT` 标记 | `"TXT"` / 空 | 同 | |
| 11 | `text` | 🔴 `GetField("m_text")` ⇒ **恒空** | ✅ `TMP_Text.text` | **本实现修掉了原版那条失效路** |
| 12 | `fontSize` | 🔴 `GetField("m_fontSize")` ⇒ **恒 0** | ✅ `TMP_Text.fontSize` | 同上 |
| 13 | `IMG` 标记 | `"IMG"` / 空 | 同 | |
| 14 | `sprite` | `Image.sprite.name` | `Image.sprite` → `RawImage.texture` → **`ImageQuad.Texture`** → `SpriteRenderer.sprite` | 见 §五·② |

⚠️ **表头故意照抄原版那行 11 列错位表头**（`path name activeSelf activeInHierarchy rect pos pivot text fontSize color image`）。
`prefab_layer_diff.load_dump_tree` 用 `first[0]=="path" and "anchoredPosition" not in first` 判表头并跳过 ——
写一行「正确的 15 列表头」（含 `anchoredPosition`）会被它**当数据行**读成一颗假根节点。⛔ 别「修正」表头。
产物编码 = **UTF-8 带 BOM**、行尾 `\r\n`（与原版 `new UTF8Encoding(true)` 同款；工具侧会自己剥 BOM）。

---

## 四、期望读数（调度台跑的时候对着看）

`shell` 这一档（验收第 1 步）：

| 读数 | 期望 | 为什么 |
|---|---|---|
| `行数（含表头）` | **> 100**（主菜单 + 左竖导航整棵树） | 行数 0 ⇒ rc=1 |
| `首节点 = … · path==name（是根）= True` | **True** | 「首节点是根」 |
| `text 非空` | **> 0** | 原版 dump 这里是 **0/324**；我们**必须 > 0**，否则就是又退回 `GetField` 那条路（rc=1） |
| `fontSize 非零` | **> 0** | 同上（原版恒 0） |
| `sprite 非空` | **> 0** | `ImageQuad` 等价映射有没有接上 |
| 退出码 | **0** | 上面任一条不成立 ⇒ rc=1 |

日志里逐条印（`[NodeDump] ` 前缀）：
```text
[NodeDump] 行数（含表头）= N · 节点 = N-1
[NodeDump] TXT 标记行 = a · text 非空 = b · fontSize 非零 = c（原版 dump 这两项是 0 / 0 …）
[NodeDump] IMG 标记行 = d · sprite 非空 = e
[NodeDump] 首节点 = `MainMenu` · path==name（是根）= True
[NodeDump] 已写出：d:/4/Unity/_tmp_view/nodedump/shell.tsv
```

**验收第 2 步（dump 自比，证明列序与工具对得上）** —— 本轮**已经离线做完**（不需要 Unity）：

```text
python -I -X utf8 d:/4/Unity/工具/prefab_layer_diff.py \
  --orig "d:/4/Unity/_tmp_view/nodedump/_selftest/sample.tsv" \
  --ours "d:/4/Unity/_tmp_view/nodedump/_selftest/sample.tsv" \
  --out  "d:/4/Unity/_tmp_view/nodedump/_selftest/_diff.tsv" \
  --root-orig MainMenu --root-ours MainMenu --ours-dead-cols "" --dry
```

实测输出（本轮真跑过）：

```text
源 = …sample.tsv (dump)
  表头 ⚠️ 错位（写 11 列，真实 15 列；本工具按真实列序读）
  节点 5 · 根 2
  根: MainMenu
  根: Main Camera
节点对齐：对上 4 · 仅原版有 0 · 仅我们有 0
属性比对 31 条：一致 31 · 不一致 0 · ? 0
```

`sample.tsv` 是按本文件 §三 的 15 列手工造的一份（带 BOM / CRLF / 错位表头 / 多根 / 一个 TXT 行 / 一个 IMG 行），
生成脚本 `d:/4/Unity/_tmp_view/nodedump/_selftest/make_sample.py`。**31/31 一致** = 列序、表头跳过、根判定、`text`/`fontSize`/`sprite` 三个真正参与比较，全部与工具对齐。

> 🔴 **跑真 dump 时必须带 `--ours-dead-cols ""`**：`prefab_layer_diff.py:1179` 的默认值是 `"fontSize,text"`
> （那是为**原版那份坏 dump** 设的口径）。我们这一版 TXT 组是**活的** —— 不带这个参数会把它们记成 `?`、白白丢掉两列比对。

---

## 五、与原版的差异（如实登记）

**① 两处「必须不同」（为了读得到真值）**
- **TXT 组**：原版 `GetField("m_text")` 不爬基类 ⇒ `text` 324/324 全空、`fontSize` 全 0；我们走公开 API `TMP_Text.text/.fontSize`。
- **IMG 列**：我们的图件主力是 `ImageQuad`（自建 mesh quad，**不是** uGUI `Image`）⇒ `sprite` 列做等价映射
  （`Image.sprite` → `RawImage.texture` → `ImageQuad.Texture` → `SpriteRenderer.sprite`）。
  ⚠️ 这是**等价映射**、不是原版实现（原版只有 uGUI `Image`），代码里已写死这条注释。

**② 两处结构差异（不是 bug，但会带来「仅我们有」行）**
- 我们的 `Label` 把 TMP 建在**名为 `text` 的子节点**上（`Battle/Label.cs:1592` `TmpFont.NewTextUgui(transform,"text",…)`），
  原版 TMP 挂在节点自己身上。⇒ dump 做了两件事：**把 text/fontSize 提升到 `Label` 节点**（让它与原版同一格可比）
  **＋ 跳过那颗纯实现细节的 `text` 子节点**（无子件 · 只挂 TMP · 父上有 `Label`）。否则每颗字都多一行噪声。
- 我们的世界空间件（`ImageQuad` / 点阵 `Label`）挂在**普通 `Transform`** 上 ⇒ `rect` 列 = `TF`、几何五列全空 ——
  这与原版 dump 对 `Transform` 节点的处理**逐字相同**。

**③ 根的顺序**：目标那棵**排第一**（让「首节点是根」成立），场景其余根按引擎序跟在后面（原版 `DumpTree` 也是遍历全部根）。
`card_display_window` 的目标节点不是场景根（它在 `BattleHud Canvas ▸ …` 下）⇒ 先沿父链爬到顶再排第一；
卡体挂在**另一个根** `Card Display Window (world)` 上，所以那棵也会被 dump（否则卡体整段丢失）。

---

## 六、还差什么 / 调度台要跑的命令

1. **跑 `shell`**（第一条要验的）：

   ```text
   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
     -projectPath "D:\4\Unity\MyGame" -executeMethod NodeDump.RunFromCLI \
     -wfDumpWindow shell -wfDumpOut "d:/4/Unity/_tmp_view/nodedump/shell.tsv" -logFile -
   ```
   对着 §四 那张表看读数（尤其 `text 非空 > 0` / `fontSize 非零 > 0`）。
2. 另外四档同一条命令，只换 slug 与输出名（**必须串行**，同工程只能一个实例）。
3. 有了 `shell.tsv` 之后，就能跑第一次真正的**原版 ↔ 我们**差异：

   ```text
   python -I -X utf8 d:/4/Unity/工具/prefab_layer_diff.py \
     --orig "<原版 MainMenu / 左栏的 .prefab 或 .unity>" --root-orig <原版根名> \
     --ours "d:/4/Unity/_tmp_view/nodedump/shell.tsv" --root-ours MainMenu \
     --ours-dead-cols "" --out … --summary … --only-diff
   ```
   ⚠️ 原版那一侧用 YAML（`d:/2/tools/assetripper_out/ExportedProject/Assets/…`）还是原版运行时 dump，由界面的实际交付物决定；
   两边根名不同时用 `--root-orig` / `--root-ours` 各自指定（工具的 `select_subtree` 会把根重定基到同一标签）。
4. **还没覆盖**：
   - `battle_hud` 整棵树很大（战斗场景全量）⇒ 第一次跑先看行数与耗时，必要时后续按「只要 HUD 画布」再收窄（改一行 `FindRootByName("BattleHud Canvas")` 即可）。
   - 原版运行时 dump 是**全场景根**（含 `Scenario` / `Directional Light` / 粒子根），我们这一版只 dump **UI 场景的根**
     （战斗那一档是 `Battle` + `Main Camera`）⇒ 比较战场 3D 那一层时两边根集合不同，用 `--root-*` 各自指定。
   - 我们**没有**原版那种「一个场景里 13 套 arena 全挂载」的结构，所以 `battle_hud` 只能和原版的 `battlearena1` 那棵树比。
