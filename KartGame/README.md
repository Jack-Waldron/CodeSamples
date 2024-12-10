For this work-in-progress personal project, I created a central character class that would facilitate the needs of the game’s various “kart” entities. To eliminate any unintended differences in gameplay properties, I developed controllers that would allow for player and computer input without overriding the character’s core movement logic.

The GDScript code within this sample describes the implementation of the base kart character entity and those of the player and computer controllers. 
- kart_racer.gd contains the definitions and physics logic for the base kart character 
- player_racer.gd and bot_racer.gd contain the definitions for the player controller and computer controller respectively