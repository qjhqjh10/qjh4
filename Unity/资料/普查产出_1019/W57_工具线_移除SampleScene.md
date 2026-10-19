# W57 工具线 TOOL-13 —— 从 `EditorBuildSettings` 移除 `SampleScene`

> 判据：`资料/普查产出_1019/W57_工具线_构建场景顺序.md`（TOOL-12 把 Shell 挪到 index 0，
> `SampleScene` 仍留 index 1、`enabled: 1`）。
> 本波口径 = **只做不验**：未跑 Unity / 未起 player / 未跑任何 `*.Run`；只跑了秒级 typecheck。
> ⛔ 未动 git、未改正本（`CLAUDE.md` / `项目任务.md`）、未删文件。

## 一、改前 / 改后（`Unity/MyGame/ProjectSettings/EditorBuildSettings.asset`）

**改前**（5 条，`enabled` 全 1）：

| index | path | guid |
|---|---|---|
| 0 | `Assets/CardPresentation/Scenes/Shell.unity` | `ec4b513efbb03e947907e9e878b17264` |
| 1 | `Assets/Scenes/SampleScene.unity` | `99c9720ab356a0642a771bea13969a05` |
| 2 | `Assets/CardPresentation/Scenes/MainMenu.unity` | `c85102cedcf79f64397d0bd2d144d715` |
| 3 | `Assets/CardPresentation/Scenes/DeckEditor.unity` | `1b392e515eb50364aae26c6398995906` |
| 4 | `Assets/CardPresentation/Scenes/Battle.unity` | `2dbe3ca9e58090b42a75f623907fc608` |

**改后**（4 条，`enabled` 全 1，相对顺序不变）：

| index | path | guid |
|---|---|---|
| **0** | `Assets/CardPresentation/Scenes/Shell.unity` | `ec4b513efbb03e947907e9e878b17264` |
| **1** | `Assets/CardPresentation/Scenes/MainMenu.unity` | `c85102cedcf79f64397d0bd2d144d715` |
| **2** | `Assets/CardPresentation/Scenes/DeckEditor.unity` | `1b392e515eb50364aae26c6398995906` |
| **3** | `Assets/CardPresentation/Scenes/Battle.unity` | `2dbe3ca9e58090b42a75f623907fc608` |

- 只删了 `SampleScene` 那一条（3 行）；**其余 4 条的 `enabled` / `guid` / `path` 一字未动**，
  `SampleScene` 之后的 3 条 index 各 −1（MainMenu 2→1、DeckEditor 3→2、Battle 4→3）。
- `Assets/Scenes/SampleScene.unity` 文件本身**保留**（11,413 B，mtime 2026-09-10 13:10 未变；
  `.meta` 也在）—— 只是不再进 build list。
- 文件行尾 / 编码：**LF、无 BOM**（改后 754 B / 22 行；改前 854 B / 25 行，同样 LF 无 BOM）。
- `git diff --numstat` = **2 / 5**（相对 HEAD：SampleScene 三条删除 = 3 行，
  加上 TOOL-12 未提交的 Shell 换位 2+/2−）⇒ **没有整篇重写**。

## 二、`PlayerBuild.cs` / 构建脚本的 `SampleScene` 引用 —— 结论：**无代码引用**

现核范围：
- `Unity/MyGame/Assets/**` 全量 grep `SampleScene`；
- `Unity/工具/**` grep `SampleScene`（**0 命中**）；
- `Unity/**` 按扩展名（`.cs/.sh/.ps1/.py/.json/.asset/.txt`）grep `SampleScene.unity`。

| 位置 | 内容 | 是否影响构建 |
|---|---|---|
| `Editor/PlayerBuild.cs:7-9` | **仅注释**（历史沿革 + 2026-10-11 订正） | ❌ 无代码引用；`Scenes[]` 数组里**没有** `SampleScene` |
| `Settings/SampleSceneProfile.asset:88` | 一个独立资产的名字（`m_Name: SampleSceneProfile`） | ❌ 与 build list 无关 |
| `WarpforgeVFX/Shaders/*.shader` | HLSL 内置 `SampleSceneDepth` / `SampleSceneColor` | ❌ 同名前缀，无关 |
| `Editor/{BattleScene,DeckScene,MainMenuScene,ShellScene}.cs` 的 `AddToBuildSettings` | 幂等**追加各自场景**，不读/不写 `SampleScene` | ❌ 无 |

⇒ **没有任何构建路径把 `SampleScene` 当兜底 / 默认场景**；`PlayerBuild.Scenes`（验证构建那条路）
本来就是 `Shell / Battle / DeckEditor / MainMenu / VFXWhiteboard`，与本次改动口径一致。

### 顺手订正了一行注释（`PlayerBuild.cs:9-13`，**非逻辑**）

原注释写「现读**5 个场景**（`SampleScene` / …）」—— 本次移除后即过期。按铁律 5 就地补一条
2026-10-20（`TOOL-13`）订正，写明现读 **4 个场景**（Shell 0 / MainMenu 1 / DeckEditor 2 / Battle 3）。
`PlayerBuild.cs` 行数 284 → 287（**+3 行，全注释**），LF 无 BOM 保持不变。
⚠️ 该文件的 `git diff --numstat` = **56 / 12** —— 其中绝大部分是 **TOOL-11/12 已留下、尚未提交的改动**
（`A1483` 启动场景换 Shell、`A1484②` 命令清单改为「未执行 · 手动复制」），**不是本次新增**。

## 三、观察（未改，不在白名单）

`ProjectSettings/ProjectSettings.asset:265` 仍有：

```
templateDefaultScene: Assets/Scenes/SampleScene.unity
```

这是**工程模板元数据**字段（旁边是 `templatePackageId: ""`），**不在 build list 里、也不参与
`BuildPipeline`**。它是否影响「新建场景 / 模板导出」**本次没查证**（不在白名单、只读不了用途）。
⇒ **未改**；留主对话裁。

## 四、typecheck

```
TMPDIR=/tmp/wf_tool13 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（2026-10-20；无并发写手，未见别人半成品文件带出的假错。）

## 五、需下次 Unity 打开 / 构建验证

1. **Build Settings 窗口**：应只剩 **4 条**，Shell 在 index 0、全勾选、`SampleScene` 不在列表里。
2. **真包（Build 窗口 / `BuildPipeline`）**：启动场景应为 Shell（无实测出包，构建约 2 GB，不在本波）。
3. `AddToBuildSettings` 幂等追加**不会**把 `SampleScene` 加回来（它只追加各自的场景）——
   但下一次跑 `BattleScene/DeckScene/MainMenuScene/ShellScene` 的构建宿主后，仍建议扫一眼 build list。

## 六、改动清单

| 文件 | 改动 | numstat（含本波前的未提交改动） |
|---|---|---|
| `Unity/MyGame/ProjectSettings/EditorBuildSettings.asset` | `m_Scenes` 删除 `SampleScene` 一条（3 行），其余不动 | 2 / 5 |
| `Unity/MyGame/Assets/CardPresentation/Editor/PlayerBuild.cs` | 仅注释 +3 行：订正「现读 5 个场景」→ 4 个 | 56 / 12（本波自身 = +3 / −0） |

⛔ 未动 git、未改正本、未删文件、未跑 Unity / player / `*.Run`。
