# 战斗优化证据包（2026-08-28 会话）

> 本目录=本会话（用户 5 问核查+战斗优化批+用户 4 条反馈闭环）产出的**视觉证据**，供新会话/用户 F5 目视回溯。
> 详细过程=`D:\2\项目任务文件.md` 顶部续2/续3/续4 行 + `D:\2\战斗优化方案_0828.md`；坑=使用地图坑 114-118。

## f1_槽位叠图/
- `full_debug.png`/`full_board.png`：F1 敌我槽位 vs 背景 RT 板面贴合验证（全屏；数值表=tools_dev 运行日志）
- `zoom_p{0,1}{l,r}.png`：四区放大（绿框=玩家槽、红框=敌方槽、黄=中线）
- **结论**：18 槽实算=权威值一致、贴板面；像素暗区=背景照片道具（玩家右侧金属栅栏/敌侧金色柱），非错位。
- ⚠ 后续（续4）用户口径=2D 对称化：`SLOT_STEP_E=131.9→149.3`+敌我同尺寸——**本项目保留的含义=F1 证据反映的是"对称化前"状态**（记录用）。

## f4_数值T7对照/
- `stats_before.png`/`stats_before_zoom.png`：攻击前（场卡数值圈 25/7 白）
- `stats_after_attack.png`/`stats_after_attack_zoom.png`：攻击后（18 红/1 红——涨红+实时）
- `zoom_player_card.png`/`zoom_enemy_card.png`：face 直显+数值覆盖层对齐精查（续4 修正后：无双数字=对齐 ✓）
- **验证工具**（副本）：check_slot_overlay.gd（F1）/check_board_stats.gd（F2+T7）/battle_target_e2e.gd（F3 T1-T6）——正常位置=D:\warpforge\tools_dev\（零写入）。

## 已验证结论（勿重查）
- 回归全绿：battle_e2e 7✓/check_board_stats 9✓/battle_target_e2e T1-T6✓/rule_test 411-414/0（rule_core 零改动）
- 备份：D:\2\Warpforge备份\0828_battle_opt_base\=F0 基线（battle.gd/sd_card.gd/battle_e2e.gd/shot_battle.gd 改动前）——保留至用户 F5 目视后
- 探针脚本：D:\2\Warpforge_tools\scripts\probe_face_stats.py（face 数值图标像素位置——色彩探测会被立绘污染，仅无干扰卡可用）
