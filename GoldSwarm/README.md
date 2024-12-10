For GoldSwarm, I created tools that would enable my teammates to efficiently shape core project elements into working form. To circumvent my team’s designers’ lack of C++ experience, I created a Lua script interpreter system that streamlined the process of writing gameplay logic.

The code within this sample describes the implementation of the gameplay behavior system within my team’s custom C++ engine. The system utilizes the Sol2 library to interpret and run Lua scripts. 
- BehaviorComp.cpp and BehaviorComp.h describe the game object component type that houses an attached script’s Lua environment
- BehaviorSystem.cpp and BehaviorSystem.h display the overarching engine system that manages these individual behavior components
- behaviorReference.lua is the document I created to teach the designers how to create new Lua gameplay logic