# CLAUDE.md — 现行重心：离线版（`E:\warpforge\`）

> 🔴 **2026-11-08 用户定：`D:\4` 的「复刻工程」长期暂停**（等用户通知，⛔ 不主动提醒）；**任务重心 = 离线版 `E:\warpforge\`**。
> 🔴 **新会话第一站** = 读 `E:\warpforge\资料\离线原版\活状态表.md` 的
> 「★ 当前未完成（ACTIVE）」和「★ 长期任务（永久保留 · 不核销）」两节；⛔ 不整份读任务总表。
> **本文件的旧版全文**（D:\4 复刻工程的全部铁律 / 判据 / 历史 / 资源地图）已归档 →
> `D:\4\Unity\资料\历史\CLAUDE_md_复刻工程全文_20261109.md`（**需要查旧知识再去翻，⛔ 不让它主导离线版工作**）。

---

## 一、真相源（先看这里，别凭记忆）

| 要查什么 | 去哪 |
|---|---|
| 现在要做什么 / 什么算做完 | `E:\warpforge\资料\离线原版\活状态表.md`（ACTIVE 区） |
| 打包 exe / 版本更新 / 邮件发送 的流程 | `E:\warpforge\资料\离线原版\长期任务工作流程\`（`打包exe.md` · `版本更新.md` · `邮件发送.md`） |
| 踩过的坑 | `E:\warpforge\资料\离线原版\已知的坑.md` |
| 假 PlayFab / 服务端 | `E:\warpforge\05_工具\server\README.md` |
| 邮件源与格式 | `E:\warpforge\邮件箱\README.md` |
| 本轮报告 / 判据 | `E:\warpforge\资料\离线原版\报告\` · `判据\` |
| 原版资产 / 反编译 | `E:\warpforge\02_解包资源\` · `03_反编译\`（只读；解包到 `_tmp_view/` 的空目录） |

---

## 二、工作规则（离线版长期适用）

1. **判据优先原版资产/反编译**；查不到就写「查不到 + 查过哪些目录/关键词」，⛔ 不猜。
2. **改完必须实机验证**：起客户端 → 截图 → 点击；⛔ 没跑过不说「通过」，⛔ 不用「应该」代替证据。
3. **发现问题当场落盘**：报告 + `活状态表` + `已知的坑`；跨会话只认文件，不认对话记忆。
4. **一个文件同一时刻只有一个写手**；并行派子代理，简报自包含（目标/判据/白名单/已知/输出格式/红线）；
   子代理 **不跑游戏、不碰 git、不改正本**。
5. **双开必须 `game` + `game_b` 两份目录**；同一目录第二个实例会**静默退出**（`Latest.log` 锁）。
6. 改 `.cs` 后**立刻编译**；DLL 同步 `game/Mods/` **和** `game_b/Mods/`。
7. **行尾 / 编码**：改已有文件先看行尾；⛔ 不用 `sed -i`；写文件用 Write/Edit 或 `python -I 文件.py`。
8. **Markdown 表格**：单元格里别写裸 `|`（会切列）；改完跑
   `python -I -X utf8 D:/4/Unity/工具/check_md_tables.py <文件> <起始行> <结束行>`。
9. **只有服务器相关的才允许自己设计**，并在代码里打 `🔶 自己设计（非原版）：…`；其余**照原版**（含原版 bug）。
10. **git**：用户说「收尾 / 交接 / 收工」才 `commit` + `push`；⛔ 没让收尾不动 git；推送失败**只试一次**。
11. **长期任务永不核销**：`LONG-01` 打包 exe · `LONG-02` 版本更新 · `LONG-03` 邮件发送；流程变了改对应 md。
12. `d:/2` 是**只读档案库**；解包到全新空目录，⛔ 不执行解出来的东西，⛔ 不改一个字节。

---

## 三、常用命令

```bash
# 启动（推荐）：起假服务端 + 起游戏 + 退出时自动清理
E:\warpforge\WarpforgeOffline.exe

# 只起假服务端（开发/多开）
cd E:/warpforge/05_工具/server && python -I -X utf8 server.py --strict
# 双开：config_a (8765) / config_b (8766)
python -I -X utf8 server.py --config config_a.json
python -I -X utf8 server.py --config config_b.json

# 编 mod（游戏关着；产物 bin/WfOfflineProbe.dll）
cd E:/warpforge/05_工具/mod/WfOfflineProbe
MelonLoaderDir="E:/warpforge/game/MelonLoader" bash build.sh
# 拷 DLL 到两边
cp bin/WfOfflineProbe.dll E:/warpforge/game/Mods/
cp bin/WfOfflineProbe.dll E:/warpforge/game_b/Mods/

# 起游戏（直接，可带环境变量）
cd E:/warpforge/game
export ELECTRON_RUN_AS_NODE= WF_OFFLINE_REDIRECT=http://127.0.0.1:8765 \
  WF_OFFLINE_STUBS=1 WF_OFFLINE_STEPPROBE=0 WF_OFFLINE_XFORM=1 WF_OFFLINE_FINISH=1 \
  WF_OFFLINE_DISCLAIMER_MODE=ingame
cmd //c start "" Warpforge.exe -screen-fullscreen 0 -screen-width 1600 -screen-height 900

# 截图 / 点击（先把窗口置前）
python -I D:/4/Unity/工具/wf_click.py win Warpforge
python -I D:/4/Unity/工具/wf_click.py shot E:/warpforge/_tmp_view/shots/<日期>/x.png Warpforge
python -I D:/4/Unity/工具/wf_click.py click <x> <y> left 1
# 🔴 双开时**必须用 PID**（两个窗口都叫 Warpforge，按标题找永远只拿到枚举里的第一个 ⇒ 坑 223）
python -I -X utf8 D:/4/Unity/工具/wf_click.py pidlist Warpforge      # 先查 PID
python -I D:/4/Unity/工具/wf_click.py win 39620                      # 之后窗口名一律换成 PID
python -I D:/4/Unity/工具/wf_click.py shot out.png 39620

# 录屏（只录目标窗口 / 可出 MP4；ffmpeg 是随包的，不用装）
python -I -X utf8 D:/4/Unity/工具/wf_record.py E:/warpforge/_tmp_view/rec/<名字> 8 --fps 12 --win Warpforge --mp4
python -I -X utf8 D:/4/Unity/工具/wf_record.py <目录> 6 --at 1.5 --drag 458 580 600 460 --mp4

# 多窗口定位（双开时）
python -I -X utf8 E:/warpforge/_tmp_view/tools/win_pick.py list
python -I -X utf8 E:/warpforge/_tmp_view/tools/win_pick.py focus <pid>
python -I -X utf8 E:/warpforge/_tmp_view/tools/win_pick.py move <pid> x y w h

# 读窗口样式 / 光标（排障）
python -I -X utf8 E:/warpforge/_tmp_view/tools/win_style.py
python -I -X utf8 E:/warpforge/_tmp_view/tools/cursor_id.py

# 邮件箱检查（不起游戏）
cd E:/warpforge/05_工具/server && python -I -X utf8 tools/mailbox_check.py --check
```

**证据目录**：截图放 `E:/warpforge/_tmp_view/shots/<日期_会话>/`；探针 TSV 默认 `E:/warpforge/07_产物/phase0/`
（或 `WF_PROBE_OUT` 指定；双开用 `07_mp/a`、`07_mp/b`）。

---

## 四、当前优先级（**以 `活状态表` 为准，这里只是指针**）

> 现读（2026-11-14 第一百七十九会话收尾）：ACTIVE **7 条**；其中 **`OFF-183` 有 2 条新活可自行推进**
> （动画在队友回合重演 · 队友回合 UI 仍可交互），其余 6 条等用户。
> ✅ 第 178 会话核销 `OFF-189`；第 179 会话在 `OFF-183` 下修掉两条硬缺陷（翼列表重复 ⇒ 位置乱/横跳 · AI 回合空转）。

- **`OFF-183` 2v2**：⏭ **下个会话先做两条**：① 查清并修「卡牌动画在切到队友回合时重演」；
  ② 修「队友回合 UI 仍可交互」（拖牌被闸拦、像点了没反应）。真人 4 端联机仍由**用户亲自做**。
  🔴 用户 2026-11-14 定：**共享 8 格上限保持不变**（不扩槽）。
  → `报告/OFF183_2v2系统梳理_1114.md` · 坑 243/244
- **`OFF-138` 机械神教**：⏸ 用户暂停，等命令恢复（效果与数值未写完）。
- **`OFF-162`**（等数值/音频）· **`OFF-168`**（暂缓）· **`OFF-173`**（方案）· **`OFF-175`**（方案）· **`OFF-178`**（安卓，暂停）
  —— 都是「已出方案/等用户」，⛔ 不主动开工。
- **长期**：`LONG-01`（打包）/ `LONG-02`（版本）/ `LONG-03`（邮件），永不核销。
- **另一个项目（不在本仓库）**：`E:\视频剪辑工具\` —— 本地网页版视频剪辑工具（AI 可驱动 + 字幕拖框 + 配音/语音克隆）。
  进度看它自己的 `进度_P0P1.md` / `进度_P2.md` / `方案_1109.md`；⛔ 不要拿 `资料/离线原版/` 的任务表管它。
- ⛔ `OFF-131` 已并入 `LONG-01`；⛔ 不再按旧的 D:\4 复刻任务开工。

---

## 五、D:\4 复刻工程（只读参考）

- **暂停**（等用户通知）；**不再新增复刻任务**、不再跑 Unity 自检、不改 `Unity/MyGame`。
- 需要旧判据 / 资源地图 / 反编译用法时：看归档的旧 CLAUDE.md，或 `D:\4\Unity\资料\`。
- 现有 `Unity/资料/交接_*.md`、`任务线/`、`项目任务.md` 都是**历史**，⛔ 不要拿它们当离线版待办。

---

## 六、变更记录

- 2026-11-09：按用户要求瘦身；旧全文归档到 `Unity/资料/历史/CLAUDE_md_复刻工程全文_20261109.md`；
  重心改为离线版，新增长期任务（打包/版本/邮件）与离线版命令、真相源表。
- 2026-11-14（第一百七十五会话）：§三 常用命令加「双开必须用 PID」（坑 223，`wf_click.py pidlist`）；
  §四 当前优先级从「2026-11-09 · ACTIVE 2 条」更新为「2026-11-14 · ACTIVE 8 条」，
  并把 **`OFF-185` 商店列为下一个会话的重点**。
- 2026-11-14（第一百七十六会话）：§三 再补「同 PID 多窗口要加标题子串 / 可用 hwnd」（坑 230）；
  §四 更新为 **ACTIVE 7 条·全部等用户**，`OFF-185` 商店与 `OFF-186/187/188` 已核销
  （交接 → `Unity/资料/完全复刻/交接_第一百七十六会话.md`）。
- 2026-11-14（第一百七十七会话）：新增 **`OFF-189` 选秀模式**（ACTIVE 7 → **8 条**）；§四 把它列为
  **下个会话第一件事**（选秀匹配走 Photon、默认启动必弹「连接服务器出错」，用户定「加人机按钮」）；
  新增坑 **237/238**；交接 → `Unity/资料/完全复刻/交接_第一百七十七会话.md`。
- 2026-11-14（第一百七十八会话）：**`OFF-189` 完成并核销**（ACTIVE 8 → **7 条**）——
  选秀「搜索中…」窗加并列「人机」按钮，点击走原版 `ChangeToBotBattle` 进人机对局；
  顺带吞掉选秀的匹配错误弹窗（否则按钮点不到）。新增坑 **239–242**（真窗口是 `SearchingMatchWindowDemo` ·
  Photon 二次失败 43 ms · 按钮父容器的 LayoutGroup 只能 Destroy · 缩尺寸必须 `localScale` + 补锚点偏移）。
  报告 `报告/OFF189_人机按钮_1114.md`；交接 → `Unity/资料/完全复刻/交接_第一百七十八会话.md`。
- 2026-11-14（第一百七十九会话）：**`OFF-183` 2v2 系统性排查**（仍 ACTIVE 7 条）—— 原版棋盘机制取证（每翼 `slotsPerSide=4`、`InsertMinion` 索引插入=连续、战将不占槽、两块棋盘不镜像）；实机修掉两条硬缺陷：① **翼列表 `leftMinions/rightMinions` 重复项** ⇒ 部队位置乱/左右横跳；② **AI 回合空转**（同一动作连发 14 次、卡 3 分钟）⇒ `sameActionBreak`。新增坑 **243/244**；🔴 用户定「**共享 8 格上限不变**」；⏭ 新会话继续：动画在队友回合重演 · 队友回合 UI 仍可交互。
  报告 `报告/OFF183_2v2系统梳理_1114.md`；交接 → `Unity/资料/完全复刻/交接_第一百七十九会话.md`。
