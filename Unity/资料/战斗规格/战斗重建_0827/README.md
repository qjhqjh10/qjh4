# 战斗重建_0827/ — 战斗重搭专项资料(2026-08-27,勿删)

> 用途:新会话**战斗重搭任务**的权威输入。用户 2026-08-27 指示:战斗相关按说明书+Unity JSON 重新搭建,场景用原版 RT 截图。

## 文件清单(2026-08-27 夜整理)

| 文件 | 内容 | 状态 |
|---|---|---|
| 战斗重建规格表_0827.md | 子代理整读说明书产出:2D 层全树/HUD 元素表/手牌场卡/特效锚点/教程/相机。**含更正:旧 battlearena1_2D层全树.md 是缺漏版 dump(缺 ~20 子树),13 场景 633 节点同源仅 6 处差异** | ✅ 完整 |
| 战斗界面JSON权威表_0827.md | 原始 Unity JSON 逐项(191 项):所有 HUD 元素 chain_rect 绝对坐标/字号/颜色/贴图/active + 2DCard 复核一致 + 16 条坑(EnemyDeck 屏外需运行时重设/ChatButton×6 重叠/字号勿统一等)+**顶部更正节 A1-A34(8-27 深读轮:两把权威表错误坐标/m_OnClick 全空结论作废/2DCard PathID 实例串号/卡名字号 0.19=JSON 0.122=定案等)** | ✅ 完整 |
| 战斗重建方案_0827.md | 重建设计方案(架构/逐区规格/行为状态机/RuleCore 接口/实施步骤 7 步/待定项 R1-R8)。**注:方案文内个别旧值(如 §2.3 Mulligan Continue 坐标/SLOT 步进)=被审查更正清单取代,以清单+代码为准** | 🔄 已被审查清单更新 |
| 审查更正清单_0827.md | **8-27 全面审查轮产物**:battle.gd 已实现元素逐项(默认全错立场)对照 Unity JSON+总册+转化文件,权威更正表+5 大结构性错误(场卡尺寸/牌库计数闪现/手牌平扇公式/Mulligan 组坐标/攻击选择器叠放)+正确项证据 | ✅ 权威(新会话首选) |
| RuleCore对战API手册_0827.md | rule_core.gd 全部对局 API+ctx 结构+UI 层职责+坑 | ✅ |
| 子代理读报_*.md ×7 | back左区/back右区/front交互层/front弹层/2dcard/csharp行为/引擎映射(全部数值 PathID 溯源) | ✅ |
| 工具_0827/ | 子代理临时分析工具(_inspect_mb/_inspect_subtree——读原始 JSON 用) | ✅ 可复用 |
| 反编译资产 | `Warpforge_tools/data/decomp_il2cpp_0827/`(decomp_out+decomp_out2=2036 方法体反编译 C+ghidra_scripts;原 Warpforge_tools/tmp/ 已迁出;dumper/ghidra 工程 3.2G 已删可重跑) | ✅ |

## 核心结论(新会话别重查)
- 已完成:Step0-3(骨架/Back 层/手牌+Mulligan/出牌攻击回合 AI)+审查修正轮(场卡尺寸体系/手牌平扇/牌库计数闪现/9 格 149.3/131.9/攻击选择器叠放/Mulligan 组 chain 等)——battle.gd 当前=审查修正版(备份 Warpforge备份/0827_battle_rebuild/battle.gd.bak_auditfix_0827)
- 9 格:MinionSeparation(0.82/1.53)×严格投影 k(182.1/86.2)=**149.3/131.9px**;玩家行 z=-6.655(近/PlayerBoardArea 1319→MinionArea 1309 实锤);场卡=GetUnitSizeInPlay(2DCard×desiredScale 0.36/0.4/0.69/0.77)玩家 137×218/督军 152×243/敌 124×198/138×221,PnP face 0.75 等比
- 手牌平扇:heightMod((n−1)/11)、幅度=curve×0.7×mod×卡高(4 张≈平/12 张深扇)、收紧=超限 spacing=max_w/n;rotation=atan(dx/330)×rotationMod×0.35(系数待核);显示时机=点卡(ShowDeckSize)
- 攻击选择器=四钮同坐标 (590,541.7) 互斥+Highlight 176.2+ValueText fs89;Spell 钮 sprite=0 运行时(记录)
- 待核项(记录在审查清单):GetUnitSizeInPlay base 常数/SLOT_Y/HAND_Y/rot 0.35/TMP autosize 缩字
- **下一步**:Step 4 Front 弹层(CardDisplayWindow/设置面板/聊天/AboveShader/结算门)→Step 5 全链验证(rule_test+整屏截图)→F5 目视→落档
- 诊断工具:d:/warpforge/tools_dev/shot_battle.gd(零写入,全链模拟点击截图)+dump 卡/槽位坐标
