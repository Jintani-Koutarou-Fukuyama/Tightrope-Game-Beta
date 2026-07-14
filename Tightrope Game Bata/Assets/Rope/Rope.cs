using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    // ロープ全体にアクセスするために使う（使わないかも）
    [SerializeField] private List<RopeParts> ropeParts;


    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    // 真ん中のロープのパーツを取得する
    public RopeParts GetMiddleRopePart()
    {
        return ropeParts[ropeParts.Count / 2];
    }

    // ランダムなロープのパーツを取得する
    public RopeParts GetRandomRopePart()
    {
        int ramdom = Random.Range((int)0, (int)ropeParts.Count);
        return ropeParts[ramdom];
    }
    public RopeParts GetRandomRopePart(int min, int max)
    {
        if (min < 0 || max > ropeParts.Count)
        {
            return GetMiddleRopePart();
        }

        int ramdom = Random.Range(min, max);
        return ropeParts[ramdom];
    }
}
