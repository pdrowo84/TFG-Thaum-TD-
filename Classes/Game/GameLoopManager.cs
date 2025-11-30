using UnityEngine;
using System.Collections;   
using System.Collections.Generic;   

public class GameLoopManager : MonoBehaviour 
{
    public bool LoopShouldEnd;
    private void Start()
    {
        
    }

    IEnumerator GameLoop()
    {
        while (LoopShouldEnd == false)
        {
          
            //Spawn Enemies

            //Spawn Towers

            //Move Enemies

            //Tick Towers

            //Apply Effects

            //Damage Enemies

            //Remove Enemies

            //Remove Towers

            yield return null;  

        }
    }
}
