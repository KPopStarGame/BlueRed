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
        BRStateResult stateResult = blueRed.stateResult as BRStateResult;
        if (stateResult == null)
        {
            Debug.LogError("#004_stateResult is NULL");
            return;
        }
        BlueRed.SideType resultType = stateResult.ResultType;
        int nIndexLeft = Random.Range(0, lHandSources.Length);
        int nIndexRight = SetRightHandIndex(nIndexLeft, resultType);

        leftHand.sprite = lHandSources[nIndexLeft]; //임시 연출. 서버 연동시 이곳을 수정
        righttHand.sprite = rHandSources[nIndexRight];
    }

    private int SetRightHandIndex(int nIndexLeft, BlueRed.SideType resultType)
    {
        int nReturnIndex = 0;
        switch(resultType)
        {
            case BlueRed.SideType.Blue:
                nReturnIndex = nIndexLeft + 1;
                if(nReturnIndex >= 3)
                {
                    nReturnIndex = 0;
                }
                break;
            case BlueRed.SideType.Green:
                nReturnIndex = nIndexLeft;
                break;
            case BlueRed.SideType.Red:
                nReturnIndex = nIndexLeft - 1;
                if (nReturnIndex < 0)
                {
                    nReturnIndex = 2;
                }
                break;
        }
        return nReturnIndex;
    }

    public void OnFinished()
    {
        gameObject.SetActive(false);
        blueRed.ChangeState(blueRed.stateReward);
    }


}
