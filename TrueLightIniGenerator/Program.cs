using System.IO;
using Noggog;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

namespace TrueLightIniGenerator
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
            Directory.SetCurrentDirectory(env.DataFolderPath.Path);
            var dataPath = env.DataFolderPath.Path;
            var iniPath = "LightPlacer\\True Light.ini";

            var ini = new string[]
            {
                "[Settings]",
                "bShowMarkers = false",
                "",
                "[LightWhiteList]",
                "Window Shadows Ultimate.esp",
                "Window Shadows Ultimate Supplement.esp",
                "True Light - Shadows and Ambient.esp",
                "CS Light.esp",
                "NOTWL - Lanterns.esp",
                ""
            };

            if (File.Exists(iniPath))
            {
                Console.WriteLine("True Light.ini exists, using existing settings and whitelist");
                ini = [.. File.ReadAllLines(iniPath).TakeWhile(line => line.Trim() != "[LightBlackList]")];
            }
            else
            {
                Console.WriteLine("True Light.ini does not exist, using default settings and whitelist");
                iniPath = "True Light.ini";
            }

            var lights = env.LoadOrder.PriorityOrder.Light().WinningOverrides().Select(l => l.FormKey);
            var mods = env.LoadOrder.PriorityOrder.Select(x => x.Mod).Where(x => x is not null && FilterMod(x.ModKey));

            Console.WriteLine("Generating blacklist...");
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

            foreach (var mod in env.LoadOrder.ListedOrder)
                if (blacklist.Contains(mod.ModKey))
                    Console.WriteLine(mod.FileName);

            Console.WriteLine("\nWriting output...");
            using StreamWriter outputFile = new(iniPath);
            ini.ForEach(outputFile.WriteLine);
            outputFile.WriteLine("[LightBlackList]");
            foreach (var mod in env.LoadOrder.ListedOrder)
                if (blacklist.Contains(mod.ModKey))
                    outputFile.WriteLine(mod.FileName);
            Console.WriteLine($"Output written to {iniPath}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
