//------------------------------------------------------------------------------
//
// File Name: BehaviorSystem.cpp   
// Author(s): Jack Waldron   
// Project:   Dream Engine
// Course:    GAM250F22
//
// Copyright � 2022 DigiPen (USA) Corporation.
//
//------------------------------------------------------------------------------

#include "BehaviorSystem.h"

// Used to temporarily troubleshoot critical errors/warnings
/*
#pragma warning( disable : 26495 )
#pragma warning( disable : 26439 )
#define SOL_ALL_SAFETIES_ON 1
#include "sol/sol.hpp"
#pragma warning( default : 26495 )
#pragma warning( default : 26439 )
*/

// Used for inter-engine communication
#include "InputSystem.h"
#include "Serialization.h"
#include "TransformComp.h"
#include "PhysicsComp.h"
#include "IComponent.h"
#include "Message.h"
#include "GameObjectSystem.h"
#include "GameObject.h"
#include "Timer.h"
#include "ColliderComp.h"
#include "ParticleEmitter.h"
#include "AudioSystem.h"
#include "AssetSystem.h"
#include "Lerp.h"
#include <functional>

sol::state lua;
int testPullCount = 0; // Used to debug

//----------------------------------------------------------------------------
// Helper and debug function declarations

template <typename Comp, void (Comp::* Func)(float, float)>
void SetVec2(Comp& component, float x, float y);

template <typename Comp, void (Comp::* Func)(vec2 vector)>
void SetVec2WithVec(Comp& component, float x, float y);

template <typename Comp, void (Comp::* Func)(vec2, float)>
void SetVec2PlusFloat(Comp& component, float x, float y, float extra_param);

vec2 ClampWrapper(vec2 values, float min, float max);
vec2 NormalizeVec3Wrapper(vec3 values);

TransformComp* GetTransform(GameObject* go);
PhysicsComp* GetPhysics(GameObject* go);
BehaviorComp* GetBehavior(GameObject* go);

void TestPull(int id);
vec2 FloatToVector(float theta);
void SendTraceMessage(std::string message);
void SendErrorMessage(GameObject* go, std::string message);
void SendSystemMessage(GameObject* go, std::string message);
void SendDebugMessage(GameObject* go, std::string message);
void SendEventMessage(GameObject* go, std::string message);

//----------------------------------------------------------------------------
// BehaviorSystem Function definitions

// Brief:  Constructor for the BehaviorSystem class.
// Author: Jack Waldron
// Params: None.
BehaviorSystem::BehaviorSystem()
{
	name = "BehaviorSystem";
	SetType(SystemType::sBehaviors);
}

// Brief:  Initializes the referenced BehaviorSystem object.
// Author: Jack Waldron
// Params: None
void BehaviorSystem::Initialize()
{
	lua.open_libraries(sol::lib::base);

	// Create usertypes for C++ engine systems in Lua
	lua.new_usertype<GameObjectSystem>("GameObjectSystem",
		"FindGameObject", &GameObjectSystem::FindGameObject,
		"SpawnSword", &GameObjectSystem::SpawnSword);
	lua.new_usertype<BehaviorSystem>("BehaviorSystem",
		"BindingWrapper", &BehaviorSystem::BindingWrapper,
		"SpawnGameObject", &BehaviorSystem::SpawnGameObjectWrapper);
	lua.new_usertype<AudioSystem>("AudioSystem",
		"PlaySound", &AudioSystem::PlaySnd,
		"SetVolumeByName", &AudioSystem::SetVolumeByName,
		"SetVolumeByType", &AudioSystem::SetVolumeByType,
		"SetAllVolume", &AudioSystem::SetAllVolume,
		"GetVolumeByType", &AudioSystem::GetVolumeByType,
		"GetMuteByType", &AudioSystem::GetMuteByType);
	lua.new_usertype<GameObject>("GameObject",
		"GetTransform", &GetTransform,
		"GetPhysics", &GetPhysics,
		"GetBehavior", &GetBehavior,
		"GetID", &GameObject::GetID,
		"SetIsDisabled", &GameObject::SetIsDisabled,
		"Destroy", &GameObject::Destroy);
	lua.new_usertype<AssetSystem>("AssetSystem",
		"NextLevel", &AssetSystem::NextLevel);

	// Create usertypes for C++ engine object components
	lua.new_usertype<TransformComp>("TransformComp",
		"GetOriginalPosition", &TransformComp::GetOriginalPosition,
		"GetPos", &TransformComp::GetPos,
		"SetPos", &SetVec2WithVec<TransformComp, &TransformComp::SetPos>,
		"GetRot", &TransformComp::GetRotation,
		"SetRot", &TransformComp::SetRotation,
		"SetScale", &SetVec2WithVec<TransformComp, &TransformComp::SetScale>,
		"RotateObject", &TransformComp::RotateObject);
	lua.new_usertype<PhysicsComp>("PhysicsComp",
		"GetVelocity", &PhysicsComp::GetVelocity,
		"SetVelocity", sol::overload(&SetVec2<PhysicsComp, &PhysicsComp::SetVelocity>, 
									 &SetVec2WithVec<PhysicsComp, &PhysicsComp::SetVelocity>),
		"GetAcceleration", &PhysicsComp::GetAcceleration,
		"SetAcceleration", &SetVec2WithVec<PhysicsComp, &PhysicsComp::SetAcceleration>,
		"LerpAcceleration", &SetVec2PlusFloat<PhysicsComp, &PhysicsComp::LerpAcceleration>,
		"AddAcceleration", &SetVec2WithVec<PhysicsComp, &PhysicsComp::AddAcceleration>,
		"SwordSlowDown", &SetVec2WithVec<PhysicsComp, &PhysicsComp::SwordSlowdown>,
		"GetAccIncrement", &PhysicsComp::GetAccIncrement,
		"MoveStop", &PhysicsComp::MoveStop);
	lua.new_usertype<ColliderComp>("ColliderComp",
		"GetObjTag", &ColliderComp::GetObjTag);
	lua.new_usertype<BehaviorComp>("BehaviorComp",
		"SendScoreEvent", &BehaviorComp::SendScoreEvent,
		"SendWinEvent", &BehaviorComp::SendWinEvent,
		"SendLoseEvent", &BehaviorComp::SendLoseEvent,
		"SendLoseEvent", &BehaviorComp::SendDeathEvent,
		"IsDead", &BehaviorComp::IsDead);
	lua.new_usertype<ParticleEmitter>("ParticleEmitter",
		"Emit", &ParticleEmitter::Emit,
		"Enable", &ParticleEmitter::Enable,
		"Disable", &ParticleEmitter::Disable);

	// Usertypes for other utilities
	lua.new_usertype<vec2>("vec2",
		"x", &vec2::x,
		"y", &vec2::y);
	lua.new_usertype<vec3>("vec3",
		"x", &vec3::x,
		"y", &vec3::y);

	// Helper function setup
	lua.set_function("ClampVec2", &ClampWrapper);
	lua.set_function("TestPull", &TestPull);
	lua.set_function("NormalizeVec3", &NormalizeVec3Wrapper);
	lua.set_function("FloatToVector", &FloatToVector); //For rotation

	// Debug function setup
	lua.set_function("Trace", &SendTraceMessage);
	lua.set_function("ErrorMessage", &SendErrorMessage);
	lua.set_function("SystemMessage", &SendSystemMessage);
	lua.set_function("DebugMessage", &SendDebugMessage);
	lua.set_function("EventMessage", &SendEventMessage);

	// Global Engine System References
	lua["InputSys"] = (dynamic_cast<InputSystem*>(GetParent()->GetSystem(SystemType::sInput)));
	lua["GOSys"] = (dynamic_cast<GameObjectSystem*>(GetParent()->GetSystem(SystemType::sGameObject)));
	lua["BehSys"] = (dynamic_cast<BehaviorSystem*>(GetParent()->GetSystem(SystemType::sBehaviors)));
	lua["AudioSys"] = (dynamic_cast<AudioSystem*>(GetParent()->GetSystem(SystemType::sAudio)));
	lua["AssetSys"] = (dynamic_cast<AssetSystem*>(GetParent()->GetSystem(SystemType::sAsset)));

	// Global Enums and Other Data
	lua["playerNo"] = 0; // Updates on each Comp. instantiation
	lua["HoldState"] = lua.create_table_with("HLDS_TAP", HLDS_TAP,
		"HLDS_HOLD", HLDS_HOLD,
		"HLDS_RELEASE", HLDS_RELEASE,
		"HLDS_NOPRESS", HLDS_NOPRESS);
	lua["SoundType"] = lua.create_table_with(
		"STYPE_MASTER", SoundType::Master,
		"STYPE_BGM", SoundType::BGM,
		"STYPE_GSFX", SoundType::GSFX,
		"STYPE_MSFX", SoundType::MSFX);
}

// Brief:  Updates the referenced BehaviorSystem object.
// Author: Jack Waldron
// Params: dt - Change in time given by the engine.
void BehaviorSystem::Update(float dt)
{
	int size = static_cast<int>(behaviorComps_.size());

	for (int i = 0; i < size; ++i)
	{
		BehaviorComp* bs = behaviorComps_[i];

		if (!bs->IsDestroyed() && (bs->GetParent() && !(bs->GetParent()->IsDestroyed())))
		{
			bs->Update(dt);
		}
		else // System responsible for deleting components, not game object
		{
			dynamic_cast<InputSystem*>(GetParent()->GetSystem(SystemType::sInput))->ClearBindingsOfObject(behaviorComps_[i]->GetParent());

			delete behaviorComps_[i];

			behaviorComps_.erase(behaviorComps_.begin() + i);
			--i;
			--size;
		}
	}
}

// Brief:  Adds a new BehaviorComp object to the internal manager of
//         the referenced BehaviorSystem object.
// Author: Jack Waldron
// Params: behavior - Pointer to the BehaviorComp to be added.
void BehaviorSystem::AddComponent(IComponent* behavior)
{ 
	if (behavior)
		behaviorComps_.push_back(dynamic_cast<BehaviorComp*>(behavior));
}

// Brief:  Removes a GameObject's BehaviorComp from the internal manager of
//         the referenced BehaviorSystem object.
// Author: Jack Waldron
// Params: go - Pointer to the GameObject whose BehaviorComp will be removed.
void BehaviorSystem::RemoveComponent(GameObjectPtr go)
{
	int i = 0;

	for (auto behavior : behaviorComps_)
	{
		if (behavior->GetParent() == go)
			behaviorComps_.erase(behaviorComps_.begin() + i);

		++i;
	}
}

void BehaviorSystem::BindingWrapper(char key, int holdState, std::string func, GameObject& obj)
{
	InputSystem* is = dynamic_cast<InputSystem*>(GetParent()->GetSystem(SystemType::sInput));
	BehaviorComp* bc = dynamic_cast<BehaviorComp*>(obj.GetComponent(ComponentType::cBehavior));
	
	if (key == ';')
		is->CreateBinding(0xBA, holdState, func, &obj);
	else
		is->CreateBinding(key, holdState, func, &obj);
}

// Spawns a game object of a specified type
GameObjectPtr BehaviorSystem::SpawnGameObjectWrapper(std::string objectType, float xPos, float yPos)
{
	GameObjectSystem* gos = dynamic_cast<GameObjectSystem*>(GetParent()->GetSystem(SystemType::sGameObject));

	if (gos != NULL)
	{
		vec3 posIn3;
		
		if (objectType == "MovingCoin")
			posIn3 = vec3(xPos, yPos, 0.9f);
		else
			posIn3 = vec3(xPos, yPos, 1);

		GameObjectPtr go = gos->SpawnGameObject(objectType, posIn3);
		if (go != NULL)
			return go;
		 
		return NULL;
	}
	
	return NULL;
}

//----------------------------------------------------------------------------
// Helper and debug function definitions

template <typename Comp, void (Comp::*Func)(float, float)>
void SetVec2(Comp& component, float x, float y)
{
	auto func = std::bind(Func, &component, std::placeholders::_1, std::placeholders::_2);
	func(x, y);
}

template <typename Comp, void (Comp::* Func)(vec2 vector)>
void SetVec2WithVec(Comp& component, float x, float y)
{
	auto func = std::bind(Func, &component, std::placeholders::_1);
	func(vec2(x, y));
}

template <typename Comp, void (Comp::* Func)(vec2, float)>
void SetVec2PlusFloat(Comp& component, float x, float y, float extra_param)
{
	vec2 vec = {x, y};
	
	auto func = std::bind(Func, &component, std::placeholders::_1, std::placeholders::_2);
	func(vec, extra_param);
}

void SendTraceMessage(std::string message)
{
	TRACE_(message);
}

void SendErrorMessage(GameObject* go, std::string message)
{
	ERROR_LOG_(go, message);
}

void SendSystemMessage(GameObject* go, std::string message)
{
	SYS_LOG_(go, message);
}

void SendDebugMessage(GameObject* go, std::string message)
{
	DEBUG_LOG_(go, message);
}

void SendEventMessage(GameObject* go, std::string message)
{
	EVENT_LOG_(go, message);
}

// Rotation to 2D position on a circle
vec2 FloatToVector(float theta)
{
	vec2 vecReturn;
	vecReturn.x = cosf(theta);
	vecReturn.y = sinf(theta);

	return vecReturn;
}

vec2 ClampWrapper(vec2 values, float min, float max)
{
	return glm::clamp(values, min, max);
}

vec2 NormalizeVec3Wrapper(vec3 values)
{
	return glm::normalize(values);
}

TransformComp* GetTransform(GameObject* go)
{
	if (go != NULL)
		return dynamic_cast<TransformComp*>(go->GetComponent(ComponentType::cTransform));

	return nullptr;
}

PhysicsComp* GetPhysics(GameObject* go)
{
	if (go != NULL)
		return dynamic_cast<PhysicsComp*>(go->GetComponent(ComponentType::cPhysics));

	return nullptr;
}

BehaviorComp* GetBehavior(GameObject* go)
{
	if (go != NULL)
		return dynamic_cast<BehaviorComp*>(go->GetComponent(ComponentType::cBehavior));

	return nullptr;
}

// Debug testing to ensure proper Lua-C++ communication
void TestPull(int id)
{
	testPullCount += id;
	return;
}