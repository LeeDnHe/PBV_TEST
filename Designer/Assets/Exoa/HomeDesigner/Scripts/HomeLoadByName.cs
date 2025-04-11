using Exoa.Designer;
using Exoa.Events;
using UnityEngine;

public class HomeLoadByName : MonoBehaviour
{
    public string fileName;
    public bool buildAtStart;

    void Start()
    {
        if (buildAtStart && !string.IsNullOrEmpty(fileName))
        {
            LoadFile(fileName);
        }
    }

    public void LoadFile(string fileName)
    {
        if (AppController.Instance == null)
        {
            AppController.Instance = GetComponent<AppController>();
        }
        if (!string.IsNullOrEmpty(fileName))
        {
            HomeReader dataReader = GetComponent<HomeReader>();
            dataReader.ReplaceAndLoad(fileName, true);

        }
    }

    public void Clear()
    {
        HomeReader dataReader = GetComponent<HomeReader>();
        dataReader.Clear();
    }

}
