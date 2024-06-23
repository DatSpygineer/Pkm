using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pkm;

public delegate void PackageManagerProcessEventHandler(int progress, string stdoutLine);
public delegate void PackageManagerFinishedEventHandler(int exitCode, string stdout);

public abstract class PackageManager {

	public event PackageManagerProcessEventHandler? OnInstallProgress;
	public event PackageManagerProcessEventHandler? OnUpdateProgress;
	public event PackageManagerProcessEventHandler? OnRemoveProgress;

	public event PackageManagerFinishedEventHandler? OnInstallFinish;
	public event PackageManagerFinishedEventHandler? OnUpdateFinish;
	public event PackageManagerFinishedEventHandler? OnRemoveFinish;
	public event PackageManagerFinishedEventHandler? OnRefreshFinish;

	public abstract void Install(string[] packages);
	public abstract void Update(string[] packages);
	public abstract void Remove(string[] packages);
	public abstract void Refresh();
	public abstract string[] Search(string searchPhrase);
	public abstract string[] InstalledPackages { get; }
	public abstract string[] UpdateAvailable { get; }

	protected void CallInstallProgress(int progress, string stdoutLine) {
		OnInstallProgress?.Invoke(progress, stdoutLine);
	}
	protected void CallUpdateProgress(int progress, string stdoutLine) {
		OnUpdateProgress?.Invoke(progress, stdoutLine);
	}
	protected void CallRemoveProgress(int progress, string stdoutLine) {
		OnRemoveProgress?.Invoke(progress, stdoutLine);
	}

	protected void CallInstallFinish(int exitCode, string stdout) {
		OnInstallFinish?.Invoke(exitCode, stdout);
	}
	protected void CallUpdateFinish(int exitCode, string stdout) {
		OnUpdateFinish?.Invoke(exitCode, stdout);
	}
	protected void CallRemoveFinish(int exitCode, string stdout) {
		OnRemoveFinish?.Invoke(exitCode, stdout);
	}
	protected void CallRefreshFinish(int exitCode, string stdout) {
		OnRefreshFinish?.Invoke(exitCode, stdout);
	}
}

public class Apt : PackageManager {
	private string[] _installed = [ ];
	private string[] _updates = [ ];

	public override void Install(string[] packages) {
		using var process = new Process();
		process.StartInfo = new ProcessStartInfo("sudo", $"apt install {string.Join(' ', packages)} -y") {
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		var sb = new StringBuilder();
		if (process.Start()) {
			process.OutputDataReceived += (_, e) => {
				if (!string.IsNullOrEmpty(e.Data)) {
					var match = Regex.Match(e.Data, @"\d+%");
					var progress = 0;
					if (match.Success) {
						var percentage = match.Value.TrimEnd('%');
						int.TryParse(percentage, out progress);
					}
					CallInstallProgress(progress, e.Data);
					sb.AppendLine(e.Data);
				}
			};
			process.WaitForExit();
			CallInstallFinish(process.ExitCode, sb.ToString());
		}
	}
	public override void Update(string[] packages) {
		using var process = new Process();
		process.StartInfo = new ProcessStartInfo("sudo", $"apt install --only-upgrade {string.Join(' ', packages)} -y") {
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		var sb = new StringBuilder();
		if (process.Start()) {
			process.OutputDataReceived += (_, e) => {
				if (!string.IsNullOrEmpty(e.Data)) {
					var match = Regex.Match(e.Data, @"\d+%");
					var progress = 0;
					if (match.Success) {
						var percentage = match.Value.TrimEnd('%');
						int.TryParse(percentage, out progress);
					}
					CallUpdateProgress(progress, e.Data);
					sb.AppendLine(e.Data);
				}
			};
			process.WaitForExit();
			CallUpdateFinish(process.ExitCode, sb.ToString());
		}
	}
	public override void Remove(string[] packages) {
		using var process = new Process();
		process.StartInfo = new ProcessStartInfo("sudo", $"apt remove {string.Join(' ', packages)} -y") {
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		var sb = new StringBuilder();
		if (process.Start()) {
			process.OutputDataReceived += (_, e) => {
				if (!string.IsNullOrEmpty(e.Data)) {
					var match = Regex.Match(e.Data, @"\d+%");
					var progress = 0;
					if (match.Success) {
						var percentage = match.Value.TrimEnd('%');
						int.TryParse(percentage, out progress);
					}
					CallRemoveProgress(progress, e.Data);
					sb.AppendLine(e.Data);
				}
			};
			process.WaitForExit();
			CallRemoveFinish(process.ExitCode, sb.ToString());
		}
	}
	public override void Refresh() {
		var process = new Process();
		process.StartInfo = new ProcessStartInfo("sudo", "apt update") {
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		if (process.Start()) {
			var log = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			CallRefreshFinish(process.ExitCode, log);

			process.Dispose();
			process = new Process();

			process.StartInfo = new ProcessStartInfo("apt", "list --installed") {
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			if (process.Start()) {
				var output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				var packages = new List<string>();
				foreach (var pkg in output.Split('\n')) {
					if (pkg.Contains('/')) {
						packages.Add(pkg.Split('/')[0].Trim());
					}
				}
				_installed = packages.ToArray();
			}

			process.Dispose();
			process = new Process();

			process.StartInfo = new ProcessStartInfo("apt", "list --upgradable") {
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			if (process.Start()) {
				var output = process.StandardOutput.ReadToEnd();
				process.WaitForExit();

				var packages = new List<string>();
				foreach (var pkg in output.Split('\n')) {
					if (pkg.Contains('/')) {
						packages.Add(pkg.Split('/')[0].Trim());
					}
				}
				_updates = packages.ToArray();
			}
			process.Dispose();
		}
	}
	public override string[] Search(string searchPhrase) {
		using var process = new Process();

		process.StartInfo = new ProcessStartInfo("apt-cache", $"search {searchPhrase}") {
			RedirectStandardOutput = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		if (process.Start()) {
			var output = process.StandardOutput.ReadToEnd();
			process.WaitForExit();

			var packages = new List<string>();
			foreach (var pkg in output.Split('\n')) {
				if (pkg.Contains(" - ")) {
					packages.Add(pkg.Split(" - ")[0].Trim());
				}
			}
			return packages.ToArray();
		}
		return [ ];
	}
	public override string[] InstalledPackages => _installed;
	public override string[] UpdateAvailable => _updates;

}
