using UnityEngine;

public class HelpFunctions {
    public static Vector3 GetMousePosition()
     {
         if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity))
         {
             return hit.point;
         }
 
         return default;
     }
    
    public static float Distance2D(Vector3 posA, Vector3 posB)
    {
        return Mathf.Abs(Vector2.Distance(new Vector2(posA.x, posA.z), new Vector2(posB.x, posB.z)));
    }

    /// <summary>
    /// get final allowed range between a requested position and the initial position
    /// </summary>
    /// <param name="initialPosition"> initial position of the caster </param>
    /// <param name="requestedPosition"> position requested by the client </param>
    /// <param name="minRange"> min allowed range of the action </param>
    /// <param name="maxRange"> max allowed range of the action </param>
    /// <returns>final allowed range</returns>
    public static float GetFinalRange(Vector3 initialPosition, Vector3 requestedPosition, float minRange, float maxRange, bool isOnMousePosition)
    {
        if (! isOnMousePosition)
        {
            return maxRange;
        }
        
        var distance = Distance2D(initialPosition, requestedPosition);
            
        return Mathf.Clamp(distance, minRange, maxRange);
    }
    
    /// <summary>
    /// Get final allowed position of requested position restrained to Max / Min range 
    /// </summary>
    /// <param name="initialPosition"> initial position of the caster </param>
    /// <param name="requestedPosition"> position requested by the client </param>
    /// <param name="minRange"> min allowed range of the action </param>
    /// <param name="maxRange"> max allowed range of the action </param>
    /// <returns>final allowed position</returns>
    public static Vector3 GetFinalPosition(Vector3 initialPosition, Vector3 requestedPosition, float minRange, float maxRange, bool isOnMousePosition)
    {
        var finalRange = GetFinalRange(initialPosition, requestedPosition, minRange, maxRange, isOnMousePosition);

        var initialDistance = Distance2D(initialPosition, requestedPosition);
            
        return initialPosition + (requestedPosition - initialPosition) * finalRange / initialDistance;
    }
}