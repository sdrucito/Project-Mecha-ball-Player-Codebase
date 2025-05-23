using System;
using Player.PlayerController;
using UnityEngine;
using UnityEngine.Events;

/*
 * Superclass for a Control Module, used by the ControlModuleManager
 * to group all the available modules and decide which one is active
 */
namespace Player.ControlModules
{
    public class ControlModule : MonoBehaviour
    {
        protected const string Name = "None";
        public Action OnActivated;
        public bool IsActive {get; set;} = true;
        public string GetName()
        {
            return Name;
        }
        
    }
}
