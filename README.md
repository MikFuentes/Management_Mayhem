# Management Mayhem

## System Requirements
- Windows 7/8/10
- Unity Version 2019.4.9f1 (or higher)

## Setting Up the Project Using Unity Hub
1. Select the project folder to add the project
2. Change Unity Version to **Unity 2019.4.9f1**
3. Keep Target Platform as **Current platform**

## Project Settings

### Build Settings (File > Build Settings)
- Swtich Platform to **Android**

### Project Settings (Edit > Project Settings)
#### Editor
- Unity Remote > Device
  - Set Unity Remote Device to **Any Android Device** 
#### Player
- Android settings > Resolution and Presentation > Allowed Orientations for Auto Rotation
  - Tick ONLY **Landscape Right and Landscape Left**  
- Android settings > Other settings > Configuration
  - Scripting Backend Configuration set to **IL2CPP** 
  - **ARM64** ticked in Target Architectures 

### Game Window
- Set Game Resolution to **2960x1440 Landscape**

## Setting Up Unity with Visual Studio
1. Download Visual Studio Tools for Unity from this [link](https://marketplace.visualstudio.com/items?itemName=SebastienLebreton.VisualStudio2015ToolsforUnity). Do this while Unity and Visual Studio are both closed.
2. From Unity Editor, go to Edit > Preferences > External Tools. On the External Script Editor drop down menu, change that to Visual Studio.
