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
13. **遇到问题先看 PC 离线版是怎么做的**（`E:\warpforge\05_工具\mod\WfOfflineProbe\Probes\` + `E:\warpforge\05_工具\server\`）：
    安卓照搬同一套机制与判据（登录重定向 · 假服务端模板 · 打桩 · `Addressables.InternalIdTransformFunc` 这类）；
    PC 也没有、确实只能自己设计的，才标 `🔶 自己设计`。⛔ 不要先在安卓侧另发明一套。
14. 🔴 **哪里遇到问题就在哪里加探针**（用户 2026-11-21 定）：现象出现在哪一层（UI 组件 / 引擎方法 / 服务端回包 /
    渲染），就先在**那一处**加只读探针（日志/字段/坐标/顶点数/状态），拿到证据再改；
    ⛔ **不要盲目反复测试**（同一现象反复跑几轮还是没数据 = 方法错了，先补探针）。
    探针要能回答「这条路径被走到没有 / 参数是什么 / 结果是什么」；验证完把噪声探针收掉、保留有用的一行。
15. **每次出 APK 也放一份到 `E:\warpforge\打包专用文件夹\`**（用户 2026-11-22 定）：
    安卓包仍按 `安卓版/APK/` + `版本记录.md` 管理，同时**复制一份**到打包专用文件夹；旧包由用户自己删，⛔ 不主动清理。

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

> 🆕 **2026-11-22 第二百二十一会话（安卓 A 机：用户 15 条反馈）**：新增 **`OFF-197`**（ACTIVE 10 → **11 条**），
> A 机可做的全部落地并实测：Java 层 12 s 触摸闸（开场点屏不再卡 0%）· 帧率显示稳定+默认关 · 去 `Time.frameCount`
> 帧钩子（PERF 55–56）· 设置退出落盘 · 头像/头衔重启保留 + 云端档案 · 默认卡组回灌（Goff 生效）· 匹配修复与加速
> （2.4–4.1 s）· 进房等对手 20 s 超时 · 跨卸载备份恢复链路。**本会话无整条核销**；B 机/装包项挂起。
> 交接 → `Unity/资料/完全复刻/交接_第二百二十一会话.md` · 报告 → `报告/OFF197_用户15条_1122.md` · 坑 **495–503**。
>
> 🆕 **2026-11-20 第二百一十会话（安卓：战斗后闪退排查 —— 未修好，已收窄到「结算视频」）**：
> ① **闪退稳定复现**：小冲突 → 战斗 → 齿轮「放弃」→ `DEFEAT` 结算 → 回主菜单时进程消失
>   （`SIGABRT` + `Il2CppExceptionWrapper`）。**与联机无关**，任何「进战斗 → 回主菜单」都走这条。
> ② ✅ **排除 4 条假设**（真机探针实测，别再重走）：`ToCurrency(0x47)` 返 null（实测非 null）·
>   `GetOwnedCount(0x47)` 抛（实测返 0）· `ResourcesBarController.Initialize` 的 `Toggle()` 摸死对象
>   （第一版修复实测无效，`deadToggles=0`）· `UIResourceCounter.Initialize` 的 `icon` 为 null（练习局实测非 null）。
> ③ 🆕 **用户关键补充：必须先点「结算视频」，回主菜单才崩**。线索 = `videos_assets_all.bundle`
>   **在 APK 里存在**（13.5 MB），但本地 `files/aa` **只有 3 个包** ⇒ 走 Unity 缓存 ⇒ 离线必失败
>   ⇒ `EndBattleDoors`（`videoPlayer 0x90` / `renderTex 0xA8`）坏状态。
>   **下次第一件事 = 把 84 个 bundle 全解到本地**，再测「打完 → 点视频 → 回主菜单」。
> ④ ⚠️ 第一版修复包 `20261120b`（md5 `6becde4c…`）**无效，⛔ 别发人**；发人仍用 `20261120.apk`（`57480f33…`）。
> ⑤ 🔴 **手机当前装的是 listen 开发包**（12:32），**脱离电脑起不来**，要恢复可玩先装回正式包。
> ⑥ 坑 **423**（本闪退，含已证伪清单）· **424**（`install-multiple` 在这台机不可用）· **425**（重装首启免责声明视口 0 ⇒ 整屏卡死，已临时绕过）。
> ⑦ 🔴 教训：**先加只读探针确认「这条路径被走到」再改代码** —— 本会话没验就出包，白跑一轮（打包+安装约 25 min）。
> 报告 → `资料/离线原版/报告/OFF194_联机开战闪退_根因_1120.md`（**§八 = 最新**）；交接 → `Unity/资料/完全复刻/交接_第二百一十会话.md`。
> **本会话无整条核销，ACTIVE 仍 8 条**。

> 🆕 **2026-11-21 第二百零九会话（OFF-194 安卓：防御卡修好 / 图形设置实测 / 改名提示窗 / 崩溃诊断说明 / 重装联机 PASS）**：
> ① **防御卡**：根因 = 离线事件数据 6 个事件 `assignDefensiveCardsInEditMode` 全 false ⇒ 卡池把 39 张防御卡滤光；
>   全改 true（模板 + `offline_data.js` + `WfOfflineData` + `dist/wf_server/responses` 已同步）⇒ 卡池出现、保存落 `defenseCard`、小冲突不再提示；
>   **用户 2026-11-21 亲测真手指拖入正常 ⇒ 子项核销**（坑 **419**）。
> ② **图形设置实测**：高刷新率 ON=60 / OFF=30 fps（60.18 vs 30.09）；质量五档 = URP Low/Medium/Medium/High/Ultra + 钳制 1280/1920/1920·原生/1920·原生/原生 + AA；
>   屏幕 60Hz 下各档都稳 60 fps；安卓帧率上限看 `hiFPS`、PC 的 `FPSLimit` 在安卓是死字段（坑 **420**）。
> ③ **改名后提示窗卡死**：`MessagePopupWindow` 无可用按钮 + `BackgroundCloseButton.window=null` + `closeOnEsc=false` ⇒ 永久卡死；
>   新模块 `mod_popupfix.js` 2 秒自动关（已进正式包，坑 **421**）。
> ④ **崩溃诊断**：正式包自带；原生崩溃可查（栈/寄存器/模块偏移/日志尾巴），OOM/ANR/温度/性能不采集；最多 10 份 + latest + 标记。
> ⑤ **重装后联机 PASS**：单 APK 重装 → 小冲突「战斗！」→ 云端 `/__mm` 配对 `wf-mm-skirmish-5` players=2 → 双端同局、回合同步。
> 报告 → `报告/OFF194_防御卡与图形设置_1120.md` · `OFF194_改名弹窗与崩溃诊断_1120.md` · `OFF194_重装后联机对战实测_1121.md`；
> APK 980,530,806 B · md5 `57480f33f5a3442fff621893c78e8f51`；交接 → `交接_第二百零九会话.md`。**本会话无整条核销，ACTIVE 仍 8 条**。

> 🆕 **2026-11-19 第二百零二会话（OFF-194 安卓：贴图包打通 / 在线对战面板与 2v2 房 / 本地存档唯一 / 混战规则移植）**：
> ① **贴图 AssetBundle 全链路打通（安卓首次）**：`wfart`（6000.3.23f1 打，Android 目标）→ 手机 `files/art/wfart` →
>   `LoadFromFile` + **`LoadAllAssetsAsync`** → `Image.sprite`；主菜单「在线对战」卡显示基里曼立绘。
>   🔴 同步 `LoadAsset` 真机**必 abort**（坑 **376**）；必须**按需加载 + 同拍就贴**（坑 **377**）。
> ② **在线对战面板 / 2v2 房**：点卡 → 探云端 `/__health`（`HttpClient` https 手机实测通）→ 面板（三张模式卡带贴图）→
>   2v2 房按 PC 常量重排（`plate=2004x856 nodes=8/8`）。中途切云端会崩主菜单 ⇒ **只探测不切换**；克隆卡会被网格重建销毁 ⇒ 常驻模板（坑 **378/379**）。
> ③ **本地存档唯一（用户 2026-11-19 口径）**：`ServerLinkProbe.OverrideBase` **恒 null**；实机验「云端可达（仅信息）」+ decks 16 套不变；
>   在线走 **Photon**，云端**不复制**本地卡组。DLL md5 **`3ee0ff850a132a96586d12ebf68922cb`**（game/game_b 已同步）。
> ④ **混战规则移植**：`src/melee_rules.js` + `tools/test_melee_rules.js` **28/28**；`mod_server.js` 落盘 `gameMode=15`。
> ⑤ 环境：Android 模块 / Windows IL2CPP / Unity 6000.2.6f2 + Android 模块 / builtin_shaders / Unity 6.3 文档 —— 全装好（Serctl 代理下载）。
> ⑥ 新坑 **376–380**；报告 → `资料/离线原版/报告/OFF194_安卓贴图与在线对战_1119.md`；交接 → `Unity/资料/完全复刻/交接_第二百零二会话.md`。
> **本会话无核销，ACTIVE 仍 8 条**。

> 🆕 **2026-11-18 第二百零一会话（OFF-194 安卓：2v2 发牌收尾 / 在线对战骨架 / 2v2 房真开局 / 资源包路线）**：
> ① **2v2 克隆座位初始手牌走通原版换牌链**（`AddCardsToMulligan → 协程 → CompleteMulliganPhase`），四座 12 拍轮转；
> ② **修掉原生 SIGSEGV**：自建 `List<CardScript>` 没托管根被 GC ⇒ `obj.ref(true)` 固定（坑 372）；
> ③ **在线对战骨架实机验**：主菜单「在线对战」卡（克隆 `LiveopMenuContainer`）+ 模式面板（快速对战/2v2/混战）+ 2v2 四座房，
>   点击用 `Button.Press` hook 按名字分发（坑 367–369）；
> ④ **2v2 房 → 真开局跑通**：`StartMatch(OwnDeckTraining, pbd, deck, deck, Easy, 1)`，卡组 = `AssetLocator.GetAssets<PrebuiltDeck>()`
>   → `new CardDeck(prebuilt,null)`；关键坑 = **`MatchMakerManager` 在 `Everguild` 命名空间**（坑 371）；
> ⑤ **贴图判死 → 转 Unity AssetBundle**：运行时建/克隆 `Texture2D` 全部 abort（坑 373）；已建 `E:/warpforge/09_安卓资源包/WfArtBundle`，
>   等 **Unity Android 模块**（用户手机下载）到位后打 `wfart`；
> ⑥ **E:/2hhl（HH Legions）安卓版拆解**写进 `08_安卓JS移植/方案.md` **附录 D**（同一套 Gadget+JS+本地服务端架构，资源用 `assets/offline` + `assets.json`）；
> ⑦ **模拟器排除**：BlueStacks（仅 32 位实例）与 MuMu 12（`igvk64.dll` Vulkan 崩）均已卸载（坑 374）；测试回到真机；
> ⑧ 新坑 **367–375**；交接 → `Unity/资料/完全复刻/交接_第二百零一会话.md`；**本会话无核销，ACTIVE 仍 8 条**。

> 🆕 **2026-11-18 第二百会话（安卓：P3 选秀状态 · B 档免责声明/错误弹窗 · 观感两条 · 2v2 P0 前 3 步）**：
> ① **P3 选秀 run 状态完成并实机验**：`mod_server.js` 照 PC `_apply_draft` 移植 12 个分支 + `InitializeLiveOps` 整块替换
>   `Persistence`；手机实跑建 run→选将(UM46)→选包(PFX1)→战绩 1-0-0，重启 `读回状态 … 选秀 run 1`；`tools/test_draft_js.js` 29/29。
> ② **B 档第一批**：连接错误弹窗 **10 s 宽限 + 重试**（`/__health` 真检查）；**游戏内免责声明滚动版**
>   （正文逐字照 PC 第六版 1,888 字 / version 5，`RectMask2D+ScrollRect` 手指拖动实测生效，闸 = 真拖过 `OnBeginDrag` + 10 s 倒计时，
>   **只在主菜单场景弹**、**不拦登录**、用全屏透明点击盾挡背后 UI）。
> ③ **两条用户纠偏**（已落代码）：不要分页只要滚动；加载阶段那扇「很小的声明窗」去掉。
>   🔴 重大教训：**别用「拦 `LoginManager.TryCompleteLogin` 再续跑」当闸** —— 会把**新手引导**带出来（坑 365）。
> ④ **观感两条**：小冲突 5 条奖励 `1256` = prefab 占位（事件 Data 缺 `rewardsPerTier`，已补 1/3/6/10/15）；
>   联盟计分条 NRE 同源缺 `DraftEventData.allianceRewards`（已补 1/2/3）。新增 **运行时 UI dump**（`curl /__uidump` → `files/ui_dump.txt`）。
> ⑤ **2v2 P0 前 3 步**（`mod_battle4.js` + `/__b4?cmd=arm|status|disarm`）：实机确认桥能读 `BattleManager` 两侧槽位；
>   捕获 P1/P2 基座 + **克隆 P3/P4**（`CreateDeck` + `SetupFullHero` + 写回基座引用 + 挂起新战将，无崩溃）；
>   挂 `NextTurn.MoveNext`（只 attach）+ **换座生效**（`TURN#3 side1 seat3 → TURN#4 side0 seat2`，四座轮转）。
>   ⏭ 未完成：克隆座位**初始手牌**（`AddDrawCard` 只入动作队列不生效）· AI 座位出牌 · 四人打完一局。
> ⑥ 新坑 **359–366**（ShowPopUp 7 参取法 · `SetParent` 在 Transform · `Il2Cpp.Array` 无 `.method` ·
>   `Localize`/`EverguildTextController` 改字 · `input swipe` 不是拖拽 · 弹窗被场景切换销毁 ·
>   拦 TryCompleteLogin 带出新手引导 · `1256`=prefab 占位）。
> ⑦ ⚠️ **模板改了 PC/安卓共用的 `ExecuteCloudScript.InitializeLiveOps.json`** ⇒ 下次出 PC 包（`LONG-01`）
>   必须重打 `WfOfflineData` / `wf_server.exe`。
> ⑧ 交接 → `Unity/资料/完全复刻/交接_第二百会话.md`；报告 → `资料/离线原版/报告/OFF194_M2P3与B档_1118.md`（§七–§十一）；ACTIVE **8 条**（本会话**无核销**）。

> 🆕 **2026-11-18 凌晨 第一百九十九会话（OFF-194 M0 达成 · 安卓离线进中文主菜单）**：
> ① **M0 达成**：`08_安卓JS移植` 独立 APK（script 模式）**不连电脑**启动 → 登录 → **中文主菜单**
>   （截图 `_tmp_view/shots/s209_1117_localcur/after.png`：玩家名 WarpforgeOffline · 顶栏水晶 9999/100/金币 2000 真图标 ·
>   玩/收藏/商店/奖励/社交 · 小冲突/选秀/播放教程/经典/练习）。手机 adb `192.168.2.103:41709`。
> ② **三个根因（均已修）**：token 的「环境名」= **`aud` 数组里 `envName:` 前缀项**（坑 348）·
>   跳过教程后 `SignInAnonymouslyAsync` 真连 Unity 后端回 400 ⇒ `stubAuth` 本地走 `ProcessAuthenticationTokens`（坑 349）·
>   安卓 `ConfigManager.GetConfig<CurrencyConfig>()` = null ⇒ 资源条 NRE ⇒ **hook `ToCurrency` 优先返回
>   `AssetLocator.GetAssets<Currency>()` 的 7 个本地资产**（含 gold/raidMedals）+ `GetIcon` 安全实现（坑 350/351）。
> ③ FTUE 四道闸照 PC `Stubs.cs` 补齐；工具修：adb 多条目必须 `-s`（坑 352）· script/listen split 混装会黑屏（坑 353）·
>   新增 `_tmp_view/tools/phone_round.sh`（出包→装包→启动→拉日志/截图 一条命令）。
> ④ **M1 摸底（实机）**：收藏（卡牌/卡组）、商店（13 包 + 详情弹窗）、社交/联盟、练习对战与结算
>   **天然可用**（同一批服务端模板）；补 **选秀「战斗！」自动转人机** —— `FindMatch` attach →
>   `ChangeToBotBattle` + `StartBotBattle`（坑 **356**；截图 `s216_botmd2.png` 进换牌界面）。
> ⑤ **M2 状态层 P0/P1/P2（实机验收）**：`files/wf_state.json` —— 卡组写盘/登录注入（P0）、
>   商店购买 + 两段式开包（P1，`shop_data.js` 权重掉表 + 30 抽保底）、成就进度 + 卡牌升级档回灌
>   `UserInventory[].CustomData.CardTier`（P2）；重启判据 `读回状态：卡组 1 · 成就 5 · 档位 1 · 购买 1`。
>   聊天封禁 4 入口（照 PC `ChatBanProbe`）。坑 **357/358**。
> ⑥ 报告 → `OFF194_M0_JS离线自举_1117.md` §七 · `OFF194_M1_安卓功能盘点_1118.md` ·
>   `OFF194_M2_服务端状态方案_1118.md`；坑 **348–358**；交接 → `交接_第一百九十九会话.md`。
> ⑦ ⏭ **新会话**：P3 选秀 run 状态 · B 档 mod 特性（错误弹窗 10 s 宽限 / 免责声明 / 2v2 本地房 / 混战）。
>   手机现走 **USB**：`ADB_LIBUSB=1`，序列号 `A9SE024A26003434`；`_tmp_view/tools/phone_round.sh` 一键轮次。

> 🆕 **2026-11-17 第一百九十八会话（安卓原生版跑通 · 只剩主菜单）**：
> ① **安卓原生版今晚跑通**：`08_安卓JS移植` 的 script 模式**独立 APK 不连电脑**即可启动 → 登录 →
>   **资源 0 报错** → **进教程战斗场景**（中文正常、可交互）；重定向/进程内假服务端/模板回包全部实机验证。
> ② **资源根因**：不是版本问题，而是 **Unity 缓存放错位置** —— 按
>   `E:\warpforge\02_解包资源\安卓版\com.Everguild.WarhammerWarpforge\files\UnityCache`（**一台能正常跑的设备的原样数据**）
>   原样回填后 `Unable to load asset bundle` / `couldn't find item` 全部归零（坑 **347**：`adb push` 目录语义 ·
>   根/条目两级 `__info` 格式不同 · 目录名 = catalog 的 BundleName/Hash128）。
> ③ **新模板** `ExecuteCloudScript.OnUnityAuth.json`（CloudHandle 1099；缺它 ⇒ NotImplemented ⇒「无法初始化服务 Game UI」）。
> ④ **打桩**：`stubCatalog` 有效；返回的托管 `Task` 必须**缓存 + GCHandle 固定**（否则 await 永不醒，坑 345）；
>   `stubUnity`/`stubContent` 会让客户端更早停住；`stubTutorial` 能跳过教程但会撞 Unity auth ⇒ GameUI 失败。
> ⑤ **手机侧两个硬坑**：Honor **logcat 被系统关**（自写 `files/wf_offline.log`，坑 343）· **Gadget 没有 Java 桥**
>   （`typeof Java === undefined`，改用 Unity `Application.*Path` + Frida Socket，坑 344）。
> ⑥ ⏭ **新会话第一件事 = 攻主菜单**：`OnUnityAuth` 的 access token 过不了 `Unity.Services.Authentication` 校验；
>   字段表已按 `AccessToken`（安卓 dump TypeDefIndex 19337）对上（`aud`/`scp` 数组、`ext` 对象、`exp/iat/nbf` 整数），
>   卡在 `environment` claim 名（`configured environment(production) … access token one(null) don't match`）。
>   细节与复跑命令 → `Unity/资料/完全复刻/交接_第一百九十八会话.md`；报告 → `资料/离线原版/报告/OFF194_M0_JS离线自举_1117.md`。

> 🆕 **2026-11-17 第一百九十七会话（手机两条路线实测 + OFF-194 转向 Frida/JS）**：
> ① **手机跑 PC 版成功**（盖世游戏 `com.xiaoji.egggame`）：绕开启动器直接跑 `game\Warpforge.exe` → 进中文主菜单；
>   两个关键坑：**目录名不能有中文**（MelonLoader `plthook_open error` ⇒ 卡「更新数据板…」，坑 337）·
>   **启动器 `WarpforgeOffline.exe` 在容器里 exit 255**（缺 .NET 6，坑 338）。
> ② **OFF-194 路线转向并验证**：**Frida Gadget + frida-il2cpp-bridge 在本游戏 Unity 6000.2.6f2 / metadata v31.1 上可用**
>   —— smali 在 `UnityPlayer.loadNative` 注入 `System.loadLibrary("gadget")`，桥枚举出 **143 个程序集**；
>   旧 LemonLoader/Cpp2IL 路线作废。报告 `报告/OFF194_FridaGadget_Unity6_验证_1117.md`。
> ③ **实施方案与新工作区**：`E:\warpforge\08_安卓JS移植\`（`方案.md` · `build/build_apk.py` 一键出包已实测 ·
>   `build/run_on_phone.py` 热加载 · `src/main.js` M0 骨架 · `tools/` 含 gadget/bridge/`Assembly-CSharp.ts`）。
>   M0 落点已从安卓 dump 确认：`PlayFabSettings.LocalApiServer` + `PlayFabSettingsRedirect.get_ProductionEnvironmentUrl`；
>   假服务端数据 = `05_工具/server/responses/*.json` 103 个模板。
> ④ **`LONG-01` 出包**：`打包专用文件夹\WarpforgeOffline_便携版_20261117\`（2.6 GB，prepack **PASS 37/WARN 2/FAIL 0**，实机进主菜单）；
>   同包在手机盖世游戏里实测可玩；按用户指示**手机上的副本已删**。
> ⑤ 坑 **337–339**（中文路径 / 盖世游戏启动器 / Honor 无线调试息屏失效）。
> ⑥ ⏭ **新会话第一件事 = 做 JS（M0）**：重定向 + 进程内假服务端 + 打桩 ⇒ 判据「断网进中文主菜单」。

> 🆕 **2026-11-17 第一百九十六会话（OFF-193 联机混战房双开实测 + 编辑器收尾）**：
> ① **分页按钮**只跟「卡组」tab 显示（`TickPagerVisibility`；坑 331 已修）· **池按钮高亮**加强 · **副战将重选**（换阵营自动清副栏）· **经典 1v1 回归 PASS**。
> ② **联机混战房（真人双开）跑通**：建房 → 客机进房（`source=melee deck`）→ 准备/开始 → 两端 `BATTLE-GV match=True` → 双端天赋窗
>   （`TALENT-HEROES main/sub raws=2` · `done continueBtn chosen=1`，手牌换成所选副天赋）。
> ③ **天赋窗「继续」**在战斗场景里没有监听 ⇒ 开窗自挂 `AddListener` + 3 s 自动收窗兜底（坑 332）· **客机「没有可用卡组」** ⇒ 两条取库存都走 + `serializedDecks` 兜底（坑 333）。
> ④ ⚠️ **云端可达时点「在线对战」会把 base 切云端，本地票据 401**（`ExecuteCloudScript`）⇒「放弃/结算上报」弹「连接服务器错误」；未改代码，属服务器相关设计（坑 334）。
> ⑤ **混战参数残留**：打完混战再开「小冲突」曾被套 `OfflineMelee` 参数 ⇒ `OnMeleeBattleEnded` 清状态（坑 335）。
> ⑥ **用户选项 A 已落地**：云端可用时先**强制重登云端拿票据**（`CloudscriptHandler.LoginWithSteamAccount`，泛型 `Action<bool,TRes,TErr>` 委托；成功 `RELOGIN-OK` 1.1 s，失败退回本地 60 s 不重试）⇒ 401 的根因被拿掉。
> ⑦ **「卡组详情→练习」电脑卡组列表为空已修**（坑 336：原版按 `CustomGameModeEvent` 逐字过滤，混战卡组带 OfflineMelee ⇒ 全灭；修法 = 空列表时用 `practiceDecks` 经典 48 套直接喂 `collectionDisplay.Initialize`）· **池按钮高亮观感已验**（金框+高光，三态截图）。
> ⑧ 报告 → `资料/离线原版/报告/OFF193_联机混战房与编辑器收尾_1117.md`；DLL md5 **`fb8fd026fd39ba128a2c5a363e7010ad`**。
> ⑨ ✅ **`OFF-193` 用户 2026-11-17 拍板核销**（真人双端结算实测 / 出包 / 战将插图三项均定「不必再管」）
>   → 明细 `资料/离线原版/历史/任务_离线原版_已核销明细_20261108.md`。
> ⑩ 🆕 **`OFF-194`（安卓版 mod 运行时移植）已立项**：用户选「直接冲『完整 mod 体验』」，但明确「**先信息搜索、执行别急做**」。
>   现状：同一款游戏 1.35.0 / Unity 6000.2.6f2 / metadata **v31**，但 PC 与安卓是两份独立平台构建，**不能转换只能移植**；
>   现成安卓 C# 加载器（LemonLoader **0.6.5.1 / 2025-03**、BepInEx.Android.Launcher）大概率解析不了 v31（Cpp2IL 旧版支持 23–29，ML #768/#1100）；
>   桌面 ML 0.7.3 能跑 v31（我们 PC 现役）；**本地 Il2CppDumper 已成功 dump 安卓包**（`07_产物/android_il2cpp_1117/dump`）。
>   ⚠️ 本阶段**未安装任何工具、未接设备**（BlueStacks 安装器已下载未安装）。报告 `资料/离线原版/报告/安卓移植可行性_1117.md`。
> ⑪ 交接 → `Unity/资料/完全复刻/交接_第一百九十六会话.md`；ACTIVE **8 条**。

> 🆕 **2026-11-17 第一百九十五会话（OFF-193 阶段 D · 战斗，实机验完）**：
> ① **天赋窗**：复用 2v2 链路（`Battle4Probe.TalentChoiceCo`），混战分支 = 主战将天赋 + **副战将天赋**各 1 张候选、
>   **只能选 1**（点第二张自动取消第一张，再点自己 = 取消；0 张 `needOne`）；选副天赋后手牌里换成副天赋卡。
>   副战将天赋**不能走 `GetTalentCard()`**（它是 `CardScript` 的方法，副将不入场）⇒ 静态抽取
>   `cardAbilities[trigger=46].abilityLogic[0].summonCriteria.unitsToSummon[0]`（坑 322）。
> ② **判「本局是混战」= `BattleManager.GameplayData` 指针 == `OfflineMelee` 的 `gameplayVariables`**（坑 323），
>   外加 `MeleeProbe.BattleArmed`（只在混战人机开局登记）+ 主将 id 校验；⛔ 不按卡组名/战将/场景推断。
> ③ **特殊资源只随主阵营**：prefix `PlayerManager.AddFaithMana/AddSpiritStoneMana/AddQuestPoints`，
>   主战将 `Uses*` 为 false 时拦（实机：主 SOR1 用 faith 放行 +1、spirit/quest 拦截；诊断命令 `res <kind>`）。
> ④ **副战将不入场**（场上只有主战将）。**2v2 语义未改**（`_talentSingle` 默认 false；混战分支要求 `!IsLocal2v2`；
>   `MeleeReset` 在 2v2 里直接 return）。⚠️ **2v2 实机回归没跑通**（「2v2（人机）」本地房 P4 一直「等待对手」、
>   点开始不进战斗）⇒ 仍欠一次实机回归。
> ⑤ **2v2 实机回归 PASS**（正确顺序 = 进房先**点一个空位入座** → 再加 3 个电脑 → 开始；先加满 4 个电脑会被
>   `全是电脑，拒绝开始` 挡掉，坑 327）。实测四座轮转、天赋窗仍**多选**（`toggle idx=0/1`、`done chosen=2`）、
>   `b4:melee` **0 条**；`BATTLE-GV … match=False` = 指针判据把 2v2 判成非混战 ⇒ 混战分支零泄漏。
> ⑥ **C 视觉（用户口径）**：主/副栏文字「主栏 20/20」「副栏 10/10」**放下面** + 两栏之间**黄色横线**
>   （抽屉锚点改成 0–0.44 / 0.56–1 留位；坑 326 = 克隆的 TMP 源是 `SetActive(false)`，克隆后要手动打开）；
>   **费用曲线/狂野计数 = 整副 30 张统一口径、不分栏**（用户 2026-11-17 定，现状即如此）。
> ⑦ **E 练习入口（实机通）**：卡组详情 →「练习」→ 原版选电脑卡组（13 套经典预设，列表本来就有、不需要改过滤）
>   →「选择卡组」⇒ prefix `DeckInfoPopup.StartPracticeMatch` 改走**统一内核** `OnlineGameProbe.StartMeleeMatch(deck, ai, "practice")`
>   （第 3 页「混战（人机）」= `source=room`，两边共用）；实机 `MELEE start source=practice aiMode=classic mode=15`
>   → 进战斗 → `TALENT-HEROES main=UM46 sub=SAU66 raws=2`。
> ⑧ **用户 5 条复查（报告 §十）**：① 主栏**改在上面**（主抽屉 0.56–1.00 / 副 0–0.44）· ② 卡池**按原版排序**
>   （= `DeckEditingWindow.GetCardCollection` 的 `OrderBy(manaCost)` 稳定排序；英雄池先按阵营）· ③ **副阵营防御卡可入槽**
>   （根因 `CheckCardDrag` 先问 `CanAddCard`，坑 328）· ④ 战将插图「只显示部分」**未定位 · 用户 2026-11-17 定「先记着、⛔ 不主动追」**（数据层 66 张战将卡图全非空、
>   GUID 互不相同、无串图；等用户给具体战将再查）· ⑤ 拖出战将后残留名字已修（引擎重建 Hero 行不一定带 `(Clone)`，
>   坑 329：全隐藏 + 清槽内 TMP + 0.15 s 延迟整理）。
> ⑨ DLL **`f0ba8422c991ffd655dfe565e55dc37b`**；报告 → `资料/离线原版/报告/OFF193_混战模式_阶段D_1117.md`（§八 · §九 · §十）；
>   坑 **322–329**；⏭ 剩：④ 等用户举例 · 池按钮高亮/飘出卡槽残留 · 副战将手感 · 联机混战房真人 · 出包（⚠️ 重打 DLL）。

> 🆕 **2026-11-17 第一百九十四会话（用户三令：OFF-191 核销 / OFF-192 长期挂着 / 继续主线做新模式）**：
> ① **`OFF-191` 已核销**（用户拍板，不再等观感验收；明细 → `历史/任务_离线原版_已核销明细_20261108.md`）。
> ② **`OFF-192` 长期挂着**（P3 引擎默认开关；⛔ 不主动改默认、不主动提醒）。
> ③ **`OFF-193` 混战模式已开工并推进到「能建、能存、能打」** ——
>   **A 数据/规则**：事件 `OfflineMelee`（服务端模板 + 排行榜定义）· `PlayModes=15` **不能 patch 事件 getter**
>   （il2cpp cctor 阶段 AccessViolation，坑 315）⇒ 改 patch 消费方 `GameModes.GetActiveEvent(PlayModes)`；
>   卡组标记 `warcry=WFM1|SUB=<副将>|S=<卡id>xN,…`；`CardDeck.gameMode` 是 `Nullable<PlayModes>` 反射写不进（坑 316）
>   ⇒ **服务端落盘归一化 gameMode=15**；`MeleeDeckRules` selftest 正/负三例 PASS。
>   **B 入口**：收藏→卡组→选择游戏模式 **第 2 页 = 混战**（上一页/下一页 + 标题 + 用户给的卡图 `mode_melee_p.raw`）；
>   在线对战→创建房间 **第 3 页 = 混战 / 混战（人机）**；`ApplyRoomModeGameplay` 对 15 套 `OfflineMelee` 参数。
>   **C 编辑器（功能通、视觉待收）**：原版编辑器上克隆副栏抽屉（影子卡组 `DeckListDrawer.Initialize` prefix）+ 顶部
>   `主卡池/副卡池/副战将` 三按钮 + 副池加减卡 + 保存写 `warcry`；⛔ 混战必须跳过 `DeckUtility.RemoveInvalidCards`
>   （它会删光非主阵营卡，坑 317）。**E**：混战（人机）= 玩家混战卡组 vs 原版经典练习 AI，`StartMatch(OwnDeckTraining)`，
>   实机进战斗且 turn 推进。
>   ⏭ 剩：副战将重选手感/双栏视觉 · **D 天赋窗（主+副各 1 选 1）/特殊资源只随主阵营** · 卡组详情→练习链路 · 联机混战房真人 ·
>   经典/冲突/2v2 回归 + `hook_audit`。报告 → `资料/离线原版/报告/OFF193_混战模式_阶段ABC_1117.md`；
>   坑 **315–320**；DLL **`93d217f1f368208c82c05a4b114b5626`**；⚠️ 出包要重打 DLL 与 `wf_server.exe`。
>   🆕 **2026-11-17 追加（用户 3 条）**：① **选战将流程** —— 混战先选主+副战将再开卡池；任一战将点回卡池
>   ⇒ 回到选战将界面（`OpenCardInformation` prefix 拦战将行）；② **副战将槽**始终可见（空槽写「副战将/主战将」）
>   并清掉原版 `Initialize` 残留的旧战将克隆行；③ 卡图/效果文本核查（子代理，先死灵后全阵营）：
>   **13 阵营 39 张防御卡卡图 GUID 39 个互不相同、文本齐全，数据层没有串图**；用户看到的「死灵防御卡=兽人图」
>   是原版隐藏副本 `Earthquake`（`03b4f101…`，spellType=200 进攻、借 GOF49 Gretchen Tower 图、无文本、`show=0 Hide`）
>   —— 根因 = 我们的混战池没走原版 `show/inventoryOptions/IsHidden` 过滤（187 张隐藏卡会进池、23 张借图）。
>   已修：`MeleeProbe.IsEligibleCard` 统一过滤 + 打开编辑器清理旧存档坏卡（主池 181→115 / 副池 86→68；坑 **321**）。

> 🆕 **2026-11-16 第一百九十三会话**：`OFF-191` 的 **P3 自身复验完成** —— 换 **右队 P4** 才暴露
> `B4TurnCo` 用座位奇偶当队伍的缺陷（敌方座位被装进我方侧），已改 `TeamOfSeat`；顺带修 `SwapSeat`
> 手牌指针竞态（`inv.I5`）与**结算后冻结**（postBattle 停自愈 / P3 循环 `loop.stop` / `_NextTurn` 交还引擎）。
> **问题 5 手卡显示模型已落地**：敌方回合不再把底部还原成本机手牌（顶部敌方卡背照常、张数可见），
> 等用户观感验收；🔴 **P3 默认开 = 用户 2026-11-16 定「按建议执行 ⇒ 保持默认关」**，并已**挂成 `OFF-192`**
> （等用户实机验收 2v2 后给口令再议切换；⛔ 不主动改默认、不主动提醒）。1v1 练习回归 + `hook_audit` 零泄漏 **PASS**。
> 🆕 **`OFF-193` = 新增「混战模式」**（主 20 + 副 10 · 双战将 · 双卡池 · 回合开始天赋窗 · 建房第 3 页 + 卡组模式第 2 页），
> **方案 v3 已定稿、无剩余决策，待用户发开工令**（报告：`资料/离线原版/报告/OFF193_混战模式_实施方案_1116.md`）。
> 🆕 追加修两条用户反馈：**结算前空白弹窗** = `UpdatePooledTrophies` 的 `NotImplemented`
> （`server.py` 加 `Handle` 兜底 + 分支模板，⚠️ 出包要重打 `wf_server.exe`）；**2v2 克隆战将少 +10 血**
> （练习事件 `warlordLifeChange` 补齐，四座一致）。
> DLL `c19c623f0fbc5194db7dc57df6b01e27`；坑 **308–314**；报告 `报告/OFF191_P3复验与手卡显示模型_1116.md`。

> 🆕 **现读（2026-11-16 第一百九十二会话收尾）**：ACTIVE **7 条** = `OFF-191` + 原 6 条（全等用户）。
> **本会话（192）**：① **坑 301 三条全装**（`AddDrawSpecificCard` 7→6 参、`BroadcastUnitSummoned` 2→3 参
> **+ 第 4 实参 `false` 是「只找非静态」而它是 `static`**；`hook_audit.py` 的 `KNOWN_NOT_INSTALLED` 清空；
> 1v1 `hook_audit` 零泄漏 PASS）· ② **2v2 本地房战将阵亡后结算不出**已修（`CurrentPlayingEvent` 未设 ⇒
> `GameModes.CombatFinished` 首句 return；两局含连打第二局结算链全走通）· ③ **用户 6 条反馈**：
> 4 房间不出名片（坑 303）· 3 电脑阵营跟玩家（坑 306）· 1/2 电脑初始手牌只剩 1 张（坑 305）·
> 6 回合间隔 1.5 s（坑 307）**均已修/已加并实机验**；**5 手卡切换动画未做（口径已定）** ——
> 两边队伍各自看自己当前行动队友的手卡，需把「显示座位 / 装载座位」按队伍解耦。
> DLL **`df6b4a1892d7bed69b3899d3c45ef179`**；报告 `报告/OFF191_2v2五条反馈与结算_1116.md`；
> 交接 → `Unity/资料/完全复刻/交接_第一百九十二会话.md`。
> ⚠️ 下次出包（`LONG-01`）必须重打 DLL。
>
> （以下为第一百九十会话存档）🆕 **现读（2026-11-16 第一百九十会话收尾）**：ACTIVE **7 条** —— **`OFF-190` 已整条核销**（19 条全部完成，
> 用户亲测断网可进游戏；明细 → `历史/任务_离线原版_已核销明细_20261108.md`）· `OFF-191`（2v2 重构：**P0 + P1 + P2 已落地并实机验**
> （P0 = 钩子作用域表 + `b4:hit` + `hook_audit.py`；P1 = `SeatBinding` + 4 条不变量；P2 = `BattleDriver` 单一分叉
> （`WF_B4_DRIVER=native` 可 A/B）；1v1 三模式 + 2v2 全 PASS、`b4:inv`/`b4:bind` 零违例；**P3 `B4TurnCo` 已实机跑通 8 拍**
> （四座轮转、`b4:turn` 每拍 1 行，双跑结构上消失；默认关，`WF_B4_TURN_ENGINE=1` 启用），长局/结算/1v1 回归待验）· 原 6 条（`OFF-138/162/168/173/175/178`，全等用户）。2026-11-16 `LONG-01` 已出包：
> `打包专用文件夹\WarpforgeOffline_便携版_20261116`（2.6 GB）+ `Warpforge离线版1.2.zip`（2,730,761,339 B / 1,459 条目），
> prepack **PASS 37 / WARN 2 / FAIL 0**。
> 🆕 2026-11-16（服务器相关，用户口径）：**连接错误弹窗改成「10 s 宽限内静默、超时才弹 + 重试/关闭」**
> （`WF_OFFLINE_ERR_GRACE` 可调；重试走真 `/__health`）· **2v2 保留联机房**（左卡，可见可加入）；
> **新增「2v2（人机）」卡**（右卡）= 纯本地房，不建 Photon 房、进房即切回本地基址 · 提示弹窗 2 s 自动收起。
> 报告 → `报告/服务器错误弹窗与2v2本地房_1116.md`；DLL `0bf8afb37fea97fff852766a8db06023`。
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

- 2026-11-16（第一百九十二会话）：**坑 301 修复 + 2v2 五条反馈 + 结算修复**（ACTIVE 仍 7 条，未核销任务）——
  ① **坑 301 三条全装**（用户拍板）：`AddDrawSpecificCard` 代码按 7 参、实际 **6 参**（两处）；
  `BroadcastUnitSummoned` 按 2 参、实际 **3 参**，且 `FindMethodInHierarchy(..., false)` 的 `false` = 「只找非静态」
  而它是 `public static`（两个错叠加）⇒ 全部改为正确参数 + `isStatic=true`；`Post_UnitSummoned` 改取 `__args[1]`；
  `hook_audit.py` 的 `KNOWN_NOT_INSTALLED` 清空。实机三行均 `OK`、`b4:fx summoned card=ASH5#-198452`、
  1v1 `hook_audit` **零泄漏 PASS**。
  ② **新缺陷已修（坑 304）**：2v2 本地房**战将阵亡后结算不出、敌人停手** —— 根因 `GameModes.CombatFinished` 首句
  `currentEvent == null ⇒ return`，本地房走 `StartMatch` 从未设 `CurrentPlayingEvent`；开局前设成 `PracticeEvent`。
  实机两局（含**连打第二局**）`COMBAT-FINISHED → PROCESS-MATCH-RESULT → SFS → EndBattleDoors` 全走通。
  ③ **用户 6 条反馈**：4 房间不出名片（坑 303，`Refresh2v2Room` 早退于 Photon 房 null）· 3 电脑阵营/战将跟玩家
  （坑 306，`DeckCollection` 未加载 ⇒ 回落玩家卡组；改「等 ≤10 s」+ `AssetLocator.GetAssets<PrebuiltDeck>()` 兜底）·
  1/2 某电脑初始手牌只剩 1 张（坑 305，换牌协程被回合推进打断 ⇒ `CompleteMulliganPhase` 没跑；电脑换牌**默认关**）·
  6 回合结束到下一名玩家留 **1.5 s**（坑 307）**均已修/已加并实机验证**；**5 手卡切换动画未做（口径已定）** ——
  **两边队伍各自看自己当前行动队友的手卡**，需把「显示座位 / 装载座位」按队伍解耦（改 `SwapSeat` /
  `RestoreHandDisplayCo` / `_shownHandSeat`）。
  DLL **`df6b4a1892d7bed69b3899d3c45ef179`**；坑 **301 转已修 + 新增 303–307**；
  报告 `报告/OFF191_2v2五条反馈与结算_1116.md`；交接 → `Unity/资料/完全复刻/交接_第一百九十二会话.md`。
  ⚠️ 下次出包（LONG-01）必须重打 DLL。
