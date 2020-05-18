using UnityEngine;
using UnityEngine.UI;

public class TextScript : MonoBehaviour
{
    public Text throwInfo;
    public string info;

    void Update()
    {
        throwInfo.text = info;
    }
}
