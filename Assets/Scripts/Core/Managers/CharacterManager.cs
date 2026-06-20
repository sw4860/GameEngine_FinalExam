using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public static CharacterManager Instance;

    public CharacterData SelectedCharacter;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 이전 세션에서 선택된 캐릭터 복원
        if (SelectedCharacter == null && GameDataManager.Instance != null && GameDataManager.Instance.CurrentData != null)
        {
            string savedCharacterName = GameDataManager.Instance.CurrentData.SelectedCharacterName;
            if (!string.IsNullOrEmpty(savedCharacterName))
            {
                CharacterData loadedCharacter = FindCharacterInResources(savedCharacterName);
                if (loadedCharacter != null)
                {
                    SelectedCharacter = loadedCharacter;
                }
            }
        }

        // 복원된 캐릭터가 없다면, 최초 실행 시 기본 해금된 캐릭터 중 하나를 자동 선택
        if (SelectedCharacter == null)
        {
            CharacterData[] characters = Resources.LoadAll<CharacterData>("SO/Character");
            foreach (var character in characters)
            {
                if (character.IsDefaultUnlocked)
                {
                    SelectCharacter(character);
                    break;
                }
            }

            // 만약 기본 해금 상태 캐릭터를 찾지 못했다면 첫 번째 캐릭터 선택
            if (SelectedCharacter == null && characters.Length > 0)
            {
                SelectCharacter(characters[0]);
            }
        }
    }

    public void SelectCharacter(CharacterData character)
    {
        SelectedCharacter = character;
        
        // 세이브 데이터에 선택정보 저장
        if (GameDataManager.Instance != null && GameDataManager.Instance.CurrentData != null && character != null)
        {
            GameDataManager.Instance.CurrentData.SelectedCharacterName = character.name;
            GameDataManager.Instance.SaveGame();
        }
    }

    private CharacterData FindCharacterInResources(string characterName)
    {
        CharacterData[] characters = Resources.LoadAll<CharacterData>("SO/Character");
        foreach (var character in characters)
        {
            if (character.name == characterName || character.CharacterName == characterName)
            {
                return character;
            }
        }
        return null;
    }
}
