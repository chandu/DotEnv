using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Machine.Specifications;

namespace DotEnv.Specs.Integration
{
	internal class DotEnvIntegrationSpecs
	{
		public static readonly Dictionary<string, string> EnvSettings = new Dictionary<string, string>
		{
			{"Eagles", "Rock"},
			{"Giants", "Suck"},
			{"Steelers", "AreOk"},
			{"Cowboys", "AreOk"}
		};

		private static void CreateEnvFile()
		{
			var content = new StringBuilder();
			content.Append("{");
			foreach (var envSetting in EnvSettings)
			{
				content.Append(string.Format(@"""{0}"":""{1}""", envSetting.Key, envSetting.Value));
			}
			content.Append("}");
			File.WriteAllText(".env", content.ToString());
		}

		private static void RemoveEnvFile()
		{
			File.Delete(".env");
		}

		private static string GetEnv(string key)
		{
			return Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process);
		}

		public class When_environment_is_installed
		{
			private Establish context = () => CreateEnvFile();

			private Because of = () =>
				DotEnvConfig.Install();

			private It should_set_environment_variables_from_env_file_in_process_scope = () =>
			{
				GetEnv("Eagles").ShouldEqual("Rock");
				GetEnv("Giants").ShouldEqual("Suck");
				GetEnv("Cowboys").ShouldEqual("AreOk");
				GetEnv("Steelers").ShouldEqual("AreOk");
			};

			private Cleanup cleanup = () => RemoveEnvFile();
		}

		public class When_environment_is_uninstalled
		{
			private Establish context = () =>
			{
				CreateEnvFile();
				DotEnvConfig.Install();
			};

			private Because of = () =>
				DotEnvConfig.Uninstall();

			private It should_reset_the_environment_variables_in_process_scope = () =>
			{
				GetEnv("Eagles").ShouldBeNull();
				GetEnv("Giants").ShouldBeNull();
				GetEnv("Cowboys").ShouldBeNull();
				GetEnv("Steelers").ShouldBeNull();
			};

			private Cleanup cleanup = () => RemoveEnvFile();
		}
	}
}