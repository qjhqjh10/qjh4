# `_tmp_view/` 清理清单（**2026-10-15 只读盘点 · ⛔ 本轮一个文件都没删**）

> 🔴 **用户 2026-10-15 的决定**：「**先不删，只出清单**」⇒ 本文件**只是清单**，⛔ 不是待办、也别照着它自动删。
> **背景**：用户说「**d:/4/Unity/资料 这里的普查文件，过时的请你清理一批，也太多了吧**」——
> 实测**真正的大头不是 `普查产出_*`（74 MB）而是这个 `d:/4/_tmp_view/`（10.39 GB）**，差约 **150 倍**。
> ⚠️ **它在 `.gitignore` 里**（`.gitignore:234` 的 `/_tmp_*/`）⇒ 删它**不动 git**，但也**没有版本历史兜底**。

## 〇、总览

| | 体量 | 文件数 | 顶层条目 |
|---|---|---|---|
| **合计** | **10.39 GB** | **7302** | **1643** |
| 🔴 **被现役文档点名引用**（⇒ 建议留） | **8.48 GB** | — | 113 条 |
| ⚪ 不在白名单 | 1.90 GB | — | 1530 条 |

**「被现役文档点名」的算法**：拿 `CLAUDE.md` · `项目任务.md` · `资料/任务线/*/{任务总表,准则与做法,任务/*}.md` ·
`资料/待办判据_*.md` · `资料/*.md` 这一批**现役文件**去匹配 `_tmp_view/<顶层条目名>`；
命中的条目**整条算「留」**（删了会让那些文档里的路径变死指针）。复现命令见文末。

## 一、⚪ **可删候选（≥ 1 MB）** —— 共 279 条 / 1.75 GB

> ⚠️ **每一条都请你自己确认再删** —— 我只保证「**现役文档没点名**」，⛔ **不保证它没用**。

| 体量 | 文件数 | 目录? | 条目 | 看着是什么 |
|---|---|---|---|---|
| 132.8 MB | 77 | 是 | `arena19` | 构建/场景产物 |
| 107.2 MB | 6 | 是 | `scene_bak_1011` | 构建/场景产物 |
| 107.1 MB | 7 | 是 | `scene_bak18` | 构建/场景产物 |
| 104.3 MB | 6 | 是 | `scene_bak2` | 构建/场景产物 |
| 36.8 MB | 1 | 否 | `truth_a1947_probe.json` | probe 数据 |
| 35.6 MB | 1219 | 是 | `iso_原件` | 构建/场景产物 |
| 30.9 MB | 9 | 是 | `play_a2080` | 构建/场景产物 |
| 26.3 MB | 42 | 是 | `arena20` | 构建/场景产物 |
| 23.7 MB | 1 | 否 | `battlescene_run2_1015.log` | 自检日志 |
| 23.6 MB | 1 | 否 | `battlescene_run3_1015.log` | 自检日志 |
| 23.5 MB | 1 | 否 | `battlescene_run_1015.log` | 自检日志 |
| 23.5 MB | 1 | 否 | `battlescene_run5_1015.log` | 自检日志 |
| 23.0 MB | 1 | 否 | `re2_BattleScene.Run.log` | 自检日志 |
| 22.1 MB | 1 | 否 | `s27_battlescene2.log` | 自检日志 |
| 22.1 MB | 1 | 否 | `re_BattleScene.Run.log` | 自检日志 |
| 22.0 MB | 1 | 否 | `s27_battlescene.log` | 自检日志 |
| 22.0 MB | 1 | 否 | `BattleScene_Run.log` | 自检日志 |
| 21.9 MB | 1 | 否 | `battlescene_run2.log` | 自检日志 |
| 21.8 MB | 1 | 否 | `s22_battle_after_w8.log` | 自检日志 |
| 21.8 MB | 1 | 否 | `s22_BattleScene.log` | 自检日志 |
| 21.3 MB | 7 | 是 | `play_manual` | 构建/场景产物 |
| 19.0 MB | 49 | 是 | `n5` | 构建/场景产物 |
| 17.6 MB | 7 | 是 | `after_第四会话` | 构建/场景产物 |
| 17.5 MB | 7 | 是 | `after_冻结` | 构建/场景产物 |
| 16.4 MB | 6 | 是 | `baseline_收口` | 构建/场景产物 |
| 16.4 MB | 8 | 是 | `baseline_第四会话` | 构建/场景产物 |
| 16.1 MB | 29 | 是 | `probe20` | 目录 |
| 15.9 MB | 36 | 是 | `md1005` | 目录 |
| 15.6 MB | 13 | 是 | `manifest_bak_第六会话` | 目录 |
| 14.8 MB | 29 | 是 | `a1770` | 目录 |
| 14.5 MB | 30 | 是 | `ref_crops` | 目录 |
| 11.8 MB | 1 | 否 | `w16_stale_after.json` | probe 数据 |
| 11.8 MB | 1 | 否 | `w16_stale_before.json` | probe 数据 |
| 11.4 MB | 1 | 否 | `stale_ids.json` | probe 数据 |
| 9.9 MB | 4 | 是 | `prev_1018_reds` | 目录 |
| 9.7 MB | 16 | 是 | `d1` | 目录 |
| 9.2 MB | 4 | 是 | `after_波2a` | 构建/场景产物 |
| 8.7 MB | 1 | 否 | `w16_stale_before.tsv` | 文件 |
| 8.4 MB | 1 | 否 | `stale_ids.tsv` | 文件 |
| 8.3 MB | 28 | 是 | `diag22` | 目录 |
| 7.1 MB | 12 | 是 | `refcrop` | 目录 |
| 6.4 MB | 70 | 是 | `wf_eng4_probe` | 目录 |
| 6.0 MB | 8 | 是 | `cmp20` | 目录 |
| 6.0 MB | 1 | 否 | `run19_battlescene.log` | 自检日志 |
| 6.0 MB | 1 | 否 | `run19_battlescene2.log` | 自检日志 |
| 6.0 MB | 1 | 否 | `s21_final_BattleScene.Run.log` | 自检日志 |
| 5.7 MB | 30 | 是 | `w2_1011` | 目录 |
| 5.6 MB | 1 | 否 | `ruleengine_a2074.log` | 自检日志 |
| 5.6 MB | 1 | 否 | `ruleengine_fix.log` | 自检日志 |
| 5.5 MB | 1 | 否 | `s27_ruleengine.log` | 自检日志 |
| 5.4 MB | 1 | 否 | `s23_ruleengine2.log` | 自检日志 |
| 5.4 MB | 1 | 否 | `s23_ruleengine.log` | 自检日志 |
| 5.4 MB | 1 | 否 | `re9_battle.log` | 自检日志 |
| 5.4 MB | 1 | 否 | `rerun_BattleScene.log` | 自检日志 |
| 5.2 MB | 1 | 否 | `battlescene_run4_1015.log` | 自检日志 |
| 5.2 MB | 1 | 否 | `s22_re_after_w10w11.log` | 自检日志 |
| 5.2 MB | 1 | 否 | `s21_re3.log` | 自检日志 |
| 5.2 MB | 1 | 否 | `s21_re2.log` | 自检日志 |
| 5.2 MB | 1 | 否 | `s21_final_RuleEngineTest.Run.log` | 自检日志 |
| 5.1 MB | 1 | 否 | `re_e1.log` | 自检日志 |
| 5.1 MB | 1 | 否 | `s21_ruleenginetest.log` | 自检日志 |
| 5.1 MB | 1 | 否 | `re_engine.log` | 自检日志 |
| 5.1 MB | 1 | 否 | `re_engine2.log` | 自检日志 |
| 5.0 MB | 1 | 否 | `sync20_re.log` | 自检日志 |
| 5.0 MB | 1 | 否 | `w4_re2.log` | 自检日志 |
| 5.0 MB | 1 | 否 | `w4_re.log` | 自检日志 |
| 4.9 MB | 1 | 否 | `run19_ruleengine2.log` | 自检日志 |
| 4.9 MB | 1 | 否 | `v_00_shell.png` | 文件 |
| 4.9 MB | 1 | 否 | `run20_ruleengine.log` | 自检日志 |
| 4.8 MB | 1 | 否 | `run19_ruleengine.log` | 自检日志 |
| 4.7 MB | 1 | 否 | `battle2_1020.log` | 自检日志 |
| 4.7 MB | 3 | 是 | `prefix_bak` | 目录 |
| 4.7 MB | 1 | 否 | `re_CollectionScene.Run.log` | 自检日志 |
| 4.6 MB | 1 | 否 | `pid_names_ALL.json` | probe 数据 |
| 4.5 MB | 1 | 否 | `s22_11_play.png` | 文件 |
| 4.5 MB | 1 | 否 | `screen_game_1002.png` | 文件 |
| 4.4 MB | 1 | 否 | `rerun_RuleEngineTest.log` | 自检日志 |
| 4.3 MB | 1 | 否 | `iso_orig.log` | 构建/场景产物 |
| 4.2 MB | 1 | 否 | `s24c_MainMenuScene.log` | 自检日志 |
| 4.2 MB | 1 | 否 | `fin_effectexporter.log` | 自检日志 |
| 4.2 MB | 1 | 否 | `ruleengine2.log` | 自检日志 |
| 4.2 MB | 1 | 否 | `s24_MainMenuScene.log` | 自检日志 |
| 4.2 MB | 1 | 否 | `s24b_MainMenuScene.log` | 自检日志 |
| 4.1 MB | 1 | 否 | `ruleengine_fin.log` | 自检日志 |
| 4.1 MB | 1 | 否 | `s24b_ShopScene.log` | 自检日志 |
| 4.1 MB | 6 | 是 | `ah_bak` | 目录 |
| 4.0 MB | 1 | 否 | `re9_shop.log` | 自检日志 |
| 4.0 MB | 1 | 否 | `mainmenuscene18.log` | 自检日志 |
| 4.0 MB | 19 | 是 | `s21_arena` | 目录 |
| 3.9 MB | 1 | 否 | `ruleengine2_1020.log` | 自检日志 |
| 3.9 MB | 1 | 否 | `s22_09_tooltip.png` | 文件 |
| 3.9 MB | 1 | 否 | `s22_10_tip2.png` | 文件 |
| 3.8 MB | 1 | 否 | `s22_01_afterclick.png` | 文件 |
| 3.8 MB | 1 | 否 | `cmp_ec.log` | 自检日志 |
| 3.8 MB | 1 | 否 | `cmp_ec2.log` | 自检日志 |
| 3.7 MB | 1 | 否 | `export_full_收口.log` | 自检日志 |
| 3.7 MB | 1 | 否 | `tone_battle.png` | 文件 |
| 3.7 MB | 1 | 否 | `reexport_full.log` | 自检日志 |
| 3.7 MB | 1 | 否 | `screen_steamwin.png` | 文件 |
| 3.7 MB | 1 | 否 | `screen_steamgame.png` | 文件 |
| 3.7 MB | 1 | 否 | `s21_06_battle.png` | 文件 |
| 3.7 MB | 1 | 否 | `screen_full2.png` | 文件 |
| 3.7 MB | 1 | 否 | `rulediag4.log` | 自检日志 |
| 3.7 MB | 1 | 否 | `export.log` | 自检日志 |
| 3.7 MB | 1 | 否 | `lic_probe3.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `s24b_CollectionScene.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `reexport3.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `export_full_1002.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `rulediag2.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `export_full_1002d.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `rulediag3.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `menu2.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `export_full_1002e.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `export2.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `s24_CollectionScene.log` | 自检日志 |
| 3.6 MB | 1 | 否 | `screen_whole.png` | 文件 |
| 3.6 MB | 1 | 否 | `export3.log` | 自检日志 |
| 3.6 MB | 5 | 是 | `vfxkey_bak_1015` | 目录 |
| 3.6 MB | 1 | 否 | `export_full_收口2.log` | 自检日志 |
| 3.5 MB | 1 | 否 | `shop_probe.log` | 自检日志 |
| 3.5 MB | 1 | 否 | `shop3.log` | 自检日志 |
| 3.5 MB | 1 | 否 | `shop4.log` | 自检日志 |
| 3.4 MB | 1 | 否 | `battle_1016.log` | 自检日志 |
| 3.4 MB | 1 | 否 | `pv18_deck3.png` | 文件 |
| 3.3 MB | 1 | 否 | `iso_exp.log` | 构建/场景产物 |
| 3.3 MB | 1 | 否 | `menu_1020.log` | 自检日志 |
| 3.3 MB | 1 | 否 | `iso_probe.log` | 构建/场景产物 |
| 3.3 MB | 1 | 否 | `iso_probe4.log` | 构建/场景产物 |
| 3.3 MB | 1 | 否 | `iso_exp_force_matcap2.log` | 构建/场景产物 |
| 3.3 MB | 1 | 否 | `iso_probe2.log` | 构建/场景产物 |
| 3.3 MB | 1 | 否 | `iso_probe3.log` | 构建/场景产物 |
| 3.3 MB | 1 | 否 | `iso_exp_force_matcap.log` | 构建/场景产物 |
| 3.2 MB | 1 | 否 | `s21_04_collection.png` | 文件 |
| 3.2 MB | 1 | 否 | `ui_01_shop.png` | 文件 |
| 3.2 MB | 1 | 否 | `ui_03_collection.png` | 文件 |
| 3.2 MB | 1 | 否 | `re_DeckScene.Run.log` | 自检日志 |
| 3.1 MB | 1 | 否 | `battle4.log` | 自检日志 |
| 3.1 MB | 1 | 否 | `battle3.log` | 自检日志 |
| 3.0 MB | 1 | 否 | `s21_03.png` | 文件 |
| 3.0 MB | 1 | 否 | `s21_02.png` | 文件 |
| 3.0 MB | 1 | 否 | `s21_01_collection.png` | 文件 |
| 3.0 MB | 1 | 否 | `w_00.png` | 文件 |
| 3.0 MB | 1 | 否 | `ui_00_shell.png` | 文件 |
| 2.9 MB | 13 | 是 | `wa345a_cache` | 目录 |
| 2.9 MB | 2 | 是 | `col` | 目录 |
| 2.9 MB | 8 | 是 | `localboot_1002` | 目录 |
| 2.9 MB | 5 | 是 | `g1_bak_1015` | 目录 |
| 2.8 MB | 5 | 是 | `a236_1011` | 目录 |
| 2.8 MB | 1 | 否 | `battle_a375.log` | 自检日志 |
| 2.8 MB | 24 | 是 | `origboot` | 目录 |
| 2.8 MB | 1 | 否 | `unity_battlescene_run.log` | 自检日志 |
| 2.8 MB | 1 | 否 | `s27_settings_2.png` | 文件 |
| 2.8 MB | 1 | 否 | `s27_settings_1.png` | 文件 |
| 2.8 MB | 1 | 否 | `s27_collect_2.png` | 文件 |
| 2.8 MB | 1 | 否 | `s27_collect_1.png` | 文件 |
| 2.8 MB | 1 | 否 | `s27_shell_1.png` | 文件 |
| 2.7 MB | 3 | 是 | `vc` | 目录 |
| 2.6 MB | 1 | 否 | `final_ShopScene.log` | 自检日志 |
| 2.6 MB | 1 | 否 | `s1007c_shop.log` | 自检日志 |
| 2.6 MB | 1 | 否 | `s1007b_battle.log` | 自检日志 |
| 2.6 MB | 1 | 否 | `s1007_battle.log` | 自检日志 |
| 2.6 MB | 5 | 是 | `c5_bak` | 目录 |
| 2.6 MB | 1 | 否 | `s24c_ShellScene.log` | 自检日志 |
| 2.5 MB | 1 | 否 | `shellscene18.log` | 自检日志 |
| 2.5 MB | 1 | 否 | `battle_after_whitelist.log` | 自检日志 |
| 2.5 MB | 1 | 否 | `battle5.log` | 自检日志 |
| 2.5 MB | 1 | 否 | `BattleScene.log` | 自检日志 |
| 2.5 MB | 1 | 否 | `menu4.log` | 自检日志 |
| 2.5 MB | 1 | 否 | `menu3.log` | 自检日志 |
| 2.4 MB | 2 | 是 | `vfx_bak` | 目录 |
| 2.4 MB | 35 | 是 | `W7_e5` | 目录 |
| 2.4 MB | 1 | 否 | `v_03.png` | 文件 |
| 2.4 MB | 1 | 否 | `v_02_now.png` | 文件 |
| 2.4 MB | 1 | 否 | `v_01_collection.png` | 文件 |
| 2.3 MB | 7 | 是 | `m_menufill` | 目录 |
| 2.2 MB | 18 | 是 | `g2` | 目录 |
| 2.2 MB | 4 | 是 | `g3_bak` | 目录 |
| 2.2 MB | 1 | 否 | `s21_05_practice.png` | 文件 |
| 2.2 MB | 1 | 否 | `s24_DeckScene.log` | 自检日志 |
| 2.1 MB | 1 | 否 | `final_MainMenuScene.log` | 自检日志 |
| 2.1 MB | 1 | 否 | `s22_12_hud.png` | 文件 |
| 2.0 MB | 1 | 否 | `menu_fix1.log` | 自检日志 |
| 2.0 MB | 1 | 否 | `RuleEngineTest.cs.bak_a1` | 文件 |
| 1.9 MB | 1 | 否 | `s1007d_menu.log` | 自检日志 |
| 1.9 MB | 1 | 否 | `deck18b.log` | 自检日志 |
| 1.9 MB | 1 | 否 | `s1007c_menu.log` | 自检日志 |
| 1.9 MB | 1 | 否 | `deck18c.log` | 自检日志 |
| 1.9 MB | 1 | 否 | `BattleScene.cs.bak_a2` | 文件 |
| 1.9 MB | 1 | 否 | `x_01_shop.png` | 文件 |
| 1.9 MB | 1 | 否 | `ui_02_rewards.png` | 文件 |
| 1.8 MB | 289 | 是 | `cmp_before` | 目录 |
| 1.8 MB | 1 | 否 | `collection2.log` | 自检日志 |
| 1.8 MB | 1 | 否 | `screen_now2.png` | 文件 |
| 1.8 MB | 1 | 否 | `rewards_a375.log` | 自检日志 |
| 1.7 MB | 1 | 否 | `rewards_a370.log` | 自检日志 |
| 1.7 MB | 1 | 否 | `probe_shell.log` | 自检日志 |
| 1.7 MB | 1 | 否 | `probe_shell_after.log` | 自检日志 |
| 1.7 MB | 1 | 否 | `s22_03_crops2.png` | 文件 |
| 1.7 MB | 272 | 是 | `cmp_f` | 目录 |
| 1.7 MB | 27 | 是 | `w5probe` | 目录 |
| 1.7 MB | 1 | 否 | `s22_05_cardface.png` | 文件 |
| 1.6 MB | 1 | 否 | `final_CollectionScene.log` | 自检日志 |
| 1.6 MB | 1 | 否 | `rewards3.log` | 自检日志 |
| 1.6 MB | 1 | 否 | `s22_02_crops.png` | 文件 |
| 1.6 MB | 8 | 是 | `c2crop` | 目录 |
| 1.6 MB | 1 | 否 | `rewards2.log` | 自检日志 |
| 1.6 MB | 1 | 否 | `iso_exp_force.log` | 构建/场景产物 |
| 1.6 MB | 1 | 否 | `s1007_collection.log` | 自检日志 |
| 1.6 MB | 1 | 否 | `s1007c_collection.log` | 自检日志 |
| 1.6 MB | 1 | 否 | `collection_1006.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `artbaker_b30.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `artbaker.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `applyimport_1015.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `pb_final.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `player_build6.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `player_build2.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `player_build3.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `pb2.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `pb_ctl.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `pb.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `player_build9.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `run_pb2.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `playerbuild3.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `playerbuild7.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `playerbuild4.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `playerbuild5.log` | 自检日志 |
| 1.5 MB | 1 | 否 | `playerbuild8.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `playerbuild_1015.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `s27_playerbuild.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `player_buildA.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `player_build5.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `player_build4.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `pb3.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `playerbuild6.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `battlescene_run6_1015.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `player_build.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `playerbuild18.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `run_playerbuild.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `playerbuild_1012.log` | 自检日志 |
| 1.4 MB | 1 | 否 | `wf_s2.png` | 文件 |
| 1.4 MB | 1 | 否 | `wf_s1.png` | 文件 |
| 1.4 MB | 1 | 否 | `wf_hover.png` | 文件 |
| 1.4 MB | 1 | 否 | `playerbuild2.log` | 自检日志 |
| 1.3 MB | 1 | 否 | `shell_retry.log` | 自检日志 |
| 1.3 MB | 1 | 否 | `shell2.log` | 自检日志 |
| 1.3 MB | 1 | 否 | `wf_shaders_extra.bundle.bak_1018` | 文件 |
| 1.3 MB | 1 | 否 | `final_RewardsScene.log` | 自检日志 |
| 1.2 MB | 1 | 否 | `shell3.log` | 自检日志 |
| 1.2 MB | 1 | 否 | `MainMenuScene.cs.bak_a3` | 文件 |
| 1.2 MB | 15 | 是 | `diag22_badge` | 目录 |
| 1.2 MB | 1 | 否 | `s22_10_crop.png` | 文件 |
| 1.2 MB | 1 | 否 | `rewards_1006b.log` | 自检日志 |
| 1.2 MB | 1 | 否 | `h6_fresh_battlearena2_manifest_crlf.json` | probe 数据 |
| 1.2 MB | 1 | 否 | `rewards_1006.log` | 自检日志 |
| 1.2 MB | 1 | 否 | `screen_steam.png` | 文件 |
| 1.2 MB | 1 | 否 | `s22_15_hudcmp2.png` | 文件 |
| 1.2 MB | 1 | 否 | `rewards_1006c.log` | 自检日志 |
| 1.1 MB | 1 | 否 | `h6_fresh_battlearena2_manifest.json` | probe 数据 |
| 1.1 MB | 1 | 否 | `h29_fresh_battlearena2_manifest.json` | probe 数据 |
| 1.1 MB | 1 | 否 | `_bak_card_stats_1015.json` | probe 数据 |
| 1.1 MB | 1 | 否 | `card_icon_plan.data.json.bak_1015` | 文件 |
| 1.1 MB | 1 | 否 | `card_icon_plan.res.json.bak_1015` | 文件 |
| 1.1 MB | 1 | 否 | `s22_14_hudcmp.png` | 文件 |
| 1.1 MB | 1 | 否 | `s22_09_crop.png` | 文件 |
| 1.1 MB | 1 | 否 | `w_01.png` | 文件 |
| 1.1 MB | 1 | 否 | `h6_fresh_battlearenadarkangels_manifest.json` | probe 数据 |
| 1.1 MB | 1 | 否 | `s22_17_right.png` | 文件 |
| 1.1 MB | 1 | 否 | `x_02_then_collection.png` | 文件 |
| 1.1 MB | 1 | 否 | `s27_settings_4.png` | 文件 |
| 1.1 MB | 1 | 否 | `s27_settings_3.png` | 文件 |
| 1.1 MB | 1 | 否 | `s27_collect_3.png` | 文件 |
| 1.0 MB | 5 | 是 | `sweep_backup_1001` | 目录 |
| 1.0 MB | 1 | 否 | `mainmenuscene.log` | 自检日志 |
| 1.0 MB | 1 | 否 | `mainmenu.log` | 自检日志 |
| 1.0 MB | 1 | 否 | `h6_fresh_battlearenaleviathan_manifest.json` | probe 数据 |
| 1.0 MB | 1 | 否 | `menu_check.log` | 自检日志 |
| 1.0 MB | 1 | 否 | `b4_MainMenuScene.log` | 自检日志 |
| 1.0 MB | 1 | 否 | `RuleEngineTest.cs.bak` | 文件 |
| 1.0 MB | 1 | 否 | `b5_MainMenuScene.log` | 自检日志 |

### 小条目（< 1 MB）—— 1251 条 / 154.5 MB

多半是**单次运行的小日志、切图、`.bak`**。⚠️ **没逐条列**（会很长）—— 要看得跑文末的脚本。

## 二、🔴 `_tmp_view/player/` 内部（**最大的一条，但它被现役文档点名 32 次**）

⇒ **不能整个删**。但里面**大头是构建产物**，可以只删那一部分：

| `player/` 里的东西 | 体量 | 性质 | 建议 |
|---|---|---|---|
| `player/WarpforgePlayer_Data/` | 6767.1 MB | **构建产物**（`PlayerBuild.RunWithSubsetLibrary` 可重建） | **可删** |
| `player/UnityPlayer.dll/` | 35.0 MB | 运行时文件（构建产物） | **可删** |
| `player/MonoBleedingEdge/` | 8.7 MB | 运行时文件（构建产物） | **可删** |
| `player/D3D12/` | 4.5 MB | 运行时文件（构建产物） | **可删** |
| `player/auto/` | 30.1 MB | **截图**（`auto_02_02_换牌之后` 等被引） | 留 |
| `player/crops/` | 11.5 MB | **截图**（`cmp.png` / `ours.png` 被引） | 留 |

⇒ **`player/` 里约 6.82 GB 是构建产物**（删了下次跑 `PlayerBuild` 就回来），**约 0.18 GB 是截图/日志（要留）**。

## 三、复现命令（**这些数字怎么来的**）

```bash
# 每个顶层条目的体量 / 文件数
du -sh /d/4/_tmp_view/* | sort -h

# 白名单：现役文档点名了哪些 _tmp_view 条目
grep -rhoE '_tmp_view/[A-Za-z0-9_.-]+' CLAUDE.md 项目任务.md Unity/资料/*.md
     Unity/资料/待办判据_*.md Unity/资料/任务线/*/*.md Unity/资料/任务线/*/任务/*.md | sort -u
```

## 四、⚠️ 如果哪天真要删 —— 三条注意

1. **先看白名单**（本文算出来是 113 条 / 8.48 GB）。白名单里的**任一条**都可能是某份判据/任务表的证据出处。
2. **`scene_bak*` 那几条要看一眼**：本工程有「**跑会存盘的自检宿主会重写场景**」的坑（`CLAUDE.md` §二），
   这些 `scene_bak*` 就是当时的场景副本 —— 删之前确认对应场景**已在 git 里**（`Scenes/` 是入 git 的）。
3. **砍之前把清单落盘**（就是本文）＋ **记下删了多少**，别让「为什么少了 X」变成下一轮的悬案。

---

> 📌 **本文件由主对话 2026-10-15 生成**；数字是**那一刻的读数**，⛔ **别当现读** —— 要现读跑上面那两条命令。
