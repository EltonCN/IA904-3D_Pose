using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadingPanel : MonoBehaviour
{
    [SerializeField] private Image _Panel;
    [SerializeField] private TMP_Text _ProgressPercent;
    [SerializeField] private TMP_Text _ProgressMessage;

    private double _ProgressSize = 0d;

    void Start()
    {
        _Panel.gameObject.SetActive(false);
    }

    void Update()
    {
        
    }

    public void StartLoad(double size)
    {
        _ProgressSize = size;
        _ProgressPercent.text = "0%";
        _ProgressMessage.text = "";

        _Panel.gameObject.SetActive(true);
        Debug.Log($"Starting load, progress size: {size}");
    }

    public void FinishLoad()
    {
        _Panel.gameObject.SetActive(false);
    }

    public void UpdateProgress(double progress, string message = null)
    {
        _ProgressPercent.text = $"{(progress * 100 / _ProgressSize):F2}%";

        if (!string.IsNullOrEmpty(message))
            _ProgressMessage.text = message;

        
    }
}
