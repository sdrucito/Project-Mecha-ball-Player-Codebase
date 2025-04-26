using System;
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

        protected string _name = "None";
        public Action OnActivated;
        public string GetName()
        {
            return _name;
        }
    }
}
