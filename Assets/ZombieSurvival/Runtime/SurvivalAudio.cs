using UnityEngine;
namespace DeadDistrict {
 public sealed class SurvivalAudio : MonoBehaviour {
  AudioSource music,battleMusic,steps,upgradeVoice,reloadVoice,switchVoice;AudioSource[] voices,blasts,launcherBlasts;AudioSource launcherVoice;int launcherBlastIndex;int index,blastIndex;float nextHit,nextKill,nextHurt,blastDuckUntil;
  AudioClip[] shots,hits;AudioClip dodge,reload,shotgunShot,shotgunReload,shotgunReloadSequence,boom,death,hurt,alert,pickup,launcherShot,launcherBoom;
  bool paused,ended,walking,pressure;readonly System.Collections.Generic.Dictionary<AudioSource,float> gains=new System.Collections.Generic.Dictionary<AudioSource,float>();
  void Gain(AudioSource source,float value){gains[source]=value;source.volume=value*MobilePreferences.Current.sfx;}
  public int ShotgunSoundsPlayed {get;private set;}
  public bool HasBattleMusic=>battleMusic&&battleMusic.clip&&battleMusic.loop;
  public float ExplorationMusicVolume=>music?music.volume:0;
  public float BattleMusicVolume=>battleMusic?battleMusic.volume:0;
  public void SetPressure(bool value){pressure=value;}
  public bool MusicEnabled=>MobilePreferences.Current.music>0;
  public bool SfxEnabled=>MobilePreferences.Current.sfx>0;
  public int LoadedClips {get;private set;}
  public int ShotsPlayed {get;private set;}
  public bool ExplosionPlaying=>blasts!=null&&(blasts[0].isPlaying||blasts[1].isPlaying);
  public float ExplosionDuration=>boom?boom.length:0;
  public bool HasLoopingMusic=>music&&music.clip&&music.loop;
  AudioClip Load(string name){var clip=Resources.Load<AudioClip>("DeadDistrict/Audio/"+name);if(!clip)throw new System.InvalidOperationException("Audio missing: "+name);LoadedClips++;return clip;}
  AudioSource Source(string name){var g=new GameObject(name);g.transform.SetParent(transform,false);var s=g.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;s.dopplerLevel=0;s.priority=100;return s;}
  public void Initialize(){
   launcherShot=Load("LauncherShot");launcherBoom=Load("LauncherBlast");launcherVoice=Source("Reserved launcher shot");launcherVoice.priority=32;launcherBlasts=new[]{Source("Launcher blast 0"),Source("Launcher blast 1"),Source("Launcher blast 2")};foreach(var s in launcherBlasts)s.priority=30;
   switchVoice=Source("Reserved weapon switch");switchVoice.priority=35;
   reloadVoice=Source("Reserved weapon reload");reloadVoice.priority=35;
   upgradeVoice=Source("Upgrade confirmation");upgradeVoice.priority=40;

   music=Source("Background music");music.clip=Load("Music");music.loop=true;music.volume=0;music.priority=128;music.Play();
   battleMusic=Source("Horde music");battleMusic.clip=Load("BattleMusic");battleMusic.loop=true;battleMusic.volume=0;battleMusic.priority=128;battleMusic.Play();
   steps=Source("Footsteps");steps.clip=Load("Steps");steps.loop=true;Gain(steps,.18f);
   blasts=new[]{Source("Reserved grenade voice 0"),Source("Reserved grenade voice 1")};foreach(var s in blasts)s.priority=30;
   voices=new AudioSource[12];for(int i=0;i<voices.Length;i++)voices[i]=Source("Combat voice "+i);
   shots=new[]{Load("Rifle1"),Load("Rifle2"),Load("Rifle3")};hits=new[]{Load("Hit1"),Load("Hit2"),Load("Hit3")};
   dodge=Load("Dodge");shotgunShot=Load("Shotgun");shotgunReload=Load("ShotgunReload");reload=Load("Reload");shotgunReloadSequence=Load("ShotgunReloadSequence");boom=Load("Explosion");death=Load("Death");hurt=Load("Hurt");alert=Load("Horde");pickup=Load("Pickup");
  }
  void Play(AudioClip clip,float gain,float pitch=1){if(!SfxEnabled||paused)return;var s=voices[index++%voices.Length];s.Stop();s.clip=clip;Gain(s,gain*(Time.time<blastDuckUntil?.4f:1));s.pitch=pitch;s.Play();}
  public void Shot(bool shotgun=false){ShotsPlayed++;if(shotgun)ShotgunSoundsPlayed++;Play(shotgun?shotgunShot:shots[ShotsPlayed%shots.Length],shotgun?.65f:.43f,Random.Range(.94f,1.05f));}
  public int LauncherSoundsPlayed {get;private set;}
  public bool HasDistinctLauncherAudio=>launcherShot&&launcherBoom&&launcherShot!=shotgunShot&&launcherBoom!=boom;
  public void LauncherShot(){ShotsPlayed++;LauncherSoundsPlayed++;if(!SfxEnabled||paused)return;launcherVoice.Stop();launcherVoice.clip=launcherShot;Gain(launcherVoice,.7f);launcherVoice.pitch=Random.Range(.97f,1.03f);launcherVoice.Play();}
  public void LauncherBlast(){if(!SfxEnabled||paused)return;var s=launcherBlasts[launcherBlastIndex++%launcherBlasts.Length];s.Stop();s.clip=launcherBoom;Gain(s,.65f);s.pitch=Random.Range(.97f,1.04f);s.Play();}
  public void Upgrade(){if(!SfxEnabled)return;upgradeVoice.Stop();upgradeVoice.clip=pickup;Gain(upgradeVoice,.48f);upgradeVoice.pitch=1.15f;upgradeVoice.Play();}
  public int WeaponSwitchesPlayed {get;private set;}
  public bool WeaponSwitchAudioPlaying=>switchVoice&&switchVoice.isPlaying;
  public void WeaponSwitch(){if(!SfxEnabled||paused)return;switchVoice.Stop();switchVoice.clip=shotgunReload;Gain(switchVoice,.64f);switchVoice.pitch=1.1f;switchVoice.Play();WeaponSwitchesPlayed++;}
  public void Mechanism(){Play(shotgunReload,.38f,1.25f);}
  public void AcidSpit(){Play(death,.4f,.62f);}
  public void AcidLand(){Play(hits[1],.5f,.65f);}
  public void BreakCrate(){Play(hits[0],.6f,.65f);}
  public void Roll(){Play(dodge,.45f,.85f);}
  public void Hit(){if(Time.time<nextHit)return;nextHit=Time.time+.13f;Play(hits[index%hits.Length],.34f,Random.Range(.93f,1.1f));}
  public void Pickup(bool heal){Play(pickup,.38f,heal?1.05f:1.2f);}
  public int ShotgunReloadsPlayed {get;private set;}
  public bool ReloadAudioPlaying=>reloadVoice&&reloadVoice.isPlaying;
  public void CancelReload(){if(reloadVoice)reloadVoice.Stop();}
  public void Reload(bool shotgun=false,float duration=2.1f){if(!SfxEnabled||paused)return;reloadVoice.Stop();reloadVoice.clip=reload;Gain(reloadVoice,.52f);reloadVoice.pitch=Mathf.Max(1,reload.length/Mathf.Max(.1f,duration));reloadVoice.Play();if(shotgun)ShotgunReloadsPlayed++;}
  public void Explosion(){if(!SfxEnabled||paused)return;var s=blasts[blastIndex++%blasts.Length];s.Stop();s.clip=boom;blastDuckUntil=Time.time+.45f;Gain(s,.92f);s.pitch=Random.Range(.96f,1.02f);s.Play();}
  public void Kill(){if(Time.time<nextKill)return;nextKill=Time.time+.3f;Play(death,.24f,Random.Range(.78f,.95f));}
  public void Hurt(){if(Time.time<nextHurt)return;nextHurt=Time.time+.28f;Play(hurt,.45f);}
  public void Horde(){Play(alert,.4f,.8f);}
  public void SetWalking(bool value){walking=value;}
  public void Pause(bool value){paused=value;if(value)launcherVoice.Pause();else launcherVoice.UnPause();foreach(var s in launcherBlasts){if(value)s.Pause();else s.UnPause();}if(value){reloadVoice.Pause();switchVoice.Pause();}else{reloadVoice.UnPause();switchVoice.UnPause();}foreach(var s in voices){if(value)s.Pause();else s.UnPause();}foreach(var s in blasts){if(value)s.Pause();else s.UnPause();}if(value)steps.Pause();}
  void StopLauncher(){launcherVoice.Stop();foreach(var s in launcherBlasts)s.Stop();}
  public void Victory(){StopLauncher();ended=true;pressure=false;walking=false;steps.Stop();switchVoice.Stop();CancelReload();foreach(var v in voices)v.Stop();foreach(var v in blasts)v.Stop();Upgrade();}
  public void EndRun(){StopLauncher();switchVoice.Stop();CancelReload();ended=true;walking=false;Play(hurt,.5f,.75f);}
  public void ToggleMusic(){var d=MobilePreferences.Current;d.music=d.music>0?0:1;MobilePreferences.Set(d);MobilePreferences.Flush();}
  public void ToggleSfx(){var d=MobilePreferences.Current;d.sfx=d.sfx>0?0:1;MobilePreferences.Set(d);MobilePreferences.Flush();}
  void Update(){
   if(!music)return;foreach(var pair in gains)if(pair.Key)pair.Key.volume=pair.Value*MobilePreferences.Current.sfx;
   if(!paused&&reloadVoice.isPlaying&&(!SurvivalGame.Instance||!SurvivalGame.Instance.Reloading))CancelReload();
   float gain=MusicEnabled?(paused?.06f:ended?.055f:ExplosionPlaying?.06f:.18f)*MobilePreferences.Current.music:0;
   bool battle=pressure&&!paused&&!ended;
   music.volume=Mathf.MoveTowards(music.volume,battle?0:gain,Time.unscaledDeltaTime*.13f);
   battleMusic.volume=Mathf.MoveTowards(battleMusic.volume,battle?gain:0,Time.unscaledDeltaTime*.13f);
   bool want=walking&&!paused&&!ended&&SfxEnabled;
   if(want&&!steps.isPlaying)steps.Play();else if(!want&&steps.isPlaying)steps.Stop();
  }
 }
}
