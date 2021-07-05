
using UnityEngine;

/*
 * 코인의 획득 연출에서 이동 처리를 위한 클래스.
 * 기준 범위내에서 어느정도 규칙성대로 컨트롤 됨
 */
public class RewardCoin : MonoBehaviour
{
	private static readonly Vector3 _beginLocate = new Vector3(0, 96.67749f, 0);
	private Vector3 _dropVelocity; //동전의 중력 값.
	private float _gravity;
	private RectTransform _rectTransform;
	private Vector3 _goalLocation;
	public RectTransform target;

	private Vector3 _magneto; //끌어당기는 힘의 변화량. 끌어당기는 힘의 크기는 일정하지 않고 변화하기 때문에 이 값이 필요함. x, y 축 각각 다르게 하기위해 Vector로 선언
	private Vector3 _magneticVelocity; //끌어당기는 방향, 힘. 중력 벡터 값에 이 값이 더해지면 최종적인 동전의 이동 방향이 결정된다.
    private float _distance;
	private bool _accelTrigger;

	private BlueRed _br;

    public void Set(BlueRed br)
	{
		_goalLocation = target.anchoredPosition3D;
		_br = br;
		gameObject.SetActive(true);

		if (_rectTransform == null)
		{
			_rectTransform = GetComponent<RectTransform>();
		}
		_rectTransform.anchoredPosition = new Vector2(Random.Range(0, 100) - 50, -800f);

		_dropVelocity.x = 0;
		_dropVelocity.y = -1f; //수직 아래 방향으로
		_dropVelocity.z = 0;
		//_dropVelocity.Normalize();
		//_dropVelocity *= Random.Range(10, 16); //10~16의 힘으로 중력 값이 설정
		//_dropVelocity *= Random.Range(30, 46); //10~16의 힘으로 중력 값이 설정
		_gravity = Random.Range(1400, 2000); //10~16의 힘으로 중력 값이 설정 (초당 이동 거리)

		_magneto.x = Random.Range(900f, 1200f); //가로 방향으로 끌어당기는 힘
		_magneto.y = 1f; //세로 방향으로 끌어당기는 힘

		_distance = Vector3.Distance(_goalLocation, _rectTransform.anchoredPosition3D);
		_accelTrigger = false;

	}

	Vector3 move;
	void Update()
	{
		UpdateMagnetoMove();
		move = _dropVelocity * (_gravity * Time.deltaTime);
		move += _magneticVelocity; //중력벡터 + 자력벡터 = 이동벡터
		move += _rectTransform.anchoredPosition3D;

		//transform.Translate(move);
		_rectTransform.anchoredPosition3D = move;


		if(Vector3.Distance(_rectTransform.anchoredPosition3D, _goalLocation) < 50f) //목표 지점의 일정 범위 내로 들어오면 도착으로 간주시킴.
        { //도착 => 코인의 소멸처리 => 동전 오브젝트의 재사용을 위해 pool에 다시 집어 넣음
			BlueRed.coinPool.Enqueue(this);
			gameObject.SetActive(false);
			//_ms.BeginGainFlashAnim();
			_br.PlayAudio(0, _br.acBetCoin);
		}

		
		//Debug.Log("_magneticVelocity: " + _magneticVelocity.ToString());
	}

	private void UpdateMagnetoMove()
	{  //끌어당기는 힘의 변화량을 처리
		float distance = Vector3.Distance(_goalLocation, _rectTransform.anchoredPosition3D);
		if (_accelTrigger == false && _distance >= distance) //가까워지고 있나
		{
		}
		else //멀어지고 있나
		{
			//_magneto.x += 1f;
			_magneto.y += 300f;

			_accelTrigger = true;
		}
		_distance = distance;

		_magneticVelocity = _goalLocation - _rectTransform.anchoredPosition3D;
		_magneticVelocity.Normalize();
		//_magneticVelocity.x *= _magneto.x;
		//_magneticVelocity.y *= _magneto.y;
		Vector3 delta = _magneto * Time.deltaTime;
		_magneticVelocity.x *= delta.x;
		_magneticVelocity.y *= delta.y;
	}
}

