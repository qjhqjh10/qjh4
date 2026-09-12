# qjh4 — 自研卡牌对战游戏

玩法模仿《战锤 40K Warpforge》。**美术由自己提供，需要的是「让卡牌动起来」的机制** ——
发牌、翻面、上场、攻击位移、命中反馈、特效播放这类交互/过渡表现。

> 用 Warpforge 的解包资源当**参考蓝本**：这类「卡牌动效」从零设计很费劲，原版有一套成熟的机制
> 和调好的手感数值。**但版权归 Everguild / Games Workshop，仓库里不放原版资源**（见下）。

---

## 仓库里有什么

| 目录 | 内容 |
|---|---|
| `项目任务.md` | **总入口**，开工前必读。含背景、红线、进度、待办、已知的坑 |
| `卡牌游戏规则.txt` | 玩法规则整理 |
| `Unity/工具/` | Python 工具：资源同步、shader 抽取、AnimFX 数据导出、比对分析 |
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/` | Unity 编辑器工具：特效导出器、比对渲染、隔离诊断等 |
| `Unity/MyGame/Assets/WarpforgeVFX/Runtime/` `Shaders/` | 特效运行时脚本（shader 映射/加载/binder/**效果库**/**播放器**）+ 5 个自建替代 shader（**这部分是自己写的代码**） |
| `Unity/工具/` | 包含 `gen_effect_index.py`（效果索引）、`analyze_sweep.py`（台账）、`dump_animfx.py` 等 |
| `Unity/资料/` | 交接文档（特效线 / 卡牌基座 / **规则引擎**各一份）、设计文档、台账、比对基线、战斗规格 |
| `Unity/MyGame/Assets/CardPresentation/` | 卡牌基座：布局/卡/拖拽/落位/状态/特效钩子（**自己写的代码**） |
| `Unity/MyGame/Assets/RuleEngine/` | **规则引擎**：棋盘/回合/出牌/攻击/伤害/胜负（`Core/` 是纯 C#，零 Unity 依赖，**自己写的代码**）+ 精简卡表 |
| `Unity/数据/` | 引擎中立的 JSON（卡牌数据、粒子参数、索引） |

## 仓库里**没有**什么（有意排除）

| 排除的东西 | 体积 | 为什么 | 怎么拿回来 |
|---|---|---|---|
| `Unity/素材/` | 4 GB | 原版解包资源 + 自购素材包，版权/授权都不允许再分发 | 原版走 `工具/sync_from_d2.py` 从本地档案库 `d:/2` 同步；自购包在本地 |
| `Unity/MyGame/Assets/WarpforgeVFX/{Prefabs,Materials,Textures,Meshes}` | 1.8 GB | 上一条的直接衍生物 | 跑 `EffectExporter.Run` 重新导出（约 7 分钟） |
| `Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/*.bundle` | 2 MB | 原版 shader 的**编译字节码** | `EffectExporter.CopyShaderBundle()` 会从本地 bundle 复制；补充包用 `工具/extract_missing_shaders.py` 生成 |
| `Unity/MyGame/Assets/Resources/WarpforgeVFX/` | 小 | 效果库资产 —— 958 条全是「导出的 prefab」的引用，而 prefab 不进仓库，引用会是空的 | `工具/gen_effect_index.py` → `EffectLibraryBuilder.Run` |
| `Unity/MyGame/Assets/Plugins/Demigiant/`（DOTween） | 6 MB | 第三方库，不是我们写的（许可证允许商用与分发，但按仓库规矩留在本地） | `工具/install_dotween.py`（幂等） |
| `Unity/MyGame/Assets/CardPresentation/Scenes/` | 15 MB | 两个演示场景：`CardBase.unity`（卡牌基座）+ `Battle.unity`（可玩对战）。占位卡面是运行时生成的贴图，存场景时被烘进去了 | `CardBaseDemo.Run` / **`BattleScene.BuildAndSaveScene`** |
| `Unity/资料/{原版参照图,说明书,规则书,原版实拍}/` | 52 MB | 原版的图/文档/截图 | `工具/sync_from_d2.py --batch docs` |
| `Unity/MyGame/Library/` 等 | 2.7 GB | Unity 自动生成 | 用 Unity 打开工程自动重建 |
| `Unity/安装Unity相关内容/` | 8.8 GB | Unity 安装包 | 从 Unity Hub 装 |

完整规则见 `.gitignore`，以及 `项目任务.md` 的「红线」一节。

---

## 从哪开始读

**开工前先读 `项目任务.md`** —— 它是总入口，第三节「新会话从这里开始（必读十份）」
有**完整且唯一维护**的阅读顺序（含每条线各自的交接文档）。本文件不另维护一份，免得两边走样。

一句话版：三条线各有一份交接文档 ——

| 线 | 交接文档 |
|---|---|
| 特效 | `Unity/资料/特效还原_进度与交接.md` |
| 玩法（规则引擎 + 可玩对战） | `Unity/资料/规则引擎_进度与交接.md` |
| 卡牌基座（表现层） | `Unity/资料/卡牌基座_进度与交接.md` |

资源总入口是 `Unity/资料/资源使用手册.md`；版面数值的权威是
`Unity/资料/对战排版_原版数值与改造方案.md`。

> **🆕 原版离线规则书**（不在仓库里，在本地档案库）：
> `d:/2/Warpforge部队卡片/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md`
> —— **61 个关键词的完整技能表**，每个都写明触发时机。做卡牌效果 / 关键词 / AnimFX 时先读它。

---

## 环境

| 项 | 版本 / 路径 |
|---|---|
| Unity | 6.3.23f1，工程在 `Unity/MyGame` |
| 渲染管线 | URP 17.3.0 |
| Python | **必须用** `D:/2/Warpforge_tools/py312/python.exe`（系统 Python 3.14 缺 UnityPy 纹理模块） |
| 本地档案库 | `d:/2`（44.9 万文件，原版解包资源、反编译产物、原版运行环境）。**改之前先确认** |

### 三个必知的环境坑

- **`ELECTRON_RUN_AS_NODE`** —— 本机环境带这个变量，会让 Unity Hub / 团结引擎 Hub **启动即静默退出**。
  跑 Unity 批处理前要 `unset ELECTRON_RUN_AS_NODE`
- **GitHub 直连不通** —— 见下面一节
- **Ghidra 反编译工具链没装** —— 脚本在 `d:/2/Warpforge_tools/data/decomp_il2cpp_0827/ghidra_scripts/`，
  但 Ghidra、JDK、Il2CppDumper、`script.json` 都没了。要重建见交接文档 P0-e

---

## 推送到 GitHub（**当前推不了，要先把通道配好**）

仓库已经建好在本地：`git init` 已做、`.gitignore` 已配，**现在有 7 个 commit**
（初始提交 1431 文件 / 28 MB 之后又提交了 6 次），remote 指向
`https://github.com/qjhqjh10/qjh4.git`（**仍然是 https，还没推上去**）。**只差一个能通的网络。**

2026-09-11 实测：

| 通道 | 结果 |
|---|---|
| `https://github.com` (443) | ❌ **连不上**（21 秒超时）→ `git push` 走这条路会失败 |
| `https://api.github.com` | ✅ 通（0.26s） |
| `https://codeload.github.com` | ✅ 通 |
| `ssh.github.com:443` | ✅ **可连** |
| 本机 SSH 密钥 | ❌ 没有（`~/.ssh` 只有 known_hosts） |
| `gh` CLI | ❌ 没装 |

**两条可行路线，任选一条：**

```bash
# 路线 A：SSH over 443（推荐 —— 这条路实测能连）
#   1) 生成密钥并把公钥加到 GitHub → Settings → SSH and GPG keys
ssh-keygen -t ed25519 -C "qjhqjh10@users.noreply.github.com"
cat ~/.ssh/id_ed25519.pub            # 把输出粘进 GitHub
#   2) 让 github.com 走 443 端口的 SSH（本机 22 端口未必通）
cat >> ~/.ssh/config <<'EOF'
Host github.com
  HostName ssh.github.com
  Port 443
  User git
EOF
#   3) 换 remote 并推送
cd d:/4
git remote set-url origin git@github.com:qjhqjh10/qjh4.git
git push -u origin master

# 路线 B：配好代理后走 HTTPS
git config --global http.proxy http://127.0.0.1:端口
git push -u origin master
```

> ⚠️ **推送前先确认仓库是私有的。** 本仓库虽然排除了原版资源和第三方素材，
> 但 `Unity/资料/` 和 `Unity/数据/` 里仍有原版游戏的**衍生产物**
> （战斗规格分析、卡牌数据表、粒子参数 JSON 等）。公开之前请自己过一遍。

---

## 当前状态（2026-09-12 第八轮）

**🆕 游戏能玩了**

- ✅ **打开 `Unity/MyGame/Assets/CardPresentation/Scenes/Battle.unity` 按 Play 就能打一局** ——
  拖牌上场（付不起拖不上去）→ 点自己的单位选中 → 点敌方目标攻击 → END TURN → 贪心 AI 回应
- ✅ **自己设计的两套卡**（`RuleEngine/Data/StarterCards.cs`）：Ember Legion 猛攻 / Tide Swarm 消耗，各 13 张
- ✅ **卡面是程序生成的黑卡白字**（费用/卡名/关键词/近战·远程·生命），不依赖任何美术资源；
  中文卡面也已打通（TMP + Dynamic CJK）
- ✅ **对战自检 126 通过 / 0 失败**（12 张截图）+ **规则引擎自检 255/255 全过**
- ✅ **第七轮「对战排版」已做完** —— 手牌布局器参数、棋盘坐标（含**敌方镜像**）、卡面数值位置
  全部换成原版实测值，落在 `HandLayout` / `BoardLayout` / `CardView` / `BattleScene`，见
  [`Unity/资料/对战排版_原版数值与改造方案.md`](Unity/资料/对战排版_原版数值与改造方案.md)

**特效线**

- ✅ **特效全量导出 958 个，0 失败**；多时刻比对跑通，**479 个对得上**（280 个高置信）
- ✅ **特效「能用」了** —— `WarpforgeEffectPlayer`（播放/生命周期）+ `WarpforgeEffectLibrary`（958 条）
  + 白板自检（**闭环 20/20、渲染与台账一致 20/20、5 轮无泄漏**）
- ✅ 修掉 C 组主因（精灵没导出 → 遮罩粒子整块渲成空），并**修掉了尺子本身的不确定性**
- ✅ **P1-a0 已重查并修完（2026-09-12）** —— 旧判据「原版 4612 亮点 / 导出 0」是**尺子的假象**，
  真 bug 是混合被内置 Standard 的残留值污染；已修 + 26 条断言
- ⬜ **🆕 P1-a1**：用**原版规则书的 61 个关键词触发表**去解 W 组 189 个「两边全程空」
- ⬜ AnimFX 脚本层（已定论可还原，2346 个组件的参数已导出）

**玩法线**

- ✅ **卡牌基座第 1–7 步全通** —— 布局、卡、悬停让位、拖拽回弹、落位动画、状态描边、落位时播特效
- ✅ **规则引擎** —— 纯 C# 核心（零 Unity 依赖，随机走 `System.Random(seed)` → **同一局永远可复现**）
- ✅ **棋盘改成 9 格**（督军居中，规则书 / `rule_core.gd` / 原版 `MinionArea` 三处一致），
  四档分辨率实测放得下且不重叠；顺带修掉「拖到战场上方空白处也算合法落点」的真 bug
- ⬜ 战术卡效果（478 张卡）/ 关键词补全（已实现 **13** 个，全卡池还有 **48** 个没结算）/ 对手强一点
- ⏭ **换成自己的美术 —— 全项目的最后一步**（用户 2026-09-12 定：所有任务做完之后才做）。
  现在画面用的还是原版美术当复刻参照，版权原因发布前必须整个移出构建

**三条线都欠**：构建后 player 验证（特效 / 玩法 / 卡牌基座，到现在只验过编辑器）

**细节见**：[`Unity/资料/对战排版_原版数值与改造方案.md`](Unity/资料/对战排版_原版数值与改造方案.md)、
[`Unity/资料/规则引擎_进度与交接.md`](Unity/资料/规则引擎_进度与交接.md)、
[`Unity/资料/特效还原_进度与交接.md`](Unity/资料/特效还原_进度与交接.md)、
[`Unity/资料/卡牌基座_进度与交接.md`](Unity/资料/卡牌基座_进度与交接.md)
