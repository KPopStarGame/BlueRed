using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//이 코드는 다른 프로젝트에서 사용한 패킷 구조체입니다.
//참고해주시고 현재 프로토콜에 맞게 수정해서 사용하셔도 될 것 같습니다.
public class RecvPacket
{
    public int rslt_cd; //응답코드 (0 : 성공, 그외 : 실패)
    public string rslt_msg; //응답메시지
}

public class BlueredGameUserInfoPacket : RecvPacket
{
    //유저정보
    [System.Serializable]
    public struct RsltSet
    {
        public string profile;
        public string nick;
        public int user_star_su;
    }
    public RsltSet rslt_set;
}

public class BlueredBattingSettingPacket : RecvPacket
{ 
    //방정보(최초 진입)
    [System.Serializable]
    public struct RsltSet
    {
        public string rtv;
        public List<int> batting_sort;
        public int win_dividend;
        public int lose_dividend;
        public int draw_dividend;
        public int batting_possible_count;
    }
    public RsltSet rslt_set;
}

public class RecvBlueredRoomInfoCheckPacket : RecvPacket
{ 
    //방 정보 확인(실시간)
    [System.Serializable]
    public struct RsltSet
    {
        public string rtv; //결과 SUCC : 성공, FALSE : 실패
        public BlueredRoomInfo bluered_room_info;
    }
    [System.Serializable]
    public struct BlueredRoomInfo
    {
        public int game_no;
        public string room_start_dt;
        public string room_end_dt;
        public string batting_start_dt;
        public string batting_end_dt;
        public int win_join_su;
        public int draw_join_su;
        public int lose_join_su;
        public int win_batting_cnt;
        public int draw_batting_cnt;
        public int lose_batting_cnt;
        public string play_result;
        public string reg_date;
    }
    public RsltSet rslt_set;
}

public class RecvUserStarCheckPacket : RecvPacket
{ //유저 재화 보유 체크
    [System.Serializable]
    public struct RsltSet
    {
        public string rtv; //결과 SUCC : 성공, FALSE : 실패
        public string msg; //메시지
        public string store_url; //상점 URL 실패시 사용
    }
    public RsltSet rslt_set;
}

public class RecvBlueredJoinProcPacket : RecvPacket
{
    [System.Serializable]
    public struct RsltSet
    {
        public string rtv; //결과 SUCC : 성공, FALSE : 실패
        public int game_no;
        public string token;
    }
    public RsltSet rslt_set;
}




public class RecvUserBetSettingPacket : RecvPacket
{ //베팅값 셋팅
    [System.Serializable]
    public struct UserData
    {
        public string rtv; //결과 SUCC : 성공, FALSE : 실패
        public List<int> batting_sort; //베팅종류
        public List<int> multiple; //배수정보
    }
    public UserData rslt_set;
}

public class RecvUserGameResultPacket : RecvPacket
{ //게임 결과
    [System.Serializable]
    public struct UserData
    {
        public string rtv; //결과 SUCC : 성공, FALSE : 실패
        public string play_result; //결과값 ("WIN" / "LOSE")
        public int index;
        public int multiple_su; //배당(배수 절대값)
        public int cal_star_su; //게임비(차감, 증감 금액)
        public int last_user_star_su; //최종코인수
    }
    public UserData rslt_set;
}


