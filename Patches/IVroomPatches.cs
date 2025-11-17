using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using FakePlayers.Config;
using FakePlayers.Managers;
using MelonLoader;
using ReluProtocol;

namespace FakePlayers.Patches
{
	/// <summary>
	/// Patches for IVroom to monitor room entry process for fake players.
	/// </summary>
	internal static class IVroomPatches
	{
		/// <summary>
		/// Patch EnterRoom to log when fake players try to enter a room, and also log host room entries.
		/// </summary>
		[HarmonyPatch(typeof(IVroom), "EnterRoom")]
		internal static class EnterRoomPatch
		{
			private static void Postfix(IVroom __instance, SessionContext context, ReluProtocol.Enum.MsgErrorCode __result)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return;
				}

				try
				{
					var steamIDProperty = typeof(SessionContext).GetProperty("SteamID");
					if (steamIDProperty != null)
					{
						ulong steamID = (ulong)steamIDProperty.GetValue(context);
						
						// Check if this is the host player
						var playerSnapshotProperty = typeof(SessionContext).GetProperty("PlayerInfoSnapshot");
						bool isHost = false;
						if (playerSnapshotProperty != null)
						{
							var snapshot = playerSnapshotProperty.GetValue(context);
							if (snapshot != null)
							{
								var isHostProperty = snapshot.GetType().GetProperty("IsHost");
								if (isHostProperty != null)
								{
									isHost = (bool)isHostProperty.GetValue(snapshot);
								}
							}
						}
						
						var getPlayerUIDMethod = typeof(SessionContext).GetMethod("GetPlayerUID");
						long playerUID = 0;
						if (getPlayerUIDMethod != null)
						{
							playerUID = (long)getPlayerUIDMethod.Invoke(context, null);
						}
						
						// Log host player room entry
						if (isHost)
						{
							MelonLogger.Msg($"[IVroomPatches] 🏠 HOST player (SteamID: {steamID}, UID: {playerUID}) EnterRoom result: {__result}, Room type: {__instance.GetType().Name}, RoomID: {__instance.RoomID}");
						}
						// Check if this is a fake player (starts with our fake SteamID range)
						else if (steamID >= 76561198000000000UL && steamID <= 76561198000000010UL)
						{
							MelonLogger.Msg($"[IVroomPatches] Fake player (SteamID: {steamID}, UID: {playerUID}) EnterRoom result: {__result}, Room type: {__instance.GetType().Name}, RoomID: {__instance.RoomID}");
						}
					}
				}
				catch (Exception)
				{
					// Don't log errors for this debug patch
				}
			}
		}

		/// <summary>
		/// Patch CanEnterChannel to allow more than 4 players for fake players.
		/// </summary>
		[HarmonyPatch(typeof(IVroom), "CanEnterChannel")]
		internal static class CanEnterChannelPatch
		{
			private static bool Prefix(IVroom __instance, long playerUID, out ReluProtocol.Enum.MsgErrorCode __result)
			{
				__result = ReluProtocol.Enum.MsgErrorCode.Success;
				
				if (!FakePlayersPreferences.Enabled)
				{
					return true; // Call original method
				}

				try
				{
					// Check if player already exists
					var vPlayerDictField = typeof(IVroom).GetField("_vPlayerDict",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					if (vPlayerDictField != null)
					{
						var vPlayerDict = vPlayerDictField.GetValue(__instance);
						if (vPlayerDict != null)
						{
							var valuesProperty = vPlayerDict.GetType().GetProperty("Values");
							if (valuesProperty != null)
							{
								var players = valuesProperty.GetValue(vPlayerDict);
								if (players is System.Collections.IEnumerable enumerable)
								{
									foreach (var player in enumerable)
									{
										var uidProperty = player.GetType().GetProperty("UID");
										if (uidProperty != null)
										{
											long existingUID = (long)uidProperty.GetValue(player);
											if (existingUID == playerUID)
											{
												MelonLogger.Error($"[IVroomPatches] CanEnterChannel: Player already exists. playerUID: {playerUID}");
												__result = ReluProtocol.Enum.MsgErrorCode.DuplicatePlayer;
												return false; // Skip original method
											}
										}
									}
								}
								
								// Check player count - allow unlimited for fake players
								var countProperty = vPlayerDict.GetType().GetProperty("Count");
								if (countProperty != null)
								{
									int currentCount = (int)countProperty.GetValue(vPlayerDict);
									
									// Check if this is a fake player UID (starts from 1000000)
									if (playerUID >= 1000000 && playerUID <= 1000010)
									{
										// Allow fake players regardless of count
										MelonLogger.Msg($"[IVroomPatches] CanEnterChannel: Allowing fake player (UID: {playerUID}) - current count: {currentCount}");
										__result = ReluProtocol.Enum.MsgErrorCode.Success;
										return false; // Skip original method
									}
									
									// For real players, use the original limit check
									if (currentCount >= 4)
									{
										MelonLogger.Error($"[IVroomPatches] CanEnterChannel: Player count is over limit. playerUID: {playerUID}, currentCount: {currentCount}");
										__result = ReluProtocol.Enum.MsgErrorCode.PlayerCountExceeded;
										return false; // Skip original method
									}
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					MelonLogger.Error($"[IVroomPatches] Error in CanEnterChannelPrefix: {ex.Message}");
				}
				
				return true; // Call original method
			}
		}

		/// <summary>
		/// Patch GetPlayerStartPoint to ensure it returns a valid spawn point for fake players.
		/// Uses Prefix to intercept and provide a spawn point if the original would return null.
		/// </summary>
		[HarmonyPatch(typeof(IVroom), "GetPlayerStartPoint")]
		internal static class GetPlayerStartPointPatch
		{
			private static bool Prefix(IVroom __instance, ref object __result)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return true; // Call original method
				}

				try
				{
					// First, try to get spawn points using reflection
					var playerStartSpawnPointsField = typeof(IVroom).GetField("_playerStartSpawnPoints",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					if (playerStartSpawnPointsField != null)
					{
						var spawnPoints = playerStartSpawnPointsField.GetValue(__instance);
						if (spawnPoints != null)
						{
							var countProperty = spawnPoints.GetType().GetProperty("Count");
							if (countProperty != null)
							{
								int count = (int)countProperty.GetValue(spawnPoints);
								
								if (count > 0)
								{
									// Dictionary has entries, let original method handle it
									return true; // Call original method
								}
								else
								{
									// Dictionary is empty - let original method try, then we'll fix it in Postfix
									return true;
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					MelonLogger.Error($"[IVroomPatches] Error in GetPlayerStartPointPrefix: {ex.Message}");
				}
				
				return true; // Call original method
			}
			
			private static void Postfix(IVroom __instance, ref object __result)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return;
				}

				try
				{
					if (__result == null)
					{
						MelonLogger.Error($"[IVroomPatches] ⚠ GetPlayerStartPoint returned NULL for {__instance.GetType().Name} - CreatePlayer will fail!");
						
						// Try to get a default spawn point as fallback
						var playerStartSpawnPointsField = typeof(IVroom).GetField("_playerStartSpawnPoints",
							System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
						if (playerStartSpawnPointsField != null)
						{
							var spawnPoints = playerStartSpawnPointsField.GetValue(__instance);
							if (spawnPoints != null)
							{
								var countProperty = spawnPoints.GetType().GetProperty("Count");
								if (countProperty != null)
								{
									int count = (int)countProperty.GetValue(spawnPoints);
									
									if (count > 0)
									{
										var valuesProperty = spawnPoints.GetType().GetProperty("Values");
										if (valuesProperty != null)
										{
											var values = valuesProperty.GetValue(spawnPoints);
											if (values is System.Collections.IEnumerable enumerable)
											{
												var firstMethod = typeof(System.Linq.Enumerable).GetMethods()
													.FirstOrDefault(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1);
												if (firstMethod != null)
												{
													// Get the actual type from the dictionary values
													var enumerableType = enumerable.GetType();
													var elementType = enumerableType.IsGenericType ? enumerableType.GetGenericArguments()[0] : null;
													
													if (elementType != null)
													{
														var genericMethod = firstMethod.MakeGenericMethod(new[] { elementType });
														var firstSpawnPoint = genericMethod.Invoke(null, new[] { enumerable });
														if (firstSpawnPoint != null)
														{
															__result = firstSpawnPoint;
														}
													}
												}
											}
										}
									}
									else
									{
										// Dictionary is empty - create a default spawn point at origin
										MelonLogger.Warning($"[IVroomPatches] _playerStartSpawnPoints is EMPTY - creating default spawn point at origin");
										
										try
										{
											// Create a default PosWithRot at origin
											var posWithRotType = System.Type.GetType("PosWithRot");
											if (posWithRotType != null)
											{
												var posWithRotConstructor = posWithRotType.GetConstructor(new System.Type[] { });
												if (posWithRotConstructor != null)
												{
													var defaultPos = posWithRotConstructor.Invoke(null);
													
													// Create a default SpawnPointData
													var spawnPointDataType = System.Type.GetType("SpawnPointData");
													if (spawnPointDataType != null)
													{
														var spawnPointConstructor = spawnPointDataType.GetConstructor(new System.Type[] 
														{ 
															typeof(int), 
															posWithRotType, 
															typeof(bool), 
															typeof(int) 
														});
														if (spawnPointConstructor != null)
														{
															var defaultSpawnPoint = spawnPointConstructor.Invoke(new object[] { 0, defaultPos, false, 0 });
															__result = defaultSpawnPoint;
														}
													}
												}
											}
										}
										catch (Exception ex)
										{
											MelonLogger.Error($"[IVroomPatches] Failed to create default spawn point: {ex.Message}");
										}
									}
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					MelonLogger.Error($"[IVroomPatches] Error in GetPlayerStartPointPostfix: {ex.Message}\n{ex.StackTrace}");
				}
			}
		}

		/// <summary>
		/// Patch ProcessEnterWaitQueue to log when fake players are being processed and check CreatePlayer results.
		/// </summary>
		[HarmonyPatch(typeof(IVroom), "ProcessEnterWaitQueue")]
		internal static class ProcessEnterWaitQueuePatch
		{
			private static void Prefix(IVroom __instance)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return;
				}

				try
				{
					// Get the enterWaitSessions queue using reflection
					var enterWaitSessionsField = __instance.GetType().GetField("_enterWaitSessions",
						System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					if (enterWaitSessionsField != null)
					{
						var enterWaitSessions = enterWaitSessionsField.GetValue(__instance);
						if (enterWaitSessions != null)
						{
							var countProperty = enterWaitSessions.GetType().GetProperty("Count");
							if (countProperty != null)
							{
								int queueCount = (int)countProperty.GetValue(enterWaitSessions);
								if (queueCount > 0)
								{
									// Check for fake players in queue
									try
									{
										if (enterWaitSessions is System.Collections.IEnumerable enumerable)
										{
											var sessionContextType = typeof(SessionContext);
											var firstMethod = typeof(System.Linq.Enumerable).GetMethods()
												.FirstOrDefault(m => m.Name == "FirstOrDefault" && m.GetParameters().Length == 1);
											if (firstMethod != null)
											{
												var genericMethod = firstMethod.MakeGenericMethod(new[] { sessionContextType });
												var firstSession = genericMethod.Invoke(null, new[] { enumerable });
												
												if (firstSession != null)
												{
													var steamIDProperty = sessionContextType.GetProperty("SteamID");
													if (steamIDProperty != null)
													{
														ulong steamID = (ulong)steamIDProperty.GetValue(firstSession);
														if (steamID >= 76561198000000000UL && steamID <= 76561198000000010UL)
														{
															var playerSnapshotProperty = sessionContextType.GetProperty("PlayerInfoSnapshot");
															if (playerSnapshotProperty?.GetValue(firstSession) == null)
															{
																MelonLogger.Error($"[IVroomPatches] ⚠ PlayerInfoSnapshot is NULL for fake player (SteamID: {steamID}) - CreatePlayer will likely fail!");
															}
														}
													}
												}
											}
										}
									}
									catch (Exception)
									{
										// Ignore errors in detailed logging
									}
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					MelonLogger.Error($"[IVroomPatches] Error in ProcessEnterWaitQueuePrefix: {ex.Message}");
				}
			}
			
			private static void Postfix(IVroom __instance)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return;
				}

				try
				{
					// Check member count after processing
					var getMemberCountMethod = typeof(IVroom).GetMethod("GetMemberCount");
					if (getMemberCountMethod != null)
					{
						int memberCount = (int)getMemberCountMethod.Invoke(__instance, null);
						
						// Check if there are still sessions in queue
						var enterWaitSessionsField = __instance.GetType().GetField("_enterWaitSessions",
							System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
						if (enterWaitSessionsField != null)
						{
							var enterWaitSessions = enterWaitSessionsField.GetValue(__instance);
							if (enterWaitSessions != null)
							{
								var countProperty = enterWaitSessions.GetType().GetProperty("Count");
								if (countProperty != null)
								{
									int queueCount = (int)countProperty.GetValue(enterWaitSessions);
									
									// If there are still sessions in queue but no new members, CreatePlayer might be failing
									if (queueCount > 0 && memberCount <= 1)
									{
										MelonLogger.Warning($"[IVroomPatches] ⚠ WARNING: {queueCount} sessions still in queue but only {memberCount} members in room - CreatePlayer may be failing!");
									}
								}
							}
						}
					}
				}
				catch (Exception)
				{
					// Ignore errors in postfix
				}
			}
		}

		/// <summary>
		/// Patch AddPlayer to log when fake players are added to the room.
		/// </summary>
		[HarmonyPatch(typeof(IVroom), "AddPlayer")]
		internal static class AddPlayerPatch
		{
			private static void Postfix(IVroom __instance, VPlayer player, int hashCode, ReluProtocol.Enum.MsgErrorCode __result)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return;
				}

				try
				{
					// Check if this is a fake player
					ulong steamID = player.SteamID;
					if (steamID >= 76561198000000000UL && steamID <= 76561198000000010UL)
					{
						MelonLogger.Msg($"[IVroomPatches] Fake player (SteamID: {steamID}, UID: {player.UID}, Name: {player.ActorName}) AddPlayer result: {__result}, Room: {__instance.GetType().Name}");
					}
				}
				catch (Exception)
				{
					// Don't log errors for this debug patch
				}
			}
		}

		/// <summary>
		/// Patch MaintenanceRoom.OnEnterRoomFailed to log when fake players fail to enter the room.
		/// </summary>
		[HarmonyPatch(typeof(MaintenanceRoom), "OnEnterRoomFailed")]
		internal static class OnEnterRoomFailedPatch
		{
			private static void Prefix(MaintenanceRoom __instance, SessionContext context, ReluProtocol.Enum.MsgErrorCode errorCode, int hashCode)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return;
				}

				try
				{
					var steamIDProperty = typeof(SessionContext).GetProperty("SteamID");
					if (steamIDProperty != null)
					{
						ulong steamID = (ulong)steamIDProperty.GetValue(context);
						if (steamID >= 76561198000000000UL && steamID <= 76561198000000010UL)
						{
							var getPlayerUIDMethod = typeof(SessionContext).GetMethod("GetPlayerUID");
							long playerUID = 0;
							if (getPlayerUIDMethod != null)
							{
								playerUID = (long)getPlayerUIDMethod.Invoke(context, null);
							}
							
							MelonLogger.Error($"[IVroomPatches] ⚠ OnEnterRoomFailed for fake player (SteamID: {steamID}, UID: {playerUID}): {errorCode}");
						}
					}
				}
				catch (Exception)
				{
					// Ignore errors
				}
			}
		}

		// NOTE: OnEnterRoomFailed is abstract in IVroom and cannot be patched directly.
		// We patch MaintenanceRoom.OnEnterRoomFailed instead to catch errors.

		/// <summary>
		/// Patch SessionContext.CreatePlayer to log when fake players are being created.
		/// </summary>
		[HarmonyPatch(typeof(SessionContext), "CreatePlayer")]
		internal static class CreatePlayerPatch
		{
			private static void Postfix(SessionContext __instance, int objectID, IVroom room, PosWithRot pos, bool isIndoor, VPlayer __result)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return;
				}

				try
				{
					var steamIDProperty = typeof(SessionContext).GetProperty("SteamID");
					if (steamIDProperty != null)
					{
						ulong steamID = (ulong)steamIDProperty.GetValue(__instance);
						if (steamID >= 76561198000000000UL && steamID <= 76561198000000010UL)
						{
							var getPlayerUIDMethod = typeof(SessionContext).GetMethod("GetPlayerUID");
							long playerUID = 0;
							if (getPlayerUIDMethod != null)
							{
								playerUID = (long)getPlayerUIDMethod.Invoke(__instance, null);
							}
							
							if (__result != null)
							{
								MelonLogger.Msg($"[IVroomPatches] ✓ CreatePlayer SUCCESS for fake player (SteamID: {steamID}, UID: {playerUID}, ObjectID: {__result.ObjectID}, Name: {__result.ActorName})");
							}
							else
							{
								MelonLogger.Error($"[IVroomPatches] ✗ CreatePlayer FAILED - returned null for fake player (SteamID: {steamID}, UID: {playerUID})");
								
								// Check PlayerInfoSnapshot
								var playerSnapshotProperty = typeof(SessionContext).GetProperty("PlayerInfoSnapshot");
								if (playerSnapshotProperty != null)
								{
									var snapshot = playerSnapshotProperty.GetValue(__instance);
									if (snapshot == null)
									{
										MelonLogger.Error($"[IVroomPatches] PlayerInfoSnapshot is NULL - this is why CreatePlayer failed!");
									}
									else
									{
										MelonLogger.Msg($"[IVroomPatches] PlayerInfoSnapshot exists: {snapshot.GetType().Name}");
									}
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					MelonLogger.Error($"[IVroomPatches] Error in CreatePlayerPostfix: {ex.Message}");
				}
			}
		}

		/// <summary>
		/// Patch OnEnterChannel to log when players successfully enter a channel (become visible in the room).
		/// </summary>
		[HarmonyPatch(typeof(IVroom), "OnEnterChannel")]
		internal static class OnEnterChannelPatch
		{
			private static void Postfix(IVroom __instance, VPlayer player, int hashCode, ReluProtocol.Enum.MsgErrorCode __result)
			{
				if (!FakePlayersPreferences.Enabled)
				{
					return;
				}

				try
				{
					// Check if this is the host player
					bool isHost = player.IsHost;
					ulong steamID = player.SteamID;
					
					// Log host player channel entry
					if (isHost)
					{
						MelonLogger.Msg($"[IVroomPatches] 🏠 HOST player (SteamID: {steamID}, UID: {player.UID}, Name: {player.ActorName}) OnEnterChannel result: {__result}, Room: {__instance.GetType().Name}, RoomID: {__instance.RoomID}");
					}
					// Check if this is a fake player
					else if (steamID >= 76561198000000000UL && steamID <= 76561198000000010UL)
					{
						MelonLogger.Msg($"[IVroomPatches] ✓ Fake player (SteamID: {steamID}, UID: {player.UID}, Name: {player.ActorName}) OnEnterChannel result: {__result}, Room: {__instance.GetType().Name}, RoomID: {__instance.RoomID}");
					}
				}
				catch (Exception)
				{
					// Don't log errors for this debug patch
				}
			}
		}
	}
}

