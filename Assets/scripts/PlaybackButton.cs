using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlaybackButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _buttonText;
    private AudioClip _clip;
    private AudioSource _source;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Play);
    }
    public void Setup(float timeStamp, AudioClip clip, AudioSource source)
    {
        _buttonText.text = $"Lag: {timeStamp - clip.length} seconds";
        _clip = clip;
        _source = source;
    }

    private void Play()
    {
        if (_clip != null && _source != null)
            _source.PlayOneShot(_clip);
            
    }
}
