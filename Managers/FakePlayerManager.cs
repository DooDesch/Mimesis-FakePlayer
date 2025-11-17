using System;
using System.Collections.Generic;
using FakePlayers.Config;
using MelonLoader;
using ReluProtocol;
using ReluProtocol.Enum;

namespace FakePlayers.Managers
{
	/// <summary>
	/// Manages fake players for testing purposes.
	/// Creates virtual sessions and adds them to the game session.
	/// </summary>
	internal static class FakePlayerManager
	{
		private static readonly List<SessionContext> _fakeSessions = new List<SessionContext>();
		private static long _fakePlayerUIDCounter = 1000000; // Start from a high number to avoid conflicts
		private static ulong _fakeSteamIDCounter = 76561198000000000UL; // Start from a fake Steam ID range

		/// <summary>
		/// Creates fake players and adds them to the game session.
		/// </summary>
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

				// Get the server dispatchers from VWorld using reflection
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

				// Create fake players
				for (int i = 0; i < fakePlayerCount; i++)
				{
					try
					{
						// Generate fake Steam ID
						ulong fakeSteamID = _fakeSteamIDCounter + (ulong)i;
						
						// Generate fake player UID
						long fakePlayerUID = _fakePlayerUIDCounter + i;
						
						// Generate fake name
						string fakeName = $"FakePlayer{i + 1}";
						
						// Generate fake GUID
						string fakeGUID = System.Guid.NewGuid().ToString();
						
						// Create VirtualAcceptSession
						var virtualSessionType = typeof(VirtualAcceptSession);
						var virtualSession = Activator.CreateInstance(virtualSessionType, serverDispatchers);
						
						// Create SessionContext
						var sessionContextType = typeof(SessionContext);
						var sessionContext = Activator.CreateInstance(sessionContextType, virtualSession);
						
						// Set context on virtual session
						var setContextMethod = virtualSessionType.GetMethod("SetContext");
						setContextMethod?.Invoke(virtualSession, new object[] { sessionContext });
						
						// Login the fake player (this will call RegistPlayer internally, which adds to game session)
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
							string.Empty, // voiceUID
							false, // isHost
							0 // hashCode
						});
						
						// Verify PlayerInfoSnapshot was created
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
						
						// Wait a tiny bit to ensure async operations complete
						System.Threading.Thread.Sleep(50);
						
						// Check if player was successfully added after login
						// Login internally calls RegistPlayer, so we don't need to call it again
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
							// Add session to session manager
							var sessionManagerField = typeof(VWorld).GetField("_sessionManager", 
								System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
							if (sessionManagerField != null)
							{
								var sessionManager = sessionManagerField.GetValue(vworld);
								var addMethod = sessionManager.GetType().GetMethod("Add");
								addMethod?.Invoke(sessionManager, new object[] { sessionContext });
							}
							
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

		/// <summary>
		/// Removes all fake players from the game session.
		/// </summary>
		internal static void RemoveFakePlayers(GameSessionInfo gameSessionInfo)
		{
			try
			{
				foreach (var session in _fakeSessions)
				{
					try
					{
						// Get SteamID from session
						var steamIDProperty = typeof(SessionContext).GetProperty("SteamID");
						if (steamIDProperty != null)
						{
							ulong steamID = (ulong)steamIDProperty.GetValue(session);
							gameSessionInfo.RemoveSteamID(steamID);
						}
						
						// Dispose session
						session?.Dispose();
					}
					catch (Exception ex)
					{
						MelonLogger.Error($"Error removing fake session: {ex.Message}");
					}
				}
				
				_fakeSessions.Clear();
				MelonLogger.Msg("Removed all fake players.");
			}
			catch (Exception ex)
			{
				MelonLogger.Error($"Error in RemoveFakePlayers: {ex.Message}\n{ex.StackTrace}");
			}
		}
	}
}

