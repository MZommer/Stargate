using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Processing;
using System.Runtime.InteropServices;
using System.Text.Json;
using Unity;
using MonoBehaviour;

namespace JDNext
{
    public class MapPackage : AssetsBundle
    {
        #region Private Fields

        private string jdMapPath = "";
        private string musicTrackPath = "";
        private string classifiersPath = "";
        private string pictosPath = "";
        private string baseBundle = "";
        private string mapName = "";
        private Dictionary<string, long> msmPathIds = new();
        private long atlasId = 0;
        private SongData? songData = null;

        #endregion

        #region Constructors

        public MapPackage(string baseBundle, string jdMapPath, string musicTrackPath, string classifiersPath, string pictosPath)
        {
            ContainerName = "MapPackage";
            this.baseBundle = baseBundle;
            this.jdMapPath = jdMapPath;
            this.musicTrackPath = musicTrackPath;
            this.classifiersPath = classifiersPath;
            this.pictosPath = pictosPath;
        }

        public MapPackage(string baseBundle, string songPath)
        {
            ContainerName = "MapPackage";
            this.baseBundle = baseBundle;
            this.jdMapPath = Path.Combine(songPath, "songdata.json");
            this.musicTrackPath = Path.Combine(songPath, "musictrack.json");
            this.classifiersPath = Path.Combine(songPath, "MoveSpace");
            this.pictosPath = Path.Combine(songPath, "Pictos");
        }

        #endregion

        #region Public Methods

        public void PatchMonoBehaviour(string outputPath)
        {
            Console.WriteLine("Loading base bundle...");
            LoadBundle(baseBundle);

            foreach (AssetFileInfo file in assetsFile.GetAssetsOfType(AssetClassID.MonoBehaviour))
            {
                AssetTypeValueField objectBaseField = Manager.GetBaseField(assetsFileInstance, file);
                AssetTypeValueField scriptBaseField = Manager.GetExtAsset(assetsFileInstance, objectBaseField["m_Script"]).baseField;
                string scriptName = scriptBaseField["m_ClassName"].AsString;

                AssetsReplacerFromMemory asset = scriptName switch
                {
                    "JDMap" when File.Exists(jdMapPath) => new(assetsFile, file, PatchJDMap(objectBaseField)),
                    "MusicTrack" when File.Exists(musicTrackPath) => new(assetsFile, file, ModifyMusicTrack(objectBaseField)),
                    _ => null
                };

                if (asset != null)
                    Replacers.Add(asset);
                else
                    Console.WriteLine($"Unknown MonoBehaviour: {scriptName}");
            }

            Console.WriteLine("Writing modded bundle...");
            SaveBundle(outputPath);
        }

        public void ReplaceBundle(string outputPath)
        {
            Console.WriteLine("Loading base bundle...");
            LoadBundle(baseBundle);

            songData = JsonSerializer.Deserialize<SongData>(File.OpenRead(jdMapPath));
            mapName = songData.MapName;

            ModifyMonoBehaviours();
            MakePreloadTable();
            SetBundleName(mapName + "_MapPackage");

            Console.WriteLine("Writing modded bundle...");
            SaveBundle(outputPath);
        }

        #endregion

        #region Private Methods

        private void CleanClassifiers()
        {
            Console.WriteLine("Cleaning classifiers...");
            foreach (AssetFileInfo file in assetsFile.GetAssetsOfType(AssetClassID.TextAsset))
            {
                //RemoveAsset(file);
            }
        }

        private void InsertClassifiers()
        {
            Console.WriteLine("Inserting classifiers...");
            foreach (string msmPath in Directory.GetFiles(classifiersPath, "*.msm", SearchOption.AllDirectories))
            {
                string classifierName = Path.GetFileName(msmPath);
                msmPathIds[classifierName.ToLower()] = WriteFile(msmPath, "TextAsset");
            }
        }

        private void MakeAtlas()
        {
            Console.WriteLine("Making atlas...");

            var pictosMap = new Dictionary<string, Tuple<int, int, int>>();

            var pictos = Directory.GetFiles(pictosPath, "*.png", SearchOption.AllDirectories).ToList();
            var atlasSize = new Size(1024, 1024);
            var sample = Image.Load<Bgra32>(pictos.First());
            var pictoSize = new Size(sample.Width, sample.Height);
            var pictosPerAtlas = Math.Ceiling((double) (atlasSize.Width * atlasSize.Height) / (pictoSize.Width * pictoSize.Height));
            var atlasCount = (int)Math.Ceiling(pictos.Count() / pictosPerAtlas);

            var atlases = new List<long>();
            for (var i = 0; i < atlasCount; i++)
            {
                var atlas = new Image<Bgra32>(atlasSize.Width, atlasSize.Height);
                for (var y = 0; y < atlasSize.Height / pictoSize.Height; y++)
                {
                    for (var x = 0; x < atlasSize.Width / pictoSize.Width; x++)
                    {
                        if (pictos.Count() > 0)
                        {
                            var picto = Image.Load<Bgra32>(pictos.First());
                            pictosMap.Add(Path.GetFileNameWithoutExtension(pictos.First()), new Tuple<int, int, int>(x * picto.Width, y * picto.Height, i));
                            // Save data of the picto
                            atlas.Mutate(ctx => ctx.DrawImage(
                                picto,
                                new Point(x * picto.Width, y * picto.Height),
                                1f
                            ));
                            pictos.RemoveAt(0);
                        }
                    }
                }
                atlases.Add(WriteTextureFromMemory(atlas, $"sactx-{i}-{atlasSize.Height}x{atlasSize.Width}-Crunch-{mapName}"));
            }
            Dictionary<string, long> pictosPathIds = new();

            foreach (AssetFileInfo file in assetsFile.GetAssetsOfType(AssetClassID.SpriteAtlas))
            {
                AssetTypeValueField atlas = Manager.GetBaseField(assetsFileInstance, file);

                atlas["m_Name"].AsString = mapName;
                atlas["m_Tag"].AsString = mapName;

                var renderDataMap = atlas["m_RenderDataMap.Array"];
                renderDataMap.Children.Clear();
                foreach (var kv in pictosMap)
                {
                    Rectf rect = new()
                    {
                        x = (float)kv.Value.Item1,
                        y = (float)(pictoSize.Height - kv.Value.Item2),
                        // because of the flipping of the texture, we need to flip the rect as well
                        width = (float)pictoSize.Width,
                        height = (float)pictoSize.Height
                    };

                    pictosPathIds[kv.Key] = MakeSprite(kv.Key, atlases[kv.Value.Item3], new string[] { mapName }, atlasId, rect);
            
                    var pathId = atlases[kv.Value.Item3];
                    var name = kv.Key;
                    var data = ValueBuilder.DefaultValueFieldFromArrayTemplate(renderDataMap);
                    data["first.first.data[0]"].AsUInt = (uint)GUID_Map[name][0];
                    data["first.first.data[1]"].AsUInt = (uint)GUID_Map[name][1];
                    data["first.first.data[2]"].AsUInt = (uint)GUID_Map[name][2];
                    data["first.first.data[3]"].AsUInt = (uint)GUID_Map[name][3];
                    data["first.second"].AsLong = 21300000;
                    data["second"]["texture"]["m_PathID"].AsLong = pathId;
                    data["second.textureRect.x"].AsFloat = rect.x;
                    data["second.textureRect.y"].AsFloat =  rect.y;
                    data["second.textureRect.width"].AsFloat = rect.width;
                    data["second.textureRect.height"].AsFloat = rect.height;
                    data["second.atlasRectOffset.x"].AsFloat = -1f;
                    data["second.atlasRectOffset.x"].AsFloat = -1f;
                    data["second.uvTransform.x"].AsFloat = 100f;
                    data["second.uvTransform.y"].AsFloat = 256f;
                    data["second.uvTransform.z"].AsFloat = 100f;
                    data["second.uvTransform.w"].AsFloat = 256f;
                    data["second.downscaleMultiplier"].AsFloat = 1f;
                    data["second.settingsRaw"].AsUInt = 3;
                    renderDataMap.Children.Add(data);
                }

                var packedSprites = atlas["m_PackedSprites.Array"];
                packedSprites.Children.Clear();
                foreach (long pathId in pictosPathIds.Values)
                {
                    var data = ValueBuilder.DefaultValueFieldFromArrayTemplate(packedSprites);
                    data["m_PathID"].AsLong = pathId;
                    packedSprites.Children.Add(data);
                }

                var namesToIndex = atlas["m_PackedSpriteNamesToIndex.Array"];
                namesToIndex.Children.Clear();
                foreach (string picto in pictosPathIds.Keys){
                    var data = ValueBuilder.DefaultValueFieldFromArrayTemplate(namesToIndex);
                    data.AsString = picto;
                    namesToIndex.Children.Add(data);
                }

                Replacers.Add(new AssetsReplacerFromMemory(assetsFile, file, atlas));
            }
        }

        private AssetTypeValueField PatchJDMap(AssetTypeValueField objectBaseField)
        {
            Console.WriteLine("Patching song data");
            Console.WriteLine($"Replacing {objectBaseField["MapName"].AsString}");

            SongData songData = JsonSerializer.Deserialize<SongData>(File.OpenRead(jdMapPath))!;
            mapName = songData.MapName;

            objectBaseField["MapName"].AsString = songData.MapName;
            objectBaseField["KaraokeData"]["MapName"].AsString = songData.MapName;

            PatchSongDesc(objectBaseField["SongDesc"], songData.SongDesc);
            PatchKaraokeData(objectBaseField["KaraokeData"], songData.KaraokeData);
            PatchDanceData(objectBaseField["DanceData"], songData.DanceData);

            return objectBaseField;
        }

        private void ModifyMonoBehaviours()
        {
            foreach (AssetFileInfo file in assetsFile.GetAssetsOfType(AssetClassID.MonoBehaviour))
            {
                AssetTypeValueField objectBaseField = Manager.GetBaseField(assetsFileInstance, file);
                AssetTypeValueField scriptBaseField = Manager.GetExtAsset(assetsFileInstance, objectBaseField["m_Script"]).baseField;
                string scriptName = scriptBaseField["m_ClassName"].AsString;

                switch (scriptName)
                {
                    case "JDMap":
                        Replacers.Add(new AssetsReplacerFromMemory(assetsFile, file, ModifyJDMap(objectBaseField)));
                        break;
                    case "MusicTrack":
                        Replacers.Add(new AssetsReplacerFromMemory(assetsFile, file, ModifyMusicTrack(objectBaseField)));
                        break;
                    default:
                        Console.WriteLine($"Unknown MonoBehaviour: {scriptName}");
                        break;
                }
            }
        }

        private AssetTypeValueField ModifyJDMap(AssetTypeValueField objectBaseField)
        {
            atlasId = objectBaseField["PictogramAtlas.m_PathID"].AsLong;

            CleanClassifiers();
            InsertClassifiers();
            MakeAtlas();

            Console.WriteLine("Modding song data");
            Console.WriteLine($"Replacing {objectBaseField["MapName"].AsString} with {mapName}");

            objectBaseField["m_Name"].AsString = mapName;
            objectBaseField["MapName"].AsString = mapName;
            objectBaseField["KaraokeData"]["MapName"].AsString = mapName;
            objectBaseField["DanceData"]["MapName"].AsString = songData.MapName;

            PatchSongDesc(objectBaseField["SongDesc"], songData.SongDesc);
            PatchKaraokeData(objectBaseField["KaraokeData"], songData.KaraokeData);
            PatchDanceData(objectBaseField["DanceData"], songData.DanceData);
            PatchMoveModels(objectBaseField);
            PatchCoachDatas(objectBaseField);

            return objectBaseField;
        }


        #region JDMap Modification
        private void PatchSongDesc(AssetTypeValueField songDescField, SongDesc songDesc)
        {
            songDescField["MapName"].AsString = songDesc.MapName;
            songDescField["JDVersion"].AsInt = songDesc.JDVersion ?? 2023;
            songDescField["OriginalJDVersion"].AsInt = songDesc.OriginalJDVersion ?? 3333;
            songDescField["Artist"].AsString = songDesc.Artist ?? "Unknown Artist";
            songDescField["Title"].AsString = songDesc.Title ?? "Unknown Title";
            songDescField["Credits"].AsString = songDesc.Credits ?? "All rights of the producer and other rightholders to the recorded work reserved. Unless otherwise authorized, the duplication, rental, loan, exchange or use of this video game for public performance, broadcasting and online distribution to the public are prohibited.";
            songDescField["NumCoach"].AsInt = songDesc.NumCoach ?? 1;
            songDescField["MainCoach"].AsInt = songDesc.MainCoach ?? 0;
            songDescField["Difficulty"].AsInt = songDesc.Difficulty ?? 1;
            songDescField["SweatDifficulty"].AsInt = songDesc.SweatDifficulty ?? 1;
        }

        private void PatchKaraokeData(AssetTypeValueField karaokeDataField, KaraokeData karaokeData)
        {
            var karaokeClips = karaokeDataField["Clips.Array"];
            karaokeClips.Children.Clear();

            foreach (var container in karaokeData.Clips)
            {
                var clip = container.KaraokeClip;
                var clipArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(karaokeClips)["KaraokeClip"];
                PatchKaraokeClip(clipArrayItem, clip);
                karaokeClips.Children.Add(clipArrayItem);
            }
        }

        private void PatchKaraokeClip(AssetTypeValueField clipField, KaraokeClip clip)
        {
            clipField["Id"].AsLong = clip.Id ?? 0;
            clipField["TrackId"].AsDouble = clip.TrackId ?? 0;
            clipField["StartTime"].AsInt = clip.StartTime ?? 0;
            clipField["Duration"].AsInt = clip.Duration ?? 0;
            clipField["IsActive"].AsByte = clip.IsActive ?? 0;
            clipField["Lyrics"].AsString = clip.Lyrics ?? "";
            clipField["Pitch"].AsDouble = clip.Pitch ?? 0;
            clipField["IsEndOfLine"].AsInt = clip.IsEndOfLine ?? 0;
            clipField["ContentType"].AsInt = 1;
            clipField["SemitoneTolerance"].AsInt = clip.SemitoneTolerance ?? 0;
            clipField["StartTimeTolerance"].AsInt = clip.StartTimeTolerance ?? 0;
            clipField["EndTimeTolerance"].AsInt = clip.EndTimeTolerance ?? 0;
        }

        private void PatchDanceData(AssetTypeValueField danceDataField, DanceTapeData danceData)
        {
            PatchMotionClips(danceDataField["MotionClips.Array"], danceData.MotionClips);
            PatchPictoClips(danceDataField["PictoClips.Array"], danceData.PictoClips);
            PatchGoldEffectClips(danceDataField["GoldEffectClips.Array"], danceData.GoldEffectClips);
            PatchHideHudClips(danceDataField["HideHudClips.Array"], danceData.HideHudClips);
        }

        private void PatchMotionClips(AssetTypeValueField motionClipsField, MotionClipData[] motionClips)
        {
            motionClipsField.Children.Clear();

            foreach (var clip in motionClips.Where(c => c.MoveType != 1)) // Skip FullBody clips
            {
                var clipArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(motionClipsField);
                PatchMotionClip(clipArrayItem, clip);
                motionClipsField.Children.Add(clipArrayItem);
            }
        }

        private void PatchMotionClip(AssetTypeValueField clipField, MotionClipData clip)
        {
            clipField["Id"].AsLong = clip.Id ?? 0;
            clipField["TrackId"].AsDouble = clip.TrackId ?? 0;
            clipField["StartTime"].AsInt = clip.StartTime ?? 0;
            clipField["Duration"].AsInt = clip.Duration ?? 0;
            clipField["IsActive"].AsByte = clip.IsActive ?? 0;
            clipField["MoveName"].AsString = clip.MoveName ?? "";
            clipField["GoldMove"].AsByte = clip.GoldMove ?? 0;
            clipField["CoachId"].AsInt = clip.CoachId ?? 0;
            clipField["MoveType"].AsInt = clip.MoveType ?? 0;
            clipField["Color"].AsString = clip.Color ?? "";
        }

        private void PatchPictoClips(AssetTypeValueField pictoClipsField, PictogramClipData[] pictoClips)
        {
            pictoClipsField.Children.Clear();

            foreach (var clip in pictoClips)
            {
                var clipArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(pictoClipsField);
                PatchPictoClip(clipArrayItem, clip);
                pictoClipsField.Children.Add(clipArrayItem);
            }
        }

        private void PatchPictoClip(AssetTypeValueField clipField, PictogramClipData clip)
        {
            clipField["Id"].AsLong = clip.Id ?? 0;
            clipField["TrackId"].AsDouble = clip.TrackId ?? 0;
            clipField["StartTime"].AsInt = clip.StartTime ?? 0;
            clipField["Duration"].AsInt = clip.Duration ?? 0;
            clipField["IsActive"].AsByte = clip.IsActive ?? 0;
            clipField["PictoPath"].AsString = clip.PictoPath ?? "";
            clipField["CoachCount"].AsUInt = clip.CoachCount ?? 4294967295;
        }

        private void PatchGoldEffectClips(AssetTypeValueField goldEffectClipsField, GoldEffectClipData[] goldEffectClips)
        {
            goldEffectClipsField.Children.Clear();

            foreach (var clip in goldEffectClips)
            {
                var clipArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(goldEffectClipsField);
                PatchGoldEffectClip(clipArrayItem, clip);
                goldEffectClipsField.Children.Add(clipArrayItem);
            }
        }

        private void PatchGoldEffectClip(AssetTypeValueField clipField, GoldEffectClipData clip)
        {
            clipField["Id"].AsLong = clip.Id ?? 0;
            clipField["TrackId"].AsDouble = clip.TrackId ?? 0;
            clipField["StartTime"].AsInt = clip.StartTime ?? 0;
            clipField["Duration"].AsInt = clip.Duration ?? 0;
            clipField["IsActive"].AsByte = clip.IsActive ?? 0;
            clipField["GoldEffectType"].AsInt = clip.GoldEffectType ?? 1;
        }

        private void PatchHideHudClips(AssetTypeValueField hideHudClipsField, HideHudClipData[] hideHudClips)
        {
            hideHudClipsField.Children.Clear();

            if (hideHudClips != null)
            {
                foreach (var clip in hideHudClips)
                {
                    var clipArrayItem = ValueBuilder.DefaultValueFieldFromArrayTemplate(hideHudClipsField);
                    PatchHideHudClip(clipArrayItem, clip);
                    hideHudClipsField.Children.Add(clipArrayItem);
                }
            }
        }

        private void PatchHideHudClip(AssetTypeValueField clipField, HideHudClipData clip)
        {
            clipField["StartTime"].AsInt = clip.StartTime ?? 0;
            clipField["Duration"].AsInt = clip.Duration ?? 0;
            clipField["IsActive"].AsByte = clip.IsActive ?? 0;
        }

        private void PatchMoveModels(AssetTypeValueField objectBaseField)
        {
            PatchCameraMoveModels(objectBaseField);
            PatchHandDeviceMoveModels(objectBaseField["HandDeviceMoveModels"]["list.Array"]);
        }

        private void PatchCameraMoveModels(AssetTypeValueField objectBaseField)
        {
            try
            {
                var cameraModels = objectBaseField["CameraMoveModels"]["list.Array"];
                cameraModels.Children.Clear();
            }
            catch
            {
                // Handle exception if the field doesn't exist
            }

            try
            {
                var cameraModels = objectBaseField["CameraBlazePoseMoveModels"]["list.Array"];
                cameraModels.Children.Clear();
            }
            catch
            {
                // Handle exception if the field doesn't exist
            }
        }

        private void PatchHandDeviceMoveModels(AssetTypeValueField handModelsField)
        {
            handModelsField.Children.Clear();

            foreach (var kv in msmPathIds)
            {
                var modelTpl = ValueBuilder.DefaultValueFieldFromArrayTemplate(handModelsField);
                modelTpl["Key"].AsString = Path.GetFileNameWithoutExtension(kv.Key);
                modelTpl["Value"]["m_FileID"].AsInt = 0;
                modelTpl["Value"]["m_PathID"].AsLong = kv.Value;
                handModelsField.Children.Add(modelTpl);
            }
        }

        private void PatchCoachDatas(AssetTypeValueField objectBaseField)
        {
            PatchFullBodyCoachDatas(objectBaseField["FullBodyCoachDatas.Array"], songData.FullBodyCoachDatas);
            PatchHandOnlyCoachDatas(objectBaseField["HandOnlyCoachDatas.Array"], songData.HandOnlyCoachDatas);
        }

        private void PatchFullBodyCoachDatas(AssetTypeValueField fullBodyField, CoachData[] fullBodyCoachDatas)
        {
            fullBodyField.Children.Clear();

            foreach (var data in fullBodyCoachDatas)
            {
                var coachDataTpl = ValueBuilder.DefaultValueFieldFromArrayTemplate(fullBodyField);
                PatchCoachData(coachDataTpl, data);
                fullBodyField.Children.Add(coachDataTpl);
            }
        }

        private void PatchHandOnlyCoachDatas(AssetTypeValueField handOnlyField, CoachData[] handOnlyCoachDatas)
        {
            handOnlyField.Children.Clear();

            foreach (var data in handOnlyCoachDatas)
            {
                var coachDataTpl = ValueBuilder.DefaultValueFieldFromArrayTemplate(handOnlyField);
                PatchCoachData(coachDataTpl, data);
                handOnlyField.Children.Add(coachDataTpl);
            }
        }

        private void PatchCoachData(AssetTypeValueField coachDataField, CoachData data)
        {
            coachDataField["GoldMovesCount"].AsUInt = data.GoldMovesCount ?? 0;
            coachDataField["StandardMovesCount"].AsUInt = data.StandardMovesCount ?? 0;
        }

        #endregion

        #region MusicTrack Modification

        private AssetTypeValueField ModifyMusicTrack(AssetTypeValueField objectBaseField)
        {
            MusicTrackStructure musicTrack = JsonSerializer.Deserialize<MusicTrackStructure>(File.OpenRead(musicTrackPath))!;

            PatchMusicTrackStructure(objectBaseField["m_structure"]["MusicTrackStructure"], musicTrack);

            return objectBaseField;
        }

        private void PatchMusicTrackStructure(AssetTypeValueField structureField, MusicTrackStructure musicTrack)
        {
            structureField["startBeat"].AsInt = musicTrack.StartBeat ?? 0;
            structureField["endBeat"].AsInt = musicTrack.EndBeat ?? 0;
            structureField["videoStartTime"].AsDouble = musicTrack.VideoStartTime ?? 0;
            structureField["previewEntry"].AsDouble = musicTrack.PreviewEntry ?? 0;
            structureField["previewLoopStart"].AsDouble = musicTrack.PreviewLoopStart ?? 0;
            structureField["previewLoopEnd"].AsDouble = musicTrack.PreviewLoopEnd ?? 30;
            structureField["previewDuration"].AsDouble = musicTrack.PreviewDuration ?? 30;

            PatchSignatures(structureField["signatures.Array"], musicTrack.Signatures);
            PatchMarkers(structureField["markers.Array"], musicTrack.Markers);
            PatchSections(structureField["sections.Array"], musicTrack.Sections);
            PatchComments(structureField["comments.Array"], musicTrack.Comments);
        }

        private void PatchSignatures(AssetTypeValueField signaturesField, SignatureContainer[] signatures)
        {
            signaturesField.Children.Clear();

            foreach (var container in signatures)
            {
                var signature = container.MusicSignature;
                var signatureTpl = ValueBuilder.DefaultValueFieldFromArrayTemplate(signaturesField);
                PatchSignature(signatureTpl["MusicSignature"], signature);
                signaturesField.Children.Add(signatureTpl);
            }
        }

        private void PatchSignature(AssetTypeValueField signatureField, MusicSignature signature)
        {
            signatureField["beats"].AsInt = signature.beats ?? 0;
            signatureField["marker"].AsDouble = signature.marker ?? 0;
            signatureField["comment"].AsString = signature.comment ?? "";
        }

        private void PatchMarkers(AssetTypeValueField markersField, TrackMarker[] markers)
        {
            markersField.Children.Clear();

            foreach (var container in markers)
            {
                var markerTpl = ValueBuilder.DefaultValueFieldFromArrayTemplate(markersField);
                markerTpl["VAL"].AsLong = container.VAL ?? 0;
                markersField.Children.Add(markerTpl);
            }
        }

        private void PatchSections(AssetTypeValueField sectionsField, SectionContainer[] sections)
        {
            sectionsField.Children.Clear();

            foreach (var container in sections)
            {
                var section = container.MusicSection;
                var sectionTpl = ValueBuilder.DefaultValueFieldFromArrayTemplate(sectionsField);
                PatchSection(sectionTpl["MusicSection"], section);
                sectionsField.Children.Add(sectionTpl);
            }
        }

        private void PatchSection(AssetTypeValueField sectionField, MusicSection section)
        {
            sectionField["sectionType"].AsInt = section.sectionType ?? 0;
            sectionField["marker"].AsLong = section.marker ?? 0;
            sectionField["comment"].AsString = section.comment ?? "";
        }

        private void PatchComments(AssetTypeValueField commentsField, CommentContainer[] comments)
        {
            commentsField.Children.Clear();

            foreach (var container in comments)
            {
                var comment = container.Comment;
                var commentContainerTpl = ValueBuilder.DefaultValueFieldFromArrayTemplate(commentsField);
                var commentTpl = commentContainerTpl["Comment"];
                PatchComment(commentTpl, comment);
                commentsField.Children.Add(commentContainerTpl);
            }
        }

        private void PatchComment(AssetTypeValueField commentField, TrackComment comment)
        {
            commentField["marker"].AsDouble = comment.marker ?? 0;
            commentField["commentType"].AsString = comment.commentType ?? "";
            commentField["comment"].AsString = comment.comment ?? "";
        }

        #endregion
    
        #endregion
    }
}
