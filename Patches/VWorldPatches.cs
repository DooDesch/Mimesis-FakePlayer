using System;
using HarmonyLib;
using FakePlayers.Config;
using FakePlayers.Managers;
using MelonLoader;
using ReluProtocol.Enum;

namespace FakePlayers.Patches
{
	/// <summary>
	/// Patches for VWorld to inject fake players after the game session is initialized.
	/// </summary>
	internal static class VWorldPatches
	{
		private static bool _fakePlayersCreated = false;

		/// <summary>
		/// Patch RegistPlayer to add fake players after the host player is registered.
		/// </summary>
		[HarmonyPatch(typeof(VWorld), "RegistPlayer")]
		internal static class RegistPlayerPatch
		{
			private static void Postfix(VWorld __instance, ulong steamID, bool isHost, MsgErrorCode __result)
			{
				// Only add fake players if the host registration was successful and we haven't created them yet
				if (!FakePlayersPreferences.Enabled || __result != MsgErrorCode.Success || !isHost || _fakePlayersCreated)
				{
					return;
				}

				try
				{
					// Get VRoomManager from VWorld
					var vRoomManager = __instance.VRoomManager;
					if (vRoomManager == null)
					{
						MelonLogger.Warning("VRoomManager is null, cannot add fake players");
						return;
					}

					// Get GameSessionInfo from VRoomManager using reflection
					var gameSessionInfoField = typeof(VRoomManager).GetField("_gameSessionInfo", 
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					if (gameSessionInfoField == null)
					{
						MelonLogger.Error("Could not find _gameSessionInfo field in VRoomManager");
						return;
					}

					var gameSessionInfo = gameSessionInfoField.GetValue(vRoomManager) as GameSessionInfo;
					if (gameSessionInfo == null)
					{
						MelonLogger.Error("_gameSessionInfo is null in VRoomManager");
						return;
					}

					// Create fake players immediately (the game should be ready by now)
					_fakePlayersCreated = true;
					FakePlayerManager.CreateFakePlayers(__instance, gameSessionInfo);
				}
				catch (Exception ex)
				{
					MelonLogger.Error($"Error in RegistPlayerPatch: {ex.Message}\n{ex.StackTrace}");
				}
			}
		}
	}
}

