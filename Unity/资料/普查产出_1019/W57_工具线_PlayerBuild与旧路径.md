# W57 · 工具·文档线 `TOOL-11` —— `A1484②` / `A1483` / T01-T02 旧路径订正

> 执行者：`TOOL-11`（全权限执行代理）。⛔ **未跑 Unity / 未起 player / 未跑任何 `*.Run` / 未动 git**。
> 白名单（**只改了这三个文件**）：
> `Unity/MyGame/Assets/CardPresentation/Editor/PlayerBuild.cs` ·
> `Unity/资料/任务线/工具文档线/任务/T01_Godot时代清理.md` ·
> `Unity/资料/任务线/工具文档线/任务/T02_打包与player.md`

---

## 一、逐项结论（原值 → 新值 + 判据）

### 1·1 `A1484②` —— 5 条 `-wfscene` 命令「只打印不执行」

- **现核（改前）**：`PlayerBuild.Build()` 尾部 `foreach (var sc in Scenes)` 只 `Debug.Log` 拼出命令行，
  注释写「复现『跑起来看什么』那一步用到的命令」⇒ 日志读起来像**已经跑过**。
- **判据原文**：`资料/待办判据_第三十三会话.md` 的 `A1481` 行 ——「`PlayerBuild.cs:225-240` 只打印不执行，
  打印得像『跑过了』—— 这一段本身就该改（**要么真跑、要么别写成跑过**）」。
- **选择：按后者（不真跑）**，理由三条：
  ① 起 player 是**验收动作**（现行口径「先复刻、别检查」）；
  ② 五条合计 ≈ 6 分钟（Battle 一条 `-wfquit 240`）；
  ③「批处理构建里起 GUI player」**本身没验过**（铁律 2：查不到就不猜，不把未验行为塞进构建脚本）。
- **新值**：每行前缀改 **`【未执行 · 手动复制】`**；命令由裸文件名 `WarpforgePlayer.exe`
  （只有 cwd = exe 目录时才跑得起来）改成**全路径**（任何 cwd 都行）。
- **六个参数语义与落点**（判据 = `Core/PlayerBoot.cs` 的常量表 + `Run()` / `LoadSceneMatching` / `Auto()`）：

| 参数 | 语义 | 落点 |
|---|---|---|
| `-wfscene <子串>` | 切到「场景名含该子串」的 build-list 场景（`IndexOf` 不区分大小写；**找不到会 `LogError`，不静默**） | 场景切换 |
| `-wfshot <文件>` | `ScreenCapture.CaptureScreenshot` 截图（**帧末才写盘**，见 `PlayerBoot` 纪律③） | `_tmp_view/player/<name>.png` |
| `-wfshotat <秒>` | 第几秒拍（默认 8） | — |
| `-wfquit <秒>` | 第几秒退出（默认 = `shotat + 5`） | — |
| `-wfdrive` | **只对 Battle 有意义**：`BattleAutoDrive` 自动打一局 | 额外 `auto_<序号>_<阶段>.png` 落在 `-wfshot` 的**目录**（判据 = `Core/BattleAutoDrive.cs:517`） |
| `-logFile <文件>` | Unity player 内置参数：日志写到这里（否则落 `LocalLow/<company>/<product>/Player.log`） | `_tmp_view/player/<name>.log` |

- **为什么是 5 条**：`Scenes` 现读 5 项（Shell / Battle / DeckEditor / MainMenu / VFXWhiteboard）。
- ⚠️ 真机点击验收（`wf_click.py`）时 **Shell 要显式全屏**：再补
  `-screen-fullscreen 1 -screen-width 2560 -screen-height 1440`（判据 → `资料/交接_第五十一~五十三会话.md`）。

### 1·2 `A1484②` —— 两处过期注释（都已订正，保留原文 + 更正痕）

| 位置（改前行号） | 原值 | 新值 / 更正痕 | 判据 |
|---|---|---|---|
| `PlayerBuild.cs:3` | 「本工程**从来没有构建过 player**」 | 加「2026-10-19 就地订正（铁律 5 · `A1484②`）」块：**只对 2026-09-16 当天成立**；此后已多次构建（最近 **2026-10-18 19:37**，产物在 `_tmp_view/player/`） | 交接_第五十一~五十三会话 + `_tmp_view/player/` |
| `PlayerBuild.cs:12` | 「『构建后 player 验证』是 `项目任务.md` 顶部待办表的**第 1 行**」 | 加订正块：**账源指空** —— `d:/4/项目任务.md` 里 `grep "构建后 player"` **0 命中**；「三条线都欠它 / 所有绿灯都来自编辑器」也过期；现行账 → `任务线/工具文档线/任务/T02_打包与player.md` | 现读 `项目任务.md` |

### 1·3 `A1483` —— 启动场景是白板（✅ 已本地改；验收需 Unity）

- **原值**：`Scenes` = `0 VFXWhiteboard / 1 Battle / 2 DeckEditor / 3 MainMenu / 4 Shell`。
- **新值**：`0 Shell / 1 Battle / 2 DeckEditor / 3 MainMenu / 4 VFXWhiteboard` —— **只交换首尾两项**
  ⇒ `Battle` / `DeckEditor` / `MainMenu` 的 build index 不变。
- **判据/理由**：`MainMenu.unity` 里 `WindowsManager` / `PointerLayer` 都是 **0 处**，`Shell.unity` **自带**
  （判据 → `资料/任务线/界面线/任务/T02_主菜单与外壳.md` §2·1：「单独 `-wfscene MainMenu` 时没有任何人建指针层」）；
  壳起来后自己会建主菜单窗 ⇒ **双击 = 壳 → 主菜单 → 可点**。
- **为什么没选 `MainMenu` 打头**：它没有指针层，双击进去也**点不动**。
- **`PlayerBoot` 不需要改**：无参数直接 return（现读 `:93-95`，原引 `:86-88` 已漂）⇒ 停在 build list 第一项，
  现在第一项就是 `Shell`。`PlayerBoot` 按**名字子串**切场景，不吃 build index（`LoadSceneMatching` 现核）。
- **还差**：**重出一次 player（`PlayerBuild.Run`）+ 双击验收**（属「需 Unity/真机」，本波不做）。

### 1·4 `A1482` —— 本波不动：前置与命令（需 Unity）

- **现状**：`Shell.unity` 里 3 个 `MonoBehaviour` 仍指向**空壳** `Shell/ShellParts.cs`（该文件已无 `class` 声明）；
  `Player.log:41-43` 那 3 条按 GameObject 点名的 `missing script` 就是它们。功能上不影响
  （`ShellRuntime.cs:484-485` 运行时自己 `AddComponent<Shade>()` / `AddComponent<BlockingOverlay>()`）。
- **前置与命令（按顺序）**：
  1. **先备份** `Assets/CardPresentation/Scenes/Shell.unity`；
  2. 跑一次 `ShellScene.BuildAndSaveScene`（或 `ShellScene.Run`）**重存场景**（生成器已按新类建件）；
  3. 确认 `ShellScene.cs` 的 `SceneStubAudit` 零输出 ＋ `grep -c "^--- !u!115" Shell.unity` = **0**；
  4. **最后才删** `Assets/CardPresentation/Shell/ShellParts.cs`（先删 ⇒ 悬空 guid，同样是 `missing script`）；
  5. 跑完 `git diff --numstat` 对一眼（该宿主会**整篇重写**场景）。
- 判据全文 → `资料/普查产出_1019/W_工具线_T02打包_1019b.md` §四·1。

### 1·5 `T01` 旧路径 / 过期说法（订正为现读，保留原文）

| 处 | 原来写的 | 现读 / 订正 | 判据 |
|---|---|---|---|
| `A2112` 数据侧 | 「仍开；两族**可归档**；`convert_unity_particles.py` 写回 `D:/warpforge` **未动、请人工裁**」 | **✅ 2026-10-19 已做完**：1,121 个文件已归档到 `Unity/数据/历史_Godot快照/`（3+201+863+54，逐字节 1,121/1,121 相同）；`sync_from_d2.py` 落点改指归档 + `_assert_not_writing_into_source()` 写保护；`convert_unity_particles.py` 现 `--out` 必填、拒写 `D:/warpforge`（退出码 2）；`verify_archive.py` 20/20 | `普查产出_1019/W_工具线_T01数据侧_1019b.md` §一 |
| 同上（**关键口径**） | — | **写入侧不再指向 `d:/warpforge`**；但**读取侧 `GDATA = 'd:/warpforge/data'` 仍是【只读历史播种源】**（⛔ 非判据）—— 没有写成「完全不指向」，按现读说清 | `Unity/工具/sync_from_d2.py:12-20,99-113,241-246` |
| 同上（19 份） | 「13/19 相同、6 份续改」 | 现读 **15/19 逐字节相同、4 份已被 d:/4 续改**（D5 的 13/6 已过期）；**⛔ 仍不许删那 19 份**（`card_stats.json` 无 d:/4 侧生成器） | 同上 §3.1 |
| `A2113` | 「⏭ 仍开着（文档还写 chain_rect）」 | **✅ 2026-10-19 文档侧已做**（`卡组编辑界面_查证_0920.md`、`普查产出_1005/块8` 已加更正）；C# 常量按 prefab 复核吻合 | `W_工具线_T01数据侧_1019b.md` §一·6 |
| `A2114` | 「**6 处仍在驱动运行时**」+ 旧行号/旧值（466/708、0.3444、0.1204…） | **✅ 6 处全落地**：①–④ 2026-10-16、⑤ 2026-10-19；⑥ 归 `A1762`/`A1763`。现读：`BattleScene.cs:148-149` = 0.6063/0.4110 · `BoardLayout.cs:80` = 0.4110 · `Battle.unity:2001/2365` · `CardBase.unity:3906` · `CardInteraction.cs:516-520` 走 `HandLimitY` · `HandLayout.cs:106` = `1.1*108/DesignPxH`（0.11）· 场景 `Battle.unity:1056=0.11 / :1249=0.9997037` · `CardBase.unity:46978=0.11` | `W_工具线_T01施工图_1019b.md` §一 + 本轮现读 |
| `A2114` 顺序铁律 | 「先拆 `A2104①`，⛔ 不许跳过」 | **✅ 已满足**（2026-10-16 拆完并存盘）⇒ T01 不再被阻塞 | 施工图 §二 |
| §三 / §四 / §六 | 数据侧「仍是」、代码侧「6 处驱动」、`chain_rect` 待裁、零引用待复核 | 全部按上面现读改写；`A2113` / `A2112` 标 ✅；`0.1204` 标「值已不用、出处仍未坐实」；零引用项标 ✅ | 同上 |

### 1·6 `T02` 其余过期状态（同文件顺手订正，均带 2026-10-19 痕）

- `A1481` **关账**（现存根因 `!u!115` 6 场景全 0 · 最新崩溃 2026-10-10 23:18 · 当前 build 零 `corrupted`）；
- `A1482` 现读 **6 行** `missing script`（不是 49），后 3 条 = `ShellParts.cs` 空壳，**不是托管裁剪**；
- 产品名 **已改** `Everguild / Warpforge`（`ProjectSettings.asset:15-16`）；
- §三表 / §四 / §五 / §六 全部改成现读（`PlayerBuild.RunWithSubsetLibrary` → 验收用 `PlayerBuild.Run`）。

---

## 二、改动清单

| 文件 | 改了什么 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Editor/PlayerBuild.cs` | ① 文件头 `:3` / `:12` 两处过期注释加「2026-10-19 就地订正」块；② `Scenes` 首尾交换（Shell ↔ 白板）+ 理由注释；③ `Build()` 尾部打印块：显式「未执行 · 手动复制」、全路径、六参数语义/落点注释（`A1484②`）；④ 删除「追加在表尾、不动启动场景」那句（已不成立，改为 2026-10-19 注） |
| `Unity/资料/任务线/工具文档线/任务/T01_Godot时代清理.md` | 状态行 · `A2112` 全节现读（归档/写保护/落点/19 份口径）· `A2113` 标 ✅ · `A2114` 全节现读（①–⑥ + 历史原文）· §三表 · §四步骤 · §六 1/3/4/7 条；全部保留历史原文 + 更正痕 |
| `Unity/资料/任务线/工具文档线/任务/T02_打包与player.md` | 状态行 · `A1483` 已做块 · `A1481` 关账块 · `A1482` 6 行现核 · `A1484` 两处已改 + 5 条命令处置 · §三表 · §四状态 · §五验收口径 · §六 1/2/4/5 |

`git diff --numstat`：**PlayerBuild.cs 52/11 · T01 74/51 · T02 69/21**；
三份仍为 **LF**（`CRLF=0`）；`git diff --check` 无空白错误。

---

## 三、没查清 / 需 Unity / 需拍板

1. **`A1483` 的验收**：需重出一次 player（`PlayerBuild.Run`）+ **双击 exe** 看是否落在壳/主菜单、能否点击 ——
   本波按口径不做；**还差这一条**。
2. **`A1482` 的修法**：需 Unity 重存 `Shell.unity` 后才能删空壳（命令见 §1·4）。
3. **`Packaging assets - resources.assets` 的 22 条错误内容仍未拿到**（只进二进制 `Library/LastBuild.buildreport`；
   仓库无解析器）——与 `A1319` 同源。
4. **`A2115①`（`GiveKw` 54 行按卡面重推）仍开** —— 不在本波范围。
5. **`A2116` 尾巴仍开**（横幅统一 / 机械降级批 / `战斗重建_0827/` 归档）—— 不在本波范围。

---

## 四、顺手发现（未改，⛔ 不在白名单）

1. 🔴 **`EditorBuildSettings.asset` 的启动场景仍是模板 `SampleScene`**：现读顺序 =
   `SampleScene → MainMenu → DeckEditor → Battle → Shell`。
   ⇒ `PlayerBuild.Scenes` 已改成 Shell 打头，但**若将来用 Unity Build 窗口出「真包」，第一场景仍是空模板**
   （与 `A1483` 同一类缺陷、另一份清单）。**建议主对话另派一笔**：把 `Shell.unity` 挪到
   `EditorBuildSettings.asset` 首位（或删 `SampleScene`）。⚠️ 本类按设计不动那个文件（文件头三条设计取舍①）。
2. **`T01` 的 `A2114⑤` 已由别的写手落地**（`HandLayout.cs` 2026-10-19 改成原版口径、场景已重烘）——
   任务书此前仍写「仍在驱动」，本轮已订正（判据 → `W_工具线_T01施工图_1019b.md` §一·⑤）。
3. **`资料/任务线/工具文档线/准则与做法.md:141`** 仍写「1,118 + 3 个文件 / 19 MB」（实测 **1,121**）——
   不在本代理白名单，建议主对话订正（前一轮 `W_工具线_T01数据侧_1019b.md` §四·2 已记）。
4. `Unity/工具/_同步日志_data.json` 仍记旧落点（上一次运行的历史日志，改它 = 伪造历史；前一轮已记）。
5. `PlayerBuild.Scenes` 与 `EditorBuildSettings.asset` 是**两份清单**（本类按设计只改前者）——
   两条路各自服务「验证构建」与「真包」，A1483 的修复目前只覆盖前者（见第 1 条）。

---

## 五、typecheck

```
TMPDIR=/tmp/wf_tool11 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

（2026-10-20 本机时钟运行；期间无「错误集中在别人正在写的文件」的情况。）
