# IME Sharp
[![Nuget](https://img.shields.io/nuget/v/ImeSharp)](https://www.nuget.org/packages/ImeSharp/)

A C# wrapper for Windows IME APIs. Its goal is to support both IMM32 and TSF.

TSF Implementation is based on WPF core.

## Packages

`dotnet add package ImeSharp`

Note ImeSharp.NetStandard package is deprecated, use ImeSharp instead.

## Usage

### Initialization

Call `InputMethod.Initialize` to initialize the input method with a window handle, e.g. `InputMethod.Initialize(someWindowHandle)`.

If you don't want the OS Candidate Window, do `InputMethod.Initialize(someWindowHandle, false)`.

### Custom message pumping

If we don't enable custom windows message pumping. Use TSF in WinForms would have a issue: Frame will randomly stuck when composing with IME.
This is because TSF disables Application.Idle event when it's busy. Enables custom message pumping fix this.

In WinForms, we add message pumping at the end line in `Application.Idle` handler, e.g.:

```c#
private void Application_Idle(object sender, EventArgs e)
{
    Game.Tick();

    // Enables custom message pumping
    InputMethod.PumpMessage();
}
```

### Hook events

```c#
InputMethod.TextComposition += (o, e) =>
{
    // get composition or candidate infos
};
```

```c#
InputMethod.TextInput += (o, e) =>
{
    // get composition result.
};
```

### Set position of OS rendered IME Candidate Window

```c#
InputMethod.SetTextInputRect(location.X, location.Y, 0, textBoxHeight);
```

## Test IMM32 implementation only

IMM32 would be only enabled if TSF service is not available.
You can `return false` manually in `TextServicesLoader.ServicesInstalled` to mimic TSF unavailable case.

## TODO

- Make it work in Unity3d

## MS Docs

- [TSF Application](https://docs.microsoft.com/en-us/windows/win32/tsf/applications)
- [TSF UILess Mode](https://docs.microsoft.com/en-us/windows/win32/tsf/uiless-mode-overview)
- [TSF msctf.h header](https://docs.microsoft.com/en-us/windows/win32/api/msctf/)
- [IMM32 Use IME in a Game](https://docs.microsoft.com/en-us/windows/win32/dxtecharts/using-an-input-method-editor-in-a-game)
- [IMM32 imm.h header](https://docs.microsoft.com/en-us/windows/win32/api/imm/)

## Other samples / implementations

- [Chromium](https://github.com/chromium/chromium/tree/master/ui/base/ime/win)
- [Windows Class Samples](https://github.com/microsoft/Windows-classic-samples/blob/master/Samples/IME/cpp/SampleIME)
- [SDL2](https://github.com/spurious/SDL-mirror/blob/master/src/video/windows/SDL_windowskeyboard.c)
- [WPF Core](https://github.com/dotnet/wpf/tree/master/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/Input)

## Credits

- [WPF Core](https://github.com/dotnet/wpf)
- [Chromium](https://github.com/chromium/chromium)
