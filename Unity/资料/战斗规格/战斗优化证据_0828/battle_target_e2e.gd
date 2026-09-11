extends SceneTree
## F3/F4 目标选择链路 e2e (2026-08-28): T1 战术拖掷 / T2 tactic 态补点+取消 / T3 pending_fx /
## T4 cost_act 弹窗 / T5 choose_one 弹窗 / T6 AI 打死督军即时弹门
## 运行: Godot_v4.7.2-stable_win64.exe --path D:/warpforge --resolution 1920x1080 \
##       --script res://tools_dev/battle_target_e2e.gd --log-file <log>

var _fails := 0

func _initialize() -> void:
	await process_frame
	_run()

func _wait(n: int) -> void:
	for i in n:
		await process_frame
	await create_timer(0.02).timeout

func _click(p: Vector2) -> void:
	var e := InputEventMouseButton.new()
	e.button_index = MOUSE_BUTTON_LEFT
	e.pressed = true
	e.position = p
	e.global_position = p
	root.push_input(e)
	e.pressed = false
	root.push_input(e)

func _rclick(p: Vector2) -> void:
	var e := InputEventMouseButton.new()
	e.button_index = MOUSE_BUTTON_RIGHT
	e.pressed = true
	e.position = p
	e.global_position = p
	root.push_input(e)
	e.pressed = false
	root.push_input(e)

func _drag(from: Vector2, to: Vector2) -> void:
	var press := InputEventMouseButton.new()
	press.button_index = MOUSE_BUTTON_LEFT
	press.pressed = true
	press.position = from
	press.global_position = from
	root.push_input(press)
	await _wait(6)
	var mo := InputEventMouseMotion.new()
	mo.position = to
	mo.global_position = to
	mo.relative = to - from
	root.push_input(mo)
	await _wait(6)
	var rel := InputEventMouseButton.new()
	rel.button_index = MOUSE_BUTTON_LEFT
	rel.pressed = false
	rel.position = to
	rel.global_position = to
	root.push_input(rel)

func _ck(cond: bool, label: String) -> void:
	print(("  OK: " if cond else "  FAIL: ") + label)
	if not cond:
		_fails += 1

func _slot(b: Node, p: int, i: int) -> Vector2:
	return b.call("_slot_center", p, i)

func _hdl(b: Node, key: String) -> String:
	var refs: Variant = b.get("_board_refs")
	if not (refs is Dictionary):
		return "no-refs"
	var card: Variant = (refs as Dictionary).get(key)
	if card == null:
		return "no-card"
	var labels: Variant = (card as Node).get("_num_labels")
	if not (labels is Dictionary):
		return "no-labels"
	var lb: Variant = (labels as Dictionary).get("health")
	if lb == null:
		return "no-label"
	return (lb as Label).text

func _state(b: Node) -> String:
	return str(b.get("_state"))

func _run() -> void:
	print("=== F3 目标选择链路 e2e ===")
	if root.get_node_or_null("GameData") == null:
		root.add_child(load("res://autoload/game_data.gd").new())
	if root.get_node_or_null("SFX") == null:
		root.add_child(load("res://autoload/sfx.gd").new())
	await _wait(30)
	root.get_node("GameData").pending_mode = "tutorial"
	root.get_node("GameData").tutorial_stage = 0
	var b: Control = load("res://scenes/battle.tscn").instantiate()
	root.add_child(b)
	await _wait(150)
	var ctx: Dictionary = b.get("_ctx")
	_ck(ctx["players"][0]["hand"].size() > 0, "M0 起手有牌")
	_click(Vector2(1352, 953))
	await _wait(60)
	ctx = b.get("_ctx")
	b.call("_refresh_hand", -1, 950.0)
	b.call("_refresh_energy")
	# 敌白盒单位 (hp 7 无武装靶标)
	var eb: Array = ctx["players"][1]["board"]
	eb[3] = RuleCore._make_unit({"name": "TargetDummy", "type": "unit", "cost": 0,
		"attack": 2, "health": 7, "armor": 0, "keywords": [], "desc": ""}, false)
	eb[3]["exhausted"] = false
	b.call("_refresh_board")
	await _wait(10)

	# ======== T1: 带目标战术拖掷 ========
	print("--- T1 战术拖掷 ---")
	var h0: Array = ctx["players"][0]["hand"]
	h0.clear()
	h0.append({"name": "TestShot", "type": "tactic", "cost": 1, "attack": 0, "health": 0,
		"armor": 0, "keywords": [], "desc": "Deal 3 damage to an enemy unit", "ranged_attack": 0})
	ctx["players"][0]["energy"] = 3
	ctx["players"][0]["max_energy"] = 3
	b.call("_refresh_hand", -1, 950.0)
	b.call("_refresh_energy")
	await _wait(10)
	var hx := 960.0
	var e3p := _slot(b, 1, 3)
	await _drag(Vector2(hx, 950), e3p)
	await _wait(60)
	ctx = b.get("_ctx")
	var e3: Variant = ctx["players"][1]["board"][3]
	var ehp := -1 if e3 == null else RuleCore._to_int((e3 as Dictionary)["health"])
	_ck(ehp == 4, "T1 战术伤害生效 7→%d" % ehp)
	_ck(_state(b) == "idle", "T1 结算后 state=idle (%s)" % _state(b))
	_ck(_hdl(b, "1:3") == "4", "T1 敌数值圈联动=4 (%s)" % _hdl(b, "1:3"))

	# ======== T2a: 拖掷未命中→tactic 态→补点敌========
	print("--- T2a tactic 态补点 ---")
	h0.clear()
	h0.append({"name": "TestShot2", "type": "tactic", "cost": 1, "attack": 0, "health": 0,
		"armor": 0, "keywords": [], "desc": "Deal 2 damage to an enemy unit", "ranged_attack": 0})
	ctx["players"][0]["energy"] = 3
	b.call("_refresh_hand", -1, 950.0)
	await _wait(10)
	await _drag(Vector2(960.0, 950), Vector2(960.0, 200.0))   # 拖到空场(未命中槽)
	await _wait(30)
	_ck(_state(b) == "tactic", "T2a 拖空场进 tactic 态 (%s)" % _state(b))
	ctx = b.get("_ctx")
	_ck(ctx["players"][0]["hand"].size() == 1, "T2a 卡未打出 (hand=1)")
	_click(e3p)   # 点敌补目标
	await _wait(60)
	ctx = b.get("_ctx")
	var e3b: Variant = ctx["players"][1]["board"][3]
	var ehp2 := -1 if e3b == null else RuleCore._to_int((e3b as Dictionary)["health"])
	_ck(ehp2 == 2, "T2a 补点后生效 4→%d" % ehp2)
	_ck(_state(b) == "idle", "T2a 结算后 idle (%s)" % _state(b))

	# ======== T2b: 右键取消 ========
	print("--- T2b 右键取消 ---")
	h0.clear()
	h0.append({"name": "TestShot3", "type": "tactic", "cost": 1, "attack": 0, "health": 0,
		"armor": 0, "keywords": [], "desc": "Deal 2 damage to an enemy unit", "ranged_attack": 0})
	ctx["players"][0]["energy"] = 3
	b.call("_refresh_hand", -1, 950.0)
	await _wait(10)
	await _drag(Vector2(960.0, 950), Vector2(960.0, 200.0))
	await _wait(30)
	_ck(_state(b) == "tactic", "T2b 进 tactic 态 (%s)" % _state(b))
	_rclick(Vector2(960.0, 200.0))
	await _wait(30)
	_ck(_state(b) == "idle", "T2b 右键取消→idle (%s)" % _state(b))
	var hand_n: int = (ctx["players"][0]["hand"] as Array).size()
	_ck(hand_n == 1, "T2b 取消后手牌不变 (%d)" % hand_n)

	# ======== T3: pending_fx 挂起点选 (Rally 单位) ========
	print("--- T3 pending_fx ---")
	h0.clear()
	h0.append({"name": "TestRally", "type": "unit", "cost": 1, "attack": 1, "health": 3,
		"armor": 0, "keywords": [], "desc": "Rally: Deal 2 damage to an enemy", "ranged_attack": 0})
	ctx["players"][0]["energy"] = 3
	b.call("_refresh_hand", -1, 950.0)
	await _wait(10)
	await _drag(Vector2(960.0, 950), _slot(b, 0, 3))
	await _wait(60)
	_ck(_state(b) == "pending_fx", "T3 部署 Rally 单位→pending_fx (%s)" % _state(b))
	_click(e3p)
	await _wait(60)
	ctx = b.get("_ctx")
	var e3c: Variant = ctx["players"][1]["board"][3]
	_ck(e3c == null, "T3 Rally 2 伤击杀敌方 (hp 2→0 阵亡)")
	_ck(_state(b) == "idle", "T3 结算后 idle (%s)" % _state(b))
	_ck(RuleCore.pending_fx_need(ctx) == "none", "T3 挂起清空")

	# ======== T4: cost_act 弹窗 ========
	print("--- T4 cost_act ---")
	eb[4] = RuleCore._make_unit({"name": "TargetDummy2", "type": "unit", "cost": 0,
		"attack": 0, "health": 9, "armor": 0, "keywords": [], "desc": ""}, false)
	eb[4]["exhausted"] = true
	b.call("_refresh_board")
	await _wait(20)
	h0.clear()
	h0.append({"name": "TestAct", "type": "tactic", "cost": 1, "attack": 0, "health": 0,
		"armor": 0, "keywords": [], "desc": "Deal 1 damage to an enemy unit. 1 [Energy]: Draw 1 card",
		"ranged_attack": 0})
	ctx["players"][0]["energy"] = 3
	b.call("_refresh_hand", -1, 950.0)
	b.call("_refresh_energy")
	await _wait(10)
	var e4p := _slot(b, 1, 4)
	await _drag(Vector2(960.0, 950), e4p)
	await _wait(60)
	ctx = b.get("_ctx")
	var pop: Variant = b.get("_energy_popup")
	_ck(pop != null and (pop as Control).visible, "T4 bounce: 弹窗 visible")
	# 卡片式: Activate 绿钮 @(860..1180, 950..1010) 中心 (1020,980)
	_click(Vector2(1020, 980))
	await _wait(60)
	ctx = b.get("_ctx")
	var e4: Variant = ctx["players"][1]["board"][4]
	var ehp4 := -1 if e4 == null else RuleCore._to_int((e4 as Dictionary)["health"])
	_ck(ehp4 == 8, "T4 Activate 基础伤害 9→%d" % ehp4)
	_ck(RuleCore._to_int((ctx["players"][0] as Dictionary)["energy"]) == 1, "T4 能量 -2 (出牌1+激活1) (%d)" %
		RuleCore._to_int((ctx["players"][0] as Dictionary)["energy"]))
	_ck((ctx["players"][0]["hand"] as Array).size() == 1, "T4 激活 Draw 1 生效 (hand=%d)" %
		(ctx["players"][0]["hand"] as Array).size())

	# ======== T5: choose_one 弹窗 ========
	print("--- T5 choose_one ---")
	h0.clear()
	h0.append({"name": "TestChoice", "type": "tactic", "cost": 1, "attack": 0, "health": 0,
		"armor": 0, "keywords": [], "desc": "Choose one: Draw 2 cards; Gain 2 armor",
		"ranged_attack": 0})
	ctx["players"][0]["energy"] = 3
	b.call("_refresh_hand", -1, 950.0)
	await _wait(10)
	await _drag(Vector2(960.0, 950), _slot(b, 0, 3))
	await _wait(60)
	ctx = b.get("_ctx")
	print("[T5dx] cost_act=%s choose_one=%s cpop=%s state=%s" % [
		str(ctx.has("pending_cost_act")), str(ctx.has("pending_choose_one")),
		str(b.get("_chooseone_popup")), _state(b)])
	var pop2: Variant = b.get("_chooseone_popup")
	_ck(pop2 != null and (pop2 as Control).visible, "T5 弹窗 visible")
	# 卡片式候选: 选项0 x=960-160=800 → slot(665,180)+pick 中心 (800,440)
	_click(Vector2(800, 440))
	await _wait(60)
	ctx = b.get("_ctx")
	var hand2: int = (ctx["players"][0]["hand"] as Array).size()
	_ck(hand2 >= 2, "T5 选 Draw 后手牌 %d" % hand2)

	# ======== T6: AI 打死玩家督军即时弹门 ========
	print("--- T6 AI 击杀即时弹门 ---")
	# 玩家督军 hp=1; 敌 5 攻单位就位 (exhausted=false)
	ctx["players"][0]["warlord"]["health"] = 1
	eb[5] = RuleCore._make_unit({"name": "BigBrute", "type": "unit", "cost": 0,
		"attack": 5, "health": 5, "armor": 0, "keywords": [], "desc": ""}, false)
	eb[5]["exhausted"] = false
	b.call("_refresh_board")
	await _wait(20)
	_click(Vector2(1848, 455))   # END TURN
	var winner_seen := 0
	for i in 120:   # AI 演出 0.45s/步 — 最多 ~10 步内应判胜负/弹门
		await _wait(12)
		ctx = b.get("_ctx")
		if not ctx.is_empty() and int(ctx.get("winner", 0)) != 0:
			winner_seen = int(ctx.get("winner", 0))
			break
	_ck(winner_seen != 0, "T6 AI 回合内胜负即时判定 (winner=%d, 无需再按 END TURN)" % winner_seen)

	print("=== F3 target e2e done: %d 失败 ===" % _fails)
	quit(1 if _fails > 0 else 0)
