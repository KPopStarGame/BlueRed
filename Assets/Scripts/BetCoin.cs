using UnityEngine;

public class BetCoin : MonoBehaviour
{
    public UnityEngine.UI.Image coinImage;
    public UnityEngine.UI.Text numText;

    public Vector2 from
    {
        get => _from;
        set 
        {
            transform.localPosition = value;
            _move = true;
            _from = value;
            _dir = _to - _from;
            _length = _dir.magnitude;
            _dir.Normalize();
            _accum = 0;
        }
    }

    public Vector2 to
    {
        get => _to;
        set
        {
            _move = true;
            _to = value;
            _dir = _to - _from;
            _length = _dir.magnitude;
            _dir.Normalize();
            _accum = 0;
        }
    }

    private bool _move;
    private Vector2 _from;
    private Vector2 _to;
    private Vector2 _dir;
    private float _length;
    private float _accum;
    private const float _velocity = 4000f; //초당 이동 거리

    [HideInInspector] public bool hideReach;


    void OnEnable()
    {
        _move = false;
    }

    float _delta;
    void Update()
    {
        if (!_move) return;

        _delta = _velocity * Time.deltaTime;
        if (_accum + _delta >= _length) //이동량이 초과 되면
        {
            _delta = _length - _accum;
            _move = false; //이동을 끝냄

            if(hideReach)
            {
                hideReach = false;
                gameObject.SetActive(false);
            }
        }

        _accum += _delta;
        //transform.Translate( _dir * _delta );

        Vector3 dir = _dir;
        dir *= _delta;
        Vector3 next = transform.localPosition + dir;
        transform.localPosition = next;

    }
}
