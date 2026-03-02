using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SortingVisualizerApp.App;

var gameSettings = new GameWindowSettings
{
    UpdateFrequency = 60
};

var nativeSettings = new NativeWindowSettings
{
    Title = "Sorting Algorithm Visualizer & Sonifier (View Expansion)",
    ClientSize = new Vector2i(1600, 900),
    APIVersion = new Version(4, 1),
    API = ContextAPI.OpenGL,
    Profile = ContextProfile.Core,
    Vsync = VSyncMode.On
};

using var window = new VisualizerWindow(gameSettings, nativeSettings);
window.Run();
