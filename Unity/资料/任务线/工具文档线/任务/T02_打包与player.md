# T02 · 打包与 player —— **「双击 exe 就能进游戏」这件事到现在还没成**

> **状态**：🔴 **未完成**（判据齐）
> **关联 A 编号**：`A1481`（🔴 **加载即崩**）· `A1482`（⚠️ 49 行 missing script）· `A1483`（🔴 启动场景是白板）· `A1484`（⚠️ 打包前必改两处）
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

> 现状：`PlayerBuild.cs:58` 第一项仍是白板 `Assets/WarpforgeArena1/Scenes/VFXWhiteboard.unity`
> （`:54` 注释自陈「顺序 = 构建顺序，第一个是启动场景」），而 `PlayerBoot.cs:86-88` **无命令行参数时直接 return**
> ⇒ **双击落在白板**。✅ 已做的只是「两个清单都登记了 `MainMenu`+`Shell`」（`A1318`），**启动场景没动**。
> 🔴 **错因留痕**：这件事只被写成 `资料/交接_第九会话.md:71-72` 的一段叙述、**从没立过号**
> ⇒ 2026-10-10 靠一个只读代理才捞出来（本仓「记录丢失」病又一例）。
> **要做**（铁律 11）：把 `Shell`（或 `MainMenu`）挪到清单首位，**或**让 `PlayerBoot` 无参数时默认进主菜单。

### `A1481` 🔴 **player 里 `Shell`(level4) 与 `DeckEditor`(level2) 【加载即崩】**

> 日志原文 `The file '…/WarpforgePlayer_Data/level2' is corrupted!`（`level4` 同款）
> → `[Position out of bounds!]` → `Crash!!!`，**退出码 3**；而**同一次构建**里 `MainMenu`(level3) 与
> `Battle`(level1) **rc=0 正常跑完**（20~147 秒 · **0 异常**）。
> 崩溃报告 → `%TEMP%\Unity Technologies\com.unity.template.urp-blank\Crashes`。
> 🔴 **它是「双击进游戏」（开场→主菜单）与「进卡组编辑」的共同拦路虎** —— 因为**指针层只有从壳启动才有**（见 `A1480`）
> ⇒ **修不好它，player 里就永远点不动**。
> 场景顺序（build index）= `PlayerBuild.cs:56-71`：`0 白板 / 1 Battle / 2 DeckEditor / 3 MainMenu / 4 Shell`。

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

### `A1484` ⚠️ **「打包成游戏」前必改的两处**

> ① 🔴 **产品名 / 公司名仍是 URP 模板默认值**：判据 = 运行期日志路径
> `…\AppData\LocalLow\Unity Technologies\com.unity.template.urp-blank\Player.log`
> ⇒ **窗口标题 · `persistentDataPath`（存档 `WarpforgeDecks.json` / `WarpforgeDaily\daily.json`）·
> 崩溃报告目录全是模板名**；
> ② `PlayerBuild.cs:12` 自称「『构建后 player 验证』是 `项目任务.md` **顶部待办表的第 1 行**」，
> 而**现读那张表没有这一行**（**账源指空**）；另 `:3`「本工程**从来没有构建过 player**」原句仍在
> （`:6-11` 的订正块只作废了它的**下半句**）。

---

## 三、我们现状

| 处 | 位置 | 现状 |
|---|---|---|
| 启动场景 | `PlayerBuild.cs:56-71`（清单 · 第一项 = 白板）· `PlayerBoot.cs:86-88`（无参数直接 return） | **双击落在白板** |
| `Shell` 崩 | `level4` = 17,720 字节（重建前后**完全相同**） | rc=3 · `corrupted` · **可复现的确定性缺陷** |
| `DeckEditor` | `level2` 11,424,876 字节 | ✅ 重构建后已修好（rc=0） |
| 托管裁剪 | `Player.log:55-87` | 三个场景各刷 **49 行** `missing script`（编辑器里没有） |
| 产品名 | 运行期路径里的 `Unity Technologies\com.unity.template.urp-blank` | 仍是 URP 模板默认值 |
| 构建告警 | `Library/LastBuild.buildreport`（**日志不落**） | `Packaging assets - resources.assets` 一步 **22 条错误** |

⚠️ **判 player 版本别的时间戳**（铁律级的坑）：**`WarpforgePlayer.exe` 的时间戳是壳、不会变**
—— 判版本看 **`WarpforgePlayer_Data/Managed/Assembly-CSharp.dll` 的 mtime**。

---

## 四、怎么改（**建议顺序：`A1483` → `A1481` → `A1482` → `A1484`**）

1. **`A1483`（最便宜、收益最大）**：把 `Shell`（或 `MainMenu`）挪到 `PlayerBuild.cs:58` 清单首位，
   **或**让 `PlayerBoot.cs:86-88` 无参数时默认进主菜单。
   ⚠️ **两选一** —— 选完把理由写进代码注释（否则下一个人会再问一遍）。
2. **`A1481`**：**从场景数据本身查 `level4`**（字节数不变 ⇒ 不是构建事故）。
   ⚠️ 已知同族先例：**`MonoBehaviour` 的类名与所在文件名不同 ⇒ Unity 建不出脚本资产
   ⇒ 存盘时烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩**（编辑器里一切正常）。
   查法（**不跑 Unity 就能查**）：`python -I -X utf8 工具/scene_class_filename_census.py d:/4/Unity/MyGame/Assets`
   ＋ `grep -c "^--- !u!115" <场景>`（**正常应为 0**）。
   ⚠️ **误报排除**：**包脚本**（如 TMP 的 `TextMeshPro`）不在 `Assets/` 下、没有 `.cs.meta` ⇒ 照 meta 反查解不出是**正常**的。
3. **`A1482`**：**先比对「哪些类被裁」**（判据还没出）—— ⚠️ **它是「未查」，⛔ 别猜成因**。
4. **`A1484`**：改产品名 / 公司名（**动之前先确认**：这会改 `persistentDataPath` ⇒ **已有存档会搬位置**）＋
   订正 `PlayerBuild.cs:12` 那句「账源指空」。

---

## 五、做完了怎么自检

**现在**：⛔ **本件按现行口径「只做不验」** —— 起 player 属于被停掉的验收动作
（`准则与做法.md` §四）。✅ **只跑类型检查**（改了 `.cs` 的话）。
**将来统一验收**：出一次 player（`PlayerBuild.RunWithSubsetLibrary`）⇒ **双击 exe** ⇒
**该进主菜单、点得动、卡组编辑不崩**、日志里**零 `corrupted` / 零 `Crash!!!`**、`missing script` 那 49 行消失。

---

## 六、还差什么 / 判不准的

1. **`A1481` 的根因【没查清】** —— 只有「`level4` 重建前后字节数完全相同」这一条硬判据；
   `!u!115` 存根那条只是**同族先例**（2026-10-13 那次是 `DeckEditor`，**不是** `Shell`）⇒ ⛔ **别当成已定论**。
2. **`A1482` 的「哪些类被裁」没比对过** —— 判据件自陈「⚠️ 未查」。
3. ⚠️ **`Packaging assets - resources.assets` 那 22 条错误的内容没拿到**（它只进 `Library/LastBuild.buildreport`、
   **日志不落**）⇒ 要查得**从 buildreport 里读**（与 `A1319` 同源）。
4. ⚠️ **本轮没查「`Shell` 崩」这件事有没有别处的最新进展** ——
   搜过的词：`level4` / `corrupted` / `A1481`（在 `资料/` 下 `grep`）⇒ **有更新的结论请先更新本件**。
5. **本件的行号（`PlayerBuild.cs:56-71` / `PlayerBoot.cs:86-88`）取自判据件、本轮【没有现核】**
   ⇒ **动手前 `grep` 现核**。
