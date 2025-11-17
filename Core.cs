using FakePlayers.Config;
using MelonLoader;

[assembly: MelonInfo(typeof(FakePlayers.Core), "FakePlayers", "1.0.0", "DooDesch", null)]
[assembly: MelonGame("ReLUGames", "MIMESIS")]

namespace FakePlayers
{
	public sealed class Core : MelonMod
	{
		public override void OnInitializeMelon()
		{
			FakePlayersPreferences.Initialize();
			// Note: HarmonyInstance.PatchAll() is called automatically by MelonLoader via HarmonyInit()
			// Do NOT call it manually here to avoid double-patching errors
			
			var enabled = FakePlayersPreferences.Enabled;
			var fakePlayerCount = FakePlayersPreferences.FakePlayerCount;
			
			if (enabled)
			{
				MelonLogger.Msg($"FakePlayers initialized. Fake player count: {fakePlayerCount}");
			}
			else
			{
				MelonLogger.Msg("FakePlayers initialized but disabled.");
			}
		}
	}
}

