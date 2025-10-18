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
        private static readonly ModKey[] BasePlugins =
        [
            Skyrim.ModKey, Update.ModKey, Dawnguard.ModKey,
            Dragonborn.ModKey, HearthFires.ModKey
        ];

        private static readonly string[] ExcludedPlugins =
        [
            "TL Bulbs ISL", "Window Shadows Ultimate Supplement", "Window Shadows Ultimate"
        ];

        private static readonly HashSet<ModKey> CreationClubPlugins = GetCreationClubPlugins(GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE).CreationClubListingsFilePath ?? string.Empty);

        public static HashSet<ModKey> GetCreationClubPlugins(FilePath CreationClubListingsFilePath)
        {
            try
            {
                if (!File.Exists(CreationClubListingsFilePath))
                    return [];

                return [.. File.ReadAllLines(CreationClubListingsFilePath)
                .Select(line => ModKey.TryFromFileName(new FileName(line)))
                .Where(plugin => plugin.HasValue)
                .Select(plugin => plugin!.Value)];
            }
            catch
            {
                return [];
            }
        }

        public static bool FilterMod(ModKey modKey)
        {
            return !BasePlugins.Contains(modKey) &&
                   !CreationClubPlugins.Contains(modKey) &&
                   !ExcludedPlugins.Contains(modKey.Name);
        }

        private static string[] GetIniContent(string iniPath)
        {
            if (!File.Exists(iniPath))
            {
                Console.WriteLine("True Light.ini does not exist\nOutput will use default settings and whitelist");
                return
                [
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
                ];
            }
            Console.WriteLine("True Light.ini exists, using existing settings and whitelist");
            return File.ReadAllLines(iniPath).TakeWhile(line => line.Trim() != "[LightBlackList]").ToArray();
        }

        private static HashSet<ModKey> GenerateBlacklist(IGameEnvironment<ISkyrimMod, ISkyrimModGetter> env)
        {
            var lights = env.LoadOrder.PriorityOrder.Light().WinningOverrides().Select(l => l.FormKey).ToHashSet();
            var blacklist = new HashSet<ModKey> { Skyrim.ModKey, Update.ModKey };
            foreach (var mod in env.LoadOrder.PriorityOrder.Select(x => x.Mod).Where(x => x != null && FilterMod(x.ModKey)))
            {
                if (HasInteriorLights(mod!, lights))
                    blacklist.Add(mod!.ModKey);
            }
            return blacklist;
        }

        private static bool HasInteriorLights(ISkyrimModGetter mod, HashSet<FormKey> lights)
        {
            var interiorCells = mod.Cells
                .SelectMany(x => x.SubBlocks)
                .SelectMany(x => x.Cells)
                .Where(cell => !FilterMod(cell.FormKey.ModKey))
                .Where(x => x.Flags.HasFlag(Cell.Flag.IsInteriorCell));

            return interiorCells.Any(cell =>
                cell.Temporary.OfType<IPlacedObjectGetter>()
                    .Where(placed => placed.FormKey.ModKey == mod.ModKey)
                    .Any(placed => lights.Contains(placed.Base.FormKey)));
        }

        private static void WriteOutput(string iniPath, string[] ini, IEnumerable<ModKey> loadOrder, HashSet<ModKey> blacklist)
        {
            Console.WriteLine("\nWriting output...");
            using var outputFile = new StreamWriter(iniPath);

            ini.ForEach(outputFile.WriteLine);
            outputFile.WriteLine("[LightBlackList]");

            foreach (var mod in loadOrder.Where(m => blacklist.Contains(m)))
                outputFile.WriteLine(mod.FileName);
        }

        public static void Main(string[] args)
        {
            var env = GameEnvironment.Typical.Skyrim(SkyrimRelease.SkyrimSE);
            var lightPlacer = Directory.CreateDirectory(Path.Combine(env.DataFolderPath.Path, "LightPlacer")).FullName;
            var iniPath = Path.Combine(lightPlacer, "True Light.ini");

            // Load existing config or use default
            var ini = GetIniContent(iniPath);

            Console.WriteLine("Generating blacklist...");
            var blacklist = GenerateBlacklist(env);

            Console.WriteLine("\nFound blacklisted mods:");
            foreach (var mod in env.LoadOrder.ListedOrder.Where(m => blacklist.Contains(m.ModKey)))
                Console.WriteLine(mod.FileName);

            WriteOutput(iniPath, ini, env.LoadOrder.ListedOrder.Select(x => x.ModKey), blacklist);

            Console.WriteLine("Output written to True Light.ini");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
