using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "KnifeSkill", menuName = "Skills/Knife")]
public class KnifeSkillData : SkillData
{
    [Serializable]
    public struct KnifeLevel
    {
        public float Damage;
        public int KnifeCount;
        public float Speed;
        public float Cooldown;
        public float HitRadius;
        public BaseStatModifier[] StatModifiers;
        [TextArea] public string LevelDescription;
    }

    [SerializeField] private GameObject _knifePrefab;
    [SerializeField] private KnifeLevel[] _levels;

    private float _timer;
    private static readonly Queue<GameObject> _knifePool = new Queue<GameObject>();

    public override int MaxLevel => _levels.Length;
    private KnifeLevel CurrentLevelData => _levels[Mathf.Clamp(CurrentLevel - 1, 0, MaxLevel - 1)];

    public override void OnEquip(GameObject owner)
    {
        base.OnEquip(owner);
        _timer = 0f;
        _knifePool.Clear();
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
    }

    public override void OnUpdate(GameObject owner)
    {
        _timer += Time.deltaTime;

        float actualCooldown = GetModifiedCooldown(CurrentLevelData.Cooldown);
        if (_timer >= actualCooldown)
        {
            _timer = 0f;
            TriggerKnifeStorm(owner);
        }
    }

    public override void OnLevelUp(GameObject owner)
    {
        base.OnLevelUp(owner);
        ApplyStatModifiers(CurrentLevelData.StatModifiers);
    }

    public override string GetLevelUpDescription()
    {
        if (IsMaxLevel) return "MAX LEVEL";
        return _levels[CurrentLevel].LevelDescription;
    }

    private void TriggerKnifeStorm(GameObject owner)
    {
        PlayerSkillManager.Instance.StartCoroutine(SpawnKnives(owner));
    }

    private IEnumerator SpawnKnives(GameObject owner)
    {
        if (_knifePrefab == null) yield break;

        Vector3 direction = Vector3.right;
        PlayerMovement movement = owner.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            if (movement.input.sqrMagnitude > 0.01f)
            {
                direction = new Vector3(movement.input.x, movement.input.y, 0f).normalized;
            }
            else
            {
                SpriteRenderer spriteRenderer = owner.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    direction = spriteRenderer.flipX ? Vector3.left : Vector3.right;
                }
            }
        }

        int count = CurrentLevelData.KnifeCount;
        for (int i = 0; i < count; i++)
        {
            GameObject knife = GetPooledKnife(owner.transform.position);
            KnifeProjectile proj = knife.GetComponent<KnifeProjectile>();
            if (proj == null)
            {
                proj = knife.AddComponent<KnifeProjectile>();
            }
            proj.Init(direction, CurrentLevelData.Speed, CurrentLevelData.Damage, CurrentLevelData.HitRadius);

            yield return new WaitForSeconds(0.1f);
        }
    }

    private GameObject GetPooledKnife(Vector3 pos)
    {
        if (_knifePool.Count > 0)
        {
            GameObject knife = _knifePool.Dequeue();
            knife.transform.position = pos;
            knife.SetActive(true);
            return knife;
        }
        return Instantiate(_knifePrefab, pos, Quaternion.identity);
    }

    public static void ReturnKnifeToPool(GameObject knife)
    {
        if (knife != null && knife.activeSelf)
        {
            knife.SetActive(false);
            _knifePool.Enqueue(knife);
        }
    }
}
