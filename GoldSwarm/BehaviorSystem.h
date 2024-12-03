//------------------------------------------------------------------------------
//
// File Name: BehaviorSystem.h   
// Author(s): Jack Waldron   
// Project:   Dream Engine
// Course:    GAM250F22
//
// Copyright � 2022 DigiPen (USA) Corporation.
//
//------------------------------------------------------------------------------

#pragma once
#include "sol/sol.hpp"
#include "ISystem.h"
#include "BehaviorComp.h"
#include <string>
#include <vector>
#include <glm/vec2.hpp>
#include <glm/ext/vector_float2.hpp>
#include "BehaviorComp.h"

//------------------------------------------------------------------------------

using vec2 = glm::vec2;
extern sol::state lua; // Creates the general Lua environment 

class BehaviorSystem : public ISystem
{
public:
	
	//Constructor
	BehaviorSystem();

	//Deconstructor
	~BehaviorSystem() override;
	
/// INTERFACE //////////////////////////////////////////////////////////

	// Initialize the system.
	void Initialize() override;
	// Update the system each game loop.
	void Update(float dt) override;
	// Delete all allocations and shutdown
	void Shutdown() override;
	// Render the system each game loop.
	void Render() override;
	// Serialize Data
	void Write() override;
	// Deserialize Data
	void Read(std::string data) override;
	void Read(jsonObj object) override {}
	
///////////////////////////////////////////////////////////////////////
		
	void AddComponent(IComponent* component) override;
	void RemoveComponent(GameObjectPtr go) override;

	void BindingWrapper(char key, int holdState, std::string func, GameObject& obj);
	GameObjectPtr SpawnGameObjectWrapper(std::string objectType, float xPos, float yPos);

private:

	std::vector<BehaviorComp*> behaviorComps_;
	int listCount = 12;
};
