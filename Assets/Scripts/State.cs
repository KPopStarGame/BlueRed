using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum eState
{
    Init = 0,
    InitUserInfo,
    Ready,
    Bet,
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
    public abstract void Init(bool bTestVer);
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
    private string userInfoAddress;
    protected List<FormData> listUserInfoFormData = new List<FormData>();

    public BRStateInit(bool bTestVer)
    {
        Init(bTestVer);
    }

    public override void Init(bool bTestVer)
    {
        userInfoAddress = bTestVer ? "https://kpoplive.m.codewiz.kr/game/game_user_info" : "/game/game_user_info";
        szPostAddress = bTestVer ? "https://kpoplive.m.codewiz.kr/game/bluered_batting_setting" : "/game/bluered_batting_setting";
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
    private string szPrevRoomStartTime = "";

    public BRStateReady(bool bTestVer)
    {
        Init(bTestVer);
    }

    public override void Init(bool bTestVer)
    {
        m_State = eState.Ready;
        szPostAddress = bTestVer ? "https://kpoplive.m.codewiz.kr/game/bluered_room_info_check" : "/game/bluered_room_info_check";
    }

    public override void Enter(BlueRed br)
    {
        br.ResetGame();
        br.PopupNoticeStartBet();
        br.StartCorWebRequest(m_State, szPostAddress, listFormData, OnCallBack, true);
        br.time = 10;
    }

    public override void Update(BlueRed br)
    {
        //br.ChangeState(br.stateBetting);
    }

    public override void Leave(BlueRed br)
    {
        br.noticeStartObj.SetActive(false);
    }

    public override void OnCallBack(string jsonData, BlueRed br)
    {
        var data = JsonUtility.FromJson<RecvBlueredRoomInfoCheckPacket>(jsonData);
        var bluered_room_info = data.rslt_set.bluered_room_info;

        string szNowRoomStartTime = bluered_room_info.room_start_dt;
        bool bRoomChange = szPrevRoomStartTime != szNowRoomStartTime;

        string szNowPlayResult = bluered_room_info.play_result;

        if (szNowPlayResult == "" && bRoomChange == true)
        {
            szPrevRoomStartTime = szNowRoomStartTime;

            br.StopCoroutine(m_State);
            br.ChangeState(br.stateBetting);
        }
    }
}


public class BRStateBetting : BRState
{
    private int nRoomNumber;
    private int nWaitOneFrame;
    private bool bCorrect;
    public bool Correct { get { return bCorrect; } }
    public int RoomNumber { get { return nRoomNumber; } }

    public BRStateBetting(bool bTestVer)
    {
        Init(bTestVer);
    }

    public override void Init(bool bTestVer)
    {
        m_State = eState.Bet;
        szPostAddress = bTestVer ? "https://kpoplive.m.codewiz.kr/game/bluered_room_info_check" : "/game/bluered_room_info_check";
    }

    public override void Enter(BlueRed br)
    {
        nWaitOneFrame = 0;
        br.InitKeepBetButton();
        br.StartCorWebRequest(m_State, szPostAddress, listFormData, OnCallBack, true);
    }

    public override void Update(BlueRed br)
    {
    }

    public override void Leave(BlueRed br)
    {
        br.PopupNoticeMyBet();
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

        br.AddServerBatCoin(BlueRed.SideType.Blue, bluered_room_info.win_batting_cnt);
        br.AddServerBatCoin(BlueRed.SideType.Green, bluered_room_info.draw_batting_cnt);
        br.AddServerBatCoin(BlueRed.SideType.Red, bluered_room_info.lose_batting_cnt);

        if(bChangeState == true)
        {
            ;
        }

        if (szNowPlayResult != "" && bChangeState == true)
        {
            BlueRed.SideType sideTypeClient = br.BetSide;
            BlueRed.SideType sideTypeServer = BlueRed.SideType.None;
            switch (szNowPlayResult)
            {
                case "WIN":
                    sideTypeServer = BlueRed.SideType.Blue;
                    break;
                case "DRAW":
                    sideTypeServer = BlueRed.SideType.Green;
                    break;
                case "LOSE":
                    sideTypeServer = BlueRed.SideType.Red;
                    break;
                case "NONE":
                    sideTypeServer = BlueRed.SideType.None;
                    break;
            }
            bCorrect = sideTypeClient == sideTypeServer && sideTypeServer != BlueRed.SideType.None;

            BRStateResult stateResult = br.stateResult as BRStateResult;
            if (stateResult != null)
            {
                stateResult.SetResult(szNowPlayResult);
            }
            br.StopCoroutine(m_State);
            br.ChangeState(br.stateResult);
        }
        else if(bChangeState == true)
        {
            if(nWaitOneFrame < 5)
            {
                nWaitOneFrame += 1;
                return;
            }
            br.StopCoroutine(m_State);
            br.ChangeState(br.stateReady);
        }
    }
}

public class BRStateResult : BRState
{
    private BlueRed.SideType resultType;
    public BlueRed.SideType ResultType { get { return resultType; } }

    public BRStateResult(bool bTestVer)
    {
        Init(bTestVer);
    }

    public override void Init(bool bTestVer)
    {
        szPostAddress = bTestVer ? "https://kpoplive.m.codewiz.kr/game/bluered_user_result_proc" : "/game/bluered_user_result_proc";
    }

    public override void Enter(BlueRed br)
    {
        br.PlayAudio(0, br.acResult);
        br.resultAct.gameObject.SetActive(true);
        br.SetTrendResult(resultType); //트렌드에 결과 값을 추가 (임시코드)

        BRStateBetting stateBetting = br.stateBetting as BRStateBetting;
        bool bCorrect = stateBetting.Correct;
        if(bCorrect == false)
        {
            return;
        }
        int nRoomNumber = stateBetting.RoomNumber;
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
    }

    public void SetResult(string szResult)
    {
        switch (szResult)
        {
            case "WIN":
                resultType = BlueRed.SideType.Blue;
                break;
            case "DRAW":
                resultType = BlueRed.SideType.Green;
                break;
            case "LOSE":
                resultType = BlueRed.SideType.Red;
                break;
        }
    }
}

public class BRStateReward : BRState
{
    private const float _coinDropInterval = 0.1f;
    private int _coinCount; //떨군 동전갯수
    private int _coinDrop; //떨굴 동전갯수
    float _elapsedTime;

    public BRStateReward(bool bTestVer)
    {
        Init(bTestVer);
    }

    public override void Init(bool bTestVer)
    {
        //szPostAddress = "https://kpoplive.m.codewiz.kr/game/bluered_batting_setting";
    }

    public override void Enter(BlueRed br)
    {
        _elapsedTime = 0;


        BRStateResult stateResult = br.stateResult as BRStateResult;

        br.SetWinAnim(stateResult != null ? stateResult.ResultType : BlueRed.SideType.Blue, true); //임시 연출
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