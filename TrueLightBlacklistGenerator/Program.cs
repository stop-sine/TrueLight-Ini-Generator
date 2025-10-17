using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.FormKeys.SkyrimSE;



namespace TrueLightBlacklistGenerator
{
    public class Program
    {

        public static bool FilterMod(ModKey modKey)
        {
            if (modKey == Skyrim.ModKey) return false;
            if (modKey == Update.ModKey) return false;
            if (modKey == Dawnguard.ModKey) return false;
            if (modKey == Dragonborn.ModKey) return false;
            if (modKey == HearthFires.ModKey) return false;
            if (modKey.Name.StartsWith("cc") || modKey.Name == "_ResourcePack") return false;
            if (modKey.Name == "TL Bulbs ISL" || modKey.Name == "Window Shadows Ultimate Supplement" || modKey.Name == "Window Shadows Ultimate") return false;
            return true;
        }

        public static void Main(string[] args)
        {
            using var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
            var cache = env.LinkCache;

            var lights = env.LoadOrder.PriorityOrder.Light().WinningOverrides().Select(l => l.FormKey).ToList();
            var mods = env.LoadOrder.PriorityOrder.Select(x => x.Mod).Where(x => x is not null && FilterMod(x.ModKey)).ToList();
            var blacklist = new List<ModKey>
            {
                Skyrim.ModKey,
                Update.ModKey,
            };
            foreach (var mod in mods)
            {
                var cells = mod!.Cells.SelectMany(x => x.SubBlocks).SelectMany(x => x.Cells);
                var interiorCells = cells.Where(x => x.Flags.HasFlag(Cell.Flag.IsInteriorCell));
                if (!interiorCells.Any()) continue;
                foreach (var cell in interiorCells.Where(x => !FilterMod(x.FormKey.ModKey)))
                {
                    if (blacklist.Contains(mod.ModKey)) break;
                    foreach (var placed in cell.Temporary)
                    {
                        if (placed is not IPlacedObjectGetter placedObject) continue;
                        if (placed.FormKey.ModKey != mod.ModKey) continue;
                        if (!lights.Contains(placedObject.Base.FormKey)) continue;
                        blacklist.Add(mod.ModKey);
                        break;
                    }
                }
            }

            using (StreamWriter outputFile = new("TrueLightBlacklist.txt"))
                foreach (var mod in env.LoadOrder.ListedOrder)
                    if (blacklist.Contains(mod.ModKey))
                        outputFile.WriteLine(mod.FileName);
            Console.WriteLine("[LightBlackList]");
            foreach (var mod in env.LoadOrder.ListedOrder)
                if (blacklist.Contains(mod.ModKey))
                    Console.WriteLine(mod.FileName);
            Console.WriteLine("\nOutput written to TrueLightBlacklist.txt");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
