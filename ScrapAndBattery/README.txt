For Scrap and Battery, one of my contributions was iterating on the existing Defender enemy type, which generates obstructive electric walls between other nearby Defenders. In order to correct major faults that plagued the initial electric wall generation behavior, I analyzed the old and complex code and developed a more straightforward and stable system.

The Blueprint script screenshots within this sample showcase the Defender enemy’s implementation, including the logic necessary for properly handling the wall ability. 
- Within the BP_DefenderEnemy class: 
  - Core images describe the overall setup for the enemy type
  - CreateConnectionCollision images describe how new walls are created
  - SparkConnection images describe how, under the revised system, enemies monitor and respond to eligibility for 
  connections with other enemies
- BP_ConnectionCollision images showcase how, under the revised system, the connection walls perform standard behaviors and respond to ineligible connections
