using UnityEngine;
using UnityEngine.UI;

public class ResultAct : MonoBehaviour
{
    public BlueRed blueRed;
    public Image leftHand;
    public Image righttHand;
    public Sprite[] lHandSources = new Sprite[3];
    public Sprite[] rHandSources = new Sprite[3];

    public void OnInitRSPSymbols()
    {
        leftHand.sprite = lHandSources[0];
        righttHand.sprite = rHandSources[0];
    }

    public void OnShowRSPSymbols()
    {
        leftHand.sprite = lHandSources[1]; //임시 연출. 서버 연동시 이곳을 수정
        righttHand.sprite = rHandSources[2];
    }

    public void OnFinished()
    {
        gameObject.SetActive(false);
        blueRed.ChangeState(blueRed.stateReward);
    }


}
