using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using System.Linq;

namespace Pkm;

public partial class MainWindow : Window {
	private PackageManager _pm;
	private bool _busy;

	public bool Busy {
		get => _busy;
		set {
			_busy = value;
			BtnApply.IsEnabled = !_busy;
			BtnRefresh.IsEnabled = !_busy;
			BtnSearch.IsEnabled = !_busy;
			TxSearchInput.IsEnabled = !_busy;
			LsPackages.IsEnabled = !_busy;
		}
	}

	public MainWindow() {
		InitializeComponent();
		_pm = new Apt();
		_pm.OnInstallProgress += (progress, _) => {
			PbProgress.Value = progress;
		};
		_pm.OnRemoveProgress += (progress, _) => {
			PbProgress.Value = progress;
		};
		_pm.OnUpdateProgress += (progress, _) => {
			PbProgress.Value = progress;
		};
		_pm.OnInstallFinish += (code, stdout) => {
			if (code == 0) {
				var msg = MessageBoxManager.GetMessageBoxStandard(
					"Success", "Successfully installed package(s)", icon: MsBox.Avalonia.Enums.Icon.Info);
				msg?.ShowAsync().Wait();
			} else {
				var msg = MessageBoxManager.GetMessageBoxStandard(
					"Failure", $"Failed to install package(s):\n{stdout}", icon: MsBox.Avalonia.Enums.Icon.Error);
				msg?.ShowAsync().Wait();
			}
			Busy = false;
		};
		_pm.OnRemoveFinish += async (code, stdout) => {
			if (code == 0) {
				var msg = MessageBoxManager.GetMessageBoxStandard(
					"Success", "Successfully removed package(s)", icon: MsBox.Avalonia.Enums.Icon.Info);
				await msg.ShowWindowDialogAsync(this);
			} else {
				var msg = MessageBoxManager.GetMessageBoxStandard(
					"Failure", $"Failed to remove package(s):\n{stdout}", icon: MsBox.Avalonia.Enums.Icon.Error);
				await msg.ShowWindowDialogAsync(this);
			}
			Busy = false;
		};
		_pm.OnUpdateFinish += async (code, stdout) => {
			if (code == 0) {
				var msg = MessageBoxManager.GetMessageBoxStandard(
					"Success", "Successfully updated package(s)", icon: MsBox.Avalonia.Enums.Icon.Info);
				await msg.ShowWindowDialogAsync(this);
			} else {
				var msg = MessageBoxManager.GetMessageBoxStandard(
					"Failure", $"Failed to update package(s):\n{stdout}", icon: MsBox.Avalonia.Enums.Icon.Error);
				await msg.ShowWindowDialogAsync(this);
			}
			Busy = false;
		};
		_pm.OnRefreshFinish += async (code, stdout) => {
			if (code != 0) {
				var msg = MessageBoxManager.GetMessageBoxStandard(
					"Failure", $"Error while refreshing packages:\n{stdout}", icon: MsBox.Avalonia.Enums.Icon.Error);
				await msg.ShowWindowDialogAsync(this);
			}
			Busy = false;
		};

		Loaded += OnLoaded;
		KeyDown += OnKeyDown;
	}
	private void ListInstalled() {
		LsPackages.Items.Clear();
		foreach (var pkg in _pm.InstalledPackages) {
			var label = new PackageLabel(pkg, true, _pm.UpdateAvailable.Contains(pkg));
			LsPackages.Items.Add(label);
		}
	}
	private void OnLoaded(object? sender, RoutedEventArgs e) {
		_pm.Refresh();
		ListInstalled();
	}
	private void OnKeyDown(object? sender, KeyEventArgs e) {
		if (e.Key == Key.Return) {
			if (TxSearchInput.IsFocused) {
				BtnSearch_OnClick(sender, new RoutedEventArgs());
			}
		}
	}
	private void BtnRefresh_OnClick(object? sender, RoutedEventArgs e) {
		_pm.Refresh();
		Busy = true;
	}
	private void BtnSearch_OnClick(object? sender, RoutedEventArgs e) {
		Busy = true;
		if (TxSearchInput.Text is { Length: > 0 } search) {
			_pm.Refresh();
			var pkgs = _pm.Search(search);
			LsPackages.Items.Clear();
			if (pkgs.Length > 0) {
				foreach (var pkg in pkgs) {
					var installed = _pm.InstalledPackages.Contains(pkg);
					var update = installed && _pm.UpdateAvailable.Contains(pkg);
					LsPackages.Items.Add(new PackageLabel(pkg, installed, update));
				}
			} else {
				LsPackages.Items.Add("No package found");
			}
		} else {
			ListInstalled();
		}
		Busy = false;
	}
}
