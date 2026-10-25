# 执行代理 `S` · 自检断言线 `T07` —— `Badges` 告警集 / 观测集拆分（2026-10-25）

## 结论

**做完了。** `Core/Badges.cs` 里把「一集两用」拆成**两个集合**，`Editor/BadgeFilterAssert.cs` 补了 **6 条**断言
（任务要求的 ①②③，其中 ③ 灭自证拆成 3 条）。秒级类型检查**全绿**（运行时 0 / 编辑器 0）。
坐标：`Badges.cs:431-504`、`BadgeFilterAssert.cs:178-243` + 小工具 `:309-324`。

## 判据与改动前的事实（复核过，与简报一致）

| 事实 | 位置（改前） |
|---|---|
| 唯一集合 `_notInDataWarned`，同时当去重集 + 自检读口数据源 | `Core/Badges.cs:431` |
| 读口 | `:445` |
| `ResetUnknownForTest()` 直接 `Clear()` | `:452` |
| `PlaysInPlayIcon` 里 `Add` | `:462` |
| doc 自陈「清空之后同样的图名会再告警一次（有意）」 | `:450-451` |
| 自检每轮先 Reset ⇒ 17 行重复日志被当「已知」解释掉 | `Editor/BadgeFilterAssert.cs:88-94` / `:155-166` |

## 改动清单（只碰白名单两个文件）

### `Assets/CardPresentation/Core/Badges.cs`

| 改动 | 位置（改后） |
|---|---|
| 新增注释块，写清**更正痕迹**（"⚠️ 2026-10-25 更正 … 原来只有一个集合，Reset 会重打日志"） | `:431-438` |
| `_notInDataWarned` —— **去重告警集**（一辈子每个图名只放行一次，`Reset` **不清**） | `:439-441` |
| `_notInDataSeen` —— **本轮观测集**（每遇到就记，`Reset` **只清这个**） | `:442-444` |
| `_notInDataWarnCount` + 读口 `UnknownIconWarnCountForTest` —— 真打出过的告警**行数**（只增不减，断言取**差值**） | `:445-453` |
| `UnknownIconSpritesForTest` 读口改读 `_notInDataSeen`（语义 = 「这一趟」） | `:455-472` |
| `ResetUnknownForTest()` 只 `_notInDataSeen.Clear()`；doc 就地订正（说明旧口径「会再告警一次」已不成立） | `:474-482` |
| `PlaysInPlayIcon`：**两句分开** —— `_notInDataSeen.Add(sprite)` 每遇到就记；`_notInDataWarned.Add(sprite)` 成功才 `++count` + `LogWarning` | `:484-504` |

⚠️ 行为上**只改了一处**：告警的「只报一次」不再被 `Reset` 撤销。白名单判定、返回值、日志文案**逐字节未动**。

### `Assets/CardPresentation/Editor/BadgeFilterAssert.cs`

| 改动 | 位置（改后） |
|---|---|
| 类 doc 补一句 T07 说明 | `:26-30` |
| 第 4 段那句「清空之后会再告警一次（有意）」**就地订正**（铁律 5，原注释会把下个会话误导） | `:88-95` |
| 原 `explained.Count > 0` 的 🧨 说明改成 `_notInDataSeen.Add(sprite)`（观测已换集合） | `:169-171` |
| **新增 §4·b 断言块（6 条）** | `:178-243` |
| 新增小工具 `SameSet` / `JoinSet` | `:276-289` |

## 断言清单（§4·b，全部写进 `Editor/BadgeFilterAssert.cs`）

| # | 断什么 | 怎么算红（灭自证 / 改回旧写法） |
|---|---|---|
| 0 | `ResetUnknownForTest()` 之后**观测集 = 0**（附上一段它是几条） | Reset 什么都不清 / 错清去重集 ⇒ 留着上一段的数 ⇒ 红 |
| ② | Reset 后**只喂** `HasIconFalse` 那 5 条 ⇒ 观测集**恰好** = 这 5 条的 `SpriteOf` 图名（`SameSet`，长度+逐元素） | 读口「拿上一轮留下的冒充」/ Reset 不清 ⇒ 多出另 12 条 ⇒ 红 |
| ① | Reset 后重跑**同一批**（5 + 12 条负例 + 47 条正例）⇒ 告警**增量 0**（`UnknownIconWarnCountForTest` 差值） | 退回「一集两用」（Reset 连去重集一起清）⇒ 那 17 条各再报一次 ⇒ 红 |
| ③a | **没见过**的图名 `wf_t07_probe_never_in_data` ⇒ 仍告警**一次**（返回 false，增量 1） | 删掉 `LogWarning`（静默）⇒ 增量 0 ⇒ 红 |
| ③b | 同一个名字**第二次** ⇒ 增量 0 | 去掉 `_notInDataWarned` 那层去重（每次都报）⇒ 增量 1 ⇒ 红 |
| ③c | 报过的那条**同时**在观测集里 | 两句合成一句（只在 `Add` 成功时记）⇒ 红（这条钉的就是「一集两用」本身） |

**期望的实机读数**：跑一遍 `BattleScene.Run` 时，`[Badges] 图 … 不在「场上图标」那 47 条里` 这 17 行日志
**应只出现 17 行（第一次），不再随 Reset 重打**；`BT W10` 的断言合计在原来基础上 **+6**。

## 验证

- ✅ `TMPDIR=/tmp/wf_s1 bash /d/4/Unity/工具/typecheck.sh` → **运行时错误数 0 · 编辑器错误数 0**。
- ✅ `git diff --numstat`：`Badges.cs 47/10` · `BadgeFilterAssert.cs 95/5` —— 局部改动，无整篇重写。
- ✅ 行尾：两个文件改前改后都是**纯 LF**（`b.count(b'\r\n') == 0`），没被翻。
- ⛔ 按红线**没跑 Unity**，断言正确性待主对话在同步点验。

## 需要主对话跑的自检

**`BattleScene.Run`**（`BadgeFilterAssert.Run()` 由 `Editor/BattleVfxAssertE3.cs:86` 调用，
而 `BattleVfxAssertE3` 由 `BattleScene.Run` 调用 —— 没有更轻的单独宿主）。
⚠️ 只读这两个文件、没碰场景生成器，所以**不需要** `BuildAndSaveScene`；按铁律 12 先留场景副本再跑。

## 顺手发现（不计入本单）

1. **`PlaysInPlayIcon` 的去重键用**原始 `sprite`**、白名单判定用 `Key(sprite)`**（`Badges.cs:494/496`）——
   同一个图名的两种写法（`huntMark` / `huntmark`）会**各告警一次**。现状无害（`SpriteOf` 返回的是表里的
   规范名），但两边口径不一致；要收口则「去重集也按 `Key`」，属另一笔账，本单**没动**。
2. `资料/任务线/自检断言线/任务总表.md:53` 的 `T07` 行仍写 `✗ 未做` —— **正本由主对话改**（红线③，我没碰）。
3. `Badges.cs:464` 那句「正常卡池里它应当恰好是那 17 条」仍成立（指**全池扫那一趟**的观测集），没改。
