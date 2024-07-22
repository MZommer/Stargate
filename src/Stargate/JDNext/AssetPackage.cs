using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Reflection.Metadata;
using Unity;

namespace JDNext
{
    public static class AssetTypes
    {
        static string coachesLarge = "coachesLarge";
        static string coachesSmall = "coachesSmall";
        static string cover = "cover";
        static string cover1024 = "cover1024";
        static string coverSmall = "coverSmall";
        static string songTitleLogo = "songTitleLogo";
        static string[] All = { coachesLarge, coachesSmall, cover, cover1024, coverSmall, songTitleLogo };
    }

    public class AssetPackage : AssetsBundle
    {
        public AssetPackage()
        {
        }


        public void repack(string input, string ouput)
        {

            // Load up base bundle to edit
            Console.WriteLine("Loading base bundle...");
            LoadBundle(input);
            // Saving bundle
            Console.WriteLine("Saving bundle...");
            SaveBundle(ouput);
        }
    }
}
