using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static Action OnPhaseChanged;
    public static Action OnEnemyDeath;
    public static Action OnPlayerDeath;
    public static Action OnPlayerHpChanged;
    public static Action<int> OnLevelUp;
}
