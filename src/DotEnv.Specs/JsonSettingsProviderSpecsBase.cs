using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Machine.Specifications;

namespace DotEnv.Specs
{
	[Subject(typeof(JsonSettingsProvider), ".Get")]
	internal class JsonSettingsProviderSpecs
	{
		private static JsonSettingsProvider _provider;
		private static IEnumerable<Setting> _settings;

		public class When_env_file_is_not_present
		{
			protected static DotEnvConfigFileException _dotEnvFileException;
			protected static IFileSystem _fileSystem;

			protected Establish context = () =>
			{
				_fileSystem = new MockFileSystem();
				_provider = new JsonSettingsProvider(_fileSystem);
			};

			private Because of = () =>
				_dotEnvFileException = Catch.Exception(() => _settings = _provider.Get()) as DotEnvConfigFileException;
		}

		public class and_load_settings_are_set_to_fail_on_missing_file : When_env_file_is_not_present
		{
			private Establish context = () =>
				_provider = new JsonSettingsProvider(_fileSystem, new EnvFileLoadSettings
				{
					ShouldFailOnMissingConfigFile = true
				});

			private It should_throw_invalid_EnvConfigFile_exception = () =>
			{
				_dotEnvFileException.ShouldNotBeNull();
				_dotEnvFileException.Message.ShouldEqual(DotEnvConfigFileException.FileMissingMessage);
			};
		}

		public class and_load_settings_are_set_not_to_fail_on_missing_file : When_env_file_is_not_present
		{
			private Establish context = () =>
				_provider = new JsonSettingsProvider(_fileSystem, new EnvFileLoadSettings
				{
					ShouldFailOnMissingConfigFile = false
				});
		}

		public class When_valid_env_file_is_present
		{
			private Establish context = () =>
			{
				var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
				{
					{".env", new MockFileData(@"{
                        ""SOME_PATH"":""LALALALALA""
                    }")}
				});
				_provider = new JsonSettingsProvider(fileSystem);
			};

			private Because of = () =>
				_settings = _provider.Get();

			private It should_read_all_settings_from_env_file = () =>
			{
				_settings.ShouldNotBeEmpty();
				_settings.ShouldContain(new Setting("SOME_PATH", "LALALALALA"));
			};
		}

		public class When_env_file_is_in_invalid_format
		{
			protected static DotEnvConfigFileException _dotEnvFileException;
			protected static IFileSystem _fileSystem;

			protected Establish context = () =>
			{
				_fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>()
				{
					{".env", new MockFileData(@"I work with propane and proane accessories.")}
				});
				_provider = new JsonSettingsProvider(_fileSystem);
			};

			private Because of = () =>
				_dotEnvFileException = Catch.Exception(() => _settings = _provider.Get()) as DotEnvConfigFileException;
		}

		public class and_load_settings_are_set_to_ignore_on_invalid_file : When_env_file_is_in_invalid_format
		{
			private Establish context = () =>
				_provider = new JsonSettingsProvider(_fileSystem, new EnvFileLoadSettings
				{
					ShouldFailOnInvalidConfigFile = false
				});

			private It should_not_throw_invalid_EnvConfigFile_exception = () =>
				_dotEnvFileException.ShouldBeNull();
		}

		public class and_load_settings_are_set_to_fail_on_invalid_file : When_env_file_is_in_invalid_format
		{
			private Establish context = () =>
				_provider = new JsonSettingsProvider(_fileSystem, new EnvFileLoadSettings
				{
					ShouldFailOnInvalidConfigFile = true
				});

			private It should_throw_invalid_EnvConfigFile_exception = () =>
			{
				_dotEnvFileException.ShouldNotBeNull();
				_dotEnvFileException.Message.ShouldEqual(DotEnvConfigFileException.FileInvalidMessage);
			};
		}
	}
}