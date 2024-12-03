---------------------------------------------------------------------------------------------------
-- Main benefits of using Lua script interpretation:
--
-- - Syntax is far more relaxed compared to C++ (designers can spend more time focusing on the
--   logic needed for features to work)
--
-- - Scripting interface w/ engine systems is directly controlled, and, as such, can be changed 
--   to either suit designers' needs or make things more straightforward
--
-- - Designers don't need to worry about pulling or managing different references to components or
--   engine systems (component references are set up when the attached behavior component is 
--   created; system references are set up globally when the behavior system is created, alongside
--   other global data refs)
---------------------------------------------------------------------------------------------------
--
-- If given the chance to iterate further on this system/devlop a new script interpretation
-- system, I would place greater emphasis on further streamlining its use on the script side. 
-- Engine system functions such as "BindingWrapper" or "PlaySound" can be implemented in a way 
-- where specific reference to those systems is not necessary. In this case, engine system
-- references would be maintained purely within the BehaviorSystem C++ code. 

-- Similar measures can also be taken to minimize the need for component specific references.
-- "GetPos" and "RotateObject" are specific enough to have implied association with a TransComp,
-- and the same goes for PhysComp and functions like "SetAcceleration". Like with the system
-- references, these component references would be maintained and stored within the C++ code for
-- the attached object's BehaviorComp.
--
---------------------------------------------------------------------------------------------------


---------------------------------------------------------------------------------------------------
-- CREATING A NEW BEHAVIOR:

-- In order to create a new behavior script that will run as expected with the current C++ engine code,
-- a common syntax/structure must be followed. All scripts posses an Init, Update, and Shutdown function.
-- Init functions are called once when an object is created, Update functions are called repeatedly for
-- every frame loop, and Shutdown functions are called once when an object is being destroyed. The syntax
-- for these functions is as shown below.
---------------------------------------------------------------------------------------------------

function Init()
{
	--Initialization code
}

function Update()
{
	--Repeatedly called code
}

function Shutdown()
{
	--Ending code
}

---------------------------------------------------------------------------------------------------
-- CREATING NEW INPUT BINDINGS:

-- If you want to set up new input bindings for an object that map to a function that is defined in
-- a script, follow the syntax as it is shown in the snippet below.
---------------------------------------------------------------------------------------------------

function InputSetup()
{
	-- First parameter is the key that will recieve the mapping,
	-- Second is how the key must be interacted with (HOLD, TAP, RELEASE, NOPRESS)
	-- Third is the name of the Lua function
	-- Fourth is the game object you're referencing (GO is the object your script is attached to)
	BehSys:BindingWrapper('A', HoldState["HLDS_TAP"], "SomeLocalFunction", GO);
}

function SomeLocalFunction()
  -- Code you want mapped to input here
end

---------------------------------------------------------------------------------------------------
-- SPAWNING NEW GAME OBJECTS:

-- If you want to spawn new game objects at a particular position, follow the syntax as it is shown
-- in the snippet below.
---------------------------------------------------------------------------------------------------

objPos = {x = 0, y = 0};

function CreateNewObject()
{
  -- First parameter is the name of the type of object you want to spawn.
  -- - Object types currently include:
  --   -  "Coin"
  -- Second and third parameters are the x and y coordinates of the desired spawn position.
  BehSys:SpawnGameObject("Coin", objPos.x, objPos.y)
}

---------------------------------------------------------------------------------------------------
-- CREATING NEW COLLISION RESOLUTIONS:

-- If you want to set up logic that only triggers when an object collides with a different object
-- of a specific type, use one of the different "OnCollision" functions listed below. You can
-- include as many types of these functions as you want, but only include one of each type (eg.
-- Only include one OnPlayerCollision defintion).
---------------------------------------------------------------------------------------------------

function OnPlayerCollision()
	-- Some logic for when this object collides with a player	
end

function OnHazardCollision()
	-- Some logic for when this object collides with a hazard
end

function OnCoinCollision()
	-- Some logic for when this object collides with a coin
end

function OnEnemyCollision()
	-- Some logic for when this object collides with an enemy
end

---------------------------------------------------------------------------------------------------
-- PLAYING/MODIFYING MUSIC AND SOUND EFFECTS:

-- For getting new music and/or sfx to play/change volume, follow the general syntax below. The
-- sounds that can be used are found/set up in Assets/Sound/sfxData_SoundPrototypes.json. Each
-- sound entry there will have a name (which is used to reference the sound in the scripts), a
-- filename for the given sound, and a field for whether the sound should be played as a stream
-- (Longer sounds need to be streamed as they are played instead of loaded, so set this field to 1
-- when playing long ambience or music). Don't forget to incriment the total number of sounds
-- listed in the protypes file as well.
---------------------------------------------------------------------------------------------------

-- (Parameters for PlaySound are "the sound's name" and "is this looping")
-- (Volume is now auto-set by the engine, so you no longer need to pass in a value for it)

-- Playing a sound effect (Doesn't need to loop, so 2nd param. is 0)
AudioSys:PlaySound("SoundEffectName", 0)

-- Playing a music track (Needs to continuously loop, so 2nd param. is 1)
AudioSys:PlaySound("MusicTrackName", 1)

-- (Parameters for SetVolumeByName are "the sound's name" and "volume on 0-to-1 scale")

-- Changing the volume of a specificly-named music track to full volume
AudioSys:SetVolumeByName("MusicTrackName", 1.0)

-- Changing the volume of a group of sounds of a specific category to half volume (Categories of sounds
-- include STYPE_MASTER, STYPE_BGM, STYPE_GSFX, and STYPE_MSFX).
AudioSys:SetVolumeByType(SoundType["STYPE_MASTER"], 0.5)

-- Changing the volume of all currently playing sounds to zero/muting all audio (Prefer using MASTER
-- channel with SetVolumeByType)
AudioSys:SetAllVolume(0.0)

---------------------------------------------------------------------------------------------------
-- MANIPULATING OBJECTS' PARTICLE EMITTERS

-- In order to manipulate the particle emitter components of game objects that have them set up,
-- follow the syntax below.
---------------------------------------------------------------------------------------------------

-- Enables the particle emitter component of a game object (Activates like in Unity)
ParticleEmit:Enable()

-- Tells an active emitter to start emitting particles (Not necessary if an emitter is set up to
-- just spew particles indefinitely; Coin contains an example of when to use this).
ParticleEmit:Emit()

-- Disables the particle emitter component of a game object (Deactivates like in Unity)
ParticleEmit:Disable()

---------------------------------------------------------------------------------------------------
-- USERTYPES/FUNCTIONS LIST

-- The functions below correspond to functions that will be called in the C++ engine. Note that
-- member functions must be preceeded by the object of the class they will be called on (demonstrated
-- in the list below).
---------------------------------------------------------------------------------------------------

-------------------------------------------------
-- For GameObjectSystem:
-------------------------------------------------

--- Finds a pointer to the game object with the given name
GOSys:FindGameObject(name)

--- Spawns a sword object in front of the player
GOSys:SpawnSword()

-------------------------------------------------
-- For BehaviorSystem:
-------------------------------------------------

--- Sets up an input binding to activate a local Lua function upon a specified interaction
--- (key must be the uppercase version of the letter key you wish to map to)
--- (holdState must be given as a member of the HoldState table; HoldState["HLDS_TAP"],
--- holdState["HLDS_HOLD"], holdState["HLDS_RELEASE"], or holdState["HLDS_NOPRESS"])
--- (gameObject will always be passed in as just GO)
BehSys:BindingWrapper(key, holdState, functionName, gameObject)

-------------------------------------------------
-- For TransformComp:
-------------------------------------------------

--- Returns the original spawn position of the referenced object
TransComp:GetOriginalPosition()

--- Returns the current position of the referenced object
TransComp:GetPos()

--- Sets the current position of the referenced object
TransComp:SetPos(newX, newY)

--- Sets the rotation of the referenced object with a rotation vector
--- (gameObject will always be passed in as just GO)
TransComp:RotateObject(gameObject, newRotVector)

-------------------------------------------------
-- For PhysicsComp:
-------------------------------------------------

--- Returns the current velocity of the referenced object
PhysComp:GetVelocity()

--- Sets the current velocity of the referenced object
PhysComp:SetVelocity(newX, newY)

--- Returns the current acceleration of the referenced object
PhysComp:GetAcceleration()

--- Sets the current acceleration of the referenced object
PhysComp:SetAcceleration(newX, newY)

--- Adjusts the current acceleration based on linear interpolation between two points
PhysComp:LerpAcceleration(targetX, targetY, intensity)

--- Adjusts the current acceleration by adding an additional vector to it
PhysComp:AddAcceleration(XtoAdd, YtoAdd)

-------------------------------------------------
-- For Logging Message:
-------------------------------------------------

--- Sends a message to the trace log
Trace("Trace Message")

--- Sends a message to the error log
ErrorMessage(GO, "Error Message")

--- Sends a message to the system log
SystemMessage(GO, "System Message")

--- Sends a message to the debug log
DebugMessage(GO, "Debug Message")

--- Sends a message to the event log
EventMessage(GO, "Event Message")
