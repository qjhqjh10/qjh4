# T02 · 打包与 player —— **「双击 exe 就能进游戏」这件事到现在还没成**

> **状态**：🔴 **未完成**（判据齐）—— **2026-10-19 现读**：`A1481` ✅ 关账 · `A1483` ✅ 已改（待重出 player 验收）· `A1484①` ✅ 已改（产品名）· `A1484②` ✅ 已订正 · **只剩 `A1482`**（需 Unity 重存场景后才能删空壳）。
> **关联 A 编号**：`A1481`（✅ 已关账）· `A1482`（⚠️ missing script 现读 6 行，剩 3 个空壳组件）· `A1483`（✅ 启动场景已改 Shell）· `A1484`（✅ 两处都已改）
> **判据全集** → `资料/待办判据_第三十三会话.md` 的 `A1481` / `A1483` · `资料/任务_卡组卡牌战斗.md` §五（`A1482` / `A1484` 那一行）

---

## 一、要做什么

让**双击 `WarpforgePlayer.exe` 就能一路进到游戏**：
**开得了场（不进白板）· 进得去主菜单 · 点得动（指针层只有在壳里启动才有）· 卡组编辑不崩**。

⚠️ **为什么是四笔一件**：它们**互为前提** ——
`A1483`（启动场景是白板）⇒ **指针层永远不起** ⇒ **player 里点不动**；
`A1481`（`Shell` 加载即崩）⇒ 就算启动对了，**壳也活不过加载**。
⇒ **四笔一起做完才算「能双击进游戏」**。

---

## 二、判据

### `A1483` 🔴 **「双击 exe 就能进游戏」这件事【账上没有】**

> 现状（**2026-10-10 读数**）：`PlayerBuild.cs` 第一项仍是白板 `Assets/WarpforgeArena1/Scenes/VFXWhiteboard.unity`
> （`:54` 注释自陈「顺序 = 构建顺序，第一个是启动场景」），而 `PlayerBoot.cs:86-88` **无命令行参数时直接 return**
> ⇒ **双击落在白板**。✅ 已做的只是「两个清单都登记了 `MainMenu`+`Shell`」（`A1318`），**启动场景没动**。
> 🔴 **错因留痕**：这件事只被写成 `资料/交接_第九会话.md:71-72` 的一段叙述、**从没立过号**
> ⇒ 2026-10-10 靠一个只读代理才捞出来（本仓「记录丢失」病又一例）。
> **要做**（铁律 11）：把 `Shell`（或 `MainMenu`）挪到清单首位，**或**让 `PlayerBoot` 无参数时默认进主菜单。
>
> ✅ **2026-10-19 已做（`TOOL-11`）—— 选了「挪 `Shell` 到清单首位」这一条**：
> · `Editor/PlayerBuild.cs` 的 `Scenes` 现读 = **`0 Shell / 1 Battle / 2 DeckEditor / 3 MainMenu / 4 VFXWhiteboard`**
> （只交换 `Shell` 与白板；`Battle`/`DeckEditor`/`MainMenu` 的 build index 不变）。
> · 理由（已写进代码注释）：**指针层只有从壳启动才有人建** —— `MainMenu.unity` 里
> `WindowsManager` / `PointerLayer` 都是 0 处、`Shell.unity` 自带（判据 →
> `资料/任务线/界面线/任务/T02_主菜单与外壳.md` §2·1）；壳起来后自己会建主菜单窗
> ⇒ **双击 = 壳 → 主菜单 → 可点**。⛔ 没选 `MainMenu` 打头，就因为它没有指针层。
> · `PlayerBoot.cs` 的「无参数直接 return」**没动、也不需要动**（现读 `:93-95`；本条上面引的 `:86-88` 已漂）。
> ⚠️ **验收仍需重出一次 player 再双击**（`PlayerBuild.Run`）—— 属「需 Unity/真机」，按现行口径攒着。

### `A1481` 🔴 **player 里 `Shell`(level4) 与 `DeckEditor`(level2) 【加载即崩】**

> 日志原文 `The file '…/WarpforgePlayer_Data/level2' is corrupted!`（`level4` 同款）
> → `[Position out of bounds!]` → `Crash!!!`，**退出码 3**；而**同一次构建**里 `MainMenu`(level3) 与
> `Battle`(level1) **rc=0 正常跑完**（20~147 秒 · **0 异常**）。
> 崩溃报告 → `%TEMP%\Unity Technologies\com.unity.template.urp-blank\Crashes`。
> 🔴 **它是「双击进游戏」（开场→主菜单）与「进卡组编辑」的共同拦路虎** —— 因为**指针层只有从壳启动才有**（见 `A1480`）
> ⇒ **修不好它，player 里就永远点不动**。
> 场景顺序（build index）= `PlayerBuild.cs:56-71`：`0 白板 / 1 Battle / 2 DeckEditor / 3 MainMenu / 4 Shell`。
>
> 🔴 **2026-10-19 就地订正（铁律 5 · `TOOL-11`）：本节【已不成立】—— `A1481` 关账。**
> 现核证据（判据全文 → `资料/普查产出_1019/W_工具线_T02打包_1019b.md` §1·1）：
> ① 已知根因（内联 `!u!115` 脚本存根）**当前 6 个场景全 0**
> （`Battle` / `CardBase` / `CollectionCheck` / `DeckEditor` / `MainMenu` / `Shell`）；
> ② 最新崩溃报告 = **2026-10-10 23:18**，此后零崩溃；
> ③ 当前 build（2026-10-18 19:37）的 `Player.log` 里 Shell 与 DeckEditor 均正常加载、**零 `corrupted` / 零 `Crash!!!`**。
> ⚠️ 上面保留 2026-10-10 的原始读数，只为交代来历；⛔ 别再按它找病。
> ⚠️ 另：场景顺序 **2026-10-19 起已改**为 `0 Shell / 1 Battle / 2 DeckEditor / 3 MainMenu / 4 白板`（见 `A1483`）。

🔴🔴 **2026-10-10 重建实测（就地订正，推翻「最便宜的判据 = 重新构建一次」那一句）**：
重构建**已跑完 `Succeeded`**（`PlayerBuild.RunWithSubsetLibrary`）⇒
**① `DeckEditor` 修好了**（`level2` 从 11,424,184 → 11,424,876 字节 · 新包 rc=0 ·
已有运行期实拍 `_tmp_view/player/DeckEditor.png`）；
**② `Shell` 仍然崩**（rc=3 · `corrupted` 命中 2 次），**而 `level4` 重建前后字节数【完全相同】（17,720）**
⇒ **不是构建事故，是可复现的确定性缺陷 —— 要从场景数据本身查**
（「重构建一次就知道」**对 Shell 不成立**、对 DeckEditor 成立）。
⚠️ **新查到一条未解**：本次构建 `Packaging assets - resources.assets` 一步报 **22 条错误**
（构建摘要里写了、**日志不落** ⇒ 与 `A1319` 的 140 条同源，都在 `Library/LastBuild.buildreport` 里）。

### `A1482` ⚠️ **player 里三个场景都刷 49 行 `missing script`**

> `The referenced script on this Behaviour (Game Object '…') is missing!`，**编辑器里没有这一族**
> ⇒ 疑似**托管代码裁剪**（与 `A1319` 的「`m_Script` guid 全零」是**同族但不同因**：
> 那条是场景数据坏、这条是**类型被裁掉**）。⚠️ **未查**（还没比对「哪些类被裁」）。
> 出处 → `Player.log:55-87`（每次运行都在同一位置重刷）。
>
> 🔴 **2026-10-19 现核订正（`TOOL-11`）**：现读 `Player.log:38-43` = **6 行**（不是 49）。
> 后 3 条已定位到 `Shell.unity` 里 3 个仍指向**空壳** `Shell/ShellParts.cs` 的组件；
> 前 3 条 `(Unknown)` 最可能是同一批的第二遍报告（3×2=6，**未硬证**）。**不是托管裁剪** ——
> `Shade` / `BlockingOverlay` / `UISafeArea` / `ShellRuntime` 在 `Assembly-CSharp.dll` 里都在。
> **前置与修法** → `资料/普查产出_1019/W_工具线_T02打包_1019b.md` §四·1。

### `A1484` ⚠️ **「打包成游戏」前必改的两处**

> ① 🔴 **产品名 / 公司名仍是 URP 模板默认值**：判据 = 运行期日志路径
> `…\AppData\LocalLow\Unity Technologies\com.unity.template.urp-blank\Player.log`
> ⇒ **窗口标题 · `persistentDataPath`（存档 `WarpforgeDecks.json` / `WarpforgeDaily\daily.json`）·
> 崩溃报告目录全是模板名**；
> ② `PlayerBuild.cs:12` 自称「『构建后 player 验证』是 `项目任务.md` **顶部待办表的第 1 行**」，
> 而**现读那张表没有这一行**（**账源指空**）；另 `:3`「本工程**从来没有构建过 player**」原句仍在
> （`:6-11` 的订正块只作废了它的**下半句**）。
>
> ✅ **2026-10-19 现状（`TOOL-11` 现核）**：
> · **① 已改**：`ProjectSettings.asset:15-16` = `companyName: Everguild` / `productName: Warpforge`
> （判据 = 原版 `d:/2/unity_run_ref/Warpforge_Data/app.info` 逐字节）。副作用：`persistentDataPath`
> → `LocalLow/Everguild/Warpforge/`（与原版同目录；我们的存档文件名不重名）。
> · **② 两处都已订正**：`PlayerBuild.cs:3` / `:12` 各带「2026-10-19 就地订正（铁律 5 · `A1484②`）」块
> （原句保留、更正痕在位；现行账指回本文件）。
> · **另**：`PlayerBuild.cs` 尾部那 5 条 `-wfscene` 命令原来**只打印、不执行**却写得像跑过 ——
> 现按 `资料/待办判据_第三十三会话.md` 的 `A1481` 行「要么真跑、要么别写成跑过」选了后者：
> 每行显式标 **「未执行 · 手动复制」**、命令改成**全路径**，并写清六个参数
> （`-wfscene` / `-wfshot` / `-wfshotat` / `-wfquit` / `-wfdrive` / `-logFile`）的语义与落点。
> ⚠️ 为什么不真跑：起 player 是**验收动作**（现行口径「先复刻、别检查」），且「批处理构建里起 GUI player」
> **本身未验过** ⇒ 不把未验行为塞进构建脚本。

---

## 三、我们现状

| 处 | 位置 | 现状（**2026-10-19 现读**） |
|---|---|---|
| 启动场景 | `PlayerBuild.Scenes`（第一项 = `Shell`）· `PlayerBoot.cs:93-95`（无参数直接 return） | ✅ **双击落在壳里**（壳自带指针层并建主菜单窗）；**待重出 player 验收** |
| `Shell` 崩 | — | ✅ **`A1481` 已关账**：6 场景 `!u!115` 全 0 · 当前 build 零 `corrupted`（判据见 `A1481` 节的订正块） |
| `DeckEditor` | `level2` 11,424,876 字节（2026-10-10 读数） | ✅ 当时重构建后已修好（rc=0） |
| 托管裁剪 | `Player.log:38-43` | 现读 **6 行** `missing script`（不是 49；后 3 条 = `ShellParts.cs` 空壳，见 `A1482`） |
| 产品名 | `ProjectSettings.asset:15-16` | ✅ 已改 `Everguild / Warpforge`（2026-10-19 现读） |
| 构建告警 | `Library/LastBuild.buildreport`（**日志不落**） | `Packaging assets - resources.assets` 一步 **22 条错误**（2026-10-18 构建；内容仍未拿到） |

⚠️ **判 player 版本别的时间戳**（铁律级的坑）：**`WarpforgePlayer.exe` 的时间戳是壳、不会变**
—— 判版本看 **`WarpforgePlayer_Data/Managed/Assembly-CSharp.dll` 的 mtime**。

---

## 四、怎么改（**建议顺序：`A1483` → `A1481` → `A1482` → `A1484`**）

> 🔴 **2026-10-19 现读状态**：① `A1483` ✅ 已做（`Shell` 打头）· ② `A1481` ✅ 已关账（跳过）· ③ `A1482` **仍开（需 Unity）** · ④ `A1484` ✅ 两处已改。

1. **`A1483`（最便宜、收益最大）**：把 `Shell`（或 `MainMenu`）挪到清单首位，
   **或**让 `PlayerBoot` 无参数时默认进主菜单。
   ⚠️ **两选一** —— 选完把理由写进代码注释（否则下一个人会再问一遍）。
   ✅ **2026-10-19 已做**：选了「`Shell` 打头」，理由已写进 `PlayerBuild.Scenes` 的注释（见 §二 `A1483` 的订正块）。
2. **`A1481`**：**从场景数据本身查 `level4`**（字节数不变 ⇒ 不是构建事故）。
   ⚠️ 已知同族先例：**`MonoBehaviour` 的类名与所在文件名不同 ⇒ Unity 建不出脚本资产
   ⇒ 存盘时烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩**（编辑器里一切正常）。
   查法（**不跑 Unity 就能查**）：`python -I -X utf8 工具/scene_class_filename_census.py d:/4/Unity/MyGame/Assets`
   ＋ `grep -c "^--- !u!115" <场景>`（**正常应为 0**）。
   ⚠️ **误报排除**：**包脚本**（如 TMP 的 `TextMeshPro`）不在 `Assets/` 下、没有 `.cs.meta` ⇒ 照 meta 反查解不出是**正常**的。
   ✅ **2026-10-19 已关账**：上述两条现核全 0、当前 build 零 `corrupted`（见 §二 `A1481` 的订正块）。
3. **`A1482`**：**先比对「哪些类被裁」**（判据还没出）—— ⚠️ **它是「未查」，⛔ 别猜成因**。
   🔴 **2026-10-19 现核**：不是托管裁剪；剩 3 个空壳组件（`ShellParts.cs`），**修法需 Unity 重存 `Shell.unity`**（见 §二 `A1482`）。
4. **`A1484`**：改产品名 / 公司名（**动之前先确认**：这会改 `persistentDataPath` ⇒ **已有存档会搬位置**）＋
   订正 `PlayerBuild.cs:12` 那句「账源指空」。
   ✅ **2026-10-19 两处都已改**（见 §二 `A1484` 的现读块）。

---

## 五、做完了怎么自检

**现在**：⛔ **本件按现行口径「只做不验」** —— 起 player 属于被停掉的验收动作
（`准则与做法.md` §四）。✅ **只跑类型检查**（改了 `.cs` 的话）。
**将来统一验收**：出一次 player（**验收用 `PlayerBuild.Run`** —— 子集库那条会故意只装 12 个效果、
伪造「库里没有」；见战斗线 `T05`）⇒ **双击 exe** ⇒
**该进主菜单、点得动、卡组编辑不崩**、日志里**零 `corrupted` / 零 `Crash!!!`**、`missing script` 那 **6 行**消失。
⚠️ **2026-10-19 现读**：`corrupted` / `Crash` 两项当前 build 已清零，只剩 `missing script` 6 行（= `A1482`）。

---

## 六、还差什么 / 判不准的

1. ✅ **`A1481` 已关账（2026-10-19 现核）** —— 原「根因没查清」不再成立：`!u!115` 现核 6 场景全 0、
   当前 build 零 `corrupted`。
   ⚠️ **历史原文（2026-10-10，保留）**：「**根因【没查清】** —— 只有『`level4` 重建前后字节数完全相同』这一条硬判据；
   `!u!115` 存根那条只是**同族先例**（2026-10-13 那次是 `DeckEditor`，**不是** `Shell`）⇒ ⛔ 别当成已定论」。
2. **`A1482`**：已排除托管裁剪（见 §二）；剩 3 个空壳组件。
   **前置与命令 → `资料/普查产出_1019/W_工具线_T02打包_1019b.md` §四·1（本波未做，需 Unity）**。
3. ⚠️ **`Packaging assets - resources.assets` 那 22 条错误的内容没拿到**（它只进 `Library/LastBuild.buildreport`、
   **日志不落**）⇒ 要查得**从 buildreport 里读**（与 `A1319` 同源）。
4. ✅ **2026-10-19 已查「`Shell` 崩」的最新进展**：`A1481` 关账（判据 → `资料/普查产出_1019/W_工具线_T02打包_1019b.md` §1·1）。
5. ✅ **2026-10-19 已现核行号**：`PlayerBuild.Scenes` 按**符号**搜（行号会漂）；
   `PlayerBoot` 的无参数 return 现读 `:93-95`（原 `:86-88` 已漂）。
