# Event Management Mayhem: A Team-Based Multiplayer Game

This is the repository for Event Management Mayhem, a multiplayer party simulation game for mobile devices that aims to teach students the basics of event management. Learn about the different roles in event management and work together to complete the assigned tasks in this fun and chaotic game!

Play now: https://play.unity.com/mg/other/event-management-mayhem

## Build Instructions:
These instructionsa are for compiling the game's source code!

If you want ot just 

### System Requirements
- Windows 7/8/10
- Unity Version 2019.4.9f1 (or higher)

### Setting Up the Project Using Unity Hub
1. Select the project folder to add the project
2. Change Unity Version to **Unity 2019.4.9f1**
3. Keep Target Platform as **Current platform**

### Project Settings

#### Build Settings (File > Build Settings)
- Swtich Platform to **Android**

#### Project Settings (Edit > Project Settings)
##### Editor
- Unity Remote > Device
  - Set Unity Remote Device to **Any Android Device** 
##### Player
- Android settings > Resolution and Presentation > Allowed Orientations for Auto Rotation
  - Tick ONLY **Landscape Right and Landscape Left**  
- Android settings > Other settings > Configuration
  - Scripting Backend Configuration set to **IL2CPP** 
  - **ARM64** ticked in Target Architectures 

#### Game Window
- Set Game Resolution to **2960x1440 Landscape**

### Setting Up Unity with Visual Studio
1. Download Visual Studio Tools for Unity from this [link](https://marketplace.visualstudio.com/items?itemName=SebastienLebreton.VisualStudio2015ToolsforUnity). Do this while Unity and Visual Studio are both closed.
2. From Unity Editor, go to Edit > Preferences > External Tools. On the External Script Editor drop down menu, change that to Visual Studio.
