using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum eState
{
    Init = 0,
    InitUserInfo,
    Ready,
    Betting,
    Result,
    Reward,
    Max,
}

public abstract class BRState
{
    public eState m_State;
    protected string szPostAddress;
    protected List<FormData> listFormData = new List<FormData>();
    public abstract void Init();
    public abstract void Enter(BlueRed br);
    public abstract void Update(BlueRed br);
    public abstract void Leave(BlueRed br);
    public abstract void OnCallBack(string jsonData, BlueRed br);
    public FormData CreateFormData(string key, string value)
    {
        FormData userInfoTockenForm = new FormData();
        userInfoTockenForm.szKey = key;
        userInfoTockenForm.szValue = value;
        return userInfoTockenForm;
    }
}


public class BRStateInit : BRState
{
    private bool bChangeState = false;
    private string userInfoAddress = "https://kpoplive.m.codewiz.kr/game/game_user_info";
    protected List<FormData> listUserInfoFormData = new List<FormData>();

    public BRStateInit()
    {
        Init();
    }

    public override void Init()
    {
        szPostAddress = "https://kpoplive.m.codewiz.kr/game/bluered_batting_setting";
        FormData userInfoTockenForm = CreateFormData("token", BlueRed.UserToken);
        listUserInfoFormData.Add(userInfoTockenForm);
    }

    public override void Enter(BlueRed br)
    {
        m_State = eState.Init;
        bChangeState = false;
        br.InitGame();
        br.InitControls();
        br.StartCorWebRequest(m_State, szPostAddress, listFormData, OnCallBack);
    }

    public override void Update(BlueRed br)
    {
        if (bChangeState == false)
        {
            return;
        }
        br.ChangeState(br.stateReady);
    }

    public override void Leave(BlueRed br)
    {

    }

    public override void OnCallBack(string jsonData, BlueRed br)
    {
        var data = JsonUtility.FromJson<BlueredBattingSettingPacket>(jsonData);
        var rslt_set = data.rslt_set;
        br.SetBetNums(rslt_set.batting_sort);
        br.SetDividenRateText(BlueRed.SideType.Blue, rslt_set.win_dividend);
        br.SetDividenRateText(BlueRed.SideType.Green, rslt_set.draw_dividend);
        br.SetDividenRateText(BlueRed.SideType.Red, rslt_set.lose_dividend);

        br.StartCorWebRequest(eState.InitUserInfo, userInfoAddress, listUserInfoFormData, OnCallBackUserInfo);
    }

    public void OnCallBackUserInfo(string jsonData, BlueRed br)
    {
        bChangeState = true;
        var data = JsonUtility.FromJson<BlueredGameUserInfoPacket>(jsonData);
        var rsltSet = data.rslt_set;
        br.SetUserInfo(rsltSet.profile, rsltSet.nick, rsltSet.user_star_su);
    }
}

public class BRStateReady : BRState
{
    private int nRoomNumber;
    private string szPrevPlayResult = null;
    public int RoomNumber { get { return nRoomNumber; } }

    public BRStateReady()
    {
        m_State = eState.Ready;
        Init();
    }

    public override void Init()
    {
        szPostAddress = "https://kpoplive.m.codewiz.kr/game/bluered_room_info_check";
    }

    public override void Enter(BlueRed br)
    {
        br.ResetGame();
        br.PopupNoticeStartBet();
        br.StartCorWebRequest(m_State, szPostAddress, listFormData, OnCallBack, true);
    }

    public override void Update(BlueRed br)
    {
        //br.ChangeState(br.stateBetting);
    }

    public override void Leave(BlueRed br)
    {

    }

    public override void OnCallBack(string jsonData, BlueRed br)
    {
        var data = JsonUtility.FromJson<RecvBlueredRoomInfoCheckPacket>(jsonData);
        var bluered_room_info = data.rslt_set.bluered_room_info;
        nRoomNumber = bluered_room_info.game_no;
        DateTime szEndDateTime = Convert.ToDateTime(bluered_room_info.batting_end_dt);
        DateTime szNowDateTime = Convert.ToDateTime(bluered_room_info.current_date);

        TimeSpan diffTime = szEndDateTime - szNowDateTime;

        br.time = diffTime.Seconds;
        bool bChangeState = br.UpdateTimer();
        string szNowPlayResult = bluered_room_info.play_result;
        if (szPrevPlayResult == null || szPrevPlayResult != szNowPlayResult)
        {
            szPrevPlayResult = szNowPlayResult;
            br.StopCoroutine(m_State);
            br.ChangeState(br.stateResult);
        }
    }
}


public class BRStateBetting : BRState
{
    float _elapsedTime;

    public BRStateBetting()
    {
        Init();
    }

    public override void Init()
    {
        szPostAddress = "https://kpoplive.m.codewiz.kr/game/bluered_batting_setting";
    }

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

    public override void OnCallBack(string jsonData, BlueRed br)
    {

    }
}

public class BRStateResult : BRState
{
    public BRStateResult()
    {
        Init();
    }

    public override void Init()
    {
        szPostAddress = "https://kpoplive.m.codewiz.kr/game/bluered_user_result_proc";
    }

    public override void Enter(BlueRed br)
    {
        br.PlayAudio(0, br.acResult);
        br.resultAct.gameObject.SetActive(true);

        BRStateReady stateReady = br.stateReady as BRStateReady;
        int nRoomNumber = stateReady.RoomNumber;
        FormData userInfoRoomNumForm = CreateFormData("game_no", nRoomNumber.ToString());
        listFormData.Add(userInfoRoomNumForm);
        FormData userInfoTockenForm = CreateFormData("token", BlueRed.UserToken);
        listFormData.Add(userInfoTockenForm);

        br.StartCorWebRequest(eState.Result, szPostAddress, listFormData, OnCallBack);
    }

    public override void Update(BlueRed br)
    {

    }

    public override void Leave(BlueRed br)
    {

    }

    public override void OnCallBack(string jsonData, BlueRed br)
    {
        var data = JsonUtility.FromJson<RecvUserGameResultPacket>(jsonData);
        var rslt_Set = data.rslt_set;

        if(rslt_Set.last_user_star_su != null && rslt_Set.last_user_star_su != "")
        {
            int lastUserStarNum = int.Parse(rslt_Set.last_user_star_su);
            br.SetUserInfo(null, null, lastUserStarNum);
        }
        br.SetTrendResult(BlueRed.SideType.Blue); //트렌드에 결과 값을 추가 (임시코드)
    }
}

public class BRStateReward : BRState
{
    private const float _coinDropInterval = 0.1f;
    private int _coinCount; //떨군 동전갯수
    private int _coinDrop; //떨굴 동전갯수
    float _elapsedTime;

    public BRStateReward()
    {
        Init();
    }

    public override void Init()
    {
        szPostAddress = "https://kpoplive.m.codewiz.kr/game/bluered_batting_setting";
    }

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
        if (_elapsedTime > 5)
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
        for (int i = 0; i < 3; i++)
        {
            br.SetWinAnim((BlueRed.SideType)i, false);
        }
        br.betCoinPool.RestoreAll();
    }

    public override void OnCallBack(string jsonData, BlueRed br)
    {

    }
}