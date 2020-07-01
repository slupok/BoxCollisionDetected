using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move : MonoBehaviour
{
    public float Speed = 4;
    private Vector3 _direction;
    
    private void Update()
    {
        _direction = new Vector3(Input.GetAxis("Horizontal"),0,Input.GetAxis("Vertical"));
        if (Input.GetKey(KeyCode.Space))
            _direction += Vector3.up;
        if (Input.GetKey(KeyCode.LeftShift))
            _direction += Vector3.down;

        transform.position += _direction.normalized * (Time.deltaTime * Speed);
    }
}
