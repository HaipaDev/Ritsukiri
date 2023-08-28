using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    [SerializeField] public FModEventSound feverEndSound;
    [SerializeField] public FModEventSound comboEndSound;
    [SerializeField] public FModEventSound specialSound;
    [ChildGameObjectsOnly][SerializeField] Transform canvasParent;
    [ChildGameObjectsOnly][SerializeField] Transform drumImgsParent;
    [ChildGameObjectsOnly][SerializeField] Image bgImg;
    [ChildGameObjectsOnly][SerializeField] Image bgFeverImg;
    [ChildGameObjectsOnly][SerializeField] Image bgMugenImg;
    [ChildGameObjectsOnly][SerializeField] Image frameImg;
    [ChildGameObjectsOnly][SerializeField] Transform comboBarParent;
    [ChildGameObjectsOnly][SerializeField] Image comboBarGekka;
    [ChildGameObjectsOnly][SerializeField] Image comboBarFill;
    [ChildGameObjectsOnly][SerializeField] Image comboBarFillFull;
    [ChildGameObjectsOnly][SerializeField] Image comboBarMugen;
    [ChildGameObjectsOnly][SerializeField] TextMeshProUGUI comboBarText;
    [SerializeField] Vector2 comboBarPosShown;
    [SerializeField] Vector2 comboBarPosHidden;

    [SerializeField] Sprite defaultSpr;
    [SerializeField] Sprite lockedSpr;
    [SerializeField]Color colorPerfect=new Color(141f/255f,183f/255f,255f/255f);
    [SerializeField]Color colorGood=new Color(252f/255f,239f/255f,141f/255f);
    [SerializeField] bool _debug;
    [SerializeField] bool _lockinCombo;
    [SerializeField] int feverPowerMax=100;

    [Header("Current Variables")]
    [DisableInEditorMode][SerializeField] bool inputLocked;
    [DisableInEditorMode][SerializeField] HitWindowState hitWindowState;
    [DisableInEditorMode][SerializeField] int[] actionHistory=new int[4];
    [DisableInEditorMode][SerializeField]bool _commandExecuting;
    [DisableInEditorMode][SerializeField] bool commandNotPerfect;
    [DisableInEditorMode][SerializeField] int commandNotPerfectCount;
    [DisableInEditorMode][SerializeField] int comboStatus;
    [DisableInEditorMode][SerializeField] float feverPower;
    [DisableInEditorMode][SerializeField] int mashingCount;
    [DisableInEditorMode][SerializeField]float _timeElapsed,_timeElapsedOverflow,_timeElapsedBetweenInputs,_timeElapsedBetweenCommands,_timeElapsedLocked,_lockedTime,_feverTimer,_mugenStartFeverTime;
    [DisableInEditorMode][SerializeField]Color frameImgColor=Color.white;

    Color _colorTransparent=new Color(1,1,1,0);
    void Start(){
        perfectSound.CreateInstance();
        feverSound.CreateInstance();
        feverEndSound.CreateInstance();
        comboEndSound.CreateInstance();
        specialSound.CreateInstance();
        foreach(FModEventSound s in drumSounds){s.CreateInstance();}

        colorPerfect=new Color(141f/255f,183f/255f,255f/255f);
        colorGood=new Color(252f/255f,239f/255f,141f/255f);
        bgFeverImg.color=_colorTransparent;
        foreach(Image i in bgFeverImg.transform.GetComponentsInChildren<Image>()){i.GetComponent<Image>().color=_colorTransparent;}
        bgMugenImg.color=_colorTransparent;
        comboBarParent.gameObject.SetActive(false);
        comboBarGekka.gameObject.SetActive(false);
        comboBarFillFull.gameObject.SetActive(false);
        comboBarFillFull.color=_colorTransparent;
        comboBarMugen.gameObject.SetActive(false);
        
        LockInput(false);
        ClearActionHistory();
        SetBGLoop(bgLoopCurrentId);
        StartUpdatingRhythm();
    }
    void Update(){CheckInput();}
    
    Vector2 _comboBarPosTarget;float _comboBarGekkaFillTarget;
    float _feverPowerTarget;
    void FixedUpdate(){
        bgLoop.eventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE _state);
        if(_state==FMOD.Studio.PLAYBACK_STATE.PLAYING){
            CheckRhythm();
            float _normalizedBeatTime=_timeElapsed/secBetweenBeats;//Debug.Log(_timeElapsed+" | "+_normalizedTime);
            frameImg.color=Color.Lerp(new Color(frameImgColor.r,frameImgColor.g,frameImgColor.b,1), new Color(frameImgColor.r,frameImgColor.g,frameImgColor.b,0), _normalizedBeatTime);
            if(!inputLocked){frameImg.sprite=defaultSpr;}else{frameImg.sprite=lockedSpr;frameImgColor=Color.white;}

            float _normalizedfeverTimer=_feverTimer/(secBetweenBeats*3);
            if(comboStatus==-1){
                bgFeverImg.color=Color.Lerp(new Color(1,1,1,bgFeverImg.color.a), Color.white, _normalizedfeverTimer);
                bgFeverImg.transform.GetComponentInChildren<Image>().color=Color.Lerp(new Color(1,1,1,bgFeverImg.color.a), Color.white, _normalizedfeverTimer);
                //foreach(Transform t in bgFeverImg.transform){if(t.GetComponent<Image>()!=null){t.GetComponent<Image>().color=Color.Lerp(new Color(1,1,1,bgFeverImg.color.a), Color.white, _normalizedfeverTimer);}}
            }
            else{
                bgFeverImg.color=Color.Lerp(new Color(1,1,1,bgFeverImg.color.a), _colorTransparent, _normalizedfeverTimer);
                bgFeverImg.transform.GetComponentInChildren<Image>().color=Color.Lerp(new Color(1,1,1,bgFeverImg.color.a), _colorTransparent, _normalizedfeverTimer);
                //foreach(Transform t in bgFeverImg.transform){if(t.GetComponent<Image>()!=null){t.GetComponent<Image>().color=Color.Lerp(new Color(1,1,1,bgFeverImg.color.a), _colorTransparent, _normalizedfeverTimer);}}
            }
            float _normalizedmugenTimer=(_feverTimer-_mugenStartFeverTime)/(secBetweenBeats*3);
            if(comboStatus==-2){
                bgMugenImg.color=Color.Lerp(new Color(1,1,1,bgMugenImg.color.a), Color.white, _normalizedfeverTimer);
            }else{
                bgMugenImg.color=Color.Lerp(new Color(1,1,1,bgMugenImg.color.a), _colorTransparent, _normalizedfeverTimer);
            }
        }else{SetBGLoop(bgLoopCurrentId);}

        //Combo bar
        if(comboStatus>0||comboStatus==-1||comboStatus==-2){
            comboBarParent.gameObject.SetActive(true);
            if(_comboBarPosTarget!=comboBarPosShown)_comboBarPosTarget=comboBarPosShown;
            if(comboStatus>0){comboBarText.text="Combo: "+comboStatus.ToString();comboBarFill.fillAmount=0;_comboBarGekkaFillTarget=0;}
            else if(comboStatus==-1){
                comboBarText.text="Gekka";
                comboBarGekka.gameObject.SetActive(true);
                comboBarMugen.gameObject.SetActive(false);
                _comboBarGekkaFillTarget=1;
                var _stepFill=Time.fixedDeltaTime;
                float _feverPowerNormalized=(float)(feverPower-0)/(float)(feverPowerMax-0);
                float _x=Mathf.Lerp(0.3f, 0.94f, _feverPowerNormalized);//Between 0.3 and 0.94 to accomodate for the empty space in sprite
                comboBarFill.fillAmount=Mathf.Clamp(Mathf.MoveTowards(comboBarFill.fillAmount,_x,_stepFill),0f,1f);
                
                var _stepAlpha=Time.fixedDeltaTime*5;
                if(feverPower>=feverPowerMax){comboBarFillFull.gameObject.SetActive(true);comboBarFillFull.color=Color.Lerp(comboBarFillFull.color,Color.white,_stepAlpha);}
                else{comboBarFillFull.gameObject.SetActive(false);comboBarFillFull.color=_colorTransparent;}
                //Debug.Log(comboBarFill.fillAmount+" | "+_feverPowerNormalized);
            }else if(comboStatus==-2){
                comboBarText.text="Mūgen";
                comboBarMugen.gameObject.SetActive(true);
            }
        }else{if(_comboBarPosTarget!=comboBarPosHidden)_comboBarPosTarget=comboBarPosHidden;}
        var _stepFillGekka=Time.fixedDeltaTime;
        comboBarGekka.fillAmount=Mathf.MoveTowards(comboBarGekka.fillAmount,_comboBarGekkaFillTarget,_stepFillGekka);
        var _stepPos=Time.fixedDeltaTime*500f;
        comboBarParent.transform.localPosition=Vector2.MoveTowards(comboBarParent.transform.localPosition,_comboBarPosTarget,_stepPos);

        //Mugen
        /*
        if(comboStatus==-2){
            if(feverPower>0){
                //var _step=Time.fixedDeltaTime*0.2f;
                float _normalizedTime=_timeElapsed/secBetweenBeats;
                feverPower=Mathf.Lerp(100f,0f,_normalizedTime);
                float _feverPowerNormalized=(float)(feverPower-0)/(float)(feverPowerMax-0);
                float _x=Mathf.Lerp(0.94f, 0.3f, _feverPowerNormalized);
                float _xMovet=Mathf.Clamp(Mathf.MoveTowards(comboBarFill.fillAmount,_x,_normalizedTime),0f,feverPowerMax);
                comboBarFill.fillAmount=_xMovet;
                comboBarFillFull.fillAmount=_xMovet;
                /*float _x=Mathf.Lerp(0.94f, 0.3f, _feverPowerNormalized);
                float _xMovet=Mathf.Clamp(Mathf.MoveTowards(comboBarFill.fillAmount,_x,_normalizedTime),0f,feverPowerMax);
                comboBarFill.fillAmount=_xMovet;
                comboBarFillFull.fillAmount=_xMovet;
                feverPower=Mathf.RoundToInt(_xMovet*100);
                Debug.Log(comboBarFill.fillAmount+" | "+_feverPowerNormalized+" | "+feverPower);
            }//else{feverPower=0;ResetCombo();}
        }
        */
        //Mugen
        if(comboStatus==-2){
            if(feverPower>0){
                float _step=Time.fixedDeltaTime*0.2f;
                float _normalizedTime=_timeElapsed/secBetweenBeats;
                //if(_timeElapsed>0.4f)Debug.Log(_timeElapsed);
                //if(_timeElapsed>=secBetweenBeats-0.03f){feverPower-=2f;}
                if(_timeElapsed>=secBetweenBeats-0.01f){_feverPowerTarget=Mathf.Clamp(feverPower-2,0f,100f);}
                feverPower=Mathf.MoveTowards(feverPower,_feverPowerTarget,_normalizedTime);
                //feverPower=Mathf.Lerp(100f,0f,_normalizedTime);
                //feverPower-=2f;//=30s because 2 beats per second
                float _feverPowerNormalized=(float)(feverPower-0)/(float)(feverPowerMax-0);
                float _x=Mathf.Lerp(0.94f, 0.3f, _feverPowerNormalized);
                //float _xMovet=Mathf.Clamp(Mathf.MoveTowards(comboBarFill.fillAmount,_x,_step),0f,feverPowerMax);
                comboBarFill.fillAmount=_x;
                comboBarFillFull.fillAmount=_x;
                /*float _x=Mathf.Lerp(0.94f, 0.3f, _feverPowerNormalized);
                float _xMovet=Mathf.Clamp(Mathf.MoveTowards(comboBarFill.fillAmount,_x,_normalizedTime),0f,feverPowerMax);
                comboBarFill.fillAmount=_xMovet;
                comboBarFillFull.fillAmount=_xMovet;
                feverPower=Mathf.RoundToInt(_xMovet*100);*/
                Debug.Log(comboBarFill.fillAmount+" | "+_feverPowerNormalized+" | "+feverPower);
            }//else{feverPower=0;ResetCombo();}
        }
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

        if(_timeElapsedOverflow<0){_timeElapsedOverflow=0;}
        if(_timeElapsedOverflow<secBetweenBeats-0.05f){_timeElapsedOverflow=_timeElapsed;}else{_timeElapsedOverflow+=Time.fixedDeltaTime;}
        _timeElapsedOverflow=(float)System.Math.Round(_timeElapsedOverflow,3);
        if(_debug)Debug.Log(_timeElapsed+" | "+_timeElapsedOverflow);
        if(_timeElapsedOverflow>secBetweenBeats+(secBetweenBeats/2f)){_timeElapsedOverflow=_timeElapsed;}
        if(comboStatus==-1||comboStatus==-2){_feverTimer+=Time.fixedDeltaTime;}else{_feverTimer=0;}

        ///HitWindow Checking
        hitWindowState=HitWindowState.locked;//float epsilon=0.001f;
        //if(_timeElapsedOverflow>=secBetweenBeats-0.12f&&_timeElapsedOverflow<=secBetweenBeats+0.4f){hitWindowState=HitWindowState.good;}//0.083f | 0.25f
        //if(_timeElapsedOverflow>=secBetweenBeats-0.0625f&&_timeElapsedOverflow<=secBetweenBeats+0.25f){hitWindowState=HitWindowState.perfect;}//0.0625f | 0.125f
        if(_timeElapsedOverflow>=secBetweenBeats-(secBetweenBeats/6)&&_timeElapsedOverflow<=secBetweenBeats+(secBetweenBeats/2)){hitWindowState=HitWindowState.good;}
        if(_timeElapsedOverflow>=secBetweenBeats-(secBetweenBeats/8)&&_timeElapsedOverflow<=secBetweenBeats+(secBetweenBeats/4)){hitWindowState=HitWindowState.perfect;}
        //if(Mathf.Abs(secBetweenBeats-_timeElapsedOverflow)<=(secBetweenBeats/4)+epsilon){hitWindowState=HitWindowState.good;}
        //if(Mathf.Abs(secBetweenBeats-_timeElapsedOverflow)<=(secBetweenBeats/6)+epsilon){hitWindowState=HitWindowState.perfect;}

        if(inputLocked){//Unlocking
            if(_timeElapsedLocked>=_lockedTime-0.04f){
                LockInput(false);ClearActionHistory();_timeElapsedLocked=0;if(_timeElapsedBetweenCommands==0)_commandExecuting=false;
            }else{_timeElapsedLocked+=Time.fixedDeltaTime;}
        }else{_timeElapsedLocked=0;}

        if(!_isActionHistoryEmpty()&&!_isActionHistoryFull()){///Time after a single drum before reset
            if(_timeElapsedBetweenInputs>=(secBetweenBeats+(secBetweenBeats/2))+0.04f){
                LockInput(true,(secBetweenBeats+(secBetweenBeats/2)));ClearActionHistory();_timeElapsedBetweenInputs=0;
                ResetCombo();
            }else{_timeElapsedBetweenInputs+=Time.fixedDeltaTime;}
        }else if(_isActionHistoryEmpty()){_timeElapsedBetweenInputs=0;}

        if(_commandExecuting&&(comboStatus>0||comboStatus==-1||comboStatus==-2)){///After a successful command, count before resetting combo etc
            if(_timeElapsedBetweenCommands>=(6*secBetweenBeats+(secBetweenBeats/2)+0.04f)){
                LockInput(true,(secBetweenBeats+(secBetweenBeats/2)));ClearActionHistory();_timeElapsedBetweenCommands=0;
                ResetCombo();
            }else{_timeElapsedBetweenCommands+=Time.fixedDeltaTime;}
        }else{_timeElapsedBetweenCommands=0;}
    }
    void StartUpdatingRhythm(){
        secBetweenBeats=(60/bpm);
        beatsPerSec=(bpm/60);
        frameImgColor=Color.white;
        _timeElapsed=0;
    }

    void HitDrum(int i){
        foreach(FModEventSound ds in drumSounds){ds.Stop();}
        drumSounds[i].Start();
        drumSounds[i].eventInstance.setParameterByName("GoodHitWindow", hitWindowState==HitWindowState.perfect ? 0 : 1);
        if(hitWindowState==HitWindowState.good){commandNotPerfect=true;frameImgColor=Color.yellow;}else{frameImgColor=Color.cyan;}

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
        Vector2 _random=new Vector2(UnityEngine.Random.Range(-10f,10f),UnityEngine.Random.Range(-10f,10f));
        _drumImg.transform.localPosition=(Vector2)drumImgsParent.transform.localPosition+new Vector2(_x,_y)+_random;
    }
    void SetActionHistory(int id){
        for(var i=0;i<actionHistory.Length;i++){if(actionHistory[i]==-1){actionHistory[i]=id;break;}}
        if(_timeElapsedBetweenInputs<secBetweenBeats/3&&_timeElapsedBetweenInputs!=0){mashingCount++;}
        if(mashingCount>2){ClearActionHistory();LockInput(true,secBetweenBeats+0.125f);mashingCount=0;}
        //if(_timeElapsedBetweenInputs>secBetweenBeats/3||_timeElapsedBetweenInputs==0){for(var i=0;i<actionHistory.Length;i++){if(actionHistory[i]==-1){actionHistory[i]=id;break;}}}else{ClearActionHistory();}
        //if(AreAllValuesEqual(actionHistory)){LockInput(true,(secBetweenBeats+(secBetweenBeats/2)));}
        _timeElapsedBetweenInputs=0;_timeElapsedBetweenCommands=0;_commandExecuting=false;
        
        //Commands
        bool _charging=false;
        if(actionHistory[0]==0&&actionHistory[1]==0&&actionHistory[2]==0&&actionHistory[3]==1){Debug.Log("Forward");_commandExecuting=true;}//Forward
        else if(actionHistory[0]==1&&actionHistory[1]==1&&actionHistory[2]==0&&actionHistory[3]==1){Debug.Log("Attack");_commandExecuting=true;}//Attack
        else if(actionHistory[0]==1&&actionHistory[1]==0&&actionHistory[2]==1&&actionHistory[3]==0){Debug.Log("Retreat");_commandExecuting=true;}//Retreat
        else if(actionHistory[0]==1&&actionHistory[1]==1&&actionHistory[2]==2&&actionHistory[3]==2){Debug.Log("Charge");_commandExecuting=true;_charging=true;}//Charge
        else if(actionHistory[0]==2&&actionHistory[1]==2&&actionHistory[2]==0&&actionHistory[3]==1){Debug.Log("Defense");_commandExecuting=true;}//Defense
        else if(actionHistory[0]==3&&actionHistory[1]==3&&actionHistory[2]==2&&actionHistory[3]==2){Debug.Log("Jump");_commandExecuting=true;}//Jump
        else if(actionHistory[0]==3&&actionHistory[1]==3&&actionHistory[2]==1&&actionHistory[3]==0){CommandSpecial();_commandExecuting=true;}//Special
        else{if(_isActionHistoryFull()){LockInput(true,(2*secBetweenBeats)-0.125f);_commandExecuting=false;ResetCombo();}}
        
        if(_commandExecuting){
            LockInput(true,((4*secBetweenBeats)));
            if(!commandNotPerfect){perfectSound.eventInstance.start();}
            mashingCount=0;
            if(comboStatus>=0){
                comboStatus++;
                if(commandNotPerfect){commandNotPerfectCount++;commandNotPerfect=false;}
                if(comboStatus==3&&commandNotPerfectCount==0){ActivateFever();}
                if(comboStatus==4&&commandNotPerfectCount==1){ActivateFever();}
                if(comboStatus==5&&commandNotPerfectCount>=2){ActivateFever();}
                if(comboStatus>5){ActivateFever();}
            }else if(comboStatus==-1){
                if(_charging){if(commandNotPerfect){feverPower+=9;}else{feverPower+=12;}}
                else{if(commandNotPerfect){feverPower+=8;}else{feverPower+=10;}}
                if(feverPower>feverPowerMax){feverPower=feverPowerMax;}
            }
        }
    }
    void ActivateFever(){
        comboStatus=-1;
        feverSound.eventInstance.start();
    }
    void ResetCombo(){
        commandNotPerfect=false;
        commandNotPerfectCount=0;
        if(!_lockinCombo){
            if(comboStatus==-1||comboStatus==-2){
                feverEndSound.eventInstance.start();
            }else if(comboStatus>0){
                comboEndSound.eventInstance.start();
            }
            comboStatus=0;
            feverPower=0;
        }
        mashingCount=0;
    }
    void CommandSpecial(){
        Debug.Log("Special");
        if(feverPower>=feverPowerMax){
            comboStatus=-2;
            _lockinCombo=true;
            specialSound.Start();
            comboBarMugen.gameObject.SetActive(true);
            _mugenStartFeverTime=_feverTimer;
        }
    }

    void ClearActionHistory(){for(var i=actionHistory.Length-1;i>=0;i--){actionHistory[i]=-1;}/*frameImgColor=Color.red;*/}
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
        if(_isLocked){_lockedTime=timer;}else{_lockedTime=0;}
        bgLoop.eventInstance.setParameterByName("LockedFX", _isLocked ? 1 : 0);
    }
    public void SetBGLoop(int id=0){
        //if(bgLoop.eventInstance!=null){
            bgLoop.eventInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE _state);
            if(_state==FMOD.Studio.PLAYBACK_STATE.PLAYING)bgLoop.Stop();
        //}
        bgLoop=bgLoops[bgLoopCurrentId];
        bgLoop.CreateInstance();
        bgLoop.Start();

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
    public void CreateInstance(){eventInstance=RuntimeManager.CreateInstance(eventName);}
    public void Start(){eventInstance.start();}
    public void Stop(){eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);}
}
[System.Serializable]
public class FModEventSoundLoop {
	public string eventName="event:/BGLoop120";
	public int bpm=120;
	[DisableInEditorMode]public FMOD.Studio.EventInstance eventInstance;
    public void CreateInstance(){eventInstance=RuntimeManager.CreateInstance(eventName);}
    public void Start(){eventInstance.start();}
    public void Stop(){eventInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);}
}
public enum HitWindowState{locked,good,perfect}