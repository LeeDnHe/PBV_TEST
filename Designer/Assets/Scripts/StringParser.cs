using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringParser : MonoBehaviour
{
    public string ExtractBetween(string inputString, string startString, string endString)
    {
        int startIndex = inputString.IndexOf(startString, StringComparison.Ordinal) + startString.Length;
        int endIndex = inputString.IndexOf(endString, startIndex);

        if (startIndex >= startString.Length && endIndex > startIndex)
        {
            return inputString.Substring(startIndex, endIndex - startIndex);
        }

        return string.Empty;
    }
}
