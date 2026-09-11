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
| `Unity/MyGame/Assets/WarpforgeVFX/Runtime/` `Shaders/` | 特效运行时脚本 + 5 个自建替代 shader（**这部分是自己写的代码**） |
| `Unity/资料/` | 交接文档、设计文档、台账、比对基线、战斗规格 |
| `Unity/数据/` | 引擎中立的 JSON（卡牌数据、粒子参数、索引） |

## 仓库里**没有**什么（有意排除）

| 排除的东西 | 体积 | 为什么 | 怎么拿回来 |
|---|---|---|---|
| `Unity/素材/` | 4 GB | 原版解包资源 + 自购素材包，版权/授权都不允许再分发 | 原版走 `工具/sync_from_d2.py` 从本地档案库 `d:/2` 同步；自购包在本地 |
| `Unity/MyGame/Assets/WarpforgeVFX/{Prefabs,Materials,Textures,Meshes}` | 1.8 GB | 上一条的直接衍生物 | 跑 `EffectExporter.Run` 重新导出（约 7 分钟） |
| `Unity/MyGame/Assets/StreamingAssets/WarpforgeVFX/*.bundle` | 2 MB | 原版 shader 的**编译字节码** | `EffectExporter.CopyShaderBundle()` 会从本地 bundle 复制；补充包用 `工具/extract_missing_shaders.py` 生成 |
| `Unity/资料/{原版参照图,说明书,规则书,原版实拍}/` | 52 MB | 原版的图/文档/截图 | `工具/sync_from_d2.py --batch docs` |
| `Unity/MyGame/Library/` 等 | 2.7 GB | Unity 自动生成 | 用 Unity 打开工程自动重建 |
| `Unity/安装Unity相关内容/` | 8.8 GB | Unity 安装包 | 从 Unity Hub 装 |

完整规则见 `.gitignore`，以及 `项目任务.md` 的「红线」一节。

---

## 从哪开始读

1. **`Unity/资料/资源使用手册.md`** —— 资源总入口：目录结构、每个目录怎么用、已知的坑
2. **`Unity/资料/自研游戏_特效与卡牌基座_设计.md`** —— 接下来怎么走：特效怎么接进游戏、卡牌基座怎么设计
3. **`Unity/资料/特效还原_进度与交接.md`** —— 特效还原线：已验证的事实、产物、已知 bug、下一步
4. **`Unity/资料/特效还原台账.tsv`** —— 957 行逐效果状态（判定 / 亮度比 / 置信度 / 技术构成）
5. `Unity/_资源评估_场景特效动画.md` —— 场景/特效/动画三类的可用性判定与逐项工时

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

仓库已经建好在本地：`git init` 已做、`.gitignore` 已配、初始提交已完成（1431 文件 / 28 MB），
remote 也指到了 `https://github.com/qjhqjh10/qjh4.git`。**只差一个能通的网络。**

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

## 当前状态（2026-09-11）

- ✅ Unity 环境、资源整理（1.38 GB）、解包资源审计、战场重建
- ✅ **特效全量导出 958 个，0 失败**；多时刻比对跑通，**470 个对得上**（289 个高置信）
- ✅ 特效运行时材质重建管线可用（`WarpforgeEffectBinder`）
- ✅ AnimFX 脚本层**已定论可还原**，2346 个组件的参数值已导出
- ⬜ **特效还没有「播放入口 + 生命周期」** —— 这是接进游戏的前提，2–3 天
- ⬜ 181 个高置信问题待啃（170 亮度/密度 + 11 导出整个丢了）
- ⬜ 卡牌表现基座（发牌/手牌/拖拽/落位/高亮）未开始
- ⬜ 游戏本身一行代码没写；规则引擎待移植
