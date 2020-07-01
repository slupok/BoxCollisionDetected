using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Physics : MonoBehaviour
{
    //отталкивает текущий куб от кубов которые указаны в массиве
    [SerializeField]private GameObject[] _boxesObj;
    
    void Update()
    {
        Box _thisBox = new Box(gameObject);

        foreach (var obj in _boxesObj)
        {
            Box box = new Box(obj);
            Vector3 result = BoxCollision.Collision(_thisBox, box);
            if (result.magnitude != 0)
                gameObject.transform.position -= result;
        }
    }
}
