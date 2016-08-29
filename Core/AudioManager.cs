// ENGINE SCRIPT: AVOID PUTTING GAME SPECIFIC CODE IN HERE
// This script/component is attached to the GameAudio game object
// To play global audio sounds/music such as the game music
// All audio should be started from here as this also regulates the mutes

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager
{
	public static GameObject audioGameObject;

	// Following two lists contain audio sources which can be null when they're destroyed in which case they should be removed from the list
	private List<AudioSource> effectsAudioSources = null;
	private List<AudioSource> musicAudioSources = null;
	private const int LIST_CLEAN_TIME = 60;  // every 60 Update calls, clean these lists as they are filled with null values over time, because the AudioSources in it are destroyed
	private int pCleanLists = LIST_CLEAN_TIME;

	// constructor
	public AudioManager()
	{
		// Create Game Audio. Maybe this could be moved as well.
		
		audioGameObject = new GameObject("GameAudio");
		audioGameObject.AddComponent<AudioListener>();
		UnityEngine.Object.DontDestroyOnLoad(audioGameObject);

		effectsAudioSources = new List<AudioSource>();
		musicAudioSources = new List<AudioSource>();
		//Debug.Log("gameObject.name: " + gameObject.name);
	}

	public void Update()
	{
		if (Input.GetKeyUp(KeyCode.M))
		{
			Data.muteAllSound = !Data.muteAllSound;

//			if (Data.muteAllSound)
//			{  // make sure all is muted
//				if (Data.sfx) MuteSFX(true);
//				if (Data.music) MuteMusic(true);
//			} else
//			{  // unmute unless sfx/music is muted
//				if (Data.sfx) MuteSFX(false);
//				if (Data.music) MuteMusic(false);
//			}

			// temporarily commented the above since it feels weird.

			if (Data.muteAllSound)
			{ 
				Data.sfx = false;
				MuteSFX(true);
				Data.music = false;
				MuteMusic(true);
			} else
			{ 
				Data.sfx = true;
				MuteSFX(false);
				Data.music = true;
				MuteMusic(false);
			}

			if (Scripts.interfaceScript.settingsPanel.activeSelf) Scripts.interfaceScript.settingsPanel.GetComponent<SettingsPanel>().ResetPanel();

			UserData.Save();
		}

		--pCleanLists;
		if (pCleanLists <= 0)
		{
			pCleanLists = LIST_CLEAN_TIME;
			CleanAudioSourcesList();
		}
	}

	//------------------------------------------------------------------------------------------------------
	// Play functions (PlaySFX, PlaySFX3D, PlayMusic
	//------------------------------------------------------------------------------------------------------
	// function overloads (kinda getting out of hand). .net 3.5 doesn't support default parameters and I can set it to 4.0 but Unity forces it back
	// without a bundle name parameter the sfx is loaded from the project's resources
	public AudioSource PlaySFX(string aSFXName) {return PlaySFX("", aSFXName, 1.0f, 0); }
	public AudioSource PlaySFX(string aSFXName, float aVolume) { return PlaySFX("", aSFXName, aVolume, 0); }
	public AudioSource PlaySFX(string aSFXName, float aVolume, int aLoop) { return PlaySFX("", aSFXName, aVolume, aLoop); }
	public AudioSource PlaySFX(string aBundle, string aSFXName) { return PlaySFX(aBundle, aSFXName, 1.0f, 0); }
	public AudioSource PlaySFX(string aBundle, string aSFXName, float aVolume) { return PlaySFX(aBundle, aSFXName, aVolume, 0); }
	public AudioSource PlaySFX(string aBundle, string aSFXName, float aVolume, int aLoop)
	{
		if (GameData.soundDebug) Debug.Log("MELVIN! I am playing a 2d SFX. It's name is: " + aSFXName);

		AudioClip tClip;
		if (aBundle.Length == 0)  // no bundle given, audio is loaded from the main file (from resources)
		{
			tClip = Resources.Load("Audio/SFX/" + aSFXName + "_SFX") as AudioClip;
			if (tClip == null)
			{
				Debug.LogError("SFX sound not found: Audio/SFX/" + aSFXName + "_SFX");
				return null;
			}
		} else
		{
			tClip = Loader.LoadAudio(aSFXName + "_SFX");
			if (tClip == null)
			{
				Debug.LogError("SFX sound not found in bundle " + aBundle + ": Audio/" + aSFXName + "_SFX");
				return null;
			}
		}

		AudioSource tSource = audioGameObject.AddComponent<AudioSource>();
		tSource.priority = 10;  // SFX gets lower priority than music
		tSource.clip = tClip;
		tSource.volume = aVolume * Data.sfxVolume;
		tSource.minDistance = 50.0f;
		tSource.dopplerLevel = 0.0f;  // no doppler
		tSource.mute = !Data.sfx || Data.muteAllSound;

		if (aLoop != 0)
			tSource.loop = true;
		if (aLoop >= 0)  // if aLoop == -1 then we loop indefinitely, we simply don't destroy it
			UnityEngine.Object.Destroy(tSource, tClip.length * (aLoop+1));  // destroy this AudioSource after it's done after its additional loops

		tSource.Play();

		effectsAudioSources.Add(tSource);

		return tSource;
	}

	// 3d sound
	public AudioSource PlaySFX3D (string aSFXName, GameObject anObjectToBindTo) {return PlaySFX3D ("", aSFXName, anObjectToBindTo, aSFXName);}
	public AudioSource PlaySFX3D (string aSFXName, GameObject anObjectToBindTo, string aSet) {return PlaySFX3D ("", aSFXName, anObjectToBindTo, aSet);}
	public AudioSource PlaySFX3D (string aBundle, string aSFXName, GameObject anObjectToBindTo, string aSet)
	{
		if (GameData.soundDebug) Debug.Log("MELVIN! I am playing a 3d SFX. It's name is: " + aSFXName);

		// GameObject & error checking
		GameObject tBindTo = anObjectToBindTo;
		if (tBindTo == null)
		{
			if (GameData.soundDebug) Debug.LogError("GameObject was 'null'. This function needs an object.");
			return null;
		}

		// Set and set error checking!
		Dictionary<string, DicEntry> soundSet;
		if (!Data.Shared["SoundSet"].d.ContainsKey(aSet))
		{
			if (GameData.soundDebug) Debug.LogWarning("There is not SoundSet for this sound : " + aSFXName + " Reverting to default so stuff gets played!");
			soundSet = Data.Shared["SoundSet"].d["Default"].d;
			//return null;
		} else soundSet = Data.Shared["SoundSet"].d[aSet].d;


		// Audio clip & error checking
		AudioClip tClip;
		if (aBundle.Length == 0)  // no bundle given, audio is loaded from the main file (from resources)
		{
			tClip = Resources.Load("Audio/SFX/" + aSFXName + "_SFX") as AudioClip;
			if (tClip == null)
			{
				if (GameData.soundDebug) Debug.LogError("SFX sound not found: Audio/SFX/" + aSFXName + "_SFX");
				return null;
			}
		} else
		{
			tClip = Loader.LoadAudio(aSFXName + "_SFX");
			if (tClip == null)
			{
				Debug.LogError("SFX sound not found in bundle " + aBundle + ": Audio/" + aSFXName + "_SFX");
				return null;
			}
		}

		// Do it
		AudioSource tSource = tBindTo.AddComponent<AudioSource>();
		tSource.priority = 10;  // SFX gets lower priority than music, we always set if for now
		tSource.clip = tClip; // Clip; must be sent
		// loop 
		int loopAmount = 0;
		// Default settings:
		tSource.volume       = 1.0f * Data.sfxVolume;
		tSource.loop         = false;
		tSource.pitch        = 1.0f;
		tSource.dopplerLevel = 1.0f;
		tSource.rolloffMode  = AudioRolloffMode.Linear;
		tSource.minDistance  = 5.0f;
		tSource.maxDistance  = 50.0f;
		tSource.spatialBlend = 1.0f;
		tSource.spread       = 0.0f;

		// Override settings from set
		foreach(string key in soundSet.Keys)
		{
			switch(key)
			{
			case "Volume":
				tSource.volume = soundSet["Volume"].f * Data.sfxVolume;
				break;

			case "Loop":
				loopAmount = soundSet["Loop"].i;
				break;

			case "Pitch":
				tSource.pitch = soundSet["Pitch"].f;
				break;

			case "DopplerLevel":
				tSource.dopplerLevel = soundSet["DopplerLevel"].f;
				break;

			case "RolloffMode":
				if (soundSet["RolloffMode"].s == "Linear") tSource.rolloffMode = AudioRolloffMode.Linear;
				else if (soundSet["RolloffMode"].s == "Logarithmic") tSource.rolloffMode = AudioRolloffMode.Logarithmic;
				break;

			case "MinDistance":
				tSource.minDistance = soundSet["MinDistance"].f;
				break;

			case "MaxDistance":
				tSource.maxDistance = soundSet["MaxDistance"].f;
				break;

			case "PanLevel":
				tSource.spatialBlend = soundSet["PanLevel"].f;
				break;

			case "Spread":
				tSource.spread = soundSet["Spread"].f;
				break;
			}
		}

		// mute it
		tSource.mute = !Data.sfx || Data.muteAllSound;

		// destroy it on loop base
		if (loopAmount != 0)
			tSource.loop = true;
		if (loopAmount >= 0)  // if aLoop == -1 then we loop indefinitely, we simply don't destroy it
			UnityEngine.Object.Destroy(tSource, tClip.length * (loopAmount+1));  // destroy this AudioSource after it's done after its additional loops

		// Play it
		tSource.Play();

		// add it to the list
		effectsAudioSources.Add(tSource);

		// return it
		return tSource;
	}

	// function overloads (kinda getting out of hand). .net 3.5 doesn't support default parameters and I can set it to 4.0 but Unity forces it back
	// without a bundle name parameter the sfx is loaded from the project's resources
	public AudioSource PlayMusic(string aMusicName) { return PlayMusic("", aMusicName, 1.0f, 0, null); }
	public AudioSource PlayMusic(string aMusicName, float aVolume) { return PlayMusic("", aMusicName, aVolume, 0, null); }
	public AudioSource PlayMusic(string aMusicName, GameObject anObjectToBindTo) { return PlayMusic("", aMusicName, 1.0f, 0, anObjectToBindTo); }
	public AudioSource PlayMusic(string aMusicName, float aVolume, int aLoop) { return PlayMusic("", aMusicName, aVolume, aLoop, null); }
	public AudioSource PlayMusic(string aMusicName, float aVolume, int aLoop, GameObject anObjectToBindTo) { return PlayMusic("", aMusicName, aVolume, aLoop, anObjectToBindTo); }
	public AudioSource PlayMusic(string aBundle, string aMusicName) { return PlayMusic(aBundle, aMusicName, 1.0f, 0, null); }
	public AudioSource PlayMusic(string aBundle, string aMusicName, float aVolume) { return PlayMusic(aBundle, aMusicName, aVolume, 0, null); }
	public AudioSource PlayMusic(string aBundle, string aMusicName, GameObject anObjectToBindTo) { return PlayMusic(aBundle, aMusicName, 1.0f, 0, anObjectToBindTo); }
	public AudioSource PlayMusic(string aBundle, string aMusicName, float aVolume, int aLoop) { return PlayMusic(aBundle, aMusicName, aVolume, aLoop, null); }
	public AudioSource PlayMusic(string aBundle, string aMusicName, float aVolume, int aLoop, GameObject anObjectToBindTo)
	{
		GameObject tBindTo = anObjectToBindTo;
		if (tBindTo == null) tBindTo = audioGameObject;  // if not bound to a game object, bind it to the GameAudio, making it 'globally heard'

		AudioClip tClip;
		if (aBundle.Length == 0)  // no bundle given, audio is loaded from the main file (from resources)
		{
			tClip = Resources.Load("Audio/Music/" + aMusicName + "_MSC") as AudioClip;
			if (tClip == null)
			{
				Debug.LogError("Music not found: Audio/Music/" + aMusicName + "_MSC");
				return null;
			}
		} else
		{
			tClip = Loader.LoadAudio(aMusicName + "_MSC");
			if (tClip == null)
			{
				Debug.LogError("Music not found in bundle " + aBundle + ": Audio/" + aMusicName + "_MSC");
				return null;
			}
		}

		AudioSource tSource = tBindTo.AddComponent<AudioSource>();
		tSource.priority = 1;  // Music gets highest priority
		tSource.clip = tClip;
		tSource.volume = aVolume * Data.musicVolume;
		tSource.mute = !Data.music || Data.muteAllSound;

		if (aLoop != 0)
			tSource.loop = true;
		if (aLoop >= 0)  // if aLoop == -1 then we loop indefinitely, we simply don't destroy it
			UnityEngine.Object.Destroy(tSource, tClip.length * (aLoop+1));  // destroy this AudioSource after it's done after its additional loops

		tSource.Play();

		musicAudioSources.Add(tSource);

		return tSource;
	}

	//------------------------------------------------------------------------------------------------------
	// Mute functions (MuteSFX, MuteMusic, MuteAllAudio)
	//------------------------------------------------------------------------------------------------------
	// MuteSFX(false) unmutes sfx, MuteSFX(true) mutes all sfx that are currently playing, but does not force future SFX sounds from playing (use Data.sfx = false for that)
	public void MuteSFX(bool aMuted)
	{
		AudioSource tAudioSource;
		int tCount = effectsAudioSources.Count;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = effectsAudioSources[i];
			if (tAudioSource != null)
			{
				tAudioSource.mute = aMuted;
			} else
			{  // if it is null then we don't need it anyway, remove it from list
				effectsAudioSources.RemoveAt(i);
			}
		}
	}

	// MuteMusic(false) unmutes music, MuteMusic(true) mutes music that is currently playing, but does not force future music from playing (use Data.music = false for that)
	public void MuteMusic(bool aMuted)
	{
		AudioSource tAudioSource;
		int tCount = musicAudioSources.Count;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = musicAudioSources[i];
			if (tAudioSource != null)
			{
				tAudioSource.mute = aMuted;
			} else
			{  // if it is null then we don't need it anyway, remove it from list
				//Debug.Log("Cleaned some music");
				musicAudioSources.RemoveAt(i);
			}
		}
	}

	public void MuteAllAudio(bool aMuted)
	{
		MuteSFX(aMuted);
		MuteMusic(aMuted);
	}

	//------------------------------------------------------------------------------------------------------
	// Stop functions (StopSFX, StopMusic, StopAllSFX, StopAllMusic, StopAllAudio)
	//------------------------------------------------------------------------------------------------------
	// stop a sfx with the given name
	public void StopSFX(string aName)
	{
		string tSFXName = aName + "_SFX";
		int tCount = effectsAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = effectsAudioSources[i];
			if (tAudioSource != null)
			{
				if (tAudioSource.clip.name == tSFXName)
				{  // found the sound
					UnityEngine.Object.Destroy(tAudioSource);
					effectsAudioSources.RemoveAt(i);
					return;
				}
			} else
			{  // if it is null then we don't need it anyway, remove it from list
				effectsAudioSources.RemoveAt(i);
			}
		}
	}

	// stop music with the given name
	public void StopMusic(string aName)
	{
		string tMSCName = aName + "_MSC";
		int tCount = musicAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = musicAudioSources[i];
			if (tAudioSource != null)
			{
				if (tAudioSource.clip.name == tMSCName)
				{  // found the sound
					UnityEngine.Object.Destroy(tAudioSource);
					musicAudioSources.RemoveAt(i);
					return;
				}
			} else
			{  // if it is null then we don't need it anyway, remove it from list
				musicAudioSources.RemoveAt(i);
			}
		}
	}

	// stop all sfx
	public void StopAllSFX()
	{
		int tCount = effectsAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = effectsAudioSources[i];
			if (tAudioSource != null)
				UnityEngine.Object.Destroy(tAudioSource);
			
			effectsAudioSources.RemoveAt(i);
		}
	}

	// stop all music
	public void StopAllMusic()
	{
		int tCount = musicAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = musicAudioSources[i];
			if (tAudioSource != null)
				UnityEngine.Object.Destroy(tAudioSource);
			musicAudioSources.RemoveAt(i);
		}
	}

	// stops all audio (sfx & music)
	public void StopAllAudio()
	{
		StopAllSFX();
		StopAllMusic();
	
	}

	//------------------------------------------------------------------------------------------------------
	// Get functions (GetSFX, GetMusic) as AudioSource
	//------------------------------------------------------------------------------------------------------
	// get a sfx (audio source) with the given name
	public AudioSource GetSFX(string aName)
	{
		string tSFXName = aName + "_SFX";
		int tCount = effectsAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = effectsAudioSources[i];
			if (tAudioSource != null)
			{
				if (tAudioSource.clip.name == tSFXName)
				{  // found the sound
					return tAudioSource;
				}
			} else
			{  // if it is null then we don't need it anyway, remove it from list
				effectsAudioSources.RemoveAt(i);
			}
		}
		return null;
	}

	// get music (audio source) with the given name
	public AudioSource GetMusic(string aName)
	{
		string tMSCName = aName + "_MSC";
		int tCount = musicAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = musicAudioSources[i];
			if (tAudioSource != null)
			{
				if (tAudioSource.clip.name == tMSCName)
				{  // found the sound
					return tAudioSource;
				}
			} else
			{  // if it is null then we don't need it anyway, remove it from list
				musicAudioSources.RemoveAt(i);
			}
		}
		return null;
	}

	//------------------------------------------------------------------------------------------------------
	// Set volume functions (SetSFXVolume, SetMusicVolume, Set
	//------------------------------------------------------------------------------------------------------
	public void SetSFXVolume(string aName, float aVolume)
	{
		string tSFXName = aName + "_SFX";
		int tCount = effectsAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = effectsAudioSources[i];
			if (tAudioSource != null)
			{
				if (tAudioSource.clip.name == tSFXName)
				{  // found the sound
					tAudioSource.volume = aVolume * Data.sfxVolume;
					return;
				}
			} else
			{  // if it is null then we don't need it anyway, remove it from list
				effectsAudioSources.RemoveAt(i);
			}
		}
	}

	public void SetMusicVolume(string aName, float aVolume)
	{
		string tMSCName = aName + "_MSC";
		int tCount = musicAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = musicAudioSources[i];
			if (tAudioSource != null)
			{
				if (tAudioSource.clip.name == tMSCName)
				{  // found the sound
					tAudioSource.volume = aVolume * Data.musicVolume;
					return;
				}
			} else
			{  // if it is null then we don't need it anyway, remove it from list
				musicAudioSources.RemoveAt(i);
			}
		}
	}

	public void SetAllSFXVolume(float aVolume)
	{
		int tCount = effectsAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = effectsAudioSources[i];
			if (tAudioSource != null)
				tAudioSource.volume = aVolume;
		}
	}

	public void SetAllMusicVolume(float aVolume)
	{
		int tCount = musicAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = musicAudioSources[i];
			if (tAudioSource != null)
				tAudioSource.volume = aVolume;
		}
	}


	//------------------------------------------------------------------------------------------------------
	// Clean & Log (CleanAudioSourcesList, LogAllAudio)
	//------------------------------------------------------------------------------------------------------
	// Every once in a while clean the lists, prevent them from filling up to huge sizes
	private void CleanAudioSourcesList()
	{
		int tCount = effectsAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = effectsAudioSources[i];
			if (tAudioSource == null)
			{  // if it is null then we don't need it anyway, remove it from list
				effectsAudioSources.RemoveAt(i);
			}
		}
		
		tCount = musicAudioSources.Count;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = musicAudioSources[i];
			if (tAudioSource == null)
			{  // if it is null then we don't need it anyway, remove it from list
				musicAudioSources.RemoveAt(i);
			}
		}
	}

	public AudioSource LogAllAudio()
	{
		Debug.Log("LogAllAudio:");
		Debug.Log("SFX:");
		int tCount = effectsAudioSources.Count;
		AudioSource tAudioSource;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = effectsAudioSources[i];
			if (tAudioSource != null)
			{
				Debug.Log(tAudioSource.name);
			}
		}

		Debug.Log("Music:");
		tCount = musicAudioSources.Count;
		for (int i = tCount - 1; i >= 0; --i)
		{
			tAudioSource = musicAudioSources[i];
			if (tAudioSource != null)
			{
				Debug.Log(tAudioSource.name);
			}
		}

		return null;
	}
}
