using UnityEngine;

[CreateAssetMenu(fileName = "NewBumperData", menuName = "Pinball/Bumper Data")]
public class BumperData : ScriptableObject
{
    public string bumperName = "Standard Bumper";
    public int cost = 50;
    public GameObject bumperPrefab;
    public Sprite icon;
}