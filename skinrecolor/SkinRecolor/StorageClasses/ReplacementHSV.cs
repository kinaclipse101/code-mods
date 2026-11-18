using System;
using System.Text;
using UnityEngine;

namespace SkinRecolor;

public class ReplacementHSV
{
    public float hue;
    public float saturation;
    public float value;

    public bool multiplySat;

    public bool SameAsOriginal()
    {
        if (hue == 0 && saturation == 0 && value == 0 && !multiplySat)
        {
            return true;
        }
        if (hue == 0 && Mathf.Approximately(saturation, 1) && value == 0 && multiplySat)
        {
            return true;
        }
        
        return false;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("H:");
        sb.Append(hue);
        sb.Append(" S:");
        sb.Append(saturation);
        sb.Append(" V:");
        sb.Append(value);
        sb.Append(" multiplySat:");
        sb.Append(multiplySat);
        return sb.ToString();
    }

    public void SetFromString(string hsvString)
    {
        try
        {
            string[] values = hsvString.Split(' ');
            hue = float.Parse(values[0].Substring(2, values[0].Length - 2));
            saturation = float.Parse(values[1].Substring(2, values[1].Length - 2));
            value = float.Parse(values[2].Substring(2, values[2].Length - 2));
            multiplySat = bool.Parse(values[3].Substring(12, values[3].Length - 12));
        }
        catch (Exception e)
        {
            Log.Error("error while saving hsv");
            Log.Error(e);
        }
    }
}