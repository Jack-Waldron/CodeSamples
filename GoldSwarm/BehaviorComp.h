//------------------------------------------------------------------------------
//
// File Name: BehaviorComp.h   
// Author(s): Jack Waldron   
// Project:   Dream Engine
// Course:    GAM250F22
//
// Copyright � 2022 DigiPen (USA) Corporation.
//
//------------------------------------------------------------------------------

#pragma once

#include "sol/sol.hpp"
#include "IComponent.h"
#include "Engine.h"
#include "IObserver.h"
#include "ISubject.h"
#include "ISubject.h"
#include "Message.h"
#include <string>
#include <stdio.h>

class BehaviorComp : public IComponent, public IObserver, public ISubject
{
public:

	// Constructor
	BehaviorComp();

	// Deconstructor
	virtual ~BehaviorComp();
	
/// INTERFACE //////////////////////////////////////////////////////////

	// Initialize the system
	void Initialize() override;
	// Update the system each game loop
	void Update(float dt) override;
	// Delete all allocations and shutdown (unused)
	void Shutdown() override; 
	// Render the system each game loop (unused)
	void Render() override; 
	// Serialize Data (unused)
	void Write() override;
	// Deserialize Data
	void Read(std::string filepath) override;
	void Read(jsonObj object) override;

///////////////////////////////////////////////////////////////////////

	static GameObject* playerOne_;
	static GameObject* playerTwo_;

	// Allows for access to this Behavior's Lua script
	sol::environment& GetEnvironment();

	// Resolves incoming message data package
	void HandleMessage(Message* message) override;

	void SendScoreEvent(int scoreChange);
	void SendWinEvent();   // unused
	void SendLoseEvent();  // unused
	void SendDeathEvent(); // unused
	bool IsDead();

private:
	
	sol::environment env_; // Local Lua environment where script is run
	bool env_loaded;
};