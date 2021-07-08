using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;

public class BlueRed : MonoBehaviour
{
    //State Machine
    private BRState _currentState = null;

    public bool IsInState(BRState state)
    {
        return state == _currentState;
    }

    public void ChangeState(BRState state)
    {
        if (IsInState(state)) return;
        if (_currentState != null)
        {
            _currentState.Leave(this);
        }

        _currentState = state;
        _currentState.Enter(this);
    }

    void Awake()
    {
        if(bTestVer == false)
        {
            GetUserToken();
        }
        stateInit = new BRStateInit(bTestVer);
        stateReady = new BRStateReady(bTestVer);
        stateBetting = new BRStateBetting(bTestVer);
        stateResult = new BRStateResult(bTestVer);
        stateReward = new BRStateReward(bTestVer);
        ChangeState(stateInit);
    }

    void Update()
    {
        if (_currentState != null)
            _currentState.Update(this);
    }

    public bool bTestVer = true;

    //State Instance
    public BRState stateInit;
    public BRState stateReady;
    public BRState stateBetting;
    public BRState stateResult;
    public BRState stateReward;



    public enum SideType
    {
        Blue,
        Green,
        Red,
        None,
    }

    public Text timerText;

    [Header("기본 화면")]
    //blue, green, red 순서
    public RectTransform[] betAreas = new RectTransform[3];
    public Image[] betAreaImages = new Image[3];
    public Sprite[] betAreaNormalImageSources = new Sprite[3];
    public Sprite[] betAreaWinImageSources = new Sprite[3];
    public Text[] totalBetTexts = new Text[3];
    public Text[] myBetTexts = new Text[3];
    public Text[] dividenRateTexts = new Text[3];

    public Sprite[] coinSpriteSrc = new Sprite[4]; //베팅 금액에 따른 코인 이미지(실버, 골드, 그린, 퍼플)
    public RectTransform myBetFrom; //내 베팅시 동전 이동의 시작 위치
    public RectTransform playersBetFrom; //다른 플레이어 베팅시 동전 이동의 시작 위치

    public Animator[] betAnimators = new Animator[3];
    public BetCoinPool betCoinPool;

    public Toggle[] betToggles = new Toggle[4]; //베팅 토글
    public Text[] betToggleNums = new Text[4];

    [Header("유저 정보")]
    public RawImage userProfile;
    public Text userNickName;
    public Text remainCoinText;
    private int remainCoin;


    [Header("결과 연출용 이미지")]
    public GameObject resultAct;
    public Image[] resultActBlueRed = new Image[2];
    public Image[] resultActHands = new Image[2];

    [Header("트렌드")]
    public Image[] trendMarks = new Image[40];
    //blue, green, red 순서
    public Text[] trendProportionText = new Text[3];
    public Sprite[] trendMarkSources = new Sprite[3];

    [Header("사운드")]
    public Toggle soundToggle;
    public AudioSource[] audioSource;
    public AudioClip acButton;
    public AudioClip acBetCoin;
    public AudioClip acCount;
    public AudioClip acResult;
    public AudioClip acWin;


    [Header("알림")]
    public GameObject noticeObj;
    public GameObject noticeStartBet;
    public Text noticeMyBet;

    public RewardCoin[] rewardCoins = new RewardCoin[20];
    public static Queue<RewardCoin> coinPool = new Queue<RewardCoin>();

    //Bet
    private int[] _betTable = new int[4] { 1, 10, 100, 500 }; //베팅 금액 (4개) 테이블. 값은 서버에서 내려 받음
    private int[] _betNums = new int[3]; //blue, green, red 각각의 베팅 금액
    private int _betIndex;
    private int _betCount;
    private SideType _betSide;
    public SideType BetSide { get { return _betSide; } }
    private const int _betLimit = 5; //유저의 최대 베팅 횟수는 5회

    //My Bet
    private struct BetInfo //Keep Betting을 위하여 필요
    {
        public SideType type;
        public int[] betNums;
        public int sum;
    }
    private BetInfo _prevBet;
    private BetInfo _currentBet;

    //Trend
    private int _gameNum;
    private int[] _trendData = new int[3] { 0, 0, 0 };
    private float[] _symbolProp = new float[3];

    //ServerCoin
    private int[] coinCount = new int[4];


    public float time
    {
        get => _time;
        set
        {
            _time = value;
            if(_time < 0)
            {
                _time = 0;
            }
            timerText.text = ((int)_time).ToString();
        }
    }


    private const float _betTime = 10.5f;
    private float _time;



    public void InitControls()
    {
        //Trend의 결과 마크 1차원=>2차원 배열 캐슁
        for (int i = 0; i < trendMarks.Length; i++)
        {
            trendMarks[i].sprite = null;
            trendMarks[i].enabled = false;
        }
        _trendData[0] = _trendData[1] = _trendData[2] = 0;
        _symbolProp[0] = _symbolProp[1] = _symbolProp[2] = 0;
        UpdateTrendDataText();

        _gameNum = 0;

        InitBetToggles();
        InitSoundToggle();
    }

    public void InitGame()
    {
        _prevBet.sum = 0;
        _prevBet.betNums = new int[4];

        _currentBet.sum = 0;
        _currentBet.betNums = new int[4];

        //ResetGame();
    }

    public void ResetGame()
    {
        for (int i = 0; i < 3; i++)
        {
            SetBetNumsText((SideType)i, 0);
            SetMyBetNumsText((SideType)i, 0);
        }
        _betSide = SideType.None;
        _betCount = 0;
    }

    public void ResetCoinCount()
    {
        for (int i = 0; i < coinCount.Length; i++)
        {
            coinCount[i] = 0;
        }
    }

    #region BetToggles
    //하단 베팅 금액 토글 초기화
    public void SetBetNums(List<int> betNums)
    {
        for (int i = 0; i < betToggleNums.Length; i++)
        {
            _betTable[i] = betNums[i];
            betToggleNums[i].text = betNums[i].ToString();
        }
        //RequestStarCheck(); //베팅 금액이 충분한지 서버에 물어봄
    }

    public void InitBetToggles()
    {
        betToggles[0].isOn = true;
        _betIndex = 0;
        betToggles[0].onValueChanged.AddListener((bool on) => { if (!on) return; _betIndex = 0; PlayClickSound(); /*PlayAudio(3, acButton); RequestStarCheck();*/ }); //베팅 토글버튼 입력시 액션
        betToggles[1].onValueChanged.AddListener((bool on) => { if (!on) return; _betIndex = 1; PlayClickSound(); /*PlayAudio(3, acButton); RequestStarCheck();*/ });
        betToggles[2].onValueChanged.AddListener((bool on) => { if (!on) return; _betIndex = 2; PlayClickSound(); /*PlayAudio(3, acButton); RequestStarCheck();*/ });
        betToggles[3].onValueChanged.AddListener((bool on) => { if (!on) return; _betIndex = 3; PlayClickSound(); /*PlayAudio(3, acButton); RequestStarCheck();*/ });
    }
    #endregion

    private int prevTime;
    public bool UpdateTimer()
    {
        if (_time <= 0)
            return true;

        bool done = false;

        if (prevTime > (int)_time)
        {
            PlayAudio(1, acCount);
        }
        prevTime = (int)_time;

        if (done = _time <= 0)
        {
            _time = 0;
        }

        timerText.text = ((int)_time).ToString();

        return done;
    }

    public void SetBetNumsText(SideType type, int bet)
    {
        int index = (int)type;
        _betNums[index] = bet;
        totalBetTexts[index].text = string.Format("{0:#,0}", _betNums[index]);
    }

    public int GetDiffValue(SideType type, int totalBet)
    {
        int index = (int)type;
        int storageBet = _betNums[index];
        return totalBet - storageBet;
    }

    public void AddServerBatCoin(SideType type, int totalBet)
    {
        ResetCoinCount();
        int nDiffValue = GetDiffValue(type, totalBet);
        if(nDiffValue == 0)
        {
            return;
        }
        while(nDiffValue > 0)
        {
            int nMinusBetValue = 0;
            int nArrayCount = 0;
            GetMinusCoinValue(nDiffValue, ref nArrayCount, ref nMinusBetValue);
            for (int i = 0; i < nArrayCount + 1; i++)
            {
                coinCount[i] += 1;
            }
            nDiffValue -= nMinusBetValue;
        }
        for (int i = 0; i < coinCount.Length; i++)
        {
            int nCoinCount = coinCount[i];
            if(nCoinCount == 0)
            {
                continue;
            }
            for (int n = 0; n < nCoinCount; n++)
            {
                BetCoin(type, false, i);
            }
        }
    }

    public void GetMinusCoinValue(int totalValue, ref int arrayCount, ref int minusBetValue)
    {
        arrayCount = 0;
        minusBetValue = 0;

        for(int i = 0; i < _betTable.Length; i++)
        {
            int nCompareValue = minusBetValue + _betTable[i];
            if(nCompareValue <= totalValue)
            {
                arrayCount = i;
                minusBetValue = nCompareValue;
            }
            else
            {
                return;
            }
        }
    }

    public void AddBetNumsText(SideType type, int bet)
    {
        int index = (int)type;
        _betNums[index] += bet;
        totalBetTexts[index].text = string.Format("{0:#,0}", _betNums[index]);
    }

    public void SetMyBetNumsText(SideType type, int bet)
    {
        int index = (int)type;
        myBetTexts[index].text = string.Format("{0:#,0}", bet); 
        ReduceRemainCoin(bet);
    }
    
    public void SetDividenRateText(SideType type, int rate)
    {
        int index = (int)type;
        dividenRateTexts[index].text = string.Format("{0}", rate);
    }


    public void OnClickBetBlueArea()
    {
        if (!IsInState(stateBetting))
            return;
        if (CheckBetCoin(SideType.Blue) == false)
        {
            return;
        }
        StartCheckBetCoin();
        //BetCoin(SideType.Blue, true, _betIndex);
    }

    public void OnClickBetGreenArea()
    {
        if (!IsInState(stateBetting))
            return;
        if (CheckBetCoin(SideType.Green) == false)
        {
            return;
        }
        StartCheckBetCoin();
        //BetCoin(SideType.Green, true, _betIndex);
    }

    public void OnClickBetRedArea()
    {
        if (!IsInState(stateBetting))
            return;
        if(CheckBetCoin(SideType.Red) == false)
        {
            return;
        }
        StartCheckBetCoin();
        //BetCoin(SideType.Red, true, _betIndex);
    }


    public bool CheckBetCoin(SideType type)
    {
        if (_betCount == 0)
        {
            _betSide = type;
        }
        else if (_betSide != type || _betCount >= _betLimit)
        {
            return false; //실패처리
        }
        return true;
    }

    public void BetCoin(SideType type, bool myBet, int betIndex)
    {
        if (myBet)
        {
            _betCount++;

            _currentBet.sum += _betTable[betIndex];
            _currentBet.betNums[betIndex]++;
            _currentBet.type = type;

            SetMyBetNumsText(type, _currentBet.sum);
        }
        AddBetNumsText(type, _betTable[betIndex]);

        //동전발사
        int index = (int)type;

        var coin = betCoinPool.GetNewItem();
        RectTransform coinRectTransform = coin.GetComponent<RectTransform>();


        RectTransform t = betAreas[index];
        Vector2 halfSize = new Vector2((t.rect.width - coinRectTransform.rect.width) / 2f, (t.rect.height - coinRectTransform.rect.height) / 2f);
        float x = UnityEngine.Random.Range(t.localPosition.x - halfSize.x, t.localPosition.x + halfSize.x);
        float y = UnityEngine.Random.Range(t.localPosition.y - halfSize.y, t.localPosition.y + halfSize.y);
        Vector2 to = new Vector2(x, y);

        coin.from = myBet ? myBetFrom.localPosition : playersBetFrom.localPosition;
        coin.to = to;
        coin.hideReach = false;

        //베팅 금액에 따라 동전 종류를 변경
        coin.coinImage.sprite = coinSpriteSrc[betIndex];
        coin.numText.text = _betTable[betIndex].ToString();

        PlayAudio(0, acBetCoin);

    }

    //테이블의 코인들이 회수되는 이동 애니메이션
    public void RestoreCoins()
    {
        BetCoin[] activeCoins = betCoinPool.activeObjects.ToArray();
        for (int i = 0; i < activeCoins.Length; i++)
        {
            activeCoins[i].hideReach = true;
            activeCoins[i].from = activeCoins[i].to;
            activeCoins[i].to = playersBetFrom.localPosition;
        }
    }



    #region TREND

    //트렌드 창에 결과 값을 하나씩 밀어 넣음
    public void SetTrendResult(SideType type)
    {
        ++_trendData[(int)type];
        if (_gameNum >= trendMarks.Length)
        {
            int firstData = (int)SpriteToSideType(trendMarks[0].sprite);
            --_trendData[firstData];

            for (int i = 0; i < trendMarks.Length - 1; i++)
            {
                trendMarks[i].sprite = trendMarks[i + 1].sprite;
            }
        }

        int n = Mathf.Min(_gameNum, trendMarks.Length - 1);
        if (trendMarks[n].enabled == false)
            trendMarks[n].enabled = true;
        trendMarks[n].sprite = trendMarkSources[(int)type];

        for (int i = 0; i < 3; i++)
        {
            _symbolProp[i] = ((float)_trendData[i] / (n + 1)) * 100f;
        }


        ++_gameNum;

        Debug.Log("Blue: " + _trendData[0].ToString() + ", Green: " + _trendData[1].ToString() + ", Red: " + _trendData[2].ToString());
        UpdateTrendDataText();
    }

    private SideType SpriteToSideType(Sprite sprite)
    {
        if (sprite == trendMarkSources[(int)SideType.Blue])
            return SideType.Blue;
        else if (sprite == trendMarkSources[(int)SideType.Green])
            return SideType.Green;

        return SideType.Red;
    }

    private void UpdateTrendDataText()
    {
        for (int i = 0; i < 3; i++)
        {
            trendProportionText[i].text = _symbolProp[i].ToString("0") + "%";
        }
        /*
        trendProportionText[0].text = _trendData[0].ToString();
        trendProportionText[1].text = _trendData[1].ToString();
        trendProportionText[2].text = _trendData[2].ToString();
        */
    }

    #endregion

    #region Sound
    public void InitSoundToggle()
    {
        soundToggle.isOn = true;
        soundToggle.onValueChanged.AddListener(
            (bool on) =>
            {
                for (int i = 0; i < audioSource.Length; i++)
                {
                    audioSource[i].mute = !on;
                }
            }
            );
    }

    public void PlayAudio(int channel, AudioClip source, bool loop = false)
    {
        audioSource[channel].clip = source;
        audioSource[channel].loop = loop;
        audioSource[channel].Play();
    }

    public void StopAudio(int channel)
    {
        audioSource[channel].Stop();
    }

    public void PlayClickSound()
    {
        PlayAudio(3, acButton);
    }

    #endregion

    /*
    public GameObject noticeObj;
    public GameObject noticeStartBet;
    public Text noticeMyBet;
     */
    public void PopupNoticeStartBet()
    {
        noticeObj.SetActive(true);
        noticeStartBet.SetActive(true);
        noticeMyBet.gameObject.SetActive(false);

        //Invoke("CloseNotice", 0.5f);
    }
    public void PopupNoticeMyBet()
    {
        noticeObj.SetActive(true);
        noticeStartBet.SetActive(false);
        noticeMyBet.gameObject.SetActive(true);
        noticeMyBet.text = "";

        Invoke("CloseNotice", 1f);
    }


    public void CloseNotice()
    {
        noticeObj.SetActive(false);
    }


    public void SetWinAnim(SideType type, bool on)
    {
        int typeIndex = (int)type;
        betAnimators[typeIndex].SetBool("win", on);
        betAreaImages[typeIndex].sprite = on ? betAreaWinImageSources[typeIndex] : betAreaNormalImageSources[typeIndex];

    }

    public Dictionary<eState, Action<string, BlueRed>> m_mapAction = new Dictionary<eState, Action<string, BlueRed>>();
    public Dictionary<eState, Coroutine> m_mapCorutine = new Dictionary<eState, Coroutine>();
    public Action<string, BlueRed> actionForWebRequest;

    public void StartCorWebRequest(eState state, string szPostAddress, List<FormData> listFormData, Action<string, BlueRed> action, bool bLoop = false)
    {
        Action<string, BlueRed> useAction = null;
        if (m_mapAction.ContainsKey(state) == true)
        {
            useAction = m_mapAction[state];
        }
        else
        {
            useAction = action;
            m_mapAction.Add(state, useAction);
        }

        Coroutine nCoroutine = null;
        if (bLoop == false)
        {
            nCoroutine = StartCoroutine(IWebRequest(szPostAddress, listFormData, action));
        }
        else
        {
            nCoroutine = StartCoroutine(IWebRequestLoop(szPostAddress, listFormData, action, bLoop));
        }
        if (m_mapCorutine.ContainsKey(state) == true)
        {
            Coroutine prevCoroutine = m_mapCorutine[state];
            if (prevCoroutine != null)
            {
                StopCoroutine(prevCoroutine);
                prevCoroutine = null;
            }
            m_mapCorutine[state] = nCoroutine;
        }
        else
        {
            m_mapCorutine.Add(state, nCoroutine);
        }


    }

    public void StopCoroutine(eState state)
    {
        if (m_mapCorutine.ContainsKey(state))
        {
            Coroutine nCoroutine = m_mapCorutine[state];
            if (nCoroutine != null)
            {
                StopCoroutine(nCoroutine);
                nCoroutine = null;
            }
        }
    }

    public IEnumerator IWebRequest(string szPostAddress, List<FormData> listFormData, Action<string, BlueRed> action)
    {
        UnityWebRequest p_webRequest = new UnityWebRequest();
        WWWForm p_form = new WWWForm();

        lock (listFormData)
        {
            for (int i = 0; i < listFormData.Count; i++)
            {
                FormData p_FormData = listFormData[i];
                p_form.AddField(p_FormData.szKey, p_FormData.szValue);
            }
        }

        using (p_webRequest = UnityWebRequest.Post(szPostAddress, p_form))
        {
            yield return p_webRequest.SendWebRequest();

            if (p_webRequest.isNetworkError)
            {
                Debug.Log(p_webRequest.error);
            }
            else
            {
                action?.Invoke(p_webRequest.downloadHandler.text, this);
            }
        }
    }

    public IEnumerator IWebRequestLoop(string szPostAddress, List<FormData> listFormData, Action<string, BlueRed> action, bool bLoop, float fWaitTime = 0.5f)
    {
        UnityWebRequest p_webRequest = new UnityWebRequest();
        WWWForm p_form = new WWWForm();

        lock (listFormData)
        {
            for (int i = 0; i < listFormData.Count; i++)
            {
                FormData p_FormData = listFormData[i];
                p_form.AddField(p_FormData.szKey, p_FormData.szValue);
            }
        }
        while (bLoop)
        {
            using (p_webRequest = UnityWebRequest.Post(szPostAddress, p_form))
            {
                yield return p_webRequest.SendWebRequest();

                if (p_webRequest.isNetworkError)
                {
                    Debug.Log(p_webRequest.error);
                }
                else
                {
                    action?.Invoke(p_webRequest.downloadHandler.text, this);
                }
            }
            yield return new WaitForSeconds(fWaitTime);
        }
    }

    IEnumerator DownloadImage(string MediaUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
        yield return request.SendWebRequest();
        if (request.isNetworkError || request.isHttpError)
            Debug.Log(request.error);
        else
            userProfile.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
    }

    public void SetUserInfo(string _imageUrl, string _nickName, int _remainCoin)
    {
        if(_imageUrl != null)
        {
            StartCoroutine(DownloadImage(string.Format("{0}{1}", "https://kpoplive.m.codewiz.kr", _imageUrl)));
        }
        if(_nickName != null)
        {
            this.userNickName.text = _nickName;
        }
        if(_remainCoin >= 0 && _remainCoin != remainCoin)
        {
            this.remainCoin = _remainCoin;
            this.remainCoinText.text = this.remainCoin.ToString();
        }
    }

    public void ReduceRemainCoin(int _reduceValue)
    {
        this.remainCoin -= _reduceValue;
        this.remainCoinText.text = this.remainCoin.ToString();
    }

    public void StartCheckBetCoin()
    {
        m_listFormData.Clear();
        FormData formData_Token = CreateFormData("token", _userToken);
        m_listFormData.Add(formData_Token);

        int betValue = _betTable[_betIndex];
        FormData formData_BetValue = CreateFormData("batting_su", betValue.ToString());
        m_listFormData.Add(formData_BetValue);

        StartCorWebRequest(eState.Betting, "https://kpoplive.m.codewiz.kr/game/game_user_star_check", m_listFormData, OnCallBack_CheckStar);
    }

    public void StartSetBetCoin()
    {
        m_listFormData.Clear();
        BRStateBetting p_stateBetting = stateBetting as BRStateBetting;
        int nRoomNumber = p_stateBetting.RoomNumber;
        FormData formData_GameNum = CreateFormData("game_no", nRoomNumber.ToString());
        m_listFormData.Add(formData_GameNum);

        FormData formData_Token = CreateFormData("token", _userToken);
        m_listFormData.Add(formData_Token);

        int betValue = _betTable[_betIndex];
        FormData formData_BetValue = CreateFormData("batting_su", betValue.ToString());
        m_listFormData.Add(formData_BetValue);

        string selectValue = null;
        switch(_betSide)
        {
            case SideType.Blue:
                selectValue = "WIN";
                break;
            case SideType.Green:
                selectValue = "DRAW";
                break;
            case SideType.Red:
                selectValue = "LOSE";
                break;
        }
        FormData formData_SelectValue = CreateFormData("select_value", selectValue);
        m_listFormData.Add(formData_SelectValue);

        StartCorWebRequest(eState.Betting, "https://kpoplive.m.codewiz.kr/game/bluered_join_proc", m_listFormData, OnCallBack_JoinProc);
    }

    public void OnCallBack_CheckStar(string jsonData, BlueRed br)
    {
        var data = JsonUtility.FromJson<RecvUserStarCheckPacket>(jsonData);
        var rslt_Set = data.rslt_set;
        switch(rslt_Set.rtv)
        {
            case "SUCC":
                StartSetBetCoin();
                break;
            case "FALSE":
                break;
        }
    }

    public void OnCallBack_JoinProc(string jsonData, BlueRed br)
    {
        var data = JsonUtility.FromJson<RecvBlueredJoinProcPacket>(jsonData);
        var rslt_Set = data.rslt_set;
        switch(rslt_Set.rtv)
        {
            case "SUCC":
                BetCoin(_betSide, true, _betIndex);
                break;
        }
    }

    //유저 토큰
    private List<FormData> m_listFormData = new List<FormData>();
    private static string _userToken = "dyt2Sys5Q3JIdnRMVWlRa0dFaEtyZz09OjprMmlTbVBlNTA4dVVmSjlI"; //테스트 토큰
    public static string UserToken { get { return _userToken; } }
    public void GetUserToken()
    {
        string source = Application.absoluteURL;

        int beginIndex = source.IndexOf('?');
        if (beginIndex == -1) //error: bad format
        {
            return;
        }

        string param = source.Substring(source.IndexOf('?') + 1);
        string[] kvPair = param.Split('=');
        if (kvPair.Length == 2 && kvPair[0] == "token")
        { //success
            _userToken = kvPair[1];
        }
        else
        { //error: bad format
            return;
        }
    }

    public static FormData CreateFormData(string key, string value)
    {
        FormData userInfoTockenForm = new FormData();
        userInfoTockenForm.szKey = key;
        userInfoTockenForm.szValue = value;
        return userInfoTockenForm;
    }

    //아래는 참고용 예시 코드
    /* 
    

    //유저 정보를 요청
    public RecvUserInfoPacket userInfo;
    public RecvUserBetSettingPacket bettingInfo;
    public void RequestUserInfo()
    {
        loadingIndicator.SetActive(true);
        StartCoroutine(RequestUserInfoImpl());
    }

    private IEnumerator RequestUserInfoImpl()
    {
        //유저 정보
        WWWForm _form = new WWWForm();
        _form.AddField("token", _userToken);
        UnityWebRequest www = UnityWebRequest.Post("/game/game_user_info", _form);
        //UnityWebRequest www = UnityWebRequest.Post("https://kpoplive.m.codewiz.kr/game/game_user_info", _form);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            loadingIndicator.SetActive(false);
            Debug.LogError(www.error);
            ShowErrorMessageBox(www.error, RequestUserInfo);
            yield break;
        }
        else
        {
            Debug.Log("profile url: " + userInfo.rslt_set.profile);
            userInfo = JsonUtility.FromJson<RecvUserInfoPacket>(www.downloadHandler.text);

            nicknameText.text = userInfo.rslt_set.nick;
            cash = userInfo.rslt_set.user_star_su;
            
            UnityWebRequest wwwImage = UnityWebRequestTexture.GetTexture(userInfo.rslt_set.profile);
            wwwImage.timeout = 2;
            yield return wwwImage.SendWebRequest();

            if (wwwImage.isNetworkError || wwwImage.isHttpError)
            {
                loadingIndicator.SetActive(false);
                Debug.LogError(wwwImage.error);
                ShowErrorMessageBox(wwwImage.error, RequestUserInfo);
                yield break;
            }
            else
            {
                userImage.texture = ((DownloadHandlerTexture)wwwImage.downloadHandler).texture;
            }
            
        }

        //베팅 정보
        _form = new WWWForm();
        _form.AddField("token", _userToken);
        www = UnityWebRequest.Post("/game/slot_batting_setting", _form);
        //www = UnityWebRequest.Post("https://kpoplive.m.codewiz.kr/game/slot_batting_setting", _form);
        www.timeout = 2;
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            loadingIndicator.SetActive(false);
            Debug.LogError(www.error);
            ShowErrorMessageBox(www.error, RequestUserInfo);
            yield break;
        }
        else
        {
            bettingInfo = JsonUtility.FromJson<RecvUserBetSettingPacket>(www.downloadHandler.text);
            if (bettingInfo.rslt_set.batting_sort.Count < betMultTbl.Length)
            { //서버에서 날려주는 갯수가 더 적으면 안됨
                Debug.LogError("Data Error!: Bet setting size");
            }
            else
            {
                SetBetNums(bettingInfo.rslt_set.batting_sort);
                loadingIndicator.SetActive(false);
            }
            //Debug.Log(www.downloadHandler.text);
        }
    }

     */
}


public class FormData
{
    public string szKey;
    public string szValue;
}