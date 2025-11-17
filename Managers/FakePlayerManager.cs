using System;
using System.Collections.Generic;
using FakePlayers.Config;
using FakePlayers.Patches;
using MelonLoader;
using ReluNetwork.ConstEnum;
using ReluProtocol;
using ReluProtocol.Enum;

namespace FakePlayers.Managers
{
	internal static class FakePlayerManager
	{
		private static readonly List<SessionContext> _fakeSessions = new List<SessionContext>();
		private static long _fakePlayerUIDCounter = 1000000;
		private static ulong _fakeSteamIDCounter = 76561198000000000UL;

		internal static void CreateFakePlayers(VWorld vworld, GameSessionInfo gameSessionInfo)
		{
			if (!FakePlayersPreferences.Enabled)
			{
				return;
			}

			int fakePlayerCount = FakePlayersPreferences.FakePlayerCount;
			if (fakePlayerCount <= 0)
			{
				return;
			}

			try
			{
				MelonLogger.Msg($"Creating {fakePlayerCount} fake players...");

				var serverDispatchersField = typeof(VWorld).GetField("_serverDispatchers", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				if (serverDispatchersField == null)
				{
					MelonLogger.Error("Could not find _serverDispatchers field in VWorld");
					return;
				}

				var serverDispatchers = serverDispatchersField.GetValue(vworld);
				if (serverDispatchers == null)
				{
					MelonLogger.Error("_serverDispatchers is null in VWorld");
					return;
				}

				var sessionManagerField = typeof(VWorld).GetField("_sessionManager", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				if (sessionManagerField == null)
				{
					MelonLogger.Error("Could not find _sessionManager field in VWorld");
					return;
				}

				var sessionManager = sessionManagerField.GetValue(vworld);
				if (sessionManager == null)
				{
					MelonLogger.Error("_sessionManager is null in VWorld");
					return;
				}

				var getNewSessionIDMethod = sessionManager.GetType().GetMethod("GetNewSessionID");
				if (getNewSessionIDMethod == null)
				{
					MelonLogger.Error("Could not find GetNewSessionID method in SessionManager");
					return;
				}

				for (int i = 0; i < fakePlayerCount; i++)
				{
					try
					{
						ulong fakeSteamID = _fakeSteamIDCounter + (ulong)i;
						long fakePlayerUID = _fakePlayerUIDCounter + i;
						string fakeName = $"FakePlayer{i + 1}";
						string fakeGUID = System.Guid.NewGuid().ToString();
						
						var virtualSessionType = typeof(VirtualAcceptSession);
						var virtualSession = Activator.CreateInstance(virtualSessionType, serverDispatchers);
						
						int newSessionId = (int)getNewSessionIDMethod.Invoke(sessionManager, null);
						VirtualAcceptSessionPatches.SetSessionIdMapping(virtualSession, newSessionId);
						
						var sessionContextType = typeof(SessionContext);
						var sessionContext = Activator.CreateInstance(sessionContextType, virtualSession);
						
						var setContextMethod = virtualSessionType.GetMethod("SetContext");
						setContextMethod?.Invoke(virtualSession, new object[] { sessionContext });
						
						var loginMethod = sessionContextType.GetMethod("Login");
						if (loginMethod == null)
						{
							MelonLogger.Error($"[FakePlayerManager] Could not find Login method in SessionContext");
							continue;
						}
						
						loginMethod.Invoke(sessionContext, new object[] 
						{ 
							fakePlayerUID, 
							fakeGUID, 
							fakeSteamID, 
							fakeName, 
							string.Empty,
							false,
							0
						});
						
						var playerSnapshotProperty = sessionContextType.GetProperty("PlayerInfoSnapshot");
						if (playerSnapshotProperty != null)
						{
							var snapshot = playerSnapshotProperty.GetValue(sessionContext);
							if (snapshot == null)
							{
								MelonLogger.Error($"[FakePlayerManager] PlayerInfoSnapshot is NULL after Login for {fakeName} - CreatePlayer will fail!");
								continue;
							}
						}
						
						System.Threading.Thread.Sleep(50);
						
						bool loginSuccessful = false;
						try
						{
							var totalPlayerSteamIDsProperty = typeof(GameSessionInfo).GetProperty("TotalPlayerSteamIDs", 
								System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
							if (totalPlayerSteamIDsProperty != null)
							{
								var totalPlayerSteamIDs = totalPlayerSteamIDsProperty.GetValue(gameSessionInfo);
								if (totalPlayerSteamIDs != null)
								{
									var containsKeyMethod = totalPlayerSteamIDs.GetType().GetMethod("ContainsKey");
									if (containsKeyMethod != null)
									{
										loginSuccessful = (bool)containsKeyMethod.Invoke(totalPlayerSteamIDs, new object[] { fakeSteamID });
									}
								}
							}
						}
						catch (Exception ex)
						{
							MelonLogger.Error($"[FakePlayerManager] Exception checking if player was added after login: {ex.Message}");
						}
						
						if (loginSuccessful)
						{
							var addMethod = sessionManager.GetType().GetMethod("Add");
							addMethod?.Invoke(sessionManager, new object[] { sessionContext });
							
							_fakeSessions.Add((SessionContext)sessionContext);
							MelonLogger.Msg($"Created fake player: {fakeName} (SteamID: {fakeSteamID}, UID: {fakePlayerUID})");
						}
						else
						{
							MelonLogger.Warning($"Failed to login fake player {fakeName} - player was not added to session");
						}
					}
					catch (Exception ex)
					{
						MelonLogger.Error($"Error creating fake player {i + 1}: {ex.Message}\n{ex.StackTrace}");
					}
				}

				MelonLogger.Msg($"Successfully created {_fakeSessions.Count} fake players.");
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"Error in CreateFakePlayers: {ex.Message}\n{ex.StackTrace}");
			}
		}

		internal static void RemoveFakePlayers(GameSessionInfo gameSessionInfo)
		{
			try
			{
				foreach (var session in _fakeSessions)
				{
					try
					{
						var steamIDProperty = typeof(SessionContext).GetProperty("SteamID");
						if (steamIDProperty != null)
						{
							ulong steamID = (ulong)steamIDProperty.GetValue(session);
							gameSessionInfo.RemoveSteamID(steamID);
						}
						
						var sessionProperty = typeof(SessionContext).GetProperty("Session");
						if (sessionProperty != null)
						{
							var virtualSession = sessionProperty.GetValue(session);
							if (virtualSession != null)
							{
								VirtualAcceptSessionPatches.RemoveSessionIdMapping(virtualSession);
							}
						}
						
						session?.Dispose();
					}
					catch (Exception ex)
					{
						MelonLogger.Error($"Error removing fake session: {ex.Message}");
					}
				}
				
				_fakeSessions.Clear();
				VirtualAcceptSessionPatches.ClearSessionIdMappings();
				MelonLogger.Msg("Removed all fake players.");
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"Error in RemoveFakePlayers: {ex.Message}\n{ex.StackTrace}");
			}
		}
	}
}

