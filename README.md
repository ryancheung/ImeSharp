# IME Sharp

A C# wrapper for Windows IME APIs. Its goal is to support both IMM32 and TSF.

Most of the code were extracted from the WPF core repo.

## TODO

1. Implemented UI-less mode for TSF, namely render IME candidate list manually instead of using the OS renderer one.
2. Raise events for text compositions and results. 
3. Unify event API for both implementation of IMM32 and TSF.
4. Refactor code.

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
