using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    //[SerializeField] GameObject optionsPanel;

    public void OnResumeClicked()    => GameManager.Instance?.Resume();
    public void OnRestartClicked()   => GameManager.Instance?.RestartLevel();
    public void OnMainMenuClicked()  => GameManager.Instance?.GoToMainMenu();

    //CONTEUDO CORTADO -- EASTER EGG
    //public void OnOptionsClicked()
    //{
    //    if (optionsPanel != null)
    //        optionsPanel.SetActive(!optionsPanel.activeSelf);
    //}
}
