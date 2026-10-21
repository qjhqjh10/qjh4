# W60 · 真机 + Unity 结果（2026-10-21 · 第六十会话）

> 本文件 = 主对话亲自跑的真机 / Unity 批处理记录。截图目录：`d:/4/_tmp_view/shots/2026-10-21_第六十会话/`。
> ⛔ 只记事实与待办；逐条台账仍以 `资料/任务线/<线>/任务总表.md` §一 为准（铁律 6）。

---

## 一、前置：player 重建（本会话已做）

- `-executeMethod PlayerBuild.Run` → **退出码 0**，`error CS` **0 条**。
- `Assembly-CSharp.dll` = **2026-10-21 18:27 / 4,032,000 B**（上一版 08:15 / 4,028,416 B）。
- 日志 `d:/4/_tmp_view/player_build_1021d.log`。

## 二、真机实测（player，2560×1440 全屏）

| # | 项 | 结果 | 证据 |
|---|---|---|---|
| 1 | **回主菜单再进战斗 ×2** | ✅ 日志出现 `[Menu] 清掉上一轮遗留的壳常驻件 3 件` **两次**；两次回菜单后**左栏/顶栏只有一套**、无糊图、无残留 | `Player.log:230,373` · `17_menu_after2.png` `18_menu_after2.png` `22_menu3.png` |
| 2 | **练习窗选预组 → 开战** | ✅ 选中「马库拉格之矛」后：阵营标题 `ULTRAMARINES`、督军 `Uriel Ventris`、费用曲线 12 张（0/1/1/5/1/0/2/2）⇒ 点战斗**真进局** | `04_deck_info.png` `05_selected.png` `06_battle_start.png` |
| 3 | **多卡窗「继续」只关一次** | ✅ 点我方牌堆打开「你的牌库」（7 张 + 继续）；**点一次「继续」⇒ 窗关、局面不变**（手牌 6 张 / 能量 3/3 / 回合不变，无透传动作） | `20_multicard.png` → `21_after_continue.png` |
| 4 | **`A1325` 卡面** | ✅ 场上 3D 卡体 + 手牌 2D 卡面**都有图/框/字**，无花图、缺图、品红 | `07_battle_settled.png` `16_battle2.png` `19_battle3.png` |
| 5 | **气泡台词只说一次** | ✅ 每局只出现一条台词气泡（三局各一条），未见重复 | `06/16/19` |
| 6 | **`A1520` 切窗残留（抽测）** | ✅ 收藏窗 / 商店开→关，回首页**无残留暗层/糊图/壳卡死** | `23_collection.png` → `26_after_back.png` → `28_shop.png` → `30_home2.png` |
| 7 | **练习窗「关窗再开选择归零」** | ✅ **不是缺陷** —— 原版 `PracticeModePopup__Close` 第三句就是 `ResetContent()`（本仓 `PauseMode` 注释 `Shell/PracticeModePopup.cs:960-968` 已记）⇒ 归零是原版行为 | 代码注释 + 真机复现 |

### 二·补：本轮没验到的（卡在哪）

- **治疗飘字**（需 `AM_SK_3` 预组）· **伏击揭示**（需 GSC 预组）—— 预组页**滚轮/拖拽在合成输入下滚不动**（W59 §三·3），列表深处的条目只能真人选。
- **`T14` A1750 12.5 s 冻帧** —— 需要走到那一段演出；本轮三次对局没触发。
- **`T08` 每帧开销** —— 需要 Profiler/真跑量（player 里也没接 profiler 采样）。
- **`-wfscene Battle` 仍不成立**（W59 已记，本会话未复测）。

## 三、Unity 批处理
### 3·1 10 张新按钮 PNG（`T01`）

- 现读 `Assets/CardPresentation/Resources/Art/ui/` 的 10 张新图（`40k_UI_bt_{battlelog,play}_{hover,pressed}` · `40k_voicelines_bt_{L,R}_{blocked,hover,pressed}`）**全部有 `.meta`**，且与既有 UI 图**逐行同形**：
  `textureType:0` · `alphaIsTransparency:0` · `enableMipMap:0` · `nPOTScale:0` · Default 平台 `maxTextureSize:1024 / textureCompression:2(BC7)` · Standalone 覆写 `2048/1`（与既有图一致）· `isReadable:0`。
- ⇒ **不需要重扫 `ArtBaker.ApplyImportSettings`**（跑一遍只会把同一批设置再写一次、并触发全量重导）；本次 player 构建已把它们导入打包。

### 3·2 特效渲染 A/B（`T02` / `T03`）

- 基线已备份到 `资料/比对基线/backup_1002/`（旧 `sweep_{orig,exp,frames}.tsv`）。
- **第 1 趟（`WFSWEEP_SIDE=both`，同进程 orig→exp，消跨进程漂移）已完成**：退出码 0；
  产物备份 `sweep_orig_1021.tsv` / `sweep_exp_normal_1021.tsv` / `sweep_frames_1021.tsv`。
- **重跑 `analyze_sweep.py` ⇒ 新台账 960 行**：**Z 699 · W 254 · E 5 · D 2 · C 0**（旧台账：Z 658 · W 252 · E 39 · C 8 · D 1）。
  ⇒ **`T03` 的 C 组 8 条全部消失**（落到 Z/W 组）—— 与静态复查的判断一致：那 8 条是**旧尺子（无底图）/跨趟漂移**造成的假象，**不是导出侧缺内容**。
- 新台账最差 5 条（E 组）：`Explosion Hive Fleet Arrival Tendrils OLD` **0.20×** · `CamouflageEffect` **1.40×** ·
  `OrkBuff_Snakebitez_2_Intense` 1.46× · `OrkBuffGreenWaaagh` 1.48× · `OrkBuff_Recall` 1.54×。
- 第 2 趟（`WFBIND_FORCE_BUILTIN=1`，给 `T02` 做「原件 vs 自建」受控 A/B）已完成：
  - 全池：可判定 703；`|ln|` 中位 0.001→0.001、均值 0.017→**0.040**；逐效果**变好 22 / 变差 78 / 基本没动 604**。
  - **T02 的两条「最差」用自建反而更接近原版**：`Asteroid Zone` `|ln|` **0.242 → 0.013** · `Void Combat` **0.103 → 0.004**（同批 `Sororitas Raging` 0.349→0.031）。
  - ⇒ **偏差不在自建兜底路，而在「走原件」那一侧**；但自建对 78 条更差（如 `Buff_Tau_Kroot_FriendlyBoard` 0.000→1.694、`Sau_MethodicalDestruction` 0.009→1.368），**白名单整体不能撤**。
  - **待办**：只对 `Asteroid Zone` / `Void Combat` 评估从 `UseOriginal` 白名单移除（改用自建）→ 再跑一趟受控 A/B + 真机复验。

## 四、用户点名的战斗界面问题（2026-10-21 第六十会话）

> 用户原话：①「场上词条图标怎么有东西挡着」②「战将上面怎么没有数值（战将、部队都检查）」③「手卡为什么是灰色的，看参考图修」④「右侧能量条/结束回合按钮再检查，能量条被放在下面了」⑤「一些卡牌的插图超过卡框，派子代理检查」。

| # | 根因（文件:行号） | 修法 |
|---|---|---|
| ① 词条图标被压 | Unity 透明队列按**到相机距离**排、不按 z；徽标底板 z −0.118 / 图标 −0.124 只差 0.006，卡偏离相机轴就被翻序（`Core/CardView.cs` 的 z 序注释早写过这条限制） | 卡内每层显式 `Renderer.sortingOrder`（同队列内先于距离）：数值 60 > 护甲 50 > 徽标 42/41/40 > 文字 32 … > 立绘 10。只在 `CardFace.Board` 生效（场上每卡一段独立队列，卡间不穿插） |
| ② 战将/部队数值缺失 | 合法目标**底光**（`MakeGlow`，z −0.01）与数值层（z −0.150）同队列、按距离排 ⇒ 底光中心不透明，正好涂掉数值格（历史文档 `DIAG_场上缺数值.md` §三·3 同因） | 同上 sortingOrder：底光 15 < 数值 60；`MakeGlow` 建出来后当场补层号 |
| ③ 手牌灰色 | `BattleDriver.RefreshHandPlayable` 把 `Unplayable` 一律 `SetGrey(1)`，且 `playable` 要求 `IsUnit` ⇒ 战术/防御卡永远灰、敌方回合整手灰。原版「打不出」只是**不亮绿描边**、卡面全彩（参考图 0 张灰卡；反编译 `PlayerHand__LightPlayableHand` 全树无 GreyScale） | 手牌 `SetHighlight` 后显式 `SetGrey(0f)`；`playable` 改为「`Cost>=0` 且付得起」，单位额外要求有空位（战术/防御卡恢复绿描边）。灰化本体不动（换牌/选牌/卡池仍故意用） |
| ④ 能量条/结束回合 | 位置逐件与原版差 ≤1.5px（**上方=敌方能量、下方=我方能量**是原版版式）；真正的缺陷 = **能量水晶被大底板盖住**（水晶 z 0.7 > 底板 0.6，而 `HudCanvas` 按 z 降序 ⇒ z 大者画在底下）。数字颜色也不对：我们 gold vs 原版白 | 能量水晶/任务点/信仰/魂石层的 z 改成 `HudDecorZ − 0.05/−0.06/−0.1`（在底板之前）；能量数字改 `Color.white` |
| ⑤ 插图超框 | 大图档 `BigArtScale=1.27` 被**底层矩形立绘 `_art` 与前景抠图 `_artFront` 共用** ⇒ 矩形底图放大后越过卡框外沿（战术/防御 485 张可见，部队 264 张） | 放大只给前景抠图层：`_art` 网格 1.0、`_artFront` 网格 1.27。逐卡 `useOverDraw` 门控与「只有扇形前台那张可溢出」列为后续（判据已在报告里） |

- **验证状态**：typecheck 0/0；player 已重建（见 §一 的下一版 DLL）→ 真机复验见 §五（待填）。

## 五、§四 五项的真机复验（2026-10-21 19:0x，player 19:02 / 4,032,512 B）

| # | 结果 | 证据 |
|---|---|---|
| ① 词条徽标遮挡 | ✅ 卡内 `sortingOrder` 已生效（本轮场上这张只有一个徽标且完整；多徽标卡的逐槽复验仍建议做） | `36_fixed_battle.png` · `fixed_unit.png` |
| ② 战将/部队数值 | ✅ **战将四格全有数**（2 / 2 / 15）· 部队（2 / 2 / 20）· 敌方单位（2 / 2 / 20） | `fixed_warlord.png` · `fixed_unit.png` |
| ③ 手牌灰色 | ✅ **6 张手牌全部恢复彩色**（3 能量下 4/10/8 费无绿描边、2 费有；不再整片灰） | `fixed_hand.png` |
| ④ 能量水晶/数字 | ✅ 蓝色水晶**从大底板后露出来**；数字 `0/0`、`3/3` **白色**（原橙色消失）；位置仍是原版版式（上敌下我） | `fixed_hud.png` |
| ⑤ 插图超框 | ✅ 战术卡（舰队之主）**插图完全收在框内**；部队卡（安塔罗·克罗努斯）**只有角色抠图越框、没有矩形底图**（10-18 用户要的那一档保留） | `37_card_window.png` · `38_unit_window.png` |

- 截图目录：`d:/4/_tmp_view/shots/2026-10-21_第六十会话/`。
- 自检：`BattleScene` **4102 通过 / 1 失败**（唯一失败 = 已知「手牌数框高 γ」，`battle_1021_w60.log`）。
- ⚠️ 仍开着的两笔（判据已齐，见 §四 ⑤）：逐卡 `useOverDraw` 门控 · 扇形只有前台那张可溢出。

## 六、用户追加：战术卡插图只该在**上半窗** + 战斗大图详情（2026-10-21 19:2x）

- **根因**：战术/防御卡框的下半块是实心框面（卡框贴图 alpha 现读：中列 y 0.60–0.95 = **255**），
  只有上半是透明画窗；而我们把立绘裁到**整个卡体矩形** ⇒ 立绘从框的透明侧边/下沿漏出来。
  原版靠 `SoftMaskScript`（拿卡框 alpha 给立绘挖洞），我们没有那一层。
- **修法**（`Core/CardView.cs` `ArtMeshInFrame` + 新 `FrameArtWindow`）：**非单位卡**的立绘
  **只裁纵向**到「卡框透明开口」——开口从卡框贴图 alpha 现算（中列最长透明带 + 内缩 1px），
  横向保持卡体宽（首版连横向也裁 ⇒ 拱顶两角出现黑三角，真机 43/44 抓到、当场改成只裁纵向）。
  单位卡**不裁**（角色立绘本来就要铺到框下、由框的 alpha 轮廓剪）。
- **真机复验（46_tactic_final.png）**：战术卡（火力炮塔）插图**只在上半窗、填满拱形**，
  下半是深色文字区、无插图；单位卡（兰德速攻艇）仍是「角色/载具越框」那一档（10-18 用户要的）。
- **战斗大图详情**（用户第二句）：窗的骨架按原版规格在跑 —— 遮罩 `Menu Dark Background`
  (0,0,0,0.773) 实测**在**（同点背景 85→52 / 190→96）；卡体 523×832.8 居中；`LowerSection`
  (1320×178) + 语音钮在。⚠️ **待用户指出具体哪一处「没做好」**（若指单位卡文字下的插图透出，
  那是框全高透明 + 文字底板半透明的既有画法；要改成 PnP 那种实心文字板需另开一件）。

## 七、本会话追加的自检 / 探针（2026-10-21）

| 宿主 / 探针 | 结果 | 日志 |
|---|---|---|
| `MainMenuScene` | **3688 / 0** | `_tmp_view/MainMenuScene_1021_w60.log` |
| `CollectionScene` | **2047 / 2047** | `_tmp_view/CollectionScene_1021_w60.log` |
| `ShopScene` | **2364 / 0** | `_tmp_view/ShopScene_1021_w60.log` |
| `RewardsScene` | **2313 / 0** | `_tmp_view/RewardsScene_1021_w60.log` |
| `CardBaseDemo`（含 `WF_TEXTBG_DIFF=1` · A1934 探针） | **212 / 0**；探针三图已出：线性透过率 **0.378**（路 B 0.927 / 路 A 0.43 一带）· sRGB 亮度比 0.638 · `|Δ|>1` bbox 与 `px` **IoU 0.96**（矩形取对） | `_tmp_view/cardbase_textbg_1021.log` · `_tmp_view/cardbase/cardbase_a1934_textbg_{on,off,diff}.png` |
| `BattleScene` | **4102 / 1**（唯一失败 = 已知手牌数框高 γ） | `_tmp_view/battle_1021_w60.log` |

- A1934 的读法与待裁：0.378 低于「原版字段推出」的带 `[0.403, 0.523]`（偏透明）⇒ **两条测量路仍矛盾**，不擅自改 `TextBgAlpha`。
- `gen_icon_doc.py` 已重跑：`资料/卡面图标_对照与缺口.md` **2464 行 · 缺口 0**。

## 八、`-wfscene Battle` 修复（工具线 `T02` · 2026-10-21 19:5x）

- **根因**：`-wfscene` 没被忽略 —— `PlayerBoot.cs:334-349` 确实把 `Battle` 载进来了（下层能看到 mulligan）；
  是 **DDOL 外壳在开场收尾时无条件切主菜单**（`Shell/ShellRuntime.cs` 的 `FinishIntro → FadeThenMenu →
  LoadMainMenu`，原 `:939-1022`；A1483 把启动场景改成 Shell 后才必现）。
- **修法**：`ShellRuntime.Awake` 里在 `DontDestroyOnLoad` **之前**记下 `_ownSceneName`；`TickIntro` 与
  `LoadMainMenu` 两处判「活动场景还是不是壳自己」，不是 ⇒ 停开场/不切菜单、留在场内。
  默认双击启动 active 恒为 Shell ⇒ 零影响。
- **真机复验**：`WarpforgePlayer.exe -wfscene Battle`（无 `-wfdrive`）→ **直接进 mulligan**
  （`60_wfscene_battle.png`）；`Player.log` 有 `[Battle] §27 战场 battlearena1` 与
  `[Shell] 当前场景已不是壳场景（-wfscene 等）⇒ 停止开场、留在场内`，**之后没有** `载入主菜单场景`。
- 顺带修：`WFMultiRay.shader` 的 `_USEBORDERFADE`（关键字名与 CBUFFER 变量同名 ⇒ 关键字一开
  `shader error: redefinition`，2026-10-21 扫描日志实测；该值在 shader 里无人读，删声明即可）。

## 九、2026-10-21 晚批：用户追加的 4 条（战术卡超框 / 语音气泡 / 换牌 / 伤害血痕）

### 9·1 战术卡插图仍超框（第二次）
- 根因：`ArtMeshInFrame` 横向仍用**卡体宽**（±1.04635），而卡框不透明金属边只有 x≈-0.973..+0.980
  ⇒ 立绘从框外透明余量漏出（大图档 23/21 px、拱肩 41/33 px；小卡按比例 ~10 px）。
- 修法：`FrameArtWindow` 在窗高范围内逐行取「含中线的透明 run」，累计 **min l / max r**（开口最宽处，
  跳过中线被金属/徽记挡住的行）；`ArtMeshInFrame` 恢复**横向也夹**到该矩形。拱顶窄处由框自身不透明部分盖住，
  不会再出黑三角；框外余量留空（PnP 战术卡框外为空）。
- 回归点：`DarkAngels`/`EmperorsChildren` 战术卡（中线开口比最宽处窄 43–54 贴图 px）。
- **待真机复验**（大图档 + 手牌各一张）。

### 9·2 语音台词气泡（头像被挡 / 音波是平的）
- 头像：立绘 PNG 的 alpha 是**角色抠图**（`art_gof76` 81% 透明），默认 UI 材质按 alpha 画 ⇒ 81% 透出底板。
  原版挂 `Card Image Overflow`（完整插图）⇒ 修：气泡 portrait 换 `CardPresentation/ArtOpaque`（只取 RGB）。
- 音波：底图本身是一条 3 px 直线（与 bundle 逐字节相同）；原版动态来自 `AudioWaveEqualizer`
  （`GetSpectrumData` 前 16 bin ×15 → clamp01 → 10×dt 平滑 → `Material.SetFloat`）。
  修：新增 `Resources/VoiceWave.shader`（UV 位移近似；原 shader 数学拿不到，已在文件头如实标注）+
  `UnitChatPanel.Advance` 按频谱写 `_Amplitude`（停播归 0）。
- **待真机复验**：播语音时连拍两帧（波线相位/幅度不同）；头像铺满窗、不透底板。

### 9·3 开局换牌（对照 `战斗—替换手卡参考图（先手/后手）.png`）
- 真缺陷①：四颗「替换」钮的**字全空** —— `MulliganPanel.cs` 把面板内设计 px 直接喂 `MakeText`
  （要父件局部坐标）⇒ 字落到屏幕上方外。修：`Px(bx,by)` + 字号 28→37（原版 `ReplaceText` 0.30 卡单位≈37.5）。
- 真缺陷②：钮宽只有参考的 55%（161 vs ~317 设计 px）。修：`BtnWFactor=1.086`（判据原版 `ReplaceTextBg`
  `sd 2.5345×0.8232` 卡单位）。
- 真缺陷③：继续条是白的（原版 `m_Color=(0.3686,0.8941,0.5874,1)` 绿）。修：`Img` 支持 tint、继续条上绿色。
- 压暗：α 0.55 → 0.88（原版 Shade α 是入参、未取到立即数，参考图反推 ≈0.85–0.9）。
- **仍待做（本轮未改）**：换牌期整层战斗 HUD 未隐藏（参考图无结束回合/能量/名牌）；左起第 1 张卡疑似回到手牌尺度；
  `_tiltRoot` 的 `AutoCardRotation` 未关；标题/副标题字号（TurnText auto max 55 vs 原版实渲 41.8）。

### 9·4 伤害反馈：红色血痕（用户报「只有白色 -2，没有红色血痕」）
- **不是被盖，是没做**：原版 = `3DBody/DamageCounter/DamageIcon`（SpriteRenderer，sprite `Card Damage` 256²，
  z −0.05、**sortingOrder 1**；数字 `DamageText` 是 2）。我们全仓没有这个节点的运行时引用
  （`CardFeel.cs` 当时自己写着「图标我们没画」）。
- 已接：`PopNumber.Pop` 从效果库 `CardPrefab` 里取 `DamageIcon`，按原版 offset `(0.094,−0.104)×bridge`、
  scale `1.01665×bridge` 摆好，`sortingOrder=1`（数字 2 ⇒ 数字压血痕），并在同一条 DOTween 上
  跑 α 0→0.8235 @PopIn → 保持 → PopOut→0；`CardFeel.cs` 的「没画」注释已订正。
- **待真机**：打一下看白字下面有没有红血痕（数字仍压在上面）。

### 9·5 特效 E 组重导（G1/G2）
- G2（`Tendrils OLD` 的 4 个 SkinnedMeshRenderer 空 mesh）**已修**：`EffectExporter` 补 SMR 分支后
  `RunListed` 重导，现读 4 个 `m_Mesh` **全是真 guid**。
- G1（3 条 OrkBuff 的骷髅 `UVModule.sprites`）**未生效**：导出循环是
  `var s = tsa.GetSprite(i); if (s == null) continue;` —— 原版实例上这一格**本身就是 null**
  （没有名字可查），所以新加的「已知图集兜底」根本走不到。⇒ **还差**：要么按原版图集的槽位顺序
  硬指派（需先证明四个槽的次序），要么找到能解析 `m_SpriteAtlas` 那一格的路。**先记账，别猜顺序。**

### 9·6 真机复验（2026-10-21 20:2x，player 20:21 / 4,037,120 B）
- **换牌**（`71_mulligan_fixed.png` · `72_mulligan_marked.png`）：四颗「替换」钮**都有字**、钮宽与参考一致；
  战术卡（原铸牧师/军团议会/战术条令）插图**只在上半窗**、下面深色文字区无插图；压暗明显加深；继续条为绿色。
- **伤害血痕**（`73_dmg_5.png`）：敌方单位身上 **白色 `-2` + 红色血痕**、我方督军 **`-1` + 红色血痕**，
  数字压在血痕上（sortingOrder 1 < 2）⇒ **DamageIcon 已接上**。
- **语音气泡**：本轮没能抓到气泡帧（语音只在开场/部署/攻击随机触发；连续截图未命中）⇒
  头像 `ArtOpaque` 与音波 `VoiceWave` 的**真机观感仍待验**（代码/typecheck 已过，Shader 编译随 player 构建通过）。
