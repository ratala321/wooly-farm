using System.Collections;
using UnityEngine;
using System;
using Unity.Mathematics;
using Unity.Netcode;


namespace Utils
{
    public class ShootingUtility :  MonoBehaviour
    {
    
        public float TimeToFly {set; private get;}
        public float Angle {set; private get;} 
        public GameObject ObjectToFire {set; private get;}

        private bool _hasFinished; 

        private GameObject _objectInstance;

        public void FireBetween(Vector3 startPosition, Vector3 endPosition)
        {
            _hasFinished = false;
            InstantiateObjectToFire(startPosition);
            Vector3 middlePosition = GetThirdPoint(startPosition, endPosition, Angle);
            StartCoroutine(MoveObject(startPosition, middlePosition, endPosition));

        }

        private void InstantiateObjectToFire(Vector3 position)
        {
            _objectInstance = Instantiate(ObjectToFire, position, quaternion.identity);
            _objectInstance.GetComponent<NetworkObject>().Spawn(true);
        }
  
        private IEnumerator MoveObject(Vector3 startPoint, Vector3 middlePoint, Vector3 endPoint)
        {
            float timer = 0; 
            while (timer < TimeToFly)
            {
                timer += Time.deltaTime ;
                float ratio = Math.Min(timer / TimeToFly, 1.0f);
                _objectInstance.transform.position = RunBezier(startPoint, middlePoint, endPoint, ratio);                
                yield return null;
            }
            Destroy(_objectInstance);
            _hasFinished = true;
        }

        private static Vector3 RunBezier(Vector3 startPoint, Vector3 middlePoint,Vector3 endPoint, float ratio )
        {
            Vector3 bezierPosition = (1-ratio) * (1-ratio) * startPoint + (2 * ratio)* (1-ratio) * middlePoint + ratio * ratio *endPoint;
            return bezierPosition;
        }
        private static Vector3 GetThirdPoint(Vector3 initpos, Vector3 targetPos, float startingAngle)
        {
            // On cherche le point du milieu entre init et target
            Vector3 midPoint = Vector3.Lerp(initpos, targetPos, 0.5f);
            float distance = Vector3.Distance(initpos, midPoint);
            float oppositeLength = (float) Math.Tan(startingAngle) * distance; 
            Vector3 oppositeVector = Vector3.up * oppositeLength;
            Vector3 thirdPointPosition = midPoint + oppositeVector;
             
            return thirdPointPosition;
        }
        public bool HasFinished()
        {
            return _hasFinished;
        } 
        
    }
}