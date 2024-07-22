import json

def loadCKD(path):
	with open(path, "rb") as f:
		content = f.read().removesuffix(b"\x00").decode("utf-8")
	return json.loads(content)

def generateSongDesc(songdesc):
    return {
		"MapName": songdesc["MapName"],
		"JDVersion": songdesc["JDVersion"],
		"OriginalJDVersion": songdesc["OriginalJDVersion"],
		"Artist": songdesc["Artist"],
		"DancerName": songdesc["DancerName"],
		"Title": songdesc["Title"],
		"Credits": songdesc["Credits"],
		"NumCoach": songdesc["NumCoach"],
		"MainCoach": songdesc["MainCoach"],
		"Difficulty": songdesc["Difficulty"],
		"SweatDifficulty": songdesc["SweatDifficulty"],
	}

def ClassToLua(obj):
	__class = obj["__class"]
	del obj["__class"]
	return {__class: obj}

def processKtape(ktape):
	try:
		del ktape["__class"]
		del ktape["SoundwichEvent"]
	except: pass
 
	return {
		**ktape,
		"Tracks": [
			{
				"TapeTrack": {
					"Id": 0,
					"Name": ""
				}
			}
		],
		"Clips": [ClassToLua(clip) for clip in ktape["Clips"]]
	} 

def processDtape(dtape, mainsequence={}):
	dancedata = {
		"TapeClock": dtape["TapeClock"],
		"TapeBarCount": dtape["TapeBarCount"],
		"SoundwichEvent": "",
		"MapName": dtape["MapName"],
		"FreeResourcesAfterPlay": dtape["FreeResourcesAfterPlay"],
		"MotionClips": [],
		"PictoClips": [],
		"GoldEffectClips": [],
		"HideHudClips": [],
	}
	if mainsequence.get("Clips"):
		for clip in mainsequence["Clips"]:
			if clip["__class"] == "HideUserInterfaceClip":
				dancedata["HideHudClips"].append({
					"IsActive": clip["IsActive"],
					"StartTime": clip["StartTime"],
					"Duration": clip["Duration"],
				})
	for clip in dtape["Clips"]:
		if clip["__class"] == "MotionClip":
			dancedata["MotionClips"].append({
				"Id": clip["Id"],
				"TrackId": clip["TrackId"],
				"IsActive": clip["IsActive"],
				"StartTime": clip["StartTime"],
				"Duration": clip["Duration"],
				"GoldMove": clip["GoldMove"],
				"CoachId": clip["CoachId"],
				"MoveType": clip["MoveType"],
				"Color": "",
				"MoveName": clip["ClassifierPath"].split("/")[-1].removesuffix(".msm").removesuffix(".gesture"),
			})
		if clip["__class"] == "PictogramClip":
			dancedata["PictoClips"].append( {
				"Id": clip["Id"],
				"TrackId": clip["TrackId"],
				"IsActive": clip["IsActive"],
				"StartTime": clip["StartTime"],
				"Duration": clip["Duration"],
				"PictoPath": clip["PictoPath"].split("/")[-1].removesuffix(".png"),
				"CoachCount": clip["CoachCount"],
			})
		if clip["__class"] == "GoldEffectClip":
			dancedata["GoldEffectClips"].append({
				"Id": clip["Id"],
				"TrackId": clip["TrackId"],
				"IsActive": clip["IsActive"],
				"StartTime": clip["StartTime"],
				"Duration": clip["Duration"],
				"GoldEffectType": 1
			})
	return dancedata

def getMoveModels(dtape):
	CoachCount = max(clip.get("CoachId", 0) for clip in dtape["Clips"]) + 1
	models = tuple([{"GoldMovesCount": 0, "StandardMovesCount": 0} for _ in range(CoachCount)] for _ in range(2))
	for clip in dtape["Clips"]:
		if clip["__class"] == "MotionClip":
			models[clip["MoveType"]][clip["CoachId"]]["StandardMovesCount"] += 1
			if clip["GoldMove"]:
				models[clip["MoveType"]][clip["CoachId"]]["GoldMovesCount"] += 1
	return {
		"FullBodyCoachDatas": models[1],
		"HandOnlyCoachDatas": models[0],
	}
songdesc = loadCKD("songdesc.tpl.ckd")["COMPONENTS"][0]
dtape = loadCKD("dtape.ckd")
mainsequence = loadCKD("mainsequence.tape.ckd")
SongData = {
	"MapName": songdesc["MapName"],
	"SongDesc": generateSongDesc(songdesc),
	"KaraokeData": processKtape(loadCKD("ktape.ckd")),
	"DanceData": processDtape(dtape, mainsequence),
	"PictogramAtlas": {
		"m_FileId": 0,
		"m_PathId": 0
	},
	"CameraMoveModels": {
		"list": [],
		"keyCollision": 0
	},
	"HandDeviceMoveModels": {
		"list": [],
		"keyCollision": 0
	}, # Placeholder, will be created when inserted assets
	**getMoveModels(dtape),

}

with open('songdata.json', 'w') as f:
	json.dump(SongData, f, indent=4)

structure = loadCKD("musictrack.tpl.ckd")["COMPONENTS"][0]['trackData']['structure']
MusicTrack = {
	**structure,
	'useFadeStartBeat': int(structure.get('useFadeStartBeat', False)),
	'useFadeEndBeat': int(structure.get('useFadeEndBeat', False)),
	'markers': [{'VAL': marker} for marker in structure.get('markers', [])],
	'signatures': [ClassToLua(signature) for signature in structure.get('signatures', [])],
	'sections': [ClassToLua(section) for section in structure.get('sections', [])],
	'comments': [ClassToLua(comment) for comment in structure.get('comments', [])],
}
del MusicTrack['__class']

with open('musictrack.json', 'w') as f:
	json.dump(MusicTrack, f, indent=4)
