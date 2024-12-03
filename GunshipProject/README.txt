For my “Gunship Project”, I developed gameplay features such as multiple animated UI elements and a boss enemy encounter. In order to quickly produce these features, I created an underlying logic system that would enable rapid construction of simple animations, delayed gameplay signals, and detailed sequences of other simple procedures.

The C# code within this sample depicts the architecture I developed to achieve this goal: the Action List. The structure stores and updates a list of command-specific objects called Actions, which can be configured to support a wide variety of behaviors. 
- ActionList.cs and Action.cs show the base implementation of these structures 
- Translate.cs showcases a basic derivation on the Action class 
- UIManager.cs shows how these structures are used to facilitate the project’s UI behaviors
- Boss.cs and BossDecision.cs show how the structures are used to create the boss encounter
