using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using Sirenix.OdinInspector;
using FMODUnity;

public class RhythmManager : MonoBehaviour{
    [Header("Setup")]
    //[SerializeField] public FModEventSound bgLoop;
    [SerializeField] public FModEventSoundLoop[] bgLoops;
    [SerializeField] public int bgLoopCurrentId;
    [SerializeField] public FModEventSoundLoop bgLoop;
    [HideInEditorMode][SerializeField] float bpm=120;
    [HideInEditorMode][SerializeField] float secBetweenBeats=(60/120);
    [HideInEditorMode][SerializeField] float beatsPerSec=(120/60);
    [SerializeField,Searchable] public FModEventSound[] drumSounds=new FModEventSound[4];
    [SerializeField,Searchable] public GameObject[] drumImgs=new GameObject[4];
    [SerializeField,Searchable] public Vector2 centerPosForDrumImgs;
    [SerializeField] public FModEventSound perfectSound;
    [SerializeField] public FModEventSound feverSound;
    [ChildGameObjectsOnly][SerializeField] Transform canvasParent;
    [ChildGameObjectsOnly][SerializeField] Transform drumImgsParent;
    [ChildGameObjectsOnly][SerializeField] Image bgImg;
    [ChildGameObjectsOnly][SerializeField] Image img;
    [SerializeField] Sprite defaultSpr;
    [SerializeField] Sprite lockedSpr;
    [SerializeField] bool _debug;

    [Header("Current Variables")]
    [DisableInEditorMode][SerializeField] bool inputLocked;
    [DisableInEditorMode][SerializeField] HitWindowState hitWindowState;
    [SerializeField] int[] actionHistory=new int[4];
    [SerializeField] bool commandNotPerfect;
    [SerializeField] int commandNotPerfectCount;
    [SerializeField] int comboStatus;
    [SerializeField] int mashingCount;
    void Start(){
        perfectSound.eventInstance=RuntimeManager.CreateInstance(perfectSound.eventName);
        feverSound.eventInstance=RuntimeManager.CreateInstance(feverSound.eventName);
        foreach(FModEventSound s in drumSounds){
            s.eventInstance=RuntimeManager.CreateInstance(s.eventName);
        }
        bgColorDefault=new Color(131f/255f,77f/255f,196f/255f);
        bgColorFever=new Color(244f/255f,147f/255f,115f/255f);
        bgColorUnlocked=new Color(90f/255f,197f/255f,79f/255f);
        bgColorGood=new Color(252f/255f,239f/255f,141f/255f);
        
        LockInput(false);
        ClearActionHistory();
        SetBGLoop(bgLoopCurrentId);
        StartUpdatingRhythm();
    }
    void Update(){CheckInput();}
    
    Color imgColor=Color.white;Color imgColorAbs=Color.white;
    float _timeElapsed,_timeElapsedOverflow,_timeElapsedBetweenInputs,_timeElapsedBetweenCommands,_timeElapsedLocked,_lockedTimer;
    bool _commandExecuting;
    [SerializeField]Color bgColorDefault=new Color(131f/255f,77f/255f,196f/255f);
    [SerializeField]Color bgColorFever=new Color(244f/255f,147f/255f,115f/255f);
    [SerializeField]Color bgColorUnlocked=new Color(90f/255f,197f/255f,79f/255f);
    [SerializeField]Color bgColorGood=new Color(252f/255f,239f/255f,141f/255f);
    void FixedUpdate(){
        bgLoop.eventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE _state);
        if(_state==FMOD.Studio.PLAYBACK_STATE.PLAYING){
            CheckRhythm();
            float normalizedTime=_timeElapsed/secBetweenBeats;//Debug.Log(_timeElapsed+" | "+normalizedTime);
            img.color=Color.Lerp(new Color(imgColorAbs.r,imgColorAbs.g,imgColorAbs.b,1), new Color(imgColorAbs.r,imgColorAbs.g,imgColorAbs.b,0), normalizedTime);
            if(!inputLocked){img.sprite=defaultSpr;}else{img.sprite=lockedSpr;imgColorAbs=Color.white;}
            if(_debug){
                if(!_isInputLocked()){
                    bgImg.color=bgColorUnlocked;
                    /*if(hitWindowState==HitWindowState.perfect){bgImg.color=bgColorUnlocked;}
                    else if(hitWindowState==HitWindowState.good){bgImg.color=bgColorGood;}*/
                }
                else{
                    if(comboStatus!=-1){bgImg.color=bgColorDefault;}
                    else{bgImg.color=bgColorFever;}
                }
            }
        }else{SetBGLoop(bgLoopCurrentId);}
    }
    void CheckInput(){
        if(Input.GetKeyDown(KeyCode.A)&&!_isInputLocked()){HitDrum(0);return;}
        else if(Input.GetKeyDown(KeyCode.D)&&!_isInputLocked()){HitDrum(1);return;}
        else if(Input.GetKeyDown(KeyCode.W)&&!_isInputLocked()){HitDrum(2);return;}
        else if(Input.GetKeyDown(KeyCode.S)&&!_isInputLocked()){HitDrum(3);return;}
    }
    void CheckRhythm(){
        if(_timeElapsed>=secBetweenBeats-0.008f&&secBetweenBeats>0.008f){StartUpdatingRhythm();}//0.008f is good for 120?
        else{
            if(_timeElapsed<0){_timeElapsed=0;}
            //bgLoop.eventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE _state);
            //if(_state==FMOD.Studio.PLAYBACK_STATE.PLAYING){_timeElapsed+=Time.fixedDeltaTime;}else{_timeElapsed=0;}
            bgLoop.eventInstance.getTimelinePosition(out int _timelinePos);
            _timeElapsed=(_timelinePos/1000f);
        }
        //else{if(_timeElapsed<0){_timeElapsed=0;}_timeElapsed+=Time.fixedDeltaTime;}

        if(_timeElapsedOverflow<0){_timeElapsedOverflow=0;}_timeElapsedOverflow+=Time.fixedDeltaTime;
        _timeElapsedOverflow=(float)System.Math.Round(_timeElapsedOverflow,3);
        if(_debug)Debug.Log(_timeElapsed+" | "+_timeElapsedOverflow);
        if(_timeElapsedOverflow>secBetweenBeats+(secBetweenBeats/2f)){_timeElapsedOverflow=_timeElapsed;}

        ///HitWindow Checking
        hitWindowState=HitWindowState.locked;//float epsilon=0.001f;
        //if(_timeElapsedOverflow>=secBetweenBeats-0.12f&&_timeElapsedOverflow<=secBetweenBeats+0.4f){hitWindowState=HitWindowState.good;}//0.083f | 0.25f
        //if(_timeElapsedOverflow>=secBetweenBeats-0.0625f&&_timeElapsedOverflow<=secBetweenBeats+0.25f){hitWindowState=HitWindowState.perfect;}//0.0625f | 0.125f
        if(_timeElapsedOverflow>=secBetweenBeats-(secBetweenBeats/6)&&_timeElapsedOverflow<=secBetweenBeats+(secBetweenBeats/2)){hitWindowState=HitWindowState.good;}
        if(_timeElapsedOverflow>=secBetweenBeats-(secBetweenBeats/8)&&_timeElapsedOverflow<=secBetweenBeats+(secBetweenBeats/4)){hitWindowState=HitWindowState.perfect;}
        //if(Mathf.Abs(secBetweenBeats-_timeElapsedOverflow)<=(secBetweenBeats/4)+epsilon){hitWindowState=HitWindowState.good;}
        //if(Mathf.Abs(secBetweenBeats-_timeElapsedOverflow)<=(secBetweenBeats/6)+epsilon){hitWindowState=HitWindowState.perfect;}

        if(inputLocked){//Unlocking
            if(_timeElapsedLocked>=_lockedTimer-0.04f){
                LockInput(false);ClearActionHistory();_timeElapsedLocked=0;if(_timeElapsedBetweenCommands==0)_commandExecuting=false;
            }else{_timeElapsedLocked+=Time.fixedDeltaTime;}
        }else{_timeElapsedLocked=0;}

        if(!_isActionHistoryEmpty()&&!_isActionHistoryFull()){///Time after a single drum before reset
            if(_timeElapsedBetweenInputs>=(secBetweenBeats+(secBetweenBeats/2))+0.04f){
                LockInput(true,(secBetweenBeats+(secBetweenBeats/2)));ClearActionHistory();_timeElapsedBetweenInputs=0;
                commandNotPerfect=false;commandNotPerfectCount=0;comboStatus=0;mashingCount=0;
            }else{_timeElapsedBetweenInputs+=Time.fixedDeltaTime;}
        }else if(_isActionHistoryEmpty()){_timeElapsedBetweenInputs=0;}

        if(_commandExecuting&&(comboStatus>0||comboStatus==-1)){///After a successful command, count before resetting combo etc
            if(_timeElapsedBetweenCommands>=(6*secBetweenBeats+(secBetweenBeats/2)+0.04f)){
                LockInput(true,(secBetweenBeats+(secBetweenBeats/2)));ClearActionHistory();_timeElapsedBetweenCommands=0;
                commandNotPerfect=false;commandNotPerfectCount=0;comboStatus=0;mashingCount=0;
            }else{_timeElapsedBetweenCommands+=Time.fixedDeltaTime;}
        }else{_timeElapsedBetweenCommands=0;}
    }
    void StartUpdatingRhythm(){
        secBetweenBeats=(60/bpm);
        beatsPerSec=(bpm/60);
        imgColorAbs=Color.white;
        _timeElapsed=0;
    }

    void HitDrum(int i){
        foreach(FModEventSound ds in drumSounds){ds.eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);}
        drumSounds[i].eventInstance.start();
        drumSounds[i].eventInstance.setParameterByName("GoodHitWindow", hitWindowState==HitWindowState.perfect ? 0 : 1);
        if(hitWindowState==HitWindowState.good){commandNotPerfect=true;imgColorAbs=Color.yellow;}else{imgColorAbs=Color.cyan;}

        VisualizeDrumHit(i);
        SetActionHistory(i);
    }
    void VisualizeDrumHit(int i){
        var _drumImg=Instantiate(drumImgs[i],drumImgsParent);Destroy(_drumImg,0.5f);
        float _x=0;float _y=0;
        switch(i){
            case 0:
                _x=-50;
            break;
            case 1:
                _x=+50;
            break;
            case 2:
                _y=+50;
            break;
            case 3:
                _y=-50;
            break;
        }
        Vector2 _random=new Vector2(Random.Range(-10f,10f),Random.Range(-10f,10f));
        _drumImg.transform.localPosition=(Vector2)drumImgsParent.transform.localPosition+new Vector2(_x,_y)+_random;
    }
    void SetActionHistory(int id){
        for(var i=0;i<actionHistory.Length;i++){if(actionHistory[i]==-1){actionHistory[i]=id;break;}}
        if(_timeElapsedBetweenInputs<secBetweenBeats/3&&_timeElapsedBetweenInputs!=0){mashingCount++;}
        if(mashingCount>2){ClearActionHistory();mashingCount=0;LockInput(true,secBetweenBeats+0.125f);}
        //if(_timeElapsedBetweenInputs>secBetweenBeats/3||_timeElapsedBetweenInputs==0){for(var i=0;i<actionHistory.Length;i++){if(actionHistory[i]==-1){actionHistory[i]=id;break;}}}else{ClearActionHistory();}
        //if(AreAllValuesEqual(actionHistory)){LockInput(true,(secBetweenBeats+(secBetweenBeats/2)));}
        _timeElapsedBetweenInputs=0;_timeElapsedBetweenCommands=0;_commandExecuting=false;
        
        //Commands
        if(actionHistory[0]==0&&actionHistory[1]==0&&actionHistory[2]==0&&actionHistory[3]==1){Debug.Log("Forward");_commandExecuting=true;}//Forward
        else if(actionHistory[0]==1&&actionHistory[1]==1&&actionHistory[2]==0&&actionHistory[3]==1){Debug.Log("Attack");_commandExecuting=true;}//Attack
        else if(actionHistory[0]==1&&actionHistory[1]==0&&actionHistory[2]==1&&actionHistory[3]==0){Debug.Log("Retreat");_commandExecuting=true;}//Retreat
        else if(actionHistory[0]==1&&actionHistory[1]==1&&actionHistory[2]==2&&actionHistory[3]==2){Debug.Log("Charge");_commandExecuting=true;}//Charge
        else if(actionHistory[0]==2&&actionHistory[1]==2&&actionHistory[2]==0&&actionHistory[3]==1){Debug.Log("Defense");_commandExecuting=true;}//Defense
        else if(actionHistory[0]==3&&actionHistory[1]==3&&actionHistory[2]==2&&actionHistory[3]==2){Debug.Log("Jump");_commandExecuting=true;}//Jump
        else if(actionHistory[0]==3&&actionHistory[1]==3&&actionHistory[2]==1&&actionHistory[3]==0){Debug.Log("Special");_commandExecuting=true;}//Special
        else{if(_isActionHistoryFull()){LockInput(true,(2*secBetweenBeats)-0.125f);_commandExecuting=false;comboStatus=0;commandNotPerfectCount=0;commandNotPerfect=false;}}
        
        if(_commandExecuting){
            LockInput(true,((4*secBetweenBeats)));//-(secBetweenBeats/2)));
            if(!commandNotPerfect){perfectSound.eventInstance.start();}
            mashingCount=0;
            if(comboStatus>=0){
                comboStatus++;
                if(commandNotPerfect){commandNotPerfectCount++;commandNotPerfect=false;}
                if(comboStatus==3&&commandNotPerfectCount==0){ActivateFever();}
                if(comboStatus==4&&commandNotPerfectCount==1){ActivateFever();}
                if(comboStatus==5&&commandNotPerfectCount>=2){ActivateFever();}
                if(comboStatus>5){ActivateFever();}
            }
        }
    }
    void ActivateFever(){
        comboStatus=-1;
        feverSound.eventInstance.start();
    }
    void ClearActionHistory(){for(var i=actionHistory.Length-1;i>=0;i--){actionHistory[i]=-1;}/*imgColorAbs=Color.red;*/}
    bool _isActionHistoryEmpty(){bool _isEmpty=true;for(var i=actionHistory.Length-1;i>=0;i--){if(actionHistory[i]!=-1){_isEmpty=false;}}return _isEmpty;}
    bool _isActionHistoryFull(){bool _isFull=true;for(var i=actionHistory.Length-1;i>=0;i--){if(actionHistory[i]==-1){_isFull=false;}}return _isFull;}
    bool _isInputLocked(){return inputLocked||hitWindowState==HitWindowState.locked;}
    bool AreAllValuesEqual(int[] array){
        if(_isActionHistoryFull()){
            for(int i=1;i<array.Length;i++){
                if(array[i]==array[0]){return false;}
            }
            return true;
        }return false;
    }
    void LockInput(bool _isLocked=true,float timer=2f){
        inputLocked=_isLocked;
        if(_isLocked){_lockedTimer=timer;}else{_lockedTimer=0;}
        bgLoop.eventInstance.setParameterByName("LockedFX", _isLocked ? 1 : 0);
    }
    public void SetBGLoop(int id=0){
        //if(bgLoop.eventInstance!=null){
            bgLoop.eventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE _state);
            if(_state==FMOD.Studio.PLAYBACK_STATE.PLAYING)bgLoop.eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        //}
        bgLoop=bgLoops[bgLoopCurrentId];
        bgLoop.eventInstance=RuntimeManager.CreateInstance(bgLoop.eventName);
        bgLoop.eventInstance.start();

        bpm=bgLoop.bpm;
        secBetweenBeats=(60/bpm);
        beatsPerSec=(bpm/60);
        _timeElapsed=0;
    }
}

[System.Serializable]
public class FModEventSound {
	public string eventName;
	[DisableInEditorMode]public FMOD.Studio.EventInstance eventInstance;
}
[System.Serializable]
public class FModEventSoundLoop {
	public string eventName="event:/BGLoop120";
	public int bpm=120;
	[DisableInEditorMode]public FMOD.Studio.EventInstance eventInstance;
}
public enum HitWindowState{locked,good,perfect}