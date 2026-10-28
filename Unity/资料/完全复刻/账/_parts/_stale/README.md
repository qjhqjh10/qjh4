# _stale —— 过期补丁（⛔ 永远不要合并）

## B92_B1_过期_勿合并.tsv（2026-10-28 重同步点归档）
这是 B1 第一轮的原始补丁。它的 10 行里：
- 5 行与主表**逐字相同**（no-op）
- **5 行会覆盖主表里更新的内容** —— `BroadcastActivateMinions` / `CheckUnitsCancellingAttack` /
  `ResolveAction` / `ResolveFollowAfterResolution` / `UpdateManaLeftAccumulation`
  这 5 行已被后续 `B92_B1b/B1c/B1d` 更新过 ⇒ 合并这份 = **用旧内容盖掉新内容，零报错**。

⇒ 保留作证据，**⛔ 不要合并**。
