using UnityEngine;

[System.Serializable]
public class StoryStep
{
    public Sprite image;      // A imagem do painel
    [TextArea(3, 10)]
    public string text;       // O texto que aparece embaixo
}

[CreateAssetMenu(fileName = "NewStory", menuName = "Story/Cutscene")]
public class StoryData : ScriptableObject
{
    public StoryStep[] steps; // Lista de cenas da história
}