extends SceneTree
## F2 动态数值验证 (2026-08-28): 部署后场卡数值=当前值; AI 攻击后数值自动变化; 玩家攻击后敌数值变化
## 运行: Godot_v4.7.2-stable_win64.exe --path D:/warpforge --resolution 1920x1080 \
##       --script res://tools_dev/check_board_stats.gd --log-file <log>

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

## 读场卡 health 数值 Label 文本 ("no-card"/"no-label"=异常)
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

func _snap(fname: String) -> void:
	var img: Image = root.get_viewport().get_texture().get_image()
	img.save_png(ProjectSettings.globalize_path("res://tools_dev/out_target/" + fname))
	print("[snap] " + fname)

func _run() -> void:
	print("=== F2 动态数值验证 ===")
	if root.get_node_or_null("GameData") == null:
		root.add_child(load("res://autoload/game_data.gd").new())
	if root.get_node_or_null("SFX") == null:
		root.add_child(load("res://autoload/sfx.gd").new())
	await _wait(30)
	print("[f2x] step0 autoload ok")
	root.get_node("GameData").pending_mode = "tutorial"
	root.get_node("GameData").tutorial_stage = 0
	var b: Control = load("res://scenes/battle.tscn").instantiate()
	root.add_child(b)
	await _wait(150)
	print("[f2x] step1 battle ready")
	var ctx: Dictionary = b.get("_ctx")
	_ck(ctx["players"][0]["hand"].size() > 0, "M0 起手有牌")
	_click(Vector2(1352, 953))
	await _wait(60)
	ctx = b.get("_ctx")
	# 注入: 手牌=TestTroop hp25; 能量3
	var h0: Array = ctx["players"][0]["hand"]
	h0.clear()
	h0.append({"name": "TestTroop", "type": "unit", "cost": 1, "attack": 3, "health": 25,
		"armor": 0, "keywords": [], "desc": "", "ranged_attack": 0})
	ctx["players"][0]["energy"] = 3
	ctx["players"][0]["max_energy"] = 3
	b.call("_refresh_hand", -1, 950.0)
	b.call("_refresh_energy")
	await _wait(10)
	var hand: Array = ctx["players"][0]["hand"]
	var hx := 960.0
	await _drag(Vector2(hx, 950), Vector2(834, 708))
	await _wait(60)
	ctx = b.get("_ctx")
	var b0: Array = ctx["players"][0]["board"]
	_ck((b0[3] as Dictionary) != null, "D1 部署到槽3")
	_ck(_hdl(b, "0:3") == "25", "S1 部署后场卡显示当前值 (label='%s')" % _hdl(b, "0:3"))
	# 敌白盒单位 (hp 7, atk 2)
	var eb: Array = ctx["players"][1]["board"]
	eb[3] = RuleCore._make_unit({"name": "TargetDummy", "type": "unit", "cost": 0,
		"attack": 2, "health": 7, "armor": 0, "keywords": [], "desc": ""}, false)
	eb[3]["exhausted"] = false
	b.call("_refresh_board")
	await _wait(20)
	_ck(_hdl(b, "1:3") == "7", "S2 敌场卡显示当前值 (label='%s')" % _hdl(b, "1:3"))
	_snap("stats_before.png")   # T7: 未受伤前 (数值圈 25/7)
	# END TURN → AI → 回 (AI 会攻击我方单位; F3 演出=0.45s/步, 轮询等待)
	_click(Vector2(1848, 455))
	var back_ok := false
	for i in 240:
		await _wait(12)
		ctx = b.get("_ctx")
		if (not ctx.is_empty()) and int(ctx["active"]) == 0:
			back_ok = true
			break
	_ck(back_ok, "S3pre AI 回合后回到玩家")
	ctx = b.get("_ctx")
	var u3: Variant = ctx["players"][0]["board"][3]
	var cur_hp := -1 if u3 == null else RuleCore._to_int((u3 as Dictionary)["health"])
	_ck(cur_hp >= 0 and cur_hp < 25, "S3 AI 回合我方受伤 hp=%d" % cur_hp)
	_ck(_hdl(b, "0:3") == str(cur_hp), "S4 AI 后场卡数值自动更新 (label='%s' == %d)" % [_hdl(b, "0:3"), cur_hp])
	# 长按→攻击→点敌: 敌被反击后数值变化 (玩家攻击 3, 敌 hp 7-3=4; 再被... 反击看 rule_core)
	var e3: Variant = ctx["players"][1]["board"][3]
	var ehp0 := -1 if e3 == null else RuleCore._to_int((e3 as Dictionary)["health"])
	# 长按我方 (834,708) → 0.35s → 点敌 (834,466)
	var pe := InputEventMouseButton.new()
	pe.button_index = MOUSE_BUTTON_LEFT
	pe.pressed = true
	pe.position = Vector2(834, 708)
	pe.global_position = Vector2(834, 708)
	root.push_input(pe)
	await create_timer(0.35).timeout
	var mo := InputEventMouseMotion.new()
	mo.position = Vector2(834, 720)
	mo.global_position = Vector2(834, 720)
	mo.relative = Vector2(0, 12)
	root.push_input(mo)
	await _wait(4)
	var rel := InputEventMouseButton.new()
	rel.button_index = MOUSE_BUTTON_LEFT
	rel.pressed = false
	rel.position = Vector2(834, 720)
	rel.global_position = Vector2(834, 720)
	root.push_input(rel)
	await _wait(20)
	_click(Vector2(834, 466))
	await _wait(60)
	ctx = b.get("_ctx")
	var e3b: Variant = ctx["players"][1]["board"][3]
	var ehp1 := -1 if e3b == null else RuleCore._to_int((e3b as Dictionary)["health"])
	_ck(ehp1 >= 0 and ehp1 < ehp0, "S5 攻击后敌方掉血 %d→%d" % [ehp0, ehp1])
	_ck(_hdl(b, "1:3") == str(ehp1), "S6 敌方数值圈更新 (label='%s' == %d)" % [_hdl(b, "1:3"), ehp1])
	_snap("stats_after_attack.png")   # T7: 攻击后 (数值圈 21→/敌 1)
	print("=== F2 done: %d 失败 ===" % _fails)
	quit(1 if _fails > 0 else 0)
