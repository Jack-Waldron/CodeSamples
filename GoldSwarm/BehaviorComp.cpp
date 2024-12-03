//------------------------------------------------------------------------------
// These files were used within this project's custom C++ engine to facilitate
// the use of Lua scripts as gameplay behavior logic. The C++ source/headers
// managed these behaviors as game object components processed through a central
// engine system. The reference document, written in Lua, was meant to inform
// the designers on my team how they should create new script files.
//------------------------------------------------------------------------------

//------------------------------------------------------------------------------
//
// File Name: BehaviorComp.cpp   
// Author(s): Jack Waldron   
// Project:   Dream Engine
// Course:    GAM250F22
//
// Copyright � 2022 DigiPen (USA) Corporation.
//
//------------------------------------------------------------------------------

#include "sol/sol.hpp"
#include "BehaviorComp.h"
#include "BehaviorSystem.h"
#include "IComponent.h"
#include "Deserializer.h"
#include "GameObjectSystem.h"

// Used for inter-engine communication
#include "Engine.h"
#include "IObserver.h"
#include "Timer.h"
#include "Message.h"
#include "ISubject.h"
#include "TransformComp.h"
#include "PhysicsComp.h"
#include "InputSystem.h"
#include "EventSystem.h"
#include "ScoreKeeper.h"

// Global player references
GameObject* BehaviorComp::playerOne_ = nullptr;
GameObject* BehaviorComp::playerTwo_ = nullptr;

//-------------------------------------------------------

// Brief:  Constructor for the BehaviorComp class.
// Author: Jack Waldron
// Params: None.
BehaviorComp::BehaviorComp()
	: IComponent(ComponentType::cBehavior)
	, env_(lua, sol::create)
	, env_loaded(false) // stops lua init if script isn't properly loaded yet
{
	SetType(ComponentType::cBehavior);
	// Script file opened in Read functions
}

// Brief:  Destructor for the BehaviorComp class.
// Author: Jack Waldron
// Params: None.
BehaviorComp::~BehaviorComp()
{
	// Calls Lua function 'Shutdown'.
	env_["Shutdown"]();

	EventSystem::instance()->RemoveObserver(this);
	// Sol/Lua should automatically clean themselves up
}

// Brief:  Initializes the referenced BehaviorComp object.
// Author: Jack Waldron
// Params: None.
void BehaviorComp::Initialize()
{
	GameObject* parent = GetParent(); 			  
	SetName(parent->GetName() + "_BehaviorComp"); // Here to avoid NULL setting/access

	// Ensure environment has valid component/system references
	// Carry over systems
	env_["InputSys"] = lua["InputSys"];
	env_["GOSys"] = lua["GOSys"];
	env_["BehSys"] = lua["BehSys"];
	env_["AudioSys"] = lua["AudioSys"];
	env_["HoldState"] = lua["HoldState"];
	env_["SoundType"] = lua["SoundType"];
	env_["AssetSys"] = lua["AssetSys"];

	// Carry over helper functions
	env_["ClampVec2"] = lua["ClampVec2"];
	env_["TestPull"] = lua["TestPull"];
	env_["NormalizeVec3"] = lua["NormalizeVec3"];
	env_["FloatToVector"] = lua["FloatToVector"];

	// Carry over debug/messaging functions
	env_["Trace"] = lua["Trace"];
	env_["ErrorMessage"] = lua["ErrorMessage"];
	env_["SystemMessage"] = lua["SystemMessage"];
	env_["DebugMessage"] = lua["DebugMessage"];
	env_["EventMessage"] = lua["EventMessage"];

	// Record player-specific info
	if (parent->GetName() == "PlayerOne")
	{
		env_["playerNo"] = 1;
		playerOne_ = parent;
	}
	else if(parent->GetName() == "PlayerTwo")
	{
		env_["playerNo"] = 2;
		playerTwo_ = parent;
	}
	env_["GO"] = parent; // Avoids copying of parent game object

	// Include global player data refs
	env_["PlayerOne"] = playerOne_;
	env_["PlayerTwo"] = playerTwo_;
	env_["PlayerOneScore"] = ScoreKeeper::instance()->GetScore(Players::Player1);
	env_["PlayerTwoScore"] = ScoreKeeper::instance()->GetScore(Players::Player2);
	
	// Carry over relevant component data
	if (parent->GetComponent(ComponentType::cPhysics))
		env_["PhysComp"] = (dynamic_cast<PhysicsComp*>(parent->GetComponent(ComponentType::cPhysics)));
	if (parent->GetComponent(ComponentType::cTransform))
		env_["TransComp"] = (dynamic_cast<TransformCompPtr>(parent->GetComponent(ComponentType::cTransform)));
	if (parent->GetComponent(ComponentType::cCollision))
		env_["CollComp"] = (dynamic_cast<ColliderCompPtr>(parent->GetComponent(ComponentType::cCollision)));
	if (parent->GetComponent(ComponentType::cPartilceEmitter))
		env_["ParticleEmit"] = (dynamic_cast<ParticleEmitter*>(parent->GetComponent(ComponentType::cPartilceEmitter)));
	env_["BehComp"] = (dynamic_cast<BehaviorComp*>(parent->GetComponent(ComponentType::cBehavior)));

	// Calls Lua function 'Init'
	env_["Init"]();
}

// Brief:  Updates the referenced BehaviorComp object.
// Author: Jack Waldron
// Params: dt - Change in time given by the engine.
void BehaviorComp::Update(float dt)
{
	env_["dt"] = dt;

	// Used during development to handle a reference sequencing error
	if (env_["PlayerOne"] != playerOne_)
		env_["PlayerOne"] = playerOne_;
	if (env_["PlayerTwo"] != playerTwo_)
		env_["PlayerTwo"] = playerTwo_;

	// Feeds in relevant score data
	env_["PlayerOneScore"] = ScoreKeeper::instance()->GetScore(Players::Player1);
	env_["PlayerTwoScore"] = ScoreKeeper::instance()->GetScore(Players::Player2);

	// Calls Lua function 'Update'.
	env_["Update"]();

	// Used to ensure bad Lua calls aren't made before component is destroyed
	if (GetParent()->IsDestroyed())
	{
		if (GetParent()->GetName() == "PlayerOne")
			playerOne_ = nullptr;
		else if (GetParent()->GetName() == "PlayerTwo")
			playerTwo_ = nullptr;
	}
}

// Loads script into component environment
void BehaviorComp::Read(std::string filepath)
{
	Deserializer urDeserial(filepath);
	jsonObj BehObject = urDeserial.getObject("Behavior");

	if (BehObject.hasObject("scriptFile"))
	{
		// How to load Behavior Script Files
		lua.safe_script_file(BehObject.getString("scriptFile"), env_);
	}
}

// Loads script into component environment
void BehaviorComp::Read(jsonObj object)
{
	// Script File Reading
	if (object.hasObject("scriptFile"))
	{
		// How to load Behavior Script Files
		lua.safe_script_file(object.getString("scriptFile"), env_);
	}
}

// Brief:  Performs a specified action based on given messsage data.
// Author: Jack Waldron 
// Params: message - A message package sent from an exterior system this Behavior
//                   is set to observe.
void BehaviorComp::HandleMessage(Message* message)
{
	if (isDestroyed)
		return;

	if (message->type == MessageType::mTrigger) // Ensures given message is a TriggerMessage
	{
		TriggerMessage* collision = dynamic_cast<TriggerMessage*>(message);

		// To ensure calls only on entry
		if (collision->triggerState == ColliderState::cEnter) 
		{
			// Checks if "first object" of collision is reacting; sender equal to collider1
			if (message->sender->GetID() == GetParent()->GetID()) 
			{
				switch (collision->collider2->GetObjTag()) // Checks what first object is colliding with
				{
					case Tag::Player1:
					case Tag::Player2:
					{
						lua.safe_script("if OnPlayerCollision ~= nil then OnPlayerCollision() end", env_);
						break;
					}
					case Tag::Coin:
					{
						lua.safe_script("if OnCoinCollision ~= nil then OnCoinCollision() end", env_);
						break;
					}
					case Tag::Hazard:
					{
						if (GetParent()->GetIsDisabled())
							return;

						lua.safe_script("if OnHazardCollision ~= nil then OnHazardCollision() end", env_);
						break;
					}
					case Tag::Enemy:
					{
						lua.safe_script("if OnEnemyCollision ~= nil then OnEnemyCollision() end", env_);
						break;
					}
					case Tag::Sword:
					{
						auto luaResult = lua.safe_script("if OnSwordCollision ~= nil then OnSwordCollision() end", env_);

						if (!luaResult.valid())
						{
							sol::error err = luaResult;
							std::cout << err.what() << std::endl;
						}

						return;
					}
					case Tag::Win:
					{
						lua.safe_script("if OnGoalCollision ~= nil then OnGoalCollision() end", env_);
						break;
					}
				}
			}
			// Checks if "second object" of collision is reacting; sender is collider2
			else if (collision->collider2->GetParent()->GetID() == GetParent()->GetID()) 
			{
				switch (collision->collider1->GetObjTag()) // Checks what second object is colliding with
				{
					case Tag::Player1:
					case Tag::Player2:
					{
						lua.safe_script("if OnPlayerCollision ~= nil then OnPlayerCollision() end", env_);
						break;
					}
					case Tag::Coin:
					{
						lua.safe_script("if OnCoinCollision ~= nil then OnCoinCollision() end", env_);
						break;
					}
					case Tag::Hazard:
					{
						lua.safe_script("if OnHazardCollision ~= nil then OnHazardCollision() end", env_);
						break;
					}
					case Tag::Enemy:
					{
						lua.safe_script("if OnEnemyCollision ~= nil then OnEnemyCollision() end", env_);
						break;
					}
					case Tag::Sword:
					{
						lua.safe_script("if OnSwordCollision ~= nil then OnSwordCollision() end", env_);
						break;
					}
					case Tag::Win:
					{
						lua.safe_script("if OnGoalCollision ~= nil then OnGoalCollision() end", env_);
						break;
					}
				}
			}
		}
	}
	else if (message->type == MessageType::mCollision)
	{
		CollisionMessage* collision = dynamic_cast<CollisionMessage*>(message);

		// Checks if "first object" of collision is reacting; sender equal to collider1
		if (message->sender->GetID() == GetParent()->GetID())
		{
			switch (collision->collider2->GetObjTag()) // Checks what first object is colliding with
			{
				case Tag::Player1:
				case Tag::Player2:
				{
					lua.safe_script("if OnPlayerCollision ~= nil then OnPlayerCollision() end", env_);
					break;
				}
				case Tag::Coin:
				{
					lua.safe_script("if OnCoinCollision ~= nil then OnCoinCollision() end", env_);
					break;
				}
				case Tag::Hazard:
				{
					lua.safe_script("if OnHazardCollision ~= nil then OnHazardCollision() end", env_);
					break;
				}
				case Tag::Enemy:
				{
					lua.safe_script("if OnEnemyCollision ~= nil then OnEnemyCollision() end", env_);
					break;
				}
				case Tag::Sword:
				{
					lua.safe_script("if OnSwordCollision ~= nil then OnSwordCollision() end", env_);
					break;
				}
				case Tag::Win:
				{
					lua.safe_script("if OnGoalCollision ~= nil then OnGoalCollision() end", env_);
					break;
				}
			}
		}
		// Checks if "second object" of collision is reacting
		else if (collision->collider2->GetParent()->GetID() == GetParent()->GetID()) 
		{
			switch (collision->collider1->GetObjTag()) // Checks what second object is colliding with
			{
				case Tag::Player1:
				case Tag::Player2:
				{
					lua.safe_script("if OnPlayerCollision ~= nil then OnPlayerCollision() end", env_);
					break;
				}
				case Tag::Coin:
				{
					lua.safe_script("if OnCoinCollision ~= nil then OnCoinCollision() end", env_);
					break;
				}
				case Tag::Hazard:
				{
					lua.safe_script("if OnHazardCollision ~= nil then OnHazardCollision() end", env_);
					break;
				}
				case Tag::Enemy:
				{
					lua.safe_script("if OnEnemyCollision ~= nil then OnEnemyCollision() end", env_);
					break;
				}
				case Tag::Sword:
				{
					lua.safe_script("if OnSwordCollision ~= nil then OnSwordCollision() end", env_);
					break;
				}
				case Tag::Win:
				{
					lua.safe_script("if OnGoalCollision ~= nil then OnGoalCollision() end", env_);
					break;
				}
			}
		}
	}
}

// Ensures player score is properly tracked by general game state/UI elements
void BehaviorComp::SendScoreEvent(int scoreChange)
{
	PlayerScoreEvent* psEvent = new PlayerScoreEvent();

	psEvent->isInLead = env_["leaderInScore"];
	psEvent->scoreChange = scoreChange;
	psEvent->sender = dynamic_cast<IObject*>(GetParent());
	psEvent->type = MessageType::mScoreEvent;
	psEvent->whichPlayer = env_["playerNo"];

	Message* scoreMessage = dynamic_cast<Message*>(psEvent);
	SendMessage(scoreMessage);
}

bool BehaviorComp::IsDead()
{
	float RTL = env_["respawnTimeLeft"];

	if (RTL > 0.0f)
		return true;
	return false;
}

// Brief:  Returns a reference to this BehaviorComp's Lua environment.
// Author: Jack Waldron
// Params: None.
sol::environment& BehaviorComp::GetEnvironment()
{
	return env_;
}