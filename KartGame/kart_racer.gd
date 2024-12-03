################################################################################
# This file serves as the basic kart character implementation used by players 
# and bots alike.
################################################################################

extends RigidBody3D

# General Game Info 
var id = 0
@export var player: bool = false

# Player/AI Input states
var stateAcc = false
var stateBrake = false
var stateLeft = false
var stateRight = false
var stateDrift = false
var stateItemUse = false

# Custom Physics Vars
var vel = 0.0
var maxVel = 30
var acc = 20.0 # Const forward/back acc value (curr also used for damping)
var angVel = 0.0
var maxAngVel = 70
var angAcc = 120

# Respawn Logic
var respawnPos # Global position where kart should be respawned at
var respawnRot # Rotation degrees that kart should be respawned with

# Item Vars
enum ItemType 
{
	BOOST,
	PROJ_WEAPON,
	INVINCIBILITY,
	TRAP
}
var heldItem = null
var rng
var boosting: bool = false
var invincible: bool = false

# Item Prefabs/References
@onready var boostTimer: Timer = $BoostTimer
@onready var invTimer: Timer = $InvincibilityTimer
@onready var pfItemProjectile = preload("res://prefabs/items/item_projectile.tscn")
@onready var pfItemTrap = preload("res://prefabs/items/item_trap.tscn")

# General Functions -----------------------------------------------------------
# -----------------------------------------------------------------------------
func _ready() -> void:
	respawnPos = global_position
	respawnRot = rotation_degrees
	
	# Item box result rng determined within each kart
	rng = RandomNumberGenerator.new() 
	rng.randomize()

func _physics_process(delta: float) -> void:
	# Adjust linear velocity along kart's forward direction
	if boosting:
		vel = 40
	else:
		if stateAcc:
			vel = min(vel + (acc * delta), maxVel)
		elif stateBrake:
			vel = max(vel - (acc * delta), -maxVel)
		else: # Gradually reduce speed to 0
			if vel > 0:
				vel = max(vel - (acc * delta), 0.0)
			else:
				vel = min(vel + (acc * delta), 0.0)
			
	# Adjust linear position along kart's forward direction
	position -= transform.basis.z * vel * delta
	
	# Adjust angular velocity
	if stateLeft:
		angVel = max(angVel - (angAcc * delta), -maxAngVel)
	elif stateRight:
		angVel = min(angVel + (angAcc * delta), maxAngVel)
	else:
		if angVel < 0:
			angVel = min(angVel + (angAcc * delta), 0.0)
		else:
			angVel = max(angVel - (angAcc * delta), 0.0)
			
	# Adjust angular movement
	rotation_degrees -= Vector3(0, 1, 0) * angVel * delta
	
	# Use whatever item is currently held
	if stateItemUse:
		if(heldItem != null):
			_spawn_item()

# Hazardous item collision resolution (knocks kart into the air, destroys item)
func _on_item_hurtbox_body_entered(body: Node3D) -> void:
	if !invincible and body.is_in_group("itemhazardobject"):
		apply_impulse(Vector3(0,12,0))
		vel = 0
		body.queue_free()
	
func _get_id() -> int:
	return id

# Item Functions ---------------------------------------------------------------
# ------------------------------------------------------------------------------
# Randomly determine which item is yielded from an item box pickup
func _gain_item():
	heldItem = rng.randi_range(0, ItemType.size() - 1)
	
	if player: # If this kart is a player, update HUD portrait
		get_parent().call("_update_item_portrait", heldItem)

# Creates hazard item object/applies item properties
# (Currently, item distribution is completely equal; this will likely change
# to reflect current race position/performance in the future)
func _spawn_item(): # (Protection against null item call is in physics_process)
	match heldItem:
		ItemType.BOOST:
			boosting = true
			boostTimer.start()
		ItemType.PROJ_WEAPON:
			var inst = pfItemProjectile.instantiate()
			inst.transform.origin = position + (transform.basis.y * 2)
			inst.linear_velocity = -global_transform.basis.z * 33
			add_sibling(inst)
		ItemType.INVINCIBILITY:
			var body: MeshInstance3D = get_node("Chassis")
			body.get_active_material(0).set("albedo_color", Color(0, 0, 0, 1))
			invincible = true
			invTimer.start()
		ItemType.TRAP: #WIP
			var inst = pfItemTrap.instantiate()
			inst.transform.origin = position 
			add_sibling(inst)
			
	heldItem = null
	if player:
		get_parent()._update_item_portrait(-1) # Clears current item portrait

# Ends boost item effect
func _on_boost_timer_timeout() -> void:
	boosting = false
	boostTimer.stop()

# Ends invincibility item effect
func _on_invincibility_timer_timeout() -> void:
	var body: MeshInstance3D = get_node("Chassis")
	body.get_active_material(0).set("albedo_color", Color(1, 1, 1, 1))
	invincible = false
	invTimer.stop()
	
# Respawn Functions ------------------------------------------------------------
# ------------------------------------------------------------------------------
func _respawn_kart() -> void:
	#print("kart " + str(id) + " respawned") # Debug info
	
	# Reset physics
	vel = 0
	angVel = 0
	
	# Set respawn properties
	global_position = respawnPos
	rotation_degrees = respawnRot

# Called by checkpoint triggers
func _update_respawn(newPos: Vector3, newRot: Vector3) -> void:
	respawnPos = newPos
	respawnRot = newRot
