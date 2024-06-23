using Avalonia;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.MaterialDesign;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Pkm;

internal static partial class Interop {
	internal static partial class Sys {
		[DllImport("libc", SetLastError = true)]
		internal static extern uint geteuid();

		internal static uint GetEUid() {
			return geteuid();
		}
	}
}

class Program {
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args) {
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
			throw new PlatformNotSupportedException("This application is for Linux only!");
		}

#if !DEBUG
		if (Interop.Sys.GetEUid() != 0) {
			throw new Exception("Application requires superuser privileges!");
		}
#endif
		BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp() {
		IconProvider.Current.Register<MaterialDesignIconProvider>();
		return AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();
	}
}
