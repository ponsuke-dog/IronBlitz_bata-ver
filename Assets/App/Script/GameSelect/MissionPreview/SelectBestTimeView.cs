using TMPro;
using UnityEngine;

public class SelectBestTimeView : MonoBehaviour
{
    [SerializeField] private TMP_Text TextUI;
    private string format;

    public void Initialize(string formatText)
    {
        format = formatText;
    }

    public void UpdateText(params object[] args)
    {
        TextUI.text = string.Format(format, args);
    }

}
