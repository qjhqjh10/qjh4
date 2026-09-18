# qjh4 — 自研卡牌对战游戏

玩法模仿《战锤 40K Warpforge》—— **目标是彻底复刻它**（用户 2026-09-17 拍板：
开场动画 → 主界面 → 战斗 / 卡组 / 商店 / 锻造厂 / 日常）。**美术一直留原版、不再换成自己的**。

> 用 Warpforge 的解包资源当**参考蓝本**：这类「卡牌动效」从零设计很费劲，原版有一套成熟的机制
> 和调好的手感数值。
> ✅ **2026-09-18 用户取消版权红线**：本项目是**个人学习用途**，原版美术/语音/文本照用。
> ⚠️ 但**仓库里仍然不放那些大件** —— 理由是「别把 4 GB 塞进 git」，不是版权（见下）。
>
> 🔴 **本文件是「仓库导览」，不是进度文档。** 进度与待办**只看 `项目任务.md`**（`README` 里写死的数字
> 迟早过期 —— 历史上就发生过，见文件末尾那条更正）。

---

## 仓库里有什么

| 目录 | 内容 |
|---|---|
| `项目任务.md` | **总入口**，开工前必读。含背景、**待办正本**（顶部那张表）、已知的坑、常用操作速查、文档总入口 |
| `Unity/工具/` | Python 工具：资源同步、shader 抽取、AnimFX 数据导出、比对分析 |
| `Unity/MyGame/Assets/WarpforgeArena1/Editor/` | Unity 编辑器工具：特效导出器、比对渲染、隔离诊断等 |
| `Unity/MyGame/Assets/WarpforgeVFX/Runtime/` `Shaders/` | 特效运行时脚本（shader 映射/加载/binder/**效果库**/**播放器**）+ 5 个自建替代 shader（**这部分是自己写的代码**） |
| `Unity/工具/` | 包含 `gen_effect_index.py`（效果索引）、`analyze_sweep.py`（台账）、`dump_animfx.py` 等 |
| `Unity/资料/` | 各线的交接文档（**五条线**，见下面「从哪开始读」）、设计文档、台账、比对基线、战斗规格 |
| `Unity/MyGame/Assets/CardPresentation/` | 卡牌基座：布局/卡/拖拽/落位/状态/特效钩子（**自己写的代码**） |
| `Unity/MyGame/Assets/RuleEngine/` | **规则引擎**：棋盘/回合/出牌/攻击/伤害/胜负（`Core/` 是纯 C#，零 Unity 依赖，**自己写的代码**）+ 精简卡表 |
| `Unity/数据/` | 引擎中立的 JSON（卡牌数据、粒子参数、索引） |

## 仓库里**没有**什么（有意排除）

| 排除的东西 | 体积 | 为什么 | 怎么拿回来 |
|---|---|---|---|
| `Unity/素材/` | 4 GB | **体积**（原版解包资源 + 自购素材包；⚠️ 其中**自购素材包**的授权确实不允许再分发） | 原版走 `工具/sync_from_d2.py` 从本地档案库 `d:/2` 同步；自购包在本地 |
| `Unity/MyGame/Assets/WarpforgeVFX/{Prefabs,Materials,Textures,Meshes}` | 1.8 GB | 上一条的直接衍生物 | 跑 `EffectExporter.Run` 重新导出（约 7 分钟） |
| `Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/*.bundle` | 2 MB | 原版 shader 的**编译字节码** | `EffectExporter.CopyShaderBundle()` 会从本地 bundle 复制；补充包用 `工具/extract_missing_shaders.py` 生成 |
| `Unity/MyGame/Assets/Resources/WarpforgeVFX/` | 小 | 效果库资产 —— 958 条全是「导出的 prefab」的引用，而 prefab 不进仓库，引用会是空的 | `工具/gen_effect_index.py` → `EffectLibraryBuilder.Run` |
| `Unity/MyGame/Assets/Plugins/Demigiant/`（DOTween） | 6 MB | 第三方库，不是我们写的（许可证允许商用与分发，但按仓库规矩留在本地） | `工具/install_dotween.py`（幂等） |
| `Unity/MyGame/Assets/CardPresentation/Scenes/` | 15 MB | **三个**演示场景：`CardBase.unity`（卡牌基座）+ `Battle.unity`（可玩对战）+ `DeckEditor.unity`（卡组编辑）。占位卡面是运行时生成的贴图，存场景时被烘进去了 | `CardBaseDemo.Run` / **`BattleScene.BuildAndSaveScene`** / **`DeckScene.BuildAndSaveScene`** |
| `Unity/资料/{原版参照图,说明书,规则书,原版实拍}/` | 52 MB | 原版的图/文档/截图 | `工具/sync_from_d2.py --batch docs` |
| `Unity/MyGame/Library/` 等 | 2.7 GB | Unity 自动生成 | 用 Unity 打开工程自动重建 |
| `Unity/安装Unity相关内容/` | 8.8 GB | Unity 安装包 | 从 Unity Hub 装 |

完整规则见 `.gitignore`，以及 `项目任务.md` §二「项目准则」第 3 条（**理由已从「版权」改成「仓库体积」**）。

---

## 从哪开始读

**开工前先读 `项目任务.md`** —— 它是总入口，第三节「**文档总入口**」
有**完整且唯一维护**的阅读顺序（必读表 **17 条**，含每条线各自的交接文档）。本文件不另维护一份，免得两边走样。

一句话版：**各线各有一份交接文档** ——

| 线 | 交接文档 |
|---|---|
| **卡牌线（主战场）** | `Unity/资料/阵营推进_清单与交接.md` |
| 特效 | `Unity/资料/特效还原_进度与交接.md` |
| 玩法（规则引擎 + 可玩对战） | `Unity/资料/规则引擎_进度与交接.md` |
| 卡牌基座（表现层） | `Unity/资料/卡牌基座_进度与交接.md` |
| **反编译（要问「原版怎么做 X」先看它）** | `Unity/资料/全量反编译_入口与用法.md` |

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
- ✅ **Ghidra 反编译工具链 2026-09-17 已重建**（JDK 21 + Ghidra 12.1.3 + Il2CppDumper，都在 `d:/2/tools/`；
  分析好的工程 `ghidra_proj` 2.6 GB，**别删**）—— 见 `Unity/资料/反编译工具链_重建记录.md`。
  🔴 **2026-09-18：整个游戏的代码已经全部反编译完毕**（`D:/2/tools/decomp_full/`，
  `DECOMP DONE ok=26275 fail=7`，含 **401 个协程体**）。
  ~~Ghidra 反编译工具链没装 —— Ghidra、JDK、Il2CppDumper、`script.json` 都没了~~ ← 2026-09-17 前的状态

---

## 推送到 GitHub（**当前推不了，要先把通道配好**）

仓库已经建好在本地：`git init` 已做、`.gitignore` 已配，remote 指向
`https://github.com/qjhqjh10/qjh4.git`（**仍然是 https，还没推上去**）。**只差一个能通的网络。**
（⚠️ 原写「**现在有 7 个 commit**」—— 那是 2026-09-11 的数；**commit 数别写死，看 `git log`**。）

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

## 当前状态

> 🔴 **这里原来是一份「2026-09-12 第八轮」的状态快照，2026-09-18 删掉了。**
> **删的原因**：项目自己的规矩是「**数字与清单只留一处**」——而那一节里有 **7 条已经过期**
> （卡面描述、自检数字、关键词数、W 组条数、AnimFX 状态、换美术计划、player 验证），
> 每一条都在跟正本打架。**留一份会过期的状态快照，比没有更糟。**
>
> ✅ **要看状态，去这两处**（**唯一出处**）：
> - **待办 / 下一步** → `项目任务.md` **顶部那张「⏭ 下次接着做」表**（全项目唯一待办正本，18 行）
> - **自检数字 / 卡池张数 / 未实现关键词数** → `Unity/资料/阵营推进_清单与交接.md` **§一**
> - 五条线各自的细节 → 上面「从哪开始读」那张表里的交接文档

**仍然成立、不会过期的三条（说清方向，不是数字）**：

- **打开 `Unity/MyGame/Assets/CardPresentation/Scenes/Battle.unity` 按 Play 就能打一局** —— 打的是**原版卡**、
  **卡面按原版规格组装**、**效果文字按原版语义结算**，对手是照原版重写的**打分型 AI**（难度可调）
- **规则引擎是纯 C# 核心**（零 Unity 依赖，随机走 `System.Random(seed)` → **同一局永远可复现**）
- **美术留原版、不再换**（用户 2026-09-17 拍板；版权红线 2026-09-18 取消）

**细节见**：[`Unity/资料/对战排版_原版数值与改造方案.md`](Unity/资料/对战排版_原版数值与改造方案.md)、
[`Unity/资料/规则引擎_进度与交接.md`](Unity/资料/规则引擎_进度与交接.md)、
[`Unity/资料/特效还原_进度与交接.md`](Unity/资料/特效还原_进度与交接.md)、
[`Unity/资料/卡牌基座_进度与交接.md`](Unity/资料/卡牌基座_进度与交接.md)
