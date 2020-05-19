using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class MinecraftGradientGenerator : MonoBehaviour {

    public Gradient gradient;
    public string text;
    public string outputText;

    void Start () {
        StringBuilder sb = new StringBuilder();
        sb.Append("[");
        for(int i = 0; i < text.Length; i++) {
            float time = (float)i / (text.Length);

            sb.Append("{\"text\":\"");
            sb.Append(text[i]);
            sb.Append("\",\"color\":\"#");
            sb.Append(ColorUtility.ToHtmlStringRGB(gradient.Evaluate(time)));
            sb.Append("\"}");

            if(i != text.Length - 1)
                sb.Append(",");
        }
        sb.Append("]");

        outputText = sb.ToString();
    }
}
