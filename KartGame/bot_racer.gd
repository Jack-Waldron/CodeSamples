###############################################################################
# These GDScript samples reference the basic vehicle character prefab and pair 
# of associated character controllers for a WIP "kart racing game" personal 
# project in Godot. This file serves as the computer-automated kart character 
# controller.
###############################################################################

extends Node3D

# Main references
@onready var kartRacer: RigidBody3D = $DefaultKartRacer
@onready var navigationAgent: NavigationAgent3D = $DefaultKartRacer/NavigationAgent3D
var gm: Node

# Basic mesh objects used to debug pathfinding logic
@onready var marker: MeshInstance3D = $DefaultKartRacer/Marker
@onready var tdMarker: MeshInstance3D = $DefaultKartRacer/TargetDirMarker

# Debug flag used to manually control AI state
var active = false

# Physics/steering properties
var angAcc # Mirrors default kart's angAcc value
# (Thresholds inform when a kart should steer to stay on target)
var rotDifMaxThreshold = 0.2 
var rotDifMinThreshold = 0.05
var turningLinVel = 5 # Max linear speed kart should go when turning

# Behavior state properties
var inDriveForwardBox = false # Overwrites path following to drive straight
var isSeekingItem = false # Overwrites path following to follow item box
var itemBoxTarget: Node # Item box that isSeekingItem flag references
var isFinished = false # Used to ensure following stops on race finish

# Local copies of current pathfinding target and path following direction
var targetPos
var targetDir

# -----------------------------------------------------------------------------

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	gm = get_node_or_null("../GameManager")	
	_nav_setup.call_deferred()

# Initializes pathfinding/pathfollowing data
# (Needs to wait for scene tree/physics server to fully init)
func _nav_setup():
	await get_tree().physics_frame
	
	if(gm != null):
		targetPos = gm.call("_get_curr_checkpoint", kartRacer.id)
	if(targetPos != null):
		navigationAgent.set_target_position(targetPos)
	
	angAcc = deg_to_rad(kartRacer.get("angAcc"))

# Used to configure debug display meshes for generated paths
func _set_point(mesh: MeshInstance3D, pos: Vector3):
	mesh.global_position = pos

# Updates current pathfinding target to relevant checkpoint
# (Called on checkpoint trigger and after leaving drive forward boxes)
func _update_target_pos():
	targetPos = gm.call("_get_curr_checkpoint", kartRacer.id)
	if(targetPos != null):
		navigationAgent.set_target_position(targetPos)
	navigationAgent.get_next_path_position()

func _physics_process(delta: float) -> void:
	# DEBUG; currently allows for bot logic to be manually started
	if Input.is_action_pressed("Drift"):
		active = true
	if !active:
		return
		
	# DEBUG; Prints box meshes to mark generated path
	#if Input.is_action_just_pressed("UseItem"):
		#navigationAgent.get_next_path_position()
		#var arr = navigationAgent.get_current_navigation_path()
		#print(arr)
		#for point in arr:
		#	var box = MeshInstance3D.new()
		#	box.mesh = BoxMesh.new()
		#	add_child(box)
		#	_set_point.call_deferred(box, point)

	# DEBUG; Prints current path subtarget
	#if Input.is_action_just_pressed("Reroll"):
		#print(targetDir.y)
		#print(navigationAgent.get_next_path_position())
	
	# Regardless of navigation state, stop moving on race completion
	if isFinished:
		kartRacer.stateAcc = false
		kartRacer.stateBrake = false
		kartRacer.stateLeft = false
		kartRacer.stateRight = false
		return;
	
	# Move directly forward when driving through marked triggers (ignores pathfinding)
	if inDriveForwardBox:
		targetPos = null
		_drive_towards_target(delta)
		return
	
	# Seek nearby item box if current behavior calls for it
	if isSeekingItem: 
		# (Item box should update targetPos to itself and later clear seeking behavior)
		targetDir = kartRacer.global_position.direction_to(itemBoxTarget.global_position)
		_drive_towards_target(delta)
		return
		
	# Use checkpoint pathfinding target to inform where kart should drive and steer
	if !navigationAgent.is_navigation_finished():
		# Updates what the current next path subtarget is
		var current_agent_position: Vector3 = kartRacer.global_position
		var next_path_position: Vector3 = navigationAgent.get_next_path_position()
		marker.global_position = next_path_position

		# Direction for kart to follow/adjust steering to (XZ plane projection)
		targetDir = current_agent_position.direction_to(next_path_position)
		# (Regenerates path if mistakenly trying to follow a point directly above/below)
		if(abs(targetDir.y) > .8):
			_update_target_pos()
			targetDir = current_agent_position.direction_to(navigationAgent.get_next_path_position())
		
		_drive_towards_target(delta)
	elif targetPos != null: # Kart has a target, but can't move anymore 
		# Checkpoints and other object should update kart pathfinding target
		#print("Error: done with pathfinding, but not done with race")
	else: # Done with pathfinding completely
		kartRacer.stateAcc = false
		kartRacer.stateBrake = false
		kartRacer.stateLeft = false
		kartRacer.stateRight = false

func _drive_towards_target(delta: float):
	# Adjusts debug direction mesh
	tdMarker.global_position = kartRacer.global_position + targetDir

	# Fully projects onto XZ plane, then gets angular offset from kart look direction
	targetDir.y = 0
	var rotDif = (-kartRacer.transform.basis.z).signed_angle_to(targetDir, Vector3.UP)
	
	if(rotDif > rotDifMaxThreshold): # Target is roughly <10 degrees to the left
		# Simulate next frame's ang position based on current speed
		var angVel = deg_to_rad(kartRacer.get("angVel"))
		var simulated = rotDif - ((angVel - (angAcc * delta)) * delta)
		
		if(simulated <= rotDifMinThreshold): # Will current angVel correct difference in dir
			if(simulated < -rotDifMaxThreshold): # Will overcorrect; need to countersteer to right
				kartRacer.stateLeft = false
				kartRacer.stateRight = true
			else: # Do nothing (as in let off input)
				kartRacer.stateLeft = false
				kartRacer.stateRight = false
		else: # Current isn't enough; steer to the left
			kartRacer.stateLeft = true
			kartRacer.stateRight = false
		
		# Adjust linear vel since we're turning
		if(kartRacer.get("vel") > turningLinVel):
			kartRacer.stateAcc = false
		else:
			kartRacer.stateAcc = true
	elif(rotDif < -rotDifMaxThreshold): # Target is roughly <10 degrees to the right
		# Simulate next frame's ang position based on current speed
		var angVel = deg_to_rad(kartRacer.get("angVel"))
		var simulated = rotDif + ((angVel - (angAcc * delta)) * delta)
		
		if(simulated >= -rotDifMinThreshold): # Will current angVel correct difference in dir
			if(simulated > rotDifMaxThreshold): # Will overcorrect; need to countersteer to left
				kartRacer.stateLeft = true
				kartRacer.stateRight = false
			else: # Do nothing (as in let off input)
				kartRacer.stateLeft = false
				kartRacer.stateRight = false
		else: # Current isn't enough; steer to the right
			kartRacer.stateLeft = false
			kartRacer.stateRight = true
		
		# Adjust linear vel since we're turning
		if(kartRacer.get("vel") > turningLinVel):
			kartRacer.stateAcc = false
		else:
			kartRacer.stateAcc = true
	else: # Difference in direction is tolerable; go straight
		kartRacer.stateLeft = false
		kartRacer.stateRight = false
		kartRacer.stateAcc = true

# External checkpoint manager disables AI behavior once kart has properly completed race
func _on_game_manager_race_finished(kart_id: int) -> void:
	if(kart_id == kartRacer.get("id")):
		#print(str(kart_id) + " finished")
		isFinished = true

# Called by item boxes on pickup; restores AI back to normal behaviors
func _clear_item_target() -> void:
	isSeekingItem = false
	itemBoxTarget = null
	_update_target_pos()
