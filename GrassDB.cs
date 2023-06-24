using System;
using System.Collections.Generic;
using UnityEngine;


namespace GrassyKnight
{
    // Responsible for storing the status of grass and letting us run various
    // queries against all the grass.
    class GrassDB {
        // Maps from scene name to a dictionary mapping from grass key to
        // state. The seperation of grass by scene is done only for query
        // speed, since GrassKey has the scene name in it already.
        private Dictionary<string, Dictionary<GrassKey, GrassState>> _grassStates =
            new Dictionary<string, Dictionary<GrassKey, GrassState>>();

        private GrassStats _globalStats = new GrassStats();
        private Dictionary<string, GrassStats> _sceneStats = new Dictionary<string, GrassStats>();

        // Maps a grass's alias to its canonical key
        private Dictionary<GrassKey, GrassKey> _aliasToCanonical =
            new Dictionary<GrassKey, GrassKey>();

        public event EventHandler OnStatsChanged;

        // Clears all game state, but does not clear the alias table
        public void Clear() {
            _grassStates = new Dictionary<string, Dictionary<GrassKey, GrassState>>();
            _globalStats = new GrassStats();
            _sceneStats = new Dictionary<string, GrassStats>();

            OnStatsChanged?.Invoke(this, EventArgs.Empty);
        }

        private const string _serializationVersion = "1";

        // Serializes the DB into a single string.
        //
        // HollowKnight doesn't ship with
        // System.Runtime.Serialization.Formatters.dll so I don't think
        // it's safe to use a stdlib serializer... Thus we make our own.
        //
        // Format is simple. It's a series of strings seperated by semicolons.
        // First string is the version of the serialization formatter (in case
        // we need to change the format in a back-incompat way). That's
        // followed by GrassKey.NumSerializationTokens number of strings that
        // make up a single grass key, followed by a single string that holds
        // the state of that grass, then it repeats for however many grass
        // states are stored.
        public string Serialize() {
            var parts = new List<string>();

            parts.Add(_serializationVersion);

            foreach (Dictionary<GrassKey, GrassState> states in _grassStates.Values) {
                foreach (KeyValuePair<GrassKey, GrassState> kv in states) {
                    parts.AddRange(kv.Key.Serialize());
                    parts.Add(((int)kv.Value).ToString());
                }
            }

            return String.Join(";", parts.ToArray());
        }

        // Adds all the data in serialized. Will not call Clear() first so you
        // may want to... NOTE: will invoke OnStatsChanged a bunch 🤷‍♀️
        public void AddSerializedData(string serialized) {
            if (serialized == null || serialized == "") {
                return;
            }

            string[] parts = serialized.Split(';');

            if (parts[0] != _serializationVersion) {
                throw new ArgumentException(
                    $"Unknown serialization version {parts[0]}. You may " +
                    $"a new version of the mod to load this save file.");
            } else if ((parts.Length - 1) % (GrassKey.NumSerializationTokens + 1) != 0) {
                throw new ArgumentException("GrassDB in save data is corrupt");
            }

            string[] grassKeyParts = new string[GrassKey.NumSerializationTokens];
            for (int i = 1; i < parts.Length; i += GrassKey.NumSerializationTokens + 1) {
                // Copy just the parts for a single grass key into
                // grassKeyParts.
                Array.Copy(
                    parts, i,
                    grassKeyParts, 0,
                    GrassKey.NumSerializationTokens);
                GrassKey k = GrassKey.Deserialize(grassKeyParts);

                // Convert the one GrassState part into a GrassState
                GrassState state = (GrassState)int.Parse(
                    parts[i + GrassKey.NumSerializationTokens]);

                TrySet(k, state);
            }
        }

        private void TryAddScene(string sceneName) {
            // Try add isn't available in the stdlib we're building against :(
            // (I think... honestly I'm not convinced I'm not missing an
            // reference but I sure can't find where it is).
            if (!_grassStates.ContainsKey(sceneName)) {
                _grassStates.Add(sceneName,
                                new Dictionary<GrassKey, GrassState>());
            }

            if (!_sceneStats.ContainsKey(sceneName)) {
                _sceneStats.Add(sceneName, new GrassStats());
            }
        }

        public bool TrySet(GrassKey k, GrassState newState) => TrySet(k, newState, false);

        public bool TrySet(GrassKey k, GrassState newState, bool allowUncut) {
            GrassKey canonical = ToCanonical(k);

            TryAddScene(canonical.SceneName);

            GrassState? oldState = null;
            if (_grassStates[canonical.SceneName].TryGetValue(
                    canonical, out GrassState state)) {
                oldState = state;
            }

            if (oldState == null || (int)oldState < (int)newState || allowUncut) {
                _grassStates[canonical.SceneName][canonical] = newState;

                _sceneStats[canonical.SceneName].HandleUpdate(oldState, newState);
                _globalStats.HandleUpdate(oldState, newState);
                OnStatsChanged?.Invoke(this, EventArgs.Empty);

                GrassyKnight.Instance.LogDebug(
                    $"Updated state of '{canonical}' to {newState} (was {oldState})");
                GrassyKnight.Instance.LogFine(
                    $"... Serialized key: {String.Join(";", canonical.Serialize())}");

                return true;
            } else {
                return false;
            }
        }

        public bool Contains(GrassKey k) {
            GrassKey canonical = ToCanonical(k);
            if (_grassStates.TryGetValue(
                    canonical.SceneName,
                    out Dictionary<GrassKey, GrassState> sceneStates)) {
                return sceneStates.ContainsKey(canonical);
            } else {
                return false;
            }
        }

        public GrassState? TryGet(GrassKey k) {
            GrassKey canonical = ToCanonical(k);
            if (_grassStates.TryGetValue(
                    canonical.SceneName,
                    out Dictionary<GrassKey, GrassState> sceneStates)) {
                if (sceneStates.TryGetValue(canonical, out GrassState state)) {
                    return state;
                }
            }

            return null;
        }

        public GrassKey? GetNearestUncutGrass(Vector2 origin, string sceneName) {
            Dictionary<GrassKey, GrassState> grassStatesForScene;
            if (!_grassStates.TryGetValue(sceneName, out grassStatesForScene)) {
                return null;
            }

            GrassKey? closest = null;
            float closestDistance = float.PositiveInfinity;
            foreach (KeyValuePair<GrassKey, GrassState> kv in grassStatesForScene) {
                if (kv.Value != GrassState.Uncut) {
                    continue;
                }

                float currentDistance = Vector2.Distance(origin, kv.Key.Position);
                if (currentDistance < closestDistance) {
                    closest = kv.Key;
                    closestDistance = currentDistance;
                }
            }

            return closest;
        }

        public GrassStats GetStatsForScene(string sceneName) {
            if (_sceneStats.TryGetValue(sceneName, out GrassStats stats)) {
                return stats;
            } else {
                return new GrassStats();
            }
        }

        public GrassStats GetGlobalStats() {
            return _globalStats;
        }

        // A change to an alias's state will actually register as a change
        // to canonical's state. But an alias will not appear in stats or be
        // considered in GetNearestUncutGrass.
        public void AddAlias(GrassKey alias, GrassKey canonical) {
            _aliasToCanonical.Add(alias, canonical);
        }

        private GrassKey ToCanonical(GrassKey key) {
            if (_aliasToCanonical.TryGetValue(key, out GrassKey canonical)) {
                return canonical;
            } else {
                return key;
            }
        }
    }
}
