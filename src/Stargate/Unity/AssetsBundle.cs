using AssetsTools.NET;
using AssetsTools.NET.Extra;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Utils;

namespace Unity
{
    /// <summary>
    /// Class for managing Unity AssetBundles with AssetsTools.NET.
    /// </summary>
    public class AssetsBundle
    {
        internal string ContainerName = "";
        internal string BundleName = "";
        internal List<AssetsReplacer> Replacers = new();
        internal AssetsManager Manager = new AssetsManager();
        internal BundleFileInstance? BundleInstance;
        internal AssetBundleFile? BundleFile;
        internal AssetsFileInstance? assetsFileInstance;
        internal AssetsFile? assetsFile;
        internal List<AssetFileInfo>? AssetBundleInfos;
        internal AssetTypeValueField? AssetBundleValue;
        internal Dictionary<string, int[]> GUID_Map = new();
        internal long LatestPathId = 0xffffffff;
        internal List<long> PathIds = new();
        private readonly Random rnd = new();

        /// <summary>
        /// Loads an AssetBundle from the specified file path.
        /// </summary>
        /// <param name="baseBundle">The file path of the base bundle to load.</param>
        /// <exception cref="Exception">Thrown if there is an error loading the bundle.</exception>
        internal void LoadBundle(string baseBundle)
        {
            try
            {
                BundleInstance = Manager.LoadBundleFile(baseBundle, true);
                BundleFile = BundleInstance.file;

                assetsFileInstance = Manager.LoadAssetsFileFromBundle(BundleInstance, 0, false);
                assetsFile = assetsFileInstance.file;

                AssetBundleInfos = assetsFile.GetAssetsOfType((int)AssetClassID.AssetBundle);
                AssetBundleValue = Manager.GetBaseField(assetsFileInstance, AssetBundleInfos[0]);

                LatestPathId = assetsFile.Metadata.AssetInfos.Max(i => i.PathId);
                // TODO: ADD AUTO CONTAINER NAME!
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading bundle: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Writes a file to the AssetBundle.
        /// </summary>
        /// <param name="filePath">The file path of the asset to add.</param>
        /// <param name="AssetType">The type of asset (e.g., "TextAsset", "Texture2D").</param>
        /// <returns>The PathId of the written asset.</returns>
        /// <exception cref="Exception">Thrown if the asset type is not implemented.</exception>
        internal long WriteFile(string filePath, string AssetType)
        {
            try
            {
                long PathId = LatestPathId++;
                AssetTypeTemplateField templateField = new();
                TypeTreeType ttType = AssetHelper.FindTypeTreeTypeByName(assetsFileInstance.file.Metadata, AssetType);
                templateField.FromTypeTree(ttType);
                AssetTypeValueField baseField = ValueBuilder.DefaultValueFieldFromTemplate(templateField);
                string fileName = "";

                switch (AssetType)
                {
                    case "TextAsset":
                        fileName = Path.GetFileName(filePath);
                        baseField.Get("m_Script").AsByteArray = File.ReadAllBytes(filePath);
                        break;
                    case "Texture2D":
                        fileName = Path.GetFileNameWithoutExtension(filePath);
                        PrepareTexture(filePath, ref baseField);
                        break;
                    default:
                        throw new Exception($"{AssetType} Not implemented!");
                }

                baseField.Get("m_Name").AsString = fileName;
                Replacers.Add(new AssetsReplacerFromMemory(0, PathId, ttType.TypeId, 0xffff, baseField.WriteToByteArray()));
                PathIds.Add(PathId);
                return PathId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing file: {ex.Message}");
                throw;
            }
        }

        internal long WriteTextureFromMemory(Image<Bgra32> image, string fileName){
             try
            {
                long PathId = LatestPathId++;
                AssetTypeTemplateField templateField = new();
                TypeTreeType ttType = AssetHelper.FindTypeTreeTypeByName(assetsFileInstance.file.Metadata, "Texture2D");
                templateField.FromTypeTree(ttType);
                AssetTypeValueField baseField = ValueBuilder.DefaultValueFieldFromTemplate(templateField);
                PrepareTexture(image, ref baseField, fileName);
                baseField.Get("m_Name").AsString = fileName;
                Replacers.Add(new AssetsReplacerFromMemory(0, PathId, ttType.TypeId, 0xffff, baseField.WriteToByteArray()));
                PathIds.Add(PathId);
                return PathId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Removes an asset from the AssetBundle.
        /// </summary>
        /// <param name="assetInfo">The asset information of the asset to remove.</param>
        public void RemoveAsset(AssetFileInfo assetInfo)
        {
            Replacers.Add(new AssetsRemover(0, assetInfo.PathId, assetInfo.ClassId, 0xffff));
        }

        /// <summary>
        /// Creates a Sprite asset in the AssetBundle.
        /// </summary>
        /// <param name="filePath">The file path of the sprite to add.</param>
        /// <param name="TextureID">The PathId of the texture.</param>
        /// <param name="AtlasTags">The atlas tags.</param>
        /// <param name="AtlasID">The PathId of the atlas.</param>
        /// <returns>The PathId of the created sprite.</returns>
        internal long MakeSprite(string filePath, long TextureID, string[] AtlasTags, long AtlasID, Rectf? rect = null)
        {
            try
            {
                long PathId = LatestPathId++;
                AssetTypeValueField baseField = Manager.GetBaseField(assetsFileInstance, assetsFile.GetAssetsOfType(AssetClassID.Sprite)[0]);
                AssetTypeTemplateField templateField = new();
                TypeTreeType ttType = AssetHelper.FindTypeTreeTypeByName(assetsFileInstance.file.Metadata, "Sprite");
                templateField.FromTypeTree(ttType);

                string fileName = Path.GetFileNameWithoutExtension(filePath);
                baseField["m_Name"].AsString = fileName;

                GUID_Map[fileName] = new int[4] { rnd.Next(0, 0xffffff), rnd.Next(0, 0xffffff), rnd.Next(0, 0xffffff), rnd.Next(0, 0xffffff) };
                baseField["m_RenderDataKey.first.data[0]"].AsUInt = (uint)GUID_Map[fileName][0];
                baseField["m_RenderDataKey.first.data[1]"].AsUInt = (uint)GUID_Map[fileName][1];
                baseField["m_RenderDataKey.first.data[2]"].AsUInt = (uint)GUID_Map[fileName][2];
                baseField["m_RenderDataKey.first.data[3]"].AsUInt = (uint)GUID_Map[fileName][3];
                baseField["m_RenderDataKey.second"].AsLong = 21300000;

                var tags = baseField["m_AtlasTags.Array"];
                tags.Children.Clear();
                foreach (string tag in AtlasTags)
                {
                    var data = ValueBuilder.DefaultValueFieldFromArrayTemplate(tags);
                    data.AsString = tag;
                    tags.Children.Add(data);
                }

                baseField["m_SpriteAtlas.m_PathID"].AsLong = AtlasID;
                baseField["m_RD.texture.m_PathID"].AsLong = TextureID;
                if (rect != null) {
                    baseField["m_Rect.x"].AsFloat = rect.x;
                    baseField["m_Rect.y"].AsFloat = rect.y;
                    baseField["m_Rect.width"].AsFloat = rect.width;
                    baseField["m_Rect.height"].AsFloat = rect.height;
                
                    baseField["m_RD.textureRect.x"].AsFloat = rect.x;
                    baseField["m_RD.textureRect.y"].AsFloat = rect.y;
                    baseField["m_RD.textureRect.width"].AsFloat = rect.width;
                    baseField["m_RD.textureRect.height"].AsFloat = rect.height;
                }
                
                Replacers.Add(new AssetsReplacerFromMemory(0, PathId, ttType.TypeId, 0xffff, baseField.WriteToByteArray()));
                PathIds.Add(PathId);
                return PathId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error making sprite: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Creates a preload table for the AssetBundle.
        /// </summary>
        internal void MakePreloadTable()
        {
            try
            {
                List<AssetFileInfo> table = assetsFile.Metadata.AssetInfos;
                AssetTypeValueField preloadTable = AssetBundleValue["m_PreloadTable.Array"];
                AssetTypeValueField containers = AssetBundleValue["m_Container.Array"];

                containers.Children.Clear();
                preloadTable.Children.Clear();

                foreach (var info in table)
                {
                    var preload = ValueBuilder.DefaultValueFieldFromArrayTemplate(preloadTable);
                    preload["m_FileID"].AsInt = 0;
                    preload["m_PathID"].AsLong = info.PathId;
                    preloadTable.Children.Add(preload);
                }

                foreach (long pathId in PathIds)
                {
                    AssetTypeValueField preload = ValueBuilder.DefaultValueFieldFromArrayTemplate(preloadTable);
                    preload["m_FileID"].AsInt = 0;
                    preload["m_PathID"].AsLong = pathId;
                    preloadTable.Children.Add(preload);
                }

                foreach (var info in preloadTable.Children)
                {
                    AssetTypeValueField container = ValueBuilder.DefaultValueFieldFromArrayTemplate(containers);
                    container["first"].AsString = ContainerName;
                    container["second"]["preloadIndex"].AsInt = 0;
                    container["second"]["preloadSize"].AsInt = preloadTable.Children.Count;
                    container["second"]["asset"]["m_FileID"].AsInt = 0;
                    container["second"]["asset"]["m_PathID"].AsLong = info["m_PathID"].AsLong;
                    containers.Children.Add(container);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error making preload table: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Sets the name of the AssetBundle.
        /// </summary>
        /// <param name="BundleName">The name to set for the AssetBundle.</param>
        internal void SetBundleName(string BundleName)
        {
            this.BundleName = BundleName;
            AssetBundleValue.Get("m_Name").AsString = BundleName;
            AssetBundleValue.Get("m_AssetBundleName").AsString = BundleName;
        }

        /// <summary>
        /// Saves the AssetBundle to the specified output folder.
        /// </summary>
        /// <param name="OutputFolder">The folder to save the AssetBundle in.</param>
        /// <exception cref="Exception">Thrown if there is an error saving the bundle.</exception>
        internal void SaveBundle(string OutputFolder)
        {
            try
            {
                Replacers.Add(new AssetsReplacerFromMemory(assetsFile, AssetBundleInfos[0], AssetBundleValue));

                using var assetsStream = new MemoryStream();
                using var assetsFileWriter = new AssetsFileWriter(assetsStream);
                assetsFile.Write(assetsFileWriter, 0, Replacers);

                string assetName = BundleHelper.GetDirInfo(BundleInstance.file, 0).Name;
                if (string.IsNullOrEmpty(BundleName))
                    BundleName = assetName;

                List<BundleReplacer> bundleReplacers = new() { new BundleReplacerFromMemory(assetName, null, true, assetsStream.ToArray(), -1) };

                string moddedBundle = Path.Combine(OutputFolder, $"{BundleName}.bundle");
                string compressedBundle = Path.Combine(OutputFolder, $"{BundleName}_compressed.bundle");

                using (var bundleWriter = new AssetsFileWriter(moddedBundle))
                {
                    BundleInstance.file.Write(bundleWriter, bundleReplacers);
                }

                Console.WriteLine("Compressing bundle...");
                var am = new AssetsManager();
                var bundle = am.LoadBundleFile(moddedBundle);
                using (var stream = File.OpenWrite(compressedBundle))
                using (var writer = new AssetsFileWriter(stream))
                {
                    bundle.file.Pack(bundle.file.Reader, writer, AssetBundleCompressionType.LZMA);
                }
                Console.WriteLine("Successfully packed bundle in LZ4 format.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving bundle: {ex.Message}");
                throw;
            }
        }
    }
}
