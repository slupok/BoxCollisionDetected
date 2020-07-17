using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

/*

    o---------------o
   /|              /|
  / |             / |
 /  |            /  |
o---|-----------o   |
|   |           |   |
|   |           |   |   
|   o-----------|---o   
|  /            |  /
| /             | /
|/              |/
o---------------o

*/




public class Box
{
    public Vector3 Center;
    public Vector3 Size;
    public Quaternion Quaternion;

    public Box(Vector3 center, Vector3 size, Quaternion quaternion)
    {
		this.Center = center;
		this.Size = size;
		this.Quaternion = quaternion;
    }

    public Box(GameObject obj)
    {
        Center = obj.transform.position;
        Size = obj.transform.lossyScale;
        Quaternion = obj.transform.rotation;
    }

}

[CreateAssetMenu(menuName = "BoxCollision")]
public class BoxCollision
{
    //поворот вектора кватернионом
    private static Vector3 QuanRotation(Vector3 v,Quaternion q)
    {
        float u0 = v.x * q.x + v.y * q.y + v.z * q.z;
        float u1 = v.x * q.w - v.y * q.z + v.z * q.y;
        float u2 = v.x * q.z + v.y * q.w - v.z * q.x;
        float u3 = -v.x * q.y + v.y * q.x + v.z * q.w;
        Quaternion M = new Quaternion(u1,u2,u3,u0);
        
        Vector3 resultVector;
        resultVector.x = q.w * M.x + q.x * M.w + q.y * M.z - q.z * M.y;  
        resultVector.y = q.w * M.y - q.x * M.z + q.y * M.w + q.z * M.x;
        resultVector.z = q.w * M.z + q.x * M.y - q.y * M.x + q.z * M.w;
        
        return resultVector;
    }
    //проекция точки v на ветор a
    private static float ProjVector3(Vector3 v, Vector3 a)
    {
        a = a.normalized;
        return Vector3.Dot(v, a) / a.magnitude;
        
    }
    private static Vector3[] GetPoint(Box box)
    {
        Vector3 center = box.Center;
        Quaternion q = box.Quaternion;
        Vector3 size = box.Size;
        
        Vector3[] point = new Vector3[8];
        
        //получаем координаты вершин
        point[0] = box.Center - box.Size/2;
        point[1] = point[0] + new Vector3(box.Size.x , 0, 0);
        point[2] = point[0] + new Vector3(0, box.Size.y, 0);
        point[3] = point[0] + new Vector3(0, 0, box.Size.z);
		
		//таким же образом находим оставшееся точки
        point[4] = box.Center + box.Size / 2;
        point[5] = point[4] - new Vector3(box.Size.x, 0, 0);
        point[6] = point[4] - new Vector3(0, box.Size.y, 0);
        point[7] = point[4] - new Vector3(0, 0, box.Size.z);

        //поворачиваем вершины кватернионом
        for (int i = 0; i < 8; i++)
        {
            point[i] -= box.Center;//перенос центра в начало координат

            point[i] = QuanRotation(point[i], box.Quaternion);//поворот

            point[i] += box.Center;//обратный перенос
        }
        
        return point;
    }
    
    
    //получение возможных разделяющих осей 
    private static List<Vector3> GetAxis(Vector3[] a, Vector3[] b)
    {
		//ребра
        Vector3 A;
        Vector3 B;
		//потенциальные разделяющие оси
        List<Vector3> Axis = new List<Vector3>();

		//нормали плоскостей первого куба
        for (int i = 1; i < 4; i++)
        {
            A = a[i] - a[0];
            B = a[(i+1)%3+1] - a[0];
            Axis.Add(Vector3.Cross(A,B).normalized);
        }
		//нормали второго куба
        for (int i = 1; i < 4; i++)
        {
            A = b[i] - b[0];
            B = b[(i+1)%3+1] - b[0];
            Axis.Add(Vector3.Cross(A,B).normalized);
        }

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
        return Axis;
    }
    //проекция на оси
    private static void ProjAxis(out float min, out float max, Vector3[] points, Vector3 Axis)
    {
        max = ProjVector3(points[0], Axis);
        min = ProjVector3(points[0], Axis);
        for (int i = 0; i < points.Length; i++)
        {
            float tmp = ProjVector3(points[i], Axis);
            if (tmp > max)
            {
                max = tmp;
            }

            if (tmp < min)
            {
                min= tmp;
            }
        }
    }
    //определение пересечений
    private static Vector3 IntersectionOfProj(Vector3[] a, Vector3[] b, List<Vector3> Axis)
    {
        Vector3 norm = new Vector3(1000,1000,1000);
        //простым нахождение мин. и макс. точек куба по заданной оси
        for (int j = 0; j < Axis.Count; j++)
        {
            //проекции куба a
            float max_a;
            float min_a;
            ProjAxis(out min_a,out max_a,a,Axis[j]);

            //проекции куба b
            float max_b;
            float min_b;
            ProjAxis(out min_b,out max_b,b,Axis[j]);

            float[] points = {min_a, max_a, min_b, max_b};
            Array.Sort(points);

            float sum = (max_b - min_b) + (max_a - min_a);
            float len = Math.Abs(points[3] - points[0]);
            
            if (sum <= len)
            {
                //разделяющая ось существует
                // объекты не пересекаются
                return new Vector3(0,0,0);
            }
            float dl = Math.Abs(points[2] - points[1]);
            if (dl < norm.magnitude)
            {
                norm = Axis[j] * dl;
                //ориентация нормы
                if(points[0] != min_a)
                    norm = -norm;
            }

        }
        return norm;
    }

    //главный метод
    public static Vector3 Collision(Box box1, Box box2)
    {
        //получаем позицию центра кубов
        var point = GetPoint(box1);
        var point2 = GetPoint(box2);

        List<Vector3> axis = GetAxis(point, point2);

        return IntersectionOfProj(point, point2, axis);
    }
    
}
    