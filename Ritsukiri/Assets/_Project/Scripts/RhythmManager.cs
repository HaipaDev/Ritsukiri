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
    [SerializeField] public FModEventSound clickSound;
    [ChildGameObjectsOnly][SerializeField] Transform canvasParent;
    [ChildGameObjectsOnly][SerializeField] Image bgImg;
    [ChildGameObjectsOnly][SerializeField] Image img;
    [SerializeField] Sprite defaultSpr;
    [SerializeField] Sprite lockedSpr;
    [SerializeField] bool _debugColors;

    [Header("Current Variables")]
    [DisableInEditorMode][SerializeField] bool inputLocked;
    [DisableInEditorMode][SerializeField] HitWindowState hitWindowState;
    [SerializeField] int[] actionHistory=new int[4];
    [SerializeField] bool commandNotPerfect;
    [SerializeField] int commandNotPerfectCount;
    [SerializeField] int comboStatus;
    void Start(){
        clickSound.eventInstance=RuntimeManager.CreateInstance(clickSound.eventName);
        foreach(FModEventSound s in drumSounds){
            s.eventInstance=RuntimeManager.CreateInstance(s.eventName);
        }
        bgColorDefault=new Color(131f/255f,77f/255f,196f/255f);
        bgColorUnlocked=new Color(90f/255f,197f/255f,79f/255f);
        bgColorGood=new Color(252f/255f,239f/255f,141f/255f);
        
        LockInput(false);
        ClearActionHistory();
        SetBGLoop(bgLoopCurrentId);
        StartCheckingRhythm();
    }
    void Update(){CheckInput();}
    
    Color imgColor=Color.white;Color imgColorAbs=Color.white;
    float _currentTimeElapsed,_currentTimeElapsedOverflow,_currentTimeElapsedBetweenInputs,_currentTimeElapsedBetweenCommands,_currentTimeElapsedLocked,_lockedTimer;
    bool _commandExecuting;
    [SerializeField]Color bgColorDefault=new Color(131f/255f,77f/255f,196f/255f);
    [SerializeField]Color bgColorUnlocked=new Color(90f/255f,197f/255f,79f/255f);
    [SerializeField]Color bgColorGood=new Color(252f/255f,239f/255f,141f/255f);
    void FixedUpdate(){bgLoop.eventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE _state);
        if(_state==FMOD.Studio.PLAYBACK_STATE.PLAYING){
            CheckRhythm();
            //bgLoop.eventInstance.setParameterByName("Tempo", bpm);
            float normalizedTime=_currentTimeElapsed/secBetweenBeats;//Debug.Log(_currentTimeElapsed+" | "+normalizedTime);
            img.color=Color.Lerp(new Color(imgColorAbs.r,imgColorAbs.g,imgColorAbs.b,1), new Color(imgColorAbs.r,imgColorAbs.g,imgColorAbs.b,0), normalizedTime);
            if(!inputLocked){img.sprite=defaultSpr;}else{img.sprite=lockedSpr;imgColorAbs=Color.white;}
            if(_debugColors){if(!_isInputLocked()){bgImg.color=bgColorUnlocked;}else{bgImg.color=bgColorDefault;}}
            /*if(hitWindowState==HitWindowState.perfect){bgImg.color=bgColorUnlocked;}
            else if(hitWindowState==HitWindowState.good){bgImg.color=bgColorGood;}
            else{bgImg.color=bgColorDefault;}*/
        }else{SetBGLoop(bgLoopCurrentId);}
    }
    void CheckInput(){
        if(Input.GetKeyDown(KeyCode.A)&&!_isInputLocked()){HitDrum(0);return;}
        else if(Input.GetKeyDown(KeyCode.D)&&!_isInputLocked()){HitDrum(1);return;}
        else if(Input.GetKeyDown(KeyCode.W)&&!_isInputLocked()){HitDrum(2);return;}
        else if(Input.GetKeyDown(KeyCode.S)&&!_isInputLocked()){HitDrum(3);return;}
    }
    void CheckRhythm(){
        if(_currentTimeElapsed>=secBetweenBeats-0.008f){StartCheckingRhythm();}//CheckRhythm();}//0.008f is good for 120?
        else{if(_currentTimeElapsed<0){_currentTimeElapsed=0;}_currentTimeElapsed+=Time.fixedDeltaTime;}

        if(_currentTimeElapsedOverflow<0){_currentTimeElapsedOverflow=0;}_currentTimeElapsedOverflow+=Time.fixedDeltaTime;

        hitWindowState=HitWindowState.locked;//float epsilon=0.001f;
        //if(_currentTimeElapsedOverflow>=secBetweenBeats-0.12f&&_currentTimeElapsedOverflow<=secBetweenBeats+0.4f){hitWindowState=HitWindowState.good;}//0.083f | 0.25f
        //if(_currentTimeElapsedOverflow>=secBetweenBeats-0.0625f&&_currentTimeElapsedOverflow<=secBetweenBeats+0.25f){hitWindowState=HitWindowState.perfect;}//0.0625f | 0.125f
        if(_currentTimeElapsedOverflow>=secBetweenBeats-(secBetweenBeats/6)&&_currentTimeElapsedOverflow<=secBetweenBeats+(secBetweenBeats/2)){hitWindowState=HitWindowState.good;}
        if(_currentTimeElapsedOverflow>=secBetweenBeats-(secBetweenBeats/8)&&_currentTimeElapsedOverflow<=secBetweenBeats+(secBetweenBeats/4)){hitWindowState=HitWindowState.perfect;}
        //if(Mathf.Abs(secBetweenBeats-_currentTimeElapsedOverflow)<=(secBetweenBeats/4)+epsilon){hitWindowState=HitWindowState.good;}
        //if(Mathf.Abs(secBetweenBeats-_currentTimeElapsedOverflow)<=(secBetweenBeats/6)+epsilon){hitWindowState=HitWindowState.perfect;}
        //Debug.Log(_currentTimeElapsed+" | "+_currentTimeElapsedOverflow);
        if(_currentTimeElapsedOverflow>secBetweenBeats+(secBetweenBeats/2f)){_currentTimeElapsedOverflow=_currentTimeElapsed;}//if(!_isActionHistoryEmpty())ClearActionHistory();}

        if(inputLocked){
            if(_currentTimeElapsedLocked>=_lockedTimer-0.04f){
                LockInput(false);ClearActionHistory();_currentTimeElapsedLocked=0;_commandExecuting=false;
            }else{_currentTimeElapsedLocked+=Time.fixedDeltaTime;}
        }else{_currentTimeElapsedLocked=0;}

        if(!_isActionHistoryEmpty()&&!_isActionHistoryFull()){
            if(_currentTimeElapsedBetweenInputs>=(secBetweenBeats+(secBetweenBeats/2))+0.04f){
                LockInput(true,(secBetweenBeats+(secBetweenBeats/2)));ClearActionHistory();_currentTimeElapsedBetweenInputs=0;
                commandNotPerfect=false;commandNotPerfectCount=0;comboStatus=0;
            }else{_currentTimeElapsedBetweenInputs+=Time.fixedDeltaTime;}
        }else if(_isActionHistoryEmpty()){_currentTimeElapsedBetweenInputs=0;}

        if((comboStatus>0||comboStatus==-1)){
            if(_currentTimeElapsedBetweenCommands>=(5*secBetweenBeats+(secBetweenBeats/2)+0.04f)){
                LockInput(true,(secBetweenBeats+(secBetweenBeats/2)));ClearActionHistory();_currentTimeElapsedBetweenCommands=0;
                commandNotPerfect=false;commandNotPerfectCount=0;comboStatus=0;
            }else{_currentTimeElapsedBetweenCommands+=Time.fixedDeltaTime;}
        }else{_currentTimeElapsedBetweenCommands=0;}
    }
    void StartCheckingRhythm(){
        //bgLoop=bgLoops[bgLoopCurrentId];
        //bpm=bgLoop.bpm;
        secBetweenBeats=(60/bpm);
        beatsPerSec=(bpm/60);
        imgColorAbs=Color.white;
        _currentTimeElapsed=0;
    }

    void HitDrum(int i){
        foreach(FModEventSound ds in drumSounds){ds.eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);}
        drumSounds[i].eventInstance.start();
        drumSounds[i].eventInstance.setParameterByName("GoodHitWindow", hitWindowState==HitWindowState.perfect ? 0 : 1);
        if(hitWindowState==HitWindowState.good){imgColorAbs=Color.yellow;commandNotPerfect=true;}else{imgColorAbs=Color.cyan;}

        var _drumImg=Instantiate(drumImgs[i],canvasParent);Destroy(_drumImg,0.5f);
        float _x=0;float _y=0;
        switch(i){
            case 0:
                _x=-50;
                _y=0;
            break;
            case 1:
                _x=+50;
                _y=0;
            break;
            case 2:
                _x=0;
                _y=+50;
            break;
            case 3:
                _x=0;
                _y=-50;
            break;
        }
        Vector2 _random=new Vector2(Random.Range(-10f,10f),Random.Range(-10f,10f));
        _drumImg.transform.localPosition=centerPosForDrumImgs+new Vector2(_x,_y)+_random;

        SetActionHistory(i);
    }
    void SetActionHistory(int id){
        for(var i=0;i<actionHistory.Length;i++){if(actionHistory[i]==-1){actionHistory[i]=id;break;}}
        //if(AreAllValuesEqual(actionHistory)){LockInput(true,(secBetweenBeats+(secBetweenBeats/2)));}
        _currentTimeElapsedBetweenInputs=0;
        
        //Commands
        if(actionHistory[0]==0&&actionHistory[1]==0&&actionHistory[2]==0&&actionHistory[3]==1){Debug.Log("Forward");_commandExecuting=true;}//Forward
        else if(actionHistory[0]==1&&actionHistory[1]==1&&actionHistory[2]==0&&actionHistory[3]==1){Debug.Log("Attack");_commandExecuting=true;}//Attack
        else if(actionHistory[0]==1&&actionHistory[1]==0&&actionHistory[2]==1&&actionHistory[3]==0){Debug.Log("Retreat");_commandExecuting=true;}//Retreat
        else if(actionHistory[0]==1&&actionHistory[1]==1&&actionHistory[2]==2&&actionHistory[3]==2){Debug.Log("Charge");_commandExecuting=true;}//Charge
        else if(actionHistory[0]==2&&actionHistory[1]==2&&actionHistory[2]==0&&actionHistory[3]==1){Debug.Log("Defense");_commandExecuting=true;}//Defense
        else if(actionHistory[0]==3&&actionHistory[1]==3&&actionHistory[2]==2&&actionHistory[3]==2){Debug.Log("Jump");_commandExecuting=true;}//Jump
        else if(actionHistory[0]==3&&actionHistory[1]==3&&actionHistory[2]==1&&actionHistory[3]==0){Debug.Log("Special");_commandExecuting=true;}//Special
        else{if(_isActionHistoryFull()){LockInput(true,(2*secBetweenBeats)-0.125f);}}
        
        if(_commandExecuting){
            LockInput(true,((4*secBetweenBeats)-0.125f));
            if(!commandNotPerfect){clickSound.eventInstance.start();}
            if(comboStatus>=0){
                comboStatus++;
                if(commandNotPerfect){commandNotPerfectCount++;commandNotPerfect=false;}
                if(comboStatus==3&&commandNotPerfectCount==0){comboStatus=-1;}
                if(comboStatus==4&&commandNotPerfectCount==1){comboStatus=-1;}
                if(comboStatus==5&&commandNotPerfectCount>=2){comboStatus=-1;}
            }
        }
    }
    void ClearActionHistory(){for(var i=actionHistory.Length-1;i>=0;i--){actionHistory[i]=-1;}imgColorAbs=Color.red;}
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
        _currentTimeElapsed=0;
    }
}

/*[System.Serializable]
public class actionHistory {
    public int drumId;
    public HitWindowState hitWindowState;
}*/
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