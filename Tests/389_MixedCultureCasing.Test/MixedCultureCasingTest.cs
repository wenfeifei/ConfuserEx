﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confuser.Core;
using Confuser.Core.Project;
using Confuser.UnitTest;
using Xunit;
using Xunit.Abstractions;

namespace MixedCultureCasing.Test
{
    public class MixedCultureCasingTest : TestBase {
		public MixedCultureCasingTest(ITestOutputHelper outputHelper) : base(outputHelper) { }

		[Theory]
		[MemberData(nameof(MixedCultureCasingData))]
		[Trait("Category", "Protection")]
		[Trait("Protection", "rename")]
		[Trait("Issue", "https://github.com/mkaring/ConfuserEx/issues/389")]
		public async Task MixedCultureCasing(string framework) =>
			await Run(
				framework,
				new [] {
					"389_MixedCultureCasing.exe",
					@"de-DE\389_MixedCultureCasing.resources.dll"
				},
				new [] {
					"Test 1 (neutral)",
					"Test 1 (deutsch)",
					"Test 2 (neutral)",
					"Test 2 (deutsch)"
				},
				new SettingItem<IProtection>("rename")
			);

		public static IEnumerable<object[]> MixedCultureCasingData() {
			foreach (var framework in "net40;net471".Split(';'))
				yield return new object[] { framework };
		}
	}
}
