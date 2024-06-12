using IA904_3DPose;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuPanel : MonoBehaviour
{
    [SerializeField] private TMP_InputField _pathIntutField;
    [SerializeField] private TMP_Dropdown _scenarioDropwn;
    [SerializeField] private Image _warningPanel;

    void Start()
    {
        _pathIntutField.text = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                .Replace("Roaming", "LocalLow") + "\\DefaultCompany\\IA904-3D_Pose";
    }

    public void GenerateDataset()
    {
        if (ValidPath())
            DataManager.Instance.GenerateDataset(_pathIntutField.text, _scenarioDropwn.options[_scenarioDropwn.value].text);
        else
            _warningPanel.gameObject.SetActive(true);
    }

    public void BuildDataframe()
    {
        if (ValidPath())
            DataManager.Instance.BuildDataframe(_pathIntutField.text);
        else
            _warningPanel.gameObject.SetActive(true);
    }

    private bool ValidPath()
    {
        var isValid = !string.IsNullOrEmpty(_pathIntutField.text) && Directory.Exists(_pathIntutField.text);

        if (!isValid)
        {
            _warningPanel.gameObject.SetActive(true);
        }

        return isValid;
    }
}
