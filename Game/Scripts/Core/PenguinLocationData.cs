using UnityEngine;

namespace PenguinPinball.Core
{
    public enum PenguinLocation
    {
        Bench,
        Inventory
    }

    public class PenguinLocationData : MonoBehaviour
    {
        [field: SerializeField] public PenguinLocation Location { get; set; } = PenguinLocation.Bench;
    }
}
