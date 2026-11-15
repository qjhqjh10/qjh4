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

> 🆕 **现读（2026-11-16 第一百九十会话收尾）**：ACTIVE **7 条** —— **`OFF-190` 已整条核销**（19 条全部完成，
> 用户亲测断网可进游戏；明细 → `历史/任务_离线原版_已核销明细_20261108.md`）· `OFF-191`（2v2 重构方案已出、
> 以后再做）· 原 6 条（`OFF-138/162/168/173/175/178`，全等用户）。2026-11-16 `LONG-01` 已出包：
> `打包专用文件夹\WarpforgeOffline_便携版_20261116`（2.6 GB）+ `Warpforge离线版1.2.zip`（2,730,761,339 B / 1,459 条目），
> prepack **PASS 37 / WARN 2 / FAIL 0**。
> 第二批摘要：① **进游戏不再检查云服务器**（启动链零云端请求），点「在线对战」卡才 `/__health` 检查；
> 失败不开面板 + 弹窗指导去 设置→账户 断开重连（可达/不可达两路实机验过）· ② 设置-账户页右下新增说明文字
> （输入 warpforge / 点注册连接；切换账户断开）· ③ 选秀战将候选存在 run 存档里（不重开就固定）；新增
> **「刷新战将」按钮，每笔 run 限 3 次**（退出重进保持、新 run 清零；进入自动重掷默认关）· ④ 卡包页「免费」
> **限 3 次** + 右侧新增 **「放弃」**（回到选秀入口可重开）· ⑤ 坑：`OnReRollFailed` 补丁会启动即栈溢出（已不挂）、
> `GetComponent` 返回基类代理需 `TryCast<T>()`、克隆按钮有多个 TMP 要全清。
> DLL `09f9dcad049e5c25ca8492e2ac2b8a1b`；报告 `报告/OFF190_启动云检查与选秀刷新_1116.md`。
>
> （以下为 2026-11-15 夜存档）🆕 **现读（2026-11-15 第一百九十会话 · 夜）**：ACTIVE **8 条** —— `OFF-190`（用户 9 条 + 追加 5 条，10/11 已处理，
> 只剩 **④ 无网可玩等用户断网复验**；⑦ 手牌上限用户已定 **保持 2v2 放宽**）· 🆕 **`OFF-191`**（2v2 外壳「复制原版代码」
> 重构，**方案已出、用户定以后再做**）+ 原 6 条（`OFF-138/162/168/173/175/178`，全等用户）。
> 追加摘要：① 在线房间就绪闸已修（真缺陷 = 客机一进房就点亮开始按钮；现要求 `guestReady=1`，按钮文案三档
> 「等待玩家 / 等待对方准备 / 开始游戏」；⚠️ 本机双开 Photon `room=null`，ready 同步需用户真机确认）·
> ② 选秀战将随机性：同进程 5 次全不同、重启后不同 ⇒ 未复现「三个固定」，补位 RNG 固定种子已改时间种子 ·
> ③ `OFF-191` 推荐方案 C（模式分叉 + 驱动接口，先做 P0 钩子作用域表）。DLL `ab81b51a8b6546a94864432168943460`。
>
> （以下为第一百八十九会话存档）🆕 **现读（2026-11-15 第一百八十九会话收尾）**：ACTIVE 仍 **6 条**、全部等用户（`OFF-138/162/168/173/175/178`）；
> 本会话处理 **`OFF-183` 余波 4 条用户复查反馈**（不新开任务 ID，已落 `历史/任务_离线原版_已核销明细_20261108.md`）：
> ① 2v2 **队友手卡散开** = `SwapSeat` 早退 + 手牌指针串牌 ⇒ 已修（坑 293/294）；
> ② **手卡张数** = 队友拍从本机手牌出牌 / 后手卡按「侧」取 raw ⇒ 已修（坑 293/295；换牌补牌本身正常）；
> ③ **练习局战将 ≤10 血不再受伤** = **原版 FTUE 新手保护**（`winCount=0/2` 命中、离线胜场永远 0）⇒ 已 patch
>   `MatchData.GetBotConfigFTUEShouldUseFirstGameLogic()` 返回 false（坑 298），实机战将 22→8→7→3→1→-1 阵亡；
> ④ **2v2 天赋串到经典/冲突** = `_local2v2` 从不清零 ⇒ 新增 `ResetLocal2v2`（坑 296，实机 `reset2v2`）；
> 另：压暗层守卫扩到**所有战斗**（坑 299）、只读诊断钩子一批、`FindMethod` DeclaredOnly（坑 300）。
> 交接 → `Unity/资料/完全复刻/交接_第一百八十九会话.md`；DLL `13bc84fbb9939bdc3acd424ed58102ee`；坑 293–300。
>
> 现读（2026-11-15 第一百八十七/八十八会话收尾）：ACTIVE **6 条**，全部等用户（`OFF-138` / `162` / `168` / `173` / `175` / `178`）；
> **`OFF-183`（2v2）已整条核销**（D4 结案=截图伪影 · D5/D3 已修 · AI 卡死/开局双跑按用户口径不修 · alerts 结束观察；
> 真人对战验收由用户做）→ `历史/任务_离线原版_已核销明细_20261108.md`。
> 🆕 **2026-11-15 第一百八十七/八十八会话**：① **敌方回合我方手牌消失**已修（`RestoreHandDisplay` + `_shownHandSeat`，坑 284）；
> ② **2v2「有点卡」**已修（新增 `phase0/perf.tsv` 探针开销采样，元凶 = UiNav/ProfileFix/Config/Inventory 的秒级全场景扫 +
>   `DisconnectPopupProbe` 每帧 2 ms；修后 OnUpdate 4.5–5.6 ms → 0.24–0.31 ms，坑 285）；③ 机器人换牌窗残留（坑 286）；
> ④ **D4 结案 = 截图伪影**（窗口 8–12 px 不可见边框透出背景；`wf_click.py shot` 改成只截客户区，坑 289）；
> ⑤ 天赋窗「选择」按钮跟着面板走（坑 288）+ 压暗层残留守卫（坑 290）；
> ⑥ **启动先本地、云端健康才连** + 非在线流程不弹「连接服务器出错」（新探针 `ServerErrorPopupProbe`，坑 291），
>   「初始化Game UI失败」= `SceneInitializer.OnInitializableError`，钩子在、近期未触发；
> ⑦ **2v2 四座各一副随机卡组**（坑 292）；⑧ **`LONG-01` 已出包**（`打包专用文件夹/WarpforgeOffline_便携版_20261115` 2.6 GB +
>   `Warpforge离线版1.2.zip` 2.54 GB，prepack PASS 35/WARN 2/FAIL 0）；⑨ **`LONG-02` 版本 1.2 全齐**（云端
>   `WF_PHOTON_EXPECT_VERSION=1.2` 已 SSH 改好）。DLL `1a2ee2a6a1388b104468bba349618d3e`；坑 284–292；
>   交接 → `Unity/资料/完全复刻/交接_第一百八十八会话.md`。
> 🆕 **2026-11-15 第一百八十六会话（`OFF-183` · 交接 §四 ①②③ 全做完）**：
> ① **重复登场真根因** = `PlayCardTypeValue()` 查错枚举（AI 动作 `AvailableAction.actionType` 是 `PlayerActions.playCard = 0`，
>   旧码按 `BattleActionType.playCard` 查 ⇒ `Enum.Parse` 恒抛 ⇒ **-1**）⇒ `skip.inPlay` / `skip.doublePlay` 两道闸**从未生效**。
>   修好后实机拦下重复出牌并落 `alerts.tsv play.double`（坑 **276**）。
> ② **`TurnStarted n=2` 对照**：**原生 1v1 一拍一次**（`turn=1..11`）；2v2 那次双跑与 **224 s 的 AI 长停滞同段**，两次复跑未复现（待收口）。
> ③ `hand.gap` 假阳性（换手牌等待窗口）已修（坑 **278**）· `deploy.repeat` 误报（回手后再打出）已改判（坑 **279**）·
>   `talent.timeout`「开窗 0.2 s 被关」= `b4_cmd.txt` 残留 `endturn` 的假象（坑 **277**）。
> ④ **2v2 长局显示问题**（用户要求长局里截图检查）：D1 换牌窗与天赋窗同屏 · D2 自动完成后压暗层残留 · D4 战斗里残留主菜单左右边条 · D5 换牌后大图面板残留；D1/D2 **已改待实机确认**，D4/D5 待查。
> DLL `d995538d0382ad285af6a6edd971c0ec`（停滞判据改为「距上一条动作多久」，长但活跃的 AI 回合不再误报）；坑 **276–283**；报告 `报告/OFF183_重复登场与双开局_1115.md` · `报告/OFF183_2v2长局观测_1115.md`。
> 上一会话（185）：天赋窗反选/选目标/间隔 0.7 s/挡板/2v2 能量/停滞自愈，坑 268–275，DLL `3e614ecb…`。

- **`OFF-183` 2v2**：✅ **已完成子项 18 条全部核销**（用户 2026-11-15 指示；含「绿眼镜 = 去掉、不做」），
  明细 → `资料/离线原版/历史/任务_离线原版_已核销明细_20261108.md`
  「🆕 2026-11-15（第一百八十六会话）核销」。**仍 ACTIVE 的只剩 4 条**：
  ① **真·AI 卡死**（186 把「长而活跃的 AI 回合」排除后仍未复现；`b4:stall snap` 带 `aiAge/aiCur/bmActive`）·
  ② 「开局双跑」（原生 1v1 一拍一次；2v2 只在 14:09 那段长 AI 回合出现过一次，之后 5 轮未再现）·
  ③ `alerts.tsv` 继续观察（`hand.gap` / `deploy.repeat` / `turn.stall` 判据 186 全部改过）·
  ④ **2v2 长局显示问题**：D4 战斗里残留主菜单（最稳复现）· D5/D3 换牌后大图残留 —— 待查；
  D1 已改未复验 · D2 已实机确认修好（见 `报告/OFF183_2v2长局观测_1115.md`）。
  真人 4 端联机仍由**用户亲自做**；🔴 2v2 电脑随机性（`aiDecks`）= 用户定「主线做完后再做」。
  🔴 用户 2026-11-14 定：**共享 8 格上限保持不变**（不扩槽）。
  → `报告/OFF183_重复登场与双开局_1115.md` · 坑 276–279 · 交接 `交接_第一百八十六会话.md`
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

- 2026-11-15（第一百八十三会话）：**`OFF-183` 动画/闸/天赋/换牌窗/告警**（仍 ACTIVE 7 条，未核销）——
  ① **回合闸自锁**已修（⛔ 不能拿 `resolvingTurnEndFlag`/`actionQueue` 判忙，它们含被闸住的这次结束回合本身；补发前 1 s 免闸窗口）；
  ② **换手牌四段顺序 + 1 s 间隔**（旧手牌飞出 0.55 s → 1 s → 引擎 `SetupCardInHand` 入手 → 1 s → 抽卡 → 1 s → 天赋窗），
  代码驱动动作间隔 3 s（动画 2 s + 间隔 1 s）；③ **天赋窗**：标题直接写 `titleTextField` 的 TMP（I2 Localize 拦不到）、
  只认真实输入（`EndSelectionWithDelay` 会随机替玩家选）、0 张 `needOne`、电脑座位 `aiPick` + 出牌门闸；
  ④ **手牌幽灵卡/空位**：每拍按 GO id 去重 + 当前座位被关掉的卡补开；⑤ **场上卡牌始终显示**（跳过正在播动画/特效的卡，
  隐藏前先 `IsOnBoard`）；⑥ **换牌窗第 4 张卡**：布局按前两帧等差对齐；第 4 颗按钮偏窄 = 父级半倍缩放（lossyScale 6.05 vs 12.36）；
  ⑦ **新增 UI 导航工具 `UiNavProbe`**（`ui_cmd.txt`：state/click/clickoff/path）与 **告警表 `alerts.tsv`**；
  ⑧ `Battle4Probe.cs` 曾被批量脚本改坏、已按成员边界重建并 0 error 编译（坑 259）。新增坑 **253–263**；
  DLL `e6b1669be6c7dc233f029bbe7d6ada32`；报告 `报告/OFF183_四修与动画重放探针_1114.md`；交接 → `Unity/资料/完全复刻/交接_第一百八十三会话.md`。

- 2026-11-15（第一百八十四会话）：**卡牌「多选一」闸口修正**（`OFF-183` 仍 ACTIVE 7 条）——
  用户报「星界军卡弹出基因窃取者教会三选一」「卡尔加（UM46）天赋看着像提古里乌斯」。查明两条**同源**：
  `ChoiceFixProbe` 断点 B（`_fixB` 默认开）把「`choicesDefinition.targetsAffected == any` 且 `filterSpecificCards` 非空」
  的卡一律开闸，而原版 `CardScript.NeedsToChooseFromPool` 对 `any` **恒返回 false**（反编译 `CardScript.cs:10219`）。
  全库命中 **11 张**，其中 **5 张是战将天赋**（卡尔加 / 列昂图斯 / 登克勒 / 风暴统领 / 简·扎尔）；
  卡尔加天赋 `Master Tactician` 的候选表与提古里乌斯 `Master of Arcana` 完全相同（3 张 UM 灵能）⇒ 观感错位；
  星界军 5 张卡的候选表原本就写着 4 张 GSC token（原版死数据）。
  **改动**：`_fixB` 默认 `false`（断点 A 保留；调试用 `WF_CHOICE_FIXB=1`），DLL `2e7ed91e6d08ba7bff6ecf95cc867baf`；
  实机 2v2（P4 · 卡尔加卡组）验证：天赋窗 = 2 张「战术大师」，打出后 `CHOICE-FIXB … skip reason=WF_CHOICE_FIXB=0`
  且无 `ChooseCardMenu`。另派子代理做全量卡牌审查（1,351 卡 / 66 战将 / 251 多选一 / 776 有图）：卡数据无串、
  角色卡无错挂立绘、服务端卡表 0 差异、uniqueId 无重复。新增坑 **264**；
  报告 `报告/OFF183_多选一开闸与阵营串味_1115.md` · `报告/卡牌效果与阵营对应_审查_1115.md`。

- 2026-11-15（第一百八十四会话 · 续）：① **2v2 等待对手恢复 120 s 超时**（用户要求；`RoomWaitCo` 去掉 2v2 跳过分支，
  实机 02:10:38→02:12:38 = 120.0 s，超时后回列表、刷新 `n=0` 房间已解散；DLL `2d792cbe6096ed3f771bb8e3b7c90db7`；
  报告 `报告/OFF183_2v2等待超时_1115.md`）。② **卡牌效果代码级全量校验**：1,358 张 typetree（1,351 正式 + 7 张 `#HB_*`），
  引用零悬空、uniqueId 零重复、卡图 GUID 全部命中 catalog；11 张原版死数据候选池、8 张跨阵营全在其中、
  13 张 choiceEnchantment 空附魔为原版设计；报告 `报告/卡牌效果_代码级校验_1115.md`（脚本 `_tmp_view/scripts/card_effect_validate.py`）。
  ③ **绿「眼镜」按钮**查明 = 原版 `BattleManager.chatButton`，首回合后一直显示；大图详情里另有一颗同图标语音按钮（关窗消失），
  实机 `s242_1115_choicefix/30–32` 证明两者独立（坑 265）。④ **2v2 电脑随机性（`aiDecks`）未解决** —— 同侧两座仍共用一份 raw。

- 2026-11-15（第一百八十四会话 · 主线推进）：`OFF-183` 交接 §六 ①②④ ——
  ① **两张相同天赋 → 手里两张（已修+实机）**：引擎 `ResolveCreateHandCard` 协程在 `targetCard==null` 时整段跳过
  （反编译 `_ResolveCreateHandCard_d__512__MoveNext.c` state0）；改成 `BattleCardManager.CreateCard(raw, parent, isPlayer, army)`
  + 加进当拍座位手牌列表。实机 P4 卡尔加：`done chosen=2`，dump 两条同 uniqueId 不同 GO（`#-197244` / `#-201670`）。
  ④ **UiNavProbe**：场景遍历 20000/24 → **60000/32**；无文字按钮用 GameObject 名/父链名；新增 `find <名字>` 与
  `clickname <名字>`（深层找第一个 onClick）。`clickname MulliganContinueButton` 4 s 后出 `turnStart`，换牌窗不用鼠标坐标。
  ② **重复登场未结案**：本轮 3 条 deployAnim 全 n=1；托管层两处 `MoveMinionIntoSlot` 都传 `useMinionConversionAnim=false`，
  同 GO 重放只能来自 `PlayMinionFromHand` 第二次调用 ⇒ 下一步盯同 GO 的 `b4:fx playFromHand` 重复。
  DLL `7ca0c3d830df6f14b8f6b2664dba702a`；坑 **266/267**；报告 `报告/OFF183_天赋第二张与UiNav_1115.md`。

- 2026-11-15（第一百八十五会话）：用户 6 条 + 内部 2 条，全部实机验证，**无任务核销**（`OFF-183` 仍 ACTIVE）——
  ① 天赋窗**反选**（原版 `SelectCardButtonFrame.SelectButtonOnClick` 选中后无条件置灰；postfix 重设，确认按钮常亮 + `needOne`）；
  ② **选目标卡住**（`NeedToChooseMinionTarget` 首条判 `card.isPlayer`，本方 AI 座位进 `UIstate=9` 等鼠标 ⇒ 非本机座位直接 `false`，
  ⛔ 不要用 `FinishedChoosingAbilityTargetByClick`，会 NRE）；③ 动画间隔 **1 s→0.7 s**（`AnimGap`、动作间隔 2 s+0.7）；
  ④ 审计：只有手牌四段/天赋窗等待/AI 动作 3 处前台用间隔，背景特效不受影响；
  ⑤ 天赋窗 **全屏点击挡板**（自建透明 Image，raycastTarget=true，菜单前一个兄弟位）；
  ⑥ **2v2 能量**：座位首次轮到先垫 `StartingMana + HeroInitialManaModifier`（首回合 2/2，之后每座位每回合 +1）。
  内部：重复 `ResolvePlayCardFromHand` 协程闸（丢协程时要清 `resolvingActionFlag`）；深局停滞定因 + raf 连续 >15 s 自愈
  （实机 turn 28 / `turn.stall` 0 次）；手牌重复挪进 1 s 心跳去重。
  DLL `3e614ecbbf1b1f6f49a0016f057a2175`；坑 **268–275**；报告 `报告/OFF183_天赋反选与选目标卡住_1115.md`；
  交接 `交接_第一百八十五会话.md`；截图 `s244_1115_off183/` · `s245_1115_blocker/`。

- 2026-11-15（第一百八十六会话）：**`OFF-183` 交接 §四 ①②③ 全做完，无任务核销** ——
  ① **重复登场真根因**：`PlayCardTypeValue()` 查错枚举（AI 动作是 `AvailableAction`，`actionType` 类型 = **`PlayerActions`**，
  `playCard = 0`；旧码按 `BattleActionType.playCard` 查、那边只有 `playCardFromHand = 5` ⇒ `Enum.Parse` 恒抛 ⇒ **-1**）
  ⇒ `skip.inPlay` / `skip.doublePlay` 两道闸**从落地起从未生效**（全库日志 0 次命中）。改查 `PlayerActions.playCard` 后
  实机拦下 `UM52#-176072`（同一动作 2.8 s 后又挑一次、法力 0）+ `alerts.tsv play.double`。
  ② **`TurnStarted n=2` 对照**：**原生 1v1 一拍一次**（`b4:turn … turn=1..11`，不开外壳）；2v2 那次双跑
  （14:09:17 / 14:09:51，`_NextTurn` 协程指针不变；反编译 `state=2` → `LAB_1809aaf0c` 重跑开局段）与 **224 s 的 AI 长停滞同段**，
  两次复跑（`turn=1..10`）未再复现 ⇒ 留作收口项。
  ③ 顺带：`talent.timeout`「开窗 0.2 s 被关」= `b4_cmd.txt` 残留 `endturn` 的假象；`hand.gap` 换手牌等待窗口假阳性已修；
  `deploy.repeat` 误报（回手后再打出）改判 `deploy.replay`；天赋窗真人路径（反选 / 两张全选 / 继续）实机通过。
  DLL `827830f9fd54452d3bdcf9b794d03b44`；坑 **276–279**；报告 `报告/OFF183_重复登场与双开局_1115.md`；截图 `s246_1115_off183/`。

- 2026-11-15（第一百八十七/八十八会话 · 同日连续）：**`OFF-183` 整条核销**（用户 2026-11-15 定）——
  ① **敌方回合我方手牌消失**：手牌 UI 按侧一块，队友拍换走后对手拍没换回 ⇒ 新增 `_shownHandSeat` + `RestoreHandDisplay`（坑 284）；
  ② **2v2 卡顿**：新增 `phase0/perf.tsv` 逐探针计时，元凶 = `UiNav.WatchScreen` 每秒全场景遍历（尖峰 110–127 ms）/
     `ProfileFix`·`Config`·`Inventory` 秒级全量扫 / `DisconnectPopupProbe` 每帧 2.2 ms / `MenuLayout.FindLayout` 每秒重扫；
     修后 OnUpdate 平均 4.5–5.6 ms → 0.24–0.31 ms、峰值 153 → 7.6 ms（坑 285）；
  ③ 机器人换牌窗残留（坑 286）；④ **D4 结案 = 截图伪影**（`GetWindowRect` 含 8–12 px 不可见边框，`PrintWindow`/客户区都没有；
     `工具/wf_click.py shot` 与 `wf_record.py` 改成只截客户区，坑 289）；⑤ 天赋窗「选择」按钮跟着面板走（坑 288）；
  ⑥ 压暗层（Shade）残留守卫（坑 290）；⑦ **启动先本地、云端健康才连** + 非在线流程不弹「连接服务器出错」
     （新探针 `ServerErrorPopupProbe`，坑 291）；「初始化Game UI失败」= `SceneInitializer.OnInitializableError`，钩子在、近期未触发；
  ⑧ **2v2 四座各一副随机卡组**（`Set2v2SeatRaws`/`SeatRawFor`，坑 292）；
  ⑨ **`LONG-01` 出包**：`打包专用文件夹/WarpforgeOffline_便携版_20261115`（2.6 GB）+ `Warpforge离线版1.2.zip`（2.54 GB，真 ZIP），
     prepack **PASS 35 / WARN 2 / FAIL 0**，包内冒烟通过；重打 `wf_server.exe`、刷新 `WfOfflineData`（对拍 0 差异）；
  ⑩ **`LONG-02` 1.2 全齐**：云端 `WF_PHOTON_EXPECT_VERSION=1.2`（SSH 已改，`version_guard=1.2`），与客户端/包 `版本.txt` 一致。
  DLL `1a2ee2a6a1388b104468bba349618d3e`；坑 284–292；交接 → `Unity/资料/完全复刻/交接_第一百八十八会话.md`。

- 2026-11-15（第一百八十九会话 · `OFF-183` 余波 4 条用户复查）：① **2v2 队友手卡散开** = 队友拍 `SwapSeat` 因
  `_loadedSeat` 与手牌指针脱钩而早退 + `PlayerHand.currentHand` 被当显示开关导致别家抽牌落进本机手牌 ⇒
  加「手牌列表必须是该座位的」早退判据 + 三个加牌入口 `RestoreTrueHandPointer` + 幽灵手牌清理（坑 293/294）；
  ② **手卡张数** = 队友拍从本机手牌出牌 / 后手卡用 `RawForSide` 取到别阵营 ⇒ 已修（坑 293/295；换牌补牌本身正常）；
  ③ **练习局战将 ≤10 血不再受伤** = **原版 FTUE 新手保护**（`CardScript` 里 `isPlayer && currentHealth<=10 &&
  ShouldBecomeInvulnerableWithLowLife` ⇒ `AddTraitSilently(invulnerable)`；flag 来自 `AIBotsConfig.BotListFTUE` 的
  `winCount=0/2`，离线练习胜场永远 0 ⇒ 每局命中）⇒ patch `MatchData.GetBotConfigFTUEShouldUseFirstGameLogic()` 返回 false，
  实机战将 22→8→7→3→1→-1 阵亡（坑 298）；④ **2v2 天赋串到经典/冲突** = `_local2v2` 只置 true 从不清零 ⇒ 新增
  `ResetLocal2v2()`，见过 Arena 后离开战斗场景 2 s 复位（坑 296，实机 `b4:seat reset2v2`）；另：压暗层守卫扩到
  **所有战斗模式**（用 `GameWindow.IsOpen` 判合法压暗，坑 299）、只读诊断钩子一批（`battle:CounterText/Adjusted/AddEffect/
  AddTraitSilently`）、`ProbeUtil.FindMethod` 是 DeclaredOnly 要用 `FindMethodInHierarchy`（坑 300）。
  DLL `13bc84fbb9939bdc3acd424ed58102ee`；坑 **293–300**；报告 `资料/离线原版/报告/OFF183_手牌指针与2v2外壳_1115.md`；
  交接 → `Unity/资料/完全复刻/交接_第一百八十九会话.md`。⚠️ 下次出包（LONG-01）必须重打 DLL。
