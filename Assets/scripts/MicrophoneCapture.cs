using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class MicrophoneCapture : MonoBehaviour
{
    // Boolean flags shows if the microphone is connected 
    private bool micConnected;

    private int selectedSampleRate;
    private int micsAvailable;
    private float _startRecTimestamp;
    private AudioClip myAudioClip;
    private string _fileName;
    private Coroutine _playingRecordingCoroutine;
    private Action OnPlayingRecordingFinished;    
    

    //A handle to the attached AudioSource  
    [SerializeField] private AudioSource _inputAudioSource, _outputAudioSoure;
    [SerializeField] List<AudioClip> _recordedClips;
    [SerializeField] PlaybackButton _playBtnPrefab;
    [SerializeField] Transform _playBtnsContainer;
    [SerializeField] TextMeshProUGUI _micStatusText;
    [SerializeField] Button _recBtn, _stopRecBtn;

    public bool IsRecording { get => Microphone.IsRecording(null); }
    public float RecordingDuration { get => _inputAudioSource.clip.length; }
    public string FileName { get => _fileName + ".wav"; }
    public float CurrentPlaybackTime { get => _inputAudioSource.time; }
    public bool IsPlaying { get => _inputAudioSource.isPlaying; }

    public static Action OnEndRecording;


    protected new void Awake()
    {
        micsAvailable = Microphone.devices.Length;
        _recordedClips = new List<AudioClip>();
        _recBtn.onClick.AddListener(StartRecording);
        _stopRecBtn.onClick.AddListener(() => EndRecordingAndSave());

    }

    private void Start()
    {
        if (micsAvailable <= 0)
        {
            micsAvailable = Microphone.devices.Length;
            Init();
        }
        else
        {
            Init();
            StartCoroutine(GetMicStatus());
            StartCoroutine(SimulateRecording());
        }
    }

    //this is needed for MacOs in order to trigger Mic access
    IEnumerator SimulateRecording()
    {
        //bool accessGranted = Application.HasUserAuthorization(UserAuthorization.Microphone);
        //if (!accessGranted)
        //{
            yield return new WaitForSeconds(1);
            StartRecording();
            yield return new WaitForSeconds(1);
            AbortRecording();
        //}
    }

    private void Init()
    {
        //Check if there is at least one microphone connected  
        if (micsAvailable <= 0)
        {

            //Throw a warning message at the console if there isn't  
            Debug.LogWarning("Microphone not connected!");
            
        }
        else //At least one microphone is present  
        {

            //Get the default microphone recording capabilities  
            Microphone.GetDeviceCaps(null, out int minFreq, out int maxFreq);

            //According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...  
            if (minFreq == 0 && maxFreq == 0)
            {
                //...meaning 44100 Hz can be used as the recording sampling rate  
                selectedSampleRate = 44100;
            }
            else
            {
                selectedSampleRate = Math.Max(minFreq, Math.Min(44100, maxFreq));
            }
            if (selectedSampleRate >= 16000 && selectedSampleRate <= 44100)
            {
                Debug.Log($"Using mic at sample rate {selectedSampleRate} (supported {minFreq}-{maxFreq})");
                micConnected = true;
            }
            else
            {
                Debug.LogWarning($"Not finding suitable mic as sample rate range {minFreq}-{maxFreq} not within 16000-44100");
            }
            
        }
    }

    IEnumerator GetMicStatus()
    {
        while (true)
        {
            bool recording = Microphone.IsRecording(null);
            _micStatusText.text = recording ? "Recording" : "Stopped";
            yield return new WaitForEndOfFrame();
            _recBtn.interactable = !recording;
            _stopRecBtn.interactable = recording;
        }
    }

    public void StartRecording()
    {
        if (!micConnected)
        {
            Init();
        }
        // OnEndRecording = callback;
        if (Microphone.IsRecording(null))
        {
            Microphone.End(null); // force end recording. this should not happen!
            Debug.LogError("Aborting previous recording session");
        }

        try
        {
            _inputAudioSource.clip = Microphone.Start(null, true, 15, selectedSampleRate); //600 sec = 10 min in lenth
            _startRecTimestamp = Time.time;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }       

    }

    public void EndRecordingAndSave(string fileName = "", Action<AudioClip, int> result = null)
    {
        if (Microphone.IsRecording(null))
        {
            _fileName = fileName;
            uint length = 0;
            //SavWav.Save(_fileName, goAudioSource.clip);            
            //newClip.name = fileName;
            _inputAudioSource.clip.name = fileName;
            OnEndRecording?.Invoke();
            //byte[] data = null;
            AudioClip newClip = SavWav.GetWav(_inputAudioSource.clip, out length, out byte[] data);
            
            Microphone.End(null); //Stop the audio recording  
            //goAudioSource.clip = newClip;
            //result?.Invoke(newClip, _startRecTimestamp);
            _recordedClips.Add(newClip);
            MakePlayableButton(newClip);
        }
        else
        {
            Debug.LogError("There is no active recording to finish up"); //should not happen
        }
    }

    private void MakePlayableButton(AudioClip clip)
    {
            float newTime = Time.time - _startRecTimestamp;
        var btn = Instantiate(_playBtnPrefab, _playBtnsContainer);
        btn.Setup(newTime, clip, _outputAudioSoure);
    }

    public void AbortRecording()
    {
        if (Microphone.IsRecording(null))
        {
            Microphone.End(null);
            _inputAudioSource.clip = null;
        }
    }

    

    public void Stop()
    {
        if (IsRecording)
        {
            Microphone.End(null); // force end recording. this should not happen           
        }
        if (_inputAudioSource.clip != null && CurrentPlaybackTime != 0)
        {
            _inputAudioSource.Stop();
            StopCoroutine(_playingRecordingCoroutine);
            _inputAudioSource.time = 0;
            OnPlayingRecordingFinished?.Invoke();
            OnPlayingRecordingFinished = null;
        }
    }      
}
