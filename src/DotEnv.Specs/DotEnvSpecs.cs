using System;
using Machine.Specifications;
using Moq;
using It = Machine.Specifications.It;

namespace DotEnv.Specs
{
	internal class EnvironmentSpecsBase
	{
		protected static Exception _exception;
		protected static EnvironmentImpl _environment;
		protected static Mock<ISettingsProvider> _settingsProviderMock;

		private Establish context = () =>
		{
			_settingsProviderMock = new Mock<ISettingsProvider>();
			_settingsProviderMock
				.Setup(a => a.Get())
				.Returns(new[]
				{
					new Setting("BatMan", "Chandu"),
					new Setting("SomeExistingKey", "They killed Kenny!!!"),
					new Setting("DotEnvTesting_IGNORE", "Dang it! Bobby."),
				});
			_environment = new EnvironmentImpl(_settingsProviderMock.Object);
			Environment.SetEnvironmentVariable("SomeExistingKey", "SomeExistingValue", EnvironmentVariableTarget.Process);
			Environment.SetEnvironmentVariable("DotEnvTesting_IGNORE", "Doh", EnvironmentVariableTarget.Machine);
			
		};
	}

	[Subject(typeof(EnvironmentImpl), ".Load")]
	internal class When_Environment_is_loaded : EnvironmentSpecsBase
	{
		private Because of = () =>
			_exception = Catch.Exception(() => _environment.Load());

		private It should_not_throw = () =>
			_exception.ShouldBeNull();

		private It should_set_environment_vairables_from_settings_provider = () =>
			Environment.GetEnvironmentVariable("BatMan").ShouldEqual("Chandu");

		private It should_set_environment_vairables_in_process_scope = () =>
			Environment.GetEnvironmentVariable("SomeExistingKey", EnvironmentVariableTarget.Machine).ShouldEqual("SomeExistingValue");

		private It should_take_snapshot_of_existing_environment = () =>
			_environment._previousState.ShouldNotBeEmpty();

		private It should_override_existing_values_if_exists = () =>
		{
			Environment.GetEnvironmentVariable("DotEnvTesting_IGNORE", EnvironmentVariableTarget.Machine).ShouldEqual("Doh");
			Environment.GetEnvironmentVariable("DotEnvTesting_IGNORE", EnvironmentVariableTarget.Process).ShouldEqual("Dang it! Bobby.");
		};

	}

	[Subject(typeof(EnvironmentImpl), ".Load")]
	internal class When_Environment_is_already_loaded : EnvironmentSpecsBase
	{
		private Because of = () =>
		{
			_environment.Load();
			_environment.Load();
		};

		private It should_ignore_subsequent_calls_to_Load = () =>
			_settingsProviderMock.Verify(a => a.Get(), Times.AtMostOnce);
	}

	[Subject(typeof(EnvironmentImpl), ".Unload")]
	internal class When_Environment_is_unloaded : EnvironmentSpecsBase
	{
		private Establish context = () =>
		{
			_settingsProviderMock = new Mock<ISettingsProvider>();
			_settingsProviderMock
				.Setup(a => a.Get())
				.Returns(new[]
				{
					new Setting("SpiderMan", "Chandu"),
					new Setting("SomeExistingKey", "They killed Kenny!!!"),
					new Setting("DotEnvTesting_IGNORE", "Dang it! Bobby."),
				});
			_environment = new EnvironmentImpl(_settingsProviderMock.Object);
			Environment.SetEnvironmentVariable("SomeExistingKey", "SomeExistingValue", EnvironmentVariableTarget.Process);
			Environment.SetEnvironmentVariable("DotEnvTesting_IGNORE", "Doh", EnvironmentVariableTarget.Machine);

		};

		private Because of = () =>
			_exception = Catch.Exception(() => _environment.Unload());

		private It should_not_throw = () =>
			_exception.ShouldBeNull();

		private It should_restore_previous_values_for_environment = () =>
			Environment.GetEnvironmentVariable("SomeExistingKey").ShouldEqual("SomeExistingValue");

		private It should_remove_new_values_added_to_environment = () =>
			Environment.GetEnvironmentVariable("SpiderMan", EnvironmentVariableTarget.Process).ShouldBeNull();
	}

	[Subject(typeof(EnvironmentImpl), ".Load")]
	internal class When_loading_environment_after_calling_unload : EnvironmentSpecsBase
	{
		private Establish context = () =>
		{
			_environment.Load();
			_environment.Unload();
		};
		private Because of = () =>
		_environment.Load();

		private It should_reload_the_environment = () =>
			Environment.GetEnvironmentVariable("SomeExistingKey").ShouldEqual("They killed Kenny!!!");
	}
}