
using UnityEngine;
public abstract class BRState
{
    public abstract void Enter(BlueRed br);
    public abstract void Update(BlueRed br);
    public abstract void Leave(BlueRed br);
}


public class BRStateInit : BRState
{
    public override void Enter(BlueRed br)
    {
        br.InitGame();
        br.InitControls();
    }

    public override void Update(BlueRed br)
    {
        br.ChangeState(br.stateReady);
    }

    public override void Leave(BlueRed br)
    {

    }
}

public class BRStateReady : BRState
{
    public override void Enter(BlueRed br)
    {
        br.ResetGame();
        br.PopupNoticeStartBet();
    }

    public override void Update(BlueRed br)
    {
        br.ChangeState(br.stateBetting);
    }

    public override void Leave(BlueRed br)
    {

    }
}


public class BRStateBetting : BRState
{
    float _elapsedTime;
    public override void Enter(BlueRed br)
    {
        _elapsedTime = 0;
    }

    public override void Update(BlueRed br)
    {
        _elapsedTime += Time.deltaTime;

        //다른 유저들의 베팅 연출 (임시코드. 서버 연동시 변경)
        if (_elapsedTime > 0.5f) //5초에 하나씩 랜덤 위치, 랜덤 코인
        {
            br.BetCoin((BlueRed.SideType)UnityEngine.Random.Range(0, 3), false, UnityEngine.Random.Range(0, 3));
            _elapsedTime = 0;
        }

        if (br.UpdateTimer())
            br.ChangeState(br.stateResult);
    }

    public override void Leave(BlueRed br)
    {

    }
}

public class BRStateResult : BRState
{
    public override void Enter(BlueRed br)
    {
        br.PlayAudio(0, br.acResult);
        br.resultAct.gameObject.SetActive(true);
        br.SetTrendResult(BlueRed.SideType.Blue); //트렌드에 결과 값을 추가 (임시코드)
    }

    public override void Update(BlueRed br)
    {

    }

    public override void Leave(BlueRed br)
    {

    }
}

public class BRStateReward : BRState
{
    private const float _coinDropInterval = 0.1f;
    private int _coinCount; //떨군 동전갯수
    private int _coinDrop; //떨굴 동전갯수
    float _elapsedTime;
    public override void Enter(BlueRed br)
    {
        _elapsedTime = 0;
        br.SetWinAnim(BlueRed.SideType.Blue, true); //임시 연출
        br.RestoreCoins();

        br.PlayAudio(0, br.acWin);


        //_coinDrop = Mathf.Min(50, rewardCash);
        _coinDrop = 50;
        _coinCount = 0;
        BlueRed.coinPool.Clear();
        for (int i = 0; i < br.rewardCoins.Length; i++)
        {
            BlueRed.coinPool.Enqueue(br.rewardCoins[i]);
            br.rewardCoins[i].gameObject.SetActive(false);
        }

    }

    public override void Update(BlueRed br)
    {
        _elapsedTime += Time.deltaTime;
        if(_elapsedTime > 5)
        {
            //br.ChangeState(br.stateReady);
        }


        //코인 떨구기
        if (_coinCount < _coinDrop)
        {
            _elapsedTime += UnityEngine.Time.deltaTime;
            while (_elapsedTime >= _coinDropInterval)
            {
                _elapsedTime -= _coinDropInterval;
                if (BlueRed.coinPool.Count > 0)
                {
                    RewardCoin rewardCoin = BlueRed.coinPool.Dequeue();
                    rewardCoin.Set(br);
                    //rsp.PlayAudio(0, rsp.acCoinDrop);

                    ++_coinCount;
                }

            }
        }
        else if (BlueRed.coinPool.Count >= br.rewardCoins.Length)
        {
            br.ChangeState(br.stateReady);
        }


    }

    public override void Leave(BlueRed br)
    {
        for(int i = 0; i < 3; i++)
        {
            br.SetWinAnim((BlueRed.SideType)i, false);
        }
        br.betCoinPool.RestoreAll();
    }
}