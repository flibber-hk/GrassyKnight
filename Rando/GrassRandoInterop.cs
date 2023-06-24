using GrassRandoV2.IC;
using ItemChanger.Placements;
using Modding;
using RandomizerMod.RC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrassyKnight.Rando
{
    internal class GrassRandoInterop
    {
        private static SimpleLogger _logger = new("GrassyKnight.Rando");

        public static void Hook()
        {
            if (ModHooks.GetMod("ItemChangerMod") is not null
                && ModHooks.GetMod("Randomizer 4") is not null
                && ModHooks.GetMod("GrassRandoV2") is not null
                )
            {
                HookInternal();
            }
        }

        private static void HookInternal()
        {

            _logger.Log("Hooking Rando");
            RandoController.OnExportCompleted += AddGrassModule;
        }

        private static void AddGrassModule(RandoController rc)
        {
            _logger.Log("Checking");

            if (ItemChanger.Internal.Ref.Settings.Placements.Values
                .OfType<IPrimaryLocationPlacement>()
                .Select(pmt => pmt.Location)
                .OfType<BreakableGrassLocation>()
                .Any()
                )
            {
                _logger.Log("TRUE");
                ItemChanger.ItemChangerMod.Modules.Add<GrassRandoModule>();
            }
        }
    }
}
