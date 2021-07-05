using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BetCoinPool : MonoBehaviour
{
    public int initCapacity = 50;
    public BetCoin itemSource;

    private Stack<BetCoin> _objActive = new Stack<BetCoin>();
    private Stack<BetCoin> _objPool = new Stack<BetCoin>();

    public Stack<BetCoin> activeObjects
    {
        get => _objActive;
    }

    void Start()
    {
        Assign(initCapacity);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public BetCoin GetNewItem()
    {
        if (_objPool.Count == 0)
        {
            Assign(10);
        }
        var item = _objPool.Pop();
        item.gameObject.SetActive(true);
        _objActive.Push(item);

        item.transform.SetSiblingIndex(_objActive.Count - 1); //최종 오브젝트가 상단 레이어로 위치
        return item;
    }

    public void RestoreAll()
    {
        while (_objActive.Count > 0)
        {
            var item = _objActive.Pop();
            item.gameObject.SetActive(false);
            _objPool.Push(item);
        }
    }


    private BetCoin CreateItem()
    {
        var item = BetCoin.Instantiate(itemSource) as BetCoin;
        item.gameObject.SetActive(false);
        item.transform.SetParent(transform);

        return item;
    }

    private void Assign(int size)
    {
        for (int i = 0; i < size; i++)
        {
            _objPool.Push(CreateItem());
        }
    }



}
