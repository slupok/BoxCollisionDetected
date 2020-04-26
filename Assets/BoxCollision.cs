using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public class BoxCollision : MonoBehaviour
{
    //ориентация находится по отношению первого куба ко второму т.е.
    
    //кубы
    public GameObject kub;
    public GameObject kub2;


    //центр и вершины
    //public Vector3 center;
    //private Vector3[] point = new Vector3[8];
    
    //public Vector3 center2;
    //private Vector3[] point2 = new Vector3[8];

    public static Vector3 GetNormal(Vector3 a, Vector3 b)
    {
        Vector3 result;
        result.x = a.y * b.z - a.z * b.y;
        result.y = a.z * b.x - a.x * b.z;
        result.z = a.x * b.y - a.y * b.x;
        return result;
    }
    //поворот вектор на кватернион
    public Vector3 QuanRotation(Vector3 tmp,Quaternion q)
    {
        //формула поворота
        // a = q*v*q^(-1)
        //a - новый вектор
        //q - кватернион
        //q^(-1) - обратный кватернион
        //v - начальный вектор
        
        float u0 = tmp.x * q.x + tmp.y * q.y + tmp.z * q.z;
        float u1 = tmp.x * q.w - tmp.y * q.z + tmp.z * q.y;
        float u2 = tmp.x * q.z + tmp.y * q.w - tmp.z * q.x;
        float u3 = -tmp.x * q.y + tmp.y * q.x + tmp.z * q.w;
        //M = v*q^(-1)
        Quaternion M = new Quaternion(u1,u2,u3,u0);

        //a = v*M
        Vector3 a;
        a.x = q.w * M.x + q.x * M.w + q.y * M.z - q.z * M.y;  
        a.y = q.w * M.y - q.x * M.z + q.y * M.w + q.z * M.x;
        a.z = q.w * M.z + q.x * M.y - q.y * M.x + q.z * M.w;
        
        return a;
    }
    //проекция точки v на ветор a
    public float ProjVector3(Vector3 v, Vector3 a)
    {
        //получение вектора oa
        a = a.normalized;
        //получаем проекцию

        float alpha = Vector3.Dot(v, a) / a.magnitude;
        //float alpha = (v.x * a.x + v.y * a.y + v.z * a.z) / a.magnitude;
        return alpha;
        
    }
    public Vector3[] GetPoint(GameObject p)
    {
        Vector3 center = p.transform.position;
        Quaternion q = p.transform.rotation;
        Vector3 size = p.transform.lossyScale;
        
        Vector3[] point = new Vector3[8];
        
        //получаем координаты вершин
        point[0] = center - size/2;
        point[1] = point[0] + new Vector3(size.x , 0, 0);
        point[2] = point[0] + new Vector3(0, size.y, 0);
        point[3] = point[0] + new Vector3(0, 0, size.z);

        point[4] = center + size / 2;
        point[5] = point[4] - new Vector3(size.x, 0, 0);
        point[6] = point[4] - new Vector3(0, size.y, 0);
        point[7] = point[4] - new Vector3(0, 0, size.z);

        //поворачиваем вершины на кватернион
        for (int i = 0; i < 8; i++)
        {
            point[i] -= center;

            point[i] = QuanRotation(point[i], q);

            point[i] += center;
        }
        
        return point;
    }
    
    
    //получение возможных разделяющих осей 
    public List<Vector3> GetAxis(Vector3[] a, Vector3[] b)
    {
        Vector3 A;
        Vector3 B;
        List<Vector3> Axis = new List<Vector3>();

        for (int i = 1; i < 4; i++)
        {
            A = a[i] - a[0];
            B = a[(i+1)%3+1] - a[0];
            Axis.Add(Vector3.Cross(A,B).normalized);
        }
        /*
        A = a[1] - a[0];
        B = a[2] - a[0];
        Axis.Add(GetNormal(A,B).normalized);
        
        A = a[2] - a[0];
        B = a[3] - a[0];
        Axis.Add(GetNormal(A,B).normalized);
        
        A = a[1] - a[0];
        B = a[3] - a[0];
        Axis.Add(GetNormal(A,B).normalized);
        */
        for (int i = 1; i < 4; i++)
        {
            A = b[i] - b[0];
            B = b[(i+1)%3+1] - b[0];
            Axis.Add(Vector3.Cross(A,B).normalized);
        }
        /*
        A = b[1] - b[0];
        B = b[2] - b[0];
        Axis.Add(GetNormal(A,B).normalized);
        
        A = b[1] - b[0];
        B = b[3] - b[0];
        Axis.Add(GetNormal(A,B).normalized);
        
        A = b[2] - b[0];
        B = b[3] - b[0];
        Axis.Add(GetNormal(A,B).normalized);
        */
        //Теперь добавляем все векторные произведения
        for (int i = 1; i < 4; i++)
        {
            A = a[i] - a[0];
            for (int j = 1; j < 4; j++)
            {
                B = b[j] - b[0];
                if (Vector3.Cross(A,B).magnitude != 0)
                {
                    Axis.Add(Vector3.Cross(A,B).normalized);
                }
            }
        }
        
        /*
        Теперь добавляем все векторные произведения
        for (int i = 1; i < 4; i++)
        {
            A = a[i] - a[0];
            for (int j = 1; j < 4; j++)
            {
                B = b[j] - b[0];
                Axis.Add(GetNormal(A,B));
            }
        }*/

        return Axis;
    }
    
    //проекция на оси
    public Vector3 IntersectionOfProj(Vector3[] a, Vector3[] b, List<Vector3> Axis)
    {
        Vector3 norm = new Vector3(1000,1000,1000);
        //простым нахождение мин. и макс. точек куба по заданной оси
        for (int j = 0; j < Axis.Count; j++)
        {
            //проекции куба a
            float max_a;
            max_a = ProjVector3(a[0], Axis[j]);
            float min_a;
            min_a = ProjVector3(a[0], Axis[j]);
            for (int i = 0; i < b.Length; i++)
            {
                float tmp = ProjVector3(a[i], Axis[j]);
                if (tmp > max_a)
                {
                    max_a = tmp;
                }

                if (tmp < min_a)
                {
                    min_a= tmp;
                }
            }
            
            //проекции куба b
            float max_b;
            max_b = ProjVector3(b[0], Axis[j]);
            float min_b;
            min_b = ProjVector3(b[0], Axis[j]);
            for (int i = 0; i < b.Length; i++)
            {
                float tmp = ProjVector3(b[i], Axis[j]);
                if (tmp > max_b)
                {
                    max_b = tmp;
                }

                if (tmp < min_b)
                {
                    min_b = tmp;
                }
            }

            float[] p = {min_a, max_a, min_b, max_b};
            Array.Sort(p);

            float sum = (max_b - min_b) + (max_a - min_a);
            float len = Math.Abs(p[3] - p[0]);
            
            if (sum <= len)
            {
                // Debug.Log("Результат: Непересек");
                return new Vector3(0,0,0);
            }
            else
            {
                float dl = Math.Abs(p[2] - p[1]);
                if (dl < norm.magnitude)
                {
                    norm = Axis[j] * dl;
                    //найти ариентацию нормы
                    if(p[0] != min_a)
                        norm = -norm;
                    
                }
                //Debug.Log(norm);
            }

        }
        return norm;
    }

    //главный метод
    public Vector3 Collision(GameObject kub, GameObject kub2)
    {
        //получаем позицию центра кубов
        var point = GetPoint(kub);
        var point2 = GetPoint(kub2);

        List<Vector3> axis = GetAxis(point, point2);

        return IntersectionOfProj(point, point2, axis);
    }

    // Start is called before the first frame update
    void Start()
    {

        //получаем вершины кубов
        //point = GetPoint(kub);
        //point2 = GetPoint(kub2);
    }


    // Update is called once per frame
    void Update()
    {
        
        
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * 4*Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * 4*Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * 4*Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * 4*Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            transform.position += transform.up * 4*Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            transform.position -= transform.up *4*Time.deltaTime;
        }

        Vector3 result = Collision(kub, kub2);
        if (result.magnitude == 0)
        {
        }
        else
        {
            Debug.Log("пересеклись");
            Debug.Log(result);
            kub2.transform.position += result;

        }

/*
        //проверяем пересечение вписаных шаров
            float min = kub.transform.localScale.x;
            float tmp = kub.transform.localScale.y;
            if (tmp < min)
                min = tmp;
            tmp = kub.transform.localScale.z;
            if (tmp < min)
                min = tmp;
            
            float min2 = kub2.transform.localScale.x;
            tmp = kub2.transform.localScale.y;
            if (tmp < min2)
                min2 = tmp;
            tmp = kub2.transform.localScale.z;
            if (tmp < min2)
                min2 = tmp;
            if((min+min2)/2 > Math.Abs((kub.transform.position - kub2.transform.position).magnitude))
            {
                Debug.Log("пересеклись");
            //пересеклись 100%
            }
            else
            {
                //Проверяем НЕ пересечение описаных шаров
                float t1 = (kub.transform.localScale / 2).magnitude;
                float t2 = (kub2.transform.localScale / 2).magnitude;
                if (Math.Abs((kub.transform.position - kub2.transform.position).magnitude) > t1 + t2) 
                    Debug.Log("Не пересеклись"); 
                else
                {
                    //ищем проекции двух кубов
                    if (result.magnitude == 0)
                    {
                    }
                    else
                    {
                    Debug.Log("пересеклись");
                    Debug.Log(result);
                    kub2.transform.position += result*2;

                    }
                }

            }
*/

    }
}
    