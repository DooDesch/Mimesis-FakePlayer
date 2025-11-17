using System;
using System.Collections.Generic;
using HarmonyLib;
using FakePlayers.Config;
using ReluNetwork.ConstEnum;

namespace FakePlayers.Patches
{
	internal static class VirtualAcceptSessionPatches
	{
		private static readonly Dictionary<object, int> _sessionIdMap = new Dictionary<object, int>();

		[HarmonyPatch(typeof(VirtualAcceptSession), "get_ID")]
		internal static class IDPropertyPatch
		{
			private static bool Prefix(VirtualAcceptSession __instance, ref int __result)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return true;
				}

				if (_sessionIdMap.TryGetValue(__instance, out int uniqueId))
				{
					__result = uniqueId;
					return false;
				}

				return true;
			}
		}

		internal static void SetSessionIdMapping(object virtualSession, int sessionId)
		{
			if (virtualSession != null)
			{
				_sessionIdMap[virtualSession] = sessionId;
			}
		}

		internal static void RemoveSessionIdMapping(object virtualSession)
		{
			if (virtualSession != null)
			{
				_sessionIdMap.Remove(virtualSession);
			}
		}

		internal static void ClearSessionIdMappings()
		{
			_sessionIdMap.Clear();
		}
	}
}

