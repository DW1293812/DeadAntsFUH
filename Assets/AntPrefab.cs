using UnityEngine;

public class AntPrefab : MonoBehaviour
{
    public GameObject grafic, schmatic;
    public int index;
    public void ChangeAppearence (bool schematic)
    {

            grafic.SetActive (!schematic);
            schmatic.SetActive(schematic);

    }

    public void SetID (int id)
    {
        index = id;
    }

}
