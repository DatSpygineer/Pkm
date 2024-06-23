using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Projektanker.Icons.Avalonia;

namespace Pkm;

public partial class PackageLabel : UserControl {
	private const string _iconInstalled = "mdi-checkbox-outline";
	private const string _iconNotInstalled = "mdi-checkbox-blank-outline";
	private const string _iconMarkedForInstall = "mdi-arrow-top-right-bold-box-outline";
	private const string _iconUpdateAvailable = "mdi-alert-box-outline";
	private const string _iconUninstall = "mdi-delete-outline";
	private const string _iconUninstallMarked = "mdi-delete-forever-outline";

	private bool _installed, _updateNeeded;
	
	public PackageLabel(string name, bool installed, bool updateNeeded) {
		InitializeComponent();
		Label.Content = name;
		Attached.SetIcon(BtnInstall, installed ? (updateNeeded ? _iconUpdateAvailable : _iconInstalled) : _iconNotInstalled);
		BtnInstall.Foreground = installed ? (updateNeeded ? Brushes.Orange : Brushes.PaleGreen) : Brushes.Gray;
		ToolTip.SetTip(BtnInstall, installed ? (updateNeeded ? "Update package" : "Up to date") : "Install package");
		BtnRemove.IsEnabled = installed;
		ToolTip.SetTip(BtnRemove, installed ? "Remove package" : "Package is not installed");
		BtnRemove.Foreground = MarkedToRemove ? Brushes.Crimson : Brushes.Black;

		_installed = installed;
		_updateNeeded = updateNeeded;
	}

	public bool MarkedToInstall { get; private set; }
	public bool MarkedToRemove  { get; private set; }

	private void BtnInstall_OnClick(object? sender, RoutedEventArgs e) {
		if (_installed && !_updateNeeded) return;

		MarkedToInstall = !MarkedToInstall;
		if (!MarkedToInstall) {
			Attached.SetIcon(BtnInstall, _installed ? (_updateNeeded ? _iconUpdateAvailable : _iconInstalled) : _iconNotInstalled);
			BtnInstall.Foreground = _installed ? (_updateNeeded ? Brushes.Orange : Brushes.PaleGreen) : Brushes.Gray;
		} else {
			Attached.SetIcon(BtnInstall, _iconMarkedForInstall);
			BtnInstall.Foreground = Brushes.White;
		}
	}
	private void BtnRemove_OnClick(object? sender, RoutedEventArgs e) {
		MarkedToRemove = !MarkedToRemove;
		Attached.SetIcon(BtnRemove, MarkedToRemove ? _iconUninstallMarked : _iconUninstall);
		BtnRemove.Foreground = MarkedToRemove ? Brushes.Crimson : Brushes.Black;
	}
}

