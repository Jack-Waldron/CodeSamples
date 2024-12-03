################################################################################
# This file serves as the player-controlled kart character controller.
################################################################################

extends Node3D

# Core References/Properties
@onready var kartRacer: RigidBody3D = $DefaultKartRacer
var localCash: int

# Player HUD UI Elements
@onready var lapLabel: Label = $DefaultKartRacer/Camera3D/BottomLeftContainer/LapLabel
@onready var itemPortrait: TextureRect = $DefaultKartRacer/Camera3D/TopLeftContainer/ItemPortrait
@onready var cashLabel: Label = $DefaultKartRacer/Camera3D/TopLeftContainer/Cash
@onready var finish: Label = $DefaultKartRacer/Camera3D/CenterContainer/Finish
@onready var topLeftContainer: VBoxContainer = $DefaultKartRacer/Camera3D/TopLeftContainer
@onready var bottomLeftContainer: VBoxContainer = $DefaultKartRacer/Camera3D/BottomLeftContainer

# Item Textures
@export var boostTexture: Texture2D
@export var projWeaponTexture: Texture2D
@export var invincibilityTexture: Texture2D
@export var trapTexture: Texture2D
@export var blackSquare: Texture2D

func _ready() -> void:
	# Subscribes to relevant match progress events
	var gm: Node = get_node_or_null("../GameManager")
	if(gm != null):
		gm.connect("lap_completed", _on_game_manager_lap_completed)
		gm.connect("race_finished", _on_game_manager_race_finished)

	# Sets up current race's cash value
	# (This relates to a secondary item gambling mechanic in the project)
	var sm: Node = get_node_or_null("/root/SceneManager")
	if(sm != null):
		localCash = sm.get("player_cash")
	else:
		localCash = 100
	cashLabel.text = "Cash: $" + str(localCash)

func _physics_process(delta: float) -> void: # Used for movement input
	kartRacer.stateAcc = Input.is_action_pressed("Accelerate")
	kartRacer.stateBrake = Input.is_action_pressed("Brake")
	kartRacer.stateLeft = Input.is_action_pressed("Left")
	kartRacer.stateRight = Input.is_action_pressed("Right")
	kartRacer.stateItemUse = Input.is_action_pressed("UseItem")
	
	# Debug respawn code
	#if Input.is_key_pressed(KEY_R):
	#	kartRacer._respawn_kart()

func _process(delta: float) -> void: # Used for rerolling items input
	if Input.is_action_just_pressed("Reroll") && kartRacer.get("heldItem") != null:
		kartRacer.call("_gain_item")
		localCash -= 25
		cashLabel.text = "Cash: $" + str(localCash)
		
	# Debug scene switching to test overall game progress
	#if Input.is_action_just_pressed("Drift"): 
	#	var sm: Node = get_node_or_null("/root/SceneManager")
	#	if(sm != null):
	#		sm.call("_finish_race", 1, localCash)

# Updates HUD for each lap
func _on_game_manager_lap_completed(kart_id: int, progress_text: String) -> void:
	if(kartRacer._get_id() == kart_id):
		lapLabel.text = "Lap " + str(progress_text)

# Updates HUD at the end of a race
func _on_game_manager_race_finished(kart_id: int) -> void:
	if(kart_id == kartRacer.get("id")):
		finish.visible = true
		topLeftContainer.visible = false
		bottomLeftContainer.visible = false

# Updates HUD when picking up a new item
func _update_item_portrait(item_id: int) -> void:
	match item_id:
		kartRacer.ItemType.BOOST:
			itemPortrait.texture = boostTexture
		kartRacer.ItemType.PROJ_WEAPON:
			itemPortrait.texture = projWeaponTexture
		kartRacer.ItemType.INVINCIBILITY:
			itemPortrait.texture = invincibilityTexture
		kartRacer.ItemType.TRAP:
			itemPortrait.texture = trapTexture
		_: # For when items are used
			itemPortrait.texture = blackSquare
