using GrassRandoV2.IC;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Internal;
using ItemChanger.Modules;
using ItemChanger.Placements;
using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrassyKnight.Rando
{
    public class GrassRandoModule : Module
    {
        private static SimpleLogger _logger = new("GrassyKnight.Rando");

        public override ModuleHandlingFlags ModuleHandlingProperties => ModuleHandlingFlags.AllowDeserializationFailure;

        public override void Initialize()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ResetSceneStats;
        }

        public override void Unload()
        {
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ResetSceneStats;
        }

        private void ResetSceneStats(Scene oldScene, Scene scene)
        {
            try
            {
                ResetSceneStats(oldScene);
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Error resetting stats for {oldScene.name} on exit\n" + ex);
            }

            try
            {
                ResetSceneStats(scene);
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Error resetting stats for {scene.name} on entry\n" + ex);
            }
        }

        private void ResetSceneStats(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.name)) return;

            IEnumerable<BreakableGrassLocation> GrassLocations = Ref.Settings?.Placements?.Values
                .OfType<IPrimaryLocationPlacement>()
                .Select(pmt => pmt.Location)
                .Where(loc => loc.sceneName == scene.name)
                .OfType<BreakableGrassLocation>()
                ?? Enumerable.Empty<BreakableGrassLocation>();

            foreach (BreakableGrassLocation loc in GrassLocations)
            {
                GameObject obj = scene.FindGameObjectByName(loc.objectName);
                if (obj == null)
                {
                    _logger.LogWarn($"Failed to find {loc.objectName} in {scene.name}");
                }

                bool hasChecked = loc.Placement.CheckVisitedAny(VisitState.ObtainedAnyItem);

                bool changed = GrassyKnight.Instance.GrassStates.TrySet(
                    GrassKey.FromGameObject(obj),
                    hasChecked ? GrassState.Cut : GrassState.Uncut,
                    allowUncut: true
                );
            }
        }
    }
}
