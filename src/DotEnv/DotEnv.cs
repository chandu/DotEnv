using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.Serialization;

namespace DotEnv
{
	[Flags]
	public enum EnvFileLoadSettings
	{
		None,
		ThrowOnMissingFile = 1,
		ThrowOnInvalidFile = 2
	}

	public static class DotEnvConfig
	{
		// ReSharper disable InconsistentNaming
		private static EnvironmentImpl _environmentImpl;
		// ReSharper restore InconsistentNaming

		
		public static void Install(EnvFileLoadSettings loadSettings = EnvFileLoadSettings.None)
		{
			if (_environmentImpl == null)
			{
				_environmentImpl = new EnvironmentImpl(new JsonSettingsProvider(null, loadSettings));
			}
			_environmentImpl.Load();
		}

		public static void Uninstall()
		{
			_environmentImpl.Unload();
		}
	}

	public class DotEnvConfigFileException : Exception
	{
		public DotEnvConfigFileException(string message)
			: base(message)
		{
		}

		public DotEnvConfigFileException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public const string FileMissingMessage = ".env file is missing.";
		public const string FileInvalidMessage = ".env file is invalid.";
	}

	public interface IEnvironment
	{
		void Load();

		void Unload();
	}

	internal class EnvironmentImpl : IEnvironment
	{
		private readonly ISettingsProvider _settingsesProvider;
		private bool _isLoaded;
		internal readonly List<Setting> _previousState = new List<Setting>();

		public EnvironmentImpl(ISettingsProvider settingsesProvider)
		{
			_settingsesProvider = settingsesProvider;
		}

		public void Load()
		{
			if (_isLoaded)
			{
				return;
			}
			foreach (var setting in _settingsesProvider.Get())
			{
				var existingValue = Environment.GetEnvironmentVariable(setting.Key, EnvironmentVariableTarget.Process);
				_previousState.Add(new Setting(setting.Key, existingValue));

				Environment.SetEnvironmentVariable(setting.Key, setting.Value, EnvironmentVariableTarget.Process);
			}
			_isLoaded = true;
		}

		public void Unload()
		{
			foreach (var setting in _previousState)
			{
				Environment.SetEnvironmentVariable(setting.Key, setting.Value, EnvironmentVariableTarget.Process);
			}
			_isLoaded = false;
		}
	}

	public interface ISettingsProvider
	{
		IEnumerable<Setting> Get();
	}

	internal class JsonSettingsProvider : ISettingsProvider
	{
		private readonly IFileSystem _fileSystem;
		private readonly EnvFileLoadSettings _loadSettings;
		private IEnumerable<Setting> _settings;

		public JsonSettingsProvider()
			: this(null)
		{
		}

		internal JsonSettingsProvider(IFileSystem fileSystem):this(fileSystem, EnvFileLoadSettings.None)
		{
			
		}

		internal JsonSettingsProvider(IFileSystem fileSystem, EnvFileLoadSettings loadSettings)
		{
			_fileSystem = fileSystem ?? new FileSystem();
			_loadSettings = loadSettings;
		}

		public IEnumerable<Setting> Get()
		{
			return _settings ?? (_settings = ReadSettings());
		}

		private IEnumerable<Setting> ReadSettings()
		{
			if (!_fileSystem.File.Exists(".env"))
			{
				if (_loadSettings.HasFlag(EnvFileLoadSettings.ThrowOnMissingFile))
				{
					throw new DotEnvConfigFileException(DotEnvConfigFileException.FileMissingMessage);
				}
				return Enumerable.Empty<Setting>();
			}
			var settingsContent = _fileSystem.File.ReadAllText(".env");
			try
			{
				var settingsDict = SimpleJson.DeserializeObject<IDictionary<string, string>>(settingsContent);
				return settingsDict.Select(a => new Setting(a.Key, a.Value));
			}
			catch (SerializationException ex)
			{
				if (_loadSettings.HasFlag(EnvFileLoadSettings.ThrowOnInvalidFile))
				{
					throw new DotEnvConfigFileException(DotEnvConfigFileException.FileInvalidMessage, ex);	
				}
			}
			return Enumerable.Empty<Setting>();
		}
	}

	//Based on the implementation @ http://funq.codeplex.com/SourceControl/latest#src/Core/Funq/ServiceKey.cs
	public class Setting
	{
		public string Key { get; private set; }

		public string Value { get; private set; }

		private readonly int hash;

		public Setting(string key, string value)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentException("Cannot be null or empty", "key");
			}
			Key = key;
			Value = value;

			hash = key.GetHashCode();
			if (!String.IsNullOrEmpty(value))
				hash ^= value.GetHashCode();
		}

		public bool Equals(Setting other)
		{
			return Equals(this, other);
		}

		public override bool Equals(object obj)
		{
			return Equals(this, obj as Setting);
		}

		public static bool Equals(Setting obj1, Setting obj2)
		{
			if (Object.Equals(null, obj1) ||
			    Object.Equals(null, obj2))
				return false;

			return obj1.Key == obj2.Key &&
			       obj1.Value == obj2.Value;
		}

		public override int GetHashCode()
		{
			return hash;
		}

		public static bool operator ==(Setting first, Setting second)
		{
			return Equals(first, second);
		}

		public static bool operator !=(Setting first, Setting second)
		{
			return !Equals(first, second);
		}
	}
}