# Unity Dynamic Split
Dynamic Voronoi 2-player 2D split screen system with optimized rendering using stencil buffer in standard rendering pipeline.
Project was made using Unity 2019.4.0f1 but it should run on Unity 2017+ no problem.

## Features
- fully dynamic, assign player transforms, system takes care of the rest
- fast rendering using stencil buffer
- customizable split line color
- FXAA implementation

## Running the demo
Clone the repo and open the folder in (ideally) latest Unity. Demo scene can be found in `Assets/Scenes/Demo.unity`

You can also download the latest [.unitypackage](https://github.com/mkskelet/unity-dynamic-split/releases) to integrate dynamic split screen into your project.

## Implementation in your project
1. Drag `Split Screen Camera` prefab into your scene
2. Assign Players and set PlayerCount either dynamically in runtime or before playing the scene
3. Make sure you use Material with `MaskedStandard` shader, for example, copy `Demo/Materials/Background.mat`

## Custom shaders
All you need to do in your own shaders to make them work with split screen rendering is to put this Stencil block in.
```
Stencil {
    Ref [_VoronoiCellsPlayerStencil]
    Comp [_MaskedStencilOp]
}
```
(you can find the full shader in `Assets/Shaders/MaskedStandard.shader`)

## License
It FREE. [MIT License, look it up.](https://github.com/mkskelet/unity-dynamic-split/blob/master/LICENSE)

## Donate
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=RAZBYJADPV83E&source=url)

If you really don't have anything better to do with your money, you can pay for my coffee/pizza/netflix/spotify/resharper lol. 
