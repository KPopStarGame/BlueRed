using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        ChangeState(stateInit);
    }

    void Update()
    {
        if (_currentState != null)
            _currentState.Update(this);
    }
    //State Instance
    public BRState stateInit = new BRStateInit();
    public BRState stateReady = new BRStateReady();
    public BRState stateBetting = new BRStateBetting();
    public BRState stateResult = new BRStateResult();
    public BRState stateReward = new BRStateReward();



    public enum SideType
    {
        Blue,
        Green,
        Red
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

    public Sprite[] coinSpriteSrc = new Sprite[4]; //베팅 금액에 따른 코인 이미지(실버, 골드, 그린, 퍼플)
    public RectTransform myBetFrom; //내 베팅시 동전 이동의 시작 위치
    public RectTransform playersBetFrom; //다른 플레이어 베팅시 동전 이동의 시작 위치

    public Animator[] betAnimators = new Animator[3];
    public BetCoinPool betCoinPool;

    public Toggle[] betToggles = new Toggle[4]; //베팅 토글
    public Text[] betToggleNums = new Text[4];


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




    public float time
    {
        get => _time;
        set
        {
            _time = value;
            timerText.text = ((int)_time).ToString();
        }
    }


    private const float _betTime = 10.5f;
    private float _time;



    public void InitControls()
    {
        //Trend의 결과 마크 1차원=>2차원 배열 캐슁
        for(int i = 0; i < trendMarks.Length; i++)
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
        for(int i = 0; i < 3; i++)
        {
            SetBetNumsText((SideType)i, 0);
            SetMyBetNumsText((SideType)i, 0);
        }

        time = _betTime;
        _betCount = 0;
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

    public bool UpdateTimer()
    {
        if (_time <= 0)
            return true;

        bool done = false;

        int prev = (int)_time;
        _time -= Time.deltaTime;

        if(prev > (int)_time)
        {
            PlayAudio(1, acCount);
        }

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
    }



    public void OnClickBetBlueArea()
    {
        if (!IsInState(stateBetting))
            return;
        BetCoin(SideType.Blue, true, _betIndex);
    }

    public void OnClickBetGreenArea()
    {
        if (!IsInState(stateBetting))
            return;
        BetCoin(SideType.Green, true, _betIndex);
    }

    public void OnClickBetRedArea()
    {
        if (!IsInState(stateBetting))
            return;
        BetCoin(SideType.Red, true, _betIndex);
    }


    public void BetCoin(SideType type, bool myBet, int betIndex)
    {
        if(myBet)
        {
            if(_betCount == 0)
            {
                _betSide = type;
            }
            else if(_betSide != type || _betCount >= _betLimit)
            {
                return; //실패처리
            }
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
        float x = Random.Range(t.localPosition.x - halfSize.x, t.localPosition.x + halfSize.x);
        float y = Random.Range(t.localPosition.y - halfSize.y, t.localPosition.y + halfSize.y);
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
        for(int i = 0; i < activeCoins.Length; i++)
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

        Invoke("CloseNotice", 0.5f);
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


    //아래는 참고용 예시 코드
    /* 
    private static string _userToken = "dyt2Sys5Q3JIdnRMVWlRa0dFaEtyZz09OjprMmlTbVBlNTA4dVVmSjlI"; //테스트 토큰
    
    public Text tokenText;

    //유저 토큰
    public void GetUserToken()
    {
        string source = Application.absoluteURL;

        int beginIndex = source.IndexOf('?');
        if (beginIndex == -1) //error: bad format
        {
            tokenText.text = "GetUserToken error: " + source;
            return;
        }

        string param = source.Substring(source.IndexOf('?') + 1);
        string[] kvPair = param.Split('=');
        if (kvPair.Length == 2 && kvPair[0] == "token")
        { //success
            tokenText.text = kvPair[1];
            _userToken = kvPair[1];
        }
        else
        { //error: bad format
            tokenText.text = "params are missing.";
            return;
        }
    }
    
    

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
