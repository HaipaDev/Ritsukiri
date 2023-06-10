using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using Sirenix.OdinInspector;

public class RhythmManager : MonoBehaviour{
    [SerializeField] AudioMixer audioMixer;
    [SerializeField] AudioMixerGroup musicMixerGroup;
    [SerializeField] AudioMixerGroup soundsMixerGroup;
    [SerializeField] AudioClip bgLoop;
    [DisableInEditorMode] AudioSource bgLoopSource;
    [SerializeField] float bpm=120;
    [HideInEditorMode][SerializeField] float secBetweenBeats=(60/120);
    [SerializeField,Searchable] DrumSound[] drumSounds;
    [SerializeField] DrumSound clickSound;
    [SerializeField] Image img;
    [SerializeField] Sprite defaultSpr;
    [SerializeField] Sprite lockedSpr;
    [DisableInEditorMode][SerializeField] bool inputLocked;
    [DisableInEditorMode][SerializeField] HitWindowState hitWindowState;

    [SerializeField] int[] actionHistory=new int[4];
    void Start(){
        //GetComponent<AudioSource>().clip=bgLoop;
        bgLoopSource=gameObject.AddComponent<AudioSource>();
        bgLoopSource.clip=bgLoop;
        bgLoopSource.loop=false;
        bgLoopSource.playOnAwake=true;
        bgLoopSource.outputAudioMixerGroup=musicMixerGroup;

        foreach(DrumSound s in drumSounds){
			s.source=gameObject.AddComponent<AudioSource>();
			s.source.clip=s.clip;
			s.source.loop=false;
			s.source.playOnAwake=false;

			s.source.outputAudioMixerGroup=soundsMixerGroup;
		}
        
        inputLocked=false;
        ClearActionHistory();
        CheckRhythm();
    }
    void Update(){}
    
    Color imgColor=Color.white;Color imgColorAbs=Color.white;
    float _currentTimeElapsed,_currentTimeElapsedOverflow,_currentTimeElapsedBetweenCommands;
    void FixedUpdate() {
        StartCheckingRhythm();
        float normalizedTime=_currentTimeElapsed/secBetweenBeats;//Debug.Log(_currentTimeElapsed+" | "+normalizedTime);
        imgColor=Color.Lerp(new Color(imgColorAbs.r,imgColorAbs.g,imgColorAbs.b,1), new Color(imgColorAbs.r,imgColorAbs.g,imgColorAbs.b,0), normalizedTime);
        img.color=imgColor;
        if(!inputLocked){img.sprite=defaultSpr;}else{img.sprite=lockedSpr;imgColorAbs=Color.white;}
        
        if(Input.GetKeyDown(KeyCode.A)&&!_isInputLocked()){HitDrum(0);}
        if(Input.GetKeyDown(KeyCode.W)&&!_isInputLocked()){HitDrum(1);}
        if(Input.GetKeyDown(KeyCode.S)&&!_isInputLocked()){HitDrum(2);}
        if(Input.GetKeyDown(KeyCode.D)&&!_isInputLocked()){HitDrum(3);}
    }
    void StartCheckingRhythm(){
        if(_currentTimeElapsed>=secBetweenBeats-0.008f){CheckRhythm();}//0.008f is good for 120?
        else{if(_currentTimeElapsed<0){_currentTimeElapsed=0;}_currentTimeElapsed+=Time.fixedDeltaTime;}

        if(_currentTimeElapsedOverflow<0){_currentTimeElapsedOverflow=0;}_currentTimeElapsedOverflow+=Time.fixedDeltaTime;

        hitWindowState=HitWindowState.locked;
        if(_currentTimeElapsedOverflow>=secBetweenBeats-(secBetweenBeats/2)&&_currentTimeElapsedOverflow<=secBetweenBeats+(secBetweenBeats/2)){hitWindowState=HitWindowState.good;}
        if(_currentTimeElapsedOverflow>=secBetweenBeats-(secBetweenBeats/4)&&_currentTimeElapsedOverflow<=secBetweenBeats+(secBetweenBeats/4)){hitWindowState=HitWindowState.perfect;}

        Debug.Log(_currentTimeElapsed+" | "+_currentTimeElapsedOverflow);

        if(_currentTimeElapsedOverflow>secBetweenBeats+(secBetweenBeats/1.7f)){_currentTimeElapsedOverflow=_currentTimeElapsed;}//if(!_isActionHistoryEmpty())ClearActionHistory();}
        if(!_isActionHistoryEmpty()){
            if(_currentTimeElapsedBetweenCommands>=((4/(bpm/60))+0.04f)){
                ClearActionHistory();_currentTimeElapsedBetweenCommands=0;
            }else{_currentTimeElapsedBetweenCommands+=Time.fixedDeltaTime;}
        }else{_currentTimeElapsedBetweenCommands=0;}
    }
    void CheckRhythm(){secBetweenBeats=(60/bpm);Debug.Log("CheckingRhythm at: "+bpm+" BPM");
        /*bgLoopSource.Stop();*/bgLoopSource.Play();

        /*Color startColor=new Color(1,1,1,1);Color endColor=new Color(1,1,1,0);imgColor=startColor;
        for(float t=0f;t<secBetweenBeats;t+=Time.fixedDeltaTime){
            float normalizedTime = t/secBetweenBeats;Debug.Log(t+" | "+normalizedTime);
            //right here, you can now use normalizedTime as the third parameter in any Lerp from start to end
            imgColor = Color.Lerp(startColor, endColor, normalizedTime);
        }*/
        /*Color startColor=new Color(1,1,1,1);Color endColor=new Color(1,1,1,0);
        for(float t=0f;t<duration;t+=Time.deltaTime){
            float normalizedTime = t/duration;Debug.Log(t+" | "+normalizedTime);
            //right here, you can now use normalizedTime as the third parameter in any Lerp from start to end
            imgColor = Color.Lerp(startColor, endColor, normalizedTime);
            yield return null;
        }
        imgColor=startColor;*/
        //img.sprite=defaultSpr;
        imgColorAbs=Color.white;
        _currentTimeElapsed=0;
        //_currentTimeElapsedOverflow=_currentTimeElapsed;
        //StartCheckingRhythm();
    }

    void HitDrum(int i){
        foreach(DrumSound ds in drumSounds){ds.source.Stop();}
        Play(i);
        SetActionHistory(i);
        imgColorAbs=Color.cyan;
        //img.sprite=lockedSpr;
    }
    void SetActionHistory(int id){
        for(var i=0;i<actionHistory.Length;i++){if(actionHistory[i]==-1){actionHistory[i]=id;break;}}
    }
    void ClearActionHistory(){for(var i=actionHistory.Length-1;i>=0;i--){actionHistory[i]=-1;}imgColorAbs=Color.red;}
    bool _isActionHistoryEmpty(){bool _isEmpty=true;for(var i=actionHistory.Length-1;i>=0;i--){if(actionHistory[i]!=-1){_isEmpty=false;}}return _isEmpty;}
    bool _isInputLocked(){return inputLocked||hitWindowState==HitWindowState.locked;}
    

    public void Play(int id){
		DrumSound s = drumSounds[id];
		if (s == null){
			Debug.LogWarning("DrumSound by id: " + id + " not found!");
			return;
		}

		s.source.volume = s.volume * (1f + UnityEngine.Random.Range(-s.volumeVariance / 2f, s.volumeVariance / 2f));
		s.source.pitch = s.pitch * (1f + UnityEngine.Random.Range(-s.pitchVariance / 2f, s.pitchVariance / 2f));

		s.source.Play();
	}
}

[System.Serializable]
public class DrumSound {
	public AudioClip clip;

	[Range(0f, 1f)]
	public float volume = .75f;
	[Range(0f, 1f)]
	public float volumeVariance = 0f;

	[Range(.1f, 3f)]
	public float pitch = 1f;
	[Range(0f, 1f)]
	public float pitchVariance = 0.02f;

	public AudioMixerGroup mixerGroup;

	[HideInInspector]
	public AudioSource source;
}
public enum HitWindowState{locked,good,perfect}