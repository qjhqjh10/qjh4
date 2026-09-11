# RuleCore 对战 API 手册(2026-08-27 重建设计·Step 0 产物)

> battle.gd(UI 层)↔ rule_core.gd(规则核,4708 行,静态函数直调)。规则核=纯逻辑同步函数;UI 动画由 battle.gd await/创建 tween 编排。
> 基线:rule_test **414/0**(2026-08-27 复跑 ✓)。

## 一、对局生命线

```
ctx = RuleCore.new_battle(deck_p, deck_e, mode, shuffle=true)   # 洗牌+起手(3/4 张)+督军入中格
#     （后手防御卡=UI 层职责:依据 GameData 防御卡池(宝石 special 39 张)给后手补插 —— 见旧 battle.gd _ai_defensive_name 语义,数值勿抄）
牌局循环(active 方):
  RuleCore.begin_turn(ctx)            # 能量+1(上限10)/抽1/回合效果/清挡位标记
  [玩家] play_card / activate_alt / declare_attack ...(任一操作后: ctx["last_events"] 被写入 → UI 消费)
  RuleCore.end_turn(ctx)              # 回合结束效果/消耗消失/换 active
  win = RuleCore.check_winner(ctx)    # 0=继续 1/2=某方胜 3=平(同死)
起步:
  RuleCore.mulligan(ctx, p, replace_idx: Array)   # 换牌(替换索引数组)
```

## 二、操作 API(返回 ERR_* 码,0=OK)

| 函数 | 签名 | 说明 |
|---|---|---|
| play_card | `(ctx, p, hand_idx, slot, tgt_p=-1, tgt_slot=-1)` | 单位→落格;战术→结算即弃(slot 忽略);效果需目标→tgt_p/slot;pick 类=进 pending |
| activate_alt | `(ctx, p, slot)` | 激活技能(单位 Alt) |
| declare_attack | `(ctx, p, atk_slot, target_p, tgt_slot, is_ranged=false)` | 近战互反击/远程无反击;Pindown/Blind/BloodThirst/Concussion 等全部内置 |
| field_attack | `(ctx, p, u, is_ranged)` | 场上攻击力(含 Pack/临时增益) |
| mulligan | `(ctx, p, replace_idx)` | 换牌 |
| begin_turn / end_turn | `(ctx)` | 回合引擎 |
| check_winner | `(ctx)` | 0/1/2/3 |
| pending_choose_cands | `(ctx)` | 选卡候选(ChooseCardMenu 用) |
| resolve_pending_choose | `(ctx, idx)` | 确认选卡 |
| resolve_choose_one | `(ctx, p, idx)` | "Choose one" 分支(卡面三选一) |
| pending_fx_need | `(ctx)` | 挂起目标需求类型(弹选目标) |
| resolve_pending_fx | `(ctx, tgt_p, tgt_slot)` | 玩家点选目标后结算 |

**错误码**:ERR_BAD_HAND/ERR_COST/ERR_SLOT/ERR_NOT_TURN/ERR_NOT_UNIT/ERR_EXHAUSTED/ERR_NO_ATTACK/ERR_SELF/ERR_STUNNED/ERR_TARGET/ERR_PINDOWN(=11 个常数,rule_core.gd:9-33)。

## 三、ctx 结构(只读+last_events 消费)

```
ctx = {
  mode, turn, turn_p:[p0,p1], active, winner,
  last_events: [String...],   # 规则文本事件(操作后读取)=VFX 解析源(_fx_from_event)
  ui_events:   [ {t:"draw",p,card}, ... ],  # UI 专用结构化事件(当前=抽卡;battle.gd _drain_ui_events 清空消费)
  players: [ {energy,max_energy,faith,skulls,fatigue,deck,hand,discard,
              board:[9格(unit dict|null)], warlord,drawn_cards,quest,spirit_stones}, ×2 ]
}
board 格=unit dict: {attack,ranged_attack,health,max_health,armor,cost,type,name,exhausted,stun,
                     attacks_turn,kws:{kw:x},is_warlord, ...temp buffs}
```

## 四、UI 层职责(rule_core 不管的)

1. **后手防御卡**:开局后手方(主动=0 先手)→ 从阵营宝石 special 卡池补 1 张入后手(原版规则 L45;UEMC 数据=defensive_cards_39.json/gems_rarity.json special)
2. **回合计时**(60s,数据资产 10/15)→ 到时强制 end_turn(原版 TurnTime 行为)
3. **事件→VFX 映射**(_fx_from_event 文本解析: deployed/destroyed/played tactic/gained/triggered 等关键词+卡名→VFX 名表=旧 battle.gd 台账,只抄映射)
4. **AI**(单机自创;原版=联网无 AI)
5. **特效/动画**(抽卡飞行/攻击/爆散/能量积攒)
6. **弹出选目标/选卡 UI**(pending_* 消费)

## 五、已知坑(接口层)

- `ctx["ui_events"]` 消费后要**清空**(旧实现 `_ctx["ui_events"] = []`)
- `_fx_from_event` 文本解析=大小写敏感→先 to_lower;中英偏移坑(CLAUDE.md UI 语言准则:事件文本同步解析)
- `int(null)` 报错——数值一律过 `RuleCore._to_int`
- 战术打出的 VFX 部署动画= `_play_deploy_flight` 式(从手牌飞到场上),由 play_card OK 后触发
